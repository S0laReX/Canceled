using System.Collections.Generic;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>Shape that identifies each player besides the colour (P1–P5), on cards, lives and edge markers.</summary>
    public enum PlayerShape { Circle, Square, Triangle, Diamond, Star }

    /// <summary>
    /// GDD 2.0 §10 combat HUD: per fighter a portrait, short name, slot P1–P5 with its shape, a large percentage whose
    /// colour heats up as it rises (and pops when it grows), lives as icons, the «escándalo» bar and the disconnect
    /// notice; the combo counter sits next to the attacker and off-camera fighters get an edge marker with their
    /// number and shape. Still IMGUI (D07 is open); every element reads at 720p.
    /// </summary>
    public sealed partial class BrawlHud
    {
        private BrawlPortraits portraits;
        /// <summary>Portrait cache shared by the selection screen and the combat cards.</summary>
        public BrawlPortraits Portraits
        {
            get
            {
                if (portraits == null)
                {
                    portraits = GetComponent<BrawlPortraits>();
                    if (portraits == null) portraits = gameObject.AddComponent<BrawlPortraits>();
                    portraits.Configure(match != null && match.Settings != null ? match.Settings.characterArt : null);
                }
                return portraits;
            }
        }

        private Texture2D[] shapeTextures;
        public static PlayerShape ShapeOf(int playerIndex) => (PlayerShape)(((playerIndex % 5) + 5) % 5);

        /// <summary>Damage pop: grows to <see cref="PopScale"/> when damage rises and settles in <see cref="PopSeconds"/>; off with reduced effects.</summary>
        public const float PopScale = 1.22f, PopSeconds = .28f;
        private readonly Dictionary<BrawlFighter, float> shownDamage = new Dictionary<BrawlFighter, float>();
        private readonly Dictionary<BrawlFighter, float> popStarted = new Dictionary<BrawlFighter, float>();

        // Per-frame counters for the checks (taken on Repaint).
        private int portraitsThisFrame, lifeIconsThisFrame, comboLabelsThisFrame, shapeMarkersThisFrame;
        public int PortraitsDrawn { get; private set; }
        public int LifeIconsDrawn { get; private set; }
        public int ComboLabelsDrawn { get; private set; }
        public int ShapeMarkersDrawn { get; private set; }

        private static bool Repainting => Event.current != null && Event.current.type == EventType.Repaint;

        private void CommitCombatCounters()
        {
            if (Event.current.type != EventType.Repaint) return;
            PortraitsDrawn = portraitsThisFrame; LifeIconsDrawn = lifeIconsThisFrame;
            ComboLabelsDrawn = comboLabelsThisFrame; ShapeMarkersDrawn = shapeMarkersThisFrame;
            portraitsThisFrame = lifeIconsThisFrame = comboLabelsThisFrame = shapeMarkersThisFrame = 0;
        }

        /// <summary>
        /// Percentage colour: paper at 0 %, yellow at 60 %, orange at 100 %, hot red from 150 %. Every step keeps
        /// ≥ 4,5:1 on the card, ≥ 7:1 with high contrast (checked by <c>-hud-smoke</c>); the colour-blind palette ends in its warning orange.
        /// </summary>
        public static Color HeatColor(float damage, BrawlHudPalette palette)
        {
            Color warm = palette.HighContrast ? new Color(1f, .97f, .5f) : new Color(1f, .9f, .3f);
            Color hot = palette.HighContrast ? new Color(1f, .8f, .45f) : new Color(1f, .6f, .2f);
            Color red = palette.ColorBlind ? palette.Warning : palette.HighContrast ? new Color(1f, .66f, .68f) : new Color(1f, .44f, .42f);
            if (damage <= 60) return Color.Lerp(palette.Paper, warm, damage / 60f);
            if (damage <= 100) return Color.Lerp(warm, hot, (damage - 60) / 40f);
            return Color.Lerp(hot, red, Mathf.Clamp01((damage - 100) / 50f));
        }

        /// <summary>Current scale of a fighter's percentage (1 when idle).</summary>
        public float DamagePop(BrawlFighter fighter)
        {
            if (fighter == null) return 1;
            float damage = fighter.Damage;
            if (!shownDamage.TryGetValue(fighter, out float last)) { shownDamage[fighter] = damage; return 1; }
            if (damage > last + .01f && !BrawlFeedback.ReducedEffects) popStarted[fighter] = Time.unscaledTime;
            shownDamage[fighter] = damage;
            if (!popStarted.TryGetValue(fighter, out float start)) return 1;
            float t = (Time.unscaledTime - start) / PopSeconds;
            if (t >= 1) return 1;
            return 1 + (PopScale - 1) * (1 - t) * (1 - t);
        }

        private Texture2D ShapeTexture(PlayerShape shape)
        {
            if (shapeTextures == null) shapeTextures = new Texture2D[5];
            int i = (int)shape;
            if (shapeTextures[i] != null) return shapeTextures[i];
            const int n = 48;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "Player shape " + shape, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    // 4×4 supersampling for clean edges.
                    int inside = 0;
                    for (int sy = 0; sy < 4; sy++)
                        for (int sx = 0; sx < 4; sx++)
                        {
                            float u = (x + (sx + .5f) / 4) / n * 2 - 1, v = (y + (sy + .5f) / 4) / n * 2 - 1;
                            if (Inside(shape, u, v)) inside++;
                        }
                    pixels[y * n + x] = new Color(1, 1, 1, inside / 16f);
                }
            texture.SetPixels(pixels); texture.Apply();
            shapeTextures[i] = texture;
            return texture;
        }

        /// <summary>Point (u, v) in [−1, 1]², v up, inside the shape.</summary>
        private static bool Inside(PlayerShape shape, float u, float v)
        {
            switch (shape)
            {
                case PlayerShape.Circle: return u * u + v * v <= .86f;
                case PlayerShape.Square: return Mathf.Abs(u) <= .78f && Mathf.Abs(v) <= .78f;
                case PlayerShape.Triangle: return v >= -.8f && v <= .9f && Mathf.Abs(u) <= (.9f - v) * .55f;
                case PlayerShape.Diamond: return Mathf.Abs(u) + Mathf.Abs(v) <= .98f;
                default:
                {
                    float angle = Mathf.Atan2(u, v), r = Mathf.Sqrt(u * u + v * v);
                    float sector = Mathf.Repeat(angle, Mathf.PI * 2 / 5) / (Mathf.PI * 2 / 5);
                    float edge = Mathf.Lerp(.95f, .42f, Mathf.Abs(sector - .5f) * -2 + 1);
                    return r <= edge;
                }
            }
        }

        private void Shape(Rect rect, int playerIndex, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, ShapeTexture(ShapeOf(playerIndex)), ScaleMode.ScaleToFit, true);
            GUI.color = old;
        }

        private void Portrait(Rect rect, int character, int skin, Color frame)
        {
            Fill(rect, BrawlPortraits.Background);
            Texture texture = Portraits.Get(character, skin);
            if (texture != null) { GUI.DrawTexture(rect, texture, ScaleMode.ScaleAndCrop, false); if (Repainting) portraitsThisFrame++; }
            Border(rect, palette.HighContrast ? 2 : 1, frame);
        }

        private void DrawFighter(Rect rect, BrawlFighter fighter)
        {
            Color accent = fighter.Eliminated ? Muted : PlayerTint(fighter);
            Panel(rect);
            Fill(new Rect(rect.x, rect.y, rect.width, 4), accent);
            // Header: shape + slot + short name.
            Shape(new Rect(rect.x + 12, rect.y + 12, 18, 18), fighter.PlayerIndex, accent);
            Label(new Rect(rect.x + 36, rect.y + 10, rect.width - 48, 24),
                $"P{fighter.PlayerIndex + 1}  {match.CharacterName(fighter.CharacterIndex).ToUpperInvariant()}", small, accent);
            // Portrait.
            Rect face = new Rect(rect.x + 12, rect.y + 36, 54, 54);
            Portrait(face, fighter.CharacterIndex, fighter.SkinIndex, accent);
            if (fighter.Eliminated) Fill(face, new Color(0, 0, 0, .55f));
            // Large percentage that heats up and pops.
            float pop = fighter.Eliminated ? 1 : DamagePop(fighter);
            Rect numberRect = new Rect(rect.x + 72, rect.y + 34, 132, 58);
            Matrix4x4 before = GUI.matrix;
            if (pop > 1.001f) GUI.matrix = before * BrawlHudLayout.About(new Vector2(numberRect.x + 8, numberRect.center.y), pop);
            Label(numberRect, fighter.Eliminated ? "FUERA" : $"{Mathf.RoundToInt(fighter.Damage)}%",
                fighter.Eliminated ? heading : number, fighter.Eliminated ? Muted : HeatColor(fighter.Damage, palette));
            GUI.matrix = before;
            // Lives as shape icons, then the state line.
            float column = rect.x + 208, columnWidth = rect.xMax - 12 - column;
            int stocks = Mathf.Max(0, fighter.Stocks);
            int icons = Mathf.Min(stocks, 5);
            float iconSize = Mathf.Min(15, (columnWidth - 2) / Mathf.Max(1, icons) - 3);
            for (int i = 0; i < icons; i++) { Shape(new Rect(column + i * (iconSize + 3), rect.y + 44, iconSize, iconSize), fighter.PlayerIndex, accent); if (Repainting) lifeIconsThisFrame++; }
            if (stocks > 5) Label(new Rect(column, rect.y + 60, columnWidth, 16), "×" + stocks, tiny, Paper);
            if (stocks == 0) Label(new Rect(column, rect.y + 40, columnWidth, 20), $"PUESTO {fighter.Place}", tiny, Paper);
            bool missing = (match.MissingDeviceMask & (1 << fighter.PlayerIndex)) != 0;
            string status = missing ? "SIN MANDO" : fighter.Eliminated ? "" : fighter.IsRespawning ? "VOLVIENDO" :
                match.IsTraining && fighter.PlayerIndex == 1 ? "OBJETIVO" : fighter.IsBot ? "IA" : "HUMANO";
            if (status.Length > 0) Label(new Rect(column, rect.y + 66, columnWidth, 20), status, tiny, missing ? Pink : Muted);
            // «Escándalo» bar.
            bool ready = fighter.Meter >= 100f && !fighter.Eliminated;
            Label(new Rect(rect.x + 12, rect.y + 92, rect.width * .5f, 16), "ESCÁNDALO", tiny, Muted);
            Label(new Rect(rect.x + rect.width * .4f, rect.y + 92, rect.width * .6f - 12, 16),
                ready ? "SUPER LISTO" : Mathf.FloorToInt(fighter.Meter) + "", tiny, ready ? Pink : Muted, TextAnchor.UpperRight);
            Fill(new Rect(rect.x + 12, rect.y + 110, rect.width - 24, 6), Track);
            Fill(new Rect(rect.x + 12, rect.y + 110, (rect.width - 24) * Mathf.Clamp01(fighter.Meter / 100f), 6), ready ? Pink : accent);
        }

        /// <summary>Combo counter beside the attacker (under its badge), for every fighter with a live combo.</summary>
        private void DrawComboNear(BrawlFighter fighter, Vector2 badgeCentre, bool outside)
        {
            int hits = fighter.ComboHits;
            if (hits < 2 || outside) return;
            Rect rect = new Rect(badgeCentre.x - 70, badgeCentre.y + 2, 140, 30);
            Fill(rect, palette.BadgeFill);
            Fill(new Rect(rect.x, rect.y, 3, rect.height), PlayerTint(fighter));
            Label(rect, hits + " GOLPES", markerText, Pink, TextAnchor.MiddleCenter);
            if (Repainting) comboLabelsThisFrame++;
        }

        private void DestroyCombatTextures()
        {
            if (shapeTextures == null) return;
            foreach (var texture in shapeTextures) if (texture != null) Destroy(texture);
        }
    }
}
