using System;
using System.Collections.Generic;
using FrontRooms.Map;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// C22 (Red, 2026-10-22; plan v2 c22_stairwell_plan_v2.md, D8): the EXIT's stairwell, the one-cell room in X
/// through which a run goes one floor deeper. The map leaves X out (SetExitArea: no floor, ceiling, lamp or
/// furniture of its own; walls on its west, east and north lines and the EXIT door on its south line, all in
/// the outside cells' paper), and this object stands in it:
/// - its own floor, a box collider that is always on (SetVisible never touches a collider), so the player can
///   never fall through X whatever is or is not built round it. The slab covers the whole cell, under the
///   map's walls, so both doorways have floor right to their lines;
/// - an inner shell (four walls, a 2.40 m ceiling) whose every piece stays <see cref="Inset"/> (0.11 m: half a
///   wall, the 2 cm a map door frame stands proud, and 1 cm clear) inside X's cell lines. Nothing of it is
///   coplanar with, or inside, a map wall or door frame, on the floor it was made on or on the next one, where
///   X is the start area and the map walls it in the same way. The shell's walls carry colliders too, so X stays
///   closed while the next floor's chunk round it is still being built;
/// - a south doorway lined up with the EXIT door (centred on it, the map door's height, and
///   <see cref="LeafRoom"/> wider each side, because the EXIT leaf swings into X through it, D6), and a static
///   shut leaf in it, hidden until Seal: from the seal on the EXIT is a shut door from inside, on this floor
///   and on the next, where the map has a plain wall behind it;
/// - a north door, the next floor's start door: a leaf that swings away from whoever Open is called for
///   (north, out of X, for the player inside) and shuts and locks for good on Close (D10). On the floor the
///   stairwell is made on, the map's wall stands 3 cm behind it; after NextFloor, X's north line is open on
///   the new map (its start area's door side), and JoinNextFloor builds the full-thickness end wall there,
///   with the map's paper on its corridor face, before the door may open;
/// - one lamp, a point light of range <see cref="LampRange"/> (1.5 m) at X's centre: its light ends on X's
///   lines, short of every map surface outside X's walls (map lamps have no shadows, so neither does it),
///   at a level the game sets (LampLevel: the blackout swap, D9).
/// Built for X from the map's own CellCenter and rotation, but parented to a transform the game gives (its
/// root), never to the map: NextFloor tears down everything the map built, and the stairwell outlives the
/// floor it was made on, since the next floor starts in it. The game drives it: SetVisible (renderers and lamp
/// on only while X's chunk is built), LampLevel, Seal (once the map's EXIT door has locked: ExitDoorSealed),
/// JoinNextFloor (after NextFloor), Open and Close. The north door moves in Update and stands still while the
/// map's DoorsPaused (the pause card), like the map's own doors.
/// Sound (AUDIO_CONTRACT.md): both hinges are named "Door hinge stairwell ...", so the audio layer's
/// hinge-driven door Foley finds them like any map door's (the south one never moves). The lamp is not named
/// "Light", so no fixture hum is attached to it until the sound chat asks for one.
/// Look: plain lit materials, and the map's own door veneer and frame trim (the EXIT door's); the visual chat
/// restyles it later (concrete, a stair cue).
/// </summary>
public sealed class FrontRoomsStairwell : MonoBehaviour
{
    // ---------- Measures: metres in the stairwell's own frame (origin on the floor at X's centre, +Z north, +X east, as the map's) ----------

    /// <summary>X's cell lines lie this far from its centre.</summary>
    const float Half = ModuleUnits.Cell * .5f;
    /// <summary>
    /// How far every piece of the shell stays inside X's cell lines: half a wall (map walls are centred on the
    /// lines), the 2 cm a map door frame stands proud of its wall (the EXIT door's frame on the south line),
    /// and 1 cm clear. Only the floor slab (under the walls, below floor level) and, from JoinNextFloor on, the
    /// north end wall come nearer.
    /// </summary>
    public const float Inset = ModuleUnits.WallHalf + ModuleUnits.TrimProud + .01f;
    /// <summary>The shell's walls: thick enough to hold a door leaf (0.05 m) inside their depth.</summary>
    public const float ShellThickness = .06f;
    /// <summary>Its ceiling, the map's low ceiling.</summary>
    public const float CeilingHeight = ModuleUnits.LowCeiling;
    /// <summary>
    /// The room a 95 deg leaf hung on a cell line needs, each side of the map's 1.0 m door opening, to swing
    /// past a wall 0.11-0.19 m in from that line: its half thickness (0.025), the 5 deg past square (0.017 over
    /// 0.19 m) and 1.8 cm clear. The south doorway (the EXIT leaf swings into X through it) and the north end
    /// wall (the north leaf swings out through it) are this much wider each side than the door they let through.
    /// </summary>
    public const float LeafRoom = .06f;
    public const float SouthOpeningWidth = ModuleUnits.DoorWidth + 2f * LeafRoom;
    /// <summary>The north door: the map's door size, hung like a map door.</summary>
    public const float NorthOpeningWidth = ModuleUnits.DoorWidth;
    public const float EndWallOpeningWidth = NorthOpeningWidth + 2f * LeafRoom;
    public const float OpeningHeight = ModuleUnits.DoorHeight;
    /// <summary>The lamp's reach: from X's centre it ends on X's lines, 8 cm short of the map's faces outside X's walls.</summary>
    public const float LampRange = Half;
    /// <summary>
    /// The light hangs this high at X's centre (its lens is on the ceiling): with a 1.5 m reach, low enough to
    /// light the floor round the middle and both doors at handle height (1.0 m on the north door is 1.42 m away).
    /// </summary>
    public const float LampHeight = 1.3f;
    public const float LampIntensity = 2.5f;
    public const float OpenDegrees = ModuleUnits.DoorSwingDegrees;

    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    static readonly int CeilingHeightId = Shader.PropertyToID("_CeilingHeight");

    FrontRoomsMapWorld map;
    GridCoord cell;
    readonly List<Renderer> renderers = new List<Renderer>();
    readonly List<Collider> solids = new List<Collider>();
    Transform southHinge, northHinge;
    Renderer southLeafRenderer;
    Collider southLeaf, northLeaf;
    Quaternion northClosed;
    Light lamp;
    Renderer lens;
    Color lensEmission;
    MaterialPropertyBlock lensBlock;
    GameObject endWall;
    bool visible, sealedShut;
    float lampLevel = 1f;

    // The north door: shut and waiting; the lever (Open.SwingStart); the eased-out swing onto its stop; open;
    // shutting (Close, a smoothstep back into the frame); locked for good.
    enum DoorPhase { Shut, Lever, Swing, Open, Shutting, Locked }
    DoorPhase phase;
    float angle, swing = 1f, clock, shutFrom, shutSeconds;

    /// <summary>The north door's leaf left the frame (Open) or started back into it (Close): the middle of X's north line.</summary>
    public event Action<Vector3> NorthDoorMoved;
    /// <summary>The north door latched after Close and is locked for good: the middle of X's north line. The sound layer's lock beat.</summary>
    public event Action<Vector3> NorthDoorLatched;

    /// <summary>
    /// Build the stairwell for cell <paramref name="cell"/> of <paramref name="map"/> (the floor's X, from
    /// FrontRoomsExitPlanner), at the map's CellCenter and rotation, under <paramref name="parent"/> (the game's
    /// root, never the map). Hidden until X's chunk is built (SetVisible). Call it with SetExitArea, before
    /// Begin, so it stands before the player can reach X.
    /// </summary>
    public static FrontRoomsStairwell Create(FrontRoomsMapWorld map, GridCoord cell, Transform parent)
    {
        var go = new GameObject("Stairwell " + cell);
        go.transform.SetPositionAndRotation(map.CellCenter(cell), map.transform.rotation);
        if (parent != null) go.transform.SetParent(parent, true);
        var stairwell = go.AddComponent<FrontRoomsStairwell>();
        stairwell.Build(map, cell);
        return stairwell;
    }

    public FrontRoomsMapWorld Map => map;
    /// <summary>X: the cell it stands in.</summary>
    public GridCoord Cell => cell;
    /// <summary>The cell its north door opens onto: the next floor's start door cell, X + (0, 1).</summary>
    public GridCoord NorthDoorCell => new GridCoord(cell.x, cell.y + 1);
    /// <summary>The middle of X's north line, on the floor: where the next floor's start door stands.</summary>
    public Vector3 NorthDoorPoint => transform.TransformPoint(new Vector3(0f, 0f, Half));
    /// <summary>The middle of X's south line, on the floor: the EXIT door.</summary>
    public Vector3 SouthDoorPoint => transform.TransformPoint(new Vector3(0f, 0f, -Half));
    public Transform NorthHinge => northHinge;
    public Transform SouthHinge => southHinge;
    public Collider NorthLeaf => northLeaf;
    public Collider SouthLeaf => southLeaf;
    public Light Lamp => lamp;
    public bool Visible => visible;
    /// <summary>Seal was called: the static leaf stands shut in the south doorway.</summary>
    public bool Sealed => sealedShut;
    /// <summary>JoinNextFloor built the north end wall: the north door may open.</summary>
    public bool Joined => endWall != null;
    /// <summary>The north leaf's angle out of the frame, 0..95 deg.</summary>
    public float NorthDoorAngle => angle;
    /// <summary>The side the north leaf swings to: +1 north (out of X), -1 south (into X).</summary>
    public float NorthDoorSwing => swing;
    /// <summary>The north leaf has left the frame (or its lever is down) and Close has not been called.</summary>
    public bool NorthDoorOpen => phase == DoorPhase.Lever || phase == DoorPhase.Swing || phase == DoorPhase.Open;
    /// <summary>Close was called and the leaf is not yet back in the frame.</summary>
    public bool NorthDoorClosing => phase == DoorPhase.Shutting;
    /// <summary>Close was called and the leaf latched: shut and locked for good (nothing opens it again).</summary>
    public bool NorthDoorLocked => phase == DoorPhase.Locked;

    /// <summary>
    /// Renderers and the lamp on or off (D8: on only while X's chunk is built, so nothing of the stairwell
    /// shows while the map round it is not there). Colliders are never touched: the floor and the shell keep
    /// X closed whatever is built.
    /// </summary>
    public void SetVisible(bool on)
    {
        visible = on;
        foreach (var r in renderers)
            if (r != null) r.enabled = on && (r != southLeafRenderer || sealedShut);
        ApplyLamp();
    }

    /// <summary>The lamp's level, 0 (dark) to 1 (normal): its light's intensity and its lens's glow. The blackout swap (D9) takes it to 0 and back.</summary>
    public float LampLevel
    {
        get => lampLevel;
        set
        {
            lampLevel = Mathf.Clamp01(value);
            ApplyLamp();
        }
    }

    /// <summary>
    /// The seal: the static leaf stands shut in the south doorway from now on (its collider too). Call it once
    /// the map's EXIT door has locked (ExitDoorSealed), so the map's leaf is back in its frame on the line, out
    /// of the way. Calling it again does nothing.
    /// </summary>
    public void Seal()
    {
        if (sealedShut) return;
        sealedShut = true;
        southLeafRenderer.enabled = visible;
        southLeaf.enabled = true;
    }

    /// <summary>
    /// Open the north door for someone standing at <paramref name="opener"/>: the leaf swings away from them,
    /// north (out of X, onto the next floor) for anyone on X's side of the door, south into X for anyone beyond
    /// it, like a map door (A12): the lever, Open.SwingStart (0.1 s) later the leaf leaves the frame
    /// (NorthDoorMoved) and eases out over Open.SwingSeconds onto 95 deg. False once it has been opened or
    /// closed. Open it out of X only after JoinNextFloor: before that the map's wall stands behind it.
    /// </summary>
    public bool Open(Vector3 opener)
    {
        if (phase != DoorPhase.Shut) return false;
        swing = transform.InverseTransformPoint(opener).z <= NorthLeafZ ? 1f : -1f;
        phase = DoorPhase.Lever;
        clock = 0f;
        return true;
    }

    /// <summary>
    /// Shut the north door for good (D10, once the player is 4 m out on the next floor): the leaf turns back
    /// into the frame on a smoothstep over the share of Open.SwingSeconds its angle is of 95 deg (NorthDoorMoved),
    /// and latches and locks there (NorthDoorLatched). A door never opened locks at once. Nothing opens it again.
    /// No body stops it: the game closes it with the player well clear and the Relay not yet released.
    /// </summary>
    public void Close()
    {
        if (phase == DoorPhase.Shutting || phase == DoorPhase.Locked) return;
        if (phase == DoorPhase.Shut || phase == DoorPhase.Lever || angle <= 0f)
        {
            angle = 0f;
            PoseNorth();
            Lock();
            return;
        }
        shutFrom = angle;
        shutSeconds = Mathf.Max(.1f, FrontRoomsShotTimings.Open.SwingSeconds * angle / OpenDegrees);
        clock = 0f;
        phase = DoorPhase.Shutting;
        NorthDoorMoved?.Invoke(NorthDoorPoint);
    }

    /// <summary>
    /// The floor that starts here has been made (NextFloor made X its start area, the door cell X + (0, 1)):
    /// build the north end wall that the new map leaves to the stairwell. X's north line is open on the new map
    /// (its start area's door side), and its west and east walls stop on that line, so this wall closes it: from
    /// the shell's outer face to half a wall past the line (its corridor face where a map wall's would be),
    /// between X's side walls, as tall as the door cell's ceiling, with an opening the north leaf swings through
    /// (<see cref="LeafRoom"/> wider each side), in the door cell's own paper (the map's material, so the print
    /// runs on), framed on the corridor side in the map's trim. Built only now: on the floor the stairwell was
    /// made on, the old map's wall stands in the same place. False (and nothing built) unless the map's start
    /// area is X; true again once built.
    /// </summary>
    public bool JoinNextFloor()
    {
        if (endWall != null) return true;
        if (map == null || !map.InStartArea(cell))
        {
            Debug.LogWarning("[FrontRoomsStairwell] JoinNextFloor: the map's start area is not " + cell + "; NextFloor must make X the next floor's start area first.");
            return false;
        }
        var door = NorthDoorCell;
        var doorCeiling = MapGrid.CeilingHeight(map.ZoneOf(door).height);
        var top = Mathf.Max(doorCeiling, CeilingHeight + ModuleUnits.CeilingSlab);
        var paper = map.WallPaperOf(door);
        var trim = FrontRoomsSurfaces.CoveBase;
        endWall = new GameObject("North end wall");
        endWall.transform.SetParent(transform, false);
        var outer = Half - Inset;
        var face = Half + ModuleUnits.WallHalf;
        var depth = face - outer;
        var z = (outer + face) * .5f;
        var span = Half - ModuleUnits.WallHalf;
        var half = EndWallOpeningWidth * .5f;
        var frame = ModuleUnits.TrimFace;
        var sideLength = span - (half + frame);
        var headerBottom = OpeningHeight + frame;
        var pieces = new List<Renderer>
        {
            Box(endWall.transform, "End wall west", paper, new Vector3(-(span - sideLength * .5f), top * .5f, z), new Vector3(sideLength, top, depth), true),
            Box(endWall.transform, "End wall east", paper, new Vector3(span - sideLength * .5f, top * .5f, z), new Vector3(sideLength, top, depth), true),
            Box(endWall.transform, "End wall header", paper, new Vector3(0f, (headerBottom + top) * .5f, z), new Vector3(2f * (half + frame), top - headerBottom, depth), true),
        };
        // The frame is flush with the wall on X's side and stands proud on the corridor side, as a map door's does.
        var frameDepth = depth + ModuleUnits.TrimProud;
        var frameZ = outer + frameDepth * .5f;
        Box(endWall.transform, "End wall frame west", trim, new Vector3(-(half + frame * .5f), headerBottom * .5f, frameZ), new Vector3(frame, headerBottom, frameDepth), false);
        Box(endWall.transform, "End wall frame east", trim, new Vector3(half + frame * .5f, headerBottom * .5f, frameZ), new Vector3(frame, headerBottom, frameDepth), false);
        Box(endWall.transform, "End wall frame head", trim, new Vector3(0f, OpeningHeight + frame * .5f, frameZ), new Vector3(2f * half, frame, frameDepth), false);
        // The paper's grime band runs under the door cell's ceiling, as on the map's own walls.
        if (paper != null && paper.HasProperty(CeilingHeightId))
        {
            var block = new MaterialPropertyBlock();
            block.SetFloat(CeilingHeightId, transform.position.y + doorCeiling);
            foreach (var r in pieces) r.SetPropertyBlock(block);
        }
        SetVisible(visible);
        return true;
    }

    /// <summary>Tools and tests in edit mode, where Update does not run: move the north door by one step (DoorsPaused holds it, as in play).</summary>
    public void TickForTools(float dt)
    {
        if (map != null && map.DoorsPaused) return;
        Tick(dt);
    }

    void Update()
    {
        if (map != null && map.DoorsPaused) return;
        Tick(Time.deltaTime);
    }

    void Tick(float dt)
    {
        if (phase == DoorPhase.Lever)
        {
            clock += dt;
            if (clock < FrontRoomsShotTimings.Open.SwingStart) return;
            // The rest of this step goes to the swing.
            dt = clock - FrontRoomsShotTimings.Open.SwingStart;
            clock = 0f;
            phase = DoorPhase.Swing;
            NorthDoorMoved?.Invoke(NorthDoorPoint);
        }
        if (phase == DoorPhase.Swing)
        {
            clock += dt;
            var t = Mathf.Clamp01(clock / FrontRoomsShotTimings.Open.SwingSeconds);
            // Eased out, as a pushed door: fast off the latch, slowing onto the stop.
            angle = OpenDegrees * (1f - (1f - t) * (1f - t));
            if (t >= 1f) phase = DoorPhase.Open;
            PoseNorth();
        }
        else if (phase == DoorPhase.Shutting)
        {
            clock += dt;
            var t = Mathf.Clamp01(clock / shutSeconds);
            angle = shutFrom * (1f - t * t * (3f - 2f * t));
            if (t >= 1f)
            {
                angle = 0f;
                Lock();
            }
            PoseNorth();
        }
    }

    void Lock()
    {
        phase = DoorPhase.Locked;
        NorthDoorLatched?.Invoke(NorthDoorPoint);
    }

    // As a map door: the hinge turns about its Y, +1 sends the leaf north.
    void PoseNorth() => northHinge.localRotation = northClosed * Quaternion.Euler(0f, -angle * swing, 0f);

    void ApplyLamp()
    {
        if (lamp != null)
        {
            lamp.intensity = LampIntensity * lampLevel;
            lamp.enabled = visible && lampLevel > .001f;
        }
        if (lens != null)
        {
            lensBlock.SetColor(EmissionId, lensEmission * lampLevel);
            lens.SetPropertyBlock(lensBlock);
        }
    }

    // The middle of the north wall's depth, where its leaf hangs.
    static float NorthLeafZ => Half - Inset - ShellThickness * .5f;

    // ---------- Building ----------

    void Build(FrontRoomsMapWorld map, GridCoord cell)
    {
        this.map = map;
        this.cell = cell;
        var wall = FrontRoomsSurfaces.Lit("Stairwell / wall", new Color(.62f, .61f, .57f), .1f);
        var floor = FrontRoomsSurfaces.Lit("Stairwell / floor", new Color(.40f, .39f, .37f), .15f);
        var ceiling = FrontRoomsSurfaces.Lit("Stairwell / ceiling", new Color(.72f, .71f, .68f), .05f);
        var housing = FrontRoomsSurfaces.Lit("Stairwell / lamp housing", new Color(.55f, .55f, .53f), .3f);
        var glow = new Color(1f, .93f, .78f) * 1.6f;
        var lensMaterial = FrontRoomsSurfaces.Lit("Stairwell / lamp lens", new Color(1f, .97f, .9f), .1f, 0f, glow);
        // The map's own door veneer and frame trim: the stairwell's doors read as the EXIT door does.
        var trim = FrontRoomsSurfaces.CoveBase;
        var veneer = FrontRoomsSurfaces.DoorVeneer;
        var outer = Half - Inset;
        var inner = outer - ShellThickness;
        var root = transform;

        // The floor: the whole cell, under the map's walls (below their feet), so each doorway has floor to its line.
        Box(root, "Floor", floor, new Vector3(0f, -ModuleUnits.FloorSlab * .5f, 0f), new Vector3(ModuleUnits.Cell, ModuleUnits.FloorSlab, ModuleUnits.Cell), true);
        Box(root, "Ceiling", ceiling, new Vector3(0f, CeilingHeight + ModuleUnits.CeilingSlab * .5f, 0f), new Vector3(2f * outer, ModuleUnits.CeilingSlab, 2f * outer), true);
        // The side walls run the whole length and close the corners; the end walls stand between them.
        Box(root, "Wall west", wall, new Vector3(-(outer + inner) * .5f, CeilingHeight * .5f, 0f), new Vector3(ShellThickness, CeilingHeight, 2f * outer), true);
        Box(root, "Wall east", wall, new Vector3((outer + inner) * .5f, CeilingHeight * .5f, 0f), new Vector3(ShellThickness, CeilingHeight, 2f * outer), true);
        southHinge = Doorway(-1f, SouthOpeningWidth, "south", wall, trim, veneer, out southLeaf);
        northHinge = Doorway(1f, NorthOpeningWidth, "north", wall, trim, veneer, out northLeaf);
        northClosed = northHinge.localRotation;
        southLeafRenderer = southLeaf.GetComponent<Renderer>();
        southLeaf.enabled = false;

        // One lamp: a housing and lens on the ceiling, the light below them at X's centre.
        Box(root, "Lamp housing", housing, new Vector3(0f, CeilingHeight - .03f, 0f), new Vector3(.36f, .06f, .36f), false);
        lens = Box(root, "Lamp lens", lensMaterial, new Vector3(0f, CeilingHeight - .065f, 0f), new Vector3(.3f, .01f, .3f), false);
        lens.shadowCastingMode = ShadowCastingMode.Off;
        lensEmission = lensMaterial.HasProperty(EmissionId) ? lensMaterial.GetColor(EmissionId) : Color.black;
        lensBlock = new MaterialPropertyBlock();
        var lightGo = new GameObject("Stairwell lamp");
        lightGo.transform.SetParent(root, false);
        lightGo.transform.localPosition = new Vector3(0f, LampHeight, 0f);
        lamp = lightGo.AddComponent<Light>();
        lamp.type = LightType.Point;
        lamp.range = LampRange;
        lamp.color = new Color(1f, .92f, .78f);
        lamp.shadows = LightShadows.None;

        SetVisible(map.IsChunkBuiltAt(cell));
    }

    /// <summary>
    /// One end of the shell (side -1 south, +1 north): the wall between the side walls with its doorway, the
    /// frame lining the doorway (flush with the wall's outer face, proud of its inner face, no collider, as a map
    /// door's), and a leaf on its hinge, shut. The hinge sits on the west jamb in the middle of the wall's depth
    /// and faces along the wall, as a map door's on its line, so the leaf spans the opening on its local Z.
    /// </summary>
    Transform Doorway(float side, float width, string name, Material wall, Material trim, Material veneer, out Collider leafCollider)
    {
        var outer = Half - Inset;
        var inner = outer - ShellThickness;
        var z = side * (outer + inner) * .5f;
        var half = width * .5f;
        var frame = ModuleUnits.TrimFace;
        var sideLength = inner - (half + frame);
        var headerBottom = OpeningHeight + frame;
        var root = transform;
        Box(root, "Wall " + name + " west", wall, new Vector3(-(inner - sideLength * .5f), CeilingHeight * .5f, z), new Vector3(sideLength, CeilingHeight, ShellThickness), true);
        Box(root, "Wall " + name + " east", wall, new Vector3(inner - sideLength * .5f, CeilingHeight * .5f, z), new Vector3(sideLength, CeilingHeight, ShellThickness), true);
        Box(root, "Wall " + name + " header", wall, new Vector3(0f, (headerBottom + CeilingHeight) * .5f, z), new Vector3(2f * (half + frame), CeilingHeight - headerBottom, ShellThickness), true);
        var frameDepth = ShellThickness + ModuleUnits.TrimProud;
        var frameZ = side * (outer - frameDepth * .5f);
        Box(root, "Frame " + name + " west", trim, new Vector3(-(half + frame * .5f), headerBottom * .5f, frameZ), new Vector3(frame, headerBottom, frameDepth), false);
        Box(root, "Frame " + name + " east", trim, new Vector3(half + frame * .5f, headerBottom * .5f, frameZ), new Vector3(frame, headerBottom, frameDepth), false);
        Box(root, "Frame " + name + " head", trim, new Vector3(0f, OpeningHeight + frame * .5f, frameZ), new Vector3(2f * half, frame, frameDepth), false);

        var hinge = new GameObject("Door hinge stairwell " + name).transform;
        hinge.SetParent(root, false);
        hinge.localPosition = new Vector3(-half, 0f, z);
        hinge.localRotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
        var leafRenderer = Box(hinge, "Door leaf", veneer, new Vector3(0f, (OpeningHeight - ModuleUnits.DoorLeafGap) * .5f, half),
            new Vector3(ModuleUnits.DoorLeafThickness, OpeningHeight - ModuleUnits.DoorLeafGap, width - ModuleUnits.DoorLeafGap), true);
        leafCollider = leafRenderer.GetComponent<Collider>();
        return hinge;
    }

    // A box of the stairwell: a cube scaled to size, lit, casting shadows; solid ones keep the cube's box collider.
    Renderer Box(Transform parent, string name, Material material, Vector3 center, Vector3 size, bool solid)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = center;
        go.transform.localScale = size;
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = ShadowCastingMode.On;
        r.enabled = visible;
        renderers.Add(r);
        var c = go.GetComponent<Collider>();
        if (solid) solids.Add(c);
        // Frames, the lamp: no collider, as the map's trims have none (immediate, so no physics query ever sees it).
        else DestroyImmediate(c);
        return r;
    }
}
