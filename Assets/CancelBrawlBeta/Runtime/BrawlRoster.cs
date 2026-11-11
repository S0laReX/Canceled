using UnityEngine;

namespace CancelBrawl.Beta
{
    public enum SpecialStyle { Plate, TagBurst, Dash, BounceToken, Emitter }
    public enum SuperStyle { Fan, Zone, GroundWave, Bubble, SafeSweep }

    /// <summary>Data that turns an archetype into a fighting kit (GDD §7–8). New fighters add an entry here.</summary>
    public sealed class BrawlKit
    {
        public string Name, Role, Strength, Weakness, HeavyName, SpecialName, RecoveryName, SuperName;
        public Color Accent;
        /// <summary>Divides received knockback. Below 1 means lighter (easier to launch).</summary>
        public float Resistance = 1;
        public float BasicRange = 2.1f, BasicCone = .25f;
        public float HeavyDamageOffset, HeavyPushScale = 1, HeavyWindup = .28f, HeavyRange = 2.8f;
        public SpecialStyle Special;
        public float SpecialDamageOffset, SpecialWindup = .23f, SpecialActive = .12f, SpecialRecovery = .31f;
        public float SpecialPush = 7.5f, SpecialLift = 5.5f, SpecialRange = 3;
        public Vector2 SpecialPreferredRange = new Vector2(3, 9);
        public float LungeSpeed;
        public SuperStyle Super;
        public float SuperDamageOffset, SuperWindup = .85f, SuperActive = .16f, SuperRange = 6, SuperHeight = 2.5f;
        public float SuperPush = 13, SuperLift = 9;
        public float RecoveryHorizontal = 4, RecoveryVertical = 1, RecoveryAirControl = 1, RecoveryHover;
        /// <summary>Model folders in Art/Characters this fighter can wear; the first is the default look.</summary>
        public string[] Skins = new string[0];
    }

    public static class BrawlRoster
    {
        public static readonly BrawlKit[] Kits =
        {
            new BrawlKit
            {
                Name = "Don Decreto", Skins = new[] { "The_Dapper_Tycoon", "Victoria_Vexley" },
                Role = "Control frontal", Accent = new Color(.22f, .93f, .93f),
                Strength = "Interrumpe rutas y disputa pasarelas.", Weakness = "Sus fuertes fallidos dejan exposición.",
                HeavyName = "Sellazo", SpecialName = "Decreto exprés", RecoveryName = "Subida en las encuestas", SuperName = "Mayoría absoluta",
                Resistance = 1.08f, BasicRange = 2.1f,
                HeavyDamageOffset = 1, HeavyPushScale = 1.1f, HeavyWindup = .34f, HeavyRange = 2.9f,
                Special = SpecialStyle.Plate, SpecialDamageOffset = 0,
                Super = SuperStyle.Fan, SuperDamageOffset = 0, SuperWindup = .85f, SuperActive = .16f, SuperRange = 6.5f, SuperHeight = 2.5f,
                RecoveryHorizontal = 4, RecoveryVertical = 1, RecoveryAirControl = .55f
            },
            new BrawlKit
            {
                Name = "Lola Loop", Skins = new[] { "Pink_Pop_Princess", "Ruby_Starlight", "Neon_Rebel" },
                Role = "Movilidad y presión aérea", Accent = new Color(1f, .33f, .65f),
                Strength = "Cambia ángulos y persigue lanzamientos.", Weakness = "Menor resistencia al empuje.",
                HeavyName = "Barrido frontal", SpecialName = "Ola de hate", RecoveryName = "Tendencia ascendente", SuperName = "Viralización masiva",
                Resistance = .9f, BasicRange = 1.95f,
                HeavyDamageOffset = -2, HeavyPushScale = .9f, HeavyWindup = .22f, HeavyRange = 3f,
                Special = SpecialStyle.TagBurst, SpecialDamageOffset = -2, SpecialWindup = .2f, SpecialActive = .1f, SpecialRecovery = .28f,
                SpecialPush = 6, SpecialLift = 5,
                Super = SuperStyle.Zone, SuperDamageOffset = -2, SuperWindup = .7f, SuperActive = .12f, SuperRange = 3.2f, SuperHeight = 2.5f,
                SuperPush = 12, SuperLift = 10,
                RecoveryHorizontal = 9, RecoveryVertical = .85f, RecoveryAirControl = 1.15f
            },
            new BrawlKit
            {
                Name = "Coach Tóxico", Skins = new[] { "Turbo_Max", "Commander_Vex" },
                Role = "Pelea cercana", Accent = new Color(1f, .62f, .18f),
                Strength = "Golpes de área corta y gran lanzamiento.", Weakness = "Necesita acercarse y comprometer su posición.",
                HeavyName = "Repetición cargada", SpecialName = "Serie al fallo", RecoveryName = "Día de pierna", SuperName = "Preentreno nuclear",
                Resistance = 1.12f, BasicRange = 1.75f,
                HeavyDamageOffset = 2, HeavyPushScale = 1.3f, HeavyWindup = .42f, HeavyRange = 2.5f,
                Special = SpecialStyle.Dash, SpecialDamageOffset = 2, SpecialWindup = .14f, SpecialActive = .24f, SpecialRecovery = .42f,
                SpecialPush = 10, SpecialLift = 6.5f, SpecialRange = 1.7f, LungeSpeed = 14, SpecialPreferredRange = new Vector2(1.5f, 4.2f),
                Super = SuperStyle.GroundWave, SuperDamageOffset = 2, SuperWindup = .7f, SuperActive = .55f, SuperRange = 7.5f, SuperHeight = .8f,
                SuperPush = 14, SuperLift = 10.5f,
                RecoveryHorizontal = 2.2f, RecoveryVertical = 1.18f, RecoveryAirControl = .35f
            },
            new BrawlKit
            {
                Name = "Cripto King", Skins = new[] { "Smirking_Tycoon", "The_Regal_Duelist" },
                Role = "Proyectiles y zonas", Accent = new Color(1f, .82f, .25f),
                Strength = "Obliga a cambiar de ruta y controla accesos.", Weakness = "Responde peor a la presión cercana.",
                HeavyName = "Flecha de mercado", SpecialName = "Token rebote", RecoveryName = "A la luna", SuperName = "Burbuja especulativa",
                Resistance = .97f, BasicRange = 1.9f,
                HeavyDamageOffset = 0, HeavyPushScale = 1.05f, HeavyWindup = .26f, HeavyRange = 3.1f,
                Special = SpecialStyle.BounceToken, SpecialDamageOffset = -1, SpecialWindup = .26f, SpecialActive = .1f, SpecialRecovery = .3f,
                SpecialPush = 7, SpecialLift = 5, SpecialPreferredRange = new Vector2(3, 11),
                Super = SuperStyle.Bubble, SuperDamageOffset = 0, SuperWindup = 1.2f, SuperActive = .15f, SuperRange = 3.6f, SuperHeight = 3.6f,
                SuperPush = 13, SuperLift = 11,
                RecoveryHorizontal = 7.5f, RecoveryVertical = .95f, RecoveryAirControl = .3f
            },
            new BrawlKit
            {
                Name = "Tío Antena", Skins = new[] { "Tin_Foil_Fred", "Neon_Mischief" },
                Role = "Trampas y desvíos", Accent = new Color(.55f, 1f, .45f),
                Strength = "Prepara rutas incómodas y ataques angulados.", Weakness = "Sus dispositivos son visibles y requieren preparación.",
                HeavyName = "Antena dirigida", SpecialName = "Interferencia", RecoveryName = "Me están llevando", SuperName = "La señal definitiva",
                Resistance = 1f, BasicRange = 2.35f, BasicCone = .05f,
                HeavyDamageOffset = 0, HeavyPushScale = 1f, HeavyWindup = .36f, HeavyRange = 3.2f,
                Special = SpecialStyle.Emitter, SpecialDamageOffset = -1, SpecialWindup = .3f, SpecialActive = .05f, SpecialRecovery = .3f,
                SpecialPush = 2.5f, SpecialLift = 15, SpecialPreferredRange = new Vector2(1.5f, 5),
                Super = SuperStyle.SafeSweep, SuperDamageOffset = -1, SuperWindup = .9f, SuperActive = .2f, SuperRange = 7, SuperHeight = 2.2f,
                SuperPush = 12.5f, SuperLift = 9,
                RecoveryHorizontal = 5, RecoveryVertical = .75f, RecoveryAirControl = 1, RecoveryHover = .55f
            }
        };

        public static int Count => Kits.Length;
        public static BrawlKit Get(int index) => Kits[((index % Kits.Length) + Kits.Length) % Kits.Length];
        public static int SkinCount(int character) => Mathf.Max(1, Get(character).Skins.Length);
        public static int WrapSkin(int character, int skin) { int n = SkinCount(character); return ((skin % n) + n) % n; }
        /// <summary>Readable look name, e.g. "The Dapper Tycoon"; "Provisional" when the kit has no model.</summary>
        public static string SkinName(int character, int skin)
        {
            BrawlKit kit = Get(character);
            return kit.Skins.Length == 0 ? "Provisional" : kit.Skins[WrapSkin(character, skin)].Replace('_', ' ');
        }
    }
}
