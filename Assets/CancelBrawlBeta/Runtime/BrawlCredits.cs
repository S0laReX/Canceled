using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// Credits screen (GDD 2.0 §10, D04), opened from «CRÉDITOS» in the main menu. The text follows
    /// <c>Docs/Licencias/CREDITOS.md</c>; the Meshy attribution line is kept verbatim (CC BY 4.0, accepted for D04).
    /// Back: the VOLVER button, Esc, B or Start. The HUD draws the button and registers it for menu navigation.
    /// </summary>
    public static class BrawlCredits
    {
        /// <summary>Required attribution for the Meshy assets, verbatim.</summary>
        public const string MeshyAttribution = "Model created with Meshy – CC BY 4.0 License";

        public static bool Open { get; private set; }
        public static int OpenCount { get; private set; }
        public static void Show() { Open = true; OpenCount++; }
        public static void Close() => Open = false;

        /// <summary>Headings start with '#'; the rest are entries.</summary>
        public static readonly string[] Lines =
        {
            "#ARTE",
            "Modelos 3D de personajes y escenarios, y arte conceptual de escenarios: creados con Meshy (meshy.ai).",
            MeshyAttribution,
            "Imágenes conceptuales generadas en Meshy con el modelo de imagen nano-banana-pro.",
            "Rig y animación de los personajes: generados por código en el proyecto (BrawlAutoRig).",
            "Shader FUNADOS Toon y efectos visuales: propios del proyecto.",
            "#AUDIO",
            "Música, voces de personajes y efectos de sonido: sintetizados por código en el proyecto.",
            "Sin grabaciones, sin clonación de voz y sin imitar a personas reales.",
            "#MOTOR Y PAQUETES",
            "Hecho con Unity 6000.5.7f1 y los paquetes oficiales de Unity (Unity Companion License, Unity Package Distribution License y MIT), con sus avisos de terceros.",
            "Blender 4.5 LTS (Blender Foundation) para el cubo de validación de escala.",
        };

        /// <summary>Back button rect in the HUD's centred canvas (1600 × 900).</summary>
        public static readonly Rect BackRect = new Rect(600, 806, 400, 54);
        public static readonly Rect PanelRect = new Rect(200, 60, 1200, 800);
        /// <summary>Bottom of the last drawn line (canvas units); must stay above the back button.</summary>
        public static float LastTextBottom { get; private set; }
        public static int LastFontSize { get; private set; }

        private static GUIStyle title, heading, body;
        private static readonly GUIContent content = new GUIContent();

        public static void Draw(BrawlHudLayout layout, BrawlHudPalette palette, float textScale, string version)
        {
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter };
                heading = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
                body = new GUIStyle(GUI.skin.label) { wordWrap = true };
            }
            title.fontSize = layout.FontSize(40, textScale, 22, 1.2f);
            heading.fontSize = layout.FontSize(20, textScale, 15);
            body.fontSize = layout.FontSize(18, textScale, 15, 1.25f);
            LastFontSize = body.fontSize;
            Color old = GUI.color;
            GUI.color = palette.HighContrast ? Color.black : palette.Ink;
            GUI.DrawTexture(PanelRect, Texture2D.whiteTexture);
            GUI.color = palette.Accent; GUI.DrawTexture(new Rect(PanelRect.x, PanelRect.y, PanelRect.width, 4), Texture2D.whiteTexture);
            GUI.color = Color.white;
            title.normal.textColor = palette.Paper; heading.normal.textColor = palette.Accent; body.normal.textColor = palette.Paper;
            GUI.Label(new Rect(PanelRect.x, PanelRect.y + 26, PanelRect.width, 56), "CRÉDITOS  ·  FUNADOS " + version, title);
            float x = PanelRect.x + 70, width = PanelRect.width - 140, y = PanelRect.y + 104;
            foreach (string line in Lines)
            {
                if (line.StartsWith("#"))
                {
                    y += 10;
                    content.text = line.Substring(1);
                    float h = heading.CalcHeight(content, width);
                    GUI.Label(new Rect(x, y, width, h), content, heading); y += h + 4;
                    continue;
                }
                content.text = "·  " + line;
                float height = body.CalcHeight(content, width);
                GUI.Label(new Rect(x, y, width, height), content, body); y += height + 6;
            }
            LastTextBottom = y;
            GUI.color = old;
        }
    }
}
