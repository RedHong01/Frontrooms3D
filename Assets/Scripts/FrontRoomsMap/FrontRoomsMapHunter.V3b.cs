#if FRONTROOMS_RELAY_V2
using System;
using System.Collections.Generic;
using FrontRooms.Map;
using UnityEngine;

/// <summary>
/// Relay v2 step V3b (RELAY_PURSUIT_REDESIGN sections 5, 6, 10, 11), compiled only with FRONTROOMS_RELAY_V2:
/// - Door licences. At each dispatch (a placement, a re-pick, a handoff, a re-dispatch on foot) every door shut on the
///   route to the call's estimate that the Relay may open becomes an OPEN licence: at the door it frees the latch and
///   pushes the leaf Ajar (FrontRoomsMapWorld.OpenAjar, 1.0 s, RelayDoorOpened), then pushes through (PushAjar, 0.4 s,
///   RelayDoorPushed); the leaf settles back Ajar once it has gone through (SettleAjar). A licence is spent when used.
///   BREAK needs a perception licence: the shut doors on the way to a sound it heard, taken at hearing time, a door it
///   heard or saw move (in a chase too), and a door it saw the player go through (V4b's eyes); it breaks only in a hunt
///   or a chase. Every other shut door is a WALL: it plans round when that way is at most 1.5 times the way through,
///   else it stops at the door and listens 2 s, then gives up in a hunt (the give-up beat and Withdraw: "shut the door
///   and walk away ends an investigation"), waits out its lose meter in a chase, and drops the leg in a patrol, a
///   search or Withdraw. An Ajar door is pushed in any state. Withdraw may OPEN one exit door (exits 1-2, RELAY 11).
/// - Re-dispatch (IFrontRoomsRelayHunterV3b): an on-map device call is a walk to the device, or the handoff (while
///   investigating, the device more than 20 walking cells away, unseen, stage 0, W at least 30 m: straight from the map
///   to Arrive at a new placement, no Away hooks); in a chase or a break there is no re-dispatch (V3c: the director
///   queues the trip, heard and queued).
/// - The quiet-player sweep (Q2): a zone along the player's recent heading, 4 waypoints (W at least 21 m, the nearest
///   21-30 m, every leg at least 21 m when planned), patrolled for the tier's sweep seconds; then the give-up beat and
///   Withdraw. Ears and eyes as usual: a noise or a sighting makes it an ordinary encounter. V3c (systems design
///   2026-10-30, "(b) + guard"): waypoint 1 is picked where it can step in, so the first leg keeps W at least 21 m from
///   the placement (PlaceSweepV2); only when no waypoint passes does the placement keep soft rule b at the sweep's W.
///   Every leg (and its replans) is planned on cells at W 21 m or more (PlanSweepLegV2).
/// </summary>
public sealed partial class FrontRoomsMapHunter : IFrontRoomsRelayHunterV3b
{
    // ---------- RELAY 5, 6, 10's numbers (SP) ----------

    /// <summary>WALL: plan round a door with no licence when that way is at most this many times the way through it.</summary>
    public const float WallReplanRatio = 1.5f;
    /// <summary>WALL: with no way round it stops at the door and listens this long.</summary>
    public const float WallListenSeconds = 2f;
    /// <summary>The handoff: the device more than this many walking cells away, and W at least HandoffMinW.</summary>
    public const int HandoffCells = 20;
    public const float HandoffMinW = 30f;
    /// <summary>The sweep (Q2): 4 waypoints, every waypoint and leg at least SweepMinW, the nearest SweepMinW to SweepNearMaxW.</summary>
    public const int SweepWaypoints = 4;
    public const float SweepMinW = 21f, SweepNearMaxW = 30f;
    // The sweep's zone is looked for this far from the player (cells of walking); legs between waypoints run 3-12 open cells
    // and no waypoint stands within 2 cells (Chebyshev) of another; "along the heading" first means within 60 deg of it.
    const int SweepSearchCells = 24, SweepLegMinCells = 3, SweepLegMaxCells = 12, SweepSpacingCells = 2;
    const float SweepAheadDot = .5f;
    /// <summary>V3c: the sweep tries at most this many waypoint-1 candidates (none within 2 cells of one tried) for a placement it can walk from.</summary>
    public const int SweepFirstTries = 12;
    // A push held short (a body in the way, a rebuild) is pushed again this long after it began past its 0.4 s.
    const float PushRetrySeconds = .6f;
    // A pushed leaf settles back Ajar once the Relay is this far from the door (the leaf, its body and a margin), or after this long.
    const float SettleClearMetres = ModuleUnits.DoorWidth + Radius + .2f, SettleAfterSeconds = 8f;
    // A noise this close to a door's centre is that door's own (DoorMoved fires at the opening's centre).
    const float HeardDoorMetres = .2f;

    enum DoorActV2 : byte { None, Open, Push, Wall }

    // Licences by door edge: OPEN (shut on the route at dispatch) and BREAK (perception).
    readonly HashSet<long> openLicenceV2 = new HashSet<long>(), breakLicenceV2 = new HashSet<long>();
    // Withdraw's one OPEN (RELAY 11): the door, whether one is granted, whether it is spent.
    long withdrawOpenEdgeV2;
    bool withdrawOpenV2, withdrawOpenSpentV2;
    // The door act under way in Follow, at which door (edge), whether it reached the stance (WALL), its clock and facing.
    DoorActV2 doorActV2;
    long doorActEdgeV2;
    bool doorActReachedV2;
    float doorActClockV2;
    Vector3 doorFacingV2 = Vector3.forward;
    // The door it pushed, to settle back Ajar once it is through.
    FrontRoomsMapWorld.Door pushedDoorV2;
    float pushedClockV2;
    // The sweep: on, its waypoints, the one it walks to, the seconds left.
    bool sweepV2;
    readonly List<GridCoord> sweepWaysV2 = new List<GridCoord>();
    int sweepWayV2;
    float sweepLeftV2;
    // A search after a sighting (a lost chase's): no handoff from it (RELAY 5).
    bool sightSearchV2;
    // Scratch: the shortest plan kept while the licensed one is tried; the sweep's candidates.
    readonly List<GridCoord> planKeepV2 = new List<GridCoord>(), sweepPoolV2 = new List<GridCoord>(), sweepOtherV2 = new List<GridCoord>();
    // V3c (Fix 3): waypoint 1's candidates (W 21-30 m along the heading), their order and sort keys, the ones tried; the
    // region round the one tried whose every cell keeps W at least 21 m on the way to it (PlaceV2 keeps to it while
    // sweepSafeOnlyV2); PlaceV2 keeps soft rule b at every stage while sweepKeepRouteV2 (the guard); the heading the sweep
    // was sent along (a step-in re-pick reuses it).
    readonly List<GridCoord> sweepFirstV2 = new List<GridCoord>(), sweepTriedV2 = new List<GridCoord>();
    readonly List<float> sweepFirstKeyV2 = new List<float>();
    readonly List<int> sweepOrderV2 = new List<int>();
    readonly HashSet<GridCoord> sweepSafeV2 = new HashSet<GridCoord>();
    bool sweepSafeOnlyV2, sweepKeepRouteV2;
    Vector3 sweepHeadingV2 = Vector3.forward;

    /// <summary>V3b counts (run-long, like DoorsBroken): doors OPENed, Ajar doors pushed through, WALLs met (one per door per act), handoffs.</summary>
    public int DoorsOpened { get; private set; }
    public int DoorsPushed { get; private set; }
    public int DoorsWalled { get; private set; }
    public int Handoffs { get; private set; }
    /// <summary>Tools: the licences held now (OPEN, BREAK) and whether Withdraw's one OPEN is granted or spent.</summary>
    public string DoorLicences => "open " + openLicenceV2.Count + ", break " + breakLicenceV2.Count + (withdrawOpenV2 ? withdrawOpenSpentV2 ? ", withdraw OPEN spent" : ", withdraw OPEN" : "");
    /// <summary>True while the given door holds an OPEN or a BREAK licence.</summary>
    public bool HasOpenLicence(FrontRoomsMapWorld.Door door) => door != null && openLicenceV2.Contains(door.edge);
    public bool HasBreakLicence(FrontRoomsMapWorld.Door door) => door != null && breakLicenceV2.Contains(door.edge);
    /// <summary>The sweep is under way (from its placement to its give-up beat, or until a noise or a sighting makes it an encounter).</summary>
    public bool Sweeping => sweepV2;
    /// <summary>The sweep's waypoints as picked (empty before one).</summary>
    public IReadOnlyList<GridCoord> SweepWaypointsV2 => sweepWaysV2;
    /// <summary>The last Sweep's first waypoint (the call's estimate as placed).</summary>
    public Vector3 SweepTarget { get; private set; }
    /// <summary>V3c, tools: the last sweep placement fell back to soft rule b at the sweep's W (no waypoint 1 it could step in for with every cell of the way at W 21 m or more).</summary>
    public bool LastSweepRuleB { get; private set; }
    /// <summary>V3c, tools: waypoint-1 candidates the last sweep placement tried (at most SweepFirstTries).</summary>
    public int LastSweepFirstTries { get; private set; }
    /// <summary>The WALL listen under way (stopped at a door it may not open or break).</summary>
    public bool AtWall => doorActV2 == DoorActV2.Wall;

    /// <summary>RELAY 5 (sound design hook): it OPENed a door (the latch and the 1.0 s push to Ajar), at the door's centre.</summary>
    public event Action<Vector3> RelayDoorOpened;
    /// <summary>RELAY 10 (sound design hook): it pushes through an Ajar door (0.4 s), at the door's centre.</summary>
    public event Action<Vector3> RelayDoorPushed;
    /// <summary>RELAY 5: it met a WALL (a shut door it may not open or break, no way round within 1.5 times), at the door's centre.</summary>
    public event Action<Vector3> DoorWalled;

    // ---------- IFrontRoomsRelayHunterV3b ----------

    /// <summary>
    /// RELAY 5, 6: what an on-map device call can do now. Chase or BreakDoor: NoiseOnly, no re-dispatch (V3c: the director
    /// queues it, heard and queued). Listen, Hunt, Search or Wander
    /// (patrol, sweep), not under FADE or the stairwell beat: the handoff while investigating (not a patrol, a sweep, the
    /// give-up beat or a search after a sighting) with the device more than 20 walking cells away, the Relay unseen, stage
    /// 0 and W at least 30 m; else the walk. Anything else (off the map, Withdraw, FADE): queued.
    /// </summary>
    public FrontRoomsRelayRedispatchKind RedispatchKind(Vector3 point, Vector3 playerFeet, Vector3 playerEye)
    {
        if (caught || world == null || !OnMap) return FrontRoomsRelayRedispatchKind.Queue;
        // V4b: the lock-on's hold is the chase's start: no re-dispatch.
        if (State == HunterState.Chase || State == HunterState.BreakDoor || LockingOn) return FrontRoomsRelayRedispatchKind.NoiseOnly;
        if (State == HunterState.Withdraw || fadingV2 || safeDoor != SafeDoorPhase.None) return FrontRoomsRelayRedispatchKind.Queue;
        if (State != HunterState.Listen && State != HunterState.Hunt && State != HunterState.Search && State != HunterState.Wander) return FrontRoomsRelayRedispatchKind.Queue;
        var investigating = !sweepV2 && !sightSearchV2
            && (State == HunterState.Hunt || State == HunterState.Search || (State == HunterState.Listen && listenKindV2 != ListenKindV2.GiveUp));
        if (investigating && !PlayerSeesRelay && WarnStage == 0 && WarningDistance >= HandoffMinW && WalkingCellsOverV2(point, HandoffCells))
            return FrontRoomsRelayRedispatchKind.Handoff;
        return FrontRoomsRelayRedispatchKind.Walk;
    }

    /// <summary>
    /// RELAY 5's handoff: straight from the map to Arrive at a new placement for the device (Arrive v2's rules), with no
    /// Away hooks (no WentAway, no Withdrew); the encounter goes on (OnFirstSend kept). False, nothing changed, when it may
    /// not hand off now or no placement fits (the director then sends it on foot).
    /// </summary>
    public bool Handoff(FrontRoomsRelayCall call, Vector3 playerFeet, Vector3 playerEye, Vector3 playerForward)
    {
        if (caught || world == null || call == null || !OnMap) return false;
        if (RedispatchKind(call.Estimate, playerFeet, playerEye) != FrontRoomsRelayRedispatchKind.Handoff) return false;
        sightFeet = playerFeet;
        sightEye = playerEye;
        sightForward = playerForward;
        if (!PlaceV2(call, false, playerFeet, playerEye, playerForward)) return false;
        // What it did on the map stops here; it is backstage again (the rig hides with Released).
        var keepFirst = OnFirstSend;
        var keepLinks = HeardLinks;
        var keepRetargets = HeardRetargets;
        StopBreaking();
        sweep.Clear();
        sweepIndex = 0;
        sweepPlanned = -1;
        lookTimer = searchTime = 0f;
        SeesPlayer = false;
        sawLastTick = false;
        lostTime = replanTimer = 0f;
        Moving = false;
        DoorSqueeze = 0f;
        safeDoor = SafeDoorPhase.None;
        EndDoorActV2();
        SettlePushedV2();
        BeginArriveV2(call, false);
        OnFirstSend = keepFirst;
        // The encounter goes on: V4a's counts with it.
        HeardLinks = keepLinks;
        HeardRetargets = keepRetargets;
        Handoffs++;
        LastArriveReport = "handoff - " + LastArriveReport;
        return true;
    }

    /// <summary>
    /// RELAY 6: a re-dispatch on foot: it hunts from where it stands to the call's estimate (snapped as Arrive's), with
    /// the shut doors on that way as OPEN licences. False, nothing changed, off the map, in a chase, a break or Withdraw.
    /// </summary>
    public bool Redirect(FrontRoomsRelayCall call)
    {
        if (caught || world == null || call == null || !OnMap) return false;
        if (State == HunterState.Chase || State == HunterState.BreakDoor || State == HunterState.Withdraw || LockingOn) return false;
        callV2 = call;
        var raw = world.CellOf(call.Estimate);
        var cell = SnapEstimateV2(raw);
        SetEstimateV2(cell, cell == raw ? call.Estimate : world.CellCenter(cell), call.ErrorCells);
        sweepV2 = false;
        sightSearchV2 = false;
        // RELAY 5's loop (V4b): as any dispatch, a cautious look unless a repeat upgrades it. The link stands: its margin still weighs what it hears next.
        cautiousLookV2 = true;
        safeDoor = SafeDoorPhase.None;
        EndDoorActV2();
        LicenseDispatchV2();
        ListenPoint = estimateV2;
        lastContactTime = clock;
        HuntToward(estimateCellV2, estimateV2);
        return true;
    }

    /// <summary>
    /// The quiet-player sweep (Q2) while Away: the zone along <paramref name="playerHeading"/>, waypoint 1 picked where it
    /// can step in and the placement for it (PlaceSweepV2: Arrive v2's hard rules; <paramref name="relaxed"/>: the PENDING
    /// fallback's W at least 36 m), then the other waypoints; after the step-in's listen it walks them at patrol pace for
    /// the tier's sweep seconds.
    /// </summary>
    public bool Sweep(FrontRoomsRelayCall call, bool relaxed, Vector3 playerFeet, Vector3 playerEye, Vector3 playerForward, Vector3 playerHeading)
    {
        if (caught || world == null || call == null || State != HunterState.Away) return false;
        sightFeet = playerFeet;
        sightEye = playerEye;
        sightForward = playerForward;
        sweepHeadingV2 = playerHeading;
        if (!PlaceSweepV2(call, relaxed, playerFeet, playerEye, playerForward, playerHeading, out var placed)) return false;
        BeginArriveV2(placed, false);
        // V3c: the doors it may open on its way to waypoint 1 that keeps W at least 21 m are this dispatch's licences too.
        LicenseSweepRouteV2();
        sweepV2 = true;
        sweepWayV2 = 0;
        sweepLeftV2 = Mathf.Max(0f, TierV2 != null ? TierV2.sweepSeconds : 40f);
        SweepTarget = placed.Estimate;
        LastArriveReport = SweepReportV2(playerFeet) + LastArriveReport;
        return true;
    }

    // "sweep of N waypoints from wp1 (W), seconds - how waypoint 1 was picked - ", the start of a sweep's LastArriveReport.
    string SweepReportV2(Vector3 playerFeet) =>
        "sweep of " + sweepWaysV2.Count + " waypoints from " + sweepWaysV2[0] + " (W " + WAtV2(sweepWaysV2[0], world.CellCenter(sweepWaysV2[0]), playerFeet).ToString("0.0") + " m), "
        + sweepLeftV2.ToString("0") + " s - " + (LastSweepRuleB ? "rule b kept at the sweep's W (no waypoint 1 to step in for in " + LastSweepFirstTries + " tries)"
            : "waypoint 1 where it steps in (" + LastSweepFirstTries + (LastSweepFirstTries == 1 ? " try" : " tries") + ")") + " - ";

    /// <summary>
    /// V3c (Fix 3): a sweep's step-in re-pick (the step-in re-check failed): the sweep's placement again from where the
    /// player is now, along the heading it was sent on (waypoint 1 where it can step in, then the others). False: none.
    /// </summary>
    bool RepickSweepV2(bool relaxed, Vector3 playerFeet, Vector3 playerEye, Vector3 playerForward)
    {
        if (callV2 == null) return false;
        if (!PlaceSweepV2(callV2, relaxed, playerFeet, playerEye, playerForward, sweepHeadingV2, out var placed)) return false;
        callV2 = placed;
        SweepTarget = placed.Estimate;
        LastArriveReport = "re-pick: " + SweepReportV2(playerFeet) + LastArriveReport;
        return true;
    }

    // ---------- Licences ----------

    /// <summary>RELAY 5: the OPEN licences of a dispatch: every door shut on the way from where it stands to the estimate that it may open (a way round locked doors first).</summary>
    void LicenseDispatchV2()
    {
        openLicenceV2.Clear();
        breakLicenceV2.Clear();
        if (!hasEstimateV2 || world == null) return;
        // From where its plan will start (a placement has no leg; mid-detour that is the leg's start).
        var from = RouteCell(world.CellOf(position));
        var to = estimateCellV2;
        if (from == to) return;
        SearchV2(from, 64, 2);
        if (!depth.ContainsKey(to))
        {
            Search(from, 64, true);
            if (!depth.ContainsKey(to)) return;
        }
        for (var c = to; c != from; )
        {
            var back = cameFrom[c];
            if (world.PassageBetween(back, c) == FrontRoomsMapWorld.Passage.ClosedDoor)
            {
                var door = world.DoorBetween(back, c);
                if (door != null && world.RelayMayOpen(door)) openLicenceV2.Add(door.edge);
            }
            c = back;
        }
    }

    /// <summary>RELAY 5: BREAK licences for the shut doors on the way to a sound it heard, taken now (at hearing time).</summary>
    void LicenseRouteV2(GridCoord from, GridCoord to)
    {
        if (from == to) return;
        Search(from, 64, true);
        if (!depth.ContainsKey(to)) return;
        for (var c = to; c != from; )
        {
            var back = cameFrom[c];
            if (world.PassageBetween(back, c) == FrontRoomsMapWorld.Passage.ClosedDoor) LicenseDoorV2(back, c);
            c = back;
        }
    }

    /// <summary>RELAY 5: a sound made at a door's centre is that door moving: it may BREAK it (a hunt or a chase).</summary>
    void LicenseHeardDoorV2(Vector3 source, GridCoord sourceCell)
    {
        if (!world.IsBuilt(sourceCell)) return;
        foreach (var s in Steps)
        {
            var door = world.DoorBetween(sourceCell, sourceCell + s);
            if (door == null || Flat(door.position - source).magnitude > HeardDoorMetres) continue;
            breakLicenceV2.Add(door.edge);
            // V4a: V4b's LoseTrack follows a door it heard move after losing sight (RELAY 7).
            NoteHeardDoorV2(door);
        }
    }

    /// <summary>The door between two cells may be broken (a perception licence).</summary>
    void LicenseDoorV2(GridCoord a, GridCoord b)
    {
        var door = world.DoorBetween(a, b);
        if (door != null) breakLicenceV2.Add(door.edge);
    }

    /// <summary>
    /// Search (BFS, as Search) where a shut door passes by <paramref name="doorMode"/>: 0 none (a patrol's, a search's,
    /// Withdraw's plan), 1 licensed ones (a hunt's or a chase's), 2 any the Relay may open (a dispatch's route); Withdraw's
    /// one OPEN passes in every mode until spent. Ajar doors always pass (it pushes them).
    /// </summary>
    void SearchV2(GridCoord start, int maxDepth, int doorMode) => SearchV2(start, maxDepth, doorMode, 0f, default(Vector3));

    /// <summary>
    /// SearchV2; with <paramref name="minW"/> above 0 (V3c: a sweep's way, RELAY 6) every cell but the start also keeps W
    /// at least <paramref name="minW"/> from a player at <paramref name="feet"/> (the player field must be built at feet's cell).
    /// </summary>
    void SearchV2(GridCoord start, int maxDepth, int doorMode, float minW, Vector3 feet)
    {
        cameFrom.Clear();
        depth.Clear();
        frontier.Clear();
        depth[start] = 0;
        frontier.Enqueue(start);
        while (frontier.Count > 0)
        {
            var cell = frontier.Dequeue();
            var d = depth[cell];
            if (d >= maxDepth) continue;
            foreach (var s in Steps)
            {
                var next = cell + s;
                if (depth.ContainsKey(next) || !world.IsBuilt(next)) continue;
                var passage = world.PassageBetween(cell, next);
                if (passage == FrontRoomsMapWorld.Passage.Wall || passage == FrontRoomsMapWorld.Passage.Glass) continue;
                if (passage == FrontRoomsMapWorld.Passage.ClosedDoor && !DoorPassesV2(cell, next, doorMode)) continue;
                if (minW > 0f && WAtV2(next, world.CellCenter(next), feet) < minW) continue;
                depth[next] = d + 1;
                cameFrom[next] = cell;
                frontier.Enqueue(next);
            }
        }
    }

    bool DoorPassesV2(GridCoord a, GridCoord b, int doorMode)
    {
        var door = world.DoorBetween(a, b);
        if (door == null) return false;
        if (withdrawOpenV2 && !withdrawOpenSpentV2 && door.edge == withdrawOpenEdgeV2) return true;
        if (doorMode == 1) return openLicenceV2.Contains(door.edge) || breakLicenceV2.Contains(door.edge);
        if (doorMode == 2) return world.RelayMayOpen(door);
        return false;
    }

    /// <summary>
    /// Plan's route under the define (RELAY 5). Through doors (a hunt, a chase): the shortest way through every door; if
    /// it crosses a shut door with no licence, the licensed way instead when it is at most WallReplanRatio times as long,
    /// else the shortest stays and Follow meets that door as a WALL. Keeping to shut doors (a patrol, a search, Withdraw):
    /// no shut door but Withdraw's one OPEN. False when nothing reaches the cell (the path left empty).
    /// </summary>
    bool PlanRouteV2(GridCoord from, GridCoord to, bool throughDoors)
    {
        var planned = PlanPathV2(from, to, throughDoors);
        // A door act (an OPEN, a push, a WALL's listen) lasts only while the new way still goes through that door.
        if (doorActV2 != DoorActV2.None && !PathCrossesV2(from, doorActEdgeV2)) EndDoorActV2();
        return planned;
    }

    bool PlanPathV2(GridCoord from, GridCoord to, bool throughDoors)
    {
        if (!throughDoors)
        {
            SearchV2(from, 64, 0);
            return BuildPathV2(from, to);
        }
        Search(from, 64, true);
        if (!BuildPathV2(from, to)) return false;
        if (!PathHasUnlicensedDoorV2(from)) return true;
        var through = path.Count;
        planKeepV2.Clear();
        planKeepV2.AddRange(path);
        SearchV2(from, 64, 1);
        if (depth.TryGetValue(to, out var licensed) && licensed <= WallReplanRatio * through && BuildPathV2(from, to)) return true;
        path.Clear();
        path.AddRange(planKeepV2);
        return true;
    }

    // The planned path crosses the door on this edge (from the path's start).
    bool PathCrossesV2(GridCoord from, long edge)
    {
        var prev = from;
        for (var i = 0; i < path.Count; i++)
        {
            var door = world.DoorBetween(prev, path[i]);
            if (door != null && door.edge == edge) return true;
            prev = path[i];
        }
        return false;
    }

    bool BuildPathV2(GridCoord from, GridCoord to)
    {
        path.Clear();
        pathIndex = 0;
        if (!depth.ContainsKey(to)) return false;
        for (var c = to; c != from; c = cameFrom[c]) path.Add(c);
        path.Reverse();
        return true;
    }

    // A shut door on the planned path that holds no licence (from the path's start).
    bool PathHasUnlicensedDoorV2(GridCoord from)
    {
        var prev = from;
        for (var i = 0; i < path.Count; i++)
        {
            var c = path[i];
            if (world.PassageBetween(prev, c) == FrontRoomsMapWorld.Passage.ClosedDoor && !DoorPassesV2(prev, c, 1)) return true;
            prev = c;
        }
        return false;
    }

    // ---------- Follow at a door ----------

    /// <summary>
    /// Follow at a shut door (RELAY 5): BREAK with a perception licence in a hunt or a chase; OPEN with a dispatch licence
    /// or Withdraw's one; else a WALL. False while it walks up, acts or listens; true when a WALL drops the leg.
    /// </summary>
    bool FollowShutV2(GridCoord here, GridCoord next, float speed, float dt)
    {
        var door = world.DoorBetween(here, next);
        // The chunk that owns this door was dropped under the path: plan again over what is built.
        if (door == null) return !Replan(here);
        if (door.broken || door.open)
        {
            // As v1: broken or opened by the player, its leaf still swinging or resting on a body short of passable:
            // wait at the stance, out of the leaf's sweep, and walk through once it swings clear.
            MoveDirect(BreakStance(door, here, next, out _), speed, dt);
            return false;
        }
        var edge = door.edge;
        if (doorActV2 != DoorActV2.None && doorActEdgeV2 != edge) EndDoorActV2();
        // BREAK: a perception licence, in a hunt or a chase (nothing else breaks doors).
        var mayBreak = planThroughDoors && breakLicenceV2.Contains(edge) && (State == HunterState.Hunt || State == HunterState.Chase);
        // OPEN: licensed at dispatch (a plan through doors), or Withdraw's one.
        var withdrawOpen = withdrawOpenV2 && !withdrawOpenSpentV2 && edge == withdrawOpenEdgeV2;
        var mayOpen = ((planThroughDoors && openLicenceV2.Contains(edge)) || withdrawOpen) && world.RelayMayOpen(door);
        if (doorActV2 == DoorActV2.Wall)
        {
            // Listening at a WALL: it goes on listening unless it has since heard the door move (a licence now).
            if (!mayBreak && !mayOpen) return TickWallV2(door, here, next, speed, dt);
            EndDoorActV2();
        }
        if (mayBreak)
        {
            if (MoveDirect(BreakStance(door, here, next, out var facing), speed, dt)) BeginBreakV2(door, facing);
            return false;
        }
        if (mayOpen)
        {
            if (!MoveDirect(BreakStance(door, here, next, out var openFacing), speed, dt)) return false;
            doorFacingV2 = openFacing;
            if (world.OpenAjar(door, position))
            {
                openLicenceV2.Remove(edge);
                if (withdrawOpen) withdrawOpenSpentV2 = true;
                DoorsOpened++;
                BeginDoorActV2(DoorActV2.Open, edge);
                RelayDoorOpened?.Invoke(door.position);
            }
            // Refused for now (a leaf still settling, a key turn, a break under way): it waits at the stance.
            return false;
        }
        // WALL: round it when that way is at most 1.5 times the way through, else stop at the door and listen.
        if (WallReplanV2(here)) return false;
        BreakStance(door, here, next, out var wallFacing);
        doorFacingV2 = wallFacing;
        BeginDoorActV2(DoorActV2.Wall, edge);
        doorActReachedV2 = false;
        DoorsWalled++;
        DoorWalled?.Invoke(door.position);
        return TickWallV2(door, here, next, speed, dt);
    }

    /// <summary>
    /// Follow at an Ajar door (RELAY 10): its own OPEN runs its 1.0 s first; then it pushes the leaf on (0.4 s) and walks
    /// through once the leaf lets it (the passage turns Open). Any state may push: an Ajar door is not shut.
    /// </summary>
    bool FollowAjarV2(GridCoord here, GridCoord next, float speed, float dt)
    {
        var door = world.DoorBetween(here, next);
        if (door == null) return !Replan(here);
        var edge = door.edge;
        if (doorActV2 != DoorActV2.None && doorActEdgeV2 != edge) EndDoorActV2();
        if (doorActV2 == DoorActV2.Open)
        {
            doorActClockV2 += dt;
            if (doorActClockV2 + TimeSlack < FrontRoomsMapWorld.RelayOpenSeconds) return false;
            EndDoorActV2();
        }
        if (doorActV2 == DoorActV2.Push)
        {
            doorActClockV2 += dt;
            // A leaf held short of passable (a body in the way, a rebuild): push again.
            if (doorActClockV2 > FrontRoomsMapWorld.RelayPushSeconds + PushRetrySeconds) EndDoorActV2();
            return false;
        }
        if (!MoveDirect(BreakStance(door, here, next, out var facing), speed, dt)) return false;
        doorFacingV2 = facing;
        if (world.PushAjar(door, position))
        {
            if (pushedDoorV2 != null && pushedDoorV2.edge != edge) SettlePushedV2();
            DoorsPushed++;
            BeginDoorActV2(DoorActV2.Push, edge);
            pushedDoorV2 = door;
            pushedClockV2 = 0f;
            RelayDoorPushed?.Invoke(door.position);
        }
        return false;
    }

    /// <summary>
    /// The WALL's listen: walk to the door's stance, listen WallListenSeconds, then in a hunt the give-up beat (no patrol)
    /// and Withdraw; in a chase it stands (its lose meter ends the chase); in a patrol, a search or Withdraw the leg is
    /// dropped (true).
    /// </summary>
    bool TickWallV2(FrontRoomsMapWorld.Door door, GridCoord here, GridCoord next, float speed, float dt)
    {
        if (!doorActReachedV2)
        {
            // MoveDirect also ends on a stance it cannot reach (a covered aim): it listens where it stands.
            if (!MoveDirect(BreakStance(door, here, next, out _), speed, dt)) return false;
            doorActReachedV2 = true;
            doorActClockV2 = 0f;
            progressAnchor = position;
            progressTime = 0f;
            return false;
        }
        doorActClockV2 += dt;
        if (doorActClockV2 + TimeSlack < WallListenSeconds || State == HunterState.Chase) return false;
        var state = State;
        EndDoorActV2();
        if (state == HunterState.Hunt)
        {
            // "Shut the door and walk away ends an investigation."
            BeginGiveUpV2(false);
            return false;
        }
        return true;
    }

    /// <summary>A WALL mid-way: the way round from here when it is at most WallReplanRatio times what was left of the way through. True: re-planned.</summary>
    bool WallReplanV2(GridCoord here)
    {
        if (path.Count == 0) return false;
        var to = path[path.Count - 1];
        var left = Mathf.Max(1, path.Count - pathIndex);
        if (SweepLegActiveV2)
        {
            // V3c: a sweep's way round keeps every cell at W 21 m or more, as its legs do.
            EnsurePlayerField(world.CellOf(sightFeet));
            SearchV2(here, 64, planThroughDoors ? 1 : 0, SweepMinW, sightFeet);
        }
        else SearchV2(here, 64, planThroughDoors ? 1 : 0);
        if (!depth.TryGetValue(to, out var round) || round > WallReplanRatio * left) return false;
        // Rebuild from here (the goal point is kept); the door is now off the way.
        path.Clear();
        pathIndex = 0;
        for (var c = to; c != here; c = cameFrom[c]) path.Add(c);
        path.Reverse();
        ResetSteering();
        return true;
    }

    void BeginBreakV2(FrontRoomsMapWorld.Door door, Vector3 facing)
    {
        breakingDoor = door;
        breakFacing = facing;
        // A12: a shut leaf goes away from it, so the blows jolt it that way and it bursts into the next room.
        world.SwingAwayFrom(door, position);
        world.MarkBeingBroken(door, true);
        StartBlows(door);
        resumeState = State;
        SetState(HunterState.BreakDoor);
    }

    void BeginDoorActV2(DoorActV2 act, long edge)
    {
        doorActV2 = act;
        doorActEdgeV2 = edge;
        doorActClockV2 = 0f;
        doorActReachedV2 = act != DoorActV2.Wall;
        // Standing at a door is not a stall.
        progressAnchor = position;
        progressTime = 0f;
    }

    void EndDoorActV2()
    {
        doorActV2 = DoorActV2.None;
        doorActEdgeV2 = 0;
        doorActClockV2 = 0f;
        doorActReachedV2 = false;
    }

    /// <summary>Its facing while it stands at a door it OPENs, pushes or listens at (Tick's Heading).</summary>
    bool DoorActFacingV2(out Vector3 facing)
    {
        facing = doorFacingV2;
        return doorActV2 != DoorActV2.None && (doorActV2 != DoorActV2.Wall || doorActReachedV2);
    }

    /// <summary>Each on-map tick: a pushed leaf settles back Ajar once it is SettleClearMetres from the door (or after SettleAfterSeconds).</summary>
    void TickPushedDoorV2(float dt)
    {
        if (pushedDoorV2 == null) return;
        pushedClockV2 += dt;
        if (doorActV2 == DoorActV2.Push && doorActEdgeV2 == pushedDoorV2.edge) return;
        if (pushedClockV2 < FrontRoomsMapWorld.RelayPushSeconds) return;
        if (Flat(position - pushedDoorV2.position).magnitude > SettleClearMetres || pushedClockV2 > SettleAfterSeconds) SettlePushedV2();
    }

    void SettlePushedV2()
    {
        if (pushedDoorV2 == null) return;
        world.SettleAjar(pushedDoorV2);
        pushedDoorV2 = null;
        pushedClockV2 = 0f;
    }

    // ---------- Withdraw's one OPEN ----------

    /// <summary>
    /// RELAY 11, exits 1-2 "failing that, with one OPEN on its own route": the entry it arrived by, else the Relay entry
    /// farthest from the player, whose way (doors passable) keeps the route rules and crosses at most one shut door that
    /// it may open. That door becomes Withdraw's one OPEN (once per Withdraw). False: none (the search scratch is spent).
    /// </summary>
    bool PickWithdrawOpenV2(GridCoord myCell, out GridCoord goalCell, out Vector3 goalPoint)
    {
        goalCell = myCell;
        goalPoint = position;
        if (withdrawOpenSpentV2) return false;
        Search(myCell, WithdrawSearchCells, true);
        long edge;
        if (hasArriveCellV2 && arriveCellV2 != myCell && depth.ContainsKey(arriveCellV2) && RouteOffV2(arriveCellV2, myCell) && OneOpenV2(arriveCellV2, myCell, out edge))
        {
            goalCell = arriveCellV2;
            goalPoint = arriveFeetV2;
            GrantWithdrawOpenV2(edge);
            return true;
        }
        world.RelayEntries(entries);
        var bestW = -1f;
        var found = false;
        var bestEdge = 0L;
        for (var k = 0; k < entries.Count; k++)
        {
            var c = world.CellOf(entries[k].pos);
            if (c == myCell || !depth.ContainsKey(c) || IsArriveExcluded(c) || world.InStartArea(c) || world.InStairwell(c)) continue;
            var feet = EntryFeet(k, c);
            var w = WAtV2(c, feet, sightFeet);
            if (w <= bestW || !RouteOffV2(c, myCell) || !OneOpenV2(c, myCell, out edge)) continue;
            bestW = w;
            goalCell = c;
            goalPoint = feet;
            bestEdge = edge;
            found = true;
        }
        if (found) GrantWithdrawOpenV2(bestEdge);
        return found;
    }

    // At most one shut door on the way back from an exit to where it stands, one it may open (edge 0: none).
    bool OneOpenV2(GridCoord exit, GridCoord myCell, out long edge)
    {
        edge = 0;
        var doors = 0;
        for (var c = exit; c != myCell; )
        {
            if (!cameFrom.TryGetValue(c, out var back)) return false;
            if (world.PassageBetween(back, c) == FrontRoomsMapWorld.Passage.ClosedDoor)
            {
                var door = world.DoorBetween(back, c);
                if (door == null || !world.RelayMayOpen(door) || ++doors > 1) return false;
                edge = door.edge;
            }
            c = back;
        }
        return true;
    }

    void GrantWithdrawOpenV2(long edge)
    {
        if (edge == 0) return;
        withdrawOpenV2 = true;
        withdrawOpenEdgeV2 = edge;
    }

    // ---------- The sweep (Q2) ----------

    bool SweepCellOkV2(GridCoord c) => world.IsBuilt(c) && !IsArriveExcluded(c) && !world.InStartArea(c) && !world.InStairwell(c) && !world.InExitArea(c);

    /// <summary>
    /// V3c (Fix 3, systems design 2026-10-30, "(b) + guard"): waypoint 1 picked where the Relay can step in, and the
    /// placement for it. Its candidates (SweepFirstCandidatesV2: W 21-30 m, in V3b's heading order) are tried in turn, at
    /// most SweepFirstTries and none within SweepSpacingCells of one tried: the region round it whose every cell keeps W at
    /// least SweepMinW on the way to it (doors it may open pass; at most ArriveWideMax steps) must hold a placement by
    /// Arrive v2's rules (PlaceV2 kept to that region, soft rule b counted as kept), so the first leg can keep W at least
    /// 21 m from the step-in. Only when none does (the guard): V3b's waypoint 1, the heading's best, placed with soft rule
    /// b kept at every stage (the route never nearer the player than waypoint 1, the sweep's W). Then waypoints 2-4
    /// (PickSweepRestV2). False, nothing placed: no candidate, or no placement (LastArriveReport says why).
    /// </summary>
    bool PlaceSweepV2(FrontRoomsRelayCall call, bool relaxed, Vector3 playerFeet, Vector3 playerEye, Vector3 playerForward, Vector3 heading, out FrontRoomsRelayCall placed)
    {
        placed = null;
        LastSweepRuleB = false;
        LastSweepFirstTries = 0;
        if (!SweepFirstCandidatesV2(playerFeet, heading))
        {
            LastArriveReport = "no sweep: no cell along the heading at W " + SweepMinW.ToString("0") + "-" + SweepNearMaxW.ToString("0") + " m";
            return false;
        }
        var minW = relaxed ? ArriveFallbackMinW : ArriveMinW;
        var best = sweepFirstV2[sweepOrderV2[0]];
        var first = best;
        sweepTriedV2.Clear();
        for (var i = 0; i < sweepOrderV2.Count && LastSweepFirstTries < SweepFirstTries && placed == null; i++)
        {
            var c = sweepFirstV2[sweepOrderV2[i]];
            if (NearAnyV2(c, sweepTriedV2)) continue;
            sweepTriedV2.Add(c);
            LastSweepFirstTries++;
            if (!SweepSafeRegionV2(c, playerFeet, minW)) continue;
            var tryCall = SweepCallV2(call, c);
            sweepSafeOnlyV2 = true;
            var ok = PlaceV2(tryCall, relaxed, playerFeet, playerEye, playerForward);
            sweepSafeOnlyV2 = false;
            if (!ok) continue;
            first = c;
            placed = tryCall;
        }
        if (placed == null)
        {
            // The guard: no waypoint 1 it could step in for keeps W 21 m all the way: rule b at the sweep's W.
            var guardCall = SweepCallV2(call, best);
            sweepKeepRouteV2 = true;
            var ok = PlaceV2(guardCall, relaxed, playerFeet, playerEye, playerForward);
            sweepKeepRouteV2 = false;
            if (!ok)
            {
                LastArriveReport = "no sweep: no placement for waypoint 1 (" + LastSweepFirstTries + " tried, then rule b) - " + LastArriveReport;
                return false;
            }
            LastSweepRuleB = true;
            first = best;
            placed = guardCall;
        }
        PickSweepRestV2(first, playerFeet);
        return true;
    }

    FrontRoomsRelayCall SweepCallV2(FrontRoomsRelayCall call, GridCoord first) =>
        new FrontRoomsRelayCall(world.CellCenter(first), 0, FrontRoomsRelayCallCause.Sweep, call.Explicit, call.Queued, call.AttentionAtCall);

    /// <summary>
    /// Waypoint 1's candidates: the cells at W 21-30 m (SweepCellOkV2, at most SweepSearchCells of walking) on the
    /// player's side of <paramref name="heading"/> (dot at least 0), ordered as V3b picked its one: in a zone next to the
    /// player's within 60 deg of the heading, then in a zone next to theirs, then their own zone; the larger dot first,
    /// then the search's order. False: none.
    /// </summary>
    bool SweepFirstCandidatesV2(Vector3 feet, Vector3 heading)
    {
        sweepFirstV2.Clear();
        sweepFirstKeyV2.Clear();
        sweepOrderV2.Clear();
        var playerCell = world.CellOf(feet);
        EnsurePlayerField(playerCell);
        var head = Flat(heading);
        if (head.sqrMagnitude < 1e-6f) head = Flat(sightForward);
        head = head.sqrMagnitude > 1e-6f ? head.normalized : Vector3.forward;
        var playerZone = world.ZoneOf(playerCell).id;
        Search(playerCell, SweepSearchCells, true);
        foreach (var pair in depth)
        {
            var c = pair.Key;
            if (!SweepCellOkV2(c)) continue;
            var p = world.CellCenter(c);
            var w = WAtV2(c, p, feet);
            if (w < SweepMinW || w > SweepNearMaxW) continue;
            var dir = Flat(p - feet);
            if (dir.sqrMagnitude < 1e-4f) continue;
            var dot = Vector3.Dot(dir.normalized, head);
            if (dot < 0f) continue;
            var otherZone = world.ZoneOf(c).id != playerZone;
            var stage = otherZone && dot >= SweepAheadDot ? 0 : otherZone ? 1 : 2;
            sweepOrderV2.Add(sweepFirstV2.Count);
            sweepFirstV2.Add(c);
            sweepFirstKeyV2.Add(stage * 4f - dot);
        }
        sweepOrderV2.Sort((a, b) =>
        {
            var byKey = sweepFirstKeyV2[a].CompareTo(sweepFirstKeyV2[b]);
            return byKey != 0 ? byKey : a.CompareTo(b);
        });
        return sweepFirstV2.Count > 0;
    }

    // The cells round <paramref name="first"/> (at most ArriveWideMax steps, doors it may open) whose way to it keeps W at
    // least SweepMinW, into sweepSafeV2; true when one of them has W at least <paramref name="minW"/> (Arrive's floor).
    bool SweepSafeRegionV2(GridCoord first, Vector3 feet, float minW)
    {
        sweepSafeV2.Clear();
        SearchV2(first, ArriveWideMax, 2, SweepMinW, feet);
        var any = false;
        foreach (var pair in depth)
        {
            sweepSafeV2.Add(pair.Key);
            if (!any && WAtV2(pair.Key, world.CellCenter(pair.Key), feet) >= minW) any = true;
        }
        return any;
    }

    // A cell within SweepSpacingCells (Chebyshev) of one in <paramref name="cells"/>.
    static bool NearAnyV2(GridCoord c, List<GridCoord> cells)
    {
        for (var i = 0; i < cells.Count; i++)
            if (Mathf.Max(Mathf.Abs(c.x - cells[i].x), Mathf.Abs(c.y - cells[i].y)) <= SweepSpacingCells) return true;
        return false;
    }

    /// <summary>
    /// The sweep's waypoints after <paramref name="first"/> (V3b's rule): up to 3 more, each 3-12 open cells from the last,
    /// every route cell at W 21 m or more, none within 2 cells of another, in waypoint 1's zone first.
    /// </summary>
    void PickSweepRestV2(GridCoord first, Vector3 feet)
    {
        sweepWaysV2.Clear();
        sweepWaysV2.Add(first);
        EnsurePlayerField(world.CellOf(feet));
        var zone = world.ZoneOf(first).id;
        var last = first;
        while (sweepWaysV2.Count < SweepWaypoints)
        {
            SearchV2(last, SweepLegMaxCells, 0);
            sweepPoolV2.Clear();
            sweepOtherV2.Clear();
            foreach (var pair in depth)
            {
                var c = pair.Key;
                if (pair.Value < SweepLegMinCells || !SweepCellOkV2(c) || NearWaypointV2(c)) continue;
                if (WAtV2(c, world.CellCenter(c), feet) < SweepMinW || !LegKeepsOffV2(c, last, feet)) continue;
                if (world.ZoneOf(c).id == zone) sweepPoolV2.Add(c);
                else sweepOtherV2.Add(c);
            }
            var pool = sweepPoolV2.Count > 0 ? sweepPoolV2 : sweepOtherV2;
            if (pool.Count == 0) break;
            last = pool[(int)(Next() % (uint)pool.Count)];
            sweepWaysV2.Add(last);
        }
    }

    /// <summary>
    /// V3c: the sweep's dispatch also licenses (OPEN) the shut doors it may open on its way from the placement to waypoint
    /// 1 that keeps every cell at W 21 m or more (the way PlaceSweepV2 found), so the first leg can take that way.
    /// </summary>
    void LicenseSweepRouteV2()
    {
        if (sweepWaysV2.Count == 0 || world == null) return;
        var from = RouteCell(world.CellOf(position));
        var to = sweepWaysV2[0];
        if (from == to) return;
        EnsurePlayerField(world.CellOf(sightFeet));
        SearchV2(from, 64, 2, SweepMinW, sightFeet);
        if (!depth.ContainsKey(to)) return;
        for (var c = to; c != from; )
        {
            var back = cameFrom[c];
            if (world.PassageBetween(back, c) == FrontRoomsMapWorld.Passage.ClosedDoor)
            {
                var door = world.DoorBetween(back, c);
                if (door != null && world.RelayMayOpen(door)) openLicenceV2.Add(door.edge);
            }
            c = back;
        }
    }

    bool NearWaypointV2(GridCoord c)
    {
        for (var i = 0; i < sweepWaysV2.Count; i++)
            if (Mathf.Max(Mathf.Abs(c.x - sweepWaysV2[i].x), Mathf.Abs(c.y - sweepWaysV2[i].y)) <= SweepSpacingCells) return true;
        return false;
    }

    // Every cell of the last search's way from <paramref name="from"/> back to <paramref name="to"/> at W SweepMinW or more.
    bool LegKeepsOffV2(GridCoord from, GridCoord to, Vector3 feet)
    {
        for (var c = from; c != to; )
        {
            if (WAtV2(c, world.CellCenter(c), feet) < SweepMinW) return false;
            if (!cameFrom.TryGetValue(c, out var back)) return false;
            c = back;
        }
        return true;
    }

    /// <summary>The step-in's listen is over: walk to the first waypoint (the dispatch's way, its OPEN licences), then the rest.</summary>
    void BeginSweepV2(GridCoord myCell)
    {
        sweepWayV2 = -1;
        if (!NextSweepLegV2(myCell, true)) EndSweepV2();
    }

    void TickSweepV2(GridCoord myCell, float dt)
    {
        sweepLeftV2 -= dt;
        if (sweepLeftV2 <= 0f || fadingV2)
        {
            EndSweepV2();
            return;
        }
        if (Follow(tuning.huntSpeed * WanderPace, dt) || Stalled(dt))
        {
            ResetSteering();
            if (!NextSweepLegV2(myCell, false)) EndSweepV2();
        }
    }

    /// <summary>
    /// The next waypoint in order (round again after the last) with a way from here that keeps W at least 21 m now (RELAY
    /// 6: "when planned"; V3c: the way is planned on such cells only, PlanSweepLegV2, not the shortest way checked after);
    /// the first leg goes through the dispatch's licensed doors, the others keep to shut doors. False: none.
    /// </summary>
    bool NextSweepLegV2(GridCoord from, bool first)
    {
        var count = sweepWaysV2.Count;
        for (var k = 1; k <= count; k++)
        {
            var i = ((sweepWayV2 < 0 ? -1 : sweepWayV2) + k) % count;
            var way = sweepWaysV2[i];
            if (way == from) continue;
            if (!PlanSweepLegV2(from, way, first && i == 0)) continue;
            sweepWayV2 = i;
            SetState(HunterState.Wander);
            return true;
        }
        path.Clear();
        pathIndex = 0;
        return false;
    }

    /// <summary>
    /// V3c: Plan for a sweep's leg (and its replans, Replan): the shortest way on cells at W SweepMinW or more from the
    /// player now (the start cell excepted), through licensed doors when <paramref name="throughDoors"/> (the first leg:
    /// the dispatch's OPEN licences), else keeping to shut doors. False when no such way reaches the cell (the path left
    /// empty: that leg is over).
    /// </summary>
    bool PlanSweepLegV2(GridCoord from, GridCoord to, bool throughDoors)
    {
        var finalPoint = world.CellCenter(to);
        KeepOutOfExit(ref to, ref finalPoint);
        goal = finalPoint;
        planThroughDoors = throughDoors;
        path.Clear();
        pathIndex = 0;
        if (from == to) return true;
        EnsurePlayerField(world.CellOf(sightFeet));
        SearchV2(from, 64, throughDoors ? 1 : 0, SweepMinW, sightFeet);
        var planned = BuildPathV2(from, to);
        // A door act lasts only while the new way still goes through that door (as PlanRouteV2).
        if (doorActV2 != DoorActV2.None && !PathCrossesV2(from, doorActEdgeV2)) EndDoorActV2();
        return planned;
    }

    // A sweep's leg is under way (its legs and their replans keep W at least 21 m).
    bool SweepLegActiveV2 => sweepV2 && State == HunterState.Wander;

    /// <summary>The sweep is over (its time, FADE, or no leg keeps off the player): the give-up beat, then Withdraw.</summary>
    void EndSweepV2()
    {
        sweepV2 = false;
        BeginGiveUpV2(false);
    }

    // ---------- Shared ----------

    // More than <paramref name="cells"/> walking cells (doors passable) from where it stands to the point's snapped cell.
    bool WalkingCellsOverV2(Vector3 point, int cells)
    {
        var target = SnapEstimateV2(world.CellOf(point));
        Search(world.CellOf(position), cells + 1, true);
        return !depth.TryGetValue(target, out var d) || d > cells;
    }

    /// <summary>V3b's part of the encounter reset (Park, DebugPlace, Arm, Away): licences, the door act, a pushed leaf settles, the sweep.</summary>
    void ResetDoorsV3b()
    {
        openLicenceV2.Clear();
        breakLicenceV2.Clear();
        withdrawOpenV2 = withdrawOpenSpentV2 = false;
        withdrawOpenEdgeV2 = 0;
        EndDoorActV2();
        SettlePushedV2();
        sweepV2 = false;
        sweepWayV2 = 0;
        sweepLeftV2 = 0f;
        sightSearchV2 = false;
    }
}
#endif
