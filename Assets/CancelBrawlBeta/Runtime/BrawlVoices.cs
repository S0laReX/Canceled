using UnityEngine;

namespace CancelBrawl.Beta
{
    public enum VoiceLine { Ko = 0, Super = 1, Win = 2 }

    /// <summary>
    /// Character lines (GDD 2.0 §10): each of the five characters says something original on KO, on the super and on
    /// winning. The audio is a synthesised babble that follows the vowels and pauses of the line, with a pitch, speed
    /// and formant colour per character; it is not speech, not recorded, cloned or modelled on any real person. The
    /// words reach the player through the subtitles (<see cref="BrawlSubtitles"/>). Kind names: <c>voice-ko@2</c> is
    /// the KO line of character 2. Placeholder: meant to be replaced by final voice acting.
    /// </summary>
    public static class BrawlVoices
    {
        public const int Rate = 22050;
        public const float MaxSeconds = 2.4f, Peak = .8f;
        public static readonly string[] LineNames = { "ko", "super", "win" };
        public static int LineCount => LineNames.Length;

        private struct Profile { public float Pitch, Formant, Syllable, Vibrato; }

        // Don Decreto, Lola Loop, Coach Tóxico, Cripto King, Tío Antena (BrawlRoster order).
        private static readonly Profile[] Profiles =
        {
            new Profile { Pitch = 104, Formant = .9f, Syllable = .115f, Vibrato = .015f },
            new Profile { Pitch = 238, Formant = 1.16f, Syllable = .078f, Vibrato = .02f },
            new Profile { Pitch = 128, Formant = .98f, Syllable = .088f, Vibrato = .01f },
            new Profile { Pitch = 146, Formant = 1.03f, Syllable = .07f, Vibrato = .012f },
            new Profile { Pitch = 178, Formant = 1.07f, Syllable = .097f, Vibrato = .06f },
        };

        private static readonly string[] KoLines =
        {
            "¡Esto no estaba en el programa!", "¡Me dejaron en visto!", "¡Eso no cuenta como repetición!",
            "¡Vendo, vendo, vendo!", "¡Me cortaron la señal!",
        };
        private static readonly string[] WinLines =
        {
            "Lo prometido es deuda.", "Esto va directo a mis historias.", "Sin dolor no hay podio.",
            "Compren mientras está barato.", "Yo ya lo sabía.",
        };

        public static int CharacterCount => Profiles.Length;
        public static string Kind(VoiceLine line, int character) => "voice-" + LineNames[(int)line] + "@" + Mathf.Clamp(character, 0, Profiles.Length - 1);

        public static string Text(VoiceLine line, int character)
        {
            character = Mathf.Clamp(character, 0, Profiles.Length - 1);
            switch (line)
            {
                case VoiceLine.Ko: return KoLines[character];
                case VoiceLine.Win: return WinLines[character];
                default: return "¡" + BrawlRoster.Get(character).SuperName + "!";
            }
        }

        /// <summary>Parses <c>voice-ko@2</c>; false for the generic lines without a speaker.</summary>
        public static bool TryParse(string kind, out VoiceLine line, out int character)
        {
            line = VoiceLine.Ko; character = -1;
            if (string.IsNullOrEmpty(kind) || !kind.StartsWith("voice-")) return false;
            int at = kind.IndexOf('@');
            if (at < 0 || !int.TryParse(kind.Substring(at + 1), out character)) return false;
            int index = System.Array.IndexOf(LineNames, kind.Substring(6, at - 6));
            if (index < 0 || character < 0 || character >= Profiles.Length) return false;
            line = (VoiceLine)index; return true;
        }

        /// <summary>Priority on the voice bus: winning beats a KO, a KO beats a super.</summary>
        public static int Priority(string kind) => kind.StartsWith("voice-win") ? 3 : kind.StartsWith("voice-ko") ? 2 : 1;

        public static AudioClip Build(string kind)
        {
            float[] data = RenderKind(kind);
            if (data == null) return null;
            var clip = AudioClip.Create(kind, data.Length, 1, Rate, false); clip.SetData(data, 0); return clip;
        }

        /// <summary>Samples of a line; pure C#, safe off the main thread (no Unity objects).</summary>
        public static float[] RenderKind(string kind)
        {
            if (!TryParse(kind, out VoiceLine line, out int character)) return null;
            return Render(Text(line, character), Profiles[character], line);
        }

        // Formants (F1, F2) of the five Spanish vowels, Hz.
        private static readonly float[,] Vowels = { { 800, 1250 }, { 450, 1900 }, { 300, 2300 }, { 500, 900 }, { 330, 750 } };

        private struct Segment { public int Vowel; public float Seconds; public int Consonant; public bool Pause; }

        private static float[] Render(string text, Profile voice, VoiceLine line)
        {
            var segments = new System.Collections.Generic.List<Segment>();
            int consonant = 0;
            string lower = text.ToLowerInvariant();
            for (int i = 0; i < lower.Length; i++)
            {
                char c = lower[i];
                int vowel = VowelOf(c);
                if (vowel >= 0)
                {
                    bool last = i + 1 >= lower.Length || ".,!?¡¿".IndexOf(lower[i + 1]) >= 0;
                    // Two vowels in a row glide in one syllable.
                    if (segments.Count > 0 && consonant == 0 && !segments[segments.Count - 1].Pause && i > 0 && VowelOf(lower[i - 1]) >= 0)
                    { var s = segments[segments.Count - 1]; s.Seconds += voice.Syllable * .4f; segments[segments.Count - 1] = s; continue; }
                    segments.Add(new Segment { Vowel = vowel, Seconds = voice.Syllable * (last ? 1.7f : 1f), Consonant = consonant });
                    consonant = 0;
                }
                else if (c == ' ') segments.Add(new Segment { Pause = true, Seconds = .035f });
                else if (".,!?".IndexOf(c) >= 0) segments.Add(new Segment { Pause = true, Seconds = .11f });
                else if (char.IsLetter(c)) consonant = "ptkbdgcq".IndexOf(c) >= 0 ? 1 : "szfjxh".IndexOf(c) >= 0 ? 2 : 3;
            }
            float total = 0; foreach (var s in segments) total += s.Seconds + (s.Consonant == 2 ? .045f : s.Consonant == 1 ? .015f : s.Consonant == 3 ? .03f : 0);
            float squeeze = total > MaxSeconds ? MaxSeconds / total : 1;
            int length = Mathf.CeilToInt((total * squeeze + .08f) * Rate);
            var data = new float[length];
            uint noise = 0x9E3779B9u;
            float phase = 0; int at = 0, syllable = 0, syllables = 0;
            foreach (var s in segments) if (!s.Pause) syllables++;
            bool exclaim = text.IndexOf('!') >= 0;
            foreach (var s in segments)
            {
                if (s.Pause) { at += Mathf.RoundToInt(s.Seconds * squeeze * Rate); continue; }
                // Consonant onset: plosive burst, fricative hiss or a short voiced murmur.
                if (s.Consonant > 0)
                {
                    int n = Mathf.RoundToInt((s.Consonant == 2 ? .045f : s.Consonant == 1 ? .015f : .03f) * squeeze * Rate);
                    float last = 0;
                    for (int i = 0; i < n && at + i < length; i++)
                    {
                        float u = i / (float)n, white = Noise(ref noise), hp = white - last; last = white;
                        float v = s.Consonant == 3 ? Mathf.Sin(phase * 2 * Mathf.PI) * .25f : (s.Consonant == 2 ? hp * .3f : white * .45f) * (1 - u);
                        if (s.Consonant == 3) phase = Mathf.Repeat(phase + voice.Pitch / Rate, 1);
                        data[at + i] += v * Mathf.Min(1, i / 40f);
                    }
                    at += n;
                }
                int count = Mathf.RoundToInt(s.Seconds * squeeze * Rate);
                float progress = syllables > 1 ? syllable / (float)(syllables - 1) : 0;
                // Intonation: an exclamation starts high and falls; a statement falls gently; the super rises at the end.
                float contour = line == VoiceLine.Super ? 1 + .25f * progress : exclaim ? 1.22f - .3f * progress : 1.05f - .15f * progress;
                float f1 = Vowels[s.Vowel, 0] * voice.Formant, f2 = Vowels[s.Vowel, 1] * voice.Formant;
                for (int i = 0; i < count && at + i < length; i++)
                {
                    float t = (at + i) / (float)Rate, u = i / (float)count;
                    float f0 = voice.Pitch * contour * (1 + voice.Vibrato * Mathf.Sin(t * 2 * Mathf.PI * 5.5f)) * (1 - .06f * u);
                    phase = Mathf.Repeat(phase + f0 / Rate, 1);
                    float sample = 0;
                    int harmonics = Mathf.Min(22, Mathf.FloorToInt(3000 / f0));
                    for (int k = 1; k <= harmonics; k++)
                    {
                        float f = k * f0;
                        float gain = 1 / (1 + Sq((f - f1) / 110)) + .55f / (1 + Sq((f - f2) / 160)) + .05f / k;
                        sample += Mathf.Sin(phase * k * 2 * Mathf.PI) * gain;
                    }
                    float envelope = Mathf.Min(1, u * 9) * Mathf.Min(1, (1 - u) * 6);
                    data[at + i] += sample * envelope * .12f;
                }
                at += count; syllable++;
            }
            float peak = 0; foreach (float v in data) peak = Mathf.Max(peak, Mathf.Abs(v));
            if (peak > 0) for (int i = 0; i < length; i++) data[i] *= Peak / peak;
            return data;
        }

        private static float Sq(float x) => x * x;

        private static int VowelOf(char c)
        {
            switch (c)
            {
                case 'a': case 'á': return 0;
                case 'e': case 'é': return 1;
                case 'i': case 'í': case 'y': return 2;
                case 'o': case 'ó': return 3;
                case 'u': case 'ú': case 'ü': return 4;
                default: return -1;
            }
        }

        private static float Noise(ref uint state)
        {
            state ^= state << 13; state ^= state >> 17; state ^= state << 5;
            return (state & 0xFFFFFF) / (float)0x800000 - 1f;
        }
    }
}
