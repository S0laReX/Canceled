using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// SYS-070: every interactive IMGUI control registers itself with an id and rect while it is drawn. Arrow keys,
    /// the bound movement keys, d-pad or stick move a visible focus spatially; Enter/Space/A activate; left/right adjust
    /// value controls; B goes back from the pause. The mouse keeps working and also moves the focus.
    /// </summary>
    public sealed partial class BrawlHud
    {
        private struct NavItem { public string Id; public Rect Rect; public Action Activate; public Action<int> Adjust; }

        private List<NavItem> navItems = new List<NavItem>(), navBuilding = new List<NavItem>();
        private string focusId;
        private MatchPhase navPhase = (MatchPhase)(-1);
        private bool navDevicePause;
        private int navPage;
        private readonly Dictionary<Gamepad, (Vector2Int dir, float next)> stickRepeat = new Dictionary<Gamepad, (Vector2Int, float)>();
        private const float RepeatDelay = .35f, RepeatRate = .12f;

        /// <summary>Lets automated checks drive menus while gameplay automation stays on.</summary>
        public bool ForceNavigation { get; set; }
        public string FocusId => focusId;
        public IReadOnlyList<string> FocusableIds { get { var ids = new List<string>(); foreach (var item in navItems) ids.Add(item.Id); return ids; } }
        public void Focus(string id) => focusId = id;

        /// <summary>Registers the control drawn at <paramref name="rect"/>. Disabled controls are not focusable.</summary>
        private void Nav(string id, Rect rect, Action activate, Action<int> adjust = null)
        {
            if (!GUI.enabled) return;
            Event current = Event.current;
            if (current.type == EventType.MouseDown && rect.Contains(current.mousePosition)) focusId = id;
            if (current.type != EventType.Repaint) return;
            // Groups use different anchors/scales, so spatial navigation compares rects in screen space.
            Vector3 a = GUI.matrix.MultiplyPoint3x4(new Vector3(rect.xMin, rect.yMin, 0));
            Vector3 b = GUI.matrix.MultiplyPoint3x4(new Vector3(rect.xMax, rect.yMax, 0));
            navBuilding.Add(new NavItem { Id = id, Rect = Rect.MinMaxRect(a.x, a.y, b.x, b.y), Activate = activate, Adjust = adjust });
            if (id == focusId) Outline(rect, 3, Paper);
        }

        private void Outline(Rect rect, float thickness, Color color)
        {
            Rect r = new Rect(rect.x - thickness - 2, rect.y - thickness - 2, rect.width + 2 * thickness + 4, rect.height + 2 * thickness + 4);
            Fill(new Rect(r.x, r.y, r.width, thickness), color);
            Fill(new Rect(r.x, r.yMax - thickness, r.width, thickness), color);
            Fill(new Rect(r.x, r.y, thickness, r.height), color);
            Fill(new Rect(r.xMax - thickness, r.y, thickness, r.height), color);
        }

        /// <summary>Called at the end of each repaint: the frame's registrations become the navigable set.</summary>
        private void CommitNavigation()
        {
            if (Event.current.type != EventType.Repaint) return;
            (navItems, navBuilding) = (navBuilding, navItems);
            navBuilding.Clear();
        }

        private string DefaultFocus()
        {
            switch (match.Phase)
            {
                case MatchPhase.Menu: return "start";
                case MatchPhase.Paused: return match.MissingDeviceMask != 0 ? "assign" : "continue";
                case MatchPhase.Results: return "rematch";
                default: return null;
            }
        }

        /// <summary>The 3D view ends exactly where the bottom HUD strip begins, at any aspect ratio and HUD size.</summary>
        private void LateUpdate()
        {
            if (match == null) return;
            if (markerCamera == null) markerCamera = Camera.main;
            if (markerCamera == null) return;
            int cards = match.Phase == MatchPhase.Menu || match.Fighters == null ? 0 : match.Fighters.Length;
            var layout = BrawlHudLayout.Compute(Screen.width, Screen.height, match.HudScale, cards);
            float strip = layout.StripFraction;
            var wanted = new Rect(0, strip, 1, 1 - strip);
            if (markerCamera.rect != wanted) markerCamera.rect = wanted;
            // The top HUD (title, clock) covers this share of the 3D view; side framing keeps fighters below it.
            var group = markerCamera.GetComponent<BrawlCamera>();
            if (group != null) group.TopReserve = 125 * layout.HudScale / Mathf.Max(1, layout.ViewHeight - layout.StripHeight);
        }

        private int IndexOf(string id) { for (int i = 0; i < navItems.Count; i++) if (navItems[i].Id == id) return i; return -1; }

        private void Update()
        {
            if (match == null) return;
            bool menuScreen = match.Phase == MatchPhase.Menu || match.Phase == MatchPhase.Paused || match.Phase == MatchPhase.Results;
            // A new screen starts on its main action.
            if (match.Phase != navPhase || match.DevicePause != navDevicePause || match.OptionsPage != navPage)
            {
                bool pageOnly = match.Phase == navPhase && match.DevicePause == navDevicePause;
                navPhase = match.Phase; navDevicePause = match.DevicePause; navPage = match.OptionsPage;
                if (!pageOnly) { focusId = DefaultFocus(); navItems.Clear(); }
            }
            if (!menuScreen || match.RemapListening || navItems.Count == 0) return;
            if (match.AutomatedTest && !ForceNavigation) return;
            if (IndexOf(focusId) < 0) { string fallback = DefaultFocus(); focusId = IndexOf(fallback) >= 0 ? fallback : navItems[0].Id; }

            ReadNavigation(out Vector2Int move, out bool confirm, out bool back);
            NavItem focused = navItems[IndexOf(focusId)];
            if (confirm) { focused.Activate?.Invoke(); return; }
            if (back && match.Phase == MatchPhase.Paused && match.MissingDeviceMask == 0) { match.TogglePause(); return; }
            if (move.x != 0 && focused.Adjust != null) { focused.Adjust(move.x); return; }
            if (move != Vector2Int.zero) MoveFocus(focused, move);
        }

        /// <summary>Nearest control in the pressed direction, preferring alignment with the current one.</summary>
        private void MoveFocus(NavItem from, Vector2Int move)
        {
            Vector2 direction = new Vector2(move.x, -move.y); // GUI y grows downwards
            Vector2 origin = from.Rect.center;
            float best = float.MaxValue; string target = null;
            foreach (var item in navItems)
            {
                if (item.Id == from.Id) continue;
                Vector2 delta = ClosestOffset(from.Rect, item.Rect, origin);
                float along = Vector2.Dot(delta, direction);
                if (along <= 1) continue;
                float across = Mathf.Abs(direction.x != 0 ? delta.y : delta.x);
                float score = along + across * 2.2f;
                if (score < best) { best = score; target = item.Id; }
            }
            if (target != null) focusId = target;
        }

        /// <summary>Offset to the target measured edge-aware, so wide buttons and small cells compare fairly.</summary>
        private static Vector2 ClosestOffset(Rect from, Rect to, Vector2 origin)
        {
            Vector2 centre = to.center;
            float x = to.xMin > from.xMax ? to.xMin - from.xMax : from.xMin > to.xMax ? to.xMax - from.xMin : 0;
            float y = to.yMin > from.yMax ? to.yMin - from.yMax : from.yMin > to.yMax ? to.yMax - from.yMin : 0;
            // Overlapping on an axis: use centre offset on that axis so siblings in a row/column still order correctly.
            if (x == 0) x = (centre.x - origin.x) * .25f;
            if (y == 0) y = (centre.y - origin.y) * .25f;
            return new Vector2(x, y);
        }

        private void ReadNavigation(out Vector2Int move, out bool confirm, out bool back)
        {
            move = Vector2Int.zero; confirm = false; back = false;
            foreach (var device in InputSystem.devices)
            {
                if (!(device is Keyboard keys) || !device.enabled) continue;
                if (keys.upArrowKey.wasPressedThisFrame || BoundKey(keys, BrawlAction.MoveUp)) move.y = 1;
                if (keys.downArrowKey.wasPressedThisFrame || BoundKey(keys, BrawlAction.MoveDown)) move.y = -1;
                if (keys.leftArrowKey.wasPressedThisFrame || BoundKey(keys, BrawlAction.MoveLeft)) move.x = -1;
                if (keys.rightArrowKey.wasPressedThisFrame || BoundKey(keys, BrawlAction.MoveRight)) move.x = 1;
                confirm |= keys.enterKey.wasPressedThisFrame || keys.numpadEnterKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame;
            }
            foreach (var pad in Gamepad.all)
            {
                if (!BrawlDeviceSlots.Available(pad)) continue;
                // In the pause only the round's own pads navigate; spare pads stay inert as in gameplay.
                if (match.Phase == MatchPhase.Paused && !match.Devices.Owns(pad)) continue;
                if (pad.dpad.up.wasPressedThisFrame) move.y = 1;
                if (pad.dpad.down.wasPressedThisFrame) move.y = -1;
                if (pad.dpad.left.wasPressedThisFrame) move.x = -1;
                if (pad.dpad.right.wasPressedThisFrame) move.x = 1;
                Vector2Int stick = StickStep(pad);
                if (stick != Vector2Int.zero) move = stick;
                confirm |= pad.buttonSouth.wasPressedThisFrame;
                back |= pad.buttonEast.wasPressedThisFrame;
            }
            if (move.x != 0 && move.y != 0) move.y = 0;
        }

        private bool BoundKey(Keyboard keys, BrawlAction action)
        {
            var control = BrawlBindings.Resolve(match.Bindings.Get(BindingScheme.Keyboard, action), keys, null);
            return control != null && control.wasPressedThisFrame;
        }

        /// <summary>Stick acts like a d-pad with auto-repeat: one step, a pause, then steady steps while held.</summary>
        private Vector2Int StickStep(Gamepad pad)
        {
            Vector2 value = pad.leftStick.ReadValue();
            Vector2Int dir = value.magnitude < .6f ? Vector2Int.zero :
                Mathf.Abs(value.x) > Mathf.Abs(value.y) ? new Vector2Int(value.x > 0 ? 1 : -1, 0) : new Vector2Int(0, value.y > 0 ? 1 : -1);
            stickRepeat.TryGetValue(pad, out var state);
            float now = Time.unscaledTime;
            if (dir == Vector2Int.zero) { stickRepeat.Remove(pad); return Vector2Int.zero; }
            if (dir != state.dir) { stickRepeat[pad] = (dir, now + RepeatDelay); return dir; }
            if (now >= state.next) { stickRepeat[pad] = (dir, now + RepeatRate); return dir; }
            return Vector2Int.zero;
        }
    }
}
