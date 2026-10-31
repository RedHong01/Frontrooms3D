#if FRONTROOMS_RELAY_V2
using System;
using System.Collections.Generic;
using System.Globalization;
using FrontRooms.Map;
using UnityEngine;

/// <summary>
/// Step V4b's half of the hunter contract (RELAY 4, 8), apart from IFrontRoomsRelayHunter so that a hunter without it (the
/// director tests' fakes) keeps V3a's stand-in: a chase's start is the lock-on. FrontRoomsMapHunter implements it.
/// </summary>
public interface IFrontRoomsRelayHunterV4b
{
    /// <summary>Stage 3 (RELAY 9.1): latched from TargetAcquired to the end of the chase.</summary>
    bool LockedOn { get; }
    /// <summary>The spotting meter, 0 to 1 (RELAY 8).</summary>
    float Spotting { get; }
    /// <summary>The meter filled (RELAY 8): the cue, Pressure +15, the hold.</summary>
    event Action<Vector3> TargetAcquired;
    /// <summary>RELAY 11: the one cautious look on the way out began; it counts toward lockOnCap.</summary>
    event Action WayOutLook;
}

/// <summary>
/// Relay v2 step V4b (RELAY_PURSUIT_REDESIGN sections 4, 5, 8, 9.1, 11), compiled only with FRONTROOMS_RELAY_V2: its own
/// sight, the lock-on, the chase and the search.
/// - Head: LookYaw, degrees off the body (Heading), clamped to 70. It canvasses in Listen and Search, sweeps 40 walking,
///   tracks the player from meter 0.5, locks forward in a chase. Under FADE and in Withdraw it stays forward (the forward cone).
/// - Sight: calm cone 60 H / 40 V round the head, 12 m in 3D (tuning.sightRange, 0 = blind); chase cone 110 round the
///   body, 15 m. No light term. SeesPlayer still means "ray and cone pass this tick". All of the Relay's own rays go
///   through RelaySightRayV2, where a door's vision panel is opaque (A42).
/// - Spotting meter: full in T(d) = (0.30 + 0.04 d) x spotMul (x 2 under FADE and in Withdraw); half rate for a player still
///   0.5 s past 6 m; drains 0.5/s after 0.3 s; never fills for a player in a sanctuary cell.
/// - Touch: within 1.5 m flat, the two cells the same or joined by an Open passage, a capsule clear: fills at 4/s, exempt
///   from the G3 gate, off at sightRange 0.
/// - G3 gate, per lock-on: the meter stays at 0.9 or less until stage 1 or more has run 3 s and stage 2 has run 1 s, both
///   unbroken. Held over 1.5 s it drops the stare and takes a cautious look at the glimpse point. Every hold is logged
///   (GateHolds, GateHoldLogged).
/// - "Almost" (meter 0.5): it stops (AlmostSeen), the head tracks; a meter that starts to drain from 0.5 or more ends in a
///   cautious look at the glimpse point.
/// - Lock-on: TargetAcquired, then a committed hold (0.7 s; 1.7 s on the first lock-on of a visit within 6 m, C37, Red
///   2026-10-30; 0.35 s when re-acquired this encounter; LockingOn; the state it began in), then StateChanged(Chase) and a
///   0.6 s ramp. A lock-on during BreakDoor abandons the break (the door keeps its blows).
/// - Chase: the lose meter (fills 1/s, drains 0.5/s, LoseTrack at 2.0 s; 1.0 s past chaseCap; 0.5 s past onMapCap + 15 s),
///   and F over 24 m for 1 s unseen with licensed doors at 0. LoseTrack goes only where perception says: a noise heard
///   since (the last known point), a door heard move, a door it saw the player take in its last 1.0 s of sight, else
///   the last-seen point.
/// - Search: the full search (hidden spots ahead of the last heading it saw, a shrinking radius, second best 30 %, one
///   backtrack, 1.2 s per spot, the tier's spots and seconds) and the cautious look (1.5 s, one hidden spot, 1.2 s).
/// - Catch: 0.7 m (tuning.catchDistance; C37 kept it), only in a chase, with sight or touch, never in a sanctuary.
/// </summary>
public sealed partial class FrontRoomsMapHunter : IFrontRoomsRelayHunterV4b
{
    // ---------- RELAY 8's numbers (SP) ----------

    /// <summary>The head, degrees off the body; the sweep while walking.</summary>
    public const float HeadLimitDegrees = 70f, WalkSweepDegrees = 40f;
    /// <summary>Calm sight: degrees round the head, across and up or down (12 m is tuning.sightRange).</summary>
    public const float CalmConeDegrees = 60f, CalmConeUpDegrees = 40f;
    /// <summary>Chase sight: degrees round the body, and its reach (fixed across tiers).</summary>
    public const float ChaseConeDegrees = 110f, ChaseSightMetres = 15f;
    /// <summary>T(d) = (SpotBaseSeconds + SpotSecondsPerMetre d) x spotMul; times SpotWithdrawScale under FADE and in Withdraw.</summary>
    public const float SpotBaseSeconds = .30f, SpotSecondsPerMetre = .04f, SpotWithdrawScale = 2f;
    /// <summary>Freeze: this share of the rate once the player has stood still this long, past this far.</summary>
    public const float FreezeScale = .5f, FreezeStillSeconds = .5f, FreezeMinMetres = 6f;
    /// <summary>The meter drains this fast after this long with no fill.</summary>
    public const float SpotDrainPerSecond = .5f, SpotDrainHoldSeconds = .3f;
    /// <summary>"Almost": it stops, the head tracks, shots cancel (Threat).</summary>
    public const float AlmostLevel = .5f;
    /// <summary>The G3 gate: the meter's ceiling until both runs are met; held longer than GateHoldSeconds it takes a cautious look.</summary>
    public const float GateLevel = .9f, GateStage1Seconds = 3f, GateStage2Seconds = 1f, GateHoldSeconds = 1.5f;
    /// <summary>Touch: flat metres, and the meter's fill per second.</summary>
    public const float TouchMetres = 1.5f, TouchFillPerSecond = 4f;
    /// <summary>The committed hold (fixed at every tier; the cue is 0.1 s shorter), the re-acquisition's, and the ramp to chase speed.</summary>
    public const float LockHoldSeconds = .7f, ReacquireHoldSeconds = .35f, ChaseRampSeconds = .6f;
    /// <summary>The lose meter drains this fast while it perceives the player, and ends the chase here (fixed at every tier).</summary>
    public const float LoseDrainPerSecond = .5f, LoseTrackSeconds = 2f;
    /// <summary>LoseTrack when F (licensed doors at 0) stays past this for this long, unseen.</summary>
    public const float ChaseExitMetres = 24f, ChaseExitSeconds = 1f;
    /// <summary>v2.0's chase cap (RELAY 8, 12).</summary>
    public const float ChaseSpeedCap = 4.4f;
    /// <summary>The cautious look's listen at the estimate, and every spot's listen.</summary>
    public const float CautiousListenSeconds = 1.5f, SpotListenSeconds = 1.2f;
    /// <summary>The cautious look checks this many spots; a full search takes the second best this often.</summary>
    public const int CautiousSpots = 1;
    public const float SecondBestShare = .3f;
    /// <summary>
    /// C37 (Red 2026-10-30, RELAY 8): the first lock-on of a visit with the Relay this close to the player (straight line, at
    /// TargetAcquired) holds CloseLockOnExtraSeconds longer: 1.7 s. Fixed at every tier; touch lock-ons count. Both SP: one line each.
    /// </summary>
    public const float CloseLockOnMetres = 6f;
    public const float CloseLockOnExtraSeconds = 1f;
    // The head's turn, the hold's square-up (SP); a canvass turn's hold, the walking sweep's period.
    const float HeadTurnDegPerSecond = 180f, SquareUpDegPerSecond = 360f, CanvassHoldSeconds = .9f, WalkSweepSeconds = 3.2f;
    // Canvass turns: at least this far from where the head is.
    const float CanvassMinTurnDegrees = 35f;
    // The player counts as still under this speed (m/s).
    const float StillSpeed = .05f;
    // Hidden spots: cells of open walking from the point (a full search's first pass, its last; a cautious look), the weight of
    // "ahead" in walking cells, and the sight tests per pass. LookPointMetres: how far out the rig's look target stands.
    const int SearchRadiusCells = 5, SearchMinRadiusCells = 2, CautiousRadiusCells = 3, SpotSightTests = 16;
    const float SpotAheadCells = 3f, LookPointMetres = 6f;

    // The meter; seconds since it last filled; a look already taken for this drain.
    float spotMeterV2, spotIdleV2;
    bool drainLookedV2;
    // "Almost": fired this climb; the stare dropped (it walks on, the head still tracking).
    bool almostFiredV2, stareDroppedV2;
    // The cap: seconds held at it, and the G3 hold under way (when it began).
    float capHeldV2, gateHoldAtV2;
    bool gateHoldingV2;
    // How long the player has stood still; their feet last tick; the last heading it saw.
    float stillForV2;
    Vector3 lastFeetV2, seenHeadingV2;
    bool hasFeetV2;
    // Touch this tick.
    bool touchV2;
    // The latch, the hold left, whether this encounter has acquired before (a re-acquisition).
    bool lockedOnV2, acquiredV2;
    float lockHoldV2;
    // The chase: seconds since the hold ended (the ramp), seconds past ChaseExitMetres unseen. lostTime is the lose meter.
    float chaseClockV2, farUnseenV2;
    // The door it last watched the player take (the far cell, when).
    GridCoord seenCrossCellV2;
    float seenCrossAtV2 = float.MinValue;
    // The head: the walking sweep's phase, the canvass's clock and target, its own random sequence (time-driven, so it never
    // moves the run's main sequence).
    float lookPhaseV2, canvassClockV2, canvassTargetV2;
    uint lookRngV2 = 2463534242u;
    // The search under way is a cautious look.
    bool searchCautiousV2;
    readonly List<float> chaseStartsV2 = new List<float>();
    readonly List<float> lockOnMetresV2 = new List<float>();
    readonly List<float> lockOnHoldsV2 = new List<float>();

    /// <summary>The head, degrees off the body (positive: to its right), within HeadLimitDegrees.</summary>
    public float LookYaw { get; private set; }
    /// <summary>Where the head looks, flat, unit length (the rig: head follows this, body follows Heading).</summary>
    public Vector3 LookDirection => (Quaternion.AngleAxis(LookYaw, Vector3.up) * Flat(Heading)).normalized;
    /// <summary>A world point along LookDirection at eye height, for the rig's head target.</summary>
    public Vector3 LookPoint => position + Vector3.up * EyeHeight + LookDirection * LookPointMetres;
    /// <summary>The spotting meter, 0 to 1.</summary>
    public float Spotting => spotMeterV2;
    /// <summary>Stage 3 (RELAY 9.1): latched from TargetAcquired to the end of the chase. WarnStage stays 0 to 2.</summary>
    public bool LockedOn => lockedOnV2;
    /// <summary>The committed hold: it stands and squares up, the state it began in reported, no catch.</summary>
    public bool LockingOn => lockHoldV2 > 0f;
    /// <summary>Seconds left of the committed hold (0 outside it).</summary>
    public float LockHoldLeft => Mathf.Max(0f, lockHoldV2);
    /// <summary>The chase as the player hears it: Chase, or a door break begun in one (never one begun in a hunt, never the hold).</summary>
    public bool InChase => State == HunterState.Chase || (State == HunterState.BreakDoor && resumeState == HunterState.Chase);
    /// <summary>The "almost" stop: it stands, the head tracking.</summary>
    public bool AlmostStopped => StopsForAlmostV2;
    /// <summary>The lose meter, seconds (0 outside a chase).</summary>
    public float LoseMeter => lostTime;
    /// <summary>Touch passed this tick.</summary>
    public bool TouchingPlayer => touchV2;
    /// <summary>Chase speed at this tier: the tier table's, capped at ChaseSpeedCap.</summary>
    public float ChaseSpeed => Mathf.Min(ChaseSpeedCap, TierV2 != null ? TierV2.chaseSpeed : tuning.chaseSpeed);
    /// <summary>The search under way is a cautious look (one spot); false: the full search.</summary>
    public bool SearchCautious => State == HunterState.Search && searchCautiousV2;
    /// <summary>The spots of the search under way, in order.</summary>
    public IReadOnlyList<GridCoord> SearchSpots => sweep;
    /// <summary>G3 holds this run, the seconds they lasted, and the last one's seconds.</summary>
    public int GateHolds { get; private set; }
    public float GateHoldSecondsTotal { get; private set; }
    public float LastGateHoldSeconds { get; private set; }
    /// <summary>Lock-ons this run (TargetAcquired, re-acquisitions included).</summary>
    public int TargetAcquisitions { get; private set; }
    /// <summary>The last TargetAcquired: straight metres to the player, the stage, the stage runs (G3), a touch, a re-acquisition.</summary>
    public float LastLockOnMetres { get; private set; }
    public int LastLockOnStage { get; private set; }
    public float LastLockOnStage1Seconds { get; private set; }
    public float LastLockOnStage2Seconds { get; private set; }
    public bool LastLockOnTouch { get; private set; }
    public bool LastLockOnReacquired { get; private set; }
    /// <summary>The last TargetAcquired's committed hold, seconds, and whether it was C37's long one.</summary>
    public float LastLockOnHoldSeconds { get; private set; }
    public bool LastLockOnLongHold { get; private set; }
    /// <summary>C37's long holds this run.</summary>
    public int LongLockHolds { get; private set; }
    /// <summary>Every lock-on this run, in order: straight metres to the player, and its hold's seconds (index for index).</summary>
    public IReadOnlyList<float> LockOnMetres => lockOnMetresV2;
    public IReadOnlyList<float> LockOnHolds => lockOnHoldsV2;
    /// <summary>Flat metres to the player as the last chase started (the hold's end), and every chase's this run (G5, C37).</summary>
    public float LastChaseStartMetres { get; private set; }
    public IReadOnlyList<float> ChaseStartMetres => chaseStartsV2;
    /// <summary>This run's lock-ons and chases in one line, for the RELAY log and the reports (G5; C37: how often the long hold fired).</summary>
    public string ChaseNumbers
    {
        get
        {
            var line = new System.Text.StringBuilder();
            line.Append(TargetAcquisitions).Append(" lock-ons (").Append(LongLockHolds).Append(" long holds):");
            for (var i = 0; i < lockOnMetresV2.Count; i++)
                line.Append(i == 0 ? " " : ", ").Append(lockOnMetresV2[i].ToString("0.0", CultureInfo.InvariantCulture)).Append(" m ")
                    .Append(lockOnHoldsV2[i].ToString("0.00", CultureInfo.InvariantCulture)).Append(" s");
            line.Append("; ").Append(chaseStartsV2.Count).Append(" chases from:");
            for (var i = 0; i < chaseStartsV2.Count; i++)
                line.Append(i == 0 ? " " : ", ").Append(chaseStartsV2[i].ToString("0.0", CultureInfo.InvariantCulture)).Append(" m");
            return line.ToString();
        }
    }
    /// <summary>Tools: the G3 gate counts as met (rows that are not about the gate). Never set in play.</summary>
    public bool GateOpenForTools { get; set; }
    /// <summary>Tools: the head stays where it is (DebugHoldLook; cone rows). Never set in play.</summary>
    public bool LookHeldForTools { get; set; }

    /// <summary>Tools: hold the head at <paramref name="yaw"/> degrees off the body.</summary>
    public void DebugHoldLook(float yaw)
    {
        LookYaw = Mathf.Clamp(yaw, -HeadLimitDegrees, HeadLimitDegrees);
        LookHeldForTools = true;
    }

    /// <summary>Tools: the meter full now (TargetAcquired and the hold, the gate skipped), for rows that start from a chase. On the map, not locked on; false otherwise.</summary>
    public bool DebugLockOn(Vector3 playerFeet)
    {
        if (!OnMap || lockedOnV2) return false;
        // It was never seen by these tools: the chase and any LoseTrack start from where they say the player is.
        lastSeen = playerFeet;
        lastSeenTime = lastContactTime = clock;
        BeginLockOnV2(playerFeet);
        return true;
    }

    /// <summary>
    /// RELAY 8: the meter filled. With the player's feet; LastLockOnReacquired says full cue (0.6 s) or short (0.25 s). Fired
    /// at the fill, before the hold; Pressure +15 (the director). The cue plays from it, never from the state.
    /// </summary>
    public event Action<Vector3> TargetAcquired;
    /// <summary>RELAY 8 (sound design hook): "almost": the scuff-and-turn, from its feet.</summary>
    public event Action<Vector3> AlmostSeen;
    /// <summary>The hold ended and the chase began, with the flat metres to the player (raised after StateChanged(Chase)).</summary>
    public event Action<float> ChaseStarted;
    /// <summary>RELAY 8: "every gate hold is logged": one line as each hold ends.</summary>
    public event Action<string> GateHoldLogged;
    /// <summary>RELAY 11: the one cautious look on the way out began; the director counts it toward lockOnCap.</summary>
    public event Action WayOutLook;

    /// <summary>T(d) (RELAY 8): seconds for the meter to fill from empty at <paramref name="metres"/>.</summary>
    public static float SpotSeconds(float metres, float spotMul) => (SpotBaseSeconds + SpotSecondsPerMetre * metres) * spotMul;

    /// <summary>The committed hold (RELAY 8, C37): a re-acquisition's, else the first lock-on's, long when it comes within CloseLockOnMetres.</summary>
    public static float LockOnHold(bool reacquired, float straightMetres) =>
        reacquired ? ReacquireHoldSeconds : straightMetres <= CloseLockOnMetres ? LockHoldSeconds + CloseLockOnExtraSeconds : LockHoldSeconds;

    float SpotMulV2 => TierV2 != null ? TierV2.spotMul : 1f;

    // The G3 gate's two runs (the hunter's own warning timers: they reset when the stage drops).
    bool GateOpenV2 => GateOpenForTools || (WarnRunSeconds + TimeSlack >= GateStage1Seconds && WarnStage2RunSeconds + TimeSlack >= GateStage2Seconds);

    bool StopsForAlmostV2 => spotMeterV2 >= AlmostLevel && !stareDroppedV2 && safeDoor == SafeDoorPhase.None
        && (State == HunterState.Listen || State == HunterState.Hunt || State == HunterState.Search || State == HunterState.Wander);

    // The hold and the stop hold its body and the state's own clocks.
    bool BodyHeldV2 => LockingOn || StopsForAlmostV2;

    // ---------- Sight ----------

    /// <summary>RELAY 8's sight test: range, the cone (calm round the head, chase round the body), then the ray from its eye.</summary>
    bool SeesV2(Vector3 playerEye)
    {
        if (tuning.sightRange <= 0f) return false;
        var eye = position + Vector3.up * EyeHeight;
        var chase = State == HunterState.Chase;
        var to = playerEye - eye;
        if (to.magnitude > (chase ? ChaseSightMetres : tuning.sightRange)) return false;
        var flat = Flat(to);
        var off = Vector3.Angle(chase ? Flat(Heading) : LookDirection, flat);
        if (flat.sqrMagnitude > 1e-6f && off > (chase ? ChaseConeDegrees : CalmConeDegrees)) return false;
        if (!chase && Mathf.Abs(Mathf.Atan2(to.y, flat.magnitude)) * Mathf.Rad2Deg > CalmConeUpDegrees) return false;
        return RelaySightRayV2(eye, playerEye);
    }

    /// <summary>
    /// The Relay's own sight ray (A42, Red 2026-10-30): every double-acting door gets a vision panel and the Relay cannot see
    /// through one. The one seam: a leaf counts as a whole slab here, whatever its collider becomes for the glass. An opening
    /// beside a leaf not in its frame stays open, as any gap. Hearing is unchanged: a panel is part of a shut door.
    /// </summary>
    bool RelaySightRayV2(Vector3 from, Vector3 to) => Visible(from, to) && !LeafBetweenV2(from, to);

    // A door leaf (not broken) lies across the segment: the doors on the edges of the cells it passes, half a cell at a time.
    bool LeafBetweenV2(Vector3 from, Vector3 to)
    {
        var length = Flat(to - from).magnitude;
        var samples = Mathf.CeilToInt(length / (MapGrid.CellSize * .5f));
        var last = new GridCoord(int.MinValue, int.MinValue);
        for (var i = 0; i <= samples; i++)
        {
            var cell = world.CellOf(Vector3.Lerp(from, to, samples > 0 ? i / (float)samples : 0f));
            if (cell == last) continue;
            last = cell;
            foreach (var s in Steps)
            {
                var door = world.DoorBetween(cell, cell + s);
                if (door != null && !door.broken && door.leaf != null && SegmentHitsBoxV2(door.leaf.transform, from, to)) return true;
            }
        }
        return false;
    }

    // The segment crosses a unit cube's box (the leaf's transform carries its size): slabs in its local space.
    static bool SegmentHitsBoxV2(Transform box, Vector3 from, Vector3 to)
    {
        var a = box.InverseTransformPoint(from);
        var b = box.InverseTransformPoint(to);
        float t0 = 0f, t1 = 1f;
        for (var axis = 0; axis < 3; axis++)
        {
            var d = b[axis] - a[axis];
            if (Mathf.Abs(d) < 1e-6f)
            {
                if (Mathf.Abs(a[axis]) > .5f) return false;
                continue;
            }
            var near = (-.5f - a[axis]) / d;
            var far = (.5f - a[axis]) / d;
            if (near > far)
            {
                var swap = near;
                near = far;
                far = swap;
            }
            t0 = Mathf.Max(t0, near);
            t1 = Mathf.Min(t1, far);
            if (t0 > t1) return false;
        }
        return true;
    }

    /// <summary>
    /// A42's seam for the player's side: whether the player seeing the Relay through a door's vision panel counts for
    /// ThreatPerceivable. Game visual's proposal to make; today it does not (a leaf's collider stops RelayInView), so the live
    /// peek shows no threat gate. Change it here and nowhere else.
    /// </summary>
    bool SeenThroughLiteV2(Camera screen) => false;

    /// <summary>Touch (RELAY 8): within 1.5 m flat, 360, the two cells joined, a capsule clear between the bodies; off at sightRange 0.</summary>
    bool TouchesV2(GridCoord myCell, GridCoord playerCell, Vector3 playerFeet)
    {
        if (tuning.sightRange <= 0f || Flat(playerFeet - position).sqrMagnitude > TouchMetres * TouchMetres) return false;
        return TouchJoinedV2(myCell, playerCell) && PathClear(position, playerFeet, true);
    }

    // The same cell, or an Open passage (open, arch, broken, pried: never shut, Ajar or a window). Cells that touch only at a
    // corner are joined when either way round the corner is Open (the capsule then says whether the corner itself is in the way).
    bool TouchJoinedV2(GridCoord a, GridCoord b)
    {
        if (a == b) return true;
        int dx = Mathf.Abs(a.x - b.x), dy = Mathf.Abs(a.y - b.y);
        if (dx + dy == 1) return OpenWayV2(a, b);
        if (dx != 1 || dy != 1) return false;
        var c = new GridCoord(a.x, b.y);
        var d = new GridCoord(b.x, a.y);
        return (OpenWayV2(a, c) && OpenWayV2(c, b)) || (OpenWayV2(a, d) && OpenWayV2(d, b));
    }

    bool OpenWayV2(GridCoord a, GridCoord b) => world.PassageBetween(a, b) == FrontRoomsMapWorld.Passage.Open;

    /// <summary>A tick that sees the player: the last heading it saw; in a chase or the hold, sight replaces what it heard.</summary>
    void SeenV2(Vector3 playerFeet)
    {
        var moved = Flat(playerFeet - lastSeen);
        if (moved.sqrMagnitude > 1e-4f && clock - lastSeenTime < 1f) seenHeadingV2 = moved.normalized;
        if (State != HunterState.Chase && !LockingOn) return;
        ListenPoint = null;
        SightClearsSenseV2();
    }

    void NotePlayerStillV2(float dt, Vector3 playerFeet)
    {
        var moved = hasFeetV2 ? Flat(playerFeet - lastFeetV2).magnitude : float.PositiveInfinity;
        stillForV2 = moved <= StillSpeed * dt ? stillForV2 + dt : 0f;
        lastFeetV2 = playerFeet;
        hasFeetV2 = true;
    }

    /// <summary>
    /// Each on-map tick, after the sight test: touch, the hold, the meter, the G3 gate, "almost" and the lock-on. Nothing of
    /// it runs in a chase (a door break begun in one included): the lose meter is TickChaseV2's.
    /// </summary>
    void TickSightV2(float dt, GridCoord myCell, GridCoord playerCell, Vector3 playerFeet, Vector3 playerEye, bool sanctuary)
    {
        NotePlayerStillV2(dt, playerFeet);
        touchV2 = !sanctuary && TouchesV2(myCell, playerCell, playerFeet);
        if (touchV2)
        {
            lastSeen = playerFeet;
            lastSeenTime = lastContactTime = clock;
        }
        if (LockingOn)
        {
            lockHoldV2 -= dt;
            if (lockHoldV2 <= TimeSlack) BeginChaseStateV2(playerFeet);
            return;
        }
        if (InChase) return;
        var rate = FillRateV2(playerEye);
        // The gate is per lock-on and touch is exempt; under FADE and in Withdraw lock-on is only within 6 m, past the hard end never.
        var gated = !touchV2 && !GateOpenV2;
        var capped = gated || !ChaseAllowedV2(playerFeet);
        var cap = capped ? GateLevel : 1f;
        if (rate > 0f)
        {
            spotMeterV2 = Mathf.Min(cap, spotMeterV2 + rate * dt);
            spotIdleV2 = 0f;
            drainLookedV2 = false;
        }
        else
        {
            spotIdleV2 += dt;
            if (spotIdleV2 + TimeSlack >= SpotDrainHoldSeconds)
            {
                // RELAY 8: a meter that drains from 0.5 or more without filling: a cautious look at the glimpse point.
                if (spotMeterV2 >= AlmostLevel && !drainLookedV2)
                {
                    drainLookedV2 = true;
                    BeginGlimpseLookV2();
                }
                spotMeterV2 = Mathf.Max(0f, spotMeterV2 - SpotDrainPerSecond * dt);
            }
        }
        if (capped && rate > 0f && spotMeterV2 >= cap - 1e-4f)
        {
            capHeldV2 += dt;
            if (gated && !gateHoldingV2)
            {
                gateHoldingV2 = true;
                gateHoldAtV2 = clock;
                GateHolds++;
            }
            if (capHeldV2 > GateHoldSeconds && !stareDroppedV2)
            {
                stareDroppedV2 = true;
                // RELAY 8: it walks closer, so the ladder catches up honestly. Held by FADE's 6 m it only walks on with its look.
                if (gated) BeginGlimpseLookV2();
            }
        }
        else
        {
            if (gateHoldingV2) EndGateHoldV2(rate > 0f ? "gate met" : "sight lost");
            capHeldV2 = 0f;
        }
        if (spotMeterV2 < AlmostLevel) almostFiredV2 = stareDroppedV2 = false;
        else if (!almostFiredV2 && rate > 0f)
        {
            almostFiredV2 = true;
            if (StopsForAlmostV2) AlmostSeen?.Invoke(position);
        }
        if (spotMeterV2 >= 1f - 1e-5f) BeginLockOnV2(playerFeet);
    }

    float FillRateV2(Vector3 playerEye)
    {
        var rate = 0f;
        if (SeesPlayer)
        {
            var metres = (playerEye - (position + Vector3.up * EyeHeight)).magnitude;
            var seconds = SpotSeconds(metres, SpotMulV2);
            if (State == HunterState.Withdraw || fadingV2) seconds *= SpotWithdrawScale;
            rate = 1f / Mathf.Max(.01f, seconds);
            if (stillForV2 >= FreezeStillSeconds && metres > FreezeMinMetres) rate *= FreezeScale;
        }
        return touchV2 ? Mathf.Max(rate, TouchFillPerSecond) : rate;
    }

    void EndGateHoldV2(string how)
    {
        var seconds = clock - gateHoldAtV2;
        gateHoldingV2 = false;
        LastGateHoldSeconds = seconds;
        GateHoldSecondsTotal += seconds;
        GateHoldLogged?.Invoke(seconds.ToString("0.00", CultureInfo.InvariantCulture) + " s (" + how + "), stage " + WarnStage + ", runs "
            + WarnRunSeconds.ToString("0.00", CultureInfo.InvariantCulture) + " / " + WarnStage2RunSeconds.ToString("0.00", CultureInfo.InvariantCulture) + " s, W "
            + WarningDistance.ToString("0.0", CultureInfo.InvariantCulture) + " m, " + State);
    }

    /// <summary>
    /// A cautious look at the glimpse point (RELAY 8: the meter drained from "almost", or the gate held it): the walk there
    /// and the Search state's look. Not while it breaks a door, in Withdraw (it walks on), at the stairwell's beat or held
    /// by a lock-on; under FADE it is the one cautious look. A full search under way stays full.
    /// </summary>
    void BeginGlimpseLookV2()
    {
        stareDroppedV2 = true;
        if (State == HunterState.BreakDoor || State == HunterState.Withdraw || LockingOn || safeDoor != SafeDoorPhase.None) return;
        if (fadingV2)
        {
            if (cautiousUsedV2) return;
            cautiousUsedV2 = true;
        }
        var fullUnderWay = sightSearchV2 && !cautiousLookV2 && (State == HunterState.Hunt || State == HunterState.Search);
        cautiousLookV2 = !fullUnderWay;
        // A search after a sighting: no handoff from it (RELAY 5).
        sightSearchV2 = true;
        lastContactTime = clock;
        HuntToward(world.CellOf(lastSeen), lastSeen);
    }

    // ---------- The lock-on ----------

    /// <summary>RELAY 8: the meter filled: TargetAcquired, the committed hold. A lock-on during BreakDoor abandons the break (RELAY 5): the door stays shut and keeps its blows.</summary>
    void BeginLockOnV2(Vector3 playerFeet)
    {
        if (gateHoldingV2) EndGateHoldV2("lock-on");
        LastLockOnReacquired = acquiredV2;
        // C37: the straight line, at TargetAcquired; "encounter" is one on-map visit (acquiredV2 clears with it, a handoff keeps it).
        LastLockOnMetres = Vector3.Distance(position, playerFeet);
        LastLockOnStage = WarnStage;
        LastLockOnStage1Seconds = WarnRunSeconds;
        LastLockOnStage2Seconds = WarnStage2RunSeconds;
        LastLockOnTouch = touchV2;
        LastLockOnHoldSeconds = lockHoldV2 = LockOnHold(acquiredV2, LastLockOnMetres);
        LastLockOnLongHold = !acquiredV2 && LastLockOnMetres <= CloseLockOnMetres;
        if (LastLockOnLongHold) LongLockHolds++;
        lockOnMetresV2.Add(LastLockOnMetres);
        lockOnHoldsV2.Add(LastLockOnHoldSeconds);
        acquiredV2 = lockedOnV2 = true;
        spotMeterV2 = 1f;
        capHeldV2 = 0f;
        if (State == HunterState.BreakDoor)
        {
            StopBreaking();
            breakSeconds = -1f;
            blowsStruck = 0;
            BlowIndex = -1;
            SetState(resumeState);
        }
        // A lock-on on the way out runs under FADE rules (RELAY 11).
        BeginChaseV2();
        safeDoor = SafeDoorPhase.None;
        EndDoorActV2();
        ResetSteering();
        path.Clear();
        pathIndex = 0;
        ListenPoint = null;
        SightClearsSenseV2();
        TargetAcquisitions++;
        TargetAcquired?.Invoke(playerFeet);
    }

    // The hold is over: StateChanged(Chase), and the ramp starts.
    void BeginChaseStateV2(Vector3 playerFeet)
    {
        lockHoldV2 = 0f;
        lostTime = farUnseenV2 = chaseClockV2 = 0f;
        replanTimer = 0f;
        safeDoor = SafeDoorPhase.None;
        LastChaseStartMetres = Flat(position - playerFeet).magnitude;
        chaseStartsV2.Add(LastChaseStartMetres);
        SetState(HunterState.Chase);
        ChaseStarted?.Invoke(LastChaseStartMetres);
    }

    // The hold's square-up: the body turns to where it last saw the player.
    Vector3 SquareUpV2(float dt)
    {
        var to = Flat(lastSeen - position);
        if (to.sqrMagnitude < 1e-4f) return Heading;
        return Flat(Vector3.RotateTowards(Flat(Heading), to.normalized, SquareUpDegPerSecond * Mathf.Deg2Rad * dt, 0f)).normalized;
    }

    /// <summary>The latch and its meters let go (LoseTrack, the stairwell's door, a handoff, an encounter's end). A meter from a lost chase starts empty.</summary>
    void EndLockV2()
    {
        lockedOnV2 = false;
        lockHoldV2 = 0f;
        spotMeterV2 = spotIdleV2 = capHeldV2 = 0f;
        drainLookedV2 = almostFiredV2 = stareDroppedV2 = false;
        lostTime = farUnseenV2 = 0f;
        chaseLoseCapV2 = -1f;
        if (gateHoldingV2) EndGateHoldV2("ended");
    }

    /// <summary>V4b's part of the encounter reset (Park, DebugPlace, Arm, Away): the meter, the gate's hold, the lock-on, the head, the door it saw taken.</summary>
    void ResetSightV2()
    {
        EndLockV2();
        acquiredV2 = false;
        seenCrossAtV2 = float.MinValue;
        touchV2 = false;
        searchCautiousV2 = false;
        seenHeadingV2 = Vector3.zero;
        chaseClockV2 = 0f;
        hasFeetV2 = false;
        stillForV2 = 0f;
        if (!LookHeldForTools) LookYaw = 0f;
        canvassClockV2 = 0f;
    }

    // ---------- The chase ----------

    /// <summary>RELAY 8: the run (a 0.6 s ramp to the tier's chase speed), then the lose meter and the 24 m exit. Follow can start a door break (no lose meter during it).</summary>
    void TickChaseV2(float dt, GridCoord myCell, GridCoord playerCell, Vector3 playerFeet)
    {
        chaseClockV2 += dt;
        var speed = ChaseSpeed * Mathf.Clamp01(chaseClockV2 / ChaseRampSeconds);
        replanTimer -= dt;
        if (replanTimer <= 0f)
        {
            replanTimer = ReplanSeconds;
            Plan(RouteCell(myCell), world.CellOf(lastSeen), lastSeen);
        }
        if ((SeesPlayer || touchV2) && myCell == playerCell) MoveDirect(playerFeet, speed, dt);
        else Follow(speed, dt);
        if (State != HunterState.Chase) return;
        var perceived = SeesPlayer || touchV2;
        lostTime = perceived ? Mathf.Max(0f, lostTime - LoseDrainPerSecond * dt) : lostTime + dt;
        // RELAY 4: past onMapCap + 15 s the first 0.5 s of broken sight ends it; a chase past chaseCap loses at 1.0 s.
        if (lostTime + TimeSlack >= LoseSecondsV2)
        {
            LoseTrack();
            return;
        }
        farUnseenV2 = !perceived && ChaseFieldMetresV2(myCell) > ChaseExitMetres ? farUnseenV2 + dt : 0f;
        if (farUnseenV2 + TimeSlack >= ChaseExitSeconds) LoseTrack();
    }

    // F for the 24 m exit: licensed shut doors on F's path count 0 ("it knows where you went"). Walked only past 24 m.
    float ChaseFieldMetresV2(GridCoord myCell)
    {
        var f = PathDistanceToPlayer;
        if (f <= ChaseExitMetres || float.IsPositiveInfinity(f) || PathOpeningCost <= 0f) return f;
        var cell = myCell;
        var here = f;
        var less = 0f;
        for (var i = 0; i < 32 && here > 0f; i++)
        {
            var next = cell;
            var nextMetres = here;
            foreach (var s in Steps)
            {
                var metres = playerField.DistanceTo(cell + s);
                if (metres >= nextMetres || world.PassageBetween(cell, cell + s) == FrontRoomsMapWorld.Passage.Wall) continue;
                next = cell + s;
                nextMetres = metres;
            }
            if (next == cell) break;
            if (world.PassageBetween(cell, next) == FrontRoomsMapWorld.Passage.ClosedDoor)
            {
                var door = world.DoorBetween(cell, next);
                if (door != null && (openLicenceV2.Contains(door.edge) || breakLicenceV2.Contains(door.edge))) less += FrontRoomsMapPathField.ShutOpening;
            }
            cell = next;
            here = nextMetres;
        }
        return f - less;
    }

    /// <summary>
    /// RELAY 7, 8: LoseTrack from what it perceived only (doorInto and playerCellBefore are gone on this path). A noise heard
    /// since it lost sight moved the last known point: that first. Then the far side of a door it heard move after losing
    /// sight (licensed, RELAY 5). Then the far side of a door it saw the player take in its last 1.0 s of sight. Else the
    /// last-seen point. Then the full search (a cautious look under FADE: BeginSearchV2).
    /// </summary>
    void LoseTrackV2()
    {
        EndLockV2();
        // V3b (RELAY 5): what follows is a search after a sighting (no handoff from it).
        sightSearchV2 = true;
        // V4a (RELAY 7): a lost chase ends in the full search, not a look.
        cautiousLookV2 = false;
        if (lastKnownHeardV2)
        {
            lastKnownHeardV2 = false;
            HuntToward(world.CellOf(lastSeen), lastSeen);
            return;
        }
        if (heardDoorV2 != null)
        {
            var far = FarSideV2(heardDoorV2);
            heardDoorV2 = null;
            HuntToward(far, world.CellCenter(far));
            return;
        }
        if (seenCrossAtV2 >= lastSeenTime - SeenDoorSeconds)
        {
            HuntToward(seenCrossCellV2, world.CellCenter(seenCrossCellV2));
            return;
        }
        HuntToward(world.CellOf(lastSeen), lastSeen);
    }

    // The door's cell farther from where it stands.
    GridCoord FarSideV2(FrontRoomsMapWorld.Door door) =>
        Flat(world.CellCenter(door.a) - position).sqrMagnitude >= Flat(world.CellCenter(door.b) - position).sqrMagnitude ? door.a : door.b;

    /// <summary>
    /// RELAY 5, ruled 2026-10-30: a door it sees move counts as heard. It is deaf in a chase with sight, so otherwise a player
    /// seen beyond a door could shut it and turn it into a WALL. A noise at a door's centre (DoorMoved) with the door in the
    /// chase cone and clear to its eye (the leaf itself may be what the ray meets) is a BREAK licence.
    /// </summary>
    void SawDoorMoveV2(Vector3 source)
    {
        var cell = world.CellOf(source);
        if (!world.IsBuilt(cell)) return;
        foreach (var s in Steps)
        {
            var door = world.DoorBetween(cell, cell + s);
            if (door == null || Flat(door.position - source).magnitude > HeardDoorMetres) continue;
            if (SeesDoorV2(door)) breakLicenceV2.Add(door.edge);
        }
    }

    bool SeesDoorV2(FrontRoomsMapWorld.Door door)
    {
        var eye = position + Vector3.up * EyeHeight;
        var point = door.position;
        point.y = eye.y;
        var to = point - eye;
        var distance = to.magnitude;
        if (distance > ChaseSightMetres) return false;
        if (distance < .01f) return true;
        if (Vector3.Angle(Flat(Heading), Flat(to)) > ChaseConeDegrees) return false;
        var count = Physics.RaycastNonAlloc(eye, to / distance, hits, distance, ~0, QueryTriggerInteraction.Ignore);
        var nearest = float.MaxValue;
        Collider blocker = null;
        for (var i = 0; i < count; i++)
        {
            var c = hits[i].collider;
            if (c == null || (rig != null && c.transform.IsChildOf(rig))) continue;
            if (hits[i].distance < nearest) { nearest = hits[i].distance; blocker = c; }
        }
        return blocker == null || blocker == door.leaf || blocker == playerCollider;
    }

    // ---------- The head ----------

    /// <summary>RELAY 8: LookYaw for the next tick's sight. Tracks in the hold and from meter 0.5 (not on the forward cone); forward in a chase, a break, FADE and Withdraw; the sweep walking; the canvass standing.</summary>
    void UpdateLookV2(float dt)
    {
        if (LookHeldForTools) return;
        var forwardOnly = State == HunterState.Withdraw || fadingV2;
        float target;
        if (LockingOn || (spotMeterV2 >= AlmostLevel && !forwardOnly && !InChase)) target = BearingV2(lastSeen);
        else if (InChase || forwardOnly || State == HunterState.BreakDoor) target = 0f;
        else if (Moving)
        {
            lookPhaseV2 += dt * 2f * Mathf.PI / WalkSweepSeconds;
            target = WalkSweepDegrees * Mathf.Sin(lookPhaseV2);
        }
        else target = CanvassV2(dt);
        target = Mathf.Clamp(target, -HeadLimitDegrees, HeadLimitDegrees);
        LookYaw = Mathf.Clamp(Mathf.MoveTowards(LookYaw, target, HeadTurnDegPerSecond * dt), -HeadLimitDegrees, HeadLimitDegrees);
    }

    float BearingV2(Vector3 point)
    {
        var to = Flat(point - position);
        return to.sqrMagnitude < 1e-4f ? LookYaw : Vector3.SignedAngle(Flat(Heading), to, Vector3.up);
    }

    // TLOU-style turns: it holds a look, then turns quickly to the next, at least CanvassMinTurnDegrees away.
    float CanvassV2(float dt)
    {
        canvassClockV2 -= dt;
        if (canvassClockV2 > 0f) return canvassTargetV2;
        canvassClockV2 = CanvassHoldSeconds;
        lookRngV2 ^= lookRngV2 << 13;
        lookRngV2 ^= lookRngV2 >> 17;
        lookRngV2 ^= lookRngV2 << 5;
        var next = (lookRngV2 % 141u) - 70f;
        if (Mathf.Abs(next - LookYaw) < CanvassMinTurnDegrees) next = next >= 0f ? next - 2f * CanvassMinTurnDegrees : next + 2f * CanvassMinTurnDegrees;
        canvassTargetV2 = next;
        return canvassTargetV2;
    }

    // ---------- The search ----------

    float SearchMaxSecondsV2 => TierV2 != null ? TierV2.searchSeconds : tuning.searchMaxSeconds;

    /// <summary>
    /// RELAY 8. A cautious look (a lone noise, a glimpse, a dispatch, anything under FADE): it listens 1.5 s where it stands,
    /// checks one spot hidden from there, listens 1.2 s, gives up. The full search (after a lost chase, or a repeat's
    /// upgrade): the tier's spots, 1.2 s each, up to the tier's seconds.
    /// </summary>
    void BeginSearchV2(GridCoord around)
    {
        searchCautiousV2 = fadingV2 || cautiousLookV2;
        sweep.Clear();
        sweepIndex = 0;
        sweepPlanned = -1;
        lookTimer = searchCautiousV2 ? CautiousListenSeconds : SpotListenSeconds;
        searchTime = 0f;
        PickSpotsV2(around, searchCautiousV2 ? CautiousSpots : Mathf.Max(1, TierV2 != null ? TierV2.searchSpots : SweepSpots), searchCautiousV2);
        ResetSteering();
        SetState(HunterState.Search);
    }

    /// <summary>
    /// The spots (RELAY 8): cells reached with no shut door, hidden from the point, ahead of the last heading it saw first.
    /// Full search: the radius shrinks each pass, the second best is taken 30 % of the time, and one pass (from the second on,
    /// with 3 or more spots) turns back: "ahead" counts the other way. With no hidden cell left, the farthest it may take.
    /// </summary>
    void PickSpotsV2(GridCoord around, int spots, bool cautious)
    {
        var point = world.CellCenter(around) + Vector3.up * EyeHeight;
        var radius = cautious ? CautiousRadiusCells : SearchRadiusCells;
        Search(around, radius, false);
        var backtrack = !cautious && spots >= 3 ? 1 + (int)(Next() % (uint)(spots - 1)) : -1;
        var hasHeading = seenHeadingV2.sqrMagnitude > .5f;
        for (var pass = 0; pass < spots; pass++)
        {
            var reach = cautious ? radius : Mathf.Max(SearchMinRadiusCells, radius - pass);
            v2Order.Clear();
            v2Key.Clear();
            v2Pick.Clear();
            foreach (var pair in depth)
            {
                var c = pair.Key;
                if (pair.Value <= 0 || pair.Value > reach || !EstimateCellOkV2(c) || NearSpotV2(c)) continue;
                var from = Flat(world.CellCenter(c) - point);
                var ahead = hasHeading && from.sqrMagnitude > 1e-4f ? Vector3.Dot(from.normalized, seenHeadingV2) : 0f;
                v2Pick.Add(v2Order.Count);
                v2Order.Add(c);
                v2Key.Add(pair.Value - SpotAheadCells * (pass == backtrack ? -ahead : ahead));
            }
            if (v2Pick.Count == 0) break;
            v2Pick.Sort((a, b) => v2Key[a].CompareTo(v2Key[b]));
            int best = -1, second = -1;
            for (var i = 0; i < v2Pick.Count && i < SpotSightTests && second < 0; i++)
            {
                var c = v2Order[v2Pick[i]];
                if (Visible(point, world.CellCenter(c) + Vector3.up * EyeHeight)) continue;
                if (best < 0) best = v2Pick[i];
                else second = v2Pick[i];
            }
            if (best < 0) best = v2Pick[v2Pick.Count - 1];
            var takeSecond = !cautious && second >= 0 && Next() % 100u < (uint)Mathf.RoundToInt(SecondBestShare * 100f);
            sweep.Add(v2Order[takeSecond ? second : best]);
        }
    }

    // A cell on or next to a spot already chosen.
    bool NearSpotV2(GridCoord c)
    {
        for (var i = 0; i < sweep.Count; i++)
            if (CellsApartV2(c, sweep[i]) <= 1) return true;
        return false;
    }
}
#endif
