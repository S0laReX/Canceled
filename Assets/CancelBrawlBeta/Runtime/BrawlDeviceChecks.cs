using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CancelBrawl.Beta
{
    /// <summary>Explicit smoke-test path only; creates and removes virtual pads, never physical devices.</summary>
    public static class BrawlDeviceChecks
    {
        public static IEnumerator Run(BrawlMatch match, Action<bool, string> check, string output)
        {
            if (Gamepad.all.Count != 0)
            {
                check(false, "Desconecta los mandos físicos para ejecutar la prueba de dispositivos virtuales");
                yield break;
            }
            var created = new List<Gamepad>();
            bool oldLocal = match.LocalCoop, oldPadOnly = match.P1UsesGamepad;
            int oldOpponents = match.OpponentCount;
            try
            {
                match.AutomatedTest = true; match.ReturnToMenu(); match.LocalCoop = true; match.P1UsesGamepad = true;
                match.StartMatch();
                check(match.Phase == MatchPhase.Menu, "Dos mandos: impide iniciar sin dispositivos suficientes");
                var first = InputSystem.AddDevice<Gamepad>(); created.Add(first);
                match.StartMatch();
                check(match.Phase == MatchPhase.Menu, "Dos mandos: un solo mando no crea un rival humano ficticio");
                var second = InputSystem.AddDevice<Gamepad>(); created.Add(second);
                match.P1UsesGamepad = false; match.OpponentCount = 2; match.StartMatch();
                check(match.Devices.KeyboardP1 && match.Devices.Pad(0) == null && match.Devices.Pad(1) == first && match.Devices.Pad(2) == second,
                    "Teclado y mandos: asignaciones P1/P2/P3 exclusivas");
                yield return new WaitForSeconds(3.2f);
                Vector3 p1 = match.Fighters[0].transform.position, p2 = match.Fighters[1].transform.position, p3 = match.Fighters[2].transform.position;
                match.AutomatedTest = false;
                InputSystem.QueueStateEvent(first, new GamepadState { leftStick = Vector2.right });
                yield return new WaitForSeconds(.2f);
                check((match.Fighters[1].transform.position - p2).magnitude > .4f && (match.Fighters[0].transform.position - p1).magnitude < .05f &&
                    (match.Fighters[2].transform.position - p3).magnitude < .05f, "Entrada real del Input System mueve solo el puesto dueño");
                InputSystem.QueueStateEvent(first, new GamepadState());
                yield return null; match.AutomatedTest = true;
                int stocks = match.Fighters[1].Stocks; float damage = match.Fighters[1].Damage;
                float clock = match.RemainingTime; Vector3 paused = match.Fighters[2].transform.position;
                InputSystem.RemoveDevice(first); match.PollDevices();
                check(match.Phase == MatchPhase.Paused && match.DevicePause && match.MissingDeviceMask == (1 << 1), "Desconectar P2 pausa e identifica el puesto perdido");
                check(match.Devices.Pad(2) == second && Gamepad.all[0] == second, "Reordenar Gamepad.all no transfiere P3 a P2");
                match.TogglePause();
                check(match.Phase == MatchPhase.Paused, "No permite reanudar mientras falta un mando");
                yield return new WaitForSeconds(.25f);
                check(match.RemainingTime == clock && match.Fighters[2].transform.position == paused && match.Fighters[1].Stocks == stocks && match.Fighters[1].Damage == damage,
                    "Desconexión conserva reloj, posición, stocks y porcentaje");
                yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, "device-disconnected.png"));
                InputSystem.AddDevice(first); match.PollDevices();
                check(match.MissingDeviceMask == 0 && match.Phase == MatchPhase.Paused && match.Devices.Pad(1) == first, "Reconectar el mismo dispositivo recupera P2 sin reanudación automática");
                match.TogglePause();
                check(match.Phase == MatchPhase.Playing, "Confirmación explícita reanuda después de reconectar");
                InputSystem.DisableDevice(second); match.PollDevices();
                check(match.Phase == MatchPhase.Paused && match.MissingDeviceMask == (1 << 2), "Dispositivo deshabilitado también bloquea la partida");
                InputSystem.EnableDevice(second); match.TogglePause();
                check(match.Phase == MatchPhase.Playing, "Dispositivo rehabilitado conserva su puesto");
                InputSystem.RemoveDevice(first); InputSystem.RemoveDevice(second); match.PollDevices();
                var replacementA = InputSystem.AddDevice<Gamepad>(); created.Add(replacementA);
                var replacementB = InputSystem.AddDevice<Gamepad>(); created.Add(replacementB);
                check(match.MissingDeviceMask == 6 && !match.Devices.Owns(replacementA), "Dos bajas simultáneas: los nuevos mandos no ocupan puestos solos");
                check(!match.Devices.ClaimMissing(first), "No se puede asignar un mando ausente");
                check(match.Devices.ClaimMissing(replacementB) && match.Devices.Pad(1) == replacementB && !match.Devices.ClaimMissing(replacementB),
                    "Reemplazo explícito reclama un solo puesto sin duplicarlo");
                match.ReassignMissingDevices();
                check(match.Devices.Pad(2) == replacementA && match.MissingDeviceMask == 0, "Asignar disponibles rellena el otro puesto sin robar el recuperado");
                match.TogglePause();
                match.ReturnToMenu(); match.OpponentCount = 1; match.P1UsesGamepad = true; match.StartMatch();
                check(!match.Devices.KeyboardP1 && !match.Fighters[0].IsBot && !match.Fighters[1].IsBot && match.Devices.Pad(0) != match.Devices.Pad(1),
                    "Dos mandos: P1 y P2 humanos con dispositivos diferentes");
                var p1Pad = match.Devices.Pad(0); var p2Pad = match.Devices.Pad(1);
                float countdown = match.CountdownRemaining;
                InputSystem.RemoveDevice(p1Pad); match.PollDevices();
                yield return new WaitForSeconds(.2f);
                check(match.Phase == MatchPhase.Paused && match.CountdownRemaining == countdown, "Desconexión durante cuenta atrás congela la cuenta");
                InputSystem.AddDevice(p1Pad); match.TogglePause();
                check(match.Phase == MatchPhase.Countdown && match.Devices.Pad(1) == p2Pad, "Reconectar en cuenta atrás vuelve a esa fase y preserva P2");
                yield return new WaitForSeconds(3.2f);
                p1 = match.Fighters[0].transform.position; p2 = match.Fighters[1].transform.position;
                match.AutomatedTest = false; InputSystem.QueueStateEvent(p1Pad, new GamepadState { leftStick = Vector2.right });
                yield return new WaitForSeconds(.2f);
                check((match.Fighters[0].transform.position - p1).magnitude > .4f && (match.Fighters[1].transform.position - p2).magnitude < .05f,
                    "Dos mandos: entrada de P1 no mueve P2");
                InputSystem.QueueStateEvent(p1Pad, new GamepadState().WithButton(GamepadButton.DpadUp).WithButton(GamepadButton.DpadRight));
                yield return null;
                var digital = match.ReadGamepad(p1Pad);
                check(digital.Move.x > 0 && digital.Move.y > 0 && digital.Move.magnitude <= 1.001f, "Cruceta digital admite diagonales normalizadas");
                InputSystem.QueueStateEvent(p1Pad, new GamepadState()); yield return null; match.AutomatedTest = true;
                match.ReturnToMenu(); match.OpponentCount = 4;
                for (int i = 0; i < 4; i++) created.Add(InputSystem.AddDevice<Gamepad>());
                match.StartMatch();
                var owners = new HashSet<Gamepad>(); bool unique = match.Fighters.Length == 5;
                for (int i = 0; i < 5; i++) unique &= !match.Fighters[i].IsBot && owners.Add(match.Devices.Pad(i));
                check(unique && match.RejectedPads == 1, "Cinco jugadores con mandos únicos; el sexto queda fuera");
                InputSystem.RemoveDevice(created[created.Count - 1]); match.PollDevices();
                check(match.Phase == MatchPhase.Countdown && !match.DevicePause, "Desconectar un mando sobrante no pausa la partida");
                match.ReturnToMenu(); match.P1UsesGamepad = false; match.OpponentCount = 1; match.StartMatch();
                check(match.RejectedPads == 4, "Número de mandos sobrantes respeta los puestos elegidos");
                match.ReturnToMenu(); match.LocalCoop = false; match.StartTraining();
                check(match.Devices.Pad(0) != null && !match.Devices.IsHuman(1), "Entrenamiento conserva mando de P1 y objetivo sin entradas");
                match.ReturnToMenu();
                check(match.Devices.Count == 0 && !match.DevicePause, "Volver al menú libera reservas y aviso de desconexión");
            }
            finally
            {
                match.ReturnToMenu();
                foreach (var pad in created) if (pad.added) InputSystem.RemoveDevice(pad);
                match.LocalCoop = oldLocal; match.P1UsesGamepad = oldPadOnly; match.OpponentCount = oldOpponents;
                match.AutomatedTest = true;
            }
        }
    }
}
