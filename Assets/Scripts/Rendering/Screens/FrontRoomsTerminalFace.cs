using System;
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
/// </summary>
public sealed class FrontRoomsTerminalFace
{
    public const int CellW = 8, CellH = 16, Codes = 256;
    public const string ResourcePath = "Terminal/TerminalFace";
    const int AtlasW = 128, AtlasH = 320, BlockTop = 256, BlockCell = 8, BlockBand = 32;

    /// <summary>One alphabet of big block letters (41 section 3.3): each of its pixels is set as full-block cells.</summary>
    public sealed class BlockAlphabet
    {
        readonly ulong[] glyphs = new ulong[64];   // 8 x 8 bits per glyph, bit (y * 8 + x)
        readonly bool[] has = new bool[64];
        public int Width { get; internal set; }
        public int Height { get; internal set; }

        internal void Set(int k, int x, int y) { glyphs[k] |= 1UL << (y * 8 + x); has[k] = true; }

        static int Index(char c)
        {
            if (c >= 'a' && c <= 'z') c = (char)(c - 32);   // block type is capitals only
            var k = c - 0x20;
            return k >= 0 && k < 64 ? k : -1;
        }

        /// <summary>The alphabet draws this character (space always).</summary>
        public bool Has(char c) { var k = Index(c); return c == ' ' || (k >= 0 && has[k]); }

        public bool Pixel(char c, int x, int y)
        {
            var k = Index(c);
            if (k < 0 || x < 0 || y < 0 || x >= 8 || y >= 8) return false;
            return (glyphs[k] >> (y * 8 + x) & 1UL) != 0;
        }
    }

    readonly byte[] rows = new byte[Codes * CellH];   // bit 7 = the left column
    readonly bool[] drawn = new bool[Codes];
    Texture2D glyphTexture;

    /// <summary>The asset's name (the stand-in is "TerminalFace" from FR816).</summary>
    public string Name { get; private set; }
    public BlockAlphabet Headline { get; } = new BlockAlphabet();
    public BlockAlphabet Second { get; } = new BlockAlphabet();
    /// <summary>Codes with ink in the atlas (0x00, 0x20 and 0xFF count as drawn: they are blank by definition).</summary>
    public int DrawnCount { get; private set; }

    static FrontRoomsTerminalFace current;

    /// <summary>Raised after <see cref="Use"/> swaps the face: every live terminal re-renders.</summary>
    public static event Action Changed;

    /// <summary>The face in use (loaded from Resources on first use).</summary>
    public static FrontRoomsTerminalFace Current
    {
        get
        {
            if (current == null) current = FromAtlas(Resources.Load<Texture2D>(ResourcePath), "TerminalFace");
            return current;
        }
    }

    /// <summary>Swap the face at run time (a readable 128 x 320 atlas in the layout above). Null restores the asset.</summary>
    public static void Use(Texture2D atlas)
    {
        var next = atlas != null ? FromAtlas(atlas, atlas.name) : FromAtlas(Resources.Load<Texture2D>(ResourcePath), "TerminalFace");
        current?.Release();
        current = next;
        Changed?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        current = null;
        Changed = null;
    }

    /// <summary>The 8 bits of one glyph row (bit 7 = left), y = 0 at the top of the 16-row cell.</summary>
    public byte Row(int code, int y) => rows[(code & 255) * CellH + y];

    public bool Drawn(int code) => drawn[code & 255];

    /// <summary>The VGA 9th column: CP437 0xC0-0xDF repeat their 8th column so box lines and blocks join.</summary>
    public static bool NinthColumn(int code) => code >= 0xC0 && code <= 0xDF;

    /// <summary>
    /// The glyph part as an R8 texture for the GPU raster: 128 x 256, memory row y = atlas row y (top-down), point, no mips.
    /// Built once per face (32 KiB).
    /// </summary>
    public Texture2D GlyphTexture
    {
        get
        {
            if (glyphTexture != null) return glyphTexture;
            var t = new Texture2D(AtlasW, 256, TextureFormat.R8, false, true)
            {
                name = "FrontRooms terminal face (" + Name + ")",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            var data = new byte[AtlasW * 256];
            for (var code = 0; code < Codes; code++)
            {
                var cx = (code % 16) * CellW;
                var cy = (code / 16) * CellH;
                for (var y = 0; y < CellH; y++)
                {
                    var bits = rows[code * CellH + y];
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

    // ---------------------------------------------------------------- Unicode <-> CP437 (41 section 6.1)

    /// <summary>The CP437 code for a character: printable ASCII as is, the 14 layout glyphs of 41 section 6.1, else -1.</summary>
    public static int ToCode(char c)
    {
        if (c >= 0x20 && c <= 0x7E) return c;
        switch (c)
        {
            case '\u2500': return 0xC4;   // box horizontal
            case '\u2502': return 0xB3;   // box vertical
            case '\u250C': return 0xDA;
            case '\u2510': return 0xBF;
            case '\u2514': return 0xC0;
            case '\u2518': return 0xD9;
            case '\u251C': return 0xC3;
            case '\u2524': return 0xB4;
            case '\u2588': return 0xDB;   // full block
            case '\u2191': return 0x18;   // arrows
            case '\u2193': return 0x19;
            case '\u2192': return 0x1A;
            case '\u2190': return 0x1B;
            case '\u25A0': return 0xFE;   // the unread mark
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
            case 0xC4: return '\u2500';
            case 0xB3: return '\u2502';
            case 0xDA: return '\u250C';
            case 0xBF: return '\u2510';
            case 0xC0: return '\u2514';
            case 0xD9: return '\u2518';
            case 0xC3: return '\u251C';
            case 0xB4: return '\u2524';
            case 0xDB: return '\u2588';
            case 0x18: return '\u2191';
            case 0x19: return '\u2193';
            case 0x1A: return '\u2192';
            case 0x1B: return '\u2190';
            case 0xFE: return '\u25A0';
            default: return '?';
        }
    }
}
