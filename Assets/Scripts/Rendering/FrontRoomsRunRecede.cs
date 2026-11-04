using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>The receding Run!'s body (71 s.5.2-s.5.6, decision 5). Its timings drive the one schedule; the head (T1) serves both.
/// T2 (re-stack, desktop and iPad; phones its half-course strips) is the default everywhere the head carries it, so desktop, iPhone and
/// iPad keep one timing (three-platform parity, s.4.8 item 12); WebGL runs the head alone on T1's timings.</summary>
public enum RunRecedeBody : byte { T2Restack, T1Roll }

/// <summary>Where the receding end is in its one recede: Armed (before the trip), Waiting (tripped, the gaze gate shut), then per step
/// Tell (the magnets let go, the jolt), Travel, Seat (the thud, the overshoot), Dwell (between steps), and Seated (the final seat: nothing
/// in the room moves again this visit).</summary>
public enum RunRecedePhase : byte { Armed, Waiting, Tell, Travel, Seat, Dwell, Seated }

/// <summary>
/// Events for 声音设计 (71 s.5.10, T1; AUDIO_CONTRACT: the rig raises C# events, nothing is played here, rule 24). Each carries an index
/// and a world point (FrontRoomsRunRecede.Cue):
///   MagnetRelease  the hold-open magnets let go, two clacks 30 ms apart (index 0 left, 1 right; at each holder);
///   Unlatch        the end's jolt toward the player (index the step; at the end);
///   TrolleyRoll    the trolleys start to roll on the overhead track (index the step; at the end's top);
///   SweepDrag      the rubber sweeps drag on the walls and floor while it moves (index the step);
///   Whump          the pressure of the space growing as it travels (index the step; at the end);
///   Seat           a seat: the thud, the door and its hardware rattling once in the frame (index the step);
///   FinalSeat      the final seat: a heavier bolt-home (index the step);
///   Grit           grit from the soffit seam at the tell and the floor seam at a seat (index 0 soffit, 1 floor);
///   CellUncovered  a cell of the reserved length comes on behind the end at the tell (index the cell from the end's first line);
///   StripStrike    at the seat, an uncovered cell's strip strikes once in the room's state (71 s.5.7): tripped, its red tube rises on the
///                  battery circuit (card zone_lamps 1, landed 11-04); armed, its white tubes (index the cell; at the strip).
/// T2 adds (the re-stack, FrontRoomsRunRestack, from its clips' contact lists; desktop, iPad and phones alike; never in a glide):
///   MortarCrack    the mortar cracks as the wall lets go, at the tell (index 0; at the wall);
///   SubframeSlide  the door's steel rolls back alone, steel on steel, as the travel starts (index 0; at the sill);
///   BlockLift      a course lifts out of its beds (index the course, 0 at the floor; at its first piece);
///   StreamClatter  the two streams of blocks along the walls, block on block (index 0 left, 1 right; at each stream);
///   CourseLand     a course lands back in its beds, a thud (index the course; at the course; the top course's is the last beat);
///   MortarSift     mortar grit sifts from that course's joints (index the course; the grit is drawn on desktop only).
/// </summary>
public enum RunRecedeCue : byte { MagnetRelease, Unlatch, TrolleyRoll, SweepDrag, Whump, Seat, FinalSeat, Grit, CellUncovered, StripStrike,
    MortarCrack, SubframeSlide, BlockLift, StreamClatter, CourseLand, MortarSift }

/// <summary>
/// A receding room's record, 8 bytes (71 s.4.6 "the record stays", the door break's 16-byte record idea, 50_rethink s.3.9): kept by the
/// map with the room; a rebuilt room is rebuilt from it (Restore), never replayed in view (a rebuild happens 48 m away or more).
/// </summary>
[Serializable]
public struct RunRecedeRecord
{
    public const byte Version1 = 1;
    public const byte Tripped = 1, GateOpen = 2, Gliding = 4, Seated = 8;
    public byte version;
    public byte flags;
    /// <summary>Steps seated.</summary>
    public byte steps;
    /// <summary>The step the keep-away glide took over at (255: none).</summary>
    public byte glideFrom;
    /// <summary>The gate's opening and the glide's start, seconds after the trip, as halves.</summary>
    public ushort gateTime, glideTime;

    public bool Started => (flags & GateOpen) != 0;
    public bool Done => (flags & Seated) != 0;
}

/// <summary>
/// The stepped schedule of one recede (71 s.4.5, motion.txt R5), from one trigger time, pure arithmetic: the same inputs give the same
/// end, every frame and every platform. R = 3k m in whole cells: k = 1 the final step alone; k = 2 the flinch alone, seated heavily;
/// k &gt;= 3 a two-cell flinch, k - 3 one-cell steps and a one-cell final step. A step is tell + travel + seat (+ a dwell between steps).
/// T2 timings: tells 0.25 (first, final) / 0.15; travels 1.1 (flinch) / 0.8 / 1.0 (final); seats 0.2 / 0.35 (final); dwell 0.2: R 9 seats
/// at 1.55 and 3.35 s, R 15 at 1.55, 2.90, 4.25 and 6.05 s. T1: travels 0.75 / 0.5 / 0.9, seats 0.15 / 0.35: R 15 at 1.15, 2.15, 3.15 and
/// 4.85 s. Travel: accelerate 0.20 s, cruise, decelerate 0.15 s (peak = length / (travel - 0.175)). The keep-away glide (L9): one move to
/// the final line at a 6 m/s peak after a 0.1 s tell, then the heavy seat.
/// </summary>
public sealed class RunRecedeSchedule
{
    public const float Accel = .20f, Decel = .15f;
    public const float GlideSpeed = 6f, GlideTell = .10f;
    /// <summary>The tell's jolt toward the player (m) and the seat's overshoot past the line (m): 15 mm, 30 mm, 35 mm on the final seat.</summary>
    public const float Jolt = .015f, Overshoot = .030f, FinalOvershoot = .035f;
    /// <summary>The second magnet's clack after the first (s).</summary>
    public const float SecondClack = .03f;

    public readonly float From, To, Cell;
    public readonly RunRecedeBody Body;
    readonly float[] start, tell, travel, seat, dwell, a, b;
    readonly bool[] final;

    public int Steps => start.Length;
    /// <summary>From the gate's opening to the final seat's end (s), with no glide.</summary>
    public float Duration { get; }

    public RunRecedeSchedule(float from, float to, RunRecedeBody body, float cell = 3f)
    {
        From = from; To = to; Body = body; Cell = cell;
        var k = Mathf.Max(0, Mathf.RoundToInt((to - from) / cell));
        var cells = new System.Collections.Generic.List<int>();
        if (k == 1) cells.Add(1);
        else if (k == 2) cells.Add(2);
        else if (k >= 3)
        {
            cells.Add(2);
            for (var i = 0; i < k - 3; i++) cells.Add(1);
            cells.Add(1);
        }
        var n = cells.Count;
        start = new float[n]; tell = new float[n]; travel = new float[n]; seat = new float[n]; dwell = new float[n];
        a = new float[n]; b = new float[n]; final = new bool[n];
        var t = 0f;
        var line = from;
        var t2 = body == RunRecedeBody.T2Restack;
        for (var i = 0; i < n; i++)
        {
            var fin = i == n - 1;
            var flinch = cells[i] == 2;
            final[i] = fin;
            tell[i] = i == 0 || fin ? .25f : .15f;
            travel[i] = t2 ? (flinch ? 1.1f : fin ? 1.0f : .8f) : (flinch ? .75f : fin ? .9f : .5f);
            seat[i] = fin ? .35f : t2 ? .2f : .15f;
            dwell[i] = fin ? 0f : .2f;
            start[i] = t;
            a[i] = line;
            line += cells[i] * cell;
            b[i] = line;
            t += tell[i] + travel[i] + seat[i] + dwell[i];
        }
        Duration = t;
    }

    public float StepStart(int i) => start[i];
    public float StepFrom(int i) => a[i];
    public float StepTo(int i) => b[i];
    public bool StepFinal(int i) => final[i];
    /// <summary>When step i's seat ends (s after the gate), the time 71 s.4.5 quotes.</summary>
    public float SeatedAt(int i) => start[i] + tell[i] + travel[i] + seat[i];
    /// <summary>Step i's phases (s) and its length in cells (2: the flinch), for the T2 body's clips.</summary>
    public float TellOf(int i) => tell[i];
    public float TravelOf(int i) => travel[i];
    public float SeatOf(int i) => seat[i];
    public int CellsOf(int i) => Mathf.RoundToInt((b[i] - a[i]) / Cell);

    /// <summary>The distance covered after time u of a travel of <paramref name="total"/> s over <paramref name="length"/> m (trapezoid).</summary>
    public static float TravelAt(float u, float total, float length)
    {
        if (length <= 0f || total <= 0f) return 0f;
        u = Mathf.Clamp(u, 0f, total);
        var v = length / Mathf.Max(.01f, total - (Accel + Decel) * .5f);
        if (u <= Accel) return v * u * u / (2f * Accel);
        if (u <= total - Decel) return v * Accel * .5f + v * (u - Accel);
        var r = total - u;
        return length - v * r * r / (2f * Decel);
    }

    /// <summary>The travel time of the keep-away glide over <paramref name="length"/> m at its peak speed.</summary>
    public static float GlideTravel(float length) => length / GlideSpeed + (Accel + Decel) * .5f;

    /// <summary>The tell's jolt at time u into it (negative: toward the player): out in 0.04 s, back by 0.14 s.</summary>
    public static float JoltAt(float u, float scale = 1f)
    {
        if (u < 0f) return 0f;
        if (u < .04f) return -Jolt * scale * Mathf.Sin(Mathf.PI * .5f * u / .04f);
        if (u < .14f) return -Jolt * scale * Mathf.Cos(Mathf.PI * .5f * (u - .04f) / .10f);
        return 0f;
    }

    /// <summary>The seat's overshoot at time u into a seat of d seconds: past the line and back in 45 % of it, a small rebound after.</summary>
    public static float OvershootAt(float u, float d, bool heavy)
    {
        if (u < 0f || u >= d) return 0f;
        var amp = heavy ? FinalOvershoot : Overshoot;
        var k = u / d;
        if (k < .45f) return amp * Mathf.Sin(Mathf.PI * k / .45f);
        return -.25f * amp * Mathf.Sin(Mathf.PI * (k - .45f) / .55f);
    }

    /// <summary>The end's place at time t after the gate (no glide): its line, the jolt or overshoot on it, the phase and the step.</summary>
    public void Sample(float t, out float line, out float offset, out RunRecedePhase phase, out int step, out float local)
    {
        offset = 0f;
        if (Steps == 0) { line = From; phase = RunRecedePhase.Seated; step = -1; local = 0f; return; }
        if (t < 0f) { line = From; phase = RunRecedePhase.Waiting; step = -1; local = 0f; return; }
        for (var i = 0; i < Steps; i++)
        {
            var u = t - start[i];
            if (u < 0f) break;
            step = i;
            if (u < tell[i]) { line = a[i]; offset = JoltAt(u); phase = RunRecedePhase.Tell; local = u; return; }
            u -= tell[i];
            if (u < travel[i]) { line = a[i] + TravelAt(u, travel[i], b[i] - a[i]); phase = RunRecedePhase.Travel; local = u; return; }
            u -= travel[i];
            if (u < seat[i]) { line = b[i]; offset = OvershootAt(u, seat[i], final[i]); phase = RunRecedePhase.Seat; local = u; return; }
            u -= seat[i];
            if (!final[i] && u < dwell[i]) { line = b[i]; phase = RunRecedePhase.Dwell; local = u; return; }
            if (final[i]) { line = b[i]; phase = RunRecedePhase.Seated; local = u; return; }
        }
        // between the last dwell and the next step's start cannot happen (they abut); past the end:
        step = Steps - 1; line = To; phase = RunRecedePhase.Seated; local = 0f;
    }

    /// <summary>The keep-away glide from <paramref name="from"/> at time u after it began: tell, one travel, the heavy seat.</summary>
    public void SampleGlide(float u, float from, out float line, out float offset, out RunRecedePhase phase, out float local)
    {
        offset = 0f;
        var length = To - from;
        var tr = GlideTravel(length);
        if (u < GlideTell) { line = from; offset = JoltAt(u, .6f); phase = RunRecedePhase.Tell; local = u; return; }
        u -= GlideTell;
        if (u < tr) { line = from + TravelAt(u, tr, length); phase = RunRecedePhase.Travel; local = u; return; }
        u -= tr;
        if (u < .35f) { line = To; offset = OvershootAt(u, .35f, true); phase = RunRecedePhase.Seat; local = u; return; }
        line = To; phase = RunRecedePhase.Seated; local = u - .35f;
    }
}

/// <summary>
/// The receding end of a Run! room (71_run_room.md s.4-s.5, decisions 4 and 5; card 2, "the recede, head first"). A new structure (rule
/// 5b): built only for a layout from RunRigLayout.Receding, which nothing in today's game asks for; it enters the game when Red has walked
/// SP7 in scene 4 and said so, through 关卡设计's map (W/apply/run_rethink_contract/README.md, "card 2").
///
/// The head (s.5.1): the end wall and the goal door made one movable assembly (Kit_RunRecede_Head, its running gear Kit_RunRecede_HeadGear,
/// its sweeps in two poses), hung from trolleys in surface-mounted tracks in both soffit corners from the entrance wall to the deepest
/// line (the honest tell, s.4.8 item 1: s.5.1 runs it from FD1, but FD1's header hides the soffit beyond 9 m from the arch, so it starts
/// at the entrance to be read before commit), guided in two 20 mm floor slots, held at each line by wall magnets (Kit_RunMagnetHolder). It carries the goal door
/// (DoorMount: the map hangs its RN-F there), the twin-head goal unit and C42 (the rig's GoalMount; the head is 灯具适配's T-A unit on our
/// mount). Ghost bands (cleaner paint, unworn wax: the *_Ghost twins of the corridor's finishes) mark every line it left.
///
/// The recede (s.4.3-s.4.6): one timeline from one trigger time. Trip() (the map, at the FD1 plane, 9 m) arms it; the gaze gate opens when
/// the camera faces the goal within 35 degrees, or the player passes two thirds of the first length (18 m); then RunRecedeSchedule runs,
/// and between steps, if the player is within 12 m of the end, the rest runs as one 6 m/s glide (the keep-away, L9). It reads the player's
/// position and gaze only, never the Relay (rule 11, s.4.7). Each step: the magnets let go and the sweeps relax at the tell, the cells it
/// uncovers are switched on behind the end (their props here, their strips in the rig), the room's light gate moves to the step's line, the
/// sweeps press at the seat. At the final seat the door looks onto the release (the rig's C42 crossfades as built). Never a second recede.
/// Render-only: the map owns the goal point, colliders, walk volume and graph per seat (SeatReached), and the record (Record / Restore).
/// </summary>
[DisallowMultipleComponent]
public sealed class FrontRoomsRunRecede : MonoBehaviour
{
    public const string HeadKit = "Kit_RunRecede_Head", GearKit = "Kit_RunRecede_HeadGear", WallKit = "Kit_RunRecede_Wall";
    public const string SweepSeatedKit = "Kit_RunRecede_SweepSeated", SweepMovingKit = "Kit_RunRecede_SweepMoving";
    public const string TrackKit = "Kit_RunRecede_Track_3m", TrackStopKit = "Kit_RunRecede_TrackStop", SlotKit = "Kit_RunRecede_FloorSlot_3m";
    public const string HolderKit = "Kit_RunMagnetHolder";
    /// <summary>The track lines (soffit corners), the floor slots, the magnets' height and place ahead of a line (Kit_RunRecede_Head).</summary>
    public const float TrackX = 1.25f, SlotX = .95f, MagnetY = 1.95f, MagnetAhead = .125f;
    /// <summary>The gaze gate (s.4.3: Portal 2's rule) and its fallback at two thirds of the first length; the keep-away distance (L9).</summary>
    public const float GazeAngle = 35f, FallbackShare = 2f / 3f, KeepAway = 12f;
    /// <summary>A ghost band's depth along the corridor (s.4.6: 200 mm where the end stood).</summary>
    public const float GhostDepth = .20f;

    public RunRigLayout Layout { get; private set; }
    public FrontRoomsRunRig Rig { get; private set; }
    public RunRecedeSchedule Schedule { get; private set; }
    /// <summary>The moving end (its kits, the door mount); localPosition.z is its line plus the jolt or overshoot.</summary>
    public Transform Head { get; private set; }
    /// <summary>Where the map hangs the goal door's frame and leaf rig: the hinge side of the opening, local +X into the corridor (the
    /// door kits' S face), local +Z along the wall from the hinge to the latch (FrontRoomsRunDress.SideDoors' convention).</summary>
    public Transform DoorMount { get; private set; }
    public RunRecedePhase Phase { get; private set; } = RunRecedePhase.Armed;
    /// <summary>The step under way or last seated (-1 before the first).</summary>
    public int Step { get; private set; } = -1;
    public int StepsDone { get; private set; }
    /// <summary>The end's line now (rig z), and the line it last seated on.</summary>
    public float Line { get; private set; }
    public float SeatLine { get; private set; }
    public bool Gliding => glideTime >= 0f;
    public bool Seated => Phase == RunRecedePhase.Seated;
    /// <summary>The trip line (the FD1 plane, a third of the first length) and the gate's fallback line (two thirds of it), rig z.</summary>
    public float TripLine => Layout.recedeLine / 3f;
    public float FallbackLine => Layout.recedeLine * FallbackShare;
    /// <summary>Seconds since Trip (-1 before).</summary>
    public float Clock => clock;

    /// <summary>For 声音设计 (RunRecedeCue): the cue, its index and the world point. Nothing is played (AUDIO_CONTRACT, rule 24).</summary>
    public event Action<RunRecedeCue, int, Vector3> Cue;
    /// <summary>The T2 body (desktop and iPad blocks, phone strips); null on WebGL (T1) or when its kit is missing.</summary>
    public FrontRoomsRunRestack Restack => restack;
    /// <summary>The end's at-rest block wall (Kit_RunRecede_Wall): hidden while a T2 step flies.</summary>
    public GameObject WallSkins => wallSkins;
    /// <summary>The end's world velocity at the last Advance (the T2 grit leaves with it).</summary>
    public Vector3 HeadVelocity { get; private set; }

    /// <summary>The T2 body's cues go out through the recede's one event.</summary>
    internal void RaiseCue(RunRecedeCue cue, int index, Vector3 at) => Cue?.Invoke(cue, index, at);
    /// <summary>For the map: a seat (step, its line in rig z, final). The map moves the goal point and normal and opens the uncovered cells'
    /// colliders, walk volume and graph edges (contract, 关卡设计 4).</summary>
    public event Action<int, float, bool> SeatReached;
    /// <summary>For the map: cell c of the reserved length (from the end's first line) is being uncovered (its props and strip are on).</summary>
    public event Action<int> CellUncovered;

    Transform viewer;
    float clock = -1f, gateTime = -1f, glideTime = -1f, glideFrom;
    int glideStep = -1;
    bool magnetTwo;
    GameObject sweepSeated, sweepMoving, wallSkins;
    FrontRoomsRunRestack restack;
    int restackStep = -1;
    float lastAdvance = -1f;
    GameObject[] cells = new GameObject[0];
    GameObject[] bands = new GameObject[0];
    float[] bandLines = new float[0];
    int revealedCells, struckCells;
    bool quiet;

    // ---------------------------------------------------------------- construction
    /// <summary>
    /// Build the receding end's parts under <paramref name="dressRoot"/> (FrontRoomsRunDress.Build's root) BEFORE the rig is created, so the
    /// rig takes them into its ambient and light gate: the head at the first line, the tracks, slots, magnets and ghost bands, and the
    /// reserved length's props regrouped per cell and switched off. Then Bind(rig). Null (and nothing built) for a plain layout.
    /// </summary>
    public static FrontRoomsRunRecede Build(Transform dressRoot, RunRigLayout layout)
    {
        if (dressRoot == null || layout == null || layout.recedeLine < 0f || layout.length <= layout.recedeLine + .5f) return null;
        var root = new GameObject("run b recede").transform;
        root.SetParent(dressRoot, false);
        var r = root.gameObject.AddComponent<FrontRoomsRunRecede>();
        r.Layout = layout;
        r.Schedule = new RunRecedeSchedule(layout.recedeLine, layout.length,
            FrontRoomsRunRig.WebGLTier ? RunRecedeBody.T1Roll : RunRecedeBody.T2Restack, RunRigLayout.CellLength);
        r.Line = r.SeatLine = layout.recedeLine;
        r.GroupHiddenProps(dressRoot);
        r.BuildHead(root);
        r.BuildTrack(root);
        r.BuildSlotsAndHolders(root);
        r.BuildGhostBands(dressRoot, root);
        return r;
    }

    /// <summary>Hook the rig built after Build: its goal mount rides the end; C42 at full while the door looks onto the hidden length.</summary>
    public void Bind(FrontRoomsRunRig rig)
    {
        Rig = rig;
        if (rig == null) return;
        rig.GoalBeyondHidden = true;
        rig.SetGoalLine(Layout.recedeLine);
        Place(Line, 0f);
    }

    public void SetViewer(Transform t) => viewer = t;

    static GameObject Kit(string kit, Transform parent, Vector3 position, Quaternion rotation, string label, Vector3? scale = null)
    {
        var go = FrontRoomsKitLibrary.Spawn(kit, parent, position, rotation, scale, false, label, false);
        if (go == null)
        {
            go = new GameObject(label + " (missing " + kit + ")");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
        }
        return go;
    }

    void BuildHead(Transform root)
    {
        Head = new GameObject("run b recede end").transform;
        Head.SetParent(root, false);
        Head.localPosition = new Vector3(0f, 0f, Layout.recedeLine);
        Kit(HeadKit, Head, Vector3.zero, Quaternion.identity, "run b recede end steel");
        // T2 body (card 2): the block field is its own static kit, hidden while a step re-stacks it (FrontRoomsRunRestack); on
        // WebGL (T1) and in the keep-away glide it simply rides with the end
        wallSkins = Kit(WallKit, Head, Vector3.zero, Quaternion.identity, "run b recede end wall");
        if (Schedule.Body == RunRecedeBody.T2Restack && !FrontRoomsRunRig.WebGLTier)
            restack = FrontRoomsRunRestack.Build(this, Head, wallSkins);
        var gear = Kit(GearKit, Head, Vector3.zero, Quaternion.identity, "run b recede end gear");
        foreach (var rr in gear.GetComponentsInChildren<Renderer>(true)) rr.shadowCastingMode = ShadowCastingMode.Off;
        sweepSeated = Kit(SweepSeatedKit, Head, Vector3.zero, Quaternion.identity, "run b recede sweeps seated");
        sweepMoving = Kit(SweepMovingKit, Head, Vector3.zero, Quaternion.identity, "run b recede sweeps moving");
        foreach (var rr in sweepSeated.GetComponentsInChildren<Renderer>(true)) rr.shadowCastingMode = ShadowCastingMode.Off;
        foreach (var rr in sweepMoving.GetComponentsInChildren<Renderer>(true)) rr.shadowCastingMode = ShadowCastingMode.Off;
        sweepMoving.SetActive(false);
        DoorMount = new GameObject("run b recede door mount (the map's RN-F)").transform;
        DoorMount.SetParent(Head, false);
        DoorMount.localPosition = new Vector3(Layout.goalPoint.x - Layout.goalHalf, 0f, 0f);
        DoorMount.localRotation = Quaternion.Euler(0f, 90f, 0f);
    }

    /// <summary>The tracks (both soffit corners) from the entrance wall's face to the deepest line's wall face, through the compartment
    /// frames, in 3 m sections (the last one cut), an end stop at each end. The honest tell (71 s.4.8 item 1) read from the arch: s.5.1
    /// starts it at FD1, but FD1's header (2.1 m) hides the soffit beyond 9 m from the arch's eye (the harness's track-tell stills), so it
    /// runs from the entrance. Render-only, no shadows (as the corridor's services).</summary>
    void BuildTrack(Transform root)
    {
        var t = new GameObject("run b recede track").transform;
        t.SetParent(root, false);
        var wh = Layout.WallHalf;
        var z0 = wh;
        var z1 = Layout.length - wh;
        for (var s = -1; s <= 1; s += 2)
        {
            var x = s * TrackX;
            var z = z0;
            while (z < z1 - .01f)
            {
                var len = Mathf.Min(3f, z1 - z);
                Kit(TrackKit, t, new Vector3(x, Layout.height, z), Quaternion.identity, "run b recede track", len < 2.999f ? new Vector3(1f, 1f, len / 3f) : (Vector3?)null);
                z += len;
            }
            Kit(TrackStopKit, t, new Vector3(x, Layout.height, z0), Quaternion.identity, "run b recede track stop");
            Kit(TrackStopKit, t, new Vector3(x, Layout.height, z1), Quaternion.Euler(0f, 180f, 0f), "run b recede track stop");
        }
        foreach (var rr in t.GetComponentsInChildren<Renderer>(true)) rr.shadowCastingMode = ShadowCastingMode.Off;
    }

    /// <summary>The floor slots (the end's first line to the deepest) and a pair of magnets on every line the end can seat on; the ones
    /// in the reserved length go into their cells (on with the cell).</summary>
    void BuildSlotsAndHolders(Transform root)
    {
        var l0 = Layout.recedeLine;
        for (var s = -1; s <= 1; s += 2)
            for (var z = l0; z < Layout.length - .01f; z += 3f)
                Kit(SlotKit, CellRoot(z + .01f, root), new Vector3(s * SlotX, 0f, z), Quaternion.identity, "run b recede floor slot");
        for (var z = l0; z <= Layout.length + .01f; z += 3f)
        {
            var parent = z <= l0 + .01f ? root : CellRoot(z - .05f, root);
            Kit(HolderKit, parent, new Vector3(Layout.halfWidth, MagnetY, z - MagnetAhead), Quaternion.LookRotation(Vector3.left, Vector3.up), "run b recede magnet R");
            Kit(HolderKit, parent, new Vector3(-Layout.halfWidth, MagnetY, z - MagnetAhead), Quaternion.LookRotation(Vector3.right, Vector3.up), "run b recede magnet L");
        }
    }

    /// <summary>The cell holder for rig z (cells from the end's first line), or <paramref name="fallback"/> short of it.</summary>
    Transform CellRoot(float z, Transform fallback)
    {
        var c = Mathf.FloorToInt((z - Layout.recedeLine) / RunRigLayout.CellLength);
        return c >= 0 && c < cells.Length ? cells[c].transform : fallback;
    }

    /// <summary>The reserved length's props (services, rails, the end wall at the deepest line), each by its first cell, under per-cell
    /// holders, off until the end uncovers the cell. The finishes that run the whole length (floor, walls, soffit) stay: through the lite
    /// they show the uncovered length lit by C42, and they cost a few renderers.</summary>
    void GroupHiddenProps(Transform dressRoot)
    {
        var l0 = Layout.recedeLine;
        var n = Mathf.Max(0, Mathf.RoundToInt((Layout.length - l0) / RunRigLayout.CellLength));
        var holder = new GameObject("run b recede hidden cells").transform;
        holder.SetParent(dressRoot, false);
        cells = new GameObject[n];
        for (var c = 0; c < n; c++)
        {
            cells[c] = new GameObject("run b recede cell " + c + " (z " + (l0 + 3f * c).ToString("0") + ")");
            cells[c].transform.SetParent(holder, false);
        }
        var candidates = new System.Collections.Generic.List<Transform>();
        foreach (Transform child in dressRoot)
        {
            if (child == holder || child.GetComponent<FrontRoomsRunRecede>() != null) continue;
            if (child.name == "run b overhead services")
            {
                foreach (Transform g in child) candidates.Add(g);
                continue;
            }
            candidates.Add(child);
        }
        foreach (var t in candidates)
        {
            if (!LocalBounds(dressRoot, t, out var min, out var max)) continue;
            if (min.z < l0 + .02f || max.z > Layout.length + .5f) continue;
            var c = Mathf.Clamp(Mathf.FloorToInt((min.z - l0) / RunRigLayout.CellLength), 0, n - 1);
            t.SetParent(cells[c].transform, true);
        }
        for (var c = 0; c < n; c++) cells[c].SetActive(false);
    }

    /// <summary>A transform's renderers' bounds in <paramref name="space"/> (the dress root: right for a rotated room too).</summary>
    static bool LocalBounds(Transform space, Transform t, out Vector3 min, out Vector3 max)
    {
        min = Vector3.positiveInfinity; max = Vector3.negativeInfinity;
        var any = false;
        foreach (var r in t.GetComponentsInChildren<MeshRenderer>(true))
        {
            var f = r.GetComponent<MeshFilter>();
            if (f == null || f.sharedMesh == null) continue;
            var b = f.sharedMesh.bounds;
            for (var i = 0; i < 8; i++)
            {
                var p = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                var q = space.InverseTransformPoint(r.transform.TransformPoint(p));
                min = Vector3.Min(min, q);
                max = Vector3.Max(max, q);
            }
            any = true;
        }
        return any;
    }

    static readonly string[] GhostOf = { "RunB_BlockLower", "RunB_BlockUpper", "RunB_Soffit", "RunB_Floor", "RunB_FloorLane", "RunB_FloorAccent", "RunB_FloorWax" };

    /// <summary>
    /// Ghost bands (s.4.6, s.5.2): a 0.2 m band of the corridor's own finish in its *_Ghost twin (less macro dirt and grime; the wax
    /// unworn: glossier) on both walls, the soffit and the floor, 1 mm proud, at every line the end will leave: its first line (on now,
    /// under the end) and each seat but the last (on when the end leaves it). Pre-built slabs, no decals (D2).
    /// </summary>
    void BuildGhostBands(Transform dressRoot, Transform root)
    {
        var lines = new System.Collections.Generic.List<float> { Layout.recedeLine };
        for (var i = 0; i < Schedule.Steps - 1; i++) lines.Add(Schedule.StepTo(i));
        bandLines = lines.ToArray();
        bands = new GameObject[bandLines.Length];
        var sources = new System.Collections.Generic.List<MeshRenderer>();
        foreach (var r in dressRoot.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (r.transform.IsChildOf(root)) continue;
            var m = r.sharedMaterial;
            if (m == null || Array.IndexOf(GhostOf, BaseName(m.name)) < 0) continue;
            sources.Add(r);
        }
        for (var b = 0; b < bandLines.Length; b++)
        {
            var z = bandLines[b];
            var band = new GameObject("run b recede ghost band z " + z.ToString("0.0"));
            band.transform.SetParent(root, false);
            bands[b] = band;
            foreach (var r in sources)
            {
                if (!LocalBounds(dressRoot, r.transform, out var min, out var max)) continue;
                if (min.z > z - GhostDepth * .5f || max.z < z + GhostDepth * .5f) continue;
                var twin = FrontRoomsSurfaces.TryGet(BaseName(r.sharedMaterial.name) + "_Ghost");
                if (twin == null) continue;
                Vector3 c, size;
                var dx = max.x - min.x; var dy = max.y - min.y;
                if (dy < .05f && min.y < .5f)            // floor slab: its top + 1 mm
                {
                    c = new Vector3((min.x + max.x) * .5f, max.y + .0005f, z);
                    size = new Vector3(dx, .001f, GhostDepth);
                }
                else if (dy < .2f && min.y > 2f)        // soffit slab: its underside - 1 mm
                {
                    c = new Vector3(0f, min.y - .0005f, z);
                    size = new Vector3(Mathf.Min(dx, 2f * Layout.halfWidth), .001f, GhostDepth);
                }
                else if (dx < .2f)                      // a long wall's slab: its room face + 1 mm
                {
                    var right = (min.x + max.x) > 0f;
                    var face = right ? min.x : max.x;
                    c = new Vector3(face + (right ? -.0005f : .0005f), (min.y + max.y) * .5f, z);
                    size = new Vector3(.001f, dy, GhostDepth);
                }
                else continue;
                var go = new GameObject("run b recede ghost " + BaseName(r.sharedMaterial.name));
                go.transform.SetParent(band.transform, false);
                go.transform.position = dressRoot.TransformPoint(c);
                go.transform.rotation = dressRoot.rotation;
                go.AddComponent<MeshFilter>().sharedMesh = FrontRoomsFilmMesh.GetPlanarBox(size);
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = twin;
                mr.shadowCastingMode = ShadowCastingMode.Off;
            }
            band.SetActive(b == 0);
        }
    }

    /// <summary>A material's surface name without the gate's twin suffix or an instance suffix.</summary>
    static string BaseName(string name)
    {
        var i = name.IndexOf(" (", StringComparison.Ordinal);
        if (i > 0) name = name.Substring(0, i);
        return name;
    }

    // ---------------------------------------------------------------- the recede
    /// <summary>The map's trip of a receding room (the FD1 plane): the recede's clock starts; the gaze gate decides the first step. Once.</summary>
    public void Trip()
    {
        if (clock >= 0f || Phase == RunRecedePhase.Seated) return;
        clock = 0f;
        Phase = RunRecedePhase.Waiting;
    }

    /// <summary>Advance (the binder's tick, or a tool's). Reads the viewer's position and gaze only. No allocation.</summary>
    public void Tick(float dt)
    {
        if (clock < 0f) return;
        if (Phase == RunRecedePhase.Seated)
        {
            // nothing moves again this visit; only the last course's grit is still falling (at most 2.6 s)
            if (restack != null && restack.Grit != null && restack.Grit.Alive > 0)
            {
                clock += Mathf.Clamp(dt, 0f, .1f);
                restack.Grit.SetClock(clock);
            }
            return;
        }
        clock += Mathf.Clamp(dt, 0f, .1f);
        if (gateTime < 0f)
        {
            if (!GateOpen()) return;
            gateTime = clock;
            if (ViewerGap() < KeepAway) StartGlide(0f, Layout.recedeLine, -1);
        }
        Advance(clock - gateTime);
    }

    /// <summary>The gaze gate (s.4.3): the camera faces the goal within GazeAngle, or the viewer has passed the fallback line; no viewer: open.</summary>
    bool GateOpen()
    {
        if (viewer == null || Rig == null) return true;
        var rt = Rig.transform;
        var p = rt.InverseTransformPoint(viewer.position);
        if (p.z >= FallbackLine) return true;
        var f = rt.InverseTransformDirection(viewer.forward);
        var goal = new Vector3(Layout.goalPoint.x, Layout.goalPoint.y, Line);
        return Vector3.Angle(f, goal - p) <= GazeAngle;
    }

    /// <summary>The viewer's distance ahead of the end along the corridor (m); infinity with no viewer.</summary>
    float ViewerGap()
    {
        if (viewer == null || Rig == null) return float.PositiveInfinity;
        return Line - Rig.transform.InverseTransformPoint(viewer.position).z;
    }

    void StartGlide(float t, float from, int step)
    {
        glideTime = t;
        glideFrom = from;
        glideStep = step;
    }

    /// <summary>Deterministic: the end at time t after the gate (the glide, if any, as recorded). Tools and tests may call it directly.</summary>
    public void Advance(float t)
    {
        float line, offset, local;
        RunRecedePhase phase;
        int step;
        if (glideTime >= 0f)
        {
            Schedule.SampleGlide(t - glideTime, glideFrom, out line, out offset, out phase, out local);
            step = glideStep + 1;
        }
        else
        {
            Schedule.Sample(t, out line, out offset, out phase, out step, out local);
            // L9: between steps, a viewer within KeepAway of the end turns the rest into the glide (from this seat, now).
            if (phase == RunRecedePhase.Dwell && ViewerGap() < KeepAway)
            {
                StartGlide(t, Schedule.StepTo(step), step);
                Schedule.SampleGlide(0f, glideFrom, out line, out offset, out phase, out local);
                step = glideStep + 1;
            }
        }
        Transition(phase, step, local);
        var dt = lastAdvance >= 0f ? t - lastAdvance : 0f;
        var axis = Rig != null ? Rig.transform.forward : transform.forward;
        HeadVelocity = dt > 1e-4f ? axis * ((line - Line) / dt) : Vector3.zero;
        lastAdvance = t;
        Line = line;
        Place(line, offset);
        DriveRestack(phase, step, local);
    }

    void Transition(RunRecedePhase phase, int step, float local)
    {
        var was = Phase;
        var wasStep = Step;
        Phase = phase;
        Step = step;
        var at = Head != null ? Head.position : transform.position;
        if (phase == RunRecedePhase.Tell && (was != RunRecedePhase.Tell || wasStep != step))
        {
            // The magnets let go (two clacks), the jolt, grit from the soffit seam; the cells this step uncovers come on behind the end;
            // the light gate moves to the line it is bound for; the line it leaves shows its ghost band.
            Cue?.Invoke(RunRecedeCue.MagnetRelease, 0, MagnetPoint(-1f));
            Cue?.Invoke(RunRecedeCue.Unlatch, step, at);
            Cue?.Invoke(RunRecedeCue.Grit, 0, at + Vector3.up * Layout.height);
            magnetTwo = false;
            var to = Gliding ? Layout.length : Schedule.StepTo(Mathf.Max(0, step));
            Uncover(to);
            if (Rig != null) Rig.SetGoalLine(to);
            ShowBand(SeatLine);
        }
        if (phase == RunRecedePhase.Tell && !magnetTwo && local >= RunRecedeSchedule.SecondClack)
        {
            magnetTwo = true;
            Cue?.Invoke(RunRecedeCue.MagnetRelease, 1, MagnetPoint(1f));
            SetSweeps(false);
        }
        if (phase == RunRecedePhase.Travel && was != RunRecedePhase.Travel)
        {
            if (!magnetTwo) { magnetTwo = true; Cue?.Invoke(RunRecedeCue.MagnetRelease, 1, MagnetPoint(1f)); }
            SetSweeps(false);
            Cue?.Invoke(RunRecedeCue.TrolleyRoll, step, at + Vector3.up * Layout.height);
            Cue?.Invoke(RunRecedeCue.SweepDrag, step, at);
            Cue?.Invoke(RunRecedeCue.Whump, step, at);
        }
        if ((phase == RunRecedePhase.Seat || phase == RunRecedePhase.Seated) && was != RunRecedePhase.Seat && was != RunRecedePhase.Seated)
        {
            var final = Gliding || Schedule.StepFinal(Mathf.Max(0, step));
            SetSweeps(true);
            StepsDone = Mathf.Max(StepsDone, step + 1);
            SeatLine = final ? Layout.length : Schedule.StepTo(Mathf.Max(0, step));
            Cue?.Invoke(final ? RunRecedeCue.FinalSeat : RunRecedeCue.Seat, step, at);
            Cue?.Invoke(RunRecedeCue.Grit, 1, at);
            // The strips the end uncovered strike once now, in the room's state (71 s.5.7: a one-way rise at the seat).
            for (var c = struckCells; c < revealedCells; c++)
                Cue?.Invoke(RunRecedeCue.StripStrike, c, StripPoint(c));
            struckCells = revealedCells;
            if (Rig != null) Rig.StrikeRevealed();
            if (final && Rig != null) Rig.GoalBeyondHidden = false;
            SeatReached?.Invoke(step, SeatLine, final);
        }
    }

    /// <summary>T2: a step (never the glide) re-stacks the wall: the body from the tell to the end of the seat, the static wall
    /// otherwise. The clip is warped onto the step's own phases (the final step's 0.25 / 1.0 / 0.35 s on the step clip).</summary>
    void DriveRestack(RunRecedePhase phase, int step, float local)
    {
        if (restack == null) return;
        if (restack.Grit != null) restack.Grit.SetClock(clock >= 0f ? clock : 0f);
        var flying = !Gliding && step >= 0 && step < Schedule.Steps
            && (phase == RunRecedePhase.Tell || phase == RunRecedePhase.Travel || phase == RunRecedePhase.Seat);
        if (!flying)
        {
            restack.End();
            restackStep = -1;
            return;
        }
        if (!restack.Active || restackStep != step)
        {
            restack.Begin(Schedule.CellsOf(step), Schedule.TellOf(step), Schedule.TravelOf(step), Schedule.SeatOf(step));
            restackStep = step;
        }
        restack.Sample(phase, local);
    }

    Vector3 MagnetPoint(float side)
    {
        var p = new Vector3(side * Layout.halfWidth, MagnetY, SeatLine - MagnetAhead);
        return Rig != null ? Rig.transform.TransformPoint(p) : transform.TransformPoint(p);
    }

    /// <summary>The world point of cell c's strip: the corridor's strips stand 1.8 m into each cell (z 1.8 + 3k).</summary>
    Vector3 StripPoint(int c)
    {
        var z = Layout.recedeLine + RunRigLayout.CellLength * c + 1.8f;
        var p = new Vector3(0f, Layout.height, z);
        return Rig != null ? Rig.transform.TransformPoint(p) : transform.TransformPoint(p);
    }

    void Uncover(float to)
    {
        while (revealedCells < cells.Length && Layout.recedeLine + RunRigLayout.CellLength * revealedCells < to - .01f)
        {
            cells[revealedCells].SetActive(true);
            if (!quiet)
            {
                CellUncovered?.Invoke(revealedCells);
                Cue?.Invoke(RunRecedeCue.CellUncovered, revealedCells, Head != null ? Head.position : transform.position);
            }
            revealedCells++;
        }
        if (Rig != null) Rig.RevealTo(to);
    }

    void ShowBand(float line)
    {
        for (var b = 0; b < bandLines.Length; b++)
            if (Mathf.Abs(bandLines[b] - line) < .05f && bands[b] != null) bands[b].SetActive(true);
    }

    void SetSweeps(bool seated)
    {
        if (sweepSeated != null && sweepSeated.activeSelf != seated) sweepSeated.SetActive(seated);
        if (sweepMoving != null && sweepMoving.activeSelf == seated) sweepMoving.SetActive(!seated);
    }

    void Place(float line, float offset)
    {
        if (Head != null) Head.localPosition = new Vector3(0f, 0f, line + offset);
        if (Rig != null && Rig.GoalMount != null) Rig.GoalMount.localPosition = new Vector3(0f, 0f, line + offset - Layout.recedeLine);
    }

    // ---------------------------------------------------------------- the record
    /// <summary>The 8-byte record of this room's recede (the map keeps it with the room).</summary>
    public RunRecedeRecord Record
    {
        get
        {
            var flags = (byte)((clock >= 0f ? RunRecedeRecord.Tripped : 0) | (gateTime >= 0f ? RunRecedeRecord.GateOpen : 0)
                | (glideTime >= 0f ? RunRecedeRecord.Gliding : 0) | (Phase == RunRecedePhase.Seated ? RunRecedeRecord.Seated : 0));
            return new RunRecedeRecord
            {
                version = RunRecedeRecord.Version1, flags = flags, steps = (byte)Mathf.Clamp(StepsDone, 0, 255),
                glideFrom = (byte)(glideTime >= 0f ? Mathf.Clamp(glideStep + 1, 0, 254) : 255),
                gateTime = Mathf.FloatToHalf(Mathf.Max(0f, gateTime)), glideTime = Mathf.FloatToHalf(Mathf.Max(0f, glideTime)),
            };
        }
    }

    /// <summary>
    /// A room rebuilt from its record (out of view, 48 m away or more): a recede that had begun is rebuilt seated at the deepest line (its
    /// cells on, its bands shown, the sweeps pressed, the door onto the release), with no event; one that had not begun stays at its first
    /// line (tripped or armed is the rig's state, the map's to restore).
    /// </summary>
    public void Restore(RunRecedeRecord record)
    {
        if (record.version != RunRecedeRecord.Version1 || !record.Started) return;
        clock = 0f;
        gateTime = 0f;
        glideTime = -1f;
        quiet = true;
        Uncover(Layout.length);
        quiet = false;
        struckCells = revealedCells;
        if (Rig != null) Rig.StrikeRevealed();
        for (var b = 0; b < bands.Length; b++) if (bands[b] != null) bands[b].SetActive(true);
        SetSweeps(true);
        if (restack != null) restack.End();
        restackStep = -1;
        Phase = RunRecedePhase.Seated;
        Step = Schedule.Steps - 1;
        StepsDone = Mathf.Max(StepsDone, Schedule.Steps);
        Line = SeatLine = Layout.length;
        if (Rig != null)
        {
            Rig.SetGoalLine(Layout.length);
            Rig.GoalBeyondHidden = false;
        }
        Place(Line, 0f);
    }
}
