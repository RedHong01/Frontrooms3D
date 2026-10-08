using FrontRooms.Map;
using UnityEngine;

/// <summary>
/// The evacuation-plan placard (Q16): a 17 × 11 in plan in a 1 in clear-anodised
/// snap frame, on the MAP side of the start door, its centre 1.560 m above the
/// floor (research/placard/10_spec.md §5; 35_fix.md §5 raised it from 1.524). RoomStream's EndStreamAt calls
/// Prepare once the terminal facade stands; the mount waits for the run
/// (FrontRooms3DGame.MapRunStarted), resolves the spot from the seed with this
/// class's rule, spawns Kit_EvacPlacard (+ Kit_EvacPlacardLens, not on WebGL)
/// render-only with shadows off, and binds FrontRoomsPlacardGlow.
///
/// The rule reads only public map data (CellOf, Cache.Edge). Mounts, in order:
///   P1 facade beside the door, in the open side cell (west first, then east);
///   P2 the side wall facing the door cell (no open side; west wall first);
///   P3 the facade past an arch pier, west (every other case).
/// Deterministic: the spot depends only on the seed. Never mirrored or scaled.
/// </summary>
public static class FrontRoomsPlacard
{
    public const string KitName = "Kit_EvacPlacard";
    public const string LensKitName = "Kit_EvacPlacardLens";

    /// <summary>
    /// Frame centre above the floor, every mount: 1.560 m (61.4 in), so the frame (1.403-1.717 m) clears the top of
    /// the wallpaper's guide row, where the cue states turn or flatten the band (z 1.184-1.391 m, cue_states/states.py,
    /// print blocks of 1125 mm from the floor), by 12 mm. The spec's 1.524 (60 in, the US room-sign height) hid the top
    /// 24 mm of that band; the top stays under NYC's 6 ft (1.83 m) sign cap (35_fix.md §5).
    /// </summary>
    public const float CentreHeight = 1.56f;
    /// <summary>Frame outside, metres (kit sidecar "placard.frame").</summary>
    public static readonly Vector2 FrameSize = new Vector2(.4668f, .3138f);
    public const float FrameDepth = .0135f;

    // §5.2 offsets from the door centreline / door line (metres).
    const float P1Offset = 1.7334f;         // inner frame edge on the cell line (1.500)
    const float P2Offset = 1.4195f;         // 0.5 mm proud of the 1.42 wall face
    const float P2Depth = 1.5f;             // mid-cell from the door line
    const float P3Offset = 1.9134f;         // 0.10 m past the pier's far face (1.58)
    const float Proud = .0005f;             // off the reveal plane / wall face

    public enum Mount { P1Facade, P2SideWall, P3PastPier }

    /// <summary>A resolved spot: where the frame's origin (its wall-face centre) goes, its facing, the cell whose lamp gates the glow, and the floor rect kept clear.</summary>
    public struct Spot
    {
        public Mount mount;
        public int side;                     // -1 west (−X), +1 east (+X)
        public Vector3 position;             // world
        public Quaternion rotation;          // world; the kit's front (+Z) faces into the map
        public GridCoord glowCell;
        public GridCoord doorCell;
        public Rect keepClear;               // world XZ (x, z)
        public EdgeKind west, east;
    }

    /// <summary>
    /// Called by FrontRoomsRoomStream.EndStreamAt after BuildFacade. Adds an inactive-until-run
    /// "Placard mount" under the terminal room, so the placard lives exactly as long as the facade.
    /// </summary>
    /// <param name="room">The terminal stream room's root.</param>
    /// <param name="doorPointWorld">The door's centre on the door line, at floor level.</param>
    /// <param name="facadeFaceZWorld">World z of the map-side face the frame sits on (the "far side" reveal plane).</param>
    public static FrontRoomsPlacardMount Prepare(Transform room, Vector3 doorPointWorld, float facadeFaceZWorld)
    {
        if (room == null) return null;
        // Retire every earlier mount on this room NOW. Destroy is deferred to the end of the frame, and an armed
        // mount would still answer MapRunStarted, so two EndStreamAt calls in one frame would spawn two placards.
        for (var i = room.childCount - 1; i >= 0; i--)
        {
            var child = room.GetChild(i);
            if (child.name != FrontRoomsPlacardMount.ObjectName) continue;
            var oldMount = child.GetComponent<FrontRoomsPlacardMount>();
            if (oldMount != null) oldMount.Retire();
            child.name = FrontRoomsPlacardMount.ObjectName + " (retired)";
            child.gameObject.SetActive(false);
            if (Application.isPlaying) Object.Destroy(child.gameObject);
            else Object.DestroyImmediate(child.gameObject);
        }
        var go = new GameObject(FrontRoomsPlacardMount.ObjectName);
        go.transform.SetParent(room, false);
        var mount = go.AddComponent<FrontRoomsPlacardMount>();
        mount.Arm(doorPointWorld, facadeFaceZWorld);
        return mount;
    }

    /// <summary>The §5.2 rule. The door opens north (+Z) onto <c>map.CellOf(door + 1.5 m north)</c>.</summary>
    public static Spot Resolve(FrontRoomsMapWorld map, Vector3 doorPointWorld, float facadeFaceZWorld)
    {
        var dc = doorPointWorld;
        var d = map.CellOf(dc + Vector3.forward * (MapGrid.CellSize * .5f));
        var westCell = new GridCoord(d.x - 1, d.y);
        var eastCell = new GridCoord(d.x + 1, d.y);
        var spot = new Spot { doorCell = d, west = map.Cache.Edge(d, westCell), east = map.Cache.Edge(d, eastCell) };
        var y = dc.y + CentreHeight;
        var facadeZ = facadeFaceZWorld + Proud;
        int s;
        if (spot.west == EdgeKind.Open || spot.east == EdgeKind.Open)
        {
            s = spot.west == EdgeKind.Open ? -1 : 1;
            spot.mount = Mount.P1Facade;
            spot.position = new Vector3(dc.x + s * P1Offset, y, facadeZ);
            spot.rotation = Quaternion.identity;
            spot.glowCell = s < 0 ? westCell : eastCell;
            spot.keepClear = MinMax(dc.x + s * 1.35f, dc.z, dc.x + s * 2.117f, dc.z + .90f);
        }
        else if (spot.west == EdgeKind.Wall || spot.east == EdgeKind.Wall)
        {
            s = spot.west == EdgeKind.Wall ? -1 : 1;
            spot.mount = Mount.P2SideWall;
            spot.position = new Vector3(dc.x + s * P2Offset, y, dc.z + P2Depth);
            spot.rotation = Quaternion.Euler(0f, -s * 90f, 0f);     // front (+Z) turned to face −s·X, into D
            spot.glowCell = d;
            spot.keepClear = MinMax(dc.x + s * .52f, dc.z + 1.117f, dc.x + s * 1.42f, dc.z + 1.883f);
        }
        else
        {
            // Both sides Arch (or, off-contract, a Door/Window edge): on the facade past the west pier.
            s = -1;
            spot.mount = Mount.P3PastPier;
            spot.position = new Vector3(dc.x - P3Offset, y, facadeZ);
            spot.rotation = Quaternion.identity;
            spot.glowCell = westCell;
            spot.keepClear = MinMax(dc.x - 2.297f, dc.z, dc.x - 1.53f, dc.z + .90f);
        }
        spot.side = s;
        return spot;
    }

    static Rect MinMax(float x0, float z0, float x1, float z1) =>
        Rect.MinMaxRect(Mathf.Min(x0, x1), Mathf.Min(z0, z1), Mathf.Max(x0, x1), Mathf.Max(z0, z1));

    public static string Label(Mount m) => m switch { Mount.P1Facade => "P1", Mount.P2SideWall => "P2", _ => "P3" };
}
