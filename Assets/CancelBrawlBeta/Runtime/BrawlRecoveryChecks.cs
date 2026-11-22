using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// SYS-030–035 (+ ARENA-357) in 2.5D CombatLab: air resources, restoring them only on valid ground (not walls or
    /// ceilings), one recovery per air phase, fast moves without tunnelling, the landing marker, and a measured table of
    /// how far each kit returns from both stage edges. Temporary test geometry is removed afterwards.
    /// </summary>
    public static class BrawlRecoveryChecks
    {
        [Serializable] public sealed class Reach { public string kit; public float rightLevel, leftLevel, rightBelow, leftBelow; }
        [Serializable] private sealed class Report { public string version = BrawlMatch.ProductionVersion; public float edgeLeft, edgeRight; public List<Reach> kits = new List<Reach>(); }

        public static IEnumerator Run(BrawlMatch match, Action<bool, string> check, string output)
        {
            int oldCharacter = match.SelectedCharacter;
            var temporary = new List<GameObject>();
            match.AutomatedTest = true;
            try
            {
                yield return Lab(match, 0);
                var f = match.Fighters[0]; var s = match.Settings;

                // ── SYS-030: jumps, fast fall and air resources ──
                Place(match, f, new Vector3(0, .05f, 0)); yield return new WaitForSeconds(.3f);
                f.Submit(new BrawlCommand { Jump = true }); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                float first = f.Velocity.y;
                yield return new WaitForSeconds(.15f);
                f.Submit(new BrawlCommand { Jump = true }); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                float second = f.Velocity.y;
                int afterTwo = f.JumpsUsed;
                yield return new WaitForSeconds(.1f);
                float beforeThird = f.Velocity.y;
                f.Submit(new BrawlCommand { Jump = true }); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                check(Mathf.Abs(first - s.jumpSpeed) < 1 && Mathf.Abs(second - s.jumpSpeed * .94f) < 1 && afterTwo == 2 && f.Velocity.y <= beforeThird + .01f,
                    "SYS-030: salto inicial, segundo salto al 94 % y sin tercer salto");
                Place(match, f, new Vector3(-2, 7, 0)); yield return new WaitForSeconds(.25f);
                float normalFall = f.Velocity.y;
                Place(match, f, new Vector3(-2, 7, 0));
                for (float t = 0; t < .25f; t += Time.fixedDeltaTime) { f.Submit(new BrawlCommand { Move = Vector2.down }); yield return new WaitForFixedUpdate(); }
                check(f.Velocity.y < normalFall * 1.6f, "SYS-030: abajo en el aire acelera la caída");
                Place(match, f, new Vector3(0, 6, 0)); yield return new WaitForFixedUpdate();
                f.Submit(new BrawlCommand { Recovery = true, Move = Vector2.right }); yield return new WaitForSeconds(.1f);
                f.Submit(new BrawlCommand { Dodge = true }); yield return new WaitForSeconds(.4f);
                check(!f.RecoveryAvailable && !f.AirDodgeAvailable && f.JumpsUsed >= 1, "SYS-030: recuperación y esquiva aérea se consumen una vez");

                // ── SYS-031: restore only on valid ground ──
                yield return new WaitForSeconds(1.5f);
                check(f.Grounded && f.RecoveryAvailable && f.AirDodgeAvailable && f.JumpsUsed == 0, "SYS-031: aterrizar en suelo válido restaura saltos, recuperación y esquiva");
                var wall = Block(temporary, "Pared de prueba", new Vector3(3.2f, 9, 0), new Vector3(.6f, 20, 3));
                Place(match, f, new Vector3(1.6f, 6, 0)); yield return new WaitForFixedUpdate();
                f.Submit(new BrawlCommand { Jump = true }); yield return new WaitForSeconds(.1f);
                f.Submit(new BrawlCommand { Jump = true, Recovery = true, Move = Vector2.right });
                for (float t = 0; t < .5f; t += Time.fixedDeltaTime) { f.Submit(new BrawlCommand { Move = Vector2.right }); yield return new WaitForFixedUpdate(); }
                bool touchedWall = f.transform.position.x > 2.3f && f.transform.position.x < 3.0f;
                check(touchedWall && !f.Grounded && !f.RecoveryAvailable && f.JumpsUsed == 2, "SYS-031: rozar una pared no devuelve saltos ni recuperación");
                UnityEngine.Object.Destroy(wall); temporary.Remove(wall);
                var ceiling = Block(temporary, "Techo de prueba", new Vector3(-4, 3.4f, 0), new Vector3(3, .4f, 3));
                Place(match, f, new Vector3(-4, .05f, 0)); yield return new WaitForSeconds(.3f);
                f.Submit(new BrawlCommand { Jump = true }); yield return new WaitForSeconds(.4f);
                bool bumped = f.transform.position.y < 2.4f && f.JumpsUsed == 1 && !f.Grounded;
                f.Submit(new BrawlCommand { Jump = true }); yield return new WaitForSeconds(.05f);
                check(bumped && f.JumpsUsed == 2, "SYS-031: tocar un techo frena la subida y no devuelve el salto");
                UnityEngine.Object.Destroy(ceiling); temporary.Remove(ceiling);

                // ── SYS-032: one recovery per air phase, vulnerable, with a defined end ──
                Place(match, f, new Vector3(0, 6, 0)); yield return new WaitForFixedUpdate();
                f.Submit(new BrawlCommand { Recovery = true, Move = Vector2.right }); yield return new WaitForSeconds(.1f);
                bool recovering = f.Recovering;
                bool vulnerable = !f.Invulnerable;
                yield return new WaitForSeconds(.7f);
                bool ended = !f.Recovering && !f.RecoveryAvailable;
                float yBefore = f.transform.position.y;
                f.Submit(new BrawlCommand { Recovery = true }); yield return new WaitForSeconds(.2f);
                check(recovering && vulnerable && ended && f.transform.position.y < yBefore, "SYS-032: una recuperación por fase aérea, vulnerable y con final; la segunda no hace nada");

                // ── SYS-033: fast moves do not tunnel ──
                Place(match, f, new Vector3(-1, 14, 0));
                for (float t = 0; t < 1.5f && !f.Grounded; t += Time.fixedDeltaTime) { f.Submit(new BrawlCommand { Move = Vector2.down }); yield return new WaitForFixedUpdate(); }
                check(f.Grounded && f.transform.position.y > -.3f, "SYS-033: caída rápida a máxima velocidad aterriza sin atravesar el suelo");
                Place(match, f, new Vector3(4.8f, 9, 0));
                for (float t = 0; t < 1.5f && !f.Grounded; t += Time.fixedDeltaTime) { f.Submit(new BrawlCommand { Move = new Vector2(0, -1) }); yield return new WaitForFixedUpdate(); }
                // Holding down onto a platform is fast fall, not a drop (dropping needs down + jump while standing).
                check(f.Grounded && f.transform.position.y > 2.4f, "SYS-033: caída rápida aterriza sobre la plataforma atravesable");
                var dodgeWall = Block(temporary, "Pared de esquiva", new Vector3(-2.6f, 1.5f, 0), new Vector3(.4f, 3, 3));
                Place(match, f, new Vector3(-1, .05f, 0)); yield return new WaitForSeconds(.3f);
                f.Submit(new BrawlCommand { Dodge = true, Move = Vector2.left }); yield return new WaitForSeconds(.4f);
                check(f.transform.position.x > -2.4f, "SYS-033: la esquiva rápida se detiene en una pared sin atravesarla");
                UnityEngine.Object.Destroy(dodgeWall); temporary.Remove(dodgeWall);

                // ── SYS-034: landing marker ──
                Place(match, f, new Vector3(4.8f, 6, 0)); yield return new WaitForFixedUpdate(); yield return null;
                bool onPlatform = f.LandingMarker.gameObject.activeSelf && Mathf.Abs(f.LandingMarker.position.y - 2.75f) < .2f;
                Place(match, f, new Vector3(0, 6, 0)); yield return new WaitForFixedUpdate(); yield return null;
                bool onFloor = f.LandingMarker.gameObject.activeSelf && f.LandingMarker.position.y < .3f && Mathf.Abs(f.LandingMarker.position.x) < .1f;
                check(onPlatform && onFloor, "SYS-034: el indicador de apoyo marca la superficie bajo el luchador (plataforma o suelo)");
                yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, "landing-marker.png"));

                // ── SYS-035 / ARENA-357: measured return from both edges, every kit ──
                var report = new Report();
                FindEdges(out report.edgeLeft, out report.edgeRight);
                Place(match, f, new Vector3(report.edgeRight - 1, .05f, 0)); yield return new WaitForSeconds(.3f);
                // Short walk: long enough to leave the floor, short enough not to hit the lower KO limit and be reset.
                for (float t = 0; t < .35f; t += Time.fixedDeltaTime) { f.Submit(new BrawlCommand { Move = Vector2.right }); yield return new WaitForFixedUpdate(); }
                check(f.transform.position.x > report.edgeRight + .3f && !f.Grounded, "SYS-035: el borde no tiene pared invisible; caminar fuera hace caer");
                for (int kit = 0; kit < BrawlRoster.Count; kit++)
                {
                    yield return Lab(match, kit);
                    var reach = new Reach { kit = match.CharacterName(kit) };
                    var result = new float[1];
                    yield return MaxReturn(match, kit, report.edgeRight, 1, 0, result); reach.rightLevel = result[0];
                    yield return MaxReturn(match, kit, report.edgeLeft, -1, 0, result); reach.leftLevel = result[0];
                    yield return MaxReturn(match, kit, report.edgeRight, 1, -2, result); reach.rightBelow = result[0];
                    yield return MaxReturn(match, kit, report.edgeLeft, -1, -2, result); reach.leftBelow = result[0];
                    report.kits.Add(reach);
                    Debug.Log($"FUNADOS_REACH {reach.kit}: a nivel der {reach.rightLevel:0.0} izq {reach.leftLevel:0.0} · 2 m abajo der {reach.rightBelow:0.0} izq {reach.leftBelow:0.0}");
                }
                File.WriteAllText(Path.Combine(output, "recovery-reach.json"), JsonUtility.ToJson(report, true));
                bool everyone = report.kits.TrueForAll(r => r.rightLevel >= 1.5f && r.leftLevel >= 1.5f);
                bool mirrored = report.kits.TrueForAll(r => Mathf.Abs(r.rightLevel - r.leftLevel) <= .5f && Mathf.Abs(r.rightBelow - r.leftBelow) <= .5f);
                bool risky = report.kits.TrueForAll(r => r.rightLevel < 12);
                check(everyone, "SYS-035: los cinco kits vuelven al menos desde 1,5 m fuera de cada borde");
                check(mirrored, "SYS-035: el alcance de vuelta es simétrico entre borde izquierdo y derecho");
                check(risky, "SYS-035: caer sigue siendo un riesgo; ningún kit vuelve desde cualquier distancia");
            }
            finally
            {
                foreach (var go in temporary) if (go) UnityEngine.Object.Destroy(go);
                match.ReturnToMenu(); match.SelectedCharacter = oldCharacter; match.AutomatedTest = true;
            }
        }

        private static IEnumerator Lab(BrawlMatch match, int kit)
        {
            match.SelectedCharacter = kit; match.ArenaIndex = 0; match.StartTraining(); match.ShowCombatVolumes = false;
            yield return null;
            match.Fighters[1].ResetPractice(new Vector3(-6, .05f, 0), 0);
            yield return new WaitForSeconds(.2f);
        }

        private static void Place(BrawlMatch match, BrawlFighter f, Vector3 p) { f.ResetPractice(p, 0); f.ClearInput(); }

        private static GameObject Block(List<GameObject> list, string name, Vector3 centre, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.position = centre; go.transform.localScale = size; go.layer = 0;
            list.Add(go);
            return go;
        }

        /// <summary>Main floor extent along X, measured by rays below the raised platforms.</summary>
        private static void FindEdges(out float left, out float right)
        {
            left = right = 0;
            for (float x = 0; x < 30; x += .05f) { if (!Physics.Raycast(new Vector3(x, 1.5f, 0), Vector3.down, out var hit, 3, 1, QueryTriggerInteraction.Ignore)) break; right = x; }
            for (float x = 0; x > -30; x -= .05f) { if (!Physics.Raycast(new Vector3(x, 1.5f, 0), Vector3.down, out var hit, 3, 1, QueryTriggerInteraction.Ignore)) break; left = x; }
        }

        /// <summary>Binary search of the farthest start (beyond the edge, at height y) from which a player-like return lands on stage.</summary>
        private static IEnumerator MaxReturn(BrawlMatch match, int kit, float edge, float side, float y, float[] result)
        {
            float lo = 0, hi = 12;
            for (int step = 0; step < 6; step++)
            {
                float d = (lo + hi) * .5f;
                var ok = new bool[1];
                yield return TryReturn(match, edge + side * d, y, -side, edge, ok);
                if (ok[0]) lo = d; else hi = d;
            }
            result[0] = lo;
        }

        private static IEnumerator TryReturn(BrawlMatch match, float x, float y, float towardStage, float edge, bool[] ok)
        {
            var f = match.Fighters[0];
            f.ResetPractice(new Vector3(x, y, 0), 37); f.ClearInput();
            yield return new WaitForFixedUpdate();
            bool jumped = false, recovered = false;
            for (float t = 0; t < 4.5f; t += Time.fixedDeltaTime)
            {
                var command = new BrawlCommand { Move = new Vector2(towardStage, 0) };
                if (!jumped) { command.Jump = true; jumped = true; }
                else if (!recovered && f.Velocity.y < 0) { command.Recovery = true; recovered = true; }
                f.Submit(command);
                yield return new WaitForFixedUpdate();
                if (f.Grounded || f.IsRespawning || Mathf.Abs(f.Damage - 37) > .01f) break;
            }
            ok[0] = f.Grounded && Mathf.Abs(f.Damage - 37) < .01f && Mathf.Abs(f.transform.position.x) <= Mathf.Abs(edge) + .05f;
            f.ClearInput();
        }
    }
}
