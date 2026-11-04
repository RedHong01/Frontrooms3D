# 14 · Post-processing budget, anti-aliasing, upscaling, per-platform quality tiers

- Line: 技术美术调研 (ledger `tech_art`), agent topic 14, source prefix U.
- Stage: research draft.
- Date: 2026-11-04.
- Red's ask (via 总控, 2026-11-04): "在做无限走廊的时候，我需要你也开启一个task用来 research专业的Technical artist会怎么做，提高画质表现的同时，节省性能。"
- Done: web reading only (WebSearch / WebFetch), main read only (read-only reads of the Unity project files named below), read-only greps of the URP 17.6.0 / SRP Core 17.6.0 source (project `Library/PackageCache` and the 6000.6.3f1 editor's `BuiltInPackages`), of the compiled `Library/ScriptAssemblies` DLLs and of the installed 6000.6.3f1 player binaries. Measured numbers come from the tech_art probe run of 2026-11-04 (`tech_art/MEASURED.md`) and from earlier project documents. No Unity run by this agent, no download, nothing played.
- Not done: no GPU timing (the batch-mode probe has no GPU timers on this Mac [U-M1]), no device run, no clone change. Unity's optimisation e-books (Unity 6 editions) are gated PDFs and were not opened (no-download rule) [U38, U39]; Unity's 6000.6 manual pages that carry the same advice were read instead. Digital Foundry's own pages could not be opened; DF findings below come from secondary summaries and are marked so. Every saving below is an ESTIMATE until measured.

## 0. Short answer

1. **The post stack is already built the cheap way.** All grading (white balance, colour adjustments, lift/gamma/gain, SMH, ACES) is one 32³ LUT render per frame, and vignette, grain, chromatic aberration, lens distortion and the bloom composite run in one uber pass [U-L1, U-L3]. What remains separate is the bloom pyramid (about 16 small passes) and SSAO. On desktop there is no free cut: SSAO off changes 3.4-9.6 % of pixels, and even SSAO normals-from-depth changes 0.24-0.29 % (noise floor 0.00-0.07 %) [U-M1]. Desktop ULTRA stays as it is.
2. **The large handheld saving is the opaque prepass, not the post.** URP 17.6 adds a full depth prepass whenever SSAO runs before opaques, whatever its source [U-L3], and Apple says hidden surface removal makes a prepass useless for speed on Apple GPUs [U10]. On iPhone / iPad (no glass ray tracing there) a handheld-only renderer with SSAO Source = Depth + After Opaque would let URP drop the prepass: −26 % batches / −42 % SetPass measured in October when SSAO was the prepass's only user [U-L6], 36-44 % of draws by today's renderer inventory (ESTIMATE, [U-M1]). The look changes (AO applied over the lit image, can over-darken [U4]), so it needs an A/B on device.
3. **Further handheld-only steps, each with an A/B:** SSAO half resolution also at HIGH (Unity rates Downsample "very high" impact [U4]), SSAO blur Medium and 8 samples, bloom Quarter start + 5 iterations + low-quality filtering (the Run zones already use low-quality bloom on every platform [U-L1]). Build them as a tier renderer and a tier volume, not as runtime reflection writes.
4. **Anti-aliasing:** 4× MSAA is the right default on Apple GPUs (resolved inside the pass when the pass is not split [U8, U10]), but it does not touch specular or shader aliasing [U3]; the project measured 8.67 % crawl on high-contrast pixels under 4× MSAA [U-P1]. STP at render scale 1.0 is the only built-in fix. It needs motion vectors, and `FrontRoomsSurface.shader` (83 of 86 surface materials) has no MotionVectors pass while URP removed its fallback [U-L1, U-L3, U-M1]. So: an opt-in desktop "TEMPORAL" AA after that shader fix, never the default.
5. **Upscaling in 6000.6.3f1:** URP has Bilinear, Nearest, FSR 1 and STP only; FSR 2 and DLSS are HDRP-only [U12]. MetalFX is not in 6000.6: no hits in the URP/Core source or the Mac/iOS players, and the 6000.6 script-reference page does not exist [U-L3, U-L8, U15]. A `MetalFXModule` with an SRP Core upscaler appears in the Unity 6.7 beta docs [U15], and Unity moved its upscaler framework to 6.7 LTS [U32, pointer]. Until an engine upgrade: handheld keeps FSR 1 below 100 %; STP is the in-box candidate; MetalFX temporal is a 6.7 item.
6. **Tiers:** move from runtime writes on one shared asset to per-tier URP assets and renderers (desktop asset untouched; iOS assets with their own SSAO renderer, so the shader variants they need are compiled), switched only at boot or behind a loading cover, because a swap is a "temporary but significant performance impact" [U23]. Add a thermal response (6000.6 has `PlayerSettings.adjustIOSFPSUsingThermalState` [U27]; Apple's four thermal states [U26]) with fixed code paths, not a feedback loop [U28].
7. **Pass-merging hygiene on Metal:** the renderer's Native RenderPass switch is dead in URP 17.6 (merging is automatic through Render Graph) [U-L3]. What costs is any pass that samples the camera targets mid-frame (the glass opaque-colour copy, an after-opaque AO, a badly placed custom pass). One such split once measured 1.07 ms at 1440p [U-P1]. Check every rendering change in the Render Graph Viewer [U6].

## 1. What FrontRooms runs today (read from main, 2026-11-04)

### 1.1 The post stack

One global volume, `Post / FrontRooms film look` (priority 0, profile `Assets/Resources/Rendering/FrontRoomsPost.asset`, built by `Assets/Scripts/Rendering/FrontRoomsPostStack.cs`), plus local zone volumes (priority 1, box trigger, 2.5 m blend) that override a few values [U-L1].

| Effect | `FrontRoomsPost` (global) | `FrontRoomsPost_Office` (zone) | `FrontRoomsPost_Run_B` (zone) | Where URP runs it |
|---|---|---|---|---|
| Tonemapping | ACES (mode 2) | — | — | LUT pass |
| White balance | temp 9, tint −7 | temp 1, tint −14 | temp −6 | LUT pass |
| Color adjustments | exposure +0.15, contrast −6, saturation −8 | contrast +4, saturation −22, filter (0.95, 0.99, 0.97) | exposure −0.1, contrast +12, saturation −10 | LUT pass (exposure: uber) |
| Lift / gamma / gain | lift (1, 0.99, 0.94, +0.035) | lift (0.98, 1, 0.98, +0.02) | neutral | LUT pass |
| Shadows / midtones / highlights | slight warm shadows and highlights | slight | as global | LUT pass |
| Bloom | threshold 1.05, intensity 0.55, scatter 0.72, warm tint, **HQ filtering on**, filter Gaussian, downscale Half, max iterations 6 (`skipIterations: 1` is an obsolete field URP ignores [U-L3]) | — | threshold 1, intensity 0.6, scatter 0.7, white tint, **HQ filtering off**, Gaussian, Half, 6 | own pyramid passes + one tap in uber |
| Vignette | 0.26, smoothness 0.45, dark warm colour | — | 0.32, smoothness 0.2, black | uber |
| Film grain | Medium3 (type 4), 0.22, response 0.75 | Thin1 (type 0), 0.18, response 0.8 | Medium3, 0.22, 0.75 | uber, or the final pass when one exists |
| Chromatic aberration | 0.06 | 0.03 | 0.06 | uber |
| Lens distortion | −0.04, scale 1.01 | — | — | uber |

Pipeline settings that shape post: HDR on at 32-bit precision (`m_HDRColorBufferPrecision: 0`), grading mode HDR (`m_ColorGradingMode: 1`) with a 32³ LUT, volume update mode every frame (`m_VolumeFrameworkUpdateMode: 0`), camera post-AA None, dithering on, MSAA 4× from the asset ("the pipeline's 4x MSAA keeps the grain sharp", `FrontRoomsPostStack.ConfigureCamera`) [U-L1, U-L2].

So all colour work is already folded into one small LUT render each frame, and the lens effects, the bloom composite and the LUT lookup already share one full-screen uber pass [U-L3]. The separate costs are the bloom pyramid and SSAO.

### 1.2 SSAO and the prepass

Renderer feature on `Assets/Settings/FrontRooms_URP_Renderer.asset`, values decoded with the URP 17.6 enums [U-L3]: method Blue Noise, Downsample off (full resolution), After Opaque off, Source **Depth Normals**, Samples High (12), Blur High (Bilateral, 3 passes), radius 0.45, intensity 1.6, direct lighting strength 0.35, falloff 40 m [U-L2].

Measured 2026-11-04 (clone, 6000.6.3f1 Editor batch, Metal, M3 Max, 1920×1080, desktop ULTRA) [U-M1]:

| variant | draws | SetPass | image change (corridor views; maze views) |
|---|---|---|---|
| ssao_off | −4 | −4 | 9.6 / 5.4 / 3.4 %; 6.2 / 4.9 / 7.4 % (clearly visible) |
| ssao_depth (normals reconstructed from depth) | = | = | 0.29 / 0.25 / 0.24 % |
| msaa1 (MSAA off) | = | = | 0.18 / 0.32 / 0.11 % (edges, max diff 106-117 of 255) |
| stp (STP at scale 1.0, MSAA off) | +26 / +21 / +17 | +3 / +2 / +2 | 0.19 / 0.34 / 0.12 % |
| depthprime, glassrt_off (corridor) | = | = | noise |

The October measurement (M3 Max Editor, Metal, 1920×1080, 2026-10-02/03) found SSAO off = −26 % batches / −42 % SetPass / −17 % triangles, because "the ×2 is the depth-normals prepass that exists only for SSAO" [U-L6]. On the Mac today SSAO off removes only its own 4 draws [U-M1]. Reason, from the URP 17.6 source (INFERENCE, not measured): a prepass is added when normals are needed, or when any pass needs depth before `AfterRenderingOpaques` (`RequirePrepassForTextures`, `UniversalRendererRenderGraph.cs`) [U-L3]; the Mac-only glass ray-tracing feature, which first ran on 2026-10-04, after the October measurement [U-P1], asks for depth at `AfterRenderingPrePasses` [U-L5], so on the Mac the prepass stays (as depth-only) when SSAO goes. On iPhone, iPad, Windows and WebGL that feature is compiled out [U-L5], so SSAO is the only reason for the prepass there, and the October −26 % / −42 % is the right order of magnitude for them (INFERENCE).

Shader-build state that limits runtime switches: the asset's prefilter flags (recomputed by URP at every build) strip the SSAO Depth-source variants, the Interleaved method and Samples Low / Medium, and the SSAO-off variant of lit shaders, because every renderer in the build has SSAO with these settings [U-L2, U-L7]. So a runtime switch to Source = Depth, Samples Low, or SSAO off would ask for a stripped variant. Only Downsample (a render-target size, no keyword) is safe, which is why `FrontRoomsQuality.cs` changes only that [U-L4].

### 1.3 Other passes that touch the frame

- `FrontRoomsMetalGlassRTRendererFeature` (Mac only, Metal only): asks for Depth input and records at `AfterRenderingPrePasses`; its trace and end passes are unsafe passes. Its own comment records that the earlier placement split URP's merged opaque + transparent native render pass and "cost a store and reload of the 4x MSAA attachments, 1.07 ms at 1440p" (run of 2026-10-04); a later interleaved test could not separate the two placements from noise [U-L5, U-P1].
- `FrontRoomsGlassSceneColor`: asks for the opaque colour texture (Color | Depth input at `BeforeRenderingTransparents`) per camera, only while a glass pane is held or cracked within 30 m; never on WebGL [U-L5].
- `FrontRoomsHeldItem`: a pass at `AfterRenderingOpaques` for the held item's depth punch, injected from `beginCameraRendering` [U-L5].
- Shader passes: `FrontRoomsSurface.shader` has DepthNormals, DepthOnly, ShadowCaster and UniversalForward passes and no MotionVectors pass; `FrontRoomsGlass.shader` has FRGlassRTPrepass and UniversalForward; `RunBeamCone.shader` has UniversalForward only [U-L1]. URP removed its automatic motion-vector fallback; custom shaders must add `UsePass "Hidden/Universal Render Pipeline/ObjectMotionVectorFallback/MOTIONVECTORS"` or their own pass (URP CHANGELOG) [U-L3].

### 1.4 Native RenderPass in URP 17.6

The renderer asset has `m_UseNativeRenderPass: 0`. In URP 17.6 this field is dead: `useNativeRenderPass` is marked obsolete "from(6000.5)" with the text "Native Render Pass is automatically enabled through Render Graph, useNativeRenderPass is not considered anymore", and its getter returns true (`Runtime/Deprecated.cs`) [U-L3]. Unity's 6000.6 performance page still says "Enable Native RenderPass if you use Vulkan, Metal or DirectX 12" [U11]; the source shows that advice is now automatic. So there is nothing to switch on; what matters is which passes break the merge (section 2.4).

### 1.5 Quality presets as built (`Assets/Scripts/FrontRoomsQuality.cs`)

- One URP asset for every quality level: all six levels in `ProjectSettings/QualitySettings.asset` point to `FrontRooms_URP.asset`; Standalone default level 5 "Ultra", WebGL default 3 [U-L2]. Presets are runtime writes to that shared asset, never applied when a value equals the asset's own, and undone on quit, so desktop ULTRA draws the asset unchanged [U-L4].
- Writes: render scale (desktop LOW 70 % / MED 85 %; handheld AUTO = a 3.2 Mpx budget, 2.0 Mpx at LOW, in 5 % steps, 50-100 %), upscaling filter FSR 1 (SHARP) or bilinear (SOFT) only below 100 %, MSAA 1 / 2 / 4, shadows (LOW 25 m, 1 cascade, 1024 maps; MED additional atlas 2048; HIGH desktop atlas 8192), SSAO half resolution at LOW / MED (by reflection on the internal settings field), bloom HQ filtering off at LOW / MED (through a priority-100 global override volume), frame-rate caps (handheld 30 / 60, ProMotion 30 / 40 / 60 / 120; desktop vsync / 30 / 60 / 120 / max), iOS Low Power Mode follow (30 fps + LOW) [U-L4].
- Not present: STP, TAA, SMAA, FXAA, MetalFX, dynamic resolution, a thermal response, per-tier URP assets [U-L4]. Player settings: iOS graphics API Metal only, WebGL WebGL2, ProMotion support on (`appleEnableProMotion: 1`), Frame Timing Stats off (`enableFrameTimingStats: 0`), Windows API list not pinned (automatic) [U-L2].

### 1.6 What URP 17.6 / Unity 6000.6.3f1 actually contains (local inspection, read-only)

- Upscaling enum in URP 17.6: `Auto, Linear, Point, FSR, STP` (`Runtime/Data/UniversalRenderPipelineAsset.cs`) [U-L3].
- A newer upscaler framework sits in SRP Core 17.6 `Runtime/Upscaling/` (`IUpscaler.cs`, `Upscaling.cs` with `UpscalerRegistry`, `FSR2IUpscaler.cs` behind `ENABLE_UPSCALER_FRAMEWORK && ENABLE_AMD && ENABLE_AMD_MODULE`, `DLSSIUpscaler.cs` behind `ENABLE_UPSCALER_FRAMEWORK && ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE`) and `Runtime/STP/STPIUpscaler.cs`; under that define `UpscalingFilterSelection` is marked obsolete "from(6000.3)" [U-L3, U-M1]. No asmdef sets the define, and the compiled `Unity.RenderPipelines.Core.Runtime.dll` and `...Universal.Runtime.dll` contain none of those type names (0 hits each; `StpPostProcessPass` is present, 3 hits) [U-L3]. In this project the framework is compiled out.
- MetalFX: 0 case-insensitive hits for "metalfx" in the URP 17.6 and Core 17.6 packages (both copies), in the 6000.6.3f1 iOS `UnityRuntime.framework` binary and in the macOS arm64 `UnityPlayer.dylib`, which links `Metal.framework` but not `MetalFX.framework` (`otool -L`) [U-L8, U-M1]. **Unity 6000.6.3f1 has no built-in MetalFX path in URP.**
- At render scale 1.0, a game camera with the filter set to STP (temporal) or FSR (sharpening) still runs the upscaler pass, and STP forces the camera's AA to TAA (`UniversalRenderPipeline.cs`) [U-L3]. A render scale within ±0.05 of 1 is treated as 1.0 (`kRenderScaleThreshold = 0.05f`) [U-L3].
- TAA (and therefore STP's temporal part) runs only if post-processing is on, MSAA is 1×, there is no camera stack, `allowDynamicResolution` is false, and the renderer supports motion vectors (`UniversalCameraData.IsTemporalAAEnabled`) [U-L3].
- Post order in URP 17.6 (`Runtime/PostProcess.cs`): StopNaN → SMAA → depth of field → STP or TAA → motion blur → Panini → bloom → lens flares → uber; then a final pass (FXAA, FSR 1 RCAS, scaling) when needed, in which film grain and dithering are applied after FXAA / RCAS (`Shaders/PostProcessing/FinalPost.shader`) [U-L3]. **No URP AA mode runs after the grain**, so none of them smears it.
- STP writes its output at the camera's full pixel size (`StpPostProcessPass.GetStpTargetDesc` uses `pixelWidth × pixelHeight`), so bloom and uber run at output resolution after STP; FSR 1 runs in the final pass, after post at render resolution [U-L3].
- On GPUs that report hidden surface removal (`SystemInfo.hasHiddenSurfaceRemovalOnGPU`, Apple GPUs), URP already skips front-to-back sorting of opaques [U-L3].
- New in 17.6 (from 6000.5): a renderer option **Tile-Only Mode** and an **On Tile Post Processing** renderer feature. The on-tile pass handles only vignette, LUT colour grading, tonemapping, film grain and dithering (`Runtime/RendererFeatures/OnTilePostProcessPass.cs`) [U-L3, U6, U7].

## 2. Post-processing budget in URP 17

### 2.1 Where post cost goes on a tile GPU and on a desktop GPU

- Apple GPUs are tile-based deferred renderers: each tile is shaded in on-chip tile memory and written to device memory at the end of the render pass; tile memory has many times the bandwidth, lower latency and less energy than device memory [U8].
- Apple: load and store actions "consume the majority of your app's system bandwidth", they run once per render pass, so traffic falls with the number of render passes [U10]. Memoryless textures live only in tile memory, for attachments used within one pass [U9].
- So on Apple GPUs (Mac, iPhone, iPad) a full-screen post pass that reads the previous image as a texture costs a store of the previous pass, a full read and a new store. On a desktop immediate-mode GPU the same pass is mostly ALU and cache work over a buffer that already lives in VRAM. INFERENCE: the number of passes matters more on Apple hardware than on a Windows dGPU; the pixel count matters on both.
- Unity's 6000.6 on-tile page lists what forces URP to store the camera targets: depth and opaque textures, TAA, MSAA, built-in post-processing, HDR, upscaling, dynamic resolution, camera stacking, deferred; and on-tile rendering is "supported on all platforms except WebGL" [U6]. FrontRooms uses depth texture, MSAA, built-in post, HDR, upscaling below 100 % on handheld, and the occasional opaque texture [U-L2, U-L4, U-L5]. Tile-Only Mode is therefore out of reach without changing the look; it is listed here to show which costs are structural.
- Unity's mobile tips for technical artists: full-screen post effects "like glows, can dramatically slow down performance" [U37].

### 2.2 How pros keep post cheap, and where FrontRooms stands

| Practice | Source | FrontRooms today |
|---|---|---|
| One uber pass for all per-pixel effects (grading lookup, vignette, grain, CA, lens distortion, bloom composite) | URP's own structure: `UberPostProcessPass` [U-L3] | already so [U-L1, U-L3] |
| Grading baked into a small 3D LUT, not evaluated per pixel | URP HDR grading mode, LUT size 32 [U-L2, U-L3] | already so. ESTIMATE: the LUT strip is 1024 × 32 = 32,768 px = 1.6 % of one 1080p pass (2,073,600 px) |
| HDR target at 32 bits per pixel | "If you need HDR, set HDR Precision to 32 Bit" [U11] | already so (`m_HDRColorBufferPrecision: 0`) [U-L2] |
| Bloom from a reduced-resolution pyramid; cheaper start and fewer mips on high-DPI mobile | "set this value to Quarter to significantly reduce the initial resource cost"; lowering Max Iterations helps "especially on mobile devices with high DPI screens"; Dual "a faster alternative for mobile", Kawase "the fastest option at low resolution" [U5]; the dual filter alternates down- and upsampling instead of ping-ponging full-size targets (ARM, SIGGRAPH 2015) [U30] | Half start, 6 mips, Gaussian, HQ filtering on (global); Run zones HQ off [U-L1] |
| Bicubic bloom upsampling only where it pays | HQ filtering "reduces flickering … but is more resource-intensive"; disabling it can "greatly improve performance, especially on lower-end hardware" [U5] | on in the global look, off in Run zones, off at LOW/MED [U-L1, U-L4] |
| Noise and grain after AA / upscaling, so they are not smeared or amplified | AMD: passes that add noise "should be rendered after upscaling to avoid those noisy components being amplified" [U22] | URP 17.6 already applies grain and dither after FXAA / RCAS, and in uber after TAA / STP [U-L3] |
| Update volumes only when they change | "Set Volume Update Mode to Via Scripting" [U11] | every frame; the zone volumes blend over 2.5 m as the player walks, so per-frame updates are needed during a blend [U-L1, U-L2] |

Bloom arithmetic, from the URP 17.6 code (`BloomPostProcessPass.cs`: start at half or quarter size, `mipCount = clamp(floor(log2(maxSize) − 1), 1, maxIterations)`, Gaussian = 1 prefilter + 2 blur passes per lower mip + 1 upsample per mip) [U-L3]:

- ESTIMATE at 1920×1080, Half start, 6 mips: mips 960×540, 480×270, 240×135, 120×67, 60×33, 30×16. Passes = 1 + 2×5 + 5 = **16 blits**. Pixels written ≈ 518,400 (prefilter) + 2 × 172,500 (blurs) + 690,420 (upsamples) ≈ **1.55 Mpx**, about 0.75 of one full-screen pass.
- ESTIMATE, Quarter start, 6 mips: mips from 480×270 down to 15×8; pixels written ≈ 129,600 + 2 × 43,020 + 172,500 ≈ **0.39 Mpx (−75 %)**; same 16 passes. The prefilter still reads the full-size source.
- The iteration cap binds at every FrontRooms resolution (1080p allows 8, the clamp is 6), so the widest mip is always about 1/64 of the screen width (ESTIMATE: ½ × (½)⁵). Quarter + 6 mips would push the widest halo to 1/128 of the width and drop the tightest glow; Quarter + 5 mips keeps the widest halo and drops only the half-resolution level. INFERENCE: no bloom knob is visually identical; each one changes the halo around thin bright tubes, the look's centre. The dead `skipIterations` field costs nothing either way.
- Dual filter: ESTIMATE 1 prefilter + 5 down + 5 up = 11 passes instead of 16, with a different look whose properties "have to be adjusted separately" [U5].

Per platform: on Metal (Mac, iPhone, iPad) each of the 16 bloom blits is its own render pass with a load and a store, so pass count matters [U10]; on D3D12/Vulkan desktop GPUs the pixel count matters more than the pass count (INFERENCE); WebGL2 runs the same passes with no on-tile path [U6].

### 2.3 SSAO cost and the depth-normals prepass

What drives SSAO cost, by Unity's own ratings (6000.6) [U4]:

| Setting | Unity's rating | Note |
|---|---|---|
| Downsample | very high | "reduces the resolution … by a factor of two", a quarter of the pixels; "makes the effect less detailed" |
| Blur Quality | very high | Bilateral 3 passes, Gaussian 2, Kawase 1 |
| Samples | high | 12 / 8 / 4; "4 to 8 doubles the computational load" |
| Radius | high | smaller radius caches better |
| Normal Quality (Depth source only) | medium | |
| After Opaque | medium | with Source = Depth, Unity "does not perform the depth prepass … and instead uses the existing depth values"; "can also increase performance on mobile devices that use tile-based rendering"; "can cause over-darkening" |
| Source | depends on the application | Depth Normals uses the DepthNormals pass; Depth reconstructs normals; "in most cases, Depth Normals produce a better visual look" |

The prepass (URP 17.6 code, `RequirePrepassForTextures`) [U-L3]:

- Any normals request forces a prepass, because "URP's forward passes don't support rendering into the normals texture".
- A depth request before `AfterRenderingOpaques` forces a prepass too. In Forward+, SSAO without After Opaque records at `AfterRenderingPrePasses + 1` with Depth (+ Normal) input; with After Opaque it records at `BeforeRenderingTransparents` (`ScreenSpaceAmbientOcclusion.cs`). So **switching only the Source to Depth keeps a prepass** (depth-only); only Depth + After Opaque removes it.
- Without a prepass, the depth texture comes from a copy after opaques when the platform can copy depth; with MSAA that needs multisampled-texture support (`CanCopyDepth`), which Metal has (INFERENCE from the code path).
- Depth priming (reuse the prepass so opaques shade only visible pixels) is "Auto or Forced for PC and console … Disabled for mobile" [U11]; URP's Auto turns it on only off mobile and never on WebGL [U-L3].

Why the prepass matters on Apple GPUs: "if you only perform a depth pre-pass for performance, then hidden surface removal serves the same purpose on Apple GPUs", and "with HSR, geometry is only processed once" [U10]. So on Mac, iPhone and iPad the prepass buys no speed; it exists only to feed SSAO (and the Mac glass tracer). Its cost is a second submission of every opaque renderer (CPU, vertex) plus the depth and normals stores (INFERENCE).

What AO costs elsewhere, for scale: Intel's XeGTAO, full resolution, High preset: about 0.56 ms at 1080p on an RTX 2060, 1.4 ms at 4K on an RTX 3070, 2.39 ms at 1080p on an 11th-gen Intel iGPU; Medium ≈ two-thirds of High; it takes depth plus optional normals and has its own depth-to-normal generator [U13]. The original GTAO ran at half resolution and reached ground-truth quality "in just 0.5 ms on a PS4 at 1080p" [U14] (snippet), and XeGTAO's FAQ suggests half resolution or checkerboard plus a bilateral upsample for further speed [U13]. No millisecond figure exists for URP SSAO on an Apple GPU in any source read; the probe could not time GPU passes in batch mode [U-M1].

What the player sees (measured on desktop [U-M1]): SSAO is part of the look (3.4-9.6 % of pixels); normals from depth move 0.24-0.29 % of pixels, above the 0.00-0.07 % noise floor, so even that is a look change by the project's rule.

### 2.4 Pass merging on Metal: what breaks it in this project

- Native render passes are automatic in URP 17.6 Render Graph [U-L3]. A merge breaks when a later pass samples a camera target as a texture instead of reading it as framebuffer input [U6].
- MSAA is cheap on Apple GPUs only when it resolves inside the pass: Apple calls storing the multisampled attachment and reloading it to resolve an anti-pattern, and says to resolve "as part of the previous pass" so "we only consume system memory bandwidth for the pixels that matter and not the samples" [U10]. Apple GPUs also run per-sample blending only on edge pixels [U8].
- In FrontRooms the candidates for a split are: the glass opaque-colour request (only while a pane is held or cracked within 30 m) [U-L5]; the glass tracer's unsafe passes, already moved to `AfterRenderingPrePasses`, where they do not split the opaque + transparent pass [U-L5, U-P1]; and any future after-opaque AO or custom effect. The one project measurement of a split is 1.07 ms at 1440p with 4× MSAA (2026-10-04, later not reproducible above noise) [U-P1].
- Unity's tool for this is the Render Graph Viewer, used "on your target device" [U6, U7].

## 3. Anti-aliasing: MSAA, FXAA, SMAA, TAA, STP

| Method | How it works, cost (sources) | Tile GPU (Apple) | Desktop IMR GPU (D3D12 / Vulkan) | Fit for FrontRooms |
|---|---|---|---|---|
| MSAA 4× (today) | multiple depth/coverage samples per pixel; best on triangle edges; "does not fix shader aliasing issues such as specular or texture aliasing"; "more resource intensive than other forms … on most hardware", but on "a tiled GPU with no post-processing anti-aliasing or custom render features in use … a cheaper option" [U3] | samples can stay in tile memory and resolve in the pass [U8, U10] | ESTIMATE at 2560×1440: 4 samples × 4 B colour (R11G11B10) × 3.69 Mpx ≈ 59 MB, same again for depth ≈ 118 MB, against ≈ 30 MB at 1× | right default; leaves specular crawl |
| FXAA | one full-screen pass, "least resource intensive"; Unity recommends it for mobile [U3] | one extra pass; in URP it runs in the final pass with grain after it [U-L3] | cheap | softens the paper texture of the walls; a fallback for a battery tier or WebGL |
| SMAA | edge patterns, "much sharper results than FXAA" [U3]; Low vs High cost difference "fairly small" [U20]; paper: morphological AA combined with multi/supersampling (Eurographics 2012) [U19]; URP runs 3 raster passes (edge detection with stencil, blend weights, neighbourhood blend) [U-L3] | 3 extra full-screen passes = 3 store/load round trips (INFERENCE from [U10]) | cheap | edges only; no help on specular shimmer; no reason to replace MSAA |
| TAA | colour history over frames; "often creates ghosting … when a GameObject moves quickly in front of a surface that contrasts with it"; not with MSAA, camera stacking or dynamic resolution [U3]; tuning: higher Base Blend Factor gives more AA and more ghosting, lower Variance Clamp less ghosting and more flicker, Contrast Adaptive Sharpening against blur [U20] | needs motion vectors and history targets: more stores (INFERENCE) | moderate | fixes shader aliasing; risks below |
| STP | "spatial and temporal upsampling techniques to produce a high quality, anti-aliased image"; needs Shader Model 5.0 compute, no OpenGL ES; implies TAA; lighter filtering on mobile [U1]; at render scale 1.0 it "applies temporal anti-aliasing (TAA) to the final rendered output" [U2] | Metal path forces groupshared reductions (`StpSetup.compute`) [U-L3]; supported | supported | the in-box temporal option; see 4.1 |

Measured in this project:
- MSAA 4× still leaves shimmer on specular and high-contrast detail: in the glass study, "the raster's own pixels" crawled 8.67 % at the 10 % highest-contrast pixels in a 1 cm/frame strafe, with 4× MSAA on [U-P1].
- Turning MSAA off changes only 0.11-0.32 % of pixels (edges), each by up to 106-117 of 255 [U-M1]: a small area, a strong local difference.
- STP at scale 1.0 with MSAA off: +17-26 draws, +2-3 SetPass, +17-24k triangles, 0.12-0.34 % pixel change; GPU time unknown [U-M1].

What TAA / STP would do to the FrontRooms image (INFERENCE from the sources named, to be checked by eye):
- Film grain stays crisp: URP adds grain after TAA / STP and after FXAA / RCAS [U-L3]. The concern in `FrontRoomsPostStack.ConfigureCamera` ("MSAA keeps the grain sharp") does not hold in URP 17.6.
- Blue-noise SSAO is animated per frame by design [U4]; a temporal filter averages it, so the AO would look calmer and slightly softer.
- Flickering and dying lamps: a full-frame lighting change moves the current neighbourhood, so the variance clamp lets the new value through within a frame or two; small bright lamp lenses against dark ceilings are the high-contrast case Unity names for ghosting [U3, U20].
- Moving objects: `FrontRoomsSurface.shader` has no MotionVectors pass and URP no longer adds a fallback [U-L1, U-L3], so doors, debris, the held item and the entity would carry only camera motion and smear. The fix is a `UsePass` line or a MotionVectors pass in the custom opaque shaders (游戏视觉).
- Precedent: Playdead's INSIDE "makes strong use of Temporal Reprojection Anti-Aliasing to deliver satisfactorily clean and stable images", and the GDC 2016 talk covers "disocclusion and trailing artefacts" [U31]. (That INSIDE's image is dark and grainy like FrontRooms is UNSOURCED here; the session page does not describe the look.) Epic's 2014 TAA talk notes the technique "does not require MSAA" [U29].

Per platform: Metal desktop and iOS can run all five (STP through compute) [U1, U-L3]; D3D12 / Vulkan Windows all five [U1]; WebGL2 has no native compute shaders [U40], so STP falls back to Auto there (`STP.IsSupported`) [U-L3], and MSAA, FXAA, SMAA or TAA remain.

## 4. Upscaling: STP, MetalFX, FSR 1, FSR 2/3, DLSS

### 4.1 What Unity 6000.6 URP offers

Unity's 6000.6 table [U12]: URP has Bilinear, Nearest-neighbor, STP ("you can't use STP together with dynamic resolution") and FSR 1. Catmull-Rom, CAS, TAAU, FSR 2 and DLSS 4 Super Resolution are HDRP-only. The table does not mention MetalFX, Metal, Apple, FSR 3 or XeSS [U12].

- **FSR 1** (used today below 100 % on handheld [U-L4]): spatial only, "without relying on other data such as frame history or motion vectors"; EASU edge-adaptive upsampling + RCAS sharpening; place after tone mapping; modes 1.3× / 1.5× / 1.7× / 2.0× per axis; cost "or less" 0.20-0.50 ms at 1440p and 0.40-1.0 ms at 4K on 2020-class desktop GPUs [U22]. URP runs it in the final pass after post at render resolution, with grain after it [U-L3].
- **STP**: requirements as in section 3 [U1, U2]. It replaces MSAA (TAA needs 1×) [U-L3]. It is not usable with dynamic resolution [U2, U12]. Because STP upsamples before bloom and uber, post then runs at output size [U-L3]. ESTIMATE for the iPhone Duo inner panel (2670×1878 = 5.01 Mpx) at AUTO 80 % (3.21 Mpx rendered): bloom and uber pixel work +56 % (5.01 / 3.21) against the FSR 1 path, plus STP's own compute, minus the 4× MSAA attachments. Net unknown until measured.
- **FSR 2/3 and DLSS in URP**: code exists in SRP Core 17.6 behind `ENABLE_UPSCALER_FRAMEWORK` plus AMD / NVIDIA module defines, compiled out in this project [U-L3, U-M1]. A Unity Discussions post (2026-05-26, Unity staff: Discourse group `unity_staff`, pointer only) says: "We have recently moved our target from Unity 6.6 to Unity 6.7 LTS for the upscaling framework" [U32]. INFERENCE: DLSS would not run on Apple hardware in any case.

### 4.2 MetalFX

- Apple: MetalFX offers temporal antialiased upscaling (needs colour, depth and motion) and spatial upscaling (colour only); the scalers "take time to initialize", so create them at launch or when the display resolution changes; the framework also lists a temporal denoised scaler and a frame interpolator; availability iOS 16, macOS 13 [U18].
- WWDC22: spatial "only requires anti-aliased input color", runs "right after the game's tone-mapping"; temporal needs jittered colour, motion and depth, "should run before any post processing effects", and gives quality "comparable to the quality of the native target resolution rendering" [U17].
- Frame interpolation (WWDC25, via the project's October settings research): two rendered frames, motion vectors and depth, placed after tone mapping, with a floor of about 30 fps [U-L9].
- **Unity, verified version by version:**
  - 6000.6.3f1 (this project): no MetalFX in the URP / Core source or the Mac / iOS players [U-L3, U-L8, U-M1]; the `UnityEngine.MetalFXModule` script reference for 6000.6 returns 404 [U15].
  - 6000.7 beta and 7000.0 alpha script reference: "A module that provides the low-level binding to Apple MetalFX upscaling (spatial and temporal)", with "the Scriptable Render Pipeline upscaler integration that uses this binding" in SRP Core `Runtime/Upscaling`, macOS 13 / iOS 16 / tvOS 16 [U15]. Which pipelines use it is not stated.
  - Unity China's Tuanjie engine manual (docs.unity.cn, Tuanjie 1.x) has `RenderTexture.metalFXOutput` ("This render texture is used for MetalFX output") [U16]. INFERENCE: reports of "Unity adding MetalFX around 6.1-6.3" most likely refer to Tuanjie or to the 6.7 work, not to Unity 6000.1-6000.6.
  - Third party: "MetalFX - Upscaling for Unity" (The Naked Dev, Asset Store, v1.0.6, 2026-10-23, listed for 2022.3.62f3, Built-in / URP / HDRP), with no stated modes or limits [U33]. The October settings research found two developer-forum reports of plugin trouble (worse GPU time on an iPhone 13 URP sample; HDRP output reverting after a few frames) [U-L7].
- **Comparable games** (Digital Foundry, secondary summaries only, DF pages not opened):
  - Resident Evil Village, Mac: the temporal "Quality" mode was judged strong, close to DLSS at high internal resolution; the spatial "Performance" mode lost detail and was not worth the small frame-rate gain; vegetation smeared in motion [U34].
  - Assassin's Creed Mirage, iPhone 15 Pro: about 300p internal against about 828p on Xbox One; the better upscaling narrowed the gap; often near 30 fps; the low preset recommended for stability [U35].
  - Death Stranding Director's Cut: good on Mac, average on iPhone 15 Pro [U36].
  - INFERENCE: on Apple hardware, temporal reconstruction is what makes low internal resolutions acceptable; spatial upscaling at a large ratio is not. FrontRooms' handheld ratio is mild (80-100 %), where FSR 1's weakness matters least.

### 4.3 Per platform

- Metal macOS: FSR 1, STP; MetalFX only after an upgrade to 6.7 or through a plugin [U12, U15, U33].
- Metal iOS / iPadOS: FSR 1, STP (mobile filtering) [U1, U12]; MetalFX as above.
- D3D12 / Vulkan Windows: FSR 1, STP [U12]; FSR 2/3 and DLSS in URP wait for the 6.7 framework [U32].
- WebGL2: FSR 1 needs shader model 4.5 or falls back to Auto (enum tooltip) [U-L3]; whether WebGL2 qualifies is UNCONFIRMED here; STP unsupported (no compute) [U40, U-L3]; no dynamic resolution [U21].

## 5. Per-platform quality tiers

### 5.1 How pros structure tiers in Unity 6 URP

- One URP asset per quality level; switch with the `QualitySettings` API; indices count only the levels enabled for the target platform; change Quality and URP asset settings only "at points where performance is not essential, such as during loading screens or on static menus", because the change has "a temporary but significant performance impact"; fine-tune individual properties through `GraphicsSettings.currentRenderPipeline` [U23]. URP rebuilds the pipeline (renderers, Render Graph, target pool, history) on an asset swap (17.3 source) [U-L7].
- Shader variants: a variant is kept if any renderer of any asset in the build needs it, so tier assets can add variants (for example an SSAO-off renderer keeps the SSAO-off variants) but never strip desktop ones; tiers that differ in rendering path double variants, so keep every tier on Forward+; keep platform lists apart per level [U-L7].
- Device choice: Unity's example picks the level from `SystemInfo.graphicsMemorySize` [U-L7]; FrontRooms already picks handheld by `FrontRoomsHandheld.Active` and sizes resolution by a pixel budget per screen [U-L4].
- Many mobile games default to a device-picked preset and label it "Recommended" (October settings research) [U-L7].

### 5.2 Dynamic resolution

- URP supports dynamic resolution on iOS, macOS and tvOS (Metal only), Android (Vulkan only), Windows and UWP (DirectX 12 only); not WebGL; targets are allocated once at full size and a portion is used, so scaling does not reallocate [U21].
- It cannot be combined with TAA or STP [U2, U3, U-L3].
- Driving it from GPU time needs `FrameTimingManager`; in release builds frame timing needs Frame Timing Stats on [U25], and it is off in this project [U-L2].
- Today FrontRooms uses fixed render-scale steps (5 %), recomputed on fold / rotation, which reallocates targets once per change [U-L4]. For a preset or a one-off change that is the right tool; for continuous load-following, dynamic resolution [U-L7].

### 5.3 Thermal and power on iOS

- Apple's thermal states: nominal, fair ("slightly elevated"), serious ("high"), critical ("significantly impacting the performance of the system and the device needs to cool down") [U26].
- Unity 6000.6: `PlayerSettings.adjustIOSFPSUsingThermalState`: "Unity lowers the iOS frame rate in Serious and Critical thermal states", with targets per state in the iOS Player Settings [U27].
- Adaptive Performance is built into 6000.6 (Basic and Simulator providers built in; other providers per platform as packages; enabling it prompts to disable the iOS thermal FPS adjustment) [U25]; the Apple provider package covers iOS, tvOS and visionOS thermal state and performance mode (Unity 6.5 manual) [U24]. The URP asset's `m_UseAdaptivePerformance: 1` is inert today: no Adaptive Performance module in the manifest [U-L2, U-L7].
- FrontRooms today follows iOS Low Power Mode (30 fps + LOW) but has no thermal response [U-L4].

### 5.4 Frame pacing at 30 / 40 / 60 / 120 Hz

- iPhone ProMotion steps include 120, 80, 60, 48, 40, 30 Hz; iPad Pro 120, 60, 40, 30, 24 Hz; "the system disables faster refresh rates in low power mode or if a device gets hot" [U28].
- "In iOS 15 and later, the system provides games with special priority to 30Hz and 60Hz refresh rates" [U28].
- "If your animation requests 120Hz but can't keep up, it may render poorly", while it may hold "a steady cadence at a lower rate" [U28].
- "Dynamically adjusting render quality in response to refresh rates … can throttle the GPU. To avoid an oscillating feedback loop, use code paths for different frame rates" [U28].
- FrontRooms offers 30 / 40 / 60 / 120 on ProMotion with ProMotion support on [U-L2, U-L4]. INFERENCE: the menu matches Apple's steps; 40 needs a measured steady cadence on iPad Pro and iPhone; 120 stays an opt-in.

### 5.5 The project rule

Desktop at ULTRA must draw byte for byte as today; mobile and WebGL are separate tiers. `FrontRoomsQuality` already honours it by never writing a value equal to the asset's own [U-L4]. A per-tier asset structure keeps it by construction: the desktop asset and renderer files stay untouched, and handheld / WebGL get their own files.

## 6. Applied to FrontRooms

Ids U-A1 … U-A13. Owners: 游戏视觉 owns rendering files (renderer, pipeline assets, shaders, post profiles); 设置菜单 owns quality presets (`FrontRoomsQuality.cs` and the menu). "Desktop look change" is about the desktop default (ULTRA).

Caution on scale: the October settings probe found that on the M3 Max each single feature (MSAA 2× or off, HDR off, SSAO off or half, bloom off or LQ, soft shadows off) moved the handheld-path picture cost by less than 1 ms, inside the noise, and only render scale clearly beat the noise (picture cost ≈ 0.85 ms + 0.73 ms per megapixel, 6000.3.10f1) [U-P2]. So the GPU savings below are small per row on desktop-class hardware; they matter on the phone (≈ 2.5× slower GPU, less bandwidth [U-P2]) and in draw count, and every one must be confirmed with an Xcode capture on the device.

| id | change | what the player sees | expected saving | risk | tier | owner | desktop look change |
|---|---|---|---|---|---|---|---|
| U-A1 | Handheld renderer with SSAO Source = Depth + After Opaque, so URP drops the opaque prepass on iPhone / iPad | AO from reconstructed normals (desktop analogue: 0.24-0.29 % of pixels [U-M1]); AO applied over the lit image, may over-darken occluded areas [U4] | ESTIMATE maze: opaque in-frustum renderers 2.1k-2.8k (2.5k-3.2k in frustum minus 380-456 transparent [U-M1]) of 4.8k-7.4k draws → 36-44 % of draws; October measured −26 % batches / −42 % SetPass with SSAO off (Mac Editor, before the glass tracer) [U-L6]; plus the prepass depth/normals stores | look change on handheld; the after-opaque composite splits the opaque/transparent pass (one full-res store + load); Depth-source variants must be in the iOS build | Mobile | 游戏视觉 (renderer) + 设置菜单 (wiring) | none |
| U-A2 | SSAO half resolution at handheld HIGH too (today LOW/MED only) | softer contact shadows at corners and furniture feet | ESTIMATE ≈ −75 % of SSAO's per-pixel work (¼ pixels) minus an upsample; for scale, a full-res 1080p AO costs 2.39 ms on an Intel iGPU in XeGTAO [U13] | halos at depth edges; flicker at thin legs | Mobile | 设置菜单 | none |
| U-A3 | Handheld SSAO blur Medium (Gaussian, 2 passes) and Samples Medium (8) | slightly noisier AO | ESTIMATE −1 of 3 blur passes; −33 % AO samples (12 → 8) | Medium-sample variants stripped today; needs the tier renderer (U-A10) | Mobile | 游戏视觉 | none |
| U-A4 | Handheld bloom: Quarter start + max iterations 5 + HQ filtering off (via the tier volume) | tightest glow around tubes softer; thin highlights may flicker more (HQ "reduces flickering" [U5]) | ESTIMATE ≈ −75 % of bloom pixels written (1.55 → 0.39 Mpx at 1080p), 16 → 13 blits | the halo is the look's centre; needs Red's eye on device | Mobile | 游戏视觉 (profile) + 设置菜单 | none |
| U-A5 | Opt-in desktop AA choice "TEMPORAL" = STP at scale 1.0 (MSAA off), after U-A6 | gain: calmer specular and wallpaper shimmer (MSAA leaves 8.67 % crawl [U-P1]); slightly softer image; possible trails on lamp lenses | +17-26 draws, +2-3 SetPass measured [U-M1]; GPU ms unknown; frees the 4× MSAA targets (ESTIMATE ≈ 88 MB at 1440p: 118 − 30), minus STP's history and intermediate targets (not sized) | ghosting without motion vectors; glass tracer untested with jitter; not with dynamic resolution | Desktop (option) | 游戏视觉 + 设置菜单 | none at default; gain if chosen |
| U-A6 | Motion vectors in custom opaque shaders (`UsePass "Hidden/Universal Render Pipeline/ObjectMotionVectorFallback/MOTIONVECTORS"` in `FrontRoomsSurface.shader` and the others used on moving objects) | nothing until TAA/STP is used | none (enables U-A5, U-A7) | a MotionVectors draw per moving object when TAA/STP is on; the added pass must keep the shader SRP-Batcher compatible (same per-material constant buffer in every pass), or desktop draw cost rises (INFERENCE from the SRP Batcher rule, UNSOURCED here) | all | 游戏视觉 | none (pass unused with MSAA) |
| U-A7 | Handheld: STP instead of FSR 1 when AUTO is below 100 % (Duo inner 80 %, iPad 11" 90 %) | gain in stability and edges; softer than FSR's sharpened look | ESTIMATE post +56 % pixels at output size, minus 4× MSAA; net unknown | STP mobile path untested; ghosting as U-A5; no dynamic resolution | Mobile | 设置菜单 + 游戏视觉 | none |
| U-A8 | MetalFX temporal for handheld (and a Mac option) below 100 % | gain over FSR 1 at low internal resolution (DF on RE Village, secondary [U34]) | unknown | not available in 6000.6 [U-L8, U15]; needs Unity 6.7 or a plugin [U15, U33] | Mobile (Mac option later) | Red (engine version) + 游戏视觉 | none |
| U-A9 | Depth Priming = Auto on the desktop renderer | no visible change (measured within noise in the three corridor views [U-M1]; maze not measured) | Windows IMR GPUs: fewer shaded hidden fragments in the deep maze, no extra draws (the prepass is already there); Mac: neutral (HSR [U10]); mobile / WebGL: Auto keeps it off [U-L3] | alpha-tested and cross-faded materials must match depth in the prepass; byte identity not proven | Desktop (Windows gain) | 游戏视觉 | none (within noise; verify bytes) |
| U-A10 | Per-tier URP assets: desktop asset untouched; handheld tier asset(s) with their own renderer (U-A1, U-A3) and a bloom/AO volume; a WebGL tier asset; chosen at boot | no change on desktop; handheld/WebGL as their rows | removes runtime reflection; puts tier variants in the right builds; lets the handheld use Volume Update "Via Scripting" if wanted [U11] | pipeline rebuild on swap (only at boot / loading cover) [U23]; prefilter interplay of two assets untested [U-L7] | all (desktop file untouched) | 设置菜单 + 游戏视觉 | none |
| U-A11 | iOS thermal response: enable `adjustIOSFPSUsingThermalState` [U27] and/or read `thermalState` [U26] to step to a fixed lower preset, never oscillate [U28] | under heat: a steady lower frame rate or a softer preset instead of stutter | avoids thermal throttling cliffs (no number in any source read) | a feedback loop if done per frame [U28] | Mobile | 设置菜单 | none |
| U-A12 | Keep the main pass unbroken on Metal (guard rule): any new effect that samples colour/depth goes after transparents or uses framebuffer input; check in the Render Graph Viewer [U6] | no visible change | up to ~1 ms per avoided split at 1440p (one project measurement [U-P1]) | none | all | 游戏视觉 | none |
| U-A13 | Rejected for now: Tile-Only Mode + On-Tile Post | would drop bloom, chromatic aberration, lens distortion, MSAA, HDR [U6, U7] | bandwidth on tile GPUs (not quantified by Unity [U7]) | the look | (none) | — | n/a |

### Row notes: how to build it, what to measure in the clone

**U-A1.** Add a second `UniversalRendererData` (for example `Assets/Settings/FrontRooms_URP_Renderer_Handheld.asset`) that copies the desktop renderer but sets SSAO Source = Depth, After Opaque on, Normal Quality High; the glass RT entry can stay (it is compiled out on iOS). Reference it from a handheld URP asset (U-A10) so the iOS build compiles the Depth-source and after-opaque variants. Measure in the clone, in an iOS build or in the Editor on the handheld path: draw calls and SetPass in `maze_heavy / maze_median / maze_lights` and the three corridor views (the probe views of [U-M1]); the frame in the Render Graph Viewer (prepass gone? opaque and transparent still merged?); the image change against the current handheld look, with Red's eye on over-darkening; GPU ms in an Xcode Metal capture on the iPhone (batch mode has no GPU timers [U-M1]).

**U-A2.** One line in `FrontRoomsQuality.SetDetailsFrom`: on handheld, `AmbientOcclusion = 0` at HIGH as well (the downsample write already exists). Better: Downsample on in the handheld renderer of U-A1, which removes the reflection path. Measure: SSAO pass GPU ms full vs half on the device; image change at furniture feet and wall bases.

**U-A3.** Settings on the handheld renderer (blur Medium, Samples Medium). Measure: the build's stripping report confirms the Medium-sample variants are in the iOS build; GPU ms of the AO + blur passes; noise in motion.

**U-A4.** A handheld bloom override on the existing quality volume (`FrontRoomsQuality.SetBloomLow` already creates a priority-100 profile) or a tier profile in `Assets/Resources/Rendering/`: downscale Quarter, maxIterations 5, highQualityFiltering off. Measure: bloom pass count and GPU ms in a Metal capture; side-by-side stills of a dark corridor with one troffer and of a lit Run zone; flicker of thin tubes in motion.

**U-A5.** Menu entry under ANTI-ALIASING (设置菜单): TEMPORAL sets `upscalingFilter = STP` at scale 1.0 and MSAA 1 through `FrontRoomsQuality.Apply`; ULTRA's default stays MSAA 4×. Pre-condition U-A6. Measure: the probe's `stp` variant again plus GPU ms in a non-batch Editor or an Xcode capture of a Mac player; motion clips of a swinging door, the held item, a flickering lamp and a dying lamp; the glass tracer's reflections under jitter.

**U-A6.** In `Assets/Resources/Rendering/FrontRoomsSurface.shader` (and any other custom opaque shader used on moving objects) add the fallback `UsePass` named in the URP CHANGELOG [U-L3]. With MSAA the motion-vector pass does not run (TAA is off), so desktop ULTRA is unchanged. If the fallback pass breaks SRP Batcher compatibility (check the shader Inspector), write the shader's own MotionVectors pass with its existing per-material buffer instead. Measure: the Rendering Debugger motion-vector view with TAA on; a moving door and debris must show object motion; SetPass count unchanged on desktop ULTRA; desktop ULTRA frames byte-identical.

**U-A7.** In `FrontRoomsQuality.Apply`, when handheld AUTO resolves below 0.999, offer STP as a third UPSCALING choice before making it the default. Measure on device at the Duo inner size: total GPU ms of FSR 1 + MSAA 4× against STP; stills and motion; history reset on fold / unfold.

**U-A8.** No work in 6000.6. When the project moves to 6.7 LTS, evaluate the SRP Core MetalFX upscaler [U15] against STP on iPhone and Mac. A plugin [U33] would need its own camera-target hand-off (October forum reports [U-L7]).

**U-A9.** `m_DepthPrimingMode: 1` (Auto) in `Assets/Settings/FrontRooms_URP_Renderer.asset`. Measure: byte-level diff of the probe frames (desktop rule); GPU ms on a Windows D3D12 / Vulkan GPU in the maze views; holes or z-fighting on alpha-clipped and cross-faded materials.

**U-A10.** Quality levels in `ProjectSettings/QualitySettings.asset`: desktop levels keep `FrontRooms_URP.asset`; new handheld levels reference a handheld asset; a WebGL level references a WebGL asset; platform exclusions per level so each build carries only its assets. `FrontRoomsQuality` picks the level at boot and keeps its per-detail writes on top. Measure: build size and variant counts per platform (stripping report), the hitch of `SetQualityLevel` on device, desktop frames byte-identical.

**U-A11.** Turn on the 6000.6 player setting (iOS Player Settings) and set its targets, or add a small iOS plugin that returns `ProcessInfo.thermalState` and map serious → a fixed lower preset, critical → 30 fps LOW. Measure: a 20-minute session on device with Xcode's thermal state recorder; frame-time stability before and after.

**U-A12.** Review rule for 游戏视觉: before landing a pass, open the Render Graph Viewer on Metal and confirm the opaque + transparent native pass stays merged and the MSAA resolve is not reloaded. Measure: the pass list per change.

**U-A13.** Revisit only if a battery tier without bloom is ever wanted.

## Sources

| id | what | URL | how read | notes |
|---|---|---|---|---|
| U-L1 | FrontRooms post profiles and stack: `Assets/Resources/Rendering/FrontRoomsPost.asset`, `FrontRoomsPost_Office.asset`, `FrontRoomsPost_Run_B.asset`, `Assets/Scripts/Rendering/FrontRoomsPostStack.cs`; shader pass lists of `FrontRoomsSurface.shader`, `FrontRoomsGlass.shader`, `FrontRoomsScreenVideo.shader`, `RunBeamCone.shader` | local, main | read (read-only) | 2026-11-04 |
| U-L2 | `Assets/Settings/FrontRooms_URP.asset`, `FrontRooms_URP_Renderer.asset`, `ProjectSettings/QualitySettings.asset`, `GraphicsSettings.asset`, `ProjectSettings.asset`, `Packages/manifest.json` | local, main | read (read-only) | 2026-11-04 |
| U-L3 | URP 17.6.0 and SRP Core 17.6.0 source: `Library/PackageCache/com.unity.render-pipelines.universal@327d93d1cbc8`, `...core@02d75770e4b3`, and `/Applications/Unity/Hub/Editor/6000.6.3f1/Unity.app/Contents/Resources/PackageManager/BuiltInPackages/com.unity.render-pipelines.{core,universal}/` (Runtime/Upscaling, Runtime/STP, Runtime/PostProcess.cs, Runtime/UniversalRendererRenderGraph.cs, Runtime/UniversalRenderer.cs, Runtime/Passes/PostProcess/*, Runtime/RendererFeatures/*, Shaders/PostProcessing/FinalPost.shader, CHANGELOG.md); compiled `Library/ScriptAssemblies/Unity.RenderPipelines.*.Runtime.dll` | local, internal source | read / grep (read-only) | file names inline |
| U-L4 | `Assets/Scripts/FrontRoomsQuality.cs` | local, main | read (read-only) | presets as built |
| U-L5 | `Assets/Scripts/Rendering/FrontRoomsMetalGlassRTRendererFeature.cs`, `Rendering/Glass/FrontRoomsGlassSceneColor.cs`, `Rendering/HeldItem/FrontRoomsHeldItem.cs` | local, main | read heads (read-only) | |
| U-L6 | `Frontrooms3D/Documentation/research/webgl/03_measured_budgets.md` | local | read (head, §0, SSAO rows of §2) | M3 Max Editor, Metal, 1920×1080, 2026-10-02/03 (before the glass tracer); WebGL-tier report |
| U-L7 | `Frontrooms3D/Documentation/research/interaction/settings/reports/R4_unity_runtime.md` (Q1-Q4, Q7.4, Q8, Q9) and `README.md` §2 | local | read parts | 6000.3 / URP 17.3 era, 2026-10-24 |
| U-L8 | Installed 6000.6.3f1 players: `PlaybackEngines/iOSSupport/Trampoline/Frameworks/UnityRuntime/UnityRuntime.framework/UnityRuntime`, `PlaybackEngines/MacStandaloneSupport/Variations/macos_arm64_player_nondevelopment_mono/UnityPlayer.app/Contents/Frameworks/UnityPlayer.dylib` | local | `grep -c -i metalfx`, `otool -L` (read-only) | 0 hits; no MetalFX.framework link |
| U-L9 | `Frontrooms3D/Documentation/research/interaction/settings/reports/R2_mobile.md` §B3 (WWDC25 session 211 notes) | local | grep | via the October settings research |
| U-M1 | `/Users/redwang/FrontRoomsVisualWork/tech_art/MEASURED.md` (probe run 2, clone proj_build at main bc9cf07c, 6000.6.3f1 batch Play, Metal, M3 Max, 1920×1080, desktop ULTRA) | local | read | counts exact, times invalid (load); image change against a noise floor of 0.00-0.07 % |
| U-P1 | `Frontrooms3D/Documentation/research/glass/rt/20_implementation.md` (run 8 2026-10-04: split 1.07 ms at 1440p; B6 crawl 8.67 %) | local | grep | project measurement, 6000.3.10f1 |
| U-P2 | `Frontrooms3D/Documentation/research/interaction/settings/README.md` §1.3 (probe v3, 2026-10-24) | local | read | project measurement, M3 Max, 6000.3.10f1 (context only) |
| U1 | Unity 6.6 Manual, "Introduction to Spatial-Temporal Post-processing" | https://docs.unity.com/en-us/engine/6000.6/manual/cameras/urp/change-resolution-scale-urp/stp-upscaler | read | 6000.6 |
| U2 | Unity 6.6 Manual, "Enable Spatial-Temporal Post-processing" | https://docs.unity.com/en-us/engine/6000.6/manual/cameras/urp/change-resolution-scale-urp/stp-enable | read | 6000.6 |
| U3 | Unity 6.6 Manual, "Add anti-aliasing in the Universal Render Pipeline" | https://docs.unity.com/en-us/engine/6000.6/manual/render-pipelines/universal-render-pipeline/anti-aliasing | read | 6000.6 |
| U4 | Unity 6.6 Manual, "SSAO Renderer Feature reference" | https://docs.unity.com/en-us/engine/6000.6/manual/lighting-overview/lighting/shadows-in-urp/post-processing-ssao/ssao-renderer-feature-reference | read | 6000.6 |
| U5 | Unity 6.6 Manual, "Bloom Volume Override reference for URP" | https://docs.unity.com/en-us/engine/6000.6/manual/post-processing-and-full-screen-effects/urp/effect-list/bloom.md | read | 6000.6, updated 2026-09-14 |
| U6 | Unity 6.6 Manual, "On-tile rendering in URP" | https://docs.unity.com/en-us/engine/6000.6/manual/analysis/graphics-performance-profiling/in-urp/on-tile-rendering | read | 6000.6 |
| U7 | Unity 6.5 Manual, "On-tile post-processing in URP" | https://docs.unity.com/en-us/engine/6000.5/manual/post-processing-and-full-screen-effects/urp/on-tile-post-processing | read | 6000.5 page; feature present in the 17.6 source |
| U8 | Apple, "Tailor your apps for Apple GPUs and tile-based deferred rendering" | https://developer.apple.com/documentation/metal/tailor-your-apps-for-apple-gpus-and-tile-based-deferred-rendering | read (.md) | TBDR, tile memory, MSAA |
| U9 | Apple, "Choosing a resource storage mode for Apple GPUs" | https://developer.apple.com/documentation/metal/choosing-a-resource-storage-mode-for-apple-gpus | read (.md) | memoryless |
| U10 | Apple WWDC20 session 10632, "Optimize Metal Performance for Apple silicon Macs" | https://developer.apple.com/videos/play/wwdc2020/10632/ | read (transcript) | load/store, MSAA resolve, HSR vs depth prepass |
| U11 | Unity 6.6 Manual, "Configure for better performance" (URP) | https://docs.unity.com/en-us/engine/6000.6/manual/render-pipelines/universal-render-pipeline/introduction/configure-for-better-performance | read | 6000.6; its NRP advice is outdated against the 17.6 source |
| U12 | Unity 6.6 Manual, "Introduction to changing resolution scale" | https://docs.unity.com/en-us/engine/6000.6/manual/cameras/resolution-scale/introduction | read | upscaler table per pipeline |
| U13 | Intel GameTechDev, XeGTAO README (2021) | https://github.com/GameTechDev/XeGTAO | read | AO cost per GPU; depth + optional normals; half-res advice |
| U14 | Jimenez, Wu, Pesce, Jarabo, "Practical Realtime Strategies for Accurate Indirect Occlusion" (Activision, SIGGRAPH 2016 course, ATVI-TR-16-01) | https://research.activision.com/publications/archives/practical-real-time-strategies-for-accurate-indirect-occlusion | read (abstract); [search] snippet for "0.5 ms on a PS4 at 1080p" | PDF not opened |
| U15 | Unity Script Reference, `UnityEngine.MetalFXModule` (Unity 6.7 Beta, 6000.7; also 7000.0 alpha) | https://docs.unity3d.com/6000.7/Documentation/ScriptReference/UnityEngine.MetalFXModule.html | read; the 6000.6 URL of the same page returns 404 | MetalFX arrives with 6.7 |
| U16 | Tuanjie (Unity China) Scripting API, `RenderTexture.metalFXOutput` | https://docs.unity.cn/cn/tuanjiemanual/ScriptReference/RenderTexture-metalFXOutput.html | read | Tuanjie 1.x manual, not Unity 6000.x |
| U17 | Apple WWDC22 session 10103, "Boost performance with MetalFX Upscaling" | https://developer.apple.com/videos/play/wwdc2022/10103/ | read (transcript) | inputs, placement, quality |
| U18 | Apple, MetalFX framework documentation | https://developer.apple.com/documentation/metalfx | read (.md) | iOS 16 / macOS 13; scalers, frame interpolator |
| U19 | Jimenez, Echevarria, Sousa, Gutierrez, "SMAA: Enhanced Subpixel Morphological Antialiasing", Eurographics 2012 (CGF 31(2)) | https://www.iryoku.com/smaa/ | read (project page) | no timings on the page |
| U20 | Unity 6.6 Manual, "Camera component reference" (URP) | https://docs.unity.com/en-us/engine/6000.6/manual/cameras/urp/camera-components-reference/camera-component-reference | read | TAA settings and trade-offs |
| U21 | Unity 6.6 Manual, "Introduction to Dynamic Resolution" | https://docs.unity.com/en-us/engine/6000.6/manual/cameras/resolution-scale/dynamic-resolution/introduction.md | read | platforms, no reallocation; updated 2026-10-30 |
| U22 | AMD GPUOpen, "AMD FidelityFX Super Resolution 1" | https://gpuopen.com/fidelityfx-superresolution/ | read | modes, cost, placement |
| U23 | Unity Manual, "Control URP Quality settings through code" (Unity 6.2 / URP 17.0 pages) | https://docs.unity3d.com/6000.2/Documentation/Manual/urp/quality/quality-settings-through-code.html | [search] snippet; the 6.3 page read via U-L7 | 6000.6 page not found by search |
| U24 | Unity 6.5 Manual, "Adaptive Performance Apple provider" | https://docs.unity3d.com/Manual/com.unity.adaptiveperformance.apple.html | [search] snippet | iOS, tvOS, visionOS |
| U25 | Unity 6.6 Manual, "Adaptive Performance project settings reference" | https://docs.unity.com/en-us/engine/6000.6/manual/unity-editor/editor-settings-reference/comp-manager-group/adaptive-performance-project-settings.md | read | built in; Basic + Simulator; frame timing needed in release |
| U26 | Apple, `ProcessInfo.ThermalState` | https://developer.apple.com/documentation/foundation/processinfo/thermalstate-swift.enum | read (.md) | four states |
| U27 | Unity 6.6 Script Reference, `PlayerSettings.adjustIOSFPSUsingThermalState` | https://docs.unity.com/en-us/engine/6000.6/script-reference/unityeditor/playersettings/adjustiosfpsusingthermalstate | read | Serious / Critical |
| U28 | Apple, "Optimizing iPhone and iPad apps to support ProMotion displays" | https://developer.apple.com/documentation/quartzcore/optimizing-iphone-and-ipad-apps-to-support-promotion-displays | read (.md) | rates, 30/60 priority, no feedback loops |
| U29 | SIGGRAPH 2014 "Advances in Real-Time Rendering" course page: Karis (Epic) "High-Quality Temporal Supersampling"; Jimenez (Sledgehammer) "Next Generation Post Processing in Call of Duty: Advanced Warfare" | https://advances.realtimerendering.com/s2014/ | read (abstracts only) | slides are pptx, not opened |
| U30 | Bjørge (ARM), "Bandwidth-Efficient Rendering", SIGGRAPH 2015 (Moving Mobile Graphics course) | https://community.arm.com/cfs-file/__key/communityserver-blogs-components-weblogfiles/00-00-00-20-66/siggraph2015_2D00_mmg_2D00_marius_2D00_slides.pdf | [search] snippet only | PDF not opened |
| U31 | Pedersen (Playdead), "Temporal Reprojection Anti-Aliasing in INSIDE", GDC 2016 | https://www.gdcvault.com/play/1022970/Temporal-Reprojection-Anti-Aliasing-in | read (session page) | talk itself gated |
| U32 | Unity Discussions, "When is DLSS going to be native in URP?" (post by ElaineK, 2026-05-26, Unity staff: Discourse group `unity_staff`) | https://discussions.unity.com/t/when-is-dlss-going-to-be-native-in-urp/1721131 | read | forum: pointer only, consistent with U-L3 |
| U33 | Unity Asset Store, "MetalFX - Upscaling for Unity" (The Naked Dev), v1.0.6, 2026-10-23 | https://assetstore.unity.com/packages/tools/utilities/metalfx-upscaling-for-unity-388280 | read | third party; listed for 2022.3.62f3 |
| U34 | Digital Foundry on Resident Evil Village for Mac (2022), via Geeky Gadgets and Macwelt summaries | https://www.geeky-gadgets.com/resident-evil-village-mac-14-11-2022/ ; https://www.macwelt.de/article/1379277/resident-evil-village-fur-mac-apples-metalfx-uberzeugt.html | [search] snippet only, secondary | DF page not reachable |
| U35 | Digital Foundry on Assassin's Creed Mirage for iPhone 15 Pro (2024), via shazoo.ru / ichip.ru summaries | https://shazoo.ru/2024/07/11/158528/sliskom-ambiciozno-analiz-assassins-creed-mirage-na-ios-ot-digital-foundry | [search] snippet only, secondary | DF page not reachable |
| U36 | Digital Foundry (Oliver Mackenzie) on Death Stranding Director's Cut for Mac and iPhone 15 Pro (2024), via shazoo.ru | https://shazoo.ru/2024/02/08/153356/xoroso-na-mac-sredne-na-iphone-15-pro-razbor-death-stranding-ot-digital-foundry | [search] snippet only, secondary | DF page not reachable |
| U37 | Unity, "Mobile optimization tips for technical artists, Part II" | https://unity.com/how-to/mobile-game-optimization-tips-part-2 | read | general advice; no version named |
| U38 | Unity blog, "Get hundreds of tips from new Unity 6 optimization guides" (2024-11-11) | https://unity.com/blog/unity-6-game-optimization-guides | read | names the Unity 6 e-books; no specifics |
| U39 | Unity e-books, "Optimize your game performance for mobile, XR, and the web in Unity (Unity 6 edition)" and "Introduction to URP for advanced creators (Unity 6 edition)" | https://unity.com/resources/mobile-xr-web-game-performance-optimization-unity-6 ; https://unity.com/resources/introduction-to-urp-advanced-creators-unity-6 | [search] landing pages only | gated PDFs, not opened (no-download rule) |
| U40 | Unity 6.6 Manual, "Compare WebGPU and WebGL2" | https://docs.unity.com/en-us/engine/6000.6/manual/platform-specific/webgl/develop/graphics/web-graphics-apis-intro.md | read | WebGL2: compute "Not natively supported" |
