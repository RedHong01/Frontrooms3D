using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Q1b gates for the Hard edge print (FrontRoomsPrintArray): the 2048^2 _PrintTex and the
/// 8-slice _FR_Print array, in the production format (FrontRoomsPrintArray.DesktopFormat)
/// against an exact uncompressed R8G8 reference built here from the encoded slices with
/// FrontRoomsPrintMips.
///   Texels   every mip of every slice read back from the GPU: production vs the R8G8
///            reference (the compression error), static _PrintTex vs slice 0 (must be equal),
///            slice order, wrap seams; plus the reduced (WebGL) import path.
///   Gate     renders of the P0 room views and the 25 m corridor (fog on/off), Lobby/Shift/Exit
///            palettes, static and live (two clocks): mean |delta| <= 1 level and p99 <= 4 per
///            channel in both the lit-HDR and the film-post metric; max reported.
///   Crossfade  static vs live at w = 0.25/0.5/1 (albedo and lit), render scale 1 and 0.5,
///            near, far and close (3 poses), for both formats; 72 runs: mean 0, max <= 0.2 levels.
///   T2       specular-only bit-identity between prints (static R8G8, static BC5, live
///            slices, the CC0 frame), 12 light positions; wear masks identical.
///   Seams    the roll tile's edges on a wall: gradient at the seam lines vs everywhere else.
/// Captures for Red (before = the CC0 chevron P0 shipped, after = Hard edge) are in
/// RunQ1bCaptures (rig) and RunQ1bRoomCaptures (the game scene's edit-mode profile rooms).
/// Writes Verification/print_q1b. Batch: -executeMethod FrontRoomsPrintP0Test.RunQ1bBatch.
/// </summary>
public static partial class FrontRoomsPrintP0Test
{
    const string FetchShaderPath = "Assets/Editor/Rendering/PrintP0/FrontRoomsPrintFetch.shader";
    static Material fetchMat;
    static Texture2DArray dummyArray;
    static Texture2D refTex;          // exact R8G8 reference of _PrintTex (K00), test-only
    static Texture2DArray refArray;   // exact R8G8 reference of _FR_Print (K00..K07), test-only
    static List<List<byte[]>> refLevels;   // the bytes uploaded to refArray: [slice][mip], R/G interleaved, rows bottom-up

    [MenuItem("FrontRooms/Rendering/Print Q1b gates (BC5, static vs live, T2, seams)")]
    public static void RunQ1b() => RunQ1bInternal();

    [MenuItem("FrontRooms/Rendering/Print Q1b captures (before vs after, slice strip)")]
    public static void RunQ1bCapturesMenu() => RunQ1bCapturesInternal();

    /// <summary>Batch: rebuilds the print (Build Print Array), then runs the gates.</summary>
    public static void RunQ1bBatch()
    {
        Debug.Log("[PrintQ1b] " + FrontRoomsPrintArray.Build());
        RunQ1bInternal();
    }

    public static void RunQ1bCapturesBatch() => RunQ1bCapturesInternal();

    /// <summary>Batch: the reduced-import check alone, the rig captures, P0's gates (regression, with
    /// the CC0 frame for T1 and the shipped array for the live check), then the game-scene captures.</summary>
    public static void RunQ1bFollowupBatch()
    {
        void Step(string name, Action a)
        {
            try { a(); Debug.Log("[PrintQ1b followup] " + name + " done"); }
            catch (Exception e) { Debug.LogError("[PrintQ1b followup] " + name + " FAILED: " + e); }
        }
        Step("build (reimports both prints with today's import rules)", () => Debug.Log("[PrintArray] " + FrontRoomsPrintArray.Build()));
        Step("reduced import", () => WithRig("print_q1b_reduced", (root, json, report, urp) =>
        {
            json.Append("{\n");
            var ok = RunReducedImportCheck(json, report);
            json.Append("  \"pass\": " + B(ok) + "\n}\n");
            File.WriteAllText(Path.Combine(OutDir, "print_q1b_reduced.json"), json.ToString());
            File.WriteAllText(Path.Combine(OutDir, "print_q1b_reduced_summary.txt"), string.Join("\n", report) + "\n");
            Debug.Log("[PrintQ1b reduced] " + (ok ? "PASS" : "FAIL") + "\n" + string.Join("\n", report));
        }));
        Step("rig captures", RunQ1bCapturesInternal);
        Step("P0 gates", RunInternal);
        Step("room captures", RunQ1bRoomCapturesBatch);
    }

    /// <summary>Batch: the final verification pass: rebuild the print, the Q1b gates (with the
    /// fetch self-test and the reduced-import check), then P0's gates as a regression (their
    /// print-mip check reads the BC5 _PrintTex through the same fetch).</summary>
    public static void RunQ1bFinalBatch()
    {
        void Step(string name, Action a)
        {
            try { a(); Debug.Log("[PrintQ1b final] " + name + " done"); }
            catch (Exception e) { Debug.LogError("[PrintQ1b final] " + name + " FAILED: " + e); }
        }
        Step("build", () => Debug.Log("[PrintArray] " + FrontRoomsPrintArray.Build()));
        Step("Q1b gates", RunQ1bInternal);
        Step("P0 gates", RunInternal);
    }

    static Texture2D ProdTex => LoadTex("Wallpaper_Print_P");
    static Texture2DArray ProdArray => Resources.Load<Texture2DArray>(FrontRoomsPrintArray.ResourcesName);

    // ------------------------------------------------------------- GPU fetch
    /// <summary>One mip of a print texture (or array slice), texel for texel, as R/G in 8-bit
    /// levels (float, unquantised: BC5 decodes between levels). Rows bottom-up.</summary>
    static float[] FetchRG(Texture tex, int slice, int mip, out int w, out int h)
    {
        if (fetchMat == null)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(FetchShaderPath);
            if (shader == null) throw new InvalidOperationException("Missing " + FetchShaderPath);
            fetchMat = Own(new Material(shader) { hideFlags = HideFlags.DontSave });
            dummyArray = Own(new Texture2DArray(1, 1, 1, TextureFormat.RGBA32, false, true) { hideFlags = HideFlags.DontSave });
        }
        w = Math.Max(1, tex.width >> mip); h = Math.Max(1, tex.height >> mip);
        var isArray = tex is Texture2DArray;
        fetchMat.SetTexture("_FetchTex", isArray ? (Texture)Texture2D.blackTexture : tex);
        fetchMat.SetTexture("_FetchArr", isArray ? tex : dummyArray);
        fetchMat.SetFloat("_FetchSlice", slice);
        fetchMat.SetFloat("_FetchMip", mip);
        fetchMat.SetFloat("_FetchUseArray", isArray ? 1f : 0f);
        fetchMat.SetVector("_FetchSize", new Vector4(w, h, 0f, 0f));
        var rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear) { filterMode = FilterMode.Point };
        var activeWas = RenderTexture.active;
        try
        {
            Graphics.Blit(null, rt, fetchMat);
            var img = ReadFloat(rt);
            var o = new float[w * h * 2];
            for (var i = 0; i < w * h; i++) { o[i * 2] = img.px[i * 4] * 255f; o[i * 2 + 1] = img.px[i * 4 + 1] * 255f; }
            return o;
        }
        finally { RenderTexture.active = activeWas == rt ? null : activeWas; rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
    }

    /// <summary>The fetch's own check: the exact R8G8 reference read back through FetchRG must equal
    /// the bytes uploaded to it, at every level of every slice (a read that returned the wrong mip,
    /// e.g. mip 0 point-sampled, fails here). Also the R8G8 _PrintTex reference at every level.</summary>
    static bool FetchSelfTest(StringBuilder json, List<string> report)
    {
        double worst = 0; var worstAt = "";
        for (var k = 0; k < refArray.depth; k++)
            for (var m = 0; m < refArray.mipmapCount; m++)
            {
                var got = FetchRG(refArray, k, m, out _, out _);
                var want = refLevels[k][m];
                for (var i = 0; i < want.Length; i++)
                {
                    var d = Math.Abs(got[i] - want[i]);
                    if (d > worst) { worst = d; worstAt = "slice " + k + " mip " + m; }
                }
            }
        for (var m = 0; m < refTex.mipmapCount; m++)
        {
            var got = FetchRG(refTex, 0, m, out _, out _);
            var want = refLevels[0][m];
            for (var i = 0; i < want.Length; i++)
            {
                var d = Math.Abs(got[i] - want[i]);
                if (d > worst) { worst = d; worstAt = "_PrintTex ref mip " + m; }
            }
        }
        var ok = worst <= .01;
        json.Append("  \"fetch_self_test\": {\"max_abs_levels\": " + worst.ToString("0.#####", Inv) + ", \"worst\": \"" + worstAt + "\", \"pass\": " + B(ok) + "},\n");
        report.Add($"Fetch self-test (Load, explicit mip): R8G8 references read back vs the uploaded bytes, {refArray.depth} slices x {refArray.mipmapCount} mips + _PrintTex ref: max |delta| {worst:0.#####} levels{(worst > 0 ? " at " + worstAt : "")} -> {(ok ? "exact" : "BROKEN")}");
        return ok;
    }

    /// <summary>|a - b| per channel (2 channels, R density and G cream): mean, p99, max, in levels.</summary>
    static (double[] mean, double[] p99, double[] max) Diff2(float[] a, float[] b, Func<float, float> roundB = null)
    {
        var mean = new double[2]; var p99 = new double[2]; var max = new double[2];
        var n = a.Length / 2;
        for (var c = 0; c < 2; c++)
        {
            var hist = new long[2049];   // 1/8-level bins up to 256 levels
            double sum = 0, mx = 0;
            for (var i = 0; i < n; i++)
            {
                var bv = roundB != null ? roundB(b[i * 2 + c]) : b[i * 2 + c];
                var d = Math.Abs(a[i * 2 + c] - bv);
                sum += d; if (d > mx) mx = d;
                hist[Math.Min(2048, (int)(d * 8))]++;
            }
            mean[c] = sum / n; max[c] = mx;
            long acc = 0, target = (long)Math.Ceiling(n * .99);
            for (var k = 0; k < hist.Length; k++) { acc += hist[k]; if (acc >= target) { p99[c] = (k + 1) / 8.0; break; } }
        }
        return (mean, p99, max);
    }

    static string J2(double[] a) => "[" + string.Join(", ", a.Select(v => v.ToString("0.####", Inv))) + "]";

    // ------------------------------------------------------- R8G8 reference
    /// <summary>Exact R8G8 levels of a rule chain: the same float -> 8-bit conversion as the
    /// importer's (SetPixels on RGBA32, then R and G kept).</summary>
    static List<byte[]> Rg8Levels(List<Color[]> chain, int w0, int h0)
    {
        var levels = new List<byte[]>();
        var t = new Texture2D(w0, h0, TextureFormat.RGBA32, chain.Count, true) { hideFlags = HideFlags.DontSave };
        try
        {
            for (var m = 0; m < chain.Count; m++) t.SetPixels(chain[m], m);
            t.Apply(false, false);
            for (var m = 0; m < chain.Count; m++)
            {
                var src = t.GetPixelData<byte>(m);
                var o = new byte[src.Length / 2];
                for (var i = 0; i < src.Length / 4; i++) { o[i * 2] = src[i * 4]; o[i * 2 + 1] = src[i * 4 + 1]; }
                levels.Add(o);
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(t); }
        return levels;
    }

    static void BuildReferences(List<string> report)
    {
        var paths = FrontRoomsPrintArray.SlicePaths();
        const int s = FrontRoomsPrintArray.SliceSize;
        var mips = FrontRoomsPrintArray.MipCount(s, s);
        refTex = Own(new Texture2D(s, s, TextureFormat.RG16, mips, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 16, hideFlags = HideFlags.DontSave, name = "Q1b ref _PrintTex R8G8" });
        refArray = Own(new Texture2DArray(s, s, paths.Length, TextureFormat.RG16, mips, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 16, hideFlags = HideFlags.DontSave, name = "Q1b ref _FR_Print R8G8" });
        refLevels = new List<List<byte[]>>();
        for (var k = 0; k < paths.Length; k++)
        {
            var px = FrontRoomsPrintArray.LoadPng(paths[k], out var w, out var h);
            for (var i = 0; i < px.Length; i++) px[i].b = 0;   // _PrintTex and the sheet carry B = 0
            var levels = Rg8Levels(FrontRoomsPrintArray.Chain(px, w, h), w, h);
            refLevels.Add(levels);
            for (var m = 0; m < levels.Count; m++)
            {
                refArray.SetPixelData(levels[m], m, k);
                if (k == 0) refTex.SetPixelData(levels[m], m);
            }
        }
        refTex.Apply(false, false);
        refArray.Apply(false, false);
        report.Add($"References: R8G8 _PrintTex {refTex.width}^2 x {refTex.mipmapCount} mips, R8G8 array x{refArray.depth}, built from {FrontRoomsPrintArray.SourceDir} with FrontRoomsPrintMips");
    }

    // --------------------------------------------------------------- main
    static void RunQ1bInternal() => WithRig("print_q1b", (root, json, report, urp) =>
    {
        var prodTex = ProdTex; var prodArr = ProdArray;
        if (prodTex == null || prodArr == null) throw new InvalidOperationException("Run FrontRooms > Rendering > Build Print Array first (missing _PrintTex or the array).");
        json.Append("{\n  \"production\": {\"printTex\": \"" + prodTex.width + "x" + prodTex.height + " " + prodTex.graphicsFormat + " mips " + prodTex.mipmapCount + " readable " + prodTex.isReadable
            + "\", \"array\": \"" + prodArr.width + "x" + prodArr.height + " x" + prodArr.depth + " " + prodArr.graphicsFormat + " mips " + prodArr.mipmapCount + " readable " + prodArr.isReadable
            + "\", \"activeBuildTarget\": \"" + EditorUserBuildSettings.activeBuildTarget + "\"},\n");
        report.Add($"Production: _PrintTex {prodTex.width}x{prodTex.height} {prodTex.graphicsFormat} mips {prodTex.mipmapCount} readable {prodTex.isReadable}; _FR_Print {prodArr.width}x{prodArr.height} x{prodArr.depth} {prodArr.graphicsFormat} mips {prodArr.mipmapCount} readable {prodArr.isReadable}");
        BuildReferences(report);
        var fetchOk = FetchSelfTest(json, report);
        var texelOk = RunTexelChecks(prodTex, prodArr, json, report) && fetchOk;
        var reducedOk = RunReducedImportCheck(json, report);
        var gate = RunBc5Gate(root, prodTex, prodArr, json, report);
        // The single-light tests: dim ambient, no fog (as P0).
        RenderSettings.fog = false;
        RenderSettings.ambientSkyColor = FrontRoomsLook.AmbientSky * .25f;
        RenderSettings.ambientEquatorColor = FrontRoomsLook.AmbientEquator * .25f;
        RenderSettings.ambientGroundColor = FrontRoomsLook.AmbientGround * .25f;
        DynamicGI.UpdateEnvironment();
        var t2 = RunT2Q1b(root, prodTex, prodArr, json, report);
        var t2p0 = RunT2(root, json, report);   // P0's T2 with today's production print as print A
        var fade = RunCrossfade(root, prodTex, prodArr, urp, json, report);
        var seams = RunSeams(root, prodTex, json, report);
        WriteQ1bMemory(prodTex, prodArr, json, report);
        json.Append("  \"gates\": {\"texels_static_equals_slice0\": " + B(texelOk) + ", \"reduced_import_path\": " + B(reducedOk) + ", \"bc5_render_gate\": " + B(gate)
            + ", \"T2_q1b\": " + B(t2) + ", \"T2_p0\": " + B(t2p0) + ", \"static_vs_live_crossfade\": " + B(fade) + ", \"seams\": " + B(seams)
            + ", \"desktop_format\": \"" + FrontRoomsPrintArray.DesktopFormat + "\"}\n}\n");
        File.WriteAllText(Path.Combine(OutDir, "print_q1b.json"), json.ToString());
        File.WriteAllText(Path.Combine(OutDir, "print_q1b_summary.txt"), string.Join("\n", report) + "\n");
        Debug.Log("[PrintQ1b] texels " + (texelOk ? "PASS" : "FAIL") + ", reduced " + (reducedOk ? "PASS" : "FAIL") + ", BC5 gate " + (gate ? "PASS" : "FAIL")
            + ", T2 " + (t2 && t2p0 ? "PASS" : "FAIL") + ", crossfade " + (fade ? "PASS" : "FAIL") + ", seams " + (seams ? "PASS" : "FAIL") + "\n" + string.Join("\n", report));
        refTex = null; refArray = null; refLevels = null; fetchMat = null; dummyArray = null;
    });

    // ------------------------------------------------------------- texels
    static bool RunTexelChecks(Texture2D prodTex, Texture2DArray prodArr, StringBuilder json, List<string> report)
    {
        var ok = prodTex.width == prodArr.width && prodTex.height == prodArr.height && prodTex.mipmapCount == prodArr.mipmapCount
            && prodTex.graphicsFormat == prodArr.graphicsFormat && prodArr.depth == FrontRoomsPrintArray.SliceCount && !prodArr.isReadable && !prodTex.isReadable;
        json.Append("  \"texels\": {\"layout_match\": " + B(ok) + ", \"static_vs_slice0\": [");
        // 1. Static frame vs slice 0, every mip: must be the same decoded texels.
        double worstStatic = 0;
        for (var m = 0; m < prodTex.mipmapCount; m++)
        {
            var a = FetchRG(prodTex, 0, m, out _, out _);
            var b = FetchRG(prodArr, 0, m, out _, out _);
            var d = Diff2(a, b);
            worstStatic = Math.Max(worstStatic, Math.Max(d.max[0], d.max[1]));
            json.Append("{\"mip\": " + m + ", \"max\": " + J2(d.max) + "}" + (m < prodTex.mipmapCount - 1 ? ", " : ""));
        }
        ok &= worstStatic == 0;
        report.Add($"Texels: static _PrintTex vs _FR_Print slice 0, all {prodTex.mipmapCount} mips: max |delta| {worstStatic:0.####} levels -> {(worstStatic == 0 ? "identical" : "DIFFERENT")}");
        json.Append("], \"static_vs_slice0_max\": " + worstStatic.ToString("0.####", Inv) + ",\n    \"slices\": [\n");
        // 2. Every slice and mip vs the exact R8G8 reference: the compression error, slice order, wrap seams.
        var allMean = new double[2]; var allMax = new double[2]; long allN = 0;
        var orderOk = true;
        for (var k = 0; k < prodArr.depth; k++)
        {
            var mipJson = new List<string>();
            var slice0Json = "";
            for (var m = 0; m < prodArr.mipmapCount; m++)
            {
                var p = FetchRG(prodArr, k, m, out var w, out var h);
                var r = FetchRG(refArray, k, m, out _, out _);
                var d = Diff2(p, r);
                mipJson.Add("{\"mip\": " + m + ", \"mean\": " + J2(d.mean) + ", \"p99\": " + J2(d.p99) + ", \"max\": " + J2(d.max) + "}");
                for (var c = 0; c < 2; c++) { allMean[c] += d.mean[c] * w * h; allMax[c] = Math.Max(allMax[c], d.max[c]); }
                allN += w * h;
                if (m != 0) continue;
                // Slice order: the production slice must be closer to its own reference than to any other.
                var own = d.mean[0] + d.mean[1];
                for (var j = 0; j < prodArr.depth; j++)
                {
                    if (j == k) continue;
                    var dj = Diff2(p, FetchRG(refArray, j, 0, out _, out _));
                    if (dj.mean[0] + dj.mean[1] <= own) orderOk = false;
                }
                var seam = SeamRatios(p, w, h);
                var seamRef = SeamRatios(r, w, h);
                double meanR = 0, meanG = 0;
                for (var i = 0; i < w * h; i++) { meanR += p[i * 2]; meanG += p[i * 2 + 1]; }
                meanR /= w * h * 255.0; meanG /= w * h * 255.0;
                slice0Json = ", \"mip0_mean_rg\": [" + meanR.ToString("0.####", Inv) + ", " + meanG.ToString("0.####", Inv) + "], \"wrap_seam_ratio_xy\": [" + seam.x.ToString("0.###", Inv) + ", " + seam.y.ToString("0.###", Inv)
                    + "], \"wrap_seam_ratio_xy_ref\": [" + seamRef.x.ToString("0.###", Inv) + ", " + seamRef.y.ToString("0.###", Inv) + "]";
                report.Add($"Slice K{k:00} mip0: production vs R8G8 ref mean {J2(d.mean)} p99 {J2(d.p99)} max {J2(d.max)} levels; mean R/G {meanR:0.####}/{meanG:0.####}; wrap seam / interior gradient x {seam.x:0.###} y {seam.y:0.###} (ref {seamRef.x:0.###} {seamRef.y:0.###})");
            }
            json.Append("      {\"slice\": " + k + slice0Json + ", \"mips\": [" + string.Join(", ", mipJson) + "]}" + (k < prodArr.depth - 1 ? ",\n" : "\n"));
        }
        for (var c = 0; c < 2; c++) allMean[c] /= Math.Max(1, allN);
        ok &= orderOk;
        json.Append("    ], \"all_mips_all_slices_vs_ref\": {\"mean\": " + J2(allMean) + ", \"max\": " + J2(allMax) + "}, \"slice_order_ok\": " + B(orderOk) + ", \"pass\": " + B(ok) + "},\n");
        report.Add($"Texels: production vs R8G8 reference over all slices and mips: mean {J2(allMean)} max {J2(allMax)} levels (R density, G cream); slice order {(orderOk ? "ok" : "WRONG")}; layout {(ok ? "ok" : "MISMATCH")}");
        return ok;
    }

    /// <summary>Wrap-seam discontinuity over the mean neighbour gradient, per axis (1 = invisible).</summary>
    static (double x, double y) SeamRatios(float[] rg, int w, int h)
    {
        double interiorX = 0, seamX = 0, interiorY = 0, seamY = 0;
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = (y * w + x) * 2;
                var ix = (y * w + (x + 1) % w) * 2;
                var iy = (((y + 1) % h) * w + x) * 2;
                var dx = Math.Abs(rg[i] - rg[ix]) + Math.Abs(rg[i + 1] - rg[ix + 1]);
                var dy = Math.Abs(rg[i] - rg[iy]) + Math.Abs(rg[i + 1] - rg[iy + 1]);
                if (x == w - 1) seamX += dx; else interiorX += dx;
                if (y == h - 1) seamY += dy; else interiorY += dy;
            }
        interiorX /= (double)(w - 1) * h; seamX /= h;
        interiorY /= (double)(h - 1) * w; seamY /= w;
        return (seamX / Math.Max(1e-9, interiorX), seamY / Math.Max(1e-9, interiorY));
    }

    // ------------------------------------------------- reduced import path
    /// <summary>The WebGL tier imports the print at half size. Checks (1) how the max size acts on a
    /// flipbook array (a temporary copy of the sheet outside the print rules, imported with max 4096
    /// and then 1024: the max applies per slice, so 4096 keeps 2048 slices and 1024 gives 1024), and
    /// (2) that the importers' reduced-size code puts exactly the rule chain's lower levels into every
    /// level, for the array and for _PrintTex.</summary>
    static bool RunReducedImportCheck(StringBuilder json, List<string> report)
    {
        var ok = true;
        const string tmpDir = "Assets/Editor/Rendering/PrintP0/Q1bTmp";
        var tmpSheet = tmpDir + "/Q1bSheetSim.png";
        int simW = 0, simDepth = 0, simW4096 = 0;
        try
        {
            if (!AssetDatabase.IsValidFolder(tmpDir)) AssetDatabase.CreateFolder("Assets/Editor/Rendering/PrintP0", "Q1bTmp");
            File.Copy(FrontRoomsPrintArray.SheetPath, tmpSheet, true);
            AssetDatabase.ImportAsset(tmpSheet, ImportAssetOptions.ForceSynchronousImport);
            var imp = (TextureImporter)AssetImporter.GetAtPath(tmpSheet);
            imp.textureShape = TextureImporterShape.Texture2DArray;
            var st = new TextureImporterSettings(); imp.ReadTextureSettings(st);
            st.flipbookColumns = FrontRoomsPrintArray.Columns; st.flipbookRows = 2; imp.SetTextureSettings(st);
            imp.sRGBTexture = false; imp.mipmapEnabled = true; imp.maxTextureSize = 16384;
            var sa = imp.GetPlatformTextureSettings("Standalone");
            sa.overridden = true; sa.maxTextureSize = FrontRoomsPrintArray.Columns * FrontRoomsPrintArray.WebGLSliceSize; sa.format = FrontRoomsPrintArray.WebGLFormat;
            imp.SetPlatformTextureSettings(sa);
            imp.SaveAndReimport();
            var sim = AssetDatabase.LoadAssetAtPath<Texture2DArray>(tmpSheet);
            simW4096 = sim != null ? sim.width : -1;
            sa.maxTextureSize = FrontRoomsPrintArray.WebGLSliceSize;   // what the sheet's WebGL tab uses
            imp.SetPlatformTextureSettings(sa);
            imp.SaveAndReimport();
            sim = AssetDatabase.LoadAssetAtPath<Texture2DArray>(tmpSheet);
            simW = sim != null ? sim.width : -1; simDepth = sim != null ? sim.depth : -1;
        }
        finally { AssetDatabase.DeleteAsset(tmpDir); }
        ok &= simW == FrontRoomsPrintArray.WebGLSliceSize && simDepth == FrontRoomsPrintArray.SliceCount && simW4096 == FrontRoomsPrintArray.SliceSize;
        report.Add($"Reduced import: the 4 x 2 sheet (2048 slices) imports with max size 4096 as {simW4096}^2 slices and with max size {FrontRoomsPrintArray.WebGLSliceSize} as {simW}^2 x {simDepth} (want 2048 and {FrontRoomsPrintArray.WebGLSliceSize}^2 x {FrontRoomsPrintArray.SliceCount}): the max size applies per slice");

        // (2) The postprocess code at the reduced size, run on stand-in textures of that size.
        const int r = FrontRoomsPrintArray.WebGLSliceSize;
        var mips = FrontRoomsPrintArray.MipCount(r, r);
        var arr = Own(new Texture2DArray(r, r, FrontRoomsPrintArray.SliceCount, TextureFormat.RGBA32, mips, true) { hideFlags = HideFlags.DontSave });
        FrontRoomsPrintArray.ApplySheetMips(arr, FrontRoomsPrintArray.SheetPath);
        var tex = Own(new Texture2D(r, r, TextureFormat.RGBA32, mips, true) { hideFlags = HideFlags.DontSave });
        FrontRoomsPrintMips.Apply(tex, TexDir + "Wallpaper_Print_P.png");
        double worst = 0;
        var paths = FrontRoomsPrintArray.SlicePaths();
        for (var k = 0; k < paths.Length; k++)
        {
            var px = FrontRoomsPrintArray.LoadPng(paths[k], out var w, out var h);
            for (var i = 0; i < px.Length; i++) px[i].b = 0;
            var chain = FrontRoomsPrintArray.Chain(px, w, h);
            for (var m = 0; m < mips; m++)
            {
                var got = arr.GetPixels(k, m); var want = chain[m + 1];
                for (var i = 0; i < got.Length; i++) worst = Math.Max(worst, Math.Max(Math.Abs(got[i].r - want[i].r), Math.Abs(got[i].g - want[i].g)) * 255.0);
                if (k == 0)
                {
                    var gt = tex.GetPixels(m);
                    for (var i = 0; i < gt.Length; i++) worst = Math.Max(worst, Math.Max(Math.Abs(gt[i].r - want[i].r), Math.Abs(gt[i].g - want[i].g)) * 255.0);
                }
            }
        }
        ok &= worst <= .501;   // 8-bit storage of the stand-ins: up to half a level
        // (3) The WebGL tabs as configured on the two production importers.
        string Tab(string path)
        {
            var i = (TextureImporter)AssetImporter.GetAtPath(path);
            var wg = i.GetPlatformTextureSettings("WebGL"); var sd = i.GetPlatformTextureSettings("Standalone");
            return "Standalone " + (sd.overridden ? sd.format + " max " + sd.maxTextureSize : "default") + " | WebGL " + (wg.overridden ? wg.format + " max " + wg.maxTextureSize : "default");
        }
        var tabs = "_PrintTex: " + Tab(TexDir + "Wallpaper_Print_P.png") + "; sheet: " + Tab(FrontRoomsPrintArray.SheetPath);
        json.Append("  \"reduced_import\": {\"sim_slice_at_max4096\": " + simW4096 + ", \"sim_slice_at_max1024\": " + simW + ", \"sim_depth\": " + simDepth + ", \"reduced_code_max_abs_vs_chain_levels\": " + worst.ToString("0.####", Inv)
            + ", \"tabs\": \"" + tabs + "\", \"pass\": " + B(ok) + "},\n");
        report.Add($"Reduced import code ({r}^2 stand-ins): every level vs the full-size rule chain's level m+1: max |delta| {worst:0.####} levels (8-bit storage) -> {(worst <= .501 ? "exact (within 8-bit storage)" : "MISMATCH")}. Tabs: {tabs}");
        return ok;
    }

    // ----------------------------------------------------------- BC5 gate
    static bool RunBc5Gate(Transform root, Texture2D prodTex, Texture2DArray prodArr, StringBuilder json, List<string> report)
    {
        var room = BuildRoom(root, "Q1b room", RoomOrigin, 6f, 9f, RoomRule.Lobby);
        var corridor = BuildRoom(root, "Q1b corridor", CorridorOrigin, CorridorWidth, CorridorDepth, RoomRule.Lobby, 3f);
        var wallMat = FrontRoomsSurfaces.Room(RoomRule.Lobby, FrontRoomsSurfaces.Slot.Wall);
        var roomWalls = room.GetComponentsInChildren<Renderer>().Where(r => r.sharedMaterial == wallMat).ToList();
        var corrWalls = corridor.GetComponentsInChildren<Renderer>().Where(r => r.sharedMaterial == wallMat).ToList();
        // Views: the 4 room views (corridor hidden), then the corridor (room hidden) with fog on and off.
        var views = new List<(string name, View v, bool corridorView, bool fog)>();
        foreach (var v in T1Views) views.Add((v.name, v, false, true));
        var cv = CorridorView; cv.pos += CorridorOrigin;
        views.Add(("corridor_fogOn", cv, true, true));
        views.Add(("corridor_fogOff", cv, true, false));
        var masks = new Dictionary<string, bool[]>();
        foreach (var (name, v, isCorr, _) in views)
        {
            room.gameObject.SetActive(!isCorr); corridor.gameObject.SetActive(isCorr);
            walls = isCorr ? corrWalls : roomWalls;
            Pose(v.pos, v.euler, v.fov);
            masks[name] = WallMask(isCorr ? corridor : room);
        }
        var cases = new (string name, Material production)[]
        {
            ("Lobby", FrontRoomsSurfaces.Get("L0_Wallpaper")),
            ("Shift", FrontRoomsSurfaces.Get("L0_Wallpaper_Shift")),
            ("Exit", FrontRoomsSurfaces.Get("Exit_Wallpaper")),
        };
        var modes = new (string name, Vector4 clock)[] { ("static", Vector4.zero), ("live_K03", new Vector4(3f, 8f, 0f, 1f)), ("live_K05.5", new Vector4(5.5f, 8f, 0f, 1f)) };
        var pass = true;
        var all = new List<Stats>(); var allP = new List<Stats>();
        var worstView = ""; double worstMean = 0, worstP99 = 0;
        var fogWas = RenderSettings.fog;
        json.Append("  \"bc5_gate\": {\"gate\": \"per view, case and mode: mean <= 1 and p99 <= 4 levels per channel, lit-HDR and film-post\", \"cases\": {\n");
        for (var ci = 0; ci < cases.Length; ci++)
        {
            var (cname, production) = cases[ci];
            var refMat = Clone(production, cname + " R8G8 reference");
            refMat.SetTexture("_PrintTex", refTex);
            json.Append("    \"" + cname + "\": {\n");
            const int tw = 480, th = 270;
            var sheet = new Color32[tw * views.Count * th * modes.Length];
            for (var vi = 0; vi < views.Count; vi++)
            {
                var (vname, v, isCorr, fog) = views[vi];
                room.gameObject.SetActive(!isCorr); corridor.gameObject.SetActive(isCorr);
                walls = isCorr ? corrWalls : roomWalls;
                RenderSettings.fog = fog;
                Pose(v.pos, v.euler, v.fov);
                var mask = masks[vname];
                json.Append("      \"" + vname + "\": {");
                for (var mi = 0; mi < modes.Length; mi++)
                {
                    var (mname, clock) = modes[mi];
                    Img iR, iP; byte[] bR, bP;
                    SetWalls(refMat);
                    if (clock.w > 0) Shader.SetGlobalTexture(PrintId, refArray);
                    Shader.SetGlobalVector(ClockId, clock);
                    iR = RenderFloat(false); bR = RenderBytes();
                    SetWalls(production);
                    if (clock.w > 0) Shader.SetGlobalTexture(PrintId, prodArr);
                    iP = RenderFloat(false); bP = RenderBytes();
                    ResetPrintGlobals(); Shader.SetGlobalTexture(PrintId, null);
                    var exp = ExposureFor(iR, mask);
                    var dR = Display(iR, exp); var dP = Display(iP, exp);
                    var sl = Diff(dR, dP, mask); var sf = Diff(Bytes3(bR), Bytes3(bP), mask);
                    all.Add(sl); allP.Add(sf);
                    var ok = sl.mean.All(x => x <= 1.0) && sl.p99.All(x => x <= 4.0) && sf.mean.All(x => x <= 1.0) && sf.p99.All(x => x <= 4.0);
                    pass &= ok;
                    var m = Math.Max(sl.mean.Max(), sf.mean.Max());
                    if (m > worstMean) { worstMean = m; worstView = cname + " " + vname + " " + mname; }
                    worstP99 = Math.Max(worstP99, Math.Max(sl.p99.Max(), sf.p99.Max()));
                    json.Append("\"" + mname + "\": {\"lit_hdr\": " + sl.Json() + ", \"film_post\": " + sf.Json() + ", \"pass\": " + B(ok) + "}" + (mi < modes.Length - 1 ? ", " : ""));
                    // Heat tile: max-channel |delta| of the lit metric (1 level dark red .. 4+ white), 4x down.
                    for (var y = 0; y < th; y++)
                        for (var x = 0; x < tw; x++)
                        {
                            float hv = 0; var inWall = false;
                            for (var yy = 0; yy < 4; yy++)
                                for (var xx = 0; xx < 4; xx++)
                                {
                                    var i = (y * 4 + yy) * W + x * 4 + xx;
                                    if (!mask[i]) continue;
                                    inWall = true;
                                    hv = Mathf.Max(hv, Mathf.Max(Mathf.Abs(dR[i * 3] - dP[i * 3]), Mathf.Max(Mathf.Abs(dR[i * 3 + 1] - dP[i * 3 + 1]), Mathf.Abs(dR[i * 3 + 2] - dP[i * 3 + 2]))));
                                }
                            sheet[((modes.Length - 1 - mi) * th + y) * tw * views.Count + vi * tw + x] = inWall ? Heat(hv) : new Color32(20, 20, 40, 255);
                        }
                    if (cname == "Lobby" && (vname == "close05" || vname == "corridor_fogOff") && mname != "live_K05.5")
                    {
                        var stem = Path.Combine(OutDir, "gate_" + cname + "_" + vname + "_" + mname);
                        SavePng(stem + "_R8G8.png", W, H, i => new Color32(bR[i * 3], bR[i * 3 + 1], bR[i * 3 + 2], 255));
                        SavePng(stem + "_" + FrontRoomsPrintArray.DesktopFormat + ".png", W, H, i => new Color32(bP[i * 3], bP[i * 3 + 1], bP[i * 3 + 2], 255));
                    }
                    report.Add($"BC5 gate {cname} {vname} {mname}: lit mean {F(sl.mean)} p99 {F(sl.p99)} max {F(sl.max)} | film mean {F(sf.mean)} p99 {F(sf.p99)} max {F(sf.max)} ({sl.n} px) -> {(ok ? "pass" : "FAIL")}");
                }
                json.Append("}" + (vi < views.Count - 1 ? ",\n" : "\n"));
            }
            json.Append("    }" + (ci < cases.Length - 1 ? ",\n" : "\n"));
            var tex = new Texture2D(tw * views.Count, th * modes.Length, TextureFormat.RGB24, false);
            tex.SetPixels32(sheet); tex.Apply(false);
            File.WriteAllBytes(Path.Combine(OutDir, "gate_heat_" + cname + ".png"), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            SetWalls(production);
        }
        RenderSettings.fog = fogWas;
        var pl = Pool(all); var pf = Pool(allP);
        json.Append("  }, \"pooled_lit_hdr\": " + pl.Json() + ", \"pooled_film_post\": " + pf.Json() + ", \"worst_mean_view\": \"" + worstView + "\", \"worst_mean\": " + worstMean.ToString("0.####", Inv)
            + ", \"worst_p99\": " + worstP99.ToString("0.####", Inv) + ", \"format\": \"" + prodTex.graphicsFormat + " / " + prodArr.graphicsFormat + "\", \"pass\": " + B(pass) + "},\n");
        report.Add($"BC5 gate ALL ({prodTex.graphicsFormat} vs R8G8; 3 palettes x 6 views x 3 modes): pooled lit mean {F(pl.mean)} p99 {F(pl.p99)} max {F(pl.max)} | film mean {F(pf.mean)} p99 {F(pf.p99)} max {F(pf.max)}; worst view mean {worstMean:0.###} ({worstView}), worst p99 {worstP99:0.##} -> {(pass ? "PASS" : "FAIL")}");
        room.gameObject.SetActive(false); corridor.gameObject.SetActive(false);
        return pass;
    }

    // ---------------------------------------------------------------- T2
    static bool RunT2Q1b(Transform root, Texture2D prodTex, Texture2DArray prodArr, StringBuilder json, List<string> report)
    {
        var production = FrontRoomsSurfaces.Get("L0_Wallpaper");
        var refStatic = Clone(production, "T2 print R8G8 static"); refStatic.SetTexture("_PrintTex", refTex);
        var cc0 = Clone(production, "T2 print CC0 static"); cc0.SetTexture("_PrintTex", LoadRef(CC0Print));
        EnsureTestWall(root, production);
        var wallR = testWall.GetComponent<Renderer>();
        var thetas = new[] { 2f, 5f, 9f, 14f, 20f, 27f, 35f, 44f, 54f, 65f, 77f, 90f };
        Pose(FaceCentre + new Vector3(.25f, -.45f, -1.6f), new Vector3(-15f, -8f, 0f), 60f);
        var variants = new (string name, Material m, Texture2DArray arr, Vector4 clock)[]
        {
            ("static R8G8", refStatic, null, Vector4.zero),
            ("static CC0", cc0, null, Vector4.zero),
            ("live BC5 K03", production, prodArr, new Vector4(3f, 8f, 0f, 1f)),
            ("live BC5 K05.5", production, prodArr, new Vector4(5.5f, 8f, 0f, 1f)),
            ("live R8G8 K06", production, refArray, new Vector4(6f, 8f, 0f, 1f)),
        };
        Img Rend(Material m, Texture2DArray arr, Vector4 clock, int mode)
        {
            wallR.sharedMaterial = m;
            Shader.SetGlobalTexture(PrintId, arr); Shader.SetGlobalVector(ClockId, clock);
            SetDebug(mode);
            var img = RenderFloat(false, rtFloatSmall);
            SetDebug(0); ResetPrintGlobals(); Shader.SetGlobalTexture(PrintId, null);
            return img;
        }
        long CountDiff(Img x, Img y) { long n = 0; for (var i = 0; i < x.px.Length; i++) if (BitConverter.SingleToInt32Bits(x.px[i]) != BitConverter.SingleToInt32Bits(y.px[i])) n++; return n; }
        double MeanAbs(Img x, Img y) { double s = 0; for (var i = 0; i < x.px.Length; i += 4) s += Math.Abs(x.px[i] - y.px[i]) + Math.Abs(x.px[i + 1] - y.px[i + 1]) + Math.Abs(x.px[i + 2] - y.px[i + 2]); return s / (x.px.Length / 4 * 3); }
        var specDiff = new long[variants.Length]; var maskDiff = new long[variants.Length]; var diffuse = new double[variants.Length];
        for (var k = 0; k < thetas.Length; k++)
        {
            PlaceLight(thetas[k], Vector3.up, 1.3f, 2.4f);
            var sA = Rend(production, null, Vector4.zero, 1);
            var dA = Rend(production, null, Vector4.zero, 2);
            var m3A = Rend(production, null, Vector4.zero, 3); var m4A = Rend(production, null, Vector4.zero, 4);
            for (var v = 0; v < variants.Length; v++)
            {
                var (_, m, arr, clock) = variants[v];
                specDiff[v] += CountDiff(sA, Rend(m, arr, clock, 1));
                maskDiff[v] += CountDiff(m3A, Rend(m, arr, clock, 3)) + CountDiff(m4A, Rend(m, arr, clock, 4));
                diffuse[v] += MeanAbs(dA, Rend(m, arr, clock, 2)) / thetas.Length;
            }
        }
        var pass = specDiff.All(x => x == 0) && maskDiff.All(x => x == 0) && diffuse.Skip(1).All(x => x > 0);
        json.Append("  \"T2_q1b\": {\"printA\": \"production static (" + prodTex.graphicsFormat + ", K00)\", \"positions\": 12, \"variants\": [");
        for (var v = 0; v < variants.Length; v++)
        {
            json.Append("{\"name\": \"" + variants[v].name + "\", \"spec_diff_values\": " + specDiff[v] + ", \"wear_mask_diff_values\": " + maskDiff[v] + ", \"diffuse_meanabs\": " + diffuse[v].ToString("0.######", Inv) + "}" + (v < variants.Length - 1 ? ", " : ""));
            report.Add($"T2 Q1b: print A (production static) vs {variants[v].name}: specular differing values {specDiff[v]}, wear masks {maskDiff[v]}, diffuse mean|d| {diffuse[v]:0.######} (12 light positions)");
        }
        json.Append("], \"pass\": " + B(pass) + "},\n");
        report.Add($"T2 Q1b -> {(pass ? "PASS (specular bit-identical for every print)" : "FAIL")}");
        return pass;
    }

    // ---------------------------------------------------------- crossfade
    static bool RunCrossfade(Transform root, Texture2D prodTex, Texture2DArray prodArr, UniversalRenderPipelineAsset urp, StringBuilder json, List<string> report)
    {
        var production = FrontRoomsSurfaces.Get("L0_Wallpaper");
        var refStatic = Clone(production, "crossfade R8G8 static"); refStatic.SetTexture("_PrintTex", refTex);
        EnsureTestWall(root, production);
        var wallR = testWall.GetComponent<Renderer>();
        RenderSettings.ambientSkyColor = FrontRoomsLook.AmbientSky; RenderSettings.ambientEquatorColor = FrontRoomsLook.AmbientEquator; RenderSettings.ambientGroundColor = FrontRoomsLook.AmbientGround;
        DynamicGI.UpdateEnvironment();
        PlaceLight(60f, Vector3.up, 1.6f, 3f);
        var poses = new[] { ("near", FaceCentre + Vector3.back * 2.6f, Vector3.zero), ("far", FaceCentre + new Vector3(0f, .2f, -6.5f), new Vector3(2f, 0f, 0f)), ("close", FaceCentre + new Vector3(.3f, 0f, -.45f), Vector3.zero) };
        var formats = new (string name, Material m, Texture2DArray arr)[] { ("R8G8", refStatic, refArray), (prodTex.graphicsFormat.ToString(), production, prodArr) };
        var pass = true; double worstMax = 0, worstMean = 0;
        json.Append("  \"crossfade\": {\"gate\": \"albedo and lit, w in {0.25, 0.5, 1} vs w = 0 at clock frame 0 (n = 8): mean 0, max <= 0.2 levels\", \"runs\": [");
        var first = true;
        foreach (var (fname, m, arr) in formats)
            foreach (var (pname, pos, euler) in poses)
                foreach (var scale in new[] { 1f, .5f })
                    foreach (var mode in new[] { 5, 0 })
                    {
                        if (urp != null) urp.renderScale = scale;
                        Pose(pos, euler, 60f);
                        wallR.sharedMaterial = m;
                        Shader.SetGlobalTexture(PrintId, arr);
                        SetDebug(mode);
                        Shader.SetGlobalVector(ClockId, new Vector4(0f, 8f, 0f, 0f));
                        var w0 = RenderFloat(false, rtFloatSmall);
                        foreach (var w in new[] { .25f, .5f, 1f })
                        {
                            Shader.SetGlobalVector(ClockId, new Vector4(0f, 8f, 0f, w));
                            var lw = RenderFloat(false, rtFloatSmall);
                            var e = mode == 5 ? 1f : .9f / Mathf.Max(1e-5f, Peak(w0));
                            var st = Diff(Display(w0, e), Display(lw, e), null);
                            var mx = st.max.Max(); var mn = st.mean.Max();
                            var ok = mn <= .0005 && mx <= .2;
                            pass &= ok; worstMax = Math.Max(worstMax, mx); worstMean = Math.Max(worstMean, mn);
                            json.Append((first ? "" : ", ") + "{\"format\": \"" + fname + "\", \"pose\": \"" + pname + "\", \"scale\": " + scale.ToString("0.0", Inv) + ", \"view\": \"" + (mode == 5 ? "albedo" : "lit") + "\", \"w\": " + w.ToString("0.##", Inv)
                                + ", \"mean\": " + J2(st.mean) + ", \"max\": " + J2(st.max) + ", \"pass\": " + B(ok) + "}");
                            first = false;
                            if (!ok || (w == 1f && scale == 1f && pname == "near"))
                                report.Add($"Crossfade {fname} {pname} scale {scale:0.0} {(mode == 5 ? "albedo" : "lit")} w {w:0.##}: mean {F(st.mean)} max {F(st.max)} -> {(ok ? "pass" : "FAIL")}");
                        }
                        SetDebug(0); ResetPrintGlobals(); Shader.SetGlobalTexture(PrintId, null);
                    }
        if (urp != null) urp.renderScale = 1f;
        json.Append("], \"worst_mean\": " + worstMean.ToString("0.######", Inv) + ", \"worst_max\": " + worstMax.ToString("0.######", Inv) + ", \"pass\": " + B(pass) + "},\n");
        report.Add($"Crossfade static vs live (both formats, 3 poses, scale 1/0.5, albedo + lit, w .25/.5/1; 72 runs): worst mean {worstMean:0.######}, worst max {worstMax:0.######} levels -> {(pass ? "PASS" : "FAIL")}");
        return pass;
    }

    // -------------------------------------------------------------- seams
    /// <summary>
    /// The roll tile on a wall (u = -x / 0.75 on a -z facing wall, v = y / 1.125): roll seams at
    /// world x = 0.75 k, pattern repeats at y = 1.125 k. Three albedo views of the same wall, camera
    /// 0.25 m away at a seam crossing (1 px ~ 0.27 mm, the print magnified ~1.4x), and at 1 m:
    ///   paper only  (_PrintTex = an empty print): the paper's own roll seam, a ~1 mm shadow drawn
    ///               at u = 0 by the generator. Its darkest column must sit on the projected seam
    ///               line (|offset| <= 1.5 px), so the paper and the tile mapping agree;
    ///   print only  (_BaseMap = a neutral paper, alpha 1): the print must be continuous across the
    ///               same line: the column (row) gradient there is no stronger than ordinary columns
    ///               (<= max(2 x median, p99));
    ///   production  both, which is what the game draws, saved with seam ticks in a margin band.
    /// </summary>
    static bool RunSeams(Transform root, Texture2D prodTex, StringBuilder json, List<string> report)
    {
        var production = FrontRoomsSurfaces.Get("L0_Wallpaper");
        var paperOnly = Clone(production, "seam paper only");
        var emptyPrint = Own(new Texture2D(4, 4, TextureFormat.RGBA32, false, true) { hideFlags = HideFlags.DontSave });
        emptyPrint.SetPixels32(Enumerable.Repeat(new Color32(0, 0, 0, 255), 16).ToArray()); emptyPrint.Apply(false);
        paperOnly.SetTexture("_PrintTex", emptyPrint);
        var printOnly = Clone(production, "seam print only");
        var neutralPaper = Own(new Texture2D(4, 4, TextureFormat.RGBA32, false, true) { hideFlags = HideFlags.DontSave });
        // (alpha, beta, gamma) = texel * (0.70, 0.14, 0.02) + (0.46, -0.001, -0.004) = (0.999, 0.0001, 0.0)
        neutralPaper.SetPixels32(Enumerable.Repeat(new Color32(197, 2, 51, 255), 16).ToArray()); neutralPaper.Apply(false);
        printOnly.SetTexture("_BaseMap", neutralPaper);
        EnsureTestWall(root, production);
        var wallR = testWall.GetComponent<Renderer>();
        RenderSettings.ambientSkyColor = FrontRoomsLook.AmbientSky; RenderSettings.ambientEquatorColor = FrontRoomsLook.AmbientEquator; RenderSettings.ambientGroundColor = FrontRoomsLook.AmbientGround;
        DynamicGI.UpdateEnvironment();
        PlaceLight(55f, new Vector3(-.4f, .9f, 0f), 2.2f, 4f);
        var faceZ = FaceCentre.z;
        var seamX = new[] { 39.0f, 39.75f, 40.5f, 41.25f };
        var seamY = new[] { 1.125f, 2.25f };
        var shots = new[] { ("close_0.25m", new Vector3(39.75f, 1.125f, faceZ - .25f)), ("mid_1.0m", new Vector3(39.75f, 1.125f, faceZ - 1.0f)) };
        var pass = true;
        json.Append("  \"seams\": {\"rule\": \"paper-only seam darkest column within 1.5 px of the projected line; print-only gradient on the line <= max(2 x median, p99) of all columns (rows)\", \"shots\": [");
        var firstShot = true;
        foreach (var (name, pos) in shots)
        {
            Pose(pos, Vector3.zero, 60f);
            float[] Lum(Material m)
            {
                wallR.sharedMaterial = m; SetDebug(5);
                var img = RenderFloat(false);
                SetDebug(0);
                var l = new float[img.w * img.h];
                for (var i = 0; i < l.Length; i++) l[i] = 255f * Srgb(.2126f * img.px[i * 4] + .7152f * img.px[i * 4 + 1] + .0722f * img.px[i * 4 + 2]);
                return l;
            }
            var lumPaper = Lum(paperOnly); var lumPrint = Lum(printOnly);
            wallR.sharedMaterial = production; SetDebug(5);
            var alb = RenderFloat(false);
            SetDebug(0);
            var lit = RenderBytes();
            int w = alb.w, h = alb.h;
            // Column (row) mean gradients of the print-only luminance.
            var colG = new double[w - 1]; var rowG = new double[h - 1];
            for (var y = 0; y < h; y++) for (var x = 0; x < w - 1; x++) colG[x] += Math.Abs(lumPrint[y * w + x + 1] - lumPrint[y * w + x]) / h;
            for (var y = 0; y < h - 1; y++) for (var x = 0; x < w; x++) rowG[y] += Math.Abs(lumPrint[(y + 1) * w + x] - lumPrint[y * w + x]) / w;
            var sortedC = colG.OrderBy(v => v).ToArray(); var sortedR = rowG.OrderBy(v => v).ToArray();
            double medC = sortedC[sortedC.Length / 2], p99C = sortedC[(int)(sortedC.Length * .99)], medR = sortedR[sortedR.Length / 2], p99R = sortedR[(int)(sortedR.Length * .99)];
            // Paper-only column means (the roll seam is a vertical shadow line).
            var colPaper = new double[w];
            for (var y = 0; y < h; y++) for (var x = 0; x < w; x++) colPaper[x] += lumPaper[y * w + x] / h;
            var cols = new List<(float world, float px, int paperMin, double printRatio, bool ok)>();
            var rows = new List<(float world, float px, double printRatio, bool ok)>();
            foreach (var sx in seamX)
            {
                var sp = cam.WorldToScreenPoint(new Vector3(sx, pos.y, faceZ));
                var fx = sp.x * w / cam.pixelWidth - .5f;   // pixel-centre coordinates
                var px = Mathf.RoundToInt(fx);
                if (px < 16 || px > w - 17) continue;
                var paperMin = px; for (var x = px - 12; x <= px + 12; x++) if (colPaper[x] < colPaper[paperMin]) paperMin = x;
                var g = Math.Max(colG[Mathf.Clamp(Mathf.FloorToInt(fx), 0, w - 2)], colG[Mathf.Clamp(Mathf.FloorToInt(fx) - 1, 0, w - 2)]);
                var ok = Math.Abs(paperMin - fx) <= 1.5f && g <= Math.Max(2 * medC, p99C);
                cols.Add((sx, fx, paperMin, g / Math.Max(1e-6, medC), ok));
            }
            foreach (var sy in seamY)
            {
                var sp = cam.WorldToScreenPoint(new Vector3(pos.x, sy, faceZ));
                var fy = sp.y * h / cam.pixelHeight - .5f;
                var py = Mathf.FloorToInt(fy);
                if (py < 2 || py > h - 3) continue;
                var g = Math.Max(rowG[Mathf.Clamp(py, 0, h - 2)], rowG[Mathf.Clamp(py - 1, 0, h - 2)]);
                rows.Add((sy, fy, g / Math.Max(1e-6, medR), g <= Math.Max(2 * medR, p99R)));
            }
            var shotOk = cols.Count > 0 && cols.All(c => c.ok) && rows.All(r => r.ok);
            pass &= shotOk;
            json.Append((firstShot ? "" : ", ") + "{\"shot\": \"" + name + "\", \"print_col_grad_median\": " + medC.ToString("0.###", Inv) + ", \"print_col_grad_p99\": " + p99C.ToString("0.###", Inv)
                + ", \"print_row_grad_median\": " + medR.ToString("0.###", Inv) + ", \"print_row_grad_p99\": " + p99R.ToString("0.###", Inv)
                + ", \"roll_seams\": [" + string.Join(", ", cols.Select(c => "{\"x\": " + c.world.ToString("0.###", Inv) + ", \"projected_px\": " + c.px.ToString("0.##", Inv) + ", \"paper_seam_px\": " + c.paperMin
                    + ", \"print_grad_ratio\": " + c.printRatio.ToString("0.###", Inv) + ", \"ok\": " + B(c.ok) + "}"))
                + "], \"repeat_rows\": [" + string.Join(", ", rows.Select(r => "{\"y\": " + r.world.ToString("0.###", Inv) + ", \"projected_px\": " + r.px.ToString("0.##", Inv) + ", \"print_grad_ratio\": " + r.printRatio.ToString("0.###", Inv) + ", \"ok\": " + B(r.ok) + "}"))
                + "], \"pass\": " + B(shotOk) + "}");
            firstShot = false;
            report.Add($"Seams {name}: roll seams " + string.Join(", ", cols.Select(c => $"x {c.world}: line at px {c.px:0.0}, paper seam at px {c.paperMin}, print gradient {c.printRatio:0.##}x median"))
                + $" (print column p99 {p99C / Math.Max(1e-6, medC):0.##}x); repeats " + string.Join(", ", rows.Select(r => $"y {r.world}: print gradient {r.printRatio:0.##}x median")) + $" (row p99 {p99R / Math.Max(1e-6, medR):0.##}x) -> {(shotOk ? "lined up, seamless" : "MISALIGNED")}");
            // Frames with a 28 px margin band (top and left) that carries the seam ticks, never over the wall.
            const int band = 28;
            var tickCols = cols.Select(c => Mathf.RoundToInt(c.px)).ToArray(); var tickRows = rows.Select(r => Mathf.RoundToInt(r.px)).ToArray();
            void SaveWithTicks(string file, Func<int, Color32> pixel)
            {
                int ow = w + band, oh = h + band;
                var paperCol = new Color32(245, 245, 240, 255); var tick = new Color32(0, 150, 255, 255);
                SavePng(file, ow, oh, i =>
                {
                    int x = i % ow, y = i / ow;                  // Unity rows bottom-up: the top band is y >= h
                    var inX = x - band;
                    if (y >= h) return x >= band && tickCols.Any(c => Math.Abs(inX - c) <= 1) ? tick : paperCol;
                    if (x < band) return tickRows.Any(r => Math.Abs(y - r) <= 1) ? tick : paperCol;
                    return pixel(y * w + inX);
                });
            }
            Color32 Grey(float v) { var b = (byte)Mathf.Clamp(v, 0, 255); return new Color32(b, b, b, 255); }
            SaveWithTicks(Path.Combine(OutDir, "seam_" + name + "_albedo.png"), i => new Color32((byte)(255 * Srgb(alb.px[i * 4])), (byte)(255 * Srgb(alb.px[i * 4 + 1])), (byte)(255 * Srgb(alb.px[i * 4 + 2])), 255));
            SaveWithTicks(Path.Combine(OutDir, "seam_" + name + "_paper_only.png"), i => Grey(lumPaper[i]));
            SaveWithTicks(Path.Combine(OutDir, "seam_" + name + "_print_only.png"), i => Grey(lumPrint[i]));
            SaveWithTicks(Path.Combine(OutDir, "seam_" + name + "_lit.png"), i => new Color32(lit[i * 3], lit[i * 3 + 1], lit[i * 3 + 2], 255));
        }
        testWall.GetComponent<Renderer>().sharedMaterial = production;
        json.Append("], \"pass\": " + B(pass) + "},\n");
        return pass;
    }

    // ------------------------------------------------------------- memory
    static void WriteQ1bMemory(Texture2D prodTex, Texture2DArray prodArr, StringBuilder json, List<string> report)
    {
        long Size(int s, int depth, GraphicsFormat f)
        {
            long sum = 0;
            for (var m = 0; m < FrontRoomsPrintArray.MipCount(s, s); m++) sum += (long)GraphicsFormatUtility.ComputeMipmapSize(Math.Max(1, s >> m), Math.Max(1, s >> m), f);
            return sum * depth;
        }
        const int s = FrontRoomsPrintArray.SliceSize, n = FrontRoomsPrintArray.SliceCount;
        var bc5 = GraphicsFormat.RG_BC5_UNorm; var rg = GraphicsFormat.R8G8_UNorm;
        var rows = new (string tier, long tex, long arr)[]
        {
            ("desktop BC5 2048", Size(s, 1, bc5), Size(s, n, bc5)),
            ("desktop R8G8 2048 (fallback)", Size(s, 1, rg), Size(s, n, rg)),
            ("WebGL R8G8 1024", Size(s / 2, 1, rg), Size(s / 2, n, rg)),
            ("P0 shipped (R8G8 2048x3072, no array)", 16777214, 0),
        };
        var runtimeTex = Profiler.GetRuntimeMemorySizeLong(prodTex); var runtimeArr = Profiler.GetRuntimeMemorySizeLong(prodArr);
        json.Append("  \"memory\": {\"tiers\": [" + string.Join(", ", rows.Select(r => "{\"tier\": \"" + r.tier + "\", \"printTex_bytes\": " + r.tex + ", \"array_bytes\": " + r.arr + ", \"total_bytes\": " + (r.tex + r.arr) + "}"))
            + "], \"editor_runtime_size_printTex\": " + runtimeTex + ", \"editor_runtime_size_array\": " + runtimeArr + "},\n");
        foreach (var r in rows) report.Add($"Memory {r.tier}: _PrintTex {r.tex / 1048576.0:0.00} MiB + array {r.arr / 1048576.0:0.00} MiB = {(r.tex + r.arr) / 1048576.0:0.00} MiB");
        report.Add($"Memory measured in the editor (Profiler.GetRuntimeMemorySizeLong): _PrintTex {runtimeTex / 1048576.0:0.00} MiB, array {runtimeArr / 1048576.0:0.00} MiB");
    }

    // ------------------------------------------------- P0's compressed mip check
    /// <summary>P0's CheckPrintMips for a block-compressed _PrintTex. The question is the same as for
    /// R8G8: are the importer's mips the rule chain (FrontRoomsPrintMips), not Unity's box mips?
    /// Pass: mip 0 within 1 level of the source, and every lower level closer to the rule than to
    /// the box chain. The block error itself (BC5 keeps 8 steps per 4 x 4 block, so the soft middle
    /// levels of a hard-edged print carry 1-2.5 levels of it) is not this check's bar: its effect
    /// on the picture is the Q1b render gate (mean <= 1, p99 <= 4 levels against R8G8).</summary>
    static bool CheckPrintMipsCompressed(Texture2D tex, Color[] mip0, int w0, int h0, List<Color[]> rule, List<Color[]> box, StringBuilder json, List<string> report)
    {
        var ok = true;
        var rows = new List<string>();
        json.Append("  \"print_mips\": {\"format\": \"" + tex.graphicsFormat + "\", \"bar\": \"mip0 mean |GPU - source| <= 1 level; every lower level closer to the rule than to box mips\", \"levels\": [");
        for (var m = 0; m < tex.mipmapCount; m++)
        {
            var got = FetchRG(tex, 0, m, out var w, out var h);
            double meanRule = 0, meanBox = 0;
            var n = w * h;
            for (var i = 0; i < n; i++)
            {
                meanRule += Math.Abs(got[i * 2] - rule[m][i].r * 255.0);
                meanBox += Math.Abs(got[i * 2] - box[m][i].r * 255.0);
            }
            meanRule /= n; meanBox /= n;
            var levelOk = m == 0 ? meanRule <= 1.0 : meanRule < meanBox;
            ok &= levelOk;
            json.Append("{\"mip\": " + m + ", \"density_mean_abs_vs_rule\": " + meanRule.ToString("0.###", Inv) + ", \"density_mean_abs_vs_box\": " + meanBox.ToString("0.###", Inv) + "}" + (m < tex.mipmapCount - 1 ? ", " : ""));
            rows.Add($"mip{m} {meanRule:0.##}/{meanBox:0.##}");
        }
        json.Append("], \"pass\": " + B(ok) + "},\n");
        report.Add($"Print mips ({tex.graphicsFormat}, GPU-decoded): mean |GPU - rule| / |GPU - box| density, 8-bit levels: " + string.Join(", ", rows) + $" -> {(ok ? "the importer's mips are the rule" : "MISMATCH")}");
        return ok;
    }

    // ------------------------------------------------------------ captures
    // For Red: before = the CC0 chevron frame P0 shipped (kept as RefDir/Wallpaper_Print_CC0_P, R8G8),
    // after = the Hard edge static frame (production _PrintTex). Everything else is production.
    static readonly (RoomRule rule, Color color, float intensity)[] CaptureRules =
    {
        // FrontRoomsRoomStream.ProfileLightColor / ProfileLightIntensity (5.2 = the rig's 5).
        (RoomRule.Lobby, new Color(1f, .96f, .88f), 5.2f),
        (RoomRule.Shift, new Color(.86f, .93f, .78f), 4.2f),
        (RoomRule.Exit, new Color(.62f, .92f, .90f), 4.4f),
    };

    static void SavePair(string file, byte[] a, byte[] b)
    {
        // before | after, side by side, full resolution, 16 px white gutter.
        const int gap = 16;
        int ow = W * 2 + gap;
        SavePng(file, ow, H, i =>
        {
            int x = i % ow, y = i / ow;
            if (x < W) { var j = (y * W + x) * 3; return new Color32(a[j], a[j + 1], a[j + 2], 255); }
            if (x < W + gap) return new Color32(255, 255, 255, 255);
            var k = (y * W + x - W - gap) * 3; return new Color32(b[k], b[k + 1], b[k + 2], 255);
        });
    }

    static void SaveBytes(string file, byte[] b) => SavePng(file, W, H, i => new Color32(b[i * 3], b[i * 3 + 1], b[i * 3 + 2], 255));

    static Material BeforeOf(Material production)
    {
        var m = Clone(production, production.name + " (before: CC0 print)");
        m.SetTexture("_PrintTex", LoadRef(CC0Print));
        return m;
    }

    static void RunQ1bCapturesInternal() => WithRig("print_q1b_captures", (root, json, report, urp) =>
    {
        var prodArr = ProdArray;
        if (prodArr == null) throw new InvalidOperationException("Run FrontRooms > Rendering > Build Print Array first.");
        var dir = OutDir;
        // 1. One rig room per rule, lit like the title stream's rule, two views each.
        var roomViews = new[] { T1Views[0], T1Views[2] };   // front2m, oblique
        foreach (var (rule, color, intensity) in CaptureRules)
        {
            var room = BuildRoom(root, "Q1b capture " + rule, RoomOrigin, 6f, 9f, rule);
            foreach (var l in room.GetComponentsInChildren<Light>()) { l.color = color; l.intensity = 5f * intensity / 5.2f; }
            var production = FrontRoomsSurfaces.Room(rule, FrontRoomsSurfaces.Slot.Wall);
            walls = room.GetComponentsInChildren<Renderer>().Where(r => r.sharedMaterial == production).ToList();
            var before = BeforeOf(production);
            foreach (var v in roomViews)
            {
                Pose(v.pos, v.euler, v.fov);
                SetWalls(before); var b0 = RenderBytes();
                SetWalls(production); var b1 = RenderBytes();
                SaveBytes(Path.Combine(dir, "room_" + rule + "_" + v.name + "_before.png"), b0);
                SaveBytes(Path.Combine(dir, "room_" + rule + "_" + v.name + "_after.png"), b1);
                SavePair(Path.Combine(dir, "pair_room_" + rule + "_" + v.name + ".png"), b0, b1);
            }
            room.gameObject.SetActive(false);
            report.Add($"Captures: room {rule} ({production.name}), light {color} x{intensity}: front2m + oblique, before/after");
        }
        // 2. The 25 m corridor (Lobby), fog on, and a 0.6 m close-up.
        {
            var room = BuildRoom(root, "Q1b capture corridor", CorridorOrigin, CorridorWidth, CorridorDepth, RoomRule.Lobby, 3f);
            var production = FrontRoomsSurfaces.Room(RoomRule.Lobby, FrontRoomsSurfaces.Slot.Wall);
            walls = room.GetComponentsInChildren<Renderer>().Where(r => r.sharedMaterial == production).ToList();
            var before = BeforeOf(production);
            var fogWas = RenderSettings.fog; RenderSettings.fog = true;
            var cv = CorridorView; cv.pos += CorridorOrigin;
            var close = new View { name = "close06", pos = CorridorOrigin + new Vector3(.55f, 1.5f, 6f), euler = new Vector3(0f, -90f, 0f), fov = 60f };
            foreach (var v in new[] { cv, close })
            {
                Pose(v.pos, v.euler, v.fov);
                SetWalls(before); var b0 = RenderBytes();
                SetWalls(production); var b1 = RenderBytes();
                SaveBytes(Path.Combine(dir, "corridor_" + v.name + "_before.png"), b0);
                SaveBytes(Path.Combine(dir, "corridor_" + v.name + "_after.png"), b1);
                SavePair(Path.Combine(dir, "pair_corridor_" + v.name + ".png"), b0, b1);
            }
            RenderSettings.fog = fogWas;
            room.gameObject.SetActive(false);
            report.Add("Captures: Lobby corridor 25 m (fog on) and a 0.6 m close-up, before/after");
        }
        // 3. The motion frames on a wall: K00..K07 forced through the live path (clock (k, 8, 0, 1)),
        // then the half-way blends (k + 0.5). Lobby and Exit palettes. 4 x 2 sheets of 960 x 540 tiles.
        {
            RenderSettings.fog = false;
            foreach (var (label, matName) in new[] { ("Lobby", "L0_Wallpaper"), ("Exit", "Exit_Wallpaper") })
            {
                var production = FrontRoomsSurfaces.Get(matName);
                EnsureTestWall(root, production);
                PlaceLight(62f, new Vector3(-.3f, .9f, 0f), 2.4f, 5.5f);
                // 1.5 m wide view: two rolls side by side, about one repeat high.
                Pose(new Vector3(40.375f, 1.4f, FaceCentre.z - 1.30f), Vector3.zero, 50f);
                foreach (var (tag, offset) in new[] { ("slices", 0f), ("blends", .5f) })
                {
                    const int tw = 960, th = 540;
                    var sheet = new Color32[tw * 4 * th * 2];
                    for (var k = 0; k < 8; k++)
                    {
                        Shader.SetGlobalTexture(PrintId, prodArr);
                        Shader.SetGlobalVector(ClockId, new Vector4(k + offset, 8f, 0f, 1f));
                        var img = RenderFloat(true, rtRake);   // film post on, 960 x 540, 4x MSAA
                        ResetPrintGlobals(); Shader.SetGlobalTexture(PrintId, null);
                        int col = k % 4, row = k / 4;
                        for (var y = 0; y < th; y++)
                            for (var x = 0; x < tw; x++)
                            {
                                var si = (y * tw + x) * 4;
                                var dy = (1 - row) * th + y; var dx = col * tw + x;
                                sheet[dy * tw * 4 + dx] = new Color32((byte)(255 * Srgb(img.px[si])), (byte)(255 * Srgb(img.px[si + 1])), (byte)(255 * Srgb(img.px[si + 2])), 255);
                            }
                    }
                    var tex = new Texture2D(tw * 4, th * 2, TextureFormat.RGB24, false);
                    tex.SetPixels32(sheet); tex.Apply(false);
                    File.WriteAllBytes(Path.Combine(dir, "strip_" + label + "_" + tag + ".png"), tex.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(tex);
                }
            }
            report.Add("Captures: slice strips (K00..K07 and the k+0.5 blends), Lobby and Exit palettes, 4 x 2 sheets");
        }
        File.WriteAllText(Path.Combine(dir, "captures_summary.txt"), string.Join("\n", report) + "\n");
        Debug.Log("[PrintQ1b captures]\n" + string.Join("\n", report));
    });

    /// <summary>The game scene's edit-mode profile preview (FrontRooms3DGame.BuildEditorPreview: one
    /// title-stream room per rule, its own lights, outlets and trim), as FrontRoomsLookdevCapture frames
    /// it: Lobby (Level 0), Shift and Exit rooms, forward and back, before and after. The wallpaper
    /// renderers get CC0 clones for "before"; no asset or scene is changed or saved.
    /// Batch: -executeMethod FrontRoomsPrintP0Test.RunQ1bRoomCapturesBatch.</summary>
    public static void RunQ1bRoomCapturesBatch()
    {
        const string scenePath = "Assets/Scenes/FrontRooms3D.unity";
        var outDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Verification", "print_q1b_captures");
        Directory.CreateDirectory(outDir);
        var asyncWas = ShaderUtil.allowAsyncCompilation;
        ShaderUtil.allowAsyncCompilation = false;
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var game = UnityEngine.Object.FindFirstObjectByType<FrontRooms3DGame>();
        if (game == null) throw new InvalidOperationException("No FrontRooms3DGame in " + scenePath);
        game.enabled = false; game.enabled = true;   // rebuild the edit-mode preview from today's assets
        var hunter = game.transform.Find("Hunter");
        if (hunter != null) hunter.gameObject.SetActive(false);
        FrontRoomsLook.ApplyAmbient();
        var volume = FrontRoomsPostStack.Ensure(null);
        if (volume != null) volume.gameObject.hideFlags = HideFlags.DontSave;
        var camGo = new GameObject("Q1b room capture camera") { hideFlags = HideFlags.DontSave };
        var camera = camGo.AddComponent<Camera>();
        camera.fieldOfView = 76f; camera.nearClipPlane = .06f; camera.farClipPlane = 80f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.13f, .13f, .11f);
        FrontRoomsPostStack.ConfigureCamera(camera);
        var data = camera.GetUniversalAdditionalCameraData();
        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        camera.targetTexture = rt;
        var wallpapers = new[] { "L0_Wallpaper", "L0_Wallpaper_Shift", "Exit_Wallpaper" }.Select(n => FrontRoomsSurfaces.Get(n)).ToArray();
        var clones = new Dictionary<Material, Material>();
        foreach (var m in wallpapers)
        {
            var c = new Material(m) { name = m.name + " (before: CC0 print)", hideFlags = HideFlags.DontSave };
            c.SetTexture("_PrintTex", AssetDatabase.LoadAssetAtPath<Texture2D>(RefDir + CC0Print + ".png"));
            clones[m] = c;
        }
        var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var saved = renderers.Select(r => r.sharedMaterials).ToArray();
        var log = new List<string>();
        byte[] Shot(Vector3 p, Quaternion q)
        {
            camera.transform.SetPositionAndRotation(p, q);
            camera.Render(); camera.Render();
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var t = new Texture2D(W, H, TextureFormat.RGB24, false);
            t.ReadPixels(new Rect(0, 0, W, H), 0, 0); t.Apply(false);
            RenderTexture.active = prev;
            var b = t.GetRawTextureData<byte>().ToArray();
            UnityEngine.Object.DestroyImmediate(t);
            return b;
        }
        void Swap(bool before)
        {
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].sharedMaterials = before ? saved[i].Select(m => m != null && clones.TryGetValue(m, out var c) ? c : m).ToArray() : saved[i];
            }
        }
        try
        {
            var root = game.transform;
            foreach (var rule in new[] { RoomRule.Lobby, RoomRule.Shift, RoomRule.Exit })
            {
                var z = (int)rule * FrontRoomsRoomStream.RoomLength;
                var shots = new[]
                {
                    ("forward", root.TransformPoint(new Vector3(0f, 1.62f, z + 1.6f)), Quaternion.Euler(4f, 0f, 0f)),
                    ("back", root.TransformPoint(new Vector3(3.6f, 1.62f, z + 10.4f)), Quaternion.Euler(10f, 205f, 0f)),
                    ("wall", root.TransformPoint(new Vector3(-4.1f, 1.45f, z + 5.2f)), Quaternion.Euler(2f, -90f, 0f)),
                };
                foreach (var (name, pos, rot) in shots)
                {
                    Swap(true); var b0 = Shot(pos, rot);
                    Swap(false); var b1 = Shot(pos, rot);
                    SaveBytes(Path.Combine(outDir, "stream_" + rule + "_" + name + "_before.png"), b0);
                    SaveBytes(Path.Combine(outDir, "stream_" + rule + "_" + name + "_after.png"), b1);
                    SavePair(Path.Combine(outDir, "pair_stream_" + rule + "_" + name + ".png"), b0, b1);
                    log.Add("stream " + rule + " " + name + " at " + pos.ToString("F2"));
                }
            }
        }
        finally
        {
            Swap(false);
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(camGo);
            rt.Release();
            foreach (var c in clones.Values) UnityEngine.Object.DestroyImmediate(c);
            if (hunter != null) hunter.gameObject.SetActive(true);
            ShaderUtil.allowAsyncCompilation = asyncWas;
        }
        File.WriteAllText(Path.Combine(outDir, "stream_summary.txt"), string.Join("\n", log) + "\n");
        Debug.Log("[PrintQ1b room captures] " + log.Count + " shots to " + outDir);
    }
}

