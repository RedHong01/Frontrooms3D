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
///   V6 window step   (C12, only while the run's sashPry flag is on: FrontRoomsRelayRig feeds SetWindowStep) the hunter's
///                    0.6 s frame step over a broken or pried window: the door keys through the opening on the true distance
///                    to the wall line, a stoop and duck under the 2.0 m head, the lead boot then the trail boot over the sill.
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
    int bThighL, bShinL, bFootL, bToeL, bThighR, bShinR, bFootR, bToeR, bArmL, bArmR, bHandR, bFingersR;
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

    // ---- V6 (C12): the frame step over a window sill (FrontRoomsMapHunter.WindowStepProgress through FrontRoomsRelayRig) ----
    /// <summary>
    /// The frame step's pose, at its middle (each comes in and goes out on sin(pi u) over the 0.6 s): the sill's height
    /// (ModuleUnits.WindowSill) and each boot's clearance over it, where the lead and trail boots land past the wall line (the
    /// hunter's body lands 0.75 m past it), the duck of the pelvis (the knees take it, as the ceiling pin), the stoop of the chest
    /// and the bow of the hood under the window head (2.0 m: 0.1 m under the doorway the door keys fit), the arms' reach through
    /// the frame. Metres and degrees.
    /// </summary>
    public const float StepSill = FrontRooms.Map.ModuleUnits.WindowSill, StepClearance = .10f, StepLeadLanding = .38f, StepTrailLanding = .62f;
    public const float StepDuck = .10f, StepStoop = 12f, StepBow = 8f, StepReach = 16f;

    float stepU = -1f, stepDrop;
    int stepLead;
    Vector3 stepCrossing, stepNormal;
    readonly Vector3[] stepFrom = new Vector3[2];
    readonly Vector3[] stepLast = new Vector3[2];

    /// <summary>In the frame step now (tests).</summary>
    public bool WindowStepping => stepU >= 0f;

    /// <summary>
    /// V6 (C12): the hunter's frame step, set by the rig before each Tick while the run's sashPry flag is on: progress 0-1
    /// (below 0: none), the opening's crossing point on the wall line (world, floor height) and the opening's normal in the
    /// direction of the step (world, flat). Picture only: nothing in the hunter reads it.
    /// </summary>
    public void SetWindowStep(float progress, Vector3 crossing, Vector3 normal)
    {
        if (progress < 0f)
        {
            if (stepU >= 0f) EndWindowStep();
            return;
        }
        if (stepU < 0f)
        {
            stepCrossing = crossing;
            normal.y = 0f;
            stepNormal = normal.sqrMagnitude > 1e-6f ? normal.normalized : transform.forward;
            for (var i = 0; i < 2; i++) stepFrom[i] = stepLast[i] = initialised ? transform.TransformPoint(footTarget[i]) : transform.position;
            // the boot further from the wall line leads (it is free to swing; the nearer one is the stance boot)
            stepLead = Vector3.Dot(stepFrom[0] - crossing, stepNormal) <= Vector3.Dot(stepFrom[1] - crossing, stepNormal) ? 0 : 1;
        }
        stepU = Mathf.Clamp01(progress);
    }

    // The step is over: the gait takes both boots where they landed, standing (no pop back to its old locks).
    void EndWindowStep()
    {
        stepU = -1f;
        stepDrop = 0f;
        for (var i = 0; i < 2; i++) { lockWorld[i] = stepLast[i]; stance[i] = true; }
        gaitLocked = true;
    }

    static float StepArc(float u) => Mathf.Sin(Mathf.PI * Mathf.Clamp01(u));

    // The door keys through the opening on the true signed distance to the wall line (the map raises no DoorSqueeze for a
    // window); past the step, UpdateDoor's own dead reckoning carries the body out of them as after a door.
    void StepDoor(float dt)
    {
        var rel = transform.position - stepCrossing;
        rel.y = 0f;
        var s = Vector3.Dot(rel, stepNormal);
        crossing = true;
        crossingSign = s > 0f ? 1 : -1;
        peakSqueeze = 1f;
        doorS = s;
        probeDist = -1f;
        var target = doorS <= 0f ? 1f - Smooth(1.10f, 1.60f, -doorS) : 1f - Smooth(.60f, .95f, doorS);
        doorW = dt > 0f ? Mathf.MoveTowards(doorW, target, dt / .12f) : target;
    }

    // The upper body under the head: the chest stoops (the top comes forward and down: negative about `left`, as the hold's hood
    // lift is positive), the hood bows, both arms reach forward through the frame.
    void StepLayers()
    {
        var a = StepArc(stepU);
        if (a <= 0f) return;
        if (bChest >= 0) RotateSubtree(subChest, Quaternion.AngleAxis(-StepStoop * a, left));
        RotateSubtree(subHead, Quaternion.AngleAxis(-StepBow * a, left));
        RotateSubtree(subArmL, Quaternion.AngleAxis(StepReach * a, left));
        RotateSubtree(subArmR, Quaternion.AngleAxis(StepReach * a, left));
    }

    // One boot over the sill: the lead boot over the first 0.04-0.58 of the step, the trail boot over 0.40-0.96, each from where it
    // stood at the step's start to its landing past the wall line (its own side offset kept), on an arc that clears the sill.
    Vector3 StepFoot(int i, Vector3 gaitPos, Vector3 neutralWorld)
    {
        var lead = i == stepLead;
        var t = lead ? Smooth(.04f, .58f, stepU) : Smooth(.40f, .96f, stepU);
        var side = Vector3.Cross(Vector3.up, stepNormal);
        var body = transform.position - stepCrossing;
        var lateral = Vector3.Dot(body, side) + Vector3.Dot(neutralWorld - transform.position, side);
        var land = stepCrossing + stepNormal * (lead ? StepLeadLanding : StepTrailLanding) + side * lateral;
        land.y = neutralWorld.y;
        var from = stepFrom[i];
        var p = Vector3.Lerp(from, land, t);
        var floor = Mathf.Lerp(from.y, land.y, t);
        p.y = floor + (StepSill + StepClearance) * StepArc(t);
        // never through the sill: within the wall's band (0.16 m) and a boot's half length of it, the boot is over the sill's top
        var along = Vector3.Dot(p - stepCrossing, stepNormal);
        if (t > 0f && t < 1f && Mathf.Abs(along) < .22f) p.y = Mathf.Max(p.y, neutralWorld.y + StepSill + .04f);
        stepLast[i] = p;
        return p;
    }

    // ---- the ceiling lamps (research/hunter/production_b/40_ceiling_lamps.md, Red's call PL01): the body reads the lamps the
    // kits registered (FrontRoomsTrofferKit.LampsNear). The true top (the mesh's, not the locators') stays SkinClear under the
    // tile or a lamp's frame over it; a deep fixture (an Office pendant, a back-of-house wrap) folds the room pose, met
    // LookAhead s ahead; the Listen glove rests ON a plain ceiling tile; and under a lit lens the contact reads (C): the lamp's
    // light on the body's top, the body's shade on the floor. The lamps themselves never change.
    /// <summary>Tools and tests: false = the body ignores the lamps (the pin, glove and light as before PL01).</summary>
    public static bool LampRules = true;
    /// <summary>The true top keeps this clear of the tile or a lamp's frame over it; it ramps in over SkinRamp round a fixture.
    /// For it the pin may reach PinExtra under its old -0.12 m floor (the door blow under a lamp in a 2.4 m room needs 18 mm
    /// more; 40_ceiling_lamps.md section 2).</summary>
    public const float SkinClear = .003f, SkinRamp = .05f, PinExtra = .04f;
    /// <summary>A fixture whose bottom hangs more than DeepFixture under its plane folds the room pose (pendant, wrap); it counts
    /// within DeepMargin of a top marker (the shoulders' reach round them), now or LookAhead s ahead along the ground velocity.</summary>
    public const float DeepFixture = .05f, DeepMargin = .22f, LookAhead = .6f;
    /// <summary>"Under a lamp" = a top marker within LampMargin of the fixture (tools).</summary>
    public const float LampMargin = .10f;
    /// <summary>The Listen glove: GloveMargin off every fixture, turned at the shoulder in GloveYawStep steps, at most GloveMaxYaw
    /// outward and GloveMaxYawIn inward; lifted until its top is GloveTouch under the tile, at most GloveMaxLift.</summary>
    public const float GloveMargin = .08f, GloveMaxYaw = 50f, GloveMaxYawIn = 20f, GloveYawStep = 5f, GloveTouch = .004f, GloveMaxLift = .16f;
    const int GloveSearchTicks = 6;
    float gloveGoal;
    int gloveSearch;
    /// <summary>C: the lamp's light on the body's top (a spot ContactHeight over the lens centre with the lens as its aperture
    /// cookie, reaching ContactReach past the top, ContactGain of the lamp's intensity, full within ContactSoft of the lens and
    /// while the top is within ContactGap of the fixture), and the floor shade under the body (ShadeStrength at the lamp's full
    /// light, ShadeHalfX / ShadeHalfZ half extents).</summary>
    public static float ContactHeight = .30f, ContactReach = .70f, ContactGain = .35f, ContactSoft = .30f, ContactGap = .08f;
    public static float ShadeStrength = .5f, ShadeHalfX = .95f, ShadeHalfZ = 1.1f;
    const float LampQueryRadius = 2.2f;

    readonly FrontRoomsTrofferKit.LiveLamp[] lamps = new FrontRoomsTrofferKit.LiveLamp[12];
    int lampCount;
    float underTop, gloveYaw, gloveLift, contactLevel, shadeLevel, foldDrop, skinTopModel, skinNeed = 99f, smoothTile = 2.9f;
    Vector3 skinTopPos;
    Vector3[] gloveBuf;
    Light contactLight;
    MeshRenderer shade;
    MaterialPropertyBlock shadeBlock;
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static Texture2D shadeTexture;
    static Mesh shadeMesh;

    /// <summary>Tests and tools: how far the room pose folds under the room's ceiling for a deep fixture (m), the glove's turn
    /// (deg) and lift (m), how far a top marker is under a lamp (0-1), the contact light (its intensity) and the floor shade
    /// (alpha), the lamps found by the last Tick, the true top after the pin (model space, m) and the hull's vertex count.</summary>
    public float LampFold => foldDrop;
    public float GloveYaw => gloveYaw;
    public float GloveLift => gloveLift;
    public float UnderLamp => underTop;
    public float ContactLevel => contactLevel;
    public float ShadeLevel => shadeLevel;
    public int LampsFound => lampCount;
    public float SkinTop => skinTopModel;
    public int SkinMarkers => skinCount;
    public Light ContactLight => contactLight;

    static float InsideWeight(Vector3 p, Rect r, float margin)
    {
        var dx = Mathf.Max(r.xMin - p.x, 0f, p.x - r.xMax);
        var dz = Mathf.Max(r.yMin - p.z, 0f, p.z - r.yMax);
        var d = Mathf.Sqrt(dx * dx + dz * dz);
        return d <= 0f ? 1f : 1f - Smooth(0f, margin, d);
    }

    // Before the room pose: the lamps round the body (the top markers as last posed, now and LookAhead s ahead) give the ceiling
    // the pose folds under (a deep fixture's bottom, ramped in over DeepMargin) and how far a top marker is under a lamp.
    float LampCeiling(float ceiling, float dt)
    {
        lampCount = 0;
        underTop = 0f;
        foldDrop = 0f;
        // the tiles' own height, smoothed as the room pose smooths it (the glove rests on the tiles, not on a fold)
        smoothTile = dt > 0f ? Mathf.Lerp(smoothTile, ceiling, 1f - Mathf.Exp(-dt / (ceiling < smoothTile ? .25f : 1f))) : ceiling;
        if (!LampRules || !initialised || topMarkers == null) return ceiling;
        var ahead = velocityDir * (speed * LookAhead);
        ahead.y = 0f;
        lampCount = FrontRoomsTrofferKit.LampsNear(transform.position + ahead * .5f, LampQueryRadius + ahead.magnitude * .5f, lamps);
        var floorY = transform.position.y;
        var fold = ceiling;
        for (var m = 0; m <= topMarkers.Length; m++)
        {
            var loc = m < topMarkers.Length ? topMarkers[m] : locHoodTop;
            var w = transform.TransformPoint(LocatorModel(loc));
            for (var k = 0; k < 2; k++)
            {
                var p = k == 0 ? w : w + ahead;
                for (var i = 0; i < lampCount; i++)
                {
                    var lamp = lamps[i];
                    var deep = lamp.ceiling - lamp.bottom > DeepFixture;
                    var bottom = lamp.bottom - floorY;
                    // "under a lamp" is where the markers are now (the look-ahead is the deep fold's)
                    if (k == 0 && m < topMarkers.Length) underTop = Mathf.Max(underTop, InsideWeight(p, lamp.outer, LampMargin));
                    if (!deep) continue;
                    var wd = InsideWeight(p, lamp.outer, DeepMargin);
                    if (wd > 0f) fold = Mathf.Min(fold, Mathf.Lerp(ceiling, bottom, wd));
                }
            }
        }
        foldDrop = ceiling - fold;
        return fold;
    }

    // ---- the true top. The locators (top_*) ride one bone each and sit about 7 mm over the mesh in the walk, but in the lock-on
    // hold and the door blow the mesh rises 8-20 mm past them (40_ceiling_lamps.md section 1), so the pin also reads the mesh:
    // the LOD0 vertices on the hull of the upper body (spine_02, chest, shrug, clavicles, neck_01; extremes over a 7.5 x 10 deg
    // grid of directions) and of the right glove, skinned here as the GPU skins them (four weights each).
    int skinCount, gloveFrom, gloveCount;
    int[] skinBone;
    float[] skinWeight;
    Vector3[] skinOffset;
    static readonly string[] SkinUpper = { "spine_02", "chest", "shrug", "clavicle_L", "clavicle_R", "neck_01" };
    static readonly string[] SkinGlove = { "hand_R", "fingers_R", "thumb_R" };

    void BuildSkin(FrontRoomsRelayPoseSet poses)
    {
        skinCount = gloveFrom = gloveCount = 0;
        SkinnedMeshRenderer lod0 = null;
        for (var i = 0; i < renderers.Length; i++)
            if (renderers[i] != null && renderers[i].name.EndsWith("_LOD0", StringComparison.Ordinal)) { lod0 = renderers[i]; break; }
        var mesh = lod0 != null ? lod0.sharedMesh : null;
        if (mesh == null || !mesh.isReadable) return;
        var verts = mesh.vertices;
        var weights = mesh.boneWeights;
        var bindposes = mesh.bindposes;
        var smrBones = lod0.bones;
        if (weights.Length != verts.Length || smrBones.Length != bindposes.Length) return;
        var map = new int[smrBones.Length];
        for (var i = 0; i < smrBones.Length; i++) map[i] = smrBones[i] != null ? poses.IndexOf(smrBones[i].name) : -1;
        var kind = new int[boneCount];
        foreach (var n in SkinUpper) { var b = poses.IndexOf(n); if (b >= 0) kind[b] = 1; }
        foreach (var n in SkinGlove) { var b = poses.IndexOf(n); if (b >= 0) kind[b] = 2; }
        // each vertex's bind position in model space (the bones are at bind while Bind runs)
        var model = new Vector3[verts.Length];
        var cls = new int[verts.Length];
        for (var v = 0; v < verts.Length; v++)
        {
            var k = weights[v].boneIndex0;
            var pb = k >= 0 && k < map.Length ? map[k] : -1;
            if (pb < 0 || kind[pb] == 0 || smrBones[k] == null) continue;
            cls[v] = kind[pb];
            model[v] = transform.InverseTransformPoint(smrBones[k].localToWorldMatrix.MultiplyPoint3x4(bindposes[k].MultiplyPoint3x4(verts[v])));
        }
        var picked = new List<int>();
        var seen = new HashSet<int>();
        for (var c = 1; c <= 2; c++)
        {
            if (c == 2) gloveFrom = picked.Count;
            for (var el = -30f; el <= 90.01f; el += 7.5f)
            {
                var steps = el >= 89.99f ? 1 : 36;
                for (var az = 0; az < steps; az++)
                {
                    var a = az * 10f * Mathf.Deg2Rad;
                    var e = el * Mathf.Deg2Rad;
                    var d = up * Mathf.Sin(e) + (forward * Mathf.Cos(a) + left * Mathf.Sin(a)) * Mathf.Cos(e);
                    var best = -1;
                    var bestDot = float.MinValue;
                    for (var v = 0; v < verts.Length; v++)
                    {
                        if (cls[v] != c) continue;
                        var dot = Vector3.Dot(model[v], d);
                        if (dot > bestDot) { bestDot = dot; best = v; }
                    }
                    if (best >= 0 && seen.Add(best)) picked.Add(best);
                }
            }
        }
        gloveCount = picked.Count - gloveFrom;
        skinCount = picked.Count;
        gloveBuf = new Vector3[Mathf.Max(1, gloveCount)];
        skinBone = new int[skinCount * 4];
        skinWeight = new float[skinCount * 4];
        skinOffset = new Vector3[skinCount * 4];
        for (var i = 0; i < skinCount; i++)
        {
            var v = picked[i];
            var bw = weights[v];
            for (var j = 0; j < 4; j++)
            {
                var k = j == 0 ? bw.boneIndex0 : j == 1 ? bw.boneIndex1 : j == 2 ? bw.boneIndex2 : bw.boneIndex3;
                var w = j == 0 ? bw.weight0 : j == 1 ? bw.weight1 : j == 2 ? bw.weight2 : bw.weight3;
                var b = k >= 0 && k < map.Length ? map[k] : -1;
                skinBone[i * 4 + j] = w > 0f ? b : -1;
                skinWeight[i * 4 + j] = b >= 0 ? w : 0f;
                skinOffset[i * 4 + j] = b >= 0 ? model[v] - bindPos[b] : Vector3.zero;
            }
        }
    }

    Vector3 SkinModel(int i)
    {
        var p = Vector3.zero;
        var sum = 0f;
        for (var j = 0; j < 4; j++)
        {
            var b = skinBone[i * 4 + j];
            if (b < 0) continue;
            var w = skinWeight[i * 4 + j];
            p += w * (P[b] + D[b] * skinOffset[i * 4 + j]);
            sum += w;
        }
        return sum > 1e-5f ? p / sum : p;
    }

    // What the pin may be at most so that every upper-body hull vertex is SkinClear under what is over it: the smoothed room
    // ceiling, or the bottom of a lamp's fixture (ramped in over SkinRamp round it). Also sets the true top.
    float SkinNeed(float h)
    {
        skinTopModel = float.MinValue;
        if (gloveFrom <= 0) { skinNeed = 99f; return 99f; }
        var floorY = transform.position.y;
        var need = 99f;
        for (var i = 0; i < gloveFrom; i++)
        {
            var p = SkinModel(i);
            var y = Vector3.Dot(p, up);
            if (y > skinTopModel) { skinTopModel = y; skinTopPos = p; }
            if (y < h - .30f) continue;
            var ceil = h;
            if (lampCount > 0)
            {
                var w = transform.TransformPoint(p);
                for (var k = 0; k < lampCount; k++)
                {
                    var bottom = lamps[k].bottom - floorY;
                    if (bottom >= ceil) continue;
                    var wk = InsideWeight(w, lamps[k].outer, SkinRamp);
                    if (wk > 0f) ceil = Mathf.Min(ceil, Mathf.Lerp(h, bottom, wk));
                }
            }
            need = Mathf.Min(need, ceil - SkinClear - y);
        }
        skinNeed = need;
        return need;
    }

    // the glove's hull as posed now (model space) into gloveBuf; returns the index of its highest point
    int GloveHull()
    {
        var best = 0;
        var bestY = float.MinValue;
        for (var k = 0; k < gloveCount; k++)
        {
            var p = SkinModel(gloveFrom + k);
            gloveBuf[k] = p;
            var y = Vector3.Dot(p, up);
            if (y > bestY) { bestY = y; best = k; }
        }
        return best;
    }

    // The pitch at the shoulder (pivot) that raises `point` by `lift`, in the vertical plane through both (identity if none).
    Quaternion GlovePitch(Vector3 pivot, Vector3 point, float lift)
    {
        var v = point - pivot;
        var len = v.magnitude;
        if (len < .1f || Mathf.Abs(lift) < 1e-4f) return Quaternion.identity;
        var y = Vector3.Dot(v, up);
        var flat = v - up * y;
        if (flat.sqrMagnitude < 1e-6f) return Quaternion.identity;
        var phi = Mathf.Asin(Mathf.Clamp(y / len, -1f, 1f));
        var phi2 = Mathf.Asin(Mathf.Clamp((y + lift) / len, -1f, 1f));
        if (Mathf.Abs(phi2 - phi) < 1e-5f) return Quaternion.identity;
        var to = flat.normalized * (Mathf.Cos(phi2) * len) + up * (Mathf.Sin(phi2) * len);
        return Quaternion.FromToRotation(v, to);
    }

    // The glove placed: its hull (gloveBuf, as posed) turned by yaw about the vertical through the shoulder, then pitched until its
    // highest point is GloveTouch under the tiles (`want`, trimmed if another point of the glove rises past them). True when every
    // point of the placed glove is GloveMargin off every fixture; `lift` is what the pitch raises the top point by.
    bool GlovePlaced(Vector3 shoulder, float yaw, int top, float want, out float lift)
    {
        var g = Quaternion.AngleAxis(yaw, up);
        var t = shoulder + g * (gloveBuf[top] - shoulder);
        lift = want;
        var pitch = Quaternion.identity;
        var limit = smoothTile - GloveTouch;
        for (var pass = 0; pass < 3; pass++)
        {
            pitch = GlovePitch(shoulder, t, lift);
            var maxY = float.MinValue;
            for (var k = 0; k < gloveCount; k++) maxY = Mathf.Max(maxY, Vector3.Dot(shoulder + pitch * (g * (gloveBuf[k] - shoulder)), up));
            var over = maxY - limit;
            if (over <= .0005f) break;
            lift -= over;
        }
        for (var k = 0; k < gloveCount; k++)
        {
            var w = transform.TransformPoint(shoulder + pitch * (g * (gloveBuf[k] - shoulder)));
            for (var i = 0; i < lampCount; i++)
                if (InsideWeight(w, lamps[i].outer, GloveMargin) > 0f) return false;
        }
        return true;
    }

    // In the Listen key the right glove rests ON a plain ceiling tile (today it stays about 0.10 m under the tiles,
    // over a lens or not). The arm turns about the vertical through its shoulder by the smallest step whose glove, turned AND
    // lifted, is GloveMargin off every fixture (none within GloveMaxYaw: it stays where it is and is not lifted), then pitches at
    // the shoulder until the glove's highest point is GloveTouch under the tiles. The lift waits for the turn and comes in with the
    // cube of the Listen key's weight (the turn with the weight), so the glove never rises through a fixture on its way.
    void GloveRule(float dt, float weight)
    {
        var live = weight > 0f && gloveCount > 0 && subArmR != null && subArmR.Length > 0;
        var liftGoal = 0f;
        var clear = false;
        if (live)
        {
            var shoulder = P[subArmR[0]];
            var top = GloveHull();
            var want = Mathf.Clamp(smoothTile - GloveTouch - Vector3.Dot(gloveBuf[top], up), -.05f, GloveMaxLift);
            float l;
            // the last goal holds while it stays clear; every GloveSearchTicks (or when it fails) the smallest clear turn is sought
            if (--gloveSearch > 0 && GlovePlaced(shoulder, gloveGoal, top, want, out l)) { liftGoal = l; clear = true; }
            else
            {
                // outward first (the hand moves aside, away from the body's midline), inward at most GloveMaxYawIn (further in,
                // the forearm would cross the face)
                gloveSearch = GloveSearchTicks;
                var outward = Vector3.Dot(Quaternion.AngleAxis(10f, up) * forward, -left) > 0f ? 1f : -1f;
                for (var s = 0f; s <= GloveMaxYaw + .01f && !clear; s += GloveYawStep)
                {
                    if (GlovePlaced(shoulder, outward * s, top, want, out l)) { gloveGoal = outward * s; liftGoal = l; clear = true; }
                    else if (s > 0f && s <= GloveMaxYawIn + .01f && GlovePlaced(shoulder, -outward * s, top, want, out l)) { gloveGoal = -outward * s; liftGoal = l; clear = true; }
                }
            }
        }
        else { gloveGoal = 0f; gloveSearch = 0; }
        gloveYaw = dt > 0f ? Mathf.Lerp(gloveYaw, gloveGoal, 1f - Mathf.Exp(-dt / .15f)) : gloveGoal;
        var settle = 1f - Smooth(1.5f, 6f, Mathf.Abs(gloveYaw - gloveGoal));
        var liftTarget = liftGoal * settle;
        gloveLift = dt > 0f ? Mathf.Lerp(gloveLift, liftTarget, 1f - Mathf.Exp(-dt / (liftTarget < gloveLift ? .05f : .12f))) : liftTarget;
        // never past the tiles: liftGoal is the exact lift to them for the pose as it is now (the smoothing never lags above it)
        if (clear) gloveLift = Mathf.Min(gloveLift, liftGoal);
        if (!live) return;
        var a = gloveYaw * weight;
        if (Mathf.Abs(a) > .01f) RotateArmAtShoulder(subArmR, Quaternion.AngleAxis(a, up));
        var lift = gloveLift * weight * weight * weight;
        if (Mathf.Abs(lift) < 1e-4f) return;
        // the pitch for the glove as turned now, through its highest point (the same point the placement measured)
        var at = GloveHull();
        var pitch = GlovePitch(P[subArmR[0]], gloveBuf[at], lift);
        if (pitch != Quaternion.identity) RotateArmAtShoulder(subArmR, pitch);
    }

    // The contact: the lamp's light on the body's top, and the floor shade under it. The light is a spot ContactHeight over the lens centre
    // with the lens as its aperture cookie (a near-field stand-in for the lens's area: its fall-off over the shrug and hood is
    // soft; the ceiling, the lens and the frame, which face down, take none of it, and the return walls sit outside the
    // aperture), aimed down, at the lamp's own colour and ContactGain of its intensity as the map drives it (flicker, dim, dead,
    // warning, blackout, the player's light radius), times how far the top is under the lens. The shade darkens the floor under
    // the body by the share of that lamp's light the body takes. The lamp itself never changes (10_plan.md records Red's
    // staged-warning decision: lamps never change where the Relay is). WebGL: the shade only (its tier keeps the Chase light alone).
    void Contact(float pinW)
    {
        var best = -1;
        var bestW = 0f;
        var cover = 0f;
        var topW = Vector3.zero;
        if (LampRules && lampCount > 0 && gloveFrom > 0)
        {
            // the true top's vertex (SkinNeed, after the pin)
            topW = transform.TransformPoint(skinTopPos);
            for (var i = 0; i < lampCount; i++)
            {
                var l = lamps[i].light;
                if (lamps[i].shape == FrontRoomsTrofferKit.LampShape.Pendant || l == null || !l.isActiveAndEnabled || l.intensity <= 0f) continue;
                var gap = lamps[i].bottom - topW.y;
                var c = InsideWeight(topW, lamps[i].lens, ContactSoft) * (1f - Smooth(.02f, ContactGap, gap)) * pinW;
                if (c * l.intensity > bestW) { bestW = c * l.intensity; best = i; cover = c; }
            }
        }
        contactLevel = 0f;
        shadeLevel = 0f;
        if (best >= 0)
        {
            var lamp = lamps[best];
            contactLevel = ContactGain * lamp.light.intensity * cover;
            shadeLevel = ShadeStrength * cover * Mathf.Clamp01(lamp.light.intensity / 5f);
#if UNITY_WEBGL && !UNITY_EDITOR
            contactLevel = 0f;
#endif
            if (contactLight != null && contactLevel > 0f)
            {
                // the light stands ContactHeight over the lens centre (a near-field stand-in for the lens's area), its cookie the lens
                // as the light sees it at the body's top: light lands only where the lens could light the body, never on the
                // fixture's return walls (they sit just outside the aperture) or on the tiles round it
                var o = lamp.lens;
                var longZ = o.height >= o.width;
                var depth = ContactHeight + Mathf.Max(0f, lamp.ceiling - lamp.bottom) + SkinClear;
                var fx = (longZ ? o.width : o.height) * .5f / depth;
                var fy = (longZ ? o.height : o.width) * .5f / depth;
                var t = Mathf.Sqrt(fx * fx + fy * fy) * 1.04f;
                var at = new Vector3(o.center.x, lamp.ceiling + ContactHeight, o.center.y);
                contactLight.transform.SetPositionAndRotation(at, Quaternion.LookRotation(Vector3.down, longZ ? Vector3.forward : Vector3.right));
                var angle = 2f * Mathf.Atan(t) * Mathf.Rad2Deg;
                if (Mathf.Abs(contactLight.spotAngle - angle) > .01f) { contactLight.spotAngle = angle; contactLight.innerSpotAngle = angle - 1f; }
                var cookie = ApertureCookie(fx / t, fy / t);
                if (contactLight.cookie != cookie) contactLight.cookie = cookie;
                contactLight.color = lamp.light.color;
                contactLight.intensity = contactLevel;
                contactLight.range = depth + ContactReach;
            }
            if (shade != null)
            {
                var c = new Vector3(lamp.lens.center.x, transform.position.y + .006f, lamp.lens.center.y);
                c = Vector3.Lerp(new Vector3(transform.position.x, c.y, transform.position.z), c, .35f);
                shade.transform.SetPositionAndRotation(c, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
                shade.transform.localScale = new Vector3(ShadeHalfX * 2f, 1f, ShadeHalfZ * 2f);
                shadeBlock.SetColor(BaseColorId, new Color(0f, 0f, 0f, shadeLevel));
                shade.SetPropertyBlock(shadeBlock);
            }
        }
        if (contactLight != null && contactLight.enabled != (contactLevel > 0f)) contactLight.enabled = contactLevel > 0f;
        if (shade != null && shade.enabled != (shadeLevel > .001f)) shade.enabled = shadeLevel > .001f;
    }

    // The aperture cookie: a rectangle of half-extents fx, fy (fractions of the cone's tangent square), soft just inside its edge.
    static readonly Dictionary<int, Texture2D> apertureCookies = new Dictionary<int, Texture2D>();

    static Texture2D ApertureCookie(float fx, float fy)
    {
        var key = Mathf.RoundToInt(fx * 200f) * 1000 + Mathf.RoundToInt(fy * 200f);
        Texture2D tex;
        if (apertureCookies.TryGetValue(key, out tex) && tex != null) return tex;
        const int n = 128;
        tex = new Texture2D(n, n, TextureFormat.RGBA32, false, true) { name = "Relay lamp contact aperture", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
        var px = new Color32[n * n];
        for (var j = 0; j < n; j++)
        for (var i = 0; i < n; i++)
        {
            var x = Mathf.Abs((i + .5f) / n * 2f - 1f);
            var y = Mathf.Abs((j + .5f) / n * 2f - 1f);
            var v = (1f - Smooth(fx * .86f, fx * .99f, x)) * (1f - Smooth(fy * .93f, fy * .995f, y));
            var b = (byte)Mathf.RoundToInt(255f * v);
            px[i + j * n] = new Color32(b, b, b, b);
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        apertureCookies[key] = tex;
        return tex;
    }

    void SetUpContact()
    {
        if (contactLight == null)
        {
            var go = new GameObject("lamp contact light");
            go.hideFlags = gameObject.hideFlags;
            go.transform.SetParent(transform, false);
            contactLight = go.AddComponent<Light>();
            // 技术美术调研 row 16, option D (Red 2026-11-04 14:14, "按推荐"): soft-shadow filter Low set explicitly.
            UnityEngine.Rendering.Universal.LightExtensions.GetUniversalAdditionalLightData(contactLight).softShadowQuality = UnityEngine.Rendering.Universal.SoftShadowQuality.Low;
            contactLight.type = LightType.Spot;
            contactLight.spotAngle = 120f;
            contactLight.innerSpotAngle = 60f;
            contactLight.shadows = LightShadows.None;
            contactLight.renderMode = LightRenderMode.ForcePixel;
            contactLight.enabled = false;
        }
        if (shade == null)
        {
            var soil = Resources.Load<Material>("Props/Troffer/Materials/Troffer_L03_Soil");
            if (soil == null) return;
            if (shadeTexture == null)
            {
                const int n = 64;
                shadeTexture = new Texture2D(n, n, TextureFormat.RGBA32, false, true) { name = "Relay lamp shade", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
                var px = new Color32[n * n];
                for (var y = 0; y < n; y++)
                for (var x = 0; x < n; x++)
                {
                    var u = (x + .5f) / n * 2f - 1f;
                    var v = (y + .5f) / n * 2f - 1f;
                    var r = Mathf.Sqrt(u * u + v * v);
                    var a = 1f - Smooth(.15f, 1f, r);
                    px[x + y * n] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(255f * a * a));
                }
                shadeTexture.SetPixels32(px);
                shadeTexture.Apply(false, true);
            }
            if (shadeMesh == null)
            {
                shadeMesh = new Mesh { name = "Relay lamp shade", hideFlags = HideFlags.DontSave };
                shadeMesh.vertices = new[] { new Vector3(-.5f, 0f, -.5f), new Vector3(.5f, 0f, -.5f), new Vector3(.5f, 0f, .5f), new Vector3(-.5f, 0f, .5f) };
                shadeMesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
                shadeMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                shadeMesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
                shadeMesh.bounds = new Bounds(Vector3.zero, new Vector3(1f, .1f, 1f));
            }
            var go = new GameObject("lamp floor shade");
            go.hideFlags = gameObject.hideFlags;
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = shadeMesh;
            shade = go.AddComponent<MeshRenderer>();
            var mat = new Material(soil) { name = "Relay lamp shade", hideFlags = HideFlags.DontSave };
            mat.SetTexture("_BaseMap", shadeTexture);
            mat.SetColor(BaseColorId, new Color(0f, 0f, 0f, 0f));
            shade.sharedMaterial = mat;
            shade.shadowCastingMode = ShadowCastingMode.Off;
            shade.receiveShadows = false;
            shade.enabled = false;
            shadeBlock = new MaterialPropertyBlock();
        }
    }

    // head
    float headYaw, headYawTarget, latchTimer, snapFrom, snapT = 1f;
    int latchCount;
    float clearance, catchW, lensFacing;
    Transform viewer;
    float viewerRefresh;
#if FRONTROOMS_RELAY_V2
    // ---- Relay v2's picture (production_b/20_build.md "relay_rig_v2"; RELAY 8; C37). All SP.
    /// <summary>The neck's limit, degrees off the body (the map's HeadLimitDegrees; 0.35 of it at neck_03, the rest at the head).</summary>
    public const float HeadLimitDegrees = 70f;
    /// <summary>The tracked head: smoothing while it walks and in a hold, and its top speed (degrees per second).</summary>
    public const float WalkTrackSeconds = .12f, HoldTrackSeconds = .07f, HeadTopSpeed = 260f;
    /// <summary>The square-up comes in with the freeze and goes out with the strike; the "almost" stop is this share of it.</summary>
    public const float SquareInSeconds = .2f, SquareOutSeconds = .22f, AlmostShare = .35f;
    /// <summary>A freeze ends the arm swing this fast; the glove leaves a ceiling tile this fast in the lock-on (never in "almost").</summary>
    public const float FreezeSeconds = .12f, GloveDownSeconds = .28f;
    /// <summary>The pose, degrees. The hood comes up off its bowed read (the neck, then the head) to the height of the look point, never
    /// past HoodLiftMax or below it (Tall's hood is already level); the back straightens over spine_02, the arms come off the thighs.
    /// The freeze takes all but HoodBeatShare of the lift; that share and BeatBackLift come over the longest hold (C37's 1.7 s).</summary>
    public const float HoodLiftMax = 24f, HoodBeatShare = .2f, BackLift = 4f, BeatBackLift = 1.5f, ArmsOut = 8f, BeatSeconds = 1.7f;
    /// <summary>A planted foot steps when the body has turned this far over it; the step's seconds and lift; planted feet give back to the gait in this long.</summary>
    public const float PlantTurnDegrees = 50f, PlantStepSeconds = .16f, PlantStepLift = .05f, PlantOutSeconds = .22f;

    FrontRoomsRelayRig.HoldKind hold, holdNow;
    int[] subSpine2;
    float squareW, beatTime, headYawVelocity;
    bool planted, feetKnown;
    float plantW;
    readonly Vector3[] footWorld = new Vector3[2];
    readonly Vector3[] plantWorld = new Vector3[2];
    readonly Vector3[] plantFrom = new Vector3[2];
    readonly float[] plantYaw = new float[2];
    readonly float[] plantT = { 1f, 1f };

    /// <summary>Set by the rig before each Tick (FrontRoomsRelayRig.HoldNow). Counts only in IdleListen.</summary>
    public void SetHold(FrontRoomsRelayRig.HoldKind kind) => hold = kind;
    /// <summary>The square-up's weight after the last Tick: 0, AlmostShare in the "almost" stop, 1 in the lock-on's hold.</summary>
    public float SquareWeight => squareW;
    /// <summary>The steps planted feet took under a turn (tests).</summary>
    public int PlantSteps { get; private set; }
#endif

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
        bArmL = poses.IndexOf("upperarm_L"); bArmR = poses.IndexOf("upperarm_R"); bHandR = poses.IndexOf("hand_R"); bFingersR = poses.IndexOf("fingers_R");
        if (bRoot < 0 || bPelvis < 0 || bHead < 0 || bThighL < 0 || bThighR < 0 || bFootL < 0 || bFootR < 0 || bArmL < 0 || bArmR < 0 || bNeck2 < 0 || bNeck3 < 0)
        {
            Debug.LogWarning("[FrontRoomsRelayBody] the armature lacks a required bone; the primitive Relay stays.");
            return false;
        }
        subPelvis = Subtree(bPelvis); subNeck2 = Subtree(bNeck2); subNeck3 = Subtree(bNeck3); subHead = Subtree(bHead);
        subArmL = Subtree(bArmL); subArmR = Subtree(bArmR); subChest = Subtree(bChest);
#if FRONTROOMS_RELAY_V2
        var spine2 = poses.IndexOf("spine_02");
        subSpine2 = spine2 >= 0 ? Subtree(spine2) : subChest;
#endif
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
        BuildSkin(poses);
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
            UnityEngine.Rendering.Universal.LightExtensions.GetUniversalAdditionalLightData(lensLight).softShadowQuality = UnityEngine.Rendering.Universal.SoftShadowQuality.Low;   // row 16 option D
            lensLight.type = LightType.Spot;
            lensLight.spotAngle = 140f;
            lensLight.innerSpotAngle = 70f;
            lensLight.shadows = LightShadows.None;
            lensLight.color = FrontRoomsRelayLens.LightColour;
            lensLight.renderMode = LightRenderMode.ForcePixel;
            lensLight.enabled = false;
        }
        SetUpContact();
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
        gloveYaw = 0f; gloveLift = 0f; gloveGoal = 0f; gloveSearch = 0; foldDrop = 0f;
#if FRONTROOMS_RELAY_V2
        squareW = 0f; beatTime = 0f; headYawVelocity = 0f; planted = false; plantW = 0f; feetKnown = false;
        plantT[0] = plantT[1] = 1f;
#endif
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

        // ---- smoothed ceiling and room weights (a deep lamp over the body may fold it lower: 40_ceiling_lamps.md)
        ceiling = LampCeiling(ceiling, dt);
        var tau = ceiling < smoothHeight ? .25f : 1f;
        smoothHeight = dt > 0f ? Mathf.Lerp(smoothHeight, ceiling, 1f - Mathf.Exp(-dt / tau)) : smoothHeight;
        var h = smoothHeight;
        var low = 1f - Smooth(2.4f, 2.9f, h);
        var tall = Smooth(4.0f, 5.2f, h);
        var std = Mathf.Max(0f, 1f - low - tall);
        var listening = state == FrontRoomsRelayRig.MotionState.IdleListen || state == FrontRoomsRelayRig.MotionState.Search;
#if FRONTROOMS_RELAY_V2
        UpdateHold(dt, state);
        // the glove comes off the tile for the lock-on; the "almost" stop moves nothing but the head, wherever the glove was
        var lockOn = holdNow == FrontRoomsRelayRig.HoldKind.LockOn;
        var almost = holdNow == FrontRoomsRelayRig.HoldKind.Almost;
        listenW = Mathf.MoveTowards(listenW, lockOn ? 0f : almost ? listenW : listening ? 1f : 0f, dt / (lockOn ? GloveDownSeconds : .25f));
#else
        listenW = Mathf.MoveTowards(listenW, listening ? 1f : 0f, dt / .25f);
#endif

        // ---- door phase
        UpdateDoor(dt, doorSqueeze, playing, moving);
        if (stepU >= 0f) StepDoor(dt);   // V6: a window's frame step

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
#if FRONTROOMS_RELAY_V2
        gaitW = Mathf.MoveTowards(gaitW, walking ? 1f : 0f, dt / (lockOn || almost ? FreezeSeconds : .3f));
#else
        gaitW = Mathf.MoveTowards(gaitW, walking ? 1f : 0f, dt / .3f);
#endif
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
        if (stepU >= 0f) StepLayers();   // V6: a window's frame step
#if FRONTROOMS_RELAY_V2
        SquareUp();
#endif
        UpdateViewer();
        UpdateHead(dt, state, listenTarget, reduceFlashing);

        // ---- forward kinematics, then the ceiling pin
        Forward();
        var topBefore = MeasureTop();
        var pinW = Smooth(.4f, .6f, low + std) * (1f - doorW);
        var pinWant = (h - .02f) - topBefore;
        var pinLow = -.12f;
        if (LampRules)
        {
            // the mesh's true top SkinClear under the tile or a lamp's frame; under a deep fixture the knees take what the low
            // key cannot (the room pose is already folded to it)
            pinWant = Mathf.Min(pinWant, SkinNeed(h));
            pinLow -= PinExtra + Mathf.Max(0f, 2.4f - h);
        }
        var pin = Mathf.Clamp(pinWant, pinLow, .02f) * pinW;
        PinOffset = pin;
        if (pin != 0f) ShiftSubtree(subPelvis, up * pin);
        Top = topBefore + pin;
        if (LampRules) skinTopModel += pin;
        PressesCeiling = pinW >= .5f && (h - Top) <= .05f;
        // V6: the duck under a window's head (the knees take it in Legs, as they take the pin)
        stepDrop = stepU >= 0f ? -StepDuck * StepArc(stepU) : 0f;
        if (stepDrop != 0f) ShiftSubtree(subPelvis, up * stepDrop);
        // the Listen glove on a plain tile
        if (LampRules) GloveRule(dt, listenW * std * room * pinW);

        // ---- the catch: camera clearance and the reach (Run or Listen, camera within 1.5 m of the head)
        Catch(state);

        // ---- gait: foot locking and leg IK
        Legs(dt, walking, stepLen, run);

        // ---- write the bones
        var rootRot = transform.rotation;
        for (var i = 0; i < boneCount; i++)
            bones[i].SetPositionAndRotation(transform.TransformPoint(P[i]), rootRot * R[i]);
        Contact(pinW);

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
#if FRONTROOMS_RELAY_V2
        // Tracked, not latched: walking (the body keeps its heading) and in a hold, the head follows the hunter's LookPoint.
        // One headYaw through every state, so a change of state never pops it; the map clamps LookYaw from Heading, this clamps
        // it from the body, which lags Heading in a turn. The bearing from the axis is LookYaw itself (the point stands 6 m out).
        var held = holdNow != FrontRoomsRelayRig.HoldKind.None;
        if (listenTarget.HasValue && (held || state == FrontRoomsRelayRig.MotionState.Walk))
        {
            latchTimer = 0f;
            snapT = 1f;
            var aim = transform.InverseTransformPoint(listenTarget.Value);
            // in a doorway the hood stays on the door keys' line (the slab is 0.96 m wide): it comes forward as the squeeze comes on
            var goal = Mathf.Clamp(Mathf.Atan2(aim.x, aim.z) * Mathf.Rad2Deg, -HeadLimitDegrees, HeadLimitDegrees) * (1f - doorW);
            if (dt > 0f) headYaw = Mathf.SmoothDamp(headYaw, goal, ref headYawVelocity, held ? HoldTrackSeconds : WalkTrackSeconds, HeadTopSpeed, dt);
            headYaw = Mathf.Clamp(headYaw, -HeadLimitDegrees, HeadLimitDegrees);
            headYawTarget = headYaw;
        }
        else
#endif
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
#if FRONTROOMS_RELAY_V2
        if (!(listenTarget.HasValue && (held || state == FrontRoomsRelayRig.MotionState.Walk))) headYawVelocity = 0f;
        // the hood comes up off its bowed read, under the yaw so it lifts about its own side axis
        var w = SquareShare();
        if (w > 0f && listenTarget.HasValue)
        {
            var from = LocatorDir(locLens);
            var to = transform.InverseTransformPoint(listenTarget.Value) - LocatorModel(locLens);
            var pitchNow = Mathf.Atan2(Vector3.Dot(from, up), new Vector2(from.x, from.z).magnitude) * Mathf.Rad2Deg;
            var pitchGoal = Mathf.Atan2(Vector3.Dot(to, up), new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg;
            var share = 1f - HoodBeatShare * (1f - Smooth(SquareInSeconds, BeatSeconds, beatTime));
            var lift = w * share * Mathf.Clamp(pitchGoal - pitchNow, 0f, HoodLiftMax);
            RotateSubtree(subNeck2, Quaternion.AngleAxis(lift * .3f, left));
            RotateSubtree(subNeck3, Quaternion.AngleAxis(lift * .3f, left));
            RotateSubtree(subHead, Quaternion.AngleAxis(lift * .4f, left));
        }
#endif
        if (Mathf.Abs(headYaw) > .01f)
        {
            RotateSubtree(subNeck3, Quaternion.AngleAxis(headYaw * .35f, up));
            RotateSubtree(subHead, Quaternion.AngleAxis(headYaw * .65f, up));
        }
    }

#if FRONTROOMS_RELAY_V2
    // ------------------------------------------------------------------ the hold: the freeze and the square-up (RELAY 8, C37)
    static float Ease(float t) => t * t * (3f - 2f * t);

    /// <summary>
    /// The hold's clocks. The lens has no part in this: it keeps following the MotionState alone (a hold is IdleListen: 0.06),
    /// so the square-up can never turn into a distance read. The body's turn is the transform's (Heading, in the game).
    /// </summary>
    void UpdateHold(float dt, FrontRoomsRelayRig.MotionState state)
    {
        holdNow = state == FrontRoomsRelayRig.MotionState.IdleListen ? hold : FrontRoomsRelayRig.HoldKind.None;
        var lockOn = holdNow == FrontRoomsRelayRig.HoldKind.LockOn;
        var goal = lockOn ? 1f : holdNow == FrontRoomsRelayRig.HoldKind.Almost ? AlmostShare : 0f;
        squareW = Mathf.MoveTowards(squareW, goal, dt / (goal > squareW ? SquareInSeconds : SquareOutSeconds));
        if (lockOn) beatTime += dt;
        else if (squareW <= 0f) beatTime = 0f;
        var held = holdNow != FrontRoomsRelayRig.HoldKind.None;
        if (held && !planted && feetKnown)
        {
            // the freeze: each foot stays where the frame caught it, mid-stride
            planted = true;
            plantW = 1f;
            var yaw = transform.eulerAngles.y;
            for (var i = 0; i < 2; i++)
            {
                plantWorld[i] = footWorld[i];
                plantYaw[i] = yaw;
                plantT[i] = 1f;
            }
        }
        else if (!held) planted = false;
        if (!planted) plantW = Mathf.MoveTowards(plantW, 0f, dt / PlantOutSeconds);
    }

    // The pose's weight: off in a doorway's keys, and off with the camera in the catch's reach (a touch lock-on), where the catch's
    // own reveal and clearance hold the hood: lifted, the lens would come over the camera (last frame's catch weight).
    float SquareShare() => Ease(squareW) * (1f - doorW) * (1f - catchW);

    /// <summary>The upper body of the square-up (the hood's lift is in UpdateHead): the back straightens, the arms come off the thighs.</summary>
    void SquareUp()
    {
        var w = SquareShare();
        if (w <= 0f) return;
        RotateSubtree(subSpine2, Quaternion.AngleAxis(w * (BackLift + BeatBackLift * Smooth(SquareInSeconds, BeatSeconds, beatTime)), left));
        RotateSubtree(subArmL, Quaternion.AngleAxis(-ArmsOut * w, forward));
        RotateSubtree(subArmR, Quaternion.AngleAxis(ArmsOut * w, forward));
    }

    /// <summary>The gait's foot position given way to the plant. Under a turn a foot takes a short step back under the body, one foot at a time.</summary>
    Vector3 PlantFoot(int i, Vector3 pos, Vector3 neutralWorld, float dt)
    {
        if (planted)
        {
            var p = plantWorld[i];
            p.y = Mathf.MoveTowards(p.y, neutralWorld.y, dt * 1.5f);    // a swinging foot comes down where it is
            var yaw = transform.eulerAngles.y;
            if (plantT[i] < 1f)
            {
                plantT[i] = Mathf.Min(1f, plantT[i] + dt / PlantStepSeconds);
                var t = plantT[i];
                p = Vector3.Lerp(plantFrom[i], neutralWorld, Ease(t)) + Vector3.up * (PlantStepLift * modelScale * Mathf.Sin(t * Mathf.PI));
                if (t >= 1f) plantYaw[i] = yaw;
            }
            else if (plantT[1 - i] >= 1f && Mathf.Abs(Mathf.DeltaAngle(plantYaw[i], yaw)) > PlantTurnDegrees)
            {
                plantFrom[i] = p;
                plantT[i] = 0f;
                PlantSteps++;
            }
            plantWorld[i] = p;
        }
        if (plantW > 0f) pos = Vector3.Lerp(pos, plantWorld[i], plantW);
        footWorld[i] = pos;
        if (i == 1) feetKnown = true;
        return pos;
    }

#endif
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
#if FRONTROOMS_RELAY_V2
        // Only at someone it has: in plain Listen the viewer may be behind it, or behind a door's lite, which is opaque to it (A42).
        // The clearance below stays: that one is for the camera, not for the Relay.
        if (toCam.sqrMagnitude > 1e-4f && (state == FrontRoomsRelayRig.MotionState.Run || holdNow != FrontRoomsRelayRig.HoldKind.None))
#else
        if (toCam.sqrMagnitude > 1e-4f)
#endif
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
        var pinShift = up * (PinOffset + stepDrop);
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
#if FRONTROOMS_RELAY_V2
            pos = PlantFoot(i, pos, neutralWorld, dt);
#endif
            if (stepU >= 0f) pos = StepFoot(i, pos, neutralWorld);   // V6: over a window's sill
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
