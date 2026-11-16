using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// SYS-071: one layout for HUD, camera and pointer tests. The 1600×900 design canvas is scaled to fit and then
    /// <b>extended</b> (not letterboxed): wider screens add side space, taller screens add height. Top elements stay
    /// at the top, fighter cards at the bottom, menus centred. The camera reserves exactly the strip the cards use.
    /// </summary>
    public readonly struct BrawlHudLayout
    {
        public const float Width = 1600f, Height = 900f;
        /// <summary>Bottom strip at 1× (cards 738–862 plus the control hint line).</summary>
        public const float BaseStrip = 171f;
        public const float MinScaleOption = 1f, MaxScaleOption = 1.3f;

        /// <summary>Screen pixels per canvas unit.</summary>
        public readonly float Scale;
        /// <summary>Visible canvas size (≥ 1600×900).</summary>
        public readonly float ViewWidth, ViewHeight;
        /// <summary>Side space added on each side of the design area / height added below it.</summary>
        public readonly float PadX, ExtraY;
        /// <summary>Requested HUD size and the size the bottom card row can actually use without overflowing.</summary>
        public readonly float HudScale, CardScale, HintScale;
        public readonly float StripHeight;
        public float StripFraction => StripHeight / ViewHeight;

        private BrawlHudLayout(float scale, float viewWidth, float viewHeight, float hudScale, float cardScale, float hintScale)
        {
            HintScale = hintScale;
            Scale = scale; ViewWidth = viewWidth; ViewHeight = viewHeight;
            PadX = (viewWidth - Width) * .5f; ExtraY = viewHeight - Height;
            HudScale = hudScale; CardScale = cardScale; StripHeight = BaseStrip * cardScale;
        }

        public static BrawlHudLayout Compute(float screenWidth, float screenHeight, float hudScale, int cards)
        {
            screenWidth = Mathf.Max(1, screenWidth); screenHeight = Mathf.Max(1, screenHeight);
            float scale = Mathf.Min(screenWidth / Width, screenHeight / Height);
            float viewWidth = screenWidth / scale, viewHeight = screenHeight / scale;
            hudScale = Mathf.Clamp(hudScale, MinScaleOption, MaxScaleOption);
            // Cards grow only as far as the row still fits; five players on 16:9 already fill the width.
            float cardScale = cards <= 0 ? hudScale : Mathf.Clamp(Mathf.Min(hudScale, (viewWidth - 40f) / RowWidth(cards)), MinScaleOption, MaxScaleOption);
            float hintScale = Mathf.Clamp(Mathf.Min(hudScale, (viewWidth - 40f) / HintWidth), MinScaleOption, MaxScaleOption);
            return new BrawlHudLayout(scale, viewWidth, viewHeight, hudScale, cardScale, hintScale);
        }

        public const float HintWidth = 1550f;
        public static float CardWidth(int cards) => cards <= 0 ? 0 : Mathf.Min(310, (Width - 70 - (cards - 1) * 12) / cards);
        public static float RowWidth(int cards) => cards <= 0 ? 0 : CardWidth(cards) * cards + 12 * (cards - 1);

        /// <summary>Screen point (pixels, origin top-left) to canvas units.</summary>
        public Vector2 ScreenToCanvas(Vector2 topLeftPixels) => topLeftPixels / Scale;

        public Matrix4x4 Canvas => Matrix4x4.Scale(new Vector3(Scale, Scale, 1));
        /// <summary>Design-area origin for top-anchored elements.</summary>
        public Matrix4x4 Top => Canvas * Matrix4x4.Translate(new Vector3(PadX, 0, 0));
        /// <summary>Design area centred in the screen (menus, pause, results, countdown).</summary>
        public Matrix4x4 Centre => Canvas * Matrix4x4.Translate(new Vector3(PadX, ExtraY * .5f, 0));
        /// <summary>Design-area element scaled by the HUD option around a pivot given in design units.</summary>
        public Matrix4x4 TopScaled(Vector2 pivot) => Top * About(pivot, HudScale);
        /// <summary>Bottom row: cards and hint, anchored to the bottom edge and scaled about its centre.</summary>
        public Matrix4x4 Bottom => Canvas * Matrix4x4.Translate(new Vector3(PadX, ExtraY, 0)) * About(new Vector2(Width * .5f, Height), CardScale);
        /// <summary>Control hint line along the bottom edge, scaled only while it still fits the width.</summary>
        public Matrix4x4 Hint => Canvas * Matrix4x4.Translate(new Vector3(PadX, ExtraY, 0)) * About(new Vector2(Width * .5f, Height), HintScale);

        public static Matrix4x4 About(Vector2 pivot, float scale) =>
            Matrix4x4.Translate(new Vector3(pivot.x, pivot.y, 0)) * Matrix4x4.Scale(new Vector3(scale, scale, 1)) * Matrix4x4.Translate(new Vector3(-pivot.x, -pivot.y, 0));

        /// <summary>Font size that respects the text option and never renders below <paramref name="minPixels"/>.</summary>
        public int FontSize(int baseSize, float textScale, float minPixels, float maxGrowth = 1.4f)
        {
            float wanted = Mathf.Max(baseSize * Mathf.Clamp(textScale, MinScaleOption, MaxScaleOption), minPixels / Mathf.Max(.01f, Scale));
            return Mathf.RoundToInt(Mathf.Min(wanted, baseSize * maxGrowth));
        }
    }
}
