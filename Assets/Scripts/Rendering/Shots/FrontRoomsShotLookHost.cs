using UnityEngine;

/// <summary>The hidden host that ticks FrontRoomsShotLook after the shots have moved (LateUpdate).</summary>
public sealed class FrontRoomsShotLookHost : MonoBehaviour
{
    void LateUpdate() => FrontRoomsShotLook.Tick(Time.deltaTime);

    void OnDestroy() => FrontRoomsShotLook.Uninstall();
}
