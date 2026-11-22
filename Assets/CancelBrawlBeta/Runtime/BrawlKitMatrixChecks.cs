using System;
using System.Collections;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// GDD 2.0 §6/§8.1 acceptance matrix (2.5D, CombatLab): special, recovery and super of every kit when it hits,
    /// misses, is interrupted, is paused, is used from a platform and from the stage edge. 5 kits × 18 cases.
    /// </summary>
    public static class BrawlKitMatrixChecks
    {
        private const float PlatformY = 2.65f, Edge = 7.4f;
        private static readonly float[] SpecialReach = { 3, 3, 3, 3, 1.6f };
        private static readonly float[] SuperReach = { 3, 3.6f, 3, 3, 3 };

        public static IEnumerator Run(BrawlMatch match, Action<bool, string> check, string output)
        {
            int oldCharacter = match.SelectedCharacter;
            match.AutomatedTest = true;
            try
            {
                for (int kit = 0; kit < BrawlRoster.Count; kit++)
                {
                    string name = match.CharacterName(kit);
                    foreach (bool super in new[] { false, true })
                    {
                        string ability = super ? "super" : "especial";
                        float reach = super ? SuperReach[kit] : SpecialReach[kit];
                        float wait = Duration(match, kit, super);
                        var result = new bool[1];

                        // Hit.
                        yield return Setup(match, kit, new Vector3(-reach / 2, .05f, 0), new Vector3(reach / 2, .05f, 0));
                        Cast(match, super, Vector2.right); yield return new WaitForSeconds(wait);
                        check(Target(match).Damage > 0 && !Caster(match).IsAttacking && (!super || Caster(match).Meter == 0), $"{name} · {ability} · acierto: conecta, termina y consume lo que debe");

                        // Miss: rival behind and out of reach.
                        yield return Setup(match, kit, new Vector3(3, .05f, 0), new Vector3(-7, .05f, 0));
                        Cast(match, super, Vector2.right); yield return new WaitForSeconds(wait);
                        check(Target(match).Damage == 0 && !Caster(match).IsAttacking && (!super || Caster(match).Meter == 0), $"{name} · {ability} · fallo: sin daño, termina y {(super ? "consume la barra" : "deja actuar")}");

                        // Interrupted during the windup: nothing may hit afterwards.
                        yield return Setup(match, kit, new Vector3(-reach / 2, .05f, 0), new Vector3(reach / 2, .05f, 0));
                        Cast(match, super, Vector2.right); yield return new WaitForSeconds(.05f);
                        bool wasAttacking = Caster(match).IsAttacking;
                        Caster(match).ReceiveHit(new AttackSpec(AttackKind.Basic, 1, 0, .1f, .1f, 2, 1, 1, 1), Vector3.left, Target(match));
                        Target(match).SetPracticeDamage(0);
                        yield return new WaitForSeconds(wait);
                        check(wasAttacking && Target(match).Damage == 0 && Caster(match).Damage > 0, $"{name} · {ability} · interrupción: el golpe cancela y no queda daño pendiente");

                        // Paused during the windup: frozen, then resolves normally.
                        yield return Setup(match, kit, new Vector3(-reach / 2, .05f, 0), new Vector3(reach / 2, .05f, 0));
                        Cast(match, super, Vector2.right); yield return new WaitForSeconds(.05f);
                        match.TogglePause(); yield return new WaitForSeconds(.5f);
                        bool frozen = match.Phase == MatchPhase.Paused && Target(match).Damage == 0 && Caster(match).IsAttacking;
                        match.TogglePause(); yield return new WaitForSeconds(wait);
                        check(frozen && Target(match).Damage > 0, $"{name} · {ability} · pausa: se congela y al reanudar conecta");

                        // From a raised platform, against a rival on the same platform.
                        yield return Setup(match, kit, new Vector3(3.3f, PlatformY, 0), new Vector3(5.9f, PlatformY, 0));
                        bool standing = Caster(match).Grounded && Caster(match).transform.position.y > 2.4f;
                        Cast(match, super, Vector2.right);
                        // A strong hit from a platform can ring the target out; CombatLab then resets its damage, so keep the peak.
                        peak = 0;
                        for (float t = 0; t < wait; t += Time.fixedDeltaTime) { yield return new WaitForFixedUpdate(); peak = Mathf.Max(peak, Target(match).Damage); }
                        check(standing && peak > 0 && Caster(match).transform.position.y > 2.4f, $"{name} · {ability} · plataforma: conecta y el lanzador sigue sobre ella");

                        // From the stage edge, facing inward.
                        yield return Setup(match, kit, new Vector3(Edge, .05f, 0), new Vector3(Edge - reach, .05f, 0));
                        Cast(match, super, Vector2.left); yield return new WaitForSeconds(wait);
                        check(Target(match).Damage > 0 && Caster(match).Grounded && Mathf.Abs(Caster(match).transform.position.x) < 8, $"{name} · {ability} · borde: conecta y el lanzador sigue en el escenario");
                    }

                    // ── Recovery ──
                    bool[] back = new bool[2];
                    for (int side = 0; side < 2; side++)
                    {
                        float s = side == 0 ? 1 : -1;
                        yield return Setup(match, kit, new Vector3(8.6f * s, -.5f, 0), new Vector3(-2 * s, .05f, 0));
                        var a = Caster(match); a.SetPracticeDamage(37);
                        yield return Recover(a, -s);
                        back[side] = a.Grounded && Mathf.Abs(a.transform.position.x) <= 8 && Mathf.Abs(a.Damage - 37) < .01f;
                    }
                    check(back[0], $"{name} · recuperación · acierto: vuelve al escenario desde fuera del borde derecho");
                    check(back[1], $"{name} · recuperación · borde: vuelve también desde el borde izquierdo");

                    yield return Setup(match, kit, new Vector3(0, .05f, 0), new Vector3(3, .05f, 0));
                    var grounded = Caster(match); float startY = grounded.transform.position.y;
                    grounded.Submit(new BrawlCommand { Recovery = true }); yield return new WaitForSeconds(.3f);
                    check(grounded.RecoveryAvailable && grounded.Grounded && Mathf.Abs(grounded.transform.position.y - startY) < .05f, $"{name} · recuperación · fallo: en el suelo no se gasta ni despega");

                    yield return Setup(match, kit, new Vector3(0, 3, 0), new Vector3(4, .05f, 0));
                    var hit = Caster(match);
                    hit.Submit(new BrawlCommand { Recovery = true }); yield return new WaitForSeconds(.12f);
                    int jumpsBefore = hit.JumpsUsed;
                    hit.ReceiveHit(new AttackSpec(AttackKind.Basic, 1, 0, .1f, .1f, 2, 1, 1, 1), Vector3.left, Target(match));
                    yield return new WaitForSeconds(.1f);
                    check(!hit.RecoveryAvailable && hit.JumpsUsed >= jumpsBefore && !hit.Grounded, $"{name} · recuperación · interrupción: el golpe no devuelve recuperación ni saltos");

                    yield return Setup(match, kit, new Vector3(0, 3, 0), new Vector3(4, .05f, 0));
                    var paused = Caster(match);
                    paused.Submit(new BrawlCommand { Recovery = true }); yield return new WaitForSeconds(.1f);
                    match.TogglePause(); Vector3 frozenAt = paused.transform.position; yield return new WaitForSeconds(.4f);
                    bool still = paused.transform.position == frozenAt;
                    match.TogglePause(); yield return new WaitForSeconds(.2f);
                    check(still && paused.transform.position != frozenAt && !paused.RecoveryAvailable, $"{name} · recuperación · pausa: se congela y continúa al reanudar");

                    yield return Setup(match, kit, new Vector3(4.8f, .05f, 0), new Vector3(-4, .05f, 0));
                    var under = Caster(match);
                    under.ResetPractice(new Vector3(4.8f, 1.2f, 0), 0); // airborne right below the platform
                    float highest = under.transform.position.y; bool above = false;
                    under.Submit(new BrawlCommand { Recovery = true });
                    for (float t = 0; t < 1.6f; t += Time.fixedDeltaTime)
                    {
                        yield return new WaitForFixedUpdate();
                        float x = under.transform.position.x;
                        highest = Mathf.Max(highest, under.transform.position.y);
                        above |= under.transform.position.y > 2.9f && x > 2.9f && x < 6.7f;
                    }
                    check(above && highest > 2.9f && under.Grounded, $"{name} · recuperación · plataforma: atraviesa la plataforma desde abajo y aterriza");
                }
            }
            finally
            {
                match.ReturnToMenu(); match.SelectedCharacter = oldCharacter; match.AutomatedTest = true;
            }
        }

        private static float peak;
        private static BrawlFighter Caster(BrawlMatch match) => match.Fighters[0];
        private static BrawlFighter Target(BrawlMatch match) => match.Fighters[1];

        private static float Duration(BrawlMatch match, int kit, bool super)
        {
            var spec = match.Settings.Attack(super ? AttackKind.Super : AttackKind.Special, kit, 0);
            return spec.Duration + (super ? .5f : .9f);
        }

        private static IEnumerator Setup(BrawlMatch match, int kit, Vector3 caster, Vector3 target)
        {
            match.SelectedCharacter = kit; match.ArenaIndex = 0; match.StartTraining(); match.ShowCombatVolumes = false;
            yield return null;
            Caster(match).ResetPractice(caster, 0); Target(match).ResetPractice(target, 0);
            yield return new WaitForSeconds(.3f);
        }

        private static void Cast(BrawlMatch match, bool super, Vector2 aim)
        {
            if (super) Caster(match).SetMeterForTest(100);
            Caster(match).Submit(new BrawlCommand { Special = !super, Super = super, Aim = aim });
        }

        /// <summary>Player-like return: drift toward the stage, double jump when falling, then the recovery.</summary>
        private static IEnumerator Recover(BrawlFighter fighter, float towardStage)
        {
            bool usedRecovery = false;
            for (float t = 0; t < 3.5f && !(fighter.Grounded && Mathf.Abs(fighter.transform.position.x) < 8); t += Time.fixedDeltaTime)
            {
                var command = new BrawlCommand { Move = new Vector2(towardStage, 0) };
                if (fighter.Velocity.y < 0 && fighter.JumpsUsed < 2) command.Jump = true;
                else if (fighter.Velocity.y < 0 && !usedRecovery) { command.Recovery = true; usedRecovery = true; }
                fighter.Submit(command);
                yield return new WaitForFixedUpdate();
            }
            fighter.ClearInput();
            yield return new WaitForSeconds(.1f);
        }
    }
}
