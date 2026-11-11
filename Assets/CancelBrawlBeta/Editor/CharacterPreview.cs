using System.IO;
using UnityEditor;
using UnityEngine;

namespace CancelBrawl.Beta.Editor
{
    /// <summary>Renders every look in six key poses to Evidence/characters (checks the auto-rig without playing).</summary>
    public static class CharacterPreview
    {
        private const int Tile = 300;

        [MenuItem("Cancel Brawl/Previsualizar poses de personaje")]
        public static void RenderAll()
        {
            var settings = AssetDatabase.LoadAssetAtPath<BrawlSettings>("Assets/CancelBrawlBeta/Data/BetaBalance.asset");
            BrawlCharacterArt art = CharacterArtSetup.Ensure(settings);
            Directory.CreateDirectory("Evidence/characters");
            foreach (var old in Directory.GetFiles("Evidence/characters", "*.png")) File.Delete(old);

            var poses = new (string label, BrawlPose pose, float gait)[]
            {
                ("reposo", new BrawlPose { Grounded = true }, 0),
                ("carrera", new BrawlPose { Grounded = true, LocalVelocity = new Vector3(0, 0, 7) }, Mathf.PI / 2),
                ("salto", new BrawlPose { Grounded = false, VerticalSpeed = 6 }, 0),
                ("golpe", Attack(AttackKind.Basic, 0, .45f), 0),
                ("fuerte", Attack(AttackKind.Heavy, 0, .3f), 0),
                ("super", Attack(AttackKind.Super, 0, .7f), 0),
            };

            var light = new GameObject("PreviewLight").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.3f;
            light.transform.rotation = Quaternion.Euler(40, 150, 0);
            RenderSettings.ambientLight = new Color(.55f, .6f, .7f);

            for (int k = 0; k < BrawlRoster.Count; k++)
                for (int s = 0; s < BrawlRoster.SkinCount(k); s++)
                {
                    var holder = new GameObject("Preview");
                    var visual = holder.AddComponent<BrawlFighterVisual>();
                    visual.Build(k, s, Color.cyan, art);
                    var sheet = new Texture2D(Tile * poses.Length, Tile, TextureFormat.RGB24, false);
                    for (int i = 0; i < poses.Length; i++)
                    {
                        visual.SetGait(poses[i].gait);
                        for (int f = 0; f < 3; f++) visual.Animate(poses[i].pose, true);
                        // Edit mode never runs the skinning update, so bake the posed mesh on the CPU for the shot.
                        var skin = holder.GetComponentInChildren<SkinnedMeshRenderer>();
                        GameObject baked = null;
                        if (skin != null)
                        {
                            var mesh = new Mesh(); skin.BakeMesh(mesh, true);
                            baked = new GameObject("Baked");
                            baked.transform.SetPositionAndRotation(skin.transform.position, skin.transform.rotation);
                            baked.AddComponent<MeshFilter>().sharedMesh = mesh;
                            baked.AddComponent<MeshRenderer>().sharedMaterial = skin.sharedMaterial;
                            skin.enabled = false;
                        }
                        RenderTile(sheet, i);
                        if (skin != null) { skin.enabled = true; Object.DestroyImmediate(baked.GetComponent<MeshFilter>().sharedMesh); Object.DestroyImmediate(baked); }
                    }
                    sheet.Apply();
                    string file = $"Evidence/characters/{k}_{BrawlRoster.Get(k).Name}_{s}_{visual.SkinName}.png".Replace(' ', '_');
                    File.WriteAllBytes(file, sheet.EncodeToPNG());
                    Debug.Log("PREVIEW_POSES " + file + " model=" + visual.UsesModel);
                    Object.DestroyImmediate(sheet);
                    Object.DestroyImmediate(holder);
                }
            Object.DestroyImmediate(light.gameObject);
        }

        private static BrawlPose Attack(AttackKind kind, int combo, float progress) => new BrawlPose
        {
            Grounded = true, Attacking = true, Attack = kind, Combo = combo, AttackProgress = progress,
            WindupEnd = kind == AttackKind.Basic ? .24f : kind == AttackKind.Super ? .55f : .4f,
            ActiveEnd = kind == AttackKind.Basic ? .55f : kind == AttackKind.Super ? .65f : .55f,
        };

        private static void RenderTile(Texture2D sheet, int index)
        {
            var camGo = new GameObject("PreviewCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.16f, .18f, .24f);
            cam.fieldOfView = 30; cam.nearClipPlane = .05f; cam.farClipPlane = 50;
            cam.transform.position = new Vector3(2.6f, 1.9f, 4.6f);
            cam.transform.LookAt(new Vector3(0, .95f, 0));
            var rt = new RenderTexture(Tile, Tile, 24); cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            sheet.ReadPixels(new Rect(0, 0, Tile, Tile), index * Tile, 0);
            RenderTexture.active = null; cam.targetTexture = null;
            Object.DestroyImmediate(rt); Object.DestroyImmediate(camGo);
        }
    }
}
