using UnityEditor;
using UnityEngine;

namespace Funados.Production.Environments
{
    /// <summary>
    /// TV DEBATE STUDIO: blue/red central stage, two side platforms and a studio floor behind with the
    /// curved LED wall, the two debate podiums and broadcast cameras.
    /// </summary>
    public static class TvDebateStudioBuilder
    {
        private static readonly StageKit Kit = new StageKit("TvDebateStudio", "TV");
        // One step below the main platform top, so fighters walk onto the solid studio floor.
        private const float FloorTop = -0.2f;

        [MenuItem("FUNADOS/Escenarios/Armar TV Debate Studio")]
        public static void Build() => Kit.Build("TV Debate Studio", Layout);

        [MenuItem("FUNADOS/Escenarios/Capturar vista previa de TV Debate Studio")]
        public static void Capture() => Kit.Capture(new Color(0.06f, 0.07f, 0.14f));

        private static void Layout(Transform root)
        {
            // ── Combat surfaces (solid box colliders with exact flat tops) ──
            Transform combat = Kit.Group(root, "Combat surfaces");
            Kit.Platform(combat, "MainPlatform", "Main platform", new Vector3(0f, -1.4f, 0f), new Vector3(16f, 1.4f, 11f));
            // Symmetric side platforms, as the brief asks: same height on both sides.
            Kit.Platform(combat, "SidePlatform", "West side platform", new Vector3(-11f, 0.7f, 1f), new Vector3(4.6f, 1.1f, 3.8f));
            Kit.Platform(combat, "SidePlatform", "East side platform", new Vector3(11f, 0.7f, 1f), new Vector3(4.6f, 1.1f, 3.8f), 180f);

            // ── Studio structure (visual only) ──
            Transform structure = Kit.Group(root, "Structure");
            Material floor = Kit.SolidMaterial("Floor", new Color(0.16f, 0.17f, 0.27f));
            Material dark = Kit.SolidMaterial("Dark", new Color(0.07f, 0.08f, 0.14f));
            Material blue = Kit.SolidMaterial("Blue", new Color(0.1f, 0.55f, 1f), 1.2f);
            Material red = Kit.SolidMaterial("Red", new Color(1f, 0.12f, 0.2f), 1.2f);
            Kit.Block(structure, "Stage tower", new Vector3(0f, -7.4f, 0f), new Vector3(15.4f, 12f, 10.4f), dark);
            Kit.BackFloor(structure, FloorTop, 36f, 7.2f, 14.5f, floor, blue);
            Kit.Block(structure, "Floor trim red", new Vector3(9.03f, FloorTop - 0.15f, 5.24f), new Vector3(18.05f, 0.3f, 0.13f), red);
            Kit.Block(structure, "Back wall", new Vector3(0f, FloorTop + 7f, 14.6f), new Vector3(36f, 14f, 0.3f), dark);
            Kit.Block(structure, "Overhead bar blue", new Vector3(-9f, FloorTop + 12.4f, 14.3f), new Vector3(17f, 0.35f, 0.25f), blue);
            Kit.Block(structure, "Overhead bar red", new Vector3(9f, FloorTop + 12.4f, 14.3f), new Vector3(17f, 0.35f, 0.25f), red);

            // ── Decoration on the studio floor (no colliders), oversized to read from the gameplay camera ──
            Transform decor = Kit.Group(root, "Decoration");
            // The wall faces +Z as imported; 180° shows its screens to the gameplay camera.
            // The curved wall is several metres deep: keep it well in front of the back wall or its
            // recessed centre screen ends up hidden behind it.
            Kit.Decor(decor, "LedWall", "LED wall", F(0f, 10.8f), 8f, 180f, 0.7f);
            Kit.Decor(decor, "PodiumBlue", "Blue podium", F(-3.4f, 8.1f), 3.2f, 0f, 0.5f);
            Kit.Decor(decor, "PodiumRed", "Red podium", F(3.4f, 8.1f), 3.2f, 0f, 0.5f);
            Kit.Decor(decor, "CameraCrane", "West camera crane", F(-13.6f, 10.4f), 6.4f);
            Kit.Decor(decor, "CameraCrane", "East camera crane", F(13.6f, 10.4f), 6.4f, 180f);
            Kit.Decor(decor, "StudioCamera", "West studio camera", F(-10.6f, 7.9f), 3.4f, 30f);
            Kit.Decor(decor, "StudioCamera", "East studio camera", F(10.6f, 7.9f), 3.4f, -30f);
        }

        /// <summary>Point on the studio floor.</summary>
        private static Vector3 F(float x, float z) => new Vector3(x, FloorTop, z);
    }
}
