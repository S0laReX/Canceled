using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace CancelBrawl.Beta
{
    public enum BrawlAction { MoveUp, MoveDown, MoveLeft, MoveRight, Jump, Basic, Heavy, Special, Recovery, Dodge, Super, FastFall }
    public enum BindingScheme { Keyboard, Gamepad }

    /// <summary>
    /// Single source of gameplay bindings (SYS-024/025): the same table is read by gameplay, drawn by the HUD,
    /// edited by remapping and persisted by <see cref="BrawlOptionsStore"/>. Pause and menu confirmation stay
    /// reserved (Esc/Start) so a remap can never lock a player out of the pause menu.
    /// </summary>
    public sealed class BrawlBindings
    {
        public const string KeyboardPrefix = "Keyboard/";
        public const string MousePrefix = "Mouse/";
        public static readonly BrawlAction[] KeyboardActions = (BrawlAction[])Enum.GetValues(typeof(BrawlAction));
        /// <summary>Gamepad movement stays on stick + d-pad; aim on right stick.</summary>
        public static readonly BrawlAction[] GamepadActions =
            { BrawlAction.Jump, BrawlAction.Basic, BrawlAction.Heavy, BrawlAction.Special, BrawlAction.Recovery, BrawlAction.Dodge, BrawlAction.Super, BrawlAction.FastFall };
        public static readonly string[] GamepadControls =
            { "buttonSouth", "buttonEast", "buttonWest", "buttonNorth", "leftShoulder", "rightShoulder", "leftTrigger", "rightTrigger", "leftStickPress", "rightStickPress" };
        private static readonly string[] MouseControls = { "leftButton", "rightButton", "middleButton", "forwardButton", "backButton" };
        public static readonly string[] ReservedKeyboard = { KeyboardPrefix + "escape" };
        public static readonly string[] ReservedGamepad = { "start", "select" };

        private static readonly Dictionary<BrawlAction, string> DefaultKeyboard = new Dictionary<BrawlAction, string>
        {
            { BrawlAction.MoveUp, KeyboardPrefix + "w" }, { BrawlAction.MoveDown, KeyboardPrefix + "s" },
            { BrawlAction.MoveLeft, KeyboardPrefix + "a" }, { BrawlAction.MoveRight, KeyboardPrefix + "d" },
            { BrawlAction.Jump, KeyboardPrefix + "space" }, { BrawlAction.Basic, MousePrefix + "leftButton" },
            { BrawlAction.Heavy, MousePrefix + "rightButton" }, { BrawlAction.Special, KeyboardPrefix + "e" },
            { BrawlAction.Recovery, KeyboardPrefix + "r" }, { BrawlAction.Dodge, KeyboardPrefix + "leftShift" },
            { BrawlAction.Super, KeyboardPrefix + "q" }, { BrawlAction.FastFall, KeyboardPrefix + "leftCtrl" }
        };
        private static readonly Dictionary<BrawlAction, string> DefaultGamepad = new Dictionary<BrawlAction, string>
        {
            { BrawlAction.Jump, "buttonSouth" }, { BrawlAction.Basic, "buttonWest" }, { BrawlAction.Heavy, "buttonNorth" },
            { BrawlAction.Special, "buttonEast" }, { BrawlAction.Recovery, "rightShoulder" }, { BrawlAction.Dodge, "rightTrigger" },
            { BrawlAction.Super, "leftShoulder" }, { BrawlAction.FastFall, "leftTrigger" }
        };

        private readonly Dictionary<BrawlAction, string> keyboard = new Dictionary<BrawlAction, string>(DefaultKeyboard);
        private readonly Dictionary<BrawlAction, string> gamepad = new Dictionary<BrawlAction, string>(DefaultGamepad);
        /// <summary>Raised after any change so the owner can persist it.</summary>
        public event Action Changed;

        public static BrawlAction[] Actions(BindingScheme scheme) => scheme == BindingScheme.Keyboard ? KeyboardActions : GamepadActions;
        private Dictionary<BrawlAction, string> Table(BindingScheme scheme) => scheme == BindingScheme.Keyboard ? keyboard : gamepad;
        public string Get(BindingScheme scheme, BrawlAction action) => Table(scheme).TryGetValue(action, out string path) ? path : "";
        public static string Default(BindingScheme scheme, BrawlAction action) =>
            (scheme == BindingScheme.Keyboard ? DefaultKeyboard : DefaultGamepad).TryGetValue(action, out string path) ? path : "";

        public static bool IsReserved(BindingScheme scheme, string path) =>
            Array.IndexOf(scheme == BindingScheme.Keyboard ? ReservedKeyboard : ReservedGamepad, path) >= 0;

        /// <summary>Only controls that exist on the scheme's device layout and are not reserved.</summary>
        public static bool IsAssignable(BindingScheme scheme, string path)
        {
            if (string.IsNullOrEmpty(path) || IsReserved(scheme, path)) return false;
            if (scheme == BindingScheme.Gamepad) return Array.IndexOf(GamepadControls, path) >= 0;
            if (path.StartsWith(MousePrefix, StringComparison.Ordinal)) return Array.IndexOf(MouseControls, path.Substring(MousePrefix.Length)) >= 0;
            if (!path.StartsWith(KeyboardPrefix, StringComparison.Ordinal)) return false;
            return KeyName(path.Substring(KeyboardPrefix.Length)) != Key.None;
        }

        private static readonly Dictionary<string, Key> keyNames = BuildKeyNames();
        private static Dictionary<string, Key> BuildKeyNames()
        {
            // Names match the Keyboard layout's control names (e.g. "space", "leftShift", "digit1").
            var names = new Dictionary<string, Key>(StringComparer.OrdinalIgnoreCase);
            foreach (Key key in Enum.GetValues(typeof(Key)))
            {
                string name = key.ToString();
                // IMESelected is a dummy entry without a key control.
                if (key == Key.None || name == "IMESelected") continue;
                name = char.ToLowerInvariant(name[0]) + name.Substring(1);
                if (!names.ContainsKey(name)) names[name] = key;
            }
            return names;
        }
        private static Key KeyName(string name) => keyNames.TryGetValue(name, out Key key) ? key : Key.None;
        public static string KeyPath(Key key) { string n = key.ToString(); return KeyboardPrefix + char.ToLowerInvariant(n[0]) + n.Substring(1); }

        /// <summary>
        /// Assign a control. A control already used by another action of the same scheme is a conflict: the other
        /// action receives this action's previous control, so the map never holds duplicates or unbound actions.
        /// </summary>
        public BindResult Rebind(BindingScheme scheme, BrawlAction action, string path)
        {
            if (Array.IndexOf(Actions(scheme), action) < 0) return new BindResult(BindStatus.NotRemappable, action, null);
            if (IsReserved(scheme, path)) return new BindResult(BindStatus.Reserved, action, null);
            if (!IsAssignable(scheme, path)) return new BindResult(BindStatus.Invalid, action, null);
            var table = Table(scheme);
            string previous = Get(scheme, action);
            if (previous == path) return new BindResult(BindStatus.Unchanged, action, null);
            BrawlAction? conflict = null;
            foreach (var pair in table) if (pair.Key != action && pair.Value == path) { conflict = pair.Key; break; }
            table[action] = path;
            if (conflict.HasValue) table[conflict.Value] = previous;
            Changed?.Invoke();
            return new BindResult(conflict.HasValue ? BindStatus.Swapped : BindStatus.Assigned, action, conflict);
        }

        /// <summary>Which action already uses this control (conflict preview), or null.</summary>
        public BrawlAction? UsedBy(BindingScheme scheme, string path)
        {
            foreach (var pair in Table(scheme)) if (pair.Value == path) return pair.Key;
            return null;
        }

        public void ResetDefaults(BindingScheme scheme) { ApplyDefaults(scheme); Changed?.Invoke(); }

        private void ApplyDefaults(BindingScheme scheme)
        {
            var table = Table(scheme); table.Clear();
            foreach (var pair in scheme == BindingScheme.Keyboard ? DefaultKeyboard : DefaultGamepad) table[pair.Key] = pair.Value;
        }

        /// <summary>Replace a whole scheme from stored data. Invalid, reserved or duplicated input keeps the safe defaults.</summary>
        public bool TryLoad(BindingScheme scheme, IList<BrawlBindingEntry> entries)
        {
            var candidate = new Dictionary<BrawlAction, string>(scheme == BindingScheme.Keyboard ? DefaultKeyboard : DefaultGamepad);
            var used = new HashSet<string>();
            bool clean = true;
            if (entries != null)
                foreach (var entry in entries)
                {
                    if (entry == null || !Enum.TryParse(entry.action, false, out BrawlAction action) || Array.IndexOf(Actions(scheme), action) < 0 ||
                        !IsAssignable(scheme, entry.path)) { clean = false; continue; }
                    candidate[action] = entry.path;
                }
            foreach (var pair in candidate) if (!used.Add(pair.Value)) clean = false;
            if (!clean && used.Count != candidate.Count)
            {
                // Duplicates cannot be resolved without guessing intent: restore this scheme's defaults.
                ApplyDefaults(scheme);
                return false;
            }
            var table = Table(scheme); table.Clear();
            foreach (var pair in candidate) table[pair.Key] = pair.Value;
            return clean && entries != null;
        }

        public List<BrawlBindingEntry> Export(BindingScheme scheme)
        {
            var list = new List<BrawlBindingEntry>();
            foreach (BrawlAction action in Actions(scheme)) list.Add(new BrawlBindingEntry { action = action.ToString(), path = Get(scheme, action) });
            return list;
        }

        // ───────────── Reading ─────────────
        public static ButtonControl Resolve(string path, Keyboard keys, Mouse mouse)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (path.StartsWith(MousePrefix, StringComparison.Ordinal))
                return mouse?.TryGetChildControl<ButtonControl>(path.Substring(MousePrefix.Length));
            if (path.StartsWith(KeyboardPrefix, StringComparison.Ordinal))
            {
                Key key = KeyName(path.Substring(KeyboardPrefix.Length));
                return keys != null && key != Key.None ? keys[key] : null;
            }
            return null;
        }

        public static ButtonControl Resolve(string path, Gamepad pad) =>
            pad == null || string.IsNullOrEmpty(path) ? null : pad.TryGetChildControl<ButtonControl>(path);

        public bool Held(BrawlAction action, Keyboard keys, Mouse mouse, bool blockMouse = false)
        {
            string path = Get(BindingScheme.Keyboard, action);
            if (blockMouse && path.StartsWith(MousePrefix, StringComparison.Ordinal)) return false;
            var control = Resolve(path, keys, mouse); return control != null && control.isPressed;
        }
        public bool Pressed(BrawlAction action, Keyboard keys, Mouse mouse, bool blockMouse = false)
        {
            string path = Get(BindingScheme.Keyboard, action);
            if (blockMouse && path.StartsWith(MousePrefix, StringComparison.Ordinal)) return false;
            var control = Resolve(path, keys, mouse); return control != null && control.wasPressedThisFrame;
        }
        public bool Held(BrawlAction action, Gamepad pad) { var c = Resolve(Get(BindingScheme.Gamepad, action), pad); return c != null && c.isPressed; }
        public bool Pressed(BrawlAction action, Gamepad pad) { var c = Resolve(Get(BindingScheme.Gamepad, action), pad); return c != null && c.wasPressedThisFrame; }

        /// <summary>First assignable control pressed this frame on any keyboard/mouse or the given pads (remap capture).</summary>
        public static string CapturePressed(BindingScheme scheme, IEnumerable<Gamepad> pads)
        {
            if (scheme == BindingScheme.Keyboard)
            {
                foreach (var device in InputSystem.devices)
                {
                    if (!device.enabled) continue;
                    if (device is Keyboard keys)
                    {
                        foreach (KeyControl key in keys.allKeys)
                            if (key != null && key.wasPressedThisFrame && IsAssignable(scheme, KeyPath(key.keyCode))) return KeyPath(key.keyCode);
                        if (keys.escapeKey.wasPressedThisFrame) return ReservedKeyboard[0];
                    }
                    else if (device is Mouse mouse)
                        foreach (string name in MouseControls)
                        {
                            var button = mouse.TryGetChildControl<ButtonControl>(name);
                            if (button != null && button.wasPressedThisFrame) return MousePrefix + name;
                        }
                }
                return null;
            }
            foreach (var pad in pads)
            {
                if (!BrawlDeviceSlots.Available(pad)) continue;
                foreach (string name in GamepadControls) if (Resolve(name, pad).wasPressedThisFrame) return name;
                foreach (string name in ReservedGamepad) { var c = Resolve(name, pad); if (c != null && c.wasPressedThisFrame) return name; }
            }
            return null;
        }

        // ───────────── Labels (real device names) ─────────────
        /// <summary>Action name; in side view the vertical keys choose attack direction instead of moving in depth.</summary>
        public static string ActionName(BrawlAction action, bool sideView)
        {
            if (sideView && action == BrawlAction.MoveUp) return "Arriba: dirección de golpe";
            if (sideView && action == BrawlAction.MoveDown) return "Abajo: golpe, caer o bajar";
            return ActionName(action);
        }

        public static string ActionName(BrawlAction action)
        {
            switch (action)
            {
                case BrawlAction.MoveUp: return "Mover adelante";
                case BrawlAction.MoveDown: return "Mover atrás";
                case BrawlAction.MoveLeft: return "Mover izquierda";
                case BrawlAction.MoveRight: return "Mover derecha";
                case BrawlAction.Jump: return "Salto / doble salto";
                case BrawlAction.Basic: return "Básico";
                case BrawlAction.Heavy: return "Fuerte";
                case BrawlAction.Special: return "Especial";
                case BrawlAction.Recovery: return "Recuperación";
                case BrawlAction.Dodge: return "Esquiva";
                case BrawlAction.Super: return "Super";
                default: return "Caída rápida";
            }
        }

        public string Label(BindingScheme scheme, BrawlAction action, Gamepad pad = null) =>
            ControlLabel(scheme, Get(scheme, action), scheme == BindingScheme.Keyboard ? Keyboard.current : null, pad);

        public static string ControlLabel(BindingScheme scheme, string path, Keyboard keys, Gamepad pad)
        {
            if (string.IsNullOrEmpty(path)) return "—";
            if (scheme == BindingScheme.Keyboard)
            {
                if (path.StartsWith(MousePrefix, StringComparison.Ordinal))
                {
                    switch (path.Substring(MousePrefix.Length))
                    {
                        case "leftButton": return "Clic izq.";
                        case "rightButton": return "Clic der.";
                        case "middleButton": return "Clic central";
                        case "forwardButton": return "Ratón adelante";
                        default: return "Ratón atrás";
                    }
                }
                Key key = KeyName(path.Substring(KeyboardPrefix.Length));
                switch (key)
                {
                    case Key.Space: return "Espacio";
                    case Key.LeftShift: return "Shift izq.";
                    case Key.RightShift: return "Shift der.";
                    case Key.LeftCtrl: return "Ctrl izq.";
                    case Key.RightCtrl: return "Ctrl der.";
                    case Key.LeftAlt: return "Alt izq.";
                    case Key.RightAlt: return "Alt der.";
                    case Key.Enter: return "Intro";
                    case Key.Tab: return "Tab";
                    case Key.Backspace: return "Retroceso";
                    case Key.Escape: return "Esc";
                    case Key.UpArrow: return "Flecha arriba";
                    case Key.DownArrow: return "Flecha abajo";
                    case Key.LeftArrow: return "Flecha izq.";
                    case Key.RightArrow: return "Flecha der.";
                }
                // Real keyboards report the character printed by the active OS layout (AZERTY, QWERTZ…).
                if (keys != null && key != Key.None)
                {
                    string shown = keys[key].displayName;
                    if (!string.IsNullOrWhiteSpace(shown)) return shown.Length == 1 ? shown.ToUpperInvariant() : shown;
                }
                return key == Key.None ? path : key.ToString();
            }
            var control = Resolve(path, pad);
            string real = control != null ? (!string.IsNullOrEmpty(control.shortDisplayName) ? control.shortDisplayName : control.displayName) : null;
            switch (real)
            {
                case "Cross": return "Cruz";
                case "Circle": return "Círculo";
                case "Square": return "Cuadrado";
                case "Triangle": return "Triángulo";
            }
            if (!string.IsNullOrEmpty(real) && !real.StartsWith("Button ", StringComparison.Ordinal) && real != "Left Stick Press" && real != "Right Stick Press") return real;
            switch (path)
            {
                case "buttonSouth": return "A"; case "buttonEast": return "B"; case "buttonWest": return "X"; case "buttonNorth": return "Y";
                case "leftShoulder": return "LB"; case "rightShoulder": return "RB"; case "leftTrigger": return "LT"; case "rightTrigger": return "RT";
                case "leftStickPress": return "L3"; case "rightStickPress": return "R3"; case "start": return "Start"; case "select": return "Select";
                default: return path;
            }
        }

        /// <summary>Family name shown beside gamepad labels (from the actual device, not a guess).</summary>
        public static string PadFamily(Gamepad pad)
        {
            if (pad == null) return "MANDO · nombres Xbox";
            string layout = pad.layout ?? "";
            if (layout.IndexOf("DualShock", StringComparison.OrdinalIgnoreCase) >= 0 || layout.IndexOf("DualSense", StringComparison.OrdinalIgnoreCase) >= 0) return "MANDO PLAYSTATION";
            if (layout.IndexOf("XInput", StringComparison.OrdinalIgnoreCase) >= 0 || layout.IndexOf("Xbox", StringComparison.OrdinalIgnoreCase) >= 0) return "MANDO XBOX";
            if (layout.IndexOf("Switch", StringComparison.OrdinalIgnoreCase) >= 0) return "MANDO NINTENDO";
            return "MANDO GENÉRICO";
        }
    }

    public enum BindStatus { Assigned, Swapped, Unchanged, Reserved, Invalid, NotRemappable }

    public readonly struct BindResult
    {
        public readonly BindStatus Status; public readonly BrawlAction Action; public readonly BrawlAction? Conflict;
        public BindResult(BindStatus status, BrawlAction action, BrawlAction? conflict) { Status = status; Action = action; Conflict = conflict; }
        public bool Accepted => Status == BindStatus.Assigned || Status == BindStatus.Swapped || Status == BindStatus.Unchanged;
    }

    [Serializable] public sealed class BrawlBindingEntry { public string action; public string path; }
}
