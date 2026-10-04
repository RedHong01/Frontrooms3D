// Desktop ray-traced glass reflections (G14 P0 + P1), URP RenderGraph side (design 10 §1.2).
// Same class name and script guid (8c7c221f…) as ChatGPT's prototype, so the renderer asset keeps its entry.
//
// Frame order for an opted-in game camera (FrontRoomsGlassRTCamera) on macOS / Metal, quality != Off:
//   BeforeRenderingTransparents
//     1. FRGlassRT Prepass (raster): the RECEIVING glass only (rendering-layer bit 30: FrontRooms/Glass's own
//        FRGlassRTPrepass pass; bit 29: Hidden/FrontRooms/GlassRTPrepass for other materials) into GlassDepth
//        (R32F linear eye depth) + GlassNormal (RGBA16F world normal, smoothness), a transient 1x D32 keeps the
//        front-most layer, and a manual test against _CameraDepthTexture keeps only glass that is visible.
//     2. FRGlassRT Trace (unsafe): IssuePluginEventAndData -> the plugin encodes into Unity's current command
//        buffer: BLAS/TLAS, one mirror ray per glass pixel, hit shading, resolve -> _FR_GlassRTReflection.
//        Sets _FR_GlassRTWeight = 1 and the global keyword _FR_GLASS_RT for this camera.
//   Transparents: FrontRooms/Glass swaps its environment term for the traced radiance (its own Fresnel, grime, fog).
//   AfterRenderingTransparents
//     3. FRGlassRT End (unsafe): keyword off, weight 0, so no other camera samples a stale texture.
//   Post: reflections are tonemapped, graded and bloomed with the scene. (The prototype's after-post composite is gone.)
//
// Platform isolation (design §2.5): the class compiles everywhere so the renderer asset never has a missing script,
// but Create/AddRenderPasses are empty unless UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX, and at runtime they also
// require Metal. WebGL / Windows players contain no pass, no P/Invoke and no plugin call.
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RendererUtils;
#endif

public sealed class FrontRoomsMetalGlassRTRendererFeature : ScriptableRendererFeature
{
    [Tooltip("Hidden/FrontRooms/GlassRTPrepass: draws receivers whose material has no FRGlassRTPrepass pass. Serialized so player builds keep it (R21).")]
    [SerializeField] Shader prepassShader;

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    TracePass trace;
    EndPass end;

    public override void Create()
    {
        if (prepassShader == null) prepassShader = Shader.Find("Hidden/FrontRooms/GlassRTPrepass");
        trace = new TracePass(prepassShader) { renderPassEvent = RenderPassEvent.BeforeRenderingTransparents };
        end = new EndPass { renderPassEvent = RenderPassEvent.AfterRenderingTransparents };
        trace.ConfigureInput(ScriptableRenderPassInput.Depth);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (trace == null || SystemInfo.graphicsDeviceType != GraphicsDeviceType.Metal || !Application.isPlaying) return;
        var cam = renderingData.cameraData.camera;
        if (cam == null || renderingData.cameraData.cameraType != CameraType.Game || renderingData.cameraData.renderType != CameraRenderType.Base) return;
        if (!cam.TryGetComponent<FrontRoomsGlassRTCamera>(out var state) || state.id < 0) return;
        if (FrontRoomsGlassRT.Quality == FrontRoomsGlassRTQuality.Off) return;
        trace.State = state;
        renderer.EnqueuePass(trace);
        renderer.EnqueuePass(end);
    }

    protected override void Dispose(bool disposing) { }

    // ------------------------------------------------------------------ passes
    internal static readonly int ReflectionId = Shader.PropertyToID("_FR_GlassRTReflection");
    internal static readonly int WeightId = Shader.PropertyToID("_FR_GlassRTWeight");
    internal static readonly int FadeId = Shader.PropertyToID("_FR_GlassRTFade");
    internal static readonly int RollScaleId = Shader.PropertyToID("_FR_GlassRTRollScale");
    internal static readonly int SceneDepthId = Shader.PropertyToID("_FRGlassRTSceneDepth");
    static GlobalKeyword keyword;
    static bool keywordReady;
    // Created on first use: Unity forbids CreateGlobalKeyword in a ScriptableObject's static/field initialisers.
    internal static GlobalKeyword Keyword
    {
        get
        {
            if (!keywordReady) { keyword = GlobalKeyword.Create("_FR_GLASS_RT"); keywordReady = true; }
            return keyword;
        }
    }
    // Shader tag ids live in their own class: Unity forbids TagToID in a ScriptableObject's static initialiser,
    // and this class is first touched from RecordRenderGraph.
    static class Tags
    {
        public static readonly ShaderTagId Prepass = new ShaderTagId("FRGlassRTPrepass");
        public static readonly ShaderTagId[] Forward = { new ShaderTagId("UniversalForward"), new ShaderTagId("UniversalForwardOnly"), new ShaderTagId("SRPDefaultUnlit") };
    }

    /// <summary>Smudge fade of the glass hook (G-2): the RT pass blurs smudges itself (P1), so RT fades out only at 0.45-0.70.</summary>
    internal static Vector4 Fade = new Vector4(.45f, .70f, 0f, 0f);
    /// <summary>Roll-wave scale while RT is on (G-3): annealed glass has none; 0.02 x 0.075 = 0.0015.</summary>
    internal static float RollScale = .075f;
    /// <summary>Harness: force weight 0 (RT traced but not shown) or skip the prepass (empty-pass cost test).</summary>
    internal static bool HarnessHideResult, HarnessEmptyPassOnly;

    sealed class PrepassData
    {
        public RendererListHandle own, overrideList;
        public TextureHandle sceneDepth;
        public Matrix4x4 view, gpuProj;
    }

    sealed class TraceData
    {
        public IntPtr func;
        public ulong seq;
        public RenderTexture outTex;
        public float weight;
        public Vector4 fade;
        public float rollScale;
        public GlobalKeyword keyword;
    }

    sealed class TracePass : ScriptableRenderPass
    {
        readonly Shader prepass;
        public FrontRoomsGlassRTCamera State;

        public TracePass(Shader prepassShader)
        {
            prepass = prepassShader;
            profilingSampler = new ProfilingSampler("FRGlassRT");
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var state = State;
            if (state == null) return;
            var resources = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var cam = cameraData.camera;
            // The trace and the glass sample the same texture rows only when the camera renders into a texture
            // (the flipped "render into texture" projection). With MSAA, HDR and post that is always true here.
            if (resources.isActiveTargetBackBuffer) return;

            var desc = cameraData.cameraTargetDescriptor;
            int w = Mathf.Max(1, desc.width), h = Mathf.Max(1, desc.height);
            var quality = FrontRoomsGlassRT.Quality;
            if (state.EnsureTargets(w, h, quality == FrontRoomsGlassRTQuality.Ultra, FrontRoomsGlassRTSystem.DebugMode != FrontRoomsGlassRTNative.DebugNone))
                FrontRoomsGlassRTSystem.SendTargets(state);

            var view = cameraData.GetViewMatrix();
            var gpuProj = GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(), true);
            var frame = FrontRoomsGlassRTSystem.BuildFrame(cam, state, w, h, view, gpuProj);
            state.lastSeq = frame.seq;
            state.lastRect = frame.rect;
            state.lastVisibleReceivers = frame.visibleReceivers;
            var weight = frame.traced && !HarnessHideResult && FrontRoomsGlassRTSystem.DebugMode == FrontRoomsGlassRTNative.DebugNone ? 1f : 0f;
            state.lastWeight = weight;

            var depthH = renderGraph.ImportTexture(state.depth);
            var normalH = renderGraph.ImportTexture(state.normal);
            var outH = renderGraph.ImportTexture(state.outTex);

            if (frame.traced && !HarnessEmptyPassOnly)
            {
                using (var builder = renderGraph.AddRasterRenderPass<PrepassData>("FRGlassRT Prepass", out var data, profilingSampler))
                {
                    builder.SetRenderAttachment(depthH, 0, AccessFlags.Write);
                    builder.SetRenderAttachment(normalH, 1, AccessFlags.Write);
                    var dd = new TextureDesc(w, h) { depthBufferBits = DepthBits.Depth32, name = "FRGlassRT prepass depth", clearBuffer = false, msaaSamples = MSAASamples.None };
                    builder.SetRenderAttachmentDepth(renderGraph.CreateTexture(dd), AccessFlags.Write);
                    data.sceneDepth = resources.cameraDepthTexture;
                    if (data.sceneDepth.IsValid()) builder.UseTexture(data.sceneDepth, AccessFlags.Read);
                    var ownDesc = new RendererListDesc(Tags.Prepass, renderingData.cullResults, cam)
                    {
                        renderQueueRange = RenderQueueRange.all,
                        sortingCriteria = SortingCriteria.CommonOpaque,
                        renderingLayerMask = 1u << FrontRoomsGlassRTSystem.PrepassBit,
                    };
                    data.own = renderGraph.CreateRendererList(ownDesc);
                    builder.UseRendererList(data.own);
                    if (prepass != null)
                    {
                        var overrideDesc = new RendererListDesc(Tags.Forward, renderingData.cullResults, cam)
                        {
                            renderQueueRange = RenderQueueRange.all,
                            sortingCriteria = SortingCriteria.CommonOpaque,
                            renderingLayerMask = 1u << FrontRoomsGlassRTSystem.OverrideBit,
                            overrideShader = prepass,
                            overrideShaderPassIndex = 0,
                        };
                        data.overrideList = renderGraph.CreateRendererList(overrideDesc);
                        builder.UseRendererList(data.overrideList);
                    }
                    data.view = view;
                    data.gpuProj = gpuProj;
                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc((PrepassData d, RasterGraphContext ctx) =>
                    {
                        ctx.cmd.ClearRenderTarget(RTClearFlags.All, Color.clear, 1f, 0);
                        if (d.sceneDepth.IsValid()) ctx.cmd.SetGlobalTexture(SceneDepthId, d.sceneDepth);
                        RenderingUtils.SetViewAndProjectionMatrices(ctx.cmd, d.view, d.gpuProj, false);
                        ctx.cmd.DrawRendererList(d.own);
                        if (d.overrideList.IsValid()) ctx.cmd.DrawRendererList(d.overrideList);
                    });
                }
            }

            using (var builder = renderGraph.AddUnsafePass<TraceData>("FRGlassRT Trace", out var data, profilingSampler))
            {
                builder.UseTexture(depthH, AccessFlags.Read);
                builder.UseTexture(normalH, AccessFlags.Read);
                builder.UseTexture(outH, AccessFlags.ReadWrite);
                data.func = FrontRoomsGlassRTSystem.EventFunc;
                data.seq = HarnessEmptyPassOnly ? 0ul : frame.traced ? frame.seq : FrontRoomsGlassRTSystem.Warmup();
                data.outTex = state.outTex.rt;
                data.weight = weight;
                data.fade = Fade;
                data.rollScale = RollScale;
                data.keyword = Keyword;
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc((TraceData d, UnsafeGraphContext ctx) =>
                {
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                    if (d.seq != 0 && d.func != IntPtr.Zero) cmd.IssuePluginEventAndData(d.func, 1, new IntPtr((long)d.seq));
                    cmd.SetGlobalTexture(ReflectionId, d.outTex);
                    cmd.SetGlobalFloat(WeightId, d.weight);
                    cmd.SetGlobalVector(FadeId, d.fade);
                    cmd.SetGlobalFloat(RollScaleId, d.rollScale);
                    cmd.SetKeyword(d.keyword, d.weight > 0f);
                });
            }
        }
    }

    sealed class EndData { public GlobalKeyword keyword; }

    sealed class EndPass : ScriptableRenderPass
    {
        public EndPass() { profilingSampler = new ProfilingSampler("FRGlassRT End"); }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            using (var builder = renderGraph.AddUnsafePass<EndData>("FRGlassRT End", out var data, profilingSampler))
            {
                data.keyword = Keyword;
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc((EndData d, UnsafeGraphContext ctx) =>
                {
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                    cmd.SetKeyword(d.keyword, false);
                    cmd.SetGlobalFloat(WeightId, 0f);
                });
            }
        }
    }
#else
    public override void Create() { }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) { }
#endif
}
