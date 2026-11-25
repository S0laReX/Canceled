using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// <c>-sfx-smoke</c>: layered hit, finisher, dodge, launch, recovery and KO sounds (GDD 2.0 §10). Checks clean
    /// clips, that impact, dodge and KO differ in measurable ways, that they stand out over the music at 100 % with
    /// voices at 0 %, the combo ladder, in-match routing, and that every event keeps a visual cue with audio off.
    /// Writes each sound as a WAV for listening.
    /// </summary>
    public static class BrawlSfxChecks
    {
        [Serializable] private sealed class Row { public string kind; public float seconds, rms, peak, brightness, attack, salience; }
        [Serializable] private sealed class Report { public float musicRms, musicBrightness; public List<Row> rows = new List<Row>(); public float[] ladder; public List<string> played = new List<string>(); }

        private static IEnumerator Setup(BrawlMatch match, float targetX, float targetY = 0)
        {
            match.SelectedCharacter = 0; match.ArenaIndex = 0; match.StartTraining();
            match.Fighters[0].ResetPractice(new Vector3(0, .05f, 0), 0);
            match.Fighters[1].ResetPractice(new Vector3(targetX, targetY + .05f, 0), 0);
            yield return new WaitForSeconds(.25f);
        }

        public static IEnumerator Run(BrawlMatch match, Action<bool, string> check, string output)
        {
            match.AutomatedTest = true;
            var report = new Report();
            Directory.CreateDirectory(Path.Combine(output, "sfx"));
            var features = new Dictionary<string, Row>();

            // Music as heard at 100 % (master 100 %).
            var musicSource = BrawlAudio.Source(BrawlAudioBus.Music);
            float[] music = new float[musicSource && musicSource.clip ? musicSource.clip.samples : 0];
            if (music.Length > 0) musicSource.clip.GetData(music, 0);
            BrawlSfxSynth.Features(music, out _, out report.musicRms, out report.musicBrightness, out _);
            float musicLevel = report.musicRms * BrawlAudio.BusGain[(int)BrawlAudioBus.Music];

            bool clean = true;
            foreach (string kind in BrawlSfxSynth.Kinds)
            {
                float[] data = BrawlSfxSynth.Build(kind, 1);
                BrawlSfxSynth.Features(data, out float seconds, out float rms, out float brightness, out float attack);
                float peak = 0; bool finite = true;
                foreach (float s in data) { peak = Mathf.Max(peak, Mathf.Abs(s)); finite &= !float.IsNaN(s) && !float.IsInfinity(s); }
                // Salience: loudness of the first 80 ms at the SFX bus default volume against the music at 100 %.
                int head = Mathf.Min(data.Length, BrawlSfxSynth.Rate * 80 / 1000); double sum = 0;
                for (int i = 0; i < head; i++) sum += data[i] * data[i];
                float headRms = Mathf.Sqrt((float)(sum / Mathf.Max(1, head)));
                float salience = headRms * BrawlAudio.BusGain[(int)BrawlAudioBus.Sfx] * BrawlAudio.DefaultVolumes[(int)BrawlAudioBus.Sfx] / Mathf.Max(1e-5f, musicLevel);
                var row = new Row { kind = kind, seconds = seconds, rms = rms, peak = peak, brightness = brightness, attack = attack, salience = salience };
                report.rows.Add(row); features[kind] = row;
                clean &= finite && peak <= .901f && peak > .2f && rms > .02f && Mathf.Abs(data[0]) < .05f && Mathf.Abs(data[data.Length - 1]) < .05f;
                WriteWav(Path.Combine(output, "sfx", kind + ".wav"), data);
            }
            check(clean, "Los siete efectos por capas son limpios: pico ≤ 0,9 (sin recortar), con nivel y sin clic al empezar o acabar");

            Row hit = features["hit-heavy"], light = features["hit-light"], dodge = features["dodge"], ko = features["ko"], spike = features["hit-spike"];
            check(hit.attack < .02f && dodge.attack > .05f, $"La esquiva no tiene golpe seco: ataque {dodge.attack * 1000:0} ms frente a {hit.attack * 1000:0} ms del impacto");
            check(dodge.brightness > hit.brightness * 1.5f, $"La esquiva es más aguda que el impacto (brillo {dodge.brightness:0.000} frente a {hit.brightness:0.000})");
            check(ko.seconds >= 2 * hit.seconds && ko.rms > light.rms, $"El KO dura más del doble que un impacto ({ko.seconds:0.00} s) y suena más que un golpe ligero");
            check(spike.seconds > hit.seconds && Mathf.Abs(spike.brightness - hit.brightness) > .001f, "El remate hacia abajo se distingue del golpe fuerte");
            bool audible = true;
            foreach (string kind in new[] { "hit-light", "hit-heavy", "hit-spike", "dodge", "ko" })
                audible &= features[kind].salience > 1.5f || features[kind].brightness > report.musicBrightness * 4;
            check(audible, "Con la música al 100 % y las voces al 0 %, impacto, remate, esquiva y KO destacan (nivel ×1,5 o 4 veces más agudos que la música)");

            // Combo ladder: one semitone per hit.
            report.ladder = new float[BrawlAudio.ComboSteps];
            bool rising = true;
            for (int step = 0; step < BrawlAudio.ComboSteps; step++)
            {
                report.ladder[step] = ZeroCrossings(BrawlSfxSynth.Build("hit-light", BrawlAudio.ComboPitch(step)), .06f, .14f);
                if (step > 0) rising &= report.ladder[step] > report.ladder[step - 1];
            }
            float ratio = report.ladder[BrawlAudio.ComboSteps - 1] / Mathf.Max(1, report.ladder[0]);
            check(rising && Mathf.Abs(ratio - BrawlAudio.ComboPitch(BrawlAudio.ComboSteps - 1)) < .15f,
                $"Escalera de combo: cada golpe sube un semitono (paso 6 / paso 1 = {ratio:0.00}, esperado {BrawlAudio.ComboPitch(BrawlAudio.ComboSteps - 1):0.00})");

            // In-match routing.
            var basic = new AttackSpec(AttackKind.Basic, 3, 0, .1f, .1f, 2, 1, 1, 1);
            var launcher = new AttackSpec(AttackKind.Heavy, 14, 0, .1f, .1f, 2, 30, 12, 1);
            yield return Setup(match, 1.4f);
            var attacker = match.Fighters[0]; var target = match.Fighters[1];
            target.ReceiveHit(basic, Vector3.right, attacker); string first = BrawlAudio.LastKind;
            target.ReceiveHit(basic, Vector3.right, attacker); string second = BrawlAudio.LastKind;
            target.ReceiveHit(basic, Vector3.right, attacker); string third = BrawlAudio.LastKind;
            report.played.Add(first); report.played.Add(second); report.played.Add(third);
            check(first == "hit-light" && second == "hit-light#1" && third == "hit-light#2", $"En partida, un combo sube de tono golpe a golpe ({first}, {second}, {third})");
            yield return Setup(match, 1.4f, 0);
            int sfx = BrawlAudio.PlayCount(BrawlAudioBus.Sfx);
            match.Fighters[1].ReceiveHit(launcher, Vector3.right, match.Fighters[0]);
            report.played.Add(BrawlAudio.LastKind);
            check(BrawlAudio.LastKind == "launch" && BrawlAudio.PlayCount(BrawlAudioBus.Sfx) == sfx + 2, "Un lanzamiento suma la capa de silbido al golpe fuerte");

            var fx = BrawlImpactFx.Instance;
            float master = BrawlAudio.Volume(BrawlAudioBus.Master);
            foreach (bool muted in new[] { false, true })
            {
                BrawlAudio.SetVolume(BrawlAudioBus.Master, muted ? 0 : master);
                string tag = muted ? "con el audio apagado" : "con audio";
                yield return Setup(match, 3);
                fx.ResetStats();
                match.Fighters[0].Submit(new BrawlCommand { Dodge = true, Move = Vector2.right });
                yield return new WaitForSeconds(.15f);
                bool dodgeSound = BrawlAudio.LastKind == "dodge";
                check(fx.PuffsEmitted > 0 && (muted || dodgeSound), $"Esquiva {tag}: {(muted ? "queda la señal visual" : "suena «dodge» y se ve")}");
                if (!muted) report.played.Add(BrawlAudio.LastKind);

                yield return Setup(match, 3);
                match.Fighters[0].ResetPractice(new Vector3(0, 3, 0), 0);
                yield return new WaitForSeconds(.1f);
                fx.ResetStats();
                match.Fighters[0].Submit(new BrawlCommand { Recovery = true, Move = Vector2.up, Aim = Vector2.up });
                yield return new WaitForSeconds(.15f);
                bool recoverySound = BrawlAudio.LastKind == "recovery";
                check(fx.PuffsEmitted > 0 && (muted || recoverySound), $"Recuperación {tag}: {(muted ? "queda la señal visual" : "suena «recovery» y se ve")}");
                if (!muted) report.played.Add(BrawlAudio.LastKind);

                yield return Setup(match, 1.4f);
                fx.ResetStats();
                match.Fighters[1].ReceiveHit(basic, Vector3.right, match.Fighters[0]);
                check(fx.RingsEmitted > 0 && (muted || BrawlAudio.LastKind.StartsWith("hit-")), $"Impacto {tag}: anillo y chispas visibles");

                fx.ResetStats();
                match.Fighters[1].ResetPractice(new Vector3(40, 9, 0), 60);
                match.Fighters[1].LoseStock();
                check(fx.LastTier == ImpactTier.Ko && (muted || BrawlAudio.LastKind.StartsWith("voice-ko") || BrawlAudio.LastKind == "ko"), $"KO {tag}: estallido de KO visible");
            }
            BrawlAudio.SetVolume(BrawlAudioBus.Master, master);
            match.ReturnToMenu();
            File.WriteAllText(Path.Combine(output, "sfx-smoke.json"), JsonUtility.ToJson(report, true));
        }

        /// <summary>Zero crossings between two times: after the noise transient, so it follows the body's pitch.</summary>
        private static float ZeroCrossings(float[] data, float from, float to)
        {
            int start = Mathf.CeilToInt(BrawlSfxSynth.Rate * from), n = Mathf.Min(data.Length, Mathf.CeilToInt(BrawlSfxSynth.Rate * to)), crossings = 0;
            for (int i = start + 1; i < n; i++) if ((data[i - 1] < 0) != (data[i] < 0)) crossings++;
            return crossings;
        }

        private static void WriteWav(string path, float[] data)
        {
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                int bytes = data.Length * 2;
                writer.Write(new[] { 'R', 'I', 'F', 'F' }); writer.Write(36 + bytes); writer.Write(new[] { 'W', 'A', 'V', 'E' });
                writer.Write(new[] { 'f', 'm', 't', ' ' }); writer.Write(16); writer.Write((short)1); writer.Write((short)1);
                writer.Write(BrawlSfxSynth.Rate); writer.Write(BrawlSfxSynth.Rate * 2); writer.Write((short)2); writer.Write((short)16);
                writer.Write(new[] { 'd', 'a', 't', 'a' }); writer.Write(bytes);
                foreach (float s in data) writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(s, -1, 1) * 32767));
            }
        }
    }
}
