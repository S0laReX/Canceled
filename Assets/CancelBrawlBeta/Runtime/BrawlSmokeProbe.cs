using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>Standalone integration checks, enabled only by -beta-smoke. Never runs in normal play.</summary>
    public sealed class BrawlSmokeProbe : MonoBehaviour
    {
        [Serializable] private sealed class Report { public string version = BrawlMatch.Version; public bool passed; public List<string> checks = new List<string>(); public List<string> failures = new List<string>(); public List<string> notes = new List<string>(); }
        private readonly Report report = new Report();
        private BrawlMatch match;
        private float started;
        private bool finished;
        private string output;
        private void Awake()
        {
            match = GetComponent<BrawlMatch>(); started = Time.realtimeSinceStartup;
            report.version = match.DisplayVersion;
            string[] args = Environment.GetCommandLineArgs();
            int outputArg = Array.IndexOf(args, "-evidence-path");
            output = Path.GetFullPath(outputArg >= 0 && outputArg + 1 < args.Length ? args[outputArg + 1] : "Evidence");
            Directory.CreateDirectory(output);
            Application.logMessageReceived += OnLog;
            // Automated runs last minutes; switching windows meanwhile must not disable the virtual pads under test.
            // Normal play keeps the default (reset and disable devices when the game loses focus).
            UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
            BrawlDeviceChecks.IsolatePhysicalPads();
            report.notes.Add("Entrada configurada para ignorar el foco de ventana durante la prueba automática.");
        }
        private void OnDestroy() => Application.logMessageReceived -= OnLog;
        private void OnLog(string message, string stack, LogType type)
        { if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) report.failures.Add(message + "\n" + stack); }
        private void Update() { if (!finished && Time.realtimeSinceStartup - started > 300) { report.failures.Add("Tiempo máximo de la prueba agotado"); Finish(); } }
        private void Check(bool condition, string name)
        {
            if (condition) { report.checks.Add(name); Debug.Log("BETA_CHECK_PASS " + name); }
            else { report.failures.Add(name); Debug.LogWarning("BETA_CHECK_FAIL " + name); }
        }
        private IEnumerator Start()
        {
            yield return null;
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-controls-smoke") >= 0)
            {
                yield return BrawlDeviceChecks.Run(match, Check, output);
                yield return BrawlBindingChecks.Run(match, Check, output);
                yield return BrawlTuningChecks.Run(match, Check, output);
                yield return BrawlMenuChecks.Run(match, Check, output);
                yield return BrawlDisplayChecks.Run(match, Check, output);
                Finish(); yield break;
            }
            Check(match.Phase == MatchPhase.Menu, "Arranque al menú sin Editor");
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, "menu.png"));
            match.StartMatch();
            yield return new WaitForSeconds(3.2f);
            Check(match.Phase == MatchPhase.Playing && match.Fighters.Length == 2, "Cuenta atrás e inicio de duelo");
            BrawlFighter p1 = match.Fighters[0], p2 = match.Fighters[1];
            p1.RespawnAt(new Vector3(-6, .05f, -2), false); p2.RespawnAt(new Vector3(6, .05f, -2), false);
            float originalX = p1.transform.position.x;
            p1.Submit(new BrawlCommand { Move = Vector2.right });
            yield return new WaitForSeconds(.35f); p1.ClearInput();
            Check(p1.transform.position.x > originalX + 1.7f, "Movimiento 3D continuo en pasos físicos");
            p1.Submit(new BrawlCommand { Jump = true });
            yield return new WaitForSeconds(.15f); float firstHeight = p1.transform.position.y;
            p1.Submit(new BrawlCommand { Jump = true });
            yield return new WaitForSeconds(.2f);
            Check(p1.transform.position.y > firstHeight + .7f && p1.JumpsUsed == 2, "Segundo salto real en el aire");
            p1.Submit(new BrawlCommand { Recovery = true });
            yield return new WaitForSeconds(.05f);
            Check(!p1.RecoveryAvailable, "Recuperación aérea consume un uso");
            p1.Submit(new BrawlCommand { Recovery = true });
            yield return new WaitForSeconds(.05f);
            Check(!p1.RecoveryAvailable, "Recuperación no se regenera en el aire");
            p1.RespawnAt(new Vector3(-6, .05f, -2), false); p1.ClearInput();
            yield return new WaitForSeconds(.2f);
            Check(p1.Grounded && p1.RecoveryAvailable, "Aterrizaje restablece recursos");
            p1.RespawnAt(new Vector3(-5, .05f, -2), false); p2.RespawnAt(new Vector3(-5, .05f, -.55f), false);
            p1.Submit(new BrawlCommand { Aim = Vector2.up, Basic = true });
            yield return new WaitForSeconds(.45f); p1.ClearInput();
            Check(Mathf.Abs(p2.Damage - match.Settings.basicDamage) < .01f, "Básico conecta una vez por activación");
            Check(Mathf.Abs(p1.Meter - 8) < .01f, "Cadena conectada carga 8 puntos");
            p2.RespawnAt(new Vector3(-5, .05f, -.55f), false);
            var spec = match.Settings.Attack(AttackKind.Special, 0, 0);
            var token = new BrawlAttackToken(p1, spec);
            float before = p2.Damage;
            Check(token.Hit(p2, Vector3.forward) && !token.Hit(p2, Vector3.forward) && Mathf.Abs(p2.Damage - before - spec.Damage) < .01f, "Token impide daño duplicado al objetivo");
            for (int i = 0; i < 9; i++)
            { p2.RespawnAt(new Vector3(6, .05f, -2), false); new BrawlAttackToken(p1, spec).Hit(p2, Vector3.forward); }
            Check(p2.Damage >= 100 && p2.Stocks == 3, "Daño superior a 100 no elimina");
            Check(p1.Meter == 100, "Carga limitada a 100");
            p1.Submit(new BrawlCommand { Super = true }); yield return new WaitForSeconds(.1f);
            Check(p1.Meter == 0, "Super consume toda la carga al activar");
            yield return new WaitForSeconds(1.5f);
            p2.RespawnAt(new Vector3(6, .05f, -2), true);
            Check(!p2.ReceiveHit(spec, Vector3.forward, p1), "Protección de aparición bloquea impactos");
            p2.Submit(new BrawlCommand { Basic = true }); yield return new WaitForSeconds(.05f);
            Check(p2.ReceiveHit(spec, Vector3.forward, p1), "Atacar cancela protección de aparición");
            p2.RespawnAt(new Vector3(6, .05f, -2), false);
            p2.Submit(new BrawlCommand { Dodge = true, Move = Vector2.left });
            yield return new WaitForSeconds(.05f); p2.ClearInput();
            Check(!p2.ReceiveHit(spec, Vector3.forward, p1), "Ventana de esquiva evita impactos");
            yield return new WaitForSeconds(.3f);
            Check(p2.ReceiveHit(spec, Vector3.forward, p1), "Final de esquiva vuelve a ser vulnerable");
            p1.RespawnAt(new Vector3(-7.4f, .03f, .05f), false); p1.ClearInput();
            p2.RespawnAt(new Vector3(-7.4f, .03f, 4.6f), false); p2.ClearInput();
            float beforeWall = p2.Damage;
            p1.Submit(new BrawlCommand { Aim = Vector2.up, Special = true });
            yield return new WaitForSeconds(1.4f); p1.ClearInput();
            Check(Mathf.Abs(p2.Damage - beforeWall) < .01f, "Geometría sólida bloquea proyectiles");
            p2.RespawnAt(new Vector3(0, -10, 0), false);
            yield return new WaitForSeconds(.1f);
            Check(p2.Stocks == 2 && p2.IsRespawning && p2.Damage == 0 && p2.Meter == 0, "Expulsión descuenta vida y reinicia daño y carga");
            Check(p1.KOs == 1 && p2.SelfDestructs == 0, "Expulsión atribuida al último atacante dentro de 8 s");
            yield return new WaitForSeconds(2.05f);
            Check(!p2.IsRespawning && p2.Invulnerable, "Reaparición automática protegida tras dos segundos");
            match.TogglePause(); float clock = match.RemainingTime; Vector3 pausedPosition = p1.transform.position;
            yield return new WaitForSeconds(.3f);
            Check(match.RemainingTime == clock && p1.transform.position == pausedPosition, "Pausa local congela reloj y simulación");
            match.TogglePause();
            match.Settings.startingStocks = 1; match.OpponentCount = 1; match.StartMatch();
            yield return new WaitForSeconds(3.1f);
            match.Fighters[0].RespawnAt(new Vector3(0, -10, 0), false);
            match.Fighters[1].RespawnAt(new Vector3(0, -10, 0), false);
            yield return new WaitForSeconds(.1f);
            Check(match.Phase == MatchPhase.Results && match.Fighters[0].Place == 1 && match.Fighters[1].Place == 1, "Últimas vidas simultáneas comparten primer puesto");
            match.OpponentCount = 4; match.StartMatch();
            yield return new WaitForSeconds(3.1f);
            match.Fighters[3].RespawnAt(new Vector3(0, -10, 0), false);
            match.Fighters[4].RespawnAt(new Vector3(0, -10, 0), false);
            yield return new WaitForSeconds(.1f);
            Check(match.Fighters[3].Place == 4 && match.Fighters[4].Place == 4, "Dos eliminados de cinco comparten cuarto puesto");
            match.FinishByTime();
            Check(match.Fighters[0].Place == 1 && match.Fighters[1].Place == 1 && match.Fighters[2].Place == 1, "Desempate exacto por tiempo conserva puestos compartidos");
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, "results.png"));
            match.Settings.startingStocks = 3; match.Settings.matchSeconds = .2f; match.OpponentCount = 1; match.StartMatch();
            yield return new WaitForSeconds(3.4f);
            Check(match.Phase == MatchPhase.Results, "El reloj termina la partida automáticamente");
            match.Settings.matchSeconds = 300; match.ReturnToMenu();
            Check(match.Phase == MatchPhase.Menu && match.Fighters.Length == 0, "Regreso al menú limpia la partida");
            yield return KitChecks();
            yield return ArenaChecks();
            match.ArenaIndex = 0; match.SelectedCharacter = 0;
            match.OpponentCount = 4; match.AutomatedTest = false; match.StartMatch();
            yield return new WaitForSeconds(11);
            Check(match.Fighters.Length == 5 && match.RemainingTime < 295, "Revancha de cinco avanza con IA activa");
            bool fought = false;
            foreach (var fighter in match.Fighters) fought |= fighter.Damage > 0 || fighter.Stocks < 3 || fighter.TotalDamageDealt > 0;
            Check(fought, "Los bots navegan y conectan ataques en partida real");
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, "gameplay.png"));
            yield return new WaitForSeconds(.5f);
            yield return TrainingChecks();
            yield return BrawlDeviceChecks.Run(match, Check, output);
            yield return BrawlBindingChecks.Run(match, Check, output);
            yield return BrawlTuningChecks.Run(match, Check, output);
            yield return BrawlMenuChecks.Run(match, Check, output);
            yield return BrawlDisplayChecks.Run(match, Check, output);
            Finish();
        }

        private IEnumerator TrainingChecks()
        {
            match.AutomatedTest = true; match.ReturnToMenu();
            match.ArenaIndex = 0; match.SelectedCharacter = 0; match.OpponentCount = 4;
            match.StartTraining();
            yield return new WaitForSeconds(.3f);
            Check(match.IsTraining && match.Phase == MatchPhase.Playing && match.Fighters.Length == 2,
                "CombatLab inicia dos puestos sin alterar el número de rivales de versus");
            var a = match.Fighters[0]; var b = match.Fighters[1];
            Vector3 position = b.transform.position; float clock = match.RemainingTime;
            yield return new WaitForSeconds(.5f);
            Check((b.transform.position - position).magnitude < .03f && a.Damage == 0, "Objetivo inmóvil no genera entradas ni ataques");
            Check(match.RemainingTime == clock, "Entrenamiento no agota el reloj");
            Check(a.SkinName != "Provisional" && b.SkinName != "Provisional", "Laboratorio reutiliza modelos y aspectos originales");
            match.SetTrainingDamage(100);
            Check(b.Damage == 100 && b.Stocks == 3, "Daño ajustable a 100 sin perder stocks");
            match.SetTrainingDamage(999); Check(b.Damage == 300, "Daño de entrenamiento acotado a 300");
            match.SetTrainingDamage(-25); Check(b.Damage == 0, "Daño de entrenamiento no negativo");
            match.FillTrainingMeter(); Check(a.Meter == 100, "Entrenamiento permite practicar super sin grind");
            a.Submit(new BrawlCommand { Basic = true, Aim = match.WorldToCamera(Vector3.forward) });
            yield return new WaitForSeconds(.12f);
            Check(a.AttackIsActive && a.TryGetMeleeVolume(out float range, out float height, out float dot, out bool safe)
                && Mathf.Abs(range - (a.CurrentAttackSpec.Range + .3f)) < .001f && height == a.CurrentAttackSpec.Height,
                "Visualizador comparte alcance y tolerancia vertical del golpe real");
            a.ClearInput(); yield return new WaitForSeconds(.3f);
            Check(b.Damage == match.Settings.basicDamage, "Golpe en laboratorio conecta una sola vez sobre el objetivo");
            match.SetTrainingDamage(125); match.ResetTraining();
            yield return new WaitForSeconds(.2f);
            a = match.Fighters[0]; b = match.Fighters[1];
            Check(b.Damage == 125 && a.Meter == 0 && !a.IsAttacking, "Reinicio conserva daño inicial y limpia ataque y barra");
            bool stable = true;
            for (int i = 0; i < 100; i++)
            {
                a.RespawnAt(new Vector3(0, -10, 0), false); b.RespawnAt(new Vector3(0, -10, 0), false);
                match.ResolveRingOuts();
                stable &= a.Stocks == 3 && b.Stocks == 3 && b.Damage == 125 && match.Phase == MatchPhase.Playing;
            }
            Check(stable, "Cien caídas dobles en laboratorio recuperan ambos luchadores sin agotar stocks");
            match.TogglePause(); position = a.transform.position;
            yield return new WaitForSeconds(.2f);
            Check(match.Phase == MatchPhase.Paused && a.transform.position == position, "Pausa de entrenamiento congela simulación");
            match.TogglePause(); match.ShowCombatVolumes = false;
            yield return null;
            bool hidden = true;
            foreach (var volume in FindObjectsByType<BrawlCombatVolumes>())
                foreach (var line in volume.GetComponentsInChildren<LineRenderer>())
                    if (line.gameObject.name == "Volumen ofensivo" || line.gameObject.name == "Referencia del objetivo") hidden &= !line.enabled;
            Check(hidden, "Opción de volúmenes oculta las líneas de laboratorio");
            match.ShowCombatVolumes = true;
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, "combat-lab.png"));
            match.ReturnToMenu(); yield return null;
            Check(!match.IsTraining && FindObjectsByType<BrawlCombatVolumes>().Length == 0, "Salir del laboratorio limpia sus visualizadores");
            match.StartMatch(); yield return new WaitForSeconds(3.2f);
            Check(!match.IsTraining && match.Fighters.Length == 5 && match.RemainingTime < 300,
                "Volver a versus restaura cinco participantes y reloj normal");
        }
        private IEnumerator KitChecks()
        {
            match.ArenaIndex = 0; match.OpponentCount = 1; match.AutomatedTest = true;
            for (int k = 0; k < BrawlRoster.Count; k++)
            {
                BrawlKit kit = BrawlRoster.Get(k);
                match.SelectedCharacter = k; match.StartMatch();
                yield return new WaitForSeconds(3.1f);
                BrawlFighter a = match.Fighters[0], b = match.Fighters[1];
                Vector3 origin = new Vector3(-9, .05f, -2);
                float reach = kit.Special == SpecialStyle.Dash ? 2.4f : kit.Special == SpecialStyle.Emitter ? 4.5f : 4f;
                Place(a, origin); Place(b, origin + Vector3.right * reach);
                yield return new WaitForSeconds(.15f);
                float before = b.Damage;
                a.Submit(new BrawlCommand { Aim = Vector2.right, Special = true });
                if (kit.Special == SpecialStyle.Emitter)
                {
                    yield return new WaitForSeconds(.8f);
                    Check(a.ActiveTrap != null && a.ActiveTrap.Armed, kit.Name + ": emisor visible y armado tras preparación");
                    Place(b, origin + Vector3.right * 1.6f);
                    yield return new WaitForSeconds(.25f);
                    Check(b.Damage > before && b.Velocity.y > 3, kit.Name + ": " + kit.SpecialName + " lanza hacia arriba al primer rival");
                }
                else
                {
                    yield return new WaitForSeconds(.9f);
                    Check(b.Damage > before, kit.Name + ": " + kit.SpecialName + " conecta");
                }
                Check(Mathf.Abs(a.Meter - 12) < .01f, kit.Name + ": especial conectado carga 12 puntos");

                if (kit.Special == SpecialStyle.Emitter)
                {
                    Place(a, origin); Place(b, new Vector3(6, .05f, -2));
                    a.Submit(new BrawlCommand { Aim = Vector2.right, Special = true }); yield return new WaitForSeconds(.75f);
                    a.Submit(new BrawlCommand { Aim = Vector2.left, Special = true }); yield return new WaitForSeconds(.75f);
                    int owned = 0;
                    foreach (var trap in FindObjectsByType<BrawlTrap>()) if (trap.Owner == a) owned++;
                    Check(owned == 1, kit.Name + ": solo mantiene un emisor activo");
                }
                if (kit.Special == SpecialStyle.BounceToken)
                {
                    Place(a, new Vector3(-7.4f, .03f, -1f)); Place(b, new Vector3(6, .05f, -2));
                    a.Submit(new BrawlCommand { Aim = Vector2.up, Special = true });
                    yield return new WaitForSeconds(.55f);
                    bool bounced = false;
                    foreach (var projectile in FindObjectsByType<BrawlProjectile>()) bounced |= projectile.Source == a && projectile.BouncesLeft < 3;
                    Check(bounced, kit.Name + ": token rebota en geometría sólida con rebotes limitados");
                    yield return new WaitForSeconds(2f);
                }

                yield return SuperCheck(kit, a, b, origin, SuperTarget(kit, origin), true);
                if (kit.Super == SuperStyle.SafeSweep)
                    yield return SuperCheck(kit, a, b, origin, origin + new Vector3(0, 0, 3), false);
            }
        }

        private static Vector3 SuperTarget(BrawlKit kit, Vector3 origin)
        {
            switch (kit.Super)
            {
                case SuperStyle.Zone: return origin + Vector3.right * 3.6f;
                case SuperStyle.Bubble: return origin + Vector3.right * 4.2f;
                default: return origin + Vector3.right * 3f;
            }
        }

        private IEnumerator SuperCheck(BrawlKit kit, BrawlFighter a, BrawlFighter b, Vector3 origin, Vector3 target, bool expectHit)
        {
            Place(a, origin); Place(b, target);
            yield return new WaitForSeconds(.2f);
            a.SetMeterForTest(100);
            float before = b.Damage;
            a.Submit(new BrawlCommand { Aim = Vector2.right, Super = true });
            yield return new WaitForSeconds(kit.SuperWindup + kit.SuperActive + .35f);
            if (expectHit) Check(b.Damage > before && a.Meter == 0, kit.Name + ": super " + kit.SuperName + " conecta y no recarga la barra");
            else Check(Mathf.Abs(b.Damage - before) < .01f, kit.Name + ": sector seguro anunciado evita el super");
            yield return new WaitForSeconds(.6f);
        }

        private IEnumerator ArenaChecks()
        {
            for (int arena = 0; arena < BrawlArena.BetaCount; arena++)
            {
                match.AutomatedTest = true; match.ArenaIndex = arena; match.SelectedCharacter = arena; match.OpponentCount = 4;
                match.StartMatch();
                // New colliders join the physics scene on the next simulation step.
                yield return new WaitForFixedUpdate();
                Physics.SyncTransforms();
                string name = match.Arena.DisplayName;
                Vector3[] spawns = match.Arena.SpawnPoints;
                bool spawnsValid = spawns.Length >= 5;
                for (int i = 0; i < spawns.Length; i++)
                {
                    spawnsValid &= Physics.Raycast(spawns[i] + Vector3.up, Vector3.down, out RaycastHit hit, 1.8f, 1, QueryTriggerInteraction.Ignore) && spawns[i].y - hit.point.y < .3f;
                    spawnsValid &= Mathf.Abs(spawns[i].x) < match.Arena.OuterLimits.x - 4 && Mathf.Abs(spawns[i].z) < match.Arena.OuterLimits.y - 4;
                    for (int j = i + 1; j < spawns.Length; j++) spawnsValid &= (spawns[i] - spawns[j]).magnitude > 1.5f;
                }
                Check(spawnsValid, name + ": cinco o más apariciones separadas sobre suelo sólido");
                yield return new WaitForSeconds(3.5f);
                bool settled = true; var cast = new HashSet<int>();
                foreach (var fighter in match.Fighters) { settled &= fighter.Grounded && fighter.Stocks == match.Settings.startingStocks; cast.Add(fighter.CharacterIndex); }
                Check(settled, name + ": los cinco participantes aterrizan sin caer");
                Check(cast.Count == 5, name + ": partida de cinco muestra a los cinco personajes");
                if (arena == 0)
                {
                    bool modeled = true; var looks = new List<string>();
                    foreach (var fighter in match.Fighters) { modeled &= fighter.SkinName != "Provisional"; looks.Add(fighter.SkinName); }
                    Check(modeled, "Los cinco luchadores usan su modelo animado (" + string.Join(", ", looks) + ")");
                    var skinned = match.Fighters[0].GetComponentInChildren<SkinnedMeshRenderer>();
                    Check(skinned != null && skinned.bones.Length == BrawlAutoRig.BoneCount && skinned.sharedMesh.boneWeights.Length > 0,
                        "El modelo de P1 tiene esqueleto generado con pesos");
                }
                // The kiosk that hides a fighter only exists in the beta greybox of this court.
                if (arena == 1 && !match.Arena.UsesStageArt)
                {
                    Place(match.Fighters[0], new Vector3(0, .12f, 1.9f));
                    yield return new WaitForSeconds(.25f);
                    Check(match.GroupCamera.FadedCount > 0, name + ": la cámara atenúa el obstáculo que tapa a un luchador");
                }
                match.AutomatedTest = false;
                yield return new WaitForSeconds(9f);
                match.AutomatedTest = true;
                float dealt = 0; int selfFalls = 0, kos = 0;
                foreach (var fighter in match.Fighters) { dealt += fighter.TotalDamageDealt; selfFalls += fighter.SelfDestructs; kos += fighter.KOs; }
                Check(dealt > 0 && match.Phase != MatchPhase.Menu, name + ": la IA navega y combate durante 9 s");
                report.notes.Add($"{name}: IA 9 s → daño {dealt:0}, KO {kos}, caídas propias {selfFalls}");
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "arena-" + arena + ".png"));
                yield return null;
            }
            match.ReturnToMenu();
        }

        private static void Place(BrawlFighter fighter, Vector3 position) { fighter.RespawnAt(position, false); fighter.ClearInput(); }

        private void Finish()
        {
            if (finished) return;
            finished = true; report.passed = report.failures.Count == 0;
            File.WriteAllText(Path.Combine(output, "smoke-report.json"), JsonUtility.ToJson(report, true));
            Debug.Log("BETA_SMOKE_RESULT " + (report.passed ? "PASS" : "FAIL") + " checks=" + report.checks.Count + " failures=" + report.failures.Count);
            Application.Quit(report.passed ? 0 : 1);
        }
    }
}
