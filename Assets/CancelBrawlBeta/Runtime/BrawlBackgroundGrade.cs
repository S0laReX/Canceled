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

        /// <summary>Frees every graded copy (the calibration sweep makes one per factor).</summary>
        public static void ClearCache(bool keepTableFactors)
        {
            var keep = new HashSet<int>();
            if (keepTableFactors) foreach (float f in AlbedoSaturation) { keep.Add(Key(f)); keep.Add(Key(EmissionSaturation(f))); }
            var drop = new List<(Texture, int)>();
            foreach (var pair in cache) if (!keep.Contains(pair.Key.Item2)) drop.Add(pair.Key);
            foreach (var key in drop) { if (cache[key]) { cache[key].Release(); Object.Destroy(cache[key]); } cache.Remove(key); }
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
            if (cache.TryGetValue(key, out RenderTexture done) && done && done.IsCreated()) return done;
            var copy = new RenderTexture(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                name = source.name + " (fondo atenuado)", useMipMap = true, autoGenerateMips = true,
                wrapMode = source.wrapMode, filterMode = source.filterMode, anisoLevel = source.anisoLevel,
            };
            copy.Create();
            blit.SetFloat(SaturationId, saturation);
            Graphics.Blit(source, copy, blit);
            cache[key] = copy;
            return copy;
        }
    }
}
