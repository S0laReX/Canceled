using System.Collections.Generic;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>Presentation only: particles, warnings and generated audio. Never decides combat results.</summary>
    public static class BrawlFeedback
    {
        private static readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();

        /// <summary>Accessibility: fewer particles, no blinking and no camera shake.</summary>
        public static bool ReducedEffects { get; set; }
        /// <summary>SFX bus volume (see <see cref="BrawlAudio"/>).</summary>
        public static float SfxVolume => BrawlAudio.Volume(BrawlAudioBus.Sfx);
        /// <summary>Music bus volume (see <see cref="BrawlAudio"/>).</summary>
        public static float MusicVolume => BrawlAudio.Volume(BrawlAudioBus.Music);

        public static void Initialize(Transform root)
        {
            foreach (var material in materials.Values) if (material) Object.Destroy(material);
            materials.Clear();
            BrawlAudio.Initialize(root);
        }

        /// <summary>Sets the SFX and Music buses; Master, Voices and UI keep their values.</summary>
        public static void SetVolumes(float sfx, float musicLevel)
        {
            BrawlAudio.SetVolume(BrawlAudioBus.Sfx, sfx); BrawlAudio.SetVolume(BrawlAudioBus.Music, musicLevel);
        }

        public static Material Material(Color color)
        {
            if (materials.TryGetValue(color, out Material material) && material) return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            material = new Material(shader); material.color = color;
            materials[color] = material; return material;
        }

        /// <summary>Plays a sound through its bus (<see cref="BrawlAudio.Route"/>).</summary>
        public static void Sound(string kind) => BrawlAudio.Play(kind);

        public static void Burst(Vector3 position, Color color, int count)
        {
            if (ReducedEffects) count = Mathf.Max(1, count / 3);
            for (int i = 0; i < count; i++)
            {
                var spark = GameObject.CreatePrimitive(PrimitiveType.Cube); spark.name = "Impacto";
                var collider = spark.GetComponent<Collider>(); collider.enabled = false; Object.Destroy(collider);
                spark.transform.position = position; spark.transform.localScale = Vector3.one * Random.Range(.06f, .14f);
                spark.GetComponent<Renderer>().sharedMaterial = Material(color);
                spark.AddComponent<BrawlTransient>().Initialize(Random.onUnitSphere * Random.Range(2, 6), .3f);
            }
        }

        public static Vector3 FloorBelow(Vector3 point)
        {
            return Physics.Raycast(point + Vector3.up * .5f, Vector3.down, out RaycastHit hit, 12, 1, QueryTriggerInteraction.Ignore)
                ? hit.point : point;
        }

        /// <summary>Flat circle drawn in local space under <paramref name="parent"/>.</summary>
        public static LineRenderer Circle(Transform parent, float radius, Color color, Quaternion rotation, float width, float y = 0)
        {
            var obj = new GameObject("Contorno"); obj.transform.SetParent(parent, false);
            obj.transform.localRotation = rotation; obj.transform.localPosition = Vector3.up * y;
            var line = obj.AddComponent<LineRenderer>(); line.sharedMaterial = Material(color); line.widthMultiplier = width;
            line.useWorldSpace = false; line.loop = true; line.positionCount = 40;
            for (int i = 0; i < 40; i++) { float a = i * Mathf.PI * 2 / 40; line.SetPosition(i, new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius); }
            return line;
        }

        /// <summary>
        /// Floor marking for an area attack: horizontal reach (arc), explicit vertical reach (ticks) and,
        /// optionally, announced safe sectors centred at ±90° of <paramref name="forward"/>.
        /// </summary>
        public static void Warning(Vector3 position, float radius, float height, float duration, Color color, Vector3 forward,
            float arcDegrees, float safeHalfAngle = 0)
        {
            var obj = new GameObject("Área anunciada · altura " + height.ToString("0.0") + " m");
            obj.transform.position = position + Vector3.up * .08f;
            Vector3 axis = forward.sqrMagnitude > .01f ? Vector3.ProjectOnPlane(forward, Vector3.up).normalized : Vector3.forward;
            bool full = arcDegrees >= 359;
            float start = full ? -180 : -arcDegrees * .5f, end = full ? 180 : arcDegrees * .5f;
            var segment = new List<Vector3>();
            const float step = 6;
            for (float angle = start; angle <= end + .01f; angle += step)
            {
                float abs = Mathf.Abs(angle);
                bool safe = safeHalfAngle > 0 && Mathf.Abs(abs - 90) < safeHalfAngle;
                if (!safe) segment.Add(Quaternion.Euler(0, angle, 0) * axis * radius);
                if ((safe || angle + step > end + .01f) && segment.Count > 1) { Polyline(obj.transform, segment, color, .08f, false); segment.Clear(); }
                else if (safe) segment.Clear();
            }
            if (!full) Polyline(obj.transform, new List<Vector3> { Quaternion.Euler(0, start, 0) * axis * radius, Vector3.zero, Quaternion.Euler(0, end, 0) * axis * radius }, color, .06f, false);
            if (safeHalfAngle > 0)
            {
                // White spokes delimit each safe sector.
                foreach (float centre in new[] { -90f, 90f })
                    foreach (float edge in new[] { centre - safeHalfAngle, centre + safeHalfAngle })
                        Polyline(obj.transform, new List<Vector3> { Vector3.zero, Quaternion.Euler(0, edge, 0) * axis * radius }, Color.white, .05f, false);
            }
            if (height > 0)
            {
                // Vertical ticks make the finite height visible without covering the floor.
                foreach (float angle in full ? new[] { 0f, 120f, 240f } : new[] { start, 0f, end })
                {
                    Vector3 edge = Quaternion.Euler(0, angle, 0) * axis * radius;
                    Polyline(obj.transform, new List<Vector3> { edge, edge + Vector3.up * height }, color, .04f, false);
                }
            }
            obj.AddComponent<BrawlTransient>().Initialize(Vector3.zero, duration);
        }

        /// <summary>Announces the direction of a heavy attack during its wind-up (GDD §7).</summary>
        public static void Telegraph(Vector3 position, Vector3 forward, float length, float duration, Color color)
        {
            var obj = new GameObject("Dirección anunciada");
            obj.transform.position = FloorBelow(position) + Vector3.up * .09f;
            Vector3 f = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            Vector3 r = Vector3.Cross(Vector3.up, f);
            Polyline(obj.transform, new List<Vector3> { f * .5f, f * length, f * (length - .45f) + r * .35f, f * length, f * (length - .45f) - r * .35f }, color, .1f, false);
            obj.AddComponent<BrawlTransient>().Initialize(Vector3.zero, duration);
        }

        /// <summary>Expanding floor ring (Coach's ground wave).</summary>
        public static void Pulse(Vector3 position, float radius, float duration, Color color)
        {
            var obj = new GameObject("Onda");
            obj.transform.position = position + Vector3.up * .1f;
            Circle(obj.transform, 1, color, Quaternion.identity, .12f);
            obj.AddComponent<BrawlTransient>().Initialize(Vector3.zero, duration, radius);
        }

        private static void Polyline(Transform parent, List<Vector3> points, Color color, float width, bool loop)
        {
            var obj = new GameObject("Trazo"); obj.transform.SetParent(parent, false);
            var line = obj.AddComponent<LineRenderer>(); line.sharedMaterial = Material(color);
            line.widthMultiplier = width; line.useWorldSpace = false; line.loop = loop;
            line.positionCount = points.Count; line.SetPositions(points.ToArray());
        }
    }
}
