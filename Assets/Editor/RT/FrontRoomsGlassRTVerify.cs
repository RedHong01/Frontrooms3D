// G14 P0 + P1 VERIFY HARNESS (clone proj_rt). Built on the runtime-probe harness (FrontRoomsGlassRTProbe, 02).
// Plays Assets/Scenes/FrontRooms3D.unity with the interaction-audit / G11 start (Random.InitState(4242) before the
// title start -> run seed 516574485), swaps the map's panes to Glass_Window at runtime (a clone-only TEST SWITCH, no
// MapWorld edit: design sec. 2.4 C8), sets the Level 0 zone cube, and measures the production RT path:
//   views v1-v5 (head-on level / centre, 50 deg, steep, close) RT Off / planar reference / High / Ultra, alignment,
//   orientation, transmission, misses, occlusion and lifetime cases, dark beyond, Office, lamp sync, temporal
//   accumulation and a moving strafe, cameras, capacity, the fracture API, a free walk, GPU cost, hit-shading parity.
// Output: <clone>/Verification/rt_p0p1 (PNG frames + verify_log.txt + metrics.tsv). Sheets: Tools/rt/compose_rt_sheets.py.
// Run: Unity -batchmode -projectPath <proj_rt> -executeMethod FrontRoomsGlassRTVerify.RunBatch -logFile <f>
//      [-frVerifyOnly name1,name2] (phases: views,orient,occluder,relay,broken,stream,dark,office,lamps,temporal,
//       cameras,capacity,fracture,walk,cost,parity,ablation,pairs,tagproof,failclosed, and since the 2026-10-14 fix pass
//       oblique,darkrelay,moving,backimage,hdroff,officeparity, and since fix round 32 jump (stream-in after a jump: the
//       gate's hold, holes while shown, lamps, frames to registered), and since fix round 33 keypickup (a presented key
//       and a chunk rebuild must not hold the gate or leave a square counted as unbuilt) and restart (scene reloads
//       with the gate held and open: burst off, gates = live cameras, walk2); shaderproof runs only when named). After the run
//       the harness leaves Play Mode and checks that the plugin's native scene is released (codex audit F4).
#if UNITY_EDITOR_OSX
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using FrontRooms.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class FrontRoomsGlassRTVerify
{
    const string ScenePath = "Assets/Scenes/FrontRooms3D.unity";
    const string ActiveKey = "FrontRooms.RTVerify.Active";
    const string DeadlineKey = "FrontRooms.RTVerify.Deadline";
    const string DoneKey = "FrontRooms.RTVerify.Done";
    const string OnlyKey = "FrontRooms.RTVerify.Only";
    const BindingFlags BF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    const int W = 1920, H = 1080;

    static string outDir;
    static readonly StringBuilder log = new StringBuilder();
    static readonly StringBuilder metrics = new StringBuilder("key\tvalue\n");
    static readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();
    static int waitUntilFrame;
    static bool finished;
    static HashSet<string> only;

    static FrontRooms3DGame game;
    static FrontRoomsMapWorld map;
    static Camera cam;
    static FrontRoomsGlassRTCamera camState;
    static RenderTexture rt, rt1440, rtHdr;
    static Texture2D frameTex, tiny;
    static Material glassWindow;

    // fix round 32 (review 2 engineering #3): small structs instead of C# tuples (clone editor scripts stay tuple-free)
    struct CellPair { public GridCoord a, b; public CellPair(GridCoord a, GridCoord b) { this.a = a; this.b = b; } }
    struct LabelledPair { public GridCoord a, b; public string label; public LabelledPair(GridCoord a, GridCoord b, string label) { this.a = a; this.b = b; this.label = label; } }
    struct PxDiff { public long px; public double max; }
    struct RendererMats { public Renderer r; public Material[] original; }
    struct PoseId { public string id; public View v; }
    struct Centre { public double x, y; public int n; }
    struct CrawlResult { public double crawl, rel; public int count; }
    struct GradPx { public float g; public int i; }
    struct SpeckCount { public int inside, checkedPx; }
    struct LensPx { public Renderer renderer; public List<int> px; }
    struct ParityCfg
    {
        public string n; public FrontRoomsGlassRTQuality q; public int lamps, rays, extra; public float radius; public Vector3 knobs;
        public ParityCfg(string n, FrontRoomsGlassRTQuality q, int lamps, int rays, int extra, float radius, Vector3 knobs)
        { this.n = n; this.q = q; this.lamps = lamps; this.rays = rays; this.extra = extra; this.radius = radius; this.knobs = knobs; }
    }
    struct ShadowKnobs { public int highLamps, ultraLamps, highRays, ultraRays, highExtra, ultraExtra; public float radius; }

    static FrontRoomsGlassRTVerify()
    {
        if (SessionState.GetBool(ActiveKey, false)) EditorApplication.update += Tick;
    }

    public static void RunBatch()
    {
        var args = Environment.GetCommandLineArgs();
        var o = "";
        for (var i = 0; i + 1 < args.Length; i++) if (args[i] == "-frVerifyOnly") o = args[i + 1];
        SessionState.SetString(OnlyKey, o);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetFloat(DeadlineKey, (float)EditorApplication.timeSinceStartup + 2700f);
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }

    static string OutDir
    {
        get
        {
            if (outDir == null)
            {
                outDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "rt_p0p1");
                Directory.CreateDirectory(outDir);
            }
            return outDir;
        }
    }

    static bool Want(string phase) => only == null || only.Count == 0 || only.Contains(phase);

    static void Log(string s)
    {
        var line = "[" + Time.frameCount + "] " + s;
        log.AppendLine(line);
        Debug.Log("[RTVerify] " + line);
        try { File.WriteAllText(Path.Combine(OutDir, "verify_log.txt"), log.ToString()); } catch { }
    }

    static void M(string key, double value) { metrics.Append(key).Append('\t').Append(value.ToString("0.####", CultureInfo.InvariantCulture)).Append('\n'); }
    static void M(string key, string value) { metrics.Append(key).Append('\t').Append(value).Append('\n'); }

    static void Finish()
    {
        finished = true;
        Time.captureDeltaTime = 0f;
        SessionState.SetBool(DoneKey, true);
        try
        {
            File.WriteAllText(Path.Combine(OutDir, "verify_log.txt"), log.ToString());
            File.WriteAllText(Path.Combine(OutDir, "metrics.tsv"), metrics.ToString());
        }
        catch { }
    }

    static int exitWait = -1;
    static double exitStart;

    static void Tick()
    {
        var timedOut = EditorApplication.timeSinceStartup > SessionState.GetFloat(DeadlineKey, float.MaxValue);
        if (finished || timedOut || SessionState.GetBool(DoneKey, false))
        {
            if (timedOut && !finished && !SessionState.GetBool(DoneKey, false)) { Log("TIMEOUT"); Finish(); }
            if (EditorApplication.isPlaying) { EditorApplication.ExitPlaymode(); return; }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            // F4 (codex audit): after Play Mode the plugin must drop its scene (meshes, BLAS, geometry pages, textures)
            if (exitWait < 0) { exitWait = 0; exitStart = EditorApplication.timeSinceStartup; }
            if (exitWait++ < 400 && !FrontRoomsGlassRTSystem.ExitReleased && EditorApplication.timeSinceStartup - exitStart < 20.0) return;
            {
                var line = "[edit] EXIT RELEASE (after Play Mode): released " + FrontRoomsGlassRTSystem.ExitReleased + " after " + FrontRoomsGlassRTSystem.ExitPumpEvents + " release events / "
                    + exitWait + " editor updates | native meshes " + FrontRoomsGlassRTNative.Get(FrontRoomsGlassRTNative.Stat.Meshes) + " | retired objects waiting " + FrontRoomsGlassRTNative.Get(FrontRoomsGlassRTNative.Stat.Retired)
                    + " | BLAS " + F3(FrontRoomsGlassRTNative.Get(FrontRoomsGlassRTNative.Stat.BlasBytes) / 1048576.0) + " MB | geometry " + F3(FrontRoomsGlassRTNative.Get(FrontRoomsGlassRTNative.Stat.GeometryBytes) / 1048576.0)
                    + " MB | cameras (C#) " + FrontRoomsGlassRTSystem.Cameras.Count + " | registered (C#) " + FrontRoomsGlassRTSystem.RegisteredInstances;
                log.AppendLine(line);
                Debug.Log("[RTVerify] " + line);
                metrics.Append("exit.released\t").Append(FrontRoomsGlassRTSystem.ExitReleased ? 1 : 0).Append('\n');
                metrics.Append("exit.native_meshes\t").Append(FrontRoomsGlassRTNative.Get(FrontRoomsGlassRTNative.Stat.Meshes)).Append('\n');
                metrics.Append("exit.blas_bytes\t").Append(FrontRoomsGlassRTNative.Get(FrontRoomsGlassRTNative.Stat.BlasBytes)).Append('\n');
                metrics.Append("exit.geometry_bytes\t").Append(FrontRoomsGlassRTNative.Get(FrontRoomsGlassRTNative.Stat.GeometryBytes)).Append('\n');
                try
                {
                    File.WriteAllText(Path.Combine(OutDir, "verify_log.txt"), log.ToString());
                    File.WriteAllText(Path.Combine(OutDir, "metrics.tsv"), metrics.ToString());
                }
                catch { }
            }
            EditorApplication.update -= Tick;
            SessionState.EraseBool(ActiveKey);
            SessionState.EraseBool(DoneKey);
            if (Application.isBatchMode) EditorApplication.Exit(0);
            return;
        }
        if (!EditorApplication.isPlaying) return;
        if (stack.Count == 0)
        {
            var o = SessionState.GetString(OnlyKey, "");
            only = new HashSet<string>(o.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            stack.Push(Main());
        }
        if (Time.frameCount < waitUntilFrame) return;
        try
        {
            for (var guard = 0; guard < 1000; guard++)
            {
                var top = stack.Peek();
                if (!top.MoveNext())
                {
                    stack.Pop();
                    if (stack.Count == 0) { Finish(); return; }
                    continue;
                }
                var cur = top.Current;
                if (cur is IEnumerator sub) { stack.Push(sub); continue; }
                waitUntilFrame = Time.frameCount + (cur is int n ? Mathf.Max(1, n) : 1);
                return;
            }
        }
        catch (Exception e)
        {
            Log("EXCEPTION " + e);
            Finish();
        }
    }

    // ------------------------------------------------------------------ reflection helpers (game internals)
    static FieldInfo FI(Type t, string n)
    {
        for (; t != null; t = t.BaseType)
        {
            var f = t.GetField(n, BF);
            if (f != null) return f;
        }
        return null;
    }
    static object Get(object o, string n) => FI(o.GetType(), n)?.GetValue(o);
    static T Get<T>(object o, string n) => (T)Get(o, n);
    static void Set(object o, string n, object v) => FI(o.GetType(), n).SetValue(o, v);
    static object Call(object o, string n, params object[] a) => o.GetType().GetMethod(n, BF).Invoke(o, a);
    static string Phase => Get(game, "phase").ToString();
    static Transform PlayerRoot => Get<Transform>(game, "playerRoot");
    static string V(Vector3 v) => "(" + v.x.ToString("0.00", CultureInfo.InvariantCulture) + ", " + v.y.ToString("0.00", CultureInfo.InvariantCulture) + ", " + v.z.ToString("0.00", CultureInfo.InvariantCulture) + ")";
    static string F(double f) => f.ToString("0.00", CultureInfo.InvariantCulture);
    static string F3(double f) => f.ToString("0.000", CultureInfo.InvariantCulture);

    // ------------------------------------------------------------------ placement
    static void Place(Vector3 feet, Vector3 lookAt)
    {
        var root = PlayerRoot;
        var body = Get<CharacterController>(game, "playerBody");
        body.enabled = false;
        root.position = feet;
        body.enabled = true;
        Physics.SyncTransforms();
        Look(lookAt);
    }

    static void Look(Vector3 lookAt)
    {
        var root = PlayerRoot;
        var eye = root.position + Vector3.up * ModuleUnits.PlayerEye;
        var d = lookAt - eye;
        var yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        var pitch = -Mathf.Asin(Mathf.Clamp(d.y / Mathf.Max(1e-4f, d.magnitude), -1f, 1f)) * Mathf.Rad2Deg;
        Set(game, "yaw", yaw);
        Set(game, "pitch", pitch);
        root.rotation = Quaternion.Euler(0f, yaw, 0f);
        cam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    static void Resume()
    {
        if (Phase != "Paused") return;
        var phaseType = typeof(FrontRooms3DGame).GetNestedType("Phase", BindingFlags.NonPublic);
        Call(game, "SetPhase", Enum.Parse(phaseType, "Playing"));
    }

    static IEnumerator Settle(int extra = 10)
    {
        yield return 3;
        var start = Time.frameCount;
        while (!map.Settled && Time.frameCount - start < 1500) yield return 1;
        yield return extra;
    }

    static bool FreeStanding(Vector3 feet)
    {
        var r = ModuleUnits.PlayerRadius;
        var root = PlayerRoot;
        foreach (var c in Physics.OverlapCapsule(feet + Vector3.up * (r + .15f), feet + Vector3.up * (ModuleUnits.PlayerHeight - r), r, ~0, QueryTriggerInteraction.Ignore))
            if (root == null || !c.transform.IsChildOf(root)) return false;
        return true;
    }

    static bool SeesPane(Vector3 feet, Collider pane)
    {
        var eye = feet + Vector3.up * ModuleUnits.PlayerEye;
        var pts = new List<Vector3> { pane.bounds.center };
        var t = pane.transform;
        foreach (var sx in new[] { -.42f, .42f })
        foreach (var sy in new[] { -.42f, .42f })
            pts.Add(t.TransformPoint(new Vector3(sx, sy, 0f)));
        foreach (var p in pts)
        {
            var d = p - eye;
            if (!Physics.Raycast(eye, d.normalized, out var hit, d.magnitude + .2f, ~0, QueryTriggerInteraction.Ignore)) return false;
            if (hit.collider != pane) return false;
        }
        return true;
    }

    static Collider WindowCollider(GridCoord a, GridCoord b)
    {
        var dict = Get(map, "windowByCollider") as IDictionary;
        var p = map.CrossingPoint(a, b);
        foreach (DictionaryEntry e in dict)
        {
            var pos = (Vector3)Get(e.Value, "position");
            if ((new Vector2(pos.x - p.x, pos.z - p.z)).sqrMagnitude < .01f) return (Collider)e.Key;
        }
        return null;
    }

    static IEnumerable<GridCoord> CellsAround(GridCoord center, int radiusCells)
    {
        for (var r = 0; r <= radiusCells; r++)
        for (var dy = -r; dy <= r; dy++)
        for (var dx = -r; dx <= r; dx++)
        {
            if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
            yield return new GridCoord(center.x + dx, center.y + dy);
        }
    }

    // ------------------------------------------------------------------ the test switch: panes on Glass_Window
    static int SwapPanes()
    {
        var n = 0;
        foreach (var t in Object.FindObjectsByType<FrontRoomsMetalGlassTarget>(FindObjectsSortMode.None))
        {
            var r = t.GetComponent<Renderer>();
            if (r == null || r.sharedMaterial == glassWindow) continue;
            // G14V 2026-10-21: GD3 (live in main since 10-21) draws the glass with its own Glass_Fracture (Glass_Window +
            // _PaneFromUV, _RTReceive 1): a pane that already receives keeps its material (the swap would drop _PaneFromUV).
            if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_RTReceive") && r.sharedMaterial.GetFloat("_RTReceive") > 0.5f) continue;
            r.sharedMaterial = glassWindow;
            r.shadowCastingMode = ShadowCastingMode.Off;
            n++;
        }
        return n;
    }

    // ------------------------------------------------------------------ rendering and readback
    static void Render(RenderTexture target, Camera c = null)
    {
        c = c != null ? c : cam;
        var prev = c.targetTexture;
        c.targetTexture = target;
        c.Render();
        c.targetTexture = prev;
    }

    static void Sync(RenderTexture target)
    {
        var active = RenderTexture.active;
        RenderTexture.active = target;
        tiny.ReadPixels(new Rect(0, 0, 1, 1), 0, 0);
        tiny.Apply();
        RenderTexture.active = active;
        System.Threading.Thread.Sleep(3);   // the plugin's completion handlers publish GPU times right after the GPU finishes
    }

    static CommandBuffer timerBegin, timerEnd;
    static double RenderTimed(RenderTexture target)
    {
        if (timerBegin == null)
        {
            var f = FrontRoomsGlassRTNative.FRGlassRT_GetRenderEventFunc();
            timerBegin = new CommandBuffer { name = "FRGlassRT timer begin" };
            timerBegin.IssuePluginEventAndData(f, 2, IntPtr.Zero);
            timerEnd = new CommandBuffer { name = "FRGlassRT timer end" };
            timerEnd.IssuePluginEventAndData(f, 3, IntPtr.Zero);
        }
        Sync(target);
        Graphics.ExecuteCommandBuffer(timerBegin);
        Render(target);
        Graphics.ExecuteCommandBuffer(timerEnd);
        Sync(target);
        return FrontRoomsGlassRTNative.Get(FrontRoomsGlassRTNative.Stat.TimerFrameGpuMs);
    }

    static byte[] Read8(RenderTexture src)
    {
        var active = RenderTexture.active;
        RenderTexture.active = src;
        if (frameTex == null || frameTex.width != src.width || frameTex.height != src.height)
            frameTex = new Texture2D(src.width, src.height, TextureFormat.RGB24, false);
        frameTex.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
        frameTex.Apply();
        RenderTexture.active = active;
        return frameTex.GetRawTextureData();
    }

    static void SavePng(string file)
    {
        File.WriteAllBytes(Path.Combine(OutDir, file), frameTex.EncodeToPNG());
    }

    static void Shot(string file, string note)
    {
        Render(rt);
        Read8(rt);
        SavePng(file);
        Log("FRAME " + file + " | " + note + " | cam " + V(cam.transform.position) + " fwd " + V(cam.transform.forward));
    }

    static Color[] ReadFloat(RenderTexture src)
    {
        if (src == null) return null;
        var active = RenderTexture.active;
        RenderTexture.active = src;
        var t = new Texture2D(src.width, src.height, TextureFormat.RGBAFloat, false, true);
        t.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
        t.Apply();
        RenderTexture.active = active;
        var px = t.GetPixels();
        Object.DestroyImmediate(t);
        return px;
    }

    static RenderTexture Out => camState != null && camState.outTex != null ? camState.outTex.rt : null;
    static RenderTexture GlassDepth => camState != null && camState.depth != null ? camState.depth.rt : null;

    // Tonemapped picture of the RT radiance (x/(1+x), exposure 2) with misses in magenta and non-glass black.
    static void SaveRadiance(Color[] o, int w, int h, string file, Color[] depth = null)
    {
        if (o == null) { Log("  (no RT output to save for " + file + ")"); return; }
        var t = new Texture2D(w, h, TextureFormat.RGB24, false);
        var px = new Color32[w * h];
        for (var i = 0; i < px.Length; i++)
        {
            var c = o[i];
            if (c.a > 1f)
            {
                float T(float x) { x *= 2f; return Mathf.LinearToGammaSpace(x / (1f + x)); }
                px[i] = new Color(T(c.r), T(c.g), T(c.b), 1f);
            }
            else if (depth != null && depth[i].r > 0f) px[i] = new Color32(160, 0, 160, 255);
            else px[i] = new Color32(0, 0, 0, 255);
        }
        t.SetPixels32(px);
        t.Apply();
        File.WriteAllBytes(Path.Combine(OutDir, file), t.EncodeToPNG());
        Object.DestroyImmediate(t);
    }

    static string StatsLine()
    {
        FrontRoomsGlassRT.TryGetStats(out var s);
        return "GPU AS " + F3(s.gpuMsAccel) + " | trace " + F3(s.gpuMsTrace) + " | resolve " + F3(s.gpuMsResolve) + " | total " + F3(s.gpuMsTotal)
            + " ms | encode " + F3(s.encodeMs) + " ms | main " + F3(s.mainThreadMs) + " ms | TLAS " + s.tlasInstances + " | meshes " + s.meshes + " (pending " + s.meshesPending
            + ") | lamps " + s.lamps + " | receivers visible " + s.receiversVisible + " | glass px " + s.glassPixels + " | miss " + s.missPixels + " | layered " + s.layerPixels
            + " | AA px " + s.aaPixels + " | shadow rays " + s.shadowRays + " | accum " + s.accumulation + " | CB errors " + s.commandBufferErrors
            + " | ring busy " + s.ringBusy + " | main breakdown scene/live/lamps/records/consts/submit " + string.Join("/", FrontRoomsGlassRTSystem.Breakdown.Select(x => F3(x)))
            + " (native submit " + F3(FrontRoomsGlassRTNative.Get(FrontRoomsGlassRTNative.Stat.SubmitMs)) + ")";
    }

    // film grain off for analysis renders (every render differs otherwise); game-look frames keep it
    static readonly Dictionary<FilmGrain, bool> grainOriginal = new Dictionary<FilmGrain, bool>();
    static bool ditherOriginal = true, ditherKnown;
    /// <summary>Analysis renders: film grain AND the camera's 8-bit dithering off (both change every frame by about
    /// one level everywhere, which would read as false differences); game-look frames keep both.</summary>
    static void Grain(bool on)
    {
        var cd = cam != null ? cam.GetUniversalAdditionalCameraData() : null;
        if (cd != null)
        {
            if (!ditherKnown) { ditherOriginal = cd.dithering; ditherKnown = true; }
            cd.dithering = on && ditherOriginal;
        }
        foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var p = v.sharedProfile;
            if (p == null || !p.TryGet<FilmGrain>(out var g)) continue;
            if (!grainOriginal.ContainsKey(g)) grainOriginal[g] = g.active;
            g.active = on && grainOriginal[g];
        }
    }

    // ------------------------------------------------------------------ masks
    static int Count(bool[] m) { var n = 0; foreach (var b in m) if (b) n++; return n; }

    static bool[] Mask(Color[] px, Func<Color, bool> f)
    {
        var m = new bool[px.Length];
        for (var i = 0; i < px.Length; i++) m[i] = f(px[i]);
        return m;
    }

    static double IoU(bool[] a, bool[] b)
    {
        long i = 0, u = 0;
        for (var k = 0; k < a.Length; k++) { if (a[k] && b[k]) i++; if (a[k] || b[k]) u++; }
        return u == 0 ? 1.0 : (double)i / u;
    }

    // pixels of a that are farther than 1 px from any pixel of b (and vice versa)
    static int BeyondOnePixel(bool[] a, bool[] b, int w, int h)
    {
        var n = 0;
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var i = y * w + x;
            if (a[i] == b[i]) continue;
            var other = a[i] ? b : a;
            var near = false;
            for (var dy = -1; dy <= 1 && !near; dy++)
            for (var dx = -1; dx <= 1 && !near; dx++)
            {
                int xx = x + dx, yy = y + dy;
                if (xx < 0 || yy < 0 || xx >= w || yy >= h) continue;
                if (other[yy * w + xx]) near = true;
            }
            if (!near) n++;
        }
        return n;
    }

    static bool[] Diff8(byte[] a, byte[] b, int threshold = 1)
    {
        var m = new bool[a.Length / 3];
        for (var i = 0; i < m.Length; i++)
        {
            var d = Math.Max(Math.Abs(a[3 * i] - b[3 * i]), Math.Max(Math.Abs(a[3 * i + 1] - b[3 * i + 1]), Math.Abs(a[3 * i + 2] - b[3 * i + 2])));
            m[i] = d >= threshold;
        }
        return m;
    }

    static double Luma8(byte[] p, int i) => .2126 * p[3 * i] + .7152 * p[3 * i + 1] + .0722 * p[3 * i + 2];

    // ------------------------------------------------------------------ planar mirror reference (G11-style)
    static Camera refCam;
    static Matrix4x4 Reflection(Vector4 plane)
    {
        var m = Matrix4x4.identity;
        m.m00 = 1f - 2f * plane.x * plane.x; m.m01 = -2f * plane.x * plane.y; m.m02 = -2f * plane.x * plane.z; m.m03 = -2f * plane.w * plane.x;
        m.m10 = -2f * plane.y * plane.x; m.m11 = 1f - 2f * plane.y * plane.y; m.m12 = -2f * plane.y * plane.z; m.m13 = -2f * plane.w * plane.y;
        m.m20 = -2f * plane.z * plane.x; m.m21 = -2f * plane.z * plane.y; m.m22 = 1f - 2f * plane.z * plane.z; m.m23 = -2f * plane.w * plane.z;
        return m;
    }

    /// <summary>Renders what a perfect mirror in the pane plane shows (pane hidden). The camera sits at the mirrored eye
    /// with a proper rotation (mirrored forward and up) and an oblique near plane on the pane, so its image is the
    /// mirror image flipped left-right: readers flip it back (FlipX) to get pixel alignment with the game camera.</summary>
    /// <summary>The visible glass of a window: the window kit's 6 mm slab under the pane (main since 2026-10-03; the map
    /// then hides its 30 mm pane cube) or the pane cube itself when no slab exists.</summary>
    // G14V 2026-10-21: GD3's slab and stage meshes hang on the window root `Window {a}-{b}` (the pane cube's parent), not
    // under the pane collider: look from the root.
    static Renderer[] GlassOf(Collider pane) => pane == null ? new Renderer[0]
        : (pane.transform.parent != null && pane.transform.parent.name.StartsWith("Window ", StringComparison.Ordinal) ? pane.transform.parent : pane.transform)
            .GetComponentsInChildren<Renderer>(false).Where(r => r.enabled && FrontRoomsGlassRTSystem.IsGlassMaterial(r.sharedMaterial)).ToArray();

    /// <summary>Turns the renderer's SSAO feature on or off; returns its previous state.</summary>
    static bool SetSsao(bool on)
    {
        var rdata = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/FrontRooms_URP_Renderer.asset");
        var ssao = rdata != null ? rdata.rendererFeatures.FirstOrDefault(f => f != null && f.GetType().Name.Contains("ScreenSpaceAmbientOcclusion")) : null;
        if (ssao == null) return false;
        var was = ssao.isActive;
        ssao.SetActive(on);
        return was;
    }

    static void RenderMirror(Collider pane, Vector3 paneCenter, Vector3 normalToCamera, RenderTexture target, bool post)
    {
        if (refCam == null)
        {
            var go = new GameObject("RTVerify / planar reference");
            refCam = go.AddComponent<Camera>();
            refCam.enabled = false;
        }
        refCam.CopyFrom(cam);
        refCam.aspect = (float)target.width / target.height;   // the game camera's own matrix outside a render has the screen's aspect
        var src = cam.GetUniversalAdditionalCameraData();
        var d = refCam.GetUniversalAdditionalCameraData();
        d.renderPostProcessing = post;
        d.antialiasing = AntialiasingMode.None;
        d.dithering = src.dithering;
        d.renderShadows = true;
        var n = normalToCamera.normalized;
        var R = Reflection(new Vector4(n.x, n.y, n.z, -Vector3.Dot(n, paneCenter)));
        var t = cam.transform;
        refCam.transform.SetPositionAndRotation(R.MultiplyPoint(t.position), Quaternion.LookRotation(R.MultiplyVector(t.forward), R.MultiplyVector(t.up)));
        refCam.ResetWorldToCameraMatrix();
        refCam.ResetProjectionMatrix();
        var w2c = refCam.worldToCameraMatrix;
        var cpos = w2c.MultiplyPoint(paneCenter + n * .001f);
        var cnormal = w2c.MultiplyVector(n).normalized;
        refCam.projectionMatrix = refCam.CalculateObliqueMatrix(new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal)));
        var hide = GlassOf(pane);
        foreach (var r in hide) r.enabled = false;
        Render(target, refCam);
        foreach (var r in hide) r.enabled = true;
        refCam.ResetProjectionMatrix();
        refCam.ResetAspect();
    }

    static void FlipX(byte[] rgb, int w, int h)
    {
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w / 2; x++)
        {
            int a = 3 * (y * w + x), b = 3 * (y * w + w - 1 - x);
            for (var c = 0; c < 3; c++) { var tmp = rgb[a + c]; rgb[a + c] = rgb[b + c]; rgb[b + c] = tmp; }
        }
    }

    static void FlipX(Color[] px, int w, int h)
    {
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w / 2; x++)
        {
            int a = y * w + x, b = y * w + w - 1 - x;
            var tmp = px[a]; px[a] = px[b]; px[b] = tmp;
        }
    }

    static void SaveMirrorPng(string file)
    {
        var rgb = Read8(rt);
        FlipX(rgb, W, H);
        frameTex.LoadRawTextureData(rgb);
        frameTex.Apply();
        SavePng(file);
    }

    // ------------------------------------------------------------------ views
    struct View { public string name, note; public Vector3 feet, look; }
    struct WindowSetup { public Collider pane; public Vector3 center, dir, along, floorCross; public View[] views; public GridCoord a, b; }

    static IEnumerator FindWindow(ZoneTheme theme, int wantViews, List<WindowSetup> into)
    {
        var startDoorCell = (GridCoord)Get(game, "startDoorCell");
        var origin = startDoorCell + new GridCoord(0, 3);
        var windows = new List<CellPair>();
        foreach (var c in CellsAround(origin, 70))
        {
            if (map.InStartArea(c)) continue;
            foreach (var s in new[] { new GridCoord(1, 0), new GridCoord(0, 1) })
            {
                var n = c + s;
                if (map.InStartArea(n)) continue;
                if (map.Cache.Edge(c, n) != EdgeKind.Window) continue;
                var office = map.Cache.ZoneOf(c).theme == ZoneTheme.Office || map.Cache.ZoneOf(n).theme == ZoneTheme.Office;
                if ((theme == ZoneTheme.Office) != office) continue;
                windows.Add(new CellPair(c, n));
            }
            if (windows.Count >= 16) break;
        }
        Log("window candidates (" + theme + "): " + windows.Count);
        foreach (var wp in windows)
        {
            var a0 = wp.a; var b0 = wp.b;
            var cross = map.CrossingPoint(a0, b0);
            var d = map.CellCenter(b0) - map.CellCenter(a0); d.y = 0f; d.Normalize();
            var center = cross + Vector3.up * ((ModuleUnits.WindowSill + ModuleUnits.WindowTop) * .5f);
            Place(cross - d * 1.5f, center);
            yield return Settle(6);
            Resume();
            var col = WindowCollider(a0, b0);
            if (col == null) continue;
            foreach (var sideSign in new[] { 1f, -1f })
            {
                var dd = d * sideSign;
                var al = Vector3.Cross(Vector3.up, -dd).normalized;
                var fHead = cross - dd * 1.5f;
                if (!FreeStanding(fHead) || !SeesPane(fHead, col)) continue;
                Vector3? f50 = null, fSteep = null; var steepDeg = 0f;
                foreach (var lat in new[] { 1f, -1f })
                {
                    var c50 = cross - (dd * Mathf.Cos(50f * Mathf.Deg2Rad) + al * lat * Mathf.Sin(50f * Mathf.Deg2Rad)) * 1.5f;
                    if (f50 == null && FreeStanding(c50) && SeesPane(c50, col)) f50 = c50;
                    foreach (var deg in new[] { 70f, 68f, 65f, 62f })
                    {
                        if (fSteep != null && steepDeg >= deg) break;
                        var cs = cross - (dd * Mathf.Cos(deg * Mathf.Deg2Rad) + al * lat * Mathf.Sin(deg * Mathf.Deg2Rad)) * 1.5f;
                        if (FreeStanding(cs) && SeesPane(cs, col)) { fSteep = cs; steepDeg = deg; break; }
                    }
                }
                if (wantViews > 2 && (f50 == null || fSteep == null)) continue;
                var pc = col.bounds.center;
                var eyeH = Vector3.up * ModuleUnits.PlayerEye;
                var views = new List<View>
                {
                    new View { name = "v1_headon_level", feet = fHead, look = fHead + eyeH + dd * 3f, note = "head-on 1.5 m, level gaze" },
                    new View { name = "v2_headon_centre", feet = fHead, look = pc, note = "head-on 1.5 m, at the pane centre" },
                };
                if (f50 != null) views.Add(new View { name = "v3_angle50", feet = f50.Value, look = pc, note = "50 deg off the normal, 1.5 m" });
                if (fSteep != null) views.Add(new View { name = "v4_steep" + Mathf.RoundToInt(steepDeg), feet = fSteep.Value, look = pc, note = steepDeg + " deg off the normal, 1.5 m" });
                foreach (var dist in new[] { .55f, .6f, .7f, .8f })
                {
                    var fc = cross - dd * dist;
                    if (!FreeStanding(fc)) continue;
                    views.Add(new View { name = "v5_close" + Mathf.RoundToInt(dist * 100), feet = fc, look = pc, note = "head-on " + F(dist) + " m: the pane fills the frame (break-shot pose)" });
                    break;
                }
                into.Add(new WindowSetup { pane = col, center = pc, dir = dd, along = al, floorCross = cross, views = views.ToArray(), a = a0, b = b0 });
                Log("WINDOW " + col.name + " (" + theme + ") | centre " + V(pc) + " | views " + string.Join(", ", views.Select(v => v.name)));
                yield break;
            }
        }
    }

    static IEnumerator GoTo(View v)
    {
        Place(v.feet, v.look);
        yield return 2;
        Resume();
        Place(v.feet, v.look);
        SwapPanes();
        yield return 1;
    }

    // G14V 2026-10-21 (finding G14V-1): receivers registered while the editor still compiled the glass shader
    // asynchronously took the override prepass (bit 29, Hidden/FrontRooms/GlassRTPrepass), which draws almost nothing of
    // GD3's slab; G14 re-picks the bit only on a material swap. With -g14vRepair the harness re-picks it here, as Rematerial
    // would, and logs every repair (without the flag it only counts them).
    static int prepassRepairs;
    static void RepairPrepassBits()
    {
        var sys = typeof(FrontRoomsGlassRTSystem);
        var list = sys.GetField("insts", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null) as IList;
        var set = sys.GetMethod("SetPrepassBit", BindingFlags.NonPublic | BindingFlags.Static);
        var repair = Environment.GetCommandLineArgs().Contains("-g14vRepair");
        var n = 0;
        foreach (var o in list)
        {
            if (o == null) continue;
            var t = o.GetType();
            if (!(bool)t.GetField("alive").GetValue(o) || !(bool)t.GetField("receiver").GetValue(o)) continue;
            if ((int)t.GetField("prepassBit").GetValue(o) != FrontRoomsGlassRTSystem.OverrideBit) continue;
            var r = t.GetField("r").GetValue(o) as Renderer;
            if (r == null || r.sharedMaterial == null || r.sharedMaterial.FindPass("FRGlassRTPrepass") < 0) continue;
            if (repair) set.Invoke(null, new[] { o, (object)FrontRoomsGlassRTSystem.PrepassBit });
            n++;
        }
        if (n > 0)
        {
            prepassRepairs += n;
            Log("HARNESS G14V: " + n + " receivers on the override prepass (bit 29) although their material has FRGlassRTPrepass" + (repair ? ": re-picked bit 30" : " (not repaired: no -g14vRepair)"));
        }
    }

    static IEnumerator WaitWarm(string why)
    {
        RepairPrepassBits();
        var start = Time.frameCount;
        for (var k = 0; k < 400; k++)
        {
            FrontRoomsGlassRTSystem.Flush();
            Render(rt);
            Sync(rt);
            FrontRoomsGlassRT.TryGetStats(out var s);
            // fix round 32: and the stream-in gate open (a camera held after a teleport shows the zone cube, weight 0)
            if (FrontRoomsGlassRT.Supported && s.meshesPending == 0 && FrontRoomsGlassRTSystem.PendingRescans == 0 && !FrontRoomsGlassRTSystem.GateBusy && k > 3) break;
            yield return 1;
        }
        RepairPrepassBits();
        Log("WARM (" + why + ") after " + (Time.frameCount - start) + " frames | gate " + FrontRoomsGlassRTSystem.GateLine(camState) + " | " + FrontRoomsGlassRT.Status + " | registered " + FrontRoomsGlassRTSystem.RegisteredInstances
            + " instances in " + FrontRoomsGlassRTSystem.KnownChunks + " chunks | meshes CPU " + FrontRoomsGlassRTSystem.MeshUploadsCPU + " / GPU " + FrontRoomsGlassRTSystem.MeshUploadsGPU
            + " (failed " + FrontRoomsGlassRTSystem.MeshUploadFailures + ") | materials " + FrontRoomsGlassRTSystem.MaterialsUsed + " | textures " + FrontRoomsGlassRTSystem.TexturesUsed
            + " | lenses " + FrontRoomsGlassRTSystem.Lens + " | receivers " + FrontRoomsGlassRTSystem.Receivers + " | dynamic " + FrontRoomsGlassRTSystem.Dynamics + " | " + StatsLine());
    }

    struct Capture { public byte[] frame; public Color[] outPx; public Color[] depth; public double frameGpu; public FrontRoomsGlassRTStats stats; }

    static Capture CaptureRT(string pngName, FrontRoomsGlassRTQuality q, int renders = 3)
    {
        FrontRoomsGlassRT.Quality = q;
        var c = new Capture();
        for (var k = 0; k < renders; k++) c.frameGpu = RenderTimed(rt);
        c.frame = Read8(rt);
        if (pngName != null) SavePng(pngName);
        FrontRoomsGlassRT.TryGetStats(out c.stats);
        if (q != FrontRoomsGlassRTQuality.Off) { c.outPx = ReadFloat(Out); c.depth = ReadFloat(GlassDepth); }
        return c;
    }

    // ------------------------------------------------------------------ main
    static IEnumerator Main()
    {
        ShaderUtil.allowAsyncCompilation = false;
        tiny = new Texture2D(1, 1, TextureFormat.RGB24, false);
        foreach (var old in Directory.GetFiles(OutDir)) if (old.EndsWith(".png") || old.EndsWith(".tsv")) File.Delete(old);
        rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4, name = "RTVerify 1080p" };
        rt.Create();
        rt1440 = new RenderTexture(2560, 1440, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4, name = "RTVerify 1440p" };
        rt1440.Create();
        rtHdr = new RenderTexture(W, H, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear) { antiAliasing = 1, name = "RTVerify HDR linear" };
        rtHdr.Create();
        glassWindow = Resources.Load<Material>("Surfaces/Glass_Window");
        // fix round 32 (art #2 tuning): -frLensInterior x sets the edge filter's strength inside lit lenses for this run
        {
            var a = Environment.GetCommandLineArgs();
            for (var i = 0; i + 1 < a.Length; i++)
                if (a[i] == "-frLensInterior" && float.TryParse(a[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var li)) FrontRoomsGlassRTSystem.LensInteriorFilter = li;
            Log("lens-interior edge filter " + F(FrontRoomsGlassRTSystem.LensInteriorFilter));
        }
        Log("CAPS before play: gfx " + SystemInfo.graphicsDeviceType + " | " + SystemInfo.graphicsDeviceName + " | Unity SystemInfo.supportsRayTracing " + SystemInfo.supportsRayTracing
            + " | plugin caps 0x" + FrontRoomsGlassRTNative.FRGlassRT_QueryCaps().ToString("x") + " | " + FrontRoomsGlassRT.Status + " | Glass_Window " + (glassWindow != null));

        while ((game = Object.FindFirstObjectByType<FrontRooms3DGame>()) == null) yield return 1;
        cam = Get<Camera>(game, "cam");
        while (Time.time < 2.0f) yield return 1;
        UnityEngine.Random.InitState(4242);
        for (var tries = 0; tries < 60 && !Get<bool>(game, "mapPlay"); tries++)
        {
            Call(game, "RequestTitleStart");
            if (!Get<bool>(game, "mapPlay")) yield return 15;
        }
        if (!Get<bool>(game, "mapPlay")) { Log("FAILED: could not start a run"); yield break; }
        map = Get<FrontRoomsMapWorld>(game, "map");
        var tuning = Get<FrontRoomsHunterTuning>(game, "hunterTuning");
        tuning.releaseDelaySeconds = 1e6f;
        var t0 = Time.time;
        while (!Get<bool>(game, "startDoorOpened") && Time.time - t0 < 8f) { Resume(); yield return 1; }
        yield return 30;
        Time.captureDeltaTime = 1f / 60f;
        camState = cam.GetComponent<FrontRoomsGlassRTCamera>();
        Log("run started | seed " + Get<int>(game, "runSeed") + " | camera fov " + F(cam.fieldOfView) + " far " + F(cam.farClipPlane)
            + " | game camera opted in by FrontRoomsPostStack.ConfigureCamera: " + (camState != null) + " | MapWorld standalone=" + Get(map, "standalone")
            + " | quality level " + QualitySettings.GetQualityLevel() + " -> default RT " + FrontRoomsGlassRT.Quality);
        if (camState == null) { FrontRoomsGlassRT.OptIn(cam); camState = cam.GetComponent<FrontRoomsGlassRTCamera>(); Log("HARNESS opted the camera in (the game had not)"); }
        FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.Level0, 0f);   // the clone's MapWorld predates the zone call
        Log("zone reflection " + FrontRoomsZoneReflection.Zone + " (" + FrontRoomsZoneReflection.Mode + ") | panes swapped to Glass_Window: " + SwapPanes());
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;

        var level0 = new List<WindowSetup>();
        yield return FindWindow(ZoneTheme.Level0, 5, level0);
        if (level0.Count == 0) { Log("NO usable Level 0 window"); yield break; }
        var W0 = level0[0];
        yield return GoTo(W0.views[1]);
        yield return WaitWarm("first window");

        if (Want("views")) yield return Views(W0);
        if (Want("orient")) yield return Orientation(W0);
        if (Want("occluder")) yield return Occluder(W0);
        if (Want("relay")) yield return Relay(W0);
        if (Want("dark")) yield return DarkBeyond(W0);
        if (Want("lamps")) yield return LampSync(W0);
        if (Want("temporal")) yield return Temporal(W0);
        if (Want("cameras")) yield return Cameras(W0);
        if (Want("capacity")) yield return Capacity(W0);
        if (Want("fracture")) yield return Fracture(W0);
        if (Want("cost")) yield return Cost(W0);
        if (Want("parity")) yield return Parity(W0, "P1_parity", "parity");
        if (Want("oblique")) yield return Oblique(W0);
        if (Want("darkrelay")) yield return DarkRelay(W0);
        if (Want("moving")) yield return Moving(W0);
        if (Want("backimage")) yield return BackImage(W0);
        if (Want("hdroff")) yield return HdrOff(W0);
        if (Want("parityvariants")) yield return ParityVariants(W0, "parityvar");
        if (Want("stream")) yield return Stream(W0);
        if (Want("ablation")) yield return Ablation(W0);            // before "broken": it needs the test pane
        if (Want("broken")) yield return Broken(W0);
        if (Want("office")) yield return Office();
        if (Want("officeparity")) yield return OfficeParity();
        if (Want("officeparityvariants"))
        {
            var ow = new List<WindowSetup>();
            yield return FindWindow(ZoneTheme.Office, 2, ow);
            if (ow.Count > 0)
            {
                FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.Office, 0f);
                yield return GoTo(ow[0].views[1]);
                yield return WaitWarm("office parity variants");
                yield return ParityVariants(ow[0], "officeparityvar");
                FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.Level0, 0f);
            }
        }
        if (Want("walk")) yield return Walk(W0);                   // before any far teleport (pairs / tagproof stream other chunks in)
        if (Want("jump")) yield return Jump(W0);                   // fix round 32: stream-in after a jump (far teleports)
        if (Want("keypickup")) yield return KeyPickup(W0);         // fix round 33 (R3-B2): presented key, chunk rebuild
        if (Want("restart"))                                       // fix round 33 (R3-B1): scene reloads; later phases use the new run
        {
            var fresh = new List<WindowSetup>();
            yield return Restart(W0, fresh);
            if (fresh.Count == 0 || map == null || cam == null || camState == null) { Log("RESTART: no usable run after the reloads, stopping"); yield break; }
            W0 = fresh[0];
        }
        if (Want("pairs")) yield return Pairs();
        if (Want("tagproof")) yield return TagProof();
        if (Want("failclosed")) yield return FailClosedTest(W0);   // last: it turns RT off for the session
        if (only != null && only.Contains("shaderproof"))   // explicit only: -frVerifyOnly shaderproof
        {
            var proofWindows = new List<WindowSetup> { W0 };
            var officeWindows = new List<WindowSetup>();
            yield return FindWindow(ZoneTheme.Office, 2, officeWindows);
            if (officeWindows.Count > 0) proofWindows.Add(officeWindows[0]);
            yield return ShaderProof(proofWindows);
        }
        Log("DONE | " + FrontRoomsGlassRT.Status + " | " + StatsLine());
    }

    // ---- P0-A2/A4/A6, P1-B2/B4: the five views, Off / reference / High / Ultra
    static Material unlitA, unlitB;
    /// <summary>Raster coverage of the window's visible glass at this pose: the glass drawn twice as opaque unlit
    /// (1,0,1) and (0,1,0) with post off; the linear red difference of the two 8-bit sRGB frames is the 4x MSAA coverage
    /// fraction of each pixel. Pixel-centre coverage (what the 1-sample prepass sees) = fraction >= 0.5.</summary>
    static float[] GlassCoverage(Collider pane) => Coverage(GlassOf(pane));

    /// <summary>4x MSAA coverage fraction of these renderers at the current pose (two opaque unlit renders, post off).</summary>
    static float[] Coverage(Renderer[] glass)
    {
        if (unlitA == null)
        {
            unlitA = new Material(Shader.Find("Universal Render Pipeline/Unlit")); unlitA.SetColor("_BaseColor", new Color(1f, 0f, 1f));
            unlitB = new Material(Shader.Find("Universal Render Pipeline/Unlit")); unlitB.SetColor("_BaseColor", new Color(0f, 1f, 0f));
        }
        var mats = glass.Select(r => r.sharedMaterial).ToArray();
        var data = cam.GetUniversalAdditionalCameraData();
        var post = data.renderPostProcessing;
        data.renderPostProcessing = false;
        byte[] Pass(Material m) { foreach (var r in glass) r.sharedMaterial = m; Render(rt); Render(rt); return (byte[])Read8(rt).Clone(); }
        var a = Pass(unlitA);
        var b = Pass(unlitB);
        for (var k = 0; k < glass.Length; k++) glass[k].sharedMaterial = mats[k];
        data.renderPostProcessing = post;
        var f = new float[a.Length / 3];
        for (var i = 0; i < f.Length; i++) f[i] = Mathf.Clamp01(Mathf.GammaToLinearSpace(a[3 * i] / 255f) - Mathf.GammaToLinearSpace(b[3 * i] / 255f));
        return f;
    }

    static IEnumerator Views(WindowSetup w)
    {
        foreach (var v in w.views)
        {
            yield return GoTo(v);
            yield return WaitWarm(v.name);
            var t = cam.transform;
            var toPane = w.center - t.position;
            var inc = Vector3.Angle(-toPane.normalized, -w.dir);
            Log("VIEW " + v.name + " - " + v.note + " | eye " + V(t.position) + " | " + F(toPane.magnitude) + " m | incidence " + F(inc) + " deg");
            var off = CaptureRT(v.name + "_off.png", FrontRoomsGlassRTQuality.Off);
            RenderMirror(w.pane, w.center, -w.dir, rt, true);
            SaveMirrorPng(v.name + "_ref.png");
            var high = CaptureRT(v.name + "_high.png", FrontRoomsGlassRTQuality.High, 4);
            Log("  HIGH " + StatsLine() + " | frame GPU " + F3(high.frameGpu) + " ms");
            SaveRadiance(high.outPx, W, H, v.name + "_rt_high.png", high.depth);
            var ultra = CaptureRT(v.name + "_ultra.png", FrontRoomsGlassRTQuality.Ultra, 4);
            Log("  ULTRA " + StatsLine() + " | frame GPU " + F3(ultra.frameGpu) + " ms");
            SaveRadiance(ultra.outPx, W, H, v.name + "_rt_ultra.png", ultra.depth);
            if (v.name.StartsWith("v1") || v.name.StartsWith("v2"))
            {
                // AD-3 attribution: the reflected lens footprint without the back-surface image and the edge filter
                var keepBack = FrontRoomsGlassRTSystem.BackSurfaceImage; var keepEdge = FrontRoomsGlassRTSystem.EdgeFilterHigh;
                FrontRoomsGlassRTSystem.BackSurfaceImage = false;
                var nb = CaptureRT(null, FrontRoomsGlassRTQuality.High, 3);
                SaveRadiance(nb.outPx, W, H, v.name + "_rt_high_noback.png", nb.depth);
                FrontRoomsGlassRTSystem.EdgeFilterHigh = 0f;
                var plain = CaptureRT(null, FrontRoomsGlassRTQuality.High, 3);
                SaveRadiance(plain.outPx, W, H, v.name + "_rt_high_plain.png", plain.depth);
                FrontRoomsGlassRTSystem.BackSurfaceImage = keepBack; FrontRoomsGlassRTSystem.EdgeFilterHigh = keepEdge;
            }

            // raster coverage of the glass (exact, two opaque unlit renders) vs the prepass mask vs RT coverage; transmission (grain off)
            Grain(false);
            var offA = CaptureRT(null, FrontRoomsGlassRTQuality.Off, 2);
            FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
            FrontRoomsMetalGlassRTRendererFeature.HarnessHideResult = true;   // traced, weight 0: must equal Off bit for bit
            Render(rt); Render(rt); var hidden = (byte[])Read8(rt).Clone();
            // the same with the event barriers forced on, and a second Off frame (the noise floor of a bit-for-bit test)
            FrontRoomsGlassRTSystem.ForceBarriers = true;
            Render(rt); Render(rt); var hiddenBarrier = (byte[])Read8(rt).Clone();
            FrontRoomsGlassRTSystem.ForceBarriers = false;
            FrontRoomsMetalGlassRTRendererFeature.HarnessHideResult = false;
            var offB = CaptureRT(null, FrontRoomsGlassRTQuality.Off, 2);
            FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
            Render(rt); Render(rt); var withPane = (byte[])Read8(rt).Clone();
            var coverage = GlassCoverage(w.pane);
            Render(rt);
            Grain(true);
            var forward = coverage.Select(x => x >= .5f).ToArray();
            var inertMask = Diff8(hidden, offA.frame, 1);
            var inertDiff = Count(inertMask);
            var inertBarrier = Count(Diff8(hiddenBarrier, offA.frame, 1));
            var offNoise = Count(Diff8(offB.frame, offA.frame, 1));
            if (inertDiff > 0)
            {
                var mrgb = new byte[W * H * 3];
                for (var i = 0; i < inertMask.Length; i++) if (inertMask[i]) { mrgb[3 * i] = 255; mrgb[3 * i + 1] = 40; }
                SaveBytes(mrgb, v.name + "_inert_diff_mask.png");
            }
            var prepass = Mask(high.depth, c => c.r > 0f);
            var traced = Mask(high.outPx, c => c.a > 1f);
            var iouPF = IoU(prepass, forward);
            var iouTP = IoU(traced, prepass);
            var beyond = BeyondOnePixel(prepass, forward, W, H);
            var glassPx = Count(prepass);
            var miss = glassPx - Count(traced.Zip(prepass, (a, b) => a && b).ToArray());
            // transmission: RT on vs off inside the glass (8-bit, after post). RT replaces the glass's environment term
            // (the zone cube) with the traced room, weighted by the glass's own Fresnel: the change is Fresnel x (rt - cube),
            // so it is small and of either sign; an opaque mirror (the prototype) moved the glass by -25 levels.
            double dSum = 0; var darker = 0; var n = 0;
            var dys = new List<double>();
            for (var i = 0; i < prepass.Length; i++)
            {
                if (!prepass[i]) continue;
                var dy = Luma8(withPane, i) - Luma8(offA.frame, i);
                dSum += dy; n++; dys.Add(dy);
                if (dy < -1.0) darker++;
            }
            var meanDY = n > 0 ? dSum / n : 0;
            var p01 = dys.Count > 0 ? Pct(dys, .01) : 0; var p99 = dys.Count > 0 ? Pct(dys, .99) : 0;
            Log("  ALIGN " + v.name + ": glass px (prepass) " + glassPx + " | raster coverage px (>= 50 % of 4x MSAA) " + Count(forward) + " | IoU(prepass, raster) " + F3(iouPF)
                + " | boundary px beyond 1 px " + beyond + " | IoU(traced, prepass) " + F3(iouTP) + " | misses " + miss + " (" + F(100.0 * miss / Math.Max(1, glassPx)) + " %)"
                + " | dY RT High - Off mean " + F(meanDY) + "/255 (p1 " + F(p01) + ", p99 " + F(p99) + ") | pixels darker by >1/255: " + darker
                + " | RT traced with weight 0 vs Off: " + inertDiff + " px differ (event barriers forced on: " + inertBarrier + " px; a second Off frame vs the first: " + offNoise + " px)");
            M(v.name + ".dY_p01", p01); M(v.name + ".dY_p99", p99); M(v.name + ".inert_px", inertDiff); M(v.name + ".inert_px_barrier", inertBarrier); M(v.name + ".off_noise_px", offNoise);
            M(v.name + ".incidence_deg", inc); M(v.name + ".glass_px", glassPx); M(v.name + ".iou_prepass_forward", iouPF); M(v.name + ".boundary_beyond_1px", beyond);
            M(v.name + ".iou_traced_prepass", iouTP); M(v.name + ".miss_pct", 100.0 * miss / Math.Max(1, glassPx)); M(v.name + ".dY_mean", meanDY); M(v.name + ".darker_px", darker);
            M(v.name + ".high.gpu_trace_ms", high.stats.gpuMsTrace); M(v.name + ".high.gpu_total_ms", high.stats.gpuMsTotal); M(v.name + ".high.frame_gpu_ms", high.frameGpu);
            M(v.name + ".ultra.gpu_trace_ms", ultra.stats.gpuMsTrace); M(v.name + ".ultra.gpu_total_ms", ultra.stats.gpuMsTotal); M(v.name + ".ultra.frame_gpu_ms", ultra.frameGpu);
            M(v.name + ".off.frame_gpu_ms", off.frameGpu); M(v.name + ".high.main_ms", high.stats.mainThreadMs); M(v.name + ".high.encode_ms", high.stats.encodeMs);
            M(v.name + ".tlas", high.stats.tlasInstances); M(v.name + ".lamps", high.stats.lamps);

            // P1-B2: RT radiance vs the planar mirror (both linear HDR, post off) in 64-px blocks
            if (v.name.StartsWith("v2") || v.name.StartsWith("v3") || v.name.StartsWith("v1"))
              foreach (var ssaoOn in new[] { true, false })
            {
                // The traced hits get no SSAO (design O4); the raster mirror does. Both are measured: with the game's
                // SSAO (what the player compares against) and with SSAO off in the mirror too (hit-shading accuracy).
                var keepSsao = SetSsao(ssaoOn);
                RenderMirror(w.pane, w.center, -w.dir, rtHdr, false);
                SetSsao(keepSsao);
                var mirror = ReadFloat(rtHdr);
                FlipX(mirror, W, H);
                int blocks = 0, good = 0;
                double sumRatio = 0;
                var ratios = new List<double>();
                for (var by = 0; by + 64 <= H; by += 64)
                for (var bx = 0; bx + 64 <= W; bx += 64)
                {
                    double lr = 0, lm = 0; var cnt = 0;
                    for (var y = by; y < by + 64; y++)
                    for (var x = bx; x < bx + 64; x++)
                    {
                        var i = y * W + x;
                        var o = high.outPx[i];
                        if (o.a <= 1f) continue;
                        lr += .2126 * o.r + .7152 * o.g + .0722 * o.b;
                        var m = mirror[i];
                        lm += .2126 * m.r + .7152 * m.g + .0722 * m.b;
                        cnt++;
                    }
                    if (cnt < 64 * 64 * .9) continue;
                    blocks++;
                    var ratio = lr / Math.Max(lm, 1e-6);
                    sumRatio += ratio;
                    ratios.Add(ratio);
                    if (ratio >= .9 && ratio <= 1.1) good++;
                }
                var tagM = ssaoOn ? "" : ".ssao_off";
                Log("  MIRROR REFERENCE " + v.name + (ssaoOn ? " (mirror with the game's SSAO)" : " (SSAO off in the mirror too)") + ": full 64-px blocks in the glass " + blocks + " | luminance ratio RT/mirror within 0.9-1.1 in " + good
                    + " (" + F(blocks > 0 ? 100.0 * good / blocks : 0) + " %) | mean ratio " + F3(blocks > 0 ? sumRatio / blocks : 0)
                    + " | median " + F3(ratios.Count > 0 ? ratios.OrderBy(x => x).ElementAt(ratios.Count / 2) : 0) + " | within 0.8-1.25: " + ratios.Count(x => x >= .8 && x <= 1.25));
                if (ratios.Count > 0) M(v.name + tagM + ".mirror_median_ratio", ratios.OrderBy(x => x).ElementAt(ratios.Count / 2));
                if (ssaoOn) SaveRadiance(mirror.Select((c, i) => high.outPx[i].a > 1f ? new Color(c.r, c.g, c.b, 2f) : new Color(0, 0, 0, 0)).ToArray(), W, H, v.name + "_mirror_radiance.png");
                M(v.name + tagM + ".mirror_blocks", blocks); M(v.name + tagM + ".mirror_good_pct", blocks > 0 ? 100.0 * good / blocks : 0); M(v.name + tagM + ".mirror_mean_ratio", blocks > 0 ? sumRatio / blocks : 0);
                Read8(rt);
            }
        }
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
    }

    // ---- F5 (codex audit): the merged glass shader (main's verified 3e4bdc16 + G14 G-1/G-2/G-3) must render exactly
    // like main's shader whenever the RT keyword is off. Hidden/FrontRooms/GlassMainBaseline (Assets/Editor/RT/Proof,
    // clone only) is main's file byte for byte with only the shader name changed. RT is Off (the feature never enables
    // _FR_GLASS_RT), post off, ARGBFloat 4x MSAA target, URP HDR buffer 64 bit, time frozen, exact bit comparison.
    // Every FrontRooms/Glass renderer in the built map (window slabs on Glass_Window, Prop_Glass, Prop_BottleBlue) is
    // switched between the two shaders. Checks per view: determinism (baseline twice); (i) merged vs baseline, globals
    // as the RT pass leaves them after its end pass (weight 0); (ii) weight 0 + a 1x1 red RT texture with A = 2;
    // (iii) weight 1 + an A = 0 texture; (iv) weight 1 + RT (2,0,0,2): all with the keyword off, all must be identical.
    // Info only: keyword ON + weight 0 (the hook's code is compiled in, so the last bits may differ, as the glass track found).
    static readonly int ProofRTTex = Shader.PropertyToID("_FR_GlassRTReflection"), ProofRTWeight = Shader.PropertyToID("_FR_GlassRTWeight");
    static RenderTexture proofRt;
    static Texture2D proofRead;

    static float[] ProofRender()
    {
        if (proofRt == null)
        {
            var desc = new RenderTextureDescriptor(W, H, RenderTextureFormat.ARGBFloat, 24) { msaaSamples = 4, sRGB = false };
            proofRt = new RenderTexture(desc) { name = "RTVerify proof (ARGBFloat)" };
            proofRt.Create();
            proofRead = new Texture2D(W, H, TextureFormat.RGBAFloat, false, true);
        }
        Render(proofRt); Render(proofRt);
        var active = RenderTexture.active;
        RenderTexture.active = proofRt;
        proofRead.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        proofRead.Apply(false);
        RenderTexture.active = active;
        return proofRead.GetRawTextureData<float>().ToArray();
    }

    static PxDiff BitDiff(float[] a, float[] b)
    {
        long px = 0; double max = 0;
        for (int p = 0, i = 0; p < W * H; p++, i += 4)
        {
            var d = false;
            for (var c = 0; c < 4; c++)
            {
                if (BitConverter.SingleToInt32Bits(a[i + c]) == BitConverter.SingleToInt32Bits(b[i + c])) continue;
                d = true;
                var e = Math.Abs((double)a[i + c] - b[i + c]);
                if (!double.IsNaN(e) && e > max) max = e;
            }
            if (d) px++;
        }
        return new PxDiff { px = px, max = max };
    }

    static IEnumerator ShaderProof(List<WindowSetup> windows)
    {
        var baseline = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Editor/RT/Proof/FrontRoomsGlassMainBaseline.shader");
        var merged = Shader.Find("FrontRooms/Glass");
        if (baseline == null || merged == null || ShaderUtil.ShaderHasError(baseline) || ShaderUtil.ShaderHasError(merged))
        { Log("SHADER PROOF: shader missing or has errors (baseline " + (baseline != null) + ", merged " + (merged != null) + ")"); yield break; }
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.Off;
        Shader.DisableKeyword("_FR_GLASS_RT");
        var urp = UniversalRenderPipeline.asset;
        var hdrWas = urp.hdrColorBufferPrecision;
        urp.hdrColorBufferPrecision = HDRColorBufferPrecision._64Bits;
        var data = cam.GetUniversalAdditionalCameraData();
        var postWas = data.renderPostProcessing;
        data.renderPostProcessing = false;
        Time.captureDeltaTime = 1e-6f;   // lamp clocks frozen: both shaders see the same lamp state
        var red = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true); red.SetPixelData(new[] { 2f, 0f, 0f, 2f }, 0); red.Apply(false);
        var alpha0 = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true); alpha0.SetPixelData(new[] { 2f, 0f, 0f, 0f }, 0); alpha0.Apply(false);
        var copies = new Dictionary<Material, Material>();
        var renderers = new List<RendererMats>();
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            var mats = r.sharedMaterials;
            if (!mats.Any(m => m != null && m.shader == merged)) continue;
            renderers.Add(new RendererMats { r = r, original = mats });
            foreach (var m in mats)
                if (m != null && m.shader == merged && !copies.ContainsKey(m))
                {
                    var kw = m.shaderKeywords;
                    var c = new Material(m) { name = m.name + " (main baseline)" };
                    c.shader = baseline; c.shaderKeywords = kw; c.renderQueue = m.renderQueue;
                    copies[m] = c;
                }
        }
        void UseBaseline(bool b)
        {
            foreach (var rm in renderers)
            {
                var r = rm.r; var original = rm.original;
                if (r == null) continue;
                r.sharedMaterials = b ? original.Select(m => m != null && copies.TryGetValue(m, out var c) ? c : m).ToArray() : original;
            }
        }
        Log("SHADER PROOF: baseline " + baseline.name + " (main's FrontRooms/Glass, md5 3e4bdc16, renamed) vs the merged FrontRooms/Glass | " + renderers.Count
            + " renderers on FrontRooms/Glass in the scene (" + string.Join(", ", copies.Keys.Select(m => m.name)) + ")");
        int checks = 0, fails = 0;
        var info = new StringBuilder();
        void Check(string id, string what, PxDiff d)
        {
            checks++;
            var ok = d.px == 0;
            if (!ok) fails++;
            M("proof." + id + "." + what.Split(' ')[0], d.px);
            Log("  PROOF " + id + " " + what + ": " + (ok ? "IDENTICAL" : "DIFFERS in " + d.px + " px, max |d| " + d.max.ToString("G4", CultureInfo.InvariantCulture)));
        }
        var poses = new List<PoseId>();
        foreach (var w in windows) foreach (var v in w.views) poses.Add(new PoseId { id = (w.a.ToString() + "_" + v.name).Replace(" ", ""), v = v });
        foreach (var pose in poses)
        {
            var id = pose.id; var v = pose.v;
            yield return GoTo(v);
            yield return 2;
            Shader.DisableKeyword("_FR_GLASS_RT");
            Shader.SetGlobalFloat(ProofRTWeight, 0f);
            UseBaseline(true); var b1 = ProofRender();
            UseBaseline(false); var m1 = ProofRender();
            UseBaseline(true); var b2 = ProofRender();
            Check(id, "determinism (baseline twice)", BitDiff(b1, b2));
            Check(id, "(i) merged vs main, keyword off, weight 0", BitDiff(b1, m1));
            Shader.SetGlobalTexture(ProofRTTex, red); Shader.SetGlobalFloat(ProofRTWeight, 0f);
            UseBaseline(false); var m2 = ProofRender();
            Check(id, "(ii) keyword off, weight 0, RT 1x1 red A=2", BitDiff(b1, m2));
            Shader.SetGlobalTexture(ProofRTTex, alpha0); Shader.SetGlobalFloat(ProofRTWeight, 1f);
            var m3 = ProofRender();
            Check(id, "(iii) keyword off, weight 1, RT A=0", BitDiff(b1, m3));
            Shader.SetGlobalTexture(ProofRTTex, red); Shader.SetGlobalFloat(ProofRTWeight, 1f);
            var m4 = ProofRender();
            Check(id, "(iv) keyword off, weight 1, RT (2,0,0) A=2", BitDiff(b1, m4));
            Shader.EnableKeyword("_FR_GLASS_RT"); Shader.SetGlobalFloat(ProofRTWeight, 0f);
            var k0 = ProofRender();
            var dk = BitDiff(b1, k0);
            Shader.DisableKeyword("_FR_GLASS_RT");
            Log("  PROOF " + id + " info: keyword ON, weight 0 vs main: " + dk.px + " px differ, max |d| " + dk.max.ToString("G4", CultureInfo.InvariantCulture));
            M("proof." + id + ".info_keyword_on_weight0_px", dk.px);
        }
        UseBaseline(false);
        Shader.SetGlobalFloat(ProofRTWeight, 0f);
        urp.hdrColorBufferPrecision = hdrWas;
        data.renderPostProcessing = postWas;
        Time.captureDeltaTime = 1f / 60f;
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
        Log("SHADER PROOF summary: " + checks + " checks, " + fails + " failed (" + poses.Count + " views x 5)");
        M("proof.checks", checks); M("proof.failed", fails);
    }

    // ---- P0-A3: a red cube behind-left and a green cube behind-right of the camera, RT vs the planar mirror
    static GameObject Box(string name, Vector3 at, Vector3 size, Color c, bool register)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.position = at;
        go.transform.localScale = size;
        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = c };
        m.SetColor("_BaseColor", c);
        go.GetComponent<Renderer>().sharedMaterial = m;
        go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        if (register) go.AddComponent<RegisteredMarker>().handle = FrontRoomsGlassRT.Register(go.GetComponent<Renderer>(), FrontRoomsGlassRTFlags.Dynamic);
        return go;
    }

    sealed class RegisteredMarker : MonoBehaviour { public int handle = -1; void OnDestroy() { FrontRoomsGlassRT.Unregister(handle); } }

    static Centre Centroid(byte[] img, bool[] within, Func<int, int, int, bool> pick)
    {
        double sx = 0, sy = 0; var n = 0;
        for (var i = 0; i < within.Length; i++)
        {
            if (!within[i]) continue;
            if (!pick(img[3 * i], img[3 * i + 1], img[3 * i + 2])) continue;
            sx += i % W; sy += i / W; n++;
        }
        return n > 0 ? new Centre { x = sx / n, y = sy / n, n = n } : new Centre();
    }

    static IEnumerator Orientation(WindowSetup w)
    {
        var v = w.views[1];
        yield return GoTo(v);
        var eye = cam.transform.position;
        var back = -w.dir;
        var red = Box("RTVerify red (behind-left)", eye + back * 1.2f - w.along * .9f + Vector3.up * .1f, Vector3.one * .5f, Color.red, true);
        var green = Box("RTVerify green (behind-right)", eye + back * 1.2f + w.along * .9f - Vector3.up * .3f, Vector3.one * .5f, Color.green, true);
        yield return 1;
        var c = CaptureRT("P0_orientation_rt.png", FrontRoomsGlassRTQuality.High, 3);
        if (c.outPx == null || c.depth == null) { Log("ORIENTATION: no RT output (" + FrontRoomsGlassRT.Status + ")"); Object.Destroy(red); Object.Destroy(green); yield break; }
        SaveRadiance(c.outPx, W, H, "P0_orientation_rt_raw.png", c.depth);
        var rtImg = Read8RadianceAsBytes(c.outPx);
        var glass = Mask(c.depth, x => x.r > 0f);
        RenderMirror(w.pane, w.center, -w.dir, rtHdr, false);
        var mirror = ReadFloat(rtHdr);
        FlipX(mirror, W, H);
        var mirImg = Read8RadianceAsBytes(mirror, glass);
        var rR = Centroid(rtImg, glass, (r, g, b) => r > 120 && g < 60 && b < 60);
        var rM = Centroid(mirImg, glass, (r, g, b) => r > 120 && g < 60 && b < 60);
        var gR = Centroid(rtImg, glass, (r, g, b) => g > 120 && r < 60 && b < 60);
        var gM = Centroid(mirImg, glass, (r, g, b) => g > 120 && r < 60 && b < 60);
        Log("ORIENTATION: red centroid RT (" + F(rR.x) + ", " + F(rR.y) + ") n " + rR.n + " vs mirror (" + F(rM.x) + ", " + F(rM.y) + ") n " + rM.n
            + " | green RT (" + F(gR.x) + ", " + F(gR.y) + ") n " + gR.n + " vs mirror (" + F(gM.x) + ", " + F(gM.y) + ") n " + gM.n
            + " | red is " + (rR.x < gR.x ? "LEFT" : "RIGHT") + " of green in RT, " + (rM.x < gM.x ? "LEFT" : "RIGHT") + " in the mirror; red is " + (rR.y < gR.y ? "BELOW" : "ABOVE") + " green in RT, " + (rM.y < gM.y ? "BELOW" : "ABOVE") + " in the mirror");
        M("orient.red_dx", rR.x - rM.x); M("orient.red_dy", rR.y - rM.y); M("orient.green_dx", gR.x - gM.x); M("orient.green_dy", gR.y - gM.y);
        SaveBytes(mirImg, "P0_orientation_mirror_raw.png");
        Object.Destroy(red); Object.Destroy(green);
        yield return 2;
    }

    static byte[] Read8RadianceAsBytes(Color[] px, bool[] within = null)
    {
        var b = new byte[px.Length * 3];
        for (var i = 0; i < px.Length; i++)
        {
            if (within != null ? !within[i] : px[i].a <= 1f) continue;
            float T(float x) { x *= 2f; return Mathf.LinearToGammaSpace(x / (1f + x)); }
            b[3 * i] = (byte)Mathf.Clamp(T(px[i].r) * 255f, 0, 255);
            b[3 * i + 1] = (byte)Mathf.Clamp(T(px[i].g) * 255f, 0, 255);
            b[3 * i + 2] = (byte)Mathf.Clamp(T(px[i].b) * 255f, 0, 255);
        }
        return b;
    }

    static void SaveBytes(byte[] rgb, string file)
    {
        var t = new Texture2D(W, H, TextureFormat.RGB24, false);
        t.LoadRawTextureData(rgb);
        t.Apply();
        File.WriteAllBytes(Path.Combine(OutDir, file), t.EncodeToPNG());
        Object.DestroyImmediate(t);
    }

    // ---- P0-A5: an UNREGISTERED panel in front of the pane: 0 traced px on it (prepass depth test)
    static IEnumerator Occluder(WindowSetup w)
    {
        var v = w.views[1];
        yield return GoTo(v);
        var eye = cam.transform.position;
        var to = (w.center - eye);
        var panel = Box("RTVerify unregistered panel", eye + to * .55f - w.along * .25f, new Vector3(.5f, .9f, .5f), new Color(.3f, .2f, .1f), false);
        panel.transform.rotation = Quaternion.LookRotation(w.dir);
        panel.transform.localScale = new Vector3(.45f, .9f, .02f);
        yield return 1;
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
        var panelPx = Coverage(new[] { panel.GetComponent<Renderer>() }).Select(x => x >= .5f).ToArray();   // exact raster coverage
        var c = CaptureRT("P0_occluder.png", FrontRoomsGlassRTQuality.High, 2);
        var traced = Mask(c.outPx, x => x.a > 1f);
        var painted = 0; var panelCount = 0;
        for (var i = 0; i < panelPx.Length; i++) { if (!panelPx[i]) continue; panelCount++; if (traced[i]) painted++; }
        // allow the 1 px dilation ring at the panel silhouette: count only panel pixels with a 3x3 panel neighbourhood
        var interior = 0;
        for (var y = 1; y < H - 1; y++)
        for (var x = 1; x < W - 1; x++)
        {
            var i = y * W + x;
            if (!panelPx[i] || !traced[i]) continue;
            var all = true;
            for (var dy = -1; dy <= 1 && all; dy++) for (var dx = -1; dx <= 1 && all; dx++) all = panelPx[(y + dy) * W + x + dx];
            if (all) interior++;
        }
        var pre = Mask(c.depth, x => x.r > 0f);
        var prepassOnPanel = 0;
        for (var i = 0; i < panelPx.Length; i++) if (panelPx[i] && pre[i]) prepassOnPanel++;
        Log("OCCLUDER: panel px " + panelCount + " | glass prepass px on the panel " + prepassOnPanel + " | traced px on the panel " + painted + " (interior, excluding the 1 px edge ring: " + interior + ") | prototype 02: 137,551");
        M("occluder.prepass_on_panel", prepassOnPanel);
        M("occluder.panel_px", panelCount); M("occluder.traced_on_panel", painted); M("occluder.interior", interior);
        Object.Destroy(panel);
        yield return 2;
    }

    // ---- P0-A5 Relay: a static copy of the Relay body registered as Dynamic | Relay: moved 0.7 m = correct the same frame
    static IEnumerator Relay(WindowSetup w)
    {
        var v = w.views[1];
        yield return GoTo(v);
        var hunter = Get<Transform>(game, "hunter");
        if (hunter == null) { Log("RELAY: no hunter"); yield break; }
        var body = new GameObject("RTVerify Relay body copy");
        foreach (var r in hunter.GetComponentsInChildren<MeshRenderer>(true))
        {
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;
            var activeUp = true;
            for (var tr = r.transform; tr != null && tr != hunter; tr = tr.parent) if (!tr.gameObject.activeSelf) { activeUp = false; break; }
            if (!activeUp || !r.enabled) continue;
            var part = new GameObject(r.name);
            part.transform.SetPositionAndRotation(hunter.InverseTransformPoint(r.transform.position), Quaternion.Inverse(hunter.rotation) * r.transform.rotation);
            part.transform.localScale = r.transform.lossyScale;
            part.transform.SetParent(body.transform, false);
            part.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            var pr = part.AddComponent<MeshRenderer>();
            pr.sharedMaterials = r.sharedMaterials;
            pr.shadowCastingMode = ShadowCastingMode.Off;
        }
        var handles = new List<int>();
        foreach (var r in body.GetComponentsInChildren<MeshRenderer>()) handles.Add(FrontRoomsGlassRT.Register(r, FrontRoomsGlassRTFlags.Dynamic | FrontRoomsGlassRTFlags.Relay));
        Vector3 Face(Vector3 at, Vector3 toward) { var d = toward - at; d.y = 0f; return d.sqrMagnitude > 1e-4f ? d.normalized : Vector3.forward; }
        var behind = v.feet - w.dir * 2.0f;
        body.transform.SetPositionAndRotation(new Vector3(behind.x, v.feet.y, behind.z), Quaternion.LookRotation(Face(behind, v.feet)));
        yield return 2;
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
        var c1 = CaptureRT("P1_relay_behind.png", FrontRoomsGlassRTQuality.High, 2);
        SaveRadiance(c1.outPx, W, H, "P1_relay_behind_rt.png", c1.depth);
        var u1 = CaptureRT("P1_relay_behind_ultra.png", FrontRoomsGlassRTQuality.Ultra, 2);
        // move 0.7 m and render ONCE: same-frame correctness = identical to a fresh registration at the new pose
        body.transform.position += w.along * .7f;
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
        var moved = CaptureRT("P0_relay_moving.png", FrontRoomsGlassRTQuality.High, 1);
        foreach (var h in handles) FrontRoomsGlassRT.Unregister(h);
        handles.Clear();
        foreach (var r in body.GetComponentsInChildren<MeshRenderer>()) handles.Add(FrontRoomsGlassRT.Register(r, FrontRoomsGlassRTFlags.Dynamic | FrontRoomsGlassRTFlags.Relay));
        var fresh = CaptureRT(null, FrontRoomsGlassRTQuality.High, 1);
        var differ = 0;
        for (var i = 0; i < moved.outPx.Length; i++)
        {
            var a = moved.outPx[i]; var b = fresh.outPx[i];
            if (Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) > 1e-3f) differ++;
        }
        var changed = 0;
        for (var i = 0; i < moved.outPx.Length; i++)
        {
            var a = moved.outPx[i]; var b = c1.outPx[i];
            if (a.a > 1f && Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) > .02f) changed++;
        }
        Log("RELAY: body copy " + handles.Count + " parts registered via FrontRoomsGlassRT.Register | moved 0.7 m: " + changed + " reflection px changed in the same frame | px differing from a fresh registration at the new pose: " + differ + " (prototype 02: 177,035 stale px)");
        M("relay.changed_px", changed); M("relay.stale_px", differ);
        foreach (var h in handles) FrontRoomsGlassRT.Unregister(h);
        Object.Destroy(body);
        yield return 2;
    }

    // ---- P0-A5: a broken pane leaves the trace on the break frame
    static int ShownPx(Capture c) => camState.lastWeight > 0f && c.outPx != null ? Count(Mask(c.outPx, x => x.a > 1f)) : 0;

    static IEnumerator Broken(WindowSetup w)
    {
        var v = w.views[1];
        yield return GoTo(v);
        var c0 = CaptureRT("P0_broken_pane_before.png", FrontRoomsGlassRTQuality.High, 2);
        var before = ShownPx(c0);
        var receiversBefore = FrontRoomsGlassRTSystem.Receivers;
        var pane = w.pane;
        float progress = 0f; var guard = 0;
        while (pane != null && guard++ < 400)
        {
            map.Hold(pane, 1f / 30f, out progress);
            if (progress >= 1f) break;
        }
        // the break frame: MapWorld raised GlassBroken and called Destroy (deferred to the end of the frame)
        var c1 = CaptureRT("P0_broken_pane.png", FrontRoomsGlassRTQuality.High, 1);
        var after = ShownPx(c1);
        Log("BROKEN PANE: shown RT px (weight > 0, coverage) before " + before + " | on the break frame " + after + " (weight " + F(camState.lastWeight) + ", visible receivers " + camState.lastVisibleReceivers
            + ") | receivers registered " + receiversBefore + " -> " + FrontRoomsGlassRTSystem.Receivers + " | hold progress " + F(progress) + " (prototype 02: 159,533 px until the next rescan)");
        M("broken.before_px", before); M("broken.after_px", after);
        yield return 3;
        var c2 = CaptureRT(null, FrontRoomsGlassRTQuality.High, 1);
        Log("BROKEN PANE: 3 frames later shown RT px " + ShownPx(c2) + " | pane object " + (w.pane == null ? "destroyed" : "alive"));
    }

    // ---- P0-A5: a runtime mesh behind the camera: registered, edited (MeshChanged), destroyed
    static IEnumerator Stream(WindowSetup w)
    {
        var v = w.views[1];
        yield return GoTo(v);
        var eye = cam.transform.position;
        var go = new GameObject("RTVerify streamed mesh");
        var mesh = new Mesh { name = "RTVerify streamed" };
        var s = .6f;
        mesh.vertices = new[] { new Vector3(-s, -s, 0), new Vector3(s, -s, 0), new Vector3(s, s, 0), new Vector3(-s, s, 0) };
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2, 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        var red = new Material(Shader.Find("Universal Render Pipeline/Unlit")); red.SetColor("_BaseColor", Color.red);
        mr.sharedMaterial = red;
        go.transform.position = eye - w.dir * 1.0f;
        go.transform.rotation = Quaternion.LookRotation(w.dir);
        int Reds(Capture c)
        {
            var n = 0;
            for (var i = 0; i < c.outPx.Length; i++) { var p = c.outPx[i]; if (p.a > 1f && p.r > .5f && p.g < .1f && p.b < .1f) n++; }
            return n;
        }
        mr.enabled = false;
        var baseline = CaptureRT(null, FrontRoomsGlassRTQuality.High, 1);   // red things already in the reflection (EGRESS signs)
        mr.enabled = true;
        var h = FrontRoomsGlassRT.Register(mr, FrontRoomsGlassRTFlags.None);
        yield return 1;
        var a = CaptureRT(null, FrontRoomsGlassRTQuality.High, 1);
        var v2 = mesh.vertices; for (var k = 0; k < v2.Length; k++) v2[k] += new Vector3(.5f, 0f, 0f); mesh.vertices = v2; mesh.RecalculateBounds();
        FrontRoomsGlassRT.MeshChanged(mesh);
        var b = CaptureRT(null, FrontRoomsGlassRTQuality.High, 1);
        double Cx(Capture c) { double sx = 0; var n = 0; for (var i = 0; i < c.outPx.Length; i++) { var p = c.outPx[i]; if (p.a > 1f && p.r > .5f && p.g < .1f && p.b < .1f) { sx += i % W; n++; } } return n > 0 ? sx / n : -1; }
        Object.Destroy(go);
        Object.Destroy(mesh);
        FrontRoomsGlassRT.Unregister(h);
        yield return 1;
        var c3 = CaptureRT("P0_stream_test.png", FrontRoomsGlassRTQuality.High, 1);
        Log("STREAM: registered red px " + Reds(a) + " (centroid x " + F(Cx(a)) + ") | after a vertex edit + MeshChanged, same frame: " + Reds(b) + " px (centroid x " + F(Cx(b)) + ", moved " + F(Cx(b) - Cx(a)) + " px)"
            + " | next frame after Destroy: " + Reds(c3) + " px (red px in the reflection before the mesh existed: " + Reds(baseline) + "; prototype 02: 17,262 px of a destroyed mesh)");
        M("stream.baseline_red_px", Reds(baseline));
        M("stream.registered_px", Reds(a)); M("stream.changed_centroid_dx", Cx(b) - Cx(a)); M("stream.destroyed_px", Reds(c3));
    }

    // ---- P1-B4 dark beyond: a black box seen through the pane (one-way mirror)
    static IEnumerator DarkBeyond(WindowSetup w)
    {
        // fix round 34 (art review 4 #9): also v5 (0.55 m, the break-shot pose), where AD-2's straight-edged step sits in the
        // radiance: in a dark-beyond window the reflection is the whole picture, so the step is checked in the final frame
        foreach (var vi in new[] { 1, 2, 4 })
        {
            if (vi >= w.views.Length) continue;
            var v = w.views[vi];
            yield return GoTo(v);
            if (vi == 4) yield return WaitWarm("dark beyond " + v.name);
            var box = Box("RTVerify dark room", w.center + w.dir * 1.6f, new Vector3(6f, 4f, 6f), Color.black, false);
            box.transform.rotation = Quaternion.LookRotation(w.dir);
            // only the far side: the box starts 0.1 m beyond the pane
            box.transform.position = w.center + w.dir * 3.1f;
            yield return 1;
            CaptureRT("P1_dark_beyond_" + v.name + "_off.png", FrontRoomsGlassRTQuality.Off, 2);
            var h = CaptureRT("P1_dark_beyond_" + v.name + "_high.png", FrontRoomsGlassRTQuality.High, vi == 4 ? 4 : 2);
            if (vi == 4)
            {
                SaveRadiance(h.outPx, W, H, "P1_dark_beyond_" + v.name + "_rt_high.png", h.depth);
                Log("DARK BEYOND " + v.name + " High: " + StatsLine());
            }
            var u = CaptureRT("P1_dark_beyond_" + v.name + "_ultra.png", FrontRoomsGlassRTQuality.Ultra, vi == 4 ? 4 : 2);
            if (vi == 4)
            {
                SaveRadiance(u.outPx, W, H, "P1_dark_beyond_" + v.name + "_rt_ultra.png", u.depth);
                // analysis frames for the step measure: film grain and dithering off (they move every px by about one level)
                Grain(false);
                CaptureRT("P1_dark_beyond_" + v.name + "_an_off.png", FrontRoomsGlassRTQuality.Off, 2);
                CaptureRT("P1_dark_beyond_" + v.name + "_an_high.png", FrontRoomsGlassRTQuality.High, 4);
                CaptureRT("P1_dark_beyond_" + v.name + "_an_ultra.png", FrontRoomsGlassRTQuality.Ultra, 4);
                RenderMirror(w.pane, w.center, -w.dir, rt, true);
                SaveMirrorPng("P1_dark_beyond_" + v.name + "_ref.png");
                Grain(true);
                FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
            }
            Log("DARK BEYOND " + v.name + ": " + StatsLine());
            Object.Destroy(box);
            yield return 1;
        }
    }

    // ---- P1-B3: lens reflections vs the real lenses, 120 frames. The hit-id debug frame tells which TLAS instance
    // (renderer) each glass pixel's reflection ray hits; for every lens hit by >= 40 pixels the mean traced radiance
    // over its pixels is compared per frame with the lens's own MaterialPropertyBlock emission (what the raster draws).
    static IEnumerator LampSync(WindowSetup w)
    {
        var v = w.views[1];
        yield return GoTo(v);
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
        FrontRoomsGlassRTSystem.AllowAccumulation = false;
        FrontRoomsGlassRTSystem.DebugMode = FrontRoomsGlassRTNative.DebugHitId;
        Render(rt); Render(rt); Sync(rt);
        var ids = ReadFloat(camState.ids != null ? camState.ids.rt : null);
        var order = FrontRoomsGlassRTSystem.SubmittedRenderers.ToList();
        FrontRoomsGlassRTSystem.DebugMode = FrontRoomsGlassRTNative.DebugNone;
        if (ids == null) { Log("LAMPS: no id target"); FrontRoomsGlassRTSystem.AllowAccumulation = true; yield break; }
        var pixelsOf = new Dictionary<int, List<int>>();
        for (var i = 0; i < ids.Length; i++)
        {
            var inst = (int)ids[i].r - 1;
            if (inst < 0 || inst >= order.Count || order[inst] == null || order[inst].name != "Lens") continue;
            if (!pixelsOf.TryGetValue(inst, out var l)) pixelsOf[inst] = l = new List<int>();
            l.Add(i);
        }
        var lenses = pixelsOf.Where(kv => kv.Value.Count >= 40).Select(kv => new LensPx { renderer = order[kv.Key], px = kv.Value }).ToList();
        Log("LAMPS: " + lenses.Count + " lenses seen in the reflection with >= 40 px (instances in the id frame " + order.Count + ")");
        if (lenses.Count == 0) { FrontRoomsGlassRTSystem.AllowAccumulation = true; yield break; }
        var block = new MaterialPropertyBlock();
        var real = lenses.Select(_ => new List<double>()).ToList();
        var refl = lenses.Select(_ => new List<double>()).ToList();
        Time.captureDeltaTime = 1f / 60f;
        for (var f = 0; f < 120; f++)
        {
            yield return 1;
            Render(rt); Sync(rt);
            var o = ReadFloat(Out);
            for (var k = 0; k < lenses.Count; k++)
            {
                double s = 0; var n = 0;
                foreach (var i in lenses[k].px) { var c = o[i]; if (c.a <= 1f) continue; s += .2126 * c.r + .7152 * c.g + .0722 * c.b; n++; }
                refl[k].Add(n > 0 ? s / n : 0);
                lenses[k].renderer.GetPropertyBlock(block);
                var e = block.GetColor("_EmissionColor").linear;
                real[k].Add(.2126 * e.r + .7152 * e.g + .0722 * e.b);
            }
        }
        var best = -1; var bestRange = 0.0;
        var sb = new StringBuilder();
        // A steady lamp's lens moves by < 1 % (sensor noise of the glow model): its correlation is meaningless. Only
        // lenses whose glow changes by >= 10 % of its mean over the 120 frames are judged; lag 0 vs +-1 frame shows
        // whether the reflection follows on the same frame.
        var judged = new List<double>(); var lagSb = new StringBuilder();
        for (var k = 0; k < lenses.Count; k++)
        {
            var mean = real[k].Average();
            if (mean <= 1e-4 || (real[k].Max() - real[k].Min()) < .1 * mean) continue;
            var r0 = Correlation(real[k], refl[k]);
            var rPrev = Correlation(real[k].Take(real[k].Count - 1).ToList(), refl[k].Skip(1).ToList());   // reflection one frame late
            var rNext = Correlation(real[k].Skip(1).ToList(), refl[k].Take(refl[k].Count - 1).ToList());   // reflection one frame early
            judged.Add(r0);
            lagSb.Append(" | " + lenses[k].renderer.transform.parent.name + ": r(same frame) " + F3(r0) + ", r(reflection 1 frame late) " + F3(rPrev) + ", r(1 frame early) " + F3(rNext));
        }
        Log("LAMPS changing by >= 10 %: " + judged.Count + " of " + lenses.Count + lagSb + (judged.Count > 0 ? " | min same-frame r " + F3(judged.Min()) : ""));
        M("lamps.changing", judged.Count); if (judged.Count > 0) M("lamps.changing_min_r", judged.Min());
        for (var k = 0; k < lenses.Count; k++)
        {
            var range = real[k].Max() - real[k].Min();
            var corr = Correlation(real[k], refl[k]);
            sb.Append(" | " + lenses[k].renderer.transform.parent.name + " (" + lenses[k].px.Count + " px): lens " + F3(real[k].Min()) + "-" + F3(real[k].Max()) + ", reflection " + F3(refl[k].Min()) + "-" + F3(refl[k].Max()) + ", r " + F3(corr));
            if (range > bestRange) { bestRange = range; best = k; }
        }
        var bestCorr = best >= 0 ? Correlation(real[best], refl[best]) : double.NaN;
        Log("LAMPS over 120 frames" + sb + " | most-changing lens: correlation " + F3(bestCorr) + " (lens range " + F3(bestRange) + ")");
        M("lamps.lenses", lenses.Count); M("lamps.best_correlation", bestCorr); M("lamps.best_range", bestRange);
        if (best >= 0)
        {
            var t = new StringBuilder("frame\tlens_linear\treflected\n");
            for (var k = 0; k < real[best].Count; k++) t.Append(k).Append('\t').Append(F3(real[best][k])).Append('\t').Append(F3(refl[best][k])).Append('\n');
            File.WriteAllText(Path.Combine(OutDir, "lamp_sync.tsv"), t.ToString());
        }
        // same-frame off: the most-seen lens is switched off (its Light disabled, its glow black) right before one render,
        // then restored; MapWorld's TickFixtures cannot run in between (one harness tick)
        var kMax = 0; for (var k = 1; k < lenses.Count; k++) if (lenses[k].px.Count > lenses[kMax].px.Count) kMax = k;
        double ReflMean(int k) { var o = ReadFloat(Out); double s = 0; var n = 0; foreach (var i in lenses[k].px) { var c = o[i]; if (c.a <= 1f) continue; s += .2126 * c.r + .7152 * c.g + .0722 * c.b; n++; } return n > 0 ? s / n : 0; }
        {
            var lensR = lenses[kMax].renderer;
            var fixtureLight = lensR.transform.parent != null ? lensR.transform.parent.GetComponentInChildren<Light>(true) : null;
            Render(rt); Sync(rt);
            var before = ReflMean(kMax);
            lensR.GetPropertyBlock(block);
            var keepGlow = block.GetColor("_EmissionColor");
            var keepLight = fixtureLight != null && fixtureLight.enabled;
            block.SetColor("_EmissionColor", Color.black); lensR.SetPropertyBlock(block);
            if (fixtureLight != null) fixtureLight.enabled = false;
            Render(rt); Sync(rt);
            var off = ReflMean(kMax);
            lensR.GetPropertyBlock(block); block.SetColor("_EmissionColor", keepGlow); lensR.SetPropertyBlock(block);
            if (fixtureLight != null) fixtureLight.enabled = keepLight;
            Render(rt); Sync(rt);
            var back = ReflMean(kMax);
            Log("LAMPS same-frame off: " + lensR.transform.parent.name + " (" + lenses[kMax].px.Count + " px) reflection " + F3(before) + " -> " + F3(off) + " on the frame it went out ("
                + F(100.0 * off / Math.Max(before, 1e-6)) + " %) -> " + F3(back) + " the frame it came back");
            M("lamps.off_ratio", off / Math.Max(before, 1e-6));
        }
        FrontRoomsGlassRTSystem.AllowAccumulation = true;
    }

    static double Correlation(List<double> a, List<double> b)
    {
        var ma = a.Average(); var mb = b.Average();
        double sab = 0, saa = 0, sbb = 0;
        for (var i = 0; i < a.Count; i++) { sab += (a[i] - ma) * (b[i] - mb); saa += (a[i] - ma) * (a[i] - ma); sbb += (b[i] - mb) * (b[i] - mb); }
        return saa < 1e-12 || sbb < 1e-12 ? double.NaN : sab / Math.Sqrt(saa * sbb);
    }

    // ---- temporal: accumulation while still; a 1 cm/frame strafe (High, Ultra, unfiltered) for edge crawl
    static IEnumerator Temporal(WindowSetup w)
    {
        var v = w.views.Length > 2 ? w.views[2] : w.views[1];
        yield return GoTo(v);
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
        // still camera: frames converge (mean |d| between consecutive frames)
        var deltas = new List<double>();
        Color[] prev = null;
        Time.captureDeltaTime = 1e-6f;   // freeze the lamp clocks so the scene is still
        for (var f = 0; f < 34; f++)
        {
            Render(rt); Sync(rt);
            var o = ReadFloat(Out);
            if (prev != null)
            {
                double s = 0; var n = 0;
                for (var i = 0; i < o.Length; i += 7) { if (o[i].a <= 1f) continue; s += Mathf.Abs(o[i].r - prev[i].r) + Mathf.Abs(o[i].g - prev[i].g) + Mathf.Abs(o[i].b - prev[i].b); n++; }
                deltas.Add(n > 0 ? s / n : 0);
            }
            prev = o;
            if (f == 0) { Read8(rt); SavePng("P1_temporal_still_frame0.png"); }
        }
        Read8(rt); SavePng("P1_temporal_still_frame33.png");
        FrontRoomsGlassRT.TryGetStats(out var st);
        Log("TEMPORAL still: accumulation index now " + st.accumulation + " | mean |d| between consecutive frames: first " + F3(deltas.First()) + " | at 8 " + F3(deltas[Math.Min(7, deltas.Count - 1)]) + " | last " + F3(deltas.Last()));
        M("temporal.still_first_delta", deltas.First()); M("temporal.still_last_delta", deltas.Last()); M("temporal.accumulation", st.accumulation);
        Time.captureDeltaTime = 1f / 60f;

        // strafe 1 cm/frame: crawl = mean |L(t) - (L(t-1) + L(t+1))/2| over the 10 % highest-gradient pixels, divided by
        // their mean L (relative, so the RT radiance and the raster's own 4x MSAA picture compare on one scale). The raster
        // baseline uses the non-glass pixels of the same frames (grain and dithering off, sRGB decoded to linear).
        Grain(false);
        foreach (var mode in new[] { "unfiltered", "high", "ultra" })
        {
            var keepEdge = FrontRoomsGlassRTSystem.EdgeFilterHigh;
            if (mode == "unfiltered") { FrontRoomsGlassRTSystem.EdgeFilterHigh = 0f; FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High; }
            else FrontRoomsGlassRT.Quality = mode == "high" ? FrontRoomsGlassRTQuality.High : FrontRoomsGlassRTQuality.Ultra;
            var frames = new List<float[]>();
            var raster = new List<float[]>();
            var start = v.feet;
            for (var f = 0; f < 30; f++)
            {
                Place(start + w.along * (.01f * f), v.look + w.along * (.01f * f));
                yield return 1;
                Render(rt); Sync(rt);
                var o = ReadFloat(Out);
                var lum = new float[o.Length];
                for (var i = 0; i < o.Length; i++) lum[i] = o[i].a > 1f ? .2126f * o[i].r + .7152f * o[i].g + .0722f * o[i].b : -1f;
                frames.Add(lum);
                if (mode == "unfiltered")
                {
                    var b8 = Read8(rt);
                    var rl = new float[lum.Length];
                    for (var i = 0; i < rl.Length; i++)
                        rl[i] = lum[i] >= 0f ? -1f : .2126f * Mathf.GammaToLinearSpace(b8[3 * i] / 255f) + .7152f * Mathf.GammaToLinearSpace(b8[3 * i + 1] / 255f) + .0722f * Mathf.GammaToLinearSpace(b8[3 * i + 2] / 255f);
                    raster.Add(rl);
                }
                if (f == 15) { Grain(true); Render(rt); Read8(rt); SavePng("P1_edge_crawl_" + mode + ".png"); Grain(false); }
                if (mode == "high" && (f == 0 || f == 10 || f == 20 || f == 29)) { Grain(true); Render(rt); Read8(rt); SavePng("P1_strafe_high_f" + f.ToString("00") + ".png"); Grain(false); }
            }
            var cr = Crawl(frames);
            double crawl = cr.crawl, rel = cr.rel; var cnt = cr.count;
            Log("TEMPORAL strafe 1 cm/frame, " + mode + ": crawl at the 10 % highest-contrast reflection pixels " + F3(crawl) + " (relative to their mean " + F3(rel) + ", " + cnt + " px)");
            M("temporal.crawl_" + mode, crawl); M("temporal.crawl_rel_" + mode, rel);
            if (raster.Count > 0)
            {
                var rcr = Crawl(raster);
                double rr = rcr.rel; var rn = rcr.count;
                Log("TEMPORAL strafe 1 cm/frame, RASTER baseline (non-glass pixels of the same frames, 4x MSAA): relative crawl " + F3(rr) + " (" + rn + " px)");
                M("temporal.crawl_rel_raster", rr);
            }
            FrontRoomsGlassRTSystem.EdgeFilterHigh = keepEdge;
        }
        Grain(true);
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
        yield return GoTo(v);
    }

    static CrawlResult Crawl(List<float[]> frames)
    {
        var grad = new List<GradPx>();
        var f0 = frames[0];
        for (var y = 1; y < H - 1; y++)
        for (var x = 1; x < W - 1; x++)
        {
            var i = y * W + x;
            if (f0[i] < 0 || f0[i + 1] < 0 || f0[i + W] < 0) continue;
            grad.Add(new GradPx { g = Mathf.Abs(f0[i + 1] - f0[i]) + Mathf.Abs(f0[i + W] - f0[i]), i = i });
        }
        grad.Sort((a, b) => b.g.CompareTo(a.g));
        var top = grad.Take(Math.Max(1, grad.Count / 10)).Select(t => t.i).ToArray();
        double sum = 0, lsum = 0; var cnt = 0;
        foreach (var i in top)
        {
            double s2 = 0, l2 = 0; var m2 = 0;
            for (var k = 1; k + 1 < frames.Count; k++)
            {
                float a = frames[k - 1][i], b = frames[k][i], c = frames[k + 1][i];
                if (a < 0 || b < 0 || c < 0) continue;
                s2 += Math.Abs(b - .5 * (a + c)); l2 += b; m2++;
            }
            if (m2 < 10) continue;
            sum += s2 / m2; lsum += l2 / m2; cnt++;
        }
        var crawl = cnt > 0 ? sum / cnt : 0;
        var mean = cnt > 0 ? lsum / cnt : 1;
        return new CrawlResult { crawl = crawl, rel = crawl / Math.Max(mean, 1e-6), count = cnt };
    }

    // ---- P0-A9: a second camera (not opted in) and 100 frames: RT only on the game camera, no re-allocation
    static IEnumerator Cameras(WindowSetup w)
    {
        yield return GoTo(w.views[1]);
        var go = new GameObject("RTVerify second camera");
        var c2 = go.AddComponent<Camera>();
        c2.CopyFrom(cam);
        c2.enabled = false;
        var half = new RenderTexture(W / 2, H / 2, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
        half.Create();
        var re0 = camState.reallocations;
        FrontRoomsGlassRT.TryGetStats(out var s0);
        for (var k = 0; k < 100; k++)
        {
            Render(rt);
            Render(half, c2);
            if (k % 10 == 0) yield return 1;
        }
        Sync(rt);
        FrontRoomsGlassRT.TryGetStats(out var s1);
        Log("CAMERAS: 100 frames game camera + a second 960x540 camera: second camera has the RT component " + (c2.GetComponent<FrontRoomsGlassRTCamera>() != null)
            + " | trace events +" + (s1.traceEvents - s0.traceEvents) + " (expected 100: game camera only) | events +" + (s1.events - s0.events) + " | skipped not ready +" + (s1.skippedNotReady - s0.skippedNotReady)
            + " | ring busy +" + (s1.ringBusy - s0.ringBusy) + " | no frame +" + (FrontRoomsGlassRTNative.Get(FrontRoomsGlassRTNative.Stat.SkippedNoFrame)) + " | game-camera target re-allocations " + (camState.reallocations - re0)
            + " | Scene view: not a Game camera (feature returns before enqueueing)");
        M("cameras.trace_events", s1.traceEvents - s0.traceEvents); M("cameras.reallocations", camState.reallocations - re0);
        Object.Destroy(go);
        half.Release();
    }

    // ---- P0-A8: +3,000 synthetic instances: the cap holds and priority instances stay
    static IEnumerator Capacity(WindowSetup w)
    {
        yield return GoTo(w.views[1]);
        var root = new GameObject("RTVerify capacity");
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        var handles = new List<int>();
        var rng = new System.Random(7);
        var eye = cam.transform.position;
        for (var k = 0; k < 3000; k++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(root.transform, false);
            go.transform.position = eye + new Vector3((float)rng.NextDouble() * 140f - 70f, -30f, (float)rng.NextDouble() * 140f - 70f);
            go.transform.localScale = Vector3.one * .1f;
            var r = go.GetComponent<Renderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off;
            handles.Add(FrontRoomsGlassRT.Register(r, FrontRoomsGlassRTFlags.None));
        }
        foreach (var q in new[] { FrontRoomsGlassRTQuality.High, FrontRoomsGlassRTQuality.Ultra })
        {
            var c = CaptureRT(null, q, 3);
            Log("CAPACITY " + q + ": registered " + FrontRoomsGlassRTSystem.RegisteredInstances + " | priority instances (receivers, glass, dynamic) " + FrontRoomsGlassRTSystem.LastPriority
                + " all kept | non-priority instances within 20 m dropped " + FrontRoomsGlassRTSystem.LastDroppedNear + " | TLAS instances " + c.stats.tlasInstances + " (cap " + (q == FrontRoomsGlassRTQuality.Ultra ? 4096 : 2048) + ") | glass px " + c.stats.glassPixels
                + " | miss " + c.stats.missPixels + " | AS GPU " + F3(c.stats.gpuMsAccel) + " ms | main " + F3(c.stats.mainThreadMs) + " ms");
            M("capacity." + q + ".tlas", c.stats.tlasInstances); M("capacity." + q + ".dropped_within_20m", FrontRoomsGlassRTSystem.LastDroppedNear); M("capacity." + q + ".gpu_as_ms", c.stats.gpuMsAccel); M("capacity." + q + ".main_ms", c.stats.mainThreadMs);
        }
        foreach (var h in handles) FrontRoomsGlassRT.Unregister(h);
        Object.Destroy(root);
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
        yield return 2;
    }

    // ---- P0-A13: PrepareMeshes with 150 ranges; a 20k-triangle deforming mesh refit
    static IEnumerator Fracture(WindowSetup w)
    {
        yield return GoTo(w.views[1]);
        var verts = new List<Vector3>(); var idx = new List<int>(); var ranges = new List<FrontRoomsGlassRTRange>();
        var rng = new System.Random(11);
        for (var p = 0; p < 150; p++)
        {
            var start = idx.Count; var bv = verts.Count;
            var c = new Vector3((float)rng.NextDouble() * 1.4f - .7f, (float)rng.NextDouble() * 1.6f - .8f, 0f);
            for (var k = 0; k < 6; k++)
            {
                var a0 = k * Mathf.PI / 3f;
                verts.Add(c + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * .05f + Vector3.forward * .003f);
                verts.Add(c + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * .05f - Vector3.forward * .003f);
            }
            for (var k = 0; k < 6; k++)
            {
                int a = 2 * k, b = 2 * ((k + 1) % 6);
                idx.AddRange(new[] { a - 0, b, a + 1, b, b + 1, a + 1 });
            }
            for (var k = 1; k < 5; k++) { idx.AddRange(new[] { 0, 2 * k, 2 * (k + 1) }); idx.AddRange(new[] { 1, 2 * (k + 1) + 1, 2 * k + 1 }); }
            ranges.Add(new FrontRoomsGlassRTRange { indexStart = start, indexCount = idx.Count - start, baseVertex = bv });
        }
        var mesh = new Mesh { name = "RTVerify pieces", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.SetVertices(verts); mesh.SetTriangles(idx, 0, false); mesh.RecalculateNormals();
        var notBefore = Time.frameCount + 3;
        var ticket = FrontRoomsGlassRT.PrepareMeshes(mesh, ranges.ToArray(), notBefore);
        var log2 = new StringBuilder();
        var doneAt = -1;
        for (var f = 0; f < 12; f++)
        {
            Render(rt); Sync(rt);
            FrontRoomsGlassRT.TryGetStats(out var s);
            var prepared = FrontRoomsGlassRT.IsPrepared(ticket);
            log2.Append(" f+" + (Time.frameCount - (notBefore - 3)) + ": builds " + FrontRoomsGlassRTNative.Get(FrontRoomsGlassRTNative.Stat.BuildsLast) + " AS " + F3(s.gpuMsAccel) + " ms" + (prepared ? " PREPARED" : ""));
            if (prepared && doneAt < 0) doneAt = Time.frameCount - notBefore;
            yield return 1;
        }
        Log("FRACTURE: PrepareMeshes 150 ranges (" + idx.Count / 3 + " triangles), notBeforeFrame = now+3 | prepared " + (doneAt >= 0 ? doneAt + " frames after notBeforeFrame" : "NO") + " |" + log2);
        M("fracture.frames_after_notbefore", doneAt);
        // deforming 20k-triangle grid
        var grid = new Mesh { name = "RTVerify deforming", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        var gv = new List<Vector3>(); var gi = new List<int>();
        const int N = 101;
        for (var y = 0; y < N; y++) for (var x = 0; x < N; x++) gv.Add(new Vector3(x * .02f, y * .02f, 0f));
        for (var y = 0; y < N - 1; y++) for (var x = 0; x < N - 1; x++) { var i = y * N + x; gi.AddRange(new[] { i, i + N, i + 1, i + 1, i + N, i + N + 1 }); }
        grid.SetVertices(gv); grid.SetTriangles(gi, 0); grid.RecalculateNormals();
        var go = new GameObject("RTVerify deforming");
        go.transform.position = cam.transform.position - w.dir * 2f + Vector3.up * -1f;
        go.AddComponent<MeshFilter>().sharedMesh = grid;
        go.AddComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        var h = FrontRoomsGlassRT.Register(go.GetComponent<MeshRenderer>(), FrontRoomsGlassRTFlags.Deforming);
        for (var f = 0; f < 3; f++) { Render(rt); Sync(rt); yield return 1; }
        FrontRoomsGlassRT.TryGetStats(out var before);
        var refit = new List<double>();
        for (var f = 0; f < 6; f++)
        {
            for (var k = 0; k < gv.Count; k++) gv[k] = new Vector3(gv[k].x, gv[k].y, .05f * Mathf.Sin(f + gv[k].x * 10f));
            grid.SetVertices(gv);
            FrontRoomsGlassRT.RefitMesh(grid);
            Render(rt); Sync(rt);
            FrontRoomsGlassRT.TryGetStats(out var s);
            refit.Add(s.gpuMsAccel - before.gpuMsAccel);
            yield return 1;
        }
        Log("DEFORMING: 20,000-triangle mesh refit every frame: AS GPU ms above the plain TLAS frame: " + string.Join(", ", refit.Select(x => F3(x))) + " (plain AS " + F3(before.gpuMsAccel) + " ms)");
        M("fracture.refit20k_ms_median", refit.OrderBy(x => x).ElementAt(refit.Count / 2));
        FrontRoomsGlassRT.Unregister(h);
        Object.Destroy(go);
        yield return 2;
    }

    // ---- P0-A11 / P1-B8: GPU cost at 1080p and 1440p: Off, empty pass split, High, Ultra (medians of 15 frames)
    static IEnumerator Cost(WindowSetup w)
    {
        var views = new List<View> { w.views[1] };
        if (w.views.Length > 4) views.Add(w.views[w.views.Length - 1]);
        foreach (var v in views)
        {
            yield return GoTo(v);
            foreach (var target in new[] { rt, rt1440 })
            {
                var res = target.height + "p";
                double Med(List<double> l) => l.OrderBy(x => x).ElementAt(l.Count / 2);
                double P10(List<double> l) => l.OrderBy(x => x).ElementAt(l.Count / 10);
                var r = new Dictionary<string, List<double>>();
                var stage = new Dictionary<string, (double tr, double rs, double asx, double main, double enc)>();
                // empty / high = the shipped placement (AfterRenderingPrePasses); emptyLate / highLate = the old
                // BeforeRenderingTransparents placement, which splits URP's merged opaque + transparent render pass
                // highRebuild: High with the TLAS rebuilt on every frame (no reuse, the pre-fix-31 path)
                var modes = new[] { "off", "empty", "emptyLate", "high", "highRebuild", "highLate", "highBarrier", "ultra" };
                var trs = new Dictionary<string, List<double>>(); var rss = new Dictionary<string, List<double>>(); var ass = new Dictionary<string, List<double>>();
                var mains = new Dictionary<string, List<double>>(); var encs = new Dictionary<string, List<double>>();
                foreach (var mode in modes) { r[mode] = new List<double>(); trs[mode] = new List<double>(); rss[mode] = new List<double>(); ass[mode] = new List<double>(); mains[mode] = new List<double>(); encs[mode] = new List<double>(); }
                // fix round 31: per mode, how many timed frames built their TLAS and how many reused the previous one
                var tlasBuiltN = new Dictionary<string, int>(); var tlasReusedN = new Dictionary<string, int>();
                foreach (var mode in modes) { tlasBuiltN[mode] = 0; tlasReusedN[mode] = 0; }
                // The Mac is shared with other workflows' Unity processes: the four modes are interleaved frame by frame
                // (one round = off, empty, High, Ultra), so drift in the other load hits every mode alike.
                // The camera is still here: temporal accumulation is off for this test (it never changes the cost much,
                // but it would differ between the interleaved modes).
                var keepAcc = FrontRoomsGlassRTSystem.AllowAccumulation;
                FrontRoomsGlassRTSystem.AllowAccumulation = false;
                for (var round = 0; round < 24; round++)
                {
                    foreach (var mode in modes)
                    {
                        FrontRoomsGlassRT.Quality = mode == "off" ? FrontRoomsGlassRTQuality.Off : mode == "ultra" ? FrontRoomsGlassRTQuality.Ultra : FrontRoomsGlassRTQuality.High;
                        FrontRoomsMetalGlassRTRendererFeature.HarnessEmptyPassOnly = mode == "empty" || mode == "emptyLate";
                        FrontRoomsMetalGlassRTRendererFeature.HarnessLateTrace = mode == "emptyLate" || mode == "highLate";
                        FrontRoomsGlassRTSystem.ForceBarriers = mode == "highLate" || mode == "highBarrier";
                        FrontRoomsGlassRTSystem.TlasReuse = mode != "highRebuild";
                        RenderTimed(target);   // the first render after a switch settles the targets
                        FrontRoomsGlassRT.TryGetStats(out var sBefore);
                        var g = RenderTimed(target);
                        if (round < 2) continue;
                        r[mode].Add(g);
                        FrontRoomsGlassRT.TryGetStats(out var s);
                        tlasBuiltN[mode] += s.tlasBuilt - sBefore.tlasBuilt; tlasReusedN[mode] += s.tlasReused - sBefore.tlasReused;
                        trs[mode].Add(s.gpuMsTrace); rss[mode].Add(s.gpuMsResolve); ass[mode].Add(s.gpuMsAccel); mains[mode].Add(s.mainThreadMs); encs[mode].Add(s.encodeMs);
                    }
                    if (round % 4 == 0) yield return 1;
                }
                foreach (var mode in modes) stage[mode] = (Med(trs[mode]), Med(rss[mode]), Med(ass[mode]), Med(mains[mode]), Med(encs[mode]));
                FrontRoomsGlassRTSystem.AllowAccumulation = keepAcc;
                FrontRoomsMetalGlassRTRendererFeature.HarnessEmptyPassOnly = false;
                FrontRoomsMetalGlassRTRendererFeature.HarnessLateTrace = false;
                FrontRoomsGlassRTSystem.ForceBarriers = false;
                FrontRoomsGlassRTSystem.TlasReuse = true;
                Log("COST " + v.name + " " + res + " TLAS reuse (fix round 31): High AS median " + F3(stage["high"].asx) + " / p10 " + F3(P10(ass["high"])) + " ms vs rebuilt every frame " + F3(stage["highRebuild"].asx) + " / " + F3(P10(ass["highRebuild"]))
                    + " ms | whole-frame High " + F3(Med(r["high"])) + " vs rebuilt " + F3(Med(r["highRebuild"])) + " ms | RT stages High (AS + trace + resolve) " + F3(stage["high"].asx + stage["high"].tr + stage["high"].rs)
                    + " vs rebuilt " + F3(stage["highRebuild"].asx + stage["highRebuild"].tr + stage["highRebuild"].rs) + " ms | frame delta High - Off " + F3(Med(r["high"]) - Med(r["off"])) + " ms"
                    + " | timed High frames: TLAS built " + tlasBuiltN["high"] + ", reused " + tlasReusedN["high"] + " | timed rebuild frames: built " + tlasBuiltN["highRebuild"] + ", reused " + tlasReusedN["highRebuild"]
                    + " | Ultra: built " + tlasBuiltN["ultra"] + ", reused " + tlasReusedN["ultra"]);
                M("cost." + v.name + "." + res + ".high.tlas_reused", tlasReusedN["high"]); M("cost." + v.name + "." + res + ".high.tlas_built", tlasBuiltN["high"]);
                M("cost." + v.name + "." + res + ".highRebuild.as", stage["highRebuild"].asx); M("cost." + v.name + "." + res + ".highRebuild", Med(r["highRebuild"]));
                M("cost." + v.name + "." + res + ".high.stages", stage["high"].asx + stage["high"].tr + stage["high"].rs);
                M("cost." + v.name + "." + res + ".highRebuild.stages", stage["highRebuild"].asx + stage["highRebuild"].tr + stage["highRebuild"].rs);
                Log("COST " + v.name + " " + res + ": whole-frame GPU median off " + F3(Med(r["off"])) + " | empty RT passes after the prepass " + F3(Med(r["empty"])) + " (d " + F3(Med(r["empty"]) - Med(r["off"])) + ")"
                    + " | empty passes before transparents (old placement, split) " + F3(Med(r["emptyLate"])) + " (d " + F3(Med(r["emptyLate"]) - Med(r["off"])) + ")"
                    + " | High at the old placement with barriers (run 8 path) " + F3(Med(r["highLate"])) + " (d " + F3(Med(r["highLate"]) - Med(r["off"])) + ")"
                    + " | High after the prepass with barriers " + F3(Med(r["highBarrier"])) + " (d " + F3(Med(r["highBarrier"]) - Med(r["off"])) + ")"
                    + " | High " + F3(Med(r["high"])) + " (d " + F3(Med(r["high"]) - Med(r["off"])) + ") | Ultra " + F3(Med(r["ultra"])) + " (d " + F3(Med(r["ultra"]) - Med(r["off"])) + ") ms"
                    + " | stage medians High: AS " + F3(stage["high"].asx) + " trace " + F3(stage["high"].tr) + " resolve " + F3(stage["high"].rs) + " | Ultra: AS " + F3(stage["ultra"].asx) + " trace " + F3(stage["ultra"].tr) + " resolve " + F3(stage["ultra"].rs)
                    + " | main thread High " + F3(stage["high"].main) + " ms | encode High " + F3(stage["high"].enc) + " ms");
                var key = "cost." + v.name + "." + res;
                M(key + ".off", Med(r["off"])); M(key + ".empty", Med(r["empty"])); M(key + ".high", Med(r["high"])); M(key + ".ultra", Med(r["ultra"]));
                M(key + ".emptyLate", Med(r["emptyLate"])); M(key + ".highLate", Med(r["highLate"])); M(key + ".highBarrier", Med(r["highBarrier"]));
                foreach (var mode in modes) M(key + "." + mode + ".p10", P10(r[mode]));
                M(key + ".high.trace.p10", P10(trs["high"])); M(key + ".ultra.trace.p10", P10(trs["ultra"])); M(key + ".high.as.p10", P10(ass["high"])); M(key + ".high.resolve.p10", P10(rss["high"]));
                Log("COST " + v.name + " " + res + " 10th percentiles (least disturbed by the other jobs): off " + F3(P10(r["off"])) + " | empty " + F3(P10(r["empty"])) + " | emptyLate " + F3(P10(r["emptyLate"]))
                    + " | High " + F3(P10(r["high"])) + " | highLate " + F3(P10(r["highLate"])) + " | highBarrier " + F3(P10(r["highBarrier"])) + " | Ultra " + F3(P10(r["ultra"]))
                    + " ms | stages High: AS " + F3(P10(ass["high"])) + " trace " + F3(P10(trs["high"])) + " resolve " + F3(P10(rss["high"])) + " | Ultra trace " + F3(P10(trs["ultra"])));
                M(key + ".high.as", stage["high"].asx); M(key + ".high.trace", stage["high"].tr); M(key + ".high.resolve", stage["high"].rs);
                M(key + ".ultra.as", stage["ultra"].asx); M(key + ".ultra.trace", stage["ultra"].tr); M(key + ".ultra.resolve", stage["ultra"].rs);
                M(key + ".high.main", stage["high"].main); M(key + ".high.encode", stage["high"].enc);
            }
        }
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
    }

    // ---- where the trace time goes: the close-up (pane fills the frame) at 1080p, High, interleaved variants:
    // as shipped (4 shadowed lamps, all lamps), no shadow rays, lamps capped at 16 / 4, and no back-surface image.
    static IEnumerator Ablation(WindowSetup w)
    {
        var v = w.views[w.views.Length - 1];
        yield return GoTo(v);
        yield return WaitWarm("ablation " + v.name);
        var keepAcc = FrontRoomsGlassRTSystem.AllowAccumulation;
        FrontRoomsGlassRTSystem.AllowAccumulation = false;
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
        var modes = new[] { "shipped", "noShadow", "hardShadow", "oldShadow", "lamps16", "lamps4", "noBackImage" };
        var trace = modes.ToDictionary(m => m, m => new List<double>());
        var rays = modes.ToDictionary(m => m, m => 0);
        for (var round = 0; round < 20; round++)
        {
            foreach (var mode in modes)
            {
                        // shipped: 6 ranked lamps x 1 soft ray (disc); hardShadow: 6 ranked lamps x 1 ray to the centre; oldShadow: the
                // pre-2026-10-14 rule (4 lamps in camera order, hard, the rest at full light)
                FrontRoomsGlassRTSystem.HighShadowLamps = mode == "noShadow" ? 0 : mode == "oldShadow" ? 4 : 6;
                FrontRoomsGlassRTSystem.ShadowLightRadius = mode == "hardShadow" || mode == "oldShadow" ? 0f : .2f;
                FrontRoomsGlassRTSystem.HarnessShadowKnobs = mode == "oldShadow" ? new Vector3(1, 1, 0) : Vector3.zero;
                FrontRoomsGlassRTSystem.LampCap = mode == "lamps16" ? 16 : mode == "lamps4" ? 4 : 128;
                FrontRoomsGlassRTSystem.BackSurfaceImage = mode != "noBackImage";
                RenderTimed(rt);
                RenderTimed(rt);
                if (round < 2) continue;
                FrontRoomsGlassRT.TryGetStats(out var s);
                trace[mode].Add(s.gpuMsTrace + s.gpuMsResolve);
                rays[mode] = s.shadowRays;
            }
            if (round % 4 == 0) yield return 1;
        }
        FrontRoomsGlassRTSystem.HighShadowLamps = 6; FrontRoomsGlassRTSystem.ShadowLightRadius = .2f; FrontRoomsGlassRTSystem.HarnessShadowKnobs = Vector3.zero;
        FrontRoomsGlassRTSystem.LampCap = 128; FrontRoomsGlassRTSystem.BackSurfaceImage = true;
        FrontRoomsGlassRTSystem.AllowAccumulation = keepAcc;
        double P(List<double> l, double q) { var o = l.OrderBy(x => x).ToList(); return o[Math.Min(o.Count - 1, (int)(o.Count * q))]; }
        Log("ABLATION " + v.name + " 1080p High, trace + resolve GPU ms (p10 / median of 18 interleaved rounds): "
            + string.Join(" | ", modes.Select(m => m + " " + F3(P(trace[m], .1)) + " / " + F3(P(trace[m], .5)) + " (shadow rays " + rays[m] + ")")));
        foreach (var m in modes) { M("ablation." + m + ".p10", P(trace[m], .1)); M("ablation." + m + ".median", P(trace[m], .5)); }
    }

    // ---- P1-B1: parity, primary rays through the hit shader vs the raster (post off, SSAO off, linear HDR)
    static IEnumerator Parity(WindowSetup w, string file, string key)
    {
        var v = w.views[1];
        yield return GoTo(v);
        // look away from the pane, into the lit room (the whole screen is non-glass)
        Look(cam.transform.position - w.dir * 3f + Vector3.up * .3f);
        yield return 1;
        var data = cam.GetUniversalAdditionalCameraData();
        var post = data.renderPostProcessing;
        data.renderPostProcessing = false;
        var rdata = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/FrontRooms_URP_Renderer.asset");
        var ssao = rdata.rendererFeatures.FirstOrDefault(f => f != null && f.GetType().Name.Contains("ScreenSpaceAmbientOcclusion"));
        var ssaoOn = ssao != null && ssao.isActive;
        if (ssao != null) ssao.SetActive(false);
        Time.captureDeltaTime = 1e-6f;
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
        Render(rtHdr); Render(rtHdr); Sync(rtHdr);
        var raster = ReadFloat(rtHdr);
        FrontRoomsGlassRTSystem.DebugMode = FrontRoomsGlassRTNative.DebugParity;
        // Ultra first (16 shadowed lamps per hit): logged per class next to High below
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.Ultra;
        Render(rtHdr); Render(rtHdr); Sync(rtHdr);
        var parityUltra = ReadFloat(camState.raw != null ? camState.raw.rt : null);
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
        // High variant: hard shadows (one ray to each lamp's centre, the pre-2026-10-14 shadow rays, ranked per hit)
        var keepRadius = FrontRoomsGlassRTSystem.ShadowLightRadius;
        FrontRoomsGlassRTSystem.ShadowLightRadius = 0f;
        Render(rtHdr); Render(rtHdr); Sync(rtHdr);
        var parityHard = ReadFloat(camState.raw != null ? camState.raw.rt : null);
        FrontRoomsGlassRTSystem.ShadowLightRadius = keepRadius;
        // parity needs a visible receiver to dispatch: keep the pane in view at the edge by using the same camera
        Render(rtHdr); Render(rtHdr); Sync(rtHdr);
        var parity = ReadFloat(camState.raw != null ? camState.raw.rt : null);
        var ids = ReadFloat(camState.ids != null ? camState.ids.rt : null);
        FrontRoomsGlassRTSystem.DebugMode = FrontRoomsGlassRTNative.DebugNone;
        if (ssao != null) ssao.SetActive(ssaoOn);
        data.renderPostProcessing = post;
        Time.captureDeltaTime = 1f / 60f;
        if (parity == null || ids == null) { Log("PARITY: targets missing"); yield break; }
        var hits = 0;
        foreach (var c in ids) if (c.r > 0) hits++;
        // per material class (material index in ids.b): mean |d| / raster and p95, luminance
        var perMat = new Dictionary<int, List<double>>();
        var perMatU = new Dictionary<int, List<double>>();
        var perMatH = new Dictionary<int, List<double>>();
        var lumMat = new Dictionary<int, double[]>();   // sum raster, sum RT High
        double sumRT = 0, sumRa = 0;
        for (var i = 0; i < raster.Length; i++)
        {
            if (ids[i].r <= 0) continue;
            var a = raster[i]; var b = parity[i];
            var la = .2126 * a.r + .7152 * a.g + .0722 * a.b;
            var lb = .2126 * b.r + .7152 * b.g + .0722 * b.b;
            sumRT += lb; sumRa += la;
            var rel = Math.Abs(lb - la) / Math.Max(la, .02);
            var mi = (int)ids[i].b;
            if (!perMat.TryGetValue(mi, out var l)) perMat[mi] = l = new List<double>();
            l.Add(rel);
            if (!lumMat.TryGetValue(mi, out var lm)) lumMat[mi] = lm = new double[2];
            lm[0] += la; lm[1] += lb;
            if (parityHard != null)
            {
                var hh = parityHard[i];
                var lh = .2126 * hh.r + .7152 * hh.g + .0722 * hh.b;
                if (!perMatH.TryGetValue(mi, out var lh2)) perMatH[mi] = lh2 = new List<double>();
                lh2.Add(Math.Abs(lh - la) / Math.Max(la, .02));
            }
            if (parityUltra != null)
            {
                var u = parityUltra[i];
                var lu = .2126 * u.r + .7152 * u.g + .0722 * u.b;
                if (!perMatU.TryGetValue(mi, out var lu2)) perMatU[mi] = lu2 = new List<double>();
                lu2.Add(Math.Abs(lu - la) / Math.Max(la, .02));
            }
        }
        var sb = new StringBuilder();
        foreach (var kv in perMat.OrderByDescending(k => k.Value.Count).Take(10))
        {
            var l = kv.Value.OrderBy(x => x).ToList();
            var mname = kv.Key >= 0 && kv.Key < FrontRoomsGlassRTSystem.MaterialNames.Count ? FrontRoomsGlassRTSystem.MaterialNames[kv.Key] : "?";
            sb.Append(" | mat " + kv.Key + " " + mname + ": n " + l.Count + " mean " + F3(l.Average()) + " median " + F3(l[l.Count / 2]) + " p95 " + F3(l[(int)(l.Count * .95)])
                + " | luminance RT/raster " + F3(lumMat[kv.Key][1] / Math.Max(lumMat[kv.Key][0], 1e-9)));
            if (perMatU.TryGetValue(kv.Key, out var lu3) && lu3.Count > 0) { lu3.Sort(); sb.Append(" (Ultra: mean " + F3(lu3.Average()) + " p95 " + F3(lu3[(int)(lu3.Count * .95)]) + ")"); M(key + ".mat" + kv.Key + ".ultra_p95", lu3[(int)(lu3.Count * .95)]); M(key + ".mat" + kv.Key + ".ultra_mean", lu3.Average()); }
            if (perMatH.TryGetValue(kv.Key, out var lh3) && lh3.Count > 0) { lh3.Sort(); sb.Append(" (High, hard centre rays: mean " + F3(lh3.Average()) + " p95 " + F3(lh3[(int)(lh3.Count * .95)]) + ")"); M(key + ".mat" + kv.Key + ".hard_mean", lh3.Average()); M(key + ".mat" + kv.Key + ".hard_p95", lh3[(int)(lh3.Count * .95)]); }
            M(key + ".mat" + kv.Key + ".mean", l.Average()); M(key + ".mat" + kv.Key + ".p95", l[(int)(l.Count * .95)]);
        }
        Log(key.ToUpperInvariant() + " (post off, SSAO off, linear): pixels hit " + hits + " of " + raster.Length + " | mean luminance raster " + F3(sumRa / Math.Max(1, hits)) + " vs RT " + F3(sumRT / Math.Max(1, hits))
            + " (ratio " + F3(sumRT / Math.Max(sumRa, 1e-6)) + ")" + sb);
        M(key + ".luma_ratio", sumRT / Math.Max(sumRa, 1e-6));
        // sheet: raster | RT | x8 difference
        var outB = new byte[W * H * 3];
        void Tone(Color[] src, int panel)
        {
            for (var i = 0; i < src.Length; i++)
            {
                float T(float x) { x *= 2f; return Mathf.LinearToGammaSpace(x / (1f + x)); }
                outB[3 * i] = (byte)Mathf.Clamp(T(src[i].r) * 255f, 0, 255); outB[3 * i + 1] = (byte)Mathf.Clamp(T(src[i].g) * 255f, 0, 255); outB[3 * i + 2] = (byte)Mathf.Clamp(T(src[i].b) * 255f, 0, 255);
            }
        }
        Tone(raster, 0); SaveBytes(outB, file + "_raster.png");
        Tone(parity, 1); SaveBytes(outB, file + "_rt.png");
        for (var i = 0; i < raster.Length; i++)
        {
            var d = ids[i].r > 0 ? Mathf.Clamp01(8f * Mathf.Abs((raster[i].r + raster[i].g + raster[i].b) - (parity[i].r + parity[i].g + parity[i].b)) / 3f) : 0f;
            outB[3 * i] = outB[3 * i + 1] = outB[3 * i + 2] = (byte)(d * 255f);
        }
        SaveBytes(outB, file + "_diff8.png");
        yield return GoTo(v);
    }

    // ---- Codex audit 10_review_rt.md sec. 4.3: the G14 hook proof's one open fail (38_deadlamp_window_1.5m, P5: a depth tag
    // 0.5 m behind the pane is NOT rejected, in that view only). Lists every glass renderer around that window and around
    // the passing test window: a second receiver about 0.5 m behind the first would take the +0.5 m tag correctly.
    static string PathOf(Transform t) { var s = t.name; for (var p = t.parent; p != null && p != map.transform; p = p.parent) s = p.name + "/" + s; return s; }

    static IEnumerator Pairs()
    {
        foreach (var lp in new[] { new LabelledPair(new GridCoord(304, 207), new GridCoord(305, 207), "38_deadlamp_window (G10 proof P5 FAIL)"),
                                   new LabelledPair(new GridCoord(282, 206), new GridCoord(282, 207), "window_L0 (G10 proof P5 pass)") })
        {
            var a = lp.a; var b = lp.b; var label = lp.label;
            var cross = map.CrossingPoint(a, b);
            var d = map.CellCenter(b) - map.CellCenter(a); d.y = 0f; d.Normalize();
            var center = cross + Vector3.up * ((ModuleUnits.WindowSill + ModuleUnits.WindowTop) * .5f);
            foreach (var side in new[] { -1f, 1f })
            {
                Place(cross + d * (1.5f * side), center);
                yield return Settle(6);
                Resume();
                Place(cross + d * (1.5f * side), center);
                yield return 2;
                var eye = cam.transform.position;
                var sb = new StringBuilder();
                var n = 0;
                foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                {
                    var m = r.sharedMaterial;
                    if (m == null || !FrontRoomsGlassRTSystem.IsGlassMaterial(m)) continue;
                    var c = r.bounds.center;
                    if (new Vector2(c.x - cross.x, c.z - cross.z).magnitude > 3f) continue;
                    n++;
                    var depth = Vector3.Dot(c - eye, cam.transform.forward);
                    sb.Append(" | " + PathOf(r.transform) + " [" + m.name + " (" + (m.shader != null ? m.shader.name : "?") + "), _RTReceive " + (m.HasProperty("_RTReceive") ? F(m.GetFloat("_RTReceive")) : "-")
                        + ", drawn " + (r.enabled && r.gameObject.activeInHierarchy) + "] centre " + V(c) + " | offset along the window axis a->b " + F3(Vector3.Dot(c - cross, d)) + " m | eye depth " + F3(depth) + " m | size " + V(r.bounds.size));
                }
                Log("PAIRS " + label + " " + a + "-" + b + ", camera on the " + (side < 0 ? "a" : "b") + " side (" + map.Cache.ZoneOf(side < 0 ? a : b).theme + "/" + map.Cache.ZoneOf(side < 0 ? a : b).height
                    + ") | edge " + map.Cache.Edge(a, b) + " | glass renderers within 3 m: " + n + sb);
            }
        }
    }

    // ---- Codex audit sec. 4.3: re-run the hook proof's P4/P5 at the dead-lamp window (304,207)-(305,207) with the RT feature
    // off and the hook's globals driven by hand (as the G10 proof does), ARGBFloat 4x MSAA, post off, exact float compare.
    // The lamp over the camera is killed (ModuleLamp.Dead) as in the proof's view: the game then cross-fades the zone cube
    // to DeadLamp over 0.5 s. Sequence A renders on consecutive frames WHILE that fade runs; sequence B after it ended.
    static Texture2D TagTexture(Renderer slab, float extra, out int count)
    {
        var data = new float[W * H * 4];
        count = 0;
        var t = slab.transform;
        var s = t.lossyScale; s = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
        var thin = s.x <= s.y && s.x <= s.z ? 0 : s.y <= s.z ? 1 : 2;
        var axes = new[] { t.right, t.up, t.forward };
        var n = axes[thin].normalized;
        int uAx = (thin + 1) % 3, vAx = (thin + 2) % 3;
        var camPos = cam.transform.position; var fwd = cam.transform.forward;
        var side = Mathf.Sign(Vector3.Dot(camPos - t.position, n));
        var planePoint = t.position + n * (s[thin] * .5f * side);
        var inv = (Matrix4x4.Perspective(cam.fieldOfView, (float)W / H, cam.nearClipPlane, cam.farClipPlane) * cam.worldToCameraMatrix).inverse;
        for (var y = 0; y < H; y++)
        for (var x = 0; x < W; x++)
        {
            var farP = inv * new Vector4((x + .5f) / W * 2f - 1f, (y + .5f) / H * 2f - 1f, 1f, 1f);
            var dir = ((Vector3)farP / farP.w - camPos).normalized;
            var den = Vector3.Dot(dir, n);
            if (Mathf.Abs(den) < 1e-6f) continue;
            var tt = Vector3.Dot(planePoint - camPos, n) / den;
            if (tt <= 0f) continue;
            var hit = camPos + dir * tt;
            var local = t.InverseTransformPoint(hit);
            if (Mathf.Abs(local[uAx]) > .5f || Mathf.Abs(local[vAx]) > .5f) continue;
            var i = (y * W + x) * 4;
            data[i] = 2f;
            data[i + 3] = 1f + Vector3.Dot(hit - camPos, fwd) + extra;
            count++;
        }
        var tex = new Texture2D(W, H, TextureFormat.RGBAFloat, false, true) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        tex.SetPixelData(data, 0);
        tex.Apply(false);
        return tex;
    }

    static IEnumerator TagProof()
    {
        var a = new GridCoord(304, 207); var b = new GridCoord(305, 207);
        var cross = map.CrossingPoint(a, b);
        var d = map.CellCenter(b) - map.CellCenter(a); d.y = 0f; d.Normalize();
        var center = cross + Vector3.up * ((ModuleUnits.WindowSill + ModuleUnits.WindowTop) * .5f);
        var feet = cross - d * 1.5f;
        Place(feet, center);
        yield return Settle(6);
        Resume();
        Place(feet, center);
        yield return 30;
        var col = WindowCollider(a, b);
        var glass = GlassOf(col);
        if (glass.Length == 0) { Log("TAGPROOF: no visible glass at the dead-lamp window"); yield break; }
        var slab = glass[0];
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.Off;   // the feature sets no globals; the test drives them
        Grain(false);
        var data = cam.GetUniversalAdditionalCameraData();
        var post = data.renderPostProcessing;
        data.renderPostProcessing = false;
        var tag0 = TagTexture(slab, 0f, out var tagged);
        var tag5 = TagTexture(slab, .5f, out _);
        var none = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true); none.SetPixel(0, 0, new Color(0, 0, 0, 0)); none.Apply();
        var kw = GlobalKeyword.Create("_FR_GLASS_RT");
        float[] R(Texture tex, float weight)
        {
            Shader.SetKeyword(kw, true);
            Shader.SetGlobalTexture(ProofRTTex, tex);
            Shader.SetGlobalFloat(ProofRTWeight, weight);
            return ProofRender();
        }
        var cell = map.CellOf(feet);
        var keepMode = map.LampModeOf(cell);
        var sb = new StringBuilder();
        map.SetLampMode(cell, ModuleLamp.Dead);   // as in the proof's view (no change when the cell's lamp is already dead)
        yield return 2;
        { var guard = 0; while (FrontRoomsZoneReflection.Fading && guard++ < 300) yield return 1; }
        yield return 5;
        // S: every render in ONE frame (nothing in the scene can move between them): the hook's own behaviour
        var k0 = R(none, 0f);
        var p4 = R(tag0, 1f);
        var p5 = R(tag5, 1f);
        var k1 = R(none, 0f);
        var same = BitDiff(k0, k1); var s4 = BitDiff(k0, p4); var s5 = BitDiff(k0, p5);
        sb.Append(" | S (same frame, zone " + FrontRoomsZoneReflection.Zone + "): baseline vs baseline " + same.px + " px | P4 (tag = the glass's own depth) " + s4.px + " px changed (max "
            + s4.max.ToString("G3", CultureInfo.InvariantCulture) + ") | P5 (tag + 0.5 m) " + s5.px + " px changed (max " + s5.max.ToString("G3", CultureInfo.InvariantCulture) + ")");
        M("tagproof.same_baseline_px", same.px); M("tagproof.same_p4_px", s4.px); M("tagproof.same_p5_px", s5.px);
        // F: the same baseline one frame later, the game clock running (lamps tick, any zone fade steps)
        yield return 1;
        var k2 = R(none, 0f);
        var drift = BitDiff(k0, k2);
        sb.Append(" | F (one frame later, clock running): baseline vs baseline " + drift.px + " px (max " + drift.max.ToString("G3", CultureInfo.InvariantCulture) + ")");
        M("tagproof.next_frame_baseline_px", drift.px);
        // Z: a zone-cube cross-fade running (Level0 -> DeadLamp over 0.5 s), clock running: P5 vs a baseline one frame apart
        FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.Level0, 0f);
        FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.DeadLamp, .5f);
        yield return 2;
        var z0 = R(none, 0f); var zf = FrontRoomsZoneReflection.Fading;
        var zp5 = R(tag5, 1f);
        var zs = BitDiff(z0, zp5);
        yield return 1;
        var z1 = R(none, 0f);
        var zd = BitDiff(z0, z1);
        sb.Append(" | Z (zone cube cross-fade running " + zf + "): P5 vs the same-frame baseline " + zs.px + " px | baseline vs baseline one frame later " + zd.px + " px (max " + zd.max.ToString("G3", CultureInfo.InvariantCulture) + ")");
        M("tagproof.fade_same_p5_px", zs.px); M("tagproof.fade_next_frame_px", zd.px);
        { var guard = 0; while (FrontRoomsZoneReflection.Fading && guard++ < 300) yield return 1; }
        Shader.SetKeyword(kw, false);
        Shader.SetGlobalFloat(ProofRTWeight, 0f);
        data.renderPostProcessing = post;
        map.SetLampMode(cell, keepMode);
        Object.Destroy(tag0); Object.Destroy(tag5); Object.Destroy(none);
        Grain(true);
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
        Log("TAGPROOF dead-lamp window " + a + "-" + b + " (" + slab.name + ", " + tagged + " tagged px, lamp cell " + cell + " set Dead, was " + keepMode + ")" + sb);
    }

    // ---- Codex audit F1: an exception in the per-frame RT build fails closed for the session (no frozen or black frames,
    // no ZBinningJob errors). The fault is injected exactly as the audit did: FrontRoomsGlassRTSystem.instBuffer = null.
    static IEnumerator FailClosedTest(WindowSetup w)
    {
        yield return GoTo(w.views[1]);
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
        Grain(false);
        var errors = new List<string>();
        Application.LogCallback onLog = (msg, stack, type) => { if (type == LogType.Error || type == LogType.Exception) errors.Add(type + ": " + (msg.Length > 140 ? msg.Substring(0, 140) : msg)); };
        double MeanLuma(byte[] p) { double s = 0; for (var i = 0; i < p.Length / 3; i += 13) s += Luma8(p, i); return s / (p.Length / 3 / 13); }
        Render(rt); var before = MeanLuma(Read8(rt));
        var failed0 = FrontRoomsGlassRTSystem.FailedClosed;
        Application.logMessageReceived += onLog;
        FrontRoomsGlassRTSystem.HarnessThrow = true;   // BuildFrame throws inside URP's record, every frame it is asked
        var lumas = new List<double>();
        var live = 0;
        for (var k = 0; k < 12; k++)
        {
            // move a little every frame so a frozen frame would show as an unchanged image
            Place(w.views[1].feet + w.along * (.02f * k), w.views[1].look + w.along * (.02f * k));
            yield return 1;
            Render(rt);
            var l = MeanLuma(Read8(rt));
            if (lumas.Count > 0 && Math.Abs(l - lumas[lumas.Count - 1]) > 1e-6) live++;
            lumas.Add(l);
        }
        FrontRoomsGlassRTSystem.HarnessThrow = false;
        Application.logMessageReceived -= onLog;
        var zbin = errors.Count(e => e.Contains("ZBinning"));
        var rg = errors.Count(e => e.Contains("Render Graph"));
        Log("FAIL CLOSED: fault injected (BuildFrame throws) for 12 frames | failed closed " + (FrontRoomsGlassRTSystem.FailedClosed - failed0) + " time(s) | quality now " + FrontRoomsGlassRT.Quality
            + " | errors logged " + errors.Count + " (ZBinningJob " + zbin + ", Render Graph " + rg + ") | frames that changed while the camera moved " + live + " of 11 | mean luma before " + F(before)
            + ", during " + F(lumas.Min()) + "-" + F(lumas.Max()) + (errors.Count > 0 ? " | first: " + errors[0] : "") + (errors.Count > 1 ? " | second: " + errors[1] : "") + " (audit on main: 24 errors, 12 of 12 frames frozen)");
        M("failclosed.errors", errors.Count); M("failclosed.zbinning", zbin); M("failclosed.live_frames", live); M("failclosed.count", FrontRoomsGlassRTSystem.FailedClosed - failed0);

        // Fix round 31 (review 1 engineering #8): every other entry point fails closed the same way. Per site: re-arm (harness
        // only), inject one fault there, drive that path, then check: failed closed exactly once, quality Off, exactly one
        // console entry, no exception reaching the caller (the map, GD3, URP's record), and frames still rendering.
        var glassComp = Object.FindObjectsByType<FrontRoomsGlassBreakable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .OrderBy(g => (g.transform.position - w.center).sqrMagnitude).FirstOrDefault();
        var onReady = typeof(FrontRoomsGlassRTSystem).GetMethod("OnGlassMeshesReady", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var sites = new[] { "mid-frame (after the scene update)", "record (after the build)", "GlassBroken (map event)", "Register (API)", "MeshesReady (GD3 event)" };
        var sitesOk = 0;
        foreach (var site in sites)
        {
            FrontRoomsGlassRTSystem.HarnessRearm(FrontRoomsGlassRTQuality.High);
            Place(w.views[1].feet, w.views[1].look);
            yield return 1;
            Render(rt); Render(rt);
            var siteErrors = new List<string>();
            Application.LogCallback onSiteLog = (msg, stack, type) => { if (type == LogType.Error || type == LogType.Exception) siteErrors.Add(type + ": " + (msg.Length > 120 ? msg.Substring(0, 120) : msg)); };
            Application.logMessageReceived += onSiteLog;
            var f0 = FrontRoomsGlassRTSystem.FailedClosed;
            var escaped = 0;
            var siteLumas = new List<double>();
            var siteLive = 0;
            try
            {
                if (site.StartsWith("mid-frame")) FrontRoomsGlassRTSystem.HarnessFault = 2;
                else if (site.StartsWith("record")) FrontRoomsMetalGlassRTRendererFeature.HarnessRecordFault = true;
                else if (site.StartsWith("GlassBroken")) { FrontRoomsGlassRTSystem.HarnessFault = 3; FrontRoomsGlassRT.GlassBroken(w.center + w.along * 40f); }
                else if (site.StartsWith("Register"))
                {
                    FrontRoomsGlassRTSystem.HarnessFault = 4;
                    var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    probe.transform.position = w.center + Vector3.up * 30f;
                    FrontRoomsGlassRT.Register(probe.GetComponent<MeshRenderer>(), FrontRoomsGlassRTFlags.None, -1);
                    Object.Destroy(probe);
                }
                else if (onReady != null && glassComp != null) { FrontRoomsGlassRTSystem.HarnessFault = 5; onReady.Invoke(null, new object[] { glassComp }); }
            }
            catch (Exception) { escaped++; }
            for (var k = 0; k < 8; k++)
            {
                Place(w.views[1].feet + w.along * (.02f * k), w.views[1].look + w.along * (.02f * k));
                yield return 1;
                try { Render(rt); } catch (Exception) { escaped++; }
                var l = MeanLuma(Read8(rt));
                if (siteLumas.Count > 0 && Math.Abs(l - siteLumas[siteLumas.Count - 1]) > 1e-6) siteLive++;
                siteLumas.Add(l);
            }
            FrontRoomsGlassRTSystem.HarnessFault = 0;
            FrontRoomsMetalGlassRTRendererFeature.HarnessRecordFault = false;
            Application.logMessageReceived -= onSiteLog;
            var count = FrontRoomsGlassRTSystem.FailedClosed - f0;
            var ok = count == 1 && FrontRoomsGlassRT.Quality == FrontRoomsGlassRTQuality.Off && siteErrors.Count == 1 && escaped == 0 && siteLive >= 6;
            if (ok) sitesOk++;
            var tag = site.Split(' ')[0];
            Log("FAIL CLOSED " + site + ": failed closed " + count + " time(s) | quality now " + FrontRoomsGlassRT.Quality + " | console entries " + siteErrors.Count
                + " | exceptions reaching the caller " + escaped + " | frames that changed while the camera moved " + siteLive + " of 7"
                + (siteErrors.Count > 0 ? " | first: " + siteErrors[0] : "") + (siteErrors.Count > 1 ? " | second: " + siteErrors[1] : "") + " -> " + (ok ? "PASS" : "FAIL"));
            M("failclosed." + tag + ".count", count); M("failclosed." + tag + ".errors", siteErrors.Count); M("failclosed." + tag + ".escaped", escaped); M("failclosed." + tag + ".live", siteLive);
        }
        Log("FAIL CLOSED entry points: " + sitesOk + " of " + sites.Length + " PASS (plus the BuildFrame entry above); RT stays Off for the rest of the session");
        M("failclosed.sites_ok", sitesOk);
        Grain(true);
    }


    // ================================================================== 2026-10-14 fix pass: the showcase shots (AD-6)
    /// <summary>Hit-id render: the glass pixels whose reflection ray hits a troffer lens (renderer "Lens"), and per glass
    /// pixel the renderer it hits (null = miss / none).</summary>
    static List<int> LensPixels(out Renderer[] hitRenderer)
    {
        var keepAcc = FrontRoomsGlassRTSystem.AllowAccumulation;
        FrontRoomsGlassRTSystem.AllowAccumulation = false;
        FrontRoomsGlassRTSystem.DebugMode = FrontRoomsGlassRTNative.DebugHitId;
        Render(rt); Render(rt); Sync(rt);
        var ids = ReadFloat(camState.ids != null ? camState.ids.rt : null);
        var order = FrontRoomsGlassRTSystem.SubmittedRenderers.ToList();
        FrontRoomsGlassRTSystem.DebugMode = FrontRoomsGlassRTNative.DebugNone;
        FrontRoomsGlassRTSystem.AllowAccumulation = keepAcc;
        var lens = new List<int>();
        hitRenderer = new Renderer[W * H];
        if (ids == null) return lens;
        for (var i = 0; i < ids.Length; i++)
        {
            var inst = (int)ids[i].r - 1;
            if (inst < 0 || inst >= order.Count || order[inst] == null) continue;
            hitRenderer[i] = order[inst];
            if (order[inst].name == "Lens") lens.Add(i);
        }
        return lens;
    }

    static float Lum(Color c) => .2126f * c.r + .7152f * c.g + .0722f * c.b;

    /// <summary>AD-6(1) + P1-B7: 45 and 60 deg off the normal with troffer rows in the reflection (the gaze on the upper
    /// pane so the mirrored rays reach the ceiling). Per angle the pose with the most reflected lens pixels wins. Off /
    /// mirror / High / Ultra / radiance frames; the reflected lens footprint and core against the planar mirror (AD-3);
    /// smudged lens pixels: still the live room (traced), blurred, not the zone cube (B7).</summary>
    static IEnumerator Oblique(WindowSetup w)
    {
        foreach (var deg in new[] { 45f, 60f })
        {
            var bestLens = -1; var best = new View();
            var found = false;
            foreach (var lat in new[] { 1f, -1f })
            {
                foreach (var dist in new[] { .9f, 1.2f, 1.5f })
                {
                    foreach (var up in new[] { .45f, .3f, .15f })
                    {
                        var feet = w.floorCross - (w.dir * Mathf.Cos(deg * Mathf.Deg2Rad) + w.along * lat * Mathf.Sin(deg * Mathf.Deg2Rad)) * dist;
                        if (!FreeStanding(feet) || !SeesPane(feet, w.pane)) continue;
                        var look = w.center + Vector3.up * up;
                        Place(feet, look); yield return 1; Resume(); Place(feet, look); SwapPanes(); yield return 1;
                        var n = LensPixels(out _).Count;
                        if (n > bestLens) { bestLens = n; best = new View { name = "o" + Mathf.RoundToInt(deg) + "_lamps", feet = feet, look = look, note = deg + " deg off the normal, " + F(dist) + " m, gaze " + F(up) + " m above the pane centre" }; }
                        if (n >= 1500) { found = true; break; }
                    }
                    if (found) break;
                }
                if (found) break;
            }
            if (bestLens < 0) { Log("OBLIQUE " + deg + ": no free-standing pose sees the pane"); continue; }
            var v = best;
            yield return GoTo(v);
            yield return WaitWarm(v.name);
            var t = cam.transform;
            var inc = Vector3.Angle(-(w.center - t.position).normalized, -w.dir);
            var lensPx = LensPixels(out _);
            var off = CaptureRT(v.name + "_off.png", FrontRoomsGlassRTQuality.Off);
            RenderMirror(w.pane, w.center, -w.dir, rt, true);
            SaveMirrorPng(v.name + "_ref.png");
            var high = CaptureRT(v.name + "_high.png", FrontRoomsGlassRTQuality.High, 4);
            Log("OBLIQUE " + v.name + " - " + v.note + " | incidence " + F(inc) + " deg | reflected lens px " + lensPx.Count + " | HIGH " + StatsLine());
            SaveRadiance(high.outPx, W, H, v.name + "_rt_high.png", high.depth);
            var ultra = CaptureRT(v.name + "_ultra.png", FrontRoomsGlassRTQuality.Ultra, 4);
            SaveRadiance(ultra.outPx, W, H, v.name + "_rt_ultra.png", ultra.depth);
            FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
            // the planar mirror's linear radiance (post off) on the same pixels: lens footprint and core (AD-3)
            RenderMirror(w.pane, w.center, -w.dir, rtHdr, false);
            var mirror = ReadFloat(rtHdr);
            FlipX(mirror, W, H);
            SaveRadiance(mirror.Select((c, i) => high.outPx[i].a > 1f ? new Color(c.r, c.g, c.b, 2f) : new Color(0, 0, 0, 0)).ToArray(), W, H, v.name + "_mirror_radiance.png");
            int fr = 0, fm = 0, cr = 0, cm = 0; double sr = 0, sm = 0, er = 0, em = 0;
            for (var i = 0; i < mirror.Length; i++)
            {
                if (high.outPx[i].a <= 1f) continue;
                float a = Lum(high.outPx[i]), b = Lum(mirror[i]);
                if (a > .8f) { fr++; er += a; }
                if (b > .8f) { fm++; em += b; }
                if (a > 3f) { cr++; sr += a; }
                if (b > 3f) { cm++; sm += b; }
            }
            // B7: lens pixels on smudged glass (prepass smoothness < SmudgeBlurBelow)
            var nrm = ReadFloat(camState.normal != null ? camState.normal.rt : null);
            int smudged = 0, smudgedTraced = 0;
            if (nrm != null)
                foreach (var i in lensPx)
                {
                    if (nrm[i].a >= FrontRoomsGlassRTSystem.SmudgeBlurBelow) continue;
                    smudged++;
                    if (high.outPx[i].a > 1f) smudgedTraced++;
                }
            Log("OBLIQUE " + v.name + " lenses (radiance, linear): footprint > 0.8: RT " + fr + " px vs mirror " + fm + " px (" + F(100.0 * (fr - fm) / Math.Max(1, fm)) + " %) | core > 3: RT mean " + F3(cr > 0 ? sr / cr : 0)
                + " (" + cr + " px) vs mirror " + F3(cm > 0 ? sm / cm : 0) + " (" + cm + " px) | lens energy RT/mirror " + F3(er / Math.Max(em, 1e-6))
                + " | B7 smudged lens px " + smudged + ", traced (live room, not the cube) " + smudgedTraced);
            M(v.name + ".lens_px", lensPx.Count); M(v.name + ".incidence_deg", inc); M(v.name + ".footprint_rt", fr); M(v.name + ".footprint_mirror", fm);
            M(v.name + ".core_rt", cr > 0 ? sr / cr : 0); M(v.name + ".core_mirror", cm > 0 ? sm / cm : 0); M(v.name + ".energy_ratio", er / Math.Max(em, 1e-6));
            M(v.name + ".smudged_lens_px", smudged); M(v.name + ".smudged_lens_traced", smudgedTraced);
            M(v.name + ".high.gpu_total_ms", high.stats.gpuMsTotal); M(v.name + ".high.shadow_rays", high.stats.shadowRays);
            Read8(rt);
        }
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
    }


    /// <summary>AD-2 shadow-ray variants on the parity view (primary rays through the hit shader vs the raster; post and
    /// SSAO off, linear): per-material mean and p95 relative error for each variant, one log line per variant.</summary>
    static IEnumerator ParityVariants(WindowSetup w, string key)
    {
        var v = w.views[1];
        yield return GoTo(v);
        Look(cam.transform.position - w.dir * 3f + Vector3.up * .3f);
        yield return 1;
        var data = cam.GetUniversalAdditionalCameraData();
        var post = data.renderPostProcessing;
        data.renderPostProcessing = false;
        var keepSsao = SetSsao(false);
        Time.captureDeltaTime = 1e-6f;
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
        Render(rtHdr); Render(rtHdr); Sync(rtHdr);
        var raster = ReadFloat(rtHdr);
        FrontRoomsGlassRTSystem.DebugMode = FrontRoomsGlassRTNative.DebugParity;
        Render(rtHdr); Render(rtHdr); Sync(rtHdr);
        var ids = ReadFloat(camState.ids != null ? camState.ids.rt : null);
        // name, quality, lamps, rays, extra, radius, knobs (rest full, no ranking, fixed pattern)
        var cfgs = new[]
        {
            new ParityCfg("old_High_4cam_hard_rest1", FrontRoomsGlassRTQuality.High, 4, 1, 0, 0f, new Vector3(1, 1, 0)),
            new ParityCfg("rank4_hard_rest1", FrontRoomsGlassRTQuality.High, 4, 1, 0, 0f, new Vector3(1, 0, 0)),
            new ParityCfg("rank4_hard_vbar", FrontRoomsGlassRTQuality.High, 4, 1, 0, 0f, Vector3.zero),
            new ParityCfg("rank4_disc1_hash_vbar", FrontRoomsGlassRTQuality.High, 4, 1, 0, .2f, Vector3.zero),
            new ParityCfg("rank4_ring2+2_hash_vbar", FrontRoomsGlassRTQuality.High, 4, 2, 2, .2f, Vector3.zero),
            new ParityCfg("rank4_ring2+2_fixed_vbar", FrontRoomsGlassRTQuality.High, 4, 2, 2, .2f, new Vector3(0, 0, 1)),
            new ParityCfg("rank6_hard_vbar", FrontRoomsGlassRTQuality.High, 6, 1, 0, 0f, Vector3.zero),
            new ParityCfg("old_Ultra_16cam_hard_rest1", FrontRoomsGlassRTQuality.Ultra, 8, 1, 0, 0f, new Vector3(1, 1, 0)),
            new ParityCfg("rank8_hard_vbar", FrontRoomsGlassRTQuality.Ultra, 8, 1, 0, 0f, Vector3.zero),
            new ParityCfg("rank8_ring2+2_hash_vbar", FrontRoomsGlassRTQuality.Ultra, 8, 2, 2, .2f, Vector3.zero),
            new ParityCfg("rank8_ring2+2_fixed_vbar", FrontRoomsGlassRTQuality.Ultra, 8, 2, 2, .2f, new Vector3(0, 0, 1)),
            new ParityCfg("rank8_ring4+4_hash_vbar", FrontRoomsGlassRTQuality.Ultra, 8, 4, 4, .2f, Vector3.zero),
        };
        var keep = new ShadowKnobs
        {
            highLamps = FrontRoomsGlassRTSystem.HighShadowLamps, ultraLamps = FrontRoomsGlassRTSystem.UltraShadowLamps,
            highRays = FrontRoomsGlassRTSystem.HighShadowRays, ultraRays = FrontRoomsGlassRTSystem.UltraShadowRays,
            highExtra = FrontRoomsGlassRTSystem.HighExtraRays, ultraExtra = FrontRoomsGlassRTSystem.UltraExtraRays, radius = FrontRoomsGlassRTSystem.ShadowLightRadius,
        };
        foreach (var c in cfgs)
        {
            FrontRoomsGlassRT.Quality = c.q;
            if (c.q == FrontRoomsGlassRTQuality.Ultra) { FrontRoomsGlassRTSystem.UltraShadowLamps = c.n.StartsWith("old_Ultra") ? 16 : c.lamps; FrontRoomsGlassRTSystem.UltraShadowRays = c.rays; FrontRoomsGlassRTSystem.UltraExtraRays = c.extra; }
            else { FrontRoomsGlassRTSystem.HighShadowLamps = c.lamps; FrontRoomsGlassRTSystem.HighShadowRays = c.rays; FrontRoomsGlassRTSystem.HighExtraRays = c.extra; }
            FrontRoomsGlassRTSystem.ShadowLightRadius = c.radius;
            FrontRoomsGlassRTSystem.HarnessShadowKnobs = c.knobs;
            Render(rtHdr); Render(rtHdr); Sync(rtHdr);
            var par = ReadFloat(camState.raw != null ? camState.raw.rt : null);
            var per = new Dictionary<int, List<double>>();
            double sumRT = 0, sumRa = 0; var hits = 0;
            for (var i = 0; i < raster.Length; i++)
            {
                if (ids == null || ids[i].r <= 0 || par == null) continue;
                var la = .2126 * raster[i].r + .7152 * raster[i].g + .0722 * raster[i].b;
                var lb = .2126 * par[i].r + .7152 * par[i].g + .0722 * par[i].b;
                sumRT += lb; sumRa += la; hits++;
                var mi = (int)ids[i].b;
                if (!per.TryGetValue(mi, out var l)) per[mi] = l = new List<double>();
                l.Add(Math.Abs(lb - la) / Math.Max(la, .02));
            }
            FrontRoomsGlassRT.TryGetStats(out var st);
            var sb = new StringBuilder();
            foreach (var kv in per.OrderByDescending(k => k.Value.Count).Take(5))
            {
                var l = kv.Value.OrderBy(x => x).ToList();
                var mname = kv.Key >= 0 && kv.Key < FrontRoomsGlassRTSystem.MaterialNames.Count ? FrontRoomsGlassRTSystem.MaterialNames[kv.Key] : "?";
                var shortName = mname.Split(' ')[0];
                sb.Append(" | " + shortName + " mean " + F3(l.Average()) + " p95 " + F3(l[(int)(l.Count * .95)]));
                M(key + "." + c.n + "." + shortName + ".mean", l.Average()); M(key + "." + c.n + "." + shortName + ".p95", l[(int)(l.Count * .95)]);
            }
            Log(key.ToUpperInvariant() + " " + c.n + ": luminance RT/raster " + F3(sumRT / Math.Max(sumRa, 1e-6)) + " | shadow rays " + st.shadowRays + sb);
            M(key + "." + c.n + ".luma_ratio", sumRT / Math.Max(sumRa, 1e-6)); M(key + "." + c.n + ".shadow_rays", st.shadowRays);
            yield return 1;
        }
        FrontRoomsGlassRTSystem.HighShadowLamps = keep.highLamps; FrontRoomsGlassRTSystem.UltraShadowLamps = keep.ultraLamps; FrontRoomsGlassRTSystem.HighShadowRays = keep.highRays;
        FrontRoomsGlassRTSystem.UltraShadowRays = keep.ultraRays; FrontRoomsGlassRTSystem.HighExtraRays = keep.highExtra; FrontRoomsGlassRTSystem.UltraExtraRays = keep.ultraExtra;
        FrontRoomsGlassRTSystem.ShadowLightRadius = keep.radius; FrontRoomsGlassRTSystem.HarnessShadowKnobs = Vector3.zero;
        FrontRoomsGlassRTSystem.DebugMode = FrontRoomsGlassRTNative.DebugNone;
        SetSsao(keepSsao);
        data.renderPostProcessing = post;
        Time.captureDeltaTime = 1f / 60f;
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
        yield return GoTo(v);
    }

    /// <summary>A static copy of the Relay body (its 16 rigid parts), registered Dynamic | Relay.</summary>
    static GameObject RelayCopy(List<int> handles)
    {
        var hunter = Get<Transform>(game, "hunter");
        if (hunter == null) return null;
        var body = new GameObject("RTVerify Relay body copy");
        foreach (var r in hunter.GetComponentsInChildren<MeshRenderer>(true))
        {
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;
            var activeUp = true;
            for (var tr = r.transform; tr != null && tr != hunter; tr = tr.parent) if (!tr.gameObject.activeSelf) { activeUp = false; break; }
            if (!activeUp || !r.enabled) continue;
            var part = new GameObject(r.name);
            part.transform.SetPositionAndRotation(hunter.InverseTransformPoint(r.transform.position), Quaternion.Inverse(hunter.rotation) * r.transform.rotation);
            part.transform.localScale = r.transform.lossyScale;
            part.transform.SetParent(body.transform, false);
            part.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            var pr = part.AddComponent<MeshRenderer>();
            pr.sharedMaterials = r.sharedMaterials;
            pr.shadowCastingMode = ShadowCastingMode.On;
        }
        foreach (var r in body.GetComponentsInChildren<MeshRenderer>()) handles.Add(FrontRoomsGlassRT.Register(r, FrontRoomsGlassRTFlags.Dynamic | FrontRoomsGlassRTFlags.Relay));
        return body;
    }

    /// <summary>AD-6(2): the Relay crossing 2 m behind the player at a dark-beyond window (the pane is a mirror there):
    /// Off / High / Ultra, the traced radiance, and a 30-frame crossing (every 10th frame saved). Logged: the final-frame
    /// change over the Relay's reflected body (AD-5) and the speck count inside it (AD-1).</summary>
    static IEnumerator DarkRelay(WindowSetup w)
    {
        var v = w.views[1];
        yield return GoTo(v);
        var box = Box("RTVerify dark room", w.center + w.dir * 3.1f, new Vector3(6f, 4f, 6f), Color.black, false);
        box.transform.rotation = Quaternion.LookRotation(w.dir);
        box.transform.position = w.center + w.dir * 3.1f;
        var handles = new List<int>();
        var body = RelayCopy(handles);
        if (body == null) { Log("DARK RELAY: no hunter"); Object.Destroy(box); yield break; }
        Vector3 Face(Vector3 at, Vector3 toward) { var d = toward - at; d.y = 0f; return d.sqrMagnitude > 1e-4f ? d.normalized : Vector3.forward; }
        var behind = v.feet - w.dir * 2.0f;
        void PlaceRelay(float s) { var p = behind + w.along * s; body.transform.SetPositionAndRotation(new Vector3(p.x, v.feet.y, p.z), Quaternion.LookRotation(Face(p, v.feet))); }
        PlaceRelay(0f);
        yield return 2;
        var off = CaptureRT("P1_darkrelay_off.png", FrontRoomsGlassRTQuality.Off, 2);
        var high = CaptureRT("P1_darkrelay_high.png", FrontRoomsGlassRTQuality.High, 3);
        SaveRadiance(high.outPx, W, H, "P1_darkrelay_rt.png", high.depth);
        Log("DARK RELAY High: " + StatsLine());
        var ultra = CaptureRT("P1_darkrelay_ultra.png", FrontRoomsGlassRTQuality.Ultra, 3);
        SaveRadiance(ultra.outPx, W, H, "P1_darkrelay_rt_ultra.png", ultra.depth);
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
        // the Relay's reflected pixels (hit id), its final-frame change and the specks inside it
        LensPixels(out var hitR);
        var relayParts = new HashSet<Renderer>(body.GetComponentsInChildren<MeshRenderer>());
        var onRelay = new bool[W * H];
        var nRelay = 0; double dY = 0;
        for (var i = 0; i < onRelay.Length; i++)
        {
            if (hitR[i] == null || !relayParts.Contains(hitR[i])) continue;
            onRelay[i] = true; nRelay++;
            dY += Luma8(high.frame, i) - Luma8(off.frame, i);
        }
        var specks = Specks(high.outPx, onRelay);
        Log("DARK RELAY: reflected Relay px " + nRelay + " | final frame change over it High - Off " + F(nRelay > 0 ? dY / nRelay : 0) + "/255 | isolated specks inside it " + specks.inside + " (of " + specks.checkedPx + " interior px checked)");
        M("darkrelay.relay_px", nRelay); M("darkrelay.dY", nRelay > 0 ? dY / nRelay : 0); M("darkrelay.specks", specks.inside);
        // fix round 33 (review 3 R3-T6): the figure above is over the hit-id px of the copy's parts; the Relay's silhouette
        // in the reflection is the px whose traced radiance changes when the copy is hidden (as art review 3 measured)
        yield return RelaySilhouette(body, high, off, "dark", "P1_darkrelay");
        // the crossing: 30 frames, 2.0 m across (6.7 cm per frame), every frame rendered, every 10th saved
        var lumFrames = new List<float[]>();
        for (var f = 0; f < 30; f++)
        {
            PlaceRelay(-1f + 2f * f / 29f);
            yield return 1;
            Render(rt); Sync(rt);
            var o = ReadFloat(Out);
            var lum = new float[o.Length];
            for (var i = 0; i < o.Length; i++) lum[i] = o[i].a > 1f ? Lum(o[i]) : -1f;
            lumFrames.Add(lum);
            if (f % 10 == 0 || f == 29) { Read8(rt); SavePng("P1_darkrelay_walk_f" + f.ToString("00") + ".png"); SaveRadiance(o, W, H, "P1_darkrelay_walk_rt_f" + f.ToString("00") + ".png"); }
        }
        var dcr = Crawl(lumFrames);
        double crawl = dcr.crawl, rel = dcr.rel; var cnt = dcr.count;
        Log("DARK RELAY crossing 30 frames: crawl at the 10 % highest-contrast reflection px " + F3(crawl) + " (relative " + F3(rel) + ", " + cnt + " px; the Relay moves, so this includes real motion)");
        M("darkrelay.crawl_rel", rel);
        // fix round 33 (R3-T2 / R3-T6): the same Relay 2 m behind the player in the LIT window (no dark room behind the pane)
        Object.Destroy(box);
        PlaceRelay(0f);
        yield return 2;
        var litOff = CaptureRT("P1_litrelay_off.png", FrontRoomsGlassRTQuality.Off, 2);
        var litHigh = CaptureRT("P1_litrelay_high.png", FrontRoomsGlassRTQuality.High, 3);
        SaveRadiance(litHigh.outPx, W, H, "P1_litrelay_rt.png", litHigh.depth);
        yield return RelaySilhouette(body, litHigh, litOff, "lit", "P1_litrelay");
        foreach (var h in handles) FrontRoomsGlassRT.Unregister(h);
        Object.Destroy(body);
        yield return 2;
    }

    /// <summary>Fix round 33 (R3-T6): the Relay copy's silhouette in the reflection = the traced px where the reflection
    /// rays hit the copy: the px whose radiance changes when only the copy's albedo changes (its materials swapped for
    /// magenta copies; same geometry and shadows, accumulation off, so nothing else changes). Logged: the final-frame
    /// change (8-bit luma) over that mask, with minus without the copy and with minus RT Off: mean and the 5th percentile
    /// (the darkest).</summary>
    static IEnumerator RelaySilhouette(GameObject body, Capture on0, Capture off, string key, string png)
    {
        var keepAcc = FrontRoomsGlassRTSystem.AllowAccumulation;
        FrontRoomsGlassRTSystem.AllowAccumulation = false;
        var parts = body.GetComponentsInChildren<MeshRenderer>();
        for (var k = 0; k < 6; k++) { Render(rt); yield return 1; }   // leave any accumulated history before the first capture
        // fix round 34 (art review 4 #7): the measured frame is saved (it was not, and the sheet showed the accumulated one)
        var on = CaptureRT(png + "_measured_high.png", FrontRoomsGlassRTQuality.High, 3);   // with the copy, accumulation off
        SaveRadiance(on.outPx, W, H, png + "_measured_rt.png", on.depth);
        var keep = new List<Material[]>();
        foreach (var r in parts)
        {
            keep.Add(r.sharedMaterials);
            var mats = new Material[r.sharedMaterials.Length];
            for (var k = 0; k < mats.Length; k++)
            {
                var src = r.sharedMaterials[k];
                var m = src != null ? new Material(src) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(1f, 0f, 1f, 1f));
                if (m.HasProperty("_Color")) m.SetColor("_Color", new Color(1f, 0f, 1f, 1f));
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", Texture2D.whiteTexture);
                mats[k] = m;
            }
            r.sharedMaterials = mats;
        }
        yield return 4;   // a material swap reaches the RT material table within 3 frames (E2)
        var magenta = CaptureRT(null, FrontRoomsGlassRTQuality.High, 3);
        for (var k = 0; k < parts.Length; k++) parts[k].sharedMaterials = keep[k];
        body.SetActive(false);
        yield return 4;
        var without = CaptureRT(png + "_norelay_high.png", FrontRoomsGlassRTQuality.High, 3);
        body.SetActive(true);
        yield return 4;
        FrontRoomsGlassRTSystem.AllowAccumulation = keepAcc;
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
        if (on.outPx == null || magenta.outPx == null || without.outPx == null) { Log("RELAY SILHOUETTE (" + key + "): no RT output"); yield break; }
        var mask = new bool[W * H];
        var dW = new List<double>(); var dOff = new List<double>();
        var noise = 0;
        for (var i = 0; i < mask.Length; i++)
        {
            if (on.outPx[i].a <= 1f) continue;
            var c0 = on.outPx[i]; var c1 = magenta.outPx[i];
            // magenta albedo: green drops, red and blue rise; the hit px of the copy change by far more than 2 %
            var dg = c0.g - c1.g; var drb = (c1.r + c1.b) - (c0.r + c0.b);
            // both at once: noise (shadow-ray stipple, a different accumulation state) moves the channels together
            if (dg <= Mathf.Max(.002f, .05f * c0.g) || drb <= Mathf.Max(.002f, .05f * (c0.r + c0.b))) { if (Mathf.Abs(Lum(c0) - Lum(c1)) > .02f) noise++; continue; }
            mask[i] = true;
            dW.Add(Luma8(on.frame, i) - Luma8(without.frame, i));
            dOff.Add(Luma8(on.frame, i) - Luma8(off.frame, i));
        }
        double Mean(List<double> l) => l.Count > 0 ? l.Average() : 0;
        double P(List<double> l, double q) => l.Count > 0 ? Pct(l, q) : 0;
        var t = new Texture2D(W, H, TextureFormat.RGB24, false);
        var px = new Color32[W * H];
        for (var i = 0; i < px.Length; i++) px[i] = mask[i] ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 255);
        t.SetPixels32(px); t.Apply();
        File.WriteAllBytes(Path.Combine(OutDir, png + "_silhouette.png"), t.EncodeToPNG());
        Object.DestroyImmediate(t);
        Log("RELAY SILHOUETTE (" + key + " window, the copy 2 m behind the player, v2 1.5 m head-on): " + dW.Count + " px where the reflection rays hit the copy (its albedo swapped to magenta: green drops AND red + blue rise in the traced radiance; other px that changed by > 0.02: " + noise + ")"
            + " | final frame with - without the Relay: mean " + F(Mean(dW)) + "/255, p5 " + F(P(dW, .05)) + ", p95 " + F(P(dW, .95))
            + " | with the Relay - RT Off: mean " + F(Mean(dOff)) + "/255, p5 " + F(P(dOff, .05)) + " | glass px " + on.stats.glassPixels + " | hit-id px of the copy (the round-2 figure's mask): see DARK RELAY");
        M("relaysil." + key + ".px", dW.Count); M("relaysil." + key + ".dY_mean", Mean(dW)); M("relaysil." + key + ".dY_p5", P(dW, .05));
        M("relaysil." + key + ".dOff_mean", Mean(dOff)); M("relaysil." + key + ".dOff_p5", P(dOff, .05)); M("relaysil." + key + ".noise_px", noise);
    }

    /// <summary>AD-1 acceptance: a speck = a reflection pixel more than 3 local MADs (and 0.02 linear) from the median
    /// of its 3x3 neighbourhood AND from both of its horizontal or both vertical neighbours, inside `within`.</summary>
    static SpeckCount Specks(Color[] o, bool[] within)
    {
        var inside = 0; var checkedPx = 0;
        var nb = new float[9];
        for (var y = 1; y < H - 1; y++)
        for (var x = 1; x < W - 1; x++)
        {
            var i = y * W + x;
            if (!within[i] || o[i].a <= 1f) continue;
            var all = true;
            for (var dy = -1; dy <= 1 && all; dy++) for (var dx = -1; dx <= 1 && all; dx++) all = within[(y + dy) * W + x + dx] && o[(y + dy) * W + x + dx].a > 1f;
            if (!all) continue;
            checkedPx++;
            var k = 0;
            for (var dy = -1; dy <= 1; dy++) for (var dx = -1; dx <= 1; dx++) nb[k++] = Lum(o[(y + dy) * W + x + dx]);
            var sorted = (float[])nb.Clone(); Array.Sort(sorted);
            var med = sorted[4];
            for (var j = 0; j < 9; j++) sorted[j] = Mathf.Abs(nb[j] - med);
            Array.Sort(sorted);
            var mad = sorted[4];
            var c = nb[4];
            var thr = Mathf.Max(3f * mad, .02f + .1f * med);
            if (Mathf.Abs(c - med) <= thr) continue;
            var h = Mathf.Abs(c - nb[3]) > thr && Mathf.Abs(c - nb[5]) > thr;
            var vv = Mathf.Abs(c - nb[1]) > thr && Mathf.Abs(c - nb[7]) > thr;
            if (h || vv) inside++;
        }
        return new SpeckCount { inside = inside, checkedPx = checkedPx };
    }

    /// <summary>AD-6(3) + AD-2: a moving shot with lamps in the reflection: v2, 60 frames at 2 cm/frame (every frame
    /// rendered, every 15th saved). Crawl (second difference) at the lens edges, on the reflected floor (lower 40 % of the
    /// glass) and on the raster's own floor (non-glass, lower 30 % of the frame); grain and dithering off.</summary>
    static IEnumerator Moving(WindowSetup w)
    {
        var v = w.views[1];
        yield return GoTo(v);
        yield return WaitWarm("moving");
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
        var lensMask = new bool[W * H];
        foreach (var i in LensPixels(out _)) lensMask[i] = true;
        Grain(false);
        var refl = new List<float[]>(); var floorR = new List<float[]>(); var rasterFloor = new List<float[]>(); var lensF = new List<float[]>();
        var glassRows = new List<int>();
        for (var f = 0; f < 60; f++)
        {
            Place(v.feet + w.along * (.02f * f), v.look + w.along * (.02f * f));
            yield return 1;
            Render(rt); Sync(rt);
            var o = ReadFloat(Out);
            var b8 = Read8(rt);
            if (f == 0)
            {
                // reflected floor = the lower 40 % of the glass rows, raster floor = non-glass in the lowest 30 % of rows
                var rows = new List<int>();
                for (var y = 0; y < H; y++) for (var x = 0; x < W; x += 16) if (o[y * W + x].a > 1f) { rows.Add(y); break; }
                if (rows.Count > 0) { glassRows.Add(rows.Min()); glassRows.Add(rows.Max()); }
            }
            int g0 = glassRows.Count > 0 ? glassRows[0] : 0, g1 = glassRows.Count > 0 ? glassRows[1] : H;
            var yFloorTop = g0 + (int)((g1 - g0) * .4f);   // texture rows go up: the lower part of the pane = low y
            var lum = new float[o.Length]; var fl = new float[o.Length]; var rf = new float[o.Length]; var lf = new float[o.Length];
            for (var i = 0; i < o.Length; i++)
            {
                var y = i / W;
                var isGlass = o[i].a > 1f;
                lum[i] = isGlass ? Lum(o[i]) : -1f;
                fl[i] = isGlass && y <= yFloorTop ? lum[i] : -1f;
                lf[i] = isGlass && lensMask[i] ? lum[i] : -1f;
                rf[i] = !isGlass && y < H * .3f ? .2126f * Mathf.GammaToLinearSpace(b8[3 * i] / 255f) + .7152f * Mathf.GammaToLinearSpace(b8[3 * i + 1] / 255f) + .0722f * Mathf.GammaToLinearSpace(b8[3 * i + 2] / 255f) : -1f;
            }
            refl.Add(lum); floorR.Add(fl); rasterFloor.Add(rf); lensF.Add(lf);
            if (f % 15 == 0 || f == 59) { Grain(true); Render(rt); Read8(rt); SavePng("P1_moving_f" + f.ToString("00") + ".png"); SaveRadiance(o, W, H, "P1_moving_rt_f" + f.ToString("00") + ".png"); Grain(false); }
        }
        Grain(true);
        // fix round 34 (art review 4 #4): the RT Off frame at each saved pose, after the move (Off has no history, and the
        // crawl above was measured before any quality change)
        foreach (var f in new[] { 0, 15, 30, 45, 59 })
        {
            Place(v.feet + w.along * (.02f * f), v.look + w.along * (.02f * f));
            yield return 1;
            CaptureRT("P1_moving_off_f" + f.ToString("00") + ".png", FrontRoomsGlassRTQuality.Off, 2);
        }
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
        var a = Crawl(refl); var b = Crawl(floorR); var c = Crawl(rasterFloor); var d = Crawl(lensF);
        Log("MOVING v2 60 frames at 2 cm/frame (High): relative crawl, reflection top-10 % contrast px " + F3(a.rel) + " (" + a.count + ") | reflected lens px " + F3(d.rel) + " (" + d.count + ")"
            + " | reflected floor " + F3(b.rel) + " (" + b.count + ") | RASTER floor " + F3(c.rel) + " (" + c.count + ")");
        M("moving.crawl_rel", a.rel); M("moving.crawl_rel_lens", d.rel); M("moving.crawl_rel_floor", b.rel); M("moving.crawl_rel_raster_floor", c.rel);
        yield return GoTo(v);
    }

    /// <summary>P1-B5: the back-surface image of the 6 mm pane, close (0.55-0.6 m) at 30-45 deg: High with the image on
    /// and off (accumulation off); the shift (dx, dy) and weight w that best explain on - off = w (shift(off) - off).</summary>
    static IEnumerator BackImage(WindowSetup w)
    {
        View v = new View(); var ok = false;
        foreach (var deg in new[] { 40f, 35f, 30f, 45f })
        {
            foreach (var lat in new[] { 1f, -1f })
            {
                var feet = w.floorCross - (w.dir * Mathf.Cos(deg * Mathf.Deg2Rad) + w.along * lat * Mathf.Sin(deg * Mathf.Deg2Rad)) * .6f;
                if (!FreeStanding(feet) || !SeesPane(feet, w.pane)) continue;
                v = new View { name = "b5_back" + Mathf.RoundToInt(deg), feet = feet, look = w.center + Vector3.up * .25f, note = deg + " deg, 0.6 m" };
                ok = true; break;
            }
            if (ok) break;
        }
        if (!ok) { Log("BACK IMAGE: no free-standing close oblique pose"); yield break; }
        yield return GoTo(v);
        yield return WaitWarm(v.name);
        var keepAcc = FrontRoomsGlassRTSystem.AllowAccumulation;
        FrontRoomsGlassRTSystem.AllowAccumulation = false;
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
        FrontRoomsGlassRTSystem.BackSurfaceImage = false;
        Render(rt); Render(rt); Sync(rt); var off = ReadFloat(Out);
        // fix round 34: a second image-off render; its difference from the first is the render-to-render noise (shadow-ray
        // stipple with accumulation off), which on - off carries too and no shift can explain
        Render(rt); Render(rt); Sync(rt); var off2 = ReadFloat(Out);
        FrontRoomsGlassRTSystem.BackSurfaceImage = true;
        var on = CaptureRT(v.name + "_high.png", FrontRoomsGlassRTQuality.High, 2);
        SaveRadiance(on.outPx, W, H, v.name + "_rt_on.png", on.depth);
        SaveRadiance(off, W, H, v.name + "_rt_off.png", on.depth);
        FrontRoomsGlassRTSystem.AllowAccumulation = keepAcc;
        // fix round 34 (art review 4 #4): the residual is now the best fit's own unexplained share of the on - off energy over
        // the SAME pixels (err / err0 at the best shift). Before, it was divided by the shift-(0,0) energy, which was never
        // set (at shift 0, g = 0 everywhere, den = 0 and the loop continued), so every run since r3a logged ~1e10.
        int bx = 0, by = 0; double bw = 0, bestErr = double.MaxValue, bestErr0 = 0, energyAll = 0, noiseAll = 0; var bestN = 0; var nAll = 0;
        var fracs = new List<double>();   // every shift's residual share: the best one must stand out from these
        for (var y = 10; y < H - 10; y += 2)
        for (var x = 10; x < W - 10; x += 2)
        {
            var i = y * W + x;
            if (on.outPx[i].a <= 1f || off[i].a <= 1f || off2[i].a <= 1f) continue;
            var d0 = Lum(on.outPx[i]) - Lum(off[i]);
            var dn = Lum(off2[i]) - Lum(off[i]);
            energyAll += d0 * d0; noiseAll += dn * dn; nAll++;
        }
        for (var dy = -8; dy <= 8; dy++)
        for (var dx = -8; dx <= 8; dx++)
        {
            if (dx == 0 && dy == 0) continue;   // g = 0: nothing to fit
            double num = 0, den = 0, err0 = 0; var n = 0;
            for (var y = 10; y < H - 10; y += 2)
            for (var x = 10; x < W - 10; x += 2)
            {
                var i = y * W + x; var j = (y + dy) * W + x + dx;
                if (on.outPx[i].a <= 1f || off[i].a <= 1f || off[j].a <= 1f || Mathf.Abs(off[j].a - off[i].a) > .05f) continue;
                double dOn = Lum(on.outPx[i]) - Lum(off[i]), g = Lum(off[j]) - Lum(off[i]);
                num += dOn * g; den += g * g; err0 += dOn * dOn; n++;
            }
            if (den <= 1e-9 || err0 <= 1e-12) continue;
            var wfit = num / den;
            var err = err0 - num * num / den;   // residual of the least-squares fit
            fracs.Add(err / err0);
            if (err < bestErr) { bestErr = err; bestErr0 = err0; bx = dx; by = dy; bw = wfit; bestN = n; }   // as before: the smallest residual
        }
        var resid = bestErr0 > 0 ? bestErr / bestErr0 : 1.0;
        fracs.Sort();
        var medFrac = fracs.Count > 0 ? fracs[fracs.Count / 2] : 1.0;
        // per-px energies (the best shift's mask and the all-glass mask differ by a few edge px)
        var eOnOff = nAll > 0 ? energyAll / nAll : 0; var eNoise = nAll > 0 ? noiseAll / nAll : 0;
        var eResid = bestN > 0 ? bestErr / bestN : 0; var eBest0 = bestN > 0 ? bestErr0 / bestN : 0;
        var aboveNoise = eBest0 - eNoise;
        var explainedAboveNoise = aboveNoise > 1e-12 ? (eBest0 - eResid) / aboveNoise : 0;
        var cam0 = cam.transform.position;
        var inc = Vector3.Angle(-(w.center - cam0).normalized, -w.dir);
        Log("BACK IMAGE " + v.name + " (incidence " + F(inc) + " deg, eye " + F((w.center - cam0).magnitude) + " m from the pane centre): best shift (" + bx + ", " + by + ") px = " + F(Mathf.Sqrt(bx * bx + by * by))
            + " px at 1080p (" + F(Mathf.Sqrt(bx * bx + by * by) * 1440f / 1080f) + " px at 1440p), weight " + F3(bw) + " | fit over " + bestN + " px: residual " + F3(resid) + " of the on - off energy (explained " + F3(1.0 - resid) + "; median over all " + fracs.Count + " shifts tried " + F3(medFrac) + ")"
            + " | on - off energy " + energyAll.ToString("0.###E+0", CultureInfo.InvariantCulture) + " over " + nAll + " glass px (rms " + (Math.Sqrt(eOnOff)).ToString("0.#####", CultureInfo.InvariantCulture) + " linear)"
            + " | render noise (image off, rendered twice) rms " + (Math.Sqrt(eNoise)).ToString("0.#####", CultureInfo.InvariantCulture) + " = " + F3(eOnOff > 0 ? eNoise / eOnOff : 0) + " of the on - off energy"
            + " | share of the on - off energy ABOVE the noise that the shifted copy explains: " + F3(explainedAboveNoise));
        M("backimage.shift_px", Mathf.Sqrt(bx * bx + by * by)); M("backimage.weight", bw); M("backimage.incidence", inc);
        M("backimage.residual", resid); M("backimage.residual_median_other", medFrac); M("backimage.fit_px", bestN); M("backimage.rms_on_off", Math.Sqrt(eOnOff)); M("backimage.rms_noise", Math.Sqrt(eNoise));
        M("backimage.noise_share", eOnOff > 0 ? eNoise / eOnOff : 0); M("backimage.explained_above_noise", explainedAboveNoise);
        yield return GoTo(w.views[1]);
    }

    /// <summary>AD-6(6): the camera with HDR off (an LDR display target): RT still traces and the glass still takes it.</summary>
    static IEnumerator HdrOff(WindowSetup w)
    {
        var v = w.views[1];
        yield return GoTo(v);
        var keep = cam.allowHDR;
        cam.allowHDR = false;
        yield return 1;
        var off = CaptureRT("P1_hdroff_off.png", FrontRoomsGlassRTQuality.Off, 2);
        var high = CaptureRT("P1_hdroff_high.png", FrontRoomsGlassRTQuality.High, 3);
        var traced = high.outPx != null ? Count(Mask(high.outPx, x => x.a > 1f)) : 0;
        var changed = Count(Diff8(high.frame, off.frame, 2));
        Log("HDR OFF: camera allowHDR false | traced px " + traced + " | weight " + F(camState.lastWeight) + " | final px changed by RT (>= 2/255) " + changed + " | " + StatsLine());
        M("hdroff.traced_px", traced); M("hdroff.changed_px", changed);
        cam.allowHDR = keep;
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
        yield return 1;
    }

    /// <summary>AD-6(6): hit-shading parity in the Office (drywall, carpet tile, door veneer, cove base, kit props).</summary>
    static IEnumerator OfficeParity()
    {
        var office = new List<WindowSetup>();
        yield return FindWindow(ZoneTheme.Office, 2, office);
        if (office.Count == 0) { Log("OFFICE PARITY: no usable window near the start"); yield break; }
        FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.Office, 0f);
        yield return GoTo(office[0].views[1]);
        yield return WaitWarm("office parity");
        yield return Parity(office[0], "P1_office_parity", "officeparity");
        FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.Level0, 0f);
    }

    // ---- Office window
    static IEnumerator Office()
    {
        var office = new List<WindowSetup>();
        yield return FindWindow(ZoneTheme.Office, 2, office);
        if (office.Count == 0) { Log("OFFICE: no usable window near the start"); yield break; }
        var w = office[0];
        FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.Office, 0f);
        foreach (var vi in new[] { 1, 2 })
        {
            if (vi >= w.views.Length) continue;
            var v = w.views[vi];
            yield return GoTo(v);
            yield return WaitWarm("office " + v.name);
            CaptureRT("P1_office_" + v.name + "_off.png", FrontRoomsGlassRTQuality.Off, 2);
            RenderMirror(w.pane, w.center, -w.dir, rt, true); SaveMirrorPng("P1_office_" + v.name + "_ref.png");
            var h = CaptureRT("P1_office_" + v.name + "_high.png", FrontRoomsGlassRTQuality.High, 3);
            SaveRadiance(h.outPx, W, H, "P1_office_" + v.name + "_rt_high.png", h.depth);
            CaptureRT("P1_office_" + v.name + "_ultra.png", FrontRoomsGlassRTQuality.Ultra, 3);
            Log("OFFICE " + v.name + ": " + StatsLine());
        }
        FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.Level0, 0f);
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
    }

    // ---- P0-A7: a scripted walk with chunk builds and drops: blank RT frames, main-thread and encode p99
    static List<Vector3> WalkPath(Vector3 start, int cells)
    {
        var s0 = map.CellOf(start);
        var prev = new Dictionary<GridCoord, GridCoord> { [s0] = s0 };
        var dist = new Dictionary<GridCoord, int> { [s0] = 0 };
        var q = new Queue<GridCoord>();
        q.Enqueue(s0);
        var far = s0;
        while (q.Count > 0 && dist.Count < 6000)
        {
            var c = q.Dequeue();
            if (dist[c] > dist[far]) far = c;
            if (dist[c] >= cells) break;
            foreach (var dlt in new[] { new GridCoord(1, 0), new GridCoord(-1, 0), new GridCoord(0, 1), new GridCoord(0, -1) })
            {
                var n = c + dlt;
                if (dist.ContainsKey(n) || map.InStartArea(n)) continue;
                var e = map.Cache.Edge(c, n);
                if (e == EdgeKind.Wall || e == EdgeKind.Window) continue;
                dist[n] = dist[c] + 1; prev[n] = c; q.Enqueue(n);
            }
        }
        var path = new List<Vector3>();
        for (var c = far; ; c = prev[c]) { var p = map.CellCenter(c); path.Add(new Vector3(p.x, start.y, p.z)); if (c.Equals(s0)) break; }
        path.Reverse();
        return path;
    }

    static double Pct(List<double> l, double q) { var o = new List<double>(l); o.Sort(); return o[Math.Min(o.Count - 1, (int)(o.Count * q))]; }

    /// <summary>Lit non-directional lights within the lamp radius of the camera: what the raster lights this frame (the
    /// reflection should carry the same lamps, up to the 128 cap).</summary>
    static int RasterLamps()
    {
        var p = cam.transform.position; var r2 = FrontRoomsGlassRTSystem.StreamGateRadius * FrontRoomsGlassRTSystem.StreamGateRadius; var n = 0;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.isActiveAndEnabled && l.type != LightType.Directional && l.intensity > 0f && (l.transform.position - p).sqrMagnitude <= r2) n++;
        return n;
    }

    /// <summary>Fix round 32 (review 2 engineering #6): the chunks whose first scan finished since `fromFrame`: frames from
    /// the root's appearance to full registration (renderers + lights), the camera distance at birth; one TSV row each.</summary>
    static void ChunkReport(string tag, int fromFrame, string tsv, string key)
    {
        var rows = FrontRoomsGlassRTSystem.ChunkTimings.Where(t => t.born >= fromFrame).ToList();
        var sb = new StringBuilder("chunk\tborn_frame\tregistered_frame\tframes\trenderers\tlights\tdistance_at_birth_m\tdistance_when_done_m\tburst\n");
        foreach (var t in rows)
            sb.Append(t.name.Replace('\t', ' ')).Append('\t').Append(t.born).Append('\t').Append(t.done).Append('\t').Append(t.done - t.born).Append('\t').Append(t.renderers).Append('\t').Append(t.lights)
              .Append('\t').Append(F(t.bornDistance > 1e6f ? -1f : t.bornDistance)).Append('\t').Append(F(t.doneDistance > 1e6f ? -1f : t.doneDistance)).Append('\t').Append(t.burst ? 1 : 0).Append('\n');
        File.WriteAllText(Path.Combine(OutDir, tsv), sb.ToString());
        var fr = rows.Select(t => (double)(t.done - t.born)).ToList();
        var near = rows.Where(t => t.bornDistance < FrontRoomsGlassRTSystem.StreamGateRadius).Select(t => (double)(t.done - t.born)).ToList();
        Log(tag + " frames to registered per chunk: " + rows.Count + " chunks | all p50 " + F(fr.Count > 0 ? Pct(fr, .5) : 0) + " p95 " + F(fr.Count > 0 ? Pct(fr, .95) : 0) + " max " + F(fr.Count > 0 ? fr.Max() : 0)
            + " | born within " + F(FrontRoomsGlassRTSystem.StreamGateRadius) + " m of the camera: " + near.Count + (near.Count > 0 ? " (p50 " + F(Pct(near, .5)) + ", max " + F(near.Max()) + " frames)" : "")
            + " | nearest birth " + F(rows.Count > 0 ? rows.Min(t => t.bornDistance) : -1f) + " m | still unregistered now: " + FrontRoomsGlassRTSystem.NearBacklog + " near");
        M(key + ".chunks_registered", rows.Count);
        if (fr.Count > 0) { M(key + ".chunk_frames_p50", Pct(fr, .5)); M(key + ".chunk_frames_p95", Pct(fr, .95)); M(key + ".chunk_frames_max", fr.Max()); }
        M(key + ".chunks_born_near", near.Count);
        if (near.Count > 0) M(key + ".chunk_frames_near_max", near.Max());
    }

    /// <summary>Fix round 32 (review 2 art #1 (c) and engineering #6): stream-in after a jump. The player is teleported to a
    /// window that is not built or not registered (the dead-lamp window (304,207) as in the art review's G14VArtR2b, a far
    /// Level 0 window ~120 m away, then back to the test window), and the game camera renders EVERY frame from the jump on
    /// (no Flush, no Settle: as the game does at a run start, a C22 floor change or a chunk rebuild). Per frame: weight,
    /// glass / miss px of that frame (synced), lamps, near backlog, main-thread CPU. Pass: no shown frame (weight > 0) misses
    /// more than 0.5 % of its glass px; the reflection's lamps are complete when it shows.</summary>
    static IEnumerator Jump(WindowSetup w)
    {
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
        var sill = (ModuleUnits.WindowSill + ModuleUnits.WindowTop) * .5f;
        var targets = new List<View>();
        {
            var a = new GridCoord(304, 207); var b = new GridCoord(305, 207);
            var cross = map.CrossingPoint(a, b); var d = map.CellCenter(b) - map.CellCenter(a); d.y = 0f; d.Normalize();
            targets.Add(new View { name = "J1_deadlamp_304_207", feet = cross - d * 1.5f, look = cross + Vector3.up * sill, note = "the art review's stream-in window, 1.5 m head-on" });
        }
        // two far jumps into map the run has not built (a run start / C22 floor change in miniature): 45 cells (135 m) west
        // and 45 cells north of the test window, the nearest Level 0 window there, 1.5 m head-on
        foreach (var off in new[] { new GridCoord(-45, 0), new GridCoord(0, 45) })
        {
            var origin = w.a + off;
            var far = new View(); var found = false;
            foreach (var c in CellsAround(origin, 20))
            {
                foreach (var st in new[] { new GridCoord(1, 0), new GridCoord(0, 1) })
                {
                    var n = c + st;
                    if (map.Cache.Edge(c, n) != EdgeKind.Window) continue;
                    if (map.Cache.ZoneOf(c).theme != ZoneTheme.Level0 || map.Cache.ZoneOf(n).theme != ZoneTheme.Level0) continue;
                    var cross = map.CrossingPoint(c, n); var d = map.CellCenter(n) - map.CellCenter(c); d.y = 0f; d.Normalize();
                    far = new View { name = "J" + (targets.Count + 1) + "_far_" + c.x + "_" + c.y, feet = cross - d * 1.5f, look = cross + Vector3.up * sill, note = "a Level 0 window " + F((cross - w.floorCross).magnitude) + " m from the test window, map not built there" };
                    found = true;
                    break;
                }
                if (found) break;
            }
            if (found) targets.Add(far); else Log("JUMP: no Level 0 window found near " + origin);
        }
        var summary = new StringBuilder();
        foreach (var v in targets)
        {
            var from = Time.frameCount;
            var holds0 = FrontRoomsGlassRTSystem.GateHolds; var burst0 = FrontRoomsGlassRTSystem.BurstFrames; var timeouts0 = FrontRoomsGlassRTSystem.GateTimeouts; var births0 = FrontRoomsGlassRTSystem.NearBirths;
            var tsv = new StringBuilder("frame\tweight\tglass_px\tmiss_px\tmiss_pct\tlamps\tregistered\tchunks\tnear_backlog\tmeshes_pending\tmain_cpu_ms\tnear_unbuilt\n");
            Place(v.feet, v.look);
            Resume();
            int firstShown = -1, fullShown = -1, shownHoles = 0, lampsAtShown = -1, rasterAtShown = -1, reholds = 0; var prevW = 0f;
            var shownMissMax = 0.0; var cpuHold = new List<double>(); var cpuAll = new List<double>();
            var maxGlass = 0;
            for (var f = 0; f <= 360; f++)
            {
                Place(v.feet, v.look);
                yield return 1;
                Render(rt); Sync(rt);
                FrontRoomsGlassRT.TryGetStats(out var s);
                var wgt = camState.lastWeight;
                var mf = s.glassPixels > 0 ? (double)s.missPixels / s.glassPixels : 0.0;
                maxGlass = Math.Max(maxGlass, s.glassPixels);
                cpuAll.Add(s.mainThreadCpuMs);
                if (wgt <= 0f) cpuHold.Add(s.mainThreadCpuMs);
                if (wgt > 0f && s.glassPixels > 0)
                {
                    if (firstShown < 0) { firstShown = f; lampsAtShown = s.lamps; rasterAtShown = RasterLamps(); }
                    shownMissMax = Math.Max(shownMissMax, mf);
                    if (mf > FrontRoomsGlassRTSystem.GateMissFraction) shownHoles++;
                }
                if (fullShown < 0 && wgt >= 1f) fullShown = f;
                if (prevW > 0f && wgt <= 0f) reholds++;
                prevW = wgt;
                tsv.Append(f).Append('\t').Append(F3(wgt)).Append('\t').Append(s.glassPixels).Append('\t').Append(s.missPixels).Append('\t').Append(F3(100.0 * mf)).Append('\t').Append(s.lamps)
                   .Append('\t').Append(FrontRoomsGlassRTSystem.RegisteredInstances).Append('\t').Append(FrontRoomsGlassRTSystem.KnownChunks).Append('\t').Append(FrontRoomsGlassRTSystem.NearBacklog)
                   .Append('\t').Append(s.meshesPending).Append('\t').Append(F3(s.mainThreadCpuMs)).Append('\t').Append(FrontRoomsGlassRTSystem.NearUnbuilt).Append('\n');
                if (f == 5 || f == 30 || f == 150 || f == 300 || f == fullShown || f == firstShown)
                {
                    var o = ReadFloat(Out);
                    Read8(rt); SavePng("P1_" + v.name + "_f" + f.ToString("000") + ".png");
                    SaveRadiance(o, W, H, "P1_" + v.name + "_rt_f" + f.ToString("000") + ".png", ReadFloat(GlassDepth));
                }
                if (f % 30 == 0)
                    Log("JUMP " + v.name + " +" + f + " | weight " + F3(wgt) + " | glass px " + s.glassPixels + " | miss " + s.missPixels + " (" + F(100.0 * mf) + " %) | lamps " + s.lamps
                        + " | registered " + FrontRoomsGlassRTSystem.RegisteredInstances + " in " + FrontRoomsGlassRTSystem.KnownChunks + " chunks | near backlog " + FrontRoomsGlassRTSystem.NearBacklog
                        + " | meshes pending " + s.meshesPending + " | main CPU " + F3(s.mainThreadCpuMs) + " ms | gate " + FrontRoomsGlassRTSystem.GateLine(camState));
            }
            FrontRoomsGlassRT.TryGetStats(out var e);
            File.WriteAllText(Path.Combine(OutDir, "jump_" + v.name + ".tsv"), tsv.ToString());
            Log("JUMP " + v.name + " (" + v.note + "): first shown frame +" + firstShown + " | weight 1 at +" + fullShown + " | shown frames missing > " + F(100.0 * FrontRoomsGlassRTSystem.GateMissFraction) + " % of the glass "
                + shownHoles + " (max " + F(100.0 * shownMissMax) + " %) | lamps when first shown " + lampsAtShown + " (lit raster lights within " + F(FrontRoomsGlassRTSystem.StreamGateRadius) + " m: " + rasterAtShown + "), at +360 " + e.lamps + " (raster " + RasterLamps() + ") | near births " + (FrontRoomsGlassRTSystem.NearBirths - births0) + " | re-holds after showing " + reholds + " | holds " + (FrontRoomsGlassRTSystem.GateHolds - holds0)
                + ", timeouts " + (FrontRoomsGlassRTSystem.GateTimeouts - timeouts0) + ", burst frames " + (FrontRoomsGlassRTSystem.BurstFrames - burst0)
                + " | main CPU while held p95 " + F3(cpuHold.Count > 0 ? Pct(cpuHold, .95) : 0) + " max " + F3(cpuHold.Count > 0 ? cpuHold.Max() : 0) + " ms (" + cpuHold.Count + " frames), all p50 " + F3(Pct(cpuAll, .5))
                + " | max glass px " + maxGlass + " | gate " + FrontRoomsGlassRTSystem.GateLine(camState));
            ChunkReport("JUMP " + v.name, from, "jump_" + v.name + "_chunks.tsv", "jump." + v.name);
            M("jump." + v.name + ".first_shown", firstShown); M("jump." + v.name + ".full_shown", fullShown); M("jump." + v.name + ".shown_with_holes", shownHoles);
            M("jump." + v.name + ".shown_miss_max_pct", 100.0 * shownMissMax); M("jump." + v.name + ".lamps_first_shown", lampsAtShown); M("jump." + v.name + ".lamps_end", e.lamps);
            M("jump." + v.name + ".raster_lamps_first_shown", rasterAtShown); M("jump." + v.name + ".reholds", reholds);
            M("jump." + v.name + ".timeouts", FrontRoomsGlassRTSystem.GateTimeouts - timeouts0); M("jump." + v.name + ".burst_frames", FrontRoomsGlassRTSystem.BurstFrames - burst0);
            M("jump." + v.name + ".cpu_hold_p95", cpuHold.Count > 0 ? Pct(cpuHold, .95) : 0); M("jump." + v.name + ".cpu_hold_max", cpuHold.Count > 0 ? cpuHold.Max() : 0);
            summary.Append(" | ").Append(v.name).Append(": shown +").Append(firstShown).Append(", holes ").Append(shownHoles).Append(", lamps ").Append(lampsAtShown).Append("/").Append(e.lamps);
        }
        Log("JUMP summary" + summary);
        yield return GoTo(w.views[1]);
        yield return WaitWarm("after jump");
    }

    // ------------------------------------------------------------------ fix round 33 (review 3)
    const int DefaultBudgetTris = 10000, DefaultBudgetBuilds = 8;   // FRGlassRTScene::kStreamTrianglesPerFrame / kStreamBuildsPerFrame

    static string Budget() => (int)FrontRoomsGlassRTNative.Get(FrontRoomsGlassRTNative.Stat.StreamTriBudget) + " tris / "
        + (int)FrontRoomsGlassRTNative.Get(FrontRoomsGlassRTNative.Stat.StreamBuildBudget) + " builds";
    static bool BudgetIsDefault() => (int)FrontRoomsGlassRTNative.Get(FrontRoomsGlassRTNative.Stat.StreamTriBudget) == DefaultBudgetTris
        && (int)FrontRoomsGlassRTNative.Get(FrontRoomsGlassRTNative.Stat.StreamBuildBudget) == DefaultBudgetBuilds;
    static string Gates() => "gates " + FrontRoomsGlassRTSystem.GateCount + " / live cameras " + FrontRoomsGlassRTSystem.LiveCameras + " / orphan gates "
        + FrontRoomsGlassRTSystem.OrphanGates + " | burst " + FrontRoomsGlassRTSystem.BlasBurst + " | native stream budget " + Budget();

    /// <summary>Map squares the map has built (MapWorld.built) within `radius` m of `at` whose chunk root G14 does not
    /// count as built (FrontRoomsGlassRTSystem.ChunkCoordRefs == 0): must be 0.</summary>
    static int BuiltButUncounted(Vector3 at, float radius, StringBuilder list)
    {
        var built = Get(map, "built") as IDictionary;
        if (built == null) return -1;
        var n = 0;
        var lp = at - map.transform.position;
        foreach (var k in built.Keys)
        {
            var c = (GridCoord)k;
            var dx = Mathf.Max(Mathf.Max(c.x * MapGrid.ChunkSize - lp.x, lp.x - (c.x + 1) * MapGrid.ChunkSize), 0f);
            var dz = Mathf.Max(Mathf.Max(c.y * MapGrid.ChunkSize - lp.z, lp.z - (c.y + 1) * MapGrid.ChunkSize), 0f);
            if (dx * dx + dz * dz > radius * radius) continue;
            if (FrontRoomsGlassRTSystem.ChunkCoordRefs(c.x, c.y) > 0) continue;
            n++;
            if (list != null) list.Append(' ').Append(c);
        }
        return n;
    }

    /// <summary>The map's own key take (MapWorld.TakeKey: off the chunk's list, KeyPickupStarted, the presenter; a presented
    /// key moves to the map root), through reflection, for the key `keyGo` lying in chunk `chunkCoord`.</summary>
    static bool TakeKeyNow(GridCoord chunkCoord, GameObject keyGo)
    {
        var built = Get(map, "built") as IDictionary;
        if (built == null || !built.Contains(chunkCoord)) return false;
        var chunk = built[chunkCoord];
        var keys = Get(chunk, "keys") as IList;
        if (keys == null) return false;
        for (var i = 0; i < keys.Count; i++)
        {
            var e = keys[i];
            var go = e.GetType().GetField("Item1").GetValue(e) as GameObject;
            if (go != keyGo) continue;
            map.GetType().GetMethod("TakeKey", BF).Invoke(map, new object[] { chunk, i });
            return true;
        }
        return false;
    }

    /// <summary>R3-B2: a presented key pickup at an intact Level 0 window (the editor placeholder key, MapWorld.TakeKey and the
    /// door shot's presenter), then MapWorld.RebuildChunk of a built neighbour of the window's chunk (Level Designer
    /// ReplaceModule's path). Asserts: the pickup makes no near birth, no gate hold and no frame below weight 1 (no drop to
    /// the zone cube); the key at the map root is not a chunk; after the key is destroyed and after the rebuild no built
    /// square within 60 m is counted as unbuilt and NearUnbuilt is 0.</summary>
    static IEnumerator KeyPickup(WindowSetup w0)
    {
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
        var list = new List<WindowSetup>();
        yield return FindWindow(ZoneTheme.Level0, 2, list);
        if (list.Count == 0) { Log("KEY PICKUP: no intact Level 0 window"); M("keypickup.ran", 0); yield break; }
        var w = list[0];
        var v = w.views[1];
        yield return GoTo(v);
        yield return WaitWarm("key pickup");
        // fix round 34 (art review 4 #5): a dark room beyond the pane, so the final frame shows the reflection at full
        // strength (in a lit window it is about 8 % strong and a drop to the zone cube cannot be seen); the RT Off frame of
        // this pose shows what a drop to the zone cube looks like. The box is not registered with G14 (raster only).
        var darkBox = Box("RTVerify dark room", w.center + w.dir * 3.1f, new Vector3(6f, 4f, 6f), Color.black, false);
        darkBox.transform.rotation = Quaternion.LookRotation(w.dir);
        darkBox.transform.position = w.center + w.dir * 3.1f;
        yield return 1;
        CaptureRT("P1_keypickup_dark_off.png", FrontRoomsGlassRTQuality.Off, 2);
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
        yield return WaitWarm("key pickup, dark beyond");
        var pre = CaptureRT("P1_keypickup_dark_high_before.png", FrontRoomsGlassRTQuality.High, 3);
        SaveRadiance(pre.outPx, W, H, "P1_keypickup_dark_rt_before.png", pre.depth);
        GridCoord zone, kchunk;
        var key = map.NearestKeyForTools(w.floorCross, out zone, out kchunk);
        if (key == null) { Log("KEY PICKUP: no key lying in a built chunk"); M("keypickup.ran", 0); yield break; }
        var keyGo = key.gameObject;
        var keyDist = (key.position - cam.transform.position).magnitude;
        FrontRoomsMapWorld.ShowPlaceholderKey = true;   // the editor's placeholder key: the door shot presents the cube key
        var lp = key.position - map.transform.position;
        var oldRound = new GridCoord(Mathf.RoundToInt(lp.x / MapGrid.ChunkSize), Mathf.RoundToInt(lp.z / MapGrid.ChunkSize));
        var births0 = FrontRoomsGlassRTSystem.NearBirths; var holds0 = FrontRoomsGlassRTSystem.GateHolds; var nonChunk0 = FrontRoomsGlassRTSystem.NonChunkRoots;
        var chunks0 = FrontRoomsGlassRTSystem.KnownChunks; var mapChildren0 = map.transform.childCount;
        Log("KEY PICKUP at " + w.pane.name + " (v2, 1.5 m): key " + keyGo.name + " in chunk " + kchunk + ", " + F(keyDist) + " m from the camera | presenter " + (map.KeyPresenter != null)
            + " | placeholder key on | the square the round-2 code would have dropped with the key: " + oldRound + " (refs " + FrontRoomsGlassRTSystem.ChunkCoordRefs(oldRound.x, oldRound.y) + ") | " + Gates() + " | gate " + FrontRoomsGlassRTSystem.GateLine(camState));
        var took = TakeKeyNow(kchunk, keyGo);
        var tsv = new StringBuilder("frame\tweight\tglass_px\tmiss_px\tnear_births\tholds\tchunks\tnon_chunk_roots\tnear_unbuilt\tkey_alive\tkey_parent\n");
        float minWeight = 1f; var framesBelow1 = 0; var destroyedAt = -1; var atRootFrames = 0; var maxNearUnbuilt = 0; var glassFrames = 0;
        var missFrames = 0; double maxMissShare = 0; var saved = new StringBuilder("frame\twhy\tweight\tglass_px\tmiss_px\n");
        for (var f = 0; f < 900; f++)
        {
            Place(v.feet, v.look);
            yield return 1;
            Render(rt); Sync(rt);
            FrontRoomsGlassRT.TryGetStats(out var s);
            var wgt = camState.lastWeight;
            if (s.glassPixels > 0)
            {
                glassFrames++; minWeight = Mathf.Min(minWeight, wgt); if (wgt < 1f) framesBelow1++;
                if (s.missPixels > 0) missFrames++;
                maxMissShare = Math.Max(maxMissShare, s.missPixels / (double)s.glassPixels);
            }
            var alive = keyGo != null;
            var atRoot = alive && keyGo.transform.parent == map.transform;
            if (atRoot) atRootFrames++;
            var justDestroyed = !alive && destroyedAt < 0;
            if (justDestroyed) destroyedAt = f;
            maxNearUnbuilt = Math.Max(maxNearUnbuilt, FrontRoomsGlassRTSystem.NearUnbuilt);
            tsv.Append(f).Append('\t').Append(F3(wgt)).Append('\t').Append(s.glassPixels).Append('\t').Append(s.missPixels).Append('\t').Append(FrontRoomsGlassRTSystem.NearBirths - births0)
               .Append('\t').Append(FrontRoomsGlassRTSystem.GateHolds - holds0).Append('\t').Append(FrontRoomsGlassRTSystem.KnownChunks).Append('\t').Append(FrontRoomsGlassRTSystem.NonChunkRoots - nonChunk0)
               .Append('\t').Append(FrontRoomsGlassRTSystem.NearUnbuilt).Append('\t').Append(alive ? 1 : 0).Append('\t').Append(alive ? (keyGo.transform.parent == map.transform ? "map root" : (keyGo.transform.parent != null ? keyGo.transform.parent.name : "none")) : "-").Append('\n');
            // saved: +1, +30, the first frame with the key at the map root and 8 frames later (round 2 dropped the reflection
            // to the zone cube there), and the frame the key is gone; each with its traced radiance
            var why = f == 1 ? "plus1" : f == 30 ? "plus30" : atRoot && atRootFrames == 1 ? "key_at_root" : atRoot && atRootFrames == 9 ? "key_at_root_plus8" : justDestroyed ? "key_destroyed" : null;
            if (why != null)
            {
                Read8(rt); SavePng("P1_keypickup_f" + f.ToString("000") + ".png");
                SaveRadiance(ReadFloat(Out), W, H, "P1_keypickup_rt_f" + f.ToString("000") + ".png", ReadFloat(GlassDepth));
                saved.Append(f).Append('\t').Append(why).Append('\t').Append(F3(wgt)).Append('\t').Append(s.glassPixels).Append('\t').Append(s.missPixels).Append('\n');
            }
            if (destroyedAt >= 0 && f - destroyedAt >= 60) break;
        }
        File.WriteAllText(Path.Combine(OutDir, "keypickup.tsv"), tsv.ToString());
        File.WriteAllText(Path.Combine(OutDir, "keypickup_saved.tsv"), saved.ToString());
        Object.Destroy(darkBox);
        yield return 1;
        var uncounted = new StringBuilder();
        var nUncounted = BuiltButUncounted(cam.transform.position, 60f, uncounted);
        var births = FrontRoomsGlassRTSystem.NearBirths - births0; var holds = FrontRoomsGlassRTSystem.GateHolds - holds0;
        var ok = took && atRootFrames > 0 && destroyedAt >= 0 && births == 0 && holds == 0 && framesBelow1 == 0 && glassFrames > 0 && nUncounted == 0 && FrontRoomsGlassRTSystem.NearUnbuilt == 0
                 && FrontRoomsGlassRTSystem.KnownChunks == chunks0;
        Log("KEY PICKUP: taken " + took + " | key at the map root for " + atRootFrames + " frames, destroyed at +" + destroyedAt + " | near births " + births + " | gate holds " + holds
            + " | traced frames with glass " + glassFrames + ", min weight " + F3(minWeight) + ", frames below weight 1: " + framesBelow1 + ", frames with misses " + missFrames + ", max miss share " + (100.0 * maxMissShare).ToString("0.###", CultureInfo.InvariantCulture) + " % (dark room beyond the pane)"
            + " | chunks " + chunks0 + " -> " + FrontRoomsGlassRTSystem.KnownChunks
            + " | map-root children that are not chunks, new: " + (FrontRoomsGlassRTSystem.NonChunkRoots - nonChunk0) + " | NearUnbuilt max " + maxNearUnbuilt + ", now " + FrontRoomsGlassRTSystem.NearUnbuilt
            + " | built squares within 60 m counted as unbuilt: " + nUncounted + uncounted + " | square " + oldRound + " refs " + FrontRoomsGlassRTSystem.ChunkCoordRefs(oldRound.x, oldRound.y)
            + " | " + (ok ? "PASS" : "FAIL"));
        M("keypickup.ran", 1); M("keypickup.taken", took ? 1 : 0); M("keypickup.at_root_frames", atRootFrames); M("keypickup.destroyed_at", destroyedAt);
        M("keypickup.near_births", births); M("keypickup.holds", holds); M("keypickup.frames_below_weight1", framesBelow1); M("keypickup.min_weight", minWeight);
        M("keypickup.miss_frames", missFrames); M("keypickup.max_miss_share", maxMissShare); M("keypickup.glass_frames", glassFrames);
        M("keypickup.uncounted_squares", nUncounted); M("keypickup.near_unbuilt", FrontRoomsGlassRTSystem.NearUnbuilt); M("keypickup.pass", ok ? 1 : 0);
        FrontRoomsMapWorld.ShowPlaceholderKey = false;

        // MapWorld.RebuildChunk of a built neighbour of the window's chunk (the Level Designer's ReplaceModule rebuilds this way:
        // the new root is added before the old one leaves)
        var builtDict = Get(map, "built") as IDictionary;
        var wc = MapGrid.ChunkOf(w.a);
        var target = wc; var found = false;
        foreach (var d in new[] { new GridCoord(1, 0), new GridCoord(-1, 0), new GridCoord(0, 1), new GridCoord(0, -1) })
        {
            var c = wc + d;
            if (builtDict != null && builtDict.Contains(c) && !map.InStartArea(MapGrid.ChunkOrigin(c))) { target = c; found = true; break; }
        }
        if (!found) { Log("REBUILD: no built neighbour of chunk " + wc); yield break; }
        var refs0 = FrontRoomsGlassRTSystem.ChunkCoordRefs(target.x, target.y);
        var rb0 = Time.frameCount;
        map.RebuildChunk(target);
        var refsMax = 0;
        for (var f = 0; f < 4; f++) { Place(v.feet, v.look); yield return 1; Render(rt); Sync(rt); refsMax = Math.Max(refsMax, FrontRoomsGlassRTSystem.ChunkCoordRefs(target.x, target.y)); }
        yield return WaitWarm("after RebuildChunk");
        var refs1 = FrontRoomsGlassRTSystem.ChunkCoordRefs(target.x, target.y);
        var u2 = new StringBuilder();
        var nU2 = BuiltButUncounted(cam.transform.position, 60f, u2);
        var ok2 = refs0 == 1 && refs1 == 1 && nU2 == 0 && FrontRoomsGlassRTSystem.NearUnbuilt == 0;
        Log("REBUILD chunk " + target + " (MapWorld.RebuildChunk, " + F((MapGrid.ChunkSize * new Vector2(target.x + .5f, target.y + .5f) - new Vector2(cam.transform.position.x - map.transform.position.x, cam.transform.position.z - map.transform.position.z)).magnitude) + " m from the camera to its centre): square refs before " + refs0
            + ", max while both roots stood " + refsMax + ", after " + refs1 + " | NearUnbuilt " + FrontRoomsGlassRTSystem.NearUnbuilt + " | built squares within 60 m counted as unbuilt: " + nU2 + u2
            + " | gate after " + (Time.frameCount - rb0) + " frames: " + FrontRoomsGlassRTSystem.GateLine(camState) + " | " + (ok2 ? "PASS" : "FAIL"));
        M("rebuild.refs_before", refs0); M("rebuild.refs_max", refsMax); M("rebuild.refs_after", refs1); M("rebuild.uncounted_squares", nU2); M("rebuild.near_unbuilt", FrontRoomsGlassRTSystem.NearUnbuilt); M("rebuild.pass", ok2 ? 1 : 0);
        SwapPanes();
    }

    /// <summary>The nearest Level 0 window within 20 cells of `origin` (1.5 m head-on): the jump phase's far target.</summary>
    static bool FarWindowView(GridCoord origin, string name, out View view)
    {
        var sill = (ModuleUnits.WindowSill + ModuleUnits.WindowTop) * .5f;
        view = new View();
        foreach (var c in CellsAround(origin, 20))
            foreach (var st in new[] { new GridCoord(1, 0), new GridCoord(0, 1) })
            {
                var n = c + st;
                if (map.Cache.Edge(c, n) != EdgeKind.Window) continue;
                if (map.Cache.ZoneOf(c).theme != ZoneTheme.Level0 || map.Cache.ZoneOf(n).theme != ZoneTheme.Level0) continue;
                var cross = map.CrossingPoint(c, n); var d = map.CellCenter(n) - map.CellCenter(c); d.y = 0f; d.Normalize();
                view = new View { name = name + "_" + c.x + "_" + c.y, feet = cross - d * 1.5f, look = cross + Vector3.up * sill, note = "a Level 0 window the map has not built" };
                return true;
            }
        return false;
    }

    /// <summary>Find the game object of the (new) scene and start a run as Main does (the title start; with fixedSeed the
    /// harness seed 4242 -> run seed 516574485, unless the game already started one itself: the R restart path).</summary>
    static IEnumerator StartRun(string why, bool fixedSeed)
    {
        var f0 = Time.frameCount;
        while ((game = Object.FindFirstObjectByType<FrontRooms3DGame>()) == null && Time.frameCount - f0 < 3000) yield return 1;
        if (game == null) { Log("START (" + why + "): no game object after the reload"); yield break; }
        cam = Get<Camera>(game, "cam");
        yield return 120;   // the title corridor loads its first rooms (Main waits for Time.time 2 s after Play)
        var startedByGame = Get<bool>(game, "mapPlay");
        if (fixedSeed && !startedByGame) UnityEngine.Random.InitState(4242);
        for (var tries = 0; tries < 60 && !Get<bool>(game, "mapPlay"); tries++)
        {
            Call(game, "RequestTitleStart");
            if (!Get<bool>(game, "mapPlay")) yield return 15;
        }
        if (!Get<bool>(game, "mapPlay")) { Log("START (" + why + "): could not start a run"); game = null; yield break; }
        map = Get<FrontRoomsMapWorld>(game, "map");
        Get<FrontRoomsHunterTuning>(game, "hunterTuning").releaseDelaySeconds = 1e6f;
        var t0 = Time.time;
        while (!Get<bool>(game, "startDoorOpened") && Time.time - t0 < 8f) { Resume(); yield return 1; }
        yield return 30;
        camState = cam.GetComponent<FrontRoomsGlassRTCamera>();
        if (camState == null) { FrontRoomsGlassRT.OptIn(cam); camState = cam.GetComponent<FrontRoomsGlassRTCamera>(); Log("HARNESS opted the camera in (the game had not)"); }
        FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.Level0, 0f);
        SwapPanes();
        Log("START (" + why + "): run " + (startedByGame ? "started by the game itself (restart flag)" : "started by the harness") + " | seed " + Get<int>(game, "runSeed")
            + " | camera id " + (camState != null ? camState.id : -1) + " | quality " + FrontRoomsGlassRT.Quality + " | " + Gates());
    }

    /// <summary>R3-B1: SceneManager reloads with RT on, first while the camera's stream-in gate is held (a far jump into
    /// unbuilt map, then the game's R restart: restart flag + LoadScene, FrontRooms3DGame.cs:2433-2434), then while it is
    /// open (at a window, weight 1; the title path with the harness seed, so the run is the first run's map). After each
    /// reload, once the NEW camera's gate opens at a window: burst off, the native stream budget back to the default
    /// (10k triangles / 8 builds), every gate belongs to a live camera, and it stays so for 120 frames. Then walk2 in the
    /// restarted session (GPU AS time against the first walk).</summary>
    static IEnumerator Restart(WindowSetup w, List<WindowSetup> into)
    {
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
        var restartField = typeof(FrontRooms3DGame).GetField("restart", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        var have = false; var now = new WindowSetup();
        var allOk = true;
        for (var pass = 0; pass < 2; pass++)
        {
            var held = pass == 0;
            var label = held ? "R1_held" : "R2_open";
            if (held)
            {
                View far;
                if (!FarWindowView(w.a + new GridCoord(-45, 0), "restart_jump", out far)) { Log("RESTART: no far window for the held case"); allOk = false; continue; }
                Place(far.feet, far.look);
                Resume();
                for (var f = 0; f < 4; f++) { Place(far.feet, far.look); yield return 1; Render(rt); Sync(rt); }
            }
            var oldId = camState != null ? camState.id : -1;
            var gateBefore = FrontRoomsGlassRTSystem.GateLine(camState);
            var heldNow = gateBefore.StartsWith("hold", StringComparison.Ordinal);
            Log("RESTART " + label + " before the reload: camera " + oldId + " | gate " + gateBefore + " | " + Gates());
            M("restart." + label + ".gate_held_before", heldNow ? 1 : 0); M("restart." + label + ".burst_before", FrontRoomsGlassRTSystem.BlasBurst ? 1 : 0);
            if (held && !heldNow) Log("RESTART " + label + ": WARNING the gate was not held at the reload (the case is not exercised)");
            if (held && restartField != null) restartField.SetValue(null, true);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            yield return 3;
            var orphans = FrontRoomsGlassRTSystem.OrphanGates;
            Log("RESTART " + label + " after the reload (old camera " + oldId + " destroyed): " + Gates());
            M("restart." + label + ".orphans_after_reload", orphans); M("restart." + label + ".burst_after_reload", FrontRoomsGlassRTSystem.BlasBurst ? 1 : 0);
            game = null; map = null; cam = null; camState = null;
            yield return StartRun("restart " + label, !held);
            if (game == null || map == null || camState == null) { allOk = false; yield break; }
            var list = new List<WindowSetup>();
            yield return FindWindow(ZoneTheme.Level0, 5, list);
            if (list.Count == 0) { Log("RESTART " + label + ": no Level 0 window in the new run"); allOk = false; yield break; }
            now = list[0]; have = true;
            yield return GoTo(now.views[1]);
            // fix round 34 (art review 4 #5): a dark room beyond the new run's window (raster only, not registered), so the
            // saved frames show the reflection at full strength: zone cube while held, traced once open; misses are logged
            // over the 120 frames after the reopen
            var rbox = Box("RTVerify dark room", now.center + now.dir * 3.1f, new Vector3(6f, 4f, 6f), Color.black, false);
            rbox.transform.rotation = Quaternion.LookRotation(now.dir);
            rbox.transform.position = now.center + now.dir * 3.1f;
            var openAt = -1; var burstAfterOpen = 0; var budgetBad = 0; var orphanFrames = 0; var gatesOver = 0; var heldFrames = 0;
            var burstWhileHeld = 0; var missFramesOpen = 0; var glassFramesOpen = 0; double maxMissOpen = 0; long missSumOpen = 0; var savedHeld = false;
            for (var f = 0; f < 900; f++)
            {
                Place(now.views[1].feet, now.views[1].look);
                yield return 1;
                Render(rt); Sync(rt);
                var open = camState.lastWeight >= 1f && !FrontRoomsGlassRTSystem.GateBusy;
                if (openAt < 0 && !open)
                {
                    heldFrames++; if (FrontRoomsGlassRTSystem.BlasBurst) burstWhileHeld++;
                    if (!savedHeld) { savedHeld = true; Read8(rt); SavePng("P1_restart_" + label + "_held_f" + f.ToString("000") + ".png"); SaveRadiance(ReadFloat(Out), W, H, "P1_restart_" + label + "_held_rt_f" + f.ToString("000") + ".png", ReadFloat(GlassDepth)); M("restart." + label + ".held_saved_frame", f); M("restart." + label + ".held_saved_weight", camState.lastWeight); }
                }
                if (openAt < 0 && open)
                {
                    openAt = f;
                    Read8(rt); SavePng("P1_restart_" + label + "_open_f000.png"); SaveRadiance(ReadFloat(Out), W, H, "P1_restart_" + label + "_open_rt_f000.png", ReadFloat(GlassDepth));
                }
                if (openAt < 0) continue;
                FrontRoomsGlassRT.TryGetStats(out var rs);
                if (rs.glassPixels > 0)
                {
                    glassFramesOpen++; missSumOpen += rs.missPixels;
                    if (rs.missPixels > 0) missFramesOpen++;
                    maxMissOpen = Math.Max(maxMissOpen, rs.missPixels / (double)rs.glassPixels);
                }
                if (FrontRoomsGlassRTSystem.BlasBurst) burstAfterOpen++;
                if (!BudgetIsDefault()) budgetBad++;
                if (FrontRoomsGlassRTSystem.OrphanGates > 0) orphanFrames++;
                if (FrontRoomsGlassRTSystem.GateCount > FrontRoomsGlassRTSystem.LiveCameras) gatesOver++;
                if (f - openAt >= 120) break;
            }
            var ok = openAt >= 0 && orphans == 0 && burstAfterOpen == 0 && budgetBad == 0 && orphanFrames == 0 && gatesOver == 0 && glassFramesOpen > 0 && maxMissOpen <= .005;
            allOk &= ok;
            Log("RESTART " + label + ": new camera " + camState.id + " | its gate opened at +" + openAt + " (held " + heldFrames + " frames, burst on in " + burstWhileHeld + " of them)"
                + " | over the 120 frames after it opened: burst on " + burstAfterOpen + ", stream budget off the default " + budgetBad + ", frames with an orphan gate " + orphanFrames
                + ", frames with more gates than live cameras " + gatesOver + ", traced frames with glass " + glassFramesOpen + ", frames with misses " + missFramesOpen + " (" + missSumOpen + " px in all), max miss share " + (100.0 * maxMissOpen).ToString("0.###", CultureInfo.InvariantCulture) + " % (dark room beyond the pane)"
                + " | now " + Gates() + " | " + (ok ? "PASS" : "FAIL"));
            M("restart." + label + ".open_at", openAt); M("restart." + label + ".burst_after_open", burstAfterOpen); M("restart." + label + ".budget_bad", budgetBad);
            M("restart." + label + ".orphan_frames", orphanFrames); M("restart." + label + ".gates", FrontRoomsGlassRTSystem.GateCount); M("restart." + label + ".live_cameras", FrontRoomsGlassRTSystem.LiveCameras);
            M("restart." + label + ".glass_frames_open", glassFramesOpen); M("restart." + label + ".miss_frames_open", missFramesOpen); M("restart." + label + ".miss_px_open", missSumOpen); M("restart." + label + ".max_miss_share_open", maxMissOpen);
            M("restart." + label + ".pass", ok ? 1 : 0);
            Read8(rt); SavePng("P1_restart_" + label + "_open.png");
            SaveRadiance(ReadFloat(Out), W, H, "P1_restart_" + label + "_open_rt.png", ReadFloat(GlassDepth));
            CaptureRT("P1_restart_" + label + "_dark_off.png", FrontRoomsGlassRTQuality.Off, 2);
            FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
            Object.Destroy(rbox);
            yield return 1;
        }
        M("restart.pass", allOk ? 1 : 0);
        if (have && Want("walk")) yield return Walk(now, "walk2");   // the same seed as the first run: the first walk's path
        if (have) into.Add(now);
    }

    static IEnumerator Walk(WindowSetup w, string tag = "walk")
    {
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
        FrontRoomsGlassRTSystem.ResetMaxima();
        var TAG = tag.ToUpperInvariant();
        var path = WalkPath(w.views[1].feet, 60);
        var frames = new StringBuilder("frame\tmain_ms\tencode_ms\tgpu_total_ms\ttlas\tvisible\ttraced\tchunks\tregistered\tpending\tscene_ms\tlive_ms\tlamps_ms\trecords_ms\tconsts_ms\tsubmit_ms\tnative_submit_ms\tgc0\tstatic_rebuilds\tmain_cpu_ms\tweight\tnear_backlog\tglass_px\tmiss_px\tgpu_as_ms\tblas_builds\tblas_tris\tbudget_tris\tbudget_builds\tburst\n");
        // fix round 33 (R3-B1): the acceleration-structure GPU time and the BLAS budget in force, per traced frame
        var asMs = new List<double>(); var buildsMax = 0; var trisMax = 0; var budgetOffDefault = 0; var burstOn = 0; var budgetTriMax = 0;
        // fix round 32 (review 2 engineering #6): frames from a chunk root's appearance to its full registration, and the
        // stream-in gate during a normal walk (it must not hold: new chunks appear at the build radius, 48 m ahead)
        var walkStart = Time.frameCount; var holds0 = FrontRoomsGlassRTSystem.GateHolds; var burst0 = FrontRoomsGlassRTSystem.BurstFrames;
        var shownMissMax = 0.0; var shownWithHoles = 0; var nearFrames = 0;
        FrontRoomsGlassRTSystem.SlowestRegistrationMs = 0; FrontRoomsGlassRTSystem.SlowestRegistration = "";
        for (var k = 0; k < 4; k++) FrontRoomsGlassRTSystem.RegProfile[k] = 0;
        var cpu0 = FrontRoomsGlassRTSystem.MeshUploadsCPU; var reg0 = FrontRoomsGlassRTSystem.RegisteredInstances;
        var texCalls0 = FrontRoomsGlassRTSystem.TexturePtrCalls; var texMs0 = FrontRoomsGlassRTSystem.TexturePtrMsTotal; FrontRoomsGlassRTSystem.TexturePtrMsMax = 0;
        var rebuilds0 = FrontRoomsGlassRTSystem.StaticRebuilds; var patches0 = FrontRoomsGlassRTSystem.StaticPatches;
        var gcFrames = 0;
        var mainsNoGc = new List<double>();
        var mains = new List<double>(); var encs = new List<double>(); var blanks = 0; var traced = 0; var visibleFrames = 0;
        var mainsStream = new List<double>(); var mainsQuiet = new List<double>();
        // fix round 31: the same spans in the main thread's CPU time (wall time on a loaded Mac includes the time other
        // processes held the core), plus the lens reads the level test skipped, gather slices and TLAS reuse
        var cpus = new List<double>(); var cpusStream = new List<double>(); var cpusQuiet = new List<double>();
        var lensReads0 = FrontRoomsGlassRTSystem.LensReads; var lensSkips0 = FrontRoomsGlassRTSystem.LensSkips; var gather0 = FrontRoomsGlassRTSystem.GatherSlices;
        FrontRoomsGlassRT.TryGetStats(out var s0w);
        var chunks0 = FrontRoomsGlassRTSystem.KnownChunks;
        var built = new HashSet<int>();
        var pathLen = 0f; for (var k = 1; k < path.Count; k++) pathLen += (path[k] - path[k - 1]).magnitude;
        var lastRegistered = FrontRoomsGlassRTSystem.RegisteredInstances;
        for (var f = 0; f < 600; f++)
        {
            // 4 m/s along the path, looking ahead
            var dAlong = Mathf.PingPong(f * 4f / 60f, Mathf.Max(pathLen, .01f));
            Vector3 pos = path[0], ahead = path[Math.Min(1, path.Count - 1)];
            var acc = 0f;
            for (var k = 1; k < path.Count; k++)
            {
                var seg = (path[k] - path[k - 1]).magnitude;
                if (acc + seg >= dAlong) { pos = Vector3.Lerp(path[k - 1], path[k], (dAlong - acc) / Mathf.Max(seg, 1e-4f)); ahead = path[Math.Min(k + 1, path.Count - 1)]; break; }
                acc += seg;
            }
            var root = PlayerRoot;
            var body = Get<CharacterController>(game, "playerBody");
            body.enabled = false; root.position = pos; body.enabled = true;
            var look = ahead - pos; look.y = 0f; if (look.sqrMagnitude < 1e-4f) look = Vector3.forward;
            Look(pos + Vector3.up * ModuleUnits.PlayerEye + look.normalized * 3f + Vector3.Cross(Vector3.up, look.normalized) * Mathf.Sin(f * .05f) * 2f);
            if (f % 20 == 0) SwapPanes();
            yield return 1;
            var gcBefore = GC.CollectionCount(0);
            Render(rt);
            var gc = GC.CollectionCount(0) - gcBefore;
            if (gc > 0) gcFrames++;
            FrontRoomsGlassRT.TryGetStats(out var s);
            if (gc == 0) mainsNoGc.Add(s.mainThreadMs);
            var vis = camState.lastVisibleReceivers;
            var tr = camState.lastWeight > 0f;
            if (vis > 0) { visibleFrames++; if (!tr) blanks++; else traced++; }
            mains.Add(s.mainThreadMs); encs.Add(s.encodeMs);
            var reg = FrontRoomsGlassRTSystem.RegisteredInstances;
            var streaming = reg != lastRegistered || FrontRoomsGlassRTSystem.PendingRescans > 0;
            (streaming ? mainsStream : mainsQuiet).Add(s.mainThreadMs);
            cpus.Add(s.mainThreadCpuMs); (streaming ? cpusStream : cpusQuiet).Add(s.mainThreadCpuMs);
            lastRegistered = reg;
            foreach (Transform c in map.transform) built.Add(c.GetInstanceID());
            var b = FrontRoomsGlassRTSystem.Breakdown;
            frames.Append(f).Append('\t').Append(F3(s.mainThreadMs)).Append('\t').Append(F3(s.encodeMs)).Append('\t').Append(F3(s.gpuMsTotal)).Append('\t').Append(s.tlasInstances)
                  .Append('\t').Append(vis).Append('\t').Append(tr ? 1 : 0).Append('\t').Append(FrontRoomsGlassRTSystem.KnownChunks).Append('\t').Append(reg)
                  .Append('\t').Append(s.meshesPending).Append('\t').Append(F3(b[0])).Append('\t').Append(F3(b[1])).Append('\t').Append(F3(b[2])).Append('\t').Append(F3(b[3])).Append('\t').Append(F3(b[4])).Append('\t').Append(F3(b[5]))
                  .Append('\t').Append(F3(FrontRoomsGlassRTNative.Get(FrontRoomsGlassRTNative.Stat.SubmitMs))).Append('\t').Append(gc).Append('\t').Append(FrontRoomsGlassRTSystem.StaticRebuilds - rebuilds0).Append('\t').Append(F3(s.mainThreadCpuMs))
                  .Append('\t').Append(F3(camState.lastWeight)).Append('\t').Append(FrontRoomsGlassRTSystem.NearBacklog).Append('\t').Append(s.glassPixels).Append('\t').Append(s.missPixels);
            {
                var bTri = (int)FrontRoomsGlassRTNative.Get(FrontRoomsGlassRTNative.Stat.StreamTriBudget);
                var bBld = (int)FrontRoomsGlassRTNative.Get(FrontRoomsGlassRTNative.Stat.StreamBuildBudget);
                var nb = (int)FrontRoomsGlassRTNative.Get(FrontRoomsGlassRTNative.Stat.BuildsLast);
                var nt = (int)FrontRoomsGlassRTNative.Get(FrontRoomsGlassRTNative.Stat.TrianglesLast);
                frames.Append('\t').Append(F3(s.gpuMsAccel)).Append('\t').Append(nb).Append('\t').Append(nt).Append('\t').Append(bTri).Append('\t').Append(bBld).Append('\t').Append(FrontRoomsGlassRTSystem.BlasBurst ? 1 : 0);
                if (tr) asMs.Add(s.gpuMsAccel);
                buildsMax = Math.Max(buildsMax, nb); trisMax = Math.Max(trisMax, nt); budgetTriMax = Math.Max(budgetTriMax, bTri);
                if (bTri != DefaultBudgetTris || bBld != DefaultBudgetBuilds) budgetOffDefault++;
                if (FrontRoomsGlassRTSystem.BlasBurst) burstOn++;
            }
            frames.Append('\n');
            if (FrontRoomsGlassRTSystem.NearBacklog > 0) nearFrames++;
            if (camState.lastWeight > 0f && s.glassPixels > 0)
            {
                var mf = (double)s.missPixels / s.glassPixels;
                shownMissMax = Math.Max(shownMissMax, mf);
                if (mf > FrontRoomsGlassRTSystem.GateMissFraction) shownWithHoles++;
            }
        }
        File.WriteAllText(Path.Combine(OutDir, tag + "_frames.tsv"), frames.ToString());
        ChunkReport(TAG, walkStart, tag + "_chunks.tsv", tag);
        {
            double PA(List<double> l, double q) => l.Count > 0 ? Pct(l, q) : 0;
            Log(TAG + " acceleration structures (fix round 33, R3-B1): GPU AS ms on traced frames p50 " + F3(PA(asMs, .5)) + " p95 " + F3(PA(asMs, .95)) + " max " + F3(asMs.Count > 0 ? asMs.Max() : 0)
                + " mean " + F3(asMs.Count > 0 ? asMs.Average() : 0) + " (" + asMs.Count + " frames) | BLAS builds per event max " + buildsMax + ", triangles max " + trisMax
                + " | frames with the stream budget off the default (" + DefaultBudgetTris + " / " + DefaultBudgetBuilds + ") " + budgetOffDefault + " (budget max " + budgetTriMax + ") | frames with the burst flag " + burstOn);
            M(tag + ".gpu_as.p50", PA(asMs, .5)); M(tag + ".gpu_as.p95", PA(asMs, .95)); M(tag + ".gpu_as.mean", asMs.Count > 0 ? asMs.Average() : 0);
            M(tag + ".blas_builds_max", buildsMax); M(tag + ".blas_tris_max", trisMax); M(tag + ".budget_off_default_frames", budgetOffDefault); M(tag + ".burst_frames_flag", burstOn);
        }
        Log(TAG + " stream-in gate: near-backlog frames " + nearFrames + " | holds started " + (FrontRoomsGlassRTSystem.GateHolds - holds0) + " | burst frames " + (FrontRoomsGlassRTSystem.BurstFrames - burst0)
            + " | shown frames with misses > " + F(100.0 * FrontRoomsGlassRTSystem.GateMissFraction) + " % of glass px " + shownWithHoles + " (max " + F(100.0 * shownMissMax) + " %) | gate " + FrontRoomsGlassRTSystem.GateLine(camState));
        M(tag + ".near_backlog_frames", nearFrames); M(tag + ".gate_holds", FrontRoomsGlassRTSystem.GateHolds - holds0); M(tag + ".burst_frames", FrontRoomsGlassRTSystem.BurstFrames - burst0);
        M(tag + ".shown_frames_with_holes", shownWithHoles); M(tag + ".shown_miss_max_pct", 100.0 * shownMissMax);
        {
            FrontRoomsGlassRT.TryGetStats(out var s1w);
            double P(List<double> l, double q) => l.Count > 0 ? Pct(l, q) : 0;
            Log(TAG + " main thread in CPU time (CLOCK_THREAD_CPUTIME_ID, not inflated by the load): all p50 " + F3(P(cpus, .5)) + " p95 " + F3(P(cpus, .95)) + " p99 " + F3(P(cpus, .99)) + " max " + F3(cpus.Count > 0 ? cpus.Max() : 0)
                + " | quiet (" + cpusQuiet.Count + ") p50 " + F3(P(cpusQuiet, .5)) + " p95 " + F3(P(cpusQuiet, .95)) + " p99 " + F3(P(cpusQuiet, .99))
                + " | streaming (" + cpusStream.Count + ") p50 " + F3(P(cpusStream, .5)) + " p95 " + F3(P(cpusStream, .95)) + " p99 " + F3(P(cpusStream, .99)) + " max " + F3(cpusStream.Count > 0 ? cpusStream.Max() : 0)
                + " | wall quiet p50 " + F3(P(mainsQuiet, .5)) + " p95 " + F3(P(mainsQuiet, .95)) + " | lens reads " + (FrontRoomsGlassRTSystem.LensReads - lensReads0) + ", skipped by the level test " + (FrontRoomsGlassRTSystem.LensSkips - lensSkips0)
                + " | gather slices " + (FrontRoomsGlassRTSystem.GatherSlices - gather0) + " | TLAS built " + (s1w.tlasBuilt - s0w.tlasBuilt) + ", reused " + (s1w.tlasReused - s0w.tlasReused));
            M(tag + ".cpu.p50", P(cpus, .5)); M(tag + ".cpu.p95", P(cpus, .95)); M(tag + ".cpu.p99", P(cpus, .99));
            M(tag + ".cpu.quiet.p50", P(cpusQuiet, .5)); M(tag + ".cpu.quiet.p95", P(cpusQuiet, .95)); M(tag + ".cpu.quiet.p99", P(cpusQuiet, .99));
            M(tag + ".cpu.stream.p95", P(cpusStream, .95)); M(tag + ".cpu.stream.p99", P(cpusStream, .99)); M(tag + ".cpu.stream.max", cpusStream.Count > 0 ? cpusStream.Max() : 0);
            M(tag + ".wall.quiet.p50", P(mainsQuiet, .5)); M(tag + ".wall.quiet.p99", P(mainsQuiet, .99)); M(tag + ".wall.stream.p95", P(mainsStream, .95)); M(tag + ".wall.stream.max", mainsStream.Count > 0 ? mainsStream.Max() : 0);
            M(tag + ".blank", blanks); M(tag + ".visible", visibleFrames);
        }
        FrontRoomsGlassRT.TryGetStats(out var end);
        Log(TAG + " 600 frames at 4 m/s along a " + path.Count + "-cell path (" + F(pathLen) + " m, ping-pong): chunk roots seen " + built.Count + " (start " + chunks0 + ", end " + FrontRoomsGlassRTSystem.KnownChunks + ")"
            + " | frames with a visible receiver " + visibleFrames + " | traced " + traced + " | BLANK " + blanks
            + " | main-thread ms p50 " + F3(Pct(mains, .5)) + " p99 " + F3(Pct(mains, .99)) + " max " + F3(mains.Max())
            + " | quiet frames p99 " + F3(mainsQuiet.Count > 0 ? Pct(mainsQuiet, .99) : 0) + " (" + mainsQuiet.Count + ") | streaming frames p99 " + F3(mainsStream.Count > 0 ? Pct(mainsStream, .99) : 0) + " (" + mainsStream.Count + ")"
            + " | encode ms p50 " + F3(Pct(encs, .5)) + " p99 " + F3(Pct(encs, .99)) + " | command-buffer errors " + end.commandBufferErrors + " | pool-busy frames " + FrontRoomsGlassRTNative.Get(FrontRoomsGlassRTNative.Stat.PoolBusy)
            + " | frames with a managed GC " + gcFrames + " | main p99 without them " + F3(mainsNoGc.Count > 0 ? Pct(mainsNoGc, .99) : 0)
            + " | static block rebuilds " + (FrontRoomsGlassRTSystem.StaticRebuilds - rebuilds0) + " (in-place patches " + (FrontRoomsGlassRTSystem.StaticPatches - patches0) + ") | native submit max " + F3(FrontRoomsGlassRTNative.Get(FrontRoomsGlassRTNative.Stat.SubmitMsMax)) + " ms"
            + " | geometry pages this session " + FrontRoomsGlassRTNative.Get(FrontRoomsGlassRTNative.Stat.GeoPages).ToString("0", CultureInfo.InvariantCulture)
            + " (taken from the pre-allocated spare " + FrontRoomsGlassRTNative.Get(FrontRoomsGlassRTNative.Stat.GeoSpareHits).ToString("0", CultureInfo.InvariantCulture) + ")"
            + " | slowest single registration " + F3(FrontRoomsGlassRTSystem.SlowestRegistrationMs) + " ms: " + FrontRoomsGlassRTSystem.SlowestRegistration
            + " | texture-pointer fetches " + (FrontRoomsGlassRTSystem.TexturePtrCalls - texCalls0) + " (" + F3(FrontRoomsGlassRTSystem.TexturePtrMsTotal - texMs0) + " ms, max " + F3(FrontRoomsGlassRTSystem.TexturePtrMsMax) + " ms)");
        double T2Ms(long t) => t * 1000.0 / Stopwatch.Frequency;
        var rp = FrontRoomsGlassRTSystem.RegProfile;
        Log(TAG + " registration profile: mesh acquire " + F3(T2Ms(rp[0])) + " ms total (" + (FrontRoomsGlassRTSystem.MeshUploadsCPU - cpu0) + " new CPU uploads; native upload call " + F3(T2Ms(rp[2]))
            + " ms total, slowest " + F3(T2Ms(rp[3])) + " ms) | material blocks " + F3(T2Ms(rp[1])) + " ms total | net registered " + (FrontRoomsGlassRTSystem.RegisteredInstances - reg0));
        M(tag + ".reg_mesh_ms", T2Ms(rp[0])); M(tag + ".reg_native_ms", T2Ms(rp[2])); M(tag + ".reg_native_max_ms", T2Ms(rp[3])); M(tag + ".reg_material_ms", T2Ms(rp[1]));
        M(tag + ".texture_ptr_calls", FrontRoomsGlassRTSystem.TexturePtrCalls - texCalls0); M(tag + ".texture_ptr_ms_max", FrontRoomsGlassRTSystem.TexturePtrMsMax);
        M(tag + ".gc_frames", gcFrames); M(tag + ".main_p99_nogc", mainsNoGc.Count > 0 ? Pct(mainsNoGc, .99) : 0); M(tag + ".static_rebuilds", FrontRoomsGlassRTSystem.StaticRebuilds - rebuilds0);
        M(tag + ".slowest_registration_ms", FrontRoomsGlassRTSystem.SlowestRegistrationMs);
        M(tag + ".chunks_seen", built.Count); M(tag + ".visible_frames", visibleFrames); M(tag + ".blank", blanks);
        M(tag + ".main_p50", Pct(mains, .5)); M(tag + ".main_p99", Pct(mains, .99)); M(tag + ".main_max", mains.Max()); M(tag + ".encode_p99", Pct(encs, .99)); M(tag + ".cb_errors", end.commandBufferErrors);
        if (mainsQuiet.Count > 0) M(tag + ".main_quiet_p99", Pct(mainsQuiet, .99));
        if (mainsStream.Count > 0) M(tag + ".main_stream_p99", Pct(mainsStream, .99));
    }
}
#endif
