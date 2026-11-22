using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace CancelBrawl.Beta
{
    /// <summary>DualShock 4 USB output report (id 5): rumble + light bar. Same layout the Input System sends.</summary>
    [StructLayout(LayoutKind.Explicit, Size = InputDeviceCommand.BaseCommandSize + 32)]
    internal struct DualShock4UsbOutput : IInputDeviceCommandInfo
    {
        public static FourCC Type => new FourCC('H', 'I', 'D', 'O');
        [FieldOffset(0)] public InputDeviceCommand baseCommand;
        [FieldOffset(InputDeviceCommand.BaseCommandSize + 0)] public byte reportId;
        [FieldOffset(InputDeviceCommand.BaseCommandSize + 1)] public byte flags;
        [FieldOffset(InputDeviceCommand.BaseCommandSize + 4)] public byte highFrequency;
        [FieldOffset(InputDeviceCommand.BaseCommandSize + 5)] public byte lowFrequency;
        [FieldOffset(InputDeviceCommand.BaseCommandSize + 6)] public byte red;
        [FieldOffset(InputDeviceCommand.BaseCommandSize + 7)] public byte green;
        [FieldOffset(InputDeviceCommand.BaseCommandSize + 8)] public byte blue;
        public FourCC typeStatic => Type;

        public static DualShock4UsbOutput Create(float low, float high, Color color) => new DualShock4UsbOutput
        {
            baseCommand = new InputDeviceCommand(Type, InputDeviceCommand.BaseCommandSize + 32),
            reportId = 5, flags = 0x03,
            lowFrequency = (byte)Mathf.Clamp(low * 255, 0, 255), highFrequency = (byte)Mathf.Clamp(high * 255, 0, 255),
            red = (byte)Mathf.Clamp(color.r * 255, 0, 255), green = (byte)Mathf.Clamp(color.g * 255, 0, 255), blue = (byte)Mathf.Clamp(color.b * 255, 0, 255)
        };
    }

    /// <summary>
    /// Human-assisted diagnostic, only with -haptics-probe: shows numbered steps on screen while it drives the light
    /// bar and the motors in different ways, logs device details and command results, then quits.
    /// </summary>
    public sealed class BrawlHapticsProbe : MonoBehaviour
    {
        [Serializable] private sealed class Report { public string version; public List<string> devices = new List<string>(); public List<string> steps = new List<string>(); }
        private readonly Report report = new Report();
        private BrawlMatch match;
        private string output, banner = "Conecta el mando y sostenlo en las manos…";
        private GUIStyle style;

        public static string Describe(InputDevice device) =>
            $"{device.displayName} | tipo={device.GetType().Name} | layout={device.layout} | interfaz={device.description.interfaceName} | " +
            $"producto={device.description.product} | fabricante={device.description.manufacturer} | id={device.deviceId} | activo={device.enabled}";

        private void Awake()
        {
            match = GetComponent<BrawlMatch>();
            report.version = match.DisplayVersion;
            string[] args = Environment.GetCommandLineArgs();
            int outputArg = Array.IndexOf(args, "-evidence-path");
            output = Path.GetFullPath(outputArg >= 0 && outputArg + 1 < args.Length ? args[outputArg + 1] : "Evidence/HapticsProbe");
            Directory.CreateDirectory(output);
        }

        private IEnumerator Start()
        {
            float wait = Time.realtimeSinceStartup;
            while (Gamepad.all.Count == 0 && Time.realtimeSinceStartup - wait < 30) yield return null;
            foreach (var device in InputSystem.devices) if (device is Gamepad) report.devices.Add(Describe(device));
            if (Gamepad.all.Count == 0) { Step("Sin mando tras 30 s"); Finish(); yield break; }
            var pad = Gamepad.all[0];
            yield return Countdown("Prepárate: empieza en", 3);

            banner = "PASO 1/4 · ¿La LUZ del mando cambia a ROSA?";
            if (pad is UnityEngine.InputSystem.DualShock.DualShockGamepad dualShock)
            { dualShock.SetLightBarColor(new Color(1, 0, .6f)); Step("1 luz rosa con SetLightBarColor (DualShockGamepad)"); }
            else Step("1 sin barra de luz: el juego no ve este mando como DualShock (" + pad.GetType().Name + ")");
            yield return new WaitForSecondsRealtime(2.5f);

            banner = "PASO 2/4 · ¿VIBRA fuerte? (llamada normal de Unity, 100 %)";
            pad.SetMotorSpeeds(1, 1); Step("2 SetMotorSpeeds(1,1)");
            yield return new WaitForSecondsRealtime(1.2f);
            pad.SetMotorSpeeds(0, 0);
            yield return new WaitForSecondsRealtime(1.5f);

            banner = "PASO 3/4 · ¿VIBRA fuerte y la luz pasa a AZUL? (informe USB directo)";
            var command = DualShock4UsbOutput.Create(1, 1, new Color(0, .6f, 1));
            long result = pad.ExecuteCommand(ref command);
            Step("3 informe USB DualShock 4 id 5, resultado=" + result + (result >= 0 ? " (aceptado)" : " (rechazado)"));
            yield return new WaitForSecondsRealtime(1.2f);
            var stop = DualShock4UsbOutput.Create(0, 0, new Color(0, .6f, 1));
            pad.ExecuteCommand(ref stop);
            yield return new WaitForSecondsRealtime(1.5f);

            banner = "PASO 4/4 · ¿Notas esta vibración? (la que hacía el juego en OPCIONES)";
            pad.SetMotorSpeeds(.25f, .25f); Step("4 vista previa anterior: 25 % durante 0,25 s");
            yield return new WaitForSecondsRealtime(.25f);
            pad.ResetHaptics();
            yield return new WaitForSecondsRealtime(1.5f);
            banner = "Fin. Dile al agente qué pasos notaste (1 luz, 2, 3, 4).";
            yield return new WaitForSecondsRealtime(3);
            Finish();
        }

        private IEnumerator Countdown(string text, int seconds)
        {
            for (int i = seconds; i > 0; i--) { banner = text + " " + i; yield return new WaitForSecondsRealtime(1); }
        }

        private void Step(string text) { report.steps.Add(text); Debug.Log("FUNADOS_HAPTIC " + text); }

        private void Finish()
        {
            foreach (var pad in Gamepad.all) { try { pad.ResetHaptics(); } catch (Exception) { } }
            File.WriteAllText(Path.Combine(output, "haptics-report.json"), JsonUtility.ToJson(report, true));
            Application.Quit();
        }

        private void OnGUI()
        {
            if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true, normal = { textColor = Color.white } };
            GUI.color = new Color(0, 0, 0, .85f);
            GUI.DrawTexture(new Rect(0, Screen.height * .3f, Screen.width, Screen.height * .4f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(40, Screen.height * .3f, Screen.width - 80, Screen.height * .4f), banner, style);
        }
    }
}
