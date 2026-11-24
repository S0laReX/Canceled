using System.Collections.Generic;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>Persistent attack volumes. Spawn selection avoids them (GDD §4).</summary>
    public abstract class BrawlHazard : MonoBehaviour
    {
        public static readonly List<BrawlHazard> Active = new List<BrawlHazard>();
        protected BrawlMatch match;
        protected BrawlAttackToken token;
        public BrawlFighter Owner => token?.Source;
        public abstract float Radius { get; }
        protected virtual void OnEnable() => Active.Add(this);
        protected virtual void OnDisable() => Active.Remove(this);
        protected bool OwnerGone => !match || token == null || token.Source == null || token.Source.Eliminated || token.Source.IsRespawning;

        protected static bool Valid(BrawlFighter fighter, BrawlFighter owner) =>
            fighter != null && fighter != owner && !fighter.Eliminated && !fighter.IsRespawning;
    }

    /// <summary>
    /// Announced area that resolves after a delay: Lola's "Viralización masiva" (cylinder on the floor)
    /// and Cripto King's "Burbuja especulativa" (sphere that grows, then bursts).
    /// </summary>
    public sealed class BrawlZone : BrawlHazard
    {
        private float radius, height, delay, elapsed;
        private bool sphere;
        private Transform visual;
        /// <summary>Extra reach added to the announced radius when resolving.</summary>
        public const float Tolerance = .3f;
        /// <summary>Cylinder zones also catch fighters standing this far below their base.</summary>
        public const float BelowTolerance = .6f;
        public override float Radius => radius;
        public bool IsSphere => sphere;

        /// <summary>Removes zones of an activation that has not resolved yet (its caster was interrupted).</summary>
        public static int CancelFor(BrawlAttackToken activation)
        {
            int removed = 0;
            for (int i = Active.Count - 1; i >= 0; i--)
                if (Active[i] is BrawlZone zone && zone.token == activation && zone.elapsed < zone.delay)
                { zone.gameObject.SetActive(false); Destroy(zone.gameObject); removed++; }
            return removed;
        }
        public float HitRadius => radius + Tolerance;
        public float Height => height;
        public float Remaining => Mathf.Max(0, delay - elapsed);

        public static BrawlZone Create(BrawlMatch owner, BrawlAttackToken activation, Vector3 center, float radius, float height, float delay, bool sphere)
        {
            var obj = new GameObject(sphere ? "Burbuja especulativa" : "Zona viral");
            obj.transform.position = center;
            var zone = obj.AddComponent<BrawlZone>();
            zone.match = owner; zone.token = activation; zone.radius = radius; zone.height = height; zone.delay = delay; zone.sphere = sphere;
            Color color = activation.Source.PlayerColor;
            if (owner.IsSideView)
                BrawlSideTelegraph.Zone(owner,obj.transform,radius,height,delay,sphere);
            else if (sphere)
            {
                zone.visual = new GameObject("Contorno").transform; zone.visual.SetParent(obj.transform, false);
                BrawlFeedback.Circle(zone.visual, 1, color, Quaternion.identity, .05f);
                BrawlFeedback.Circle(zone.visual, 1, color, Quaternion.Euler(90, 0, 0), .05f);
                BrawlFeedback.Circle(zone.visual, 1, color, Quaternion.Euler(90, 90, 0), .05f);
                zone.visual.localScale = Vector3.one * .3f;
                // Floor footprint shows where the burst reaches even while the sphere is small.
                BrawlFeedback.Warning(BrawlFeedback.FloorBelow(center), radius, 0, delay, color, Vector3.zero, 360);
            }
            else BrawlFeedback.Warning(center, radius, height, delay, color, Vector3.zero, 360);
            return zone;
        }

        private void FixedUpdate()
        {
            if (OwnerGone) { Destroy(gameObject); return; }
            if (match.Phase != MatchPhase.Playing) return;
            elapsed += Time.fixedDeltaTime;
            if (visual) visual.localScale = Vector3.one * Mathf.Lerp(.3f, radius, Mathf.Clamp01(elapsed / delay));
            if (elapsed < delay) return;
            foreach (var fighter in match.Fighters)
            {
                if (!Valid(fighter, Owner)) continue;
                Vector3 chest = fighter.transform.position + Vector3.up * BrawlProjectile.ChestHeight;
                Vector3 flat = Vector3.ProjectOnPlane(chest - transform.position, Vector3.up);
                float dy = fighter.transform.position.y - transform.position.y;
                bool inside = sphere ? (chest - transform.position).magnitude <= HitRadius
                                     : flat.magnitude <= HitRadius && dy > -BelowTolerance && dy < height;
                if (inside) token.Hit(fighter, flat.sqrMagnitude > .01f ? flat.normalized : Owner.Facing);
            }
            if (BrawlImpactFx.Instance) BrawlImpactFx.Instance.Impact(transform.position + Vector3.up * .6f, Owner.PlayerColor, ImpactTier.Super, Vector3.up);
            BrawlFeedback.Sound("super");
            match.Shake(.35f);
            Destroy(gameObject);
        }
    }

    /// <summary>Tío Antena's "Interferencia": one active emitter that launches the first valid rival upward.</summary>
    public sealed class BrawlTrap : BrawlHazard
    {
        public const float Lifetime = 7f, ArmTime = .35f, TriggerRadius = 1.15f;
        /// <summary>Vertical window, relative to the emitter base, in which a fighter's root triggers it.</summary>
        public const float BelowReach = .4f, AboveReach = 1.3f;
        private float age;
        private Transform pulse;
        public override float Radius => TriggerRadius;
        public bool Armed => age >= ArmTime;

        public static BrawlTrap Create(BrawlMatch owner, BrawlAttackToken activation, Vector3 floor)
        {
            var obj = new GameObject("Emisor de interferencia");
            obj.transform.position = floor;
            var trap = obj.AddComponent<BrawlTrap>();
            trap.match = owner; trap.token = activation;
            Color color = activation.Source.PlayerColor;
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder); post.name = "Antena";
            Object.Destroy(post.GetComponent<Collider>());
            post.transform.SetParent(obj.transform, false); post.transform.localPosition = Vector3.up * .45f; post.transform.localScale = new Vector3(.12f, .45f, .12f);
            post.GetComponent<Renderer>().sharedMaterial = BrawlFeedback.Material(new Color(.75f, .78f, .82f));
            var tip = GameObject.CreatePrimitive(PrimitiveType.Sphere); tip.name = "Señal";
            Object.Destroy(tip.GetComponent<Collider>());
            tip.transform.SetParent(obj.transform, false); tip.transform.localPosition = Vector3.up * .95f; tip.transform.localScale = Vector3.one * .26f;
            tip.GetComponent<Renderer>().sharedMaterial = BrawlFeedback.Material(color);
            BrawlFeedback.Circle(obj.transform, TriggerRadius, color, Quaternion.identity, .05f, .04f);
            trap.pulse = new GameObject("Pulso").transform; trap.pulse.SetParent(obj.transform, false);
            BrawlFeedback.Circle(trap.pulse, 1, color, Quaternion.identity, .03f, .05f);
            return trap;
        }

        private void FixedUpdate()
        {
            if (OwnerGone) { Destroy(gameObject); return; }
            if (match.Phase != MatchPhase.Playing) return;
            age += Time.fixedDeltaTime;
            if (pulse) pulse.localScale = Vector3.one * (TriggerRadius * Mathf.Repeat(age * 1.4f, 1f));
            if (age >= Lifetime) { Destroy(gameObject); return; }
            if (!Armed) return;
            foreach (var fighter in match.Fighters)
            {
                if (!Valid(fighter, Owner)) continue;
                Vector3 flat = Vector3.ProjectOnPlane(fighter.transform.position - transform.position, Vector3.up);
                float dy = fighter.transform.position.y - transform.position.y;
                if (flat.magnitude > TriggerRadius || dy < -BelowReach || dy > AboveReach) continue;
                if (!token.Hit(fighter, flat.sqrMagnitude > .01f ? flat.normalized : Owner.Facing)) continue;
                if (BrawlImpactFx.Instance) BrawlImpactFx.Instance.Impact(transform.position + Vector3.up, Owner.PlayerColor, ImpactTier.Strong, Vector3.up);
                Destroy(gameObject);
                return;
            }
        }
    }
}
