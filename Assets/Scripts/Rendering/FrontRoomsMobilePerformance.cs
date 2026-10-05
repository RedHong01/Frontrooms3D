using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Runtime render budget for the iPhone player. The desktop/standalone look is
/// left untouched; the iOS path trades invisible overdraw and shadow work for
/// a stable 60 fps target while keeping HDR and the authored film grade.
/// </summary>
public static class FrontRoomsMobilePerformance
{
    public const float RenderScale = .82f;
    public const float LightRadius = 12f;
    public const float ShadowRadius = 5f;

    public static bool Active { get; private set; }

    public static void Apply()
    {
        Active = Application.platform == RuntimePlatform.IPhonePlayer;
        if (!Active) return;

        QualitySettings.vSyncCount = 0;
        QualitySettings.antiAliasing = 2;
        Application.targetFrameRate = 60;

        var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (pipeline == null)
            pipeline = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        if (pipeline == null)
        {
            Debug.LogWarning("[FrontRoomsMobilePerformance] URP asset is unavailable; using frame-rate and light budgets only.");
            return;
        }

        // Keep HDR enabled for the authored fluorescent grade. RenderScale and
        // 2x MSAA remove the largest high-resolution tile cost on iPhone.
        pipeline.renderScale = RenderScale;
        pipeline.msaaSampleCount = 2;
        pipeline.shadowDistance = Mathf.Min(pipeline.shadowDistance, 24f);
        pipeline.mainLightShadowmapResolution = Mathf.Min(pipeline.mainLightShadowmapResolution, 1024);
        pipeline.additionalLightsShadowmapResolution = Mathf.Min(pipeline.additionalLightsShadowmapResolution, 1024);
        // SSAO is a full-resolution depth/normal pass. The wall and fixture
        // materials already carry their authored occlusion, so the mobile
        // profile can remove this pass without changing the room layout.
        var renderers = pipeline.rendererDataList;
        if (renderers != null)
        {
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                foreach (var feature in renderer.rendererFeatures)
                {
                    if (feature == null) continue;
                    if (feature.GetType().Name == "ScreenSpaceAmbientOcclusion")
                        feature.SetActive(false);
                }
            }
        }

        Debug.Log("[FrontRoomsMobilePerformance] iPhone profile active: renderScale "
            + RenderScale.ToString("0.00") + ", MSAA 2x, SSAO off, fixture shadows off, shadow distance 24m.");
    }

    public static void ApplyMapBudget(FrontRoomsMapWorld map)
    {
        if (!Active || map == null) return;
        map.ApplyMobileLightBudget(LightRadius, ShadowRadius);
    }
}
