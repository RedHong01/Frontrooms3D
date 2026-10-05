using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Glass materials (VISUAL_CHAT_TASKS G1-G4; audit 10 §4.1-4.2). Re-running updates
/// the values in place and keeps every asset GUID.
///   Glass_Window    FrontRooms/Glass with grime: the map's window pane. The map loads it
///                   by name from Resources/Surfaces (the map chat switches to it).
///   Glass_Edge      URP Lit opaque green float-glass edge, linear (.28, .42, .34), smoothness .6.
///   Glass_Shard     URP Lit opaque, base 0.85 x the Level 0 carpet albedo, smoothness .95 (shard
///                   faces; give the shard mesh's edge faces Glass_Edge). URP Lit keeps a MotionVectors pass.
///   Glass_ShardClear FrontRooms/Glass, no grime: clear shard faces for desktop debris (edges: Glass_Edge).
///   Prop_Glass      moved onto FrontRooms/Glass (no grime): kit glazing (hutch, cabinet,
///                   clock, vending front, phone).
///   Prop_BottleBlue moved onto FrontRooms/Glass (no grime): the water-cooler bottle, tinted.
/// Grime maps: Textures/GlassGrime_M.png and GlassSmear_N.png, packed from the CC0
/// ambientCG scans by Tools/lookdev/pack_glass_grime.py; this sets their importers.
/// FrontRoomsRenderSetup.SetUp calls EnsureAll after its own glass pass (one-line hook),
/// so re-running the URP setup keeps these.
/// Menu: FrontRoomsss → Rendering → Set up glass materials
/// Batch: -executeMethod FrontRoomsGlassSetup.RunBatch
/// </summary>
public static class FrontRoomsGlassSetup
{
    public const string SurfaceDir = "Assets/Resources/Surfaces";
    public const string TextureDir = SurfaceDir + "/Textures";
    public const string GrimePath = TextureDir + "/GlassGrime_M.png";
    public const string SmearNormalPath = TextureDir + "/GlassSmear_N.png";
    public const string GlassShaderName = "FrontRooms/Glass";

    // Linear values from the audit (10 §4.1-4.2); written as sRGB swatches (Color.gamma)
    // because material colours are gamma-space and URP linearises them.
    public static readonly Color FaceLinear = new Color(.02f, .025f, .022f);
    public static readonly Color DustLinear = new Color(.42f, .40f, .34f);
    public static readonly Color EdgeLinear = new Color(.28f, .42f, .34f);
    // Opaque shard faces (WebGL / far shards): 0.85 x the Level 0 carpet's mean albedo (Carpet_LoopPile_A.png,
    // mean sRGB .539/.473/.326 = linear .252/.190/.087). The audit's near-black base read as black chips on the
    // carpet (G10 run 3: 97.6 % of shard pixels darker than 0.6 x the carpet). Other floors need their own
    // instance (Office carpet, Run VCT: 30_final.md); desktop shards should use the transparent glass shader.
    public static readonly Color ShardLinear = new Color(.214f, .162f, .074f);

    // Reflection floor on glass only, linear. The zone cubes were captured under the game's own
    // lamps, so 1.0 (the full captured light) is the physical value; the world's default stays at
    // FrontRoomsZoneReflection.MaxLinear (0.5) until per-room probes land (G7).
    public const float WindowReflectionMin = 1f;
    public const float PropReflectionMin = 1f;

    // Reflectance face-on of a pane, both surfaces (n 1.52, incoherent): 0.082. Alpha = absorption
    // (~.03) + reflectance, rising with Fresnel^5, so what the pane reflects is taken from the view
    // behind it (transmission .89 face-on, about .86 at 50° and .69 at 75°).
    public const float PaneF0 = .08f;

    [MenuItem("FrontRoomsss/Rendering/Set up glass materials")]
    public static void SetUp()
    {
        EnsureAll();
        AssetDatabase.SaveAssets();
        Debug.Log("[FrontRoomsGlass] Glass_Window, Glass_Edge, Glass_Shard, Glass_ShardClear, Prop_Glass and Prop_BottleBlue are up to date.");
        FrontRoomsReflectionCapture.WarnIfStale();
    }

    public static void RunBatch() => SetUp();

    public static void EnsureAll()
    {
        ImportTextures();
        var glass = Shader.Find(GlassShaderName);
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        if (glass == null || lit == null)
        {
            Debug.LogError("[FrontRoomsGlass] Missing shader " + (glass == null ? GlassShaderName : "Universal Render Pipeline/Lit") + "; glass materials not generated.");
            return;
        }
        var grime = AssetDatabase.LoadAssetAtPath<Texture2D>(GrimePath);
        var smear = AssetDatabase.LoadAssetAtPath<Texture2D>(SmearNormalPath);
        if (grime == null || smear == null)
            Debug.LogWarning("[FrontRoomsGlass] Grime maps missing; run Tools/lookdev/pack_glass_grime.py. Glass_Window is made without them.");

        var window = Ensure("Glass_Window", glass);
        Reset(window);
        SetGlass(window, FaceLinear.gamma, .11f, .89f, .96f, .02f);
        window.SetFloat("_Grime", 1f);
        window.EnableKeyword("_FR_GLASS_GRIME");
        window.SetTexture("_GrimeMap", grime);
        window.SetTexture("_SmearNormal", smear);
        window.SetFloat("_SmudgeNormal", .05f);
        window.SetColor("_DustColor", DustLinear.gamma);
        window.SetFloat("_DustAlpha", .25f);
        window.SetFloat("_Dust", 1f);
        window.SetFloat("_Smear", 1f);
        window.SetFloat("_Prints", 1f);
        window.SetFloat("_FloorY", 0f);
        window.SetVector("_PaneSize", Vector4.zero);
        window.SetFloat("_AutoEdge", 1f);
        window.SetColor("_EdgeColor", EdgeLinear.gamma);
        window.SetFloat("_EdgeAlpha", .92f);
        window.SetFloat("_EdgeSmoothness", .6f);
        // Dust and smudges scatter the room light from both sides; at most ~1 (3 was a flat veil).
        window.SetFloat("_Scatter", 1f);
        window.SetFloat("_ReflectionMin", WindowReflectionMin);
        window.SetFloat("_RTReceive", 1f);   // G14: only window panes take _FR_GlassRTReflection
        Done(window);

        var edge = Ensure("Glass_Edge", lit);
        Opaque(edge, EdgeLinear.gamma, .6f);

        var shard = Ensure("Glass_Shard", lit);
        Opaque(shard, ShardLinear.gamma, .95f);

        // Kit glazing: clear glass without grime (no pane frame on a prop mesh).
        var prop = Ensure("Prop_Glass", glass);
        Reset(prop);
        SetGlass(prop, FaceLinear.gamma, .12f, .88f, .94f, 0f);
        prop.SetFloat("_AutoEdge", 0f);
        // Case goods: an even light dust film that the room light catches (the grime layout needs a pane
        // frame, which a kit mesh does not have); without it clear glass over a dark interior vanishes.
        prop.SetFloat("_DustFilm", .12f);
        prop.SetFloat("_Scatter", .5f);
        prop.SetFloat("_ReflectionMin", PropReflectionMin);
        Done(prop);

        // Clear shard faces for desktop debris (G10 run 3, variant S3: reads as glass on Level 0 and Office carpet,
        // shard/carpet luminance 0.96-0.98, where the opaque Glass_Shard needs a colour per floor). Transparent, so
        // GD3 decides where sorting/overdraw allow it; edges keep Glass_Edge.
        var shardClear = Ensure("Glass_ShardClear", glass);
        Reset(shardClear);
        SetGlass(shardClear, FaceLinear.gamma, .12f, .88f, .94f, 0f);
        shardClear.SetFloat("_AutoEdge", 0f);
        shardClear.SetFloat("_ReflectionMin", PropReflectionMin);
        Done(shardClear);

        // Water-cooler bottle: keeps its blue (sRGB .36/.58/.80) as a tint that is lit and
        // laid over the water behind; the old flat .42 veil becomes .30 face-on + Fresnel.
        var bottle = Ensure("Prop_BottleBlue", glass);
        Reset(bottle);
        SetGlass(bottle, new Color(.36f, .58f, .80f), .30f, .70f, .90f, 0f);
        bottle.SetFloat("_AutoEdge", 0f);
        bottle.SetFloat("_ReflectionMin", PropReflectionMin);
        Done(bottle);
    }

    static Material Ensure(string name, Shader shader)
    {
        var path = SurfaceDir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(m, path);
        }
        if (m.shader != shader) m.shader = shader;
        return m;
    }

    // Drop keywords and tags left by an earlier shader (URP Lit's transparent set).
    static void Reset(Material m)
    {
        m.shaderKeywords = Array.Empty<string>();
        m.SetOverrideTag("RenderType", "");
        foreach (var pass in new[] { "MOTIONVECTORS", "DepthOnly", "SHADOWCASTER" }) m.SetShaderPassEnabled(pass, true);
        foreach (var t in new[] { "_GrimeMap", "_SmearNormal" }) m.SetTexture(t, null);
        m.SetFloat("_Grime", 0f);
        m.SetFloat("_Crack", 0f);
        m.SetFloat("_Palm", 0f);
        m.SetFloat("_GrimeDebug", 0f);
        m.SetFloat("_Scatter", 0f);
        m.SetFloat("_ReflectionMin", 0f);
        m.SetFloat("_RTReceive", 0f);
        m.SetFloat("_DustFilm", 0f);
        m.SetFloat("_CrackSeed", 0f);
        m.SetVector("_ImpactUV", new Vector4(.5f, .5f, 0f, 0f));
        m.SetFloat("_Cull", (float)CullMode.Back);
    }

    static void SetGlass(Material m, Color baseSrgb, float alphaFace, float alphaFresnel, float smoothness, float roll)
    {
        m.SetColor("_BaseColor", new Color(baseSrgb.r, baseSrgb.g, baseSrgb.b, 1f));
        m.SetFloat("_AlphaFace", alphaFace);
        m.SetFloat("_AlphaFresnel", alphaFresnel);
        m.SetFloat("_Smoothness", smoothness);
        m.SetFloat("_SmudgeSmoothness", .62f);
        m.SetFloat("_Metallic", 0f);
        m.SetFloat("_PaneF0", PaneF0);
        m.SetFloat("_RollStrength", roll);
        m.SetFloat("_RollPeriod", .37f);
    }

    static void Done(Material m)
    {
        m.renderQueue = (int)RenderQueue.Transparent;
        m.enableInstancing = false;
        EditorUtility.SetDirty(m);
    }

    static void Opaque(Material m, Color baseSrgb, float smoothness)
    {
        m.shaderKeywords = Array.Empty<string>();
        m.SetFloat("_Surface", 0f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_AlphaClip", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.One);
        m.SetFloat("_DstBlend", (float)BlendMode.Zero);
        m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        m.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
        m.SetFloat("_ZWrite", 1f);
        m.SetFloat("_Cull", (float)CullMode.Back);
        m.SetFloat("_WorkflowMode", 1f);
        m.SetFloat("_Metallic", 0f);
        m.SetFloat("_Smoothness", smoothness);
        m.SetFloat("_EnvironmentReflections", 1f);
        m.SetFloat("_SpecularHighlights", 1f);
        m.SetFloat("_ReceiveShadows", 1f);
        m.SetColor("_BaseColor", new Color(baseSrgb.r, baseSrgb.g, baseSrgb.b, 1f));
        m.SetOverrideTag("RenderType", "Opaque");
        m.renderQueue = (int)RenderQueue.Geometry;
        foreach (var pass in new[] { "MOTIONVECTORS", "DepthOnly", "SHADOWCASTER", "DepthNormals" }) m.SetShaderPassEnabled(pass, true);
        EditorUtility.SetDirty(m);
    }

    /// <summary>Importer settings for the two packed grime maps (desktop: BC7 / BC5; WebGL: half size).</summary>
    public static void ImportTextures()
    {
        Configure(GrimePath, false, 1024, 512);
        Configure(SmearNormalPath, true, 512, 256);
    }

    static void Configure(string path, bool normal, int size, int webSize)
    {
        if (!File.Exists(path)) return;
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) { AssetDatabase.ImportAsset(path); importer = AssetImporter.GetAtPath(path) as TextureImporter; }
        if (importer == null) return;
        var before = EditorJsonUtility.ToJson(importer);
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.sRGBTexture = false;
        importer.alphaSource = normal ? TextureImporterAlphaSource.None : TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = false;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Trilinear;
        importer.anisoLevel = 4;
        importer.maxTextureSize = size;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        var web = importer.GetPlatformTextureSettings("WebGL");
        web.overridden = true;
        web.maxTextureSize = webSize;
        web.textureCompression = TextureImporterCompression.Compressed;
        web.format = TextureImporterFormat.Automatic;
        importer.SetPlatformTextureSettings(web);
        if (EditorJsonUtility.ToJson(importer) != before) importer.SaveAndReimport();
        Debug.Log("[FrontRoomsGlass] " + Path.GetFileName(path) + ": Standalone " + importer.GetAutomaticFormat("Standalone") + " " + size + " px, WebGL " + importer.GetAutomaticFormat("WebGL") + " " + webSize + " px");
    }
}
