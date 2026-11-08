using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The lamps held in the glaze of the title's pool room (title_renovation, 2026-11-05; line level_space for 游戏视觉's title
/// stream). Red, 2026-11-05 13:1x in the level_space chat, after walking scene 4: "我看了一下，这个瓷砖非常work，因此我需要你在无限流
/// 房间里利用这个瓷砖来做新无限流房间的纹理材质和水光。水色需要最深"; 13:3x: "我需要你翻新无限流房间然后直接合并到主项目里".
/// Scene 4's GZ-A (FrontRoomsStructuresLook; research/level_space/73_pool_glaze.md sections 3.3-3.4 and 12; Red's GZ-C) for
/// the lit Pool vignette: one realtime box probe, built once with the vignette and moved with it from slot to slot, captured
/// once per attach while the room is drawn behind its still shut door. For the probe's own face renders only, the slot's live
/// lamps are on at their steady output and a reflection-only copy of each live lamp's lens glows at the lamp's tint x
/// ProxyGain, so the dished tile (_FR_TILE, which box-projects on its own) holds the room's own lamps, broken per tile, at
/// their real places. The viewer never sees the lamps forced on (begin / end camera rendering) nor the copies (layer 27, out
/// of every other camera; active only while a capture renders).
/// Which renderers take it: the vignette's tile and the slot's shell faces of the room, through Renderer.probeAnchor at the
/// room's centre (the end walls' bounds centres sit on the door lines, outside the box, and the URP assets select probes per
/// object); the walnut pairs of both door lines are anchored off the corridor's axis, where no probe stands, so they keep
/// today's cube. The probe's brightness follows the room's lamps (the mean output of the live lamps, smoothed over
/// FadeSeconds): dark while the room is dark, up with the strike, out with DimRoom. Desktop only (realtime probes are off at
/// Medium and Handheld: phones keep Refl_PoolLevel); WebGL keeps today's title. 游戏视觉's notes (W/level_space/title_pool/
/// visual_reply.txt): one probe per pool room, captured once per attach while drawn, never per frame.
/// </summary>
public sealed class FrontRoomsTitlePoolGlaze
{
    /// <summary>The copies' layer: unused in the game (its layers are 4, 5, 28-31); scene 4 uses it the same way.</summary>
    public const int ProxyLayer = 27;
    public const int ProxyMask = 1 << ProxyLayer;
    /// <summary>A copy's radiance: (the lamp's tint, linear, x ProxyGain), linearised once more (scene 4's ProxyRadiance). Scene 4's
    /// 30 is set for compact 0.17 m2 lenses; the L03 lens is about 0.63 m2 and the room hangs twelve: 30 washed the walls x2.4 and
    /// the ceiling x3.6-5, 12 lit the ceiling's grout, 8 holds white lamp cores per tile with the grout dark (title_pool, 15:0x).</summary>
    public const float ProxyGain = 8f;
    /// <summary>How far a copy sits in front of its lens, along its lamp's beam (m).</summary>
    public const float ProxyOffset = .002f;
    public const int Resolution = 512;
    /// <summary>The probe's eye over the water (m), at the room's centre.</summary>
    public const float EyeAboveWater = 1.6f;
    /// <summary>The box stops this far short of the end faces (m). Unity gives a renderer every probe whose box its bounds touch,
    /// and the neighbouring slots' carpet and ceiling boxes reach 2 cm past the door lines: with the box on the faces, the Level 0
    /// rooms on both sides took this probe (bright lamp images in their carpet's damp patches, title_renovation loop 2, R15 and
    /// R17). The room's own tile looks the probe up from its anchor, inside the box whatever its bounds.</summary>
    public const float BoxEndInset = .03f;
    /// <summary>The time constant of the probe's brightness following the room's lamps (s).</summary>
    public const float FadeSeconds = .25f;
    /// <summary>The most frames a capture keeps its copies on while its probe has not said it is done.</summary>
    public const int CaptureFramesMax = 90;
    /// <summary>Where the tile looks the probe up (room space: the room's centre) and where the walnut pairs do (off the axis).</summary>
    public static readonly Vector3 AnchorLocal = new Vector3(0f, 1.4f, FrontRoomsRoomStream.RoomLength * .5f);
    public static readonly Vector3 OffAxisLocal = new Vector3(50f, 1.4f, FrontRoomsRoomStream.RoomLength * .5f);
    const string ProxyName = "title pool reflection copy (layer 27, probe only) ";

    public readonly ReflectionProbe probe;
    public readonly Transform anchor, offAxisAnchor;
    readonly Transform root;
    readonly List<Renderer> proxies = new List<Renderer>(12);
    readonly List<int> proxyLamp = new List<int>(12);
    readonly List<bool> proxyPosed = new List<bool>(12);
    readonly List<Renderer> anchored = new List<Renderer>(32);
    readonly List<Renderer> anchoredOffAxis = new List<Renderer>(96);
    readonly List<Light> forced = new List<Light>(12);
    readonly List<bool> forcedOn = new List<bool>(12);
    readonly List<float> forcedIntensity = new List<float>(12);
    Light[] lamps;
    bool[] dead;
    float[] baseIntensity;
    int renderId = -1, captureFrame = -1;
    bool capturing, captured, registered;
    float fade;

    /// <summary>Tools and logs.</summary>
    public int Captures { get; private set; }
    public int LastCaptureFrame => captureFrame;
    public int LastCaptureFrames { get; private set; }
    public float LastCaptureCpuMs { get; private set; }
    public int FacesSeen { get; private set; }
    /// <summary>The lamp copies shown in the last capture (one per live lamp).</summary>
    public int ProxyCount { get; private set; }
    public int AnchoredCount => anchored.Count;
    public int OffAxisCount => anchoredOffAxis.Count;
    public bool Captured => captured;
    public bool Capturing => capturing;
    public float Level => fade;

    /// <summary>True where the probe can run: desktop Play mode with the quality level's realtime reflection probes on (High, Very
    /// High, Ultra); never on the phone tier (Medium and Handheld have them off) or WebGL.</summary>
    public static bool Supported
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return false;
#else
            return Application.isPlaying && !FrontRoomsTrofferKit.PhoneTier && QualitySettings.realtimeReflectionProbes;
#endif
        }
    }

    /// <summary>The probe (inactive until its first capture: an unrendered realtime probe never binds) and its two anchors under the
    /// pool vignette's root. Box: the room's inner faces, from the basin floor to the ceiling, side face to side face, end face to
    /// end face less BoxEndInset; the water and the UI left out of its captures.</summary>
    public static FrontRoomsTitlePoolGlaze Build(Transform vignetteRoot, float waterY, float floorY, float sideFace, float endFace)
    {
        if (vignetteRoot == null) return null;
        try { return new FrontRoomsTitlePoolGlaze(vignetteRoot, waterY, floorY, sideFace, endFace); }
        catch (Exception e)
        {
            Debug.LogWarning("[FrontRooms3D] title pool glaze: the probe could not be built, the pool keeps its zone cube (" + e.Message + ")");
            return null;
        }
    }

    FrontRoomsTitlePoolGlaze(Transform vignetteRoot, float waterY, float floorY, float sideFace, float endFace)
    {
        root = vignetteRoot;
        const float H = FrontRoomsRoomStream.RoomHeight, L = FrontRoomsRoomStream.RoomLength;
        var go = new GameObject("title pool probe (realtime, once per attach)");
        go.SetActive(false);
        go.transform.SetParent(vignetteRoot, false);
        var eyeY = waterY + EyeAboveWater;
        go.transform.localPosition = new Vector3(0f, eyeY, L * .5f);
        probe = go.AddComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
        probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
        probe.resolution = Resolution;
        probe.hdr = true;
        probe.boxProjection = true;
        var z0 = endFace + BoxEndInset;
        var z1 = L - endFace - BoxEndInset;
        probe.size = new Vector3(sideFace * 2f, H - floorY, z1 - z0);
        probe.center = new Vector3(0f, (floorY + H) * .5f - eyeY, (z0 + z1) * .5f - L * .5f);
        probe.importance = 2;
        probe.nearClipPlane = .1f;
        probe.farClipPlane = 20f;
        probe.cullingMask = ~((1 << FrontRoomsWaterPlanar.WaterLayer) | (1 << 5));
        probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
        probe.backgroundColor = Color.black;
        probe.intensity = 0f;
        anchor = Child("title pool probe anchor (the room's centre)", AnchorLocal);
        offAxisAnchor = Child("title pool door anchor (off the axis: no probe there)", OffAxisLocal);
    }

    Transform Child(string name, Vector3 local)
    {
        var t = new GameObject(name).transform;
        t.SetParent(root, false);
        t.localPosition = local;
        return t;
    }

    // ---------------------------------------------------------------------------------------------------------- capture
    /// <summary>Captures the probe for the slot the vignette now stands in: its lenses (copies posed and shown for the live lamps),
    /// its lamps (forced on for the probe's faces), the shell faces of its room and both door lines (anchors). Starts a
    /// time-sliced render (one face a frame, then the filtering); Tick ends it. False when the probe cannot run here.</summary>
    public bool Capture(Transform slotRoot, Renderer[] lenses, Light[] slotLights, bool[] slotDead, float[] slotBase, Transform[] doorRoots, Camera viewer)
    {
        if (probe == null || slotRoot == null || !Supported) return false;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        RestoreForced();
        lamps = slotLights;
        dead = slotDead;
        baseIntensity = slotBase;
        PoseProxies(lenses);
        ProxyCount = ShowProxies(true);
        Anchor(slotRoot, doorRoots);
        StripProxyLayer(viewer);
        StripProxyLayer(FrontRoomsWaterPlanar.Viewer);
        StripProxyLayer(Camera.main);
        Register();
        fade = 0f;
        probe.intensity = 0f;
        if (!probe.gameObject.activeSelf) probe.gameObject.SetActive(true);
        FacesSeen = 0;
        renderId = -1;
        try { renderId = probe.RenderProbe(); }
        catch (Exception e) { Debug.LogWarning("[FrontRooms3D] title pool glaze: the probe did not render (" + e.Message + ")"); }
        if (renderId < 0)
        {
            Release();
            return false;
        }
        capturing = true;
        captured = false;
        captureFrame = Time.frameCount;
        Captures++;
        LastCaptureCpuMs = (float)watch.Elapsed.TotalMilliseconds;
        return true;
    }

    /// <summary>Once a stream tick while the vignette stands in a slot: ends a capture once its probe is done (the copies go), and
    /// follows the room's lamps: <paramref name="lampLevel"/> = the mean output of its live lamps over their steady output.</summary>
    public void Tick(float dt, float lampLevel)
    {
        if (probe == null || (!capturing && !captured)) return;
        RestoreForced();
        if (!Supported)
        {
            // the quality level turned realtime probes off: the room goes back to its zone cube
            Release();
            return;
        }
        if (capturing)
        {
            var done = probe.IsFinishedRendering(renderId);
            if (done || Time.frameCount - captureFrame > CaptureFramesMax)
            {
                capturing = false;
                captured = true;
                LastCaptureFrames = Time.frameCount - captureFrame;
                ShowProxies(false);
            }
        }
        var k = 1f - Mathf.Exp(-Mathf.Max(dt, 0f) / FadeSeconds);
        fade += (Mathf.Clamp01(lampLevel) - fade) * k;
        probe.intensity = fade;
    }

    /// <summary>The vignette leaves its slot (or the probe may not run): the probe off, the copies off, every anchor it set cleared.</summary>
    public void Release()
    {
        RestoreForced();
        ShowProxies(false);
        if (probe != null)
        {
            probe.intensity = 0f;
            if (probe.gameObject.activeSelf) probe.gameObject.SetActive(false);
        }
        ClearAnchors();
        capturing = false;
        captured = false;
        fade = 0f;
        renderId = -1;
        lamps = null;
        dead = null;
        baseIntensity = null;
        Unregister();
    }

    /// <summary>The title has ended (the start door shut for good): a capture in flight stops, the camera hooks go; the probe keeps
    /// what it holds (its rooms are never seen again).</summary>
    void End()
    {
        RestoreForced();
        if (capturing)
        {
            capturing = false;
            captured = true;
            ShowProxies(false);
        }
        registered = false;
    }

    // ---------------------------------------------------------------------------------------------------------- the lamp copies
    void PoseProxies(Renderer[] lenses)
    {
        for (var k = 0; k < proxyPosed.Count; k++) proxyPosed[k] = false;
        if (lenses == null) return;
        var m = ProxyMaterial();
        if (m == null) return;
        if (proxyBlock == null) proxyBlock = new MaterialPropertyBlock();
        for (var i = 0; i < lenses.Length; i++)
        {
            var lens = lenses[i];
            var mf = lens != null ? lens.GetComponent<MeshFilter>() : null;
            var k = proxyLamp.IndexOf(i);
            if (mf == null || mf.sharedMesh == null) continue;
            Renderer r;
            if (k < 0 || proxies[k] == null)
            {
                var go = new GameObject(ProxyName + i);
                go.SetActive(false);
                go.layer = ProxyLayer;
                go.transform.SetParent(root, false);
                go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.lightProbeUsage = LightProbeUsage.Off;
                mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
                mr.allowOcclusionWhenDynamic = false;
                if (k < 0)
                {
                    k = proxies.Count;
                    proxies.Add(mr);
                    proxyLamp.Add(i);
                    proxyPosed.Add(false);
                }
                else proxies[k] = mr;
                r = mr;
            }
            else r = proxies[k];
            proxyPosed[k] = true;
            r.GetComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            r.sharedMaterial = m;
            // the lens's pose, ProxyOffset down its lamp's beam (the L03 lens faces down)
            var t = lens.transform;
            var light = lamps != null && i < lamps.Length ? lamps[i] : null;
            var beam = light != null ? light.transform.forward : Vector3.down;
            r.transform.SetPositionAndRotation(t.position + beam * ProxyOffset, t.rotation);
            var rs = root.lossyScale;
            var ls = t.lossyScale;
            r.transform.localScale = new Vector3(ls.x / Mathf.Max(1e-6f, rs.x), ls.y / Mathf.Max(1e-6f, rs.y), ls.z / Mathf.Max(1e-6f, rs.z));
            // its lamp's radiance at the lamp's steady state, as the GPU gets it (SetVector: already linear and HDR)
            var c = light != null ? (light.color.linear * ProxyGain).linear : Color.black;
            r.GetPropertyBlock(proxyBlock);
            proxyBlock.SetVector(proxyUnlit ? BaseColorId : EmissionColorId, new Vector4(c.r, c.g, c.b, 1f));
            r.SetPropertyBlock(proxyBlock);
        }
    }

    int ShowProxies(bool on)
    {
        var shown = 0;
        for (var k = 0; k < proxies.Count; k++)
        {
            var r = proxies[k];
            if (r == null) continue;
            var i = proxyLamp[k];
            var live = on && proxyPosed[k] && lamps != null && i < lamps.Length && lamps[i] != null && !(dead != null && i < dead.Length && dead[i]);
            if (r.gameObject.activeSelf != live) r.gameObject.SetActive(live);
            if (live) shown++;
        }
        return shown;
    }

    static Material proxyMaterial;
    static bool proxyUnlit, proxyMissing;
    static MaterialPropertyBlock proxyBlock;
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    static readonly int EmissionMapId = Shader.PropertyToID("_EmissionMap");
    static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");

    /// <summary>The copies' material: the L03 lens's own (FrontRooms/Surface with _EMISSION, in Resources, so its variant is in every
    /// player), made flat: no tint, a white emission map, no gloss; its emission is the copy's radiance. URP Unlit (scene 4's) only
    /// as a fallback: no material of the build uses its opaque variant.</summary>
    static Material ProxyMaterial()
    {
        if (proxyMaterial != null) return proxyMaterial;
        if (proxyMissing) return null;
        var lens = FrontRoomsTrofferKit.LensMaterial;
        if (lens != null && lens.HasProperty(EmissionColorId) && lens.IsKeywordEnabled("_EMISSION"))
        {
            var m = new Material(lens) { name = "Title pool reflection copy (L03 lens, probe only)", hideFlags = HideFlags.DontSave };
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, Color.black);
            if (m.HasProperty(EmissionMapId)) m.SetTexture(EmissionMapId, Texture2D.whiteTexture);
            if (m.HasProperty(SmoothnessId)) m.SetFloat(SmoothnessId, 0f);
            m.SetVector(EmissionColorId, Vector4.zero);
            proxyMaterial = m;
            proxyUnlit = false;
            return m;
        }
        var sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh != null)
        {
            var m = new Material(sh) { name = "Title pool reflection copy (URP Unlit, probe only)", hideFlags = HideFlags.DontSave };
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
            m.SetColor(BaseColorId, Color.white);
            proxyMaterial = m;
            proxyUnlit = true;
            return m;
        }
        proxyMissing = true;
        Debug.LogWarning("[FrontRooms3D] title pool glaze: no material for the lamp copies (no L03 lens, no URP Unlit): the glaze shows the room without its lamps.");
        return null;
    }

    // ---------------------------------------------------------------------------------------------------------- anchors
    static readonly List<Renderer> rendererBuffer = new List<Renderer>(256);

    void Anchor(Transform slotRoot, Transform[] doorRoots)
    {
        ClearAnchors();
        // the vignette's tile (basin, sills, skirts, band): the room's centre. Not the water (its reflection is the mirror, its
        // probe usage is off) nor the copies.
        root.GetComponentsInChildren(true, rendererBuffer);
        for (var i = 0; i < rendererBuffer.Count; i++)
        {
            var r = rendererBuffer[i];
            if (r == null) continue;
            var layer = r.gameObject.layer;
            if (layer == ProxyLayer || layer == FrontRoomsWaterPlanar.WaterLayer || UnderAny(r.transform, doorRoots)) continue;
            r.probeAnchor = anchor;
            anchored.Add(r);
        }
        // the slot's shell faces of this room: its walls, ceiling, rear seal, the far door wall and its room-side reveals
        for (var i = 0; i < slotRoot.childCount; i++)
        {
            var c = slotRoot.GetChild(i);
            if (c == root || !ShellFace(c.name)) continue;
            var r = c.GetComponent<Renderer>();
            if (r == null) continue;
            r.probeAnchor = anchor;
            anchored.Add(r);
        }
        // the walnut pairs of both door lines (this room's far door, and its entry pair, the previous slot's, which swings into
        // this room): off the axis, where no probe stands, so they keep today's cube
        if (doorRoots != null)
            for (var d = 0; d < doorRoots.Length; d++)
            {
                if (doorRoots[d] == null) continue;
                doorRoots[d].GetComponentsInChildren(true, rendererBuffer);
                for (var i = 0; i < rendererBuffer.Count; i++)
                {
                    var r = rendererBuffer[i];
                    if (r == null) continue;
                    r.probeAnchor = offAxisAnchor;
                    anchoredOffAxis.Add(r);
                }
            }
        rendererBuffer.Clear();
    }

    static bool ShellFace(string name)
    {
        switch (name)
        {
            case "ceiling":
            case "left wallpaper wall":
            case "right wallpaper wall":
            case "opaque rear boundary seal":
            case "door wall return left":
            case "door wall return right":
            case "door wall above":
            case "door reveal left":
            case "door reveal right":
            case "door reveal header":
                return true;
            default:
                return false;
        }
    }

    static bool UnderAny(Transform t, Transform[] roots)
    {
        if (roots == null) return false;
        for (var i = 0; i < roots.Length; i++) if (roots[i] != null && t.IsChildOf(roots[i])) return true;
        return false;
    }

    void ClearAnchors()
    {
        for (var i = 0; i < anchored.Count; i++)
            if (anchored[i] != null && anchored[i].probeAnchor == anchor) anchored[i].probeAnchor = null;
        for (var i = 0; i < anchoredOffAxis.Count; i++)
            if (anchoredOffAxis[i] != null && anchoredOffAxis[i].probeAnchor == offAxisAnchor) anchoredOffAxis[i].probeAnchor = null;
        anchored.Clear();
        anchoredOffAxis.Clear();
    }

    // ---------------------------------------------------------------------------------------------------------- the camera hooks
    static readonly List<FrontRoomsTitlePoolGlaze> live = new List<FrontRoomsTitlePoolGlaze>(2);
    static bool hooked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        RenderPipelineManager.beginCameraRendering -= OnBegin;
        RenderPipelineManager.endCameraRendering -= OnEnd;
        hooked = false;
        live.Clear();
        proxyMaterial = null;
        proxyMissing = false;
        proxyUnlit = false;
        proxyBlock = null;
    }

    void Register()
    {
        if (!registered)
        {
            registered = true;
            if (!live.Contains(this)) live.Add(this);
        }
        if (hooked) return;
        hooked = true;
        RenderPipelineManager.beginCameraRendering += OnBegin;
        RenderPipelineManager.endCameraRendering += OnEnd;
    }

    void Unregister()
    {
        registered = false;
        live.Remove(this);
        if (live.Count == 0) Unhook();
    }

    static void Unhook()
    {
        if (!hooked) return;
        hooked = false;
        RenderPipelineManager.beginCameraRendering -= OnBegin;
        RenderPipelineManager.endCameraRendering -= OnEnd;
    }

    /// <summary>The title has ended (the stream's start door shut, or the stream destroyed): every capture in flight stops and the
    /// camera hooks go.</summary>
    public static void EndAll()
    {
        for (var i = 0; i < live.Count; i++) if (live[i] != null) live[i].End();
        live.Clear();
        Unhook();
    }

    static void StripProxyLayer(Camera c)
    {
        if (c != null && c.cameraType != CameraType.Reflection && (c.cullingMask & ProxyMask) != 0) c.cullingMask &= ~ProxyMask;
    }

    // Every camera as it begins rendering, before its culling: a face of one of our probes in its capture gets the slot's live
    // lamps on at their steady output (put back at its end); every other camera, the water's mirrors (Reflection cameras too)
    // and the glass among them, loses layer 27.
    static void OnBegin(ScriptableRenderContext context, Camera camera)
    {
        if (camera == null) return;
        if (camera.cameraType == CameraType.Reflection)
        {
            var g = GlazeAt(camera);
            if (g != null)
            {
                if (g.capturing) g.ForceLamps();
                return;
            }
        }
        if ((camera.cullingMask & ProxyMask) != 0) camera.cullingMask &= ~ProxyMask;
    }

    static void OnEnd(ScriptableRenderContext context, Camera camera)
    {
        if (camera == null || camera.cameraType != CameraType.Reflection) return;
        var g = GlazeAt(camera);
        if (g != null) g.RestoreForced();
    }

    // The glaze whose probe this camera renders: its eye (as it renders: the view matrix) on the probe.
    static FrontRoomsTitlePoolGlaze GlazeAt(Camera camera)
    {
        Vector3 eye = camera.cameraToWorldMatrix.GetColumn(3);
        FrontRoomsTitlePoolGlaze found = null;
        for (var i = live.Count - 1; i >= 0; i--)
        {
            var g = live[i];
            if (g == null || g.probe == null)
            {
                live.RemoveAt(i);
                continue;
            }
            if (found == null && g.probe.isActiveAndEnabled && (g.probe.transform.position - eye).sqrMagnitude < 1e-4f) found = g;
        }
        if (live.Count == 0) Unhook();
        return found;
    }

    void ForceLamps()
    {
        FacesSeen++;
        RestoreForced();
        if (lamps == null) return;
        for (var i = 0; i < lamps.Length; i++)
        {
            var l = lamps[i];
            if (l == null || (dead != null && i < dead.Length && dead[i])) continue;
            forced.Add(l);
            forcedOn.Add(l.enabled);
            forcedIntensity.Add(l.intensity);
            l.enabled = true;
            if (baseIntensity != null && i < baseIntensity.Length && baseIntensity[i] > 0f) l.intensity = baseIntensity[i];
        }
    }

    void RestoreForced()
    {
        for (var i = forced.Count - 1; i >= 0; i--)
        {
            var l = forced[i];
            if (l == null) continue;
            l.intensity = forcedIntensity[i];
            l.enabled = forcedOn[i];
        }
        forced.Clear();
        forcedOn.Clear();
        forcedIntensity.Clear();
    }
}
