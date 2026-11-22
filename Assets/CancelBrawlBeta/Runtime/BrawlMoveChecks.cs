using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// Explicit smoke-test path only (2.5D): moves and kits as validated data (SYS-011) and X/Y hit boxes against
    /// hurtboxes, in height, at the stage edge and mirrored (SYS-042–048). Restores the data it edits.
    /// </summary>
    public static class BrawlMoveChecks
    {
        public static IEnumerator Run(BrawlMatch match, Action<bool, string> check, string output)
        {
            var settings = match.Settings;
            var savedMoves = (MoveDef[])settings.sideMoves.Clone();
            int oldCharacter = match.SelectedCharacter;
            try
            {
                match.AutomatedTest = true; match.ReturnToMenu();

                // ── SYS-011: data and validation ──
                check(BrawlMoveData.Validate(settings).Count == 0, "Datos de golpes y kits válidos por defecto");
                var up = settings.SideMoveDef(SideMove.UpBasic); var down = settings.SideMoveDef(SideMove.AirDownBasic); var air = settings.SideMoveDef(SideMove.AirNeutral);
                check(up.damage == 4 && Mathf.Approximately(up.lift, 8.8f) && down.damage == 7 && Mathf.Approximately(down.lift, -11) && air.damage == 5 &&
                      settings.SideMoveDef(SideMove.UpHeavy).damage == 10 && settings.SideMoveDef(SideMove.AirDownHeavy).damage == 12,
                    "Valores por defecto iguales a la tabla del GDD 2.0");
                bool kitsMatch = settings.kitTuning.Length == BrawlRoster.Count;
                for (int i = 0; kitsMatch && i < BrawlRoster.Count; i++)
                    kitsMatch = settings.kitTuning[i].kit == BrawlRoster.Kits[i].Name && Mathf.Approximately(settings.kitTuning[i].resistance, BrawlRoster.Kits[i].Resistance);
                check(kitsMatch, "Ajustes de los cinco kits presentes y aplicados al roster");

                var broken = ScriptableObject.CreateInstance<BrawlSettings>();
                broken.sideMoves = new[] { up, up, air };
                broken.sideMoves[2].hitbox = new MoveBox(0, 0, 0, 0);
                broken.kitTuning = BrawlMoveData.DefaultKits();
                broken.kitTuning[1].resistance = 9;
                var problems = BrawlMoveData.Validate(broken);
                check(problems.Exists(p => p.Contains("repetido")) && problems.Exists(p => p.Contains("Falta")) &&
                      problems.Exists(p => p.Contains("vacía")) && problems.Exists(p => p.Contains("resistencia")), "Validación detecta repetidos, faltantes, cajas vacías y kits fuera de rango");
                float before = BrawlRoster.Kits[1].Resistance;
                broken.ApplyKitTuning();
                check(Mathf.Approximately(BrawlRoster.Kits[1].Resistance, before), "Datos no válidos no se aplican: el kit conserva sus valores");
                UnityEngine.Object.Destroy(broken);

                // ── SYS-042–048: boxes reproduce the historical rule, mirror and height ──
                bool equivalent = true, mirrored = true;
                foreach (var move in settings.sideMoves)
                {
                    bool upward = move.id == SideMove.UpBasic || move.id == SideMove.UpHeavy;
                    bool downward = move.id == SideMove.AirDownBasic || move.id == SideMove.AirDownHeavy;
                    for (float dx = -4; dx <= 4.001f; dx += .25f)
                        for (float dy = -4; dy <= 4.001f; dy += .25f)
                        {
                            bool old = Mathf.Abs(dx) <= move.range + .3f && Mathf.Abs(dy) <= move.height &&
                                       (upward || downward || dx * dx <= .2f || dx > 0) &&
                                       !(upward && dy < -.5f) && !(downward && dy > .5f);
                            bool now = BrawlMoveData.Overlaps(move.hitbox, Vector2.zero, 1, new Vector2(dx, dy));
                            equivalent &= old == now;
                            mirrored &= now == BrawlMoveData.Overlaps(move.hitbox, new Vector2(3, 1), -1, new Vector2(3 - dx, 1 + dy));
                        }
                }
                check(equivalent, "Cajas por defecto reproducen exactamente el alcance anterior (cuadrícula de 1.089 puntos por golpe)");
                check(mirrored, "Cajas en espejo: mirar a la izquierda equivale a mirar a la derecha reflejado");
                check(BrawlMoveData.Overlaps(up.hitbox, Vector2.zero, 1, new Vector2(.5f, 2)) && !BrawlMoveData.Overlaps(up.hitbox, Vector2.zero, 1, new Vector2(.5f, -1)) &&
                      BrawlMoveData.Overlaps(down.hitbox, Vector2.zero, 1, new Vector2(.3f, -1.5f)) && !BrawlMoveData.Overlaps(down.hitbox, Vector2.zero, 1, new Vector2(.3f, 1.2f)),
                    "En altura: el ascendente alcanza arriba y no abajo; el remate, abajo y no arriba");

                // ── Launch formula K = (B + G·P/100) / resistance at the move's angle (GDD 2.0 §5) ──
                bool angleKept = true, upStays = true, downStays = true, grows = true;
                var compare = new System.Text.StringBuilder("golpe;%;vx_anterior;vy_anterior;vx_nuevo;vy_nuevo" + System.Environment.NewLine);
                foreach (var move in settings.sideMoves)
                {
                    var spec = move.ToSpec(AttackKind.Basic);
                    angleKept &= Mathf.Abs(move.launchAngle - Mathf.Atan2(move.lift, move.push) * Mathf.Rad2Deg) < .5f;
                    Vector2 previous = Vector2.zero;
                    foreach (int percent in new[] { 0, 50, 100, 150 })
                    {
                        Vector2 v = BrawlLaunch.Velocity(spec, settings, percent, 1, 1);
                        if (move.lift > 0) upStays &= v.y > 0; if (move.lift < 0) downStays &= v.y < 0;
                        if (percent > 0) grows &= v.magnitude >= previous.magnitude - .001f;
                        previous = v;
                        float oldX = Mathf.Min(settings.maxKnockback, move.push + percent * settings.knockbackPerDamage), oldY = Mathf.Min(16, move.lift + percent * .018f);
                        compare.AppendLine($"{move.id};{percent};{oldX:0.00};{oldY:0.00};{v.x:0.00};{v.y:0.00}");
                    }
                }
                File.WriteAllText(Path.Combine(output, "launch-compare.csv"), compare.ToString());
                check(angleKept, "Fórmula de lanzamiento: cada golpe conserva su ángulo de 0 %");
                check(upStays && downStays && grows, "Lanzamiento: ascendentes suben y remates bajan a todo porcentaje; K crece con el daño");
                var upSpec = up.ToSpec(AttackKind.Basic);
                check(BrawlLaunch.Velocity(upSpec, settings, 50, .9f, 1).magnitude > BrawlLaunch.Velocity(upSpec, settings, 50, 1.12f, 1).magnitude &&
                      BrawlLaunch.Velocity(upSpec, settings, 50, 1, -1).x < 0, "Resistencia divide el lanzamiento y la dirección se refleja");
                settings.angledLaunch = false;
                var legacy = BrawlFighter.CombineHits(30, 1, settings, new System.Collections.Generic.List<PendingHit> { new PendingHit(null, null, up.ToSpec(AttackKind.Basic), Vector3.right) }, true);
                settings.angledLaunch = true;
                float legacyDamage = 30 + up.damage;
                check(Mathf.Abs(legacy.Velocity.x - (up.push + legacyDamage * settings.knockbackPerDamage)) < .001f && Mathf.Abs(legacy.Velocity.y - (up.lift + legacyDamage * .018f)) < .001f,
                    "Con el lanzamiento angular desactivado se recupera la fórmula histórica");

                // ── Real rounds (CombatLab, 2.5D) ──
                match.SelectedCharacter = 0; match.StartTraining(); match.ShowCombatVolumes = true;
                yield return new WaitForSeconds(.3f);
                BrawlFighter a = match.Fighters[0], b = match.Fighters[1];

                // Data drives the game: a different value in the table changes the real hit.
                var edited = (MoveDef[])savedMoves.Clone();
                for (int i = 0; i < edited.Length; i++) if (edited[i].id == SideMove.UpBasic) edited[i].damage = 6;
                settings.sideMoves = edited;
                Place(a, new Vector3(-1, .05f, 0)); Place(b, new Vector3(0, .05f, 0)); yield return new WaitForSeconds(.3f);
                a.Submit(new BrawlCommand { Move = Vector2.up, Basic = true }); yield return new WaitForSeconds(.2f); a.ClearInput();
                check(Mathf.Abs(b.Damage - 6) < .01f && a.CurrentSideMove == SideMove.UpBasic, "Cambiar el daño en los datos cambia el golpe real");
                settings.sideMoves = (MoveDef[])savedMoves.Clone();

                // The box, not a literal range, decides contact.
                edited = (MoveDef[])savedMoves.Clone();
                for (int i = 0; i < edited.Length; i++) if (edited[i].id == SideMove.UpBasic) { var box = edited[i].hitbox; box.front = .1f; box.back = -.1f; edited[i].hitbox = box; }
                settings.sideMoves = edited;
                Place(a, new Vector3(-1.2f, .05f, 0)); Place(b, new Vector3(0, .05f, 0)); yield return new WaitForSeconds(.3f);
                a.Submit(new BrawlCommand { Move = Vector2.up, Basic = true }); yield return new WaitForSeconds(.2f); a.ClearInput();
                bool shrunkMissed = b.Damage < .01f;
                settings.sideMoves = (MoveDef[])savedMoves.Clone();
                Place(a, new Vector3(-1.2f, .05f, 0)); Place(b, new Vector3(0, .05f, 0)); yield return new WaitForSeconds(.3f);
                a.Submit(new BrawlCommand { Move = Vector2.up, Basic = true }); yield return new WaitForSeconds(.2f); a.ClearInput();
                check(shrunkMissed && b.Damage > 0, "La caja de golpe de los datos decide el contacto real");

                // Mirror in a real round: same move, same distance, both directions.
                float[] dealt = new float[2];
                for (int side = 0; side < 2; side++)
                {
                    float s = side == 0 ? 1 : -1;
                    Place(a, new Vector3(-1.5f * s, .05f, 0)); Place(b, new Vector3(0, .05f, 0)); yield return new WaitForSeconds(.3f);
                    a.Submit(new BrawlCommand { Aim = new Vector2(s, 0), Basic = true }); yield return new WaitForSeconds(.3f); a.ClearInput();
                    dealt[side] = b.Damage;
                }
                check(dealt[0] > 0 && Mathf.Approximately(dealt[0], dealt[1]), "Espejo en partida: el mismo golpe conecta igual a izquierda y derecha");

                // Stage edge: a rival just off the floor (in the air) is still inside the hit box.
                bool[] edge = new bool[2];
                for (int side = 0; side < 2; side++)
                {
                    float s = side == 0 ? 1 : -1;
                    Place(a, new Vector3(7.4f * s, .05f, 0)); yield return new WaitForSeconds(.35f);
                    Place(b, new Vector3(8.4f * s, .05f, 0));
                    a.Submit(new BrawlCommand { Move = Vector2.up, Aim = new Vector2(s, 0), Basic = true }); yield return new WaitForSeconds(.2f); a.ClearInput();
                    edge[side] = b.Damage > 0 && b.Velocity.y > 0;
                }
                check(edge[0] && edge[1], "Borde: el ascendente alcanza a un rival fuera del suelo en ambos bordes");

                // CombatLab draws exactly the box that is tested.
                Place(a, new Vector3(-2, .05f, 0)); Place(b, new Vector3(2, .05f, 0)); yield return new WaitForSeconds(.3f);
                a.Submit(new BrawlCommand { Aim = Vector2.right, Heavy = true });
                yield return new WaitForSeconds(.12f);
                var view = a.GetComponent<BrawlCombatVolumes>();
                bool drawn = view != null && view.LastHitbox.HasValue && a.TryGetSideHitbox(out MoveBox heavyBox, out _) &&
                             RectClose(view.LastHitbox.Value, BrawlMoveData.World(heavyBox, a.Root2D, a.FacingSign));
                yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, "lab-hitboxes.png"));
                yield return new WaitForSeconds(.5f); a.ClearInput();
                check(drawn, "CombatLab dibuja la caja de golpe que usa el contacto y la hurtbox de cada luchador");
            }
            finally
            {
                settings.angledLaunch = true;
                settings.sideMoves = savedMoves;
                match.ReturnToMenu();
                match.SelectedCharacter = oldCharacter;
                match.AutomatedTest = true;
            }
        }

        private static void Place(BrawlFighter fighter, Vector3 position) => fighter.ResetPractice(position, 0);

        private static bool RectClose(Rect a, Rect b) =>
            Mathf.Abs(a.xMin - b.xMin) < .05f && Mathf.Abs(a.xMax - b.xMax) < .05f && Mathf.Abs(a.yMin - b.yMin) < .05f && Mathf.Abs(a.yMax - b.yMax) < .05f;
    }
}
