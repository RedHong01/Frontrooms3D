# R4. What a Unity 6 URP game can switch at runtime on Metal (macOS, iOS, iPadOS)

Purpose: back a graphics-settings menu (presets and custom detail options) for FrontRooms.
Scope: research only. Nothing in the project was changed, built or run.

## Method

- The material was gathered on 2026-10-24, 07:27 to 07:49 PDT, by the original research agent. That agent hit its account limit before writing anything. This report was written up from its saved reads on 2026-10-24.
- Sources are of four kinds: (a) reads of the project and of the URP 17.3.0 and SRP Core package source, (b) reads of Unity's iOS trampoline source in the 6000.3.10f1 editor install, (c) Unity and Apple documentation pages that were actually opened, (d) a small number of extra checks made during write-up (package source reads and a few page fetches, marked "write-up check" in the SOURCES table).
- Search results that were never opened are not used as evidence anywhere in this report.
- Where the material does not confirm a fact, the report says "unverified".
- Project: Unity 6000.3.10f1, URP 17.3.0. Project path: `.../Frontrooms3D`. In this report `URP/` means `Library/PackageCache/com.unity.render-pipelines.universal@37e0d4fc2503`, `CORE/` means `Library/PackageCache/com.unity.render-pipelines.core@04ab0eefa0c3`, `TRAMP/` means `/Applications/Unity/Hub/Editor/6000.3.10f1/PlaybackEngines/iOSSupport/Trampoline/Classes`.
- Source numbers like [S12] point to the SOURCES table at the end.
- Line numbers are from the files as read on 2026-10-24.

## Summary (read this first)

1. Only some URP asset properties can be set from script. These have a public setter: `renderScale`, `upscalingFilter`, `msaaSampleCount`, `supportsHDR`, `hdrColorBufferPrecision`, `shadowDistance`, `shadowCascadeCount`, `mainLightShadowmapResolution`, `additionalLightsShadowmapResolution`, `supportsCameraDepthTexture`, `supportsCameraOpaqueTexture`, `colorGradingLutSize`, `volumeProfile`. These have no public setter in 17.3 (internal set or no set): `supportsMainLightShadows`, `supportsAdditionalLightShadows`, `supportsSoftShadows`, `softShadowQuality`, `enableLODCrossFade`, `mainLightRenderingMode`, `additionalLightsRenderingMode`. See Q1.
2. SSAO can be switched on and off with `ScriptableRendererFeature.SetActive`, and the feature object is reachable from `asset.rendererDataList[i].rendererFeatures`. Its settings (downsample, samples, blur) are `internal`. The current build has stripped the SSAO-OFF shader variants and the Low/Medium sample variants, so switching SSAO off in a build made from today's setup is unsafe. The fix is a second renderer without SSAO, selected per camera with `SetRenderer`. See Q2.
3. A different URP asset per quality level works and is Unity's documented pattern. The switch recreates the pipeline and Unity says it has a "temporary but significant performance impact". See Q3.
4. Volume overrides (bloom, grain, chromatic aberration) are plain public fields and are safe to change, because this project does not strip post-processing variants. See Q4.
5. iOS ignores `vSyncCount`. Frame rate is set only by `Application.targetFrameRate`, and 40 fps on a 120 Hz ProMotion screen is allowed by the documented rule, but only if ProMotion support is on. See Q6.
6. `Render Scale` does not scale UI. Overlay UI (uGUI and UI Toolkit) is drawn after the upscale at the drawable size. See Q7.
7. Unity 6.3 has no MetalFX in URP. In-box upscalers are Bilinear, Nearest, FSR 1 and STP. See Q8.
8. In Unity 6.3 there is no documented Adaptive Performance Apple provider. Thermal state needs a native plugin or a later Unity version. Low Power Mode is available through `UnityEngine.iOS.Device.lowPowerModeEnabled`. See Q9.

The final TABLE and the recommended preset architecture are at the end of the file.

## Current project state (facts used below)

- One URP asset `Assets/Settings/FrontRooms_URP.asset` is assigned to all six quality levels (Very Low, Low, Medium, High, Very High, Ultra) by `Assets/Editor/Rendering/FrontRoomsRenderSetup.cs:49-55`. Defaults per platform: iPhone 2 (Medium), Standalone 5 (Ultra) (`ProjectSettings/QualitySettings.asset:328-342`).
- Asset values: HDR on (`m_SupportsHDR: 1`, `m_HDRColorBufferPrecision: 0`), MSAA 4, render scale 1, upscaling filter 0, main shadow 2048, additional light shadows on at 4096, shadow distance 40, 2 cascades, soft shadow quality 3, SRP Batcher on, Adaptive Performance flag on, LUT 32 (`FrontRooms_URP.asset:22-81`).
- Renderer `FrontRooms_URP_Renderer.asset`: Forward+ (`m_RenderingMode: 2`), SSAO feature active (`:70`, settings `:72-81`: BlueNoise, no downsample, depth normals, High samples, High blur), Metal glass RT feature active (`:94-96`).
- URP Global Settings: `m_StripUnusedVariants: 1`, `m_StripUnusedPostProcessingVariants: 0`, `m_IncludeReferencedInScenes: 0`, `m_IncludeAssetsByLabel: 0` (`Assets/UniversalRenderPipelineGlobalSettings.asset:22-23, 28-29, 254-255, 397-398`).
- Player Settings: `strictShaderVariantMatching: 0` (`ProjectSettings/ProjectSettings.asset:193`), `enableFrameTimingStats: 0` (`:156`), `resolutionScalingMode: 0` and `targetPixelDensity: 30` (`:162-163`), `appleEnableProMotion: 0` (`:257`), `useHDRDisplay: 0` (`:159`), `mipStripping: 0` (`:55`).
- Quality level values (`QualitySettings.asset`): vSyncCount 0 on Very Low and Low, 1 on Medium to Ultra (`:32, 85, 138, 191, 244, 297`); lodBias 0.3, 0.4, 0.7, 1, 1.5, 2; `globalTextureMipmapLimit` 1 on Very Low, 0 elsewhere; anisotropicTextures 0, 0, 1, 1, 2, 2; realtimeReflectionProbes 0 on the first three and 1 on the others.
- Runtime code: `FrontRooms3DGame.cs:432` and `:2510` set `camera.allowHDR`; `:496` sets `Application.targetFrameRate = 60`; `:502` sets `QualitySettings.globalTextureMipmapLimit = 0`. `FrontRooms3DGame.Baseline.cs:500-578` saves, sets to -1 and restores the frame rate for a baseline run.
- `Packages/manifest.json` has URP 17.3.0 and no Adaptive Performance entry. The URP asmdef turns on `ENABLE_ADAPTIVE_PERFORMANCE` only if the module `com.unity.modules.adaptiveperformance` is present (`URP/Runtime/Unity.RenderPipelines.Universal.Runtime.asmdef`, versionDefines). The compiled `Unity.RenderPipelines.Universal.Runtime.dll` in `Library/ScriptAssemblies` has no `ApplyAdaptivePerformance` string, so the `m_UseAdaptivePerformance: 1` flag in the asset does nothing in this project today. [S1, S14-S20, S33]

## Q1. UniversalRenderPipelineAsset at runtime: setters, upscaling filters, cost

### 1.1 Which properties can be set from script

Unity's own page says only a property with a `set` method can be changed by script, and the change should be made where performance does not matter (loading screen, static menu) [S49, S50]. Reading `UniversalRenderPipelineAsset.cs` gives this split:

| Property | Setter in 17.3.0 | Evidence |
|---|---|---|
| `supportsCameraDepthTexture` | public | `UniversalRenderPipelineAsset.cs:1053` |
| `supportsCameraOpaqueTexture` | public | `:1062` |
| `storeActionsOptimization` | public | `:1083` |
| `supportsHDR` | public | `:1093` |
| `hdrColorBufferPrecision` | public | `:1102` |
| `msaaSampleCount` | public (cast to `MsaaQuality`, no validation) | `:1112` |
| `renderScale` | public, clamped to 0.1 to 3.0 | `:1121`, `:2014-2017`, `UniversalRenderPipeline.cs:107-118` |
| `upscalingFilter` | public | `:1142` |
| `fsrOverrideSharpness`, `fsrSharpness` | public | `:1185`, `:1197` |
| `mainLightShadowmapResolution` | public | `:1322` |
| `additionalLightsShadowmapResolution` | public | `:1365` |
| `maxAdditionalLightsCount` | public | `:1341` |
| `shadowDistance` | public, `Max(0, v)` | `:1465` |
| `shadowCascadeCount` | public, throws `ArgumentException` outside 1 to 4 | `:1474-1481`, `:667` |
| `cascade2Split`, `cascade3Split`, `cascade4Split`, `cascadeBorder` | public | `:1491-1520` |
| `shadowDepthBias`, `shadowNormalBias` | public | `:1529`, `:1538` |
| `supportsDynamicBatching`, `useSRPBatcher` | public | `:1566`, `:1619` |
| `volumeProfile` | public | `:1603` |
| `colorGradingMode`, `colorGradingLutSize` | public | `:1653`, `:1662` |
| `useAdaptivePerformance` | public | `:1692` |
| `supportsMainLightShadows` | **internal set** | `:1307` |
| `supportsAdditionalLightShadows` | **internal set** | `:1350` |
| `supportsSoftShadows` | **internal set** | `:1547` |
| `softShadowQuality` | **internal** (getter and setter) | `:1553-1557` |
| `mainLightRenderingMode`, `additionalLightsRenderingMode` | **internal set** | `:1298`, `:1332` |
| `reflectionProbeBlending` | **internal set** | `:1415` |
| `additionalLightsShadowResolutionTierLow/Medium/High` | **internal set** | `:1374`, `:1383`, `:1392` |
| `enableLODCrossFade`, `lodCrossFadeDitheringType` | **getter only** | `:1127`, `:1132` |
| `opaqueDownsampling`, `supportsTerrainHoles`, `supportsMixedLighting`, `supportsLightCookies` | **getter only** | `:1068`, `:1074`, `:1573`, `:1578` |

So for the options a settings menu usually wants:

- Soft shadow quality (`softShadowQuality`), the "Additional light shadows" switch and the main-light shadow switch have no public setter. They need reflection on the private serialized fields (`m_SoftShadowQuality`, `m_AdditionalLightShadowsSupported`, ...) or a separate asset. Unity's pages do not mention reflection [S50]. Whether reflection survives IL2CPP managed stripping on iOS is unverified. It would need a test build.
- LOD cross-fade is read-only on the asset. The pipeline copies it into `QualitySettings.enableLODCrossFade` once, when the pipeline is created (`UniversalRenderPipeline.cs:300`). A runtime change needs a separate asset (pipeline recreation).
- There are public per-object alternatives that need no asset change:
  - Per light: `UniversalAdditionalLightData.softShadowQuality` has a public setter (`UniversalAdditionalLightData.cs:161-165`, default `UsePipelineSettings`). On Mac and iOS the shader gets the quality as a per-light parameter, not as a keyword (see 1.4).
  - Per light: `Light.shadows = LightShadows.Hard` turns the soft path off for that light (`MainLightShadowCasterPass.cs:364`: soft keyword needs `light.shadows == LightShadows.Soft`).
  - Per camera (`UniversalAdditionalCameraData`): `renderShadows` (`:536-539`), `requiresDepthOption` (`:547-550`), `renderPostProcessing` (`:789-792`), `antialiasing` (`:799-802`), `antialiasingQuality` (`:809-812`), `SetRenderer(int)` (`:688-691`). Also `Camera.allowMSAA` and `Camera.allowHDR` (used at `UniversalRenderPipeline.cs:1408` and `:1488`).

### 1.2 When a change takes effect

- There is no event. The setters only store a field. The pipeline reads the asset every frame when it builds per-camera data, so a change shows on the next rendered frame:
  - render scale and upscaling filter: `UniversalRenderPipeline.cs:1505-1509` (inside `InitializeStackedCameraData`, `var settings = asset` at `:1442`);
  - MSAA: `:1408-1409`;
  - HDR: `:1488` (`cameraData.isHdrEnabled = baseCamera.allowHDR && settings.supportsHDR`) and `:597` (HDR precision);
  - shadow resolutions, cascade count: `:1764-1768`;
  - max shadow distance: `:1592`.
- What is not re-run: the constructor work, which happens only when the pipeline instance is created. It syncs `QualitySettings.antiAliasing` to the asset MSAA (`:257-265`), copies `enableLODCrossFade` (`:300`), sets the SRP Batcher flag (`:255`) and loads the default volume profile (`:267-268`). So `useSRPBatcher`, `enableLODCrossFade` and the volume default profile are not reliably live-switchable by a setter alone. The effect of `useSRPBatcher` at runtime is unverified.
- For MSAA the setter alone changes the camera target sample count (`:1408-1409`). Unity's own constructor also updates `QualitySettings.antiAliasing` (`:257-265`), and `SetQualityLevel` calls MSAA the example of an "expensive" change [S41]. A safe plan is to set both the asset value and `QualitySettings.antiAliasing`. This is a recommendation, not something the material proves.

### 1.3 What a change costs

- Render target reallocation: when render scale, MSAA, HDR format or shadow resolution changes, the target descriptor changes. `RenderingUtils.ReAllocateHandleIfNeeded` allocates a new target when the descriptor differs and passes the old one to a stale-resource pool (`URP/Runtime/RenderingUtils.cs:843-863`). Shadow maps use the same idea (`ShadowUtils.cs:621-630`). The render graph keeps unused resources for 10 frames before purging (`CORE/Runtime/RenderGraph/RenderGraphResourcePool.cs:24, 199`). So expect a short allocation hitch and a memory spike (old and new targets alive together) at the moment of change. The size of the hitch was not measured in the material.
- Cheap changes: `shadowDistance` (culling range only, `:1592`), cascade splits, bias values, `fsrSharpness`. No new target.
- `shadowCascadeCount` changes the shadow atlas layout. The shadow target descriptor is rebuilt only if width, height or format change (`MainLightShadowCasterPass.cs:213-222`, `UpdateTextureDescriptorIfNeeded`). Likely cheap; the exact reallocation behaviour is unverified.
- Do not animate `renderScale` every frame: each new size reallocates. For continuous scaling Unity offers dynamic resolution instead (Q7).
- Render scale is ignored inside a dead zone: if `abs(1 - renderScale) < 0.05` the camera uses 1.0 (`UniversalRenderPipeline.cs:1502-1506`). Presets must use 0.9 or lower, not 0.97.

### 1.4 Soft shadows: why quality is a light property here

- On Mac and iOS `ShadowUtils.SupportsPerLightSoftShadowQuality()` returns true (`ShadowUtils.cs:673-683`; false only for XR mobile). Then the three keywords `_SHADOWS_SOFT_LOW/MEDIUM/HIGH` are forced off (`:691-699`) and only `_SHADOWS_SOFT` is used.
- The quality is passed as a number in the shadow params: `SoftShadowQualityToShaderProperty` multiplies by the light's quality, which falls back to the asset when the light says `UsePipelineSettings` (`:659-668`). The shader branches on `shadowParams.y > SOFT_SHADOW_QUALITY_OFF` (`Shadows.hlsl:322-330`, `:138`).
- This matches the project asset, which prefilters the three quality keywords away (`FrontRooms_URP.asset:126-128`: `m_PrefilterSoftShadowsQualityLow/Medium/High: 1`) while keeping `_SHADOWS_SOFT` (`m_PrefilterSoftShadows: 0`).
- Result: soft shadow quality can be changed at runtime with no new variants and no asset change, by setting `UniversalAdditionalLightData.softShadowQuality` on each shadow-casting light. To change the asset-wide default (`UsePipelineSettings`) you need reflection.

### 1.5 Upscaling filters in URP 17.3.0

- The enum `UpscalingFilterSelection` has exactly: `Auto`, `Linear` (Bilinear), `Point` (Nearest-Neighbor), `FSR` (AMD FSR 1.0), `STP` (`UniversalRenderPipelineAsset.cs:342-377`). An extra `IUpscaler` entry exists only under `#if ENABLE_UPSCALER_FRAMEWORK`.
- That define is not set in this project: neither asmdef contains it, and the compiled URP and Core assemblies in `Library/ScriptAssemblies` contain no `UpscalerRegistry`, `IUpscaler`, `STPIUpscaler` or `FSR2IUpscaler` names. So the new "upscaler framework" code in the package is compiled out. [S33]
- Resolution logic (`UniversalRenderPipeline.cs:2246-2312`):
  - default is Linear;
  - FSR falls back to Auto if `FSRUtils.IsSupported()` is false; STP falls back to Auto if `STP.IsSupported()` is false or the render graph is off;
  - Auto picks Point only when the scale is an integer ratio and the image size divides evenly, else Linear.
- Requirements on Metal:
  - STP: `STP.IsSupported()` is true when `SystemInfo.supportsComputeShaders` and the device is not OpenGL ES 3 (`CORE/Runtime/STP/STP.cs:19-33`). Unity's page: STP needs Shader Model 5.0, does not run on GLES, needs TAA and enables it automatically; mobile uses lighter filtering [S53]. Metal appears in the `#pragma target` table for 4.5 and 5.0 [S56], so STP and FSR 1 (which the asset tooltip says needs shader model 4.5, `UniversalRenderPipelineAsset.cs:365`) are expected to run on Metal. A run on a real iPhone or iPad was not done: unverified on device.
  - FSR 1.0 and STP stay active at render scale 1.0 and then act as an image filter (`UniversalRenderPipeline.cs:1531-1536`; docs [S45]). Nearest-Neighbor "doesn't work when Post-processing is enabled" [S45]. This project always has post-processing on, so Point is not a useful preset choice. Avoid render scales of exactly 0.5 or 0.25 with the Auto filter: Auto would pick Point there (`UniversalRenderPipeline.cs:2266-2280`).
- STP conditions in this project (important):
  - STP forces the camera to TAA (`UniversalRenderPipeline.cs:1539-1542`).
  - TAA runs only with post-processing on, **no MSAA** (`msaaSamples == 1`), **no camera stack**, **no dynamic resolution** and a renderer with motion vectors (`UniversalCameraData.cs:502-508`). If any condition fails, STP behaves like plain TAA (`:524-527`).
  - The project asset uses MSAA 4 (`FrontRooms_URP.asset:28`). So an STP preset must also set MSAA to 1 (the temporal filter then replaces MSAA). Whether the custom Metal glass ray-tracing feature and the other custom materials work with TAA jitter and motion vectors is unverified.
  - Unity lists STP as "not with dynamic resolution" in URP [S54, S53].
- Cost of changing the filter: `upscalingFilter` is read each frame (`:1509`). Switching to or from STP changes the AA mode and history buffers, so expect target allocation. Not measured.

## Q2. Renderer features: SSAO on/off, SSAO settings, keyword and variant effects

### 2.1 Switching the feature on and off

- `ScriptableRendererFeature.SetActive(bool)` is public and only stores `m_Active` (`URP/Runtime/ScriptableRendererFeature.cs:14-18, 123-126`).
- The renderer checks `isActive` every frame before it calls a feature: `ScriptableRenderer.cs:1637` (pre-cull), `:1657` (add render passes), `:1700` (setup). An inactive feature adds no pass, so its cost goes away on the next frame.
- The feature object is reachable without reflection: `UniversalRenderPipelineAsset.rendererDataList` is public (`UniversalRenderPipelineAsset.cs:687`) and `ScriptableRendererData.rendererFeatures` is public (`ScriptableRendererData.cs:51-54`). The class `ScreenSpaceAmbientOcclusion` is public (`ScreenSpaceAmbientOcclusion.cs:118`). So `foreach (var f in data.rendererFeatures) if (f is ScreenSpaceAmbientOcclusion) f.SetActive(false);` is possible. The same call works for the project's custom Metal glass RT feature if it derives from `ScriptableRendererFeature` (it is listed as a renderer feature in `FrontRooms_URP_Renderer.asset:94-96`; its class was not read).
- Caution for Editor tests: the feature is a project asset. A `SetActive` call in Play mode changes the loaded asset object. Restore it on exit. (General Unity behaviour, not read in the material.)

### 2.2 Changing SSAO settings (downsample, samples, blur)

- There is **no public API**. The settings class `ScreenSpaceAmbientOcclusionSettings` is `internal`, the field `m_Settings` is `private`, and the accessor is `internal ref settings` (`ScreenSpaceAmbientOcclusion.cs:121, 130`; class declared near the top of the same file). Downsample, Samples, Blur Quality, Source, Normal Quality, After Opaque, Radius, Intensity and Falloff are all `internal` fields. Changing them needs reflection (unverified under IL2CPP) or a separate renderer/asset.
- The pass copies the settings each frame into material parameters (`ScreenSpaceAmbientOcclusionPass.cs:103-123`). Downsample is a plain number: `1.0f / (settings.Downsample ? 2 : 1)` (`:120`). So Downsample and Radius, Intensity and Falloff are value changes, not variant changes.
- Unity's reference gives the cost ranking for this feature [S48]: Downsample "very high" impact (halves the AO resolution, quarter the pixels), Blur Quality "very high" (Bilateral 3 passes, Gaussian 2, Kawase 1), Samples "high" (12 / 8 / 4; 4 to 8 doubles GPU load), Radius "high", Normal Quality "medium", After Opaque "medium". The page says nothing about mobile for these values beyond After Opaque helping tile-based GPUs.
- The project's current values (`FrontRooms_URP_Renderer.asset:71-81`): BlueNoise, Downsample off, After Opaque off, Source DepthNormals, Normal samples High, Samples High (12), Blur High (Bilateral).

### 2.3 Keyword and variant consequences (this is the important part)

1. The feature drives one global keyword on lit shaders. The pass sets `_SCREEN_SPACE_OCCLUSION` true when it runs and After Opaque is off (`ScreenSpaceAmbientOcclusionPass.cs:457, 565`) and sets it false in cleanup (`:666-667`). The renderer also resets the AO parameter vector to zero (`ScriptableRenderer.cs:1731`). Lit shaders compile both states: `Lit.shader:146` has `#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION`. With the keyword off the shader uses factor 1.0 (`ShaderLibrary/AmbientOcclusion.hlsl:28-35`).
2. At build time URP removes the OFF variant when "Strip Unused Variants" is on and every renderer of the asset has an active SSAO feature (`URP/Editor/ShaderBuildPreprocessor.cs:1107-1117`, `everyRendererHasSSAO` at `:636-657`). Inactive features are not counted at all (`:828-830`).
3. This project is in exactly that state: `m_PrefilteringModeScreenSpaceOcclusion: 2` (`FrontRooms_URP.asset:109`) and the enum is `Remove, Select, SelectOnly` = 0, 1, 2 (`UniversalRenderPipelineAssetPrefiltering.cs:12-17`, "SelectOnly: selects the keyword and removes others"). Global Settings have `m_StripUnusedVariants: 1`. So **a player build today carries no SSAO-OFF variants for URP lit shaders**.
4. Consequence: calling `SetActive(false)` on the SSAO feature in a build made from the current setup asks lit shaders for a variant that was stripped. What Unity then draws is not confirmed by any page that was opened (Player Settings has `strictShaderVariantMatching: 0`). Treat it as unsafe until tested in a real build.
5. The SSAO shader's own keywords are also tied to the build. Variants are kept only for the values used by active SSAO features (`ShaderBuildPreprocessor.cs:1119-1141`). The asset's prefilter flags show which are gone: `m_PrefilterSSAOSampleCountLow/Medium: 1` (removed), `High: 0` (kept); `m_PrefilterSSAOSourceDepthLow/Medium/High: 1` (removed), `DepthNormals: 0` (kept); `Interleaved: 1` (removed), `BlueNoise: 0` (kept) (`FrontRooms_URP.asset:114-122`). So Samples Low/Medium, Source = Depth and the Interleaved method cannot be chosen at runtime from this build. Downsample is safe (no keyword). Blur quality has no keyword in the constants list (`ScreenSpaceAmbientOcclusion.cs:131-140`), so it is probably a pass choice, not a variant; unverified.
6. The SSAO shader and noise textures are loaded from global settings resources. The error text says "If this exception appears in the Player, make sure at least one ScreenSpaceAmbientOcclusion render feature is enabled or adjust your stripping settings" (`ScreenSpaceAmbientOcclusion.cs:191-196`). So a build with no active SSAO feature may not carry the resources; unverified.
7. When SSAO runs the pass asks for the depth-normals prepass (`ScreenSpaceAmbientOcclusionPass.cs:179-182`: `ConfigureInput(Depth | Normal)`). Turning the feature off should also remove that input, unless another feature requests it. Not verified for this project.

### 2.4 Ways to make an SSAO switch safe

- **Second renderer, no SSAO (recommended).** Add a second `UniversalRendererData` to the asset's renderer list without the SSAO feature. Then `everyRendererHasSSAO` is false and the preprocessor keeps both variants (`ShaderBuildPreprocessor.cs:1112-1116`: "Otherwise we keep both"). At runtime switch per camera with `UniversalAdditionalCameraData.SetRenderer(index)` (`UniversalAdditionalCameraData.cs:688-691`, index maps to `m_RendererDataList`, `UniversalRenderPipelineAsset.cs:922-935`). No pipeline recreation. The same trick can drop the glass RT feature, change Forward+ settings, and so on. Unverified: hitch size of a renderer switch, memory cost of the second renderer.
- **Tier asset with its own renderer (no SSAO).** The build keeps a variant if any renderer of any included asset needs it (Q3). Same effect, but switching it recreates the pipeline.
- **Turn off Strip Unused Variants.** Keeps all variants. Larger build and longer build time; changes the desktop build too, so it conflicts with the "desktop byte-identical" rule (variants are not looks, but the build output changes).

## Q3. Different pipeline assets per quality level

### 3.1 How the swap works

- Unity's URP page (6.3): assign a different URP asset to each quality level in Project Settings > Quality, then call `QualitySettings.SetQualityLevel()` from a script and the level's asset becomes active. Quality-level indices count only the levels enabled for the target platform. Do it in a place where performance does not matter, such as a loading screen or a static menu. Unity warns that the change has a "temporary but significant performance impact" [S49].
- `SetQualityLevel(i, applyExpensiveChanges)`: use `true` (the default) when applying options from a game's settings menu, `false` for dynamic runtime adjustment. Anti-aliasing is the example of an expensive change. In builds, levels not used by the target platform are stripped, so query the available levels and use the returned index. The page does not mention render pipeline asset switching [S41].
- Which asset is active: `QualitySettings.renderPipeline` (per level) overrides `GraphicsSettings.defaultRenderPipeline`; `GraphicsSettings.currentRenderPipeline` returns the active one; `RenderPipelineManager.currentPipeline` updates only after one frame has rendered with the new pipeline [S51].
- What happens inside: the 6.3 SRP page does not describe it. An older page (Unity 2021.1, Spanish) says the current pipeline instance is discarded and the new asset's `CreatePipeline()` is called, and the cost depends on how big the change is [S52]. That is an old-version page. The URP 17.3.0 source confirms the shape:
  - `UniversalRenderPipelineAsset.CreatePipeline()` runs `DestroyRenderers()`, `new UniversalRenderPipeline(this)`, `CreateRenderers()` and reinitialises the GPU resident drawer (`UniversalRenderPipelineAsset.cs:811-835`).
  - The pipeline constructor builds a new `RenderGraph`, a new RT-handle pool, global keywords, blitter and volume defaults (`UniversalRenderPipeline.cs:250-292`). Its `Dispose` destroys the renderers, cleans up the render graph and the RT-handle pool, releases constant buffers, deinitialises the `VolumeManager` and disposes per-camera history (`:331-370`).
  - So every intermediate target, the shadow maps, the TAA history and the renderer feature resources are rebuilt. That explains the "significant" cost. The real duration on iPhone or iPad was not measured in the material: unverified.
- Not confirmed: whether `SetQualityLevel` between two levels that point to the **same** asset recreates the pipeline. The engine side is not in the material. The project is in that state today (one asset for all six levels), so a test is needed before relying on either answer.

### 3.2 Shader variant stripping with several assets

- Which assets are in a build: the assets referenced by the Quality levels enabled for the build target, plus `GraphicsSettings.defaultRenderPipeline` if any level has none, plus extra assets chosen by scene reference or label (`CORE/Editor/BuildTargetExtensions.cs:85-109`; the extra options are off in this project, `GlobalSettings.asset:397-398`). Unity's 6.2 URP page says the same [S70] (page version is 6.2, URP 17.2).
- How many variants are kept: each renderer of each included asset gives one "feature set" (`ShaderBuildPreprocessor.cs:449-505`). The stripper runs over all sets and keeps a variant unless **every** set says it is unused (`URP/Editor/ShaderScriptableStripper.cs:1251-1277`, comment "All feature sets need to have this variant unused to be stripped out"). Unity's page: to strip a feature, disable it in every asset in the build; mixed settings across assets prevent stripping; "Strip Unused Variants" must be on [S70].
- What that means here:
  - Adding mobile tier assets with fewer features does **not** remove anything from the Mac/Windows build, because the desktop tier still asks for the variants. By this rule it can only add variants (for example the SSAO-OFF variant), subject to the prefilter caveat below.
  - Tier assets that differ in rendering path (Forward vs Forward+) double the variants for each keyword [S70]. Keep every tier on Forward+.
  - Keep platform lists apart: set `excludedTargetPlatforms` on tier levels so the Mac player does not carry iOS-only assets and the reverse. (`QualitySettings.asset` has an `excludedTargetPlatforms: []` field on every level; `BuildTargetExtensions` asks for assets "for platform" at `:94`; Unity says levels unused by the target platform are stripped [S41].)
- The per-asset `m_Prefilter*` fields (for example `FrontRooms_URP.asset:103-129`) are **build output**, not hand-set options. Before every build `ShaderBuildPreprocessor` recomputes them for each URP asset, marks the asset dirty and saves it (`ShaderBuildPreprocessor.cs:477-500`). Do not edit them by hand, and expect them to change in version control after a build.
- Not confirmed by anything opened: how Unity's `ShaderKeywordFilter` rules (the `[ShaderKeywordFilter.RemoveIf(...)]` attributes on those fields, `UniversalRenderPipelineAssetPrefiltering.cs:140-153`) from two different assets combine in one build. URP computes each asset's prefilter data from that asset's own features (`:477`), so a tier asset that turns a feature off might prefilter away a keyword a desktop asset still needs. The scriptable stripper above uses the union. Test with a two-asset build and read the stripping report before committing to tier assets. Global Settings already have `m_ShaderVariantLogLevel` and `m_ExportShaderVariants` fields (`GlobalSettings.asset:17-18, 25-26`); what they print was not checked.
- Post-processing variants are separate: with `m_StripUnusedPostProcessingVariants: 0` (this project) all volume-feature variants are kept (`ShaderBuildPreprocessor.cs:263-266`, `GetEveryVolumeFeatures`). So bloom LQ/HQ, grain, chromatic aberration and so on can be toggled at runtime safely (Q4).

### 3.3 Recommended Unity 6 pattern for user-facing presets

From Unity's two pages [S49, S50] and the performance page [S46]:

1. One URP asset per tier, assigned to a quality level (names, not indices).
2. Pick the level from script on start (the Unity example uses `SystemInfo.graphicsMemorySize`), and apply a user's choice from the settings menu with `SetQualityLevel(i, true)`.
3. Fine-tune with the public setters on `GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset` (the Unity example sets `renderScale` and `shadowDistance` per memory tier). Unity says you can combine both methods so you do not need a quality level for every hardware case.
4. Do both at a loading screen or static menu.

Cautions found while reading:

- `currentRenderPipeline` is the shared asset object. A setter changes that object in memory for everyone who uses it. In the Editor, Play-mode changes to a project asset persist in memory and could be saved. Re-apply all values from a saved preset at start-up and restore on exit in Editor runs. For the desktop tier, do not call setters at all (see the architecture section).
- Quality level indices differ per platform (`SetQualityLevel` page [S41]). Store the preset by name.

### 3.4 Hitch when switching

- Evidence: the Unity warning above [S49] and the teardown/rebuild in the source. No timing for Metal on iPhone or iPad exists in the material.
- Practical rule: switch behind a fade or a loading cover; never switch during play. Prefer property setters (next-frame effect, one target reallocation each) to asset swaps for small changes.

## Q4. Post-processing at runtime (Volume overrides, per-quality cost)

### 4.1 What can be changed and how

- Bloom exposes all the knobs as public fields (`URP/Runtime/Overrides/Bloom.cs`): `intensity` (`:182`), `highQualityFiltering` (`:208`), `filter` (Gaussian, Dual, Kawase; `:214`, enum `:24-40`), `downscale` (Half, Quarter; `:220`, enum `:8-17`), `maxIterations` (clamped 2 to 8, default 6; `:226`). Bloom is active when `intensity > 0` (`:245`).
- Unity's bloom page (6.3) [S47]:
  - High Quality Filtering uses bicubic instead of bilinear; smoother, more GPU cost; turn it off on low-end or constrained hardware.
  - Filter: Gaussian is the default and best quality; Dual is faster and "aimed at mobile"; Kawase uses least memory and is fastest at low resolution; each looks different and needs its own tuning.
  - Downscale: Quarter is recommended for best performance; it cuts the first blur cost. (The page text read does not describe Half.)
  - Max Iterations: lower it to improve performance, especially on high-DPI mobile screens.
- How the project builds its volume: `FrontRoomsPostStack.Ensure` makes a global `Volume` (priority 0) and sets `sharedProfile = Resources.Load("Rendering/FrontRoomsPost")` (`Assets/Scripts/Rendering/FrontRoomsPostStack.cs:17, 41-43`); zone volumes such as `FrontRoomsPost_Office` override a few values (white balance, saturation, contrast, grain) (`:35-53`).
- Editing a profile at runtime:
  - `Volume.sharedProfile` is the asset itself (`CORE/Runtime/Volume/Volume.cs:63`). Writing to it changes the asset object for everyone.
  - `Volume.profile` makes a per-volume copy on first access and is documented as safe to edit at runtime without changing the original asset (`Volume.cs:66-100`; `HasInstantiatedProfile()` at `:120`). Use it for tweaks to the existing film volume.
  - A parameter only takes effect if its `overrideState` is true (`CORE/Runtime/Volume/VolumeComponent.cs:289`), and a component only if `active` (`VolumeManager.cs:596`).
- **Cleanest option for presets: a second global Volume for quality, higher priority than the film volume and the zone volumes.** `VolumeManager` skips a volume that is disabled or has `weight <= 0` (`CORE/Runtime/Volume/VolumeManager.cs:766-768`) and blends a global volume by `weight` (`:771-773`; `Volume` has `priority`, `weight`, `isGlobal`: `Volume.cs:23-51`). A quality volume that only overrides bloom (HQ off, Dual or Kawase, Quarter, fewer iterations), grain, chromatic aberration and lens distortion leaves the film profile untouched. On desktop that volume stays disabled, so the desktop frame is unchanged. The exact priority sort order is not shown in the lines read; verify that a higher priority wins before relying on it.
- The URP asset has a "quality default" volume profile slot (`UniversalRenderPipelineAsset.volumeProfile`, setter at `:1603`; handed to `VolumeManager.Initialize` in the pipeline constructor, `UniversalRenderPipeline.cs:267-268`; `VolumeManager.SetQualityDefaultProfile`, `VolumeManager.cs:304`). This sets component *defaults* below all volumes. The film volume overrides most parameters on top of it, so it is not a way to turn the film look down. It does not change when you swap tiers by property setters.

### 4.2 Variant and build effects

- Bloom quality is a keyword in the uber post shader: `_BLOOM_LQ`, `_BLOOM_HQ`, `_BLOOM_LQ_DIRT`, `_BLOOM_HQ_DIRT` (comment at `ShaderBuildPreprocessor.cs:359`; stripping logic `:334-370`).
- This project sets `m_StripUnusedPostProcessingVariants: 0` (`GlobalSettings.asset:22, 28, 254`), so all volume-feature variants are kept (`ShaderBuildPreprocessor.cs:263-266`). Toggling HQ filtering, grain or chromatic aberration at runtime is therefore build-safe today.
- If anyone turns that stripping on later, only features used by `VolumeProfile` assets under `Assets/` are kept (`:334-370`). A preset that enables HQ bloom would then need a profile in `Assets/` that uses it.
- First use of a variant that has not been drawn yet may cause a one-time hitch on Metal. This is common Unity behaviour but no opened page confirms it. Unverified; plan a warm-up frame behind the loading cover.

### 4.3 Cost notes for mobile

- Unity's "Configure for better performance in URP" page (6.3) [S46] lists, for the URP asset: turn off Depth and Opaque textures unless needed; HDR off if not needed, else 32-bit precision; lower main-light shadow resolution, atlas resolution, shadow distance and cascade count; disable additional-light shadows; reduce or disable MSAA; set Store Actions to Auto or Discard on lower-end mobile; set Volume Update Mode to "Via Scripting"; on lower-end mobile disable LOD Cross Fade and soft shadows (or keep a lower quality); disable reflection probe blending and box projection. For the renderer: Intermediate Texture "Auto", Depth Priming Disabled on mobile, Depth Texture Mode "After Transparents", enable Native RenderPass with Metal.
- Unity's page on soft shadows says they have a high cost on tile-based GPUs, and Low is "a good balance for mobile" [S45].
- Unity's mobile post-processing page was not reachable (HTTP 404 for `.../urp/post-processing/post-processing-mobile.html`). So there is **no Unity-sourced ranking** of film grain, chromatic aberration or lens distortion cost on mobile in this material. Unverified. Measure them with the GPU timers in Q10.
- The project's current volume state (`volumeFrameworkUpdateMode`): `m_VolumeFrameworkUpdateMode: 0` (`FrontRooms_URP.asset:94`), the asset property is read-only (`UniversalRenderPipelineAsset.cs:1594`). Changing it needs a separate asset.

## Q5. QualitySettings at runtime: what applies, what URP ignores

### 5.1 What URP reads from QualitySettings

Searching all `QualitySettings.` uses in the URP runtime source (`URP/Runtime/**/*.cs`, write-up check) gives only these:

- `antiAliasing`: read and **overwritten** from the asset in the pipeline constructor (`UniversalRenderPipeline.cs:257-265, 271`).
- `enableLODCrossFade`: **written** from the asset (`:300`).
- `activeColorSpace` (many places), `shadowmaskMode` (`ForwardLights.cs:533`, `DeferredLights.cs:369`), `billboardsFaceCameraPosition` (`ScriptableRenderer.cs:404`), `renderPipeline` (Editor-only reflection-probe code, `URPReflectionProbeSettings.cs:45`).
- The core package's GPU Resident Drawer culler reads `lodBias`, `meshLodThreshold`, `maximumLODLevel`, `enableLODCrossFade` (`CORE/Runtime/GPUDriven/InstanceCuller.cs:1865, 1898, 1968`). The drawer is off in this project (`m_GPUResidentDrawerMode: 0`, `FrontRooms_URP.asset:86`).

Everything else in the Quality panel is either engine-level (it acts without URP) or ignored by URP.

### 5.2 Shadows: ignored by URP

- Unity's Quality Settings page says the detailed shadow properties (Shadows, Shadow Resolution, Projection, Distance, cascades) apply only to the Built-in pipeline; for URP use the asset. Soft Particles and LOD Cross Fade in the panel are also Built-in only [S43].
- The source agrees: URP takes shadow distance, resolutions and cascade count from the asset (`UniversalRenderPipeline.cs:1592, 1764-1768`) and never reads `QualitySettings.shadow*`.
- Trap in this project: the quality levels still carry shadow values (for example `shadows: 0` on Very Low and Low, `shadowDistance: 15 / 20 / 20 / 48 / 70 / 48`, `QualitySettings.asset:12-16, 65-69, 118-122, 171-175, 224-228, 277-281`). They do nothing under URP. A "Very Low" level does **not** turn shadows off, because all six levels share one asset with 2048 shadows and 40 m distance.
- Likewise the quality level's `antiAliasing: 4` and `enableLODCrossFade: 1` are overwritten by the asset on pipeline creation.

### 5.3 The settings that do act

| Setting | Runtime change | Evidence and cost |
|---|---|---|
| `QualitySettings.globalTextureMipmapLimit` | Yes. No reload needed | Unity re-uploads already loaded affected textures immediately; streaming textures re-upload asynchronously; textures with an upload in progress can take a few frames. Applies to 2D and 2D-array textures only (not cubemaps, render targets, non-mipmapped textures, virtual texturing). A value that was stripped at build is replaced by the nearest unstripped value. Raising it cuts video memory [S42]. Project: `mipStripping: 0` (`ProjectSettings.asset:55-56`), so all mips ship; `streamingMipmapsActive: 0` on all levels (`QualitySettings.asset:40, 93, 146, 199, 252, 305`), so the immediate path is the one used. Hitch size on a large scene: not measured. `FrontRooms3DGame.cs:502` forces the limit to 0 at start, which would undo a level's value (Very Low has 1). |
| `QualitySettings.lodBias` | Yes (a static property). Cheap | Global LOD Group switch bias, minimum 0.1; affects LOD Groups only, not mesh LOD [S44]. Project values 0.3 to 2 across the six levels (`QualitySettings.asset:36, 89, 142, 195, 248, 301`). |
| `QualitySettings.maximumLODLevel` | Property exists | The page is one line with no runtime or build notes, so whether it affects what is included in a build is unverified [S44]. |
| `QualitySettings.anisotropicFiltering` | Property exists | The page only shows `Disable`; Enable and ForceEnable are not described on it. Runtime behaviour and cost unverified [S44]. The levels use `anisotropicTextures` 0, 0, 1, 1, 2, 2. |
| `QualitySettings.realtimeReflectionProbes` | Listed in the Rendering group [S43] | Not referenced in URP source (no match), so it is engine-level. How and when it applies at runtime is unverified. |
| `QualitySettings.resolutionScalingFixedDPIFactor` | Settable by script (sample sets it in `Start`) | Multiplier on the target DPI, only in resolution scaling mode; the page does not say when a change applies [S44]. The project mode is Disabled (`ProjectSettings.asset:163`), so it does nothing today. |
| `QualitySettings.vSyncCount` | Yes | See Q6. Ignored on iOS. |
| `QualitySettings.SetQualityLevel(i, applyExpensive)` | Yes | See Q3. |

### 5.4 Consequence for the design

- Use the URP asset (or a camera or light) for anything that is a shadow, MSAA or LOD cross-fade choice. Use QualitySettings only for texture mip limit, LOD bias, anisotropic filtering and vSync (desktop).
- Treat the six existing quality levels as a naming and asset-selection list, not as a source of values.

## Q6. Frame rate: iOS, iPadOS, macOS

### 6.1 iOS and iPadOS: `Application.targetFrameRate` is the only control

- Unity's `vSyncCount` page: on Android and iOS the value is **always ignored**, because mobile devices do not allow unsynchronised rendering; use `Application.targetFrameRate` there [S35].
- Unity's `targetFrameRate` page (6.3) [S34]:
  - Default is -1. On mobile with vSync 0 and target -1 the content renders at a fixed 30 fps to save battery, independent of the display's refresh rate.
  - If the target does not divide the display's current refresh rate evenly, it is **rounded down** to the nearest value that does. (The only worked example on the page is Android: 25 on a 60 Hz display gives 20.)
  - To match the display, set it to the integer refresh rate.
  - ProMotion (iOS only): when ProMotion is disabled in the project (the default) the native rate is 60 Hz; when enabled it is 120 Hz on iOS displays that support ProMotion and 60 Hz otherwise.
- What the player does with the number (Unity's trampoline, `TRAMP/UnityAppController+Rendering.mm:209-242`): take the requested FPS (or `UnityGetTargetFPS()` if <= 0); clamp it to `[UIScreen mainScreen].maximumFramesPerSecond` (`:218-229`); then set `preferredFrameRateRange = CAFrameRateRangeMake(fps, fps, fps)` (minimum, maximum and preferred all equal) on the `CADisplayLink` (`:240`). A change of `Application.targetFrameRate` at runtime reaches this code through `UnityFramerateChangeCallback` (`:268-271`), so switching 30, 40, 60 or 120 is immediate and cheap.
- The newer `CAMetalDisplayLink` path exists in the trampoline but is switched off: `shouldUseMetalDisplayLink` always returns NO with the comment "We get GPU Timeout errors when enabling the CAMetalDisplayLink" (`TRAMP/UnityAppController.mm:128-144`). So iOS in 6000.3.10f1 uses plain `CADisplayLink`.
- Core Animation treats the range as a hint and gives "the closest refresh rate possible"; changes take effect at the next display-link callback; the app must not assume the requested rate is used [S38].

### 6.2 ProMotion: what `appleEnableProMotion` adds

- Unity's iOS Player page: "Enable ProMotion Support" allows 120 Hz refresh rates on ProMotion displays; higher rates may increase battery use [S36].
- Apple's side [S37, S38]: the Info.plist key `CADisableMinimumFrameDurationOnPhone` (Boolean, default NO, iOS and iPadOS 15.0+) lets the app request frame rates above the system default on ProMotion displays. On iPhone, rates above 60 Hz are unavailable unless the key is `YES`. The iPad Pro does not need it. (A trade article says only iPhone 13 Pro and Pro Max were the affected models at the time [S71]; secondary.)
- That Unity writes that key when the setting is on: the iOS build program binary `iOSPlayerBuildProgram.exe` contains the strings `CADisableMinimumFrameDuration`, `CADisableMinimumFrameDurationOnPhone` next to other Info.plist keys, and `EnableProMotion` (python search, `iOSSupport/iOSPlayerBuildProgram.exe`) [S32]. The logic was not read, so this is consistent with the setting writing the key, not proof.
- Project state: `appleEnableProMotion: 0` (`ProjectSettings.asset:257`). So iPhones are held to 60 Hz today and `Application.targetFrameRate = 60` (`FrontRooms3DGame.cs:496`) is the native rate.
- Unverified: whether an iPad Pro, which does not need the plist key, can still run above 60 Hz when ProMotion support is off in Unity. The Unity page says 60 Hz is the native rate when it is off, but the clamp in the code above uses `maximumFramesPerSecond` from the OS.

### 6.3 How Unity picks 120, 60, 40 or 30; the 40 fps question

- There is no table in Unity. The value is the integer you set, limited by the display's maximum (6.1). The OS then picks the closest supported rate.
- Apple's supported ProMotion rates [S38]: iPhone 120, 80, 60, 48, 40, 30, 24, 20, 16, 15, 12, 10 Hz; iPad Pro 120, 60, 40, 30, 24 Hz. Both lists include 40.
- 40 fps on a 120 Hz display: allowed by the documented rule, since 120 / 40 = 3, no rounding [S34], and Unity requests exactly 40/40/40 (`:240`). It needs "Enable ProMotion Support" on, otherwise the native rate is 60 Hz and 40 would round down to 30 by the same rule (this last step is an inference from the rule, not stated for iOS on the page). **Not tested on a device: unverified.**
- Things that can stop it: Apple says the system disables faster refresh rates in Low Power Mode and when the device is hot, with no numeric caps [S38]. Then the achieved rate falls back and 40 cannot be honoured. Read the actual rate at runtime and show it in a debug overlay (`Resolution.refreshRateRatio` is the API named on the vSync page [S35]).
- No thermal frame-rate system for iOS is documented for 6.3 in the pages read. A 6.5 beta release note lists "iOS: Added a system that adjusts the frame rate based on the thermal state" [S62]; and the AP Editor settings page for 6.3 mentions an "Adjust iOS FPS based on thermal state" prompt [S60]. The project has no such setting (`ProjectSettings.asset` has no thermal entry; checked by grep).

### 6.4 `OnDemandRendering.renderFrameInterval`

- Static read/write property. It sets how often frames are rendered. The effective rate is the base rate (from `targetFrameRate` or `vSyncCount`) divided by the interval; the page's example is target 60 with interval 6 giving about 10 fps, "every sixth frame". 0 or 1 means normal rendering. No platform notes [S39]. The property exists in `UnityEngine.CoreModule.dll` of the iOS player (`renderFrameInterval` string found).
- Use: on a 120 Hz iPad Pro, `targetFrameRate = 120` and `renderFrameInterval = 3` would render at 40 fps while the player loop still runs at 120. The page does not say outright that scripts and input keep running at full rate; the example implies it. Verify before using it for input-heavy menus. A plain `targetFrameRate = 40` gives the same display pacing with less to verify.

### 6.5 macOS

- Desktop and Web: `vSyncCount` applies; when it is above 0, `targetFrameRate` is ignored and the frame rate is the display refresh rate divided by `vSyncCount` (60 Hz with 2 gives 30) [S35, S34]. Refresh rates can change at runtime (moving a window to another monitor, unplugging a laptop) [S35].
- Project: the Standalone default level is 5 (Ultra) with `vSyncCount: 1` (`QualitySettings.asset:297, 338`), so on a Mac the default frame rate is the display refresh rate and `targetFrameRate = 60` (`FrontRooms3DGame.cs:496`) is ignored. Only Very Low and Low have `vSyncCount: 0` (`:32, 85`), where the 60 fps cap is used.
- ProMotion MacBook Pro: **nothing in the material says** what refresh rate a Mac player gets or how Unity paces on it. Unverified. Two leads that were not read in full: a 6.5 beta release note "macOS: Added support for `CAMetalDisplayLink` on the Mac Player" [S62] (so macOS pacing is changing in newer Unity), and an Issue Tracker entry titled "Frame rate stutters in macOS Player when vSyncCount is 2 or more" whose body could not be read [S72]. Do not build a Mac frame-cap feature on this without a test on the target Mac.
- Rule for the project: leave the Mac/Windows frame-rate logic alone. Put any 30/40/60/120 choice behind an iOS/iPadOS check.

## Q7. Resolution on iOS and iPadOS

### 7.1 How the drawable size is chosen (trampoline)

- The main Unity view gets `contentScaleFactor = UnityScreenScaleFactor([UIScreen mainScreen])` (`TRAMP/UnityAppController+Rendering.mm:182`, also `UI/UnityAppController+ViewHandling.mm:97`, `UI/UnityView.mm:147`).
- `UnityScreenScaleFactor` (`TRAMP/Unity/DisplayManager.mm:752-776`): if `screen.nativeScale` is NaN or 0 return 1.0; otherwise ask `UnityCalculateScalingFactorFromTargetDPI(screen)` and return it if it is above 0; else return **`screen.nativeScale`**. The comment (`:757-759`) says it queries `nativeScale` "to get the true device resolution" and avoid needlessly large frame buffers. So the default (Resolution Scaling Mode = Disabled) is the true native pixel size.
- The Metal layer's `drawableSize` is the view bounds times that factor (`TRAMP/UI/UnityView.mm:55-75`; surface sizes at `DisplayManager.mm:197-199, 222-226`). The code comment at `:196` says: "On metal we depend on hardware screen compositor to handle upscaling this way avoiding additional blit".
- Unity's iOS Player page: Resolution Scaling Mode "Disabled" renders at native resolution; "Fixed DPI" lowers the render resolution when the screen DPI exceeds the Target DPI; the scale is `min(Target DPI x Factor / Screen DPI, 1)` with the factor from the Quality setting `resolutionScalingFixedDPIFactor` [S36, S44]. Project: Disabled, `targetPixelDensity: 30` (unused) (`ProjectSettings.asset:162-163`).
- With Fixed DPI the factor from `UnityCalculateScalingFactorFromTargetDPI` becomes the `contentScaleFactor`, so the **whole** drawable, UI included, is smaller and the OS compositor scales it up. Whether Fixed DPI can be changed live (the factor is read when the view is made) is unverified; the `resolutionScalingFixedDPIFactor` page does not say when a change applies [S44].

### 7.2 `Screen.SetResolution` on iOS

- Unity's page does not cover iOS at all (no word on whether render resolution changes or the OS upscales). It says the change takes effect at the end of the current frame [S40]. So there is no Unity statement to rely on.
- The trampoline has an entry for a rendering-resolution request: `UnityDisplayManager_SetRenderingResolution` calls `UnityRequestRenderingResolution(w, h)` for the main screen (`DisplayManager.mm:712-727`). The next present then re-creates the surface with `renderW/renderH` and sets `targetW/targetH` (`:317-335`, `:192-232`).
- When the target size differs from the system (native) size, `CreateUnityRenderBuffersMTL` makes an interim colour target ("needInterimColor") and the swapchain blit copies it to the backbuffer (`TRAMP/Unity/MetalHelper.mm:90-91, 142`). A source comment says Unity moved to OS scaling by tweaking `CAMetalLayer.drawableSize` (`DisplayManager.mm:661-671`). Both mechanisms are in the source. Which one runs on Metal when script requests a size, and which managed call reaches this export, is unverified. Do not use `Screen.SetResolution` as the iOS quality knob without a device test.

### 7.3 URP render scale and UI

- Unity's URP asset page (6.3): Render Scale "scales the render target resolution, not the device's resolution. UI is still drawn at the device's native resolution" [S45].
- The source is consistent with this: URP draws the Screen Space Overlay UI itself, right after the `AfterRendering` injection point, into the screen target, only for the last base camera (`UniversalRendererRenderGraph.cs:1545-1547`; `rendersOverlayUI = rendersUIOverlay && resolveToScreen`, `UniversalCameraData.cs:395`). URP turns this ownership on whenever it renders at least one camera and no XR display is active (`UniversalRenderPipeline.cs:2594-2609`, called at `:448`).
- The UI pass draws uGUI and UI Toolkit together: it asks for an overlay renderer list with `UISubset.UIToolkit_UGUI` (`URP/Runtime/Passes/DrawScreenSpaceUIPass.cs:200, 249`) and a separate `UISubset.LowLevel` list (`:223, 266`). So the project's Screen Space - Overlay canvas and UI Toolkit panels both stay sharp at the drawable size when `renderScale` is below 1, and the scaled 3D image is upscaled underneath them.
- If HDR display output were on, URP renders the UI offscreen first (`UniversalRendererRenderGraph.cs:1331-1333`). The project has `useHDRDisplay: 0`, so this path is not used.
- Not covered by anything read: a Screen Space - Camera canvas or a render-texture UI would follow the camera's scaled target. The project uses overlay and UI Toolkit, so this does not apply today.

### 7.4 Dynamic resolution on Metal

- Unity's dynamic resolution page (6.3): iOS, macOS and tvOS support it on Metal only (Android Vulkan only; Windows and UWP DirectX 12 only). It is a Camera setting. "Unity does not re-allocate render targets": targets are allocated at full size and a portion is used [S55]. The page does not explain `camera.allowDynamicResolution` or `ScalableBufferManager`; it only links to their API pages.
- URP source: "URP uses the camera's allowDynamicResolution flag to decide if useDynamicScale should be enabled for camera render targets", and it forces `RTHandles.SetHardwareDynamicResolutionState(true)` each frame (`UniversalRenderPipeline.cs:480-485`). There are two ways to scale: the `ScalableBufferManager` API (dynamic resolution) and the asset `renderScale`; "cameraData.renderScale resizes screen space textures, while dynamic resolution (ScalableBufferManager) doesn't and instead uses memory aliasing... can be used together" (`UniversalRenderer.cs:2050-2066`).
- Limits: TAA, and therefore STP, is switched off when `camera.allowDynamicResolution` is true (`UniversalCameraData.cs:507`); Unity's table says STP is "not with dynamic resolution" in URP [S54]. A 6.5 beta note fixes "graphical corruption when using dynamic resolution with STP upscaling when using the Metal graphics API" [S62], which suggests the combination was unreliable earlier.
- Confirmed parts: the API names and the URP hooks. Not verified: how well the project's custom Metal glass RT feature and other custom passes cope with a scaled viewport; how to drive the scale from `FrameTimingManager` (Unity's page for that was not read); cost of the first scaled frame.
- When to use which:
  - Fixed preset (set once, 0.67, 0.75, 0.85): `renderScale` (reallocates once).
  - Continuous GPU-load following: dynamic resolution (no reallocation per change) but no STP/TAA.

## Q8. Upscaling on Apple: MetalFX, STP, FSR 1

### 8.1 MetalFX in Unity 6.3 URP: not supported out of the box

- Package source: a case-insensitive search for "metalfx" in the URP 17.3.0 and SRP Core 17.3.0 packages (all files, including `Documentation~`) returns nothing (original agent and write-up check). Neither package changelog has an entry about it (the only "upscal" entries are STP, FSR and the pixel-perfect pass: `CORE/CHANGELOG.md:152, 246`; `URP/CHANGELOG.md:65, 113, 665, 765`).
- Unity's docs: the "Introduction to changing resolution scale" page for 6.3 lists, for URP, only Bilinear, Nearest-neighbor, STP (not with dynamic resolution) and FSR 1. The words Metal, Apple, MetalFX, iOS and macOS do not appear on it. The same is true of the unversioned page, which showed "Unity 6.6" when fetched, so the table did not gain MetalFX by 6.6 either [S54].
- The enum in URP 17.3.0 is `Auto, Linear, Point, FSR, STP` (Q1).

### 8.2 What exists for Apple GPUs

| Option | In URP 17.3.0? | Works on Metal? | Notes |
|---|---|---|---|
| Bilinear | yes | yes | default filter |
| Nearest-neighbor | yes | yes | not usable with post-processing on [S45] |
| FSR 1.0 | yes | expected (needs shader model 4.5; falls back to Auto if unsupported) | spatial only; sharpness settable; stays on at scale 1.0 [S45]; `UniversalRenderPipelineAsset.cs:365` |
| STP | yes | expected (compute; not GLES; Metal lists SM 4.5 and 5.0 with compute [S56]) | needs TAA, so no MSAA, no camera stack, no dynamic resolution (`UniversalCameraData.cs:502-508`); lighter filtering on mobile [S53] |
| `IUpscaler` framework (custom upscalers, FSR2 and STP as `IUpscaler`) | in source, **compiled out** | n/a | needs `ENABLE_UPSCALER_FRAMEWORK`, which no asmdef sets (Q1.5). How it is meant to be enabled is not stated anywhere in the material |
| MetalFX Spatial or Temporal | no | n/a | third-party native plugins only (below) |

None of these was run on an iPhone or iPad. All "expected" entries are from the docs and the source conditions, not from a device test.

### 8.3 Third-party MetalFX evidence (weak)

- An Apple Developer Forums thread (no replies) reports a Unity URP sample project called "MetalFXSamples" on an iPhone 13 with an iOS beta: GPU cost about 14 ms at native scale and about 26 ms with MetalFX Spatial at render scale 0.8 or 0.5, and a blurrier image [S57]. One anecdote, undated in the extract, on an old beta. It does not show how MetalFX performs on current devices.
- A second thread (Unity 2022.3.62f3, HDRP, custom MetalFX Spatial plugin on iPhone 17 Pro Max and iPad Pro M4): the plugin worked for a few frames, then the output reverted to native rendering because the camera target seemed to be cleared by HDRP. The author notes MetalFX does not appear to work in the Unity Editor [S58]. That is HDRP, not URP.
- Lesson: a MetalFX route is a native-plugin project that must own the camera target hand-off. It is not a setting.

### 8.4 Newer Unity versions

- The 6000.5.0b3 release notes lines that were read contain STP-on-Metal fixes and the Apple Adaptive Performance provider but no MetalFX line. Only the first 100,000 of 443,061 characters were read, so absence there is not proof [S62]. MetalFX in 6.4 or 6.5: unverified.

### 8.5 What to offer in the menu (6.3)

- "Resolution scale" slider (presets 100, 85, 75, 67) with Bilinear.
- Optional "Sharpening" toggle that switches the filter to FSR 1.0 at scales below 1.0.
- STP only as a tested, experimental choice, because it forces MSAA off and needs TAA to cooperate with the glass ray-tracing feature (unverified).
- No MetalFX entry.

## Q9. Thermal state and power state

### 9.1 What Unity 6.3.x provides

- Low Power Mode (iOS and iPadOS): `UnityEngine.iOS.Device.lowPowerModeEnabled`. Unity's page: "Indicates whether Low Power Mode is enabled on the device"; iOS then reduces CPU and GPU performance and screen brightness, and the app can reduce activity or quality [S64]. The trampoline implements it as `[NSProcessInfo processInfo].lowPowerModeEnabled` (`TRAMP/Unity/DeviceSettings.mm:94-97`, declared at `UnityInterface.h:360`), and `UnityEngine.CoreModule.dll` of the iOS player contains `get_lowPowerModeEnabled` [S63]. No change event is exposed in the material; poll it (for example on app resume and every few seconds). A change notification would need a native hook: unverified.
- Thermal state: **no Unity API in CoreModule.** A binary search of the iOS `UnityEngine.CoreModule.dll` found `lowPowerModeEnabled` but no `ThermalState`; `ThermalState` exists only in `UnityEngine.dll` and `UnityEngine.AdaptivePerformanceModule.dll` [S63].

### 9.2 Adaptive Performance in 6.3 versus later

| Item | Finding | Source |
|---|---|---|
| What it is | Tracks thermal and power state, CPU/GPU utilization, bottlenecks; adjusts quality through "scalers"; built into the Editor (enabling it adds the built-in module and reloads the domain) | [S60] |
| Built-in providers in 6.3 | Basic and Simulator. Basic uses `FrameTimingManager`; runs on Android, iOS, Linux, macOS, tvOS, visionOS, UWP; **does not support thermal** (`IThermalStatus`) | [S60] |
| Android provider in 6.3 | `com.unity.adaptiveperformance.google.android` is in the 6000.3 core package list | [S60] |
| Apple provider in 6.3 | **Not in the 6000.3 core package list** (14 packages listed, no `adaptiveperformance.apple`); its manual page returns 404 for 6000.3 (both docs hosts) and for 6000.4 (docs.unity3d.com) | [S60, S61] |
| Apple provider elsewhere | Package v6.0.0, changelog "[6.0.0] - 2025-11-18, first release"; "integrates with the ThermalState Apple API"; thermal state and performance mode; iOS, iPadOS, tvOS, visionOS (**no macOS**); a core package whose version follows the Editor. The 6000.5 manual lists it as a core package | [S59, S61] |
| 6000.5.0b3 notes | "The new Apple provider for Adaptive Performance enables Thermal Warnings for iOS, tvOS and visionOS"; "Activated Adaptive Performance Basic Provider for additional platforms"; "iOS: Added a system that adjusts the frame rate based on the thermal state" | [S62] |
| URP hook | `UniversalRenderPipeline.ApplyAdaptivePerformance` (`:716-717, 875-876, 1091-1092, 2474, 2513`) scales main-light shadow map size and cascade count (`:2523, 2527`). It is compiled only with `ENABLE_ADAPTIVE_PERFORMANCE`, which needs the AP module in the manifest; this project's compiled URP assembly has no such code | [S3, S33] |
| Scalers seen in the editor DLL | Adaptive Batching, Adaptive Decal, Auto Game Mode (changes target FPS from device GameMode), Adaptive LUT, Adaptive Shadow Cascade, Adaptive Shadowmap Resolution, Adaptive Sorting; the URP ones say "Requires Universal Render Pipeline". Only strings matching a filter were printed, so the list may be incomplete | [S63] |

So on Unity 6000.3.10f1 an Apple thermal provider is not documented or bundled. Whether the 6.0.0 Apple package can be installed by hand in a 6000.3 project is unverified (no minimum Editor version was readable). The asset flag `m_UseAdaptivePerformance: 1` is inert today (Current project state).

### 9.3 Native plugin route for thermal state (iOS, iPadOS; also macOS if ever needed)

- Apple: `ProcessInfo.thermalState` is a read-only property of enum type `ProcessInfo.ThermalState` (Objective-C `NSProcessInfoThermalState`), available on iOS and iPadOS 11.0+, macOS 10.10.3+, tvOS 11.0+, visionOS 1.0+. When it is higher the app should reduce its use of system resources. The page does not list the cases or a notification name [S68].
- The pattern is the same as Unity's own `UnityGetLowPowerModeEnabled` export: a small C function in a `.mm` file inside `Assets/Plugins/iOS/` that returns `(int)[NSProcessInfo processInfo].thermalState`, called from C# with `[DllImport("__Internal")]`. The enum case names and values (nominal, fair, serious, critical) are not in any page that was opened: unverified here; read them from Apple's `ThermalState` page before writing the plugin.
- Poll it a few times per minute. A change notification would also need native code: unverified.
- Hard rule for the project: the plugin must compile only for iOS (`#if UNITY_IOS && !UNITY_EDITOR`); desktop gets a stub that returns "nominal".

### 9.4 Android equivalents

- Adaptive Performance Android provider (`com.unity.adaptiveperformance.google.android` 6.0.0): Unity's page says it sends device data (thermal state, performance mode) to Adaptive Performance, integrates ADPF thermal APIs into `IThermalStatus`, covers the PerformanceHintManager APIs (hint sessions, reported work duration, preferred update rate), and covers Android GameMode and GameState APIs [S69]. It is the only provider listed in the 6.3 core packages besides Basic.
- Android is not a target of this project. Keep the same app-side interface (`DeviceHealth.Thermal`, `DeviceHealth.LowPower`) so an Android provider could be plugged in later.

## Q10. Measuring GPU frame time on Metal

### 10.1 FrameTimingManager

- Platform support (Unity 6.3 manual): fully supported on iOS (Metal), macOS (Metal) and tvOS (Metal); Linux with OpenGL and WebGL have no GPU time. On Apple's tile-based GPUs reported GPU time can exceed total frame time under heavy load, because gaps between rendering phases are counted [S65].
- Delay and accuracy: results arrive with a fixed four-frame delay (GPU results with a three-frame delay). Results are not guaranteed accurate. If GPU data is unavailable, GPU timings are reported as zero [S65].
- When it is active: "Frame timing is always active in development builds and the Unity Editor, but for release builds, you must enable Frame Timing Stats" (Player Settings > Other Settings > Rendering) [S60]. The enable page says the same: always active in the Editor and development builds. Attaching a `ProfilerRecorder` to a counter enables it automatically, including in release builds, and measurements happen only while a recorder is attached [S65]. Project: `enableFrameTimingStats: 0` (`ProjectSettings.asset:156`).
- API (class page): `CaptureFrameTimings`, `GetLatestTimings`, `IsFeatureEnabled`, `GetCpuTimerFrequency`, `GetGpuTimerFrequency` (always 1,000,000,000), `GetVSyncsPerSecond` [S65].
- `ProfilerRecorder` counters (units nanoseconds): `CPU Total Frame Time` (`cpuFrameTime`), `CPU Main Thread Frame Time`, `CPU Render Thread Frame Time`, `GPU Frame Time` (`gpuFrameTime`); `cpuMainThreadPresentWaitTime` has no counter. The counter-reference page does not name a `ProfilerCategory`; the usage page's example uses `ProfilerCategory.Internal` with "CPU Main Thread Frame Time" [S65]. So `ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GPU Frame Time")` is the natural call but is an inference from that example.
- CPU vs GPU bound (Unity's 2022 blog, still the clearest rule): GPU time near the frame time with low CPU times means GPU-bound; present-wait large with nothing near the frame time means limited by vsync or the frame cap; GPU time zero means the bottleneck cannot be determined [S66].

### 10.2 Editor on macOS Metal, and batch mode

- Whether `gpuFrameTime` is **non-zero in the Editor on macOS Metal**: not confirmed. The pages say the manager is always active in the Editor and list macOS (Metal) as supported, but none says the Editor Game view returns GPU time, and the class page notes GPU time becomes zero when the platform cannot provide it. Treat as unverified. A 5-minute check is cheap: log `gpuFrameTime` for a few seconds in Play mode.
- Batch mode: the manual pages read do not mention batch mode or `-nographics` [S65]. `-nographics` means Unity runs without a graphics device, so no GPU time can exist there (general Unity meaning, not in the pages read). Batch mode with a real Metal device: unverified.
- Editor timing in general is a poor proxy for iOS. A 6.5 beta known issue lists "Metal: macOS stutters in a minimal project in the Editor in Play Mode" [S62]. Use a development player on the target device for GPU numbers.

### 10.3 Xcode Metal Performance HUD

From Apple's article "Monitoring your Metal app's graphics performance" (JSON edition) [S67]:

- Xcode scheme: Edit Scheme > Run > Diagnostics > "Show Graphics Overview" (and "Log Graphics Overview" for logging; both must be on to log).
- Environment variables: `MTL_HUD_ENABLED=1`; `MTL_HUD_LOG_ENABLED=1` (needs the first); `MTL_HUD_LOG_SHADER_ENABLED=1` (shader compile activity); `MTL_HUD_ENCODER_TIMING_ENABLED=1` (per-encoder GPU time); `MTL_HUD_SHOW_VALUE_RANGE=1`.
- Devices (iOS, iPadOS, tvOS): Settings > Developer > Graphics HUD > Show Graphics HUD (and Log Graphics Performance). The device needs a development provisioning profile; the HUD only appears for apps you build and install yourself on development devices.
- Programmatic: `MetalHudEnabled` in Info.plist, or `MetalHUDForceEnabled=1` in the app's UserDefaults.
- What it shows: Metal device, resolution, present mode, memory (app and Metal), Game Mode on/off; average FPS, GPU time and frame interval with a 120-frame chart; optional encoder timing (vertex, fragment, compute) with a three-frame GPU timeline. With logging, a `metal-HUD:` line is written once per second with memory, present interval and GPU time per frame. Encoder timing may add HUD CPU cost and is unavailable if the app uses a Metal counter sample buffer.
- How to pass `MTL_HUD_ENABLED=1` to a double-clicked Mac player is not in the page; launching the player binary from a shell with the variable set is the obvious route (not verified here).
- The article's macOS version requirement was not visible in the extract: unverified.

### 10.4 Instruments

Not researched: no Instruments page was opened. Unverified. Add a follow-up task (Metal System Trace template, GPU counters) if the HUD and `FrameTimingManager` are not enough.

### 10.5 Suggested measurement setup for the settings work

1. Development player on a real iPhone and iPad, Xcode Metal HUD on (device Developer setting), plus a `ProfilerRecorder` for `GPU Frame Time` shown in the debug overlay.
2. Same scene, same camera path, one preset at a time, 30 seconds each; log average and 95th percentile GPU time and the achieved frame interval.
3. Add `Resolution.refreshRateRatio` and `iOS.Device.lowPowerModeEnabled` to the log so rate caps are visible.

## TABLE: candidate setting, API, runtime-switchable, hitch risk, build implications, notes

Legend. Switchable: **yes** = a public API changes it live; **separate asset** = needs a different URP asset or renderer (pipeline recreation or `SetRenderer`); **restart** = a build-time Player Setting. Hitch risk is a judgement from the code read; none of these was timed on a device. "Q#" points to the section above.

| # | Candidate setting | API | Runtime-switchable? | Hitch risk | Variant / build implications | Notes |
|---|---|---|---|---|---|---|
| 1 | Render scale | `asset.renderScale` (0.1 to 3.0) | yes | medium: target reallocation, old targets live about 10 frames | none | Values within 0.05 of 1.0 are ignored (Q1.3). UI stays at native size (Q7.3). Apply on slider release, not per frame |
| 2 | Upscaling: Bilinear / Auto | `asset.upscalingFilter` | yes | low | none | Auto picks Point only for integer ratios; Point is useless with post-processing on (Q1.5) |
| 3 | Upscaling: FSR 1.0 and sharpness | `upscalingFilter = FSR`, `fsrSharpness`, `fsrOverrideSharpness` | yes | low to medium (extra pass) | none seen; falls back to Auto without SM 4.5 | Active even at scale 1.0. Not device-tested on Metal |
| 4 | Upscaling: STP | `upscalingFilter = STP` | yes, but only with MSAA 1, no camera stack, no dynamic resolution | medium to high (history buffers, AA mode change) | none seen | Forces TAA. Glass RT feature compatibility unverified (Q1.5, Q8) |
| 5 | MSAA 1 / 2 / 4 / 8 | `asset.msaaSampleCount` (also set `QualitySettings.antiAliasing`); per camera `Camera.allowMSAA` | yes | medium to high: target format and size change; Unity calls it the expensive change | none read | Pipeline constructor syncs `QualitySettings.antiAliasing` only at creation (Q1.2) |
| 6 | HDR on / off | `camera.allowHDR` (project toggle exists), `asset.supportsHDR` | yes | medium (colour target format) | HDR display-output variants are prefiltered away (`m_PrefilterHDROutput: 1`); the HDR toggle here is the colour buffer, not display output | `isHdrEnabled = allowHDR && supportsHDR` (`UniversalRenderPipeline.cs:1488`) |
| 7 | HDR buffer precision 32 / 64 | `asset.hdrColorBufferPrecision` | yes | medium (format change) | none | 64-bit costs more bandwidth [S45] |
| 8 | Shadow distance | `asset.shadowDistance` | yes | low (culling only) | none | Read as `min(distance, farClip)` (`UniversalRenderPipeline.cs:1592`) |
| 9 | Shadow cascades 1 to 4 | `asset.shadowCascadeCount` (throws outside range) | yes | low to medium | cascade variants kept (`m_PrefilteringModeMainLightShadows: 3` = main light + cascades, OFF removed) | Project uses 2 |
| 10 | Main light shadow resolution | `asset.mainLightShadowmapResolution` | yes | medium (shadow target realloc) | none | 2048 today. Max 2048 x 2048 x 16 bit = 8 MiB (layout for 2 cascades not read) |
| 11 | Main light shadows off | per camera `renderShadows = false`; per light `Light.shadows`; asset `supportsMainLightShadows` is internal | yes (camera or light) | low | OFF variant removed by prefilter, but URP draws an empty 1x1 shadow map and keeps the keyword on when `stripShadowsOffVariants` is true (`MainLightShadowCasterPass.cs:224-236`; renderer data default true, `UniversalRendererData.cs:376`) | Designed to be safe, but the keyword on the empty path follows `shadowsEnabled` (`MainLightShadowCasterPass.cs:234`); confirm in a build |
| 12 | Additional light shadows on / off | asset `supportsAdditionalLightShadows` is internal; per light `Light.shadows` | per light: yes; asset-wide: separate asset | low per light | prefilter is `SelectOnly` (OFF removed, `m_PrefilteringModeAdditionalLightShadows: 2`); same empty-map path as row 11 (`AdditionalLightsShadowCasterPass.cs:681-687`) | A tier asset with it off keeps both variants in the build (union) |
| 13 | Additional light shadow atlas size | `asset.additionalLightsShadowmapResolution` | yes | medium to high (big target) | none | 4096 x 4096 x 16 bit = 32 MiB (arithmetic from `shadowmapDepthBufferBits = 16`, `UniversalRenderPipeline.cs:1762`). Prime memory saving on mobile |
| 14 | Soft shadows hard / soft | per light `Light.shadows`; asset `supportsSoftShadows` is internal | per light: yes | low | `_SHADOWS_SOFT` on/off both kept (`m_PrefilterSoftShadows: 0`) | `MainLightShadowCasterPass.cs:364` |
| 15 | Soft shadow quality Low / Medium / High | per light `UniversalAdditionalLightData.softShadowQuality`; asset one is internal | per light: yes; asset default: reflection | low | no quality keywords on Mac/iOS (per-light shader parameter); quality keywords prefiltered away | Q1.4. Low = 4 taps; Medium = 5x5; High = 7x7 [S45] |
| 16 | SSAO on / off | `ScriptableRendererFeature.SetActive`, or `SetRenderer(index)` to a renderer without it | API yes; **unsafe with the current build** (OFF variant stripped) → use a second renderer | medium for renderer switch (unmeasured) | with a no-SSAO renderer in the asset, OFF variants are kept (Q2.4) | Q2 |
| 17 | SSAO downsample | internal field; reflection | reflection only | low | no keyword (`settings.Downsample` is a number) | Biggest SSAO saving per Unity [S48] |
| 18 | SSAO samples / source / method | internal fields | separate asset or rebuild | n/a | Low/Medium samples, Depth source and Interleaved method variants are stripped in this build | Q2.3 |
| 19 | LOD cross-fade | asset `enableLODCrossFade` is read-only | separate asset | high (pipeline recreation) | cross-fade variants follow the included assets | Pipeline copies it to `QualitySettings.enableLODCrossFade` at creation |
| 20 | Depth / opaque texture | `asset.supportsCameraDepthTexture`, `supportsCameraOpaqueTexture`; per camera `requiresDepthOption` | yes | medium (extra copies) | none | Leave alone unless a feature needs it |
| 21 | Bloom: HQ filtering, filter mode, downscale, iterations | Volume override fields `highQualityFiltering`, `filter`, `downscale`, `maxIterations` | yes | low (first use of a variant may hitch: unverified) | all post variants kept (`m_StripUnusedPostProcessingVariants: 0`) | Use a second higher-priority Volume (Q4.1) |
| 22 | Film grain, chromatic aberration, lens distortion | Volume overrides | yes | low | same | No Unity mobile cost ranking was readable (Q4.3). Measure |
| 23 | Colour grading LUT size / mode | `asset.colorGradingLutSize`, `colorGradingMode` | yes | low to medium | none | Not measured |
| 24 | Texture quality | `QualitySettings.globalTextureMipmapLimit` | yes, no reload | medium to high: immediate re-upload of loaded textures | `mipStripping: 0`, all mips ship | `FrontRooms3DGame.cs:502` resets it to 0 at start |
| 25 | LOD bias | `QualitySettings.lodBias` | yes | low | none | LOD Groups only |
| 26 | Anisotropic filtering | `QualitySettings.anisotropicFiltering` | property exists; runtime behaviour unverified | unknown | none | Page is sparse (Q5.3) |
| 27 | Frame cap on iOS / iPadOS | `Application.targetFrameRate` | yes | low | none | `vSyncCount` ignored. 40 and 120 need ProMotion support on (Q6) |
| 28 | ProMotion support | Player Setting `appleEnableProMotion` (Info.plist key) | **restart** (build setting) | n/a | none | Battery cost [S36]. Currently 0 |
| 29 | Frame cap on Mac / Windows | `QualitySettings.vSyncCount` (ignored target when > 0) | yes | low | none | Leave desktop alone (Q6.5) |
| 30 | Fixed DPI resolution scaling | Player Setting + `QualitySettings.resolutionScalingFixedDPIFactor` | probably **restart**; live change unverified | unknown | none | Shrinks UI too. Not recommended (Q7.1) |
| 31 | Dynamic resolution | `Camera.allowDynamicResolution`, `ScalableBufferManager.ResizeBuffers` | yes | low per step (no reallocation) | none | Not with STP/TAA. Custom passes unverified (Q7.4) |
| 32 | Whole tier swap | `QualitySettings.SetQualityLevel(i, true)` with a different URP asset per level | yes (but heavy) | **high**: pipeline destroyed and recreated | variants kept if any included asset needs them; keep Forward+ in every tier; mind prefilter combination (Q3.2) | Do behind a cover. Same-asset switch behaviour unverified |
| 33 | Renderer swap (e.g. no SSAO, no glass RT) | `UniversalAdditionalCameraData.SetRenderer(i)` | yes | medium (unmeasured) | keeps SSAO-OFF variants because not every renderer has SSAO | Best way to drop a renderer feature without recreating the pipeline |
| 34 | Glass RT feature (macOS only) | `ScriptableRendererFeature.SetActive` on the custom feature | yes (desktop only) | unknown | none read | Not for iOS; class not read |
| 35 | Low Power Mode | `UnityEngine.iOS.Device.lowPowerModeEnabled` | read only | none | none | Poll. Apple drops fast refresh rates in Low Power Mode [S38] |
| 36 | Thermal state | native plugin over `ProcessInfo.thermalState`; AP Apple provider only in later Unity | read only | none | none | No Unity 6.3 API (Q9) |
| 37 | GPU frame time | `FrameTimingManager`, `ProfilerRecorder` "GPU Frame Time"; Xcode Metal HUD | read only | none | needs Frame Timing Stats in release builds | Editor/batch GPU time unverified (Q10) |

## Recommended architecture for presets

### Decision

Use a **hybrid**: one URP asset per mobile tier for the choices that have no public setter or that change variants, plus runtime setters, Volume overrides, per-light and per-camera calls for the fine options. Do not make one shared asset carry every option by mutation. Setters are used only for the fine options and only on the mobile tier assets. The existing desktop asset is never touched by new code on Mac and Windows.

Why not "one asset, mutate at runtime":

- Six options a menu wants have no public setter (main/additional shadows on-off, soft shadow quality, LOD cross-fade, rendering mode, reflection probe blending) (Q1.1).
- `GraphicsSettings.currentRenderPipeline` is the shared asset object. Setters change it for every user of it (Q3.3).
- Turning SSAO off in a build made from the current setup asks for variants that were stripped (Q2.3).

Why not "asset swap for everything":

- Each swap rebuilds the pipeline and is a loading-screen event (Q3.1). A slider cannot sit on that.
- Tier count multiplies with every custom option; custom combinations cannot all be assets.

### Layers

1. **Desktop reference tier (Mac, Windows).** The current `FrontRooms_URP.asset` and `FrontRooms_URP_Renderer.asset`, exactly as they are. The desktop "Reference" preset applies **zero writes**. Existing desktop code (`targetFrameRate = 60`, `globalTextureMipmapLimit = 0`, HDR toggle) stays and is not edited. Any new graphics code is guarded by `#if UNITY_IOS` or `Application.platform` checks. This is the byte-identical rule.
2. **Mobile tier assets (iOS, iPadOS only).** Three assets cloned from the desktop asset, all Forward+ (different rendering paths double variants [S70]). Names are placeholders:
   - `Mobile_High`: close to desktop, MSAA 4 or 2, additional-light shadows kept, SSAO renderer.
   - `Mobile_Balanced`: smaller shadow maps and distance, 1 to 2 cascades, no additional-light shadows, soft shadows Low, LOD cross-fade off, no-SSAO renderer.
   - `Mobile_Battery`: as Balanced, MSAA off, HDR precision 32, smaller LUT.
   Each tier asset lists two renderers: `Renderer_Full` (SSAO on) and `Renderer_Lite` (no SSAO, no glass RT). Pick by `SetRenderer`. This keeps the SSAO-OFF variants in the build (Q2.4).
   Assign tier assets to new quality levels with `excludedTargetPlatforms` set so they are not in Mac/Windows builds, and set the old desktop levels to exclude iOS (Q3.2). Check that WebGL keeps its own level (`m_PerPlatformDefaultQuality` has `WebGL: 3`); WebGL is a separate track.
3. **Preset record.** A preset is a name plus a table of option values and a tier id. Applying a preset runs these steps in order, behind a cover if step 1 or the mip limit changes:
   1. `SetQualityLevel(tierLevel, true)` only when the tier changes.
   2. Renderer index on every camera (`SetRenderer`).
   3. Asset setters that have public setters (render scale, upscaling filter, shadow distance/resolution/cascades, LUT size).
   4. Quality-level values (`globalTextureMipmapLimit`, `lodBias`, `anisotropicFiltering`).
   5. Volume override layer: a **second global Volume** with higher priority than the film volume and zone volumes (`enabled = false` on desktop and in the "Reference" preset). It carries bloom HQ/filter/downscale/iterations, grain, chromatic aberration, lens distortion (Q4.1).
   6. Per-light calls: soft shadow quality, `Light.shadows`.
   7. Frame cap (iOS only).
4. **Custom options override a preset.** Store `presetId` plus a sparse dictionary of overrides (only options the player changed). On start and on any change: apply the preset, then apply the overrides on top. Show "Custom" when the dictionary is not empty. Picking a preset clears the overrides. Options that are structural (baked into a tier asset: LOD cross-fade, additional-light shadow on/off, asset-wide soft shadow quality) are shown read-only in a custom view, or are preset-only. Offer as custom options only those that are live: resolution scale, upscaling filter, MSAA (with an Apply button), shadow distance, shadow resolution, cascades, soft-shadow quality (per light), SSAO (renderer index), post quality, texture quality, LOD bias, frame cap.
5. **Persistence and safety.** Save `presetId` and overrides in the player's own preferences, not in the assets. Re-apply from the saved record on every start so a changed asset never sticks. In the Editor, restore changed assets on exit (Q3.3).
6. **Device health (iOS/iPadOS).** A small service reads `Device.lowPowerModeEnabled` (poll) and thermal state (native plugin, Q9.3). It may only suggest or step down cheap knobs with hysteresis: frame cap first, then render scale one notch. It never swaps tiers. The Android provider is out of scope.
7. **Frame rate.** iOS/iPadOS options 30, 60, and, if `appleEnableProMotion` is turned on, 40 and 120. Read the real rate and show it in the debug overlay. `appleEnableProMotion` is a build setting and a battery trade-off (Q6.2): a decision for the project owner.
8. **Upscaling.** Offer Bilinear and FSR 1.0. Keep STP off the menu until it is tested with the glass RT feature and with MSAA off. No MetalFX (Q8).

### Keeping the desktop byte-identical

- Mac/Windows quality levels continue to reference the unchanged desktop asset. New tier assets are excluded from desktop platforms.
- No new setter or `SetQualityLevel` call runs on desktop unless the user changes a desktop option. The "Reference" preset has no writes.
- The second quality Volume stays disabled on desktop (`VolumeManager` skips disabled volumes, Q4.1).
- Build side: the build rewrites the `m_Prefilter*` fields of every included asset (Q3.2). Variants can only be added by tier assets (union rule), never removed from the desktop build. The desktop asset file may change on disk after an iOS build if it is included there; exclude it from iOS levels and review the diff of `FrontRooms_URP.asset` after each platform build. Inference from `ShaderBuildPreprocessor.cs:477-500`, not tested.
- Proof step before shipping: a pixel diff of the same desktop camera path built before and after the change.

### Tests to run before relying on this (all unverified above)

1. Does `SetQualityLevel` between two levels that share one asset recreate the pipeline? (Q3.1)
2. A two-asset build: stripping report, check that the High tier keeps every variant it had, and that prefilter rules from different assets do not remove each other's keywords. (Q3.2)
3. `SetRenderer` switch between a SSAO and a no-SSAO renderer on an iPhone: hitch size, memory, correct lit shading (no missing variants). (Q2.4)
4. 40 fps on a 120 Hz iPhone and iPad with ProMotion on; behaviour in Low Power Mode. (Q6.3)
5. STP with MSAA 1 and the glass RT feature. (Q1.5)
6. Reflection on `softShadowQuality` and `m_*` fields in an IL2CPP iOS build, if the asset-wide soft shadow quality is wanted. (Q1.1)
7. GPU frame time values in the Editor on macOS Metal and in batch mode. (Q10.2)
8. Dynamic resolution with the project's custom passes. (Q7.4)
9. Apple Instruments GPU capture (not researched). (Q10.4)

## SOURCES

Abbreviations: `PROJ` = `/Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01 Individual Game Project/Frontrooms3D`. `URP/` = `PROJ/Library/PackageCache/com.unity.render-pipelines.universal@37e0d4fc2503`. `CORE/` = `PROJ/Library/PackageCache/com.unity.render-pipelines.core@04ab0eefa0c3`. `TRAMP/` = `/Applications/Unity/Hub/Editor/6000.3.10f1/PlaybackEngines/iOSSupport/Trampoline/Classes`. "Accessed": all 2026-10-24; "agent" = read by the original agent 07:27 to 07:49 PDT; "write-up" = read again or newly opened during the write-up.

| # | URL or file path | What it shows | Accessed |
|---|---|---|---|
| S1 | `PROJ/Packages/manifest.json`; `URP/package.json` | URP 17.3.0 for Unity 6000.3; no Adaptive Performance module in the manifest | agent |
| S2 | `URP/Runtime/Data/UniversalRenderPipelineAsset.cs` | Property setters and their visibility; upscaling enum (`:342-377`); `CreatePipeline` (`:811-835`); renderer list (`:475, 687, 922`); soft shadow enum | agent, write-up |
| S3 | `URP/Runtime/UniversalRenderPipeline.cs` | Constructor (`:250-305`); camera data init (`:1367-1412, 1442, 1488, 1495-1565, 1592`); shadow data (`:1762-1768`); filter resolution (`:2246-2312`); dynamic resolution hooks (`:480-485`); UI ownership (`:2594-2609`); Adaptive Performance hooks; `Dispose` (`:331-370`) | agent, write-up |
| S4 | `URP/Runtime/FrameData/UniversalCameraData.cs` | `rendersOverlayUI` (`:395`); TAA and STP enable conditions (`:497-533`) | agent, write-up |
| S5 | `CORE/Runtime/STP/STP.cs` | `STP.IsSupported()` (`:19-33`): compute shaders and not GLES3 | agent |
| S6 | `URP/Runtime/RendererFeatures/ScreenSpaceAmbientOcclusion.cs` | Public feature class; internal settings; keyword constants; resource error text | agent, write-up |
| S7 | `URP/Runtime/ScriptableRendererFeature.cs`, `URP/Runtime/ScriptableRenderer.cs` | `SetActive` and `isActive` (`:14-18, 123-126`); active checks (`:1637, 1657, 1700`); AO parameter reset (`:1731`) | agent, write-up |
| S8 | `URP/Runtime/ScriptableRendererData.cs`, `URP/Runtime/UniversalRendererData.cs`, `URP/Runtime/UniversalRenderer.cs` | Public `rendererFeatures`; `stripShadowsOffVariants` default true; dynamic resolution note (`UniversalRenderer.cs:2050-2066`) | write-up |
| S9 | `URP/Runtime/Passes/ScreenSpaceAmbientOcclusionPass.cs`, `URP/ShaderLibrary/AmbientOcclusion.hlsl`, `URP/Shaders/Lit.shader` | SSAO keyword set and reset; shader use; `multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION` | agent, write-up |
| S10 | `URP/Editor/ShaderBuildPreprocessor.cs` | Asset gathering, SSAO stripping, volume feature stripping, per-asset prefilter update and save | agent, write-up |
| S11 | `URP/Editor/ShaderScriptableStripper.cs` | A variant is kept unless every feature set strips it (`:1251-1277`); SSAO stripper (`:654-669`) | write-up |
| S12 | `CORE/Editor/BuildTargetExtensions.cs` | Which render pipeline assets enter a build (`:85-109`) | agent, write-up |
| S13 | `URP/Runtime/Data/UniversalRenderPipelineAssetPrefiltering.cs` | `PrefilteringMode` enums (`:12-26`); SSAO sample count filters | agent, write-up |
| S14 | `PROJ/Assets/Settings/FrontRooms_URP.asset` | Current URP asset values and prefilter fields | agent, write-up |
| S15 | `PROJ/Assets/Settings/FrontRooms_URP_Renderer.asset` | Renderer features and SSAO settings | agent |
| S16 | `PROJ/Assets/UniversalRenderPipelineGlobalSettings.asset` | Strip settings, additional-asset inclusion flags | agent, write-up |
| S17 | `PROJ/ProjectSettings/QualitySettings.asset` | Six quality levels, per-platform defaults | agent, write-up |
| S18 | `PROJ/ProjectSettings/ProjectSettings.asset` | ProMotion, resolution scaling, frame timing, mip stripping, strict variant matching | agent, write-up |
| S19 | `PROJ/Assets/Editor/Rendering/FrontRoomsRenderSetup.cs` | One asset assigned to all levels (`:49-55`) | agent |
| S20 | `PROJ/Assets/Scripts/FrontRooms3DGame.cs`, `FrontRooms3DGame.Baseline.cs`, `Rendering/FrontRoomsPostStack.cs` | Runtime graphics calls and the film volume set-up | agent, write-up |
| S21 | `URP/Runtime/Passes/MainLightShadowCasterPass.cs`, `AdditionalLightsShadowCasterPass.cs`, `URP/Runtime/ShadowUtils.cs`, `URP/ShaderLibrary/Shadows.hlsl` | Empty shadow map path; soft shadow keywords and per-light quality; shadow target reallocation | agent, write-up |
| S22 | `URP/Runtime/UniversalAdditionalLightData.cs` | Per-light `softShadowQuality` public setter (`:161-165`) | agent |
| S23 | `URP/Runtime/Overrides/Bloom.cs` | Bloom parameters and enums | agent, write-up |
| S24 | `CORE/Runtime/Volume/Volume.cs`, `VolumeManager.cs`, `VolumeComponent.cs` | Profile instancing; priority, weight; skip rules; override state | agent, write-up |
| S25 | `URP/Runtime/RenderingUtils.cs`, `CORE/Runtime/RenderGraph/RenderGraphResourcePool.cs` | Target reallocation and 10-frame stale lifetime | agent |
| S26 | `URP/Runtime/Passes/DrawScreenSpaceUIPass.cs`, `URP/Runtime/UniversalRendererRenderGraph.cs` | Overlay UI drawn by URP after the final blit; uGUI and UI Toolkit lists | agent, write-up |
| S27 | `URP/Runtime/UniversalAdditionalCameraData.cs` | Per-camera setters, `SetRenderer` (`:688-691`) | agent, write-up |
| S28 | `TRAMP/UnityAppController+Rendering.mm`, `UnityAppController.mm` | Frame rate callback (`:209-242`); Metal display link disabled (`UnityAppController.mm:128-144`); scale factor use | agent, write-up |
| S29 | `TRAMP/Unity/DisplayManager.mm` | Scale factor from `nativeScale` (`:752-776`); surface sizing and rendering-resolution request (`:192-232, 317-342, 712-727`) | agent, write-up |
| S30 | `TRAMP/UI/UnityView.mm`, `TRAMP/Unity/MetalHelper.mm` | Drawable size; interim colour target when sizes differ | agent |
| S31 | `TRAMP/Unity/DeviceSettings.mm`, `TRAMP/Unity/UnityInterface.h` | Low Power Mode export (`:94-97`) | agent, write-up |
| S32 | `/Applications/Unity/Hub/Editor/6000.3.10f1/PlaybackEngines/iOSSupport/iOSPlayerBuildProgram.exe` (string search) | Info.plist key names `CADisableMinimumFrameDuration...` and `EnableProMotion` present | agent |
| S33 | `PROJ/Library/ScriptAssemblies/Unity.RenderPipelines.*.Runtime.dll`; the two `*.asmdef` files; recursive search of `URP/` and `CORE/` | `IUpscaler`/`UpscalerRegistry`/Adaptive Performance code absent from compiled assemblies; define not in asmdefs; no "metalfx" anywhere | agent, write-up |
| S34 | https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-targetFrameRate.html | Default, rounding rule, iOS ProMotion statements, desktop vSync rule | agent |
| S35 | https://docs.unity3d.com/6000.3/Documentation/ScriptReference/QualitySettings-vSyncCount.html | vSyncCount ignored on Android and iOS; desktop rule; refresh rate can change | agent |
| S36 | https://docs.unity3d.com/6000.3/Documentation/Manual/class-PlayerSettingsiOS.html | ProMotion, resolution scaling mode, Target DPI, frame timing stats | agent |
| S37 | https://developer.apple.com/tutorials/data/documentation/bundleresources/information-property-list/cadisableminimumframedurationonphone.json | `CADisableMinimumFrameDurationOnPhone` key | agent |
| S38 | https://developer.apple.com/tutorials/data/documentation/quartzcore/optimizing-iphone-and-ipad-apps-to-support-promotion-displays.json | ProMotion rate lists; frame rate range hints; Low Power Mode and heat | agent |
| S39 | https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rendering.OnDemandRendering-renderFrameInterval.html | `renderFrameInterval` | agent |
| S40 | https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Screen.SetResolution.html | Takes effect at end of frame; iOS not covered | agent |
| S41 | https://docs.unity3d.com/6000.3/Documentation/ScriptReference/QualitySettings.SetQualityLevel.html | `applyExpensiveChanges`; stripped levels | agent |
| S42 | https://docs.unity3d.com/6000.3/Documentation/ScriptReference/QualitySettings-globalTextureMipmapLimit.html | Live change, immediate re-upload, scope | agent |
| S43 | https://docs.unity3d.com/6000.3/Documentation/Manual/class-QualitySettings.html | Which Quality settings are Built-in only | agent |
| S44 | https://docs.unity3d.com/6000.3/Documentation/ScriptReference/QualitySettings-lodBias.html; `.../QualitySettings-maximumLODLevel.html`; `.../QualitySettings-anisotropicFiltering.html`; `.../QualitySettings-resolutionScalingFixedDPIFactor.html` | LOD bias, max LOD level, aniso (sparse), fixed DPI factor | agent |
| S45 | https://docs.unity3d.com/6000.3/Documentation/Manual/urp/universalrp-asset.html | URP asset settings; render scale and UI; filters; soft shadow levels; HDR precision | agent |
| S46 | https://docs.unity3d.com/6000.3/Documentation/Manual/urp/configure-for-better-performance.html | URP performance checklist | agent |
| S47 | https://docs.unity.com/en-us/engine/6000.3/manual/post-processing-and-full-screen-effects/urp/effect-list/bloom | Bloom quality advice | agent |
| S48 | https://docs.unity3d.com/6000.3/Documentation/Manual/urp/ssao-renderer-feature-reference.html; `.../urp/post-processing-ssao.html` | SSAO settings and cost ratings; no Volume dependence | agent |
| S49 | https://docs.unity3d.com/6000.3/Documentation/Manual/urp/quality/quality-settings-through-code.html | Assign asset per level; switch in code; significant temporary cost | agent |
| S50 | https://docs.unity3d.com/6000.3/Documentation/Manual/urp/quality/change-urp-asset-settings.html | Only properties with a setter; example | agent |
| S51 | https://docs.unity3d.com/6000.3/Documentation/Manual/srp-setting-render-pipeline-asset.html | Active asset selection | agent |
| S52 | https://docs.unity3d.com/es/2021.1/Manual/srp-setting-render-pipeline-asset.html | Old (2021.1) page: asset change discards the pipeline instance | agent |
| S53 | https://docs.unity3d.com/6000.3/Documentation/Manual/urp/stp/stp-upscaler.html; `.../urp/stp/stp-enable.html` | STP requirements and limits | agent |
| S54 | https://docs.unity3d.com/6000.3/Documentation/Manual/resolution-scale-introduction.html; https://docs.unity3d.com/Manual/resolution-scale-introduction.html (showed 6.6) | Upscaler table per pipeline; no Apple or MetalFX | agent |
| S55 | https://docs.unity3d.com/6000.3/Documentation/Manual/DynamicResolution-introduction.html | Dynamic resolution platforms (Metal on Apple) | agent |
| S56 | https://docs.unity3d.com/6000.3/Documentation/Manual/SL-Pragma-target.html | Metal supports targets 4.5 and 5.0 with compute | agent |
| S57 | https://developer.apple.com/forums/thread/736915 | MetalFX Spatial anecdote on iPhone 13 with a URP sample | agent |
| S58 | https://www.developer.apple.com/forums/thread/807096 | MetalFX plugin on HDRP; reverted to native | agent |
| S59 | https://docs.unity3d.com/Packages/com.unity.adaptiveperformance.apple@6.0/manual/index.html; `.../manual/requirements.html`; `.../changelog/CHANGELOG.html` | Apple provider v6.0.0: thermal state; iOS, iPadOS, tvOS, visionOS; first release 2025-11-18 | agent |
| S60 | https://docs.unity3d.com/6000.3/Documentation/Manual/adaptive-performance/introduction.html; `.../adaptive-performance/providers.html`; `.../adaptive-performance/basic-provider.html`; https://docs.unity.com/en-us/engine/6000.3/manual/unity-editor/editor-settings-reference/comp-manager-group/adaptive-performance-project-settings; https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/packages-all/pack-core | AP in 6.3: providers, Basic limits, project settings text, core package list without Apple | agent |
| S61 | https://docs.unity.com/en-us/engine/6000.5/manual/packages-list/packages-all/pack-core/com-unity-adaptiveperformance-apple; 404s for the same package page in 6000.3 (both hosts) and 6000.4 (docs.unity3d.com) | Apple provider is a core package in 6000.5, absent in 6000.3 | agent |
| S62 | https://unity.com/releases/editor/beta/6000.5.0b3 (first 100,000 of 443,061 characters read) | 6.5 beta: Apple AP provider, thermal frame rate system, Metal items | agent |
| S63 | `/Applications/Unity/Hub/Editor/6000.3.10f1/PlaybackEngines/iOSSupport/Variations/il2cpp/Managed/*.dll` and `Unity.app/Contents/Resources/Scripting/Managed/UnityEngine/UnityEditor.AdaptivePerformanceModule.dll` (binary string search) | `lowPowerModeEnabled` in CoreModule; no `ThermalState` in CoreModule; AP scaler names | agent |
| S64 | https://docs.unity3d.com/6000.3/Documentation/ScriptReference/iOS.Device-lowPowerModeEnabled.html | Low Power Mode property | agent |
| S65 | https://docs.unity3d.com/6000.3/Documentation/Manual/frame-timing-manager.html; `.../frame-timing-manager-enable.html`; `.../frame-timing-manager-get-timing-data.html`; `.../frame-timing-manager-record-timing-data.html`; `.../frame-timing-manager-counter-reference.html`; https://docs.unity3d.com/6000.3/Documentation/ScriptReference/FrameTimingManager.html | Platform support, delay, enabling, counters | agent |
| S66 | https://unity.com/blog/engine-platform/detecting-performance-bottlenecks-with-unity-frame-timing-manager | CPU vs GPU bound rules; GPU zero | agent |
| S67 | https://developer.apple.com/tutorials/data/documentation/xcode/monitoring-your-metal-apps-graphics-performance.json | Metal Performance HUD enabling and metrics | agent |
| S68 | https://developer.apple.com/tutorials/data/documentation/foundation/processinfo/thermalstate-swift.property.json | `ProcessInfo.thermalState` type and availability | write-up |
| S69 | https://docs.unity3d.com/Packages/com.unity.adaptiveperformance.google.android@6.0/manual/index.html | Android provider: ADPF thermal, performance hints, GameMode | write-up |
| S70 | https://docs.unity3d.com/6000.2/Documentation/Manual/urp/shader-stripping-features.html; `.../urp/shader-stripping.html` (Unity 6.2 pages; the 6000.3 path returned 404) | Which assets are included; strip a feature in every asset; Strip Unused Variants required | write-up |
| S71 | https://www.imore.com/apple-clears-120hz-iphone-13-pro-confusion-developers | Secondary report on the ProMotion plist key | agent |
| S72 | https://issuetracker.unity.com/api/v1.0/redirects/slug/frame-rate-stutters-in-macos-player-when-vsynccount-is-2-or-more | Title only ("Frame rate stutters in macOS Player when vSyncCount is 2 or more"); body not readable | agent |

Pages that did not load or had no useful text: `.../urp/post-processing/post-processing-mobile.html` (404); `.../urp/shader-stripping.html` on 6000.3 (404); the Apple "Optimizing ProMotion refresh rates" HTML page (no body text); the Apple article on the Metal HUD HTML page (no body text; the JSON edition S67 was used); `.../manual/shader-error.html` on 6000.3 (opened, says nothing about stripped variants).

Search results seen but never opened, therefore **not used as evidence**: other Unity version copies of the shader stripping pages; Unity pages on strict shader variant matching and shader loading (so what Unity does when a stripped variant is requested stays unverified); Apple developer forum and third-party pages on MetalFX and the Metal HUD; Unity 6.2 to 6.6 pages on Adaptive Performance Apple.
