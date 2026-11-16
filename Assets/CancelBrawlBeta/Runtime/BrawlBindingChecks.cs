using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// Explicit smoke-test path only (SYS-025, SYS-068, SYS-069, QA-445). Uses virtual devices and option files inside
    /// the evidence folder; the player's own option file is never written.
    /// </summary>
    public static class BrawlBindingChecks
    {
        public static IEnumerator Run(BrawlMatch match, Action<bool, string> check, string output)
        {
            if (Gamepad.all.Count != 0)
            {
                check(false, "Desconecta los mandos físicos para ejecutar la prueba de remapeo");
                yield break;
            }
            string folder = Path.Combine(output, "options-checks");
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            Directory.CreateDirectory(folder);
            string originalPath = match.OptionsPath;
            var created = new List<InputDevice>();
            int oldOpponents = match.OpponentCount;
            try
            {
                match.AutomatedTest = true; match.ReturnToMenu();
                string live = Path.Combine(folder, "live.json");
                match.UseOptionsFile(live);
                var b = match.Bindings;
                check((match.OptionsStatus == OptionsLoadStatus.Defaults || match.OptionsStatus == OptionsLoadStatus.MigratedLegacy) && b.Get(BindingScheme.Keyboard, BrawlAction.Jump) == "Keyboard/space",
                    "Sin archivo de opciones se usan controles por defecto");
                check(Unique(b, BindingScheme.Keyboard) && Unique(b, BindingScheme.Gamepad), "Controles por defecto sin conflictos");

                var keys = InputSystem.AddDevice<Keyboard>(); created.Add(keys);
                var mouse = InputSystem.AddDevice<Mouse>(); created.Add(mouse);
                var pad = InputSystem.AddDevice<Gamepad>(); created.Add(pad);

                // ── Direct remap and conflict detection ──
                var result = b.Rebind(BindingScheme.Keyboard, BrawlAction.Jump, "Keyboard/k");
                check(result.Status == BindStatus.Assigned && b.Get(BindingScheme.Keyboard, BrawlAction.Jump) == "Keyboard/k", "Remapeo de teclado asigna la nueva tecla");
                InputSystem.QueueStateEvent(keys, new KeyboardState(Key.K)); yield return null;
                var read = match.ReadKeyboardButtons(keys, mouse, false);
                InputSystem.QueueStateEvent(keys, new KeyboardState()); yield return null;
                InputSystem.QueueStateEvent(keys, new KeyboardState(Key.Space)); yield return null;
                var oldKey = match.ReadKeyboardButtons(keys, mouse, false);
                InputSystem.QueueStateEvent(keys, new KeyboardState()); yield return null;
                check(read.Jump && !oldKey.Jump, "La partida lee el control remapeado y deja de leer el anterior");

                result = b.Rebind(BindingScheme.Keyboard, BrawlAction.Special, "Keyboard/k");
                check(result.Status == BindStatus.Swapped && result.Conflict == BrawlAction.Jump &&
                    b.Get(BindingScheme.Keyboard, BrawlAction.Special) == "Keyboard/k" && b.Get(BindingScheme.Keyboard, BrawlAction.Jump) == "Keyboard/e" &&
                    Unique(b, BindingScheme.Keyboard), "Tecla en conflicto se detecta e intercambia sin duplicados");
                check(b.UsedBy(BindingScheme.Keyboard, "Keyboard/k") == BrawlAction.Special, "Vista previa de conflicto identifica la acción ocupante");
                result = b.Rebind(BindingScheme.Keyboard, BrawlAction.Jump, "Keyboard/escape");
                check(result.Status == BindStatus.Reserved && b.Get(BindingScheme.Keyboard, BrawlAction.Jump) == "Keyboard/e", "Esc reservado para pausa no se asigna");
                check(b.Rebind(BindingScheme.Keyboard, BrawlAction.Jump, "Keyboard/noExiste").Status == BindStatus.Invalid &&
                      b.Rebind(BindingScheme.Gamepad, BrawlAction.Jump, "dpad/up").Status == BindStatus.Invalid &&
                      b.Rebind(BindingScheme.Gamepad, BrawlAction.Jump, "start").Status == BindStatus.Reserved &&
                      b.Rebind(BindingScheme.Gamepad, BrawlAction.MoveUp, "buttonNorth").Status == BindStatus.NotRemappable,
                    "Controles inexistentes, reservados o no remapeables se rechazan");

                result = b.Rebind(BindingScheme.Gamepad, BrawlAction.Jump, "buttonNorth");
                check(result.Status == BindStatus.Swapped && result.Conflict == BrawlAction.Heavy && b.Get(BindingScheme.Gamepad, BrawlAction.Heavy) == "buttonSouth",
                    "Conflicto de mando intercambia salto y fuerte");
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.North)); yield return null;
                var padRead = match.ReadGamepad(pad);
                InputSystem.QueueStateEvent(pad, new GamepadState()); yield return null;
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.South)); yield return null;
                var padSouth = match.ReadGamepad(pad);
                InputSystem.QueueStateEvent(pad, new GamepadState()); yield return null;
                check(padRead.Jump && !padRead.Heavy && padSouth.Heavy && !padSouth.Jump, "Mando lee la asignación remapeada en ambos botones");

                b.Rebind(BindingScheme.Keyboard, BrawlAction.Dodge, "Mouse/middleButton");
                InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Middle)); yield return null;
                bool mouseOn = match.ReadKeyboardButtons(keys, mouse, false).Dodge, mouseBlocked = match.ReadKeyboardButtons(keys, mouse, true).Dodge;
                InputSystem.QueueStateEvent(mouse, new MouseState()); yield return null;
                check(mouseOn && !mouseBlocked, "Botón de ratón remapeable; bloqueado sobre el panel del laboratorio");

                // ── Interactive capture (same path as clicking a cell in the HUD) ──
                b.ResetDefaults(BindingScheme.Keyboard); b.ResetDefaults(BindingScheme.Gamepad);
                match.BeginRemap(BindingScheme.Keyboard, BrawlAction.Super); yield return null;
                InputSystem.QueueStateEvent(keys, new KeyboardState(Key.L)); yield return null;
                InputSystem.QueueStateEvent(keys, new KeyboardState()); yield return null;
                check(!match.RemapListening && b.Get(BindingScheme.Keyboard, BrawlAction.Super) == "Keyboard/l", "Captura interactiva asigna la tecla pulsada");
                match.BeginRemap(BindingScheme.Keyboard, BrawlAction.Super); yield return null;
                InputSystem.QueueStateEvent(keys, new KeyboardState(Key.Escape)); yield return null;
                InputSystem.QueueStateEvent(keys, new KeyboardState()); yield return null;
                check(!match.RemapListening && b.Get(BindingScheme.Keyboard, BrawlAction.Super) == "Keyboard/l" && match.Phase == MatchPhase.Menu,
                    "Esc cancela la captura sin cambiar controles");
                match.BeginRemap(BindingScheme.Keyboard, BrawlAction.Super); yield return null;
                InputSystem.QueueStateEvent(keys, new KeyboardState(Key.E)); yield return null;
                InputSystem.QueueStateEvent(keys, new KeyboardState()); yield return null;
                check(match.RemapMessageIsWarning && match.RemapMessage.StartsWith("CONFLICTO", StringComparison.Ordinal) &&
                      b.Get(BindingScheme.Keyboard, BrawlAction.Special) == "Keyboard/l" && Unique(b, BindingScheme.Keyboard),
                    "Captura en conflicto avisa e intercambia");
                yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, "controls-remap.png"));
                yield return null;
                match.BeginRemap(BindingScheme.Gamepad, BrawlAction.Super); yield return null;
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.RightStick)); yield return null;
                InputSystem.QueueStateEvent(pad, new GamepadState()); yield return null;
                check(b.Get(BindingScheme.Gamepad, BrawlAction.Super) == "rightStickPress", "Captura de mando asigna el botón pulsado");
                match.BeginRemap(BindingScheme.Gamepad, BrawlAction.Super); yield return null;
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.Start)); yield return null;
                InputSystem.QueueStateEvent(pad, new GamepadState()); yield return null;
                check(!match.RemapListening && b.Get(BindingScheme.Gamepad, BrawlAction.Super) == "rightStickPress", "Start cancela la captura de mando");

                // ── Real labels per device ──
                check(b.Label(BindingScheme.Keyboard, BrawlAction.Jump) == "Espacio" && BrawlBindings.ControlLabel(BindingScheme.Keyboard, "Keyboard/l", keys, null) == "L" &&
                      b.Label(BindingScheme.Keyboard, BrawlAction.Basic) == "Clic izq.", "Etiquetas de teclado y ratón salen de la misma tabla");
                var ps = InputSystem.AddDevice<UnityEngine.InputSystem.DualShock.DualShock4GamepadHID>(); created.Add(ps);
                check(BrawlBindings.PadFamily(ps) == "MANDO PLAYSTATION" && b.Label(BindingScheme.Gamepad, BrawlAction.Jump, ps) == "Cruz",
                    "Mando PlayStation muestra sus botones reales");
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                var xbox = InputSystem.AddDevice<UnityEngine.InputSystem.XInput.XInputControllerWindows>(); created.Add(xbox);
                check(BrawlBindings.PadFamily(xbox) == "MANDO XBOX" && b.Label(BindingScheme.Gamepad, BrawlAction.Jump, xbox) == "A",
                    "Mando Xbox muestra sus botones reales");
                InputSystem.RemoveDevice(xbox);
#endif
                InputSystem.RemoveDevice(ps);
                check(BrawlBindings.PadFamily(null).StartsWith("MANDO", StringComparison.Ordinal) && b.Label(BindingScheme.Gamepad, BrawlAction.Super, null) == "R3",
                    "Sin mando conectado las etiquetas siguen definidas");

                // ── Remap reaches the fighter in a real round ──
                b.ResetDefaults(BindingScheme.Gamepad);
                b.Rebind(BindingScheme.Gamepad, BrawlAction.Jump, "buttonNorth");
                match.LocalCoop = true; match.P1UsesGamepad = false; match.OpponentCount = 1; match.StartMatch();
                yield return new WaitForSeconds(3.2f);
                var fighter = match.Fighters[1];
                fighter.RespawnAt(match.Arena.SpawnPoints[1], false);
                yield return new WaitForSeconds(.3f);
                match.AutomatedTest = false;
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.North));
                yield return new WaitForSeconds(.1f);
                InputSystem.QueueStateEvent(pad, new GamepadState());
                yield return null; match.AutomatedTest = true;
                check(match.Devices.Pad(1) == pad && fighter.JumpsUsed >= 1, "Luchador salta con el botón remapeado durante la partida");
                match.TogglePause();
                check(match.CanRemap, "Remapeo disponible en pausa");
                match.TogglePause(); match.ReturnToMenu();

                // ── SYS-068: versioned, recoverable persistence ──
                yield return null;
                check(File.Exists(live) && BrawlOptionsStore.TryParse(File.ReadAllText(live), out var saved, out _) &&
                      saved.version == BrawlOptionsStore.CurrentVersion && saved.format == BrawlOptionsStore.Format && saved.localCoop &&
                      saved.gamepad.Exists(e => e.action == "Jump" && e.path == "buttonNorth"), "Remapeo y modo se guardan en archivo versionado");
                check(File.Exists(BrawlOptionsStore.BackupPath(live)) && BrawlOptionsStore.TryParse(File.ReadAllText(BrawlOptionsStore.BackupPath(live)), out _, out _) &&
                      !File.Exists(BrawlOptionsStore.TempPath(live)), "Escritura deja copia válida y ningún temporal");
                // Forget the in-memory table without saving, as a fresh start would.
                b.TryLoad(BindingScheme.Keyboard, null); b.TryLoad(BindingScheme.Gamepad, null);
                match.UseOptionsFile(live);
                check(match.OptionsStatus == OptionsLoadStatus.Loaded && b.Get(BindingScheme.Gamepad, BrawlAction.Jump) == "buttonNorth" &&
                      b.Get(BindingScheme.Keyboard, BrawlAction.Super) == "Keyboard/e", "Reinicio restaura controles guardados");

                string backupText = File.ReadAllText(BrawlOptionsStore.BackupPath(live));
                File.WriteAllText(live, "{\"format\":\"funados-options\",\"version\":1,\"sfx\":0.");
                match.UseOptionsFile(live);
                check(match.OptionsStatus == OptionsLoadStatus.RecoveredBackup && BrawlOptionsStore.TryParse(File.ReadAllText(live), out _, out _) &&
                      Directory.GetFiles(folder, "live.json.danado-*").Length == 1, "Archivo truncado: se recupera la copia y se aparta el dañado");
                check(!string.IsNullOrEmpty(match.OptionsStatusText), "La recuperación se comunica en opciones");

                string lone = Path.Combine(folder, "lone.json");
                File.WriteAllText(lone, "\u0000\u0013basura");
                var data = BrawlOptionsStore.Load(lone, out var status, out _);
                check(status == OptionsLoadStatus.ResetCorrupt && data.sfx == .8f && data.music == .5f && !data.reducedEffects,
                    "Archivo dañado sin copia restaura valores seguros");
                File.WriteAllText(lone, "");
                BrawlOptionsStore.Load(lone, out status, out _);
                check(status == OptionsLoadStatus.ResetCorrupt, "Archivo vacío se trata como dañado");
                File.WriteAllText(lone, "{\"format\":\"funados-options\",\"version\":1,\"sfx\":0.3}");
                data = BrawlOptionsStore.Load(lone, out status, out _);
                check(status == OptionsLoadStatus.ResetCorrupt && data.sfx == .8f, "Archivo incompleto no se acepta a medias");
                File.WriteAllText(lone, "{\"format\":\"otro\",\"version\":1,\"reducedEffects\":true,\"sfx\":0.1,\"music\":0.1,\"keyboard\":[],\"gamepad\":[]}");
                BrawlOptionsStore.Load(lone, out status, out _);
                check(status == OptionsLoadStatus.ResetCorrupt, "Formato ajeno se rechaza");
                File.WriteAllText(lone, "{\"format\":\"funados-options\",\"version\":1,\"reducedEffects\":true,\"sfx\":7,\"music\":-3,\"keyboard\":[],\"gamepad\":[]}");
                data = BrawlOptionsStore.Load(lone, out status, out _);
                check(status == OptionsLoadStatus.RepairedFields && data.sfx == 1 && data.music == 0 && data.reducedEffects, "Valores fuera de rango se corrigen");
                File.WriteAllText(lone, "{\"format\":\"funados-options\",\"version\":99,\"reducedEffects\":true,\"sfx\":0.4,\"music\":0.2,\"moveDeadzone\":0.3,\"aimDeadzone\":0.3,\"stickResponse\":1,\"vibration\":0.2,\"keyboard\":[],\"gamepad\":[],\"nuevo\":1}");
                data = BrawlOptionsStore.Load(lone, out status, out _);
                check(status == OptionsLoadStatus.RepairedFields && Mathf.Approximately(data.sfx, .4f), "Versión futura conserva campos conocidos");

                File.WriteAllText(live, "{\"format\":\"funados-options\",\"version\":1,\"reducedEffects\":false,\"sfx\":0.6,\"music\":0.4,\"localCoop\":false,\"p1UsesGamepad\":false," +
                    "\"keyboard\":[{\"action\":\"Jump\",\"path\":\"Keyboard/q\"},{\"action\":\"Super\",\"path\":\"Keyboard/q\"}],\"gamepad\":[{\"action\":\"Pausa\",\"path\":\"start\"},{\"action\":\"Super\",\"path\":\"start\"}]}");
                match.UseOptionsFile(live);
                check(match.OptionsStatus == OptionsLoadStatus.RepairedFields && Unique(b, BindingScheme.Keyboard) &&
                      b.Get(BindingScheme.Keyboard, BrawlAction.Jump) == "Keyboard/space" && b.Get(BindingScheme.Gamepad, BrawlAction.Super) == "leftShoulder" &&
                      Mathf.Approximately(BrawlFeedback.SfxVolume, .6f), "Controles duplicados, reservados o desconocidos vuelven a valores seguros");
                check(BrawlOptionsStore.TryParse(File.ReadAllText(live), out var rewritten, out bool stillRepaired) && !stillRepaired &&
                      rewritten.keyboard.Exists(e => e.action == "Jump" && e.path == "Keyboard/space"), "El archivo reparado se reescribe limpio");

                File.WriteAllText(BrawlOptionsStore.TempPath(live), "{\"format\":\"funados-options\"");
                string before = File.ReadAllText(live);
                match.UseOptionsFile(live);
                check(match.OptionsStatus == OptionsLoadStatus.Loaded && File.ReadAllText(live) == before && !File.Exists(BrawlOptionsStore.TempPath(live)),
                    "Escritura interrumpida (temporal huérfano) no afecta al archivo válido");
                File.Delete(live); File.WriteAllText(BrawlOptionsStore.BackupPath(live), backupText);
                match.UseOptionsFile(live);
                check(match.OptionsStatus == OptionsLoadStatus.RecoveredBackup && File.Exists(live), "Sin archivo principal se recupera la copia");
            }
            finally
            {
                match.CancelRemap();
                match.ReturnToMenu();
                foreach (var device in created) if (device.added) InputSystem.RemoveDevice(device);
                // Reload the player's own options; the test files stay in the evidence folder.
                match.UseOptionsFile(originalPath);
                match.OpponentCount = oldOpponents;
                match.AutomatedTest = true;
            }
        }

        private static bool Unique(BrawlBindings bindings, BindingScheme scheme)
        {
            var seen = new HashSet<string>();
            foreach (BrawlAction action in BrawlBindings.Actions(scheme))
            {
                string path = bindings.Get(scheme, action);
                if (string.IsNullOrEmpty(path) || !seen.Add(path)) return false;
            }
            return true;
        }
    }
}
