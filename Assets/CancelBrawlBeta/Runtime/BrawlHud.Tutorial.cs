using UnityEngine;

namespace CancelBrawl.Beta
{
    public enum TutorialStep { Off, Launcher, Chase, Aerial, Finisher, RecoveryJump, RecoverySpecial, RecoveryLand, Done }

    /// <summary>
    /// SYS-076: guided combo escalera (ascendente → salto → aéreo → remate) and recovery (doble salto → recuperación →
    /// aterrizar) inside the training lab. There is no clock: a step only advances when the player does it, never
    /// fails by time, and a dropped combo or a fall just repeats the step. Pausing freezes it like the lab.
    /// </summary>
    public sealed class BrawlTutorial
    {
        public bool Active => Step != TutorialStep.Off;
        public TutorialStep Step { get; private set; }
        public bool InRecoveryPart => Step >= TutorialStep.RecoveryJump && Step <= TutorialStep.RecoveryLand;
        public string Feedback { get; private set; } = "";
        public bool FeedbackIsWarning { get; private set; }
        /// <summary>Counters for checks and for the player: how often a combo dropped / a recovery fell.</summary>
        public int ComboRestarts { get; private set; }
        public int RecoveryRetries { get; private set; }
        /// <summary>Highest combo reached in the last finisher.</summary>
        public int FinisherCombo { get; private set; }
        private float lastDummyDamage;
        private Vector3 lastPosition;
        private bool hasLast;

        public void Begin(BrawlMatch match)
        {
            Step = TutorialStep.Launcher; ComboRestarts = 0; RecoveryRetries = 0; FinisherCombo = 0;
            PlaceForCombo(match);
            Say("Sin límite de tiempo: cada paso espera a que lo hagas.", false);
        }

        public void Stop() { Step = TutorialStep.Off; Feedback = ""; hasLast = false; }

        /// <summary>Places both fighters again for the current part without losing progress.</summary>
        public void Repeat(BrawlMatch match)
        {
            if (!Active || !Ready(match)) return;
            if (InRecoveryPart) { Step = TutorialStep.RecoveryJump; PlaceOffStage(match); }
            else if (Step != TutorialStep.Done) { Step = TutorialStep.Launcher; PlaceForCombo(match); }
            Say("Colocados otra vez. Tómate el tiempo que quieras.", false);
        }

        /// <summary>Jumps to the next part (combo → recovery → end).</summary>
        public void Skip(BrawlMatch match)
        {
            if (!Active || !Ready(match)) return;
            if (Step < TutorialStep.RecoveryJump) { Step = TutorialStep.RecoveryJump; PlaceOffStage(match); Say("Parte de recuperación.", false); }
            else Step = TutorialStep.Done;
        }

        private static bool Ready(BrawlMatch match) => match != null && match.IsTraining && match.Fighters != null && match.Fighters.Length >= 2;

        private void Say(string text, bool warning) { Feedback = text; FeedbackIsWarning = warning; }

        private void PlaceForCombo(BrawlMatch match)
        {
            if (!Ready(match)) return;
            Vector3 spawn = match.Arena.SpawnPoints[0];
            match.Fighters[0].ResetPractice(spawn, 0);
            // One metre apart, as in the lab's own combo check: the launcher connects and the chase reaches.
            match.Fighters[1].ResetPractice(spawn + Vector3.right * ComboGap, 0);
            Sync(match);
        }

        public const float ComboGap = 1f;

        /// <summary>Beyond the right edge of the whole layout, at floor height: close enough for every kit to make it back.</summary>
        private void PlaceOffStage(BrawlMatch match)
        {
            if (!Ready(match)) return;
            Vector3 spawn = match.Arena.SpawnPoints[0];
            match.Fighters[1].ResetPractice(spawn + Vector3.right * 1.6f, 0);
            match.Fighters[0].ResetPractice(new Vector3(OffStageX(match), 1f, 0), 0);
            Sync(match);
        }

        public static float OffStageX(BrawlMatch match) => match.Arena.PlayBounds.xMax + 2f;

        private void Sync(BrawlMatch match)
        {
            lastDummyDamage = match.Fighters[1].Damage;
            lastPosition = match.Fighters[0].transform.position; hasLast = true;
        }

        public void Tick(BrawlMatch match)
        {
            if (!Active) return;
            if (!Ready(match) || match.Phase == MatchPhase.Menu || match.Phase == MatchPhase.Results) { Stop(); return; }
            if (match.Phase != MatchPhase.Playing) return; // paused: nothing moves, nothing expires
            BrawlFighter player = match.Fighters[0], dummy = match.Fighters[1];
            bool hit = dummy.Damage > lastDummyDamage + .01f;
            lastDummyDamage = dummy.Damage;
            Vector3 position = player.transform.position;
            // The lab puts a fallen fighter back on the stage; seen here as a jump of several metres in one frame.
            bool teleported = hasLast && (position - lastPosition).sqrMagnitude > 36f;
            lastPosition = position; hasLast = true;

            switch (Step)
            {
                case TutorialStep.Launcher:
                    if (hit && player.AttackVertical > 0 && !player.AttackAirborne) { Step = TutorialStep.Chase; Say("¡Lanzado! Ahora salta tras él.", false); }
                    else if (hit) Say("Golpe conectado, pero no hacia arriba: mantén arriba al golpear desde el suelo.", true);
                    break;
                case TutorialStep.Chase:
                    if (!player.Grounded && player.JumpsUsed > 0 && !dummy.Grounded) { Step = TutorialStep.Aerial; Say("Bien. Golpéalo en el aire antes de que caiga.", false); }
                    else if (player.ComboHits == 0) ComboDropped();
                    break;
                case TutorialStep.Aerial:
                    if (hit && player.AttackAirborne && player.ComboHits >= 2) { Step = TutorialStep.Finisher; Say("¡Combo de " + player.ComboHits + "! Termina con el remate.", false); }
                    else if (player.ComboHits == 0) ComboDropped();
                    break;
                case TutorialStep.Finisher:
                    if (hit && player.AttackAirborne && player.AttackVertical < 0)
                    {
                        FinisherCombo = player.ComboHits;
                        Step = TutorialStep.RecoveryJump; PlaceOffStage(match);
                        Say((FinisherCombo >= 3 ? "¡Combo escalera completo (" + FinisherCombo + " golpes)! " : "Remate conectado. ") +
                            "Ahora estás fuera del borde: vuelve.", false);
                    }
                    break;
                case TutorialStep.RecoveryJump:
                case TutorialStep.RecoverySpecial:
                case TutorialStep.RecoveryLand:
                    TickRecovery(match, player, teleported);
                    break;
            }
        }

        private void ComboDropped()
        {
            Step = TutorialStep.Launcher; ComboRestarts++;
            Say("Se cortó el combo. No pasa nada: empieza otra vez con el golpe ascendente.", true);
        }

        private void TickRecovery(BrawlMatch match, BrawlFighter player, bool teleported)
        {
            if (teleported)
            {
                RecoveryRetries++; Step = TutorialStep.RecoveryJump; PlaceOffStage(match);
                Say("Caíste. Sin prisa: primero el doble salto, luego la recuperación hacia el escenario.", true);
                return;
            }
            if (Step == TutorialStep.RecoveryJump && player.JumpsUsed >= 2) { Step = TutorialStep.RecoverySpecial; Say("Doble salto usado. Ahora la recuperación.", false); }
            if (Step == TutorialStep.RecoverySpecial && !player.RecoveryAvailable) { Step = TutorialStep.RecoveryLand; Say("¡Eso es! Aterriza en el escenario.", false); }
            if (!player.Grounded) return;
            if (Step == TutorialStep.RecoveryLand) { Step = TutorialStep.Done; Say("Tutorial completado. Puedes repetirlo cuando quieras.", false); }
            else
            {
                RecoveryRetries++; Step = TutorialStep.RecoveryJump; PlaceOffStage(match);
                Say("Llegaste sin usar todos los recursos; se repite para practicarlos.", true);
            }
        }
    }

    public sealed partial class BrawlHud
    {
        public BrawlTutorial Tutorial { get; } = new BrawlTutorial();
        /// <summary>Takes the lab panel's place (top right, same anchor) so it never covers P1's side of the stage.</summary>
        private static readonly Rect TutorialRect = new Rect(1000, 106, 570, 262);

        /// <summary>Opens the lab if needed and starts the guided tutorial.</summary>
        public void StartTutorial()
        {
            if (match == null) return;
            if (!match.IsTraining) match.StartTraining();
            if (match.IsTraining) Tutorial.Begin(match);
        }

        public void ToggleTutorial() { if (Tutorial.Active) Tutorial.Stop(); else StartTutorial(); }

        private void TickTutorial() => Tutorial.Tick(match);

        private bool PointerOverTutorial(Vector2 local) => Tutorial.Active && TutorialRect.Contains(local);

        private string PadLabel(BrawlAction action) => match.Bindings.Label(BindingScheme.Gamepad, action, match.LabelPad);

        private void DrawTutorial()
        {
            if (!Tutorial.Active || !match.IsTraining) return;
            Rect r = TutorialRect;
            Panel(r);
            Fill(new Rect(r.x, r.y, 4, r.height), Cyan);
            bool recovery = Tutorial.InRecoveryPart || Tutorial.Step == TutorialStep.Done;
            Label(new Rect(r.x + 18, r.y + 10, r.width - 36, 26), recovery ? "TUTORIAL  ·  RECUPERACIÓN" : "TUTORIAL  ·  COMBO ESCALERA", body, Cyan);
            Label(new Rect(r.x + 18, r.y + 38, r.width - 36, 20), "Sin límite de tiempo  ·  cada paso espera a que lo hagas", tiny, Muted);
            string up = KeyLabel(BrawlAction.MoveUp), down = KeyLabel(BrawlAction.MoveDown), basic = KeyLabel(BrawlAction.Basic), jump = KeyLabel(BrawlAction.Jump),
                recover = KeyLabel(BrawlAction.Recovery), left = KeyLabel(BrawlAction.MoveLeft);
            string[] steps = recovery
                ? new[]
                {
                    $"Doble salto en el aire: {jump}  ·  mando {PadLabel(BrawlAction.Jump)}",
                    $"Recuperación hacia el escenario: {left} + {recover}  ·  mando palanca + {PadLabel(BrawlAction.Recovery)}",
                    "Aterriza en el suelo o en una plataforma"
                }
                : new[]
                {
                    $"Golpe ascendente desde el suelo: {up} + {basic}  ·  mando arriba + {PadLabel(BrawlAction.Basic)}",
                    $"Salta tras el golpe: {jump}  ·  mando {PadLabel(BrawlAction.Jump)}",
                    $"Golpe aéreo que conecte: {basic} en el aire (combo 2)",
                    $"Remate en el aire: {down} + {basic} sobre el rival"
                };
            int current = recovery ? (int)Tutorial.Step - (int)TutorialStep.RecoveryJump : (int)Tutorial.Step - (int)TutorialStep.Launcher;
            float y = r.y + 62;
            for (int i = 0; i < steps.Length; i++)
            {
                bool done = Tutorial.Step == TutorialStep.Done || i < current, now = i == current;
                Fill(new Rect(r.x + 18, y + 6, 10, 10), done ? Cyan : now ? Paper : Track);
                Label(new Rect(r.x + 36, y, r.width - 56, 24), (done ? "HECHO  " : now ? "AHORA  " : "") + steps[i], small, done ? Cyan : now ? Paper : Muted);
                y += 26;
            }
            y = r.y + 172;
            Label(new Rect(r.x + 18, y, r.width - 36, 40), Tutorial.Feedback, small, Tutorial.FeedbackIsWarning ? Pink : Paper);
            bool oldEnabled = GUI.enabled;
            GUI.enabled = match.Phase == MatchPhase.Playing;
            if (SmallButton(new Rect(r.x + 18, r.yMax - 40, 160, 30), "REPETIR PASO", Muted)) Tutorial.Repeat(match);
            if (SmallButton(new Rect(r.x + 188, r.yMax - 40, 160, 30), Tutorial.Step == TutorialStep.Done ? "OTRA VEZ" : "SIGUIENTE PARTE", Muted))
            { if (Tutorial.Step == TutorialStep.Done) Tutorial.Begin(match); else Tutorial.Skip(match); }
            if (SmallButton(new Rect(r.x + 358, r.yMax - 40, 164, 30), "SALIR", Muted)) Tutorial.Stop();
            GUI.enabled = oldEnabled;
        }
    }
}
