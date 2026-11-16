using UnityEditor;

namespace Funados.Production.Environments
{
    /// <summary>Batch-mode entry points (Unity -batchmode -executeMethod ...).</summary>
    public static class StageBatch
    {
        public static void InfluencerRooftop()
        {
            AssetDatabase.Refresh();
            InfluencerRooftopBuilder.Build();
            InfluencerRooftopBuilder.Capture();
        }

        /// <summary>Rebuilds the four stages, refreshes their previews and registers them in the game.</summary>
        public static void All()
        {
            InfluencerRooftop();
            CorporateOfficeTower();
            TvDebateStudio();
            ColonialPlaza();
            StageCatalog.Assign();
        }

        public static void ColonialPlaza()
        {
            AssetDatabase.Refresh();
            ColonialPlazaBuilder.Build();
            ColonialPlazaBuilder.Capture();
        }

        public static void TvDebateStudio()
        {
            AssetDatabase.Refresh();
            TvDebateStudioBuilder.Build();
            TvDebateStudioBuilder.Capture();
        }

        public static void CorporateOfficeTower()
        {
            AssetDatabase.Refresh();
            CorporateOfficeTowerBuilder.Build();
            CorporateOfficeTowerBuilder.Capture();
        }
    }
}
