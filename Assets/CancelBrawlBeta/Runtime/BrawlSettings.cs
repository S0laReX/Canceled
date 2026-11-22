using System.Collections.Generic;
using UnityEngine;

namespace CancelBrawl.Beta
{
    public enum AttackKind { Basic, Heavy, Special, Super }
    public enum MatchPhase { Menu, Countdown, Playing, Paused, Results }

    [CreateAssetMenu(menuName = "Cancel Brawl/Beta balance")]
    public sealed class BrawlSettings : ScriptableObject
    {
        [Header("Reglas provisionales")]
        [Min(1)] public int startingStocks = 3;
        [Min(10)] public float matchSeconds = 300;
        public float respawnSeconds = 2;
        public float protectionSeconds = 2;
        [Tooltip("Ventana para atribuir una expulsión al último atacante (GDD §10).")]
        public float attributionSeconds = 8;
        [Header("Movimiento")]
        public float moveSpeed = 7;
        public float airControl = .72f;
        public float jumpSpeed = 10.8f;
        public float gravity = 25;
        public float recoverySpeed = 13.8f;
        public float dodgeSpeed = 15;
        public float dodgeCooldown = .9f;
        [Header("Combate (base; cada kit aplica su desplazamiento)")]
        public float knockbackPerDamage = .105f;
        public float maxKnockback = 28;
        public float basicDamage = 5;
        public float heavyDamage = 12;
        public float specialDamage = 10;
        public float superDamage = 22;
        [Header("Presentación")]
        [Tooltip("Material URP transparente usado para atenuar obstáculos que tapan a un luchador.")]
        public Material occluderMaterial;
        [Tooltip("Modelos importados de cada luchador (Art/Characters). Sin catálogo se usan primitivas.")]
        public BrawlCharacterArt characterArt;
        [Tooltip("Escenarios de producción, uno por cancha (mismo orden). Solo los usan las escenas de producción; un hueco vacío deja la cancha gris de la beta.")]
        public GameObject[] stagePrefabs;

        [Header("Golpes y kits (SYS-011) · datos editables con validación")]
        [Tooltip("Golpes direccionales 2.5D: tiempos, daño, empuje y caja de golpe local (X hacia donde mira, Y desde los pies).")]
        public MoveDef[] sideMoves = BrawlMoveData.DefaultSideMoves();
        [Tooltip("Valores numéricos de cada kit, en el orden del roster. Los nombres, estilos y arte siguen en BrawlRoster.")]
        public KitTuning[] kitTuning = BrawlMoveData.DefaultKits();
        [Tooltip("2.5D: lanzamiento K = (base + crecimiento·%/100) / resistencia con ángulo por golpe (GDD 2.0 §5). Desactivado = fórmula histórica.")]
        public bool angledLaunch = true;

        /// <summary>Data for a directional move; falls back to the built-in default if the entry is missing.</summary>
        public MoveDef SideMoveDef(SideMove id)
        {
            if (sideMoves != null) foreach (var move in sideMoves) if (move.id == id) return move;
            foreach (var move in BrawlMoveData.DefaultSideMoves()) if (move.id == id) return move;
            throw new System.ArgumentOutOfRangeException(nameof(id));
        }

        /// <summary>Copies the editable kit numbers onto the roster when the data is valid; otherwise keeps the built-in values.</summary>
        public List<string> ApplyKitTuning()
        {
            var problems = BrawlMoveData.Validate(this);
            if (problems.Count > 0) { Debug.LogWarning("FUNADOS: datos de golpes/kits no válidos, se usan los valores incorporados: " + string.Join("; ", problems)); return problems; }
            for (int i = 0; i < kitTuning.Length; i++) kitTuning[i].ApplyTo(BrawlRoster.Kits[i]);
            return problems;
        }

        private void OnValidate()
        {
            // Keep hit boxes non-empty while editing; the Range attributes bound every other number.
            if (sideMoves == null) return;
            for (int i = 0; i < sideMoves.Length; i++)
            {
                var box = sideMoves[i].hitbox;
                if (box.front < box.back + .05f) box.front = box.back + .05f;
                if (box.top < box.bottom + .05f) box.top = box.bottom + .05f;
                sideMoves[i].hitbox = box;
            }
        }

        public AttackSpec Attack(AttackKind kind, int character, int combo) => Attack(kind, BrawlRoster.Get(character), combo);

        public AttackSpec Attack(AttackKind kind, BrawlKit kit, int combo)
        {
            switch (kind)
            {
                case AttackKind.Heavy:
                    return new AttackSpec(kind, heavyDamage + kit.HeavyDamageOffset, kit.HeavyWindup, .13f, .36f,
                        kit.HeavyRange, 9.2f * kit.HeavyPushScale, 7.2f, 1.45f);
                case AttackKind.Special:
                    return new AttackSpec(kind, specialDamage + kit.SpecialDamageOffset, kit.SpecialWindup, kit.SpecialActive,
                        kit.SpecialRecovery, kit.SpecialRange, kit.SpecialPush, kit.SpecialLift, 1.4f);
                case AttackKind.Super:
                    return new AttackSpec(kind, superDamage + kit.SuperDamageOffset, kit.SuperWindup, kit.SuperActive, .5f,
                        kit.SuperRange, kit.SuperPush, kit.SuperLift, kit.SuperHeight);
                default:
                    return new AttackSpec(kind, basicDamage + (combo == 2 ? 2 : 0), .08f, .1f, .16f, kit.BasicRange,
                        combo == 2 ? 6 : 3.6f, combo == 2 ? 5 : 3.4f, 1.45f);
            }
        }
    }

    public readonly struct AttackSpec
    {
        public readonly AttackKind Kind;
        public readonly float Damage, Windup, Active, Recovery, Range, Push, Lift, Height;
        /// <summary>Explicit launch (GDD 2.0 §5): base, growth per 100 % and angle in degrees. Unset = derived from Push/Lift.</summary>
        public readonly float LaunchBase, LaunchGrowth, LaunchAngle;
        public readonly bool HasLaunch;
        public float Duration => Windup + Active + Recovery;
        public AttackSpec(AttackKind kind, float damage, float windup, float active, float recovery, float range, float push, float lift, float height)
        {
            Kind = kind; Damage = damage; Windup = windup; Active = active; Recovery = recovery; Range = range; Push = push; Lift = lift; Height = height;
            LaunchBase = LaunchGrowth = LaunchAngle = 0; HasLaunch = false;
        }
        private AttackSpec(AttackSpec s, float launchBase, float growth, float angle)
        {
            Kind = s.Kind; Damage = s.Damage; Windup = s.Windup; Active = s.Active; Recovery = s.Recovery; Range = s.Range; Push = s.Push; Lift = s.Lift; Height = s.Height;
            LaunchBase = launchBase; LaunchGrowth = growth; LaunchAngle = angle; HasLaunch = true;
        }
        public AttackSpec WithLaunch(float launchBase, float growth, float angle) => new AttackSpec(this, launchBase, growth, angle);
    }

    /// <summary>
    /// Launch formula of GDD 2.0 §5: K = (B + G · P / 100) / resistance, sent at angle θ (X toward the hit direction,
    /// Y up). Defaults are derived from the historical push/lift so a move keeps its 0 % angle and its 0 %/100 % strength.
    /// </summary>
    public static class BrawlLaunch
    {
        /// <summary>Historical vertical growth per % (lift + 0.018 · P).</summary>
        public const float LegacyLiftGrowth = .018f;

        public static void Derive(float push, float lift, float knockbackPerDamage, out float launchBase, out float growth, out float angle)
        {
            launchBase = Mathf.Sqrt(push * push + lift * lift);
            float px = push + 100 * knockbackPerDamage, py = lift + 100 * LegacyLiftGrowth;
            growth = Mathf.Max(0, Mathf.Sqrt(px * px + py * py) - launchBase);
            angle = Mathf.Abs(push) < 1e-4f && Mathf.Abs(lift) < 1e-4f ? 0 : Mathf.Atan2(lift, push) * Mathf.Rad2Deg;
        }

        public static void Resolve(AttackSpec spec, BrawlSettings settings, out float launchBase, out float growth, out float angle)
        {
            if (spec.HasLaunch) { launchBase = spec.LaunchBase; growth = spec.LaunchGrowth; angle = spec.LaunchAngle; return; }
            Derive(spec.Push, spec.Lift, settings.knockbackPerDamage, out launchBase, out growth, out angle);
        }

        /// <summary>Launch velocity in X/Y for a hit at <paramref name="percentAfter"/>; X is toward <paramref name="directionSign"/>.</summary>
        public static Vector2 Velocity(AttackSpec spec, BrawlSettings settings, float percentAfter, float resistance, float directionSign)
        {
            Resolve(spec, settings, out float b, out float g, out float angle);
            float k = Mathf.Min(settings.maxKnockback, (b + g * percentAfter / 100f) / Mathf.Max(.01f, resistance));
            float rad = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad) * k * (directionSign < 0 ? -1 : 1), Mathf.Min(16, Mathf.Sin(rad) * k));
        }
    }

    public struct BrawlCommand
    {
        public Vector2 Move, Aim;
        public bool Jump, Basic, Heavy, Special, Recovery, Dodge, Super, FastFall;
    }
}
