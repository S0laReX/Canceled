using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace CancelBrawl.Beta.Editor
{
    public static class BetaProjectBuilder
    {
        private const string ScenePath = "Assets/CancelBrawlBeta/Scenes/CancelBrawlBeta.unity";
        private const string BalancePath = "Assets/CancelBrawlBeta/Data/BetaBalance.asset";
        private const string OccluderPath = "Assets/CancelBrawlBeta/Data/OccluderFade.mat";

        /// <summary>Recreates the beta scene from scratch. Replaces the scene currently open in the Editor.</summary>
        [MenuItem("Cancel Brawl/Preparar escena beta")]
        public static void CreateScene()
        {
            Directory.CreateDirectory("Assets/CancelBrawlBeta/Scenes");
            BrawlSettings settings = EnsureData();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Cancel Brawl · beta local");
            var match = root.AddComponent<BrawlMatch>();
            var serializedMatch = new SerializedObject(match);
            serializedMatch.FindProperty("settings").objectReferenceValue = settings;
            serializedMatch.ApplyModifiedPropertiesWithoutUndo();
            if (serializedMatch.FindProperty("settings").objectReferenceValue == null)
                throw new System.InvalidOperationException("No se pudo asignar BetaBalance a la escena");
            RenderSettings.ambientLight = new Color(.53f, .62f, .75f);
            RenderSettings.ambientIntensity = 1;
            EditorSceneManager.SaveScene(scene, ScenePath);
            if (!File.ReadAllText(ScenePath).Contains(AssetDatabase.AssetPathToGUID(BalancePath)))
                throw new System.InvalidOperationException("La escena guardada perdió la referencia de balance");
            ConfigureProject();
            Debug.Log("BETA_SCENE_READY " + ScenePath);
        }

        /// <summary>Data assets, player settings and shaders; never touches open scenes.</summary>
        [MenuItem("Cancel Brawl/Actualizar datos y ajustes de la beta")]
        public static void ConfigureProject()
        {
            EnsureData();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.companyName = "Cancel Brawl";
            PlayerSettings.productName = "Cancel Brawl Beta";
            PlayerSettings.bundleVersion = BrawlMatch.Version;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            IncludeRuntimeShaders();
            AssetDatabase.SaveAssets();
            Debug.Log("BETA_PROJECT_READY " + BrawlMatch.Version);
        }

        private static BrawlSettings EnsureData()
        {
            Directory.CreateDirectory("Assets/CancelBrawlBeta/Data");
            var settings = AssetDatabase.LoadAssetAtPath<BrawlSettings>(BalancePath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<BrawlSettings>();
                AssetDatabase.CreateAsset(settings, BalancePath);
            }
            var occluder = AssetDatabase.LoadAssetAtPath<Material>(OccluderPath);
            if (occluder == null)
            {
                Shader lit = Shader.Find("Universal Render Pipeline/Lit");
                if (lit == null) throw new System.InvalidOperationException("Falta shader URP Lit");
                occluder = new Material(lit) { name = "OccluderFade" };
                AssetDatabase.CreateAsset(occluder, OccluderPath);
            }
            MakeTransparent(occluder);
            EditorUtility.SetDirty(occluder);
            if (settings.occluderMaterial != occluder)
            {
                settings.occluderMaterial = occluder;
                EditorUtility.SetDirty(settings);
            }
            CharacterArtSetup.Ensure(settings);
            AssetDatabase.SaveAssets();
            return settings;
        }

        /// <summary>URP Lit, transparent alpha blend. Keeping it as an asset keeps the variant in the build.</summary>
        private static void MakeTransparent(Material material)
        {
            material.SetFloat("_Surface", 1);
            material.SetFloat("_Blend", 0);
            material.SetFloat("_AlphaClip", 0);
            material.SetFloat("_ZWrite", 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetShaderPassEnabled("DepthOnly", false);
            material.SetShaderPassEnabled("ShadowCaster", false);
            material.color = new Color(.5f, .55f, .65f, .3f);
        }

        private static void IncludeRuntimeShaders()
        {
            Object graphics = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset").First();
            var serialized = new SerializedObject(graphics);
            var included = serialized.FindProperty("m_AlwaysIncludedShaders");
            foreach (string name in new[] { "Universal Render Pipeline/Lit", "Universal Render Pipeline/Unlit", BrawlImpactFx.ParticleShader })
            {
                Shader shader = Shader.Find(name);
                if (shader == null) throw new System.InvalidOperationException("Falta shader " + name);
                bool exists = false;
                for (int i = 0; i < included.arraySize; i++) if (included.GetArrayElementAtIndex(i).objectReferenceValue == shader) exists = true;
                if (exists) continue;
                included.InsertArrayElementAtIndex(included.arraySize);
                included.GetArrayElementAtIndex(included.arraySize - 1).objectReferenceValue = shader;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [MenuItem("Cancel Brawl/Generar beta Windows")]
        public static void CreateAndBuild()
        {
            // Reuse the existing scene so an open, unsaved scene in the Editor is never discarded.
            if (File.Exists(ScenePath)) ConfigureProject();
            else CreateScene();
            Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath }, locationPathName = "Builds/Windows/CancelBrawlBeta.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            Debug.Log("BETA_BUILD " + report.summary.result + " bytes=" + report.summary.totalSize);
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.InvalidOperationException("La beta no compiló: " + report.summary.result);
        }
    }
}
