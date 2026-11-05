using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// SP7's treatments of the unreachable end (scene 4 only, RULES 5b; research/level_space/72_unreachable_end.md section 3,
// Red 2026-11-04: make each of them in scene 4 to look at). X in SP7 cycles them (Shift+X back), resets the room and
// shows the name; 1 stays today's SP7. Nothing here is in the game scene, the Level Designer or the build.
//
// The frame (FrontRoomsStructuresRunRecede) owns the room as built today: the corridor in Run! B's dress, the head (end
// wall, RN-F goal door with its F5 lite and C42, the goal head), the soffit track, the 14 cells and their reveal, the ghost
// bands and magnets, the 14 strips with their red tubes, the twin-head units, looks A/B/C, the reset re-strike, Reduce
// Flashing (F), the release hall. A treatment is a FrontRoomsStructuresRunEnd: the frame calls its hooks and hands it a
// FrontRoomsStructuresRunEndRoom, the API to everything the frame owns. A treatment's own things live under its Own root
// (built once, lazily, the first time it becomes active), shown only while it is active.

/// <summary>The room's phase as SP7 runs it (Armed: waiting for the trip; Tripped: the alarm; Resetting: T's re-strike).</summary>
public enum FrontRoomsStructuresRunEndPhase { Armed, Tripped, Resetting }

/// <summary>The walker's gait this frame: from its stamina (falling only while it sprints) and its own speed (pushes taken out).</summary>
public enum FrontRoomsStructuresRunEndGait { Still, Slow, Walk, Sprint }

/// <summary>
/// One R-B strip's drive for one frame, in the frame's own units (the numbers looks A/B/C and the re-strike use). A colour
/// left at zero (black) means the rig's own colour, so a new struct with only levels set lights in the strip's own colours.
/// </summary>
public struct FrontRoomsStructuresRunEndStrip
{
    /// <summary>The white tubes' Light: 1 = Run! B's wrap intensity (times the strip's scale).</summary>
    public float white;
    /// <summary>The room's red on the same Light: 1 = Red's picked level (room.RedLight).</summary>
    public float red;
    /// <summary>The white tubes' glow: 1 = the rig's strip lens emission.</summary>
    public float lens;
    /// <summary>The red tube's own burn, 0..1 of the rig's tube (the tube also shows the white tubes' bounce at lens).</summary>
    public float tube;
    /// <summary>Linear colour of the white part of the Light (zero: the rig's wrap colour).</summary>
    public Color whiteColour;
    /// <summary>Linear hue of the red part of the Light (zero: the rig's tube hue).</summary>
    public Color redColour;
    /// <summary>HDR emission of the white tubes at lens 1 (zero: the rig's strip lens emission).</summary>
    public Color lensColour;
    /// <summary>HDR emission of the red tube at tube 1 (zero: the rig's tube, redTubeLinear.gamma).</summary>
    public Color tubeColour;
    /// <summary>Read only: the strip's cell is lit and solid (a closed cell's strip comes in at 0).</summary>
    public bool open;

    /// <summary>All four levels at 0, the colours kept.</summary>
    public FrontRoomsStructuresRunEndStrip AsDark()
    {
        var s = this;
        s.white = s.red = s.lens = s.tube = 0f;
        return s;
    }

    /// <summary>The white tubes at a level (Light and lens), no red.</summary>
    public FrontRoomsStructuresRunEndStrip AsWhite(float level)
    {
        var s = this;
        s.white = s.lens = level;
        s.red = s.tube = 0f;
        return s;
    }

    /// <summary>Looks A and B's tripped strip: the white tubes out, the room's red and the red tube at a level.</summary>
    public FrontRoomsStructuresRunEndStrip AsRed(float level)
    {
        var s = this;
        s.white = s.lens = 0f;
        s.red = s.tube = level;
        return s;
    }

    /// <summary>Levels and colours from a to b (k 0..1); open from b.</summary>
    public static FrontRoomsStructuresRunEndStrip Lerp(FrontRoomsStructuresRunEndStrip a, FrontRoomsStructuresRunEndStrip b, float k)
    {
        k = Mathf.Clamp01(k);
        return new FrontRoomsStructuresRunEndStrip
        {
            white = Mathf.Lerp(a.white, b.white, k),
            red = Mathf.Lerp(a.red, b.red, k),
            lens = Mathf.Lerp(a.lens, b.lens, k),
            tube = Mathf.Lerp(a.tube, b.tube, k),
            whiteColour = Color.Lerp(a.whiteColour, b.whiteColour, k),
            redColour = Color.Lerp(a.redColour, b.redColour, k),
            lensColour = Color.Lerp(a.lensColour, b.lensColour, k),
            tubeColour = Color.Lerp(a.tubeColour, b.tubeColour, k),
            open = b.open,
        };
    }
}

/// <summary>One twin-head lamp's drive for one frame. A colour left at zero means the rig's own.</summary>
public struct FrontRoomsStructuresRunEndHead
{
    /// <summary>0..1 of the head's own intensity (its sealed-beam lens follows).</summary>
    public float level;
    /// <summary>The Light's colour (gamma, as Light.color takes it; zero: the rig's flood colour).</summary>
    public Color colour;
    /// <summary>HDR lens emission at level 1 (zero: the rig's head lens emission).</summary>
    public Color lensColour;
    /// <summary>Read only: the head's cell is lit and solid; the head rides the moving end (the goal head, C42's partner).</summary>
    public bool open, goal;

    public static FrontRoomsStructuresRunEndHead Lerp(FrontRoomsStructuresRunEndHead a, FrontRoomsStructuresRunEndHead b, float k)
    {
        k = Mathf.Clamp01(k);
        return new FrontRoomsStructuresRunEndHead
        {
            level = Mathf.Lerp(a.level, b.level, k),
            colour = Color.Lerp(a.colour, b.colour, k),
            lensColour = Color.Lerp(a.lensColour, b.lensColour, k),
            open = b.open,
            goal = b.goal,
        };
    }
}

/// <summary>
/// What a treatment's Tick is given each frame. Positions and directions are district metres (x across, y up, z along the
/// corridor from the entrance wall at z 0; the FD1 plane at 9, FD2 at 18, the 14th cell's end at 42).
/// </summary>
public struct FrontRoomsStructuresRunEndFrame
{
    /// <summary>Seconds this frame (the host's Time.deltaTime).</summary>
    public float dt;
    /// <summary>Seconds since the trip (Tripped) or since T began the re-strike (Resetting); 0 while armed.</summary>
    public float clock;
    /// <summary>Seconds since this treatment became active (never resets; lamp hold fades run on it).</summary>
    public float time;
    public FrontRoomsStructuresRunEndPhase phase;
    /// <summary>The walker's feet, its eye (1.62 m up) and the camera's forward (unit).</summary>
    public Vector3 feet, eye, view;
    /// <summary>The feet's velocity since the last frame (m/s), and the same with the treatment's own pushes taken out (the
    /// walker's own gait); both zero across a jump (R, [ ], a reset).</summary>
    public Vector3 velocity, own;
    /// <summary>The horizontal size of own (m/s): walk 3.2, sprint 5.5, Shift's slow gait 1.44, less against a wall.</summary>
    public float speed;
    /// <summary>Degrees between the view and the corridor's axis (+z), 0..180; the signed yaw off the axis, -180..180.</summary>
    public float axisAngle, yawOff;
    public FrontRoomsStructuresRunEndGait gait;
    /// <summary>Sprinting (the stamina bar falls), grounded (the walker's CharacterController).</summary>
    public bool sprinting, grounded;
    /// <summary>The 5 s bar, 0..1.</summary>
    public float stamina01;
}

/// <summary>
/// The room as the frame owns it, handed to every hook. District metres throughout (see FrontRoomsStructuresRunEndFrame).
/// Everything a treatment changes on the frame's objects through these members (or directly on the objects they return) is
/// put back by the frame before every reset (Y, V, X, entering and leaving SP7): transforms and parents, active states,
/// renderer enabled states and materials, lamp holds, the lamps' colours and ranges, the lite, the hall, the fog. A
/// treatment re-arranges its armed room in Reset, every time.
/// </summary>
public abstract class FrontRoomsStructuresRunEndRoom
{
    // ------------------------------------------------------------ the corridor's numbers (the frame's, unchanged)

    public const float HalfWidth = 1.42f, Ceiling = 2.9f, CellLength = 3f, Length = 42f, HeadTop = 2.88f, HeadHalf = 1.40f;
    public const float Fd1 = 9f, Fd2 = 18f, SlotBand = 2.25f, GazeDegrees = 35f, KeepAway = 12f, GlideSpeed = 6f;
    public const float HallZ0 = 42.1f, HallZ1 = 51f, HallCeiling = 5.4f, HallX0 = -1.5f, HallX1 = 7.5f, VestibuleZ0 = -6f;
    /// <summary>The head wall as BuildHead builds it (head space): its half thickness (the wall spans z -0.08..+0.08), the cove's
    /// top and depth on its corridor face, the door casing's outer half width.</summary>
    public const float HeadWallHalf = .08f, HeadCoveTop = .102f, HeadCoveDepth = .0035f, HeadCasingHalf = .56f;
    public const int Cells = 14;
    /// <summary>The lines the ghost bands stand on (a band shows once the head leaves its line), and the lines with magnets.</summary>
    public static readonly float[] BandLines = { 21f, 27f, 33f, 36f, 39f };
    public static readonly float[] MagnetLines = { 21f, 27f, 33f, 36f, 39f, 42f };

    /// <summary>The cell a z falls in (0..13).</summary>
    public static int CellOf(float z) => Mathf.Clamp(Mathf.FloorToInt(z / CellLength), 0, Cells - 1);

    // ------------------------------------------------------------ context

    public abstract FrontRoomsStructures Host { get; }
    public abstract FrontRoomsStructuresDistrict District { get; }
    /// <summary>The district's root (world position = Site, no rotation); district metres are its local space.</summary>
    public abstract Transform Root { get; }
    public abstract Vector3 Site { get; }
    /// <summary>The district's module kit (boxes, walls, slabs, stand-ins, kits by name): build inside BeginPart / EndPart.</summary>
    public abstract FrontRoomsStructuresKit Kit { get; }
    /// <summary>Run! B's rig numbers as the frame reads them (headDelay, wrapIntensity, resetCellSeconds, floodColor...).</summary>
    public abstract RunRigLayout Rig { get; }

    public Vector3 Local(Vector3 world) => world - Site;
    public Vector3 World(Vector3 local) => local + Site;
    /// <summary>The walker's feet now (district metres), in every hook (Trip and Done too, where no frame is handed in).</summary>
    public abstract Vector3 Feet { get; }

    // ------------------------------------------------------------ materials (the frame's; never destroy them)

    public abstract Material Lower { get; }        // Run! B's lower block tone (under 1.2 m)
    public abstract Material Upper { get; }        // the upper block tone
    public abstract Material FloorMat { get; }     // the corridor floor (release hall, fallback shell)
    public abstract Material Soffit { get; }
    public abstract Material Cove { get; }
    public abstract Material FrameSteel { get; }   // the dark-bronze door casing
    public abstract Material LeafMat { get; }      // door enamel (the fire-door leaves)
    public abstract Material Rubber { get; }       // the sweeps
    public abstract Material RedTubeStandIn { get; }
    public abstract Material HaloWhite { get; }    // the soffit halo (white), may be null
    public abstract Material HaloRed { get; }      // the soffit halo (red), may be null
    /// <summary>A material this treatment made: destroyed with the district.</summary>
    public abstract void Own(Material m);
    /// <summary>A mesh this treatment made: destroyed with the district.</summary>
    public abstract void Own(Mesh m);
    /// <summary>A texture this treatment made (a cookie, a mask): destroyed with the district.</summary>
    public abstract void Own(Texture t);

    // ------------------------------------------------------------ settings (the keys)

    /// <summary>V: 0..3 plain 7 / 9 / 12 / 14 cells, 4 receding 9 -> 12, 5 receding 9 -> 14.</summary>
    public abstract int Variant { get; }
    public abstract bool Receding { get; }
    /// <summary>The head's line when armed (27 in both receding variants) and its last line (36 or 42); R = FinalLine - RestLine.</summary>
    public abstract float RestLine { get; }
    public abstract float FinalLine { get; }
    /// <summary>L in force: 0 today's, 1 A, 2 B (the agreed one), 3 C (0 whenever the red tube is off).</summary>
    public abstract int Look { get; }
    /// <summary>H: the red tube.</summary>
    public abstract bool Tube { get; }
    /// <summary>F: Reduce Flashing, local to SP7. Every new light effect must calm down with it (rule 11: no large-area
    /// change over 3 Hz; with F on, fronts become 0.5 s cross-fades, no flick, no strobe, no stutter).</summary>
    public abstract bool Reduce { get; }
    /// <summary>B: T2 (the wall comes apart and re-stacks) or T1 (the whole end rolls).</summary>
    public abstract bool BodyT2 { get; }
    /// <summary>The strip Light's intensity at red 1 (Red's picked level).</summary>
    public abstract float RedLight { get; }

    // ------------------------------------------------------------ state

    public abstract FrontRoomsStructuresRunEndPhase Phase { get; }
    /// <summary>Seconds since the trip (Tripped) or since T's re-strike began (Resetting); 0 while armed.</summary>
    public abstract float Clock { get; }
    /// <summary>The end has done its recede (room.Done): the next trip is at the entrance, as a plain room's, until Y.</summary>
    public abstract bool Receded { get; }
    /// <summary>Clock at the final seat, -1 before it (look B's pulses start 1.0 s after it, or at 3.0 s).</summary>
    public abstract float FinalSeatTime { get; }
    /// <summary>The end (or whatever carries the door) is moving: look B's pulses wait and the door does not open. Set it
    /// while your end moves; room.Done clears it.</summary>
    public abstract bool HeadMoving { get; set; }
    /// <summary>The end is waiting to start (a gaze gate): look B's pulses wait. T's re-strike clears it.</summary>
    public abstract bool Waiting { get; set; }
    /// <summary>Trip the room now (as T; the frame trips it at the FD1 plane on its own).</summary>
    public abstract void Trip();
    /// <summary>Start T's re-strike now (only while tripped).</summary>
    public abstract void StartReset();
    /// <summary>The final seat: Receded, FinalSeatTime = Clock, the record keeps the line, the "look back" stop faces the head,
    /// HeadMoving and Waiting cleared; then the treatment's Done hook.</summary>
    public abstract void Done();
    /// <summary>A line in the HUD's flash (3 s).</summary>
    public abstract void Flash(string message);
    /// <summary>The plate is redrawn on the next frame (after the treatment's Status or OptionLine changed).</summary>
    public abstract void Dirty();

    // ------------------------------------------------------------ the walker

    public abstract FrontRoomsStructuresWalker Walker { get; }
    /// <summary>The walker's camera (FOV 76, far 46 in SP7): for frustum tests (GeometryUtility).</summary>
    public abstract Camera View { get; }
    /// <summary>Move the walker by delta (district metres) this frame through its CharacterController (it collides, steps and
    /// stays on the floor). False when it is frozen, riding or its body is off. The frame takes pushes out of frame.own.</summary>
    public abstract bool Push(Vector3 delta);

    // ------------------------------------------------------------ the head (the moving end)

    /// <summary>The head: block-faced end wall, the goal door, the goal head on its bracket, trolleys; built at z 0, placed at
    /// its line (localPosition (0, 0, z)). Its wall pieces are solid (one MeshCollider in the head part).</summary>
    public abstract Transform Head { get; }
    public abstract float HeadZ { get; }
    /// <summary>The head at z; moving relaxes the sweeps (seated: pressed). The goal head and C42 ride it.</summary>
    public abstract void PlaceHead(float z, bool moving);
    public abstract IReadOnlyList<Renderer> HeadWall { get; }
    public abstract IReadOnlyList<Transform> Sweeps { get; }
    public abstract Transform GoalLeaf { get; }
    public abstract Collider GoalLeafCollider { get; }
    /// <summary>C42's one-sided glow in the F5 lite (an unlit _BaseColor glow).</summary>
    public abstract Renderer GoalLite { get; }
    /// <summary>The lite's glow times scale (1 = as built).</summary>
    public abstract void SetLite(float scale);
    public abstract bool GoalOpen { get; }
    public abstract void CloseGoal();
    /// <summary>T2's blocks (about 90, head-local seats, parked hidden): shown while a T2 step is in the air.</summary>
    public abstract Transform Blocks { get; }
    /// <summary>Shows the blocks and hides the head's solid wall (or the reverse).</summary>
    public abstract void ShowBlocks(bool on);
    /// <summary>T2's programmed paths at t of a travel of the given length from one line to another (today's FlyBlocks).</summary>
    public abstract void FlyBlocks(float t, float travel, float from, float to);
    /// <summary>World bounds of the head's renderers (a frustum test: "is any of the end on screen").</summary>
    public abstract Bounds HeadBounds();
    /// <summary>The goal head's twin-head unit on its bracket (a child of the head, its pose as built: nod it, put it back).</summary>
    public abstract Transform GoalUnit { get; }
    /// <summary>The goal leaf's panic bar (a child of the leaf, its pose as built).</summary>
    public abstract Transform PanicBar { get; }
    /// <summary>The head's machine: the soffit trolleys with their hangers, the slots' brush seals and the guide pin, its own part
    /// under the head (render only): hide it for an end that never moves (#5: no track, so nothing hangs in the corners).</summary>
    public abstract Transform HeadMachine { get; }
    /// <summary>The head wall's solid pieces in head space (x across, y up; each spans z +-HeadWallHalf; the lower tone under
    /// 1.2 m) and its slots (x from, to; y from the slot's bottom to HeadTop), as BuildHead cuts them.</summary>
    public abstract IReadOnlyList<Rect> HeadWallPieces { get; }
    public abstract IReadOnlyList<Rect> HeadSlots { get; }
    /// <summary>The lit stand-in room behind the goal door while a treatment that OpensShort is active and the head stands short
    /// of 42 (it rides the head; its strip lights while the door is open), or null before it is first needed.</summary>
    public abstract Transform ShortRoom { get; }

    // ------------------------------------------------------------ the cells (0..13, cell c from z 3c to 3c + 3)

    /// <summary>Drawn: its dressing, strips and heads on (dark unless open).</summary>
    public abstract bool CellShown(int c);
    /// <summary>Lit and solid: its lamps live.</summary>
    public abstract bool CellOpen(int c);
    /// <summary>Clock when its lamps struck (a revealed cell), or float.NegativeInfinity (it follows the room's state).</summary>
    public abstract float RevealTime(int c);
    public abstract void ShowCell(int c, bool on);
    /// <summary>Lit and solid. strike: its lamps strike once from now (a seat); false: it is already in the room's state (a cell
    /// that appears unseen, as #4's).</summary>
    public abstract void OpenCell(int c, bool strike);
    /// <summary>Hidden and dark.</summary>
    public abstract void CloseCell(int c);
    /// <summary>Today's tell: every cell from one line to another drawn (dark).</summary>
    public abstract void ShowBetween(float from, float to);
    /// <summary>Today's seat: every cell from one line to another open (struck or not), the magnets and the hall's blind panel
    /// brought up to date; then the treatment's Seated hook.</summary>
    public abstract void Seat(float from, float to, bool strike);
    /// <summary>The dress's small pieces that start in the cell (the long shell pieces are in LongShell).</summary>
    public abstract IReadOnlyList<Renderer> CellRenderers(int c);
    /// <summary>The cell's strip root and twin-head units (GameObjects, switched with the cell).</summary>
    public abstract IReadOnlyList<GameObject> CellObjects(int c);
    /// <summary>World bounds of cell c's clear volume (a frustum test before a cell appears unseen).</summary>
    public Bounds CellBounds(int c) =>
        new Bounds(World(new Vector3(0f, Ceiling * .5f, c * CellLength + CellLength * .5f)), new Vector3(2f * HalfWidth, Ceiling, CellLength));

    // ------------------------------------------------------------ the record, the track, the fire-door frames

    /// <summary>The head leaves a line: its ghost band shows (if one stands there) and the record keeps the line.</summary>
    public abstract void Leaving(float line);
    /// <summary>The ghost band part at a line (BandLines), or null.</summary>
    public abstract GameObject Band(float line);
    /// <summary>The pair of hold-open magnets at a line (MagnetLines), or null.</summary>
    public abstract GameObject Magnets(float line);
    /// <summary>The magnets shown per the treatment's Lines and the cells shown.</summary>
    public abstract void RefreshMagnets();
    public abstract IReadOnlyList<float> SeatedLines { get; }
    /// <summary>The soffit track and the floor's guide slot (shown in receding variants).</summary>
    public abstract Transform Track { get; }
    /// <summary>0: FD1 at z 9 (the trip plane), 1: FD2 at z 18: each frame (the block wall with its 2.0 x 2.1 opening, the
    /// casing, the two open leaves, the magnets) is its own part with its own collider, so it can move or hide.</summary>
    public abstract Transform FireFrame(int i);

    // ------------------------------------------------------------ the dress (Run! B's production corridor, 1 x 14)

    /// <summary>The dress's root, or null when the dress failed and the plain fallback shell stands in.</summary>
    public abstract Transform Dress { get; }
    public abstract IReadOnlyList<Renderer> DressRenderers { get; }
    /// <summary>The dress's pieces longer than 4.5 m (floor, VCT lanes, walls' faces, soffit deck, main, tray, EMT): always on
    /// in today's room. Hiding one keeps its collider.</summary>
    public abstract IReadOnlyList<Renderer> LongShell { get; }
    /// <summary>The dress's colliders (the floor slabs: box colliders; the walker stands on these).</summary>
    public abstract IReadOnlyList<Collider> DressColliders { get; }
    /// <summary>A copy of one mesh piece (a LongShell renderer, any mesh renderer) cut to a box in district metres: its triangles
    /// clipped to the box (the cut faces stay open: cover the cut, as #2's collar does), a new renderer under parent where the
    /// piece stands now, with the piece's materials or the one given (every submesh), its UVs kept or (planarUv) the
    /// FrontRooms/Surface world planar frame at this pose (per face t, b with Cross(t, b) = n: floors x and -z, walls along the
    /// wall with v up), so a mesh-UV copy of a world-projected surface draws today's texels until it moves and then carries
    /// them. Render only (the piece's collider stays). Null when nothing of it is inside or its mesh cannot be read. The mesh
    /// is the treatment's (destroyed with the district).</summary>
    public abstract Renderer CutPiece(FrontRoomsStructuresRunEnd end, Renderer piece, Bounds box, Transform parent, Material material, bool planarUv);

    // ------------------------------------------------------------ the release hall (z 42.1..51)

    /// <summary>The block panel in the hall's frame while the head stands short of z 42.</summary>
    public abstract GameObject Blind { get; }
    public abstract void SetBlind(bool on);
    /// <summary>The hall's nine strips at a level (1 = as built, always on).</summary>
    public abstract void SetHall(float level);

    // ------------------------------------------------------------ lamps
    // Strips: index 0..13, strip i on the soffit at z 1.8 + 3i in cell i (strip 4 is the stutter tube). Heads: index 0..15,
    // two per twin-head unit; 0 and 1 are the goal heads (they ride the head), then units at z 3 (T-A), 9.2 (on FD1's north
    // face), 15, 22.5, 28.5, 34.5, 40.5. Each frame the frame computes today's drive (looks A/B/C, the reveal strike, T's
    // re-strike, Reduce Flashing), then a hold if one is set (cross-faded), then the treatment's Strip / Head hook, then lights
    // the lamp. T's re-strike and every reset release every hold, so today's re-strike always runs; hold again from
    // ResetStarted if a lamp must stay yours.

    public abstract int StripCount { get; }
    public abstract float StripZ(int i);
    public abstract int StripCell(int i);
    public abstract Light StripLight(int i);
    public abstract Transform StripRoot(int i);
    public abstract int HeadCount { get; }
    public abstract float HeadLampZ(int i);
    public abstract int HeadCell(int i);
    public abstract bool IsGoalHead(int i);
    public abstract Light HeadLight(int i);
    /// <summary>Today's drive of strip i now (what look A/B/C or the re-strike would show), the rig's colours filled in.</summary>
    public abstract FrontRoomsStructuresRunEndStrip TodayStrip(int i);
    public abstract FrontRoomsStructuresRunEndHead TodayHead(int i);
    /// <summary>What strip i showed last frame.</summary>
    public abstract FrontRoomsStructuresRunEndStrip ShownStrip(int i);
    public abstract FrontRoomsStructuresRunEndHead ShownHead(int i);
    /// <summary>Strip i shows target from now on, cross-fading from what it showed over fade seconds (0: at once).</summary>
    public abstract void HoldStrip(int i, FrontRoomsStructuresRunEndStrip target, float fade);
    /// <summary>Strip i back to today's drive, cross-fading over fade seconds.</summary>
    public abstract void ReleaseStrip(int i, float fade);
    public abstract bool StripHeld(int i);
    public abstract void HoldHead(int i, FrontRoomsStructuresRunEndHead target, float fade);
    public abstract void ReleaseHead(int i, float fade);
    public abstract bool HeadHeld(int i);
    /// <summary>Every strip and head back to today's drive.</summary>
    public abstract void ReleaseLamps(float fade);
    /// <summary>Today's drive of strip i as if it hung at z: its cell's own state stays (open, its reveal); every time that
    /// depends on the place comes from z, continuously (A/B's band and B's pulses at 0.15 s a cell from strip 0's z, C's tide,
    /// the rig's relay stagger, the re-strike's cell). At its own z it is TodayStrip. For a lamp that stands (one moved and
    /// docked); a lamp still moving times its band from its own catch with the curves below (#2's island).</summary>
    public abstract FrontRoomsStructuresRunEndStrip TodayStripAt(int i, float z);
    /// <summary>Today's drive of head lamp i as if it hung at z (the goal heads follow the head as always).</summary>
    public abstract FrontRoomsStructuresRunEndHead TodayHeadAt(int i, float z);
    /// <summary>The look's own tripped strip i, open or not, as today's drive shows it once its look's front has passed: A and B
    /// the room's red and the tube at 1 (B's pulse on it), C the red at 1, today's look the tube alone.</summary>
    public abstract FrontRoomsStructuresRunEndStrip TrippedStrip(int i);
    /// <summary>The clock from which today's drive of strip i is its TrippedStrip (its look's front and strike are over; B's
    /// pulses and today's lens afterglow aside); +infinity while not tripped.</summary>
    public abstract float TrippedSince(int i);
    // The frame's own light curves (FrontRoomsStructuresRunRecede), for a lamp timed on its own clock; F is in them as there.
    /// <summary>The white tubes' mains drop, 1 -> 0, t seconds after the lamp's front (F: a 0.5 s fall).</summary>
    public abstract float WhiteDrop(float t);
    /// <summary>The white lens's afterglow after the drop.</summary>
    public abstract float Afterglow(float t);
    /// <summary>The red strike: 0 -> 0.35 at once, a smoothstep to 1 over 0.40 s (F: a 0.5 s rise).</summary>
    public abstract float RedRise(float t);
    /// <summary>B's pulse level (0.35..1) at clock t for a lamp at z: every 2.5 s from 3.0 s (or 1.0 s after the final seat), 0.15 s
    /// a cell from the entrance, 12 at most, none while the end moves or waits (easing over 0.4 s when either changes).</summary>
    public abstract float PulseAt(float t, float z);
    /// <summary>T's re-strike level tr seconds after T for a lamp at z (the flick, dark, the rise; F: a 0.5 s rise, no flick).</summary>
    public abstract float Restrike(float tr, float z);
    /// <summary>The re-strike clock at which the white starts rising over the cell of z (F included: no 0.45 s wait).</summary>
    public abstract float RestrikeAt(float z);

    /// <summary>A light of the treatment's own under parent (local pose): not on the scene's light budget (the budget would
    /// set its intensity), no shadow. Use it instead of Kit.Lamp for a light you drive.</summary>
    public Light NewLight(Transform parent, string name, LightType type, Vector3 localPosition, Vector3 direction, float spot, float inner, float intensity, float range, Color colour)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        if (direction.sqrMagnitude > 1e-6f)
            go.transform.localRotation = Quaternion.LookRotation(direction.normalized, Mathf.Abs(direction.normalized.y) > .95f ? Vector3.forward : Vector3.up);
        var l = go.AddComponent<Light>();
        FrontRoomsMapWorld.ShippedShadowFilter(l);   // 关卡设计's rule since 11-04 14:18 (tech_art row 16): the shipped shadow filter
        l.type = type;
        if (type == LightType.Spot)
        {
            l.spotAngle = spot;
            l.innerSpotAngle = Mathf.Min(inner, spot);
        }
        l.color = colour;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
        l.shadowStrength = .9f;
        l.shadowNearPlane = .1f;
        return l;
    }

    // ------------------------------------------------------------ the look

    /// <summary>The scene's fog colour and density while set (applied after SP7's preset each frame; #3's near-black fog).</summary>
    public abstract void Fog(Color colour, float density);
    /// <summary>The preset's own fog again.</summary>
    public abstract void ClearFog();
    /// <summary>The fog the current preset sets (under any Fog override): the colour to fade from and back to.</summary>
    public abstract Color PresetFogColour { get; }
    public abstract float PresetFogDensity { get; }
    /// <summary>The scene's trilight ambient while set (gamma colours, as RenderSettings takes them), applied after SP7's preset
    /// each frame (#2's warm gap). It stays until ClearAmbient or the room resets.</summary>
    public abstract void Ambient(Color sky, Color equator, Color ground);
    /// <summary>The preset's own ambient again.</summary>
    public abstract void ClearAmbient();

    // ------------------------------------------------------------ the treatment's own things

    /// <summary>Start a kit part (the kit's boxes, walls, slabs, stand-ins and kits go into it; solid pieces make its one
    /// MeshCollider) under the treatment's Own root. One part at a time; EndPart finishes it.</summary>
    public abstract Transform BeginPart(FrontRoomsStructuresRunEnd end, string name);
    public abstract void EndPart();
    /// <summary>A plain GameObject under the treatment's Own root.</summary>
    public abstract Transform NewObject(FrontRoomsStructuresRunEnd end, string name);
}

/// <summary>
/// One treatment of SP7's end. The frame calls the hooks below on the active treatment only (the one X picked, in a receding
/// variant unless RunsInPlain). Every hook is wrapped: an exception logs once, the treatment is left out and the room runs
/// today's SP7. While a treatment is active today's schedule does not run (unless UsesTodayRecede): the treatment moves
/// the head itself (room.PlaceHead, ShowBetween, Seat, Leaving, Done, HeadMoving).
/// </summary>
public abstract class FrontRoomsStructuresRunEnd
{
    public const string DefaultPlain = "普通长度里不起作用，和今天一样；按 V 换到会退的 9→12 或 9→14 格。";

    /// <summary>1..5 (72 section 6's numbers).</summary>
    public abstract int Number { get; }
    /// <summary>72 section 6's name, in Chinese (as the plate and the sign show it).</summary>
    public abstract string Name { get; }
    /// <summary>One line on what the player should try (plate and sign; plain Chinese, about 40 characters, no " 2 " to
    /// " 5 " with spaces round a digit: the sign numbers its rows on them).</summary>
    public virtual string TryLine => "";
    /// <summary>What it does in the plain variants (V), for the plate.</summary>
    public virtual string PlainLine => DefaultPlain;
    /// <summary>N's line on the plate (Chinese, starting with "N" and a full-width colon), empty when the treatment has no option.</summary>
    public virtual string OptionLine => "";
    /// <summary>True only for #1: the frame runs today's SP7, no hook is called.</summary>
    public virtual bool IsToday => false;
    /// <summary>True: also active in the plain variants (the trip there is at the entrance: 1.5 m arch, 2.5 m door).</summary>
    public virtual bool RunsInPlain => false;
    /// <summary>True: today's schedule (gaze gate, flinch, steps, keep-away glide, seats) runs as well, and Seated / Done
    /// report its seats; the treatment layers on it.</summary>
    public virtual bool UsesTodayRecede => false;
    /// <summary>True: B (T2 / T1) means something here (the plate says so).</summary>
    public virtual bool UsesBody => false;
    /// <summary>True: the goal door opens wherever the head stands (when DoorReachable). Short of 42 m (scene 4 has its hall only
    /// there) a lit stand-in room behind it, the frame's (3 m deep, riding the head), stands in for the room beyond (72 3.4: "a
    /// player who never looks back meets a plain room"; C38: the door is never locked).</summary>
    public virtual bool OpensShort => false;

    /// <summary>The treatment's own root (under the district's root, at its origin): shown only while it is active.</summary>
    public Transform Own { get; private set; }

    /// <summary>The frame gives the treatment its root before Build.</summary>
    public void Bind(Transform own) { Own = own; }

    // ------------------------------------------------------------ hooks (all optional)

    /// <summary>Once, the first time the treatment becomes active: build its geometry, colliders, lights and materials under
    /// Own (room.BeginPart / EndPart, room.NewObject, room.NewLight; room.Own for materials and meshes).</summary>
    public virtual void Build(FrontRoomsStructuresRunEndRoom room) { }
    /// <summary>It becomes active (X, V back into a receding variant, entering SP7): once per activation, before Reset.</summary>
    public virtual void Enter(FrontRoomsStructuresRunEndRoom room) { }
    /// <summary>Back to armed, exactly: after every reset of the room (Y, V, X, entering or leaving SP7). The frame has put its
    /// own room back (head on the rest line, cells per the variant, bands off, magnets, doors closed, lamps released, fog
    /// cleared, every shared object as built); arrange the treatment's armed room here and re-arm its state.</summary>
    public virtual void Reset(FrontRoomsStructuresRunEndRoom room) { }
    /// <summary>It stops being active (X away, V into a plain variant, a fault): undo what the frame does not put back (its own
    /// objects hide with Own).</summary>
    public virtual void Exit(FrontRoomsStructuresRunEndRoom room) { }
    /// <summary>The alarm trips (the FD1 plane in a receding variant, the entrance otherwise or after Done; or T).</summary>
    public virtual void Trip(FrontRoomsStructuresRunEndRoom room) { }
    /// <summary>Every frame while active (armed, tripped, resetting), after the trip test and before the lamps are lit.</summary>
    public virtual void Tick(FrontRoomsStructuresRunEndRoom room, FrontRoomsStructuresRunEndFrame frame) { }
    /// <summary>A seat (room.Seat, or today's when UsesTodayRecede): the cells from one line to the other are open.</summary>
    public virtual void Seated(FrontRoomsStructuresRunEndRoom room, float from, float to) { }
    /// <summary>The final seat (room.Done, or today's when UsesTodayRecede).</summary>
    public virtual void Done(FrontRoomsStructuresRunEndRoom room) { }
    /// <summary>T while tripped: today's white re-strike walks from the entrance; every lamp hold was released just before.
    /// A recede already moving must finish (never stop midway).</summary>
    public virtual void ResetStarted(FrontRoomsStructuresRunEndRoom room) { }
    /// <summary>The re-strike ended: armed again where the end stands (Y puts it back).</summary>
    public virtual void Rearmed(FrontRoomsStructuresRunEndRoom room) { }
    /// <summary>The lines the head can stand on (the magnets show there). Default: the rest line and the final line.</summary>
    public virtual void Lines(FrontRoomsStructuresRunEndRoom room, List<float> lines)
    {
        lines.Add(room.RestLine);
        if (Mathf.Abs(room.FinalLine - room.RestLine) > .01f) lines.Add(room.FinalLine);
    }
    /// <summary>The goal door can be pushed now (scene 4 also needs the head at 42 m, where the hall is). Default: not while
    /// the end moves.</summary>
    public virtual bool DoorReachable(FrontRoomsStructuresRunEndRoom room) => !room.HeadMoving;
    /// <summary>The goal door's prompt (what E would do, or why it cannot here); the frame's comes in.</summary>
    public virtual string DoorPrompt(FrontRoomsStructuresRunEndRoom room, string today) => today;
    /// <summary>A metric mark's words (M) while this treatment is active; today's come in. i: 0 the arch's 1.5 m, 1 the Run
    /// door's 2.5 m, 2 FD1, 3 FD2 (today: the gaze gate's fallback), 4 21 m, 5 27 m, 6 36 m, 7 42 m.</summary>
    public virtual string Mark(FrontRoomsStructuresRunEndRoom room, int i, string today) => today;
    /// <summary>Strip i this frame: s comes in as today's drive (or the hold); change it to light the strip otherwise.</summary>
    public virtual void Strip(FrontRoomsStructuresRunEndRoom room, int i, ref FrontRoomsStructuresRunEndStrip s) { }
    /// <summary>Head lamp i this frame, as Strip.</summary>
    public virtual void Head(FrontRoomsStructuresRunEndRoom room, int i, ref FrontRoomsStructuresRunEndHead h) { }
    /// <summary>The scene look (ambient, reflections, grade) at a district point; today's comes in.</summary>
    public virtual FrontRoomsStructuresLook.Preset PresetAt(FrontRoomsStructuresRunEndRoom room, Vector3 local, FrontRoomsStructuresLook.Preset today) => today;
    /// <summary>N: the treatment's own option (say what it does in OptionLine). True when handled; message is flashed.</summary>
    public virtual bool Option(FrontRoomsStructuresRunEndRoom room, out string message) { message = null; return false; }
    /// <summary>A few words of state for the plate and the HUD (Chinese), or empty.</summary>
    public virtual string Status(FrontRoomsStructuresRunEndRoom room) => "";
    /// <summary>Lines for the G panel, after the frame's.</summary>
    public virtual void Guide(FrontRoomsStructuresRunEndRoom room, StringBuilder sb) { }
    /// <summary>The district is destroyed: free what Build made that room.Own and the kit do not.</summary>
    public virtual void Release(FrontRoomsStructuresRunEndRoom room) { }

    // ------------------------------------------------------------ helpers (the frame's own curves)

    public static float Smooth(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    /// <summary>The frame's trapezoidal travel: u 0..1 of the time, ramps of fraction r at both ends; returns 0..1 of the way
    /// (peak speed = length / (time x (1 - r))).</summary>
    public static float Trapezoid(float u, float r)
    {
        u = Mathf.Clamp01(u);
        r = Mathf.Clamp(r, .001f, .5f);
        var v = 1f / (1f - r);
        if (u < r) return .5f * v * u * u / r;
        if (u > 1f - r) return 1f - .5f * v * (1f - u) * (1f - u) / r;
        return .5f * v * r + v * (u - r);
    }

    /// <summary>Distance covered at time t of a move of a given length: a ramp up over accel s to a cruise speed, the cruise,
    /// a ramp down over brake s (both ramps linear in speed). Returns min(length, covered); done when t >= MoveTime.</summary>
    public static float Move(float t, float length, float cruise, float accel, float brake)
    {
        if (t <= 0f || length <= 0f) return 0f;
        cruise = Mathf.Max(.01f, cruise);
        accel = Mathf.Max(0f, accel);
        brake = Mathf.Max(0f, brake);
        var ramps = .5f * cruise * (accel + brake);
        if (ramps > length)
        {
            // Too short to reach the cruise: scale both ramps down to fit.
            var k = Mathf.Sqrt(length / ramps);
            cruise *= k;
            accel *= k;
            brake *= k;
            ramps = length;
        }
        var cruiseTime = (length - ramps) / cruise;
        if (t < accel) return .5f * cruise * t * t / Mathf.Max(1e-4f, accel);
        var d = .5f * cruise * accel;
        t -= accel;
        if (t < cruiseTime) return d + cruise * t;
        d += cruise * cruiseTime;
        t -= cruiseTime;
        if (t < brake) return d + cruise * t - .5f * cruise * t * t / Mathf.Max(1e-4f, brake);
        return length;
    }

    /// <summary>The time Move takes.</summary>
    public static float MoveTime(float length, float cruise, float accel, float brake)
    {
        if (length <= 0f) return 0f;
        cruise = Mathf.Max(.01f, cruise);
        accel = Mathf.Max(0f, accel);
        brake = Mathf.Max(0f, brake);
        var ramps = .5f * cruise * (accel + brake);
        if (ramps > length)
        {
            var k = Mathf.Sqrt(length / ramps);
            return (accel + brake) * k;
        }
        return accel + brake + (length - ramps) / cruise;
    }

    /// <summary>A fade time that obeys F: the effect's own time, or 0.5 s with Reduce Flashing (never shorter).</summary>
    public static float Fade(FrontRoomsStructuresRunEndRoom room, float own) =>
        room != null && room.Reduce ? Mathf.Max(.5f, own) : Mathf.Max(0f, own);

    static readonly Plane[] Frustum = new Plane[6];

    /// <summary>True when any part of these world bounds is inside the camera's view (a step must never be seen). Its planes are
    /// kept (no allocation: call it every frame).</summary>
    public static bool OnScreen(Camera view, Bounds world)
    {
        if (view == null) return true;
        GeometryUtility.CalculateFrustumPlanes(view, Frustum);
        return GeometryUtility.TestPlanesAABB(Frustum, world);
    }
}

/// <summary>#1 (72 section 6's first treatment), today's SP7 (71's M1 as agreed on 11-03): the frame runs its own code unchanged, no hook is called.</summary>
public sealed class FrontRoomsStructuresRunEndToday : FrontRoomsStructuresRunEnd
{
    public override int Number => 1;
    public override string Name => "机械后退（今天的）";
    public override bool IsToday => true;
    public override bool UsesBody => true;
    public override string TryLine => "冲过 FD1，看尽头的砖墙拆开、门往后退，再一层层砌回去。";
    public override string PlainLine => "普通长度：门不动，和今天一样。";
}
