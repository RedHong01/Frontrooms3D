// FrontRoomsGlassFracture.cs -- GD3 step 3: one generated fracture pattern (the C# twin of crack_graph_ref.py's JSON).
// Pure C#. Polygons live in shared vertex lists addressed by (v0, vn) ranges, so a result can be reused for the next
// break without garbage (Reset()). Coordinates are slab-local metres: x = window-root +X, y up, origin = slab centre =
// window root (0, 1.175, 0); z (pose_point) across the glass, +z toward cell b.
using System;
using System.Collections.Generic;

public enum GlassPieceKind : byte
{
    Crush = 0,      // the crushed core: powder, crumbs and the crater, never a body
    Shard = 1,      // a free piece: a moving body at the shatter
    Tooth = 2,      // stays in the stop (tooth band)
    Clump = 3,      // tempered glass: a clump of ~9 mm granules
}

public enum GlassCorner : byte { None = 0, TL = 1, TR = 2, BL = 3, BR = 4 }

[Serializable]
public struct GlassRange
{
    public int v0, vn;

    public GlassRange(int v0, int vn)
    {
        this.v0 = v0;
        this.vn = vn;
    }
}

[Serializable]
public struct GlassPiece
{
    public int id;
    public GlassPieceKind kind;
    public int v0, vn;                  // polygon: FrontRoomsGlassFracture.verts[v0 .. v0 + vn), CCW
    public double area;                 // m2 (rounded like the reference)
    public GlassV2 centroid;
    public double r;                    // centroid distance from the impact
    public int band, sector, cluster;   // hierarchy (cluster = band * 64 + sector)
    public char side;                   // tooth: 'L', 'R', 'T', 'B'; else '\0'
    public GlassCorner corner;          // corner tooth
    public GlassV2 posePivot, poseAxis; // stage-2 pose (pose_point)
    public double poseDeg, posePush;
    public bool hasRelease;             // shards only
    public int releaseWave;             // 0 inner, 1 middle, 2 outer
    public double releaseMs, releaseSpeed;
    public int s0, sn;                  // level-2 sub-cells: FrontRoomsGlassFracture.subPolys[s0 .. s0 + sn)
    public int granules;                // tempered clumps only

    public string EdgeName
    {
        get
        {
            switch (side)
            {
                case 'L': return "left";
                case 'R': return "right";
                case 'T': return "top";
                case 'B': return "bottom";
                default: return null;
            }
        }
    }
}

[Serializable]
public struct GlassTrackOut
{
    public int id, parent;
    public bool initial;
    public int v0, vn;                  // FrontRoomsGlassFracture.graphVerts
}

[Serializable]
public struct GlassChordOut
{
    public int ring, a, b;
    public GlassV2 p0, p1;
}

[Serializable]
public struct GlassForkOut
{
    public int ring, parent, child;
    public GlassV2 at;
}

[Serializable]
public struct GlassCrack
{
    public int track;
    public double reach, reachFrac;
    public int v0, vn;                  // FrontRoomsGlassFracture.crackVerts
}

/// <summary>Who owns a graph edge: a track (a = tid), a chord (ring, a, b), the core ring or the outer ring (a = index).</summary>
[Serializable]
public struct GlassOwner
{
    public const byte Track = 0, Chord = 1, Core = 2, Outer = 3;
    public byte kind;
    public int a, b, c;

    public static GlassOwner OfTrack(int tid) => new GlassOwner { kind = Track, a = tid };
    public static GlassOwner OfChord(int ring, int ta, int tb) => new GlassOwner { kind = Chord, a = ring, b = ta, c = tb };
    public static GlassOwner OfIndex(byte kind, int i) => new GlassOwner { kind = kind, a = i };
}

public enum GlassRepairKind : byte
{
    BuildErrorDampKinks, CrossingDropChord, CrossingDropFork, CrossingDampKinks, NonconvexFaceDampKinks,
    SmallDropChord, SmallDropFork, SmallMergeNeighbour, SmallUnfixed, CapDropOuterChord, CapDropFork, CapUnmet,
}

[Serializable]
public struct GlassRepair
{
    public GlassRepairKind kind;
    public GlassOwner o1, o2;           // chord / fork keys (o1), crossing owners (o1, o2)
    public double value;                // small_merge_neighbour, small_unfixed (cm2, rounded to 1e-4)
    public int count;                   // cap_unmet
    public string text;                 // build_error_damp_kinks

    public static string Name(GlassRepairKind k)
    {
        switch (k)
        {
            case GlassRepairKind.BuildErrorDampKinks: return "build_error_damp_kinks";
            case GlassRepairKind.CrossingDropChord: return "crossing_drop_chord";
            case GlassRepairKind.CrossingDropFork: return "crossing_drop_fork";
            case GlassRepairKind.CrossingDampKinks: return "crossing_damp_kinks";
            case GlassRepairKind.NonconvexFaceDampKinks: return "nonconvex_face_damp_kinks";
            case GlassRepairKind.SmallDropChord: return "small_drop_chord";
            case GlassRepairKind.SmallDropFork: return "small_drop_fork";
            case GlassRepairKind.SmallMergeNeighbour: return "small_merge_neighbour";
            case GlassRepairKind.SmallUnfixed: return "small_unfixed";
            case GlassRepairKind.CapDropOuterChord: return "cap_drop_outer_chord";
            case GlassRepairKind.CapDropFork: return "cap_drop_fork";
            default: return "cap_unmet";
        }
    }
}

[Serializable]
public struct GlassFrame
{
    public double hx, hy;
    public double pocketL, pocketR, pocketT, pocketB;
    public double bandL, bandR, bandT, bandB;
    public string bandMode;             // "band" (tooth band, default) or "stop" (fallback: no visible teeth)
    public double stopX0, stopX1, stopY0, stopY1;
    public double clearX0, clearX1, clearY0, clearY1;

    public double Band(char side) => side == 'L' ? bandL : side == 'R' ? bandR : side == 'T' ? bandT : bandB;
    public double Pocket(char side) => side == 'L' ? pocketL : side == 'R' ? pocketR : side == 'T' ? pocketT : pocketB;
}

[Serializable]
public struct GlassStats
{
    public int crush, shard, tooth, clump;
    public int piecesTotal, bodies;
    public int tracksInitial, tracksTotal, forks, chords, rings;
    public int tJunctions, plusJunctions, kinks, kinksZeroed;
    public double shardAreaCm2P10, shardAreaCm2P50, shardAreaCm2P90;
    public double maxPieceLen, avgVerts;
    public int trisStage2, trisTeeth, trisFins, trisCrater;
    public int level2Cells, stage1Cracks;
    public long granulesEstimate;       // dice mode
}

/// <summary>
/// One fracture pattern: the crack graph, the pieces with their hierarchy, stage-2 poses and release waves, the
/// stage-1 cracks, crater and chips, and the reference's stats. Built by FrontRoomsGlassFracturePattern.
/// </summary>
public sealed class FrontRoomsGlassFracture
{
    public const string Version = "frontrooms-glass-crackgraph-ref 1.3";

    public string profile, glass, tier;
    public uint seed;
    public double rotationDeg;
    public int side, farZSign;
    public GlassV2 impact, impactRoot, impactUV;
    public double slabW, slabH, slabT, bevel, centreRootY;
    public GlassFrame frame;
    public bool isDice;
    public double granule;

    public readonly List<double> rings = new List<double>();

    // crack graph (slab coords; edges outside the slab are kept whole, for debugging)
    public readonly List<GlassV2> graphVerts = new List<GlassV2>();
    public readonly List<GlassTrackOut> tracks = new List<GlassTrackOut>();
    public readonly List<GlassChordOut> chords = new List<GlassChordOut>();
    public readonly List<GlassForkOut> forks = new List<GlassForkOut>();

    // pieces (sorted: crush, shards, teeth; then by distance from the impact, then by angle)
    public readonly List<GlassPiece> pieces = new List<GlassPiece>();
    public readonly List<GlassV2> verts = new List<GlassV2>();
    public readonly List<GlassRange> subPolys = new List<GlassRange>();
    public readonly List<GlassV2> subVerts = new List<GlassV2>();

    // stage 1 (Crack1 fins + crater + chips) and stage 2 (chips; the crush core is removed)
    public readonly List<GlassCrack> cracks = new List<GlassCrack>();
    public readonly List<GlassV2> crackVerts = new List<GlassV2>();
    public readonly List<GlassV2> craterPoly = new List<GlassV2>();
    public double craterRadius;
    public readonly List<GlassV2> chips1 = new List<GlassV2>();
    public readonly List<GlassV2> chips2 = new List<GlassV2>();

    public double wireGrid;
    public bool wireHangs;

    public readonly List<GlassRepair> repairs = new List<GlassRepair>();
    public int attempts;
    public GlassStats stats;

    public FrontRoomsGlassFracture()
    {
        // above the largest break (Cinematic: ~170 pieces, ~1,000 piece vertices, ~1,300 level-2 vertices)
        pieces.Capacity = 256;
        verts.Capacity = 2048;
        subPolys.Capacity = 512;
        subVerts.Capacity = 4096;
        graphVerts.Capacity = 1024;
        tracks.Capacity = 64;
        chords.Capacity = 256;
        forks.Capacity = 64;
        crackVerts.Capacity = 256;
        repairs.Capacity = 64;
    }

    public void Reset()
    {
        profile = glass = tier = null;
        seed = 0;
        rotationDeg = 0.0;
        side = farZSign = 0;
        impact = impactRoot = impactUV = default(GlassV2);
        slabW = slabH = slabT = bevel = centreRootY = 0.0;
        frame = default(GlassFrame);
        isDice = false;
        granule = 0.0;
        rings.Clear();
        graphVerts.Clear();
        tracks.Clear();
        chords.Clear();
        forks.Clear();
        pieces.Clear();
        verts.Clear();
        subPolys.Clear();
        subVerts.Clear();
        cracks.Clear();
        crackVerts.Clear();
        craterPoly.Clear();
        craterRadius = 0.0;
        chips1.Clear();
        chips2.Clear();
        wireGrid = 0.0;
        wireHangs = false;
        repairs.Clear();
        attempts = 0;
        stats = default(GlassStats);
    }

    /// <summary>Copies piece i's polygon into dst (cleared first).</summary>
    public void GetPoly(int piece, List<GlassV2> dst)
    {
        dst.Clear();
        var p = pieces[piece];
        for (var k = 0; k < p.vn; k++)
            dst.Add(verts[p.v0 + k]);
    }

    /// <summary>Stage-2 pose of slab point (x, y, z) for piece i (pose_point; z across the glass, +z toward cell b).</summary>
    public void PosePoint(int piece, double x, double y, double z, out double px, out double py, out double pz)
    {
        var p = pieces[piece];
        FrontRoomsGlassFracturePattern.PosePoint(x, y, z, p.posePivot, p.poseAxis, p.poseDeg, p.posePush, farZSign, out px, out py, out pz);
    }
}
