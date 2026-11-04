using System;
using System.Collections.Generic;
using System.Reflection;
using FrontRooms.Map;
using UnityEngine;
using UnityEngine.InputSystem;

// Scene 3, menu 5 MUSEUM (交互设计, 2026-11-03; scene3/rooms/90_plan.md §1 and §3 row 3, 80_museum.md I1, 82_integration.md):
// the museum of 10_research §3 built in the GAME scene, from the game's own cells and doors, walked by the game's own player.
// - The tour's copy of the level profile (DontSave, no station): every zone Standard 2.9 Level 0, no office, module, column
//   or pile, no devices, no revisit shift, one fixed seed. The run starts without the title (the private RequestTitleStart,
//   by reflection, as the game's own editor harnesses start runs) and the Relay stays dormant (its tools shim,
//   hunterTuning.releaseDelaySeconds 1e6, plus the tours' HeldForTools).
// - At MapRunStarted the museum becomes ONE carve on the map's cache (FrontRoomsMapCarve.From with a stand-in layout, the
//   station's own path; Message A of 90 §4): hub 0 and rooms 1-4 on the two chunks 40 north of the player's, every edge
//   with a museum cell on either side forced, the two museum chunks' zone keys moved into sealed corners, the lamps Steady
//   inside and Off on a two-cell ring (an unshadowed lamp lights through walls). On the next frame its chunks and their west
//   and south neighbours are built at once, the stream is let go and the player walks in at the hub, facing rooms 2 and 3.
// - Room 1 is live: six real Door records on its bay fronts. 1.1 one door, A body / B E (the map-wide switch, so every
//   museum door switches with it: Q-3 is not answered, one switched door is the plan's recommendation); 1.2 its lite; 1.3 the
//   LOCKED model for show (the kit's DoorLockedOverride); 1.5 broken with the map's BreakDoor (B); 1.6 the Run! goal door's
//   look (DoorMemberOverride); 1.7 the member stand A-D. Rooms 2-4 and the hub's two stations stand as framed codes.
// - Keys: [ ] room to room, 1-7 a station (again: its next variant), 0 the hub, R room 1's doors as built (only while the
//   game's phase is Playing: outside it R is the game's END RUN), B a museum door broken, P the plan (.Plan.cs), G the list.
// Everything private it reads is found by name and type; a missing one turns its part off with one message, never throws.
// A failure anywhere leaves the run as the game's own floor, with one message; the plan (P) keeps working.
public sealed partial class FrontRoomsInteractablesShowcase
{
    // The layout (80 §4): museum cells (u 0-15 west to east, v 0-7 south to north) fill the chunks O and O + (1, 0) exactly.
    const int MuseumCols = 16, MuseumRows = 8;
    /// <summary>O = the player's chunk at MapRunStarted + (0, 40): 960 m north of the start rooms (no public start-door getter).</summary>
    const int MuseumChunksNorth = 40, MuseumShiftTries = 12;
    /// <summary>Lamps Off this many cells round the museum: past the shadow radius a lamp lights through walls.</summary>
    const int MuseumRing = 2;
    /// <summary>The copy's map seed: the same museum surroundings, key sites and origin on every Play.</summary>
    const int MuseumSeed = 20261103;
    const float MuseumStartDelay = .5f, MuseumStartEvery = .5f, MuseumStartGiveUp = 30f;
    const float DoorTagHeight = 2.5f, WallTagHeight = 1.6f, SignTagHeight = 2.55f, MuseumTagFar = 22f, MuseumTagSeconds = .1f;
    const float StopNear = 1.6f, PlacardNear = 2.2f, MuseumCardSeconds = 8f;
    const string MuseumKeysLine = "[ ] ROOM  ·  1-7 STATION (AGAIN: VARIANT)  ·  0 HUB  ·  R RESET  ·  B BREAK  ·  P PLAN  ·  G HIDE  ·  F2 TOURS";

    static readonly Vector2 HubSpawn = new Vector2(8f, .8f);

    enum MuseumState { Title, Carved, Building, Built, Failed }
    enum MuseumPart { Lane, Bay, Alcove, Corner }

    /// <summary>A stop in museum units (cells): where to stand and face, and where its framed code hangs (on which wall face, how high).</summary>
    struct MuseumSpot
    {
        public Vector2 stand, face, anchor, normal;
        public float height;
        public bool ok;
    }

    /// <summary>A framed code on a wall: a station (slot 1-7; the hub's 1-2), or with slot 0 a room's sign over its arches in the hub.</summary>
    sealed class MuseumTag
    {
        public int room, slot;
        public Vector3 at, normal;
        public bool occluded;
    }

    // The game's private start and the Relay's tools shim, by name and type (GameFields, .Rooms.cs). Null: that part is off.
    static readonly MethodInfo TitleStartMethod = GameMethod("RequestTitleStart");
    static readonly FieldInfo HunterTuningField = GameField("hunterTuning", typeof(FrontRoomsHunterTuning));

    MuseumState museumState;
    GridCoord museumChunk, museumCell0;
    int museumMetres;
    FrontRoomsMapCarve museumCarve;
    bool museumPlanOpen, museumStartStopped;
    float museumLoadedAt, museumNextTry, museumTagClock, museumMarkUntil;
    // 1.7's member: 0 Lobby, 1 Office, 2 Run!, 3 Exit (FrontRoomsDoorMember's order).
    int museumMember;
    // The station last walked to, and the one whose stop the player stands at now (-1 none).
    int museumMarkRoom = -1, museumMarkSlot = -1, museumNearRoom = -1, museumNearSlot = -1;
    // Room 1's door fronts by edge id, and what the kit is told about them (render only).
    readonly HashSet<long> museumLiveEdges = new HashSet<long>();
    readonly HashSet<long> museumLockedModel = new HashSet<long>();
    readonly Dictionary<long, FrontRoomsDoorMember> museumMembers = new Dictionary<long, FrontRoomsDoorMember>();
    readonly List<MuseumTag> museumTags = new List<MuseumTag>();
    readonly List<MuseumTag> museumTagPool = new List<MuseumTag>();
    // The kit's two tools statics: ours while the museum stands, the values found before put back when it goes.
    Func<FrontRoomsMapWorld.Door, FrontRoomsDoorMember?> museumMemberHook, museumPrevMember;
    Func<FrontRoomsMapWorld.Door, bool?> museumLockedHook, museumPrevLocked;
    bool museumHooksSet;

    static MethodInfo GameMethod(string name)
    {
        try
        {
            return typeof(FrontRooms3DGame).GetMethod(name, GameFields, null, Type.EmptyTypes, null);
        }
        catch (Exception)
        {
            return null;
        }
    }

    // ---------- the copy, the scene, the run (called from the landed tour code)

    /// <summary>
    /// The MUSEUM copy (80 §4, 90 §1 step 1): every zone Standard 2.9 Level 0 (shares 0 / 1 / 0, officeShare 0), no module,
    /// column or pile, no station (no floor key, no station carve for the cache's one slot), doors free, body doors (1.1 A),
    /// no devices, no revisit shift, no renovated crossing, one seed. Only this DontSave copy changes.
    /// </summary>
    void ApplyMuseumCopy()
    {
        var g = tourProfile.generation;
        if (g != null)
        {
            g.lowShare = 0f;
            g.standardShare = 1f;
            g.tallShare = 0f;
            g.officeShare = 0f;
            g.moduleChance = 0f;
            g.level0ColumnRooms = g.officeColumnRooms = g.tallColumnHalls = 0f;
        }
        tourProfile.runSeed = MuseumSeed;
        tourProfile.stationModule = tourProfile.stationWaiting = tourProfile.stationDesk = null;
        tourProfile.doorsNeedKeys = false;
        tourProfile.bodyDoors = true;
        tourProfile.sashPry = tourProfile.foil = tourProfile.alarmDoor = tourProfile.runRooms = false;
        tourProfile.pileChance = 0f;
        tourProfile.renovationShare = 0f;
        tourProfile.shiftAfterSeconds = 1e6f;
        // The museum's west and south walls belong to its neighbour chunks: from room 4 the west one is two chunks away.
        tourProfile.buildRadius = Mathf.Max(2, tourProfile.buildRadius);
    }

    /// <summary>The game scene is in (after the game's Awake, before its Start): the Relay's shim, the start's clock, and a run already going.</summary>
    void MuseumSceneLoaded()
    {
        museumState = MuseumState.Title;
        museumCarve = null;
        museumPlanOpen = false;
        museumStartStopped = false;
        museumLoadedAt = Time.unscaledTime;
        museumNextTry = museumLoadedAt + MuseumStartDelay;
        museumTags.Clear();
        museumMarkRoom = museumMarkSlot = museumNearRoom = museumNearSlot = -1;
        ClearMuseumTools();
        if (game == null) return;
        // The Relay never arms while releaseDelaySeconds >= the director's never-arm value (RelayV2's arm test); v1's release
        // waits on the same field. The EXAMINE and glass harnesses set 1e6 the same way. An instance field: it dies with the scene.
        var tuning = HunterTuningField != null ? HunterTuningField.GetValue(game) as FrontRoomsHunterTuning : null;
        if (tuning != null) tuning.releaseDelaySeconds = Mathf.Max(tuning.releaseDelaySeconds, 1e6f);
        else MuseumNote("No Relay shim (FrontRooms3DGame.hunterTuning not found): the tools' hold keeps the Relay where it stands instead.");
        if (TitleStartMethod == null) MuseumNote("No automatic start (FrontRooms3DGame.RequestTitleStart not found): SPACE starts the run, the title's own way.");
        // The caught card's TRY AGAIN starts its run inside the game's Awake, before this scene callback hands the copy over.
        if (game.GetComponentInChildren<FrontRoomsMapWorld>(true) != null)
        {
            museumState = MuseumState.Failed;
            MuseumNote("This run started before the museum's copy was handed over (a retry): it is the game's own floor. F2, then 5 builds the museum.");
        }
    }

    /// <summary>MapRunStarted: the carve now (pure data, before any museum chunk is generated); the build next frame.</summary>
    void MuseumRunStarted()
    {
        museumTags.Clear();
        museumMarkRoom = museumMarkSlot = museumNearRoom = museumNearSlot = -1;
        if (museumState == MuseumState.Failed) return;
        try
        {
            string why;
            if (PlanMuseum(out why)) museumState = MuseumState.Carved;
            else MuseumFail(why, null);
        }
        catch (Exception e)
        {
            MuseumFail("the carve threw", e);
        }
    }

    void MuseumRunEnded()
    {
        ClearMuseumTools();
        museumCarve = null;
        museumTags.Clear();
        museumNearRoom = museumNearSlot = -1;
        museumState = MuseumState.Title;
    }

    // NextFloor made a new map: the museum (one floor-1 carve) is gone with the old cache.
    void MuseumFloorChanged()
    {
        ClearMuseumTools();
        museumTags.Clear();
        museumPlanOpen = false;
        museumState = MuseumState.Failed;
        MuseumNote("The floor changed (the EXIT): the museum stays on floor 1. F2, then 5 builds it again.");
        Toast("MUSEUM", "The floor changed: the museum stays on floor 1. F2, then 5 builds it again.");
    }

    /// <summary>Put the kit's two tools statics back (as they were before the museum set them) and forget room 1's doors. OnDestroy, BackToMenu, Choose, the run's end.</summary>
    void ClearMuseumTools()
    {
        if (museumHooksSet)
        {
            if (FrontRoomsInteractableKit.DoorMemberOverride == museumMemberHook) FrontRoomsInteractableKit.DoorMemberOverride = museumPrevMember;
            if (FrontRoomsInteractableKit.DoorLockedOverride == museumLockedHook) FrontRoomsInteractableKit.DoorLockedOverride = museumPrevLocked;
            museumHooksSet = false;
        }
        museumPrevMember = null;
        museumPrevLocked = null;
        museumMembers.Clear();
        museumLockedModel.Clear();
        museumLiveEdges.Clear();
    }

    void SetMuseumHooks()
    {
        if (museumHooksSet) return;
        if (museumMemberHook == null) museumMemberHook = MuseumMemberOf;
        if (museumLockedHook == null) museumLockedHook = MuseumLockedOf;
        museumPrevMember = FrontRoomsInteractableKit.DoorMemberOverride;
        museumPrevLocked = FrontRoomsInteractableKit.DoorLockedOverride;
        FrontRoomsInteractableKit.DoorMemberOverride = museumMemberHook;
        FrontRoomsInteractableKit.DoorLockedOverride = museumLockedHook;
        museumHooksSet = true;
    }

    // The kit asks these for every door it dresses: room 1's fronts only, null (the kit's own rule) for every other door.
    FrontRoomsDoorMember? MuseumMemberOf(FrontRoomsMapWorld.Door door)
    {
        FrontRoomsDoorMember member;
        if (door != null && museumMembers.TryGetValue(door.edge, out member)) return member;
        return null;
    }

    bool? MuseumLockedOf(FrontRoomsMapWorld.Door door)
    {
        if (door != null && museumLockedModel.Contains(door.edge)) return true;
        return null;
    }

    /// <summary>One line of trouble on the museum's list (and once in the console): the part it names is off, the rest goes on.</summary>
    void MuseumNote(string line)
    {
        problem = problem == null ? line : problem + " " + line;
        Debug.LogWarning("Interactables showcase (museum): " + line);
    }

    /// <summary>The museum is not built: the run stays the game's own floor; one message; the kit's statics back; the plan (P) still works.</summary>
    void MuseumFail(string what, Exception e)
    {
        museumState = MuseumState.Failed;
        ClearMuseumTools();
        var line = "MUSEUM NOT BUILT: " + what + (e != null ? " (" + e.GetType().Name + ": " + e.Message + ")" : "")
            + ". This run is the game's own floor; P opens the plan, F2 the tours.";
        problem = problem == null ? line : problem + " " + line;
        Toast("MUSEUM", line);
        Debug.LogWarning("Interactables showcase (museum): " + line + (e != null ? "\n" + e : ""));
    }

    // ---------- the carve (90 §1 steps 3-5; Message A)

    /// <summary>
    /// The origin, the carve and the lamps. The carve is the station's own type: a StartGroupCarve with every museum edge
    /// forced, FrontRoomsMapCarve.From with StartGroupTemplate.StandIn and a stand-in layout (no station, corridor 0, X on
    /// room 2's sealed corner, E1 and E2 on room 1's arches), then the cache's one carve slot. False with the reason.
    /// </summary>
    bool PlanMuseum(out string why)
    {
        why = null;
        if (map.Cache == null)
        {
            why = "the map has no cache";
            return false;
        }
        // F24: the cache holds one carve; a station's would be dropped by ours (this run is not the museum's copy).
        if (map.HasStation || map.Cache.Carve != null)
        {
            why = "this run's map holds a station and its carve, so it is not the museum's copy";
            return false;
        }
        if (player == null)
        {
            why = "the game has no Player yet";
            return false;
        }
        var start = MapGrid.ChunkOf(map.CellOf(player.position));
        var o = new GridCoord(start.x, start.y + MuseumChunksNorth);
        // F20: clear of the start rooms, X and N, and of anything built; else whole chunks further north.
        for (var tries = 0; !MuseumClear(o, start); tries++)
        {
            if (tries >= MuseumShiftTries)
            {
                why = "no clear place " + MuseumChunksNorth + "-" + (MuseumChunksNorth + MuseumShiftTries) + " chunks north of the start";
                return false;
            }
            o = new GridCoord(o.x, o.y + 1);
        }
        museumChunk = o;
        museumCell0 = MapGrid.ChunkOrigin(o);
        museumMetres = Mathf.RoundToInt((o.y - start.y) * MapGrid.ChunkSize);

        var overlay = new StartGroupCarve();
        // No revisit shift on the museum's chunks (the copy has none either).
        overlay.pinned.Add(o);
        overlay.pinned.Add(new GridCoord(o.x + 1, o.y));
        for (var v = 0; v < MuseumRows; v++)
        for (var u = 0; u < MuseumCols; u++)
        {
            int ci, cj;
            if (MuseumRoomOf(u, v, out ci, out cj) < 0) continue;
            ForceMuseumEdge(overlay, u, v, u + 1, v);
            ForceMuseumEdge(overlay, u, v, u - 1, v);
            ForceMuseumEdge(overlay, u, v, u, v + 1);
            ForceMuseumEdge(overlay, u, v, u, v - 1);
        }
        // A zone's key lies at its site, in its home chunk: the two museum chunks' own keys go to sealed corners of the same chunk.
        AddMuseumKeyMove(overlay, o, MuseumCell(0, 0));
        AddMuseumKeyMove(overlay, new GridCoord(o.x + 1, o.y), MuseumCell(15, 0));
        var layout = new StartGroupLayout
        {
            door = MuseumCell(8, 0),
            station = new CellRect(0, 0, 0, 0),
            e1In = MuseumCell(3, 2),
            e1Out = MuseumCell(4, 2),
            e2In = MuseumCell(3, 1),
            e2Out = MuseumCell(4, 1),
            corridor = 0,
            front = MuseumCell(4, 6),
            exit = MuseumCell(4, 7),
        };
        var carve = FrontRoomsMapCarve.From(overlay, StartGroupTemplate.StandIn(4, 1, 4), layout, museumCell0);
        map.Cache.SetCarve(carve);
        // F23: the cache must hold this carve (a later check inside the carve's path would show here).
        if (map.Cache.Carve != carve)
        {
            why = "the map's cache did not keep the museum's carve";
            return false;
        }
        museumCarve = carve;
        MuseumLamps();

        // Room 1's six door fronts, what the kit dresses them as, then the kit's statics (before any museum chunk is built).
        museumLiveEdges.Clear();
        museumLockedModel.Clear();
        museumMembers.Clear();
        for (var slot = 1; slot <= 7; slot++)
        {
            long edge;
            if (MuseumEdgeOf(slot, out edge)) museumLiveEdges.Add(edge);
        }
        long signEdge, runEdge, standEdge;
        if (MuseumEdgeOf(3, out signEdge)) museumLockedModel.Add(signEdge);
        if (MuseumEdgeOf(6, out runEdge)) museumMembers[runEdge] = FrontRoomsDoorMember.Run;
        museumMember = 0;
        if (MuseumEdgeOf(7, out standEdge)) museumMembers[standEdge] = FrontRoomsDoorMember.Lobby;
        SetMuseumHooks();
        return true;
    }

    // The museum's chunks and the ring of chunks round them: not the start's, not X's or N's, nothing built; no reserved cell.
    bool MuseumClear(GridCoord o, GridCoord start)
    {
        for (var cy = o.y - 1; cy <= o.y + 1; cy++)
        for (var cx = o.x - 1; cx <= o.x + 2; cx++)
        {
            var c = new GridCoord(cx, cy);
            if (c == start || map.IsChunkBuiltAt(MapGrid.ChunkOrigin(c))) return false;
            if (map.HasExitArea && (MapGrid.ChunkOf(map.ExitCell) == c || MapGrid.ChunkOf(map.ExitFrontCell) == c)) return false;
        }
        var c0 = MapGrid.ChunkOrigin(o);
        const int n = MapGrid.ChunkCells;
        for (var y = -n; y < 2 * n; y++)
        for (var x = -n; x < 3 * n; x++)
            if (map.IsReserved(new GridCoord(c0.x + x, c0.y + y))) return false;
        return true;
    }

    void ForceMuseumEdge(StartGroupCarve overlay, int ua, int va, int ub, int vb) =>
        overlay.Force(MuseumCell(ua, va), MuseumCell(ub, vb), MuseumEdge(ua, va, ub, vb), StartGroupCarve.Source.Template);

    void AddMuseumKeyMove(StartGroupCarve overlay, GridCoord zone, GridCoord to)
    {
        // Pure: the generator's zone (memoised); a Tall zone has no key (the copy has none).
        var z = map.Cache.Generator.Zone(zone);
        if (z.height == ZoneHeight.Tall) return;
        var site = new GridCoord(Mathf.FloorToInt(z.siteX / MapGrid.CellSize), Mathf.FloorToInt(z.siteZ / MapGrid.CellSize));
        overlay.keyMoves.Add(new StartGroupKeyMove { zone = zone, from = site, site = site, to = to });
    }

    /// <summary>Steady in the hub, lanes, alcoves and room 1's lit closets; Off in the sealed cells, the two maze blocks and the ring.</summary>
    void MuseumLamps()
    {
        for (var v = -MuseumRing; v < MuseumRows + MuseumRing; v++)
        for (var u = -MuseumRing; u < MuseumCols + MuseumRing; u++)
            map.SetLampMode(MuseumCell(u, v), MuseumLamp(u, v));
    }

    static ModuleLamp MuseumLamp(int u, int v)
    {
        int i, j;
        var room = MuseumRoomOf(u, v, out i, out j);
        if (room < 0) return ModuleLamp.Off;
        if (room == 0) return ModuleLamp.Steady;
        var part = MuseumPartOf(i, j);
        if (part == MuseumPart.Corner) return ModuleLamp.Off;
        // Room 1's closets stand behind real doors: lit, so a lite reads live. Rooms 2-4's bays are sealed for now.
        if (part == MuseumPart.Bay) return room == 1 ? ModuleLamp.Steady : ModuleLamp.Off;
        return ModuleLamp.Steady;
    }

    // ---------- the layout as rules (80 §4; the plan page draws the same, .Plan.cs LocalRect)

    /// <summary>The room of a museum cell (0 the hub, 1-4) and its room-local cell (i from the visitor's left, j from the hub side); -1 outside or in a maze block.</summary>
    static int MuseumRoomOf(int u, int v, out int i, out int j)
    {
        i = j = 0;
        if (u < 0 || u >= MuseumCols || v < 0 || v >= MuseumRows) return -1;
        if (v < 4)
        {
            if (u < 4)
            {
                j = 3 - u;
                i = v;
                return 1;
            }
            if (u < 12) return 0;
            j = u - 12;
            i = 3 - v;
            return 4;
        }
        if (u >= 4 && u < 8)
        {
            i = u - 4;
            j = v - 4;
            return 2;
        }
        if (u >= 8 && u < 12)
        {
            i = u - 8;
            j = v - 4;
            return 3;
        }
        return -1;
    }

    // Bays .1-.3 up the right wall (i 3), .5-.7 down the left (i 0), the alcove .4 and the closed corners at the far side (j 3).
    static MuseumPart MuseumPartOf(int i, int j)
    {
        var side = i == 0 || i == 3;
        if (j == 3) return side ? MuseumPart.Corner : MuseumPart.Alcove;
        return side ? MuseumPart.Bay : MuseumPart.Lane;
    }

    static bool BayOfSlot(int slot, out int i, out int j)
    {
        i = j = 0;
        if (slot >= 1 && slot <= 3)
        {
            i = 3;
            j = slot - 1;
            return true;
        }
        if (slot >= 5 && slot <= 7)
        {
            i = 0;
            j = 7 - slot;
            return true;
        }
        return false;
    }

    /// <summary>The forced kind of the edge between two neighbouring museum cells (one may lie outside: the rim is Wall).</summary>
    static EdgeKind MuseumEdge(int ua, int va, int ub, int vb)
    {
        int ia, ja, ib, jb;
        var ra = MuseumRoomOf(ua, va, out ia, out ja);
        var rb = MuseumRoomOf(ub, vb, out ib, out jb);
        if (ra < 0 || rb < 0) return EdgeKind.Wall;
        // Hub 0: one 24 x 12 m hall.
        if (ra == 0 && rb == 0) return EdgeKind.Open;
        if (ra != rb)
        {
            if (ra != 0 && rb != 0) return EdgeKind.Wall;
            // A room's two arches on the hub wall: E at lane (2, 0), right of the middle facing in; X at lane (1, 0).
            var i = ra == 0 ? ib : ia;
            var j = ra == 0 ? jb : ja;
            return j == 0 && (i == 1 || i == 2) ? EdgeKind.Arch : EdgeKind.Wall;
        }
        var pa = MuseumPartOf(ia, ja);
        var pb = MuseumPartOf(ib, jb);
        var aOpen = pa == MuseumPart.Lane || pa == MuseumPart.Alcove;
        var bOpen = pb == MuseumPart.Lane || pb == MuseumPart.Alcove;
        if (aOpen && bOpen) return EdgeKind.Open;
        // A bay's front: room 1 a real door, rooms 2-4 a wall with the station's framed code (their devices come later).
        if (pa == MuseumPart.Bay && pb == MuseumPart.Lane) return ra == 1 ? EdgeKind.Door : EdgeKind.Wall;
        if (pa == MuseumPart.Lane && pb == MuseumPart.Bay) return rb == 1 ? EdgeKind.Door : EdgeKind.Wall;
        return EdgeKind.Wall;
    }

    static void LocalToMuseum(int room, int i, int j, out int u, out int v)
    {
        if (room == 1)
        {
            u = 3 - j;
            v = i;
        }
        else if (room == 2 || room == 3)
        {
            u = (room == 2 ? 4 : 8) + i;
            v = 4 + j;
        }
        else
        {
            u = 12 + j;
            v = 3 - i;
        }
    }

    /// <summary>A room-local point (x along i, y along j, in cells, the room spanning 0-4 both ways) in museum cells.</summary>
    static Vector2 LocalPoint(int room, float x, float y)
    {
        if (room == 1) return new Vector2(4f - y, x);
        if (room == 2 || room == 3) return new Vector2((room == 2 ? 4f : 8f) + x, 4f + y);
        return new Vector2(12f + y, 4f - x);
    }

    static Vector2 LocalDir(int room, float dx, float dy)
    {
        if (room == 1) return new Vector2(-dy, dx);
        if (room == 2 || room == 3) return new Vector2(dx, dy);
        return new Vector2(dy, -dx);
    }

    GridCoord MuseumCell(int u, int v) => new GridCoord(museumCell0.x + u, museumCell0.y + v);

    Vector3 MuseumWorld(Vector2 p, float height) =>
        map.transform.TransformPoint(new Vector3((museumCell0.x + p.x) * MapGrid.CellSize, height, (museumCell0.y + p.y) * MapGrid.CellSize));

    Vector3 MuseumWorldDir(Vector2 d) => map.transform.rotation * new Vector3(d.x, 0f, d.y);

    // A bay's front in map cells: the bay, and the lane cell in front of it.
    bool MuseumFront(int room, int slot, out GridCoord bay, out GridCoord lane)
    {
        bay = lane = default(GridCoord);
        int i, j, u, v;
        if (!BayOfSlot(slot, out i, out j)) return false;
        LocalToMuseum(room, i, j, out u, out v);
        bay = MuseumCell(u, v);
        LocalToMuseum(room, i == 3 ? 2 : 1, j, out u, out v);
        lane = MuseumCell(u, v);
        return true;
    }

    bool MuseumEdgeOf(int slot, out long edge)
    {
        edge = 0L;
        GridCoord bay, lane;
        if (!MuseumFront(1, slot, out bay, out lane)) return false;
        edge = MapGrid.EdgeId(bay, lane);
        return true;
    }

    /// <summary>Room 1's built door at a station (rebuilt chunks make new Door records: always asked of the map), or null.</summary>
    FrontRoomsMapWorld.Door MuseumDoor(int slot)
    {
        GridCoord bay, lane;
        if (map == null || !MuseumFront(1, slot, out bay, out lane)) return null;
        return map.DoorBetween(bay, lane);
    }

    bool IsMuseumDoor(FrontRoomsMapWorld.Door door) => door != null && museumLiveEdges.Contains(door.edge);

    /// <summary>Where to stand for a station and where its framed code hangs: the hub's 0.1 and 0.2, a room's bays and alcove.</summary>
    static MuseumSpot MuseumSpotOf(int room, int slot)
    {
        var s = new MuseumSpot();
        if (room == 0)
        {
            if (slot != 1 && slot != 2) return s;
            // The overview strip and the locker frame on the hub's south wall, left of the spawn (the plan's SlotRect).
            var u = slot == 1 ? 5f : 6.5f;
            s.stand = new Vector2(u, 1.3f);
            s.face = new Vector2(0f, -1f);
            s.anchor = new Vector2(u, 0f);
            s.normal = new Vector2(0f, 1f);
            s.height = WallTagHeight;
            s.ok = true;
            return s;
        }
        if (room < 1 || room > 4) return s;
        Vector2 stand, face, anchor, normal;
        float height;
        if (slot == 4)
        {
            // The alcove: from the lane's far end, its code on the alcove's back wall.
            stand = new Vector2(2f, 2.3f);
            face = new Vector2(0f, 1f);
            anchor = new Vector2(2f, 4f);
            normal = new Vector2(0f, -1f);
            height = WallTagHeight;
        }
        else
        {
            int i, j;
            if (!BayOfSlot(slot, out i, out j)) return s;
            // The lane cell in front of the bay, facing its front; the code above the door (room 1) or on the wall.
            var right = i == 3;
            stand = new Vector2(right ? 2.5f : 1.5f, j + .5f);
            face = new Vector2(right ? 1f : -1f, 0f);
            anchor = new Vector2(right ? 3f : 1f, j + .5f);
            normal = new Vector2(right ? -1f : 1f, 0f);
            height = room == 1 ? DoorTagHeight : WallTagHeight;
        }
        s.stand = LocalPoint(room, stand.x, stand.y);
        s.face = LocalDir(room, face.x, face.y);
        s.anchor = LocalPoint(room, anchor.x, anchor.y);
        s.normal = LocalDir(room, normal.x, normal.y);
        s.height = height;
        s.ok = true;
        return s;
    }

    // A room's sign in the hub, on the wall between its two arches, above them.
    static MuseumSpot MuseumSignSpot(int room)
    {
        var s = new MuseumSpot { height = SignTagHeight, ok = room >= 1 && room <= 4 };
        if (room == 1)
        {
            s.anchor = new Vector2(4f, 2f);
            s.normal = new Vector2(1f, 0f);
        }
        else if (room == 2 || room == 3)
        {
            s.anchor = new Vector2(room == 2 ? 6f : 10f, 4f);
            s.normal = new Vector2(0f, -1f);
        }
        else
        {
            s.anchor = new Vector2(12f, 2f);
            s.normal = new Vector2(-1f, 0f);
        }
        return s;
    }

    // ---------- the build and the walk in (90 §1 step 6)

    /// <summary>
    /// The frame after the carve: the museum's chunks and their west and south neighbours built now (a chunk builds its own
    /// east and north walls, so the museum's west and south rims are theirs), the stream let go, the player in at the hub.
    /// </summary>
    void BuildMuseum()
    {
        museumState = MuseumState.Building;
        try
        {
            // F23: still the cache's carve (nothing between MapRunStarted and this frame may take the one slot).
            if (museumCarve == null || map.Cache == null || map.Cache.Carve != museumCarve)
            {
                MuseumFail("the map's cache no longer holds the museum's carve", null);
                return;
            }
            var o = museumChunk;
            map.RebuildChunk(o);
            map.RebuildChunk(new GridCoord(o.x + 1, o.y));
            map.RebuildChunk(new GridCoord(o.x - 1, o.y));
            map.RebuildChunk(new GridCoord(o.x, o.y - 1));
            map.RebuildChunk(new GridCoord(o.x + 1, o.y - 1));
            if (!map.IsChunkBuiltAt(MuseumCell(0, 0)) || !map.IsChunkBuiltAt(MuseumCell(8, 0)))
            {
                MuseumFail("a museum chunk did not build (the console has the map's error)", null);
                return;
            }
            var missing = 0;
            for (var slot = 1; slot <= 7; slot++)
                if (slot != 4 && MuseumDoor(slot) == null) missing++;
            if (missing > 0) MuseumNote(missing + " of room 1's six doors were not built: those stations stand as frames.");
            if (player == null)
            {
                MuseumFail("there is no player to walk in", null);
                return;
            }
            // The stream follows the player from now on (it held the start door); the start rooms drop behind.
            map.StreamFocus = null;
            Physics.SyncTransforms();
            MuseumMove(HubSpawn, MuseumWorld(HubSpawn + new Vector2(0f, 3f), ModuleUnits.PlayerEye));
            museumState = MuseumState.Built;
            museumMarkRoom = 0;
            museumMarkSlot = -1;
            museumTagClock = 0f;
            Toast("MUSEUM  ·  0  HUB", "Hub 0 and four rooms of the game's own cells and doors, " + museumMetres + " m north of the start. Room 1 is live; rooms 2-4 stand as framed codes.  [ ] rooms  ·  1-7 stations  ·  P the plan.");
            var sign = RoomSign(0);
            if (sign != null)
            {
                toastDecide = sign.decide;
                toastUntil = Time.unscaledTime + 8f;
            }
        }
        catch (Exception e)
        {
            MuseumFail("the build threw", e);
        }
    }

    /// <summary>The player to a museum spot (the map's floor under it; a step round a leaf in the way), facing a point.</summary>
    void MuseumMove(Vector2 stand, Vector3 look)
    {
        if (player == null || map == null) return;
        var spot = MuseumWorld(stand, 0f);
        Vector3 feet;
        if (!TryFloor(spot, out feet))
        {
            var found = false;
            for (var k = 0; k < 8 && !found; k++)
                found = TryFloor(spot + Quaternion.Euler(0f, k * 45f, 0f) * Vector3.forward * .6f, out feet);
            // The controller settles a body in a door's swing on its own.
            if (!found) feet = spot;
        }
        MoveAndFace(feet, look);
    }

    // ---------- per frame

    /// <summary>The museum's frame: the title's start, the build, the framed codes; its keys (P the plan in every state).</summary>
    void UpdateMuseum(Keyboard keys)
    {
        if (map == null) UpdateMuseumTitle();
        else if (museumState == MuseumState.Carved) BuildMuseum();
        else if (museumState == MuseumState.Built) TickMuseum();
        if (keys == null || paused || FrontRooms3DGame.InspectingNow) return;
        if (keys.pKey.wasPressedThisFrame)
        {
            ToggleMuseumPlan();
            return;
        }
        if (museumPlanOpen)
        {
            UpdateMuseumPlan(keys);
            return;
        }
        if (map == null || museumState != MuseumState.Built) return;
        if (keys.gKey.wasPressedThisFrame) listShown = !listShown;
        var here = MuseumRoomHere();
        if (here < 0) here = 0;
        if (keys.leftBracketKey.wasPressedThisFrame || keys.pageUpKey.wasPressedThisFrame) MuseumGoToRoom((here + 4) % 5);
        else if (keys.rightBracketKey.wasPressedThisFrame || keys.pageDownKey.wasPressedThisFrame) MuseumGoToRoom((here + 1) % 5);
        else if (keys.digit0Key.wasPressedThisFrame || keys.numpad0Key.wasPressedThisFrame) MuseumGoToRoom(0);
        else if (keys.rKey.wasPressedThisFrame) MuseumReset();
        else if (keys.bKey.wasPressedThisFrame) MuseumBreak();
        else
        {
            var slot = SlotDown(keys);
            if (slot > 0) MuseumKeyStation(here, slot);
        }
    }

    /// <summary>The title, until the run starts: the game's private RequestTitleStart every half second (it returns until the title is ready).</summary>
    void UpdateMuseumTitle()
    {
        if (game == null || museumState != MuseumState.Title || museumStartStopped || TitleStartMethod == null || paused) return;
        var now = Time.unscaledTime;
        if (now < museumNextTry) return;
        museumNextTry = now + MuseumStartEvery;
        if (now - museumLoadedAt > MuseumStartGiveUp)
        {
            museumStartStopped = true;
            MuseumNote("The run did not start by itself in " + Mathf.RoundToInt(MuseumStartGiveUp) + " s: SPACE starts it, the title's own way.");
            return;
        }
        try
        {
            TitleStartMethod.Invoke(game, null);
        }
        catch (Exception e)
        {
            museumStartStopped = true;
            var inner = e.InnerException ?? e;
            MuseumNote("The automatic start failed (" + inner.GetType().Name + ": " + inner.Message + "): SPACE starts the run, the title's own way.");
        }
    }

    void TickMuseum()
    {
        museumTagClock -= Time.unscaledDeltaTime;
        if (museumTagClock > 0f) return;
        museumTagClock = MuseumTagSeconds;
        RefreshMuseumTags();
    }

    int MuseumRoomHere()
    {
        if (map == null || player == null || museumState != MuseumState.Built) return -1;
        var c = map.CellOf(player.position);
        int i, j;
        return MuseumRoomOf(c.x - museumCell0.x, c.y - museumCell0.y, out i, out j);
    }

    /// <summary>The framed codes of the room the player is in (in the hub: its two stations and the four room signs), each with
    /// its line of sight from the eye; and the station whose stop the player stands at.</summary>
    void RefreshMuseumTags()
    {
        museumTags.Clear();
        museumNearRoom = museumNearSlot = -1;
        var room = MuseumRoomHere();
        if (room < 0) return;
        var used = 0;
        if (room == 0)
        {
            for (var r = 1; r <= 4; r++) AddMuseumTag(ref used, r, 0, MuseumSignSpot(r));
            AddMuseumTag(ref used, 0, 1, MuseumSpotOf(0, 1));
            AddMuseumTag(ref used, 0, 2, MuseumSpotOf(0, 2));
        }
        else
            for (var slot = 1; slot <= 7; slot++) AddMuseumTag(ref used, room, slot, MuseumSpotOf(room, slot));
        if (view != null)
        {
            var eye = view.transform.position;
            for (var t = 0; t < museumTags.Count; t++)
            {
                var tag = museumTags[t];
                RaycastHit hit;
                tag.occluded = Physics.Linecast(eye, tag.at, out hit, ~0, QueryTriggerInteraction.Ignore) && hit.distance < Vector3.Distance(eye, tag.at) - .2f;
            }
        }
        var feet = Flat(Feet);
        var best = PlacardNear;
        var last = room == 0 ? 2 : 7;
        for (var slot = 1; slot <= last; slot++)
        {
            var spot = MuseumSpotOf(room, slot);
            if (!spot.ok) continue;
            var d = Vector3.Distance(feet, Flat(MuseumWorld(spot.stand, 0f)));
            if (d >= best) continue;
            best = d;
            museumNearRoom = room;
            museumNearSlot = slot;
        }
    }

    void AddMuseumTag(ref int used, int room, int slot, MuseumSpot spot)
    {
        if (!spot.ok) return;
        if (used == museumTagPool.Count) museumTagPool.Add(new MuseumTag());
        var t = museumTagPool[used++];
        t.room = room;
        t.slot = slot;
        t.normal = MuseumWorldDir(spot.normal).normalized;
        t.at = MuseumWorld(spot.anchor, spot.height) + t.normal * (ModuleUnits.WallHalf + .06f);
        t.occluded = false;
        museumTags.Add(t);
    }

    /// <summary>The station the left card shows: the stop the player stands at, else the one last walked to (8 s, same room).</summary>
    bool MuseumMarked(out int room, out int slot)
    {
        room = slot = -1;
        if (museumNearRoom >= 0)
        {
            room = museumNearRoom;
            slot = museumNearSlot;
            return true;
        }
        if (museumMarkRoom >= 0 && museumMarkSlot >= 1 && museumMarkRoom == MuseumRoomHere() && Time.unscaledTime < museumMarkUntil)
        {
            room = museumMarkRoom;
            slot = museumMarkSlot;
            return true;
        }
        return false;
    }

    // ---------- the museum's keys

    void ToggleMuseumPlan()
    {
        museumPlanOpen = !museumPlanOpen;
        if (!museumPlanOpen) return;
        // The plan opens on the room the player is in, with the station they stand at selected.
        var here = MuseumRoomHere();
        SelectRoom(here >= 0 ? here : 0);
        int room, slot;
        if (!MuseumMarked(out room, out slot) || room != planRoom) return;
        var st = MuseumRow(room, slot);
        var all = FrontRoomsShowcaseTable.Stations;
        if (st != null && all != null) planIndex = Array.IndexOf(all, st);
    }

    /// <summary>The plan over the game (.Plan.cs keys); ENTER on a station built here walks to it, the media bay is scene 3's.</summary>
    void UpdateMuseumPlan(Keyboard keys)
    {
        if (keys.enterKey.wasPressedThisFrame || keys.numpadEnterKey.wasPressedThisFrame)
        {
            var st = SelectedStation;
            if (st != null && MuseumBuiltHere(st))
            {
                museumPlanOpen = false;
                MuseumGoToRow(st);
                return;
            }
            if (st != null && st.tour == 1)
            {
                Toast(LabelOf(st), "The media bay is scene 3's own: F2, then 1.");
                return;
            }
        }
        UpdatePlan(keys);
    }

    /// <summary>A row the plan shows as HERE: room 1's, once the museum stands.</summary>
    bool MuseumBuiltHere(FrontRoomsShowcaseTable.Station st) =>
        tour == Tour.Museum && museumState == MuseumState.Built && st != null && FrontRoomsShowcaseTable.RoomOf(st) == 1;

    void MuseumGoToRow(FrontRoomsShowcaseTable.Station st)
    {
        var room = FrontRoomsShowcaseTable.RoomOf(st);
        var slot = FrontRoomsShowcaseTable.SlotOf(st);
        // 1.1B from the plan: the E door, switched before the walk (1.1A: the body door).
        if (room == 1 && slot == 1 && map != null && (FrontRoomsShowcaseTable.VariantOf(st) == 'B') == map.BodyDoors && GateRefusal() == null) MuseumSwitchDoors();
        MuseumGoToStation(room, slot);
    }

    void MuseumKeyStation(int room, int slot)
    {
        if (room == 0 && slot > 2)
        {
            Toast("0  HUB", "The hub holds 0.1 and 0.2 (keys 1 and 2); [ ] goes from room to room.");
            return;
        }
        // Again at the same stop: its next variant (1.1 A/B, 1.7 A-D).
        if (room == museumMarkRoom && slot == museumMarkSlot && MuseumAtStop(room, slot))
        {
            MuseumStepVariant(room, slot);
            return;
        }
        MuseumGoToStation(room, slot);
    }

    bool MuseumAtStop(int room, int slot)
    {
        var spot = MuseumSpotOf(room, slot);
        return spot.ok && player != null && Vector3.Distance(Flat(Feet), Flat(MuseumWorld(spot.stand, 0f))) < StopNear;
    }

    void MuseumGoToStation(int room, int slot)
    {
        var spot = MuseumSpotOf(room, slot);
        var row = MuseumRow(room, slot);
        var title = row != null ? LabelOf(row) : room + "." + slot;
        if (!spot.ok) return;
        // The tours' gate (.Rooms.cs): never during a close-up, a shot, a climb, a pull, the seal or the floor change.
        var why = GateRefusal();
        if (why != null)
        {
            Toast(title, why);
            return;
        }
        MuseumMove(spot.stand, MuseumWorld(spot.anchor, room == 1 && slot != 4 ? 1.3f : 1.4f));
        museumMarkRoom = room;
        museumMarkSlot = slot;
        museumMarkUntil = Time.unscaledTime + MuseumCardSeconds;
        museumTagClock = 0f;
        Toast(title, MuseumHere(room, slot, row));
    }

    /// <summary>[ ] and 0: into a room just inside its E arch, facing the alcove; the hub at the spawn, facing rooms 2 and 3.</summary>
    void MuseumGoToRoom(int room)
    {
        var title = MuseumRoomTitle(room);
        var why = GateRefusal();
        if (why != null)
        {
            Toast(title, why);
            return;
        }
        Vector2 stand, face;
        if (room == 0)
        {
            stand = HubSpawn;
            face = new Vector2(0f, 1f);
        }
        else
        {
            stand = LocalPoint(room, 2.5f, .7f);
            face = LocalDir(room, 0f, 1f);
        }
        MuseumMove(stand, MuseumWorld(stand + face * 3f, ModuleUnits.PlayerEye));
        museumMarkRoom = room;
        museumMarkSlot = -1;
        museumTagClock = 0f;
        var sign = RoomSign(room);
        Toast(title, room == 0 ? "The board on the left: P opens the plan of every station." : room == 1 ? "Live: six doors of the game's own and an alcove. 1-7 a station, again for its next variant."
            : "Framed codes for now: each station's code, what it is and where it can be tried today (P, then ENTER).");
        if (sign != null)
        {
            toastDecide = sign.decide;
            toastUntil = Time.unscaledTime + 6f;
        }
    }

    void MuseumStepVariant(int room, int slot)
    {
        var row = MuseumRow(room, slot);
        var why = GateRefusal();
        if (why != null)
        {
            Toast(row != null ? LabelOf(row) : "", why);
            return;
        }
        if (room == 1 && slot == 1) MuseumSwitchDoors();
        else if (room == 1 && slot == 7) MuseumNextMember();
        row = MuseumRow(room, slot);
        museumMarkUntil = Time.unscaledTime + MuseumCardSeconds;
        Toast(row != null ? LabelOf(row) : "", MuseumHere(room, slot, row));
    }

    /// <summary>1.1 A/B: the map's own switch (body doors are one rule per floor), then room 1's chunk built again, since a door's hinge side is chosen when it is built.</summary>
    void MuseumSwitchDoors()
    {
        var toBody = !map.BodyDoors;
        if (toBody && !FrontRooms3DGame.BodyDoorsCompiled) return;
        try
        {
            map.BodyDoors = toBody;
            map.RebuildChunk(museumChunk);
            Physics.SyncTransforms();
        }
        catch (Exception e)
        {
            MuseumNote("1.1's switch failed (" + e.GetType().Name + ": " + e.Message + ").");
        }
        museumTagClock = 0f;
    }

    /// <summary>1.7: the next of the kit's four members on the same door, dressed again in place (render only).</summary>
    void MuseumNextMember()
    {
        long edge;
        if (!MuseumEdgeOf(7, out edge)) return;
        museumMember = (museumMember + 1) % 4;
        museumMembers[edge] = (FrontRoomsDoorMember)museumMember;
        var door = MuseumDoor(7);
        if (door == null) return;
        try
        {
            FrontRoomsInteractableKit.RedressDoor(map, door);
        }
        catch (Exception e)
        {
            MuseumNote("1.7's member could not be dressed (" + e.GetType().Name + ": " + e.Message + "); the map's plain leaf shows.");
        }
    }

    /// <summary>R: room 1's doors back as built (the map's ResetDoorForTools: shut, latched, whole), only while the game plays.</summary>
    void MuseumReset()
    {
        // Outside Playing the game reads R itself (END RUN in the pause): the museum never takes it then.
        if (PhaseField == null)
        {
            Toast("R  RESET", "Off: FrontRooms3DGame.phase was not found (renamed?), so R stays the game's.");
            return;
        }
        var phase = PhaseField.GetValue(game);
        if (phase == null || phase.ToString() != "Playing") return;
        var why = GateRefusal();
        if (why != null)
        {
            Toast("R  RESET", why);
            return;
        }
        var n = 0;
        var broken = false;
        for (var slot = 1; slot <= 7; slot++)
        {
            var door = MuseumDoor(slot);
            if (door == null) continue;
            broken |= door.broken;
            map.ResetDoorForTools(door);
            n++;
        }
        if (broken)
        {
            // A broken leaf's dressing (the break's pieces) comes back whole only with a fresh dress.
            try
            {
                map.RebuildChunk(museumChunk);
                Physics.SyncTransforms();
            }
            catch (Exception e)
            {
                MuseumNote("R's rebuild failed (" + e.GetType().Name + ": " + e.Message + ").");
            }
        }
        museumTagClock = 0f;
        Toast("R  ·  ROOM 1 AS BUILT", n + " doors shut, latched and whole again." + (broken ? " The broken one too: B breaks it again." : ""));
    }

    /// <summary>B: a museum door broken with the map's BreakDoor, as the Relay breaks one: the door in view within 6 m, else 1.5's within 4 m.</summary>
    void MuseumBreak()
    {
        var row = FrontRoomsShowcaseTable.Find("1.5");
        var title = row != null ? LabelOf(row) : Names[(int)Kind.BrokenDoor];
        var why = GateRefusal();
        if (why != null)
        {
            Toast(title, why);
            return;
        }
        FrontRoomsMapWorld.Door door = null;
        RaycastHit hit;
        if (view != null && Physics.Raycast(view.ViewportPointToRay(new Vector3(.5f, .5f, 0f)), out hit, 6f, ~0, QueryTriggerInteraction.Ignore))
            door = map.DoorOf(hit.collider);
        if (!IsMuseumDoor(door)) door = null;
        if (door == null)
        {
            var kept = MuseumDoor(5);
            if (kept != null && Vector3.Distance(Feet, kept.position) < 4f) door = kept;
        }
        if (door == null)
        {
            Toast(title, "B breaks a museum door: look at one within 6 m (1.5 is the one kept for it). R makes them whole.");
            return;
        }
        if (door.broken)
        {
            Toast(title, "Broken already: E says WON'T SHUT. R makes it whole.");
            return;
        }
        // From the player's side, so the leaf is thrown away from the body (the Relay breaks into the room it stands in).
        map.BreakDoor(door, Feet);
        museumTagClock = 0f;
        Toast(title, "Broken as the Relay breaks a door: it hangs askew, E says WON'T SHUT. R makes it whole.");
    }

    // ---------- the words (the table's rows, and what the museum adds)

    /// <summary>The table's row at a stop: 1.1 is A or B as the map's doors are now; 1.7 has one row for its four members.</summary>
    FrontRoomsShowcaseTable.Station MuseumRow(int room, int slot)
    {
        if (room == 1 && slot == 1)
            return FrontRoomsShowcaseTable.Find(map != null && !map.BodyDoors ? "1.1B" : "1.1A") ?? FrontRoomsShowcaseTable.At(1, 1);
        return FrontRoomsShowcaseTable.At(room, slot);
    }

    bool MuseumLive(int room, int slot)
    {
        if (museumState != MuseumState.Built || map == null || room != 1) return false;
        return slot == 4 || MuseumDoor(slot) != null;
    }

    static string MemberName(int member) => member == 1 ? "OFFICE" : member == 2 ? "RUN!" : member == 3 ? "EXIT" : "LOBBY";

    string MemberLetter() => ((char)('A' + Mathf.Clamp(museumMember, 0, 3))).ToString();

    static string MuseumRoomTitle(int room)
    {
        var sign = RoomSign(room);
        return room + "  " + (sign != null ? sign.name ?? "" : "");
    }

    /// <summary>What this stop is in the museum now, one line: a live station's state and its key, or a frame's tour of today.</summary>
    string MuseumHere(int room, int slot, FrontRoomsShowcaseTable.Station st)
    {
        if (slot == 4 && room >= 1 && room <= 4)
            return "An empty alcove in game light. Its 1990 corner (" + (room == 1 ? "this room's doors together" : "this room's things together") + ") is dressed later.";
        if (MuseumLive(room, slot))
        {
            switch (slot)
            {
                case 1:
                    if (!FrontRooms3DGame.BodyDoorsCompiled) return "B  E DOOR: body doors need Relay v2 compiled (FRONTROOMS_RELAY_V2), so this build has E doors only.";
                    return map.BodyDoors
                        ? "A  BODY DOOR now. 1 again: B, the E door. The game switches every door of a floor at once, so every museum door switches with it."
                        : "B  E DOOR now. 1 again: A, the body door. Every museum door switched with it (one rule per floor).";
                case 2:
                    return "Its closet is lit, so the wired-glass lite reads live. Every dressed leaf has one; this is the one to judge.";
                case 3:
                    return "The LOCKED model shown on a free door (the kit's DoorLockedOverride, render only): the sign hangs, the door still opens.";
                case 5:
                {
                    var door = MuseumDoor(5);
                    return door != null && door.broken
                        ? "BROKEN: it hangs askew and E says WON'T SHUT. R makes every museum door whole again."
                        : "WHOLE. B, looking at it, breaks it as the Relay does; then E says WON'T SHUT. R makes it whole again.";
                }
                case 6:
                    return "The Run! goal door's look (the kit's RUN member, free). Nothing arms here: the corridor is ROOM TYPES R11 and tour 3.";
                case 7:
                    return "MEMBER " + MemberLetter() + "  " + MemberName(museumMember) + ". 7 again: the next member (A Lobby, B Office, C Run!, D Exit).";
            }
            return "";
        }
        if (st == null) return "";
        var today = st.tour >= 2 && st.tour <= 4 ? "tour " + st.tour + " (P, then ENTER on " + st.code + ")" : st.tour == 1 ? "the media bay (F2, then 1)" : "in no tour";
        return "FRAME: built here in a later increment. Today: " + today + ".";
    }

    // A museum-only Try line where the table's would mislead here (1.6: the tours' line is about walking to a Run! room).
    static string MuseumTryLine(int room, int slot) =>
        room == 1 && slot == 6 ? "Look at the leaf and its lite: the Run! goal door's member. The corridor itself: ROOM TYPES R11, tour 3." : null;

    /// <summary>The short state of a row, on the list and under its framed code.</summary>
    string MuseumRowTag(FrontRoomsShowcaseTable.Station st)
    {
        if (st == null) return "";
        var room = FrontRoomsShowcaseTable.RoomOf(st);
        var slot = FrontRoomsShowcaseTable.SlotOf(st);
        if (slot == 4 && room >= 1) return "OPEN  ·  GAME LIGHT";
        if (MuseumLive(room, slot))
        {
            if (slot == 1) return (FrontRoomsShowcaseTable.VariantOf(st) == 'B') == !map.BodyDoors ? "LIVE  ·  NOW" : "LIVE  ·  1 AGAIN";
            if (slot == 5)
            {
                var door = MuseumDoor(5);
                return door != null && door.broken ? "LIVE  ·  BROKEN" : "LIVE  ·  B BREAKS IT";
            }
            if (slot == 7) return "LIVE  ·  MEMBER " + MemberLetter();
            return "LIVE";
        }
        if (st.tour >= 2 && st.tour <= 4) return "FRAME  ·  TOUR " + st.tour;
        if (st.tour == 1) return "FRAME  ·  MEDIA BAY";
        return "FRAME";
    }

    // ---------- drawing (the guide's faces and keyline; framed codes are drawn on the screen over their wall: no world mesh)

    /// <summary>Menu 5 on screen: the plan (P), the title's line, or the framed codes, the room's list and the station's card.</summary>
    void DrawMuseum(float margin, float s)
    {
        if (museumPlanOpen)
        {
            DrawPlan(margin, s);
            Text(new Rect(margin, margin * .35f, Screen.width - 2f * margin, 24f * s), "P  BACK TO THE MUSEUM", statusStyle, Accent);
            return;
        }
        var w = 560f * s;
        var x = Screen.width - margin - w;
        var y = margin;
        if (map == null || museumState != MuseumState.Built)
        {
            Text(new Rect(x, y, w, 40f * s), TourName(tour), titleStyle, Paper);
            y += 40f * s;
            string line;
            if (map != null) line = museumState == MuseumState.Failed ? "This run is the game's own floor." : "Building the museum...";
            else if (museumState == MuseumState.Failed) line = "";
            else line = TitleStartMethod != null && !museumStartStopped ? "The run starts by itself: no title crawl, no Relay." : "SPACE starts the run (the game's own title).";
            y = Para(x, y, w, line, lineStyle, Paper, .9f, 4f * s);
            y = Para(x, y, w, "P  THE PLAN  ·  F2 TOURS", lineStyle, Accent, .95f, 8f * s);
            Para(x, y, w, problem, smallStyle, Accent, 1f, 0f);
        }
        else
        {
            DrawMuseumTags(s);
            if (listShown)
            {
                DrawMuseumColumn(x, y, w, s);
                DrawMuseumCard(margin, s);
            }
            else Text(new Rect(x, y, w, 24f * s), "G · MUSEUM", lineStyle, Paper, .85f);
        }
        if (Time.unscaledTime < toastUntil && toastTitle != null)
        {
            var tw = Mathf.Min(820f * s, Screen.width - 2f * margin);
            DrawToast((Screen.width - tw) * .5f, Screen.height * .2f, tw, s);
        }
    }

    /// <summary>The framed codes in view: each over its wall face, toward the player, fading with distance; none through a wall.</summary>
    void DrawMuseumTags(float s)
    {
        if (view == null) return;
        var cam = view.transform.position;
        for (var i = 0; i < museumTags.Count; i++)
        {
            var t = museumTags[i];
            if (t.occluded) continue;
            var to = cam - t.at;
            if (Vector3.Dot(to, t.normal) <= .05f) continue;
            var d = to.magnitude;
            if (d > MuseumTagFar || d < .4f) continue;
            var sp = view.WorldToScreenPoint(t.at);
            if (sp.z <= .2f) continue;
            string code, kind, line;
            bool live;
            MuseumTagText(t, out code, out kind, out line, out live);
            DrawMuseumFrame(sp.x, Screen.height - sp.y, code, kind, line, live, Mathf.Clamp01((MuseumTagFar - d) / 5f), s);
        }
    }

    void MuseumTagText(MuseumTag t, out string code, out string kind, out string line, out bool live)
    {
        if (t.slot == 0)
        {
            var sign = RoomSign(t.room);
            code = t.room.ToString();
            kind = sign != null ? sign.name ?? "" : "";
            live = t.room == 1;
            line = live ? "LIVE  ·  SIX DOORS AND AN ALCOVE" : "FRAMED CODES  ·  P FOR THE PLAN";
            return;
        }
        var st = MuseumRow(t.room, t.slot);
        code = st != null ? st.code ?? "" : t.room + "." + t.slot;
        kind = st != null ? st.kind ?? "" : "";
        live = MuseumLive(t.room, t.slot);
        line = MuseumRowTag(st);
    }

    // A framed code: the code in the HUD's face, its kind and its state under it, in a ruled frame (accent for a live station).
    void DrawMuseumFrame(float cx, float cy, string code, string kind, string line, bool live, float alpha, float s)
    {
        var pad = 7f * s;
        var codeSize = nameStyle.CalcSize(new GUIContent(code));
        var kindSize = smallStyle.CalcSize(new GUIContent(kind));
        var lineSize = smallStyle.CalcSize(new GUIContent(line));
        var w = Mathf.Max(codeSize.x, Mathf.Max(kindSize.x, lineSize.x)) + pad * 2f;
        var h = codeSize.y + kindSize.y + lineSize.y + pad * 2f + 2f * s;
        var r = new Rect(cx - w * .5f, cy - h * .5f, w, h);
        if (r.xMax < 0f || r.x > Screen.width || r.yMax < 0f || r.y > Screen.height) return;
        var th = Mathf.Max(1f, 2f * s);
        var rule = live ? Accent : Paper;
        Outline(new Rect(r.x - 1f, r.y - 1f, r.width + 2f, r.height + 2f), new Color(Ink.r, Ink.g, Ink.b, .55f * alpha), th + 2f);
        Outline(r, new Color(rule.r, rule.g, rule.b, (live ? 1f : .7f) * alpha), th);
        var ty = r.y + pad;
        Text(new Rect(r.x + pad, ty, w - pad * 2f, codeSize.y), code, nameStyle, live ? Accent : Paper, alpha);
        ty += codeSize.y;
        Text(new Rect(r.x + pad, ty, w - pad * 2f, kindSize.y), kind, smallStyle, Paper, .95f * alpha);
        ty += kindSize.y + 2f * s;
        Text(new Rect(r.x + pad, ty, w - pad * 2f, lineSize.y), line, smallStyle, live ? Accent : Paper, (live ? .95f : .6f) * alpha);
    }

    static void Outline(Rect r, Color c, float th)
    {
        Fill(new Rect(r.x, r.y, r.width, th), c);
        Fill(new Rect(r.x, r.yMax - th, r.width, th), c);
        Fill(new Rect(r.x, r.y + th, th, r.height - 2f * th), c);
        Fill(new Rect(r.xMax - th, r.y + th, th, r.height - 2f * th), c);
    }

    /// <summary>The right column: the room (sign B, its holds and what Red decides), its stations with their state, the keys.</summary>
    void DrawMuseumColumn(float x, float y, float w, float s)
    {
        var room = MuseumRoomHere();
        if (room < 0) room = 0;
        Text(new Rect(x, y, w, 40f * s), "MUSEUM  ·  " + MuseumRoomTitle(room), titleStyle, Paper);
        y += 38f * s;
        y = Para(x, y, w, MuseumKeysLine, smallStyle, Paper, .8f, 6f * s);
        y = Para(x, y, w, problem, smallStyle, Accent, 1f, 8f * s);
        var sign = RoomSign(room);
        if (sign != null)
        {
            y = Para(x, y, w, sign.holds, smallStyle, Paper, .85f, 2f * s);
            y = Para(x, y, w, Decide(sign.decide), zhSmallStyle, Accent, .9f, 8f * s);
        }
        int markRoom, markSlot;
        var marked = MuseumMarked(out markRoom, out markSlot);
        var all = FrontRoomsShowcaseTable.Stations;
        for (var i = 0; all != null && i < all.Length; i++)
        {
            var st = all[i];
            if (st == null || FrontRoomsShowcaseTable.RoomOf(st) != room) continue;
            var slot = FrontRoomsShowcaseTable.SlotOf(st);
            var live = MuseumLive(room, slot);
            var isMarked = marked && markRoom == room && markSlot == slot && MuseumRow(room, slot) == st;
            if (isMarked) Fill(new Rect(x - 14f * s, y + 6f * s, 4f * s, 14f * s), Accent);
            Text(new Rect(x, y, 64f * s, 26f * s), st.code ?? "", nameStyle, isMarked || live ? Accent : Paper, live || isMarked ? 1f : .75f);
            Text(new Rect(x + 64f * s, y, Mathf.Max(40f * s, w - 304f * s), 26f * s), st.kind ?? "", nameStyle, Paper, live ? 1f : .7f);
            Text(new Rect(x + w - 240f * s, y + 5f * s, 240f * s, 22f * s), MuseumRowTag(st), statusStyle, Paper, live ? .95f : .55f);
            y += 28f * s;
        }
        if (room != 0) return;
        var rooms = "";
        for (var r = 1; r <= 4; r++) rooms += (r > 1 ? "  ·  " : "") + MuseumRoomTitle(r) + (r == 1 ? " (LIVE)" : "");
        Para(x, y + 6f * s, w, "ROOMS  " + rooms + "  ·  [ ] GOES ROUND THEM", smallStyle, Paper, .75f, 0f);
    }

    /// <summary>The left card: the placard of the station the player stands at (or last walked to); in the hub, the hub board.</summary>
    void DrawMuseumCard(float margin, float s)
    {
        var x = margin;
        var y = Mathf.Max(240f * s, Screen.height * .36f);
        var w = Mathf.Min(600f * s, Screen.width * .42f);
        int room, slot;
        if (MuseumMarked(out room, out slot))
        {
            var st = MuseumRow(room, slot);
            if (st != null)
            {
                DrawMuseumPlacard(st, room, slot, x, y, w, s);
                return;
            }
        }
        if (MuseumRoomHere() == 0) DrawHubBoard(x, y, w, s);
    }

    /// <summary>Placard A, the hub board (10_research §3.4 A): the table's board lines, and P for the plan.</summary>
    void DrawHubBoard(float x, float y, float w, float s)
    {
        y = Para(x, y, w, BoardLine(0), titleStyle, Paper, 1f, 2f * s);
        y = Para(x, y, w, BoardLine(1), lineStyle, Paper, .9f, 8f * s);
        y = Para(x, y, w, "P  THE PLAN: every station, its code and its placard; ENTER tries one.", lineStyle, Accent, 1f, 4f * s);
        y = Para(x, y, w, "[ ] ROOM TO ROOM  ·  1-7 A STATION, AGAIN FOR ITS NEXT VARIANT  ·  0 THE HUB  ·  R ROOM 1'S DOORS AS BUILT  ·  B BREAK THE DOOR IN VIEW  ·  G HIDE  ·  F2 TOURS", smallStyle, Paper, .85f, 6f * s);
        y = Para(x, y, w, "Room 1 is live: six doors of the game's own and an alcove. Rooms 2-4 stand as framed codes; each placard says where to try that station today.", smallStyle, Paper, .8f, 6f * s);
        y = Para(x, y, w, BoardLine(3), smallStyle, Paper, .7f, 6f * s);
        var sign = RoomSign(0);
        if (sign != null) Para(x, y, w, Decide(sign.decide), zhLineStyle, Accent, 1f, 0f);
    }

    /// <summary>Placard C in the museum: code and kind, what the museum shows here now, Try, touch, what it is, what Red decides, the small line.</summary>
    void DrawMuseumPlacard(FrontRoomsShowcaseTable.Station st, int room, int slot, float x, float y, float w, float s)
    {
        var live = MuseumLive(room, slot);
        Text(new Rect(x, y, w, 40f * s), LabelOf(st), titleStyle, live ? Accent : Paper);
        y += 40f * s;
        y = Para(x, y, w, MuseumHere(room, slot, st), lineStyle, live ? Accent : Paper, 1f, 6f * s);
        var tryLine = MuseumTryLine(room, slot) ?? st.tryKeys ?? (st.guideKind >= 0 && st.guideKind < KindCount ? HowTo[st.guideKind] : "");
        if (!string.IsNullOrEmpty(tryLine)) y = Para(x, y, w, "TRY  " + tryLine, lineStyle, Paper, .95f, 4f * s);
        if (!string.IsNullOrEmpty(st.tryTouch)) y = Para(x, y, w, "TOUCH  " + st.tryTouch, smallStyle, Paper, .8f, 8f * s);
        y = Para(x, y, w, st.what ?? "", zhLineStyle, Paper, 1f, 4f * s);
        y = Para(x, y, w, Decide(st.decide), zhLineStyle, Accent, 1f, 8f * s);
        var rule = st.rule == FrontRoomsShowcaseTable.Rule.Media ? "MEDIA BAY"
            : st.rule == FrontRoomsShowcaseTable.Rule.None ? "" : "RULE " + FrontRoomsShowcaseTable.RuleName(st.rule);
        var small = st.staging ?? "";
        if (!string.IsNullOrEmpty(st.status)) small += (small.Length > 0 ? "  ·  " : "") + st.status;
        if (rule.Length > 0) small += (small.Length > 0 ? "  ·  " : "") + rule;
        y = Para(x, y, w, small, smallStyle, Paper, .7f, 6f * s);
        if (!string.IsNullOrEmpty(st.variants)) Para(x, y, w, "VARIANTS  " + st.variants, smallStyle, Paper, .9f, 0f);
    }
}
