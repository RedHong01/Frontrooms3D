using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// EditMode tests for Relay v2's director adapter (FrontRoomsRelayDirector, step V3a), driven against a fake
/// IFrontRoomsRelayHunter: no scene, no map, no game. They run with or without FRONTROOMS_RELAY_V2 (the adapter compiles
/// either way; without the define nothing in the game calls it). The real hunter's half (Arrive v2, the step-in, the
/// give-up beat, Withdraw) is checked in FrontRoomsMapInteractionTests under the define.
/// Covered: Arm (once a floor, the 60 s RELAX) and Disarm (the floor change); floor 1's held first call (a device trip
/// waits, Summon is refused, ReleaseFirstCall sends the queued device or a Gate call, always through FirstSend); PENDING's
/// retry clock, the one Relaxed try before the 10 s drop, CallDropped and the re-sent device call; RELAX after an Away
/// (half after an unbuilt cell) and Restore; the give-up beat's FADE (no hunter Fade), onMapCap's FADE and the hard end;
/// lock-ons at a chase's start and chaseCap's CapChase; the sanctuary; the sweep stand-in; a WentAway while a call is
/// pending; the tools shim constant and the retired 6-argument StationSendDue without a map. The onboarding (Red
/// 2026-10-30): floor 1's hold lasts until the game's gate (150 cells explored or 180 s; the director has no clock of its
/// own for it), a device trip waiting; the run's first dispatch is FirstSend on whichever floor it comes (Arm(hold,
/// gentleFirstSend); FirstSendDone is run-level, Disarm keeps it), every later one Normal; Arm(hold) = Arm(hold, hold).
/// Step V3b, with a second fake that implements IFrontRoomsRelayHunterV3b (FakeHunterV3b): an on-map trip re-dispatches
/// on foot or by the handoff while the budget lasts, then queues; a trip in a chase or a break is heard and queued (V3c,
/// systems design 2026-10-30: out at the next Away with QueuedCallOut; V3b had it heard only); a trip the
/// hunter cannot take queues (the latest wins) and its QueuedCallOut (the CALLED tick) comes at the next placement; A40's
/// hold still queues; the give-up beat's FADE still re-dispatches; the dry spell's sweep is the hunter's, along
/// RecentHeading (the run's first send keeps V3a's stand-in). FakeHunter (no V3b) keeps V3a's behaviour throughout.
/// Headless: -executeMethod FrontRoomsRelayDirectorTests.RunBatch -quit (throws on FAIL).
/// </summary>
public static class FrontRoomsRelayDirectorTests
{
    // A quarter second is exact in binary, so the retry clock lands on 0 exactly every second tick.
    const float Dt = .25f;
    static readonly Vector3 Feet = new Vector3(10f, 0f, 5f);
    static readonly Vector3 Eye = new Vector3(10f, 1.6f, 5f);
    static readonly Vector3 Forward = new Vector3(0f, .5f, 1f);

    /// <summary>What the director drives, recorded: no movement, no senses; the test sets State and the readings.</summary>
    sealed class FakeHunter : IFrontRoomsRelayHunter
    {
        public HunterState State { get; set; } = HunterState.Dormant;
        public bool Armed => State != HunterState.Dormant;
        public bool OnMap => FrontRoomsMapHunter.IsOnMap(State);
        public int WarnStage { get; set; }
        public bool PlayerPerceivesRelay { get; set; }
        public Vector3 Position { get; set; }
        public bool Accept = true;
        public int ArmCalls, DisarmCalls, FadeCalls, HardFadeCalls, CapCalls;
        public float CapSeconds = -1f;
        public float Now;
        public readonly List<FrontRoomsRelayPlacement> Tries = new List<FrontRoomsRelayPlacement>();
        public readonly List<float> TryTimes = new List<float>();
        public FrontRoomsRelayCall LastCall;
        public event Action<FrontRoomsRelayAwayReason> WentAway;
        public event Action<Vector3> GaveUp;

        public void Arm()
        {
            ArmCalls++;
            if (State == HunterState.Dormant) State = HunterState.Away;
        }

        public void Disarm()
        {
            DisarmCalls++;
            State = HunterState.Dormant;
        }

        public bool Dispatch(FrontRoomsRelayCall call, FrontRoomsRelayPlacement placement, Vector3 playerFeet, Vector3 playerEye, Vector3 playerForward)
        {
            Tries.Add(placement);
            TryTimes.Add(Now);
            if (!Accept || State != HunterState.Away) return false;
            LastCall = call;
            State = HunterState.Arrive;
            return true;
        }

        public void Fade(bool hardEnd)
        {
            if (hardEnd) HardFadeCalls++;
            else FadeCalls++;
        }

        public void CapChase(float loseSeconds)
        {
            CapCalls++;
            CapSeconds = loseSeconds;
        }

        public void SendAway(FrontRoomsRelayAwayReason reason)
        {
            if (State == HunterState.Dormant || State == HunterState.Away) return;
            State = HunterState.Away;
            WentAway?.Invoke(reason);
        }

        public void RaiseGaveUp() => GaveUp?.Invoke(Position);
    }

    /// <summary>
    /// Step V3b's fake: FakeHunter's recording plus IFrontRoomsRelayHunterV3b. The test sets what RedispatchKind answers
    /// (on the map; off it, Queue) and whether Handoff and Sweep find a placement; Handoff and Sweep go to Arrive, Redirect
    /// to Hunt.
    /// </summary>
    sealed class FakeHunterV3b : IFrontRoomsRelayHunter, IFrontRoomsRelayHunterV3b
    {
        public HunterState State { get; set; } = HunterState.Dormant;
        public bool Armed => State != HunterState.Dormant;
        public bool OnMap => FrontRoomsMapHunter.IsOnMap(State);
        public int WarnStage { get; set; }
        public bool PlayerPerceivesRelay { get; set; }
        public Vector3 Position { get; set; }
        public bool Accept = true, HandoffAccept = true, SweepAccept = true;
        public FrontRoomsRelayRedispatchKind Kind = FrontRoomsRelayRedispatchKind.Walk;
        public int ArmCalls, KindCalls, HandoffCalls, RedirectCalls, SweepCalls, FadeCalls;
        public float Now;
        public readonly List<FrontRoomsRelayPlacement> Tries = new List<FrontRoomsRelayPlacement>();
        public FrontRoomsRelayCall LastCall, LastHandoff, LastRedirect, LastSweep;
        public Vector3 LastHeading;
        public Vector3 SweepTarget { get; set; }
        public event Action<FrontRoomsRelayAwayReason> WentAway;
        public event Action<Vector3> GaveUp;

        public void Arm()
        {
            ArmCalls++;
            if (State == HunterState.Dormant) State = HunterState.Away;
        }

        public void Disarm() => State = HunterState.Dormant;

        public bool Dispatch(FrontRoomsRelayCall call, FrontRoomsRelayPlacement placement, Vector3 playerFeet, Vector3 playerEye, Vector3 playerForward)
        {
            Tries.Add(placement);
            if (!Accept || State != HunterState.Away) return false;
            LastCall = call;
            State = HunterState.Arrive;
            return true;
        }

        public void Fade(bool hardEnd) => FadeCalls++;

        public void CapChase(float loseSeconds) { }

        public void SendAway(FrontRoomsRelayAwayReason reason)
        {
            if (State == HunterState.Dormant || State == HunterState.Away) return;
            State = HunterState.Away;
            WentAway?.Invoke(reason);
        }

        public void RaiseGaveUp() => GaveUp?.Invoke(Position);

        public FrontRoomsRelayRedispatchKind RedispatchKind(Vector3 point, Vector3 playerFeet, Vector3 playerEye)
        {
            KindCalls++;
            return OnMap ? Kind : FrontRoomsRelayRedispatchKind.Queue;
        }

        public bool Handoff(FrontRoomsRelayCall call, Vector3 playerFeet, Vector3 playerEye, Vector3 playerForward)
        {
            HandoffCalls++;
            if (!HandoffAccept || !OnMap) return false;
            LastHandoff = call;
            State = HunterState.Arrive;
            return true;
        }

        public bool Redirect(FrontRoomsRelayCall call)
        {
            RedirectCalls++;
            if (!OnMap) return false;
            LastRedirect = call;
            State = HunterState.Hunt;
            return true;
        }

        public bool Sweep(FrontRoomsRelayCall call, bool relaxed, Vector3 playerFeet, Vector3 playerEye, Vector3 playerForward, Vector3 playerHeading)
        {
            SweepCalls++;
            LastHeading = playerHeading;
            if (!SweepAccept || State != HunterState.Away) return false;
            LastSweep = call;
            SweepTarget = playerFeet + playerHeading * 25f;
            State = HunterState.Arrive;
            return true;
        }
    }

    static int checks;

    [MenuItem("FrontRooms/Map/Test Relay director (v2 adapter)")]
    public static void Run() => Execute();

    public static void RunBatch() => Execute();

    static void Execute()
    {
        checks = 0;

        Check("Arm: Dormant until the shut; then RELAX 60 s, the hunter armed once (Away); a second Arm and RELAX noise add nothing", () =>
        {
            var h = new FakeHunter();
            var d = new FrontRoomsRelayDirector(h);
            Tick(d, h, 30f);
            var dormant = d.Phase == FrontRoomsRelayDirectorPhase.Dormant && !d.Armed && h.State == HunterState.Dormant && h.ArmCalls == 0 && h.Tries.Count == 0;
            d.Arm(false);
            d.Arm(false);
            d.Disturb(FrontRoomsRelayDisturbanceKind.Slam, Feet, false);
            Tick(d, h, 1f);
            return dormant && d.Armed && d.Phase == FrontRoomsRelayDirectorPhase.Relax && Near(d.Policy.RelaxRemaining, 59f) && h.ArmCalls == 1
                && h.State == HunterState.Away && d.Arms == 1 && !d.FirstCallHeld && !d.FirstSendNext && Near(d.Policy.Attention, 20f)
                && Near(FrontRoomsRelayDirector.NeverArmDelaySeconds, 1e5f);
        });

        Check("Disarm (the floor change): Dormant, the hunter disarmed, the hold, the queued device and the first send dropped; the next floor's Arm is a fresh 60 s RELAX", () =>
        {
            var h = new FakeHunter();
            var d = new FrontRoomsRelayDirector(h);
            d.Arm(true);
            Tick(d, h, 5f);
            d.DeviceTrigger(new Vector3(2f, 0f, 2f), "station");
            var busy = d.FirstCallHeld && d.FirstSendNext && d.Policy.HasQueuedDevice;
            d.Disarm();
            var reset = !d.Armed && d.Phase == FrontRoomsRelayDirectorPhase.Dormant && h.DisarmCalls == 1 && h.State == HunterState.Dormant
                && !d.FirstCallHeld && !d.FirstSendNext && !d.Policy.HasQueuedDevice && !d.Policy.HasPendingCall;
            Tick(d, h, 10f);
            var stays = d.Phase == FrontRoomsRelayDirectorPhase.Dormant && h.ArmCalls == 1;
            d.Arm(false);
            var fresh = d.Phase == FrontRoomsRelayDirectorPhase.Relax && Near(d.Policy.RelaxRemaining, 60f) && h.ArmCalls == 2 && d.Arms == 2 && !d.FirstSendNext;
            Tick(d, h, 61f);
            return busy && reset && stays && fresh && d.Phase == FrontRoomsRelayDirectorPhase.BuildUp && h.Tries.Count == 0 && d.Calls == 0;
        });

        Check("floor 1's hold: a device trip sounds and waits; Summon is refused; ReleaseFirstCall sends the queued device, placed through FirstSend", () =>
        {
            var h = new FakeHunter();
            var d = new FrontRoomsRelayDirector(h);
            var take = new Vector3(2f, 0f, 2f);
            var gate = new Vector3(30f, 0f, 30f);
            var sounds = 0;
            var called = new List<FrontRoomsRelayCallCause>();
            var calledExplicit = false;
            var dispatched = 0;
            var dispatchedHow = FrontRoomsRelayPlacement.Normal;
            d.DeviceTriggered += c => sounds++;
            d.Called += (cause, explicitCall, at) => { called.Add(cause); calledExplicit = explicitCall; };
            d.Dispatched += (c, how) => { dispatched++; dispatchedHow = how; };
            d.Arm(true);
            Tick(d, h, 5f);
            var queued = !d.DeviceTrigger(take, "station") && sounds == 1 && d.Policy.HasQueuedDevice && !d.Policy.HasPendingCall;
            var summonRefused = !d.Summon(gate) && !d.Policy.HasPendingCall;
            Tick(d, h, 100f);
            var held = h.Tries.Count == 0 && d.Phase == FrontRoomsRelayDirectorPhase.BuildUp && d.FirstCallHeld && d.FirstSendNext;
            var released = d.ReleaseFirstCall(gate);
            var pendingDevice = d.Phase == FrontRoomsRelayDirectorPhase.Pending && d.Policy.PendingCall != null
                && d.Policy.PendingCall.Cause == FrontRoomsRelayCallCause.Device && d.Policy.PendingCall.Estimate == take && !d.FirstCallHeld;
            Tick(d, h, Dt);
            var sent = h.Tries.Count == 1 && h.Tries[0] == FrontRoomsRelayPlacement.FirstSend && dispatched == 1 && dispatchedHow == FrontRoomsRelayPlacement.FirstSend
                && d.LastPlacement == FrontRoomsRelayPlacement.FirstSend && called.Count == 1 && called[0] == FrontRoomsRelayCallCause.Device && calledExplicit
                && h.LastCall != null && h.LastCall.Estimate == take && !d.FirstSendNext && d.Phase == FrontRoomsRelayDirectorPhase.Encounter;
            return queued && summonRefused && held && released && pendingDevice && sent && !d.ReleaseFirstCall(gate);
        });

        Check("floor 1's hold with nothing queued: ReleaseFirstCall (in RELAX) makes a Gate call to the gate's point; every retry is FirstSend", () =>
        {
            var h = new FakeHunter();
            var d = new FrontRoomsRelayDirector(h);
            var gate = new Vector3(30f, 0f, 30f);
            var cause = FrontRoomsRelayCallCause.Attention;
            var calledExplicit = true;
            d.Called += (c, explicitCall, at) => { cause = c; calledExplicit = explicitCall; };
            var notHeld = new FrontRoomsRelayDirector(new FakeHunter());
            notHeld.Arm(false);
            var refusedWithoutHold = !notHeld.ReleaseFirstCall(gate);
            d.Arm(true);
            Tick(d, h, 10f);
            var released = d.ReleaseFirstCall(gate);
            var pending = d.Phase == FrontRoomsRelayDirectorPhase.Pending && d.Policy.PendingCall.Cause == FrontRoomsRelayCallCause.Gate
                && !d.Policy.PendingCall.Explicit && d.Policy.PendingCall.Estimate == gate;
            h.Accept = false;
            Tick(d, h, 1.25f);
            h.Accept = true;
            Tick(d, h, .5f);
            var allFirst = h.Tries.Count >= 3;
            foreach (var p in h.Tries) allFirst &= p == FrontRoomsRelayPlacement.FirstSend;
            return refusedWithoutHold && released && pending && allFirst && h.State == HunterState.Arrive && cause == FrontRoomsRelayCallCause.Gate && !calledExplicit
                && d.LastPlacement == FrontRoomsRelayPlacement.FirstSend && h.LastCall.Estimate == gate && d.Phase == FrontRoomsRelayDirectorPhase.Encounter;
        });

        Check("the onboarding's hold (Red 2026-10-30): Arm(true, true) holds floor 1's first call through 200 s of ticks (only the game's gate releases it); a glass trip waits queued; ReleaseFirstCall sends it through FirstSend and the run's FirstSendDone turns true", () =>
        {
            var h = new FakeHunter();
            var d = new FrontRoomsRelayDirector(h);
            var glass = new Vector3(6f, 0f, 6f);
            var gate = new Vector3(30f, 0f, 30f);
            var fresh = !d.FirstSendDone && !d.FirstSendNext;
            d.Arm(true, true);
            var armed = d.FirstCallHeld && d.FirstSendNext && !d.FirstSendDone;
            Tick(d, h, 5f);
            var queued = !d.DeviceTrigger(glass, "glass") && d.Policy.HasQueuedDevice && !d.Policy.HasPendingCall;
            Tick(d, h, 200f);
            var held = h.Tries.Count == 0 && d.FirstCallHeld && d.FirstSendNext && !d.FirstSendDone && d.Calls == 0 && d.Policy.HasQueuedDevice;
            var released = d.ReleaseFirstCall(gate);
            Tick(d, h, Dt);
            var sent = h.Tries.Count == 1 && h.Tries[0] == FrontRoomsRelayPlacement.FirstSend && d.LastPlacement == FrontRoomsRelayPlacement.FirstSend && h.LastCall != null
                && h.LastCall.Estimate == glass && d.FirstSendDone && !d.FirstSendNext && !d.FirstCallHeld;
            return fresh && armed && queued && held && released && sent;
        });

        Check("the run-level first send over a floor change: after floor 1's FirstSend, Disarm keeps FirstSendDone; the next floor's Arm(false, true) is not gentle and its first placement is Normal", () =>
        {
            var h = new FakeHunter();
            var d = new FrontRoomsRelayDirector(h);
            d.Arm(true, true);
            Tick(d, h, 5f);
            d.ReleaseFirstCall(new Vector3(30f, 0f, 30f));
            Tick(d, h, Dt);
            var first = h.Tries.Count == 1 && d.LastPlacement == FrontRoomsRelayPlacement.FirstSend && d.FirstSendDone;
            d.Disarm();
            var kept = d.FirstSendDone && !d.FirstSendNext && !d.Armed && h.State == HunterState.Dormant;
            d.Arm(false, true);
            var next = !d.FirstSendNext && !d.FirstCallHeld && d.FirstSendDone && d.Arms == 2;
            Tick(d, h, 60f);
            var summoned = d.Summon(new Vector3(40f, 0f, 40f));
            Tick(d, h, Dt);
            var normal = h.Tries.Count == 2 && h.Tries[1] == FrontRoomsRelayPlacement.Normal && d.LastPlacement == FrontRoomsRelayPlacement.Normal && d.FirstSendDone;
            return first && kept && next && summoned && normal;
        });

        Check("the run's first send below floor 1 (no send on floor 1): on a fresh director Arm(false, true) holds nothing, its first dispatch is FirstSend (FirstSendDone); the next call on that floor is Normal", () =>
        {
            var h = new FakeHunter();
            var d = new FrontRoomsRelayDirector(h);
            d.Arm(false, true);
            var gentle = d.FirstSendNext && !d.FirstCallHeld && !d.FirstSendDone;
            Tick(d, h, 60f);
            var firstCall = d.Summon(new Vector3(40f, 0f, 40f));
            Tick(d, h, Dt);
            var first = h.Tries.Count == 1 && h.Tries[0] == FrontRoomsRelayPlacement.FirstSend && d.LastPlacement == FrontRoomsRelayPlacement.FirstSend && d.FirstSendDone && !d.FirstSendNext;
            h.SendAway(FrontRoomsRelayAwayReason.ExitReached);
            Tick(d, h, 101f);
            var secondCall = d.Summon(new Vector3(-40f, 0f, 40f));
            Tick(d, h, Dt);
            var later = h.Tries.Count == 2 && h.Tries[1] == FrontRoomsRelayPlacement.Normal && d.LastPlacement == FrontRoomsRelayPlacement.Normal && d.FirstSendDone;
            return gentle && firstCall && first && secondCall && later;
        });

        Check("Arm(hold) is Arm(hold, hold): Arm(true) holds and is gentle, Arm(false) neither; Arm(false, true) is gentle with no hold; a second Arm on the floor changes nothing", () =>
        {
            var d1 = new FrontRoomsRelayDirector(new FakeHunter());
            d1.Arm(true);
            var d2 = new FrontRoomsRelayDirector(new FakeHunter());
            d2.Arm(true, true);
            var d3 = new FrontRoomsRelayDirector(new FakeHunter());
            d3.Arm(false);
            var d4 = new FrontRoomsRelayDirector(new FakeHunter());
            d4.Arm(false, false);
            var d5 = new FrontRoomsRelayDirector(new FakeHunter());
            d5.Arm(false, true);
            d5.Arm(true, false);
            return d1.FirstCallHeld && d1.FirstSendNext && d2.FirstCallHeld && d2.FirstSendNext && !d3.FirstCallHeld && !d3.FirstSendNext && !d4.FirstCallHeld && !d4.FirstSendNext
                && d5.FirstSendNext && !d5.FirstCallHeld && d5.Arms == 1;
        });

        Check("PENDING: tried in the first tick after the call, then every 0.5 s; one Relaxed try in the tick of the 10 s drop; CallDropped, Attention = threshold - 20", () =>
        {
            var h = new FakeHunter();
            var d = new FrontRoomsRelayDirector(h);
            d.Arm(false);
            Tick(d, h, 60f);
            var buildUp = d.Phase == FrontRoomsRelayDirectorPhase.BuildUp;
            h.Accept = false;
            var calledAt = h.Now;
            var drops = 0;
            var dropAt = -1f;
            d.CallDropped += c => { drops++; dropAt = h.Now; };
            var summoned = d.Summon(new Vector3(40f, 0f, 40f));
            Tick(d, h, 12.5f);
            var n = h.Tries.Count;
            var normalOk = n == 21;
            for (var i = 0; i < n - 1 && normalOk; i++)
            {
                normalOk &= h.Tries[i] == FrontRoomsRelayPlacement.Normal;
                if (i > 0) normalOk &= Near(h.TryTimes[i] - h.TryTimes[i - 1], FrontRoomsRelayDirector.RetrySeconds);
            }
            var firstAt = n > 0 ? h.TryTimes[0] - calledAt : -1f;
            var relaxedLast = n > 0 && h.Tries[n - 1] == FrontRoomsRelayPlacement.Relaxed && Near(h.TryTimes[n - 1], dropAt);
            var threshold = new FrontRoomsRelayDirectorRules().At(new FrontRoomsRelayDirectorRules().callThreshold, 1, 60f);
            return buildUp && summoned && normalOk && Near(firstAt, Dt) && relaxedLast && drops == 1 && d.Drops == 1 && d.FailedTries == 21
                && Near(dropAt - calledAt, 10f + Dt) && d.Phase == FrontRoomsRelayDirectorPhase.BuildUp && Near(d.Policy.Attention, threshold - 20f)
                && !d.Policy.HasPendingCall && !d.Policy.HasQueuedDevice && Near(d.PendingSecondsTotal, 10f + Dt) && d.Calls == 0;
        });

        Check("a device call dropped by PENDING is queued and goes out again RequeueSeconds (10 s) after the drop, then is placed", () =>
        {
            var h = new FakeHunter();
            var d = new FrontRoomsRelayDirector(h);
            var glass = new Vector3(6f, 0f, 18f);
            d.Arm(false);
            Tick(d, h, 60f);
            h.Accept = false;
            var dropAt = -1f;
            d.CallDropped += c => dropAt = h.Now;
            var calledNow = d.DeviceTrigger(glass, "glass");
            var guard = 0;
            while (dropAt < 0f && guard++ < 100) Tick(d, h, Dt);
            var queued = dropAt >= 0f && d.Policy.HasQueuedDevice && d.Phase == FrontRoomsRelayDirectorPhase.BuildUp;
            h.Accept = true;
            Tick(d, h, FrontRoomsRelayDirector.RequeueSeconds - Dt);
            var notYet = d.Phase == FrontRoomsRelayDirectorPhase.BuildUp && !d.Policy.HasPendingCall && d.Policy.HasQueuedDevice;
            Tick(d, h, Dt);
            var resent = d.Phase == FrontRoomsRelayDirectorPhase.Pending && d.Policy.PendingCall != null && d.Policy.PendingCall.Cause == FrontRoomsRelayCallCause.Device
                && d.Policy.PendingCall.Queued && d.Policy.PendingCall.Estimate == glass && !d.Policy.HasQueuedDevice && Near(h.Now - dropAt, FrontRoomsRelayDirector.RequeueSeconds);
            Tick(d, h, Dt);
            return calledNow && queued && notYet && resent && h.State == HunterState.Arrive && h.LastCall != null && h.LastCall.Estimate == glass
                && d.Calls == 1 && d.Drops == 1 && d.Phase == FrontRoomsRelayDirectorPhase.Encounter;
        });

        Check("Away: RELAX 100 s at tier 1 after an exit (Attention 20, no Restore without a warning); half (50 s) after an unbuilt cell, with Restore after a felt stage", () =>
        {
            var h1 = new FakeHunter();
            var d1 = new FrontRoomsRelayDirector(h1);
            var restores1 = 0;
            var aways1 = 0;
            d1.Restore += () => restores1++;
            d1.Away += r => aways1++;
            PlaceOnMap(d1, h1);
            Tick(d1, h1, 3f);
            h1.SendAway(FrontRoomsRelayAwayReason.ExitReached);
            var exit = d1.Phase == FrontRoomsRelayDirectorPhase.Relax && Near(d1.Policy.RelaxRemaining, 100f) && restores1 == 0 && aways1 == 1
                && d1.LastAwayReason == FrontRoomsRelayAwayReason.ExitReached && Near(d1.Policy.Attention, 20f) && Near(d1.OnMapSeconds, 0f);

            var h2 = new FakeHunter();
            var d2 = new FrontRoomsRelayDirector(h2);
            var restores2 = 0;
            d2.Restore += () => restores2++;
            PlaceOnMap(d2, h2);
            h2.WarnStage = 1;
            Tick(d2, h2, Dt);
            var felt = d2.Policy.WarningFelt;
            h2.WarnStage = 0;
            Tick(d2, h2, 2f);
            h2.SendAway(FrontRoomsRelayAwayReason.UnbuiltCell);
            var unbuilt = d2.Phase == FrontRoomsRelayDirectorPhase.Relax && Near(d2.Policy.RelaxRemaining, 50f) && restores2 == 1
                && d2.LastAwayReason == FrontRoomsRelayAwayReason.UnbuiltCell;
            return exit && felt && unbuilt;
        });

        Check("the give-up beat (GaveUp) is the policy's FADE but never the hunter's Fade; onMapCap (75 s): SUSTAIN 4 s, then Fade(false) once; onMapCap + 15 s: Fade(true) once", () =>
        {
            var h1 = new FakeHunter();
            var d1 = new FrontRoomsRelayDirector(h1);
            var fades1 = 0;
            d1.FadeBegan += r => fades1++;
            PlaceOnMap(d1, h1);
            Tick(d1, h1, 5f);
            h1.RaiseGaveUp();
            Tick(d1, h1, 1f);
            var beat = d1.Phase == FrontRoomsRelayDirectorPhase.Fade && h1.FadeCalls == 0 && h1.HardFadeCalls == 0 && fades1 == 0;

            var h2 = new FakeHunter();
            var d2 = new FrontRoomsRelayDirector(h2);
            var reason = FrontRoomsRelayAwayReason.Test;
            d2.FadeBegan += r => reason = r;
            PlaceOnMap(d2, h2);
            float fadeAt = -1f, hardAt = -1f;
            var sustainSeen = false;
            for (var i = 0; i < 480; i++)
            {
                Tick(d2, h2, Dt);
                sustainSeen |= d2.Phase == FrontRoomsRelayDirectorPhase.Sustain && h2.FadeCalls == 0;
                if (fadeAt < 0f && h2.FadeCalls > 0) fadeAt = d2.OnMapSeconds;
                if (hardAt < 0f && h2.HardFadeCalls > 0) hardAt = d2.OnMapSeconds;
            }
            var cap = new FrontRoomsRelayDirectorRules().onMapCap[1];
            var capOk = sustainSeen && fadeAt >= cap + 4f - .5f && fadeAt <= cap + 4f + .5f && reason == FrontRoomsRelayAwayReason.Timeout && h2.FadeCalls == 1;
            var hardOk = hardAt >= cap + FrontRoomsRelayDirector.HardEndSeconds && hardAt <= cap + FrontRoomsRelayDirector.HardEndSeconds + Dt + .001f
                && h2.HardFadeCalls == 1 && d2.HardEnd;
            return beat && capOk && hardOk;
        });

        Check("a chase's start is a lock-on (TargetAcquired, Pressure +15); Chase - BreakDoor - Chase is one chase; a BreakDoor from a hunt is none; the second chase reaches lockOnCap 2 (Fade)", () =>
        {
            var h = new FakeHunter();
            var d = new FrontRoomsRelayDirector(h);
            PlaceOnMap(d, h);
            h.State = HunterState.Hunt;
            Tick(d, h, Dt);
            h.State = HunterState.BreakDoor;
            Tick(d, h, Dt);
            var noneFromHunt = d.LockOns == 0;
            h.State = HunterState.Hunt;
            Tick(d, h, Dt);
            h.State = HunterState.Chase;
            Tick(d, h, Dt);
            var first = d.LockOns == 1 && d.Policy.Pressure >= 15f - .001f;
            h.State = HunterState.BreakDoor;
            Tick(d, h, 1f);
            h.State = HunterState.Chase;
            Tick(d, h, 1f);
            var one = d.LockOns == 1 && h.FadeCalls == 0;
#if FRONTROOMS_RELAY_V2
            // V4b (ruled 2026-10-30): a lock-on counts toward lockOnCap once its chase has lasted 5 s; a shorter one does not, and
            // the cap's FADE comes after the chase that reached it. (A fake has no TargetAcquired: its chase's start is the +15.)
            var short1 = d.Policy.LockOnsCounted == 0;
            Tick(d, h, 3.1f);
            var counted = d.Policy.LockOnsCounted == 1;
            h.State = HunterState.Search;
            Tick(d, h, 1f);
            h.State = HunterState.Chase;
            Tick(d, h, 5.1f);
            var noFadeInChase = d.LockOns == 2 && d.Policy.LockOnsCounted == 2 && h.FadeCalls == 0;
            h.State = HunterState.Search;
            Tick(d, h, Dt);
            return noneFromHunt && first && one && short1 && counted && noFadeInChase && h.FadeCalls == 1 && d.Phase == FrontRoomsRelayDirectorPhase.Fade;
#else
            h.State = HunterState.Search;
            Tick(d, h, 1f);
            h.State = HunterState.Chase;
            Tick(d, h, Dt);
            return noneFromHunt && first && one && d.LockOns == 2 && h.FadeCalls == 1 && d.Phase == FrontRoomsRelayDirectorPhase.Fade;
#endif
        });

        Check("chaseCap (25 s at tier 1): CapChase(1.0 s) once, 25 s into the chase", () =>
        {
            var h = new FakeHunter();
            var d = new FrontRoomsRelayDirector(h);
            PlaceOnMap(d, h);
            h.State = HunterState.Chase;
            var capAt = -1f;
            var chaseSeconds = 0f;
            for (var i = 0; i < 120; i++)
            {
                Tick(d, h, Dt);
                chaseSeconds += Dt;
                if (capAt < 0f && h.CapCalls > 0) capAt = chaseSeconds;
            }
            var cap = new FrontRoomsRelayDirectorRules().chaseCap[1];
            return h.CapCalls == 1 && Near(h.CapSeconds, FrontRoomsRelayDirector.ChaseCapLoseSeconds) && Near(FrontRoomsRelayDirector.ChaseCapLoseSeconds, 1f)
                && capAt >= cap - .001f && capAt <= cap + Dt + .001f && d.LockOns == 1;
        });

        Check("a sanctuary (InStairwell): gains 0 and the dry clock paused while the director ticks it; the Disturb's own flag also gives 0", () =>
        {
            var h = new FakeHunter();
            var d = new FrontRoomsRelayDirector(h);
            d.Arm(false);
            Tick(d, h, 60f);
            Tick(d, h, 300f, true);
            var paused = d.Phase == FrontRoomsRelayDirectorPhase.BuildUp && Near(d.Policy.DrySpell, 0f) && !d.Policy.HasPendingCall && h.Tries.Count == 0;
            d.Disturb(FrontRoomsRelayDisturbanceKind.Sprint, Feet, false);
            var quietInside = Near(d.Policy.Attention, 20f);
            Tick(d, h, Dt, false);
            var clockRuns = Near(d.Policy.DrySpell, Dt);
            d.Disturb(FrontRoomsRelayDisturbanceKind.Sprint, Feet, false);
            var counted = Near(d.Policy.Attention, 23f);
            d.Disturb(FrontRoomsRelayDisturbanceKind.Sprint, Feet, true);
            d.Disturb(FrontRoomsRelayDisturbanceKind.Walk, Feet, false);
            return paused && quietInside && clockRuns && counted && Near(d.Policy.Attention, 23f);
        });

        Check("the dry spell's sweep (V3a stand-in): an ordinary placement with the estimate 24 m along the player's flat heading, error 1, logged as Sweep", () =>
        {
            var h = new FakeHunter();
            var d = new FrontRoomsRelayDirector(h);
            var cause = FrontRoomsRelayCallCause.Attention;
            var at = Vector3.zero;
            d.Called += (c, explicitCall, p) => { cause = c; at = p; };
            d.Arm(false);
            var dry = new FrontRoomsRelayDirectorRules().drySpellSeconds[1];
            Tick(d, h, 60f + dry + 1f);
            var want = Feet + new Vector3(0f, 0f, FrontRoomsRelayDirector.SweepAheadMetres);
            return h.Tries.Count == 1 && h.Tries[0] == FrontRoomsRelayPlacement.Normal && h.LastCall != null && h.LastCall.Cause == FrontRoomsRelayCallCause.Sweep
                && (h.LastCall.Estimate - want).sqrMagnitude < 1e-6f && h.LastCall.ErrorCells == 1 && cause == FrontRoomsRelayCallCause.Sweep && (at - want).sqrMagnitude < 1e-6f
                && d.LastCall.Cause == FrontRoomsRelayCallCause.Sweep && Near(FrontRoomsRelayDirector.SweepAheadMetres, 24f);
        });

        Check("a WentAway while a call is pending (a Relay placed by tools) keeps the call pending; it is tried and placed once the hunter is Away", () =>
        {
            var h = new FakeHunter();
            var d = new FrontRoomsRelayDirector(h);
            var aways = 0;
            d.Away += r => aways++;
            d.Arm(false);
            Tick(d, h, 60f);
            h.Accept = false;
            var target = new Vector3(40f, 0f, 40f);
            d.Summon(target);
            Tick(d, h, Dt);
            var triedOnce = h.Tries.Count == 1;
            h.State = HunterState.Search;
            Tick(d, h, 1f);
            var noTryOnMap = h.Tries.Count == 1;
            h.SendAway(FrontRoomsRelayAwayReason.Test);
            var kept = d.Phase == FrontRoomsRelayDirectorPhase.Pending && d.Policy.HasPendingCall && d.Policy.PendingCall.Estimate == target && aways == 1
                && d.LastAwayReason == FrontRoomsRelayAwayReason.Test;
            h.Accept = true;
            Tick(d, h, .75f);
            return triedOnce && noTryOnMap && kept && h.State == HunterState.Arrive && h.LastCall.Estimate == target && d.Phase == FrontRoomsRelayDirectorPhase.Encounter && d.Calls == 1;
        });

        Check("the gate's enum keeps its numbers (Fallback 2, the retired ThirdZone 3, the onboarding's Explored 4); with no map the retired 6-argument and the onboarding's 3-argument StationSendDue are Waiting", () =>
        {
            var noMap = FrontRoomsMapHunter.StationSendDue(null, new FrontRooms.Map.GridCoord(0, 0), 1000f, true, false, 9);
            var noMapOnboarding = FrontRoomsMapHunter.StationSendDue(null, 1000f, 1000);
            return (int)FrontRoomsMapHunter.StationGate.ThirdZone == 3 && (int)FrontRoomsMapHunter.StationGate.Fallback == 2 && (int)FrontRoomsMapHunter.StationGate.Explored == 4
                && FrontRoomsMapHunter.StationThirdZone == 3 && noMap == FrontRoomsMapHunter.StationGate.Waiting && noMapOnboarding == FrontRoomsMapHunter.StationGate.Waiting;
        });

        Check("the read-only predicates: IsOnMap is every state but Dormant, Away and Arrive; UsesArrive follows the define", () =>
        {
            var ok = !FrontRoomsMapHunter.IsOnMap(HunterState.Dormant) && !FrontRoomsMapHunter.IsOnMap(HunterState.Away) && !FrontRoomsMapHunter.IsOnMap(HunterState.Arrive);
            foreach (var s in new[] { HunterState.Listen, HunterState.Hunt, HunterState.Search, HunterState.Chase, HunterState.BreakDoor, HunterState.Wander, HunterState.Withdraw })
                ok &= FrontRoomsMapHunter.IsOnMap(s);
#if FRONTROOMS_RELAY_V2
            ok &= FrontRoomsMapHunter.UsesArrive;
#else
            ok &= !FrontRoomsMapHunter.UsesArrive;
#endif
            return ok;
        });

        // ---------- Step V3b (2026-10-30): on-map device calls, chase trips heard and queued (V3c), the queued slot's CALLED tick, the sweep ----------

        Check("V3b, a hunter with IFrontRoomsRelayHunterV3b: an on-map trip re-dispatches on foot while the budget lasts (Redirect with the Device call, Redispatched(walk), Called(Device), the device sounds once, one re-dispatch spent); the next trip queues; with FakeHunter (no V3b) the first trip queues as in V3a", () =>
        {
            var h = new FakeHunterV3b();
            var d = new FrontRoomsRelayDirector(h);
            var flags = new List<bool>();
            var causes = new List<FrontRoomsRelayCallCause>();
            var sounds = 0;
            d.Redispatched += (c, handoff) => flags.Add(handoff);
            d.Called += (c, e, p) => causes.Add(c);
            d.DeviceTriggered += c => sounds++;
            PlaceOnMapV3b(d, h);
            causes.Clear();
            var device = new Vector3(20f, 0f, 20f);
            var took = d.DeviceTrigger(device, "glass");
            var walked = took && h.RedirectCalls == 1 && h.LastRedirect != null && h.LastRedirect.Cause == FrontRoomsRelayCallCause.Device && h.LastRedirect.Estimate == device
                && flags.Count == 1 && !flags[0] && causes.Count == 1 && causes[0] == FrontRoomsRelayCallCause.Device && sounds == 1 && d.Redispatches == 1 && d.Handoffs == 0
                && d.Policy.Redispatches == 1 && !d.Policy.CanRedispatch && d.LastCall.Cause == FrontRoomsRelayCallCause.Device && h.State == HunterState.Hunt && d.UsesV3b;
            var second = d.DeviceTrigger(device + Vector3.right, "glass");
            var queued = !second && h.RedirectCalls == 1 && flags.Count == 1 && d.Policy.HasQueuedDevice && d.DevicesQueuedOnMap == 1 && sounds == 2;
            var v3a = new FakeHunter();
            var dv = new FrontRoomsRelayDirector(v3a);
            PlaceOnMap(dv, v3a);
            var v3aQueued = !dv.DeviceTrigger(device, "glass") && dv.Policy.HasQueuedDevice && dv.Redispatches == 0 && dv.DevicesQueuedOnMap == 0 && !dv.UsesV3b;
            return walked && queued && v3aQueued;
        });

        Check("V3b's handoff: straight from the map to Arrive (Handoff with a Handoff call at the device, Redispatched(handoff), Called(Handoff), Handoffs 1, no Away, still ENCOUNTER); a handoff with no placement walks instead (Redirect, Redispatched(walk))", () =>
        {
            var h = new FakeHunterV3b { Kind = FrontRoomsRelayRedispatchKind.Handoff };
            var d = new FrontRoomsRelayDirector(h);
            var flags = new List<bool>();
            var causes = new List<FrontRoomsRelayCallCause>();
            var aways = 0;
            d.Redispatched += (c, handoff) => flags.Add(handoff);
            d.Called += (c, e, p) => causes.Add(c);
            d.Away += r => aways++;
            PlaceOnMapV3b(d, h);
            causes.Clear();
            var device = new Vector3(60f, 0f, 60f);
            var took = d.DeviceTrigger(device, "glass");
            var handed = took && h.HandoffCalls == 1 && h.LastHandoff != null && h.LastHandoff.Cause == FrontRoomsRelayCallCause.Handoff && h.LastHandoff.Estimate == device
                && h.RedirectCalls == 0 && flags.Count == 1 && flags[0] && causes.Count == 1 && causes[0] == FrontRoomsRelayCallCause.Handoff && d.Handoffs == 1 && d.Redispatches == 1
                && d.LastCall.Cause == FrontRoomsRelayCallCause.Handoff && h.State == HunterState.Arrive && aways == 0 && d.Phase == FrontRoomsRelayDirectorPhase.Encounter;
            TickV3b(d, h, 1f);
            var kept = aways == 0 && d.Phase == FrontRoomsRelayDirectorPhase.Encounter && h.Tries.Count == 1;
            var h2 = new FakeHunterV3b { Kind = FrontRoomsRelayRedispatchKind.Handoff, HandoffAccept = false };
            var d2 = new FrontRoomsRelayDirector(h2);
            var flags2 = new List<bool>();
            d2.Redispatched += (c, handoff) => flags2.Add(handoff);
            PlaceOnMapV3b(d2, h2);
            var took2 = d2.DeviceTrigger(device, "glass");
            var walked = took2 && h2.HandoffCalls == 1 && h2.RedirectCalls == 1 && h2.LastRedirect != null && h2.LastRedirect.Cause == FrontRoomsRelayCallCause.Device
                && flags2.Count == 1 && !flags2[0] && d2.Handoffs == 0 && d2.Redispatches == 1;
            return handed && kept && walked;
        });

        Check("V3c (systems design 2026-10-30, reading 2 changed): a trip in a chase or a door break is heard and queued (the device sounds once per trip; no re-dispatch, no budget, Attention unchanged; the slot holds the latest trip; DevicesHeardOnly and DevicesQueuedOnMap count both); after the Away its call goes out and QueuedCallOut fires once at the placement, after Called(Device), at the last trip's point", () =>
        {
            var h = new FakeHunterV3b { Kind = FrontRoomsRelayRedispatchKind.NoiseOnly };
            var d = new FrontRoomsRelayDirector(h);
            var sounds = 0;
            var flags = 0;
            var outs = new List<Vector3>();
            var order = new List<string>();
            d.DeviceTriggered += c => sounds++;
            d.Redispatched += (c, handoff) => flags++;
            d.QueuedCallOut += p => { outs.Add(p); order.Add("tick"); };
            d.Called += (c, e, p) => order.Add("called " + c);
            PlaceOnMapV3b(d, h);
            order.Clear();
            var attention = d.Policy.Attention;
            var first = new Vector3(9f, 0f, 9f);
            var latest = new Vector3(11f, 0f, 9f);
            h.State = HunterState.Chase;
            var took = d.DeviceTrigger(first, "glass");
            h.State = HunterState.BreakDoor;
            var took2 = d.DeviceTrigger(latest, "station");
            var heardQueued = !took && !took2 && sounds == 2 && flags == 0 && d.DevicesHeardOnly == 2 && d.DevicesQueuedOnMap == 2 && d.Policy.HasQueuedDevice
                && d.Policy.QueuedDeviceId == "station" && d.Policy.Redispatches == 0 && d.Policy.CanRedispatch && Near(d.Policy.Attention, attention) && h.RedirectCalls == 0
                && h.HandoffCalls == 0 && h.KindCalls == 2 && outs.Count == 0 && order.Count == 0;
            h.SendAway(FrontRoomsRelayAwayReason.Test);
            var noneYet = outs.Count == 0 && d.Phase == FrontRoomsRelayDirectorPhase.Pending;
            TickV3b(d, h, Dt);
            var placed = h.State == HunterState.Arrive && h.LastCall != null && h.LastCall.Cause == FrontRoomsRelayCallCause.Device && h.LastCall.Queued && h.LastCall.Estimate == latest;
            return heardQueued && noneYet && placed && outs.Count == 1 && outs[0] == latest && order.Count == 2 && order[0] == "called Device" && order[1] == "tick"
                && !d.Policy.HasQueuedDevice && sounds == 2 && flags == 0;
        });

        Check("V3b: a trip the hunter cannot take (Withdraw, FADE, Arrive: Queue) waits in the queued slot with budget left, the latest trip winning; after the Away its call goes out and QueuedCallOut (the CALLED tick) fires once at the placement, after Called(Device), at the trip's point, never at the trip", () =>
        {
            var h = new FakeHunterV3b { Kind = FrontRoomsRelayRedispatchKind.Queue };
            var d = new FrontRoomsRelayDirector(h);
            var outs = new List<Vector3>();
            var order = new List<string>();
            d.QueuedCallOut += p => { outs.Add(p); order.Add("tick"); };
            d.Called += (c, e, p) => order.Add("called " + c);
            PlaceOnMapV3b(d, h);
            order.Clear();
            h.State = HunterState.Withdraw;
            var first = new Vector3(5f, 0f, 5f);
            var latest = new Vector3(7f, 0f, 7f);
            var a = d.DeviceTrigger(first, "glass");
            var b = d.DeviceTrigger(latest, "station");
            var waiting = !a && !b && d.Policy.HasQueuedDevice && d.Policy.QueuedDeviceId == "station" && outs.Count == 0 && d.DevicesQueuedOnMap == 2 && d.Policy.CanRedispatch
                && h.RedirectCalls == 0 && h.HandoffCalls == 0;
            h.SendAway(FrontRoomsRelayAwayReason.ExitReached);
            var noneYet = outs.Count == 0 && d.Phase == FrontRoomsRelayDirectorPhase.Pending;
            TickV3b(d, h, Dt);
            var placed = h.State == HunterState.Arrive && h.LastCall != null && h.LastCall.Cause == FrontRoomsRelayCallCause.Device && h.LastCall.Queued && h.LastCall.Estimate == latest;
            return waiting && noneYet && placed && outs.Count == 1 && outs[0] == latest && order.Count == 2 && order[0] == "called Device" && order[1] == "tick" && !d.Policy.HasQueuedDevice;
        });

        Check("V3b keeps A40's hold: with floor 1's first call held a trip queues even with the Relay on the map (RedispatchKind not asked, no re-dispatch, the device sounds once); ReleaseFirstCall sends it through FirstSend", () =>
        {
            var h = new FakeHunterV3b();
            var d = new FrontRoomsRelayDirector(h);
            var sounds = 0;
            d.DeviceTriggered += c => sounds++;
            d.Arm(true);
            TickV3b(d, h, 5f);
            h.State = HunterState.Search;
            var take = new Vector3(2f, 0f, 2f);
            var took = d.DeviceTrigger(take, "station");
            var held = !took && h.KindCalls == 0 && h.RedirectCalls == 0 && h.HandoffCalls == 0 && d.Redispatches == 0 && d.DevicesQueuedOnMap == 0 && d.Policy.HasQueuedDevice
                && sounds == 1 && d.FirstCallHeld;
            h.State = HunterState.Away;
            var released = d.ReleaseFirstCall(new Vector3(30f, 0f, 30f));
            TickV3b(d, h, Dt);
            return held && released && h.Tries.Count == 1 && h.Tries[0] == FrontRoomsRelayPlacement.FirstSend && h.LastCall != null && h.LastCall.Estimate == take;
        });

        Check("V3b: the give-up beat's FADE still re-dispatches (its patrol is on the map): CanRedispatch, a walk, the phase back to ENCOUNTER; onMapCap's FADE does not (the trip queues)", () =>
        {
            var h = new FakeHunterV3b();
            var d = new FrontRoomsRelayDirector(h);
            PlaceOnMapV3b(d, h);
            h.RaiseGaveUp();
            var beat = d.Phase == FrontRoomsRelayDirectorPhase.Fade && d.Policy.FadeReason == FrontRoomsRelayAwayReason.GiveUp && d.Policy.CanRedispatch && h.FadeCalls == 0;
            h.State = HunterState.Wander;
            var took = d.DeviceTrigger(new Vector3(9f, 0f, 9f), "glass");
            var back = took && h.RedirectCalls == 1 && d.Phase == FrontRoomsRelayDirectorPhase.Encounter && d.Redispatches == 1;
            var h2 = new FakeHunterV3b();
            var d2 = new FrontRoomsRelayDirector(h2);
            PlaceOnMapV3b(d2, h2);
            TickV3b(d2, h2, 80f);
            var capped = d2.Phase == FrontRoomsRelayDirectorPhase.Fade && d2.Policy.FadeReason != FrontRoomsRelayAwayReason.GiveUp && !d2.Policy.CanRedispatch;
            var took2 = d2.DeviceTrigger(new Vector3(9f, 0f, 9f), "glass");
            var queued = !took2 && h2.RedirectCalls == 0 && d2.Policy.HasQueuedDevice && d2.DevicesQueuedOnMap == 1;
            return beat && back && capped && queued;
        });

        Check("V3b's sweep (Q2): the dry spell's call goes to the hunter's Sweep along RecentHeading (the player's walk over the last 8 s, here +X; standing still, the flat look), Sweeps 1, Called(Sweep) at its SweepTarget, no Dispatch; the run's first send keeps V3a's stand-in (Dispatch FirstSend, no Sweep)", () =>
        {
            var h = new FakeHunterV3b();
            var d = new FrontRoomsRelayDirector(h);
            var at = Vector3.zero;
            var cause = FrontRoomsRelayCallCause.Attention;
            d.Called += (c, e, p) => { cause = c; at = p; };
            d.Arm(false);
            var dry = new FrontRoomsRelayDirectorRules().drySpellSeconds[1];
            var n = Mathf.RoundToInt((60f + dry + 1f) / Dt);
            for (var i = 0; i < n && h.SweepCalls == 0; i++)
            {
                var f = Feet + new Vector3(i * Dt, 0f, 0f);
                d.Tick(Dt, 1, false, f, f + Vector3.up * 1.6f, Forward);
            }
            var heading = h.LastHeading;
            var swept = h.SweepCalls == 1 && Mathf.Abs(heading.x - 1f) < .001f && Mathf.Abs(heading.z) < .001f && h.LastSweep != null && h.LastSweep.Cause == FrontRoomsRelayCallCause.Sweep
                && d.Sweeps == 1 && cause == FrontRoomsRelayCallCause.Sweep && (at - h.SweepTarget).sqrMagnitude < 1e-6f && d.LastCall.Cause == FrontRoomsRelayCallCause.Sweep && h.Tries.Count == 0;
            var h2 = new FakeHunterV3b();
            var d2 = new FrontRoomsRelayDirector(h2);
            d2.Arm(false);
            TickV3b(d2, h2, 10f);
            var still = d2.RecentHeading;
            var lookOk = Mathf.Abs(still.x) < 1e-4f && Mathf.Abs(still.y) < 1e-4f && Mathf.Abs(still.z - 1f) < 1e-4f;
            var h3 = new FakeHunterV3b();
            var d3 = new FrontRoomsRelayDirector(h3);
            d3.Arm(false, true);
            TickV3b(d3, h3, 60f + dry + 1f);
            var firstSendStandIn = h3.SweepCalls == 0 && h3.Tries.Count == 1 && h3.Tries[0] == FrontRoomsRelayPlacement.FirstSend && h3.LastCall != null
                && h3.LastCall.Cause == FrontRoomsRelayCallCause.Sweep && d3.Sweeps == 0;
            return swept && lookOk && firstSendStandIn;
        });

        Debug.Log("[RelayDirectorTests] PASS: " + checks + " checks"
#if FRONTROOMS_RELAY_V2
            + " (FRONTROOMS_RELAY_V2 on)"
#else
            + " (FRONTROOMS_RELAY_V2 off)"
#endif
            );
    }

    // Armed, past the 60 s RELAX, Summoned and placed (Arrive), then stepped in (Listen): on the map, the encounter at 0.
    static void PlaceOnMap(FrontRoomsRelayDirector d, FakeHunter h)
    {
        d.Arm(false);
        Tick(d, h, 60f);
        d.Summon(new Vector3(40f, 0f, 40f));
        Tick(d, h, Dt);
        h.State = HunterState.Listen;
    }

    static void Tick(FrontRoomsRelayDirector d, FakeHunter h, float seconds, bool sanctuary = false)
    {
        var n = Mathf.RoundToInt(seconds / Dt);
        for (var i = 0; i < n; i++)
        {
            h.Now += Dt;
            d.Tick(Dt, 1, sanctuary, Feet, Eye, Forward);
        }
    }

    // V3b's fake: as PlaceOnMap.
    static void PlaceOnMapV3b(FrontRoomsRelayDirector d, FakeHunterV3b h)
    {
        d.Arm(false);
        TickV3b(d, h, 60f);
        d.Summon(new Vector3(40f, 0f, 40f));
        TickV3b(d, h, Dt);
        h.State = HunterState.Listen;
    }

    static void TickV3b(FrontRoomsRelayDirector d, FakeHunterV3b h, float seconds)
    {
        var n = Mathf.RoundToInt(seconds / Dt);
        for (var i = 0; i < n; i++)
        {
            h.Now += Dt;
            d.Tick(Dt, 1, false, Feet, Eye, Forward);
        }
    }

    static bool Near(float a, float b) => Mathf.Abs(a - b) < .001f;

    static void Check(string name, Func<bool> test)
    {
        checks++;
        bool ok;
        try { ok = test(); }
        catch (Exception e) { throw new Exception("[RelayDirectorTests] FAIL: " + name + " (" + e.GetType().Name + ": " + e.Message + ")\n" + e.StackTrace); }
        if (!ok) throw new Exception("[RelayDirectorTests] FAIL: " + name);
        Debug.Log("[RelayDirectorTests] ok: " + name);
    }
}
