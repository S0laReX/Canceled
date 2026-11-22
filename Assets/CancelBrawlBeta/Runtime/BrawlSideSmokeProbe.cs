using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>Opt-in integration proof for the production 2.5D rules, in a standalone Player.</summary>
    public sealed class BrawlSideSmokeProbe : MonoBehaviour
    {
        [Serializable] private sealed class Report
        {
            public string version = BrawlMatch.ProductionVersion;
            public bool passed;
            public List<string> checks = new List<string>(), failures = new List<string>();
        }
        private readonly Report report = new Report();
        private BrawlMatch match;
        private string output;
        private float started;
        private bool finished;
        private void Awake()
        {
            match = GetComponent<BrawlMatch>(); started = Time.realtimeSinceStartup;
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "-evidence-path");
            output = Path.GetFullPath(index >= 0 && index + 1 < args.Length ? args[index + 1] : "Evidence/Production-"+BrawlMatch.ProductionVersion);
            Directory.CreateDirectory(output); Application.logMessageReceived += OnLog;
            // Long runs: switching windows must not disable the virtual pads under test (normal play keeps the default).
            UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
            BrawlDeviceChecks.IsolatePhysicalPads();
        }
        private void OnDestroy() => Application.logMessageReceived -= OnLog;
        private void OnLog(string message, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) report.failures.Add(message + "\n" + stack); }
        private void Check(bool value, string name)
        { (value ? report.checks : report.failures).Add(name); Debug.Log("SIDE_CHECK_" + (value ? "PASS " : "FAIL ") + name); }
        private void Update() { if (!finished && Time.realtimeSinceStartup - started > 2400) { Check(false, "Tiempo máximo agotado"); Finish(); } }
        private void Finish()
        {
            finished = true; report.passed = report.failures.Count == 0;
            File.WriteAllText(Path.Combine(output, "side-report.json"), JsonUtility.ToJson(report, true));
            Debug.Log("SIDE_RESULT " + report.passed + " checks=" + report.checks.Count + " failures=" + report.failures.Count);
            Application.Quit(report.passed ? 0 : 1);
        }
        private IEnumerator Start()
        {
            yield return null;
            if (Array.IndexOf(Environment.GetCommandLineArgs(),"-power-smoke")>=0)
            {
                yield return BrawlSidePowerChecks.Run(match,Check,output); Finish(); yield break;
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(),"-camera-smoke")>=0)
            {
                Check(match.IsSideView, "Producción usa reglas laterales 2.5D");
                yield return BrawlCameraChecks.Run(match,Check,output); Finish(); yield break;
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(),"-recovery-smoke")>=0)
            {
                Check(match.IsSideView, "Producción usa reglas laterales 2.5D");
                yield return BrawlRecoveryChecks.Run(match,Check,output); Finish(); yield break;
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(),"-kits-smoke")>=0)
            {
                Check(match.IsSideView, "Producción usa reglas laterales 2.5D");
                yield return BrawlKitMatrixChecks.Run(match,Check,output); Finish(); yield break;
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(),"-moves-smoke")>=0)
            {
                Check(match.IsSideView, "Producción usa reglas laterales 2.5D");
                yield return BrawlMoveChecks.Run(match,Check,output);
                yield return BrawlComboStudy.Run(match,Check,output); Finish(); yield break;
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(),"-side-controls-smoke")>=0)
            {
                Check(match.IsSideView, "Producción usa reglas laterales 2.5D");
                yield return ControlChecks(); Finish(); yield break;
            }
            Check(match.IsSideView, "Producción usa reglas laterales 2.5D");
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output,"menu.png"));
            match.StartTraining(); yield return new WaitForSeconds(.25f);
            var a = match.Fighters[0]; var b = match.Fighters[1];
            Check(a.SkinName != "Provisional" && b.SkinName != "Provisional", "Personajes originales conservados");
            var start = a.transform.position;
            a.Submit(new BrawlCommand { Move = Vector2.up }); yield return new WaitForSeconds(.2f); a.ClearInput();
            Check(Mathf.Abs(a.transform.position.z) < .001f && Mathf.Abs(a.transform.position.x-start.x) < .01f, "Arriba no desplaza al personaje en profundidad");
            a.Submit(new BrawlCommand { Move = Vector2.right }); yield return new WaitForSeconds(.2f); a.ClearInput();
            Check(a.transform.position.x > start.x + .5f && Mathf.Abs(a.transform.position.z)<.001f, "Movimiento horizontal sobre plano fijo");
            Place(a,new Vector3(-2,.05f,8)); Place(b,new Vector3(-1,.05f,-6)); yield return new WaitForSeconds(.2f);
            Check(a.transform.position.z == 0 && b.transform.position.z == 0, "Reaparición corrige profundidad fuera del plano");
            a.Submit(new BrawlCommand { Move = Vector2.up, Basic = true }); yield return new WaitForSeconds(.13f);
            Check(b.Damage == 4 && b.Velocity.y > 6, "Golpe ascendente conecta una vez y eleva al rival");
            a.Submit(new BrawlCommand { Jump = true }); yield return new WaitForSeconds(.2f);
            Check(a.transform.position.y > .3f && a.JumpsUsed == 1, "Salto tras golpe confirmado permite perseguir");
            a.Submit(new BrawlCommand { Move=Vector2.up, Basic=true }); yield return new WaitForSeconds(.19f); a.ClearInput();
            Check(b.Damage >= 8 && a.ComboHits >= 2, "Combo escalera conecta segundo golpe en el aire");
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output,"aerial-combo.png"));
            Place(a,new Vector3(0,5,0)); Place(b,new Vector3(1,4.2f,0));
            a.Submit(new BrawlCommand { Move=Vector2.down, Basic=true }); yield return new WaitForSeconds(.2f); a.ClearInput();
            Check(b.Damage >= 7 && b.Velocity.y < -5, "Ataque aéreo descendente golpea hacia abajo");
            Place(a,new Vector3(-2,.05f,0)); Place(b,new Vector3(0,.05f,0)); yield return new WaitForSeconds(.2f);
            a.Submit(new BrawlCommand { Aim=Vector2.right, Heavy=true }); yield return new WaitForSeconds(.5f); a.ClearInput();
            Check(b.Damage > 0 && b.Velocity.x > 4, "Fuerte horizontal lanza al rival lateralmente");
            Place(a,new Vector3(-4.8f,.05f,0)); Place(b,new Vector3(5,.05f,0)); yield return new WaitForSeconds(.2f);
            a.Submit(new BrawlCommand { Jump=true }); yield return new WaitForSeconds(.2f);
            a.Submit(new BrawlCommand { Jump=true }); yield return new WaitForSeconds(.4f);
            Check(a.transform.position.y > 3, "Doble salto atraviesa plataforma desde abajo");
            yield return new WaitForSeconds(1f);
            Check(a.Grounded && Mathf.Abs(a.transform.position.y-2.6f)<.15f, "Caída aterriza sobre plataforma elevada");
            a.Submit(new BrawlCommand { Move=Vector2.down, Jump=true }); yield return new WaitForSeconds(.45f); a.ClearInput();
            Check(a.transform.position.y < 2.2f, "Abajo y salto permiten bajar de la plataforma");
            yield return new WaitForSeconds(.5f);
            Check(a.Grounded && a.transform.position.y < .2f, "Tras bajar se recupera colisión con el suelo principal");
            Place(a,new Vector3(-2,.05f,0)); Place(b,new Vector3(1,.05f,0)); yield return new WaitForSeconds(.15f);
            a.Submit(new BrawlCommand { Special=true, Aim=Vector2.right }); yield return new WaitForSeconds(.8f); a.ClearInput();
            Check(b.Damage > 0 && Mathf.Abs(b.Velocity.z)<.001f, "Poder de Don Promesio conecta sin empuje en profundidad");
            Place(a,new Vector3(-2,.05f,0)); Place(b,new Vector3(1,.05f,0)); yield return new WaitForSeconds(.15f);
            a.SetMeterForTest(100); a.Submit(new BrawlCommand { Super=true, Aim=Vector2.right });
            yield return new WaitForSeconds(1.2f); a.ClearInput();
            Check(a.Meter == 0 && b.Damage > 0, "Super consume barra y conecta en 2.5D");
            match.SelectedCharacter=1; match.StartTraining(); yield return new WaitForSeconds(.2f);
            a=match.Fighters[0]; b=match.Fighters[1]; Place(a,new Vector3(-2,.05f,0)); Place(b,new Vector3(1,.05f,0));
            a.Submit(new BrawlCommand { Special=true, Aim=Vector2.right }); yield return new WaitForSeconds(.85f);
            Check(b.Damage > 0, "Poder de Lola Viral conecta en el plano lateral");
            for (int stage=0;stage<BrawlArena.Count;stage++)
            {
                match.ArenaIndex=stage; match.SelectedCharacter=0; match.OpponentCount=4; match.StartMatch();
                yield return new WaitForSeconds(3.3f);
                bool valid=match.Fighters.Length==5 && match.Arena.OneWayPlatforms.Length==2;
                foreach(var fighter in match.Fighters) valid &= fighter.Grounded && fighter.Stocks==3 && Mathf.Abs(fighter.transform.position.z)<.001f;
                Check(valid,"Arena " + stage + ": cinco participantes sobre el plano y dos plataformas atravesables");
                Check(Camera.main.orthographic && Vector3.Dot(Camera.main.transform.forward,Vector3.forward)>.999f,"Arena " + stage + ": cámara lateral sin inclinación");
                yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output,"stage-"+stage+".png"));
            }
            match.OpponentCount=1; match.StartMatch(); yield return new WaitForSeconds(3.2f);
            b=match.Fighters[1]; b.RespawnAt(new Vector3(0,21,0),false); yield return new WaitForSeconds(.1f);
            Check(b.Stocks==2 && b.IsRespawning,"Cruzar límite superior descuenta una vida");
            yield return new WaitForSeconds(2.1f);
            Check(!b.IsRespawning && b.Invulnerable && b.transform.position.z==0,"Reaparición protegida conserva plano lateral");
            float time=match.RemainingTime; match.TogglePause(); yield return new WaitForSeconds(.2f);
            Check(match.RemainingTime==time,"Pausa mantiene reloj"); match.TogglePause();
            match.ReturnToMenu();
            yield return BrawlDeviceChecks.Run(match,Check,output);
            match.ArenaIndex=0; match.OpponentCount=4; match.SelectedCharacter=0; match.StartMatch(); match.AutomatedTest=false;
            yield return new WaitForSeconds(9);
            float dealt=0; bool plane=true;
            foreach (var fighter in match.Fighters) { dealt+=fighter.TotalDamageDealt; plane &= Mathf.Abs(fighter.transform.position.z)<.001f; }
            Check(dealt>0 && plane && match.Fighters.Length==5,"Partida normal con cinco luchadores: bots combaten y conservan plano lateral");
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output,"gameplay.png"));
            match.AutomatedTest=true;
            yield return BrawlSidePowerChecks.Run(match,Check,output);
            yield return BrawlMoveChecks.Run(match,Check,output);
            yield return BrawlComboStudy.Run(match,Check,output);
            yield return BrawlKitMatrixChecks.Run(match,Check,output);
            yield return BrawlRecoveryChecks.Run(match,Check,output);
            yield return BrawlCameraChecks.Run(match,Check,output);
            // Controls, options, menus and display suites, run under the production 2.5D rules (GDD 2.0 §7, §10).
            yield return ControlChecks(skipDevices: true);
            Finish();
        }

        private IEnumerator ControlChecks(bool skipDevices = false)
        {
            if (!skipDevices) yield return BrawlDeviceChecks.Run(match, Check, output);
            yield return BrawlBindingChecks.Run(match, Check, output);
            yield return BrawlTuningChecks.Run(match, Check, output);
            yield return BrawlMenuChecks.Run(match, Check, output);
            yield return BrawlDisplayChecks.Run(match, Check, output);
        }
        private static void Place(BrawlFighter f,Vector3 p) => f.ResetPractice(p,0);
    }
}
