using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// Replaceable greyboxes of the four GDD courts (§9). All gameplay surfaces use solid 3D colliders.
    /// Metrics: single jump ≈ 2.3 m high / 4 m long, double jump ≈ 4.3 m high. Platforms are static
    /// (competitive versions have no moving parts, destruction or random hazards).
    /// </summary>
    public sealed class BrawlArena : MonoBehaviour
    {
        public static readonly string[] Names = { "Azotea Neón", "Mall del Ego", "Campus en Llamas", "Núcleo Cripto" };
        public static readonly string[] Descriptions =
        {
            "Disputa el cruce o arriesga un salto lateral.",
            "Rodea el kiosco o busca ventaja desde las pasarelas.",
            "Controla las terrazas sin perder la escapatoria.",
            "Camino seguro o salto corto junto al vacío."
        };
        private static readonly string[] Calls = { "¡A LA AZOTEA!", "¡AL MALL!", "¡AL CAMPUS!", "¡AL NÚCLEO!" };
        public static int Count => Names.Length;

        // Production stages (FunadosProduction/Environments) that replace the greyboxes slot by slot.
        public static readonly string[] StageNames = { "Azotea Influencer", "Torre Corporativa", "Estudio de Debate", "Plaza Colonial" };
        public static readonly string[] StageDescriptions =
        {
            "Tarima central y dos plataformas flotantes.",
            "Atrio ejecutivo con dos plataformas colgantes.",
            "Plató central y dos plataformas simétricas.",
            "Tarima de piedra entre dos balcones."
        };
        private static readonly string[] StageCalls = { "¡A LA AZOTEA!", "¡A LA TORRE!", "¡AL DEBATE!", "¡A LA PLAZA!" };
        public static readonly Color DefaultBackdrop = new Color(0.025f, 0.045f, 0.085f);

        public static string NameOf(int layout, bool stageArt) => (stageArt ? StageNames : Names)[layout];
        public static string DescriptionOf(int layout, bool stageArt) => (stageArt ? StageDescriptions : Descriptions)[layout];

        private const string GeneratedName = "Arena geometry";
        private Vector3[] spawns = new Vector3[0];

        public int Layout { get; private set; }
        public bool IsSideView { get; private set; }
        public float UpperLimit => IsSideView ? 20 : float.PositiveInfinity;

        /// <summary>Distance from a position to the nearest KO limit (left/right, bottom, top in 2.5D) (SYS-039).</summary>
        public float DistanceToKoLimit(Vector3 p)
        {
            float side = OuterLimits.x - Mathf.Abs(p.x), below = p.y - LowerLimit;
            float above = IsSideView ? UpperLimit - p.y : float.PositiveInfinity;
            return Mathf.Min(side, Mathf.Min(below, above));
        }
        public Collider[] OneWayPlatforms { get; private set; } = new Collider[0];
        /// <summary>True when this court was built from a production stage prefab instead of greyboxes.</summary>
        public bool UsesStageArt { get; private set; }
        /// <summary>Clear colour the group camera should use behind this court.</summary>
        public Color Backdrop { get; private set; } = DefaultBackdrop;
        public string DisplayName => NameOf(Layout, UsesStageArt);
        public string StartCall => (UsesStageArt ? StageCalls : Calls)[Layout];
        public Vector3[] SpawnPoints => spawns;
        /// <summary>Half extents (x, z) of the side elimination volumes.</summary>
        public Vector2 OuterLimits { get; private set; } = new Vector2(23, 19);
        /// <summary>Height of the lower elimination volume.</summary>
        public float LowerLimit { get; private set; } = -9;
        /// <summary>Half extents (x, z) the group camera may follow.</summary>
        public Vector2 CameraClamp { get; private set; } = new Vector2(14, 11);

        private Material surface, bridge, cyan, pink, white, dark, warm;

        public Vector3 SafePoint(Vector3 from)
        {
            Vector3 nearest = spawns.Length > 0 ? spawns[0] : Vector3.zero;
            float distance = float.PositiveInfinity;
            foreach (Vector3 point in spawns)
            {
                float candidate = (point - from).sqrMagnitude;
                if (candidate >= distance) continue;
                nearest = point;
                distance = candidate;
            }
            return nearest;
        }

        [ContextMenu("Build beta arena")]
        public void Build() => Build(Layout);

        /// <param name="stage">Production stage prefab for this slot; null builds the beta greybox.</param>
        public void Build(int layout, GameObject stage = null, bool sideView = false)
        {
            IsSideView = sideView;
            Layout = Mathf.Clamp(layout, 0, Count - 1);
            UsesStageArt = stage != null;
            Backdrop = DefaultBackdrop;
            gameObject.name = UsesStageArt ? DisplayName : DisplayName + " · beta";
            Transform old = transform.Find(GeneratedName);
            if (old)
            {
                old.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(old.gameObject);
                else DestroyImmediate(old.gameObject);
            }

            Transform root = new GameObject(GeneratedName).transform;
            root.SetParent(transform, false);
            if (UsesStageArt) { BuildStage(root, stage); if (sideView) AdaptSideStage(root); return; }
            if (sideView) { BuildSideGreybox(root); return; }
            switch (Layout)
            {
                case 1: BuildMall(root); break;
                case 2: BuildCampus(root); break;
                case 3: BuildCore(root); break;
                default: BuildRooftop(root); break;
            }
        }

        private void SideMetrics()
        {
            OuterLimits = new Vector2(22, 1); LowerLimit = -10; CameraClamp = new Vector2(19, 1);
            spawns = new[] { P(-4, 0, 0), P(4, 0, 0), P(-6, 0, 0), P(6, 0, 0), P(0, 0, 0) };
        }

        private void BuildSideGreybox(Transform root)
        {
            SideMetrics(); Palette(new Color(.12f,.17f,.27f), new Color(.2f,.9f,1), new Color(1,.3f,.6f));
            Platform(root, "Side main", Vector3.zero, new Vector3(16,.8f,3), surface, cyan);
            Platform(root, "Side left", new Vector3(-4.8f,2.6f,0), new Vector3(3.8f,.3f,3), bridge, pink);
            Platform(root, "Side right", new Vector3(4.8f,2.6f,0), new Vector3(3.8f,.3f,3), bridge, pink);
            OneWayPlatforms = new[] { root.Find("Side left").GetComponent<Collider>(), root.Find("Side right").GetComponent<Collider>() };
            AddLight(root, Color.white, new Color(.5f,.55f,.65f), DefaultBackdrop);
        }

        private void AdaptSideStage(Transform root)
        {
            SideMetrics(); var elevated = new List<Collider>();
            foreach (var child in root.GetComponentsInChildren<Transform>())
                if (child.name == "Back floor" || child.name == "Back floor trim") child.gameObject.SetActive(false);
            // Preserve the source prefab: adapt only this runtime instance.
            foreach (var collider in root.GetComponentsInChildren<Collider>())
            {
                bool combat = collider.transform.parent && collider.transform.parent.name == "Combat surfaces";
                collider.enabled = combat;
                if (!combat || collider.name == "Main platform") continue;
                float top = collider.bounds.max.y;
                Vector3 p = collider.transform.position;
                collider.transform.position = new Vector3(p.x < 0 ? -4.8f : 4.8f, p.y + 2.6f - top, 0);
                // One-way platforms only need their top surface. A thick body (Plaza Colonial's balcony reached down to
                // 0.4 m) would act as an invisible wall for line-of-sight and projectile checks of fighters below it.
                if (collider is BoxCollider box) ThinToTop(box, OneWayThickness);
                elevated.Add(collider);
            }
            OneWayPlatforms = elevated.ToArray();
        }

        /// <summary>Collision thickness of a one-way platform in 2.5D (its top stays where it was).</summary>
        public const float OneWayThickness = .3f;

        private static void ThinToTop(BoxCollider box, float worldThickness)
        {
            float scale = Mathf.Max(1e-4f, Mathf.Abs(box.transform.lossyScale.y));
            float localTop = box.center.y + box.size.y * .5f;
            float localHeight = Mathf.Min(box.size.y, worldThickness / scale);
            box.size = new Vector3(box.size.x, localHeight, box.size.z);
            box.center = new Vector3(box.center.x, localTop - localHeight * .5f, box.center.z);
        }

        // ───────────────────────── Production stages ─────────────────────────
        /// <summary>
        /// The four stage prefabs share one footprint: a 16 × 11 m main platform with its top at y = 0 and
        /// two side platforms at x = ±11. Their solid surfaces carry their own box colliders. Behind the
        /// main platform a solid floor one step lower ends in an invisible wall (z ≈ 7.5), so the back is
        /// not a ledge: fighters only fall off the front and the sides. Scenery past that wall is visual.
        /// </summary>
        private void BuildStage(Transform root, GameObject stage)
        {
            Instantiate(stage, root, false).name = stage.name;
            OuterLimits = new Vector2(23, 19); LowerLimit = -9; CameraClamp = new Vector2(14, 11);
            spawns = new[] { P(-5, 0, -2), P(5, 0, -2), P(-3, 0, 3), P(3, 0, 3), P(0, 0, 0) };
            switch (Layout)
            {
                case 1: // Torre Corporativa
                    Backdrop = new Color(.16f, .12f, .2f);
                    AddLight(root, new Color(1f, .95f, .88f), new Color(.5f, .46f, .52f), Backdrop);
                    break;
                case 2: // Estudio de Debate
                    Backdrop = new Color(.05f, .06f, .12f);
                    AddLight(root, new Color(.85f, .9f, 1f), new Color(.4f, .42f, .55f), Backdrop);
                    break;
                case 3: // Plaza Colonial: pale stone washes out under the default key light.
                    Backdrop = new Color(.45f, .72f, .95f);
                    AddLight(root, new Color(1f, .96f, .85f), new Color(.45f, .45f, .48f), Backdrop, 1.2f);
                    break;
                default: // Azotea Influencer
                    Backdrop = new Color(.2f, .14f, .38f);
                    AddLight(root, new Color(1f, .85f, .95f), new Color(.5f, .42f, .6f), Backdrop);
                    break;
            }
        }

        // ───────────────────────── Azotea Neón ─────────────────────────
        private void BuildRooftop(Transform root)
        {
            Palette(new Color(.12f, .17f, .24f), new Color(.12f, .92f, 1f), new Color(1f, .22f, .63f));
            OuterLimits = new Vector2(23, 19); LowerLimit = -9; CameraClamp = new Vector2(14, 11);
            spawns = new[] { P(-6, 0, -2), P(6, 0, -2), P(-4, 0, 4), P(4, 0, 4), P(0, 0, 0) };

            Platform(root, "West rooftop", new Vector3(-6f, 0f, 0f), new Vector3(10f, 0.8f, 13f), surface, cyan);
            Platform(root, "East rooftop", new Vector3(6f, 0f, 0f), new Vector3(10f, 0.8f, 13f), surface, pink);
            Platform(root, "Central crossing", Vector3.zero, new Vector3(3.6f, 0.7f, 4.8f), bridge, white);
            for (int side = -1; side <= 1; side += 2)
            {
                Material accent = side < 0 ? cyan : pink;
                Platform(root, side < 0 ? "West raised terrace" : "East raised terrace",
                    new Vector3(side * 7.4f, 1.25f, 2.6f), new Vector3(3.8f, 0.55f, 3.4f), bridge, accent);
                foreach (int end in new[] { -1, 1 })
                    Platform(root, "Lower return " + side + " " + end,
                        new Vector3(side * 5.4f, -1.35f, end * 8.35f), new Vector3(5.8f, 0.6f, 2.9f), bridge, accent);
                // Dark fascia clarifies that these are rooftops without pretending to be a railing.
                Box(root, "Facade " + side, new Vector3(side * 6f, -3.55f, 0f), new Vector3(9.8f, 5.5f, 12.8f), dark, false);
                for (int stripe = 0; stripe < 4; stripe++)
                    Box(root, "Crossing chevron", new Vector3(side * (1.9f + stripe * 0.55f), 0.018f, -0.35f),
                        new Vector3(0.18f, 0.025f, 1.3f), accent, false);
            }
            Platform(root, "North return island", new Vector3(0f, -1.35f, 8.35f), new Vector3(3.4f, 0.6f, 2.9f), bridge, white);
            Platform(root, "South return island", new Vector3(0f, -1.35f, -8.35f), new Vector3(3.4f, 0.6f, 2.9f), bridge, white);
            Skyline(root, 24f);
            AddLight(root, new Color(0.79f, 0.87f, 1f), new Color(0.38f, 0.44f, 0.55f), new Color(0.025f, 0.045f, 0.085f));
        }

        // ───────────────────────── Mall del Ego ─────────────────────────
        private void BuildMall(Transform root)
        {
            Palette(new Color(.26f, .22f, .27f), new Color(1f, .45f, .78f), new Color(.98f, .8f, .3f));
            OuterLimits = new Vector2(21, 17); LowerLimit = -9; CameraClamp = new Vector2(13, 10);
            spawns = new[] { P(-5, 0, -2.5f), P(5, 0, -2.5f), P(-5, 0, 3), P(5, 0, 3), P(0, 1.8f, 7.4f), P(0, 1.3f, 0) };

            Platform(root, "Atrium floor", Vector3.zero, new Vector3(16f, 0.8f, 11f), surface, white);
            Platform(root, "Ego kiosk", new Vector3(0f, 1.3f, 0f), new Vector3(3f, 1.3f, 3f), bridge, pink);
            for (int side = -1; side <= 1; side += 2)
            {
                Material accent = side < 0 ? cyan : pink;
                Platform(root, side < 0 ? "South mezzanine walkway" : "North mezzanine walkway",
                    new Vector3(0f, 1.8f, side * 7.4f), new Vector3(18f, 0.5f, 2.8f), bridge, accent);
                Platform(root, side < 0 ? "West mid platform" : "East mid platform",
                    new Vector3(side * 11.2f, 1f, 0f), new Vector3(3.6f, 0.5f, 4.5f), bridge, accent);
                foreach (int end in new[] { -1, 1 })
                    Platform(root, "Lower return " + side + " " + end,
                        new Vector3(side * 10.5f, -1.4f, end * 6.6f), new Vector3(3f, 0.5f, 2.4f), bridge, white);
                for (int column = -1; column <= 1; column++)
                    Box(root, "Mezzanine column", new Vector3(column * 6f, -1.4f, side * 7.4f), new Vector3(.5f, 5.4f, .5f), dark, false);
            }
            Box(root, "Atrium base", new Vector3(0f, -3.4f, 0f), new Vector3(15.6f, 5f, 10.6f), dark, false);
            for (int i = 0; i < 4; i++)
                Box(root, "Atrium chevron", new Vector3(-3.6f + i * 2.4f, 0.018f, -3.9f), new Vector3(1.2f, 0.025f, 0.16f), cyan, false);
            // Storefronts: decorative backdrop with neon signs (no colliders).
            for (int index = 0; index < 9; index++)
            {
                float x = -24f + index * 6f;
                Box(root, "Storefront", new Vector3(x, 1f, 16f), new Vector3(5.4f, 9f, 2f), dark, false);
                Box(root, "Store sign", new Vector3(x, 4.2f, 14.9f), new Vector3(3.6f, .5f, .05f), index % 3 == 0 ? pink : index % 3 == 1 ? cyan : warm, false);
            }
            AddLight(root, new Color(1f, .9f, .86f), new Color(.45f, .4f, .48f), new Color(.07f, .05f, .09f));
        }

        // ───────────────────────── Campus en Llamas ─────────────────────────
        private void BuildCampus(Transform root)
        {
            Palette(new Color(.25f, .21f, .18f), new Color(1f, .5f, .18f), new Color(.35f, .8f, 1f));
            OuterLimits = new Vector2(25, 18); LowerLimit = -9; CameraClamp = new Vector2(15, 11);
            spawns = new[] { P(-4, 0, -2), P(4, 0, -2), P(-10.25f, 2f, 2.5f), P(10.25f, 2f, 2.5f), P(0, 0, -7.4f), P(-3.5f, 0, 3.5f) };

            Platform(root, "Central plaza", Vector3.zero, new Vector3(13f, 0.8f, 11f), surface, white);
            Platform(root, "Front walk", new Vector3(0f, 0f, -7.4f), new Vector3(28f, 0.8f, 3.8f), surface, cyan);
            Platform(root, "Founder statue", new Vector3(0f, 2.4f, 2.2f), new Vector3(1.2f, 2.4f, 1.2f), bridge, cyan);
            for (int side = -1; side <= 1; side += 2)
            {
                Platform(root, side < 0 ? "West terrace" : "East terrace",
                    new Vector3(side * 10.25f, 2f, 1f), new Vector3(5.5f, 0.6f, 8f), bridge, cyan);
                Ramp(root, side < 0 ? "West stairway" : "East stairway",
                    new Vector3(side * 10.25f, 0f, -7.4f), new Vector3(side * 10.25f, 2f, -3f), 2.6f, bridge, white);
                Platform(root, "Side return " + side, new Vector3(side * 16f, -1.2f, 1f), new Vector3(3f, 0.5f, 3f), bridge, white);
                Box(root, "Terrace pillar", new Vector3(side * 10.25f, -1.5f, 1f), new Vector3(4.6f, 6.4f, 7f), dark, false);
                // Banners named after the chaos, not fire damage.
                Box(root, "Protest banner", new Vector3(side * 12.6f, 3.6f, 4.9f), new Vector3(.08f, 1.6f, 1.2f), cyan, false);
            }
            Platform(root, "North return", new Vector3(0f, -1.2f, 8.3f), new Vector3(4f, 0.5f, 2.6f), bridge, white);
            Box(root, "Plaza base", new Vector3(0f, -3.4f, -1f), new Vector3(12.6f, 5f, 12.4f), dark, false);
            for (int index = 0; index < 8; index++)
            {
                float x = -25f + index * 7f;
                float height = 7f + (index * 5) % 6;
                Box(root, "Faculty building", new Vector3(x, -6f + height * .5f, 20f), new Vector3(6f, height, 4f), dark, false);
                Box(root, "Burning window", new Vector3(x, -3f + (index % 3), 17.95f), new Vector3(2.4f, .5f, .05f), cyan, false);
            }
            AddLight(root, new Color(1f, .82f, .64f), new Color(.46f, .38f, .34f), new Color(.09f, .045f, .03f));
        }

        // ───────────────────────── Núcleo Cripto ─────────────────────────
        private void BuildCore(Transform root)
        {
            Palette(new Color(.13f, .15f, .2f), new Color(1f, .82f, .25f), new Color(.3f, 1f, .62f));
            OuterLimits = new Vector2(22, 19); LowerLimit = -9; CameraClamp = new Vector2(13, 12);
            spawns = new[] { P(0, .5f, 0), P(-10, 0, 0), P(10, 0, 0), P(0, 0, 8.5f), P(0, 0, -8.5f) };

            Platform(root, "Dispute hub", new Vector3(0f, .5f, 0f), new Vector3(6f, 0.6f, 6f), surface, cyan);
            for (int side = -1; side <= 1; side += 2)
            {
                Platform(root, side < 0 ? "West ledger" : "East ledger", new Vector3(side * 10f, 0f, 0f), new Vector3(5f, 0.6f, 7f), bridge, pink);
                Platform(root, side < 0 ? "South ledger" : "North ledger", new Vector3(0f, 0f, side * 8.5f), new Vector3(7f, 0.6f, 3.5f), bridge, pink);
                // Displaced L-shaped connections: the long, safe route between ledgers.
                foreach (int end in new[] { -1, 1 })
                {
                    Platform(root, "Chain link A", new Vector3(side * 10f, 0f, end * 6.4f), new Vector3(1.6f, 0.4f, 5.8f), bridge, white);
                    Platform(root, "Chain link B", new Vector3(side * 7.15f, 0f, end * 8.5f), new Vector3(7.3f, 0.4f, 1.6f), bridge, white);
                }
            }
            Box(root, "Hub reactor", new Vector3(0f, -3f, 0f), new Vector3(2.4f, 6f, 2.4f), cyan, false);
            for (int index = 0; index < 16; index++)
            {
                float angle = index * Mathf.PI * 2f / 16f;
                float radius = 26f + (index % 3) * 4f;
                Transform crystal = Box(root, "Floating block", new Vector3(Mathf.Cos(angle) * radius, -4f + (index * 3) % 7, Mathf.Sin(angle) * radius + 6f),
                    Vector3.one * (1.2f + (index % 4) * .5f), index % 2 == 0 ? dark : bridge, false).transform;
                crystal.localRotation = Quaternion.Euler(index * 23f, index * 41f, index * 17f);
            }
            AddLight(root, new Color(.78f, .95f, .86f), new Color(.34f, .4f, .42f), new Color(.02f, .05f, .05f));
        }

        // ───────────────────────── Helpers ─────────────────────────
        private Vector3 P(float x, float top, float z) => transform.TransformPoint(new Vector3(x, top + 0.12f, z));

        private void Palette(Color floor, Color accentA, Color accentB)
        {
            surface = Material("Floor", floor);
            bridge = Material("Bridge", Color.Lerp(floor, Color.white, .12f));
            cyan = Material("Accent A", accentA, true);
            pink = Material("Accent B", accentB, true);
            white = Material("Wayfinding", new Color(0.72f, 0.83f, 0.88f), true);
            dark = Material("Walls", new Color(0.035f, 0.055f, 0.095f));
            warm = Material("Warm sign", new Color(1f, .62f, .2f), true);
        }

        private void Skyline(Transform root, float z0)
        {
            for (int index = 0; index < 14; index++)
            {
                float x = -37f + index * 5.7f;
                float height = 6f + ((index * 7) % 9);
                float z = z0 + (index % 3) * 3f;
                Box(root, "Skyline " + index, new Vector3(x, -13f + height * 0.5f, z), new Vector3(3.5f + index % 2, height, 4f), dark, false);
                for (int row = 0; row < 3; row++)
                    Box(root, "Distant window", new Vector3(x, -10.5f + row * 1.9f, z - 2.02f),
                        new Vector3(1.8f, 0.13f, 0.025f), index % 2 == 0 ? cyan : pink, false);
            }
        }

        private static void Platform(Transform root, string name, Vector3 top, Vector3 size, Material surface, Material edge)
        {
            Vector3 center = top - Vector3.up * (size.y * 0.5f);
            Box(root, name, center, size, surface, true);
            const float width = 0.065f;
            float y = top.y + 0.018f;
            Box(root, name + " front edge", new Vector3(top.x, y, top.z - size.z * 0.5f + width), new Vector3(size.x, 0.026f, width), edge, false);
            Box(root, name + " back edge", new Vector3(top.x, y, top.z + size.z * 0.5f - width), new Vector3(size.x, 0.026f, width), edge, false);
            Box(root, name + " left edge", new Vector3(top.x - size.x * 0.5f + width, y, top.z), new Vector3(width, 0.026f, size.z), edge, false);
            Box(root, name + " right edge", new Vector3(top.x + size.x * 0.5f - width, y, top.z), new Vector3(width, 0.026f, size.z), edge, false);
        }

        /// <summary>Solid ramp whose walkable top runs from <paramref name="low"/> to <paramref name="high"/>, with step stripes.</summary>
        private static void Ramp(Transform root, string name, Vector3 low, Vector3 high, float width, Material surface, Material steps)
        {
            const float thickness = 0.4f;
            Vector3 direction = high - low;
            Quaternion rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            Vector3 normal = rotation * Vector3.up;
            Vector3 center = (low + high) * .5f - normal * thickness * .5f;
            Box(root, name, center, new Vector3(width, thickness, direction.magnitude), surface, true).transform.localRotation = rotation;
            const int count = 7;
            for (int i = 1; i < count; i++)
            {
                Vector3 point = Vector3.Lerp(low, high, i / (float)count) + normal * .015f;
                Box(root, name + " step", point, new Vector3(width, .02f, .08f), steps, false).transform.localRotation = rotation;
            }
        }

        private static GameObject Box(Transform root, string name, Vector3 position, Vector3 scale, Material material, bool solid)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(root, false);
            box.transform.localPosition = position;
            box.transform.localScale = scale;
            MeshRenderer renderer = box.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = solid ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = solid;
            if (!solid)
            {
                Collider collider = box.GetComponent<Collider>();
                collider.enabled = false;
                if (Application.isPlaying) Destroy(collider);
                else DestroyImmediate(collider);
            }
            return box;
        }

        private readonly List<Material> owned = new List<Material>();

        private Material Material(string name, Color color, bool emissive = false)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) shader = Shader.Find("Standard");
            Material material = new Material(shader) { name = "Beta " + name, color = color };
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.22f);
            if (emissive && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.1f);
            }
            owned.Add(material);
            return material;
        }

        private void OnDestroy()
        {
            foreach (Material material in owned)
            {
                if (!material) continue;
                if (Application.isPlaying) Destroy(material);
                else DestroyImmediate(material);
            }
            owned.Clear();
        }

        private static void AddLight(Transform root, Color key, Color ambient, Color fog, float intensity = 1.65f)
        {
            GameObject source = new GameObject("Beta key light");
            source.transform.SetParent(root, false);
            source.transform.localRotation = Quaternion.Euler(46f, -28f, 0f);
            Light light = source.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = key;
            light.intensity = intensity;
            light.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fog;
            RenderSettings.fogStartDistance = 48f;
            RenderSettings.fogEndDistance = 110f;
        }
    }
}
