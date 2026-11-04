using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// The handheld URP pair (FrontRooms_URP_Handheld + FrontRooms_URP_Handheld_Renderer) is the desktop pair with three SSAO
/// values of its own (source Depth, After Opaque, Medium samples), its own renderer list (no water mirror renderer: phones
/// use the probe), the opaque copy at half resolution (the desktop's full-resolution copy serves its water refraction) and
/// STP as its upscaler, which is there so the iPhone build keeps STP's compute shaders (tech_art 20_ranked_changes 3.13; FrontRoomsQuality picks the pair on iPhone and iPad
/// and writes Auto at full resolution). Sync copies every other value from the desktop pair, so a change to the desktop
/// asset or its renderer reaches the phones too, and points the Handheld quality level back at its asset
/// (FrontRoomsRenderSetup.SetUp gives every level the desktop asset first, then calls Sync).
/// Menu: FrontRooms > Rendering > Sync handheld URP asset.
/// </summary>
public static class FrontRoomsHandheldPipeline
{
    const string DesktopPath = "Assets/Settings/FrontRooms_URP.asset";
    const string PipelinePath = "Assets/Settings/FrontRooms_URP_Handheld.asset";
    const string RendererPath = "Assets/Settings/FrontRooms_URP_Handheld_Renderer.asset";
    const string LevelName = "Handheld";

    // The handheld asset keeps its name, its renderers, its opaque copy size and its upscaler; the m_Prefilter values are
    // left to URP, which rewrites them for each asset when it builds.
    static readonly string[] KeepPipeline = { "m_Name", "m_RendererDataList", "m_RendererData", "m_OpaqueDownsampling", "m_UpscalingFilter" };
    static readonly string[] KeepRenderer = { "m_Name", "m_RendererFeatures", "m_RendererFeatureMap" };
    static readonly string[] KeepFeature = { "m_Name" };
    static readonly string[] KeepSsao = { "m_Settings.Source", "m_Settings.AfterOpaque", "m_Settings.Samples" };

    [MenuItem("FrontRooms/Rendering/Sync handheld URP asset")]
    public static void SyncMenu()
    {
        Sync(AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(DesktopPath));
        AssetDatabase.SaveAssets();
    }

    public static void Sync(UniversalRenderPipelineAsset desktop)
    {
        var handheld = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        var desktopRenderer = desktop != null && desktop.rendererDataList.Length > 0 ? desktop.rendererDataList[0] as UniversalRendererData : null;
        if (handheld == null || renderer == null || desktopRenderer == null)
        {
            Debug.LogWarning("[FrontRoomsHandheldPipeline] the desktop or the handheld URP pair is missing; nothing synced.");
            return;
        }
        Copy(desktop, handheld, KeepPipeline, "m_Prefilter");
        Copy(desktopRenderer, renderer, KeepRenderer, null);
        foreach (var feature in desktopRenderer.rendererFeatures)
        {
            if (feature == null) continue;
            ScriptableRendererFeature twin = null;
            foreach (var f in renderer.rendererFeatures)
                if (f != null && f.GetType() == feature.GetType()) { twin = f; break; }
            if (twin == null)
            {
                Debug.LogWarning("[FrontRoomsHandheldPipeline] " + feature.name + " is on the desktop renderer only: add it to " + RendererPath + " by hand.");
                continue;
            }
            Copy(feature, twin, feature is ScreenSpaceAmbientOcclusion ? KeepSsao : KeepFeature, null);
        }

        var active = QualitySettings.GetQualityLevel();
        var names = QualitySettings.names;
        for (var i = 0; i < names.Length; i++)
        {
            if (names[i] != LevelName) continue;
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = handheld;
        }
        QualitySettings.SetQualityLevel(active, false);
        Debug.Log("[FrontRoomsHandheldPipeline] " + PipelinePath + " follows " + DesktopPath + " (SSAO source, After Opaque, samples, renderers, opaque copy size and the STP upscaler kept).");
    }

    // Copies each serialized value of `from` into `to`, except the paths in `keep` (a nested path such as m_Settings.Source
    // keeps that one child) and, when given, every top-level path that starts with keepPrefix.
    static void Copy(UnityEngine.Object from, UnityEngine.Object to, string[] keep, string keepPrefix)
    {
        var src = new SerializedObject(from);
        var dst = new SerializedObject(to);
        var it = src.GetIterator();
        var enter = true;
        while (it.NextVisible(enter))
        {
            enter = false;
            var path = it.propertyPath;
            if (path == "m_Script" || Array.IndexOf(keep, path) >= 0) continue;
            if (keepPrefix != null && path.StartsWith(keepPrefix, StringComparison.Ordinal)) continue;
            if (it.hasVisibleChildren && HasKeptChild(keep, path))
            {
                var child = it.Copy();
                var end = it.GetEndProperty();
                var more = child.NextVisible(true);
                while (more && !SerializedProperty.EqualContents(child, end))
                {
                    if (Array.IndexOf(keep, child.propertyPath) < 0) dst.CopyFromSerializedProperty(child);
                    more = child.NextVisible(false);
                }
                continue;
            }
            dst.CopyFromSerializedProperty(it);
        }
        dst.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(to);
    }

    static bool HasKeptChild(string[] keep, string path)
    {
        foreach (var k in keep)
            if (k.StartsWith(path + ".", StringComparison.Ordinal)) return true;
        return false;
    }
}
