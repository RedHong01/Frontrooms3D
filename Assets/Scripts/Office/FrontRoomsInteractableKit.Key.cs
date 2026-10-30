using System;
using UnityEngine;

/// <summary>
/// The key part of the interactables kit facade (D1.6, 10_design §4.3 and §6; 10_spec §4). Owned by the visual session.
/// Call sites (map contract M3 / M4, d16/data/contract_map_D16.diff): the map's SpawnKey builds an unscaled empty
/// "Key · zone {id}", hangs it from a key host (DressKeyHost) for a module key spot with a host, or lays it on its
/// spot, then calls DressKey; the unlock shot's key is DressShotKey. All parts are FrontRoomsKitLibrary.Spawn(colliders:
/// false, inspectable: false) (a lying key shows E · TAKE KEY, never the EXAMINE eye, DECISIONS #18). The one collider is
/// the look+E trigger, ON THE ROOT, only when the map asks (KeyPickupNeedsE).
/// </summary>
public static partial class FrontRoomsInteractableKit
{
    public enum KeyPlacement { HostHung, Surface, SurfaceOverEdge, Floor }

    public const string KeyHostLabel = "Key host (kit)", KeyPartLabel = "Key", KeyRingLabel = "Key ring", KeyTagLabel = "Key tag";
    public const string KeyKit = "Kit_Key_Zone", KeyRingKit = "Kit_KeyRing";

    /// <summary>Tools and tests: DressKey builds nothing and returns null (the map's cube key).</summary>
    public static bool DisableKeyDressForTools { get; set; }

    /// <summary>The map asks before it builds a key root (SpawnKey): false keeps today's cube key and no extra object.</summary>
    public static bool KeyDressWanted => !DisableKeyDressForTools && DressAllowed && FrontRoomsKitLibrary.Has(KeyKit);

    /// <summary>
    /// Dress a zone key on the map's root (unscaled; a hung key's root is the host's hook pose, a lying key's root is on
    /// the surface, +Z = the support's front). Returns the assembly, or null when a kit is missing.
    /// </summary>
    public static FrontRoomsKeyAssembly DressKey(Transform root, FrontRoomsKeyIdentity id, KeyPlacement placement, bool trigger)
    {
        if (DisableKeyDressForTools || !DressAllowed || root == null) return null;
        var tagKit = id.TagKit;
        if (!FrontRoomsKitLibrary.Has(KeyKit) || !FrontRoomsKitLibrary.Has(KeyRingKit) || !FrontRoomsKitLibrary.Has(tagKit)) return null;
        GameObject keyGo = null, ringGo = null, tagGo = null;
        FrontRoomsKeyAssembly assembly = null;
        try
        {
            keyGo = KeyParts.Spawn(KeyKit, root, KeyPartLabel);
            ringGo = KeyParts.Spawn(KeyRingKit, root, KeyRingLabel);
            tagGo = KeyParts.Spawn(tagKit, root, KeyTagLabel);
            SetTagToken(tagGo, id);
            assembly = root.gameObject.AddComponent<FrontRoomsKeyAssembly>();
            assembly.Init(id, keyGo.transform, ringGo.transform, tagGo.transform);
            HookFloorToken();
            var pose = placement == KeyPlacement.HostHung ? KeyPose.Hung : placement == KeyPlacement.SurfaceOverEdge ? KeyPose.OverEdge : KeyPose.Flat;
            assembly.PoseRest(pose, Quaternion.identity);
            if (trigger) assembly.AddTrigger();
            return assembly;
        }
        catch
        {
            if (assembly != null) DoorParts.Kill(assembly);
            DoorParts.Kill(keyGo);
            DoorParts.Kill(ringGo);
            DoorParts.Kill(tagGo);
            throw;
        }
    }

    /// <summary>
    /// A key host for a module key spot (case A, 10_spec §4.3): Kit_KeyHookBoard ("key rack"), Kit_KeyCabinet or Kit_KeyHook,
    /// spawned at the spot (floor level, its back on the wall, its front along the yaw), label "Key host (kit)". keyHook =
    /// the world pose of the host's default hook (sidecar keyHostDefault, else "key_hook"), rotation = the host's (+Z room).
    /// Null when the kit is missing.
    /// </summary>
    public static Transform DressKeyHost(Transform chunkRoot, string hostKit, Vector3 localPos, float yaw, out Pose keyHook)
    {
        keyHook = new Pose(chunkRoot != null ? chunkRoot.TransformPoint(localPos) : localPos, Quaternion.identity);
        if (DisableKeyDressForTools || !DressAllowed || chunkRoot == null || string.IsNullOrEmpty(hostKit) || !FrontRoomsKitLibrary.Has(hostKit)) return null;
        var host = DoorParts.Spawn(hostKit, chunkRoot, localPos, Quaternion.Euler(0f, yaw, 0f), Vector3.one, KeyHostLabel);
        // The rack's and the cabinet's hook labels are printed numbers too (cells 01-24): blank paper while the type is held.
        if (!ShowKitType) DoorParts.SetAtlasCell(host, "Prop_KeyTagNo", "Prop_Paper", 10, 10, FrontRoomsKeyIdentity.BlankCell, true);
        var info = FrontRoomsKitLibrary.GetInfo(hostKit);
        var hook = Vector3.zero;
        if (info != null && !info.TryAnchor("key_hook", out hook)) hook = new Vector3(0f, 1.476f, .018f);
        keyHook = new Pose(host.transform.TransformPoint(hook), host.transform.rotation);
        return host.transform;
    }

    /// <summary>The unlock shot's key (contract M4): a transient assembly with no trigger, on the Default layer.</summary>
    public static FrontRoomsKeyAssembly DressShotKey(Transform parent, FrontRoomsKeyIdentity id)
    {
        if (DisableKeyDressForTools || !DressAllowed) return null;
        var root = new GameObject("Key (unlock shot)").transform;
        if (parent != null) root.SetParent(parent, false);
        var assembly = DressKey(root, id, KeyPlacement.Surface, false);
        if (assembly == null) DoorParts.Kill(root.gameObject);
        return assembly;
    }

    static class KeyParts
    {
        internal static GameObject Spawn(string kit, Transform root, string label)
        {
            var go = FrontRoomsKitLibrary.Spawn(kit, root, Vector3.zero, Quaternion.identity, Vector3.one, false, label, false);
            if (go == null) throw new InvalidOperationException("key kit missing: " + kit);
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) DoorParts.Kill(c);
            DoorParts.Remap(go, false, kit == KeyKit);
            return go;
        }
    }
}
