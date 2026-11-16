using UnityEditor;
using UnityEngine;

namespace Funados.Production.Environments
{
    /// <summary>
    /// CORPORATE OFFICE TOWER: panelled central platform, two suspended side platforms and a marble
    /// lobby behind with columns, panoramic window, executive balcony and the golden bull.
    /// </summary>
    public static class CorporateOfficeTowerBuilder
    {
        private static readonly StageKit Kit = new StageKit("CorporateOfficeTower", "CO");
        // One step below the main platform top, so fighters walk onto the solid lobby floor.
        private const float LobbyTop = -0.2f;
        private const float BalconyTop = 5.6f;

        [MenuItem("FUNADOS/Escenarios/Armar Corporate Office Tower")]
        public static void Build() => Kit.Build("Corporate Office Tower", Layout);

        [MenuItem("FUNADOS/Escenarios/Capturar vista previa de Corporate Office Tower")]
        public static void Capture() => Kit.Capture(new Color(0.16f, 0.12f, 0.2f));

        private static void Layout(Transform root)
        {
            // ── Combat surfaces (solid box colliders with exact flat tops) ──
            Transform combat = Kit.Group(root, "Combat surfaces");
            Kit.Platform(combat, "MainPlatform", "Main platform", new Vector3(0f, -1.2f, 0f), new Vector3(16f, 1.2f, 11f));
            // The side platform model has its support bracket on one side; the east copy is mirrored by yaw.
            Kit.Platform(combat, "SidePlatform", "West side platform", new Vector3(-11f, 0.3f, 1f), new Vector3(4.6f, 1f, 3.8f));
            Kit.Platform(combat, "SidePlatform", "East side platform", new Vector3(11f, 1.2f, 1f), new Vector3(4.6f, 1f, 3.8f), 180f);

            // ── Atrium structure (visual only) ──
            Transform structure = Kit.Group(root, "Structure");
            Material marble = Kit.SolidMaterial("Marble", new Color(0.62f, 0.58f, 0.7f));
            Material charcoal = Kit.SolidMaterial("Charcoal", new Color(0.14f, 0.13f, 0.17f));
            Material burgundy = Kit.SolidMaterial("Burgundy", new Color(0.42f, 0.09f, 0.2f));
            Material gold = Kit.SolidMaterial("Gold", new Color(0.95f, 0.72f, 0.2f), 0.25f);
            Material glass = Kit.SolidMaterial("Window", new Color(0.3f, 0.55f, 0.75f), 0.3f);
            Kit.Block(structure, "Stage tower", new Vector3(0f, -7.2f, 0f), new Vector3(15.4f, 12f, 10.4f), charcoal);
            Kit.BackFloor(structure, LobbyTop, 36f, 7.6f, 14.5f, marble, gold);
            Kit.Block(structure, "Panoramic window", new Vector3(0f, LobbyTop + 7f, 14.6f), new Vector3(36f, 14f, 0.3f), glass);
            for (int mullion = -3; mullion <= 3; mullion++)
                Kit.Block(structure, "Window mullion", new Vector3(mullion * 5f, LobbyTop + 7f, 14.4f), new Vector3(0.25f, 14f, 0.2f), charcoal);
            Kit.Block(structure, "Executive balcony", new Vector3(0f, BalconyTop - 0.3f, 12.4f), new Vector3(11f, 0.6f, 3.6f), burgundy);
            Kit.Block(structure, "Executive balcony trim", new Vector3(0f, BalconyTop - 0.3f, 10.58f), new Vector3(11.1f, 0.22f, 0.1f), gold);

            // ── Decoration in the lobby (no colliders), oversized to read from the gameplay camera ──
            Transform decor = Kit.Group(root, "Decoration");
            foreach (float x in new[] { -15.5f, -6.4f, 6.4f, 15.5f })
                Kit.Decor(decor, "Column", "Column " + x, L(x, 13.2f), 12f);
            // These models face +Z as imported; 180° turns them towards the gameplay camera.
            Kit.Decor(decor, "ExecutiveChair", "Executive chair", new Vector3(0f, BalconyTop, 12.4f), 4.6f, 180f);
            Kit.Decor(decor, "ReceptionDesk", "Reception desk", L(0f, 9.2f), 1.9f, 180f);
            Kit.Decor(decor, "ReceptionDesk", "West reception desk", L(-11.2f, 10.6f), 1.9f, 205f);
            Kit.Decor(decor, "BullStatue", "Golden bull", L(10.6f, 10f), 6.2f, 180f, 0.35f);
            Kit.Decor(decor, "Tree", "Tree A", L(-3.9f, 8.6f), 3.8f);
            Kit.Decor(decor, "Tree", "Tree B", L(3.9f, 8.6f), 3.8f, 90f);
            Kit.Decor(decor, "Tree", "Tree C", L(-16f, 8.6f), 4.4f, 180f);
            Kit.Decor(decor, "Tree", "Tree D", L(16f, 8.6f), 4.4f, 270f);
        }

        /// <summary>Point on the lobby floor.</summary>
        private static Vector3 L(float x, float z) => new Vector3(x, LobbyTop, z);
    }
}
