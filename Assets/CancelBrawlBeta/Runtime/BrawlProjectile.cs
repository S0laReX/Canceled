using UnityEngine;
namespace CancelBrawl.Beta
{
    /// <summary>Straight projectile at a fixed height. Solid geometry stops it unless it still has bounces.</summary>
    public sealed class BrawlProjectile : MonoBehaviour
    {
        private const float Radius = .22f;
        private BrawlMatch match;
        private BrawlAttackToken token;
        private Vector3 direction;
        private float remaining, speed;
        public int BouncesLeft { get; private set; }
        public BrawlFighter Source => token?.Source;

        public static BrawlProjectile Create(BrawlMatch owner, BrawlAttackToken activation, Vector3 position, Vector3 forward, Color color,
            string label, Vector3 scale, float speed = 12, float lifetime = 1.15f, int bounces = 0)
        {
            var obj = GameObject.CreatePrimitive(bounces > 0 ? PrimitiveType.Cylinder : PrimitiveType.Cube); obj.name = label;
            var collider = obj.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
            obj.transform.position = position; obj.transform.rotation = Quaternion.LookRotation(forward);
            if (bounces > 0) obj.transform.rotation *= Quaternion.Euler(90, 0, 0);
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = BrawlFeedback.Material(color);
            var projectile = obj.AddComponent<BrawlProjectile>();
            projectile.match = owner; projectile.token = activation; projectile.direction = forward.normalized;
            projectile.speed = speed; projectile.remaining = lifetime; projectile.BouncesLeft = bounces;
            return projectile;
        }

        private void FixedUpdate()
        {
            if (!match || token.Source.Eliminated || token.Source.IsRespawning) { Destroy(gameObject); return; }
            if (match.Phase != MatchPhase.Playing) return;
            // A cast omits initial overlaps: a projectile born against a wall must still stop.
            if (Physics.CheckSphere(transform.position, Radius, 1, QueryTriggerInteraction.Ignore))
            { BrawlFeedback.Burst(transform.position, token.Source.PlayerColor, 3); Destroy(gameObject); return; }
            float distance = speed * Time.fixedDeltaTime;
            Vector3 next = transform.position + direction * distance;
            if (Physics.SphereCast(transform.position, Radius, direction, out RaycastHit hit, distance, 1, QueryTriggerInteraction.Ignore))
            {
                BrawlFeedback.Burst(hit.point, token.Source.PlayerColor, 3);
                // Bounce stays at the same height so its route remains readable from the camera.
                Vector3 reflected = Vector3.Reflect(direction, hit.normal); reflected.y = 0;
                if (BouncesLeft <= 0 || reflected.sqrMagnitude < .01f) { Destroy(gameObject); return; }
                Vector3 contact = transform.position + direction * Mathf.Max(0, hit.distance - .02f);
                BouncesLeft--; direction = reflected.normalized;
                next = contact + direction * Mathf.Max(0, distance - hit.distance);
                BrawlFeedback.Sound("bounce");
            }
            transform.position = next;
            foreach (var fighter in match.Fighters)
            {
                if (fighter == token.Source || fighter.Eliminated || fighter.IsRespawning) continue;
                if (Vector3.Distance(fighter.transform.position + Vector3.up * .85f, transform.position) < .8f && token.Hit(fighter, direction))
                { Destroy(gameObject); return; }
            }
            remaining -= Time.fixedDeltaTime;
            if (remaining <= 0) Destroy(gameObject);
        }
    }
}
