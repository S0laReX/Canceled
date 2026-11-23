using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>Colour vision simulated by the accessibility checks (Machado, Oliveira y Fernandes 2009, severidad 1).</summary>
    public enum ColorVision { Normal, Protanopia, Deuteranopia, Tritanopia }

    /// <summary>
    /// SYS-071: every colour the HUD draws, for the four combinations of high contrast × colour-blind palette, and the
    /// maths the checks use to prove they stay distinguishable (CIELAB ΔE76 and WCAG contrast ratio, both after
    /// simulating protanopia, deuteranopia and tritanopia in linear RGB).
    /// </summary>
    public readonly struct BrawlHudPalette
    {
        public readonly Color Ink, Card, Paper, Muted, Accent, Warning, Track, BadgeFill, DangerFill, Strip;
        public readonly bool HighContrast, ColorBlind;

        private BrawlHudPalette(bool highContrast, bool colorBlind)
        {
            HighContrast = highContrast; ColorBlind = colorBlind;
            Ink = highContrast ? new Color(0, 0, 0, 1) : new Color(.035f, .047f, .085f, .96f);
            Card = highContrast ? new Color(.11f, .11f, .13f) : new Color(.075f, .09f, .14f);
            Paper = highContrast ? Color.white : new Color(.94f, .96f, 1f);
            Muted = highContrast ? new Color(.86f, .89f, .95f) : new Color(.59f, .66f, .77f);
            Accent = highContrast ? new Color(.35f, 1f, 1f) : new Color(.22f, .93f, .93f);
            // Pink reads poorly next to grey text under red-green deficiency; the colour-blind warning is orange.
            Warning = colorBlind ? (highContrast ? new Color(1f, .7f, .2f) : new Color(1f, .55f, .1f)) : highContrast ? new Color(1f, .6f, .85f) : new Color(1f, .33f, .65f);
            Track = highContrast ? new Color(.32f, .34f, .40f) : new Color(.18f, .22f, .30f);
            BadgeFill = highContrast ? new Color(0, 0, 0, 1) : new Color(.025f, .035f, .065f, .94f);
            DangerFill = colorBlind ? new Color(.42f, .2f, 0f, .97f) : new Color(.45f, .05f, .2f, .96f);
            Strip = highContrast ? Color.black : new Color(.02f, .03f, .06f, 1f);
        }

        public static BrawlHudPalette For(bool highContrast, bool colorBlind) => new BrawlHudPalette(highContrast, colorBlind);

        /// <summary>Player colours P1–P5 (the historical neon set).</summary>
        public static readonly Color[] PlayerColors =
            { new Color(.18f, .95f, 1), new Color(1, .3f, .6f), new Color(1, .77f, .2f), new Color(.5f, 1, .42f), new Color(.66f, .5f, 1) };
        /// <summary>
        /// Colour-blind set: teal, coral, mint white, violet, amber. Chosen by search so that every pair stays ≥ 40 ΔE
        /// with normal vision and under the three simulated deficiencies, and each one keeps ≥ 5:1 on the HUD ink.
        /// </summary>
        public static readonly Color[] ColorBlindPlayerColors =
            { new Color(0f, .6f, .65f), new Color(.9f, .45f, .3f), new Color(.85f, 1f, .9f), new Color(.6f, .4f, .95f), new Color(1f, .8f, 0f) };

        public static Color Player(int index, bool colorBlind)
        {
            Color[] set = colorBlind ? ColorBlindPlayerColors : PlayerColors;
            return set[((index % set.Length) + set.Length) % set.Length];
        }

        // ─────────────── Simulation and metrics ───────────────
        private static readonly float[][] Protan = { new[] { .152286f, 1.052583f, -.204868f }, new[] { .114503f, .786281f, .099216f }, new[] { -.003882f, -.048116f, 1.051998f } };
        private static readonly float[][] Deutan = { new[] { .367322f, .860646f, -.227968f }, new[] { .280085f, .672501f, .047413f }, new[] { -.011820f, .042940f, .968881f } };
        private static readonly float[][] Tritan = { new[] { 1.255528f, -.076749f, -.178779f }, new[] { -.078411f, .930809f, .147602f }, new[] { .004733f, .691367f, .303900f } };

        private static float ToLinear(float c) => c <= .04045f ? c / 12.92f : Mathf.Pow((c + .055f) / 1.055f, 2.4f);
        private static float ToGamma(float c) => c <= .0031308f ? c * 12.92f : 1.055f * Mathf.Pow(c, 1 / 2.4f) - .055f;

        /// <summary>Linear-RGB colour as seen with <paramref name="vision"/>; alpha is composited over <paramref name="over"/> first.</summary>
        public static Vector3 SimulateLinear(Color color, ColorVision vision, Color? over = null)
        {
            if (over.HasValue && color.a < 1) color = Color.Lerp(over.Value, color, color.a);
            var l = new Vector3(ToLinear(color.r), ToLinear(color.g), ToLinear(color.b));
            float[][] m = vision == ColorVision.Protanopia ? Protan : vision == ColorVision.Deuteranopia ? Deutan : vision == ColorVision.Tritanopia ? Tritan : null;
            if (m == null) return l;
            return new Vector3(
                Mathf.Clamp01(m[0][0] * l.x + m[0][1] * l.y + m[0][2] * l.z),
                Mathf.Clamp01(m[1][0] * l.x + m[1][1] * l.y + m[1][2] * l.z),
                Mathf.Clamp01(m[2][0] * l.x + m[2][1] * l.y + m[2][2] * l.z));
        }

        /// <summary>sRGB colour as seen with <paramref name="vision"/> (for the comparison strip in the evidence).</summary>
        public static Color Simulate(Color color, ColorVision vision)
        {
            Vector3 l = SimulateLinear(color, vision);
            return new Color(ToGamma(l.x), ToGamma(l.y), ToGamma(l.z), 1);
        }

        private static Vector3 Lab(Vector3 l)
        {
            float x = (.4124f * l.x + .3576f * l.y + .1805f * l.z) / .95047f;
            float y = .2126f * l.x + .7152f * l.y + .0722f * l.z;
            float z = (.0193f * l.x + .1192f * l.y + .9505f * l.z) / 1.08883f;
            float F(float t) => t > .008856f ? Mathf.Pow(t, 1f / 3) : 7.787f * t + 16f / 116;
            float fx = F(x), fy = F(y), fz = F(z);
            return new Vector3(116 * fy - 16, 500 * (fx - fy), 200 * (fy - fz));
        }

        private static float Luminance(Vector3 l) => .2126f * l.x + .7152f * l.y + .0722f * l.z;

        /// <summary>CIELAB ΔE76 between two colours as seen with <paramref name="vision"/>.</summary>
        public static float DeltaE(Color a, Color b, ColorVision vision, Color? over = null) =>
            Vector3.Distance(Lab(SimulateLinear(a, vision, over)), Lab(SimulateLinear(b, vision, over)));

        /// <summary>WCAG 2 contrast ratio (1–21) between two colours as seen with <paramref name="vision"/>.</summary>
        public static float Contrast(Color a, Color b, ColorVision vision, Color? over = null)
        {
            float la = Luminance(SimulateLinear(a, vision, over)) + .05f, lb = Luminance(SimulateLinear(b, vision, over)) + .05f;
            return Mathf.Max(la, lb) / Mathf.Min(la, lb);
        }

        /// <summary>Smallest ΔE between any two colours of <paramref name="set"/> under <paramref name="vision"/>.</summary>
        public static float MinPairDeltaE(Color[] set, ColorVision vision, out int first, out int second)
        {
            float best = float.MaxValue; first = second = -1;
            for (int i = 0; i < set.Length; i++)
                for (int j = i + 1; j < set.Length; j++)
                {
                    float d = DeltaE(set[i], set[j], vision);
                    if (d < best) { best = d; first = i; second = j; }
                }
            return best;
        }
    }
}
