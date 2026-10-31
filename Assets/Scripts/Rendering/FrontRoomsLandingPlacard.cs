using System.Collections.Generic;
using FrontRooms.Map;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// BS6 (research/tutorial/20_requests.md, 教程设计's row, "V6 now"; 10_proposal.md §5.4; 叙事 research/narrative/
/// 40_floors_exit_tutorial_strings.md §3): the copy of the evacuation plan on every stairwell landing. On each floor below
/// LEVEL 0 (the run's floor 2 = LEVEL B1, ...) the Q16 placard hangs on the MAP side of the stairwell's far (north) door,
/// at P1 west, exactly as Floor 1's title placard did beside the start door (FrontRoomsPlacard.Resolve, the same kit,
/// height, offsets, keep-clear rect and glow), so the sheet's YOU ARE HERE is true. FrontRoomsPlacard.Prepare stays the
/// title stream's (Floor 1); this is its per-floor counterpart.
///
/// Hook: FrontRooms3DGame.FloorChanged (raised after NextFloor, the floor's EXIT and station plans, SetExitLocked and Begin,
/// which builds nothing), subscribed directly (WebGL High stripping). One component per map; each floor change retires the
/// last floor's copy (its stairwell is two floors back) and hangs this floor's:
/// - only where 关卡设计's G1 rule for floors 2+ holds (FrontRoomsExitPlanner.StairDoorPlacardWest: the door cell's west
///   edge Open, a wall on the west cell's south edge to hang it on). Where it does not, the floor hangs no sheet (a P1 east
///   or P2 copy would draw YOU ARE HERE on the wrong side of the door);
/// - the sheet for THAT floor's own state (叙事 40 §3, A35): the full sheet (the security desk, the padlocked stair door)
///   when the floor has its own station and a locked EXIT (map.FloorHasStation and map.ExitLocked at the change), else the
///   Phase 0 sheet; with its LEVEL token, B10 and deeper LEVEL B10+ (FrontRoomsLandingPlacardSheets, generated with the
///   packed prints by Tools/lookdev/pack_landing_placards.py from 平面视觉's art, used as delivered);
/// - its EXAMINE transcript and regions from that sheet's examine.json (FrontRoomsInspectCatalog.SetTranscript);
/// - one floor's textures at a time: the print (and the full sheets' Locked glow) is loaded at the hang and unloaded one
///   frame after the copy goes. Prop_EvacPlan_E (the Phase 0 glow, shared with the kit's own material) is never unloaded.
/// The frame shows only while its wall's chunk and the west cell's are built (as the stairwell does, D8); it goes for good when the start
/// area is given back (the stairwell removed, its door no longer there) or when a rebuilt chunk no longer has the wall.
/// Desktop, iPhone and iPad hang the same copy (the lens kit too); WebGL keeps the title mount's rule: no lens.
/// </summary>
[AddComponentMenu("")]
[DisallowMultipleComponent]
public sealed class FrontRoomsLandingPlacard : MonoBehaviour
{
    public const string ObjectName = "Landing placard";

    // WebGL (a separate track, placard 10_spec §6): no lens renderer, as FrontRoomsPlacardMount.
#if (UNITY_WEBGL && !UNITY_EDITOR) || FRONTROOMS_WEBGL_PREVIEW
    static readonly bool SpawnLens = false;
#else
    static readonly bool SpawnLens = true;
#endif

    /// <summary>The shared Phase 0 glow (main's Prop_EvacPlan_E, also the kit material's emission map): never unloaded here.</summary>
    const string SharedGlow = "Surfaces/Textures/Prop_EvacPlan_E";

    // ---------------------------------------------------------------- bootstrap

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Boot()
    {
        FrontRooms3DGame.FloorChanged -= OnFloorChanged;
        FrontRooms3DGame.FloorChanged += OnFloorChanged;
    }

    static void OnFloorChanged(FrontRoomsMapWorld map, FrontRoomsMapHunter relay, int floor)
    {
        if (map == null) return;
        var landing = map.GetComponent<FrontRoomsLandingPlacard>();
        if (landing == null) landing = map.gameObject.AddComponent<FrontRoomsLandingPlacard>();
        landing.Hang(map, relay, floor);
    }

    /// <summary>The map's landing placard component, or null (tools and tests).</summary>
    public static FrontRoomsLandingPlacard Of(FrontRoomsMapWorld map) => map != null ? map.GetComponent<FrontRoomsLandingPlacard>() : null;

    // ---------------------------------------------------------------- state

    FrontRoomsMapWorld map;
    GameObject root;
    GridCoord stairCell, doorCell, westCell, wallCell;
    bool wasBuilt;
    Texture2D sheetPrint, sheetGlow;
    readonly List<Texture2D> unloadNextFrame = new List<Texture2D>();
    int retiredFrame = -1;

    /// <summary>The floor of the run this copy was hung for (2 = LEVEL B1), 0 before the first change.</summary>
    public int Floor { get; private set; }
    /// <summary>The sheet hung (null: none on this floor).</summary>
    public FrontRoomsLandingPlacardSheets.Sheet Sheet { get; private set; }
    /// <summary>Why this floor hangs no copy (null when one hangs).</summary>
    public string NoSheetReason { get; private set; }
    public FrontRoomsPlacard.Spot Spot { get; private set; }
    public bool Placed => root != null && Frame != null;
    public GameObject Root => root;
    public GameObject Frame { get; private set; }
    public GameObject Lens { get; private set; }
    public FrontRoomsPlacardGlow Glow { get; private set; }

    /// <summary>
    /// Hang this floor's copy (FloorChanged; public for tools). Retires the last floor's first. Floors below 2 hang none
    /// (LEVEL 0 has the title / station placard).
    /// </summary>
    public void Hang(FrontRoomsMapWorld world, FrontRoomsMapHunter relay, int floor)
    {
        Retire(true);
        map = world;
        Floor = floor;
        Sheet = null;
        NoSheetReason = null;
        Spot = default;
        if (map == null || floor < 2) { NoSheetReason = "LEVEL 0"; return; }
        var label = "LEVEL B" + (floor - 1) + " (floor " + floor + ")";
        var stair = StartStairwell(map);
        if (stair == null || map.Cache == null)
        {
            NoSheetReason = "no start stairwell";
            Debug.LogWarning("[Placard] landing " + label + ": no start stairwell on the map; no copy this floor");
            return;
        }
        stairCell = stair.Cell;
        doorCell = stair.NorthDoorCell;
        westCell = new GridCoord(doorCell.x - 1, doorCell.y);
        // The wall the frame hangs on is the north edge of the cell south of W, so that cell's chunk builds it (each chunk
        // builds its cells' east and north edges); W's chunk builds W and the open D-W side.
        wallCell = new GridCoord(westCell.x, westCell.y - 1);
        if (!FrontRoomsExitPlanner.StairDoorPlacardWest(map.Cache, doorCell))
        {
            // G1 for floors 2+ did not hold on this seed (SeedFor prefers it; the validator counts placardWestMissing).
            NoSheetReason = "StairDoorPlacardWest false (west " + map.Cache.Edge(doorCell, westCell) + ", wall under it "
                            + map.Cache.Edge(westCell, wallCell) + ")";
            Debug.Log("[Placard] landing " + label + ": no sheet: " + NoSheetReason + ", door cell " + doorCell);
            return;
        }
        var full = map.FloorHasStation && map.ExitLocked;
        var sheet = FrontRoomsLandingPlacardSheets.For(floor - 1, full);
        // The far door's centre on its line, at floor level; the frame sits on the map wall's north face (centred on the line).
        var doorPoint = stair.NorthDoorPoint;
        var spot = FrontRoomsPlacard.Resolve(map, doorPoint, doorPoint.z + ModuleUnits.WallHalf);
        if (spot.mount != FrontRoomsPlacard.Mount.P1Facade || spot.side >= 0 || spot.doorCell != doorCell)
        {
            NoSheetReason = "the spot resolved to " + FrontRoomsPlacard.Label(spot.mount) + (spot.side < 0 ? " west" : " east") + " at door cell " + spot.doorCell;
            Debug.LogWarning("[Placard] landing " + label + ": " + NoSheetReason + ", not P1 west at " + doorCell + "; no copy this floor");
            return;
        }
        Spot = spot;
        map.KeepClearAtStart(spot.keepClear);
        root = new GameObject(ObjectName + " " + sheet.level);
        root.transform.SetParent(map.transform, false);
        Frame = FrontRoomsKitLibrary.Spawn(FrontRoomsPlacard.KitName, root.transform, Vector3.zero, Quaternion.identity, null, false, "evacuation placard (landing)");
        if (Frame == null)
        {
            NoSheetReason = FrontRoomsPlacard.KitName + " missing";
            Debug.LogWarning("[Placard] " + FrontRoomsPlacard.KitName + " is missing; no landing copy");
            Retire(false);
            return;
        }
        Frame.transform.SetPositionAndRotation(spot.position, spot.rotation);
        var renderers = new List<Renderer>(Frame.GetComponentsInChildren<Renderer>(true));
        if (SpawnLens)
        {
            Lens = FrontRoomsKitLibrary.Spawn(FrontRoomsPlacard.LensKitName, root.transform, Vector3.zero, Quaternion.identity, null, false, "evacuation placard lens (landing)");
            if (Lens != null) Lens.transform.SetPositionAndRotation(spot.position, spot.rotation);
        }
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = true;
        }
        Glow = Frame.AddComponent<FrontRoomsPlacardGlow>();
        Glow.Bind(map, relay, spot.glowCell, renderers);
        sheetPrint = Resources.Load<Texture2D>(sheet.print);
        sheetGlow = Resources.Load<Texture2D>(sheet.glow);
        if (Glow.Instance != null && sheetPrint != null && sheetGlow != null)
        {
            Glow.Instance.SetTexture("_BaseMap", sheetPrint);
            Glow.Instance.SetTexture("_EmissionMap", sheetGlow);
        }
        else Debug.LogWarning("[Placard] landing " + label + ": sheet textures " + sheet.print + " / " + sheet.glow + " missing; the copy keeps the kit's default sheet");
        var inspectable = Frame.GetComponent<FrontRoomsInspectable>();
        if (inspectable != null && inspectable.Def != null)
        {
            FrontRoomsInspectCatalog.SetTranscript(sheet.transcript, sheet.lines);
            var def = inspectable.Def.Clone();
            def.transcript = sheet.transcript;
            def.regions = FrontRoomsLandingPlacardSheets.Regions(sheet);
            inspectable.Configure(inspectable.Kit, def, FrontRoomsKitLibrary.GetInfo(FrontRoomsPlacard.KitName));
        }
        Sheet = sheet;
        // Nothing of the floor is built yet (Begin built nothing): the copy shows with its wall (Update).
        wasBuilt = false;
        root.SetActive(false);
        Debug.Log("[Placard] landing " + label + ": P1 west, sheet " + sheet.folder + (full ? " (station, EXIT locked)" : " (no station of its own)")
                  + ", door cell " + doorCell + ", glow cell " + spot.glowCell + ", at " + spot.position.ToString("F4"));
    }

    void Update()
    {
        if (unloadNextFrame.Count > 0 && Time.frameCount > retiredFrame)
        {
            // The retired copy's material instance went at the end of the frame it was retired in: nothing holds these any more.
            foreach (var t in unloadNextFrame)
                if (t != null && t != sheetPrint && t != sheetGlow) Resources.UnloadAsset(t);
            unloadNextFrame.Clear();
        }
        if (root == null || map == null) return;
        // The start area given back: the stairwell and its door are gone, so the plan's YOU ARE HERE is no longer true.
        if (!map.HasStartArea || !map.InStartArea(stairCell))
        {
            Retire(true);
            return;
        }
        // Shown only while both chunks stand: the wall's (the cell south of W) and W's. A revisit shift needs a drop first, so
        // every rebuild passes through "not built" and is checked here.
        var built = map.IsChunkBuiltAt(wallCell) && map.IsChunkBuiltAt(westCell);
        if (built && !wasBuilt && !WallStands())
        {
            // A revisit shift rebuilt a chunk without the wall (or with the door side walled): the copy goes.
            Debug.Log("[Placard] landing LEVEL B" + (Floor - 1) + ": the wall at " + westCell + " is gone; the copy is taken down");
            Retire(true);
            return;
        }
        wasBuilt = built;
        if (root.activeSelf != built) root.SetActive(built);
    }

    bool WallStands() =>
        map.Cache != null && map.Cache.Edge(doorCell, westCell) == EdgeKind.Open
        && map.Cache.Edge(westCell, wallCell) == EdgeKind.Wall;

    // The stairwell the map's floor started in (its start area is that one cell).
    static FrontRoomsStairwell StartStairwell(FrontRoomsMapWorld map)
    {
        foreach (var s in FindObjectsByType<FrontRoomsStairwell>())
            if (s != null && s.Map == map && map.InStartArea(s.Cell)) return s;
        return null;
    }

    /// <summary>Take the copy down (deactivated now, destroyed at the end of the frame); its textures unload next frame.</summary>
    void Retire(bool unload)
    {
        if (root != null)
        {
            root.SetActive(false);
            if (Application.isPlaying) Destroy(root);
            else DestroyImmediate(root);
        }
        root = null;
        Frame = null;
        Lens = null;
        Glow = null;
        if (unload)
        {
            retiredFrame = Time.frameCount;
            if (sheetPrint != null) unloadNextFrame.Add(sheetPrint);
            if (sheetGlow != null && sheetGlow.name != System.IO.Path.GetFileName(SharedGlow)) unloadNextFrame.Add(sheetGlow);
        }
        sheetPrint = null;
        sheetGlow = null;
    }

    void OnDestroy()
    {
        // The map goes (a restart reloads the scene): the copy, a child of the map, goes with it, and the scene load unloads
        // what nothing holds. Nothing is touched here (the root may be mid-destroy).
        root = null;
        Frame = null;
        Lens = null;
        Glow = null;
        sheetPrint = null;
        sheetGlow = null;
        unloadNextFrame.Clear();
    }
}
