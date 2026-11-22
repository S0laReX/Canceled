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
    /// Explicit smoke-test path only: stick tuning and vibration (SYS-026), options v1→v2 migration (SYS-068) and
    /// order-independent simultaneous hits (SYS-045). Virtual devices and evidence-folder option files only.
    /// </summary>
    public static class BrawlTuningChecks
    {
        public static IEnumerator Run(BrawlMatch match, Action<bool, string> check, string output)
        {
            if (Gamepad.all.Count != 0)
            {
                check(false, "Desconecta los mandos físicos para ejecutar la prueba de ajustes de mando");
                yield break;
            }
            string folder = Path.Combine(output, "tuning-checks");
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            Directory.CreateDirectory(folder);
            string originalPath = match.OptionsPath;
            int oldCharacter = match.SelectedCharacter, oldOpponents = match.OpponentCount;
            var created = new List<InputDevice>();
            try
            {
                match.AutomatedTest = true; match.ReturnToMenu();
                string file = Path.Combine(folder, "tuning.json");
                match.UseOptionsFile(file);

                // ── SYS-026: stick shaping ──
                check(BrawlInputTuning.Shape(new Vector2(.15f, 0), .2f, 1) == Vector2.zero &&
                      BrawlInputTuning.Shape(new Vector2(0, -.19f), .2f, 1) == Vector2.zero, "Zona muerta ignora deriva de la palanca");
                Vector2 full = BrawlInputTuning.Shape(new Vector2(.95f, 0), .2f, 1), half = BrawlInputTuning.Shape(new Vector2(.575f, 0), .2f, 1);
                check(Mathf.Abs(full.x - 1) < .001f && Mathf.Abs(half.x - .5f) < .01f, "Fuera de la zona muerta la escala recorre 0–1 sin saltos");
                float fast = BrawlInputTuning.Shape(new Vector2(.575f, 0), .2f, 2).x, fine = BrawlInputTuning.Shape(new Vector2(.575f, 0), .2f, .5f).x;
                check(fast > .6f && fine < .3f, "Respuesta rápida y fina cambian la curva");
                Vector2 diagonal = BrawlInputTuning.Shape(new Vector2(.6f, .6f), .2f, 1);
                check(Vector2.Angle(diagonal, Vector2.one) < .01f && diagonal.magnitude <= 1.0001f, "Diagonal conserva dirección y magnitud máxima 1");

                match.SetStickTuning(5, -1, 9);
                check(match.MoveDeadzone == BrawlInputTuning.MaxDeadzone && match.AimDeadzone == BrawlInputTuning.MinDeadzone &&
                      match.StickResponse == BrawlInputTuning.MaxResponse, "Ajustes de palanca limitados a rangos seguros");

                var pad = InputSystem.AddDevice<Gamepad>(); created.Add(pad);
                match.SetStickTuning(.3f, .25f, 1);
                InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = new Vector2(.25f, 0), rightStick = new Vector2(.2f, 0) }); yield return null;
                var small = match.ReadGamepad(pad);
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.DpadRight)); yield return null;
                var digital = match.ReadGamepad(pad);
                match.SetStickTuning(.1f, .1f, 1);
                InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = new Vector2(.25f, 0), rightStick = new Vector2(.2f, 0) }); yield return null;
                var sensitive = match.ReadGamepad(pad);
                InputSystem.QueueStateEvent(pad, new GamepadState()); yield return null;
                check(small.Move == Vector2.zero && small.Aim == Vector2.zero && sensitive.Move.x > 0 && sensitive.Aim.x > 0,
                    "Zonas muertas de mover y orientar se aplican al mando real");
                check(Mathf.Approximately(digital.Move.x, 1), "Cruceta sigue siendo alternativa digital completa");

                match.SetStickTuning(.15f, .2f, 1.25f);
                match.SetVibration(.75f);
                yield return new WaitForSecondsRealtime(.6f); // let the preview pulse end
                check(match.HapticLevel(pad) == 0, "Vista previa de vibración termina sola");
                match.UseOptionsFile(file);
                check(match.OptionsStatus == OptionsLoadStatus.Loaded && Mathf.Approximately(match.MoveDeadzone, .15f) && Mathf.Approximately(match.AimDeadzone, .2f) &&
                      Mathf.Approximately(match.StickResponse, 1.25f) && Mathf.Approximately(match.VibrationStrength, .75f) &&
                      BrawlOptionsStore.TryParse(File.ReadAllText(file), out var stored, out _) && stored.version == BrawlOptionsStore.CurrentVersion, "Ajustes de mando se guardan en el formato actual");

                // ── SYS-068: version 1 files migrate ──
                string legacy = Path.Combine(folder, "v1.json");
                File.WriteAllText(legacy, "{\"format\":\"funados-options\",\"version\":1,\"reducedEffects\":true,\"sfx\":0.3,\"music\":0.6,\"localCoop\":true,\"p1UsesGamepad\":false," +
                    "\"keyboard\":[{\"action\":\"Jump\",\"path\":\"Keyboard/k\"}],\"gamepad\":[]}");
                match.UseOptionsFile(legacy);
                check(match.OptionsStatus == OptionsLoadStatus.MigratedLegacy && match.Bindings.Get(BindingScheme.Keyboard, BrawlAction.Jump) == "Keyboard/k" &&
                      Mathf.Approximately(BrawlFeedback.SfxVolume, .3f) && Mathf.Approximately(match.VibrationStrength, new BrawlOptionsData().vibration) && Mathf.Approximately(match.MoveDeadzone, .2f),
                    "Archivo versión 1 se migra conservando controles y volúmenes");
                check(BrawlOptionsStore.TryParse(File.ReadAllText(legacy), out var migrated, out _, out bool stillOld) && migrated.version == BrawlOptionsStore.CurrentVersion && !stillOld &&
                      File.ReadAllText(BrawlOptionsStore.BackupPath(legacy)).Contains("\"version\":1"), "Migración reescribe v2 y guarda la v1 como copia");
                File.WriteAllText(legacy, "{\"format\":\"funados-options\",\"version\":2,\"reducedEffects\":false,\"sfx\":0.3,\"music\":0.6,\"keyboard\":[],\"gamepad\":[]}");
                BrawlOptionsStore.Load(legacy, out var status, out _);
                check(status == OptionsLoadStatus.RecoveredBackup, "Archivo v2 sin campos de mando se trata como incompleto");
                File.WriteAllText(legacy, "{\"format\":\"funados-options\",\"version\":2,\"reducedEffects\":false,\"sfx\":0.3,\"music\":0.6,\"moveDeadzone\":3," +
                    "\"aimDeadzone\":-1,\"stickResponse\":0,\"vibration\":4,\"keyboard\":[],\"gamepad\":[]}");
                var repaired = BrawlOptionsStore.Load(legacy, out status, out _);
                check(status == OptionsLoadStatus.RepairedFields && repaired.moveDeadzone == BrawlInputTuning.MaxDeadzone && repaired.aimDeadzone == BrawlInputTuning.MinDeadzone &&
                      repaired.stickResponse == BrawlInputTuning.MinResponse && repaired.vibration == 1, "Ajustes de mando fuera de rango se corrigen");
                match.UseOptionsFile(file);

                // ── SYS-045: order-independent combination ──
                var settings = match.Settings;
                var spec = settings.Attack(AttackKind.Heavy, BrawlRoster.Get(0), 0);
                match.SelectedCharacter = 0; match.OpponentCount = 2; match.LocalCoop = false; match.StartMatch();
                yield return new WaitForSeconds(3.2f);
                BrawlFighter a = match.Fighters[0], b = match.Fighters[1], c = match.Fighters[2];
                var forward = new List<PendingHit> { new PendingHit(c, a, spec, Vector3.right), new PendingHit(c, b, settings.Attack(AttackKind.Basic, BrawlRoster.Get(1), 0), Vector3.forward) };
                var reverse = new List<PendingHit> { forward[1], forward[0] };
                var one = BrawlFighter.CombineHits(20, 1, settings, forward);
                var two = BrawlFighter.CombineHits(20, 1, settings, reverse);
                check(Mathf.Approximately(one.Damage, two.Damage) && (one.Velocity - two.Velocity).sqrMagnitude < 1e-6f && one.Attacker == two.Attacker && one.Attacker == a,
                    "Golpes simultáneos al mismo objetivo no dependen del orden de jugadores");
                var single = BrawlFighter.CombineHits(20, 1.1f, settings, new List<PendingHit> { forward[0] });
                float damage = 20 + spec.Damage, push = Mathf.Min(settings.maxKnockback, (spec.Push + damage * settings.knockbackPerDamage) / 1.1f);
                check(Mathf.Approximately(single.Damage, damage) && Mathf.Abs(single.Velocity.x - push) < .001f &&
                      Mathf.Abs(single.Velocity.y - Mathf.Min(16, spec.Lift + damage * .018f)) < .001f, "Un solo golpe conserva la fórmula de empuje anterior");
                var opposite = BrawlFighter.CombineHits(0, 1, settings, new List<PendingHit> { new PendingHit(c, a, spec, Vector3.right), new PendingHit(c, b, spec, Vector3.left) });
                check(new Vector2(opposite.Velocity.x, opposite.Velocity.z).magnitude < .001f && opposite.Damage == spec.Damage * 2 && opposite.Attacker == a,
                    "Golpes opuestos suman daño y anulan el empuje horizontal");

                // ── SYS-045: a real mirror trade inside one physics step ──
                match.ReturnToMenu();
                match.SelectedCharacter = 0; while (match.SlotCharacters[1] != 0) match.CycleCharacter(1);
                match.OpponentCount = 1; match.StartMatch();
                yield return new WaitForSeconds(3.2f);
                bool[] traded = new bool[2];
                for (int round = 0; round < 2; round++)
                {
                    BrawlFighter south = match.Fighters[round], north = match.Fighters[1 - round];
                    south.RespawnAt(new Vector3(-5, .05f, -2), false); north.RespawnAt(new Vector3(-5, .05f, -.55f), false);
                    south.SetPracticeDamage(0); north.SetPracticeDamage(0);
                    yield return new WaitForSeconds(.3f);
                    yield return new WaitForFixedUpdate();
                    south.Submit(new BrawlCommand { Aim = Vector2.up, Basic = true });
                    north.Submit(new BrawlCommand { Aim = Vector2.down, Basic = true });
                    yield return new WaitForSeconds(.45f);
                    south.ClearInput(); north.ClearInput();
                    traded[round] = Mathf.Abs(south.Damage - settings.basicDamage) < .01f && Mathf.Abs(north.Damage - settings.basicDamage) < .01f &&
                                    south.LastAttacker == north && north.LastAttacker == south;
                }
                check(traded[0], "Intercambio simultáneo: ambos reciben el golpe (P1 al sur)");
                check(traded[1], "Intercambio simultáneo: mismo resultado con los puestos invertidos");
                check(!match.CollectingHits, "La ventana de impactos se cierra al final de cada paso");

                // ── SYS-026: vibration follows ownership and the player's setting ──
                match.ReturnToMenu();
                match.LocalCoop = true; match.P1UsesGamepad = false; match.OpponentCount = 1; match.StartMatch();
                yield return new WaitForSeconds(3.2f);
                var heavy = settings.Attack(AttackKind.Heavy, BrawlRoster.Get(0), 0);
                match.Fighters[1].RespawnAt(new Vector3(6, .05f, -2), false);
                match.Fighters[1].ReceiveHit(heavy, Vector3.left, match.Fighters[0]);
                float hitLevel = match.HapticLevel(pad);
                check(match.Devices.Pad(1) == pad && hitLevel >= BrawlMatch.HapticFloor * .6f && hitLevel <= 1, "Golpe recibido vibra el mando dueño con fuerza perceptible");
                check(BrawlMatch.HapticOutput(.05f, .25f) >= .38f && BrawlMatch.HapticOutput(1, 1) == 1 && BrawlMatch.HapticOutput(1, 0) == 0 &&
                      BrawlMatch.HapticOutput(.5f, 1) > BrawlMatch.HapticOutput(.5f, .25f), "Vibración nunca queda bajo el mínimo perceptible; 0 % la apaga");
                match.TogglePause(); yield return null; yield return null;
                check(match.HapticLevel(pad) == 0, "Pausa detiene la vibración");
                match.TogglePause();
                match.SetVibration(0); yield return null;
                match.Fighters[1].RespawnAt(new Vector3(6, .05f, -2), false);
                match.Fighters[1].ReceiveHit(heavy, Vector3.left, match.Fighters[0]);
                check(match.HapticLevel(pad) == 0, "Vibración apagada no hace vibrar");
                match.SetVibration(.5f);
                yield return new WaitForSecondsRealtime(.6f); // preview pulse on the panel's pad ends first
                match.Fighters[0].RespawnAt(new Vector3(-6, .05f, -2), false);
                match.Fighters[0].ReceiveHit(heavy, Vector3.right, match.Fighters[1]);
                check(match.HapticLevel(pad) == 0, "Golpe a P1 con teclado no hace vibrar el mando de P2");
                match.ShowPadOptions = true; match.TogglePause(); yield return null;
                yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, "pad-options.png"));
                yield return null;
                match.ShowPadOptions = false;
            }
            finally
            {
                match.ReturnToMenu();
                foreach (var device in created) if (device.added) InputSystem.RemoveDevice(device);
                match.UseOptionsFile(originalPath);
                match.SelectedCharacter = oldCharacter; match.OpponentCount = oldOpponents;
                match.AutomatedTest = true;
            }
        }
    }
}
