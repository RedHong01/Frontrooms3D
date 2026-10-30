using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if !(UNITY_WEBGL && !UNITY_EDITOR)
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RendererUtils;
#endif

/// <summary>
/// V3 HeldItem (D1.6, 10_design §7; VISUAL_CHAT_TASKS D1.6 row): a held key never clips into a wall and the
/// EXAMINE depth of field still focuses on it.
/// - Layer 30 (unnamed; 31 is the editor tools' scratch layer). FrontRoomsKeyAssembly moves its part renderers here
///   once the held root is closer to the view than 1.5 x KeyPickup.HeldDistance, and back on Release.
/// - While at least one renderer is registered, one pass is injected into the view camera's URP renderer from
///   RenderPipelineManager.beginCameraRendering (no renderer asset, layer mask or TagManager change), at
///   AfterRenderingOpaques: (1) depth punch: the layer-30 renderers with Hidden/FrontRooms/HeldItemDepthPunch
///   (ColorMask 0, ZTest Always, ZWrite On, depth = the far plane, reversed-Z aware); (2) redraw: the same renderers
///   with their own forward passes, ZTest LEqual, opaque sorting. The opaque pass has already drawn them where they were
///   in front, so the result is the item over everything with correct self-occlusion, and its true depth is in the
///   attachment the after-transparents depth copy reads (DOF focus). Nothing registered: no pass, no cost.
/// - SSAO (fix 1, verify r1 S7): the redraw runs with URP's _AmbientOcclusionParam at zero (ssao = 1, no AO), then puts
///   the frame's value back, so the AO the background had where the item now covers it (a wall in front of a held key)
///   never darkens the item. The opaque pass's own draw of the item (where it was in front) keeps its normal AO.
/// - The held fill (fix 3, verify r2 look L1): while a holder on the layer is active (a key in the hand, up to the HUD
///   hand-off, when the presenter hides it), a weak unshadowed point light sits at the view camera (HeldFillIntensity,
///   range HeldFillRange 0.6 m, warm white: the V5 shot light's recipe at a fraction of its 0.6). The held key's satin brass
///   faces the lens, so at a level look it reflects the room behind the player and read 0.070 linear (dark brown); the fill
///   lifts it over 0.10 and within 1.5 stops of what is behind it (research/interactables/d16/data/fix3/l1/). URP Forward+
///   has no per-light layer mask and the asset's Light Layers are off, so the range is the limit: only what is within
///   0.6 m of the lens is lit, nothing on the floor or the walls of a room at a normal stance. Put on and moved from
///   beginCameraRendering (before the camera's culling), off with the last holder; destroyed on Application.quitting.
/// - WebGL: compiled out (#if UNITY_WEBGL && !UNITY_EDITOR): Supported is false, no layer switch, no pass, no fill (every
///   quality level shares one URP asset, so the gate is in code). iOS keeps the pass (3-4 draws) and the fill.
/// </summary>
public static class FrontRoomsHeldItem
{
    public const int Layer = 30;
    public const string DepthPunchShaderName = "Hidden/FrontRooms/HeldItemDepthPunch";

#if UNITY_WEBGL && !UNITY_EDITOR
    public static readonly bool Supported = false;
#else
    public static readonly bool Supported = true;
#endif

    static readonly List<Object> holders = new List<Object>();
    static Camera cachedCamera;
    static Camera[] cameraBuffer = new Camera[8];
    static int cachedFrame = -1;

    /// <summary>How many holders have renderers on the layer now.</summary>
    public static int Count => holders.Count;

    /// <summary>The held fill's intensity (shipped value; tools and captures may change it) and range (m).</summary>
    public static float HeldFillIntensity = .05f;
    public const float HeldFillRange = .6f;
    public static readonly Color HeldFillColor = new Color(1f, .95f, .88f);

    /// <summary>True while the held fill is lit (tests, captures).</summary>
    public static bool FillOn
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return false;
#else
            return fill != null && fill.enabled;
#endif
        }
    }

    // An active holder: a registered renderer owner whose GameObject is in the hierarchy (the presenter hides the held key
    // at the HUD hand-off; Release unregisters it later, at the pocket's end).
    static bool AnyActiveHolder()
    {
        for (var i = 0; i < holders.Count; i++)
        {
            var c = holders[i] as Component;
            if (c != null && c.gameObject.activeInHierarchy) return true;
        }
        return false;
    }

    /// <summary>The game's view camera: enabled, no target texture, the highest depth (re-validated once per frame).</summary>
    public static Camera ViewCamera
    {
        get
        {
            if (cachedFrame == Time.frameCount && cachedCamera != null && cachedCamera.isActiveAndEnabled) return cachedCamera;
            cachedFrame = Time.frameCount;
            cachedCamera = null;
            var best = float.NegativeInfinity;
            var n = Camera.allCamerasCount;
            if (cameraBuffer.Length < n) cameraBuffer = new Camera[Mathf.Max(n, cameraBuffer.Length * 2)];
            n = Camera.GetAllCameras(cameraBuffer);
            for (var i = 0; i < n; i++)
            {
                var c = cameraBuffer[i];
                if (c == null || !c.isActiveAndEnabled || c.targetTexture != null || c.cameraType != CameraType.Game) continue;
                if (c.depth > best) { best = c.depth; cachedCamera = c; }
            }
            return cachedCamera;
        }
    }

    public static void Register(Object holder)
    {
        if (!Supported || holder == null || holders.Contains(holder)) return;
        holders.Add(holder);
        Hook(true);
    }

    public static void Unregister(Object holder)
    {
        holders.Remove(holder);
        for (var i = holders.Count - 1; i >= 0; i--) if (holders[i] == null) holders.RemoveAt(i);
        if (holders.Count == 0) Hook(false);
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    static void Hook(bool on) { }
    public static void SetFill(Camera camera) { }
#else
    static bool hooked;
    static HeldItemPass pass;
    static Light fill;

    static void Hook(bool on)
    {
        if (on == hooked) return;
        hooked = on;
        if (on) RenderPipelineManager.beginCameraRendering += OnBeginCamera;
        else RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
        if (!on) SetFill(null);
    }

    /// <summary>
    /// The held fill on a camera: lit at the lens while an active holder is on the layer, else off (null: off). The game's
    /// view camera gets it from beginCameraRendering; edit-mode captures (a target texture) call it for their own camera.
    /// </summary>
    public static void SetFill(Camera camera)
    {
        if (camera == null || !AnyActiveHolder())
        {
            if (fill != null && fill.enabled) fill.enabled = false;
            return;
        }
        if (fill == null)
        {
            var go = new GameObject("Held fill (HeldItem)") { hideFlags = HideFlags.DontSave };
            fill = go.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.shadows = LightShadows.None;
            fill.range = HeldFillRange;
            fill.color = HeldFillColor;
            fill.renderMode = LightRenderMode.ForcePixel;
            Application.quitting -= TakeDownFill;
            Application.quitting += TakeDownFill;
        }
        if (fill.transform.parent != camera.transform) fill.transform.SetParent(camera.transform, false);
        fill.transform.localPosition = Vector3.zero;
        fill.intensity = HeldFillIntensity;
        if (!fill.enabled) fill.enabled = true;
    }

    // DontSave objects outlive Play Mode in the Editor: take the fill down with the session.
    static void TakeDownFill()
    {
        if (fill != null)
        {
            if (Application.isPlaying) Object.Destroy(fill.gameObject);
            else Object.DestroyImmediate(fill.gameObject);
        }
        fill = null;
    }

    static void OnBeginCamera(ScriptableRenderContext context, Camera camera)
    {
        if (holders.Count == 0 || camera == null || camera.cameraType != CameraType.Game || camera.targetTexture != null) return;
        if (camera == ViewCamera) SetFill(camera);
        if ((camera.cullingMask & (1 << Layer)) == 0) return;
        var data = camera.GetUniversalAdditionalCameraData();
        if (data == null || data.renderType != CameraRenderType.Base) return;
        var renderer = data.scriptableRenderer;
        if (renderer == null) return;
        if (pass == null)
        {
            var shader = Shader.Find(DepthPunchShaderName);
            if (shader == null) return;
            pass = new HeldItemPass(CoreUtils.CreateEngineMaterial(shader)) { renderPassEvent = RenderPassEvent.AfterRenderingOpaques };
        }
        renderer.EnqueuePass(pass);
    }

    sealed class HeldItemPass : ScriptableRenderPass
    {
        readonly Material punch;

        static class Tags
        {
            public static readonly ShaderTagId[] Forward = { new ShaderTagId("UniversalForward"), new ShaderTagId("UniversalForwardOnly"), new ShaderTagId("SRPDefaultUnlit") };
            public static readonly List<ShaderTagId> ForwardList = new List<ShaderTagId>(Forward);
        }

        sealed class PassData
        {
            public RendererListHandle punch, draw;
            public Vector4 aoRestore;
        }

        static readonly int AoParam = Shader.PropertyToID("_AmbientOcclusionParam");
        // URP's SSAO pass value for this renderer (Intensity 1, DirectLightingStrength 0.35), used only if the global is unset.
        static readonly Vector4 AoFallback = new Vector4(1f, 0f, 0f, .35f);

        public HeldItemPass(Material punchMaterial)
        {
            punch = punchMaterial;
            profilingSampler = new ProfilingSampler("FR HeldItem");
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var lightData = frameData.Get<UniversalLightData>();
            if (!resources.activeColorTexture.IsValid() || !resources.activeDepthTexture.IsValid()) return;
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("FR HeldItem", out var data, profilingSampler))
            {
                builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.ReadWrite);
                var punchDesc = new RendererListDesc(Tags.Forward, renderingData.cullResults, cameraData.camera)
                {
                    layerMask = 1 << Layer,
                    renderQueueRange = RenderQueueRange.opaque,
                    sortingCriteria = SortingCriteria.CommonOpaque,
                    overrideMaterial = punch,
                    overrideMaterialPassIndex = 0,
                };
                data.punch = renderGraph.CreateRendererList(punchDesc);
                builder.UseRendererList(data.punch);
                var drawSettings = RenderingUtils.CreateDrawingSettings(Tags.ForwardList, renderingData, cameraData, lightData, SortingCriteria.CommonOpaque);
                var filter = new FilteringSettings(RenderQueueRange.opaque, 1 << Layer);
                var drawParams = new RendererListParams(renderingData.cullResults, drawSettings, filter);
                data.draw = renderGraph.CreateRendererList(drawParams);
                builder.UseRendererList(data.draw);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                var ao = Shader.GetGlobalVector(AoParam);
                data.aoRestore = ao == Vector4.zero ? AoFallback : ao;
                builder.SetRenderFunc((PassData d, RasterGraphContext ctx) =>
                {
                    ctx.cmd.DrawRendererList(d.punch);
                    ctx.cmd.SetGlobalVector(AoParam, Vector4.zero);
                    ctx.cmd.DrawRendererList(d.draw);
                    ctx.cmd.SetGlobalVector(AoParam, d.aoRestore);
                });
            }
        }
    }
#endif
}
