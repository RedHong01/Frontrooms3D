// Level transitions (production, 2026-10-22): asset preparation and cost measurement for FrontRoomsTransitionKit.
//   Prepare: the 20 transition materials from their FrontRoomsRenderSetup rows (nothing else is touched), the reduced-tier
//            import caps (WebGL / iOS / Android max 1024 on the 10 renovation texture sets; the desktop tab is untouched),
//            the four Kit_Trans* props re-imported.
//     Unity -batchmode -projectPath <clone> -buildTarget OSXUniversal -executeMethod FrontRoomsTransitionAssets.Prepare -quit -logFile <log>
//   Stats:   renderers, triangles, materials, lights within 46 m of each shot's eye, kit renderers in the camera's frustum,
//            chunk mesh memory and chunk build time, per mode (now = kits off, frame, pick = frame + renovation, reduced).
//     ... -executeMethod FrontRoomsTransitionAssets.Stats -quit -shots <shots.json> -out <dir> [-modes now,frame,pick,reduced]
// ASCII only, no tuples (clone-safe editor script).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using FrontRooms.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

public static class FrontRoomsTransitionAssets
{
    const string SurfaceDir = "Assets/Resources/Surfaces";
    const string TextureDir = SurfaceDir + "/Textures";
    static readonly CultureInfo IC = CultureInfo.InvariantCulture;

    /// <summary>The renovation's texture sets (Tools/lookdev/gen_transition_surfaces.py), each _A, _N, _S.</summary>
    public static readonly string[] NewTextures =
    {
        "Wall_FreshPaint", "Wall_CutIn", "Wall_Primer", "Wall_Stripped", "Wall_Compound", "Slab_Adhesive", "Paper_Backing",
        "Prop_Canvas", "Prop_TapeBeige", "Prop_RollerNap",
    };

    public static readonly string[] Props = { "Kit_TransBucket", "Kit_TransPaintTray", "Kit_TransDropCloth", "Kit_TransTileBox" };

    /// <summary>The reduced tier's import cap (WebGL and handheld players; FrontRoomsTransitionKit.Reduced).</summary>
    public const int ReducedMaxSize = 1024;
    static readonly string[] ReducedPlatforms = { "WebGL", "iPhone", "Android" };

    public static void Prepare()
    {
        var code = 0;
        try { PrepareCore(); }
        catch (Exception e) { Debug.LogError("[Transitions] EXCEPTION " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    public static void PrepareCore()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        // 1. Reduced-tier caps on the renovation textures (desktop untouched).
        foreach (var stem in NewTextures)
            foreach (var suffix in new[] { "_A", "_N", "_S" })
            {
                var path = TextureDir + "/" + stem + suffix + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) { Debug.LogWarning("[Transitions] missing texture " + path); continue; }
                var changed = false;
                foreach (var platform in ReducedPlatforms)
                {
                    var s = importer.GetPlatformTextureSettings(platform);
                    if (s.overridden && s.maxTextureSize == ReducedMaxSize) continue;
                    s.overridden = true;
                    s.maxTextureSize = ReducedMaxSize;
                    if (s.format == 0) s.format = TextureImporterFormat.Automatic;
                    importer.SetPlatformTextureSettings(s);
                    changed = true;
                }
                if (changed) importer.SaveAndReimport();
            }
        // 2. The materials, from FrontRoomsRenderSetup's rows only.
        var n = FrontRoomsRenderSetup.EnsureTransitionSurfaces();
        Debug.Log("[Transitions] " + n + " transition surface materials written from FrontRoomsRenderSetup");
        // 3. The props, so their slots remap to the materials.
        foreach (var k in Props)
        {
            var p = "Assets/Resources/Props/Models/" + k + ".fbx";
            if (File.Exists(p)) AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[Transitions] reimported " + p + " exists " + File.Exists(p));
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[Transitions] " + TextureReport());
    }

    /// <summary>GPU memory of the renovation textures as imported for the desktop, and for the reduced tier's 1024 cap.</summary>
    public static string TextureReport()
    {
        var sb = new StringBuilder("renovation textures:\n");
        double total = 0, reduced = 0;
        foreach (var stem in NewTextures)
            foreach (var suffix in new[] { "_A", "_N", "_S" })
            {
                var t = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureDir + "/" + stem + suffix + ".png");
                if (t == null) continue;
                // Block-compressed bytes per texel x mip chain (BC7 / DXT5 / ASTC 4x4: 1 byte; DXT1 / BC4: 0.5).
                var bpt = t.format == TextureFormat.DXT1 || t.format == TextureFormat.BC4 ? .5 : t.format == TextureFormat.RGBA32 || t.format == TextureFormat.ARGB32 ? 4.0 : 1.0;
                var mips = t.mipmapCount > 1 ? 4.0 / 3.0 : 1.0;
                var bytes = t.width * (double)t.height * bpt * mips;
                total += bytes;
                var w = Mathf.Min(ReducedMaxSize, t.width);
                var h = Mathf.Min(ReducedMaxSize, t.height);
                reduced += w * (double)h * bpt * mips;
                sb.Append("  " + stem + suffix + " " + t.width + "x" + t.height + " " + t.format + " mips " + t.mipmapCount + " " + (bytes / 1048576.0).ToString("0.00", IC) + " MB\n");
            }
        sb.Append("  TOTAL desktop (GPU, mips) " + (total / 1048576.0).ToString("0.0", IC) + " MB; reduced tier (1024 cap) " + (reduced / 1048576.0).ToString("0.0", IC) + " MB\n");
        return sb.ToString();
    }

    // ------------------------------------------------------------------ stats

    [Serializable] class ShotFile { public float[] mapRoot; public Shot[] shots; }
    [Serializable] class Shot { public string id; public int seed; public float[] eyeMap; public float yaw; public float pitch; public float fov; }

    static string Arg(string name, string fallback)
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
        return fallback;
    }

    /// <summary>Set the kits' tool switches for one measuring mode: now (kits off), frame, pick (frame + renovation), reduced.</summary>
    public static void SetMode(string mode)
    {
        FrontRoomsTransitionKit.Kits = mode != "now";
        FrontRoomsTransitionKit.FrameOnly = mode == "frame";
        FrontRoomsTransitionKit.ShareOverride = -1f;
        FrontRoomsTransitionKit.ReducedTier = mode == "reduced" || mode == "framereduced";
        if (mode == "framereduced") FrontRoomsTransitionKit.FrameOnly = true;
    }

    public static void Stats()
    {
        var code = 0;
        try { StatsCore(); }
        catch (Exception e) { Debug.LogError("[Transitions] EXCEPTION " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    sealed class Row
    {
        public string shot, mode;
        public int renderers, materials, tris, lit, shadowed, kitRenderers, kitTris, kitInFrustum, inFrustum, props, posts;
        public double meshMB, buildMs;
        public readonly Dictionary<string, int> byMaterial = new Dictionary<string, int>();
    }

    /// <summary>
    /// The same counts at the game's own spawn of many seeds (-seeds a,b,c): a fairer sample of how many renovated
    /// crossings stand within 46 m than the fixed shots, which sit in one patch of one map.
    /// </summary>
    public static void SeedStats()
    {
        var code = 0;
        try { SeedStatsCore(); }
        catch (Exception e) { Debug.LogError("[Transitions] EXCEPTION " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    static void SeedStatsCore()
    {
        var seeds = Arg("-seeds", "516574485,2554,20388,20261001,1,7,42,99,123,777,1001,1990,2026,3141,4242,5555,8080,9001,12345,31337").Split(',').Select(x => int.Parse(x.Trim(), IC)).ToArray();
        var modes = Arg("-modes", "now,frame,pick,reduced").Split(',');
        var outDir = Arg("-out", Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "transitions"));
        Directory.CreateDirectory(outDir);
        EditorSceneManager.OpenScene("Assets/Scenes/FrontRooms3D.unity", OpenSceneMode.Single);
        var root = new Vector3(26f * ModuleUnits.WorldPeriod, 0f, 26f * ModuleUnits.WorldPeriod);
        var sb = new StringBuilder("seed | mode | renderers <=46 m | triangles | kit renderers | kit tris | renovated crossings in 25 chunks\n");
        var d = new Dictionary<string, List<double>>();
        var dr = new Dictionary<string, List<double>>();
        foreach (var seed in seeds)
        {
            int nowTris = 0, nowR = 0;
            foreach (var mode in modes)
            {
                SetMode(mode);
                FrontRoomsTransitionKit.ResetStats();
                var profile = Object.Instantiate(FrontRoomsLevelProfiles.Resolve());
                profile.hideFlags = HideFlags.DontSave;
                profile.generation = profile.Generation(seed);
                var go = new GameObject("SEEDSTATS map") { hideFlags = HideFlags.DontSave };
                go.transform.position = root;
                var world = go.AddComponent<FrontRoomsMapWorld>();
                world.Profile = profile;
                var eyeW = world.BuildForCapture();
                int renderers = 0, tris = 0, kitR = 0, kitT = 0;
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
                {
                    if (!r.enabled || !r.gameObject.activeInHierarchy || r.bounds.SqrDistance(eyeW) > 46f * 46f) continue;
                    var lod = r.GetComponentInParent<LODGroup>();
                    if (lod != null && r.name.Contains("_LOD") && !r.name.EndsWith("_LOD0")) continue;
                    var mf = r.GetComponent<MeshFilter>();
                    var t = 0;
                    if (mf != null && mf.sharedMesh != null) for (var sm = 0; sm < mf.sharedMesh.subMeshCount; sm++) t += (int)mf.sharedMesh.GetIndexCount(sm) / 3;
                    renderers++; tris += t;
                    if (HasAncestor(r.transform, "Transitions")) { kitR++; kitT += t; }
                }
                var reno = FrontRoomsTransitionKit.StatCellsTaken;
                sb.Append(seed + " | " + mode + " | " + renderers + " | " + tris + " | " + kitR + " | " + kitT + " | " + reno + "\n");
                if (mode == "now") { nowTris = tris; nowR = renderers; }
                else
                {
                    if (!d.ContainsKey(mode)) { d[mode] = new List<double>(); dr[mode] = new List<double>(); }
                    d[mode].Add(tris - nowTris);
                    dr[mode].Add(100.0 * (renderers - nowR) / Math.Max(1, nowR));
                }
                world.Release();
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(profile);
            }
        }
        SetMode("pick");
        sb.Append("\nmode | d triangles mean | median | max | d renderers % mean | max   (" + seeds.Length + " seeds, at each seed's spawn, within 46 m)\n");
        foreach (var kv in d)
        {
            var v = kv.Value.OrderBy(x => x).ToList();
            var r = dr[kv.Key].OrderBy(x => x).ToList();
            sb.Append(kv.Key + " | " + v.Average().ToString("0", IC) + " | " + v[v.Count / 2].ToString("0", IC) + " | " + v.Max().ToString("0", IC) + " | " + r.Average().ToString("0.0", IC) + " | " + r.Max().ToString("0.0", IC) + "\n");
        }
        File.WriteAllText(Path.Combine(outDir, "transition_seed_stats.txt"), sb.ToString().Replace("\r", ""));
        Debug.Log("[Transitions] seed stats\n" + sb);
    }

    static void StatsCore()
    {
        var file = JsonUtility.FromJson<ShotFile>(File.ReadAllText(Arg("-shots", null)));
        var outDir = Arg("-out", Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "transitions"));
        var modes = Arg("-modes", "now,frame,pick,reduced").Split(',');
        var only = Arg("-only", "");
        Directory.CreateDirectory(outDir);
        EditorSceneManager.OpenScene("Assets/Scenes/FrontRooms3D.unity", OpenSceneMode.Single);
        var root = new Vector3(file.mapRoot[0], file.mapRoot[1], file.mapRoot[2]);
        var rows = new List<Row>();
        var sb = new StringBuilder();
        sb.Append("shot | mode | renderers <=46 m | in frustum | materials | triangles | lit | shadowed | kit renderers | kit tris | kit in frustum | props | free posts | chunk mesh MB (25) | chunk build ms (median)\n");
        foreach (var s in file.shots)
        {
            if (only.Length > 0 && !only.Split(',').Contains(s.id)) continue;
            foreach (var mode in modes)
            {
                SetMode(mode);
                FrontRoomsTransitionKit.ResetStats();
                var eye = new Vector3(s.eyeMap[0], s.eyeMap[1], s.eyeMap[2]);
                var profile = Object.Instantiate(FrontRoomsLevelProfiles.Resolve());
                profile.hideFlags = HideFlags.DontSave;
                profile.generation = profile.Generation(s.seed);
                var go = new GameObject("STATS map") { hideFlags = HideFlags.DontSave };
                go.transform.position = root;
                var world = go.AddComponent<FrontRoomsMapWorld>();
                world.Profile = profile;
                world.OverrideSpawn(new Vector3(eye.x, 0f, eye.z), 0f);
                world.BuildForCapture();
                world.TickFixturesForTools(0f);
                var eyeW = root + eye;
                var row = new Row { shot = s.id, mode = mode, posts = FrontRoomsTransitionKit.StatPosts };
                var mats = new HashSet<Material>();
                var fov = s.fov > 0f ? s.fov : 76f;
                var view = Matrix4x4.Perspective(fov, 16f / 9f, .06f, 46f) * Matrix4x4.TRS(eyeW, Quaternion.Euler(s.pitch, s.yaw, 0f), new Vector3(1f, 1f, -1f)).inverse;
                var planes = GeometryUtility.CalculateFrustumPlanes(view);
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
                {
                    if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                    if (r.bounds.SqrDistance(eyeW) > 46f * 46f) continue;
                    var lod = r.GetComponentInParent<LODGroup>();
                    if (lod != null && r.name.Contains("_LOD") && !r.name.EndsWith("_LOD0")) continue;
                    var mf = r.GetComponent<MeshFilter>();
                    var t = 0;
                    if (mf != null && mf.sharedMesh != null) for (var sm = 0; sm < mf.sharedMesh.subMeshCount; sm++) t += (int)mf.sharedMesh.GetIndexCount(sm) / 3;
                    row.renderers++;
                    row.tris += t;
                    var mk = (r.sharedMaterial != null ? r.sharedMaterial.name : "null") + (HasAncestor(r.transform, "Transitions") ? " [kit]" : "");
                    if (r.name.StartsWith("Kit_") || HasAncestor(r.transform, "Renovation prop")) mk = HasAncestor(r.transform, "Renovation prop") ? "renovation props [kit]" : "office/pile kits";
                    row.byMaterial.TryGetValue(mk, out var bm);
                    row.byMaterial[mk] = bm + t;
                    foreach (var m in r.sharedMaterials) if (m != null) mats.Add(m);
                    var inF = GeometryUtility.TestPlanesAABB(planes, r.bounds);
                    if (inF) row.inFrustum++;
                    if (HasAncestor(r.transform, "Transitions"))
                    {
                        row.kitRenderers++;
                        row.kitTris += t;
                        if (inF) row.kitInFrustum++;
                    }
                }
                foreach (var t in go.GetComponentsInChildren<Transform>()) if (t.name.StartsWith("Renovation prop") && (t.position - eyeW).sqrMagnitude < 46f * 46f) row.props++;
                row.materials = mats.Count;
                foreach (var l in go.GetComponentsInChildren<Light>())
                    if (l.enabled && l.intensity > 0f && (l.transform.position - eyeW).sqrMagnitude < 46f * 46f) { row.lit++; if (l.shadows != LightShadows.None) row.shadowed++; }
                // Mesh memory of the 25 chunks (every mesh under the map root that is not an asset).
                long bytes = 0;
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
                    if (mf.sharedMesh != null && !EditorUtility.IsPersistent(mf.sharedMesh)) bytes += Profiler.GetRuntimeMemorySizeLong(mf.sharedMesh);
                foreach (var mc in go.GetComponentsInChildren<MeshCollider>(true))
                    if (mc.sharedMesh != null && !EditorUtility.IsPersistent(mc.sharedMesh)) bytes += Profiler.GetRuntimeMemorySizeLong(mc.sharedMesh);
                row.meshMB = bytes / 1048576.0;
                // Chunk build time: rebuild each of the 25 chunks once (furnished, as RebuildChunk does), median.
                var centre = MapGrid.ChunkOf(FrontRoomsMapWorld.CellAt(eye));
                var times = new List<double>();
                for (var dy = -2; dy <= 2; dy++)
                    for (var dx = -2; dx <= 2; dx++)
                    {
                        var w = System.Diagnostics.Stopwatch.StartNew();
                        world.RebuildChunk(new GridCoord(centre.x + dx, centre.y + dy));
                        times.Add(w.Elapsed.TotalMilliseconds);
                    }
                times.Sort();
                row.buildMs = times[times.Count / 2];
                rows.Add(row);
                sb.Append(s.id + " | " + mode + " | " + row.renderers + " | " + row.inFrustum + " | " + row.materials + " | " + row.tris + " | " + row.lit + " | " + row.shadowed
                    + " | " + row.kitRenderers + " | " + row.kitTris + " | " + row.kitInFrustum + " | " + row.props + " | " + row.posts
                    + " | " + row.meshMB.ToString("0.00", IC) + " | " + row.buildMs.ToString("0.00", IC) + "\n");
                if (mode != "now")
                {
                    sb.Append("   frame: " + FrontRoomsTransitionKit.FrameKit.Count + "\n   renovation (25 chunks): ");
                    foreach (var kv in FrontRoomsTransitionKit.RenoKit.Stats.OrderBy(k => k.Key)) sb.Append(kv.Key + "=" + kv.Value.ToString("0.##", IC) + "; ");
                    sb.Append("\n   pieces taken " + FrontRoomsTransitionKit.StatPiecesTaken + ", cells taken " + FrontRoomsTransitionKit.StatCellsTaken + ", non-finite tris dropped " + FrontRoomsTransitionKit.TransitionMesh.NonFinite + "\n");
                }
                world.Release();
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(profile);
            }
        }
        SetMode("pick");
        // Deltas against "now" per shot.
        sb.Append("\nshot | mode | d triangles | d renderers (46 m) | d renderers % | d in frustum | kit in frustum | d materials | d mesh MB | d build ms (median)\n");
        foreach (var r in rows)
        {
            if (r.mode == "now") continue;
            var n = rows.FirstOrDefault(q => q.shot == r.shot && q.mode == "now");
            if (n == null) continue;
            sb.Append(r.shot + " | " + r.mode + " | " + (r.tris - n.tris) + " | " + (r.renderers - n.renderers) + " | " + (100.0 * (r.renderers - n.renderers) / Math.Max(1, n.renderers)).ToString("0.0", IC)
                + " | " + (r.inFrustum - n.inFrustum) + " | " + r.kitInFrustum + " | " + (r.materials - n.materials) + " | " + (r.meshMB - n.meshMB).ToString("0.00", IC)
                + " | " + (r.buildMs - n.buildMs).ToString("0.00", IC) + "\n");
        }
        // Where the triangles go: per material (block meshes and kit renderers), mode minus now, the larger first.
        sb.Append("\ntriangle deltas by material (mode - now, within 46 m):\n");
        foreach (var r in rows)
        {
            if (r.mode == "now") continue;
            var n = rows.FirstOrDefault(q => q.shot == r.shot && q.mode == "now");
            if (n == null) continue;
            var keys = r.byMaterial.Keys.Union(n.byMaterial.Keys).ToList();
            var deltas = keys.Select(k => new KeyValuePair<string, int>(k, (r.byMaterial.TryGetValue(k, out var a) ? a : 0) - (n.byMaterial.TryGetValue(k, out var b) ? b : 0))).Where(kv => kv.Value != 0).OrderByDescending(kv => Math.Abs(kv.Value)).Take(14);
            sb.Append("  " + r.shot + " " + r.mode + ": " + string.Join("; ", deltas.Select(kv => kv.Key + " " + (kv.Value > 0 ? "+" : "") + kv.Value)) + "\n");
        }
        sb.Append("\n" + TextureReport());
        File.WriteAllText(Path.Combine(outDir, "transition_stats.txt"), sb.ToString().Replace("\r", ""));
        Debug.Log("[Transitions] stats\n" + sb);
    }

    static bool HasAncestor(Transform t, string prefix)
    {
        for (var p = t; p != null; p = p.parent) if (p.name.StartsWith(prefix)) return true;
        return false;
    }
}
