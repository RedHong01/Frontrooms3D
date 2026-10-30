using UnityEngine;

/// <summary>
/// The one hidden host that steps every FrontRoomsDoorRig in LateUpdate (Play Mode), as FrontRoomsInspectLook's host does.
/// An idle rig (no beat, jolt, wobble or strike fall, its hinge and collider unmoved) returns after a few compares
/// (FrontRoomsDoorRig.Tick). Every AuditSeconds it also runs the facade's lock-rule audit (verify r1 E3a): a door whose
/// LOCKED / FREE model no longer matches map.LockedByRule is dressed again.
/// </summary>
public sealed class FrontRoomsDoorRigDriver : MonoBehaviour
{
    /// <summary>How often the lock-rule audit runs (unscaled seconds).</summary>
    public const float AuditSeconds = .25f;

    static FrontRoomsDoorRigDriver instance;
    float auditClock;

    /// <summary>Re-dresses the audit has made since Play started (tests, captures).</summary>
    public static int Redressed { get; private set; }

    internal static void Ensure()
    {
        if (instance != null || !Application.isPlaying) return;
        var go = new GameObject("Door rigs (driver)") { hideFlags = HideFlags.HideAndDontSave };
        DontDestroyOnLoad(go);
        instance = go.AddComponent<FrontRoomsDoorRigDriver>();
        Redressed = 0;
        // DontSave objects outlive Play Mode in the Editor: take the host down when Play Mode (or the player) ends.
        Application.quitting -= Shutdown;
        Application.quitting += Shutdown;
    }

    static void Shutdown()
    {
        Application.quitting -= Shutdown;
        if (instance != null) Destroy(instance.gameObject);
        instance = null;
    }

    void LateUpdate()
    {
        FrontRoomsDoorRig.TickAllForTools(Time.deltaTime);
        auditClock += Time.unscaledDeltaTime;
        if (auditClock < AuditSeconds) return;
        auditClock = 0f;
        Redressed += FrontRoomsInteractableKit.AuditLockModels();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
