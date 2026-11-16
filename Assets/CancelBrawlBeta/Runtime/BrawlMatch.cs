using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CancelBrawl.Beta
{
    public sealed class BrawlMatch : MonoBehaviour
    {
        public const int MaxPlayers = 5;
        public const string Version = "0.3.0";
        [SerializeField] private BrawlSettings settings;
        [SerializeField] private bool productionPresentation;
        [SerializeField] private bool startInTraining;
        private bool legacyRegression;
        public bool IsProduction => productionPresentation && !legacyRegression;
        public bool IsSideView => IsProduction;
        public bool IsTraining { get; private set; }
        public bool ShowCombatVolumes { get; set; } = true;
        public float TrainingDamage { get; private set; }
        public string Title => IsProduction ? "FUNADOS" : "CANCEL BRAWL";
        public const string ProductionVersion = "0.5.0";
        public string DisplayVersion => IsProduction ? ProductionVersion : Version;
        public string CharacterName(int character) => !IsProduction ? BrawlRoster.Get(character).Name :
            character == 0 ? "Don Promesio" : character == 1 ? "Lola Viral" : BrawlRoster.Get(character).Name;
        public MatchPhase Phase { get; private set; } = MatchPhase.Menu;
        public float RemainingTime { get; private set; }
        public float CountdownRemaining { get; private set; }
        /// <summary>Simulation clock of the current match (fixed steps while playing).</summary>
        public float SimTime { get; private set; }
        public BrawlFighter[] Fighters { get; private set; } = Array.Empty<BrawlFighter>();
        public string Announcement => announcementTime > 0 ? announcement : "";
        /// <summary>Character of every slot P1–P5. P1 is <see cref="SelectedCharacter"/>.</summary>
        public int[] SlotCharacters { get; } = { 0, 1, 2, 3, 4 };
        public int SelectedCharacter
        {
            get => SlotCharacters[0];
            set
            {
                int character = ((value % BrawlRoster.Count) + BrawlRoster.Count) % BrawlRoster.Count;
                // Rivals rotate through the rest of the roster so a five-player match shows the whole cast.
                for (int i = 0; i < MaxPlayers; i++) SlotCharacters[i] = (character + i) % BrawlRoster.Count;
                for (int i = 0; i < MaxPlayers; i++) SlotSkins[i] = 0;
            }
        }
        /// <summary>Look chosen for every slot (index into the kit's Skins). Repeated fighters get distinct looks at match start.</summary>
        public int[] SlotSkins { get; } = { 0, 0, 0, 0, 0 };

        public void CycleCharacter(int slot)
        {
            SlotCharacters[slot] = (SlotCharacters[slot] + 1) % BrawlRoster.Count;
            SlotSkins[slot] = 0;
        }

        public void CycleSkin(int slot) => SlotSkins[slot] = BrawlRoster.WrapSkin(SlotCharacters[slot], SlotSkins[slot] + 1);

        /// <summary>Skin actually worn: a mirror match moves the later slot to the next free look so both stay readable.</summary>
        private int ResolveSkin(int slot, int count)
        {
            int character = SlotCharacters[slot], skins = BrawlRoster.SkinCount(character);
            int skin = BrawlRoster.WrapSkin(character, SlotSkins[slot]);
            for (int attempt = 0; attempt < skins; attempt++)
            {
                bool taken = false;
                for (int other = 0; other < slot && other < count; other++)
                    if (SlotCharacters[other] == character && BrawlRoster.WrapSkin(character, resolvedSkins[other]) == skin) taken = true;
                if (!taken) break;
                skin = (skin + 1) % skins;
            }
            return skin;
        }
        private readonly int[] resolvedSkins = new int[MaxPlayers];
        public int OpponentCount { get; set; } = 1;
        public int ArenaIndex { get; set; }
        public bool LocalCoop { get; set; }
        public bool P1UsesGamepad { get; set; }
        public bool CanStartMatch => !LocalCoop || !P1UsesGamepad || ConnectedPads >= 2;
        public bool CanStartTraining => !LocalCoop || !P1UsesGamepad || ConnectedPads >= 1;
        public BrawlDeviceSlots Devices { get; } = new BrawlDeviceSlots();
        public bool DevicePause { get; private set; }
        public int MissingDeviceMask => Devices.MissingMask;
        private MatchPhase resumePhase = MatchPhase.Playing;
        public string MissingDeviceNames
        {
            get
            {
                string names = "";
                for (int i = 0; i < MaxPlayers; i++)
                    if ((MissingDeviceMask & (1 << i)) != 0) names += (names.Length > 0 ? ", " : "") + "P" + (i + 1);
                return names;
            }
        }
        public BrawlArena Arena { get; private set; }
        public BrawlSettings Settings => settings;
        public bool AutomatedTest { get; set; }
        public int ConnectedPads
        {
            get { int count = 0; foreach (var pad in Gamepad.all) if (BrawlDeviceSlots.Available(pad)) count++; return count; }
        }
        /// <summary>Pads beyond P2–P5 are rejected: a match never exceeds five participants.</summary>
        public int RejectedPads => Mathf.Max(0, ConnectedPads - (LocalCoop ? Mathf.Clamp(OpponentCount + 1, 2, MaxPlayers) - (P1UsesGamepad ? 0 : 1) : 1));
        public BrawlCamera GroupCamera => groupCamera;
        private BrawlCamera groupCamera;
        private Camera view;
        private Transform participants;
        private string announcement;
        private float announcementTime;
        private readonly List<BrawlFighter> finalLosses = new List<BrawlFighter>();
        private readonly Color[] colors = { new Color(.18f,.95f,1), new Color(1,.3f,.6f), new Color(1,.77f,.2f), new Color(.5f,1,.42f), new Color(.66f,.5f,1) };

        private void Awake()
        {
            string[] args = Environment.GetCommandLineArgs();
            legacyRegression = Array.IndexOf(args, "-beta-smoke") >= 0 || Array.IndexOf(args, "-controls-smoke") >= 0;
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            Time.fixedDeltaTime = 1f / 60;
            Physics.IgnoreLayerCollision(8, 8, true);
            if (settings == null) settings = ScriptableObject.CreateInstance<BrawlSettings>();
            var cameraObject = new GameObject("Cámara de grupo"); cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(transform);
            view = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>(); groupCamera = cameraObject.AddComponent<BrawlCamera>();
            BuildArena(0);
            groupCamera.Configure(null);
            participants = new GameObject("Participantes").transform; participants.SetParent(transform);
            gameObject.AddComponent<BrawlHud>().Configure(this);
            BrawlFeedback.Initialize(transform);
            LoadOptions();
            RemainingTime = settings.matchSeconds;
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-beta-smoke") >= 0 || Array.IndexOf(Environment.GetCommandLineArgs(), "-controls-smoke") >= 0)
            { AutomatedTest = true; gameObject.AddComponent<BrawlSmokeProbe>(); }
            if (Array.IndexOf(args, "-side-smoke") >= 0)
            { AutomatedTest = true; gameObject.AddComponent<BrawlSideSmokeProbe>(); }
        }

        private void BuildArena(int index)
        {
            index = Mathf.Clamp(index, 0, BrawlArena.Count - 1);
            if (Arena != null && Arena.Layout == index) return;
            if (Arena != null) { Arena.gameObject.SetActive(false); Destroy(Arena.gameObject); }
            Arena = new GameObject("Cancha").AddComponent<BrawlArena>();
            Arena.transform.SetParent(transform); Arena.Build(index, StagePrefab(index), IsSideView);
            groupCamera.Setup(Arena, settings.occluderMaterial);
        }

        /// <summary>True when court <paramref name="index"/> is played on its production stage prefab.</summary>
        public bool UsesStageArt(int index) => StagePrefab(index) != null;

        private GameObject StagePrefab(int index) =>
            IsProduction && settings != null && settings.stagePrefabs != null && index >= 0 && index < settings.stagePrefabs.Length
                ? settings.stagePrefabs[index] : null;

        public void StartMatch()
        {
            if (!CanStartMatch) return;
            IsTraining = false;
            BeginRound();
        }

        private void Start()
        {
            if (startInTraining && !AutomatedTest) StartTraining();
        }

        public void StartTraining()
        {
            if (!CanStartTraining) return;
            IsTraining = true;
            BeginRound();
        }

        public void SetTrainingDamage(float damage)
        {
            if (!IsTraining || float.IsNaN(damage) || float.IsInfinity(damage)) return;
            TrainingDamage = Mathf.Clamp(damage, 0, 300);
            if (Fighters.Length > 1) Fighters[1].SetPracticeDamage(TrainingDamage);
        }

        public void ResetTraining()
        {
            if (IsTraining) BeginRound();
        }

        public void FillTrainingMeter()
        {
            if (IsTraining && Fighters.Length > 0) Fighters[0].SetMeterForTest(100);
        }

        private void BeginRound()
        {
            CleanupRound();
            ArenaIndex = Mathf.Clamp(ArenaIndex, 0, BrawlArena.Count - 1);
            BuildArena(ArenaIndex);
            int count = IsTraining ? 2 : Mathf.Clamp(OpponentCount + 1, 2, MaxPlayers);
            Fighters = new BrawlFighter[count];
            var targets = new Transform[count];
            Vector3[] spawns = Arena.SpawnPoints;
            Devices.Configure(count, LocalCoop, P1UsesGamepad, IsTraining);
            DevicePause = false;
            for (int i = 0; i < count; i++)
            {
                var fighterObject = new GameObject("P" + (i + 1)); fighterObject.transform.SetParent(participants);
                fighterObject.transform.position = spawns[i % spawns.Length];
                var fighter = fighterObject.AddComponent<BrawlFighter>();
                resolvedSkins[i] = ResolveSkin(i, count);
                fighter.Configure(this, settings, i, SlotCharacters[i], resolvedSkins[i], !IsTraining && !Devices.IsHuman(i), colors[i]);
                Fighters[i] = fighter; targets[i] = fighter.transform;
            }
            RemainingTime = settings.matchSeconds; CountdownRemaining = 3; SimTime = 0;
            Phase = MatchPhase.Countdown; announcementTime = 0;
            groupCamera.Configure(targets);
            if (IsTraining)
            {
                // Same arena and combat rules; only input, timer and stock exhaustion differ.
                Fighters[0].RespawnAt(Arena.SpawnPoints[0], false);
                Fighters[1].RespawnAt(Arena.SpawnPoints[0] + (IsSideView ? Vector3.right : Vector3.forward) * 1.6f, false);
                Fighters[1].SetPracticeDamage(TrainingDamage);
                foreach (var fighter in Fighters) fighter.gameObject.AddComponent<BrawlCombatVolumes>().Configure(this, fighter);
                Phase = MatchPhase.Playing;
            }
        }

        private void Update()
        {
            PollDevices();
            if (AutomatedTest) return;
            var keys = Keyboard.current;
            bool confirm = keys != null && keys.enterKey.wasPressedThisFrame;
            bool pause = keys != null && keys.escapeKey.wasPressedThisFrame;
            foreach (var pad in Gamepad.all)
            {
                if (Phase == MatchPhase.Menu || Devices.Owns(pad))
                { pause |= pad.startButton.wasPressedThisFrame; confirm |= pad.buttonSouth.wasPressedThisFrame; }
                else if (DevicePause && pad.startButton.wasPressedThisFrame) Devices.ClaimMissing(pad);
            }
            if (Phase == MatchPhase.Menu || Phase == MatchPhase.Results) { if (confirm) StartMatch(); return; }
            if (pause && (Phase == MatchPhase.Playing || Phase == MatchPhase.Paused || Phase == MatchPhase.Countdown)) { TogglePause(); return; }
            if (Phase != MatchPhase.Playing) return;
            for (int i = 0; i < Fighters.Length; i++)
            {
                if ((IsTraining && i > 0) || Fighters[i].IsBot || Fighters[i].Eliminated || Fighters[i].IsRespawning) continue;
                if (i == 0)
                {
                    var input = Devices.KeyboardP1 ? ReadKeyboard(Fighters[i]) : default;
                    var pad = Devices.Pad(0);
                    if (BrawlDeviceSlots.Available(pad)) input = Combine(input, ReadGamepad(pad, IsSideView), IsSideView);
                    Fighters[i].Submit(input);
                }
                else if (BrawlDeviceSlots.Available(Devices.Pad(i))) Fighters[i].Submit(ReadGamepad(Devices.Pad(i), IsSideView));
            }
        }

        private BrawlCommand ReadKeyboard(BrawlFighter fighter)
        {
            var input = new BrawlCommand(); var keys = Keyboard.current;
            if (keys != null)
            {
                input.Move = Vector2.ClampMagnitude(new Vector2((keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0), (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0)), 1);
                if (IsSideView) input.Move = new Vector2((keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0), (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0));
                input.Jump = keys.spaceKey.wasPressedThisFrame; input.Special = keys.eKey.wasPressedThisFrame;
                input.Recovery = keys.rKey.wasPressedThisFrame; input.Super = keys.qKey.wasPressedThisFrame;
                input.Dodge = keys.leftShiftKey.wasPressedThisFrame || keys.rightShiftKey.wasPressedThisFrame;
                input.FastFall = keys.leftCtrlKey.isPressed || keys.rightCtrlKey.isPressed;
            }
            var mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 pointer = mouse.position.ReadValue();
                float scale = Mathf.Min(Screen.width / 1600f, Screen.height / 900f);
                Vector2 uiPoint = (new Vector2(pointer.x, Screen.height - pointer.y) -
                    new Vector2((Screen.width - 1600 * scale) * .5f, (Screen.height - 900 * scale) * .5f)) / Mathf.Max(.001f, scale);
                bool overLab = IsTraining && new Rect(1000, 106, 570, 178).Contains(uiPoint);
                input.Basic = !overLab && mouse.leftButton.wasPressedThisFrame; input.Heavy = !overLab && mouse.rightButton.wasPressedThisFrame;
                Ray ray = view.ScreenPointToRay(mouse.position.ReadValue());
                var plane = IsSideView ? new Plane(Vector3.forward, Vector3.zero) : new Plane(Vector3.up, fighter.transform.position + Vector3.up * .75f);
                if (plane.Raycast(ray, out float enter)) input.Aim = IsSideView ? new Vector2(Mathf.Sign(ray.GetPoint(enter).x - fighter.transform.position.x), 0) : WorldToCamera(Vector3.ProjectOnPlane(ray.GetPoint(enter) - fighter.transform.position, Vector3.up).normalized);
            }
            return input;
        }

        internal static BrawlCommand ReadGamepad(Gamepad pad, bool sideView = false)
        {
            Vector2 movement;
            if (sideView)
            {
                // DpadControl.ReadValue normalizes diagonals; direction selection must not slow X.
                Vector2 digital = new Vector2((pad.dpad.right.isPressed ? 1 : 0) - (pad.dpad.left.isPressed ? 1 : 0),
                    (pad.dpad.up.isPressed ? 1 : 0) - (pad.dpad.down.isPressed ? 1 : 0));
                Vector2 combined = pad.leftStick.ReadValue() + digital;
                movement = new Vector2(Mathf.Clamp(combined.x,-1,1), Mathf.Clamp(combined.y,-1,1));
            }
            else movement = Vector2.ClampMagnitude(pad.leftStick.ReadValue() + pad.dpad.ReadValue(), 1);
            return new BrawlCommand
            {
                Move = movement, Aim = pad.rightStick.ReadValue(),
                Jump = pad.buttonSouth.wasPressedThisFrame, Basic = pad.buttonWest.wasPressedThisFrame,
                Heavy = pad.buttonNorth.wasPressedThisFrame, Special = pad.buttonEast.wasPressedThisFrame,
                Recovery = pad.rightShoulder.wasPressedThisFrame, Dodge = pad.rightTrigger.wasPressedThisFrame,
                Super = pad.leftShoulder.wasPressedThisFrame, FastFall = pad.leftTrigger.isPressed
            };
        }

        internal static BrawlCommand Combine(BrawlCommand keyboard, BrawlCommand pad, bool sideView = false)
        {
            Vector2 movement = keyboard.Move + pad.Move;
            keyboard.Move = sideView ? new Vector2(Mathf.Clamp(movement.x,-1,1), Mathf.Clamp(movement.y,-1,1)) : Vector2.ClampMagnitude(movement,1);
            if (pad.Aim.sqrMagnitude > .08f) keyboard.Aim = pad.Aim;
            keyboard.Jump |= pad.Jump; keyboard.Basic |= pad.Basic; keyboard.Heavy |= pad.Heavy;
            keyboard.Special |= pad.Special; keyboard.Recovery |= pad.Recovery; keyboard.Dodge |= pad.Dodge;
            keyboard.Super |= pad.Super; keyboard.FastFall |= pad.FastFall;
            return keyboard;
        }

        /// <summary>Safest spawn: far from active fighters and from persistent attacks (zones, emitters).</summary>
        public Vector3 SafeSpawnFor(BrawlFighter returning)
        {
            Vector3[] points = Arena.SpawnPoints;
            Vector3 best = points[returning.PlayerIndex % points.Length];
            float bestScore = float.NegativeInfinity;
            foreach (Vector3 point in points)
            {
                float nearest = 1000;
                foreach (var other in Fighters)
                {
                    if (other == null || other == returning || other.Eliminated || other.IsRespawning) continue;
                    nearest = Mathf.Min(nearest, (other.transform.position - point).sqrMagnitude);
                }
                foreach (var hazard in BrawlHazard.Active)
                {
                    if (!hazard) continue;
                    float clearance = Mathf.Max(0, (hazard.transform.position - point).magnitude - hazard.Radius);
                    nearest = Mathf.Min(nearest, clearance * clearance * .25f);
                }
                if (nearest > bestScore) { bestScore = nearest; best = point; }
            }
            return best;
        }

        private void FixedUpdate()
        {
            PollDevices();
            if (Phase == MatchPhase.Countdown)
            {
                CountdownRemaining -= Time.fixedDeltaTime;
                if (CountdownRemaining <= 0) { Phase = MatchPhase.Playing; Announce(Arena.StartCall, 1.5f); }
                return;
            }
            if (Phase != MatchPhase.Playing) return;
            float dt = Time.fixedDeltaTime;
            SimTime += dt;
            if (!IsTraining) RemainingTime = Mathf.Max(0, RemainingTime - dt);
            announcementTime -= dt;
            foreach (var fighter in Fighters) fighter.Tick(dt);
            ResolveRingOuts();
            if (!IsTraining && Phase == MatchPhase.Playing && RemainingTime <= 0) FinishByTime();
        }

        internal void ResolveRingOuts()
        {
            finalLosses.Clear();
            foreach (var fighter in Fighters)
            {
                if (!fighter.OutsideArena) continue;
                if (IsTraining)
                {
                    // A lab fall never exhausts stocks or ends the session.
                    fighter.ResetPractice(Arena.SpawnPoints[0] + (fighter.PlayerIndex == 1 ? (IsSideView ? Vector3.right : Vector3.forward) * 1.6f : Vector3.zero),
                        fighter.PlayerIndex == 1 ? TrainingDamage : 0);
                    continue;
                }
                BrawlFighter credited = fighter.LoseStock();
                string who = "P" + (fighter.PlayerIndex + 1);
                string by = credited != null ? "P" + (credited.PlayerIndex + 1) + " EXPULSA A " + who : who + " CAE SOLO";
                Announce(by + (fighter.Eliminated ? "  ·  FUERA DE LA PARTIDA" : "  ·  PIERDE UNA VIDA"), 2);
                Shake(.45f);
                if (fighter.Eliminated) finalLosses.Add(fighter);
            }
            int survivors = 0; BrawlFighter last = null;
            foreach (var fighter in Fighters) if (!fighter.Eliminated) { survivors++; last = fighter; }
            foreach (var fighter in finalLosses) fighter.Place = survivors + 1;
            if (survivors <= 1)
            {
                if (last != null) last.Place = 1;
                Phase = MatchPhase.Results; ClearInputs();
            }
        }

        internal void FinishByTime()
        {
            var alive = new List<BrawlFighter>();
            foreach (var fighter in Fighters) if (!fighter.Eliminated) alive.Add(fighter);
            alive.Sort((a, b) => a.Stocks != b.Stocks ? b.Stocks.CompareTo(a.Stocks) : a.Damage.CompareTo(b.Damage));
            for (int i = 0; i < alive.Count; i++)
            {
                alive[i].Place = i > 0 && alive[i].Stocks == alive[i - 1].Stocks && Mathf.Abs(alive[i].Damage - alive[i - 1].Damage) < .0001f ? alive[i - 1].Place : i + 1;
            }
            Phase = MatchPhase.Results; ClearInputs();
        }

        public void TogglePause()
        {
            if (Phase == MatchPhase.Playing || Phase == MatchPhase.Countdown)
            { resumePhase = Phase; Phase = MatchPhase.Paused; ClearInputs(); }
            else if (Phase == MatchPhase.Paused && MissingDeviceMask == 0)
            { Phase = resumePhase; DevicePause = false; ClearInputs(); }
        }

        public void PollDevices()
        {
            if (Phase != MatchPhase.Playing && Phase != MatchPhase.Countdown && Phase != MatchPhase.Paused) return;
            if (MissingDeviceMask == 0) return;
            DevicePause = true;
            if (Phase != MatchPhase.Paused) { resumePhase = Phase; Phase = MatchPhase.Paused; }
            ClearInputs();
        }

        public void ReassignMissingDevices()
        {
            if (Phase != MatchPhase.Paused || !DevicePause) return;
            foreach (var pad in Gamepad.all) Devices.ClaimMissing(pad);
            ClearInputs();
        }

        public void ReturnToMenu()
        {
            IsTraining = false;
            Phase = MatchPhase.Menu; CleanupRound(); RemainingTime = settings.matchSeconds;
            groupCamera.Configure(null);
        }

        private void CleanupRound()
        {
            Devices.Clear(); DevicePause = false;
            foreach (var fighter in Fighters) if (fighter != null) { fighter.gameObject.SetActive(false); Destroy(fighter.gameObject); }
            Fighters = Array.Empty<BrawlFighter>();
            foreach (var projectile in FindObjectsByType<BrawlProjectile>()) { projectile.gameObject.SetActive(false); Destroy(projectile.gameObject); }
            foreach (var hazard in FindObjectsByType<BrawlHazard>()) { hazard.gameObject.SetActive(false); Destroy(hazard.gameObject); }
            foreach (var effect in FindObjectsByType<BrawlTransient>()) { effect.gameObject.SetActive(false); Destroy(effect.gameObject); }
        }

        public void Shake(float amount) { if (groupCamera) groupCamera.Shake(amount); }

        // ───────────── Options (per device; GDD §12 accessibility) ─────────────
        public void SetReducedEffects(bool value) { BrawlFeedback.ReducedEffects = value; SaveOptions(); }
        public void SetVolumes(float sfx, float music) { BrawlFeedback.SetVolumes(sfx, music); SaveOptions(); }

        private void LoadOptions()
        {
            try
            {
                BrawlFeedback.ReducedEffects = PlayerPrefs.GetInt("cb.reducedEffects", 0) == 1;
                BrawlFeedback.SetVolumes(PlayerPrefs.GetFloat("cb.sfx", .8f), PlayerPrefs.GetFloat("cb.music", .5f));
            }
            catch (Exception) { /* Options are conveniences: defaults are fine. */ }
        }

        private void SaveOptions()
        {
            if (AutomatedTest) return;
            try
            {
                PlayerPrefs.SetInt("cb.reducedEffects", BrawlFeedback.ReducedEffects ? 1 : 0);
                PlayerPrefs.SetFloat("cb.sfx", BrawlFeedback.SfxVolume);
                PlayerPrefs.SetFloat("cb.music", BrawlFeedback.MusicVolume);
                PlayerPrefs.Save();
            }
            catch (Exception) { }
        }

        private void ClearInputs() { foreach (var fighter in Fighters) fighter.ClearInput(); }
        public void Announce(string message, float seconds) { announcement = message; announcementTime = seconds; }
        public Vector3 CameraDirection(Vector2 input)
        {
            if (IsSideView) return Vector3.right * Mathf.Clamp(input.x, -1, 1);
            Vector3 forward = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized;
            return Vector3.ClampMagnitude(view.transform.right * input.x + forward * input.y, 1);
        }
        public Vector2 WorldToCamera(Vector3 direction)
        {
            if (IsSideView) return new Vector2(direction.x, direction.y);
            Vector3 forward = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized;
            return new Vector2(Vector3.Dot(view.transform.right, direction), Vector3.Dot(forward, direction));
        }
    }
}
