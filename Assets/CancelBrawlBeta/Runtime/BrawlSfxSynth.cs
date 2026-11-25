using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// Layered procedural sound effects (GDD 2.0 §10): light hit, strong hit, downward finisher, dodge, launch,
    /// recovery and KO, each built from a body (pitched thump), a transient (filtered noise) and a tail or sweep.
    /// Deterministic (fixed noise seed per kind) and marked as replaceable placeholders: no recorded audio.
    /// </summary>
    public static class BrawlSfxSynth
    {
        public const int Rate = 22050;
        public static readonly string[] Kinds = { "hit-light", "hit-heavy", "hit-spike", "dodge", "launch", "recovery", "ko" };

        public static bool Handles(string kind) => System.Array.IndexOf(Kinds, kind) >= 0;

        /// <summary>Samples for <paramref name="kind"/>; <paramref name="pitch"/> scales every frequency (combo ladder).</summary>
        public static float[] Build(string kind, float pitch)
        {
            float duration = kind == "ko" ? .75f : kind == "recovery" ? .36f : kind == "launch" ? .32f : kind == "hit-spike" ? .3f :
                kind == "hit-heavy" ? .26f : kind == "dodge" ? .22f : .15f;
            int count = Mathf.CeilToInt(Rate * duration);
            var data = new float[count];
            var random = new System.Random(Seed(kind));
            var sweep = new Filter(); var crack = new Filter();
            float phase = 0, sweepPhase = 0;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)Rate, u = (float)i / count;
                float noise = (float)(random.NextDouble() * 2 - 1);
                float sample = 0;
                switch (kind)
                {
                    case "hit-light":
                        sample = Thump(ref phase, t, 190 * pitch, 95 * pitch, .05f, .09f) * .55f
                               + crack.High(noise, 2600 * pitch, .7f) * Decay(t, .018f) * .5f;
                        break;
                    case "hit-heavy":
                        sample = Thump(ref phase, t, 130 * pitch, 48 * pitch, .08f, .16f) * .75f
                               + crack.Band(noise, 1800 * pitch, .5f) * Decay(t, .035f) * .65f
                               + Mathf.Sin(t * 330 * pitch * Mathf.PI * 2) * Decay(t, .09f) * .12f;
                        break;
                    case "hit-spike":
                        // Thump plus a falling whistle: the finisher sends the rival down.
                        sample = Thump(ref phase, t, 150 * pitch, 38 * pitch, .1f, .2f) * .7f
                               + Tone(ref sweepPhase, Mathf.Lerp(1100, 180, Mathf.Clamp01(t / .2f)) * pitch) * Window(t, 0, .22f) * .22f
                               + crack.Band(noise, 1500 * pitch, .5f) * Decay(t, .03f) * .5f;
                        break;
                    case "dodge":
                        // No thump at all: an airy rising whoosh is easy to tell from a hit.
                        sample = sweep.Band(noise, Mathf.Lerp(700, 3200, u) * pitch, .35f) * Bell(u) * .9f;
                        break;
                    case "launch":
                        sample = sweep.Band(noise, Mathf.Lerp(350, 1700, u) * pitch, .4f) * Bell(u) * .7f
                               + Thump(ref phase, t, 110 * pitch, 60 * pitch, .05f, .08f) * .35f;
                        break;
                    case "recovery":
                        // Rising tone with a fast shimmer and a soft air layer.
                        sample = Tone(ref sweepPhase, Mathf.Lerp(420, 1250, u) * pitch) * (.55f + .45f * Mathf.Sin(t * 70)) * Bell(u) * .3f
                               + sweep.Band(noise, Mathf.Lerp(900, 2400, u) * pitch, .5f) * Bell(u) * .25f;
                        break;
                    case "ko":
                        // Low boom, a burst, a long falling sweep and a tail: the biggest event of the match.
                        sample = Thump(ref phase, t, 80 * pitch, 28 * pitch, .25f, .45f) * .85f
                               + crack.Band(noise, 1200 * pitch, .45f) * Decay(t, .08f) * .7f
                               + Tone(ref sweepPhase, Mathf.Lerp(1400, 140, Mathf.Clamp01(t / .55f)) * pitch) * Window(t, .02f, .6f) * .2f;
                        break;
                }
                // Short fade in and out so no clip clicks.
                data[i] = sample * Mathf.Min(1, i / 40f) * Mathf.Min(1, (count - i) / 220f);
            }
            // Peak normalised to 0.9: loud layers never clip, and every sound keeps the same headroom.
            float peak = 0; foreach (float s in data) peak = Mathf.Max(peak, Mathf.Abs(s));
            if (peak > .9f) for (int i = 0; i < count; i++) data[i] *= .9f / peak;
            return data;
        }

        private static int Seed(string kind) { int seed = 17; foreach (char c in kind) seed = seed * 31 + c; return seed & 0x7fffffff; }

        /// <summary>Sine whose pitch falls exponentially from <paramref name="from"/> to <paramref name="to"/>.</summary>
        private static float Thump(ref float phase, float t, float from, float to, float fall, float decay)
        {
            float f = to + (from - to) * Mathf.Exp(-t / fall);
            phase += f / Rate * Mathf.PI * 2;
            return Mathf.Sin(phase) * Decay(t, decay);
        }

        private static float Tone(ref float phase, float frequency) { phase += frequency / Rate * Mathf.PI * 2; return Mathf.Sin(phase); }
        private static float Decay(float t, float time) => Mathf.Exp(-t / time);
        private static float Bell(float u) => Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI);
        private static float Window(float t, float start, float end) => t < start || t > end ? 0 : Bell((t - start) / (end - start));

        /// <summary>State-variable filter (Chamberlin), enough for noise bands and sweeps.</summary>
        private sealed class Filter
        {
            private float low, band;
            private void Step(float x, float cutoff, float q)
            {
                float f = 2 * Mathf.Sin(Mathf.PI * Mathf.Min(cutoff, Rate * .2f) / Rate);
                low += f * band;
                float high = x - low - q * band;
                band += f * high;
            }
            public float Band(float x, float cutoff, float q) { Step(x, cutoff, q); return band; }
            public float High(float x, float cutoff, float q) { Step(x, cutoff, q); return x - low - q * band; }
        }

        // ───────────── Analysis used by the checks ─────────────

        /// <summary>Simple features: duration, RMS, share of energy above ~1.5 kHz (first difference) and attack time.</summary>
        public static void Features(float[] data, out float seconds, out float rms, out float brightness, out float attack)
        {
            seconds = data.Length / (float)Rate;
            double sum = 0, diff = 0; float peak = 0; int peakAt = 0;
            for (int i = 0; i < data.Length; i++)
            {
                sum += data[i] * data[i];
                if (i > 0) { float d = data[i] - data[i - 1]; diff += d * d; }
                if (Mathf.Abs(data[i]) > peak) { peak = Mathf.Abs(data[i]); peakAt = i; }
            }
            rms = Mathf.Sqrt((float)(sum / Mathf.Max(1, data.Length)));
            brightness = sum > 0 ? (float)(diff / sum) : 0;
            attack = peakAt / (float)Rate;
        }
    }
}
