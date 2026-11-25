using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// ARENA-366 in 2.5D: twenty full five-fighter matches per arena, played by the bots under the real rules
    /// (hits, knockback, ring-out, stock loss, respawn timer, safe spawn, protection, results), at least one hundred
    /// KOs per arena. To keep the run short, fighters in play are held at high damage so real hits launch them out,
    /// and the simulation runs faster than real time; nothing else is forced. Every KO and respawn is audited.
    /// </summary>
    public static class BrawlArenaStress
    {
        public const int MatchesPerArena = 20, MinKosPerArena = 100;
        private const float StressDamage = 300, TimeScale = 16, MatchCap = 330;
        // A bot's own chain (jump, a late air jump, then a recovery of 13.8 m/s with hover at a quarter gravity) can keep a
        // fighter airborne for roughly 3.5 s without any hit (estimate from BrawlSettings), so 3 s flagged legitimate play. Stuck is what matters: a fighter that
        // has not moved for StuckSeconds while airborne (a spawn that does not let go), or one still airborne after MaxAirSeconds.
        private const float MaxAirSeconds = 6, StuckSeconds = .5f, StuckDistance = .05f, LongAirSeconds = 3;

        [Serializable] private sealed class Row
        {
            public string arena; public int matches, finishedByStocks, finishedByTime, kos, credited, selfDestructs, respawns, landed;
            public float avgMatchSeconds, maxRespawnError, maxAirAfterRespawn; public int hitBeforeLanding, longAirAfterRespawn, objectGrowth; public float memoryGrowthMb;
            public List<string> problems = new List<string>();
        }
        [Serializable] private sealed class Report { public string version = BrawlMatch.ProductionVersion; public List<Row> arenas = new List<Row>(); }

        private sealed class Track { public int stocks; public float koAt = -1, respawnAt = -1, sampleAt; public bool waitingLand, longAir; public Vector3 sample; }

        public static IEnumerator Run(BrawlMatch match, Action<bool, string> check, string output)
        {
            int oldArena = match.ArenaIndex, oldOpponents = match.OpponentCount, oldCharacter = match.SelectedCharacter;
            bool oldAutomated = match.AutomatedTest; float oldScale = Time.timeScale;
            var settings = match.Settings;
            var report = new Report();
            try
            {
                for (int index = 0; index < BrawlArena.Count; index++)
                {
                    string name = BrawlArena.NameOf(index, match.UsesStageArt(index));
                    var row = new Row { arena = name };
                    int baselineObjects = 0; long baselineMemory = 0; float totalSeconds = 0;
                    bool placesOk = true, planeOk = true, stockStepOk = true, respawnOk = true, spawnOk = true, attributionOk = true;

                    for (int m = 0; m < MatchesPerArena; m++)
                    {
                        match.AutomatedTest = true; Time.timeScale = 1;
                        match.ReturnToMenu();
                        yield return null;
                        if (m == 1)
                        {
                            // Baseline after the first match was torn down: later matches must not keep accumulating objects.
                            GC.Collect(); baselineMemory = GC.GetTotalMemory(true);
                            baselineObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length;
                        }
                        match.ArenaIndex = index; match.OpponentCount = BrawlMatch.MaxPlayers - 1; match.SelectedCharacter = (index + m) % BrawlRoster.Count;
                        match.StartMatch();
                        match.AutomatedTest = false; // bots play; the probe isolates physical pads and P1 receives no input
                        Time.timeScale = TimeScale;
                        var fighters = match.Fighters;
                        var tracks = new Track[fighters.Length];
                        for (int i = 0; i < fighters.Length; i++) tracks[i] = new Track { stocks = fighters[i].Stocks };
                        float start = match.SimTime; int startStocks = 0;
                        foreach (var f in fighters) startStocks += f.Stocks;

                        while (match.Phase != MatchPhase.Results && match.SimTime - start < MatchCap)
                        {
                            yield return new WaitForFixedUpdate();
                            if (match.Phase != MatchPhase.Playing)
                            {
                                if (Time.frameCount % 600 == 0) Debug.Log($"FUNADOS_STRESS_PROGRESO fase={match.Phase} cuenta={match.CountdownRemaining:0.00} sim={match.SimTime:0.0}");
                                continue;
                            }
                            float now = match.SimTime;
                            for (int i = 0; i < fighters.Length; i++)
                            {
                                var f = fighters[i]; var t = tracks[i];
                                if (f.Stocks != t.stocks)
                                {
                                    if (t.stocks - f.Stocks != 1) { stockStepOk = false; row.problems.Add($"partida {m + 1}: P{i + 1} perdió {t.stocks - f.Stocks} vidas de golpe"); }
                                    if (t.waitingLand && f.Stocks > 0 && now - t.respawnAt < settings.protectionSeconds)
                                    { spawnOk = false; row.problems.Add($"partida {m + 1}: P{i + 1} cayó otra vez antes de aterrizar y con protección"); }
                                    row.kos++; t.stocks = f.Stocks; t.koAt = now; t.waitingLand = false;
                                    continue;
                                }
                                if (f.Eliminated) continue;
                                if (t.koAt >= 0 && !f.IsRespawning)
                                {
                                    // Respawned this step: on time, inside the limits, on the plane, protected.
                                    float error = Mathf.Abs(now - t.koAt - settings.respawnSeconds);
                                    row.maxRespawnError = Mathf.Max(row.maxRespawnError, error);
                                    Vector3 p = f.transform.position;
                                    bool ok = error <= Time.fixedDeltaTime * 2.5f && !f.OutsideArena && Mathf.Abs(p.z) < .001f && f.Invulnerable && !f.Hittable;
                                    if (!ok) { respawnOk = false; row.problems.Add($"partida {m + 1}: reaparición de P{i + 1} en {p} tras {now - t.koAt:0.00} s protegida={f.Invulnerable}"); }
                                    row.respawns++; t.koAt = -1; t.respawnAt = now; t.waitingLand = true; t.longAir = false; t.sample = p; t.sampleAt = now;
                                }
                                // Once protection ends a bot may legitimately launch the fighter before it lands: that is
                                // play, not a broken spawn. Protection itself is checked above (no hit, no second fall).
                                if (t.waitingLand && f.InHitstun && now - t.respawnAt >= settings.protectionSeconds)
                                { t.waitingLand = false; row.hitBeforeLanding++; }
                                if (t.waitingLand && f.Grounded)
                                {
                                    row.landed++; t.waitingLand = false;
                                    row.maxAirAfterRespawn = Mathf.Max(row.maxAirAfterRespawn, now - t.respawnAt);
                                }
                                if (t.waitingLand && !t.longAir && now - t.respawnAt > LongAirSeconds) { t.longAir = true; row.longAirAfterRespawn++; }
                                if (t.waitingLand && now - t.sampleAt >= StuckSeconds)
                                {
                                    Vector3 at = f.transform.position;
                                    if ((at - t.sample).magnitude < StuckDistance)
                                    { spawnOk = false; t.waitingLand = false; row.problems.Add($"partida {m + 1}: P{i + 1} quieto en el aire {StuckSeconds} s sin aterrizar tras reaparecer, en {at}"); }
                                    t.sample = at; t.sampleAt = now;
                                }
                                if (t.waitingLand && now - t.respawnAt > MaxAirSeconds)
                                { spawnOk = false; t.waitingLand = false; row.problems.Add($"partida {m + 1}: P{i + 1} no tocó suelo {MaxAirSeconds} s después de reaparecer en {f.transform.position}"); }
                                Vector3 q = f.transform.position;
                                if (!f.IsRespawning && (float.IsNaN(q.x) || float.IsNaN(q.y) || Mathf.Abs(q.z) > .001f)) planeOk = false;
                                // Keep fighters in play at high damage so that real bot hits produce KOs quickly.
                                if (!f.IsRespawning && f.Damage < StressDamage) f.SetPracticeDamage(StressDamage);
                            }
                        }
                        Time.timeScale = 1; match.AutomatedTest = true;
                        totalSeconds += match.SimTime - start;
                        Debug.Log($"FUNADOS_STRESS_PROGRESO {name} partida {m + 1}: fase={match.Phase} juego={match.SimTime - start:0.0} s KO acumulados={row.kos} real={Time.realtimeSinceStartup:0} s");
                        row.matches++;

                        int lost = 0, kos = 0, self = 0, winners = 0;
                        var places = new List<int>();
                        foreach (var f in fighters) { lost += startStocks / fighters.Length - f.Stocks; kos += f.KOs; self += f.SelfDestructs; places.Add(f.Place); if (f.Place == 1) winners++; }
                        row.credited += kos; row.selfDestructs += self;
                        if (kos + self != lost) { attributionOk = false; row.problems.Add($"partida {m + 1}: {lost} vidas perdidas pero {kos} KO + {self} autodestrucciones"); }
                        if (match.Phase != MatchPhase.Results) { placesOk = false; row.problems.Add($"partida {m + 1}: no terminó en {MatchCap} s de juego"); }
                        else
                        {
                            int survivors = 0; foreach (var f in fighters) if (!f.Eliminated) survivors++;
                            if (survivors <= 1) row.finishedByStocks++; else row.finishedByTime++;
                            bool valid = winners >= 1 && places.TrueForAll(pl => pl >= 1 && pl <= fighters.Length);
                            // Standard competition ranking (1,2,3,3,5): fighters out in the same step share a place, and
                            // each place is one more than the number of fighters ranked strictly ahead.
                            places.Sort();
                            for (int k = 0; k < places.Count; k++) valid &= places[k] == (k > 0 && places[k] == places[k - 1] ? places[k - 1] : k + 1);
                            if (!valid) { placesOk = false; row.problems.Add($"partida {m + 1}: puestos inválidos {string.Join(",", places)}"); }
                        }
                    }

                    match.ReturnToMenu(); yield return null;
                    GC.Collect();
                    row.memoryGrowthMb = (GC.GetTotalMemory(true) - baselineMemory) / (1024f * 1024f);
                    row.objectGrowth = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length - baselineObjects;
                    row.avgMatchSeconds = totalSeconds / Mathf.Max(1, row.matches);
                    report.arenas.Add(row);

                    check(row.matches == MatchesPerArena && placesOk,
                        $"{name}: {row.matches} partidas completas de cinco luchadores con resultados válidos ({row.finishedByStocks} por vidas, {row.finishedByTime} por tiempo)");
                    check(row.kos >= MinKosPerArena && stockStepOk && attributionOk,
                        $"{name}: {row.kos} KO reales (≥{MinKosPerArena}); cada uno descuenta una vida y se atribuye ({row.credited} KO, {row.selfDestructs} autodestrucciones)");
                    check(respawnOk && row.respawns > 0 && row.respawns <= row.kos,
                        $"{name}: {row.respawns} reapariciones a tiempo ({settings.respawnSeconds} s, error máx. {row.maxRespawnError:0.000} s), dentro de los límites, en el plano y protegidas");
                    check(spawnOk,
                        $"{name}: tras reaparecer se aterriza en suelo real (máx. {row.maxAirAfterRespawn:0.00} s en el aire; {row.longAirAfterRespawn} más de {LongAirSeconds} s por su propio movimiento; nadie quieto en el aire ni más de {MaxAirSeconds} s; {row.hitBeforeLanding} golpeados al acabar la protección antes de aterrizar) y nadie cae de nuevo protegido");
                    check(planeOk, $"{name}: ninguna posición inválida ni fuera del plano lateral en {row.matches} partidas");
                    check(row.objectGrowth <= 5 && row.memoryGrowthMb < 32,
                        $"{name}: sin fugas entre partidas ({row.objectGrowth:+0;-0;0} objetos, {row.memoryGrowthMb:+0.0;-0.0;0} MB tras {row.matches - 1} partidas)");
                    foreach (var problem in row.problems.GetRange(0, Mathf.Min(5, row.problems.Count))) Debug.Log("FUNADOS_STRESS " + name + ": " + problem);
                }
                File.WriteAllText(Path.Combine(output, "arena-stress.json"), JsonUtility.ToJson(report, true));
            }
            finally
            {
                Time.timeScale = oldScale; match.AutomatedTest = oldAutomated;
                match.ReturnToMenu(); match.ArenaIndex = oldArena; match.OpponentCount = oldOpponents; match.SelectedCharacter = oldCharacter;
            }
        }
    }
}
