using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Unity.Profiling;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// QA-435–440 (GDD 2.0 §14): performance measured in the Windows build with five bot fighters, at real speed
    /// (Time.timeScale = 1, 60 Hz simulation) and full effects. <see cref="RunPerf"/> plays every arena and records
    /// average FPS, 1 % low, frame-time peaks, CPU main/render thread and GPU time (median, p95, peak, from
    /// FrameTimingManager), managed and native memory, arena load and return-to-menu time. <see cref="RunSoak"/> is the
    /// 30-minute session with chained rematches: no lock-ups, no errors and no growth of objects or memory.
    /// To keep the effects at their maximum, bots get a full super meter every few seconds and fighters in play are
    /// held at high damage so real hits launch and KO them; nothing else is forced.
    /// </summary>
    public static class BrawlPerfSuite
    {
        // GDD 2.0: 60 FPS sustained, 60 Hz simulation, CPU main thread and GPU each under 13.5 ms (median, p95, peaks).
        public const float BudgetMs = 13.5f, TargetFps = 60;
        // Proposed (the GDD fixes no number for these): see CONTINUAR.md «Relevo perf/rendimiento».
        public const float MinAvgFps = 58, MinLowFps = 45, MaxPlayFrameMs = 100, MaxArenaLoadSeconds = 2, MaxMenuSeconds = .5f;
        public const float MaxManagedMb = 256, MaxProcessMb = 2048;
        public const float PerfSecondsPerArena = 60, SoakSeconds = 1800;
        public const int SoakMatchesPerArena = 3, ArenaCycles = 5;
        /// <summary>Soak match clock: bots rarely finish 300 s matches by stocks, so the session plays 90 s matches to chain many rematches.</summary>
        public const float SoakMatchSeconds = 90;
        private const float HeldDamage = 130, MeterEvery = 4, WarmupSeconds = 1.5f;

        // ───────────── Report data ─────────────
        [Serializable] public sealed class Dist { public float median, p95, p99, max, mean; public int samples; }
        [Serializable] public sealed class Memory
        {
            public float managedMb, gcReservedMb, unityTotalUsedMb, unityTotalReservedMb, systemUsedMb, processPrivateMb, processWorkingSetMb;
            public int gameObjects, allObjects;
        }
        [Serializable] public sealed class PerfRow
        {
            public string arena; public int fighters; public float seconds; public int frames;
            public float avgFps, low1Fps, maxFrameMs; public int framesOver20ms, framesOver33ms;
            public Dist frameMs, cpuMainMs, cpuRenderMs, gpuMs, gcAllocKbPerFrame;
            public float simRatio, fixedStepsPerSecond; public int kos, supers, rematches;
            public List<string> hitches = new List<string>(), pauses = new List<string>(); public bool lockup;
            public float arenaLoadSeconds, firstFrameMs, menuReturnSeconds;
            public Memory memoryPeak, memoryAfterMenu;
        }
        [Serializable] public sealed class PerfReport
        {
            public string version = BrawlMatch.ProductionVersion, gpu, cpu, graphicsApi, startedUtc;
            public int processors, systemMemoryMb, graphicsMemoryMb, screenWidth, screenHeight, targetFrameRate, vSyncCount;
            public bool frameTimingAvailable, gpuTimingAvailable, reducedEffects; public float timeScale, budgetMs = BudgetMs;
            public string thresholds = $"FPS medio ≥{MinAvgFps}; 1 % bajo ≥{MinLowFps}; CPU principal y GPU mediana y p95 <{BudgetMs} ms; frame máx. <{MaxPlayFrameMs} ms; " +
                                       $"carga de arena <{MaxArenaLoadSeconds} s; vuelta al menú <{MaxMenuSeconds} s; gestionada <{MaxManagedMb} MB; proceso <{MaxProcessMb} MB";
            public List<PerfRow> arenas = new List<PerfRow>();
            public Dist allFrameMs, allCpuMainMs, allGpuMs; public float allAvgFps, allLow1Fps;
            public List<int> arenaCycleObjects = new List<int>(); public string arenaCycleGrowth;
        }
        [Serializable] public sealed class SoakSample
        {
            public int match; public string arena; public string kind; public float realSeconds, matchGameSeconds; public bool finishedByStocks; public int kos;
            public Memory memory; public string growth;
        }
        [Serializable] public sealed class SoakReport
        {
            public string version = BrawlMatch.ProductionVersion, startedUtc; public float targetSeconds = SoakSeconds, realSeconds;
            public int matches, rematches, menuReturns, arenaChanges, kos, errors, hitchesOver100ms, hitchesOver250ms, lockups;
            public float avgFps, low1Fps, maxFrameMs, longestMatchGameSeconds;
            public Dist frameMs, cpuMainMs, gpuMs;
            public float matchSeconds = SoakMatchSeconds; public List<string> hitches = new List<string>(), pauses = new List<string>();
            public int maxGameObjectGrowth, maxAllObjectGrowth; public float managedGrowthMb, processGrowthMb, managedSlopeMbPer10Min, processSlopeMbPer10Min;
            public List<string> problems = new List<string>();
            public List<SoakSample> samples = new List<SoakSample>();
        }

        // ───────────── Sampling ─────────────
        private sealed class Sampler : IDisposable
        {
            public readonly List<float> frame = new List<float>(), cpuMain = new List<float>(), cpuRender = new List<float>(), gpu = new List<float>(), gcAlloc = new List<float>();
            private readonly FrameTiming[] timing = new FrameTiming[1];
            private ProfilerRecorder gcAllocRecorder;
            public bool frameTimingSeen, gpuSeen;
            public Sampler() { gcAllocRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame"); }
            public void Clear() { frame.Clear(); cpuMain.Clear(); cpuRender.Clear(); gpu.Clear(); gcAlloc.Clear(); }
            /// <summary>Call once per rendered frame.</summary>
            public void Sample()
            {
                frame.Add(Time.unscaledDeltaTime * 1000f);
                FrameTimingManager.CaptureFrameTimings();
                if (FrameTimingManager.GetLatestTimings(1, timing) > 0)
                {
                    var t = timing[0];
                    if (t.cpuMainThreadFrameTime > 0) { frameTimingSeen = true; cpuMain.Add((float)t.cpuMainThreadFrameTime); }
                    if (t.cpuRenderThreadFrameTime > 0) cpuRender.Add((float)t.cpuRenderThreadFrameTime);
                    if (t.gpuFrameTime > 0) { gpuSeen = true; gpu.Add((float)t.gpuFrameTime); }
                }
                if (gcAllocRecorder.Valid) gcAlloc.Add(gcAllocRecorder.LastValue / 1024f);
            }
            public void Dispose() => gcAllocRecorder.Dispose();
        }

        private static Dist Distribution(List<float> values)
        {
            var d = new Dist { samples = values.Count };
            if (values.Count == 0) return d;
            var sorted = new List<float>(values); sorted.Sort();
            float sum = 0; foreach (float v in sorted) sum += v;
            d.mean = sum / sorted.Count; d.max = sorted[sorted.Count - 1];
            d.median = Percentile(sorted, .5f); d.p95 = Percentile(sorted, .95f); d.p99 = Percentile(sorted, .99f);
            return d;
        }
        private static float Percentile(List<float> sorted, float q)
        {
            float pos = q * (sorted.Count - 1); int lo = Mathf.FloorToInt(pos), hi = Mathf.Min(sorted.Count - 1, lo + 1);
            return Mathf.Lerp(sorted[lo], sorted[hi], pos - lo);
        }
        private static float AvgFps(List<float> frameMs) { float sum = 0; foreach (float v in frameMs) sum += v; return sum > 0 ? frameMs.Count * 1000f / sum : 0; }
        /// <summary>1 % low: the FPS equivalent of the mean of the slowest 1 % of frames.</summary>
        private static float LowFps(List<float> frameMs)
        {
            if (frameMs.Count == 0) return 0;
            var sorted = new List<float>(frameMs); sorted.Sort();
            int n = Mathf.Max(1, sorted.Count / 100); float sum = 0;
            for (int i = sorted.Count - n; i < sorted.Count; i++) sum += sorted[i];
            return 1000f / (sum / n);
        }

        private const float Mb = 1024f * 1024f;
        private static float Counter(string name)
        {
            using (var r = ProfilerRecorder.StartNew(ProfilerCategory.Memory, name)) return r.Valid ? r.CurrentValue / Mb : -1;
        }
        public static Memory Snapshot(bool countObjects)
        {
            var m = new Memory
            {
                managedMb = GC.GetTotalMemory(false) / Mb,
                gcReservedMb = Counter("GC Reserved Memory"), unityTotalUsedMb = Counter("Total Used Memory"),
                unityTotalReservedMb = Counter("Total Reserved Memory"), systemUsedMb = Counter("System Used Memory"),
            };
            ProcessMemory(out m.processPrivateMb, out m.processWorkingSetMb);
            if (countObjects)
            {
                m.gameObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
                m.allObjects = Resources.FindObjectsOfTypeAll<UnityEngine.Object>().Length;
            }
            return m;
        }
        // Mono's Process.PrivateMemorySize64 reads 0 on Windows; ask the OS directly (committed private bytes, working set).
        [StructLayout(LayoutKind.Sequential)] private struct ProcessMemoryCounters
        {
            public uint cb, PageFaultCount; public UIntPtr PeakWorkingSetSize, WorkingSetSize, QuotaPeakPagedPoolUsage, QuotaPagedPoolUsage,
                QuotaPeakNonPagedPoolUsage, QuotaNonPagedPoolUsage, PagefileUsage, PeakPagefileUsage, PrivateUsage;
        }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [DllImport("kernel32.dll")] private static extern IntPtr GetCurrentProcess();
        [DllImport("kernel32.dll")] private static extern bool K32GetProcessMemoryInfo(IntPtr process, out ProcessMemoryCounters counters, uint size);
#endif
        private static void ProcessMemory(out float privateMb, out float workingSetMb)
        {
            privateMb = workingSetMb = -1;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                if (K32GetProcessMemoryInfo(GetCurrentProcess(), out var c, (uint)Marshal.SizeOf<ProcessMemoryCounters>()))
                { privateMb = c.PrivateUsage.ToUInt64() / Mb; workingSetMb = c.WorkingSetSize.ToUInt64() / Mb; }
            }
            catch (Exception) { }
#endif
        }
        /// <summary>Census of every loaded Unity object by type and name, to name what grows when the count does.</summary>
        public static Dictionary<string, int> Census()
        {
            var counts = new Dictionary<string, int>();
            foreach (var o in Resources.FindObjectsOfTypeAll<UnityEngine.Object>())
            {
                if (o == null) continue;
                string key = o.GetType().Name + " «" + o.name + "»";
                counts.TryGetValue(key, out int n); counts[key] = n + 1;
            }
            return counts;
        }
        public static string CensusGrowth(Dictionary<string, int> before, Dictionary<string, int> after, int top = 12)
        {
            var growth = new List<KeyValuePair<string, int>>();
            foreach (var pair in after) { before.TryGetValue(pair.Key, out int b); if (pair.Value > b) growth.Add(new KeyValuePair<string, int>(pair.Key, pair.Value - b)); }
            growth.Sort((x, y) => y.Value.CompareTo(x.Value));
            var parts = new List<string>();
            for (int i = 0; i < Mathf.Min(top, growth.Count); i++) parts.Add($"{growth[i].Key} +{growth[i].Value}");
            return growth.Count == 0 ? "ninguno" : string.Join("; ", parts);
        }
        private static Memory Max(Memory a, Memory b)
        {
            if (a == null) return b;
            return new Memory
            {
                managedMb = Mathf.Max(a.managedMb, b.managedMb), gcReservedMb = Mathf.Max(a.gcReservedMb, b.gcReservedMb),
                unityTotalUsedMb = Mathf.Max(a.unityTotalUsedMb, b.unityTotalUsedMb), unityTotalReservedMb = Mathf.Max(a.unityTotalReservedMb, b.unityTotalReservedMb),
                systemUsedMb = Mathf.Max(a.systemUsedMb, b.systemUsedMb), processPrivateMb = Mathf.Max(a.processPrivateMb, b.processPrivateMb),
                processWorkingSetMb = Mathf.Max(a.processWorkingSetMb, b.processWorkingSetMb),
                gameObjects = Mathf.Max(a.gameObjects, b.gameObjects), allObjects = Mathf.Max(a.allObjects, b.allObjects)
            };
        }

        /// <summary>Full effects during play: give bots their super and keep fighters launchable by real hits.</summary>
        private static int Stoke(BrawlMatch match, ref float nextMeter)
        {
            int supers = 0; bool refill = match.SimTime >= nextMeter;
            if (refill) nextMeter = match.SimTime + MeterEvery;
            foreach (var f in match.Fighters)
            {
                if (f == null || f.Eliminated || f.IsRespawning) continue;
                if (refill && f.Meter < 100) { f.SetMeterForTest(100); supers++; }
                if (f.Damage < HeldDamage) f.SetPracticeDamage(HeldDamage);
            }
            return supers;
        }
        private static int CountKos(BrawlMatch match) { int k = 0; foreach (var f in match.Fighters) if (f != null) k += f.KOs + f.SelfDestructs; return k; }

        /// <summary>
        /// Real keyboard and mouse are disabled for the run (the probe reads input in the background, so a stray Esc
        /// elsewhere on the machine would pause the match) and every fighter, P1 included, is a bot.
        /// </summary>
        private static void IsolateInput(bool isolate)
        {
            foreach (var device in UnityEngine.InputSystem.InputSystem.devices)
                if (device is UnityEngine.InputSystem.Keyboard || device is UnityEngine.InputSystem.Mouse)
                {
                    if (isolate && device.enabled) UnityEngine.InputSystem.InputSystem.DisableDevice(device);
                    else if (!isolate && !device.enabled) UnityEngine.InputSystem.InputSystem.EnableDevice(device);
                }
        }
        private static void AllBots(BrawlMatch match) { foreach (var f in match.Fighters) if (f != null) f.MakeBotForTest(); }
        /// <summary>An unexpected pause is resumed and logged; returns false while it cannot be resumed.</summary>
        private static bool ResumeIfPaused(BrawlMatch match, List<string> log)
        {
            if (match.Phase != MatchPhase.Paused) return true;
            if (log.Count < 20) log.Add($"{DateTime.Now:HH:mm:ss} pausa imprevista (dispositivo={match.DevicePause}, faltan={match.MissingDeviceNames})");
            match.TogglePause();
            return match.Phase != MatchPhase.Paused;
        }

        // ───────────── QA-435–440: per-arena measurement ─────────────
        public static IEnumerator RunPerf(BrawlMatch match, Action<bool, string> check, string output)
        {
            int oldArena = match.ArenaIndex, oldOpponents = match.OpponentCount, oldCharacter = match.SelectedCharacter;
            bool oldAutomated = match.AutomatedTest, oldReduced = BrawlFeedback.ReducedEffects; float oldScale = Time.timeScale;
            var report = new PerfReport
            {
                gpu = SystemInfo.graphicsDeviceName, cpu = SystemInfo.processorType, graphicsApi = SystemInfo.graphicsDeviceVersion,
                processors = SystemInfo.processorCount, systemMemoryMb = SystemInfo.systemMemorySize, graphicsMemoryMb = SystemInfo.graphicsMemorySize,
                screenWidth = Screen.width, screenHeight = Screen.height, targetFrameRate = Application.targetFrameRate, vSyncCount = QualitySettings.vSyncCount,
                startedUtc = DateTime.UtcNow.ToString("o")
            };
            var sampler = new Sampler();
            var all = new Sampler();
            try
            {
                BrawlFeedback.ReducedEffects = false; Time.timeScale = 1; IsolateInput(true);
                report.reducedEffects = BrawlFeedback.ReducedEffects; report.timeScale = Time.timeScale;
                // Warm the arenas once (shaders, prefabs) so a first-load spike in one arena does not mask the others.
                for (int index = 0; index < BrawlArena.Count; index++)
                {
                    match.AutomatedTest = true; match.ArenaIndex = index; match.OpponentCount = BrawlMatch.MaxPlayers - 1; match.StartMatch();
                    for (int k = 0; k < 10; k++) yield return null;
                    match.ReturnToMenu(); yield return null;
                }
                for (int index = 0; index < BrawlArena.Count; index++)
                {
                    string name = BrawlArena.NameOf(index, match.UsesStageArt(index));
                    var row = new PerfRow { arena = name };
                    match.AutomatedTest = true; match.ReturnToMenu();
                    // Load from another arena so the build path really runs (the menu keeps the last arena).
                    match.ArenaIndex = (index + 1) % BrawlArena.Count; match.OpponentCount = 1; match.StartMatch(); yield return null;
                    match.ReturnToMenu(); yield return null; yield return null;

                    match.ArenaIndex = index; match.OpponentCount = BrawlMatch.MaxPlayers - 1; match.SelectedCharacter = index % BrawlRoster.Count;
                    var clock = System.Diagnostics.Stopwatch.StartNew();
                    match.StartMatch();
                    yield return new WaitForEndOfFrame();
                    row.arenaLoadSeconds = (float)clock.Elapsed.TotalSeconds;
                    yield return null; row.firstFrameMs = Time.unscaledDeltaTime * 1000f;
                    row.fighters = match.Fighters.Length; AllBots(match);
                    match.AutomatedTest = false; // bots play; the probe isolates physical pads and P1 receives no input

                    while (match.Phase == MatchPhase.Countdown || match.Phase == MatchPhase.Paused) { ResumeIfPaused(match, row.pauses); yield return null; }
                    float warm = Time.realtimeSinceStartup + WarmupSeconds; float nextMeter = 0;
                    while (Time.realtimeSinceStartup < warm) { Stoke(match, ref nextMeter); yield return null; }

                    // Only frames of live play are measured; a match won by stocks chains a rematch (its load is not a play frame).
                    sampler.Clear();
                    float playReal = 0, playSim = 0, lastSim = match.SimTime; int kosBefore = 0, kos0 = CountKos(match);
                    Memory peak = null; float nextMemory = 0; int gcSeen = GC.CollectionCount(0);
                    float lastProgress = Time.realtimeSinceStartup;
                    while (playReal < PerfSecondsPerArena)
                    {
                        yield return null;
                        if (Time.realtimeSinceStartup - lastProgress > 30) { row.lockup = true; Debug.Log($"FUNADOS_PERF {name}: sin avance 30 s en fase {match.Phase}"); break; }
                        if (!ResumeIfPaused(match, row.pauses)) continue;
                        if (match.Phase == MatchPhase.Results)
                        {
                            kosBefore += CountKos(match) - kos0; row.rematches++;
                            match.StartMatch(); AllBots(match); kos0 = 0; lastSim = -1; continue;
                        }
                        if (match.Phase == MatchPhase.Countdown) lastProgress = Time.realtimeSinceStartup;
                        if (match.Phase != MatchPhase.Playing) { lastSim = -1; continue; }
                        if (lastSim < 0) { lastSim = match.SimTime; continue; }
                        sampler.Sample(); all.Sample();
                        int gcNow = GC.CollectionCount(0);
                        if (Time.unscaledDeltaTime > .05f && row.hitches.Count < 20)
                            row.hitches.Add($"{DateTime.Now:HH:mm:ss.fff} juego {match.SimTime:0.00} s: {Time.unscaledDeltaTime * 1000:0} ms{(gcNow != gcSeen ? ", con GC" : ", sin GC")}");
                        gcSeen = gcNow;
                        lastProgress = Time.realtimeSinceStartup;
                        playReal += Time.unscaledDeltaTime; playSim += match.SimTime - lastSim; lastSim = match.SimTime;
                        row.supers += Stoke(match, ref nextMeter);
                        if (Time.realtimeSinceStartup >= nextMemory) { nextMemory = Time.realtimeSinceStartup + 5; peak = Max(peak, Snapshot(false)); }
                    }
                    row.seconds = playReal;
                    row.simRatio = playSim / Mathf.Max(.001f, playReal);
                    row.fixedStepsPerSecond = row.simRatio / Time.fixedDeltaTime;
                    row.kos = kosBefore + CountKos(match) - kos0;
                    row.frames = sampler.frame.Count;
                    row.frameMs = Distribution(sampler.frame); row.cpuMainMs = Distribution(sampler.cpuMain); row.cpuRenderMs = Distribution(sampler.cpuRender);
                    row.gpuMs = Distribution(sampler.gpu); row.gcAllocKbPerFrame = Distribution(sampler.gcAlloc);
                    row.avgFps = AvgFps(sampler.frame); row.low1Fps = LowFps(sampler.frame); row.maxFrameMs = row.frameMs.max;
                    foreach (float v in sampler.frame) { if (v > 20) row.framesOver20ms++; if (v > 33.4f) row.framesOver33ms++; }
                    row.memoryPeak = Max(peak, Snapshot(true));

                    match.AutomatedTest = true;
                    clock.Restart();
                    match.ReturnToMenu();
                    yield return new WaitForEndOfFrame();
                    row.menuReturnSeconds = (float)clock.Elapsed.TotalSeconds;
                    yield return null; GC.Collect(); yield return null;
                    row.memoryAfterMenu = Snapshot(true);
                    report.arenas.Add(row);
                    foreach (var hitch in row.hitches) Debug.Log($"FUNADOS_PERF tirón {name}: {hitch}");
                    Debug.Log($"FUNADOS_PERF {name}: {row.avgFps:0.0} fps (1 % bajo {row.low1Fps:0.0}), frame máx. {row.maxFrameMs:0.0} ms, CPU {row.cpuMainMs.median:0.00}/{row.cpuMainMs.p95:0.00}/{row.cpuMainMs.max:0.00} ms, " +
                              $"GPU {row.gpuMs.median:0.00}/{row.gpuMs.p95:0.00}/{row.gpuMs.max:0.00} ms, carga {row.arenaLoadSeconds:0.000} s, menú {row.menuReturnSeconds:0.000} s, KO {row.kos}, supers {row.supers}");

                    foreach (var pause in row.pauses) Debug.Log($"FUNADOS_PERF {name}: {pause}");
                    check(!row.lockup && row.fighters == BrawlMatch.MaxPlayers && row.seconds >= PerfSecondsPerArena - .5f && row.kos > 0 && Mathf.Abs(row.simRatio - 1) < .02f,
                        $"{name}: {row.fighters} luchadores bot a velocidad real {row.seconds:0} s, simulación {row.fixedStepsPerSecond:0.0} Hz ({row.kos} KO, {row.supers} supers, efectos completos)");
                    check(row.avgFps >= MinAvgFps && row.low1Fps >= MinLowFps,
                        $"{name}: {row.avgFps:0.0} FPS medios (≥{MinAvgFps}), 1 % bajo {row.low1Fps:0.0} (≥{MinLowFps}); frame mediana {row.frameMs.median:0.00} ms, p95 {row.frameMs.p95:0.00} ms");
                    check(report.frameTimingAvailable = sampler.frameTimingSeen,
                        $"{name}: FrameTimingManager mide CPU y GPU en la build ({row.cpuMainMs.samples} muestras de CPU, {row.gpuMs.samples} de GPU)");
                    check(row.cpuMainMs.samples > 0 && row.cpuMainMs.median < BudgetMs && row.cpuMainMs.p95 < BudgetMs,
                        $"{name}: CPU principal mediana {row.cpuMainMs.median:0.00} ms, p95 {row.cpuMainMs.p95:0.00} ms (<{BudgetMs}), pico {row.cpuMainMs.max:0.00} ms; hilo de render p95 {row.cpuRenderMs.p95:0.00} ms");
                    check(row.gpuMs.samples > 0 && row.gpuMs.median < BudgetMs && row.gpuMs.p95 < BudgetMs,
                        $"{name}: GPU mediana {row.gpuMs.median:0.00} ms, p95 {row.gpuMs.p95:0.00} ms (<{BudgetMs}), pico {row.gpuMs.max:0.00} ms");
                    check(row.maxFrameMs < MaxPlayFrameMs,
                        $"{name}: pico de frame {row.maxFrameMs:0.0} ms (<{MaxPlayFrameMs}); {row.framesOver20ms} frames >20 ms y {row.framesOver33ms} >33 ms de {row.frames}");
                    check(row.arenaLoadSeconds < MaxArenaLoadSeconds && row.menuReturnSeconds < MaxMenuSeconds,
                        $"{name}: carga de arena con cinco luchadores {row.arenaLoadSeconds * 1000:0} ms (<{MaxArenaLoadSeconds} s), primer frame {row.firstFrameMs:0} ms; vuelta al menú {row.menuReturnSeconds * 1000:0} ms (<{MaxMenuSeconds} s)");
                    check(row.memoryPeak.managedMb < MaxManagedMb && row.memoryPeak.processPrivateMb > 0 && row.memoryPeak.processPrivateMb < MaxProcessMb,
                        $"{name}: memoria gestionada pico {row.memoryPeak.managedMb:0.0} MB (<{MaxManagedMb}), nativa Unity {row.memoryPeak.unityTotalUsedMb:0} MB, proceso {row.memoryPeak.processPrivateMb:0} MB (<{MaxProcessMb}); " +
                        $"asignación GC mediana {row.gcAllocKbPerFrame.median:0.0} KB/frame");
                }
                // Arena changes must not leave objects behind: several full cycles through the four arenas, census after each.
                {
                    match.AutomatedTest = true;
                    var counts = new List<int>(); Dictionary<string, int> firstCensus = null, lastCensus = null;
                    for (int cycle = 0; cycle < ArenaCycles; cycle++)
                    {
                        for (int index = 0; index < BrawlArena.Count; index++)
                        {
                            match.ArenaIndex = index; match.OpponentCount = BrawlMatch.MaxPlayers - 1; match.SelectedCharacter = (index + cycle) % BrawlRoster.Count;
                            match.StartMatch(); AllBots(match); match.AutomatedTest = false;
                            for (int k = 0; k < 90; k++) yield return null; // into play: countdown, first hits
                            match.AutomatedTest = true; match.ReturnToMenu(); yield return null;
                        }
                        match.ArenaIndex = 0; match.StartMatch(); yield return null; match.ReturnToMenu(); // same resting arena for every census
                        yield return null; yield return null; GC.Collect(); yield return null;
                        var census = Census(); int total = 0; foreach (int v in census.Values) total += v;
                        counts.Add(total);
                        if (cycle == 1) firstCensus = census; // cycle 0 warms caches
                        lastCensus = census;
                    }
                    report.arenaCycleObjects = counts;
                    report.arenaCycleGrowth = firstCensus != null ? CensusGrowth(firstCensus, lastCensus) : "";
                    int growth = counts.Count > 1 ? counts[counts.Count - 1] - counts[1] : 0;
                    Debug.Log($"FUNADOS_PERF ciclos de arena: objetos {string.Join(" → ", counts)}; crecimiento {report.arenaCycleGrowth}");
                    check(counts.Count == ArenaCycles && growth <= 2,
                        $"Cambiar de arena no deja objetos atrás: {ArenaCycles} ciclos por las {BrawlArena.Count} arenas, {growth:+0;-0;0} objetos de Unity tras {ArenaCycles - 2} ciclos ({report.arenaCycleGrowth})");
                }
                report.gpuTimingAvailable = all.gpuSeen;
                report.allFrameMs = Distribution(all.frame); report.allCpuMainMs = Distribution(all.cpuMain); report.allGpuMs = Distribution(all.gpu);
                report.allAvgFps = AvgFps(all.frame); report.allLow1Fps = LowFps(all.frame);
                check(report.arenas.Count == BrawlArena.Count && !report.reducedEffects && report.timeScale == 1,
                    $"Rendimiento medido en las {report.arenas.Count} arenas sin acelerar el tiempo y con efectos completos: {report.allAvgFps:0.0} FPS medios, 1 % bajo {report.allLow1Fps:0.0}, " +
                    $"CPU p95 {report.allCpuMainMs.p95:0.00} ms, GPU p95 {report.allGpuMs.p95:0.00} ms ({report.gpu})");
                File.WriteAllText(Path.Combine(output, "perf-smoke.json"), JsonUtility.ToJson(report, true));
            }
            finally
            {
                sampler.Dispose(); all.Dispose(); IsolateInput(false);
                Time.timeScale = oldScale; match.AutomatedTest = oldAutomated; BrawlFeedback.ReducedEffects = oldReduced;
                match.ReturnToMenu(); match.ArenaIndex = oldArena; match.OpponentCount = oldOpponents; match.SelectedCharacter = oldCharacter;
            }
        }

        // ───────────── 30-minute session with chained rematches ─────────────
        public static IEnumerator RunSoak(BrawlMatch match, Action<bool, string> check, string output, float seconds = SoakSeconds)
        {
            int oldArena = match.ArenaIndex, oldOpponents = match.OpponentCount, oldCharacter = match.SelectedCharacter;
            bool oldAutomated = match.AutomatedTest, oldReduced = BrawlFeedback.ReducedEffects; float oldScale = Time.timeScale;
            var report = new SoakReport { targetSeconds = seconds, startedUtc = DateTime.UtcNow.ToString("o") };
            int errors = 0, gcSeen = GC.CollectionCount(0);
            Application.LogCallback onLog = (message, stack, type) => { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++; };
            Application.logMessageReceived += onLog;
            var sampler = new Sampler();
            var firstByArena = new Dictionary<int, Memory>(); var censusByArena = new Dictionary<int, Dictionary<string, int>>();
            float oldMatchSeconds = match.Settings.matchSeconds;
            float limit = SoakMatchSeconds + 3 + 20; // countdown + margin, in game seconds
            try
            {
                BrawlFeedback.ReducedEffects = false; Time.timeScale = 1; match.Settings.matchSeconds = SoakMatchSeconds; IsolateInput(true);
                float start = Time.realtimeSinceStartup;
                int arena = 0, character = 0;
                match.AutomatedTest = true; match.ReturnToMenu(); yield return null;
                while (Time.realtimeSinceStartup - start < seconds)
                {
                    int m = report.matches;
                    string kind;
                    if (m == 0 || m % SoakMatchesPerArena != 0)
                    {
                        kind = m == 0 ? "inicio" : "revancha";
                        if (m > 0) report.rematches++;
                        match.ArenaIndex = arena; match.OpponentCount = BrawlMatch.MaxPlayers - 1; match.SelectedCharacter = character;
                    }
                    else
                    {
                        // Every few rematches: back to the menu, next arena and character, as a player would.
                        kind = "menú"; report.menuReturns++; report.arenaChanges++;
                        match.ReturnToMenu();
                        for (int k = 0; k < 30; k++) yield return null;
                        arena = (arena + 1) % BrawlArena.Count; character = (character + 1) % BrawlRoster.Count;
                        match.ArenaIndex = arena; match.OpponentCount = BrawlMatch.MaxPlayers - 1; match.SelectedCharacter = character;
                    }
                    // Rematch from the results screen is the same call as the HUD's REVANCHA button.
                    match.StartMatch(); AllBots(match);
                    match.AutomatedTest = false;
                    float matchStartSim = match.SimTime, lastSim = -1, stalledSince = Time.realtimeSinceStartup, nextMeter = 0;
                    int kos0 = 0; bool locked = false;
                    // The first frame after StartMatch also carries the previous memory snapshot (object count + GC): not a play frame.
                    yield return null;
                    while (match.Phase != MatchPhase.Results)
                    {
                        yield return null;
                        sampler.Sample();
                        float dt = Time.unscaledDeltaTime * 1000f;
                        if (dt > 100) report.hitchesOver100ms++;
                        if (dt > 250) report.hitchesOver250ms++;
                        int gcNow = GC.CollectionCount(0);
                        if (dt > 100 && report.hitches.Count < 50)
                            report.hitches.Add($"{Time.realtimeSinceStartup - start:0.0} s, partida {m + 1}, {match.Phase}, juego {match.SimTime:0.0} s: {dt:0} ms" +
                                               $"{(gcNow != gcSeen ? ", con recolección GC" : ", sin GC")}, ventana {(Application.isFocused ? "con" : "sin")} foco");
                        gcSeen = gcNow;
                        if (match.Phase == MatchPhase.Paused) ResumeIfPaused(match, report.pauses);
                        if (match.Phase == MatchPhase.Playing) Stoke(match, ref nextMeter);
                        // Lock-up: the simulation stops advancing for two real seconds, or the match outlives its clock.
                        float progress = match.SimTime - match.CountdownRemaining; // advances in countdown and in play
                        if (progress != lastSim) { lastSim = progress; stalledSince = Time.realtimeSinceStartup; }
                        else if (Time.realtimeSinceStartup - stalledSince > 2) { locked = true; report.problems.Add($"partida {m + 1}: la simulación se detuvo en fase {match.Phase}"); break; }
                        if (match.SimTime - matchStartSim > limit) { locked = true; report.problems.Add($"partida {m + 1}: no terminó en {limit:0} s de juego"); break; }
                        if (Time.realtimeSinceStartup - start > seconds + 600) { locked = true; report.problems.Add($"partida {m + 1}: sesión excedida"); break; }
                    }
                    match.AutomatedTest = true;
                    if (locked) { report.lockups++; break; }
                    int survivors = 0; foreach (var f in match.Fighters) if (!f.Eliminated) survivors++;
                    int kos = CountKos(match) - kos0; report.kos += kos;
                    float gameSeconds = match.SimTime - matchStartSim;
                    report.longestMatchGameSeconds = Mathf.Max(report.longestMatchGameSeconds, gameSeconds);
                    report.matches++;
                    // Results screen for a moment, like a player reading it; sample memory here (same state every match).
                    for (int k = 0; k < 30; k++) { yield return null; sampler.Sample(); }
                    GC.Collect(); yield return null;
                    var memory = Snapshot(true);
                    var sample = new SoakSample
                    {
                        match = report.matches, arena = BrawlArena.NameOf(arena, match.UsesStageArt(arena)), kind = kind,
                        realSeconds = Time.realtimeSinceStartup - start, matchGameSeconds = gameSeconds, finishedByStocks = survivors <= 1, kos = kos, memory = memory
                    };
                    report.samples.Add(sample);
                    // The first match of each arena warms caches; compare later visits with the second sample of that arena.
                    if (kind == "revancha" && !firstByArena.ContainsKey(arena)) { firstByArena[arena] = memory; censusByArena[arena] = Census(); }
                    else if (firstByArena.TryGetValue(arena, out var first))
                    {
                        if (memory.allObjects > first.allObjects) sample.growth = CensusGrowth(censusByArena[arena], Census(), 6);
                        report.maxGameObjectGrowth = Mathf.Max(report.maxGameObjectGrowth, memory.gameObjects - first.gameObjects);
                        report.maxAllObjectGrowth = Mathf.Max(report.maxAllObjectGrowth, memory.allObjects - first.allObjects);
                    }
                    Debug.Log($"FUNADOS_SOAK partida {report.matches} ({kind}, {sample.arena}): {gameSeconds:0} s de juego, KO {kos}, real {sample.realSeconds:0} s, " +
                              $"objetos {memory.gameObjects}/{memory.allObjects}{(string.IsNullOrEmpty(sample.growth) ? "" : " (crece: " + sample.growth + ")")}, gestionada {memory.managedMb:0.0} MB, proceso {memory.processPrivateMb:0} MB, errores {errors}");
                }
                report.realSeconds = Time.realtimeSinceStartup - start;
                report.errors = errors;
                report.frameMs = Distribution(sampler.frame); report.cpuMainMs = Distribution(sampler.cpuMain); report.gpuMs = Distribution(sampler.gpu);
                report.avgFps = AvgFps(sampler.frame); report.low1Fps = LowFps(sampler.frame); report.maxFrameMs = report.frameMs.max;

                // Memory trend over the session, after the first arena cycle has warmed every cache.
                var warmSamples = report.samples.FindAll(s => s.match > SoakMatchesPerArena * BrawlArena.Count / 2);
                if (warmSamples.Count < 2) warmSamples = report.samples;
                if (warmSamples.Count >= 2)
                {
                    var a = warmSamples[0].memory; var z = warmSamples[warmSamples.Count - 1].memory;
                    report.managedGrowthMb = z.managedMb - a.managedMb; report.processGrowthMb = z.processPrivateMb - a.processPrivateMb;
                    report.managedSlopeMbPer10Min = Slope(warmSamples, s => s.memory.managedMb) * 600;
                    report.processSlopeMbPer10Min = Slope(warmSamples, s => s.memory.processPrivateMb) * 600;
                }
                File.WriteAllText(Path.Combine(output, "soak-session.json"), JsonUtility.ToJson(report, true));

                check(report.realSeconds >= seconds && report.lockups == 0 && report.matches >= 2,
                    $"Sesión de {report.realSeconds / 60:0.0} min a velocidad real: {report.matches} partidas de cinco luchadores ({report.rematches} revanchas encadenadas, {report.menuReturns} vueltas al menú, {report.kos} KO), sin bloqueos");
                check(report.errors == 0, $"Sesión sin errores ni excepciones en el registro ({report.errors})");
                check(report.hitchesOver250ms == 0 && report.avgFps >= MinAvgFps,
                    $"Sesión fluida: {report.avgFps:0.0} FPS medios, 1 % bajo {report.low1Fps:0.0}, frame máx. {report.maxFrameMs:0} ms ({report.hitchesOver100ms} >100 ms, {report.hitchesOver250ms} >250 ms); CPU p95 {report.cpuMainMs.p95:0.00} ms, GPU p95 {report.gpuMs.p95:0.00} ms");
                check(firstByArena.Count > 0 && report.maxGameObjectGrowth <= 5 && report.maxAllObjectGrowth <= 20,
                    $"Sin crecimiento de entidades entre revanchas: {report.maxGameObjectGrowth:+0;-0;0} GameObjects y {report.maxAllObjectGrowth:+0;-0;0} objetos de Unity como máximo en la misma arena");
                check(report.managedGrowthMb < 16 && report.managedSlopeMbPer10Min < 4 && report.processGrowthMb < 128 && report.processSlopeMbPer10Min < 32,
                    $"Sin fugas de memoria: gestionada {report.managedGrowthMb:+0.0;-0.0;0} MB ({report.managedSlopeMbPer10Min:+0.00;-0.00;0} MB/10 min), proceso {report.processGrowthMb:+0;-0;0} MB ({report.processSlopeMbPer10Min:+0.0;-0.0;0} MB/10 min)");
                foreach (var problem in report.problems) Debug.Log("FUNADOS_SOAK " + problem);
                foreach (var hitch in report.hitches) Debug.Log("FUNADOS_SOAK tirón " + hitch);
                foreach (var pause in report.pauses) Debug.Log("FUNADOS_SOAK " + pause);
            }
            finally
            {
                Application.logMessageReceived -= onLog; sampler.Dispose(); IsolateInput(false); match.Settings.matchSeconds = oldMatchSeconds;
                Time.timeScale = oldScale; match.AutomatedTest = oldAutomated; BrawlFeedback.ReducedEffects = oldReduced;
                match.ReturnToMenu(); match.ArenaIndex = oldArena; match.OpponentCount = oldOpponents; match.SelectedCharacter = oldCharacter;
            }
        }

        private static float Slope(List<SoakSample> samples, Func<SoakSample, float> value)
        {
            // Least-squares slope in MB per real second.
            float n = samples.Count, sx = 0, sy = 0, sxx = 0, sxy = 0;
            foreach (var s in samples) { float x = s.realSeconds, y = value(s); sx += x; sy += y; sxx += x * x; sxy += x * y; }
            float den = n * sxx - sx * sx;
            return Mathf.Abs(den) < 1e-6f ? 0 : (n * sxy - sx * sy) / den;
        }
    }
}
