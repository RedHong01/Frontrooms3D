using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

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
/// Three kinds, decided by the base's import settings and the copy's form:
///   swap  the base is not readable: materials, kits, property blocks and EXAMINE draw it. While a language other than
///         English is in force the base itself holds its copy's pixels (Graphics.CopyTexture on the GPU: same size, format
///         and mips), so every renderer shows the language whoever built it and whenever. Back to English, or to a language
///         without a copy, the base is unloaded and reloads its own pixels from disk on its next use
///         (Resources.UnloadAsset). No asset is ever written, in the Editor or in a player, and the Editor's play mode ends
///         back in English.
///   patch a swap for a big sheet whose words cover part of it (the evacuation plans: a full copy per language would add
///         about 54 MB to the build for 简体中文 alone). The copy holds only the grid cells that differ from English (128
///         texels where every platform uses 4 x 4 blocks, 192 where one uses ASTC 6 x 6), packed into power-of-two atlases
///         &lt;path&gt;~patch0.png, ~patch1.png ... beside their places in &lt;path&gt;~patch.json; Tools/lookdev/pack_world_text_patch.py
///         writes both and proves the base with the cells pasted is the language's sheet. The cells' compressed blocks are
///         copied into the base level by level while they stay on the block grid (levels 0-5 at 128 texels, where an atlas
///         cell's own box mips equal the sheet's). The smaller levels come whole from &lt;path&gt;~patchtail (the language's
///         sheet at the first of them, 64 x 32 for an evacuation plan, with its mips), so a far plan shows no ghost of the
///         English layout. A patched base is unloaded before another language patches it, so patches always go onto English.
///   own   the base is readable: code reads its pixels on the CPU (the stair signs' plate art and glyph atlases). That code
///         loads the language's copy itself with Texture() (any size) and its layout file with Text(), and rebuilds on
///         Applied.
/// A piece is no kind of its own: a language-only texture &lt;path&gt;~piece0, ~piece1 ... beside &lt;path&gt;'s copy, with no English
/// base and never swapped. The import gives it &lt;path&gt;'s settings and the manifest does not list it; only code whose
/// layout names it loads it (Piece): the landing placards' extra token pieces, where a language's floor token is wider
/// than the English pieces (Prop_EvacPlan_A_Landing_rects "extra", FrontRoomsLandingPlacard.ExtraPiecesFor).
/// The language is read from FrontRoomsLanguage.Current once a frame (its start-up Load raises no event) and applied in
/// that frame; then Applied is raised. WebGL lists English alone (FrontRoomsLanguage.Listed), so nothing here runs there.
/// Cost: none in English; in another language the swapped and patched bases stay loaded (they are what the rooms show) and
/// each copy or atlas is loaded only for its copy, then unloaded the next frame.
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
        /// <summary>Resources paths of bases patched with this language's differing cells (&lt;path&gt;~patch.json).</summary>
        public string[] patch;
    }

    /// <summary>The suffix of a patch copy's plan (&lt;path&gt;~patch.json) and atlases (&lt;path&gt;~patch0, ~patch1, ...).</summary>
    public const string PatchSuffix = "~patch";

    /// <summary>The suffix of a language-only piece (&lt;path&gt;~piece0, ~piece1, ...): imported with &lt;path&gt;'s settings, never in
    /// the manifest, loaded by Piece().</summary>
    public const string PieceSuffix = "~piece";

    /// <summary>A patch copy's plan (Tools/lookdev/pack_world_text_patch.py). Texels at level 0, from the bottom left.</summary>
    [Serializable]
    public sealed class PatchPlan
    {
        /// <summary>The base's size.</summary>
        public int width, height;
        /// <summary>The grid the cells sit on, from the base's origin.</summary>
        public int cell;
        public int atlases;
        /// <summary>Seven numbers per copy: the atlas, x and y in it, width, height, x and y in the base.</summary>
        public int[] rects;
        /// <summary>The first level the tail (&lt;path&gt;~patchtail, that level of the language's sheet with its mips) gives
        /// whole; 0 for none.</summary>
        public int tail;
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
    // Copies and atlases whose blocks went into a base: unloaded the next frame, once the GPU has made the copies (as the
    // landing placards drop their patches).
    static readonly List<UnityEngine.Object> retire = new List<UnityEngine.Object>();
    static int retireFrame = -1;

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

    /// <summary>A language-only piece (its Resources path inside the language's folder, e.g.
    /// Surfaces/Textures/Prop_EvacPlan_A_Landing_B10plus_Title0~piece0) in the language on screen (AppliedCode); null in
    /// English or when it is missing. The caller owns what it loaded, as with Resources.Load.</summary>
    public static Texture2D Piece(string path)
    {
        if (string.IsNullOrEmpty(path) || AppliedCode == English) return null;
        var piece = Resources.Load<Texture2D>(Root + AppliedCode + "/" + path);
        if (piece == null) Warn(path + " (" + AppliedCode + " piece) is named by a layout but missing");
        return piece;
    }

    /// <summary>Follow the game's language now if it changed (the watcher calls this once a frame).</summary>
    public static void Follow()
    {
        if (retire.Count > 0 && Time.frameCount > retireFrame) Flush();
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
        // Bases the new language does not swap go back to their own pixels; so do patched ones, since a patch covers only
        // its cells and goes onto English.
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
        {
            foreach (var path in k.language.swap ?? Array.Empty<string>())
                if (Swap(path, k.language.code)) Swapped++; else Refused++;
            foreach (var path in k.language.patch ?? Array.Empty<string>())
                if (Patch(path, k.language.code)) Swapped++; else Refused++;
        }
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
        Retire(v);
        held[path] = b;
        return true;
    }

    static bool Patch(string path, string code)
    {
        var stem = Root + code + "/" + path + PatchSuffix;
        var b = Resources.Load<Texture2D>(path);
        PatchPlan plan = null;
        var planText = Resources.Load<TextAsset>(stem);
        if (planText != null)
        {
            try { plan = JsonUtility.FromJson<PatchPlan>(planText.text); }
            catch (Exception) { plan = null; }
            Resources.UnloadAsset(planText);
        }
        var r = plan?.rects;
        string why = null;
        if (b == null) why = "no base";
        else if (r == null || r.Length == 0 || r.Length % 7 != 0 || plan.atlases < 1) why = "no plan";
        else if (b.isReadable) why = "the base is readable (an own copy, loaded by its code)";
        else if (b.width != plan.width || b.height != plan.height) why = "the base is " + b.width + "x" + b.height + ", the plan's " + plan.width + "x" + plan.height;
        else if (b.activeMipmapLimit != 0) why = "a mipmap limit is in force";
        else if ((SystemInfo.copyTextureSupport & CopyTextureSupport.Basic) == 0) why = "no CopyTexture on this GPU";
        var atlases = why == null ? new Texture2D[plan.atlases] : null;
        for (var i = 0; why == null && i < atlases.Length; i++)
        {
            var a = atlases[i] = Resources.Load<Texture2D>(stem + i);
            if (a == null) why = "no atlas " + i;
            else if (a.graphicsFormat != b.graphicsFormat || a.activeMipmapLimit != 0) why = "atlas " + i + " is " + a.graphicsFormat + ", the base " + b.graphicsFormat;
        }
        // The levels every copy takes: while each stays on the format's block grid in both textures (the cells share one
        // grid, so they agree: 6 at 128 texels with 4 x 4 blocks, 5 at 192).
        var levels = why == null ? b.mipmapCount : 0;
        if (why == null)
        {
            int bw = (int)GraphicsFormatUtility.GetBlockWidth(b.graphicsFormat), bh = (int)GraphicsFormatUtility.GetBlockHeight(b.graphicsFormat);
            for (var i = 0; why == null && i < r.Length; i += 7)
            {
                var a = r[i] >= 0 && r[i] < atlases.Length ? atlases[r[i]] : null;
                if (a == null || r[i + 3] <= 0 || r[i + 4] <= 0 || r[i + 1] < 0 || r[i + 2] < 0 || r[i + 5] < 0 || r[i + 6] < 0
                    || r[i + 1] + r[i + 3] > a.width || r[i + 2] + r[i + 4] > a.height || r[i + 5] + r[i + 3] > b.width || r[i + 6] + r[i + 4] > b.height)
                {
                    why = "copy " + i / 7 + " falls outside its textures";
                    break;
                }
                var n = 0;
                while (n < levels && n < a.mipmapCount && OnGrid(r, i, bw << n, bh << n)) n++;
                levels = n;
            }
            if (why == null && levels == 0) why = "a copy is off the " + bw + " x " + bh + " block grid";
        }
        if (why != null)
        {
            Warn(path + " (" + code + " patch): " + why + "; English stays");
            if (atlases != null)
                foreach (var a in atlases)
                    if (a != null) Resources.UnloadAsset(a);
            return false;
        }
        for (var i = 0; i < r.Length; i += 7)
            for (var m = 0; m < levels; m++)
                Graphics.CopyTexture(atlases[r[i]], 0, m, r[i + 1] >> m, r[i + 2] >> m, r[i + 3] >> m, r[i + 4] >> m, b, 0, m, r[i + 5] >> m, r[i + 6] >> m);
        foreach (var a in atlases) Retire(a);
        // The smaller levels whole from the tail; without one (or one that does not fit) they keep English.
        if (plan.tail > 0 && plan.tail < b.mipmapCount)
        {
            var t = Resources.Load<Texture2D>(stem + "tail");
            if (t == null) Warn(path + " (" + code + " patch): no tail; levels from " + plan.tail + " keep English");
            else if (t.graphicsFormat != b.graphicsFormat || t.width != b.width >> plan.tail || t.height != b.height >> plan.tail
                     || t.mipmapCount < b.mipmapCount - plan.tail || t.activeMipmapLimit != 0)
            {
                Warn(path + " (" + code + " patch): the tail is " + t.width + "x" + t.height + " " + t.graphicsFormat + " mips " + t.mipmapCount + "; levels from " + plan.tail + " keep English");
                Resources.UnloadAsset(t);
            }
            else
            {
                for (var m = 0; plan.tail + m < b.mipmapCount; m++) Graphics.CopyTexture(t, 0, m, b, 0, plan.tail + m);
                Retire(t);
            }
        }
        held[path] = b;
        return true;
    }

    static bool OnGrid(int[] r, int i, int bw, int bh) =>
        r[i + 1] % bw == 0 && r[i + 2] % bh == 0 && r[i + 3] % bw == 0 && r[i + 4] % bh == 0 && r[i + 5] % bw == 0 && r[i + 6] % bh == 0;

    static void Retire(UnityEngine.Object copy)
    {
        retire.Add(copy);
        retireFrame = Time.frameCount;
    }

    static void Flush()
    {
        foreach (var o in retire)
            if (o != null) Resources.UnloadAsset(o);
        retire.Clear();
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
        Flush();
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
        retire.Clear();
        retireFrame = -1;
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
