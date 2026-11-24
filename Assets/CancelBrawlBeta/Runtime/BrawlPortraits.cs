using System.Collections.Generic;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// Character portraits for the selection screen and the combat cards (GDD 2.0 §10), rendered from the real
    /// fighter model and look: one off-screen shot per character × look, cached for the whole session. Requests made
    /// while drawing the HUD are queued and rendered one per frame outside OnGUI.
    /// </summary>
    public sealed class BrawlPortraits : MonoBehaviour
    {
        public const int Size = 256;
        /// <summary>Unnamed layer used only by the portrait camera and the temporary model.</summary>
        public const int Layer = 30;
        private static readonly Vector3 StudioOrigin = new Vector3(0, -600, 0);
        public static readonly Color Background = new Color(.06f, .075f, .12f, 1);
        /// <summary>Three-quarter view: the model faces +Z and the camera looks back along −Z.</summary>
        public const float PortraitYaw = 20f;

        private readonly Dictionary<int, RenderTexture> cache = new Dictionary<int, RenderTexture>();
        private readonly List<int> queue = new List<int>();
        private BrawlCharacterArt art;
        private Camera studio;
        private Light keyLight;

        /// <summary>Portraits rendered so far (each look once).</summary>
        public int Rendered { get; private set; }
        public int Pending => queue.Count;

        public void Configure(BrawlCharacterArt characterArt) => art = characterArt;

        private static int Key(int character, int skin)
        {
            character = Mathf.Clamp(character, 0, BrawlRoster.Count - 1);
            return character * 64 + BrawlRoster.WrapSkin(character, skin);
        }

        /// <summary>The portrait, or null while it is still queued.</summary>
        public Texture Get(int character, int skin)
        {
            int key = Key(character, skin);
            if (cache.TryGetValue(key, out var texture) && texture != null) return texture;
            if (!queue.Contains(key)) queue.Add(key);
            return null;
        }

        /// <summary>Queues every look of every character (selection screen).</summary>
        public void Prewarm()
        {
            for (int c = 0; c < BrawlRoster.Count; c++)
                for (int s = 0; s < BrawlRoster.SkinCount(c); s++) Get(c, s);
        }

        /// <summary>Renders everything queued right now (tests and loading screens).</summary>
        public void Flush() { while (queue.Count > 0) RenderNext(); }

        private void LateUpdate() { if (queue.Count > 0) RenderNext(); }

        private void RenderNext()
        {
            int key = queue[0]; queue.RemoveAt(0);
            if (cache.ContainsKey(key)) return;
            int character = key / 64, skin = key % 64;
            EnsureStudio();
            var holder = new GameObject("Portrait " + character + "-" + skin);
            holder.transform.position = StudioOrigin;
            holder.transform.rotation = Quaternion.Euler(0, PortraitYaw, 0);
            var visual = holder.AddComponent<BrawlFighterVisual>();
            visual.Build(character, skin, BrawlRoster.Get(character).Accent, art);
            visual.Animate(new BrawlPose { Grounded = true }, true);
            Bounds bounds = new Bounds(StudioOrigin + Vector3.up, Vector3.one);
            bool any = false;
            foreach (var renderer in holder.GetComponentsInChildren<Renderer>(true))
            {
                renderer.gameObject.layer = Layer;
                if (renderer is SkinnedMeshRenderer skinned) skinned.updateWhenOffscreen = true;
                if (!any) { bounds = renderer.bounds; any = true; } else bounds.Encapsulate(renderer.bounds);
            }
            foreach (var collider in holder.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (Transform t in holder.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layer;

            // Head and shoulders: the upper 45 % of the figure, framed square.
            float height = Mathf.Max(.5f, bounds.size.y);
            Vector3 focus = new Vector3(bounds.center.x, bounds.max.y - height * .24f, bounds.center.z);
            studio.orthographicSize = height * .27f;
            studio.transform.position = focus + new Vector3(0, height * .04f, 6f);
            studio.transform.rotation = Quaternion.LookRotation(Vector3.back);
            keyLight.transform.position = focus + new Vector3(2, 3, 4);

            var texture = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32) { name = "Portrait " + BrawlRoster.SkinName(character, skin), antiAliasing = 4 };
            texture.Create();
            studio.targetTexture = texture;
            studio.enabled = true;
            studio.Render();
            studio.enabled = false;
            studio.targetTexture = null;
            holder.SetActive(false);
            Destroy(holder);
            cache[key] = texture;
            Rendered++;
        }

        private void EnsureStudio()
        {
            if (studio != null) return;
            var root = new GameObject("Portrait studio");
            root.transform.SetParent(transform, false);
            root.transform.position = StudioOrigin;
            studio = root.AddComponent<Camera>();
            studio.orthographic = true;
            studio.cullingMask = 1 << Layer;
            studio.clearFlags = CameraClearFlags.SolidColor;
            studio.backgroundColor = Background;
            studio.nearClipPlane = .1f; studio.farClipPlane = 20f;
            studio.allowHDR = false; studio.allowMSAA = true;
            studio.enabled = false;
            var lightObject = new GameObject("Portrait key light");
            lightObject.transform.SetParent(root.transform, false);
            keyLight = lightObject.AddComponent<Light>();
            keyLight.type = LightType.Point; keyLight.range = 30; keyLight.intensity = 6f; keyLight.color = new Color(1f, .96f, .9f);
            keyLight.cullingMask = 1 << Layer;
        }

        /// <summary>Reads a portrait back (tests): share of pixels that differ from the background, and the mean colour.</summary>
        public static float Coverage(Texture texture, out Color mean)
        {
            mean = Color.clear;
            if (!(texture is RenderTexture rt)) return 0;
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var read = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            read.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); read.Apply();
            RenderTexture.active = previous;
            Color[] pixels = read.GetPixels();
            Destroy(read);
            int covered = 0; Color sum = Color.clear;
            foreach (Color p in pixels)
            {
                sum += p;
                if (Mathf.Abs(p.r - Background.r) + Mathf.Abs(p.g - Background.g) + Mathf.Abs(p.b - Background.b) > .08f) covered++;
            }
            mean = sum / Mathf.Max(1, pixels.Length);
            return covered / (float)Mathf.Max(1, pixels.Length);
        }

        /// <summary>Mean per-channel difference between two portraits (0–1), on a 64×64 grid.</summary>
        public static float Difference(Texture a, Texture b)
        {
            Color[] pa = Grid(a), pb = Grid(b);
            if (pa == null || pb == null) return 0;
            float sum = 0;
            for (int i = 0; i < pa.Length; i++) sum += (Mathf.Abs(pa[i].r - pb[i].r) + Mathf.Abs(pa[i].g - pb[i].g) + Mathf.Abs(pa[i].b - pb[i].b)) / 3;
            return sum / pa.Length;
        }

        private static Color[] Grid(Texture texture)
        {
            if (!(texture is RenderTexture rt)) return null;
            var previous = RenderTexture.active;
            var small = RenderTexture.GetTemporary(64, 64, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(rt, small);
            RenderTexture.active = small;
            var read = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            read.ReadPixels(new Rect(0, 0, 64, 64), 0, 0); read.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(small);
            Color[] pixels = read.GetPixels();
            Destroy(read);
            return pixels;
        }

        private void OnDestroy()
        {
            foreach (var texture in cache.Values) if (texture != null) { texture.Release(); Destroy(texture); }
            cache.Clear();
        }
    }
}
