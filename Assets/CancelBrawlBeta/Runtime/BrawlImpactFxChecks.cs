using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// <c>-fx-smoke</c>: impact, finisher, KO and super effects in full and reduced mode (GDD 2.0 §11 and §12).
    /// Checks tiers, the shapes reduced mode keeps, the flash and bloom it removes, depth behind the fighter,
    /// pause, cleanup and the pool limit, and saves one capture per mode.
    /// </summary>
    public static class BrawlImpactFxChecks
    {
        [Serializable]
        private sealed class ModeRow
        {
            public string mode, tier; public int sparks, rings, flashes, rays; public float peakBloom;
        }
        [Serializable]
        private sealed class Report
        {
            public bool particleShader, postProcessing; public float depthOffset;
            public List<ModeRow> rows = new List<ModeRow>();
            public int poolAlive, poolObjectsBefore, poolObjectsAfter; public float burstFrameMs;
        }

        private static IEnumerator Setup(BrawlMatch match, int character, float targetX)
        {
            match.SelectedCharacter = character; match.ArenaIndex = 0; match.StartTraining();
            match.Fighters[0].ResetPractice(new Vector3(0, .05f, 0), 0);
            match.Fighters[1].ResetPractice(new Vector3(targetX, .05f, 0), 0);
            yield return new WaitForSeconds(.2f);
        }

        private static ModeRow Row(BrawlImpactFx fx, string mode, string tier) => new ModeRow
        {
            mode = mode, tier = tier, sparks = fx.SparksEmitted, rings = fx.RingsEmitted, flashes = fx.FlashesEmitted,
            rays = fx.RaysEmitted, peakBloom = fx.PeakBloom
        };

        private static int SceneObjects() => UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).Length;

        /// <summary>Captures a KO-tier and a strong effect on the frame they appear (they last about 0.3 s).</summary>
        private static IEnumerator Capture(BrawlImpactFx fx, BrawlFighter target, string output, string name)
        {
            Vector3 at = target.transform.position + Vector3.up * .9f;
            fx.Impact(at, target.PlayerColor, ImpactTier.Ko, new Vector3(1, .6f, 0));
            yield return null;
            fx.Impact(at + Vector3.left * 2.2f, Color.cyan, ImpactTier.Strong, Vector3.left);
            yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, name));
            yield return new WaitForSeconds(.5f);
        }

        public static IEnumerator Run(BrawlMatch match, Action<bool, string> check, string output)
        {
            match.AutomatedTest = true;
            bool oldReduced = BrawlFeedback.ReducedEffects;
            var report = new Report { depthOffset = BrawlImpactFx.DepthOffset };
            var fx = BrawlImpactFx.Instance;
            check(fx != null, "El sistema de efectos existe junto a la partida");
            if (fx == null) yield break;
            report.particleShader = fx.UsesParticleShader;
            check(fx.UsesParticleShader, "La build incluye el shader de partículas de URP");
            var cameraData = Camera.main ? Camera.main.GetUniversalAdditionalCameraData() : null;
            report.postProcessing = cameraData && cameraData.renderPostProcessing;
            check(report.postProcessing && fx.BloomWeight == 0, "Posproceso activo con el bloom en reposo (peso 0)");

            // Tier rules, deterministic.
            var basic = new AttackSpec(AttackKind.Basic, 3, 0, .1f, .1f, 2, 1, 1, 1);
            var heavy = new AttackSpec(AttackKind.Heavy, 9, 0, .1f, .1f, 2, 4, 3, 1);
            var hitsBasic = new List<PendingHit> { new PendingHit(null, null, basic, Vector3.right) };
            var hitsHeavy = new List<PendingHit> { new PendingHit(null, null, heavy, Vector3.right) };
            check(BrawlFighter.ImpactTierFor(hitsBasic, new BrawlFighter.HitOutcome { Velocity = new Vector3(3, 2, 0) }, .1f) == ImpactTier.Light,
                "Básico flojo se lee como impacto ligero");
            check(BrawlFighter.ImpactTierFor(hitsHeavy, new BrawlFighter.HitOutcome { Velocity = new Vector3(3, 2, 0) }, .1f) == ImpactTier.Strong,
                "Fuerte o especial se lee como impacto fuerte");
            check(BrawlFighter.ImpactTierFor(hitsBasic, new BrawlFighter.HitOutcome { Velocity = new Vector3(3, 2, 0) }, .6f) == ImpactTier.Strong,
                "Empuje alto se lee como impacto fuerte aunque sea básico");
            check(BrawlFighter.ImpactTierFor(hitsHeavy, new BrawlFighter.HitOutcome { Velocity = new Vector3(1, -6, 0) }, .6f) == ImpactTier.Spike,
                "Lanzamiento hacia abajo se lee como remate");

            ModeRow full = null, reduced = null;
            foreach (bool reducedMode in new[] { false, true })
            {
                string mode = reducedMode ? "reducidos" : "completos";
                BrawlFeedback.ReducedEffects = reducedMode;

                // Real hits through ApplyHits.
                yield return Setup(match, 0, 1.5f);
                var target = match.Fighters[1];
                fx.ResetStats();
                target.ReceiveHit(heavy, Vector3.right, match.Fighters[0]);
                yield return null;
                var strong = Row(fx, mode, fx.LastTier.ToString()); report.rows.Add(strong);
                check(fx.LastTier == ImpactTier.Strong && strong.rings == 1, $"Efectos {mode}: golpe fuerte real usa su nivel y conserva el anillo");
                check(Mathf.Abs(fx.LastPosition.z - (target.transform.position.z + BrawlImpactFx.DepthOffset)) < .01f && Camera.main.transform.forward.z > .9f,
                    $"Efectos {mode}: el impacto queda detrás del luchador visto desde la cámara");
                if (reducedMode) reduced = strong; else full = strong;
                if (!reducedMode)
                {
                    check(strong.flashes == 1 && strong.peakBloom > 0, "Efectos completos: destello y pulso de bloom en golpe fuerte");
                    yield return null;
                    check(fx.BloomWeight > 0, "Efectos completos: el volumen de bloom sube tras el golpe");
                    yield return new WaitForSeconds(.4f);
                    check(fx.BloomWeight == 0, "Efectos completos: el bloom vuelve a 0 en menos de 0,4 s");
                    yield return Capture(fx, target, output, "fx-full.png");
                }
                else
                {
                    check(strong.flashes == 0 && strong.peakBloom == 0 && fx.BloomWeight == 0, "Efectos reducidos: sin destello ni bloom");
                    yield return Capture(fx, target, output, "fx-reduced.png");
                }

                // Finisher (downward) and light hit, through the presentation call.
                fx.ResetStats(); fx.Impact(target.transform.position + Vector3.up, Color.red, ImpactTier.Spike, Vector3.down);
                var spike = Row(fx, mode, "Spike"); report.rows.Add(spike);
                check(spike.rays >= 1 && spike.rings == 1, $"Efectos {mode}: el remate marca su dirección con estelas y anillo");
                fx.ResetStats(); fx.Impact(target.transform.position + Vector3.up, Color.red, ImpactTier.Light, Vector3.right);
                report.rows.Add(Row(fx, mode, "Light"));

                // Super activation through the fighter.
                yield return Setup(match, 0, 3);
                fx.ResetStats();
                match.Fighters[0].SetMeterForTest(100);
                match.Fighters[0].Submit(new BrawlCommand { Super = true, Aim = Vector2.right });
                float until = Time.time + 2; while (fx.RaysEmitted == 0 && Time.time < until) yield return null;
                var super = Row(fx, mode, "Super"); report.rows.Add(super);
                check(super.rays > 0 && super.rings > 0, $"Efectos {mode}: el super muestra rayos y anillo al activarse");
                if (reducedMode) check(super.flashes == 0 && super.peakBloom == 0 && super.rays <= 4, "Efectos reducidos: super sin destello ni bloom y con menos rayos");
                else check(super.flashes > 0 && super.peakBloom >= 1, "Efectos completos: super con destello y bloom máximo");

                // KO: the burst stays inside the visible play area.
                yield return Setup(match, 0, 3);
                fx.ResetStats();
                var faller = match.Fighters[1];
                faller.ResetPractice(new Vector3(40, 9, 0), 50);
                faller.LoseStock();
                Rect bounds = match.Arena.PlayBounds;
                var ko = Row(fx, mode, "Ko"); report.rows.Add(ko);
                check(fx.LastTier == ImpactTier.Ko && bounds.Contains(new Vector2(fx.LastPosition.x, fx.LastPosition.y)) && ko.rays >= 1,
                    $"Efectos {mode}: el KO se ve dentro del área de juego y apunta por dónde salió");
            }
            if (full != null && reduced != null)
                check(reduced.sparks * 3 <= full.sparks + 2 && reduced.rings == full.rings,
                    $"Efectos reducidos: un tercio de chispas ({reduced.sparks} de {full.sparks}) y las mismas formas");

            // Pause freezes particles; resuming continues; returning to the menu clears them.
            BrawlFeedback.ReducedEffects = false;
            yield return Setup(match, 0, 1.5f);
            fx.Impact(match.Fighters[1].transform.position + Vector3.up, Color.yellow, ImpactTier.Ko, Vector3.right);
            yield return null;
            match.TogglePause();
            yield return null;
            int frozen = fx.AliveParticles;
            yield return new WaitForSecondsRealtime(.5f);
            check(match.Phase == MatchPhase.Paused && fx.IsPaused && frozen > 0 && fx.AliveParticles == frozen, "La pausa congela las partículas");
            match.TogglePause();
            yield return new WaitForSeconds(.6f);
            check(!fx.IsPaused && fx.AliveParticles < frozen, "Al reanudar las partículas siguen y se apagan");

            // Pool: a storm of effects adds no scene objects and stays under the cap.
            report.poolObjectsBefore = SceneObjects();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 40; i++)
            {
                fx.Impact(new Vector3(i % 8 - 4, 2, 0), Color.cyan, (ImpactTier)(i % 5), Vector3.right);
                fx.Super(new Vector3(4 - i % 8, 2, 0), Color.magenta);
                BrawlFeedback.Burst(new Vector3(0, 1, 0), Color.white, 9);
            }
            watch.Stop(); report.burstFrameMs = (float)watch.Elapsed.TotalMilliseconds;
            yield return null;
            report.poolAlive = fx.AliveParticles; report.poolObjectsAfter = SceneObjects();
            check(report.poolObjectsAfter == report.poolObjectsBefore, $"120 efectos seguidos no crean objetos ({report.poolObjectsBefore} → {report.poolObjectsAfter})");
            check(report.poolAlive <= 600 + 64 + 32 + 96 + 300, $"Las partículas vivas respetan el tope del grupo ({report.poolAlive})");
            check(report.burstFrameMs < 8, $"Emitir 120 efectos cuesta menos de 8 ms ({report.burstFrameMs:0.00} ms)");

            match.ReturnToMenu(); yield return null;
            check(fx.AliveParticles == 0 && fx.BloomWeight == 0, "Volver al menú borra partículas y bloom");
            check(UnityEngine.Object.FindObjectsByType<BrawlTransient>().Length == 0 && GameObject.Find("Impacto") == null,
                "No quedan chispas antiguas como objetos sueltos");

            BrawlFeedback.ReducedEffects = oldReduced;
            File.WriteAllText(Path.Combine(output, "fx-smoke.json"), JsonUtility.ToJson(report, true));
        }
    }
}
