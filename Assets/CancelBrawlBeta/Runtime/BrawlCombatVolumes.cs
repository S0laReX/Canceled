using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>Build-visible wire volumes for the lab. Does not create colliders or apply damage.</summary>
    public sealed class BrawlCombatVolumes : MonoBehaviour
    {
        private BrawlMatch match;
        private BrawlFighter fighter;
        private LineRenderer reach, target;
        private Material attackMaterial, targetMaterial;
        private readonly Vector3[] points = new Vector3[205];
        public void Configure(BrawlMatch owner, BrawlFighter subject)
        {
            match = owner; fighter = subject;
            attackMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            targetMaterial = new Material(attackMaterial); targetMaterial.color = subject.PlayerColor;
            reach = MakeLine("Volumen ofensivo", attackMaterial); target = MakeLine("Referencia del objetivo", targetMaterial);
        }

        /// <summary>World rectangle drawn last frame for the current side-view hit box (null when none); for checks.</summary>
        public Rect? LastHitbox { get; private set; }
        // Slightly in front of the plane so the outline is not hidden inside the model.
        private static Vector3 Corner(float x, float y) => new Vector3(x, y, -.6f);

        private LineRenderer MakeLine(string name, Material material)
        {
            var child = new GameObject(name); child.transform.SetParent(transform, false);
            var line = child.AddComponent<LineRenderer>();
            line.sharedMaterial = material; line.widthMultiplier = .035f;
            line.useWorldSpace = true; line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return line;
        }

        private void LateUpdate()
        {
            bool visible = match && match.IsTraining && match.ShowCombatVolumes && !fighter.IsRespawning;
            target.enabled = visible; reach.enabled = false;
            if (!visible) return;
            // Sweeps compare the fighters' roots; this cross marks that exact reference point.
            Vector3 root = transform.position + Vector3.up * .04f;
            // Projectiles and sphere zones compare the chest point instead; a vertical tick leads to its cross.
            Vector3 chest = transform.position + Vector3.up * BrawlProjectile.ChestHeight;
            target.positionCount = 11;
            target.startColor = target.endColor = fighter.PlayerColor;
            target.SetPosition(0, root - Vector3.right * .3f); target.SetPosition(1, root + Vector3.right * .3f);
            target.SetPosition(2, root); target.SetPosition(3, root - Vector3.forward * .3f); target.SetPosition(4, root + Vector3.forward * .3f);
            target.SetPosition(5, root); target.SetPosition(6, chest);
            target.SetPosition(7, chest - Vector3.right * .18f); target.SetPosition(8, chest + Vector3.right * .18f);
            target.SetPosition(9, chest); target.SetPosition(10, chest + Vector3.forward * .18f);
            if (match.IsSideView)
            {
                // Side view (SYS-042–048): the hurtbox outline joins the reference cross; the hit box is a rectangle.
                Rect hurt = BrawlMoveData.Hurtbox(fighter.Root2D);
                target.positionCount = 16;
                target.SetPosition(11, Corner(hurt.xMin, hurt.yMin)); target.SetPosition(12, Corner(hurt.xMax, hurt.yMin));
                target.SetPosition(13, Corner(hurt.xMax, hurt.yMax)); target.SetPosition(14, Corner(hurt.xMin, hurt.yMax));
                target.SetPosition(15, Corner(hurt.xMin, hurt.yMin));
                LastHitbox = null;
                if (!fighter.TryGetSideHitbox(out MoveBox box, out _)) return;
                Rect hit = BrawlMoveData.World(box, fighter.Root2D, fighter.FacingSign);
                LastHitbox = hit;
                reach.positionCount = 5;
                reach.SetPosition(0, Corner(hit.xMin, hit.yMin)); reach.SetPosition(1, Corner(hit.xMax, hit.yMin));
                reach.SetPosition(2, Corner(hit.xMax, hit.yMax)); reach.SetPosition(3, Corner(hit.xMin, hit.yMax));
                reach.SetPosition(4, Corner(hit.xMin, hit.yMin));
                reach.startColor = reach.endColor = fighter.AttackIsActive ? new Color(1, .3f, .4f) : new Color(1, .85f, .2f);
                attackMaterial.color = reach.startColor;
                reach.enabled = true;
                return;
            }
            if (!fighter.TryGetMeleeVolume(out float range, out float height, out float dot, out bool safe)) return;
            // SafeSweep has two excluded sectors. Its existing telegraph draws these; avoid drawing an incorrect full disk.
            if (safe) return;
            float halfAngle = Mathf.Acos(Mathf.Clamp(dot, -1, 1));
            Vector3 forward = fighter.Facing, right = Vector3.Cross(Vector3.up, forward);
            int n = 0;
            for (int level = 0; level < 2; level++)
            {
                Vector3 center = transform.position + Vector3.up * (level == 0 ? -height : height);
                points[n++] = center;
                for (int i = 0; i <= 48; i++)
                {
                    float angle = Mathf.Lerp(-halfAngle, halfAngle, i / 48f);
                    points[n++] = center + (forward * Mathf.Cos(angle) + right * Mathf.Sin(angle)) * range;
                }
                points[n++] = center;
            }
            // Vertical edges at the front and sides show the real height tolerance.
            for (int i = 0; i < 3; i++)
            {
                float angle = (i - 1) * halfAngle;
                Vector3 edge = transform.position + (forward * Mathf.Cos(angle) + right * Mathf.Sin(angle)) * range;
                points[n++] = edge - Vector3.up * height; points[n++] = edge + Vector3.up * height;
            }
            reach.positionCount = n;
            for (int i = 0; i < n; i++) reach.SetPosition(i, points[i]);
            reach.startColor = reach.endColor = fighter.AttackIsActive ? new Color(1, .3f, .4f) : new Color(1, .85f, .2f);
            attackMaterial.color = reach.startColor;
            reach.enabled = true;
        }

        private void OnDestroy() { if (attackMaterial) Destroy(attackMaterial); if (targetMaterial) Destroy(targetMaterial); }
    }
}
