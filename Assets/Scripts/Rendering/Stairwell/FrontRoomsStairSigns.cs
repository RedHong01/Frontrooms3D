using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stairwell A (game visual line, C22 look A): the engraved two-ply plates of the information-design line's stair set (SPEC v1.2,
/// ~/FrontRoomsVisualWork/info_type/stair_signs), as textures for the plate meshes.
/// - Fixed plates (stair_letter_A, far_door, not_an_exit) come from their art as it is.
/// - Plates with per-floor slots (rt_exit_door: ON LEVEL {to}; rt_id: {floor}, LEVELS {from} TO {to}) are baked here
///   once per stair k (k = the floor - 1 the stair leaves; token(0) = 0, token(k) = Bk), from glyph sprites laid out
///   exactly as CoreText lays the runs out in the information-design line's generator (advances, pair kerning, tracking; the ink box centred
///   on the slot's centre_x; glyph origins on quarter pixels, floored, as CoreGraphics places them). The bake was
///   checked against the information-design line's out_ref plates for stairs 0-10: albedo within 1/255, mask within 1/255, height within 3/255
///   (stairwell/70_production_a.md, 10-22). Letters take the slot's core colour (albedo), the engraved value (height 123,
///   the art's own) blurred 0.75 px, and the mask.
/// - Every plate's normal map (from the height: 255 = 3.2 mm, SPEC section 5) and its mask map (FrontRooms/Surface _MaskMap:
///   R smoothness 0.55 face / 0.25 engraved, G cavity) are made here too, so fixed and baked plates shade alike.
/// Textures are reference counted per plate and k (Acquire / Release); the last Release destroys them. The reduced
/// Plates are composed at the art's 4 px/mm (so the bake still matches the out_ref gate) and uploaded box-filtered:
/// desktop at 2 px/mm (a 12 x 14 in ID sign is 610 x 712 px, about 3.5 times what the screen shows of it at 1.2 m),
/// the reduced tier (phones, WebGL) at 1 px/mm. At 4 px/mm the six plate sets of one stair took ~86 MB of RGBA32
/// with mips (two stairwells live at once on floors 2 and up: ~150 MB); at 2 px/mm ~21 MB a stair, 1 px/mm ~5 MB.
/// </summary>
public static class FrontRoomsStairSigns
{
    public const string Folder = "Print/Stair/";
    /// <summary>The art's height scale: 255 = 3.2 mm off the surface (SPEC section 5).</summary>
    public const float HeightRangeMM = 3.2f;
    /// <summary>The engraved floor and the face as the art draws them (measured on out_ref: 123 and 128).</summary>
    const float EngravedValue = 123f, FaceValue = 128f;
    const float HeightBlurPx = .75f;
    public const float SmoothFace = .55f, SmoothEngraved = .25f;

    public static string Token(int k) => k <= 0 ? "0" : "B" + k;

    /// <summary>The plate kinds of the set (SPEC section 0).</summary>
    public enum Plate { ExitDoor, IdSign, IdSignFar, FarDoor, NotAnExit, LetterA }

    public sealed class PlateSet
    {
        public Plate plate;
        public int k;
        public Texture2D albedo, normal, mask;
        /// <summary>The plate's FrontRooms/Surface material (mesh UVs in metres, _TileSize = size), owned by the set.</summary>
        public Material material;
        /// <summary>The plate's size in metres (SPEC section 0).</summary>
        public Vector2 size;
        public int refs;
        public string text;
        public bool Alive => albedo != null && normal != null && mask != null;
    }

    static readonly Dictionary<long, PlateSet> live = new Dictionary<long, PlateSet>();

    /// <summary>Live plate sets and the textures they hold (the leak check reads these).</summary>
    public static int LiveSets => live.Count;
    public static int LiveTextures { get { var n = 0; foreach (var s in live.Values) n += (s.albedo != null ? 1 : 0) + (s.normal != null ? 1 : 0) + (s.mask != null ? 1 : 0); return n; } }
    public static int LiveRefs { get { var n = 0; foreach (var s in live.Values) n += s.refs; return n; } }

    /// <summary>The reduced upload (1 px/mm): phones and WebGL. Tools may force it.</summary>
    public static bool Reduced => FrontRoomsStairwellDresser.ReducedTier;

    static long Key(Plate p, int k) => ((long)p << 32) | (uint)(HasSlots(p) ? Mathf.Max(0, k) : 0);
    static bool HasSlots(Plate p) => p == Plate.ExitDoor || p == Plate.IdSign || p == Plate.IdSignFar;

    /// <summary>The plate hangs inside the stairwell (its material takes only the stair's own lights): all but NOT AN
    /// EXIT (the far door's corridor face) and the STAIR A letter (the EXIT door's maze face).</summary>
    public static bool Inside(Plate p) => p != Plate.NotAnExit && p != Plate.LetterA;

    static string ArtName(Plate p)
    {
        switch (p)
        {
            case Plate.ExitDoor: return "rt_exit_door";
            case Plate.IdSign:
            case Plate.IdSignFar: return "rt_id";
            case Plate.FarDoor: return "far_door";
            case Plate.NotAnExit: return "not_an_exit";
            default: return "stair_letter_A";
        }
    }

    /// <summary>The plate's size in metres (SPEC section 0: 11 x 5 in, 12 x 14 in, 12 x 3 in, 6 x 6 in).</summary>
    public static Vector2 SizeOf(Plate p)
    {
        const float In = .0254f;
        switch (p)
        {
            case Plate.ExitDoor:
            case Plate.FarDoor: return new Vector2(11f * In, 5f * In);
            case Plate.IdSign:
            case Plate.IdSignFar: return new Vector2(12f * In, 14f * In);
            case Plate.NotAnExit: return new Vector2(12f * In, 3f * In);
            default: return new Vector2(6f * In, 6f * In);
        }
    }

    /// <summary>The plate's textures for stair k (ignored for fixed plates), baked on first use; Release when done.</summary>
    public static PlateSet Acquire(Plate p, int k)
    {
        var key = Key(p, k);
        if (live.TryGetValue(key, out var set) && set.Alive)
        {
            set.refs++;
            return set;
        }
        set = Bake(p, k);
        if (set == null) return null;
        set.refs = 1;
        live[key] = set;
        return set;
    }

    public static void Release(PlateSet set)
    {
        if (set == null) return;
        set.refs--;
        if (set.refs > 0) return;
        var key = Key(set.plate, set.k);
        if (live.TryGetValue(key, out var s) && s == set) live.Remove(key);
        Kill(set.albedo);
        Kill(set.normal);
        Kill(set.mask);
        Kill(set.material);
        set.albedo = set.normal = set.mask = null;
        set.material = null;
    }

    static void Kill(UnityEngine.Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) UnityEngine.Object.Destroy(o); else UnityEngine.Object.DestroyImmediate(o);
    }

    // ---------------------------------------------------------------- the bake

    [Serializable] sealed class GlyphFace { public string face; public float em_mm, track_mm; public string chars; public float[] advance_mm, ink_mm, kern_mm; public string[] kern_pairs; }
    [Serializable] sealed class GlyphSprite { public string face, ch; public int phase, x, y, w, h, left, bottom; }
    [Serializable] sealed class GlyphSlot
    {
        public string slot, plate, template, colour;
        public float cap_mm, tracking_em, baseline_mm_from_bottom, centre_x_mm, measure_mm, px_per_mm_x, px_per_mm_y, y_phase_px;
        public int phases, atlas_w, atlas_h;
        public GlyphFace[] faces;
        public GlyphSprite[] sprites;
    }

    sealed class SlotData
    {
        public GlyphSlot j;
        public byte[] atlas;
        public readonly Dictionary<string, GlyphSprite> sprites = new Dictionary<string, GlyphSprite>();
    }

    static readonly Dictionary<string, SlotData> slotCache = new Dictionary<string, SlotData>();

    static SlotData LoadSlot(string name)
    {
        if (slotCache.TryGetValue(name, out var d)) return d;
        var json = Resources.Load<TextAsset>(Folder + name + "_glyphs");
        var tex = Resources.Load<Texture2D>(Folder + name + "_glyphs");
        if (json == null || tex == null) { Debug.LogWarning("[StairSigns] glyphs for slot " + name + " are missing"); return null; }
        d = new SlotData { j = JsonUtility.FromJson<GlyphSlot>(json.text) };
        d.atlas = Red(tex);
        if (d.atlas == null || tex.width != d.j.atlas_w || tex.height != d.j.atlas_h) { Debug.LogWarning("[StairSigns] glyph atlas " + name + " is not readable or has the wrong size"); return null; }
        foreach (var s in d.j.sprites) d.sprites[s.face + "|" + s.ch + "|" + s.phase] = s;
        slotCache[name] = d;
        // The source assets are not kept: the bytes are.
        Resources.UnloadAsset(tex);
        return d;
    }

    static byte[] Red(Texture2D t)
    {
        if (t == null || !t.isReadable) return null;
        var px = t.GetPixels32();
        var r = new byte[px.Length];
        for (var i = 0; i < px.Length; i++) r[i] = px[i].r;
        return r;
    }

    // {floor} is the landing's token: the upper one (k) on the exit landing, the lower one (k + 1) on the far landing.
    static string Fill(string template, int k, bool far) => template.Replace("{to}", Token(k + 1)).Replace("{from}", Token(k)).Replace("{floor}", Token(far ? k + 1 : k));

    // One run laid out in a face: the pens (mm), the width without the trailing tracking, and the ink box (mm).
    static bool Layout(GlyphFace f, string text, float[] pens, out float width, out float x0, out float x1)
    {
        width = 0f; x0 = float.PositiveInfinity; x1 = float.NegativeInfinity;
        var pen = 0f;
        for (var i = 0; i < text.Length; i++)
        {
            var c = f.chars.IndexOf(text[i]);
            if (c < 0) return false;
            pens[i] = pen;
            var kern = 0f;
            if (i + 1 < text.Length)
            {
                var pair = text.Substring(i, 2);
                for (var p = 0; p < f.kern_pairs.Length; p++)
                    if (f.kern_pairs[p] == pair) { kern = f.kern_mm[p]; break; }
            }
            var bx0 = f.ink_mm[c * 4];
            var bx1 = f.ink_mm[c * 4 + 2];
            if (bx1 > bx0)
            {
                x0 = Mathf.Min(x0, pen + bx0);
                x1 = Mathf.Max(x1, pen + bx1);
            }
            pen += f.advance_mm[c] + f.track_mm + kern;
        }
        width = pen - f.track_mm;
        return true;
    }

    /// <summary>Adds the slot's run for stair k into cov (W x H, y up, 0..1); returns the run, or null.</summary>
    static string AddSlot(SlotData s, int k, bool far, float[] cov, int W, int H)
    {
        var j = s.j;
        var text = Fill(j.template, k, far);
        var pens = new float[text.Length];
        GlyphFace face = null;
        float width = 0f, x0 = 0f, x1 = 0f;
        foreach (var f in j.faces)
        {
            if (!Layout(f, text, pens, out width, out x0, out x1)) continue;
            face = f;
            // Bold first; the condensed fallback when the run is wider than the measure (SPEC v1.2 section 6).
            if (width <= j.measure_mm) break;
        }
        if (face == null) { Debug.LogWarning("[StairSigns] no face sets '" + text + "'"); return null; }
        var xl = j.centre_x_mm - (x0 + (x1 - x0) * .5f);
        var iy = Mathf.FloorToInt(j.baseline_mm_from_bottom * j.px_per_mm_y);
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == ' ') continue;
            var X = (double)(xl + pens[i]) * j.px_per_mm_x;
            var ix = (int)Math.Floor(X);
            var q = (int)Math.Floor((X - ix) * j.phases);
            if (q >= j.phases) { ix++; q = 0; }
            if (!s.sprites.TryGetValue(face.face + "|" + text[i] + "|" + q, out var sp)) continue;
            var bx = ix + sp.left;
            var by = iy + sp.bottom;
            for (var y = 0; y < sp.h; y++)
            {
                var ty = by + y;
                if (ty < 0 || ty >= H) continue;
                var src = (sp.y + y) * j.atlas_w + sp.x;
                var dst = ty * W;
                for (var x = 0; x < sp.w; x++)
                {
                    var tx = bx + x;
                    if (tx < 0 || tx >= W) continue;
                    var v = s.atlas[src + x];
                    if (v == 0) continue;
                    cov[dst + tx] = Mathf.Min(1f, cov[dst + tx] + v / 255f);
                }
            }
        }
        return text;
    }

    static Color32 Hex(string h)
    {
        ColorUtility.TryParseHtmlString(h, out var c);
        return c;
    }

    /// <summary>The plate's art with stair k's slot runs baked in (y up, the art's resolution).</summary>
    static bool Compose(Plate p, int k, out Color32[] albedo, out byte[] height, out byte[] mask, out int W, out int H, out float pxPerMm, out string text)
    {
        albedo = null; height = mask = null; W = H = 0; pxPerMm = 4f;
        var name = ArtName(p);
        text = name;
        var albedoSrc = Resources.Load<Texture2D>(Folder + name + "_albedo");
        var heightSrc = Resources.Load<Texture2D>(Folder + name + "_height");
        var maskSrc = Resources.Load<Texture2D>(Folder + name + "_mask");
        if (albedoSrc == null || heightSrc == null || maskSrc == null || !albedoSrc.isReadable || !heightSrc.isReadable || !maskSrc.isReadable)
        {
            Debug.LogWarning("[StairSigns] plate art " + name + " is missing or not readable");
            return false;
        }
        W = albedoSrc.width; H = albedoSrc.height;
        albedo = albedoSrc.GetPixels32();
        height = Red(heightSrc);
        mask = Red(maskSrc);
        Resources.UnloadAsset(albedoSrc);
        Resources.UnloadAsset(heightSrc);
        Resources.UnloadAsset(maskSrc);
        pxPerMm = W / (SizeOf(p).x * 1000f);
        if (!HasSlots(p)) return true;
        var cov = new float[W * H];
        var slots = p == Plate.ExitDoor ? new[] { "exit_door_to" } : new[] { "id_floor", "id_terminus" };
        var runs = new List<string>();
        Color32 colour = default;
        foreach (var sn in slots)
        {
            var s = LoadSlot(sn);
            if (s == null) return false;
            pxPerMm = s.j.px_per_mm_x;
            colour = Hex(s.j.colour);
            var run = AddSlot(s, k, p == Plate.IdSignFar, cov, W, H);
            if (run != null) runs.Add(run);
        }
        text = name + " k" + k + ": " + string.Join(" / ", runs);
        // Albedo: the core colour over the cap by coverage (sRGB values, as the art is drawn); mask: max.
        for (var i = 0; i < cov.Length; i++)
        {
            var c = cov[i];
            if (c <= 0f) continue;
            var a = albedo[i];
            albedo[i] = new Color32((byte)Mathf.RoundToInt(a.r + (colour.r - a.r) * c), (byte)Mathf.RoundToInt(a.g + (colour.g - a.g) * c),
                (byte)Mathf.RoundToInt(a.b + (colour.b - a.b) * c), a.a);
            mask[i] = (byte)Mathf.Max(mask[i], Mathf.RoundToInt(c * 255f));
        }
        // Height: the engraved value under the letters, blurred as the art's height is.
        var blurred = Blur(cov, W, H, HeightBlurPx);
        for (var i = 0; i < blurred.Length; i++)
            if (blurred[i] > 0f) height[i] = (byte)Mathf.Clamp(Mathf.RoundToInt(height[i] + (EngravedValue - FaceValue) * blurred[i]), 0, 255);
        return true;
    }

    static PlateSet Bake(Plate p, int k)
    {
        if (!Compose(p, k, out var albedo, out var height, out var mask, out var W, out var H, out var pxPerMm, out var text)) return null;
        var reduce = Reduced ? 4 : 2;
        var set = new PlateSet { plate = p, k = HasSlots(p) ? Mathf.Max(0, k) : 0, size = SizeOf(p), text = text };
        var tag = "StairSign " + ArtName(p) + (p == Plate.IdSignFar ? " far" : "") + " k" + set.k;
        set.albedo = Make(albedo, W, H, reduce, false, tag + " albedo");
        set.normal = MakeNormal(height, W, H, pxPerMm, reduce, tag + " normal");
        set.mask = MakeMask(mask, W, H, reduce, tag + " mask");
        set.material = FrontRoomsStairKit.PlateMaterial(set, Inside(p));
        return set;
    }

    // A separable Gaussian over the coverage (0 outside), radius 3 sigma.
    static float[] Blur(float[] src, int W, int H, float sigma)
    {
        var r = Mathf.CeilToInt(sigma * 3f);
        var k = new float[2 * r + 1];
        var sum = 0f;
        for (var i = -r; i <= r; i++) { k[i + r] = Mathf.Exp(-i * i / (2f * sigma * sigma)); sum += k[i + r]; }
        for (var i = 0; i < k.Length; i++) k[i] /= sum;
        var tmp = new float[src.Length];
        var dst = new float[src.Length];
        for (var y = 0; y < H; y++)
        for (var x = 0; x < W; x++)
        {
            var v = src[y * W + x];
            if (v <= 0f) continue;
            for (var d = -r; d <= r; d++)
            {
                var xx = x + d;
                if (xx >= 0 && xx < W) tmp[y * W + xx] += v * k[d + r];
            }
        }
        for (var y = 0; y < H; y++)
        for (var x = 0; x < W; x++)
        {
            var v = tmp[y * W + x];
            if (v <= 0f) continue;
            for (var d = -r; d <= r; d++)
            {
                var yy = y + d;
                if (yy >= 0 && yy < H) dst[yy * W + x] += v * k[d + r];
            }
        }
        return dst;
    }

    static Texture2D Make(Color32[] px, int W, int H, int reduce, bool linear, string name)
    {
        int w = W / reduce, h = H / reduce;
        var outPx = reduce == 1 ? px : Down(px, W, H, reduce);
        var t = new Texture2D(w, h, TextureFormat.RGBA32, true, linear) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
        t.SetPixels32(outPx);
        t.Apply(true, true);
        return t;
    }

    static Color32[] Down(Color32[] px, int W, int H, int f)
    {
        int w = W / f, h = H / f;
        var o = new Color32[w * h];
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            int r = 0, g = 0, b = 0, a = 0;
            for (var dy = 0; dy < f; dy++)
            for (var dx = 0; dx < f; dx++)
            {
                var c = px[(y * f + dy) * W + x * f + dx];
                r += c.r; g += c.g; b += c.b; a += c.a;
            }
            var n = f * f;
            o[y * w + x] = new Color32((byte)(r / n), (byte)(g / n), (byte)(b / n), (byte)(a / n));
        }
        return o;
    }

    // Tangent-space normal from the height (255 = 3.2 mm), central differences. Packed R = x, G = y, B = z, A = 1:
    // URP's UnpackNormalScale reads it on desktop (RG or AG: x * a) and on phones (RGB) alike.
    static Texture2D MakeNormal(byte[] h, int W, int H, float pxPerMm, int reduce, string name)
    {
        var px = new Color32[W * H];
        var mmPerPx = 1f / Mathf.Max(.1f, pxPerMm);
        var scale = HeightRangeMM / 255f / (2f * mmPerPx);
        for (var y = 0; y < H; y++)
        for (var x = 0; x < W; x++)
        {
            int xl = Mathf.Max(0, x - 1), xr = Mathf.Min(W - 1, x + 1), yd = Mathf.Max(0, y - 1), yu = Mathf.Min(H - 1, y + 1);
            var dx = (h[y * W + xr] - h[y * W + xl]) * scale;
            var dy = (h[yu * W + x] - h[yd * W + x]) * scale;
            var n = new Vector3(-dx, -dy, 1f).normalized;
            px[y * W + x] = new Color32((byte)Mathf.RoundToInt((n.x * .5f + .5f) * 255f), (byte)Mathf.RoundToInt((n.y * .5f + .5f) * 255f),
                (byte)Mathf.RoundToInt((n.z * .5f + .5f) * 255f), 255);
        }
        return Make(px, W, H, reduce, true, name);
    }

    // FrontRooms/Surface _MaskMap: R smoothness (x the material's _Smoothness = SmoothFace), G cavity (1 = none).
    static Texture2D MakeMask(byte[] m, int W, int H, int reduce, string name)
    {
        var px = new Color32[W * H];
        var engraved = SmoothEngraved / SmoothFace;
        for (var i = 0; i < px.Length; i++)
        {
            var e = m[i] / 255f;
            var r = Mathf.Lerp(1f, engraved, e);
            var g = Mathf.Lerp(1f, .82f, e);
            px[i] = new Color32((byte)Mathf.RoundToInt(r * 255f), (byte)Mathf.RoundToInt(g * 255f), 255, 255);
        }
        return Make(px, W, H, reduce, true, name);
    }

    /// <summary>Tools: stair k's plate as baked (albedo, height, mask; y up), for the gate against the information-design line's out_ref.</summary>
    public static bool BakeRawForTools(Plate p, int k, out Color32[] albedo, out byte[] height, out byte[] mask, out int W, out int H, out string text)
        => Compose(p, k, out albedo, out height, out mask, out W, out H, out _, out text);
}
