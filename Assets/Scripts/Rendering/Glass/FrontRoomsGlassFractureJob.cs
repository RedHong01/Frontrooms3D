// FrontRoomsGlassFractureJob.cs -- GD3 step 3: the crack-graph generator, time-sliced (plan 10 sec 2.3 "E-down: the
// generator runs; meshes are built in slices"). Pure C#.
//
// A literal port of Tools/Blender/glass/crack_graph_ref.py generate() (version 1.3): every function keeps the
// reference's name in its comment, its evaluation order, its sort keys and its tie rules, so the doubles come out the
// same (build/03_generator.md sec 3-4). Each Step() runs ONE bounded slice: a phase of an attempt, or a chunk of one
// (a few rings, faces, frame pieces, records ...); a break takes about 60-300 slices (Annealed6 p50 about 85). StepFor(ms)
// packs as many slices into one frame as fit the budget (Annealed6: about 7 frames at 1 ms). The work and the output do not
// depend on how it is sliced. The job keeps every working list between runs (pools), so it allocates almost nothing
// once it is warm.
using System;
using System.Collections.Generic;

public sealed class FrontRoomsGlassFractureJob
{
    // ---------------------------------------------------------------------------------------------------------
    // Stream ids (part of every RNG key) -- crack_graph_ref.py S_*
    // ---------------------------------------------------------------------------------------------------------
    const long S_TRACKS = 1, S_CORE = 2, S_RINGS = 3, S_FORK = 4, S_CHORD = 5, S_KINK = 6, S_STAGE1 = 7, S_TEETH = 8,
        S_SPLIT = 9, S_LEVEL2 = 10, S_POSE = 11, S_CHIPS = 12, S_DICE = 14;
    const double TwoPi = GlassMath.TwoPi;

    public enum Phase
    {
        Idle, GraphInit, GraphRings, GraphClose, Crossings, FaceWalk, FacePieces, Small, PostDaggers, PostTeeth, PostLevel2,
        Cap, CapEval, Stage1, Records, GraphExport, Stats, Round, Validate, Dice, Done, Failed, FaceAdj
    }

    // Chunk sizes of the sliced phases (tuned so one slice stays well under 1 ms of CPU: build/03_generator.md sec 5)
    public static int RingsPerStep = 3, FaceWalkKeysPerStep = 160, FacesPerStep = 40, DaggersPerStep = 48, TeethPerStep = 6, Level2PerStep = 12,
        CapOwnersPerStep = 12, RecordsPerStep = 24, RoundPointsPerStep = 700, RoundPiecesPerStep = 60;

    /// <summary>The phase the next Step() runs.</summary>
    public Phase Next { get; private set; }
    public Phase LastPhase { get; private set; }
    public bool IsDone => Next == Phase.Done || Next == Phase.Failed;
    public bool Failed => Next == Phase.Failed;
    public string Error { get; private set; }
    public int StepsTaken { get; private set; }
    public int Attempt => attempt;
    public FrontRoomsGlassFracture Result => res;
    public FrontRoomsGlassFractureValidation Validation => validation;

    /// <summary>Run validate() as the last slice (plan sec 2.6: "validation on every build").</summary>
    public bool ValidateOnBuild = true;
    /// <summary>Round every output double to 1e-7 like the reference (keeps C# == Python == Blender).</summary>
    public bool RoundOutput = true;
    public int MaxAttempts = 96;

    // ---------------------------------------------------------------------------------------------------------
    // Inputs
    // ---------------------------------------------------------------------------------------------------------
    FrontRoomsGlassProfileData P;
    uint seed;
    double ix, iy, rotationDeg, rot, w, h;
    int side;
    GlassFrame fr;
    FrontRoomsGlassFracture res;
    readonly FrontRoomsGlassFractureValidation validation = new FrontRoomsGlassFractureValidation();

    // ---------------------------------------------------------------------------------------------------------
    // Working storage (kept between runs)
    // ---------------------------------------------------------------------------------------------------------
    sealed class Track
    {
        public int tid, parent;
        public readonly List<int> pts = new List<int>();
        public double x, y, phi, lo, hi;
        public bool initial;
        // chord events of the current ring (at most 2: as 'a' of one wedge and 'b' of the next)
        public int evCount;
        public Ev ev0, ev1;
    }

    struct Ev
    {
        public double rho, sgn;
        public int chord;
        public bool slotA;      // "va"
    }

    struct Key3 : IEquatable<Key3>
    {
        public int a, b, c;

        public Key3(int a, int b, int c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }

        public bool Equals(Key3 o) => a == o.a && b == o.b && c == o.c;
        public override bool Equals(object o) => o is Key3 && Equals((Key3)o);
        public override int GetHashCode() => unchecked((a * 73856093) ^ (b * 19349663) ^ (c * 83492791));

        // Python tuple order
        public static int Compare(Key3 x, Key3 y)
        {
            if (x.a != y.a) return x.a < y.a ? -1 : 1;
            if (x.b != y.b) return x.b < y.b ? -1 : 1;
            if (x.c != y.c) return x.c < y.c ? -1 : 1;
            return 0;
        }

        public GlassOwner Owner => GlassOwner.OfChord(a, b, c);
    }

    struct ChordW
    {
        public int ring, a, b;
        public double ra, rb;
        public Key3 key;
        public int va, vb;
    }

    struct ForkW
    {
        public int ring, parent, child, vid;
        public Key3 key;
    }

    struct EdgeW
    {
        public int u, v;
        public GlassOwner owner;
    }

    sealed class Raw
    {
        public List<GlassV2> poly;
        public GlassPieceKind kind;
        public int f0, fn;      // face vertex ids: faceVids[f0 .. f0 + fn)
    }

    sealed class PieceW
    {
        public List<GlassV2> poly;
        public GlassPieceKind kind;
        public char side;
        public GlassCorner corner;
        public readonly List<List<GlassV2>> sub = new List<List<GlassV2>>();
    }

    struct Box
    {
        public double x0, x1, y0, y1;
        public int idx;
    }

    // inputs of one attempt
    readonly List<double> rings = new List<double>();
    int K;
    double rFar, rOut;
    readonly List<GlassV2> V = new List<GlassV2>();
    readonly List<Track> trackPool = new List<Track>();
    int trackUsed;
    readonly List<Track> allTracks = new List<Track>();
    readonly List<Track> active = new List<Track>();
    readonly Dictionary<int, Track> byTid = new Dictionary<int, Track>();
    readonly List<double> th = new List<double>();
    readonly List<double> cr = new List<double>();
    readonly List<int> coreVids = new List<int>();
    readonly List<ChordW> chords = new List<ChordW>();
    readonly List<ForkW> forks = new List<ForkW>();
    int n0, kinks, kinksZeroed;
    readonly List<EdgeW> edges = new List<EdgeW>();
    readonly Dictionary<long, int> edgeOwner = new Dictionary<long, int>();
    // repairs carried across attempts (supp)
    readonly HashSet<Key3> suppChords = new HashSet<Key3>();
    readonly HashSet<Key3> suppForks = new HashSet<Key3>();
    readonly Dictionary<int, double> kinkScale = new Dictionary<int, double>();
    double globalKink;
    int attempt;
    string lastErr;
    // fork planning
    readonly HashSet<int> used = new HashSet<int>();
    readonly List<ForkPlan> plan = new List<ForkPlan>();

    struct ForkPlan
    {
        public int idx;
        public Track a, b;
        public bool wrap;
        public Key3 key;
        public double side, rho, cf, pk, gap;
    }

    // crossings
    readonly List<Box> boxes = new List<Box>();
    readonly List<long> found = new List<long>();
    static readonly Comparison<Box> BoxOrder = CompareBox;
    // faces
    readonly List<int> nbrKeys = new List<int>();
    readonly List<GlassPySet> nbrSets = new List<GlassPySet>();
    readonly List<bool> nbrHas = new List<bool>();
    readonly List<int> ordStart = new List<int>();
    readonly List<int> ordCount = new List<int>();
    readonly List<int> ordList = new List<int>();
    readonly List<int> setScratch = new List<int>();
    readonly List<double> angScratch = new List<double>();
    readonly HashSet<long> seen = new HashSet<long>();
    readonly List<int> faceVids = new List<int>();
    readonly List<int> faceStart = new List<int>();
    readonly List<int> faceCount = new List<int>();
    readonly List<bool> isCoreV = new List<bool>();
    // pieces
    readonly List<Raw> rawPool = new List<Raw>();
    int rawUsed;
    readonly List<Raw> raw = new List<Raw>();
    readonly List<Raw> smallList = new List<Raw>();
    readonly List<Key3> batch = new List<Key3>();
    readonly HashSet<long> unfixed = new HashSet<long>();
    readonly List<PieceW> piecePool = new List<PieceW>();
    int pieceUsed;
    readonly List<PieceW> pieces = new List<PieceW>();
    readonly List<List<GlassV2>> shardParts = new List<List<GlassV2>>();
    readonly List<List<GlassV2>> splitOut = new List<List<GlassV2>>();
    readonly List<List<GlassV2>> splitOut2 = new List<List<GlassV2>>();
    readonly Dictionary<Key3, OwnerFaces> byOwner = new Dictionary<Key3, OwnerFaces>();
    readonly List<Cand> cands = new List<Cand>();
    readonly List<ForkW> fks = new List<ForkW>();

    struct OwnerFaces
    {
        public int count;
        public Raw r0, r1;
    }

    struct Cand
    {
        public int tier, negRing;
        public double aU;
        public Key3 owner;
    }

    // polygon pool: every list handed out during a run comes back at the end of the run
    readonly Stack<List<GlassV2>> polyFree = new Stack<List<GlassV2>>();
    readonly List<List<GlassV2>> polyRented = new List<List<GlassV2>>();
    // scratch polygons for helpers that do not keep their result
    readonly List<GlassV2> sA = new List<GlassV2>(), sB = new List<GlassV2>(), sC = new List<GlassV2>(), sD = new List<GlassV2>();
    // assembly
    readonly List<Track> initialTracks = new List<Track>();
    readonly List<double> scoreTmp = new List<double>();
    readonly List<int> idxTmp = new List<int>();
    readonly List<RecKey> recs = new List<RecKey>();
    readonly List<GlassV2> s1Pts = new List<GlassV2>();
    readonly List<double> shardAreas = new List<double>();
    readonly List<char> sidesTmp = new List<char>();

    struct RecKey
    {
        public int rank;
        public double rr, ra;
        public int index;
    }

    // cached comparisons (a lambda that reads fields would allocate a closure per break)
    static readonly Comparison<RecKey> RecOrder = (x, y) =>
    {
        if (x.rank != y.rank) return x.rank < y.rank ? -1 : 1;
        if (x.rr != y.rr) return x.rr < y.rr ? -1 : 1;
        if (x.ra != y.ra) return x.ra < y.ra ? -1 : 1;
        return x.index.CompareTo(y.index);     // stable
    };
    readonly Comparison<int> byScoreThenTid, byTrackId;

    public FrontRoomsGlassFractureJob()
    {
        // capacities above the largest break seen (Annealed6_Cinematic: ~2,000 graph vertices, ~350 faces), so a
        // warm job does not grow its lists
        V.Capacity = 4096;
        edges.Capacity = 8192;
        faceVids.Capacity = 16384;
        faceStart.Capacity = 1024;
        faceCount.Capacity = 1024;
        ordList.Capacity = 16384;
        ordStart.Capacity = 4096;
        ordCount.Capacity = 4096;
        isCoreV.Capacity = 4096;
        chords.Capacity = 1024;
        forks.Capacity = 256;
        raw.Capacity = 512;
        pieces.Capacity = 512;
        shardParts.Capacity = 512;
        recs.Capacity = 512;
        pieceDiam.Capacity = 512;
        polyRented.Capacity = 8192;
        byScoreThenTid = (x, y) =>
        {
            var a = scoreTmp[x];
            var b = scoreTmp[y];
            if (a != b) return a < b ? -1 : 1;
            return initialTracks[x].tid.CompareTo(initialTracks[y].tid);
        };
        byTrackId = (x, y) => initialTracks[x].tid.CompareTo(initialTracks[y].tid);
    }

    List<GlassV2> Rent()
    {
        var l = polyFree.Count > 0 ? polyFree.Pop() : new List<GlassV2>(8);
        l.Clear();
        polyRented.Add(l);
        return l;
    }

    List<GlassV2> RentCopy(List<GlassV2> src)
    {
        var l = Rent();
        l.AddRange(src);
        return l;
    }

    void ReturnAll()
    {
        for (var i = 0; i < polyRented.Count; i++)
            polyFree.Push(polyRented[i]);
        polyRented.Clear();
    }

    // ---------------------------------------------------------------------------------------------------------
    // Begin: generate() up to the attempt loop
    // ---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Starts one break (use FrontRoomsGlassFracturePattern.Begin for record-style inputs). impactSlabX/Y = the impact
    /// in SLAB-LOCAL metres before the 0.2 m clamp; slab size w x h (the map window: 1.391 x 1.642); rotationDeg =
    /// record.rotation (NaN = the map's default MapHash.Unit(seed) * 360); side = record.side; bandMode "band" or
    /// "stop". The result object is reset and filled.
    /// </summary>
    public void BeginSlab(FrontRoomsGlassProfileData profile, uint seed, double impactSlabX, double impactSlabY, double slabW,
        double slabH, double rotationDeg, int side, string bandMode, FrontRoomsGlassFracture result)
    {
        if (profile == null)
            throw new ArgumentNullException("profile");
        P = profile;
        this.seed = seed;
        this.side = side >= 0 ? 1 : -1;
        w = slabW;
        h = slabH;
        fr = FrontRoomsGlassFracturePattern.FrameForSlab(w, h, bandMode ?? "band");
        ix = impactSlabX;
        iy = impactSlabY;
        FrontRoomsGlassFracturePattern.ClampImpact(ref ix, ref iy, fr);
        if (double.IsNaN(rotationDeg))
            rotationDeg = FrontRoomsGlassFracturePattern.MapRotationDeg(seed);
        this.rotationDeg = rotationDeg;
        rot = GlassMath.Radians(rotationDeg);
        res = result ?? new FrontRoomsGlassFracture();
        res.Reset();
        validation.Reset();
        ReturnAll();
        suppChords.Clear();
        suppForks.Clear();
        kinkScale.Clear();
        globalKink = 1.0;
        repairsClear();
        attempt = 0;
        lastErr = null;
        Error = null;
        StepsTaken = 0;
        LastPhase = Phase.Idle;
        Next = P.mode == GlassFractureMode.Dice ? Phase.Dice : Phase.GraphInit;
    }

    void repairsClear() => res.repairs.Clear();

    /// <summary>Runs one slice. Returns true when the break is finished (or failed).</summary>
    public bool Step()
    {
        if (IsDone)
            return true;
        StepsTaken++;
        LastPhase = Next;
        switch (Next)
        {
            case Phase.GraphInit:
                if (attempt >= MaxAttempts)
                {
                    Error = "no valid pattern after " + MaxAttempts + " attempts (" + lastErr + ")";
                    Next = Phase.Failed;
                    break;
                }
                if (!GraphInit())
                {
                    GraphFailed();
                    break;
                }
                Next = ringK < K ? Phase.GraphRings : Phase.GraphClose;
                break;
            case Phase.GraphRings:
                for (var i = 0; i < RingsPerStep && ringK < K; i++, ringK++)
                    if (!GraphRing(ringK))
                    {
                        GraphFailed();
                        return IsDone;
                    }
                if (ringK >= K)
                    Next = Phase.GraphClose;
                break;
            case Phase.GraphClose:
                if (!GraphClose())
                {
                    GraphFailed();
                    break;
                }
                BuildEdgeOwner();
                Next = Phase.Crossings;
                break;
            case Phase.Crossings:
                if (CheckCrossings())
                    NextAttempt();
                else
                    Next = Phase.FaceAdj;
                break;
            case Phase.FaceAdj:
                FaceAdjacency();
                FacesBegin();
                Next = Phase.FaceWalk;
                break;
            case Phase.FaceWalk:
                if (FaceWalk())
                {
                    cursor = 0;
                    Next = Phase.FacePieces;
                }
                break;
            case Phase.FacePieces:
            {
                var end = Math.Min(faceStart.Count, cursor + FacesPerStep);
                for (; cursor < end; cursor++)
                    if (!FacePiece(cursor))
                    {
                        supDamp();
                        AddRepair(GlassRepairKind.NonconvexFaceDampKinks, default(GlassOwner), default(GlassOwner), 0.0, 0, null);
                        NextAttempt();
                        return IsDone;
                    }
                if (cursor >= faceStart.Count)
                {
                    smallState = 0;
                    Next = Phase.Small;
                }
                break;
            }
            case Phase.Small:
                if (SmallStep())
                    NextAttempt();
                break;
            case Phase.PostDaggers:
            {
                var end = Math.Min(raw.Count, cursor + DaggersPerStep);
                for (; cursor < end; cursor++)
                {
                    var r = raw[cursor];
                    if (r.kind == GlassPieceKind.Crush)
                        pieces.Add(NewPiece(r.poly, GlassPieceKind.Crush, '\0', GlassCorner.None));
                    else
                        SplitDagger(r.poly, shardParts);
                }
                if (cursor >= raw.Count)
                {
                    cursor = 0;
                    Next = Phase.PostTeeth;
                }
                break;
            }
            case Phase.PostTeeth:
            {
                var end = Math.Min(shardParts.Count, cursor + TeethPerStep);
                for (; cursor < end; cursor++)
                    ShardTeeth(shardParts[cursor]);
                if (cursor >= shardParts.Count)
                {
                    cursor = 0;
                    Next = Phase.PostLevel2;
                }
                break;
            }
            case Phase.PostLevel2:
            {
                var end = Math.Min(pieces.Count, cursor + Level2PerStep);
                for (; cursor < end; cursor++)
                    Level2For(pieces[cursor]);
                if (cursor >= pieces.Count)
                    Next = Phase.Cap;
                break;
            }
            case Phase.Cap:
                Next = CapStart() ? Phase.CapEval : Phase.Stage1;
                cursor = 0;
                break;
            case Phase.CapEval:
            {
                var end = Math.Min(ownerKeys.Count, cursor + CapOwnersPerStep);
                for (; cursor < end; cursor++)
                    CapEvalOwner(ownerKeys[cursor]);
                if (cursor >= ownerKeys.Count)
                {
                    if (CapDecide())
                        NextAttempt();
                    else
                        Next = Phase.Stage1;
                }
                break;
            }
            case Phase.Stage1:
                Stage1AndOrder();
                cursor = 0;
                Next = Phase.Records;
                break;
            case Phase.Records:
            {
                var end = Math.Min(recs.Count, cursor + RecordsPerStep);
                for (; cursor < end; cursor++)
                    Record(cursor);
                if (cursor >= recs.Count)
                    Next = Phase.GraphExport;
                break;
            }
            case Phase.GraphExport:
                GraphExport();
                Next = Phase.Stats;
                break;
            case Phase.Stats:
                Stats();
                ReturnAll();
                AfterBuild();
                break;
            case Phase.Dice:
                GenerateDice();
                AfterBuild();
                break;
            case Phase.Round:
                if (RoundChunk())
                    BeginValidateOrDone();
                break;
            case Phase.Validate:
                if (validation.Step())
                    Next = Phase.Done;
                break;
        }
        return IsDone;
    }

    /// <summary>
    /// Runs slices for one frame: always one, then more while the time spent plus the expected cost of the next slice
    /// (a running estimate per phase) stays within budgetMs of wall time. Returns true when the break is done.
    /// </summary>
    public bool StepFor(double budgetMs)
    {
        var clock = ClockMs;
        var start = clock();
        var first = true;
        while (true)
        {
            var now = clock();
            if (!first && now - start + ExpectedMs(Next) > budgetMs)
                return false;
            first = false;
            var phase = Next;
            var done = Step();
            Learn(phase, clock() - now);
            if (done)
                return true;
        }
    }

    /// <summary>
    /// The clock StepFor reads, in ms. Default: Stopwatch wall time, as the game runs. The edit-mode test swaps in the
    /// thread's CPU clock to measure the packing without other processes' preemption (build/03_generator.md sec 5.2).
    /// </summary>
    public static Func<double> ClockMs = StopwatchMs;

    static readonly double MsPerTick = 1000.0 / System.Diagnostics.Stopwatch.Frequency;

    static double StopwatchMs() => System.Diagnostics.Stopwatch.GetTimestamp() * MsPerTick;

    /// <summary>
    /// Runs every remaining slice now (the deadline call: the stage builder calls it on the last frame before the
    /// Crack1 swap if StepFor has not finished; build/03_generator.md sec 5.2). Returns true unless the break failed.
    /// </summary>
    public bool Finish()
    {
        while (!Step())
        {
        }
        return !Failed;
    }

    // Per-phase cost estimate for StepFor: a decaying maximum of that phase's recent slices (it falls by PhaseDecay per
    // slice of the phase; StepFor plans with PhaseGuard x the estimate), so a phase whose slices vary is planned at its
    // recent worst. It starts at PhasePriorMs, the per-phase p99 of one slice measured on the M3 Max under load
    // (build/03_generator.md sec 5), so the first break already packs well; on a slower machine the observed costs
    // take over within a few slices. A slice that took longer than 1 ms counts as 1 ms: a preempted slice says little
    // about the work, and the decay forgets it within a few slices.
    public static double PhaseDecay = 0.9, PhaseGuard = 1.2, PhaseDefaultMs = 0.5;

    static readonly double[] PhasePriorMs = BuildPriors();

    static double[] BuildPriors()
    {
        var a = new double[32];
        for (var i = 0; i < a.Length; i++)
            a[i] = PhaseDefaultMs;
        a[(int)Phase.GraphInit] = 0.12;
        a[(int)Phase.GraphRings] = 0.18;
        a[(int)Phase.GraphClose] = 0.26;
        a[(int)Phase.Crossings] = 0.36;
        a[(int)Phase.FaceAdj] = 0.28;
        a[(int)Phase.FaceWalk] = 0.42;
        a[(int)Phase.FacePieces] = 0.16;
        a[(int)Phase.Small] = 0.13;
        a[(int)Phase.PostDaggers] = 0.29;
        a[(int)Phase.PostTeeth] = 0.09;
        a[(int)Phase.PostLevel2] = 0.17;
        a[(int)Phase.Cap] = 0.31;
        a[(int)Phase.CapEval] = 0.40;
        a[(int)Phase.Stage1] = 0.41;
        a[(int)Phase.Records] = 0.37;
        a[(int)Phase.GraphExport] = 0.06;
        a[(int)Phase.Stats] = 0.16;
        a[(int)Phase.Round] = 0.13;
        a[(int)Phase.Validate] = 0.44;
        a[(int)Phase.Dice] = 0.13;
        return a;
    }

    readonly double[] phaseMs = new double[32];

    double ExpectedMs(Phase p)
    {
        var e = phaseMs[(int)p];
        return PhaseGuard * (e > 0.0 ? e : PhasePriorMs[(int)p]);
    }

    void Learn(Phase p, double ms)
    {
        var i = (int)p;
        if (ms > 1.0)
            ms = 1.0;
        var decayed = PhaseDecay * (phaseMs[i] > 0.0 ? phaseMs[i] : PhasePriorMs[i]);
        phaseMs[i] = ms > decayed ? ms : decayed;
    }

    void AfterBuild()
    {
        roundStage = 0;
        roundIdx = 0;
        if (RoundOutput)
            Next = Phase.Round;
        else
            BeginValidateOrDone();
    }

    void BeginValidateOrDone()
    {
        if (ValidateOnBuild)
        {
            validation.Begin(res, P);
            Next = Phase.Validate;
        }
        else
            Next = Phase.Done;
    }

    void GraphFailed()
    {
        supDamp();
        AddRepair(GlassRepairKind.BuildErrorDampKinks, default(GlassOwner), default(GlassOwner), 0.0, 0, lastErr);
        NextAttempt();
    }

    int cursor, ringK, smallState, excess, roundStage, roundIdx, faceKi;

    bool RoundChunk()
    {
        switch (roundStage)
        {
            case 0:
                FrontRoomsGlassFracturePattern.RoundHeader(res);
                roundStage = 1;
                roundIdx = 0;
                return false;
            case 1:
                roundIdx = FrontRoomsGlassFracturePattern.RoundPoints(res.graphVerts, roundIdx, RoundPointsPerStep);
                if (roundIdx >= res.graphVerts.Count) { roundStage = 2; roundIdx = 0; }
                return false;
            case 2:
                roundIdx = FrontRoomsGlassFracturePattern.RoundPieces(res, roundIdx, RoundPiecesPerStep);
                if (roundIdx >= res.pieces.Count) { roundStage = 3; roundIdx = 0; }
                return false;
            case 3:
                roundIdx = FrontRoomsGlassFracturePattern.RoundPoints(res.verts, roundIdx, RoundPointsPerStep);
                if (roundIdx >= res.verts.Count) { roundStage = 4; roundIdx = 0; }
                return false;
            default:
                roundIdx = FrontRoomsGlassFracturePattern.RoundPoints(res.subVerts, roundIdx, RoundPointsPerStep);
                return roundIdx >= res.subVerts.Count;
        }
    }

    void supDamp() => globalKink *= 0.5;

    void NextAttempt()
    {
        attempt++;
        ReturnAll();
        Next = Phase.GraphInit;
    }

    void AddRepair(GlassRepairKind kind, GlassOwner o1, GlassOwner o2, double value, int count, string text)
    {
        res.repairs.Add(new GlassRepair { kind = kind, o1 = o1, o2 = o2, value = value, count = count, text = text });
    }

    // ---------------------------------------------------------------------------------------------------------
    // _build_graph
    // ---------------------------------------------------------------------------------------------------------
    Track NewTrack(int tid, int parent, int vid, double x, double y, double phi, double lo, double hi, bool initial)
    {
        Track t;
        if (trackUsed < trackPool.Count)
            t = trackPool[trackUsed];
        else
        {
            t = new Track();
            trackPool.Add(t);
        }
        trackUsed++;
        t.tid = tid;
        t.parent = parent;
        t.pts.Clear();
        t.pts.Add(vid);
        t.x = x;
        t.y = y;
        t.phi = phi;
        t.lo = lo;
        t.hi = hi;
        t.initial = initial;
        t.evCount = 0;
        return t;
    }

    int AddV(double x, double y)
    {
        V.Add(new GlassV2(x, y));
        return V.Count - 1;
    }

    double DistToStop(double phi) =>
        GlassPoly.RayRectExit(ix, iy, GlassMath.Cos(phi), GlassMath.Sin(phi), fr.stopX0, fr.stopX1, fr.stopY0, fr.stopY1);

    double TSepAt(int k) => Math.Max(P.tSep, P.tSepFrac * rings[k]);
    double ChordRMax(int k) => rings[k] * (1.0 + P.ringNodeJitter) + 0.5 * TSepAt(k);
    double ChordRMin(int k) => rings[k] * (1.0 - P.ringNodeJitter) - 0.5 * TSepAt(k);

    // advance(): false = GenerationError (lastErr set)
    bool Advance(Track t, double rho, out double ox, out double oy)
    {
        var px = t.x;
        var py = t.y;
        var dx = GlassMath.Cos(t.phi);
        var dy = GlassMath.Sin(t.phi);
        var pd = px * dx + py * dy;
        var disc = pd * pd - (px * px + py * py - rho * rho);
        ox = oy = 0.0;
        if (disc < 0.0)
        {
            lastErr = "track " + t.tid + " cannot reach radius " + FrontRoomsGlassFracturePattern.FormatFixed(rho, 4);
            return false;
        }
        var s = -pd + GlassMath.Sqrt(disc);
        if (s <= 1e-9)
        {
            lastErr = "track " + t.tid + " does not move outward at " + FrontRoomsGlassFracturePattern.FormatFixed(rho, 4);
            return false;
        }
        t.x = px + dx * s;
        t.y = py + dy * s;
        ox = t.x;
        oy = t.y;
        return true;
    }

    bool Peek(Track t, double rho, out GlassV2 p)
    {
        var px = t.x;
        var py = t.y;
        var dx = GlassMath.Cos(t.phi);
        var dy = GlassMath.Sin(t.phi);
        var pd = px * dx + py * dy;
        var disc = pd * pd - (px * px + py * py - rho * rho);
        if (disc < 0.0)
        {
            p = default(GlassV2);
            return false;
        }
        var s = -pd + GlassMath.Sqrt(disc);
        p = new GlassV2(px + dx * s, py + dy * s);
        return true;
    }

    static void Kink(Track t, double kappa)
    {
        // clamp into the lane without flipping the sign required by the convexity rule
        var nw = t.phi + kappa;
        if (kappa > 0.0)
            nw = Math.Min(nw, Math.Max(t.phi, t.hi));
        else if (kappa < 0.0)
            nw = Math.Max(nw, Math.Min(t.phi, t.lo));
        t.phi = nw;
    }

    static double PairGap(Track a, Track b, bool wrap) => (b.phi + (wrap ? TwoPi : 0.0)) - a.phi;

    bool GraphInit()
    {
        var hx = fr.hx;
        var hy = fr.hy;
        // corners = [(-hx, -hy), (hx, -hy), (hx, hy), (-hx, hy)]; r_far = max(hypot(...))
        rFar = GlassMath.Hypot(-hx - ix, -hy - iy);
        rFar = PyMax(rFar, GlassMath.Hypot(hx - ix, -hy - iy));
        rFar = PyMax(rFar, GlassMath.Hypot(hx - ix, hy - iy));
        rFar = PyMax(rFar, GlassMath.Hypot(-hx - ix, hy - iy));
        rOut = 2.6 * rFar + 0.1;
        V.Clear();
        trackUsed = 0;
        allTracks.Clear();
        active.Clear();
        byTid.Clear();
        chords.Clear();
        forks.Clear();
        kinks = 0;
        kinksZeroed = 0;

        // ---- rings
        rings.Clear();
        rings.Add(P.coreRadius);
        rings.Add(P.ring1);
        var kk = 1;
        while (rings[rings.Count - 1] < rFar)
        {
            var rg = GlassRng.Make(seed, S_RINGS, kk);
            var g = rg.Uniform(1.0 - P.ringGrowthJitter, 1.0 + P.ringGrowthJitter);
            rings.Add(rings[rings.Count - 1] * P.ringGrowth * g);
            kk++;
        }
        K = rings.Count - 1;

        // ---- initial radials + crush core
        var r0 = GlassRng.Make(seed, S_TRACKS);
        n0 = r0.RandInt(P.radialsMin, P.radialsMax);
        th.Clear();
        for (var i = 0; i < n0; i++)
        {
            var rj1 = GlassRng.Make(seed, S_TRACKS, 1, i);
            var j = rj1.Uniform(-P.radialAngleJitter, P.radialAngleJitter);
            th.Add(rot + (i + j) * TwoPi / n0);
        }
        cr.Clear();
        for (var i = 0; i < n0; i++)
        {
            var rc = GlassRng.Make(seed, S_CORE, i);
            cr.Add(P.coreRadius * (1.0 + rc.Uniform(-P.coreJitter, P.coreJitter)));
        }
        // make the core polygon strictly convex (raise any vertex that sits inside its neighbours' chord)
        for (var it = 0; it < 4 * n0; it++)
        {
            var fixedAll = true;
            for (var i = 0; i < n0; i++)
            {
                var im = (i - 1 + n0) % n0;
                var ip = (i + 1) % n0;
                var ax = cr[im] * GlassMath.Cos(th[im]);
                var ay = cr[im] * GlassMath.Sin(th[im]);
                var bx = cr[ip] * GlassMath.Cos(th[ip]);
                var by = cr[ip] * GlassMath.Sin(th[ip]);
                var ux = GlassMath.Cos(th[i]);
                var uy = GlassMath.Sin(th[i]);
                var ex = bx - ax;
                var ey = by - ay;
                var den = ux * ey - uy * ex;
                if (Math.Abs(den) < 1e-15)
                    continue;
                var t = (ax * ey - ay * ex) / den;
                if (cr[i] < t * 1.02)
                {
                    cr[i] = t * 1.02;
                    fixedAll = false;
                }
            }
            if (fixedAll)
                break;
        }
        // the crater must stay <= 15 mm (V5): scale the whole core down if the jitter + convexity raise grew it
        var m = cr[0];
        for (var i = 1; i < n0; i++)
            m = PyMax(m, cr[i]);
        if (m > P.coreMax)
            for (var i = 0; i < n0; i++)
                cr[i] = cr[i] * P.coreMax / m;
        coreVids.Clear();
        for (var i = 0; i < n0; i++)
        {
            var gl = i > 0 ? (th[i] - th[i - 1]) : (th[0] - (th[n0 - 1] - TwoPi));
            var gr = i + 1 < n0 ? (th[i + 1] - th[i]) : ((th[0] + TwoPi) - th[i]);
            var x = cr[i] * GlassMath.Cos(th[i]);
            var y = cr[i] * GlassMath.Sin(th[i]);
            var vid = AddV(x, y);
            var t = NewTrack(i, -1, vid, x, y, th[i], th[i] - P.laneFrac * gl, th[i] + P.laneFrac * gr, true);
            allTracks.Add(t);
            active.Add(t);
            byTid[i] = t;
            coreVids.Add(vid);
        }
        ringK = 1;
        return true;
    }

    // one ring k of the main loop of _build_graph: forks in the annulus below ring k, then the chords at ring k
    bool GraphRing(int k)
    {
        var rj = P.ringNodeJitter;
        {
            // -------- forks in the annulus below ring k
            if (k >= P.forkFromRing && P.forkP > 0.0)
            {
                var loR = ChordRMax(k - 1) * 1.02;
                var hiR = ChordRMin(k) * 0.98;
                if (hiR > loR)
                {
                    used.Clear();
                    plan.Clear();
                    var n = active.Count;
                    for (var idx = 0; idx < n; idx++)
                    {
                        var a = active[idx];
                        var b = active[(idx + 1) % n];
                        var wrap = idx + 1 == n;
                        var key = new Key3(k, a.tid, b.tid);
                        var rng = GlassRng.Make(seed, S_FORK, k, a.tid, b.tid);
                        var c = rng.U01();
                        var sideU = rng.U01();
                        var frU = rng.U01();
                        var cf = rng.U01();
                        var pk = rng.U01();
                        var gap = PairGap(a, b, wrap);
                        var width = rings[k] * gap;
                        var pF = P.forkP;
                        if (width > P.forkWide)
                            pF += (P.forkWideP - pF) * Math.Min(1.0, (width - P.forkWide) / P.forkWide);
                        var gate = gap > P.forkMinGap || (width > P.forkWide && gap > 0.10);
                        if (!gate || c >= pF || suppForks.Contains(key))
                            continue;
                        if (used.Contains(a.tid) || used.Contains(b.tid))
                            continue;
                        var rho = loR + frU * (hiR - loR);
                        var mid = a.phi + 0.5 * gap;
                        if (rho > P.forkMaxFracToStop * DistToStop(mid))
                            continue;
                        used.Add(a.tid);
                        used.Add(b.tid);
                        plan.Add(new ForkPlan { idx = idx, a = a, b = b, wrap = wrap, key = key, side = sideU, rho = rho, cf = cf, pk = pk, gap = gap });
                    }
                    for (var pi = 0; pi < plan.Count; pi++)
                    {
                        var fp = plan[pi];
                        var a = fp.a;
                        var b = fp.b;
                        var wrap = fp.wrap;
                        var gap = fp.gap;
                        var parent = fp.side < 0.5 ? a : b;
                        var s = parent == a ? 1.0 : -1.0;
                        double sx, sy;
                        if (!Advance(parent, fp.rho, out sx, out sy))
                            return false;
                        var vid = AddV(sx, sy);
                        parent.pts.Add(vid);
                        var childFrac = P.forkChildFracMin + fp.cf * (P.forkChildFracMax - P.forkChildFracMin);
                        var pkFrac = P.forkParentKinkFracMin + fp.pk * (P.forkParentKinkFracMax - P.forkParentKinkFracMin);
                        // unwrapped child direction (between parent and the other track)
                        var phic = parent.phi + s * gap * childFrac;
                        var margin = 0.08 * gap;
                        Kink(parent, -s * gap * pkFrac);
                        kinks++;
                        var tid = 1000 + (int)(GlassRng.MixKey(fp.key.a, fp.key.b, fp.key.c) & 0x7FFFFFFUL);
                        Track child;
                        if (s > 0)
                        {
                            // child between a (parent) and b
                            var midpc = 0.5 * (parent.phi + phic);
                            var midcb = 0.5 * (phic + b.phi + (wrap ? TwoPi : 0.0));
                            parent.hi = Math.Min(parent.hi, midpc - margin);
                            child = NewTrack(tid, parent.tid, vid, sx, sy, phic, midpc + margin, midcb - margin, false);
                            if (wrap)
                                b.lo = Math.Max(b.lo, midcb + margin - TwoPi);
                            else
                                b.lo = Math.Max(b.lo, midcb + margin);
                        }
                        else
                        {
                            // child between a and b (parent)
                            var aPhi = a.phi - (wrap ? TwoPi : 0.0);
                            var midcp = 0.5 * (phic + parent.phi);
                            var midac = 0.5 * (aPhi + phic);
                            parent.lo = Math.Max(parent.lo, midcp + margin);
                            child = NewTrack(tid, parent.tid, vid, sx, sy, phic, midac + margin, midcp - margin, false);
                            if (wrap)
                                a.hi = Math.Min(a.hi, midac - margin + TwoPi);
                            else
                                a.hi = Math.Min(a.hi, midac - margin);
                            if (wrap)
                            {
                                // keep the child's angles in a's frame so the active list stays monotonic
                                child.phi += TwoPi;
                                child.lo += TwoPi;
                                child.hi += TwoPi;
                            }
                        }
                        forks.Add(new ForkW { ring = k, parent = parent.tid, child = tid, vid = vid, key = fp.key });
                        allTracks.Add(child);
                        byTid[tid] = child;
                        var pos = active.IndexOf(parent);
                        if (s > 0)
                            active.Insert(pos + 1, child);
                        else if (wrap)
                            active.Add(child);
                        else
                            active.Insert(pos, child);
                    }
                    Renormalise();
                }
            }

            // -------- chords at ring k
            {
                var n = active.Count;
                for (var i = 0; i < n; i++)
                    active[i].evCount = 0;
                var ringStart = chords.Count;
                for (var idx = 0; idx < n; idx++)
                {
                    var a = active[idx];
                    var b = active[(idx + 1) % n];
                    var wrap = idx + 1 == n;
                    var key = new Key3(k, a.tid, b.tid);
                    var rng = GlassRng.Make(seed, S_CHORD, k, a.tid, b.tid);
                    var u = rng.U01();
                    var ja = rng.U01();
                    var jb = rng.U01();
                    var mid = a.phi + 0.5 * PairGap(a, b, wrap);
                    var dstop = PyMax(DistToStop(mid), P.ring1 * 1.0001);
                    var tt = dstop <= P.ring1 ? 0.0 :
                        Math.Min(1.0, Math.Max(0.0, GlassMath.Log(rings[k] / P.ring1) / GlassMath.Log(dstop / P.ring1)));
                    var p = P.chordPInner + (P.chordPOuter - P.chordPInner) * tt;
                    if (u >= p || suppChords.Contains(key))
                        continue;
                    // chords whose wedge lies wholly outside the slab are useless
                    if (rings[k] * (1.0 - rj) > rFar)
                        continue;
                    var ra = rings[k] * (1.0 - rj + 2.0 * rj * ja);
                    var rb = rings[k] * (1.0 - rj + 2.0 * rj * jb);
                    // a ring crack runs ACROSS its wedge: in a thin wedge (next to a fork) skip it, and keep the chord
                    // within ~48 deg of tangential, so both faces stay convex at its T-ends
                    GlassV2 pa, pb;
                    if (!Peek(a, rings[k], out pa) || !Peek(b, rings[k], out pb))
                        continue;
                    var sW = GlassMath.Hypot(pa.x - pb.x, pa.y - pb.y);
                    if (sW < PyMax(P.chordMinWedge, 2.5 * TSepAt(k)))
                        continue;
                    var lim = 0.5 * sW;
                    if (Math.Abs(ra - rb) > lim)
                    {
                        var mm = 0.5 * (ra + rb);
                        if (ra < rb)
                        {
                            ra = mm - 0.5 * lim;
                            rb = mm + 0.5 * lim;
                        }
                        else
                        {
                            ra = mm + 0.5 * lim;
                            rb = mm - 0.5 * lim;
                        }
                    }
                    chords.Add(new ChordW { ring = k, a = a.tid, b = b.tid, ra = ra, rb = rb, key = key, va = -1, vb = -1 });
                    var ci = chords.Count - 1;
                    AddEv(a, new Ev { rho = ra, sgn = -1.0, chord = ci, slotA = true });    // chord on a's higher side -> bend lower
                    AddEv(b, new Ev { rho = rb, sgn = 1.0, chord = ci, slotA = false });    // chord on b's lower side -> bend higher
                }
                for (var ti = 0; ti < n; ti++)
                {
                    var t = active[ti];
                    if (t.evCount == 0)
                        continue;
                    // lst.sort(key=e[0]) -- stable
                    if (t.evCount == 2 && t.ev1.rho < t.ev0.rho)
                    {
                        var tmp = t.ev0;
                        t.ev0 = t.ev1;
                        t.ev1 = tmp;
                    }
                    if (t.evCount == 2)
                    {
                        // V2: the ring cracks on the two sides of a radial end at visibly different points (two T's)
                        var sep = TSepAt(k);
                        var gap = t.ev1.rho - t.ev0.rho;
                        if (gap < sep)
                        {
                            var d = 0.5 * (sep - gap);
                            t.ev0.rho = t.ev0.rho - d;
                            t.ev1.rho = t.ev1.rho + d;
                        }
                    }
                    var lastR = -1.0;
                    for (var e = 0; e < t.evCount; e++)
                    {
                        var ev = e == 0 ? t.ev0 : t.ev1;
                        var rho = ev.rho;
                        if (rho <= lastR + 1e-6)
                            rho = lastR + 1e-6;
                        lastR = rho;
                        double x, y;
                        if (!Advance(t, rho, out x, out y))
                            return false;
                        var vid = AddV(x, y);
                        t.pts.Add(vid);
                        var ch = chords[ev.chord];
                        if (ev.slotA)
                            ch.va = vid;
                        else
                            ch.vb = vid;
                        chords[ev.chord] = ch;
                        var rk = GlassRng.Make(seed, S_KINK, ch.key.a, ch.key.b, ch.key.c, ev.slotA ? 0 : 1);
                        var mag = GlassMath.Radians(P.kinkDeg) * rk.Uniform(0.5, 1.5);
                        if (rk.U01() < P.kinkStrongP)
                        {
                            // now and then a radial deflects visibly where a ring crack meets it (V2: "kinked")
                            mag = GlassMath.Radians(rk.Uniform(P.kinkStrongDegMin, P.kinkStrongDegMax));
                        }
                        double ks;
                        if (!kinkScale.TryGetValue(t.tid, out ks))
                            ks = 1.0;
                        mag *= ks * globalKink;
                        var before = t.phi;
                        Kink(t, ev.sgn * mag);
                        kinks++;
                        if (Math.Abs(t.phi - before) < 1e-12)
                            kinksZeroed++;
                    }
                }
            }
        }
        return true;
    }

    bool GraphClose()
    {
        // ---- run every track out to r_out and close the outer boundary
        for (var i = 0; i < active.Count; i++)
        {
            double x, y;
            if (!Advance(active[i], rOut, out x, out y))
                return false;
            active[i].pts.Add(AddV(x, y));
        }
        edges.Clear();
        for (var ti = 0; ti < allTracks.Count; ti++)
        {
            var t = allTracks[ti];
            for (var i = 0; i < t.pts.Count - 1; i++)
                edges.Add(new EdgeW { u = t.pts[i], v = t.pts[i + 1], owner = GlassOwner.OfTrack(t.tid) });
        }
        for (var i = 0; i < chords.Count; i++)
            edges.Add(new EdgeW { u = chords[i].va, v = chords[i].vb, owner = chords[i].key.Owner });
        for (var i = 0; i < coreVids.Count; i++)
            edges.Add(new EdgeW { u = coreVids[i], v = coreVids[(i + 1) % coreVids.Count], owner = GlassOwner.OfIndex(GlassOwner.Core, i) });
        for (var i = 0; i < active.Count; i++)
        {
            var e0 = active[i].pts[active[i].pts.Count - 1];
            var e1 = active[(i + 1) % active.Count].pts[active[(i + 1) % active.Count].pts.Count - 1];
            edges.Add(new EdgeW { u = e0, v = e1, owner = GlassOwner.OfIndex(GlassOwner.Outer, i) });
        }
        return true;
    }

    static void AddEv(Track t, Ev e)
    {
        if (t.evCount == 0)
            t.ev0 = e;
        else
            t.ev1 = e;
        t.evCount++;
    }

    void Renormalise()
    {
        // keep the active list's unwrapped angles monotonic, starting near active[0]
        var bas = active[0].phi;
        for (var i = 0; i < active.Count; i++)
        {
            var t = active[i];
            while (t.phi < bas - 1e-9)
            {
                t.phi += TwoPi;
                t.lo += TwoPi;
                t.hi += TwoPi;
            }
            while (t.phi >= bas + TwoPi - 1e-9)
            {
                t.phi -= TwoPi;
                t.lo -= TwoPi;
                t.hi -= TwoPi;
            }
        }
    }

    static double PyMax(double a, double b) => b > a ? b : a;    // Python max(a, b): the first of equals
    static double PyMin(double a, double b) => b < a ? b : a;

    static long DirKey(int u, int v) => ((long)u << 32) | (uint)v;

    void BuildEdgeOwner()
    {
        edgeOwner.Clear();
        for (var i = 0; i < edges.Count; i++)
        {
            edgeOwner[DirKey(edges[i].u, edges[i].v)] = i;
            edgeOwner[DirKey(edges[i].v, edges[i].u)] = i;
        }
    }

    GlassOwner OwnerOf(int u, int v) => edges[edgeOwner[DirKey(u, v)]].owner;

    // ---------------------------------------------------------------------------------------------------------
    // crossings (prototype fix 1): drop the younger element and rebuild
    // ---------------------------------------------------------------------------------------------------------
    static int CompareBox(Box a, Box b)
    {
        if (a.x0 != b.x0) return a.x0 < b.x0 ? -1 : 1;
        if (a.x1 != b.x1) return a.x1 < b.x1 ? -1 : 1;
        if (a.y0 != b.y0) return a.y0 < b.y0 ? -1 : 1;
        if (a.y1 != b.y1) return a.y1 < b.y1 ? -1 : 1;
        return a.idx.CompareTo(b.idx);
    }

    /// <summary>_graph_crossings: sorted pairs (i, j) of edges that touch or cross without sharing a vertex.</summary>
    static void GraphCrossings(List<GlassV2> verts, List<int> eu, List<int> ev, double bx0, double bx1, double by0, double by1,
        bool useBox, List<Box> boxes, List<long> found)
    {
        boxes.Clear();
        found.Clear();
        for (var idx = 0; idx < eu.Count; idx++)
        {
            var p = verts[eu[idx]];
            var q = verts[ev[idx]];
            if (useBox)
            {
                GlassV2 cp, cq;
                if (!ClipSegment(p, q, bx0, bx1, by0, by1, out cp, out cq))
                    continue;
                p = cp;
                q = cq;
            }
            boxes.Add(new Box { x0 = Math.Min(p.x, q.x), x1 = Math.Max(p.x, q.x), y0 = Math.Min(p.y, q.y), y1 = Math.Max(p.y, q.y), idx = idx });
        }
        boxes.Sort(BoxOrder);
        var n = boxes.Count;
        for (var i = 0; i < n; i++)
        {
            var A = boxes[i];
            int u1 = eu[A.idx], v1 = ev[A.idx];
            for (var j = i + 1; j < n; j++)
            {
                var B = boxes[j];
                if (B.x0 > A.x1 + 1e-12)
                    break;
                if (B.y0 > A.y1 + 1e-12 || B.y1 < A.y0 - 1e-12)
                    continue;
                int u2 = eu[B.idx], v2 = ev[B.idx];
                if (u1 == u2 || u1 == v2 || v1 == u2 || v1 == v2)
                    continue;
                if (GlassPoly.SegmentsCross(verts[u1], verts[v1], verts[u2], verts[v2]))
                {
                    var lo = Math.Min(A.idx, B.idx);
                    var hi = Math.Max(A.idx, B.idx);
                    found.Add(((long)lo << 32) | (uint)hi);
                }
            }
        }
        found.Sort();
    }

    /// <summary>Liang-Barsky: the part of segment p-q inside the box, or false.</summary>
    static bool ClipSegment(GlassV2 p, GlassV2 q, double x0, double x1, double y0, double y1, out GlassV2 a, out GlassV2 b)
    {
        double t0 = 0.0, t1 = 1.0;
        var dx = q.x - p.x;
        var dy = q.y - p.y;
        a = b = default(GlassV2);
        for (var k = 0; k < 4; k++)
        {
            double pp, qq;
            switch (k)
            {
                case 0: pp = -dx; qq = p.x - x0; break;
                case 1: pp = dx; qq = x1 - p.x; break;
                case 2: pp = -dy; qq = p.y - y0; break;
                default: pp = dy; qq = y1 - p.y; break;
            }
            if (pp == 0.0)
            {
                if (qq < 0.0)
                    return false;
                continue;
            }
            var r = qq / pp;
            if (pp < 0.0)
            {
                if (r > t1)
                    return false;
                if (r > t0)
                    t0 = r;
            }
            else
            {
                if (r < t0)
                    return false;
                if (r < t1)
                    t1 = r;
            }
        }
        a = new GlassV2(p.x + t0 * dx, p.y + t0 * dy);
        b = new GlassV2(p.x + t1 * dx, p.y + t1 * dy);
        return true;
    }

    readonly List<int> eU = new List<int>(), eV = new List<int>();

    bool CheckCrossings()
    {
        var hx = fr.hx;
        var hy = fr.hy;
        // box = (-hx - 0.05, hx + 0.05, -hy - 0.05, hy + 0.05); limit_box = box shifted by the impact
        double b0 = -hx - 0.05, b1 = hx + 0.05, b2 = -hy - 0.05, b3 = hy + 0.05;
        eU.Clear();
        eV.Clear();
        for (var i = 0; i < edges.Count; i++)
        {
            eU.Add(edges[i].u);
            eV.Add(edges[i].v);
        }
        GraphCrossings(V, eU, eV, b0 - ix, b1 - ix, b2 - iy, b3 - iy, true, boxes, found);
        if (found.Count == 0)
            return false;
        var ea = (int)(found[0] >> 32);
        var eb = (int)(found[0] & 0xFFFFFFFFL);
        if (RepairCrossing(edges[ea], edges[eb]))
            return true;
        lastErr = "unrepairable crossing";
        supDamp();
        return true;
    }

    // _repair_crossing: drop a chord of the crossing pair, else the younger fork, else damp both tracks' kinks
    bool RepairCrossing(EdgeW A, EdgeW B)
    {
        for (var k = 0; k < 2; k++)
        {
            var E = k == 0 ? A : B;
            if (E.owner.kind == GlassOwner.Chord)
            {
                var key = new Key3(E.owner.a, E.owner.b, E.owner.c);
                if (!suppChords.Contains(key))
                {
                    suppChords.Add(key);
                    AddRepair(GlassRepairKind.CrossingDropChord, E.owner, default(GlassOwner), 0.0, 0, null);
                    return true;
                }
            }
        }
        for (var k = 0; k < 2; k++)
        {
            var E = k == 0 ? A : B;
            if (E.owner.kind == GlassOwner.Track)
            {
                var t = byTid[E.owner.a];
                if (!t.initial)
                {
                    var fk = FindForkOfChild(t.tid);
                    if (!suppForks.Contains(fk.key))
                    {
                        suppForks.Add(fk.key);
                        AddRepair(GlassRepairKind.CrossingDropFork, fk.key.Owner, default(GlassOwner), 0.0, 0, null);
                        return true;
                    }
                }
            }
        }
        var done = false;
        for (var k = 0; k < 2; k++)
        {
            var E = k == 0 ? A : B;
            if (E.owner.kind == GlassOwner.Track)
            {
                double cur;
                if (!kinkScale.TryGetValue(E.owner.a, out cur))
                    cur = 1.0;
                if (cur > 0.0)
                {
                    kinkScale[E.owner.a] = cur <= 0.5 ? 0.0 : 0.5;
                    done = true;
                }
            }
        }
        if (done)
            AddRepair(GlassRepairKind.CrossingDampKinks, A.owner, B.owner, 0.0, 0, null);
        return done;
    }

    ForkW FindForkOfChild(int tid)
    {
        for (var i = 0; i < forks.Count; i++)
            if (forks[i].child == tid)
                return forks[i];
        throw new InvalidOperationException("fork of track " + tid + " not found");
    }

    // ---------------------------------------------------------------------------------------------------------
    // _faces + raw pieces
    // ---------------------------------------------------------------------------------------------------------
    GlassPySet SetOf(int v)
    {
        while (nbrSets.Count <= v)
        {
            nbrSets.Add(new GlassPySet());
            nbrHas.Add(false);
        }
        if (!nbrHas[v])
        {
            nbrHas[v] = true;
            nbrSets[v].Clear();
            nbrKeys.Add(v);
        }
        return nbrSets[v];
    }

    void FaceAdjacency()
    {
        // bounded faces of the planar straight-line graph (CCW), as vertex-id lists
        for (var i = 0; i < nbrKeys.Count; i++)
            nbrHas[nbrKeys[i]] = false;
        nbrKeys.Clear();
        for (var i = 0; i < edges.Count; i++)
        {
            var u = edges[i].u;
            var v = edges[i].v;
            if (u == v)
                continue;
            SetOf(u).Add(v);
            SetOf(v).Add(u);
        }
        // order[v]: neighbours sorted by angle (stable, from set order)
        while (ordStart.Count < V.Count)
        {
            ordStart.Add(0);
            ordCount.Add(0);
        }
        ordList.Clear();
        for (var ki = 0; ki < nbrKeys.Count; ki++)
        {
            var v = nbrKeys[ki];
            var vx = V[v].x;
            var vy = V[v].y;
            setScratch.Clear();
            nbrSets[v].CopyTo(setScratch);
            angScratch.Clear();
            for (var i = 0; i < setScratch.Count; i++)
                angScratch.Add(GlassMath.Atan2(V[setScratch[i]].y - vy, V[setScratch[i]].x - vx));
            // stable insertion sort by angle
            for (var i = 1; i < setScratch.Count; i++)
            {
                var a = angScratch[i];
                var id = setScratch[i];
                var j = i - 1;
                while (j >= 0 && angScratch[j] > a)
                {
                    angScratch[j + 1] = angScratch[j];
                    setScratch[j + 1] = setScratch[j];
                    j--;
                }
                angScratch[j + 1] = a;
                setScratch[j + 1] = id;
            }
            ordStart[v] = ordList.Count;
            ordCount[v] = setScratch.Count;
            ordList.AddRange(setScratch);
        }
    }

    // _faces(): the walk runs over nbrKeys in order; FacesRange(from, to) walks the faces that start at keys
    // [from, to). Resuming at the next key with the same seen set gives the same faces in the same order, so the
    // walk can be split across slices (FaceWalkKeysPerStep) without changing the output.
    void FacesBegin()
    {
        seen.Clear();
        faceVids.Clear();
        faceStart.Clear();
        faceCount.Clear();
        faceKi = 0;
    }

    void FacesRange(int from, int to)
    {
        for (var ki = from; ki < to; ki++)
        {
            var u = nbrKeys[ki];
            setScratch.Clear();
            nbrSets[u].CopyTo(setScratch);
            for (var si = 0; si < setScratch.Count; si++)
            {
                var v = setScratch[si];
                if (seen.Contains(DirKey(u, v)))
                    continue;
                var f0 = faceVids.Count;
                int a = u, b = v;
                var guard = 0;
                while (!seen.Contains(DirKey(a, b)))
                {
                    seen.Add(DirKey(a, b));
                    faceVids.Add(a);
                    var s0 = ordStart[b];
                    var sn = ordCount[b];
                    var i = -1;
                    for (var q = 0; q < sn; q++)
                        if (ordList[s0 + q] == a)
                        {
                            i = q;
                            break;
                        }
                    var c = ordList[s0 + ((i - 1) % sn + sn) % sn];
                    a = b;
                    b = c;
                    if (++guard > 100000)
                        throw new InvalidOperationException("face walk did not close");
                }
                faceStart.Add(f0);
                faceCount.Add(faceVids.Count - f0);
            }
        }
    }

    Raw NewRaw()
    {
        Raw r;
        if (rawUsed < rawPool.Count)
            r = rawPool[rawUsed];
        else
        {
            r = new Raw();
            rawPool.Add(r);
        }
        rawUsed++;
        return r;
    }

    // one slice of the walk; true when every face is walked (FaceAdjacency and FacesBegin ran in the previous slice)
    bool FaceWalk()
    {
        var end = Math.Min(nbrKeys.Count, faceKi + FaceWalkKeysPerStep);
        FacesRange(faceKi, end);
        faceKi = end;
        if (faceKi < nbrKeys.Count)
            return false;
        while (isCoreV.Count < V.Count)
            isCoreV.Add(false);
        for (var i = 0; i < V.Count; i++)
            isCoreV[i] = false;
        for (var i = 0; i < coreVids.Count; i++)
            isCoreV[coreVids[i]] = true;
        raw.Clear();
        rawUsed = 0;
        return true;
    }

    // one face -> a raw piece in slab coordinates; false = a non-convex face (bad_face: damp the kinks and rebuild)
    bool FacePiece(int fi)
    {
        var hx = fr.hx;
        var hy = fr.hy;
        var f0 = faceStart[fi];
        var fn = faceCount[fi];
        // keep faces with area > 0 (impact-relative coords)
        var s = 0.0;
        for (var i = 0; i < fn; i++)
        {
            var p = V[faceVids[f0 + i]];
            var q = V[faceVids[f0 + (i + 1) % fn]];
            s += p.x * q.y - q.x * p.y;
        }
        if (!(0.5 * s > 0.0))
            return true;
        var poly = Rent();
        for (var i = 0; i < fn; i++)
        {
            var p = V[faceVids[f0 + i]];
            poly.Add(new GlassV2(p.x + ix, p.y + iy));
        }
        var cp = Rent();
        GlassPoly.Clean(poly, cp);
        var cl = Rent();
        ClipRect(cp, -hx, hx, -hy, hy, cl);
        var cl2 = Rent();
        GlassPoly.Clean(cl, cl2);
        if (cl2.Count < 3 || GlassPoly.Area(cl2) <= 1e-14)
            return true;                                    // wholly outside the slab
        if (!GlassPoly.IsConvex(cp, 1e-12))
            return false;
        var isCore = fn == coreVids.Count;
        if (isCore)
            for (var i = 0; i < fn; i++)
                if (!isCoreV[faceVids[f0 + i]])
                {
                    isCore = false;
                    break;
                }
        var r = NewRaw();
        r.poly = cl2;
        r.kind = isCore ? GlassPieceKind.Crush : GlassPieceKind.Shard;
        r.f0 = f0;
        r.fn = fn;
        raw.Add(r);
        return true;
    }

    /// <summary>clip_rect into dst (empty when any intermediate has fewer than 3 vertices).</summary>
    void ClipRect(List<GlassV2> poly, double x0, double x1, double y0, double y1, List<GlassV2> dst)
    {
        dst.Clear();
        GlassPoly.ClipHalf(poly, 1.0, 0.0, x1, sA);
        if (sA.Count < 3)
            return;
        GlassPoly.ClipHalf(sA, -1.0, 0.0, -x0, sB);
        if (sB.Count < 3)
            return;
        GlassPoly.ClipHalf(sB, 0.0, 1.0, y1, sA);
        if (sA.Count < 3)
            return;
        GlassPoly.ClipHalf(sA, 0.0, -1.0, -y0, dst);
        if (dst.Count < 3)
            dst.Clear();
    }

    static long PieceKeyPacked(List<GlassV2> poly)
    {
        long k1, k2;
        PieceKey(poly, out k1, out k2);
        return (k1 << 32) ^ (k2 & 0xFFFFFFFFL);
    }

    // _piece_key(poly) = (int(round((c.x + 10) * 10000)), int(round((c.y + 10) * 10000)))
    static void PieceKey(List<GlassV2> poly, out long k1, out long k2)
    {
        var c = GlassPoly.Centroid(poly);
        k1 = GlassMath.RoundHalfEven((c.x + 10.0) * 10000.0);
        k2 = GlassMath.RoundHalfEven((c.y + 10.0) * 10000.0);
    }

    static int CompareSmall(List<GlassV2> a, List<GlassV2> b)
    {
        var aa = GlassPoly.Area(a);
        var ab = GlassPoly.Area(b);
        if (aa != ab) return aa < ab ? -1 : 1;
        long a1, a2, b1, b2;
        PieceKey(a, out a1, out a2);
        PieceKey(b, out b1, out b2);
        if (a1 != b1) return a1 < b1 ? -1 : 1;
        if (a2 != b2) return a2 < b2 ? -1 : 1;
        return 0;
    }

    // ---------------------------------------------------------------------------------------------------------
    // small faces (< min_piece_area inside the slab): drop a bounding chord, else a fork, else merge
    // ---------------------------------------------------------------------------------------------------------
    // the small-face repair of generate(): the batch on the first slice, then one round of the "unfixed" loop per slice.
    // Returns true to rebuild; sets Next = PostDaggers when no small face is left.
    bool SmallStep()
    {
        if (smallState == 0)
        {
            // batch: every small face drops its own outermost chord in the same rebuild (keyed RNG keeps the rest)
            smallList.Clear();
            for (var i = 0; i < raw.Count; i++)
                if (raw[i].kind != GlassPieceKind.Crush && GlassPoly.Area(raw[i].poly) < P.minPieceArea)
                    smallList.Add(raw[i]);
            StableSortSmall(smallList);
            batch.Clear();
            for (var i = 0; i < smallList.Count; i++)
            {
                Key3 ch;
                if (OutermostChord(smallList[i], true, out ch))
                    batch.Add(ch);
            }
            if (batch.Count > 0)
            {
                for (var i = 0; i < batch.Count; i++)
                {
                    suppChords.Add(batch[i]);
                    AddRepair(GlassRepairKind.SmallDropChord, batch[i].Owner, default(GlassOwner), 0.0, 0, null);
                }
                return true;
            }
            unfixed.Clear();
            smallState = 1;
            if (smallList.Count > 0)
                return false;      // small faces to repair: the rounds run in the next slices
        }
        Raw best = null;
        for (var i = 0; i < raw.Count; i++)
        {
            var r = raw[i];
            if (r.kind == GlassPieceKind.Crush || GlassPoly.Area(r.poly) >= P.minPieceArea)
                continue;
            if (unfixed.Contains(PieceKeyPacked(r.poly)))
                continue;
            if (best == null || CompareSmall(r.poly, best.poly) < 0)
                best = r;
        }
        if (best == null)
        {
            pieces.Clear();
            pieceUsed = 0;
            shardParts.Clear();
            cursor = 0;
            Next = Phase.PostDaggers;
            return false;
        }
        var nBefore = raw.Count;
        if (RepairSmall(best))
            return true;
        if (raw.Count == nBefore)
            unfixed.Add(PieceKeyPacked(best.poly));
        return false;
    }

    void StableSortSmall(List<Raw> list)
    {
        // insertion sort: stable, lists are short
        for (var i = 1; i < list.Count; i++)
        {
            var x = list[i];
            var j = i - 1;
            while (j >= 0 && CompareSmall(list[j].poly, x.poly) > 0)
            {
                list[j + 1] = list[j];
                j--;
            }
            list[j + 1] = x;
        }
    }

    // _outermost_chord: the outermost ring chord bounding face r that is not suppressed or already taken
    bool OutermostChord(Raw r, bool checkTaken, out Key3 best)
    {
        best = default(Key3);
        var have = false;
        var bestD = 0.0;
        for (var i = 0; i < r.fn; i++)
        {
            var u = faceVids[r.f0 + i];
            var v = faceVids[r.f0 + (i + 1) % r.fn];
            var o = OwnerOf(u, v);
            if (o.kind != GlassOwner.Chord)
                continue;
            var key = new Key3(o.a, o.b, o.c);
            if (suppChords.Contains(key))
                continue;
            if (checkTaken && batch.Contains(key))
                continue;
            var a = V[u];
            var b = V[v];
            var d = GlassMath.Hypot(0.5 * (a.x + b.x), 0.5 * (a.y + b.y));
            // cand.sort(reverse=True)[0] = max by (d, owner)
            if (!have || d > bestD || (d == bestD && Key3.Compare(key, best) > 0))
            {
                have = true;
                bestD = d;
                best = key;
            }
        }
        return have;
    }

    // _repair_small: returns true to rebuild
    bool RepairSmall(Raw r)
    {
        Key3 ch;
        if (OutermostChord(r, false, out ch))
        {
            suppChords.Add(ch);
            AddRepair(GlassRepairKind.SmallDropChord, ch.Owner, default(GlassOwner), 0.0, 0, null);
            return true;
        }
        for (var i = 0; i < r.fn; i++)
        {
            var o = OwnerOf(faceVids[r.f0 + i], faceVids[r.f0 + (i + 1) % r.fn]);
            if (o.kind != GlassOwner.Track)
                continue;
            var t = byTid[o.a];
            if (t.initial)
                continue;
            var fk = FindForkOfChild(t.tid);
            if (!suppForks.Contains(fk.key))
            {
                suppForks.Add(fk.key);
                AddRepair(GlassRepairKind.SmallDropFork, fk.key.Owner, default(GlassOwner), 0.0, 0, null);
                return true;
            }
        }
        // others = sorted([o for o in raw if o is not r and o kind != crush], key=piece_key) -- stable
        smallList.Clear();
        keyA.Clear();
        keyB.Clear();
        for (var i = 0; i < raw.Count; i++)
            if (raw[i] != r && raw[i].kind != GlassPieceKind.Crush)
            {
                long k1, k2;
                PieceKey(raw[i].poly, out k1, out k2);
                smallList.Add(raw[i]);
                keyA.Add(k1);
                keyB.Add(k2);
            }
        // stable insertion sort by the piece key (keys computed once)
        for (var i = 1; i < smallList.Count; i++)
        {
            var x = smallList[i];
            var x1 = keyA[i];
            var x2 = keyB[i];
            var j = i - 1;
            while (j >= 0 && (keyA[j] > x1 || (keyA[j] == x1 && keyB[j] > x2)))
            {
                smallList[j + 1] = smallList[j];
                keyA[j + 1] = keyA[j];
                keyB[j + 1] = keyB[j];
                j--;
            }
            smallList[j + 1] = x;
            keyA[j + 1] = x1;
            keyB[j + 1] = x2;
        }
        for (var i = 0; i < smallList.Count; i++)
        {
            var other = smallList[i];
            if (!SharesEdge(r.poly, other.poly))
                continue;
            var m = MergeConvex(r.poly, other.poly);
            if (m != null)
            {
                other.poly = m;
                raw.Remove(r);
                AddRepair(GlassRepairKind.SmallMergeNeighbour, default(GlassOwner), default(GlassOwner),
                    GlassMath.PyRound(GlassPoly.Area(r.poly) * 1e4, 4), 0, null);
                return false;
            }
        }
        AddRepair(GlassRepairKind.SmallUnfixed, default(GlassOwner), default(GlassOwner),
            GlassMath.PyRound(GlassPoly.Area(r.poly) * 1e4, 4), 0, null);
        return false;
    }

    static bool SharesEdge(List<GlassV2> a, List<GlassV2> b, double tol = 1e-9)
    {
        for (var i = 0; i < a.Count; i++)
        {
            var p = a[i];
            var q = a[(i + 1) % a.Count];
            var ex = q.x - p.x;
            var ey = q.y - p.y;
            var l2 = ex * ex + ey * ey;
            if (l2 < 1e-18)
                continue;
            for (var j = 0; j < b.Count; j++)
            {
                var r = b[j];
                var s = b[(j + 1) % b.Count];
                if (Math.Abs(GlassPoly.Orient(p, q, r)) > tol * Math.Sqrt(l2) || Math.Abs(GlassPoly.Orient(p, q, s)) > tol * Math.Sqrt(l2))
                    continue;
                var t0 = ((r.x - p.x) * ex + (r.y - p.y) * ey) / l2;
                var t1 = ((s.x - p.x) * ex + (s.y - p.y) * ey) / l2;
                var lo = Math.Max(0.0, Math.Min(t0, t1));
                var hi = Math.Min(1.0, Math.Max(t0, t1));
                if ((hi - lo) * Math.Sqrt(l2) > 1e-6)
                    return true;
            }
        }
        return false;
    }

    readonly List<long> keyA = new List<long>(), keyB = new List<long>();

    List<GlassV2> MergeConvex(List<GlassV2> a, List<GlassV2> b)
    {
        sC.Clear();
        sC.AddRange(a);
        sC.AddRange(b);
        var hull = Rent();
        GlassPoly.ConvexHull(sC, sA, sB, hull);
        if (hull.Count >= 3 && Math.Abs(GlassPoly.Area(hull) - (GlassPoly.Area(a) + GlassPoly.Area(b))) < 1e-10)
        {
            var c = Rent();
            GlassPoly.Clean(hull, c);
            return c;
        }
        return null;
    }

    // ---------------------------------------------------------------------------------------------------------
    // _post_process: daggers -> teeth -> level-2 sub-cells
    // ---------------------------------------------------------------------------------------------------------
    PieceW NewPiece(List<GlassV2> poly, GlassPieceKind kind, char side, GlassCorner corner)
    {
        PieceW p;
        if (pieceUsed < piecePool.Count)
            p = piecePool[pieceUsed];
        else
        {
            p = new PieceW();
            piecePool.Add(p);
        }
        pieceUsed++;
        p.poly = poly;
        p.kind = kind;
        p.side = side;
        p.corner = corner;
        p.sub.Clear();
        return p;
    }

    // _post_process, second loop: one shard (after the dagger split) -> teeth or a free shard
    void ShardTeeth(List<GlassV2> poly)
    {
        double cL, cR, cT, cB;
        Contacts(poly, out cL, out cR, out cT, out cB);
        if (PyMax(PyMax(PyMax(cL, cR), cT), cB) < 0.005)
        {
            pieces.Add(NewPiece(poly, GlassPieceKind.Shard, '\0', GlassCorner.None));
            return;
        }
        long k1, k2;
        PieceKey(poly, out k1, out k2);
        var first = FirstSide(cL, cR, cT, cB);
        // long contact: cracks into the frame first (each part keeps its own tooth)
        splitOut.Clear();
        var rs = GlassRng.Make(seed, S_SPLIT, 7, k1, k2);
        SplitLongForTeeth(poly, first, ref rs, splitOut);
        for (var j = 0; j < splitOut.Count; j++)
            TeethFor(splitOut[j]);
    }

    // _post_process, third loop: level-2 sub-cells for the floor re-break (desktop only)
    void Level2For(PieceW p)
    {
        p.sub.Clear();
        if (p.kind == GlassPieceKind.Shard && P.level2Area > 0.0 && GlassPoly.Area(p.poly) > P.level2Area)
        {
            splitOut2.Clear();
            Level2(p.poly, 0, splitOut2);
            if (splitOut2.Count > 1)
                p.sub.AddRange(splitOut2);
        }
    }

    // order = sorted(c.keys(), key=lambda s: (-c[s], s)); first
    static char FirstSide(double cL, double cR, double cT, double cB)
    {
        // keys sorted by name: B < L < R < T; the largest contact wins, ties by name
        var best = 'B';
        var bv = cB;
        if (cL > bv) { best = 'L'; bv = cL; }
        if (cR > bv) { best = 'R'; bv = cR; }
        if (cT > bv) { best = 'T'; bv = cT; }
        return best;
    }

    void Contacts(List<GlassV2> poly, out double cL, out double cR, out double cT, out double cB)
    {
        var hx = fr.hx;
        var hy = fr.hy;
        cL = cR = cT = cB = 0.0;
        var n = poly.Count;
        for (var i = 0; i < n; i++)
        {
            var p = poly[i];
            var q = poly[(i + 1) % n];
            var L = GlassMath.Hypot(q.x - p.x, q.y - p.y);
            if (Math.Abs(p.x + hx) < 1e-9 && Math.Abs(q.x + hx) < 1e-9)
                cL += L;
            if (Math.Abs(p.x - hx) < 1e-9 && Math.Abs(q.x - hx) < 1e-9)
                cR += L;
            if (Math.Abs(p.y - hy) < 1e-9 && Math.Abs(q.y - hy) < 1e-9)
                cT += L;
            if (Math.Abs(p.y + hy) < 1e-9 && Math.Abs(q.y + hy) < 1e-9)
                cB += L;
        }
    }

    double ContactOf(List<GlassV2> poly, char side)
    {
        double cL, cR, cT, cB;
        Contacts(poly, out cL, out cR, out cT, out cB);
        return side == 'L' ? cL : side == 'R' ? cR : side == 'T' ? cT : cB;
    }

    double InClearArea(List<GlassV2> poly)
    {
        if (poly.Count == 0)
            return 0.0;
        ClipRect(poly, fr.clearX0, fr.clearX1, fr.clearY0, fr.clearY1, sD);
        return GlassPoly.Area(sD);
    }

    // _side_frames: d = n.p + off (depth from the glass edge, inward), u = t.p (along the side)
    void SideFrame(char side, out double nx, out double ny, out double off, out double tx, out double ty)
    {
        switch (side)
        {
            case 'L': nx = 1.0; ny = 0.0; off = fr.hx; tx = 0.0; ty = 1.0; break;
            case 'R': nx = -1.0; ny = 0.0; off = fr.hx; tx = 0.0; ty = 1.0; break;
            case 'T': nx = 0.0; ny = -1.0; off = fr.hy; tx = 1.0; ty = 0.0; break;
            default: nx = 0.0; ny = 1.0; off = fr.hy; tx = 1.0; ty = 0.0; break;
        }
    }

    void SplitByLine(List<GlassV2> poly, double px, double py, double dx, double dy, List<GlassV2> left, List<GlassV2> right)
    {
        var nx = -dy;
        var ny = dx;
        var c = nx * px + ny * py;
        GlassPoly.ClipHalf(poly, -nx, -ny, -c, left);     // n.p >= c
        GlassPoly.ClipHalf(poly, nx, ny, c, right);       // n.p <= c
    }

    void SplitDagger(List<GlassV2> poly, List<List<GlassV2>> outParts)
    {
        int i, j;
        var d = GlassPoly.Diameter(poly, out i, out j);
        if (d <= P.daggerMax)
        {
            outParts.Add(poly);
            return;
        }
        long k1, k2;
        PieceKey(poly, out k1, out k2);
        var rng = GlassRng.Make(seed, S_SPLIT, k1, k2);
        var f = rng.Uniform(P.daggerSplitMin, P.daggerSplitMax);
        var ax = poly[j].x - poly[i].x;
        var ay = poly[j].y - poly[i].y;
        var L = GlassMath.Hypot(ax, ay);
        ax = ax / L;
        ay = ay / L;
        var px = poly[i].x + ax * f * L;
        var py = poly[i].y + ay * f * L;
        var ang = GlassMath.Radians(90.0 + rng.Uniform(-P.daggerSplitTiltDeg, P.daggerSplitTiltDeg));
        var dx = ax * GlassMath.Cos(ang) - ay * GlassMath.Sin(ang);
        var dy = ax * GlassMath.Sin(ang) + ay * GlassMath.Cos(ang);
        var l0 = Rent();
        var r0 = Rent();
        SplitByLine(poly, px, py, dx, dy, l0, r0);
        var left = Rent();
        var right = Rent();
        GlassPoly.Clean(l0, left);
        GlassPoly.Clean(r0, right);
        if (left.Count < 3 || right.Count < 3)
        {
            outParts.Add(poly);
            return;
        }
        SplitDagger(left, outParts);
        SplitDagger(right, outParts);
    }

    // _split_long_for_teeth: cracks into the frame so no tooth runs longer than tooth_max_len along its side
    void SplitLongForTeeth(List<GlassV2> poly, char side, ref GlassRng rng, List<List<GlassV2>> outParts)
    {
        double nx, ny, off, tx, ty;
        SideFrame(side, out nx, out ny, out off, out tx, out ty);
        var D = fr.Band(side);
        GlassPoly.ClipHalf(poly, nx, ny, D - off, sA);
        if (sA.Count < 3)
        {
            outParts.Add(poly);
            return;
        }
        var u0 = double.PositiveInfinity;
        var u1 = double.NegativeInfinity;
        for (var i = 0; i < sA.Count; i++)
        {
            var u = tx * sA[i].x + ty * sA[i].y;
            u0 = i == 0 ? u : PyMin(u0, u);
            u1 = i == 0 ? u : PyMax(u1, u);
        }
        if (u1 - u0 <= P.toothMaxLen)
        {
            outParts.Add(poly);
            return;
        }
        var f = rng.Uniform(0.35, 0.65);
        var us = u0 + f * (u1 - u0);
        var ang = GlassMath.Radians(rng.Uniform(-P.toothSplitTiltDeg, P.toothSplitTiltDeg));
        // point on the glass edge at u = us, direction = inward normal rotated by ang
        var px = -nx * off + tx * us;
        var py = -ny * off + ty * us;
        var dx = nx * GlassMath.Cos(ang) - ny * GlassMath.Sin(ang);
        var dy = nx * GlassMath.Sin(ang) + ny * GlassMath.Cos(ang);
        var l0 = Rent();
        var r0 = Rent();
        SplitByLine(poly, px, py, dx, dy, l0, r0);
        var left = Rent();
        var right = Rent();
        GlassPoly.Clean(l0, left);
        GlassPoly.Clean(r0, right);
        if (left.Count < 3 || right.Count < 3 || GlassPoly.Area(left) < P.minPieceArea || GlassPoly.Area(right) < P.minPieceArea)
        {
            outParts.Add(poly);
            return;
        }
        var rl = GlassRng.Make(rng.NextU32(), S_SPLIT, 1);
        SplitLongForTeeth(left, side, ref rl, outParts);
        var rr = GlassRng.Make(rng.NextU32(), S_SPLIT, 2);
        SplitLongForTeeth(right, side, ref rr, outParts);
    }

    void Level2(List<GlassV2> poly, int depth, List<List<GlassV2>> outParts)
    {
        if (P.level2Area <= 0.0 || GlassPoly.Area(poly) <= P.level2Area || depth >= P.level2MaxDepth)
        {
            outParts.Add(poly);
            return;
        }
        long k1, k2;
        PieceKey(poly, out k1, out k2);
        var rng = GlassRng.Make(seed, S_LEVEL2, depth, k1, k2);
        int i, j;
        GlassPoly.Diameter(poly, out i, out j);
        var ax = poly[j].x - poly[i].x;
        var ay = poly[j].y - poly[i].y;
        var L = GlassMath.Hypot(ax, ay);
        ax = ax / L;
        ay = ay / L;
        var f = rng.Uniform(0.35, 0.65);
        var px = poly[i].x + ax * f * L;
        var py = poly[i].y + ay * f * L;
        var ang = GlassMath.Radians(90.0 + rng.Uniform(-30.0, 30.0));
        var dx = ax * GlassMath.Cos(ang) - ay * GlassMath.Sin(ang);
        var dy = ax * GlassMath.Sin(ang) + ay * GlassMath.Cos(ang);
        var l0 = Rent();
        var r0 = Rent();
        SplitByLine(poly, px, py, dx, dy, l0, r0);
        var left = Rent();
        var right = Rent();
        GlassPoly.Clean(l0, left);
        GlassPoly.Clean(r0, right);
        if (left.Count < 3 || right.Count < 3 || GlassPoly.Area(left) < P.minPieceArea || GlassPoly.Area(right) < P.minPieceArea)
        {
            outParts.Add(poly);
            return;
        }
        Level2(left, depth + 1, outParts);
        Level2(right, depth + 1, outParts);
    }

    GlassCorner CornerOf(List<GlassV2> poly, double tol = 1e-9)
    {
        var hx = fr.hx;
        var hy = fr.hy;
        for (var c = 0; c < 4; c++)
        {
            double cx, cy;
            GlassCorner name;
            switch (c)
            {
                case 0: name = GlassCorner.TL; cx = -hx; cy = hy; break;
                case 1: name = GlassCorner.TR; cx = hx; cy = hy; break;
                case 2: name = GlassCorner.BL; cx = -hx; cy = -hy; break;
                default: name = GlassCorner.BR; cx = hx; cy = -hy; break;
            }
            for (var i = 0; i < poly.Count; i++)
                if (Math.Abs(poly[i].x - cx) <= tol && Math.Abs(poly[i].y - cy) <= tol)
                    return name;
        }
        return GlassCorner.None;
    }

    // _corner_cut: a tooth across a slab corner (legs a multiple of the band depth; the clear zone's corner stays out)
    void CornerCut(List<GlassV2> poly, GlassCorner corner, ref GlassRng rng, List<GlassV2> tooth, List<GlassV2> rest)
    {
        var hx = fr.hx;
        var hy = fr.hy;
        var sx = (corner == GlassCorner.TR || corner == GlassCorner.BR) ? 1.0 : -1.0;
        var sy = (corner == GlassCorner.TL || corner == GlassCorner.TR) ? 1.0 : -1.0;
        var sideX = sx > 0 ? 'R' : 'L';      // the jamb (vertical edge) of this corner
        var sideY = sy > 0 ? 'T' : 'B';      // the head or sill (horizontal edge)
        var bx = fr.Band(sideX);
        var by = fr.Band(sideY);
        var lo = P.cornerLegMin;
        var hi = P.cornerLegMax;
        var la = rng.Uniform(lo, hi);
        var lb = rng.Uniform(lo, hi);
        if (1.0 / la + 1.0 / lb <= 1.02)
            lb = 1.02 - 1.0 / la > 1e-6 ? 1.0 / (1.02 - 1.0 / la) : hi;
        var a = la * bx;                     // along the horizontal edge, from the corner
        var b = lb * by;                     // along the vertical edge, from the corner
        var cx = sx * hx;
        var cy = sy * hy;
        var p1x = cx - sx * a;               // on the horizontal edge
        var p1y = cy;
        var p2x = cx;                        // on the vertical edge
        var p2y = cy - sy * b;
        // half-plane containing the corner: n.p >= n.p1 with n pointing to the corner
        var ex = p2x - p1x;
        var ey = p2y - p1y;
        var nx = -ey;
        var ny = ex;
        if (nx * (cx - p1x) + ny * (cy - p1y) < 0.0)
        {
            nx = -nx;
            ny = -ny;
        }
        var c = nx * p1x + ny * p1y;
        GlassPoly.ClipHalf(poly, -nx, -ny, -c, sA);
        GlassPoly.Clean(sA, tooth);
        GlassPoly.ClipHalf(poly, nx, ny, c, sB);
        GlassPoly.Clean(sB, rest);
    }

    // _tooth_cut: false = None; the cut is a tilted crack that stays within the side's band
    bool ToothCut(List<GlassV2> poly, char side, ref GlassRng rng, List<GlassV2> tooth, List<GlassV2> rest)
    {
        double nx, ny, off, tx, ty;
        SideFrame(side, out nx, out ny, out off, out tx, out ty);
        var D = fr.Band(side);
        var pk = fr.Pocket(side);
        // part of the piece inside the band depth
        GlassPoly.ClipHalf(poly, nx, ny, D - off, sA);
        if (sA.Count < 3 || GlassPoly.Area(sA) < 1e-10)
            return false;
        var u0 = 0.0;
        var u1 = 0.0;
        for (var i = 0; i < sA.Count; i++)
        {
            var u = tx * sA[i].x + ty * sA[i].y;
            u0 = i == 0 ? u : PyMin(u0, u);
            u1 = i == 0 ? u : PyMax(u1, u);
        }
        if (u1 - u0 < 0.01)
            return false;
        var vis = P.toothVisMin;
        var loD = pk + vis;
        if (loD >= D)
            return false;
        var tri = rng.U01() < P.toothTriP;
        var a1 = rng.U01();
        var a2 = rng.U01();
        var flip = rng.U01();
        var span = D - pk;
        double d0, d1;
        if (tri)
        {
            // a pointed tooth: the cut runs from (nearly) the stop line at one end to (nearly) the band at the other
            d0 = pk + 0.25 * span * a1;
            d1 = D - 0.10 * span * a2;
            if (flip < 0.5)
            {
                var t = d0;
                d0 = d1;
                d1 = t;
            }
        }
        else
        {
            // a blunt stub: both ends 30-100 % of the way into the band
            d0 = pk + (0.30 + 0.70 * a1) * span;
            d1 = pk + (0.30 + 0.70 * a2) * span;
        }
        if (PyMax(d0, d1) < loD)
            return false;
        var s = (d1 - d0) / (u1 - u0);
        // tooth: n.p + off - d0 - s (t.p - u0) <= 0
        var a = nx - s * tx;
        var b = ny - s * ty;
        var c = d0 - off - s * u0;
        GlassPoly.ClipHalf(poly, a, b, c, sA);
        GlassPoly.ClipHalf(poly, -a, -b, -c, sB);
        GlassPoly.Clean(sA, tooth);
        GlassPoly.Clean(sB, rest);
        return true;
    }

    // _teeth_for: teeth that stay in the stop (a subset of the frame pieces, clipped to the tooth band)
    void TeethFor(List<GlassV2> poly)
    {
        double cL, cR, cT, cB;
        Contacts(poly, out cL, out cR, out cT, out cB);
        if (PyMax(PyMax(PyMax(cL, cR), cT), cB) < 0.005)
        {
            pieces.Add(NewPiece(poly, GlassPieceKind.Shard, '\0', GlassCorner.None));
            return;
        }
        long k1, k2;
        PieceKey(poly, out k1, out k2);
        var rng = GlassRng.Make(seed, S_TEETH, 1, k1, k2);
        var corner = CornerOf(poly);
        if (corner != GlassCorner.None && P.cornerToothP > 0.0 && fr.bandMode == "band")
        {
            var rc = GlassRng.Make(seed, S_TEETH, 3, k1, k2);
            if (rc.U01() < P.cornerToothP)
            {
                for (var at = 0; at < 3; at++)
                {
                    var tooth = Rent();
                    var rest = Rent();
                    var rcut = GlassRng.Make(seed, S_TEETH, 4, at, k1, k2);
                    CornerCut(poly, corner, ref rcut, tooth, rest);
                    if (tooth.Count < 3 || GlassPoly.Area(tooth) < P.minPieceArea || InClearArea(tooth) > 1e-12)
                        continue;
                    var cornerSide = (corner == GlassCorner.TR || corner == GlassCorner.BR) ? 'R' : 'L';
                    if (rest.Count >= 3 && GlassPoly.Area(rest) >= P.minPieceArea)
                    {
                        pieces.Add(NewPiece(tooth, GlassPieceKind.Tooth, cornerSide, corner));
                        pieces.Add(NewPiece(rest, GlassPieceKind.Shard, '\0', GlassCorner.None));
                        return;
                    }
                    if (rest.Count < 3 || GlassPoly.Area(rest) < 1e-10)
                    {
                        pieces.Add(NewPiece(tooth, GlassPieceKind.Tooth, cornerSide, corner));
                        return;
                    }
                }
            }
        }
        // sides = sorted([s for s in c if c[s] >= 0.005], key=lambda s: (-c[s], s))
        sidesTmp.Clear();
        if (cB >= 0.005) sidesTmp.Add('B');
        if (cL >= 0.005) sidesTmp.Add('L');
        if (cR >= 0.005) sidesTmp.Add('R');
        if (cT >= 0.005) sidesTmp.Add('T');
        for (var i = 1; i < sidesTmp.Count; i++)
        {
            var x = sidesTmp[i];
            var xv = x == 'L' ? cL : x == 'R' ? cR : x == 'T' ? cT : cB;
            var j = i - 1;
            while (j >= 0)
            {
                var y = sidesTmp[j];
                var yv = y == 'L' ? cL : y == 'R' ? cR : y == 'T' ? cT : cB;
                if (yv < xv)      // -yv > -xv: y sorts after x
                {
                    sidesTmp[j + 1] = sidesTmp[j];
                    j--;
                }
                else
                    break;
            }
            sidesTmp[j + 1] = x;
        }
        var restP = poly;
        for (var si = 0; si < sidesTmp.Count; si++)
        {
            var side = sidesTmp[si];
            if (rng.U01() >= P.ToothKeep(side))
                continue;
            if (ContactOf(restP, side) < 0.005)
                continue;
            // the whole remaining piece fits in the band: it stays as one tooth
            if (InClearArea(restP) < 1e-12 && GlassPoly.Area(restP) >= P.minPieceArea && fr.Band(side) > fr.Pocket(side) + 1e-9)
            {
                pieces.Add(NewPiece(restP, GlassPieceKind.Tooth, side, GlassCorner.None));
                restP = null;
                break;
            }
            for (var at = 0; at < 4; at++)
            {
                var tooth = Rent();
                var rest2 = Rent();
                var rcut = GlassRng.Make(seed, S_TEETH, 2, at, k1, k2);
                if (!ToothCut(restP, side, ref rcut, tooth, rest2))
                    break;
                if (tooth.Count < 3 || rest2.Count < 3)
                    continue;
                if (GlassPoly.Area(tooth) < P.minPieceArea || GlassPoly.Area(rest2) < P.minPieceArea)
                    continue;
                if (InClearArea(tooth) > 1e-12)
                    continue;
                pieces.Add(NewPiece(tooth, GlassPieceKind.Tooth, side, GlassCorner.None));
                restP = rest2;
                break;
            }
        }
        if (restP != null)
            pieces.Add(NewPiece(restP, GlassPieceKind.Shard, '\0', GlassCorner.None));
    }

    // ---------------------------------------------------------------------------------------------------------
    // count cap (pieces, and moving bodies on WebGL)
    // ---------------------------------------------------------------------------------------------------------
    // the count cap of generate(): pieces, and moving bodies on WebGL. True = over the cap: evaluate the chords.
    bool CapStart()
    {
        var nBodies = 0;
        for (var i = 0; i < pieces.Count; i++)
            if (pieces[i].kind == GlassPieceKind.Shard)
                nBodies++;
        excess = Math.Max(pieces.Count - P.maxPieces, nBodies - P.maxBodies);
        if (excess <= 0)
            return false;
        // drop OUTER ring chords whose union stays shorter than dagger_max, so each drop merges two pieces.
        // tier 0 = both faces free shards and small, tier 1 = free shards, tier 2 = a face touches the frame.
        byOwner.Clear();
        ownerKeys.Clear();
        for (var ri = 0; ri < raw.Count; ri++)
        {
            var rr = raw[ri];
            if (rr.kind == GlassPieceKind.Crush)
                continue;
            for (var i = 0; i < rr.fn; i++)
            {
                var o = OwnerOf(faceVids[rr.f0 + i], faceVids[rr.f0 + (i + 1) % rr.fn]);
                if (o.kind != GlassOwner.Chord)
                    continue;
                var key = new Key3(o.a, o.b, o.c);
                if (suppChords.Contains(key))
                    continue;
                OwnerFaces of;
                if (!byOwner.TryGetValue(key, out of))
                    ownerKeys.Add(key);
                if (of.count == 0) of.r0 = rr;
                else if (of.count == 1) of.r1 = rr;
                of.count++;
                byOwner[key] = of;
            }
        }
        cands.Clear();
        return true;
    }

    void CapEvalOwner(Key3 key)
    {
        var of = byOwner[key];
        if (of.count != 2)
            return;
        var p0 = of.r0.poly;
        var p1 = of.r1.poly;
        sC.Clear();
        sC.AddRange(p0);
        sC.AddRange(p1);
        GlassPoly.ConvexHull(sC, sA, sB, sD);
        int di, dj;
        if (GlassPoly.Diameter(sD, out di, out dj) > P.daggerMax)
            return;
        var aU = GlassPoly.Area(p0) + GlassPoly.Area(p1);
        double a0, a1, a2, a3, b0, b1, b2, b3;
        Contacts(p0, out a0, out a1, out a2, out a3);
        Contacts(p1, out b0, out b1, out b2, out b3);
        var framed = PyMax(PyMax(PyMax(a0, a1), a2), a3) > 0.0 || PyMax(PyMax(PyMax(b0, b1), b2), b3) > 0.0;
        var tier = framed ? 2 : (aU <= P.capMergeArea ? 0 : 1);
        cands.Add(new Cand { tier = tier, negRing = -key.a, aU = aU, owner = key });
    }

    // true = rebuild (chords or a fork dropped); false = cap unmet, go on
    bool CapDecide()
    {
        if (cands.Count > 0)
        {
            cands.Sort(CompareCand);
            for (var i = 0; i < cands.Count && i < excess; i++)
            {
                suppChords.Add(cands[i].owner);
                AddRepair(GlassRepairKind.CapDropOuterChord, cands[i].owner.Owner, default(GlassOwner), 0.0, 0, null);
            }
            return true;
        }
        fks.Clear();
        for (var i = 0; i < forks.Count; i++)
            if (!suppForks.Contains(forks[i].key))
                fks.Add(forks[i]);
        if (fks.Count > 0)
        {
            // sorted by (-ring, key): the outermost fork, then the smallest key
            var best = fks[0];
            for (var i = 1; i < fks.Count; i++)
            {
                var f = fks[i];
                if (f.ring > best.ring || (f.ring == best.ring && Key3.Compare(f.key, best.key) < 0))
                    best = f;
            }
            suppForks.Add(best.key);
            AddRepair(GlassRepairKind.CapDropFork, best.key.Owner, default(GlassOwner), 0.0, 0, null);
            return true;
        }
        AddRepair(GlassRepairKind.CapUnmet, default(GlassOwner), default(GlassOwner), 0.0, pieces.Count, null);
        return false;
    }

    readonly List<Key3> ownerKeys = new List<Key3>();

    static readonly Comparison<Cand> CompareCand = (x, y) =>
    {
        if (x.tier != y.tier) return x.tier < y.tier ? -1 : 1;
        if (x.negRing != y.negRing) return x.negRing < y.negRing ? -1 : 1;
        if (x.aU != y.aU) return x.aU < y.aU ? -1 : 1;
        return Key3.Compare(x.owner, y.owner);
    };

    // ---------------------------------------------------------------------------------------------------------
    // _assemble: stage 1, chips, piece records (pose, release, hierarchy), graph export
    // ---------------------------------------------------------------------------------------------------------
    void Stage1AndOrder()
    {
        res.attempts = attempt + 1;
        // ---- stage 1: 5-8 of the initial radials, to 25-35 % of the way to the stop line
        var rng = GlassRng.Make(seed, S_STAGE1);
        var nS1 = Math.Min(n0, rng.RandInt(P.stage1CountMin, P.stage1CountMax));
        initialTracks.Clear();
        for (var i = 0; i < allTracks.Count; i++)
            if (allTracks[i].initial)
                initialTracks.Add(allTracks[i]);
        // scored = sorted(initial, key=(Rng(seed, S_STAGE1, 1, tid).u01(), tid)); chosen = sorted(scored[:n], key=tid)
        scoreTmp.Clear();
        idxTmp.Clear();
        for (var i = 0; i < initialTracks.Count; i++)
        {
            var rs = GlassRng.Make(seed, S_STAGE1, 1, initialTracks[i].tid);
            scoreTmp.Add(rs.U01());
            idxTmp.Add(i);
        }
        idxTmp.Sort(byScoreThenTid);
        if (idxTmp.Count > nS1)
            idxTmp.RemoveRange(nS1, idxTmp.Count - nS1);
        idxTmp.Sort(byTrackId);
        res.cracks.Clear();
        res.crackVerts.Clear();
        for (var ci = 0; ci < idxTmp.Count; ci++)
        {
            var t = initialTracks[idxTmp[ci]];
            var rr = GlassRng.Make(seed, S_STAGE1, 2, t.tid);
            var reachF = rr.Uniform(P.stage1ReachMin, P.stage1ReachMax);
            var tht = th[t.tid];
            var dstop = GlassPoly.RayRectExit(ix, iy, GlassMath.Cos(tht), GlassMath.Sin(tht), fr.stopX0, fr.stopX1, fr.stopY0, fr.stopY1);
            var reach = reachF * dstop;
            s1Pts.Clear();
            s1Pts.Add(V[t.pts[0]]);
            for (var i = 1; i < t.pts.Count; i++)
            {
                var p = V[t.pts[i]];
                var rp = GlassMath.Hypot(p.x, p.y);
                if (rp >= reach)
                {
                    var q = s1Pts[s1Pts.Count - 1];
                    // interpolate on the segment to radius 'reach' (radius is monotonic along a track)
                    double lo = 0.0, hi = 1.0;
                    for (var it = 0; it < 60; it++)
                    {
                        var mm = 0.5 * (lo + hi);
                        var x = q.x + (p.x - q.x) * mm;
                        var y = q.y + (p.y - q.y) * mm;
                        if (GlassMath.Hypot(x, y) < reach)
                            lo = mm;
                        else
                            hi = mm;
                    }
                    s1Pts.Add(new GlassV2(q.x + (p.x - q.x) * hi, q.y + (p.y - q.y) * hi));
                    break;
                }
                s1Pts.Add(p);
            }
            var v0 = res.crackVerts.Count;
            for (var i = 0; i < s1Pts.Count; i++)
                res.crackVerts.Add(new GlassV2(s1Pts[i].x + ix, s1Pts[i].y + iy));
            res.cracks.Add(new GlassCrack { track = t.tid, reach = reach, reachFrac = reachF, v0 = v0, vn = s1Pts.Count });
        }
        // ---- chips (particle spawn points on the new cracks of each stage)
        var rngc = GlassRng.Make(seed, S_CHIPS, 1);
        res.chips1.Clear();
        var n1 = rngc.RandInt(P.chipsS1Min, P.chipsS1Max);
        for (var i = 0; i < n1; i++)
        {
            var c = res.cracks[(int)(rngc.NextU32() % (uint)res.cracks.Count)];
            var seg = (int)(rngc.NextU32() % (uint)(c.vn - 1));
            var f = rngc.U01();
            var a = res.crackVerts[c.v0 + seg];
            var b = res.crackVerts[c.v0 + seg + 1];
            res.chips1.Add(new GlassV2(a.x + (b.x - a.x) * f, a.y + (b.y - a.y) * f));
        }
        var rngc2 = GlassRng.Make(seed, S_CHIPS, 2);
        var n2 = rngc2.RandInt(P.chipsS2Min, P.chipsS2Max);
        res.chips2.Clear();
        idxTmp.Clear();
        for (var i = 0; i < pieces.Count; i++)
            if (pieces[i].kind != GlassPieceKind.Crush)
                idxTmp.Add(i);
        for (var i = 0; i < n2 * 8; i++)
        {
            if (res.chips2.Count >= n2)
                break;
            var pc = pieces[idxTmp[(int)(rngc2.NextU32() % (uint)idxTmp.Count)]].poly;
            var e = (int)(rngc2.NextU32() % (uint)pc.Count);
            var f = rngc2.U01();
            var a = pc[e];
            var b = pc[(e + 1) % pc.Count];
            var q = new GlassV2(a.x + (b.x - a.x) * f, a.y + (b.y - a.y) * f);
            if (Math.Abs(q.x) >= fr.hx - 1e-6 || Math.Abs(q.y) >= fr.hy - 1e-6)
                continue;        // on the glass edge: not a crack
            res.chips2.Add(q);
        }

        // ---- final piece records (sorted for stable ids: by kind, then distance from the impact, then angle)
        recs.Clear();
        for (var i = 0; i < pieces.Count; i++)
        {
            var c = GlassPoly.Centroid(pieces[i].poly);
            var r = GlassMath.Hypot(c.x - ix, c.y - iy);
            var ang = GlassMath.Atan2(c.y - iy, c.x - ix);
            var kr = pieces[i].kind == GlassPieceKind.Crush ? 0 : pieces[i].kind == GlassPieceKind.Shard ? 1 : 2;
            recs.Add(new RecKey { rank = kr, rr = GlassMath.PyRound(r, 6), ra = GlassMath.PyRound(ang, 6), index = i });
        }
        recs.Sort(RecOrder);
        res.pieces.Clear();
        res.verts.Clear();
        res.subPolys.Clear();
        res.subVerts.Clear();
        pieceDiam.Clear();
    }

    readonly List<double> pieceDiam = new List<double>();

    // one final piece record (in sorted order): hierarchy, pose, release, level-2 cells
    void Record(int idx)
    {
        var theta00 = th[0];
        {
            var p = pieces[recs[idx].index];
            var poly = p.poly;
            var c = GlassPoly.Centroid(poly);
            var r = GlassMath.Hypot(c.x - ix, c.y - iy);
            var band = 0;
            while (band + 1 < rings.Count && rings[band + 1] <= r)
                band++;
            var ang = GlassMath.Atan2(c.y - iy, c.x - ix);
            var rel = GlassMath.PyMod(ang - theta00, TwoPi);
            var sector = 0;
            for (var si = 0; si < n0; si++)
                if (GlassMath.PyMod(th[si] - theta00, TwoPi) <= rel + 1e-12)
                    sector = si;
            var rec = new GlassPiece
            {
                id = idx, kind = p.kind, v0 = res.verts.Count, vn = poly.Count, area = GlassPoly.Area(poly), centroid = c, r = r,
                band = band, sector = sector, cluster = band * 64 + sector, side = p.kind == GlassPieceKind.Tooth ? p.side : '\0',
                corner = p.kind == GlassPieceKind.Tooth ? p.corner : GlassCorner.None,
            };
            res.verts.AddRange(poly);
            int ddi, ddj;
            pieceDiam.Add(GlassPoly.Diameter(poly, out ddi, out ddj));   // for _stats max_piece_len (same values)
            Pose(poly, p.kind, ref rec);
            Release(poly, p.kind, ref rec);
            rec.s0 = res.subPolys.Count;
            rec.sn = p.sub.Count;
            for (var si = 0; si < p.sub.Count; si++)
            {
                res.subPolys.Add(new GlassRange(res.subVerts.Count, p.sub[si].Count));
                res.subVerts.AddRange(p.sub[si]);
            }
            res.pieces.Add(rec);
        }
    }

    void GraphExport()
    {
        // ---- graph export (slab coords), clipped edges are kept whole for debugging
        res.graphVerts.Clear();
        res.tracks.Clear();
        for (var ti = 0; ti < allTracks.Count; ti++)
        {
            var t = allTracks[ti];
            var v0 = res.graphVerts.Count;
            for (var i = 0; i < t.pts.Count; i++)
                res.graphVerts.Add(new GlassV2(V[t.pts[i]].x + ix, V[t.pts[i]].y + iy));
            res.tracks.Add(new GlassTrackOut { id = t.tid, parent = t.parent, initial = t.initial, v0 = v0, vn = t.pts.Count });
        }
        res.chords.Clear();
        for (var i = 0; i < chords.Count; i++)
        {
            var c = chords[i];
            res.chords.Add(new GlassChordOut
            {
                ring = c.ring, a = c.a, b = c.b, p0 = new GlassV2(V[c.va].x + ix, V[c.va].y + iy), p1 = new GlassV2(V[c.vb].x + ix, V[c.vb].y + iy)
            });
        }
        res.forks.Clear();
        for (var i = 0; i < forks.Count; i++)
            res.forks.Add(new GlassForkOut { ring = forks[i].ring, parent = forks[i].parent, child = forks[i].child, at = new GlassV2(V[forks[i].vid].x + ix, V[forks[i].vid].y + iy) });
        res.craterPoly.Clear();
        res.craterRadius = 0.0;
        for (var i = 0; i < res.pieces.Count; i++)
            if (res.pieces[i].kind == GlassPieceKind.Crush)
            {
                var cp = res.pieces[i];
                var cr0 = 0.0;
                for (var k = 0; k < cp.vn; k++)
                {
                    var q = res.verts[cp.v0 + k];
                    res.craterPoly.Add(q);
                    var d = GlassMath.Hypot(q.x - ix, q.y - iy);
                    cr0 = k == 0 ? d : PyMax(cr0, d);
                }
                res.craterRadius = cr0;
                break;
            }
        FillHeader();
        res.rings.Clear();
        res.rings.AddRange(rings);
        res.wireGrid = P.wireGrid;
        res.wireHangs = P.hangsOnWire;
    }

    void FillHeader()
    {
        res.profile = P.name;
        res.glass = P.glass;
        res.tier = P.tier;
        res.seed = seed;
        res.rotationDeg = rotationDeg;
        res.side = side;
        res.farZSign = side > 0 ? 1 : -1;
        res.impact = new GlassV2(ix, iy);
        res.impactRoot = new GlassV2(ix, iy + FrontRoomsGlassFracturePattern.SlabCentreY);
        res.impactUV = new GlassV2((ix - fr.stopX0) / (fr.stopX1 - fr.stopX0), (iy - fr.stopY0) / (fr.stopY1 - fr.stopY0));
        res.slabW = w;
        res.slabH = h;
        res.slabT = P.thickness;
        res.bevel = P.mode == GlassFractureMode.Dice ? 0.0 : P.bevel;
        res.centreRootY = FrontRoomsGlassFracturePattern.SlabCentreY;
        res.frame = fr;
    }

    void Pose(List<GlassV2> poly, GlassPieceKind kind, ref GlassPiece rec)
    {
        if (kind != GlassPieceKind.Shard)
        {
            rec.posePivot = new GlassV2(0.0, 0.0);
            rec.poseAxis = new GlassV2(1.0, 0.0);
            rec.poseDeg = 0.0;
            rec.posePush = 0.0;
            return;
        }
        long k1, k2;
        PieceKey(poly, out k1, out k2);
        var rng = GlassRng.Make(seed, S_POSE, k1, k2);
        var c = GlassPoly.Centroid(poly);
        var r = GlassMath.Hypot(c.x - ix, c.y - iy);
        var wgt = GlassMath.Exp(-r / P.poseFalloff);
        var tilt = rng.Uniform(P.tiltDegMin, P.tiltDegMax) * wgt;
        var push = rng.Uniform(P.pushMin, P.pushMax) * wgt;
        var jit = GlassMath.Radians(rng.Uniform(-P.tiltAxisJitterDeg, P.tiltAxisJitterDeg));
        // pivot: the vertex farthest from the impact (the inner end swings to the far side)
        // max(poly, key=lambda p: (p[0] - ix) ** 2 + (p[1] - iy) ** 2): Python's ** 2 is libm pow(), which is not
        // always x * x (MEASURED: 133 in 100,000), so the port calls Math.Pow as the reference does
        var far = poly[0];
        var fd = Math.Pow(far.x - ix, 2.0) + Math.Pow(far.y - iy, 2.0);
        for (var i = 1; i < poly.Count; i++)
        {
            var d = Math.Pow(poly[i].x - ix, 2.0) + Math.Pow(poly[i].y - iy, 2.0);
            if (d > fd)
            {
                fd = d;
                far = poly[i];
            }
        }
        var inx = ix - far.x;
        var iny = iy - far.y;
        var L = GlassMath.Hypot(inx, iny);
        if (L == 0.0)
            L = 1.0;
        inx = inx / L;
        iny = iny / L;
        var ax = iny;
        var ay = -inx;                         // rotation about +a moves the inward side toward +far
        var ca = GlassMath.Cos(jit);
        var sa = GlassMath.Sin(jit);
        var ax2 = ax * ca - ay * sa;
        var ay2 = ax * sa + ay * ca;
        rec.posePivot = far;
        rec.poseAxis = new GlassV2(ax2, ay2);
        rec.poseDeg = tilt;
        rec.posePush = push;
    }

    void Release(List<GlassV2> poly, GlassPieceKind kind, ref GlassPiece rec)
    {
        rec.hasRelease = false;
        if (kind != GlassPieceKind.Shard)
            return;
        long k1, k2;
        PieceKey(poly, out k1, out k2);
        var rng = GlassRng.Make(seed, S_POSE, 99, k1, k2);
        var c = GlassPoly.Centroid(poly);
        var r = GlassMath.Hypot(c.x - ix, c.y - iy);
        var rr = r != 0.0 ? r : 1.0;
        var dstop = GlassPoly.RayRectExit(ix, iy, (c.x - ix) / rr, (c.y - iy) / rr, fr.stopX0, fr.stopX1, fr.stopY0, fr.stopY1);
        double cL, cR, cT, cB;
        Contacts(poly, out cL, out cR, out cT, out cB);
        var edge = PyMax(PyMax(PyMax(cL, cR), cT), cB) >= 0.005;
        double ms;
        int wave;
        if (r < P.releaseInnerR)
        {
            ms = 0.0;
            wave = 0;
        }
        else if (edge || r > 0.75 * dstop)
        {
            ms = rng.Uniform(P.releaseOuterMsMin, P.releaseOuterMsMax);
            wave = 2;
        }
        else
        {
            var f = Math.Min(1.0, Math.Max(0.0, (r - P.releaseInnerR) / PyMax(1e-6, 0.75 * dstop - P.releaseInnerR)));
            ms = P.releaseMidMsMin + f * (P.releaseMidMsMax - P.releaseMidMsMin) + rng.Uniform(-10.0, 10.0);
            ms = PyMax(P.releaseMidMsMin, PyMin(P.releaseMidMsMax, ms));
            wave = 1;
        }
        var speed = rng.Uniform(P.releaseSpeedMin, P.releaseSpeedMax) * GlassMath.Exp(-r / P.releaseSpeedFalloff);
        rec.hasRelease = true;
        rec.releaseWave = wave;
        rec.releaseMs = ms;
        rec.releaseSpeed = speed;
    }

    // ---------------------------------------------------------------------------------------------------------
    // _stats
    // ---------------------------------------------------------------------------------------------------------
    void Stats()
    {
        var s = new GlassStats();
        var pcs = res.pieces;
        shardAreas.Clear();
        var totalVerts = 0;
        var maxLen = 0.0;
        for (var i = 0; i < pcs.Count; i++)
        {
            var p = pcs[i];
            if (p.kind == GlassPieceKind.Shard) { s.shard++; shardAreas.Add(p.area); }
            else if (p.kind == GlassPieceKind.Tooth) s.tooth++;
            else if (p.kind == GlassPieceKind.Crush) s.crush++;
            totalVerts += p.vn;
            var d = pieceDiam[i];          // diameter(poly) of this piece, computed in Record()
            maxLen = i == 0 ? d : PyMax(maxLen, d);
            s.level2Cells += p.sn;
            var tri = P.bevel > 0.0 ? 12 * p.vn - 4 : 4 * p.vn - 4;
            if (p.kind != GlassPieceKind.Crush)
                s.trisStage2 += tri;
            if (p.kind == GlassPieceKind.Tooth)
                s.trisTeeth += tri;
        }
        shardAreas.Sort(DoubleAscending);
        s.shardAreaCm2P10 = Q(shardAreas, 0.1) * 1e4;
        s.shardAreaCm2P50 = Q(shardAreas, 0.5) * 1e4;
        s.shardAreaCm2P90 = Q(shardAreas, 0.9) * 1e4;
        s.piecesTotal = pcs.Count;
        s.bodies = s.shard;
        s.tracksInitial = n0;
        s.tracksTotal = allTracks.Count;
        s.forks = forks.Count;
        s.chords = chords.Count;
        s.rings = rings.Count;
        s.tJunctions = 2 * chords.Count + forks.Count;
        s.plusJunctions = PlusJunctions();
        s.kinks = kinks;
        s.kinksZeroed = kinksZeroed;
        s.maxPieceLen = maxLen;
        s.avgVerts = (double)totalVerts / pcs.Count;
        var finSegs = 0;
        for (var i = 0; i < res.cracks.Count; i++)
            finSegs += res.cracks[i].vn - 1;
        s.trisFins = 2 * finSegs;
        s.trisCrater = P.craterTrisPerVertex * res.craterPoly.Count;
        s.stage1Cracks = res.cracks.Count;
        res.stats = s;
    }

    // A cached Comparison<double>: List.Sort(Comparison) allocates nothing on Mono, while Sort() and Sort(IComparer)
    // allocate 128 B per call (measured, build/03_generator.md sec 5.3).
    static readonly Comparison<double> DoubleAscending = (x, y) => x.CompareTo(y);

    static double Q(List<double> a, double f) => a.Count == 0 ? 0.0 : a[Math.Min(a.Count - 1, (int)(a.Count * f))];

    readonly Dictionary<int, int> plusCnt = new Dictionary<int, int>();

    int PlusJunctions()
    {
        // vertices where two chords meet a radial at the same point (a '+'); must be 0
        plusCnt.Clear();
        for (var i = 0; i < chords.Count; i++)
        {
            int c;
            plusCnt.TryGetValue(chords[i].va, out c);
            plusCnt[chords[i].va] = c + 1;
            plusCnt.TryGetValue(chords[i].vb, out c);
            plusCnt[chords[i].vb] = c + 1;
        }
        var n = 0;
        foreach (var kv in plusCnt)
            if (kv.Value > 1)
                n++;
        return n;
    }

    // ---------------------------------------------------------------------------------------------------------
    // Tempered (PARKED, data only): the whole pane dices at 1.0 s; clumps of ~1 cm granules, no teeth
    // ---------------------------------------------------------------------------------------------------------
    readonly List<GlassV2> diceNodes = new List<GlassV2>();

    void GenerateDice()
    {
        var hx = fr.hx;
        var hy = fr.hy;
        var nx = Math.Max(2, (int)Math.Round(w / P.clump, MidpointRounding.ToEven));
        var ny = Math.Max(2, (int)Math.Round(h / P.clump, MidpointRounding.ToEven));
        var dx = w / nx;
        var dy = h / ny;
        var J = P.clumpJitter * 0.5;
        diceNodes.Clear();
        for (var i = 0; i <= nx; i++)
            for (var j = 0; j <= ny; j++)
            {
                var x = -hx + i * dx;
                var y = -hy + j * dy;
                var r = GlassRng.Make(seed, S_DICE, i, j);
                var jx = r.Uniform(-J, J) * dx;
                var jy = r.Uniform(-J, J) * dy;
                if (0 < i && i < nx)
                    x += jx;
                if (0 < j && j < ny)
                    y += jy;
                diceNodes.Add(new GlassV2(x, y));
            }
        FillHeader();
        res.isDice = true;
        res.granule = P.granule;
        res.attempts = 0;
        var poly = sC;
        for (var i = 0; i < nx; i++)
            for (var j = 0; j < ny; j++)
            {
                poly.Clear();
                poly.Add(diceNodes[i * (ny + 1) + j]);
                poly.Add(diceNodes[(i + 1) * (ny + 1) + j]);
                poly.Add(diceNodes[(i + 1) * (ny + 1) + j + 1]);
                poly.Add(diceNodes[i * (ny + 1) + j + 1]);
                var c = GlassPoly.Centroid(poly);
                var rr = GlassMath.Hypot(c.x - ix, c.y - iy);
                var rng = GlassRng.Make(seed, S_DICE, 1000 + i, j);
                var a = GlassPoly.Area(poly);
                var rec = new GlassPiece
                {
                    id = res.pieces.Count, kind = GlassPieceKind.Clump, v0 = res.verts.Count, vn = 4, area = a, centroid = c, r = rr,
                    granules = (int)(a / Math.Pow(P.granule, 2.0)),
                    hasRelease = true, releaseWave = 0,
                };
                // the crazed sheet holds for the 50 ms hit-stop, then sags out of the frame from the impact outward
                rec.releaseMs = PyMin(80.0, 60.0 * rr) + rng.Uniform(0.0, 15.0);
                rec.releaseSpeed = rng.Uniform(1.0, 2.0) * GlassMath.Exp(-rr / 0.35);
                res.verts.AddRange(poly);
                res.pieces.Add(rec);
            }
        var st = new GlassStats();
        st.clump = res.pieces.Count;
        st.piecesTotal = res.pieces.Count;
        st.granulesEstimate = (long)(w * h / Math.Pow(P.granule, 2.0));
        res.stats = st;
    }
}
