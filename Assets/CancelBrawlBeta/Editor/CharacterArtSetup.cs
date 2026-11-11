using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CancelBrawl.Beta.Editor
{
    /// <summary>
    /// Imports every model folder in Art/Characters (FBX + "_Albedo" texture) and lists it in the character catalog.
    /// To add a look: drop "Name/Name.fbx" and "Name/Name_Albedo.png" in Art/Characters and add "Name" to a kit's Skins.
    /// </summary>
    public static class CharacterArtSetup
    {
        public const string Root = "Assets/CancelBrawlBeta/Art/Characters";
        public const string CatalogPath = "Assets/CancelBrawlBeta/Data/CharacterArt.asset";

        [MenuItem("Cancel Brawl/Actualizar modelos de personaje")]
        public static void RefreshMenu()
        {
            var settings = AssetDatabase.LoadAssetAtPath<BrawlSettings>("Assets/CancelBrawlBeta/Data/BetaBalance.asset");
            Ensure(settings);
        }

        public static BrawlCharacterArt Ensure(BrawlSettings settings)
        {
            var entries = new List<BrawlCharacterArt.Entry>();
            if (Directory.Exists(Root))
                foreach (string dir in Directory.GetDirectories(Root))
                {
                    string id = Path.GetFileName(dir);
                    string modelPath = $"{Root}/{id}/{id}.fbx", texturePath = $"{Root}/{id}/{id}_Albedo.png";
                    if (!File.Exists(modelPath)) { Debug.LogWarning("CHARACTER_ART sin FBX: " + id); continue; }
                    ConfigureModel(modelPath);
                    if (File.Exists(texturePath)) ConfigureTexture(texturePath);
                    entries.Add(new BrawlCharacterArt.Entry
                    {
                        id = id,
                        model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath),
                        albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath)
                    });
                }

            var catalog = AssetDatabase.LoadAssetAtPath<BrawlCharacterArt>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<BrawlCharacterArt>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.entries = entries.ToArray();
            EditorUtility.SetDirty(catalog);
            if (settings != null && settings.characterArt != catalog)
            {
                settings.characterArt = catalog;
                EditorUtility.SetDirty(settings);
            }
            foreach (BrawlKit kit in BrawlRoster.Kits)
                foreach (string skin in kit.Skins)
                    if (catalog.Find(skin) == null) Debug.LogWarning($"CHARACTER_ART falta el modelo '{skin}' de {kit.Name}");
            AssetDatabase.SaveAssets();
            Debug.Log("CHARACTER_ART_READY " + entries.Count);
            return catalog;
        }

        private static void ConfigureModel(string path)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            if (importer == null) return;
            bool changed = !importer.isReadable || importer.materialImportMode != ModelImporterMaterialImportMode.None
                || importer.importAnimation || importer.animationType != ModelImporterAnimationType.None
                || importer.importCameras || importer.importLights;
            if (!changed) return;
            importer.isReadable = true; // the runtime auto-rig reads the vertices
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importCameras = false; importer.importLights = false;
            importer.SaveAndReimport();
        }

        private static void ConfigureTexture(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null || importer.maxTextureSize == 2048 && importer.sRGBTexture && importer.mipmapEnabled) return;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true; importer.mipmapEnabled = true;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
    }
}
