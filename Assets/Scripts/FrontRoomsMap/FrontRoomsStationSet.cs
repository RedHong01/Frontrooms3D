using System.Collections.Generic;
using FrontRooms.Map;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// A26 step 3 (notes/floor1_start_group_plan.md 4.4, 4.7, 9): the floor-1 station's set piece, what the map builds in
/// the start group's room beyond its carved shell. Made by FrontRoomsMapWorld.SetStation as a child of the map (the
/// stairwell precedent, but map-owned: it lives for the floor, through every rebuild of the station's chunks, and goes
/// with ClearStation and NextFloor). It holds:
/// - the station module's props, through the map's own per-prop step (PropsLeftOut, PlaceProp: the PlaceProps rules),
///   a few a frame within the map's furnishing budget (StepProps from the map's Update; CompleteProps for builds
///   nobody watches). Besides PlaceProps' rules it keeps the start door's sweep clear at any height (the stream door's
///   cell, 1.28 m deep, as for a room the stream door opens onto), floor props 1.0 m clear inside E1 and E2, and
///   nothing standing over 1.50 m within 0.6 m of the door axis (B8; plan 4.9);
/// - the south liner: two render-only panels in the station's wall skin, 0.004 m proud of the stream facade's map
///   face, from the station's west wall face to its east wall face, leaving out the stream door's 2.4 m opening and
///   its header, while the start area stands (the station's south edge is open to the stream rooms then);
/// - the riser chase in the north-east cell (FD-b; plan 9): a solid shell block floor to ceiling, its west face
///   RiserWidth in from the east wall line and its south face RiserDepth in from the north wall line, in the wall
///   skin, registered as the map's architecture so the Relay never walks through it;
/// - from ClearStartArea, the static entrance: the start door seen from the room, shut, render-only, on the inner face
///   of the data wall that D's south edge builds once the strip is given back (the liner goes then);
/// - the plate's anchor (PlateAnchor): THIS DOOR TO REMAIN UNLOCKED DURING BUSINESS HOURS on the west liner of D.
/// Renderers show while the station's chunk c or X's chunk c' is built (SetVisible, from the map); colliders stay on.
/// The visual chat dresses it (its own entrance, the plate, the board on the riser's west face) from Renderers and
/// the anchors; the map draws only stand-ins.
/// </summary>
public sealed class FrontRoomsStationSet : MonoBehaviour
{
    /// <summary>The riser chase's footprint in the north-east cell, from the east and north wall lines (FD-b: west face x 10.3, south face 2.1 m from the north wall).</summary>
    public const float RiserWidth = 1.7f, RiserDepth = 2.1f;
    /// <summary>The stream facade's map face lies this far north of the door line (FrontRoomsRoomStream's 0.04 m end wall, centred on it); the liner stands LinerProud in front of it.</summary>
    public const float FacadeFace = .02f, LinerProud = .004f;
    /// <summary>The stream door's opening and header (FrontRoomsRoomStream DoorWidth, DoorHeaderHeight; RoomHeight 2.9 m): the liner leaves them out.</summary>
    public const float StreamDoorWidth = 2.4f, StreamHeaderBottom = 2.66f;
    /// <summary>B8: nothing over AxisMaxHeight within AxisHalfWidth of the door axis, from the south wall to the north.</summary>
    public const float AxisHalfWidth = .6f, AxisMaxHeight = 1.5f;
    /// <summary>The plate (the information design chat's, 457.2 x 152.4 mm, centre 1,525 mm up), its door-side edge PlateDoorGap west of the stream door's opening.</summary>
    public const float PlateWidth = .4572f, PlateHeight = .1524f, PlateCentreHeight = 1.525f, PlateDoorGap = .075f;
    /// <summary>The static entrance's stand-in leaves (the stream door's: 1.12 m, up to its 2.62 m), proud of the wall face.</summary>
    public const float EntranceLeafWidth = 1.12f, EntranceLeafHeight = 2.62f, EntranceLeafDepth = .05f;

    FrontRoomsMapWorld map;
    StartGroupLayout layout;
    RoomModuleData module;
    Material wall;
    Transform propsRoot;
    GameObject liner, entrance;
    BoxCollider riser;
    readonly List<Renderer> renderers = new List<Renderer>();
    string[] leftOut;
    Rect?[] rects;
    int next;
    bool visible = true;
    float ox, oz;

    /// <summary>The station's props not yet placed: 0 when every one stands (or was left out).</summary>
    public int PropsWaiting => module == null || module.props == null ? 0 : module.props.Length - next;
    /// <summary>Every prop of the station module has been placed or left out (the map's StationReady).</summary>
    public bool PropsDone => PropsWaiting == 0;
    /// <summary>Props spawned so far, props left out (with a warning), and the StepProps calls that spawned something.</summary>
    public int PropsPlaced { get; private set; }
    public int PropsLeftOutCount { get; private set; }
    public int PropSteps { get; private set; }
    /// <summary>Every renderer of the set (liner, riser block, static entrance, props spawned so far): for the visual chat's rendering layers.</summary>
    public IReadOnlyList<Renderer> Renderers => renderers;
    /// <summary>The riser chase's collider (registered as architecture).</summary>
    public BoxCollider RiserBlock => riser;
    /// <summary>The south liner (active while the start area stands) and the static entrance (active from ClearStartArea).</summary>
    public GameObject Liner => liner;
    public GameObject Entrance => entrance;
    public bool Visible => visible;
    /// <summary>The keep-clear rects in map metres (x, z): the start door's sweep, inside E1, inside E2, the door axis.</summary>
    public Rect DoorSweep { get; private set; }
    public Rect E1Clear { get; private set; }
    public Rect E2Clear { get; private set; }
    public Rect AxisStrip { get; private set; }

    /// <summary>
    /// Build the set for <paramref name="layout"/> on <paramref name="map"/> (FrontRoomsMapWorld.SetStation): the liner,
    /// the riser block and the hidden static entrance at once, the props queued. <paramref name="wallSkin"/> is the
    /// station's wall material, <paramref name="trim"/> and <paramref name="leaf"/> the map's frame and door materials.
    /// </summary>
    public static FrontRoomsStationSet Create(FrontRoomsMapWorld map, StartGroupLayout layout, RoomModuleData module, Material wallSkin, Material trim, Material leaf)
    {
        var go = new GameObject("Station set");
        go.transform.SetParent(map.transform, false);
        var set = go.AddComponent<FrontRoomsStationSet>();
        set.map = map;
        set.layout = layout;
        set.module = module;
        set.wall = wallSkin;
        set.Build(trim, leaf);
        return set;
    }

    void Build(Material trim, Material leaf)
    {
        var cs = MapGrid.CellSize;
        var s = layout.station;
        ox = s.x * cs;
        oz = s.y * cs;
        float west = s.x * cs, east = (s.x + s.w) * cs, south = s.y * cs, north = (s.y + s.h) * cs;
        var ceiling = ModuleUnits.StandardCeiling;
        var axis = (layout.door.x + .5f) * cs;

        // The keep-clear rects: the door's cell as for a room the stream door opens onto (MW Dress), 1.0 m inside each arch.
        DoorSweep = new Rect(layout.door.x * cs, south, cs, ModuleUnits.DoorClearDepth + ModuleUnits.WallHalf);
        var depth = ModuleUnits.EntryClearDepth + ModuleUnits.WallHalf;
        map.Cache.ArchOverride(layout.e1In, layout.e1Out, out var w1, out var c1);
        E1Clear = new Rect(layout.e1In.x * cs + c1 - w1 * .5f, north - depth, w1, depth);
        map.Cache.ArchOverride(layout.e2Out, layout.e2In, out var w2, out var c2);
        E2Clear = new Rect(west, layout.e2In.y * cs + c2 - w2 * .5f, depth, w2);
        AxisStrip = new Rect(axis - AxisHalfWidth, south, AxisHalfWidth * 2f, north - south);

        // The south liner over the facade, west and east of the stream door (render-only).
        liner = new GameObject("South liner");
        liner.transform.SetParent(transform, false);
        var z = south + FacadeFace + LinerProud * .5f;
        float inWest = west + ModuleUnits.WallHalf, inEast = east - ModuleUnits.WallHalf, doorWest = axis - StreamDoorWidth * .5f, doorEast = axis + StreamDoorWidth * .5f;
        Panel(liner.transform, "South liner west", wall, new Vector3((inWest + doorWest) * .5f, ceiling * .5f, z), new Vector3(doorWest - inWest, ceiling, LinerProud), false);
        Panel(liner.transform, "South liner east", wall, new Vector3((doorEast + inEast) * .5f, ceiling * .5f, z), new Vector3(inEast - doorEast, ceiling, LinerProud), false);

        // The riser chase: solid shell, floor to ceiling (into the ceiling slab), architecture.
        var block = Panel(transform, "Riser block", wall, new Vector3(east - RiserWidth * .5f, (ceiling + ModuleUnits.CeilingSlab * .5f) * .5f, north - RiserDepth * .5f),
            new Vector3(RiserWidth, ceiling + ModuleUnits.CeilingSlab * .5f, RiserDepth), true);
        riser = block.GetComponent<BoxCollider>();
        map.RegisterArchitecture(riser);

        // The static entrance (shut, from ClearStartArea on): the stream door's leaves and a frame on the wall's inner face.
        entrance = new GameObject("Static entrance");
        entrance.transform.SetParent(transform, false);
        var face = south + ModuleUnits.WallHalf;
        var frame = ModuleUnits.TrimFace;
        for (var side = -1; side <= 1; side += 2)
        {
            Panel(entrance.transform, side < 0 ? "Static entrance leaf west" : "Static entrance leaf east", leaf,
                new Vector3(axis + side * (StreamDoorWidth * .5f - EntranceLeafWidth * .5f - .04f), EntranceLeafHeight * .5f, face + EntranceLeafDepth * .5f),
                new Vector3(EntranceLeafWidth, EntranceLeafHeight, EntranceLeafDepth), false);
            Panel(entrance.transform, side < 0 ? "Static entrance jamb west" : "Static entrance jamb east", trim,
                new Vector3(axis + side * (StreamDoorWidth * .5f + frame * .5f), StreamHeaderBottom * .5f, face + ModuleUnits.TrimProud * .5f),
                new Vector3(frame, StreamHeaderBottom, ModuleUnits.TrimProud), false);
        }
        Panel(entrance.transform, "Static entrance head", trim, new Vector3(axis, StreamHeaderBottom + frame * .5f, face + ModuleUnits.TrimProud * .5f),
            new Vector3(StreamDoorWidth + frame * 2f, frame, ModuleUnits.TrimProud), false);
        entrance.SetActive(false);

        // The props, placed a few a frame (StepProps). Which are left out is decided now, for all of them.
        propsRoot = new GameObject("Station props").transform;
        propsRoot.SetParent(transform, false);
        if (module != null && module.props != null && module.props.Length > 0)
        {
            var clear = new List<Rect> { E1Clear, E2Clear };
            leftOut = FrontRoomsMapWorld.PropsLeftOut(module, ox, oz, clear, DoorSweep, null, AxisRule, out rects);
        }
    }

    // B8 (plan 4.9): nothing standing over 1.50 m within 0.6 m of the door axis.
    string AxisRule(ModuleProp p, float[] footprint, Rect rect) =>
        p.y + footprint[4] > AxisMaxHeight && rect.Overlaps(AxisStrip) ? "would stand over " + AxisMaxHeight.ToString("0.00") + " m on the door axis of" : null;

    // A box in the set's (the map's) metres: a cube with its collider kept (solid) or gone at once (render-only).
    GameObject Panel(Transform parent, string name, Material material, Vector3 center, Vector3 size, bool solid)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = center;
        go.transform.localScale = size;
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = solid ? ShadowCastingMode.On : ShadowCastingMode.Off;
        r.enabled = visible;
        renderers.Add(r);
        if (!solid)
        {
            // Out of the physics scene at once: a deferred Destroy would leave it for this frame's queries.
            var c = go.GetComponent<Collider>();
            c.enabled = false;
            if (Application.isPlaying) Destroy(c); else DestroyImmediate(c);
        }
        return go;
    }

    /// <summary>Place props until <paramref name="budgetMs"/> is spent (at least one per call, as a furnishing step). True once all are done.</summary>
    public bool StepProps(float budgetMs)
    {
        if (PropsDone) return true;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        PropSteps++;
        do NextProp();
        while (!PropsDone && watch.Elapsed.TotalMilliseconds < budgetMs);
        return PropsDone;
    }

    /// <summary>Place every prop left now (builds nobody watches: Begin's buildAllNow).</summary>
    public void CompleteProps()
    {
        while (!PropsDone) NextProp();
    }

    void NextProp()
    {
        var index = next++;
        var p = module.props[index];
        if (string.IsNullOrEmpty(p.kit)) return;
        if (leftOut[index] != null)
        {
            PropsLeftOutCount++;
            Debug.LogWarning("[FrontRoomsMap] Station prop " + p.kit + " at (" + p.x.ToString("0.00") + ", " + p.z.ToString("0.00") + ") " + leftOut[index] + " the station; left out.");
            return;
        }
        var spawned = map.PlaceProp(module, index, propsRoot, ox, oz);
        if (spawned == null) return;
        PropsPlaced++;
        // Only the renderers the kit shows: SetVisible turns them off and on again, and leaves the others as the kit made them.
        foreach (var r in spawned.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.enabled) continue;
            r.enabled = visible;
            renderers.Add(r);
        }
    }

    /// <summary>Show or hide every renderer of the set (the map: while c or c' is built). Colliders stay on.</summary>
    public void SetVisible(bool show)
    {
        if (show == visible) return;
        visible = show;
        foreach (var r in renderers)
            if (r != null) r.enabled = show;
    }

    /// <summary>The start area is given back (ClearStartArea): the liner goes, the static entrance stands on D's new wall.</summary>
    public void StartAreaCleared()
    {
        if (liner != null) liner.SetActive(false);
        if (entrance != null) entrance.SetActive(true);
    }

    /// <summary>
    /// The plate's place (B3c; 61 4.4): on the west liner of D (from ClearStartArea, on the wall's inner face), its
    /// door-side edge PlateDoorGap west of the stream door's opening, its centre PlateCentreHeight up, facing into the
    /// room (+Z), in world space. The visual chat mounts the plate there.
    /// </summary>
    public void PlateAnchor(out Vector3 position, out Quaternion rotation)
    {
        var cs = MapGrid.CellSize;
        var axis = (layout.door.x + .5f) * cs;
        var face = layout.station.y * cs + (entrance != null && entrance.activeSelf ? ModuleUnits.WallHalf : FacadeFace + LinerProud);
        position = transform.TransformPoint(new Vector3(axis - StreamDoorWidth * .5f - PlateDoorGap - PlateWidth * .5f, PlateCentreHeight, face));
        rotation = transform.rotation;
    }

    /// <summary>Let go of what the map holds for it (the riser block's architecture), before the set is destroyed.</summary>
    public void Release()
    {
        if (map != null && riser != null) map.UnregisterArchitecture(riser);
    }
}
