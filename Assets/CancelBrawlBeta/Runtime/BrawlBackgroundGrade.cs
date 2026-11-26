using System.Collections.Generic;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// Background grade for GDD 2.0 §9 («El fondo tiene menos contraste y saturación que los luchadores»): when an arena
    /// is loaded (BrawlMatch.BeginRound, before the countdown, never during play) its stage gets less saturation.
    /// Nothing on disk changes. Every texture the stage uses (<c>_BaseMap</c>, <c>_MainTex</c>, <c>_EmissionMap</c>) is
    /// copied on the GPU through <c>Hidden/FUNADOS/Desaturate</c> into a render texture, once per texture and factor,
    /// and handed to the stage's renderers through a <see cref="MaterialPropertyBlock"/>; the base, emission and clear
    /// colours follow. The neon (emission) loses half as much as the albedo, so the arenas keep their identity.
    /// Option «FONDO ATENUADO» (<see cref="Enabled"/>, optional field <c>dimBackground</c> of the v4 options file).
    /// </summary>
    public static class BrawlBackgroundGrade
    {
        /// <summary>
        /// Share of the albedo saturation kept, per arena layout (Azotea, Torre, Estudio, Plaza). Each is the largest step
        /// of 0.05 that leaves the background's mean CIELAB chroma at most <see cref="TargetShare"/> of the fighters'
        /// chroma, measured by <c>-read-smoke</c> (its calibration sweep is in <c>read-smoke.json</c>).
        /// </summary>
        public static readonly float[] AlbedoSaturation = { .40f, .85f, .30f, .60f };
        /// <summary>Background chroma allowed, as a share of the fighters' (margin under GDD §9's "less than").</summary>
        public const float TargetShare = .85f;
        /// <summary>
        /// Longest side of a graded copy. The stages use 2048 px textures; at full size the copies (albedo and emission,
        /// with mips) cost ≈300 MB per arena and the soak flagged it. From the side camera a stage prop covers a few hundred
        /// pixels at 720p, so 1024 keeps every texel that reaches the screen (≈75 MB per arena). Only the current arena's
        /// copies are kept, so changing arena does not grow memory.
        /// </summary>
        public const int MaxCopySize = 1024;

        public static bool Enabled = true;
        public static bool Applied { get; private set; }
        public static float LastApplyMilliseconds { get; private set; }
        public static int LastRenderers { get; private set; }
        public static float LastAlbedo { get; private set; } = 1;
        public static int CachedTextures => cache.Count;

        /// <summary>Emission keeps more colour: it loses half the albedo's reduction.</summary>
        public static float EmissionSaturation(float albedo) => 1 - (1 - albedo) * .5f;
        public static float AlbedoFor(int layout) => AlbedoSaturation[Mathf.Clamp(layout, 0, AlbedoSaturation.Length - 1)];

        private static readonly Dictionary<(Texture, int), RenderTexture> cache = new Dictionary<(Texture, int), RenderTexture>();
        private static readonly List<Renderer> graded = new List<Renderer>();
        private static readonly HashSet<(Texture, int)> used = new HashSet<(Texture, int)>();
        /// <summary>GPU memory held by the graded copies (with mips).</summary>
        public static long CachedBytes { get; private set; }
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap"), MainTex = Shader.PropertyToID("_MainTex"),
            EmissionMap = Shader.PropertyToID("_EmissionMap"), BaseColor = Shader.PropertyToID("_BaseColor"),
            ColorId = Shader.PropertyToID("_Color"), EmissionColor = Shader.PropertyToID("_EmissionColor"), SaturationId = Shader.PropertyToID("_Saturation");
        private static Camera gradedCamera; private static Color originalBackground, gradedBackground;
        private static Material blit;

        /// <summary>Grades a production stage with its table factor (or <paramref name="albedoOverride"/>, for the calibration sweep).</summary>
        public static void Apply(BrawlArena arena, Camera camera, float? albedoOverride = null)
        {
            Revert();
            if (!arena || !arena.UsesStageArt) return;
            if (blit == null)
            {
                var shader = Resources.Load<Shader>("FunadosDesaturate");
                if (shader == null || !shader.isSupported) return;
                blit = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }
            var clock = System.Diagnostics.Stopwatch.StartNew();
            float albedo = Mathf.Clamp01(albedoOverride ?? AlbedoFor(arena.Layout)), emission = EmissionSaturation(albedo);
            used.Clear();
            var block = new MaterialPropertyBlock();
            foreach (var renderer in arena.GetComponentsInChildren<Renderer>())
            {
                if (renderer is LineRenderer || renderer is ParticleSystemRenderer) continue;
                var materials = renderer.sharedMaterials;
                bool any = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (!material) continue;
                    block.Clear();
                    Grade(material, block, BaseMap, albedo); Grade(material, block, MainTex, albedo); Grade(material, block, EmissionMap, emission);
                    if (material.HasProperty(BaseColor)) block.SetColor(BaseColor, Desaturate(material.GetColor(BaseColor), albedo));
                    if (material.HasProperty(ColorId)) block.SetColor(ColorId, Desaturate(material.GetColor(ColorId), albedo));
                    if (material.HasProperty(EmissionColor)) block.SetColor(EmissionColor, Desaturate(material.GetColor(EmissionColor), emission));
                    renderer.SetPropertyBlock(block, i); any = true;
                }
                if (any) graded.Add(renderer);
            }
            if (camera)
            {
                gradedCamera = camera; originalBackground = camera.backgroundColor;
                gradedBackground = camera.backgroundColor = Desaturate(originalBackground, albedo);
            }
            ReleaseUnused();
            Applied = true; LastRenderers = graded.Count; LastAlbedo = albedo;
            LastApplyMilliseconds = (float)clock.Elapsed.TotalMilliseconds;
        }

        public static void Revert()
        {
            foreach (var renderer in graded)
                if (renderer) for (int i = 0; i < renderer.sharedMaterials.Length; i++) renderer.SetPropertyBlock(null, i);
            graded.Clear();
            // Only undo the clear colour if nobody set another one since (a new arena sets its own).
            if (gradedCamera && gradedCamera.backgroundColor == gradedBackground) gradedCamera.backgroundColor = originalBackground;
            gradedCamera = null; Applied = false; LastAlbedo = 1;
        }

        /// <summary>Option off: undoes the grade and frees every copy.</summary>
        public static void Off() { Revert(); used.Clear(); ReleaseUnused(); }

        /// <summary>Frees every graded copy (the calibration sweep makes one per factor).</summary>
        public static void ClearCache(bool keepTableFactors)
        {
            var keep = new HashSet<int>();
            if (keepTableFactors) foreach (float f in AlbedoSaturation) { keep.Add(Key(f)); keep.Add(Key(EmissionSaturation(f))); }
            var drop = new List<(Texture, int)>();
            foreach (var pair in cache) if (!keep.Contains(pair.Key.Item2)) drop.Add(pair.Key);
            foreach (var key in drop) { if (cache[key]) { cache[key].Release(); Object.Destroy(cache[key]); } cache.Remove(key); }
        }

        /// <summary>Frees the copies the arena just graded does not use (the previous arena's, or other sweep factors).</summary>
        private static void ReleaseUnused()
        {
            var drop = new List<(Texture, int)>();
            foreach (var pair in cache) if (!used.Contains(pair.Key)) drop.Add(pair.Key);
            foreach (var key in drop) { if (cache[key]) { cache[key].Release(); Object.Destroy(cache[key]); } cache.Remove(key); }
            CachedBytes = 0;
            foreach (var pair in cache) if (pair.Value) CachedBytes += (long)pair.Value.width * pair.Value.height * 4 * 4 / 3;
        }

        private static int Key(float saturation) => Mathf.RoundToInt(saturation * 1000);

        private static void Grade(Material material, MaterialPropertyBlock block, int property, float saturation)
        {
            if (!material.HasProperty(property)) return;
            Texture source = material.GetTexture(property);
            if (!source || source.dimension != UnityEngine.Rendering.TextureDimension.Tex2D) return;
            block.SetTexture(property, Desaturated(source, saturation));
        }

        /// <summary>Saturation scaled toward the Rec. 709 luminance of the colour (alpha and HDR intensity kept).</summary>
        public static Color Desaturate(Color color, float saturation)
        {
            float l = .2126f * color.r + .7152f * color.g + .0722f * color.b;
            return new Color(Mathf.Lerp(l, color.r, saturation), Mathf.Lerp(l, color.g, saturation), Mathf.Lerp(l, color.b, saturation), color.a);
        }

        private static Texture Desaturated(Texture source, float saturation)
        {
            if (saturation >= .999f) return source;
            var key = (source, Key(saturation));
            used.Add(key);
            if (cache.TryGetValue(key, out RenderTexture done) && done && done.IsCreated()) return done;
            // Downscaled copies sample the source's own mips (tex2D picks the level from the blit's derivatives).
            float scale = Mathf.Min(1, (float)MaxCopySize / Mathf.Max(source.width, source.height));
            int width = Mathf.Max(1, Mathf.RoundToInt(source.width * scale)), height = Mathf.Max(1, Mathf.RoundToInt(source.height * scale));
            var copy = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                name = source.name + " (fondo atenuado)", useMipMap = true, autoGenerateMips = true,
                wrapMode = source.wrapMode, filterMode = source.filterMode, anisoLevel = source.anisoLevel,
            };
            copy.Create();
            blit.SetFloat(SaturationId, saturation);
            // Graphics.Blit leaves the copy as RenderTexture.active; restore it so screenshots and ReadPixels keep the screen.
            RenderTexture previous = RenderTexture.active;
            Graphics.Blit(source, copy, blit);
            RenderTexture.active = previous;
            cache[key] = copy;
            return copy;
        }
    }
}
