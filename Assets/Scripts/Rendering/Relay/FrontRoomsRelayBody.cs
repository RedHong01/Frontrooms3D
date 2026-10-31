using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The production Relay body, "Night Shift" (research/hunter/production_b/10_plan.md sections 1-5): one skinned mesh
/// (Resources/Creatures/Relay_NightShift.fbx, LOD0/1/2 on one 36-bone armature) posed every frame by
/// <see cref="FrontRoomsRelayRig"/>, which keeps its public API and only forwards to this when the asset exists.
///
/// Per frame, in the body's own model space:
///   1. room pose     low / std / tall keys blended from the smoothed CeilingHeight (folds in 0.25 s, unfolds in
///                    1.0 s); in std the Listen key (right glove on a ceiling tile) while listening or searching;
///                    every pinned key twice, neck long and face +0.3, blended by faceLift (Red's open neck call);
///   2. door squeeze  door_in -> door_mid -> door_out from DoorSqueeze and its direction of change, plus a rig-only
///                    lintel probe that starts the duck before the map's value rises (the hood leads the nav axis);
///   3. layers        gait arm swing, BreakDoor hammer, Stagger, latched Listen/Search head snaps (15 deg, holds of
///                    0.4-0.9 s, 0.08 s snaps), the Run head-lock, the catch (camera clearance and the reach);
///   4. constraints   the ceiling pin (pelvis moved so the shrug sits 0.02 m under the ceiling, the knees take it),
///                    foot locking and two-bone leg IK, so no stance foot slides;
///   5. lens          FrontRoomsRelayLens (state only, never distance) into a per-material property block and a
///                    point light (Walk light desktop only);
///   6. contacts      Step at each foot plant (the boot's ball), CeilingBrush at the step's peak while pinned
///                    (>= 0.5 s apart), Squeeze from real collider contact (fallback: the old threshold rule).
/// No allocation per frame after Bind. No colliders on the body.
/// </summary>
[DisallowMultipleComponent]
public sealed class FrontRoomsRelayBody : MonoBehaviour
{
    public const string ResourcePath = "Creatures/Relay_NightShift";
    public const string PosesPath = "Creatures/Relay_NightShift_Poses";
    public const string ChildName = "RELAY BODY / Night Shift";
    public static readonly string[] Slots = { "Creature_TwillSpruce", "Creature_LensOpal", "Creature_TapeSilver" };
    public const int LensMaterialIndex = 1;

    /// <summary>The viewer for the head-lock and the camera clearance (tools and tests). Null: the active game camera.</summary>
    public static Transform ViewerOverride;
    /// <summary>Tools and tests: run the lintel probe and the squeeze contacts in edit mode too (play mode always does).</summary>
    public static bool PhysicsInEditMode;
    /// <summary>Tools: log every bind head when the axis map fails.</summary>
    public static bool LogBindDetail;

    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    // ------------------------------------------------------------------ bound data
    FrontRoomsRelayPoseSet set;
    Transform[] bones;
    Vector3[] bindPos;
    Quaternion[] bindRot;
    int[] parent;
    int boneCount;
    List<FrontRoomsRelayPoseSet.Key> keys;
    float modelScale = 1f;
    Vector3 up = Vector3.up, forward = Vector3.forward, left = Vector3.left;

    int bRoot, bPelvis, bChest, bShrug, bNeck1, bNeck2, bNeck3, bHead;
    int bThighL, bShinL, bFootL, bToeL, bThighR, bShinR, bFootR, bToeR, bArmL, bArmR, bHandR;
    int[] subPelvis, subNeck2, subNeck3, subHead, subArmL, subArmR, subChest;
    float thighLen, shinLen;

    struct Locator { public int bone; public Vector3 offset; public Vector3 dir; }
    Locator locLens, locHoodTop, locHoodLow, locHoodCentre, locBallL, locBallR, locShoulderR, locHips;
    Locator[] topMarkers;

    FrontRoomsRelayPoseSet.Key kStdL, kStdF, kListenL, kListenF, kLowL, kLowF, kTall, kDoorIn, kDoorMid, kDoorOut;
    readonly FrontRoomsRelayPoseSet.Key[] slotKey = new FrontRoomsRelayPoseSet.Key[12];
    readonly float[] slotW = new float[12];
    int slotCount;

    // per-frame working arrays
    Quaternion[] D;
    Vector3[] O, P;
    Quaternion[] R;

    SkinnedMeshRenderer[] renderers;
    bool[] lensSlot;
    LODGroup lodGroup;
    Light lensLight;
    MaterialPropertyBlock block;
    float lastLensLevel = -1f;
    FrontRoomsRelayGlassMirror mirror;

    // ------------------------------------------------------------------ state
    readonly FrontRoomsRelayLens lens = new FrontRoomsRelayLens();
    bool initialised;
    Vector3 lastWorldPos;
    float speed;
    Vector3 velocityDir;
    float smoothHeight = 2.9f;
    float listenW;
    float time;
    FrontRoomsRelayRig.MotionState lastState;

    // door
    bool crossing;
    int crossingSign;           // -1 approaching (door ahead), +1 leaving (door behind)
    float lastSqueeze, peakSqueeze, doorS = -10f, doorW, probeDist = -1f, probeAge = 10f, probeTimer;
    // squeeze contacts
    float squeezeTimer, squeezeRaisedAt;
    bool squeezeRaised, squeezeContact;
    Vector3 squeezePoint;
    readonly Collider[] overlap = new Collider[8];
    readonly RaycastHit[] rayHits = new RaycastHit[8];

    // gait
    float phase, gaitW;
    readonly bool[] stance = { true, true };
    readonly Vector3[] lockWorld = new Vector3[2];
    readonly Vector3[] swingFrom = new Vector3[2];
    readonly Vector3[] footTarget = new Vector3[2];
    readonly float[] lastFootPhase = { 0f, .5f };
    readonly bool[] landed = new bool[2];
    bool gaitLocked;
    float brushCooldown;
    bool brushArmed;

    // head
    float headYaw, headYawTarget, latchTimer, snapFrom, snapT = 1f;
    int latchCount;
    float clearance, catchW, lensFacing;
    Transform viewer;
    float viewerRefresh;

    // outputs
    public bool PressesCeiling { get; private set; }
    public float LensLevel => lens.Level;
    public FrontRoomsRelayLens Lens => lens;
    public float SmoothedCeiling => smoothHeight;
    public float DoorWeight => doorW;
    public float ListenWeight => listenW;
    public float DoorPhaseS => doorS;
    public float Top { get; private set; }
    public float PinOffset { get; private set; }
    public float Speed => speed;
    public bool Bound => set != null;
    public int TransitionsInWindow => lens.TransitionsInWindow;
    /// <summary>Wall-clock cost of the last Tick in ms (for the budget check; UNDER LOAD numbers compare only relatively).</summary>
    public double LastTickMs { get; private set; }

    Action<int, Vector3> onStep;
    Action<Vector3> onBrush;
    Action<Vector3, float> onSqueeze;
    readonly System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

    // ------------------------------------------------------------------ creation
    /// <summary>The prefab and pose file, or null (the rig then keeps the primitive body).</summary>
    public static GameObject LoadPrefab() => Resources.Load<GameObject>(ResourcePath);

    /// <summary>Instantiates the body under the rig. Null when the asset or its pose file is missing or does not bind.</summary>
    public static FrontRoomsRelayBody Create(Transform rig, bool preview)
    {
        var prefab = LoadPrefab();
        var json = Resources.Load<TextAsset>(PosesPath);
        if (prefab == null || json == null) return null;
        var poses = FrontRoomsRelayPoseSet.Load(json);
        if (poses == null) return null;
        var go = Instantiate(prefab, rig, false);
        go.name = ChildName;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        if (preview) SetHideFlags(go, HideFlags.DontSave);
        var body = go.AddComponent<FrontRoomsRelayBody>();
        if (preview) body.hideFlags = HideFlags.DontSave;
        if (!body.Bind(poses))
        {
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
            return null;
        }
        return body;
    }

    static void SetHideFlags(GameObject go, HideFlags flags)
    {
        go.hideFlags = flags;
        foreach (Transform t in go.transform) SetHideFlags(t.gameObject, flags);
    }

    public void SetCallbacks(Action<int, Vector3> step, Action<Vector3> brush, Action<Vector3, float> squeeze)
    {
        onStep = step; onBrush = brush; onSqueeze = squeeze;
    }

    // ------------------------------------------------------------------ bind
    /// <summary>Finds the bones, measures the Blender -> model axis map and converts every key.</summary>
    public bool Bind(FrontRoomsRelayPoseSet poses)
    {
        set = null;
        boneCount = poses.BoneCount;
        bones = new Transform[boneCount];
        var found = new Dictionary<string, Transform>();
        foreach (var t in GetComponentsInChildren<Transform>(true)) if (!found.ContainsKey(t.name)) found[t.name] = t;
        for (var i = 0; i < boneCount; i++)
        {
            if (!found.TryGetValue(poses.BoneNames[i], out bones[i]))
            {
                Debug.LogWarning("[FrontRoomsRelayBody] bone " + poses.BoneNames[i] + " is missing from " + name + "; the primitive Relay stays.");
                return false;
            }
        }
        parent = poses.Parent;
        bindPos = new Vector3[boneCount];
        bindRot = new Quaternion[boneCount];
        var inv = Quaternion.Inverse(transform.rotation);
        for (var i = 0; i < boneCount; i++)
        {
            bindPos[i] = transform.InverseTransformPoint(bones[i].position);
            bindRot[i] = inv * bones[i].rotation;
        }
        if (!poses.MeasureAxes(bindPos, out var axes, out modelScale, out var origin, out var residual))
        {
            if (LogBindDetail)
                for (var i = 0; i < boneCount; i++)
                    Debug.Log("[FrontRoomsRelayBody] bind " + poses.BoneNames[i] + " blender " + FrontRoomsRelayPoseSet.V(poses.Data.bones[i].bind).ToString("F3") + " model " + bindPos[i].ToString("F3") + " parent " + (bones[i].parent != null ? bones[i].parent.name : "-"));
            Debug.LogWarning("[FrontRoomsRelayBody] the bind heads do not match the pose file (residual " + residual.ToString("0.0000") + " m); the primitive Relay stays.");
            return false;
        }
        up = axes.MultiplyVector(new Vector3(0f, 0f, 1f)).normalized;
        forward = axes.MultiplyVector(new Vector3(0f, -1f, 0f)).normalized;      // the figure faces Blender -Y
        left = axes.MultiplyVector(new Vector3(1f, 0f, 0f)).normalized;          // its left is Blender +X
        if (Vector3.Dot(up, Vector3.up) < .99f || Vector3.Dot(forward, Vector3.forward) < .99f)
            Debug.LogWarning("[FrontRoomsRelayBody] unexpected model axes up " + up + " forward " + forward + " (expected +Y / +Z).");
        keys = poses.Convert(axes, modelScale, origin);
        kStdL = Find("std", 0); kStdF = Find("std", 1);
        kListenL = Find("listen_std", 0); kListenF = Find("listen_std", 1);
        kLowL = Find("low", 0); kLowF = Find("low", 1);
        kTall = Find("tall", 0);
        kDoorIn = Find("door_in", 0); kDoorMid = Find("door_mid", 0); kDoorOut = Find("door_out", 0);
        if (kStdL == null || kLowL == null || kTall == null || kDoorIn == null || kDoorMid == null || kDoorOut == null)
        {
            Debug.LogWarning("[FrontRoomsRelayBody] the pose file lacks a key; the primitive Relay stays.");
            return false;
        }
        if (kStdF == null) kStdF = kStdL;
        if (kListenL == null) kListenL = kStdL;
        if (kListenF == null) kListenF = kListenL == kStdL ? kStdF : kListenL;
        if (kLowF == null) kLowF = kLowL;

        bRoot = poses.IndexOf("root"); bPelvis = poses.IndexOf("pelvis"); bChest = poses.IndexOf("chest"); bShrug = poses.IndexOf("shrug");
        bNeck1 = poses.IndexOf("neck_01"); bNeck2 = poses.IndexOf("neck_02"); bNeck3 = poses.IndexOf("neck_03"); bHead = poses.IndexOf("head");
        bThighL = poses.IndexOf("thigh_L"); bShinL = poses.IndexOf("shin_L"); bFootL = poses.IndexOf("foot_L"); bToeL = poses.IndexOf("toe_L");
        bThighR = poses.IndexOf("thigh_R"); bShinR = poses.IndexOf("shin_R"); bFootR = poses.IndexOf("foot_R"); bToeR = poses.IndexOf("toe_R");
        bArmL = poses.IndexOf("upperarm_L"); bArmR = poses.IndexOf("upperarm_R"); bHandR = poses.IndexOf("hand_R");
        if (bRoot < 0 || bPelvis < 0 || bHead < 0 || bThighL < 0 || bThighR < 0 || bFootL < 0 || bFootR < 0 || bArmL < 0 || bArmR < 0 || bNeck2 < 0 || bNeck3 < 0)
        {
            Debug.LogWarning("[FrontRoomsRelayBody] the armature lacks a required bone; the primitive Relay stays.");
            return false;
        }
        subPelvis = Subtree(bPelvis); subNeck2 = Subtree(bNeck2); subNeck3 = Subtree(bNeck3); subHead = Subtree(bHead);
        subArmL = Subtree(bArmL); subArmR = Subtree(bArmR); subChest = Subtree(bChest);
        thighLen = .5f * ((bindPos[bShinL] - bindPos[bThighL]).magnitude + (bindPos[bShinR] - bindPos[bThighR]).magnitude);
        shinLen = .5f * ((bindPos[bFootL] - bindPos[bShinL]).magnitude + (bindPos[bFootR] - bindPos[bShinR]).magnitude);

        set = poses;
        var locs = poses.Data.locators ?? Array.Empty<FrontRoomsRelayPoseSet.LocatorData>();
        var tops = new List<Locator>();
        foreach (var l in locs)
        {
            var b = poses.IndexOf(l.bone);
            if (b < 0) continue;
            var loc = new Locator
            {
                bone = b,
                offset = modelScale * axes.MultiplyVector(FrontRoomsRelayPoseSet.V(l.pos) - FrontRoomsRelayPoseSet.V(poses.Data.bones[b].bind)),
                dir = axes.MultiplyVector(FrontRoomsRelayPoseSet.V(l.dir)).normalized
            };
            switch (l.name)
            {
                case "lens": locLens = loc; break;
                case "hood_front_top": locHoodTop = loc; break;
                case "hood_front_low": locHoodLow = loc; break;
                case "hood_centre": locHoodCentre = loc; break;
                case "ball_L": locBallL = loc; break;
                case "ball_R": locBallR = loc; break;
                case "shoulder_R": locShoulderR = loc; break;
                case "hips": locHips = loc; break;
                default: if (l.name.StartsWith("top_", StringComparison.Ordinal)) tops.Add(loc); break;
            }
        }
        // the shrug and hood tops always count
        tops.Add(new Locator { bone = bShrug >= 0 ? bShrug : bChest, offset = Vector3.zero, dir = up });
        topMarkers = tops.ToArray();

        D = new Quaternion[boneCount]; O = new Vector3[boneCount]; P = new Vector3[boneCount]; R = new Quaternion[boneCount];
        SetUpRenderers();
        initialised = false;
        lastLensLevel = -1f;
        return true;
    }

    FrontRoomsRelayPoseSet.Key Find(string keyName, int face)
    {
        foreach (var k in keys) if (k.name == keyName && k.faceLift == face) return k;
        return null;
    }

    int[] Subtree(int root)
    {
        var list = new List<int> { root };
        for (var i = 0; i < boneCount; i++)
        {
            var p = parent[i];
            while (p >= 0) { if (p == root) { list.Add(i); break; } p = parent[p]; }
        }
        return list.ToArray();
    }

    void SetUpRenderers()
    {
        renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        lensSlot = new bool[renderers.Length];
        if (block == null) block = new MaterialPropertyBlock();
        lodGroup = GetComponentInChildren<LODGroup>(true);
        var mats = new Material[Slots.Length];
        for (var i = 0; i < Slots.Length; i++) mats[i] = FrontRoomsSurfaces.TryGet(Slots[i]);
        // the pose bounds: x +-1.0, y 0-3.4, z +-1.2 (plan section 4), widened for the 80 deg door yaw and the reach
        var corners = new Vector3[8];
        var k = 0;
        for (var ix = 0; ix < 2; ix++) for (var iy = 0; iy < 2; iy++) for (var iz = 0; iz < 2; iz++)
            corners[k++] = new Vector3(ix == 0 ? -1.35f : 1.35f, iy == 0 ? -.05f : 3.5f, iz == 0 ? -1.35f : 1.45f);
        for (var ri = 0; ri < renderers.Length; ri++)
        {
            var r = renderers[ri];
            var shared = r.sharedMaterials;
            lensSlot[ri] = shared.Length > LensMaterialIndex;
            for (var s = 0; s < shared.Length && s < mats.Length; s++)
                if (mats[s] != null && (shared[s] == null || shared[s].name.StartsWith(Slots[s], StringComparison.Ordinal) || shared[s].shader == null || shared[s].shader.name != "FrontRooms/Surface")) shared[s] = mats[s];
            r.sharedMaterials = shared;
            r.updateWhenOffscreen = false;
            r.skinnedMotionVectors = true;
            var space = r.rootBone != null ? r.rootBone : r.transform;
            var b = new Bounds(space.InverseTransformPoint(transform.TransformPoint(corners[0])), Vector3.zero);
            for (var c = 1; c < 8; c++) b.Encapsulate(space.InverseTransformPoint(transform.TransformPoint(corners[c])));
            r.localBounds = b;
            var lod2 = r.name.EndsWith("_LOD2", StringComparison.Ordinal);
            r.shadowCastingMode = lod2 ? ShadowCastingMode.Off : ShadowCastingMode.On;
        }
#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL tier (production_b section 4): LOD1 at most, so no LOD0 skinning; Mac / Windows untouched.
        if (lodGroup != null)
        {
            var lods = lodGroup.GetLODs();
            if (lods.Length >= 3)
            {
                foreach (var r0 in lods[0].renderers) if (r0 != null) r0.enabled = false;
                lodGroup.SetLODs(new[] { new LOD(lods[1].screenRelativeTransitionHeight, lods[1].renderers), new LOD(0f, lods[2].renderers) });
            }
        }
#endif
        if (lodGroup != null)
        {
            // never culled: a lit lens at 30 m must still draw (darkness is never cover)
            var lods = lodGroup.GetLODs();
            if (lods.Length > 0 && lods[lods.Length - 1].screenRelativeTransitionHeight > 0f)
            {
                lods[lods.Length - 1].screenRelativeTransitionHeight = 0f;
                lodGroup.SetLODs(lods);
            }
        }
        if (lensLight == null)
        {
            var lightGo = new GameObject("lens light");
            lightGo.hideFlags = gameObject.hideFlags;
            lightGo.transform.SetParent(transform, false);
            lensLight = lightGo.AddComponent<Light>();
            lensLight.type = LightType.Spot;
            lensLight.spotAngle = 140f;
            lensLight.innerSpotAngle = 70f;
            lensLight.shadows = LightShadows.None;
            lensLight.color = FrontRoomsRelayLens.LightColour;
            lensLight.renderMode = LightRenderMode.ForcePixel;
            lensLight.enabled = false;
        }
    }

    /// <summary>Re-activation or a relay jump: gait, contacts, door latch and lens reset; the Listen pose with the lens on standby.</summary>
    public void ResetArrival(float ceiling)
    {
        initialised = true;
        lastWorldPos = transform.position;
        speed = 0f;
        velocityDir = transform.forward;
        smoothHeight = ceiling;
        listenW = 1f;
        crossing = false; crossingSign = 0; lastSqueeze = 0f; peakSqueeze = 0f; doorS = -10f; doorW = 0f; probeDist = -1f; probeAge = 10f;
        squeezeRaised = false; squeezeContact = false;
        phase = 0f; gaitW = 0f; gaitLocked = false; stance[0] = stance[1] = true;
        lastFootPhase[0] = 0f; lastFootPhase[1] = .5f;
        headYaw = headYawTarget = 0f; latchTimer = 0f; snapT = 1f; latchCount = 0;
        brushCooldown = 0f; brushArmed = false;
        lens.Reset();
    }

    /// <summary>Re-activation (the rig's OnEnable): the next Tick is an arrival.</summary>
    public void Rearm() { initialised = false; }

    public void DoorBlow(int blow, int blowCount, bool reduceFlashing)
    {
        var final = blow >= blowCount - 1;
        lens.DoorBlow(final, reduceFlashing);
        blowImpulse = 1f;
        finalBlow = final;
    }

    float blowImpulse;
    bool finalBlow;

    // ------------------------------------------------------------------ tick
    public void Tick(float deltaTime, FrontRoomsRelayRig.MotionState state, bool moving, float ceiling, float doorSqueeze, Vector3? listenTarget, float faceLift, bool reduceFlashing)
    {
        if (set == null) return;
        watch.Restart();
        var dt = Mathf.Clamp(deltaTime, 0f, .1f);
        var world = transform.position;
        if (!initialised || (world - lastWorldPos).sqrMagnitude > 9f) ResetArrival(ceiling);
        time += dt;
        var playing = Application.isPlaying || PhysicsInEditMode;

        // ---- measured ground speed (the cadence follows it, not speedScale)
        if (dt > 0f)
        {
            var delta = world - lastWorldPos;
            delta.y = 0f;
            var v = delta.magnitude / dt;
            speed = Mathf.Lerp(speed, v, 1f - Mathf.Exp(-dt / .12f));
            if (delta.sqrMagnitude > 1e-8f) velocityDir = delta.normalized;
        }
        lastWorldPos = world;
        var walking = moving && speed > .15f;

        // ---- smoothed ceiling and room weights
        var tau = ceiling < smoothHeight ? .25f : 1f;
        smoothHeight = dt > 0f ? Mathf.Lerp(smoothHeight, ceiling, 1f - Mathf.Exp(-dt / tau)) : smoothHeight;
        var h = smoothHeight;
        var low = 1f - Smooth(2.4f, 2.9f, h);
        var tall = Smooth(4.0f, 5.2f, h);
        var std = Mathf.Max(0f, 1f - low - tall);
        var listening = state == FrontRoomsRelayRig.MotionState.IdleListen || state == FrontRoomsRelayRig.MotionState.Search;
        listenW = Mathf.MoveTowards(listenW, listening ? 1f : 0f, dt / .25f);

        // ---- door phase
        UpdateDoor(dt, doorSqueeze, playing, moving);

        // ---- blend
        var f = Mathf.Clamp01(faceLift);
        slotCount = 0;
        var room = 1f - doorW;
        AddSlot(kLowL, room * low * (1f - f)); AddSlot(kLowF, room * low * f);
        AddSlot(kStdL, room * std * (1f - listenW) * (1f - f)); AddSlot(kStdF, room * std * (1f - listenW) * f);
        AddSlot(kListenL, room * std * listenW * (1f - f)); AddSlot(kListenF, room * std * listenW * f);
        AddSlot(kTall, room * tall);
        if (doorW > 0f)
        {
            FrontRoomsRelayPoseSet.Key a, b;
            float u;
            var sIn = set.Data.door.sIn; var sOut = set.Data.door.sOut;
            if (doorS <= 0f) { a = kDoorIn; b = kDoorMid; u = Smooth(sIn, 0f, doorS); }
            else { a = kDoorMid; b = kDoorOut; u = Smooth(0f, sOut, doorS); }
            AddSlot(a, doorW * (1f - u)); AddSlot(b, doorW * u);
        }
        Blend();

        // ---- additive layers (model-space rotations of whole subtrees at their root joint)
        var run = state == FrontRoomsRelayRig.MotionState.Run;
        gaitW = Mathf.MoveTowards(gaitW, walking ? 1f : 0f, dt / .3f);
        var stepLen = StepLength(low, std, tall, run);
        var amp = gaitW * (1f - .7f * doorW) * (run ? 1.6f : 1f);
        var swing = Mathf.Sin(phase * 2f * Mathf.PI);
        if (amp > 0f)
        {
            RotateSubtree(subArmL, Quaternion.AngleAxis(-swing * 13f * amp, left));
            RotateSubtree(subArmR, Quaternion.AngleAxis(swing * 13f * amp, left));
            if (bChest >= 0) RotateSubtree(subChest, Quaternion.AngleAxis(swing * 3f * amp, up));
        }
        blowImpulse = Mathf.Max(0f, blowImpulse - dt * (finalBlow ? 2.2f : 4f));
        if (state == FrontRoomsRelayRig.MotionState.BreakDoor)
        {
            // the forearm-and-fist hammer on the leaf's upper panel; the same on every blow (02 section 2.6)
            var raise = 62f - 46f * blowImpulse;
            RotateSubtree(subArmR, Quaternion.AngleAxis(raise, left));
            if (bChest >= 0) RotateSubtree(subChest, Quaternion.AngleAxis(blowImpulse * (finalBlow ? 22f : 14f), left));
        }
        if (state == FrontRoomsRelayRig.MotionState.Stagger)
        {
            var jerk = Mathf.Sin(time * 16f) * 9f;
            RotateSubtree(subHead, Quaternion.AngleAxis(jerk, up) * Quaternion.AngleAxis(Mathf.Sin(time * 13f) * 6f, forward));
            RotateSubtree(subArmL, Quaternion.AngleAxis(Mathf.Sin(time * 7f) * 8f, forward));
            RotateSubtree(subArmR, Quaternion.AngleAxis(-Mathf.Sin(time * 6f) * 8f, forward));
        }
        UpdateViewer();
        UpdateHead(dt, state, listenTarget, reduceFlashing);

        // ---- forward kinematics, then the ceiling pin
        Forward();
        var topBefore = MeasureTop();
        var pinW = Smooth(.4f, .6f, low + std) * (1f - doorW);
        var pin = Mathf.Clamp((h - .02f) - topBefore, -.12f, .02f) * pinW;
        PinOffset = pin;
        if (pin != 0f) ShiftSubtree(subPelvis, up * pin);
        Top = topBefore + pin;
        PressesCeiling = pinW >= .5f && (h - Top) <= .05f;

        // ---- the catch: camera clearance and the reach (Run or Listen, camera within 1.5 m of the head)
        Catch(state);

        // ---- gait: foot locking and leg IK
        Legs(dt, walking, stepLen, run);

        // ---- write the bones
        var rootRot = transform.rotation;
        for (var i = 0; i < boneCount; i++)
            bones[i].SetPositionAndRotation(transform.TransformPoint(P[i]), rootRot * R[i]);

        // ---- lens and light
#if UNITY_WEBGL && !UNITY_EDITOR
        lens.AllowWalkLight = false;   // WebGL tier: the Chase light only
#endif
        lens.Tick(dt, state, reduceFlashing);
        ApplyLens();

        // ---- contacts
        Contacts(dt, moving, doorSqueeze, playing, state);
        lastState = state;
        watch.Stop();
        LastTickMs = watch.Elapsed.TotalMilliseconds;
    }

    static float Smooth(float a, float b, float x)
    {
        if (Mathf.Approximately(a, b)) return x >= b ? 1f : 0f;
        var t = Mathf.Clamp01((x - a) / (b - a));
        return t * t * (3f - 2f * t);
    }

    float StepLength(float low, float std, float tall, bool run)
    {
        var g = set.Data.gait;
        return run ? low * g.stepLowRun + std * g.stepStdRun + tall * g.stepTallRun
                   : low * g.stepLow + std * g.stepStd + tall * g.stepTall;
    }

    void AddSlot(FrontRoomsRelayPoseSet.Key k, float w)
    {
        if (k == null || w <= 1e-5f || slotCount >= slotKey.Length) return;
        for (var i = 0; i < slotCount; i++) if (slotKey[i] == k) { slotW[i] += w; return; }
        slotKey[slotCount] = k;
        slotW[slotCount] = w;
        slotCount++;
    }

    void Blend()
    {
        if (slotCount == 0) { AddSlot(kStdL, 1f); }
        var total = 0f;
        var best = 0;
        for (var i = 0; i < slotCount; i++) { total += slotW[i]; if (slotW[i] > slotW[best]) best = i; }
        var inv = total > 0f ? 1f / total : 0f;
        var refKey = slotKey[best];
        for (var b = 0; b < boneCount; b++)
        {
            var r = refKey.delta[b];
            float x = 0f, y = 0f, z = 0f, w = 0f;
            var o = Vector3.zero;
            for (var i = 0; i < slotCount; i++)
            {
                var k = slotKey[i];
                var q = k.delta[b];
                var wi = slotW[i] * inv;
                if (q.x * r.x + q.y * r.y + q.z * r.z + q.w * r.w < 0f) wi = -wi;
                x += q.x * wi; y += q.y * wi; z += q.z * wi; w += q.w * wi;
                o += k.offset[b] * (slotW[i] * inv);
            }
            var m = Mathf.Sqrt(x * x + y * y + z * z + w * w);
            D[b] = m > 1e-6f ? new Quaternion(x / m, y / m, z / m, w / m) : r;
            O[b] = o;
        }
    }

    void RotateSubtree(int[] sub, Quaternion g)
    {
        for (var i = 0; i < sub.Length; i++) D[sub[i]] = g * D[sub[i]];
    }

    void ShiftSubtree(int[] sub, Vector3 by)
    {
        for (var i = 0; i < sub.Length; i++) P[sub[i]] += by;
    }

    void Forward()
    {
        for (var b = 0; b < boneCount; b++)
        {
            var p = parent[b];
            P[b] = p < 0 ? O[b] : P[p] + D[p] * O[b];
            R[b] = D[b] * bindRot[b];
        }
    }

    Vector3 LocatorModel(Locator l) => P[l.bone] + D[l.bone] * l.offset;
    Vector3 LocatorDir(Locator l) => D[l.bone] * l.dir;

    float MeasureTop()
    {
        var top = float.MinValue;
        for (var i = 0; i < topMarkers.Length; i++) top = Mathf.Max(top, Vector3.Dot(LocatorModel(topMarkers[i]), up));
        // the hood shell is the top in the tall pose: its front top point
        top = Mathf.Max(top, Vector3.Dot(LocatorModel(locHoodTop), up));
        return top;
    }

    // ------------------------------------------------------------------ door
    void UpdateDoor(float dt, float d, bool playing, bool moving)
    {
        // The header probe (rig-only). The std hood leads the nav axis by ~0.9 m and the body is 2.86 m tall, so it must be
        // squeezing before the map's DoorSqueeze rises (0.6 m reach), and under every low header, not only doors: the map's
        // doorless arches (1.1-1.8 m wide, top 2.2 m) never raise DoorSqueeze at all. A header is a wall band at 2.3 m that
        // runs past at least one edge of an opening (a ray 0.95 m to the side hits the same plane) over a gap clear at
        // 1.95 m and 1.0 m. A hanging sign, a cubicle or a plain wall is not. Probed at 10 Hz while moving; the distance is
        // dead-reckoned between probes; at the plane a crossing starts and is dead-reckoned through, as after a door.
        probeAge += dt;
        if (probeDist > 0f) probeDist = Mathf.Max(0f, probeDist - Mathf.Max(speed, 0f) * dt);
        if (playing && moving)
        {
            probeTimer -= dt;
            if (probeTimer <= 0f)
            {
                probeTimer = .1f;
                var origin = transform.position;
                var fwd = transform.forward; fwd.y = 0f;
                if (fwd.sqrMagnitude > 1e-6f)
                {
                    fwd.Normalize();
                    var side = Vector3.Cross(Vector3.up, fwd);
                    const float range = 2.3f;
                    var head = Nearest(origin + Vector3.up * 2.3f, fwd, range);
                    if (head > 0f)
                    {
                        var l = Nearest(origin + Vector3.up * 2.3f - side * .95f, fwd, range);
                        var r = Nearest(origin + Vector3.up * 2.3f + side * .95f, fwd, range);
                        var mid = Nearest(origin + Vector3.up * 1.95f, fwd, range);
                        var lo = Nearest(origin + Vector3.up * 1.0f, fwd, range);
                        var band = (l > 0f && Mathf.Abs(l - head) < .3f) || (r > 0f && Mathf.Abs(r - head) < .3f);
                        var open = (mid < 0f || mid > head + .4f) && (lo < 0f || lo > head + .4f);
                        // the hit is the near wall face: the opening's plane is half a wall (0.08 m) further
                        if (band && open) { probeDist = head + .08f; probeAge = 0f; }
                    }
                }
            }
        }
        if (probeAge > .35f && probeDist > .2f) probeDist = -1f;   // turned away: the header is no longer ahead

        var reach = .6f;
        if (d > .02f)
        {
            // a map door: DoorSqueeze is exact (1 on the door line, 0 from 0.6 m) and its change gives the side
            if (!crossing) { crossing = true; crossingSign = -1; peakSqueeze = d; }
            peakSqueeze = Mathf.Max(peakSqueeze, d);
            if (crossingSign < 0 && peakSqueeze > .85f && d < peakSqueeze - .03f) crossingSign = 1;
            doorS = crossingSign * reach * (1f - d);
            probeDist = -1f;
        }
        else if (crossing && crossingSign > 0)
        {
            // past the opening's plane: dead-reckon the distance behind it until the body is clear
            doorS += Mathf.Max(speed, .4f) * dt;
            if (doorS > 1.05f) { crossing = false; crossingSign = 0; doorS = -10f; }
        }
        else if (probeDist >= 0f && probeDist <= .12f && moving)
        {
            // at a header the map does not report (an arch): the crossing starts here
            crossing = true;
            crossingSign = 1;
            doorS = 0f;
            probeDist = -1f;
        }
        else
        {
            crossing = false;
            crossingSign = 0;
            doorS = probeDist > 0f ? -probeDist : Mathf.MoveTowards(doorS, -10f, dt * 3f);
        }
        lastSqueeze = d;
        // the std hood and gloves lead the nav axis by ~0.9 m: the squeeze is fully on 1.1 m before the door line
        var target = doorS <= 0f ? 1f - Smooth(1.10f, 1.60f, -doorS) : 1f - Smooth(.60f, .95f, doorS);
        doorW = dt > 0f ? Mathf.MoveTowards(doorW, target, dt / .12f) : target;
    }

    float Nearest(Vector3 from, Vector3 dir, float length)
    {
        var n = Physics.RaycastNonAlloc(from, dir, rayHits, length, ~0, QueryTriggerInteraction.Ignore);
        var best = -1f;
        for (var i = 0; i < n; i++)
        {
            var c = rayHits[i].collider;
            if (c == null || c is CharacterController || c.transform.IsChildOf(transform.parent != null ? transform.parent : transform)) continue;
            if (best < 0f || rayHits[i].distance < best) best = rayHits[i].distance;
        }
        return best;
    }

    // ------------------------------------------------------------------ head
    void UpdateViewer()
    {
        if (ViewerOverride != null) { viewer = ViewerOverride; return; }
        viewerRefresh -= Time.unscaledDeltaTime;
        if (viewer != null && viewer.gameObject.activeInHierarchy && viewerRefresh > 0f) return;
        viewerRefresh = 1f;
        viewer = null;
        var main = Camera.main;
        if (main != null && main.isActiveAndEnabled && main.targetTexture == null) { viewer = main.transform; return; }
        var bestDepth = float.MinValue;
        var count = Camera.allCamerasCount;
        if (count > cams.Length) cams = new Camera[count];
        count = Camera.GetAllCameras(cams);
        for (var i = 0; i < count; i++)
        {
            var c = cams[i];
            if (c == null || !c.isActiveAndEnabled || c.targetTexture != null) continue;
            if (c.depth > bestDepth) { bestDepth = c.depth; viewer = c.transform; }
        }
    }

    Camera[] cams = new Camera[8];

    void UpdateHead(float dt, FrontRoomsRelayRig.MotionState state, Vector3? listenTarget, bool reduceFlashing)
    {
        var listening = state == FrontRoomsRelayRig.MotionState.IdleListen || state == FrontRoomsRelayRig.MotionState.Search;
        if (listening)
        {
            // latched: hold 0.4-0.9 s, then snap (0.08 s) to the next 15 deg step toward the noise
            latchTimer -= dt;
            if (latchTimer <= 0f)
            {
                latchCount++;
                latchTimer = .4f + .5f * Hash01(latchCount);
                float yaw;
                if (listenTarget.HasValue)
                {
                    var local = transform.InverseTransformPoint(listenTarget.Value);
                    yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -70f, 70f);
                }
                else yaw = (Hash01(latchCount * 7 + 3) - .5f) * 50f;
                yaw = Mathf.Round(yaw / 15f) * 15f;
                if (!Mathf.Approximately(yaw, headYawTarget))
                {
                    snapFrom = headYaw;
                    headYawTarget = yaw;
                    snapT = 0f;
                    lens.SearchSnap(reduceFlashing);
                }
            }
            if (snapT < 1f)
            {
                snapT = Mathf.Min(1f, snapT + dt / .08f);
                headYaw = Mathf.Lerp(snapFrom, headYawTarget, snapT);
            }
        }
        else
        {
            latchTimer = 0f;
            var target = 0f;
            if (state == FrontRoomsRelayRig.MotionState.Run && viewer != null)
            {
                // the head-lock: direction only (never brightness), within +-70 deg
                var local = transform.InverseTransformPoint(viewer.position);
                target = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -70f, 70f);
            }
            headYaw = Mathf.MoveTowards(headYaw, target, dt * (state == FrontRoomsRelayRig.MotionState.Run ? 240f : 120f));
            headYawTarget = headYaw;
        }
        if (Mathf.Abs(headYaw) > .01f)
        {
            RotateSubtree(subNeck3, Quaternion.AngleAxis(headYaw * .35f, up));
            RotateSubtree(subHead, Quaternion.AngleAxis(headYaw * .65f, up));
        }
    }

    static float Hash01(int n)
    {
        unchecked
        {
            var x = (uint)n * 747796405u + 2891336453u;
            x = ((x >> (int)((x >> 28) + 4u)) ^ x) * 277803737u;
            x = (x >> 22) ^ x;
            return (x & 0xFFFFFF) / 16777216f;
        }
    }

    // ------------------------------------------------------------------ the catch
    void Catch(FrontRoomsRelayRig.MotionState state)
    {
        clearance = 0f;
        var w = 0f;
        if (viewer != null && (state == FrontRoomsRelayRig.MotionState.Run || state == FrontRoomsRelayRig.MotionState.IdleListen))
        {
            var cam = transform.InverseTransformPoint(viewer.position);
            var headPos = P[bHead];
            var dist = (cam - headPos).magnitude;
            w = 1f - Smooth(1.0f, 1.5f, dist);
        }
        catchW = w;
        if (w <= 0f) return;
        var camM = transform.InverseTransformPoint(viewer.position);
        // the reveal: the hood looks straight at you (only here may it pitch up)
        var lensPos = LocatorModel(locLens);
        var lensDir = LocatorDir(locLens);
        var toCam = camM - lensPos;
        if (toCam.sqrMagnitude > 1e-4f)
        {
            var turn = Quaternion.FromToRotation(lensDir, toCam.normalized);
            turn.ToAngleAxis(out var angle, out var axis);
            if (angle > 180f) angle -= 360f;
            angle = Mathf.Clamp(angle, -30f, 30f) * w;
            if (Mathf.Abs(angle) > .01f && axis.sqrMagnitude > .5f)
            {
                var g = Quaternion.AngleAxis(angle, axis);
                var pivot = P[bHead];
                for (var i = 0; i < subHead.Length; i++)
                {
                    var c = subHead[i];
                    D[c] = g * D[c];
                    R[c] = g * R[c];
                    P[c] = pivot + g * (P[c] - pivot);
                }
                lensPos = LocatorModel(locLens);
            }
        }
        // camera clearance: the lens stays >= 0.30 m in front of the camera (no vertex within 0.15 m; near plane 0.06)
        var back = -forward;
        var need = 0f;
        for (var iter = 0; iter < 3; iter++)
        {
            var lp = lensPos + back * need;
            var top = LocatorModel(locHoodTop) + back * need;
            var lo = LocatorModel(locHoodLow) + back * need;
            var gap = Mathf.Min((camM - lp).magnitude - .30f, Mathf.Min((camM - top).magnitude, (camM - lo).magnitude) - .15f);
            if (gap >= 0f) break;
            need += -gap + .02f;
        }
        need = Mathf.Min(need, .7f);
        clearance = need;
        if (need > 0f)
        {
            var third = back * (need / 3f);
            ShiftSubtree(subNeck2, third);
            ShiftSubtree(subNeck3, third);
            ShiftSubtree(subHead, third);
        }
        // the reach: both gloves rise toward the frame edges in Run
        if (state == FrontRoomsRelayRig.MotionState.Run)
        {
            var g = 48f * w;
            RotateArmAtShoulder(subArmL, Quaternion.AngleAxis(g, left) * Quaternion.AngleAxis(-12f * w, forward));
            RotateArmAtShoulder(subArmR, Quaternion.AngleAxis(g, left) * Quaternion.AngleAxis(12f * w, forward));
        }
    }

    void RotateArmAtShoulder(int[] sub, Quaternion g)
    {
        var pivot = P[sub[0]];
        for (var i = 0; i < sub.Length; i++)
        {
            var c = sub[i];
            D[c] = g * D[c];
            R[c] = g * R[c];
            P[c] = pivot + g * (P[c] - pivot);
        }
    }

    // ------------------------------------------------------------------ legs
    void Legs(float dt, bool walking, float stepLen, bool run)
    {
        // neutral ankles = the blended key's (before the pin), on the floor
        var pinShift = up * PinOffset;
        var nL = P[bFootL] - pinShift;
        var nR = P[bFootR] - pinShift;
        var beta = run ? .40f : .62f;
        var lift = set.Data.gait.swingLift * (1f - .7f * doorW);
        var ahead = velocityDir * (beta * stepLen) ;
        if (walking)
        {
            if (!gaitLocked)
            {
                gaitLocked = true;
                lockWorld[0] = transform.TransformPoint(nL);
                lockWorld[1] = transform.TransformPoint(nR);
                stance[0] = stance[1] = true;
            }
            var rate = speed / Mathf.Max(2f * stepLen, .2f);
            phase = Mathf.Repeat(phase + rate * dt, 1f);
        }
        for (var i = 0; i < 2; i++)
        {
            var neutralWorld = transform.TransformPoint(i == 0 ? nL : nR);
            var p = Mathf.Repeat(phase + .5f * i, 1f);
            landed[i] = false;
            Vector3 pos;
            if (!gaitLocked) pos = neutralWorld;
            else
            {
                var inStance = p < beta;
                var landing = neutralWorld + ahead;
                landing.y = neutralWorld.y;
                if (inStance && !stance[i])
                {
                    stance[i] = true;
                    lockWorld[i] = landing;
                    landed[i] = true;
                }
                else if (!inStance && stance[i])
                {
                    stance[i] = false;
                    swingFrom[i] = lockWorld[i];
                }
                if (stance[i])
                {
                    // a rescue step if the body turned or stopped far from the locked foot
                    var off = lockWorld[i] - neutralWorld; off.y = 0f;
                    if (off.magnitude > beta * stepLen + .35f) lockWorld[i] = Vector3.MoveTowards(lockWorld[i], neutralWorld, dt * 2.5f);
                    pos = lockWorld[i];
                }
                else
                {
                    var t = (p - beta) / (1f - beta);
                    var s = t * t * (3f - 2f * t);
                    pos = Vector3.Lerp(swingFrom[i], landing, s) + Vector3.up * (lift * Mathf.Sin(t * Mathf.PI) * modelScale);
                }
                if (gaitW < 1f) pos = Vector3.Lerp(neutralWorld, pos, gaitW);
                if (!walking && gaitW <= 0f) gaitLocked = false;
            }
            footTarget[i] = transform.InverseTransformPoint(pos);
            lastFootPhase[i] = p;
        }
        SolveLeg(bThighL, bShinL, bFootL, bToeL, footTarget[0]);
        SolveLeg(bThighR, bShinR, bFootR, bToeR, footTarget[1]);
    }

    void SolveLeg(int thigh, int shin, int foot, int toe, Vector3 target)
    {
        var hip = P[thigh];
        var kneeKey = P[shin];
        var ankleKey = P[foot];
        var toeOff = toe >= 0 ? P[toe] - ankleKey : Vector3.zero;
        var to = target - hip;
        var dist = to.magnitude;
        if (dist < 1e-4f) return;
        var dir = to / dist;
        var dMin = Mathf.Abs(thighLen - shinLen) + 1e-3f;
        var dMax = thighLen + shinLen - 1e-3f;
        var d = Mathf.Clamp(dist, dMin, dMax);
        var cosA = Mathf.Clamp((thighLen * thighLen + d * d - shinLen * shinLen) / (2f * thighLen * d), -1f, 1f);
        var sinA = Mathf.Sqrt(1f - cosA * cosA);
        var pole = (kneeKey - hip) - dir * Vector3.Dot(kneeKey - hip, dir);
        if (pole.sqrMagnitude < 1e-6f) pole = forward - dir * Vector3.Dot(forward, dir);
        pole.Normalize();
        var knee = hip + dir * (thighLen * cosA) + pole * (thighLen * sinA);
        var ankle = hip + dir * d;
        var gThigh = Quaternion.FromToRotation(kneeKey - hip, knee - hip);
        var gShin = Quaternion.FromToRotation(ankleKey - kneeKey, ankle - knee);
        R[thigh] = gThigh * R[thigh]; D[thigh] = gThigh * D[thigh];
        R[shin] = gShin * R[shin]; D[shin] = gShin * D[shin];
        P[shin] = knee;
        P[foot] = ankle;
        if (toe >= 0) P[toe] = ankle + toeOff;
    }

    // ------------------------------------------------------------------ lens
    void ApplyLens()
    {
        var level = lens.Level;
        if (Mathf.Abs(level - lastLensLevel) > .0005f)
        {
            lastLensLevel = level;
            var c = FrontRoomsRelayLens.FullEmission.linear * level;
            block.Clear();
            block.SetVector(EmissionColorId, new Vector4(c.r, c.g, c.b, 1f));
            for (var i = 0; i < renderers.Length; i++)
                if (renderers[i] != null && lensSlot[i]) renderers[i].SetPropertyBlock(block, LensMaterialIndex);
        }
        if (lensLight != null)
        {
            var on = lens.LightOn;
            if (lensLight.enabled != on) lensLight.enabled = on;
            if (on)
            {
                lensLight.intensity = lens.LightIntensity;
                lensLight.range = lens.LightRange;
                var lp = LocatorModel(locLens);
                var ld = LocatorDir(locLens);
                // just ahead of the opening: behind it, the hem would catch the spot at 10 cm and turn into a white bezel
                lensLight.transform.SetPositionAndRotation(transform.TransformPoint(lp + ld * .03f), transform.rotation * Quaternion.LookRotation(ld, up));
            }
        }
    }

    // ------------------------------------------------------------------ contacts
    void Contacts(float dt, bool moving, float d, bool playing, FrontRoomsRelayRig.MotionState state)
    {
        // Step: at each foot plant, the planted boot's ball
        if (onStep != null && moving && dt > 0f)
        {
            if (landed[0]) onStep(0, transform.TransformPoint(LocatorModel(locBallL)));
            if (landed[1]) onStep(1, transform.TransformPoint(LocatorModel(locBallR)));
        }
        // CeilingBrush: at the step's peak (mid-swing) while pinned and moving, >= 0.5 s apart
        brushCooldown -= dt;
        var midSwing = false;
        for (var i = 0; i < 2; i++) if (!stance[i] && gaitLocked) midSwing = true;
        if (PressesCeiling && moving && speed > .2f && midSwing && brushArmed && brushCooldown <= 0f)
        {
            brushCooldown = .5f;
            brushArmed = false;
            var top = transform.TransformPoint(LocatorModel(topMarkers[topMarkers.Length - 1]));
            top.y = transform.position.y + smoothHeight;
            onBrush?.Invoke(top);
        }
        if (!midSwing) brushArmed = true;

        // Squeeze: real contact with the jamb (overlap spheres at 10 Hz), again each +0.3 of DoorSqueeze
        if (d <= .01f)
        {
            squeezeRaised = false;
            squeezeContact = false;
            squeezeTimer = 0f;
            return;
        }
        if (playing)
        {
            squeezeTimer -= dt;
            if (squeezeTimer <= 0f)
            {
                squeezeTimer = .1f;
                if (Probe(LocatorModel(locShoulderR), .12f) || Probe(LocatorModel(locHoodCentre), .18f) || Probe(LocatorModel(locHips), .15f))
                    squeezeContact = true;
            }
        }
        var raise = false;
        if (!squeezeRaised) raise = squeezeContact || d > .5f || !playing;
        else if (d >= squeezeRaisedAt + .3f) raise = true;
        if (raise)
        {
            squeezeRaised = true;
            squeezeRaisedAt = Mathf.Max(d, .01f);
            var point = squeezeContact ? squeezePoint : transform.position + Vector3.up * 1.5f;
            onSqueeze?.Invoke(point, d);
        }
    }

    bool Probe(Vector3 model, float radius)
    {
        var w = transform.TransformPoint(model);
        var n = Physics.OverlapSphereNonAlloc(w, radius * modelScale, overlap, ~0, QueryTriggerInteraction.Ignore);
        for (var i = 0; i < n; i++)
        {
            var c = overlap[i];
            if (c == null || c is CharacterController) continue;
            if (c.transform.IsChildOf(transform.parent != null ? transform.parent : transform)) continue;
            squeezePoint = c.ClosestPoint(w);
            return true;
        }
        return false;
    }

    // ------------------------------------------------------------------ glass mirror
    /// <summary>Ray-traced glass (G14) registers MeshRenderers only: in Play on a supported Mac a hidden baked mirror stands in.</summary>
    public void EnsureGlassMirror()
    {
        if (!Application.isPlaying || mirror != null) return;
        mirror = FrontRoomsRelayGlassMirror.Create(this);
    }

    public SkinnedMeshRenderer MirrorSource()
    {
        if (renderers == null) return null;
        foreach (var r in renderers) if (r != null && r.name.EndsWith("_LOD1", StringComparison.Ordinal)) return r;
        return renderers.Length > 0 ? renderers[0] : null;
    }

    void OnDestroy()
    {
        if (mirror != null)
        {
            if (Application.isPlaying) Destroy(mirror.gameObject); else DestroyImmediate(mirror.gameObject);
        }
    }

    // ------------------------------------------------------------------ test access
    /// <summary>Model-space bone position after the last Tick (tests).</summary>
    public Vector3 BoneModel(string bone) { var i = set != null ? set.IndexOf(bone) : -1; return i >= 0 ? P[i] : Vector3.zero; }
    public Vector3 LensWorld => transform.TransformPoint(LocatorModel(locLens));
    public Vector3 LensDirWorld => transform.TransformDirection(LocatorDir(locLens));
    public float Clearance => clearance;
    public float CatchWeight => catchW;
    public float HeadYaw => headYaw;
    public SkinnedMeshRenderer[] Renderers => renderers;
    public Light LensLight => lensLight;
    public int BoneCount => boneCount;
    public float ModelScale => modelScale;
    public Vector3 ModelUp => up;
    public Vector3 ModelForward => forward;
}
