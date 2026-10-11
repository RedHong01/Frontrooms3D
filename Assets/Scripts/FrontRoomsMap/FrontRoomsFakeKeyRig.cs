#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tests only (FrontRoomsDoorShotTests, door_shots_plan.md section 5.2, check 21): a dressed zone key that records
/// what the pickup presentation asks of it, and moves its root to the held pose as the visual chat's KeyRig
/// would. Editor only (compiled out of builds); it lives outside the Editor folder because Unity attaches no
/// component from an editor assembly. The test adds it to an object of its own and destroys it with that object.
/// </summary>
public sealed class FrontRoomsFakeKeyRig : MonoBehaviour, IFrontRoomsKeyRig
{
    /// <summary>Every pose PoseHeld was given, in order.</summary>
    public readonly List<Pose> held = new List<Pose>();
    /// <summary>The test's clock, to stamp the sway.</summary>
    public Func<float> clock;
    public int sways, releases;
    public float swayAt = -1f, swayDegrees, swayHz, swayDecay;

    public Transform Root => transform;

    public void PoseInLock(Pose shoulder) { }

    public void PoseHeld(Pose lensFacing)
    {
        held.Add(lensFacing);
        transform.SetPositionAndRotation(lensFacing.position, lensFacing.rotation);
    }

    public void Sway(float degrees, float hz, float decay)
    {
        sways++;
        swayAt = clock != null ? clock() : -1f;
        swayDegrees = degrees;
        swayHz = hz;
        swayDecay = decay;
    }

    public void Release() => releases++;
}
#endif
