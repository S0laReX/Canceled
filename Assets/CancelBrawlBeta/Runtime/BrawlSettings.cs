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
        public float Duration => Windup + Active + Recovery;
        public AttackSpec(AttackKind kind, float damage, float windup, float active, float recovery, float range, float push, float lift, float height)
        { Kind = kind; Damage = damage; Windup = windup; Active = active; Recovery = recovery; Range = range; Push = push; Lift = lift; Height = height; }
    }

    public struct BrawlCommand
    {
        public Vector2 Move, Aim;
        public bool Jump, Basic, Heavy, Special, Recovery, Dodge, Super, FastFall;
    }
}
