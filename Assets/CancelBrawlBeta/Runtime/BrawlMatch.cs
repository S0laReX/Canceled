using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CancelBrawl.Beta
{
    /// <summary>Runs before projectiles and zones so a whole physics step shares one hit window (SYS-045).</summary>
    [DefaultExecutionOrder(-100)]
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
        public const string ProductionVersion = "0.6.3";
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
        public bool LocalCoop { get => localCoop; set { if (localCoop == value) return; localCoop = value; SaveOptions(); } }
        public bool P1UsesGamepad { get => p1UsesGamepad; set { if (p1UsesGamepad == value) return; p1UsesGamepad = value; SaveOptions(); } }
        private bool localCoop, p1UsesGamepad;
        /// <summary>Single binding table: gameplay reads it, the HUD labels it, remapping edits it, options persist it.</summary>
        public BrawlBindings Bindings { get; } = new BrawlBindings();
        public string OptionsPath { get; private set; }
        public OptionsLoadStatus OptionsStatus { get; private set; }
        public string OptionsStatusText
        {
            get
            {
                switch (OptionsStatus)
                {
                    case OptionsLoadStatus.RecoveredBackup: return "Opciones recuperadas de la copia de seguridad.";
                    case OptionsLoadStatus.RepairedFields: return "Opciones no válidas corregidas con valores seguros.";
                    case OptionsLoadStatus.ResetCorrupt: return "Archivo de opciones dañado: valores seguros restaurados.";
                    case OptionsLoadStatus.MigratedLegacy: return "Opciones anteriores importadas al nuevo formato.";
                    default: return "";
                }
            }
        }
        // ───────────── Remap capture (SYS-025) ─────────────
        public bool RemapListening { get; private set; }
        public BindingScheme RemapScheme { get; private set; }
        public BrawlAction RemapAction { get; private set; }
        public string RemapMessage { get; private set; } = "";
        public bool RemapMessageIsWarning { get; private set; }
        private int remapArmedFrame;
        private float remapStarted;
        public bool CanRemap => Phase == MatchPhase.Menu || Phase == MatchPhase.Paused;
        /// <summary>Gamepad whose real button names the controls panel shows: P1's pad, else the first connected.</summary>
        public Gamepad LabelPad
        {
            get
            {
                var own = Devices.Pad(0);
                if (BrawlDeviceSlots.Available(own)) return own;
                foreach (var pad in Gamepad.all) if (BrawlDeviceSlots.Available(pad)) return pad;
                return null;
            }
        }
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
            settings.ApplyKitTuning(); // SYS-011: editable kit numbers, validated; invalid data keeps the built-in values
            var cameraObject = new GameObject("Cámara de grupo"); cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(transform);
            view = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>(); groupCamera = cameraObject.AddComponent<BrawlCamera>();
            BuildArena(0);
            groupCamera.Configure(null);
            participants = new GameObject("Participantes").transform; participants.SetParent(transform);
            Hud = gameObject.AddComponent<BrawlHud>(); Hud.Configure(this);
            HazardVolumes = gameObject.AddComponent<BrawlHazardVolumes>(); HazardVolumes.Configure(this);
            gameObject.AddComponent<BrawlHitResolver>().Configure(this);
            BrawlFeedback.Initialize(transform);
            OptionsPath = ResolveOptionsPath();
            LoadOptions();
            Bindings.Changed += SaveOptions;
            RemainingTime = settings.matchSeconds;
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-beta-smoke") >= 0 || Array.IndexOf(Environment.GetCommandLineArgs(), "-controls-smoke") >= 0)
            { AutomatedTest = true; gameObject.AddComponent<BrawlSmokeProbe>(); }
            else if (Array.IndexOf(Environment.GetCommandLineArgs(), "-haptics-probe") >= 0)
            { AutomatedTest = true; gameObject.AddComponent<BrawlHapticsProbe>(); }
            if (Array.IndexOf(args, "-side-smoke") >= 0 || Array.IndexOf(args, "-power-smoke") >= 0 || Array.IndexOf(args, "-side-controls-smoke") >= 0 || Array.IndexOf(args, "-moves-smoke") >= 0)
            { AutomatedTest = true; gameObject.AddComponent<BrawlSideSmokeProbe>(); }
            // Support log: which pads the game sees and how, so field reports can be diagnosed from player.log.
            foreach (var device in InputSystem.devices) if (device is Gamepad) Debug.Log("FUNADOS_INPUT presente " + BrawlHapticsProbe.Describe(device));
            InputSystem.onDeviceChange += LogDeviceChange;
        }

        private static void LogDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device is Gamepad && change != InputDeviceChange.UsageChanged && change != InputDeviceChange.ConfigurationChanged)
                Debug.Log("FUNADOS_INPUT " + change + " " + BrawlHapticsProbe.Describe(device));
        }

        private void OnDestroy() => InputSystem.onDeviceChange -= LogDeviceChange;

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
            // Normal-mode reproduction of option saving (no automation flags): change one option, report, quit.
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-options-probe") >= 0)
            {
                SetVibration(VibrationStrength >= 1 ? .75f : VibrationStrength + .25f);
                Debug.Log("FUNADOS_OPTIONS probe automated=" + AutomatedTest + " existe=" + System.IO.File.Exists(OptionsPath) + " ruta=" + OptionsPath +
                          " persistent=" + Application.persistentDataPath);
                Application.Quit();
            }
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
            UpdateHaptics();
            if (RemapListening) { PollRemap(); return; }
            if (AutomatedTest) return;
            var keys = Keyboard.current;
            bool pause = keys != null && keys.escapeKey.wasPressedThisFrame;
            foreach (var pad in Gamepad.all)
            {
                if (Phase == MatchPhase.Menu || Devices.Owns(pad)) pause |= pad.startButton.wasPressedThisFrame;
                else if (DevicePause && pad.startButton.wasPressedThisFrame) Devices.ClaimMissing(pad);
            }
            // Enter/A in menus belong to the focused control (BrawlHud navigation, SYS-070); Start is a shortcut to play.
            if (Phase == MatchPhase.Menu || Phase == MatchPhase.Results) { if (pause && !(keys != null && keys.escapeKey.wasPressedThisFrame)) StartMatch(); return; }
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
            var mouse = Mouse.current;
            bool overLab = false;
            if (mouse != null)
            {
                // Clicks on the lab panel are UI, not attacks, whatever action the mouse button is bound to.
                overLab = IsTraining && Hud != null && Hud.PointerOverTraining(mouse.position.ReadValue());
            }
            input = ReadKeyboardButtons(keys, mouse, overLab);
            if (mouse != null)
            {
                Ray ray = view.ScreenPointToRay(mouse.position.ReadValue());
                var plane = IsSideView ? new Plane(Vector3.forward, Vector3.zero) : new Plane(Vector3.up, fighter.transform.position + Vector3.up * .75f);
                if (plane.Raycast(ray, out float enter)) input.Aim = IsSideView ? new Vector2(Mathf.Sign(ray.GetPoint(enter).x - fighter.transform.position.x), 0) : WorldToCamera(Vector3.ProjectOnPlane(ray.GetPoint(enter) - fighter.transform.position, Vector3.up).normalized);
            }
            return input;
        }

        /// <summary>Keyboard/mouse buttons and movement through the editable binding table.</summary>
        internal BrawlCommand ReadKeyboardButtons(Keyboard keys, Mouse mouse, bool blockMouse)
        {
            var b = Bindings;
            float x = (b.Held(BrawlAction.MoveRight, keys, mouse, blockMouse) ? 1 : 0) - (b.Held(BrawlAction.MoveLeft, keys, mouse, blockMouse) ? 1 : 0);
            float y = (b.Held(BrawlAction.MoveUp, keys, mouse, blockMouse) ? 1 : 0) - (b.Held(BrawlAction.MoveDown, keys, mouse, blockMouse) ? 1 : 0);
            return new BrawlCommand
            {
                // Side view: W/S choose the attack direction, so each axis is limited on its own and never slows X.
                Move = IsSideView ? new Vector2(x, y) : Vector2.ClampMagnitude(new Vector2(x, y), 1),
                Jump = b.Pressed(BrawlAction.Jump, keys, mouse, blockMouse), Basic = b.Pressed(BrawlAction.Basic, keys, mouse, blockMouse),
                Heavy = b.Pressed(BrawlAction.Heavy, keys, mouse, blockMouse), Special = b.Pressed(BrawlAction.Special, keys, mouse, blockMouse),
                Recovery = b.Pressed(BrawlAction.Recovery, keys, mouse, blockMouse), Dodge = b.Pressed(BrawlAction.Dodge, keys, mouse, blockMouse),
                Super = b.Pressed(BrawlAction.Super, keys, mouse, blockMouse), FastFall = b.Held(BrawlAction.FastFall, keys, mouse, blockMouse)
            };
        }

        // ───────────── Stick tuning and vibration (SYS-026, SYS-072) ─────────────
        public float MoveDeadzone { get; private set; } = .2f;
        public float AimDeadzone { get; private set; } = .25f;
        public float StickResponse { get; private set; } = 1f;
        public float VibrationStrength { get; private set; } = .75f;
        /// <summary>
        /// Lowest motor output used whenever vibration is on. Human test 0.4.5 (DualShock 4, USB): pulses of 25 % for
        /// 0.25 s were not felt at all, while 100 % was; below this floor a pulse is effectively invisible.
        /// </summary>
        public const float HapticFloor = .55f, HapticMinSeconds = .18f;

        /// <summary>Motor output for an event level (0–1) and the player's strength; zero only when either is zero.</summary>
        public static float HapticOutput(float level, float strength) =>
            strength <= 0 || level <= 0 ? 0 : Mathf.Clamp01(Mathf.Lerp(HapticFloor, 1f, Mathf.Clamp01(level)) * (.6f + .4f * Mathf.Clamp01(strength)));
        public BrawlHud Hud { get; private set; }
        public BrawlHazardVolumes HazardVolumes { get; private set; }
        /// <summary>Options panel page shown by the HUD (GENERAL / MANDO).</summary>
        /// <summary>Options panel page: 0 GENERAL, 1 MANDO, 2 PANTALLA.</summary>
        public int OptionsPage { get; set; }
        public bool ShowPadOptions { get => OptionsPage == 1; set => OptionsPage = value ? 1 : 0; }
        /// <summary>SYS-071: text size (1–1.3) for menus and HUD, and HUD element size (1–1.3) during play.</summary>
        public float TextScale { get; private set; } = 1f;
        public float HudScale { get; private set; } = 1f;

        public void SetDisplayScale(float text, float hud)
        {
            TextScale = Mathf.Clamp(Round(text), BrawlHudLayout.MinScaleOption, BrawlHudLayout.MaxScaleOption);
            HudScale = Mathf.Clamp(Round(hud), BrawlHudLayout.MinScaleOption, BrawlHudLayout.MaxScaleOption);
            SaveOptions();
        }

        public void SetStickTuning(float moveDeadzone, float aimDeadzone, float response)
        {
            MoveDeadzone = Mathf.Clamp(Round(moveDeadzone), BrawlInputTuning.MinDeadzone, BrawlInputTuning.MaxDeadzone);
            AimDeadzone = Mathf.Clamp(Round(aimDeadzone), BrawlInputTuning.MinDeadzone, BrawlInputTuning.MaxDeadzone);
            StickResponse = Mathf.Clamp(Round(response), BrawlInputTuning.MinResponse, BrawlInputTuning.MaxResponse);
            SaveOptions();
        }

        public void SetVibration(float strength)
        {
            VibrationStrength = Mathf.Clamp01(Round(strength));
            SaveOptions();
            // Immediate preview on the pad shown in the controls panel, so the change can be felt.
            var pad = LabelPad;
            if (pad != null && VibrationStrength > 0) StartHaptic(pad, .8f, .8f, .45f, true);
        }

        private static float Round(float value) => float.IsNaN(value) ? 0 : Mathf.Round(value * 100) / 100;

        private struct HapticPulse { public float Low, High, Until; public bool Preview; }
        private readonly Dictionary<Gamepad, HapticPulse> haptics = new Dictionary<Gamepad, HapticPulse>();
        private readonly List<Gamepad> hapticScratch = new List<Gamepad>();

        /// <summary>Rumble for the pad that owns this slot; bots and keyboard-only players get none.</summary>
        internal void Haptic(int playerIndex, float low, float high, float seconds)
        {
            if (Phase != MatchPhase.Playing || DevicePause || VibrationStrength <= 0) return;
            var pad = Devices.Pad(playerIndex);
            if (!BrawlDeviceSlots.Available(pad)) return;
            StartHaptic(pad, low, high, seconds, false);
        }

        private void StartHaptic(Gamepad pad, float low, float high, float seconds, bool preview)
        {
            low = HapticOutput(low, VibrationStrength); high = HapticOutput(high, VibrationStrength);
            seconds = Mathf.Max(seconds, HapticMinSeconds);
            if (haptics.TryGetValue(pad, out var current) && current.Until > Time.unscaledTime)
            { low = Mathf.Max(low, current.Low); high = Mathf.Max(high, current.High); }
            haptics[pad] = new HapticPulse { Low = low, High = high, Until = Time.unscaledTime + seconds, Preview = preview };
            try { pad.SetMotorSpeeds(low, high); } catch (Exception) { /* pads without motors */ }
        }

        /// <summary>Current motor level for a pad (0 when idle); used by the HUD-free automated checks.</summary>
        public float HapticLevel(Gamepad pad) => pad != null && haptics.TryGetValue(pad, out var pulse) ? Mathf.Max(pulse.Low, pulse.High) : 0;

        private void UpdateHaptics()
        {
            if (haptics.Count == 0) return;
            bool live = Phase == MatchPhase.Playing && !DevicePause;
            hapticScratch.Clear();
            foreach (var pair in haptics)
                if (pair.Value.Until <= Time.unscaledTime || (!live && !pair.Value.Preview) || !BrawlDeviceSlots.Available(pair.Key)) hapticScratch.Add(pair.Key);
            foreach (var pad in hapticScratch)
            {
                haptics.Remove(pad);
                try { if (pad.added) pad.ResetHaptics(); } catch (Exception) { }
            }
        }

        private void StopHaptics()
        {
            foreach (var pad in haptics.Keys) { try { if (pad.added) pad.ResetHaptics(); } catch (Exception) { } }
            haptics.Clear();
        }

        private void OnDisable() => StopHaptics();
        private void OnApplicationFocus(bool focused) { if (!focused) StopHaptics(); }

        internal BrawlCommand ReadGamepad(Gamepad pad, bool sideView = false)
        {
            var b = Bindings;
            // Digital d-pad stays an exact alternative; the analog stick goes through the player's tuning.
            Vector2 stick = BrawlInputTuning.Shape(pad.leftStick.ReadUnprocessedValue(), MoveDeadzone, StickResponse);
            Vector2 aim = BrawlInputTuning.Shape(pad.rightStick.ReadUnprocessedValue(), AimDeadzone, 1);
            Vector2 movement;
            if (sideView)
            {
                // DpadControl.ReadValue normalizes diagonals; direction selection must not slow X.
                Vector2 digital = new Vector2((pad.dpad.right.isPressed ? 1 : 0) - (pad.dpad.left.isPressed ? 1 : 0),
                    (pad.dpad.up.isPressed ? 1 : 0) - (pad.dpad.down.isPressed ? 1 : 0));
                Vector2 combined = stick + digital;
                movement = new Vector2(Mathf.Clamp(combined.x, -1, 1), Mathf.Clamp(combined.y, -1, 1));
            }
            else movement = Vector2.ClampMagnitude(stick + pad.dpad.ReadValue(), 1);
            return new BrawlCommand
            {
                Move = movement, Aim = aim,
                Jump = b.Pressed(BrawlAction.Jump, pad), Basic = b.Pressed(BrawlAction.Basic, pad),
                Heavy = b.Pressed(BrawlAction.Heavy, pad), Special = b.Pressed(BrawlAction.Special, pad),
                Recovery = b.Pressed(BrawlAction.Recovery, pad), Dodge = b.Pressed(BrawlAction.Dodge, pad),
                Super = b.Pressed(BrawlAction.Super, pad), FastFall = b.Held(BrawlAction.FastFall, pad)
            };
        }

        public void BeginRemap(BindingScheme scheme, BrawlAction action)
        {
            if (!CanRemap || Array.IndexOf(BrawlBindings.Actions(scheme), action) < 0) return;
            RemapListening = true; RemapScheme = scheme; RemapAction = action;
            remapArmedFrame = Time.frameCount; remapStarted = Time.realtimeSinceStartup;
            RemapMessage = $"{BrawlBindings.ActionName(action)}: pulsa {(scheme == BindingScheme.Keyboard ? "una tecla o botón del ratón" : "un botón del mando")}. Esc o Start cancelan.";
            RemapMessageIsWarning = false;
            ClearInputs();
        }

        public void CancelRemap(string message = "Reasignación cancelada.")
        {
            if (!RemapListening) return;
            RemapListening = false; RemapMessage = message; RemapMessageIsWarning = false;
        }

        /// <summary>Applies a captured control; also the path used by automated checks.</summary>
        public BindResult ApplyRemap(string path)
        {
            BindingScheme scheme = RemapScheme; BrawlAction action = RemapAction;
            RemapListening = false;
            var result = Bindings.Rebind(scheme, action, path);
            string control = BrawlBindings.ControlLabel(scheme, path, Keyboard.current, LabelPad);
            switch (result.Status)
            {
                case BindStatus.Swapped:
                    RemapMessage = $"CONFLICTO: {control} ya era {BrawlBindings.ActionName(result.Conflict.Value)}. Intercambiados: {BrawlBindings.ActionName(result.Conflict.Value)} pasa a {Bindings.Label(scheme, result.Conflict.Value, LabelPad)}.";
                    RemapMessageIsWarning = true; break;
                case BindStatus.Reserved:
                    RemapMessage = $"{control} está reservado para pausa/menú. No se cambió nada."; RemapMessageIsWarning = true; break;
                case BindStatus.Invalid:
                case BindStatus.NotRemappable:
                    RemapMessage = "Ese control no se puede asignar."; RemapMessageIsWarning = true; break;
                case BindStatus.Unchanged:
                    RemapMessage = $"{BrawlBindings.ActionName(action)} sigue en {control}."; RemapMessageIsWarning = false; break;
                default:
                    RemapMessage = $"{BrawlBindings.ActionName(action)} ahora usa {control}."; RemapMessageIsWarning = false; break;
            }
            return result;
        }

        private void PollRemap()
        {
            ClearInputs();
            if (Time.frameCount <= remapArmedFrame) return;
            if (!CanRemap) { CancelRemap(); return; }
            if (Time.realtimeSinceStartup - remapStarted > 10) { CancelRemap("Tiempo agotado: no se cambió nada."); return; }
            string path = BrawlBindings.CapturePressed(RemapScheme, Gamepad.all);
            if (path == null)
            {
                // The reserved cancel control of the other scheme also aborts (e.g. Start while capturing a key).
                bool cancel = false;
                foreach (var device in InputSystem.devices) if (device is Keyboard k && k.escapeKey.wasPressedThisFrame) cancel = true;
                foreach (var pad in Gamepad.all) cancel |= BrawlDeviceSlots.Available(pad) && pad.startButton.wasPressedThisFrame;
                if (cancel) CancelRemap();
                return;
            }
            if (BrawlBindings.IsReserved(RemapScheme, path)) { CancelRemap(); return; }
            ApplyRemap(path);
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
            OpenHitWindow();
            foreach (var fighter in Fighters) fighter.Tick(dt);
            ResolveRingOuts();
            if (!IsTraining && Phase == MatchPhase.Playing && RemainingTime <= 0) FinishByTime();
        }

        // ───────────── Simultaneous hits (SYS-045) ─────────────
        /// <summary>True from the start of a playing step until <see cref="BrawlHitResolver"/> flushes it.</summary>
        public bool CollectingHits { get; private set; }
        private readonly List<PendingHit> pendingHits = new List<PendingHit>();
        private readonly HashSet<BrawlFighter> hittableAtStepStart = new HashSet<BrawlFighter>();
        private readonly List<PendingHit> targetHits = new List<PendingHit>();
        public int LastStepHitCount { get; private set; }

        private void OpenHitWindow()
        {
            if (CollectingHits) FlushHits(); // never carry hits into the next step
            pendingHits.Clear(); hittableAtStepStart.Clear();
            // Vulnerability is judged on the state every fighter had before anyone moved this step.
            foreach (var fighter in Fighters) if (fighter != null && fighter.Hittable) hittableAtStepStart.Add(fighter);
            CollectingHits = true;
        }

        internal bool QueueHit(PendingHit hit)
        {
            if (!hittableAtStepStart.Contains(hit.Target)) return false;
            pendingHits.Add(hit);
            return true;
        }

        /// <summary>Applies all hits of the step at once, grouped by target, so player order cannot decide a trade.</summary>
        internal void FlushHits()
        {
            if (!CollectingHits) return;
            CollectingHits = false;
            LastStepHitCount = pendingHits.Count;
            if (Phase == MatchPhase.Playing)
                foreach (var fighter in Fighters)
                {
                    targetHits.Clear();
                    foreach (var hit in pendingHits) if (hit.Target == fighter) targetHits.Add(hit);
                    if (targetHits.Count > 0) fighter.ApplyHits(targetHits);
                }
            pendingHits.Clear(); hittableAtStepStart.Clear();
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
            CollectingHits = false; pendingHits.Clear(); hittableAtStepStart.Clear();
            StopHaptics();
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

        private static string ResolveOptionsPath()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-options-path");
            return index >= 0 && index + 1 < args.Length ? System.IO.Path.GetFullPath(args[index + 1]) : BrawlOptionsStore.DefaultPath;
        }

        private bool loadingOptions;
        /// <summary>SYS-068/069: versioned file; damaged or incomplete data falls back to safe values.</summary>
        internal void LoadOptions()
        {
            loadingOptions = true;
            try
            {
                BrawlOptionsData data;
                OptionsLoadStatus status;
                try { data = BrawlOptionsStore.Load(OptionsPath, out status, out _); }
                catch (Exception) { data = new BrawlOptionsData(); status = OptionsLoadStatus.ResetCorrupt; }
                if (status == OptionsLoadStatus.Defaults && HasLegacyOptions())
                {
                    // 0.4.1 and earlier kept three options in PlayerPrefs (implicit format version 0).
                    try
                    {
                        data.reducedEffects = PlayerPrefs.GetInt("cb.reducedEffects", 0) == 1;
                        data.sfx = Mathf.Clamp01(PlayerPrefs.GetFloat("cb.sfx", .8f));
                        data.music = Mathf.Clamp01(PlayerPrefs.GetFloat("cb.music", .5f));
                        status = OptionsLoadStatus.MigratedLegacy;
                    }
                    catch (Exception) { }
                }
                ApplyOptions(data, ref status);
                OptionsStatus = status;
                Debug.Log("FUNADOS_OPTIONS cargadas estado=" + status + " ruta=" + OptionsPath);
            }
            finally { loadingOptions = false; }
            // Rewrite a repaired/migrated/recovered file so the next start reads clean data.
            if (OptionsStatus != OptionsLoadStatus.Loaded && OptionsStatus != OptionsLoadStatus.Defaults) SaveOptions();
        }

        private void ApplyOptions(BrawlOptionsData data, ref OptionsLoadStatus status)
        {
            BrawlFeedback.ReducedEffects = data.reducedEffects;
            BrawlFeedback.SetVolumes(data.sfx, data.music);
            localCoop = data.localCoop; p1UsesGamepad = data.p1UsesGamepad;
            MoveDeadzone = data.moveDeadzone; AimDeadzone = data.aimDeadzone; StickResponse = data.stickResponse; VibrationStrength = data.vibration;
            TextScale = data.textScale; HudScale = data.hudScale;
            bool keyboardClean = Bindings.TryLoad(BindingScheme.Keyboard, data.keyboard);
            bool gamepadClean = Bindings.TryLoad(BindingScheme.Gamepad, data.gamepad);
            if ((status == OptionsLoadStatus.Loaded || status == OptionsLoadStatus.MigratedLegacy) && (!keyboardClean || !gamepadClean))
                status = OptionsLoadStatus.RepairedFields;
        }

        private static bool HasLegacyOptions()
        {
            try { return PlayerPrefs.HasKey("cb.sfx") || PlayerPrefs.HasKey("cb.music") || PlayerPrefs.HasKey("cb.reducedEffects"); }
            catch (Exception) { return false; }
        }

        internal BrawlOptionsData CaptureOptions() => new BrawlOptionsData
        {
            reducedEffects = BrawlFeedback.ReducedEffects, sfx = BrawlFeedback.SfxVolume, music = BrawlFeedback.MusicVolume,
            localCoop = localCoop, p1UsesGamepad = p1UsesGamepad,
            moveDeadzone = MoveDeadzone, aimDeadzone = AimDeadzone, stickResponse = StickResponse, vibration = VibrationStrength,
            textScale = TextScale, hudScale = HudScale,
            keyboard = Bindings.Export(BindingScheme.Keyboard), gamepad = Bindings.Export(BindingScheme.Gamepad)
        };

        private void SaveOptions()
        {
            // Automated runs never touch the player's own file; they point OptionsPath at their evidence folder.
            if (loadingOptions || OptionsPath == null || (AutomatedTest && OptionsPath == BrawlOptionsStore.DefaultPath))
            {
                if (!AutomatedTest) Debug.Log("FUNADOS_OPTIONS guardado omitido cargando=" + loadingOptions + " ruta=" + OptionsPath);
                return;
            }
            try { BrawlOptionsStore.Save(OptionsPath, CaptureOptions()); if (!AutomatedTest) Debug.Log("FUNADOS_OPTIONS guardadas ruta=" + OptionsPath); }
            catch (Exception error) { Debug.LogWarning("FUNADOS: no se pudieron guardar las opciones: " + error.Message); }
        }

        /// <summary>Test hook: point options at another file and reload it.</summary>
        internal void UseOptionsFile(string path) { OptionsPath = path; LoadOptions(); }

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
