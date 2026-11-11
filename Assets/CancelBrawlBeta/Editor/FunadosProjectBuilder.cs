using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CancelBrawl.Beta.Editor
{
    /// <summary>Production scenes reuse the preserved beta's combat and art catalog.</summary>
    public static class FunadosProjectBuilder
    {
        public const string MainScene = "Assets/FunadosProduction/Scenes/Funados.unity";
        public const string LabScene = "Assets/FunadosProduction/Scenes/CombatLab.unity";
        public const string BetaScene = "Assets/CancelBrawlBeta/Scenes/CancelBrawlBeta.unity";

        [MenuItem("FUNADOS/Crear escenas de producción si faltan")]
        public static void EnsureScenes()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Detén Play antes de preparar escenas.");
            Scene current = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(current.path))
            {
                if (current.isDirty) throw new InvalidOperationException("Guarda la escena sin título antes de preparar producción.");
                EditorSceneManager.OpenScene(BetaScene);
            }
            var settings = AssetDatabase.LoadAssetAtPath<BrawlSettings>("Assets/CancelBrawlBeta/Data/BetaBalance.asset");
            if (!settings || !settings.characterArt) throw new InvalidOperationException("Falta el catálogo original de personajes.");
            Directory.CreateDirectory("Assets/FunadosProduction/Scenes");
            AssetDatabase.Refresh();
            CreateMissing(MainScene, false, settings);
            CreateMissing(LabScene, true, settings);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainScene, true),
                new EditorBuildSettingsScene(LabScene, true),
                new EditorBuildSettingsScene(BetaScene, true)
            };
            AssetDatabase.SaveAssets();
        }

        private static void CreateMissing(string path, bool training, BrawlSettings settings)
        {
            if (File.Exists(path)) return;
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var root = new GameObject(training ? "FUNADOS · CombatLab" : "FUNADOS · juego local");
                var match = root.AddComponent<BrawlMatch>();
                var serialized = new SerializedObject(match);
                serialized.FindProperty("settings").objectReferenceValue = settings;
                serialized.FindProperty("productionPresentation").boolValue = true;
                serialized.FindProperty("startInTraining").boolValue = training;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                RenderSettings.ambientLight = new Color(.53f, .62f, .75f);
                RenderSettings.ambientIntensity = 1;
                if (!EditorSceneManager.SaveScene(scene, path)) throw new IOException("No se guardó " + path);
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [MenuItem("FUNADOS/Abrir juego local")]
        public static void OpenMain() => Open(MainScene);

        [MenuItem("FUNADOS/Abrir CombatLab")]
        public static void OpenLab() => Open(LabScene);

        private static void Open(string path)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Detén Play antes de cambiar de escena.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureScenes(); EditorSceneManager.OpenScene(path);
        }

        [MenuItem("FUNADOS/Generar Windows")]
        public static void BuildWindows()
        {
            EnsureScenes();
            // The original beta builder owns runtime shader inclusion; configure then restore production's scene list.
            BetaProjectBuilder.ConfigureProject();
            EnsureScenes();
            PlayerSettings.productName = "FUNADOS";
            PlayerSettings.bundleVersion = "0.4.0";
            Directory.CreateDirectory("Builds/Funados/Windows");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { MainScene, LabScene, BetaScene },
                locationPathName = "Builds/Funados/Windows/FUNADOS.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            Directory.CreateDirectory("Evidence/Production-2026-11-11");
            File.WriteAllText("Evidence/Production-2026-11-11/build.txt",
                $"result={report.summary.result}\nerrors={report.summary.totalErrors}\nwarnings={report.summary.totalWarnings}\nbytes={report.summary.totalSize}\nseconds={report.summary.totalTime.TotalSeconds}\n");
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("FUNADOS build: " + report.summary.result);
            Debug.Log("FUNADOS_BUILD_SUCCEEDED");
        }
    }
}
