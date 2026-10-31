using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// EditMode smoke tests for the isolated v2 pacing policy.  They do not start
/// a scene or the legacy hunter, so the current release/leash tests remain
/// independent until the staged adapter is ready.
/// </summary>
public static class FrontRoomsRelayDirectorPolicyTests
{
    [MenuItem("FrontRooms/Map/Test Relay director policy")]
    public static void Run()
    {
        var checks = 0;
        Check("arm starts first-run RELAX", () =>
        {
            var policy = NewPolicy();
            policy.Arm();
            return policy.Armed && policy.Phase == FrontRoomsRelayDirectorPhase.Relax && policy.FirstRunGrace;
        }, ref checks);

        Check("noise is ignored during RELAX", () =>
        {
            var policy = NewPolicy();
            policy.Arm();
            policy.Disturb(FrontRoomsRelayDisturbanceKind.Slam, Vector3.one);
            return Mathf.Abs(policy.Attention - 20f) < .001f && !policy.HasPendingCall;
        }, ref checks);

        Check("threshold creates PENDING and resolves to a call", () =>
        {
            var policy = NewPolicy();
            var called = 0;
            policy.Called += call => called++;
            policy.Arm();
            for (var i = 0; i < 60; i++) policy.Tick(1f);
            for (var i = 0; i < 5; i++)
                policy.Disturb(FrontRoomsRelayDisturbanceKind.Slam, new Vector3(4f + i, 0f, 0f));
            return policy.Phase == FrontRoomsRelayDirectorPhase.Pending
                && policy.ResolvePending(true)
                && called == 1
                && policy.Phase == FrontRoomsRelayDirectorPhase.Encounter;
        }, ref checks);

        Check("device sound is immediate and queued while on map", () =>
        {
            var policy = NewPolicy();
            var deviceSounds = 0;
            policy.DeviceTriggered += call => deviceSounds++;
            policy.Arm();
            for (var i = 0; i < 60; i++) policy.Tick(1f);
            policy.DeviceTrigger(new Vector3(2f, 0f, 2f));
            policy.ResolvePending(true);
            policy.SetOnMap(true);
            var dispatched = policy.DeviceTrigger(new Vector3(12f, 0f, 12f));
            return !dispatched && deviceSounds == 2 && policy.HasQueuedDevice;
        }, ref checks);

        Check("pressure peak asks the hunter to leave", () =>
        {
            var policy = NewPolicy();
            var leave = 0;
            policy.SendAwayRequested += reason => leave++;
            policy.Arm();
            for (var i = 0; i < 60; i++) policy.Tick(1f);
            policy.DeviceTrigger(Vector3.zero);
            policy.ResolvePending(true);
            policy.SetOnMap(true);
            policy.NotifyStage(2);
            for (var i = 0; i < 40; i++) policy.Tick(1f);
            return policy.Phase == FrontRoomsRelayDirectorPhase.Sustain && leave == 0;
        }, ref checks);

        // ---------- Relay v2 V0 (2026-10-29): aligned with the 10-07..10-14 rules ----------

        Check("walking (desktop or handheld) gives 0; unlock and rattle +1 each (10-08)", () =>
        {
            var policy = NewPolicy();
            policy.Arm();
            for (var i = 0; i < 60; i++) policy.Tick(1f);
            var start = policy.Attention;
            policy.Disturb(FrontRoomsRelayDisturbanceKind.Walk, Vector3.one);
            var afterWalk = policy.Attention;
            policy.Disturb(FrontRoomsRelayDisturbanceKind.Unlock, Vector3.one);
            var afterUnlock = policy.Attention;
            policy.Disturb(FrontRoomsRelayDisturbanceKind.Rattle, Vector3.one);
            return policy.Phase == FrontRoomsRelayDirectorPhase.BuildUp && Near(afterWalk, start)
                && Near(afterUnlock, start + 1f) && Near(policy.Attention, start + 2f);
        }, ref checks);

        Check("a rattle by the Relay or the world counts nothing; Rattle is appended (Other keeps its value)", () =>
        {
            var policy = NewPolicy();
            policy.Arm();
            for (var i = 0; i < 60; i++) policy.Tick(1f);
            var start = policy.Attention;
            policy.Disturb(FrontRoomsRelayDisturbanceKind.Rattle, Vector3.one, FrontRoomsRelayDisturbanceSource.Relay);
            policy.Disturb(FrontRoomsRelayDisturbanceKind.Rattle, Vector3.one, FrontRoomsRelayDisturbanceSource.World);
            return Near(policy.Attention, start) && (int)FrontRoomsRelayDisturbanceKind.Other == 7
                && (int)FrontRoomsRelayDisturbanceKind.Rattle == 8;
        }, ref checks);

        Check("HoldFirstCall: no threshold call and no sweep while held; a device sounds and waits; lifting the hold sends it", () =>
        {
            var policy = NewPolicy();
            var sounds = 0;
            var called = 0;
            policy.DeviceTriggered += call => sounds++;
            policy.Called += call => called++;
            policy.Arm();
            policy.HoldFirstCall(true);
            for (var i = 0; i < 60; i++) policy.Tick(1f);
            for (var i = 0; i < 6; i++)
                policy.Disturb(FrontRoomsRelayDisturbanceKind.Slam, new Vector3(4f + i, 0f, 0f));
            var heldAtThreshold = policy.Attention >= 60f && policy.Phase == FrontRoomsRelayDirectorPhase.BuildUp && !policy.HasPendingCall;
            for (var i = 0; i < 400; i++) policy.Tick(1f);
            var noSweep = policy.Phase == FrontRoomsRelayDirectorPhase.BuildUp && !policy.HasPendingCall && Near(policy.DrySpell, 0f);
            var dispatched = policy.DeviceTrigger(new Vector3(2f, 0f, 2f), "station");
            var waiting = !dispatched && sounds == 1 && policy.HasQueuedDevice && policy.QueuedDeviceId == "station"
                && policy.Phase == FrontRoomsRelayDirectorPhase.BuildUp && policy.FirstCallHeld;
            policy.HoldFirstCall(false);
            var sent = policy.Phase == FrontRoomsRelayDirectorPhase.Pending && policy.HasPendingCall && !policy.HasQueuedDevice
                && policy.PendingCall.Cause == FrontRoomsRelayCallCause.Device && policy.PendingCall.Estimate == new Vector3(2f, 0f, 2f)
                && !policy.FirstCallHeld && policy.ResolvePending(true) && called == 1;
            return heldAtThreshold && noSweep && waiting && sent;
        }, ref checks);

        Check("HoldFirstCall: a device tripped in the first-run RELAX waits too, and goes out the moment the hold lifts", () =>
        {
            var policy = NewPolicy();
            policy.HoldFirstCall(true);
            policy.Arm();
            policy.Tick(5f);
            var dispatched = policy.DeviceTrigger(Vector3.one);
            var held = !dispatched && policy.Phase == FrontRoomsRelayDirectorPhase.Relax && policy.HasQueuedDevice;
            policy.HoldFirstCall(false);
            return held && policy.Phase == FrontRoomsRelayDirectorPhase.Pending && !policy.HasQueuedDevice;
        }, ref checks);

        Check("Disarm per floor: everything but the tier goes; Arm on the next floor starts a fresh 60 s RELAX", () =>
        {
            var policy = NewPolicy();
            var restores = 0;
            policy.Restore += () => restores++;
            policy.SetTier(3);
            policy.HoldFirstCall(true);
            policy.Arm();
            for (var i = 0; i < 60; i++) policy.Tick(1f);
            policy.HoldFirstCall(false);
            policy.DeviceTrigger(Vector3.zero);
            policy.ResolvePending(true);
            policy.SetOnMap(true);
            policy.SetSanctuary(true);
            policy.NotifyStage(2);
            policy.NotifyPlayerPerceivesRelay(true);
            policy.NotifyAlmostSeen(true);
            policy.Tick(3f);
            policy.DeviceTrigger(new Vector3(9f, 0f, 9f), "foil");
            policy.HoldFirstCall(true);
            var busy = policy.Pressure > 0f && policy.HasQueuedDevice && policy.WarningFelt && policy.OnMap;
            policy.Disarm();
            var reset = !policy.Armed && policy.Phase == FrontRoomsRelayDirectorPhase.Dormant && !policy.OnMap
                && !policy.HasPendingCall && !policy.HasQueuedDevice && policy.QueuedDeviceId == null && !policy.FirstCallHeld
                && !policy.WarningFelt && Near(policy.Attention, 20f) && Near(policy.Pressure, 0f) && Near(policy.EncounterTime, 0f)
                && Near(policy.DrySpell, 0f) && policy.CurrentStage == 0 && policy.Tier == 3 && restores == 0;
            policy.Arm();
            var fresh = policy.Phase == FrontRoomsRelayDirectorPhase.Relax && policy.FirstRunGrace && Near(policy.RelaxRemaining, 60f);
            policy.Disturb(FrontRoomsRelayDisturbanceKind.Slam, Vector3.one);
            var relaxQuiet = Near(policy.Attention, 20f);
            for (var i = 0; i < 60; i++) policy.Tick(1f);
            // Not in a sanctuary any more (Disarm cleared it): a slam counts again.
            policy.Disturb(FrontRoomsRelayDisturbanceKind.Slam, Vector3.one);
            return busy && reset && fresh && relaxQuiet && policy.Phase == FrontRoomsRelayDirectorPhase.BuildUp && Near(policy.Attention, 32f);
        }, ref checks);

        Check("a perceived Relay (NotifyPlayerPerceivesRelay) feeds Pressure +4/s at stage 0", () =>
        {
            var policy = NewPolicy();
            policy.Arm();
            for (var i = 0; i < 60; i++) policy.Tick(1f);
            policy.DeviceTrigger(Vector3.zero);
            policy.ResolvePending(true);
            policy.SetOnMap(true);
            policy.NotifyPlayerPerceivesRelay(true);
            for (var i = 0; i < 10; i++) policy.Tick(1f);
            var seen = policy.Pressure;
            policy.NotifyPlayerPerceivesRelay(false);
            for (var i = 0; i < 10; i++) policy.Tick(1f);
            return Near(seen, 40f) && Near(policy.Pressure, 40f);
        }, ref checks);

        Check("the tier table's v2 director rows are the policy's defaults (one place per number)", () =>
        {
            var fromTable = new FrontRoomsTierRules().DirectorRules();
            var defaults = new FrontRoomsRelayDirectorRules();
            return Same(fromTable.callThreshold, defaults.callThreshold) && Same(fromTable.attentionDecay, defaults.attentionDecay)
                && Same(fromTable.peakThreshold, defaults.peakThreshold) && Same(fromTable.onMapCap, defaults.onMapCap)
                && Same(fromTable.redispatchBudget, defaults.redispatchBudget) && Same(fromTable.lockOnCap, defaults.lockOnCap)
                && Same(fromTable.chaseCap, defaults.chaseCap) && Same(fromTable.relaxSeconds, defaults.relaxSeconds)
                && Same(fromTable.drySpellSeconds, defaults.drySpellSeconds) && fromTable.ClampTier(9) == 5;
        }, ref checks);

        // ---------- Relay v2 V3b (2026-10-30): on-map re-dispatch, heard-only trips, the give-up beat's FADE ----------

        Check("V3b CanRedispatch: not unarmed, in RELAX, off the map or held; on the map in ENCOUNTER with budget left it is; RedispatchDevice spends one and raises DeviceTriggered, Called (Device, explicit) and DispatchRequested; spent, it returns null and raises nothing", () =>
        {
            var policy = NewPolicy();
            var unarmed = !policy.CanRedispatch && policy.RedispatchDevice(Vector3.one, "glass", false) == null;
            policy.Arm();
            var relax = !policy.CanRedispatch;
            for (var i = 0; i < 60; i++) policy.Tick(1f);
            policy.DeviceTrigger(Vector3.zero);
            policy.ResolvePending(true);
            var offMap = !policy.CanRedispatch;
            policy.SetOnMap(true);
            policy.HoldFirstCall(true);
            var held = !policy.CanRedispatch;
            policy.HoldFirstCall(false);
            var sounds = 0;
            var called = 0;
            var requested = 0;
            FrontRoomsRelayCall last = null;
            policy.DeviceTriggered += c => sounds++;
            policy.Called += c => { called++; last = c; };
            policy.DispatchRequested += c => requested++;
            var can = policy.CanRedispatch && policy.Phase == FrontRoomsRelayDirectorPhase.Encounter && policy.Redispatches == 0;
            var call = policy.RedispatchDevice(new Vector3(3f, 0f, 3f), "glass", false);
            var spent = call != null && call.Cause == FrontRoomsRelayCallCause.Device && call.Explicit && !call.Queued && policy.Redispatches == 1 && sounds == 1 && called == 1
                && requested == 1 && last == call;
            var refused = !policy.CanRedispatch && policy.RedispatchDevice(new Vector3(4f, 0f, 4f), "glass", true) == null && sounds == 1 && called == 1 && requested == 1
                && policy.Redispatches == 1;
            return unarmed && relax && offMap && held && can && spent && refused;
        }, ref checks);

        Check("V3b DeviceHeardOnly (a trip in a chase or a break): DeviceTriggered only, nothing queued or pending, no budget; unarmed, nothing", () =>
        {
            var policy = NewPolicy();
            var sounds = 0;
            policy.DeviceTriggered += c => sounds++;
            policy.DeviceHeardOnly(Vector3.one);
            var unarmed = sounds == 0;
            policy.Arm();
            for (var i = 0; i < 60; i++) policy.Tick(1f);
            policy.DeviceTrigger(Vector3.zero);
            policy.ResolvePending(true);
            policy.SetOnMap(true);
            sounds = 0;
            policy.DeviceHeardOnly(new Vector3(6f, 0f, 6f));
            return unarmed && sounds == 1 && !policy.HasQueuedDevice && !policy.HasPendingCall && policy.Redispatches == 0 && policy.Phase == FrontRoomsRelayDirectorPhase.Encounter;
        }, ref checks);

        Check("V3b FadeReason: the give-up beat's FADE is GiveUp and still re-dispatches (back to ENCOUNTER; a handoff's call has cause Handoff); the lock-on cap's FADE is not, and a trip then queues", () =>
        {
            var policy = NewPolicy();
            policy.Arm();
            for (var i = 0; i < 60; i++) policy.Tick(1f);
            policy.DeviceTrigger(Vector3.zero);
            policy.ResolvePending(true);
            policy.SetOnMap(true);
            policy.NotifyGaveUp();
            var beat = policy.Phase == FrontRoomsRelayDirectorPhase.Fade && policy.FadeReason == FrontRoomsRelayAwayReason.GiveUp && policy.CanRedispatch;
            var call = policy.RedispatchDevice(new Vector3(5f, 0f, 5f), "glass", true);
            var back = call != null && call.Cause == FrontRoomsRelayCallCause.Handoff && call.Explicit && policy.Phase == FrontRoomsRelayDirectorPhase.Encounter;
            var other = NewPolicy();
            other.Arm();
            for (var i = 0; i < 60; i++) other.Tick(1f);
            other.DeviceTrigger(Vector3.zero);
            other.ResolvePending(true);
            other.SetOnMap(true);
#if FRONTROOMS_RELAY_V2
            // V4b (ruled 2026-10-30): a lock-on counts once its chase has lasted 5 s, and the cap's FADE comes after that chase.
            for (var n = 0; n < 2; n++)
            {
                other.NotifyTargetAcquired();
                other.NotifyChaseStarted();
                for (var i = 0; i < 11; i++) other.Tick(.5f);
                other.NotifyChaseEnded();
            }
#else
            other.NotifyTargetAcquired();
            other.NotifyTargetAcquired();
#endif
            var capped = other.Phase == FrontRoomsRelayDirectorPhase.Fade && other.FadeReason != FrontRoomsRelayAwayReason.GiveUp && !other.CanRedispatch
                && other.RedispatchDevice(Vector3.one, "glass", false) == null;
            var queued = !other.DeviceTrigger(Vector3.one, "glass") && other.HasQueuedDevice;
            return beat && back && capped && queued;
        }, ref checks);

#if FRONTROOMS_RELAY_V2
        Check("V4b Pressure (ruled 2026-10-30): the highest applicable rate, never the sum; a chase adds +6/s; +15 once at TargetAcquired", () =>
        {
            var policy = NewPolicy();
            policy.Arm();
            for (var i = 0; i < 60; i++) policy.Tick(1f);
            policy.DeviceTrigger(Vector3.zero);
            policy.ResolvePending(true);
            policy.SetOnMap(true);
            float Rate()
            {
                var before = policy.Pressure;
                policy.Tick(1f);
                return policy.Pressure - before;
            }
            policy.NotifyStage(2);
            var stage = Near(Rate(), 2.5f);
            policy.NotifyPlayerPerceivesRelay(true);
            var seen = Near(Rate(), 4f);
            policy.NotifyAlmostSeen(true);
            var almost = Near(Rate(), 5f);
            var before2 = policy.Pressure;
            policy.NotifyTargetAcquired();
            var plus = Near(policy.Pressure, before2 + 15f);
            policy.NotifyChaseStarted();
            var chase = Near(Rate(), 6f);
            return stage && seen && almost && plus && chase;
        }, ref checks);

        Check("V4b lockOnCap (ruled 2026-10-30): a chase under 5 s never counts, however many; a chase of 5 s does; FADE waits for that chase's end", () =>
        {
            var policy = NewPolicy();
            policy.Arm();
            for (var i = 0; i < 60; i++) policy.Tick(1f);
            policy.DeviceTrigger(Vector3.zero);
            policy.ResolvePending(true);
            policy.SetOnMap(true);
            // Short ones, so Pressure stays under the peak here (+6/s).
            for (var n = 0; n < 2; n++)
            {
                policy.NotifyChaseStarted();
                for (var i = 0; i < 4; i++) policy.Tick(.5f);
                policy.NotifyChaseEnded();
            }
            var noneCounted = policy.LockOnsCounted == 0 && policy.Phase == FrontRoomsRelayDirectorPhase.Encounter;
            policy.NotifyChaseStarted();
            for (var i = 0; i < 11; i++) policy.Tick(.5f);
            var one = policy.LockOnsCounted == 1;
            policy.NotifyChaseEnded();
            var stillOn = policy.Phase == FrontRoomsRelayDirectorPhase.Encounter;
            policy.NotifyChaseStarted();
            for (var i = 0; i < 11; i++) policy.Tick(.5f);
            var inChase = policy.LockOnsCounted == 2 && policy.Phase != FrontRoomsRelayDirectorPhase.Fade;
            policy.NotifyChaseEnded();
            return noneCounted && one && stillOn && inChase && policy.Phase == FrontRoomsRelayDirectorPhase.Fade;
        }, ref checks);
#endif

        Debug.Log("[RelayDirectorPolicyTests] PASS: " + checks + " checks");
    }

    static bool Near(float a, float b) => Mathf.Abs(a - b) < .001f;

    static bool Same(float[] a, float[] b)
    {
        if (a == null || b == null || a.Length != b.Length) return false;
        for (var i = 1; i < a.Length; i++) if (!Near(a[i], b[i])) return false;
        return true;
    }

    static bool Same(int[] a, int[] b)
    {
        if (a == null || b == null || a.Length != b.Length) return false;
        for (var i = 1; i < a.Length; i++) if (a[i] != b[i]) return false;
        return true;
    }

    static FrontRoomsRelayDirectorPolicy NewPolicy()
    {
        var policy = new FrontRoomsRelayDirectorPolicy();
        policy.SetTier(1);
        return policy;
    }

    static void Check(string name, Func<bool> test, ref int checks)
    {
        checks++;
        if (!test()) throw new Exception("[RelayDirectorPolicyTests] FAIL: " + name);
        Debug.Log("[RelayDirectorPolicyTests] ok: " + name);
    }
}
