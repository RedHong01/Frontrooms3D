using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The import side of FrontRoomsWorldText (world text follows the language, Red 2026-11-03;
/// research/narrative/95_world_text_localization.md). A language's copy of a texture lives at
/// Assets/Resources/WorldText/&lt;code&gt;/&lt;the base's path under Resources&gt;, any image extension:
/// - On import the copy takes its base's import settings (texture settings, size, compression, every platform's override),
///   so a swapped copy has the base's size, format and mips on every target.
/// - After any import, move or delete under WorldText the manifest (Assets/Resources/WorldText/manifest.json) is rebuilt:
///   per language, "swap" copies (the base is not readable), "patch" copies (&lt;path&gt;~patch.json with its atlases
///   &lt;path&gt;~patch0, ~patch1 ... and its tail ~patchtail: only the cells that differ, for a big non-readable sheet), "own" copies (the base is
///   readable: its code loads the copy), "texts" (json or txt layouts with a base of the same path) and "models" (an .fbx copy of
///   a kit model under Props/Models, with its sidecar: the EXIT sign's legend is geometry; FrontRoomsKitImporter imports it
///   as its base). A copy without a
///   base, a swap copy whose source size differs from its base's, or a patch whose plan does not fit its base or misses an
///   atlas, is refused: logged and left out, so the game keeps English for it. Where a path has both a full copy and a
///   patch, the patch is used and the full copy is reported (it only adds to the build: delete it).
/// - A piece (&lt;path&gt;~piece0, ~piece1 ...: a language-only texture that a layout names, FrontRoomsWorldText.Piece, such
///   as the landing placards' extra token pieces) takes &lt;path&gt;'s import settings and is not listed; one with no image
///   at Resources/&lt;path&gt; is refused (reported).
/// FrontRooms/World Text/Rebuild Manifest does the same from the menu and prints the list.
/// Runs after the other texture importers (FrontRoomsSurfaceTextureImporter configures everything under
/// Resources/Surfaces/Textures/ by its name, copies included, and an atlas's name is not its base's), so a copy always ends
/// with its base's settings.
/// </summary>
public sealed class FrontRoomsWorldTextImport : AssetPostprocessor
{
    public const string Folder = "Assets/Resources/WorldText/";
    public const string ManifestFile = Folder + "manifest.json";
    const string ResourcesFolder = "Assets/Resources/";
    static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".tga", ".psd", ".tif", ".tiff", ".exr" };
    static readonly string[] TextExtensions = { ".json", ".txt", ".bytes" };
    static readonly string[] ModelExtensions = { ".fbx" };
    static readonly string[] Platforms = { "Standalone", "iPhone", "WebGL", "Android" };

    public override int GetPostprocessOrder() => 1000;

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Folder, StringComparison.Ordinal)) return;
        var basePath = BaseOf(assetPath, ImageExtensions);
        var baseImporter = basePath != null ? AssetImporter.GetAtPath(basePath) as TextureImporter : null;
        if (baseImporter == null) return;
        var importer = (TextureImporter)assetImporter;
        var settings = new TextureImporterSettings();
        baseImporter.ReadTextureSettings(settings);
        importer.SetTextureSettings(settings);
        importer.maxTextureSize = baseImporter.maxTextureSize;
        importer.textureCompression = baseImporter.textureCompression;
        importer.compressionQuality = baseImporter.compressionQuality;
        importer.crunchedCompression = baseImporter.crunchedCompression;
        foreach (var platform in Platforms)
        {
            var p = baseImporter.GetPlatformTextureSettings(platform);
            p.name = platform;
            importer.SetPlatformTextureSettings(p);
        }
    }

    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        if (Touches(imported) || Touches(deleted) || Touches(moved) || Touches(movedFrom)) Rebuild(false);
    }

    static bool Touches(string[] paths)
    {
        foreach (var p in paths)
            if (p.StartsWith(Folder, StringComparison.Ordinal) && p != ManifestFile && !p.EndsWith(".meta", StringComparison.Ordinal)) return true;
        return false;
    }

    [MenuItem("FrontRooms/World Text/Rebuild Manifest")]
    static void RebuildFromMenu() => Debug.Log(Rebuild(true));

    /// <summary>The base of a copy (a patch's plan or atlas included): Assets/Resources/&lt;path&gt; with any of the extensions,
    /// or null.</summary>
    public static string BaseOf(string copyPath, string[] extensions)
    {
        if (!copyPath.StartsWith(Folder, StringComparison.Ordinal)) return null;
        var rest = copyPath.Substring(Folder.Length);
        var slash = rest.IndexOf('/');
        if (slash < 0) return null;
        var rel = rest.Substring(slash + 1);
        var stem = ResourcesFolder + PatchKey(rel.Substring(0, rel.Length - Path.GetExtension(rel).Length), out _);
        foreach (var e in extensions)
            if (File.Exists(stem + e)) return stem + e;
        return null;
    }

    /// <summary>A copy's path without a patch or piece suffix; part = the atlas number, -1 for the plan, -3 for the tail,
    /// -4 for a piece (&lt;path&gt;~piece&lt;n&gt;), -2 for a file that is no part of a patch.</summary>
    public static string PatchKey(string stem, out int part)
    {
        part = -2;
        var slash = stem.LastIndexOf('/');
        var piece = stem.LastIndexOf(FrontRoomsWorldText.PieceSuffix, StringComparison.Ordinal);
        if (piece >= 0 && piece > slash)
        {
            var number = stem.Substring(piece + FrontRoomsWorldText.PieceSuffix.Length);
            if (number.Length > 0 && number.Length <= 3 && int.TryParse(number, out var k) && k.ToString() == number)
            {
                part = -4;
                return stem.Substring(0, piece);
            }
        }
        var cut = stem.LastIndexOf(FrontRoomsWorldText.PatchSuffix, StringComparison.Ordinal);
        if (cut < 0 || cut < slash) return stem;
        var rest = stem.Substring(cut + FrontRoomsWorldText.PatchSuffix.Length);
        if (rest.Length == 0) part = -1;
        else if (rest == "tail") part = -3;
        else if (rest.Length <= 3 && int.TryParse(rest, out var n) && n.ToString() == rest) part = n;
        else return stem;
        return stem.Substring(0, cut);
    }

    /// <summary>The image at Assets/Resources/... stem with any image extension, or null.</summary>
    static string ImageAt(string stem)
    {
        foreach (var e in ImageExtensions)
            if (File.Exists(stem + e)) return stem + e;
        return null;
    }

    /// <summary>Why a patch's plan does not fit (null when it does): base not readable, its size, every atlas there.</summary>
    static string CheckPatch(string planPath, string dir, string key, TextureImporter baseImporter)
    {
        if (baseImporter.isReadable) return "its base is readable (code reads its pixels: give it a full copy)";
        FrontRoomsWorldText.PatchPlan plan = null;
        try { plan = JsonUtility.FromJson<FrontRoomsWorldText.PatchPlan>(File.ReadAllText(planPath)); }
        catch (Exception e) { return "unreadable plan (" + e.Message + ")"; }
        if (plan == null || plan.rects == null || plan.rects.Length == 0 || plan.rects.Length % 7 != 0 || plan.atlases < 1) return "an empty or broken plan";
        baseImporter.GetSourceTextureWidthAndHeight(out var bw, out var bh);
        if (bw != plan.width || bh != plan.height) return "the plan is for " + plan.width + "x" + plan.height + ", its base is " + bw + "x" + bh;
        for (var i = 0; i < plan.atlases; i++)
            if (ImageAt(dir + "/" + key + FrontRoomsWorldText.PatchSuffix + i) == null) return "atlas " + i + " is missing";
        if (plan.tail > 0 && ImageAt(dir + "/" + key + FrontRoomsWorldText.PatchSuffix + "tail") == null) return "its tail is missing";
        return null;
    }

    /// <summary>Lists every copy by language and kind; writes the manifest only when it changed. Returns a report.</summary>
    public static string Rebuild(bool verbose)
    {
        if (!Directory.Exists(Folder)) return "[WorldText] no " + Folder + ": nothing to list";
        var manifest = new FrontRoomsWorldText.Manifest();
        var languages = new List<FrontRoomsWorldText.Language>();
        var report = new List<string>();
        var dirs = Directory.GetDirectories(Folder);
        Array.Sort(dirs, StringComparer.Ordinal);
        foreach (var dir in dirs)
        {
            var code = Path.GetFileName(dir);
            if (code == FrontRoomsWorldText.English || FrontRoomsLanguage.Find(code) == null)
            {
                report.Add("refused folder " + code + ": not a language of FrontRoomsLanguage.All (or English)");
                continue;
            }
            List<string> swap = new List<string>(), own = new List<string>(), texts = new List<string>(), patch = new List<string>(), pieces = new List<string>(), models = new List<string>();
            var files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.Ordinal);
            // Patches first: a path with a working patch uses it, and a full copy beside it only adds to the build.
            foreach (var file in files)
            {
                var path = file.Replace('\\', '/');
                if (Path.GetExtension(path).ToLowerInvariant() != ".json") continue;
                var rel = path.Substring(dir.Length + 1);
                var key = PatchKey(rel.Substring(0, rel.Length - 5), out var part);
                if (part != -1) continue;
                var planBase = BaseOf(path, ImageExtensions);
                var planImporter = planBase != null ? AssetImporter.GetAtPath(planBase) as TextureImporter : null;
                var why = planImporter == null ? "no base image at Resources/" + key : CheckPatch(path, dir, key, planImporter);
                if (why != null) report.Add("refused " + code + "/" + rel + ": " + why);
                else patch.Add(key);
            }
            var planned = new HashSet<string>(patch, StringComparer.Ordinal);
            foreach (var file in files)
            {
                var path = file.Replace('\\', '/');
                var ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext == ".meta" || Path.GetFileName(path).StartsWith(".", StringComparison.Ordinal)) continue;
                var rel = path.Substring(dir.Length + 1).Replace('\\', '/');
                var key = PatchKey(rel.Substring(0, rel.Length - Path.GetExtension(rel).Length), out var part);
                if (part == -4)
                {
                    // a piece: imported with its base's settings, named by a layout, never listed
                    if (Array.IndexOf(ImageExtensions, ext) >= 0 && BaseOf(path, ImageExtensions) == null)
                        report.Add("refused " + code + "/" + rel + ": a piece with no image at Resources/" + key + " (it takes that texture's import settings)");
                    else pieces.Add(rel);
                    continue;
                }
                if (part != -2) continue;   // a plan, an atlas or a tail: listed above
                if (Array.IndexOf(ImageExtensions, ext) >= 0)
                {
                    if (planned.Contains(key)) { report.Add("superseded " + code + "/" + rel + ": its patch is used, delete this full copy"); continue; }
                    var basePath = BaseOf(path, ImageExtensions);
                    var baseImporter = basePath != null ? AssetImporter.GetAtPath(basePath) as TextureImporter : null;
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (baseImporter == null || importer == null) { report.Add("refused " + code + "/" + rel + ": no base at Resources/" + key); continue; }
                    if (baseImporter.isReadable) { own.Add(key); continue; }
                    baseImporter.GetSourceTextureWidthAndHeight(out var bw, out var bh);
                    importer.GetSourceTextureWidthAndHeight(out var cw, out var ch);
                    if (bw != cw || bh != ch) { report.Add("refused " + code + "/" + rel + ": " + cw + "x" + ch + ", its base is " + bw + "x" + bh + " (a swapped copy keeps the base's size)"); continue; }
                    swap.Add(key);
                }
                else if (Array.IndexOf(TextExtensions, ext) >= 0)
                {
                    if (BaseOf(path, TextExtensions) == null) { report.Add("refused " + code + "/" + rel + ": no base text at Resources/" + key); continue; }
                    texts.Add(key);
                }
                else if (Array.IndexOf(ModelExtensions, ext) >= 0)
                {
                    if (BaseOf(path, ModelExtensions) == null) { report.Add("refused " + code + "/" + rel + ": no base model at Resources/" + key); continue; }
                    models.Add(key);
                }
            }
            if (swap.Count + own.Count + texts.Count + patch.Count + models.Count == 0) continue;
            languages.Add(new FrontRoomsWorldText.Language { code = code, swap = swap.ToArray(), own = own.ToArray(), texts = texts.ToArray(), patch = patch.ToArray(), models = models.ToArray() });
            report.Add(code + ": " + swap.Count + " swap, " + patch.Count + " patch, " + own.Count + " own, " + texts.Count + " texts, " + models.Count + " models");
            if (verbose)
            {
                foreach (var p in swap) report.Add("  swap " + p);
                foreach (var p in patch) report.Add("  patch " + p);
                foreach (var p in own) report.Add("  own  " + p);
                foreach (var p in texts) report.Add("  text " + p);
                foreach (var p in models) report.Add("  model " + p);
                foreach (var p in pieces) report.Add("  piece " + p + " (not listed: named by a layout)");
            }
        }
        manifest.languages = languages.ToArray();
        var json = JsonUtility.ToJson(manifest, true).Replace("\r\n", "\n") + "\n";
        var old = File.Exists(ManifestFile) ? File.ReadAllText(ManifestFile) : null;
        if (old != json)
        {
            File.WriteAllText(ManifestFile, json);
            AssetDatabase.ImportAsset(ManifestFile);
            report.Add("manifest written");
        }
        foreach (var line in report)
            if (line.StartsWith("refused", StringComparison.Ordinal) || line.StartsWith("superseded", StringComparison.Ordinal)) Debug.LogWarning("[WorldText] " + line);
        return "[WorldText] " + string.Join("\n", report);
    }
}
