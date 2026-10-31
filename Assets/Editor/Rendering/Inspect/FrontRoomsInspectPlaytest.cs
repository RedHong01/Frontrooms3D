// EXAMINE play-mode harness (research/inspect/10_design.md §7.1 file 9; AT-01 c-p, AT-05, AT-04 clip, AT-01g).
// Plays Assets/Scenes/FrontRooms3D.unity, starts a run through the game's own title handoff (seed fixed by
// Random.InitState(4242)), and drives the game with FrontRoomsInput.InjectSnapshot (E, S, Esc, R, mouse), never
// the autopilot (the game gates close-ups off under it). The game camera renders into a 1920x1080 target, so
// every solve the game makes sees 16:9 / 1080 rows; 4K frames are a second render of the same pose.
// Frames and logs: Verification/inspect/play/ (PNG + frames.txt + playtest.json + log.txt).
// Run:     Unity -batchmode -projectPath <clone> -buildTarget OSXUniversal -executeMethod FrontRoomsInspectPlaytest.RunBatch -logFile <f>
// Pause neutrality (AT-01g, three replays of one seed): -executeMethod FrontRoomsInspectPlaytest.RunNeutralityBatch
// Camera motion and the Relay are set by reflection on the clone only; nothing writes PlayerPrefs.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using FrontRooms.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using I = FrontRoomsShotTimings.Inspect;

[InitializeOnLoad]
public static class FrontRoomsInspectPlaytest
{
    const string ScenePath = "Assets/Scenes/FrontRooms3D.unity";
    const string ActiveKey = "FrontRooms.InspectPlay.Active", DeadlineKey = "FrontRooms.InspectPlay.Deadline", DoneKey = "FrontRooms.InspectPlay.Done", ModeKey = "FrontRooms.InspectPlay.Mode";
    const BindingFlags BF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    const float Step = 1f / 60f;
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    static int waitUntilFrame = -1;
    static bool finished;
    static string outDir;
    static readonly StringBuilder log = new StringBuilder();
    static readonly StringBuilder frames = new StringBuilder();
    static readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();

    [Serializable]
    sealed class Result
    {
        public string verdict;
        public int passed, failed;
        public List<string> checks = new List<string>();
        public List<string> numbers = new List<string>();
    }
    static Result result = new Result();

    // FrontRoomsInspect.ZoomInputWired as the game set it before Main (C7 sets it true in Game.Awake): PlacardScenario
    // forces false for its v1 half and true for its wired half; Main's end (or Finish, on an early end) puts it back.
    static bool? zoomWiredSaved;
    static FrontRooms3DGame game;
    static FrontRoomsMapWorld map;
    static FrontRoomsCameraRig rig;
    static Camera cam;
    static RenderTexture rt1080, rt4k;
    static int inspectEnded, inspectStarted, doorMoved, fixtureChanged, lampDipped, heldReturned, openedEvents, inspectingTrue, inspectingFalse, closedEvents;

    static FrontRoomsInspectPlaytest()
    {
        if (SessionState.GetBool(ActiveKey, false)) EditorApplication.update += Tick;
    }

    public static void RunBatch() => Begin("main", 2700f);
    public static void RunNeutralityBatch() => Begin("neutrality", 4200f);

    static void Begin(string mode, float seconds)
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetString(ModeKey, mode);
        SessionState.SetFloat(DeadlineKey, (float)EditorApplication.timeSinceStartup + seconds);
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }

    static string Mode => SessionState.GetString(ModeKey, "main");

    static string OutDir
    {
        get
        {
            if (outDir == null)
            {
                outDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "inspect", Mode == "main" ? "play" : "neutrality");
                Directory.CreateDirectory(outDir);
            }
            return outDir;
        }
    }

    static void Log(string s)
    {
        var line = "[" + Time.frameCount + " t=" + Time.time.ToString("0.000", Inv) + "] " + s;
        log.AppendLine(line);
        Debug.Log("[InspectPlay] " + line);
        try { File.WriteAllText(Path.Combine(OutDir, "log.txt"), log.ToString()); } catch { }
    }

    static void Check(bool ok, string what)
    {
        if (ok) result.passed++; else result.failed++;
        result.checks.Add((ok ? "ok   " : "FAIL ") + what);
        Log((ok ? "ok   " : "FAIL ") + what);
        Save();
    }

    static void Note(string what) { result.numbers.Add(what); Log("note " + what); Save(); }

    static void Save()
    {
        result.verdict = (result.failed == 0 && result.passed > 0 ? "PASS" : "FAIL") + ": " + result.passed + " passed, " + result.failed + " failed";
        try
        {
            File.WriteAllText(Path.Combine(OutDir, "playtest.json"), JsonUtility.ToJson(result, true));
            File.WriteAllText(Path.Combine(OutDir, "frames.txt"), frames.ToString());
        }
        catch { }
    }

    static void Finish()
    {
        finished = true;
        RestoreZoomWired();
        Time.captureDeltaTime = 0f;
        try { FrontRoomsInput.ClearInjectedSnapshot(); } catch { }
        SessionState.SetBool(DoneKey, true);
        Save();
        Log("FINISHED " + result.verdict);
    }

    static void Tick()
    {
        var timedOut = EditorApplication.timeSinceStartup > SessionState.GetFloat(DeadlineKey, float.MaxValue);
        if (finished || timedOut || SessionState.GetBool(DoneKey, false))
        {
            if (timedOut && !finished && !SessionState.GetBool(DoneKey, false)) { Log("TIMEOUT"); Check(false, "harness finished before its deadline"); Finish(); }
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
            stack.Push(Mode == "neutrality" ? Neutrality() : Main());
            Log("routine created (" + Mode + ")");
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
            Check(false, "harness exception: " + e.GetType().Name + " " + e.Message);
            Finish();
        }
    }

    // ---------------------------------------------------------------- reflection
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
    static T Get<T>(object o, string n) { var v = Get(o, n); return v is T t ? t : default; }
    static void Set(object o, string n, object v) => FI(o.GetType(), n).SetValue(o, v);
    static object Call(object o, string n, params object[] a) => o.GetType().GetMethod(n, BF).Invoke(o, a);
    static string Phase => Get(game, "phase").ToString();
    // The prompt the game shows for its aim (verb + object): the game's own prompt string. promptText draws it while the
    // game owns the line; with HUD key B attached (contract HK1) the key HUD draws the READ / EXAMINE line from this same
    // string and the game's promptText is empty, so the checks read the source and pass with and without HUD B.
    static string ShownPrompt => Get<string>(game, "prompt") ?? "";
    // The prompt as DRAWN (engineering verify 2026-10-29 #4): with HUD key B attached and owning the line (HK1), the key
    // HUD's line 1 this frame; otherwise the game's promptText. Read at rest only (the key HUD fades its line in over
    // <= 0.30 s and out over its leave, the map's promptText snaps), so the range / hysteresis checks keep ShownPrompt.
    static string DrawnPrompt
    {
        get
        {
            var hud = Get<FrontRoomsKeyHud>(game, "keyHud");
            if (hud != null && hud.OwnsPrompt) return hud.Line1Drawn;
            var t = Get<Text>(game, "promptText");
            return t != null ? t.text : "";
        }
    }
    static string PromptOwner => Get<FrontRoomsKeyHud>(game, "keyHud") is FrontRoomsKeyHud h && h.OwnsPrompt ? "drawn by the key HUD" : "drawn by promptText";
    static Transform PlayerRoot => Get<Transform>(game, "playerRoot");
    static FrontRoomsMapHunter Relay => Get<FrontRoomsMapHunter>(game, "relay");
    static string V(Vector3 v) => "(" + v.x.ToString("0.000", Inv) + ", " + v.y.ToString("0.000", Inv) + ", " + v.z.ToString("0.000", Inv) + ")";
    static string F(float f, string fmt = "0.000") => f.ToString(fmt, Inv);

    static void SetMotion(int percent) =>
        typeof(FrontRoomsSettings).GetProperty("CameraMotionPercent", BindingFlags.Public | BindingFlags.Static)?.GetSetMethod(true)?.Invoke(null, new object[] { percent });

    // ---------------------------------------------------------------- input
    static void Inject(Vector2 move = default, Vector2 look = default, bool useDown = false, bool useHeld = false, bool pauseDown = false, bool restartDown = false, bool shotBackDown = false) =>
        FrontRoomsInput.InjectSnapshot(new FrontRoomsInput.FrameSnapshot(move, look, false, useDown, useHeld, pauseDown, false, false, restartDown, shotBackDown));
    static void Idle() => Inject();

    /// <summary>E down for one frame, held for two more, then released.</summary>
    static IEnumerator PressE()
    {
        Inject(useDown: true, useHeld: true);
        yield return 1;
        Inject(useHeld: true);
        yield return 2;
        Idle();
        yield return 1;
    }

    // ---------------------------------------------------------------- capture
    static void EnsureRTs()
    {
        if (rt1080 == null) { rt1080 = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4, name = "Inspect 1080p" }; rt1080.Create(); }
        if (rt4k == null) { rt4k = new RenderTexture(3840, 2160, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4, name = "Inspect 4K" }; rt4k.Create(); }
    }

    static int shotIndex;
    const int HudLayer = 31;

    static Texture2D Read(RenderTexture rt, TextureFormat format)
    {
        var active = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, format, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = active;
        return tex;
    }

    static float ToLin(float c) => c <= .04045f ? c / 12.92f : Mathf.Pow((c + .055f) / 1.055f, 2.4f);
    static float ToSrgb(float c) { c = Mathf.Clamp01(c); return c <= .0031308f ? c * 12.92f : 1.055f * Mathf.Pow(c, 1f / 2.4f) - .055f; }

    /// <summary>
    /// The HUD the way the screen shows it (平面视觉's method, research/desktop_ui/README.md): every overlay canvas moved
    /// onto a post-free camera at the capture size (so UpdateHud lays it out for 1080 rows), drawn alone over
    /// transparent black, then composited over the post-processed frame in linear light. v1 put the HUD under the post
    /// (ScreenSpaceCamera at 0.07 m): the DOF blurred the hint and the prompt lost brightness (verifier 2).
    /// </summary>
    static void CompositeHud(Texture2D scene, RenderTexture size, string uiFile)
    {
        var go = new GameObject("Inspect HUD camera");
        var hc = go.AddComponent<Camera>();
        hc.enabled = false;
        hc.clearFlags = CameraClearFlags.SolidColor;
        hc.backgroundColor = new Color(0f, 0f, 0f, 0f);
        hc.cullingMask = 1 << HudLayer;
        hc.nearClipPlane = .1f; hc.farClipPlane = 10f;
        hc.allowHDR = false; hc.allowMSAA = false;
        var data = UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(hc);
        data.renderPostProcessing = false;
        data.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.None;
        data.renderShadows = false;
        var uiRT = new RenderTexture(size.width, size.height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 1, name = "inspect ui" };
        uiRT.Create();
        hc.targetTexture = uiRT;
        var restore = new List<Action>();
        foreach (var c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!c.isRootCanvas || !c.isActiveAndEnabled || c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            var canvas = c; var mode = c.renderMode; var wc = c.worldCamera; var plane = c.planeDistance;
            foreach (var t in c.GetComponentsInChildren<Transform>(true)) { var g = t.gameObject; var layer = g.layer; restore.Add(() => { if (g != null) g.layer = layer; }); g.layer = HudLayer; }
            c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = hc; c.planeDistance = 1f;
            restore.Add(() => { canvas.renderMode = mode; canvas.worldCamera = wc; canvas.planeDistance = plane; });
        }
        Canvas.ForceUpdateCanvases();
        try { Call(game, "UpdateHud"); } catch (Exception e) { Log("UpdateHud for the capture: " + e.GetType().Name); }
        Canvas.ForceUpdateCanvases();
        hc.Render();
        Canvas.ForceUpdateCanvases();
        hc.Render();
        var ui = Read(uiRT, TextureFormat.RGBA32);
        if (uiFile != null) File.WriteAllBytes(Path.Combine(OutDir, uiFile), ui.EncodeToPNG());   // the UI layer alone (alpha), for the glyph measurements
        for (var i = restore.Count - 1; i >= 0; i--) restore[i]();
        Canvas.ForceUpdateCanvases();
        try { Call(game, "UpdateHud"); } catch { }
        hc.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(go);
        uiRT.Release();
        var sp = scene.GetPixels32();
        var up = ui.GetPixels32();
        for (var i = 0; i < sp.Length; i++)
        {
            var a = up[i].a / 255f;
            if (a <= 0f && up[i].r == 0 && up[i].g == 0 && up[i].b == 0) continue;
            sp[i] = new Color32(
                (byte)Mathf.RoundToInt(255f * ToSrgb(ToLin(sp[i].r / 255f) * (1f - a) + ToLin(up[i].r / 255f))),
                (byte)Mathf.RoundToInt(255f * ToSrgb(ToLin(sp[i].g / 255f) * (1f - a) + ToLin(up[i].g / 255f))),
                (byte)Mathf.RoundToInt(255f * ToSrgb(ToLin(sp[i].b / 255f) * (1f - a) + ToLin(up[i].b / 255f))), 255);
        }
        scene.SetPixels32(sp);
        scene.Apply();
        UnityEngine.Object.DestroyImmediate(ui);
    }

    static string Capture(string name, string note, bool hud, bool fourK = false)
    {
        EnsureRTs();
        var target = fourK ? rt4k : rt1080;
        var prev = cam.targetTexture;
        cam.targetTexture = target;
        cam.Render();
        cam.Render();
        var tex = Read(target, TextureFormat.RGBA32);
        cam.targetTexture = prev;
        if (hud) CompositeHud(tex, target, "ui_" + shotIndex.ToString("00") + "_" + name + (fourK ? "_4k" : "") + ".png");
        var file = shotIndex.ToString("00") + "_" + name + (fourK ? "_4k" : "") + ".png";
        shotIndex++;
        File.WriteAllBytes(Path.Combine(OutDir, file), tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        var prompt = Get<Text>(game, "promptText");
        var promptRect = prompt != null ? prompt.rectTransform.anchoredPosition : Vector2.zero;
        frames.AppendLine(file + " | frame " + Time.frameCount + " | t " + F(Time.time) + " | phase " + Phase + " | timeScale " + F(Time.timeScale, "0.##")
            + " | cam " + V(cam.transform.position) + " fwd " + V(cam.transform.forward) + " fov " + F(cam.fieldOfView, "0.00")
            + " | prompt \"" + (prompt != null ? prompt.text : "") + "\" a " + (prompt != null ? F(prompt.color.a, "0.00") : "-")
            + " | shot w " + (FrontRoomsInspect.Shot != null ? F(FrontRoomsInspect.Shot.EasedWeight) : "-") + " | look Shot " + F(FrontRoomsInspectLook.ShotWeight) + " Adapt " + F(FrontRoomsInspectLook.Adapt)
            + " lift " + F(FrontRoomsInspectLook.LiftEv, "0.00") + " EV W " + F(FrontRoomsInspectLook.CellLight) + " own " + F(FrontRoomsInspectLook.OwnLight) + " | StartLampsNorth " + F(map.StartLampsNorth, "0.00")
            + " | " + note + (boxNote.Length > 0 ? " | boxes " + boxNote : ""));
        boxNote = "";
        Save();
        return file;
    }

    /// <summary>Boxes (sheet mm -> viewport) logged with the next capture, for the contrast script (lines.py).</summary>
    static string boxNote = "";

    /// <summary>Log the viewport rects of sheet-mm boxes on the placard for the next capture (x, y, w, h from the sheet's top-left).</summary>
    static void PlacardBoxes(FrontRoomsInspectable placard, params object[] namedBoxes)
    {
        var sb = new StringBuilder();
        var f = placard.Face;
        var right = f.rotation * Vector3.right; var up = f.rotation * Vector3.up;
        var sx = Vector3.Dot(right, cam.transform.right) >= 0f ? 1f : -1f;   // viewer right in face axes
        for (var i = 0; i + 4 < namedBoxes.Length; i += 5)
        {
            var name = (string)namedBoxes[i];
            float x = (float)namedBoxes[i + 1], y = (float)namedBoxes[i + 2], w = (float)namedBoxes[i + 3], h = (float)namedBoxes[i + 4];
            var pts = new Vector3[4];
            for (var k = 0; k < 4; k++)
            {
                var mx = x + ((k & 1) == 0 ? 0f : w) - 216f;
                var my = 139.5f - (y + ((k & 2) == 0 ? 0f : h));
                pts[k] = f.position + right * (sx * mx * .001f) + up * (my * .001f);
            }
            float x0 = 1, x1 = 0, y0 = 1, y1 = 0;
            foreach (var pt in pts) { var v = cam.WorldToViewportPoint(pt); x0 = Mathf.Min(x0, v.x); x1 = Mathf.Max(x1, v.x); y0 = Mathf.Min(y0, v.y); y1 = Mathf.Max(y1, v.y); }
            sb.Append(name + "=" + F(x0, "0.0000") + "," + F(1f - y1, "0.0000") + "," + F(x1, "0.0000") + "," + F(1f - y0, "0.0000") + ";");
        }
        boxNote = sb.ToString();
    }

    // Sheet-mm boxes on tutorial sheet A and its Phase 0 (平面视觉's tutorial_placard.swift art, 432 x 279 mm), measured on
    // the 6 px/mm print and glow mask on 2026-10-22; every box holds for both variants.
    static readonly object[] SheetBoxes =
    {
        "title", 21f, 21f, 200f, 17f,          // EVACUATION PLAN, 15 mm caps
        "paper", 122f, 115f, 36f, 22f,         // blank paper inside the plan rectangle
        "youarehere", 116f, 70.5f, 56f, 8f,    // YOU ARE HERE (red ink, not glowing)
        "glow_dot", 106f, 71.7f, 6.7f, 6.7f,   // the YOU ARE HERE dot (glow ink)
        "glow_exit", 259f, 112.5f, 17f, 5.5f,  // EXIT over the stair door (glow ink, 5 mm)
        "glow_paper", 259f, 105f, 17f, 6f,     // blank paper just above it
        "line1", 26f, 222.5f, 79f, 6.5f,       // 1 WALK, DO NOT RUN. (5.0 mm, white on red)
        "fineprint", 140.5f, 212f, 142f, 5f,   // ON ALARM, A FLOOR WARDEN CHECKS EVERY ROOM. (3.6 mm)
        "legend", 296f, 46f, 118f, 158f,       // the legend region (examine.json)
        "plan", 20f, 46f, 268f, 158f,          // the plan region (examine.json)
    };

    /// <summary>The viewport rect of a world point list (for the crops; logged with the frame).</summary>
    static string ScreenRect(params Vector3[] world)
    {
        float x0 = 1, x1 = 0, y0 = 1, y1 = 0;
        foreach (var w in world)
        {
            var v = cam.WorldToViewportPoint(w);
            x0 = Mathf.Min(x0, v.x); x1 = Mathf.Max(x1, v.x); y0 = Mathf.Min(y0, v.y); y1 = Mathf.Max(y1, v.y);
        }
        return "viewport x " + F(x0) + "–" + F(x1) + " y " + F(y0) + "–" + F(y1);
    }

    // ---------------------------------------------------------------- placement
    static void Place(Vector3 feet, Vector3 lookAt)
    {
        var root = PlayerRoot;
        var body = Get<CharacterController>(game, "playerBody");
        body.enabled = false;
        root.position = feet;
        body.enabled = true;
        Physics.SyncTransforms();
        var eye = root.position + Vector3.up * ModuleUnits.PlayerEye;
        var d = lookAt - eye;
        var yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        var pitch = -Mathf.Asin(Mathf.Clamp(d.y / Mathf.Max(1e-4f, d.magnitude), -1f, 1f)) * Mathf.Rad2Deg;
        Set(game, "yaw", yaw);
        Set(game, "pitch", pitch);
        root.rotation = Quaternion.Euler(0f, yaw, 0f);
        rig.SetBase(pitch);
    }

    static bool FreeStanding(Vector3 feet)
    {
        var r = ModuleUnits.PlayerRadius;
        var root = PlayerRoot;
        foreach (var c in Physics.OverlapCapsule(feet + Vector3.up * (r + .15f), feet + Vector3.up * (ModuleUnits.PlayerHeight - r), r, ~0, QueryTriggerInteraction.Ignore))
            if (root == null || !c.transform.IsChildOf(root)) return false;
        return true;
    }

    static Vector3 Feet(Pose face, float distance, float yawDeg)
    {
        var n = face.rotation * Vector3.forward;
        n.y = 0f;
        n.Normalize();
        var dir = Quaternion.AngleAxis(yawDeg, Vector3.up) * n;
        var p = face.position + dir * distance;
        p.y = PlayerRoot.position.y;
        return p;
    }

    // ---------------------------------------------------------------- world state (freeze and neutrality)
    sealed class WorldState
    {
        public float time, elapsed, relayStateTime;
        public int tier, keys;
        public Vector3 relayPos, playerPos;
        public string relayState;
        public readonly SortedDictionary<string, float> lamps = new SortedDictionary<string, float>(StringComparer.Ordinal);
        public readonly SortedDictionary<string, float> doors = new SortedDictionary<string, float>(StringComparer.Ordinal);
        public readonly SortedDictionary<string, float> glow = new SortedDictionary<string, float>(StringComparer.Ordinal);
    }

    static WorldState Snapshot()
    {
        var s = new WorldState
        {
            time = Time.time,
            elapsed = Get<float>(game, "elapsed"),
            tier = Get<int>(game, "tier"),
            keys = Get<int>(game, "keysTaken"),
            playerPos = PlayerRoot.position,
        };
        var relay = Relay;
        if (relay != null) { s.relayPos = relay.Position; s.relayState = relay.State.ToString(); s.relayStateTime = relay.StateTime; }
        if (Get(map, "fixtureByCell") is IDictionary fixtures)
            foreach (DictionaryEntry e in fixtures)
            {
                var level = FI(e.Value.GetType(), "level")?.GetValue(e.Value);
                if (level is float f) s.lamps[e.Key.ToString()] = f;
            }
        if (Get(map, "doorByEdge") is IDictionary doors)
            foreach (DictionaryEntry e in doors)
            {
                var door = e.Value as FrontRoomsMapWorld.Door;
                if (door != null) s.doors[e.Key.ToString()] = door.angle + 1000f * door.progress + (door.open ? 1e6f : 0f);
            }
        foreach (var glow in UnityEngine.Object.FindObjectsByType<FrontRoomsPlacardGlow>(FindObjectsSortMode.None))
            foreach (var f in glow.GetType().GetFields(BF))
                if (f.FieldType == typeof(float)) s.glow[f.Name] = (float)f.GetValue(glow);
        return s;
    }

    static string Diff(WorldState a, WorldState b, bool includeTime)
    {
        var sb = new StringBuilder();
        if (includeTime && Math.Abs(a.time - b.time) > 1e-6f) sb.Append("Time.time " + F(a.time) + "→" + F(b.time) + "; ");
        if (Math.Abs(a.elapsed - b.elapsed) > 1e-6f) sb.Append("elapsed " + F(a.elapsed) + "→" + F(b.elapsed) + "; ");
        if (a.tier != b.tier) sb.Append("tier; ");
        if ((a.relayPos - b.relayPos).sqrMagnitude > 1e-12f) sb.Append("relay pos " + V(a.relayPos) + "→" + V(b.relayPos) + "; ");
        if (a.relayState != b.relayState) sb.Append("relay state " + a.relayState + "→" + b.relayState + "; ");
        if (Math.Abs(a.relayStateTime - b.relayStateTime) > 1e-6f) sb.Append("relay StateTime " + F(a.relayStateTime) + "→" + F(b.relayStateTime) + "; ");
        var lampDiff = 0; foreach (var kv in a.lamps) if (b.lamps.TryGetValue(kv.Key, out var v) && Math.Abs(v - kv.Value) > 1e-6f) lampDiff++;
        if (lampDiff > 0) sb.Append(lampDiff + " lamp levels; ");
        var doorDiff = 0; foreach (var kv in a.doors) if (b.doors.TryGetValue(kv.Key, out var v) && Math.Abs(v - kv.Value) > 1e-6f) doorDiff++;
        if (doorDiff > 0) sb.Append(doorDiff + " doors; ");
        var glowDiff = 0; foreach (var kv in a.glow) if (b.glow.TryGetValue(kv.Key, out var v) && Math.Abs(v - kv.Value) > 1e-6f) glowDiff++;
        if (glowDiff > 0) sb.Append(glowDiff + " placard glow fields; ");
        return sb.ToString();
    }

    // ---------------------------------------------------------------- setup
    static void Subscribe()
    {
        FrontRoomsCameraRig.ShotStarted += (k, f) => { if (k == ShotKind.Inspect) inspectStarted++; };
        FrontRoomsCameraRig.ShotEnded += (k, f) => { if (k == ShotKind.Inspect) inspectEnded++; };
        FrontRoomsInspect.HeldReturned += () => heldReturned++;
        FrontRoomsInspect.Opened += t => openedEvents++;
        FrontRoomsInspect.Closed += t => closedEvents++;
        var inspecting = typeof(FrontRooms3DGame).GetEvent("Inspecting", BindingFlags.Public | BindingFlags.Static);
        Action<bool> onInspecting = b => { if (b) inspectingTrue++; else inspectingFalse++; };
        inspecting?.AddEventHandler(null, onInspecting);
        map.DoorMoved += _ => doorMoved++;
        map.FixtureChanged += (_, __) => fixtureChanged++;
        map.LampDipped += (_, __) => lampDipped++;
    }

    static IEnumerator StartRun()
    {
        ShaderUtil.allowAsyncCompilation = false;
        while ((game = UnityEngine.Object.FindFirstObjectByType<FrontRooms3DGame>()) == null) yield return 1;
        cam = Get<Camera>(game, "cam");
        EnsureRTs();
        cam.targetTexture = rt1080;   // the game's solves and zoom see 1920 x 1080
        Log("game found · phase " + Phase + " · screen " + Screen.width + "x" + Screen.height + " · camera target " + cam.pixelWidth + "x" + cam.pixelHeight + " aspect " + F(cam.aspect) + " near " + F(cam.nearClipPlane) + " fov " + F(cam.fieldOfView, "0.0")
            + " · gfx " + SystemInfo.graphicsDeviceType);
        while (Time.time < 2.0f) yield return 1;
        UnityEngine.Random.InitState(4242);
        for (var tries = 0; tries < 60 && !Get<bool>(game, "mapPlay"); tries++)
        {
            Call(game, "RequestTitleStart");
            if (!Get<bool>(game, "mapPlay")) yield return 15;
        }
        map = Get<FrontRoomsMapWorld>(game, "map");
        rig = Get<FrontRoomsCameraRig>(game, "rig");
        Log("run started · seed " + Get<int>(game, "runSeed") + " · map root " + V(map.transform.position));
        var tuning = Get<FrontRoomsHunterTuning>(game, "hunterTuning");
        tuning.releaseDelaySeconds = 1e6f;   // the Relay stays dormant until a test places it
        var start = Time.frameCount;
        while (!map.Settled && Time.frameCount - start < 1500) yield return 1;
        Log("map settled " + map.Settled + " after " + (Time.frameCount - start) + " frames");
        // Fix 2026-10-14 (verifier 2): open the start door the way a player does, from inside the stream rooms, and wait
        // for the map's north lamps to come up (StartLampsNorth 0 -> 1 over 0.6 s). v1 went on with the door shut, so
        // every lamp past the door line was held dark and the close-ups showed the dark state.
        var door = (Vector3)Get(game, "startDoorPoint");
        Place(new Vector3(door.x, PlayerRoot.position.y, door.z - 1.6f), door + Vector3.forward * 3f + Vector3.up * 1.6f);
        var t0 = Time.time;
        while (!Get<bool>(game, "startDoorOpened") && Time.time - t0 < 15f) yield return 1;
        var t1 = Time.time;
        while (map.StartLampsNorth < .999f && Time.time - t1 < 5f) yield return 1;
        Log("start door opened " + Get<bool>(game, "startDoorOpened") + " after " + F(t1 - t0, "0.00") + " s; StartLampsNorth " + F(map.StartLampsNorth) + ", StartLampsSouth " + F(map.StartLampsSouth));
        yield return 10;
    }

    static FrontRoomsInspectable FindKit(string kit)
    {
        foreach (var t in UnityEngine.Object.FindObjectsByType<FrontRoomsInspectable>(FindObjectsSortMode.None))
            if (t.Kit == kit) return t;
        return null;
    }

    static bool WallAt(Vector3 from, Vector3 dir, float max, out RaycastHit hit)
    {
        var hits = Physics.RaycastAll(from, dir, max, ~0, QueryTriggerInteraction.Ignore);
        hit = default;
        var best = float.MaxValue;
        foreach (var h in hits)
        {
            if (PlayerRoot != null && h.collider.transform.IsChildOf(PlayerRoot)) continue;
            if (h.distance < best) { best = h.distance; hit = h; }
        }
        // A wall face parallel to the facade, never a door leaf.
        return best < float.MaxValue && Vector3.Dot(hit.normal, -dir) > .97f && !map.IsOpenDoorLeaf(hit.collider) && !(Get(map, "doorByCollider") is IDictionary leaves && leaves.Contains(hit.collider));
    }

    /// <summary>The first wall spot along the facade at these offsets from the door (both sides, nearest first).</summary>
    static bool FacadeSpot(Vector3 door, float side, Vector3 n, float faceZ, float y, float[] offsets, Vector3 keepAway, out RaycastHit hit)
    {
        foreach (var o in offsets)
            foreach (var s in new[] { -side, side })
            {
                var probe = new Vector3(door.x + s * o, y, faceZ) + n * 1.0f;
                if (!WallAt(probe, -n, 2f, out hit) || !FreeStanding(new Vector3(probe.x, PlayerRoot.position.y, probe.z))) continue;
                // Never on or beside the placard (it has no collider; the ray finds the wall behind it).
                var flat = hit.point - keepAway; flat.y = 0f;
                if (flat.magnitude < .8f) continue;
                return true;
            }
        hit = default;
        return false;
    }

    /// <summary>A stand at distance d and ±yaw off the face where the body fits and the face centre is in plain sight.</summary>
    static bool ClearStand(Pose face, float d, float yaw, out Vector3 feet)
    {
        foreach (var sgn in new[] { 1f, -1f })
        {
            feet = Feet(face, d, yaw * sgn);
            if (!FreeStanding(feet)) continue;
            var eye = feet + Vector3.up * ModuleUnits.PlayerEye;
            var blocked = false;
            foreach (var h in Physics.RaycastAll(eye, face.position - eye, Vector3.Distance(eye, face.position) - .02f, ~0, QueryTriggerInteraction.Ignore))
                if (!h.collider.transform.IsChildOf(PlayerRoot)) { blocked = true; break; }
            if (!blocked) return true;
        }
        feet = default;
        return false;
    }

    /// <summary>The test bed on the facade on the door's other side: the door sign (R5-B) at 1.524 m and a hanging key + ring + tag at 1.40 m.</summary>
    static (FrontRoomsInspectable sign, FrontRoomsInspectable key, Transform keyRoot) SpawnTestBed(FrontRoomsInspectable placard)
    {
        var face = placard.Face;
        var n = face.rotation * Vector3.forward;
        var door = (Vector3)Get(game, "startDoorPoint");
        var side = Mathf.Sign(Vector3.Dot(face.position - door, Vector3.right));
        var floorY = PlayerRoot.position.y;
        FrontRoomsInspectable sign = null, key = null;
        Transform keyRoot = null;
        var holder = new GameObject("INSPECT TEST BED").transform;
        // Door sign: on the facade on the door's other side first (found by a ray), 1.524 m up.
        var signProbe = new Vector3(door.x - side * 1.6f, floorY + 1.524f, face.position.z) + n * 1.0f;
        if (FacadeSpot(door, side, n, face.position.z, floorY + 1.524f, new[] { 1.6f, 2.0f, 2.6f, 3.2f, 3.8f }, face.position, out var signHit))
        {
            var go = FrontRoomsKitLibrary.Spawn("Kit_DoorSign", holder, Vector3.zero, Quaternion.identity, null, false, "test door sign");
            if (go != null)
            {
                go.transform.SetPositionAndRotation(signHit.point + signHit.normal * .0005f, Quaternion.LookRotation(signHit.normal, Vector3.up));
                sign = go.GetComponent<FrontRoomsInspectable>();
                Log("test bed: door sign at " + V(go.transform.position) + " on " + signHit.collider.name + (sign != null ? " (inspectable)" : " (NO inspectable)"));
            }
        }
        else Log("test bed: no wall for the door sign at " + V(signProbe));
        // Key + ring + tag, hanging from a point 2.3 m beyond the door on the same side as the sign, 1.40 m up.
        var keyProbe = new Vector3(door.x - side * 2.4f, floorY + 1.40f, face.position.z) + n * 1.0f;
        if (FacadeSpot(door, side, n, face.position.z, floorY + 1.40f, new[] { 3.0f, 3.6f, 4.2f, 2.4f, 4.8f }, face.position, out var keyHit) && (signHit.collider == null || Mathf.Abs(keyHit.point.x - signHit.point.x) > .5f))
        {
            keyRoot = new GameObject("test key assembly").transform;
            keyRoot.SetParent(holder, false);
            keyRoot.SetPositionAndRotation(keyHit.point + keyHit.normal * .006f, Quaternion.LookRotation(keyHit.normal, Vector3.up));
            var ringInfo = FrontRoomsKitLibrary.GetInfo("Kit_KeyRing");
            ringInfo.TryAnchor("key_contact", out var keyContact);
            ringInfo.TryAnchor("tag_contact", out var tagContact);
            FrontRoomsKitLibrary.Spawn("Kit_KeyRing", keyRoot, Vector3.zero, Quaternion.identity, null, false, "ring");
            // The key hangs from the ring by its bow hole; the tag by its hole (anchors: Kit_Key_Zone ring_hole, Kit_KeyTag_Rect hole).
            FrontRoomsKitLibrary.GetInfo("Kit_Key_Zone").TryAnchor("ring_hole", out var keyHole);
            var keyRot = Quaternion.Euler(0f, 0f, 90f) * Quaternion.Euler(0f, -90f, 0f);   // flat (local X) to the camera, blade (local +Z) down
            FrontRoomsKitLibrary.Spawn("Kit_Key_Zone", keyRoot, keyContact - keyRot * keyHole, keyRot, null, false, "key");
            var tagGo = FrontRoomsKitLibrary.Spawn("Kit_KeyTag_Rect", keyRoot, tagContact, Quaternion.identity, null, false, "tag");
            key = tagGo != null ? tagGo.GetComponent<FrontRoomsInspectable>() : null;
            if (key != null) key.Display = keyRoot;
            Log("test bed: key assembly at " + V(keyRoot.position) + (key != null ? " (tag inspectable, Display = assembly)" : " (NO inspectable)"));
        }
        else Log("test bed: no wall for the key at " + V(keyProbe));
        Physics.SyncTransforms();
        return (sign, key, keyRoot);
    }

    // ---------------------------------------------------------------- main
    static IEnumerator Main()
    {
        zoomWiredSaved = FrontRoomsInspect.ZoomInputWired;
        Note("zoom input as the game set it: " + (FrontRoomsInspect.ZoomInputWired ? "wired (C7)" : "not wired (v1)") + "; restored at the end");
        foreach (var old in Directory.GetFiles(OutDir, "*.png")) File.Delete(old);
        yield return StartRun();
        if (!Get<bool>(game, "mapPlay")) { Check(false, "a run started"); yield break; }
        Time.captureDeltaTime = Step;
        SetMotion(100);
        Subscribe();
        Idle();
        FrontRoomsInspect.DevTargets = true;   // the test bed (ship: false) registers
        var placard = FindKit("Kit_EvacPlacard");
        Check(placard != null, "the placard registers (Kit_EvacPlacard, ship true) " + (placard != null ? placard.Id + " at " + V(placard.Face.position) : ""));
        if (placard == null) yield break;
        var mount = UnityEngine.Object.FindFirstObjectByType<FrontRoomsPlacardMount>();
        if (mount != null) Note("placard mount " + FrontRoomsPlacard.Label(mount.Spot.mount) + " side " + mount.Spot.side + ", glow cell " + mount.Spot.glowCell);
        var (sign, key, keyRoot) = SpawnTestBed(placard);
        yield return 2;

        yield return PlacardScenario(placard);
        yield return LampStateScenario(placard);
        yield return ExitAndGateScenarios(placard);
        yield return MotionScenarios(placard);
        if (sign != null) yield return FlatScenario(sign, "sign", 1.0f); else Check(false, "test door sign placed");
        if (key != null) yield return KeyScenario(key, keyRoot); else Check(false, "test key assembly placed");
        yield return ClipScenario(placard, 50);
        if (sign != null) yield return ClipScenario(sign, 50);
        if (key != null) yield return ClipScenario(key, 50);
        yield return RelayFreezeScenario(placard);
        // A43: the Relay walking off from the freeze scenario never wanders into the live close-ups below (parked: dormant).
        Relay?.Park();
        if (sign != null) yield return ReframeScenario(sign, placard); else Check(false, "reframe: the test door sign is placed");
        yield return A43Scenario(placard, sign, key, keyRoot);
        RestoreZoomWired();
        Note("events: Inspect shots started " + inspectStarted + ", ended " + inspectEnded + "; Inspecting(true) " + inspectingTrue + ", (false) " + inspectingFalse);
        Check(inspectStarted == inspectEnded && inspectingTrue == inspectingFalse, "every Inspect shot ended once; Inspecting(true) and (false) pair up");
        Log("DONE");
    }

    // The game's own ZoomInputWired back (C7: true), never forced to false; once.
    static void RestoreZoomWired()
    {
        if (!zoomWiredSaved.HasValue) return;
        FrontRoomsInspect.ZoomInputWired = zoomWiredSaved.Value;
        zoomWiredSaved = null;
    }

    static float PxPerMm(float d) => FrontRoomsInspectFraming.PxPerMm(d, cam.fieldOfView, 1080f);

    // The world as it stood at the end of the commit frame (the last frame the game ran), and the event counters from then.
    static WorldState entrySnap;

    static IEnumerator OpenOn(FrontRoomsInspectable t, string label, bool captureMid, bool waitArrival = true)
    {
        var t0 = Time.time;
        Inject(useDown: true, useHeld: true);
        yield return 1;
        Inject(useHeld: true);
        var commitFrame = -1;
        for (var i = 0; i < 4 && commitFrame < 0; i++)
        {
            if (Phase == "Inspect") commitFrame = Time.frameCount;
            else yield return 1;
        }
        // A43: a reading close-up pauses (timeScale 0); a looking one runs live (1) once the map's A43 contract is in the game.
        var live = ContractLive && !FrontRoomsInspect.CurrentPauses;
        Check(Phase == "Inspect" && Time.timeScale == (live ? 1f : 0f), label + ": E commits the close-up: phase Inspect, timeScale " + F(Time.timeScale, "0.##") + (live ? " (A43: looking, live)" : ""));
        Check(Math.Abs(Time.time - t0) < 2.5f * Step, label + ": Time.time advanced " + F((Time.time - t0) * 1000f, "0.0") + " ms over the entry (< 2 frames)");
        Idle();
        entrySnap = Snapshot();
        doorMoved = fixtureChanged = lampDipped = 0;
        if (!waitArrival) yield break;
        var shot = FrontRoomsInspect.Shot;
        var arrivalAge = -1f;
        var marks = new[] { .10f, .22f, .33f };
        var mark = 0;
        for (var i = 0; i < 90 && shot != null; i++)
        {
            if (captureMid && mark < marks.Length && FrontRoomsInspect.Age >= marks[mark] - 1e-4f)
            {
                Capture(label + "_in_" + (marks[mark] * 100f).ToString("00", Inv), "transition in, inspect clock " + F(FrontRoomsInspect.Age, "0.00") + " s", true);
                mark++;
            }
            if (shot.Weight >= 1f) { arrivalAge = FrontRoomsInspect.Age; break; }
            yield return 1;
        }
        var solved = FrontRoomsInspect.LastSolve;
        Note(label + ": arrived at " + F(arrivalAge, "0.00") + " s on the inspect clock; d " + F(solved.distance) + " m, composition " + (solved.compositionB ? "B" : "A") + ", FOV " + F(cam.fieldOfView, "0.0")
             + ", camera off the solved pose by " + F((cam.transform.position - solved.pose.position).magnitude * 1000f, "0.00") + " mm");
        var snap = Snapshot();
        var d = Diff(entrySnap, snap, true);
        if (live)
            Check(snap.time > entrySnap.time && snap.elapsed > entrySnap.elapsed && !map.DoorsPaused, label + ": A43 live: the world ran from the commit frame to the arrival (game time +" + F(snap.elapsed - entrySnap.elapsed, "0.00") + " s, doors not paused)");
        else
            Check(d.Length == 0 && doorMoved == 0 && fixtureChanged == 0 && lampDipped == 0, label + ": nothing in the world moved from the commit frame to the arrival (" + snap.lamps.Count + " lamps, " + snap.doors.Count + " doors)" + (d.Length > 0 ? " — " + d : ""));
    }

    static IEnumerator ExitWith(string how, string label)
    {
        var tBefore = Time.time;
        if (how == "E") Inject(useDown: true, useHeld: true);
        else if (how == "S") Inject(move: new Vector2(0f, -1f), shotBackDown: true);
        else if (how == "Esc") Inject(pauseDown: true);
        yield return 1;
        if (how == "E") Inject(useHeld: true); else if (how == "S") Inject(move: new Vector2(0f, -1f)); else Idle();
        Check(Phase == "Playing" && Time.timeScale == 1f, label + ": " + how + " exits: phase " + Phase + ", timeScale " + F(Time.timeScale, "0.##"));
        Check(Time.time - tBefore < 2.5f * Step, label + ": Time.time advanced " + F((Time.time - tBefore) * 1000f, "0.0") + " ms across the exit frame (< 2 frames)");
    }

    // ---------------------------------------------------------------- the placard (AT-01 c, d, f, h; captures)
    static IEnumerator PlacardScenario(FrontRoomsInspectable placard)
    {
        // v1 first (no zoom input), whatever the game set (C7 wires it in Game.Awake); the wired half below sets it true.
        FrontRoomsInspect.ZoomInputWired = false;
        var face = placard.Face;
        // Before the prompt: 2.4 m (out of reach), then the 1.5 m spot (prompt).
        Place(Feet(face, 2.4f, 0f), face.position);
        yield return 3;
        var far = ShownPrompt;
        Capture("placard_before_prompt_2p4m", "2.4 m square-on: out of reach (1.6 m): no prompt", true);
        Check(string.IsNullOrEmpty(far), "no prompt at 2.4 m (\"" + far + "\")");
        Place(Feet(face, 1.5f, 0f), face.position);
        yield return 3;
        Capture("placard_prompt_1p5m", "1.5 m square-on: prompt", true);
        Check(ShownPrompt == placard.Prompt && placard.Prompt == FrontRoomsInspectCatalog.PromptReadPlan, "prompt at 1.5 m: \"" + ShownPrompt + "\" (verb + object)");
        yield return 20;   // at rest: the key HUD's line is in by 0.30 s (grammar section 2)
        Check(DrawnPrompt == placard.Prompt, "prompt at 1.5 m as drawn: \"" + DrawnPrompt + "\" (" + PromptOwner + ")");
        // Prompt range and hysteresis (AT-01c): shows at 1.6 m and 60°, not at 1.7 m or 65° on approach.
        Place(Feet(face, 1.75f, 0f), face.position); yield return 2;
        var at175 = ShownPrompt;
        Place(Feet(face, 1.58f, 0f), face.position); yield return 2;
        var at158 = ShownPrompt;
        Place(Feet(face, 1.66f, 0f), face.position); yield return 2;
        var at166 = ShownPrompt;
        Check(string.IsNullOrEmpty(at175) && at158 == placard.Prompt && at166 == placard.Prompt,
            "prompt range: none at 1.75 m, shown at 1.58 m, kept at 1.66 m by the hysteresis (\"" + at175 + "\" / \"" + at158 + "\" / \"" + at166 + "\")");
        if (ClearStand(face, 1.2f, 58f, out var feet58) && ClearStand(face, 1.2f, 70f, out var feet70))
        {
            Place(feet58, face.position); yield return 2;
            var at58 = ShownPrompt;
            if (at58 == placard.Prompt) Capture("placard_prompt_58deg", "1.2 m at 58° off the face: prompt", true);
            Place(feet70, face.position); yield return 2;
            var at70 = ShownPrompt;
            Check(at58 == placard.Prompt && string.IsNullOrEmpty(at70), "approach angle: shown at 58°, none at 70° (\"" + at58 + "\" / \"" + at70 + "\")");
        }
        else Note("approach angle: no clear 58° / 70° stand at 1.2 m in this layout (walls); covered by the EditMode Find test");

        // Enter from 1.5 m square-on; transition frames; arrival.
        Place(Feet(face, 1.5f, 0f), face.position);
        yield return 6;
        yield return OpenOn(placard, "placard", true);
        var before = entrySnap;
        var solved = FrontRoomsInspect.LastSolve;
        var corners = Corners(placard);
        var px = PxPerMm(solved.distance);
        Note("placard base: d " + F(solved.distance) + " m, FOV " + F(cam.fieldOfView, "0.0") + " → " + F(px, "0.00") + " px/mm at 1080p (" + F(px * 2f, "0.00") + " at 4K); title 15 mm = " + F(15f * px, "0.0") + " px, numbered alarm lines 5.0 mm = " + F(5f * px, "0.0") + " px, smallest cap " + F(placard.Def.SmallestCapMm, "0.0") + " mm = " + F(placard.Def.SmallestCapMm * px, "0.0") + " px; frame " + ScreenRect(corners));
        Capture("placard_closeup", "placard close-up, base (composition " + (solved.compositionB ? "B" : "A") + ", d " + F(solved.distance) + "), hint on", true);
        PlacardBoxes(placard, SheetBoxes);
        Capture("placard_closeup_clean", "placard close-up, base, no HUD", false);
        PlacardBoxes(placard, SheetBoxes);
        Capture("placard_closeup", "placard close-up, base, 4K render (crop in post: " + LegendRect(placard) + ")", false, true);
        Check(AllOnScreen(Corners8(placard)), "placard frame: all 8 frame-box corners on screen at 1080p (" + ScreenRect(Corners8(placard)) + ")");
        // Look at base: ±3°, shrunk so the frame box never leaves the screen (fix: v1 let the left edge go ~100 px off).
        var lookOk = true;
        foreach (var dir in new[] { 1f, -1f })
        {
            for (var i = 0; i < 10; i++) { Inject(look: new Vector2(dir, 0f)); yield return 1; }
            Idle();
            yield return 20;
            var yawOff = Vector3.SignedAngle(solved.pose.rotation * Vector3.forward, cam.transform.forward, Vector3.up);
            var limit = dir > 0f ? solved.lookMax.x : solved.lookMin.x;
            var on = AllOnScreen(Corners8(placard));
            Note("base look " + (dir > 0f ? "right" : "left") + ": clamps at " + F(yawOff, "0.00") + " deg (limit " + F(limit, "0.00") + "), frame box " + ScreenRect(Corners8(placard)));
            lookOk &= Mathf.Abs(yawOff - limit) < .1f && on && Mathf.Abs(limit) <= 3.001f;
            for (var i = 0; i < 10; i++) { Inject(look: new Vector2(-dir, 0f)); yield return 1; }
            Idle();
            yield return 20;
        }
        Check(lookOk, "base look clamps at the solved limits (yaw " + F(solved.lookMin.x, "0.00") + " to " + F(solved.lookMax.x, "0.00") + " deg, within ±3) and all 8 frame-box corners stay on screen");
        // V1-2: no zoom input in v1, so no zoom step and no CLOSER hint over a 5 s hold.
        var sawCloser = false; var hintsSeen = "";
        for (var i = 0; i < 300; i++)
        {
            var h = FrontRoomsInspect.HintText;
            if (h == FrontRoomsInspectCatalog.HintCloser || h == FrontRoomsInspectCatalog.HintCloserTouch) sawCloser = true;
            if (h.Length > 0 && !hintsSeen.Contains(h)) hintsSeen += "\"" + h + "\" ";
            yield return 1;
        }
        Check(!FrontRoomsInspect.ZoomInputWired && !FrontRoomsInspect.ZoomStep.offered && !sawCloser, "zoom not wired (v1): step offered " + FrontRoomsInspect.ZoomStep.offered + ", hints over 5 s: " + hintsSeen + "(no CLOSER)");
        Capture("placard_hint_back", "hint after 5 s of the first close-up (overlay HUD, linear composite)", true);
        yield return ExitWith("E", "placard (no zoom)");
        Idle();
        yield return WaitShotEnd();
        // The zoom with its input wired (touch T3 + the map chat's C7 follow-up, simulated here).
        FrontRoomsInspect.ZoomInputWired = true;
        Place(Feet(face, 1.5f, 0f), face.position);
        yield return 6;
        yield return OpenOn(placard, "placard (zoom wired)", false);
        before = entrySnap;
        Check(FrontRoomsInspect.ZoomStep.offered, "zoom wired: the step is offered");
        // Zoom step (desktop: the angle binds, so the dolly scales with the sidecar's smallest cap: 2.4 mm -> 0.184 m on
        // the v3 sheet, tutorial sheet A's 2.8 mm -> 0.215 m).
        FrontRoomsInspect.SetZoomTarget(true);
        yield return 40;
        var zoomD = Vector3.Dot(cam.transform.position - placard.Face.position, placard.Face.rotation * Vector3.forward);
        var zpx = PxPerMm(zoomD);
        var zCap = placard.Def.SmallestCapMm > 0f ? placard.Def.SmallestCapMm : 2.4f;
        var zExpect = .184f * zCap / 2.4f;
        Check(Mathf.Abs(FrontRoomsInspect.ZoomStep.distance - zExpect) < .003f && Mathf.Abs(zoomD - FrontRoomsInspect.ZoomStep.distance) < .002f,
            "zoom step: dolly to " + F(zoomD) + " m (step " + F(FrontRoomsInspect.ZoomStep.distance) + ", expect " + F(zExpect) + " for the " + F(zCap, "0.0") + " mm cap), FOV " + F(cam.fieldOfView, "0.0"));
        var tpm = placard.Def.TexelsPerMetre;
        Note("placard zoom: " + F(zpx, "0.00") + " px/mm at 1080p, " + F(zCap, "0.0") + " mm cap = " + F(zCap * zpx, "0.0") + " px (1080p) / " + F(2f * zCap * zpx, "0.0") + " px (4K); texel magnification " + F(zpx * 1000f / tpm, "0.00") + "x (1080p), " + F(zpx * 2000f / tpm, "0.00") + "x (4K) on " + F(tpm, "0") + " px/m");
        // The zoom frames the region nearest the base view's centre: all of it on screen, its bottom >= 40 px above the hint.
        var lv = FramedRegionViewport(placard, out var lvId);
        var hintTop = (72f + 16f) / 1080f;
        Check(lv.xMin >= 0f && lv.xMax <= 1f && lv.yMax <= 1f && lv.yMin * 1080f >= hintTop * 1080f + 40f - 1f,
            "zoom frames the " + lvId + " region: viewport x " + F(lv.xMin) + "-" + F(lv.xMax) + ", y " + F(lv.yMin) + "-" + F(lv.yMax) + " (centre " + F(lv.center.x) + ", " + F(lv.center.y) + "); bottom " + F(lv.yMin * 1080f - hintTop * 1080f, "0") + " px above the hint top");
        PlacardBoxes(placard, SheetBoxes);
        Capture("placard_zoom", "zoom step, the legend framed (" + LegendRect(placard) + ")", true);
        PlacardBoxes(placard, SheetBoxes);
        Capture("placard_zoom", "zoom step, 4K render", false, true);
        // Pan while zoomed.
        for (var i = 0; i < 12; i++) { Inject(look: new Vector2(-2f, .5f)); yield return 1; }
        Idle();
        yield return 20;
        Capture("placard_zoom_pan", "zoom step after a pan (mouse left/up)", false);
        FrontRoomsInspect.SetZoomTarget(false);
        yield return 40;
        // Freeze over a 10 s close-up (AT-01f): 600 frames of the inspect clock.
        var held = Snapshot();
        for (var i = 0; i < 600; i++) yield return 1;
        var after = Snapshot();
        var d = Diff(before, after, true);
        Check(d.Length == 0 && doorMoved == 0 && fixtureChanged == 0 && lampDipped == 0,
            "freeze over 10 s: lamps (" + after.lamps.Count + "), doors (" + after.doors.Count + "), Relay, placard glow, elapsed, Time.time unchanged; DoorMoved " + doorMoved + ", FixtureChanged " + fixtureChanged + ", LampDipped " + lampDipped + (d.Length > 0 ? " — " + d : ""));
        Note("freeze snapshot: " + after.lamps.Count + " lamps, " + after.doors.Count + " doors, " + after.glow.Count + " glow fields, elapsed " + F(after.elapsed) + " s, Time.time " + F(after.time) + " s, inspect age " + F(FrontRoomsInspect.Age, "0.00") + " s");
        Capture("placard_hold_10s", "after 10 s held (frozen world; the adapt and the hint have run out)", true);
        // Exit with E: control at once, rotation home in 0.15 s, position in 0.40 s; ShotEnded once.
        var endedBefore = inspectEnded;
        yield return ExitWith("E", "placard");
        float rotHome = -1f, posHome = -1f;
        var exitMarks = new[] { .05f, .15f, .30f };
        var em = 0;
        for (var i = 1; i <= 60; i++)
        {
            Inject(useHeld: true);   // E still held: latched, opens nothing
            yield return 1;
            var age = i * Step;
            var eye = rig.BaseEye;
            if (rotHome < 0f && Quaternion.Angle(cam.transform.rotation, eye.rotation) < .05f) rotHome = age;
            if (posHome < 0f && (cam.transform.position - eye.position).magnitude < 1e-3f) posHome = age;
            if (em < exitMarks.Length && age >= exitMarks[em] - 1e-4f) { Capture("placard_out_" + (exitMarks[em] * 100f).ToString("00", Inv), "transition out, " + F(age, "0.00") + " s after the exit (E held)", true); em++; }
            if (posHome >= 0f && rotHome >= 0f && i > 30) break;
        }
        Idle();
        Check(rotHome > 0f && Mathf.Abs(rotHome - .15f) <= .034f, "exit: rotation home at " + F(rotHome) + " s (0.15 ± 0.02, +1 frame)");
        Check(posHome > 0f && Mathf.Abs(posHome - .40f) <= .034f, "exit: position home at " + F(posHome) + " s (0.40 ± 0.02, +1 frame)");
        Check(inspectEnded - endedBefore == 1 && Phase == "Playing" && !FrontRoomsInspect.Active, "exit: ShotEnded(Inspect) once; E held after the exit opened nothing (phase " + Phase + ")");
        Check(doorMoved == 0, "exit: the exit E used no door (DoorMoved " + doorMoved + ")");
        yield return 30;
    }

    // ---------------------------------------------------------------- lamp states (AT-01e / AT-01j; verifier issue 3 / V2 blockers)
    static float Lvl(GridCoord c) { var l = map.LampLevel(c); return l; }

    static IEnumerator LampStateScenario(FrontRoomsInspectable placard)
    {
        var face = placard.Face;
        var n = face.rotation * Vector3.forward;
        var own = map.CellOf(face.position + n * FrontRoomsInspectLook.LightProbeAhead);
        var nbs = new[] { new GridCoord(own.x + 1, own.y), new GridCoord(own.x - 1, own.y), new GridCoord(own.x, own.y + 1), new GridCoord(own.x, own.y - 1) };
        var mount = UnityEngine.Object.FindFirstObjectByType<FrontRoomsPlacardMount>();
        var glowCell = mount != null ? mount.Spot.glowCell : own;
        Check(glowCell.Equals(own), "the adapt reads the placard's glow cell: the cell 0.3 m in front of the face " + own + " = glow cell " + glowCell + " (mount " + (mount != null ? FrontRoomsPlacard.Label(mount.Spot.mount) : "?") + ")");
        string Levels() => "own " + F(Lvl(own), "0.00") + ", neighbours " + F(Lvl(nbs[0]), "0.00") + " " + F(Lvl(nbs[1]), "0.00") + " " + F(Lvl(nbs[2]), "0.00") + " " + F(Lvl(nbs[3]), "0.00");
        Note("lamp states: as built, " + Levels() + "; mode " + map.LampModeOf(own) + "; StartLampsNorth " + F(map.StartLampsNorth, "0.00"));
        var envelope = new FrontRoomsMapWorld.LampEnvelope(.05f, .05f);
        foreach (var state in new[] { "lit", "own_off", "dead" })
        {
            var handles = new List<int>();
            if (state == "lit")
            {
                map.SetLampMode(own, ModuleLamp.Steady);
                if (Lvl(own) < 0f) Note("lamp lit: the glow cell has no lamp (module Off): the lit state cannot be forced here");
            }
            else
            {
                handles.Add(map.SetLampOverride(own, FrontRoomsMapWorld.LampFx.Dip, 0f, float.PositiveInfinity, envelope));
                if (state == "dead") foreach (var c in nbs) handles.Add(map.SetLampOverride(c, FrontRoomsMapWorld.LampFx.Dip, 0f, float.PositiveInfinity, envelope));
            }
            Place(Feet(face, 1.5f, 0f), face.position);
            for (var i = 0; i < 600; i++) yield return 1;   // 10 s of game time: the ink charges and its gate opens (A4)
            var glow = mount != null ? mount.Glow : null;
            Note("lamp " + state + " (10 s): " + Levels() + (glow != null ? "; glow shown " + F(glow.Shown) + ", charge " + F(glow.Charge) : ""));
            yield return OpenOn(placard, "lamp " + state, false);
            for (var i = 0; i < 180; i++) yield return 1;   // the adapt settles (tau 0.6 s)
            var lift = FrontRoomsInspectLook.LiftEv;
            Note("lamp " + state + " close-up: W " + F(FrontRoomsInspectLook.CellLight) + ", own " + F(FrontRoomsInspectLook.OwnLight) + ", adapt " + F(FrontRoomsInspectLook.Adapt) + " -> lift " + F(lift, "0.00") + " EV, contrast +" + F(FrontRoomsInspectLook.Adapt * FrontRoomsInspectLook.AdaptContrast, "0.0"));
            if (state == "lit") Check(lift < .01f || Lvl(own) < .45f, "lamp lit: no lift (" + F(lift, "0.00") + " EV): the lit legend stays as faint as the spec intends");
            else if (state == "own_off") Check(lift >= 1f, "lamp own off (A4): lift " + F(lift, "0.00") + " EV (>= 1)");
            else Check(lift >= FrontRoomsInspectLook.AdaptMaxEv - .05f, "lamp dead area: lift " + F(lift, "0.00") + " EV (the cap, " + F(FrontRoomsInspectLook.AdaptMaxEv, "0.0") + ")");
            PlacardBoxes(placard, SheetBoxes);
            Capture("placard_lamp_" + state, "lamp state " + state + " (" + Levels() + "), base close-up, no HUD", false);
            PlacardBoxes(placard, SheetBoxes);
            Capture("placard_lamp_" + state, "lamp state " + state + ", 4K", false, true);
            if (state != "lit")
            {
                FrontRoomsInspect.SetZoomTarget(true);
                yield return 40;
                PlacardBoxes(placard, SheetBoxes);
                Capture("placard_lamp_" + state + "_zoom", "lamp state " + state + ", zoom step", false);
                FrontRoomsInspect.SetZoomTarget(false);
                yield return 40;
            }
            yield return ExitWith("E", "lamp " + state);
            Idle();
            yield return WaitShotEnd();
            foreach (var h in handles) map.RemoveLampOverride(h);
            if (state == "lit") map.SetLampMode(own, ModuleLamp.Auto);
            for (var i = 0; i < 120; i++) yield return 1;
        }
    }

    static Vector3[] Corners(FrontRoomsInspectable t)
    {
        var f = t.Face; var s = t.Size;
        var r = f.rotation * Vector3.right * (s.x * .5f); var u = f.rotation * Vector3.up * (s.y * .5f);
        return new[] { f.position + r + u, f.position - r + u, f.position + r - u, f.position - r - u };
    }

    /// <summary>The 8 corners of the framed box (frame side walls included: DepthFront / DepthBack along the normal).</summary>
    static Vector3[] Corners8(FrontRoomsInspectable t)
    {
        var f = t.Face; var s = t.Size;
        var r = f.rotation * Vector3.right * (s.x * .5f); var u = f.rotation * Vector3.up * (s.y * .5f); var n = f.rotation * Vector3.forward;
        var list = new List<Vector3>();
        foreach (var depth in new[] { t.DepthFront, t.DepthBack })
            foreach (var c in new[] { r + u, -r + u, r - u, -r - u }) list.Add(f.position + c + n * depth);
        return list.ToArray();
    }

    /// <summary>
    /// The viewport rect (y up) of the sidecar region the zoom framed: the one whose centre sits nearest the zoom's aim
    /// point (the screen centre raised by half the hint band). Tutorial sheet A has four regions (plan, legend, alarm,
    /// title block); v3 had the legend only.
    /// </summary>
    static Rect FramedRegionViewport(FrontRoomsInspectable placard, out string id)
    {
        id = "-";
        var best = new Rect(0f, 0f, 0f, 0f);
        var bestD = float.PositiveInfinity;
        var aim = new Vector2(.5f, .5f + .5f * FrontRoomsInspect.HintBand);
        var regions = placard.Def.regions;
        var f = placard.Face;
        var right = f.rotation * Vector3.right; var up = f.rotation * Vector3.up;
        var sx = Vector3.Dot(right, cam.transform.right) >= 0f ? 1f : -1f;
        for (var ri = 0; regions != null && ri < regions.Length; ri++)
        {
            if (!placard.Def.TryRegion(ri, placard.Size, out var c, out var size)) continue;
            float x0 = 1, x1 = 0, y0 = 1, y1 = 0;
            for (var k = 0; k < 4; k++)
            {
                var p = f.position + right * (sx * (c.x + ((k & 1) == 0 ? -.5f : .5f) * size.x)) + up * (c.y + ((k & 2) == 0 ? -.5f : .5f) * size.y);
                var v = cam.WorldToViewportPoint(p);
                x0 = Mathf.Min(x0, v.x); x1 = Mathf.Max(x1, v.x); y0 = Mathf.Min(y0, v.y); y1 = Mathf.Max(y1, v.y);
            }
            var r = Rect.MinMaxRect(x0, y0, x1, y1);
            var d = (r.center - aim).sqrMagnitude;
            if (d >= bestD) continue;
            bestD = d;
            best = r;
            id = regions[ri] != null && !string.IsNullOrEmpty(regions[ri].id) ? regions[ri].id : ri.ToString(Inv);
        }
        return best;
    }

    static bool AllOnScreen(Vector3[] world)
    {
        foreach (var w in world)
        {
            var v = cam.WorldToViewportPoint(w);
            if (v.z <= 0f || v.x < 0f || v.x > 1f || v.y < 0f || v.y > 1f) return false;
        }
        return true;
    }

    static string LegendRect(FrontRoomsInspectable placard)
    {
        var info = FrontRoomsKitLibrary.GetInfo("Kit_EvacPlacard");
        if (info == null || !info.TryAnchor("legend_centre", out var lc)) return "no legend anchor";
        var c = placard.transform.TransformPoint(lc);
        var v = cam.WorldToViewportPoint(c);
        return "legend centre at viewport (" + F(v.x) + ", " + F(v.y) + ")";
    }

    // ---------------------------------------------------------------- exits, gates, pause card (AT-01 h, l; AT-05)
    static IEnumerator WaitShotEnd(int max = 60)
    {
        for (var i = 0; i < max && (rig.InShot || FrontRoomsInspect.Active); i++) yield return 1;
        yield return 20;   // the 0.30 s cooldown has run
    }

    static IEnumerator ExitAndGateScenarios(FrontRoomsInspectable placard)
    {
        var face = placard.Face;
        Place(Feet(face, 1.5f, 0f), face.position);
        yield return 4;
        // S exit: no walking backward while S is held.
        yield return OpenOn(placard, "S-exit", false);
        yield return 20;
        var feet0 = PlayerRoot.position;
        yield return ExitWith("S", "S-exit");
        for (var i = 0; i < 30; i++) { Inject(move: new Vector2(0f, -1f)); yield return 1; }
        var moved = (PlayerRoot.position - feet0).magnitude;
        Idle();
        Check(moved < .002f, "S exit: S held for 0.5 s after the exit moved the player " + F(moved * 1000f, "0.0") + " mm (latched)");
        yield return 2;
        for (var i = 0; i < 6; i++) { Inject(move: new Vector2(0f, -1f)); yield return 1; }
        Idle();
        Check((PlayerRoot.position - feet0).magnitude > .01f, "S released then pressed again: the player walks backward (" + F((PlayerRoot.position - feet0).magnitude * 1000f, "0") + " mm)");
        yield return WaitShotEnd();
        // Transient gate: during the return, the prompt stays but a fresh E is swallowed.
        Place(Feet(face, 1.5f, 0f), face.position);
        yield return 4;
        yield return OpenOn(placard, "return-swallow", false);
        yield return 20;
        yield return ExitWith("E", "return-swallow");
        Idle(); yield return 2;   // E released: the latch clears
        var promptDuringReturn = ShownPrompt;
        Inject(useDown: true, useHeld: true); yield return 1; Idle(); yield return 2;
        Check(Phase == "Playing" && rig.InShot, "a fresh E during the 0.40 s return is swallowed (phase " + Phase + ", prompt \"" + promptDuringReturn + "\")");
        yield return WaitShotEnd();
        // Esc: ignored before 0.20 s, exits after it; never the pause card.
        Place(Feet(face, 1.5f, 0f), face.position);
        yield return 4;
        yield return OpenOn(placard, "Esc", false, false);
        Inject(pauseDown: true); yield return 1; Idle();
        var early = Phase;
        var earlyAge = FrontRoomsInspect.Age;
        yield return 14;
        yield return ExitWith("Esc", "Esc after 0.20 s");
        Idle();
        Check(early == "Inspect" && Phase == "Playing", "Esc at " + F(earlyAge, "0.00") + " s is ignored (" + early + "), after 0.20 s it exits (" + Phase + "), no pause card");
        yield return WaitShotEnd();
        // Focus loss → Paused from Inspect; Esc → the same close-up; R ignored in Inspect.
        Place(Feet(face, 1.5f, 0f), face.position);
        yield return 4;
        yield return OpenOn(placard, "focus", false);
        yield return 30;
        Call(game, "OnApplicationFocus", false);
        yield return 2;
        var paused = Phase; var pausedScale = Time.timeScale; var pausedFrom = Get(game, "pausedFrom")?.ToString();
        Inject(pauseDown: true); yield return 1; Idle(); yield return 2;
        var resumed = Phase;
        Check(paused == "Paused" && pausedScale == 0f && pausedFrom == "Inspect" && resumed == "Inspect" && Time.timeScale == 0f,
            "focus loss in a close-up → Paused (from " + pausedFrom + ", timeScale " + F(pausedScale, "0.##") + "); Esc → back to the close-up (" + resumed + ")");
        var gameBefore = game;
        Inject(restartDown: true); yield return 1; Idle(); yield return 3;
        Check(Phase == "Inspect" && game == gameBefore && game != null, "R in a close-up is ignored (no restart)");
        yield return ExitWith("E", "focus");
        Idle();
        yield return WaitShotEnd();
        Inject(pauseDown: true); yield return 1; Idle(); yield return 2;
        var p1 = Phase;
        Inject(pauseDown: true); yield return 1; Idle(); yield return 2;
        Check(p1 == "Paused" && Phase == "Playing" && Time.timeScale == 1f, "after a close-up: Playing → Esc → Paused → Esc → Playing (no stale pausedFrom; timeScale " + F(Time.timeScale, "0.##") + ")");
        // Hard gates (AT-05): climb, pull-clear, a chase → no prompt and E opens nothing.
        Place(Feet(face, 1.5f, 0f), face.position);
        yield return 3;
        var canShow = (Func<FrontRoomsInspectable, bool>)Delegate.CreateDelegate(typeof(Func<FrontRoomsInspectable, bool>), game, typeof(FrontRooms3DGame).GetMethod("CanShowInspect", BF));
        Check(canShow(placard), "gate: allowed when calm");
        Set(game, "climbTime", .3f);
        var climb = canShow(placard);
        Set(game, "climbTime", -1f);
        Set(game, "pullClear", (Vector3?)PlayerRoot.position);
        var pull = canShow(placard);
        Set(game, "pullClear", null);
        var relay = Relay;
        var setState = typeof(FrontRoomsMapHunter).GetMethod("SetState", BF);
        setState.Invoke(relay, new object[] { HunterState.Chase });
        var chase = canShow(placard);
        setState.Invoke(relay, new object[] { HunterState.BreakDoor });
        var breakDoor = canShow(placard);
        setState.Invoke(relay, new object[] { HunterState.Dormant });
        Check(!climb && !pull && !chase && !breakDoor && canShow(placard), "gate: no prompt during a climb, a pull-clear step, a chase or a door break; back after (climb " + climb + ", pull " + pull + ", chase " + chase + ", break " + breakDoor + ")");
        // Autopilot / baseline gate.
        Set(game, "autopilot", true);
        var auto = canShow(placard);
        Set(game, "autopilot", false);
        Check(!auto, "gate: never opens under the autopilot");
        yield return 2;
    }

    // ---------------------------------------------------------------- Camera motion 50 / Off (AT-01 i)
    static IEnumerator MotionScenarios(FrontRoomsInspectable placard)
    {
        var face = placard.Face;
        foreach (var m in new[] { 50, 0 })
        {
            SetMotion(m);
            Place(Feet(face, 1.5f, 0f), face.position);
            yield return 4;
            yield return OpenOn(placard, "motion " + m, false);
            var arrive = -1f;
            for (var i = 0; i < 80; i++) { if (FrontRoomsInspect.Shot != null && FrontRoomsInspect.Shot.Weight >= 1f) { arrive = FrontRoomsInspect.Age; break; } yield return 1; }
            var travel = Vector3.Distance(rig.BaseEye.position, FrontRoomsInspect.LastSolve.pose.position);
            var expect = m == 0 ? 0f : Mathf.Clamp(I.TravelBase + I.TravelPerMetre * travel, I.TravelMin, I.TravelMax) * I.HalfMotionTimeScale;
            var poseErr = (cam.transform.position - FrontRoomsInspect.LastSolve.pose.position).magnitude;
            Check(poseErr < .001f && Mathf.Abs(arrive - expect) <= 1.5f * Step, "Camera motion " + m + " %: full pose (off by " + F(poseErr * 1000f, "0.0") + " mm), arrives at " + F(arrive) + " s (expect " + F(expect) + ")");
            yield return 15;
            yield return ExitWith("E", "motion " + m);
            Idle();
            var home = -1f;
            for (var i = 1; i <= 60; i++) { yield return 1; if ((cam.transform.position - rig.BaseEye.position).magnitude < 1e-3f) { home = i * Step; break; } }
            var expectOut = m == 0 ? Step : I.BlendOut * I.HalfMotionTimeScale;
            Check(Mathf.Abs(home - expectOut) <= 2f * Step, "Camera motion " + m + " %: home at " + F(home) + " s (expect " + F(expectOut) + ")");
            yield return WaitShotEnd();
        }
        SetMotion(100);
    }

    // ---------------------------------------------------------------- a second flat target (the door sign, R5-B test bed)
    static IEnumerator FlatScenario(FrontRoomsInspectable t, string label, float stand)
    {
        var face = t.Face;
        Place(Feet(face, stand, 0f), face.position);
        yield return 4;
        Capture(label + "_prompt", label + ": " + F(stand, "0.0") + " m square-on, prompt \"" + ShownPrompt + "\"", true);
        Check(ShownPrompt == t.Prompt, label + ": prompt at " + F(stand, "0.0") + " m (\"" + ShownPrompt + "\")");
        yield return OpenOn(t, label, true);
        var solved = FrontRoomsInspect.LastSolve;
        var px = PxPerMm(solved.distance);
        Note(label + ": d " + F(solved.distance) + " m, composition " + (solved.compositionB ? "B" : "A") + ", " + F(px, "0.00") + " px/mm → 17 mm cap = " + F(17f * px, "0.0") + " px (1080p), " + F(34f * px, "0.0") + " px (4K); zoom offered " + FrontRoomsInspect.ZoomStep.offered);
        Capture(label + "_closeup", label + " close-up (d " + F(solved.distance) + ")", true);
        Capture(label + "_closeup", label + " close-up, 4K", false, true);
        Check(AllOnScreen(Corners(t)), label + ": all 4 corners on screen");
        yield return 20;
        yield return ExitWith("E", label);
        Idle();
        yield return WaitShotEnd();
    }

    // ---------------------------------------------------------------- the object kind (key + ring + tag)
    static IEnumerator KeyScenario(FrontRoomsInspectable key, Transform keyRoot)
    {
        var face = key.Face;
        var lp = keyRoot.localPosition; var lr = keyRoot.localRotation;
        Place(Feet(face, .9f, 0f), key.BoxCentreWorld);
        yield return 4;
        Capture("key_prompt", "key + tag on the wall at 0.9 m: prompt \"" + ShownPrompt + "\"", true);
        Check(ShownPrompt == key.Prompt && key.Prompt == FrontRoomsInspectCatalog.PromptExamineKey, "key: prompt \"" + ShownPrompt + "\" (verb + object)");
        yield return 20;
        Check(DrawnPrompt == key.Prompt, "key: prompt as drawn \"" + DrawnPrompt + "\" (" + PromptOwner + ")");
        yield return OpenOn(key, "key", true);
        var camFwd = cam.transform.forward;
        var centre = BoundsCentre(keyRoot);
        var along = Vector3.Dot(centre - cam.transform.position, camFwd);
        var lateral = (centre - cam.transform.position - camFwd * along).magnitude;
        var fill = ScreenFill(keyRoot);
        Check(Mathf.Abs(along - .20f) <= .01f && lateral < .01f && fill >= .44f && fill <= .56f,
            "key: centred at " + F(along) + " m (0.20 ± 0.01, off-axis " + F(lateral * 1000f, "0.0") + " mm), FOV " + F(cam.fieldOfView, "0.0") + ", fills " + F(fill * 100f, "0.0") + " % of the frame (45-55; v1 29 %)");
        var tagPx = FrontRoomsInspectFraming.PxPerMm(along, cam.fieldOfView, 1080f) * 15f;
        Note("key: tag digits 15 mm = " + F(tagPx, "0.0") + " px at 1080p, " + F(tagPx * 2f, "0.0") + " px at 4K; tag texture 3300 px/m → magnification " + F(tagPx / 15f * 1000f / 3300f, "0.00") + "× (1080p)");
        Capture("key_closeup", "key in front of the camera (object kind), DOF " + F(FrontRoomsInspectLook.DofWeight, "0.0"), true);
        Capture("key_closeup", "key close-up, 4K", false, true);
        for (var i = 0; i < 15; i++) { Inject(look: new Vector2(6f, 0f)); yield return 1; }
        Idle();
        yield return 15;
        Capture("key_turned_90", "the key turned 90° (mouse)", false);
        for (var i = 0; i < 15; i++) { Inject(look: new Vector2(6f, -2f)); yield return 1; }
        Idle();
        yield return 15;
        Capture("key_turned_180", "turned 180° and tipped", false);
        FrontRoomsInspect.SetZoomTarget(true);
        yield return 30;
        Capture("key_zoom", "zoom step (0.15 m)", false);
        yield return ExitWith("E", "key");
        Idle();
        var mid = false;
        for (var i = 0; i < 60 && FrontRoomsInspect.Active; i++)
        {
            yield return 1;
            if (!mid && i == 12) { Capture("key_out_20", "the key returning (0.20 s after the exit)", false); mid = true; }
        }
        yield return 2;
        Check((keyRoot.localPosition - lp).sqrMagnitude < 1e-12f && Quaternion.Angle(keyRoot.localRotation, lr) < 1e-3f, "key: back on the wall exactly (local pose restored)");
        // OpenHeld (C21 hand-off): the key comes from a held pose and returns to it; HeldReturned fires once.
        var eye = rig.BaseEye;
        var heldPose = new Pose(eye.position + eye.rotation * new Vector3(.12f, -.12f, .35f), eye.rotation * Quaternion.Euler(0f, 160f, 0f));
        var returnedBefore = heldReturned;
        var shot = FrontRoomsInspect.OpenHeld(keyRoot, "Kit_KeyTag_Rect", heldPose, rig);
        Check(shot != null, "OpenHeld begins a shot from the held pose");
        for (var i = 0; i < 40; i++) yield return 1;
        FrontRoomsInspect.Close(rig);
        for (var i = 0; i < 60 && FrontRoomsInspect.Active; i++) yield return 1;
        yield return 2;
        Check(heldReturned - returnedBefore == 1, "OpenHeld: HeldReturned fired once after the return (" + (heldReturned - returnedBefore) + ")");
        keyRoot.localPosition = lp; keyRoot.localRotation = lr;
        yield return WaitShotEnd();
    }

    /// <summary>The share of the frame the renderers under root take (the larger of height and width / aspect, screen space).</summary>
    static float ScreenFill(Transform root)
    {
        float x0 = 1, x1 = 0, y0 = 1, y1 = 0;
        foreach (var r in root.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled) continue;
            var b = r.localBounds; var t = r.transform;
            for (var i = 0; i < 8; i++)
            {
                var c = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                var v = cam.WorldToViewportPoint(t.TransformPoint(c));
                x0 = Mathf.Min(x0, v.x); x1 = Mathf.Max(x1, v.x); y0 = Mathf.Min(y0, v.y); y1 = Mathf.Max(y1, v.y);
            }
        }
        return Mathf.Max(y1 - y0, x1 - x0);
    }

    static Vector3 BoundsCentre(Transform root)
    {
        var rs = root.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return root.position;
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b.center;
    }

    // ---------------------------------------------------------------- no clipping in the real map (50 approaches per asset)
    static IEnumerator ClipScenario(FrontRoomsInspectable t, int samples)
    {
        var face = t.Face;
        var rng = new System.Random(4242 + t.Kit.Length);
        int stood = 0, solvedCount = 0, refused = 0, clipped = 0, offscreen = 0, occluded = 0, objectBlocked = 0;
        var refusedWhy = new Dictionary<string, int>();
        var body = PlayerRoot;
        var near = cam.nearClipPlane;
        var opened = 0; var maxClampMm = 0f;
        var openAt = new HashSet<int> { 3, 13, 23, 33, 43 };
        for (var i = 0; i < samples; i++)
        {
            var dist = .5f + (float)rng.NextDouble() * (t.Def.Reach - .5f);
            var ang = -60f + 120f * (float)rng.NextDouble();
            var feet = Feet(face, dist, ang);
            if (!FreeStanding(feet)) { refused++; Count(refusedWhy, "cannot stand there"); continue; }
            stood++;
            Place(feet, t.Kind == InspectKind.Object ? t.BoxCentreWorld : face.position);
            Physics.SyncTransforms();
            if (!FrontRoomsInspect.CanOpen(t, rig)) { refused++; Count(refusedWhy, FrontRoomsInspect.LastSolveFor(t).why ?? "?"); continue; }
            solvedCount++;
            var r = FrontRoomsInspect.LastSolveFor(t);
            var eye = rig.BaseEye.position;
            if (t.Kind == InspectKind.Object)
            {
                // The object travels from the wall to the eye: its sphere must stay clear of colliders.
                // (It starts touching its own wall: only the part of the path clear of the mount, beyond radius + 2 cm, is tested.)
                var radius = t.BoundingRadius + .01f;
                var end = eye + rig.BaseEye.rotation * Vector3.forward * r.distance;
                var start = t.BoxCentreWorld;
                var normal = face.rotation * Vector3.forward;
                var lift = normal * Mathf.Min(FrontRoomsInspect.ObjectLift, .5f * Mathf.Max(0f, Vector3.Dot(eye - start, normal)));
                for (var k = 1; k <= 20; k++)
                {
                    var p = FrontRoomsInspect.ObjectPath(start, lift, end, FrontRoomsCameraRig.SineIn(k / 20f));
                    if (Vector3.Distance(p, start) < radius + .02f) continue;
                    if (Overlaps(p, radius, body))
                    {
                        objectBlocked++;
                        Log("object path sample " + i + " (" + F(dist, "0.00") + " m at " + F(ang, "0") + "°): at " + F(k / 20f, "0.00") + " of the path the key's sphere touches " + (lastBlocker != null ? lastBlocker.name + " (" + lastBlocker.GetType().Name + ", parent " + (lastBlocker.transform.parent != null ? lastBlocker.transform.parent.name : "-") + ")" : "?"));
                        break;
                    }
                }
            }
            else
            {
                var rc = FrontRoomsInspectFraming.NearCornerRadius(near, rig.BaseFov, cam.aspect);
                var bad = Overlaps(r.pose.position, rc, body);
                for (var k = 1; k <= 20 && !bad; k++)
                    if (Overlaps(Vector3.Lerp(eye, r.pose.position, FrontRoomsCameraRig.SineIn(k / 20f)), rc, body)) bad = true;
                if (bad) clipped++;
                var fov = rig.BaseFov + t.Def.FovDelta;
                var cs = Corners(t);
                var off = false; var occ = false;
                foreach (var c in cs)
                {
                    var v = FrontRoomsInspectFraming.Project(r.pose, fov, cam.aspect, c);
                    if (float.IsNaN(v.x) || v.x < -1e-4f || v.x > 1.0001f || v.y < -1e-4f || v.y > 1.0001f) off = true;
                    var toward = c + face.rotation * Vector3.forward * .003f;
                    foreach (var h in Physics.RaycastAll(r.pose.position, toward - r.pose.position, (toward - r.pose.position).magnitude, ~0, QueryTriggerInteraction.Ignore))
                        if (!h.collider.transform.IsChildOf(body)) { occ = true; break; }
                }
                if (off) offscreen++;
                if (occ) occluded++;
            }
            // A few real openings: the rendered camera must sit on the solved pose (the rig's clamp never fires).
            if (openAt.Contains(i))
            {
                yield return 2;
                if (FrontRoomsInspect.CanOpen(t, rig))
                {
                    var solvedNow = FrontRoomsInspect.LastSolveFor(t);
                    Inject(useDown: true, useHeld: true); yield return 1; Idle();
                    for (var k = 0; k < 60 && FrontRoomsInspect.Shot != null && FrontRoomsInspect.Shot.Weight < 1f; k++) yield return 1;
                    yield return 1;
                    if (Phase == "Inspect")
                    {
                        opened++;
                        var target = t.Kind == InspectKind.Object ? rig.BaseEye.position : solvedNow.pose.position;
                        maxClampMm = Mathf.Max(maxClampMm, (cam.transform.position - target).magnitude * 1000f);
                        if (opened == 1) Capture("clip_" + t.Kit + "_sample" + i, "clip sample " + i + ": " + F(dist, "0.00") + " m at " + F(ang, "0") + "°", false);
                        yield return 14;
                        Inject(useDown: true, useHeld: true); yield return 1; Idle();
                        yield return WaitShotEnd();
                    }
                    else Log("clip sample " + i + ": E did not open (phase " + Phase + ")");
                }
            }
        }
        var why = new StringBuilder();
        foreach (var kv in refusedWhy) why.Append(kv.Key + " " + kv.Value + "; ");
        Check(clipped == 0 && offscreen == 0 && occluded == 0 && objectBlocked == 0 && solvedCount > 0 && maxClampMm < 1f,
            "no clipping, " + t.Kit + " in the map: " + samples + " approaches → " + stood + " stood, " + solvedCount + " poses, " + refused + " refused (" + why + "), "
            + clipped + " clipped, " + offscreen + " off screen, " + occluded + " occluded, " + objectBlocked + " object paths blocked; " + opened + " opened, camera ≤ " + F(maxClampMm, "0.00") + " mm off the solved pose");
        yield return 2;
    }

    static void Count(Dictionary<string, int> d, string k) { d.TryGetValue(k, out var n); d[k] = n + 1; }

    static readonly Collider[] overlap = new Collider[32];
    static Collider lastBlocker;
    static bool Overlaps(Vector3 p, float r, Transform body)
    {
        var n = Physics.OverlapSphereNonAlloc(p, r, overlap, ~0, QueryTriggerInteraction.Ignore);
        for (var i = 0; i < n; i++) if (!overlap[i].transform.IsChildOf(body)) { lastBlocker = overlap[i]; return true; }
        return false;
    }

    // ---------------------------------------------------------------- the Relay does not advance (AT-01f with a live Relay)
    static IEnumerator RelayFreezeScenario(FrontRoomsInspectable placard)
    {
        var face = placard.Face;
        Place(Feet(face, 1.5f, 0f), face.position);
        yield return 3;
        // A built cell 12-25 m away, out of the player's sight.
        var here = map.CellOf(PlayerRoot.position);
        GridCoord? spot = null;
        var eye = rig.BaseEye.position;
        for (var r = 4; r <= 8 && !spot.HasValue; r++)
            for (var dx = -r; dx <= r && !spot.HasValue; dx++)
                for (var dy = -r; dy <= r && !spot.HasValue; dy++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                    var c = new GridCoord(here.x + dx, here.y + dy);
                    if (!map.IsBuilt(c) || map.InStartArea(c)) continue;
                    var p = map.CellCenter(c);
                    if (!FreeStanding(p)) continue;
                    if (!Physics.Linecast(eye, p + Vector3.up * 1.5f, ~0, QueryTriggerInteraction.Ignore)) continue;   // visible: skip
                    spot = c;
                }
        if (!spot.HasValue) { Check(false, "Relay freeze: a hidden built cell 12-25 m away"); yield break; }
        var relay = Relay;
        relay.DebugPlace(map.CellCenter(spot.Value));
        Log("Relay placed at " + spot.Value + " " + V(relay.Position) + " state " + relay.State);
        var away = relay.Position - PlayerRoot.position; away.y = 0f;
        relay.Noise(relay.Position + away.normalized * 6f, 30f);   // somewhere to walk, away from the player
        var p0 = relay.Position;
        yield return 60;
        var moved = (relay.Position - p0).magnitude;
        Note("Relay in play for 1 s: moved " + F(moved) + " m, state " + relay.State + ", sees the player " + relay.SeesPlayer + ", player sees it " + relay.PlayerSeesRelay);
        Place(Feet(face, 1.5f, 0f), face.position);
        yield return 2;
        if (relay.State == HunterState.Chase || relay.PlayerSeesRelay) { Check(false, "Relay freeze: the Relay stayed out of a chase (" + relay.State + ")"); yield break; }
        doorMoved = fixtureChanged = lampDipped = 0;
        yield return OpenOn(placard, "relay-live", false);
        if (Phase != "Inspect") { Check(false, "Relay freeze: the close-up opened with the Relay searching"); yield break; }
        var a = Snapshot();
        for (var i = 0; i < 180; i++) yield return 1;
        var b = Snapshot();
        var d = Diff(a, b, true);
        Check(d.Length == 0 && doorMoved == 0 && fixtureChanged == 0 && lampDipped == 0,
            "freeze with a live Relay (" + a.relayState + ", StateTime " + F(a.relayStateTime, "0.00") + " s): 3 s close-up, Relay position, state and StateTime, lamps, doors unchanged" + (d.Length > 0 ? " — " + d : ""));
        yield return ExitWith("E", "relay-live");
        Idle();
        var p1 = relay.Position;
        yield return 60;
        Note("after the exit the Relay moved " + F((relay.Position - p1).magnitude) + " m in 1 s (" + relay.State + ")");
        Check(relay.State != HunterState.Chase || (relay.Position - p1).magnitude > 0f, "the Relay runs again after the exit");
        yield return WaitShotEnd();
    }

    // ---------------------------------------------------------------- A43 (2026-10-30): reading pauses, looking runs live
    // research/inspect/NOTE_1030_a43_pause_sort.md. Each test-bed target with the Relay walking out of sight: a reading close-up
    // (the plan) pauses the world; a looking one (the door sign, the key) runs it once the map's A43 contract is in the game
    // (the Relay ticks, game time and Time.time advance, the doors are not paused) and pauses it as today without the contract.
    // Then, with the contract: a perceivable threat ends a live close-up at once; without the threat gate (C36, the door peek)
    // it stays. The first key (OpenHeld, not wired by the map) reports itself as looking.

    /// <summary>The map's A43 contract is compiled into the game (its field; W/apply/inspect_a43_contract).</summary>
    static bool ContractLive => typeof(FrontRooms3DGame).GetField("inspectLive", BF) != null;

    /// <summary>The Relay released and searching on a built cell 12-40 m away out of the player's sight, walking further away.</summary>
    static bool PlaceRelayHidden(out string where)
    {
        where = "";
        var relay = Relay;
        if (relay == null) return false;
        var here = map.CellOf(PlayerRoot.position);
        var eye = rig.BaseEye.position;
        for (var r = 4; r <= 10; r++)
            for (var dx = -r; dx <= r; dx++)
                for (var dy = -r; dy <= r; dy++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                    var c = new GridCoord(here.x + dx, here.y + dy);
                    if (!map.IsBuilt(c) || map.InStartArea(c)) continue;
                    var p = map.CellCenter(c);
                    if (!FreeStanding(p)) continue;
                    if (!Physics.Linecast(eye, p + Vector3.up * 1.5f, ~0, QueryTriggerInteraction.Ignore)) continue;
                    relay.DebugPlace(p);
                    var away = relay.Position - PlayerRoot.position;
                    away.y = 0f;
                    relay.Noise(relay.Position + away.normalized * 6f, 30f);
                    where = c + " " + V(relay.Position);
                    return true;
                }
        return false;
    }

    static void SetRelayState(HunterState state) =>
        Relay.GetType().GetMethod("SetState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Relay, new object[] { state });

    static IEnumerator StandAt(FrontRoomsInspectable t, FrontRoomsInspectable placard)
    {
        var face = t.Face;
        var obj = t.Kind == InspectKind.Object;
        Place(Feet(face, obj ? .9f : t == placard ? 1.5f : 1.0f, 0f), obj ? t.BoxCentreWorld : face.position);
        yield return 3;
    }

    static IEnumerator A43Scenario(FrontRoomsInspectable placard, FrontRoomsInspectable sign, FrontRoomsInspectable key, Transform keyRoot)
    {
        var live = ContractLive;
        Note("A43: the map's A43 contract is " + (live ? "APPLIED: looking close-ups run live" : "absent: every close-up pauses, as today"));
        var targets = new[] { placard, sign, key };
        var labels = new[] { "plan (placard)", "door sign", "key" };
        for (var n = 0; n < targets.Length; n++)
        {
            var t = targets[n];
            var label = "A43 " + labels[n];
            if (t == null) { Check(false, label + ": the target is placed"); continue; }
            yield return StandAt(t, placard);
            if (!PlaceRelayHidden(out var where)) { Check(false, label + ": a hidden built cell for the Relay"); continue; }
            yield return 30;   // it walks off
            yield return StandAt(t, placard);
            var relay = Relay;
            if (relay.ThreatPerceivable(cam)) { Check(false, label + ": the Relay stayed out of the player's perception (" + relay.State + ")"); relay.Park(); continue; }
            var pauses = FrontRoomsInspect.PausesWorld(t);
            var wantLive = live && !pauses;
            yield return OpenOn(t, label, false);
            if (Phase != "Inspect") { Check(false, label + ": the close-up opened (phase " + Phase + ")"); relay.Park(); continue; }
            var a = Snapshot();
            for (var i = 0; i < 60; i++) yield return 1;
            var b = Snapshot();
            var moved = (b.relayPos - a.relayPos).magnitude;
            var relayTicked = moved > 1e-4f || Math.Abs(b.relayStateTime - a.relayStateTime) > 1e-4f || a.relayState != b.relayState;
            if (wantLive)
            {
                var onPose = t.Kind == InspectKind.Object || (cam.transform.position - FrontRoomsInspect.LastSolve.pose.position).magnitude < 1e-3f;
                Check(Phase == "Inspect" && FrontRoomsInspect.Active && !FrontRoomsInspect.Closing && Time.timeScale == 1f && !map.DoorsPaused
                      && b.time - a.time > .9f && b.elapsed - a.elapsed > .9f && relayTicked && onPose,
                    label + " (LOOKING): the world runs under the close-up for 1 s: Relay " + a.relayState + "->" + b.relayState + ", moved " + F(moved, "0.00") + " m, StateTime "
                    + F(a.relayStateTime, "0.00") + "->" + F(b.relayStateTime, "0.00") + "; game time +" + F(b.elapsed - a.elapsed, "0.00") + " s, Time.time +" + F(b.time - a.time, "0.00")
                    + " s; doors paused " + map.DoorsPaused + "; the camera on its solved pose " + onPose + " (Relay at " + where + ")");
            }
            else
            {
                var d = Diff(a, b, true);
                Check(Phase == "Inspect" && Time.timeScale == 0f && map.DoorsPaused && d.Length == 0,
                    label + (pauses ? " (READING)" : " (LOOKING, no contract: pauses as today)") + ": the world pauses for 1 s (Relay " + a.relayState + ", StateTime " + F(a.relayStateTime, "0.00")
                    + ", doors paused " + map.DoorsPaused + ")" + (d.Length > 0 ? " - " + d : ""));
            }
            Check(pauses == (n == 0), label + ": sorted " + (pauses ? "READING (pauses)" : "LOOKING (live)") + " as the NOTE says");
            yield return ExitWith("E", label);
            Idle();
            yield return WaitShotEnd();
            relay.Park();
            yield return 2;
        }
        // The first key (OpenHeld, C21): not wired by the map yet; FrontRoomsInspect reports it as a looking close-up.
        if (keyRoot != null)
        {
            var lp = keyRoot.localPosition; var lr = keyRoot.localRotation;
            var eye = rig.BaseEye;
            var heldPose = new Pose(eye.position + eye.rotation * new Vector3(.12f, -.12f, .35f), eye.rotation * Quaternion.Euler(0f, 160f, 0f));
            var shot = FrontRoomsInspect.OpenHeld(keyRoot, "Kit_KeyTag_Rect", heldPose, rig);
            Check(shot != null && FrontRoomsInspect.HeldOpen && !FrontRoomsInspect.CurrentPauses && FrontRoomsInspect.CurrentThreatGated,
                "A43 first key (OpenHeld): a looking close-up with the threat gate (CurrentPauses " + FrontRoomsInspect.CurrentPauses + ")");
            for (var i = 0; i < 20; i++) yield return 1;
            FrontRoomsInspect.Close(rig);
            for (var i = 0; i < 60 && FrontRoomsInspect.Active; i++) yield return 1;
            keyRoot.localPosition = lp; keyRoot.localRotation = lr;
            yield return WaitShotEnd();
        }
        if (sign == null) yield break;
        // The threat gate on a live close-up (contract only; without it the close-up pauses, so nothing can change under it).
        yield return StandAt(sign, placard);
        if (!PlaceRelayHidden(out var w2)) { Check(false, "A43 threat: a hidden built cell for the Relay"); yield break; }
        yield return 10;
        yield return StandAt(sign, placard);
        yield return OpenOn(sign, "A43 threat", false);
        if (Phase != "Inspect") { Check(false, "A43 threat: the sign close-up opened"); Relay.Park(); yield break; }
        var closedBefore = closedEvents;
        SetRelayState(HunterState.Chase);
        var threat = Relay.ThreatPerceivable(cam);
        var frames = 0;
        while (frames < 6 && Phase == "Inspect") { yield return 1; frames++; }
        if (live)
            Check(threat && Phase == "Playing" && (!FrontRoomsInspect.Active || FrontRoomsInspect.Closing) && closedEvents == closedBefore + 1 && Time.timeScale == 1f,
                "A43 threat gate: a chase (ThreatPerceivable " + threat + ") ends the live sign close-up in " + frames + " frame(s): phase " + Phase + ", Closed heard " + (closedEvents - closedBefore) + "x, timeScale " + F(Time.timeScale, "0.##"));
        else
            Check(Phase == "Inspect" && Time.timeScale == 0f && !FrontRoomsInspect.WantsEnd,
                "A43 threat, no contract: the paused close-up stays (nothing ticks under it; WantsEnd false)");
        Relay.Park();
        if (Phase == "Inspect") yield return ExitWith("E", "A43 threat");
        Idle();
        yield return WaitShotEnd();
        if (!live) { Note("C36 (no threat gate): needs the live world (the contract run)"); yield break; }
        // C36: without the threat gate (the door peek's flag) the same chase never ends it.
        var def = sign.Def;
        def.noThreatGate = true;
        try
        {
            yield return StandAt(sign, placard);
            if (!PlaceRelayHidden(out var w3)) { Check(false, "C36: a hidden built cell for the Relay"); yield break; }
            yield return 10;
            yield return StandAt(sign, placard);
            yield return OpenOn(sign, "C36 ungated", false);
            if (Phase != "Inspect") { Check(false, "C36: the ungated sign close-up opened"); Relay.Park(); yield break; }
            SetRelayState(HunterState.Chase);
            var threat2 = Relay.ThreatPerceivable(cam);
            for (var i = 0; i < 10; i++) yield return 1;
            var sh = FrontRoomsInspect.Shot;
            Check(threat2 && Phase == "Inspect" && FrontRoomsInspect.Active && !FrontRoomsInspect.Closing && sh != null && !sh.Cancelled && !FrontRoomsInspect.WantsEnd,
                "C36: with noThreatGate the chase (ThreatPerceivable " + threat2 + ") leaves the live close-up open for 10 frames (shot not cancelled, no end asked)");
            Relay.Park();
            yield return ExitWith("E", "C36 ungated");
            Idle();
            yield return WaitShotEnd();
        }
        finally { def.noThreatGate = false; }
    }

    // ---------------------------------------------------------------- transcript reframe (10-24): SetTranscript mid close-up
    static Rect ViewportBounds(Vector3[] world)
    {
        float x0 = 9f, x1 = -9f, y0 = 9f, y1 = -9f;
        foreach (var w in world)
        {
            var v = cam.WorldToViewportPoint(w);
            x0 = Mathf.Min(x0, v.x); x1 = Mathf.Max(x1, v.x); y0 = Mathf.Min(y0, v.y); y1 = Mathf.Max(y1, v.y);
        }
        return Rect.MinMaxRect(x0, y0, x1, y1);
    }

    static string Vp(Rect r) => "x " + F(r.xMin) + "–" + F(r.xMax) + ", y " + F(r.yMin) + "–" + F(r.yMax);

    static float ReframeWant(float travel) => Mathf.Clamp(FrontRoomsShotTimings.Inspect.TravelBase + FrontRoomsShotTimings.Inspect.TravelPerMetre * travel,
        FrontRoomsShotTimings.Inspect.TravelMin, FrontRoomsShotTimings.Inspect.TravelMax);

    static readonly List<Vector3> reframeSteps = new List<Vector3>();

    /// <summary>Game frames while the reframe eases: seconds on the inspect clock; each frame's camera move goes to reframeSteps.</summary>
    static IEnumerator RunReframe(FrontRoomsInspectable t, Rect free, float[] seconds, float[] inside)
    {
        var last = cam.transform.position;
        seconds[0] = 0f;
        inside[0] = -1f;
        for (var i = 0; i < 90 && FrontRoomsInspect.Reframing; i++)
        {
            yield return 1;
            seconds[0] += Step;
            reframeSteps.Add(cam.transform.position - last);
            last = cam.transform.position;
            var b = ViewportBounds(Corners(t));
            if (inside[0] < 0f && b.xMin >= free.xMin - 1e-3f && b.xMax <= free.xMax + 1e-3f && b.yMin >= free.yMin - 1e-3f && b.yMax <= free.yMax + 1e-3f) inside[0] = seconds[0];
        }
        yield return 1;
    }

    /// <summary>
    /// The TEXT toggle during a close-up, in the game (FrontRooms3DGame ticks FrontRoomsInspect then the rig): the test bed's door
    /// sign has no screen rect (composition B, centred), so with the panel's 36 % reserved it must ease into the left 64 % and
    /// back, and a second toggle mid-ease must not jump. The placard's composition A already keeps the left 64 %: no move.
    /// </summary>
    static IEnumerator ReframeScenario(FrontRoomsInspectable sign, FrontRoomsInspectable placard)
    {
        var face = sign.Face;
        Place(Feet(face, 1.0f, 0f), face.position);
        yield return 4;
        yield return OpenOn(sign, "reframe sign", false);
        if (Phase != "Inspect") { Check(false, "reframe: the sign close-up opened"); yield break; }
        yield return 10;
        var open = FrontRoomsInspect.LastSolve;
        var p0 = cam.transform.position;
        var r0 = cam.transform.rotation;
        var b0 = ViewportBounds(Corners(sign));
        Check(!FrontRoomsInspect.ShowTranscript && !FrontRoomsInspect.Reframing && (p0 - open.pose.position).magnitude < 1e-4f,
            "reframe sign: before TEXT the camera sits on the open's solved pose (composition " + (open.compositionB ? "B" : "A") + ", " + Vp(b0) + ")");
        Capture("reframe_sign_text_off", "sign close-up, transcript off: composition B, centred", false);
        var secs = new float[1];
        var inside = new float[1];
        // TEXT on.
        reframeSteps.Clear();
        FrontRoomsInspect.SetTranscript(true);
        var free = FrontRoomsInspect.FreeScreenRect;
        var to = FrontRoomsInspect.LastSolve;
        var travel = (to.pose.position - p0).magnitude;
        var want = ReframeWant(travel);
        yield return RunReframe(sign, free, secs, inside);
        var b1 = ViewportBounds(Corners(sign));
        var peak = 0f;
        foreach (var st in reframeSteps) peak = Mathf.Max(peak, st.magnitude);
        Check(FrontRoomsInspect.Reframing == false && Mathf.Abs(secs[0] - want) <= Step + 1e-4f && inside[0] > 0f && inside[0] <= want + Step + 1e-4f
              && b1.xMin >= free.xMin - 1e-3f && b1.xMax <= free.xMax + 1e-3f && (cam.transform.position - to.pose.position).magnitude < 1e-4f,
            "reframe sign, TEXT on: into the free rect (x " + F(free.xMin) + "–" + F(free.xMax) + ") at " + F(inside[0]) + " s, settled at " + F(secs[0]) + " s (want " + F(want) + " for " + F(travel * 1000f, "0") + " mm): " + Vp(b0) + " → " + Vp(b1));
        Capture("reframe_sign_text_on", "sign close-up, transcript on: eased into the left 64 % (the panel's 36 % kept clear)", false);
        // TEXT off: back to the open's pose.
        reframeSteps.Clear();
        FrontRoomsInspect.SetTranscript(false);
        yield return RunReframe(sign, FrontRoomsInspect.FreeScreenRect, secs, inside);
        var b2 = ViewportBounds(Corners(sign));
        Check((cam.transform.position - p0).magnitude < 1e-5f && Quaternion.Angle(cam.transform.rotation, r0) < 1e-3f && Mathf.Abs(secs[0] - want) <= Step + 1e-4f,
            "reframe sign, TEXT off: back on the open's pose in " + F(secs[0]) + " s (" + Vp(b2) + ")");
        // A second toggle mid-ease (0.20 s in): no jump, no speed step.
        reframeSteps.Clear();
        FrontRoomsInspect.SetTranscript(true);
        var last = cam.transform.position;
        for (var i = 0; i < 12; i++) { yield return 1; reframeSteps.Add(cam.transform.position - last); last = cam.transform.position; }
        FrontRoomsInspect.SetTranscript(false);
        yield return RunReframe(sign, FrontRoomsInspect.FreeScreenRect, secs, inside);
        float stepMax = 0f, jerk = 0f, jerkSingle = 0f;
        for (var i = 0; i < reframeSteps.Count; i++)
        {
            stepMax = Mathf.Max(stepMax, reframeSteps[i].magnitude);
            if (i > 0) jerk = Mathf.Max(jerk, (reframeSteps[i] - reframeSteps[i - 1]).magnitude);
        }
        // The single ease's own largest speed step (sine in-out at 60 fps): D × (π² / 2) × (dt / T)².
        jerkSingle = travel * Mathf.PI * Mathf.PI * .5f * (Step / want) * (Step / want);
        Check(stepMax <= peak * 1.02f && jerk <= 2.5f * jerkSingle && (cam.transform.position - p0).magnitude < 1e-5f,
            "reframe sign, double toggle at 0.20 s: largest frame step " + F(stepMax * 1000f, "0.00") + " mm (one toggle " + F(peak * 1000f, "0.00") + "), largest speed step " + F(jerk * 1000f, "0.000") + " mm/frame (one ease " + F(jerkSingle * 1000f, "0.000") + "); back on the open's pose");
        Note("reframe sign: d " + F(open.distance) + " → " + F(to.distance) + " m, travel " + F(travel * 1000f, "0") + " mm, ease " + F(want) + " s (the open's rule), peak " + F(peak / Step, "0.00") + " m/s");
        yield return ExitWith("E", "reframe sign");
        Idle();
        yield return WaitShotEnd();
        // The placard: composition A already keeps the left 64 %.
        var pf = placard.Face;
        Place(Feet(pf, 1.5f, 0f), pf.position);
        yield return 6;
        yield return OpenOn(placard, "reframe placard", false);
        if (Phase != "Inspect") { Check(false, "reframe: the placard close-up opened"); yield break; }
        yield return 10;
        var pp = cam.transform.position;
        var pb = ViewportBounds(Corners8(placard));
        FrontRoomsInspect.SetTranscript(true);
        yield return RunReframe(placard, FrontRoomsInspect.FreeScreenRect, secs, inside);
        var moved = (cam.transform.position - pp).magnitude;
        var pb1 = ViewportBounds(Corners8(placard));
        Check(!FrontRoomsInspect.LastSolve.compositionB && moved < 1e-4f && pb1.xMax <= .64f + 1e-3f,
            "reframe placard, TEXT on: composition A already in the left 64 % (" + Vp(pb) + "); the camera moves " + F(moved * 1000f, "0.000") + " mm");
        FrontRoomsInspect.SetTranscript(false);
        yield return RunReframe(placard, FrontRoomsInspect.FreeScreenRect, secs, inside);
        yield return ExitWith("E", "reframe placard");
        Idle();
        yield return WaitShotEnd();
        Check(!FrontRoomsInspect.ShowTranscript, "reframe: the transcript is off again after the scenario");
    }

    // ---------------------------------------------------------------- pause neutrality (AT-01g): three replays of one seed
    // Control A (0.2 s close-up), control B (0.2 s), test (10 s). Same seed, same frame-indexed input, captureDeltaTime 1/60.
    // The timeline runs on PLAYING frames (game time), so a long close-up only adds paused frames. Hash at 40 s of game time.
    static readonly List<string> neutral = new List<string>();
    static readonly List<WorldState> finals = new List<WorldState>();

    static IEnumerator Neutrality()
    {
        // The short hold is 0.25 s: at exactly 0.20 s the summed inspect clock can sit a hair under CancelAfter.
        foreach (var hold in new[] { .25f, .25f, 10f })
        {
            yield return NeutralityRun(hold);
            if (finals.Count < 3)
            {
                Time.captureDeltaTime = 0f;
                FrontRoomsInput.ClearInjectedSnapshot();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                game = null;
                yield return 5;
            }
        }
        if (finals.Count == 3)
        {
            var control = Diff(finals[0], finals[1], false);
            var test = Diff(finals[0], finals[2], false);
            Note("control A vs control B (both 0.25 s): " + (control.Length == 0 ? "identical" : control));
            Note("control A (0.25 s) vs test (10 s): " + (test.Length == 0 ? "identical" : test));
            Check(control.Length > 0 || test.Length == 0, "pause neutrality: a 10 s and a 0.25 s close-up give the same gameplay state at 40 s of game time" + (control.Length > 0 ? " — INCONCLUSIVE: the two identical controls already differ, so the sim is not deterministic across replays" : ""));
            foreach (var line in neutral) Note(line);
        }
    }

    static IEnumerator NeutralityRun(float hold)
    {
        Time.captureDeltaTime = Step;
        while ((game = UnityEngine.Object.FindFirstObjectByType<FrontRooms3DGame>()) == null) yield return 1;
        cam = Get<Camera>(game, "cam");
        EnsureRTs();
        cam.targetTexture = rt1080;
        for (var i = 0; i < 120; i++) yield return 1;   // 2.0 s of title at 1/60
        UnityEngine.Random.InitState(4242);
        Call(game, "RequestTitleStart");
        if (!Get<bool>(game, "mapPlay")) { Check(false, "neutrality: run start"); yield break; }
        map = Get<FrontRoomsMapWorld>(game, "map");
        rig = Get<FrontRoomsCameraRig>(game, "rig");
        Get<FrontRoomsHunterTuning>(game, "hunterTuning").releaseDelaySeconds = 1e6f;
        Idle();
        FrontRoomsInspectable placard = null;
        var playFrame = 0; var guard = 0;
        var opened = false; var closed = false; var holdFrames = 0; var openFrame = -1;
        var settledFor = 0;
        // Fixed, frame-indexed script on PLAYING frames: stand at the placard at 10.0 s, Relay placed at 11.0 s,
        // E at 12.0 s, hold, E; then walk back 2 s, turn 90° in 0.5 s, walk on 2 s, stand. Hash at 40.0 s.
        while (playFrame < 2400 && guard < 2400 + 700 + 300)
        {
            guard++;
            if (Phase == "Caught") { neutral.Add("hold " + F(hold, "0.0") + " s: CAUGHT at play frame " + playFrame); break; }
            if (Phase == "Playing" && Time.deltaTime > 0f) playFrame++;
            if (placard == null) placard = FindKit("Kit_EvacPlacard");
            settledFor = string.IsNullOrEmpty(map.WorkInFrame(Time.frameCount - 1)) ? settledFor + 1 : 0;
            if (playFrame == 600 && placard != null && Phase == "Playing")
            {
                var face = placard.Face;
                Place(Feet(face, 1.5f, 0f), face.position);
            }
            if (playFrame == 660 && Phase == "Playing" && Relay != null && !Relay.Released)
                Relay.DebugPlace(map.CellCenter(map.CellOf(PlayerRoot.position) + new GridCoord(6, 10)));
            if (playFrame == 720 && !opened && Phase == "Playing")
            {
                Inject(useDown: true, useHeld: true);
                opened = true;
                openFrame = playFrame;
                neutral.Add("hold " + F(hold, "0.0") + " s: E at play frame " + playFrame + " (streaming idle for " + settledFor + " frames)");
                yield return 1;
                Idle();
                continue;
            }
            if (opened && !closed && Phase == "Inspect")
            {
                holdFrames++;
                if (holdFrames >= Mathf.RoundToInt(hold / Step))
                {
                    // E until the close-up lets go (one press normally; the loop only guards the harness).
                    for (var k = 0; k < 10 && Phase == "Inspect"; k++)
                    {
                        Inject(useDown: true, useHeld: true);
                        yield return 1;
                        Idle();
                        yield return 1;
                    }
                    closed = true;
                    neutral.Add("hold " + F(hold, "0.00") + " s: exit after " + holdFrames + " inspect frames, phase " + Phase);
                    continue;
                }
            }
            if (closed && Phase == "Playing")
            {
                var rel = playFrame - openFrame;
                if (rel > 10 && rel <= 130) Inject(move: new Vector2(0f, -1f));
                else if (rel > 130 && rel <= 160) Inject(look: new Vector2(3f, 0f));
                else if (rel > 160 && rel <= 280) Inject(move: new Vector2(0f, 1f));
                else Idle();
            }
            yield return 1;
        }
        Idle();
        var s = Snapshot();
        finals.Add(s);
        neutral.Add("hold " + F(hold, "0.0") + " s → at play frame " + playFrame + ": player " + V(s.playerPos) + ", Relay " + V(s.relayPos) + " " + s.relayState + " " + F(s.relayStateTime, "0.000")
            + ", lamps " + s.lamps.Count + ", doors " + s.doors.Count + ", elapsed " + F(s.elapsed) + ", inspect frames held " + holdFrames);
    }
}
