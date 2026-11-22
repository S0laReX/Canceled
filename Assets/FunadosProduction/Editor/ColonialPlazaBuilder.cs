using UnityEditor;
using UnityEngine;

namespace Funados.Production.Environments
{
    /// <summary>
    /// COLONIAL PLAZA SHOWDOWN: tiled stone platform, two wooden balconies at different heights and a
    /// paved plaza behind with the Baroque church, arcade buildings, the aristocrat fountain and banners.
    /// </summary>
    public static class ColonialPlazaBuilder
    {
        private static readonly StageKit Kit = new StageKit("ColonialPlaza", "CP");
        // One step below the main platform top, so fighters walk onto the solid plaza paving.
        private const float PlazaTop = -0.2f;

        [MenuItem("FUNADOS/Escenarios/Armar Colonial Plaza")]
        public static void Build() => Kit.Build("Colonial Plaza Showdown", Layout);

        [MenuItem("FUNADOS/Escenarios/Capturar vista previa de Colonial Plaza")]
        public static void Capture() => Kit.Capture(new Color(0.45f, 0.72f, 0.95f));

        private static void Layout(Transform root)
        {
            // ── Combat surfaces (solid box colliders with exact flat tops) ──
            Transform combat = Kit.Group(root, "Combat surfaces");
            Kit.Platform(combat, "MainPlatform", "Main platform", new Vector3(0f, -1.4f, 0f), new Vector3(16f, 1.4f, 11f));
            // Balcony tops sit at 1.3 m and 2.2 m. Each collider is the full 2.2 m bounding box, so the
            // drapery and brackets hanging below the deck are solid too.
            // Yaw 180° on both: the drapery side of the model faces +Z as imported.
            Kit.Platform(combat, "Balcony", "West balcony", new Vector3(-11f, -0.9f, 1f), new Vector3(4.6f, 2.2f, 3.8f), 180f);
            Kit.Platform(combat, "Balcony", "East balcony", new Vector3(11f, 0f, 1f), new Vector3(4.6f, 2.2f, 3.8f), 180f);

            // ── Plaza structure (visual only) ──
            Transform structure = Kit.Group(root, "Structure");
            Material stone = Kit.SolidMaterial("Paving", new Color(0.86f, 0.74f, 0.56f));
            Material wall = Kit.SolidMaterial("Foundation", new Color(0.7f, 0.56f, 0.42f));
            Material trim = Kit.SolidMaterial("Trim", new Color(0.2f, 0.72f, 0.7f));
            Kit.Block(structure, "Platform foundation", new Vector3(0f, -7.4f, 0f), new Vector3(15.4f, 12f, 10.4f), wall);
            Kit.BackFloor(structure, PlazaTop, 40f, 7.4f, 18f, stone, trim);

            // ── Decoration on the plaza (no colliders), oversized to read from the gameplay camera ──
            Transform decor = Kit.Group(root, "Decoration");
            // Buildings face +Z as imported; 180° shows their facades to the gameplay camera. The church is
            // several metres deep, so it sits at the back of the plaza to leave the fountain in front.
            Kit.Decor(decor, "Church", "Baroque church", P(0f, 15.6f), 12.5f, 180f);
            Kit.Decor(decor, "ArcadeBuilding", "West arcade building", P(-13.5f, 14.4f), 9.5f, 180f);
            Kit.Decor(decor, "ArcadeBuilding", "East arcade building", P(13.5f, 14.4f), 9.5f, 180f);
            Kit.Decor(decor, "Fountain", "Aristocrat fountain", P(0f, 10f), 4.6f, 180f);
            Kit.Decor(decor, "Banner", "West banner", P(-6.2f, 9.6f), 5.6f);
            Kit.Decor(decor, "Banner", "East banner", P(6.2f, 9.6f), 5.6f, 180f);
            Kit.Decor(decor, "FlowerPot", "Flower pot A", P(-9.4f, 8.2f), 2.2f);
            Kit.Decor(decor, "FlowerPot", "Flower pot B", P(9.4f, 8.2f), 2.2f, 90f);
            Kit.Decor(decor, "FlowerPot", "Flower pot C", P(-17.6f, 8.6f), 2.6f, 180f);
            Kit.Decor(decor, "FlowerPot", "Flower pot D", P(17.6f, 8.6f), 2.6f, 270f);
        }

        /// <summary>Point on the plaza floor.</summary>
        private static Vector3 P(float x, float z) => new Vector3(x, PlazaTop, z);
    }
}
