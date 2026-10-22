// FrontRoomsGlassFractureMeshes.cs -- GD3 step 5 (plan 10 sec 2.3, 2.4, 4.3; build/04_stages.md): the stage meshes of one
// fracture pattern, built in time slices while E is held (from E-down, done before the Crack1 frame).
//
//   Slab     S0: the intact 6 mm slab 1.391 x 1.642 (its 4 rim faces sit in the stop pocket).
//   Stage1   S1: the Crack1 fins (thin air gaps through the 6 mm, each segment leaned 3-9 deg, the lean flipping now and
//            then: twist hackle) and the crushed crater (an irregular pit on the struck face, <= 15 mm).
//   Stage2   S2: every piece (shards and teeth; the crushed core has fallen out) as a closed 6 mm prism, every edge
//            bevelled (the profile's bevel, 0.3 mm desktop, none on WebGL), a 0.08 mm air gap between neighbours, shards
//            at their stage-2 pose: the generator's (tilt 0.2-0.8 deg, push 0.5-3 mm to the far side, falling off from the
//            impact as e^(-r / 0.35 m)) after a SEAT tilt of 0.40-0.60 deg about a random in-plane axis through the piece's
//            centroid that does not fall off (every loose piece re-seats in the stop's ~1 mm of putty play; without it the
//            reflection only breaks within ~0.3 m of the impact: V6, build/04_stages.md sec 4.5). StagePoint() is the one
//            stage-2 pose for every user (this mesh, the debris world of plan step 6, G14 P4).
//
// Every mesh: slab-local metres (x = window root +X, y up, origin = the slab centre = root (0, 1.175, 0); z across, +z =
// root +Z = toward cell b), one submesh, Float32 x 3 positions in stream 0, 16-bit indices, flat normals, readable (G14 P4,
// plan sec 4.7). Vertex data for FrontRooms/Glass with _PaneFromUV: UV0 = the REST pane metres (x, y), so grime and
// smudges never move through a swap or on a moving piece; UV1 = (per-pane hash key A, key B, band, kind); colour R =
// fracture-edge mask, G = per-segment random, B = depth across the glass (0 back .. 1 front).
// Stage2's index buffer draws every crack face (sides, bevels) before every pane face, so the faces blend over the
// cracks behind them; PieceSides / PieceFaces give each piece's two index ranges, and BuildPieceSource() makes the
// per-piece contiguous closed-slab mesh that FrontRoomsGlassRT.PrepareMeshes wants (one BLAS per range).
// Pure arithmetic in doubles from the generator's result; deterministic (same pattern, same bytes).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class FrontRoomsGlassFractureMeshes
{
    public const byte KindFace = 0, KindFracture = 1, KindCrushed = 2, KindEdge = 3;

    // Look constants (the Cycles look-dev, glass_lookdev.py: build_fins, build_crater, build_stage2).
    public const double FinHalfGap = 0.00002;        // a 0.04 mm air gap per Crack1 fin
    public const double FinDepthScale = 0.996;       // fins stop a hair inside the faces
    public const double FinLeanMinDeg = 3.0, FinLeanMaxDeg = 9.0, FinLeanFlipP = 0.25;
    public const double FinOverlap = 0.0001;         // segments overlap 0.1 mm at a kink
    public const double PieceInset = 0.00004;        // per piece: a 0.08 mm air gap between neighbours at S2
    public const double CraterLift = 0.00003;        // crater rim just proud of the struck face
    public const double CraterMidDepth0 = 0.0004, CraterMidDepth1 = 0.0012, CraterApexDepth = 0.0016;
    public const int CraterSubdiv = 3;
    /// <summary>The seat tilt of every loose shard (degrees, uniform; a look decision keyed off the pattern seed, like the fin lean).</summary>
    public const double SeatDegMin = 0.40, SeatDegMax = 0.60;
    public const int PiecesPerSlice = 6;

    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Color32 color;
        public Vector2 uv0;
        public Vector4 uv1;
    }

    public static readonly VertexAttributeDescriptor[] Layout =
    {
        new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
        new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
        new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
        new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
        new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 4),
    };

    /// <summary>Where the mesh build stands.</summary>
    public enum Phase { Idle, Stage1, Pieces, Upload1, Upload2, Done }

    public struct Options
    {
        /// <summary>The two per-pane hash keys (UV1.xy): FrontRooms/Glass's own transform rule, see PaneKeys.</summary>
        public float keyA, keyB;
        /// <summary>Lab diagnostics: build stage 2 at the rest pose (V7 continuity renders).</summary>
        public bool zeroPose;
        /// <summary>Bevel override (metres); NaN = the profile's.</summary>
        public double bevel;
    }

    public struct Counts
    {
        public int slabVerts, slabTris, stage1Verts, stage1Tris, finTris, craterTris, stage2Verts, stage2Tris, stage2SideTris, pieces, fins;
        public int bevelClamped;          // pieces whose bevel was reduced to fit a short edge
    }

    public Phase Next { get; private set; } = Phase.Idle;
    public bool IsDone => Next == Phase.Done;
    public Mesh Slab { get; private set; }
    public Mesh Stage1 { get; private set; }
    public Mesh Stage2 { get; private set; }
    public Counts Stats => counts;
    /// <summary>Per piece (FrontRoomsGlassFracture.pieces index; the crushed core has none): index ranges in Stage2.</summary>
    public readonly List<FrontRoomsGlassRTRange> PieceSides = new List<FrontRoomsGlassRTRange>(256), PieceFaces = new List<FrontRoomsGlassRTRange>(256);
    /// <summary>Stage2 piece order: PieceIds[k] = the fracture piece index of range k.</summary>
    public readonly List<int> PieceIds = new List<int>(256);
    /// <summary>The clock StepFor and the slice timings use (ms). Wall time by default; tests and the lab may pass the thread's
    /// CPU time, which leaves out other processes' preemption.</summary>
    public static Func<double> ClockMs = () => Stopwatch.GetTimestamp() * 1000.0 / Stopwatch.Frequency;
    public double SliceMsMax { get; private set; }
    /// <summary>The worst slice per phase (ms, ClockMs): Stage1, Pieces, Upload1, Upload2.</summary>
    public readonly double[] PhaseMsMax = new double[6];
    /// <summary>Tests: when set, every slice's time is added to the list of its phase.</summary>
    public static List<double>[] SliceLog;
    public double TotalMs { get; private set; }
    public int Slices { get; private set; }

    FrontRoomsGlassFracture f;
    Options o;
    Counts counts;
    double hx, hy, h, bevel;
    int nextPiece;
    readonly List<Vertex> vS1 = new List<Vertex>(1024), vS2 = new List<Vertex>(24576);
    readonly List<ushort> iS1 = new List<ushort>(2048), iS2Side = new List<ushort>(24576), iS2Face = new List<ushort>(8192), iS2 = new List<ushort>(32768);
    readonly List<GlassV2> poly = new List<GlassV2>(32), q0 = new List<GlassV2>(32), qf = new List<GlassV2>(32), tmp = new List<GlassV2>(32);
    readonly List<Vector3d> rim = new List<Vector3d>(64), mid = new List<Vector3d>(64);
    readonly List<int> faceStartOfPiece = new List<int>(256), faceCountOfPiece = new List<int>(256), sideStartOfPiece = new List<int>(256), sideCountOfPiece = new List<int>(256);

    // ---------------------------------------------------------------------------------------------------------
    // Per-pane keys: FrontRooms/Glass's cube path takes its per-pane random from the object's origin
    // (Hash11(dot(floor(c * 10), (1, 57, 113))) and Hash11(dot(floor(c * 10), (7, 3, 29)) + .37)); the stage meshes
    // carry the two dot products in UV1, from the slab object's world matrix, so every stage shows the same grime as
    // the interim slab (and as each other) even when a piece moves.
    // ---------------------------------------------------------------------------------------------------------
    public static void PaneKeys(Matrix4x4 slabLocalToWorld, out float keyA, out float keyB)
    {
        var cx = Mathf.Floor(slabLocalToWorld.m03 * 10f);
        var cy = Mathf.Floor(slabLocalToWorld.m13 * 10f);
        var cz = Mathf.Floor(slabLocalToWorld.m23 * 10f);
        keyA = cx * 1f + cy * 57f + cz * 113f;
        keyB = cx * 7f + cy * 3f + cz * 29f;
    }

    /// <summary>The intact slab (S0). Cheap (24 vertices): built at once.</summary>
    public static Mesh BuildSlab(Mesh reuse, float keyA, float keyB, double slabW = FrontRoomsGlassFracturePattern.SlabW,
        double slabH = FrontRoomsGlassFracturePattern.SlabH, double slabT = FrontRoomsGlassFracturePattern.SlabT)
    {
        var v = new List<Vertex>(24);
        var idx = new List<ushort>(36);
        double x = slabW * .5, y = slabH * .5, z = slabT * .5;
        var key = new Vector4(keyA, keyB, 0f, KindFace);
        var keyEdge = new Vector4(keyA, keyB, 0f, KindEdge);
        Quad(v, idx, new Vector3d(-x, -y, z), new Vector3d(x, -y, z), new Vector3d(x, y, z), new Vector3d(-x, y, z), new Vector3d(0, 0, 1), key, 0, 0);
        Quad(v, idx, new Vector3d(-x, -y, -z), new Vector3d(x, -y, -z), new Vector3d(x, y, -z), new Vector3d(-x, y, -z), new Vector3d(0, 0, -1), key, 0, 0);
        Quad(v, idx, new Vector3d(x, -y, -z), new Vector3d(x, y, -z), new Vector3d(x, y, z), new Vector3d(x, -y, z), new Vector3d(1, 0, 0), keyEdge, 255, 0);
        Quad(v, idx, new Vector3d(-x, -y, -z), new Vector3d(-x, y, -z), new Vector3d(-x, y, z), new Vector3d(-x, -y, z), new Vector3d(-1, 0, 0), keyEdge, 255, 0);
        Quad(v, idx, new Vector3d(-x, y, -z), new Vector3d(x, y, -z), new Vector3d(x, y, z), new Vector3d(-x, y, z), new Vector3d(0, 1, 0), keyEdge, 255, 0);
        Quad(v, idx, new Vector3d(-x, -y, -z), new Vector3d(x, -y, -z), new Vector3d(x, -y, z), new Vector3d(-x, -y, z), new Vector3d(0, -1, 0), keyEdge, 255, 0);
        var mesh = reuse != null ? reuse : NewMesh("GD3 slab (S0)");
        Upload(mesh, v, idx, slabW, slabH, slabT);
        return mesh;
    }

    // ---------------------------------------------------------------------------------------------------------
    // The sliced build of Stage1 + Stage2
    // ---------------------------------------------------------------------------------------------------------
    public void Begin(FrontRoomsGlassFracture fracture, Options options)
    {
        f = fracture;
        o = options;
        counts = default;
        hx = f.slabW * .5;
        hy = f.slabH * .5;
        h = f.slabT * .5;
        bevel = double.IsNaN(o.bevel) ? f.bevel : o.bevel;
        vS1.Clear(); iS1.Clear(); vS2.Clear(); iS2Side.Clear(); iS2Face.Clear(); iS2.Clear();
        PieceSides.Clear(); PieceFaces.Clear(); PieceIds.Clear();
        faceStartOfPiece.Clear(); faceCountOfPiece.Clear(); sideStartOfPiece.Clear(); sideCountOfPiece.Clear();
        nextPiece = 0;
        SliceMsMax = TotalMs = 0;
        Slices = 0;
        Array.Clear(PhaseMsMax, 0, PhaseMsMax.Length);
        Next = f.isDice ? Phase.Upload1 : Phase.Stage1;   // tempered glass: no crack stages (S1 = S2 = the intact pane)
    }

    /// <summary>One slice (a bounded amount of work). True when everything is built.</summary>
    public bool Step()
    {
        if (Next == Phase.Done || Next == Phase.Idle) return Next == Phase.Done;
        var t0 = ClockMs();
        var phase = Next;
        switch (Next)
        {
            case Phase.Stage1:
                BuildStage1();
                Next = Phase.Pieces;
                break;
            case Phase.Pieces:
                var end = Math.Min(f.pieces.Count, nextPiece + PiecesPerSlice);
                for (; nextPiece < end; nextPiece++)
                    if (f.pieces[nextPiece].kind != GlassPieceKind.Crush) EmitPiece(nextPiece);
                if (nextPiece >= f.pieces.Count) Next = Phase.Upload1;
                break;
            case Phase.Upload1:
                UploadStage1();
                Next = Phase.Upload2;
                break;
            case Phase.Upload2:
                UploadStage2();
                Next = Phase.Done;
                break;
        }
        var ms = ClockMs() - t0;
        PhaseMsMax[(int)phase] = Math.Max(PhaseMsMax[(int)phase], ms);
        if (SliceLog != null) SliceLog[(int)phase].Add(ms);
        SliceMsMax = Math.Max(SliceMsMax, ms);
        TotalMs += ms;
        Slices++;
        return Next == Phase.Done;
    }

    /// <summary>Slices until the budget (ms of this call) is spent; at least one. True when done.</summary>
    public bool StepFor(double budgetMs)
    {
        var start = ClockMs();
        do
        {
            if (Step()) return true;
        } while (ClockMs() - start < budgetMs);
        return false;
    }

    /// <summary>The CPU-only phases (fins, crater, pieces) at once, without the GPU upload: safe on a worker thread (no Unity
    /// API). FrontRoomsGlassBreakable's JIT warm-up uses it.</summary>
    public void BuildCpuOnly()
    {
        while (Next == Phase.Stage1 || Next == Phase.Pieces) Step();
    }

    /// <summary>Everything left, now (a beat arrived before the slices finished).</summary>
    public void Finish()
    {
        while (!Step())
        {
        }
    }

    public void ReleaseMeshes()
    {
        Destroy(Stage1); Stage1 = null;
        Destroy(Stage2); Stage2 = null;
        Next = Phase.Idle;
    }

    static void Destroy(Mesh m)
    {
        if (m == null) return;
        if (Application.isPlaying) UnityEngine.Object.Destroy(m); else UnityEngine.Object.DestroyImmediate(m);
    }

    // ---------------------------------------------------------------------------------------------------------
    // Stage 1: fins + crater
    // ---------------------------------------------------------------------------------------------------------
    void BuildStage1()
    {
        var key = new Vector4(o.keyA, o.keyB, 0f, KindFracture);
        var depth = h * FinDepthScale;
        for (var c = 0; c < f.cracks.Count; c++)
        {
            var crack = f.cracks[c];
            var sign = Rand01(f.seed, 11, c, 0) < .5 ? 1.0 : -1.0;
            for (var i = 0; i + 1 < crack.vn; i++)
            {
                var a = f.crackVerts[crack.v0 + i];
                var b = f.crackVerts[crack.v0 + i + 1];
                double ex = b.x - a.x, ey = b.y - a.y;
                var L = Math.Sqrt(ex * ex + ey * ey);
                if (L < 1e-5) continue;
                if (Rand01(f.seed, 12, c, i) < FinLeanFlipP) sign = -sign;
                var lean = sign * (FinLeanMinDeg + (FinLeanMaxDeg - FinLeanMinDeg) * Rand01(f.seed, 13, c, i)) * Math.PI / 180.0;
                var shear = Math.Tan(lean);
                double dx = ex / L, dy = ey / L, nx = -dy, ny = dx;
                double ux = dx * FinOverlap, uy = dy * FinOverlap;
                var g = (byte)(40 + (int)(215 * Rand01(f.seed, 14, c, i)));
                counts.fins++;
                // The two walls of the air gap (the gap prism's outward faces), sheared by the lean across the depth.
                for (var w = -1; w <= 1; w += 2)
                {
                    var off = w * FinHalfGap;
                    double lo = off - shear * depth, hi = off + shear * depth;
                    var p0 = new Vector3d(a.x - ux + nx * lo, a.y - uy + ny * lo, -depth);
                    var p1 = new Vector3d(b.x + ux + nx * lo, b.y + uy + ny * lo, -depth);
                    var p2 = new Vector3d(b.x + ux + nx * hi, b.y + uy + ny * hi, depth);
                    var p3 = new Vector3d(a.x - ux + nx * hi, a.y - uy + ny * hi, depth);
                    // outward normal of the wall: across the segment (sign w), leaned with the plane
                    var nrm = new Vector3d(nx * w, ny * w, -shear * w).Normalized();
                    var before = iS1.Count;
                    QuadDepth(vS1, iS1, p0, p1, p2, p3, nrm, key, g);
                    counts.finTris += (iS1.Count - before) / 3;
                }
            }
        }
        BuildCrater();
        counts.stage1Verts = vS1.Count;
        counts.stage1Tris = iS1.Count / 3;
    }

    void BuildCrater()
    {
        if (f.craterPoly.Count < 3) return;
        var key = new Vector4(o.keyA, o.keyB, 0f, KindCrushed);
        var ix = f.impact.x;
        var iy = f.impact.y;
        var far = f.farZSign;
        var faceZ = -far * h;               // the struck face (the breaker's side)
        var inward = far;                   // into the glass
        tmp.Clear();
        var n = f.craterPoly.Count;
        // desktop: the outline subdivided 3x (9 tris per crater vertex, <= 200); no-bevel tiers (WebGL): 3 tris per vertex (<= 60)
        var subdiv = bevel > 0 ? CraterSubdiv : 1;
        for (var i = 0; i < n; i++)
        {
            var p0 = f.craterPoly[i];
            var p1 = f.craterPoly[(i + 1) % n];
            for (var j = 0; j < subdiv; j++)
            {
                var t = j / (double)subdiv;
                var x = p0.x + (p1.x - p0.x) * t;
                var y = p0.y + (p1.y - p0.y) * t;
                var k = .72 + (1.04 - .72) * Rand01(f.seed, 21, i, j);
                tmp.Add(new GlassV2(ix + (x - ix) * k, iy + (y - iy) * k));
            }
        }
        var m = tmp.Count;
        rim.Clear();
        mid.Clear();
        for (var i = 0; i < m; i++)
        {
            var p = tmp[i];
            rim.Add(new Vector3d(p.x, p.y, faceZ - inward * CraterLift));
            var k = .3 + .35 * Rand01(f.seed, 22, i, 0);
            var d = CraterMidDepth0 + (CraterMidDepth1 - CraterMidDepth0) * Rand01(f.seed, 23, i, 0);
            mid.Add(new Vector3d(ix + (p.x - ix) * k, iy + (p.y - iy) * k, faceZ + inward * d));
        }
        var apex = new Vector3d(ix, iy, faceZ + inward * CraterApexDepth);
        var outward = new Vector3d(0, 0, -far);   // the pit faces the breaker
        var before = iS1.Count;
        for (var i = 0; i < m; i++)
        {
            var j = (i + 1) % m;
            Tri(vS1, iS1, rim[i], rim[j], mid[j], outward, key, 255, 128);
            Tri(vS1, iS1, rim[i], mid[j], mid[i], outward, key, 255, 128);
            Tri(vS1, iS1, mid[i], mid[j], apex, outward, key, 255, 128);
        }
        counts.craterTris = (iS1.Count - before) / 3;
    }

    // ---------------------------------------------------------------------------------------------------------
    // Stage 2: one closed, bevelled prism per piece at its stage-2 pose
    // ---------------------------------------------------------------------------------------------------------
    void EmitPiece(int pi)
    {
        var pc = f.pieces[pi];
        f.GetPoly(pi, poly);
        InsetConvex(poly, PieceInset, q0);
        if (!ValidInset(poly, q0)) { q0.Clear(); q0.AddRange(poly); }
        var n = q0.Count;
        if (n < 3) return;
        // the bevel: the profile's, reduced on short edges and thin corners until the inset outline is valid
        var b = bevel;
        var minEdge = double.MaxValue;
        for (var i = 0; i < n; i++) minEdge = Math.Min(minEdge, Dist(q0[i], q0[(i + 1) % n]));
        if (b > .4 * minEdge) { b = .4 * minEdge; counts.bevelClamped++; }
        if (b > 0)
        {
            InsetConvex(q0, b, qf);
            var tries = 0;
            while (!ValidInset(q0, qf) && tries++ < 6)
            {
                b *= .5;
                InsetConvex(q0, b, qf);
            }
            if (!ValidInset(q0, qf)) { b = 0; counts.bevelClamped++; }
            else if (tries > 0) counts.bevelClamped++;
        }
        if (b <= 0) { qf.Clear(); qf.AddRange(q0); }

        var pose = pc.kind == GlassPieceKind.Shard && !o.zeroPose;
        if (pose) PoseAffine(f, pi, out pT, out pM0, out pM1, out pM2);
        var key = new Vector4(o.keyA, o.keyB, pc.band, KindFace);
        var sideStart = iS2Side.Count;
        var faceStart = iS2Face.Count;
        var vStart = vS2.Count;

        // pane faces (front +z, back -z)
        EmitPolygon(qf, h, new Vector3d(0, 0, 1), key, 0, pc, pose);
        EmitPolygon(qf, -h, new Vector3d(0, 0, -1), key, 0, pc, pose);
        // crack faces: sides, bevel strips and corner triangles
        for (var i = 0; i < n; i++)
        {
            var j = (i + 1) % n;
            var a = q0[i];
            var c = q0[j];
            double ex = c.x - a.x, ey = c.y - a.y;
            var L = Math.Sqrt(ex * ex + ey * ey);
            double dx = ex / L, dy = ey / L;
            var nOut = new Vector3d(dy, -dx, 0);    // outward of a CCW polygon
            var onRim = OnSlabRim(poly, i, j);
            var kind = onRim ? KindEdge : KindFracture;
            var keyK = new Vector4(o.keyA, o.keyB, pc.band, kind);
            var g = (byte)(40 + (int)(215 * Rand01(f.seed, 31, pc.id, i)));
            var Ai = new GlassV2(a.x + dx * b, a.y + dy * b);
            var Bi = new GlassV2(c.x - dx * b, c.y - dy * b);
            // side face
            QuadPose(new Vector3d(Ai.x, Ai.y, -h + b), new Vector3d(Bi.x, Bi.y, -h + b), new Vector3d(Bi.x, Bi.y, h - b), new Vector3d(Ai.x, Ai.y, h - b), nOut, keyK, g, pc, pose);
            if (b > 0)
            {
                // top / bottom bevel strips of edge i
                var nTop = new Vector3d(nOut.x, nOut.y, 1).Normalized();
                var nBot = new Vector3d(nOut.x, nOut.y, -1).Normalized();
                QuadPose(new Vector3d(qf[i].x, qf[i].y, h), new Vector3d(qf[j].x, qf[j].y, h), new Vector3d(Bi.x, Bi.y, h - b), new Vector3d(Ai.x, Ai.y, h - b), nTop, keyK, g, pc, pose);
                QuadPose(new Vector3d(qf[i].x, qf[i].y, -h), new Vector3d(qf[j].x, qf[j].y, -h), new Vector3d(Bi.x, Bi.y, -h + b), new Vector3d(Ai.x, Ai.y, -h + b), nBot, keyK, g, pc, pose);
                // vertical strip and corner triangles at vertex j (between edge i and edge j)
                var c2 = q0[(j + 1) % n];
                double fx = c2.x - c.x, fy = c2.y - c.y;
                var L2 = Math.Sqrt(fx * fx + fy * fy);
                double d2x = fx / L2, d2y = fy / L2;
                var nOut2 = new Vector3d(d2y, -d2x, 0);
                var Aj = new GlassV2(c.x + d2x * b, c.y + d2y * b);       // edge j's side face start
                var nV = new Vector3d(nOut.x + nOut2.x, nOut.y + nOut2.y, 0).Normalized();
                var cornerKind = onRim && OnSlabRim(poly, j, (j + 1) % n) ? KindEdge : KindFracture;
                var keyC = new Vector4(o.keyA, o.keyB, pc.band, cornerKind);
                QuadPose(new Vector3d(Bi.x, Bi.y, -h + b), new Vector3d(Aj.x, Aj.y, -h + b), new Vector3d(Aj.x, Aj.y, h - b), new Vector3d(Bi.x, Bi.y, h - b), nV, keyC, g, pc, pose);
                var nCt = new Vector3d(nV.x, nV.y, 1.2).Normalized();
                var nCb = new Vector3d(nV.x, nV.y, -1.2).Normalized();
                TriPose(new Vector3d(qf[j].x, qf[j].y, h), new Vector3d(Bi.x, Bi.y, h - b), new Vector3d(Aj.x, Aj.y, h - b), nCt, keyC, g, pc, pose);
                TriPose(new Vector3d(qf[j].x, qf[j].y, -h), new Vector3d(Bi.x, Bi.y, -h + b), new Vector3d(Aj.x, Aj.y, -h + b), nCb, keyC, g, pc, pose);
            }
        }
        PieceIds.Add(pi);
        sideStartOfPiece.Add(sideStart);
        sideCountOfPiece.Add(iS2Side.Count - sideStart);
        faceStartOfPiece.Add(faceStart);
        faceCountOfPiece.Add(iS2Face.Count - faceStart);
        counts.pieces++;
        if (vS2.Count > 65000) throw new InvalidOperationException("glass stage 2: more than 65,000 vertices (16-bit indices)");
        _ = vStart;
    }

    void EmitPolygon(List<GlassV2> q, double z, Vector3d nrm, Vector4 key, byte g, GlassPiece pc, bool pose)
    {
        var baseV = vS2.Count;
        for (var k = 0; k < q.Count; k++) vS2.Add(MakeVertex(new Vector3d(q[k].x, q[k].y, z), nrm, key, g, pc, pose));
        // fan (convex); orient by the normal
        var flip = z < 0;   // a CCW polygon faces +z
        for (var k = 1; k + 1 < q.Count; k++)
        {
            if (!flip) { iS2Face.Add((ushort)baseV); iS2Face.Add((ushort)(baseV + k)); iS2Face.Add((ushort)(baseV + k + 1)); }
            else { iS2Face.Add((ushort)baseV); iS2Face.Add((ushort)(baseV + k + 1)); iS2Face.Add((ushort)(baseV + k)); }
        }
    }

    void QuadPose(Vector3d p0, Vector3d p1, Vector3d p2, Vector3d p3, Vector3d nrm, Vector4 key, byte g, GlassPiece pc, bool pose)
    {
        var baseV = vS2.Count;
        vS2.Add(MakeVertex(p0, nrm, key, g, pc, pose));
        vS2.Add(MakeVertex(p1, nrm, key, g, pc, pose));
        vS2.Add(MakeVertex(p2, nrm, key, g, pc, pose));
        vS2.Add(MakeVertex(p3, nrm, key, g, pc, pose));
        var front = Vector3d.Dot(Vector3d.Cross(p1 - p0, p2 - p0), nrm) >= 0;
        AddQuadIndices(iS2Side, baseV, front);
    }

    void TriPose(Vector3d p0, Vector3d p1, Vector3d p2, Vector3d outward, Vector4 key, byte g, GlassPiece pc, bool pose)
    {
        // the corner patch's own plane normal, turned outward
        var nrm = Vector3d.Cross(p1 - p0, p2 - p0).Normalized();
        if (Vector3d.Dot(nrm, outward) < 0) nrm = -nrm;
        var baseV = vS2.Count;
        vS2.Add(MakeVertex(p0, nrm, key, g, pc, pose));
        vS2.Add(MakeVertex(p1, nrm, key, g, pc, pose));
        vS2.Add(MakeVertex(p2, nrm, key, g, pc, pose));
        var front = Vector3d.Dot(Vector3d.Cross(p1 - p0, p2 - p0), nrm) >= 0;
        iS2Side.Add((ushort)baseV);
        iS2Side.Add((ushort)(baseV + (front ? 1 : 2)));
        iS2Side.Add((ushort)(baseV + (front ? 2 : 1)));
    }

    Vertex MakeVertex(Vector3d p, Vector3d nrm, Vector4 key, byte g, GlassPiece pc, bool pose)
    {
        var pos = p;
        var nn = nrm;
        if (pose)
        {
            // pose_point is a rigid motion: x' = T + M x, n' = M n (M, T from PosePoint once per piece, PoseAffine)
            pos = new Vector3d(pT.x + pM0.x * p.x + pM1.x * p.y + pM2.x * p.z, pT.y + pM0.y * p.x + pM1.y * p.y + pM2.y * p.z, pT.z + pM0.z * p.x + pM1.z * p.y + pM2.z * p.z);
            nn = new Vector3d(pM0.x * nrm.x + pM1.x * nrm.y + pM2.x * nrm.z, pM0.y * nrm.x + pM1.y * nrm.y + pM2.y * nrm.z, pM0.z * nrm.x + pM1.z * nrm.y + pM2.z * nrm.z).Normalized();
        }
        var edge = key.w > .5f ? (byte)255 : (byte)0;
        var depth = (byte)Mathf.Clamp(Mathf.RoundToInt((float)((p.z + h) / (2 * h)) * 255f), 0, 255);
        return new Vertex
        {
            position = new Vector3((float)pos.x, (float)pos.y, (float)pos.z),
            normal = new Vector3((float)nn.x, (float)nn.y, (float)nn.z),
            color = new Color32(edge, g, depth, 255),
            uv0 = new Vector2((float)p.x, (float)p.y),
            uv1 = key,
        };
    }

    Vector3d pT, pM0, pM1, pM2;

    /// <summary>
    /// THE stage-2 pose of piece pi (slab-local metres, rest -> stage 2): the seat tilt (shards only), then the generator's
    /// PosePoint. Teeth and the crushed core stay at rest. The mesh, the debris world (plan step 6: stage-3 pieces start
    /// exactly here) and G14 P4 all use this one function.
    /// </summary>
    public static Vector3d StagePoint(FrontRoomsGlassFracture f, int pi, Vector3d rest)
    {
        var pc = f.pieces[pi];
        if (pc.kind != GlassPieceKind.Shard) return rest;
        var p = Seat(f, pi, rest);
        FrontRoomsGlassFracturePattern.PosePoint(p.x, p.y, p.z, pc.posePivot, pc.poseAxis, pc.poseDeg, pc.posePush, f.farZSign, out var ox, out var oy, out var oz);
        return new Vector3d(ox, oy, oz);
    }

    /// <summary>The seat tilt alone: a rotation of SeatDegMin-SeatDegMax about a random in-plane axis through the piece's
    /// centroid (keyed off the pattern seed and the piece id; never the pattern's own RNG).</summary>
    public static Vector3d Seat(FrontRoomsGlassFracture f, int pi, Vector3d p)
    {
        var pc = f.pieces[pi];
        var deg = SeatDegMin + (SeatDegMax - SeatDegMin) * Rand01(f.seed, 41, pc.id, 0);
        var phi = 2.0 * Math.PI * Rand01(f.seed, 42, pc.id, 0);
        double ax = Math.Cos(phi), ay = Math.Sin(phi);
        var th = deg * Math.PI / 180.0;
        double c = Math.Cos(th), s = Math.Sin(th);
        // Rodrigues about the unit axis (ax, ay, 0) through (cx, cy, 0)
        double x = p.x - pc.centroid.x, y = p.y - pc.centroid.y, z = p.z;
        var kdot = ax * x + ay * y;
        double kx = ay * z, ky = -ax * z, kz = ax * y - ay * x;   // axis x p
        double rx = x * c + kx * s + ax * kdot * (1 - c);
        double ry = y * c + ky * s + ay * kdot * (1 - c);
        double rz = z * c + kz * s;
        return new Vector3d(rx + pc.centroid.x, ry + pc.centroid.y, rz);
    }

    /// <summary>Piece pi's stage-2 pose as an affine map x' = T + M x (StagePoint of the origin and the three unit axes).</summary>
    public static void PoseAffine(FrontRoomsGlassFracture f, int pi, out Vector3d T, out Vector3d M0, out Vector3d M1, out Vector3d M2)
    {
        T = StagePoint(f, pi, new Vector3d(0, 0, 0));
        M0 = StagePoint(f, pi, new Vector3d(1, 0, 0)) - T;
        M1 = StagePoint(f, pi, new Vector3d(0, 1, 0)) - T;
        M2 = StagePoint(f, pi, new Vector3d(0, 0, 1)) - T;
    }

    static void AddQuadIndices(List<ushort> idx, int baseV, bool front)
    {
        if (front)
        {
            idx.Add((ushort)baseV); idx.Add((ushort)(baseV + 1)); idx.Add((ushort)(baseV + 2));
            idx.Add((ushort)baseV); idx.Add((ushort)(baseV + 2)); idx.Add((ushort)(baseV + 3));
        }
        else
        {
            idx.Add((ushort)baseV); idx.Add((ushort)(baseV + 2)); idx.Add((ushort)(baseV + 1));
            idx.Add((ushort)baseV); idx.Add((ushort)(baseV + 3)); idx.Add((ushort)(baseV + 2));
        }
    }

    bool OnSlabRim(List<GlassV2> p, int i, int j)
    {
        const double e = 1e-6;
        var a = p[i];
        var c = p[j];
        return (Math.Abs(a.x - hx) < e && Math.Abs(c.x - hx) < e) || (Math.Abs(a.x + hx) < e && Math.Abs(c.x + hx) < e)
            || (Math.Abs(a.y - hy) < e && Math.Abs(c.y - hy) < e) || (Math.Abs(a.y + hy) < e && Math.Abs(c.y + hy) < e);
    }

    // ---------------------------------------------------------------------------------------------------------
    // Upload
    // ---------------------------------------------------------------------------------------------------------
    void UploadStage1()
    {
        if (Stage1 == null) Stage1 = NewMesh("GD3 stage 1 (fins + crater)");
        Upload(Stage1, vS1, iS1, f != null ? f.slabW : FrontRoomsGlassFracturePattern.SlabW, f != null ? f.slabH : FrontRoomsGlassFracturePattern.SlabH, .012);
    }

    void UploadStage2()
    {
        if (Stage2 == null) Stage2 = NewMesh("GD3 stage 2 (pieces at the stage-2 pose)");
        // sides first, then faces (blending order inside the one draw)
        iS2.Clear();
        iS2.AddRange(iS2Side);
        var sideTotal = iS2Side.Count;
        iS2.AddRange(iS2Face);
        PieceSides.Clear();
        PieceFaces.Clear();
        for (var k = 0; k < PieceIds.Count; k++)
        {
            PieceSides.Add(new FrontRoomsGlassRTRange { indexStart = sideStartOfPiece[k], indexCount = sideCountOfPiece[k], baseVertex = 0 });
            PieceFaces.Add(new FrontRoomsGlassRTRange { indexStart = sideTotal + faceStartOfPiece[k], indexCount = faceCountOfPiece[k], baseVertex = 0 });
        }
        Upload(Stage2, vS2, iS2, f != null ? f.slabW : FrontRoomsGlassFracturePattern.SlabW, f != null ? f.slabH : FrontRoomsGlassFracturePattern.SlabH, .02);
        counts.stage2Verts = vS2.Count;
        counts.stage2Tris = iS2.Count / 3;
        counts.stage2SideTris = sideTotal / 3;
    }

    /// <summary>
    /// G14 P4 (plan sec 4.7): the stage-2 vertices with the indices re-ordered per piece (each piece one contiguous range:
    /// its closed 6 mm slab), and the ranges, for FrontRoomsGlassRT.PrepareMeshes. Not drawn. Built on demand.
    /// </summary>
    public Mesh BuildPieceSource(List<FrontRoomsGlassRTRange> ranges, Mesh reuse = null)
    {
        ranges.Clear();
        var idx = new List<ushort>(iS2.Count);
        var sideTotal = iS2Side.Count;
        for (var k = 0; k < PieceIds.Count; k++)
        {
            var start = idx.Count;
            for (var t = 0; t < sideCountOfPiece[k]; t++) idx.Add(iS2Side[sideStartOfPiece[k] + t]);
            for (var t = 0; t < faceCountOfPiece[k]; t++) idx.Add(iS2Face[faceStartOfPiece[k] + t]);
            ranges.Add(new FrontRoomsGlassRTRange { indexStart = start, indexCount = idx.Count - start, baseVertex = 0 });
        }
        var mesh = reuse != null ? reuse : NewMesh("GD3 stage 2 pieces (RT source, per-piece ranges)");
        Upload(mesh, vS2, idx, f.slabW, f.slabH, .02);
        _ = sideTotal;
        return mesh;
    }

    static Mesh NewMesh(string name)
    {
        var m = new Mesh { name = name };
        m.MarkDynamic();
        return m;
    }

    static void Upload(Mesh mesh, List<Vertex> v, List<ushort> idx, double w, double hgt, double depth)
    {
        mesh.Clear();
        const MeshUpdateFlags flags = MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers;
        mesh.SetVertexBufferParams(v.Count, Layout);
        if (v.Count > 0) mesh.SetVertexBufferData(v, 0, 0, v.Count, 0, flags);
        mesh.SetIndexBufferParams(idx.Count, IndexFormat.UInt16);
        if (idx.Count > 0) mesh.SetIndexBufferData(idx, 0, 0, idx.Count, flags);
        var bounds = new Bounds(Vector3.zero, new Vector3((float)w + .01f, (float)hgt + .01f, (float)depth + .01f));
        mesh.subMeshCount = 1;
        mesh.SetSubMesh(0, new SubMeshDescriptor(0, idx.Count) { bounds = bounds, vertexCount = v.Count }, flags);
        mesh.bounds = bounds;
    }

    static Vertex V(Vector3d p, Vector3d nrm, Vector4 key, byte r, byte g, byte depth) => new Vertex
    {
        position = new Vector3((float)p.x, (float)p.y, (float)p.z),
        normal = new Vector3((float)nrm.x, (float)nrm.y, (float)nrm.z),
        color = new Color32(r, g, depth, 255),
        uv0 = new Vector2((float)p.x, (float)p.y),
        uv1 = key,
    };

    byte Depth(Vector3d p) => (byte)Mathf.Clamp(Mathf.RoundToInt((float)((p.z + h) / (2 * h)) * 255f), 0, 255);

    static void Quad(List<Vertex> v, List<ushort> idx, Vector3d p0, Vector3d p1, Vector3d p2, Vector3d p3, Vector3d nrm, Vector4 key, byte r, byte g)
    {
        var baseV = v.Count;
        v.Add(V(p0, nrm, key, r, g, 128)); v.Add(V(p1, nrm, key, r, g, 128)); v.Add(V(p2, nrm, key, r, g, 128)); v.Add(V(p3, nrm, key, r, g, 128));
        AddQuadIndices(idx, baseV, Vector3d.Dot(Vector3d.Cross(p1 - p0, p2 - p0), nrm) >= 0);
    }

    void QuadDepth(List<Vertex> v, List<ushort> idx, Vector3d p0, Vector3d p1, Vector3d p2, Vector3d p3, Vector3d nrm, Vector4 key, byte g)
    {
        var baseV = v.Count;
        v.Add(V(p0, nrm, key, 255, g, Depth(p0))); v.Add(V(p1, nrm, key, 255, g, Depth(p1)));
        v.Add(V(p2, nrm, key, 255, g, Depth(p2))); v.Add(V(p3, nrm, key, 255, g, Depth(p3)));
        AddQuadIndices(idx, baseV, Vector3d.Dot(Vector3d.Cross(p1 - p0, p2 - p0), nrm) >= 0);
    }

    void Tri(List<Vertex> v, List<ushort> idx, Vector3d p0, Vector3d p1, Vector3d p2, Vector3d facing, Vector4 key, byte r, byte g)
    {
        var nrm = Vector3d.Cross(p1 - p0, p2 - p0).Normalized();
        var front = Vector3d.Dot(nrm, facing) >= 0;
        if (!front) nrm = -nrm;
        var baseV = v.Count;
        v.Add(V(p0, nrm, key, r, g, Depth(p0))); v.Add(V(p1, nrm, key, r, g, Depth(p1))); v.Add(V(p2, nrm, key, r, g, Depth(p2)));
        idx.Add((ushort)baseV);
        idx.Add((ushort)(baseV + (front ? 1 : 2)));
        idx.Add((ushort)(baseV + (front ? 2 : 1)));
    }

    // ---------------------------------------------------------------------------------------------------------
    // Geometry helpers
    // ---------------------------------------------------------------------------------------------------------
    /// <summary>Offset a convex CCW polygon inward by d (glass_lookdev.py inset_poly).</summary>
    public static void InsetConvex(List<GlassV2> p, double d, List<GlassV2> dst)
    {
        dst.Clear();
        var n = p.Count;
        for (var i = 0; i < n; i++)
        {
            var a0 = p[(i - 1 + n) % n];
            var a1 = p[i];
            var b1 = p[(i + 1) % n];
            // edge i-1: a0 -> a1, edge i: a1 -> b1, each moved inward (left of a CCW edge) by d
            double adx = a1.x - a0.x, ady = a1.y - a0.y, la = Math.Sqrt(adx * adx + ady * ady);
            double bdx = b1.x - a1.x, bdy = b1.y - a1.y, lb = Math.Sqrt(bdx * bdx + bdy * bdy);
            if (la < 1e-12 || lb < 1e-12) { dst.Add(a1); continue; }
            double anx = -ady / la, any = adx / la, bnx = -bdy / lb, bny = bdx / lb;
            double ax = a0.x + anx * d, ay = a0.y + any * d;
            double bx = a1.x + bnx * d, by = a1.y + bny * d;
            var den = adx * bdy - ady * bdx;
            if (Math.Abs(den) < 1e-18) { dst.Add(new GlassV2(bx, by)); continue; }
            var t = ((bx - ax) * bdy - (by - ay) * bdx) / den;
            dst.Add(new GlassV2(ax + adx * t, ay + ady * t));
        }
    }

    /// <summary>The inset outline keeps the orientation of every edge (no edge flipped or collapsed) and stays convex.</summary>
    static bool ValidInset(List<GlassV2> outer, List<GlassV2> inner)
    {
        var n = outer.Count;
        if (inner.Count != n) return false;
        for (var i = 0; i < n; i++)
        {
            var j = (i + 1) % n;
            double ox = outer[j].x - outer[i].x, oy = outer[j].y - outer[i].y;
            double ix = inner[j].x - inner[i].x, iy = inner[j].y - inner[i].y;
            if (ox * ix + oy * iy <= 1e-14) return false;
            var k = inner[(i + 2) % n];
            if ((ix) * (k.y - inner[j].y) - (iy) * (k.x - inner[j].x) <= 0) return false;
        }
        return true;
    }

    static double Dist(GlassV2 a, GlassV2 b) => Math.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));

    /// <summary>A keyed uniform in [0, 1): look decisions (fin lean, crater outline) never touch the pattern's own RNG.</summary>
    public static double Rand01(uint seed, int salt, int a, int b)
    {
        unchecked
        {
            var z = (ulong)seed * 0x9E3779B97F4A7C15UL ^ (ulong)(uint)salt * 0xC2B2AE3D27D4EB4FUL ^ (ulong)(uint)a * 0x165667B19E3779F9UL ^ (ulong)(uint)b * 0x27D4EB2F165667C5UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;
            return (z >> 11) * (1.0 / 9007199254740992.0);
        }
    }

    public struct Vector3d
    {
        public double x, y, z;
        public Vector3d(double x, double y, double z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3d operator -(Vector3d a, Vector3d b) => new Vector3d(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3d operator -(Vector3d a) => new Vector3d(-a.x, -a.y, -a.z);
        public static double Dot(Vector3d a, Vector3d b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public static Vector3d Cross(Vector3d a, Vector3d b) => new Vector3d(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        public Vector3d Normalized()
        {
            var l = Math.Sqrt(x * x + y * y + z * z);
            return l > 1e-30 ? new Vector3d(x / l, y / l, z / l) : this;
        }
    }
}
