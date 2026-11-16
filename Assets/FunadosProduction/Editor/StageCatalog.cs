using System;
using CancelBrawl.Beta;
using UnityEditor;
using UnityEngine;

namespace Funados.Production.Environments
{
    /// <summary>
    /// Registers the stage prefabs in the shared balance asset, in court order, so production scenes
    /// play on them instead of the beta greyboxes (see BrawlMatch.UsesStageArt).
    /// </summary>
    public static class StageCatalog
    {
        private const string BalancePath = "Assets/CancelBrawlBeta/Data/BetaBalance.asset";

        // Same order as BrawlArena.StageNames.
        private static readonly string[] Stages = { "InfluencerRooftop", "CorporateOfficeTower", "TvDebateStudio", "ColonialPlaza" };

        [MenuItem("FUNADOS/Escenarios/Asignar escenarios al juego")]
        public static void Assign()
        {
            var settings = AssetDatabase.LoadAssetAtPath<BrawlSettings>(BalancePath);
            if (!settings) throw new InvalidOperationException("Falta " + BalancePath);
            var prefabs = new GameObject[Stages.Length];
            for (int i = 0; i < Stages.Length; i++)
            {
                string path = "Assets/FunadosProduction/Environments/" + Stages[i] + "/" + Stages[i] + ".prefab";
                prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!prefabs[i]) throw new InvalidOperationException("Falta el escenario " + path);
            }
            settings.stagePrefabs = prefabs;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("FUNADOS: " + prefabs.Length + " escenarios asignados a " + BalancePath);
        }

        [MenuItem("FUNADOS/Escenarios/Quitar escenarios del juego (volver a canchas grises)")]
        public static void Clear()
        {
            var settings = AssetDatabase.LoadAssetAtPath<BrawlSettings>(BalancePath);
            if (!settings) throw new InvalidOperationException("Falta " + BalancePath);
            settings.stagePrefabs = new GameObject[0];
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("FUNADOS: escenarios retirados; producción vuelve a las canchas grises.");
        }
    }
}
