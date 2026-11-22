using System.Collections.Generic;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// Stable, bounded group camera; it never rotates controls during a match. Obstacles that hide a fighter
    /// are faded (GDD §5) and the optional shake is purely visual.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class BrawlCamera : MonoBehaviour
    {
        [SerializeField, Range(35f, 65f)] private float elevation = 50f;
        [SerializeField] private float minimumDistance = 24f;
        [SerializeField] private float maximumDistance = 43f;
        [SerializeField] private float followSharpness = 4f;
        /// <summary>Camera pitch and aim height used on production stages (frontal view).</summary>
        private const float StageElevation = 12f, StageAimLift = 2.6f;
        private Transform[] targets = System.Array.Empty<Transform>();
        private Camera view;
        private bool initialized;
        private BrawlArena arena;
        private Material fadeTemplate;
        private Vector3 basePosition;
        private float shake;
        /// <summary>Fraction of the viewport covered by the top HUD (title, clock); side framing keeps fighters below it.</summary>
        public float TopReserve { get; set; } = .14f;
        /// <summary>Orthographic zoom limits of the side camera (SYS-036).</summary>
        public const float MinSideSize = 5.5f, MaxSideSize = 16f;
        public Camera View => view;
        private readonly Dictionary<Renderer, Material> faded = new Dictionary<Renderer, Material>();
        private readonly Dictionary<Material, Material> fadeCache = new Dictionary<Material, Material>();
        private readonly HashSet<Renderer> occluding = new HashSet<Renderer>();
        private readonly RaycastHit[] hits = new RaycastHit[16];

        public int FadedCount => faded.Count;

        private void Awake()
        {
            view = GetComponent<Camera>();
            view.fieldOfView = 44f;
            // Reserve the bottom of the window for the HUD so it never covers return platforms.
            view.rect = new Rect(0f, 0.19f, 1f, 0.81f);
            view.nearClipPlane = 0.2f;
            view.farClipPlane = 180f;
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = BrawlArena.DefaultBackdrop;
        }

        public void Setup(BrawlArena court, Material occluderTemplate)
        {
            if (arena != court) RestoreAll();
            arena = court; fadeTemplate = occluderTemplate;
            if (!view) Awake();
            view.backgroundColor = court ? court.Backdrop : BrawlArena.DefaultBackdrop;
        }

        public void Configure(Transform[] participants)
        {
            targets = participants ?? System.Array.Empty<Transform>();
            if (!view) Awake();
            initialized = false;
            RestoreAll();
            Frame(1f);
        }

        public void Shake(float amount)
        {
            if (BrawlFeedback.ReducedEffects) return;
            shake = Mathf.Max(shake, amount);
        }

        private void LateUpdate()
        {
            Frame(1f - Mathf.Exp(-followSharpness * Time.unscaledDeltaTime));
            UpdateOcclusion();
        }

        private void Frame(float blend)
        {
            if (!view) return;
            if (arena && arena.IsSideView) { FrameSide(blend); return; }
            Vector2 clamp = arena ? arena.CameraClamp : new Vector2(14f, 11f);
            Bounds bounds = new Bounds(new Vector3(0f, 0.5f, 0f), Vector3.zero);
            int count = 0;
            foreach (Transform target in targets)
            {
                if (!target || !target.gameObject.activeInHierarchy) continue;
                Vector3 point = target.position;
                // Extreme falls should not force every remaining participant to become a dot.
                point.x = Mathf.Clamp(point.x, -clamp.x, clamp.x);
                point.z = Mathf.Clamp(point.z, -clamp.y, clamp.y);
                point.y = Mathf.Clamp(point.y, -2f, 8f);
                if (count++ == 0) bounds = new Bounds(point, Vector3.zero);
                else bounds.Encapsulate(point);
            }

            Vector3 center = count > 0 ? bounds.center : new Vector3(0f, 0.6f, 0f);
            center.x = Mathf.Clamp(center.x * 0.3f, -clamp.x * .18f, clamp.x * .18f);
            center.z = Mathf.Clamp(center.z * 0.25f, -clamp.y * .15f, clamp.y * .15f);
            center.y = Mathf.Clamp(center.y * 0.25f, 0.4f, 2f);

            // Production stages are framed from the front like a platform fighter: a low camera aimed
            // above the floor, so the scenery behind the platforms fills the frame.
            bool frontal = arena && arena.UsesStageArt;
            if (frontal) center.y += StageAimLift;
            Quaternion rotation = Quaternion.Euler(frontal ? StageElevation : elevation, 0f, 0f);
            float verticalHalfFov = view.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float horizontalFit = (bounds.extents.x + 4.8f) / (Mathf.Tan(verticalHalfFov) * Mathf.Max(0.8f, view.aspect));
            float depthFit = (bounds.extents.z + 6f) / Mathf.Tan(verticalHalfFov);
            float distance = Mathf.Clamp(Mathf.Max(horizontalFit, depthFit), minimumDistance, maximumDistance);
            // Keep a complete useful slice of the court in view even when fighters meet.
            distance = Mathf.Max(distance, view.aspect < 1.5f ? 34f : minimumDistance);
            Vector3 desired = center - rotation * Vector3.forward * distance;
            transform.rotation = rotation;
            basePosition = initialized ? Vector3.Lerp(basePosition, desired, blend) : desired;
            initialized = true;
            Vector3 offset = Vector3.zero;
            if (shake > 0.001f && !BrawlFeedback.ReducedEffects)
            {
                offset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * shake * .6f;
                shake = Mathf.MoveTowards(shake, 0f, Time.unscaledDeltaTime * 1.6f);
            }
            else shake = 0f;
            transform.position = basePosition + rotation * offset;
        }

        /// <summary>
        /// Side framing (SYS-036): every active fighter, head and marker included, plus the main stage, inside the part
        /// of the view not covered by the top HUD; zoom clamped; fixed rotation so aiming never changes reference.
        /// </summary>
        private void FrameSide(float blend)
        {
            float minX = -8.5f, maxX = 8.5f, minY = -.5f, maxY = 5.5f;
            foreach (var target in targets)
            {
                if (!target || !target.gameObject.activeInHierarchy) continue;
                float x = Mathf.Clamp(target.position.x, -19, 19), y = Mathf.Clamp(target.position.y, -4, 18);
                minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y + 2.4f); // marker badge sits 2.25 above the feet
            }
            view.orthographic = true;
            const float marginX = 2f, marginBottom = 1f, marginTop = .8f;
            float reserve = Mathf.Clamp(TopReserve, 0, .4f);
            float bottom = minY - marginBottom, top = maxY + marginTop;
            float sizeByHeight = (top - bottom) / (2 * (1 - reserve));
            float sizeByWidth = (maxX - minX + 2 * marginX) * .5f / Mathf.Max(.7f, view.aspect);
            float size = Mathf.Clamp(Mathf.Max(sizeByHeight, sizeByWidth), MinSideSize, MaxSideSize);
            view.orthographicSize = initialized ? Mathf.Lerp(view.orthographicSize, size, blend) : size;
            transform.rotation = Quaternion.identity;
            // The usable band [centre − size, centre + size − 2·reserve·size] is centred on the content.
            float centreY = (bottom + top) * .5f + reserve * size;
            Vector3 desired = new Vector3((minX + maxX) * .5f, centreY, -35);
            basePosition = initialized ? Vector3.Lerp(basePosition, desired, blend) : desired;
            transform.position = basePosition; initialized = true;
        }

        /// <summary>Fades any solid court piece between the camera and a fighter; restores it when clear.</summary>
        private void UpdateOcclusion()
        {
            occluding.Clear();
            Vector3 eye = transform.position;
            foreach (Transform target in targets)
            {
                if (!target || !target.gameObject.activeInHierarchy) continue;
                foreach (float height in new[] { .25f, 1.2f })
                {
                    Vector3 point = target.position + Vector3.up * height;
                    Vector3 ray = point - eye;
                    int count = Physics.RaycastNonAlloc(eye, ray.normalized, hits, ray.magnitude - .3f, 1, QueryTriggerInteraction.Ignore);
                    for (int i = 0; i < count; i++)
                    {
                        Renderer renderer = hits[i].collider.GetComponent<Renderer>();
                        if (renderer) occluding.Add(renderer);
                    }
                }
            }
            var restore = new List<Renderer>();
            foreach (var pair in faded) if (!pair.Key || !occluding.Contains(pair.Key)) restore.Add(pair.Key);
            foreach (Renderer renderer in restore) Restore(renderer);
            foreach (Renderer renderer in occluding)
            {
                if (faded.ContainsKey(renderer)) continue;
                faded[renderer] = renderer.sharedMaterial;
                if (fadeTemplate) renderer.sharedMaterial = FadeOf(renderer.sharedMaterial);
                else renderer.enabled = false;
            }
        }

        private Material FadeOf(Material original)
        {
            if (fadeCache.TryGetValue(original, out Material fade) && fade) return fade;
            fade = new Material(fadeTemplate) { name = original.name + " (atenuado)" };
            Color color = original.color; color.a = .3f; fade.color = color;
            if (fade.HasProperty("_BaseColor")) fade.SetColor("_BaseColor", color);
            fadeCache[original] = fade;
            return fade;
        }

        private void Restore(Renderer renderer)
        {
            if (renderer && faded.TryGetValue(renderer, out Material original))
            {
                renderer.sharedMaterial = original;
                renderer.enabled = true;
            }
            faded.Remove(renderer);
        }

        private void RestoreAll()
        {
            foreach (Renderer renderer in new List<Renderer>(faded.Keys)) Restore(renderer);
            faded.Clear();
        }

        private void OnDestroy()
        {
            foreach (Material material in fadeCache.Values) if (material) Destroy(material);
        }
    }
}
