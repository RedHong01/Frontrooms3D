// FrontRoomsGlassFractureValidation.cs -- GD3 step 3: crack_graph_ref.py validate() in C# (plan 10 sec 2.6: "validation on
// every build, also an edit-mode test"). Works on the ROUNDED output, like the reference. Pure C#; reusable buffers.
using System;
using System.Collections.Generic;
using System.Text;

public sealed class FrontRoomsGlassFractureValidation
{
    public bool ran;
    // area_sum: the pieces tile the slab within +-0.05 % (prototype fix 4: holes fail)
    public bool areaSumOk;
    public double areaRelErrorPct;
    // no_overlap: worst pairwise overlap < 1e-7 m2 (0.1 mm2; outputs are rounded to 1e-7 m)
    public bool noOverlapOk;
    public double worstOverlapMm2;
    // convex: every piece convex
    public bool convexOk;
    public readonly List<int> nonConvexIds = new List<int>();
    // min_area: no piece (or level-2 cell) under 1 cm2 outside the crush core
    public bool minAreaOk;
    public double minCm2;
    public readonly List<int> smallIds = new List<int>();
    public readonly List<int> smallSubOf = new List<int>();
    // no_crossings: the crack graph (tracks + chords) has no crossing inside the slab (prototype fix 1)
    public bool hasGraph, noCrossingsOk;
    public int crossings;
    // teeth_in_band: teeth stay in the tooth band, held by the edge, nothing in the clear zone (prototype fix 3)
    public bool teethOk;
    public readonly List<int> teethBad = new List<int>();
    public int teeth;
    // impact_clamp: impact 0.2 m inside the exposed glass
    public bool impactClampOk;
    // budget (crack-graph mode): pieces, bodies, stage-2 / teeth / fin / crater triangles
    public bool hasBudget, budgetOk;
    // v2_t_not_plus: ring cracks end in T's, never '+'
    public bool v2Ok;
    public int plusJunctions, tJunctions;
    // no_unfixed_repairs
    public bool repairsOk;
    // crater <= 15 mm (V5)
    public bool craterOk;
    public double craterMm;
    // area_field: stored areas match the polygons
    public bool areaFieldOk;
    public double areaFieldWorstMm2;
    // stage1_on_stage2: every Crack1 fin lies on a radial of the stage-2 graph
    public bool hasStage1Check, stage1Ok;
    public double stage1WorstMm;

    public bool AllOk =>
        ran && areaSumOk && noOverlapOk && convexOk && minAreaOk && (!hasGraph || noCrossingsOk) && teethOk && impactClampOk &&
        (!hasBudget || (budgetOk && v2Ok && repairsOk && craterOk)) && areaFieldOk && (!hasStage1Check || stage1Ok);

    /// <summary>
    /// Every check except the count/triangle budget (plan 10 sec 4.6 budgets are estimates). The reference itself fails 4 of
    /// 1,015 cross-check breaks on the teeth-triangle budget alone (2,020-2,120 vs 2,000; build/03_generator.md sec 6), so
    /// the stage builder (step 5) can tell "geometry broken: use the bake fallback" from "a few triangles over budget".
    /// </summary>
    public bool AllOkButBudget =>
        ran && areaSumOk && noOverlapOk && convexOk && minAreaOk && (!hasGraph || noCrossingsOk) && teethOk && impactClampOk &&
        (!hasBudget || (v2Ok && repairsOk && craterOk)) && areaFieldOk && (!hasStage1Check || stage1Ok);

    // buffers
    readonly List<GlassV2> a = new List<GlassV2>(), b = new List<GlassV2>(), s0 = new List<GlassV2>(), s1 = new List<GlassV2>(), s2 = new List<GlassV2>();
    readonly List<double> bx0 = new List<double>(), bx1 = new List<double>(), by0 = new List<double>(), by1 = new List<double>();
    readonly List<GlassV2> gv = new List<GlassV2>();
    readonly Dictionary<long, int> vIndex = new Dictionary<long, int>();
    readonly Dictionary<long, int> ends = new Dictionary<long, int>();
    readonly List<int> eu = new List<int>(), ev = new List<int>();
    readonly List<Box> boxes = new List<Box>();
    readonly List<long> found = new List<long>();

    struct Box
    {
        public double x0, x1, y0, y1;
        public int idx;
    }

    static readonly Comparison<Box> BoxOrder = (A, B) =>
    {
        if (A.x0 != B.x0) return A.x0 < B.x0 ? -1 : 1;
        if (A.x1 != B.x1) return A.x1 < B.x1 ? -1 : 1;
        if (A.y0 != B.y0) return A.y0 < B.y0 ? -1 : 1;
        if (A.y1 != B.y1) return A.y1 < B.y1 ? -1 : 1;
        return A.idx.CompareTo(B.idx);
    };

    public void Reset()
    {
        ran = false;
        nonConvexIds.Clear();
        smallIds.Clear();
        smallSubOf.Clear();
        teethBad.Clear();
    }

    void Poly(FrontRoomsGlassFracture r, int i, List<GlassV2> dst)
    {
        dst.Clear();
        var p = r.pieces[i];
        for (var k = 0; k < p.vn; k++)
            dst.Add(r.verts[p.v0 + k]);
    }

    /// <summary>Validates a result now (all slices).</summary>
    public void Run(FrontRoomsGlassFracture r, FrontRoomsGlassProfileData P)
    {
        Begin(r, P);
        while (!Step())
        {
        }
    }

    FrontRoomsGlassFracture vr;
    FrontRoomsGlassProfileData vp;
    GlassFrame vfr;
    int stage, row;
    double worst;

    /// <summary>Overlap rows tested per slice (O(n^2) broad phase + convex clipping).</summary>
    public static int OverlapRowsPerStep = 12;

    public void Begin(FrontRoomsGlassFracture r, FrontRoomsGlassProfileData P)
    {
        Reset();
        ran = true;
        vr = r;
        vp = P;
        vfr = FrontRoomsGlassFracturePattern.FrameForSlab(r.slabW, r.slabH, r.frame.bandMode ?? "band");
        stage = 0;
        row = 0;
        worst = 0.0;
    }

    /// <summary>One slice of the validation; true when finished.</summary>
    public bool Step()
    {
        var r = vr;
        var P = vp;
        var fr = vfr;
        var w = r.slabW;
        var h = r.slabH;
        var n = r.pieces.Count;
        switch (stage)
        {
            case 0:
            {
                // area sum
                var total = 0.0;
                for (var i = 0; i < n; i++)
                    total += GlassPoly.Area(r.verts, r.pieces[i].v0, r.pieces[i].vn);
                var rel = (total - w * h) / (w * h);
                areaSumOk = Math.Abs(rel) <= 0.0005;
                areaRelErrorPct = rel * 100.0;
                // bounding boxes for the overlap broad phase
                bx0.Clear(); bx1.Clear(); by0.Clear(); by1.Clear();
                for (var i = 0; i < n; i++)
                {
                    var p = r.pieces[i];
                    double x0 = double.PositiveInfinity, x1 = double.NegativeInfinity, y0 = double.PositiveInfinity, y1 = double.NegativeInfinity;
                    for (var k = 0; k < p.vn; k++)
                    {
                        var q = r.verts[p.v0 + k];
                        x0 = Math.Min(x0, q.x); x1 = Math.Max(x1, q.x);
                        y0 = Math.Min(y0, q.y); y1 = Math.Max(y1, q.y);
                    }
                    bx0.Add(x0); bx1.Add(x1); by0.Add(y0); by1.Add(y1);
                }
                // convex
                for (var i = 0; i < n; i++)
                {
                    Poly(r, i, a);
                    if (!GlassPoly.IsConvex(a, 1e-11))
                        nonConvexIds.Add(r.pieces[i].id);
                }
                convexOk = nonConvexIds.Count == 0;
                // min area (outside the crush core), level-2 cells too
                var mn = double.PositiveInfinity;
                var any = false;
                for (var i = 0; i < n; i++)
                {
                    var p = r.pieces[i];
                    if (p.kind == GlassPieceKind.Crush)
                        continue;
                    if (p.area < P.minPieceArea - 1e-12)
                        smallIds.Add(p.id);
                    for (var k = 0; k < p.sn; k++)
                    {
                        var sr = r.subPolys[p.s0 + k];
                        if (GlassPoly.Area(r.subVerts, sr.v0, sr.vn) < P.minPieceArea - 1e-12)
                            smallSubOf.Add(p.id);
                    }
                    mn = any ? Math.Min(mn, p.area) : p.area;
                    any = true;
                }
                minAreaOk = smallIds.Count == 0 && smallSubOf.Count == 0;
                minCm2 = (any ? mn : 0.0) * 1e4;
                stage = 1;
                row = 0;
                worst = 0.0;
                return false;
            }
            case 1:
            {
                // overlaps (bbox broad phase), a few rows per slice
                var end = Math.Min(n, row + OverlapRowsPerStep);
                for (var i = row; i < end; i++)
                {
                    Poly(r, i, a);
                    for (var j = i + 1; j < n; j++)
                    {
                        if (bx1[i] < bx0[j] || bx1[j] < bx0[i] || by1[i] < by0[j] || by1[j] < by0[i])
                            continue;
                        Poly(r, j, b);
                        worst = Math.Max(worst, GlassPoly.ConvexOverlapArea(a, b, s0, s1));
                    }
                }
                row = end;
                if (row >= n)
                {
                    noOverlapOk = worst < 1e-7;
                    worstOverlapMm2 = worst * 1e6;
                    stage = 2;
                }
                return false;
            }
            case 2:
            {
                // crossings of the crack graph (tracks + chords) inside the slab
                hasGraph = !r.isDice && r.tracks.Count > 0;
                if (hasGraph)
                {
                    gv.Clear();
                    vIndex.Clear();
                    eu.Clear();
                    ev.Clear();
                    for (var ti = 0; ti < r.tracks.Count; ti++)
                    {
                        var t = r.tracks[ti];
                        var prev = -1;
                        for (var k = 0; k < t.vn; k++)
                        {
                            var id = Vid(r.graphVerts[t.v0 + k]);
                            if (k > 0)
                            {
                                eu.Add(prev);
                                ev.Add(id);
                            }
                            prev = id;
                        }
                    }
                    for (var i = 0; i < r.chords.Count; i++)
                    {
                        eu.Add(Vid(r.chords[i].p0));
                        ev.Add(Vid(r.chords[i].p1));
                    }
                    crossings = CountCrossings(-w / 2, w / 2, -h / 2, h / 2);
                    noCrossingsOk = crossings == 0;
                }
                stage = 3;
                return false;
            }
        }
        Finish(r, P, fr, n);
        stage = 4;
        return true;
    }

    void Finish(FrontRoomsGlassFracture r, FrontRoomsGlassProfileData P, GlassFrame fr, int n)
    {
        // teeth: inside the band, held by the edge, nothing in the clear zone
        teeth = 0;
        for (var i = 0; i < n; i++)
        {
            var p = r.pieces[i];
            if (p.kind != GlassPieceKind.Tooth)
                continue;
            teeth++;
            Poly(r, i, a);
            if (InClear(a, fr) > 1e-10 || MaxContact(a, fr) < 0.004)
                teethBad.Add(p.id);
            for (var k = 0; k < a.Count; k++)
                if (Math.Abs(a[k].x) > fr.hx + 1e-9 || Math.Abs(a[k].y) > fr.hy + 1e-9)
                {
                    teethBad.Add(p.id);
                    break;
                }
        }
        teethOk = teethBad.Count == 0;
        // impact clamp
        var m = FrontRoomsGlassFracturePattern.ImpactMinFromStop - 1e-9;
        var ix = r.impact.x;
        var iy = r.impact.y;
        impactClampOk = fr.stopX0 + m <= ix && ix <= fr.stopX1 - m && fr.stopY0 + m <= iy && iy <= fr.stopY1 - m;
        hasBudget = !r.isDice;
        if (hasBudget)
        {
            var s = r.stats;
            budgetOk = s.piecesTotal <= P.maxPieces && s.bodies <= P.maxBodies && s.trisStage2 <= P.budgetS2Tris &&
                       s.trisTeeth <= P.budgetTeethTris && s.trisFins <= P.budgetFinTris && s.trisCrater <= P.budgetCraterTris;
            // V2 from the geometry: every ring-crack end is a T on a radial, never a '+'
            ends.Clear();
            for (var i = 0; i < r.chords.Count; i++)
            {
                AddEnd(r.chords[i].p0);
                AddEnd(r.chords[i].p1);
            }
            plusJunctions = 0;
            foreach (var kv in ends)
                if (kv.Value > 1)
                    plusJunctions++;
            tJunctions = ends.Count + s.forks;
            v2Ok = plusJunctions == 0;
            repairsOk = true;
            for (var i = 0; i < r.repairs.Count; i++)
                if (r.repairs[i].kind == GlassRepairKind.SmallUnfixed)
                    repairsOk = false;
            craterOk = r.craterRadius <= 0.015 + 1e-9;
            craterMm = r.craterRadius * 1000.0;
        }
        // stored areas match the polygons
        var wa = 0.0;
        for (var i = 0; i < n; i++)
            wa = Math.Max(wa, Math.Abs(r.pieces[i].area - GlassPoly.Area(r.verts, r.pieces[i].v0, r.pieces[i].vn)));
        areaFieldOk = wa < 2e-7;
        areaFieldWorstMm2 = wa * 1e6;
        // stage 1 is a subset of stage 2
        hasStage1Check = !r.isDice && r.tracks.Count > 0;
        if (hasStage1Check)
        {
            var worstD = 0.0;
            for (var ci = 0; ci < r.cracks.Count; ci++)
            {
                var c = r.cracks[ci];
                var tIndex = -1;
                for (var ti = 0; ti < r.tracks.Count; ti++)
                    if (r.tracks[ti].id == c.track)
                    {
                        tIndex = ti;
                        break;
                    }
                for (var k = 0; k < c.vn; k++)
                {
                    var q = r.crackVerts[c.v0 + k];
                    var d = 1.0;
                    if (tIndex >= 0)
                    {
                        var t = r.tracks[tIndex];
                        var have = false;
                        for (var i = 0; i < t.vn - 1; i++)
                        {
                            var dd = GlassPoly.DistPointSeg(q, r.graphVerts[t.v0 + i], r.graphVerts[t.v0 + i + 1]);
                            d = have ? Math.Min(d, dd) : dd;
                            have = true;
                        }
                    }
                    worstD = Math.Max(worstD, d);
                }
            }
            stage1Ok = worstD < 1e-6;
            stage1WorstMm = worstD * 1000.0;
        }
    }

    static long RoundKey(GlassV2 p, int nd)
    {
        long qx, qy;
        GlassMath.PyRoundScaled(p.x, nd, out qx);
        GlassMath.PyRoundScaled(p.y, nd, out qy);
        return unchecked(qx * 1000003L * 1000003L + qy);
    }

    int Vid(GlassV2 p)
    {
        // vid(p): vertices merged by (round(x, 9), round(y, 9))
        long qx, qy;
        GlassMath.PyRoundScaled(p.x, 9, out qx);
        GlassMath.PyRoundScaled(p.y, 9, out qy);
        var key = unchecked(qx * 73856093L ^ qy * 19349663L ^ (qx << 21));
        // resolve hash collisions exactly: keys are (qx, qy) pairs; store index of the first vertex with that pair
        int id;
        while (vIndex.TryGetValue(key, out id))
        {
            long ex, ey;
            GlassMath.PyRoundScaled(gv[id].x, 9, out ex);
            GlassMath.PyRoundScaled(gv[id].y, 9, out ey);
            if (ex == qx && ey == qy)
                return id;
            key = unchecked(key * 6364136223846793005L + 1442695040888963407L);
        }
        vIndex[key] = gv.Count;
        gv.Add(p);
        return gv.Count - 1;
    }

    void AddEnd(GlassV2 p)
    {
        var key = RoundKey(p, 6);
        int c;
        ends.TryGetValue(key, out c);
        ends[key] = c + 1;
    }

    int CountCrossings(double x0, double x1, double y0, double y1)
    {
        boxes.Clear();
        found.Clear();
        for (var idx = 0; idx < eu.Count; idx++)
        {
            GlassV2 p = gv[eu[idx]], q = gv[ev[idx]], cp, cq;
            if (!ClipSegment(p, q, x0, x1, y0, y1, out cp, out cq))
                continue;
            boxes.Add(new Box { x0 = Math.Min(cp.x, cq.x), x1 = Math.Max(cp.x, cq.x), y0 = Math.Min(cp.y, cq.y), y1 = Math.Max(cp.y, cq.y), idx = idx });
        }
        boxes.Sort(BoxOrder);
        var cnt = 0;
        for (var i = 0; i < boxes.Count; i++)
        {
            var A = boxes[i];
            int u1 = eu[A.idx], v1 = ev[A.idx];
            for (var j = i + 1; j < boxes.Count; j++)
            {
                var B = boxes[j];
                if (B.x0 > A.x1 + 1e-12)
                    break;
                if (B.y0 > A.y1 + 1e-12 || B.y1 < A.y0 - 1e-12)
                    continue;
                int u2 = eu[B.idx], v2 = ev[B.idx];
                if (u1 == u2 || u1 == v2 || v1 == u2 || v1 == v2)
                    continue;
                if (GlassPoly.SegmentsCross(gv[u1], gv[v1], gv[u2], gv[v2]))
                    cnt++;
            }
        }
        return cnt;
    }

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
                if (r > t1) return false;
                if (r > t0) t0 = r;
            }
            else
            {
                if (r < t0) return false;
                if (r < t1) t1 = r;
            }
        }
        a = new GlassV2(p.x + t0 * dx, p.y + t0 * dy);
        b = new GlassV2(p.x + t1 * dx, p.y + t1 * dy);
        return true;
    }

    double InClear(List<GlassV2> poly, GlassFrame fr)
    {
        if (poly.Count == 0)
            return 0.0;
        GlassPoly.ClipHalf(poly, 1.0, 0.0, fr.clearX1, s0);
        if (s0.Count < 3) return 0.0;
        GlassPoly.ClipHalf(s0, -1.0, 0.0, -fr.clearX0, s1);
        if (s1.Count < 3) return 0.0;
        GlassPoly.ClipHalf(s1, 0.0, 1.0, fr.clearY1, s0);
        if (s0.Count < 3) return 0.0;
        GlassPoly.ClipHalf(s0, 0.0, -1.0, -fr.clearY0, s2);
        if (s2.Count < 3) return 0.0;
        return GlassPoly.Area(s2);
    }

    static double MaxContact(List<GlassV2> poly, GlassFrame fr)
    {
        double cL = 0.0, cR = 0.0, cT = 0.0, cB = 0.0;
        var n = poly.Count;
        for (var i = 0; i < n; i++)
        {
            var p = poly[i];
            var q = poly[(i + 1) % n];
            var L = GlassMath.Hypot(q.x - p.x, q.y - p.y);
            if (Math.Abs(p.x + fr.hx) < 1e-9 && Math.Abs(q.x + fr.hx) < 1e-9) cL += L;
            if (Math.Abs(p.x - fr.hx) < 1e-9 && Math.Abs(q.x - fr.hx) < 1e-9) cR += L;
            if (Math.Abs(p.y - fr.hy) < 1e-9 && Math.Abs(q.y - fr.hy) < 1e-9) cT += L;
            if (Math.Abs(p.y + fr.hy) < 1e-9 && Math.Abs(q.y + fr.hy) < 1e-9) cB += L;
        }
        return Math.Max(Math.Max(cL, cR), Math.Max(cT, cB));
    }

    public string Summary()
    {
        var sb = new StringBuilder();
        sb.Append(AllOk ? "valid" : "INVALID");
        sb.Append(" area ").Append(areaRelErrorPct.ToString("0.0000000")).Append("%");
        sb.Append(" overlap ").Append(worstOverlapMm2.ToString("0.000")).Append("mm2");
        sb.Append(" nonconvex ").Append(nonConvexIds.Count);
        sb.Append(" min ").Append(minCm2.ToString("0.000")).Append("cm2");
        if (hasGraph) sb.Append(" crossings ").Append(crossings);
        sb.Append(" teeth ").Append(teeth).Append(" bad ").Append(teethBad.Count);
        if (hasBudget) sb.Append(" budget ").Append(budgetOk ? "ok" : "OVER").Append(" plus ").Append(plusJunctions).Append(" crater ").Append(craterMm.ToString("0.0")).Append("mm");
        if (hasStage1Check) sb.Append(" s1 ").Append(stage1WorstMm.ToString("0.000000")).Append("mm");
        if (!impactClampOk) sb.Append(" CLAMP");
        if (!areaFieldOk) sb.Append(" AREAFIELD");
        if (hasBudget && !repairsOk) sb.Append(" UNFIXED");
        return sb.ToString();
    }
}
