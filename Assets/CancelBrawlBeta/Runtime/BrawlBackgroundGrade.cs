using System.Collections.Generic;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// PROTOTYPE (not wired into the game yet, pending review): desaturates an arena's background at run time so it has
    /// less saturation than the fighters (GDD 2.0 §9 «El fondo tiene menos contraste y saturación que los luchadores»).
    /// Nothing on disk changes: every texture the stage uses (<c>_BaseMap</c>, <c>_EmissionMap</c>) is copied once,
    /// desaturated in memory, and handed to the stage's renderers through a <see cref="MaterialPropertyBlock"/>, with
    /// the base and emission colours desaturated the same way. The camera's clear colour follows. <see cref="Revert"/>
    /// removes the property blocks, so the switch compares both looks in the same scene.
    /// </summary>
    public static class BrawlBackgroundGrade
    {
        /// <summary>Share of the original saturation kept (0 = grey, 1 = unchanged).</summary>
        public static float Saturation = .45f;
        public static bool Applied { get; private set; }
        public static float LastApplyMilliseconds { get; private set; }
        public static int LastRenderers { get; private set; }
        public static int CachedTextures => cache.Count;

        private static readonly Dictionary<Texture, Texture2D> cache = new Dictionary<Texture, Texture2D>();
        private static readonly List<Renderer> graded = new List<Renderer>();
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap"), MainTex = Shader.PropertyToID("_MainTex"),
            EmissionMap = Shader.PropertyToID("_EmissionMap"), BaseColor = Shader.PropertyToID("_BaseColor"),
            ColorId = Shader.PropertyToID("_Color"), EmissionColor = Shader.PropertyToID("_EmissionColor");
        private static Camera gradedCamera; private static Color originalBackground;

        public static void Apply(BrawlArena arena, Camera camera)
        {
            Revert();
            if (!arena) return;
            var clock = System.Diagnostics.Stopwatch.StartNew();
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
                    Grade(material, block, BaseMap); Grade(material, block, MainTex); Grade(material, block, EmissionMap);
                    if (material.HasProperty(BaseColor)) block.SetColor(BaseColor, Desaturate(material.GetColor(BaseColor)));
                    if (material.HasProperty(ColorId)) block.SetColor(ColorId, Desaturate(material.GetColor(ColorId)));
                    if (material.HasProperty(EmissionColor)) block.SetColor(EmissionColor, Desaturate(material.GetColor(EmissionColor)));
                    renderer.SetPropertyBlock(block, i); any = true;
                }
                if (any) graded.Add(renderer);
            }
            if (camera) { gradedCamera = camera; originalBackground = camera.backgroundColor; camera.backgroundColor = Desaturate(originalBackground); }
            Applied = true; LastRenderers = graded.Count;
            LastApplyMilliseconds = (float)clock.Elapsed.TotalMilliseconds;
        }

        public static void Revert()
        {
            foreach (var renderer in graded)
                if (renderer) for (int i = 0; i < renderer.sharedMaterials.Length; i++) renderer.SetPropertyBlock(null, i);
            graded.Clear();
            if (gradedCamera) gradedCamera.backgroundColor = originalBackground;
            gradedCamera = null; Applied = false;
        }

        private static void Grade(Material material, MaterialPropertyBlock block, int property)
        {
            if (!material.HasProperty(property)) return;
            Texture source = material.GetTexture(property);
            if (!source) return;
            block.SetTexture(property, Desaturated(source));
        }

        /// <summary>Saturation scaled toward the Rec. 709 luminance of the colour (sRGB values, alpha kept).</summary>
        public static Color Desaturate(Color color)
        {
            float l = .2126f * color.r + .7152f * color.g + .0722f * color.b;
            return new Color(Mathf.Lerp(l, color.r, Saturation), Mathf.Lerp(l, color.g, Saturation), Mathf.Lerp(l, color.b, Saturation), color.a);
        }

        /// <summary>Desaturated copy of a texture, made once: drawn into a render target (works without read/write access), read back, regraded.</summary>
        private static Texture2D Desaturated(Texture source)
        {
            if (cache.TryGetValue(source, out Texture2D done) && done) return done;
            int w = source.width, h = source.height;
            var target = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(source, target);
            var previous = RenderTexture.active; RenderTexture.active = target;
            var copy = new Texture2D(w, h, TextureFormat.RGBA32, true, false) { name = source.name + " (fondo atenuado)", wrapMode = source.wrapMode, filterMode = source.filterMode, anisoLevel = source.anisoLevel };
            copy.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
            RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target);
            Color32[] pixels = copy.GetPixels32();
            int keep = Mathf.RoundToInt(Mathf.Clamp01(Saturation) * 256);
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 p = pixels[i];
                int l = (54 * p.r + 183 * p.g + 19 * p.b) >> 8;
                p.r = (byte)(l + (((p.r - l) * keep) >> 8)); p.g = (byte)(l + (((p.g - l) * keep) >> 8)); p.b = (byte)(l + (((p.b - l) * keep) >> 8));
                pixels[i] = p;
            }
            copy.SetPixels32(pixels); copy.Apply(true, true);
            cache[source] = copy;
            return copy;
        }
    }
}
