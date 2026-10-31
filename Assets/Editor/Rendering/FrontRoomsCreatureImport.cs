using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Import and material rules for the production Relay body, Hunter B "Night Shift" (research/hunter/production_b/
/// 10_plan.md sections 1.4 and 4). Runs after FrontRoomsKitImporter and FrontRoomsSurfaceTextureImporter (order 50) and
/// only touches what they leave open:
///   Relay_*.fbx            read/write on: SkinnedMeshRenderer.BakeMesh feeds the ray-traced glass mirror in Mac players
///                          (FrontRoomsRelayGlassMirror); the rest (unit scale, Mikk tangents, slot remap, LODs) is the kit's.
///   Creature_* textures    atlases, so clamped (no bleed across island edges at far mips); the WebGL tab only is capped
///                          (twill 2048, trim 512, lens 1024 = as authored). Mac / Windows tabs are never touched.
///   Creature_* materials   _FR_SHEEN on the twill (sheen from mask A), the trim atlas and the lens (metal from mask B, sheen 0),
///                          re-applied whenever RenderSetup rewrites them. Idempotent: writes only what differs.
/// </summary>
sealed class FrontRoomsCreatureImport : AssetPostprocessor
{
    const string CreatureFolder = "Assets/Resources/Creatures/";
    const string TextureFolder = "Assets/Resources/Surfaces/Textures/";
    const string SurfaceFolder = "Assets/Resources/Surfaces/";

    /// <summary>The twill sheen (the first repair knob of the 2.05 m contrast test, plan section 6 T2).</summary>
    public const float TwillSheen = .35f;

    public override int GetPostprocessOrder() => 50;
    public override uint GetVersion() => 3;

    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(CreatureFolder + "Relay_", System.StringComparison.Ordinal)) return;
        var importer = (ModelImporter)assetImporter;
        importer.isReadable = true;
        // the kit's "None" drops the skin (MeshRenderers, no bones): Generic without an avatar keeps the
        // SkinnedMeshRenderers and the bone transforms; no clips, the body disables the Animator
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
        importer.importAnimation = false;
        importer.optimizeGameObjects = false;
    }

    /// <summary>The project has the Animation module off: drop whatever animation component Generic adds (by name,
    /// the type is not referenced). The body is posed by FrontRoomsRelayBody, never by an Animator.</summary>
    void OnPostprocessModel(GameObject root)
    {
        if (!assetPath.StartsWith(CreatureFolder + "Relay_", System.StringComparison.Ordinal)) return;
        foreach (var c in root.GetComponentsInChildren<Component>(true))
        {
            if (c == null) continue;
            var type = c.GetType().Name;
            if (type == "Animator" || type == "Animation") Object.DestroyImmediate(c);
        }
    }

    void OnPreprocessTexture()
    {
        var path = assetPath.Replace('\\', '/');
        if (!path.StartsWith(TextureFolder + "Creature_", System.StringComparison.Ordinal)) return;
        var importer = (TextureImporter)assetImporter;
        var stem = Path.GetFileNameWithoutExtension(path);
        importer.wrapMode = TextureWrapMode.Clamp;
        var size = stem.StartsWith("Creature_TwillSpruce", System.StringComparison.Ordinal) ? 2048
                 : stem.StartsWith("Creature_TapeSilver", System.StringComparison.Ordinal) ? 512 : 1024;
        var web = importer.GetPlatformTextureSettings("WebGL");
        web.overridden = true;
        web.maxTextureSize = size;
        web.format = TextureImporterFormat.Automatic;
        web.textureCompression = TextureImporterCompression.Compressed;
        importer.SetPlatformTextureSettings(web);
    }

    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        foreach (var path in imported)
        {
            if (!path.StartsWith(SurfaceFolder + "Creature_", System.StringComparison.Ordinal) || !path.EndsWith(".mat", System.StringComparison.Ordinal)) continue;
            EditorApplication.delayCall -= EnsureLater;
            EditorApplication.delayCall += EnsureLater;
            return;
        }
    }

    static void EnsureLater() => EnsureCreatureMaterials();

    /// <summary>The _FR_SHEEN settings on the creature materials; true if anything was written.</summary>
    public static bool EnsureCreatureMaterials()
    {
        var changed = Apply("Creature_TwillSpruce", TwillSheen, 0f);
        changed |= Apply("Creature_TapeSilver", 0f, 1f);
        changed |= Apply("Creature_LensOpal", 0f, 1f);     // mask B marks the sleeve (lit by the lamp only), the glass stays dielectric
        if (changed) AssetDatabase.SaveAssets();
        return changed;
    }

    static bool Apply(string name, float sheen, float metallic)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(SurfaceFolder + name + ".mat");
        if (m == null || m.shader == null || !m.HasProperty("_Sheen")) return false;
        var changed = false;
        if (!m.IsKeywordEnabled("_FR_SHEEN")) { m.EnableKeyword("_FR_SHEEN"); changed = true; }
        if (m.HasProperty("_UseSheen") && m.GetFloat("_UseSheen") != 1f) { m.SetFloat("_UseSheen", 1f); changed = true; }
        if (!Mathf.Approximately(m.GetFloat("_Sheen"), sheen)) { m.SetFloat("_Sheen", sheen); changed = true; }
        if (!Mathf.Approximately(m.GetFloat("_Metallic"), metallic)) { m.SetFloat("_Metallic", metallic); changed = true; }
        if (changed) EditorUtility.SetDirty(m);
        return changed;
    }
}
