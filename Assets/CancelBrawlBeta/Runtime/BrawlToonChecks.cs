using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// <c>-toon-smoke</c>: the fighters' single toon material and outline (GDD 2.0 §11). Checks the shader is in the
    /// build and used by every fighter, that it keeps shadow and depth passes, that colour tints (hit flash,
    /// protection) still reach it, and measures the outline on the portraits' silhouettes. Saves captures per arena.
    /// </summary>
    public static class BrawlToonChecks
    {
        [Serializable] private sealed class Row { public string character; public int skin; public float edgeLuminance, innerLuminance; }
        [Serializable] private sealed class Report { public bool shaderFound; public int fighterRenderers, toonRenderers; public List<Row> rows = new List<Row>(); }

        public static IEnumerator Run(BrawlMatch match, Action<bool, string> check, string output)
        {
            match.AutomatedTest = true;
            var report = new Report();
            Shader shader = BrawlFighterVisual.ToonShader;
            report.shaderFound = shader != null;
            check(shader != null && shader.isSupported, "El shader toon FUNADOS/Toon está en la build y es compatible");
            if (shader == null) yield break;
            var probe = new Material(shader);
            check(probe.FindPass("Outline") >= 0 && probe.FindPass("ShadowCaster") >= 0 && probe.FindPass("DepthOnly") >= 0,
                "El material toon tiene contorno, sombra y profundidad");
            check(probe.HasProperty("_BaseColor") && probe.HasProperty("_BaseMap"), "El material toon acepta color y textura (destello de golpe y aspectos)");
            UnityEngine.Object.Destroy(probe);

            // Every fighter in a five-player match uses it.
            match.OpponentCount = 4; match.ArenaIndex = 0;
            match.StartMatch();
            for (int i = 0; i < 10; i++) yield return null;
            foreach (var fighter in match.Fighters)
                foreach (var renderer in fighter.GetComponentsInChildren<Renderer>())
                {
                    if (renderer is LineRenderer) continue;
                    report.fighterRenderers++;
                    if (renderer.sharedMaterial && renderer.sharedMaterial.shader == shader) report.toonRenderers++;
                }
            check(report.fighterRenderers > 0 && report.toonRenderers == report.fighterRenderers,
                $"Los cinco luchadores usan el material toon ({report.toonRenderers}/{report.fighterRenderers} renderers)");
            bool shadows = true;
            foreach (var fighter in match.Fighters)
                foreach (var renderer in fighter.GetComponentsInChildren<SkinnedMeshRenderer>())
                    shadows &= renderer.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off;
            check(shadows, "Los luchadores toon siguen proyectando sombra");

            // Hit flash still tints the toon material (lab: no countdown or spawn protection).
            match.SelectedCharacter = 0; match.StartTraining();
            match.Fighters[0].ResetPractice(new Vector3(0, .05f, 0), 0);
            match.Fighters[1].ResetPractice(new Vector3(1.4f, .05f, 0), 0);
            yield return new WaitForSeconds(.3f);
            var target = match.Fighters[1];
            var material = target.GetComponentInChildren<SkinnedMeshRenderer>() ? target.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial : null;
            Color before = material ? material.color : Color.white;
            target.ReceiveHit(new AttackSpec(AttackKind.Heavy, 8, 0, .1f, .1f, 2, 3, 2, 1), Vector3.right, match.Fighters[0]);
            bool tinted = false;
            for (int i = 0; i < 8 && material; i++) { yield return null; tinted |= material.color != before; }
            check(material && tinted, "El destello de golpe sigue tiñendo el material toon");

            for (int arena = 0; arena < BrawlArena.Count; arena++)
            {
                match.ArenaIndex = arena; match.StartMatch();
                for (int i = 0; i < 40; i++) yield return null;
                yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, $"toon-arena-{arena}.png"));
                yield return null;
            }
            match.ReturnToMenu();
            for (int i = 0; i < 3; i++) yield return null;

            // Outline on the silhouettes: every character and look, rendered alone over a flat background.
            bool outlined = true;
            for (int c = 0; c < BrawlRoster.Count; c++)
                for (int s = 0; s < BrawlRoster.SkinCount(c); s++)
                {
                    RenderTexture texture = Snapshot(match.Settings.characterArt, c, s);
                    Silhouette(texture, Backdrop, out float edge, out float inner);
                    if (c == 0 && s == 0) Save(texture, Path.Combine(output, "toon-silhouette.png"));
                    texture.Release(); UnityEngine.Object.Destroy(texture);
                    report.rows.Add(new Row { character = BrawlRoster.Get(c).Name, skin = s, edgeLuminance = edge, innerLuminance = inner });
                    outlined &= edge < .12f && inner > edge;
                }
            check(outlined, "Cada silueta tiene contorno oscuro (borde < 0,12 de luminancia sobre fondo gris 0,54, y más oscuro que el interior)");
            check(GameObject.Find("Silueta toon") == null, "Las siluetas de prueba no dejan objetos en la escena");
            File.WriteAllText(Path.Combine(output, "toon-smoke.json"), JsonUtility.ToJson(report, true));
        }

        private static readonly Color Backdrop = new Color(.5f, .55f, .6f);
        private const int Layer = 29;

        /// <summary>One fighter alone, three-quarter view, over a flat grey, on a layer only this camera sees.</summary>
        private static RenderTexture Snapshot(BrawlCharacterArt art, int character, int skin)
        {
            var root = new GameObject("Silueta toon"); root.transform.position = new Vector3(0, -500, 0);
            root.transform.rotation = Quaternion.LookRotation(new Vector3(-.5f, 0, -.87f));
            root.AddComponent<BrawlFighterVisual>().Build(character, skin, BrawlRoster.Get(character).Accent, art);
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) renderer.gameObject.layer = Layer;
            // 720 px tall like the game at 720p: the outline width is a share of the screen height.
            var texture = new RenderTexture(480, 720, 16, RenderTextureFormat.ARGB32); texture.Create();
            var cameraObject = new GameObject("Cámara silueta"); var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 1.15f;
            camera.transform.position = root.transform.position + new Vector3(0, .95f, -6);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Backdrop;
            camera.cullingMask = 1 << Layer; camera.targetTexture = texture; camera.enabled = false;
            camera.Render(); camera.targetTexture = null;
            UnityEngine.Object.Destroy(cameraObject); root.SetActive(false); UnityEngine.Object.Destroy(root);
            return texture;
        }

        private static void Save(RenderTexture source, string path)
        {
            var previous = RenderTexture.active; RenderTexture.active = source;
            var read = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            read.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0); read.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(path, read.EncodeToPNG()); UnityEngine.Object.Destroy(read);
        }

        /// <summary>Mean luminance of the first body pixel next to the background (edge) and of pixels 4 px further in.</summary>
        private static void Silhouette(Texture texture, Color background, out float edge, out float inner)
        {
            edge = 1; inner = 0;
            if (!(texture is RenderTexture source)) return;
            var previous = RenderTexture.active; RenderTexture.active = source;
            var read = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            read.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0); read.Apply();
            RenderTexture.active = previous;
            Color[] pixels = read.GetPixels(); int w = read.width, h = read.height; UnityEngine.Object.Destroy(read);
            bool Body(int x, int y) { Color p = pixels[y * w + x]; return Mathf.Abs(p.r - background.r) + Mathf.Abs(p.g - background.g) + Mathf.Abs(p.b - background.b) > .1f; }
            float L(int x, int y) { Color p = pixels[y * w + x]; return .2126f * p.r + .7152f * p.g + .0722f * p.b; }
            double edgeSum = 0, innerSum = 0; int edges = 0, inners = 0;
            for (int y = 8; y < h - 8; y += 4)
                for (int x = 1; x < w - 6; x++)
                    if (!Body(x - 1, y) && Body(x, y))
                    {
                        edgeSum += L(x, y); edges++;
                        if (Body(x + 5, y)) { innerSum += L(x + 5, y); inners++; }
                        break; // first boundary from the left on this row
                    }
            if (edges > 0) edge = (float)(edgeSum / edges);
            if (inners > 0) inner = (float)(innerSum / inners);
        }
    }
}
