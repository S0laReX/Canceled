using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// ARENA-358/359 in 2.5D, for every arena: KO limits left/right/bottom/top only (no depth KO), exact at the limit,
    /// beyond the camera clamp; two-to-five separated spawns on real ground without overlapping geometry; respawns
    /// avoid points covered by active traps.
    /// </summary>
    public static class BrawlArenaChecks
    {
        [Serializable] private sealed class Row { public string arena; public float left, right, bottom, top, cameraClamp; public int spawns; public float minSpacing; }
        [Serializable] private sealed class Report { public string version = BrawlMatch.ProductionVersion; public List<Row> arenas = new List<Row>(); }

        public static IEnumerator Run(BrawlMatch match, Action<bool, string> check, string output)
        {
            int oldArena = match.ArenaIndex, oldOpponents = match.OpponentCount;
            var report = new Report();
            var traps = new List<GameObject>();
            match.AutomatedTest = true;
            try
            {
                for (int index = 0; index < BrawlArena.Count; index++)
                {
                    string name = BrawlArena.NameOf(index, match.UsesStageArt(index));
                    match.ReturnToMenu(); match.ArenaIndex = index; match.OpponentCount = 1; match.StartMatch();
                    yield return new WaitForSeconds(3.2f);
                    var arena = match.Arena; var p2 = match.Fighters[1];
                    float side = arena.OuterLimits.x, bottom = arena.LowerLimit, top = arena.UpperLimit;
                    var row = new Row { arena = name, left = -side, right = side, bottom = bottom, top = top, cameraClamp = arena.CameraClamp.x };

                    // ── ARENA-358: limits on the four sides, exact, none in depth ──
                    bool exact = true;
                    foreach (var probe in new[] { new Vector3(side, 1, 0), new Vector3(-side, 1, 0), new Vector3(0, bottom, 0), new Vector3(0, top, 0) })
                    {
                        Vector3 outward = probe.x != 0 ? new Vector3(Mathf.Sign(probe.x), 0, 0) : new Vector3(0, Mathf.Sign(probe.y), 0);
                        p2.RespawnAt(probe - outward * .5f, false); bool insideOk = !p2.OutsideArena;
                        p2.RespawnAt(probe + outward * .5f, false); bool outsideOk = p2.OutsideArena;
                        exact &= insideOk && outsideOk;
                    }
                    p2.RespawnAt(new Vector3(0, 1, 0), false);
                    p2.transform.position = new Vector3(0, 1, 40); // depth far beyond the historical front/back limit
                    bool noDepthKo = !p2.OutsideArena;
                    p2.RespawnAt(new Vector3(0, .05f, 0), false);
                    check(exact && noDepthKo, $"{name}: KO exacto a izquierda, derecha, abajo y arriba; sin KO en profundidad");

                    int[] lost = new int[4]; int sideIndex = 0;
                    foreach (var beyond in new[] { new Vector3(side + 1, 2, 0), new Vector3(-side - 1, 2, 0), new Vector3(0, bottom - 1, 0), new Vector3(0, top + 1, 0) })
                    {
                        match.ReturnToMenu(); match.ArenaIndex = index; match.OpponentCount = 1; match.StartMatch();
                        yield return new WaitForSeconds(3.2f);
                        p2 = match.Fighters[1]; int before = p2.Stocks;
                        p2.RespawnAt(beyond, false); yield return new WaitForSeconds(.1f);
                        lost[sideIndex++] = before - p2.Stocks;
                    }
                    check(Array.TrueForAll(lost, l => l == 1), $"{name}: cruzar cada uno de los cuatro límites descuenta exactamente una vida");
                    check(side > arena.CameraClamp.x && side - 8 >= 10 && top - 0 >= 15 && bottom <= -8,
                        $"{name}: límites coherentes con la arena: más allá del encuadre lateral, ±{side} m, {bottom} m abajo y {top} m arriba");

                    // ── ARENA-359: spawns ──
                    Vector3[] spawns = arena.SpawnPoints;
                    float spacing = float.PositiveInfinity; bool distinct = true;
                    for (int a = 0; a < spawns.Length; a++)
                        for (int b = a + 1; b < spawns.Length; b++)
                        { float d = Vector3.Distance(spawns[a], spawns[b]); spacing = Mathf.Min(spacing, d); distinct &= d > .01f; }
                    bool grounded = true, clear = true, planar = true;
                    foreach (var spawn in spawns)
                    {
                        planar &= Mathf.Abs(spawn.z) < .001f;
                        grounded &= Physics.Raycast(spawn + Vector3.up * 1f, Vector3.down, out RaycastHit hit, 1.6f, 1, QueryTriggerInteraction.Ignore) && Mathf.Abs(hit.point.y - spawn.y) < .35f;
                        clear &= !Physics.CheckCapsule(spawn + Vector3.up * (BrawlMoveData.HurtboxHalfWidth + .12f), spawn + Vector3.up * (BrawlMoveData.HurtboxHeight - BrawlMoveData.HurtboxHalfWidth),
                            BrawlMoveData.HurtboxHalfWidth * .9f, 1, QueryTriggerInteraction.Ignore);
                    }
                    row.spawns = spawns.Length; row.minSpacing = spacing;
                    check(spawns.Length >= 2 && spawns.Length <= BrawlMatch.MaxPlayers && distinct && spacing >= 2 && planar,
                        $"{name}: {spawns.Length} puntos de aparición en el plano, separados al menos {spacing:0.0} m");
                    check(grounded && clear, $"{name}: cada aparición está sobre suelo real y no se solapa con la geometría");
                    bool thin = true;
                    var layout = arena.CurrentSideLayout;
                    foreach (var platform in arena.OneWayPlatforms)
                        thin &= platform && platform.bounds.size.y <= BrawlArena.OneWayThickness + .05f && Mathf.Abs(platform.bounds.max.y - layout.PlatformTop) < .15f &&
                                Mathf.Abs(Mathf.Abs(platform.bounds.center.x) - layout.PlatformX) < .2f;
                    bool lineClear = !Physics.Linecast(new Vector3(-6, .85f, 0), new Vector3(-3.6f, .85f, 0), 1, QueryTriggerInteraction.Ignore) &&
                                     !Physics.Linecast(new Vector3(6, .85f, 0), new Vector3(3.6f, .85f, 0), 1, QueryTriggerInteraction.Ignore) &&
                                     !Physics.CheckSphere(new Vector3(-4.8f, .85f, 0), .22f, 1, QueryTriggerInteraction.Ignore);
                    check(thin && lineClear, $"{name}: plataformas atravesables finas en su sitio (±{layout.PlatformX} m, cara a {layout.PlatformTop} m); bajo ellas nada invisible bloquea golpes ni proyectiles");

                    // ── Distinct layout (GDD 2.0 §9): reachable with a double jump, a drop leads down, always in view ──
                    var empty = new bool[1];
                    bool platformsVisible = true;
                    foreach (var platform in arena.OneWayPlatforms)
                        foreach (var corner in new[] { new Vector3(platform.bounds.min.x, platform.bounds.max.y, 0), new Vector3(platform.bounds.max.x, platform.bounds.max.y + 1.7f, 0) })
                        {
                            Vector3 v = match.GroupCamera.View.WorldToViewportPoint(corner);
                            platformsVisible &= v.x >= 0 && v.x <= 1 && v.y >= 0 && v.y <= 1 - match.GroupCamera.TopReserve + .01f;
                        }
                    check(platformsVisible, $"{name}: la cámara muestra las dos plataformas aunque estén vacías");

                    match.ReturnToMenu(); match.SelectedCharacter = 2; match.ArenaIndex = index; match.StartTraining(); match.ShowCombatVolumes = false;
                    yield return null;
                    var climber = match.Fighters[0];
                    match.Fighters[1].ResetPractice(new Vector3(-2, .05f, 0), 0);
                    bool[] reached = new bool[2];
                    for (int s = 0; s < 2; s++)
                    {
                        float dir = s == 0 ? 1 : -1;
                        float start = Mathf.Min(layout.PlatformX - 2.2f, BrawlArena.MainFloorHalfWidth - .6f) * dir;
                        climber.ResetPractice(new Vector3(start, .05f, 0), 0); yield return new WaitForSeconds(.35f);
                        bool first = false, second = false;
                        for (float t = 0; t < 2.5f; t += Time.fixedDeltaTime)
                        {
                            float x = climber.transform.position.x;
                            var command = new BrawlCommand { Move = new Vector2(Mathf.Abs(x - layout.PlatformX * dir) > .4f ? Mathf.Sign(layout.PlatformX * dir - x) : 0, 0) };
                            // One press per jump: the first on the ground, the second near the apex.
                            if (!first) { command.Jump = true; first = true; }
                            else if (!second && climber.Velocity.y < .5f && !climber.Grounded) { command.Jump = true; second = true; }
                            climber.Submit(command);
                            yield return new WaitForFixedUpdate();
                            if (second && climber.Grounded) break;
                        }
                        reached[s] = climber.Grounded && Mathf.Abs(climber.transform.position.y - layout.PlatformTop) < .25f;
                        climber.ClearInput();
                    }
                    check(reached[0] && reached[1], $"{name}: el kit de peor alcance sube a ambas plataformas con doble salto");

                    climber.ResetPractice(new Vector3(layout.PlatformX, layout.PlatformTop + .05f, 0), 0); yield return new WaitForSeconds(.4f);
                    bool onTop = climber.Grounded;
                    climber.Submit(new BrawlCommand { Move = Vector2.down, Jump = true });
                    float lowest = climber.transform.position.y;
                    for (float t = 0; t < 1.2f; t += Time.fixedDeltaTime) { yield return new WaitForFixedUpdate(); lowest = Mathf.Min(lowest, climber.transform.position.y); }
                    bool overVoid = layout.PlatformX - 1.9f > BrawlArena.MainFloorHalfWidth - .4f;
                    bool dropOk = onTop && (overVoid ? lowest < -1f : climber.Grounded && climber.transform.position.y < .3f);
                    check(dropOk, overVoid ? $"{name}: bajar del balcón lleva al vacío (riesgo real de caída)" : $"{name}: bajar de la plataforma lleva al suelo principal");
                    match.SelectedCharacter = 0;

                    match.ReturnToMenu(); match.ArenaIndex = index; match.OpponentCount = 4; match.StartMatch();
                    yield return new WaitForSeconds(3.3f);
                    bool startOk = true;
                    foreach (var fighter in match.Fighters) startOk &= fighter.Grounded && Mathf.Abs(fighter.transform.position.z) < .001f;
                    check(startOk, $"{name}: cinco luchadores empiezan apoyados en sus apariciones");

                    // Respawn avoids points covered by active traps (and other fighters).
                    var owner = match.Fighters[0];
                    var token = new BrawlAttackToken(owner, match.Settings.Attack(AttackKind.Special, 4, 0));
                    int free = 2;
                    for (int i = 0; i < spawns.Length; i++)
                        if (i != free) traps.Add(BrawlTrap.Create(match, token, spawns[i]).gameObject);
                    foreach (var other in match.Fighters) if (other != match.Fighters[3]) other.RespawnAt(new Vector3(spawns[free].x > 0 ? -7.5f : 7.5f, .05f, 0), false);
                    var victim = match.Fighters[3];
                    victim.RespawnAt(new Vector3(0, bottom - 1, 0), false);
                    yield return new WaitForSeconds(match.Settings.respawnSeconds + .3f);
                    bool avoided = !victim.IsRespawning && Mathf.Abs(victim.transform.position.x - spawns[free].x) < .5f;
                    foreach (var trap in traps) if (trap) UnityEngine.Object.Destroy(trap);
                    traps.Clear();
                    check(avoided, $"{name}: la reaparición evita los puntos ocupados por trampas activas");
                    report.arenas.Add(row);
                }
                File.WriteAllText(Path.Combine(output, "arena-limits.json"), JsonUtility.ToJson(report, true));
                bool layoutsDiffer = true;
                for (int a = 0; a < BrawlArena.SideLayouts.Length; a++)
                    for (int b = a + 1; b < BrawlArena.SideLayouts.Length; b++)
                        layoutsDiffer &= Mathf.Abs(BrawlArena.SideLayouts[a].PlatformX - BrawlArena.SideLayouts[b].PlatformX) + Mathf.Abs(BrawlArena.SideLayouts[a].PlatformTop - BrawlArena.SideLayouts[b].PlatformTop) >= .8f;
                check(layoutsDiffer && BrawlArena.SideLayouts.Length == BrawlArena.Count, "Las cuatro arenas tienen distribuciones distintas (cada par difiere al menos 0,8 m en posición o altura)");
            }
            finally
            {
                foreach (var trap in traps) if (trap) UnityEngine.Object.Destroy(trap);
                match.ReturnToMenu(); match.ArenaIndex = oldArena; match.OpponentCount = oldOpponents; match.AutomatedTest = true;
            }
        }
    }
}
