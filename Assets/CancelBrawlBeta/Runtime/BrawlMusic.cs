using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// Generated music (GDD 2.0 §10): one loop for the menu and one per arena, each with its own tempo, key, scale,
    /// drum pattern and lead. Eight bars rendered note by note into a buffer that wraps around, so the loop has no seam.
    /// Every track is normalised to the same loudness (a little under the old placeholder loop), so impacts, dodges and
    /// KO keep standing out at music 100 %. All tracks are built once at start-up: changing arena only swaps the clip.
    /// Placeholder music made in code, no samples or recordings; meant to be replaced by the final soundtrack.
    /// </summary>
    public static class BrawlMusic
    {
        public const int Rate = 22050, Bars = 8, StepsPerBar = 16;
        public const int MenuTrack = 0;
        /// <summary>Loudness every track is normalised to, and the peak it may never exceed.</summary>
        public const float TargetRms = .17f, MaxPeak = .9f;
        public static int TrackCount => Tracks.Length;
        public static int ArenaTrack(int arena) => 1 + Mathf.Clamp(arena, 0, Tracks.Length - 2);

        private sealed class Track
        {
            public string Name, Style; public float Bpm; public int Root; public int[] Scale; public int[] Chords;
            public string Kick, Snare, Hat, Bass; public int LeadWave; public bool Arp; public int LeadOctave; public int Seed;
            public float PadLevel, LeadLevel, BassLevel;
        }

        private static readonly int[] Major = { 0, 2, 4, 5, 7, 9, 11 }, Minor = { 0, 2, 3, 5, 7, 8, 10 },
            Dorian = { 0, 2, 3, 5, 7, 9, 10 }, Phrygian = { 0, 1, 3, 5, 7, 8, 10 };

        // Waves: 0 sine, 1 square, 2 saw. Patterns: 16 steps per bar; bass 'x' root, 'o' octave, '5' fifth.
        private static readonly Track[] Tracks =
        {
            new Track { Name = "Sala de espera", Style = "menú, tranquilo", Bpm = 100, Root = 53, Scale = Major, Chords = new[] { 0, 5, 3, 4 },
                Kick = "x.......x.......", Snare = "....x.......x...", Hat = "..x...x...x...x.", Bass = "x.......x...5...",
                LeadWave = 0, Arp = false, LeadOctave = 1, Seed = 11, PadLevel = .5f, LeadLevel = .55f, BassLevel = .8f },
            new Track { Name = "Neón de madrugada", Style = "synthwave", Bpm = 118, Root = 57, Scale = Minor, Chords = new[] { 0, 5, 2, 6 },
                Kick = "x...x...x...x...", Snare = "....x.......x...", Hat = "..x...x...x...x.", Bass = "x.x.x.x.o.x.x.x.",
                LeadWave = 2, Arp = true, LeadOctave = 1, Seed = 23, PadLevel = .45f, LeadLevel = .4f, BassLevel = .8f },
            new Track { Name = "Rebajas del ego", Style = "funk", Bpm = 104, Root = 50, Scale = Dorian, Chords = new[] { 0, 3, 0, 3 },
                Kick = "x..x..x...x..x..", Snare = "....x.......x..x", Hat = "x.x.x.x.x.x.x.x.", Bass = "x..o..x.5.x..o.x",
                LeadWave = 1, Arp = false, LeadOctave = 1, Seed = 37, PadLevel = .3f, LeadLevel = .45f, BassLevel = .95f },
            new Track { Name = "Debate en llamas", Style = "punk rápido", Bpm = 150, Root = 52, Scale = Minor, Chords = new[] { 0, 5, 6, 0 },
                Kick = "x...x.x.x...x.x.", Snare = "....x.......x...", Hat = "x.x.x.x.x.x.x.x.", Bass = "x.x.x.x.x.x.x.x.",
                LeadWave = 2, Arp = false, LeadOctave = 1, Seed = 41, PadLevel = .35f, LeadLevel = .45f, BassLevel = .85f },
            new Track { Name = "Bloque génesis", Style = "techno", Bpm = 126, Root = 48, Scale = Phrygian, Chords = new[] { 0, 1, 0, 6 },
                Kick = "x...x...x...x...", Snare = "........x.......", Hat = "..x.xxx...x.xxx.", Bass = "..x...x...x...xo",
                LeadWave = 1, Arp = true, LeadOctave = 2, Seed = 53, PadLevel = .25f, LeadLevel = .3f, BassLevel = .9f },
            // Catalogue arenas 4-7 (Mall del Ego, Cumbre de la Liquidez, Palacio del Privilegio, Centro de Cumplimiento).
            new Track { Name = "Liquidación total", Style = "disco", Bpm = 112, Root = 55, Scale = Dorian, Chords = new[] { 0, 3, 4, 3 },
                Kick = "x...x...x...x...", Snare = "....x.......x...", Hat = ".x.x.x.x.x.x.x.x", Bass = "x.o.x.o.x.o.x.o.",
                LeadWave = 0, Arp = true, LeadOctave = 1, Seed = 67, PadLevel = .4f, LeadLevel = .4f, BassLevel = .85f },
            new Track { Name = "Mercado alcista", Style = "trance barato", Bpm = 136, Root = 51, Scale = Minor, Chords = new[] { 0, 6, 5, 4 },
                Kick = "x...x...x...x...", Snare = "........x.......", Hat = "..x...x...x...x.", Bass = ".xx..xx..xx..xx.",
                LeadWave = 2, Arp = true, LeadOctave = 2, Seed = 71, PadLevel = .35f, LeadLevel = .3f, BassLevel = .8f },
            new Track { Name = "Reloj detenido", Style = "barroco sintético", Bpm = 92, Root = 50, Scale = Major, Chords = new[] { 0, 4, 5, 3 },
                Kick = "x.......x.......", Snare = "....x.......x...", Hat = "x...x...x...x...", Bass = "x...5...o...5...",
                LeadWave = 1, Arp = true, LeadOctave = 1, Seed = 79, PadLevel = .5f, LeadLevel = .45f, BassLevel = .75f },
            new Track { Name = "Ventanilla única", Style = "lounge de oficina", Bpm = 86, Root = 56, Scale = Dorian, Chords = new[] { 0, 2, 3, 1 },
                Kick = "x.....x...x.....", Snare = "....x.......x...", Hat = "x.x.x.x.x.x.x.x.", Bass = "x.....o...5.....",
                LeadWave = 0, Arp = false, LeadOctave = 1, Seed = 83, PadLevel = .55f, LeadLevel = .5f, BassLevel = .7f },
        };

        public static string Name(int track) => Tracks[Mathf.Clamp(track, 0, Tracks.Length - 1)].Name;
        public static string Style(int track) => Tracks[Mathf.Clamp(track, 0, Tracks.Length - 1)].Style;
        public static float Bpm(int track) => Tracks[Mathf.Clamp(track, 0, Tracks.Length - 1)].Bpm;
        /// <summary>Exact loop length in samples: a whole number of 16th steps, so the beat never drifts across loops.</summary>
        public static int LoopSamples(int track) => StepSamples(Tracks[track]) * StepsPerBar * Bars;
        private static int StepSamples(Track track) => Mathf.RoundToInt(Rate * 60f / track.Bpm / 4f);

        public static AudioClip Build(int track)
        {
            float[] data = Render(track);
            var clip = AudioClip.Create("music-" + track + " " + Tracks[track].Name, data.Length, 1, Rate, false);
            clip.SetData(data, 0); return clip;
        }

        public static float[] Render(int index)
        {
            Track track = Tracks[index];
            int step = StepSamples(track), length = step * StepsPerBar * Bars;
            float[] drums = new float[length], bass = new float[length], pad = new float[length], lead = new float[length];
            uint noise = (uint)(track.Seed * 2654435761u) | 1u;
            for (int bar = 0; bar < Bars; bar++)
            {
                int chord = track.Chords[(bar / 2) % track.Chords.Length];
                int rootNote = track.Root + Degree(track, chord);
                for (int s = 0; s < StepsPerBar; s++)
                {
                    int at = (bar * StepsPerBar + s) * step;
                    // Fill on the last bar: extra snare hits lead back into the loop.
                    bool fill = bar == Bars - 1 && s >= 12;
                    if (track.Kick[s] == 'x') Kick(drums, at);
                    if (track.Snare[s] == 'x' || (fill && s % 2 == 0)) Snare(drums, at, ref noise);
                    if (track.Hat[s] == 'x') Hat(drums, at, ref noise, s % 4 == 2 ? 1f : .6f);
                    char b = track.Bass[s];
                    if (b != '.')
                    {
                        int next = 1; while (s + next < StepsPerBar && track.Bass[s + next] == '.') next++;
                        int note = rootNote - 12 + (b == 'o' ? 12 : b == '5' ? 7 : 0);
                        Tone(bass, at, step * next * .85f, Midi(note), 2, .004f, .06f, track.BassLevel, 900);
                    }
                }
                // Pad: the chord's three tones for two bars, slow attack (every other bar starts a new chord).
                if (bar % 2 == 0)
                    for (int voice = 0; voice < 3; voice++)
                        Tone(pad, bar * StepsPerBar * step, step * StepsPerBar * 2, Midi(track.Root + 12 + Degree(track, chord + voice * 2)), 0, .4f, .5f, track.PadLevel * .3f, 0);
                // Lead: arpeggio of chord tones, or a melody with call (bars 0–3) and answer (4–7, varied).
                if (track.Arp)
                {
                    for (int s = 0; s < StepsPerBar; s += 2)
                    {
                        int tone = (s / 2) % 4; int degree = chord + (tone == 3 ? 2 : tone * 2);
                        Tone(lead, (bar * StepsPerBar + s) * step, step * 1.6f, Midi(track.Root + 12 * track.LeadOctave + Degree(track, degree)),
                             track.LeadWave, .003f, .05f, track.LeadLevel, 2400);
                    }
                }
                else
                {
                    var phrase = new System.Random(track.Seed * 31 + (bar % 4) * 7 + (bar >= 4 && bar % 4 == 3 ? 101 : 0));
                    int position = 0, degree = chord + phrase.Next(0, 3) * 2;
                    while (position < StepsPerBar)
                    {
                        int length2 = new[] { 2, 2, 4, 1, 3 }[phrase.Next(0, 5)];
                        if (position + length2 > StepsPerBar) length2 = StepsPerBar - position;
                        if (phrase.NextDouble() > .18)
                            Tone(lead, (bar * StepsPerBar + position) * step, step * length2 * .9f, Midi(track.Root + 12 * track.LeadOctave + Degree(track, degree)),
                                 track.LeadWave, .006f, .08f, track.LeadLevel, 2200);
                        position += length2;
                        degree += phrase.Next(-2, 3);
                        degree = Mathf.Clamp(degree, chord - 2, chord + 7);
                    }
                }
            }
            var mix = new float[length];
            for (int i = 0; i < length; i++) mix[i] = drums[i] * .9f + bass[i] * .8f + pad[i] + lead[i] * .55f;
            Normalise(mix);
            return mix;
        }

        /// <summary>Same loudness for every track; peak capped so nothing clips.</summary>
        private static void Normalise(float[] data)
        {
            double sum = 0; float peak = 0;
            foreach (float v in data) { sum += v * v; peak = Mathf.Max(peak, Mathf.Abs(v)); }
            float rms = Mathf.Sqrt((float)(sum / data.Length));
            float gain = rms > 0 ? TargetRms / rms : 1;
            if (peak * gain > MaxPeak)
            {
                // Soft limit the few loudest samples instead of turning the whole track down.
                for (int i = 0; i < data.Length; i++) data[i] = MaxPeak * Tanh(data[i] * gain / MaxPeak);
                return;
            }
            for (int i = 0; i < data.Length; i++) data[i] *= gain;
        }

        private static float Tanh(float x) { float e = Mathf.Exp(2 * x); return (e - 1) / (e + 1); }
        private static float Midi(int note) => 440f * Mathf.Pow(2, (note - 69) / 12f);

        private static int Degree(Track track, int degree)
        {
            int n = track.Scale.Length, octave = Mathf.FloorToInt(degree / (float)n);
            return track.Scale[degree - octave * n] + 12 * octave;
        }

        /// <summary>Adds a note; samples past the end wrap to the start, so tails cross the loop point seamlessly.</summary>
        private static void Tone(float[] buffer, int start, float lengthSamples, float frequency, int wave, float attack, float release, float level, float lowpass)
        {
            int length = buffer.Length, total = Mathf.CeilToInt(lengthSamples + release * Rate);
            float a = Mathf.Max(1, attack * Rate), r = Mathf.Max(1, release * Rate), held = lengthSamples;
            float phase = 0, step = frequency / Rate, filtered = 0;
            float k = lowpass > 0 ? 1 - Mathf.Exp(-2 * Mathf.PI * lowpass / Rate) : 1;
            for (int i = 0; i < total; i++)
            {
                float envelope = Mathf.Min(1, i / a) * (i < held ? 1 : Mathf.Max(0, 1 - (i - held) / r));
                if (envelope <= 0 && i >= held) break;
                phase += step; if (phase >= 1) phase -= 1;
                float value = wave == 0 ? Mathf.Sin(phase * 2 * Mathf.PI) + .25f * Mathf.Sin(phase * 4 * Mathf.PI)
                    : wave == 1 ? (phase < .5f ? .6f : -.6f) : (phase * 2 - 1) * .7f;
                filtered += (value - filtered) * k;
                buffer[(start + i) % length] += filtered * envelope * level;
            }
        }

        private static void Kick(float[] buffer, int start)
        {
            int total = Mathf.CeilToInt(.28f * Rate); float phase = 0;
            for (int i = 0; i < total; i++)
            {
                float t = i / (float)Rate;
                phase += (45 + 75 * Mathf.Exp(-t * 28)) / Rate;
                buffer[(start + i) % buffer.Length] += Mathf.Sin(phase * 2 * Mathf.PI) * Mathf.Exp(-t * 11) * Mathf.Min(1, i / 30f) * .9f;
            }
        }

        private static void Snare(float[] buffer, int start, ref uint noise)
        {
            int total = Mathf.CeilToInt(.16f * Rate); float last = 0;
            for (int i = 0; i < total; i++)
            {
                float t = i / (float)Rate, n = Noise(ref noise), hp = n - last; last = n;
                float body = Mathf.Sin(t * 185 * 2 * Mathf.PI) * Mathf.Exp(-t * 30) * .35f;
                buffer[(start + i) % buffer.Length] += (hp * .35f * Mathf.Exp(-t * 22) + body) * Mathf.Min(1, i / 20f);
            }
        }

        private static void Hat(float[] buffer, int start, ref uint noise, float level)
        {
            int total = Mathf.CeilToInt(.045f * Rate); float last = 0;
            for (int i = 0; i < total; i++)
            {
                float t = i / (float)Rate, n = Noise(ref noise), hp = n - last; last = n;
                buffer[(start + i) % buffer.Length] += hp * .12f * level * Mathf.Exp(-t * 90) * Mathf.Min(1, i / 10f);
            }
        }

        private static float Noise(ref uint state)
        {
            state ^= state << 13; state ^= state >> 17; state ^= state << 5;
            return (state & 0xFFFFFF) / (float)0x800000 - 1f;
        }
    }
}
