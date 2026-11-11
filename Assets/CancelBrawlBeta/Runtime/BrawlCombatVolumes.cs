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
            target.positionCount = 5;
            target.startColor = target.endColor = fighter.PlayerColor;
            target.SetPosition(0, root - Vector3.right * .3f); target.SetPosition(1, root + Vector3.right * .3f);
            target.SetPosition(2, root); target.SetPosition(3, root - Vector3.forward * .3f); target.SetPosition(4, root + Vector3.forward * .3f);
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
