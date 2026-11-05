using System;
using System.Collections.Generic;
using System.Reflection;
using FrontRooms.Map;
using UnityEngine;
using UnityEngine.InputSystem;

// Scene 3, menu 5 MUSEUM (交互设计, 2026-11-03; scene3/rooms/90_plan.md §1, §3 rows 3, 5 and 6, 80_museum.md I1-I3, 82_integration.md):
// the museum of 10_research §3 built in the GAME scene, from the game's own cells and doors, walked by the game's own player.
// - The tour's copy of the level profile (DontSave, no station): every zone Standard 2.9 Level 0, no office, module, column
//   or pile, no devices, no revisit shift, one fixed seed. The run starts without the title (the private RequestTitleStart,
//   by reflection, as the game's own editor harnesses start runs) and the Relay stays dormant (its tools shim,
//   hunterTuning.releaseDelaySeconds 1e6, plus the tours' HeldForTools).
// - At MapRunStarted the museum becomes ONE carve on the map's cache (FrontRoomsMapCarve.From with a stand-in layout, the
//   station's own path; Message A of 90 §4): hub 0 and rooms 1-4 on the two chunks 40 north of the player's (a few chunks
//   east or west where that puts room 2 in one zone), every edge with a museum cell on either side forced, room 2's zone key
//   moved onto bay 2.2 and the museum chunks' other keys into sealed corners, the lamps Steady inside and Off on a two-cell
//   ring (an unshadowed lamp lights through walls). On the next frame its chunks and their west and south neighbours are
//   built at once, the stream is let go and the player walks in at the hub, facing rooms 2 and 3.
// - Room 1 is live: six real Door records on its bay fronts. 1.1 one door, A body / B E (the map-wide switch, so every
//   museum door switches with it: Q-3 is not answered, one switched door is the plan's recommendation); 1.2 its lite; 1.3 the
//   LOCKED model for show (the kit's DoorLockedOverride); 1.5 broken with the map's BreakDoor (B); 1.6 the Run! goal door's
//   look (DoorMemberOverride); 1.7 the member stand A-D.
// - Room 3 is live (90 §3 row 5, 80 I2): three carved windows on its right wall (the kit faces a same-height window to its
//   lower cell, the lane), every one with its sash latch (the copy's sashPry); 3.3's record foiled from WindowBuilt with the
//   kit's own FrontRoomsWindowFoil; 3.4 an office window from the alcove into a lit corner; 3.5 a door the map marks as an
//   alarm door (MarkAlarmDoorForTools, editor only); 3.6 the ALARM ZONE tell on a Run! room's 1.4 m arch (the carve's E2 span).
// - Room 4 is live: kit objects of the game's own on its fronts (4.1 a desk CRT made a terminal, 4.3 three CRTs with the
//   media bay's ad, 4.5 the evacuation placard, 4.6 the station's dated papers, 4.7 the four test-bed close-ups); 4.2 the
//   station TV stays a frame (its close-up belongs to a station: tour 2).
// - Room 2 is live (90 §3 row 6, 80 I3), in the game's order: 2.1 and 2.3 real doors locked by their own rule (the map's
//   tools rule LockRuleForTools: true for those two, false for every other museum door, so the rest stay free), 2.2 their
//   zone's key on the floor of an open bay (the carve's key move: the game opens a locked door with the key of the zone the
//   player stands in, so every cell of room 2 a visitor walks is one zone), taken with look + E into the key HUD; 2.5 that
//   key's tag kit on a plinth (E · EXAMINE KEY). 2.6 STAIR KEY and 2.7 EXIT stand as placards (tour 2; Q-1).
//   The hub's two stations are framed codes.
// - Keys: [ ] room to room, 1-7 a station (again: its next variant), 0 the hub, R the museum as built (doors shut and whole,
//   2.1 and 2.3 locked again, the key back on 2.2, windows whole; only while the game's phase is Playing: outside it R is
//   the game's END RUN), B a museum door broken, P the plan, G the list.
// Everything private it reads is found by name and type; a missing one turns its part off with one message, never throws.
// A failure anywhere leaves the run as the game's own floor, with one message; the plan (P) keeps working.
public sealed partial class FrontRoomsInteractablesShowcase
{
    // The layout (80 §4): museum cells (u 0-15 west to east, v 0-7 south to north) fill the chunks O and O + (1, 0) exactly.
    const int MuseumCols = 16, MuseumRows = 8;
    /// <summary>O = the player's chunk at MapRunStarted + (0, 40): 960 m north of the start rooms (no public start-door getter);
    /// a row further north per try, and on each row up to MuseumSideTries chunks east or west (room 2 in one zone).</summary>
    const int MuseumChunksNorth = 40, MuseumShiftTries = 12, MuseumSideTries = 4;
    /// <summary>Lamps Off this many cells round the museum: past the shadow radius a lamp lights through walls.</summary>
    const int MuseumRing = 2;
    /// <summary>The copy's map seed: the same museum surroundings, key sites and origin on every Play.</summary>
    const int MuseumSeed = 20261103;
    const float MuseumStartDelay = .5f, MuseumStartEvery = .5f, MuseumStartGiveUp = 30f;
    const float DoorTagHeight = 2.5f, WallTagHeight = 1.6f, SignTagHeight = 2.55f, MuseumTagFar = 22f, MuseumTagSeconds = .1f;
    const float StopNear = 1.6f, PlacardNear = 2.2f, MuseumCardSeconds = 8f;
    /// <summary>A window's stop: this far from its glass (the glass breaks from 1.2 m; the guide's own jumps stand at 1.0 m).</summary>
    const float WindowStand = 1f;
    /// <summary>Room 4's kits on its fronts: a desk's back this far off the wall (the office kit's), a wall unit's, a flush kit's.</summary>
    const float DeskGap = .045f, UnitGap = .02f, FlushProud = .0005f;
    /// <summary>Where room 4's longer kits stand along their wall (room cells from the hub wall): clear of the arches beside
    /// the hub wall (4.1 next to E, 4.7 next to X), the stops and framed codes of 4.1 and 4.7 on them.</summary>
    const float TerminalAlong = .75f, PapersAlong = 1.55f, DevRowAlong = .7f;
    /// <summary>2.2's stop: this far into its open bay, so the key on the bay's floor is within the game's reach (2.4 m from
    /// the eye; from here 1.96 m) and the look down at it is the pickup's.</summary>
    const float KeyStand = .3f;
    /// <summary>2.5's plinth: the plywood case good of the game's piles, its top at 1.2 m (10_research §3.3: about 1.15 m).</summary>
    const string KeyTagPlinth = "Kit_PlyCabinet";
    /// <summary>A key tag lying flat: its origin (the hole) this far over the surface (the tag kits are 4.2 mm thick).</summary>
    const float TagHalfThick = .0021f;
    /// <summary>4.7's test-bed close-ups, A-D as the table names them.</summary>
    const int DevCount = 4;
    static readonly string[] DevNames = { "DESK PHONE", "WALL CLOCK", "VENDING MACHINE", "COPIER" };
    const string MuseumKeysLine = "[ ] ROOM  ·  1-7 STATION (AGAIN: VARIANT)  ·  0 HUB  ·  R RESET  ·  B BREAK  ·  P PLAN  ·  G HIDE  ·  F2 TOURS";

    // The world placards (inc 8b; FrontRoomsShowcasePlacards to 20_placard_type.md v2, placed by 10_research §3.2-3.7). Every
    // length in metres; ~/FrontRoomsInteractionWork/scene3/land_inc8c/placement/model.py reads these and mirrors the placing.
    /// <summary>C's clearance from walls, kits, every leaf's sweep and every arch's opening.</summary>
    const float PlacardClear = .05f;
    /// <summary>C in plan: half its width plus PlacardSlack; behind its face the slab and bracket reach PlacardBack more.</summary>
    const float PlacardSlack = .01f, PlacardBack = .03f;
    /// <summary>Half the side of C's floor plate (builder A's 360 mm, the post on its middle), centred under the bracket
    /// (FrontRoomsShowcasePlacards.StationBracketBack x cos(tilt) behind the face's centre): kept out of every stop area.</summary>
    const float PlacardStand = .18f;
    /// <summary>C's face grown by this in the test that it hides nothing of its exhibit from the stop's eye.</summary>
    const float PlacardViewPad = .02f;
    /// <summary>C's bearing, degrees right of the stop's facing: tried from 0 in 1-degree steps up to this.</summary>
    const int PlacardBearingMax = 120;
    /// <summary>A leaf's sweep into the lane: the leaf (0.98 m) with half its thickness and the kit's pivot, to a dressed
    /// double-acting leaf's stop at 95.4 degrees (FrontRoomsMapWorld.DoorBreakStopDegreesDA).</summary>
    const float SwingReach = 1.03f, SwingDegrees = 96f;
    /// <summary>W1's stop area on every stop (10_research §3.2), centred on it: across the front, and toward it.</summary>
    const float StopAlong = .76f, StopDeep = 1.22f;
    /// <summary>An arch: its opening and this far into the room stay clear (a body standing in the opening: PlayerRadius).</summary>
    const float ArchPassage = .3f;
    /// <summary>The exhibit as the view test samples it: a door with its frame kit (75 mm past the opening, 2.175 m), a window
    /// with its casing and latch, an arch, a framed code (FrameExhibitRise about its tag's height); grid steps.</summary>
    const float DoorExhibitHalf = .58f, DoorExhibitTop = 2.18f, DoorExhibitProud = .025f;
    const float WindowExhibitHalf = .78f, WindowExhibitSill = .28f, WindowExhibitHead = 2.08f, WindowExhibitProud = .05f;
    const float ArchExhibitHalf = .75f, ArchExhibitTop = 2.25f, FrameExhibitHalf = .6f, FrameExhibitRise = .3f;
    const float ExhibitStep = .1f, AlcoveStep = .25f;
    /// <summary>B and D stay this far from a corner, a casing or a kit; a floor tag's far edge stands TagGap in front of its kit,
    /// two tags side by side TagSideGap apart.</summary>
    const float SignGap = .05f, TagGap = .1f, TagSideGap = .04f;
    /// <summary>C's live state (its foot): refreshed this often, only in the room the player stands in (s).</summary>
    const float MuseumLiveSeconds = .25f;
    /// <summary>The kits of the exhibits that only carry one (desks, the credenza, the plinth, the cabinet): kept clear, but
    /// not the exhibit a placard must leave in view. Their labels are BuildMuseumExhibits'.</summary>
    static readonly string[] PlacardSupports = { "2.5 plinth", "4.1 terminal desk", "4.3 credenza", "4.6 station desk", "4.7 filing cabinet" };
    static readonly string[] DevLetters = { "A", "B", "C", "D" };

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
    // The museum's door fronts by edge id (room 1's six and 3.5's), and what the kit is told about room 1's (render only).
    readonly HashSet<long> museumLiveEdges = new HashSet<long>();
    readonly HashSet<long> museumLockedModel = new HashSet<long>();
    readonly Dictionary<long, FrontRoomsDoorMember> museumMembers = new Dictionary<long, FrontRoomsDoorMember>();
    readonly List<MuseumTag> museumTags = new List<MuseumTag>();
    readonly List<MuseumTag> museumTagPool = new List<MuseumTag>();
    // The kit's two tools statics: ours while the museum stands, the values found before put back when it goes.
    Func<FrontRoomsMapWorld.Door, FrontRoomsDoorMember?> museumMemberHook, museumPrevMember;
    Func<FrontRoomsMapWorld.Door, bool?> museumLockedHook, museumPrevLocked;
    bool museumHooksSet;
    // Room 3's four windows (3.1-3.3 and the alcove's 3.4) by edge, as the map last built them (WindowBuilt; WindowReleased drops
    // a record), on the map our two handlers are on; 3.3's edge is the one foiled.
    readonly HashSet<long> museumWindowEdges = new HashSet<long>();
    readonly Dictionary<long, FrontRoomsMapWorld.Window> museumWindows = new Dictionary<long, FrontRoomsMapWorld.Window>();
    long museumFoilEdge = long.MinValue;
    FrontRoomsMapWorld museumHookedMap;
    // The kit objects of rooms 2-4 under one root of ours in the game scene (never a chunk's: rebuilds leave them be),
    // which station set up (room * 8 + slot), 4.1's terminal, 4.3's screens and 4.7's four stops (museum cells) and faces.
    GameObject museumExhibits;
    readonly bool[] museumExhibitOk = new bool[5 * 8];
    FrontRoomsTerminalScreen museumTerminal;
    int museumAdScreens;
    readonly bool[] museumDevOk = new bool[DevCount];
    readonly Vector2[] museumDevStand = new Vector2[DevCount];
    readonly Vector3[] museumDevLook = new Vector3[DevCount];
    // 4.7's close-up the player was last stood at (-1: none yet; 7 again steps A-D).
    int museumDev = -1;
    // Room 2: the zone every cell of it a visitor walks lies in (its key goes onto 2.2), whether room 2 runs (the origin put
    // it in one zone and its key moved), its two locked doors by edge, the map's lock rule we set (ours, on that map), and
    // where a walk to 2.5 looks (the tag's face).
    GridCoord museumKeyZone;
    bool museumKeysLive, museumKeyMoved;
    readonly HashSet<long> museumLockedEdges = new HashSet<long>();
    Func<FrontRoomsMapWorld.Door, bool?> museumLockRule;
    FrontRoomsMapWorld museumRuleMap;
    Vector3 museumTagLook;
    // The world placards: ONE root of ours in the game scene (unit scale at the origin, never a chunk's, so rebuilds leave it
    // be), its parts by room (index 1-4: a room's inside, on only while the player is in it or at its entry; 0 the hub's, always
    // on), C by station (room * 8 + slot; 1.1's B apart: both stand, the door's variant shows) with the live word its foot
    // shows while the station stands as a frame (where it is tried today, from its row at the build), 1.7's four floor tags
    // (the member on the door shows), the rooms whose sign stands on the hub side (B or the number over E), the arrow each
    // room's inside B was built with (the plan page's B card shows it), the hub cells at each room's E and X (2 per room,
    // museum cells).
    GameObject museumPlacards;
    readonly GameObject[] museumPlacardC = new GameObject[5 * 8];
    GameObject museumPlacardC11B;
    readonly string[] museumPlacardFrame = new string[5 * 8];
    string museumPlacardFrame11B;
    readonly bool[] museumHubSigns = new bool[5];
    readonly string[] museumSignArrows = new string[5];
    readonly GameObject[] museumMemberTags = new GameObject[4];
    readonly List<Canvas>[] museumPlacardCanvases = { new List<Canvas>(), new List<Canvas>(), new List<Canvas>(), new List<Canvas>(), new List<Canvas>() };
    readonly List<Renderer>[] museumPlacardRenderers = { new List<Renderer>(), new List<Renderer>(), new List<Renderer>(), new List<Renderer>(), new List<Renderer>() };
    readonly Vector2Int[] museumEntryCells = new Vector2Int[8];
    int museumPlacardMask = -1, museumPlacardMember = -1;
    bool museumPlacardBody;
    float museumLiveClock;

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
    /// column or pile, no station (no floor key, no station carve for the cache's one slot), doors free (room 2's two locked
    /// doors by their own rule, the map's LockRuleForTools, not by the floor's), body doors (1.1 A),
    /// sash latches on (3.2; the copy makes no Tall zone, so the museum's carved windows are the floor's only ones), no
    /// planned device (3.3 and 3.5 are marked on their own edges), no revisit shift, no renovated crossing, one seed.
    /// Only this DontSave copy changes.
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
        tourProfile.foil = tourProfile.alarmDoor = tourProfile.runRooms = false;
        // C12: every map window gets its latch (AddSashLatch in BuildEdge) and the kit its double-hung sashes (DressWindow).
        tourProfile.sashPry = true;
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
            if (PlanMuseum(out why))
            {
                // F5 (95 review): the editor's FrontRooms/Map/Play with keys on sets the map's doorsNeedKeys in StartRunInPlace
                // (FrontRooms3DGame.cs:1004-1010), after the map took the copy's false. The museum's own doors follow its lock
                // rule whatever the floor's (PlanMuseum set it), but the floor around it would lock: back to the copy's own rule
                // before any chunk is built (BuildMuseum is next frame; nothing in the plan reads the rule). Only once the museum
                // is carved: a run that stays the game's own floor keeps the switch, on this floor and the next (NextFloor keeps it).
                if (map.DoorsNeedKeysForTools)
                {
                    map.DoorsNeedKeysForTools = false;
                    MuseumNote("FrontRooms/Map/Play with keys on is ticked: the museum keeps its own rule all the same (room 2's two doors locked, every other door free).");
                }
                museumState = MuseumState.Carved;
            }
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

    /// <summary>
    /// Put the kit's two tools statics back (as they were before the museum set them), take the map's lock rule off (room 2),
    /// leave the map's window events, take the exhibits and the placards down and forget the museum's doors, windows and key.
    /// OnDestroy, BackToMenu (F2), Choose, the run's end (TRY AGAIN and R restart too), the floor change, a failure, a new scene.
    /// </summary>
    void ClearMuseumTools()
    {
        DestroyMuseumPlacards();
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
        ClearMuseumLockRule();
        museumLockedEdges.Clear();
        museumKeysLive = museumKeyMoved = false;
        UnhookMuseumWindows();
        museumWindowEdges.Clear();
        museumFoilEdge = long.MinValue;
        DestroyMuseumExhibits();
    }

    // ---------- room 2's locks: the map's tools rule (H2, FrontRoomsMapWorld.LockRuleForTools, read in LockedByRule)

    /// <summary>Our rule on the map (one map at a time): every lock reader and the kit's dress follow it from now on.</summary>
    void SetMuseumLockRule(FrontRoomsMapWorld m)
    {
        ClearMuseumLockRule();
        if (m == null) return;
        if (museumLockRule == null) museumLockRule = MuseumLockOf;
        m.LockRuleForTools = museumLockRule;
        museumRuleMap = m;
    }

    /// <summary>Our rule off the map we set it on (by our own reference: at the run's end the guide has let go of `map`, and the
    /// map may be destroyed: the property is plain C#). A rule someone else set since stays.</summary>
    void ClearMuseumLockRule()
    {
        if ((object)museumRuleMap != null && museumRuleMap.LockRuleForTools == museumLockRule) museumRuleMap.LockRuleForTools = null;
        museumRuleMap = null;
    }

    /// <summary>
    /// The lock rule (asked for every door, often: no allocation): 2.1 and 2.3 locked; every other door with a museum cell on
    /// either side free, whatever the floor's rule; null (the floor's own rule) for every door outside the museum.
    /// </summary>
    bool? MuseumLockOf(FrontRoomsMapWorld.Door door)
    {
        if (door == null) return null;
        if (museumLockedEdges.Contains(door.edge)) return true;
        return InMuseum(door.a) || InMuseum(door.b) ? false : (bool?)null;
    }

    bool InMuseum(GridCoord cell)
    {
        var u = cell.x - museumCell0.x;
        var v = cell.y - museumCell0.y;
        return u >= 0 && u < MuseumCols && v >= 0 && v < MuseumRows;
    }

    // ---------- room 3's windows: the map's own records, as it builds them (3.3's foil from here)

    void HookMuseumWindows(FrontRoomsMapWorld m)
    {
        UnhookMuseumWindows();
        if (m == null) return;
        m.WindowBuilt += OnMuseumWindowBuilt;
        m.WindowReleased += OnMuseumWindowReleased;
        museumHookedMap = m;
    }

    void UnhookMuseumWindows()
    {
        // By reference: the map may be destroyed already (the run's end); its events are plain C# and leave cleanly.
        if ((object)museumHookedMap != null)
        {
            museumHookedMap.WindowBuilt -= OnMuseumWindowBuilt;
            museumHookedMap.WindowReleased -= OnMuseumWindowReleased;
        }
        museumHookedMap = null;
        museumWindows.Clear();
    }

    /// <summary>
    /// The map built one of its windows (after the kit dressed it: frame, glass, the sashes with sashPry on). Room 3's are kept;
    /// 3.3's record is foiled here, as the planner foils one at build (MapWorld BuildEdge reads Cache.Foil, off in this copy),
    /// and the kit's foil is hung the way DressWindow hangs it for a foiled window (FrontRoomsInteractableKit.Window.cs:99-102).
    /// Every reader takes the record's flag: the ALARMED pry prompt, PryAlarmed, TripFoil on a break or a pry.
    /// </summary>
    void OnMuseumWindowBuilt(FrontRoomsMapWorld.Window window, FrontRoomsMapWorld.GlassBreakRecord record)
    {
        if (window == null || !museumWindowEdges.Contains(window.edge)) return;
        museumWindows[window.edge] = window;
        var m = museumHookedMap;
        if (window.edge != museumFoilEdge || m == null || m.Cache == null) return;
        try
        {
            window.foiled = true;
            // The kit's frame turn: face A to the room, the non-tall side (here both are Standard, so cell a, the lane).
            var roomIsB = m.Cache.ZoneOf(window.a).height == ZoneHeight.Tall;
            var member = FrontRoomsInteractableKit.WindowMemberFor(m.Cache.ZoneOf(roomIsB ? window.b : window.a).theme);
            var turn = roomIsB ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
            if (FrontRoomsWindowFoil.Attach(m, window, record, member == FrontRoomsInteractableKit.WindowMember.Lobby, turn) == null)
                Debug.LogWarning("Interactables showcase (museum): 3.3's foil was not hung (FrontRoomsWindowFoil.Attach); the window is foiled all the same.");
        }
        catch (Exception e)
        {
            Debug.LogWarning("Interactables showcase (museum): 3.3's foil threw (" + e.GetType().Name + ": " + e.Message + "); the window is foiled all the same.");
        }
    }

    void OnMuseumWindowReleased(FrontRoomsMapWorld.Window window)
    {
        FrontRoomsMapWorld.Window kept;
        if (window != null && museumWindows.TryGetValue(window.edge, out kept) && kept == window) museumWindows.Remove(window.edge);
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
    /// room 2's sealed corner, E1 on room 1's E arch, E2 on 3.6's arch), then the cache's one carve slot. False with the reason.
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
        // F20: clear of the start rooms, X and N, and of anything built; room 2 in one zone (its locks and key need it).
        GridCoord o, keyZone;
        bool oneZone;
        if (!FindMuseumOrigin(start, out o, out oneZone, out keyZone))
        {
            why = "no clear place " + MuseumChunksNorth + "-" + (MuseumChunksNorth + MuseumShiftTries) + " chunks north of the start (up to "
                + MuseumSideTries + " east or west)";
            return false;
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
        // A zone's key lies at its site, in its home chunk: room 2's zone key goes onto the floor of bay 2.2 (the key of the zone
        // every cell of room 2 lies in: its locked doors take it), the two museum chunks' other keys to sealed corners.
        GridCoord keyBay, keyLane;
        var keyHere = MuseumFront(2, 2, out keyBay, out keyLane) && oneZone;
        var east = new GridCoord(o.x + 1, o.y);
        if (keyHere) AddMuseumKeyMove(overlay, keyZone, keyBay);
        if (!keyHere || keyZone != o) AddMuseumKeyMove(overlay, o, MuseumCell(0, 0));
        if (!keyHere || keyZone != east) AddMuseumKeyMove(overlay, east, MuseumCell(15, 0));
        if (!oneZone)
            MuseumNote("Room 2 stands as framed codes: no clear place in the search puts all of it in one zone, so its doors stay free and its key sealed (tour 4 has them).");
        // The carve's two authored arch spans (ArchOpening reads them before its hash): E1 on room 1's E arch as before, E2 on
        // 3.6's front, the 1.4 m centred arch of a Run! room's entrance (FrontRoomsRunRooms.ArchWidth), so its ALARM ZONE card
        // hangs where the Run! rig hangs it. Room 1's X arch is now the map's own, as rooms 2-4's arches are.
        GridCoord tellBay, tellLane;
        MuseumFront(3, 6, out tellBay, out tellLane);
        var layout = new StartGroupLayout
        {
            door = MuseumCell(8, 0),
            station = new CellRect(0, 0, 0, 0),
            e1In = MuseumCell(3, 2),
            e1Out = MuseumCell(4, 2),
            e2In = tellLane,
            e2Out = tellBay,
            corridor = 0,
            front = MuseumCell(4, 6),
            exit = MuseumCell(4, 7),
        };
        var template = StartGroupTemplate.StandIn(4, 1, 4);
        template.e2Width = FrontRoomsRunRooms.ArchWidth;
        var carve = FrontRoomsMapCarve.From(overlay, template, layout, museumCell0);
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
        PlanMuseumRoomThree();
        PlanMuseumRoomTwo(keyHere, keyZone);
        return true;
    }

    /// <summary>
    /// Room 2 before its chunk is built: its two door fronts (2.1, 2.3: B breaks them, R mends them), locked by the map's tools
    /// rule when room 2 runs, and the rule on the map now, so the build dresses them LOCKED (the kit reads the rule at dress time,
    /// FrontRoomsInteractableKit.DressDoor) and every other museum door free.
    /// </summary>
    void PlanMuseumRoomTwo(bool live, GridCoord zone)
    {
        museumLockedEdges.Clear();
        // With room 2 off, 2.5's tag still shows a key's tag: the west museum chunk's zone.
        museumKeyZone = live ? zone : museumChunk;
        museumKeysLive = live;
        museumKeyMoved = false;
        for (var slot = 1; slot <= 3; slot += 2)
        {
            GridCoord bay, lane;
            if (!MuseumFront(2, slot, out bay, out lane)) continue;
            var edge = MapGrid.EdgeId(bay, lane);
            museumLiveEdges.Add(edge);
            if (live) museumLockedEdges.Add(edge);
        }
        SetMuseumLockRule(map);
    }

    /// <summary>
    /// The museum's chunk O (80 §8.1's pure search): 40 chunks north of the start, a row further north per try (as before), and on
    /// each row also up to MuseumSideTries chunks east and west: the first clear place where every cell of room 2 a visitor can
    /// stand in lies in one zone (RoomTwoOneZone). The game opens a locked door with the key of the zone the PLAYER stands in
    /// (MapWorld KeyFor) and its key HUD shows a key only inside its zone, so room 2's lane, alcove and bays must be one zone.
    /// No such place (about 1 in 2.5 places has it): the first clear one, with room 2 off (oneZone false). False: none is clear.
    /// </summary>
    bool FindMuseumOrigin(GridCoord start, out GridCoord o, out bool oneZone, out GridCoord zone)
    {
        o = zone = default(GridCoord);
        oneZone = false;
        var clear = false;
        for (var dy = 0; dy <= MuseumShiftTries; dy++)
        for (var k = 0; k <= 2 * MuseumSideTries; k++)
        {
            // 0, +1, -1, +2, -2, ... chunks east.
            var dx = (k + 1) / 2 * (k % 2 == 1 ? 1 : -1);
            var c = new GridCoord(start.x + dx, start.y + MuseumChunksNorth + dy);
            if (!MuseumClear(c, start)) continue;
            GridCoord z;
            if (RoomTwoOneZone(c, out z))
            {
                o = c;
                zone = z;
                oneZone = true;
                return true;
            }
            if (clear) continue;
            o = c;
            clear = true;
        }
        return clear;
    }

    /// <summary>
    /// Pure (the generator's nearest site, memoised; no tier recorded): with the museum at chunk o, do room 2's lane, alcove and
    /// three right bays (2.1's and 2.3's closets, 2.2's open bay) all lie in one zone, and is it one with a key (not Tall)?
    /// </summary>
    bool RoomTwoOneZone(GridCoord o, out GridCoord zone)
    {
        zone = default(GridCoord);
        var generator = map.Cache.Generator;
        var c0 = MapGrid.ChunkOrigin(o);
        var first = true;
        for (var j = 0; j <= 3; j++)
        for (var i = 1; i <= 3; i++)
        {
            // The closed corner behind the alcove's right end.
            if (i == 3 && j == 3) continue;
            int u, v;
            LocalToMuseum(2, i, j, out u, out v);
            var id = generator.ZoneOf(new GridCoord(c0.x + u, c0.y + v)).id;
            if (first)
            {
                zone = id;
                first = false;
            }
            else if (id != zone) return false;
        }
        return generator.Zone(zone).height != ZoneHeight.Tall;
    }

    /// <summary>
    /// Room 3 before its chunk is built: its four windows' edges (3.3's foiled from WindowBuilt) and the map's window events,
    /// and 3.5's edge marked as an alarm door, so the build makes it one and the kit dresses its push bars and horn
    /// (MarkAlarmDoorForTools reads only the edge and the cells; a stand-in record carries them; kept by edge until NextFloor).
    /// </summary>
    void PlanMuseumRoomThree()
    {
        museumWindowEdges.Clear();
        museumFoilEdge = long.MinValue;
        for (var slot = 1; slot <= 4; slot++)
        {
            GridCoord a, b;
            if (!MuseumWindowCells(slot, out a, out b)) continue;
            var edge = MapGrid.EdgeId(a, b);
            museumWindowEdges.Add(edge);
            if (slot == 3) museumFoilEdge = edge;
        }
        HookMuseumWindows(map);
        GridCoord bay, lane;
        if (!MuseumFront(3, 5, out bay, out lane)) return;
        museumLiveEdges.Add(MapGrid.EdgeId(bay, lane));
#if UNITY_EDITOR
        // Editor only on the map's side (C16's tools mark): a player build keeps 3.5 a plain door, and its placard says so.
        map.MarkAlarmDoorForTools(new FrontRoomsMapWorld.Door { a = bay, b = lane, edge = MapGrid.EdgeId(bay, lane) }, true);
#endif
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

    /// <summary>Steady in the hub, lanes, alcoves and the lit bays of rooms 1-3; Off in the sealed cells, the two maze blocks and the ring.</summary>
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
        // Room 3's right corner is the closet its alcove's window (3.4) looks into.
        if (part == MuseumPart.Corner) return room == 3 && i == 3 ? ModuleLamp.Steady : ModuleLamp.Off;
        // The closets behind room 1's doors, room 2's locked doors and room 3's windows, alarm door and arch: lit, so a lite, a
        // pane or the detector reads live; room 2's open bay 2.2 lit for its key on the floor. Room 3's free bay, room 2's left
        // wall and room 4 (kits stand on their walls) stay sealed and dark.
        if (part == MuseumPart.Bay)
            return room == 1 || (room == 2 && i == 3) || (room == 3 && SlotOfBay(i, j) != 7) ? ModuleLamp.Steady : ModuleLamp.Off;
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

    /// <summary>The station of a bay cell (the inverse of BayOfSlot): .1-.3 up the right wall, .5-.7 down the left; 0 for any other cell.</summary>
    static int SlotOfBay(int i, int j)
    {
        if (j < 0 || j > 2) return 0;
        if (i == 3) return j + 1;
        if (i == 0) return 7 - j;
        return 0;
    }

    /// <summary>
    /// What stands on a bay's front: room 1 a real door at each; room 2 its two locked doors at .1 and .3 and an open front at
    /// .2 (its key lies on that bay's floor), a wall at .5-.7 (2.5's plinth stands against it, 2.6 and 2.7 are frames); room 3
    /// a window at .1-.3 (the right wall, so the lane is the window's lower cell and the kit's face A is on it), an alarm door
    /// at .5, the ALARM ZONE arch at .6 and a wall at .7 (FREE); room 4 a wall (its kits stand against it).
    /// </summary>
    static EdgeKind FrontKind(int room, int slot)
    {
        if (room == 1) return EdgeKind.Door;
        if (room == 2) return slot == 1 || slot == 3 ? EdgeKind.Door : slot == 2 ? EdgeKind.Open : EdgeKind.Wall;
        if (room != 3) return EdgeKind.Wall;
        if (slot >= 1 && slot <= 3) return EdgeKind.Window;
        return slot == 5 ? EdgeKind.Door : slot == 6 ? EdgeKind.Arch : EdgeKind.Wall;
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
        // A bay's front: what its station needs (FrontKind); the station's framed code hangs over it.
        if (pa == MuseumPart.Bay && pb == MuseumPart.Lane) return FrontKind(ra, SlotOfBay(ia, ja));
        if (pa == MuseumPart.Lane && pb == MuseumPart.Bay) return FrontKind(rb, SlotOfBay(ib, jb));
        // 3.4: room 3's alcove looks through an office window into its lit right corner (the alcove is the lower cell).
        if (ra == 3 && ((pa == MuseumPart.Alcove && ia == 2 && pb == MuseumPart.Corner && ib == 3)
            || (pb == MuseumPart.Alcove && ib == 2 && pa == MuseumPart.Corner && ia == 3))) return EdgeKind.Window;
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
    FrontRoomsMapWorld.Door MuseumDoor(int slot) => MuseumDoorAt(1, slot);

    /// <summary>The built door on a room's bay front (room 1's six, room 2's two locked ones, room 3's alarm door), or null.</summary>
    FrontRoomsMapWorld.Door MuseumDoorAt(int room, int slot)
    {
        GridCoord bay, lane;
        if (map == null || FrontKind(room, slot) != EdgeKind.Door || !MuseumFront(room, slot, out bay, out lane)) return null;
        return map.DoorBetween(bay, lane);
    }

    bool IsMuseumDoor(FrontRoomsMapWorld.Door door) => door != null && museumLiveEdges.Contains(door.edge);

    /// <summary>A room-3 window's two cells, a the lower (the lane, or the alcove for 3.4) and b the closet: slots 1-3 and 4.</summary>
    bool MuseumWindowCells(int slot, out GridCoord a, out GridCoord b)
    {
        a = b = default(GridCoord);
        int u, v;
        if (slot == 4)
        {
            LocalToMuseum(3, 2, 3, out u, out v);
            a = MuseumCell(u, v);
            LocalToMuseum(3, 3, 3, out u, out v);
            b = MuseumCell(u, v);
            return true;
        }
        if (slot < 1 || slot > 3) return false;
        return MuseumFront(3, slot, out b, out a);
    }

    /// <summary>A room-3 window's record as the map last built it (WindowBuilt), or null.</summary>
    FrontRoomsMapWorld.Window MuseumWindowAt(int slot)
    {
        GridCoord a, b;
        FrontRoomsMapWorld.Window w;
        if (map == null || !MuseumWindowCells(slot, out a, out b)) return null;
        return museumWindows.TryGetValue(MapGrid.EdgeId(a, b), out w) && w != null && w.root != null ? w : null;
    }

    bool ExhibitOk(int room, int slot) => room >= 0 && room < 5 && slot >= 0 && slot < 8 && museumExhibitOk[room * 8 + slot];

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
        if (slot == 4 && room == 3)
        {
            // 3.4: inside the alcove, a metre from its window into the lit corner; the code over the window.
            stand = new Vector2(3f - WindowStand / MapGrid.CellSize, 3.5f);
            face = new Vector2(1f, 0f);
            anchor = new Vector2(3f, 3.5f);
            normal = new Vector2(-1f, 0f);
            height = DoorTagHeight;
        }
        else if (slot == 4)
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
            // The lane cell in front of the bay, facing its front (a window's stop a metre from the glass; 2.2's just inside its
            // open bay, the key on the floor within reach); the code over the opening, over a kit (room 4's, 2.5's plinth), or on
            // a bare wall at eye height.
            var right = i == 3;
            var window = FrontKind(room, slot) == EdgeKind.Window;
            var keyBay = room == 2 && slot == 2;
            // Room 4's terminal desk and test-bed row stand off their bays' middles (clear of the hub's arches): so do their stops.
            var along = room == 4 && slot == 1 ? TerminalAlong : room == 4 && slot == 7 ? DevRowAlong : j + .5f;
            var rightStand = window ? 3f - WindowStand / MapGrid.CellSize : keyBay ? 3f + KeyStand / MapGrid.CellSize : 2.5f;
            stand = new Vector2(right ? rightStand : 1.5f, along);
            face = new Vector2(right ? 1f : -1f, 0f);
            anchor = new Vector2(right ? 3f : 1f, along);
            normal = new Vector2(right ? -1f : 1f, 0f);
            height = FrontKind(room, slot) != EdgeKind.Wall || (room == 4 && slot != 2) || (room == 2 && slot == 5) ? DoorTagHeight : WallTagHeight;
        }
        s.stand = LocalPoint(room, stand.x, stand.y);
        s.face = LocalDir(room, face.x, face.y);
        s.anchor = LocalPoint(room, anchor.x, anchor.y);
        s.normal = LocalDir(room, normal.x, normal.y);
        s.height = height;
        s.ok = true;
        return s;
    }

    /// <summary>Where a walk to a station looks: 2.2 down at its key on the bay floor, 2.5 at the tag on its plinth, every other
    /// stop at its front at MuseumLookHeight.</summary>
    Vector3 MuseumLookAt(int room, int slot, MuseumSpot spot)
    {
        if (room == 2 && slot == 2)
        {
            var key = MuseumKeyLying();
            return key != null ? key.position + Vector3.up * .02f : MuseumWorld(LocalPoint(2, 3.5f, 1.5f), .03f);
        }
        if (room == 2 && slot == 5 && ExhibitOk(2, 5)) return museumTagLook;
        return MuseumWorld(spot.anchor, MuseumLookHeight(room, slot));
    }

    /// <summary>Room 2's key as it lies on 2.2's floor (the map's nearest lying key, of its zone, in that bay), or null: taken,
    /// or never laid there.</summary>
    Transform MuseumKeyLying()
    {
        if (map == null) return null;
        var at = MuseumWorld(LocalPoint(2, 3.5f, 1.5f), 0f);
        GridCoord zone, chunk;
        var key = map.NearestKeyForTools(at, out zone, out chunk);
        if (key == null || zone != museumKeyZone) return null;
        var d = key.position - at;
        return new Vector2(d.x, d.z).sqrMagnitude < 1f ? key : null;
    }

    /// <summary>How high a walk to a station looks at its front: a door's leaf, a pane or latch, the arch with its card and
    /// detector, room 4's kit (a CRT on a desk, the placard, the papers, the test-bed row), 2.5's plinth top.</summary>
    static float MuseumLookHeight(int room, int slot)
    {
        if (room == 1) return slot == 4 ? 1.4f : 1.3f;
        if (room == 2) return slot == 1 || slot == 3 ? 1.3f : slot == 5 ? 1.2f : 1.4f;
        if (room == 3)
        {
            // 3.1 (break the glass) looks at the upper lite: from its 1 m stop a 1.2 m look meets the map's latch box on the meeting
            // rail (1.105-1.245 m, its face 5 cm off the wall line, in front of the glass), and E would pry the sash, 3.2's verb.
            // 3.2-3.4 look at the rail: 3.2's latch; 3.3 and 3.4 take either verb.
            if (slot == 1) return 1.55f;
            if (slot >= 2 && slot <= 4) return 1.2f;
            return slot == 5 ? 1.3f : slot == 6 ? 1.75f : 1.4f;
        }
        if (room == 4)
        {
            switch (slot)
            {
                case 1: return 1f;
                case 3: return .95f;
                case 5: return FrontRoomsPlacard.CentreHeight;
                case 6: return .9f;
                case 7: return 1.2f;
            }
        }
        return 1.4f;
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
            var lost = "";
            for (var slot = 1; slot <= 4; slot++)
                if (MuseumWindowAt(slot) == null) lost += " 3." + slot;
            var alarm = MuseumDoorAt(3, 5);
            if (alarm == null) lost += " 3.5";
            if (lost.Length > 0) MuseumNote("Room 3 built without" + lost + " (no record from the map): those stations stand as frames.");
#if UNITY_EDITOR
            if (alarm != null && !alarm.alarm) MuseumNote("3.5 was built as a plain door (the map's tools mark did not take): it stands as a frame.");
#endif
            if (museumKeysLive) CheckMuseumRoomTwo();
            if (player == null)
            {
                MuseumFail("there is no player to walk in", null);
                return;
            }
            // Room 3's ALARM ZONE tell and room 4's kits: under our own root, each on its own (one that fails stays a frame).
            BuildMuseumExhibits();
            // The placards, once the exhibits stand (C keeps clear of their kits and out of their view): one root of their own.
            BuildMuseumPlacards();
            // The stream follows the player from now on (it held the start door); the start rooms drop behind.
            map.StreamFocus = null;
            Physics.SyncTransforms();
            MuseumMove(HubSpawn, MuseumWorld(HubSpawn + new Vector2(0f, 3f), ModuleUnits.PlayerEye));
            museumState = MuseumState.Built;
            museumMarkRoom = 0;
            museumMarkSlot = -1;
            museumTagClock = 0f;
            Toast("MUSEUM  ·  0  HUB", "Hub 0 and four rooms of the game's own cells, doors, windows, keys and kits, " + museumMetres + " m north of the start. "
                + (MuseumKeysRun() ? "All four rooms are live; 2.6, 2.7 and 4.2 stand as placards." : "Rooms 1, 3 and 4 are live; room 2's locks and key stand as placards.")
                + "  [ ] rooms  ·  1-7 stations  ·  P the plan.");
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

    /// <summary>
    /// Room 2 as built: both doors there and locked by our rule (so the kit dressed them LOCKED), and its key moved onto 2.2 (the
    /// key's home chunk carries the carve's move: on the floor of 2.2's bay, no key spot). A key whose home is a third chunk (a
    /// neighbour's zone over all of room 2) is built now, so it lies there at arrival. Anything missing: room 2's doors go free
    /// (a lock with no key to take would trap the visitor's test), its stations stand as frames, one note.
    /// </summary>
    void CheckMuseumRoomTwo()
    {
        var lost = "";
        for (var slot = 1; slot <= 3; slot += 2)
        {
            var door = MuseumDoorAt(2, slot);
            if (door == null || !map.LockedByRule(door)) lost += " 2." + slot;
        }
        if (!map.IsChunkBuiltAt(MapGrid.ChunkOrigin(museumKeyZone))) map.RebuildChunk(museumKeyZone);
        GridCoord bay, lane;
        MapChunk data;
        // The carve's move took (the home chunk's data), and the map laid the key there (the key object, lying in a built chunk).
        museumKeyMoved = MuseumFront(2, 2, out bay, out lane) && map.Cache.TryGetGenerated(museumKeyZone, out data) && data != null
            && data.hasKey && !data.keySpot && data.keySiteCell == bay && MuseumKeyLying() != null;
        if (!museumKeyMoved) lost += " 2.2";
        if (lost.Length == 0) return;
        museumKeysLive = false;
        museumLockedEdges.Clear();
        MuseumNote("Room 2 built without" + lost + " (no door record, a lock that did not take, or the key's move): its doors stay free and its stations stand as frames.");
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

    // ---------- 2.5's key tag, room 3's ALARM ZONE tell and room 4's kits (90 §5 rows 2.5, 3.6 and 4.x): the game's own kits

    /// <summary>
    /// Every kit object of the museum, under one root of ours in the game scene (it goes with the scene; ClearMuseumTools takes it
    /// down sooner). Each station on its own: one that fails says so on the list and stands as a frame. Kits spawn through
    /// FrontRoomsKitLibrary.Spawn with their close-ups (inspectable; the test bed's with DevTargets, on in every tour by default).
    /// </summary>
    void BuildMuseumExhibits()
    {
        DestroyMuseumExhibits();
        museumExhibits = new GameObject("Scene 3 museum · rooms 2-4 kits (交互设计)");
        var root = museumExhibits.transform;
        Exhibit(2, 5, root);
        Exhibit(3, 6, root);
        Exhibit(4, 1, root);
        Exhibit(4, 3, root);
        Exhibit(4, 5, root);
        Exhibit(4, 6, root);
        Exhibit(4, 7, root);
    }

    void DestroyMuseumExhibits()
    {
        if (museumExhibits != null) Destroy(museumExhibits);
        museumExhibits = null;
        Array.Clear(museumExhibitOk, 0, museumExhibitOk.Length);
        Array.Clear(museumDevOk, 0, museumDevOk.Length);
        museumTerminal = null;
        museumAdScreens = 0;
        museumDev = -1;
        museumTagLook = Vector3.zero;
    }

    void Exhibit(int room, int slot, Transform root)
    {
        string why;
        try
        {
            why = room == 2 ? ExhibitKeyTag(root)
                : room == 3 ? ExhibitAlarmZone(root)
                : slot == 1 ? ExhibitTerminal(root)
                : slot == 3 ? ExhibitAds(root)
                : slot == 5 ? ExhibitPlacard(root)
                : slot == 6 ? ExhibitPapers(root)
                : ExhibitDevRow(root);
        }
        catch (Exception e)
        {
            why = e.GetType().Name + ": " + e.Message;
        }
        museumExhibitOk[room * 8 + slot] = why == null;
        if (why != null) MuseumNote(room + "." + slot + " was not set up (" + why + "): it stands as a frame.");
    }

    /// <summary>A bay front's lane-side wall face in room cells (its x) and the way into the lane along x (+1 or -1).</summary>
    static void FrontFace(int slot, out float faceX, out float intoLane)
    {
        var right = slot >= 1 && slot <= 3;
        intoLane = right ? -1f : 1f;
        faceX = (right ? 3f : 1f) + intoLane * ModuleUnits.WallHalf / MapGrid.CellSize;
    }

    /// <summary>A world point off a bay front's wall face: `along` room cells along the wall, `off` metres into the lane, `height` metres up.</summary>
    Vector3 FrontPoint(int room, int slot, float along, float off, float height)
    {
        float faceX, intoLane;
        FrontFace(slot, out faceX, out intoLane);
        return MuseumWorld(LocalPoint(room, faceX + intoLane * off / MapGrid.CellSize, along), height);
    }

    /// <summary>The turn of a kit on a bay front: its front (+Z) to the lane.</summary>
    Quaternion FrontTurn(int room, int slot)
    {
        float faceX, intoLane;
        FrontFace(slot, out faceX, out intoLane);
        return Quaternion.LookRotation(MuseumWorldDir(LocalDir(room, intoLane, 0f)).normalized, map.transform.up);
    }

    /// <summary>A floor kit with its back to a bay front: its footprint's back `gap` off the wall face, the footprint centred
    /// `along` room cells along the wall, facing the lane (the root of the exhibits sits at the world's origin).</summary>
    GameObject KitAgainstFront(Transform root, int room, int slot, string kit, float along, float gap, bool colliders, string label)
    {
        var info = FrontRoomsKitLibrary.GetInfo(kit);
        var fp = info != null ? info.Footprint : new Rect(-.25f, -.25f, .5f, .5f);
        var turn = FrontTurn(room, slot);
        var at = FrontPoint(room, slot, along, gap - fp.yMin, 0f) - turn * new Vector3(fp.center.x, 0f, 0f);
        return FrontRoomsKitLibrary.Spawn(kit, root, at, turn, null, colliders, label);
    }

    /// <summary>A kit on another (a CRT on a desk, the phone on the cabinet): a point in the host's frame and a turn about its up.</summary>
    static GameObject KitOn(Transform root, GameObject host, string kit, Vector3 local, float yaw, bool colliders, string label)
    {
        if (host == null) return null;
        var t = host.transform;
        return FrontRoomsKitLibrary.Spawn(kit, root, t.TransformPoint(local), t.rotation * Quaternion.Euler(0f, yaw, 0f), null, colliders, label);
    }

    static float SupportTop(string kit, float fallback)
    {
        var info = FrontRoomsKitLibrary.GetInfo(kit);
        Vector3 centre;
        Vector2 size;
        return info != null && info.TrySupport("top", out centre, out size) ? centre.y : fallback;
    }

    /// <summary>
    /// 2.5: the tag of 2.2's key (its zone's FrontRoomsKeyIdentity.Stub tag kit, the one the map's dressed key carries) as a kit
    /// object on a plinth against 2.5's wall: Kit_PlyCabinet, its top at 1.2 m. The tag lies flat on the top, face up, its hole
    /// toward the wall so the printed code reads from the lane, a little turned as if set down. Its close-up is the sidecar's
    /// (kind object, ship false: with DevTargets, on in the museum), and every Kit_KeyTag_* asks E · EXAMINE KEY.
    /// </summary>
    string ExhibitKeyTag(Transform root)
    {
        var id = FrontRoomsKeyIdentity.Stub(museumKeyZone);
        var kit = id.TagKit;
        if (!FrontRoomsKitLibrary.Has(kit)) return "no " + kit;
        var plinth = KitAgainstFront(root, 2, 5, KeyTagPlinth, 2.5f, UnitGap, true, "2.5 plinth (" + KeyTagPlinth + ")");
        if (plinth == null) return "no " + KeyTagPlinth;
        var t = plinth.transform;
        var at = t.TransformPoint(new Vector3(.06f, SupportTop(KeyTagPlinth, 1.2f) + TagHalfThick, .02f));
        var turn = t.rotation * Quaternion.Euler(0f, -14f, 0f) * Quaternion.Euler(-90f, 0f, 0f);
        var tag = FrontRoomsKitLibrary.Spawn(kit, root, at, turn, null, false, "2.5 key tag (" + kit + ")", true);
        if (tag == null) return "no " + kit;
        var target = tag.GetComponent<FrontRoomsInspectable>();
        if (target == null) return FrontRoomsInspect.DevTargets ? "the tag came without its close-up" : "DevTargets was off when it spawned (the tag's close-up ships off)";
        museumTagLook = target.Face.position;
        return null;
    }

    /// <summary>
    /// 3.6: the ALARM ZONE tell on 3.6's arch, dressed as FrontRoomsRunRigBinder.DressTell dresses a Run! room's entrance arch: the
    /// entrance frame on the floor at the opening's centre, +Z into the trigger room (the bay), the card on the approacher's left
    /// outside, the red-banded detector on the ceiling inside at the sight rule's depth (FrontRoomsAlarmTell.Dress).
    /// </summary>
    string ExhibitAlarmZone(Transform root)
    {
        GridCoord bay, lane;
        if (!MuseumFront(3, 6, out bay, out lane)) return "no front";
        if (map.EffectiveEdge(lane, bay) != EdgeKind.Arch) return "its front was not built as an arch";
        var into = map.CellCenter(bay) - map.CellCenter(lane);
        var entrance = new GameObject("3.6 ALARM ZONE entrance").transform;
        entrance.SetParent(root, false);
        entrance.SetPositionAndRotation((map.CellCenter(bay) + map.CellCenter(lane)) * .5f, Quaternion.LookRotation(into.normalized, map.transform.up));
        var dress = FrontRoomsAlarmTell.Dress(entrance, FrontRoomsRunRooms.ArchWidth, ModuleUnits.WallThickness, MapGrid.CeilingHeight(ZoneHeight.Standard),
            ModuleUnits.ArchTop, -1, MapGrid.CellSize - ModuleUnits.WallThickness);
        return dress != null ? null : "FrontRoomsAlarmTell.Dress spawned neither the card nor the detector";
    }

    /// <summary>
    /// 4.1: a desk CRT made a terminal, as the office kit stands a workstation (desk back to the wall, the CRT on its monitor
    /// anchor, keyboard and mouse in front): the CRT's screen first (FrontRoomsScreenVideo.AttachDesk, the office kit's), then
    /// FrontRoomsTerminalScreen.Attach (content set 0), which makes its E · READ SCREEN close-up.
    /// </summary>
    string ExhibitTerminal(Transform root)
    {
        var desk = KitAgainstFront(root, 4, 1, FrontRoomsOfficeKit.Desk, TerminalAlong, DeskGap, true, "4.1 terminal desk");
        if (desk == null) return "no " + FrontRoomsOfficeKit.Desk;
        var info = FrontRoomsKitLibrary.GetInfo(FrontRoomsOfficeKit.Desk);
        var top = SupportTop(FrontRoomsOfficeKit.Desk, .74f);
        Vector3 monitorAt, keysAt;
        if (info == null || !info.TryAnchor("monitor", out monitorAt)) monitorAt = new Vector3(0f, top, -.1f);
        if (info == null || !info.TryAnchor("keyboard", out keysAt)) keysAt = new Vector3(0f, top, .2f);
        var monitor = KitOn(root, desk, FrontRoomsOfficeKit.Monitor, monitorAt, 0f, true, "4.1 terminal CRT");
        if (monitor == null) return "no " + FrontRoomsOfficeKit.Monitor;
        KitOn(root, desk, FrontRoomsOfficeKit.Keyboard, keysAt, 0f, false, "4.1 keyboard");
        KitOn(root, desk, FrontRoomsOfficeKit.Mouse, keysAt + new Vector3(.3f, 0f, .02f), 0f, false, "4.1 mouse");
        if (FrontRoomsScreenVideo.AttachDesk(monitor) == null) return "the CRT took no screen";
        museumTerminal = FrontRoomsTerminalScreen.Attach(monitor, 0);
        if (museumTerminal == null) return "FrontRoomsTerminalScreen.Attach refused the CRT";
        return museumTerminal.ReadTarget != null ? null : "the terminal has no READ target (no screen surface in this player)";
    }

    /// <summary>4.3: three CRTs on a credenza with the media bay's ad and its E power switch (FrontRoomsScreenVideo.Attach, as
    /// Editor/FrontRoomsScreenAdsDemoScene.cs does; its distances and power are the component's defaults, no sound role).</summary>
    string ExhibitAds(Transform root)
    {
        const string stand = "Kit_Credenza";
        var credenza = KitAgainstFront(root, 4, 3, stand, 2.5f, UnitGap, true, "4.3 credenza");
        if (credenza == null) return "no " + stand;
        var top = SupportTop(stand, .76f);
        museumAdScreens = 0;
        for (var k = -1; k <= 1; k++)
        {
            // The media bay's toe-in: the outer two turned 5 degrees toward the middle one.
            var monitor = KitOn(root, credenza, FrontRoomsOfficeKit.Monitor, new Vector3(k * .5f, top, 0f), -5f * k, true, "4.3 CRT ad " + (k + 2));
            if (monitor != null && FrontRoomsScreenVideo.Attach(monitor, FrontRoomsScreenVideo.DefaultStreamFile) != null) museumAdScreens++;
        }
        return museumAdScreens > 0 ? null : "no CRT took its ad screen";
    }

    /// <summary>4.5: the evacuation placard as FrontRoomsPlacardMount hangs it (frame and lens 0.5 mm proud at 1.56 m, no shadow,
    /// the photoluminescent glow bound to the lane cell); the kit's own close-up, E · READ PLAN and T for the text.</summary>
    string ExhibitPlacard(Transform root)
    {
        var at = FrontPoint(4, 5, 2.5f, FrontRoomsPlacard.StationProud, FrontRoomsPlacard.CentreHeight);
        var turn = FrontTurn(4, 5);
        var frame = FrontRoomsKitLibrary.Spawn(FrontRoomsPlacard.KitName, root, at, turn, null, false, "4.5 evacuation placard");
        if (frame == null) return "no " + FrontRoomsPlacard.KitName;
        var renderers = new List<Renderer>(frame.GetComponentsInChildren<Renderer>(true));
        var lens = FrontRoomsKitLibrary.Spawn(FrontRoomsPlacard.LensKitName, root, at, turn, null, false, "4.5 evacuation placard lens");
        foreach (var r in renderers)
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = true;
        }
        if (lens != null)
            foreach (var r in lens.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = true;
            }
        GridCoord bay, lane;
        if (MuseumFront(4, 5, out bay, out lane)) frame.AddComponent<FrontRoomsPlacardGlow>().Bind(map, relay, lane, renderers);
        return frame.GetComponent<FrontRoomsInspectable>() != null ? null : "the placard came without its close-up";
    }

    /// <summary>4.6: the station's three dated kits (FrontRoomsStationPaper.DatedKits) as the B1 desk station lays its papers: the
    /// clipboard and the register on a desk with its blotter, the sign-out sheet on the wall over it.</summary>
    string ExhibitPapers(Transform root)
    {
        var desk = KitAgainstFront(root, 4, 6, FrontRoomsOfficeKit.Desk, PapersAlong, DeskGap, true, "4.6 station desk");
        if (desk == null) return "no " + FrontRoomsOfficeKit.Desk;
        var top = SupportTop(FrontRoomsOfficeKit.Desk, .74f) + .001f;
        KitOn(root, desk, "Kit_DeskBlotter", new Vector3(.05f, top, 0f), 0f, false, "4.6 desk blotter");
        var register = KitOn(root, desk, "Kit_RegisterBook", new Vector3(-.47f, top, .03f), 3f, false, "4.6 register");
        var clipboard = KitOn(root, desk, "Kit_Clipboard", new Vector3(.55f, top, 0f), -6f, false, "4.6 clipboard (daily activity report)");
        var sheet = FrontRoomsKitLibrary.Spawn("Kit_Clipboard_Wall", root, FrontPoint(4, 6, PapersAlong, FlushProud, 0f), FrontTurn(4, 6), null, false, "4.6 sign-out sheet");
        return register != null || clipboard != null || sheet != null ? null : "none of the three dated kits spawned";
    }

    /// <summary>
    /// 4.7: the test bed's four close-ups in one row along the wall, from the hub side: a filing cabinet with the desk phone on
    /// it and the wall clock over it at 2.1 m (the catalog's look-up pose), the copier, the vending machine. Their close-ups are
    /// the catalog's fallbacks (ship false), attached at spawn while DevTargets is on. A stop in front of each for 7 again.
    /// </summary>
    string ExhibitDevRow(Transform root)
    {
        const float cs = MapGrid.CellSize;
        const string cabinetKit = "Kit_FilingCabinet";
        var cabinet = KitAgainstFront(root, 4, 7, cabinetKit, 1.25f / cs, UnitGap, true, "4.7 filing cabinet");
        var phone = KitOn(root, cabinet, "Kit_DeskPhone", new Vector3(0f, SupportTop(cabinetKit, 1.32f), .06f), -12f, false, "4.7 A desk phone");
        var clock = FrontRoomsKitLibrary.Spawn("Kit_WallClock", root, FrontPoint(4, 7, 1.25f / cs, FlushProud, 2.1f), FrontTurn(4, 7), null, false, "4.7 B wall clock");
        var vending = KitAgainstFront(root, 4, 7, "Kit_VendingMachine", 3.15f / cs, UnitGap, true, "4.7 C vending machine");
        var copier = KitAgainstFront(root, 4, 7, "Kit_Copier", 2.15f / cs, UnitGap, true, "4.7 D copier");
        DevStop(0, phone, 1.25f / cs, UnitGap + .66f + .8f);
        DevStop(1, clock, 1.25f / cs, 1.5f);
        DevStop(2, vending, 3.15f / cs, UnitGap + .84f + .8f);
        DevStop(3, copier, 2.15f / cs, UnitGap + .6f + .8f);
        var n = 0;
        for (var k = 0; k < DevCount; k++) if (museumDevOk[k]) n++;
        if (n == 0) return FrontRoomsInspect.DevTargets ? "no test-bed kit came with its close-up" : "DevTargets was off when they spawned";
        return null;
    }

    // One of 4.7's stops: a close-up kit and where to stand for it (off the wall face, at its place along the wall), facing its face.
    void DevStop(int k, GameObject kit, float along, float off)
    {
        var target = kit != null ? kit.GetComponent<FrontRoomsInspectable>() : null;
        museumDevOk[k] = target != null;
        if (target == null) return;
        float faceX, intoLane;
        FrontFace(7, out faceX, out intoLane);
        museumDevStand[k] = LocalPoint(4, faceX + intoLane * off / MapGrid.CellSize, along);
        museumDevLook[k] = target.Face.position;
    }

    // ---------- the world placards (inc 8b): FrontRoomsShowcasePlacards to 20_placard_type.md v2, placed by 10_research §3.2-3.7

    /// <summary>
    /// One room's keep-outs for its placards, in room-local metres: x from the room's left wall line as one walks in (its cells
    /// i), y from its hub wall line (cells j); boxes are (x, height, y). The model (land_inc8c/placement/model.py) builds the same.
    /// </summary>
    sealed class PlacardScene
    {
        public int room;
        /// <summary>Where a stand may stand: the lane with its alcove, an open bay (2.2).</summary>
        public readonly List<Rect> floor = new List<Rect>();
        /// <summary>Kits on the floor, floor tags, and the C placards set so far (plan polygons).</summary>
        public readonly List<Vector2[]> blocks = new List<Vector2[]>();
        /// <summary>Every leaf's sweep into the lane, for each jamb its hinge can take (either door mode).</summary>
        public readonly List<Vector2[]> swings = new List<Vector2[]>();
        /// <summary>The arches' openings with their first ArchPassage into the room.</summary>
        public readonly List<Vector2[]> passages = new List<Vector2[]>();
        /// <summary>W1's stop area at every stop.</summary>
        public readonly List<Vector2[]> stops = new List<Vector2[]>();
        /// <summary>The room's kit renderers as boxes, and the exhibit object each belongs to (BuildMuseumExhibits' labels).</summary>
        public readonly List<Bounds> kits = new List<Bounds>();
        public readonly List<string> kitNames = new List<string>();
        /// <summary>E's and X's centres on the hub wall (x) and half spans: the carve's own for an authored arch, else the widest
        /// the map's hash can give an arch centred there (ArchOpening: 1.1-1.8 m, 0.2 m from the cell's corners).</summary>
        public float eCentre, eHalf, xCentre, xHalf;
    }

    /// <summary>
    /// Every placard, once per museum build, after the exhibits stand (BuildMuseum; no other tour builds the museum): C at every
    /// station of rooms 1-4 (1.1 both variants at one place, the door's shown), D and the floor tags at the variant rows 1.7 and
    /// 4.7, B just inside each room, then on the hub side B beside each entry, the room number over it and A facing the spawn.
    /// Under one root of ours (unit scale, at the origin), with the hub's and each room's parts apart (TickMuseumPlacards turns a
    /// room's on and off). A part that fails is left out with one note; the museum stands all the same. Nothing collides: the
    /// builder makes no collider (a code mesh, no primitive; no raycaster, raycastTarget off) and any found is switched off, so the
    /// game's USE ray (FrontRooms3DGame.AimCast: every layer, 2.4 m), the museum's own casts and the player's body pass through.
    /// </summary>
    void BuildMuseumPlacards()
    {
        DestroyMuseumPlacards();
        museumPlacards = new GameObject("Scene 3 museum · placards (交互设计)");
        var root = museumPlacards.transform;
        root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        root.localScale = Vector3.one;
        var groups = new Transform[5];
        for (var r = 0; r <= 4; r++)
        {
            groups[r] = new GameObject(r == 0 ? "Hub" : "Room " + r).transform;
            groups[r].SetParent(root, false);
        }
        for (var r = 1; r <= 4; r++)
        for (var k = 0; k < 2; k++)
        {
            int u, v;
            LocalToMuseum(r, 2 - k, 0, out u, out v);
            var outward = LocalDir(r, 0f, -1f);
            museumEntryCells[2 * (r - 1) + k] = new Vector2Int(u + Mathf.RoundToInt(outward.x), v + Mathf.RoundToInt(outward.y));
        }
        var lost = "";
        var scenes = new PlacardScene[5];
        for (var room = 1; room <= 4; room++)
        {
            try
            {
                scenes[room] = MuseumPlacardScene(room);
                lost += BuildRoomPlacards(scenes[room], groups[room]);
            }
            catch (Exception e)
            {
                lost += " room " + room + " (" + e.GetType().Name + ": " + e.Message + ")";
            }
        }
        try
        {
            lost += BuildHubPlacards(scenes, groups[0]);
        }
        catch (Exception e)
        {
            lost += " the hub's (" + e.GetType().Name + ": " + e.Message + ")";
        }
        var colliders = 0;
        foreach (var c in root.GetComponentsInChildren<Collider>(true))
        {
            if (!c.enabled) continue;
            c.enabled = false;
            colliders++;
        }
        if (colliders > 0) Debug.LogWarning("Interactables showcase (museum): " + colliders + " placard collider(s) switched off (the USE ray and the walk pass through placards).");
        for (var r = 0; r <= 4; r++)
        {
            museumPlacardCanvases[r].Clear();
            museumPlacardRenderers[r].Clear();
            groups[r].GetComponentsInChildren(true, museumPlacardCanvases[r]);
            groups[r].GetComponentsInChildren(true, museumPlacardRenderers[r]);
        }
        // Built with every room's on: from the first frame only the rooms the player stands at (the tick follows each move).
        ShowMuseumPlacardRooms(MuseumPlacardMask());
        museumLiveClock = 0f;
        SyncMuseumPlacardVariants();
        if (lost.Length > 0) MuseumNote("Placards left out:" + lost + ".");
    }

    void DestroyMuseumPlacards()
    {
        if (museumPlacards != null) Destroy(museumPlacards);
        museumPlacards = null;
        Array.Clear(museumPlacardC, 0, museumPlacardC.Length);
        museumPlacardC11B = null;
        Array.Clear(museumPlacardFrame, 0, museumPlacardFrame.Length);
        museumPlacardFrame11B = null;
        Array.Clear(museumHubSigns, 0, museumHubSigns.Length);
        Array.Clear(museumSignArrows, 0, museumSignArrows.Length);
        Array.Clear(museumMemberTags, 0, museumMemberTags.Length);
        for (var r = 0; r < museumPlacardCanvases.Length; r++)
        {
            museumPlacardCanvases[r].Clear();
            museumPlacardRenderers[r].Clear();
        }
        museumPlacardMask = museumPlacardMember = -1;
    }

    /// <summary>A room's inside placards: D and the floor tags at a variant row (the tags are kept-out floor for C), B just
    /// inside, then C at each station in order. Returns what was left out (empty: nothing).</summary>
    string BuildRoomPlacards(PlacardScene sc, Transform parent)
    {
        var room = sc.room;
        var lost = "";
        for (var slot = 1; slot <= 7; slot++)
        {
            var row = FrontRoomsShowcaseTable.At(room, slot);
            if (row != null && row.group != null) lost += BuildRowPlacards(sc, slot, row, parent);
        }
        Vector2 wallPoint, outward;
        if (!MuseumInsideSign(sc, out wallPoint, out outward)) lost += " " + room + " B inside (no wall wide enough)";
        else
        {
            // Its arrow the way its reader walks to .1's stop (10 §3.1: the inside sign's arrow gives the order).
            Vector2 first, firstFace;
            var sign = PlacardStop(room, 1, out first, out firstFace) ? SignToward(RoomSign(room), wallPoint, outward, first) : RoomSign(room);
            if (FrontRoomsShowcasePlacards.BuildRoomSign(sign, parent,
                    FrontRoomsShowcasePlacards.OnWall(RoomWorld(room, wallPoint, 0f), RoomWorldDir(room, outward), FrontRoomsShowcasePlacards.RoomSignCentre)) == null)
                lost += " " + room + " B inside";
            else museumSignArrows[room] = sign.signArrow;
        }
        var samples = new List<Vector3>();
        for (var slot = 1; slot <= 7; slot++)
        {
            var st = room == 1 && slot == 1 ? FrontRoomsShowcaseTable.Find("1.1A") : FrontRoomsShowcaseTable.At(room, slot);
            if (st == null) continue;
            Vector2 floorPoint, toward;
            int bearing;
            string fails;
            if (!MuseumPlaceStation(sc, slot, samples, out floorPoint, out toward, out bearing, out fails))
            {
                lost += " " + st.code + " C (no stop)";
                continue;
            }
            if (fails != null)
                Debug.LogWarning("Interactables showcase (museum): " + st.code + "'s placard stands at " + bearing + " degrees, the least crowded place, but " + fails + ".");
            var pose = FrontRoomsShowcasePlacards.Standing(RoomWorld(room, floorPoint, 0f), RoomWorldDir(room, toward));
            museumPlacardC[room * 8 + slot] = FrontRoomsShowcasePlacards.BuildStationPlacard(st, parent, pose, true);
            museumPlacardFrame[room * 8 + slot] = MuseumFrameWord(st);
            if (museumPlacardC[room * 8 + slot] == null) lost += " " + st.code + " C";
            if (room == 1 && slot == 1)
            {
                // 1.1 is one door, A body / B E (the floor's switch): both placards stand at its place, the door's variant shows.
                var b = FrontRoomsShowcaseTable.Find("1.1B");
                museumPlacardC11B = FrontRoomsShowcasePlacards.BuildStationPlacard(b, parent, pose, true);
                museumPlacardFrame11B = MuseumFrameWord(b);
                if (museumPlacardC11B == null) lost += " 1.1B C";
            }
        }
        return lost;
    }

    /// <summary>
    /// A variant row (10_research §3.4 D; 20 §2): card D on the front at the row's left end as seen from the lane, centred in the
    /// wall between that corner and the row, GroupCentre high; a floor tag before each variant, read from TagStandBehind behind
    /// it. 1.7 is one door showing its four members in turn: its four tags lie at one place before it, clear of its leaf's sweep,
    /// and the member on the door shows. 4.7's four close-ups stand along the wall: each tag's far edge TagGap off the kit it
    /// stands before (the phone and the clock share the cabinet: two tags side by side). Returns what was left out.
    /// </summary>
    string BuildRowPlacards(PlacardScene sc, int slot, FrontRoomsShowcaseTable.Station st, Transform parent)
    {
        var room = sc.room;
        var cs = MapGrid.CellSize;
        var wh = ModuleUnits.WallHalf;
        int i, j;
        if (!BayOfSlot(slot, out i, out j)) return " " + st.code + " D";
        float faceX, into;
        FrontFace(slot, out faceX, out into);
        faceX *= cs;
        var lost = "";
        float lo, hi;
        FrontSpan(sc, slot, out lo, out hi);
        // Facing the front from the lane: on the left wall (into +1, facing -x) the left is -y; on the right wall it is +y.
        var leftIsLow = into > 0f;
        var corner = leftIsLow ? j * cs + wh : (j + 1) * cs - wh;
        var edge = leftIsLow ? lo : hi;
        if (Mathf.Abs(edge - corner) >= FrontRoomsShowcasePlacards.GroupWidth + 2f * SignGap - 1e-4f)
        {
            var pose = FrontRoomsShowcasePlacards.OnWall(RoomWorld(room, new Vector2(faceX, (corner + edge) * .5f), 0f), RoomWorldDir(room, new Vector2(into, 0f)),
                FrontRoomsShowcasePlacards.GroupCentre);
            if (FrontRoomsShowcasePlacards.BuildGroupCard(st, parent, pose) == null) lost += " " + st.code + " D";
        }
        else lost += " " + st.code + " D (no wall wide enough)";
        var g = st.group;
        var count = g.letters != null ? g.letters.Length : 0;
        var toward = new Vector2(-into, 0f);
        var halfDeep = FrontRoomsShowcasePlacards.TagHeight * .5f;
        var halfWide = FrontRoomsShowcasePlacards.TagWidth * .5f;
        if (FrontKind(room, slot) == EdgeKind.Door)
        {
            // Before the door, on its middle, the nearest place clear of every leaf's sweep and kit.
            var wallLine = faceX - into * wh;
            var mid = (j + .5f) * cs;
            var at = Vector2.zero;
            var found = false;
            for (var step = 0; step < 300 && !found; step++)
            {
                at = new Vector2(wallLine + into * (wh + PlacardClear + halfDeep + step * .01f), mid);
                found = KeptClear(RectPolygon(at.x - halfDeep, at.y - halfWide, at.x + halfDeep, at.y + halfWide), sc.swings, PlacardClear)
                    && KeptClear(RectPolygon(at.x - halfDeep, at.y - halfWide, at.x + halfDeep, at.y + halfWide), sc.blocks, PlacardClear);
            }
            if (!found) return lost + " " + st.code + " floor tags";
            sc.blocks.Add(RectPolygon(at.x - halfDeep, at.y - halfWide, at.x + halfDeep, at.y + halfWide));
            var pose = FrontRoomsShowcasePlacards.OnFloor(RoomWorld(room, at, 0f), RoomWorldDir(room, toward));
            for (var k = 0; k < count && k < museumMemberTags.Length; k++)
            {
                museumMemberTags[k] = FrontRoomsShowcasePlacards.BuildFloorTag(st, k, parent, pose);
                if (museumMemberTags[k] == null) lost += " " + st.code + " " + g.letters[k] + " tag";
            }
            return lost;
        }
        // 4.7: each variant before the kit it stands on or over (BuildMuseumExhibits' labels), side by side where they share one.
        for (var k = 0; k < count; k++)
        {
            var stand = k < DevStands.Length ? DevStands[k] : null;
            Bounds kit;
            if (stand == null || !KitBox(sc, stand, out kit))
            {
                lost += " " + st.code + " " + g.letters[k] + " tag";
                continue;
            }
            int share = 0, nth = 0;
            for (var m = 0; m < count && m < DevStands.Length; m++)
            {
                if (DevStands[m] != stand) continue;
                if (m < k) nth++;
                share++;
            }
            var front = into > 0f ? kit.max.x : kit.min.x;
            var y = kit.center.z + (leftIsLow ? 1f : -1f) * (nth - (share - 1) * .5f) * (FrontRoomsShowcasePlacards.TagWidth + TagSideGap);
            var at = new Vector2(front + into * (TagGap + halfDeep), y);
            sc.blocks.Add(RectPolygon(at.x - halfDeep, at.y - halfWide, at.x + halfDeep, at.y + halfWide));
            if (FrontRoomsShowcasePlacards.BuildFloorTag(st, k, parent, FrontRoomsShowcasePlacards.OnFloor(RoomWorld(room, at, 0f), RoomWorldDir(room, toward))) == null)
                lost += " " + st.code + " " + g.letters[k] + " tag";
        }
        return lost;
    }

    /// <summary>The kit each of 4.7's variants stands on or before (A the phone on the cabinet, B the clock over it, C, D).</summary>
    static readonly string[] DevStands = { "4.7 filing cabinet", "4.7 filing cabinet", "4.7 C vending machine", "4.7 D copier" };

    /// <summary>The union of an exhibit object's renderer boxes (room-local), by its label's start.</summary>
    static bool KitBox(PlacardScene sc, string label, out Bounds box)
    {
        box = default(Bounds);
        var found = false;
        for (var k = 0; k < sc.kits.Count; k++)
        {
            if (!sc.kitNames[k].StartsWith(label, StringComparison.Ordinal)) continue;
            if (!found) box = sc.kits[k];
            else box.Encapsulate(sc.kits[k]);
            found = true;
        }
        return found;
    }

    /// <summary>
    /// B just inside a room (10_research §3.4: "beside each entry on the hub side and again just inside"), on bay .1's front, so
    /// its reader finds .1 along the sign's own wall whatever the map did with the arches: by the hub corner, where .1's exhibit
    /// leaves the sign's width and SignGap each side (rooms 1, 2, 4); else past the exhibit, centred in the wall between .1's and
    /// .2's (room 3: the window's casing leaves the corner 0.64 m, the two windows 1.44 m; .1's stop 1.5 m along, 0.92 m out).
    /// Never the hub wall, where some arches leave .1's stop more out than along or no span at all, nor .7's front, where .1 is
    /// across the lane behind the reader. Room-local wall point and the way out of the wall; false where neither is wide enough.
    /// </summary>
    bool MuseumInsideSign(PlacardScene sc, out Vector2 wallPoint, out Vector2 outward)
    {
        var cs = MapGrid.CellSize;
        var wh = ModuleUnits.WallHalf;
        var width = FrontRoomsShowcasePlacards.RoomSignWidth;
        var need = width + 2f * SignGap - 1e-4f;
        wallPoint = outward = Vector2.zero;
        float lo, hi, lo2, hi2;
        if (FrontKind(sc.room, 1) == EdgeKind.Open || !FrontSpan(sc, 1, out lo, out hi)) return false;
        if (lo - wh >= need) wallPoint = new Vector2(3f * cs - wh, wh + SignGap + width * .5f);
        else if (FrontKind(sc.room, 2) != EdgeKind.Open && FrontSpan(sc, 2, out lo2, out hi2) && lo2 - hi >= need)
            wallPoint = new Vector2(3f * cs - wh, (hi + lo2) * .5f);
        else return false;
        outward = new Vector2(-1f, 0f);
        return true;
    }

    /// <summary>
    /// Sign B's row with its arrow the way its reader walks: → when <paramref name="target"/> (room-local: .1's stop inside, E on
    /// the hub side) lies to the reader's right along the wall, else ← (the table writes "→ n.1"; a copy of its room with the
    /// arrow turned, the rest as written). The reader faces the sign (-outward); their right is (facing.y, -facing.x).
    /// </summary>
    static FrontRoomsShowcaseTable.Room SignToward(FrontRoomsShowcaseTable.Room room, Vector2 wallPoint, Vector2 outward, Vector2 target)
    {
        if (room == null || string.IsNullOrEmpty(room.signArrow) || (room.signArrow[0] != '→' && room.signArrow[0] != '←')) return room;
        var right = new Vector2(-outward.y, outward.x);
        var arrow = Vector2.Dot(target - wallPoint, right) >= 0f ? "→" : "←";
        if (room.signArrow.StartsWith(arrow, StringComparison.Ordinal)) return room;
        return new FrontRoomsShowcaseTable.Room
        {
            number = room.number, name = room.name, holds = room.holds, decide = room.decide,
            signNameLines = room.signNameLines, signKindsLines = room.signKindsLines, boardNameLines = room.boardNameLines,
            nameZh = room.nameZh, signFoot = room.signFoot, signArrow = arrow + room.signArrow.Substring(1)
        };
    }

    /// <summary>The arrow on room <paramref name="room"/>'s inside B as built ("← 1.1", SignToward), for the plan page's B card
    /// (.Plan.cs DrawRoomSign). With none built (the menu's plan, no museum) the arrow its place gives, which no map changes
    /// (MuseumInsideSign): by the hub corner .1's stop lies deeper, to the reader's left ("← n.1", rooms 1, 2, 4); past .1's
    /// window, where room 3's hangs, it lies back toward the hub, to the reader's right ("→ 3.1").</summary>
    string MuseumSignArrow(int room) =>
        room > 0 && room < museumSignArrows.Length && !string.IsNullOrEmpty(museumSignArrows[room]) ? museumSignArrows[room]
            : (FrontKind(room, 1) == EdgeKind.Window ? "→ " : "← ") + room + ".1";

    /// <summary>What a bay front shows along its wall (room-local y): a door with its frame kit, a window with its casing, an arch
    /// (and, 3.6, its card), an open front its whole width, else its kits, else the framed code. False for no bay.</summary>
    bool FrontSpan(PlacardScene sc, int slot, out float lo, out float hi)
    {
        lo = hi = 0f;
        int i, j;
        if (!BayOfSlot(slot, out i, out j)) return false;
        var mid = (j + .5f) * MapGrid.CellSize;
        var kind = FrontKind(sc.room, slot);
        var half = kind == EdgeKind.Door ? DoorExhibitHalf : kind == EdgeKind.Window ? WindowExhibitHalf : kind == EdgeKind.Arch ? ArchExhibitHalf
            : kind == EdgeKind.Open ? MapGrid.CellSize * .5f : FrameExhibitHalf;
        lo = mid - half;
        hi = mid + half;
        Bounds kits;
        if (kind != EdgeKind.Door && kind != EdgeKind.Window && kind != EdgeKind.Open && KitBox(sc, sc.room + "." + slot + " ", out kits))
        {
            lo = kind == EdgeKind.Arch ? Mathf.Min(lo, kits.min.z) : kits.min.z;
            hi = kind == EdgeKind.Arch ? Mathf.Max(hi, kits.max.z) : kits.max.z;
        }
        return true;
    }

    /// <summary>The hub's placards: B on each entry's hub side beside its arch (away from X), the room's number over the arch, A
    /// facing the spawn (its centre BoardFar from the spawn's eye, straight ahead: rooms 2 and 3 open over it, the four walks from
    /// the spawn to the entries pass beside it), its plan from the museum's own rules. Returns what was left out.</summary>
    string BuildHubPlacards(PlacardScene[] scenes, Transform parent)
    {
        var lost = "";
        var wh = ModuleUnits.WallHalf;
        var into = new Vector2(0f, -1f);
        for (var room = 1; room <= 4; room++)
        {
            var sc = scenes[room];
            if (sc == null)
            {
                lost += " " + room + " B, number";
                continue;
            }
            var x = sc.eCentre + sc.eHalf + SignGap + FrontRoomsShowcasePlacards.RoomSignWidth * .5f;
            var sign = FrontRoomsShowcasePlacards.OnWall(RoomWorld(room, new Vector2(x, -wh), 0f), RoomWorldDir(room, into), FrontRoomsShowcasePlacards.RoomSignCentre);
            // Its arrow toward E beside it, the arch its reader walks through to .1.
            var row = SignToward(RoomSign(room), new Vector2(x, -wh), into, new Vector2(sc.eCentre, -wh));
            if (FrontRoomsShowcasePlacards.BuildRoomSign(row, parent, sign) == null) lost += " " + room + " B";
            else museumHubSigns[room] = true;
            var number = FrontRoomsShowcasePlacards.OnWall(RoomWorld(room, new Vector2(sc.eCentre, -wh), 0f), RoomWorldDir(room, into), SignTagHeight);
            if (FrontRoomsShowcasePlacards.BuildEntryNumber(room, parent, number) == null) lost += " " + room + " number";
            else museumHubSigns[room] = true;
        }
        var drop = ModuleUnits.PlayerEye - (FrontRoomsShowcasePlacards.BoardTop - FrontRoomsShowcasePlacards.BoardHeight * .5f);
        var reach = Mathf.Sqrt(Mathf.Max(0f, FrontRoomsShowcasePlacards.BoardFar * FrontRoomsShowcasePlacards.BoardFar - drop * drop));
        var at = HubSpawn + new Vector2(0f, reach / MapGrid.CellSize);
        var board = FrontRoomsShowcasePlacards.Standing(MuseumWorld(at, 0f), MuseumWorldDir(new Vector2(0f, -1f)));
        if (FrontRoomsShowcasePlacards.BuildHubBoard(MuseumBoardPlan(), parent, board) == null) lost += " A";
        return lost;
    }

    /// <summary>Board A's plan from the museum's own layout rules (MuseumRoomOf, MuseumPartOf, MuseumEdge), never pasted: each room's
    /// cells and its alcove's, and every arch between two rooms; x = u, up = v = forward from the spawn. No alcove picture: none
    /// exists yet (the alcoves are not dressed; the builder draws one into its alcove's cells once a texture is passed here).</summary>
    static FrontRoomsShowcasePlacards.Plan MuseumBoardPlan()
    {
        var lo = new Vector2Int[5];
        var hi = new Vector2Int[5];
        var aLo = new Vector2Int[5];
        var aHi = new Vector2Int[5];
        var has = new bool[5];
        var hasAlcove = new bool[5];
        var gates = new List<FrontRoomsShowcasePlacards.PlanGate>();
        for (var v = 0; v < MuseumRows; v++)
        for (var u = 0; u < MuseumCols; u++)
        {
            int i, j;
            var room = MuseumRoomOf(u, v, out i, out j);
            if (room < 0) continue;
            var cell = new Vector2Int(u, v);
            lo[room] = has[room] ? Vector2Int.Min(lo[room], cell) : cell;
            hi[room] = has[room] ? Vector2Int.Max(hi[room], cell) : cell;
            has[room] = true;
            if (room > 0 && MuseumPartOf(i, j) == MuseumPart.Alcove)
            {
                aLo[room] = hasAlcove[room] ? Vector2Int.Min(aLo[room], cell) : cell;
                aHi[room] = hasAlcove[room] ? Vector2Int.Max(aHi[room], cell) : cell;
                hasAlcove[room] = true;
            }
            for (var k = 0; k < 2; k++)
            {
                var nu = u + (k == 0 ? 1 : 0);
                var nv = v + (k == 0 ? 0 : 1);
                int ni, nj;
                var other = MuseumRoomOf(nu, nv, out ni, out nj);
                if (other >= 0 && other != room && MuseumEdge(u, v, nu, nv) == EdgeKind.Arch)
                    gates.Add(new FrontRoomsShowcasePlacards.PlanGate(cell, new Vector2Int(nu, nv)));
            }
        }
        var rooms = new List<FrontRoomsShowcasePlacards.PlanRoom>();
        for (var room = 0; room <= 4; room++)
        {
            if (!has[room]) continue;
            rooms.Add(new FrontRoomsShowcasePlacards.PlanRoom
            {
                number = room,
                cells = new RectInt(lo[room].x, lo[room].y, hi[room].x - lo[room].x + 1, hi[room].y - lo[room].y + 1),
                alcove = hasAlcove[room] ? new RectInt(aLo[room].x, aLo[room].y, aHi[room].x - aLo[room].x + 1, aHi[room].y - aLo[room].y + 1) : default(RectInt),
                alcovePicture = null
            });
        }
        return new FrontRoomsShowcasePlacards.Plan { cols = MuseumCols, rows = MuseumRows, rooms = rooms.ToArray(), gates = gates.ToArray() };
    }

    /// <summary>
    /// The keep-outs of a room (the model's Scene): its floor, its kits (BuildMuseumExhibits' renderers), every door front's leaf
    /// sweep into the lane for each jamb the hinge can take (BuildEdge hangs an E door on the edge's start jamb, the south end of
    /// an east edge or the west end of a north edge; a body door on the other where FrontRoomsMapWorld.BodyDoorReverseHinge says
    /// so, and 1.1 switches the floor between the two), the arches (E and X on the hub wall where the map put them, 3.6's),
    /// every stop's area, E's and X's centres.
    /// </summary>
    PlacardScene MuseumPlacardScene(int room)
    {
        var cs = MapGrid.CellSize;
        var wh = ModuleUnits.WallHalf;
        var sc = new PlacardScene { room = room };
        sc.floor.Add(Rect.MinMaxRect(cs + wh, wh, 3f * cs - wh, 4f * cs - wh));
        for (var slot = 1; slot <= 7; slot++)
        {
            int i, j;
            if (FrontKind(room, slot) != EdgeKind.Open || !BayOfSlot(slot, out i, out j)) continue;
            var x0 = i == 3 ? 3f * cs - wh : wh;
            sc.floor.Add(Rect.MinMaxRect(x0, j * cs + wh, x0 + cs, (j + 1) * cs - wh));
        }
        MuseumKitBoxes(sc);
        for (var k = 0; k < sc.kits.Count; k++)
        {
            var b = sc.kits[k];
            if (b.min.y < FrontRoomsShowcasePlacards.StationTop + PlacardClear) sc.blocks.Add(RectPolygon(b.min.x, b.min.z, b.max.x, b.max.z));
        }
        for (var slot = 1; slot <= 7; slot++)
        {
            Vector2 first, second;
            bool both;
            if (FrontKind(room, slot) != EdgeKind.Door || !MuseumDoorJambs(room, slot, out first, out second, out both)) continue;
            float faceX, into;
            FrontFace(slot, out faceX, out into);
            var open = new Vector2(into, 0f);
            sc.swings.Add(SweepPolygon(first, (second - first).normalized, open));
            if (both) sc.swings.Add(SweepPolygon(second, (first - second).normalized, open));
        }
        MuseumArch(room, 2, out sc.eCentre, out sc.eHalf);
        MuseumArch(room, 1, out sc.xCentre, out sc.xHalf);
        sc.passages.Add(RectPolygon(sc.eCentre - sc.eHalf, 0f, sc.eCentre + sc.eHalf, wh + ArchPassage));
        sc.passages.Add(RectPolygon(sc.xCentre - sc.xHalf, 0f, sc.xCentre + sc.xHalf, wh + ArchPassage));
        for (var slot = 1; slot <= 7; slot++)
        {
            int i, j;
            if (FrontKind(room, slot) != EdgeKind.Arch || !BayOfSlot(slot, out i, out j)) continue;
            float faceX, into;
            FrontFace(slot, out faceX, out into);
            var wallLine = faceX * cs - into * wh;
            var reach = faceX * cs + into * ArchPassage;
            var mid = (j + .5f) * cs;
            sc.passages.Add(RectPolygon(Mathf.Min(wallLine, reach), mid - ArchExhibitHalf, Mathf.Max(wallLine, reach), mid + ArchExhibitHalf));
        }
        for (var slot = 1; slot <= 7; slot++)
        {
            Vector2 stand, face;
            if (!PlacardStop(room, slot, out stand, out face)) continue;
            sc.stops.Add(Obb(stand, new Vector2(face.y, -face.x), face, StopAlong * .5f, StopDeep * .5f));
        }
        return sc;
    }

    /// <summary>A room's arch on its hub wall (lane cell i 2: E, 1: X) as room-local x of its centre (where the map put it,
    /// CrossingPoint) and its half span (the carve's for an authored arch, else the widest ArchOpening gives at that centre).</summary>
    void MuseumArch(int room, int i, out float centre, out float half)
    {
        var cs = MapGrid.CellSize;
        int u, v;
        LocalToMuseum(room, i, 0, out u, out v);
        var outward = LocalDir(room, 0f, -1f);
        var lane = MuseumCell(u, v);
        var hub = MuseumCell(u + Mathf.RoundToInt(outward.x), v + Mathf.RoundToInt(outward.y));
        centre = RoomLocal(room, map.CrossingPoint(lane, hub)).x;
        float width, along;
        if (map.Cache != null && map.Cache.ArchOverride(lane, hub, out width, out along))
        {
            half = width * .5f;
            return;
        }
        var inCell = centre - i * cs;
        half = Mathf.Min(ModuleUnits.ArchMaxWidth * .5f, Mathf.Min(inCell - ModuleUnits.ArchCornerMargin, cs - ModuleUnits.ArchCornerMargin - inCell));
        half = Mathf.Max(half, ModuleUnits.ArchMinWidth * .5f);
    }

    /// <summary>A door front's two jambs (room-local) as BuildEdge hangs its leaf: first the E door's hinge (the edge's start
    /// jamb), second the other; both: a body door takes the second (FrontRoomsMapWorld.BodyDoorReverseHinge).</summary>
    bool MuseumDoorJambs(int room, int slot, out Vector2 first, out Vector2 second, out bool both)
    {
        first = second = Vector2.zero;
        both = false;
        GridCoord bay, lane;
        if (!MuseumFront(room, slot, out bay, out lane)) return false;
        var lo = bay;
        var hi = lane;
        if (hi.x < lo.x || hi.y < lo.y)
        {
            lo = lane;
            hi = bay;
        }
        var cs = MapGrid.CellSize;
        var east = hi.x > lo.x;
        // The edge in the map's cell frame (metres): an east edge starts at its south end and runs north, a north edge starts at
        // its west end and runs east (FrontRoomsMapWorld's BuildEdge calls); the door is centred on it.
        var start = east ? new Vector2((lo.x + 1) * cs, lo.y * cs) : new Vector2(lo.x * cs, (lo.y + 1) * cs);
        var along = east ? new Vector2(0f, 1f) : new Vector2(1f, 0f);
        var half = ModuleUnits.DoorWidth * .5f;
        first = MapToRoom(room, start + along * (cs * .5f - half));
        second = MapToRoom(room, start + along * (cs * .5f + half));
        both = FrontRoomsMapWorld.BodyDoorReverseHinge(lo, hi);
        return true;
    }

    /// <summary>A leaf's sweep into the lane: from its hinge, from shut (toward the other jamb) through SwingDegrees, SwingReach
    /// out, in 6-degree steps (convex: under 180 degrees).</summary>
    static Vector2[] SweepPolygon(Vector2 hinge, Vector2 shut, Vector2 open)
    {
        var steps = Mathf.CeilToInt(SwingDegrees / 6f);
        var polygon = new Vector2[steps + 2];
        polygon[0] = hinge;
        for (var k = 0; k <= steps; k++)
        {
            var a = Mathf.Min(k * 6f, SwingDegrees) * Mathf.Deg2Rad;
            polygon[k + 1] = hinge + SwingReach * (Mathf.Cos(a) * shut + Mathf.Sin(a) * open);
        }
        return polygon;
    }

    /// <summary>The room's exhibit objects' renderers as room-local boxes (x, height, y), each with its object's label.</summary>
    void MuseumKitBoxes(PlacardScene sc)
    {
        if (museumExhibits == null) return;
        var prefix = sc.room + ".";
        var root = museumExhibits.transform;
        for (var k = 0; k < root.childCount; k++)
        {
            var child = root.GetChild(k);
            if (!child.name.StartsWith(prefix, StringComparison.Ordinal)) continue;
            foreach (var r in child.GetComponentsInChildren<Renderer>(true))
            {
                var b = r.bounds;
                var lo = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                var hi = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                for (var c = 0; c < 8; c++)
                {
                    var p = RoomLocal(sc.room, new Vector3((c & 1) == 0 ? b.min.x : b.max.x, (c & 2) == 0 ? b.min.y : b.max.y, (c & 4) == 0 ? b.min.z : b.max.z));
                    lo = Vector3.Min(lo, p);
                    hi = Vector3.Max(hi, p);
                }
                var box = new Bounds();
                box.SetMinMax(lo, hi);
                sc.kits.Add(box);
                sc.kitNames.Add(child.name);
            }
        }
    }

    /// <summary>
    /// C's place at a station (10_research §3.4, 20 §2 C): on the stop's right, its face's centre StationStandOff (flat) from the
    /// stop's eye and facing it (the builder tilts it), so the eye is StationNear from it. The bearing, degrees right of the
    /// stop's facing, is the first from 0 at which its stand is out of every stop area, its plan PlacardClear off walls, kits,
    /// tags, the placards set, every leaf's sweep and every arch, and its face hides no point of the exhibit from the stop's eye.
    /// None (a crowded station): the bearing that fails the fewest of those, then the smallest; fails names them. Its plan joins
    /// the room's blocks. The walking line from stop to stop is no check (none can hold it here: NOTES); the model measures it.
    /// </summary>
    bool MuseumPlaceStation(PlacardScene sc, int slot, List<Vector3> samples, out Vector2 floorPoint, out Vector2 toward, out int bearing, out string fails)
    {
        floorPoint = toward = Vector2.zero;
        bearing = 0;
        fails = null;
        Vector2 stand, face;
        if (!PlacardStop(sc.room, slot, out stand, out face)) return false;
        var eye = new Vector3(stand.x, ModuleUnits.PlayerEye, stand.y);
        samples.Clear();
        MuseumExhibitSamples(sc, slot, eye, samples);
        var bestCount = int.MaxValue;
        var bestMask = 0;
        for (var b = 0; b <= PlacardBearingMax; b++)
        {
            var mask = PlacardChecks(sc, stand, face, b, eye, samples, bestCount);
            if (mask == 0)
            {
                bearing = b;
                bestMask = 0;
                bestCount = 0;
                break;
            }
            var count = Bits(mask);
            if (count >= bestCount) continue;
            bestCount = count;
            bestMask = mask;
            bearing = b;
        }
        Vector2 at, d, w;
        PlacardAxes(stand, face, bearing, out at, out d, out w);
        floorPoint = at;
        toward = -d;
        sc.blocks.Add(PlacardFootprint(at, d));
        if (bestMask != 0) fails = CheckNames(bestMask);
        return true;
    }

    // The checks of one bearing, as bits (wall 1, kit 2, sweep 4, arch 8, stop area 16, view 32); the view, the costly one, is
    // skipped once the others already fail as many as the best so far (it could only add to them).
    int PlacardChecks(PlacardScene sc, Vector2 stand, Vector2 face, int bearing, Vector3 eye, List<Vector3> samples, int bestCount)
    {
        Vector2 at, d, w;
        PlacardAxes(stand, face, bearing, out at, out d, out w);
        var plan = PlacardFootprint(at, d);
        var mask = 0;
        if (!OnFloor(sc, plan)) mask |= 1;
        if (!KeptClear(plan, sc.blocks, PlacardClear)) mask |= 2;
        if (!KeptClear(plan, sc.swings, PlacardClear)) mask |= 4;
        if (!KeptClear(plan, sc.passages, PlacardClear)) mask |= 8;
        // The floor plate stands under the bracket, behind the face's centre along d.
        var behind = FrontRoomsShowcasePlacards.StationBracketBack * Mathf.Cos(FrontRoomsShowcasePlacards.StationTilt * Mathf.Deg2Rad);
        var post = Obb(at + d * behind, w, d, PlacardStand, PlacardStand);
        for (var k = 0; k < sc.stops.Count; k++)
            if (PolygonDistance(post, sc.stops[k]) <= 0f)
            {
                mask |= 16;
                break;
            }
        if (mask != 0 && Bits(mask) >= bestCount) return mask;
        if (Hides(eye, at, d, w, samples)) mask |= 32;
        return mask;
    }

    static int Bits(int mask)
    {
        var n = 0;
        for (; mask != 0; mask &= mask - 1) n++;
        return n;
    }

    static string CheckNames(int mask)
    {
        var s = "";
        if ((mask & 1) != 0) s += " a wall nearer than 5 cm";
        if ((mask & 2) != 0) s += " a kit or tag nearer than 5 cm";
        if ((mask & 4) != 0) s += " in a leaf's sweep";
        if ((mask & 8) != 0) s += " at an arch";
        if ((mask & 16) != 0) s += " its stand in a stop area";
        if ((mask & 32) != 0) s += " hiding part of its exhibit from the stop";
        return s.Trim();
    }

    /// <summary>C at a bearing from a stop (room-local): its floor point (under its face's centre), d (stop to it, flat, unit),
    /// w (its width's way, flat).</summary>
    static void PlacardAxes(Vector2 stand, Vector2 face, int bearing, out Vector2 at, out Vector2 d, out Vector2 w)
    {
        var right = new Vector2(face.y, -face.x);
        var t = bearing * Mathf.Deg2Rad;
        d = face * Mathf.Cos(t) + right * Mathf.Sin(t);
        at = stand + d * FrontRoomsShowcasePlacards.StationStandOff;
        w = new Vector2(-d.y, d.x);
    }

    /// <summary>C in plan: StationWidth (and PlacardSlack) across, its tilted face's depth (and PlacardBack) along d.</summary>
    static Vector2[] PlacardFootprint(Vector2 at, Vector2 d)
    {
        var w = new Vector2(-d.y, d.x);
        var halfWide = FrontRoomsShowcasePlacards.StationWidth * .5f + PlacardSlack;
        var halfDeep = FrontRoomsShowcasePlacards.StationHeight * .5f * Mathf.Sin(FrontRoomsShowcasePlacards.StationTilt * Mathf.Deg2Rad) + PlacardBack;
        return Obb(at, w, d, halfWide, halfDeep);
    }

    /// <summary>Does C's face (grown by PlacardViewPad) stand between the eye and any exhibit point? Its face as the builder
    /// makes it: top StationTop, StationHeight tilted StationTilt back from the reader, StationWidth across.</summary>
    static bool Hides(Vector3 eye, Vector2 at, Vector2 d, Vector2 w, List<Vector3> samples)
    {
        var tilt = FrontRoomsShowcasePlacards.StationTilt * Mathf.Deg2Rad;
        var h = FrontRoomsShowcasePlacards.StationHeight;
        var centre = new Vector3(at.x, FrontRoomsShowcasePlacards.StationTop - Mathf.Cos(tilt) * h * .5f, at.y);
        var normal = new Vector3(-d.x * Mathf.Cos(tilt), Mathf.Sin(tilt), -d.y * Mathf.Cos(tilt));
        var up = new Vector3(d.x * Mathf.Sin(tilt), Mathf.Cos(tilt), d.y * Mathf.Sin(tilt));
        var across = new Vector3(w.x, 0f, w.y);
        var halfWide = FrontRoomsShowcasePlacards.StationWidth * .5f + PlacardViewPad;
        var halfHigh = h * .5f + PlacardViewPad;
        var toFace = Vector3.Dot(centre - eye, normal);
        for (var k = 0; k < samples.Count; k++)
        {
            var ray = samples[k] - eye;
            var den = Vector3.Dot(ray, normal);
            if (Mathf.Abs(den) < 1e-9f) continue;
            var t = toFace / den;
            if (t <= 1e-6f || t >= 1f - 1e-6f) continue;
            var rel = eye + ray * t - centre;
            if (Mathf.Abs(Vector3.Dot(rel, across)) <= halfWide && Mathf.Abs(Vector3.Dot(rel, up)) <= halfHigh) return true;
        }
        return false;
    }

    /// <summary>
    /// What C must leave in view from the stop: the alcove's walls (and 3.4's window); a door with its frame kit; a window with
    /// its casing; 3.6's arch with its card and detector; 2.2's key on the bay floor; the station's kits but the ones that only
    /// carry it (PlacardSupports); with none, the framed code's place. Points on the faces the eye sees, ExhibitStep apart.
    /// </summary>
    void MuseumExhibitSamples(PlacardScene sc, int slot, Vector3 eye, List<Vector3> samples)
    {
        var cs = MapGrid.CellSize;
        var wh = ModuleUnits.WallHalf;
        var room = sc.room;
        if (slot == 4)
        {
            var top = ModuleUnits.StandardCeiling;
            AddBoxSamples(samples, eye, PlanBox(cs + wh, 4f * cs - wh - .02f, 0f, 3f * cs - wh, 4f * cs - wh, top), AlcoveStep);
            AddBoxSamples(samples, eye, PlanBox(cs + wh, 3f * cs, 0f, cs + wh + .02f, 4f * cs - wh, top), AlcoveStep);
            AddBoxSamples(samples, eye, PlanBox(3f * cs - wh - .02f, 3f * cs, 0f, 3f * cs - wh, 4f * cs - wh, top), AlcoveStep);
            if (room == 3)
            {
                var window = 3.5f * cs;
                AddBoxSamples(samples, eye, PlanBox(3f * cs - wh - WindowExhibitProud, window - WindowExhibitHalf, WindowExhibitSill, 3f * cs - wh, window + WindowExhibitHalf, WindowExhibitHead), ExhibitStep);
            }
            return;
        }
        int i, j;
        if (!BayOfSlot(slot, out i, out j)) return;
        float faceX, into;
        FrontFace(slot, out faceX, out into);
        faceX *= cs;
        var mid = (j + .5f) * cs;
        var kind = FrontKind(room, slot);
        if (kind == EdgeKind.Door) AddBoxSamples(samples, eye, WallBox(faceX, into, mid, DoorExhibitHalf, 0f, DoorExhibitTop, DoorExhibitProud), ExhibitStep);
        else if (kind == EdgeKind.Window) AddBoxSamples(samples, eye, WallBox(faceX, into, mid, WindowExhibitHalf, WindowExhibitSill, WindowExhibitHead, WindowExhibitProud), ExhibitStep);
        else if (kind == EdgeKind.Open)
        {
            // 2.2's key where it lies (the bay's middle when taken or not laid).
            var key = MuseumKeyLying();
            var k = key != null ? RoomLocal(room, key.position) : new Vector3(3.5f * cs, 0f, 1.5f * cs);
            AddBoxSamples(samples, eye, PlanBox(k.x - .2f, k.z - .2f, 0f, k.x + .2f, k.z + .2f, .05f), .05f);
        }
        else
        {
            if (kind == EdgeKind.Arch) AddBoxSamples(samples, eye, WallBox(faceX, into, mid, ArchExhibitHalf, 0f, ArchExhibitTop, .02f), ExhibitStep);
            var code = room + "." + slot + " ";
            var any = false;
            for (var k = 0; k < sc.kits.Count; k++)
            {
                if (!sc.kitNames[k].StartsWith(code, StringComparison.Ordinal) || IsSupport(sc.kitNames[k])) continue;
                AddBoxSamples(samples, eye, sc.kits[k], ExhibitStep);
                any = true;
            }
            if (!any && kind == EdgeKind.Wall)
            {
                var spot = MuseumSpotOf(room, slot);
                AddBoxSamples(samples, eye, WallBox(faceX, into, mid, FrameExhibitHalf, spot.height - FrameExhibitRise, spot.height + FrameExhibitRise, .01f), ExhibitStep);
            }
        }
    }

    static bool IsSupport(string label)
    {
        for (var k = 0; k < PlacardSupports.Length; k++)
            if (label.StartsWith(PlacardSupports[k], StringComparison.Ordinal)) return true;
        return false;
    }

    // A box on a bay front's lane face: half along the wall about mid, z0-z1 high, proud into the lane.
    static Bounds WallBox(float faceX, float into, float mid, float half, float z0, float z1, float proud) =>
        PlanBox(Mathf.Min(faceX, faceX + into * proud), mid - half, z0, Mathf.Max(faceX, faceX + into * proud), mid + half, z1);

    // A room-local box from plan x, plan y, height ranges (Bounds: x, height, y).
    static Bounds PlanBox(float x0, float y0, float h0, float x1, float y1, float h1)
    {
        var b = new Bounds();
        b.SetMinMax(new Vector3(x0, h0, y0), new Vector3(x1, h1, y1));
        return b;
    }

    /// <summary>Points on the faces of a box that face the eye, on a grid no coarser than step (at least 2 a side).</summary>
    static void AddBoxSamples(List<Vector3> samples, Vector3 eye, Bounds box, float step)
    {
        var lo = box.min;
        var hi = box.max;
        if (eye.x < lo.x) FaceSamples(samples, step, 0, lo.x, lo.z, hi.z, lo.y, hi.y);
        if (eye.x > hi.x) FaceSamples(samples, step, 0, hi.x, lo.z, hi.z, lo.y, hi.y);
        if (eye.z < lo.z) FaceSamples(samples, step, 2, lo.z, lo.x, hi.x, lo.y, hi.y);
        if (eye.z > hi.z) FaceSamples(samples, step, 2, hi.z, lo.x, hi.x, lo.y, hi.y);
        if (eye.y > hi.y) FaceSamples(samples, step, 1, hi.y, lo.x, hi.x, lo.z, hi.z);
        if (eye.y < lo.y) FaceSamples(samples, step, 1, lo.y, lo.x, hi.x, lo.z, hi.z);
    }

    // One face: axis 0 (x = at; a along plan y, b along height), 2 (plan y = at; a along x, b along height), 1 (height = at; a
    // along x, b along plan y).
    static void FaceSamples(List<Vector3> samples, float step, int axis, float at, float a0, float a1, float b0, float b1)
    {
        var na = Mathf.Max(2, Mathf.CeilToInt((a1 - a0) / step) + 1);
        var nb = Mathf.Max(2, Mathf.CeilToInt((b1 - b0) / step) + 1);
        for (var p = 0; p < na; p++)
        for (var q = 0; q < nb; q++)
        {
            var a = a0 + (a1 - a0) * p / (na - 1);
            var b = b0 + (b1 - b0) * q / (nb - 1);
            samples.Add(axis == 0 ? new Vector3(at, b, a) : axis == 2 ? new Vector3(a, b, at) : new Vector3(a, at, b));
        }
    }

    // Every point along a plan's edges, 5 cm apart, on the floor (each floor rectangle shrunk by PlacardClear).
    static bool OnFloor(PlacardScene sc, Vector2[] plan)
    {
        for (var k = 0; k < plan.Length; k++)
        {
            var a = plan[k];
            var b = plan[(k + 1) % plan.Length];
            var n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / .05f));
            for (var t = 0; t < n; t++)
            {
                var p = Vector2.Lerp(a, b, (float)t / n);
                var inside = false;
                for (var r = 0; r < sc.floor.Count && !inside; r++)
                {
                    var f = sc.floor[r];
                    inside = p.x >= f.xMin + PlacardClear && p.x <= f.xMax - PlacardClear && p.y >= f.yMin + PlacardClear && p.y <= f.yMax - PlacardClear;
                }
                if (!inside) return false;
            }
        }
        return true;
    }

    static bool KeptClear(Vector2[] plan, List<Vector2[]> others, float gap)
    {
        for (var k = 0; k < others.Count; k++)
            if (PolygonDistance(plan, others[k]) < gap) return false;
        return true;
    }

    static Vector2[] Obb(Vector2 c, Vector2 ax, Vector2 ay, float hx, float hy) =>
        new[] { c - hx * ax - hy * ay, c + hx * ax - hy * ay, c + hx * ax + hy * ay, c - hx * ax + hy * ay };

    static Vector2[] RectPolygon(float x0, float y0, float x1, float y1) =>
        new[] { new Vector2(x0, y0), new Vector2(x1, y0), new Vector2(x1, y1), new Vector2(x0, y1) };

    static float Cross(Vector2 o, Vector2 a, Vector2 b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);

    static bool InsideConvex(Vector2 p, Vector2[] polygon)
    {
        var sign = 0;
        for (var k = 0; k < polygon.Length; k++)
        {
            var c = Cross(polygon[k], polygon[(k + 1) % polygon.Length], p);
            if (Mathf.Abs(c) < 1e-12f) continue;
            var s = c > 0f ? 1 : -1;
            if (sign == 0) sign = s;
            else if (s != sign) return false;
        }
        return true;
    }

    static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var l2 = ab.sqrMagnitude;
        var t = l2 < 1e-12f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / l2);
        return Vector2.Distance(a + ab * t, p);
    }

    static bool SegmentsCross(Vector2 a, Vector2 b, Vector2 c, Vector2 d) =>
        (Cross(c, d, a) > 0f) != (Cross(c, d, b) > 0f) && (Cross(a, b, c) > 0f) != (Cross(a, b, d) > 0f);

    /// <summary>The distance between two convex polygons (a two-point one is a segment); 0 when they touch or overlap.</summary>
    static float PolygonDistance(Vector2[] a, Vector2[] b)
    {
        if (b.Length > 2)
            for (var k = 0; k < a.Length; k++)
                if (InsideConvex(a[k], b)) return 0f;
        if (a.Length > 2)
            for (var k = 0; k < b.Length; k++)
                if (InsideConvex(b[k], a)) return 0f;
        var ea = a.Length > 2 ? a.Length : 1;
        var eb = b.Length > 2 ? b.Length : 1;
        for (var p = 0; p < ea; p++)
        for (var q = 0; q < eb; q++)
            if (SegmentsCross(a[p], a[(p + 1) % a.Length], b[q], b[(q + 1) % b.Length])) return 0f;
        var best = float.MaxValue;
        for (var p = 0; p < a.Length; p++)
        for (var q = 0; q < eb; q++)
            best = Mathf.Min(best, SegmentDistance(a[p], b[q], b[(q + 1) % b.Length]));
        for (var q = 0; q < b.Length; q++)
        for (var p = 0; p < ea; p++)
            best = Mathf.Min(best, SegmentDistance(b[q], a[p], a[(p + 1) % a.Length]));
        return best;
    }

    // ---------- room-local metres <-> the world

    /// <summary>A station's stop in room-local metres and its facing (MuseumSpotOf's, back from museum cells).</summary>
    static bool PlacardStop(int room, int slot, out Vector2 stand, out Vector2 face)
    {
        var spot = MuseumSpotOf(room, slot);
        stand = MuseumToLocal(room, spot.stand.x, spot.stand.y) * MapGrid.CellSize;
        face = MuseumToLocalDir(room, spot.face).normalized;
        return spot.ok;
    }

    /// <summary>Museum cells to room cells (the inverse of LocalPoint).</summary>
    static Vector2 MuseumToLocal(int room, float u, float v)
    {
        if (room == 1) return new Vector2(v, 4f - u);
        if (room == 2 || room == 3) return new Vector2(u - (room == 2 ? 4f : 8f), v - 4f);
        return new Vector2(4f - v, u - 12f);
    }

    /// <summary>A museum direction in the room's frame (the inverse of LocalDir).</summary>
    static Vector2 MuseumToLocalDir(int room, Vector2 d)
    {
        if (room == 1) return new Vector2(d.y, -d.x);
        if (room == 2 || room == 3) return d;
        return new Vector2(-d.y, d.x);
    }

    Vector3 RoomWorld(int room, Vector2 p, float height) => MuseumWorld(LocalPoint(room, p.x / MapGrid.CellSize, p.y / MapGrid.CellSize), height);

    Vector3 RoomWorldDir(int room, Vector2 d) => MuseumWorldDir(LocalDir(room, d.x, d.y)).normalized;

    /// <summary>A world point in room-local metres (x, height, y).</summary>
    Vector3 RoomLocal(int room, Vector3 world)
    {
        var p = map.transform.InverseTransformPoint(world);
        var cs = MapGrid.CellSize;
        var l = MuseumToLocal(room, p.x / cs - museumCell0.x, p.z / cs - museumCell0.y) * cs;
        return new Vector3(l.x, p.y, l.y);
    }

    // A point of the map's cell frame (metres, x and z) in room-local metres.
    Vector2 MapToRoom(int room, Vector2 p)
    {
        var cs = MapGrid.CellSize;
        return MuseumToLocal(room, p.x / cs - museumCell0.x, p.y / cs - museumCell0.y) * cs;
    }

    // ---------- the placards while the museum stands

    /// <summary>
    /// Every frame while the museum stands, after the museum's keys (a jump this frame shows its room's placards in this frame):
    /// a room's inside placards on only while the player stands in it or in the hub cell at one of its arches (the hub's always:
    /// fewer canvases and draws on an iPhone), 1.1's and 1.7's variant as the door shows it; every MuseumLiveSeconds the live
    /// state on the foot of each C in the player's room. Nothing here allocates (the words are constants or kept from the build).
    /// </summary>
    void TickMuseumPlacards()
    {
        if (museumPlacards == null) return;
        var mask = MuseumPlacardMask();
        if (mask != museumPlacardMask) ShowMuseumPlacardRooms(mask);
        if (museumMember != museumPlacardMember || (map == null || map.BodyDoors) != museumPlacardBody) SyncMuseumPlacardVariants();
        museumLiveClock -= Time.unscaledDeltaTime;
        if (museumLiveClock > 0f) return;
        museumLiveClock = MuseumLiveSeconds;
        var room = MuseumRoomHere();
        if (room < 1 || room > 4) return;
        for (var slot = 1; slot <= 7; slot++)
        {
            var b = room == 1 && slot == 1 && !museumPlacardBody;
            var placard = b ? museumPlacardC11B : museumPlacardC[room * 8 + slot];
            if (placard != null) FrontRoomsShowcasePlacards.SetLiveState(placard, MuseumLiveWord(room, slot, b ? museumPlacardFrame11B : museumPlacardFrame[room * 8 + slot]));
        }
    }

    // The rooms whose inside placards show: the one the player stands in, or the ones whose E or X hub cell the player is in.
    int MuseumPlacardMask()
    {
        if (map == null || player == null) return 0;
        var c = map.CellOf(player.position);
        var u = c.x - museumCell0.x;
        var v = c.y - museumCell0.y;
        int i, j;
        var room = MuseumRoomOf(u, v, out i, out j);
        if (room > 0) return 1 << room;
        if (room < 0) return 0;
        var mask = 0;
        for (var k = 0; k < museumEntryCells.Length; k++)
            if (museumEntryCells[k].x == u && museumEntryCells[k].y == v) mask |= 1 << (1 + k / 2);
        return mask;
    }

    // A room's canvases and renderers on or off (Canvas.enabled keeps every line's mesh: no text is built again when it shows).
    void ShowMuseumPlacardRooms(int mask)
    {
        museumPlacardMask = mask;
        for (var r = 1; r <= 4; r++)
        {
            var on = (mask & (1 << r)) != 0;
            var canvases = museumPlacardCanvases[r];
            for (var k = 0; k < canvases.Count; k++)
                if (canvases[k] != null && canvases[k].enabled != on) canvases[k].enabled = on;
            var renderers = museumPlacardRenderers[r];
            for (var k = 0; k < renderers.Count; k++)
                if (renderers[k] != null && renderers[k].enabled != on) renderers[k].enabled = on;
        }
    }

    /// <summary>1.1's placard is the variant on the door (A while the floor's doors are body doors, as MuseumRow reads it), 1.7's
    /// floor tag the member on its door; the other one(s) stand switched off at the same place.</summary>
    void SyncMuseumPlacardVariants()
    {
        museumPlacardBody = map == null || map.BodyDoors;
        museumPlacardMember = museumMember;
        if (museumPlacards == null) return;
        var a = museumPlacardC[1 * 8 + 1];
        if (a != null && a.activeSelf != museumPlacardBody) a.SetActive(museumPlacardBody);
        if (museumPlacardC11B != null && museumPlacardC11B.activeSelf == museumPlacardBody) museumPlacardC11B.SetActive(!museumPlacardBody);
        for (var k = 0; k < museumMemberTags.Length; k++)
        {
            var tag = museumMemberTags[k];
            if (tag != null && tag.activeSelf != (k == museumMember)) tag.SetActive(k == museumMember);
        }
    }

    /// <summary>
    /// The live state on C's foot (20 §2 C: right-aligned, accent, up to 10 characters): the station's state in the words the
    /// museum's list uses (MuseumRowTag, MuseumKeysTag, MuseumWindowTag, MuseumAlarmTag; a door SHUT, OPEN or BROKEN), or for a
    /// frame where it is tried today (<paramref name="frame"/>, MuseumFrameWord of the placard's row, kept from the build). A
    /// constant or kept string each: nothing is built per call.
    /// </summary>
    string MuseumLiveWord(int room, int slot, string frame)
    {
        if (slot == 4) return room == 3 && MuseumLive(3, 4) ? MuseumWindowTag(4) : "GAME LIGHT";
        if (room == 3 && slot == 7) return "FREE";
        if (!MuseumLive(room, slot))
        {
            if (room == 3 && slot == 5 && MuseumDoorAt(3, 5) != null) return "PLAIN DOOR";
            return frame ?? "FRAME";
        }
        switch (room)
        {
            case 1:
            {
                var door = MuseumDoor(slot);
                return door == null ? "FRAME" : door.broken ? "BROKEN" : door.open ? "OPEN" : "SHUT";
            }
            case 2:
                if (slot == 2) return map.HasKeyFor(museumKeyZone) ? "TAKEN" : "ON FLOOR";
                if (slot == 5) return "LIVE";
                var keys = MuseumKeysTag(slot);
                return keys == "KEY IN HAND" ? "KEY HELD" : keys;
            case 3:
                return slot == 5 ? MuseumAlarmTag() : slot == 6 ? "LIVE" : MuseumWindowTag(slot);
            case 4:
                return slot == 7 && museumDev >= 0 && museumDev < DevCount ? DevLetters[museumDev] : "LIVE";
        }
        return "";
    }

    /// <summary>A frame's live word on C's foot (MuseumLiveWord): where the station is tried today, by its row's tour.</summary>
    static string MuseumFrameWord(FrontRoomsShowcaseTable.Station st)
    {
        var tour = st != null ? st.tour : 0;
        return tour == 2 ? "TOUR 2" : tour == 3 ? "TOUR 3" : tour == 4 ? "TOUR 4" : tour == 1 ? "MEDIA BAY" : "FRAME";
    }

    // ---------- per frame

    /// <summary>The museum's frame: the title's start, the build, the framed codes; its keys (P the plan in every state); then the
    /// placards, after the keys, so a jump this frame shows its room's placards in this frame's render.</summary>
    void UpdateMuseum(Keyboard keys)
    {
        if (map == null) UpdateMuseumTitle();
        else if (museumState == MuseumState.Carved) BuildMuseum();
        else if (museumState == MuseumState.Built) TickMuseum();
        UpdateMuseumKeys(keys);
        if (museumState == MuseumState.Built) TickMuseumPlacards();
    }

    void UpdateMuseumKeys(Keyboard keys)
    {
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
    /// its line of sight from the eye, but where a world placard shows the same code (a station's C, a room's sign B or number on
    /// the hub side): so the hub's 0.1 and 0.2 keep theirs, and any station or sign whose placard was left out. And the station
    /// whose stop the player stands at.</summary>
    void RefreshMuseumTags()
    {
        museumTags.Clear();
        museumNearRoom = museumNearSlot = -1;
        var room = MuseumRoomHere();
        if (room < 0) return;
        var used = 0;
        if (room == 0)
        {
            for (var r = 1; r <= 4; r++)
                if (!museumHubSigns[r]) AddMuseumTag(ref used, r, 0, MuseumSignSpot(r));
            AddMuseumTag(ref used, 0, 1, MuseumSpotOf(0, 1));
            AddMuseumTag(ref used, 0, 2, MuseumSpotOf(0, 2));
        }
        else
            for (var slot = 1; slot <= 7; slot++)
                if (!MuseumPlacardShown(room, slot)) AddMuseumTag(ref used, room, slot, MuseumSpotOf(room, slot));
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

    // A station of rooms 1-4 whose C stands (1.1: the variant on the door).
    bool MuseumPlacardShown(int room, int slot)
    {
        if (room < 1 || room > 4 || slot < 1 || slot > 7) return false;
        return (room == 1 && slot == 1 && !museumPlacardBody ? museumPlacardC11B : museumPlacardC[room * 8 + slot]) != null;
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

    /// <summary>A row the plan shows as HERE (ENTER walks to it), once the museum stands: every row of rooms 1, 3 and 4 but 4.2,
    /// whose live close-up needs a station (its ENTER is tour 2's), and of room 2 the alcove 2.4 and the stations it runs now
    /// (MuseumLive: 2.1-2.3 while its locks and key stand, 2.5 while its tag does). A room-2 frame (2.6, 2.7, and 2.1-2.3 or
    /// 2.5 when off) goes to its tour, as its placard says (P, then ENTER: tour 4 or 2), never back to the same frame; the
    /// hub's rows go to their tours.</summary>
    bool MuseumBuiltHere(FrontRoomsShowcaseTable.Station st)
    {
        if (tour != Tour.Museum || museumState != MuseumState.Built || st == null) return false;
        var room = FrontRoomsShowcaseTable.RoomOf(st);
        var slot = FrontRoomsShowcaseTable.SlotOf(st);
        return room == 1 || room == 3 || (room == 4 && slot != 2) || (room == 2 && slot >= 1 && slot <= 5 && MuseumLive(2, slot));
    }

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
        // Again at the same stop: its next variant (1.1 A/B, 1.7 A-D, 4.7 A-D).
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
        // 4.7's row is 2.6 m long: its four close-up stops count as its stop.
        var near = room == 4 && slot == 7 ? 2.6f : StopNear;
        return spot.ok && player != null && Vector3.Distance(Flat(Feet), Flat(MuseumWorld(spot.stand, 0f))) < near;
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
        MuseumMove(spot.stand, MuseumLookAt(room, slot, spot));
        museumMarkRoom = room;
        museumMarkSlot = slot;
        // 4.7: a walk to the row starts its A-D stepping again.
        if (room == 4 && slot == 7) museumDev = -1;
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
        Toast(title, MuseumRoomLine(room));
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
        else if (room == 4 && slot == 7) MuseumNextDev();
        row = MuseumRow(room, slot);
        museumMarkUntil = Time.unscaledTime + MuseumCardSeconds;
        Toast(row != null ? LabelOf(row) : "", MuseumHere(room, slot, row));
    }

    /// <summary>1.1 A/B: the map's own switch (body doors are one rule per floor), then both museum chunks built again (room 1's
    /// doors and 3.5's), since a door's hinge side is chosen when it is built. A latch pried from under the plan is let go first.</summary>
    void MuseumSwitchDoors()
    {
        var toBody = !map.BodyDoors;
        if (toBody && !FrontRooms3DGame.BodyDoorsCompiled) return;
        try
        {
            MuseumLetGoOfLatches();
            map.BodyDoors = toBody;
            map.RebuildChunk(museumChunk);
            map.RebuildChunk(new GridCoord(museumChunk.x + 1, museumChunk.y));
            Physics.SyncTransforms();
        }
        catch (Exception e)
        {
            MuseumNote("1.1's switch failed (" + e.GetType().Name + ": " + e.Message + ").");
        }
        museumTagClock = 0f;
        // 1.1's placard is the variant the door is now.
        SyncMuseumPlacardVariants();
    }

    /// <summary>4.7: in front of the row's next close-up (A desk phone, B wall clock, C vending machine, D copier), facing its face.</summary>
    void MuseumNextDev()
    {
        for (var n = 0; n < DevCount; n++)
        {
            museumDev = (museumDev + 1) % DevCount;
            if (!museumDevOk[museumDev]) continue;
            MuseumMove(museumDevStand[museumDev], museumDevLook[museumDev]);
            return;
        }
        museumDev = -1;
    }

    /// <summary>1.7: the next of the kit's four members on the same door, dressed again in place (render only).</summary>
    void MuseumNextMember()
    {
        long edge;
        if (!MuseumEdgeOf(7, out edge)) return;
        museumMember = (museumMember + 1) % 4;
        museumMembers[edge] = (FrontRoomsDoorMember)museumMember;
        // 1.7's floor tag is the member's.
        SyncMuseumPlacardVariants();
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

    /// <summary>
    /// R: the museum back as built, only while the game plays. Doors with the map's ResetDoorForTools (shut, latched, whole, an
    /// alarm door armed again, 2.1 and 2.3 locked again: the key's unlock is forgotten); a window that was touched with
    /// ResetWindowForTools (H6: unbroken, unpried, no crack, no sash progress, its foil armed; the map builds its chunk again);
    /// room 2's key with TakeKeyBackForTools (H6: the player no longer holds it, its home chunk is built again, so it lies on
    /// 2.2's floor). The player first steps out of a window's closet and out of 2.1's and 2.3's, so a mended pane or a door
    /// locked again never shuts them in. A door whose dressing changed (broken, a tripped alarm, a key-drawn deadbolt) comes back
    /// with its chunk built again; each chunk is built once.
    /// </summary>
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
        var doors = 0;
        var windows = 0;
        var brokenDoors = 0;
        var keyBack = false;
        var east = new GridCoord(museumChunk.x + 1, museumChunk.y);
        bool rebuildWest = false, rebuildEast = false;
        try
        {
            // A latch under E is let go first, as the game lets go (its StopPry keeps the progress; the reset below clears it).
            MuseumLetGoOfLatches();
            MuseumOutOfCloset();
            for (var room = 1; room <= 3; room++)
            for (var slot = 1; slot <= 7; slot++)
            {
                var door = MuseumDoorAt(room, slot);
                if (door == null) continue;
                // A broken leaf's dressing (the break's pieces), a tripped alarm door's, or a key-opened door's (the LOCKED model
                // keeps its deadbolt drawn, FrontRoomsInteractableKit.DressDoor) comes back as built only with a fresh dress.
                var redress = door.broken || (door.alarm && map.IsAlarmTripped(door.edge)) || map.IsUnlockedForTools(door);
                if (door.broken) brokenDoors++;
                if (redress && MapGrid.ChunkOf(door.a) == museumChunk) rebuildWest = true;
                else if (redress) rebuildEast = true;
                map.ResetDoorForTools(door);
                doors++;
            }
            for (var slot = 1; slot <= 4; slot++)
            {
                GridCoord a, b;
                if (!MuseumWindowCells(slot, out a, out b) || !MuseumWindowTouched(slot, a, b)) continue;
                // The rebuild inside ResetWindowForTools releases the old record first, and the release keeps a latch's part-pried
                // progress (KeepSash: sashHolds from the record) after the reset has cleared it: the old record goes to 0 first.
                var w = MuseumWindowAt(slot);
                if (w != null)
                {
                    w.sashHold = 0f;
                    w.hold = 0f;
                }
                map.ResetWindowForTools(a, b);
                windows++;
                // It built the windows' chunk (room 3's, with 3.5) again.
                if (MapGrid.ChunkOf(a) == east) rebuildEast = false;
            }
            // Room 2's key back on 2.2's floor: the map forgets it is held (the key HUD lets go) and builds its home chunk again,
            // which lays it there (after the windows, so a part-pried latch's zeroed record is never released twice). Whenever
            // the carve's move laid it there, room 2's locks on or off (a lost door turns them off and leaves the key takeable).
            if (museumKeyMoved && map.HasKeyFor(museumKeyZone))
            {
                map.TakeKeyBackForTools(museumKeyZone);
                keyBack = true;
                if (museumKeyZone == museumChunk) rebuildWest = false;
                else if (museumKeyZone == east) rebuildEast = false;
            }
            if (rebuildWest) map.RebuildChunk(museumChunk);
            if (rebuildEast) map.RebuildChunk(east);
            Physics.SyncTransforms();
        }
        catch (Exception e)
        {
            MuseumNote("R stopped part way (" + e.GetType().Name + ": " + e.Message + ").");
        }
        museumTagClock = 0f;
        Toast("R  ·  THE MUSEUM AS BUILT", doors + " doors shut, latched and whole" + (brokenDoors > 0 ? " (the broken too: B breaks one again)" : "")
            + (museumKeysLive ? ", 2.1 and 2.3 locked again; " + (keyBack ? "the key back on 2.2's floor" : "the key still on 2.2's floor")
                : keyBack ? "; the key back on 2.2's floor" : "")
            + "; " + (windows > 0 ? windows + (windows == 1 ? " window" : " windows") + " whole and latched again, the foil armed" : "every window is as built") + ".");
    }

    /// <summary>A room-3 window someone has touched: broken, pried, cracked, held, its latch worked or its foil tripped.</summary>
    bool MuseumWindowTouched(int slot, GridCoord a, GridCoord b)
    {
        var edge = MapGrid.EdgeId(a, b);
        if (map.IsBrokenWindow(a, b) || map.IsSashPried(edge) || map.IsFoilTripped(edge)) return true;
        var w = MuseumWindowAt(slot);
        return w != null && (w.stage > 0 || w.hold > 0f || w.sashHold > 0f);
    }

    /// <summary>
    /// Before a museum rebuild (R, 1.1's switch): every room-3 latch let go the game's way (ReleaseHold: StopPry keeps the
    /// progress and raises SashPryReleased). A rebuild drops a latch from the map's tables with no release, and the sound
    /// layer's sash strain would go on after the window stands whole. A latch not under E is left as it is. The four windows
    /// share room 3's chunk, so this runs before the first rebuild, never per window.
    /// </summary>
    void MuseumLetGoOfLatches()
    {
        for (var slot = 1; slot <= 4; slot++)
        {
            var w = MuseumWindowAt(slot);
            if (w != null && w.latch != null) map.ReleaseHold(w.latch);
        }
    }

    /// <summary>The player in a closet behind one of room 3's windows (its bays .1-.3, its lit corner) or behind 2.1 or 2.3 (R
    /// locks them again, and their key goes back to 2.2): out to that station's stop first.</summary>
    void MuseumOutOfCloset()
    {
        if (player == null) return;
        var c = map.CellOf(player.position);
        int i, j;
        var room = MuseumRoomOf(c.x - museumCell0.x, c.y - museumCell0.y, out i, out j);
        if (room != 2 && room != 3) return;
        var part = MuseumPartOf(i, j);
        int slot;
        if (room == 3) slot = part == MuseumPart.Corner && i == 3 ? 4 : part == MuseumPart.Bay ? SlotOfBay(i, j) : 0;
        else slot = part == MuseumPart.Bay && FrontKind(2, SlotOfBay(i, j)) == EdgeKind.Door ? SlotOfBay(i, j) : 0;
        if (slot < 1 || (room == 3 && slot > 4)) return;
        var spot = MuseumSpotOf(room, slot);
        if (spot.ok) MuseumMove(spot.stand, MuseumLookAt(room, slot, spot));
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

    /// <summary>A station the museum runs now: room 1's doors; room 2's two locked doors (our rule took), its key (moved onto
    /// 2.2) and 2.5's tag; room 3's windows, its alarm door (the map's mark took) and its ALARM ZONE tell; room 4's kits; the
    /// alcoves of rooms 1, 2, 3 (with its window) and 4. 2.6, 2.7, 3.7 and 4.2 are frames.</summary>
    bool MuseumLive(int room, int slot)
    {
        if (museumState != MuseumState.Built || map == null) return false;
        switch (room)
        {
            case 1:
                return slot == 4 || MuseumDoor(slot) != null;
            case 2:
                if (slot == 4) return true;
                if (slot == 5) return ExhibitOk(2, 5);
                if (!museumKeysLive) return false;
                if (slot == 2) return museumKeyMoved;
                if (slot == 1 || slot == 3)
                {
                    var locked = MuseumDoorAt(2, slot);
                    return locked != null && map.LockedByRule(locked);
                }
                return false;
            case 3:
                if (slot >= 1 && slot <= 4) return MuseumWindowAt(slot) != null;
                if (slot == 5)
                {
                    var door = MuseumDoorAt(3, 5);
                    return door != null && door.alarm;
                }
                return slot == 6 && ExhibitOk(3, 6);
            case 4:
                return slot == 4 || (slot != 2 && ExhibitOk(4, slot));
        }
        return false;
    }

    /// <summary>Room 2's run in the game's order stands: both doors locked by our rule and the key moved onto 2.2
    /// (CheckMuseumRoomTwo clears museumKeysLive when any of it did not take).</summary>
    bool MuseumKeysRun() => museumKeysLive && museumKeyMoved && museumState == MuseumState.Built && map != null;

    /// <summary>A room with a live station (the hub's room signs and the list say LIVE).</summary>
    bool MuseumRoomLive(int room)
    {
        if (room < 1 || room > 4) return false;
        for (var slot = 1; slot <= 7; slot++)
            if (slot != 4 && MuseumLive(room, slot)) return true;
        return false;
    }

    static string MemberName(int member) => member == 1 ? "OFFICE" : member == 2 ? "RUN!" : member == 3 ? "EXIT" : "LOBBY";

    string MemberLetter() => ((char)('A' + Mathf.Clamp(museumMember, 0, 3))).ToString();

    // 4.7's close-up stood at now ("A".."D"), or the row's span before the first 7 again.
    string DevLetter() => museumDev >= 0 && museumDev < DevCount ? ((char)('A' + museumDev)).ToString() : "A-D";

    static string MuseumRoomTitle(int room)
    {
        var sign = RoomSign(room);
        return room + "  " + (sign != null ? sign.name ?? "" : "");
    }

    /// <summary>The toast on walking into a room ([ ] and 0).</summary>
    string MuseumRoomLine(int room)
    {
        if (room == 0) return "The board on the left: P opens the plan of every station.";
        if (room == 1) return "Live: six doors of the game's own and an alcove. 1-7 a station, again for its next variant.";
        if (room == 2 && MuseumKeysRun())
            return "Live, in the game's order up the right wall: 2.1 a locked door, 2.2 its zone's key on the bay floor, 2.3 the door that key opens; 2.5 the key's tag. 2.6 and 2.7 (stair key, EXIT): tour 2.";
        if (room == 3 && MuseumRoomLive(3))
            return "Live: three windows with their sash latches (3.3 foiled), an alarm door and a Run! room's ALARM ZONE arch; the alcove looks through a window. 1-7 a station.";
        if (room == 4 && MuseumRoomLive(4))
            return "Live: a terminal, three ad screens, the evacuation placard, the station's papers and the four test-bed close-ups. 4.2's TV needs a station: tour 2.";
        return "Placards for now: each station's code, what it is and where it can be tried today (P, then ENTER).";
    }

    /// <summary>What this stop is in the museum now, one line: a live station's state and its key, or a frame's tour of today.</summary>
    string MuseumHere(int room, int slot, FrontRoomsShowcaseTable.Station st)
    {
        if (room == 3 && slot == 4 && MuseumLive(3, 4))
            return "An office window into a lit corner, in game light: break or pry it as 3.1 and 3.2 (R makes it whole). The 1990 corner (window, latch and foil together) is dressed later.";
        if (slot == 4 && room >= 1 && room <= 4)
            return "An empty alcove in game light. Its 1990 corner (" + (room == 1 ? "this room's doors together" : "this room's things together") + ") is dressed later.";
        if (room == 2) return MuseumHereRoomTwo(slot, st);
        if (room == 3) return MuseumHereRoomThree(slot, st);
        if (room == 4) return MuseumHereRoomFour(slot, st);
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
        return MuseumFrameLine(st);
    }

    static string MuseumFrameLine(FrontRoomsShowcaseTable.Station st)
    {
        if (st == null) return "";
        var today = st.tour >= 2 && st.tour <= 4 ? "tour " + st.tour + " (P, then ENTER on " + st.code + ")" : st.tour == 1 ? "the media bay (F2, then 1)" : "in no tour";
        return "FRAME: built here in a later increment. Today: " + today + ".";
    }

    /// <summary>Room 2's lines: each locked door's state (locked, the key in hand, unlocked, broken), the key on 2.2 or in the
    /// HUD, 2.5's tag; 2.6 and 2.7 frames for tour 2.</summary>
    string MuseumHereRoomTwo(int slot, FrontRoomsShowcaseTable.Station st)
    {
        if (slot == 6)
            return "FRAME: the stair key hangs on a station's key plate, and the museum has no station (Q-1). Today: tour 2 (P, then ENTER on 2.6).";
        if (slot == 7)
            return "FRAME: the chained EXIT is the floor's own way down, planned with its station (Q-1). Today: tour 2 (P, then ENTER on 2.7).";
        if (!MuseumLive(2, slot))
        {
            // Off: no one-zone place (the key sealed away), or a door lost at the build while the key's move took (it lies on 2.2).
            if (slot >= 1 && slot <= 3 && !museumKeysLive)
                return "FRAME: room 2's locks are off on this map (see the note on the list): its doors open free and "
                    + (!museumKeyMoved ? "its key lies sealed away"
                        : map != null && map.HasKeyFor(museumKeyZone) ? "its zone's key is in your hand, though no lock here needs it (R puts it back on 2.2)"
                        : "its zone's key still lies on 2.2's floor, though no lock here needs it (E takes it all the same)")
                    + ". Today: tour 4 (P, then ENTER on 2." + slot + ").";
            return MuseumFrameLine(st);
        }
        var held = map.HasKeyFor(museumKeyZone);
        if (slot == 2)
            return held
                ? "TAKEN: the key HUD holds it while you stand in its zone (all of room 2). It opens 2.3, and 2.1 too. R puts it back on this floor."
                : "Its zone's key, lying on the bay floor as the game lays a key at its site: look at it, E · TAKE KEY. The pickup shot, then the key HUD holds it.";
        if (slot == 5)
            return (museumKeyMoved ? "The tag of 2.2's key (the same shape and colour)" : "A zone key's tag (this museum's own zone)")
                + ", set on a plinth at 1.2 m: E · EXAMINE KEY, the mouse turns it. It prints the floor's code (00 on floor 1).";
        var door = MuseumDoorAt(2, slot);
        if (door == null) return MuseumFrameLine(st);
        if (door.broken) return "BROKEN (B, as the Relay breaks a door, locked or not): it hangs askew, E says WON'T SHUT. R makes it whole and locked again.";
        if (map.IsUnlockedForTools(door))
            return slot == 3
                ? "UNLOCKED by the key-turn: it stays unlocked from both sides, its deadbolt drawn. R locks it again and puts the key back on 2.2."
                : "UNLOCKED: 2.2's key turned here too (one key opens every locked door of its zone). R locks it again and puts the key back on 2.2.";
        if (slot == 1)
            return held
                ? "LOCKED, and its zone's key is in your hand: from inside room 2, E · UNLOCK opens this one too (one key opens every locked door of the zone you stand in). 2.3 is the one kept for the key-turn."
                : "LOCKED by its own rule (the museum's other doors stay free): E rattles it, LOCKED · NEEDS THIS ZONE'S KEY. Its key lies on 2.2.";
        return held
            ? "KEY IN HAND: E · UNLOCK, the key-turn shot, then the door opens."
            : "LOCKED: take 2.2's key first (until then E rattles it). With the key in hand: E · UNLOCK and the key-turn shot.";
    }

    /// <summary>Room 3's lines: each window's state as the map keeps it (broken, pried, cracked, its foil tripped), 3.5's alarm, 3.6's tell.</summary>
    string MuseumHereRoomThree(int slot, FrontRoomsShowcaseTable.Station st)
    {
        if (slot == 7) return "FREE: a bay kept for the next device. Nothing stands here.";
        if (!MuseumLive(3, slot))
        {
            // A player build has no tools mark (MarkAlarmDoorForTools is editor-only): 3.5 is then the plain door it was carved as.
            if (slot == 5 && MuseumDoorAt(3, 5) != null) return "A plain door: the map's alarm mark (editor only) did not take here. Tour 3 has the planner's alarm doors.";
            return MuseumFrameLine(st);
        }
        if (slot == 5)
        {
            var door = MuseumDoorAt(3, 5);
            if (door.broken) return "BROKEN (B, the Relay's break, which never trips it): it hangs askew. R makes it whole and armed again.";
            return map.IsAlarmTripped(door.edge)
                ? "TRIPPED: its first opening sounded the horn (once per door and floor; the Relay is held here, so nothing comes). R arms it again."
                : "ARMED: push-bar alarm door. Its first opening, either side, trips the horn and calls the Relay (held here). R arms it again.";
        }
        if (slot == 6)
            return "A Run! room's entrance as tour 3 hangs it: the 1.4 m arch, the ALARM ZONE card on your left at 1.524 m, the red-banded detector on the ceiling inside.";
        GridCoord a, b;
        MuseumWindowCells(slot, out a, out b);
        var edge = MapGrid.EdgeId(a, b);
        var w = MuseumWindowAt(slot);
        var foil = slot == 3 && map.IsFoilTripped(edge) ? " Its foil TRIPPED (one call per window and floor; the Relay is held here)." : "";
        if (map.IsSashPried(edge))
            return "PRIED: the sash is up for good; walk into the opening to climb into its lit closet." + foil + " R latches it again.";
        if (map.IsBrokenWindow(a, b))
            return "BROKEN: walk into the opening to climb through into its lit closet (and back)." + foil + " R makes it whole again.";
        if (w != null && w.stage > 0)
            return "CRACKED: hold E again to finish it; cracks never heal." + foil + " R makes it whole again.";
        if (slot == 1) return "WHOLE: hold E on the glass within 1.2 m to break it, then walk in to climb through. Its latch pries too (3.2).";
        if (slot == 2) return "LATCHED: hold E on the latch at the meeting rail to pry the sash up: quiet, heard within 8 m (the Relay is held here).";
        return "ARMED: foil tape on the glass and a reed contact at the sash: a pry or a break trips it (its latch says ALARMED).";
    }

    /// <summary>Room 4's lines: the kit objects as they stand, and 4.2's frame.</summary>
    string MuseumHereRoomFour(int slot, FrontRoomsShowcaseTable.Station st)
    {
        if (slot == 2)
            return "FRAME: the station TV's live close-up belongs to a station, and the museum has none. Today: tour 2 (P, then ENTER on 4.2).";
        if (!MuseumLive(4, slot)) return MuseumFrameLine(st);
        switch (slot)
        {
            case 1:
                return "The first terminal in the game: no desk carries one yet (level design, L2). E · READ SCREEN; inside, its own keys (F1 help, F3 out).";
            case 3:
                return museumAdScreens + " CRTs with the media bay's ad (FrontRoomsScreenVideo, no sound): E on a screen within 2.4 m turns it off or on.";
            case 5:
                return "The stream room's sheet, hung as the game hangs it (1.56 m, the glow on). A station floor hangs its own sheet: tour 2.";
            case 6:
                return "The station's clipboard, register and sign-out sheet, as its desk holds them. No close-up is wired for these kits yet (their sidecars carry a note only), and with no station their dates stay blank.";
            case 7:
                return (museumDev >= 0 ? DevLetter() + "  " + DevNames[museumDev] + " now. " : "")
                    + "E · LOOK on each: the test bed's close-ups (never shipped), on here. 7 again: the next one (A phone, B clock, C vending machine, D copier).";
        }
        return "";
    }

    /// <summary>A museum-only Try line where the table's would mislead here (it speaks of the tours or the media bay, or has none).</summary>
    string MuseumTryLine(int room, int slot)
    {
        if (room == 1 && slot == 6) return "Look at the leaf and its lite: the Run! goal door's member. The corridor itself: ROOM TYPES R11, tour 3.";
        if (room == 2 && slot == 6) return "In tour 2 (P, then ENTER on 2.6): E · TAKE KEY on the station's key plate; taking it signals the panel.";
        if (room == 2 && slot == 7) return "In tour 2 (P, then ENTER on 2.7): the stair key in hand, E · UNLOCK, the padlock cut and the chain fall.";
        if (room == 3 && slot == 4 && MuseumLive(3, 4)) return "Hold E on its glass to break it or on its latch to pry it, as at 3.1 and 3.2; walk in to climb.";
        if (room == 3 && slot == 6) return "Look at the card beside the arch and the detector on the ceiling inside; walk in under it. Nothing arms.";
        if (room == 4)
        {
            if (slot == 2) return "In tour 2 (P, then ENTER on 4.2): E · WATCH TV at the station's TV; the world runs on.";
            if (slot == 3) return "Look at a screen within 2.4 m: E turns it off or on, as in the media bay (menu 1).";
            if (slot == 6) return "Look at them: the daily activity report, the register and the sign-out sheet (E opens nothing yet).";
            if (slot == 7) return "E · LOOK on each one; 7 again stands you at the next.";
        }
        return null;
    }

    /// <summary>The short state of a row, on the list and under its framed code.</summary>
    string MuseumRowTag(FrontRoomsShowcaseTable.Station st)
    {
        if (st == null) return "";
        var room = FrontRoomsShowcaseTable.RoomOf(st);
        var slot = FrontRoomsShowcaseTable.SlotOf(st);
        if (room == 3 && slot == 4 && MuseumLive(3, 4)) return "LIVE  ·  " + MuseumWindowTag(4);
        if (slot == 4 && room >= 1) return "OPEN  ·  GAME LIGHT";
        if (MuseumLive(room, slot))
        {
            if (room == 2) return "LIVE  ·  " + MuseumKeysTag(slot);
            if (room == 3) return "LIVE  ·  " + (slot == 5 ? MuseumAlarmTag() : slot == 6 ? "CARD AND DETECTOR" : MuseumWindowTag(slot));
            if (room == 4)
                return slot == 1 ? "LIVE  ·  E READ SCREEN" : slot == 3 ? "LIVE  ·  E ON / OFF" : slot == 5 ? "LIVE  ·  E READ PLAN"
                    : slot == 6 ? "LIVE  ·  LOOK ONLY" : "LIVE  ·  " + DevLetter();
            if (slot == 1) return (FrontRoomsShowcaseTable.VariantOf(st) == 'B') == !map.BodyDoors ? "LIVE  ·  NOW" : "LIVE  ·  1 AGAIN";
            if (slot == 5)
            {
                var door = MuseumDoor(5);
                return door != null && door.broken ? "LIVE  ·  BROKEN" : "LIVE  ·  B BREAKS IT";
            }
            if (slot == 7) return "LIVE  ·  MEMBER " + MemberLetter();
            return "LIVE";
        }
        if (room == 3 && slot == 7) return "FREE";
        if (st.tour >= 2 && st.tour <= 4) return "FRAME  ·  TOUR " + st.tour;
        if (st.tour == 1) return "FRAME  ·  MEDIA BAY";
        return "FRAME";
    }

    /// <summary>Room 2's short states: a locked door LOCKED / KEY IN HAND / UNLOCKED / BROKEN, the key E TAKE KEY / TAKEN, the tag.</summary>
    string MuseumKeysTag(int slot)
    {
        if (slot == 5) return "E EXAMINE KEY";
        var held = museumKeysLive && map.HasKeyFor(museumKeyZone);
        if (slot == 2) return held ? "TAKEN" : "E TAKE KEY";
        var door = MuseumDoorAt(2, slot);
        if (door == null) return "";
        if (door.broken) return "BROKEN";
        if (map.IsUnlockedForTools(door)) return "UNLOCKED";
        return held ? "KEY IN HAND" : "LOCKED";
    }

    string MuseumWindowTag(int slot)
    {
        GridCoord a, b;
        if (!MuseumWindowCells(slot, out a, out b)) return "";
        var edge = MapGrid.EdgeId(a, b);
        if (slot == 3 && map.IsFoilTripped(edge)) return "TRIPPED";
        if (map.IsSashPried(edge)) return "PRIED";
        if (map.IsBrokenWindow(a, b)) return "BROKEN";
        var w = MuseumWindowAt(slot);
        if (w != null && w.stage > 0) return "CRACKED";
        return slot == 2 ? "LATCHED" : slot == 3 ? "ARMED" : "WHOLE";
    }

    string MuseumAlarmTag()
    {
        var door = MuseumDoorAt(3, 5);
        if (door == null) return "";
        return door.broken ? "BROKEN" : map.IsAlarmTripped(door.edge) ? "TRIPPED" : "ARMED";
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
            live = MuseumRoomLive(t.room);
            line = !live ? "FRAMED CODES  ·  P FOR THE PLAN" : t.room == 1 ? "LIVE  ·  SIX DOORS AND AN ALCOVE"
                : t.room == 2 ? (MuseumKeysRun() ? "LIVE  ·  LOCKED DOORS, ZONE KEY, KEY TAG" : "LIVE  ·  KEY TAG (LOCKS OFF)")
                : t.room == 3 ? "LIVE  ·  WINDOWS, ALARM DOOR, ALARM ZONE" : "LIVE  ·  SCREENS, PLACARD, PAPERS, TEST BED";
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
        // The code in Plex Mono Medium (20 §1: codes and figures; Bayon's "2.1" read "2.I"), as the lists' rows set it.
        var codeSize = rowCodeStyle.CalcSize(new GUIContent(code));
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
        Text(new Rect(r.x + pad, ty, w - pad * 2f, codeSize.y), code, rowCodeStyle, live ? Accent : Paper, alpha);
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
            y = RowDecide(x, y, w, sign.decide, .9f, 8f * s);
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
            Span(x, y + BayonAscent * nameStyle.fontSize, st.code ?? "", rowCodeStyle, PlexAscent, isMarked || live ? Accent : Paper, live || isMarked ? 1f : .75f);
            Text(new Rect(x + 64f * s, y, Mathf.Max(40f * s, w - 304f * s), 26f * s), st.kind ?? "", nameStyle, Paper, live ? 1f : .7f);
            Text(new Rect(x + w - 240f * s, y + 5f * s, 240f * s, 22f * s), MuseumRowTag(st), statusStyle, Paper, live ? .95f : .55f);
            y += 28f * s;
        }
        if (room != 0) return;
        var rooms = "";
        for (var r = 1; r <= 4; r++) rooms += (r > 1 ? "  ·  " : "") + MuseumRoomTitle(r) + (MuseumRoomLive(r) ? " (LIVE)" : "");
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
        y = Para(x, y, w, "[ ] ROOM TO ROOM  ·  1-7 A STATION, AGAIN FOR ITS NEXT VARIANT  ·  0 THE HUB  ·  R THE MUSEUM AS BUILT (DOORS, WINDOWS, ROOM 2'S KEY)  ·  B BREAK THE DOOR IN VIEW  ·  G HIDE  ·  F2 TOURS", smallStyle, Paper, .85f, 6f * s);
        y = Para(x, y, w, MuseumKeysRun()
            ? "Live: room 1's six doors; room 2's locked doors, zone key and key tag; room 3's windows, alarm door and ALARM ZONE arch; room 4's terminal, screens, placard, papers and test bed. 2.6, 2.7 and 4.2 stand as placards; each says where to try that station today."
            : "Live: room 1's six doors; room 3's windows, alarm door and ALARM ZONE arch; room 4's terminal, screens, placard, papers and test bed. Room 2's locks and key, 2.6, 2.7 and 4.2 stand as placards; each says where to try that station today.", smallStyle, Paper, .8f, 6f * s);
        y = Para(x, y, w, BoardLine(3), smallStyle, Paper, .7f, 6f * s);
        var sign = RoomSign(0);
        if (sign != null && !string.IsNullOrEmpty(sign.decide))
            DecideLines(x, y + NotoAscent * cardZhBoldStyle.fontSize, w, sign.decide, cardZhBoldStyle, ZhPitch * s, 1f, true, true);
    }

    /// <summary>Placard C in the museum, in the placard type (.Plan.cs DrawCard, on C's 540 measure): code chip and kind, Try (KEYS,
    /// TOUCH), what it is, what Red decides with its bar, the small line, then what the museum shows here now as the accent live
    /// line at the foot, and the variants as letter chips.</summary>
    void DrawMuseumPlacard(FrontRoomsShowcaseTable.Station st, int room, int slot, float x, float y, float w, float s)
    {
        var tryLine = MuseumTryLine(room, slot) ?? st.tryKeys ?? (st.guideKind >= 0 && st.guideKind < KindCount ? HowTo[st.guideKind] : "");
        var rule = st.rule == FrontRoomsShowcaseTable.Rule.Media ? "MEDIA BAY"
            : st.rule == FrontRoomsShowcaseTable.Rule.None ? "" : "RULE " + FrontRoomsShowcaseTable.RuleName(st.rule);
        var small = st.staging ?? "";
        if (!string.IsNullOrEmpty(st.status)) small += (small.Length > 0 ? "  ·  " : "") + st.status;
        if (rule.Length > 0) small += (small.Length > 0 ? "  ·  " : "") + rule;
        DrawCard(x, y + 8f * s, w, 1f, true, st.code, st.kind, TryLabel, tryLine, TouchLabel, st.tryTouch, st.what, st.decide, small,
            MuseumHere(room, slot, st), st.variants, null);
    }
}
