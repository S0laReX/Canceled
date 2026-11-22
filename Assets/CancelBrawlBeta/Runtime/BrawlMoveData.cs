using System;
using System.Collections.Generic;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>Directional 2.5D moves (GDD 2.0 §5). Kit-specific neutral/heavy/special/super come from <see cref="KitTuning"/>.</summary>
    public enum SideMove { UpBasic, UpHeavy, AirNeutral, AirDownBasic, AirDownHeavy }

    /// <summary>
    /// Hit volume of a move in the attacker's local X/Y: X grows toward where the attacker faces (mirrored when facing
    /// left), Y grows upward from the attacker's root. It is compared against the target's hurtbox (SYS-042–048).
    /// </summary>
    [Serializable]
    public struct MoveBox
    {
        [Tooltip("Borde trasero (negativo = detrás del atacante).")] public float back;
        [Tooltip("Borde delantero, en la dirección en que mira.")] public float front;
        [Tooltip("Borde inferior respecto a los pies del atacante.")] public float bottom;
        [Tooltip("Borde superior respecto a los pies del atacante.")] public float top;
        public MoveBox(float back, float front, float bottom, float top) { this.back = back; this.front = front; this.bottom = bottom; this.top = top; }
        public float Width => front - back;
        public float HeightSpan => top - bottom;
    }

    [Serializable]
    public struct MoveDef
    {
        public SideMove id;
        [Range(0, 40)] public float damage;
        [Range(.02f, 1)] public float windup;
        [Range(.02f, 1)] public float active;
        [Range(0, 1.5f)] public float recovery;
        [Range(.3f, 8)] public float range;
        [Range(0, 30)] public float push;
        [Range(-20, 20)] public float lift;
        [Range(.2f, 6)] public float height;
        public MoveBox hitbox;

        public AttackSpec ToSpec(AttackKind kind) => new AttackSpec(kind, damage, windup, active, recovery, range, push, lift, height);
    }

    /// <summary>Editable per-kit numbers (names, styles and art stay in <see cref="BrawlRoster"/>).</summary>
    [Serializable]
    public sealed class KitTuning
    {
        public string kit;
        [Range(.5f, 2)] public float resistance = 1;
        [Range(.5f, 5)] public float basicRange = 2.1f;
        [Range(-1, 1)] public float basicCone = .25f;
        [Range(-10, 15)] public float heavyDamageOffset;
        [Range(.2f, 3)] public float heavyPushScale = 1;
        [Range(.05f, 1.2f)] public float heavyWindup = .28f;
        [Range(.5f, 6)] public float heavyRange = 2.8f;
        [Range(-10, 15)] public float specialDamageOffset;
        [Range(.02f, 1.2f)] public float specialWindup = .23f;
        [Range(.02f, 1.2f)] public float specialActive = .12f;
        [Range(0, 1.5f)] public float specialRecovery = .31f;
        [Range(0, 30)] public float specialPush = 7.5f;
        [Range(-20, 20)] public float specialLift = 5.5f;
        [Range(.3f, 8)] public float specialRange = 3;
        [Range(-15, 20)] public float superDamageOffset;
        [Range(.1f, 2.5f)] public float superWindup = .85f;
        [Range(.02f, 1.5f)] public float superActive = .16f;
        [Range(.5f, 12)] public float superRange = 6;
        [Range(.2f, 6)] public float superHeight = 2.5f;
        [Range(0, 30)] public float superPush = 13;
        [Range(-20, 20)] public float superLift = 9;
        [Range(0, 15)] public float recoveryHorizontal = 4;
        [Range(.2f, 2)] public float recoveryVertical = 1;
        [Range(0, 2)] public float recoveryAirControl = 1;
        [Range(0, 2)] public float recoveryHover;

        public static KitTuning From(BrawlKit k) => new KitTuning
        {
            kit = k.Name, resistance = k.Resistance, basicRange = k.BasicRange, basicCone = k.BasicCone,
            heavyDamageOffset = k.HeavyDamageOffset, heavyPushScale = k.HeavyPushScale, heavyWindup = k.HeavyWindup, heavyRange = k.HeavyRange,
            specialDamageOffset = k.SpecialDamageOffset, specialWindup = k.SpecialWindup, specialActive = k.SpecialActive, specialRecovery = k.SpecialRecovery,
            specialPush = k.SpecialPush, specialLift = k.SpecialLift, specialRange = k.SpecialRange,
            superDamageOffset = k.SuperDamageOffset, superWindup = k.SuperWindup, superActive = k.SuperActive, superRange = k.SuperRange,
            superHeight = k.SuperHeight, superPush = k.SuperPush, superLift = k.SuperLift,
            recoveryHorizontal = k.RecoveryHorizontal, recoveryVertical = k.RecoveryVertical, recoveryAirControl = k.RecoveryAirControl, recoveryHover = k.RecoveryHover
        };

        public void ApplyTo(BrawlKit k)
        {
            k.Resistance = resistance; k.BasicRange = basicRange; k.BasicCone = basicCone;
            k.HeavyDamageOffset = heavyDamageOffset; k.HeavyPushScale = heavyPushScale; k.HeavyWindup = heavyWindup; k.HeavyRange = heavyRange;
            k.SpecialDamageOffset = specialDamageOffset; k.SpecialWindup = specialWindup; k.SpecialActive = specialActive; k.SpecialRecovery = specialRecovery;
            k.SpecialPush = specialPush; k.SpecialLift = specialLift; k.SpecialRange = specialRange;
            k.SuperDamageOffset = superDamageOffset; k.SuperWindup = superWindup; k.SuperActive = superActive; k.SuperRange = superRange;
            k.SuperHeight = superHeight; k.SuperPush = superPush; k.SuperLift = superLift;
            k.RecoveryHorizontal = recoveryHorizontal; k.RecoveryVertical = recoveryVertical; k.RecoveryAirControl = recoveryAirControl; k.RecoveryHover = recoveryHover;
        }
    }

    public static class BrawlMoveData
    {
        /// <summary>Target hurtbox in X/Y: same footprint as the fighter's CharacterController (radius 0.36, height 1.7).</summary>
        public const float HurtboxHalfWidth = .36f, HurtboxHeight = 1.7f;
        /// <summary>Melee reach tolerance added to a move's range (the historical sweep used range + 0.3).</summary>
        public const float ReachTolerance = .3f;
        /// <summary>Targets this close in X are hit even slightly behind the attacker (historical sweep: |dx|² ≤ 0.2).</summary>
        public static readonly float BehindTolerance = Mathf.Sqrt(.2f);

        /// <summary>
        /// Box that reproduces the historical sweep exactly: reach |dx| ≤ range + 0.3 in front (or both sides), vertical
        /// window dy ∈ [dyMin, dyMax] between the roots. Converted so that box ∩ hurtbox ≠ ∅ ⇔ the old rule passed.
        /// </summary>
        public static MoveBox SweepBox(float range, float dyMin, float dyMax, bool bothSides)
        {
            float front = range + ReachTolerance - HurtboxHalfWidth;
            float back = bothSides ? -front : -(BehindTolerance - HurtboxHalfWidth);
            return new MoveBox(back, front, dyMin + HurtboxHeight, dyMax);
        }

        /// <summary>Defaults = the values hard-coded in 0.5.1, now data (GDD 2.0 §5 table).</summary>
        public static MoveDef[] DefaultSideMoves() => new[]
        {
            Move(SideMove.UpBasic, 4, .06f, .12f, .13f, 1.35f, 1.1f, 8.8f, 2.6f, upward: true),
            Move(SideMove.UpHeavy, 10, .06f, .12f, .13f, 1.35f, 1.1f, 12, 2.6f, upward: true),
            Move(SideMove.AirNeutral, 5, .05f, .12f, .15f, 2.1f, 3.5f, 5, 1.65f),
            Move(SideMove.AirDownBasic, 7, .12f, .12f, .27f, 1.3f, 2.5f, -11, 2.3f, downward: true),
            Move(SideMove.AirDownHeavy, 12, .12f, .12f, .27f, 1.3f, 2.5f, -11, 2.3f, downward: true),
        };

        private static MoveDef Move(SideMove id, float damage, float windup, float active, float recovery, float range, float push, float lift, float height,
            bool upward = false, bool downward = false)
        {
            // Historical filters: vertical moves reach both sides; upward ignores targets more than 0.5 below, downward aerials more than 0.5 above.
            float dyMin = upward ? -.5f : -height, dyMax = downward ? .5f : height;
            return new MoveDef
            {
                id = id, damage = damage, windup = windup, active = active, recovery = recovery, range = range, push = push, lift = lift, height = height,
                hitbox = SweepBox(range, dyMin, dyMax, bothSides: upward || downward)
            };
        }

        public static KitTuning[] DefaultKits()
        {
            var list = new KitTuning[BrawlRoster.Count];
            for (int i = 0; i < list.Length; i++) list[i] = KitTuning.From(BrawlRoster.Kits[i]);
            return list;
        }

        /// <summary>True when the target's hurtbox overlaps the move box (positions are roots, X/Y plane).</summary>
        public static bool Overlaps(MoveBox box, Vector2 attackerRoot, float facingSign, Vector2 targetRoot)
        {
            float localX = (targetRoot.x - attackerRoot.x) * (facingSign < 0 ? -1 : 1);
            float dy = targetRoot.y - attackerRoot.y;
            return localX - HurtboxHalfWidth <= box.front && localX + HurtboxHalfWidth >= box.back &&
                   dy <= box.top && dy + HurtboxHeight >= box.bottom;
        }

        /// <summary>World-space rectangle of a move box (for drawing).</summary>
        public static Rect World(MoveBox box, Vector2 root, float facingSign)
        {
            float a = root.x + box.back * Mathf.Sign(facingSign == 0 ? 1 : facingSign), b = root.x + box.front * Mathf.Sign(facingSign == 0 ? 1 : facingSign);
            return Rect.MinMaxRect(Mathf.Min(a, b), root.y + box.bottom, Mathf.Max(a, b), root.y + box.top);
        }

        public static Rect Hurtbox(Vector2 root) => Rect.MinMaxRect(root.x - HurtboxHalfWidth, root.y, root.x + HurtboxHalfWidth, root.y + HurtboxHeight);

        /// <summary>Problems in the editable data; empty when it is safe to play with.</summary>
        public static List<string> Validate(BrawlSettings settings)
        {
            var problems = new List<string>();
            var moves = settings.sideMoves ?? Array.Empty<MoveDef>();
            foreach (SideMove id in Enum.GetValues(typeof(SideMove)))
            {
                int count = 0;
                foreach (var move in moves) if (move.id == id) count++;
                if (count == 0) problems.Add($"Falta el golpe {id}");
                if (count > 1) problems.Add($"Golpe {id} repetido");
            }
            foreach (var m in moves)
            {
                if (m.damage < 0 || m.damage > 40) problems.Add($"{m.id}: daño fuera de 0–40");
                if (m.windup < .02f || m.active < .02f || m.recovery < 0) problems.Add($"{m.id}: tiempos inválidos");
                if (m.windup + m.active + m.recovery > 2.5f) problems.Add($"{m.id}: duración total > 2,5 s");
                if (m.hitbox.Width <= .05f || m.hitbox.HeightSpan <= .05f) problems.Add($"{m.id}: caja de golpe vacía");
                if (Mathf.Abs(m.lift) > 20 || m.push < 0 || m.push > 30) problems.Add($"{m.id}: empuje/elevación fuera de rango");
            }
            var kits = settings.kitTuning ?? Array.Empty<KitTuning>();
            if (kits.Length != BrawlRoster.Count) problems.Add($"Ajustes de kit: {kits.Length} de {BrawlRoster.Count}");
            for (int i = 0; i < kits.Length; i++)
            {
                var k = kits[i];
                if (k == null) { problems.Add($"Kit {i}: vacío"); continue; }
                if (i < BrawlRoster.Count && k.kit != BrawlRoster.Kits[i].Name) problems.Add($"Kit {i}: nombre «{k.kit}» no coincide con {BrawlRoster.Kits[i].Name}");
                if (k.resistance < .5f || k.resistance > 2) problems.Add($"{k.kit}: resistencia fuera de 0,5–2");
                if (k.basicRange <= 0 || k.heavyRange <= 0 || k.specialRange <= 0 || k.superRange <= 0 || k.superHeight <= 0) problems.Add($"{k.kit}: alcance no positivo");
                if (k.heavyWindup <= 0 || k.specialWindup <= 0 || k.specialActive <= 0 || k.superWindup <= 0 || k.superActive <= 0) problems.Add($"{k.kit}: tiempos no positivos");
            }
            return problems;
        }
    }
}
