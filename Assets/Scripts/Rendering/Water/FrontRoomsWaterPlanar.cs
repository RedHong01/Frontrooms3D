// The pool family's water: the planar reflection (research/bathroom_rooms/70_look_build.md, "Water surface";
// 17_water.md 2.3, 6.1, 7 item 6). Game visual line.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

/// <summary>
/// The mirror cameras of FrontRooms/Water. One camera per water height: connected water finds one level, so one camera
/// covers a whole flooded level, and the club's pool is one more. Each frame the visible water bodies are grouped by
/// height; up to two heights (Ultra) or one (High) get a planar image, every other water keeps the probe.
///
/// How a planar image is made (17 2.3's recipe): an ordinary camera (no mirrored matrix, so culling stays as it is) at
/// the eye mirrored through the water plane, looking along the mirrored view, with an oblique near plane 5 mm above
/// the water (nothing under the water, a contact line for every leg and pillar standing in it); the frustum widened by
/// the overscan 1.15 so rippled samples near the screen's edge stay inside; no post-processing, no shadows, no AA, HDR
/// (R11G11B10) with a mip chain for the roughness filter; the culling mask is the viewer's minus the water, UI and the
/// first-person held layers (the Relay and everything else stays). It is submitted outside the render loop
/// (Application.onBeforeRender, after every LateUpdate; never from a camera callback, which errors under Render
/// Graph, W2/W3) as a SingleCameraRequest. Its zone look is weighed at the viewer (volume trigger), so the reflection
/// carries the zone's ambient, haze and height fog. The water samples it with the view-projection of the frame it was
/// rendered in, so a High-tier image a frame old still lands on the right place (world-space reprojection).
///
/// Tiers (read only from the settings line's FrontRoomsQuality and the kits' phone rule; this file changes no setting):
///   Ultra   planar at 1/2 of the viewer's pixels (x 1.15 overscan) every frame, up to 2 heights
///   High    planar at 1/3 (1/4 above 1440 rows) every second frame, 1 height
///   Probe   desktop Medium and Low, iOS Low Power Mode, every phone and iPad (FrontRoomsTrofferKit.PhoneTier) and WebGL:
///           no camera; the water shows the box-projected probe and its own lamp reflections
/// The planar uses the renderer named ReflectionRendererName when the pipeline asset lists one (no SSAO, no glass RT:
/// the settings line's contract W/apply/pool_water_contract adds it); until then it renders with the default renderer.
/// Refraction (desktop High and Ultra, every body whose FrontRoomsWaterSurface.refraction is on): the viewer camera asks
/// for URP's opaque texture the way FrontRoomsGlassSceneColor does: a pass that records nothing but declares the input,
/// enqueued from beginCameraRendering for that camera only, while such water is in view; no camera or asset setting
/// changes. _FR_WaterOpaque tells the shader which camera has it.
/// Nothing runs until a water body registers; with none, no camera, texture or callback exists.
/// </summary>
public static class FrontRoomsWaterPlanar
{
    public enum Tier { Probe = 0, High = 1, Ultra = 2 }

    /// <summary>Unity's built-in "Water" layer: water renderers sit on it and the mirror cameras never see it.</summary>
    public const int WaterLayer = 4;
    public const float Overscan = 1.15f;
    /// <summary>The oblique near plane sits this far above the water (m).</summary>
    public const float ClipOffset = .005f;
    /// <summary>Layers the mirror never renders: water, UI, the first-person held glass (28) and held item (30).</summary>
    public const int ExcludedLayers = (1 << WaterLayer) | (1 << 5) | (1 << 28) | (1 << 30);
    public const string ReflectionRendererName = "FrontRoomsWaterReflection_Renderer";

    public static float UltraScale = .5f, HighScale = 1f / 3f, HighScaleAbove1440 = .25f;
    public static float UltraFar = 60f, HighFar = 40f;
    /// <summary>Tools: -1 follows the game's rule, else (int)Tier.</summary>
    public static int ForceTier = -1;
    /// <summary>Tools: the camera to mirror; null = Camera.main, else the on-screen game camera of highest depth.</summary>
    public static Camera Viewer;
    /// <summary>Tools: a renderer index for the mirror cameras (-1 = find ReflectionRendererName, else the default).</summary>
    public static int RendererOverride = -1;

    public static Tier LastTier { get; private set; }
    public static int LastPlanarCount { get; private set; }
    public static Vector2Int LastSize { get; private set; }
    public static string LastRenderer { get; private set; } = "none";

    public static Tier CurrentTier
    {
        get
        {
            if (ForceTier >= 0) return (Tier)Mathf.Clamp(ForceTier, 0, 2);
#if UNITY_WEBGL && !UNITY_EDITOR
            return Tier.Probe;
#else
            if (FrontRoomsTrofferKit.PhoneTier || FrontRoomsQuality.LowPowerActive) return Tier.Probe;
            var p = FrontRoomsQuality.CurrentPreset;
            return p == FrontRoomsQuality.Preset.Ultra ? Tier.Ultra : p == FrontRoomsQuality.Preset.High ? Tier.High : Tier.Probe;
#endif
        }
    }

    sealed class Body
    {
        public Renderer renderer;
        public float y;
        public bool refract;
        public Vector4 boxMin, boxMax, probe;
        public MaterialPropertyBlock mpb;
    }

    sealed class Plane
    {
        public int key;
        public float y;
        public Camera cam;
        public UniversalAdditionalCameraData data;
        public RenderTexture rt;
        public Matrix4x4 vp;
        public int renderedFrame = -10;
        public bool valid;
    }

    static readonly List<Body> bodies = new List<Body>();
    static readonly List<Plane> planes = new List<Plane>();
    static readonly Dictionary<int, float> heightScore = new Dictionary<int, float>();
    static readonly List<int> picked = new List<int>();
    static readonly List<int> keys = new List<int>();
    static readonly Comparison<int> byDistance = ByDistance;
    static readonly UnityEngine.Plane[] frustum = new UnityEngine.Plane[6];
    static readonly Vector3[] quad = new Vector3[4];
    static readonly Vector3[] clipped = new Vector3[8];
    static Camera opaqueCamera;
    static OpaqueRequest opaquePass;
    static bool opaqueHooked;
    static bool hooked;
    static int lastFrame = -1;
    static int firstKey = int.MinValue;

    static readonly int PlanarTexId = Shader.PropertyToID("_FR_PlanarTex");
    static readonly int PlanarVPId = Shader.PropertyToID("_FR_PlanarVP");
    static readonly int PlanarParamsId = Shader.PropertyToID("_FR_PlanarParams");
    static readonly int LampSpecId = Shader.PropertyToID("_FR_WaterLampSpec");
    static readonly int RefractId = Shader.PropertyToID("_FR_WaterRefract");
    static readonly int BoxMinId = Shader.PropertyToID("_FR_WaterBoxMin");
    static readonly int BoxMaxId = Shader.PropertyToID("_FR_WaterBoxMax");
    static readonly int ProbePosId = Shader.PropertyToID("_FR_WaterProbePos");
    static readonly int OpaqueId = Shader.PropertyToID("_FR_WaterOpaque");

    /// <summary>Asks URP for the opaque (and depth) texture of the camera it is enqueued on; records no work of its own.</summary>
    sealed class OpaqueRequest : ScriptableRenderPass
    {
        public OpaqueRequest()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) { }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        ReleaseAll();
        bodies.Clear();
        ForceTier = -1;
        Viewer = null;
        RendererOverride = -1;
        lastFrame = -1;
    }

    // ------------------------------------------------------------------------------------------------ registry
    /// <summary>A water body: its renderer (a flat mesh at height y, on WaterLayer), whether what lies under it is refracted
    /// on desktop, the room's box above it (reflections are projected on it) and where its probe was captured.</summary>
    public static void Register(Renderer renderer, float y, bool refract, Bounds roomBox, Vector3 probePoint)
    {
        if (renderer == null) return;
        Unregister(renderer);
        var b = new Body
        {
            renderer = renderer, y = y, refract = refract, mpb = new MaterialPropertyBlock(),
            boxMin = new Vector4(roomBox.min.x, roomBox.min.y, roomBox.min.z, 1f),
            boxMax = new Vector4(roomBox.max.x, roomBox.max.y, roomBox.max.z, 1f),
            probe = new Vector4(probePoint.x, probePoint.y, probePoint.z, 1f),
        };
        bodies.Add(b);
        Apply(b, null, false);
        if (Application.isPlaying && !hooked)
        {
            Application.onBeforeRender += BeforeRender;
            hooked = true;
        }
    }

    public static void Unregister(Renderer renderer)
    {
        for (var i = bodies.Count - 1; i >= 0; i--)
            if (bodies[i].renderer == renderer || bodies[i].renderer == null)
            {
                if (bodies[i].renderer != null) bodies[i].renderer.SetPropertyBlock(null);
                bodies.RemoveAt(i);
            }
        if (bodies.Count == 0) ReleaseAll();
    }

    public static int BodyCount => bodies.Count;

    /// <summary>Frees every mirror camera and texture and stops asking for the opaque texture.</summary>
    public static void ReleaseAll()
    {
        foreach (var p in planes) Free(p);
        planes.Clear();
        AskOpaque(null, false);
        if (hooked)
        {
            Application.onBeforeRender -= BeforeRender;
            hooked = false;
        }
    }

    static void Free(Plane p)
    {
        if (p.rt != null)
        {
            p.rt.Release();
            Destroy(p.rt);
        }
        if (p.cam != null) Destroy(p.cam.gameObject);
        p.rt = null;
        p.cam = null;
    }

    static void Destroy(UnityEngine.Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) UnityEngine.Object.Destroy(o);
        else UnityEngine.Object.DestroyImmediate(o);
    }

    // ------------------------------------------------------------------------------------------------ per frame
    static void BeforeRender()
    {
        if (bodies.Count == 0) return;
        if (Time.frameCount == lastFrame) return;
        lastFrame = Time.frameCount;
        var viewer = PickViewer();
        if (viewer == null) return;
        FrontRoomsWaterRipples.Tick(Time.deltaTime, viewer.transform.position);
        RenderFor(viewer, Time.frameCount);
    }

    static Camera PickViewer()
    {
        if (Viewer != null && Viewer.isActiveAndEnabled) return Viewer;
        var main = Camera.main;
        if (main != null && main.isActiveAndEnabled) return main;
        Camera best = null;
        foreach (var c in Camera.allCameras)
        {
            if (c == null || c.cameraType != CameraType.Game || c.targetTexture != null) continue;
            if (best == null || c.depth > best.depth) best = c;
        }
        return best;
    }

    /// <summary>Renders the mirror images for this viewer (tools call it before rendering the viewer; the game calls it
    /// from Application.onBeforeRender) and points every water body at its image or at the probe. frame decides the
    /// High tier's every-second-frame refresh.</summary>
    public static void RenderFor(Camera viewer, int frame)
    {
        if (viewer == null || bodies.Count == 0) return;
        // what lies under the water and the niches' thrown light (the mirror images below render it too)
        FrontRoomsWaterSubmerged.ViewerPosition = viewer.transform.position;
        FrontRoomsWaterSubmerged.Publish();
        var tier = CurrentTier;
        LastTier = tier;
        LastPlanarCount = 0;
        GeometryUtility.CalculateFrustumPlanes(viewer, frustum);
        var eye = viewer.transform.position;

        // the visible heights, nearest first
        heightScore.Clear();
        var anyRefract = false;
        foreach (var b in bodies)
        {
            if (b.renderer == null || !b.renderer.enabled || !b.renderer.gameObject.activeInHierarchy) continue;
            var bounds = b.renderer.bounds;
            if (eye.y <= b.y + .01f || !GeometryUtility.TestPlanesAABB(frustum, bounds)) continue;
            var key = Key(b.y);
            var d = Mathf.Sqrt(bounds.SqrDistance(eye));
            float s;
            if (!heightScore.TryGetValue(key, out s) || d < s) heightScore[key] = d;
            if (b.refract) anyRefract = true;
        }
        picked.Clear();
        if (tier != Tier.Probe)
        {
            var budget = tier == Tier.Ultra ? 2 : 1;
            keys.Clear();
            keys.AddRange(heightScore.Keys);
            keys.Sort(byDistance);
            for (var i = 0; i < keys.Count && i < budget; i++) picked.Add(keys[i]);
        }

        firstKey = picked.Count > 0 ? picked[0] : int.MinValue;
        foreach (var key in picked)
        {
            var p = GetPlane(key);
            var due = tier == Tier.Ultra || frame - p.renderedFrame >= 2 || !p.valid || frame < p.renderedFrame;
            if (due) RenderPlane(p, viewer, tier, frame);
            if (p.valid) LastPlanarCount++;
        }
        // planes not in view this frame keep their texture a little, then go
        for (var i = planes.Count - 1; i >= 0; i--)
            if (!picked.Contains(planes[i].key) && frame - planes[i].renderedFrame > 120) { Free(planes[i]); planes.RemoveAt(i); }

        var refract = tier != Tier.Probe && anyRefract;
        AskOpaque(viewer, refract);
        foreach (var b in bodies)
        {
            if (b.renderer == null) continue;
            Plane p = null;
            if (picked.Contains(Key(b.y))) p = FindPlane(Key(b.y));
            Apply(b, p != null && p.valid ? p : null, refract && b.refract);
        }
    }

    static int Key(float y) { return Mathf.RoundToInt(y * 1000f); }

    static int ByDistance(int a, int b) { return heightScore[a].CompareTo(heightScore[b]); }

    static Plane FindPlane(int key)
    {
        foreach (var p in planes) if (p.key == key) return p;
        return null;
    }

    static Plane GetPlane(int key)
    {
        var p = FindPlane(key);
        if (p != null) return p;
        p = new Plane { key = key, y = key / 1000f };
        planes.Add(p);
        return p;
    }

    static void Apply(Body b, Plane p, bool refract)
    {
        b.renderer.GetPropertyBlock(b.mpb);
        b.mpb.SetVector(BoxMinId, b.boxMin);
        b.mpb.SetVector(BoxMaxId, b.boxMax);
        b.mpb.SetVector(ProbePosId, b.probe);
        if (p != null)
        {
            b.mpb.SetTexture(PlanarTexId, p.rt);
            b.mpb.SetMatrix(PlanarVPId, p.vp);
            b.mpb.SetVector(PlanarParamsId, new Vector4(1f, Mathf.Max(0, p.rt.mipmapCount - 1), p.rt.width, p.rt.height));
            b.mpb.SetFloat(LampSpecId, 0f);
        }
        else
        {
            b.mpb.SetTexture(PlanarTexId, Texture2D.blackTexture);
            b.mpb.SetVector(PlanarParamsId, Vector4.zero);
            b.mpb.SetFloat(LampSpecId, 1f);
        }
        b.mpb.SetFloat(RefractId, refract ? 1f : 0f);
        b.renderer.SetPropertyBlock(b.mpb);
    }

    static void AskOpaque(Camera viewer, bool want)
    {
        opaqueCamera = want ? viewer : null;
        if (want && !opaqueHooked)
        {
            if (opaquePass == null) opaquePass = new OpaqueRequest();
            RenderPipelineManager.beginCameraRendering += OnBeginCamera;
            opaqueHooked = true;
        }
        else if (!want && opaqueHooked)
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
            opaqueHooked = false;
            Shader.SetGlobalFloat(OpaqueId, 0f);
        }
    }

    // URP's way to add a pass from a script; the global tells FrontRooms/Water whether this camera has the texture
    static void OnBeginCamera(ScriptableRenderContext context, Camera camera)
    {
        var has = camera != null && camera == opaqueCamera;
        Shader.SetGlobalFloat(OpaqueId, has ? 1f : 0f);
        if (!has) return;
        var data = camera.GetUniversalAdditionalCameraData();
        if (data != null && data.scriptableRenderer != null) data.scriptableRenderer.EnqueuePass(opaquePass);
    }

    // ------------------------------------------------------------------------------------------------ the mirror camera
    static void RenderPlane(Plane p, Camera viewer, Tier tier, int frame)
    {
        var scale = tier == Tier.Ultra ? UltraScale : viewer.pixelHeight > 1440 ? HighScaleAbove1440 : HighScale;
        var w = Mathf.Max(64, Mathf.RoundToInt(viewer.pixelWidth * scale * Overscan));
        var h = Mathf.Max(64, Mathf.RoundToInt(viewer.pixelHeight * scale * Overscan));
        if (p.rt == null || p.rt.width != w || p.rt.height != h)
        {
            if (p.rt != null) { p.rt.Release(); Destroy(p.rt); }
            var fmt = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB111110Float) ? RenderTextureFormat.RGB111110Float : RenderTextureFormat.ARGBHalf;
            p.rt = new RenderTexture(w, h, 24, fmt, RenderTextureReadWrite.Linear)
            {
                name = "FrontRooms water planar " + p.key, useMipMap = true, autoGenerateMips = false, antiAliasing = 1,
                filterMode = FilterMode.Trilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave,
            };
            p.rt.Create();
            p.valid = false;
        }
        if (p.cam == null)
        {
            var go = new GameObject("FrontRooms water planar " + p.key) { hideFlags = HideFlags.HideAndDontSave };
            p.cam = go.AddComponent<Camera>();
            p.cam.enabled = false;
            p.data = go.AddComponent<UniversalAdditionalCameraData>();
        }
        var c = p.cam;
        var d = p.data;
        c.CopyFrom(viewer);
        c.enabled = false;
        c.cameraType = CameraType.Reflection;
        c.targetTexture = null;
        c.useOcclusionCulling = false;
        c.allowMSAA = false;
        c.allowHDR = true;
        c.cullingMask = viewer.cullingMask & ~ExcludedLayers;
        c.clearFlags = CameraClearFlags.SolidColor;
        c.backgroundColor = viewer.backgroundColor;
        d.renderPostProcessing = false;
        d.antialiasing = AntialiasingMode.None;
        d.renderShadows = false;
        d.requiresColorOption = CameraOverrideOption.Off;
        d.requiresDepthOption = CameraOverrideOption.Off;
        d.stopNaN = false;
        d.dithering = false;
        d.allowXRRendering = false;
        d.volumeTrigger = viewer.transform;
        UniversalAdditionalCameraData vd;
        if (viewer.TryGetComponent(out vd)) d.volumeLayerMask = vd.volumeLayerMask;
        var ri = ReflectionRendererIndex();
        d.SetRenderer(ri);
        LastRenderer = ri >= 0 ? "renderer " + ri : "default renderer";

        // the eye mirrored through the water plane, looking along the mirrored view (up flipped: a proper rotation)
        var y = p.y;
        var t = viewer.transform;
        var e = t.position;
        var f = t.forward;
        var u = t.up;
        c.transform.SetPositionAndRotation(new Vector3(e.x, 2f * y - e.y, e.z), Quaternion.LookRotation(new Vector3(f.x, -f.y, f.z), new Vector3(-u.x, u.y, -u.z)));
        c.ResetWorldToCameraMatrix();
        c.ResetProjectionMatrix();
        c.fieldOfView = viewer.fieldOfView;
        c.aspect = viewer.aspect;
        c.nearClipPlane = viewer.nearClipPlane;
        c.farClipPlane = Mathf.Min(viewer.farClipPlane, FarFor(p, e, tier));
        // the frustum fitted to where this height's water lies on the viewer's screen: a water point at the viewer's
        // NDC (x, y) is at (x, -y) for this camera (its up is the mirrored up turned over), so the image spends its texels
        // on the water and the room above it, never on the clipped space below the mirror's horizon; widened by the
        // overscan and a margin for rippled samples, and never finer than the viewer's own pixels
        var r = WaterRect(p, viewer, scale);
        var n = c.nearClipPlane;
        var ty = Mathf.Tan(viewer.fieldOfView * .5f * Mathf.Deg2Rad) * n;
        var tx = ty * viewer.aspect;
        c.projectionMatrix = Matrix4x4.Frustum(r.xMin * tx, r.xMax * tx, -r.yMax * ty, -r.yMin * ty, n, c.farClipPlane);
        var w2c = c.worldToCameraMatrix;
        var cpos = w2c.MultiplyPoint(new Vector3(e.x, y + ClipOffset, e.z));
        var cn = w2c.MultiplyVector(Vector3.up).normalized;
        c.projectionMatrix = c.CalculateObliqueMatrix(new Vector4(cn.x, cn.y, cn.z, -Vector3.Dot(cpos, cn)));
        p.vp = c.projectionMatrix * w2c;

        var req = new UniversalRenderPipeline.SingleCameraRequest { destination = p.rt };
        if (RenderPipeline.SupportsRenderRequest(c, req))
        {
            RenderPipeline.SubmitRenderRequest(c, req);
            p.rt.GenerateMips();
            p.valid = true;
            p.renderedFrame = frame;
            LastSize = new Vector2Int(w, h);
        }
        else p.valid = false;
        c.targetTexture = null;
    }

    /// <summary>The viewer-NDC rectangle (x and y in -1..1) holding this height's visible water (each body's rectangle
    /// clipped to the viewer's near plane, so water reaching behind the viewer still fits), widened by the overscan about
    /// its centre plus 0.05 for rippled samples, at least (scale x overscan) of the screen across so the image is never
    /// finer than the viewer's pixels, and clamped to the overscanned screen.</summary>
    static Rect WaterRect(Plane p, Camera viewer, float scale)
    {
        var w2c = viewer.worldToCameraMatrix;
        var proj = viewer.projectionMatrix;
        var near = viewer.nearClipPlane;
        float x0 = float.MaxValue, x1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue;
        foreach (var b in bodies)
        {
            if (b.renderer == null || Key(b.y) != p.key || !b.renderer.enabled) continue;
            var bb = b.renderer.bounds;
            if (!GeometryUtility.TestPlanesAABB(frustum, bb)) continue;      // only the water in view (frustum of RenderFor)
            quad[0] = w2c.MultiplyPoint(new Vector3(bb.min.x, p.y, bb.min.z));
            quad[1] = w2c.MultiplyPoint(new Vector3(bb.max.x, p.y, bb.min.z));
            quad[2] = w2c.MultiplyPoint(new Vector3(bb.max.x, p.y, bb.max.z));
            quad[3] = w2c.MultiplyPoint(new Vector3(bb.min.x, p.y, bb.max.z));
            // clip to the near plane (view space looks down -z): keep -z >= near
            var n = 0;
            for (var i = 0; i < 4; i++)
            {
                var a = quad[i];
                var c = quad[(i + 1) & 3];
                var da = -a.z - near;
                var dc = -c.z - near;
                if (da >= 0f) clipped[n++] = a;
                if ((da >= 0f) != (dc >= 0f)) clipped[n++] = a + (c - a) * (da / (da - dc));
            }
            for (var i = 0; i < n; i++)
            {
                var h = proj * new Vector4(clipped[i].x, clipped[i].y, clipped[i].z, 1f);
                if (h.w <= 1e-5f) continue;
                var x = h.x / h.w;
                var y = h.y / h.w;
                x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x); y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y);
            }
        }
        var r = new Rect(-Overscan, -Overscan, 2f * Overscan, 2f * Overscan);
        x0 = Mathf.Max(x0, -1f); x1 = Mathf.Min(x1, 1f); y0 = Mathf.Max(y0, -1f); y1 = Mathf.Min(y1, 1f);
        if (x1 > x0 && y1 > y0)
        {
            var minHalf = Mathf.Min(scale * Overscan, Overscan);      // the image's texels never finer than the viewer's pixels
            var cx = (x0 + x1) * .5f;
            var cy = (y0 + y1) * .5f;
            var hx = Mathf.Max((x1 - x0) * .5f * Overscan + .05f, minHalf);
            var hy = Mathf.Max((y1 - y0) * .5f * Overscan + .05f, minHalf);
            r = Rect.MinMaxRect(Mathf.Max(cx - hx, -Overscan), Mathf.Max(cy - hy, -Overscan), Mathf.Min(cx + hx, Overscan), Mathf.Min(cy + hy, Overscan));
        }
        if (p.key == firstKey) LastRect = r;
        return r;
    }

    /// <summary>Logs: the viewer-NDC rectangle the last mirror image covered.</summary>
    public static Rect LastRect { get; private set; }

    static float FarFor(Plane p, Vector3 eye, Tier tier)
    {
        var far = 0f;
        var me = new Vector3(eye.x, 2f * p.y - eye.y, eye.z);
        foreach (var b in bodies)
        {
            if (b.renderer == null || Key(b.y) != p.key) continue;
            for (var i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? b.boxMin.x : b.boxMax.x, (i & 2) == 0 ? b.boxMin.y : b.boxMax.y, (i & 4) == 0 ? b.boxMin.z : b.boxMax.z);
                far = Mathf.Max(far, Vector3.Distance(me, corner));
            }
        }
        return Mathf.Min(far + 2f, tier == Tier.Ultra ? UltraFar : HighFar);
    }

    static int ReflectionRendererIndex()
    {
        if (RendererOverride >= 0) return RendererOverride;
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp == null) return -1;
        var list = urp.rendererDataList;
        for (var i = 0; i < list.Length; i++)
            if (list[i] != null && list[i].name == ReflectionRendererName) return i;
        return -1;
    }

    /// <summary>Tools: the mirror image of a water height (null when none), and the view-projection it was made with.</summary>
    public static RenderTexture PlanarTexture(float y, out Matrix4x4 vp)
    {
        var p = FindPlane(Key(y));
        vp = p != null ? p.vp : Matrix4x4.identity;
        return p != null && p.valid ? p.rt : null;
    }

    /// <summary>Tools and logs: what the last RenderFor did.</summary>
    public static string Describe()
    {
        return "tier " + LastTier + ", planar images " + LastPlanarCount + " (" + LastSize.x + "x" + LastSize.y + ", " + LastRenderer + ", ndc x "
            + LastRect.xMin.ToString("0.00") + ".." + LastRect.xMax.ToString("0.00") + " y " + LastRect.yMin.ToString("0.00") + ".." + LastRect.yMax.ToString("0.00") + "), bodies " + bodies.Count;
    }
}
