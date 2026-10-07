using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Anti-aliased sprites for the touch layer, drawn at the device's pixel
/// density (points × FrontRoomsHandheld.PointScale) so a 2 pt ring is a crisp
/// 6 px ring on a @3x iPhone. The shapes are the desktop HUD kit's: discs,
/// rings (the keycap outline and the hold ring), a rounded keycap and the
/// pause card's legend glyphs. Cached by size.
/// </summary>
public static class FrontRoomsTouchSprites
{
    public enum Glyph { Move, Drag, Socket, Use, Hold, SprintButton }

    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    static float Density => Mathf.Clamp(FrontRoomsHandheld.PointScale, 1f, 4f);

    /// <summary>A filled disc <paramref name="diameter"/> points across.</summary>
    public static Sprite Disc(float diameter) => Ring(diameter, 0f);

    /// <summary>A ring whose stroke sits inside the diameter (Figma's inside stroke). A stroke of 0 fills it.</summary>
    public static Sprite Ring(float diameter, float stroke)
    {
        var px = Mathf.Clamp(Mathf.CeilToInt(diameter * Density) + 2, 8, 1024);
        var strokePx = stroke * Density;
        var key = "ring:" + px + ":" + Mathf.RoundToInt(strokePx * 10f);
        if (cache.TryGetValue(key, out var cached) && cached != null) return cached;
        var tex = NewTexture(px, px, key);
        var pixels = new Color32[px * px];
        var c = (px - 1) * .5f;
        var outer = diameter * Density * .5f;
        var inner = stroke > 0f ? outer - strokePx : -1f;
        for (var y = 0; y < px; y++)
        for (var x = 0; x < px; x++)
        {
            var d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
            var a = Mathf.Clamp01(outer - d + .5f);
            if (inner >= 0f) a *= Mathf.Clamp01(d - inner + .5f);
            pixels[y * px + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
        }
        return Finish(tex, pixels, key);
    }

    /// <summary>A rounded rectangle, filled or (stroke &gt; 0) outlined, as a 9-slice sprite.</summary>
    public static Sprite RoundRect(float radius, float stroke)
    {
        var r = Mathf.Max(1f, radius * Density);
        var s = stroke * Density;
        var px = Mathf.CeilToInt(r * 2f + 4f);
        var key = "rrect:" + px + ":" + Mathf.RoundToInt(r * 10f) + ":" + Mathf.RoundToInt(s * 10f);
        if (cache.TryGetValue(key, out var cached) && cached != null) return cached;
        var tex = NewTexture(px, px, key);
        var pixels = new Color32[px * px];
        var half = px * .5f;
        for (var y = 0; y < px; y++)
        for (var x = 0; x < px; x++)
        {
            var dx = Mathf.Max(Mathf.Abs(x + .5f - half) - (half - r), 0f);
            var dy = Mathf.Max(Mathf.Abs(y + .5f - half) - (half - r), 0f);
            var d = Mathf.Sqrt(dx * dx + dy * dy);
            var a = Mathf.Clamp01(r - d + .5f);
            if (s > 0f) a *= Mathf.Clamp01(d - (r - s) + .5f);
            pixels[y * px + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
        }
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        var border = Mathf.Floor(half - 1f);
        // 100 × density pixels per unit: a sliced border of n px is n / density points on a canvas at the default 100 reference.
        var sprite = Sprite.Create(tex, new Rect(0, 0, px, px), new Vector2(.5f, .5f), 100f * Density, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        sprite.name = key;
        cache[key] = sprite;
        return sprite;
    }

    /// <summary>A white square for washes, rules and dividers.</summary>
    public static Sprite Solid()
    {
        const string key = "solid";
        if (cache.TryGetValue(key, out var cached) && cached != null) return cached;
        var tex = NewTexture(4, 4, key);
        var pixels = new Color32[16];
        for (var i = 0; i < 16; i++) pixels[i] = new Color32(255, 255, 255, 255);
        return Finish(tex, pixels, key);
    }

    /// <summary>A vertical fade (opaque at the bottom) for the scroll edge of the settings column.</summary>
    public static Sprite FadeUp()
    {
        const string key = "fadeup";
        if (cache.TryGetValue(key, out var cached) && cached != null) return cached;
        var tex = NewTexture(4, 64, key);
        var pixels = new Color32[4 * 64];
        for (var y = 0; y < 64; y++)
        {
            var a = (byte)Mathf.RoundToInt((1f - y / 63f) * 255f);
            for (var x = 0; x < 4; x++) pixels[y * 4 + x] = new Color32(255, 255, 255, a);
        }
        return Finish(tex, pixels, key);
    }

    /// <summary>The pause card's legend glyphs (22 pt, drawn in white, tinted ink by the view).</summary>
    public static Sprite GlyphSprite(Glyph glyph)
    {
        const float size = 22f;
        var px = Mathf.CeilToInt(size * Density);
        var key = "glyph:" + glyph + ":" + px;
        if (cache.TryGetValue(key, out var cached) && cached != null) return cached;
        var tex = NewTexture(px, px, key);
        var pixels = new Color32[px * px];
        var k = px / size;
        for (var y = 0; y < px; y++)
        for (var x = 0; x < px; x++)
        {
            // Points with y up from the glyph's bottom.
            var p = new Vector2((x + .5f) / k, (y + .5f) / k);
            var a = GlyphCoverage(glyph, p, 1f / k);
            pixels[y * px + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
        }
        return Finish(tex, pixels, key);
    }

    static float RingCov(Vector2 p, Vector2 c, float radius, float stroke, float aa)
    {
        var d = (p - c).magnitude;
        return Mathf.Clamp01((radius - d) / aa + .5f) * Mathf.Clamp01((d - (radius - stroke)) / aa + .5f);
    }

    static float DiscCov(Vector2 p, Vector2 c, float radius, float aa) => Mathf.Clamp01((radius - (p - c).magnitude) / aa + .5f);

    static float GlyphCoverage(Glyph glyph, Vector2 p, float aa)
    {
        var c = new Vector2(11f, 11f);
        switch (glyph)
        {
            case Glyph.Move:
                return Mathf.Max(RingCov(p, c, 10f, 2f, aa), DiscCov(p, c + new Vector2(3f, 3f), 4f, aa));
            case Glyph.Drag:
            {
                var line = Mathf.Clamp01((1f - Mathf.Abs(p.y - 11f)) / aa + .5f) * (p.x > 3f && p.x < 19f ? 1f : 0f);
                return Mathf.Max(line, Mathf.Max(DiscCov(p, new Vector2(4f, 11f), 3.5f, aa), DiscCov(p, new Vector2(19f, 11f), 2.5f, aa) * .45f));
            }
            case Glyph.Socket:
                return Mathf.Max(RingCov(p, new Vector2(11f, 7f), 6.5f, 1.5f, aa) * .4f, Mathf.Max(RingCov(p, new Vector2(11f, 17.5f), 4f, 1.5f, aa), DiscCov(p, new Vector2(11f, 17.5f), 2.25f, aa)));
            case Glyph.Use:
                return Mathf.Max(RingCov(p, c, 10f, 2f, aa), DiscCov(p, c, 3f, aa));
            case Glyph.Hold:
            {
                var track = RingCov(p, c, 9f, 2.5f, aa) * .22f;
                // The progress arc: clockwise from the top over 60 % of the turn.
                var ang = Mathf.Atan2(p.x - c.x, p.y - c.y);
                if (ang < 0f) ang += Mathf.PI * 2f;
                var arc = ang <= .6f * Mathf.PI * 2f ? RingCov(p, c, 9f, 2.5f, aa) : 0f;
                return Mathf.Max(track, arc);
            }
            case Glyph.SprintButton:
                return RingCov(p, c, 10f, 2f, aa);
        }
        return 0f;
    }

    static Texture2D NewTexture(int w, int h, string name)
    {
        return new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            name = "FrontRooms touch " + name,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave
        };
    }

    static Sprite Finish(Texture2D tex, Color32[] pixels, string key)
    {
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f), 100f * Density, 0, SpriteMeshType.FullRect);
        sprite.name = key;
        sprite.hideFlags = HideFlags.DontSave;
        cache[key] = sprite;
        return sprite;
    }
}
