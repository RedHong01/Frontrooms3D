using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Import rules for the pool water's ripple loops (research/bathroom_rooms/70_look_build.md, "Ripples"): the sheets
/// Assets/Resources/Rendering/Water/FR_Ripple_{Fine,FinePhone,Long}.png that Tools/lookdev/pool_water/
/// gen_pool_ripples.py writes import as Texture2DArrays (flipbook, slice k = column k % cols, row k / cols from the
/// top), which FrontRoomsWaterRipples loads from Resources "Rendering/Water/FR_Ripple_*". As FrontRoomsPrintArray: a
/// PNG sheet, not a saved .asset (Force Text would make a 64 x 512^2 array a hex YAML file of hundreds of MB), with
/// platform tabs.
///
/// The rules: linear, two channels of slope (R dh/dx, G dh/dz), box mips (a mip texel holds the mean slope of the
/// texels under it: the footprint table in FR_Ripple_Stats.json assumes exactly that), trilinear, aniso 16, repeat,
/// no global mip limit. Formats per slice: Standalone BC5 (1 byte a texel), iOS and Android EAC_RG (two 11-bit
/// channels, 1 byte a texel), WebGL RG16 (R8G8 is core in WebGL2; BC5 needs an extension mobile browsers lack). A sheet
/// another tier uses is a 32 px stub on that platform, so no tier carries another tier's loop: desktop 21.3 MiB (Fine)
/// + 2.7 (Long); phones 2.7 (FinePhone) + 2.7 (Long); WebGL 5.3 (FinePhone, RG16) + 1.3 (Long at 64 px).
///
/// The generator writes these rules into each sheet's .meta, so an import needs nothing else. This is not an
/// AssetPostprocessor on purpose: adding a texture postprocessor makes Unity reimport every texture in the project
/// (measured in the clone: 1,262 texture imports), which would stall Red's Editor for nothing. Instead:
///   FrontRooms > Rendering > Ripple Arrays: Apply Import Rules   writes the rules into the three importers, reimports
///   Check()                                                      lists any difference (the harness and the gate call it)
/// </summary>
public static class FrontRoomsRippleArray
{
    public const string Folder = "Assets/Resources/Rendering/Water";
    public const int Stub = 32;

    sealed class Rule
    {
        public string name;
        public int slice, cols, standalone, phone, webgl;
    }

    static readonly Rule[] Rules =
    {
        new Rule { name = "FR_Ripple_Fine", slice = 512, cols = 8, standalone = 512, phone = Stub, webgl = Stub },
        new Rule { name = "FR_Ripple_FinePhone", slice = 256, cols = 8, standalone = Stub, phone = 256, webgl = 256 },
        new Rule { name = "FR_Ripple_Long", slice = 128, cols = 16, standalone = 128, phone = 128, webgl = 64 },
    };

    static readonly string[] Targets = { "Standalone", "iPhone", "Android", "WebGL" };

    static TextureImporterFormat FormatFor(string target)
    {
        if (target == "Standalone") return TextureImporterFormat.BC5;
        if (target == "WebGL") return TextureImporterFormat.RG16;
        return TextureImporterFormat.EAC_RG;
    }

    static int SizeFor(Rule r, string target)
    {
        if (target == "Standalone") return r.standalone;
        if (target == "WebGL") return r.webgl;
        return r.phone;
    }

    static string PathOf(Rule r) { return Folder + "/" + r.name + ".png"; }

    /// <summary>Every difference between the three importers and the rules (empty when they match).</summary>
    public static List<string> Check()
    {
        var o = new List<string>();
        foreach (var r in Rules)
        {
            var p = PathOf(r);
            var imp = AssetImporter.GetAtPath(p) as TextureImporter;
            if (imp == null) { o.Add(p + ": no TextureImporter (run Tools/lookdev/pool_water/gen_pool_ripples.py)"); continue; }
            if (imp.textureType != TextureImporterType.Default) o.Add(p + ": textureType " + imp.textureType);
            if (imp.textureShape != TextureImporterShape.Texture2DArray) o.Add(p + ": shape " + imp.textureShape);
            if (imp.sRGBTexture) o.Add(p + ": sRGB on");
            if (!imp.mipmapEnabled || imp.mipmapFilter != TextureImporterMipFilter.BoxFilter) o.Add(p + ": mips " + imp.mipmapEnabled + " " + imp.mipmapFilter);
            if (imp.filterMode != FilterMode.Trilinear || imp.anisoLevel != 16 || imp.wrapMode != TextureWrapMode.Repeat)
                o.Add(p + ": sampling " + imp.filterMode + " aniso " + imp.anisoLevel + " " + imp.wrapMode);
            if (!imp.ignoreMipmapLimit) o.Add(p + ": follows the global mip limit");
            var s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            if (s.flipbookColumns != r.cols) o.Add(p + ": flipbook columns " + s.flipbookColumns);
            foreach (var t in Targets)
            {
                var ps = imp.GetPlatformTextureSettings(t);
                if (!ps.overridden || ps.format != FormatFor(t) || ps.maxTextureSize != SizeFor(r, t))
                    o.Add(p + ": " + t + " " + (ps.overridden ? ps.format + " " + ps.maxTextureSize : "not overridden"));
            }
        }
        return o;
    }

    [MenuItem("FrontRooms/Rendering/Ripple Arrays: Apply Import Rules")]
    public static void Apply()
    {
        foreach (var r in Rules)
        {
            var p = PathOf(r);
            var imp = AssetImporter.GetAtPath(p) as TextureImporter;
            if (imp == null) { Debug.LogWarning("[RippleArray] missing " + p + " (run Tools/lookdev/pool_water/gen_pool_ripples.py)"); continue; }
            imp.textureType = TextureImporterType.Default;
            imp.textureShape = TextureImporterShape.Texture2DArray;
            imp.sRGBTexture = false;
            imp.mipmapEnabled = true;
            imp.mipmapFilter = TextureImporterMipFilter.BoxFilter;
            imp.mipMapsPreserveCoverage = false;
            imp.fadeout = false;
            imp.streamingMipmaps = false;
            imp.isReadable = false;
            imp.filterMode = FilterMode.Trilinear;
            imp.anisoLevel = 16;
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.alphaSource = TextureImporterAlphaSource.None;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            imp.compressionQuality = 100;
            imp.ignoreMipmapLimit = true;
            int w = r.cols * r.slice, h = r.slice;
            if (PngSize(p, out var pw, out var ph)) { w = pw; h = ph; }
            var size = Math.Max(1, w / r.cols);
            if (size != r.slice) Debug.LogError("[RippleArray] " + p + ": " + size + " px a slice, expected " + r.slice + " (re-run gen_pool_ripples.py)");
            var s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            s.flipbookColumns = r.cols;
            s.flipbookRows = Math.Max(1, h / size);
            imp.SetTextureSettings(s);
            imp.maxTextureSize = 16384;
            // for a flipbook array the max size applies to each slice (FrontRoomsPrintArray's Q1b check)
            foreach (var t in Targets)
            {
                var ps = imp.GetPlatformTextureSettings(t);
                ps.overridden = true;
                ps.maxTextureSize = SizeFor(r, t);
                ps.format = FormatFor(t);
                ps.compressionQuality = 100;
                ps.textureCompression = TextureImporterCompression.CompressedHQ;
                imp.SetPlatformTextureSettings(ps);
            }
            imp.SaveAndReimport();
        }
        var left = Check();
        if (left.Count == 0) Debug.Log("[RippleArray] import rules applied: 3 sheets match");
        else foreach (var l in left) Debug.LogError("[RippleArray] " + l);
    }

    static bool PngSize(string path, out int width, out int height)
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
        catch (IOException) { return false; }
    }
}
