using UnityEngine;

/// <summary>
/// Hidden per-frame tick for FrontRoomsZoneReflection fades. Created on the
/// first fade (DontDestroyOnLoad, HideAndDontSave); nothing to place in a scene.
/// Unscaled time, so a fade still finishes while the game is paused or in slow motion
/// (timeScale 0 or below 1); during a fixed-step capture (Time.captureDeltaTime, e.g. the
/// Recorder) it advances by the capture step, so recorded fades last their real length.
/// </summary>
[AddComponentMenu("")]
public sealed class FrontRoomsZoneReflectionDriver : MonoBehaviour
{
    void LateUpdate() => FrontRoomsZoneReflection.Step(Time.captureDeltaTime > 0f ? Time.captureDeltaTime : Time.unscaledDeltaTime);
}
