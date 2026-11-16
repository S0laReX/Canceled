using System.Collections.Generic;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// Lab-only wire view of every non-melee hit volume, drawn from the same constants the hit checks use:
    /// projectiles (sphere around the projectile, compared with the target's chest), announced zones (cylinder or
    /// sphere), the emitter trap (cylinder window) and the ground wave ring. Yellow = not yet able to hit, red = live.
    /// Creates no colliders and never affects the match.
    /// </summary>
    public sealed class BrawlHazardVolumes : MonoBehaviour
    {
        private const int Segments = 40;
        private static readonly Color Pending = new Color(1, .85f, .2f), Live = new Color(1, .3f, .4f);
        private BrawlMatch match;
        private readonly List<LineRenderer> pool = new List<LineRenderer>();
        private int used;
        private Material pendingMaterial, liveMaterial;
        /// <summary>Volumes drawn in the last frame (projectiles, zones, traps and waves).</summary>
        public int DrawnVolumes { get; private set; }

        public void Configure(BrawlMatch owner)
        {
            match = owner;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            pendingMaterial = new Material(shader) { color = Pending };
            liveMaterial = new Material(shader) { color = Live };
        }

        private void LateUpdate()
        {
            used = 0; DrawnVolumes = 0;
            if (match != null && match.IsTraining && match.ShowCombatVolumes && match.Phase != MatchPhase.Menu)
            {
                foreach (var projectile in FindObjectsByType<BrawlProjectile>())
                {
                    Sphere(projectile.transform.position, BrawlProjectile.HitDistance, true); DrawnVolumes++;
                }
                foreach (var hazard in BrawlHazard.Active)
                {
                    if (!hazard) continue;
                    Vector3 p = hazard.transform.position;
                    if (hazard is BrawlZone zone)
                    {
                        bool live = zone.Remaining <= 0;
                        if (zone.IsSphere) Sphere(p, zone.HitRadius, live);
                        else Cylinder(p, zone.HitRadius, -BrawlZone.BelowTolerance, zone.Height, live);
                        DrawnVolumes++;
                    }
                    else if (hazard is BrawlTrap trap)
                    {
                        Cylinder(p, BrawlTrap.TriggerRadius, -BrawlTrap.BelowReach, BrawlTrap.AboveReach, trap.Armed); DrawnVolumes++;
                    }
                }
                foreach (var fighter in match.Fighters)
                {
                    if (fighter == null || !fighter.TryGetWaveVolume(out float radius, out float height)) continue;
                    Cylinder(fighter.transform.position, radius, -height, height, fighter.AttackIsActive); DrawnVolumes++;
                }
            }
            for (int i = used; i < pool.Count; i++) pool[i].enabled = false;
        }

        private void Sphere(Vector3 centre, float radius, bool live)
        {
            Circle(centre, radius, Quaternion.identity, live);
            Circle(centre, radius, Quaternion.Euler(90, 0, 0), live);
            Circle(centre, radius, Quaternion.Euler(0, 0, 90), live);
        }

        /// <summary>Vertical window [bottom, top] relative to <paramref name="baseCentre"/>, with four edges.</summary>
        private void Cylinder(Vector3 baseCentre, float radius, float bottom, float top, bool live)
        {
            Circle(baseCentre + Vector3.up * bottom, radius, Quaternion.identity, live);
            Circle(baseCentre + Vector3.up * top, radius, Quaternion.identity, live);
            for (int i = 0; i < 4; i++)
            {
                Vector3 offset = Quaternion.Euler(0, i * 90, 0) * Vector3.forward * radius;
                var edge = Next(live); edge.positionCount = 2;
                edge.SetPosition(0, baseCentre + offset + Vector3.up * bottom);
                edge.SetPosition(1, baseCentre + offset + Vector3.up * top);
            }
        }

        private void Circle(Vector3 centre, float radius, Quaternion rotation, bool live)
        {
            var line = Next(live); line.positionCount = Segments + 1;
            for (int i = 0; i <= Segments; i++)
            {
                float a = i * Mathf.PI * 2 / Segments;
                line.SetPosition(i, centre + rotation * new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius));
            }
        }

        private LineRenderer Next(bool live)
        {
            if (used == pool.Count)
            {
                var child = new GameObject("Volumen de peligro"); child.transform.SetParent(transform, false);
                var created = child.AddComponent<LineRenderer>();
                created.widthMultiplier = .035f; created.useWorldSpace = true;
                created.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                pool.Add(created);
            }
            var line = pool[used++];
            line.sharedMaterial = live ? liveMaterial : pendingMaterial;
            line.startColor = line.endColor = live ? Live : Pending;
            line.enabled = true;
            return line;
        }

        private void OnDestroy() { if (pendingMaterial) Destroy(pendingMaterial); if (liveMaterial) Destroy(liveMaterial); }
    }
}
