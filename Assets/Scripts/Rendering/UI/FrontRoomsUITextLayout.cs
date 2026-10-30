using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lays out one line of legacy Text like the motion lab's SVG text (FrontRoomsKeyHud): letter-spacing
/// (tracking, canvas units, added after every character like the browser), the anchor (left of the
/// line, or centred on its full advance incl. tracking, SVG text-anchor middle) at local x = 0, and
/// the BASELINE at local y = 0. The pen start is put on a whole screen pixel so glyphs stay crisp.
/// It must come before Outline on the GameObject, so the keyline copies follow the moved glyphs.
/// Runs only when the Text rebuilds its mesh (string, font, size or canvas scale change); no allocation.
/// </summary>
[RequireComponent(typeof(Text))]
public sealed class FrontRoomsUITextLayout : BaseMeshEffect
{
    public float tracking;
    public bool centre = true;

    /// <summary>The line's advance width (canvas units, tracking included) from the last layout.</summary>
    public float Advance { get; private set; }

    Text text;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;
        if (text == null) text = GetComponent<Text>();
        var s = text.text;
        var font = text.font;
        var quads = vh.currentVertCount / 4;
        if (font == null || string.IsNullOrEmpty(s) || quads == 0) return;
        var ppu = Mathf.Max(.01f, text.pixelsPerUnit);
        var style = text.fontStyle;
        // The size the generator rendered at: fontSize * ppu, rounded either way; the one whose glyphs are in the texture.
        var size = Mathf.RoundToInt(text.fontSize * ppu);
        var first = FirstVisible(s);
        CharacterInfo ci;
        if (first >= 0 && !font.GetCharacterInfo(s[first], out ci, size, style))
        {
            var alt = Mathf.FloorToInt(text.fontSize * ppu);
            if (alt != size && font.GetCharacterInfo(s[first], out ci, alt, style)) size = alt;
        }
        font.RequestCharactersInTexture(s, size, style);
        var visible = 0;
        for (var i = 0; i < s.Length; i++) if (!char.IsWhiteSpace(s[i])) visible++;
        var withSpaces = quads == s.Length && quads != visible;
        // full advance (browser: tracking after every character)
        var total = 0f;
        for (var i = 0; i < s.Length; i++)
        {
            font.GetCharacterInfo(s[i], out ci, size, style);
            total += ci.advance / ppu + tracking;
        }
        Advance = total;
        var start = centre ? -total / 2f : 0f;
        var scale = text.canvas != null ? text.canvas.scaleFactor : ppu;
        start = Mathf.Round(start * scale) / scale;
        var pen = start;
        var q = 0;
        var v = new UIVertex();
        for (var i = 0; i < s.Length && q < quads; i++)
        {
            font.GetCharacterInfo(s[i], out ci, size, style);
            var hasQuad = withSpaces || !char.IsWhiteSpace(s[i]);
            if (hasQuad)
            {
                // the quad's current left and bottom
                var minX = float.MaxValue; var minY = float.MaxValue;
                for (var k = 0; k < 4; k++)
                {
                    vh.PopulateUIVertex(ref v, q * 4 + k);
                    if (v.position.x < minX) minX = v.position.x;
                    if (v.position.y < minY) minY = v.position.y;
                }
                var dx = pen + ci.minX / ppu - minX;
                var dy = ci.minY / ppu - minY;
                for (var k = 0; k < 4; k++)
                {
                    vh.PopulateUIVertex(ref v, q * 4 + k);
                    v.position.x += dx;
                    v.position.y += dy;
                    vh.SetUIVertex(v, q * 4 + k);
                }
                q++;
            }
            pen += ci.advance / ppu + tracking;
        }
    }

    static int FirstVisible(string s)
    {
        for (var i = 0; i < s.Length; i++) if (!char.IsWhiteSpace(s[i])) return i;
        return -1;
    }
}
