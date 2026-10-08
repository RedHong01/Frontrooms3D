using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The live wallpaper print (global _FR_Print, FrontRooms/Surface _FR_PRINT) and its static
/// frame 0 (_PrintTex), both built from the wallpaper chat's ENCODED slices
/// (Tools/print/patterns/out/hard_edge/encoded/K00..K07.png: 2048 x 2048 RGBA, linear,
/// R density and G cream already through print_encode, B 0, A 1, no mips).
///
/// FrontRooms > Rendering > Build Print Array writes two textures, then reimports them:
///   1. Assets/Resources/Surfaces/Textures/Wallpaper_Print_P.png = K00's R and G (B 0), the
///      static frame 0. Its GUID and .meta stay, so the three wallpaper materials keep it.
///   2. Assets/Resources/Print/FR_Print_HardEdge.png = the 8 slices as a 4 x 2 sheet. It
///      imports as a Texture2DArray (flipbook, slice k = column k % 4, row k / 4 from the
///      top); FrontRoomsPrintDriver loads it from Resources "Print/FR_Print_HardEdge".
/// Why a PNG sheet and not a saved .asset: the project serialises as Force Text, so a saved
/// 8 x 2048^2 array would be a 90 MB (BC5) to 180 MB (R8G8) hex YAML file, over GitHub's
/// 100 MB limit for R8G8, and a .asset has no platform tabs. The sheet is under 1 MB, and its
/// TextureImporter gives the array the same Standalone and WebGL tabs as _PrintTex.
///
/// Both textures: linear, repeat, trilinear, aniso 16, print mips (FrontRoomsPrintMips, the
/// cream-weighted density rule; every level of every slice, mip 0 included, comes from the
/// rule chain of the full-size source, so a reduced platform size is exactly the chain's
/// lower levels), and the same per-platform format and compressor quality
/// (ConfigurePlatforms), so the static frame and slice 0 decode to the same texels.
/// </summary>
public static class FrontRoomsPrintArray
{
    public const string SourceDir = "Tools/print/patterns/out/hard_edge/encoded";
    public const string LutPath = "Tools/lookdev/print_encode_lut.json";
    public const string Pattern = "hard_edge";
    public const int SliceCount = 8;
    public const int SliceSize = 2048;
    public const int Columns = 4;                       // sheet layout: 4 slices per row
    public const string PrintFolder = "Assets/Resources/Print";
    public const string SheetPath = PrintFolder + "/FR_Print_HardEdge.png";
    public const string ResourcesName = "Print/FR_Print_HardEdge";   // FrontRoomsPrintDriver.DefaultPrint
    public const string PrintTexPath = "Assets/Resources/Surfaces/Textures/Wallpaper_Print_P.png";

    // Desktop tier: the Standalone tab (Mac/Win players, and the editor on a desktop target).
    // Q1b gate 2026-10-03 (FrontRoomsPrintP0Test.RunQ1bBatch): BC5 against the uncompressed
    // R8G8 reference. If the gate fails, this is RG16 (R8G8) at the same size.
    public const TextureImporterFormat DesktopFormat = TextureImporterFormat.BC5;
    // WebGL tier, the WebGL tab only: R8G8 is core in WebGL2 (BC5 needs
    // EXT_texture_compression_rgtc, which mobile browsers lack), at half size per slice.
    public const TextureImporterFormat WebGLFormat = TextureImporterFormat.RG16;
    public const int WebGLSliceSize = SliceSize / 2;
    public const int CompressionQuality = 100;            // "Best" for both textures

    public static string[] SlicePaths() => Enumerable.Range(0, SliceCount).Select(i => SourceDir + "/K" + i.ToString("00") + ".png").ToArray();

    public static bool IsPrintSheet(string assetPath)
    {
        var p = assetPath.Replace('\\', '/');
        return p.StartsWith(PrintFolder + "/", StringComparison.Ordinal) && p.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            && Path.GetFileName(p).StartsWith("FR_Print_", StringComparison.Ordinal);
    }

    /// <summary>The print's platform tabs: Standalone = DesktopFormat up to desktopMax,
    /// WebGL = WebGLFormat up to webglMax. Shared by _PrintTex and the array sheet.</summary>
    public static void ConfigurePlatforms(TextureImporter importer, int desktopMax, int webglMax)
    {
        var standalone = importer.GetPlatformTextureSettings("Standalone");
        standalone.overridden = true;
        standalone.maxTextureSize = desktopMax;
        standalone.format = DesktopFormat;
        standalone.compressionQuality = CompressionQuality;
        importer.SetPlatformTextureSettings(standalone);
        var webgl = importer.GetPlatformTextureSettings("WebGL");
        webgl.overridden = true;
        webgl.maxTextureSize = webglMax;
        webgl.format = WebGLFormat;
        webgl.compressionQuality = CompressionQuality;
        importer.SetPlatformTextureSettings(webgl);
    }

    // ------------------------------------------------------------- helpers
    /// <summary>A PNG's pixels in Unity order (bottom-up), exact 8-bit values (no colour conversion).</summary>
    public static Color32[] LoadPng(string path, out int width, out int height)
    {
        var t = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
        try
        {
            if (!t.LoadImage(File.ReadAllBytes(path), false)) throw new IOException("Cannot decode " + path);
            width = t.width; height = t.height;
            return t.GetPixels32();
        }
        finally { UnityEngine.Object.DestroyImmediate(t); }
    }

    /// <summary>Width and height from a PNG's IHDR (no decode).</summary>
    public static bool PngSize(string path, out int width, out int height)
    {
        width = height = 0;
        try
        {
            using (var f = File.OpenRead(path))
            {
                var b = new byte[24];
                if (f.Read(b, 0, 24) != 24 || b[1] != 'P' || b[2] != 'N' || b[3] != 'G') return false;
                width = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19];
                height = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
                return width > 0 && height > 0;
            }
        }
        catch { return false; }
    }

    public static Color[] ToColors(Color32[] c)
    {
        var o = new Color[c.Length];
        for (var i = 0; i < c.Length; i++) o[i] = c[i];
        return o;
    }

    public static int MipCount(int w, int h) => 1 + (int)Math.Floor(Math.Log(Math.Max(w, h), 2));

    /// <summary>Levels to drop so the top level is at most maxSize (power-of-two steps).</summary>
    public static int Skip(int w, int maxSize)
    {
        var s = 0;
        while ((w >> s) > maxSize && (w >> s) > 1) s++;
        return s;
    }

    /// <summary>The full print-mip chain (FrontRoomsPrintMips.Build, the rule ApplySlice uses) from mip 0.</summary>
    public static List<Color[]> Chain(Color32[] mip0, int w, int h) => FrontRoomsPrintMips.Build(ToColors(mip0), w, h, MipCount(w, h));

    /// <summary>Slice k of a sheet (bottom-up pixels): column k % cols, row k / cols counted from the TOP.</summary>
    public static Color32[] SliceOf(Color32[] sheet, int sheetW, int sheetH, int size, int k)
    {
        int rows = sheetH / size, col = k % Columns, rowFromTop = k / Columns;
        int x0 = col * size, y0 = (rows - 1 - rowFromTop) * size;
        var o = new Color32[size * size];
        for (var y = 0; y < size; y++) Array.Copy(sheet, (y0 + y) * sheetW + x0, o, y * size, size);
        return o;
    }

    public static string Sha1(string path)
    {
        using (var sha = SHA1.Create())
            return string.Concat(sha.ComputeHash(File.ReadAllBytes(path)).Select(b => b.ToString("x2")));
    }

    // ------------------------------------------------------- import rules
    /// <summary>Import rules for the sheet: a linear Texture2DArray flipbook (4 columns), print
    /// tabs per slice (desktop full size, WebGL half).</summary>
    internal static void ConfigureSheet(TextureImporter importer, string assetPath)
    {
        importer.textureType = TextureImporterType.Default;
        importer.textureShape = TextureImporterShape.Texture2DArray;
        importer.sRGBTexture = false;
        importer.mipmapEnabled = true;
        importer.streamingMipmaps = false;
        importer.isReadable = false;
        importer.filterMode = FilterMode.Trilinear;
        importer.anisoLevel = 16;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.alphaSource = TextureImporterAlphaSource.None;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        int w = Columns * SliceSize, h = 2 * SliceSize;
        if (PngSize(assetPath, out var pw, out var ph)) { w = pw; h = ph; }
        var size = Math.Max(1, w / Columns);
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.flipbookColumns = Columns;
        settings.flipbookRows = Math.Max(1, h / size);
        importer.SetTextureSettings(settings);
        importer.maxTextureSize = 16384;
        // For a flipbook array the max size applies to each SLICE, not to the sheet (measured by
        // the Q1b reduced-import check: a 4 x 2 sheet of 2048 slices with max 4096 keeps 2048).
        ConfigurePlatforms(importer, size, Math.Max(1, size * WebGLSliceSize / SliceSize));
    }

    /// <summary>Every level of every slice from the full-size source's rule chain (mip 0 included),
    /// so a reduced tab (WebGL) gets exactly the chain's lower levels.</summary>
    internal static void ApplySheetMips(Texture2DArray array, string assetPath)
    {
        var sheet = LoadPng(assetPath, out var sw, out var sh);
        var size = sw / Columns;
        var skip = Skip(size, array.width);
        if ((size >> skip) != array.width || array.depth != (sh / size) * Columns)
        {
            Debug.LogError("[PrintArray] " + assetPath + ": imported " + array.width + "x" + array.height + " x" + array.depth
                + " does not match the " + sw + "x" + sh + " sheet of " + size + " px slices; print mips not applied.");
            return;
        }
        for (var k = 0; k < array.depth; k++)
        {
            var chain = Chain(SliceOf(sheet, sw, sh, size, k), size, size);
            for (var m = 0; m < array.mipmapCount && m + skip < chain.Count; m++) array.SetPixels(chain[m + skip], k, m);
        }
    }

    // ------------------------------------------------------------- the menu
    [Serializable] public sealed class SourceSlice { public string file; public string sha1; }
    [Serializable]
    public sealed class Provenance
    {
        public string about;
        public string pattern;
        public float[] tileMetres;
        public int sliceSize;
        public string lut;
        public string lutSha1;
        public SourceSlice[] slices;
    }

    [MenuItem("FrontRooms/Rendering/Build Print Array")]
    public static void BuildMenu()
    {
        string report;
        try { report = Build(); }
        catch (Exception e) { report = "FAILED: " + e.Message; Debug.LogException(e); }
        Debug.Log("[PrintArray] " + report);
        if (!Application.isBatchMode) EditorUtility.DisplayDialog("Build Print Array", report, "OK");
    }

    /// <summary>Batch entry: -executeMethod FrontRoomsPrintArray.BuildBatch.</summary>
    public static void BuildBatch() => Debug.Log("[PrintArray] " + Build());

    public static string Build()
    {
        var paths = SlicePaths();
        foreach (var p in paths)
            if (!File.Exists(p)) throw new FileNotFoundException("Missing print slice (run Tools/print/print_tool.py build-print hard_edge --size 2048x2048)", p);
        var sb = new StringBuilder();
        var slices = new List<Color32[]>();
        foreach (var p in paths)
        {
            var px = LoadPng(p, out var w, out var h);
            if (w != SliceSize || h != SliceSize) throw new InvalidOperationException(p + " is " + w + "x" + h + ", expected " + SliceSize + "^2");
            slices.Add(px);
        }

        // 1. Static frame 0: K00's R and G, B = 0 (reserved), no alpha. Written only when the
        // texels change, so the .meta (GUID, import rules) stays and the materials keep the link.
        var frame0 = new Color32[SliceSize * SliceSize];
        for (var i = 0; i < frame0.Length; i++) frame0[i] = new Color32(slices[0][i].r, slices[0][i].g, 0, 255);
        sb.AppendLine(WriteRgbPng(PrintTexPath, frame0, SliceSize, SliceSize) + " " + PrintTexPath + " (" + SliceSize + "^2, K00 R/G)");

        // 2. The sheet: slice k at column k % 4, row k / 4 from the top (Unity rows run bottom-up).
        var rows = (SliceCount + Columns - 1) / Columns;
        int sw = Columns * SliceSize, sh = rows * SliceSize;
        var sheet = new Color32[sw * sh];
        for (var k = 0; k < SliceCount; k++)
        {
            int x0 = (k % Columns) * SliceSize, y0 = (rows - 1 - k / Columns) * SliceSize;
            for (var y = 0; y < SliceSize; y++)
                for (var x = 0; x < SliceSize; x++)
                {
                    var c = slices[k][y * SliceSize + x];
                    sheet[(y0 + y) * sw + x0 + x] = new Color32(c.r, c.g, 0, 255);
                }
        }
        if (!AssetDatabase.IsValidFolder(PrintFolder)) AssetDatabase.CreateFolder("Assets/Resources", "Print");
        sb.AppendLine(WriteRgbPng(SheetPath, sheet, sw, sh) + " " + SheetPath + " (" + sw + "x" + sh + ", " + SliceCount + " slices, 4 x " + rows + ")");

        AssetDatabase.ImportAsset(PrintTexPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(SheetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

        // Provenance in the sheet importer's userData (the .meta), not in a shipped file.
        var prov = new Provenance
        {
            about = "FrontRooms live wallpaper print (_FR_Print). Built by FrontRooms > Rendering > Build Print Array "
                + "(Assets/Editor/Rendering/FrontRoomsPrintArray.cs) from the encoded slices below; do not edit the PNG by hand.",
            pattern = Pattern,
            tileMetres = new[] { .75f, 1.125f },
            sliceSize = SliceSize,
            lut = LutPath,
            lutSha1 = File.Exists(LutPath) ? Sha1(LutPath) : "missing",
            slices = paths.Select(p => new SourceSlice { file = p, sha1 = Sha1(p) }).ToArray(),
        };
        var importer = (TextureImporter)AssetImporter.GetAtPath(SheetPath);
        var userData = JsonUtility.ToJson(prov);
        if (importer != null && importer.userData != userData) { importer.userData = userData; importer.SaveAndReimport(); }

        var print = AssetDatabase.LoadAssetAtPath<Texture2D>(PrintTexPath);
        var array = Resources.Load<Texture2DArray>(ResourcesName);
        sb.AppendLine("_PrintTex: " + (print != null ? print.width + "x" + print.height + " " + print.graphicsFormat + " mips " + print.mipmapCount : "MISSING"));
        sb.AppendLine("_FR_Print: " + (array != null ? array.width + "x" + array.height + " x" + array.depth + " " + array.graphicsFormat + " mips " + array.mipmapCount + " readable " + array.isReadable : "MISSING (see the console)"));
        sb.AppendLine("LUT " + prov.lutSha1 + "; target " + EditorUserBuildSettings.activeBuildTarget);
        return sb.ToString().TrimEnd();
    }

    /// <summary>Writes an RGB PNG unless the file already holds the same R/G/B texels; returns "wrote" or "kept".</summary>
    static string WriteRgbPng(string path, Color32[] px, int w, int h)
    {
        if (File.Exists(path) && SameTexels(path, px, w, h)) return "kept";
        var t = new Texture2D(w, h, TextureFormat.RGB24, false, true);
        try
        {
            t.SetPixels32(px);
            t.Apply(false);
            File.WriteAllBytes(path, t.EncodeToPNG());
        }
        finally { UnityEngine.Object.DestroyImmediate(t); }
        return "wrote";
    }

    static bool SameTexels(string path, Color32[] c, int w, int h)
    {
        try
        {
            var px = LoadPng(path, out var pw, out var ph);
            if (pw != w || ph != h) return false;
            for (var i = 0; i < c.Length; i++)
                if (px[i].r != c[i].r || px[i].g != c[i].g || px[i].b != c[i].b) return false;
            return true;
        }
        catch { return false; }
    }
}

/// <summary>Import rules for Assets/Resources/Print/FR_Print_*.png (see FrontRoomsPrintArray).</summary>
public sealed class FrontRoomsPrintArrayImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (FrontRoomsPrintArray.IsPrintSheet(assetPath)) FrontRoomsPrintArray.ConfigureSheet((TextureImporter)assetImporter, assetPath);
    }

    void OnPostprocessTexture2DArray(Texture2DArray texture)
    {
        if (FrontRoomsPrintArray.IsPrintSheet(assetPath)) FrontRoomsPrintArray.ApplySheetMips(texture, assetPath);
    }
}
