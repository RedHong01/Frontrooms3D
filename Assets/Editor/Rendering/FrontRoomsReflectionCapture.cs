using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using FrontRooms.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// G6: one HDR reflection cube per zone type, captured in the real map.
///
/// Builds the shipped level profile's map in a throwaway scene (the same edit-mode
/// build the map's own captures use, FrontRoomsMapWorld.BuildForCapture), picks one
/// representative cell per zone type, lights the lamps exactly as the game does with
/// the player standing there (the map's own fixture tick: temperaments, the 16 m
/// light radius, the 9 m shadow set), and bakes a 256 px HDR cube at eye height with
/// a Custom ReflectionProbe that also renders dynamic objects (Lightmapping.BakeReflectionProbe;
/// URP renders the faces, Unity writes an EXR and convolves its mips for roughness).
///   Refl_Level0   a lit Level 0 room (standard ceiling, steady lamp, lit neighbours)
///   Refl_Office   a lit Office room
///   Refl_Tall     a lit tall hall
///   Refl_DeadLamp a Level 0 cell whose own lamp is dead, with lit lamps around it
/// Output: Assets/Resources/Rendering/Reflections/Refl_*.exr, capture_manifest.txt (what the
/// cubes were captured from) and a pick log in Verification/glass. Reflections inside the
/// capture are off (first bounce only); the RT glass input is forced off; every built window
/// pane is drawn as the game will draw it (Glass_Window, 6 mm, no shadow) instead of the map's
/// placeholder TransparentGlass. Nothing is saved to any scene.
/// RECAPTURE after: FrontRoomsLook ambient changes; the map switching panes to Glass_Window;
/// the Level 0 lens change to Troffer_Lens (audit 1.5 / F10); any wallpaper print change.
/// WarnIfStale() (called by FrontRoomsGlassSetup) compares the manifest with the project.
/// Menu: FrontRooms → Rendering → Capture zone reflection cubemaps
/// Batch: -executeMethod FrontRoomsReflectionCapture.RunBatch
/// </summary>
public static class FrontRoomsReflectionCapture
{
    public const string OutDir = "Assets/Resources/Rendering/Reflections";
    public const string ManifestPath = OutDir + "/capture_manifest.txt";
    const float PaneThickness = .006f;
    public const int Resolution = 256;
    public const int WebResolution = 128;
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    const BindingFlags Public = BindingFlags.Instance | BindingFlags.Public;

    public struct Lamp { public Light light; public int mode; }

    /// <summary>The built map, its lamps by cell and the chunks that have geometry.</summary>
    public sealed class MapCapture
    {
        public FrontRoomsMapWorld world;
        public readonly Dictionary<GridCoord, Lamp> lamps = new Dictionary<GridCoord, Lamp>();
        public readonly HashSet<GridCoord> builtChunks = new HashSet<GridCoord>();
        public bool IsBuilt(GridCoord cell) => builtChunks.Contains(MapGrid.ChunkOf(cell));
    }

    public struct Pick
    {
        public FrontRoomsLook.ReflectionZone zone;
        public GridCoord cell;
        public Vector3 point;
        public string note;
        public bool found;
    }

    [MenuItem("FrontRooms/Rendering/Capture zone reflection cubemaps")]
    public static void Capture() => Run();

    public static void RunBatch() => Run();

    /// <summary>Re-apply the importer settings to the existing cubes without re-capturing.</summary>
    public static void RunImportersBatch()
    {
        var log = new StringBuilder();
        foreach (var name in FrontRoomsZoneReflection.CubeNames) ConfigureImporter(OutDir + "/" + name + ".exr", log);
        Debug.Log("[ReflectionCapture] importers\n" + log);
    }

    static void Run()
    {
        Directory.CreateDirectory(OutDir);
        var log = new StringBuilder();
        var previous = EditorSceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(scene);
        try
        {
            var map = BuildMap();
            var world = map.world;
            log.AppendLine("seed " + world.Profile.generation.seed + ", built chunks " + world.BuiltChunkCount + ", lamps " + map.lamps.Count);
            var panes = SwapPanes(world, log);
            // First bounce only: no default sky cube inside the capture, no RT glass input.
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = null;
            RenderSettings.reflectionIntensity = 0f;
            rtWeightWas = Shader.GetGlobalFloat(RTWeightId);
            Shader.SetGlobalFloat(RTWeightId, 0f);
            Shader.SetGlobalFloat(NominalId, 0f);
            var picks = new List<Pick>();
            foreach (FrontRoomsLook.ReflectionZone zone in Enum.GetValues(typeof(FrontRoomsLook.ReflectionZone)))
            {
                var pick = PickCell(map, zone);
                log.AppendLine(zone + ": " + (pick.found ? "cell " + pick.cell + " at " + pick.point.ToString("F2") + " - " + pick.note : "NO CELL FOUND"));
                if (!pick.found) continue;
                picks.Add(pick);
                LightLampsFor(world, pick.point);
                var path = OutDir + "/" + FrontRoomsZoneReflection.CubeNames[(int)zone] + ".exr";
                var ok = Bake(pick.point, world.SightDistance, path);
                log.AppendLine("  bake " + (ok ? "ok " : "FAILED ") + path + LampSummary(map.lamps, pick.point));
            }
            WriteManifest(world, picks, panes, log);
            AssetDatabase.Refresh();
            foreach (var name in FrontRoomsZoneReflection.CubeNames) ConfigureImporter(OutDir + "/" + name + ".exr", log);
            Shader.SetGlobalFloat(RTWeightId, rtWeightWas);
        }
        catch (Exception e)
        {
            log.AppendLine("ERROR " + e);
            Debug.LogException(e);
        }
        finally
        {
            var report = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "glass");
            Directory.CreateDirectory(report);
            File.WriteAllText(Path.Combine(report, "reflection_capture.txt"), log.ToString());
            Debug.Log("[ReflectionCapture]\n" + log);
            if (!Application.isBatchMode)
            {
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid()) EditorSceneManager.SetActiveScene(previous);
            }
        }
    }

    static readonly int RTWeightId = Shader.PropertyToID("_FR_GlassRTWeight");
    static readonly int NominalId = Shader.PropertyToID("_FR_ZoneReflNominal");
    static float rtWeightWas;

    /// <summary>
    /// Draw every built window pane as the game will (map contract): Glass_Window, 6 mm, no shadow.
    /// A temporary copy without the reflection floor (the capture has no environment to reflect).
    /// Returns "material name, count" for the manifest.
    /// </summary>
    static string SwapPanes(FrontRoomsMapWorld world, StringBuilder log)
    {
        var src = AssetDatabase.LoadAssetAtPath<Material>(FrontRoomsGlassSetup.SurfaceDir + "/Glass_Window.mat");
        var built = typeof(FrontRoomsMapWorld).GetField("built", Private)?.GetValue(world) as IDictionary;
        if (src == null || built == null) { log.AppendLine("panes: NOT swapped (" + (src == null ? "Glass_Window missing" : "map.built not found") + ")"); return "map default (not swapped)"; }
        var m = new Material(src) { name = "Glass_Window (capture copy)", hideFlags = HideFlags.DontSave };
        m.SetFloat("_ReflectionMin", 0f);
        var n = 0;
        foreach (DictionaryEntry e in built)
        {
            var windows = e.Value.GetType().GetField("windows", Public)?.GetValue(e.Value) as IEnumerable;
            if (windows == null) continue;
            foreach (var w in windows)
            {
                var pane = w.GetType().GetField("pane", Public)?.GetValue(w) as GameObject;
                var r = pane != null ? pane.GetComponent<Renderer>() : null;
                if (r == null) continue;
                r.sharedMaterial = m;
                r.shadowCastingMode = ShadowCastingMode.Off;
                var sc = pane.transform.localScale;
                var a = new Vector3(Mathf.Abs(sc.x), Mathf.Abs(sc.y), Mathf.Abs(sc.z));
                var thin = a.x <= a.y && a.x <= a.z ? 0 : (a.y <= a.z ? 1 : 2);
                sc[thin] = Mathf.Sign(sc[thin]) * PaneThickness;
                pane.transform.localScale = sc;
                n++;
            }
        }
        log.AppendLine("panes: " + n + " built panes drawn with Glass_Window (6 mm, shadows off)");
        return "Glass_Window x" + n;
    }

    // ---------------- manifest: what the cubes hold ----------------
    static string Md5(string assetPath)
    {
        var full = Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
        if (!File.Exists(full)) return "missing";
        using (var md5 = MD5.Create()) return BitConverter.ToString(md5.ComputeHash(File.ReadAllBytes(full))).Replace("-", "").ToLowerInvariant().Substring(0, 12);
    }

    static string C(Color c) => c.r.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "/" + c.g.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "/" + c.b.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>The inputs a recapture depends on, as key = value lines (the same keys WarnIfStale checks).</summary>
    public static Dictionary<string, string> CurrentInputs()
    {
        return new Dictionary<string, string>
        {
            { "ambient", C(FrontRoomsLook.AmbientSky) + " " + C(FrontRoomsLook.AmbientEquator) + " " + C(FrontRoomsLook.AmbientGround) },
            { "fog", C(FrontRoomsLook.FogColor) + " " + FrontRoomsLook.FogDensity.ToString(System.Globalization.CultureInfo.InvariantCulture) },
            { "FrontRoomsSurface.shader", Md5("Assets/Resources/Rendering/FrontRoomsSurface.shader") },
            { "level profile", FrontRoomsLevelProfiles.Resolve() != null ? FrontRoomsLevelProfiles.Resolve().name : "none" },
        };
    }

    static string FindScript(string className)
    {
        foreach (var guid in AssetDatabase.FindAssets(className + " t:MonoScript"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == className) return path;
        }
        return "missing";
    }

    static void WriteManifest(FrontRoomsMapWorld world, List<Pick> picks, string panes, StringBuilder log)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Zone reflection cubes: what they were captured from (FrontRoomsReflectionCapture). Recapture when any line below changes.");
        sb.AppendLine("captured = " + DateTime.Now.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture));
        sb.AppendLine("seed = " + world.Profile.generation.seed + "; chunks " + world.BuiltChunkCount);
        foreach (var p in picks) sb.AppendLine("cell." + p.zone + " = " + p.cell + " (" + p.note + ")");
        sb.AppendLine("panes = " + panes);
        sb.AppendLine("lens materials = " + LensMaterials(world));
        sb.AppendLine("wallpaper print = " + PrintVersion());
        foreach (var kv in CurrentInputs()) sb.AppendLine(kv.Key + " = " + kv.Value);
        // Information only (the map script changes daily; WarnIfStale does not compare it).
        sb.AppendLine("info.FrontRoomsMapWorld.cs = " + Md5(FindScript("FrontRoomsMapWorld")));
        sb.AppendLine("info.FrontRoomsGlass.shader = " + Md5("Assets/Resources/Rendering/FrontRoomsGlass.shader"));
        sb.AppendLine("info.Glass_Window.mat = " + Md5(FrontRoomsGlassSetup.SurfaceDir + "/Glass_Window.mat"));
        File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName, ManifestPath), sb.ToString());
        log.AppendLine("manifest written: " + ManifestPath);
    }

    static string LensMaterials(FrontRoomsMapWorld world)
    {
        var names = new SortedSet<string>();
        foreach (var r in world.GetComponentsInChildren<Renderer>(true))
            if (r.sharedMaterial != null && r.sharedMaterial.name.IndexOf("lens", StringComparison.OrdinalIgnoreCase) >= 0) names.Add(r.sharedMaterial.name);
        return names.Count > 0 ? string.Join(", ", names) : "none found by name";
    }

    static string PrintVersion()
    {
        var shader = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Assets/Resources/Rendering/FrontRoomsSurface.shader");
        var hasPrint = File.Exists(shader) && File.ReadAllText(shader).Contains("_FR_PRINT");
        return hasPrint ? "_FR_PRINT layer present (surface shader " + Md5("Assets/Resources/Rendering/FrontRoomsSurface.shader") + ")" : "static paper (no _FR_PRINT in the surface shader)";
    }

    /// <summary>Log a warning when the project no longer matches what the cubes were captured from.</summary>
    [MenuItem("FrontRooms/Rendering/Check zone reflection cubes are current")]
    public static void WarnIfStale()
    {
        var full = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ManifestPath);
        if (!File.Exists(full)) { Debug.LogWarning("[ReflectionCapture] No " + ManifestPath + ": the zone cubes' inputs are unknown. Recapture (FrontRooms > Rendering > Capture zone reflection cubemaps)."); return; }
        var stored = new Dictionary<string, string>();
        foreach (var line in File.ReadAllLines(full))
        {
            var i = line.IndexOf(" = ", StringComparison.Ordinal);
            if (i > 0 && !line.StartsWith("#")) stored[line.Substring(0, i)] = line.Substring(i + 3);
        }
        var stale = new List<string>();
        foreach (var kv in CurrentInputs())
            if (!stored.TryGetValue(kv.Key, out var v) || v != kv.Value) stale.Add(kv.Key + ": captured " + (v ?? "?") + ", now " + kv.Value);
        var print = PrintVersion();
        if (stored.TryGetValue("wallpaper print", out var pv) && pv != print) stale.Add("wallpaper print: captured " + pv + ", now " + print);
        if (stale.Count == 0) Debug.Log("[ReflectionCapture] Zone cubes are current (" + ManifestPath + ").");
        else Debug.LogWarning("[ReflectionCapture] Zone cubes may be STALE; recapture (FrontRooms > Rendering > Capture zone reflection cubemaps). Changed: " + string.Join("; ", stale));
    }

    /// <summary>Edit-mode build of the shipped map around its spawn, plus every lamp by cell.</summary>
    public static MapCapture BuildMap()
    {
        var root = new GameObject("GLASS CAPTURE / map");
        var world = root.AddComponent<FrontRoomsMapWorld>();
        world.Profile = FrontRoomsLevelProfiles.Resolve();
        world.BuildForCapture();
        Physics.SyncTransforms();
        var map = new MapCapture { world = world };
        var lamps = map.lamps;
        var built = typeof(FrontRoomsMapWorld).GetField("built", Private)?.GetValue(world) as IDictionary;
        if (built == null) { Debug.LogError("[ReflectionCapture] FrontRoomsMapWorld.built not found (map code changed?)"); return map; }
        foreach (DictionaryEntry entry in built)
        {
            if (entry.Key is GridCoord chunkCoord) map.builtChunks.Add(chunkCoord);
            var fixtures = entry.Value.GetType().GetField("fixtures", Public)?.GetValue(entry.Value) as IList;
            if (fixtures == null) continue;
            foreach (var f in fixtures)
            {
                var t = f.GetType();
                var light = t.GetField("light", Public)?.GetValue(f) as Light;
                if (light == null) continue;
                var mode = (int)t.GetField("mode", Public).GetValue(f);
                lamps[world.CellOf(light.transform.position)] = new Lamp { light = light, mode = mode };
            }
        }
        return map;
    }

    /// <summary>Light the map's lamps as the game does with the player standing at <paramref name="eye"/>.</summary>
    public static void LightLampsFor(FrontRoomsMapWorld world, Vector3 eye)
    {
        var player = world.Player;
        if (player != null) player.position = new Vector3(eye.x, world.transform.position.y, eye.z);
        var tick = typeof(FrontRoomsMapWorld).GetMethod("TickFixtures", Private);
        if (tick == null) { Debug.LogError("[ReflectionCapture] FrontRoomsMapWorld.TickFixtures not found (map code changed?)"); return; }
        for (var i = 0; i < 3; i++) tick.Invoke(world, new object[] { .02f });
    }

    static bool Lit(int mode) => mode == 0;
    const int DeadMode = 3;

    /// <summary>The best cell for a zone type: its own lamp in the wanted state, most lit neighbours in the same zone, nothing in the way at eye height.</summary>
    public static Pick PickCell(MapCapture map, FrontRoomsLook.ReflectionZone kind)
    {
        var world = map.world;
        var lamps = map.lamps;
        var best = new Pick { zone = kind };
        var bestScore = float.MinValue;
        var centre = world.SpawnWorldPosition;
        foreach (var chunk in world.Cache.Built)
        {
            for (var j = 0; j < MapGrid.ChunkCells; j++)
            for (var i = 0; i < MapGrid.ChunkCells; i++)
            {
                var cell = chunk.Cell(i, j);
                if (!map.IsBuilt(cell)) continue;
                var zone = world.ZoneOf(cell);
                if (!Matches(kind, zone)) continue;
                if (!lamps.TryGetValue(cell, out var own)) continue;
                if (kind == FrontRoomsLook.ReflectionZone.DeadLamp ? own.mode != DeadMode : !Lit(own.mode)) continue;
                var lit = 0;
                var same = 0;
                for (var dy = -1; dy <= 1; dy++)
                for (var dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var n = new GridCoord(cell.x + dx, cell.y + dy);
                    if (!map.IsBuilt(n) || !world.ZoneOf(n).id.Equals(zone.id)) continue;
                    same++;
                    if (lamps.TryGetValue(n, out var l) && Lit(l.mode)) lit++;
                }
                var point = world.transform.TransformPoint(new Vector3((cell.x + .5f) * MapGrid.CellSize, ModuleUnits.PlayerEye, (cell.y + .5f) * MapGrid.CellSize));
                if (Blocked(point)) continue;
                var score = lit * 2f + same - Vector3.Distance(point, centre) * .02f;
                if (score <= bestScore) continue;
                bestScore = score;
                best = new Pick { zone = kind, cell = cell, point = point, found = true, note = zone.theme + "/" + zone.height + ", own lamp mode " + own.mode + ", same-zone neighbours " + same + ", lit " + lit };
            }
        }
        return best;
    }

    static bool Matches(FrontRoomsLook.ReflectionZone kind, ZoneInfo zone)
    {
        switch (kind)
        {
            case FrontRoomsLook.ReflectionZone.Level0: return zone.theme == ZoneTheme.Level0 && zone.height == ZoneHeight.Standard;
            case FrontRoomsLook.ReflectionZone.Office: return zone.theme == ZoneTheme.Office && zone.height != ZoneHeight.Tall;
            case FrontRoomsLook.ReflectionZone.Tall: return zone.height == ZoneHeight.Tall;
            default: return zone.theme == ZoneTheme.Level0 && zone.height == ZoneHeight.Standard;
        }
    }

    /// <summary>Something (a prop, a column) within 0.45 m of the point.</summary>
    public static bool Blocked(Vector3 point)
    {
        if (Physics.CheckSphere(point, .45f, ~0, QueryTriggerInteraction.Ignore)) return true;
        foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            var b = r.bounds;
            if (b.size.x > 4f || b.size.z > 4f) continue; // merged shell blocks
            b.Expand(.5f);
            if (b.Contains(point)) return true;
        }
        return false;
    }

    static string LampSummary(Dictionary<GridCoord, Lamp> lamps, Vector3 point)
    {
        int on = 0, shadowed = 0;
        foreach (var l in lamps.Values)
        {
            if (l.light == null || !l.light.enabled) continue;
            if (Vector3.Distance(l.light.transform.position, point) > 16f) continue;
            on++;
            if (l.light.shadows != LightShadows.None) shadowed++;
        }
        return " (lamps on within 16 m: " + on + ", shadowed: " + shadowed + ")";
    }

    static bool Bake(Vector3 point, float far, string path)
    {
        var go = new GameObject("GLASS CAPTURE / probe");
        go.transform.position = point;
        var probe = go.AddComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Custom;
        probe.renderDynamicObjects = true;
        probe.resolution = Resolution;
        probe.hdr = true;
        probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
        probe.backgroundColor = FrontRoomsLook.FogColor;
        probe.nearClipPlane = .05f;
        probe.farClipPlane = Mathf.Max(20f, far);
        probe.shadowDistance = 40f;
        probe.cullingMask = ~0;
        probe.boxProjection = false;
        probe.size = new Vector3(3f, 3f, 3f);
        bool ok;
        try { ok = Lightmapping.BakeReflectionProbe(probe, path); }
        finally { UnityEngine.Object.DestroyImmediate(go); }
        return ok;
    }

    static void ConfigureImporter(string path, StringBuilder log)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) { log.AppendLine("  (no importer for " + path + ")"); return; }
        importer.textureShape = TextureImporterShape.TextureCube;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.cubemapConvolution = TextureImporterCubemapConvolution.Specular;
        importer.SetTextureSettings(settings);
        importer.mipmapEnabled = true;
        importer.sRGBTexture = false;
        importer.filterMode = FilterMode.Trilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = Resolution;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        var web = importer.GetPlatformTextureSettings("WebGL");
        web.overridden = true;
        web.maxTextureSize = WebResolution;
        // WebGL's automatic choice for an HDR cube is DXT1 (LDR: lamp highlights clip at 1).
        // RGB9e5 keeps HDR at 4 bytes per pixel and is supported by desktop and mobile
        // browsers (Unity 6.3 manual, GPU texture formats reference); BC6H would be
        // decompressed to RGBA Half on macOS browsers.
        web.format = TextureImporterFormat.RGB9E5;
        importer.SetPlatformTextureSettings(web);
        importer.SaveAndReimport();
        var cube = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
        log.AppendLine("  " + Path.GetFileName(path) + ": " + (cube != null ? cube.width + " px, " + cube.format + ", mips " + cube.mipmapCount : "not a Cubemap")
            + "; WebGL override " + importer.GetPlatformTextureSettings("WebGL").format + " at " + WebResolution + " px; Standalone automatic " + importer.GetAutomaticFormat("Standalone"));
    }
}
