using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// Explicit smoke-test path only (SYS-071): layout at several aspect ratios, minimum legible text, text/HUD size
    /// options with v2→v3 migration, camera strip and lab pointer sharing the HUD layout, and real screenshots after
    /// resizing the window. Restores the original window size and options at the end.
    /// </summary>
    public static class BrawlDisplayChecks
    {
        private static readonly Vector2Int[] Screens = { new Vector2Int(1600, 900), new Vector2Int(1280, 720), new Vector2Int(960, 720), new Vector2Int(1280, 800), new Vector2Int(2560, 1080) };

        public static IEnumerator Run(BrawlMatch match, Action<bool, string> check, string output)
        {
            string folder = Path.Combine(output, "display-checks");
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            Directory.CreateDirectory(folder);
            string originalPath = match.OptionsPath;
            int width = Screen.width, height = Screen.height, oldOpponents = match.OpponentCount;
            try
            {
                match.AutomatedTest = true; match.ReturnToMenu();
                string file = Path.Combine(folder, "display.json");
                match.UseOptionsFile(file);

                // ── Layout maths ──
                var wide = BrawlHudLayout.Compute(1600, 900, 1, 2);
                check(Mathf.Abs(wide.PadX) < .01f && Mathf.Abs(wide.ExtraY) < .01f && Mathf.Abs(wide.StripFraction - .19f) < .001f, "16:9 conserva el diseño y la franja del 19 %");
                var classic = BrawlHudLayout.Compute(960, 720, 1, 2);
                check(Mathf.Abs(classic.PadX) < .01f && classic.ExtraY > 250 && classic.StripFraction < .19f, "4:3 extiende el lienzo hacia abajo en lugar de dejar bandas");
                var ultra = BrawlHudLayout.Compute(2560, 1080, 1, 2);
                check(ultra.PadX > 200 && Mathf.Abs(ultra.ExtraY) < .01f, "21:9 añade espacio lateral y mantiene la altura");
                bool fits = true, legibleWide = true, legibleAll = true;
                foreach (var size in Screens)
                    for (int cards = 1; cards <= 5; cards++)
                        foreach (float hudSize in new[] { 1f, 1.15f, 1.3f })
                        {
                            var layout = BrawlHudLayout.Compute(size.x, size.y, hudSize, cards);
                            fits &= BrawlHudLayout.RowWidth(cards) * layout.CardScale <= layout.ViewWidth - 39.9f &&
                                    BrawlHudLayout.HintWidth * layout.HintScale <= layout.ViewWidth - 39.9f && layout.CardScale <= hudSize + .0001f;
                            float smallest = Mathf.Min(layout.FontSize(13, 1, 12), layout.FontSize(15, 1, 13)) * layout.Scale;
                            legibleAll &= smallest >= 10.5f;
                            if (size.x * 9 >= size.y * 16) legibleWide &= smallest >= 11.5f;
                        }
                check(fits, "Tarjetas y ayuda caben en 5 proporciones, de 1 a 5 jugadores y 3 tamaños de HUD");
                check(legibleWide, "En 16:9 y 21:9 desde 720p el texto más pequeño no baja de 12 px reales");
                check(legibleAll, "En 4:3 y 16:10 pequeños el texto más pequeño no baja de 10,5 px reales");
                var big = BrawlHudLayout.Compute(1600, 900, 1.3f, 2);
                var full = BrawlHudLayout.Compute(1600, 900, 1.3f, 5);
                check(Mathf.Approximately(big.CardScale, 1.3f) && full.CardScale < 1.05f && big.StripHeight > wide.StripHeight,
                    "HUD grande agranda las tarjetas mientras quepan y amplía la franja reservada");
                var hd = BrawlHudLayout.Compute(1280, 720, 1, 0);
                check(hd.FontSize(13, 1.3f, 12) > hd.FontSize(13, 1, 12) && hd.FontSize(13, 1.3f, 12) <= Mathf.RoundToInt(13 * 1.4f),
                    "Tamaño de texto crece con la opción y tiene un tope para no romper paneles");

                // ── Options v3 ──
                match.SetDisplayScale(1.3f, 9);
                check(Mathf.Approximately(match.TextScale, 1.3f) && Mathf.Approximately(match.HudScale, BrawlHudLayout.MaxScaleOption), "Tamaños limitados a 100–130 %");
                match.SetDisplayScale(1.15f, 1.3f);
                match.UseOptionsFile(file);
                check(match.OptionsStatus == OptionsLoadStatus.Loaded && Mathf.Approximately(match.TextScale, 1.15f) && Mathf.Approximately(match.HudScale, 1.3f) &&
                      BrawlOptionsStore.TryParse(File.ReadAllText(file), out var stored, out _) && stored.version >= 3, "Tamaños de texto y HUD se guardan en formato versión 3");
                string v2 = Path.Combine(folder, "v2.json");
                File.WriteAllText(v2, "{\"format\":\"funados-options\",\"version\":2,\"reducedEffects\":false,\"sfx\":0.4,\"music\":0.4,\"moveDeadzone\":0.3," +
                    "\"aimDeadzone\":0.25,\"stickResponse\":1,\"vibration\":0.25,\"keyboard\":[],\"gamepad\":[]}");
                match.UseOptionsFile(v2);
                check(match.OptionsStatus == OptionsLoadStatus.MigratedLegacy && Mathf.Approximately(match.MoveDeadzone, .3f) &&
                      Mathf.Approximately(match.VibrationStrength, .25f) && Mathf.Approximately(match.TextScale, 1), "Archivo versión 2 se migra conservando ajustes de mando");

                // ── Runtime: camera strip, lab pointer and screenshots at other window sizes ──
                match.UseOptionsFile(file);
                match.SetDisplayScale(1.3f, 1.3f);
                match.OpponentCount = 2; match.StartMatch();
                yield return null; yield return null;
                var hud = match.Hud;
                check(Mathf.Abs(Camera.main.rect.y - hud.Layout.StripFraction) < .002f && hud.Layout.CardScale > 1.2f, "La cámara reserva exactamente la franja de las tarjetas");
                match.ReturnToMenu(); match.StartTraining(); yield return null; yield return null;
                Matrix4x4 lab = hud.Layout.TopScaled(new Vector2(BrawlHudLayout.Width, 0));
                Vector3 inside = lab.MultiplyPoint3x4(new Vector3(1285, 195, 0));
                check(hud.PointerOverTraining(new Vector2(inside.x, Screen.height - inside.y)) &&
                      !hud.PointerOverTraining(new Vector2(Screen.width * .5f, Screen.height * .5f)), "Clics sobre el panel del laboratorio se reconocen con el HUD agrandado");
                match.ReturnToMenu();

                int captured = 0, resized = 0; bool allLegible = true, stripOk = true, navOk = true;
                foreach (var size in new[] { new Vector2Int(960, 720), new Vector2Int(1280, 800), new Vector2Int(1280, 548) })
                {
                    Screen.SetResolution(size.x, size.y, FullScreenMode.Windowed);
                    yield return new WaitForSecondsRealtime(.8f);
                    if (Screen.width != size.x || Screen.height != size.y) continue; // window could not take that size
                    resized++;
                    string tag = size.x + "x" + size.y;
                    match.OpponentCount = 4; match.StartMatch(); yield return null; yield return null;
                    allLegible &= hud.SmallestTextPixels >= 10.5f;
                    stripOk &= Mathf.Abs(Camera.main.rect.y - hud.Layout.StripFraction) < .002f;
                    yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, "display-" + tag + "-match.png")); captured++;
                    yield return null;
                    match.TogglePause(); match.OptionsPage = 2; yield return null; yield return null;
                    yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, "display-" + tag + "-pause.png")); captured++;
                    yield return null;
                    match.OptionsPage = 0; match.ReturnToMenu(); yield return null; yield return null; yield return null;
                    navOk &= new System.Collections.Generic.List<string>(hud.FocusableIds).Contains("start");
                    yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, "display-" + tag + "-menu.png")); captured++;
                    yield return null;
                }
                check(resized >= 2 && captured == resized * 3, $"Capturas de menú, partida y pausa en {resized} tamaños de ventana");
                check(allLegible && stripOk && navOk, "En cada tamaño: texto legible, franja de cámara correcta y menú navegable");
            }
            finally
            {
                match.ReturnToMenu(); match.OptionsPage = 0;
                Screen.SetResolution(width, height, FullScreenMode.Windowed);
                match.UseOptionsFile(originalPath);
                match.OpponentCount = oldOpponents;
                match.AutomatedTest = true;
            }
            yield return new WaitForSecondsRealtime(.5f);
        }
    }
}
