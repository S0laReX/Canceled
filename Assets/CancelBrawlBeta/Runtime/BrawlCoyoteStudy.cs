using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// Coyote-time measurement for Santiago's decision (GDD 2.0 §4): walk off the stage edge and press jump d ticks
    /// later, with coyote 0 / 3 / 6 ticks. Records whether the press still counted as the ground jump (air jump kept)
    /// and the take-off speed. Also checks the abuses that must never get coyote: after a jump, a hit or a platform drop.
    /// Restores the setting afterwards.
    /// </summary>
    public static class BrawlCoyoteStudy
    {
        [Serializable] public sealed class Row { public int coyoteTicks; public int pressDelayTicks; public bool groundJumpKept; public float takeoffSpeed; public int jumpsLeftAfter; }
        [Serializable] private sealed class Report { public string version = BrawlMatch.ProductionVersion; public List<Row> rows = new List<Row>(); public bool abuseAfterJump, abuseAfterHit, abuseAfterDrop, abuseAfterSpawn; }

        public static IEnumerator Run(BrawlMatch match, Action<bool, string> check, string output)
        {
            var settings = match.Settings;
            float saved = settings.coyoteSeconds;
            int oldCharacter = match.SelectedCharacter;
            var report = new Report();
            match.AutomatedTest = true;
            try
            {
                match.SelectedCharacter = 0; match.ArenaIndex = 0; match.StartTraining(); match.ShowCombatVolumes = false;
                yield return null;
                match.Fighters[1].ResetPractice(new Vector3(-6, .05f, 0), 0);
                float edge = 0;
                for (float x = 0; x < 30; x += .05f) { if (!Physics.Raycast(new Vector3(x, 1.5f, 0), Vector3.down, 3, 1, QueryTriggerInteraction.Ignore)) break; edge = x; }

                foreach (int coyote in new[] { 0, 3, 6 })
                {
                    settings.coyoteSeconds = coyote / 60f;
                    foreach (int delay in new[] { 0, 1, 2, 3, 4, 6, 8, 10 })
                    {
                        var row = new Row { coyoteTicks = coyote, pressDelayTicks = delay };
                        yield return WalkOffAndJump(match, edge, delay, row);
                        report.rows.Add(row);
                    }
                }

                // Abuses, with the most generous window.
                settings.coyoteSeconds = 6 / 60f;
                var f = match.Fighters[0];
                // (1) After a jump: leaving the ground by jumping must not refill the ground jump.
                f.ResetPractice(new Vector3(edge - 2, .05f, 0), 0); yield return new WaitForSeconds(.3f);
                f.Submit(new BrawlCommand { Jump = true }); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                f.Submit(new BrawlCommand { Jump = true }); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                report.abuseAfterJump = f.LastJumpWasCoyote || f.JumpsUsed < 2;
                // (2) After being launched by a hit.
                f.ResetPractice(new Vector3(edge - .3f, .05f, 0), 0); yield return new WaitForSeconds(.3f);
                f.ReceiveHit(new AttackSpec(AttackKind.Basic, 1, 0, .1f, .1f, 2, 6, 1, 1), Vector3.right, match.Fighters[1]);
                for (int i = 0; i < 40 && f.InHitstun; i++) yield return new WaitForFixedUpdate();
                f.Submit(new BrawlCommand { Jump = true }); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                report.abuseAfterHit = f.LastJumpWasCoyote;
                // (3) After dropping through a platform (down + jump while standing on it).
                f.ResetPractice(new Vector3(4.8f, 2.65f, 0), 0); yield return new WaitForSeconds(.4f);
                f.Submit(new BrawlCommand { Move = Vector2.down, Jump = true }); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                f.Submit(new BrawlCommand { Jump = true }); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                report.abuseAfterDrop = f.LastJumpWasCoyote;
                // (4) Appearing in the air (respawn / lab placement) is not walking off a ledge.
                f.ResetPractice(new Vector3(0, 5, 0), 0); yield return new WaitForFixedUpdate();
                f.Submit(new BrawlCommand { Jump = true }); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                report.abuseAfterSpawn = f.LastJumpWasCoyote;

                File.WriteAllText(Path.Combine(output, "coyote-study.json"), JsonUtility.ToJson(report, true));
                foreach (var row in report.rows)
                    Debug.Log($"FUNADOS_COYOTE coyote={row.coyoteTicks} retraso={row.pressDelayTicks} conserva={row.groundJumpKept} velocidad={row.takeoffSpeed:0.00} saltos_restantes={row.jumpsLeftAfter}");

                bool offMeansOff = report.rows.TrueForAll(r => r.coyoteTicks != 0 || !r.groundJumpKept);
                bool windowHonoured = report.rows.TrueForAll(r => r.coyoteTicks == 0 || r.groundJumpKept == (r.pressDelayTicks <= r.coyoteTicks));
                check(report.rows.Count == 24 && offMeansOff, "Coyote: con 0 no cambia nada (pulsar tarde gasta el salto aéreo)");
                check(windowHonoured, "Coyote: con 3 y 6 ticks la pulsación tardía cuenta como salto de suelo solo dentro de la ventana");
                check(!report.abuseAfterJump && !report.abuseAfterHit && !report.abuseAfterDrop && !report.abuseAfterSpawn, "Coyote: nunca tras un salto, un golpe, al bajar de una plataforma ni al aparecer en el aire");
                check(Mathf.Approximately(saved, .05f), "Coyote fijado en 3 ticks (0,05 s) por decisión de Santiago");
            }
            finally
            {
                settings.coyoteSeconds = saved;
                match.ReturnToMenu(); match.SelectedCharacter = oldCharacter; match.AutomatedTest = true;
            }
        }

        private static IEnumerator WalkOffAndJump(BrawlMatch match, float edge, int delay, Row row)
        {
            var f = match.Fighters[0];
            f.ResetPractice(new Vector3(edge - 1.2f, .05f, 0), 0); f.ClearInput();
            yield return new WaitForSeconds(.3f);
            int guard = 0;
            while (f.Grounded && guard++ < 120) { f.Submit(new BrawlCommand { Move = Vector2.right }); yield return new WaitForFixedUpdate(); }
            // Count ticks since the first airborne tick, still walking right.
            for (int t = 1; t < delay; t++) { f.Submit(new BrawlCommand { Move = Vector2.right }); yield return new WaitForFixedUpdate(); }
            f.Submit(new BrawlCommand { Move = Vector2.right, Jump = true });
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            row.takeoffSpeed = f.Velocity.y;
            row.groundJumpKept = f.JumpsUsed == 1;
            row.jumpsLeftAfter = 2 - f.JumpsUsed;
            f.ClearInput();
        }
    }
}
