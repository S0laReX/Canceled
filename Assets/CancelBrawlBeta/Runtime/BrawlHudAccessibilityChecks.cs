using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// Explicit smoke-test path only (<c>-ui-smoke</c>): SYS-071 high contrast, colour-blind palette measured under
    /// simulated protanopia/deuteranopia/tritanopia, new options saved without breaking v1/v2/v3 files and screenshots
    /// at every text/HUD size; SYS-076 combo and recovery tutorial in the lab, with no time limit.
    /// </summary>
    public static class BrawlHudAccessibilityChecks
    {
        /// <summary>Distinct player colours: CIELAB ΔE76 between every pair, under every simulated vision.</summary>
        public const float PlayerMinDeltaE = 25f;
        /// <summary>Warnings against the text colours they sit next to.</summary>
        public const float WarningMinDeltaE = 20f;
        public const float TextContrast = 4.5f, HighContrastText = 7f, NonTextContrast = 3f;
        // BrawlSideTelegraph: danger zones are red, the safe refuge is white (see its `danger` colour).
        private static readonly Color TelegraphDanger = new Color(1, .25f, .2f), TelegraphRefuge = Color.white;
        private static readonly ColorVision[] Visions = { ColorVision.Normal, ColorVision.Protanopia, ColorVision.Deuteranopia, ColorVision.Tritanopia };

        [Serializable] private sealed class VisionRow { public string vision; public float normalPlayers, colorBlindPlayers; public string normalWorstPair, colorBlindWorstPair; public float warningVsMuted, warningVsAccent, dangerBadge, telegraph; }
        [Serializable] private sealed class Report { public List<VisionRow> rows = new List<VisionRow>(); public List<string> contrast = new List<string>(); public List<string> tutorial = new List<string>(); }

        public static IEnumerator Run(BrawlMatch match, Action<bool, string> check, string output)
        {
            string folder = Path.Combine(output, "ui-checks");
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            Directory.CreateDirectory(folder);
            string originalPath = match.OptionsPath;
            var report = new Report();
            int oldOpponents = match.OpponentCount;
            try
            {
                match.AutomatedTest = true; match.ReturnToMenu();
                string file = Path.Combine(folder, "ui-options.json");
                match.UseOptionsFile(file);
                CheckOptions(match, check, folder, file);
                CheckColours(check, report);
                File.WriteAllText(Path.Combine(output, "ui-colour.json"), JsonUtility.ToJson(report, true));
                yield return Captures(match, check, output);
                yield return Tutorial(match, check, output, report);
                File.WriteAllText(Path.Combine(output, "ui-colour.json"), JsonUtility.ToJson(report, true));
            }
            finally
            {
                match.Hud.Tutorial.Stop();
                match.ReturnToMenu(); match.OptionsPage = 0;
                match.UseOptionsFile(originalPath);
                match.OpponentCount = oldOpponents;
                match.AutomatedTest = true;
            }
        }

        // ───────────── Options ─────────────
        private static void CheckOptions(BrawlMatch match, Action<bool, string> check, string folder, string file)
        {
            check(!match.HighContrast && !match.ColorBlindPalette, "Alto contraste y daltonismo empiezan desactivados");
            match.SetAccessibility(true, true);
            match.UseOptionsFile(file);
            string json = File.ReadAllText(file);
            check(match.OptionsStatus == OptionsLoadStatus.Loaded && match.HighContrast && match.ColorBlindPalette &&
                  json.Contains("\"highContrast\": true") && json.Contains("\"colorBlindPalette\": true") &&
                  BrawlOptionsStore.TryParse(json, out var stored, out _) && stored.version == BrawlOptionsStore.CurrentVersion,
                "Alto contraste y daltonismo se guardan y se recuperan sin cambiar el formato");
            const string v1 = "{\"format\":\"funados-options\",\"version\":1,\"reducedEffects\":true,\"sfx\":0.3,\"music\":0.2,\"keyboard\":[],\"gamepad\":[]}";
            const string v2 = "{\"format\":\"funados-options\",\"version\":2,\"reducedEffects\":false,\"sfx\":0.4,\"music\":0.4,\"moveDeadzone\":0.3," +
                              "\"aimDeadzone\":0.25,\"stickResponse\":1,\"vibration\":0.25,\"keyboard\":[],\"gamepad\":[]}";
            const string v3 = "{\"format\":\"funados-options\",\"version\":3,\"reducedEffects\":false,\"sfx\":0.4,\"music\":0.4,\"moveDeadzone\":0.3," +
                              "\"aimDeadzone\":0.25,\"stickResponse\":1,\"vibration\":0.25,\"textScale\":1.15,\"hudScale\":1.3,\"keyboard\":[],\"gamepad\":[]}";
            bool allOld = true;
            foreach (var (name, text, expected) in new[] { ("v1", v1, OptionsLoadStatus.MigratedLegacy), ("v2", v2, OptionsLoadStatus.MigratedLegacy), ("v3", v3, OptionsLoadStatus.Loaded) })
            {
                string path = Path.Combine(folder, name + ".json");
                File.WriteAllText(path, text);
                match.UseOptionsFile(path);
                bool ok = match.OptionsStatus == expected && !match.HighContrast && !match.ColorBlindPalette;
                if (name == "v1") ok &= BrawlFeedback.ReducedEffects && Mathf.Approximately(BrawlFeedback.SfxVolume, .3f);
                if (name == "v3") ok &= Mathf.Approximately(match.TextScale, 1.15f) && Mathf.Approximately(match.HudScale, 1.3f);
                if (!ok) Debug.Log("FUNADOS_UI options " + name + " estado=" + match.OptionsStatus);
                allOld &= ok;
            }
            check(allOld, "Archivos v1, v2 y v3 sin los campos nuevos cargan como antes, con las opciones nuevas desactivadas");
            match.UseOptionsFile(file); match.SetAccessibility(false, false); match.SetDisplayScale(1, 1);
        }

        // ───────────── Colour maths ─────────────
        private static void CheckColours(Action<bool, string> check, Report report)
        {
            var normal = BrawlHudPalette.For(false, false);
            var colourBlind = BrawlHudPalette.For(false, true);
            var both = BrawlHudPalette.For(true, true);
            bool playersOk = true, playersOnInk = true, warningsOk = true, badgeOk = true, telegraphOk = true;
            float normalWorst = float.MaxValue, cbWorst = float.MaxValue;
            foreach (ColorVision vision in Visions)
            {
                var row = new VisionRow { vision = vision.ToString() };
                row.normalPlayers = BrawlHudPalette.MinPairDeltaE(BrawlHudPalette.PlayerColors, vision, out int a, out int b);
                row.normalWorstPair = $"P{a + 1}-P{b + 1}";
                row.colorBlindPlayers = BrawlHudPalette.MinPairDeltaE(BrawlHudPalette.ColorBlindPlayerColors, vision, out a, out b);
                row.colorBlindWorstPair = $"P{a + 1}-P{b + 1}";
                normalWorst = Mathf.Min(normalWorst, row.normalPlayers); cbWorst = Mathf.Min(cbWorst, row.colorBlindPlayers);
                playersOk &= row.colorBlindPlayers >= PlayerMinDeltaE;
                foreach (var palette in new[] { colourBlind, both })
                    foreach (Color player in BrawlHudPalette.ColorBlindPlayerColors)
                        playersOnInk &= BrawlHudPalette.Contrast(player, palette.Ink, vision, Color.black) >= TextContrast;
                // Warnings (KO danger badge, ≥100 % damage, last 30 s, remap notices) against the text beside them.
                foreach (var palette in new[] { colourBlind, both })
                {
                    float vsMuted = BrawlHudPalette.DeltaE(palette.Warning, palette.Muted, vision);
                    float vsAccent = BrawlHudPalette.DeltaE(palette.Warning, palette.Accent, vision);
                    float vsPaper = BrawlHudPalette.DeltaE(palette.Warning, palette.Paper, vision);
                    warningsOk &= vsMuted >= WarningMinDeltaE && vsAccent >= WarningMinDeltaE && vsPaper >= WarningMinDeltaE;
                    if (!palette.HighContrast) { row.warningVsMuted = vsMuted; row.warningVsAccent = vsAccent; }
                    // The danger badge also says "!" and has an outline: the outline must stand out from the badge.
                    float fill = BrawlHudPalette.DeltaE(palette.DangerFill, palette.BadgeFill, vision, Color.black);
                    badgeOk &= fill >= WarningMinDeltaE && BrawlHudPalette.Contrast(palette.Warning, palette.DangerFill, vision, Color.black) >= NonTextContrast;
                    if (!palette.HighContrast) row.dangerBadge = fill;
                }
                row.telegraph = BrawlHudPalette.DeltaE(TelegraphDanger, TelegraphRefuge, vision);
                telegraphOk &= row.telegraph >= 30;
                report.rows.Add(row);
            }
            check(playersOk, $"Paleta de daltonismo: los cinco colores de jugador se distinguen (ΔE ≥ {PlayerMinDeltaE}) con visión normal, protanopía, deuteranopía y tritanopía; mínimo {cbWorst:0.0}");
            check(cbWorst > normalWorst, $"La paleta habitual baja a ΔE {normalWorst:0.0} con daltonismo; la nueva lo mejora ({cbWorst:0.0})");
            check(playersOnInk, "Cada color de jugador de la paleta de daltonismo se lee sobre el panel (≥ 4,5:1) en las cuatro visiones");
            check(warningsOk, $"Con DALTONISMO los avisos se distinguen del texto normal, apagado y de acento (ΔE ≥ {WarningMinDeltaE}) en las cuatro visiones");
            check(badgeOk, "El marcador de peligro de KO se distingue del normal por color de fondo y por un borde de ≥ 3:1, además de «!»");
            check(telegraphOk, "Avisos de super: rojo de peligro y blanco de refugio se distinguen (ΔE ≥ 30) en las cuatro visiones");

            bool textOk = true, highOk = true, improves = true;
            foreach (bool cb in new[] { false, true })
            {
                var plain = BrawlHudPalette.For(false, cb); var high = BrawlHudPalette.For(true, cb);
                foreach (ColorVision vision in Visions)
                {
                    Color[] plainText = { plain.Paper, plain.Muted, plain.Accent, plain.Warning }, highText = { high.Paper, high.Muted, high.Accent, high.Warning };
                    for (int i = 0; i < plainText.Length; i++)
                    {
                        // Panels are slightly translucent in normal mode: measure them over a white scene (worst case).
                        float p = Mathf.Min(BrawlHudPalette.Contrast(plainText[i], plain.Ink, vision, Color.white), BrawlHudPalette.Contrast(plainText[i], plain.Card, vision));
                        float h = Mathf.Min(BrawlHudPalette.Contrast(highText[i], high.Ink, vision, Color.white), BrawlHudPalette.Contrast(highText[i], high.Card, vision));
                        textOk &= p >= TextContrast; highOk &= h >= HighContrastText; improves &= h >= p - .01f;
                        report.contrast.Add($"{(cb ? "daltonismo" : "normal")} {vision} texto{i}: {p:0.0}:1 → alto contraste {h:0.0}:1");
                    }
                }
            }
            check(textOk, $"Texto normal, apagado, acento y avisos ≥ {TextContrast}:1 sobre paneles y tarjetas (también simulando daltonismo)");
            check(highOk && improves, $"ALTO CONTRASTE: todos los textos ≥ {HighContrastText}:1 y ninguno empeora");
        }

        // ───────────── Screenshots at every size ─────────────
        private static IEnumerator Captures(BrawlMatch match, Action<bool, string> check, string output)
        {
            var hud = match.Hud;
            int captured = 0; bool legible = true, strip = true, grows = true; int lastBody = 0;
            foreach (float scale in new[] { 1f, 1.15f, 1.3f })
            {
                match.SetDisplayScale(scale, scale);
                string tag = Mathf.RoundToInt(scale * 100).ToString();
                match.OpponentCount = 4; match.StartMatch(); yield return null; yield return null;
                legible &= hud.SmallestTextPixels >= 10.5f;
                strip &= Mathf.Abs(Camera.main.rect.y - hud.Layout.StripFraction) < .002f;
                int body = hud.BodyFontSize; grows &= body >= lastBody; lastBody = body;
                yield return Capture(output, "ui-" + tag + "-match.png"); captured++;
                match.TogglePause(); match.OptionsPage = 2; yield return null; yield return null;
                yield return Capture(output, "ui-" + tag + "-pause.png"); captured++;
                match.OptionsPage = 0; match.ReturnToMenu(); yield return null; yield return null;
                yield return Capture(output, "ui-" + tag + "-menu.png"); captured++;
            }
            check(captured == 9 && legible && strip && grows, "Capturas de partida, pausa (pestaña PANTALLA) y menú a 100, 115 y 130 %: texto legible, crece con la opción y la cámara respeta la franja");

            match.SetAccessibility(true, true);
            match.OpponentCount = 4; match.StartMatch(); yield return null; yield return null;
            bool coloured = true;
            foreach (var fighter in match.Fighters) coloured &= fighter.PlayerColor == BrawlHudPalette.Player(fighter.PlayerIndex, true);
            check(hud.Palette.HighContrast && hud.Palette.ColorBlind && coloured, "Con las opciones activas el HUD usa alto contraste y los luchadores la paleta de daltonismo");
            yield return Capture(output, "ui-contrast-daltonismo-match.png");
            // A fighter near a KO limit shows the danger badge: captured to review it with the colour-blind palette.
            match.Fighters[1].ResetPractice(new Vector3(-20.5f, 2, 0), 0);
            yield return new WaitForSecondsRealtime(.05f); yield return null;
            check(hud.DangerMarkers >= 1, "Marcador de peligro visible con alto contraste y daltonismo");
            yield return Capture(output, "ui-contrast-daltonismo-danger.png");
            match.TogglePause(); match.OptionsPage = 2; yield return null; yield return null;
            var ids = new List<string>(hud.FocusableIds);
            check(ids.Contains("display-contrast") && ids.Contains("display-colorblind") && ids.Contains("display-text") && ids.Contains("display-hud"),
                "Pestaña PANTALLA: tamaño de texto, de HUD, alto contraste y daltonismo navegables con teclado y mando");
            yield return Capture(output, "ui-contrast-daltonismo-pause.png");
            match.OptionsPage = 0; match.ReturnToMenu(); yield return null; yield return null;
            check(new List<string>(hud.FocusableIds).Contains("tutorial-menu"), "El menú ofrece el tutorial con foco navegable");
            yield return Capture(output, "ui-contrast-daltonismo-menu.png");
            match.SetAccessibility(false, false); match.SetDisplayScale(1, 1);
        }

        private static IEnumerator Capture(string output, string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output, name));
            yield return null;
        }

        // ───────────── Tutorial (SYS-076) ─────────────
        private static IEnumerator Tutorial(BrawlMatch match, Action<bool, string> check, string output, Report report)
        {
            var hud = match.Hud; var tutorial = hud.Tutorial;
            match.SelectedCharacter = 0; match.ArenaIndex = 0;
            hud.StartTutorial(); yield return null; yield return null;
            check(match.IsTraining && match.Phase == MatchPhase.Playing && tutorial.Step == TutorialStep.Launcher, "El tutorial abre el laboratorio en el primer paso");
            var a = match.Fighters[0]; var b = match.Fighters[1];
            float clock = match.RemainingTime;
            yield return new WaitForSecondsRealtime(6f);
            check(tutorial.Step == TutorialStep.Launcher && !tutorial.FeedbackIsWarning && Mathf.Approximately(match.RemainingTime, clock),
                "Sin límite de tiempo: 6 s sin hacer nada no avanzan, no fallan y no corre ningún reloj");
            yield return Capture(output, "tutorial-combo.png");

            a.Submit(new BrawlCommand { Basic = true }); yield return new WaitForSeconds(.35f); a.ClearInput();
            yield return new WaitForSeconds(1.3f);
            check(tutorial.Step == TutorialStep.Launcher && tutorial.FeedbackIsWarning, "Un golpe que no es ascendente no cuenta y lo explica");

            tutorial.Repeat(match); yield return null; yield return new WaitForSeconds(.2f);
            a.Submit(new BrawlCommand { Move = Vector2.up, Basic = true }); yield return new WaitForSeconds(.13f);
            check(tutorial.Step == TutorialStep.Chase, "Golpe ascendente reconocido");
            a.ClearInput(); yield return new WaitForSeconds(1.6f);
            check(tutorial.Step == TutorialStep.Launcher && tutorial.ComboRestarts == 1, "Si el combo se corta, el paso se repite sin penalización");

            tutorial.Repeat(match); yield return null; yield return new WaitForSeconds(.2f);
            a.Submit(new BrawlCommand { Move = Vector2.up, Basic = true }); yield return new WaitForSeconds(.13f);
            a.Submit(new BrawlCommand { Jump = true }); yield return new WaitForSeconds(.2f);
            check(tutorial.Step == TutorialStep.Aerial, "Salto tras el golpe reconocido");
            a.Submit(new BrawlCommand { Move = Vector2.up, Basic = true }); yield return new WaitForSeconds(.19f); a.ClearInput();
            check(tutorial.Step == TutorialStep.Finisher, "Golpe aéreo en combo reconocido (combo ≥ 2)");
            yield return Capture(output, "tutorial-finisher.png");

            match.TogglePause(); yield return new WaitForSecondsRealtime(1.5f);
            bool pausedSame = tutorial.Step == TutorialStep.Finisher && new List<string>(hud.FocusableIds).Contains("tutorial");
            match.TogglePause(); yield return null;
            check(pausedSame, "La pausa congela el tutorial y ofrece salir de él con el mando");

            a.ResetPractice(new Vector3(0, 5, 0), 0); b.ResetPractice(new Vector3(1, 4.2f, 0), 0); yield return null;
            a.Submit(new BrawlCommand { Move = Vector2.down, Basic = true }); yield return new WaitForSeconds(.2f); a.ClearInput();
            yield return null;
            check(tutorial.Step == TutorialStep.RecoveryJump && Mathf.Abs(a.transform.position.x - BrawlTutorial.OffStageX(match)) < 1.5f,
                "Remate reconocido; la segunda parte coloca al jugador fuera del borde");
            yield return Capture(output, "tutorial-recovery.png");

            float wait = 0;
            while (tutorial.RecoveryRetries == 0 && wait < 8) { wait += Time.unscaledDeltaTime; yield return null; }
            check(tutorial.RecoveryRetries == 1 && tutorial.Step == TutorialStep.RecoveryJump && a.transform.position.x > BrawlArena.MainFloorHalfWidth,
                "Caer fuera no termina el tutorial: se vuelve a colocar al jugador para intentarlo");

            yield return new WaitForSeconds(.1f);
            a.Submit(new BrawlCommand { Move = Vector2.left, Jump = true }); yield return new WaitForSeconds(.15f);
            check(tutorial.Step == TutorialStep.RecoverySpecial, "Doble salto fuera del borde reconocido");
            a.Submit(new BrawlCommand { Move = Vector2.left, Recovery = true }); yield return new WaitForSeconds(.1f);
            check(tutorial.Step == TutorialStep.RecoveryLand, "Recuperación reconocida");
            a.Submit(new BrawlCommand { Move = Vector2.left });
            wait = 0;
            while (tutorial.Step != TutorialStep.Done && wait < 6) { wait += Time.deltaTime; yield return null; }
            a.ClearInput();
            check(tutorial.Step == TutorialStep.Done && a.Grounded, "Volver al escenario completa el tutorial");
            yield return Capture(output, "tutorial-done.png");
            report.tutorial.Add($"reinicios de combo {tutorial.ComboRestarts}, reintentos de recuperación {tutorial.RecoveryRetries}, combo del remate {tutorial.FinisherCombo}");

            tutorial.Stop(); match.ReturnToMenu(); yield return null;
            hud.StartTutorial(); yield return null;
            match.ReturnToMenu(); yield return null; yield return null;
            check(!tutorial.Active, "Volver al menú cierra el tutorial");
        }
    }
}
