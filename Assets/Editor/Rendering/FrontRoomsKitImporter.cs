using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Import settings for the Blender prop kit (Assets/Resources/Props/Models):
/// metres at unit scale, imported normals with MikkTSpace tangents, no
/// cameras/lights/animation, and each kit slot ("Prop_WoodCherry") remapped
/// to the project material Resources/Surfaces/&lt;slot&gt;.mat when it exists.
/// The slot list comes from the asset's JSON sidecar.
/// A language's copy of a kit model (Assets/Resources/WorldText/&lt;code&gt;/Props/Models/, world text: the EXIT sign's legend
/// is geometry, research/exit_sign/80_world_text_options.md) is imported exactly as a kit, from its own sidecar.
/// </summary>
sealed class FrontRoomsKitImporter : AssetPostprocessor
{
    // The sidecar's final distance is the cull threshold. Bump the importer
    // version so existing kit FBX assets are re-imported with this mapping.
    public override uint GetVersion() => 2;

    const string Folder = "Assets/Resources/Props/Models/";
    const string CreatureFolder = "Assets/Resources/Creatures/";
    const string WorldTextFolder = "Assets/Resources/WorldText/";
    const string WorldTextModels = "/Props/Models/";

    /// <summary>A kit model, or a language's copy of one (WorldText/&lt;code&gt;/Props/Models/).</summary>
    static bool IsKit(string path) => path.StartsWith(Folder) || (path.StartsWith(WorldTextFolder) && path.IndexOf(WorldTextModels, WorldTextFolder.Length, System.StringComparison.Ordinal) > 0);
    const string SurfaceFolder = "Assets/Resources/Surfaces/";

    void OnPreprocessModel()
    {
        if (!IsKit(assetPath) && !assetPath.StartsWith(CreatureFolder)) return;
        var importer = (ModelImporter)assetImporter;
        importer.globalScale = 1f;
        importer.useFileScale = true;
        importer.importCameras = false;
        importer.importLights = false;
        importer.importVisibility = false;
        importer.importBlendShapes = false;
        importer.animationType = ModelImporterAnimationType.None;
        importer.importAnimation = false;
        importer.importNormals = ModelImporterNormals.Import;
        importer.importTangents = ModelImporterTangents.CalculateMikk;
        importer.meshCompression = ModelImporterMeshCompression.Off;
        // Door break debris (Kit_DoorLeafBreak_*_Deb<k>, production P1): readable, because the break merges the settled
        // pieces into one floor mesh per door at run time (research/door_break/50_rethink.md 3.9). BS7: the station kits with
        // a dated paper cell (FrontRoomsStationPaper.DatedKits: the DAR and sign-out clipboards, the register) are readable
        // too, because the live date's patch is laid through their UVs at run time. Every other kit stays GPU-only.
        var stem = Path.GetFileNameWithoutExtension(assetPath);
        importer.isReadable = (stem.StartsWith("Kit_DoorLeafBreak_", System.StringComparison.Ordinal) && stem.Contains("_Deb"))
            || FrontRoomsStationPaper.IsDatedKit(stem);
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;

        var sidecar = Path.ChangeExtension(assetPath, ".json");
        if (!File.Exists(sidecar)) return;
        var info = JsonUtility.FromJson<FrontRoomsKitLibrary.Info>(File.ReadAllText(sidecar));
        if (info?.slots == null) return;
        foreach (var slot in info.slots)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(SurfaceFolder + slot + ".mat");
            var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), slot);
            if (material != null) importer.AddRemap(id, material);
            else importer.RemoveRemap(id);
        }
    }

    /// <summary>
    /// Kit FBX files with _LOD0/_LOD1 children get a LODGroup from Unity;
    /// set the switch heights from the research spec: LOD1 below 10 % of the
    /// screen, culled below 2 % (large pieces) or 3 % (desk props).
    /// </summary>
    /// <summary>A re-exported kit model or sidecar (or a surface material) invalidates
    /// FrontRoomsKitLibrary's caches in the editor.</summary>
    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        foreach (var list in new[] { imported, deleted, moved, movedFrom })
            foreach (var path in list)
                if (IsKit(path) || path.StartsWith("Assets/Resources/Surfaces/"))
                {
                    FrontRoomsKitLibrary.ClearCache();
                    return;
                }
    }

    void OnPostprocessModel(GameObject root)
    {
        if (!IsKit(assetPath) && !assetPath.StartsWith(CreatureFolder)) return;
        var group = root.GetComponent<LODGroup>();
        if (group == null) return;
        var lods = group.GetLODs();
        if (lods.Length < 2) return;
        var sidecar = Path.ChangeExtension(assetPath, ".json");
        var authored = File.Exists(sidecar) ? JsonUtility.FromJson<FrontRoomsKitLibrary.Info>(File.ReadAllText(sidecar)) : null;
        if (authored?.lodDistances != null && authored.lodDistances.Length >= lods.Length)
        {
            // Sidecars use metres at the reference vertical FOV (76 degrees):
            // screenHeight = size / (2 d tan(38 degrees)). Zero means no cull.
            var tanHalfFov = Mathf.Tan(38f * Mathf.Deg2Rad);
            var last = lods.Length - 1;
            for (var i = 0; i <= last; i++)
            {
                // Sidecars list d01, d12, dcull. When the FBX has only LOD0
                // and LOD1, the last imported level still owns dcull.
                var distance = i == last
                    ? authored.lodDistances[authored.lodDistances.Length - 1]
                    : authored.lodDistances[i];
                lods[i].screenRelativeTransitionHeight = distance > 0f
                    ? group.size / (2f * distance * tanHalfFov)
                    : i == last ? 0f : lods[i].screenRelativeTransitionHeight;
            }
            group.SetLODs(lods);
            group.fadeMode = LODFadeMode.None;
            return;
        }
        var size = group.size;
        lods[0].screenRelativeTransitionHeight = .10f;
        lods[1].screenRelativeTransitionHeight = size < .6f ? .03f : .02f;
        group.SetLODs(lods);
        group.fadeMode = LODFadeMode.None;
    }
}
