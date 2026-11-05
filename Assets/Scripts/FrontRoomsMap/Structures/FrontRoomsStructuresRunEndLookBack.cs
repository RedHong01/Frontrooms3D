using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// SP7 treatment #4, DON'T LOOK BACK (research/level_space/72_unreachable_end.md section 3.4; Red 2026-11-04: make each of the
/// five in scene 4 to look at). The end never moves while it is seen. After the trip, each time the walker's view turns 90
/// degrees or more off the corridor's axis for 0.10 s (a look back at the hunter, or a hard look sideways) and nothing of the
/// end, of the cells it would uncover or of their light is on screen, the end steps back out of view in one frame: two cells
/// the first time, then one per look, up to the room's last line; the view must come back within 35 degrees of the axis
/// before the next look counts; it never steps once the walker is within 9 m of the door. The cells it uncovers open already
/// in the room's state (crimson under look B: no strike, no flash), the line it left keeps its ghost band, its magnets wait
/// at the new line. A walker who never looks back gets a plain Run!.
///
/// The tail (72: "in the first half-second they catch the tail"): the end wall the walker sees is a twin of the head's own
/// block wall (the same pieces, materials and world-planar UVs; the head's wall renderers are hidden while #4 runs, its
/// collider stays), so its top two courses (17 blocks on the frame's own bond) can come apart. At a step they are left out of
/// their beds, back toward where the wall stood: course 12's outer blocks queued turned along T2's two soffit-corner streams,
/// the top course's outer blocks further out along the corners in their own channels, the centre blocks (boxed in by the
/// tray, the strip and the main) hanging tipped forward under their beds. When the walker turns back (default: it waits for
/// the turn, at most 2 s; N: at once, 72's literal timing) they come home in 0.62 s, course 12 first and the top course
/// last, all moving from the first frame: the stream blocks peel off at 6 m/s and swing square into their beds, the corner
/// blocks shoot in and brake, the centre ones are lifted; the last one's knock sets off the end's settle: the sweeps press
/// home, the leaf rattles once in its frame, the panic bar bounces, the carried goal head nods on its bracket (its light pool
/// sways on the leaf and the floor; held still with Reduce Flashing), and mortar grit (the game's FrontRoomsRunGrit, lit by
/// the goal head) sifts from every landed joint and the soffit seam through the door light for 2.6 s. No block ever touches
/// a service, the goal head's bracket, another block or the walk volume (checked at 480 Hz: the builder's log).
///
/// Kept from 71 as agreed: the trip at FD1, the goal door never locked (C38: it only stays shut while the tail settles; short
/// of 42 m it opens onto the frame's lit stand-in room, OpensShort: "a player who never looks back meets a plain room"), no
/// EXIT sign, nothing reads the Relay (the rule reads the walker's camera only), no cut, no teleport, no FOV change, the floor
/// never moves, looks A/B/C and today's look untouched, the white re-strike from the entrance (T), the record until Y.
/// HeadMoving is set while the tail settles (B's pulses wait then, as for any moving end; the frame eases them out and back
/// over 0.4 s, and eases the restart at the final step's Done, so no dipping strip ever jumps).
/// Events for the sound line (a list only, nothing plays here, rule 24): StepRelease (two magnet clacks at the old line,
/// behind the walker), StepSeat (the heavy thud at the new line), StreamRun, CourseLand per block, MortarSift, Knock (the
/// last block), LeafRattle, PanicBar.
/// Scene 4 only (RULES 5b): nothing here is in the game scene, the Level Designer or the build.
/// </summary>
public sealed class FrontRoomsStructuresRunEndLookBack : FrontRoomsStructuresRunEnd
{
    // ---------------------------------------------------------------- identity

    public override int Number => 4;
    public override string Name => "别回头";
    public override string TryLine => "过了 FD1 盯着门，门不动；回头看一眼身后再转回来：门远了，最后几块砖正落回墙顶。";
    public override string OptionLine => tailAtStep
        ? "N：改回等你转回来才落砖；现在是退的同时就落（72 原文，转得快才看得到）"
        : "N：改成退的同时就落砖（72 原文）；现在等你转回来才落";
    public override string Status(FrontRoomsStructuresRunEndRoom room) => status;
    // The goal door opens wherever the end stands: short of 42 m onto the frame's lit stand-in room (72 3.4, C38).
    public override bool OpensShort => true;
    // FD2's mark: no gaze gate here (the end steps only on a look back).
    public override string Mark(FrontRoomsStructuresRunEndRoom room, int i, string today) => i == 3 ? "FD2 · 18 米" : today;

    // ---------------------------------------------------------------- the corridor's numbers (the frame's)

    const float HalfWidth = FrontRoomsStructuresRunEndRoom.HalfWidth, Ceiling = FrontRoomsStructuresRunEndRoom.Ceiling;
    const float CellLength = FrontRoomsStructuresRunEndRoom.CellLength, HeadTop = FrontRoomsStructuresRunEndRoom.HeadTop;
    const float HeadHalf = FrontRoomsStructuresRunEndRoom.HeadHalf, Band = FrontRoomsStructuresRunEndRoom.SlotBand;

    // ---------------------------------------------------------------- the rule (72 section 3.4; its section 5 lists these as proposed there, not agreed)

    /// <summary>72 3.4: "each time the camera turns 90 degrees or more off the corridor's axis for 0.10 s".</summary>
    const float LookDegrees = 90f, LookHold = .10f;
    /// <summary>72 3.4: "the view must come back within 35 degrees of the axis before the next look counts" (71 4.3's gaze gate).</summary>
    const float RearmDegrees = FrontRoomsStructuresRunEndRoom.GazeDegrees;
    /// <summary>72 3.4: "No step once the player is within 9 m of the door" (the door's line to the walker's feet).</summary>
    const float NoStepWithin = 9f;
    /// <summary>72 3.4: "two cells the first time, then one cell per look, up to R".</summary>
    const int FirstCells = 2, LaterCells = 1;
    /// <summary>What must be off screen for a step (district metres about the head's line; this builder's margin): from 4.5 m in
    /// front of the old line (the head with its goal head 1.2 m out, then the uncovered cells' lamps: the strips' 10 m range
    /// and the T-B heads aimed 3.8 m back add light there first) to 0.3 m past the new line.</summary>
    const float StepAhead = 4.5f, StepBeyond = .3f;

    // ---------------------------------------------------------------- the tail (72 3.4: "a 0.5 s tail clip ... the last course landing, the door's rattle, grit")

    /// <summary>The head wall's half thickness (room.HeadWallHalf: FrontRoomsStructuresRunRecede.BuildHead's t).</summary>
    const float WallT = FrontRoomsStructuresRunEndRoom.HeadWallHalf;
    /// <summary>The tail: the frame's 0.2 m courses 12 and 13 (BuildBlocks' rows from y 0; the last runs to HeadTop), 2.4 to 2.88.</summary>
    const float TailFrom = 2.4f, Row13 = 2.6f;
    /// <summary>The services the paths keep clear of (they run the corridor's length through the head's slots, RunRecede.cs
    /// Slots): the tray, the main, the strip's slot (its column ties the goal head's bracket into the wall and stays put).</summary>
    const float TrayX0 = -.84f, TrayX1 = -.36f, MainX0 = .47f, MainX1 = .63f, StripHalf = .14f;
    /// <summary>T2's soffit stream (RunRecede.cs AddBlock: the top courses ride at x +-0.95; here 0.90, so the widest block, 0.37 m,
    /// turns square in it without crossing the side slot at 1.10): course 12's outer blocks ride it turned along the lane at
    /// 2.48 m (1 cm under the top course's channel, 3 cm under the tray), leave it 0.5 m out and stand square 0.17 m in front
    /// of their beds before they go home; wall space (z 0 = the wall's centre plane, negative toward the walker). The sideways
    /// move (and the rise to the bed) waits for 30 % of the swing and the block is square by 80 %, so a block never turns its
    /// length into the tray, the side slot or another block (checked at 480 Hz against every service: the log).</summary>
    const float LaneX = .90f, LaneY = 2.48f, LaneExitZ = -.50f, PreSeatZ = -.17f, LaneSlideFrom = .3f, LaneSquareBy = .8f;
    /// <summary>The stream's speed: 71 4.5's keep-away glide, 6 m/s (T2's steps peak at 4.8-6.5 m/s); one block off a lane every
    /// 0.1 s (a block has left the lane exit before the next arrives: no two pieces ever meet).</summary>
    const float StreamSpeed = FrontRoomsStructuresRunEndRoom.GlideSpeed, StreamGap = .1f;
    /// <summary>A stream block's swing to the front of its bed (0.08 s plus 0.1 s a metre of sideways travel, leaving the lane at
    /// the stream's speed) and its push home.</summary>
    const float SwingBase = .08f, SwingPerMetre = .1f, InsertTime = .06f;
    /// <summary>The top course's outer blocks have no room to turn (each fills its channel between the tray or the main and the
    /// side slot exactly): they stream in square along their own soffit corner at 6 m/s and brake over the last 0.12 s.</summary>
    const float ColumnBrake = .12f;
    /// <summary>A centre block hangs this far under its bed, tipped forward, drifting from the settle's start (15 % of its rise,
    /// a quarter of its tip) and then lifted home in FrontTime in its own column; a 4 mm overshoot settles into the bed.</summary>
    const float FrontRise12 = .16f, FrontRise13 = .22f, FrontTime = .24f, FrontDrift = .15f, Overshoot = .004f;
    /// <summary>Small life in the stream (degrees) and the last rock in the bed (kept under 1 mm at a block's end).</summary>
    const float LaneWobble = 1f, Rock = .3f;
    /// <summary>By default the tail waits for the turn back: it settles once the view comes back inside 80 degrees (the end shows
    /// at about 59 + 9 degrees on a 19.5:9 screen), when any of the end comes on screen, or after 2 s at most.</summary>
    const float SettleDegrees = 80f, HangCap = 2f;
    /// <summary>The knock (the last block home): the leaf's rattle in its frame (0.4 degrees, 11 Hz), the panic bar's bounce (3 mm,
    /// 16 Hz), the goal head's nod on its 1.2 m bracket (1.6 degrees, 3.5 Hz, about the bracket's plate at y 2.65, z -1.2): all
    /// gone within 0.6 s (this builder's numbers, after 71 5.2's seat: "the door rattles once in its frame").</summary>
    const float LeafRattle = .4f, LeafHz = 11f, LeafDecay = .08f, BarBounce = .003f, BarHz = 16f, BarDecay = .06f;
    const float NodDegrees = 1.6f, NodHz = 3.5f, NodDecay = .18f, KnockRattle = .6f;
    static readonly Vector3 NodPivot = new Vector3(0f, 2.65f, -1.2f);
    /// <summary>The sweeps relaxed while the end moved, pressed when it seats (RunRecede.cs PlaceHead).</summary>
    static readonly Vector3 SweepMoving = new Vector3(.012f, HeadTop, .05f), SweepSeated = new Vector3(.02f, HeadTop, .06f);

    // ---------------------------------------------------------------- state

    enum Tail { Idle, Hanging, Settling }

    /// <summary>How a tail block comes home: off T2's soffit stream (course 12, outer), square along its own soffit corner (the
    /// top course, outer), or lifted in its own column (the centre, between the tray and the main).</summary>
    enum Path { Lane, Column, Front }

    sealed class TailBlock
    {
        public Transform part;      // its kit part (pivot at the district origin, the block built at its bed)
        public Vector3 seat, size;  // centre and size in wall space
        public int row;             // 0: course 12 (2.4-2.6), 1: course 13, the top one (2.6-2.88)
        public float side;          // -1 west, +1 east (FlyBlocks: x < 0 west)
        public Path path;
        public bool underService;   // the tray or the main hangs over its bed: no overshoot
        public float hangZ, start, arrive, swing, run, tip, land, wobble;
        public bool landed;
    }

    struct Spec
    {
        public float x0, x1, y0, y1;
        public int row;
    }

    readonly List<TailBlock> blocks = new List<TailBlock>();
    readonly List<TailBlock> sorting = new List<TailBlock>();
    readonly List<Vector2> spans = new List<Vector2>();
    Transform wall, twin, unit, bar;
    Vector3 unitPos, barPos;
    Quaternion unitRot = Quaternion.identity;
    FrontRoomsRunGrit grit;
    float gritClock;
    bool twinOn;
    // the rule
    bool lookArmed;
    float lookDwell;
    int steps;
    // the tail
    bool tailAtStep;            // N: 72's literal timing (the settle starts at the step)
    Tail tail = Tail.Idle;
    float tailClock, knock, settleEnd, hangDepth = 3.5f;
    bool knocked;
    // the plate / HUD (cached: the HUD reads it every frame)
    string status = "";
    int statusKey = -1;

    // ---------------------------------------------------------------- build (once)

    public override void Build(FrontRoomsStructuresRunEndRoom room)
    {
        wall = room.NewObject(this, "SP7 #4 end wall twin (rides the head; the head's own wall hides while #4 runs)");
        BuildWall(room);
        Choreograph();
        try { grit = FrontRoomsRunGrit.Create(wall); }
        catch (Exception e)
        {
            grit = null;
            FrontRoomsPoolMaterials.LogOnce("SP7 end 4 grit", "SP7 #4: the game's grit failed: " + e.Message);
        }
        if (grit == null) FrontRoomsPoolMaterials.LogOnce("SP7 end 4 grit missing", "SP7 #4: the game's grit (FrontRooms/RunGrit, compute) is not available here: the tail has no dust.");
        SeatAll();
    }

    // The twin: every piece of the head's wall (FrontRoomsStructuresRunRecede.BuildHead: two tones split at 1.2 m, the cove),
    // built at the district origin as the head was, so the kit's world-planar UVs land as on the head; the band pieces stop at
    // 2.4 m and their courses 12 and 13 are cut on the frame's running bond into blocks, each its own part (it can fly). The
    // strip slot's column (where the goal head's bracket ties in) stays whole; a sliver under 3 cm rides with the course below.
    void BuildWall(FrontRoomsStructuresRunEndRoom room)
    {
        var kit = room.Kit;
        var specs = new List<Spec>();
        twin = room.BeginPart(this, "SP7 #4 end wall twin: the head's block wall under its top two courses");
        kit.Shell(Ceiling, 0f);
        foreach (var r in room.HeadWallPieces)
        {
            var band = r.yMin >= Band - .001f;
            var tie = band && r.xMin > -StripHalf - .01f && r.xMax < StripHalf + .01f;
            var keep = band && !tie ? Mathf.Min(r.yMax, TailFrom) : r.yMax;
            WallBox(kit, room, r.xMin, r.yMin, r.xMax, keep);
            if (!band || tie || r.yMax <= TailFrom + .001f) continue;
            var top = r.yMax - Row13 >= .03f;
            Cut(specs, r.xMin, r.xMax, TailFrom, top ? Row13 : r.yMax, 0);
            if (top) Cut(specs, r.xMin, r.xMax, Row13, r.yMax, 1);
        }
        const float coveDepth = FrontRoomsStructuresRunEndRoom.HeadCoveDepth, coveTop = FrontRoomsStructuresRunEndRoom.HeadCoveTop;
        const float casing = FrontRoomsStructuresRunEndRoom.HeadCasingHalf;
        kit.Box(-HeadHalf, 0f, -WallT - coveDepth, -casing, coveTop, -WallT, room.Cove, false);
        kit.Box(casing, 0f, -WallT - coveDepth, HeadHalf, coveTop, -WallT, room.Cove, false);
        room.EndPart();
        twin.SetParent(wall, false);
        for (var i = 0; i < specs.Count; i++)
        {
            var s = specs[i];
            var part = room.BeginPart(this, "SP7 #4 tail block " + i + (s.row == 0 ? " (course 12)" : " (course 13, the top one)"));
            kit.Shell(Ceiling, 0f);
            kit.Box(s.x0, s.y0, -WallT, s.x1, s.y1, WallT, room.Upper, false);
            room.EndPart();
            part.SetParent(wall, false);
            var seat = new Vector3((s.x0 + s.x1) * .5f, (s.y0 + s.y1) * .5f, 0f);
            // The frame's per-block hash (RunRecede.cs AddBlock), so each block keeps its own small wobble.
            var hash = Mathf.Abs(Mathf.Sin(seat.x * 12.9898f + seat.y * 78.233f) * 43758.547f) % 1f;
            blocks.Add(new TailBlock
            {
                part = part, seat = seat, size = new Vector3(s.x1 - s.x0, s.y1 - s.y0, 2f * WallT), row = s.row,
                side = seat.x < 0f ? -1f : 1f, wobble = hash * 2f - 1f,
                underService = Overlaps(s.x0, s.x1, TrayX0, TrayX1) || Overlaps(s.x0, s.x1, MainX0, MainX1),
            });
        }
    }

    static bool Overlaps(float a0, float a1, float b0, float b1) => a1 > b0 + .001f && a0 < b1 - .001f;

    static void WallBox(FrontRoomsStructuresKit kit, FrontRoomsStructuresRunEndRoom room, float x0, float y0, float x1, float y1)
    {
        if (x1 - x0 < 1e-4f || y1 - y0 < 1e-4f) return;
        if (y0 < 1.2f - .001f) kit.Box(x0, y0, -WallT, x1, Mathf.Min(1.2f, y1), WallT, room.Lower, false);
        if (y1 > 1.2f + .001f) kit.Box(x0, Mathf.Max(1.2f, y0), -WallT, x1, y1, WallT, room.Upper, false);
    }

    // The frame's running bond (RunRecede.cs BuildBlocks): 0.4 m units from the wall's west edge, every other course offset
    // 0.2 m (course 12 even, 13 odd); no cut within 5 cm of a piece's edge.
    static void Cut(List<Spec> specs, float x0, float x1, float y0, float y1, int row)
    {
        var offset = row == 0 ? 0f : .2f;
        var a = x0;
        for (var c = -HeadHalf + offset; c < x1 - .05f; c += .4f)
        {
            if (c <= x0 + .05f) continue;
            specs.Add(new Spec { x0 = a, x1 = c, y0 = y0, y1 = y1, row = row });
            a = c;
        }
        specs.Add(new Spec { x0 = a, x1 = x1, y0 = y0, y1 = y1, row = row });
    }

    // Who comes home how and when. Off the stream: course 12's outer columns (west of the tray's far edge, so under it; east of
    // the main). Square along the soffit corner: the top course's outer columns (west of the tray, east of the main). Lifted:
    // the centre columns (a path from the stream would cross the tray or the main). Course 12 first, the top course last
    // (T2, 71 5.3: "bottom first ... the top course last into the seat"); in a lane and in a row the outer blocks first, so a
    // block always swings in front of empty beds. Every block is moving from the settle's first frame: nothing hangs still
    // once the end can be seen. The last one home (the knock) is the top course's west centre block, over the door.
    void Choreograph()
    {
        foreach (var b in blocks)
        {
            var x0 = b.seat.x - b.size.x * .5f;
            var x1 = b.seat.x + b.size.x * .5f;
            var outer = b.side < 0f ? x1 <= (b.row == 0 ? TrayX1 : TrayX0) + .001f : x0 >= MainX1 - .001f;
            b.path = !outer ? Path.Front : b.row == 0 ? Path.Lane : Path.Column;
        }
        Lane(-1f, 0f);
        Lane(1f, .05f);
        Front(0, .06f, .04f, -.28f);
        Columns(.52f, .04f);
        Front(1, .26f, .06f, -.66f);
        knock = 0f;
        hangDepth = .5f;
        foreach (var b in blocks)
        {
            knock = Mathf.Max(knock, b.land);
            hangDepth = Mathf.Max(hangDepth, .3f - b.hangZ);
        }
        settleEnd = knock + KnockRattle;
    }

    // One side's lane: the stream runs from the settle's first frame; block q reaches the lane exit at first + 0.1 q.
    void Lane(float side, float first)
    {
        Pick(Path.Lane, side, 0);
        for (var q = 0; q < sorting.Count; q++)
        {
            var b = sorting[q];
            b.start = 0f;
            b.arrive = first + q * StreamGap;
            b.hangZ = LaneExitZ - StreamSpeed * b.arrive;
            b.swing = SwingBase + SwingPerMetre * Mathf.Abs(b.seat.x - side * LaneX);
            b.land = b.arrive + b.swing + InsertTime;
        }
    }

    // The top course's outer blocks: at the stream's speed from the first frame, braking into the bed (Move, accel 0): the run
    // that lands each at its time.
    void Columns(float firstLand, float step)
    {
        Pick(Path.Column, 0f, 1);
        for (var j = 0; j < sorting.Count; j++)
        {
            var b = sorting[j];
            b.start = 0f;
            b.land = firstLand + j * step;
            b.run = StreamSpeed * (b.land - ColumnBrake) + .5f * StreamSpeed * ColumnBrake;
            b.hangZ = -b.run;
        }
    }

    void Front(int row, float start, float step, float hangZ)
    {
        Pick(Path.Front, 0f, row);
        for (var j = 0; j < sorting.Count; j++)
        {
            var b = sorting[j];
            b.start = start + j * step;
            b.land = b.start + FrontTime;
            // Staggered 4 cm in depth, tipped forward 12-20 degrees (the top toward the walker), each its own.
            b.hangZ = hangZ - .04f * (j % 2);
            b.tip = (row == 0 ? -12f : -16f) - 4f * b.wobble;
        }
    }

    // The blocks on one path (and side, for a lane) in one row, outer first.
    void Pick(Path path, float side, int row)
    {
        sorting.Clear();
        foreach (var b in blocks)
            if (b.path == path && b.row == row && (path != Path.Lane || b.side == side)) sorting.Add(b);
        sorting.Sort(OuterFirst);
    }

    static int OuterFirst(TailBlock a, TailBlock b) => Mathf.Abs(b.seat.x).CompareTo(Mathf.Abs(a.seat.x));

    // ---------------------------------------------------------------- the room's hooks

    public override void Reset(FrontRoomsStructuresRunEndRoom room)
    {
        // The frame has put its room back (head on the rest line, every shared object as built): take the rest poses of what
        // the knock moves, show the twin in place of the head's wall, re-arm.
        tail = Tail.Idle;
        SeatAll();
        FindHandles(room);
        SetTwin(room, true);
        if (wall != null) wall.localPosition = new Vector3(0f, 0f, room.HeadZ);
        // Any grain from before the reset is long dead (a grain lives 2.6 s).
        gritClock += 10f;
        if (grit != null) grit.SetClock(gritClock);
        steps = 0;
        lookArmed = true;
        lookDwell = 0f;
        status = "";
        statusKey = -1;
    }

    public override void Exit(FrontRoomsStructuresRunEndRoom room)
    {
        // The frame puts its own objects back after this; leave them as built anyway (a fault can bring us here mid-tail).
        tail = Tail.Idle;
        SeatAll();
        RestPoses(room);
        SetTwin(room, false);
    }

    public override void Trip(FrontRoomsStructuresRunEndRoom room)
    {
        lookDwell = 0f;
        statusKey = -1;
    }

    public override void Rearmed(FrontRoomsStructuresRunEndRoom room)
    {
        // Armed again where the end stands: the looks it has taken stay counted (R is the cap in all), Y puts it back.
        lookDwell = 0f;
        statusKey = -1;
    }

    public override void Lines(FrontRoomsStructuresRunEndRoom room, List<float> lines)
    {
        // 27, 33, 36 (R 9) or 27, 33, 36, 39, 42 (R 15): two cells, then one a look.
        var rest = room.RestLine;
        var final = room.FinalLine;
        lines.Add(rest);
        if (final - rest < .01f) return;
        var z = Mathf.Min(rest + FirstCells * CellLength, final);
        lines.Add(z);
        while (z < final - .01f)
        {
            z = Mathf.Min(z + LaterCells * CellLength, final);
            lines.Add(z);
        }
    }

    // The door stays shut only while its wall's top courses are out of their beds, up to the knock (C38: never locked
    // otherwise; the walker is 12 m off or more when that happens, the rattles after the knock never hold it).
    public override bool DoorReachable(FrontRoomsStructuresRunEndRoom room) =>
        !room.HeadMoving && (tail == Tail.Idle || (tail == Tail.Settling && tailClock >= knock));

    // The tail settling is the end moving (B's pulses wait, eased by the frame); set from the settle's start to the knock.
    static void Moving(FrontRoomsStructuresRunEndRoom room, bool on)
    {
        if (room.HeadMoving != on) room.HeadMoving = on;
    }

    public override bool Option(FrontRoomsStructuresRunEndRoom room, out string message)
    {
        tailAtStep = !tailAtStep;
        message = tailAtStep
            ? "最后几块砖：退的同时就落（72 原文，约半秒，转得快才看得到）"
            : "最后几块砖：等你转回来才落（最多等 2 秒）";
        room.Dirty();
        return true;
    }

    public override void Guide(FrontRoomsStructuresRunEndRoom room, StringBuilder sb)
    {
        sb.AppendLine("4 别回头的规则：过了 FD1，视线离开走廊方向 90 度以上（回头看身后，或者狠狠看向一边）持续 0.1 秒，而且门、要露出来的格子和它们的灯光都不在画面里，门就趁你不看退一步：第一次两格，以后每次一格，退到底为止；视线回到离门 35 度以内，下一次才算；离门 9 米以内不再退；从不回头，就是一间普通的 Run!。新露出来的格子直接是当时的红色，不闪；它站过的地方留下墙上、地上的印子。");
        sb.AppendLine("转回来时：墙顶最后两层砖还在回家，外侧的顺着天花角落的砖流滑进来，中间的从下面抬回去；最后一块落定，门在框里一震、推杠一跳、门上方的应急灯在支架上点一下头（光斑在门上和地上晃一下，减少闪烁时不晃），灰从砖缝和墙顶落下来，被门灯照亮。N 换\"等你转回来才落\"（默认，最多等 2 秒）和\"退的同时就落\"（72 原文）。");
        sb.AppendLine("场景 4 没有声音：游戏里每次退会在背后响两声咔哒和一声闷响。门在哪儿都推得开（游戏里门永远不锁）：停在 42 米时门后是大厅，停在 27 到 39 米时门后是一间亮着的占位小房间，代替游戏里的下一个房间（场景 4 的大厅只在 42 米）。");
    }

    // ---------------------------------------------------------------- every frame

    public override void Tick(FrontRoomsStructuresRunEndRoom room, FrontRoomsStructuresRunEndFrame frame)
    {
        gritClock += frame.dt;
        if (grit != null) grit.SetClock(gritClock);
        var z = room.HeadZ;
        if (wall != null && Mathf.Abs(wall.localPosition.z - z) > 1e-4f) wall.localPosition = new Vector3(0f, 0f, z);
        if (frame.phase == FrontRoomsStructuresRunEndPhase.Tripped) Watch(room, frame);
        else lookDwell = 0f;
        if (tail != Tail.Idle) Advance(room, frame);
        Restatus(room, frame);
    }

    // The look back: a turn past 90 degrees held 0.1 s, armed by a look within 35 degrees, never within 9 m of the door, and
    // only when nothing of the end, the uncovered cells or their light is on screen (else the look goes on and it waits).
    void Watch(FrontRoomsStructuresRunEndRoom room, FrontRoomsStructuresRunEndFrame frame)
    {
        if (!lookArmed && frame.axisAngle < RearmDegrees) lookArmed = true;
        lookDwell = frame.axisAngle >= LookDegrees ? lookDwell + frame.dt : 0f;
        if (!lookArmed || lookDwell < LookHold) return;
        var from = room.HeadZ;
        var final = room.FinalLine;
        if (from >= final - .01f) return;
        if (from - frame.feet.z < NoStepWithin) return;
        var to = Mathf.Min(from + (steps == 0 ? FirstCells : LaterCells) * CellLength, final);
        if (OnScreen(room.View, Volume(room, from - StepAhead, to + StepBeyond))) return;
        Step(room, from, to);
    }

    // One step, in one frame, unseen: the record, the head on its new line, the cells open in the room's state (no strike), the
    // twin and the grit with it, the tail left out; at the last line the final seat.
    void Step(FrontRoomsStructuresRunEndRoom room, float from, float to)
    {
        FinishTail(room);
        // A door left open at the old line goes shut with the step (unseen: the walker is 9 m off or more, looking away).
        if (room.GoalOpen) room.CloseGoal();
        room.Leaving(from);
        room.PlaceHead(to, true);
        if (wall != null) wall.localPosition = new Vector3(0f, 0f, to);
        room.Seat(from, to, false);
        steps++;
        lookArmed = false;
        lookDwell = 0f;
        // The final seat first (the frame eases B's pulse schedule restart), then the tail (it may set HeadMoving at once).
        if (to >= room.FinalLine - .01f) room.Done();
        StartTail(room);
        statusKey = -1;
    }

    // ---------------------------------------------------------------- the tail

    void StartTail(FrontRoomsStructuresRunEndRoom room)
    {
        if (!twinOn || blocks.Count == 0)
        {
            room.PlaceHead(room.HeadZ, false);
            return;
        }
        foreach (var b in blocks)
        {
            b.landed = false;
            PoseBlock(b, 0f);
        }
        knocked = false;
        tailClock = 0f;
        tail = tailAtStep ? Tail.Settling : Tail.Hanging;
        Moving(room, tail == Tail.Settling);
    }

    void Advance(FrontRoomsStructuresRunEndRoom room, FrontRoomsStructuresRunEndFrame frame)
    {
        tailClock += frame.dt;
        if (tail == Tail.Hanging)
        {
            // Waiting for the turn back (it never moves seen: it starts just before the end can show).
            if (frame.axisAngle >= SettleDegrees && tailClock < HangCap && !OnScreen(room.View, Volume(room, room.HeadZ - hangDepth, room.HeadZ + .25f))) return;
            tail = Tail.Settling;
            tailClock = 0f;
            Moving(room, true);
        }
        var t = tailClock;
        foreach (var b in blocks)
        {
            PoseBlock(b, t);
            if (b.landed || t < b.land) continue;
            b.landed = true;
            GritBed(b);
        }
        if (t >= knock)
        {
            if (!knocked)
            {
                knocked = true;
                Moving(room, false);
                GritSoffit();
            }
            Knock(room, t - knock);
        }
        if (t >= settleEnd) FinishTail(room);
    }

    void FinishTail(FrontRoomsStructuresRunEndRoom room)
    {
        if (tail == Tail.Idle) return;
        Moving(room, false);
        tail = Tail.Idle;
        SeatAll();
        RestPoses(room);
    }

    // A block's pose at time t of the settle (t 0: as left out at the step): its centre p and turn r, wall space.
    void PoseBlock(TailBlock b, float t)
    {
        if (b.part == null) return;
        Vector3 p;
        Quaternion r;
        if (t >= b.land) Landed(b, t, out p, out r);
        else if (b.path == Path.Lane) LanePose(b, t, out p, out r);
        else if (b.path == Path.Column) ColumnPose(b, t, out p, out r);
        else FrontPose(b, t, out p, out r);
        b.part.localRotation = r;
        b.part.localPosition = p - r * b.seat;
    }

    // Off the stream: riding the lane turned along it at the stream's speed, then the swing to just in front of its bed (it
    // leaves the lane at the stream's speed and slows to a stand, moves sideways only after 30 %, square by 80 %), the push home.
    void LanePose(TailBlock b, float t, out Vector3 p, out Quaternion r)
    {
        var turned = 90f * b.side;
        var exit = new Vector3(b.side * LaneX, LaneY, LaneExitZ);
        var pre = new Vector3(b.seat.x, b.seat.y, PreSeatZ);
        if (t < b.arrive)
        {
            p = new Vector3(exit.x, exit.y, Mathf.Min(LaneExitZ, b.hangZ + StreamSpeed * t));
            r = Quaternion.Euler(LaneWobble * b.wobble, turned + 3f * b.wobble, .7f * LaneWobble * b.wobble);
            return;
        }
        var u = t - b.arrive;
        if (u < b.swing)
        {
            var k = u / b.swing;
            // z leaves at about the stream's speed (2k - k^2: twice the mean speed at k 0) and stands at the bed's front.
            var kz = k * (2f - k);
            var kx = Smooth((k - LaneSlideFrom) / (1f - LaneSlideFrom));
            var turn = 1f - Smooth(k / LaneSquareBy);
            p = new Vector3(Mathf.Lerp(exit.x, pre.x, kx), Mathf.Lerp(exit.y, pre.y, kx), Mathf.Lerp(exit.z, pre.z, kz));
            r = Quaternion.Euler(LaneWobble * b.wobble * turn, turned * turn + 3f * b.wobble * Mathf.Sin(Mathf.PI * k), .7f * LaneWobble * b.wobble * turn);
            return;
        }
        u -= b.swing;
        p = new Vector3(b.seat.x, b.seat.y, Mathf.Lerp(PreSeatZ, 0f, Smooth(u / InsertTime)));
        r = Quaternion.identity;
    }

    // The top course's outer blocks: square in their own channel along the soffit corner, at the stream's speed, braking home.
    void ColumnPose(TailBlock b, float t, out Vector3 p, out Quaternion r)
    {
        p = new Vector3(b.seat.x, b.seat.y, Mathf.Min(0f, b.hangZ + Move(t, b.run, StreamSpeed, 0f, ColumnBrake)));
        r = Quaternion.identity;
    }

    // In its own column: hanging tipped forward under its bed and drifting from the first frame (15 % of the rise, a quarter
    // of the tip), then lifted to the bed (course 12 by 60 %, the top course by 70 %) while it closes on the wall (from 40 %
    // and 25 %: never into the course under it), square before it reaches its bed's height (a tipped block is taller: the
    // main over P6 leaves no headroom).
    void FrontPose(TailBlock b, float t, out Vector3 p, out Quaternion r)
    {
        var rise = b.row == 0 ? FrontRise12 : FrontRise13;
        var over = b.underService ? 0f : Overshoot;
        if (t < b.start)
        {
            var d = b.start > .001f ? Smooth(t / b.start) : 1f;
            p = new Vector3(b.seat.x, b.seat.y - rise * (1f - FrontDrift * d), b.hangZ);
            r = Quaternion.Euler(b.tip * (1f - .25f * d), 0f, 0f);
            return;
        }
        var k = (t - b.start) / FrontTime;
        var closeFrom = b.row == 0 ? .4f : .25f;
        var ky = Smooth(k / (b.row == 0 ? .6f : .7f));
        var kz = Smooth((k - closeFrom) / (1f - closeFrom));
        var y0 = b.seat.y - rise * (1f - FrontDrift);
        p = new Vector3(b.seat.x, y0 + (b.seat.y + over - y0) * ky, Mathf.Lerp(b.hangZ, 0f, kz));
        r = Quaternion.Euler(.75f * b.tip * (1f - Smooth(k / (b.row == 0 ? .55f : .65f))), 0f, 0f);
    }

    // Home: a front block lets its 4 mm overshoot settle into the bed; every block a last rock under a millimetre.
    void Landed(TailBlock b, float t, out Vector3 p, out Quaternion r)
    {
        var tl = t - b.land;
        var dy = b.path == Path.Front && !b.underService ? Overshoot * (1f - Smooth(tl / .05f)) : 0f;
        p = b.seat + new Vector3(0f, dy, 0f);
        var rock = tl < .25f ? Rock * b.wobble * Mathf.Exp(-tl / .05f) * Mathf.Sin(2f * Mathf.PI * 14f * tl) : 0f;
        r = Quaternion.Euler(0f, rock, .5f * rock);
    }

    // The last block home: the sweeps press, the leaf rattles in its frame, the panic bar bounces, the goal head nods on its
    // bracket (its light moves: held still with Reduce Flashing).
    void Knock(FrontRoomsStructuresRunEndRoom room, float tk)
    {
        var sw = room.Sweeps;
        var ks = Smooth(tk / .08f);
        for (var i = 0; i < sw.Count; i++)
            if (sw[i] != null) sw[i].localScale = Vector3.Lerp(SweepMoving, SweepSeated, ks);
        var leaf = room.GoalLeaf;
        if (leaf != null && !room.GoalOpen)
            leaf.localRotation = Quaternion.Euler(0f, LeafRattle * Mathf.Exp(-tk / LeafDecay) * Mathf.Sin(2f * Mathf.PI * LeafHz * tk), 0f);
        if (bar != null) bar.localPosition = barPos + new Vector3(0f, BarBounce * Mathf.Exp(-tk / BarDecay) * Mathf.Sin(2f * Mathf.PI * BarHz * tk), 0f);
        if (unit != null)
        {
            // The bracket stops hard: the unit's foot swings toward the wall first (negative about x: the bottom to +z).
            var a = room.Reduce ? 0f : -NodDegrees * Mathf.Exp(-tk / NodDecay) * Mathf.Sin(2f * Mathf.PI * NodHz * tk);
            var q = Quaternion.AngleAxis(a, Vector3.right);
            unit.localRotation = q * unitRot;
            unit.localPosition = NodPivot + q * (unitPos - NodPivot);
        }
    }

    // Mortar grit from a landed block's bed and head joints (the game's FrontRoomsRunGrit: written once, moved and lit on the
    // GPU, lit by the goal head, gone within 2.6 s), and from the soffit seam over the wall at the knock.
    void GritBed(TailBlock b)
    {
        if (grit == null || wall == null) return;
        spans.Clear();
        spans.Add(new Vector2(b.seat.x - b.size.x * .5f, b.seat.x + b.size.x * .5f));
        grit.EmitCourse(wall, spans, b.seat.y - b.size.y * .5f, b.size.y, gritClock, Vector3.zero);
    }

    void GritSoffit()
    {
        if (grit == null || wall == null) return;
        spans.Clear();
        spans.Add(new Vector2(-1.10f, TrayX0));
        spans.Add(new Vector2(TrayX1, -StripHalf));
        spans.Add(new Vector2(StripHalf, MainX0));
        spans.Add(new Vector2(MainX1, 1.10f));
        grit.EmitCourse(wall, spans, HeadTop - .02f, .02f, gritClock, Vector3.zero);
    }

    void SeatAll()
    {
        foreach (var b in blocks)
        {
            b.landed = true;
            if (b.part == null) continue;
            b.part.localPosition = Vector3.zero;
            b.part.localRotation = Quaternion.identity;
        }
    }

    // The head's moving bits at rest (as the frame built them).
    void RestPoses(FrontRoomsStructuresRunEndRoom room)
    {
        var sw = room.Sweeps;
        for (var i = 0; i < sw.Count; i++) if (sw[i] != null) sw[i].localScale = SweepSeated;
        var leaf = room.GoalLeaf;
        if (leaf != null && !room.GoalOpen) leaf.localRotation = Quaternion.identity;
        if (bar != null) bar.localPosition = barPos;
        if (unit != null)
        {
            unit.localPosition = unitPos;
            unit.localRotation = unitRot;
        }
    }

    // The goal head's unit and the panic bar (room.GoalUnit, room.PanicBar), with their rest poses (the frame put them back).
    void FindHandles(FrontRoomsStructuresRunEndRoom room)
    {
        unit = room.GoalUnit;
        bar = room.PanicBar;
        if (unit != null)
        {
            unitPos = unit.localPosition;
            unitRot = unit.localRotation;
        }
        if (bar != null) barPos = bar.localPosition;
    }

    // The twin in place of the head's own wall (its renderers off, its collider kept), or the head's wall back.
    void SetTwin(FrontRoomsStructuresRunEndRoom room, bool on)
    {
        var hw = room.HeadWall;
        on &= wall != null && twin != null && hw != null && hw.Count > 0;
        if (hw != null)
            for (var i = 0; i < hw.Count; i++)
                if (hw[i] != null) hw[i].enabled = !on;
        if (wall != null) wall.gameObject.SetActive(on);
        twinOn = on;
    }

    // A box across the corridor's section between two lines (district metres), in world space.
    static Bounds Volume(FrontRoomsStructuresRunEndRoom room, float z0, float z1)
    {
        var centre = new Vector3(0f, Ceiling * .5f, (z0 + z1) * .5f);
        return new Bounds(room.World(centre), new Vector3(2f * HalfWidth + .1f, Ceiling + .1f, Mathf.Max(.01f, z1 - z0)));
    }

    // The plate's and the HUD's few words (rebuilt only when they change).
    void Restatus(FrontRoomsStructuresRunEndRoom room, FrontRoomsStructuresRunEndFrame frame)
    {
        var back = Mathf.Max(0, Mathf.RoundToInt((room.HeadZ - room.RestLine) / CellLength));
        var cap = room.HeadZ >= room.FinalLine - .01f;
        var tripped = frame.phase == FrontRoomsStructuresRunEndPhase.Tripped;
        var near = tripped && !cap && room.HeadZ - frame.feet.z < NoStepWithin;
        var key = back * 8 + (cap ? 4 : 0) + (near ? 2 : 0) + (tripped ? 1 : 0);
        if (key == statusKey) return;
        statusKey = key;
        if (cap) status = "退到底了（" + back + " 格）";
        else if (back > 0) status = "已退 " + back + " 格" + (near ? "，9 米内不再退" : "");
        else if (tripped) status = near ? "9 米内不再退" : "等你回头";
        else status = "";
        room.Dirty();
    }
}
