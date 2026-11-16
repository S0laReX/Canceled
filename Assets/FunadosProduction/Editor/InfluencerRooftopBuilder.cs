using UnityEditor;
using UnityEngine;

namespace Funados.Production.Environments
{
    /// <summary>INFLUENCER ROOFTOP: central stage, two side platforms and a decorated back terrace.</summary>
    public static class InfluencerRooftopBuilder
    {
        private static readonly StageKit Kit = new StageKit("InfluencerRooftop", "IR");
        // One step below the main platform top, so fighters walk onto the solid terrace.
        private const float TerraceTop = -0.2f;

        [MenuItem("FUNADOS/Escenarios/Armar Influencer Rooftop")]
        public static void Build() => Kit.Build("Influencer Rooftop", Layout);

        [MenuItem("FUNADOS/Escenarios/Capturar vista previa de Influencer Rooftop")]
        public static void Capture() => Kit.Capture(new Color(0.36f, 0.3f, 0.6f));

        private static void Layout(Transform root)
        {
            // ── Combat surfaces (solid box colliders with exact flat tops) ──
            Transform combat = Kit.Group(root, "Combat surfaces");
            Kit.Platform(combat, "MainPlatform", "Main platform", new Vector3(0f, -1.6f, 0f), new Vector3(16f, 1.6f, 11f));
            Kit.Platform(combat, "SidePlatform", "West side platform", new Vector3(-11f, 0.6f, 1f), new Vector3(4.6f, 0.7f, 3.8f));
            Kit.Platform(combat, "SidePlatform", "East side platform", new Vector3(11f, 1.5f, 1f), new Vector3(4.6f, 0.7f, 3.8f));

            // ── Rooftop structure (visual only) ──
            Transform structure = Kit.Group(root, "Structure");
            Material deck = Kit.SolidMaterial("Deck", new Color(0.36f, 0.27f, 0.52f));
            Material facade = Kit.SolidMaterial("Facade", new Color(0.2f, 0.12f, 0.32f));
            Material trim = Kit.SolidMaterial("Trim", new Color(1f, 0.3f, 0.66f));
            Kit.Block(structure, "Stage tower", new Vector3(0f, -7.6f, 0f), new Vector3(15.4f, 12f, 10.4f), facade);
            Kit.BackFloor(structure, TerraceTop, 36f, 7.6f, 14f, deck, trim);

            // ── Decoration on the back terrace (no colliders, behind the invisible wall) ──
            // Props are deliberately oversized (cartoon proportions) so they read from the gameplay camera.
            Transform decor = Kit.Group(root, "Decoration");
            Kit.Decor(decor, "SmartphoneArch", "West smartphone arch", T(-11f, 10.8f), 7f, 0f, 1.1f);
            Kit.Decor(decor, "SmartphoneArch", "East smartphone arch", T(11f, 10.8f), 7f, 0f, 1.1f);
            Kit.Decor(decor, "RingLight", "West ring light", T(-3.4f, 9.4f), 5.2f, 0f, 1.3f);
            Kit.Decor(decor, "RingLight", "East ring light", T(3.4f, 9.4f), 5.2f, 0f, 1.3f);
            Kit.Decor(decor, "RingLight", "Giant ring light", T(-16f, 10.2f), 6.5f, 20f, 1.3f);
            Kit.Decor(decor, "Sofa", "West sofa", T(-6.8f, 11.4f), 1.9f);
            Kit.Decor(decor, "Sofa", "East sofa", T(6.8f, 11.4f), 1.9f);
            Kit.Decor(decor, "Plant", "Plant A", T(-8.4f, 8.2f), 3.2f);
            Kit.Decor(decor, "Plant", "Plant B", T(8.4f, 8.2f), 3.2f, 70f);
            Kit.Decor(decor, "Plant", "Plant C", T(0f, 12.2f), 3.8f, 140f);
            Kit.Decor(decor, "Plant", "Plant D", T(16.4f, 11.8f), 3.2f, 210f);
            Kit.Decor(decor, "CameraTripod", "West camera", T(-14f, 8.2f), 3f, 25f);
            Kit.Decor(decor, "CameraTripod", "East camera", T(14.6f, 8.4f), 3f, -25f);
        }

        /// <summary>Point on the back terrace floor.</summary>
        private static Vector3 T(float x, float z) => new Vector3(x, TerraceTop, z);
    }
}
