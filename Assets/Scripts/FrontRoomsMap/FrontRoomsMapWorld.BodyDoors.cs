using System;
using System.Collections.Generic;
using UnityEngine;
using FrontRooms.Map;

// All logic is compiled in both build configurations. The game enables BodyDoors only with Relay v2.
public sealed partial class FrontRoomsMapWorld
{
    public enum BodyDoorGait { Slow, Walk, Sprint }
    public enum BodyDoorBump { None, Lean, LockedSilent, Locked, HaveKey, Broken, Sealed, Alarmed, Held }

    public struct BodyDoorInput
    {
        public Vector3 feet, inputMove, viewForward, aimPoint;
        public float dt, sinceSprint, rawTilt;
        public bool sprintingNow, playerMove;
        public BodyDoorGait gait;
        public Door peekDoor, aimDoor;
    }

    public struct BodyDoorResult
    {
        public float moveScale;
        public Door door;
        public BodyDoorBump bumpKind;
        public DoorSwingKind verb;
        public bool blocked, opened, shut, bodyMoved;
    }

    public event Action<Door> DoorLatchBurst;
    public event Action<Door> DoorBodyBlocked;
    public event Action<Door> DoorSlammedInto;
    public event Action<Door> DoorHeldAgainstRelay;
    public event Action<Door> DoorThrowStoppedByPlayer;

    public const float BodyDoorReach = .355f;
    public const float BodyDoorPullSeconds = .30f;
    public const float BodyDoorPullBackSeconds = .12f;
    public const float BodyDoorBargeGrace = .15f;
    public const float BodyDoorSlamWindow = 2f;
    public const float BodyDoorUnlockDegrees = 10f;
    /// <summary>A barge meets a plain door this long before the shoulder would (or this frame's move, if longer), so the leaf leaves first.</summary>
    public const float BodyDoorBargeLead = .035f;

    // One motion per body swing (barge 2026-11-01, Red: "门被撞开的这套逻辑实际体验特别卡卡的"): the verb's ease-out, at a speed
    // planned from the body's straight path so the leaf stays ahead of it on every frame; the plan only ever raises the speed.
    // The plan keeps the leaf this far past the reach, looks this far along the path in these steps, and may ask at most a full
    // swing in BodySwingMinSeconds (a hit beside the hinge). A body this close to a leaf has its swing re-checked every frame.
    const float BodyPlanSlack = .015f, BodyPlanMetres = 1.6f, BodyPlanStep = .01f, BodySwingMinSeconds = .12f, BodyPlanNear = 2f;
    // The most a plan asks of a leaf: square to the wall (past it a body is beside the hinge, where nothing clears it).
    const float BodyNeedCapDegrees = 90f;
    // A squeeze keeps its contact this far past the reach, and while the leaf turns away a press within 80 deg of its face.
    const float BodyFollowSlack = .05f, BodyFollowCone = .17f;

    // For either parity of a cell, its north/south and east/west leaves choose different corners.
    public static bool BodyDoorReverseHinge(GridCoord a, GridCoord b)
    {
        if (b.x < a.x || b.y < a.y) { var swap = a; a = b; b = swap; }
        var odd = ((a.x + a.y) & 1) != 0;
        return b.x == a.x ? !odd : odd;
    }

    bool bodyDoors;
    public bool BodyDoors
    {
        get => bodyDoors;
        set
        {
            if (bodyDoors == value) return;
            ResetBodyDoorInput();
            bodyDoors = value;
            // V7b look (visual line): the alarm bar reads its press through this hook; render-only, set once, no capture.
            FrontRoomsInteractableKit.AlarmBarPressFromMap = (map, door) => map != null ? map.AlarmBarPress(door) : 0f;
        }
    }

    sealed class BodyContact
    {
        public float nearAt, pressedAt, lean;
        public bool verb, handle, bump;
        public BodyDoorBump lastBump;
    }
    readonly Dictionary<long, BodyContact> bodyContacts = new Dictionary<long, BodyContact>();
    readonly HashSet<long> bodyRelayTouched = new HashSet<long>();
    readonly Dictionary<long, float> bodyRestAngles = new Dictionary<long, float>();
    readonly Dictionary<long, float> bodyHeldSeconds = new Dictionary<long, float>();
    readonly HashSet<long> bodyHeldReported = new HashSet<long>();
    readonly HashSet<long> bodyThrowReported = new HashSet<long>();
    readonly HashSet<long> bodySlamReported = new HashSet<long>();
    readonly List<long> bodyScratch = new List<long>();
    float bodyClock, bodyPullLook, bodyPullBack, bodyPassedAt = float.NegativeInfinity;
    long bodyPullEdge, bodyPassedEdge;
    bool bodyPreviousValid, bodyPreviousSprint, bodyPassedSprint;
    Vector3 bodyPreviousFeet;
    Door bodyPromptDoor;
    bool bodyPromptSlam;
    // TickDoors' count (a body swing stepped by the solver since the last one is left alone there), the solver's own step
    // in progress, and a leaf near the body moved this frame (one SyncTransforms before the body's move).
    int doorTicks;
    bool bodyStepping, bodySyncPending;
    readonly List<Door> bodyStepScratch = new List<Door>();

    /// <summary>Discard input holds after a pause, teleport or move lock. Rest angles are not lost.</summary>
    public void ResetBodyDoorInput()
    {
        bodyContacts.Clear();
        bodyPreviousValid = false;
        bodyPullLook = bodyPullBack = 0f;
        bodyPullEdge = 0;
        bodyPassedAt = float.NegativeInfinity;
        bodyPromptDoor = null;
        foreach (var d in doorByEdge.Values)
        {
            d.bodyFloorActive = false;
            // TickDoors moves its swing again from the next tick on.
            d.bodyStepTick = int.MinValue;
            if (d.motion == DoorMotion.Follow) RestBodyDoor(d);
        }
    }

    void ForgetBodyDoors()
    {
        ResetBodyDoorInput();
        bodyRestAngles.Clear();
        bodyRelayTouched.Clear();
        bodyHeldSeconds.Clear();
        bodyHeldReported.Clear();
        bodyThrowReported.Clear();
        bodySlamReported.Clear();
        bodyClock = 0f;
    }

    /// <summary>Already primed by a settled look at the latch half. Never names an opening/closing button.</summary>
    public string BodyDoorPullPrompt(Door door)
    {
        if (!BodyDoors || door == null || bodyPromptDoor == null || door.edge != bodyPromptDoor.edge) return null;
        return bodyPromptSlam ? "STEP BACK  ·  SLAM DOOR" : "STEP BACK  ·  SHUT DOOR";
    }

    public bool RelayDoorHeldByPlayer(Door door) => door != null && bodyHeldReported.Contains(door.edge);

    /// <summary>The 10A hold on an armed alarm door's bar (the alarm branch of SolveBodyDoors uses the same 0.4 s), and the
    /// press a refused push (slow, sprint, oblique: the hard stop and its strain cue) shows on the bar.</summary>
    public const float AlarmBarHoldSeconds = .4f, AlarmBarStrain = .35f;

    /// <summary>
    /// V7b look (visual line; render-only, reads the episode, changes nothing): the press on an armed alarm door's bar this
    /// frame, signed by the face pushed (+ from the door root's +X side, - from -X, FaceOf's geometry): the facing push's hold
    /// so far over AlarmBarHoldSeconds (0..1), AlarmBarStrain for a push the door refuses, 0 with no body contact in the last
    /// 0.10 s, with body doors off, or on a door that is not an armed alarm door. FrontRoomsAlarmDoorLook moves the bar by it.
    /// </summary>
    public float AlarmBarPress(Door door)
    {
        if (!BodyDoors || door == null || !bodyPreviousValid || !IsAlarmArmed(door)) return 0f;
        if (!bodyContacts.TryGetValue(door.edge, out var c) || bodyClock - c.pressedAt > .10f) return 0f;
        var press = c.lean > 0f ? Mathf.Clamp01(c.lean / AlarmBarHoldSeconds)
            : c.bump && c.lastBump == BodyDoorBump.Alarmed ? AlarmBarStrain : 0f;
        return Vector3.Dot(bodyPreviousFeet - door.position, DoorRotation(door) * Vector3.right) >= 0f ? press : -press;
    }

    /// <summary>
    /// One input-only horizontal move, before CharacterController.Move. inputMove is a world displacement,
    /// not velocity. dt is supplied by the caller (seconds), so all gates behave at 20, 30, 60 and 120 Hz.
    /// Tools may enable BodyDoors directly; the production game also requires Relay v2.
    /// </summary>
    public BodyDoorResult SolveBodyDoors(BodyDoorInput input)
    {
        var result = new BodyDoorResult { moveScale = 1f };
        if (!BodyDoors || DoorsPaused || !input.playerMove || input.dt <= 0f)
        {
            ResetBodyDoorInput();
            return result;
        }
        var dt = Mathf.Min(input.dt, .25f);
        bodyClock += dt;
        input.inputMove.y = 0f;
        input.viewForward.y = 0f;
        input.viewForward = input.viewForward.sqrMagnitude > 1e-6f ? input.viewForward.normalized : Vector3.forward;
        TrackBodyPasses(input);
        bodyPromptDoor = null;
        if (!TryBodyPull(input, ref result)) result = SolveBodyContact(input, dt, result);
        // Every body swing moves here, once a frame and before the body does, so the controller meets the leaf where it is drawn.
        StepBodySwings(input, dt, ref result);
        if (bodySyncPending)
        {
            // Only body-driven transforms need a sync; the rest of the world never pays this per frame.
            bodySyncPending = false;
            Physics.SyncTransforms();
            result.bodyMoved = true;
        }
        return result;
    }

    BodyDoorResult SolveBodyContact(BodyDoorInput input, float dt, BodyDoorResult result)
    {
        var moving = input.inputMove.sqrMagnitude > 1e-8f;
        var speed = input.inputMove.magnitude / dt;
        var deliberate = moving && speed >= 1f && input.rawTilt >= .2f;
        var direction = moving ? input.inputMove.normalized : Vector3.zero;
        var slow = input.gait == BodyDoorGait.Slow;
        var sprint = input.sprintingNow || input.gait == BodyDoorGait.Sprint;
        var barge = sprint || input.sinceSprint >= 0f && input.sinceSprint <= BodyDoorBargeGrace
            && speed >= 3.2f - .01f && Vector3.Dot(direction, input.viewForward) >= Mathf.Cos(70f * Mathf.Deg2Rad);
        // Where a barge meets a plain door: BodyDoorBargeLead ahead, so the latch bursts before the shoulder arrives.
        var ahead = input.feet + direction * (speed * Mathf.Max(dt, BodyDoorBargeLead));
        Door contact = null;
        float best = float.PositiveInfinity;
        foreach (var door in doorByEdge.Values)
        {
            if (door.hinge == null) continue;
            door.bodyFloorActive = false;
            var distance = BodyLeafDistance(door, input.feet, out _);
            if (bodyContacts.TryGetValue(door.edge, out var old))
            {
                if (distance <= .5f) old.nearAt = bodyClock;
                if (bodyClock - old.pressedAt > .10f) { old.lean = 0f; old.handle = false; }
            }
            // The Relay's push of a leaf the body was driving keeps clear of the body as before (the player's own swings: StepBodySwings).
            if (door.bodyDriven && door.swingKind == DoorSwingKind.Relay && (door.motion == DoorMotion.Swing || door.motion == DoorMotion.Through))
                FloorBodyDoor(door, input.feet + input.inputMove, DoorMaxStepDegrees);
            if (!deliberate || door.angle >= DoorPassableDegrees && door.motion == DoorMotion.None) continue;
            var follow = door.motion == DoorMotion.Follow;
            var next = input.feet + input.inputMove;
            var nextDistance = BodyLeafDistance(door, next, out var along);
            if (barge && nextDistance > BodyDoorReach && BargeReady(door))
            {
                var aheadDistance = BodyLeafDistance(door, ahead, out var aheadAlong);
                if (aheadDistance <= BodyDoorReach) { next = ahead; nextDistance = aheadDistance; along = aheadAlong; }
            }
            // A squeeze holds its leaf a little past the reach and the cone (the leaf turns away under the press).
            if (nextDistance > BodyDoorReach + (follow ? BodyFollowSlack : 0f) || along < .15f || along > LeafLength) continue;
            var normal = BodyLeafNormal(door, input.feet);
            if (Vector3.Dot(direction, -normal) < (follow ? BodyFollowCone : .5f)) continue;
            // Only a crossing can carry a partially open leaf through the frame.
            if (!IsLatched(door) && AheadOfLeaf(door, input.feet) && !BodyIntendsCrossing(door, input.feet, next)) continue;
            if (nextDistance < best) { contact = door; best = nextDistance; }
        }
        bodyScratch.Clear();
        foreach (var pair in bodyContacts)
            if (bodyClock - pair.Value.nearAt > .5f) bodyScratch.Add(pair.Key);
        foreach (var edge in bodyScratch) bodyContacts.Remove(edge);
        foreach (var other in doorByEdge.Values)
            if (other != contact && other.motion == DoorMotion.Follow) RestBodyDoor(other);
        if (contact == null) return result;
        var d = contact;
        result.door = d;
        if (!bodyContacts.TryGetValue(d.edge, out var episode))
        {
            episode = new BodyContact { nearAt = bodyClock, pressedAt = bodyClock };
            bodyContacts.Add(d.edge, episode);
        }
        episode.nearAt = episode.pressedAt = bodyClock;
        if (IsSealedExit(d)) return BodyBlocked(d, episode, BodyDoorBump.Sealed, ref result);
        if (IsBeingBroken(d) || IsHeldForKey(d) || d.motion == DoorMotion.Throw || d.motion == DoorMotion.Bounce
            || d.swingKind == DoorSwingKind.Relay && d.motion != DoorMotion.None)
            return BodyBlocked(d, episode, BodyDoorBump.Held, ref result);
        if (d.broken)
        {
            if (!slow && !episode.bump && bodyClock - d.brokenTriedAt >= FrontRoomsShotTimings.BrokenDoor.Duration)
            {
                d.brokenTriedAt = bodyClock;
                DoorBrokenTried?.Invoke(d, input.feet);
            }
            return BodyBlocked(d, episode, slow ? BodyDoorBump.None : BodyDoorBump.Broken, ref result);
        }
        if (NeedsKey(d))
        {
            if (KeyFor(d, input.feet).HasValue) return BodyBlocked(d, episode, BodyDoorBump.HaveKey, ref result);
            if (!slow && !episode.bump && doorClock - d.rattledAt >= FrontRoomsShotTimings.Rattle.Duration)
            {
                d.rattledAt = doorClock;
                SwingAwayFrom(d, input.feet);
                DoorLocked?.Invoke(d.position);
                DoorRattled?.Invoke(d, LockPoint(d, input.feet));
                Rattle(d);
            }
            return BodyBlocked(d, episode, slow ? BodyDoorBump.LockedSilent : BodyDoorBump.Locked, ref result);
        }
        var face = BodyLeafNormal(d, input.feet);
        var facing = Vector3.Dot(input.viewForward, -face);
        if (IsAlarmArmed(d))
        {
            if (slow || sprint || facing < .5f || Vector3.Dot(direction, -face) < Mathf.Cos(30f * Mathf.Deg2Rad))
            {
                episode.lean = 0f;
                return BodyBlocked(d, episode, BodyDoorBump.Alarmed, ref result);
            }
            episode.lean += dt;
            if (episode.lean < .4f) return BodyBlocked(d, episode, BodyDoorBump.Alarmed, ref result, false);
            barge = false;
        }
        else if (IsLatched(d) && !barge)
        {
            // The walk's lean is 0.12 s whatever the aim: every map leaf has a vision lite, so PEEK's 0.30 s held every walk
            // into every door dead for 0.30 s (measured 2026-11-01). PEEK is still there before the press and while the lean holds.
            var lean = facing < .5f ? .40f : slow ? .30f : .12f;
            episode.lean += dt;
            if (!episode.handle && episode.lean >= lean - .10f)
            {
                SwingAwayFrom(d, input.feet);
                DoorHandleTurned?.Invoke(d, LockPoint(d, input.feet));
                episode.handle = true;
            }
            if (episode.lean + 1e-5f < lean)
                return BodyBlocked(d, episode, BodyDoorBump.Lean, ref result, false);
        }
        if (d.motion == DoorMotion.Shut || d.motion == DoorMotion.Lever)
            return BodyBlocked(d, episode, BodyDoorBump.Held, ref result);
        if (!episode.verb)
        {
            episode.verb = true;
            var verb = barge ? DoorSwingKind.Barge : DoorSwingKind.Ease;
            if (slow) BeginBodyFollow(d, input.feet, episode.handle, verb);
            else
            {
                OpenDoor(d, input.feet, DoorOpenSource.Body, true, verb);
                if (!episode.handle && !barge) DoorHandleTurned?.Invoke(d, LockPoint(d, input.feet));
                if (barge) DoorLatchBurst?.Invoke(d);
            }
            d.bodyDriven = true;
            result.opened = true;
            result.verb = verb;
        }
        else if (!slow && (d.motion == DoorMotion.Follow || d.motion == DoorMotion.None && d.bodyDriven && !IsLatched(d)
            && !AheadOfLeaf(d, input.feet)))
        {
            // A single contact stays a single noise even when the player changes gait, or presses on, while pushing it.
            d.open = true;
            openDoors.Add(d.edge);
            ajarDoors.Remove(d.edge);
            StartSwing(d, ModuleUnits.DoorSwingDegrees, true);
            if (!movingDoors.Contains(d)) movingDoors.Add(d);
        }
        // A squeeze pressed again in the same contact goes on from where its leaf stopped (1.3), not as a push.
        else if (slow && d.motion == DoorMotion.None && d.bodyDriven && !IsLatched(d) && !AheadOfLeaf(d, input.feet)) d.motion = DoorMotion.Follow;
        if (d.motion == DoorMotion.Follow)
        {
            result.moveScale = .8f;
            FloorBodyDoor(d, input.feet + input.inputMove * .8f, DoorMaxStepDegrees);
            RememberBodyRest(d);
        }
        // A swing is planned, stepped and floored with the others (StepBodySwings). Here the Relay standing in the arc the
        // body needs holds the body (a door that will not move under your shoulder), as the floor's own test meant to.
        var relay = RelayBody?.Invoke();
        var need = d.bodyFloorActive ? d.bodyAngleFloor : d.motion == DoorMotion.Swing ? BodyClearAngle(d, input.feet + input.inputMove) : -1f;
        if (relay.HasValue && need > d.angle + .01f && LimitByBody(d, d.angle, need, relay.Value, RelayClearance) + .01f < need)
            return BodyBlocked(d, episode, BodyDoorBump.Held, ref result);
        return result;
    }

    // A plain door a barge would burst open now: unlocked, unsealed, whole, no armed bar, held by nothing, still, short of open.
    bool BargeReady(Door d) => d.motion == DoorMotion.None && d.angle < DoorPassableDegrees && !d.broken && !IsSealedExit(d)
        && !IsBeingBroken(d) && !IsHeldForKey(d) && !NeedsKey(d) && !IsAlarmArmed(d);

    BodyDoorResult BodyBlocked(Door d, BodyContact episode, BodyDoorBump bump, ref BodyDoorResult result, bool hard = true)
    {
        result.blocked = true;
        result.moveScale = 0f;
        if (!episode.bump || episode.lastBump != bump)
        {
            result.bumpKind = bump;
            if (bump == BodyDoorBump.Held) DoorBodyBlocked?.Invoke(d);
            episode.bump = true;
            episode.lastBump = bump;
        }
        return result;
    }

    void BeginBodyFollow(Door door, Vector3 from, bool handlePlayed, DoorSwingKind verb)
    {
        SwingAwayFrom(door, from);
        BeginVerb(door, verb);
        door.bodyDriven = true;
        door.open = false;
        openDoors.Remove(door.edge);
        ajarDoors.Add(door.edge);
        doorSides[door.edge] = door.swing;
        door.pull = false;
        door.wait = 0f;
        door.motion = DoorMotion.Follow;
        door.angle = Mathf.Max(3f, door.angle);
        if (!handlePlayed) DoorHandleTurned?.Invoke(door, LockPoint(door, from));
        if (door.alarm) TripAlarmDoor(door);
        DoorOpenBegan?.Invoke(door, DoorOpenSource.Body, false);
        RaiseDoorMoved(door);
        PoseDoor(door);
        TouchPassageRevision();
        movingDoors.Remove(door);
    }

    // The hard floor: the leaf at least clears the body where this frame's move leaves it. The solver poses (and syncs) the
    // leaf before that move, so it needs no look-ahead; the old one (0.12 m plus a physics step) popped a barge's leaf 30-80 deg
    // in its first frame. bodyAngleFloor keeps what the body needs, before the Relay's body limits it.
    void FloorBodyDoor(Door door, Vector3 next, float maxStep)
    {
        if (door.motion == DoorMotion.Through || maxStep <= 0f) return;
        var required = BodyClearAngle(door, next);
        if (required <= door.angle) return;
        var allowed = required;
        var relay = RelayBody?.Invoke();
        if (relay.HasValue) allowed = LimitByBody(door, door.angle, allowed, relay.Value, RelayClearance);
        door.bodyFloorActive = true;
        door.bodyAngleFloor = required;
        if (allowed <= door.angle) return;
        var wasPassable = door.angle >= DoorPassableDegrees;
        door.angle = Mathf.Min(allowed, door.angle + maxStep);
        PoseDoor(door);
        if (wasPassable != (door.angle >= DoorPassableDegrees)) TouchPassageRevision();
        bodySyncPending = true;
    }

    // The angle a leaf must reach to clear a body at this point (BodyDoorReach from its centre plane), or -1 when it already
    // does, cannot touch it from behind (out of reach or beside the hinge), or the body stands ahead of it (it stops on it).
    static float BodyClearAngle(Door door, Vector3 point)
    {
        var p = DoorPlan(door, point);
        var radius = p.magnitude;
        if (radius <= BodyDoorReach || p.x < .15f || p.x > LeafLength + .3f) return -1f;
        var bearing = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
        var clearance = Mathf.Asin(Mathf.Clamp01(BodyDoorReach / radius)) * Mathf.Rad2Deg;
        if (bearing + clearance <= door.angle || bearing - clearance > door.angle) return -1f;
        return Mathf.Clamp(bearing + clearance, 3f, ModuleUnits.DoorSwingDegrees);
    }

    // ---------- The body's swing: one planned ease-out ----------
    // A barge, a walk's push or a gait change opens the leaf on its verb's ease-out (95 deg in FastSwing's 0.30 s or Open's
    // 0.55 s), but never slower than the body's straight path from here needs: the plan picks the least starting speed whose
    // curve keeps the leaf past the body (BodyNeed, BodyPlanSlack beyond the reach) at every sample. Hit at the centre it is
    // about the verb's own; beside the hinge up to a full swing in BodySwingMinSeconds. The swing then runs on its own clock,
    // stepped here once a frame before the body moves. Each frame a body near it re-checks the plan; a body that turns into
    // the leaf raises the speed, nothing lowers it, so the leaf accelerates once and slows once (no pop, no stall, no second
    // push). A push back through the frame (Through) runs to the frame at the same starting speed, then opens on the far side.
    void StepBodySwings(BodyDoorInput input, float dt, ref BodyDoorResult result)
    {
        bodyStepScratch.Clear();
        foreach (var door in movingDoors)
            if (door.hinge != null && BodySwing(door)) bodyStepScratch.Add(door);
        if (bodyStepScratch.Count == 0) return;
        var move = input.inputMove * result.moveScale;
        var velocity = move / dt;
        var next = input.feet + move;
        foreach (var door in bodyStepScratch)
        {
            if (!BodySwing(door)) continue;
            var near = BodyLeafDistance(door, input.feet, out _) < BodyPlanNear;
            if (door.bodySeconds <= 0f || near) PlanBodySwing(door, input.feet, velocity, dt, false);
            var before = door.angle;
            bodyStepping = true;
            TickSwing(door, dt);
            bodyStepping = false;
            door.bodyStepTick = doorTicks;
            if (near && door.bodyDriven && door.motion == DoorMotion.Swing)
            {
                // The safety floor: a body that outran the plan this frame. The curve then leaves from where the leaf is.
                var stepped = door.angle;
                FloorBodyDoor(door, next, DoorMaxStepDegrees - Mathf.Abs(stepped - before));
                if (door.angle > stepped + 1e-4f) PlanBodySwing(door, next, velocity, dt, true);
            }
            if (near && Mathf.Abs(door.angle - before) > 1e-4f) bodySyncPending = true;
        }
    }

    // The player's own swing of a leaf (a barge, a push, a gait change; not the Relay's push of a leaf the body drove).
    static bool BodySwing(Door door) => door.bodyDriven && door.swingKind != DoorSwingKind.Relay
        && (door.motion == DoorMotion.Swing || door.motion == DoorMotion.Through);

    // TickDoors leaves a body's swing to the solver while it steps it (it did since the previous door tick), whichever Update runs first.
    bool BodySteppedSwing(Door door) => !bodyStepping && BodySwing(door) && door.bodyStepTick >= doorTicks - 1;

    // Plans a body swing that has none yet (from its verb's speed), or re-checks one (from its speed now) against the body's
    // straight path from feet at velocity, sampled from the end of this frame's step; restarts the curve only to go faster.
    void PlanBodySwing(Door door, Vector3 feet, Vector3 velocity, float dt, bool restart)
    {
        var planned = door.bodySeconds > 0f;
        if (door.motion == DoorMotion.Swing && door.angle >= ModuleUnits.DoorSwingDegrees - 1f) return;
        var least = planned ? BodySwingSpeed(door) : 2f * FrontRoomsShotTimings.Open.SwingDeg / SwingSecondsOf(door);
        var omega = Mathf.Max(least, 1f);
        var speed = velocity.magnitude;
        if (speed > .01f)
        {
            var until = dt + Mathf.Clamp(BodyPlanMetres / speed, .15f, .6f);
            if (!BodySwingClears(door, omega, feet, velocity, dt, until))
            {
                var top = Mathf.Max(omega, 2f * FrontRoomsShotTimings.Open.SwingDeg / BodySwingMinSeconds);
                if (!BodySwingClears(door, top, feet, velocity, dt, until)) omega = top;
                else
                {
                    var low = omega;
                    for (var i = 0; i < 10; i++)
                    {
                        var mid = (low + top) * .5f;
                        if (BodySwingClears(door, mid, feet, velocity, dt, until)) top = mid; else low = mid;
                    }
                    omega = top;
                }
            }
        }
        // The curve already runs ahead of the body (or already left at the fastest a plan may ask): leave it alone.
        if (planned && !restart && (omega <= least * 1.04f + 2f || door.bodySeconds <= BodySwingMinSeconds + 1e-4f)) return;
        door.bodySeconds = 2f * FrontRoomsShotTimings.Open.SwingDeg / omega;
        if (door.motion == DoorMotion.Through) { door.from = door.angle; door.clock = 0f; return; }
        StartSwing(door, ModuleUnits.DoorSwingDegrees, true);
        // The planned speed exactly, also for the last few degrees (StartSwing eases those over at least MinSwingSeconds).
        door.seconds = Mathf.Max(BodyPlanStep, 2f * (ModuleUnits.DoorSwingDegrees - door.from) / omega);
    }

    // True when the curve leaving the leaf's angle at omega deg/s keeps it at or past what the body needs from s0 to s1 seconds on.
    bool BodySwingClears(Door door, float omega, Vector3 feet, Vector3 velocity, float s0, float s1)
    {
        var through = door.motion == DoorMotion.Through;
        for (var s = s0; s <= s1 + 1e-4f; s += BodyPlanStep)
        {
            var angle = BodySwingAngle(door, omega, s);
            if (angle + .25f < BodyNeed(door, feet + velocity * s, through, angle)) return false;
        }
        return true;
    }

    // A fresh ease-out from the leaf's angle leaving at omega deg/s, s seconds on, in the frame of the side it opens to (StartSwing's
    // curve when bodySeconds = 190 / omega). A push back through the frame first runs to it at omega (TickSwing's Through).
    static float BodySwingAngle(Door door, float omega, float s)
    {
        var start = door.motion == DoorMotion.Through ? -door.angle : door.angle;
        if (start < 0f)
        {
            var toFrame = -start / omega;
            if (s <= toFrame) return start + omega * s;
            s -= toFrame;
            start = 0f;
        }
        var span = ModuleUnits.DoorSwingDegrees - start;
        if (span <= 0f) return start;
        var u = Mathf.Min(1f, s * omega / (2f * span));
        return start + span * u * (2f - u);
    }

    // The leaf's speed on its curve now (deg/s), which a re-plan never drops below.
    static float BodySwingSpeed(Door door)
    {
        if (door.motion == DoorMotion.Through) return ThroughSpeedOf(door);
        if (door.seconds <= 0f) return 0f;
        return Mathf.Abs(door.to - door.from) * 2f * (1f - Mathf.Clamp01(door.clock / door.seconds)) / door.seconds;
    }

    // What a body at this point needs of a leaf at leafAngle, in the frame of the side the leaf opens to (farSide: the far side
    // of a leaf pushed back through the frame): at or past the body's far tangent, BodyPlanSlack beyond the reach. Negative
    // infinity when the leaf cannot touch it from behind: out of reach, or the body ahead of the leaf (TickSwing stops it there).
    static float BodyNeed(Door door, Vector3 point, bool farSide, float leafAngle)
    {
        var p = DoorPlan(door, point);
        if (farSide) p.y = -p.y;
        var r = p.magnitude;
        // The slack shrinks to nothing for a body passing right by the hinge, where a full swing only just clears the reach.
        var reach = Mathf.Max(BodyDoorReach, Mathf.Min(BodyDoorReach + BodyPlanSlack, r - .005f));
        if (r >= LeafLength + reach) return float.NegativeInfinity;
        if (r <= reach) return BodyNeedCapDegrees;
        var bearing = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
        var half = r <= LeafLength ? Mathf.Asin(reach / r) * Mathf.Rad2Deg
            : Mathf.Acos(Mathf.Clamp((LeafLength * LeafLength + r * r - reach * reach) / (2f * LeafLength * r), -1f, 1f)) * Mathf.Rad2Deg;
        if (bearing - half > leafAngle) return float.NegativeInfinity;
        return Mathf.Min(bearing + half, BodyNeedCapDegrees);
    }

    void RestBodyDoor(Door door)
    {
        door.motion = DoorMotion.None;
        door.bodyFloorActive = false;
        door.swingLocked = false;
        RememberBodyRest(door);
    }

    void RememberBodyRest(Door door)
    {
        if (door.broken || IsLatched(door)) { bodyRestAngles.Remove(door.edge); return; }
        bodyRestAngles[door.edge] = door.angle;
        doorSides[door.edge] = door.swing;
        door.open = door.angle >= DoorPassableDegrees;
        if (door.open) { openDoors.Add(door.edge); ajarDoors.Remove(door.edge); }
        else { openDoors.Remove(door.edge); ajarDoors.Add(door.edge); }
    }

    static float BodyLeafDistance(Door door, Vector3 feet, out float along)
    {
        var p = DoorPlan(door, feet);
        var radians = door.angle * Mathf.Deg2Rad;
        var axis = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        along = Vector2.Dot(p, axis);
        return (p - axis * Mathf.Clamp(along, 0f, LeafLength)).magnitude;
    }

    static Vector3 BodyLeafNormal(Door door, Vector3 feet)
    {
        var p = DoorPlan(door, feet);
        var rad = door.angle * Mathf.Deg2Rad;
        var normal = new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad));
        var world = DoorRotation(door) * (Vector3.forward * normal.x + Vector3.right * (-door.swing * normal.y));
        return Vector2.Dot(p, normal) >= 0f ? world.normalized : -world.normalized;
    }

    static bool BodyIntendsCrossing(Door door, Vector3 from, Vector3 to)
    {
        var p = DoorPlan(door, from);
        var delta = DoorPlan(door, to) - p;
        if (delta.sqrMagnitude < 1e-8f || Mathf.Abs(delta.y) / delta.magnitude < .5f || p.y * delta.y >= 0f) return false;
        var cross = p.x - p.y * delta.x / delta.y;
        return cross >= .15f && cross <= LeafLength;
    }

    static bool BodyCrossesOpening(Door door, Vector3 from, Vector3 to)
    {
        var p = DoorPlan(door, from);
        var q = DoorPlan(door, to);
        if (p.y * q.y > 0f) return false;
        var dy = q.y - p.y;
        if (Mathf.Abs(dy) < 1e-5f) return false;
        var x = Mathf.Lerp(p.x, q.x, Mathf.Clamp01(-p.y / dy));
        return x >= .15f && x <= LeafLength;
    }

    void TrackBodyPasses(BodyDoorInput input)
    {
        if (bodyPreviousValid && (input.feet - bodyPreviousFeet).sqrMagnitude < 4f)
        {
            foreach (var door in doorByEdge.Values)
                if (!IsExitDoor(door) && BodyCrossesOpening(door, bodyPreviousFeet, input.feet))
                {
                    bodyPassedEdge = door.edge;
                    bodyPassedAt = bodyClock;
                    bodyPassedSprint = input.sprintingNow || input.gait == BodyDoorGait.Sprint || bodyPreviousSprint;
                }
        }
        bodyPreviousFeet = input.feet;
        bodyPreviousSprint = input.sprintingNow || input.gait == BodyDoorGait.Sprint;
        bodyPreviousValid = true;
    }

    bool TryBodyPull(BodyDoorInput input, ref BodyDoorResult result)
    {
        var door = input.aimDoor;
        if (door != null && doorByEdge.TryGetValue(door.edge, out var current)) door = current;
        if (door == null || door.hinge == null || IsLatched(door) || door.broken || IsBeingBroken(door)
            || IsHeldForKey(door) || IsExitDoor(door) || IsSealedExit(door) || door.motion != DoorMotion.None
            || AheadOfLeaf(door, input.feet))
        {
            bodyPullLook = bodyPullBack = 0f;
            bodyPullEdge = 0;
            return false;
        }
        var aim = DoorPlan(door, input.aimPoint);
        var along = Vector2.Dot(aim, new Vector2(Mathf.Cos(door.angle * Mathf.Deg2Rad), Mathf.Sin(door.angle * Mathf.Deg2Rad)));
        var eye = input.feet + Vector3.up * 1.62f;
        var handle = door.hinge.TransformPoint(new Vector3(0f, 1f, .92f));
        if (along < LeafLength * .5f || Vector3.Distance(eye, handle) > 1.4f)
        {
            bodyPullLook = bodyPullBack = 0f;
            bodyPullEdge = 0;
            return false;
        }
        if (bodyPullEdge != door.edge) { bodyPullLook = bodyPullBack = 0f; bodyPullEdge = door.edge; }
        bodyPullLook += Mathf.Min(input.dt, .25f);
        if (bodyPullLook < BodyDoorPullSeconds) return false;
        bodyPromptDoor = door;
        bodyPromptSlam = bodyPassedEdge == door.edge && bodyPassedSprint && bodyClock - bodyPassedAt <= BodyDoorSlamWindow;
        var speed = input.dt > 0f ? input.inputMove.magnitude / input.dt : 0f;
        var back = input.inputMove.sqrMagnitude > 1e-8f && Vector3.Dot(input.inputMove.normalized, -input.viewForward) >= Mathf.Cos(40f * Mathf.Deg2Rad);
        bodyPullBack = back && input.rawTilt >= .2f && speed >= 1f ? bodyPullBack + input.dt : 0f;
        if (bodyPullBack + 1e-5f < BodyDoorPullBackSeconds) return false;
        var kind = bodyPromptSlam && input.gait != BodyDoorGait.Slow && speed >= 3.2f - .01f ? DoorSwingKind.Slam : DoorSwingKind.Ease;
        BeginVerb(door, kind);
        door.open = false;
        door.pull = false;
        door.bodyDriven = false;
        door.bodyFloorActive = false;
        openDoors.Remove(door.edge);
        ajarDoors.Remove(door.edge);
        bodyRestAngles.Remove(door.edge);
        bodySlamReported.Remove(door.edge);
        door.wait = door.clock = 0f;
        door.from = door.angle;
        door.motion = DoorMotion.Shut;
        door.shutPhase = InverseSmoothstep(Mathf.Clamp01(door.angle / ModuleUnits.DoorSwingDegrees));
        if (!movingDoors.Contains(door)) movingDoors.Add(door);
        RaiseDoorMoved(door);
        TouchPassageRevision();
        result.door = door;
        result.verb = kind;
        result.shut = true;
        bodyPullLook = bodyPullBack = 0f;
        bodyPromptDoor = null;
        return true;
    }

    void BeginBodyUnlockCrack(Door door, Vector3 from, bool latchDrawn)
    {
        SwingAwayFrom(door, from);
        BeginVerb(door, DoorSwingKind.Unlock);
        door.open = false;
        openDoors.Remove(door.edge);
        ajarDoors.Add(door.edge);
        doorSides[door.edge] = door.swing;
        door.pull = false;
        door.bodyDriven = false;
        door.wait = 0f;
        DoorOpenBegan?.Invoke(door, DoorOpenSource.Unlock, latchDrawn);
        if (!latchDrawn) DoorHandleTurned?.Invoke(door, LockPoint(door, from));
        StartSwing(door, BodyDoorUnlockDegrees, false);
        if (!movingDoors.Contains(door)) movingDoors.Add(door);
        // Drawing the latch only cracks the leaf. The first actual body push
        // owns the motion/noise verb and, for an alarm bar, its deliberate trip.
        TouchPassageRevision();
    }

    // Called from TickSwing before body limiting. A player push never loses speed to its own easing curve.
    float BodyFloorTarget(Door door, float target)
    {
        return BodyDoors && door.bodyDriven && door.bodyFloorActive && door.motion == DoorMotion.Swing
            ? Mathf.Max(target, door.bodyAngleFloor) : target;
    }

    void BodySwingBlocked(Door door, float target, float allowed, float dt)
    {
        if (player == null) return;
        var byPlayer = Mathf.Abs(LimitByBody(door, door.angle, target, player.position, PlayerClearance) - target) > 1e-3f;
        if (!byPlayer) return;
        if (door.motion == DoorMotion.Throw && bodyThrowReported.Add(door.edge)) DoorThrowStoppedByPlayer?.Invoke(door);
        if (BodyDoors && door.motion == DoorMotion.Shut && door.swingKind == DoorSwingKind.Slam && bodySlamReported.Add(door.edge)) DoorSlammedInto?.Invoke(door);
        if (door.swingKind != DoorSwingKind.Relay || door.broken) return;
        bodyHeldSeconds.TryGetValue(door.edge, out var held);
        held += dt;
        bodyHeldSeconds[door.edge] = held;
        if (held >= .6f && bodyHeldReported.Add(door.edge)) DoorHeldAgainstRelay?.Invoke(door);
    }

    void BodySwingCompleted(Door door, bool latched)
    {
        if (latched)
        {
            bodyRestAngles.Remove(door.edge);
            bodyHeldSeconds.Remove(door.edge);
            bodyHeldReported.Remove(door.edge);
            bodySlamReported.Remove(door.edge);
        }
        else if (BodyDoors && !door.broken && door.angle < DoorPassableDegrees && door.angle >= 3f && door.swingKind != DoorSwingKind.Relay)
            RememberBodyRest(door);
        door.bodyDriven = door.bodyFloorActive = false;
        door.bodySeconds = 0f;
    }
}
