// FrontRoomsGlassShard.cs -- GD3 step 6 (build/06_debris.md): one pooled shard body of the debris world. It lives in the debris
// world's own physics scene (LocalPhysicsMode.Physics3D), so no gameplay query sees it; it only reports its contacts.
using UnityEngine;

[DisallowMultipleComponent]
public sealed class FrontRoomsGlassShard : MonoBehaviour
{
    public Rigidbody Body { get; internal set; }
    public MeshFilter Filter { get; internal set; }
    public MeshRenderer Renderer { get; internal set; }
    public MeshCollider Hull { get; internal set; }
    /// <summary>Index of this shard in its run (FrontRoomsGlassDebrisWorld), or -1 in the pool.</summary>
    public int Slot { get; internal set; } = -1;
    internal object Run;

    void OnCollisionEnter(Collision c)
    {
        if (Slot >= 0 && Run != null) FrontRoomsGlassDebrisWorld.OnShardContact(this, c);
    }
}
