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
        if (TryBodyPull(input, ref result)) return result;

        var moving = input.inputMove.sqrMagnitude > 1e-8f;
        var speed = input.inputMove.magnitude / dt;
        var deliberate = moving && speed >= 1f && input.rawTilt >= .2f;
        var direction = moving ? input.inputMove.normalized : Vector3.zero;
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
            if (door.bodyDriven && (door.motion == DoorMotion.Swing || door.motion == DoorMotion.Through))
                FloorBodyDoor(door, input, false);
            if (!deliberate || door.angle >= DoorPassableDegrees && door.motion == DoorMotion.None) continue;
            var next = input.feet + input.inputMove;
            var nextDistance = BodyLeafDistance(door, next, out var along);
            if (nextDistance > BodyDoorReach || along < .15f || along > LeafLength) continue;
            var normal = BodyLeafNormal(door, input.feet);
            if (Vector3.Dot(direction, -normal) < .5f) continue;
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
        var slow = input.gait == BodyDoorGait.Slow;
        var sprint = input.sprintingNow || input.gait == BodyDoorGait.Sprint;
        var barge = sprint || input.sinceSprint >= 0f && input.sinceSprint <= BodyDoorBargeGrace
            && speed >= 3.2f - .01f && Vector3.Dot(direction, input.viewForward) >= Mathf.Cos(70f * Mathf.Deg2Rad);
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
            var lean = input.peekDoor != null && input.peekDoor.edge == d.edge ? .30f : facing < .5f ? .40f : slow ? .30f : .12f;
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
        else if (d.motion == DoorMotion.Follow && !slow)
        {
            // A single contact stays a single noise even when the player changes gait while pushing it.
            d.open = true;
            openDoors.Add(d.edge);
            ajarDoors.Remove(d.edge);
            StartSwing(d, ModuleUnits.DoorSwingDegrees, true);
            if (!movingDoors.Contains(d)) movingDoors.Add(d);
        }
        if (d.motion == DoorMotion.Follow)
        {
            result.moveScale = .8f;
            FloorBodyDoor(d, input, true);
            RememberBodyRest(d);
        }
        else FloorBodyDoor(d, input, false);
        var relay = RelayBody?.Invoke();
        if (relay.HasValue && d.bodyFloorActive && LimitByBody(d, d.angle, d.bodyAngleFloor, relay.Value, RelayClearance) + .01f < d.bodyAngleFloor)
            return BodyBlocked(d, episode, BodyDoorBump.Held, ref result);
        result.bodyMoved = d.bodyFloorActive;
        return result;
    }

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

    void FloorBodyDoor(Door door, BodyDoorInput input, bool follow)
    {
        if (door.motion == DoorMotion.Through) return;
        var move = input.inputMove;
        var dir = move.sqrMagnitude > 1e-8f ? move.normalized : Vector3.zero;
        var prediction = follow ? input.feet + move * .8f
            : input.feet + move + dir * .12f + (input.dt > 0f ? move / input.dt * Time.fixedDeltaTime : Vector3.zero);
        var p = DoorPlan(door, prediction);
        var radius = p.magnitude;
        if (radius <= BodyDoorReach || p.x < .15f || p.x > LeafLength + .3f) return;
        var bearing = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
        var clearance = Mathf.Asin(Mathf.Clamp01(BodyDoorReach / radius)) * Mathf.Rad2Deg;
        var required = Mathf.Clamp(bearing + clearance, 3f, ModuleUnits.DoorSwingDegrees);
        if (required <= door.angle) return;
        var allowed = required;
        var relay = RelayBody?.Invoke();
        if (relay.HasValue) allowed = LimitByBody(door, door.angle, allowed, relay.Value, RelayClearance);
        door.bodyFloorActive = true;
        door.bodyAngleFloor = allowed;
        var wasPassable = door.angle >= DoorPassableDegrees;
        door.angle = Mathf.Min(allowed, door.angle + DoorMaxStepDegrees);
        PoseDoor(door);
        if (wasPassable != (door.angle >= DoorPassableDegrees)) TouchPassageRevision();
        // Only body-driven transforms need a sync; the rest of the world never pays this per frame.
        Physics.SyncTransforms();
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
    }
}
