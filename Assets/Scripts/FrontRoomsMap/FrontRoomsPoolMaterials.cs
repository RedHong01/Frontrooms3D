using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The pool family's own materials, made at run time and never saved (structures scene, RULES 5b; s67_pool DESIGN section 6).
/// Shared by the map's pool pass (FrontRoomsMapWorld.Pool.cs) and the structures kit. The game's materials are only read
/// (Prop_CeramicGlaze is cloned, never edited). Red's binding look: tile porcelain white (0.93, 0.95, 0.96), smoothness 0.85,
/// real 150 mm joints (the LS_PoolTile150 set the structures scene hands in through SetTileSet; without it a plainer
/// runtime tile, for the day the pool enters the game); stairs porcelain with only a dark nosing (0.24, 0.27, 0.31); level
/// water the "wrong" turquoise, club water clearer. Every lookup that can miss logs once and falls back to a plain material.
/// </summary>
public static class FrontRoomsPoolMaterials
{
    public const string LogTag = "STRUCTURES · ";
    public static readonly Color TileColor = new Color(.93f, .95f, .96f);
    public const float TileSmoothness = .85f;
    /// <summary>The texture sheet's size: 10 x 10 tiles of 0.15 m (the LS_PoolTile150 set).</summary>
    public const float TileSheet = 1.5f;
    public static readonly Color NosingColor = new Color(.24f, .27f, .31f);

    static Texture2D tileBase, tileNormal, tileMask;
    static Texture2D fallbackBase, fallbackMask, rippleNormal, causticTexture;
    static readonly Dictionary<string, Material> made = new Dictionary<string, Material>();
    static readonly HashSet<string> logged = new HashSet<string>();

    /// <summary>The structures scene's tile set (albedo, normal, mask). Null keeps the runtime tile. Materials made before are remade.</summary>
    public static void SetTileSet(Texture2D baseMap, Texture2D normalMap, Texture2D maskMap)
    {
        tileBase = baseMap;
        tileNormal = normalMap;
        tileMask = maskMap;
        Forget("tile");
        Forget("band");
        if (baseMap == null || normalMap == null || maskMap == null) LogOnce("tileset", "the LS_PoolTile150 set is incomplete (" + (baseMap == null ? "A " : "") + (normalMap == null ? "N " : "") + (maskMap == null ? "S" : "") + "): the runtime tile stands in.");
    }

    public static bool HasTileSet => tileBase != null && tileNormal != null && tileMask != null;

    /// <summary>Log a line once per key (the structures scene's "log once and carry on").</summary>
    public static void LogOnce(string key, string message)
    {
        if (logged.Add(key)) Debug.LogWarning("[FrontRooms] " + LogTag + message);
    }

    /// <summary>Destroy every material and texture made here (the scene's OnDestroy).</summary>
    public static void Release()
    {
        foreach (var m in made.Values) Kill(m);
        made.Clear();
        Kill(fallbackBase); Kill(fallbackMask); Kill(rippleNormal); Kill(causticTexture); Kill(chainTexture);
        fallbackBase = fallbackMask = rippleNormal = causticTexture = chainTexture = null;
    }

    static void Forget(string key)
    {
        if (made.TryGetValue(key, out var m)) { Kill(m); made.Remove(key); }
    }

    static void Kill(Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
    }

    static bool Cached(string key, out Material m) => made.TryGetValue(key, out m) && m != null;

    static Material Keep(string key, Material m)
    {
        if (m != null) { m.hideFlags = HideFlags.DontSave; made[key] = m; }
        return m;
    }

    static Shader LitShader()
    {
        var s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (s == null) s = Shader.Find("Standard");
        return s;
    }

    static Shader UnlitShader()
    {
        var s = Shader.Find("Universal Render Pipeline/Unlit");
        if (s == null) s = Shader.Find("Unlit/Color");
        return s;
    }

    // ---------------------------------------------------------------- the tile

    /// <summary>
    /// The pool tile: the game's tile, 游戏视觉's Prop_PoolTile_FieldMesh (FrontRooms/Surface with _FR_TILE, mesh UVs in metres,
    /// porcelain white (0.93, 0.95, 0.96), 150 mm with 3 mm grey-green joints; research/bathroom_rooms/70_look_build.md
    /// sections 4 and 13), so the structures scene and the map draw one tile (research/level_space/73_pool_glaze.md section 7).
    /// Its look (Today, GZ-C, the dish) is FrontRoomsPoolTile.SetLook's, a global switch: no rebuild. Without that material (a
    /// build before pool_tile_real) the runtime clone of Prop_CeramicGlaze on the LS_PoolTile150 set stands in, as before.
    /// </summary>
    public static Material Tile { get { var m = FrontRoomsPoolTile.Get("FieldMesh"); return m != null ? m : LsTile; } }

    /// <summary>The aqua course: Prop_PoolTile_CourseMesh (the same tile in the course's aqua .40/.76/.77, Red's D8 T1), or the
    /// LS set's band as before.</summary>
    public static Material Band { get { var m = FrontRoomsPoolTile.Get("CourseMesh"); return m != null ? m : LsBand; } }

    /// <summary>The structures scene's own tile on the LS_PoolTile150 set (what Tile was before pool_tile_real).</summary>
    public static Material LsTile => TileVariant("tile", TileColor, TileSmoothness);

    /// <summary>The LS set's aqua course (DESIGN section 6, palette F1).</summary>
    public static Material LsBand => TileVariant("band", new Color(.40f, .76f, .77f), TileSmoothness);

    static Material TileVariant(string key, Color color, float smoothness)
    {
        if (Cached(key, out var cached)) return cached;
        var glaze = FrontRoomsSurfaces.TryGet("Prop_CeramicGlaze");
        Material m;
        if (glaze != null && glaze.shader != null && glaze.shader.name == "FrontRooms/Surface") m = new Material(glaze);
        else
        {
            var surface = Shader.Find("FrontRooms/Surface");
            if (surface != null) m = new Material(surface);
            else
            {
                LogOnce("tileshader", "FrontRooms/Surface is missing: the pool tile is a plain URP Lit tile.");
                m = new Material(LitShader());
            }
        }
        m.name = "Structures / pool " + key;
        var a = HasTileSet ? tileBase : FallbackBase();
        var s = HasTileSet ? tileMask : FallbackMask();
        SetTex(m, "_BaseMap", a);
        SetTex(m, "_MainTex", a);
        SetTex(m, "_MaskMap", s);
        if (HasTileSet && m.HasProperty("_BumpMap"))
        {
            m.SetTexture("_BumpMap", tileNormal);
            m.EnableKeyword("_NORMALMAP");
        }
        SetColor(m, "_BaseColor", color);
        SetColor(m, "_Color", color);
        SetFloat(m, "_Smoothness", smoothness);
        SetFloat(m, "_BumpScale", 1f);
        SetFloat(m, "_OcclusionStrength", .8f);
        SetFloat(m, "_Metallic", 0f);
        foreach (var wear in new[] { "_MacroTone", "_MacroDirt", "_StainStrength", "_WetStrength", "_FloorGrime", "_CeilingGrime", "_UseSheen", "_Sheen" }) SetFloat(m, wear, 0f);
        foreach (var k in new[] { "_FR_PRINT", "_FR_LAMP_FIELD", "_FR_OWN_LIGHTS", "_FR_DAMAGE", "_FR_SHEEN", "_EMISSION" }) m.DisableKeyword(k);
        if (m.HasProperty("_TileSize"))
        {
            m.SetVector("_TileSize", new Vector4(TileSheet, TileSheet, 0f, 0f));
            m.mainTextureScale = Vector2.one;
            m.mainTextureOffset = Vector2.zero;
            SetFloat(m, "_MeshUV", 1f);
            m.EnableKeyword("_FR_MESH_UV");
        }
        else
        {
            // URP Lit: mesh UVs are metres, the sheet is 1.5 m.
            m.mainTextureScale = new Vector2(1f / TileSheet, 1f / TileSheet);
        }
        return Keep(key, m);
    }

    static void SetTex(Material m, string p, Texture t) { if (m.HasProperty(p) && t != null) m.SetTexture(p, t); }
    static void SetColor(Material m, string p, Color c) { if (m.HasProperty(p)) m.SetColor(p, c); }
    static void SetFloat(Material m, string p, float v) { if (m.HasProperty(p)) m.SetFloat(p, v); }

    // A plainer tile for a run without the scene's set (1.5 m, 10 x 10 tiles, grey-green joints; mask R 1 tile, 0.3 joint).
    static Texture2D FallbackBase()
    {
        if (fallbackBase != null) return fallbackBase;
        const int n = 600;
        var t = new Texture2D(n, n, TextureFormat.RGBA32, true, false) { name = "Structures / runtime tile A", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 8, hideFlags = HideFlags.DontSave };
        var px = new Color32[n * n];
        var grout = new Color(.72f, .76f, .78f);
        for (var y = 0; y < n; y++)
        for (var x = 0; x < n; x++)
        {
            int tx = x / 60, ty = y / 60, lx = x % 60, ly = y % 60;
            var joint = lx < 2 || ly < 2;
            var tone = 1f + ((Hash(tx, ty) & 255) / 255f - .5f) * .024f;
            var c = joint ? grout : new Color(tone, tone, tone);
            px[x + y * n] = c;
        }
        t.SetPixels32(px);
        t.Apply(true, false);
        return fallbackBase = t;
    }

    static Texture2D FallbackMask()
    {
        if (fallbackMask != null) return fallbackMask;
        const int n = 600;
        var t = new Texture2D(n, n, TextureFormat.RGBA32, true, true) { name = "Structures / runtime tile S", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave };
        var px = new Color32[n * n];
        for (var y = 0; y < n; y++)
        for (var x = 0; x < n; x++)
        {
            var joint = x % 60 < 2 || y % 60 < 2;
            px[x + y * n] = joint ? new Color32(77, 115, 0, 0) : new Color32(255, 255, 0, 0);
        }
        t.SetPixels32(px);
        t.Apply(true, false);
        return fallbackMask = t;
    }

    static uint Hash(int a, int b)
    {
        unchecked
        {
            var h = (uint)(a * 73856093) ^ (uint)(b * 19349663) ^ 0x9e3779b9u;
            h ^= h >> 16; h *= 0x7feb352du; h ^= h >> 15; h *= 0x846ca68bu; h ^= h >> 16;
            return h;
        }
    }

    // ---------------------------------------------------------------- plain lit and unlit

    /// <summary>A plain URP Lit material, made once per key (stand-in greys, the nosing, steel, signs).</summary>
    public static Material Lit(string key, Color color, float smoothness = .3f, float metallic = 0f)
    {
        if (Cached("lit " + key, out var cached)) return cached;
        var shader = LitShader();
        if (shader == null) { LogOnce("lit", "no lit shader found"); return null; }
        var m = new Material(shader) { name = "Structures / " + key };
        SetColor(m, "_BaseColor", color);
        SetColor(m, "_Color", color);
        SetFloat(m, "_Smoothness", smoothness);
        SetFloat(m, "_Glossiness", smoothness);
        SetFloat(m, "_Metallic", metallic);
        return Keep("lit " + key, m);
    }

    /// <summary>An unlit glow (HDR colour): window panels, glass block, the blank exit sign, lens discs.</summary>
    public static Material Glow(string key, Color hdr)
    {
        if (Cached("glow " + key, out var cached)) return cached;
        var shader = UnlitShader();
        if (shader == null) { LogOnce("unlit", "no unlit shader found"); return Lit(key, hdr); }
        var m = new Material(shader) { name = "Structures / glow " + key };
        SetColor(m, "_BaseColor", hdr);
        SetColor(m, "_Color", hdr);
        return Keep("glow " + key, m);
    }

    /// <summary>
    /// A plain transparent URP Lit material (premultiplied, no depth write), made once per key: the slide's translucent
    /// gelcoat, a stand-in glass, a falling water sheet. Emission is switched on (black) so a district can light it at night.
    /// </summary>
    public static Material Transparent(string key, Color color, float smoothness = .8f)
    {
        if (Cached("transparent " + key, out var cached)) return cached;
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) { LogOnce("transparent", "URP Lit is missing: a transparent stand-in is opaque."); return Lit(key, color, smoothness); }
        var m = new Material(shader) { name = "Structures / " + key };
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", smoothness);
        m.SetFloat("_Metallic", 0f);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_AlphaClip", 0f);
        m.SetFloat("_ZWrite", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.One);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        if (m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        m.EnableKeyword("_EMISSION");
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.black);
        m.renderQueue = (int)RenderQueue.Transparent;
        return Keep("transparent " + key, m);
    }

    /// <summary>A lit material with emission on (black until a district drives it), made once per key: lamps' lenses that switch, the slide at night.</summary>
    public static Material Emissive(string key, Color color, float smoothness = .5f)
    {
        if (Cached("emissive " + key, out var cached)) return cached;
        var m = Lit("emissive base " + key, color, smoothness);
        if (m == null) return null;
        m = new Material(m) { name = "Structures / " + key };
        m.EnableKeyword("_EMISSION");
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.black);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        return Keep("emissive " + key, m);
    }

    /// <summary>Galvanised chain-link (E8 mesh guards and the cage): a runtime diamond-wire texture, alpha-clipped, front faces only.</summary>
    public static Material ChainLink()
    {
        if (Cached("chain link", out var cached)) return cached;
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) return Lit("chain link fallback", new Color(.55f, .57f, .58f, 1f), .4f, .6f);
        const int n = 64;
        var t = new Texture2D(n, n, TextureFormat.RGBA32, true, false) { name = "Structures / chain link", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4, hideFlags = HideFlags.DontSave };
        // SP2-F16 (夸张结构's scene 4 review, 11-04): the open texels were black, so every mip pulled the wire toward black
        // and the clip left dark dots at 4-8 m. Every texel now carries the wire's colour (only alpha says where wire is),
        // and each mip's alpha is rescaled until the share of texels over the cutoff equals the full-size level's
        // (coverage-preserving alpha mips, Castano 2010): the mesh keeps its density with distance instead of thinning out.
        const float cutoff = .5f;
        var px = new Color32[n * n];
        var alpha = new float[n * n];
        var covered = 0;
        for (var y = 0; y < n; y++)
        for (var x = 0; x < n; x++)
        {
            // Two diagonal wires per 64 px cell (a 50 mm diamond at 0.05 m a repeat).
            var d1 = Mathf.Abs(((x + y) % n) - n * .5f);
            var d2 = Mathf.Abs(((x - y + n) % n) - n * .5f);
            var wire = Mathf.Min(Mathf.Abs(d1 - n * .5f), Mathf.Abs(d2 - n * .5f)) < 3.2f;
            px[x + y * n] = new Color32(WireRgb.r, WireRgb.g, WireRgb.b, wire ? (byte)255 : (byte)0);
            alpha[x + y * n] = wire ? 1f : 0f;
            if (wire) covered++;
        }
        t.SetPixels32(px, 0);
        var coverage = covered / (float)(n * n);
        for (int mip = 1, size = n; mip < t.mipmapCount; mip++)
        {
            // A 2 x 2 box of the previous level's unscaled alpha, then this level's scale for the base coverage.
            var next = Mathf.Max(1, size / 2);
            var down = new float[next * next];
            for (var y = 0; y < next; y++)
            for (var x = 0; x < next; x++)
            {
                int x0 = Mathf.Min(x * 2, size - 1), x1 = Mathf.Min(x * 2 + 1, size - 1), y0 = Mathf.Min(y * 2, size - 1), y1 = Mathf.Min(y * 2 + 1, size - 1);
                down[x + y * next] = (alpha[x0 + y0 * size] + alpha[x1 + y0 * size] + alpha[x0 + y1 * size] + alpha[x1 + y1 * size]) * .25f;
            }
            var scale = CoverageScale(down, coverage, cutoff);
            var level = new Color32[next * next];
            for (var i = 0; i < level.Length; i++)
                level[i] = new Color32(WireRgb.r, WireRgb.g, WireRgb.b, (byte)Mathf.RoundToInt(Mathf.Clamp01(down[i] * scale) * 255f));
            t.SetPixels32(level, mip);
            alpha = down;
            size = next;
        }
        t.Apply(false, false);
        var m = new Material(shader) { name = "Structures / chain link" };
        m.SetTexture("_BaseMap", t);
        m.mainTextureScale = new Vector2(1f / .05f, 1f / .05f);
        // The wire's albedo is the texel colour alone (about .44 linear, a light zinc grey): at .75 tint over (150, 154, 156)
        // it was .16, and the lit wires read darker than the cage behind them (SP5, 夸张结构's review9, 11-04).
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Smoothness", .45f);
        // Galvanised wire under the zone's cube read dark at .6 (SP2-F16): mostly the zinc's diffuse grey.
        m.SetFloat("_Metallic", .2f);
        m.SetFloat("_AlphaClip", 1f);
        m.SetFloat("_Cutoff", .5f);
        m.EnableKeyword("_ALPHATEST_ON");
        // Every panel is a closed 1 cm solid (Prism, RakeWall, a stand-in cube), so the near face is always the one facing the eye.
        // Drawn two-sided, the far face showed its wire lit through its back: a black twin 1 cm behind every wire up close.
        if (m.HasProperty("_Cull")) m.SetFloat("_Cull", (float)CullMode.Back);
        m.renderQueue = (int)RenderQueue.AlphaTest;
        chainTexture = t;
        return Keep("chain link", m);
    }

    static Texture2D chainTexture;
    static readonly Color32 WireRgb = new Color32(176, 180, 182, 255);

    // The alpha scale that keeps the share of texels at or over the cutoff nearest the target (Castano's binary search).
    static float CoverageScale(float[] alpha, float target, float cutoff)
    {
        float lo = 0f, hi = 64f;
        for (var k = 0; k < 24; k++)
        {
            var mid = (lo + hi) * .5f;
            if (Coverage(alpha, mid, cutoff) < target) lo = mid; else hi = mid;
        }
        // hi is the least scale that reaches the target and lo just misses it: keep the nearer.
        return Mathf.Abs(Coverage(alpha, hi, cutoff) - target) <= Mathf.Abs(Coverage(alpha, lo, cutoff) - target) ? hi : lo;
    }

    static float Coverage(float[] alpha, float scale, float cutoff)
    {
        var count = 0;
        foreach (var a in alpha) if (a * scale >= cutoff) count++;
        return count / (float)alpha.Length;
    }

    /// <summary>Dark nosing on every pool step (50 mm on the tread, 25 mm down the riser).</summary>
    public static Material Nosing => Lit("nosing", NosingColor, .35f);

    /// <summary>Stainless steel for rails, ladders, bars and pipes: the game's own (Prop_StainlessSatin), else a plain steel.</summary>
    public static Material Steel
    {
        get
        {
            var m = FrontRoomsSurfaces.TryGet("Prop_StainlessSatin");
            return m != null ? m : Lit("steel", new Color(.62f, .64f, .66f), .55f, .9f);
        }
    }

    /// <summary>One of the game's surfaces by name (never edited), else a plain lit stand-in in the given colour (logged once).</summary>
    public static Material Surface(string name, Color fallback)
    {
        var m = FrontRoomsSurfaces.TryGet(name);
        if (m != null) return m;
        LogOnce("surface " + name, "surface '" + name + "' is missing: a plain stand-in is used.");
        return Lit("missing " + name, fallback, .2f);
    }

    // ---------------------------------------------------------------- water and caustics

    public enum Water { Level, Club, Dark }

    /// <summary>
    /// Water: URP Lit transparent (premultiplied, as the map's glass), smoothness 0.95, a runtime ripple normal at one
    /// repeat per 2 m (meshes carry UVs in metres). Level: the "wrong" turquoise (0.16, 0.60, 0.62) at alpha 0.36; club:
    /// clearer (physical, Red Q1); dark: (0.10, 0.45, 0.50).
    /// </summary>
    public static Material WaterOf(Water kind)
    {
        var key = "water " + kind;
        if (Cached(key, out var cached)) return cached;
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) { LogOnce("water", "URP Lit is missing: the water is opaque."); return Lit("water fallback", new Color(.16f, .60f, .62f), .9f); }
        var color = kind == Water.Club ? new Color(.30f, .68f, .70f, .22f) : kind == Water.Dark ? new Color(.10f, .45f, .50f, .36f) : new Color(.16f, .60f, .62f, .36f);
        var m = new Material(shader) { name = "Structures / " + key };
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", .95f);
        m.SetFloat("_Metallic", 0f);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_AlphaClip", 0f);
        m.SetFloat("_ZWrite", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.One);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        if (m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = (int)RenderQueue.Transparent;
        m.SetTexture("_BumpMap", RippleNormal());
        m.SetFloat("_BumpScale", kind == Water.Club ? .35f : .5f);
        m.EnableKeyword("_NORMALMAP");
        m.mainTextureScale = new Vector2(.5f, .5f);
        return Keep(key, m);
    }

    // A tileable sum of sines (256 px), packed so both of URP's normal unpacks (RGB, and AG with a = 1) read it.
    static Texture2D RippleNormal()
    {
        if (rippleNormal != null) return rippleNormal;
        const int n = 256;
        var t = new Texture2D(n, n, TextureFormat.RGBA32, true, true) { name = "Structures / ripple N", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4, hideFlags = HideFlags.DontSave };
        var px = new Color32[n * n];
        var tau = Mathf.PI * 2f;
        for (var y = 0; y < n; y++)
        for (var x = 0; x < n; x++)
        {
            float u = x / (float)n, v = y / (float)n;
            // Derivatives of h = sum a sin(2 pi (kx u + ky v) + p): whole-number k keep it tileable.
            float dx = 0f, dy = 0f;
            Wave(u, v, 3, 1, .020f, .3f, tau, ref dx, ref dy);
            Wave(u, v, -2, 4, .016f, 1.7f, tau, ref dx, ref dy);
            Wave(u, v, 5, -3, .010f, 4.1f, tau, ref dx, ref dy);
            Wave(u, v, 1, 7, .006f, 2.2f, tau, ref dx, ref dy);
            var nrm = new Vector3(-dx, -dy, 1f).normalized;
            px[x + y * n] = new Color32((byte)(nrm.x * 127.5f + 127.5f), (byte)(nrm.y * 127.5f + 127.5f), (byte)(nrm.z * 127.5f + 127.5f), 255);
        }
        t.SetPixels32(px);
        t.Apply(true, false);
        return rippleNormal = t;
    }

    static void Wave(float u, float v, int kx, int ky, float a, float phase, float tau, ref float dx, ref float dy)
    {
        var c = Mathf.Cos(tau * (kx * u + ky * v) + phase) * a * tau;
        dx += c * kx;
        dy += c * ky;
    }

    /// <summary>
    /// Greybox caustics (DESIGN section 6, PLAN 2.6): a runtime cell-web texture on URP Unlit, additive, for quads 8 mm under
    /// ceilings over water; UVs in metres / 2.4. Animate drifts its offset (two scrolls). Listed on every sign as a greybox placeholder.
    /// </summary>
    public static Material Caustic(bool dark)
    {
        var key = dark ? "caustic dark" : "caustic";
        if (Cached(key, out var cached)) return cached;
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) { LogOnce("caustic", "URP Unlit is missing: no caustics."); return null; }
        var m = new Material(shader) { name = "Structures / " + key };
        var level = dark ? 1f : .6f;
        m.SetColor("_BaseColor", new Color(.32f * level, .50f * level, .50f * level, 1f));
        m.SetTexture("_BaseMap", CausticTexture());
        m.mainTextureScale = new Vector2(1f / 2.4f, 1f / 2.4f);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 2f);
        m.SetFloat("_ZWrite", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.One);
        m.SetFloat("_DstBlend", (float)BlendMode.One);
        if (m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha", (float)BlendMode.Zero);
        if (m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha", (float)BlendMode.One);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent + 1;
        return Keep(key, m);
    }

    /// <summary>Drift the caustic textures (called once a frame by whoever shows water; cheap: two offsets).</summary>
    public static void Animate(float time)
    {
        if (Cached("caustic", out var a)) a.mainTextureOffset = new Vector2(time * .021f, time * .013f);
        if (Cached("caustic dark", out var b)) b.mainTextureOffset = new Vector2(-time * .017f, time * .011f);
    }

    // Distance to the nearest cell edge of a tileable Worley field, as a bright web.
    static Texture2D CausticTexture()
    {
        if (causticTexture != null) return causticTexture;
        const int n = 256, cells = 6;
        var t = new Texture2D(n, n, TextureFormat.RGBA32, true, false) { name = "Structures / caustic", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave };
        var pts = new Vector2[cells * cells];
        for (var j = 0; j < cells; j++)
        for (var i = 0; i < cells; i++)
        {
            var h = Hash(i + 11, j + 7);
            pts[i + j * cells] = new Vector2((i + .15f + .7f * ((h & 255) / 255f)) / cells, (j + .15f + .7f * (((h >> 8) & 255) / 255f)) / cells);
        }
        var px = new Color32[n * n];
        for (var y = 0; y < n; y++)
        for (var x = 0; x < n; x++)
        {
            var p = new Vector2((x + .5f) / n, (y + .5f) / n);
            float d1 = 9f, d2 = 9f;
            for (var k = 0; k < pts.Length; k++)
            for (var oy = -1; oy <= 1; oy++)
            for (var ox = -1; ox <= 1; ox++)
            {
                var q = pts[k] + new Vector2(ox, oy);
                var d = (q - p).magnitude;
                if (d < d1) { d2 = d1; d1 = d; } else if (d < d2) d2 = d;
            }
            var edge = Mathf.Clamp01(1f - (d2 - d1) * cells * 3.2f);
            var b = (byte)(Mathf.Pow(edge, 3f) * 255f);
            px[x + y * n] = new Color32(b, b, b, 255);
        }
        t.SetPixels32(px);
        t.Apply(true, false);
        return causticTexture = t;
    }
}
