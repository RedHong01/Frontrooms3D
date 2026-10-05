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
    [MenuItem("FrontRoomsss/Map/Test Relay director policy")]
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

        Debug.Log("[RelayDirectorPolicyTests] PASS: " + checks + " checks");
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
