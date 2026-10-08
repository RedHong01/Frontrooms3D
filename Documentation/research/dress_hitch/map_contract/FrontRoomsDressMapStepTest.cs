using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using FrontRooms.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

/// <summary>
/// Map-level check of the dress-queue integration (map ticket): the same map
/// built with the synchronous dressers one room per frame (today), with the
/// stepped jobs at 3 ms and at 0 ms (one unit per step), and with buildAllNow
/// (Complete), must come out identical, object for object. Also: RebuildChunk
/// in the middle of a job, Settled while a job is open, and the per-frame
/// furnishing cost of each way.
/// -executeMethod FrontRoomsDressMapStepTest.RunBatch -mapStepOut out.txt [-mapStepBaseline dir]
/// -executeMethod FrontRoomsDressMapStepTest.RunBaseline -mapStepOut out.txt -mapStepBaseline dir
///   (any map, patched or not: one room per frame with whatever dressers are
///   compiled in; writes each map's exact dump and furnishing-frame times to
///   dir. RunBatch with -mapStepBaseline then requires the stepped map to equal
///   those dumps, e.g. today's map with the original dressers.)
/// </summary>
public static class FrontRoomsDressMapStepTest
{
    const BindingFlags S = BindingFlags.NonPublic | BindingFlags.Static;
    const BindingFlags I = BindingFlags.NonPublic | BindingFlags.Instance;
    static readonly Type W = typeof(FrontRoomsMapWorld);

    static readonly (int seed, GridCoord? focus, string tag)[] Maps =
    {
        (20388, new GridCoord(16, 10), "20388@(16,10)"), (2554, null, "2554"), (777, new GridCoord(3, -2), "777@(3,-2)"),
        (4242, null, "4242"), (20261001, null, "20261001"), (31337, null, "31337"), (99, null, "99"),
    };

    static string Arg(string key, string fallback)
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length - 1; i++) if (args[i] == key) return args[i + 1];
        return fallback;
    }

    static string FileTag(string tag) => tag.Replace("@", "_at_").Replace("(", "").Replace(")", "").Replace(",", "_");

    /// <summary>
    /// The map as it is compiled (patched or not), furnished one room per
    /// frame with the synchronous dressers. Writes per map: tag.dump.txt (exact
    /// dump) and tag.frames.txt (ms per furnishing frame), and a summary.
    /// </summary>
    public static void RunBaseline()
    {
        var outPath = Arg("-mapStepOut", "map-step-baseline.txt");
        var dir = Arg("-mapStepBaseline", "map-step-baseline");
        Directory.CreateDirectory(dir);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var lines = new List<string>();
        var fOfficeBegin = W.GetField("officeBegin", S);
        var fPileBegin = W.GetField("pileBegin", S);
        var watch = Stopwatch.StartNew();
        FrontRoomsMapWorld.Prewarm();
        lines.Add("map " + (fOfficeBegin != null ? "patched (stepping switched off for this run)" : "unpatched") + "; Prewarm " + watch.Elapsed.TotalMilliseconds.ToString("0.0", CultureInfo.InvariantCulture) + " ms");
        fOfficeBegin?.SetValue(null, null);
        fPileBegin?.SetValue(null, null);
        // One throwaway build so first-use loads and JIT are not counted against the first map.
        Build(Maps[0].seed, Maps[0].focus, Mode.Loop, 3f, out _, out _);
        var all = new List<double>();
        foreach (var (seed, focus, tag) in Maps)
        {
            var a = Build(seed, focus, Mode.Loop, 3f, out var fa, out _);
            var d = Build(seed, focus, Mode.AllNow, 3f, out _, out _);
            File.WriteAllText(Path.Combine(dir, FileTag(tag) + ".dump.txt"), a);
            File.WriteAllText(Path.Combine(dir, FileTag(tag) + ".frames.txt"), string.Join("\n", fa.ConvertAll(x => x.ToString("0.000", CultureInfo.InvariantCulture))));
            all.AddRange(fa);
            lines.Add((a == d ? "ok   " : "FAIL ") + tag + ": one room per frame == buildAllNow; " + a.Split('\n').Length + " lines; furnishing frames " + fa.Count + ", mean " + Mean(fa) + " p95 " + Pct(fa, .95) + " max " + Max(fa) + " ms");
        }
        lines.Add("all maps, frames that furnished: n " + all.Count + " mean " + Mean(all) + " median " + Pct(all, .5) + " p95 " + Pct(all, .95) + " p99 " + Pct(all, .99) + " max " + Max(all) + " ms (module props included)");
        File.WriteAllText(outPath, string.Join("\n", lines));
        Debug.Log("[DressMapStep] baseline -> " + outPath);
    }

    public static void RunBatch()
    {
        var outPath = Arg("-mapStepOut", "map-step.txt");
        var baseline = Arg("-mapStepBaseline", null);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var lines = new List<string>();
        var failures = 0;
        void Check(bool ok, string what) { if (!ok) failures++; lines.Add((ok ? "ok   " : "FAIL ") + what); }

        var fOfficeBegin = W.GetField("officeBegin", S);
        var fPileBegin = W.GetField("pileBegin", S);
        if (fOfficeBegin == null || fPileBegin == null) throw new Exception("FrontRoomsMapWorld has no officeBegin / pileBegin: not the patched map");
        var watch = Stopwatch.StartNew();
        FrontRoomsMapWorld.Prewarm();
        lines.Add("Prewarm (ResolveDressers + FrontRoomsDressJob.WarmUp + grade profile) in edit mode: " + watch.Elapsed.TotalMilliseconds.ToString("0.0", CultureInfo.InvariantCulture) + " ms");
        var officeBegin = fOfficeBegin.GetValue(null);
        var pileBegin = fPileBegin.GetValue(null);
        Check(officeBegin != null && pileBegin != null, "ResolveDressers binds BeginDress and BeginBuild");

        var maps = Maps;
        // One throwaway build (stepped) so first-use costs are not counted against the first map.
        Build(Maps[0].seed, Maps[0].focus, Mode.Loop, 3f, out _, out _);
        var allFramesA = new List<double>(); var allFramesB = new List<double>();
        foreach (var (seed, focus, tag) in maps)
        {
            fOfficeBegin.SetValue(null, null); fPileBegin.SetValue(null, null);
            var a = Build(seed, focus, Mode.Loop, 3f, out var fa, out _);
            fOfficeBegin.SetValue(null, officeBegin); fPileBegin.SetValue(null, pileBegin);
            var b = Build(seed, focus, Mode.Loop, 3f, out var fb, out var settledB);
            var c = Build(seed, focus, Mode.Loop, 0f, out var fc, out _);
            var d = Build(seed, focus, Mode.AllNow, 3f, out _, out _);
            allFramesA.AddRange(fa); allFramesB.AddRange(fb);
            Check(a.Length > 0 && a == b, tag + ": stepped 3 ms == one room per frame (synchronous dressers)" + Diff(a, b));
            Check(a == c, tag + ": stepped 0 ms (one unit per step) == synchronous" + Diff(a, c));
            Check(a == d, tag + ": buildAllNow (Complete) == synchronous" + Diff(a, d));
            Check(settledB, tag + ": Settled stays false while a job is open, true at the end");
            if (baseline != null)
            {
                var file = Path.Combine(baseline, FileTag(tag) + ".dump.txt");
                if (!File.Exists(file)) Check(false, tag + ": no baseline dump " + file);
                else
                {
                    var o = File.ReadAllText(file);
                    Check(o == b, tag + ": stepped 3 ms == baseline run (" + baseline + ")" + Diff(o, b));
                }
            }
            lines.Add("     " + tag + ": furnishing frames, synchronous " + fa.Count + " (worst " + Max(fa) + " ms), stepped 3 ms " + fb.Count + " (worst " + Max(fb) + " ms, p95 " + Pct(fb, .95) + "), stepped 0 ms " + fc.Count);
            // RebuildChunk on the chunk of an open job: same as rebuilding it after a synchronous build.
            fOfficeBegin.SetValue(null, null); fPileBegin.SetValue(null, null);
            var r = Build(seed, focus, Mode.RebuildAfter, 3f, out _, out _, out var rebuilt);
            fOfficeBegin.SetValue(null, officeBegin); fPileBegin.SetValue(null, pileBegin);
            if (rebuilt.HasValue)
            {
                // 0 ms: every job stays open for several frames, so the rebuild lands mid-job.
                var e = Build(seed, focus, Mode.RebuildMidJob, 0f, out _, out _, out var rebuiltMid, rebuilt);
                Check(rebuiltMid.HasValue && r == e, tag + ": RebuildChunk " + rebuilt.Value + " mid-job == rebuilt after a synchronous build" + Diff(r, e));
            }
        }
        allFramesA.Sort(); allFramesB.Sort();
        lines.Add("all maps, frames that furnished: synchronous n " + allFramesA.Count + " mean " + Mean(allFramesA) + " median " + Pct(allFramesA, .5) + " p95 " + Pct(allFramesA, .95) + " p99 " + Pct(allFramesA, .99) + " max " + Max(allFramesA)
            + " ms | stepped 3 ms n " + allFramesB.Count + " mean " + Mean(allFramesB) + " median " + Pct(allFramesB, .5) + " p95 " + Pct(allFramesB, .95) + " p99 " + Pct(allFramesB, .99) + " max " + Max(allFramesB) + " ms (module props included)");
        if (baseline != null)
        {
            File.WriteAllText(Path.Combine(baseline, "stepped3.frames.txt"), string.Join("\n", allFramesB.ConvertAll(x => x.ToString("0.000", CultureInfo.InvariantCulture))));
            File.WriteAllText(Path.Combine(baseline, "sync_new.frames.txt"), string.Join("\n", allFramesA.ConvertAll(x => x.ToString("0.000", CultureInfo.InvariantCulture))));
        }
        lines.Insert(0, (failures == 0 ? "PASS" : "FAIL") + ": " + failures + " failed");
        File.WriteAllText(outPath, string.Join("\n", lines));
        Debug.Log("[DressMapStep] " + lines[0] + " -> " + outPath);
        if (failures > 0) throw new Exception("[DressMapStep] " + lines[0]);
    }

    enum Mode { Loop, AllNow, RebuildAfter, RebuildMidJob }

    static string Build(int seed, GridCoord? focus, Mode mode, float budget, out List<double> frames, out bool settledOk)
        => Build(seed, focus, mode, budget, out frames, out settledOk, out _);

    /// <summary>Build a map round the spawn the way BuildForCapture does, furnishing per <paramref name="mode"/>; returns the exact dump.</summary>
    static string Build(int seed, GridCoord? focus, Mode mode, float budget, out List<double> frames, out bool settledOk, out GridCoord? rebuilt, GridCoord? rebuildThis = null)
    {
        frames = new List<double>();
        settledOk = true;
        rebuilt = null;
        var profile = Object.Instantiate(FrontRoomsLevelProfiles.Resolve());
        profile.hideFlags = HideFlags.DontSave;
        profile.generation.seed = seed;
        var root = new GameObject("MAP STEP " + seed) { hideFlags = HideFlags.DontSave };
        var map = root.AddComponent<FrontRoomsMapWorld>();
        map.Profile = profile;
        if (focus.HasValue)
            map.OverrideSpawn(new Vector3((focus.Value.x * MapGrid.ChunkCells + MapGrid.ChunkCells / 2 + .5f) * MapGrid.CellSize, .05f,
                (focus.Value.y * MapGrid.ChunkCells + MapGrid.ChunkCells / 2 + .5f) * MapGrid.CellSize));
        try
        {
            if (mode == Mode.AllNow) map.BuildForCapture();
            else
            {
                // BuildForCapture's steps, with Begin(eye, buildAllNow: false), then the stream and the dress queue by hand.
                Call(map, "TakeProfile", map.Profile, map.Profile.generation.seed);
                Call(map, "CreateCache");
                W.GetField("block", I).SetValue(map, new MaterialPropertyBlock());
                Call(map, "BuildMaterials");
                Call(map, "ApplyRenderSettings");
                var eye = new GameObject("CAPTURE / eye").transform;
                eye.SetParent(map.transform, false);
                eye.localPosition = (Vector3)Call(map, "SpawnPoint");
                W.GetField("dressBudgetMs", I)?.SetValue(map, budget);
                map.Begin(eye, false);
                var center = Call(map, "ChunkOf", eye.position);
                Call(map, "Stream", center, int.MaxValue);
                var guard = 0;
                while (guard++ < 1000000)
                {
                    var w = Stopwatch.StartNew();
                    var worked = (bool)(W.GetField("currentJob", I) != null ? Call(map, "DressNext", false) : Call(map, "DressNext"));
                    if (!worked) break;
                    frames.Add(w.Elapsed.TotalMilliseconds);
                    var open = W.GetField("currentJob", I)?.GetValue(map) != null;
                    if (open && map.Settled) settledOk = false;
                    if (mode == Mode.RebuildMidJob && open && rebuilt == null)
                    {
                        // Rebuild the chunk of a job that is still open (the first one, or the asked one).
                        var room = W.GetField("currentRoom", I).GetValue(map);
                        var data = room.GetType().GetField("data").GetValue(room);
                        var coord = (GridCoord)data.GetType().GetField("coord").GetValue(data);
                        if (!rebuildThis.HasValue || rebuildThis.Value.Equals(coord))
                        {
                            map.RebuildChunk(coord);
                            rebuilt = coord;
                        }
                    }
                }
                Call(map, "TickFixtures", 0f);
                if (!map.Settled) settledOk = false;
                if (mode == Mode.RebuildAfter)
                {
                    // Rebuild the chunk of the first room that was furnished.
                    var first = FirstFurnishedChunk(map);
                    if (first.HasValue) { map.RebuildChunk(first.Value); rebuilt = first; }
                }
            }
            var sb = new StringBuilder();
            for (var i = 0; i < map.transform.childCount; i++) Strict(map.transform.GetChild(i), 0, sb);
            return sb.ToString();
        }
        finally
        {
            map.Release();
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(profile);
        }
    }

    static GridCoord? FirstFurnishedChunk(FrontRoomsMapWorld map)
    {
        var rx = new System.Text.RegularExpressions.Regex(@"Chunk \((-?\d+), (-?\d+)\)");
        for (var i = 0; i < map.transform.childCount; i++)
        {
            var chunk = map.transform.GetChild(i);
            for (var k = 0; k < chunk.childCount; k++)
            {
                var n = chunk.GetChild(k).name;
                if (n == "office dressing" || n.StartsWith("furniture pile"))
                {
                    var m = rx.Match(chunk.name);
                    if (m.Success) return new GridCoord(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value));
                }
            }
        }
        return null;
    }

    static object Call(object target, string name, params object[] a)
    {
        foreach (var m in W.GetMethods(I))
            if (m.Name == name && m.GetParameters().Length == a.Length) return m.Invoke(target, a);
        throw new MissingMethodException(W.Name, name);
    }

    static string Diff(string a, string b)
    {
        if (a == b) return "";
        var la = a.Split('\n'); var lb = b.Split('\n');
        for (var i = 0; i < Math.Min(la.Length, lb.Length); i++)
            if (la[i] != lb[i]) return " — first difference at line " + i + ":\n        " + Cut(la[i]) + "\n        " + Cut(lb[i]);
        return " — lengths " + la.Length + " vs " + lb.Length;
    }

    static string Mean(List<double> v) { if (v.Count == 0) return "-"; var m = 0.0; foreach (var x in v) m += x; return (m / v.Count).ToString("0.00", CultureInfo.InvariantCulture); }

    static string Cut(string s) => s.Length > 240 ? s.Substring(0, 240) + "…" : s;
    static string Max(List<double> v) { var m = 0.0; foreach (var x in v) m = Math.Max(m, x); return m.ToString("0.00", CultureInfo.InvariantCulture); }
    static string Pct(List<double> v, double p)
    {
        if (v.Count == 0) return "-";
        var s = new List<double>(v); s.Sort();
        return s[(int)Math.Floor((s.Count - 1) * p)].ToString("0.00", CultureInfo.InvariantCulture);
    }

    /// <summary>Exact dump (float bits, flags, renderers, colliders, LOD groups), no instance ids (each map makes its own meshes).</summary>
    static void Strict(Transform t, int depth, StringBuilder sb)
    {
        var go = t.gameObject;
        sb.Append(depth).Append(' ').Append(t.name).Append(" a").Append(go.activeSelf ? 1 : 0).Append(" l").Append(go.layer).Append(" t").Append(go.tag)
          .Append(" h").Append((int)go.hideFlags).Append(" p").Append(X(t.localPosition)).Append(" r").Append(X(t.localRotation)).Append(" s").Append(X(t.localScale));
        foreach (var comp in t.GetComponents<Component>())
        {
            if (comp is Transform) continue;
            sb.Append(" |").Append(comp == null ? "null" : comp.GetType().Name);
            switch (comp)
            {
                case MeshFilter mf:
                    sb.Append(' ').Append(mf.sharedMesh != null ? mf.sharedMesh.name + " v" + mf.sharedMesh.vertexCount : "null");
                    break;
                case Renderer r:
                    sb.Append(' ').Append(r.enabled).Append(' ').Append(r.shadowCastingMode).Append(' ').Append(r.receiveShadows).Append(" [");
                    foreach (var m in r.sharedMaterials) sb.Append(m != null ? m.name : "null").Append(',');
                    sb.Append(']');
                    break;
                case BoxCollider bc:
                    sb.Append(' ').Append(bc.enabled).Append(' ').Append(bc.isTrigger).Append(" c").Append(X(bc.center)).Append(" z").Append(X(bc.size));
                    break;
                case Collider col:
                    sb.Append(' ').Append(col.enabled).Append(' ').Append(col.isTrigger);
                    break;
                case LODGroup lg:
                    sb.Append(" n").Append(lg.lodCount).Append(" size").Append(B(lg.size));
                    break;
            }
        }
        sb.Append('\n');
        for (var i = 0; i < t.childCount; i++) Strict(t.GetChild(i), depth + 1, sb);
    }

    static string X(Vector3 v) => B(v.x) + "," + B(v.y) + "," + B(v.z);
    static string X(Quaternion q) => B(q.x) + "," + B(q.y) + "," + B(q.z) + "," + B(q.w);
    static string B(float f) => BitConverter.ToInt32(BitConverter.GetBytes(f), 0).ToString("x8");
}
