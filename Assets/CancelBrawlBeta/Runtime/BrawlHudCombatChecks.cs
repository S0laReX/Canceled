using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// Explicit smoke-test path only (<c>-hud-smoke</c>): GDD 2.0 §10 combat HUD and selection portraits. Portraits come
    /// from every real look and differ between characters; the percentage heats up (readable in all four palettes and
    /// simulated visions) and pops only without reduced effects; lives are icons; the combo counter follows the
    /// attacker; off-camera fighters show number and shape; everything is legible at 1280×720 with five fighters.
    /// </summary>
    public static class BrawlHudCombatChecks
    {
        public const float MinCoverage = .12f, MinPortraitDifference = .02f, TextContrast = 4.5f, HighContrastText = 7f;
        private static readonly ColorVision[] Visions = { ColorVision.Normal, ColorVision.Protanopia, ColorVision.Deuteranopia, ColorVision.Tritanopia };

        [Serializable] private sealed class PortraitRow { public string look; public float coverage; public string mean; }
        [Serializable] private sealed class Report { public List<PortraitRow> portraits = new List<PortraitRow>(); public List<string> heat = new List<string>(); public List<string> notes = new List<string>(); }

        public static IEnumerator Run(BrawlMatch match, Action<bool, string> check, string output)
        {
            var report = new Report();
            int oldOpponents = match.OpponentCount;
            bool oldReduced = BrawlFeedback.ReducedEffects;
            try
            {
                match.AutomatedTest = true; match.ReturnToMenu(); match.SetAccessibility(false, false); match.SetDisplayScale(1, 1);
                Portraits(match, check, report);
                CheckHeat(check, report);
                yield return Menu(match, check, output);
                yield return Combat(match, check, output, report);
            }
            finally
            {
                BrawlFeedback.ReducedEffects = oldReduced;
                match.ReturnToMenu(); match.OpponentCount = oldOpponents; match.AutomatedTest = true;
                File.WriteAllText(Path.Combine(output, "hud-combat.json"), JsonUtility.ToJson(report, true));
            }
        }

        // ───────────── Portraits ─────────────
        private static void Portraits(BrawlMatch match, Action<bool, string> check, Report report)
        {
            var portraits = match.Hud.Portraits;
            portraits.Prewarm(); portraits.Flush();
            int looks = 0; bool covered = true;
            var firsts = new Texture[BrawlRoster.Count];
            for (int c = 0; c < BrawlRoster.Count; c++)
                for (int s = 0; s < BrawlRoster.SkinCount(c); s++)
                {
                    Texture texture = portraits.Get(c, s);
                    float coverage = BrawlPortraits.Coverage(texture, out Color mean);
                    if (s == 0) firsts[c] = texture;
                    covered &= texture != null && coverage >= MinCoverage;
                    looks++;
                    report.portraits.Add(new PortraitRow { look = $"{match.CharacterName(c)} / {BrawlRoster.SkinName(c, s)}", coverage = coverage, mean = ColorUtility.ToHtmlStringRGB(mean) });
                }
            int expected = 0; for (int c = 0; c < BrawlRoster.Count; c++) expected += BrawlRoster.SkinCount(c);
            check(covered && looks == expected && portraits.Rendered == expected,
                $"Retrato de cada aspecto ({looks}: {BrawlRoster.Count} personajes) generado desde su modelo; la figura ocupa ≥ {MinCoverage:P0} del cuadro");
            float closest = float.MaxValue;
            for (int i = 0; i < firsts.Length; i++)
                for (int j = i + 1; j < firsts.Length; j++)
                    closest = Mathf.Min(closest, BrawlPortraits.Difference(firsts[i], firsts[j]));
            report.notes.Add($"diferencia mínima entre retratos de personajes distintos: {closest:0.000}");
            check(closest >= MinPortraitDifference, $"Los retratos de los cinco personajes son distintos entre sí (diferencia media por píxel ≥ {MinPortraitDifference}; mínima {closest:0.000})");
            int before = portraits.Rendered;
            portraits.Prewarm(); portraits.Flush();
            check(portraits.Rendered == before, "Los retratos se generan una sola vez y se reutilizan");
        }

        // ───────────── Heat colour ─────────────
        private static void CheckHeat(Action<bool, string> check, Report report)
        {
            bool readable = true, warms = true;
            foreach (bool high in new[] { false, true })
                foreach (bool cb in new[] { false, true })
                {
                    var palette = BrawlHudPalette.For(high, cb);
                    float lastBlue = float.MaxValue; float worst = float.MaxValue;
                    foreach (float damage in new[] { 0f, 30f, 60f, 80f, 100f, 125f, 150f, 220f })
                    {
                        Color c = BrawlHud.HeatColor(damage, palette);
                        foreach (ColorVision vision in Visions)
                        {
                            float ratio = Mathf.Min(BrawlHudPalette.Contrast(c, palette.Card, vision), BrawlHudPalette.Contrast(c, palette.Ink, vision, Color.white));
                            worst = Mathf.Min(worst, ratio);
                        }
                        // Warmer = less green relative to red (white → yellow → orange → red), monotonically.
                        float blue = c.g - c.r;
                        warms &= blue <= lastBlue + .001f; lastBlue = blue;
                    }
                    readable &= worst >= (high ? HighContrastText : TextContrast);
                    report.heat.Add($"{(high ? "alto contraste" : "normal")}{(cb ? " + daltonismo" : "")}: contraste mínimo {worst:0.0}:1");
                }
            var normal = BrawlHudPalette.For(false, false);
            float spread = BrawlHudPalette.DeltaE(BrawlHud.HeatColor(0, normal), BrawlHud.HeatColor(150, normal), ColorVision.Normal);
            check(warms && spread >= 30, $"El porcentaje se calienta al subir (blanco → amarillo → naranja → rojo; ΔE 0–150 % = {spread:0})");
            check(readable, $"El porcentaje se lee sobre la tarjeta a cualquier daño (≥ {TextContrast}:1; ≥ {HighContrastText}:1 con alto contraste), en las cuatro paletas y simulando daltonismo");
        }

        // ───────────── Selection screen ─────────────
        private static IEnumerator Menu(BrawlMatch match, Action<bool, string> check, string output)
        {
            match.OpponentCount = 4; match.ReturnToMenu();
            yield return null; yield return null; yield return null;
            check(match.Hud.PortraitsDrawn >= BrawlRoster.Count + 4, $"Selección: retrato en las cinco tarjetas de personaje y en los rivales ({match.Hud.PortraitsDrawn} dibujados)");
            check(match.Hud.GlyphWarmups > 0 && BrawlAudio.ClipCount >= BrawlAudio.EffectKinds.Length + BrawlAudio.VoiceKinds.Length,
                $"Letras del HUD ({match.Hud.GlyphWarmups} tamaños) y sonidos ({BrawlAudio.ClipCount}) preparados antes de jugar: el primer KO no genera nada");
            yield return Capture(output, "hud-menu-1280x720.png");
        }

        // ───────────── Combat ─────────────
        private static IEnumerator Combat(BrawlMatch match, Action<bool, string> check, string output, Report report)
        {
            var hud = match.Hud;
            match.OpponentCount = 4; match.StartMatch();
            yield return null; yield return null;
            var fighters = match.Fighters;
            int stocks = 0; foreach (var f in fighters) stocks += Mathf.Min(5, f.Stocks);
            yield return null;
            check(hud.PortraitsDrawn >= fighters.Length && hud.LifeIconsDrawn == stocks,
                $"Tarjetas de combate: retrato de los {fighters.Length} luchadores y una forma por vida ({hud.LifeIconsDrawn} de {stocks})");
            var shapes = new HashSet<PlayerShape>(); foreach (var f in fighters) shapes.Add(BrawlHud.ShapeOf(f.PlayerIndex));
            check(shapes.Count == fighters.Length, "Cada puesto P1–P5 tiene su forma (círculo, cuadrado, triángulo, rombo, estrella) además del color");
            check(Screen.height <= 720 ? hud.SmallestTextPixels >= 10.5f : hud.SmallestTextPixels >= 10.5f * Screen.height / 720f - .01f,
                $"Todo se lee a 720p: texto más pequeño {hud.SmallestTextPixels:0.0} px con cinco luchadores");

            // Damage pop and heat.
            BrawlFeedback.ReducedEffects = false;
            var target = fighters[1];
            hud.DamagePop(target);
            target.SetPracticeDamage(120); yield return null;
            float popped = hud.DamagePop(target);
            yield return new WaitForSecondsRealtime(BrawlHud.PopSeconds + .05f);
            float settled = hud.DamagePop(target);
            check(popped > 1.1f && Mathf.Approximately(settled, 1), $"El porcentaje crece al recibir daño (×{popped:0.00}) y vuelve a su tamaño en {BrawlHud.PopSeconds:0.00} s");
            BrawlFeedback.ReducedEffects = true;
            target.SetPracticeDamage(160); yield return null;
            check(Mathf.Approximately(hud.DamagePop(target), 1), "Con efectos reducidos el porcentaje no se anima");
            BrawlFeedback.ReducedEffects = false;
            fighters[2].SetPracticeDamage(70); fighters[3].SetPracticeDamage(100); fighters[4].SetPracticeDamage(30);
            yield return new WaitForSecondsRealtime(.4f);
            yield return Capture(output, "hud-combat-1280x720.png");

            // Combo beside the attacker: P3 hits P2 three times in the lab-free match.
            var attacker = fighters[2]; var victim = fighters[1];
            victim.ResetPractice(new Vector3(1.2f, 0, 0), 0); attacker.ResetPractice(new Vector3(0, 0, 0), 0);
            yield return null;
            float wait = 0; bool comboShown = false;
            while (wait < 4 && !comboShown)
            {
                attacker.Submit(new BrawlCommand { Move = Vector2.right * .01f, Basic = true });
                yield return new WaitForSeconds(.12f); attacker.ClearInput(); yield return new WaitForSeconds(.08f);
                wait += .2f;
                victim.ResetPractice(attacker.transform.position + Vector3.right * 1.1f, victim.Damage);
                comboShown = attacker.ComboHits >= 2 && hud.ComboLabelsDrawn >= 1;
            }
            report.notes.Add($"combo P3: {attacker.ComboHits} golpes tras {wait:0.0} s");
            if (comboShown) yield return Capture(output, "hud-combo-1280x720.png");
            check(comboShown, $"El contador de combo aparece junto al atacante (P3, {attacker.ComboHits} golpes), no solo para P1");

            // Off-camera marker with number and shape.
            fighters[4].ResetPractice(new Vector3(-20.5f, 2, 0), 0);
            yield return new WaitForSecondsRealtime(.05f); yield return null;
            check(hud.OffscreenMarkers >= 1 && hud.ShapeMarkersDrawn >= 1, "Un luchador fuera de cámara muestra en el borde su número y su forma");
            yield return Capture(output, "hud-offscreen-1280x720.png");

            // Same screen with high contrast and colour-blind palette.
            match.SetAccessibility(true, true); yield return null; yield return null;
            yield return Capture(output, "hud-combat-contraste-daltonismo.png");
            match.SetAccessibility(false, false);
        }

        private static IEnumerator Capture(string output, string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output, name));
            yield return null;
        }
    }
}
