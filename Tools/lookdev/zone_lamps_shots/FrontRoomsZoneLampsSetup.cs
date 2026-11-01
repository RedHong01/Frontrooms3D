// CLONE ONLY (proj_zonelamps, zone_lamps workflow, 2026-10-31). Never lands in main.
// Imports the zone-lamp kits built by zone_lamps/kit/zl_build.py + zl_tex.py, makes their materials and prefabs,
// the pool stand-in room's surfaces and the Run! wrap variants, and measures every mesh and texture as Unity imported them.
// Run: Unity -batchmode -projectPath <clone> -buildTarget OSXUniversal -executeMethod FrontRoomsZoneLampsSetup.RunBatch -frGlassRT off -logFile <log>
// (no -quit: the method exits by itself). ASCII only, no C# tuples.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class FrontRoomsZoneLampsSetup
{
    public const string Root = "Assets/Resources/Props/ZoneLamps/";
    const string ModelDir = Root + "Models/";
    const string TexDir = Root + "Textures/";
    const string MatDir = Root + "Materials/";
    const string PrefabDir = Root + "Prefabs/";
    const string ReportDir = "/Users/redwang/FrontRoomsVisualWork/zone_lamps/kit/unity/";
    static readonly string[] Kits = { "PEND", "MHDL", "HIDUP", "MHLB", "VT8", "NICHE", "PORT", "STRIP", "INDR" };
    static readonly string[] MetalLens = { "MHDL", "MHLB" };
    static readonly Color DefaultEmission = new Color(1f, .96f, .88f, 1f);
    const float LodNear = 4f, LodFar = 12f, LodCull = 46f;

    public static void RunBatch()
    {
        var log = new StringBuilder();
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureImporters(log);
            var trim = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Props/LampOptions/Materials/LampOpt_Trim.mat");
            if (trim == null) throw new Exception("missing LampOpt_Trim");
            var mats = new Dictionary<string, Material>();
            foreach (var k in Kits) mats[k] = MakeLens(k);
            mats["PEND_Glow"] = MakeGlow("ZL_PEND_Glow", .42f, new Vector4(.5f, .5f, .30f, .36f), .35f, new Vector4(1f, 1f, 0f, 0f));
            mats["HIDUP_Glow"] = MakeGlow("ZL_HIDUP_Glow", .20f, new Vector4(.5f, .80f, .32f, .38f), .45f, new Vector4(1f, 1f, 0f, 0f));
            mats["INDR_Glow"] = MakeGlow("ZL_INDR_Glow", .30f, new Vector4(.15f, .5f, .30f, 1.2f), .0f, new Vector4(0f, 1f, 11.5f, .55f));
            MakeRoom(log);
            MakeWrapVariants(log);
            AssetDatabase.SaveAssets();
            BuildPrefabs(mats, trim, log);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Directory.CreateDirectory(ReportDir);
            File.WriteAllText(ReportDir + "unity_report.json", Measure(log));
            File.WriteAllText(ReportDir + "unity_setup_log.txt", log.ToString());
            Debug.Log("ZONELAMPS SETUP DONE\n" + log);
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError("ZONELAMPS SETUP FAILED " + e);
            try { Directory.CreateDirectory(ReportDir); File.WriteAllText(ReportDir + "unity_setup_log.txt", log + "\nFAILED " + e); } catch (Exception) { }
            EditorApplication.Exit(1);
        }
    }

    static void ConfigureImporters(StringBuilder log)
    {
        foreach (var path in Directory.GetFiles(TexDir, "*.png"))
        {
            var p = path.Replace('\\', '/');
            var imp = (TextureImporter)AssetImporter.GetAtPath(p);
            var stem = Path.GetFileNameWithoutExtension(p);
            imp.mipmapEnabled = true;
            imp.filterMode = FilterMode.Trilinear;
            imp.anisoLevel = 8;
            imp.maxTextureSize = 2048;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            imp.alphaSource = TextureImporterAlphaSource.FromInput;
            imp.npotScale = TextureImporterNPOTScale.ToNearest;
            var repeat = stem.StartsWith("ZL_Tile") || stem.StartsWith("ZL_Caustic") || stem.StartsWith("ZL_WaterRipple");
            imp.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            if (stem.StartsWith("ZL_WrapLens")) imp.wrapMode = TextureWrapMode.Repeat;
            if (stem.EndsWith("_N"))
            {
                imp.textureType = TextureImporterType.NormalMap;
                imp.sRGBTexture = false;
            }
            else
            {
                imp.textureType = TextureImporterType.Default;
                imp.sRGBTexture = stem.EndsWith("_A") || stem.EndsWith("_E");
            }
            imp.SaveAndReimport();
        }
        foreach (var path in Directory.GetFiles(ModelDir, "*.fbx"))
        {
            var mi = (ModelImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
            mi.globalScale = 1f;
            mi.useFileScale = true;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importVisibility = false;
            mi.importBlendShapes = false;
            mi.animationType = ModelImporterAnimationType.None;
            mi.importAnimation = false;
            mi.importNormals = ModelImporterNormals.Import;
            mi.importTangents = ModelImporterTangents.CalculateMikk;
            mi.meshCompression = ModelImporterMeshCompression.Off;
            mi.isReadable = true;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.addCollider = false;
            mi.SaveAndReimport();
        }
        log.AppendLine("importers configured");
    }

    static Texture2D Tex(string stem)
    {
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + stem + ".png");
        if (t == null) throw new Exception("missing texture " + stem);
        return t;
    }

    static Material Mat(string name, Shader shader)
    {
        if (shader == null) throw new Exception("missing shader for " + name);
        var path = MatDir + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        m.shader = shader;
        m.shaderKeywords = new string[0];
        return m;
    }

    static void SurfaceCommon(Material m, string stem, bool emission, bool metal, Vector2 tile)
    {
        m.SetTexture("_BaseMap", Tex(stem + "_A"));
        m.SetTexture("_BumpMap", Tex(stem + "_N"));
        m.SetTexture("_MaskMap", Tex(stem + "_S"));
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_BumpScale", 1f);
        m.SetFloat("_Smoothness", 1f);
        m.SetFloat("_OcclusionStrength", .8f);
        m.SetFloat("_MeshUV", 1f);
        m.EnableKeyword("_FR_MESH_UV");
        m.SetVector("_TileSize", new Vector4(tile.x, tile.y, 0f, 0f));
        m.SetFloat("_MacroTone", 0f);
        m.SetFloat("_MacroDirt", 0f);
        m.SetFloat("_StainStrength", 0f);
        if (metal)
        {
            m.SetFloat("_UseSheen", 1f);
            m.EnableKeyword("_FR_SHEEN");
            m.SetFloat("_Sheen", 0f);
            m.SetFloat("_Metallic", 1f);
        }
        else
        {
            m.SetFloat("_UseSheen", 0f);
            m.SetFloat("_Metallic", 0f);
        }
        if (emission)
        {
            m.SetFloat("_UseEmission", 1f);
            m.EnableKeyword("_EMISSION");
            m.SetTexture("_EmissionMap", Tex(stem + "_E"));
            m.SetColor("_EmissionColor", DefaultEmission);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }
        else
        {
            m.SetFloat("_UseEmission", 0f);
            m.SetColor("_EmissionColor", Color.black);
        }
        EditorUtility.SetDirty(m);
    }

    static Material MakeLens(string k)
    {
        var m = Mat("ZL_" + k + "_Lens", Shader.Find("FrontRooms/Surface"));
        SurfaceCommon(m, "ZL_" + k + "_Lens", true, Array.IndexOf(MetalLens, k) >= 0, Vector2.one);
        return m;
    }

    static Material MakeGlow(string name, float gain, Vector4 shape, float hot, Vector4 edge)
    {
        var m = Mat(name, Shader.Find("FrontRooms/ZoneLamps/GlowAdd"));
        m.SetColor("_EmissionColor", DefaultEmission);
        m.SetFloat("_Gain", gain);
        m.SetVector("_Shape", shape);
        m.SetFloat("_Hot", hot);
        m.SetVector("_Edge", edge);
        m.renderQueue = (int)RenderQueue.Transparent - 10;
        EditorUtility.SetDirty(m);
        return m;
    }

    // ---------------------------------------------------------------- the pool stand-in's surfaces
    static void MakeRoom(StringBuilder log)
    {
        var sh = Shader.Find("FrontRooms/Surface");
        var wall = Mat("ZL_TileWall", sh); SurfaceCommon(wall, "ZL_TileWall", false, false, Vector2.one);
        var deck = Mat("ZL_TileDeck", sh); SurfaceCommon(deck, "ZL_TileDeck", false, false, Vector2.one);
        var basin = Mat("ZL_TileBasin", sh); SurfaceCommon(basin, "ZL_TileBasin", false, false, Vector2.one);
        basin.SetColor("_BaseColor", new Color(.86f, .97f, 1f));
        var lane = Mat("ZL_TileLane", sh); SurfaceCommon(lane, "ZL_TileBasin", false, false, Vector2.one);
        lane.SetColor("_BaseColor", new Color(.10f, .15f, .30f));
        var coping = Mat("ZL_TileCoping", sh); SurfaceCommon(coping, "ZL_TileWall", false, false, Vector2.one);
        coping.SetColor("_BaseColor", new Color(.93f, .92f, .88f));
        var caustic = Mat("ZL_Caustic", Shader.Find("FrontRooms/ZoneLamps/CausticAdd"));
        caustic.SetTexture("_MainTex", Tex("ZL_Caustic_A"));
        caustic.SetColor("_Color", new Color(.30f, .42f, .42f, 1f));
        caustic.SetFloat("_Level", 1f);
        caustic.SetFloat("_Scale", 1.7f);
        caustic.SetFloat("_Speed", .04f);
        caustic.SetFloat("_Fade", .5f);
        caustic.renderQueue = (int)RenderQueue.Transparent - 20;
        EditorUtility.SetDirty(caustic);
        // water: URP Lit, transparent premultiplied (reflections are not dimmed by alpha), ripple normals
        var water = Mat("ZL_Water", Shader.Find("Universal Render Pipeline/Lit"));
        water.SetFloat("_Surface", 1f);
        water.SetFloat("_Blend", 1f);
        water.SetFloat("_SrcBlend", (float)BlendMode.One);
        water.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        water.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        water.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        water.SetFloat("_ZWrite", 0f);
        water.SetFloat("_Cull", 2f);
        water.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        water.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        water.SetOverrideTag("RenderType", "Transparent");
        water.renderQueue = (int)RenderQueue.Transparent;
        water.SetColor("_BaseColor", new Color(.05f, .20f, .22f, .30f));
        water.SetFloat("_Smoothness", .95f);
        water.SetFloat("_Metallic", 0f);
        water.SetTexture("_BumpMap", Tex("ZL_WaterRipple_N"));
        water.SetFloat("_BumpScale", .30f);
        water.EnableKeyword("_NORMALMAP");
        water.SetTextureScale("_BaseMap", new Vector2(5f, 10f));
        water.SetFloat("_SpecularHighlights", 1f);
        water.SetFloat("_EnvironmentReflections", 1f);
        EditorUtility.SetDirty(water);
        log.AppendLine("room materials made");
    }

    // ---------------------------------------------------------------- Run! wrap variants (copies of Run_WrapLens)
    static void MakeWrapVariants(StringBuilder log)
    {
        var src = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Surfaces/Run_WrapLens.mat");
        if (src == null) throw new Exception("missing Run_WrapLens.mat");
        foreach (var v in new[] { "R1", "R1a", "R1b", "R1c", "OneTube" })
        {
            var path = MatDir + "ZL_WrapLens_" + v + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(src); AssetDatabase.CreateAsset(m, path); }
            else m.CopyPropertiesFromMaterial(src);
            m.SetTexture("_EmissionMap", Tex("ZL_WrapLens_" + v + "_E"));
            if (v == "R1c") m.SetTexture("_BaseMap", Tex("ZL_WrapLens_R1c_A"));
            EditorUtility.SetDirty(m);
        }
        log.AppendLine("wrap variants made from " + src.name + " (shader " + src.shader.name + ")");
    }

    // ---------------------------------------------------------------- prefabs
    static Dictionary<string, Mesh> Meshes(string k)
    {
        var map = new Dictionary<string, Mesh>();
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(ModelDir + "ZL_" + k + ".fbx"))
        {
            var mesh = o as Mesh;
            if (mesh != null) map[mesh.name] = mesh;
        }
        return map;
    }

    static GameObject Part(Transform parent, string name, Mesh mesh, Material[] mats)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        var use = new Material[mesh.subMeshCount];
        for (var i = 0; i < use.Length; i++) use[i] = mats[Mathf.Min(i, mats.Length - 1)];
        r.sharedMaterials = use;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = LightProbeUsage.BlendProbes;
        return go;
    }

    static float Height(float size, float distance) { return size / (2f * distance * Mathf.Tan(38f * Mathf.Deg2Rad)); }

    static GameObject Housing(Transform parent, Dictionary<string, Mesh> m, Material trim)
    {
        var h = new GameObject("Housing");
        if (parent != null) h.transform.SetParent(parent, false);
        var rs = new Renderer[3];
        for (var i = 0; i < 3; i++)
        {
            Mesh mesh;
            if (!m.TryGetValue("Housing_L" + i, out mesh)) throw new Exception("missing mesh Housing_L" + i);
            rs[i] = Part(h.transform, "Housing_L" + i, mesh, new[] { trim }).GetComponent<Renderer>();
            rs[i].receiveShadows = true;
        }
        var lg = h.AddComponent<LODGroup>();
        lg.fadeMode = LODFadeMode.None;
        lg.SetLODs(new[] { new LOD(.5f, new[] { rs[0] }), new LOD(.2f, new[] { rs[1] }), new LOD(.05f, new[] { rs[2] }) });
        lg.RecalculateBounds();
        var size = lg.size;
        lg.SetLODs(new[] { new LOD(Height(size, LodNear), new[] { rs[0] }), new LOD(Height(size, LodFar), new[] { rs[1] }), new LOD(Height(size, LodCull), new[] { rs[2] }) });
        return h;
    }

    static void Save(GameObject go, string name, StringBuilder log)
    {
        PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + name + ".prefab");
        UnityEngine.Object.DestroyImmediate(go);
        log.AppendLine("prefab " + name);
    }

    static void BuildPrefabs(Dictionary<string, Material> mats, Material trim, StringBuilder log)
    {
        foreach (var k in Kits)
        {
            var m = Meshes(k);
            Material glow;
            mats.TryGetValue(k + "_Glow", out glow);
            var lensMats = glow != null ? new[] { mats[k], glow } : new[] { mats[k] };
            var root = new GameObject("ZL_" + k);
            Part(root.transform, "Lens", m["Lens"], lensMats);
            Housing(root.transform, m, trim);
            Save(root, "ZL_" + k, log);
            Save(Housing(null, m, trim), "ZL_" + k + "_Housing", log);
            var phone = new GameObject("ZL_" + k + "_Phone");
            Part(phone.transform, "Lens", m["Phone"], lensMats);
            Save(phone, "ZL_" + k + "_Phone", log);
            if (k == "HIDUP")
            {
                var t = new GameObject("ZL_HIDUP_Tall");
                Part(t.transform, "Lens", m["Lens_Tall"], lensMats);
                Housing(t.transform, m, trim);
                Save(t, "ZL_HIDUP_Tall", log);
                var tp = new GameObject("ZL_HIDUP_Tall_Phone");
                Part(tp.transform, "Lens", m["Phone_Tall"], lensMats);
                Save(tp, "ZL_HIDUP_Tall_Phone", log);
            }
        }
        var rp = Meshes("RPLATE");
        Save(Housing(null, rp, trim), "ZL_RPLATE_Housing", log);
    }

    // ---------------------------------------------------------------- measuring
    static string Measure(StringBuilder log)
    {
        var sb = new StringBuilder("{\n \"meshes\": {\n");
        var first = true;
        foreach (var path in Directory.GetFiles(ModelDir, "*.fbx"))
        {
            var file = Path.GetFileNameWithoutExtension(path);
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path.Replace('\\', '/')))
            {
                var mesh = o as Mesh;
                if (mesh == null) continue;
                long tris = 0;
                for (var s = 0; s < mesh.subMeshCount; s++) tris += mesh.GetIndexCount(s) / 3;
                var b = mesh.bounds;
                sb.Append(first ? "" : ",\n");
                first = false;
                sb.AppendFormat("  \"{0}/{1}\": {{\"tris\": {2}, \"verts\": {3}, \"submeshes\": {4}, \"size_m\": [{5:F4}, {6:F4}, {7:F4}]}}",
                    file, mesh.name, tris, mesh.vertexCount, mesh.subMeshCount, b.size.x, b.size.y, b.size.z);
            }
        }
        sb.Append("\n },\n \"textures\": {\n");
        first = true;
        foreach (var path in Directory.GetFiles(TexDir, "*.png"))
        {
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(path.Replace('\\', '/'));
            if (t == null) continue;
            var bytes = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t);
            sb.Append(first ? "" : ",\n");
            first = false;
            sb.AppendFormat("  \"{0}\": {{\"w\": {1}, \"h\": {2}, \"format\": \"{3}\", \"bytes\": {4}}}", t.name, t.width, t.height, t.graphicsFormat, bytes);
        }
        sb.Append("\n }\n}\n");
        log.AppendLine("measured");
        return sb.ToString();
    }
}
