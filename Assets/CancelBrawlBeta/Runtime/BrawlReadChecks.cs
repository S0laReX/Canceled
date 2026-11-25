using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CancelBrawl.Beta
{
    /// <summary>
    /// <c>-read-smoke</c>: how well the fighters read in the side camera (GDD 2.0 §9 and §11). In a real five-player
    /// match, in every arena and with all eleven looks, it renders the group camera's view three ways: the full frame,
    /// the background alone (fighters hidden) and one mask per fighter. That gives the pixels of each fighter and
    /// exactly what is behind them. Each fighter pixel is compared with the pixel behind it (CIELAB ΔE, also under the
    /// three colour-blind simulations), including where the neon of the backdrop is behind. Also checks that the
    /// background has less saturation and contrast than the fighters (§9 rule), that fighters are big enough at 720p,
    /// and that the five characters' profile silhouettes, accessories included, differ from each other. Thresholds
    /// are this suite's (the GDD gives none): ΔE ≥ 10 counts as "clearly different at a glance".
    /// Thresholds and where they come from:
    /// - GDD §9 «El fondo tiene menos contraste y saturación que los luchadores»: checked literally per arena, mean CIELAB
    ///   chroma and standard deviation of L* of the background vs the fighters' pixels.
    /// - GDD §11 «Un mismo personaje debe reconocerse de perfil» and the identity accessory: profile silhouettes of
    ///   different characters overlap (IoU, normalised height) less than <see cref="MaxProfileOverlap"/>.
    /// - GDD §10 «Todo debe leerse a 720p»: each fighter at least <see cref="MinHeightPixels"/> px tall at 720p.
    /// - The ΔE ≥ 10 per pixel and the 60 % shares (<see cref="MinSeparated"/>, <see cref="MinEdgeSeparated"/>) are this
    ///   suite's own numbers; findings below them are reported, never relaxed to pass.
    /// </summary>
    public static class BrawlReadChecks
    {
        public const int Width = 1280, Height = 720;
        public const float ClearDeltaE = 10, MinSeparated = .6f, MinEdgeSeparated = .6f, MinHeightPixels = 64, MaxProfileOverlap = .8f;
        private const int MaskLayer = 29;

        [Serializable] private sealed class FighterRow
        {
            public string arena, character, skin, placement, worstVision;
            public int pixels, heightPixels;
            public float separated, separatedWorstVision, edgeSeparated, medianDeltaE, neonBehind;
        }
        [Serializable] private sealed class ArenaRow
        {
            public string arena;
            public float backgroundChroma, backgroundContrast, fighterChroma, fighterContrast, neonShare, separatedOverNeon;
            public int neonPixelsBehind;
        }
        [Serializable] private sealed class ProfileRow { public string character, skin; public int area, widthPixels, heightPixels; public float profileToFrontArea; }
        [Serializable] private sealed class Report
        {
            public List<ArenaRow> arenas = new List<ArenaRow>();
            public List<FighterRow> fighters = new List<FighterRow>();
            public List<ProfileRow> profiles = new List<ProfileRow>();
            public List<string> profileOverlaps = new List<string>();
            public float maxProfileOverlap;
            public List<string> notes = new List<string>();
        }

        private static readonly ColorVision[] Visions = { ColorVision.Normal, ColorVision.Protanopia, ColorVision.Deuteranopia, ColorVision.Tritanopia };

        public static IEnumerator Run(BrawlMatch match, Action<bool, string> check, string output)
        {
            match.AutomatedTest = true;
            var report = new Report();
            var texture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB); texture.Create();
            var read = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            var cameraObject = new GameObject("Cámara lectura"); var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false;

            int rounds = 1;
            for (int c = 0; c < BrawlRoster.Count; c++) rounds = Mathf.Max(rounds, BrawlRoster.SkinCount(c));
            var seen = new HashSet<string>();
            bool restored = true;

            for (int arena = 0; arena < BrawlArena.Count; arena++)
            {
                var arenaRow = new ArenaRow();
                double bgChroma = 0, bgL = 0, bgL2 = 0, fChroma = 0, fL = 0, fL2 = 0; long bgCount = 0, fCount = 0, neon = 0;
                int neonBehind = 0, neonSeparated = 0;
                for (int round = 0; round < rounds; round++)
                {
                    match.OpponentCount = BrawlMatch.MaxPlayers - 1; match.ArenaIndex = arena; match.SelectedCharacter = 0;
                    for (int slot = 0; slot < BrawlMatch.MaxPlayers; slot++) match.SlotSkins[slot] = BrawlRoster.WrapSkin(match.SlotCharacters[slot], round);
                    match.StartMatch();
                    yield return null;
                    arenaRow.arena = match.Arena.DisplayName;
                    var layout = match.Arena.CurrentSideLayout;
                    for (int placement = 0; placement < 2; placement++)
                    {
                        Vector3[] points = placement == 0
                            ? new[] { new Vector3(-6, .05f, 0), new Vector3(-3, .05f, 0), new Vector3(0, .05f, 0), new Vector3(3, .05f, 0), new Vector3(6, .05f, 0) }
                            : new[] { new Vector3(-layout.PlatformX, layout.PlatformTop + .05f, 0), new Vector3(layout.PlatformX, layout.PlatformTop + .05f, 0),
                                      new Vector3(-4.5f, .05f, 0), new Vector3(1.5f, .05f, 0), new Vector3(5, .05f, 0) };
                        for (int i = 0; i < match.Fighters.Length; i++) match.Fighters[i].RespawnAt(points[i % points.Length], false);
                        yield return new WaitForSecondsRealtime(.7f);
                        yield return new WaitForEndOfFrame();

                        if (match.Phase != MatchPhase.Countdown) report.notes.Add($"Captura con la partida ya en marcha: {match.Arena.DisplayName}, ronda {round}, {(placement == 0 ? "suelo" : "plataformas")}");
                        Camera view = match.GroupCamera.View;
                        camera.CopyFrom(view); camera.enabled = false; camera.targetTexture = texture;
                        Color32[] full = Grab(camera, read, texture, Color.black);
                        Rect inside = camera.pixelRect;
                        // Background alone: every renderer of every fighter (body, marker, facing line) off.
                        var hidden = new List<Renderer>();
                        foreach (var fighter in match.Fighters)
                            foreach (var renderer in fighter.GetComponentsInChildren<Renderer>())
                                if (renderer.enabled) { renderer.enabled = false; hidden.Add(renderer); }
                        Color32[] behind = Grab(camera, read, texture, Color.black);
                        foreach (var renderer in hidden) renderer.enabled = true;

                        if (round == 0)
                        {
                            for (int p = 0; p < behind.Length; p += 3)
                            {
                                if (!inside.Contains(new Vector2(p % Width + .5f, p / Width + .5f))) continue;
                                Lab(behind[p], ColorVision.Normal, out float l, out float chroma);
                                bgChroma += chroma; bgL += l; bgL2 += l * l; bgCount++;
                                if (IsNeon(l, chroma)) neon++;
                            }
                            if (placement == 0) Save(full, read, Path.Combine(output, $"lectura-arena-{arena}.png"));
                        }

                        for (int i = 0; i < match.Fighters.Length; i++)
                        {
                            var fighter = match.Fighters[i];
                            string key = arena + "/" + placement + "/" + fighter.CharacterIndex + "/" + fighter.SkinIndex;
                            bool[] mask = Mask(camera, read, texture, fighter, out bool layersBack);
                            restored &= layersBack;
                            if (!seen.Add(key)) continue;
                            var row = Measure(mask, full, behind, out int fighterNeon, out int fighterNeonSeparated, ref fChroma, ref fL, ref fL2, ref fCount);
                            row.arena = arenaRow.arena; row.character = BrawlRoster.Get(fighter.CharacterIndex).Name; row.skin = fighter.SkinName;
                            row.placement = placement == 0 ? "suelo" : "plataformas";
                            neonBehind += fighterNeon; neonSeparated += fighterNeonSeparated;
                            report.fighters.Add(row);
                        }
                        camera.targetTexture = null;
                    }
                }
                arenaRow.backgroundChroma = (float)(bgChroma / Math.Max(1, bgCount));
                arenaRow.backgroundContrast = Std(bgL, bgL2, bgCount);
                arenaRow.fighterChroma = (float)(fChroma / Math.Max(1, fCount));
                arenaRow.fighterContrast = Std(fL, fL2, fCount);
                arenaRow.neonShare = bgCount > 0 ? neon / (float)bgCount : 0;
                arenaRow.neonPixelsBehind = neonBehind;
                arenaRow.separatedOverNeon = neonBehind > 0 ? neonSeparated / (float)neonBehind : 1;
                report.arenas.Add(arenaRow);
            }
            match.ReturnToMenu();
            for (int i = 0; i < 3; i++) yield return null;
            UnityEngine.Object.Destroy(cameraObject);

            // ── Checks on the match captures ──
            int looks = 0; for (int c = 0; c < BrawlRoster.Count; c++) looks += BrawlRoster.SkinCount(c);
            var lookSet = new HashSet<string>(); foreach (var row in report.fighters) lookSet.Add(row.character + "/" + row.skin);
            check(report.arenas.Count == BrawlArena.Count && lookSet.Count == looks,
                $"Medido en partida real con la cámara lateral: {report.arenas.Count} arenas, {lookSet.Count} de {looks} aspectos, en el suelo y en las plataformas ({report.fighters.Count} casos)");
            check(restored, "Las capas de los luchadores vuelven a su sitio después de cada máscara");

            foreach (var a in report.arenas)
                check(a.backgroundChroma < a.fighterChroma && a.backgroundContrast < a.fighterContrast,
                    $"{a.arena}: el fondo tiene menos saturación y contraste que los luchadores (croma {a.backgroundChroma:0.0} < {a.fighterChroma:0.0}; contraste L* {a.backgroundContrast:0.0} < {a.fighterContrast:0.0})");

            FighterRow worst = null, worstVision = null, worstEdge = null, smallest = null;
            foreach (var row in report.fighters)
            {
                if (worst == null || row.separated < worst.separated) worst = row;
                if (worstVision == null || row.separatedWorstVision < worstVision.separatedWorstVision) worstVision = row;
                if (worstEdge == null || row.edgeSeparated < worstEdge.edgeSeparated) worstEdge = row;
                if (smallest == null || row.heightPixels < smallest.heightPixels) smallest = row;
            }
            var failing = new List<string>();
            foreach (var row in report.fighters)
                if (row.separated < MinSeparated || row.separatedWorstVision < MinSeparated || row.edgeSeparated < MinEdgeSeparated)
                    failing.Add($"{row.arena} · {row.character} {row.skin} ({row.placement}): {row.separated:P0}/{row.separatedWorstVision:P0} {row.worstVision}/borde {row.edgeSeparated:P0}");
            report.notes.AddRange(failing);
            if (worst != null)
            {
                check(worst.separated >= MinSeparated,
                    $"Cada luchador se separa de lo que tiene detrás: ≥{MinSeparated:P0} de su silueta con ΔE ≥ {ClearDeltaE:0} (peor: {worst.character} {worst.skin} en {worst.arena}, {worst.separated:P0}; mediana ΔE {worst.medianDeltaE:0})");
                check(worstVision.separatedWorstVision >= MinSeparated,
                    $"También con protanopía, deuteranopía y tritanopía: ≥{MinSeparated:P0} (peor: {worstVision.character} {worstVision.skin} en {worstVision.arena}, {worstVision.separatedWorstVision:P0} con {worstVision.worstVision})");
                check(worstEdge.edgeSeparated >= MinEdgeSeparated,
                    $"El contorno de la silueta se distingue del fondo: ≥{MinEdgeSeparated:P0} del borde con ΔE ≥ {ClearDeltaE:0} (peor: {worstEdge.character} {worstEdge.skin} en {worstEdge.arena}, {worstEdge.edgeSeparated:P0})");
                check(smallest.heightPixels >= MinHeightPixels,
                    $"A 720p cada luchador mide al menos {MinHeightPixels:0} px de alto con cinco en pantalla (el menor: {smallest.character} {smallest.skin} en {smallest.arena}, {smallest.heightPixels} px)");
            }
            if (failing.Count > 0) Debug.Log("FUNADOS_READ casos por debajo: " + string.Join(" | ", failing));

            ArenaRow neonArena = null;
            foreach (var a in report.arenas) if (neonArena == null || a.neonShare > neonArena.neonShare) neonArena = a;
            int neonTotal = 0; float neonWorst = 1;
            foreach (var a in report.arenas) { neonTotal += a.neonPixelsBehind; if (a.neonPixelsBehind > 0) neonWorst = Mathf.Min(neonWorst, a.separatedOverNeon); }
            check(neonTotal == 0 || neonWorst >= MinSeparated,
                $"Delante del neón el luchador sigue leyéndose: {neonWorst:P0} de los píxeles con neón detrás con ΔE ≥ {ClearDeltaE:0} ({neonTotal} píxeles; más neón: {neonArena.arena}, {neonArena.neonShare:P1} del fondo)");

            // ── Profiles: the silhouette the side camera actually sees, accessories included ──
            var profileMasks = new List<bool[]>(); var profileCharacter = new List<int>(); var profileNames = new List<string>();
            var sheet = new Texture2D(SheetCell * looks, SheetCellHeight * 2, TextureFormat.RGBA32, false);
            int column = 0;
            for (int c = 0; c < BrawlRoster.Count; c++)
                for (int s = 0; s < BrawlRoster.SkinCount(c); s++, column++)
                {
                    bool[] side = Snapshot(match.Settings.characterArt, c, s, true, sheet, column, 0, out int area, out int w, out int h, out string skinName);
                    Snapshot(match.Settings.characterArt, c, s, false, sheet, column, 1, out int frontArea, out _, out _, out _);
                    report.profiles.Add(new ProfileRow { character = BrawlRoster.Get(c).Name, skin = skinName, area = area, widthPixels = w, heightPixels = h, profileToFrontArea = frontArea > 0 ? area / (float)frontArea : 0 });
                    profileMasks.Add(Normalise(side)); profileCharacter.Add(c); profileNames.Add(BrawlRoster.Get(c).Name + " " + skinName);
                }
            sheet.Apply(); File.WriteAllBytes(Path.Combine(output, "lectura-perfiles.png"), sheet.EncodeToPNG()); UnityEngine.Object.Destroy(sheet);
            float maxOverlap = 0; string pair = "";
            for (int i = 0; i < profileMasks.Count; i++)
                for (int j = i + 1; j < profileMasks.Count; j++)
                {
                    if (profileCharacter[i] == profileCharacter[j]) continue;
                    float iou = IoU(profileMasks[i], profileMasks[j]);
                    if (iou > .7f) report.profileOverlaps.Add($"{profileNames[i]} ~ {profileNames[j]}: {iou:0.00}");
                    if (iou > maxOverlap) { maxOverlap = iou; pair = profileNames[i] + " y " + profileNames[j]; }
                }
            report.maxProfileOverlap = maxOverlap;
            bool allProfiles = report.profiles.Count == looks; foreach (var p in report.profiles) allProfiles &= p.area > 0;
            check(allProfiles, $"Silueta de perfil de los {looks} aspectos, como la ve la cámara lateral (hoja `lectura-perfiles.png`: de frente arriba, de perfil abajo)");
            check(maxOverlap < MaxProfileOverlap,
                $"De perfil, las siluetas de personajes distintos no se confunden: solapamiento máximo {maxOverlap:0.00} < {MaxProfileOverlap:0.00} ({pair})");
            yield return null;
            check(GameObject.Find("Silueta lectura") == null && GameObject.Find("Cámara lectura") == null, "La prueba no deja objetos en la escena");

            texture.Release(); UnityEngine.Object.Destroy(texture); UnityEngine.Object.Destroy(read);
            report.notes.Add("ΔE es CIELAB 1976 entre el píxel del luchador y el del fondo en el mismo sitio, sin posproceso; daltonismo con las matrices de BrawlHudPalette.");
            report.notes.Add("Neón: píxel de fondo con L* > 70 y croma > 45.");
            File.WriteAllText(Path.Combine(output, "read-smoke.json"), JsonUtility.ToJson(report, true));
        }

        private static float Std(double sum, double sum2, long count)
        {
            if (count < 2) return 0;
            double mean = sum / count; return (float)Math.Sqrt(Math.Max(0, sum2 / count - mean * mean));
        }

        private static bool IsNeon(float l, float chroma) => l > 70 && chroma > 45;

        private static FighterRow Measure(bool[] mask, Color32[] full, Color32[] behind, out int neonPixels, out int neonSeparated,
                                          ref double chromaSum, ref double lSum, ref double l2Sum, ref long count)
        {
            var row = new FighterRow(); neonPixels = neonSeparated = 0;
            int pixels = 0, edges = 0, edgesSeparated = 0, top = -1, bottom = Height;
            var separated = new int[Visions.Length];
            var deltas = new List<float>();
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    int p = y * Width + x;
                    if (!mask[p]) continue;
                    pixels++; top = Mathf.Max(top, y); bottom = Mathf.Min(bottom, y);
                    Color a = full[p], b = behind[p];
                    float normal = 0;
                    for (int v = 0; v < Visions.Length; v++)
                    {
                        float d = BrawlHudPalette.DeltaE(a, b, Visions[v]);
                        if (v == 0) normal = d;
                        if (d >= ClearDeltaE) separated[v]++;
                    }
                    deltas.Add(normal);
                    Lab(full[p], ColorVision.Normal, out float l, out float chroma);
                    chromaSum += chroma; lSum += l; l2Sum += l * l; count++;
                    Lab(behind[p], ColorVision.Normal, out float bl, out float bc);
                    if (IsNeon(bl, bc)) { neonPixels++; if (normal >= ClearDeltaE) neonSeparated++; }
                    bool edge = x == 0 || x == Width - 1 || y == 0 || y == Height - 1 || !mask[p - 1] || !mask[p + 1] || !mask[p - Width] || !mask[p + Width];
                    if (edge) { edges++; if (normal >= ClearDeltaE) edgesSeparated++; }
                }
            row.pixels = pixels; row.heightPixels = pixels > 0 ? top - bottom + 1 : 0;
            row.separated = pixels > 0 ? separated[0] / (float)pixels : 0;
            row.separatedWorstVision = 1; row.worstVision = "Normal";
            for (int v = 1; v < Visions.Length; v++)
            {
                float share = pixels > 0 ? separated[v] / (float)pixels : 0;
                if (share < row.separatedWorstVision) { row.separatedWorstVision = share; row.worstVision = Visions[v].ToString(); }
            }
            row.edgeSeparated = edges > 0 ? edgesSeparated / (float)edges : 0;
            deltas.Sort(); row.medianDeltaE = deltas.Count > 0 ? deltas[deltas.Count / 2] : 0;
            row.neonBehind = pixels > 0 ? neonPixels / (float)pixels : 0;
            return row;
        }

        /// <summary>CIELAB L* and chroma of an sRGB pixel as seen with <paramref name="vision"/>.</summary>
        private static void Lab(Color color, ColorVision vision, out float l, out float chroma)
        {
            Vector3 lin = BrawlHudPalette.SimulateLinear(color, vision);
            float x = (.4124f * lin.x + .3576f * lin.y + .1805f * lin.z) / .95047f;
            float y = .2126f * lin.x + .7152f * lin.y + .0722f * lin.z;
            float z = (.0193f * lin.x + .1192f * lin.y + .9505f * lin.z) / 1.08883f;
            float F(float t) => t > .008856f ? Mathf.Pow(t, 1f / 3) : 7.787f * t + 16f / 116;
            float fx = F(x), fy = F(y), fz = F(z);
            l = 116 * fy - 16; float a = 500 * (fx - fy), b = 200 * (fy - fz);
            chroma = Mathf.Sqrt(a * a + b * b);
        }

        /// <summary>Renders into the whole texture; outside the camera's viewport (the game keeps a strip for the HUD) the
        /// texture is first cleared to <paramref name="outside"/>, so stale pixels never leak into a mask.</summary>
        private static Color32[] Grab(Camera camera, Texture2D read, RenderTexture texture, Color? outside = null)
        {
            var previous = RenderTexture.active; RenderTexture.active = texture;
            GL.Clear(true, true, outside ?? camera.backgroundColor);
            RenderTexture.active = previous;
            camera.targetTexture = texture; camera.Render();
            RenderTexture.active = texture;
            read.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0); read.Apply(false);
            RenderTexture.active = previous;
            return read.GetPixels32();
        }

        /// <summary>Pixels of one fighter's body: its renderers alone on a spare layer, over black and then white.</summary>
        private static bool[] Mask(Camera camera, Texture2D read, RenderTexture texture, BrawlFighter fighter, out bool restored)
        {
            var visual = fighter.GetComponentInChildren<BrawlFighterVisual>();
            var renderers = visual ? visual.GetComponentsInChildren<Renderer>() : new Renderer[0];
            var layers = new int[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) { layers[i] = renderers[i].gameObject.layer; renderers[i].gameObject.layer = MaskLayer; }
            int cullingMask = camera.cullingMask; var flags = camera.clearFlags; Color background = camera.backgroundColor;
            camera.cullingMask = 1 << MaskLayer; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black; Color32[] dark = Grab(camera, read, texture);
            camera.backgroundColor = Color.white; Color32[] light = Grab(camera, read, texture);
            camera.cullingMask = cullingMask; camera.clearFlags = flags; camera.backgroundColor = background;
            restored = true;
            for (int i = 0; i < renderers.Length; i++) { renderers[i].gameObject.layer = layers[i]; restored &= renderers[i].gameObject.layer != MaskLayer; }
            var mask = new bool[dark.Length];
            for (int p = 0; p < dark.Length; p++)
                mask[p] = Mathf.Abs(dark[p].r - light[p].r) < 64 && Mathf.Abs(dark[p].g - light[p].g) < 64 && Mathf.Abs(dark[p].b - light[p].b) < 64;
            return mask;
        }

        private static void Save(Color32[] pixels, Texture2D read, string path)
        {
            read.SetPixels32(pixels); read.Apply(false);
            File.WriteAllBytes(path, read.EncodeToPNG());
        }

        private const int SnapWidth = 480, SnapHeight = 720, SheetCell = 160, SheetCellHeight = 240;
        private static readonly Color SheetBackdrop = new Color(.5f, .55f, .6f);

        /// <summary>One look alone, in profile (facing right, as in play) or from the front, 720 px tall; mask over black/white, drawn into the sheet over grey.</summary>
        private static bool[] Snapshot(BrawlCharacterArt art, int character, int skin, bool profile, Texture2D sheet, int column, int sheetRow,
                                       out int area, out int width, out int height, out string skinName)
        {
            var root = new GameObject("Silueta lectura"); root.transform.position = new Vector3(0, -500, 0);
            root.transform.rotation = Quaternion.LookRotation(profile ? Vector3.right : Vector3.back);
            var visual = root.AddComponent<BrawlFighterVisual>(); visual.Build(character, skin, BrawlRoster.Get(character).Accent, art);
            skinName = visual.SkinName;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) renderer.gameObject.layer = MaskLayer;
            var texture = new RenderTexture(SnapWidth, SnapHeight, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB); texture.Create();
            var read = new Texture2D(SnapWidth, SnapHeight, TextureFormat.RGBA32, false);
            var cameraObject = new GameObject("Cámara lectura"); var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 1.15f;
            camera.transform.position = root.transform.position + new Vector3(0, .95f, -6);
            camera.cullingMask = 1 << MaskLayer; camera.clearFlags = CameraClearFlags.SolidColor; camera.enabled = false;
            camera.backgroundColor = Color.black; Color32[] dark = Grab(camera, read, texture);
            camera.backgroundColor = Color.white; Color32[] light = Grab(camera, read, texture);
            camera.backgroundColor = SheetBackdrop; Color32[] grey = Grab(camera, read, texture);
            camera.targetTexture = null;
            UnityEngine.Object.Destroy(cameraObject); root.SetActive(false); UnityEngine.Object.Destroy(root);
            texture.Release(); UnityEngine.Object.Destroy(texture); UnityEngine.Object.Destroy(read);

            var mask = new bool[dark.Length]; area = 0; int left = SnapWidth, right = -1, low = SnapHeight, high = -1;
            for (int p = 0; p < dark.Length; p++)
            {
                mask[p] = Mathf.Abs(dark[p].r - light[p].r) < 64 && Mathf.Abs(dark[p].g - light[p].g) < 64 && Mathf.Abs(dark[p].b - light[p].b) < 64;
                if (!mask[p]) continue;
                area++; int x = p % SnapWidth, y = p / SnapWidth;
                left = Mathf.Min(left, x); right = Mathf.Max(right, x); low = Mathf.Min(low, y); high = Mathf.Max(high, y);
            }
            width = right >= left ? right - left + 1 : 0; height = high >= low ? high - low + 1 : 0;
            // Sheet cell: grey render scaled down 3×.
            for (int y = 0; y < SheetCellHeight; y++)
                for (int x = 0; x < SheetCell; x++)
                    sheet.SetPixel(column * SheetCell + x, sheetRow * SheetCellHeight + y, grey[(y * 3) * SnapWidth + x * 3]);
            return mask;
        }

        private const int GridWidth = 48, GridHeight = 72;

        /// <summary>Silhouette scaled to a fixed height and anchored at its feet, centred on its bounding box.</summary>
        private static bool[] Normalise(bool[] mask)
        {
            int left = SnapWidth, right = -1, low = SnapHeight, high = -1;
            for (int p = 0; p < mask.Length; p++)
            {
                if (!mask[p]) continue;
                int x = p % SnapWidth, y = p / SnapWidth;
                left = Mathf.Min(left, x); right = Mathf.Max(right, x); low = Mathf.Min(low, y); high = Mathf.Max(high, y);
            }
            var grid = new bool[GridWidth * GridHeight];
            if (high < low) return grid;
            float scale = (high - low + 1) / (float)GridHeight, centre = (left + right) * .5f;
            for (int gy = 0; gy < GridHeight; gy++)
                for (int gx = 0; gx < GridWidth; gx++)
                {
                    int x = Mathf.RoundToInt(centre + (gx - GridWidth * .5f + .5f) * scale), y = Mathf.RoundToInt(low + (gy + .5f) * scale);
                    grid[gy * GridWidth + gx] = x >= 0 && x < SnapWidth && y >= 0 && y < SnapHeight && mask[y * SnapWidth + x];
                }
            return grid;
        }

        private static float IoU(bool[] a, bool[] b)
        {
            int both = 0, either = 0;
            for (int i = 0; i < a.Length; i++) { if (a[i] && b[i]) both++; if (a[i] || b[i]) either++; }
            return either > 0 ? both / (float)either : 0;
        }
    }
}
