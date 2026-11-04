using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// World text follows the language (Red, 2026-11-03: "所有的diegetic narratives 需要adapt 语言的变化";
/// research/narrative/95_world_text_localization.md, frame A "a dubbed film"; words are 叙事设计's, art and type 平面视觉's,
/// textures and swapping 游戏视觉's).
///
/// A language's copy of a texture with words in it sits at the same path under Resources/WorldText/&lt;code&gt;/ as its English
/// base under Resources/ (the base Surfaces/Textures/Prop_PlateBusinessHours_A → its 简体中文 copy at
/// Resources/WorldText/zh-Hans/Surfaces/Textures/Prop_PlateBusinessHours_A.png). The editor import
/// (Editor/Rendering/FrontRoomsWorldTextImport) gives every copy its base's import settings and lists it in
/// Resources/WorldText/manifest.json. With no copies there is no manifest and nothing here runs.
///
/// Two kinds, decided by the base's import settings:
///   swap  the base is not readable: materials, kits, property blocks and EXAMINE draw it. While a language other than
///         English is in force the base itself holds its copy's pixels (Graphics.CopyTexture on the GPU: same size, format
///         and mips), so every renderer shows the language whoever built it and whenever. Back to English, or to a language
///         without a copy, the base is unloaded and reloads its own pixels from disk on its next use
///         (Resources.UnloadAsset). No asset is ever written, in the Editor or in a player, and the Editor's play mode ends
///         back in English.
///   own   the base is readable: code reads its pixels on the CPU (the stair signs' plate art and glyph atlases). That code
///         loads the language's copy itself with Texture() (any size) and its layout file with Text(), and rebuilds on
///         Applied.
/// The language is read from FrontRoomsLanguage.Current once a frame (its start-up Load raises no event) and applied in
/// that frame; then Applied is raised. WebGL lists English alone (FrontRoomsLanguage.Listed), so nothing here runs there.
/// Cost: none in English; in another language the swapped bases stay loaded (they are what the rooms show) and each copy is
/// loaded only for its copy, then unloaded.
/// </summary>
public static class FrontRoomsWorldText
{
    public const string Root = "WorldText/";
    public const string ManifestPath = Root + "manifest";
    public const string English = "en";

    [Serializable]
    public sealed class Language
    {
        public string code;
        /// <summary>Resources paths (no extension) of bases whose copy is swapped into them.</summary>
        public string[] swap;
        /// <summary>Resources paths of readable bases whose copy code loads itself (Texture).</summary>
        public string[] own;
        /// <summary>Resources paths of text assets (layouts) with a copy in this language (Text).</summary>
        public string[] texts;
    }

    [Serializable]
    public sealed class Manifest
    {
        public int version = 1;
        public Language[] languages;
    }

    sealed class Lookup
    {
        public Language language;
        public readonly HashSet<string> swap = new HashSet<string>(StringComparer.Ordinal);
        public readonly HashSet<string> own = new HashSet<string>(StringComparer.Ordinal);
        public readonly HashSet<string> texts = new HashSet<string>(StringComparer.Ordinal);
    }

    static bool read;
    static readonly Dictionary<string, Lookup> byCode = new Dictionary<string, Lookup>(StringComparer.Ordinal);
    // The bases that hold another language's pixels, by Resources path. The reference also keeps each one loaded:
    // Resources.UnloadUnusedAssets examines statics, so a base nobody draws right now never reloads its English pixels.
    static readonly Dictionary<string, Texture2D> held = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
    static readonly List<string> scratch = new List<string>();
    static readonly HashSet<string> warned = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>The language whose world text is on screen (English = every base as it is on disk).</summary>
    public static string AppliedCode { get; private set; } = English;

    /// <summary>Raised after the world text follows a new language: code that holds "own" copies or layouts rebuilds.</summary>
    public static event Action Applied;

    /// <summary>Bases swapped and copies refused by the last Apply (tools and the combined test read them).</summary>
    public static int Swapped { get; private set; }
    public static int Refused { get; private set; }

    /// <summary>A manifest exists: at least one copy in some language.</summary>
    public static bool HasCopies { get { Read(); return byCode.Count > 0; } }

    /// <summary>The language the game is in now (English when the language system has not started).</summary>
    public static string CurrentCode => FrontRoomsLanguage.Current != null ? FrontRoomsLanguage.Current.Code : English;

    static void Read()
    {
        if (read) return;
        read = true;
        byCode.Clear();
        var text = Resources.Load<TextAsset>(ManifestPath);
        if (text == null) return;
        Manifest m = null;
        try { m = JsonUtility.FromJson<Manifest>(text.text); }
        catch (Exception e) { Debug.LogWarning("[WorldText] manifest unreadable: " + e.Message); }
        Resources.UnloadAsset(text);
        if (m == null || m.languages == null) return;
        foreach (var l in m.languages)
        {
            if (l == null || string.IsNullOrEmpty(l.code) || l.code == English) continue;
            var k = new Lookup { language = l };
            if (l.swap != null) foreach (var p in l.swap) k.swap.Add(p);
            if (l.own != null) foreach (var p in l.own) k.own.Add(p);
            if (l.texts != null) foreach (var p in l.texts) k.texts.Add(p);
            byCode[l.code] = k;
        }
    }

    static Lookup For(string code)
    {
        if (string.IsNullOrEmpty(code) || code == English) return null;
        Read();
        return byCode.TryGetValue(code, out var k) ? k : null;
    }

    /// <summary>A readable texture for code that reads its pixels: the copy in the language on screen (AppliedCode, so own
    /// copies, layouts and swapped bases always agree) when it has one, else the base (Resources.Load). The caller owns what
    /// it loaded, as with Resources.Load.</summary>
    public static Texture2D Texture(string path)
    {
        var k = For(AppliedCode);
        if (k != null && k.own.Contains(path))
        {
            var copy = Resources.Load<Texture2D>(Root + k.language.code + "/" + path);
            if (copy != null) return copy;
            Warn(path + " (" + k.language.code + ") is listed but missing");
        }
        return Resources.Load<Texture2D>(path);
    }

    /// <summary>A layout or text file: the copy in the language on screen (AppliedCode) when it has one, else the base
    /// (Resources.Load).</summary>
    public static TextAsset Text(string path)
    {
        var k = For(AppliedCode);
        if (k != null && k.texts.Contains(path))
        {
            var copy = Resources.Load<TextAsset>(Root + k.language.code + "/" + path);
            if (copy != null) return copy;
            Warn(path + " (" + k.language.code + ") is listed but missing");
        }
        return Resources.Load<TextAsset>(path);
    }

    /// <summary>Follow the game's language now if it changed (the watcher calls this once a frame).</summary>
    public static void Follow()
    {
        var code = CurrentCode;
        if (code != AppliedCode) Apply(code);
    }

    /// <summary>Tools (renders, previews, the combined test): show a language's world text without changing the game's language.
    /// Restore() ends it.</summary>
    public static void Preview(string code) => Apply(string.IsNullOrEmpty(code) ? English : code);

    static void Apply(string code)
    {
        var k = For(code);
        Swapped = Refused = 0;
        // Bases the new language does not cover go back to their own pixels.
        scratch.Clear();
        foreach (var pair in held)
            if (k == null || !k.swap.Contains(pair.Key)) scratch.Add(pair.Key);
        foreach (var path in scratch)
        {
            var b = held[path];
            held.Remove(path);
            if (b != null) Resources.UnloadAsset(b);
        }
        if (k != null)
            foreach (var path in k.language.swap ?? Array.Empty<string>())
                if (Swap(path, k.language.code)) Swapped++; else Refused++;
        AppliedCode = k != null ? code : English;
        Applied?.Invoke();
    }

    static bool Swap(string path, string code)
    {
        var b = Resources.Load<Texture2D>(path);
        var v = Resources.Load<Texture2D>(Root + code + "/" + path);
        string why = null;
        if (b == null) why = "no base";
        else if (v == null) why = "no copy";
        else if (b.isReadable) why = "the base is readable (an own copy, loaded by its code)";
        else if (v.width != b.width || v.height != b.height || v.mipmapCount != b.mipmapCount || v.graphicsFormat != b.graphicsFormat)
            why = "the copy is " + v.width + "x" + v.height + " " + v.graphicsFormat + " mips " + v.mipmapCount + ", the base " + b.width + "x" + b.height + " " + b.graphicsFormat + " mips " + b.mipmapCount;
        else if ((SystemInfo.copyTextureSupport & UnityEngine.Rendering.CopyTextureSupport.Basic) == 0) why = "no CopyTexture on this GPU";
        if (why != null)
        {
            Warn(path + " (" + code + "): " + why + "; English stays");
            if (v != null) Resources.UnloadAsset(v);
            return false;
        }
        Graphics.CopyTexture(v, b);
        Resources.UnloadAsset(v);
        held[path] = b;
        return true;
    }

    /// <summary>Resources.UnloadAsset for code that loads a texture, copies it and drops it (the landing placards' composites):
    /// a base that holds a language's pixels stays loaded, since unloaded it would come back with its English pixels on its
    /// next load. Anything else is unloaded as asked.</summary>
    public static void Unload(UnityEngine.Object asset)
    {
        if (asset == null) return;
        if (asset is Texture2D t && held.ContainsValue(t)) return;
        Resources.UnloadAsset(asset);
    }

    /// <summary>Every base back to its own pixels (English), without raising Applied: the end of play mode, a domain reload,
    /// the end of a tool's Preview.</summary>
    public static void Restore()
    {
        foreach (var b in held.Values)
            if (b != null) Resources.UnloadAsset(b);
        held.Clear();
        AppliedCode = English;
    }

    static void Warn(string message)
    {
        if (warned.Add(message)) Debug.LogWarning("[WorldText] " + message);
    }

    // ---------------------------------------------------------------- the watcher

    sealed class Watcher : MonoBehaviour
    {
        void Update() => Follow();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        // Play mode without a domain reload keeps statics: start from the disk again (the last session restored its bases).
        read = false;
        byCode.Clear();
        held.Clear();
        warned.Clear();
        AppliedCode = English;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer || !HasCopies) return;
        var go = new GameObject("FrontRooms World Text") { hideFlags = HideFlags.HideInHierarchy };
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<Watcher>();
        Follow();
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    static void EditorHooks()
    {
        UnityEditor.EditorApplication.playModeStateChanged += s =>
        {
            if (s == UnityEditor.PlayModeStateChange.ExitingPlayMode) Restore();
        };
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += Restore;
    }
#endif
}
