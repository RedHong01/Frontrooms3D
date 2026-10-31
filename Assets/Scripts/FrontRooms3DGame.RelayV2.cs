#if FRONTROOMS_RELAY_V2
using System.Globalization;
using FrontRooms.Map;
using UnityEngine;

// Relay v2's game side (RELAY_PURSUIT_REDESIGN sections 4-6; step V3a), compiled only with FRONTROOMS_RELAY_V2. Without the
// define (main's default) none of this exists and the game's v1 release, leash and park run as before.
// - The director (FrontRoomsRelayDirector) is made with the hunter at the run's start and ticked before it.
// - It arms at each floor's start-door shut (systems design P2), never while hunterTuning.releaseDelaySeconds is at least
//   FrontRoomsRelayDirector.NeverArmDelaySeconds (the tools shim: the EXAMINE and glass RT harnesses set 1e6 and place the
//   Relay themselves). On floor 1 with a station its first call is held until the onboarding's gate (Red 2026-10-30: 150
//   cells explored or 180 s; StationGateNow). In a station run the run's first dispatch, on whichever floor it comes, is
//   placed through FirstSend (C32; the generic ladder below floor 1) and carries the first send's grace.
// - Disturb: a sprint step +3, a player door move +1 (not the swing after an unlock), a key unlock +1; handheld walking
//   is 0 (Mobile.cs is not touched: it never reports). Glass and the station take are devices. The stairwells are
//   sanctuaries (gains 0, the dry clock paused).
// - The autopilot (editor): a debug Summon AutoSummonSeconds after Armed (unless floor 1's first call is held); PASS
//   needs the Relay on the map once; the EXIT scenario checks "armed at the shut, on the map within the first dispatch".
// - Step V3b: the station take goes only to the director (it re-dispatches the Relay on the map, or queues the call; v1's
//   HuntStation is not called), and the logs and the autopilot report carry the door outcomes (OPEN, push, break, WALL)
//   and the on-map device calls (re-dispatches, handoffs, sweeps).
// - Step V3c: a trip in a chase or a break is heard and queued (out at the next Away); the first send's RELAY lines carry
//   W at the placement, and its first warning sample (at the step-in) is logged (FirstSendSample, C32's W fix).
// - Step V4a: noises reach the hunter at the v2 loudness (RelayNoise; hearing is the tier's, in the hunter): sprint 27,
//   glass 60, the station take as a device 45, the door noise at one loudness until V5 split it by verb (below). The
//   walk (9 m, the Walk gait) was already wired in Mobile.cs and stays off the director: Attention 0. Step V6 adds the sash
//   pry: loudness 8 and Attention +8 as the sash gives (OnSashPried, FrontRooms3DGame.cs).
// - Step V4b: each lock-on with its distance and hold (C37), each chase's start distance (G5) and each G3 gate hold go to the log; the hunter's latch
//   drives the warning lamps (FrontRooms3DGame.cs), and ChaseGrace is no longer read (the game still holds it for its rows).
// - Step V5 (RELAY 10): the door verbs. The fast test is input only: sprinting, or within FastAfterSprintSeconds of the last
//   sprint frame (never the pull step or the title glide; on a phone the Run gait); the map reads it (map.PlayerFast) for
//   every player open (BARGE) and shut (SLAM), both in FastSwing.SwingSeconds, else EASE; the swing lock is on
//   (map.SwingLock). Each door move is heard at its verb's loudness (ease 6, barge 18, slam 27, the unlock's open 6, the
//   map's own as an ease) and adds its Attention with its source (ease +1, barge +8, slam +12; the unlock's +1 came at the
//   key's turn; the map's and the Relay's never count). A locked door's rattle: 6 m, +1. Steps: stepTime restarts with a
//   sprint, and a step is heard (noise, Attention) only HeardStepMetres or more from the last heard one.
public sealed partial class FrontRooms3DGame
{
    FrontRoomsRelayDirector director;
    // When (elapsed) and on which floor the director last armed; -1 / 0 before.
    float relayArmedAt = -1f;
    int relayArmedFloor;
    // V5: seconds since the last sprint frame (the fast test), and where the last heard step was (none yet: the next is heard).
    float relaySinceSprint = NoSprintYet;
    Vector3 relayHeardStepAt;
    bool relayHeardStep;

    /// <summary>V5 (RELAY 10's fast test): a door verb is fast while sprinting or within this many seconds of the last sprint frame.</summary>
    public const float FastAfterSprintSeconds = .5f;
    /// <summary>V5 (RELAY 10, stamina row): a step is heard (noise, Attention) only this far (flat) from the last heard one.</summary>
    public const float HeardStepMetres = .5f;
    // Seconds since the last sprint frame before any (and the most it counts to).
    const float NoSprintYet = 1e6f;

    /// <summary>Tools: this run's Relay v2 director (null before a map run).</summary>
    public FrontRoomsRelayDirector RelayDirector => director;

    void StartRelayDirector()
    {
        relay.TierV2 = TierRules.V2At(tier);
        director = new FrontRoomsRelayDirector(relay, TierRules.DirectorRules(), tier);
        relayArmedAt = -1f;
        relayArmedFloor = 0;
        // V5 (RELAY 10): the swing lock, and the fast test fed every frame of map play (RelayGaitV5).
        relaySinceSprint = NoSprintYet;
        relayHeardStep = false;
        map.SwingLock = true;
        map.PlayerFast = false;
        director.PhaseChanged += p => Event("director", p.ToString());
        director.Called += (cause, explicitCall, at) => Event("called", cause + (explicitCall ? " (explicit)" : "") + " " + Flat(at));
        director.Dispatched += OnRelayDispatched;
        director.CallDropped += c =>
        {
            Event("call dropped", c.Cause.ToString());
            Log("RELAY - call dropped after PENDING (" + c.Cause + " to " + Flat(c.Estimate) + "): " + relay.LastArriveReport);
        };
        director.FadeBegan += r => Event("fade", r.ToString());
        director.Away += r =>
        {
            Event("relay away", r.ToString());
            Log("RELAY - away (" + r + ") - RELAX " + director.Policy.RelaxRemaining.ToString("0", CultureInfo.InvariantCulture) + " s, " + director.Calls + " calls, "
                + director.Drops + " dropped this run - " + RelayDoorTally() + " - " + relay.ChaseNumbers);
        };
        // V3b: on-map device calls (a walk or the handoff), and a queued trip's call going out (the CALLED tick).
        director.Redispatched += (c, handoff) =>
        {
            Event("redispatch", (handoff ? "handoff " : "walk ") + Flat(c.Estimate));
            Log("RELAY - " + (handoff ? "handoff (straight to Arrive)" : "re-dispatch on foot") + " for a device at " + Flat(c.Estimate) + " - " + director.Policy.Redispatches
                + " of the encounter's budget spent - " + (handoff ? relay.LastArriveReport : "the Relay " + relay.State + ", " + relay.DoorLicences));
        };
        director.QueuedCallOut += p => Event("called", "queued device " + Flat(p));
        relay.GaveUp += p => Event("gave up", Flat(p).ToString());
        // V3b's doors: OPEN (latch, 1.0 s to Ajar), the 0.4 s push through an Ajar door, a WALL (no licence, no way round).
        relay.RelayDoorOpened += p => Event("relay door", "OPEN " + Flat(p));
        relay.RelayDoorPushed += p => Event("relay door", "push " + Flat(p));
        relay.DoorWalled += p =>
        {
            Event("relay door", "WALL " + Flat(p));
            Log("RELAY - WALL at the door " + Flat(p) + " (" + relay.State + "): no licence and no way round within " + FrontRoomsMapHunter.WallReplanRatio.ToString("0.0", CultureInfo.InvariantCulture)
                + "x - it listens " + FrontRoomsMapHunter.WallListenSeconds.ToString("0", CultureInfo.InvariantCulture) + " s - " + RelayDoorTally());
        };
        // V4b: every lock-on, chase start and gate hold is logged (RELAY 8, section 14's G3 and G5, Red's C37).
        relay.TargetAcquired += OnRelayTargetAcquired;
        relay.ChaseStarted += metres =>
        {
            Event("chase", metres.ToString("0.0", CultureInfo.InvariantCulture) + " m");
            Log("RELAY - chase started at " + metres.ToString("0.0", CultureInfo.InvariantCulture) + " m, chase " + relay.ChaseStartMetres.Count + " this run, speed "
                + relay.ChaseSpeed.ToString("0.0", CultureInfo.InvariantCulture) + " m/s, catch " + relayTuning.catchDistance.ToString("0.0", CultureInfo.InvariantCulture) + " m");
        };
        relay.GateHoldLogged += line =>
        {
            Event("gate hold", line);
            Log("RELAY - G3 gate hold " + relay.GateHolds + " this run: " + line);
        };
        relay.AlmostSeen += p => Event("almost", Flat(p).ToString());
        map.DoorUnlocked += OnRelayDoorUnlocked;
    }

    void OnRelayTargetAcquired(Vector3 at)
    {
        var hold = relay.LastLockOnHoldSeconds.ToString("0.00", CultureInfo.InvariantCulture);
        Event("lock-on", (relay.LastLockOnReacquired ? "again " : "") + relay.LastLockOnMetres.ToString("0.0", CultureInfo.InvariantCulture) + " m, hold " + hold + " s");
        Log("RELAY - lock-on" + (relay.LastLockOnReacquired ? " (re-acquired)" : "") + (relay.LastLockOnTouch ? " (touch)" : "") + " at " + relay.LastLockOnMetres.ToString("0.0", CultureInfo.InvariantCulture)
            + " m, hold " + hold + " s" + (relay.LastLockOnLongHold ? " (long, " + relay.LongLockHolds + " this run)" : "") + ", stage " + relay.LastLockOnStage + ", runs " + relay.LastLockOnStage1Seconds.ToString("0.00", CultureInfo.InvariantCulture) + " / " + relay.LastLockOnStage2Seconds.ToString("0.00", CultureInfo.InvariantCulture)
            + " s, " + relay.State + ", " + relay.GateHolds + " gate holds this run");
    }

    // The floor's start door has fully shut (floor 1: the title's door, or no stream rooms; below it: the stairwell's north door).
    bool RelayStartDoorShut => floor <= 1 ? streamFade >= 0f || roomStream == null : floorStartShut;

    /// <summary>Each frame of map play, where v1 released and ticked the Relay: arm, the floor-1 gate, the director, then the hunter.</summary>
    void UpdateRelayV2(float dt)
    {
        var feet = playerRoot.position;
        var eye = rig != null ? rig.BaseEye.position : cam.transform.position;
        var forward = rig != null ? rig.BaseEye.rotation * Vector3.forward : cam.transform.forward;
        UpdateRelayTuning();
        relay.TierV2 = TierRules.V2At(tier);
        if (!director.Armed && RelayStartDoorShut && hunterTuning.releaseDelaySeconds < FrontRoomsRelayDirector.NeverArmDelaySeconds) ArmRelay();
        // The first send's grace (Red 2026-10-30: the run's first send wherever it comes): no chase before the first W1 burst
        // after it unless the player is within 6 m.
        UpdateFirstSendGrace();
        if (director.Armed)
        {
            if (director.FirstCallHeld && floor == 1 && stationPlan != null) UpdateStationGateV2(feet);
            director.Tick(dt, tier, map.InStairwell(map.CellOf(feet)), feet, eye, forward);
        }
        // Armed by the director, or placed on the map by tools (DebugPlace).
        if (!relay.Armed) return;
        // The Relay sees the player's base eye, never a shot or shake pose (RELAY 8, V2).
        relay.Tick(dt, feet, eye, forward);
        // C32's W fix: the run's first send's first warning sample (at its step-in), one RELAY line.
        FirstSendSample();
        // The hunter's Threat reading frees the player from any shot that allows it, at once (V2).
        if (rig != null && relay.Threat.Any) rig.CancelForRelay();
    }

    void ArmRelay()
    {
        var hold = floor == 1 && stationPlan != null;
        // The onboarding (Red 2026-10-30): a station run's first dispatch is gentle wherever it comes (the director keeps
        // the run-level FirstSendDone, so a later floor's Arm makes no FirstSend once it is done).
        director.Arm(hold, stationRun);
        relayArmedAt = elapsed;
        relayArmedFloor = floor;
        Event("relay", hold ? "armed, first call held" : "armed");
        Log("RELAY - armed at the start door's shut - floor " + floor + ", tier " + tier + ", " + elapsed.ToString("0.0", CultureInfo.InvariantCulture) + " s - RELAX "
            + director.Policy.RelaxRemaining.ToString("0", CultureInfo.InvariantCulture) + " s" + (hold ? " - first call held for the onboarding's gate (" + FrontRoomsMapHunter.StationExploreCells
            + " cells explored or " + FallbackSecondsNow.ToString("0", CultureInfo.InvariantCulture) + " s)" : "") + (director.FirstSendNext ? " - the run's first dispatch is gentle (FirstSend)" : ""));
    }

    /// <summary>
    /// A26 M7 under the director (V3a; the onboarding, Red 2026-10-30): floor 1's first call waits for the onboarding's gate
    /// (StationGateNow: 150 cells of the floor explored, or 180 s since the start door shut; never the key, never a zone
    /// count). Once due (it latches) the hold lifts: a device tripped meanwhile (glass, the station take), queued, goes out;
    /// with none a Gate call to the station's take point or spot. Placed through FirstSend (OnRelayDispatched). Taking the
    /// key while held only queues its device call.
    /// </summary>
    void UpdateStationGateV2(Vector3 feet)
    {
        if (stationSendDue) return;
        var cell = map.CellOf(feet);
        var why = StationGateNow(cell);
        if (why == FrontRoomsMapHunter.StationGate.Waiting) return;
        LatchStationGate(why, feet, cell);
        var estimate = map.StationKeyIsTaken ? map.StationTakePoint : map.StationTakeSpot(out var spot) ? spot : feet;
        var released = director.ReleaseFirstCall(estimate);
        Log("RELAY - floor 1's gate (onboarding) - " + StationGateName(why) + " at " + elapsed.ToString("0.0", CultureInfo.InvariantCulture) + " s, " + stationSendExplored
            + " cells explored, the player " + stationSendPath + " path cells out, key " + (map.StationKeyIsTaken ? "taken" : "not taken") + " - first call "
            + (released ? director.Phase.ToString() : "NOT released"));
    }

    void OnRelayDispatched(FrontRoomsRelayCall call, FrontRoomsRelayPlacement placement)
    {
        Event("dispatch", call.Cause + " " + placement);
        if (placement != FrontRoomsRelayPlacement.FirstSend)
        {
            Log("RELAY - dispatched (" + call.Cause + ", " + placement + ") - " + relay.LastArriveReport);
            return;
        }
        // The run's first send (the onboarding, Red 2026-10-30): its grace counts W1 bursts from here.
        FirstSendPlaced();
        var s = relay.LastFirstSend;
        if (floor == 1 && stationPlan != null)
        {
            stationSentAt = elapsed;
#if UNITY_EDITOR
            autoStationLeg = relay.State;
            autoStationLegPoint = relay.ArriveHuntPoint;
#endif
            Log("RELAY - first send (M7, C32) - gate " + StationGateName(stationSendWhy) + " at " + stationSendAt.ToString("0.0", CultureInfo.InvariantCulture) + " s, placed "
                + (stationSentAt - stationSendAt).ToString("0.00", CultureInfo.InvariantCulture) + " s later (" + call.Cause + ") - rung " + s.rung + " (" + s.rung1 + "/" + s.rung2 + "/" + s.rung3
                + " cells), " + s.cell + ", " + s.cells + " walking cells, " + s.straight.ToString("0.0", CultureInfo.InvariantCulture) + " m straight, " + FirstSendWText(s) + ", " + FirstSendSide(s)
                + (s.unseen ? ", unseen" : ", SEEN") + " - steps in after " + relay.ArriveSecondsLeft.ToString("0.00", CultureInfo.InvariantCulture) + " s, then hunts to "
                + (s.hunt ? "the take point" : "the gate's estimate"));
#if UNITY_EDITOR
            // The station's row at the placement (the step-in comes the arrive delay later; the floor may be left before it).
            if (autopilot && autoStationPlanned && !autoStationSent) AutoStationSend(stationPlan.layout);
#endif
            return;
        }
        Log("RELAY - the run's first send on floor " + floor + " (" + FloorLabel(floor) + "; none on floor 1: left before the onboarding's gate) - " + call.Cause + " - rung " + s.rung
            + " (" + s.rung1 + "/" + s.rung2 + "/" + s.rung3 + " cells), " + s.cell + ", " + s.cells + " walking cells, " + s.straight.ToString("0.0", CultureInfo.InvariantCulture)
            + " m straight, " + FirstSendWText(s) + ", " + FirstSendSide(s) + (s.unseen ? ", unseen" : ", SEEN") + " - steps in after " + relay.ArriveSecondsLeft.ToString("0.00", CultureInfo.InvariantCulture)
            + " s, then hunts to the call's estimate; grace " + (relay.ChaseGrace ? "on" : "off") + FirstSendWalkText(s));
#if UNITY_EDITOR
        if (autopilot && autoStationPlanned) AutoFirstSendBelowRow();
#endif
    }

    // V3b: the run's door outcomes and on-map device calls so far, for the logs.
    string RelayDoorTally() => "doors " + relay.DoorsOpened + " OPENed, " + relay.DoorsPushed + " pushed, " + relay.DoorsBroken + " broken, " + relay.DoorsWalled + " WALLed; "
        + director.Redispatches + " re-dispatches (" + director.Handoffs + " handoffs), " + director.Sweeps + " sweeps, " + director.DevicesHeardOnly + " trips in a chase or a break (heard and queued), "
        + director.DevicesQueuedOnMap + " queued on the map; heard re-targets " + relay.HeardRetargets + " this encounter, " + relay.HeardRetargetsTotal + " this run";

    // ---------- V4a: what each noise carries (RELAY 7, 10) ----------

    // V5: the door noise is its verb's (Ease, Barge, Slam, Unlock, Rattle); V4a's one door loudness (doorNoiseRadius) is gone.
    enum RelayNoiseKind : byte { Glass, Sprint, Device, Pry, Ease, Barge, Slam, Unlock, Rattle }

    float RelayLoudness(RelayNoiseKind kind)
    {
        switch (kind)
        {
            case RelayNoiseKind.Glass: return hunterTuning.glassLoudness;
            case RelayNoiseKind.Sprint: return hunterTuning.sprintLoudness;
            case RelayNoiseKind.Pry: return hunterTuning.pryLoudness;
            case RelayNoiseKind.Ease: return hunterTuning.easeLoudness;
            case RelayNoiseKind.Barge: return hunterTuning.bargeLoudness;
            case RelayNoiseKind.Slam: return hunterTuning.slamLoudness;
            case RelayNoiseKind.Unlock: return hunterTuning.unlockLoudness;
            case RelayNoiseKind.Rattle: return hunterTuning.rattleLoudness;
            default: return hunterTuning.deviceLoudness;
        }
    }

    static bool IsDoorNoise(RelayNoiseKind kind) => kind >= RelayNoiseKind.Ease;

    // A noise the Relay may hear, at the v2 loudness (the hunter multiplies by the tier's hearing). The walk is not here: it
    // reaches relay.Noise from Mobile.cs's WalkStepNoise at the same 9 m, the Walk gait only, and never reaches the director.
    void RelayNoise(RelayNoiseKind kind, Vector3 at)
    {
#if UNITY_EDITOR
        AutopilotNoise(IsDoorNoise(kind) ? AutoCauseDoor : kind == RelayNoiseKind.Glass ? AutoCauseGlass : kind == RelayNoiseKind.Sprint ? AutoCauseSprint : AutoCauseOther, at, RelayLoudness(kind));
#else
        relay?.Noise(at, RelayLoudness(kind));
#endif
    }

    // RELAY 6: a disturbance, at a point, by the player unless said (V5: Disturb carries its source; only Player counts);
    // nothing inside a sanctuary.
    void RelayDisturb(FrontRoomsRelayDisturbanceKind kind, Vector3 at, FrontRoomsRelayDisturbanceSource source = FrontRoomsRelayDisturbanceSource.Player)
    {
        if (director == null || map == null) return;
        director.Disturb(kind, at, source, map.InStairwell(map.CellOf(playerRoot != null ? playerRoot.position : at)));
    }

    // ---------- V5: the door verbs and the steps (RELAY 10) ----------

    /// <summary>
    /// A door's leaf started to move (DoorMoved; the map's LastSwingKind is its verb): heard at the verb's loudness, and the
    /// player's verbs add their Attention. EASE 6 m +1; BARGE 18 m +8; SLAM 27 m +12; the open after a key turn 6 m (its +1
    /// came at the turn, OnRelayDoorUnlocked); the map's own move (World: the EXIT's seal) is heard as an ease and its
    /// Disturb, source World, never counts; the Relay's own moves raise no DoorMoved.
    /// </summary>
    void RelayDoorMoved(Vector3 at)
    {
        var verb = map.LastSwingKind;
        Event("door verb", verb + " " + Flat(at));
        switch (verb)
        {
            case FrontRoomsMapWorld.DoorSwingKind.Barge:
                RelayNoise(RelayNoiseKind.Barge, at);
                RelayDisturb(FrontRoomsRelayDisturbanceKind.Barge, at);
                break;
            case FrontRoomsMapWorld.DoorSwingKind.Slam:
                RelayNoise(RelayNoiseKind.Slam, at);
                RelayDisturb(FrontRoomsRelayDisturbanceKind.Slam, at);
                break;
            case FrontRoomsMapWorld.DoorSwingKind.Unlock:
                RelayNoise(RelayNoiseKind.Unlock, at);
                break;
            case FrontRoomsMapWorld.DoorSwingKind.World:
                RelayNoise(RelayNoiseKind.Ease, at);
                RelayDisturb(FrontRoomsRelayDisturbanceKind.EaseDoor, at, FrontRoomsRelayDisturbanceSource.World);
                break;
            case FrontRoomsMapWorld.DoorSwingKind.Relay:
                break;
            default:
                RelayNoise(RelayNoiseKind.Ease, at);
                RelayDisturb(FrontRoomsRelayDisturbanceKind.EaseDoor, at);
                break;
        }
    }

    // A locked door tried (DoorRattled, at the lock on the trier's face): 6 m, +1 (RELAY 10, decided 2026-10-08).
    void RelayDoorRattled(Vector3 lockPoint)
    {
        RelayNoise(RelayNoiseKind.Rattle, lockPoint);
        RelayDisturb(FrontRoomsRelayDisturbanceKind.Rattle, lockPoint);
    }

    /// <summary>
    /// Each frame of map play, after the gait and before PlayerSprinting takes this frame's value: a sprint's start restarts
    /// stepTime (so its first step comes 0.3 s in, never at once), and the fast test goes to the map (PlayerFast):
    /// sprinting, or within FastAfterSprintSeconds of the last sprint frame. Input only: the pull step and the title glide
    /// never sprint.
    /// </summary>
    void RelayGaitV5(float dt, bool sprinting)
    {
        if (sprinting)
        {
            if (!PlayerSprinting) stepTime = 0f;
            relaySinceSprint = 0f;
        }
        else relaySinceSprint = Mathf.Min(relaySinceSprint + dt, NoSprintYet);
        if (map != null) map.PlayerFast = relaySinceSprint <= FastAfterSprintSeconds;
    }

    // A step's noise and Attention (RELAY 10: "needs 0.5 m of movement since the last"): only HeardStepMetres (flat) or more
    // from the last heard step, so sprinting into a wall makes none; the Foley step plays either way.
    bool RelayStepHeard(Vector3 feet)
    {
        if (relayHeardStep && (Flat(feet) - Flat(relayHeardStepAt)).sqrMagnitude < HeardStepMetres * HeardStepMetres) return false;
        relayHeardStep = true;
        relayHeardStepAt = feet;
        return true;
    }

    /// <summary>
    /// Tools (V5; DebugPlaceOnMap under the define): the director's DebugPlaceOnMap, facing the player: armed if not yet,
    /// a Relay out goes Away first, then on the map at <paramref name="feet"/> in the step-in's Listen, its encounter begun.
    /// </summary>
    bool RelayDebugPlaceOnMap(Vector3 feet)
    {
        if (director == null || relay == null) return false;
        var toPlayer = playerRoot != null ? playerRoot.position - feet : Vector3.zero;
        var placed = director.DebugPlaceOnMap(feet, toPlayer);
        Log("RELAY - tools: DebugPlaceOnMap at " + Flat(feet) + ": " + (placed ? relay.State + ", " + relay.LastArriveReport : "NOT placed (" + relay.State + ", " + director.Phase + ")"));
        return placed;
    }

    // A key unlock: +1 at the keyhole (the swing it starts is not counted again).
    void OnRelayDoorUnlocked(FrontRoomsMapWorld.Door door, GridCoord zone, Vector3 keyhole) =>
        RelayDisturb(FrontRoomsRelayDisturbanceKind.Unlock, keyhole);

#if UNITY_EDITOR
    // ---------- The autopilot under the define ----------

    /// <summary>V3a: the debug Summon comes this long after Armed, once a floor, while the Relay is Away and no first call is held.</summary>
    const float AutoSummonSeconds = 20f;
    /// <summary>The EXIT scenario's limit from a floor's shut to the Relay on the map: the Summon, PENDING's 10 s, the arrive delay and a margin.</summary>
    const float AutoOnMapLimitSeconds = AutoSummonSeconds + 10f + 3f + 7f;
    // When (play seconds) the Relay was first on the map this run (-1: never), the floor of the last Summon, and the EXIT row's floor.
    float autoV2OnMapAt = -1f;
    int autoV2SummonFloor, autoV2ExitArmFloor, autoV2ExitCallsAtShut;

    /// <summary>Each autopilot frame (from AutopilotTick): the Summon, and the latch PASS reads.</summary>
    void AutoRelayV2Tick()
    {
        if (relay == null || director == null) return;
        if (relay.OnMap && autoV2OnMapAt < 0f)
        {
            autoV2OnMapAt = autoPlayClock;
            Log("AUTOPILOT RELAY v2 - on the map at " + AutoF(autoPlayClock, "0.0") + " s (" + relay.State + ", " + director.Calls + " calls: " + (director.LastCall != null ? director.LastCall.Cause + ", " + director.LastPlacement : "none") + ")");
        }
        // The Relay baselines drive the director themselves (level-design research's v2 fields). A Relay held for the door scenario is
        // placed all the same and steps in once let go (Dispatch is not held; its Arrive tick is).
        if (autoBaseline || !director.Armed || director.FirstCallHeld || relayArmedFloor != floor || autoV2SummonFloor == floor) return;
        if (elapsed - relayArmedAt < AutoSummonSeconds || relay.State != HunterState.Away || director.Phase == FrontRoomsRelayDirectorPhase.Pending) return;
        autoV2SummonFloor = floor;
        var summoned = director.Summon(playerRoot.position);
        Log("AUTOPILOT RELAY v2 - debug Summon " + AutoF(elapsed - relayArmedAt, "0.0") + " s after Armed on floor " + floor + ": " + (summoned ? "pending" : "refused (" + director.Phase + ")"));
    }

    /// <summary>
    /// The EXIT scenario's last step on a floor below the first (V3a): the director armed at the start door's shut, and the
    /// Relay on the map within the first dispatch (the debug Summon's) and AutoOnMapLimitSeconds; then the floor's row.
    /// </summary>
    void AutoExitReleaseV2()
    {
        if (relay == null || director == null) { AutoExitFail("floor " + floor + ": no Relay or director"); return; }
        if (autoV2ExitArmFloor != floor)
        {
            autoV2ExitArmFloor = floor;
            autoV2ExitCallsAtShut = director.Calls;
            var armedAtShut = director.Armed && relayArmedFloor == floor && relayArmedAt >= 0f && elapsed - relayArmedAt < 1f;
            AutoExitCheck(armedAtShut, "floor " + floor + ": the director armed at the start door's shut (" + (relayArmedFloor == floor ? AutoF(elapsed - relayArmedAt) + " s before this check" : "NOT on this floor")
                + "), " + director.Phase + " " + AutoF(director.Policy.RelaxRemaining, "0") + " s; the Relay " + relay.State);
            if (!armedAtShut) return;
        }
        if (!relay.OnMap)
        {
            if (autoExitClock > AutoOnMapLimitSeconds)
                AutoExitFail("floor " + floor + ": the Relay was not on the map within " + AutoF(AutoOnMapLimitSeconds, "0") + " s of the start door's shut (director " + director.Phase + ", "
                    + (director.Calls - autoV2ExitCallsAtShut) + " calls, Relay " + relay.State + "; " + relay.LastArriveReport + ")");
            return;
        }
        var after = autoPlayClock - autoExitShutAt;
        var calls = director.Calls - autoV2ExitCallsAtShut;
        AutoExitCheck(calls == 1, "floor " + floor + ": armed at the shut; on the map " + AutoF(after) + " s after it, within the first dispatch (" + calls + " call"
            + (calls == 1 ? "" : "s") + ": " + (director.LastCall != null ? director.LastCall.Cause + ", " + director.LastPlacement : "none") + "; " + relay.LastArriveReport + ")");
        if (autoStationPlanned && floor >= 2) AutoStationBelowCheck();
        autoExitRows.Add("floor " + autoExitFloorFrom + " -> " + floor + " (" + FloorLabel(floor) + ", seed " + floorSeed + "): EXIT X " + autoExitX + ", " + autoExitDistance
            + " walking cells; route to N " + AutoF(autoExitRouteSeconds, "0.0") + " s; sealed at " + AutoF(autoExitSealDepthSeen) + " m into X; change " + AutoF(autoExitSwapMs, "0.0")
            + " ms (its frame " + AutoF(autoExitSwapFrameMs, "0.0") + " ms); north door held " + AutoF(floorHeldFor) + " s" + (floorOpenedLate ? " (limit)" : "") + "; shut "
            + AutoF(autoExitShutSeconds) + " s after opening; armed at the shut, on the map " + AutoF(after) + " s after it; tier " + tier + ", " + tierCells.Count + " cells, " + zonesVisited.Count + " zones");
        AutopilotLookFrom(startDoorPoint + AutoExitNorthOf(startDoorPoint) * 2.7f + Vector3.up * EyeHeight, startDoorPoint + Vector3.up * 1.3f,
            "exit_floor" + floor + "_start_door_from_floor");
        autoExitCycles++;
        if (autoExitCycles >= AutoExitFloors) { autoExitDone = true; return; }
        AutoExitNext(ExitRoute);
    }
#endif
}
#endif
