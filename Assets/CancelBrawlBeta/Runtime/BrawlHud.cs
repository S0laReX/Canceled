using System;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>Small, resolution-independent presentation layer for the local prototype.</summary>
    public sealed class BrawlHud : MonoBehaviour
    {
        private const float Width = 1600f;
        private const float Height = 900f;
        private static readonly Color Ink = new Color(.035f, .047f, .085f, .96f);
        private static readonly Color Card = new Color(.075f, .09f, .14f);
        private static readonly Color Paper = new Color(.94f, .96f, 1f);
        private static readonly Color Muted = new Color(.59f, .66f, .77f);
        private static readonly Color Cyan = new Color(.22f, .93f, .93f);
        private static readonly Color Pink = new Color(1f, .33f, .65f);
        private BrawlMatch match;
        private Camera markerCamera;
        private Texture2D pixel;
        private GUIStyle title, heading, body, small, number, centered, button, markerText, tiny;
        private bool stylesReady;

        public void Configure(BrawlMatch owner) => match = owner;

        private void OnDestroy()
        {
            if (pixel != null) Destroy(pixel);
        }

        private void EnsureStyles()
        {
            if (stylesReady) return;
            pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            pixel.name = "Beta UI pixel";
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
            title = TextStyle(76, FontStyle.Bold);
            heading = TextStyle(28, FontStyle.Bold);
            body = TextStyle(20);
            small = TextStyle(15);
            tiny = TextStyle(13);
            number = TextStyle(49, FontStyle.Bold);
            centered = TextStyle(21, FontStyle.Bold, TextAnchor.MiddleCenter);
            markerText = TextStyle(17, FontStyle.Bold, TextAnchor.MiddleCenter);
            button = TextStyle(19, FontStyle.Bold, TextAnchor.MiddleCenter);
            button.normal.background = pixel;
            button.normal.textColor = Ink;
            button.hover.background = pixel;
            button.hover.textColor = Ink;
            button.active.background = pixel;
            button.active.textColor = Ink;
            button.padding = new RectOffset(10, 10, 4, 4);
            stylesReady = true;
        }

        private static GUIStyle TextStyle(int size, FontStyle weight = FontStyle.Normal,
            TextAnchor alignment = TextAnchor.UpperLeft)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = weight,
                alignment = alignment,
                wordWrap = true,
                richText = false,
                normal = { textColor = Color.white },
                padding = new RectOffset(0, 0, 0, 0)
            };
        }

        private void OnGUI()
        {
            if (match == null) return;
            EnsureStyles();
            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            float scale = Mathf.Min(Screen.width / Width, Screen.height / Height);
            Vector3 offset = new Vector3((Screen.width - Width * scale) * .5f,
                (Screen.height - Height * scale) * .5f, 0f);
            GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one * scale);
            // The camera reserves this strip for UI; repaint it fully to remove previous menu pixels.
            Fill(new Rect(0, 729, Width, 171), new Color(.02f, .03f, .06f, 1f));
            if (match.Phase == MatchPhase.Menu) DrawMenu();
            else
            {
                DrawWorldMarkers(scale, offset);
                DrawMatch();
                if (match.Phase == MatchPhase.Countdown) DrawCountdown();
                else if (match.Phase == MatchPhase.Paused) DrawPause();
                else if (match.Phase == MatchPhase.Results) DrawResults();
            }
            GUI.color = oldColor;
            GUI.matrix = oldMatrix;
        }

        private void DrawWorldMarkers(float scale, Vector3 offset)
        {
            if (match.Fighters == null || scale <= 0f) return;
            if (markerCamera == null) markerCamera = Camera.main;
            if (markerCamera == null) return;

            // Leave space for the header and stock cards, even when a launch leaves the view.
            Rect field = new Rect(52, 125, Width - 104, 575);
            foreach (BrawlFighter fighter in match.Fighters)
            {
                if (fighter == null || fighter.Eliminated || fighter.IsRespawning ||
                    !fighter.gameObject.activeInHierarchy) continue;
                Vector3 screen = markerCamera.WorldToScreenPoint(fighter.transform.position + Vector3.up * 2.25f);
                Vector2 position = new Vector2((screen.x - offset.x) / scale,
                    (Screen.height - screen.y - offset.y) / scale);
                if (screen.z < 0)
                    position = field.center - (position - field.center);

                bool outside = screen.z < 0 || !field.Contains(position);
                string label = "P" + (fighter.PlayerIndex + 1) + (fighter.IsBot ? "  IA" : "");
                if (outside)
                {
                    Vector2 direction = position - field.center;
                    float horizontal = Mathf.Abs(direction.x) / (field.width * .5f);
                    float vertical = Mathf.Abs(direction.y) / (field.height * .5f);
                    string arrow = horizontal > vertical ? direction.x < 0 ? "<" : ">" : direction.y < 0 ? "^" : "v";
                    label = "P" + (fighter.PlayerIndex + 1) + "  " + arrow;
                    position.x = Mathf.Clamp(position.x, field.xMin, field.xMax);
                    position.y = Mathf.Clamp(position.y, field.yMin, field.yMax);
                }

                Rect badge = new Rect(position.x - 39, position.y - 29, 78, 28);
                Fill(badge, new Color(.025f, .035f, .065f, .94f));
                Fill(new Rect(badge.x, badge.yMax - 3, badge.width, 3), fighter.PlayerColor);
                Label(new Rect(badge.x + 2, badge.y, badge.width - 4, badge.height - 3), label,
                    markerText, fighter.PlayerColor);
            }
        }

        // ───────────────────────── Menu ─────────────────────────
        private void DrawMenu()
        {
            Fill(new Rect(0, 0, Width, Height), new Color(.02f, .03f, .065f, .9f));
            Fill(new Rect(80, 46, 65, 5), Cyan);
            Label(new Rect(80, 58, 800, 86), match.Title, title, Paper);
            Label(new Rect(84, 146, 820, 34), "El último en pie se queda con la cancha.", heading, Muted);
            Label(new Rect(1010, 70, 510, 26), "LOCAL  /  " + match.DisplayVersion, body, Cyan, TextAnchor.UpperRight);
            Label(new Rect(1010, 102, 510, 52), "Empuja a tus rivales fuera del escenario.\n100 % de daño no elimina por sí solo.", small, Paper, TextAnchor.UpperRight);
            bool menuEnabled = GUI.enabled;
            GUI.enabled = match.CanStartTraining;
            if (match.IsProduction && Button(new Rect(1010, 158, 510, 32), "ENTRENAMIENTO / COMBAT LAB", Cyan)) match.StartTraining();
            GUI.enabled = menuEnabled;

            Panel(new Rect(80, 196, 900, 606));
            // 01 · fighter
            Label(new Rect(104, 212, 852, 28), "01  TU LUCHADOR  (P1)  ·  clic otra vez para cambiar de aspecto", body, Muted);
            for (int i = 0; i < BrawlRoster.Count; i++)
            {
                BrawlKit kit = BrawlRoster.Get(i);
                CharacterCard(new Rect(104 + i * 172, 246, 162, 92), kit, match.SelectedCharacter == i, i);
            }
            BrawlKit chosen = BrawlRoster.Get(match.SelectedCharacter);
            Label(new Rect(104, 348, 852, 22), $"Fortaleza: {chosen.Strength}   ·   Debilidad: {chosen.Weakness}", small, Paper);
            Label(new Rect(104, 372, 852, 22),
                $"Fuerte {chosen.HeavyName}  ·  Especial {chosen.SpecialName}  ·  Recuperación {chosen.RecoveryName}  ·  Super {chosen.SuperName}", small, Muted);

            // 02 · court
            Label(new Rect(104, 410, 700, 28), "02  CANCHA", body, Muted);
            for (int i = 0; i < BrawlArena.Count; i++)
            {
                Rect rect = new Rect(104 + i * 214, 444, 206, 78);
                bool selected = match.ArenaIndex == i;
                Fill(rect, selected ? new Color(Cyan.r * .16f, Cyan.g * .16f, Cyan.b * .16f, 1f) : Card);
                Fill(new Rect(rect.x, rect.y, 4, rect.height), selected ? Cyan : Muted * .5f);
                if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) match.ArenaIndex = i;
                Label(new Rect(rect.x + 14, rect.y + 10, rect.width - 22, 24), BrawlArena.Names[i].ToUpperInvariant(), small, selected ? Cyan : Paper);
                Label(new Rect(rect.x + 14, rect.y + 34, rect.width - 22, 40), BrawlArena.Descriptions[i], tiny, Muted);
            }

            // 03 · rivals
            Label(new Rect(104, 540, 700, 28), "03  RIVALES  ·  clic: personaje  ·  clic derecho: aspecto", body, Muted);
            for (int i = 1; i <= 4; i++)
            {
                int choice = i;
                if (Button(new Rect(104 + (i - 1) * 56, 578, 48, 46), i.ToString(), match.OpponentCount == i ? Cyan : Muted))
                    match.OpponentCount = choice;
            }
            int pads = match.LocalCoop ? Mathf.Min(Mathf.Max(0, match.ConnectedPads - (match.P1UsesGamepad ? 1 : 0)), 4) : 0;
            for (int slot = 1; slot <= match.OpponentCount; slot++)
            {
                Rect chip = new Rect(338 + (slot - 1) * 156, 578, 148, 46);
                BrawlKit kit = BrawlRoster.Get(match.SlotCharacters[slot]);
                Fill(chip, Card);
                Fill(new Rect(chip.x, chip.yMax - 3, chip.width, 3), kit.Accent);
                Event click = Event.current;
                if (click.type == EventType.MouseDown && click.button == 1 && chip.Contains(click.mousePosition))
                { match.CycleSkin(slot); click.Use(); }
                if (GUI.Button(chip, GUIContent.none, GUIStyle.none)) match.CycleCharacter(slot);
                int character = match.SlotCharacters[slot];
                Label(new Rect(chip.x + 8, chip.y + 4, chip.width - 12, 20), $"P{slot + 1}  ·  {(slot <= pads ? "MANDO" : "IA")}", tiny, Muted);
                if (BrawlRoster.SkinCount(character) > 1)
                    Label(new Rect(chip.x + 8, chip.y + 4, chip.width - 14, 20),
                        $"aspecto {BrawlRoster.WrapSkin(character, match.SlotSkins[slot]) + 1}/{BrawlRoster.SkinCount(character)}", tiny, Muted, TextAnchor.UpperRight);
                Label(new Rect(chip.x + 8, chip.y + 21, chip.width - 12, 22), match.CharacterName(character), small, Paper);
            }
            if (Button(new Rect(104, 642, 852, 50),
                    match.LocalCoop ? "MANDOS LOCALES ACTIVOS  ·  clic para jugar solo" :
                        "JUGAR SOLO CONTRA IA  ·  clic para sumar mandos", new Color(.69f, .75f, .86f)))
                match.LocalCoop = !match.LocalCoop;
            GUI.enabled = match.LocalCoop;
            if (Button(new Rect(104, 702, 320, 36), match.P1UsesGamepad ? "P1: MANDO" : "P1: TECLADO Y RATÓN", Cyan))
                match.P1UsesGamepad = !match.P1UsesGamepad;
            GUI.enabled = menuEnabled;
            string padInfo = $"Mandos: {match.ConnectedPads}.  " + (match.LocalCoop && match.P1UsesGamepad ? "Dos mandos como mínimo para versus." : "Conecta mandos antes de empezar.");
            if (match.RejectedPads > 0) padInfo += $"  {match.RejectedPads} mando(s) sobrante(s) no participan: máximo 5 jugadores.";
            Label(new Rect(438, 702, 518, 42), padInfo, small, match.RejectedPads > 0 ? Pink : Muted);
            Label(new Rect(104, 748, 852, 40), $"{match.Settings.startingStocks} vidas  ·  {Mathf.RoundToInt(match.Settings.matchSeconds / 60)} minutos  ·  mayor daño = mayor empuje  ·  partida libre sin rating", small, Muted);

            GUI.enabled = match.CanStartMatch;
            if (Button(new Rect(80, 818, 900, 62), match.CanStartMatch ? $"ENTRAR A {BrawlArena.Names[match.ArenaIndex].ToUpperInvariant()}   /   ENTER" : "CONECTA DOS MANDOS PARA INICIAR", Cyan)) match.StartMatch();
            GUI.enabled = menuEnabled;
            DrawControls(new Rect(1010, 196, 510, 470), true);
            DrawOptions(new Rect(1010, 682, 510, 198));
        }

        private void CharacterCard(Rect rect, BrawlKit kit, bool selected, int index)
        {
            Fill(rect, selected ? new Color(kit.Accent.r * .18f, kit.Accent.g * .18f, kit.Accent.b * .18f, 1f) : Card);
            Fill(new Rect(rect.x, rect.y, 4, rect.height), selected ? kit.Accent : Muted * .5f);
            // Clicking the selected card again cycles its look.
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                if (selected) match.CycleSkin(0); else match.SelectedCharacter = index;
            }
            Label(new Rect(rect.x + 13, rect.y + 12, rect.width - 18, 26), match.CharacterName(index).ToUpperInvariant(), small, selected ? kit.Accent : Paper);
            Label(new Rect(rect.x + 13, rect.y + 38, rect.width - 18, 24), kit.Role, tiny, Muted);
            int skins = BrawlRoster.SkinCount(index);
            string look = selected
                ? $"{BrawlRoster.SkinName(index, match.SlotSkins[0])}  {match.SlotSkins[0] + 1}/{skins}" + (skins > 1 ? "  ›" : "")
                : skins > 1 ? $"{skins} aspectos" : "";
            Label(new Rect(rect.x + 13, rect.y + 62, rect.width - 18, 24), look, tiny, selected ? Paper : Muted);
        }

        private void DrawOptions(Rect rect)
        {
            Panel(rect);
            float x = rect.x + 24, y = rect.y + 16;
            Label(new Rect(x, y, rect.width - 48, 30), "OPCIONES", heading, Paper);
            if (SmallButton(new Rect(rect.xMax - 24 - 196, y + 2, 96, 26), "GENERAL", match.ShowPadOptions ? Muted : Cyan)) match.ShowPadOptions = false;
            if (SmallButton(new Rect(rect.xMax - 24 - 96, y + 2, 96, 26), "MANDO", match.ShowPadOptions ? Cyan : Muted)) match.ShowPadOptions = true;
            y += 40;
            if (match.ShowPadOptions) { DrawPadOptions(x, y, rect.width - 48); return; }
            Label(new Rect(x, y + 8, 250, 26), "Reducir destellos y temblor", small, Muted);
            if (Button(new Rect(rect.xMax - 150, y, 126, 36), BrawlFeedback.ReducedEffects ? "SÍ" : "NO",
                    BrawlFeedback.ReducedEffects ? Cyan : Muted))
                match.SetReducedEffects(!BrawlFeedback.ReducedEffects);
            y += 46;
            VolumeRow(new Rect(x, y, rect.width - 48, 36), "Efectos", BrawlFeedback.SfxVolume, v => match.SetVolumes(v, BrawlFeedback.MusicVolume));
            y += 46;
            VolumeRow(new Rect(x, y, rect.width - 48, 36), "Música", BrawlFeedback.MusicVolume, v => match.SetVolumes(BrawlFeedback.SfxVolume, v));
        }

        /// <summary>SYS-026: stick deadzones, response curve and vibration; the d-pad stays a digital alternative.</summary>
        private void DrawPadOptions(float x, float y, float width)
        {
            const float row = 34;
            StepRow(new Rect(x, y, width, row - 4), "Zona muerta mover", $"{Mathf.RoundToInt(match.MoveDeadzone * 100)} %",
                Mathf.InverseLerp(BrawlInputTuning.MinDeadzone, BrawlInputTuning.MaxDeadzone, match.MoveDeadzone),
                d => match.SetStickTuning(match.MoveDeadzone + d * .05f, match.AimDeadzone, match.StickResponse));
            y += row;
            StepRow(new Rect(x, y, width, row - 4), "Zona muerta orientar", $"{Mathf.RoundToInt(match.AimDeadzone * 100)} %",
                Mathf.InverseLerp(BrawlInputTuning.MinDeadzone, BrawlInputTuning.MaxDeadzone, match.AimDeadzone),
                d => match.SetStickTuning(match.MoveDeadzone, match.AimDeadzone + d * .05f, match.StickResponse));
            y += row;
            string response = match.StickResponse > 1.01f ? "rápida" : match.StickResponse < .99f ? "fina" : "lineal";
            StepRow(new Rect(x, y, width, row - 4), "Respuesta palanca", $"{match.StickResponse:0.00} {response}",
                Mathf.InverseLerp(BrawlInputTuning.MinResponse, BrawlInputTuning.MaxResponse, match.StickResponse),
                d => match.SetStickTuning(match.MoveDeadzone, match.AimDeadzone, match.StickResponse + d * .25f));
            y += row;
            StepRow(new Rect(x, y, width, row - 4), "Vibración", match.VibrationStrength <= 0 ? "apagada" : $"{Mathf.RoundToInt(match.VibrationStrength * 100)} %",
                match.VibrationStrength, d => match.SetVibration(match.VibrationStrength + d * .25f));
        }

        /// <summary>Label · value · [–] bar [+] row; <paramref name="step"/> receives -1 or +1.</summary>
        private void StepRow(Rect rect, string name, string value, float fraction, Action<int> step)
        {
            Label(new Rect(rect.x, rect.y + 6, 170, 24), name, small, Muted);
            Label(new Rect(rect.x + 168, rect.y + 6, 92, 24), value, tiny, Paper);
            if (Button(new Rect(rect.xMax - 200, rect.y, 38, rect.height), "–", Muted)) step(-1);
            Fill(new Rect(rect.xMax - 154, rect.y + rect.height * .5f - 4, 108, 8), new Color(.18f, .22f, .30f));
            Fill(new Rect(rect.xMax - 154, rect.y + rect.height * .5f - 4, 108 * Mathf.Clamp01(fraction), 8), Cyan);
            if (Button(new Rect(rect.xMax - 38, rect.y, 38, rect.height), "+", Muted)) step(1);
        }

        private void VolumeRow(Rect rect, string name, float value, Action<float> apply)
        {
            Label(new Rect(rect.x, rect.y + 8, 140, 26), name, small, Muted);
            if (Button(new Rect(rect.xMax - 236, rect.y, 44, rect.height), "–", Muted)) apply(Mathf.Round((value - .1f) * 10) / 10);
            Fill(new Rect(rect.xMax - 184, rect.y + 14, 132, 8), new Color(.18f, .22f, .30f));
            Fill(new Rect(rect.xMax - 184, rect.y + 14, 132 * value, 8), Cyan);
            if (Button(new Rect(rect.xMax - 44, rect.y, 44, rect.height), "+", Muted)) apply(Mathf.Round((value + .1f) * 10) / 10);
        }

        // ───────────────────────── Match ─────────────────────────
        private void DrawMatch()
        {
            Panel(new Rect(25, 22, 360, 68));
            Label(new Rect(42, 31, 330, 28), match.Title, body, Paper);
            Label(new Rect(42, 63, 330, 20), "BETA LOCAL  ·  " + match.Arena.DisplayName.ToUpperInvariant(), small, Cyan);
            Panel(new Rect(710, 18, 180, 72));
            int seconds = Mathf.CeilToInt(Mathf.Max(0, match.RemainingTime));
            Label(new Rect(710, 18, 180, 72), match.IsTraining ? "LAB" : $"{seconds / 60:00}:{seconds % 60:00}", number,
                seconds <= 30 ? Pink : Paper, TextAnchor.MiddleCenter);
            Label(new Rect(1190, 30, 380, 34), "ESC  /  PAUSA", small, Muted, TextAnchor.UpperRight);
            if (match.IsTraining) DrawTraining();
            if (!string.IsNullOrEmpty(match.Announcement) && match.Phase == MatchPhase.Playing)
            {
                Panel(new Rect(440, 112, 720, 47));
                Label(new Rect(450, 114, 700, 43), match.Announcement, body, Paper, TextAnchor.MiddleCenter);
            }

            BrawlFighter[] fighters = match.Fighters;
            if (fighters != null && fighters.Length > 0)
            {
                float cardWidth = Mathf.Min(310, (Width - 70 - (fighters.Length - 1) * 12) / fighters.Length);
                float start = (Width - (cardWidth * fighters.Length + 12 * (fighters.Length - 1))) * .5f;
                for (int i = 0; i < fighters.Length; i++)
                {
                    if (fighters[i] != null) DrawFighter(new Rect(start + i * (cardWidth + 12), 738, cardWidth, 124), fighters[i]);
                }
            }
            Label(new Rect(25, 876, 1550, 22),
                $"{MoveLabel()} mover   ·   RATÓN orientar   ·   {KeyLabel(BrawlAction.Jump)} doble salto   ·   {KeyLabel(BrawlAction.Basic)} / {KeyLabel(BrawlAction.Heavy)} básico / fuerte   ·   " +
                $"{KeyLabel(BrawlAction.Special)} especial   ·   {KeyLabel(BrawlAction.Recovery)} recuperar   ·   {KeyLabel(BrawlAction.Dodge)} esquiva   ·   {KeyLabel(BrawlAction.Super)} super   ·   {KeyLabel(BrawlAction.FastFall)} caer",
                small, Paper, TextAnchor.MiddleCenter);
        }

        private void DrawFighter(Rect rect, BrawlFighter fighter)
        {
            Color accent = fighter.Eliminated ? Muted : fighter.PlayerColor;
            Panel(rect);
            Fill(new Rect(rect.x, rect.y, rect.width, 4), accent);
            Label(new Rect(rect.x + 14, rect.y + 13, rect.width - 28, 24),
                $"P{fighter.PlayerIndex + 1}  {match.CharacterName(fighter.CharacterIndex).ToUpperInvariant()}", small, accent);
            Label(new Rect(rect.x + 14, rect.y + 36, 146, 57),
                fighter.Eliminated ? "FUERA" : $"{Mathf.RoundToInt(fighter.Damage)}%",
                fighter.Eliminated ? heading : number,
                fighter.Damage >= 100f ? Pink : Paper);
            string status = (match.MissingDeviceMask & (1 << fighter.PlayerIndex)) != 0 ? "SIN MANDO" : fighter.Eliminated ? $"PUESTO {fighter.Place}" :
                fighter.IsRespawning ? "VOLVIENDO…" : match.IsTraining && fighter.PlayerIndex == 1 ? "OBJETIVO" : fighter.IsBot ? "IA" : "HUMANO";
            Label(new Rect(rect.x + 164, rect.y + 41, rect.width - 176, 22),
                $"{fighter.Stocks} VIDAS", small, Paper);
            Label(new Rect(rect.x + 164, rect.y + 67, rect.width - 176, 24), status, small, Muted);
            Fill(new Rect(rect.x + 14, rect.y + 103, rect.width - 28, 6), new Color(.18f, .22f, .30f));
            Fill(new Rect(rect.x + 14, rect.y + 103, (rect.width - 28) * Mathf.Clamp01(fighter.Meter / 100f), 6),
                fighter.Meter >= 100f ? Pink : accent);
            if (fighter.Meter >= 100f && !fighter.Eliminated)
                Label(new Rect(rect.x + 154, rect.y + 85, rect.width - 168, 18), "SUPER LISTO", small, Pink);
        }

        private void DrawTraining()
        {
            Panel(new Rect(1000, 106, 570, 178));
            Label(new Rect(1016, 114, 540, 26), "ENTRENAMIENTO  ·  objetivo sin entradas", body, Cyan);
            Label(new Rect(1016, 148, 245, 24), $"Daño inicial: {match.TrainingDamage:0}%", small, Paper);
            bool oldEnabled = GUI.enabled;
            GUI.enabled = match.Phase == MatchPhase.Playing;
            if (Button(new Rect(1260, 146, 62, 32), "−25", Muted)) match.SetTrainingDamage(match.TrainingDamage - 25);
            if (Button(new Rect(1330, 146, 62, 32), "+25", Muted)) match.SetTrainingDamage(match.TrainingDamage + 25);
            if (Button(new Rect(1400, 146, 150, 32), "REINICIAR", Cyan)) match.ResetTraining();
            if (Button(new Rect(1016, 190, 258, 34), match.ShowCombatVolumes ? "VOLÚMENES: SÍ" : "VOLÚMENES: NO", Muted))
                match.ShowCombatVolumes = !match.ShowCombatVolumes;
            if (Button(new Rect(1286, 190, 264, 34), "SUPER LISTO", Pink)) match.FillTrainingMeter();
            GUI.enabled = oldEnabled;
            Label(new Rect(1016, 234, 530, 42), "Amarillo: preparación/final · rojo: activo.\nAlcance de melee; paredes bloquean. Cruz: raíz del objetivo.", small, Muted);
        }

        private void DrawCountdown()
        {
            int count = Mathf.CeilToInt(match.CountdownRemaining);
            string value = count > 0 ? count.ToString() : "¡LUCHA!";
            Panel(new Rect(645, 304, 310, 163));
            Label(new Rect(645, 310, 310, 135), value, title, Cyan, TextAnchor.MiddleCenter);
            Label(new Rect(470, 485, 660, 52), "Sobrevive. El borde es el verdadero rival.", centered, Paper);
        }

        private void DrawPause()
        {
            Fill(new Rect(0, 0, Width, Height), new Color(.015f, .022f, .045f, .87f));
            Panel(new Rect(130, 180, 530, 420));
            Label(new Rect(166, 210, 458, 70), "PAUSA", title, Paper);
            string pauseText = match.DevicePause
                ? match.MissingDeviceMask != 0 ? "Falta mando: " + match.MissingDeviceNames + ". Reconéctalo o pulsa Start en uno nuevo para ocupar el puesto libre."
                    : "Mandos recuperados. Pulsa Continuar o Start para reanudar."
                : "Toma aire. La cancha puede esperar.";
            Label(new Rect(170, 300, 420, 90), pauseText, body, Muted);
            bool previous = GUI.enabled;
            GUI.enabled = match.MissingDeviceMask == 0;
            if (Button(new Rect(170, 400, 450, 62), "CONTINUAR  /  ESC", Cyan)) match.TogglePause();
            GUI.enabled = previous;
            if (match.DevicePause && match.MissingDeviceMask != 0 && Button(new Rect(170, 470, 450, 40), "ASIGNAR MANDOS DISPONIBLES", Cyan)) match.ReassignMissingDevices();
            if (Button(new Rect(170, 526, 450, 58), "VOLVER AL MENÚ", Muted)) match.ReturnToMenu();
            DrawOptions(new Rect(130, 616, 530, 198));
            DrawControls(new Rect(720, 180, 750, 470), false);
        }

        private void DrawResults()
        {
            Fill(new Rect(0, 0, Width, Height), new Color(.015f, .022f, .045f, .9f));
            Panel(new Rect(260, 70, 1080, 760));
            Label(new Rect(300, 98, 1000, 42), match.Arena.DisplayName.ToUpperInvariant() + " TIENE UN VEREDICTO", heading, Cyan);
            Label(new Rect(300, 146, 1000, 92), "RESULTADOS", title, Paper);
            Label(new Rect(870, 252, 430, 20), "VIDAS · DAÑO     KO · CAÍDAS PROPIAS · DAÑO INFLIGIDO", tiny, Muted, TextAnchor.UpperRight);
            BrawlFighter[] fighters = match.Fighters == null ? Array.Empty<BrawlFighter>() :
                (BrawlFighter[])match.Fighters.Clone();
            Array.Sort(fighters, (a, b) => (a == null ? 99 : a.Place).CompareTo(b == null ? 99 : b.Place));
            float y = 280;
            foreach (BrawlFighter fighter in fighters)
            {
                if (fighter == null) continue;
                Color accent = fighter.Place == 1 ? Cyan : Muted;
                Fill(new Rect(300, y, 1000, 72), Card);
                Label(new Rect(320, y + 14, 82, 44), $"#{fighter.Place}", heading, accent);
                Label(new Rect(411, y + 10, 440, 30), $"P{fighter.PlayerIndex + 1}  {match.CharacterName(fighter.CharacterIndex)}", heading, fighter.PlayerColor);
                Label(new Rect(415, y + 44, 440, 20), fighter.IsBot ? "RIVAL IA" : "JUGADOR LOCAL", small, Muted);
                Label(new Rect(850, y + 10, 430, 28), $"{fighter.Stocks} vidas  ·  {Mathf.RoundToInt(fighter.Damage)}%", body, Paper, TextAnchor.MiddleRight);
                Label(new Rect(850, y + 42, 430, 22),
                    $"{fighter.KOs} KO  ·  {fighter.SelfDestructs} caídas propias  ·  {Mathf.RoundToInt(fighter.TotalDamageDealt)} daño", small, Muted, TextAnchor.MiddleRight);
                y += 82;
            }
            if (Button(new Rect(300, 712, 490, 58), "REVANCHA  /  ENTER", Cyan)) match.StartMatch();
            if (Button(new Rect(810, 712, 490, 58), "CAMBIAR LUCHADOR O CANCHA", Muted)) match.ReturnToMenu();
            Label(new Rect(300, 782, 1000, 40), "Partida libre local · sin puntuación competitiva. KO y daño son estadísticas: el puesto lo decide la supervivencia.", small, Muted, TextAnchor.MiddleCenter);
        }

        /// <summary>
        /// Controls table drawn from <see cref="BrawlMatch.Bindings"/> — the same data gameplay reads — with the
        /// real names of the connected keyboard layout and gamepad family. Clicking a cell starts a remap.
        /// </summary>
        private void DrawControls(Rect rect, bool includeIntro)
        {
            Panel(rect);
            BrawlBindings bindings = match.Bindings;
            UnityEngine.InputSystem.Gamepad pad = match.LabelPad;
            float x = rect.x + 22, inner = rect.width - 44;
            float y = rect.y + 14;
            Label(new Rect(x, y, 214, 32), "CONTROLES", heading, Paper);
            Label(new Rect(x + 214, y, inner - 214, 48),
                "Clic en un control para cambiarlo.\nEsc / Start: pausa (reservados).\nOrientar: ratón / palanca der.", tiny, Muted);
            bool previous = GUI.enabled;
            GUI.enabled = previous && match.CanRemap && !match.RemapListening;
            y += 48;
            float actionWidth = inner * .36f, cellWidth = (inner - actionWidth - 8) * .5f;
            Label(new Rect(x + actionWidth, y, cellWidth, 20), "TECLADO Y RATÓN", tiny, Cyan);
            Label(new Rect(x + actionWidth + cellWidth + 8, y, cellWidth, 20), BrawlBindings.PadFamily(pad), tiny, Cyan);
            y += 22;
            float row = Mathf.Floor((rect.yMax - 76 - y) / BrawlBindings.KeyboardActions.Length);
            foreach (BrawlAction action in BrawlBindings.KeyboardActions)
            {
                Label(new Rect(x, y + 3, actionWidth - 6, row), BrawlBindings.ActionName(action), small, Muted);
                BindingCell(new Rect(x + actionWidth, y, cellWidth, row - 3), BindingScheme.Keyboard, action, bindings.Label(BindingScheme.Keyboard, action));
                Rect padCell = new Rect(x + actionWidth + cellWidth + 8, y, cellWidth, row - 3);
                if (Array.IndexOf(BrawlBindings.GamepadActions, action) >= 0)
                    BindingCell(padCell, BindingScheme.Gamepad, action, bindings.Label(BindingScheme.Gamepad, action, pad));
                else if (action == BrawlAction.MoveUp)
                    Label(new Rect(padCell.x + 8, padCell.y + 3, padCell.width, row), "Palanca izq. / cruceta", small, Muted);
                y += row;
            }
            // Priority: remap feedback, then how the options file was recovered, then the conflict rule.
            string message = match.RemapMessage;
            bool optionsNotice = string.IsNullOrEmpty(message) && !string.IsNullOrEmpty(match.OptionsStatusText);
            if (optionsNotice) message = match.OptionsStatusText;
            Label(new Rect(x, rect.yMax - 72, inner, 34), string.IsNullOrEmpty(message) ? "Un control repetido se intercambia con la acción que lo tenía." : message,
                tiny, match.RemapListening ? Cyan : match.RemapMessageIsWarning || optionsNotice ? Pink : Muted);
            if (SmallButton(new Rect(x, rect.yMax - 36, 168, 26), "RESTAURAR TECLADO", Muted)) bindings.ResetDefaults(BindingScheme.Keyboard);
            if (SmallButton(new Rect(x + 176, rect.yMax - 36, 168, 26), "RESTAURAR MANDO", Muted)) bindings.ResetDefaults(BindingScheme.Gamepad);
            GUI.enabled = previous;
        }

        private void BindingCell(Rect rect, BindingScheme scheme, BrawlAction action, string text)
        {
            bool listening = match.RemapListening && match.RemapScheme == scheme && match.RemapAction == action;
            Fill(rect, listening ? new Color(Pink.r * .3f, Pink.g * .3f, Pink.b * .3f, 1f) : Card);
            Fill(new Rect(rect.x, rect.y, 3, rect.height), listening ? Pink : Muted * .5f);
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) match.BeginRemap(scheme, action);
            Label(new Rect(rect.x + 10, rect.y + 2, rect.width - 14, rect.height - 2), listening ? "pulsa…" : text, small, listening ? Pink : Paper);
        }

        /// <summary>Movement keys as one label: "WASD" when all four are single characters.</summary>
        private string MoveLabel()
        {
            BrawlBindings b = match.Bindings;
            string up = b.Label(BindingScheme.Keyboard, BrawlAction.MoveUp), left = b.Label(BindingScheme.Keyboard, BrawlAction.MoveLeft);
            string down = b.Label(BindingScheme.Keyboard, BrawlAction.MoveDown), right = b.Label(BindingScheme.Keyboard, BrawlAction.MoveRight);
            return up.Length == 1 && left.Length == 1 && down.Length == 1 && right.Length == 1 ? up + left + down + right : $"{up}/{left}/{down}/{right}";
        }

        private string KeyLabel(BrawlAction action) => match.Bindings.Label(BindingScheme.Keyboard, action).ToUpperInvariant();

        private void Panel(Rect rect) => Fill(rect, Ink);

        private void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, pixel);
            GUI.color = previous;
        }

        private void Label(Rect rect, string text, GUIStyle style, Color color,
            TextAnchor? alignment = null)
        {
            Color previous = GUI.color;
            TextAnchor previousAlignment = style.alignment;
            GUI.color = color;
            if (alignment.HasValue) style.alignment = alignment.Value;
            GUI.Label(rect, text, style);
            style.alignment = previousAlignment;
            GUI.color = previous;
        }

        private bool SmallButton(Rect rect, string text, Color color)
        {
            int size = button.fontSize; button.fontSize = 12;
            bool clicked = Button(rect, text, color);
            button.fontSize = size;
            return clicked;
        }

        private bool Button(Rect rect, string text, Color color)
        {
            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = color;
            bool clicked = GUI.Button(rect, text, button);
            GUI.backgroundColor = previous;
            return clicked;
        }
    }
}
