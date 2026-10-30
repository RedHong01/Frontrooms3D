using System.Collections.Generic;
using UnityEngine;

/// <summary>How a dressed door is hung (D1.6, research/interactables/d16/10_design.md §1 D1). Double: every map door
/// since A12 / A19 (Red 2026-10-22): no stops, pivots, the leaf rests where it is left. Single: the dormant stop-frame
/// mode of 10_spec §1 (A2 axis, handing mirror), kept as a rig parameter and never selected by D1.6.</summary>
public enum DoorActing { Double, Single }

/// <summary>The door family member (10_spec §2.1): which frame, leaf and hardware a door gets.</summary>
public enum FrontRoomsDoorMember { Lobby, Office, Run, Exit }

/// <summary>
/// The dressed door's moving parts (D1.6, 10_design §4.1), on the empty "Leaf rig" under "Door hinge {a}-{b}". It
/// implements the map's seam (FrontRoomsKitSeam: IFrontRoomsDoorRig) with exactly its members, and is render-only:
/// no collider, no light, no gameplay read. The hinge (the map's) turns the rig; the rig adds what the hinge does not:
/// - the collider follow: the map's crooked hang, rattle jolts and Relay blows move the "Door leaf" collider child
///   (PoseLeaf); the rig copies that offset (colliderLocal ∘ home⁻¹) so the kit leaf shows them with no map change;
/// - the visual pivot (fix 1, VL319): the DA leaf kit turns about its own pivot axis, set PIVOT_Z (36 mm) into the
///   opening from the map's hinge point on the wall centre plane (the leaf sidecar's "pivot_axis" anchor), as a real
///   centre-hung double-acting pivot is set in from the jamb, so the radius stile clears the jamb, the casing and the
///   map's trims at every angle. The hinge (the map's) still turns about its own point: the rig adds the translation
///   R(-a) p - p for the hinge's angle a, so the kit leaf turns about p. Render-only: the map's collider differs from the
///   kit leaf by at most 2 p sin(a / 2) (53 mm at 95 deg); shut (a = 0) nothing moves;
/// - its own Jolt (the unlock's bolt shift), about the pivot, toward the side the leaf opens to (−door.swing·X);
/// - the lock parts: plug (per face), knob / lever (both faces, a through-spindle), deadbolt, latchbolt, from two
///   channels: the seam's Set* calls (the unlock shot) and the facade's event beats (open, rattle, latch click,
///   broken wobble); a part shows the larger of the two, so a beat never fights the shot.
/// Faces: a DoorFace is resolved through the map's own FrontRoomsMapWorld.FaceOf, so the rig is right both under
/// main's pre-A12 faces (S = the swing side) and under A12's geometric faces (S = the door root's +X, 10_design §1
/// D3): the face whose FaceOf a point on the root's +X side answers is the +X side.
/// One hidden driver (FrontRoomsDoorRigDriver) advances every rig in LateUpdate on Time.deltaTime, frozen while the
/// map's DoorsPaused is set. No per-frame allocation.
/// </summary>
public sealed class FrontRoomsDoorRig : MonoBehaviour, IFrontRoomsDoorRig
{
    // ---------------------------------------------------------------- numbers (door root, metres)
    /// <summary>The keyhole on each face: the escutcheon's `keyhole` anchor (0, 0.032, 0.0095) on (±0.022, 0.968, 0.920).</summary>
    public const float KeyholeX = .0315f, KeyholeY = 1.000f, KeyholeZ = .920f;
    /// <summary>A free door's rose spindle point 6.5 mm proud (the seam's fallback for a door with no keyway).</summary>
    public const float RoseSpindleX = .022f + .010f + .0065f;
    public const float DeadboltTravel = .025f, LatchTravelBored = .013f, LatchTravelMortise = .019f;
    /// <summary>The DA latch end (the strike and the jolt's lever arm).</summary>
    public const float LatchEndZ = .995f;
    /// <summary>The dormant Single mode's leaf-rig offset from the A2 hinge (10_spec §6.2), × S_sign on X.</summary>
    public static readonly Vector3 SingleRigOffset = new Vector3(-.0295f, 0f, -.0098f);

    // ---------------------------------------------------------------- state
    static readonly List<FrontRoomsDoorRig> all = new List<FrontRoomsDoorRig>();
    /// <summary>Every live rig (render-only tools: the shot look's LOD0 force, tests).</summary>
    public static IReadOnlyList<FrontRoomsDoorRig> All => all;

    public FrontRoomsMapWorld.Door Door { get; private set; }
    public DoorActing Acting { get; private set; }
    /// <summary>+1 in Double (no handing mirror). Single: −door.swing (10_spec §1.2).</summary>
    public float SSign { get; private set; } = 1f;
    public FrontRoomsDoorMember Member { get; private set; }
    /// <summary>The map this rig's door belongs to.</summary>
    public FrontRoomsMapWorld Map => map;
    /// <summary>The kit leaf's visual pivot on the door root's Z axis (metres from the map's hinge point; fix 1).</summary>
    public float VisualPivotZ => pivotZ;
    /// <summary>The LOCKED model (knob, deadbolt, keyed cylinders) rather than the FREE one (lever, bored latch).</summary>
    public bool LockedModel { get; private set; }
    /// <summary>The frame this rig's door hangs in (its strike's parent; LOD0 force).</summary>
    public Transform Frame { get; private set; }
    public bool StrikeDetached => strikeState != 0;

    FrontRoomsMapWorld map;
    Transform leafCollider;
    Vector3 basePosition, baseScale = Vector3.one;
    // The kit leaf's visual pivot on the door root's Z (fix 1): 0 = the map's hinge point.
    float pivotZ;
    // The last inputs ApplyPose saw (the idle early-out, verify r1 E2): hinge local rotation, collider local pose.
    Quaternion lastHinge = new Quaternion(0f, 0f, 0f, 0f);
    Vector3 lastColliderPos;
    Quaternion lastColliderRot;
    bool poseDirty = true;
    // Number plates (face 0 = +X, 1 = -X) and the identity each one prints (the floor token follows the run).
    readonly GameObject[] plates = new GameObject[2];
    readonly FrontRoomsKeyIdentity[] plateIds = new FrontRoomsKeyIdentity[2];

    // Parts. Index 0 = the root's +X side, 1 = the −X side.
    readonly Transform[] handle = new Transform[2];     // lever (free) or knob (locked)
    readonly Transform[] plug = new Transform[2];
    readonly Quaternion[] handleRest = new Quaternion[2], plugRest = new Quaternion[2];
    readonly float[] handleSign = new float[2], plugSign = new float[2];
    Transform latch, deadbolt, strike;
    Vector3 latchRest, deadboltRest, strikeRestPos;
    Quaternion strikeRestRot;
    float latchTravel = LatchTravelBored;
    LODGroup[] lodGroups;

    // Channels: what the seam (shot) last set, what the beats want; applied = the larger.
    readonly float[] plugShot = new float[2];
    float handleShot, latchShot, boltShot;
    float handleBeat, latchBeat;
    float boltValue = -1f;
    readonly float[] plugApplied = { float.NaN, float.NaN };
    float handleApplied = float.NaN, latchApplied = float.NaN, boltApplied = float.NaN;

    // Jolt (the seam's): latch-end metres, clock, spring.
    float joltClock = -1f, joltDegrees, joltSpring;
    // Beats (the facade's): 1 open lever / knob, 2 rattle knob; latch click; broken wobble.
    int handleBeatKind;
    float handleBeatClock, handleReleaseClock = -1f;
    float latchClickClock = -1f;
    float wobbleClock = -1f;
    // Strike: 0 in place, 1 falling, 2 resting on the floor.
    int strikeState;
    float strikeClock;
    Vector3 strikeFallFrom, strikeFallTo;
    Quaternion strikeRotFrom, strikeRotTo;

    // ---------------------------------------------------------------- beats' numbers (10_design §5.2)
    const float OpenHandleDown = FrontRoomsShotTimings.Open.LeverDown;     // 0.08 s to the full turn
    const float OpenReleaseDelay = .02f, OpenHandleBack = .10f;            // back 0.02 s after the hinge passes 0.5 deg
    const float OpenReleaseAngle = .5f;
    const float OpenHoldMax = 1.5f;                                        // a leaf that never moves lets go anyway
    const float RattleDown = .05f, RattleHoldEnd = .20f, RattleBackEnd = .40f;
    const float LatchClick = .06f;
    const float WobbleDeg = .6f, WobbleHz = 2.4f, WobbleSeconds = .3f;
    const float StrikeFallSeconds = .4f;

    /// <summary>The nearest live rig to a point within a radius (the shot look's LOD0 force), or null.</summary>
    public static FrontRoomsDoorRig Nearest(Vector3 point, float within)
    {
        FrontRoomsDoorRig best = null;
        var bestSq = within * within;
        for (var i = 0; i < all.Count; i++)
        {
            var r = all[i];
            if (r == null || r.Door == null) continue;
            var d = (r.Door.position - point).sqrMagnitude;
            if (d <= bestSq) { bestSq = d; best = r; }
        }
        return best;
    }

    /// <summary>The live LOCKED rig whose keyhole (either face) is nearest a point within a radius, and that face, or null
    /// (the key assembly's tag-over-knob rest, fix 1). No allocation.</summary>
    public static FrontRoomsDoorRig NearestKeyhole(Vector3 point, float within, out DoorFace face)
    {
        FrontRoomsDoorRig best = null;
        face = DoorFace.S;
        var bestSq = within * within;
        for (var i = 0; i < all.Count; i++)
        {
            var r = all[i];
            if (r == null || r.Door == null || !r.LockedModel) continue;
            if ((r.Door.position - point).sqrMagnitude > 4f) continue;   // the keyhole is about 1.1 m from the door's floor centre
            var ds = (r.Keyhole(DoorFace.S) - point).sqrMagnitude;
            if (ds <= bestSq) { bestSq = ds; best = r; face = DoorFace.S; }
            var dp = (r.Keyhole(DoorFace.P) - point).sqrMagnitude;
            if (dp <= bestSq) { bestSq = dp; best = r; face = DoorFace.P; }
        }
        return best;
    }

    // ---------------------------------------------------------------- set-up (the facade)

    internal void Bind(FrontRoomsMapWorld world, FrontRoomsMapWorld.Door door, DoorActing acting, FrontRoomsDoorMember member, bool lockedModel, Transform frame)
    {
        map = world;
        Door = door;
        Acting = acting;
        Member = member;
        LockedModel = lockedModel;
        Frame = frame;
        leafCollider = door != null && door.leaf != null ? door.leaf.transform : null;
        SSign = acting == DoorActing.Double || door == null ? 1f : -door.swing;
        basePosition = acting == DoorActing.Double ? Vector3.zero : new Vector3(SingleRigOffset.x * SSign, SingleRigOffset.y, SingleRigOffset.z);
        baseScale = acting == DoorActing.Double ? Vector3.one : new Vector3(SSign, 1f, 1f);
        latchTravel = lockedModel ? LatchTravelMortise : LatchTravelBored;
        if (!all.Contains(this)) all.Add(this);
        FrontRoomsDoorRigDriver.Ensure();
    }

    /// <summary>Register a face part. side 0 = the root's +X face, 1 = −X. sign: the mirror rule (+1, or −1 on the −X face).</summary>
    internal void SetHandle(int side, Transform part, float sign)
    {
        handle[side] = part;
        handleRest[side] = part != null ? part.localRotation : Quaternion.identity;
        handleSign[side] = sign;
    }

    internal void SetPlugPart(int side, Transform part, float sign)
    {
        plug[side] = part;
        plugRest[side] = part != null ? part.localRotation : Quaternion.identity;
        plugSign[side] = sign;
    }

    internal void SetBolts(Transform latchPart, Transform deadboltPart)
    {
        latch = latchPart;
        deadbolt = deadboltPart;
        if (latch != null) latchRest = latch.localPosition;
        if (deadbolt != null) deadboltRest = deadbolt.localPosition;
    }

    internal void SetStrike(Transform strikePart)
    {
        strike = strikePart;
        if (strike != null) { strikeRestPos = strike.localPosition; strikeRestRot = strike.localRotation; }
    }

    /// <summary>The kit leaf's visual pivot (the leaf sidecar's pivot_axis Z); Double mode only.</summary>
    internal void SetVisualPivot(float z)
    {
        pivotZ = Acting == DoorActing.Double ? Mathf.Clamp(z, 0f, .1f) : 0f;
        poseDirty = true;
    }

    /// <summary>A number plate on face side (0 = +X, 1 = -X) and the identity it prints.</summary>
    internal void SetPlate(int side, GameObject plate, FrontRoomsKeyIdentity id)
    {
        plates[side] = plate;
        plateIds[side] = id;
    }

    /// <summary>The plate on a side (null when none) and its identity (the facade re-sets the token on a floor change).</summary>
    internal GameObject PlateOf(int side) => plates[side];
    internal FrontRoomsKeyIdentity PlateIdOf(int side) => plateIds[side];
    internal void SetPlateId(int side, FrontRoomsKeyIdentity id) => plateIds[side] = id;

    internal void CollectLods()
    {
        var a = Frame != null ? Frame.GetComponentsInChildren<LODGroup>(true) : new LODGroup[0];
        var b = GetComponentsInChildren<LODGroup>(true);
        lodGroups = new LODGroup[a.Length + b.Length];
        a.CopyTo(lodGroups, 0);
        b.CopyTo(lodGroups, a.Length);
    }

    void OnDestroy()
    {
        all.Remove(this);
    }

    // ---------------------------------------------------------------- faces

    /// <summary>The geometric side (+1 = the root's +X, −1 = −X) a DoorFace stands for, through the map's own FaceOf.</summary>
    public float SideOf(DoorFace face)
    {
        if (Door == null) return face == DoorFace.S ? 1f : -1f;
        var probe = Door.position + RootRotation * Vector3.right;
        return FrontRoomsMapWorld.FaceOf(Door, probe) == face ? 1f : -1f;
    }

    /// <summary>The geometric side of a world point: + on the root's +X side (10_design §5.2, event handlers).</summary>
    public float SideOfPoint(Vector3 point)
    {
        if (Door == null) return 1f;
        return Vector3.Dot(point - Door.position, RootRotation * Vector3.right) >= 0f ? 1f : -1f;
    }

    /// <summary>The DoorFace of a geometric side (the inverse of SideOf).</summary>
    public DoorFace FaceOfSide(float side) => SideOf(DoorFace.S) * side > 0f ? DoorFace.S : DoorFace.P;

    // The shut door root's world rotation (the map's DoorRotation).
    Quaternion RootRotation => Door != null && Door.hinge != null && Door.hinge.parent != null ? Door.hinge.parent.rotation * Door.closed : Door != null ? Door.closed : transform.rotation;

    static int Index(float side) => side > 0f ? 0 : 1;

    // ---------------------------------------------------------------- IFrontRoomsDoorRig

    public Vector3 Keyhole(DoorFace face)
    {
        var side = SideOf(face);
        var x = LockedModel ? KeyholeX : RoseSpindleX;
        // Through the rig's current pose; the dormant Single mode's mirror is in the rig's scale.
        return transform.TransformPoint(new Vector3(side * x * (Acting == DoorActing.Double ? 1f : SSign), KeyholeY, KeyholeZ));
    }

    public Vector3 KeyholeDir(DoorFace face)
    {
        var side = SideOf(face);
        return transform.TransformDirection(new Vector3(-side, 0f, 0f)).normalized;
    }

    public Vector3 KeyholeUp(DoorFace face) => transform.TransformDirection(Vector3.up).normalized;

    public void SetPlug(DoorFace face, float degrees)
    {
        if (!LockedModel) return;
        plugShot[Index(SideOf(face))] = degrees;
        ApplyParts();
    }

    public void SetKnob(DoorFace face, float degrees)
    {
        if (!LockedModel) return;
        handleShot = degrees;
        ApplyParts();
    }

    public void SetLever(DoorFace face, float degrees)
    {
        if (LockedModel) return;
        handleShot = degrees;
        ApplyParts();
    }

    public void SetDeadbolt(float t)
    {
        boltShot = Mathf.Clamp01(t);
        ApplyParts();
    }

    public void SetLatch(float t)
    {
        latchShot = Mathf.Clamp01(t);
        ApplyParts();
    }

    public void Jolt(float metres)
    {
        var m = Mathf.Abs(metres);
        if (m <= 0f) return;
        poseDirty = true;
        joltDegrees = Mathf.Asin(Mathf.Clamp(m / LatchEndZ, 0f, 1f)) * Mathf.Rad2Deg;
        joltSpring = m <= .0016f ? FrontRoomsShotTimings.Unlock.BoltJolt
            : m <= .0025f ? FrontRoomsShotTimings.Rattle.JoltSpring
            : FrontRoomsShotTimings.DoorBreak.LeafJoltSpring;
        joltClock = 0f;
    }

    // ---------------------------------------------------------------- beyond the seam

    /// <summary>The deadbolt's persistent value (0 thrown, 1 drawn): an unlocked locked-model door keeps it drawn for the run.</summary>
    public void SetDeadboltRest(float t)
    {
        boltValue = Mathf.Clamp01(t);
        boltShot = boltValue;
        ApplyParts();
    }

    /// <summary>The strike comes off the frame: a render-only 0.4 s fall to a floor rest away from <paramref name="from"/>. Idempotent.</summary>
    public void DetachStrike(Vector3 from) => DetachStrike(from, false);

    /// <summary>Detach the strike; <paramref name="atRest"/> puts it on the floor at once (a broken door rebuilt).</summary>
    public void DetachStrike(Vector3 from, bool atRest)
    {
        if (strike == null || strikeState != 0) return;
        var away = -SideOfPoint(from);
        var h = (uint)(Door != null ? Door.edge * 2654435761L : 7);
        var yaw = (h % 360u) - 180f;
        var reach = .18f + (h >> 8) % 100u * .0015f;
        var parent = strike.parent;
        // The rest pose, in the frame's space: on the floor (just above the saddle's level) beside the latch jamb, flat, front up.
        var restDoor = new Vector3(away * reach, .004f, .90f - (h >> 16) % 100u * .002f);
        strikeFallFrom = strike.localPosition;
        strikeRotFrom = strike.localRotation;
        strikeFallTo = parent == Frame || parent == null ? restDoor : parent.InverseTransformPoint(Frame.TransformPoint(restDoor));
        strikeRotTo = Quaternion.Euler(-90f, yaw, 0f);
        strikeClock = 0f;
        strikeState = 1;
        if (atRest) { strikeClock = StrikeFallSeconds; ApplyStrike(); }
    }

    /// <summary>LOD0 on every LODGroup of the frame and the leaf rig (the unlock shot's door, 10_design §8), or back to automatic.</summary>
    public void ForceLod0(bool on)
    {
        if (lodGroups == null) CollectLods();
        for (var i = 0; i < lodGroups.Length; i++)
            if (lodGroups[i] != null) lodGroups[i].ForceLOD(on ? 0 : -1);
    }

    // ---- the facade's beats (10_design §5.2)

    internal void BeatOpen()
    {
        handleBeatKind = 1;
        handleBeatClock = 0f;
        handleReleaseClock = -1f;
    }

    internal void BeatRattle()
    {
        handleBeatKind = 2;
        handleBeatClock = 0f;
    }

    internal void BeatLatchClick() => latchClickClock = 0f;

    internal void BeatWobble() { wobbleClock = 0f; poseDirty = true; }

    // ---------------------------------------------------------------- per frame

    /// <summary>True while a beat, jolt, wobble or strike fall is running (the driver steps only these and moved doors).</summary>
    bool Active => handleBeatKind != 0 || latchClickClock >= 0f || joltClock >= 0f || wobbleClock >= 0f || strikeState == 1;

    // The hinge or the map's collider child moved since the last pose (the door swung, hung crooked, was jolted).
    bool Moved()
    {
        if (poseDirty) return true;
        if (Door != null && Door.hinge != null)
        {
            var h = Door.hinge.localRotation;
            if (h.x != lastHinge.x || h.y != lastHinge.y || h.z != lastHinge.z || h.w != lastHinge.w) return true;
        }
        if (leafCollider != null)
        {
            leafCollider.GetLocalPositionAndRotation(out var p, out var r);
            if (p.x != lastColliderPos.x || p.y != lastColliderPos.y || p.z != lastColliderPos.z) return true;
            if (r.x != lastColliderRot.x || r.y != lastColliderRot.y || r.z != lastColliderRot.z || r.w != lastColliderRot.w) return true;
        }
        return false;
    }

    /// <summary>One step (the driver's LateUpdate, the facade's dress, tools in edit mode). An idle rig whose hinge and
    /// collider have not moved returns after a few compares (verify r1 E2: about 100-200 live rigs at 25 chunks).</summary>
    internal void Tick(float dt)
    {
        if (this == null) return;
        if (!Active && !Moved()) return;
        var paused = map != null && map.DoorsPaused;
        if (paused) dt = 0f;
        TickBeats(dt);
        if (joltClock >= 0f)
        {
            joltClock += dt;
            if (joltClock >= joltSpring) joltClock = -1f;
        }
        if (wobbleClock >= 0f)
        {
            wobbleClock += dt;
            if (wobbleClock >= WobbleSeconds) wobbleClock = -1f;
        }
        if (strikeState == 1)
        {
            strikeClock += dt;
            ApplyStrike();
        }
        ApplyPose();
        ApplyParts();
    }

    void TickBeats(float dt)
    {
        var maxDeg = LockedModel ? FrontRoomsShotTimings.Rattle.KnobDeg : FrontRoomsShotTimings.Open.LeverDownDeg;
        handleBeat = 0f;
        if (handleBeatKind == 1)
        {
            handleBeatClock += dt;
            var down = Mathf.Clamp01(handleBeatClock / OpenHandleDown);
            var released = Door == null || Door.angle > OpenReleaseAngle || handleBeatClock > OpenHoldMax;
            if (released && handleReleaseClock < 0f && handleBeatClock >= OpenHandleDown) handleReleaseClock = 0f;
            var back = 0f;
            if (handleReleaseClock >= 0f)
            {
                handleReleaseClock += dt;
                back = Mathf.Clamp01((handleReleaseClock - OpenReleaseDelay) / OpenHandleBack);
            }
            handleBeat = maxDeg * Smooth(down) * (1f - Smooth(back));
            if (back >= 1f) handleBeatKind = 0;
        }
        else if (handleBeatKind == 2)
        {
            handleBeatClock += dt;
            var t = handleBeatClock;
            var k = t < RattleDown ? t / RattleDown : t < RattleHoldEnd ? 1f : t < RattleBackEnd ? 1f - (t - RattleHoldEnd) / (RattleBackEnd - RattleHoldEnd) : 0f;
            handleBeat = FrontRoomsShotTimings.Rattle.KnobDeg * Smooth(k) * (LockedModel ? 1f : FrontRoomsShotTimings.Rattle.LeverDeg / FrontRoomsShotTimings.Rattle.KnobDeg);
            if (t >= RattleBackEnd) handleBeatKind = 0;
        }
        latchBeat = maxDeg > 0f ? handleBeat / maxDeg : 0f;
        if (latchClickClock >= 0f)
        {
            latchClickClock += dt;
            var u = Mathf.Clamp01(latchClickClock / LatchClick);
            latchBeat = Mathf.Max(latchBeat, Mathf.Sin(u * Mathf.PI));
            if (u >= 1f) latchClickClock = -1f;
        }
    }

    static float Smooth(float u)
    {
        u = Mathf.Clamp01(u);
        return u * u * (3f - 2f * u);
    }

    // A jolt's size through its spring: out in the first fifth, then eased home (the map's JoltShape).
    static float JoltShape(float s)
    {
        if (s <= 0f || s >= 1f) return 0f;
        if (s < .2f) return s / .2f;
        var back = 1f - (s - .2f) / .8f;
        return back * back;
    }

    // The rig's pose under the hinge: the collider follow ∘ the base ∘ the jolt and wobble.
    void ApplyPose()
    {
        var followPos = Vector3.zero;
        var followRot = Quaternion.identity;
        if (leafCollider != null && Door != null)
        {
            followRot = leafCollider.localRotation;
            followPos = leafCollider.localPosition - followRot * Door.leafHome;
        }
        var swing = Door != null ? Door.swing : 1f;
        var turn = 0f;
        if (joltClock >= 0f) turn += -swing * joltDegrees * JoltShape(joltClock / Mathf.Max(.01f, joltSpring));
        if (wobbleClock >= 0f) turn += WobbleDeg * Mathf.Exp(-wobbleClock / (WobbleSeconds * .4f)) * Mathf.Sin(2f * Mathf.PI * WobbleHz * wobbleClock);
        // The visual pivot (fix 1): the hinge turns the rig by a about the map's point; R(-a) p - p puts the turn about p.
        var pivot = Vector3.zero;
        if (pivotZ > 0f && Door != null && Door.hinge != null)
        {
            var a = Quaternion.Inverse(Door.closed) * Door.hinge.localRotation;
            var p = new Vector3(0f, 0f, pivotZ);
            pivot = Quaternion.Inverse(a) * p - p;
        }
        var pos = followPos + followRot * (basePosition + pivot);
        var rot = followRot * Quaternion.Euler(0f, turn, 0f);
        // Exact compares: Unity's == on Quaternion treats turns under ~0.16 deg as equal, which would drop the 1.5 mm unlock
        // jolt (0.09 deg) and leave a small rest turn behind.
        var t = transform;
        t.GetLocalPositionAndRotation(out var curPos, out var curRot);
        if (curPos.x != pos.x || curPos.y != pos.y || curPos.z != pos.z || curRot.x != rot.x || curRot.y != rot.y || curRot.z != rot.z || curRot.w != rot.w)
            t.SetLocalPositionAndRotation(pos, rot);
        var curScale = t.localScale;
        if (curScale.x != baseScale.x || curScale.y != baseScale.y || curScale.z != baseScale.z) t.localScale = baseScale;
        if (Door != null && Door.hinge != null) lastHinge = Door.hinge.localRotation;
        if (leafCollider != null) leafCollider.GetLocalPositionAndRotation(out lastColliderPos, out lastColliderRot);
        poseDirty = false;
    }

    void ApplyParts()
    {
        var h = Mathf.Max(handleShot, handleBeat);
        if (h != handleApplied)
        {
            handleApplied = h;
            for (var i = 0; i < 2; i++)
            {
                if (handle[i] == null) continue;
                // Lever: rotate about part −Z, + = grip down. Knob: about part +Z, + = retract. Negated on the mirrored −X part.
                var axis = LockedModel ? Vector3.forward : Vector3.back;
                handle[i].localRotation = handleRest[i] * Quaternion.AngleAxis(handleSign[i] * h, axis);
            }
        }
        for (var i = 0; i < 2; i++)
        {
            var p = plugShot[i];
            if (p == plugApplied[i] || plug[i] == null) continue;
            plugApplied[i] = p;
            // Plug: rotate about part −Z, + = cuts toward the hinge; negated on the −X face (G2 §4.7).
            plug[i].localRotation = plugRest[i] * Quaternion.AngleAxis(plugSign[i] * p, Vector3.back);
        }
        var l = Mathf.Max(latchShot, latchBeat);
        if (l != latchApplied && latch != null)
        {
            latchApplied = l;
            latch.localPosition = latchRest + Vector3.back * (latchTravel * l);
        }
        var b = Mathf.Max(boltShot, Mathf.Max(0f, boltValue));
        if (b != boltApplied && deadbolt != null)
        {
            boltApplied = b;
            deadbolt.localPosition = deadboltRest + Vector3.back * (DeadboltTravel * b);
        }
    }

    void ApplyStrike()
    {
        if (strike == null) return;
        var u = Mathf.Clamp01(strikeClock / StrikeFallSeconds);
        // A short arc: out from the lining, then down (gravity-like ease-in on Y).
        var p = Vector3.Lerp(strikeFallFrom, strikeFallTo, u);
        p.y = Mathf.Lerp(strikeFallFrom.y, strikeFallTo.y, u * u) + Mathf.Sin(u * Mathf.PI) * .05f * (1f - u);
        strike.localPosition = p;
        strike.localRotation = Quaternion.Slerp(strikeRotFrom, strikeRotTo, Smooth(u));
        if (u >= 1f) strikeState = 2;
    }

    // ---------------------------------------------------------------- tools and tests

    /// <summary>Edit mode (tools, tests, the facade's dress): step every rig as the driver would.</summary>
    public static void TickAllForTools(float dt)
    {
        for (var i = all.Count - 1; i >= 0; i--)
        {
            if (all[i] == null) { all.RemoveAt(i); continue; }
            all[i].Tick(dt);
        }
    }

    /// <summary>Tests: the values the parts show now (handle, latch, deadbolt; plug on the +X / −X face).</summary>
    public Vector4 AppliedForTests => new Vector4(handleApplied, latchApplied, boltApplied, plugApplied[0]);
    public float PlugAppliedForTests(int side) => plugApplied[side];
    public Transform HandleForTests(int side) => handle[side];
    public Transform PlugForTests(int side) => plug[side];
    public Transform LatchForTests => latch;
    public Transform DeadboltForTests => deadbolt;
    public Transform StrikeForTests => strike;
}
