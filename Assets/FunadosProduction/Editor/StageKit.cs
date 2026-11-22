using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Funados.Production.Environments
{
    /// <summary>
    /// Shared tooling to assemble a stage prefab from Meshy pieces stored in
    /// Environments/&lt;Stage&gt;/Models/&lt;Key&gt;/&lt;Prefix&gt;_&lt;Key&gt;.fbx (+ _base_color.png).
    /// Every piece is rescaled from its measured bounds, so layouts are written in metres regardless of
    /// the scale the FBX files were exported with. Prefab origin = centre of the main platform's
    /// walkable top (y = 0), like the beta arenas.
    /// </summary>
    internal sealed class StageKit
    {
        private readonly string root;
        private readonly string stage;
        private readonly string prefix;

        public StageKit(string stage, string prefix)
        {
            this.stage = stage;
            this.prefix = prefix;
            root = "Assets/FunadosProduction/Environments/" + stage;
        }

        public string PrefabPath => root + "/" + stage + ".prefab";
        public string PreviewPath => root + "/Concept/" + stage + "_UnityPreview.png";

        public void Build(string displayName, Action<Transform> layout)
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject rootObject = new GameObject(displayName);
                SceneManager.MoveGameObjectToScene(rootObject, scene);
                layout(rootObject.transform);
                PrefabUtility.SaveAsPrefabAsset(rootObject, PrefabPath);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("FUNADOS: " + displayName + " armado en " + PrefabPath);
        }

        /// <summary>Renders the saved prefab, so the capture verifies what the game will load.</summary>
        public void Capture(Color background)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (!prefab) throw new FileNotFoundException("Arma primero el escenario: falta " + PrefabPath);

            Scene scene = EditorSceneManager.NewPreviewScene();
            RenderTexture target = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            Texture2D image = new Texture2D(1600, 900, TextureFormat.RGBA32, false);
            // Without this a first render shows placeholder shaders while variants compile in the background.
            bool asyncCompilation = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            try
            {
                PrefabUtility.InstantiatePrefab(prefab, scene);

                GameObject sun = new GameObject("Preview light");
                SceneManager.MoveGameObjectToScene(sun, scene);
                sun.transform.rotation = Quaternion.Euler(46f, -28f, 0f);
                Light light = sun.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.6f;

                GameObject eye = new GameObject("Preview camera");
                SceneManager.MoveGameObjectToScene(eye, scene);
                eye.transform.position = new Vector3(0f, 15f, -25f);
                eye.transform.LookAt(new Vector3(0f, 0.5f, 3.5f));
                Camera camera = eye.AddComponent<Camera>();
                camera.scene = scene;
                camera.fieldOfView = 45f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = background;
                camera.targetTexture = target;
                // The first frame of a freshly opened editor (batch mode) binds one texture to every
                // material; warm-up renders let the textures upload before the frame that is saved.
                for (int frame = 0; frame < 3; frame++) camera.Render();

                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                image.Apply();
                RenderTexture.active = null;
                camera.targetTexture = null;
                File.WriteAllBytes(PreviewPath, image.EncodeToPNG());
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = asyncCompilation;
                EditorSceneManager.ClosePreviewScene(scene);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(image);
            }
            AssetDatabase.ImportAsset(PreviewPath);
            Debug.Log("FUNADOS: vista previa guardada en " + PreviewPath);
        }

        public Transform Group(Transform parent, string name)
        {
            Transform group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        /// <summary>Solid combat surface stretched to an exact box, with a flat box collider.</summary>
        public void Platform(Transform parent, string key, string name, Vector3 basePoint, Vector3 size, float yaw = 0f)
        {
            Piece(parent, key, name, basePoint, size, true, yaw, true, 0.2f);
        }

        /// <summary>Decoration without collider, scaled uniformly to <paramref name="height"/>.</summary>
        public void Decor(Transform parent, string key, string name, Vector3 basePoint, float height, float yaw = 0f, float glow = 0.2f)
        {
            Piece(parent, key, name, basePoint, new Vector3(0f, height, 0f), false, yaw, false, glow);
        }
        /// <summary>Plain box (structure, floors, walls). Only <paramref name="solid"/> blocks keep a collider.</summary>
        public void Block(Transform parent, string name, Vector3 center, Vector3 size, Material material, bool solid = false)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            if (!solid) Object.DestroyImmediate(block.GetComponent<Collider>());
            block.transform.SetParent(parent, false);
            block.transform.localPosition = center;
            block.transform.localScale = size;
            MeshRenderer renderer = block.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>
        /// Walkable floor behind the main platform plus an invisible wall that closes it, so the back of
        /// the stage is solid ground instead of a ledge. The floor starts under the platform's back edge
        /// (no gap to fall through) and its top sits one step below the platform top.
        /// </summary>
        /// <param name="top">Floor height; keep it within the fighters' step offset (0.22 m) of y = 0.</param>
        /// <param name="wallZ">Depth where fighters are stopped, in front of the non-solid scenery.</param>
        /// <param name="backZ">Depth where the visible floor ends.</param>
        public void BackFloor(Transform parent, float top, float width, float wallZ, float backZ, Material floor, Material trim)
        {
            const float frontZ = 5.3f, depthBelow = 12f;
            Block(parent, "Back floor", new Vector3(0f, top - depthBelow * 0.5f, (frontZ + backZ) * 0.5f),
                new Vector3(width, depthBelow, backZ - frontZ), floor, true);
            Block(parent, "Back floor trim", new Vector3(0f, top - 0.15f, frontZ - 0.05f), new Vector3(width + 0.1f, 0.3f, 0.12f), trim);

            GameObject wall = new GameObject("Back wall (invisible)");
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = new Vector3(0f, top + 20f, wallZ + 0.5f);
            wall.AddComponent<BoxCollider>().size = new Vector3(width, 40f, 1f);
        }

        public Material SolidMaterial(string key, Color color, float glow = 0f)
        {
            Material material = LoadOrCreate(key);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.1f);
            if (glow > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                material.SetColor("_EmissionColor", color * glow);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <param name="basePoint">Bottom centre of the piece.</param>
        /// <param name="size">Exact box when <paramref name="exact"/>; otherwise only y (height) is used, uniformly.</param>
        private void Piece(Transform parent, string key, string name, Vector3 basePoint, Vector3 size, bool exact, float yaw, bool solid, float glow)
        {
            string modelPath = root + "/Models/" + key + "/" + prefix + "_" + key + ".fbx";
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (!model) throw new FileNotFoundException("Falta el modelo " + modelPath);

            Transform holder = new GameObject(name).transform;
            holder.SetParent(parent, false);
            Transform fit = new GameObject("Fit").transform;
            fit.SetParent(holder, false);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model, fit);

            Material material = TexturedMaterial(key, glow);
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
            {
                Material[] slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++) slots[i] = material;
                renderer.sharedMaterials = slots;
            }

            // Measure while every parent is still at identity.
            Bounds bounds = Measure(instance);
            if (exact && bounds.size.x < bounds.size.z)
            {
                // Platforms: long side along X.
                instance.transform.rotation = Quaternion.Euler(0f, 90f, 0f) * instance.transform.rotation;
                bounds = Measure(instance);
            }
            fit.localScale = exact
                ? new Vector3(size.x / bounds.size.x, size.y / bounds.size.y, size.z / bounds.size.z)
                : Vector3.one * (size.y / bounds.size.y);
            bounds = Measure(instance);
            fit.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);

            holder.localPosition = basePoint;
            holder.localRotation = Quaternion.Euler(0f, yaw, 0f);

            if (solid)
            {
                BoxCollider box = holder.gameObject.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, size.y * 0.5f, 0f);
                box.size = size;
            }
        }

        private static Bounds Measure(GameObject target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new InvalidDataException(target.name + " no tiene malla visible.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private Material TexturedMaterial(string key, float glow)
        {
            string texturePath = root + "/Models/" + key + "/" + prefix + "_" + key + "_base_color.png";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (!texture) throw new FileNotFoundException("Falta la textura " + texturePath);
            Material material = LoadOrCreate(key);
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.05f);
            // The painted cel-shaded texture also feeds emission so the colours stay vivid under any light.
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            material.SetTexture("_EmissionMap", texture);
            material.SetColor("_EmissionColor", Color.white * glow);
            EditorUtility.SetDirty(material);
            return material;
        }

        private Material LoadOrCreate(string key)
        {
            string folder = root + "/Materials";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(root, "Materials");
            string path = folder + "/" + prefix + "_" + key + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material) return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) throw new InvalidDataException("No se encontró el shader URP Lit.");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
