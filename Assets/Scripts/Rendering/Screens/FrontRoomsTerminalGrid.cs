using System.Text;
using UnityEngine;

/// <summary>
/// VGA text mode's screen buffer (41 section 3.1): 80 x 25 cells of (glyph, attribute). Attribute = background in the high
/// nibble, foreground in the low nibble, indices into <see cref="FrontRoomsTerminalPalette.Vga"/>; there is no blink bit
/// (41 section 3.4: nothing blinks). Every write compares first, so <see cref="Version"/> moves only on a real change: the
/// panel machine reads it to know whether a key changed the screen (the 0.34 s rule counts screen changes).
/// Rows and columns are 0-based here; the spec's row n is index n - 1.
/// Languages (W18, 2026-11-03): a glyph is a slot of the face (0-255 CP437, then the language's, FrontRoomsTerminalFace), and a
/// full-width glyph (CJK, Hangul, full-width punctuation) spans two cells as on the CJK DOS screens: its left half in one
/// cell, its right half (slot + 1) in the next. Widths count cells (<see cref="FrontRoomsTerminalFace.Cells(string)"/>), so a
/// string's clip, right alignment and centring hold in every language. A write over either half of a full-width glyph
/// blanks the other half, so no half-glyph is ever left on the screen.
/// </summary>
public sealed class FrontRoomsTerminalGrid
{
    public const int Cols = 80, Rows = 25;
    /// <summary>A cell's kind: a half-width glyph, or the left or right half of a full-width one.</summary>
    public const byte Half = 0, WideLeft = 1, WideRight = 2;

    readonly ushort[] glyph = new ushort[Cols * Rows];
    readonly byte[] attr = new byte[Cols * Rows];
    readonly byte[] kind = new byte[Cols * Rows];

    /// <summary>Bumped on every change of any cell.</summary>
    public int Version { get; private set; }
    /// <summary>Characters written that the face does not draw, since the last <see cref="ResetChecks"/>.</summary>
    public int Missing { get; private set; }
    /// <summary>Strings clipped at the row's end since the last <see cref="ResetChecks"/> (41 AT-C7: every row fits 80 cells).</summary>
    public int Clipped { get; private set; }
    public FrontRoomsTerminalFace Face { get; set; }

    public FrontRoomsTerminalGrid(FrontRoomsTerminalFace face) { Face = face; }

    /// <summary>The cell's glyph slot (CP437 code below 256).</summary>
    public int Glyph(int col, int row) => glyph[row * Cols + col];
    public byte Attr(int col, int row) => attr[row * Cols + col];
    public byte Kind(int col, int row) => kind[row * Cols + col];

    public void ResetChecks() { Missing = 0; Clipped = 0; }

    // One cell, written as is (the pair rule is the callers').
    void Write(int i, int g, byte a, byte k)
    {
        if (glyph[i] == g && attr[i] == a && kind[i] == k) return;
        glyph[i] = (ushort)g;
        attr[i] = a;
        kind[i] = k;
        Version++;
    }

    // A full-width glyph's other half becomes a blank half-width cell (its colours kept).
    void Unpair(int col, int row)
    {
        var i = row * Cols + col;
        if (kind[i] == WideLeft && col + 1 < Cols) Write(i + 1, 0x20, attr[i + 1], Half);
        else if (kind[i] == WideRight && col > 0) Write(i - 1, 0x20, attr[i - 1], Half);
    }

    /// <summary>A half-width glyph (a CP437 code or a language's half-width slot).</summary>
    public void Set(int col, int row, int code, byte a)
    {
        if (col < 0 || row < 0 || col >= Cols || row >= Rows) return;
        var i = row * Cols + col;
        if (glyph[i] == code && attr[i] == a && kind[i] == Half) return;
        Unpair(col, row);
        Write(i, code, a, Half);
    }

    /// <summary>A full-width glyph whose left half is <paramref name="slot"/> (the right half is slot + 1), at col and col + 1.</summary>
    public void SetWide(int col, int row, int slot, byte a)
    {
        if (col < 0 || row < 0 || col + 1 >= Cols || row >= Rows) return;
        var i = row * Cols + col;
        if (!(glyph[i] == slot && kind[i] == WideLeft)) Unpair(col, row);
        if (!(glyph[i + 1] == slot + 1 && kind[i + 1] == WideRight)) Unpair(col + 1, row);
        Write(i, slot, a, WideLeft);
        Write(i + 1, slot + 1, a, WideRight);
    }

    /// <summary>Copy one cell from another grid (the motion's composite); a half of a full-width glyph comes with its pair.</summary>
    public void CopyCell(int col, int row, FrontRoomsTerminalGrid from)
    {
        if (col < 0 || row < 0 || col >= Cols || row >= Rows || from == null) return;
        var i = row * Cols + col;
        var k = from.kind[i];
        if (k == Half) { Set(col, row, from.glyph[i], from.attr[i]); return; }
        var left = k == WideLeft ? col : col - 1;
        if (left < 0 || left + 1 >= Cols) { Set(col, row, 0x20, from.attr[i]); return; }
        var j = row * Cols + left;
        SetWide(left, row, from.glyph[j], from.attr[j]);
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

    /// <summary>Write a string from (col, row); clipped at maxWidth CELLS and at the row's end (a full-width glyph never split).
    /// Returns the cells written.</summary>
    public int Put(int col, int row, string s, byte a, int maxWidth = Cols)
    {
        if (string.IsNullOrEmpty(s) || row < 0 || row >= Rows) return 0;
        var n = 0;
        for (var k = 0; k < s.Length; k++)
        {
            var ch = s[k];
            int slot;
            bool isWide, drawn;
            var code = FrontRoomsTerminalFace.ToCode(ch);
            if (code >= 0) { slot = code; isWide = false; drawn = Face == null || Face.Drawn(code); }
            else if (Face != null && Face.Lookup(ch, out slot, out isWide)) drawn = true;
            else { slot = '?'; isWide = FrontRoomsTerminalFace.Cells(ch) == 2; drawn = false; }
            var w = isWide ? 2 : 1;
            var c = col + n;
            if (n + w > maxWidth || c + w > Cols) { Clipped++; break; }
            if (!drawn) Missing++;
            if (code >= 0) Set(c, row, code, a);   // a CP437 code the face lacks still goes in (it draws blank), as before W18
            else if (!drawn)
            {
                Set(c, row, '?', a);
                if (w == 2) Set(c + 1, row, 0x20, a);
            }
            else if (isWide) SetWide(c, row, slot, a);
            else Set(c, row, slot, a);
            n += w;
        }
        return n;
    }

    /// <summary>Write a string right-aligned so that its last cell sits on column lastCol.</summary>
    public int PutRight(int lastCol, int row, string s, byte a) => string.IsNullOrEmpty(s) ? 0 : Put(lastCol - FrontRoomsTerminalFace.Cells(s) + 1, row, s, a);

    /// <summary>Write a string centred on the 80 columns.</summary>
    public int PutCentred(int row, string s, byte a) => string.IsNullOrEmpty(s) ? 0 : Put((Cols - FrontRoomsTerminalFace.Cells(s)) / 2, row, s, a);

    /// <summary>
    /// Big type from full blocks (41 section 3.3: "the only way to show large letters in text mode"): each pixel of the
    /// block alphabet is cellsPerPixel full-block cells wide and one row tall, letters one block column apart, centred
    /// on the 80 columns from row top. A language's half-block glyphs (CJK, Hangul; W18) take one column a pixel and two
    /// pixel rows a cell (the upper, lower and full block), one column apart. Returns the width in cells (0 if nothing
    /// fits; a string wider than the screen is clipped and counted).
    /// </summary>
    public int BlockText(string s, FrontRoomsTerminalFace.BlockAlphabet abc, int top, byte a, int cellsPerPixel = 2)
    {
        if (string.IsNullOrEmpty(s) || abc == null || (abc.Width == 0 && abc.HalfWidth == 0)) return 0;
        var width = 0;
        var fit = 0;
        for (var k = 0; k < s.Length; k++)
        {
            var w = Advance(s[k], abc, cellsPerPixel);
            var next = width + (k > 0 ? Gap(s[k - 1], abc, cellsPerPixel) : 0) + w;
            if (next > Cols) { Clipped++; break; }
            width = next;
            fit = k + 1;
        }
        if (fit == 0) return 0;
        s = s.Substring(0, fit);
        var left = (Cols - width) / 2;
        var x0 = left;
        for (var k = 0; k < s.Length; k++)
        {
            var ch = s[k];
            if (k > 0) x0 += Gap(s[k - 1], abc, cellsPerPixel);
            if (!abc.Has(ch)) Missing++;
            if (abc.IsHalf(ch))
            {
                for (var y = 0; y < abc.HalfHeight; y += 2)
                    for (var x = 0; x < abc.HalfWidth; x++)
                    {
                        var up = abc.HalfPixel(ch, x, y);
                        var down = abc.HalfPixel(ch, x, y + 1);
                        if (up || down) Set(x0 + x, top + y / 2, up && down ? 0xDB : up ? 0xDF : 0xDC, a);
                    }
            }
            else
            {
                for (var y = 0; y < abc.Height; y++)
                    for (var x = 0; x < abc.Width; x++)
                    {
                        if (!abc.Pixel(ch, x, y)) continue;
                        var c0 = x0 + x * cellsPerPixel;
                        for (var d = 0; d < cellsPerPixel; d++) Set(c0 + d, top + y, 0xDB, a);
                    }
            }
            x0 += Advance(ch, abc, cellsPerPixel);
        }
        return width;
    }

    // A big glyph's own width in cells and the gap after it (one block column, or one column for a half-block glyph).
    static int Advance(char c, FrontRoomsTerminalFace.BlockAlphabet abc, int cellsPerPixel) => abc.IsHalf(c) ? abc.HalfWidth : abc.Width * cellsPerPixel;
    static int Gap(char c, FrontRoomsTerminalFace.BlockAlphabet abc, int cellsPerPixel) => abc.IsHalf(c) ? 1 : cellsPerPixel;

    /// <summary>Copy another grid's cells (only real changes move <see cref="Version"/>).</summary>
    public void CopyFrom(FrontRoomsTerminalGrid other)
    {
        for (var i = 0; i < Cols * Rows; i++)
        {
            if (glyph[i] == other.glyph[i] && attr[i] == other.attr[i] && kind[i] == other.kind[i]) continue;
            glyph[i] = other.glyph[i];
            attr[i] = other.attr[i];
            kind[i] = other.kind[i];
            Version++;
        }
    }

    /// <summary>One row as text (each glyph back to its character; a full-width glyph once), trailing spaces trimmed.</summary>
    public string RowText(int row)
    {
        var sb = new StringBuilder(Cols);
        for (var c = 0; c < Cols; c++)
        {
            var i = row * Cols + c;
            if (kind[i] == WideRight) continue;
            sb.Append(Face != null ? Face.CharOf(glyph[i]) : FrontRoomsTerminalFace.ToChar(glyph[i]));
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>The cells as RGBA bytes for the GPU raster: r = slot low byte, g = attribute, b = slot high byte, a = the cell's
    /// kind (memory row = screen row).</summary>
    public void CopyTo(Color32[] cells)
    {
        for (var i = 0; i < Cols * Rows; i++) cells[i] = new Color32((byte)(glyph[i] & 255), attr[i], (byte)(glyph[i] >> 8), kind[i]);
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
