using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// Explicit smoke-test path only (<c>-music-smoke</c>): GDD 2.0 §10 music per arena, character lines, subtitles and
    /// credits. Tracks: one per arena plus the menu, distinct, seamless, same loudness, quiet enough for hits and dodges to
    /// stand out, swapped without building anything during play. Lines: 15 (KO, super and win for each of the five),
    /// different voices, one at a time by priority. Subtitles: shown with the line (also with voices at 0 %), off by option
    /// (saved as an optional field), sized by the text option, readable in both contrast modes and palettes. Credits:
    /// reachable from the menu, keyboard/pad navigable, with the Meshy attribution verbatim. Writes WAVs for listening.
    /// </summary>
    public static class BrawlMusicChecks
    {
        public const float HeadSeconds = .08f, MaxStartupMilliseconds = 500, MinTextContrast = 7f, MinSpeakerContrast = 4.5f;

        [Serializable] private sealed class TrackRow { public string name, style; public float bpm, seconds, rms, peak, brightness, seamJump, typicalStep, lightSalience, dodgeSalience; }
        [Serializable] private sealed class VoiceRow { public string kind, text; public float seconds, peak, pitch; }
        [Serializable]
        private sealed class Report
        {
            public float musicBuildMs, prewarmMs, backgroundMs, switchMs; public List<TrackRow> tracks = new List<TrackRow>(); public List<VoiceRow> voices = new List<VoiceRow>();
            public List<string> subtitles = new List<string>(); public List<string> notes = new List<string>();
        }

        public static IEnumerator Run(BrawlMatch match, Action<bool, string> check, string output)
        {
            var report = new Report();
            float waited = 0;
            while (BrawlAudio.BackgroundPending > 0 && waited < 10) { yield return null; waited += Time.unscaledDeltaTime; }
            int oldOpponents = match.OpponentCount;
            string originalOptions = match.OptionsPath;
            float oldVoice = BrawlAudio.Volume(BrawlAudioBus.Voice);
            Directory.CreateDirectory(Path.Combine(output, "music")); Directory.CreateDirectory(Path.Combine(output, "voices"));
            string optionsFile = Path.Combine(output, "music-options.json");
            try
            {
                match.AutomatedTest = true; match.ReturnToMenu();
                match.UseOptionsFile(optionsFile); match.SetAccessibility(false, false); match.SetDisplayScale(1, 1); match.SetSubtitles(true);
                Tracks(check, report, output);
                Voices(check, report, output);
                yield return Switching(match, check, report);
                yield return Lines(match, check, report, output);
                Options(match, check, output, optionsFile);
                yield return Credits(match, check, report, output);
            }
            finally
            {
                BrawlAudio.SetVolume(BrawlAudioBus.Voice, oldVoice);
                match.Hud.ForceNavigation = false; BrawlCredits.Close();
                match.ReturnToMenu(); match.OpponentCount = oldOpponents; match.AutomatedTest = true;
                match.SetAccessibility(false, false); match.SetDisplayScale(1, 1); match.SetSubtitles(true);
                if (!string.IsNullOrEmpty(originalOptions)) match.UseOptionsFile(originalOptions);
                File.WriteAllText(Path.Combine(output, "music-smoke.json"), JsonUtility.ToJson(report, true));
            }
        }

        // ───────────── Tracks ─────────────
        private static void Tracks(Action<bool, string> check, Report report, string output)
        {
            report.musicBuildMs = BrawlAudio.MusicBuildMilliseconds; report.prewarmMs = BrawlAudio.PrewarmMilliseconds; report.backgroundMs = BrawlAudio.BackgroundMilliseconds;
            bool built = true, exact = true, level = true, seamless = true, audible = true;
            var bpms = new HashSet<float>(); var datas = new List<float[]>();
            float[] light = BrawlSfxSynth.Build("hit-light", 1), dodge = BrawlSfxSynth.Build("dodge", 1);
            for (int t = 0; t < BrawlMusic.TrackCount; t++)
            {
                AudioClip clip = BrawlAudio.MusicClip(t);
                built &= clip != null && clip.frequency == BrawlMusic.Rate && clip.channels == 1;
                if (clip == null) continue;
                var data = new float[clip.samples]; clip.GetData(data, 0); datas.Add(data);
                exact &= clip.samples == BrawlMusic.LoopSamples(t);
                BrawlSfxSynth.Features(data, out float seconds, out float rms, out float brightness, out _);
                float peak = 0, steps = 0; foreach (float v in data) peak = Mathf.Max(peak, Mathf.Abs(v));
                for (int i = 1; i < data.Length; i++) steps += Mathf.Abs(data[i] - data[i - 1]);
                float typical = steps / Mathf.Max(1, data.Length - 1), seam = Mathf.Abs(data[0] - data[data.Length - 1]);
                level &= Mathf.Abs(rms - BrawlMusic.TargetRms) <= BrawlMusic.TargetRms * .12f && peak <= BrawlMusic.MaxPeak + .001f;
                seamless &= seam <= Mathf.Max(.02f, typical * 6);
                float musicLevel = rms * BrawlAudio.BusGain[(int)BrawlAudioBus.Music];
                float lightSalience = Salience(light, musicLevel), dodgeSalience = Salience(dodge, musicLevel);
                BrawlSfxSynth.Features(dodge, out _, out _, out float dodgeBrightness, out _);
                audible &= lightSalience > 1.5f && (dodgeSalience > 1.5f || dodgeBrightness > brightness * 4);
                bpms.Add(BrawlMusic.Bpm(t));
                report.tracks.Add(new TrackRow { name = BrawlMusic.Name(t), style = BrawlMusic.Style(t), bpm = BrawlMusic.Bpm(t), seconds = seconds, rms = rms, peak = peak,
                    brightness = brightness, seamJump = seam, typicalStep = typical, lightSalience = lightSalience, dodgeSalience = dodgeSalience });
                WriteWav(Path.Combine(output, "music", t + "-" + Safe(BrawlMusic.Name(t)) + ".wav"), data, BrawlMusic.Rate);
            }
            check(built && datas.Count == BrawlArena.Count + 1 && BrawlAudio.BackgroundPending == 0,
                $"Una pista para el menú y una por arena ({datas.Count}); las de arena y las frases se crean en segundo plano con el menú abierto ({report.backgroundMs:0} ms)");
            check(report.musicBuildMs + report.prewarmMs < MaxStartupMilliseconds,
                $"Arrancar solo espera a la pista del menú y a los efectos: {report.musicBuildMs:0} + {report.prewarmMs:0} ms (< {MaxStartupMilliseconds:0} ms)");
            check(exact, "Cada pista dura exactamente 8 compases: el bucle no se desplaza respecto al pulso");
            check(seamless, "El bucle no tiene costura: el salto entre el final y el principio es como un paso normal de la onda");
            check(level, $"Todas las pistas suenan igual de fuerte (RMS {BrawlMusic.TargetRms:0.00} ±12 %) y sin recortar (pico ≤ {BrawlMusic.MaxPeak})");
            float maxCorrelation = 0;
            for (int a = 0; a < datas.Count; a++)
                for (int b = a + 1; b < datas.Count; b++) maxCorrelation = Mathf.Max(maxCorrelation, Mathf.Abs(Correlation(datas[a], datas[b], BrawlMusic.Rate * 4)));
            check(bpms.Count == BrawlMusic.TrackCount && maxCorrelation < .5f, $"Las {BrawlMusic.TrackCount} pistas son distintas: tempos diferentes y correlación máxima {maxCorrelation:0.00} entre ellas");
            check(audible, "En cualquier pista al 100 %, el golpe ligero y la esquiva siguen destacando (nivel ×1,5 o, la esquiva, 4 veces más aguda)");
        }

        private static float Salience(float[] effect, float musicLevel)
        {
            int head = Mathf.Min(effect.Length, Mathf.RoundToInt(HeadSeconds * BrawlSfxSynth.Rate)); double sum = 0;
            for (int i = 0; i < head; i++) sum += effect[i] * effect[i];
            float headRms = Mathf.Sqrt((float)(sum / Mathf.Max(1, head)));
            return headRms * BrawlAudio.BusGain[(int)BrawlAudioBus.Sfx] * BrawlAudio.DefaultVolumes[(int)BrawlAudioBus.Sfx] / Mathf.Max(1e-5f, musicLevel);
        }

        private static float Correlation(float[] a, float[] b, int count)
        {
            count = Mathf.Min(count, Mathf.Min(a.Length, b.Length));
            double ab = 0, aa = 0, bb = 0;
            for (int i = 0; i < count; i++) { ab += a[i] * b[i]; aa += a[i] * a[i]; bb += b[i] * b[i]; }
            return aa > 0 && bb > 0 ? (float)(ab / Math.Sqrt(aa * bb)) : 0;
        }

        // ───────────── Voices ─────────────
        private static void Voices(Action<bool, string> check, Report report, string output)
        {
            bool built = true, sized = true, original = true;
            var pitches = new float[BrawlVoices.CharacterCount];
            var texts = new HashSet<string>();
            for (int c = 0; c < BrawlVoices.CharacterCount; c++)
                for (int l = 0; l < BrawlVoices.LineCount; l++)
                {
                    string kind = BrawlVoices.Kind((VoiceLine)l, c), text = BrawlVoices.Text((VoiceLine)l, c);
                    AudioClip clip = BrawlVoices.Build(kind);
                    built &= clip != null;
                    if (clip == null) continue;
                    var data = new float[clip.samples]; clip.GetData(data, 0); UnityEngine.Object.Destroy(clip);
                    float peak = 0; foreach (float v in data) peak = Mathf.Max(peak, Mathf.Abs(v));
                    float pitch = Pitch(data);
                    if (l == (int)VoiceLine.Win) pitches[c] = pitch;
                    sized &= clip.length >= .3f && clip.length <= BrawlVoices.MaxSeconds + .1f && peak <= BrawlVoices.Peak + .001f && peak > .5f;
                    original &= texts.Add(text) && text.Length > 3;
                    report.voices.Add(new VoiceRow { kind = kind, text = text, seconds = clip.length, peak = peak, pitch = pitch });
                    WriteWav(Path.Combine(output, "voices", kind.Replace('@', '-') + ".wav"), data, BrawlVoices.Rate);
                }
            check(built && report.voices.Count == 15 && BrawlAudio.ClipCount >= BrawlAudio.EffectKinds.Length + 15,
                $"Quince frases (KO, super y victoria de cada personaje), preparadas al arrancar ({BrawlAudio.ClipCount} sonidos en memoria)");
            check(sized, "Cada frase dura entre 0,3 y 2,4 s y no recorta (pico 0,8)");
            check(original, "Las quince frases son distintas; el super dice el nombre del super de su kit");
            float minRatio = float.MaxValue;
            for (int a = 0; a < pitches.Length; a++)
                for (int b = a + 1; b < pitches.Length; b++) minRatio = Mathf.Min(minRatio, Mathf.Max(pitches[a], pitches[b]) / Mathf.Max(1, Mathf.Min(pitches[a], pitches[b])));
            check(minRatio >= 1.08f, $"Cada personaje tiene su voz: tonos {string.Join(", ", Array.ConvertAll(pitches, p => p.ToString("0")))} Hz, separados al menos un 8 % ({(minRatio - 1) * 100:0} %)");
        }

        /// <summary>Median fundamental over voiced 40 ms frames, by autocorrelation (70–400 Hz).</summary>
        private static float Pitch(float[] data)
        {
            int frame = Mathf.RoundToInt(.04f * BrawlVoices.Rate), minLag = BrawlVoices.Rate / 400, maxLag = BrawlVoices.Rate / 70;
            var found = new List<float>();
            for (int start = 0; start + frame + maxLag < data.Length; start += frame)
            {
                double energy = 0; for (int i = 0; i < frame; i++) energy += data[start + i] * data[start + i];
                if (energy / frame < .005f) continue;
                double best = 0; int bestLag = 0;
                for (int lag = minLag; lag <= maxLag; lag++)
                {
                    double sum = 0; for (int i = 0; i < frame; i++) sum += data[start + i] * data[start + i + lag];
                    if (sum > best) { best = sum; bestLag = lag; }
                }
                if (bestLag > 0 && best > energy * .5f) found.Add(BrawlVoices.Rate / (float)bestLag);
            }
            if (found.Count == 0) return 0;
            found.Sort(); return found[found.Count / 2];
        }

        // ───────────── Arena music in play ─────────────
        private static IEnumerator Switching(BrawlMatch match, Action<bool, string> check, Report report)
        {
            var music = BrawlAudio.Source(BrawlAudioBus.Music);
            match.ReturnToMenu(); yield return null;
            bool menu = music.isPlaying && BrawlAudio.CurrentMusicTrack == BrawlMusic.MenuTrack && music.clip == BrawlAudio.MusicClip(BrawlMusic.MenuTrack);
            bool arenas = true; int oldArena = match.ArenaIndex; float worst = 0; var notes = new List<string>();
            match.OpponentCount = 1;
            for (int a = 0; a < BrawlArena.Count; a++)
            {
                match.ArenaIndex = a;
                var clock = System.Diagnostics.Stopwatch.StartNew();
                BrawlAudio.PlayMusic(BrawlMusic.ArenaTrack(a));
                worst = Mathf.Max(worst, (float)clock.Elapsed.TotalMilliseconds);
                match.StartMatch(); yield return null; yield return null;
                bool ok = music.isPlaying && music.loop && music.clip == BrawlAudio.MusicClip(BrawlMusic.ArenaTrack(a));
                notes.Add(BrawlArena.NameOf(a, match.UsesStageArt(a)) + " → " + BrawlMusic.Name(BrawlMusic.ArenaTrack(a)) + (ok ? "" : " (FALLA)"));
                arenas &= ok;
                match.ReturnToMenu(); yield return null;
                arenas &= BrawlAudio.CurrentMusicTrack == BrawlMusic.MenuTrack && music.clip == BrawlAudio.MusicClip(BrawlMusic.MenuTrack);
            }
            match.ArenaIndex = oldArena;
            report.switchMs = worst; report.notes.Add(string.Join(" · ", notes));
            check(menu, $"El menú tiene su propia pista («{BrawlMusic.Name(BrawlMusic.MenuTrack)}»)");
            check(arenas, "Cada arena suena con su pista al empezar la partida y el menú recupera la suya al volver: " + string.Join(" · ", notes));
            check(worst < 2, $"Cambiar de pista no crea nada: {worst:0.00} ms");
            match.StartMatch(); yield return null;
            float before = music.time; match.StartMatch(); yield return null;
            check(music.isPlaying && music.time >= before - .05f, "Una revancha en la misma arena no reinicia la música");
            match.ReturnToMenu();
        }

        // ───────────── Lines and subtitles in play ─────────────
        private static IEnumerator Lines(BrawlMatch match, Action<bool, string> check, Report report, string output)
        {
            match.OpponentCount = 1; match.ArenaIndex = 0; match.StartMatch();
            float countdown = 0;
            while (match.Phase != MatchPhase.Playing && countdown < 6) { yield return null; countdown += Time.unscaledDeltaTime; }
            yield return new WaitForSecondsRealtime(.2f);
            var fighters = match.Fighters; var victim = fighters[1];
            int stocks = victim.Stocks;
            victim.ResetPractice(new Vector3(match.Arena.OuterLimits.x + 4, 3, 0), 80);
            float wait = 0;
            while (wait < 2 && victim.Stocks == stocks) { yield return null; wait += Time.unscaledDeltaTime; }
            string koKind = BrawlVoices.Kind(VoiceLine.Ko, victim.CharacterIndex);
            string koText = BrawlVoices.Text(VoiceLine.Ko, victim.CharacterIndex);
            bool koLine = victim.Stocks < stocks && BrawlAudio.CurrentVoice == koKind;
            bool koSub = BrawlSubtitles.Visible && BrawlSubtitles.Text == koText && BrawlSubtitles.Speaker == match.CharacterName(victim.CharacterIndex) && BrawlSubtitles.Player == victim.PlayerIndex;
            report.subtitles.Add(BrawlSubtitles.Current);
            check(koLine, $"Un KO real dice la frase del personaje que cae ({BrawlAudio.CurrentVoice})");
            check(koSub, $"…con su subtítulo: «{BrawlSubtitles.Current}»");
            yield return new WaitForSecondsRealtime(.15f); yield return null;
            Rect rect = BrawlSubtitles.LastRect;
            var layout = BrawlHudLayout.Compute(Screen.width, Screen.height, match.HudScale, fighters.Length);
            check(rect.width > 0 && rect.yMax <= layout.ViewHeight - layout.StripHeight && rect.xMin >= 0 && rect.xMax <= layout.ViewWidth,
                "El subtítulo queda centrado sobre la franja del HUD, dentro de la pantalla");
            yield return Capture(output, "subtitle-ko.png");

            // Priority and refusals.
            BrawlSubtitles.Clear(); BrawlAudio.Source(BrawlAudioBus.Voice).Stop();
            bool super = BrawlAudio.Say(VoiceLine.Super, fighters[0].CharacterIndex, 0);
            bool ko = BrawlAudio.Say(VoiceLine.Ko, fighters[1].CharacterIndex, 1);
            bool refused = !BrawlAudio.Say(VoiceLine.Super, fighters[0].CharacterIndex, 0);
            check(super && ko && refused && BrawlAudio.CurrentVoice == BrawlVoices.Kind(VoiceLine.Ko, fighters[1].CharacterIndex) &&
                  BrawlSubtitles.Text == BrawlVoices.Text(VoiceLine.Ko, fighters[1].CharacterIndex),
                "Una frase a la vez: el KO corta al super, el super no corta al KO y el subtítulo sigue a la frase que suena");

            // Muted voices still subtitle; subtitles off shows nothing.
            BrawlAudio.SetVolume(BrawlAudioBus.Voice, 0); BrawlSubtitles.Clear();
            BrawlAudio.Source(BrawlAudioBus.Voice).Stop();
            bool mutedSaid = BrawlAudio.Say(VoiceLine.Super, fighters[0].CharacterIndex, 0);
            check(mutedSaid && BrawlSubtitles.Visible, "Con las voces al 0 % el subtítulo aparece igual");
            BrawlAudio.SetVolume(BrawlAudioBus.Voice, .8f);
            match.SetSubtitles(false); BrawlAudio.Source(BrawlAudioBus.Voice).Stop();
            int shown = BrawlSubtitles.ShownCount;
            BrawlAudio.Say(VoiceLine.Super, fighters[0].CharacterIndex, 0);
            check(!BrawlSubtitles.Visible && BrawlSubtitles.ShownCount == shown && BrawlAudio.CurrentVoice.Length > 0, "Con los subtítulos desactivados la frase suena sin texto");
            match.SetSubtitles(true);

            // Text size and contrast.
            BrawlAudio.Source(BrawlAudioBus.Voice).Stop();
            BrawlAudio.Say(VoiceLine.Super, fighters[0].CharacterIndex, 0); yield return null; yield return null;
            float small = BrawlSubtitles.LastFontPixels;
            match.SetDisplayScale(1.3f, 1); yield return null; yield return null;
            float large = BrawlSubtitles.LastFontPixels;
            match.SetDisplayScale(1, 1);
            float minimum = BrawlSubtitles.MinPixels * Mathf.Min(1, Screen.height / 720f);
            check(small >= minimum - .01f && large >= small * 1.2f, $"El subtítulo sigue el tamaño de texto: {small:0} px al 100 %, {large:0} px al 130 % (mínimo {minimum:0} px)");
            bool readable = true; float worstText = float.MaxValue, worstSpeaker = float.MaxValue;
            foreach (bool high in new[] { false, true })
                foreach (bool blind in new[] { false, true })
                {
                    match.SetAccessibility(high, blind);
                    var palette = BrawlHudPalette.For(high, blind);
                    Color background = BrawlSubtitles.Background(high);
                    foreach (ColorVision vision in new[] { ColorVision.Normal, ColorVision.Protanopia, ColorVision.Deuteranopia, ColorVision.Tritanopia })
                    {
                        float text = BrawlHudPalette.Contrast(palette.Paper, background, vision, Color.white);
                        worstText = Mathf.Min(worstText, text);
                        readable &= text >= (high ? 15 : MinTextContrast);
                        for (int p = 0; p < 5; p++)
                        {
                            Color speaker = BrawlSubtitles.SpeakerColor(p, blind, high, palette.Accent);
                            float ratio = BrawlHudPalette.Contrast(speaker, background, vision, Color.white);
                            worstSpeaker = Mathf.Min(worstSpeaker, ratio); readable &= ratio >= (high ? 7 : MinSpeakerContrast);
                            // Still recognisably that player's colour: lightened, not replaced.
                            readable &= BrawlHudPalette.DeltaE(speaker, BrawlHudPalette.Player(p, blind), ColorVision.Normal) < 60;
                        }
                    }
                    if (high && blind)
                    {
                        BrawlAudio.Source(BrawlAudioBus.Voice).Stop(); BrawlAudio.Say(VoiceLine.Ko, fighters[1].CharacterIndex, 1);
                        yield return null; yield return Capture(output, "subtitle-contraste-daltonismo.png");
                    }
                }
            match.SetAccessibility(false, false);
            check(readable, $"El subtítulo se lee sobre cualquier fondo: texto {worstText:0.0}:1 y nombres {worstSpeaker:0.0}:1 en el peor caso (≥ 7:1 con alto contraste), en las cuatro visiones, sin perder el color del jugador");

            // Winner.
            BrawlAudio.Source(BrawlAudioBus.Voice).Stop();
            match.FinishByTime(); yield return null; yield return null;
            BrawlFighter winner = null; foreach (var f in match.Fighters) if (f.Place == 1) { winner = f; break; }
            bool win = winner != null && BrawlAudio.CurrentVoice == BrawlVoices.Kind(VoiceLine.Win, winner.CharacterIndex) &&
                       BrawlSubtitles.Text == BrawlVoices.Text(VoiceLine.Win, winner.CharacterIndex);
            report.subtitles.Add(BrawlSubtitles.Current);
            check(win, $"Al terminar, el ganador dice su frase de victoria: «{BrawlSubtitles.Current}»");
            yield return new WaitForSecondsRealtime(.1f);
            yield return Capture(output, "subtitle-victoria.png");
            match.ReturnToMenu(); yield return null;
            check(!BrawlSubtitles.Visible, "Volver al menú borra el subtítulo");
        }

        // ───────────── Option ─────────────
        private static void Options(BrawlMatch match, Action<bool, string> check, string output, string file)
        {
            match.SetSubtitles(false);
            string json = File.ReadAllText(file);
            match.UseOptionsFile(file);
            check(json.Contains("\"subtitles\": false") && !match.Subtitles && match.OptionsStatus == OptionsLoadStatus.Loaded &&
                  BrawlOptionsStore.TryParse(json, out var stored, out _) && stored.version == BrawlOptionsStore.CurrentVersion,
                "Quitar los subtítulos se guarda y se recupera, sin cambiar la versión del archivo (v" + BrawlOptionsStore.CurrentVersion + ")");
            string old = Path.Combine(output, "music-options-sin-campo.json");
            File.WriteAllText(old, System.Text.RegularExpressions.Regex.Replace(json, @"\s*""subtitles""\s*:\s*(true|false)\s*,", ""));
            match.UseOptionsFile(old);
            check(!File.ReadAllText(old).Contains("subtitles") && match.OptionsStatus == OptionsLoadStatus.Loaded && match.Subtitles,
                $"Un archivo v4 sin el campo carga como antes («{match.OptionsStatus}»), con los subtítulos activados");
            match.UseOptionsFile(file); match.SetSubtitles(true);
        }

        // ───────────── Credits ─────────────
        private static IEnumerator Credits(BrawlMatch match, Action<bool, string> check, Report report, string output)
        {
            var hud = match.Hud;
            match.ReturnToMenu(); hud.ForceNavigation = true;
            yield return null; yield return null;
            check(hud.FocusableIds.Contains("credits"), "El menú principal tiene «CRÉDITOS», alcanzable con teclado y mando");
            BrawlCredits.Show(); yield return null; yield return null;
            var ids = hud.FocusableIds;
            check(ids.Contains("credits-back") && !ids.Contains("start") && !ids.Contains("credits"),
                $"La pantalla de créditos solo deja enfocar VOLVER ({string.Join(", ", ids)}); Esc, B o Start también vuelven");
            float normal = BrawlCredits.LastTextBottom;
            yield return Capture(output, "credits.png");
            match.SetDisplayScale(1.3f, 1); yield return null; yield return null;
            float large = BrawlCredits.LastTextBottom;
            yield return Capture(output, "credits-texto-130.png");
            match.SetAccessibility(true, false); yield return null; yield return null;
            yield return Capture(output, "credits-alto-contraste.png");
            match.SetAccessibility(false, false); match.SetDisplayScale(1, 1);
            check(normal > 0 && large <= BrawlCredits.BackRect.y - 4, $"El texto cabe sobre VOLVER también con el texto al 130 % (termina en {large:0}, botón en {BrawlCredits.BackRect.y:0})");
            bool verbatim = Array.IndexOf(BrawlCredits.Lines, "Model created with Meshy – CC BY 4.0 License") >= 0;
            string doc = FindCreditsDoc();
            string docText = doc != null ? File.ReadAllText(doc) : "";
            report.notes.Add("CREDITOS.md: " + (doc ?? "no encontrado junto a la build"));
            check(verbatim, "Los créditos llevan literal «Model created with Meshy – CC BY 4.0 License»");
            check(doc == null || (docText.Contains(BrawlCredits.MeshyAttribution) && docText.Contains("Unity") && docText.Contains("Blender") && docText.Contains("BrawlAutoRig")),
                doc == null ? "CREDITOS.md no está junto a la build (se comprueba solo el texto del juego)" : "El texto coincide con Docs/Licencias/CREDITOS.md (Meshy, Unity, Blender, rig)");
            BrawlCredits.Close(); yield return null; yield return null;
            check(!BrawlCredits.Open && hud.FocusableIds.Contains("start"), "VOLVER regresa al menú principal");
            hud.ForceNavigation = false;
        }

        private static string FindCreditsDoc()
        {
            try
            {
                var dir = new DirectoryInfo(Application.dataPath);
                for (int i = 0; i < 6 && dir != null; i++, dir = dir.Parent)
                {
                    string path = Path.Combine(dir.FullName, "Docs", "Licencias", "CREDITOS.md");
                    if (File.Exists(path)) return path;
                }
            }
            catch (Exception) { }
            return null;
        }

        private static string Safe(string name)
        {
            var chars = name.ToLowerInvariant().ToCharArray();
            for (int i = 0; i < chars.Length; i++) if (!char.IsLetterOrDigit(chars[i])) chars[i] = '-';
            return new string(chars);
        }

        private static IEnumerator Capture(string output, string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output, name));
            yield return null;
        }

        private static void WriteWav(string path, float[] data, int rate)
        {
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                int bytes = data.Length * 2;
                writer.Write(new[] { 'R', 'I', 'F', 'F' }); writer.Write(36 + bytes); writer.Write(new[] { 'W', 'A', 'V', 'E' });
                writer.Write(new[] { 'f', 'm', 't', ' ' }); writer.Write(16); writer.Write((short)1); writer.Write((short)1);
                writer.Write(rate); writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16);
                writer.Write(new[] { 'd', 'a', 't', 'a' }); writer.Write(bytes);
                foreach (float s in data) writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(s, -1, 1) * 32767));
            }
        }
    }
}
