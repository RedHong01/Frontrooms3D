// FrontRoomsGlassFracturePattern.cs -- GD3 step 3 (plan 10 sec 2.6, 4.3): the one source of truth for the fracture
// pattern of a struck window pane, in C#. Pure C# (no UnityEngine), deterministic, the same code on desktop and WebGL.
//
//   var f = FrontRoomsGlassFracturePattern.Generate(profile, seed, impactUV, slabSize, rotationDeg, side);
//   var job = FrontRoomsGlassFracturePattern.Begin(...);  while (!job.Step()) { /* one phase per frame */ }
//
// The output equals Tools/Blender/glass/crack_graph_ref.py (version 1.3) -- the golden test checks it byte for byte.
// Inputs follow the map's GlassBreakRecord (FrontRoomsMapWorld.cs): impact (root metres) or impactUV (over the EXPOSED
// glass 1.367 x 1.617 from x -0.6835, y 0.3665), rotation in degrees (default MapHash.Unit((uint)seed) * 360), side
// (+1 = struck from cell a, so the glass flies toward +Z; it never changes the pattern, only far_z_sign).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static class FrontRoomsGlassFracturePattern
{
    // Window geometry (window-root metres): 06w sec 4.2-4.4, plan 10 sec 2.2
    public const double SlabW = 1.391, SlabH = 1.642, SlabT = 0.006;
    public const double SlabCentreY = 1.175;                                  // slab centre height above the floor (root y)
    public const double StopX = 0.6835, StopTop = 1.9835, StopBot = 0.3665;  // exposed-glass edge (stop line), root frame
    public const double ClearX = 0.600, ClearTop = 1.900, ClearBot = 0.390;  // nothing may remain inside this after the break
    public const double ImpactMinFromStop = 0.2;                             // FrontRoomsShotTimings.GlassBreak.ImpactMinFromFrame

    // The 06w sec 4.4 bake centres (plan sec 4.1: 3 x 3 impact centres x 2 seeds = the 18-variant fallback per profile)
    public static readonly double[] BakeX = { -0.40, 0.0, 0.40 };
    public static readonly double[] BakeY = { 0.90, 1.30, 1.65 };

    /// <summary>The bake centre nearest an impact in window-root metres (index = row * 3 + column, rows by BakeY).</summary>
    public static int NearestBakeCentre(double rootX, double rootY)
    {
        var best = 0;
        var bestD = double.PositiveInfinity;
        for (var j = 0; j < BakeY.Length; j++)
            for (var i = 0; i < BakeX.Length; i++)
            {
                var dx = rootX - BakeX[i];
                var dy = rootY - BakeY[j];
                var d = dx * dx + dy * dy;
                if (d < bestD)
                {
                    bestD = d;
                    best = j * BakeX.Length + i;
                }
            }
        return best;
    }

    [ThreadStatic] static FrontRoomsGlassFractureJob cachedJob;

    /// <summary>The job this thread reuses for Generate() (its pools persist, so repeated breaks allocate little).</summary>
    public static FrontRoomsGlassFractureJob SharedJob => cachedJob ?? (cachedJob = new FrontRoomsGlassFractureJob());

    // ---------------------------------------------------------------------------------------------------------
    // Entry points
    // ---------------------------------------------------------------------------------------------------------

    /// <summary>Plan sec 2.6: Generate(profile, seed, impactUV, slabSize, rotation). Runs every slice now.</summary>
    public static FrontRoomsGlassFracture Generate(FrontRoomsGlassProfileData profile, uint seed, GlassV2 impactUV,
        GlassV2 slabSize, double rotationDeg = double.NaN, int side = 1, string bandMode = "band", FrontRoomsGlassFracture result = null)
    {
        var job = SharedJob;
        BeginUV(job, profile, seed, impactUV, slabSize, rotationDeg, side, bandMode, result);
        return Finish(job);
    }

    /// <summary>The same with the impact in window-root metres (GlassBreakRecord.impact; wins over impactUV in the reference).</summary>
    public static FrontRoomsGlassFracture GenerateAtRoot(FrontRoomsGlassProfileData profile, uint seed, double impactRootX, double impactRootY,
        double rotationDeg = double.NaN, int side = 1, string bandMode = "band", FrontRoomsGlassFracture result = null,
        double slabW = SlabW, double slabH = SlabH)
    {
        var job = SharedJob;
        BeginRoot(job, profile, seed, impactRootX, impactRootY, slabW, slabH, rotationDeg, side, bandMode, result);
        return Finish(job);
    }

    static FrontRoomsGlassFracture Finish(FrontRoomsGlassFractureJob job)
    {
        while (!job.Step())
        {
        }
        if (job.Failed)
            throw new InvalidOperationException("glass fracture: " + job.Error);
        return job.Result;
    }

    /// <summary>Starts a time-sliced break on job (impact over the exposed glass, record.impactUV).</summary>
    public static void BeginUV(FrontRoomsGlassFractureJob job, FrontRoomsGlassProfileData profile, uint seed, GlassV2 impactUV,
        GlassV2 slabSize, double rotationDeg, int side, string bandMode, FrontRoomsGlassFracture result)
    {
        var fr = FrameForSlab(slabSize.x, slabSize.y, bandMode ?? "band");
        var ix = fr.stopX0 + impactUV.x * (fr.stopX1 - fr.stopX0);
        var iy = fr.stopY0 + impactUV.y * (fr.stopY1 - fr.stopY0);
        job.BeginSlab(profile, seed, ix, iy, slabSize.x, slabSize.y, rotationDeg, side, bandMode, result);
    }

    /// <summary>Starts a time-sliced break on job (impact in window-root metres, record.impact).</summary>
    public static void BeginRoot(FrontRoomsGlassFractureJob job, FrontRoomsGlassProfileData profile, uint seed, double impactRootX,
        double impactRootY, double slabW, double slabH, double rotationDeg, int side, string bandMode, FrontRoomsGlassFracture result)
    {
        job.BeginSlab(profile, seed, impactRootX, impactRootY - SlabCentreY, slabW, slabH, rotationDeg, side, bandMode, result);
    }

    // ---------------------------------------------------------------------------------------------------------
    // Frame, clamp, rotation (crack_graph_ref.py frame_for_slab, _clamp_impact, map_rotation_deg, _exposed_uv)
    // ---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Stop line, clear zone and tooth band in SLAB-LOCAL coordinates for a slab of w x h. Depths are measured inward
    /// from the glass edge (which sits in the hidden pocket behind the stop). bandMode "band" = the tooth band (the
    /// map chat approved it, 00_map_contract.md item 7); "stop" = the fallback: teeth end at the stop line.
    /// </summary>
    public static GlassFrame FrameForSlab(double w, double h, string bandMode)
    {
        var f = new GlassFrame();
        var hx = w / 2.0;
        var hy = h / 2.0;
        var exactW = Math.Abs(w - SlabW) < 1e-9;
        var exactH = Math.Abs(h - SlabH) < 1e-9;
        f.hx = hx;
        f.hy = hy;
        f.pocketL = exactW ? hx - StopX : 0.012;
        f.pocketR = exactW ? hx - StopX : 0.012;
        f.pocketT = exactH ? (SlabCentreY + SlabH / 2) - StopTop : 0.0125;
        f.pocketB = exactH ? StopBot - (SlabCentreY - SlabH / 2) : 0.0125;
        f.bandL = exactW ? hx - ClearX : 0.0955;
        f.bandR = exactW ? hx - ClearX : 0.0955;
        f.bandT = exactH ? (SlabCentreY + SlabH / 2) - ClearTop : 0.096;
        f.bandB = exactH ? ClearBot - (SlabCentreY - SlabH / 2) : 0.036;
        if (bandMode == "stop")
        {
            f.bandL = f.pocketL;
            f.bandR = f.pocketR;
            f.bandT = f.pocketT;
            f.bandB = f.pocketB;
        }
        else if (bandMode != "band")
            throw new ArgumentException("bandMode must be 'band' or 'stop'");
        f.bandMode = bandMode;
        f.stopX0 = -hx + f.pocketL;
        f.stopX1 = hx - f.pocketR;
        f.stopY0 = -hy + f.pocketB;
        f.stopY1 = hy - f.pocketT;
        f.clearX0 = -hx + f.bandL;
        f.clearX1 = hx - f.bandR;
        f.clearY0 = -hy + f.bandB;
        f.clearY1 = hy - f.bandT;
        return f;
    }

    /// <summary>The impact is clamped 0.2 m inside the exposed glass (the map does the same).</summary>
    public static void ClampImpact(ref double x, ref double y, GlassFrame fr)
    {
        var m = ImpactMinFromStop;
        x = Math.Min(Math.Max(x, fr.stopX0 + m), fr.stopX1 - m);
        y = Math.Min(Math.Max(y, fr.stopY0 + m), fr.stopY1 - m);
    }

    /// <summary>
    /// The map's record rotation for a seed: MapHash.Unit((uint)seed) * 360f (FrontRoomsMapWorld.GlassBreakOf), with
    /// float32 rounding. Unit(h) = (h &gt;&gt; 8) / 2^24 is exact in float32; the product is rounded to float32.
    /// </summary>
    public static double MapRotationDeg(uint seed)
    {
        var u = (seed >> 8) * (1.0 / 16777216.0);
        return (double)(float)(u * 360.0);
    }

    /// <summary>The impactUV of the record for a slab-local impact (over the exposed glass).</summary>
    public static GlassV2 ExposedUV(double ix, double iy, GlassFrame fr) =>
        new GlassV2((ix - fr.stopX0) / (fr.stopX1 - fr.stopX0), (iy - fr.stopY0) / (fr.stopY1 - fr.stopY0));

    /// <summary>
    /// Stage-2 pose of one slab point (pose_point): rotate by deg about the in-plane axis through pivot, in the sense
    /// that moves the piece's IMPACT side toward the FAR side, then push toward the far side. farSign = +1 when the far
    /// side is +z (struck from cell a). The reference transform for the meshes, the look-dev and the ray tracer.
    /// </summary>
    public static void PosePoint(double x, double y, double z, GlassV2 pivot, GlassV2 axis, double deg, double push, int farSign,
        out double ox, out double oy, out double oz)
    {
        var th = GlassMath.Radians(deg);
        var ax = axis.x;
        var ay = axis.y;
        var vx = x - pivot.x;
        var vy = y - pivot.y;
        var vf = z * farSign;
        var c = GlassMath.Cos(th);
        var s = GlassMath.Sin(th);
        var kdotv = ax * vx + ay * vy;
        // k x v with k = (ax, ay, 0): (ay*vf, -ax*vf, ax*vy - ay*vx)
        var cx = ay * vf;
        var cy = -ax * vf;
        var cf = ax * vy - ay * vx;
        var rx = vx * c + cx * s + ax * kdotv * (1.0 - c);
        var ry = vy * c + cy * s + ay * kdotv * (1.0 - c);
        var rf = vf * c + cf * s;
        ox = pivot.x + rx;
        oy = pivot.y + ry;
        oz = (rf + push) * farSign;
    }

    // ---------------------------------------------------------------------------------------------------------
    // Output rounding (_round_tree: every float to 1e-7, -0.0 -> 0.0)
    // ---------------------------------------------------------------------------------------------------------
    public static double R7(double x)
    {
        var r = GlassMath.PyRound(x, 7);
        return r == 0.0 ? 0.0 : r;
    }

    static GlassV2 R7(GlassV2 p) => new GlassV2(R7(p.x), R7(p.y));

    static void R7(List<GlassV2> l)
    {
        for (var i = 0; i < l.Count; i++)
            l[i] = R7(l[i]);
    }

    /// <summary>Rounds every double of a result like the reference's _round_tree (consumers read the rounded values).</summary>
    public static void RoundResult(FrontRoomsGlassFracture r)
    {
        RoundHeader(r);
        RoundPoints(r.graphVerts, 0, int.MaxValue);
        RoundPieces(r, 0, int.MaxValue);
        RoundPoints(r.verts, 0, int.MaxValue);
        RoundPoints(r.subVerts, 0, int.MaxValue);
    }

    /// <summary>Rounds everything but the big point lists and the piece records.</summary>
    public static void RoundHeader(FrontRoomsGlassFracture r)
    {
        r.rotationDeg = R7(r.rotationDeg);
        r.impact = R7(r.impact);
        r.impactRoot = R7(r.impactRoot);
        r.impactUV = R7(r.impactUV);
        r.slabW = R7(r.slabW);
        r.slabH = R7(r.slabH);
        r.slabT = R7(r.slabT);
        r.bevel = R7(r.bevel);
        r.centreRootY = R7(r.centreRootY);
        var f = r.frame;
        f.pocketL = R7(f.pocketL); f.pocketR = R7(f.pocketR); f.pocketT = R7(f.pocketT); f.pocketB = R7(f.pocketB);
        f.bandL = R7(f.bandL); f.bandR = R7(f.bandR); f.bandT = R7(f.bandT); f.bandB = R7(f.bandB);
        f.stopX0 = R7(f.stopX0); f.stopX1 = R7(f.stopX1); f.stopY0 = R7(f.stopY0); f.stopY1 = R7(f.stopY1);
        f.clearX0 = R7(f.clearX0); f.clearX1 = R7(f.clearX1); f.clearY0 = R7(f.clearY0); f.clearY1 = R7(f.clearY1);
        // hx, hy are not exported (the consumers recompute them from the slab)
        r.frame = f;
        for (var i = 0; i < r.rings.Count; i++)
            r.rings[i] = R7(r.rings[i]);
        for (var i = 0; i < r.chords.Count; i++)
        {
            var c = r.chords[i];
            c.p0 = R7(c.p0);
            c.p1 = R7(c.p1);
            r.chords[i] = c;
        }
        for (var i = 0; i < r.forks.Count; i++)
        {
            var c = r.forks[i];
            c.at = R7(c.at);
            r.forks[i] = c;
        }
        for (var i = 0; i < r.cracks.Count; i++)
        {
            var c = r.cracks[i];
            c.reach = R7(c.reach);
            c.reachFrac = R7(c.reachFrac);
            r.cracks[i] = c;
        }
        R7(r.crackVerts);
        R7(r.craterPoly);
        r.craterRadius = R7(r.craterRadius);
        R7(r.chips1);
        R7(r.chips2);
        r.wireGrid = R7(r.wireGrid);
        r.granule = R7(r.granule);
        for (var i = 0; i < r.repairs.Count; i++)
        {
            var q = r.repairs[i];
            q.value = R7(q.value);
            r.repairs[i] = q;
        }
        var s = r.stats;
        s.shardAreaCm2P10 = R7(s.shardAreaCm2P10);
        s.shardAreaCm2P50 = R7(s.shardAreaCm2P50);
        s.shardAreaCm2P90 = R7(s.shardAreaCm2P90);
        s.maxPieceLen = R7(s.maxPieceLen);
        s.avgVerts = R7(s.avgVerts);
        r.stats = s;
    }

    /// <summary>Rounds points [from, from + count) of l; returns the next index.</summary>
    public static int RoundPoints(List<GlassV2> l, int from, int count)
    {
        var end = (int)Math.Min((long)l.Count, (long)from + count);
        for (var i = from; i < end; i++)
            l[i] = R7(l[i]);
        return end;
    }

    /// <summary>Rounds piece records [from, from + count) (their scalars; the polygons are in r.verts); returns the next index.</summary>
    public static int RoundPieces(FrontRoomsGlassFracture r, int from, int count)
    {
        var end = (int)Math.Min((long)r.pieces.Count, (long)from + count);
        for (var i = from; i < end; i++)
        {
            var p = r.pieces[i];
            p.area = R7(p.area);
            p.centroid = R7(p.centroid);
            p.r = R7(p.r);
            p.posePivot = R7(p.posePivot);
            p.poseAxis = R7(p.poseAxis);
            p.poseDeg = R7(p.poseDeg);
            p.posePush = R7(p.posePush);
            p.releaseMs = R7(p.releaseMs);
            p.releaseSpeed = R7(p.releaseSpeed);
            r.pieces[i] = p;
        }
        return end;
    }

    /// <summary>Python's "%.{nd}f" % x (correctly rounded, ties to even on the exact binary value).</summary>
    public static string FormatFixed(double x, int nd)
    {
        long q;
        if (!GlassMath.PyRoundScaled(x, nd, out q))
            return x.ToString("F" + nd, CultureInfo.InvariantCulture);
        var neg = q < 0 || (q == 0 && x < 0.0);
        var a = (ulong)Math.Abs(q);
        var digits = a.ToString(CultureInfo.InvariantCulture).PadLeft(nd + 1, '0');
        var sb = new StringBuilder();
        if (neg)
            sb.Append('-');
        sb.Append(digits, 0, digits.Length - nd);
        if (nd > 0)
        {
            sb.Append('.');
            sb.Append(digits, digits.Length - nd, nd);
        }
        return sb.ToString();
    }

    // ---------------------------------------------------------------------------------------------------------
    // JSON export: the reference's to_json (sort_keys, separators (",", ":"), float repr). Tools and tests only.
    // ---------------------------------------------------------------------------------------------------------

    /// <summary>Python repr() of a double that is a 1e-7-rounded value (every output float); exact otherwise too where possible.</summary>
    public static string PyFloatRepr(double x)
    {
        if (double.IsNaN(x)) return "NaN";
        if (double.IsInfinity(x)) return x > 0 ? "Infinity" : "-Infinity";
        if (x == 0.0) return (1.0 / x) < 0 ? "-0.0" : "0.0";
        // the shortest decimal with <= 15 fraction digits that round-trips (true for every rounded output value)
        for (var nd = 0; nd <= 15; nd++)
        {
            long q;
            if (!GlassMath.PyRoundScaled(x, nd, out q))
                break;
            if (Math.Abs(q) >= 9007199254740992L)
                break;
            if (q / Pow10(nd) != x)
                continue;
            return DecimalRepr(q, nd);
        }
        return x.ToString("R", CultureInfo.InvariantCulture);
    }

    static double Pow10(int n)
    {
        var p = 1.0;
        for (var i = 0; i < n; i++)
            p *= 10.0;
        return p;
    }

    // q * 10^-nd in Python repr style: fixed for 1e-4 <= |x| < 1e16, else d.ddde-XX
    static string DecimalRepr(long q, int nd)
    {
        var neg = q < 0;
        var digits = ((ulong)Math.Abs(q)).ToString(CultureInfo.InvariantCulture);
        // strip trailing zeros of the fraction (q = digits * 10^-nd)
        var tz = 0;
        while (tz < nd && digits.Length - tz > 1 && digits[digits.Length - 1 - tz] == '0')
            tz++;
        digits = digits.Substring(0, digits.Length - tz);
        nd -= tz;
        // decimal exponent of the first significant digit
        var exp10 = digits.Length - 1 - nd;
        var sb = new StringBuilder();
        if (neg)
            sb.Append('-');
        if (exp10 < -4 || exp10 >= 16)
        {
            var sig = digits.TrimEnd('0');
            if (sig.Length == 0)
                sig = "0";
            sb.Append(sig[0]);
            if (sig.Length > 1)
            {
                sb.Append('.');
                sb.Append(sig, 1, sig.Length - 1);
            }
            sb.Append('e');
            sb.Append(exp10 < 0 ? '-' : '+');
            var e = Math.Abs(exp10);
            if (e < 10)
                sb.Append('0');
            sb.Append(e.ToString(CultureInfo.InvariantCulture));
            return sb.ToString();
        }
        if (nd <= 0)
        {
            sb.Append(digits);
            for (var i = 0; i < -nd; i++)
                sb.Append('0');
            sb.Append(".0");
            return sb.ToString();
        }
        if (digits.Length <= nd)
        {
            sb.Append("0.");
            for (var i = 0; i < nd - digits.Length; i++)
                sb.Append('0');
            sb.Append(digits);
            return sb.ToString();
        }
        sb.Append(digits, 0, digits.Length - nd);
        sb.Append('.');
        sb.Append(digits, digits.Length - nd, nd);
        return sb.ToString();
    }

    /// <summary>
    /// The reference's JSON for a result (crack_graph_ref.py to_json: sorted keys, no spaces). For a result built with
    /// RoundOutput (the default) the bytes equal the Python reference's for the same inputs.
    /// </summary>
    public static string ToJson(FrontRoomsGlassFracture r)
    {
        var sb = new StringBuilder(128 * 1024);
        WriteValue(sb, ToTree(r));
        return sb.ToString();
    }

    sealed class JObj : SortedDictionary<string, object>
    {
        public JObj() : base(StringComparer.Ordinal)
        {
        }
    }

    static List<object> Pt(GlassV2 p) => new List<object> { p.x, p.y };

    static List<object> Pts(List<GlassV2> l, int v0, int vn)
    {
        var o = new List<object>(vn);
        for (var i = 0; i < vn; i++)
            o.Add(Pt(l[v0 + i]));
        return o;
    }

    static JObj Sides(double l, double r, double t, double b)
    {
        var o = new JObj();
        o["L"] = l;
        o["R"] = r;
        o["T"] = t;
        o["B"] = b;
        return o;
    }

    static JObj Rect(double x0, double x1, double y0, double y1)
    {
        var o = new JObj();
        o["x0"] = x0;
        o["x1"] = x1;
        o["y0"] = y0;
        o["y1"] = y1;
        return o;
    }

    static object OwnerJson(GlassOwner o)
    {
        if (o.kind == GlassOwner.Chord)
            return new List<object> { (long)o.a, (long)o.b, (long)o.c };
        return (long)o.a;
    }

    static string CornerName(GlassCorner c)
    {
        switch (c)
        {
            case GlassCorner.TL: return "TL";
            case GlassCorner.TR: return "TR";
            case GlassCorner.BL: return "BL";
            case GlassCorner.BR: return "BR";
            default: return null;
        }
    }

    static string KindName(GlassPieceKind k)
    {
        switch (k)
        {
            case GlassPieceKind.Crush: return "crush";
            case GlassPieceKind.Shard: return "shard";
            case GlassPieceKind.Tooth: return "tooth";
            default: return "clump";
        }
    }

    /// <summary>The result as a tree of JObj / List / double / long / bool / string (the reference's dict layout).</summary>
    public static object ToTree(FrontRoomsGlassFracture r)
    {
        var o = new JObj();
        o["version"] = FrontRoomsGlassFracture.Version;
        o["profile"] = r.profile;
        o["glass"] = r.glass;
        o["tier"] = r.tier;
        o["seed"] = (long)r.seed;
        o["rotation_deg"] = r.rotationDeg;
        o["side"] = (long)r.side;
        o["far_z_sign"] = (long)r.farZSign;
        o["impact"] = Pt(r.impact);
        o["impact_root"] = Pt(r.impactRoot);
        o["impact_uv"] = Pt(r.impactUV);
        var slab = new JObj();
        slab["w"] = r.slabW;
        slab["h"] = r.slabH;
        slab["t"] = r.slabT;
        slab["bevel"] = r.bevel;
        slab["centre_root_y"] = r.centreRootY;
        o["slab"] = slab;
        var f = r.frame;
        var frame = new JObj();
        frame["pocket"] = Sides(f.pocketL, f.pocketR, f.pocketT, f.pocketB);
        frame["band"] = Sides(f.bandL, f.bandR, f.bandT, f.bandB);
        frame["band_mode"] = f.bandMode;
        frame["stop"] = Rect(f.stopX0, f.stopX1, f.stopY0, f.stopY1);
        frame["clear"] = Rect(f.clearX0, f.clearX1, f.clearY0, f.clearY1);
        o["frame"] = frame;
        var pieces = new List<object>(r.pieces.Count);
        for (var i = 0; i < r.pieces.Count; i++)
        {
            var p = r.pieces[i];
            var po = new JObj();
            po["id"] = (long)p.id;
            po["kind"] = KindName(p.kind);
            po["poly"] = Pts(r.verts, p.v0, p.vn);
            po["area"] = p.area;
            po["centroid"] = Pt(p.centroid);
            po["r"] = p.r;
            if (r.isDice)
            {
                po["granules"] = (long)p.granules;
                var rel = new JObj();
                rel["wave"] = (long)p.releaseWave;
                rel["ms"] = p.releaseMs;
                rel["speed"] = p.releaseSpeed;
                po["release"] = rel;
                pieces.Add(po);
                continue;
            }
            po["band"] = (long)p.band;
            po["sector"] = (long)p.sector;
            po["cluster"] = (long)p.cluster;
            if (p.kind == GlassPieceKind.Tooth)
            {
                po["side"] = p.side.ToString();
                po["edge"] = p.EdgeName;
                if (p.corner != GlassCorner.None)
                    po["corner"] = CornerName(p.corner);
            }
            var pose = new JObj();
            pose["pivot"] = Pt(p.posePivot);
            pose["axis"] = Pt(p.poseAxis);
            pose["deg"] = p.poseDeg;
            pose["push"] = p.posePush;
            po["pose"] = pose;
            if (p.hasRelease)
            {
                var rel = new JObj();
                rel["wave"] = (long)p.releaseWave;
                rel["ms"] = p.releaseMs;
                rel["speed"] = p.releaseSpeed;
                po["release"] = rel;
            }
            var subs = new List<object>(p.sn);
            for (var k = 0; k < p.sn; k++)
            {
                var sr = r.subPolys[p.s0 + k];
                subs.Add(Pts(r.subVerts, sr.v0, sr.vn));
            }
            po["sub"] = subs;
            pieces.Add(po);
        }
        o["pieces"] = pieces;
        var st = r.stats;
        if (r.isDice)
        {
            o["mode"] = "dice";
            o["granule"] = r.granule;
            var s1d = new JObj();
            s1d["cracks"] = new List<object>();
            var crd = new JObj();
            crd["poly"] = new List<object>();
            crd["radius"] = 0.0;
            s1d["crater"] = crd;
            s1d["chips"] = new List<object>();
            o["stage1"] = s1d;
            var s2d = new JObj();
            s2d["chips"] = new List<object>();
            s2d["removed"] = new List<object>();
            o["stage2"] = s2d;
            var sd = new JObj();
            var pk = new JObj();
            pk["clump"] = (long)st.clump;
            sd["pieces"] = pk;
            sd["pieces_total"] = (long)st.piecesTotal;
            sd["granules_estimate"] = st.granulesEstimate;
            o["stats"] = sd;
            return o;
        }
        var rings = new List<object>(r.rings.Count);
        for (var i = 0; i < r.rings.Count; i++)
            rings.Add(r.rings[i]);
        o["rings"] = rings;
        var graph = new JObj();
        var tracks = new List<object>(r.tracks.Count);
        for (var i = 0; i < r.tracks.Count; i++)
        {
            var t = r.tracks[i];
            var to = new JObj();
            to["id"] = (long)t.id;
            to["parent"] = (long)t.parent;
            to["initial"] = t.initial;
            to["points"] = Pts(r.graphVerts, t.v0, t.vn);
            tracks.Add(to);
        }
        graph["tracks"] = tracks;
        var chords = new List<object>(r.chords.Count);
        for (var i = 0; i < r.chords.Count; i++)
        {
            var c = r.chords[i];
            var co = new JObj();
            co["ring"] = (long)c.ring;
            co["a"] = (long)c.a;
            co["b"] = (long)c.b;
            co["p"] = new List<object> { Pt(c.p0), Pt(c.p1) };
            chords.Add(co);
        }
        graph["chords"] = chords;
        var forks = new List<object>(r.forks.Count);
        for (var i = 0; i < r.forks.Count; i++)
        {
            var c = r.forks[i];
            var fo = new JObj();
            fo["ring"] = (long)c.ring;
            fo["parent"] = (long)c.parent;
            fo["child"] = (long)c.child;
            fo["at"] = Pt(c.at);
            forks.Add(fo);
        }
        graph["forks"] = forks;
        o["graph"] = graph;
        var s1 = new JObj();
        var cracks = new List<object>(r.cracks.Count);
        for (var i = 0; i < r.cracks.Count; i++)
        {
            var c = r.cracks[i];
            var co = new JObj();
            co["track"] = (long)c.track;
            co["reach"] = c.reach;
            co["reach_frac"] = c.reachFrac;
            co["points"] = Pts(r.crackVerts, c.v0, c.vn);
            cracks.Add(co);
        }
        s1["cracks"] = cracks;
        var crater = new JObj();
        crater["poly"] = Pts(r.craterPoly, 0, r.craterPoly.Count);
        crater["radius"] = r.craterRadius;
        s1["crater"] = crater;
        s1["chips"] = Pts(r.chips1, 0, r.chips1.Count);
        o["stage1"] = s1;
        var s2 = new JObj();
        s2["chips"] = Pts(r.chips2, 0, r.chips2.Count);
        s2["removed"] = new List<object> { "crush" };
        o["stage2"] = s2;
        var wire = new JObj();
        wire["grid"] = r.wireGrid;
        wire["hangs"] = r.wireHangs;
        o["wire"] = wire;
        var reps = new List<object>(r.repairs.Count);
        for (var i = 0; i < r.repairs.Count; i++)
        {
            var q = r.repairs[i];
            object val;
            switch (q.kind)
            {
                case GlassRepairKind.BuildErrorDampKinks: val = q.text; break;
                case GlassRepairKind.NonconvexFaceDampKinks: val = null; break;
                case GlassRepairKind.CrossingDampKinks: val = new List<object> { OwnerJson(q.o1), OwnerJson(q.o2) }; break;
                case GlassRepairKind.SmallMergeNeighbour:
                case GlassRepairKind.SmallUnfixed: val = q.value; break;
                case GlassRepairKind.CapUnmet: val = (long)q.count; break;
                default: val = OwnerJson(q.o1); break;
            }
            reps.Add(new List<object> { GlassRepair.Name(q.kind), val });
        }
        o["repairs"] = reps;
        o["attempts"] = (long)r.attempts;
        var so = new JObj();
        var kinds = new JObj();
        kinds["shard"] = (long)st.shard;
        kinds["tooth"] = (long)st.tooth;
        kinds["crush"] = (long)st.crush;
        so["pieces"] = kinds;
        so["pieces_total"] = (long)st.piecesTotal;
        so["bodies"] = (long)st.bodies;
        so["tracks_initial"] = (long)st.tracksInitial;
        so["tracks_total"] = (long)st.tracksTotal;
        so["forks"] = (long)st.forks;
        so["chords"] = (long)st.chords;
        so["rings"] = (long)st.rings;
        so["t_junctions"] = (long)st.tJunctions;
        so["plus_junctions"] = (long)st.plusJunctions;
        so["kinks"] = (long)st.kinks;
        so["kinks_zeroed"] = (long)st.kinksZeroed;
        so["shard_area_cm2_p10_p50_p90"] = new List<object> { st.shardAreaCm2P10, st.shardAreaCm2P50, st.shardAreaCm2P90 };
        so["max_piece_len"] = st.maxPieceLen;
        so["avg_verts"] = st.avgVerts;
        so["tris_stage2"] = (long)st.trisStage2;
        so["tris_teeth"] = (long)st.trisTeeth;
        so["tris_fins"] = (long)st.trisFins;
        so["tris_crater"] = (long)st.trisCrater;
        so["level2_cells"] = (long)st.level2Cells;
        so["stage1_cracks"] = (long)st.stage1Cracks;
        o["stats"] = so;
        return o;
    }

    static void WriteValue(StringBuilder sb, object v)
    {
        if (v == null)
        {
            sb.Append("null");
            return;
        }
        var d = v as IDictionary<string, object>;
        if (d != null)
        {
            sb.Append('{');
            var first = true;
            foreach (var kv in d)
            {
                if (!first)
                    sb.Append(',');
                first = false;
                WriteString(sb, kv.Key);
                sb.Append(':');
                WriteValue(sb, kv.Value);
            }
            sb.Append('}');
            return;
        }
        var l = v as List<object>;
        if (l != null)
        {
            sb.Append('[');
            for (var i = 0; i < l.Count; i++)
            {
                if (i > 0)
                    sb.Append(',');
                WriteValue(sb, l[i]);
            }
            sb.Append(']');
            return;
        }
        if (v is double)
        {
            sb.Append(PyFloatRepr((double)v));
            return;
        }
        if (v is long)
        {
            sb.Append(((long)v).ToString(CultureInfo.InvariantCulture));
            return;
        }
        if (v is bool)
        {
            sb.Append((bool)v ? "true" : "false");
            return;
        }
        WriteString(sb, v.ToString());
    }

    static void WriteString(StringBuilder sb, string s)
    {
        sb.Append('"');
        foreach (var ch in s)
        {
            switch (ch)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (ch < 0x20 || ch > 0x7E)
                        sb.Append("\\u").Append(((int)ch).ToString("x4"));
                    else
                        sb.Append(ch);
                    break;
            }
        }
        sb.Append('"');
    }
}
