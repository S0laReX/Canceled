using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// Subtitles for character lines (GDD 2.0 §10): «SPEAKER: line», one at a time, centred above the bottom HUD strip.
    /// They follow the text size and high-contrast options, show the speaker's name in their player colour (colour-blind
    /// palette included) and switch off with the PANTALLA option. Shown whenever a line is accepted on the voice bus,
    /// even with voices muted, because that is when they matter most.
    /// </summary>
    public static class BrawlSubtitles
    {
        public const int BaseFontSize = 22;
        public const float MinPixels = 16, MinSeconds = 1.8f, SecondsPerCharacter = .045f, Tail = .6f;
        /// <summary>Background opacity in normal mode (high contrast is opaque).</summary>
        public const float BackgroundAlpha = .78f;

        public static bool Enabled { get; set; } = true;
        public static string Speaker { get; private set; } = "";
        public static string Text { get; private set; } = "";
        public static int Player { get; private set; } = -1;
        public static int ShownCount { get; private set; }
        private static float until;

        /// <summary>Line on screen now, or empty.</summary>
        public static bool Visible => Enabled && Text.Length > 0 && Time.unscaledTime < until;
        public static string Current => Visible ? Speaker + ": " + Text : "";
        /// <summary>Last drawn box (canvas units), font size in real pixels and the colours used, for the checks.</summary>
        public static Rect LastRect { get; private set; }
        public static float LastFontPixels { get; private set; }
        public static Color LastBackground { get; private set; }
        public static Color LastText { get; private set; }
        public static Color LastSpeakerColor { get; private set; }

        public static float DurationFor(string text, float audioSeconds) =>
            Mathf.Max(MinSeconds + SecondsPerCharacter * (text?.Length ?? 0), audioSeconds + Tail);

        public static void Show(string speaker, string text, int player, float audioSeconds)
        {
            if (!Enabled || string.IsNullOrEmpty(text)) return;
            Speaker = speaker ?? ""; Text = text; Player = player;
            until = Time.unscaledTime + DurationFor(text, audioSeconds);
            ShownCount++;
        }

        public static void Clear() { Text = ""; Speaker = ""; Player = -1; until = 0; }

        public const float SpeakerContrast = 4.5f;
        private static readonly ColorVision[] Visions = { ColorVision.Normal, ColorVision.Protanopia, ColorVision.Deuteranopia, ColorVision.Tritanopia };

        /// <summary>Background of the subtitle box (opaque with high contrast).</summary>
        public static Color Background(bool highContrast) => highContrast ? new Color(0, 0, 0, 1) : new Color(0, 0, 0, BackgroundAlpha);

        /// <summary>
        /// The speaker's player colour, lightened just enough to read on the box (≥ 4.5:1 over any scene, in the four
        /// simulated visions); with high contrast, a little lighter still.
        /// </summary>
        public static Color SpeakerColor(int player, bool colorBlind, bool highContrast, Color fallback)
        {
            Color color = player >= 0 ? BrawlHudPalette.Player(player, colorBlind) : fallback;
            Color background = Background(highContrast);
            float target = highContrast ? 7f : SpeakerContrast;
            Color result = color;
            for (int step = 0; step <= 20; step++)
            {
                result = Color.Lerp(color, Color.white, step * .05f);
                float worst = float.MaxValue;
                foreach (var vision in Visions) worst = Mathf.Min(worst, BrawlHudPalette.Contrast(result, background, vision, Color.white));
                if (worst >= target) break;
            }
            return result;
        }

        private static GUIStyle style;
        private static bool warmed;
        private static readonly GUIContent content = new GUIContent();

        /// <summary>Draws the current line; call with the canvas matrix of the HUD.</summary>
        public static void Draw(BrawlHudLayout layout, BrawlHudPalette palette, float textScale, bool colorBlind)
        {
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true, richText = true, fontStyle = FontStyle.Bold };
            }
            style.fontSize = layout.FontSize(BaseFontSize, textScale, MinPixels);
            if (!warmed) { Warm(); warmed = true; }
            if (!Visible) return;
            Color speakerColor = SpeakerColor(Player, colorBlind, palette.HighContrast, palette.Accent);
            content.text = "<color=#" + ColorUtility.ToHtmlStringRGB(speakerColor) + ">" + Speaker.ToUpperInvariant() + ":</color>  " + Text;
            float maxWidth = Mathf.Min(980, layout.ViewWidth - 120);
            // Measured with slack: bold rich text renders a little wider than CalcSize reports, which made a short line
            // wrap inside a box sized for one line and clip.
            style.wordWrap = false;
            float width = Mathf.Min(maxWidth, style.CalcSize(content).x * 1.08f + 64);
            style.wordWrap = true;
            float height = style.CalcHeight(content, width - 64) * 1.1f + 20;
            var rect = new Rect((layout.ViewWidth - width) * .5f, layout.ViewHeight - layout.StripHeight - height - 22, width, height);
            Color background = Background(palette.HighContrast);
            Color old = GUI.color;
            GUI.color = background; GUI.DrawTexture(rect, Texture2D.whiteTexture);
            if (palette.HighContrast)
            {
                GUI.color = palette.Paper;
                GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(rect.x, rect.yMax - 2, rect.width, 2), Texture2D.whiteTexture);
            }
            GUI.color = Color.white; style.normal.textColor = palette.Paper;
            GUI.Label(new Rect(rect.x + 32, rect.y + 6, rect.width - 64, rect.height - 12), content, style);
            GUI.color = old;
            LastRect = rect; LastFontPixels = style.fontSize * layout.Scale; LastBackground = background; LastText = palette.Paper; LastSpeakerColor = speakerColor;
        }

        /// <summary>Puts every subtitle character into the font at the sizes the option range can use, before the first line.</summary>
        private static void Warm()
        {
            Font font = style.font ? style.font : GUI.skin.font;
            if (!font || !font.dynamic) return;
            var characters = new System.Text.StringBuilder(": ");
            for (int c = 0; c < BrawlVoices.CharacterCount; c++)
            {
                characters.Append(BrawlAudio.SpeakerName(c).ToUpperInvariant());
                for (int line = 0; line < BrawlVoices.LineCount; line++) characters.Append(BrawlVoices.Text((VoiceLine)line, c));
            }
            string all = characters.ToString();
            for (int size = Mathf.RoundToInt(MinPixels) - 2; size <= Mathf.CeilToInt(BaseFontSize * BrawlHudLayout.MaxScaleOption * 1.5f); size++)
                font.RequestCharactersInTexture(all, size, FontStyle.Bold);
        }
    }
}
