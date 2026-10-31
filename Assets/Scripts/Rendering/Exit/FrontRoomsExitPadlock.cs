using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// A13 R1 (Red 2026-10-31; research/unlock/20_design.md §2.3): one 2 in laminated padlock on the EXIT's chain, as three
/// moving parts: the body (Kit_Padlock_Body), the shackle (Kit_Padlock_Shackle, about its heel leg) and the plug face
/// (Kit_Padlock_Plug, turning with the key). Owner: the game visual line.
///
/// The root is the body's frame (origin the top face's centre, +Y up, +Z the lock's face, +X toward the toe); the
/// shackle hangs at FrontRoomsExitChainKit.ShackleLocal(pop, swivel) under it, the plug at PlugLocal. A kit missing from
/// Resources is replaced by a plain stand-in mesh (FrontRoomsExitChainKit.StandIn), so the lock always has its parts.
/// Render-only: no collider, no EXAMINE. Pose writes only transforms (no allocation).
/// </summary>
public sealed class FrontRoomsExitPadlock
{
    public const string BodyName = "padlock body", ShackleName = "padlock shackle", PlugName = "padlock plug";

    public Transform Root { get; private set; }
    public Transform Body { get; private set; }
    public Transform Shackle { get; private set; }
    public Transform Plug { get; private set; }
    LODGroup[] lods;

    /// <summary>The padlock under <paramref name="parent"/> (local pose <paramref name="body"/>), shut or open.</summary>
    public static FrontRoomsExitPadlock Build(Transform parent, string name, Matrix4x4 body, bool open)
    {
        var p = new FrontRoomsExitPadlock();
        var root = new GameObject(name).transform;
        root.SetParent(parent, false);
        p.Root = root;
        p.Body = Part(root, FrontRoomsExitChainKit.BodyKit, BodyName);
        p.Shackle = Part(root, FrontRoomsExitChainKit.ShackleKit, ShackleName);
        p.Plug = Part(root, FrontRoomsExitChainKit.PlugKit, PlugName);
        p.lods = root.GetComponentsInChildren<LODGroup>(true);
        p.Pose(body.GetColumn(3), body.rotation, open ? FrontRoomsExitChainKit.PopTravel : 0f, open ? FrontRoomsExitChainKit.SwivelDeg : 0f, 0f);
        return p;
    }

    static Transform Part(Transform root, string kit, string label)
    {
        GameObject go = null;
        if (FrontRoomsKitLibrary.Has(kit)) go = FrontRoomsKitLibrary.Spawn(kit, root, Vector3.zero, Quaternion.identity, Vector3.one, false, label, false);
        if (go == null)
        {
            go = new GameObject(label);
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = FrontRoomsExitChainKit.StandIn(kit);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = FrontRoomsExitChainKit.Steel;
            r.shadowCastingMode = ShadowCastingMode.On;
            r.lightProbeUsage = LightProbeUsage.Off;
        }
        return go.transform;
    }

    /// <summary>
    /// The padlock's pose in its parent: the body's top-face centre and rotation, the shackle's pop (m, out of the body)
    /// and swivel (degrees about the heel leg), the plug's turn (degrees about the key's axis).
    /// </summary>
    public void Pose(Vector3 top, Quaternion rotation, float pop, float swivelDeg, float plugDeg)
    {
        if (Root == null) return;
        Root.localPosition = top;
        Root.localRotation = rotation;
        if (Shackle != null)
        {
            Shackle.localPosition = new Vector3(FrontRoomsExitChainKit.HeelX, pop, 0f);
            Shackle.localRotation = Quaternion.Euler(0f, swivelDeg, 0f);
        }
        if (Plug != null)
        {
            Plug.localPosition = FrontRoomsExitChainKit.PlugLocal;
            Plug.localRotation = Quaternion.Euler(0f, plugDeg, 0f);
        }
    }

    /// <summary>Hold every part at its finest level (the cut's close-up), or give the levels back to distance.</summary>
    public void ForceLod0(bool on)
    {
        if (lods == null) return;
        foreach (var g in lods)
            if (g != null) g.ForceLOD(on ? 0 : -1);
    }
}
