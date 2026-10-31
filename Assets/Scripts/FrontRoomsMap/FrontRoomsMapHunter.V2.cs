#if FRONTROOMS_RELAY_V2
using System;
using System.Collections.Generic;
using FrontRooms.Map;
using UnityEngine;

/// <summary>
/// Relay v2's half of the map hunter (RELAY_PURSUIT_REDESIGN sections 4, 5, 11; step V3a), compiled only with
/// FRONTROOMS_RELAY_V2. FrontRoomsRelayDirector drives it through IFrontRoomsRelayHunter:
/// Dormant -(Arm, at the floor's start-door shut)-> Away -(Dispatch)-> Arrive (backstage: placed, hidden, silent, stage 0,
/// for the tier's arrive delay) -(step-in: W at least 39 m and unseen, else re-picked; Arrived)-> Listen 1.5 s -> Hunt to the
/// call's estimate -> the look (V4b's search and eyes) -> the give-up beat (Listen 2 s, GaveUp) -> patrol (Wander,
/// tier 3 and up, not after FADE, never after L11) -> Withdraw (exits 1-4 with their route rules, the terminal fallback, the
/// 20 s timeout, seen 2 s re-pick, watched 6 s then occluded) -> Away (Withdrew, WentAway) -> the director's RELAX.
/// The timed release and both leashes are not compiled with the define; an unbuilt cell under it is Away (UnbuiltCell).
/// Without a director (nav and interaction tests) it still runs: DebugPlace puts it on the map, and Away comes only through
/// Withdraw or SendAway.
/// </summary>
public sealed partial class FrontRoomsMapHunter : IFrontRoomsRelayHunter
{
    // ---------- RELAY 5 and 11's numbers (SP) ----------

    /// <summary>Arrive's hard rule (W at least 42 m), the step-in re-check (39 m) and the 10 s fallback (36 m, still past stage 1's 36 m exit).</summary>
    public const float ArriveMinW = 42f, StepInMinW = 39f, ArriveFallbackMinW = 36f;
    /// <summary>Soft rule f: 8-16 walking cells from the estimate, widened last to 6-20; the fallback takes 2-20.</summary>
    public const int ArriveNearMin = 8, ArriveNearMax = 16, ArriveWideMin = 6, ArriveWideMax = 20, ArriveFallbackMinCells = 2;
    /// <summary>Soft rule c: cells inside the built window; soft rule e: outside the player's forward half-plane below this dot.</summary>
    public const int ArriveInsideCells = 3;
    public const float ArriveForwardDot = .3f;
    /// <summary>The listen on the step-in, and the give-up beat.</summary>
    public const float StepInListenSeconds = 1.5f, GiveUpSeconds = 2f;
    /// <summary>Patrol legs are planned with W at least this (RELAY 11), 4-12 cells of open walking away.</summary>
    public const float PatrolMinW = 21f;
    const int PatrolMinCells = 4, PatrolMaxCells = 12;
    /// <summary>Withdraw exits 1-2: every route cell with W over 15 m when planned and 4 cells inside the built window; exit 4: F at least 10 cells.</summary>
    public const float WithdrawRouteMinW = 15f;
    public const int WithdrawInsideCells = 4, WithdrawUnseenCells = 10;
    const int WithdrawSearchCells = 40, WithdrawRepickCells = 20, WithdrawUnseenTests = 32;
    /// <summary>Withdraw: no exit within 2 s is the terminal fallback; 20 s is the timeout (terminal from where it is); 25 s the invariant's backstop.</summary>
    public const float WithdrawPickSeconds = 2f, WithdrawTimeoutSeconds = 20f, WithdrawBackstopSeconds = 25f;
    /// <summary>Seen 2 s on the way out: re-pick; watched 6 s: Away on the first frame it is occluded by geometry with W at least 21 m or a door or corner between.</summary>
    public const float WithdrawSeenSeconds = 2f, WithdrawWatchedSeconds = 6f, WithdrawWatchedMinW = 21f;
    /// <summary>On the way out and under FADE a lock-on comes only within this (RELAY 4, 11).</summary>
    public const float FadeLockOnMetres = 6f;
    /// <summary>Past onMapCap + 15 s a chase ends on this much broken sight (RELAY 4).</summary>
    public const float HardEndLoseSeconds = .5f;
    // The step-in re-pick follows PENDING's retry (RELAY 5): every 0.5 s, the fallback at 10 s, then dropped (Away).
    const float RepickRetrySeconds = .5f, RepickTimeoutSeconds = 10f;
    // A door or a corner between (watched Away): an opening on the path, or the path this much longer than the straight line (plus a cell).
    const float CornerPathRatio = 1.25f;

    const int SoftEntry = 1, SoftRoute = 2, SoftInside = 4, SoftFarSide = 8, SoftBehind = 16, SoftNear = 32;
    // Arrive's soft rules dropped in RELAY 5's order: a, b, c, d, e, then f widened (stage 6 keeps only the 6-20 band).
    static readonly int[] SoftStages = { 63, 62, 60, 56, 48, 32, 0 };

    enum ListenKindV2 : byte { Plain, StepIn, GiveUp }

    ListenKindV2 listenKindV2;
    // The call it answers (null when placed by tools), the estimate it hunts after the step-in's listen.
    FrontRoomsRelayCall callV2;
    Vector3 estimateV2;
    GridCoord estimateCellV2;
    bool hasEstimateV2;
    // Arrive: clocks, the delay, the step-in re-pick, whether this is floor 1's first send, the entry tag and the arrival.
    float arriveClockV2, arriveDelayV2, repickClockV2, repickRetryV2;
    bool repickingV2, firstSendArriveV2;
    string arriveTagV2;
    GridCoord arriveCellV2;
    Vector3 arriveFeetV2;
    bool hasArriveCellV2;
    // FADE and the hard end; a heard noise's cautious look on the way out; this chase's capped lose threshold (-1: none).
    bool fadingV2, hardEndV2, cautiousUsedV2;
    float chaseLoseCapV2 = -1f;
    // The give-up beat's patrol and what is left of it.
    bool patrolAfterBeatV2;
    float patrolLeftV2;
    // Withdraw. withdrawBegunV2 / withdrawAtV2: its one clock, from the first entry (ruled 2026-10-30: a look never restarts it).
    GridCoord withdrawGoalV2;
    bool withdrawPickedV2, withdrawTerminalV2, withdrawSeenRepickedV2, withdrawWatchedV2, withdrawBegunV2;
    float withdrawClockV2, withdrawAtV2, withdrawRetryV2, withdrawSeenRunV2, withdrawWatchedForV2;
    FrontRoomsRelayAwayReason withdrawReasonV2;
    // Placement scratch: the candidates (cell, walking cells from the estimate, soft-rule bits, entry index or -1, W, checked: 0 / 1 ok / -1 not).
    readonly List<GridCoord> v2Cells = new List<GridCoord>();
    readonly List<int> v2Depth = new List<int>(), v2Flags = new List<int>(), v2Entry = new List<int>(), v2Pick = new List<int>();
    readonly List<float> v2W = new List<float>();
    readonly List<sbyte> v2Checked = new List<sbyte>();
    readonly Dictionary<GridCoord, int> entryAtV2 = new Dictionary<GridCoord, int>();
    readonly List<GridCoord> v2Order = new List<GridCoord>();
    readonly List<float> v2Key = new List<float>();

    /// <summary>The run tier's v2 row (arrive delay, patrol seconds): the game sets it with the tuning (FrontRoomsTierRules.V2At).</summary>
    public FrontRoomsTierV2 TierV2 { get; set; } = new FrontRoomsTierV2();
    /// <summary>Why it last went Away (Test before any).</summary>
    public FrontRoomsRelayAwayReason LastAwayReason { get; private set; } = FrontRoomsRelayAwayReason.Test;
    /// <summary>Tools: what the last placement chose (the soft-rule stage, W, cells from the estimate, the entry tag) or why none.</summary>
    public string LastArriveReport { get; private set; } = "";
    /// <summary>Tools: the soft-rule stage of the last Normal placement (0 = every rule kept, 6 = only the 6-20 band), 7 for Relaxed, -1 for FirstSend or none.</summary>
    public int LastArriveStage { get; private set; } = -1;
    /// <summary>The point the hunt after the step-in's listen goes to (the call's estimate, error applied), or null.</summary>
    public Vector3? ArriveHuntPoint => hasEstimateV2 ? estimateV2 : (Vector3?)null;
    /// <summary>The call it is answering (null when placed by tools or Away).</summary>
    public FrontRoomsRelayCall CurrentCall => callV2;
    /// <summary>FADE reached it (Fade), or it chased or heard on the way out; the hard end (onMapCap + 15 s).</summary>
    public bool Fading => fadingV2;
    public bool HardEnded => hardEndV2;
    /// <summary>Seconds since the current Withdraw began (0 outside Withdraw); true once it is on the terminal fallback.</summary>
    public float WithdrawSeconds => State == HunterState.Withdraw ? withdrawClockV2 : 0f;
    public bool WithdrawTerminal => State == HunterState.Withdraw && withdrawTerminalV2;
    /// <summary>Seconds left of the arrive delay (0 outside Arrive), and whether the step-in is being re-picked.</summary>
    public float ArriveSecondsLeft => State == HunterState.Arrive ? Mathf.Max(0f, arriveDelayV2 - arriveClockV2) : 0f;
    public bool ArriveRepicking => State == HunterState.Arrive && repickingV2;

    /// <summary>RELAY 5 (sound design hook): the give-up beat began, at its feet (it reports Listen).</summary>
    public event Action<Vector3> GaveUp;
    /// <summary>RELAY 11 (sound design hook): it left the map from where it stood (Away from an on-map state).</summary>
    public event Action<Vector3> Withdrew;
    /// <summary>It went Away, and why (the director's NotifyAway).</summary>
    public event Action<FrontRoomsRelayAwayReason> WentAway;

    // ---------- IFrontRoomsRelayHunter ----------

    /// <summary>RELAY 4: Dormant to Away (armed, off the map, hidden). Nothing else changes.</summary>
    public void Arm()
    {
        if (caught || State != HunterState.Dormant) return;
        ResetEncounterV2();
        ResetWarningMetrics();
        PlayerSeesRelay = RelayLit = false;
        SetState(HunterState.Away);
    }

    /// <summary>RELAY 4: Park becomes Disarm (C22's floor change): Dormant, everything of the floor dropped.</summary>
    public void Disarm() => Park();

    /// <summary>
    /// Place for a call while Away: FirstSend (the run's first send, Red 2026-10-30: A26 M7's ladder from the player, C32, on
    /// floor 1's station or the generic ladder below it), Relaxed (every soft rule dropped, W at least 36 m) or Normal (Arrive
    /// v2). True: placed, Arrive under way (a FirstSend sets FirstSendDone and OnFirstSend; any other clears OnFirstSend).
    /// False: nothing changed.
    /// </summary>
    public bool Dispatch(FrontRoomsRelayCall call, FrontRoomsRelayPlacement placement, Vector3 playerFeet, Vector3 playerEye, Vector3 playerForward)
    {
        if (caught || world == null || call == null || State != HunterState.Away) return false;
        sightFeet = playerFeet;
        sightEye = playerEye;
        sightForward = playerForward;
        var first = placement == FrontRoomsRelayPlacement.FirstSend;
        var placed = first ? PlaceFirstSendV2(call, playerFeet, playerEye, playerForward)
            : PlaceV2(call, placement == FrontRoomsRelayPlacement.Relaxed, playerFeet, playerEye, playerForward);
        if (!placed) return false;
        BeginArriveV2(call, first);
        return true;
    }

    /// <summary>FADE (RELAY 4): no patrol after this (a patrol under way ends in Withdraw: it counts against onMapCap). <paramref name="hardEnd"/>: past onMapCap + 15 s.</summary>
    public void Fade(bool hardEnd)
    {
        fadingV2 = true;
        if (hardEnd) hardEndV2 = true;
        if (State == HunterState.Wander && !LockingOn) BeginWithdrawV2();
    }

    /// <summary>chaseCap (RELAY 4): this chase loses the player after <paramref name="loseSeconds"/> of broken sight.</summary>
    public void CapChase(float loseSeconds)
    {
        if (State == HunterState.Chase || State == HunterState.BreakDoor) chaseLoseCapV2 = Mathf.Max(0f, loseSeconds);
    }

    /// <summary>Off the map now, from Arrive or the map (a test call, or the director's): WentAway with <paramref name="reason"/>.</summary>
    public void SendAway(FrontRoomsRelayAwayReason reason)
    {
        if (caught || State == HunterState.Dormant || State == HunterState.Away) return;
        GoAwayV2(reason);
    }

    // ---------- Off the map: Dormant, Away, Arrive ----------

    /// <summary>True for a state off the map (Dormant, Away, Arrive): stage 0, unseen, no body; Arrive counts down to its step-in.</summary>
    bool TickOffMap(float dt, Vector3 playerFeet, Vector3 playerEye)
    {
        if (OnMap) return false;
        ResetWarningMetrics();
        PlayerSeesRelay = RelayLit = false;
        playerSeesRelayTimer = 0f;
        SeesPlayer = false;
        Moving = false;
        DoorSqueeze = 0f;
        if (State == HunterState.Arrive)
        {
            StateTime += dt;
            TickArriveV2(dt, playerFeet, playerEye);
        }
        return true;
    }

    void BeginArriveV2(FrontRoomsRelayCall call, bool firstSend)
    {
        callV2 = call;
        firstSendArriveV2 = firstSend;
        // The onboarding (Red 2026-10-30): this encounter is the run's first send (its grace), or a later one (none).
        OnFirstSend = firstSend;
        if (firstSend) FirstSendDone = true;
        arriveClockV2 = 0f;
        arriveDelayV2 = Mathf.Max(0f, TierV2 != null ? TierV2.arriveDelay : 3f);
        repickingV2 = false;
        repickClockV2 = repickRetryV2 = 0f;
        arriveCellV2 = world.CellOf(position);
        arriveFeetV2 = position;
        hasArriveCellV2 = true;
        fadingV2 = hardEndV2 = cautiousUsedV2 = patrolAfterBeatV2 = false;
        chaseLoseCapV2 = -1f;
        patrolLeftV2 = 0f;
        // V3b (RELAY 5): this dispatch's OPEN licences, the doors shut on the way to its estimate now.
        sightSearchV2 = false;
        EndDoorActV2();
        SettlePushedV2();
        LicenseDispatchV2();
        // V4a (RELAY 7): a dispatch's estimate is no heard one: no link carries over, and this encounter's count starts.
        ResetSenseV4a();
        HeardLinks = HeardRetargets = 0;
        // V4b: backstage it perceives nothing (a handoff keeps "acquired this encounter").
        EndLockV2();
        SetState(HunterState.Arrive);
    }

    void TickArriveV2(float dt, Vector3 playerFeet, Vector3 playerEye)
    {
        if (repickingV2)
        {
            repickClockV2 += dt;
            repickRetryV2 -= dt;
            if (repickRetryV2 > 0f) return;
            repickRetryV2 = RepickRetrySeconds;
            var relaxed = repickClockV2 >= RepickTimeoutSeconds;
            // V3c: a sweep re-picks as a sweep (waypoint 1 where it can step in, its legs at W 21 m or more).
            var placed = firstSendArriveV2 ? PlaceFirstSendV2(callV2, playerFeet, playerEye, sightForward)
                : sweepV2 ? RepickSweepV2(relaxed, playerFeet, playerEye, sightForward)
                : callV2 != null && PlaceV2(callV2, relaxed, playerFeet, playerEye, sightForward);
            if (placed)
            {
                repickingV2 = false;
                arriveClockV2 = 0f;
                arriveCellV2 = world.CellOf(position);
                arriveFeetV2 = position;
                // V3b: the re-pick is the dispatch now: its licences are the new way's.
                LicenseDispatchV2();
                if (sweepV2) LicenseSweepRouteV2();
                return;
            }
            // No step-in found in PENDING's 10 s: the call is dropped; it never came on the map.
            if (relaxed || callV2 == null) GoAwayV2(FrontRoomsRelayAwayReason.Timeout);
            return;
        }
        arriveClockV2 += dt;
        if (arriveClockV2 + TimeSlack < arriveDelayV2) return;
        if (!StepInClearV2(playerFeet, playerEye))
        {
            // RELAY 5: the step-in re-check failed (W under 39 m, or seen): re-pick through the same retry.
            repickingV2 = true;
            repickClockV2 = 0f;
            repickRetryV2 = 0f;
            LastArriveReport += " - step-in refused";
            return;
        }
        StepInV2();
    }

    /// <summary>RELAY 5's step-in re-check: unseen at its 1.6 m eye and 1.95 m head; W at least 39 m (floor 1's first send, C32: unseen only).</summary>
    bool StepInClearV2(Vector3 playerFeet, Vector3 playerEye)
    {
        if (VisibleToPlayer(playerEye, position + Vector3.up * RelayLowPoint) || VisibleToPlayer(playerEye, position + Vector3.up * RelayHighPoint)) return false;
        if (firstSendArriveV2) return true;
        EnsurePlayerField(world.CellOf(playerFeet));
        return WAtV2(world.CellOf(position), position, playerFeet) >= StepInMinW;
    }

    void StepInV2()
    {
        SetState(HunterState.Listen);
        listenKindV2 = ListenKindV2.StepIn;
        ListenPoint = hasEstimateV2 ? estimateV2 : (Vector3?)null;
        lastContactTime = clock;
        sawLastTick = false;
        playerSeesRelayTimer = 0f;
        sightTick++;
        // RELAY 5 (sound design hook): Arrived moves from placement to here.
        Arrived?.Invoke(position, arriveTagV2);
    }

    // ---------- Placement ----------

    /// <summary>
    /// RELAY 5's Arrive v2. Hard rules (never relaxed): built (not the station's ring, the start area or a stairwell), the body
    /// fits, unseen at its eye and head, W at least 42 m (36 m relaxed), a route to the estimate with shut doors passable (the
    /// search from the estimate). Soft rules dropped in order: (a) a Relay entry, (b) the route never closer to the player than
    /// the estimate, (c) 3 cells inside the built window, (d) the far side of the estimate, (e) outside the forward half-plane,
    /// (f) 8-16 cells from the estimate, widened last to 6-20. Relaxed: the candidate keeping the most soft rules (then the
    /// largest W), 2-20 cells. The behind-less nearest-entry fallback is gone. One random draw per stage tried.
    /// V3c (the sweep, PlaceSweepV2): while sweepSafeOnlyV2 only the cells of sweepSafeV2 are candidates (their way to
    /// waypoint 1 keeps W at least 21 m, so rule b counts as kept); while sweepKeepRouteV2 (the guard) rule b is kept at
    /// every stage, and the relaxed pick takes a cell keeping it first.
    /// </summary>
    bool PlaceV2(FrontRoomsRelayCall call, bool relaxed, Vector3 playerFeet, Vector3 playerEye, Vector3 playerForward)
    {
        var raw = world.CellOf(call.Estimate);
        var estCell = SnapEstimateV2(raw);
        var estPoint = estCell == raw ? call.Estimate : world.CellCenter(estCell);
        if (!world.IsBuilt(estCell))
        {
            LastArriveReport = "no Arrive: the estimate " + raw + " has no built cell near it";
            return false;
        }
        var playerCell = world.CellOf(playerFeet);
        EnsurePlayerField(playerCell);
        var estW = WAtV2(estCell, estPoint, playerFeet);
        world.RelayEntries(entries);
        entryAtV2.Clear();
        for (var k = 0; k < entries.Count; k++)
        {
            var c = world.CellOf(entries[k].pos);
            if (!entryAtV2.ContainsKey(c)) entryAtV2[c] = k;
        }
        Search(estCell, ArriveWideMax, true);
        v2Cells.Clear();
        v2Depth.Clear();
        v2Flags.Clear();
        v2Entry.Clear();
        v2W.Clear();
        v2Checked.Clear();
        var minW = relaxed ? ArriveFallbackMinW : ArriveMinW;
        var minCells = relaxed ? ArriveFallbackMinCells : ArriveWideMin;
        var fromPlayer = Flat(estPoint - playerFeet);
        var look = Flat(playerForward);
        var hasLook = look.sqrMagnitude > 1e-6f;
        if (hasLook) look.Normalize();
        foreach (var pair in depth)
        {
            var cell = pair.Key;
            var d = pair.Value;
            if (d < minCells || d > ArriveWideMax || !world.IsBuilt(cell) || IsArriveExcluded(cell) || world.InStartArea(cell) || world.InStairwell(cell)) continue;
            if (sweepSafeOnlyV2 && !sweepSafeV2.Contains(cell)) continue;
            var entry = entryAtV2.TryGetValue(cell, out var e) ? e : -1;
            var feet = entry >= 0 ? EntryFeet(entry, cell) : world.CellCenter(cell);
            var w = WAtV2(cell, feet, playerFeet);
            if (w < minW) continue;
            var flags = 0;
            if (entry >= 0) flags |= SoftEntry;
            if (sweepSafeOnlyV2 || RouteKeepsOffV2(cell, estCell, estW, playerFeet)) flags |= SoftRoute;
            if (InsideBuiltV2(cell, ArriveInsideCells)) flags |= SoftInside;
            if (fromPlayer.sqrMagnitude < 1e-4f || Vector3.Dot(Flat(feet - estPoint), fromPlayer) > 0f) flags |= SoftFarSide;
            var off = Flat(feet - playerFeet);
            if (!hasLook || off.sqrMagnitude < 1e-6f || Vector3.Dot(off.normalized, look) < ArriveForwardDot) flags |= SoftBehind;
            if (d >= ArriveNearMin && d <= ArriveNearMax) flags |= SoftNear;
            v2Cells.Add(cell);
            v2Depth.Add(d);
            v2Flags.Add(flags);
            v2Entry.Add(entry);
            v2W.Add(w);
            v2Checked.Add(0);
        }
        var pick = -1;
        var stage = -1;
        if (relaxed)
        {
            // The best: most soft rules kept, then the largest W; checked in that order.
            v2Pick.Clear();
            for (var i = 0; i < v2Cells.Count; i++) v2Pick.Add(i);
            var keep = sweepKeepRouteV2 ? SoftRoute : 0;
            v2Pick.Sort((a, b) =>
            {
                var ka = (v2Flags[a] & keep) == keep;
                var kb = (v2Flags[b] & keep) == keep;
                if (ka != kb) return ka ? -1 : 1;
                var ca = Bits(v2Flags[a]);
                var cb = Bits(v2Flags[b]);
                return ca != cb ? cb.CompareTo(ca) : v2W[b].CompareTo(v2W[a]);
            });
            for (var i = 0; i < v2Pick.Count && pick < 0; i++)
                if (CandidateOkV2(v2Pick[i], playerEye)) pick = v2Pick[i];
            stage = 7;
        }
        else
        {
            for (var s = 0; s < SoftStages.Length && pick < 0; s++)
            {
                var need = SoftStages[s] | (sweepKeepRouteV2 ? SoftRoute : 0);
                v2Pick.Clear();
                for (var i = 0; i < v2Cells.Count; i++)
                    if ((v2Flags[i] & need) == need && v2Checked[i] >= 0) v2Pick.Add(i);
                while (v2Pick.Count > 0)
                {
                    var j = (int)(Next() % (uint)v2Pick.Count);
                    var i = v2Pick[j];
                    if (CandidateOkV2(i, playerEye)) { pick = i; stage = s; break; }
                    v2Pick[j] = v2Pick[v2Pick.Count - 1];
                    v2Pick.RemoveAt(v2Pick.Count - 1);
                }
            }
        }
        if (pick < 0)
        {
            LastArriveStage = -1;
            LastArriveReport = "no Arrive" + (relaxed ? " (relaxed)" : "") + " for " + call.Cause + " at " + estCell + ": " + v2Cells.Count + " cells with W >= " + minW.ToString("0") + " m, none fits unseen";
            return false;
        }
        var at = v2Cells[pick];
        var tag = v2Entry[pick] >= 0 ? entries[v2Entry[pick]].tag : null;
        var feetAt = v2Entry[pick] >= 0 ? EntryFeet(v2Entry[pick], at) : world.CellCenter(at);
        Appear(feetAt, tag);
        LastArriveStage = stage;
        LastArriveReport = (relaxed ? "relaxed" : "soft stage " + stage) + " - " + call.Cause + " - " + at + ", W " + v2W[pick].ToString("0.0") + " m, "
            + v2Depth[pick] + " cells from the estimate " + estCell + (tag != null ? ", entry " + tag : v2Entry[pick] >= 0 ? ", entry" : "") + " - " + v2Cells.Count + " candidates";
        SetEstimateV2(estCell, estPoint, call.ErrorCells);
        return true;
    }

    static int Bits(int v)
    {
        var n = 0;
        for (; v != 0; v &= v - 1) n++;
        return n;
    }

    // The body fits at the candidate and neither its eye nor its head is in the player's sight (cached per placement).
    bool CandidateOkV2(int i, Vector3 playerEye)
    {
        if (v2Checked[i] != 0) return v2Checked[i] > 0;
        var feet = v2Entry[i] >= 0 ? EntryFeet(v2Entry[i], v2Cells[i]) : world.CellCenter(v2Cells[i]);
        var ok = BodyFits(feet) && !VisibleToPlayer(playerEye, feet + Vector3.up * RelayLowPoint) && !VisibleToPlayer(playerEye, feet + Vector3.up * RelayHighPoint);
        v2Checked[i] = (sbyte)(ok ? 1 : -1);
        return ok;
    }

    /// <summary>
    /// The run's first send (C32, A26 M7; Red 2026-10-30: wherever it comes): FirstSend's ladder from the player (rungs 1-3,
    /// else rung 4, v1's Arrive with the station and its ring kept out; rungs 1-2 at W at most 30 m since C32's W fix,
    /// 10-30), and never W at least 42 m; on floor 1's
    /// station its layout (ForStation), with no station the generic ladder (FirstSendPolicy.Generic: rung 1 behind the
    /// player). Its leg after the step-in's listen: the hunt to the station's take point once the key is taken
    /// (FirstSendPolicy.huntTo), else to the call's estimate.
    /// </summary>
    bool PlaceFirstSendV2(FrontRoomsRelayCall call, Vector3 playerFeet, Vector3 playerEye, Vector3 playerForward)
    {
        var policy = FirstSendPolicy.ForStation(world) ?? FirstSendPolicy.Generic();
        var report = new FirstSendReport { rung = 4, cells = -1, margin = float.NaN };
        if (!FirstSend(policy, playerFeet, playerEye, playerForward, ref report))
        {
            LastArriveReport = "no first send: FirstSend's ladder found no cell";
            return false;
        }
        report.hunt = policy.huntTo.HasValue;
        LastFirstSend = report;
        LastArriveStage = -1;
        LastArriveReport = "first send" + (report.station ? "" : " (generic)") + " - rung " + report.rung + " (" + report.rung1 + "/" + report.rung2 + "/" + report.rung3 + "), "
            + report.cell + ", " + report.cells + " walking cells, " + report.straight.ToString("0.0") + " m straight, W " + report.w.ToString("0.0") + " m (C32's W fix: rungs 1-2 at most "
            + FirstSendMaxW.ToString("0") + " m), " + (report.farSide ? report.station && !float.IsNaN(report.margin) ? "far" : "behind" : "near")
            + " side" + (report.unseen ? "" : ", SEEN");
        var to = policy.huntTo ?? (call != null ? call.Estimate : playerFeet);
        // The take point itself, never its cell's middle (as HuntStation).
        estimateV2 = to;
        estimateCellV2 = world.CellOf(to);
        hasEstimateV2 = true;
        return true;
    }

    /// <summary>The estimate's cell, or the nearest (rings out to 4 cells) that is built and not the start area or a stairwell (RELAY 6's snap).</summary>
    GridCoord SnapEstimateV2(GridCoord cell)
    {
        if (EstimateCellOkV2(cell)) return cell;
        for (var r = 1; r <= 4; r++)
        {
            var best = cell;
            var bestD = int.MaxValue;
            for (var dy = -r; dy <= r; dy++)
            for (var dx = -r; dx <= r; dx++)
            {
                if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                var c = new GridCoord(cell.x + dx, cell.y + dy);
                var d = dx * dx + dy * dy;
                if (d < bestD && EstimateCellOkV2(c)) { best = c; bestD = d; }
            }
            if (bestD != int.MaxValue) return best;
        }
        return cell;
    }

    bool EstimateCellOkV2(GridCoord c) => world.IsBuilt(c) && !world.InStartArea(c) && !world.InStairwell(c);

    /// <summary>The hunt's goal after the step-in: the estimate, or (RELAY 6's error: sprint 2, door 1, device 0 cells) a random built cell that many steps from it.</summary>
    void SetEstimateV2(GridCoord estCell, Vector3 estPoint, int errorCells)
    {
        estimateCellV2 = estCell;
        estimateV2 = estPoint;
        hasEstimateV2 = true;
        if (errorCells <= 0) return;
        Search(estCell, errorCells, true);
        candidates.Clear();
        foreach (var pair in depth)
            if (pair.Value > 0 && EstimateCellOkV2(pair.Key)) candidates.Add(pair.Key);
        if (candidates.Count == 0) return;
        var c = candidates[(int)(Next() % (uint)candidates.Count)];
        estimateCellV2 = c;
        estimateV2 = world.CellCenter(c);
    }

    /// <summary>W at a cell (RELAY 7): the player field's F (shut doors +9 m; infinite past its 45 m reach), or three times the straight line, whichever is less.</summary>
    float WAtV2(GridCoord cell, Vector3 point, Vector3 playerFeet) =>
        Mathf.Min(playerField.DistanceTo(cell), 3f * Vector3.Distance(point, playerFeet));

    // Soft rule b: from the candidate back to the estimate (the search's route), no cell is nearer the player (by W) than the estimate.
    bool RouteKeepsOffV2(GridCoord cell, GridCoord estCell, float estW, Vector3 playerFeet)
    {
        for (var c = cell; c != estCell; )
        {
            if (WAtV2(c, world.CellCenter(c), playerFeet) < estW - .5f) return false;
            if (!cameFrom.TryGetValue(c, out var back)) return false;
            c = back;
        }
        return true;
    }

    // At least n cells inside the built window: built n cells away in the 8 directions.
    bool InsideBuiltV2(GridCoord c, int n)
    {
        for (var dy = -1; dy <= 1; dy++)
        for (var dx = -1; dx <= 1; dx++)
            if ((dx != 0 || dy != 0) && !world.IsBuilt(new GridCoord(c.x + dx * n, c.y + dy * n))) return false;
        return true;
    }

    // ---------- On the map: the step-in's listen, the give-up beat, patrol ----------

    void TickListenV2(GridCoord myCell)
    {
        switch (listenKindV2)
        {
            case ListenKindV2.StepIn:
                if (StateTime < StepInListenSeconds) return;
                // V3b (Q2): a sweep walks its waypoints instead of hunting.
                if (sweepV2)
                {
                    BeginSweepV2(myCell);
                    return;
                }
                if (hasEstimateV2)
                {
                    ListenPoint = estimateV2;
                    // RELAY 5's loop (V4b): a dispatch ends in a cautious look, unless a repeat upgrades it.
                    cautiousLookV2 = true;
                    HuntToward(estimateCellV2, estimateV2);
                }
                else BeginGiveUpV2(true);
                return;
            case ListenKindV2.GiveUp:
                if (StateTime < GiveUpSeconds) return;
                if (patrolAfterBeatV2 && patrolLeftV2 > 0f && !fadingV2) BeginPatrolV2(myCell);
                else BeginWithdrawV2();
                return;
            default:
                // A plain Listen (tools: ReleaseNow's first leg): it does not know where the player is; the beat, then out.
                if (StateTime > tuning.listenSeconds) BeginGiveUpV2(true);
                return;
        }
    }

    /// <summary>RELAY 5, 11: the give-up beat (2 s, Listen, GaveUp): then a patrol from tier 3 (not after FADE; never after L11, <paramref name="allowPatrol"/> false), then Withdraw.</summary>
    void BeginGiveUpV2(bool allowPatrol)
    {
        ResetSteering();
        path.Clear();
        pathIndex = 0;
        sweep.Clear();
        sweepIndex = 0;
        sweepPlanned = -1;
        ListenPoint = null;
        SetState(HunterState.Listen);
        listenKindV2 = ListenKindV2.GiveUp;
        patrolAfterBeatV2 = allowPatrol && !fadingV2 && TierV2 != null && TierV2.patrolSeconds > 0f;
        patrolLeftV2 = patrolAfterBeatV2 ? TierV2.patrolSeconds : 0f;
        // V3b: the investigation is over: its licences go (a patrol and Withdraw keep to shut doors; Withdraw's one OPEN is its own).
        openLicenceV2.Clear();
        breakLicenceV2.Clear();
        sweepV2 = false;
        sightSearchV2 = false;
        // V4a (RELAY 7): the sense link goes with it: what it hears from here is a new noise.
        ClearLinkV2();
        GaveUp?.Invoke(position);
    }

    /// <summary>A patrol leg (RELAY 11): a random cell 4-12 cells of open walking away, W at least 21 m when planned, in its zone when one fits.</summary>
    void BeginPatrolV2(GridCoord from)
    {
        EnsurePlayerField(world.CellOf(sightFeet));
        var zone = world.ZoneOf(from).id;
        Search(from, PatrolMaxCells, false);
        candidates.Clear();
        v2Order.Clear();
        foreach (var pair in depth)
        {
            if (pair.Value < PatrolMinCells || !world.IsBuilt(pair.Key) || world.InStartArea(pair.Key) || world.InStairwell(pair.Key)) continue;
            if (WAtV2(pair.Key, world.CellCenter(pair.Key), sightFeet) < PatrolMinW) continue;
            if (world.ZoneOf(pair.Key).id == zone) candidates.Add(pair.Key);
            else v2Order.Add(pair.Key);
        }
        var pool = candidates.Count > 0 ? candidates : v2Order;
        if (pool.Count == 0)
        {
            BeginWithdrawV2();
            return;
        }
        var to = pool[(int)(Next() % (uint)pool.Count)];
        Plan(from, to, world.CellCenter(to), false);
        SetState(HunterState.Wander);
    }

    void TickPatrolV2(GridCoord myCell, float dt)
    {
        // V3b (Q2): Wander is also the sweep's patrol.
        if (sweepV2)
        {
            TickSweepV2(myCell, dt);
            return;
        }
        patrolLeftV2 -= dt;
        if (patrolLeftV2 <= 0f || fadingV2)
        {
            BeginWithdrawV2();
            return;
        }
        if (Follow(tuning.huntSpeed * WanderPace, dt) || Stalled(dt))
        {
            ResetSteering();
            BeginPatrolV2(myCell);
        }
    }

    // ---------- Senses under FADE and on the way out ----------

    /// <summary>A lock-on may fire: always, but on the way out and under FADE only within 6 m, and on the way out past the hard end never.</summary>
    bool ChaseAllowedV2(Vector3 playerFeet)
    {
        if (State != HunterState.Withdraw && !fadingV2) return true;
        if (State == HunterState.Withdraw && hardEndV2) return false;
        return Flat(position - playerFeet).magnitude <= FadeLockOnMetres;
    }

    /// <summary>A lock-on on the way out runs under FADE rules (RELAY 11), then the walk out starts again.</summary>
    void BeginChaseV2()
    {
        if (State == HunterState.Withdraw) fadingV2 = true;
    }

    // RELAY 8 (V4b): LoseTrackSeconds at every tier (never the v1 tuning's 1.5 s); RELAY 4's two backstops.
    float LoseSecondsV2 => hardEndV2 ? HardEndLoseSeconds
        : chaseLoseCapV2 >= 0f ? Mathf.Min(chaseLoseCapV2, LoseTrackSeconds) : LoseTrackSeconds;

    /// <summary>On the way out and under FADE a noise gets one cautious look at most.</summary>
    bool HearsV2() => !((State == HunterState.Withdraw || fadingV2) && cautiousUsedV2);

    /// <summary>A noise was heard: on the way out or under FADE it is the one cautious look (FADE rules: a one-spot search, no patrol after).</summary>
    void HeardV2()
    {
        if (State != HunterState.Withdraw && !fadingV2) return;
        // RELAY 11: the look on the way out counts toward lockOnCap (the look under FADE needs no count: FADE has come).
        if (State == HunterState.Withdraw) WayOutLook?.Invoke();
        cautiousUsedV2 = true;
        fadingV2 = true;
    }

    /// <summary>RELAY 5: an unbuilt cell under it is Away at once; in a chase it loses track first, and goes Away if the last-seen cell is unbuilt too. True when it went Away.</summary>
    bool UnbuiltCell()
    {
        var chasing = State == HunterState.Chase || (State == HunterState.BreakDoor && resumeState == HunterState.Chase);
        if (chasing && world.IsBuilt(world.CellOf(lastSeen)))
        {
            StopBreaking();
            LoseTrack();
            return false;
        }
        GoAwayV2(FrontRoomsRelayAwayReason.UnbuiltCell);
        return true;
    }

    // ---------- Withdraw ----------

    void BeginWithdrawV2()
    {
        ResetSteering();
        path.Clear();
        pathIndex = 0;
        sweep.Clear();
        ListenPoint = null;
        // Ruled 2026-10-30 (G6): Withdraw keeps one clock from its first entry; the one look (or a lock-on) that came between never restarts it.
        if (!withdrawBegunV2)
        {
            withdrawBegunV2 = true;
            withdrawAtV2 = clock;
            withdrawClockV2 = 0f;
            // V3b (RELAY 11): this Withdraw may OPEN one exit door.
            withdrawOpenV2 = withdrawOpenSpentV2 = false;
            withdrawOpenEdgeV2 = 0;
        }
        withdrawSeenRunV2 = withdrawWatchedForV2 = 0f;
        withdrawPickedV2 = withdrawTerminalV2 = withdrawSeenRepickedV2 = withdrawWatchedV2 = false;
        withdrawReasonV2 = FrontRoomsRelayAwayReason.ExitReached;
        // V4a (RELAY 7): no link on the way out (FADE ends a patrol with no give-up beat): half range starts clean.
        ClearLinkV2();
        SetState(HunterState.Withdraw);
        withdrawPickedV2 = PickWithdrawExitV2();
        withdrawRetryV2 = RepickRetrySeconds;
    }

    /// <summary>
    /// Ruled 2026-10-30 (G6's 25 s): the look on the way out never buys time. With the one clock past the timeout it stops where
    /// it stands, in the walk, the search or the beat, and Withdraw goes terminal from there. A chase is not a look: it ends first.
    /// </summary>
    void EndLookPastTimeoutV2()
    {
        if (!withdrawBegunV2 || LockingOn || safeDoor != SafeDoorPhase.None || clock - withdrawAtV2 < WithdrawTimeoutSeconds) return;
        if (State == HunterState.Hunt || State == HunterState.Search || State == HunterState.Listen) BeginWithdrawV2();
    }

    void TickWithdrawV2(float dt, GridCoord myCell, Vector3 playerEye)
    {
        withdrawClockV2 = clock - withdrawAtV2;
        if (PlayerSeesRelay)
        {
            withdrawSeenRunV2 += dt;
            withdrawWatchedForV2 += dt;
        }
        else
        {
            withdrawSeenRunV2 = 0f;
            withdrawSeenRepickedV2 = false;
        }
        if (withdrawWatchedForV2 >= WithdrawWatchedSeconds) withdrawWatchedV2 = true;
        // It never vanishes in view: Away only on a frame both of its points are hidden by geometry (not merely off the frustum).
        if ((withdrawTerminalV2 || hardEndV2 || withdrawWatchedV2) && OccludedV2(playerEye))
        {
            if (withdrawTerminalV2 || hardEndV2)
            {
                GoAwayV2(FrontRoomsRelayAwayReason.Timeout);
                return;
            }
            if (WarningDistance >= WithdrawWatchedMinW || DoorOrCornerBetweenV2())
            {
                GoAwayV2(FrontRoomsRelayAwayReason.Watched);
                return;
            }
        }
        // The invariant's backstop: Away within 25 s, on a frame the player does not see it.
        if (withdrawClockV2 >= WithdrawBackstopSeconds && !PlayerSeesRelay)
        {
            GoAwayV2(FrontRoomsRelayAwayReason.Timeout);
            return;
        }
        if (withdrawClockV2 >= WithdrawTimeoutSeconds && !withdrawTerminalV2) BeginTerminalV2(myCell);
        if (withdrawSeenRunV2 >= WithdrawSeenSeconds && !withdrawSeenRepickedV2 && !withdrawTerminalV2)
        {
            withdrawSeenRepickedV2 = true;
            if (RepickUnseenV2(myCell)) withdrawPickedV2 = true;
        }
        if (!withdrawPickedV2)
        {
            withdrawRetryV2 -= dt;
            if (withdrawRetryV2 <= 0f)
            {
                withdrawRetryV2 = RepickRetrySeconds;
                withdrawPickedV2 = PickWithdrawExitV2();
            }
            if (!withdrawPickedV2 && withdrawClockV2 >= WithdrawPickSeconds) BeginTerminalV2(myCell);
            if (!withdrawPickedV2) return;
        }
        var reached = Follow(tuning.huntSpeed, dt);
        var stalled = !reached && Stalled(dt);
        if (!reached && !stalled) return;
        ResetSteering();
        var atGoal = world.CellOf(position) == withdrawGoalV2 || Flat(position - goal).magnitude < MapGrid.CellSize * .5f;
        if (withdrawTerminalV2) return; // it stands there: Away on the first occluded frame
        if (!atGoal)
        {
            // A door shut across the way out, or held in place: another exit, else the terminal fallback.
            withdrawPickedV2 = PickWithdrawExitV2();
            if (!withdrawPickedV2) BeginTerminalV2(world.CellOf(position));
            return;
        }
        // At the exit: gone, unless the player is looking at it (then it waits; the watched rule or the backstop ends it).
        if (!PlayerSeesRelay) GoAwayV2(withdrawReasonV2);
    }

    /// <summary>
    /// RELAY 11's exits, in order: 1 the entry it arrived by; 2 the Relay entry farthest from the player; 3 the build edge on
    /// the far side; 4 the best unseen cell at least 10 cells of F from the player. 1-2 need every route cell over 15 m of W
    /// and 4 cells inside the built window. No shut door on any route; failing that, 1-2 with one OPEN on their route (V3b:
    /// Withdraw's one OPEN, PickWithdrawOpenV2). False: none yet.
    /// </summary>
    bool PickWithdrawExitV2()
    {
        var myCell = world.CellOf(position);
        EnsurePlayerField(world.CellOf(sightFeet));
        Search(myCell, WithdrawSearchCells, false);
        var found = false;
        var goalCell = myCell;
        var goalPoint = position;
        var reason = FrontRoomsRelayAwayReason.ExitReached;
        // 1. The entry (or cell) it arrived by.
        if (hasArriveCellV2 && arriveCellV2 != myCell && depth.ContainsKey(arriveCellV2) && RouteOffV2(arriveCellV2, myCell))
        {
            goalCell = arriveCellV2;
            goalPoint = arriveFeetV2;
            found = true;
        }
        // 2. The Relay entry farthest from the player (by W).
        if (!found)
        {
            world.RelayEntries(entries);
            var bestW = -1f;
            for (var k = 0; k < entries.Count; k++)
            {
                var c = world.CellOf(entries[k].pos);
                if (c == myCell || !depth.ContainsKey(c) || IsArriveExcluded(c) || world.InStartArea(c) || world.InStairwell(c)) continue;
                var feet = EntryFeet(k, c);
                var w = WAtV2(c, feet, sightFeet);
                if (w <= bestW || !RouteOffV2(c, myCell)) continue;
                bestW = w;
                goalCell = c;
                goalPoint = feet;
                found = true;
            }
        }
        // 1-2 again, "failing that, with one OPEN on its own route" (V3b): at most one shut door it may open on the way.
        if (!found && PickWithdrawOpenV2(myCell, out var openCell, out var openPoint))
        {
            goalCell = openCell;
            goalPoint = openPoint;
            found = true;
        }
        // Exits 3 and 4 read the search with no shut doors (the pass above used its own).
        if (!found) Search(myCell, WithdrawSearchCells, false);
        // 3. The build edge on the far side of it from the player.
        if (!found)
        {
            var away = Flat(position - sightFeet);
            var best = -1f;
            foreach (var pair in depth)
            {
                var c = pair.Key;
                if (pair.Value < 1 || !EdgeCellV2(c)) continue;
                var p = world.CellCenter(c);
                if (Vector3.Dot(Flat(p - sightFeet), away) <= 0f) continue;
                var far = Flat(p - sightFeet).sqrMagnitude;
                if (far <= best) continue;
                best = far;
                goalCell = c;
                goalPoint = p;
                found = true;
                reason = FrontRoomsRelayAwayReason.BuildEdge;
            }
        }
        // 4. The best unseen cell at least 10 cells of F from the player.
        if (!found && BestUnseenV2(myCell, WithdrawUnseenCells * MapGrid.CellSize, false, out var unseen))
        {
            goalCell = unseen;
            goalPoint = world.CellCenter(unseen);
            found = true;
        }
        if (!found) return false;
        withdrawGoalV2 = goalCell;
        withdrawReasonV2 = reason;
        Plan(myCell, goalCell, goalPoint, false);
        return true;
    }

    /// <summary>The terminal fallback (RELAY 11): walk to the reachable cell with the largest F the player cannot see; Away on the first frame it is occluded by geometry.</summary>
    void BeginTerminalV2(GridCoord myCell)
    {
        withdrawTerminalV2 = true;
        withdrawPickedV2 = true;
        withdrawReasonV2 = FrontRoomsRelayAwayReason.Timeout;
        EnsurePlayerField(world.CellOf(sightFeet));
        Search(myCell, WithdrawSearchCells, false);
        if (BestUnseenV2(myCell, 0f, false, out var cell))
        {
            withdrawGoalV2 = cell;
            Plan(myCell, cell, world.CellCenter(cell), false);
        }
        else
        {
            // Nowhere unseen to go: it stands; the first occluded frame ends it.
            withdrawGoalV2 = myCell;
            path.Clear();
            pathIndex = 0;
            goal = position;
        }
    }

    /// <summary>Seen 2 s on the way out (RELAY 11): the nearest cell that is unseen and farther from the player than it stands.</summary>
    bool RepickUnseenV2(GridCoord myCell)
    {
        EnsurePlayerField(world.CellOf(sightFeet));
        Search(myCell, WithdrawRepickCells, false);
        if (!BestUnseenV2(myCell, WAtV2(myCell, position, sightFeet) + MapGrid.CellSize, true, out var cell)) return false;
        withdrawGoalV2 = cell;
        withdrawReasonV2 = FrontRoomsRelayAwayReason.ExitReached;
        Plan(myCell, cell, world.CellCenter(cell), false);
        return true;
    }

    /// <summary>
    /// Over the last Search: a cell the player cannot see (its eye and head points at the cell's middle) with W at least
    /// <paramref name="minW"/>; the nearest by walking when <paramref name="nearest"/>, else the largest F (past the field's
    /// reach first, then the farthest straight). At most WithdrawUnseenTests sight tests.
    /// </summary>
    bool BestUnseenV2(GridCoord myCell, float minW, bool nearest, out GridCoord cell)
    {
        cell = myCell;
        v2Order.Clear();
        v2Key.Clear();
        foreach (var pair in depth)
        {
            var c = pair.Key;
            if (pair.Value < 2 || !world.IsBuilt(c) || IsArriveExcluded(c) || world.InStartArea(c) || world.InStairwell(c)) continue;
            var p = world.CellCenter(c);
            var w = WAtV2(c, p, sightFeet);
            if (w < minW) continue;
            var f = playerField.DistanceTo(c);
            v2Order.Add(c);
            v2Key.Add(nearest ? pair.Value : -(float.IsPositiveInfinity(f) ? 1000f + Flat(p - sightFeet).magnitude : f));
        }
        if (v2Order.Count == 0) return false;
        v2Pick.Clear();
        for (var i = 0; i < v2Order.Count; i++) v2Pick.Add(i);
        v2Pick.Sort((a, b) => v2Key[a].CompareTo(v2Key[b]));
        for (var i = 0; i < v2Pick.Count && i < WithdrawUnseenTests; i++)
        {
            var c = v2Order[v2Pick[i]];
            var p = world.CellCenter(c);
            if (VisibleToPlayer(sightEye, p + Vector3.up * RelayLowPoint) || VisibleToPlayer(sightEye, p + Vector3.up * RelayHighPoint)) continue;
            cell = c;
            return true;
        }
        return false;
    }

    // Exits 1-2's route rule: from the exit back to where it stands, every cell (its own excepted) over 15 m of W and 4 cells inside the built window.
    bool RouteOffV2(GridCoord exit, GridCoord myCell)
    {
        for (var c = exit; c != myCell; )
        {
            if (WAtV2(c, world.CellCenter(c), sightFeet) <= WithdrawRouteMinW || !InsideBuiltV2(c, WithdrawInsideCells)) return false;
            if (!cameFrom.TryGetValue(c, out var back)) return false;
            c = back;
        }
        return true;
    }

    // A build edge: a built map cell with an unbuilt neighbour that is not the start area or X (those are walled off, not the edge).
    bool EdgeCellV2(GridCoord c)
    {
        if (!world.IsBuilt(c) || world.InStartArea(c) || world.InStairwell(c) || IsArriveExcluded(c)) return false;
        foreach (var s in Steps)
        {
            var n = c + s;
            if (!world.IsBuilt(n) && !world.InStartArea(n) && !world.InExitArea(n)) return true;
        }
        return false;
    }

    /// <summary>Both of its points (1.6 m and 1.95 m) hidden from the player's eye by geometry (RELAY 11: not merely off the frustum).</summary>
    bool OccludedV2(Vector3 playerEye) =>
        !VisibleToPlayer(playerEye, position + Vector3.up * RelayLowPoint) && !VisibleToPlayer(playerEye, position + Vector3.up * RelayHighPoint);

    // RELAY 11's "a door or corner lies between": an opening on F's path, F past the field's reach, or F well longer than the straight line.
    bool DoorOrCornerBetweenV2()
    {
        if (float.IsPositiveInfinity(PathDistanceToPlayer)) return true;
        if (PathOpeningCost > 0f && !float.IsPositiveInfinity(PathOpeningCost)) return true;
        return PathDistanceToPlayer > CornerPathRatio * Flat(position - sightFeet).magnitude + MapGrid.CellSize;
    }

    // ---------- Away ----------

    /// <summary>Off the map (RELAY 4, 11): hidden, stage 0, everything of the encounter dropped; Withdrew (from the map) and WentAway.</summary>
    void GoAwayV2(FrontRoomsRelayAwayReason reason)
    {
        var at = position;
        var fromMap = OnMap;
        StopBreaking();
        breakSeconds = -1f;
        blowsStruck = 0;
        BlowIndex = -1;
        path.Clear();
        pathIndex = 0;
        planThroughDoors = true;
        ResetSteering();
        sweep.Clear();
        sweepIndex = 0;
        sweepPlanned = -1;
        lookTimer = 0f;
        searchTime = 0f;
        ListenPoint = null;
        SeesPlayer = false;
        sawLastTick = false;
        seenCell = default;
        lastSeenTime = float.MinValue;
        lostTime = replanTimer = 0f;
        Moving = false;
        DoorSqueeze = 0f;
        safeDoor = SafeDoorPhase.None;
        ResetWarningMetrics();
        PlayerSeesRelay = RelayLit = false;
        playerSeesRelayTimer = 0f;
        sightTick++;
        ResetEncounterV2();
        LastAwayReason = reason;
        if (rig != null) rig.gameObject.SetActive(false);
        SetState(HunterState.Away);
        // Its clock stands while it is off the map: the holds of this encounter must not outlive it.
        ClearThreatHolds();
        if (fromMap) Withdrew?.Invoke(at);
        WentAway?.Invoke(reason);
    }

    /// <summary>The encounter's state: the call, Arrive, FADE, the beat, the patrol and Withdraw (Park, DebugPlace, Away).</summary>
    void ResetEncounterV2()
    {
        listenKindV2 = ListenKindV2.Plain;
        callV2 = null;
        hasEstimateV2 = false;
        arriveClockV2 = arriveDelayV2 = repickClockV2 = repickRetryV2 = 0f;
        repickingV2 = firstSendArriveV2 = false;
        hasArriveCellV2 = false;
        fadingV2 = hardEndV2 = cautiousUsedV2 = patrolAfterBeatV2 = false;
        chaseLoseCapV2 = -1f;
        patrolLeftV2 = 0f;
        withdrawPickedV2 = withdrawTerminalV2 = withdrawSeenRepickedV2 = withdrawWatchedV2 = withdrawBegunV2 = false;
        withdrawClockV2 = withdrawRetryV2 = withdrawSeenRunV2 = withdrawWatchedForV2 = 0f;
        // V3b: the licences, a door act, a pushed leaf (it settles), the sweep.
        ResetDoorsV3b();
        // V4a: the sense link, its look, the break's slot, the door heard since sight.
        ResetSenseV4a();
        // V4b: the meter, the gate's hold, the lock-on, the head.
        ResetSightV2();
    }
}
#endif
