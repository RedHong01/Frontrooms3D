// G14 P0 + P1 VERIFY HARNESS (clone proj_rt). Built on the runtime-probe harness (FrontRoomsGlassRTProbe, 02).
// Plays Assets/Scenes/FrontRooms3D.unity with the interaction-audit / G11 start (Random.InitState(4242) before the
// title start → run seed 516574485), swaps the map's panes to Glass_Window at runtime (a clone-only TEST SWITCH, no
// MapWorld edit: design §2.4 C8), sets the Level 0 zone cube, and measures the production RT path:
//   views v1-v5 (head-on level / centre, 50°, steep, close) RT Off / planar reference / High / Ultra, alignment,
//   orientation, transmission, misses, occlusion and lifetime cases, dark beyond, Office, lamp sync, temporal
//   accumulation and a moving strafe, cameras, capacity, the fracture API, a free walk, GPU cost, hit-shading parity.
// Output: <clone>/Verification/rt_p0p1 (PNG frames + verify_log.txt + metrics.tsv). Sheets: Tools/rt/compose_rt_sheets.py.
// Run: Unity -batchmode -projectPath <proj_rt> -executeMethod FrontRoomsGlassRTVerify.RunBatch -logFile <f>
//      [-frVerifyOnly name1,name2] (phases: views,orient,occluder,relay,broken,stream,dark,office,lamps,temporal,
//       cameras,capacity,fracture,walk,cost,parity,isolation)
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
        SessionState.SetFloat(DeadlineKey, (float)EditorApplication.timeSinceStartup + 1500f);
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

    static void Tick()
    {
        var timedOut = EditorApplication.timeSinceStartup > SessionState.GetFloat(DeadlineKey, float.MaxValue);
        if (finished || timedOut || SessionState.GetBool(DoneKey, false))
        {
            if (timedOut && !finished && !SessionState.GetBool(DoneKey, false)) { Log("TIMEOUT"); Finish(); }
            if (EditorApplication.isPlaying) { EditorApplication.ExitPlaymode(); return; }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
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
        Log("FRAME " + file + " · " + note + " · cam " + V(cam.transform.position) + " fwd " + V(cam.transform.forward));
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
        return "GPU AS " + F3(s.gpuMsAccel) + " · trace " + F3(s.gpuMsTrace) + " · resolve " + F3(s.gpuMsResolve) + " · total " + F3(s.gpuMsTotal)
            + " ms · encode " + F3(s.encodeMs) + " ms · main " + F3(s.mainThreadMs) + " ms · TLAS " + s.tlasInstances + " · meshes " + s.meshes + " (pending " + s.meshesPending
            + ") · lamps " + s.lamps + " · receivers visible " + s.receiversVisible + " · glass px " + s.glassPixels + " · miss " + s.missPixels + " · layered " + s.layerPixels
            + " · AA px " + s.aaPixels + " · shadow rays " + s.shadowRays + " · accum " + s.accumulation + " · CB errors " + s.commandBufferErrors
            + " · main breakdown scene/live/cand/lamps/submit " + string.Join("/", FrontRoomsGlassRTSystem.Breakdown.Select(x => F3(x)));
    }

    // film grain off for analysis renders (every render differs otherwise); game-look frames keep it
    static readonly Dictionary<FilmGrain, bool> grainOriginal = new Dictionary<FilmGrain, bool>();
    static void Grain(bool on)
    {
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
    static void RenderMirror(Renderer pane, Vector3 paneCenter, Vector3 normalToCamera, RenderTexture target, bool post)
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
        var prev = pane.enabled;
        pane.enabled = false;
        Render(target, refCam);
        pane.enabled = prev;
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
        var windows = new List<(GridCoord a, GridCoord b)>();
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
                windows.Add((c, n));
            }
            if (windows.Count >= 16) break;
        }
        Log("window candidates (" + theme + "): " + windows.Count);
        foreach (var (a0, b0) in windows)
        {
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
                Log("WINDOW " + col.name + " (" + theme + ") · centre " + V(pc) + " · views " + string.Join(", ", views.Select(v => v.name)));
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

    static IEnumerator WaitWarm(string why)
    {
        var start = Time.frameCount;
        for (var k = 0; k < 400; k++)
        {
            FrontRoomsGlassRTSystem.Flush();
            Render(rt);
            Sync(rt);
            FrontRoomsGlassRT.TryGetStats(out var s);
            if (FrontRoomsGlassRT.Supported && s.meshesPending == 0 && FrontRoomsGlassRTSystem.PendingRescans == 0 && k > 3) break;
            yield return 1;
        }
        Log("WARM (" + why + ") after " + (Time.frameCount - start) + " frames · " + FrontRoomsGlassRT.Status + " · registered " + FrontRoomsGlassRTSystem.RegisteredInstances
            + " instances in " + FrontRoomsGlassRTSystem.KnownChunks + " chunks · meshes CPU " + FrontRoomsGlassRTSystem.MeshUploadsCPU + " / GPU " + FrontRoomsGlassRTSystem.MeshUploadsGPU
            + " (failed " + FrontRoomsGlassRTSystem.MeshUploadFailures + ") · materials " + FrontRoomsGlassRTSystem.MaterialsUsed + " · textures " + FrontRoomsGlassRTSystem.TexturesUsed
            + " · lenses " + FrontRoomsGlassRTSystem.Lens + " · receivers " + FrontRoomsGlassRTSystem.Receivers + " · dynamic " + FrontRoomsGlassRTSystem.Dynamics + " · " + StatsLine());
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
        Log("CAPS before play: gfx " + SystemInfo.graphicsDeviceType + " · " + SystemInfo.graphicsDeviceName + " · Unity SystemInfo.supportsRayTracing " + SystemInfo.supportsRayTracing
            + " · plugin caps 0x" + FrontRoomsGlassRTNative.FRGlassRT_QueryCaps().ToString("x") + " · " + FrontRoomsGlassRT.Status + " · Glass_Window " + (glassWindow != null));

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
        Log("run started · seed " + Get<int>(game, "runSeed") + " · camera fov " + F(cam.fieldOfView) + " far " + F(cam.farClipPlane)
            + " · game camera opted in by FrontRoomsPostStack.ConfigureCamera: " + (camState != null) + " · MapWorld standalone=" + Get(map, "standalone")
            + " · quality level " + QualitySettings.GetQualityLevel() + " → default RT " + FrontRoomsGlassRT.Quality);
        if (camState == null) { FrontRoomsGlassRT.OptIn(cam); camState = cam.GetComponent<FrontRoomsGlassRTCamera>(); Log("HARNESS opted the camera in (the game had not)"); }
        FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.Level0, 0f);   // the clone's MapWorld predates the zone call
        Log("zone reflection " + FrontRoomsZoneReflection.Zone + " (" + FrontRoomsZoneReflection.Mode + ") · panes swapped to Glass_Window: " + SwapPanes());
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
        if (Want("parity")) yield return Parity(W0);
        if (Want("stream")) yield return Stream(W0);
        if (Want("broken")) yield return Broken(W0);
        if (Want("office")) yield return Office();
        if (Want("walk")) yield return Walk(W0);
        Log("DONE · " + FrontRoomsGlassRT.Status + " · " + StatsLine());
    }

    // ---- P0-A2/A4/A6, P1-B2/B4: the five views, Off / reference / High / Ultra
    static IEnumerator Views(WindowSetup w)
    {
        var paneR = w.pane.GetComponent<Renderer>();
        foreach (var v in w.views)
        {
            yield return GoTo(v);
            yield return WaitWarm(v.name);
            var t = cam.transform;
            var toPane = w.center - t.position;
            var inc = Vector3.Angle(-toPane.normalized, -w.dir);
            Log("VIEW " + v.name + " — " + v.note + " · eye " + V(t.position) + " · " + F(toPane.magnitude) + " m · incidence " + F(inc) + " deg");
            var off = CaptureRT(v.name + "_off.png", FrontRoomsGlassRTQuality.Off);
            RenderMirror(paneR, w.center, -w.dir, rt, true);
            SaveMirrorPng(v.name + "_ref.png");
            var high = CaptureRT(v.name + "_high.png", FrontRoomsGlassRTQuality.High, 4);
            Log("  HIGH " + StatsLine() + " · frame GPU " + F3(high.frameGpu) + " ms");
            SaveRadiance(high.outPx, W, H, v.name + "_rt_high.png", high.depth);
            var ultra = CaptureRT(v.name + "_ultra.png", FrontRoomsGlassRTQuality.Ultra, 4);
            Log("  ULTRA " + StatsLine() + " · frame GPU " + F3(ultra.frameGpu) + " ms");
            SaveRadiance(ultra.outPx, W, H, v.name + "_rt_ultra.png", ultra.depth);

            // forward coverage of the glass (pane drawn vs hidden, RT High) vs the prepass mask vs RT coverage (grain off)
            Grain(false);
            var offA = CaptureRT(null, FrontRoomsGlassRTQuality.Off, 2);
            FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
            Render(rt); Render(rt); var withPane = Read8(rt);
            paneR.enabled = false; Render(rt); var noPane = Read8(rt); paneR.enabled = true;
            Render(rt);
            Grain(true);
            var forward = Diff8(withPane, noPane, 1);
            var prepass = Mask(high.depth, c => c.r > 0f);
            var traced = Mask(high.outPx, c => c.a > 1f);
            var iouPF = IoU(prepass, forward);
            var iouTP = IoU(traced, prepass);
            var beyond = BeyondOnePixel(prepass, forward, W, H);
            var glassPx = Count(prepass);
            var miss = glassPx - Count(traced.Zip(prepass, (a, b) => a && b).ToArray());
            // transmission: RT on vs off inside the glass (8-bit, after post)
            double dSum = 0; var darker = 0; var n = 0;
            for (var i = 0; i < prepass.Length; i++)
            {
                if (!prepass[i]) continue;
                var dy = Luma8(withPane, i) - Luma8(offA.frame, i);
                dSum += dy; n++;
                if (dy < -1.0) darker++;
            }
            var meanDY = n > 0 ? dSum / n : 0;
            Log("  ALIGN " + v.name + ": glass px (prepass) " + glassPx + " · forward-diff px " + Count(forward) + " · IoU(prepass, forward) " + F3(iouPF)
                + " · boundary px beyond 1 px " + beyond + " · IoU(traced, prepass) " + F3(iouTP) + " · misses " + miss + " (" + F(100.0 * miss / Math.Max(1, glassPx)) + " %)"
                + " · ΔY RT High − Off mean " + F(meanDY) + "/255 · pixels darker by >1/255: " + darker);
            M(v.name + ".incidence_deg", inc); M(v.name + ".glass_px", glassPx); M(v.name + ".iou_prepass_forward", iouPF); M(v.name + ".boundary_beyond_1px", beyond);
            M(v.name + ".iou_traced_prepass", iouTP); M(v.name + ".miss_pct", 100.0 * miss / Math.Max(1, glassPx)); M(v.name + ".dY_mean", meanDY); M(v.name + ".darker_px", darker);
            M(v.name + ".high.gpu_trace_ms", high.stats.gpuMsTrace); M(v.name + ".high.gpu_total_ms", high.stats.gpuMsTotal); M(v.name + ".high.frame_gpu_ms", high.frameGpu);
            M(v.name + ".ultra.gpu_trace_ms", ultra.stats.gpuMsTrace); M(v.name + ".ultra.gpu_total_ms", ultra.stats.gpuMsTotal); M(v.name + ".ultra.frame_gpu_ms", ultra.frameGpu);
            M(v.name + ".off.frame_gpu_ms", off.frameGpu); M(v.name + ".high.main_ms", high.stats.mainThreadMs); M(v.name + ".high.encode_ms", high.stats.encodeMs);
            M(v.name + ".tlas", high.stats.tlasInstances); M(v.name + ".lamps", high.stats.lamps);

            // P1-B2: RT radiance vs the planar mirror (both linear HDR, post off) in 64-px blocks
            if (v.name.StartsWith("v2") || v.name.StartsWith("v3") || v.name.StartsWith("v1"))
            {
                RenderMirror(paneR, w.center, -w.dir, rtHdr, false);
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
                Log("  MIRROR REFERENCE " + v.name + ": full 64-px blocks in the glass " + blocks + " · luminance ratio RT/mirror within 0.9-1.1 in " + good
                    + " (" + F(blocks > 0 ? 100.0 * good / blocks : 0) + " %) · mean ratio " + F3(blocks > 0 ? sumRatio / blocks : 0)
                    + " · median " + F3(ratios.Count > 0 ? ratios.OrderBy(x => x).ElementAt(ratios.Count / 2) : 0) + " · within 0.8-1.25: " + ratios.Count(x => x >= .8 && x <= 1.25));
                if (ratios.Count > 0) M(v.name + ".mirror_median_ratio", ratios.OrderBy(x => x).ElementAt(ratios.Count / 2));
                SaveRadiance(mirror.Select((c, i) => high.outPx[i].a > 1f ? new Color(c.r, c.g, c.b, 2f) : new Color(0, 0, 0, 0)).ToArray(), W, H, v.name + "_mirror_radiance.png");
                M(v.name + ".mirror_blocks", blocks); M(v.name + ".mirror_good_pct", blocks > 0 ? 100.0 * good / blocks : 0); M(v.name + ".mirror_mean_ratio", blocks > 0 ? sumRatio / blocks : 0);
                Read8(rt);
            }
        }
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
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

    static (double x, double y, int n) Centroid(byte[] img, bool[] within, Func<int, int, int, bool> pick)
    {
        double sx = 0, sy = 0; var n = 0;
        for (var i = 0; i < within.Length; i++)
        {
            if (!within[i]) continue;
            if (!pick(img[3 * i], img[3 * i + 1], img[3 * i + 2])) continue;
            sx += i % W; sy += i / W; n++;
        }
        return n > 0 ? (sx / n, sy / n, n) : (0, 0, 0);
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
        RenderMirror(w.pane.GetComponent<Renderer>(), w.center, -w.dir, rtHdr, false);
        var mirror = ReadFloat(rtHdr);
        FlipX(mirror, W, H);
        var mirImg = Read8RadianceAsBytes(mirror, glass);
        var rR = Centroid(rtImg, glass, (r, g, b) => r > 120 && g < 60 && b < 60);
        var rM = Centroid(mirImg, glass, (r, g, b) => r > 120 && g < 60 && b < 60);
        var gR = Centroid(rtImg, glass, (r, g, b) => g > 120 && r < 60 && b < 60);
        var gM = Centroid(mirImg, glass, (r, g, b) => g > 120 && r < 60 && b < 60);
        Log("ORIENTATION: red centroid RT (" + F(rR.x) + ", " + F(rR.y) + ") n " + rR.n + " vs mirror (" + F(rM.x) + ", " + F(rM.y) + ") n " + rM.n
            + " · green RT (" + F(gR.x) + ", " + F(gR.y) + ") n " + gR.n + " vs mirror (" + F(gM.x) + ", " + F(gM.y) + ") n " + gM.n
            + " · red is " + (rR.x < gR.x ? "LEFT" : "RIGHT") + " of green in RT, " + (rM.x < gM.x ? "LEFT" : "RIGHT") + " in the mirror; red is " + (rR.y < gR.y ? "BELOW" : "ABOVE") + " green in RT, " + (rM.y < gM.y ? "BELOW" : "ABOVE") + " in the mirror");
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
        Render(rt); var withPanel = Read8(rt);
        panel.SetActive(false); Render(rt); var without = Read8(rt); panel.SetActive(true);
        var c = CaptureRT("P0_occluder.png", FrontRoomsGlassRTQuality.High, 2);
        var panelPx = Diff8(withPanel, without, 3);
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
        Log("OCCLUDER: panel px " + panelCount + " · glass prepass px on the panel " + prepassOnPanel + " · traced px on the panel " + painted + " (interior, excluding the 1 px edge ring: " + interior + ") · prototype 02: 137,551");
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
        Log("RELAY: body copy " + handles.Count + " parts registered via FrontRoomsGlassRT.Register · moved 0.7 m: " + changed + " reflection px changed in the same frame · px differing from a fresh registration at the new pose: " + differ + " (prototype 02: 177,035 stale px)");
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
        Log("BROKEN PANE: shown RT px (weight > 0, coverage) before " + before + " · on the break frame " + after + " (weight " + F(camState.lastWeight) + ", visible receivers " + camState.lastVisibleReceivers
            + ") · receivers registered " + receiversBefore + " -> " + FrontRoomsGlassRTSystem.Receivers + " · hold progress " + F(progress) + " (prototype 02: 159,533 px until the next rescan)");
        M("broken.before_px", before); M("broken.after_px", after);
        yield return 3;
        var c2 = CaptureRT(null, FrontRoomsGlassRTQuality.High, 1);
        Log("BROKEN PANE: 3 frames later shown RT px " + ShownPx(c2) + " · pane object " + (w.pane == null ? "destroyed" : "alive"));
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
        var h = FrontRoomsGlassRT.Register(mr, FrontRoomsGlassRTFlags.None);
        yield return 1;
        int Reds(Capture c)
        {
            var n = 0;
            for (var i = 0; i < c.outPx.Length; i++) { var p = c.outPx[i]; if (p.a > 1f && p.r > .5f && p.g < .1f && p.b < .1f) n++; }
            return n;
        }
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
        Log("STREAM: registered red px " + Reds(a) + " (centroid x " + F(Cx(a)) + ") · after a vertex edit + MeshChanged, same frame: " + Reds(b) + " px (centroid x " + F(Cx(b)) + ", moved " + F(Cx(b) - Cx(a)) + " px)"
            + " · next frame after Destroy: " + Reds(c3) + " px (prototype 02: 17,262 px of a destroyed mesh)");
        M("stream.registered_px", Reds(a)); M("stream.changed_centroid_dx", Cx(b) - Cx(a)); M("stream.destroyed_px", Reds(c3));
    }

    // ---- P1-B4 dark beyond: a black box seen through the pane (one-way mirror)
    static IEnumerator DarkBeyond(WindowSetup w)
    {
        foreach (var vi in new[] { 1, 2 })
        {
            if (vi >= w.views.Length) continue;
            var v = w.views[vi];
            yield return GoTo(v);
            var box = Box("RTVerify dark room", w.center + w.dir * 1.6f, new Vector3(6f, 4f, 6f), Color.black, false);
            box.transform.rotation = Quaternion.LookRotation(w.dir);
            // only the far side: the box starts 0.1 m beyond the pane
            box.transform.position = w.center + w.dir * 3.1f;
            yield return 1;
            CaptureRT("P1_dark_beyond_" + v.name + "_off.png", FrontRoomsGlassRTQuality.Off, 2);
            var h = CaptureRT("P1_dark_beyond_" + v.name + "_high.png", FrontRoomsGlassRTQuality.High, 2);
            CaptureRT("P1_dark_beyond_" + v.name + "_ultra.png", FrontRoomsGlassRTQuality.Ultra, 2);
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
        var lenses = pixelsOf.Where(kv => kv.Value.Count >= 40).Select(kv => (renderer: order[kv.Key], px: kv.Value)).ToList();
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
        for (var k = 0; k < lenses.Count; k++)
        {
            var range = real[k].Max() - real[k].Min();
            var corr = Correlation(real[k], refl[k]);
            sb.Append(" · " + lenses[k].renderer.transform.parent.name + " (" + lenses[k].px.Count + " px): lens " + F3(real[k].Min()) + "-" + F3(real[k].Max()) + ", reflection " + F3(refl[k].Min()) + "-" + F3(refl[k].Max()) + ", r " + F3(corr));
            if (range > bestRange) { bestRange = range; best = k; }
        }
        var bestCorr = best >= 0 ? Correlation(real[best], refl[best]) : double.NaN;
        Log("LAMPS over 120 frames" + sb + " · most-changing lens: correlation " + F3(bestCorr) + " (lens range " + F3(bestRange) + ")");
        M("lamps.lenses", lenses.Count); M("lamps.best_correlation", bestCorr); M("lamps.best_range", bestRange);
        if (best >= 0)
        {
            var t = new StringBuilder("frame\tlens_linear\treflected\n");
            for (var k = 0; k < real[best].Count; k++) t.Append(k).Append('\t').Append(F3(real[best][k])).Append('\t').Append(F3(refl[best][k])).Append('\n');
            File.WriteAllText(Path.Combine(OutDir, "lamp_sync.tsv"), t.ToString());
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
        // still camera: frames converge (mean |Δ| between consecutive frames)
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
        Log("TEMPORAL still: accumulation index now " + st.accumulation + " · mean |Δ| between consecutive frames: first " + F3(deltas.First()) + " · at 8 " + F3(deltas[Math.Min(7, deltas.Count - 1)]) + " · last " + F3(deltas.Last()));
        M("temporal.still_first_delta", deltas.First()); M("temporal.still_last_delta", deltas.Last()); M("temporal.accumulation", st.accumulation);
        Time.captureDeltaTime = 1f / 60f;

        // strafe: temporal std of luminance per glass pixel, averaged over the 10 % highest-gradient pixels
        foreach (var mode in new[] { "unfiltered", "high", "ultra" })
        {
            var keepEdge = FrontRoomsGlassRTSystem.EdgeFilterHigh;
            if (mode == "unfiltered") { FrontRoomsGlassRTSystem.EdgeFilterHigh = 0f; FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High; }
            else FrontRoomsGlassRT.Quality = mode == "high" ? FrontRoomsGlassRTQuality.High : FrontRoomsGlassRTQuality.Ultra;
            var frames = new List<float[]>();
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
                if (f == 15) { Read8(rt); SavePng("P1_edge_crawl_" + mode + ".png"); }
            }
            // per pixel: std of the frame-to-frame change of a moving image ≈ crawl; use |Δ| of consecutive frames at high-gradient pixels
            var grad = new List<(float g, int i)>();
            var f0 = frames[0];
            for (var y = 1; y < H - 1; y++)
            for (var x = 1; x < W - 1; x++)
            {
                var i = y * W + x;
                if (f0[i] < 0 || f0[i + 1] < 0 || f0[i + W] < 0) continue;
                grad.Add((Mathf.Abs(f0[i + 1] - f0[i]) + Mathf.Abs(f0[i + W] - f0[i]), i));
            }
            grad.Sort((a, b) => b.g.CompareTo(a.g));
            var top = grad.Take(Math.Max(1, grad.Count / 10)).Select(t => t.i).ToArray();
            double sum = 0; var cnt = 0;
            foreach (var i in top)
            {
                var vals = frames.Select(fr => fr[i]).ToArray();
                double s2 = 0; var m2 = 0;
                for (var k = 1; k + 1 < vals.Length; k++)
                {
                    if (vals[k - 1] < 0 || vals[k] < 0 || vals[k + 1] < 0) continue;
                    s2 += Math.Abs(vals[k] - .5 * (vals[k - 1] + vals[k + 1])); m2++;
                }
                if (m2 < 10) continue;
                sum += s2 / m2; cnt++;
            }
            var crawl = cnt > 0 ? sum / cnt : 0;
            Log("TEMPORAL strafe 1 cm/frame, " + mode + ": crawl = mean |L(t) - (L(t-1) + L(t+1))/2| at the 10 % highest-contrast reflection pixels " + F3(crawl) + " (" + cnt + " px; a smooth slide gives ~0)");
            M("temporal.crawl_" + mode, crawl);
            FrontRoomsGlassRTSystem.EdgeFilterHigh = keepEdge;
        }
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
        yield return GoTo(v);
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
            + " · trace events +" + (s1.traceEvents - s0.traceEvents) + " (expected 100: game camera only) · game-camera target re-allocations " + (camState.reallocations - re0)
            + " · Scene view: not a Game camera (feature returns before enqueueing)");
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
            Log("CAPACITY " + q + ": registered " + FrontRoomsGlassRTSystem.RegisteredInstances + " · priority instances (receivers, glass, dynamic) " + FrontRoomsGlassRTSystem.LastPriority
                + " all kept · non-priority instances within 20 m dropped " + FrontRoomsGlassRTSystem.LastDroppedNear + " · TLAS instances " + c.stats.tlasInstances + " (cap " + (q == FrontRoomsGlassRTQuality.Ultra ? 4096 : 2048) + ") · glass px " + c.stats.glassPixels
                + " · miss " + c.stats.missPixels + " · AS GPU " + F3(c.stats.gpuMsAccel) + " ms · main " + F3(c.stats.mainThreadMs) + " ms");
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
        Log("FRACTURE: PrepareMeshes 150 ranges (" + idx.Count / 3 + " triangles), notBeforeFrame = now+3 · prepared " + (doneAt >= 0 ? doneAt + " frames after notBeforeFrame" : "NO") + " ·" + log2);
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
                var r = new Dictionary<string, List<double>>();
                var stage = new Dictionary<string, (double tr, double rs, double asx, double main, double enc)>();
                foreach (var mode in new[] { "off", "empty", "high", "ultra" })
                {
                    FrontRoomsGlassRT.Quality = mode == "off" ? FrontRoomsGlassRTQuality.Off : mode == "ultra" ? FrontRoomsGlassRTQuality.Ultra : FrontRoomsGlassRTQuality.High;
                    FrontRoomsMetalGlassRTRendererFeature.HarnessEmptyPassOnly = mode == "empty";
                    var list = new List<double>(); var trs = new List<double>(); var rss = new List<double>(); var ass = new List<double>(); var mains = new List<double>(); var encs = new List<double>();
                    for (var k = 0; k < 18; k++)
                    {
                        var g = RenderTimed(target);
                        if (k < 3) continue;
                        list.Add(g);
                        FrontRoomsGlassRT.TryGetStats(out var s);
                        trs.Add(s.gpuMsTrace); rss.Add(s.gpuMsResolve); ass.Add(s.gpuMsAccel); mains.Add(s.mainThreadMs); encs.Add(s.encodeMs);
                        if (k % 6 == 0) yield return 1;
                    }
                    r[mode] = list;
                    stage[mode] = (Med(trs), Med(rss), Med(ass), Med(mains), Med(encs));
                }
                FrontRoomsMetalGlassRTRendererFeature.HarnessEmptyPassOnly = false;
                Log("COST " + v.name + " " + res + ": whole-frame GPU median off " + F3(Med(r["off"])) + " · empty RT pass split " + F3(Med(r["empty"])) + " (Δ " + F3(Med(r["empty"]) - Med(r["off"])) + ")"
                    + " · High " + F3(Med(r["high"])) + " (Δ " + F3(Med(r["high"]) - Med(r["off"])) + ") · Ultra " + F3(Med(r["ultra"])) + " (Δ " + F3(Med(r["ultra"]) - Med(r["off"])) + ") ms"
                    + " · stage medians High: AS " + F3(stage["high"].asx) + " trace " + F3(stage["high"].tr) + " resolve " + F3(stage["high"].rs) + " · Ultra: AS " + F3(stage["ultra"].asx) + " trace " + F3(stage["ultra"].tr) + " resolve " + F3(stage["ultra"].rs)
                    + " · main thread High " + F3(stage["high"].main) + " ms · encode High " + F3(stage["high"].enc) + " ms");
                var key = "cost." + v.name + "." + res;
                M(key + ".off", Med(r["off"])); M(key + ".empty", Med(r["empty"])); M(key + ".high", Med(r["high"])); M(key + ".ultra", Med(r["ultra"]));
                M(key + ".high.as", stage["high"].asx); M(key + ".high.trace", stage["high"].tr); M(key + ".high.resolve", stage["high"].rs);
                M(key + ".ultra.as", stage["ultra"].asx); M(key + ".ultra.trace", stage["ultra"].tr); M(key + ".ultra.resolve", stage["ultra"].rs);
                M(key + ".high.main", stage["high"].main); M(key + ".high.encode", stage["high"].enc);
            }
        }
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
    }

    // ---- P1-B1: parity, primary rays through the hit shader vs the raster (post off, SSAO off, linear HDR)
    static IEnumerator Parity(WindowSetup w)
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
        // per material class (material index in ids.b): mean |Δ| / raster and p95, luminance
        var perMat = new Dictionary<int, List<double>>();
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
        }
        var sb = new StringBuilder();
        foreach (var kv in perMat.OrderByDescending(k => k.Value.Count).Take(10))
        {
            var l = kv.Value.OrderBy(x => x).ToList();
            sb.Append(" · mat " + kv.Key + ": n " + l.Count + " mean " + F3(l.Average()) + " p95 " + F3(l[(int)(l.Count * .95)]));
            M("parity.mat" + kv.Key + ".mean", l.Average()); M("parity.mat" + kv.Key + ".p95", l[(int)(l.Count * .95)]);
        }
        Log("PARITY (post off, SSAO off, linear): pixels hit " + hits + " of " + raster.Length + " · mean luminance raster " + F3(sumRa / Math.Max(1, hits)) + " vs RT " + F3(sumRT / Math.Max(1, hits))
            + " (ratio " + F3(sumRT / Math.Max(sumRa, 1e-6)) + ")" + sb);
        M("parity.luma_ratio", sumRT / Math.Max(sumRa, 1e-6));
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
        Tone(raster, 0); SaveBytes(outB, "P1_parity_raster.png");
        Tone(parity, 1); SaveBytes(outB, "P1_parity_rt.png");
        for (var i = 0; i < raster.Length; i++)
        {
            var d = ids[i].r > 0 ? Mathf.Clamp01(8f * Mathf.Abs((raster[i].r + raster[i].g + raster[i].b) - (parity[i].r + parity[i].g + parity[i].b)) / 3f) : 0f;
            outB[3 * i] = outB[3 * i + 1] = outB[3 * i + 2] = (byte)(d * 255f);
        }
        SaveBytes(outB, "P1_parity_diff8.png");
        yield return GoTo(v);
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
            RenderMirror(w.pane.GetComponent<Renderer>(), w.center, -w.dir, rt, true); SaveMirrorPng("P1_office_" + v.name + "_ref.png");
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

    static IEnumerator Walk(WindowSetup w)
    {
        FrontRoomsGlassRT.Quality = FrontRoomsGlassRTQuality.High;
        FrontRoomsGlassRTSystem.ResetMaxima();
        var path = WalkPath(w.views[1].feet, 60);
        var frames = new StringBuilder("frame\tmain_ms\tencode_ms\tgpu_total_ms\ttlas\tvisible\ttraced\tchunks\tregistered\tpending\tscene_ms\tlive_ms\tcand_ms\tlamps_ms\tsubmit_ms\n");
        var mains = new List<double>(); var encs = new List<double>(); var blanks = 0; var traced = 0; var visibleFrames = 0;
        var mainsStream = new List<double>(); var mainsQuiet = new List<double>();
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
            Render(rt);
            FrontRoomsGlassRT.TryGetStats(out var s);
            var vis = camState.lastVisibleReceivers;
            var tr = camState.lastWeight > 0f;
            if (vis > 0) { visibleFrames++; if (!tr) blanks++; else traced++; }
            mains.Add(s.mainThreadMs); encs.Add(s.encodeMs);
            var reg = FrontRoomsGlassRTSystem.RegisteredInstances;
            (reg != lastRegistered || FrontRoomsGlassRTSystem.PendingRescans > 0 ? mainsStream : mainsQuiet).Add(s.mainThreadMs);
            lastRegistered = reg;
            foreach (Transform c in map.transform) built.Add(c.GetInstanceID());
            var b = FrontRoomsGlassRTSystem.Breakdown;
            frames.Append(f).Append('\t').Append(F3(s.mainThreadMs)).Append('\t').Append(F3(s.encodeMs)).Append('\t').Append(F3(s.gpuMsTotal)).Append('\t').Append(s.tlasInstances)
                  .Append('\t').Append(vis).Append('\t').Append(tr ? 1 : 0).Append('\t').Append(FrontRoomsGlassRTSystem.KnownChunks).Append('\t').Append(reg)
                  .Append('\t').Append(s.meshesPending).Append('\t').Append(F3(b[0])).Append('\t').Append(F3(b[1])).Append('\t').Append(F3(b[2])).Append('\t').Append(F3(b[3])).Append('\t').Append(F3(b[4])).Append('\n');
        }
        File.WriteAllText(Path.Combine(OutDir, "walk_frames.tsv"), frames.ToString());
        FrontRoomsGlassRT.TryGetStats(out var end);
        Log("WALK 600 frames at 4 m/s along a " + path.Count + "-cell path (" + F(pathLen) + " m, ping-pong): chunk roots seen " + built.Count + " (start " + chunks0 + ", end " + FrontRoomsGlassRTSystem.KnownChunks + ")"
            + " · frames with a visible receiver " + visibleFrames + " · traced " + traced + " · BLANK " + blanks
            + " · main-thread ms p50 " + F3(Pct(mains, .5)) + " p99 " + F3(Pct(mains, .99)) + " max " + F3(mains.Max())
            + " · quiet frames p99 " + F3(mainsQuiet.Count > 0 ? Pct(mainsQuiet, .99) : 0) + " (" + mainsQuiet.Count + ") · streaming frames p99 " + F3(mainsStream.Count > 0 ? Pct(mainsStream, .99) : 0) + " (" + mainsStream.Count + ")"
            + " · encode ms p50 " + F3(Pct(encs, .5)) + " p99 " + F3(Pct(encs, .99)) + " · command-buffer errors " + end.commandBufferErrors + " · pool-busy frames " + FrontRoomsGlassRTNative.Get(FrontRoomsGlassRTNative.Stat.PoolBusy));
        M("walk.chunks_seen", built.Count); M("walk.visible_frames", visibleFrames); M("walk.blank", blanks);
        M("walk.main_p50", Pct(mains, .5)); M("walk.main_p99", Pct(mains, .99)); M("walk.main_max", mains.Max()); M("walk.encode_p99", Pct(encs, .99)); M("walk.cb_errors", end.commandBufferErrors);
        if (mainsQuiet.Count > 0) M("walk.main_quiet_p99", Pct(mainsQuiet, .99));
        if (mainsStream.Count > 0) M("walk.main_stream_p99", Pct(mainsStream, .99));
    }
}
#endif
