using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// D03 measurement (GDD 2.0 §5, §14): a scripted attacker runs the ladder combo (up basic → jump on confirm → up
    /// basic…) while a scripted defender tries to escape on the first frame it can act. A "true chain" counts hits the
    /// defender had no actionable frame to avoid. Run at 0/50/100 % against the lightest and the heaviest kit. It reports
    /// data for the decision (hitstun decay vs hit cap); it does not decide balance.
    /// </summary>
    public static class BrawlComboStudy
    {
        [Serializable] public sealed class Row { public string target; public int percent; public int trueChain; public int comboCounter; public int hits; public float damageDealt; public bool escaped; }
        [Serializable] private sealed class Report { public string version = BrawlMatch.ProductionVersion; public string attacker; public bool angledLaunch; public List<Row> rows = new List<Row>(); }

        public static IEnumerator Run(BrawlMatch match, Action<bool, string> check, string output)
        {
            var report = new Report { attacker = match.CharacterName(0), angledLaunch = match.Settings.angledLaunch };
            int oldCharacter = match.SelectedCharacter;
            try
            {
                foreach (int targetKit in new[] { 1, 2 }) // Lola Viral (0.90), Coach Tóxico (1.12)
                    foreach (int percent in new[] { 0, 50, 100 })
                    {
                        var row = new Row { target = match.CharacterName(targetKit), percent = percent };
                        yield return Trial(match, targetKit, percent, row);
                        report.rows.Add(row);
                    }
                File.WriteAllText(Path.Combine(output, "combo-study.json"), JsonUtility.ToJson(report, true));
                foreach (var row in report.rows)
                    Debug.Log($"FUNADOS_COMBO {row.target} {row.percent}%: cadena verdadera {row.trueChain}, contador {row.comboCounter}, golpes {row.hits}, escapó {row.escaped}");
                check(report.rows.Count == 6 && report.rows.TrueForAll(r => r.hits > 0), "Estudio de combos D03 completado: 6 pruebas con impactos registrados");
            }
            finally
            {
                match.ReturnToMenu(); match.SelectedCharacter = oldCharacter; match.AutomatedTest = true;
            }
        }

        private static IEnumerator Trial(BrawlMatch match, int targetKit, int percent, Row row)
        {
            match.AutomatedTest = true; match.ReturnToMenu();
            match.SelectedCharacter = 0; match.SlotCharacters[1] = targetKit;
            match.StartTraining(); match.ShowCombatVolumes = false;
            yield return new WaitForSeconds(.3f);
            BrawlFighter a = match.Fighters[0], d = match.Fighters[1];
            a.ResetPractice(new Vector3(-.9f, .05f, 0), 0); d.ResetPractice(new Vector3(0, .05f, 0), percent);
            yield return new WaitForSeconds(.4f);

            float lastDamage = d.Damage, idle = 0, elapsed = 0;
            bool actionableSinceHit = true;
            int chain = 0;
            while (elapsed < 6 && idle < 2.5f && !d.IsRespawning)
            {
                // Defender: caught off guard by the first hit, then escapes as soon as it can act.
                if (row.hits > 0 && !d.InHitstun)
                {
                    actionableSinceHit = true;
                    float away = Mathf.Sign(d.transform.position.x - a.transform.position.x);
                    d.Submit(new BrawlCommand { Move = new Vector2(away == 0 ? 1 : away, 0), Dodge = !d.Grounded, Jump = true });
                }
                else
                {
                    if (!d.InHitstun) actionableSinceHit = true;
                    d.ClearInput();
                }

                // Attacker: ladder combo.
                Vector2 delta = d.Root2D - a.Root2D;
                var command = new BrawlCommand { Move = new Vector2(Mathf.Abs(delta.x) > .6f ? Mathf.Sign(delta.x) : 0, 0) };
                if (!a.IsAttacking && Mathf.Abs(delta.x) < 1.5f && delta.y > -.4f && delta.y < 2.4f) { command.Move = new Vector2(Mathf.Sign(delta.x), 1); command.Basic = true; }
                else if (!a.IsAttacking && delta.y > 1.1f && a.JumpsUsed < 2) command.Jump = true;

                yield return new WaitForFixedUpdate();
                if (d.Damage > lastDamage + .01f)
                {
                    row.hits++; row.damageDealt += d.Damage - lastDamage;
                    chain = actionableSinceHit ? 1 : chain + 1;
                    row.trueChain = Mathf.Max(row.trueChain, chain);
                    actionableSinceHit = false;
                    lastDamage = d.Damage; idle = 0;
                    command.Jump = true; // jump-cancel the confirmed basic, as a player would
                }
                row.comboCounter = Mathf.Max(row.comboCounter, a.ComboHits);
                a.Submit(command);
                idle += Time.fixedDeltaTime; elapsed += Time.fixedDeltaTime;
            }
            row.escaped = idle >= 2.5f || d.IsRespawning;
            a.ClearInput(); d.ClearInput();
        }
    }
}
