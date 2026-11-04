using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The office terminal's bitmap face (C20, Red 2026-10-30: our own 8 x 16 face; graphic visual draws it). ONE asset:
/// Resources/Terminal/TerminalFace.png, 128 x 320, white ink on black (red &gt; 127 = ink), imported readable, point,
/// uncompressed, no mips. Until graphic visual's face lands it holds the mock's FR816 stand-in (W/terminal/make_face.py), and
/// swapping the face is replacing that one file (or <see cref="Use"/> at run time).
///   y   0-255  the 8 x 16 face, 16 x 16 cells in CP437 order (code c at x = (c % 16) * 8, y = (c / 16) * 16, top-down)
///   y 256-287  block alphabet 1, MAIN's headline: 8 x 8 cells for ASCII 0x20-0x5F (k = code - 0x20 at x = (k % 16) * 8,
///              y = 256 + (k / 16) * 8), each glyph from its cell's top-left; width and height = the widest / tallest glyph
///   y 288-319  block alphabet 2, MAIN's second line: same layout
/// The VGA 9th column (41 section 3.1): the hardware's 9 x 16 cell repeats the 8th column of CP437 0xC0-0xDF only.
/// Spec: Documentation/research/computer_ui/41_c19_period_interaction.md sections 3.1 and 6.
///
/// Languages (W18, 2026-11-03; research/narrative/95_world_text_localization.md 5.5, research/localization/WORLD_TYPE.md 3b):
/// a language's terminal adds its own glyphs to C20's, as the 1990 machines did (CP850 and CP866 half-width; GB 2312,
/// Big5, JIS X 0208 and Hangul full-width at 16 x 16 across two cells). Its face is two files graphic visual makes:
///   Resources/Terminal/Faces/&lt;code&gt;.png  readable, white ink on black, 128 px wide: slots of 8 x 16, slot k at
///       x = (k % 16) * 8, y = (k / 16) * 16 (top-down); a full-width glyph takes two slots side by side, its left half on an
///       even slot, so the atlas reads as a grid of 16 x 16 glyphs; the big-type bands sit below the slots (their "top").
///   Resources/Terminal/Faces/&lt;code&gt;.json  {"half": "&lt;the half-width characters in slot order from slot 0&gt;",
///       "wide": "&lt;the full-width characters, each taking the next even slot pair&gt;",
///       "big": [{"role": "headline" | "second", "mode": "block" | "halfblock", "chars": "...", "w": 12, "h": 12,
///                "top": &lt;y of the band&gt;, "perRow": 10}, ...]}
///     "block" adds letters to C20's block alphabet of that role (Latin and Cyrillic capitals: a pixel is full-block cells);
///     "halfblock" draws a pixel glyph a column per pixel and two pixel rows per cell with the half blocks (CJK and Hangul,
///     Fusion Pixel 12 and 10 px: a 12-px glyph is 12 columns x 6 rows).
/// The language's glyphs follow C20's in one slot space (slots 0-255 are C20's CP437 codes, then the language's), which
/// is what the grid stores and the raster draws. A language with no face shows the English set (FrontRoomsTerminalContent).
/// </summary>
public sealed class FrontRoomsTerminalFace
{
    public const int CellW = 8, CellH = 16, Codes = 256;
    public const string ResourcePath = "Terminal/TerminalFace";
    /// <summary>Where a language's terminal face lives (Resources path; the file name is the language code).</summary>
    public const string LanguageFolder = "Terminal/Faces/";
    const int AtlasW = 128, AtlasH = 320, BlockTop = 256, BlockCell = 8, BlockBand = 32;

    /// <summary>One alphabet of big block letters (41 section 3.3): each of its pixels is set as full-block cells.</summary>
    public sealed class BlockAlphabet
    {
        sealed class Extra { public bool[] bits; public int w, h; public bool halfBlock; }

        readonly ulong[] glyphs = new ulong[64];   // 8 x 8 bits per glyph, bit (y * 8 + x)
        readonly bool[] has = new bool[64];
        // the language's letters (W18): "block" letters past ASCII (Latin, Cyrillic), and "halfblock" pixel glyphs (CJK, Hangul)
        readonly Dictionary<char, Extra> extra = new Dictionary<char, Extra>();
        public int Width { get; internal set; }
        public int Height { get; internal set; }
        /// <summary>The language's pixel glyphs drawn with half blocks (CJK, Hangul), 0 when none: their size in pixels.</summary>
        public int HalfWidth { get; internal set; }
        public int HalfHeight { get; internal set; }

        internal void Set(int k, int x, int y) { glyphs[k] |= 1UL << (y * 8 + x); has[k] = true; }

        internal void SetExtra(char c, bool[] bits, int w, int h, bool halfBlock) => extra[c] = new Extra { bits = bits, w = w, h = h, halfBlock = halfBlock };

        static int Index(char c)
        {
            if (c >= 'a' && c <= 'z') c = (char)(c - 32);   // block type is capitals only
            var k = c - 0x20;
            return k >= 0 && k < 64 ? k : -1;
        }

        Extra Find(char c)
        {
            if (extra.TryGetValue(c, out var e)) return e;
            return extra.TryGetValue(char.ToUpperInvariant(c), out e) ? e : null;
        }

        /// <summary>The alphabet draws this character (space always).</summary>
        public bool Has(char c) { var k = Index(c); return c == ' ' || (k >= 0 && has[k]) || Find(c) != null; }

        /// <summary>The character is one of the language's half-block pixel glyphs (draw it with <see cref="HalfPixel"/>).</summary>
        public bool IsHalf(char c) { var e = Find(c); return e != null && e.halfBlock; }

        public bool Pixel(char c, int x, int y)
        {
            if (x < 0 || y < 0) return false;
            var e = Find(c);
            if (e != null && !e.halfBlock) return x < e.w && y < e.h && e.bits[y * e.w + x];
            var k = Index(c);
            if (k < 0 || x >= 8 || y >= 8) return false;
            return (glyphs[k] >> (y * 8 + x) & 1UL) != 0;
        }

        /// <summary>A half-block glyph's pixel (x across, y down, both from the top-left).</summary>
        public bool HalfPixel(char c, int x, int y)
        {
            var e = Find(c);
            if (e == null || !e.halfBlock || x < 0 || y < 0 || x >= e.w || y >= e.h) return false;
            return e.bits[y * e.w + x];
        }
    }

    byte[] rows = new byte[Codes * CellH];   // per slot, bit 7 = the left column
    bool[] drawn = new bool[Codes];
    readonly Dictionary<char, int> half = new Dictionary<char, int>();   // a language's half-width character -> slot
    readonly Dictionary<char, int> wide = new Dictionary<char, int>();   // a language's full-width character -> its left slot
    Texture2D glyphTexture;

    /// <summary>The asset's name (the stand-in is "TerminalFace" from FR816).</summary>
    public string Name { get; private set; }
    /// <summary>The language this face is for ("en": C20 alone).</summary>
    public string Language { get; private set; } = FrontRoomsWorldText.English;
    public BlockAlphabet Headline { get; } = new BlockAlphabet();
    public BlockAlphabet Second { get; } = new BlockAlphabet();
    /// <summary>Codes with ink in the atlas (0x00, 0x20 and 0xFF count as drawn: they are blank by definition).</summary>
    public int DrawnCount { get; private set; }
    /// <summary>Glyph slots: C20's 256, then the language's.</summary>
    public int Slots => drawn.Length;

    static FrontRoomsTerminalFace current;
    static readonly Dictionary<string, FrontRoomsTerminalFace> byLanguage = new Dictionary<string, FrontRoomsTerminalFace>();
    static readonly Dictionary<string, bool> hasFace = new Dictionary<string, bool>();
    static bool hooked;

    /// <summary>Raised after <see cref="Use"/> or a language change swaps the face: every live terminal re-renders (and, on a
    /// language change, reloads its content set: FrontRoomsTerminalScreen).</summary>
    public static event Action Changed;

    /// <summary>The face in use: the terminal language's (<see cref="TerminalLanguage"/>), loaded on first use.</summary>
    public static FrontRoomsTerminalFace Current
    {
        get
        {
            Hook();
            var code = TerminalLanguage;
            if (current == null || current.Language != code) current = ForLanguage(code);
            return current;
        }
    }

    /// <summary>The language the terminals are in: the world text's (FrontRoomsWorldText.AppliedCode) when it has a terminal
    /// face, else English (a language without its glyphs keeps the English set rather than show '?').</summary>
    public static string TerminalLanguage
    {
        get
        {
            var code = FrontRoomsWorldText.AppliedCode;
            return code != FrontRoomsWorldText.English && HasFace(code) ? code : FrontRoomsWorldText.English;
        }
    }

    /// <summary>A terminal face exists for the language (its json and png in Resources/Terminal/Faces/).</summary>
    public static bool HasFace(string code)
    {
        if (string.IsNullOrEmpty(code) || code == FrontRoomsWorldText.English) return true;
        if (hasFace.TryGetValue(code, out var yes)) return yes;
        var json = Resources.Load<TextAsset>(LanguageFolder + code);
        yes = json != null;
        if (json != null) Resources.UnloadAsset(json);
        hasFace[code] = yes;
        return yes;
    }

    /// <summary>Swap the face at run time (a readable 128 x 320 atlas in the layout above). Null restores the asset.</summary>
    public static void Use(Texture2D atlas)
    {
        var next = atlas != null ? FromAtlas(atlas, atlas.name) : FromAtlas(Resources.Load<Texture2D>(ResourcePath), "TerminalFace");
        if (current != null && !byLanguage.ContainsValue(current)) current.Release();
        current = next;
        Changed?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        current = null;
        Changed = null;
        byLanguage.Clear();
        hasFace.Clear();
        hooked = false;
    }

    static void Hook()
    {
        if (hooked) return;
        hooked = true;
        FrontRoomsWorldText.Applied += OnLanguageApplied;
    }

    // The world text followed a new language: the terminals follow when that language has a face (else they stay English).
    static void OnLanguageApplied()
    {
        var code = TerminalLanguage;
        if (current != null && current.Language == code) return;
        current = ForLanguage(code);
        Changed?.Invoke();
    }

    /// <summary>C20 with the language's glyphs (cached; English = C20 alone).</summary>
    public static FrontRoomsTerminalFace ForLanguage(string code)
    {
        if (string.IsNullOrEmpty(code)) code = FrontRoomsWorldText.English;
        if (byLanguage.TryGetValue(code, out var face) && face != null) return face;
        face = FromAtlas(Resources.Load<Texture2D>(ResourcePath), "TerminalFace");
        if (code != FrontRoomsWorldText.English) face.AddLanguage(code);
        byLanguage[code] = face;
        return face;
    }

    /// <summary>The 8 bits of one glyph row (bit 7 = left), y = 0 at the top of the 16-row cell, for any slot.</summary>
    public byte Row(int slot, int y) => slot >= 0 && slot < drawn.Length ? rows[slot * CellH + y] : (byte)0;

    public bool Drawn(int slot) => slot >= 0 && slot < drawn.Length && drawn[slot];

    /// <summary>The VGA 9th column: CP437 0xC0-0xDF repeat their 8th column so box lines and blocks join.</summary>
    public static bool NinthColumn(int code) => code >= 0xC0 && code <= 0xDF;

    /// <summary>
    /// Where a character goes: its slot (a full-width glyph's left half; the right half is slot + 1), whether it is
    /// full-width (two cells), and whether this face draws it. CP437's characters first, then the language's.
    /// </summary>
    public bool Lookup(char c, out int slot, out bool isWide)
    {
        isWide = false;
        var code = ToCode(c);
        if (code >= 0) { slot = code; return Drawn(code); }
        if (wide.TryGetValue(c, out slot)) { isWide = true; return true; }
        if (half.TryGetValue(c, out slot)) return true;
        isWide = Cells(c) == 2;
        slot = '?';
        return false;
    }

    /// <summary>The character a slot draws (transcripts, checks): CP437's, the language's, or '?'.</summary>
    public char CharOf(int slot)
    {
        if (slot < Codes) return ToChar(slot);
        foreach (var p in half) if (p.Value == slot) return p.Key;
        foreach (var p in wide) if (p.Value == slot) return p.Key;
        return '?';
    }

    /// <summary>
    /// The glyph slots as an R8 texture for the GPU raster: 128 px wide, 16 slots a row, memory row y = atlas row y
    /// (top-down), point, no mips. Built once per face (C20 alone: 128 x 256, 32 KiB).
    /// </summary>
    public Texture2D GlyphTexture
    {
        get
        {
            if (glyphTexture != null) return glyphTexture;
            var slotRows = (drawn.Length + 15) / 16;
            var height = slotRows * CellH;
            var t = new Texture2D(AtlasW, height, TextureFormat.R8, false, true)
            {
                name = "FrontRooms terminal face (" + Name + ", " + Language + ")",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            var data = new byte[AtlasW * height];
            for (var slot = 0; slot < drawn.Length; slot++)
            {
                var cx = (slot % 16) * CellW;
                var cy = (slot / 16) * CellH;
                for (var y = 0; y < CellH; y++)
                {
                    var bits = rows[slot * CellH + y];
                    for (var x = 0; x < CellW; x++)
                        data[(cy + y) * AtlasW + cx + x] = (byte)((bits >> (7 - x) & 1) != 0 ? 255 : 0);
                }
            }
            t.SetPixelData(data, 0);
            t.Apply(false, true);   // GPU only from here: the CPU copy is in rows[]
            glyphTexture = t;
            return t;
        }
    }

    void Release()
    {
        if (glyphTexture == null) return;
        if (Application.isPlaying) UnityEngine.Object.Destroy(glyphTexture); else UnityEngine.Object.DestroyImmediate(glyphTexture);
        glyphTexture = null;
    }

    static FrontRoomsTerminalFace FromAtlas(Texture2D atlas, string name)
    {
        var face = new FrontRoomsTerminalFace { Name = name };
        if (atlas == null || atlas.width != AtlasW || atlas.height != AtlasH || !atlas.isReadable)
        {
            Debug.LogWarning("[FrontRoomsTerminalFace] " + name + ": needs a readable " + AtlasW + " x " + AtlasH
                + " atlas at Resources/" + ResourcePath + " (got " + (atlas == null ? "none" : atlas.width + " x " + atlas.height + (atlas.isReadable ? "" : ", not readable")) + "); terminals draw blank cells.");
            face.drawn[0] = face.drawn[0x20] = face.drawn[0xFF] = true;
            face.DrawnCount = 3;
            return face;
        }
        var px = atlas.GetPixels32();   // bottom row first
        Func<int, int, bool> ink = (x, yTop) => px[(AtlasH - 1 - yTop) * AtlasW + x].r > 127;
        for (var code = 0; code < Codes; code++)
        {
            var cx = (code % 16) * CellW;
            var cy = (code / 16) * CellH;
            var any = false;
            for (var y = 0; y < CellH; y++)
            {
                var bits = 0;
                for (var x = 0; x < CellW; x++)
                    if (ink(cx + x, cy + y)) bits |= 1 << (7 - x);
                face.rows[code * CellH + y] = (byte)bits;
                any |= bits != 0;
            }
            face.drawn[code] = any || code == 0 || code == 0x20 || code == 0xFF;
            if (face.drawn[code]) face.DrawnCount++;
        }
        ReadBlocks(face.Headline, BlockTop, ink);
        ReadBlocks(face.Second, BlockTop + BlockBand, ink);
        return face;
    }

    static void ReadBlocks(BlockAlphabet abc, int top, Func<int, int, bool> ink)
    {
        int w = 0, h = 0;
        for (var k = 0; k < 64; k++)
        {
            var cx = (k % 16) * BlockCell;
            var cy = top + (k / 16) * BlockCell;
            for (var y = 0; y < BlockCell; y++)
                for (var x = 0; x < BlockCell; x++)
                    if (ink(cx + x, cy + y))
                    {
                        abc.Set(k, x, y);
                        w = Mathf.Max(w, x + 1);
                        h = Mathf.Max(h, y + 1);
                    }
        }
        abc.Width = w;
        abc.Height = h;
    }

    // ---------------------------------------------------------------- a language's glyphs (W18)

    [Serializable] sealed class BigBand { public string role = "", mode = "", chars = ""; public int w, h, top, perRow; }
    [Serializable] sealed class LanguageFile { public string half = "", wide = ""; public BigBand[] big = new BigBand[0]; }

    void AddLanguage(string code)
    {
        var json = Resources.Load<TextAsset>(LanguageFolder + code);
        var atlas = Resources.Load<Texture2D>(LanguageFolder + code);
        LanguageFile file = null;
        try { file = json != null ? JsonUtility.FromJson<LanguageFile>(json.text) : null; }
        catch (Exception e) { Debug.LogWarning("[FrontRoomsTerminalFace] " + code + ": bad face json: " + e.Message); }
        if (json != null) Resources.UnloadAsset(json);
        if (file == null || atlas == null || atlas.width != AtlasW || !atlas.isReadable)
        {
            Debug.LogWarning("[FrontRoomsTerminalFace] " + code + ": needs Resources/" + LanguageFolder + code + ".json and a readable "
                + AtlasW + " px wide .png; the terminal keeps C20 alone.");
            return;
        }
        Language = code;
        Name = Name + "+" + code;
        var h = atlas.height;
        var px = atlas.GetPixels32();
        Func<int, int, bool> ink = (x, yTop) => yTop >= 0 && yTop < h && x >= 0 && x < AtlasW && px[(h - 1 - yTop) * AtlasW + x].r > 127;
        var halfChars = file.half ?? "";
        var wideChars = file.wide ?? "";
        var firstWide = halfChars.Length + (halfChars.Length & 1);
        var langSlots = firstWide + 2 * wideChars.Length;
        var total = Codes + langSlots;
        Array.Resize(ref rows, total * CellH);
        Array.Resize(ref drawn, total);
        Func<int, bool> readSlot = k =>
        {
            var cx = (k % 16) * CellW;
            var cy = (k / 16) * CellH;
            var any = false;
            for (var y = 0; y < CellH; y++)
            {
                var bits = 0;
                for (var x = 0; x < CellW; x++)
                    if (ink(cx + x, cy + y)) bits |= 1 << (7 - x);
                rows[(Codes + k) * CellH + y] = (byte)bits;
                any |= bits != 0;
            }
            drawn[Codes + k] = true;
            return any;
        };
        for (var i = 0; i < halfChars.Length; i++)
        {
            readSlot(i);
            half[halfChars[i]] = Codes + i;
        }
        for (var i = 0; i < wideChars.Length; i++)
        {
            var k = firstWide + 2 * i;
            readSlot(k);
            readSlot(k + 1);
            wide[wideChars[i]] = Codes + k;
        }
        DrawnCount += halfChars.Length + wideChars.Length;
        if (file.big != null)
            foreach (var band in file.big)
            {
                if (band == null || string.IsNullOrEmpty(band.chars) || band.w <= 0 || band.h <= 0) continue;
                var abc = band.role == "second" ? Second : Headline;
                var per = band.perRow > 0 ? band.perRow : Mathf.Max(1, AtlasW / band.w);
                var halfMode = band.mode == "halfblock";
                for (var i = 0; i < band.chars.Length; i++)
                {
                    var gx = (i % per) * band.w;
                    var gy = band.top + (i / per) * band.h;
                    var bits = new bool[band.w * band.h];
                    for (var y = 0; y < band.h; y++)
                        for (var x = 0; x < band.w; x++)
                            bits[y * band.w + x] = ink(gx + x, gy + y);
                    abc.SetExtra(band.chars[i], bits, band.w, band.h, halfMode);
                }
                if (halfMode) { abc.HalfWidth = band.w; abc.HalfHeight = band.h; }
                else { abc.Width = Mathf.Max(abc.Width, band.w); abc.Height = Mathf.Max(abc.Height, band.h); }
            }
        Resources.UnloadAsset(atlas);
    }

    // ---------------------------------------------------------------- widths (W18: the builder's rule)

    /// <summary>Cells a character takes: 2 for East Asian Wide and Fullwidth (CJK, kana, Hangul, full-width forms), else 1 —
    /// the rule research/narrative's builder checks every terminal string with (unicodedata.east_asian_width in "WF").</summary>
    public static int Cells(char c)
    {
        if (c < 0x1100) return 1;
        if (c <= 0x115F) return 2;                       // Hangul Jamo initials
        if (c >= 0x2E80 && c <= 0x303E) return 2;        // CJK radicals, punctuation, ideographic space
        if (c >= 0x3041 && c <= 0x33FF) return 2;        // kana, CJK compatibility
        if (c >= 0x3400 && c <= 0x4DBF) return 2;        // CJK extension A
        if (c >= 0x4E00 && c <= 0x9FFF) return 2;        // CJK unified ideographs
        if (c >= 0xA000 && c <= 0xA4CF) return 2;        // Yi
        if (c >= 0xAC00 && c <= 0xD7A3) return 2;        // Hangul syllables
        if (c >= 0xF900 && c <= 0xFAFF) return 2;        // CJK compatibility ideographs
        if (c >= 0xFE10 && c <= 0xFE19) return 2;        // vertical forms
        if (c >= 0xFE30 && c <= 0xFE6F) return 2;        // CJK compatibility and small forms
        if (c >= 0xFF00 && c <= 0xFF60) return 2;        // full-width forms
        if (c >= 0xFFE0 && c <= 0xFFE6) return 2;        // full-width signs
        return 1;
    }

    /// <summary>Cells a string takes on the grid.</summary>
    public static int Cells(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        var n = 0;
        for (var i = 0; i < s.Length; i++) n += Cells(s[i]);
        return n;
    }

    // ---------------------------------------------------------------- Unicode <-> CP437 (41 section 6.1)

    /// <summary>The CP437 code for a character: printable ASCII as is, the 14 layout glyphs of 41 section 6.1, else -1.</summary>
    public static int ToCode(char c)
    {
        if (c >= 0x20 && c <= 0x7E) return c;
        switch (c)
        {
            case '─': return 0xC4;   // box horizontal
            case '│': return 0xB3;   // box vertical
            case '┌': return 0xDA;
            case '┐': return 0xBF;
            case '└': return 0xC0;
            case '┘': return 0xD9;
            case '├': return 0xC3;
            case '┤': return 0xB4;
            case '█': return 0xDB;   // full block
            case '▀': return 0xDF;   // upper half block (W18: big CJK type)
            case '▄': return 0xDC;   // lower half block
            case '↑': return 0x18;   // arrows
            case '↓': return 0x19;
            case '→': return 0x1A;
            case '←': return 0x1B;
            case '■': return 0xFE;   // the unread mark
            default: return -1;
        }
    }

    /// <summary>The character a CP437 code stands for (transcripts and checks), '?' for codes outside the set.</summary>
    public static char ToChar(int code)
    {
        if (code >= 0x20 && code <= 0x7E) return (char)code;
        switch (code)
        {
            case 0x00: return ' ';
            case 0xC4: return '─';
            case 0xB3: return '│';
            case 0xDA: return '┌';
            case 0xBF: return '┐';
            case 0xC0: return '└';
            case 0xD9: return '┘';
            case 0xC3: return '├';
            case 0xB4: return '┤';
            case 0xDB: return '█';
            case 0xDF: return '▀';
            case 0xDC: return '▄';
            case 0x18: return '↑';
            case 0x19: return '↓';
            case 0x1A: return '→';
            case 0x1B: return '←';
            case 0xFE: return '■';
            default: return '?';
        }
    }
}
