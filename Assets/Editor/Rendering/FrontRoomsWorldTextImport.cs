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
///   per language, "swap" copies (the base is not readable), "own" copies (the base is readable: its code loads the copy)
///   and "texts" (json or txt layouts with a base of the same path). A copy without a base, or a swap copy whose source
///   size differs from its base's, is refused: logged and left out, so the game keeps English for it.
/// FrontRooms/World Text/Rebuild Manifest does the same from the menu and prints the list.
/// </summary>
public sealed class FrontRoomsWorldTextImport : AssetPostprocessor
{
    public const string Folder = "Assets/Resources/WorldText/";
    public const string ManifestFile = Folder + "manifest.json";
    const string ResourcesFolder = "Assets/Resources/";
    static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".tga", ".psd", ".tif", ".tiff", ".exr" };
    static readonly string[] TextExtensions = { ".json", ".txt", ".bytes" };
    static readonly string[] Platforms = { "Standalone", "iPhone", "WebGL", "Android" };

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

    /// <summary>The base of a copy: Assets/Resources/&lt;path&gt; with any of the extensions, or null.</summary>
    public static string BaseOf(string copyPath, string[] extensions)
    {
        if (!copyPath.StartsWith(Folder, StringComparison.Ordinal)) return null;
        var rest = copyPath.Substring(Folder.Length);
        var slash = rest.IndexOf('/');
        if (slash < 0) return null;
        var rel = rest.Substring(slash + 1);
        var stem = ResourcesFolder + rel.Substring(0, rel.Length - Path.GetExtension(rel).Length);
        foreach (var e in extensions)
            if (File.Exists(stem + e)) return stem + e;
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
            List<string> swap = new List<string>(), own = new List<string>(), texts = new List<string>();
            var files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.Ordinal);
            foreach (var file in files)
            {
                var path = file.Replace('\\', '/');
                var ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext == ".meta" || Path.GetFileName(path).StartsWith(".", StringComparison.Ordinal)) continue;
                var rel = path.Substring(dir.Length + 1).Replace('\\', '/');
                var key = rel.Substring(0, rel.Length - Path.GetExtension(rel).Length);
                if (Array.IndexOf(ImageExtensions, ext) >= 0)
                {
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
            }
            if (swap.Count + own.Count + texts.Count == 0) continue;
            languages.Add(new FrontRoomsWorldText.Language { code = code, swap = swap.ToArray(), own = own.ToArray(), texts = texts.ToArray() });
            report.Add(code + ": " + swap.Count + " swap, " + own.Count + " own, " + texts.Count + " texts");
            if (verbose)
            {
                foreach (var p in swap) report.Add("  swap " + p);
                foreach (var p in own) report.Add("  own  " + p);
                foreach (var p in texts) report.Add("  text " + p);
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
            if (line.StartsWith("refused", StringComparison.Ordinal)) Debug.LogWarning("[WorldText] " + line);
        return "[WorldText] " + string.Join("\n", report);
    }
}
