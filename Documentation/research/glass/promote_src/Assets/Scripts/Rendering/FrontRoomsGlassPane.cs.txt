using UnityEngine;

/// <summary>
/// Helpers for a window pane drawn with FrontRooms/Glass (Resources/Surfaces/Glass_Window).
/// The shader reads the pane frame from the object: the thinnest scaled axis is the pane
/// normal, v is object up (unless the pane lies flat), u is the remaining axis; a unit-cube
/// mesh (Unity's cube primitive) unless the material's _PaneSize gives the mesh size.
/// These helpers use the same rule, so a hit point maps to the uv the shader uses.
/// </summary>
public static class FrontRoomsGlassPane
{
    public const string WindowMaterial = "Surfaces/Glass_Window";
    public const string EdgeMaterial = "Surfaces/Glass_Edge";
    public const string ShardMaterial = "Surfaces/Glass_Shard";

    static readonly int CrackId = Shader.PropertyToID("_Crack");
    static readonly int ImpactId = Shader.PropertyToID("_ImpactUV");
    static readonly int SeedId = Shader.PropertyToID("_CrackSeed");
    static readonly int PalmId = Shader.PropertyToID("_Palm");
    static MaterialPropertyBlock block;

    /// <summary>The shared window material, or null if the glass setup has not been run.</summary>
    public static Material Window => Resources.Load<Material>(WindowMaterial);

    /// <summary>Pane uv (0-1, u along the pane, v up) of a world-space point, as FrontRooms/Glass computes it.</summary>
    public static Vector2 ImpactUV(Transform pane, Vector3 worldPoint, Vector2? meshSize = null)
    {
        var s = pane.lossyScale;
        var size = meshSize ?? Vector2.one;
        var e = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
        var thin = e.x <= e.y && e.x <= e.z ? 0 : (e.y <= e.z ? 1 : 2);
        var v = thin == 1 ? 2 : 1;
        var u = 3 - thin - v;
        var local = pane.InverseTransformPoint(worldPoint);
        return new Vector2(Mathf.Clamp01(local[u] / Mathf.Max(size.x, 1e-4f) + .5f), Mathf.Clamp01(local[v] / Mathf.Max(size.y, 1e-4f) + .5f));
    }

    /// <summary>
    /// Drive the crack hooks on one pane (crack 0-1 reach, palm 0-1). Uses a property block on
    /// that renderer only (it leaves the SRP Batcher for as long as it is cracked); pass
    /// crack = palm = 0 to clear it.
    /// </summary>
    public static void SetCrack(Renderer pane, float crack, Vector2 impactUV, float seed, float palm = 0f)
    {
        if (pane == null) return;
        if (crack <= 0f && palm <= 0f) { pane.SetPropertyBlock(null); return; }
        block ??= new MaterialPropertyBlock();
        pane.GetPropertyBlock(block);
        block.SetFloat(CrackId, Mathf.Clamp01(crack));
        block.SetVector(ImpactId, new Vector4(impactUV.x, impactUV.y, 0f, 0f));
        block.SetFloat(SeedId, seed);
        block.SetFloat(PalmId, Mathf.Clamp01(palm));
        pane.SetPropertyBlock(block);
    }
}
