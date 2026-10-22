using UnityEngine;

// The map's side of the visual chat's interactable kit (research/interactables/10_spec.md §6.1): the
// handles the door shots drive, with exactly the members the spec lists. The map compiles and tests
// against these interfaces with fakes; the visual chat's DoorRig / KeyRig implement them, or the map
// binds to their concrete types at landing (D1.6). Until then every door's rig is null and the shots
// fall back to the spec's numbers on today's cube leaf (FrontRoomsMapWorld.LockPose, JoltLeaf).
// Render only: no gameplay query ever reads a rig, and no kit part carries a collider.

/// <summary>A door's face in the kit's terms (SPEC §6.4): S is the door root's +X face, P its −X face. Since A12 (double-acting doors, Red 2026-10-22) the rig is not mirrored for a handing, so S and P are fixed faces and the leaf swings toward either (FrontRoomsMapWorld.FaceOf).</summary>
public enum DoorFace { S, P }

/// <summary>
/// The dressed door's moving hardware, in world space, from its current pose. Angles are logical (the rig
/// applies the mirror rule for a −X placement, G2 §1): + is "down" for the lever, the retract direction for
/// the knob, and "cuts toward the hinge" for the plug. <c>face</c> is the opener's face; the rig decides
/// whether the other face's part follows (a through-spindle).
/// </summary>
public interface IFrontRoomsDoorRig
{
    /// <summary>The keyway's mouth on this face (where the key's shoulder sits when inserted).</summary>
    Vector3 Keyhole(DoorFace face);
    /// <summary>Unit, into the lock from this face.</summary>
    Vector3 KeyholeDir(DoorFace face);
    /// <summary>Unit, the keyway's "cuts up" direction on this face.</summary>
    Vector3 KeyholeUp(DoorFace face);
    void SetPlug(DoorFace face, float degrees);
    void SetKnob(DoorFace face, float degrees);
    void SetLever(DoorFace face, float degrees);
    /// <summary>0 thrown (locked) to 1 retracted.</summary>
    void SetDeadbolt(float t);
    /// <summary>0 out (latched) to 1 drawn.</summary>
    void SetLatch(float t);
    /// <summary>Shift the leaf rig (never the hinge or the "Door leaf" collider) and let it settle.</summary>
    void Jolt(float metres);
}

/// <summary>
/// A dressed zone key (ring, key and tag) on its "Key · zone {id}" root, for the pickup and the unlock
/// shots. The pickup presentation (FrontRoomsDoorShot.BeginKeyPickup, plan §3.3) finds it on the key's
/// object (GetComponentInChildren): a key that has one is presented at once, with no dev switch. The
/// unlock does not move a key yet (it lands with the key models, D1.6).
/// </summary>
public interface IFrontRoomsKeyRig
{
    /// <summary>The "Key · zone {id}" empty the map moves.</summary>
    Transform Root { get; }
    /// <summary>Put the key's shoulder at a world pose (+Z = insertion, +Y = cuts up), ring and tag hanging.</summary>
    void PoseInLock(Pose shoulder);
    /// <summary>Put the grip and fob face at a world pose, the fob face to the lens.</summary>
    void PoseHeld(Pose lensFacing);
    /// <summary>The tag's damped swing (and the ring's during the key turn).</summary>
    void Sway(float degrees, float hz, float decay);
    /// <summary>The shot is done with the key: stop any sway. The pickup calls it just before it destroys the root.</summary>
    void Release();
}
