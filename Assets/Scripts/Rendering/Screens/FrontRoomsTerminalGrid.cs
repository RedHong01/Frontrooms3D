using System.Text;
using UnityEngine;

/// <summary>
/// VGA text mode's screen buffer (41 section 3.1): 80 x 25 cells of (CP437 glyph, attribute). Attribute = background in
/// the high nibble, foreground in the low nibble, indices into <see cref="FrontRoomsTerminalPalette.Vga"/>; there is no
/// blink bit (41 section 3.4: nothing blinks). Every write compares first, so <see cref="Version"/> moves only on a real
/// change: the panel machine reads it to know whether a key changed the screen (the 0.34 s rule counts screen changes).
/// Rows and columns are 0-based here; the spec's row n is index n - 1.
/// </summary>
public sealed class FrontRoomsTerminalGrid
{
    public const int Cols = 80, Rows = 25;

    readonly byte[] glyph = new byte[Cols * Rows];
    readonly byte[] attr = new byte[Cols * Rows];

    /// <summary>Bumped on every change of any cell.</summary>
    public int Version { get; private set; }
    /// <summary>Characters written that the face does not draw (or that have no CP437 code), since the last <see cref="ResetChecks"/>.</summary>
    public int Missing { get; private set; }
    /// <summary>Strings clipped at the row's end since the last <see cref="ResetChecks"/> (41 AT-C7: every row fits 80 cells).</summary>
    public int Clipped { get; private set; }
    public FrontRoomsTerminalFace Face { get; set; }

    public FrontRoomsTerminalGrid(FrontRoomsTerminalFace face) { Face = face; }

    public byte Glyph(int col, int row) => glyph[row * Cols + col];
    public byte Attr(int col, int row) => attr[row * Cols + col];

    public void ResetChecks() { Missing = 0; Clipped = 0; }

    public void Set(int col, int row, int code, byte a)
    {
        if (col < 0 || row < 0 || col >= Cols || row >= Rows) return;
        var i = row * Cols + col;
        var g = (byte)(code & 255);
        if (glyph[i] == g && attr[i] == a) return;
        glyph[i] = g;
        attr[i] = a;
        Version++;
    }

    public void Fill(int col, int row, int width, int height, int code, byte a)
    {
        for (var r = row; r < row + height; r++)
            for (var c = col; c < col + width; c++)
                Set(c, r, code, a);
    }

    public void Clear(byte a) => Fill(0, 0, Cols, Rows, 0x20, a);

    /// <summary>Recolour cells without changing their glyphs (the highlight bar).</summary>
    public void Paint(int col, int row, int width, byte a)
    {
        for (var c = Mathf.Max(0, col); c < Mathf.Min(Cols, col + width); c++)
        {
            var i = row * Cols + c;
            if (attr[i] == a) continue;
            attr[i] = a;
            Version++;
        }
    }

    /// <summary>Write a string from (col, row); clipped at maxWidth and at the row's end. Returns the cells written.</summary>
    public int Put(int col, int row, string s, byte a, int maxWidth = Cols)
    {
        if (string.IsNullOrEmpty(s) || row < 0 || row >= Rows) return 0;
        var n = 0;
        for (var k = 0; k < s.Length; k++)
        {
            var c = col + k;
            if (k >= maxWidth || c >= Cols) { Clipped++; break; }
            var code = FrontRoomsTerminalFace.ToCode(s[k]);
            if (code < 0 || (Face != null && !Face.Drawn(code))) { Missing++; if (code < 0) code = '?'; }
            Set(c, row, code, a);
            n++;
        }
        return n;
    }

    /// <summary>Write a string right-aligned so that its last character sits on column lastCol.</summary>
    public int PutRight(int lastCol, int row, string s, byte a) => string.IsNullOrEmpty(s) ? 0 : Put(lastCol - s.Length + 1, row, s, a);

    /// <summary>Write a string centred on the 80 columns.</summary>
    public int PutCentred(int row, string s, byte a) => string.IsNullOrEmpty(s) ? 0 : Put((Cols - s.Length) / 2, row, s, a);

    /// <summary>
    /// Big type from full blocks (41 section 3.3: "the only way to show large letters in text mode"): each pixel of the
    /// block alphabet is cellsPerPixel full-block cells wide and one row tall, letters one block column apart, centred
    /// on the 80 columns from row top. Returns the width in cells (0 if nothing fits; a string wider than the screen is
    /// clipped and counted).
    /// </summary>
    public int BlockText(string s, FrontRoomsTerminalFace.BlockAlphabet abc, int top, byte a, int cellsPerPixel = 2)
    {
        if (string.IsNullOrEmpty(s) || abc == null || abc.Width == 0) return 0;
        var advance = abc.Width + 1;
        var width = (s.Length * advance - 1) * cellsPerPixel;
        if (width > Cols)
        {
            Clipped++;
            var fit = (Cols / cellsPerPixel + 1) / advance;
            s = s.Substring(0, Mathf.Max(0, fit));
            width = (s.Length * advance - 1) * cellsPerPixel;
            if (s.Length == 0) return 0;
        }
        var left = (Cols - width) / 2;
        for (var k = 0; k < s.Length; k++)
        {
            var ch = s[k];
            if (!abc.Has(ch)) Missing++;
            for (var y = 0; y < abc.Height; y++)
                for (var x = 0; x < abc.Width; x++)
                {
                    if (!abc.Pixel(ch, x, y)) continue;
                    var c0 = left + (k * advance + x) * cellsPerPixel;
                    for (var d = 0; d < cellsPerPixel; d++) Set(c0 + d, top + y, 0xDB, a);
                }
        }
        return width;
    }

    /// <summary>Copy another grid's cells (only real changes move <see cref="Version"/>).</summary>
    public void CopyFrom(FrontRoomsTerminalGrid other)
    {
        for (var i = 0; i < Cols * Rows; i++)
        {
            if (glyph[i] == other.glyph[i] && attr[i] == other.attr[i]) continue;
            glyph[i] = other.glyph[i];
            attr[i] = other.attr[i];
            Version++;
        }
    }

    /// <summary>One row as text (CP437 mapped back to Unicode), trailing spaces trimmed.</summary>
    public string RowText(int row)
    {
        var sb = new StringBuilder(Cols);
        for (var c = 0; c < Cols; c++) sb.Append(FrontRoomsTerminalFace.ToChar(glyph[row * Cols + c]));
        return sb.ToString().TrimEnd();
    }

    /// <summary>The cells as RGBA bytes for the GPU raster: r = glyph, g = attribute (memory row = screen row).</summary>
    public void CopyTo(Color32[] cells)
    {
        for (var i = 0; i < Cols * Rows; i++) cells[i] = new Color32(glyph[i], attr[i], 0, 255);
    }
}

/// <summary>
/// The 16-colour text palette and the colour roles of 41 section 3.4. The values are graphic visual's to set;
/// these are the spec's suggestions. Nothing here is green, amber, yellow or red (20_design section 8.7).
/// </summary>
public static class FrontRoomsTerminalPalette
{
    /// <summary>The VGA text palette, sRGB bytes.</summary>
    public static readonly Color32[] Vga =
    {
        new Color32(0x00, 0x00, 0x00, 255), new Color32(0x00, 0x00, 0xAA, 255), new Color32(0x00, 0xAA, 0x00, 255), new Color32(0x00, 0xAA, 0xAA, 255),
        new Color32(0xAA, 0x00, 0x00, 255), new Color32(0xAA, 0x00, 0xAA, 255), new Color32(0xAA, 0x55, 0x00, 255), new Color32(0xAA, 0xAA, 0xAA, 255),
        new Color32(0x55, 0x55, 0x55, 255), new Color32(0x55, 0x55, 0xFF, 255), new Color32(0x55, 0xFF, 0x55, 255), new Color32(0x55, 0xFF, 0xFF, 255),
        new Color32(0xFF, 0x55, 0x55, 255), new Color32(0xFF, 0x55, 0xFF, 255), new Color32(0xFF, 0xFF, 0x55, 255), new Color32(0xFF, 0xFF, 0xFF, 255),
    };

    /// <summary>FIELD: body text, #AAAAAA on #0000AA (5.7 : 1).</summary>
    public static byte Field = 0x17;
    /// <summary>BRIGHT: headline, labels, messages, the key names in row 25, #FFFFFF on #0000AA (13.3 : 1).</summary>
    public static byte Bright = 0x1F;
    /// <summary>BAR: the highlight bar, inverse video (FIELD's colours swapped).</summary>
    public static byte Bar = 0x71;
    /// <summary>TITLE: row 1, #000000 on #00AAAA (7.3 : 1).</summary>
    public static byte Title = 0x30;
    /// <summary>SOFTKEY: the touch soft keys in row 25 (BAR's colours).</summary>
    public static byte SoftKey = 0x71;

    public static Color32 Fg(byte a) => Vga[a & 15];
    public static Color32 Bg(byte a) => Vga[(a >> 4) & 15];
}
