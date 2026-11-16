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
    /// Explicit smoke-test path only: menu navigation with keyboard/gamepad and visible focus (SYS-070), and the lab's
    /// projectile/zone/trap volumes sharing the real hit constants. Virtual devices and evidence-folder options only.
    /// </summary>
    public static class BrawlMenuChecks
    {
        public static IEnumerator Run(BrawlMatch match, Action<bool, string> check, string output)
        {
            if (Gamepad.all.Count != 0)
            {
                check(false, "Desconecta los mandos físicos para ejecutar la prueba de navegación");
                yield break;
            }
            string folder = Path.Combine(output, "menu-checks");
            Directory.CreateDirectory(folder);
            string originalPath = match.OptionsPath;
            int oldCharacter = match.SelectedCharacter, oldOpponents = match.OpponentCount, oldArena = match.ArenaIndex;
            var hud = match.Hud;
            var created = new List<InputDevice>();
            try
            {
                match.AutomatedTest = true; match.ReturnToMenu();
                match.UseOptionsFile(Path.Combine(folder, "menu.json"));
                match.ShowPadOptions = false; match.ArenaIndex = 0; match.SelectedCharacter = 0; match.OpponentCount = 1;
                hud.ForceNavigation = true;
                var keys = InputSystem.AddDevice<Keyboard>(); created.Add(keys);
                var pad = InputSystem.AddDevice<Gamepad>(); created.Add(pad);
                yield return null; yield return null; yield return null;

                var ids = new List<string>(hud.FocusableIds);
                check(hud.FocusId == "start", "Menú abre con el foco en ENTRAR");
                check(ids.Contains("char-4") && ids.Contains("arena-3") && ids.Contains("rivals-4") && ids.Contains("rival-1") && ids.Contains("coop") &&
                      ids.Contains("kb-Jump") && ids.Contains("pad-Super") && ids.Contains("opt-sfx") && ids.Contains("tab-pad") && ids.Contains("training"),
                    "Todos los controles del menú son alcanzables con foco");

                hud.Focus("arena-0");
                yield return Press(pad, GamepadButton.DpadRight);
                check(hud.FocusId == "arena-1", "Cruceta mueve el foco a la cancha de la derecha");
                yield return Press(pad, GamepadButton.South);
                check(match.ArenaIndex == 1, "A elige la cancha enfocada");
                yield return Tap(keys, Key.UpArrow);
                check(hud.FocusId == "char-1", "Flecha arriba lleva al luchador alineado encima");
                yield return Tap(keys, Key.Enter);
                check(match.SelectedCharacter == 1 && match.Phase == MatchPhase.Menu, "Enter elige el luchador enfocado sin iniciar la partida");
                yield return Tap(keys, Key.Enter);
                check(match.SlotSkins[0] == 1 || BrawlRoster.SkinCount(1) == 1, "Elegir de nuevo cambia el aspecto, como con el ratón");

                match.SetVolumes(.5f, .5f);
                hud.Focus("opt-sfx");
                yield return Press(pad, GamepadButton.DpadRight);
                bool raised = Mathf.Approximately(BrawlFeedback.SfxVolume, .6f);
                yield return Tap(keys, Key.LeftArrow);
                check(raised && Mathf.Approximately(BrawlFeedback.SfxVolume, .5f) && hud.FocusId == "opt-sfx", "Izquierda/derecha ajustan el valor sin perder el foco");

                hud.Focus("rival-1");
                int skin = match.SlotSkins[1];
                yield return Press(pad, GamepadButton.DpadRight);
                check(match.SlotSkins[1] != skin || BrawlRoster.SkinCount(match.SlotCharacters[1]) == 1, "Derecha en un rival cambia su aspecto");

                hud.Focus("tab-pad");
                yield return Press(pad, GamepadButton.South);
                yield return null; yield return null;
                check(match.ShowPadOptions && hud.FocusId == "tab-pad" && new List<string>(hud.FocusableIds).Contains("pad-vibration"), "Pestaña MANDO navegable sin perder el foco");
                match.ShowPadOptions = false; yield return null; yield return null;

                hud.Focus("char-0");
                InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = Vector2.right });
                yield return new WaitForSecondsRealtime(.62f);
                InputSystem.QueueStateEvent(pad, new GamepadState()); yield return null;
                check(hud.FocusId == "char-3" || hud.FocusId == "char-4" || hud.FocusId == "char-2", "Palanca mantenida repite el movimiento del foco");

                hud.Focus("kb-Jump");
                yield return Press(pad, GamepadButton.South);
                check(match.RemapListening && match.RemapScheme == BindingScheme.Keyboard && match.RemapAction == BrawlAction.Jump, "Remapeo accesible con el mando desde el foco");
                match.CancelRemap();

                hud.Focus("arena-1"); yield return null; yield return null;
                yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, "menu-focus.png"));
                yield return null;

                hud.Focus("start"); match.ArenaIndex = 0; match.SelectedCharacter = 0;
                yield return Tap(keys, Key.Enter);
                check(match.Phase == MatchPhase.Countdown, "Enter sobre ENTRAR inicia la partida como antes");
                match.ReturnToMenu();

                // ── Pause: owned pads navigate; spare pads stay inert ──
                var spare = InputSystem.AddDevice<Gamepad>(); created.Add(spare);
                match.LocalCoop = true; match.P1UsesGamepad = false; match.OpponentCount = 1; match.StartMatch();
                yield return null;
                match.TogglePause(); yield return null; yield return null; yield return null;
                check(match.Phase == MatchPhase.Paused && hud.FocusId == "continue" && match.Devices.Pad(1) == pad, "Pausa abre con el foco en CONTINUAR");
                yield return Press(spare, GamepadButton.DpadDown);
                check(hud.FocusId == "continue", "Mando sobrante no mueve el foco de la pausa");
                yield return Press(pad, GamepadButton.DpadDown);
                check(hud.FocusId == "menu", "Mando del jugador navega la pausa");
                yield return Press(pad, GamepadButton.East);
                check(match.Phase == MatchPhase.Countdown, "B reanuda desde la pausa");
                match.FinishByTime(); yield return null; yield return null; yield return null;
                check(match.Phase == MatchPhase.Results && hud.FocusId == "rematch", "Resultados abren con el foco en REVANCHA");
                yield return Tap(keys, Key.Enter);
                check(match.Phase == MatchPhase.Countdown, "Enter en resultados inicia la revancha");
                match.ReturnToMenu();
                InputSystem.RemoveDevice(spare);

                // ── Lab volumes share the hit constants ──
                hud.ForceNavigation = false;
                match.LocalCoop = false; match.StartTraining(); match.ShowCombatVolumes = true;
                yield return new WaitForSeconds(.3f);
                BrawlFighter p1 = match.Fighters[0], target = match.Fighters[1];
                p1.RespawnAt(match.Arena.SpawnPoints[0], false); target.RespawnAt(match.Arena.SpawnPoints[0] + Vector3.right * 3, false);
                yield return new WaitForSeconds(.4f);
                Vector3 chest = target.transform.position + Vector3.up * BrawlProjectile.ChestHeight;
                var spec = match.Settings.Attack(AttackKind.Special, BrawlRoster.Get(0), 0);
                float before = target.Damage;
                BrawlProjectile.Create(match, new BrawlAttackToken(p1, spec), chest + Vector3.left * (BrawlProjectile.HitDistance + .1f), Vector3.left, p1.PlayerColor, "Prueba", Vector3.one * .2f, 0, .3f);
                yield return null;
                int drawn = match.HazardVolumes.DrawnVolumes;
                yield return new WaitForSeconds(.35f);
                bool missed = Mathf.Approximately(target.Damage, before);
                BrawlProjectile.Create(match, new BrawlAttackToken(p1, spec), chest + Vector3.left * (BrawlProjectile.HitDistance - .1f), Vector3.left, p1.PlayerColor, "Prueba", Vector3.one * .2f, 0, .3f);
                yield return new WaitForSeconds(.1f);
                check(drawn >= 1 && missed && target.Damage > before, "Volumen de proyectil coincide con su distancia real de impacto");

                var zoneToken = new BrawlAttackToken(p1, match.Settings.Attack(AttackKind.Super, BrawlRoster.Get(1), 0));
                var zone = BrawlZone.Create(match, zoneToken, target.transform.position + Vector3.forward * 6, 2, 2, 5, false);
                var trap = BrawlTrap.Create(match, new BrawlAttackToken(p1, spec), target.transform.position + Vector3.back * 6);
                yield return null; yield return null;
                check(match.HazardVolumes.DrawnVolumes >= 2 && Mathf.Approximately(zone.HitRadius, 2 + BrawlZone.Tolerance), "Zonas y emisor se dibujan con su volumen real");
                yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, "lab-hazards.png"));
                yield return null;
                match.ShowCombatVolumes = false; yield return null; yield return null;
                check(match.HazardVolumes.DrawnVolumes == 0, "Opción de volúmenes oculta también proyectiles y zonas");
                if (zone) UnityEngine.Object.Destroy(zone.gameObject);
                if (trap) UnityEngine.Object.Destroy(trap.gameObject);
                match.ShowCombatVolumes = true;
            }
            finally
            {
                hud.ForceNavigation = false;
                match.CancelRemap();
                match.ReturnToMenu();
                foreach (var device in created) if (device.added) InputSystem.RemoveDevice(device);
                match.UseOptionsFile(originalPath);
                match.SelectedCharacter = oldCharacter; match.OpponentCount = oldOpponents; match.ArenaIndex = oldArena;
                match.AutomatedTest = true;
            }
        }

        private static IEnumerator Press(Gamepad pad, GamepadButton button)
        {
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(button)); yield return null;
            InputSystem.QueueStateEvent(pad, new GamepadState()); yield return null;
        }

        private static IEnumerator Tap(Keyboard keys, Key key)
        {
            InputSystem.QueueStateEvent(keys, new KeyboardState(key)); yield return null;
            InputSystem.QueueStateEvent(keys, new KeyboardState()); yield return null;
        }
    }
}
