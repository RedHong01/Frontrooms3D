// D2 (b) (Red 2026-10-22 12:36, "G14 的 D1、D6、D2 全部按推荐"; research/glass/rt/NOTE_1022_red_decisions.md): the
// player's body in the traced reflections. Without it a dark-beyond window became a mirror with nobody where the player
// stands, and in a lit window the Relay 2 m behind showed as a ghost exactly where the player's own reflection should hide it
// (30_final.md §5.3, 40_for_red.md "What D2 fixes"). Built in the g14-followup clone (10-22), ported onto main 2026-10-29
// (Unity 6.6, after the D1 (c) GPU-core gate landed) as the glass_rt_body_proxy package.
//
// The proxy is a dark, body-sized 1990 office worker (suit jacket with padded shoulders, trousers, shoes, a short haircut, no
// face detail; FrontRoomsGlassRTBodyProxyMesh.cs, 2,540 vertices / 4,612 triangles, built in Blender by
// Tools/rt/body_proxy/build_body_proxy.py) standing on the player's feet, eye on the eye. It is ONE dynamic instance (flags
// Dynamic | ReflectionOnly) in G14's TLAS on the reflection-only mask FR_MASK_REFLONLY 0x10, which reflection rays include and
// shadow rays (FR_RAYMASK_SHADOW) and primary / parity rays (FR_RAYMASK_PRIMARY) leave out; the plugin already had the mask,
// so nothing native changes. Its renderer is forceRenderingOff and on rendering layer mask 0, so no camera, shadow, depth or
// motion-vector pass ever draws it; it casts and receives no raster shadow; it has no collider (physics, the Relay's sight and
// the aim ray never meet it), and the game has no minimap.
//
// Gate: the proxy exists only while G14 traces. It is made by the first traced frame and placed inside each traced frame;
// every frame G14 does not trace (Quality Off: the D1 (c) default on a GPU under 16 cores or at quality level < 3, the
// settings, -frGlassRT off; no plugin; kernel or layout not ready; a latched failure) and every frame of the reduced glass tier
// (FrontRoomsGlassTypeProfile.ReducedTier) takes it out (inactive). WebGL, iOS and Windows never compile this file.
//
// Pose, per traced camera, right before the frame's dynamic transforms are read (no lag): the body is the CharacterController
// the camera rides on (FrontRooms3DGame: Camera -> "Eye" pivot -> "Player"): feet on the capsule's bottom, on its axis; the
// height follows the eye pivot (BaseEye: eye height minus the stance drop) so the head stays on the eye and the feet on the
// floor (the mesh is scaled vertically by eye / 1.62; never floating, never through the floor); the yaw follows the camera's.
// The body also follows the render camera's own offset from BaseEye (a shot's lunge, brace and flinch, a lean), clamped to
// 0.25 m sideways and -0.25 / +0.10 m vertically so a full-pose shot never drags it away. A drop of the eye deeper than 0.20 m
// below standing (the 0.6 s window-climb duck, a door-unlock dip) has no honest pose with one rigid mesh (a uniform squash reads
// as a short squat man), so the body leaves the TLAS for those frames; up to 0.20 m it is squashed by at most 12 % (a knee
// bend). A camera without a CharacterController above it (the title stream before the run) gets the body at eye height 1.62
// below it. No animation: the game has no gait lean or body animation to follow (crouch and gait are not modelled, as Red's
// D2 (b) says). The transform is only rewritten when the pose moved by more than 2 mm / 0.2 deg / 0.2 % scale, so a player
// who stands still keeps G14's TLAS reuse (the plugin reuses the TLAS only while every instance descriptor is unchanged).
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
using UnityEngine;
using UnityEngine.Rendering;

static partial class FrontRoomsGlassRTSystem
{
    /// <summary>Harness A/B only (the cost and the camera-only checks): false takes the proxy out of the TLAS (its GameObject
    /// is deactivated). The game never changes it.</summary>
    internal static bool BodyProxyOn = true;

    /// <summary>The mesh is modelled with its eye point at (0, ModelEye, 0) and its feet on y = 0, facing +Z
    /// (FrontRooms.Map.ModuleUnits.PlayerEye = 1.62 m, PlayerHeight = 1.75 m).</summary>
    internal const float BodyModelEye = 1.62f;

    /// <summary>How far the body may follow the render camera's offset from BaseEye: sideways, down, up (m).</summary>
    internal const float BodyFollowSide = .25f, BodyFollowDown = .25f, BodyFollowUp = .10f;
    /// <summary>An eye more than this far below standing (BodyModelEye) leaves the body out of the reflections (m).</summary>
    internal const float BodyHideDrop = .20f;
    /// <summary>The pose is rewritten only past these steps (m, degrees, scale), so a still player keeps the TLAS reuse.</summary>
    internal const float BodyMoveStep = .002f, BodyYawStep = .2f, BodyScaleStep = .002f;

    static GameObject bodyGo;
    static MeshRenderer bodyRenderer;
    static Mesh bodyMesh;
    static Material[] bodyMaterials;
    static Inst bodyInst;
    static bool bodyFailed;
    static Transform bodyCamParent;
    static CharacterController bodyController;
    static bool bodyPlaced;
    static Vector3 bodyLastFeet;
    static float bodyLastYaw, bodyLastScale;

    // harness read-outs (the last placement)
    internal static Renderer BodyProxyRenderer => bodyRenderer;
    internal static uint BodyProxyMask => bodyInst != null && bodyInst.alive ? bodyInst.mask : 0u;
    internal static uint BodyProxyFlags => bodyInst != null && bodyInst.alive ? bodyInst.flags : 0u;
    internal static bool BodyProxyActive => bodyGo != null && bodyGo.activeSelf;
    internal static Vector3 BodyProxyFeet { get; private set; }
    internal static float BodyProxyEyeAbove { get; private set; }
    internal static float BodyProxyScaleY { get; private set; }
    internal static float BodyProxyYaw { get; private set; }
    internal static bool BodyProxyOnRig { get; private set; }
    internal static int BodyProxyPlacements { get; private set; }
    internal static int BodyProxyWrites { get; private set; }       // placements that rewrote the transform
    internal static int BodyProxyHides { get; private set; }        // frames G14 did not trace (or the reduced tier) with the proxy up
    internal static bool BodyProxyHidden { get; private set; }      // the last placement left it out (eye drop > BodyHideDrop)
    internal static Vector3 BodyProxyCamOffset { get; private set; } // the render camera's offset from BaseEye it followed (clamped)
    internal static long BodyProxyTicks { get; private set; }

    // Material colours (sRGB), all dark and matte (smoothness 0.15-0.3): a 1990 charcoal-navy wool suit, a dark grey shirt,
    // near-black shoes, hair and tie, and skin kept as dark as the suit's light side (art review #3 and #11 of the follow-up:
    // the earlier #6E584B skin and #868784 shirt were mid values; lit by the troffers the head matched the wallpaper it covered).
    static readonly Color SuitColor = new Color32(0x2B, 0x2E, 0x36, 0xFF);
    static readonly Color DarkColor = new Color32(0x17, 0x15, 0x16, 0xFF);
    static readonly Color SkinColor = new Color32(0x3A, 0x2F, 0x29, 0xFF);
    static readonly Color ShirtColor = new Color32(0x4A, 0x4C, 0x50, 0xFF);

    /// <summary>The render camera's offset from BaseEye that the body follows: sideways clamped to BodyFollowSide, vertically
    /// to -BodyFollowDown .. +BodyFollowUp. Pure (EditMode test).</summary>
    internal static Vector3 BodyFollow(Vector3 cameraOffset)
    {
        var side = new Vector2(cameraOffset.x, cameraOffset.z);
        if (side.sqrMagnitude > BodyFollowSide * BodyFollowSide) side = side.normalized * BodyFollowSide;
        return new Vector3(side.x, Mathf.Clamp(cameraOffset.y, -BodyFollowDown, BodyFollowUp), side.y);
    }

    /// <summary>The body's yaw and vertical scale for an eye eyeAbove metres over the feet and a camera looking along
    /// camForward (camUp breaks the tie straight up / down). False = no honest pose (an eye drop deeper than BodyHideDrop):
    /// the body leaves the reflections. Pure (EditMode test).</summary>
    internal static bool BodyPose(float eyeAbove, Vector3 camForward, Vector3 camUp, out float yaw, out float scaleY)
    {
        yaw = 0f; scaleY = 1f;
        if (BodyModelEye - eyeAbove > BodyHideDrop) return false;
        var fwd = camForward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-6f) { fwd = camForward.y < 0f ? camUp : -camUp; fwd.y = 0f; }   // looking straight down / up
        yaw = fwd.sqrMagnitude > 1e-8f ? Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg : 0f;
        scaleY = Mathf.Clamp(eyeAbove / BodyModelEye, (BodyModelEye - BodyHideDrop) / BodyModelEye, 1.15f);
        return true;
    }

    /// <summary>True when the new pose differs from the last written one by more than the steps (or nothing is written yet).
    /// Pure (EditMode test).</summary>
    internal static bool BodyPoseMoved(bool placed, Vector3 lastFeet, float lastYaw, float lastScale, Vector3 feet, float yaw, float scale)
    {
        return !placed || (feet - lastFeet).sqrMagnitude > BodyMoveStep * BodyMoveStep
               || Mathf.Abs(Mathf.DeltaAngle(lastYaw, yaw)) > BodyYawStep || Mathf.Abs(scale - lastScale) > BodyScaleStep;
    }

    /// <summary>A frame G14 does not trace (RT Off by the D1 (c) default, the settings or -frGlassRT; no plugin; not ready; a
    /// latched failure): the proxy goes out with it. A null check while it was never made.</summary>
    static void HideBodyProxy()
    {
        if (bodyGo == null || !bodyGo.activeSelf) return;
        bodyGo.SetActive(false);
        bodyPlaced = false;
        BodyProxyHides++;
    }

    static void PlaceBodyProxy(Camera cam)
    {
        if (bodyFailed || cam == null || !Application.isPlaying) return;
        var t0 = System.Diagnostics.Stopwatch.GetTimestamp();
        // the reduced glass tier (WebGL look in the editor's lab switch, handheld once it joins) never shows the body
        if (!BodyProxyOn || FrontRoomsGlassTypeProfile.ReducedTier)
        {
            if (bodyGo != null && bodyGo.activeSelf) { bodyGo.SetActive(false); bodyPlaced = false; }
            return;
        }
        if (bodyRenderer == null && !MakeBodyProxy()) return;
        if (bodyInst == null || !bodyInst.alive || bodyInst.r != bodyRenderer)
        {
            if (!bodyGo.activeSelf) bodyGo.SetActive(true);   // registered while active; a deep drop takes it out below
            bodyPlaced = false;
            bodyInst = RegisterRenderer(bodyRenderer, FrontRoomsGlassRTFlags.Dynamic | FrontRoomsGlassRTFlags.ReflectionOnly, -1, null, true);
            if (bodyInst == null) { bodyFailed = true; Debug.LogWarning("[FrontRoomsGlassRT] body proxy: registration failed; reflections show no body this session"); return; }
        }
        // the layer must be one this camera renders (G14 keeps only instances on the camera's culling mask)
        var mask = cam.cullingMask;
        if ((mask & (1 << bodyGo.layer)) == 0)
        {
            var layer = 0;
            while (layer < 31 && (mask & (1 << layer)) == 0) layer++;
            bodyGo.layer = layer;
            bodyInst.layer = layer;
        }

        var ct = cam.transform;
        if (ct.parent != bodyCamParent || (bodyController == null && !ReferenceEquals(bodyController, null)))
        {
            bodyCamParent = ct.parent;
            bodyController = ct.GetComponentInParent<CharacterController>(true);
        }
        Vector3 feet;
        float eyeAbove;
        var follow = Vector3.zero;
        if (bodyController != null)
        {
            var bt = bodyController.transform;
            var axis = bt.TransformPoint(bodyController.center);
            feet = new Vector3(axis.x, axis.y - bodyController.height * .5f * bt.lossyScale.y, axis.z);
            // BaseEye: the rig's eye pivot (the camera's parent under the body), else the camera itself
            var eyeT = ct.parent != null && ct.parent != bt && ct.parent.IsChildOf(bt) ? ct.parent : ct;
            eyeAbove = eyeT.position.y - feet.y;
            // the render camera's picture-only offset from BaseEye (a shot's lunge / brace / flinch, a lean), clamped
            follow = BodyFollow(ct.position - eyeT.position);
            feet += new Vector3(follow.x, 0f, follow.z);
            eyeAbove += follow.y;
            BodyProxyOnRig = true;
        }
        else
        {
            var p = ct.position;
            feet = new Vector3(p.x, p.y - BodyModelEye, p.z);
            eyeAbove = BodyModelEye;
            BodyProxyOnRig = false;
        }
        BodyProxyCamOffset = follow;
        BodyProxyFeet = feet; BodyProxyEyeAbove = eyeAbove;
        BodyProxyPlacements++;
        // a drop with no honest rigid pose (the climb duck, an unlock dip): out of the TLAS for those frames, never squashed
        if (!BodyPose(eyeAbove, ct.forward, ct.up, out var yaw, out var sy))
        {
            if (bodyGo.activeSelf) bodyGo.SetActive(false);
            bodyPlaced = false;
            BodyProxyHidden = true;
            BodyProxyTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0;
            return;
        }
        BodyProxyHidden = false;
        if (!bodyGo.activeSelf) { bodyGo.SetActive(true); bodyPlaced = false; }
        if (BodyPoseMoved(bodyPlaced, bodyLastFeet, bodyLastYaw, bodyLastScale, feet, yaw, sy))
        {
            var bt0 = bodyGo.transform;
            bt0.SetPositionAndRotation(feet, Quaternion.Euler(0f, yaw, 0f));
            bt0.localScale = new Vector3(1f, sy, 1f);
            bodyPlaced = true; bodyLastFeet = feet; bodyLastYaw = yaw; bodyLastScale = sy;
            BodyProxyWrites++;
        }
        BodyProxyScaleY = bodyLastScale; BodyProxyYaw = bodyLastYaw;
        BodyProxyTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0;
    }

    static bool MakeBodyProxy()
    {
        if (bodyGo != null) Object.Destroy(bodyGo);
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Simple Lit");
        if (shader == null) { bodyFailed = true; Debug.LogWarning("[FrontRoomsGlassRT] body proxy: no URP Lit shader in this build; reflections show no body"); return false; }
        if (bodyMesh == null) bodyMesh = FrontRoomsGlassRTBodyProxyMesh.Build();
        if (bodyMesh == null) { bodyFailed = true; return false; }
        if (bodyMaterials == null)
        {
            // submesh order of FrontRoomsGlassRTBodyProxyMesh: suit (jacket, sleeves, trousers), dark (shoes, hair, tie), skin, shirt
            bodyMaterials = new[]
            {
                BodyMaterial(shader, "Body proxy / suit", SuitColor, .18f),
                BodyMaterial(shader, "Body proxy / shoes, hair, tie", DarkColor, .25f),
                BodyMaterial(shader, "Body proxy / skin", SkinColor, .30f),
                BodyMaterial(shader, "Body proxy / shirt", ShirtColor, .15f),
            };
        }
        bodyGo = new GameObject("FrontRooms glass RT body proxy (reflection only)");
        Object.DontDestroyOnLoad(bodyGo);   // a restart reloads the scene; the proxy and its registration stay
        bodyGo.AddComponent<MeshFilter>().sharedMesh = bodyMesh;
        bodyRenderer = bodyGo.AddComponent<MeshRenderer>();
        bodyRenderer.sharedMaterials = bodyMaterials;
        ConfigureBodyRenderer(bodyRenderer);
        bodyInst = null;
        bodyPlaced = false;
        return true;
    }

    /// <summary>Never drawn by any raster pass, never a raster shadow caster or receiver (EditMode test reads it back).</summary>
    internal static void ConfigureBodyRenderer(MeshRenderer r)
    {
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = LightProbeUsage.Off;
        r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        r.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        r.allowOcclusionWhenDynamic = false;
        r.renderingLayerMask = 0u;      // no DrawRenderers filter can match it
        r.forceRenderingOff = true;     // and Unity culls it from every camera, shadow and depth pass
    }

    static Material BodyMaterial(Shader shader, string name, Color color, float smoothness)
    {
        var m = new Material(shader) { name = name };
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", smoothness);
        m.SetFloat("_Metallic", 0f);
        return m;
    }

    /// <summary>A new Play session / domain: the registry was cleared, so the proxy registers again on the next traced frame.</summary>
    static void ResetBodyProxy()
    {
        bodyInst = null;
        bodyFailed = false;
        bodyCamParent = null;
        bodyController = null;
        bodyPlaced = false;
        BodyProxyPlacements = 0;
        BodyProxyWrites = 0;
        BodyProxyHides = 0;
        BodyProxyTicks = 0;
        BodyProxyHidden = false;
    }

    /// <summary>Play-mode exit (editor): the proxy's objects go with the session (a player simply ends).</summary>
    static void ReleaseBodyProxy()
    {
        if (bodyGo != null) Object.DestroyImmediate(bodyGo);
        if (bodyMesh != null) Object.DestroyImmediate(bodyMesh);
        if (bodyMaterials != null) foreach (var m in bodyMaterials) if (m != null) Object.DestroyImmediate(m);
        bodyGo = null; bodyRenderer = null; bodyMesh = null; bodyMaterials = null;
        ResetBodyProxy();
    }
}
#endif
