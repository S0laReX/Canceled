using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// Explicit smoke-test path only (<c>-audio-smoke</c>, AUDIO-428 to 434): every sound reaches its bus, bus volumes
    /// follow Master × bus, a bus at 0 % is silent at the real listener output while the others still sound, the
    /// voice bus plays one phrase at a time, bus volumes are saved and reloaded (v1/v2/v3 files migrate), and the
    /// AUDIO options tab is reachable with focus. Restores the original options at the end.
    /// </summary>
    public static class BrawlAudioChecks
    {
        [Serializable] private sealed class Route { public string kind, bus; }
        [Serializable] private sealed class Level { public string bus; public float audible, silenced; }
        [Serializable] private sealed class Report
        {
            public string version = BrawlMatch.ProductionVersion;
            public List<Route> routes = new List<Route>();
            public List<Level> output = new List<Level>();
            public float masterSilenced;
        }

        private const float Audible = 1e-3f, Silent = 1e-4f;
        private static readonly float[] samples = new float[1024];

        public static IEnumerator Run(BrawlMatch match, Action<bool, string> check, string output)
        {
            string folder = Path.Combine(output, "audio-checks");
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            Directory.CreateDirectory(folder);
            string originalPath = match.OptionsPath;
            var report = new Report();
            var hud = match.Hud;
            Keyboard keys = null;
            try
            {
                match.AutomatedTest = true; match.ReturnToMenu();
                string file = Path.Combine(folder, "audio.json");
                match.UseOptionsFile(file);

                // ── Buses and routing ──
                var sources = new HashSet<AudioSource>();
                bool sourcesOk = BrawlAudio.Source(BrawlAudioBus.Master) == null;
                for (int bus = 1; bus < BrawlAudio.BusCount; bus++)
                {
                    var source = BrawlAudio.Source((BrawlAudioBus)bus);
                    sourcesOk &= source != null && sources.Add(source) && source.spatialBlend == 0 && source.gameObject.name == "Bus " + BrawlAudio.BusNames[bus];
                }
                check(sourcesOk && BrawlAudio.BusNames.Length == 5, "Cinco buses: Master y una fuente propia para Música, SFX, Voces y UI");
                var music = BrawlAudio.Source(BrawlAudioBus.Music);
                check(music.isPlaying && music.loop && music.clip != null && BrawlAudio.Route("music") == BrawlAudioBus.Music, "La música suena en bucle por el bus Música");

                bool effectsOk = true, uiOk = true, voicesOk = true;
                foreach (string kind in BrawlAudio.EffectKinds)
                {
                    int before = BrawlAudio.PlayCount(BrawlAudioBus.Sfx);
                    BrawlFeedback.Sound(kind);
                    effectsOk &= BrawlAudio.Route(kind) == BrawlAudioBus.Sfx && BrawlAudio.PlayCount(BrawlAudioBus.Sfx) == before + 1;
                    report.routes.Add(new Route { kind = kind, bus = BrawlAudio.Route(kind).ToString() });
                }
                foreach (string kind in BrawlAudio.UiKinds)
                {
                    int before = BrawlAudio.PlayCount(BrawlAudioBus.Ui);
                    BrawlFeedback.Sound(kind);
                    uiOk &= BrawlAudio.LastBus == BrawlAudioBus.Ui && BrawlAudio.PlayCount(BrawlAudioBus.Ui) == before + 1;
                    report.routes.Add(new Route { kind = kind, bus = BrawlAudio.Route(kind).ToString() });
                }
                foreach (string kind in BrawlAudio.VoiceKinds)
                {
                    int before = BrawlAudio.PlayCount(BrawlAudioBus.Voice);
                    BrawlFeedback.Sound(kind);
                    voicesOk &= BrawlAudio.LastBus == BrawlAudioBus.Voice && BrawlAudio.PlayCount(BrawlAudioBus.Voice) == before + 1;
                    report.routes.Add(new Route { kind = kind, bus = BrawlAudio.Route(kind).ToString() });
                }
                check(effectsOk, $"Los {BrawlAudio.EffectKinds.Length} efectos del juego (salto, golpe, KO, super…) van al bus SFX");
                check(uiOk, "Sonidos de interfaz (foco, confirmar, pausa) van al bus UI");
                check(voicesOk, "Frases de personaje van al bus Voces");
                int voices = BrawlAudio.PlayCount(BrawlAudioBus.Voice);
                BrawlFeedback.Sound("ko");
                check(BrawlAudio.PlayCount(BrawlAudioBus.Voice) == voices + 1 && BrawlAudio.CurrentVoice == "voice-ko", "El KO suena en SFX y añade su frase en Voces");

                // ── One phrase at a time; KO has priority over the super warning ──
                BrawlAudio.Source(BrawlAudioBus.Voice).Stop();
                BrawlAudio.Play("voice-super"); BrawlAudio.Play("voice-ko");
                bool koInterrupts = BrawlAudio.CurrentVoice == "voice-ko";
                BrawlAudio.Play("voice-super");
                check(koInterrupts && BrawlAudio.CurrentVoice == "voice-ko", "Una sola frase a la vez: el KO interrumpe al super y el super no corta al KO");
                BrawlAudio.Source(BrawlAudioBus.Voice).Stop();

                // ── Volumes: gain × Master × bus ──
                BrawlAudio.SetVolumes(.5f, .4f, .6f, .8f, 1f);
                bool formula = true;
                for (int bus = 1; bus < BrawlAudio.BusCount; bus++)
                    formula &= Mathf.Abs(BrawlAudio.Source((BrawlAudioBus)bus).volume - BrawlAudio.BusGain[bus] * .5f * BrawlAudio.Volume((BrawlAudioBus)bus)) < 1e-5f;
                check(formula, "Cada fuente suena a ganancia × Master × bus");
                BrawlAudio.SetVolumes(7, -2, float.NaN, .5f, .5f);
                check(BrawlAudio.Volume(BrawlAudioBus.Master) == 1 && BrawlAudio.Volume(BrawlAudioBus.Music) == 0 &&
                      Mathf.Approximately(BrawlAudio.Volume(BrawlAudioBus.Sfx), BrawlAudio.DefaultVolumes[2]), "Volúmenes fuera de rango se limitan a 0–100 %");

                bool isolated = true;
                for (int muted = 1; muted < BrawlAudio.BusCount; muted++)
                {
                    BrawlAudio.SetVolumes(1, 1, 1, 1, 1); BrawlAudio.SetVolume((BrawlAudioBus)muted, 0);
                    for (int bus = 1; bus < BrawlAudio.BusCount; bus++)
                    {
                        float volume = BrawlAudio.Source((BrawlAudioBus)bus).volume;
                        isolated &= bus == muted ? volume == 0 && BrawlAudio.IsSilent((BrawlAudioBus)bus) : volume > 0 && !BrawlAudio.IsSilent((BrawlAudioBus)bus);
                    }
                }
                check(isolated, "Silenciar un bus no afecta a los otros tres");
                BrawlAudio.SetVolumes(0, 1, 1, 1, 1);
                bool masterAll = true;
                for (int bus = 1; bus < BrawlAudio.BusCount; bus++) masterAll &= BrawlAudio.Source((BrawlAudioBus)bus).volume == 0;
                check(masterAll, "Master al 0 % silencia todos los buses");

                // ── Real output at the listener: each bus audible alone, silent at 0 % ──
                bool outputOk = true;
                foreach (var bus in new[] { BrawlAudioBus.Music, BrawlAudioBus.Sfx, BrawlAudioBus.Voice, BrawlAudioBus.Ui })
                {
                    var level = new Level { bus = bus.ToString() };
                    yield return Measure(bus, 1, v => level.audible = v);
                    yield return Measure(bus, 0, v => level.silenced = v);
                    report.output.Add(level);
                    outputOk &= level.audible > Audible && level.silenced < Silent;
                }
                check(outputOk, "Salida real del oyente: cada bus suena solo y calla al 0 %");
                BrawlAudio.SetVolumes(0, 1, 1, 1, 1);
                yield return new WaitForSecondsRealtime(.1f);
                float peak = 0;
                BrawlAudio.Play("warning"); BrawlAudio.Play("ui-confirm"); BrawlAudio.Play("voice-ko");
                for (int frame = 0; frame < 12; frame++) { yield return null; peak = Mathf.Max(peak, Rms()); }
                report.masterSilenced = peak;
                check(peak < Silent, "Salida real con Master al 0 %: silencio con música, efectos, voz e interfaz sonando");
                BrawlAudio.Source(BrawlAudioBus.Voice).Stop();

                // ── Saved and reloaded ──
                match.SetBusVolume(BrawlAudioBus.Master, .9f); match.SetBusVolume(BrawlAudioBus.Music, .3f);
                match.SetBusVolume(BrawlAudioBus.Sfx, .6f); match.SetBusVolume(BrawlAudioBus.Voice, 0); match.SetBusVolume(BrawlAudioBus.Ui, .4f);
                bool stored = BrawlOptionsStore.TryParse(File.ReadAllText(file), out var saved, out _) && saved.version == 4 &&
                              Mathf.Approximately(saved.master, .9f) && Mathf.Approximately(saved.music, .3f) && Mathf.Approximately(saved.sfx, .6f) &&
                              saved.voice == 0 && Mathf.Approximately(saved.ui, .4f);
                check(stored, "Los cinco volúmenes se guardan en el formato versión 4");
                BrawlAudio.SetVolumes(1, 1, 1, 1, 1);
                match.UseOptionsFile(file);
                check(match.OptionsStatus == OptionsLoadStatus.Loaded && Mathf.Approximately(BrawlAudio.Volume(BrawlAudioBus.Master), .9f) &&
                      Mathf.Approximately(BrawlAudio.Volume(BrawlAudioBus.Music), .3f) && Mathf.Approximately(BrawlAudio.Volume(BrawlAudioBus.Sfx), .6f) &&
                      BrawlAudio.IsSilent(BrawlAudioBus.Voice) && Mathf.Approximately(BrawlAudio.Volume(BrawlAudioBus.Ui), .4f) &&
                      BrawlAudio.Source(BrawlAudioBus.Voice).volume == 0, "Al recargar se recuperan los cinco volúmenes, también un bus en silencio");

                // ── Older files migrate: SFX and Música kept, new buses at their defaults ──
                string legacyCore = "\"reducedEffects\":false,\"sfx\":0.3,\"music\":0.7,\"localCoop\":false,\"p1UsesGamepad\":false,";
                string v2Fields = "\"moveDeadzone\":0.2,\"aimDeadzone\":0.25,\"stickResponse\":1,\"vibration\":0.75,";
                string v3Fields = "\"textScale\":1,\"hudScale\":1,";
                string[] legacy =
                {
                    "{\"format\":\"funados-options\",\"version\":1," + legacyCore + "\"keyboard\":[],\"gamepad\":[]}",
                    "{\"format\":\"funados-options\",\"version\":2," + legacyCore + v2Fields + "\"keyboard\":[],\"gamepad\":[]}",
                    "{\"format\":\"funados-options\",\"version\":3," + legacyCore + v2Fields + v3Fields + "\"keyboard\":[],\"gamepad\":[]}"
                };
                for (int v = 0; v < legacy.Length; v++)
                {
                    string path = Path.Combine(folder, "v" + (v + 1) + ".json");
                    File.WriteAllText(path, legacy[v]);
                    match.UseOptionsFile(path);
                    bool rewritten = BrawlOptionsStore.TryParse(File.ReadAllText(path), out var again, out _, out bool stillOld) && again.version == 4 && !stillOld;
                    check(match.OptionsStatus == OptionsLoadStatus.MigratedLegacy && Mathf.Approximately(BrawlFeedback.SfxVolume, .3f) &&
                          Mathf.Approximately(BrawlFeedback.MusicVolume, .7f) && BrawlAudio.Volume(BrawlAudioBus.Master) == BrawlAudio.DefaultVolumes[0] &&
                          Mathf.Approximately(BrawlAudio.Volume(BrawlAudioBus.Voice), BrawlAudio.DefaultVolumes[3]) &&
                          Mathf.Approximately(BrawlAudio.Volume(BrawlAudioBus.Ui), BrawlAudio.DefaultVolumes[4]) && rewritten,
                        $"Archivo versión {v + 1} carga: conserva SFX y Música, Master/Voces/UI por defecto y se reescribe en v4");
                }
                string v4 = "{\"format\":\"funados-options\",\"version\":4," + legacyCore + v2Fields + v3Fields;
                string incomplete = Path.Combine(folder, "incompleto.json");
                File.WriteAllText(incomplete, v4 + "\"master\":0.5,\"ui\":0.5,\"keyboard\":[],\"gamepad\":[]}");
                BrawlOptionsStore.Load(incomplete, out var incompleteStatus, out _);
                check(incompleteStatus == OptionsLoadStatus.ResetCorrupt, "Archivo v4 sin el volumen de Voces no se acepta a medias");
                string outOfRange = Path.Combine(folder, "rango.json");
                File.WriteAllText(outOfRange, v4 + "\"master\":5,\"voice\":-1,\"ui\":0.5,\"keyboard\":[],\"gamepad\":[]}");
                var repairedData = BrawlOptionsStore.Load(outOfRange, out var rangeStatus, out _);
                check(rangeStatus == OptionsLoadStatus.RepairedFields && repairedData.master == 1 && repairedData.voice == 0, "Volúmenes de bus fuera de rango se corrigen al cargar");

                // ── In play: gameplay sounds reach SFX, pausing reaches UI ──
                match.UseOptionsFile(file);
                match.StartTraining(); yield return new WaitForSeconds(.4f);
                var fighter = match.Fighters[0];
                int sfxBefore = BrawlAudio.PlayCount(BrawlAudioBus.Sfx);
                fighter.Submit(new BrawlCommand { Jump = true }); yield return new WaitForSeconds(.15f); fighter.ClearInput();
                check(BrawlAudio.PlayCount(BrawlAudioBus.Sfx) > sfxBefore && fighter.JumpsUsed >= 1, "En partida, el salto suena por el bus SFX");
                int uiBefore = BrawlAudio.PlayCount(BrawlAudioBus.Ui);
                match.TogglePause(); yield return null;
                bool paused = match.Phase == MatchPhase.Paused && BrawlAudio.LastKind == "ui-pause";
                match.TogglePause(); yield return null;
                check(paused && BrawlAudio.PlayCount(BrawlAudioBus.Ui) == uiBefore + 2, "Pausar y reanudar suenan por el bus UI");
                match.ReturnToMenu(); yield return null;

                // ── Options tab AUDIO, reachable with focus ──
                hud.ForceNavigation = true;
                keys = InputSystem.AddDevice<Keyboard>();
                match.OptionsPage = 3; yield return null; yield return null; yield return null;
                var ids = new List<string>(hud.FocusableIds);
                check(ids.Contains("tab-audio") && ids.Contains("audio-master") && ids.Contains("audio-music") && ids.Contains("audio-sfx") &&
                      ids.Contains("audio-voice") && ids.Contains("audio-ui"), "Pestaña AUDIO con una fila por bus, alcanzable con foco");
                yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, "audio-options.png"));
                yield return null;
                hud.Focus("audio-voice");
                yield return Tap(keys, Key.RightArrow);
                bool raised = Mathf.Approximately(BrawlAudio.Volume(BrawlAudioBus.Voice), .1f);
                yield return Tap(keys, Key.LeftArrow);
                check(raised && BrawlAudio.IsSilent(BrawlAudioBus.Voice) && hud.FocusId == "audio-voice" &&
                      BrawlOptionsStore.TryParse(File.ReadAllText(file), out var afterMenu, out _) && afterMenu.voice == 0,
                    "Izquierda/derecha ajustan el bus enfocado y se guarda");

                File.WriteAllText(Path.Combine(output, "audio-report.json"), JsonUtility.ToJson(report, true));
            }
            finally
            {
                hud.ForceNavigation = false;
                if (keys != null && keys.added) InputSystem.RemoveDevice(keys);
                match.OptionsPage = 0; match.ReturnToMenu();
                match.UseOptionsFile(originalPath);
                match.AutomatedTest = true;
            }
            yield return new WaitForSecondsRealtime(.3f);
        }

        /// <summary>Only <paramref name="bus"/> on (at <paramref name="volume"/>), plays its sound and keeps the loudest frame.</summary>
        private static IEnumerator Measure(BrawlAudioBus bus, float volume, Action<float> result)
        {
            BrawlAudio.SetVolumes(1, 0, 0, 0, 0);
            BrawlAudio.Source(BrawlAudioBus.Voice).Stop();
            yield return new WaitForSecondsRealtime(.6f); // let earlier one-shots end
            BrawlAudio.SetVolume(bus, volume);
            yield return new WaitForSecondsRealtime(.05f);
            if (bus == BrawlAudioBus.Sfx) BrawlAudio.Play("warning");
            else if (bus == BrawlAudioBus.Voice) BrawlAudio.Play("voice-ko");
            else if (bus == BrawlAudioBus.Ui) BrawlAudio.Play("ui-confirm");
            float peak = 0;
            for (int frame = 0; frame < 12; frame++) { yield return null; peak = Mathf.Max(peak, Rms()); }
            result(peak);
        }

        private static float Rms()
        {
            float sum = 0;
            for (int channel = 0; channel < 2; channel++)
            {
                AudioListener.GetOutputData(samples, channel);
                foreach (float s in samples) sum += s * s;
            }
            return Mathf.Sqrt(sum / (samples.Length * 2));
        }

        private static IEnumerator Tap(Keyboard keys, Key key)
        {
            InputSystem.QueueStateEvent(keys, new KeyboardState(key)); yield return null;
            InputSystem.QueueStateEvent(keys, new KeyboardState()); yield return null;
        }
    }
}
