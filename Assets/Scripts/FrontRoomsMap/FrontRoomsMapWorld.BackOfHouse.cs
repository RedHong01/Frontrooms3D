using FrontRooms.Map;

// Back-of-house map doors (zone doors D-2 option 2: Red, 2026-11-05 21:26 in the visual chat, a new back-of-house door, painted
// steel with a kick plate and a louvre; players in the low areas see doors more like plant rooms; part of today's Level 0 doors
// change; at 19:35 Level 0's doors stay as they are. Verbatim in handoff/visual.md section 5, 0t and 0x).
// The map line's rule for which map doors wear the visual line's back-of-house member: one Low zone in three is a service zone,
// and every Level 0 door on its border is back-of-house. A pure predicate for the floor, and INERT: nothing in the game calls it
// until the visual kit's DoorMemberFor asks it, right after the map hook (Run) and before its Office and Pool tests.
// Precedence: Run / Exit > Office / Pool > back-of-house > Lobby. The lamps, paper, carpet and ceilings of a service zone stay as
// today (Red 10-31, low areas as recommended); WebGL leaves map doors undressed. Doc: Documentation/MAP_GENERATION.md,
// "Back-of-house doors".
public sealed partial class FrontRoomsMapWorld
{
    /// <summary>The share of Low zones that are service zones (BackOfHouseZone): one in three.</summary>
    public const float BackOfHouseShare = 1f / 3f;

    // The service-zone hash's salt, "BOH1" (MapHash uses 11-73; this map 89, 97, 211, 223, 307, 977; the Run! and device salts are "RUN.", "FOI1", "ALM.").
    const int BackOfHouseSalt = 0x424F4831;

    /// <summary>
    /// D-2 option 2: does this map door wear the back-of-house member? True for a door between a Low Level 0 zone that is a
    /// service zone (BackOfHouseZone) and a Standard Level 0 zone. False for the EXIT door, a Run! door (IsRunDoor), any door
    /// with an Office or pool-theme side (the kit's Office and Pool tests win), any door touching a cell the transition kits
    /// leave as the map builds it (TransitionExcluded: the start area and its stairwell, X and the EXIT's 2-cell approach,
    /// the start stairwell door's hold on later floors, the station's group and strip, pool cells), any pair of sides that is
    /// not Low | Standard, and any edge that is not a door. Pure for the floor: the seed and the two cells' zones (and the
    /// floor's start, EXIT and station, set before the first build) decide; no revision, tier or build order, so both chunk
    /// copies of a border door, every rebuild and every revisit shift agree. The edge test only says whether the edge is a
    /// door now. Reads zones through the generator, so it never records a zone's tier or meets a Run! zone.
    /// Inert until the visual kit's DoorMemberFor asks it.
    /// </summary>
    public bool BackOfHouseDoor(Door door)
    {
        if (door == null || Cache == null) return false;
        var a = door.a;
        var b = door.b;
        if (System.Math.Abs(a.x - b.x) + System.Math.Abs(a.y - b.y) != 1) return false;
        if (IsExitEdge(a, b) || IsRunDoor(door)) return false;
        if (TransitionExcluded(a) || TransitionExcluded(b)) return false;
        var za = Cache.Generator.ZoneOf(a);
        var zb = Cache.Generator.ZoneOf(b);
        if (za.theme != ZoneTheme.Level0 || zb.theme != ZoneTheme.Level0) return false;
        ZoneInfo low;
        if (za.height == ZoneHeight.Low && zb.height == ZoneHeight.Standard) low = za;
        else if (zb.height == ZoneHeight.Low && za.height == ZoneHeight.Standard) low = zb;
        else return false;
        if (!BackOfHouseZone(low.id)) return false;
        return Cache.Edge(a, b) == EdgeKind.Door;
    }

    /// <summary>
    /// The rule's zone half: the zone named <paramref name="zoneId"/> (its site chunk) is a Low Level 0 zone drawn as a service
    /// zone, at BackOfHouseShare by a hash of the seed and the id (salt "BOH1"). Pure in the seed. For BackOfHouseDoor, tools
    /// and the plan pictures; the kit asks BackOfHouseDoor.
    /// </summary>
    public bool BackOfHouseZone(GridCoord zoneId)
    {
        if (Cache == null) return false;
        var zone = Cache.Generator.Zone(zoneId);
        if (zone.height != ZoneHeight.Low || zone.theme != ZoneTheme.Level0) return false;
        return MapHash.Unit(MapHash.Hash(Cache.Generator.Seed, zoneId.x, zoneId.y, BackOfHouseSalt)) < BackOfHouseShare;
    }
}
