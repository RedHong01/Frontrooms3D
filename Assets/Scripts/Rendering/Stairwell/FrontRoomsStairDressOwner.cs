using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stairwell A: what one dressing made for itself (baked plate sets, per-stairwell meshes and materials), released when
/// the dressing goes: with the EXIT door's chunk (drop, rebuild, NextFloor) or with its stairwell. ExecuteAlways so the
/// edit-mode harness's DestroyImmediate releases too.
/// </summary>
[ExecuteAlways]
public sealed class FrontRoomsStairDressOwner : MonoBehaviour
{
    public readonly List<FrontRoomsStairSigns.PlateSet> plates = new List<FrontRoomsStairSigns.PlateSet>();
    public readonly List<Mesh> meshes = new List<Mesh>();
    public readonly List<Material> materials = new List<Material>();
    public readonly List<Texture> textures = new List<Texture>();

    static int alive;
    /// <summary>Owners alive now (the leak check reads it).</summary>
    public static int Alive => alive;

    bool released;

    void Awake() => alive++;

    void OnDestroy() => ReleaseAll();

    public void ReleaseAll()
    {
        if (released) return;
        released = true;
        alive--;
        foreach (var p in plates) FrontRoomsStairSigns.Release(p);
        plates.Clear();
        foreach (var m in meshes) Kill(m);
        meshes.Clear();
        foreach (var m in materials) Kill(m);
        materials.Clear();
        foreach (var t in textures) Kill(t);
        textures.Clear();
    }

    static void Kill(Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
    }
}
