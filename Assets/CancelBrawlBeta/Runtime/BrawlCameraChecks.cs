using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// SYS-036–040 in 2.5D: five fighters spread in X and height in every arena, framed below the top HUD with zoom
    /// limits; fixed rotation (aiming never changes reference); occluders faded without hiding the playable floor;
    /// off-camera badges and KO-limit warnings. Fighters are posed during the countdown, when the simulation is frozen.
    /// </summary>
    public static class BrawlCameraChecks
    {
        private static readonly Vector2[] Spread = { new Vector2(-14, .05f), new Vector2(-7, 6), new Vector2(0, 12), new Vector2(7, 3), new Vector2(14, 9) };
        private static readonly Vector2[] Extreme = { new Vector2(-20, 2), new Vector2(-3, .05f), new Vector2(0, 18), new Vector2(3, .05f), new Vector2(6, .05f) };
        private static readonly Vector2[] Together = { new Vector2(-1.5f, .05f), new Vector2(-.5f, .05f), new Vector2(.5f, .05f), new Vector2(1.5f, .05f), new Vector2(0, .05f) };

        public static IEnumerator Run(BrawlMatch match, Action<bool, string> check, string output)
        {
            int oldArena = match.ArenaIndex, oldOpponents = match.OpponentCount;
            GameObject blocker = null;
            match.AutomatedTest = true;
            try
            {
                var group = match.GroupCamera; var hud = match.Hud;
                for (int arena = 0; arena < BrawlArena.Count; arena++)
                {
                    string name = BrawlArena.NameOf(arena, match.UsesStageArt(arena));
                    // Spread: everyone visible below the top HUD, no edge badges, zoom inside its limits.
                    yield return Pose(match, arena, Spread);
                    bool visible = AllVisible(match, group);
                    float size = group.View.orthographicSize;
                    check(visible && hud.OffscreenMarkers == 0 && size >= BrawlCamera.MinSideSize - .01f && size <= BrawlCamera.MaxSideSize + .01f,
                        $"{name}: cinco luchadores separados en X y altura quedan encuadrados bajo el HUD (zoom {size:0.0})");
                    yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, $"camera-{arena}-spread.png"));

                    // Extreme: near the side and top KO limits → warnings; zoom never beyond its maximum.
                    yield return Pose(match, arena, Extreme);
                    check(hud.DangerMarkers >= 2 && group.View.orthographicSize <= BrawlCamera.MaxSideSize + .01f,
                        $"{name}: cerca de los límites de expulsión el marcador avisa con «!» y el zoom no supera el máximo");
                    if (arena == 0) { yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, "camera-ko-warning.png")); }

                    // Together: minimum zoom, fixed rotation and aiming reference.
                    yield return Pose(match, arena, Together);
                    check(Mathf.Abs(group.View.orthographicSize - BrawlCamera.MinSideSize) < .05f && group.transform.rotation == Quaternion.identity &&
                          match.CameraDirection(Vector2.right) == Vector3.right && match.CameraDirection(Vector2.left) == Vector3.left,
                        $"{name}: juntos usa el zoom mínimo; la cámara no gira y derecha sigue siendo derecha al apuntar");
                }

                // Off-camera badge: a fighter beyond the framed area still has an edge indicator.
                yield return Pose(match, 0, new[] { new Vector2(-21.5f, .05f), new Vector2(-3, .05f), new Vector2(0, .05f), new Vector2(3, .05f), new Vector2(19, .05f) });
                check(hud.OffscreenMarkers >= 1 && hud.DangerMarkers >= 1, "Luchador fuera de cámara: indicador en el borde y aviso de límite");

                // Occlusion: a block between the camera and a fighter fades; the playable floor does not.
                yield return Pose(match, 0, Together);
                blocker = GameObject.CreatePrimitive(PrimitiveType.Cube); blocker.name = "Oclusor de prueba";
                blocker.transform.position = new Vector3(0, 1, -6); blocker.transform.localScale = new Vector3(3, 3, 1);
                var blockRenderer = blocker.GetComponent<Renderer>();
                yield return null; yield return null;
                bool faded = !blockRenderer.enabled || blockRenderer.sharedMaterial.name.Contains("atenuado");
                bool floorIntact = true;
                foreach (var platform in match.Arena.GetComponentsInChildren<Renderer>())
                    if (platform.sharedMaterial != null && platform.sharedMaterial.name.Contains("atenuado") && platform.transform.position.y < .5f && platform.bounds.size.x > 10)
                        floorIntact = false;
                check(faded && floorIntact, "Oclusión: un bloque delante del luchador se atenúa y el suelo jugable sigue visible");
            }
            finally
            {
                if (blocker) UnityEngine.Object.Destroy(blocker);
                match.ReturnToMenu(); match.ArenaIndex = oldArena; match.OpponentCount = oldOpponents; match.AutomatedTest = true;
            }
        }

        /// <summary>Starts a 5-fighter match and poses everyone during the countdown (simulation frozen), letting the camera settle.</summary>
        private static IEnumerator Pose(BrawlMatch match, int arena, Vector2[] spots)
        {
            // A fresh countdown each time (3 s) leaves room to pose and let the camera settle.
            match.ReturnToMenu(); match.ArenaIndex = arena; match.OpponentCount = 4; match.StartMatch();
            yield return null;
            for (int i = 0; i < 5; i++) match.Fighters[i].RespawnAt(new Vector3(spots[i].x, spots[i].y, 0), false);
            yield return new WaitForSecondsRealtime(1.2f);
            if (match.Phase != MatchPhase.Countdown) { match.ReturnToMenu(); yield break; }
        }

        private static bool AllVisible(BrawlMatch match, BrawlCamera group)
        {
            float topLimit = 1 - group.TopReserve;
            foreach (var fighter in match.Fighters)
            {
                foreach (float height in new[] { 0f, 2.4f })
                {
                    Vector3 v = group.View.WorldToViewportPoint(fighter.transform.position + Vector3.up * height);
                    if (v.x < 0 || v.x > 1 || v.y < 0 || v.y > topLimit + .005f) return false;
                }
            }
            return true;
        }
    }
}
