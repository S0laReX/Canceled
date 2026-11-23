using System.Collections.Generic;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>Audio buses of GDD 2.0 §10 (AUDIO-428 to 434). Master scales the other four.</summary>
    public enum BrawlAudioBus { Master = 0, Music = 1, Sfx = 2, Voice = 3, Ui = 4 }

    /// <summary>
    /// Code mixer: one <see cref="AudioSource"/> per bus (Music, SFX, Voices, UI) under the match root. The volume a
    /// source plays at is <c>gain × Master × bus</c>; a bus at 0 (or Master at 0) is silent. Every sound goes through
    /// <see cref="Play"/>, which picks its bus from its name (<see cref="Route"/>). Presentation only: it never decides
    /// combat results. The generated clips are placeholders, meant to be replaced by final audio.
    /// </summary>
    public static class BrawlAudio
    {
        public const int BusCount = 5;
        public static readonly string[] BusNames = { "Master", "Música", "SFX", "Voces", "UI" };
        /// <summary>Safe defaults; SFX and Music keep the values the game used before the buses existed.</summary>
        public static readonly float[] DefaultVolumes = { 1f, .5f, .8f, .8f, .7f };
        /// <summary>Fixed mix gain of each bus at 100 % (Master has none of its own).</summary>
        public static readonly float[] BusGain = { 1f, .12f, .2f, .3f, .15f };
        /// <summary>Sounds the game emits today, all on the SFX bus.</summary>
        public static readonly string[] EffectKinds = { "jump", "hit", "ko", "ready", "super", "warning", "bounce", "trap", "swing" };
        public static readonly string[] UiKinds = { "ui-move", "ui-confirm", "ui-pause" };
        public static readonly string[] VoiceKinds = { "voice-ko", "voice-super" };

        private static readonly float[] volumes = (float[])DefaultVolumes.Clone();
        private static readonly AudioSource[] sources = new AudioSource[BusCount];
        private static readonly int[] plays = new int[BusCount];
        private static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        private static AudioClip musicClip;
        private static int voicePriority;

        public static string LastKind { get; private set; } = "";
        public static BrawlAudioBus LastBus { get; private set; }
        /// <summary>Phrase on the voice bus right now, or empty. Only one phrase plays at a time.</summary>
        public static string CurrentVoice => sources[(int)BrawlAudioBus.Voice] && sources[(int)BrawlAudioBus.Voice].isPlaying && sources[(int)BrawlAudioBus.Voice].clip
            ? sources[(int)BrawlAudioBus.Voice].clip.name : "";

        public static void Initialize(Transform root)
        {
            foreach (var clip in clips.Values) if (clip) Object.Destroy(clip);
            clips.Clear();
            if (musicClip) Object.Destroy(musicClip);
            for (int bus = 1; bus < BusCount; bus++)
            {
                if (sources[bus]) Object.Destroy(sources[bus].gameObject);
                var obj = new GameObject("Bus " + BusNames[bus]); obj.transform.SetParent(root, false);
                var source = obj.AddComponent<AudioSource>(); source.spatialBlend = 0; source.playOnAwake = false;
                sources[bus] = source; plays[bus] = 0;
            }
            var music = sources[(int)BrawlAudioBus.Music];
            musicClip = BuildMusic(); music.clip = musicClip; music.loop = true;
            voicePriority = 0; LastKind = "";
            Apply();
            music.Play();
        }

        public static float Volume(BrawlAudioBus bus) => volumes[(int)bus];

        /// <summary>What a bus actually plays at, Master included.</summary>
        public static float EffectiveVolume(BrawlAudioBus bus) => bus == BrawlAudioBus.Master ? volumes[0] : volumes[0] * volumes[(int)bus];

        public static bool IsSilent(BrawlAudioBus bus) => EffectiveVolume(bus) <= 0;

        public static void SetVolume(BrawlAudioBus bus, float value)
        {
            volumes[(int)bus] = float.IsNaN(value) ? DefaultVolumes[(int)bus] : Mathf.Clamp01(value);
            Apply();
        }

        public static void SetVolumes(float master, float music, float sfx, float voice, float ui)
        {
            float[] values = { master, music, sfx, voice, ui };
            for (int bus = 0; bus < BusCount; bus++) volumes[bus] = float.IsNaN(values[bus]) ? DefaultVolumes[bus] : Mathf.Clamp01(values[bus]);
            Apply();
        }

        /// <summary>Test/inspection hook: the source a bus plays through (null for Master).</summary>
        public static AudioSource Source(BrawlAudioBus bus) => sources[(int)bus];
        public static int PlayCount(BrawlAudioBus bus) => plays[(int)bus];

        /// <summary>Bus of a sound by name: <c>ui-*</c> → UI, <c>voice-*</c> → Voces, <c>music</c> → Música, the rest → SFX.</summary>
        public static BrawlAudioBus Route(string kind)
        {
            if (string.IsNullOrEmpty(kind)) return BrawlAudioBus.Sfx;
            if (kind.StartsWith("ui-")) return BrawlAudioBus.Ui;
            if (kind.StartsWith("voice-")) return BrawlAudioBus.Voice;
            if (kind == "music") return BrawlAudioBus.Music;
            return BrawlAudioBus.Sfx;
        }

        public static void Play(string kind)
        {
            BrawlAudioBus bus = Route(kind);
            var source = sources[(int)bus];
            if (!source) return;
            LastKind = kind; LastBus = bus; plays[(int)bus]++;
            if (bus == BrawlAudioBus.Music) { if (!source.isPlaying) source.Play(); return; }
            if (bus == BrawlAudioBus.Voice) { PlayVoice(kind, source); return; }
            source.PlayOneShot(Clip(kind));
            // GDD §10: a character line accompanies the KO and the super warning.
            if (kind == "ko") Play("voice-ko");
            else if (kind == "super") Play("voice-super");
        }

        /// <summary>One phrase at a time; a KO line interrupts a super line, never the other way round.</summary>
        private static void PlayVoice(string kind, AudioSource source)
        {
            int priority = kind == "voice-ko" ? 2 : 1;
            if (source.isPlaying && priority < voicePriority) return;
            source.Stop(); source.clip = Clip(kind); source.loop = false; source.Play();
            voicePriority = priority;
        }

        private static void Apply()
        {
            for (int bus = 1; bus < BusCount; bus++)
                if (sources[bus]) sources[bus].volume = BusGain[bus] * EffectiveVolume((BrawlAudioBus)bus);
        }

        private static AudioClip Clip(string kind)
        {
            if (clips.TryGetValue(kind, out AudioClip clip) && clip) return clip;
            clip = kind.StartsWith("voice-") ? BuildVoice(kind) : kind.StartsWith("ui-") ? BuildUi(kind) : BuildEffect(kind);
            clips[kind] = clip; return clip;
        }

        private static AudioClip BuildEffect(string kind)
        {
            float frequency = kind == "jump" ? 530 : kind == "hit" ? 120 : kind == "ko" ? 85 : kind == "ready" ? 880 :
                kind == "super" ? 180 : kind == "warning" ? 420 : kind == "bounce" ? 700 : kind == "trap" ? 300 : 230;
            float duration = kind == "ko" ? .4f : kind == "warning" ? .25f : kind == "bounce" ? .06f : .12f;
            int count = Mathf.CeilToInt(22050 * duration); var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / 22050f; float u = (float)i / count;
                data[i] = Mathf.Sin(t * frequency * (kind == "jump" ? 1 + u : 1 - u * .35f) * Mathf.PI * 2) * (1 - u) * Mathf.Min(1, u * 25) * .6f;
            }
            var clip = AudioClip.Create(kind, count, 1, 22050, false); clip.SetData(data, 0); return clip;
        }

        /// <summary>Short soft click: menu focus, confirm and pause.</summary>
        private static AudioClip BuildUi(string kind)
        {
            float frequency = kind == "ui-confirm" ? 1320 : kind == "ui-pause" ? 660 : 990;
            int count = Mathf.CeilToInt(22050 * .05f); var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float u = (float)i / count;
                data[i] = Mathf.Sin(i / 22050f * frequency * Mathf.PI * 2) * (1 - u) * (1 - u) * .5f;
            }
            var clip = AudioClip.Create(kind, count, 1, 22050, false); clip.SetData(data, 0); return clip;
        }

        /// <summary>Placeholder "voice": a vowel-like buzz with vibrato. No recorded or cloned voice.</summary>
        private static AudioClip BuildVoice(string kind)
        {
            bool ko = kind == "voice-ko";
            float pitch = ko ? 150 : 210, duration = ko ? .45f : .32f;
            int count = Mathf.CeilToInt(22050 * duration); var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / 22050f, u = (float)i / count;
                float f = pitch * (1 + .04f * Mathf.Sin(t * 34)) * (ko ? 1 - u * .3f : 1 + u * .2f);
                float phase = t * f * Mathf.PI * 2;
                float buzz = Mathf.Sin(phase) + .5f * Mathf.Sin(phase * 2) + .3f * Mathf.Sin(phase * 3) + .15f * Mathf.Sin(phase * 5);
                data[i] = buzz * Mathf.Min(1, u * 12) * Mathf.Min(1, (1 - u) * 6) * .3f;
            }
            var clip = AudioClip.Create(kind, count, 1, 22050, false); clip.SetData(data, 0); return clip;
        }

        /// <summary>Short procedural loop (bass + pulse) so music and effects can be balanced separately.</summary>
        private static AudioClip BuildMusic()
        {
            const int rate = 22050; const float bpm = 112; float beat = 60f / bpm;
            int count = Mathf.CeilToInt(rate * beat * 16);
            var data = new float[count];
            float[] roots = { 55f, 55f, 65.4f, 49f };
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate;
                int step = Mathf.FloorToInt(t / beat);
                float local = t - step * beat;
                float root = roots[(step / 4) % roots.Length];
                float bass = Mathf.Sin(2 * Mathf.PI * root * t) * Mathf.Exp(-local * 3.2f) * .55f;
                float off = Mathf.Repeat(t + beat * .5f, beat);
                float pulse = (Mathf.PerlinNoise(i * .37f, 0) - .5f) * Mathf.Exp(-off * 40f) * .25f;
                float pad = Mathf.Sin(2 * Mathf.PI * root * 4 * t) * .05f * (.6f + .4f * Mathf.Sin(t * .8f));
                data[i] = bass + pulse + pad;
            }
            var clip = AudioClip.Create("Beta loop", count, 1, rate, false); clip.SetData(data, 0);
            return clip;
        }
    }
}
