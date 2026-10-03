using System;
using System.Collections;
using System.Collections.Generic;
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

/// <summary>
/// Evidence for the glass and zone-reflection work (G1-G4, G6). Writes to Verification/glass.
///   RunReflectionTestBatch  Play mode: proves SetZoneReflection reaches URP at runtime. Two
///                           metal spheres (mirror, smoothness .6), no lights, black ambient,
///                           so every pixel is the default reflection. Cube A, a 0.5 s fade,
///                           cube B; the render-texture path against the plain asset swap; the
///                           intensity knob; ApplyAmbient; the WebGL dip mode; a mid-fade retarget.
///                           Ends with EditorApplication.Exit (run without -quit).
///   RunWindowLookdevBatch   Edit mode: the real map's window pane from the game's eye height
///                           (1.5 m straight, 50°, 60°, 0.7 m) with the old material and default
///                           sky reflection, the new Glass_Window (6 mm) with and without the zone
///                           cube, and the crack / palm hooks. Full post stack, 1920x1080, 4x MSAA.
///   RunPropLookdevBatch     Edit mode: kit props that use Prop_Glass / Prop_BottleBlue, old (URP Lit
///                           values as saved) against new (FrontRooms/Glass + Office cube).
/// </summary>
[InitializeOnLoad]
public static class FrontRoomsGlassVerification
{
    const string ActiveKey = "FrontRooms.GlassVerify.Active";
    const string DeadlineKey = "FrontRooms.GlassVerify.Deadline";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    const BindingFlags Public = BindingFlags.Instance | BindingFlags.Public;

    static IEnumerator routine;
    static readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();
    static bool done;
    static readonly StringBuilder log = new StringBuilder();

    static string OutDir
    {
        get
        {
            var d = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "glass");
            Directory.CreateDirectory(d);
            return d;
        }
    }

    static FrontRoomsGlassVerification()
    {
        if (SessionState.GetBool(ActiveKey, false)) EditorApplication.update += Tick;
    }

    // ------------------------------------------------------------------ play-mode proof

    public static void RunReflectionTestBatch()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetFloat(DeadlineKey, (float)EditorApplication.timeSinceStartup + 240f);
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }

    static void Tick()
    {
        if (done) return;
        if (EditorApplication.timeSinceStartup > SessionState.GetFloat(DeadlineKey, float.MaxValue))
        {
            Line("TIMEOUT");
            Finish(2);
            return;
        }
        if (!EditorApplication.isPlaying) return;
        if (routine == null) { routine = ReflectionTest(); stack.Push(routine); }
        try
        {
            // A tiny coroutine runner: nested IEnumerators (Frames) run to completion first.
            var top = stack.Peek();
            if (top.MoveNext())
            {
                if (top.Current is IEnumerator nested) stack.Push(nested);
            }
            else
            {
                stack.Pop();
                if (stack.Count == 0) Finish(0);
            }
        }
        catch (Exception e)
        {
            Line("EXCEPTION " + e);
            Finish(3);
        }
    }

    static void Finish(int code)
    {
        done = true;
        SessionState.SetBool(ActiveKey, false);
        EditorApplication.update -= Tick;
        File.WriteAllText(Path.Combine(OutDir, "reflection_test.txt"), log.ToString());
        Debug.Log("[GlassVerify]\n" + log);
        if (Application.isBatchMode) EditorApplication.Exit(code);
    }

    static void Line(string s) => log.AppendLine(s);

    static Camera testCam;
    static RenderTexture testRt;
    static Texture2D testRead;

    static IEnumerator Frames(int n)
    {
        var target = Time.frameCount + n;
        while (Time.frameCount < target) yield return null;
    }

    struct Shot { public string name; public Color[] pixels; public Vector3 mirror, rough; }

    static IEnumerator ReflectionTest()
    {
        Time.captureDeltaTime = 1f / 60f;
        Line("Unity " + Application.unityVersion + ", " + SystemInfo.graphicsDeviceType + ", URP " + (GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.name : "none"));
        RenderSettings.fog = false;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.black;
        testCam = new GameObject("GLASS VERIFY camera").AddComponent<Camera>();
        testCam.transform.position = Vector3.zero;
        testCam.fieldOfView = 30f;
        testCam.clearFlags = CameraClearFlags.SolidColor;
        testCam.backgroundColor = Color.black;
        testCam.nearClipPlane = .1f;
        testCam.farClipPlane = 50f;
        testCam.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        testRt = new RenderTexture(512, 256, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        testCam.targetTexture = testRt;
        testCam.enabled = false;
        testRead = new Texture2D(512, 256, TextureFormat.RGBA32, false);
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        Sphere(lit, new Vector3(-.62f, 0f, 4f), 1f);
        Sphere(lit, new Vector3(.62f, 0f, 4f), .6f);
        yield return Frames(3);

        var cubes = new Dictionary<FrontRoomsLook.ReflectionZone, Cubemap>();
        foreach (FrontRoomsLook.ReflectionZone z in Enum.GetValues(typeof(FrontRoomsLook.ReflectionZone)))
        {
            cubes[z] = FrontRoomsZoneReflection.Cube(z);
            Line("cube " + z + ": " + (cubes[z] != null ? cubes[z].name + " " + cubes[z].width + " px " + cubes[z].format + " mips " + cubes[z].mipmapCount : "MISSING"));
        }

        var s0 = Render("00_default_reflection");
        Line(State("before any call"));

        // 1. First call snaps (blend-texture path on desktop).
        FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.Level0);
        yield return Frames(2);
        var a = Render("01_A_Level0");
        Line(State("after SetZoneReflection(Level0)"));
        FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.Level0);
        Line("same zone again -> fading " + FrontRoomsZoneReflection.Fading + " (expect False)");

        // 2. A 0.5 s fade to Office.
        FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.Office, .5f);
        var t0 = Render("02_fade_t0.00");
        yield return Frames(15);
        var mid = Render("03_fade_t0.25");
        Line(State("fade at 15 frames (0.25 s)"));
        yield return Frames(25);
        var b = Render("04_B_Office");
        Line(State("fade +40 frames"));
        Line("fading after 40 frames: " + FrontRoomsZoneReflection.Fading + " (expect False)");
        Compare("A (Level0) vs default reflection", s0, a);
        Compare("fade t=0 vs A", a, t0);
        Compare("fade t=0.25 vs A", a, mid);
        Compare("fade t=0.25 vs B", b, mid);
        Compare("B (Office) vs A (Level0)", a, b);

        // 3. Same cube through the plain asset (no render texture): orientation and HDR decode check.
        FrontRoomsZoneReflection.AllowBlendTexture = false;
        FrontRoomsZoneReflection.SetImmediate(FrontRoomsLook.ReflectionZone.Office);
        yield return Frames(2);
        var bAsset = Render("05_B_Office_asset_direct");
        Line(State("Office as the plain asset"));
        Compare("render texture (FlipY false) vs plain asset", bAsset, b);
        FrontRoomsZoneReflection.AllowBlendTexture = true;
        FrontRoomsZoneReflection.FlipY = true;
        FrontRoomsZoneReflection.SetImmediate(FrontRoomsLook.ReflectionZone.Office);
        yield return Frames(2);
        var bFlip = Render("06_B_Office_rt_flipY");
        Compare("render texture (FlipY true) vs plain asset", bAsset, bFlip);
        FrontRoomsZoneReflection.FlipY = false;

        // 4. The intensity knob at runtime.
        FrontRoomsZoneReflection.SetImmediate(FrontRoomsLook.ReflectionZone.Level0);
        yield return Frames(2);
        var full = Render("07_Level0_intensity_" + RenderSettings.reflectionIntensity.ToString("0.00", CultureInfo.InvariantCulture));
        var was = RenderSettings.reflectionIntensity;
        RenderSettings.reflectionIntensity = was * .5f;
        yield return Frames(2);
        var half = Render("08_Level0_intensity_half");
        Line(State("intensity halved"));
        Line("intensity " + was.ToString("0.00", CultureInfo.InvariantCulture) + " -> " + (was * .5f).ToString("0.00", CultureInfo.InvariantCulture)
            + ": mirror mean ratio " + Ratio(half.mirror, full.mirror) + ", rough " + Ratio(half.rough, full.rough)
            + " (Unity applies the slider in gamma: expect GammaToLinear(" + (was * .5f).ToString("0.00", CultureInfo.InvariantCulture) + ")/GammaToLinear(" + was.ToString("0.00", CultureInfo.InvariantCulture) + ") = "
            + (Mathf.GammaToLinearSpace(was * .5f) / Mathf.GammaToLinearSpace(was)).ToString("0.000", CultureInfo.InvariantCulture) + ")");

        // 5. ApplyAmbient (UpdateEnvironment) keeps the zone cube and intensity.
        FrontRoomsLook.ApplyAmbient();
        RenderSettings.fog = false;
        yield return Frames(2);
        var ambient = Render("09_after_ApplyAmbient");
        Line(State("after ApplyAmbient"));
        Compare("after ApplyAmbient vs Level0 at full intensity", full, ambient);

        // 6. Dip mode (what WebGL uses).
        FrontRoomsZoneReflection.AllowBlendTexture = false;
        FrontRoomsZoneReflection.SetImmediate(FrontRoomsLook.ReflectionZone.Level0);
        FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.DeadLamp, .5f);
        yield return Frames(15);
        var dipMid = Render("10_dip_t0.25");
        Line(State("dip at 0.25 s"));
        yield return Frames(25);
        var dipEnd = Render("11_dip_end_DeadLamp");
        Line(State("dip end"));
        FrontRoomsZoneReflection.AllowBlendTexture = true;
        FrontRoomsZoneReflection.SetImmediate(FrontRoomsLook.ReflectionZone.DeadLamp);
        yield return Frames(2);
        var deadRt = Render("12_DeadLamp_rt");
        Compare("dip end vs DeadLamp via render texture", deadRt, dipEnd);
        Line("dip mid brightness (mirror) " + Fmt(dipMid.mirror) + " vs end " + Fmt(dipEnd.mirror));

        // 7. Retarget in the middle of a fade: no pop.
        FrontRoomsZoneReflection.SetImmediate(FrontRoomsLook.ReflectionZone.Level0);
        FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.Office, .5f);
        yield return Frames(12);
        var before = Render("13_retarget_before");
        FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.Tall, .5f);
        var after = Render("14_retarget_after_same_frame");
        Compare("retarget: frame before vs frame after the call", before, after);
        yield return Frames(40);
        var tall = Render("15_retarget_end_Tall");
        FrontRoomsZoneReflection.SetImmediate(FrontRoomsLook.ReflectionZone.Tall);
        yield return Frames(2);
        var tallSnap = Render("16_Tall_snap");
        Compare("retarget end vs Tall snapped", tallSnap, tall);

        // 8. Every cube once, for the record.
        foreach (FrontRoomsLook.ReflectionZone z in Enum.GetValues(typeof(FrontRoomsLook.ReflectionZone)))
        {
            FrontRoomsZoneReflection.SetImmediate(z);
            yield return Frames(2);
            var s = Render("20_cube_" + z);
            Line("cube " + z + " at intensity " + RenderSettings.reflectionIntensity.ToString("0.00", CultureInfo.InvariantCulture) + ": mirror " + Fmt(s.mirror) + ", rough " + Fmt(s.rough));
        }

        // ---- fix pass (2026-10-03) ----
        var inv = CultureInfo.InvariantCulture;
        var nominalId = Shader.PropertyToID("_FR_ZoneReflNominal");
        // 9. Zone intensities are linear: the slider gets LinearToGamma(linear), URP's decode x is the linear value.
        FrontRoomsZoneReflection.SetImmediate(FrontRoomsLook.ReflectionZone.Level0);
        yield return Frames(2);
        Line("LINEAR Level0: slider " + RenderSettings.reflectionIntensity.ToString("0.0000", inv) + " (expect LinearToGamma(" + FrontRoomsZoneReflection.LinearOf(FrontRoomsLook.ReflectionZone.Level0).ToString("0.00", inv) + ") = "
            + Mathf.LinearToGammaSpace(FrontRoomsZoneReflection.LinearOf(FrontRoomsLook.ReflectionZone.Level0)).ToString("0.0000", inv) + "), decode x " + ReflectionProbe.defaultTextureHDRDecodeValues.x.ToString("0.0000", inv)
            + ", _FR_ZoneReflNominal " + Shader.GetGlobalFloat(nominalId).ToString("0.0000", inv));
        FrontRoomsZoneReflection.SetImmediate(FrontRoomsLook.ReflectionZone.Tall);
        yield return Frames(2);
        Line("LINEAR Tall: slider " + RenderSettings.reflectionIntensity.ToString("0.0000", inv) + ", decode x " + ReflectionProbe.defaultTextureHDRDecodeValues.x.ToString("0.0000", inv) + " (expect 0.45), nominal " + Shader.GetGlobalFloat(nominalId).ToString("0.0000", inv));

        // 10. Glass dips with the world in the dip mode (the reflection floor divides by the steady nominal intensity,
        //     not the current decode). The rough sphere is swapped to FrontRooms/Glass with _ReflectionMin 1.
        var roughRenderer = GameObject.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).First(r => r.transform.position.x > 0f);
        var roughMaterial = roughRenderer.sharedMaterial;
        var glassShader = Shader.Find("FrontRooms/Glass");
        var glassTest = new Material(glassShader) { name = "verify glass" };
        glassTest.SetFloat("_ReflectionMin", 1f);
        glassTest.SetFloat("_PaneF0", .08f);
        glassTest.SetFloat("_AlphaFace", .11f);
        glassTest.SetFloat("_AlphaFresnel", .89f);
        roughRenderer.sharedMaterial = glassTest;
        FrontRoomsZoneReflection.AllowBlendTexture = false;
        FrontRoomsZoneReflection.SetImmediate(FrontRoomsLook.ReflectionZone.Level0);
        yield return Frames(2);
        var gSteady = Render("21_glass_steady_Level0");
        FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.Office, .5f);
        yield return Frames(14);
        var gDip = Render("22_glass_dip_0.23s");
        Line(State("glass dip at 0.23 s") + ", nominal " + Shader.GetGlobalFloat(nominalId).ToString("0.000", inv));
        Line("DIP mirror (world) dip/steady " + Ratio(gDip.mirror, gSteady.mirror) + " · glass sphere dip/steady " + Ratio(gDip.rough, gSteady.rough)
            + " (fix: glass follows the world; before the fix glass kept ~1.0 through the dip)");
        yield return Frames(30);
        Line("DIP end: fading " + FrontRoomsZoneReflection.Fading + ", nominal " + Shader.GetGlobalFloat(nominalId).ToString("0.000", inv));
        FrontRoomsZoneReflection.AllowBlendTexture = true;
        roughRenderer.sharedMaterial = roughMaterial;

        // 11. Fades run on unscaled time: a fade finishes with timeScale 0 (pause, slow-motion shots).
        FrontRoomsZoneReflection.SetImmediate(FrontRoomsLook.ReflectionZone.Level0);
        Time.timeScale = 0f;
        FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.Office, .5f);
        yield return Frames(45);   // captureDeltaTime 1/60: the driver steps by the capture step (0.75 s)
        Line("UNSCALED: timeScale 0, 45 frames after a 0.5 s fade: fading " + FrontRoomsZoneReflection.Fading + " (expect False), zone " + FrontRoomsZoneReflection.Zone
            + ", unscaledDeltaTime " + Time.unscaledDeltaTime.ToString("0.0000", inv));
        // The same with no capture step (as in a real paused game): unscaled wall time, 0.7 s.
        FrontRoomsZoneReflection.SetImmediate(FrontRoomsLook.ReflectionZone.Level0);
        Time.captureDeltaTime = 0f;
        FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.Office, .5f);
        var wall0 = Time.realtimeSinceStartup;
        var wallFrames = 0;
        while (Time.realtimeSinceStartup - wall0 < .7f) { wallFrames++; yield return null; }
        Line("UNSCALED (no capture step, timeScale 0): after " + (Time.realtimeSinceStartup - wall0).ToString("0.00", inv) + " s wall time (" + wallFrames + " frames): fading " + FrontRoomsZoneReflection.Fading + " (expect False)");
        Time.captureDeltaTime = 1f / 60f;
        Time.timeScale = 1f;

        // 12. Same zone, but something rewrote RenderSettings: the early return re-applies.
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.Office);
        Line("REWRITE: mode after SetZoneReflection(same zone) " + RenderSettings.defaultReflectionMode + " (expect Custom), custom " + (RenderSettings.customReflectionTexture != null ? RenderSettings.customReflectionTexture.name : "null"));

        // 13. R-restart: a Single scene load forgets the zone, so the run-start call snaps instead of fading.
        //     (Simulated: the handler is invoked directly; the empty test scene cannot be reloaded by build index.)
        var onLoaded = typeof(FrontRoomsZoneReflection).GetMethod("OnSceneLoaded", BindingFlags.NonPublic | BindingFlags.Static);
        onLoaded.Invoke(null, new object[] { UnityEngine.SceneManagement.SceneManager.GetActiveScene(), UnityEngine.SceneManagement.LoadSceneMode.Single });
        Line("RESTART: after a Single load: Active " + FrontRoomsZoneReflection.Active + " (expect False), nominal " + Shader.GetGlobalFloat(nominalId).ToString("0.000", inv));
        FrontRoomsLook.SetZoneReflection(FrontRoomsLook.ReflectionZone.Office, .5f);
        Line("RESTART: first call after the load: fading " + FrontRoomsZoneReflection.Fading + " (expect False: snapped), zone " + FrontRoomsZoneReflection.Zone);
        Line("FLIPY: SystemInfo.graphicsUVStartsAtTop " + SystemInfo.graphicsUVStartsAtTop + " -> FlipY " + FrontRoomsZoneReflection.FlipY + " (Metal measured: false)");

        // 14. Does URP apply ReflectionProbe.intensity linearly or in gamma? (G7 needs the room probes at the zone's
        //     linear intensity.) A Custom probe around both spheres with the Office cube, intensity 1 then 0.5.
        FrontRoomsZoneReflection.SetImmediate(FrontRoomsLook.ReflectionZone.Level0);
        var probeGo = new GameObject("GLASS VERIFY probe");
        probeGo.transform.position = new Vector3(0f, 0f, 4f);
        var probe = probeGo.AddComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Custom;
        probe.customBakedTexture = FrontRoomsZoneReflection.Cube(FrontRoomsLook.ReflectionZone.Office);
        probe.size = new Vector3(6f, 6f, 6f);
        probe.importance = 10;
        probe.intensity = 1f;
        yield return Frames(3);
        var p1 = Render("23_probe_Office_intensity_1");
        probe.intensity = .5f;
        yield return Frames(3);
        var p05 = Render("24_probe_Office_intensity_0.5");
        Line("PROBE intensity 1 -> 0.5: mirror ratio " + Ratio(p05.mirror, p1.mirror) + ", rough " + Ratio(p05.rough, p1.rough)
            + " (linear: 0.500; gamma: " + Mathf.GammaToLinearSpace(.5f).ToString("0.000", inv) + ")");
        Compare("probe at 1 vs the default (Level0) cube: the probe is used", a, p1);
        UnityEngine.Object.Destroy(probeGo);
        // Returning ends the routine: Finish writes the log and exits (still in Play mode).
    }

    static void Sphere(Shader lit, Vector3 position, float smoothness)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 1.1f;
        var m = new Material(lit);
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Metallic", 1f);
        m.SetFloat("_Smoothness", smoothness);
        go.GetComponent<Renderer>().sharedMaterial = m;
    }

    static Shot Render(string name)
    {
        testCam.Render();
        var previous = RenderTexture.active;
        RenderTexture.active = testRt;
        testRead.ReadPixels(new Rect(0, 0, testRt.width, testRt.height), 0, 0);
        testRead.Apply();
        RenderTexture.active = previous;
        File.WriteAllBytes(Path.Combine(OutDir, "rt_" + name + ".png"), testRead.EncodeToPNG());
        var px = testRead.GetPixels();
        var shot = new Shot { name = name, pixels = px, mirror = Mean(px, 0), rough = Mean(px, 1) };
        Line(name + ": mirror " + Fmt(shot.mirror) + ", rough " + Fmt(shot.rough));
        return shot;
    }

    // Mean colour inside the central disc of the left (0) or right (1) sphere.
    static Vector3 Mean(Color[] px, int which)
    {
        int w = 512, h = 256;
        var cx = which == 0 ? w * .5f - 74f : w * .5f + 74f;
        var cy = h * .5f;
        Vector3 sum = Vector3.zero;
        var n = 0;
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var dx = x - cx; var dy = y - cy;
            if (dx * dx + dy * dy > 40f * 40f) continue;
            var c = px[y * w + x];
            sum += new Vector3(c.r, c.g, c.b);
            n++;
        }
        return n > 0 ? sum / n : Vector3.zero;
    }

    static void Compare(string label, Shot x, Shot y)
    {
        double d = 0, peak = 0;
        for (var i = 0; i < x.pixels.Length; i++)
        {
            var e = Mathf.Abs(x.pixels[i].r - y.pixels[i].r) + Mathf.Abs(x.pixels[i].g - y.pixels[i].g) + Mathf.Abs(x.pixels[i].b - y.pixels[i].b);
            d += e / 3.0;
            peak = Math.Max(peak, e / 3.0);
        }
        Line("COMPARE " + label + ": mean abs diff " + (d / x.pixels.Length * 255.0).ToString("0.00", CultureInfo.InvariantCulture)
            + " /255, max " + (peak * 255.0).ToString("0", CultureInfo.InvariantCulture) + " /255");
    }

    static string State(string label)
    {
        var def = ReflectionProbe.defaultTexture;
        var hdr = ReflectionProbe.defaultTextureHDRDecodeValues;
        return "  [" + label + "] mode " + RenderSettings.defaultReflectionMode + ", custom " + (RenderSettings.customReflectionTexture != null ? RenderSettings.customReflectionTexture.name : "null")
            + ", ReflectionProbe.defaultTexture " + (def != null ? def.name + " (" + def.GetType().Name + ")" : "null")
            + ", decode " + hdr.ToString("F3") + ", intensity " + RenderSettings.reflectionIntensity.ToString("0.000", CultureInfo.InvariantCulture)
            + ", zone " + FrontRoomsZoneReflection.Zone + ", path " + FrontRoomsZoneReflection.Mode + ", fading " + FrontRoomsZoneReflection.Fading;
    }

    static string Fmt(Vector3 v) => "(" + v.x.ToString("0.000", CultureInfo.InvariantCulture) + ", " + v.y.ToString("0.000", CultureInfo.InvariantCulture) + ", " + v.z.ToString("0.000", CultureInfo.InvariantCulture) + ")";

    static string Ratio(Vector3 a, Vector3 b)
    {
        // Pixels are sRGB-encoded; compare in linear.
        float L(float c) => Mathf.GammaToLinearSpace(c);
        var la = L(a.x) + L(a.y) + L(a.z);
        var lb = L(b.x) + L(b.y) + L(b.z);
        return lb > 1e-5f ? (la / lb).ToString("0.000", CultureInfo.InvariantCulture) : "n/a";
    }

    // ------------------------------------------------------------------ window look-dev

    public static void RunWindowLookdevBatch()
    {
        var log = new StringBuilder();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        try { WindowLookdev(log); }
        catch (Exception e) { log.AppendLine("ERROR " + e); Debug.LogException(e); }
        File.WriteAllText(Path.Combine(OutDir, "window_lookdev.txt"), log.ToString());
        Debug.Log("[GlassVerify] window look-dev\n" + log);
    }

    static void WindowLookdev(StringBuilder log)
    {
        var map = FrontRoomsReflectionCapture.BuildMap();
        var world = map.world;
        var root = new GameObject("GLASS LOOKDEV").transform;
        FrontRoomsPostStack.Ensure(root);
        var built = typeof(FrontRoomsMapWorld).GetField("built", Private)?.GetValue(world) as IDictionary;
        var panes = new List<GameObject>();
        if (built != null)
            foreach (DictionaryEntry entry in built)
                if (entry.Value.GetType().GetField("windows", Public)?.GetValue(entry.Value) is IList windows)
                    foreach (var w in windows)
                        if (w.GetType().GetField("pane", Public)?.GetValue(w) is GameObject pane && pane != null) panes.Add(pane);
        log.AppendLine("window panes built: " + panes.Count);
        var spawn = world.SpawnWorldPosition;
        var floorY = world.transform.position.y;
        GameObject chosen = null;
        Vector3 normal = Vector3.forward, eye = Vector3.zero;
        FrontRoomsLook.ReflectionZone zone = FrontRoomsLook.ReflectionZone.Level0;
        foreach (var pane in panes.OrderBy(p => Vector3.Distance(p.transform.position, spawn)))
        {
            var s = pane.transform.lossyScale;
            var n = Mathf.Abs(s.x) < Mathf.Abs(s.z) ? Vector3.right : Vector3.forward;
            foreach (var side in new[] { 1f, -1f })
            {
                var p = pane.transform.position + n * side * 1.5f;
                p.y = floorY + ModuleUnits.PlayerEye;
                var info = world.ZoneOf(world.CellOf(p));
                if (info.height == ZoneHeight.Tall) continue;
                if (FrontRoomsReflectionCapture.Blocked(p)) continue;
                chosen = pane; normal = n * side; eye = p;
                zone = info.theme == ZoneTheme.Office ? FrontRoomsLook.ReflectionZone.Office : FrontRoomsLook.ReflectionZone.Level0;
                break;
            }
            if (chosen != null) break;
        }
        if (chosen == null) { log.AppendLine("no usable window"); return; }
        var centre = chosen.transform.position;
        var along = Vector3.Cross(Vector3.up, normal).normalized;
        log.AppendLine("pane " + chosen.name + " at " + centre.ToString("F2") + ", scale " + chosen.transform.lossyScale.ToString("F3") + ", camera side zone " + zone);
        FrontRoomsReflectionCapture.LightLampsFor(world, eye);

        var renderer = chosen.GetComponent<Renderer>();
        var oldMaterial = renderer.sharedMaterial;
        var oldScale = chosen.transform.localScale;
        var newMaterial = AssetDatabase.LoadAssetAtPath<Material>(FrontRoomsGlassSetup.SurfaceDir + "/Glass_Window.mat");
        var thinScale = oldScale;
        if (Mathf.Abs(normal.x) > .5f) thinScale.x = .006f; else thinScale.z = .006f;

        var shots = new List<(string name, Vector3 pos)>
        {
            ("straight_1.5m", eye),
            ("oblique_50deg", centre + (normal * Mathf.Cos(50f * Mathf.Deg2Rad) + along * Mathf.Sin(50f * Mathf.Deg2Rad)) * 1.5f),
            ("steep_60deg", centre + (normal * Mathf.Cos(60f * Mathf.Deg2Rad) + along * Mathf.Sin(60f * Mathf.Deg2Rad)) * 1.6f),
            ("close_0.7m", centre + normal * .7f),
        };
        for (var i = 1; i < shots.Count; i++) { var p = shots[i].pos; p.y = floorY + ModuleUnits.PlayerEye; shots[i] = (shots[i].name, p); }

        var defaultSky = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
        void GameReflection()
        {
            // What the game scene has today: Unity's default sky as the reflection, at 0.3.
            RenderSettings.skybox = defaultSky;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = .3f;
            DynamicGI.UpdateEnvironment();
        }

        var crackMaterial = newMaterial != null ? new Material(newMaterial) { name = "Glass_Window (crack test)" } : null;
        if (crackMaterial != null)
        {
            crackMaterial.SetFloat("_Crack", .55f);
            crackMaterial.SetVector("_ImpactUV", new Vector4(.42f, .58f, 0f, 0f));
            crackMaterial.SetFloat("_CrackSeed", 3f);
        }
        var palmMaterial = newMaterial != null ? new Material(newMaterial) { name = "Glass_Window (palm test)" } : null;
        if (palmMaterial != null)
        {
            palmMaterial.SetFloat("_Palm", 1f);
            palmMaterial.SetFloat("_Crack", .2f);
            palmMaterial.SetVector("_ImpactUV", new Vector4(.5f, .6f, 0f, 0f));
            palmMaterial.SetFloat("_CrackSeed", 1f);
        }

        var debugMaterial = newMaterial != null ? new Material(newMaterial) { name = "Glass_Window (mask debug)" } : null;
        if (debugMaterial != null) debugMaterial.SetFloat("_GrimeDebug", 1f);
        var variants = new List<(string name, Action setup)>
        {
            ("a_old_game", () => { renderer.sharedMaterial = oldMaterial; chosen.transform.localScale = oldScale; renderer.shadowCastingMode = ShadowCastingMode.On; GameReflection(); }),
            ("b_newglass_oldreflection", () => { renderer.sharedMaterial = newMaterial; chosen.transform.localScale = thinScale; renderer.shadowCastingMode = ShadowCastingMode.Off; GameReflection(); }),
            ("c_newglass_zonecube", () => { renderer.sharedMaterial = newMaterial; chosen.transform.localScale = thinScale; renderer.shadowCastingMode = ShadowCastingMode.Off; RenderSettings.skybox = null; FrontRoomsZoneReflection.SetImmediate(zone); }),
            ("d_crack_0.55", () => { renderer.sharedMaterial = crackMaterial; FrontRoomsZoneReflection.SetImmediate(zone); }),
            ("e_palm_crack_0.2", () => { renderer.sharedMaterial = palmMaterial; FrontRoomsZoneReflection.SetImmediate(zone); }),
            ("g_debug_masks", () => { renderer.sharedMaterial = debugMaterial; }),
            ("z_no_pane", () => { renderer.enabled = false; }),
        };
        var camera = MakeCamera(world.SightDistance);
        foreach (var (vname, setup) in variants)
        {
            setup();
            log.AppendLine(vname + ": reflection " + RenderSettings.defaultReflectionMode + " " + (RenderSettings.defaultReflectionMode == DefaultReflectionMode.Custom && RenderSettings.customReflectionTexture != null ? RenderSettings.customReflectionTexture.name : "sky")
                + " x" + RenderSettings.reflectionIntensity.ToString("0.00", CultureInfo.InvariantCulture) + ", material " + (renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "null"));
            foreach (var (sname, pos) in shots)
            {
                if (vname.StartsWith("d_") || vname.StartsWith("e_") || vname.StartsWith("g_"))
                    if (sname != "straight_1.5m" && sname != "close_0.7m") continue;
                camera.transform.position = pos;
                camera.transform.rotation = Quaternion.LookRotation(centre - pos);
                Shoot(camera, Path.Combine(OutDir, "window_" + vname + "_" + sname + ".png"));
            }
        }
        // The pane on its own in the dead-lamp state, same straight view.
        renderer.enabled = true;
        FrontRoomsZoneReflection.SetImmediate(FrontRoomsLook.ReflectionZone.DeadLamp);
        renderer.sharedMaterial = newMaterial;
        camera.transform.position = eye;
        camera.transform.rotation = Quaternion.LookRotation(centre - eye);
        Shoot(camera, Path.Combine(OutDir, "window_f_newglass_deadlampcube_straight_1.5m.png"));
        UnityEngine.Object.DestroyImmediate(camera.targetTexture);
    }

    static Camera MakeCamera(float far)
    {
        var camera = new GameObject("GLASS LOOKDEV camera").AddComponent<Camera>();
        camera.fieldOfView = 76f;
        camera.nearClipPlane = .06f;
        camera.farClipPlane = Mathf.Max(20f, far);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = FrontRoomsLook.FogColor;
        FrontRoomsPostStack.ConfigureCamera(camera);
        camera.targetTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        camera.enabled = false;
        return camera;
    }

    static void Shoot(Camera camera, string path)
    {
        camera.Render();
        camera.Render();
        var rt = camera.targetTexture;
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        var image = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        image.Apply();
        RenderTexture.active = previous;
        File.WriteAllBytes(path, image.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(image);
    }

    // ------------------------------------------------------------------ prop look-dev

    static readonly string[] GlassProps = { "Kit_DisplayCabinet", "Kit_Hutch", "Kit_WaterCooler", "Kit_WallClock", "Kit_VendingMachine", "Kit_DeskPhone" };

    public static void RunPropLookdevBatch()
    {
        var log = new StringBuilder();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        try { PropLookdev(log); }
        catch (Exception e) { log.AppendLine("ERROR " + e); Debug.LogException(e); }
        File.WriteAllText(Path.Combine(OutDir, "prop_lookdev.txt"), log.ToString());
        Debug.Log("[GlassVerify] prop look-dev\n" + log);
    }

    static void PropLookdev(StringBuilder log)
    {
        var root = new GameObject("GLASS PROP LOOKDEV").transform;
        FrontRoomsLook.ApplyAmbient();
        FrontRoomsPostStack.Ensure(root);
        const float width = 14f, depth = 8f, height = 2.9f;
        SimpleRoom(root, width, depth, height);
        var names = FrontRoomsKitLibrary.AllNames();
        var spawned = new List<(string name, GameObject go)>();
        var x = -width * .5f + 1.4f;
        foreach (var n in GlassProps)
        {
            if (!names.Contains(n)) { log.AppendLine("missing kit asset " + n); continue; }
            var info = FrontRoomsKitLibrary.GetInfo(n);
            var half = info != null ? Mathf.Max(info.Size.x, info.Size.z) * .5f : .5f;
            var y = n == "Kit_WallClock" ? 1.6f : 0f;
            var go = FrontRoomsKitLibrary.Spawn(n, root, new Vector3(x + half, y, depth - .5f), 180f, null, false);
            if (go != null) spawned.Add((n, go));
            x += half * 2f + .9f;
        }
        var propGlass = AssetDatabase.LoadAssetAtPath<Material>(FrontRoomsGlassSetup.SurfaceDir + "/Prop_Glass.mat");
        var bottle = AssetDatabase.LoadAssetAtPath<Material>(FrontRoomsGlassSetup.SurfaceDir + "/Prop_BottleBlue.mat");
        log.AppendLine("Prop_Glass shader " + (propGlass != null ? propGlass.shader.name : "missing") + ", Prop_BottleBlue shader " + (bottle != null ? bottle.shader.name : "missing"));
        // The saved values before this change: URP Lit, premultiplied + Preserve Specular.
        var oldGlass = OldLitGlass("Prop_Glass (old)", new Color(.82f, .88f, .88f, .16f), .92f);
        var oldBottle = OldLitGlass("Prop_BottleBlue (old)", new Color(.36f, .58f, .80f, .42f), .9f);
        var renderers = spawned.SelectMany(s => s.go.GetComponentsInChildren<Renderer>(true)).ToArray();
        var camera = MakeCamera(40f);
        foreach (var variant in new[] { "old", "new" })
        {
            foreach (var r in renderers)
            {
                var mats = r.sharedMaterials;
                for (var i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    var isGlass = mats[i] == propGlass || mats[i] == oldGlass;
                    var isBottle = mats[i] == bottle || mats[i] == oldBottle;
                    if (isGlass) mats[i] = variant == "old" ? oldGlass : propGlass;
                    if (isBottle) mats[i] = variant == "old" ? oldBottle : bottle;
                }
                r.sharedMaterials = mats;
            }
            if (variant == "old")
            {
                RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
                RenderSettings.reflectionIntensity = .3f;
                DynamicGI.UpdateEnvironment();
            }
            else
            {
                RenderSettings.skybox = null;
                FrontRoomsZoneReflection.SetImmediate(FrontRoomsLook.ReflectionZone.Office);
            }
            foreach (var (n, go) in spawned)
            {
                var info = FrontRoomsKitLibrary.GetInfo(n);
                var t = go.transform;
                var c = info != null ? t.TransformPoint((info.Min + info.Max) * .5f) : t.position + Vector3.up;
                var r = info != null ? Mathf.Max(info.Size.magnitude * .5f, .25f) : .6f;
                var dir = Quaternion.Euler(0f, -28f, 0f) * (t.rotation * Vector3.forward);
                var pos = c + dir * r * 2.4f + Vector3.up * r * .35f;
                camera.fieldOfView = 45f;
                camera.transform.position = pos;
                camera.transform.rotation = Quaternion.LookRotation(c - pos);
                Shoot(camera, Path.Combine(OutDir, "prop_" + n + "_" + variant + ".png"));
            }
        }
        log.AppendLine("props: " + string.Join(", ", spawned.Select(s => s.name)));
    }

    static Material OldLitGlass(string name, Color color, float smooth)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_BlendModePreserveSpecular", 1f);
        m.SetFloat("_SrcBlend", (float)BlendMode.One);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = (int)RenderQueue.Transparent;
        m.SetShaderPassEnabled("DepthOnly", false);
        m.SetShaderPassEnabled("SHADOWCASTER", false);
        return m;
    }

    static void SimpleRoom(Transform root, float width, float depth, float height)
    {
        var wall = FrontRoomsSurfaces.Room(RoomRule.Office, FrontRoomsSurfaces.Slot.Wall);
        var floor = FrontRoomsSurfaces.Room(RoomRule.Office, FrontRoomsSurfaces.Slot.Floor);
        var ceiling = FrontRoomsSurfaces.Room(RoomRule.Office, FrontRoomsSurfaces.Slot.Ceiling);
        Box(root, new Vector3(0f, -.1f, depth * .5f), new Vector3(width + .4f, .2f, depth + .4f), floor);
        Box(root, new Vector3(0f, height + .1f, depth * .5f), new Vector3(width + .4f, .2f, depth + .4f), ceiling);
        Box(root, new Vector3(0f, height * .5f, -.08f), new Vector3(width, height, .16f), wall);
        Box(root, new Vector3(0f, height * .5f, depth + .08f), new Vector3(width, height, .16f), wall);
        Box(root, new Vector3(-width * .5f - .08f, height * .5f, depth * .5f), new Vector3(.16f, height, depth), wall);
        Box(root, new Vector3(width * .5f + .08f, height * .5f, depth * .5f), new Vector3(.16f, height, depth), wall);
        var lens = FrontRoomsSurfaces.OfficeLens;
        var index = 0;
        for (var lx = -width * .5f + 1.8f; lx < width * .5f - 1f; lx += 3f)
        for (var lz = 1.8f; lz < depth - 1f; lz += 3f)
        {
            Box(root, new Vector3(lx, height - .033f, lz), new Vector3(.54f, .006f, 1.14f), lens);
            var light = new GameObject("lamp " + index).AddComponent<Light>();
            light.transform.SetParent(root, false);
            light.transform.localPosition = new Vector3(lx, height - .05f, lz);
            light.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            light.type = LightType.Spot;
            light.spotAngle = 162f;
            light.innerSpotAngle = 96f;
            light.range = 10f;
            light.intensity = 5.5f;
            light.color = new Color(1f, .96f, .88f);
            light.shadows = index % 3 == 0 ? LightShadows.Soft : LightShadows.None;
            index++;
        }
    }

    static void Box(Transform parent, Vector3 position, Vector3 size, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = size;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = material;
    }
}
