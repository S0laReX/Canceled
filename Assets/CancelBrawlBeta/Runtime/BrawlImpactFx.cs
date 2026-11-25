using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CancelBrawl.Beta
{
    /// <summary>How strong an effect reads: basic hit, strong hit, downward finisher, KO or super.</summary>
    public enum ImpactTier { Light, Strong, Spike, Ko, Super }

    /// <summary>
    /// Presentation only (GDD 2.0 §11): impact, finisher, KO and super effects drawn with five pooled particle systems
    /// and a short bloom pulse. Nothing here decides combat. Effects sit a little behind the fighter, seen from the
    /// side camera, so they never cover a fighter (GDD §2). Reduced effects keep the shapes (ring and hit direction)
    /// and drop the white flash, the bloom pulse and two thirds of the sparks.
    /// </summary>
    public sealed class BrawlImpactFx : MonoBehaviour
    {
        /// <summary>Depth offset (+Z, away from the side camera) of every effect relative to the point that caused it.</summary>
        public const float DepthOffset = .35f;
        public const string ParticleShader = "Universal Render Pipeline/Particles/Unlit";
        public static BrawlImpactFx Instance { get; private set; }

        private BrawlMatch match;
        private ParticleSystem sparks, rings, flashes, rays, puffs;
        private ParticleSystem[] all;
        private Texture2D dot, ring;
        private Material[] materials;
        private Volume volume; private VolumeProfile profile;
        private float bloomPulse; private bool paused;

        /// <summary>Counters read by the effect checks; reset with <see cref="ResetStats"/>.</summary>
        public int SparksEmitted { get; private set; }
        public int RingsEmitted { get; private set; }
        public int FlashesEmitted { get; private set; }
        public int RaysEmitted { get; private set; }
        public int PuffsEmitted { get; private set; }
        public float PeakBloom { get; private set; }
        public ImpactTier LastTier { get; private set; }
        public Vector3 LastPosition { get; private set; }
        public float BloomWeight => volume ? volume.weight : 0;
        public bool UsesParticleShader { get; private set; }
        public bool IsPaused => paused;
        public int AliveParticles
        {
            get { int n = 0; if (all != null) foreach (var system in all) n += system.particleCount; return n; }
        }
        public void ResetStats() { SparksEmitted = RingsEmitted = FlashesEmitted = RaysEmitted = PuffsEmitted = 0; PeakBloom = 0; }

        public static BrawlImpactFx Create(BrawlMatch owner, Camera view)
        {
            var obj = new GameObject("Efectos de impacto"); obj.transform.SetParent(owner.transform, false);
            var fx = obj.AddComponent<BrawlImpactFx>(); fx.match = owner;
            fx.Build(view);
            return Instance = fx;
        }

        private void Build(Camera view)
        {
            dot = Texture(false); ring = Texture(true);
            Shader shader = Shader.Find(ParticleShader);
            UsesParticleShader = shader != null;
            if (!shader) shader = Shader.Find("Universal Render Pipeline/Unlit");
            materials = new[]
            {
                Additive(shader, dot, 1.5f), // sparks and rays: slightly over 1 so bright sparks catch the bloom
                Additive(shader, ring, 1f),
                Additive(shader, dot, 1.8f), // flash core, the brightest thing on screen for a few frames
                Additive(shader, dot, 1f),
            };
            sparks = System("Chispas", materials[0], ParticleSystemRenderMode.Stretch, 600, 1.2f, false);
            rings = System("Anillos", materials[1], ParticleSystemRenderMode.Billboard, 64, 0, true);
            flashes = System("Destellos", materials[2], ParticleSystemRenderMode.Billboard, 32, 0, true);
            rays = System("Rayos", materials[0], ParticleSystemRenderMode.Stretch, 96, 0, false);
            puffs = System("Polvo", materials[3], ParticleSystemRenderMode.Billboard, 300, .4f, false);
            all = new[] { sparks, rings, flashes, rays, puffs };

            // Bloom only on impacts and supers: a global volume at weight 0 that pulses for a moment.
            profile = ScriptableObject.CreateInstance<VolumeProfile>(); profile.name = "Bloom de impacto";
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1.1f); bloom.intensity.Override(1.1f); bloom.scatter.Override(.55f);
            volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true; volume.priority = 10; volume.weight = 0; volume.sharedProfile = profile;
            if (view) view.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            // First use of each system and of the bloom pass compiles and allocates on the GPU: do it now, invisibly.
            foreach (var system in all) Emit(system, transform.position + Vector3.forward * DepthOffset, Vector3.zero, .01f, .05f, Color.clear);
            warmFrames = WarmFrames;
        }

        /// <summary>Frames after creation with the bloom pass on at a weight no one can see, so its first real pulse is cheap.</summary>
        public const int WarmFrames = 3;
        private const float WarmWeight = .0005f;
        private int warmFrames;
        /// <summary>True once the start-up warm-up of particles and bloom has run.</summary>
        public bool Warmed => warmFrames == 0;

        private ParticleSystem System(string name, Material material, ParticleSystemRenderMode mode, int max, float gravity, bool grow)
        {
            var obj = new GameObject(name); obj.transform.SetParent(transform, false);
            var system = obj.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = true; main.duration = 1; main.playOnAwake = false; main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.gravityModifier = gravity;
            main.startSpeed = 0; main.startLifetime = 1; main.startSize = 1;
            var emission = system.emission; emission.enabled = false;
            var shape = system.shape; shape.enabled = false;
            var fade = system.colorOverLifetime; fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(.8f, .8f, .8f), .4f), new GradientColorKey(Color.black, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(grow ? .85f : 1, .35f), new GradientAlphaKey(0, 1) });
            fade.color = gradient;
            var size = system.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, grow ? AnimationCurve.EaseInOut(0, .3f, 1, 1) : AnimationCurve.Linear(0, 1, 1, .2f));
            var renderer = obj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = mode; renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.alignment = ParticleSystemRenderSpace.View;
            if (mode == ParticleSystemRenderMode.Stretch) { renderer.velocityScale = .04f; renderer.lengthScale = 1.6f; }
            system.Play();
            return system;
        }

        private static Material Additive(Shader shader, Texture2D texture, float intensity)
        {
            var material = new Material(shader) { name = "Efecto aditivo" };
            material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 2);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)BlendMode.One);
            material.SetFloat("_ZWrite", 0);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetTexture("_BaseMap", texture); material.mainTexture = texture;
            material.SetColor("_BaseColor", new Color(intensity, intensity, intensity, 1));
            return material;
        }

        /// <summary>Soft dot or thin ring, white with alpha, generated once.</summary>
        private static Texture2D Texture(bool isRing)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = isRing ? "Anillo" : "Punto", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float r = new Vector2(x - size / 2f + .5f, y - size / 2f + .5f).magnitude / (size / 2f);
                    float a = isRing ? Mathf.Clamp01(1 - Mathf.Abs(r - .82f) / .1f) : Mathf.Clamp01(1 - r) * Mathf.Clamp01(1 - r);
                    // Fade carried in colour as well as alpha: with additive blending black adds nothing, so edges stay soft
                    // even if the build lacks the shader's transparent variant (alpha would then read as 1).
                    byte v = (byte)(a * 255); pixels[y * size + x] = new Color32(v, v, v, v);
                }
            texture.SetPixels32(pixels); texture.Apply(false, true);
            return texture;
        }

        // ───────────── Effects ─────────────

        /// <summary>Hit spark: sparks along the launch direction, an outline ring, and a flash plus bloom on strong tiers.</summary>
        public void Impact(Vector3 position, Color color, ImpactTier tier, Vector3 direction)
        {
            bool reduced = BrawlFeedback.ReducedEffects;
            position.z += DepthOffset;
            Vector3 dir = new Vector3(direction.x, direction.y, 0);
            dir = dir.sqrMagnitude > .01f ? dir.normalized : Vector3.up;
            LastTier = tier; LastPosition = position;
            int count = tier == ImpactTier.Light ? 10 : tier == ImpactTier.Ko ? 34 : tier == ImpactTier.Super ? 26 : 18;
            if (reduced) count = Mathf.Max(2, count / 3);
            float speed = tier == ImpactTier.Light ? 7 : tier == ImpactTier.Ko ? 15 : 11;
            float cone = tier == ImpactTier.Spike ? 22 : tier == ImpactTier.Light ? 55 : tier == ImpactTier.Super ? 180 : 40;
            Color hot = Color.Lerp(color, Color.white, .35f);
            for (int i = 0; i < count; i++)
            {
                Vector3 velocity = Quaternion.Euler(0, 0, Random.Range(-cone, cone)) * dir * speed * Random.Range(.55f, 1.15f);
                Emit(sparks, position, velocity, Random.Range(.06f, .12f), Random.Range(.16f, .3f), hot);
            }
            SparksEmitted += count;
            float ringSize = tier == ImpactTier.Light ? .9f : tier == ImpactTier.Ko ? 2.6f : tier == ImpactTier.Super ? 3.2f : 1.5f;
            Emit(rings, position, Vector3.zero, ringSize, tier == ImpactTier.Ko || tier == ImpactTier.Super ? .32f : .22f, color);
            RingsEmitted++;
            if (tier == ImpactTier.Spike || tier == ImpactTier.Ko)
            {
                // Finisher and KO: three long streaks that show where the fighter was sent.
                int streaks = reduced ? 1 : 3;
                for (int i = 0; i < streaks; i++)
                    Emit(rays, position, Quaternion.Euler(0, 0, (i - (streaks - 1) * .5f) * 9) * dir * 24, .14f, .2f, hot);
                RaysEmitted += streaks;
            }
            if (reduced) return;
            float flash = tier == ImpactTier.Light ? .45f : tier == ImpactTier.Ko ? 1.05f : tier == ImpactTier.Super ? 1.15f : .7f;
            Emit(flashes, position, Vector3.zero, flash, tier == ImpactTier.Ko ? .09f : .06f, Color.white);
            FlashesEmitted++;
            if (tier != ImpactTier.Light) Pulse(tier == ImpactTier.Ko || tier == ImpactTier.Super ? 1 : .55f);
        }

        /// <summary>Super activation: rays rising around the caster plus a super-tier impact.</summary>
        public void Super(Vector3 position, Color color)
        {
            Impact(position, color, ImpactTier.Super, Vector3.up);
            int count = BrawlFeedback.ReducedEffects ? 2 : 8;
            Vector3 at = position + Vector3.forward * DepthOffset;
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = Vector3.right * Mathf.Lerp(-.9f, .9f, count == 1 ? .5f : i / (count - 1f));
                Emit(rays, at + offset + Vector3.down * .7f, Vector3.up * Random.Range(7f, 10f), .1f, .35f, Color.Lerp(color, Color.white, .5f));
            }
            RaysEmitted += count;
        }

        /// <summary>Small dust for jumps, dodges, recoveries and swings (the old cube burst, now pooled).</summary>
        public void Puff(Vector3 position, Color color, int count)
        {
            if (BrawlFeedback.ReducedEffects) count = Mathf.Max(1, count / 3);
            position.z += DepthOffset;
            for (int i = 0; i < count; i++)
                Emit(puffs, position, Random.onUnitSphere * Random.Range(2f, 5f), Random.Range(.1f, .2f), .3f, color);
            PuffsEmitted += count;
        }

        private static void Emit(ParticleSystem system, Vector3 position, Vector3 velocity, float size, float life, Color color)
        {
            var p = new ParticleSystem.EmitParams
            {
                position = position, velocity = velocity, startSize = size, startLifetime = life, startColor = color,
                applyShapeToPosition = false,
            };
            system.Emit(p, 1);
        }

        private void Pulse(float amount) { bloomPulse = Mathf.Max(bloomPulse, amount); PeakBloom = Mathf.Max(PeakBloom, amount); }

        /// <summary>Removes every live particle and the bloom (menu return and round cleanup).</summary>
        public void Clear()
        {
            if (all != null) foreach (var system in all) system.Clear(true);
            bloomPulse = 0; if (volume) volume.weight = 0;
        }

        private void Update()
        {
            bool shouldPause = match && match.Phase == MatchPhase.Paused;
            if (shouldPause != paused)
            {
                paused = shouldPause;
                foreach (var system in all) { if (paused) system.Pause(true); else system.Play(true); }
            }
            if (paused) return;
            bloomPulse = Mathf.Max(0, bloomPulse - Time.deltaTime / .22f);
            volume.weight = BrawlFeedback.ReducedEffects ? 0 : bloomPulse;
            // The frame the count reaches 0 already renders at the real weight, so Warmed means "back at rest".
            if (warmFrames > 0 && --warmFrames > 0) volume.weight = Mathf.Max(volume.weight, WarmWeight);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (materials != null) foreach (var material in materials) if (material) Destroy(material);
            if (dot) Destroy(dot); if (ring) Destroy(ring); if (profile) Destroy(profile);
        }
    }
}
