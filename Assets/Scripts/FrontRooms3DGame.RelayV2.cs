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
//   glass 60, the station take as a device 45, the door noise at one loudness until V5 splits it (RelayDoorLoudness). The
//   walk (9 m, the Walk gait) was already wired in Mobile.cs and stays off the director: Attention 0.
public sealed partial class FrontRooms3DGame
{
    FrontRoomsRelayDirector director;
    // When (elapsed) and on which floor the director last armed; -1 / 0 before.
    float relayArmedAt = -1f;
    int relayArmedFloor;
    // The swing after an unlock's open is the door's own (RELAY 6: not the player's), so its DoorMoved is not an ease.
    bool relayUnlockSwing;
    Vector3 relayUnlockSwingAt;

    /// <summary>Tools: this run's Relay v2 director (null before a map run).</summary>
    public FrontRoomsRelayDirector RelayDirector => director;

    void StartRelayDirector()
    {
        relay.TierV2 = TierRules.V2At(tier);
        director = new FrontRoomsRelayDirector(relay, TierRules.DirectorRules(), tier);
        relayArmedAt = -1f;
        relayArmedFloor = 0;
        relayUnlockSwing = false;
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
                + director.Drops + " dropped this run - " + RelayDoorTally());
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
        map.DoorUnlocked += OnRelayDoorUnlocked;
        map.DoorOpenBegan += OnRelayDoorOpenBegan;
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
            + " s, then hunts to the call's estimate; grace " + (relay.ChaseGrace ? "on" : "off"));
#if UNITY_EDITOR
        if (autopilot && autoStationPlanned) AutoFirstSendBelowRow();
#endif
    }

    // V3b: the run's door outcomes and on-map device calls so far, for the logs.
    string RelayDoorTally() => "doors " + relay.DoorsOpened + " OPENed, " + relay.DoorsPushed + " pushed, " + relay.DoorsBroken + " broken, " + relay.DoorsWalled + " WALLed; "
        + director.Redispatches + " re-dispatches (" + director.Handoffs + " handoffs), " + director.Sweeps + " sweeps, " + director.DevicesHeardOnly + " trips in a chase or a break (heard and queued), "
        + director.DevicesQueuedOnMap + " queued on the map; heard re-targets " + relay.HeardRetargets + " this encounter, " + relay.HeardRetargetsTotal + " this run";

    // ---------- V4a: what each noise carries (RELAY 7, 10) ----------

    enum RelayNoiseKind : byte { Door, Glass, Sprint, Device }

    // The door noise: today's one loudness for every player door move (RELAY 10's ease, barge and slam, the unlock and the
    // rattle are V5's). V5 replaces this one reading with the swing's verb; RelayDoorMoved's Disturb is its other half.
    float RelayDoorLoudness => hunterTuning.doorNoiseRadius;

    float RelayLoudness(RelayNoiseKind kind) => kind == RelayNoiseKind.Door ? RelayDoorLoudness : kind == RelayNoiseKind.Glass ? hunterTuning.glassLoudness
        : kind == RelayNoiseKind.Sprint ? hunterTuning.sprintLoudness : hunterTuning.deviceLoudness;

    // A noise the Relay may hear, at the v2 loudness (the hunter multiplies by the tier's hearing). The walk is not here: it
    // reaches relay.Noise from Mobile.cs's WalkStepNoise at the same 9 m, the Walk gait only, and never reaches the director.
    void RelayNoise(RelayNoiseKind kind, Vector3 at)
    {
#if UNITY_EDITOR
        AutopilotNoise(kind == RelayNoiseKind.Door ? AutoCauseDoor : kind == RelayNoiseKind.Glass ? AutoCauseGlass : kind == RelayNoiseKind.Sprint ? AutoCauseSprint : AutoCauseOther, at, RelayLoudness(kind));
#else
        relay?.Noise(at, RelayLoudness(kind));
#endif
    }

    // RELAY 6: a player disturbance, at a point; nothing inside a sanctuary.
    void RelayDisturb(FrontRoomsRelayDisturbanceKind kind, Vector3 at)
    {
        if (director == null || map == null) return;
        director.Disturb(kind, at, map.InStairwell(map.CellOf(playerRoot != null ? playerRoot.position : at)));
    }

    // Every player door move is an ease for now (+1; V5 adds barge and slam), except the swing after an unlock's open.
    void RelayDoorMoved(Vector3 at)
    {
        if (relayUnlockSwing && Flat(at - relayUnlockSwingAt).sqrMagnitude < .01f)
        {
            relayUnlockSwing = false;
            return;
        }
        RelayDisturb(FrontRoomsRelayDisturbanceKind.EaseDoor, at);
    }

    void OnRelayDoorOpenBegan(FrontRoomsMapWorld.Door door, FrontRoomsMapWorld.DoorOpenSource source, bool latchDrawn)
    {
        if (source != FrontRoomsMapWorld.DoorOpenSource.Unlock) return;
        relayUnlockSwing = true;
        relayUnlockSwingAt = door.position;
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
