using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// P0 gates for the wallpaper "sandwich" (Documentation/research/wallpaper_motion,
/// §7): the paper/print split in FrontRooms/Surface (_FR_PRINT).
///   T1a  pipeline parity: legacy albedo regenerated with keep_hue = 0 vs the split
///        material, both with TODAY's normal/mask, same camera and lights. Gate: wall
///        region mean |delta| <= 1/255 per channel, in a 6 x 9 m room (4 views). The
///        same comparison down a 25 m corridor (side walls at 3-30 deg and beyond, fog
///        on and off, per distance band) is reported with its own pass flag.
///   T1b  today's legacy (with keep_hue) vs the split: reported, no gate (the hue loss).
///   Raking sheet: legacy vs the split with the NEW ink-free paper normal/mask, three
///        raking angles, two distances (art sign-off, no numeric gate).
///   T2   lighting independence: one point light, 12 positions grazing -> frontal,
///        specular-only renders with print A (chevron, static) vs print B (a different
///        pattern, static and live) must be bit-identical; wear masks identical too.
///   Invariance: a room of non-wallpaper materials, new shader vs the pre-P0 shader
///        (Hidden/FrontRooms/Surface P0 Baseline), bit-identical.
/// The legacy and keep_hue = 0 reference textures live in RefDir (outside Resources, so
/// builds never ship them). The rooms are built here, from public APIs only. Every object
/// the run creates is destroyed and the print globals are unbound when it ends.
/// Writes PNGs and print_p0.json to Verification/print_p0. Nothing is saved to a scene.
/// Batch: -executeMethod FrontRoomsPrintP0Test.RunBatch (needs graphics).
/// </summary>
public static class FrontRoomsPrintP0Test
{
    const int W = 1920, H = 1080;
    const string TexDir = "Assets/Resources/Surfaces/Textures/";
    internal const string RefDir = "Assets/Editor/Rendering/PrintP0/Ref/";
    static readonly Vector3 RoomOrigin = Vector3.zero;
    static readonly Vector3 CorridorOrigin = new Vector3(-30f, 0f, 0f);
    static readonly Vector3 WallCentre = new Vector3(40f, 1.45f, 0f);   // test wall for raking + T2, front face at z = -0.08

    static string OutDir => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "print_p0");
    static readonly int ClockId = Shader.PropertyToID("_FR_PrintClock");
    static readonly int PrintId = Shader.PropertyToID("_FR_Print");
    static readonly int DebugViewId = Shader.PropertyToID("_FR_DebugView");
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    [MenuItem("FrontRooms/Rendering/Print P0 gates (T1, T2, raking, invariance)")]
    public static void Run() => RunInternal();

    /// <summary>Batch entry: reimports the print first (its mips come from the importer's
    /// FrontRoomsPrintMips pass, which a code change alone does not rerun), then runs the gates.</summary>
    public static void RunBatch()
    {
        AssetDatabase.ImportAsset(TexDir + "Wallpaper_Print_P.png", ImportAssetOptions.ForceUpdate);
        RunInternal();
    }

    /// <summary>Logs how the wallpaper textures import in this editor (override, target, format).</summary>
    public static void Probe()
    {
        var sb = new StringBuilder();
        sb.AppendLine("activeBuildTarget " + EditorUserBuildSettings.activeBuildTarget + ", overrideTextureCompression " + EditorUserBuildSettings.overrideTextureCompression);
        foreach (var path in new[] { RefDir + "Wallpaper_Chevron_A", TexDir + "Wallpaper_Paper_M", TexDir + "Wallpaper_Print_P", TexDir + "Wallpaper_Paper_N" })
        {
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(path + ".png");
            var imp = (TextureImporter)AssetImporter.GetAtPath(path + ".png");
            sb.AppendLine(path + ": " + t.graphicsFormat + " mips " + t.mipmapCount + " aniso " + t.anisoLevel + " filter " + t.filterMode + " wrap " + t.wrapMode
                + " | importer compression " + imp.textureCompression + " sRGB " + imp.sRGBTexture + " auto(OSX) " + imp.GetAutomaticFormat("Standalone"));
        }
        Debug.Log("[PrintP0 probe]\n" + sb);
        Directory.CreateDirectory(OutDir);
        File.WriteAllText(Path.Combine(OutDir, "import_probe.txt"), sb.ToString());
    }

    // ------------------------------------------------------------- ownership
    // Everything the run creates outside the scene (materials, textures, arrays, render
    // textures, the volume profile) is registered here and destroyed in RunInternal's finally.
    static readonly List<UnityEngine.Object> Owned = new List<UnityEngine.Object>();

    static T Own<T>(T o) where T : UnityEngine.Object
    {
        if (o != null) Owned.Add(o);
        return o;
    }

    // ------------------------------------------------------------------ images
    sealed class Img
    {
        public int w, h; public float[] px;   // RGBA float, Unity row order (bottom-up)
        public Img(int w, int h) { this.w = w; this.h = h; px = new float[w * h * 4]; }
    }

    static Img ReadFloat(RenderTexture rt)
    {
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBAFloat, false, true);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply(false);
        RenderTexture.active = prev;
        var img = new Img(rt.width, rt.height);
        tex.GetRawTextureData<float>().CopyTo(img.px);
        UnityEngine.Object.DestroyImmediate(tex);
        return img;
    }

    static byte[] ReadBytes(RenderTexture rt)
    {
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply(false);
        RenderTexture.active = prev;
        var b = tex.GetRawTextureData<byte>().ToArray();
        UnityEngine.Object.DestroyImmediate(tex);
        return b;
    }

    static float Srgb(float x)
    {
        x = Mathf.Clamp01(x);
        return x <= .0031308f ? 12.92f * x : 1.055f * Mathf.Pow(x, 1f / 2.4f) - .055f;
    }

    static void SavePng(string path, int w, int h, Func<int, Color32> pixel)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        var c = new Color32[w * h];
        for (var i = 0; i < c.Length; i++) c[i] = pixel(i);
        tex.SetPixels32(c);
        tex.Apply(false);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
    }

    static Color32 Heat(float v)   // v in 8-bit levels; 1 level = dark red, 4+ = white
    {
        var t = Mathf.Clamp01(v / 4f);
        var r = Mathf.Clamp01(t * 3f); var g = Mathf.Clamp01(t * 3f - 1f); var b = Mathf.Clamp01(t * 3f - 2f);
        return new Color32((byte)(r * 255), (byte)(g * 255), (byte)(b * 255), 255);
    }

    // --------------------------------------------------------------- stats
    sealed class Stats
    {
        public double[] mean = new double[3], p99 = new double[3], max = new double[3], signed = new double[3];
        public long n;
        public string Json() => "{\"mean\": [" + J(mean) + "], \"p99\": [" + J(p99) + "], \"max\": [" + J(max) + "], \"signed_mean\": [" + J(signed) + "], \"n\": " + n + "}";
        static string J(double[] a) => string.Join(", ", a.Select(v => v.ToString("0.####", Inv)));
    }

    /// <summary>|a - b| per channel over the mask; signed_mean is mean(b - a).</summary>
    static Stats Diff(float[] a, float[] b, bool[] mask)
    {
        var s = new Stats();
        var lists = new List<float>[3] { new List<float>(), new List<float>(), new List<float>() };
        var signed = new double[3];
        var pixels = a.Length / 3;
        for (var i = 0; i < pixels; i++)
        {
            if (mask != null && !mask[i]) continue;
            for (var c = 0; c < 3; c++) { lists[c].Add(Mathf.Abs(a[i * 3 + c] - b[i * 3 + c])); signed[c] += b[i * 3 + c] - a[i * 3 + c]; }
        }
        for (var c = 0; c < 3; c++)
        {
            var l = lists[c];
            if (l.Count == 0) continue;
            l.Sort();
            double sum = 0; foreach (var v in l) sum += v;
            s.mean[c] = sum / l.Count;
            s.signed[c] = signed[c] / l.Count;
            s.p99[c] = l[Math.Min(l.Count - 1, (int)(l.Count * .99))];
            s.max[c] = l[l.Count - 1];
            s.n = l.Count;
        }
        return s;
    }

    static Stats Pool(IEnumerable<Stats> parts)   // pooled mean (n-weighted), worst p99/max
    {
        var s = new Stats(); long n = 0;
        foreach (var p in parts)
        {
            for (var c = 0; c < 3; c++) { s.mean[c] += p.mean[c] * p.n; s.signed[c] += p.signed[c] * p.n; s.p99[c] = Math.Max(s.p99[c], p.p99[c]); s.max[c] = Math.Max(s.max[c], p.max[c]); }
            n += p.n;
        }
        for (var c = 0; c < 3; c++) { s.mean[c] /= Math.Max(1, n); s.signed[c] /= Math.Max(1, n); }
        s.n = n;
        return s;
    }

    /// <summary>Linear HDR -> sRGB*255 (float, unquantised) with a fixed exposure.</summary>
    static float[] Display(Img img, float exposure)
    {
        var o = new float[img.w * img.h * 3];
        for (var i = 0; i < img.w * img.h; i++)
            for (var c = 0; c < 3; c++) o[i * 3 + c] = 255f * Srgb(img.px[i * 4 + c] * exposure);
        return o;
    }

    static float[] Bytes3(byte[] b) { var o = new float[b.Length]; for (var i = 0; i < b.Length; i++) o[i] = b[i]; return o; }

    static float ExposureFor(Img img, bool[] mask)
    {
        var v = new List<float>();
        for (var i = 0; i < img.w * img.h; i++)
            if (mask == null || mask[i]) v.Add(Mathf.Max(img.px[i * 4], Mathf.Max(img.px[i * 4 + 1], img.px[i * 4 + 2])));
        if (v.Count == 0) return 1f;
        v.Sort();
        return .92f / Mathf.Max(1e-4f, v[(int)(v.Count * .995f)]);
    }

    // ------------------------------------------------------------- the rig
    static Camera cam;
    static UniversalAdditionalCameraData camData;
    static RenderTexture rtFloat, rtBytes, rtFloatSmall, rtRake;

    static void Pose(Vector3 position, Vector3 euler, float fov)
    {
        cam.transform.SetPositionAndRotation(position, Quaternion.Euler(euler));
        cam.fieldOfView = fov;
    }

    static Img RenderFloat(bool post, RenderTexture rt = null)
    {
        rt = rt ?? rtFloat;
        camData.renderPostProcessing = post;
        cam.targetTexture = rt;
        cam.Render(); cam.Render();
        return ReadFloat(rt);
    }

    static byte[] RenderBytes()
    {
        camData.renderPostProcessing = true;
        cam.targetTexture = rtBytes;
        cam.Render(); cam.Render();
        return ReadBytes(rtBytes);
    }

    static void SetDebug(int mode)
    {
        if (mode == 0) Shader.DisableKeyword("_FR_DEBUG_VIEW"); else Shader.EnableKeyword("_FR_DEBUG_VIEW");
        Shader.SetGlobalFloat(DebugViewId, mode);
    }

    static void ResetPrintGlobals()
    {
        Shader.SetGlobalVector(ClockId, Vector4.zero);
    }

    static Texture2D LoadTex(string stem) => AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + stem + ".png");
    static Texture2D LoadRef(string stem) => AssetDatabase.LoadAssetAtPath<Texture2D>(RefDir + stem + ".png");

    static Material Clone(Material src, string name) => Own(new Material(src) { name = name, hideFlags = HideFlags.DontSave });

    /// <summary>Legacy one-layer wallpaper: keyword off, given albedo, given N/S (both from RefDir).</summary>
    static Material Legacy(Material production, string albedo, string ns, string name)
    {
        var m = Clone(production, name);
        m.DisableKeyword("_FR_PRINT");
        m.SetFloat("_UsePrint", 0f);
        m.SetTexture("_BaseMap", LoadRef(albedo));
        m.SetTexture("_BumpMap", LoadRef(ns + "_N"));
        m.SetTexture("_MaskMap", LoadRef(ns + "_S"));
        return m;
    }

    /// <summary>The production split (paper + print) with a reference N/S from RefDir (today's).</summary>
    static Material SplitWithNS(Material production, string ns, string name)
    {
        var m = Clone(production, name);
        m.SetTexture("_BaseMap", LoadTex("Wallpaper_Paper_M"));
        m.SetTexture("_PrintTex", LoadTex("Wallpaper_Print_P"));
        m.SetTexture("_BumpMap", LoadRef(ns + "_N"));
        m.SetTexture("_MaskMap", LoadRef(ns + "_S"));
        return m;
    }

    // ------------------------------------------------------------ the rooms
    // The look-dev room of FrontRoomsKitLookdev (walls, cove, floor, ceiling, troffer grid
    // with spot lights), rebuilt from public APIs so this file does not depend on that
    // file's private members. segment > 0 splits the walls into pieces of that length
    // (same shading: the UVs are world-projected) so masks can pick distance bands.
    const float RoomHeight = 2.9f, GridX = .6f, GridZ = 1.2f;

    static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = size;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    static Transform BuildRoom(Transform root, string name, Vector3 origin, float width, float depth, RoomRule rule, float segment = 0f)
    {
        var room = new GameObject(name).transform;
        room.SetParent(root, false);
        room.localPosition = origin;
        var wall = FrontRoomsSurfaces.Room(rule, FrontRoomsSurfaces.Slot.Wall);
        Box(room, "floor", new Vector3(0f, -.1f, depth * .5f), new Vector3(width + .4f, .2f, depth + .4f), FrontRoomsSurfaces.Room(rule, FrontRoomsSurfaces.Slot.Floor));
        Box(room, "ceiling", new Vector3(0f, RoomHeight + .1f, depth * .5f), new Vector3(width + .4f, .2f, depth + .4f), FrontRoomsSurfaces.Room(rule, FrontRoomsSurfaces.Slot.Ceiling));
        const float t = .16f;
        Wall(room, wall, new Vector3(-width * .5f, 0f, 0f), Vector3.right, width, t, new Vector3(0f, 0f, -t * .5f + .08f), segment);
        Wall(room, wall, new Vector3(-width * .5f, 0f, depth), Vector3.right, width, t, new Vector3(0f, 0f, t * .5f - .08f), segment);
        Wall(room, wall, new Vector3(-width * .5f, 0f, 0f), Vector3.forward, depth, t, new Vector3(-t * .5f + .08f, 0f, 0f), segment);
        Wall(room, wall, new Vector3(width * .5f, 0f, 0f), Vector3.forward, depth, t, new Vector3(t * .5f - .08f, 0f, 0f), segment);
        Troffers(room, width, depth, rule == RoomRule.Office ? FrontRoomsSurfaces.OfficeLens : FrontRoomsSurfaces.TrofferLens);
        return room;
    }

    static void Wall(Transform room, Material wall, Vector3 start, Vector3 along, float length, float thickness, Vector3 inset, float segment)
    {
        var pieces = segment > 0f ? Mathf.Max(1, Mathf.RoundToInt(length / segment)) : 1;
        for (var k = 0; k < pieces; k++)
        {
            float a = length * k / pieces, b = length * (k + 1) / pieces;
            var mid = start + along * ((a + b) * .5f) + inset;
            var size = along.x != 0 ? new Vector3(b - a, RoomHeight, thickness) : new Vector3(thickness, RoomHeight, b - a);
            Box(room, "wall", mid + Vector3.up * (RoomHeight * .5f), size, wall);
            var coveSize = along.x != 0 ? new Vector3(b - a, .1f, thickness + .02f) : new Vector3(thickness + .02f, .1f, b - a);
            Box(room, "cove", mid + Vector3.up * .05f, coveSize, FrontRoomsSurfaces.CoveBase);
        }
    }

    static void Troffers(Transform room, float width, float depth, Material lens)
    {
        var pan = FrontRoomsSurfaces.PaintedMetal;
        var index = 0;
        var half = Mathf.FloorToInt(width * .5f / GridX);
        for (var cx = -half + 2; cx <= half - 2; cx += 4)
        for (var cz = 1; (cz + 1) * GridZ < depth - .5f; cz += 3)
        {
            var fixture = new GameObject("troffer " + index).transform;
            fixture.SetParent(room, false);
            fixture.localPosition = new Vector3((cx + .5f) * GridX, 0f, (cz + .5f) * GridZ);
            Box(fixture, "pan", new Vector3(0f, RoomHeight - .015f, 0f), new Vector3(GridX - .012f, .03f, GridZ - .012f), pan);
            Box(fixture, "lens", new Vector3(0f, RoomHeight - .033f, 0f), new Vector3(GridX - .06f, .006f, GridZ - .06f), lens);
            var light = new GameObject("light").AddComponent<Light>();
            light.transform.SetParent(fixture, false);
            light.transform.localPosition = new Vector3(0f, RoomHeight - .05f, 0f);
            light.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            light.type = LightType.Spot;
            light.spotAngle = 162f;
            light.innerSpotAngle = 96f;
            light.range = 10f;
            light.intensity = 5f;
            light.color = new Color(1f, .96f, .88f);
            light.shadows = index % 3 == 0 ? LightShadows.Soft : LightShadows.None;
            light.shadowStrength = .9f;
            light.shadowNearPlane = .1f;
            index++;
        }
    }

    // -------------------------------------------------------------- main
    static void RunInternal()
    {
        var asyncWas = ShaderUtil.allowAsyncCompilation;
        ShaderUtil.allowAsyncCompilation = false;
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        var precisionWas = urp != null ? urp.hdrColorBufferPrecision : HDRColorBufferPrecision._32Bits;
        var renderScaleWas = urp != null ? urp.renderScale : 1f;
        var json = new StringBuilder();
        var report = new List<string>();
        Directory.CreateDirectory(OutDir);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(scene);
        try
        {
            // 64-bit HDR colour buffer: T2's bit-identity is checked at half precision, not 11/10 bit.
            if (urp != null) urp.hdrColorBufferPrecision = HDRColorBufferPrecision._64Bits;
            ResetPrintGlobals();
            SetDebug(0);
            var root = new GameObject("PRINT P0 TEST").transform;
            FrontRoomsLook.ApplyAmbient();
            FrontRoomsPostStack.Ensure(root);
            // Clean post: grain and dithering are random per frame; lens distortion and
            // chromatic aberration move pixels off the wall mask. Everything else stays.
            var clean = Own(ScriptableObject.CreateInstance<VolumeProfile>());
            clean.Add<FilmGrain>(true).intensity.Override(0f);
            clean.Add<LensDistortion>(true).intensity.Override(0f);
            clean.Add<ChromaticAberration>(true).intensity.Override(0f);
            foreach (var component in clean.components) Own(component);
            var cleanGo = new GameObject("Post / P0 clean");
            cleanGo.transform.SetParent(root, false);
            var cleanVol = cleanGo.AddComponent<Volume>();
            cleanVol.isGlobal = true; cleanVol.priority = 100f; cleanVol.sharedProfile = clean;

            var camGo = new GameObject("P0 camera");
            camGo.transform.SetParent(root, false);
            cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = .03f; cam.farClipPlane = 80f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            FrontRoomsPostStack.ConfigureCamera(cam);
            camData = cam.GetUniversalAdditionalCameraData();
            camData.dithering = false;
            rtFloat = Own(new RenderTexture(W, H, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear) { antiAliasing = 4 });
            rtFloatSmall = Own(new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear) { antiAliasing = 4 });
            rtRake = Own(new RenderTexture(960, 540, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear) { antiAliasing = 4 });
            rtBytes = Own(new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 });

            json.Append("{\n");
            var t1Pass = RunT1(root, json, report);
            var (corridorFogOn, corridorFogOff) = RunT1Corridor(root, json, report);
            var invPass = RunInvariance(root, json, report);
            // Raking + T2 use one light: dim the ambient, no fog.
            RenderSettings.fog = false;
            RenderSettings.ambientSkyColor = FrontRoomsLook.AmbientSky * .25f;
            RenderSettings.ambientEquatorColor = FrontRoomsLook.AmbientEquator * .25f;
            RenderSettings.ambientGroundColor = FrontRoomsLook.AmbientGround * .25f;
            DynamicGI.UpdateEnvironment();
            RunRaking(root, report);
            var t2Pass = RunT2(root, json, report);
            RunLiveCheck(root, json, report, urp);
            var mipsOk = CheckPrintMips(json, report);
            WriteMemory(json, report);
            WriteShaderStats(json);
            json.Append("  \"gates\": {\"T1a\": " + B(t1Pass) + ", \"T2\": " + B(t2Pass) + ", \"invariance\": " + B(invPass)
                + ", \"T1a_corridor_fog_on (reported)\": " + B(corridorFogOn) + ", \"T1a_corridor_fog_off (reported)\": " + B(corridorFogOff)
                + ", \"print_mips_are_FrontRoomsPrintMips (reported)\": " + B(mipsOk) + "}\n}\n");
            File.WriteAllText(Path.Combine(OutDir, "print_p0.json"), json.ToString());
            File.WriteAllText(Path.Combine(OutDir, "print_p0_summary.txt"), string.Join("\n", report) + "\n");
            Debug.Log("[PrintP0] T1a " + (t1Pass ? "PASS" : "FAIL") + ", T2 " + (t2Pass ? "PASS" : "FAIL") + ", invariance " + (invPass ? "PASS" : "FAIL")
                + ", corridor fog on " + (corridorFogOn ? "pass" : "over") + " / off " + (corridorFogOff ? "pass" : "over") + "\n" + string.Join("\n", report));
        }
        catch (Exception e)
        {
            Debug.LogError("[PrintP0] " + e);
            File.WriteAllText(Path.Combine(OutDir, "print_p0_error.txt"), e.ToString());
        }
        finally
        {
            SetDebug(0);
            ResetPrintGlobals();
            Shader.SetGlobalTexture(PrintId, null);   // never leave a test array bound
            if (urp != null) { urp.hdrColorBufferPrecision = precisionWas; urp.renderScale = renderScaleWas; }
            ShaderUtil.allowAsyncCompilation = asyncWas;
            if (cam != null) cam.targetTexture = null;
            foreach (var o in Owned)
            {
                if (o is RenderTexture rt) rt.Release();
                if (o != null) UnityEngine.Object.DestroyImmediate(o);
            }
            Owned.Clear();
            cam = null; camData = null; rtFloat = rtFloatSmall = rtRake = rtBytes = null;
            testWall = null; pointLight = null; walls.Clear();
            printB = null; arrayB = arrayAB = null;
            FrontRoomsLook.ApplyAmbient();
            if (!Application.isBatchMode) EditorSceneManager.CloseScene(scene, true);
        }
    }

    static string B(bool v) => v ? "true" : "false";

    // ------------------------------------------------------------------ T1
    struct View { public string name; public Vector3 pos, euler; public float fov; }

    static readonly View[] T1Views =
    {
        new View { name = "front2m", pos = new Vector3(.4f, 1.55f, 7.0f), euler = new Vector3(2f, 0f, 0f), fov = 76f },
        new View { name = "close05", pos = new Vector3(.2f, 1.45f, 8.4f), euler = new Vector3(0f, 0f, 0f), fov = 60f },
        new View { name = "oblique", pos = new Vector3(1.8f, 1.6f, .6f), euler = new Vector3(3f, -20f, 0f), fov = 76f },
        new View { name = "far", pos = new Vector3(0f, 1.6f, .3f), euler = new Vector3(0f, 0f, 0f), fov = 76f },
    };

    // A 2.4 x 25 m corridor: the camera 1.4 m from the left wall and 0.8 m from the right one,
    // looking down it, sees the side walls from ~50 deg near to ~2-3 deg at 24 m (P1 T3's
    // 3-30 deg range and beyond), plus the end wall head-on at 24.4 m.
    const float CorridorWidth = 2.4f, CorridorDepth = 25f;
    static readonly View CorridorView = new View { name = "corridor", pos = new Vector3(.3f, 1.6f, .6f), euler = new Vector3(0f, 0f, 0f), fov = 76f };
    static readonly (string name, float z0, float z1)[] CorridorBands = { ("0-8m", 0f, 8f), ("8-16m", 8f, 16f), ("16-25m", 16f, 99f) };

    static List<Renderer> walls = new List<Renderer>();

    /// <summary>Wall-region mask: an unlit render with white on the chosen walls, eroded 1 px.</summary>
    static bool[] WallMask(Transform room, Func<Renderer, bool> white = null)
    {
        white = white ?? (r => walls.Contains(r));
        var all = room.GetComponentsInChildren<Renderer>();
        var saved = all.Select(r => r.sharedMaterial).ToArray();
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        var black = Own(new Material(unlit) { hideFlags = HideFlags.DontSave }); black.SetColor("_BaseColor", Color.black);
        var whiteMat = Own(new Material(unlit) { hideFlags = HideFlags.DontSave }); whiteMat.SetColor("_BaseColor", Color.white);
        for (var i = 0; i < all.Length; i++) all[i].sharedMaterial = white(all[i]) ? whiteMat : black;
        var fogWas = RenderSettings.fog; RenderSettings.fog = false;
        var img = RenderFloat(false);
        RenderSettings.fog = fogWas;
        for (var i = 0; i < all.Length; i++) all[i].sharedMaterial = saved[i];
        var m = new bool[img.w * img.h];
        for (var i = 0; i < m.Length; i++) m[i] = img.px[i * 4] > .25f;   // white unlit wall (URP Unlit still applies SSAO near corners); edges eroded below
        var e = new bool[m.Length];
        for (var y = 1; y < img.h - 1; y++)
            for (var x = 1; x < img.w - 1; x++)
            {
                var i = y * img.w + x;
                e[i] = m[i] && m[i - 1] && m[i + 1] && m[i - img.w] && m[i + img.w];
            }
        return e;
    }

    static void SetWalls(Material m) { foreach (var r in walls) r.sharedMaterial = m; }

    static bool RunT1(Transform root, StringBuilder json, List<string> report)
    {
        var room = BuildRoom(root, "T1 room", RoomOrigin, 6f, 9f, RoomRule.Lobby);
        var wallMat = FrontRoomsSurfaces.Room(RoomRule.Lobby, FrontRoomsSurfaces.Slot.Wall);
        walls = room.GetComponentsInChildren<Renderer>().Where(r => r.sharedMaterial == wallMat).ToList();
        var masks = new Dictionary<string, bool[]>();
        foreach (var v in T1Views) { Pose(v.pos, v.euler, v.fov); masks[v.name] = WallMask(room); }
        var pass = RunT1Set("T1", "", T1Views, masks, null, true, json, report);
        room.gameObject.SetActive(false);
        return pass;
    }

    /// <summary>T1a down the corridor, fog on (the game's) and off; per distance band. Reported, own pass flags.</summary>
    static (bool fogOn, bool fogOff) RunT1Corridor(Transform root, StringBuilder json, List<string> report)
    {
        var room = BuildRoom(root, "T1 corridor", CorridorOrigin, CorridorWidth, CorridorDepth, RoomRule.Lobby, 3f);
        var wallMat = FrontRoomsSurfaces.Room(RoomRule.Lobby, FrontRoomsSurfaces.Slot.Wall);
        walls = room.GetComponentsInChildren<Renderer>().Where(r => r.sharedMaterial == wallMat).ToList();
        var view = CorridorView; view.pos += CorridorOrigin;
        var views = new[] { view };
        Pose(view.pos, view.euler, view.fov);
        var masks = new Dictionary<string, bool[]> { [view.name] = WallMask(room) };
        var bands = new Dictionary<string, Dictionary<string, bool[]>>();
        foreach (var (name, z0, z1) in CorridorBands)
        {
            var inBand = walls.Where(r => { var z = r.bounds.center.z - CorridorOrigin.z; return z >= z0 && z < z1; }).ToList();
            bands[name] = new Dictionary<string, bool[]> { [view.name] = WallMask(room, r => inBand.Contains(r)) };
        }
        var fogWas = RenderSettings.fog;
        RenderSettings.fog = true;
        var on = RunT1Set("T1_corridor_fog_on", "_fogOn", views, masks, bands, true, json, report);
        RenderSettings.fog = false;
        var off = RunT1Set("T1_corridor_fog_off", "_fogOff", views, masks, bands, false, json, report);
        RenderSettings.fog = fogWas;
        room.gameObject.SetActive(false);
        return (on, off);
    }

    static bool RunT1Set(string key, string tag, View[] views, Dictionary<string, bool[]> masks, Dictionary<string, Dictionary<string, bool[]>> bands, bool saveFrames, StringBuilder json, List<string> report)
    {
        var cases = new (string name, Material production, string legacy)[]
        {
            ("Lobby", FrontRoomsSurfaces.Get("L0_Wallpaper"), "Wallpaper_Chevron"),
            ("Shift", FrontRoomsSurfaces.Get("L0_Wallpaper_Shift"), "Wallpaper_Chevron"),
            ("Exit", FrontRoomsSurfaces.Get("Exit_Wallpaper"), "Wallpaper_Chevron_Cold"),
        };
        var allA = new List<Stats>(); var allPost = new List<Stats>();
        var pass = true;
        json.Append("  \"" + key + "\": {\n");
        var formats = "";
        foreach (var (name, production, legacy) in cases)
        {
            var refNoHue = Legacy(production, legacy + "_NoHue_A", legacy, name + " ref keep_hue=0");
            var today = Legacy(production, legacy + "_A", legacy, name + " today");
            var split = SplitWithNS(production, legacy, name + " split, today's N/S");
            formats = "ref " + refNoHue.GetTexture("_BaseMap").graphicsFormat + ", paper " + split.GetTexture("_BaseMap").graphicsFormat + ", print " + split.GetTexture("_PrintTex").graphicsFormat;
            json.Append("    \"" + name + "\": {\n");
            var caseA = new List<Stats>(); var caseB = new List<Stats>(); var casePost = new List<Stats>(); var casePostB = new List<Stats>();
            foreach (var v in views)
            {
                Pose(v.pos, v.euler, v.fov);
                var mask = masks[v.name];
                ResetPrintGlobals();
                SetWalls(refNoHue); var iRef = RenderFloat(false); var bRef = RenderBytes();
                SetWalls(split); var iSplit = RenderFloat(false); var bSplit = RenderBytes();
                SetWalls(today); var iToday = RenderFloat(false); var bToday = RenderBytes();
                var exp = ExposureFor(iRef, mask);
                var dRef = Display(iRef, exp); var dSplit = Display(iSplit, exp); var dToday = Display(iToday, exp);
                var sA = Diff(dRef, dSplit, mask); var sB = Diff(dToday, dSplit, mask);
                var pRef = Bytes3(bRef); var pSplit = Bytes3(bSplit); var pToday = Bytes3(bToday);
                var sPost = Diff(pRef, pSplit, mask); var sPostB = Diff(pToday, pSplit, mask);
                caseA.Add(sA); caseB.Add(sB); casePost.Add(sPost); casePostB.Add(sPostB);
                var bandJson = "";
                if (bands != null)
                    foreach (var kv in bands)
                    {
                        var bm = kv.Value[v.name];
                        var bl = Diff(dRef, dSplit, bm); var bp = Diff(pRef, pSplit, bm);
                        bandJson += ", \"band_" + kv.Key + "\": {\"T1a_lit_hdr\": " + bl.Json() + ", \"T1a_film_post\": " + bp.Json() + "}";
                        report.Add($"{key} {name} band {kv.Key}: T1a lit mean {F(bl.mean)} signed {F(bl.signed)} | film mean {F(bp.mean)} signed {F(bp.signed)} ({bl.n} px)");
                    }
                json.Append("      \"" + v.name + "\": {\"exposure\": " + exp.ToString("0.####", Inv) + ", \"wall_px\": " + sA.n
                    + ", \"T1a_lit_hdr\": " + sA.Json() + ", \"T1b_lit_hdr\": " + sB.Json()
                    + ", \"T1a_film_post\": " + sPost.Json() + ", \"T1b_film_post\": " + sPostB.Json() + bandJson + "},\n");
                // Heatmaps (max channel |delta| in 8-bit levels: 1 dark red, 2 red, 3 yellow, 4+ white).
                var stem = Path.Combine(OutDir, "T1_" + name + "_" + v.name);
                SavePng(stem + "_heat_T1a" + tag + ".png", W, H, i => mask[i] ? Heat(Mathf.Max(Mathf.Abs(dRef[i * 3] - dSplit[i * 3]), Mathf.Max(Mathf.Abs(dRef[i * 3 + 1] - dSplit[i * 3 + 1]), Mathf.Abs(dRef[i * 3 + 2] - dSplit[i * 3 + 2])))) : new Color32(20, 20, 40, 255));
                SavePng(stem + "_heat_T1b" + tag + ".png", W, H, i => mask[i] ? Heat(Mathf.Max(Mathf.Abs(dToday[i * 3] - dSplit[i * 3]), Mathf.Max(Mathf.Abs(dToday[i * 3 + 1] - dSplit[i * 3 + 1]), Mathf.Abs(dToday[i * 3 + 2] - dSplit[i * 3 + 2])))) : new Color32(20, 20, 40, 255));
                if (saveFrames)
                {
                    SetWalls(production); var bNew = RenderBytes();
                    SavePng(stem + "_ref_nohue" + tag + ".png", W, H, i => new Color32(bRef[i * 3], bRef[i * 3 + 1], bRef[i * 3 + 2], 255));
                    SavePng(stem + "_split_todayNS" + tag + ".png", W, H, i => new Color32(bSplit[i * 3], bSplit[i * 3 + 1], bSplit[i * 3 + 2], 255));
                    SavePng(stem + "_today_legacy" + tag + ".png", W, H, i => new Color32(bToday[i * 3], bToday[i * 3 + 1], bToday[i * 3 + 2], 255));
                    SavePng(stem + "_new_paperNS" + tag + ".png", W, H, i => new Color32(bNew[i * 3], bNew[i * 3 + 1], bNew[i * 3 + 2], 255));
                }
            }
            var pa = Pool(caseA); var pb = Pool(caseB); var pp = Pool(casePost); var ppb = Pool(casePostB);
            allA.AddRange(caseA); allPost.AddRange(casePost);
            var ok = pa.mean.All(m => m <= 1.0) && pp.mean.All(m => m <= 1.0);
            pass &= ok;
            json.Append("      \"pooled\": {\"T1a_lit_hdr\": " + pa.Json() + ", \"T1b_lit_hdr\": " + pb.Json() + ", \"T1a_film_post\": " + pp.Json() + ", \"T1b_film_post\": " + ppb.Json() + ", \"T1a_pass\": " + B(ok) + "}\n");
            json.Append("    },\n");
            report.Add($"{key} {name}: T1a lit mean {F(pa.mean)} p99 {F(pa.p99)} max {F(pa.max)} signed {F(pa.signed)} | film mean {F(pp.mean)} p99 {F(pp.p99)} max {F(pp.max)} | T1b lit mean {F(pb.mean)} p99 {F(pb.p99)} | T1b film mean {F(ppb.mean)} -> {(ok ? "PASS" : "FAIL")}");
            SetWalls(production);
        }
        var all = Pool(allA); var allP = Pool(allPost);
        json.Append("    \"textures\": \"" + formats + "\",\n");
        json.Append("    \"T1a_all\": {\"lit_hdr\": " + all.Json() + ", \"film_post\": " + allP.Json() + ", \"pass\": " + B(pass) + "}\n  },\n");
        report.Add($"{key} all cases/views ({formats}): lit mean {F(all.mean)} p99 {F(all.p99)} max {F(all.max)}; film mean {F(allP.mean)} p99 {F(allP.p99)} max {F(allP.max)}");
        return pass;
    }

    static string F(double[] a) => "[" + string.Join(", ", a.Select(v => v.ToString("0.###", Inv))) + "]";

    // ---------------------------------------------------------- invariance
    static bool RunInvariance(Transform root, StringBuilder json, List<string> report)
    {
        var room = BuildRoom(root, "Invariance room", RoomOrigin + new Vector3(0f, 0f, -20f), 6f, 9f, RoomRule.Lobby);
        var wallMat = FrontRoomsSurfaces.Room(RoomRule.Lobby, FrontRoomsSurfaces.Slot.Wall);
        var office = FrontRoomsSurfaces.Get("Office_Wall");
        var all = room.GetComponentsInChildren<Renderer>();
        foreach (var r in all) if (r.sharedMaterial == wallMat) r.sharedMaterial = office;
        var current = all.Select(r => r.sharedMaterial).ToArray();
        var baseline = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Editor/Rendering/PrintP0/FrontRoomsSurface_P0Baseline.shader");
        var map = new Dictionary<Material, Material>();
        var names = new SortedSet<string>();
        foreach (var m in current.Distinct())
        {
            var b = Clone(m, m.name + " (P0 baseline shader)");
            if (m.shader.name == "FrontRooms/Surface") { b.shader = baseline; names.Add(m.name); }
            map[m] = b;
        }
        var poses = new[] { (new Vector3(.4f, 1.55f, -13.0f), new Vector3(2f, 0f, 0f)), (new Vector3(1.8f, 1.6f, -19.4f), new Vector3(8f, -20f, 0f)) };
        long diffPx = 0; double maxAbs = 0; long totalPx = 0; long diffPost = 0;
        foreach (var (p, e) in poses)
        {
            Pose(p, e, 76f);
            for (var i = 0; i < all.Length; i++) all[i].sharedMaterial = current[i];
            var a = RenderFloat(false); var ab = RenderBytes();
            for (var i = 0; i < all.Length; i++) all[i].sharedMaterial = map[current[i]];
            var b = RenderFloat(false); var bb = RenderBytes();
            for (var i = 0; i < a.px.Length; i += 4)
            {
                var d = 0.0;
                for (var c = 0; c < 3; c++) d = Math.Max(d, Math.Abs(a.px[i + c] - b.px[i + c]));
                if (d > 0) diffPx++;
                maxAbs = Math.Max(maxAbs, d);
            }
            for (var i = 0; i < ab.Length; i++) if (ab[i] != bb[i]) diffPost++;
            totalPx += a.w * a.h;
        }
        for (var i = 0; i < all.Length; i++) all[i].sharedMaterial = current[i];
        var pass = diffPx == 0 && diffPost == 0 && baseline != null;
        json.Append("  \"invariance\": {\"materials\": [" + string.Join(", ", names.Select(n => "\"" + n + "\"")) + "], \"pixels\": " + totalPx
            + ", \"differing_pixels_hdr\": " + diffPx + ", \"max_abs_hdr\": " + maxAbs.ToString("R", Inv) + ", \"differing_bytes_film_post\": " + diffPost + ", \"pass\": " + B(pass) + "},\n");
        report.Add($"Invariance (non-wallpaper: {string.Join(", ", names)}): differing px HDR {diffPx}, max |d| {maxAbs}, differing bytes post {diffPost} -> {(pass ? "PASS" : "FAIL")}");
        room.gameObject.SetActive(false);
        return pass;
    }

    // ------------------------------------------------------- test wall rig
    static GameObject testWall;
    static Light pointLight;
    const float TestWallWidth = 6f;   // wider than every raking and T2 view, so no tile shows background

    static void EnsureTestWall(Transform root, Material m)
    {
        if (testWall == null)
        {
            testWall = Box(root, "P0 test wall", WallCentre, new Vector3(TestWallWidth, 2.9f, .16f), m);
            var lg = new GameObject("P0 point light");
            lg.transform.SetParent(root, false);
            pointLight = lg.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.range = 8f;
            pointLight.color = new Color(1f, .96f, .88f);
            pointLight.shadows = LightShadows.None;
        }
        testWall.GetComponent<Renderer>().sharedMaterial = m;
    }

    static Vector3 FaceCentre => WallCentre + new Vector3(0f, 0f, -.08f);

    /// <summary>Light at angle theta (deg) from the wall plane, coming from direction `along` in the plane.</summary>
    static void PlaceLight(float theta, Vector3 along, float distance, float intensity)
    {
        var t = theta * Mathf.Deg2Rad;
        pointLight.transform.position = FaceCentre + (along.normalized * Mathf.Cos(t) + Vector3.back * Mathf.Sin(t)) * distance;
        pointLight.intensity = intensity;
    }

    static void RunRaking(Transform root, List<string> report)
    {
        var production = FrontRoomsSurfaces.Get("L0_Wallpaper");
        var legacy = Legacy(production, "Wallpaper_Chevron_A", "Wallpaper_Chevron", "Lobby today (legacy)");
        EnsureTestWall(root, production);
        var angles = new[] { 5f, 15f, 35f };
        var dists = new[] { (.7f, 50f, 1.6f, "close 0.7 m"), (2.4f, 50f, 3.2f, "mid 2.4 m") };
        int tw = rtRake.width, th = rtRake.height;
        var sheet = new Color32[tw * 2 * th * 6];
        var row = 0;
        foreach (var (dist, fov, lightDist, dname) in dists)
            foreach (var a in angles)
            {
                PlaceLight(a, new Vector3(-.6f, .8f, 0f), lightDist, 1.2f * lightDist * lightDist);
                Pose(FaceCentre + Vector3.back * dist, Vector3.zero, fov);
                testWall.GetComponent<Renderer>().sharedMaterial = legacy;
                var iL = RenderFloat(false, rtRake);
                testWall.GetComponent<Renderer>().sharedMaterial = production;
                var iN = RenderFloat(false, rtRake);
                var exp = .6f / Mathf.Max(1e-5f, Percentile(iL, .75f));   // same exposure for both columns
                for (var col = 0; col < 2; col++)
                {
                    var img = col == 0 ? iL : iN;
                    for (var y = 0; y < th; y++)
                        for (var x = 0; x < tw; x++)
                        {
                            // rendered at the tile size (no resampling); sheet rows top-down -> Unity bottom-up
                            var si = (y * tw + x) * 4;
                            var dy = (5 - row) * th + y; var dx = col * tw + x;
                            sheet[dy * tw * 2 + dx] = new Color32((byte)(255 * Srgb(img.px[si] * exp)), (byte)(255 * Srgb(img.px[si + 1] * exp)), (byte)(255 * Srgb(img.px[si + 2] * exp)), 255);
                        }
                }
                row++;
            }
        var tex = new Texture2D(tw * 2, th * 6, TextureFormat.RGB24, false);
        tex.SetPixels32(sheet); tex.Apply(false);
        File.WriteAllBytes(Path.Combine(OutDir, "raking_sheet_legacy_vs_new.png"), tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        report.Add($"Raking sheet: raking_sheet_legacy_vs_new.png ({tw}x{th} tiles rendered directly, 4x MSAA, {TestWallWidth} m wall; left today's legacy, right split + new paper N/S; rows close 0.7 m @5/15/35 deg (light 1.6 m), mid 2.4 m @5/15/35 deg (light 3.2 m); post off, one exposure per row)");
    }

    // ------------------------------------------------------------------- T2
    static Texture2D printB;
    static Texture2DArray arrayB, arrayAB;

    static Color32[] PatternB(int w, int h)
    {
        var c = new Color32[w * h];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                float u = (float)x / w, v = (float)y / h;
                var s = .5f + .5f * Mathf.Sin(2f * Mathf.PI * (5f * u + 3f * v));        // diagonal bands, periodic
                var d = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.25f, .75f, s));
                var cu = (u * 4f) % 1f - .5f; var cv = (v * 6f) % 1f - .5f;               // 4 x 6 cream dots
                var cream = Mathf.Clamp01((.18f - Mathf.Sqrt(cu * cu + cv * cv)) * 40f);
                c[y * w + x] = new Color32((byte)(d * 255), (byte)(cream * 255), 0, 255);
            }
        return c;
    }

    static Color[] ToColors(Color32[] c) { var o = new Color[c.Length]; for (var i = 0; i < c.Length; i++) o[i] = c[i]; return o; }

    /// <summary>Print textures built the way the print tools must build _FR_Print: every slice's
    /// mips from FrontRoomsPrintMips (the rule the importer applies to _PrintTex).</summary>
    static void MakePrints()
    {
        const int w = 1024, h = 1536;
        var b = PatternB(w, h);
        var bCol = ToColors(b);
        printB = Own(new Texture2D(w, h, TextureFormat.RGBA32, true, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 16, hideFlags = HideFlags.DontSave });
        var levels = FrontRoomsPrintMips.Build(bCol, w, h, printB.mipmapCount);
        for (var m = 0; m < levels.Count; m++) printB.SetPixels(levels[m], m);
        printB.Apply(false, false);
        arrayB = Own(new Texture2DArray(w, h, 1, TextureFormat.RGBA32, true, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 16, hideFlags = HideFlags.DontSave });
        FrontRoomsPrintMips.ApplySlice(arrayB, 0, bCol); arrayB.Apply(false, false);
        // Live check array: slice 0 = today's frame 0 (from the PNG), slice 1 = B, both 2048 x 3072.
        var png = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
        png.LoadImage(File.ReadAllBytes(TexDir + "Wallpaper_Print_P.png"));
        var a = ToColors(png.GetPixels32());
        var bw = png.width; var bh = png.height;
        var bBig = new Color[bw * bh];
        for (var y = 0; y < bh; y++) for (var x = 0; x < bw; x++) bBig[y * bw + x] = bCol[(y * h / bh) * w + x * w / bw];
        arrayAB = Own(new Texture2DArray(bw, bh, 2, TextureFormat.RGBA32, true, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 16, hideFlags = HideFlags.DontSave });
        FrontRoomsPrintMips.ApplySlice(arrayAB, 0, a); FrontRoomsPrintMips.ApplySlice(arrayAB, 1, bBig); arrayAB.Apply(false, false);
        UnityEngine.Object.DestroyImmediate(png);
    }

    static bool RunT2(Transform root, StringBuilder json, List<string> report)
    {
        MakePrints();
        var production = FrontRoomsSurfaces.Get("L0_Wallpaper");     // print A: static chevron, new paper N/S
        var bStatic = Clone(production, "print B static");
        bStatic.SetTexture("_PrintTex", printB);
        EnsureTestWall(root, production);
        var wallR = testWall.GetComponent<Renderer>();
        var thetas = new[] { 2f, 5f, 9f, 14f, 20f, 27f, 35f, 44f, 54f, 65f, 77f, 90f };
        // Camera below the light's path, looking up at the wall: the highlight sweeps through.
        Pose(FaceCentre + new Vector3(.25f, -.45f, -1.6f), new Vector3(-15f, -8f, 0f), 60f);
        var pass = true;
        const int tw = 320, th = 180, cols = 7;
        var sheet = new Color32[tw * cols * th * thetas.Length];
        json.Append("  \"T2\": {\"light\": \"point, 1.3 m from the wall centre, sweep in the vertical plane from the ceiling side\", \"positions\": [\n");
        long specDiffStatic = 0, specDiffLive = 0, maskDiff = 0;
        double diffuseMean = 0, fullMean = 0;
        for (var k = 0; k < thetas.Length; k++)
        {
            PlaceLight(thetas[k], Vector3.up, 1.3f, 2.4f);
            Img Rend(Material m, bool live, int mode)
            {
                wallR.sharedMaterial = m;
                if (live) { Shader.SetGlobalTexture(PrintId, arrayB); Shader.SetGlobalVector(ClockId, new Vector4(0f, 1f, 0f, 1f)); }
                else ResetPrintGlobals();
                SetDebug(mode);
                var img = RenderFloat(false, rtFloatSmall);
                SetDebug(0); ResetPrintGlobals();
                return img;
            }
            var sA = Rend(production, false, 1); var sBs = Rend(bStatic, false, 1); var sBl = Rend(production, true, 1);
            var dA = Rend(production, false, 2); var dB = Rend(bStatic, false, 2);
            var fA = Rend(production, false, 0); var fB = Rend(bStatic, false, 0);
            var m3A = Rend(production, false, 3); var m3B = Rend(bStatic, false, 3); var m3L = Rend(production, true, 3);
            var m4A = Rend(production, false, 4); var m4B = Rend(bStatic, false, 4);
            long CountDiff(Img x, Img y) { long n = 0; for (var i = 0; i < x.px.Length; i++) if (BitConverter.SingleToInt32Bits(x.px[i]) != BitConverter.SingleToInt32Bits(y.px[i])) n++; return n; }
            double MaxAbs(Img x, Img y) { double m = 0; for (var i = 0; i < x.px.Length; i++) m = Math.Max(m, Math.Abs(x.px[i] - y.px[i])); return m; }
            double MeanAbs(Img x, Img y) { double s = 0; for (var i = 0; i < x.px.Length; i += 4) s += Math.Abs(x.px[i] - y.px[i]) + Math.Abs(x.px[i + 1] - y.px[i + 1]) + Math.Abs(x.px[i + 2] - y.px[i + 2]); return s / (x.px.Length / 4 * 3); }
            double Mean(Img x) { double s = 0; for (var i = 0; i < x.px.Length; i += 4) s += x.px[i] + x.px[i + 1] + x.px[i + 2]; return s / (x.px.Length / 4 * 3); }
            var nS = CountDiff(sA, sBs); var nL = CountDiff(sA, sBl);
            var nM = CountDiff(m3A, m3B) + CountDiff(m3A, m3L) + CountDiff(m4A, m4B);
            var dd = MeanAbs(dA, dB); var fd = MeanAbs(fA, fB);
            specDiffStatic += nS; specDiffLive += nL; maskDiff += nM; diffuseMean += dd; fullMean += fd;
            var specMean = Mean(sA);
            json.Append("    {\"theta_deg\": " + thetas[k].ToString(Inv) + ", \"spec_mean\": " + specMean.ToString("0.######", Inv)
                + ", \"spec_A_vs_Bstatic_diff_values\": " + nS + ", \"spec_A_vs_Bstatic_maxabs\": " + MaxAbs(sA, sBs).ToString("R", Inv)
                + ", \"spec_A_vs_Blive_diff_values\": " + nL + ", \"spec_A_vs_Blive_maxabs\": " + MaxAbs(sA, sBl).ToString("R", Inv)
                + ", \"wear_masks_diff_values\": " + nM
                + ", \"diffuse_meanabs_A_vs_B\": " + dd.ToString("0.######", Inv) + ", \"full_meanabs_A_vs_B\": " + fd.ToString("0.######", Inv) + "}" + (k < thetas.Length - 1 ? ",\n" : "\n"));
            // Contact sheet row: spec A | spec B | |spec A - spec B| x 1000 | diffuse A | diffuse B | full A | full B
            var tiles = new[] { sA, sBs, null, dA, dB, fA, fB };
            var specExp = .9f / Mathf.Max(1e-5f, Peak(sA));
            var litExp = .9f / Mathf.Max(1e-5f, Peak(fA));
            for (var c = 0; c < cols; c++)
                for (var y = 0; y < th; y++)
                    for (var x = 0; x < tw; x++)
                    {
                        var sx = x * 1280 / tw; var sy = y * 720 / th; var si = (sy * 1280 + sx) * 4;
                        Color32 px;
                        if (c == 2)
                        {
                            var d = Mathf.Abs(sA.px[si] - sBs.px[si]) + Mathf.Abs(sA.px[si + 1] - sBs.px[si + 1]) + Mathf.Abs(sA.px[si + 2] - sBs.px[si + 2]);
                            var v = (byte)Mathf.Clamp(d * 1000f * 255f, 0, 255);
                            px = new Color32(v, 0, 0, 255);
                        }
                        else
                        {
                            var img = tiles[c]; var e = c < 2 ? specExp : litExp;
                            px = new Color32((byte)(255 * Srgb(img.px[si] * e)), (byte)(255 * Srgb(img.px[si + 1] * e)), (byte)(255 * Srgb(img.px[si + 2] * e)), 255);
                        }
                        var dy = (thetas.Length - 1 - k) * th + y; var dx = c * tw + x;
                        sheet[dy * tw * cols + dx] = px;
                    }
        }
        pass = specDiffStatic == 0 && specDiffLive == 0 && maskDiff == 0 && diffuseMean > 0;
        json.Append("  ], \"spec_diff_values_static_total\": " + specDiffStatic + ", \"spec_diff_values_live_total\": " + specDiffLive + ", \"wear_mask_diff_values_total\": " + maskDiff
            + ", \"diffuse_meanabs_avg\": " + (diffuseMean / thetas.Length).ToString("0.######", Inv) + ", \"full_meanabs_avg\": " + (fullMean / thetas.Length).ToString("0.######", Inv)
            + ", \"pass\": " + B(pass) + "},\n");
        var tex = new Texture2D(tw * cols, th * thetas.Length, TextureFormat.RGB24, false);
        tex.SetPixels32(sheet); tex.Apply(false);
        File.WriteAllBytes(Path.Combine(OutDir, "T2_light_sweep_sheet.png"), tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        report.Add($"T2: spec-only A vs B(static) differing values {specDiffStatic}, A vs B(live) {specDiffLive}, wear masks {maskDiff}; diffuse mean|d| {diffuseMean / thetas.Length:0.####}, full mean|d| {fullMean / thetas.Length:0.####} -> {(pass ? "PASS" : "FAIL")}");
        return pass;
    }

    static float Percentile(Img img, float q)
    {
        var v = new List<float>(img.w * img.h);
        for (var i = 0; i < img.px.Length; i += 4) { var m = Mathf.Max(img.px[i], Mathf.Max(img.px[i + 1], img.px[i + 2])); if (m > 0) v.Add(m); }
        if (v.Count == 0) return 1f;
        v.Sort();
        return v[(int)(v.Count * q)];
    }

    static float Peak(Img img)
    {
        var v = new List<float>(img.w * img.h);
        for (var i = 0; i < img.px.Length; i += 4) v.Add(Mathf.Max(img.px[i], Mathf.Max(img.px[i + 1], img.px[i + 2])));
        v.Sort();
        return v[(int)(v.Count * .995f)];
    }

    // ---------------------------------------------------- live path sanity
    static void RunLiveCheck(Transform root, StringBuilder json, List<string> report, UniversalRenderPipelineAsset urp)
    {
        // The live array path vs the static frame: slice 0 = the same frame 0 (RGBA32 in the
        // array, R8G8 in _PrintTex; both with FrontRoomsPrintMips mips), clock (0, 2, 0, 1).
        // Repeated at render scale 0.5, where URP sets _GlobalMipBias = (-1, 0.5): both print
        // fetches take gradients, URP scales the static one's (SAMPLE_TEXTURE2D_GRAD) and the
        // shader the array's by the same FR_PRINT_GRAD_SCALE, so they must still match. Then a
        // per-roll phase demo, clock (0, 2, 1, 1): each roll strip & 3 picks frame hash(strip) in [0, 2).
        var production = FrontRoomsSurfaces.Get("L0_Wallpaper");
        EnsureTestWall(root, production);
        RenderSettings.ambientSkyColor = FrontRoomsLook.AmbientSky; RenderSettings.ambientEquatorColor = FrontRoomsLook.AmbientEquator; RenderSettings.ambientGroundColor = FrontRoomsLook.AmbientGround;
        DynamicGI.UpdateEnvironment();
        PlaceLight(60f, Vector3.up, 1.6f, 3f);
        // Far enough (6.5 m, 60 deg) that the print samples mip 1-2, where a missed bias shows.
        var poses = new[] { ("near", FaceCentre + Vector3.back * 2.6f, Vector3.zero), ("far", FaceCentre + new Vector3(0f, .2f, -6.5f), new Vector3(2f, 0f, 0f)) };
        json.Append("  \"live_path\": {");
        Img stat = null, live = null, phase = null, half = null;
        foreach (var (pname, pos, euler) in poses)
            foreach (var scale in new[] { 1f, .5f })
            {
                if (urp != null) urp.renderScale = scale;
                Pose(pos, euler, 60f);
                ResetPrintGlobals(); SetDebug(5);
                var s = RenderFloat(false, rtFloatSmall);
                Shader.SetGlobalTexture(PrintId, arrayAB);
                Shader.SetGlobalVector(ClockId, new Vector4(0f, 2f, 0f, 1f));
                var l = RenderFloat(false, rtFloatSmall);
                if (pname == "near" && scale == 1f)
                {
                    stat = s; live = l;
                    Shader.SetGlobalVector(ClockId, new Vector4(0f, 2f, 1f, 1f));
                    phase = RenderFloat(false, rtFloatSmall);
                    Shader.SetGlobalVector(ClockId, new Vector4(.5f, 2f, 0f, 1f));
                    half = RenderFloat(false, rtFloatSmall);
                }
                var mipBias = Shader.GetGlobalVector("_GlobalMipBias");   // URP sets it per camera; read back after the render
                SetDebug(0); ResetPrintGlobals();
                var st = Diff(Display(s, 1f), Display(l, 1f), null);
                var key = "albedo_static_vs_live_frame0_" + pname + "_scale" + scale.ToString("0.0", Inv);
                json.Append("\"" + key + "\": " + st.Json() + ", \"" + key + "_GlobalMipBias\": [" + mipBias.x.ToString("0.###", Inv) + ", " + mipBias.y.ToString("0.###", Inv) + "], ");
                report.Add($"Live path {pname}, render scale {scale:0.0} (_GlobalMipBias {mipBias.x:0.##}, {mipBias.y:0.##}): albedo static (_PrintTex) vs live (_FR_Print slice 0 = same PNG), clock (0,2,0,1): mean {F(st.mean)} p99 {F(st.p99)} max {F(st.max)} (8-bit sRGB units)");
            }
        if (urp != null) urp.renderScale = 1f;
        Shader.SetGlobalTexture(PrintId, null);
        json.Append("\"note\": \"8-bit sRGB units\"},\n");
        foreach (var (img, name) in new[] { (stat, "static"), (live, "live_frame0"), (phase, "live_per_roll_phase"), (half, "live_blend_0.5") })
            SavePng(Path.Combine(OutDir, "live_" + name + ".png"), img.w, img.h, i => new Color32((byte)(255 * Srgb(img.px[i * 4])), (byte)(255 * Srgb(img.px[i * 4 + 1])), (byte)(255 * Srgb(img.px[i * 4 + 2])), 255));
    }

    // --------------------------------------------------------- print mips
    /// <summary>Reads every mip of the imported _PrintTex back from the GPU and compares it with
    /// FrontRoomsPrintMips built from the PNG, and with plain box mips (what the importer would
    /// make without the rule), in 8-bit levels of R (density) and G (cream).</summary>
    static bool CheckPrintMips(StringBuilder json, List<string> report)
    {
        var tex = LoadTex("Wallpaper_Print_P");
        var png = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
        png.LoadImage(File.ReadAllBytes(TexDir + "Wallpaper_Print_P.png"));
        var mip0 = ToColors(png.GetPixels32());
        int w0 = png.width, h0 = png.height;
        UnityEngine.Object.DestroyImmediate(png);
        var rule = FrontRoomsPrintMips.Build(mip0, w0, h0, tex.mipmapCount);
        // Plain box chain (density and cream averaged independently), for contrast.
        var box = new List<Color[]> { mip0 };
        for (int m = 1, w = w0, h = h0; m < tex.mipmapCount; m++)
        {
            int tw = Math.Max(1, w >> 1), th = Math.Max(1, h >> 1);
            var prev = box[m - 1]; var next = new Color[tw * th];
            for (var y = 0; y < th; y++)
            {
                int y0 = y * h / th, y1 = Math.Max(y0 + 1, (y + 1) * h / th);
                for (var x = 0; x < tw; x++)
                {
                    int x0 = x * w / tw, x1 = Math.Max(x0 + 1, (x + 1) * w / tw);
                    var sum = Color.clear;
                    for (var yy = y0; yy < y1; yy++) for (var xx = x0; xx < x1; xx++) sum += prev[yy * w + xx];
                    next[y * tw + x] = sum / ((x1 - x0) * (y1 - y0));
                }
            }
            box.Add(next); w = tw; h = th;
        }
        var bpp = (int)GraphicsFormatUtility.GetBlockSize(tex.graphicsFormat);
        var ok = true;
        var rows = new List<string>();
        json.Append("  \"print_mips\": {\"format\": \"" + tex.graphicsFormat + "\", \"levels\": [");
        for (var m = 0; m < tex.mipmapCount; m++)
        {
            var req = AsyncGPUReadback.Request(tex, m);
            req.WaitForCompletion();
            if (req.hasError) { ok = false; json.Append("{\"mip\": " + m + ", \"error\": true}, "); continue; }
            var data = req.GetData<byte>();
            int n = rule[m].Length;
            double maxRule = 0, meanBoxD = 0, maxBoxD = 0;
            for (var i = 0; i < n; i++)
            {
                double r = data[i * bpp], g = data[i * bpp + 1];
                maxRule = Math.Max(maxRule, Math.Max(Math.Abs(r - rule[m][i].r * 255.0), Math.Abs(g - rule[m][i].g * 255.0)));
                var bd = Math.Abs(rule[m][i].r - box[m][i].r) * 255.0;
                meanBoxD += bd; maxBoxD = Math.Max(maxBoxD, bd);
            }
            meanBoxD /= n;
            ok &= maxRule <= 1.0;   // 8-bit rounding
            json.Append("{\"mip\": " + m + ", \"texels\": " + n + ", \"max_abs_vs_rule\": " + maxRule.ToString("0.###", Inv)
                + ", \"density_rule_minus_box_mean\": " + meanBoxD.ToString("0.###", Inv) + ", \"density_rule_minus_box_max\": " + maxBoxD.ToString("0.###", Inv) + "}" + (m < tex.mipmapCount - 1 ? ", " : ""));
            rows.Add($"mip{m} {maxRule:0.##}/{meanBoxD:0.##}");
        }
        json.Append("], \"pass\": " + B(ok) + "},\n");
        report.Add($"Print mips (imported _PrintTex read back from the GPU, {tex.graphicsFormat}): max |GPU - FrontRoomsPrintMips| / mean |rule - plain box| density, 8-bit levels: " + string.Join(", ", rows) + $" -> {(ok ? "the importer's mips are the rule" : "MISMATCH")}");
        return ok;
    }

    // ------------------------------------------------------------- memory
    static long Bytes(Texture t)
    {
        if (t == null) return 0;
        long sum = 0;
        int w = t.width, h = t.height;
        for (var m = 0; m < t.mipmapCount; m++) { sum += (long)GraphicsFormatUtility.ComputeMipmapSize(Math.Max(1, w >> m), Math.Max(1, h >> m), t.graphicsFormat); }
        return sum;
    }

    static void WriteMemory(StringBuilder json, List<string> report)
    {
        var sets = new (string label, string dir, string[] stems)[]
        {
            ("legacy_materials (now in RefDir)", RefDir, new[] { "Wallpaper_Chevron_A", "Wallpaper_Chevron_N", "Wallpaper_Chevron_S", "Wallpaper_Chevron_Cold_A", "Wallpaper_Chevron_Cold_N", "Wallpaper_Chevron_Cold_S" }),
            ("split_materials (Resources)", TexDir, new[] { "Wallpaper_Paper_M", "Wallpaper_Paper_N", "Wallpaper_Paper_S", "Wallpaper_Print_P" }),
            ("t1_test_refs (RefDir)", RefDir, new[] { "Wallpaper_Chevron_NoHue_A", "Wallpaper_Chevron_Cold_NoHue_A" }),
        };
        var totals = new Dictionary<string, long>();
        json.Append("  \"texture_memory\": {");
        foreach (var (label, dir, stems) in sets)
        {
            long total = 0;
            json.Append("\"" + label + "\": {");
            foreach (var s in stems)
            {
                var t = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + s + ".png");
                var b = Bytes(t);
                total += b;
                totals[s] = b;
                json.Append("\"" + s + "\": {\"bytes\": " + b + ", \"format\": \"" + (t != null ? t.graphicsFormat.ToString() : "missing") + "\", \"size\": \"" + (t != null ? t.width + "x" + t.height : "") + "\"}, ");
            }
            json.Append("\"total_bytes\": " + total + "}, ");
        }
        long Sum(params string[] s) => s.Sum(x => totals.TryGetValue(x, out var v) ? v : 0);
        var lobbyLegacy = Sum("Wallpaper_Chevron_A", "Wallpaper_Chevron_N", "Wallpaper_Chevron_S");
        var allLegacy = lobbyLegacy + Sum("Wallpaper_Chevron_Cold_A", "Wallpaper_Chevron_Cold_N", "Wallpaper_Chevron_Cold_S");
        var split = Sum("Wallpaper_Paper_M", "Wallpaper_Paper_N", "Wallpaper_Paper_S", "Wallpaper_Print_P");
        json.Append("\"lobby_only_delta_bytes\": " + (split - lobbyLegacy) + ", \"all_three_delta_bytes\": " + (split - allLegacy)
            + ", \"budget_bytes (synthesis §7)\": " + (16L << 20) + ", \"note\": \"runtime: the three wallpaper materials load the split set instead of the legacy set; only Resources ships\"},\n");
        report.Add($"Texture memory: split set {split / 1048576.0:0.0} MB (print {totals["Wallpaper_Print_P"] / 1048576.0:0.0} MB); Lobby-only delta {(split - lobbyLegacy) / 1048576.0:+0.0;-0.0} MB, all three wallpapers {(split - allLegacy) / 1048576.0:+0.0;-0.0} MB (budget +16 MB)");
        var probe = LoadTex("Wallpaper_Print_P");
        var imp = (TextureImporter)AssetImporter.GetAtPath(TexDir + "Wallpaper_Print_P.png");
        var st = imp.GetPlatformTextureSettings("Standalone");
        json.Append("  \"compression\": {\"importer\": \"" + imp.textureCompression + "\", \"print_standalone_override\": \"" + (st.overridden ? st.format.ToString() : "none")
            + "\", \"imported_format_print\": \"" + probe.graphicsFormat + "\", \"imported_format_paper\": \"" + LoadTex("Wallpaper_Paper_M").graphicsFormat
            + "\", \"reason\": \"2048x3072 is NPOT with mips; Unity refuses BC7 for NPOT mipped textures and falls back to RGBA32 (legacy wallpaper textures too); the print is R8G8 by override\"},\n");
    }

    // ---------------------------------------------------------- shader cost
    static void WriteShaderStats(StringBuilder json)
    {
        var shader = Shader.Find("FrontRooms/Surface");
        var data = ShaderUtil.GetShaderData(shader);
        var sub = data.GetSubshader(0);
        ShaderData.Pass fwd = null;
        for (var i = 0; i < sub.PassCount; i++) if (sub.GetPass(i).Name == "ForwardLit") fwd = sub.GetPass(i);
        string[] gameKeys = { "_MAIN_LIGHT_SHADOWS_CASCADE", "_ADDITIONAL_LIGHTS", "_ADDITIONAL_LIGHT_SHADOWS", "_SHADOWS_SOFT_HIGH", "_SCREEN_SPACE_OCCLUSION", "_CLUSTER_LIGHT_LOOP" };
        var codeDir = Path.Combine(OutDir, "shader_code");
        Directory.CreateDirectory(codeDir);
        // SRP Batcher compatibility (internal editor API; "unknown" if it moved).
        var srp = "unknown";
        try
        {
            var mi = typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (mi != null) srp = Convert.ToString(mi.Invoke(null, new object[] { shader, 0 }), Inv) + " (0 = compatible)";
        }
        catch (Exception e) { srp = "unknown: " + e.GetType().Name; }
        json.Append("  \"srp_batcher_code\": \"" + srp + "\",\n");
        json.Append("  \"shader_cost\": {");
        foreach (var (platform, target, label, sampleTokens) in new[]
        {
            (ShaderCompilerPlatform.Metal, BuildTarget.StandaloneOSX, "metal", new[] { ".sample(", ".sample_compare(" }),
            (ShaderCompilerPlatform.GLES3x, BuildTarget.WebGL, "gles3", new[] { "texture(", "textureGrad(", "textureLod(" }),
        })
        {
            json.Append("\"" + label + "\": {");
            foreach (var (vname, keys) in new[] { ("base", gameKeys), ("print", gameKeys.Concat(new[] { "_FR_PRINT" }).ToArray()) })
            {
                try
                {
                    var info = fwd.CompileVariant(ShaderType.Fragment, keys, platform, target, true);
                    var text = info.ShaderData != null ? Encoding.UTF8.GetString(info.ShaderData) : "";
                    File.WriteAllText(Path.Combine(codeDir, label + "_" + vname + ".txt"), text);
                    var fetch = sampleTokens.Sum(tok => Count(text, tok));
                    var lines = text.Split('\n').Count(l => l.Contains("=") && !l.TrimStart().StartsWith("//"));
                    json.Append("\"" + vname + "\": {\"success\": " + B(info.Success) + ", \"sample_calls\": " + fetch + ", \"assignment_lines\": " + lines + ", \"bytes\": " + text.Length + "}, ");
                }
                catch (Exception e)
                {
                    json.Append("\"" + vname + "\": {\"error\": \"" + e.GetType().Name + ": " + e.Message.Replace("\"", "'").Replace("\n", " ") + "\"}, ");
                }
            }
            json.Append("\"keywords\": \"" + string.Join(" ", gameKeys) + "\"}, ");
        }
        json.Append("\"note\": \"text of each variant in Verification/print_p0/shader_code\"},\n");
    }

    static int Count(string s, string tok)
    {
        int n = 0, i = 0;
        while ((i = s.IndexOf(tok, i, StringComparison.Ordinal)) >= 0) { n++; i += tok.Length; }
        return n;
    }
}

/// <summary>
/// Imports the P0 test references (RefDir, outside Resources so builds never ship them)
/// with the surface texture rules, so regenerated references match production imports.
/// </summary>
public sealed class FrontRoomsPrintP0RefImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Replace('\\', '/').StartsWith(FrontRoomsPrintP0Test.RefDir)) return;
        FrontRoomsSurfaceTextureImporter.Configure((TextureImporter)assetImporter, Path.GetFileNameWithoutExtension(assetPath));
    }
}
