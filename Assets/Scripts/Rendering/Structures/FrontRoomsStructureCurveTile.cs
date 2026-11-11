// The structures scene's tile on curves, its reveals, the dome, the vault and the spiral's soffit (game visual line, structures
// G2-c, package structures_curve_tile; research/structures_assets/20_research.md A.4, 30_plan.md ask 2, W/structures_assets/
// g2_review_assets.md sections 4, 5.3 and 10).
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tile on curves as a setter lays it (Red's ask 2, decided 2026-11-07 21:52 "全部按推荐": A, the tile by radius). A tile is flat:
/// the setter picks its size by the tightest radius (FrontRoomsStructuresAssets.CurvePitch: 150 mm at R 1.2 m and over, 4 1/4 in
/// to 0.85, 75 mm to 0.6, 50 mm to 0.3, 25 mm below; ask 2 B keeps 150 mm) and every column (on a ring), course (on an arch or a
/// vault) or tile (on a dome) is its own flat facet with its joints on the facet's edges, so under a lamp the reflection steps
/// tile by tile instead of sliding round the curve as one band (A.4.1-A.4.3; POOLS' columns, SD:D28). The facet's UV cell sits
/// exactly on it, so FrontRooms/Surface's per-tile hash, set and dish (_FR_TILE) fall per facet.
/// Openings are cut on the true circle and their arrises take the tile's bullnose (the map's BevelTileRadius, the same round
/// K1's bevels give every box arris): a quarter round between the face and the reveal, in the reveal's tile.
/// Everything here is geometry only (triangle lists by tile kind, district-local metres); the staged contract K2 calls it from
/// FrontRoomsStructuresKit / PoolTerrain in NEW and appends the lists with the kind's material (FrontRoomsPoolTile). TODAY never
/// reaches it; nothing here makes a collider (the callers keep today's colliders, call for call).
/// </summary>
public static class FrontRoomsStructureCurveTile
{
    /// <summary>The bullnose round on every tile arris (3/8 in, the map's BevelTileRadius) and its segments.</summary>
    public static float RoundR { get { return FrontRoomsMapWorld.BevelTileRadius; } }
    static int RoundSegs { get { return Mathf.Clamp(FrontRoomsMapWorld.BevelTileSegments, 2, 6); } }

    /// <summary>One triangle list (district-local positions, normals, UVs in metres).</summary>
    public sealed class Lists
    {
        public readonly List<Vector3> v = new List<Vector3>();
        public readonly List<Vector3> n = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>();
        public readonly List<int> t = new List<int>();

        public int Add(Vector3 p, Vector3 nrm, Vector2 u)
        {
            v.Add(p);
            n.Add(nrm.sqrMagnitude > 1e-12f ? nrm.normalized : Vector3.up);
            uv.Add(u);
            return v.Count - 1;
        }

        /// <summary>A triangle facing 'outward' (turned if its winding disagrees; degenerate ones are dropped).</summary>
        public void Tri(int a, int b, int c, Vector3 outward)
        {
            var f = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
            if (f.sqrMagnitude < 1e-16f) return;
            if (Vector3.Dot(f, outward) < 0f) { var s = b; b = c; c = s; }
            t.Add(a); t.Add(b); t.Add(c);
        }

        public void Quad(int a, int b, int c, int d, Vector3 outward)
        {
            Tri(a, b, c, outward);
            Tri(a, c, d, outward);
        }

        public int Triangles { get { return t.Count / 3; } }
    }

    /// <summary>A build's lists by tile kind (FrontRoomsPoolTile kinds: FieldMesh, Field108Mesh, Field75Mesh, Field50Mesh,
    /// MosaicMesh, or the caller's own material key "" for pieces that keep the caller's material).</summary>
    public sealed class Out
    {
        public readonly Dictionary<string, Lists> Kinds = new Dictionary<string, Lists>();

        public Lists For(string kind)
        {
            Lists l;
            if (!Kinds.TryGetValue(kind ?? "", out l)) Kinds[kind ?? ""] = l = new Lists();
            return l;
        }

        public int Triangles
        {
            get
            {
                var n = 0;
                foreach (var kv in Kinds) n += kv.Value.Triangles;
                return n;
            }
        }
    }

    /// <summary>A hole in a ring (as FrontRoomsStructuresKit.RingHole): angles in degrees from +x toward +z, y0..y1 (round: a head
    /// of the hole's width on y1, measured on the outer face).</summary>
    public struct Hole
    {
        public float a0, a1, y0, y1;
        public bool round;

        public Hole(float a0, float a1, float y0, float y1, bool round)
        {
            this.a0 = Mathf.Min(a0, a1);
            this.a1 = Mathf.Max(a0, a1);
            this.y0 = y0;
            this.y1 = y1;
            this.round = round;
        }
    }

    /// <summary>The tile kind a curve of radius r takes (ask 2) and its pitch: a field kind by radius; 'own' (the caller's
    /// material, not the white field: the aqua course, a mosaic) keeps its own pitch and the key "".</summary>
    public static string KindFor(float radius, out float pitch)
    {
        pitch = FrontRoomsStructuresAssets.CurvePitch(radius);
        return FrontRoomsStructuresAssets.CurveTileKind(pitch);
    }

    static Vector3 Er(float deg)
    {
        var a = deg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
    }

    static Vector3 Et(float deg)
    {
        var a = deg * Mathf.Deg2Rad;
        return new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
    }

    // ---------------------------------------------------------------- a faceted face (one flat facet a tile column)

    /// <summary>One face of a ring: radius R, its columns (edges at aRef + k * step degrees), its facing (+1 out, -1 in).</summary>
    sealed class Face
    {
        public float R, step, aRef, pitch, side, u0;
        public string kind;

        // the facet point at angle th (deg), height y: on the chord of th's column
        public Vector3 Point(Vector3 c, float th, float y)
        {
            var k = Mathf.Floor((th - aRef) / step);
            var mid = aRef + (k + .5f) * step;
            var d = (th - mid) * Mathf.Deg2Rad;
            var rho = R * Mathf.Cos(step * .5f * Mathf.Deg2Rad) / Mathf.Max(.2f, Mathf.Cos(d));
            return c + Er(th) * rho + Vector3.up * y;
        }

        // a column edge exactly (the circle's point: both neighbours share it)
        public Vector3 EdgePoint(Vector3 c, int k, float y)
        {
            return c + Er(aRef + k * step) * R + Vector3.up * y;
        }

        public Vector3 Normal(float th)
        {
            var k = Mathf.Floor((th - aRef) / step);
            return Er(aRef + (k + .5f) * step) * side;
        }

        public float U(float th)
        {
            return u0 + (th - aRef) / step * pitch;
        }
    }

    static Face MakeFace(float R, float side, float aRef, string ownKind, float ownPitch, int seedCols)
    {
        var f = new Face { R = R, side = side, aRef = aRef };
        if (string.IsNullOrEmpty(ownKind))
        {
            float p;
            f.kind = KindFor(R, out p);
            f.pitch = p;
        }
        else
        {
            f.kind = ownKind;
            f.pitch = ownPitch;
        }
        // a whole number of columns round the circle; each facet's UV cell is exactly one pitch (its chord within 0.1 % of it)
        var n = Mathf.Max(6, Mathf.RoundToInt(2f * Mathf.PI * R / f.pitch));
        f.step = 360f / n;
        f.u0 = seedCols * f.pitch;
        return f;
    }

    // ---------------------------------------------------------------- hole outlines (the reveals' lines)

    // One point of a hole's outline: its angle (deg) and height, how its normal is found (0 jamb: nx -1 left, +1 right; 1 flat
    // head: up; 2 sill: down; 3 round head at phi), and the reveal's v there (y on a jamb, the course on a head, metres along a
    // flat head or sill).
    struct Sample
    {
        public float th, y, phi, nx, v;
        public int kind;
    }

    // A smooth stretch of an outline in one tile kind (a jamb, a round head, a flat head, a sill); corners start a new run.
    sealed class Run
    {
        public readonly List<Sample> s = new List<Sample>();
        public string kind;
    }

    // A hole as a face sees it: its dilated (by the round) angular span, bottom and top line, for the face's strips.
    sealed class HoleLine
    {
        public float thL, thR, bottom;
        public readonly List<Vector2> top = new List<Vector2>();   // (angle, height), angles rising
        public readonly List<float> breaks = new List<float>();

        public float TopAt(float th)
        {
            if (top.Count == 0) return 1e6f;
            if (th <= top[0].x) return top[0].y;
            for (var i = 1; i < top.Count; i++)
                if (th <= top[i].x)
                {
                    var a = top[i - 1];
                    var b = top[i];
                    var f = b.x - a.x > 1e-7f ? (th - a.x) / (b.x - a.x) : 1f;
                    return Mathf.Lerp(a.y, b.y, f);
                }
            return top[top.Count - 1].y;
        }
    }

    // The outline's unit normal at a sample in the face's own metres (x along rising angle, y up), pointing out of the hole into
    // the wall. A round head is an ellipse on a face of radius R (its span scales with R, its rise does not).
    static Vector2 N2(Sample s, float R, float rOut, float headR)
    {
        switch (s.kind)
        {
            case 0: return new Vector2(s.nx, 0f);
            case 1: return Vector2.up;
            case 2: return Vector2.down;
            default:
            {
                var a = Mathf.Max(1e-4f, headR * R / Mathf.Max(1e-4f, rOut));
                var n = new Vector2(Mathf.Cos(s.phi) / a, Mathf.Sin(s.phi) / Mathf.Max(1e-4f, headR));
                return n.sqrMagnitude > 1e-12f ? n.normalized : Vector2.up;
            }
        }
    }

    static float HeadR(Hole h, float rOut) { return (h.a1 - h.a0) * .5f * Mathf.Deg2Rad * rOut; }

    // The runs of a hole's outline inside a wall from y0 to y1 (clipped at its top; a hole from the floor has no sill).
    static List<Run> Outline(Hole h, float rOut, float y0, float y1, out float headR)
    {
        var runs = new List<Run>();
        headR = h.round ? HeadR(h, rOut) : 0f;
        var yB = Mathf.Max(h.y0, y0);
        var sill = h.y0 > y0 + 1e-3f;
        float pf;
        var flat = KindFor(10f, out pf);
        if (h.round)
        {
            var mid = (h.a0 + h.a1) * .5f;
            float ph;
            var headKind = KindFor(headR, out ph);
            var n = Mathf.Max(4, Mathf.RoundToInt(Mathf.PI * headR / ph));
            var springs = Mathf.Min(h.y1, y1);
            if (springs > yB + 1e-4f)
            {
                runs.Add(Jamb(h.a0, -1f, yB, springs, flat));
                runs.Add(Jamb(h.a1, 1f, springs, yB, flat));
            }
            if (h.y1 < y1 - 1e-4f)
            {
                var head = new Run { kind = headKind };
                for (var j = 0; j <= n; j++)
                {
                    var phi = Mathf.PI - Mathf.PI * j / n;
                    head.s.Add(new Sample { th = mid + headR * Mathf.Cos(phi) / rOut * Mathf.Rad2Deg, y = h.y1 + headR * Mathf.Sin(phi), phi = phi, kind = 3, v = j * ph });
                }
                runs.AddRange(ClipTop(head, y1));
            }
        }
        else
        {
            var yT = Mathf.Min(h.y1, y1);
            if (yT > yB + 1e-4f)
            {
                runs.Add(Jamb(h.a0, -1f, yB, yT, flat));
                runs.Add(Jamb(h.a1, 1f, yT, yB, flat));
            }
            var m = Mathf.Max(2, Mathf.CeilToInt((h.a1 - h.a0) / 2f) + 1);
            var rv = rOut * Mathf.Deg2Rad;
            if (h.y1 < y1 - 1e-4f)
            {
                var head = new Run { kind = flat };
                for (var j = 0; j < m; j++)
                {
                    var th = Mathf.Lerp(h.a0, h.a1, j / (m - 1f));
                    head.s.Add(new Sample { th = th, y = h.y1, kind = 1, v = th * rv });
                }
                runs.Add(head);
            }
            if (sill)
            {
                var s = new Run { kind = flat };
                for (var j = 0; j < m; j++)
                {
                    var th = Mathf.Lerp(h.a1, h.a0, j / (m - 1f));
                    s.s.Add(new Sample { th = th, y = h.y0, kind = 2, v = th * rv });
                }
                runs.Add(s);
            }
        }
        return runs;
    }

    static Run Jamb(float th, float nx, float ya, float yb, string kind)
    {
        var r = new Run { kind = kind };
        r.s.Add(new Sample { th = th, y = ya, kind = 0, nx = nx, v = ya });
        r.s.Add(new Sample { th = th, y = yb, kind = 0, nx = nx, v = yb });
        return r;
    }

    // A run cut where it rises over the wall's top (an arch higher than its wall: two runs ending on the top).
    static List<Run> ClipTop(Run r, float yTop)
    {
        var list = new List<Run>();
        Run cur = null;
        for (var i = 0; i < r.s.Count; i++)
        {
            var s = r.s[i];
            var inside = s.y <= yTop + 1e-5f;
            if (i > 0)
            {
                var p = r.s[i - 1];
                var pin = p.y <= yTop + 1e-5f;
                if (pin != inside)
                {
                    var f = (yTop - p.y) / (s.y - p.y);
                    var x = new Sample { th = Mathf.Lerp(p.th, s.th, f), y = yTop, phi = Mathf.Lerp(p.phi, s.phi, f), kind = s.kind, nx = s.nx, v = Mathf.Lerp(p.v, s.v, f) };
                    if (cur != null) { cur.s.Add(x); list.Add(cur); cur = null; }
                    else { cur = new Run { kind = r.kind }; cur.s.Add(x); }
                }
            }
            if (inside)
            {
                if (cur == null) cur = new Run { kind = r.kind };
                cur.s.Add(s);
            }
        }
        if (cur != null && cur.s.Count > 1) list.Add(cur);
        return list;
    }

    // The face's view of a hole: dilated by the round, its strips' breakpoints and its top line.
    static HoleLine LineFor(Hole h, Face f, float rOut, float y0, float y1, List<Run> runs, float headR)
    {
        var b = RoundR;
        var dth = b / Mathf.Max(.05f, f.R) * Mathf.Rad2Deg;
        var line = new HoleLine { thL = h.a0 - dth, thR = h.a1 + dth };
        line.bottom = h.y0 > y0 + 1e-3f ? h.y0 - b : -1e6f;
        if (h.round)
        {
            Run head = null;
            foreach (var r in runs) if (r.s.Count > 0 && r.s[0].kind == 3) { head = r; break; }
            if (h.y1 >= y1 - 1e-4f || head == null)
            {
                line.top.Add(new Vector2(line.thL, 1e6f));
                line.top.Add(new Vector2(line.thR, 1e6f));
            }
            else
            {
                foreach (var r in runs)
                {
                    if (r.s.Count == 0 || r.s[0].kind != 3) continue;
                    foreach (var s in r.s)
                    {
                        var n = N2(s, f.R, rOut, headR);
                        line.top.Add(new Vector2(s.th + b * n.x / f.R * Mathf.Rad2Deg, s.y + b * n.y));
                    }
                }
                // an arch cut by the wall's top: its crown region is open to the top
                if (line.top.Count > 0 && line.top[line.top.Count - 1].x < line.thR - 1e-4f && HasClippedCrown(runs)) InsertOpenCrown(line, runs, f, rOut, headR);
                line.top.Sort((p, q) => p.x.CompareTo(q.x));
            }
        }
        else
        {
            var top = h.y1 < y1 - 1e-4f ? h.y1 + b : 1e6f;
            line.top.Add(new Vector2(line.thL, top));
            line.top.Add(new Vector2(line.thR, top));
        }
        line.breaks.Add(line.thL);
        line.breaks.Add(line.thR);
        foreach (var p in line.top) line.breaks.Add(p.x);
        return line;
    }

    static bool HasClippedCrown(List<Run> runs)
    {
        var heads = 0;
        foreach (var r in runs) if (r.s.Count > 0 && r.s[0].kind == 3) heads++;
        return heads > 1;
    }

    // Two head runs (the crown over the wall's top): between them the hole is open to the top.
    static void InsertOpenCrown(HoleLine line, List<Run> runs, Face f, float rOut, float headR)
    {
        var ends = new List<float>();
        foreach (var r in runs)
            if (r.s.Count > 0 && r.s[0].kind == 3)
            {
                ends.Add(r.s[0].th);
                ends.Add(r.s[r.s.Count - 1].th);
            }
        ends.Sort();
        if (ends.Count < 4) return;
        // the gap between the first run's end and the second run's start
        line.top.Add(new Vector2(ends[1] + 1e-4f, 1e6f));
        line.top.Add(new Vector2(ends[2] - 1e-4f, 1e6f));
    }

    // ---------------------------------------------------------------- the ring (C1)

    /// <summary>
    /// A curved tiled wall (FrontRoomsStructuresKit.Ring in NEW): an annular prism round c, radii rIn..rOut (rIn 0: a solid round
    /// pier or drum), y0..y1, angles a0..a1 (degrees; 360 apart: whole), with holes. Each face one flat facet a tile column in the
    /// tile its radius takes; a full circle holds a whole number of columns, their joints symmetric about the first round hole's
    /// crown (else the first hole's middle); the holes cut on the true circle (a round head swept through the wall as a true
    /// intrados in courses), every reveal arris and, with topRound, the top's arrises in the tile's bullnose; solid caps as a fan
    /// with its normal (review D1: the drums' tops had none). 'own' / ownPitch: the caller's material keeps its own tile (key "",
    /// e.g. the aqua course) instead of the white field by radius. Render only.
    /// </summary>
    public static void Ring(Out o, Vector3 c, float rIn, float rOut, float y0, float y1, float a0, float a1, IList<Hole> holes, bool topRound, bool bottomCap,
        string own = null, float ownPitch = .15f)
    {
        if (a1 < a0) { var sw = a0; a0 = a1; a1 = sw; }
        if (y1 - y0 < 1e-4f || rOut < 1e-3f || a1 - a0 < 1e-3f) return;
        if (rIn < 1e-3f) rIn = 0f;
        var full = a1 - a0 >= 359.99f;
        var b = RoundR;
        // the joints' reference: the first round hole's crown, else the first hole's middle, else +x
        var aRef = 0f;
        var hs = new List<Hole>();
        if (holes != null)
        {
            foreach (var h in holes) hs.Add(h);
            var pick = -1;
            for (var i = 0; i < hs.Count && pick < 0; i++) if (hs[i].round) pick = i;
            if (pick < 0 && hs.Count > 0) pick = 0;
            if (pick >= 0) aRef = (hs[pick].a0 + hs[pick].a1) * .5f;
        }
        // a whole ring runs [aRef - 180, aRef + 180): each hole moved into it by whole turns (by its middle)
        var lo = a0;
        var hi = a1;
        if (full)
        {
            lo = aRef - 180f;
            hi = aRef + 180f;
            for (var i = 0; i < hs.Count; i++)
            {
                var h = hs[i];
                var m = (h.a0 + h.a1) * .5f;
                var k = Mathf.Floor((m - lo) / 360f);
                if (k != 0f) { h.a0 -= 360f * k; h.a1 -= 360f * k; hs[i] = h; }
            }
        }
        // columns offset per ring (by its place), so two rings of one radius do not repeat each other's tiles
        var seed = Mathf.FloorToInt(Mathf.Repeat(c.x * 7.31f + c.z * 3.17f + y0 * 1.7f, 97f));
        float ownP = own != null ? Mathf.Max(.01f, ownPitch) : 0f;
        var faceOut = MakeFace(rOut, 1f, aRef, own, ownP, seed);
        var faceIn = rIn > 0f ? MakeFace(rIn, -1f, aRef, own, ownP, seed + 11) : null;
        // the holes' outlines (rings without an inner face have none: a solid pier is never cut)
        var outlines = new List<List<Run>>();
        var headRs = new List<float>();
        foreach (var h in hs)
        {
            float hr;
            outlines.Add(rIn > 0f ? Outline(h, rOut, y0, y1, out hr) : new List<Run>());
            hr = h.round ? HeadR(h, rOut) : 0f;
            headRs.Add(hr);
        }
        var yTopFace = topRound ? y1 - b : y1;
        FaceStrips(o, c, faceOut, hs, outlines, headRs, rOut, y0, y1, yTopFace, lo, hi, full);
        if (faceIn != null) FaceStrips(o, c, faceIn, hs, outlines, headRs, rOut, y0, y1, yTopFace, lo, hi, full);
        if (faceIn != null)
            for (var i = 0; i < hs.Count; i++)
                foreach (var run in outlines[i]) Reveal(o, c, faceOut, faceIn, run, rOut, headRs[i], own);
        // the top and bottom: the angle ranges no hole opens
        var topRanges = Solid(hs, rOut, y1 - 1e-4f, lo, hi, true);
        Cap(o, c, faceOut, faceIn, topRanges, y1, topRound ? b : 0f, true, own, full && topRanges.Count == 1 && hs.Count == 0);
        if (topRound)
            foreach (var rg in topRanges)
            {
                TopRound(o, c, faceOut, rg.x, rg.y, y1, own);
                if (faceIn != null) TopRound(o, c, faceIn, rg.x, rg.y, y1, own);
            }
        if (bottomCap)
        {
            var botRanges = Solid(hs, rOut, y0 + 1e-4f, lo, hi, false);
            Cap(o, c, faceOut, faceIn, botRanges, y0, 0f, false, own, full && botRanges.Count == 1 && hs.Count == 0);
        }
        if (!full)
        {
            EndFace(o, c, faceOut, faceIn, rIn, a0, hs, rOut, y0, y1, -1f, own);
            EndFace(o, c, faceOut, faceIn, rIn, a1, hs, rOut, y0, y1, 1f, own);
        }
    }

    // A hole's top (undilated) at an angle, or -1e6 where it does not reach.
    static float HoleTop(Hole h, float rOut, float th)
    {
        if (th < h.a0 || th > h.a1) return -1e6f;
        if (!h.round) return h.y1;
        var r = HeadR(h, rOut);
        var d = (th - (h.a0 + h.a1) * .5f) * Mathf.Deg2Rad * rOut;
        return h.y1 + Mathf.Sqrt(Mathf.Max(0f, r * r - d * d));
    }

    // The angle ranges in [lo, hi] where no hole is open at height y (top: holes reaching y from below; else holes from y up).
    static List<Vector2> Solid(List<Hole> hs, float rOut, float y, float lo, float hi, bool top)
    {
        var open = new List<Vector2>();
        foreach (var h in hs)
        {
            if (top)
            {
                if (h.y0 > y) continue;
                // where its top is at or over y
                const int N = 64;
                var inRun = false;
                var start = 0f;
                for (var i = 0; i <= N; i++)
                {
                    var th = Mathf.Lerp(h.a0, h.a1, i / (float)N);
                    var on = HoleTop(h, rOut, th) >= y;
                    if (on && !inRun) { inRun = true; start = th; }
                    if ((!on || i == N) && inRun) { inRun = false; open.Add(new Vector2(start, th)); }
                }
            }
            else if (h.y0 <= y && HoleTop(h, rOut, (h.a0 + h.a1) * .5f) > y) open.Add(new Vector2(h.a0, h.a1));
        }
        open.Sort((p, q) => p.x.CompareTo(q.x));
        var list = new List<Vector2>();
        var at = lo;
        foreach (var g in open)
        {
            if (g.x > at + 1e-4f) list.Add(new Vector2(at, Mathf.Min(g.x, hi)));
            at = Mathf.Max(at, g.y);
        }
        if (hi > at + 1e-4f) list.Add(new Vector2(at, hi));
        return list;
    }

    // One face's strips: per column (clipped to the arc), split where a dilated hole's line breaks, the solid height intervals
    // (the hole's sloped top linear between breaks) as trapezoids on the column's facet.
    static void FaceStrips(Out o, Vector3 c, Face f, List<Hole> hs, List<List<Run>> outlines, List<float> headRs, float rOut, float y0, float y1, float yTopFace,
        float lo, float hi, bool full)
    {
        var l = o.For(f.kind);
        var lines = new List<HoleLine>();
        for (var i = 0; i < hs.Count; i++) lines.Add(LineFor(hs[i], f, rOut, y0, y1, outlines[i], headRs[i]));
        var k0 = Mathf.FloorToInt((lo - f.aRef) / f.step + 1e-4f);
        var k1 = Mathf.CeilToInt((hi - f.aRef) / f.step - 1e-4f);
        var bps = new List<float>();
        var ivs = new List<Vector4>();
        for (var k = k0; k < k1; k++)
        {
            var ta = Mathf.Max(lo, f.aRef + k * f.step);
            var tb = Mathf.Min(hi, f.aRef + (k + 1) * f.step);
            if (tb - ta < 1e-5f) continue;
            bps.Clear();
            bps.Add(ta);
            bps.Add(tb);
            foreach (var ln in lines)
                foreach (var x in ln.breaks)
                    if (x > ta + 1e-5f && x < tb - 1e-5f) bps.Add(x);
            bps.Sort();
            var nrm = f.Normal((ta + tb) * .5f);
            for (var q = 0; q + 1 < bps.Count; q++)
            {
                var ti = bps[q];
                var tj = bps[q + 1];
                if (tj - ti < 1e-5f) continue;
                var tm = (ti + tj) * .5f;
                // hole intervals here: x lo at ti, y lo at tj, z hi at ti, w hi at tj
                ivs.Clear();
                foreach (var ln in lines)
                {
                    if (tm <= ln.thL || tm >= ln.thR) continue;
                    ivs.Add(new Vector4(ln.bottom, ln.bottom, ln.TopAt(ti), ln.TopAt(tj)));
                }
                ivs.Sort((p, r) => (p.x + p.y).CompareTo(r.x + r.y));
                float ai = y0, aj = y0;
                foreach (var iv in ivs)
                {
                    Trap(l, c, f, ti, tj, ai, aj, Mathf.Min(iv.x, yTopFace), Mathf.Min(iv.y, yTopFace), nrm);
                    ai = Mathf.Max(ai, Mathf.Max(iv.z, y0));
                    aj = Mathf.Max(aj, Mathf.Max(iv.w, y0));
                }
                Trap(l, c, f, ti, tj, ai, aj, yTopFace, yTopFace, nrm);
            }
        }
    }

    // A trapezoid on a face's facet between angles ti, tj, from (ai, aj) up to (bi, bj).
    static void Trap(Lists l, Vector3 c, Face f, float ti, float tj, float ai, float aj, float bi, float bj, Vector3 nrm)
    {
        if (bi <= ai + 1e-5f && bj <= aj + 1e-5f) return;
        bi = Mathf.Max(bi, ai);
        bj = Mathf.Max(bj, aj);
        float ui = f.U(ti), uj = f.U(tj);
        var p0 = l.Add(f.Point(c, ti, ai), nrm, new Vector2(ui, ai));
        var p1 = l.Add(f.Point(c, tj, aj), nrm, new Vector2(uj, aj));
        var p2 = l.Add(f.Point(c, tj, bj), nrm, new Vector2(uj, bj));
        var p3 = l.Add(f.Point(c, ti, bi), nrm, new Vector2(ui, bi));
        l.Quad(p0, p1, p2, p3, nrm);
    }

    // A hole's reveal along one run of its outline: at each sample the section from the inner face to the outer face (the inner
    // face's bullnose round, the reveal, the outer face's round), joined sample to sample. The round meets each face exactly where
    // that face's strips stop (the outline dilated by the round in the face's own metres). The reveal's tile: the run's (a jamb or
    // a flat head in the 150 mm field, a round head in courses of the tile its radius takes); u across the reveal from the inner
    // face, v up a jamb, along a head's courses.
    static void Reveal(Out o, Vector3 c, Face fo, Face fi, Run run, float rOut, float headR, string own)
    {
        if (run.s.Count < 2) return;
        var l = o.For(own ?? run.kind);
        var b = RoundR;
        var S = RoundSegs;
        var w = 2 * S + 2;
        var pts = new Vector3[w];
        var nrs = new Vector3[w];
        var prev = new int[w];
        var cur = new int[w];
        for (var i = 0; i < run.s.Count; i++)
        {
            var s = run.s[i];
            var er = Er(s.th);
            var et = Et(s.th);
            var n2o = N2(s, fo.R, rOut, headR);
            var n2i = N2(s, fi.R, rOut, headR);
            var n3o = (et * n2o.x + Vector3.up * n2o.y).normalized;
            var n3i = (et * n2i.x + Vector3.up * n2i.y).normalized;
            var po = fo.Point(c, s.th, s.y);
            var pi = fi.Point(c, s.th, s.y);
            var eo = er;
            var ei = -er;
            var co = po + n3o * b - eo * b;
            var ci = pi + n3i * b - ei * b;
            for (var k = 0; k <= S; k++)
            {
                var tau = Mathf.PI * .5f * k / S;
                nrs[k] = Mathf.Cos(tau) * ei - Mathf.Sin(tau) * n3i;
                pts[k] = ci + b * nrs[k];
                var tauO = Mathf.PI * .5f * (S - k) / S;
                nrs[S + 1 + k] = Mathf.Cos(tauO) * eo - Mathf.Sin(tauO) * n3o;
                pts[S + 1 + k] = co + b * nrs[S + 1 + k];
            }
            var u = 0f;
            for (var k = 0; k < w; k++)
            {
                if (k > 0) u += Vector3.Distance(pts[k - 1], pts[k]);
                cur[k] = l.Add(pts[k], nrs[k], new Vector2(u, s.v));
            }
            if (i > 0)
                for (var k = 0; k + 1 < w; k++)
                {
                    var outward = l.n[prev[k]] + l.n[prev[k + 1]] + l.n[cur[k]] + l.n[cur[k + 1]];
                    l.Quad(prev[k], prev[k + 1], cur[k + 1], cur[k], outward);
                }
            var sw = prev; prev = cur; cur = sw;
        }
    }

    // The angles a cap's or a round's edge breaks at along a face over [a, b]: the ends and every column edge between.
    static List<float> EdgeAngles(Face f, float a, float b)
    {
        var list = new List<float> { a };
        var k0 = Mathf.CeilToInt((a - f.aRef) / f.step + 1e-4f);
        for (var k = k0; ; k++)
        {
            var th = f.aRef + k * f.step;
            if (th >= b - 1e-4f) break;
            list.Add(th);
        }
        list.Add(b);
        return list;
    }

    // A ring's top (up) or bottom over the solid ranges, inset by the round from both faces: a strip zipped between the inner and
    // outer edges (both on their faces' column edges), or a fan from the centre on a solid ring. Planar UVs in metres.
    static void Cap(Out o, Vector3 c, Face fo, Face fi, List<Vector2> ranges, float y, float inset, bool up, string own, bool wholeLoop)
    {
        // a ring's top is laid in its outer face's tile (a drum's in 50 mm, the tower's in the 150 mm field)
        var l = o.For(own ?? fo.kind);
        var nrm = up ? Vector3.up : Vector3.down;
        foreach (var rg in ranges)
        {
            var outer = EdgeAngles(fo, rg.x, rg.y);
            var oi = new List<int>();
            foreach (var th in outer)
            {
                var p = fo.Point(c, th, y) - Er(th) * inset;
                oi.Add(l.Add(p, nrm, new Vector2(p.x, p.z)));
            }
            if (fi == null)
            {
                var ctr = l.Add(c + Vector3.up * y, nrm, new Vector2(c.x, c.z));
                for (var k = 0; k + 1 < oi.Count; k++) l.Tri(ctr, oi[k], oi[k + 1], nrm);
                continue;
            }
            var inner = EdgeAngles(fi, rg.x, rg.y);
            var ii = new List<int>();
            foreach (var th in inner)
            {
                var p = fi.Point(c, th, y) + Er(th) * inset;
                ii.Add(l.Add(p, nrm, new Vector2(p.x, p.z)));
            }
            // zip the two chains by angle
            int a = 0, bI = 0;
            while (a < inner.Count - 1 || bI < outer.Count - 1)
            {
                var advanceInner = bI >= outer.Count - 1 || (a < inner.Count - 1 && inner[a + 1] <= outer[bI + 1]);
                if (advanceInner) { l.Tri(ii[a], oi[bI], ii[a + 1], nrm); a++; }
                else { l.Tri(ii[a], oi[bI], oi[bI + 1], nrm); bI++; }
            }
        }
    }

    // The bullnose along a face's top edge over [a, b] (a free-standing ring's top: a drum, the tower's cap ring).
    static void TopRound(Out o, Vector3 c, Face f, float a, float b, float y1, string own)
    {
        var l = o.For(own ?? f.kind);
        var r = RoundR;
        var S = RoundSegs;
        var ths = EdgeAngles(f, a, b);
        int[] prev = null;
        foreach (var th in ths)
        {
            var e = Er(th) * f.side;
            var p = f.Point(c, th, y1);
            var ctr = p - e * r - Vector3.up * r;
            var cur = new int[S + 1];
            for (var k = 0; k <= S; k++)
            {
                var tau = Mathf.PI * .5f * k / S;
                var n = Mathf.Cos(tau) * e + Mathf.Sin(tau) * Vector3.up;
                cur[k] = l.Add(ctr + n * r, n, new Vector2(f.U(th), y1 - r + r * tau));
            }
            if (prev != null)
                for (var k = 0; k < S; k++)
                    l.Quad(prev[k], prev[k + 1], cur[k + 1], cur[k], l.n[prev[k]] + l.n[cur[k + 1]]);
            prev = cur;
        }
    }

    // A partial ring's end at angle th (dir -1 at its first angle, +1 at its last): the radial face over the solid heights.
    static void EndFace(Out o, Vector3 c, Face fo, Face fi, float rIn, float th, List<Hole> hs, float rOut, float y0, float y1, float dir, string own)
    {
        float pf;
        var l = o.For(own ?? KindFor(10f, out pf));
        var cut = new List<Vector2> { new Vector2(y0, y1) };
        foreach (var h in hs)
        {
            var top = HoleTop(h, rOut, th);
            if (top < -1e5f) continue;
            var next = new List<Vector2>();
            foreach (var iv in cut)
            {
                if (top <= iv.x || h.y0 >= iv.y) { next.Add(iv); continue; }
                if (h.y0 > iv.x + 1e-4f) next.Add(new Vector2(iv.x, h.y0));
                if (top < iv.y - 1e-4f) next.Add(new Vector2(top, iv.y));
            }
            cut = next;
        }
        var nrm = Et(th) * dir;
        foreach (var iv in cut)
        {
            var pi0 = fi != null ? fi.Point(c, th, iv.x) : c + Vector3.up * iv.x;
            var pi1 = fi != null ? fi.Point(c, th, iv.y) : c + Vector3.up * iv.y;
            var po0 = fo.Point(c, th, iv.x);
            var po1 = fo.Point(c, th, iv.y);
            var d = Vector3.Distance(new Vector3(pi0.x, 0f, pi0.z), new Vector3(po0.x, 0f, po0.z));
            var a = l.Add(pi0, nrm, new Vector2(0f, iv.x));
            var b = l.Add(po0, nrm, new Vector2(d, iv.x));
            var cc = l.Add(po1, nrm, new Vector2(d, iv.y));
            var e = l.Add(pi1, nrm, new Vector2(0f, iv.y));
            l.Quad(a, b, cc, e, nrm);
        }
    }

    // ---------------------------------------------------------------- the round head in a straight wall (C2)

    /// <summary>
    /// A round opening's head in a straight wall (FrontRoomsStructuresKit.RoundHead in NEW; every arch of SP1, SP2 and SP6): the
    /// wall runs from a along t (plan), thick centred on its line; the opening from..to (metres along), springing at 'spring', the
    /// wall over it up to 'top'. The intrados as the period laid it: one flat facet a course of the tile its radius takes (150 mm at
    /// R 1.2, 4 1/4 in at 0.85, 75 mm at 0.6, 50 mm at 0.475), a bullnose round on both arrises continuing the jambs' (K1's rounds
    /// on the piers' boxes, the same radius); the spandrels on both faces in the caller's own material (key "": u along the line
    /// from uLine0, v = y, as K1's prisms) stop at the round. An arch higher than its wall is cut at the wall's top.
    /// </summary>
    public static void Arch(Out o, Vector3 a, Vector3 t, float thick, float from, float to, float spring, float top, float uLine0)
    {
        if (to - from < .02f || top <= spring + 1e-4f) return;
        a.y = 0f;
        t.y = 0f;
        t.Normalize();
        var s = new Vector3(t.z, 0f, -t.x);
        var hw = thick * .5f;
        var r = (to - from) * .5f;
        var mid = from + r;
        float ph;
        var kind = KindFor(r, out ph);
        var n = Mathf.Max(4, Mathf.RoundToInt(Mathf.PI * r / ph));
        var b = RoundR;
        var S = RoundSegs;
        var up = Vector3.up;
        // the head's samples at the course joints, left springing to right, and its runs under the wall's top
        var full = new Run { kind = kind };
        for (var j = 0; j <= n; j++)
        {
            var phi = Mathf.PI - Mathf.PI * j / n;
            full.s.Add(new Sample { th = mid + r * Mathf.Cos(phi), y = spring + r * Mathf.Sin(phi), phi = phi, kind = 3, v = j * ph });
        }
        var runs = ClipTop(full, top);
        // the spandrels: above the outline dilated by the round, inside the opening's span (the piers' boxes own the rest)
        var spand = o.For("");
        for (var side = -1; side <= 1; side += 2)
        {
            var e = s * side;
            var pts = new List<Vector2>();
            for (var j = 0; j <= n; j++)
            {
                var phi = Mathf.PI - Mathf.PI * j / n;
                pts.Add(new Vector2(mid + (r + b) * Mathf.Cos(phi), spring + (r + b) * Mathf.Sin(phi)));
            }
            var cut = new List<Vector2>();
            for (var j = 0; j < pts.Count; j++)
            {
                var p = pts[j];
                if (j > 0)
                {
                    var q = pts[j - 1];
                    foreach (var edge in new[] { from, to })
                        if ((q.x - edge) * (p.x - edge) < 0f)
                        {
                            var f = (edge - q.x) / (p.x - q.x);
                            cut.Add(new Vector2(edge, Mathf.Lerp(q.y, p.y, f)));
                        }
                }
                if (p.x >= from - 1e-5f && p.x <= to + 1e-5f) cut.Add(p);
            }
            for (var j = 0; j + 1 < cut.Count; j++)
            {
                var p0 = cut[j];
                var p1 = cut[j + 1];
                if (p1.x - p0.x < 1e-5f) continue;
                float y0 = Mathf.Min(p0.y, top), y1 = Mathf.Min(p1.y, top);
                if (y0 >= top - 1e-5f && y1 >= top - 1e-5f) continue;
                var nrm = e;
                var i0 = spand.Add(a + t * p0.x + e * hw + up * y0, nrm, new Vector2(uLine0 + p0.x, y0));
                var i1 = spand.Add(a + t * p1.x + e * hw + up * y1, nrm, new Vector2(uLine0 + p1.x, y1));
                var i2 = spand.Add(a + t * p1.x + e * hw + up * top, nrm, new Vector2(uLine0 + p1.x, top));
                var i3 = spand.Add(a + t * p0.x + e * hw + up * top, nrm, new Vector2(uLine0 + p0.x, top));
                spand.Quad(i0, i1, i2, i3, nrm);
            }
        }
        // the intrados and its two rounds: at each joint the section from one face to the other
        var l = o.For(kind);
        var w = 2 * S + 2;
        var ptsX = new Vector3[w];
        var nrs = new Vector3[w];
        foreach (var run in runs)
        {
            int[] prev = null;
            foreach (var sm in run.s)
            {
                var n2 = new Vector2(Mathf.Cos(sm.phi), Mathf.Sin(sm.phi));
                var n3 = (t * n2.x + up * n2.y).normalized;
                var basePt = a + t * sm.th + up * sm.y;
                var pm = basePt - s * hw;
                var pp = basePt + s * hw;
                var cm = pm + n3 * b + s * b;
                var cp = pp + n3 * b - s * b;
                for (var k = 0; k <= S; k++)
                {
                    var tau = Mathf.PI * .5f * k / S;
                    nrs[k] = Mathf.Cos(tau) * -s - Mathf.Sin(tau) * n3;
                    ptsX[k] = cm + b * nrs[k];
                    var tauO = Mathf.PI * .5f * (S - k) / S;
                    nrs[S + 1 + k] = Mathf.Cos(tauO) * s - Mathf.Sin(tauO) * n3;
                    ptsX[S + 1 + k] = cp + b * nrs[S + 1 + k];
                }
                var cur = new int[w];
                var u = 0f;
                for (var k = 0; k < w; k++)
                {
                    if (k > 0) u += Vector3.Distance(ptsX[k - 1], ptsX[k]);
                    cur[k] = l.Add(ptsX[k], nrs[k], new Vector2(u, sm.v));
                }
                if (prev != null)
                    for (var k = 0; k + 1 < w; k++)
                        l.Quad(prev[k], prev[k + 1], cur[k + 1], cur[k], l.n[prev[k]] + l.n[prev[k + 1]] + l.n[cur[k]] + l.n[cur[k + 1]]);
                prev = cur;
            }
        }
    }

    // ---------------------------------------------------------------- the saucer dome and its oculus (C4)

    /// <summary>
    /// A tiled saucer dome seen from below (PoolTerrain.BuildDome in NEW, dome light A and B alike; review D2, D4): a sphere of
    /// radius R round 'centre' (its crown straight over it), cut at plan radius rim; whole 150 mm tiles in rings (courses) from a
    /// crown roundel to the rim, every tile its own flat facet, each ring a whole number of tiles fitted to its circumference
    /// (the setter's closure spread over the ring's joints), joints staggered ring to ring (A.4.1). The oculus at plan point
    /// 'oculus' (radius oculusR) is cut true round, the tiles clipped to it; a bullnose round turns the shell up into the shaft,
    /// whose inside face rises in one facet a column of the tile its radius takes (75 mm at 0.6) from the shell to shaftTop
    /// (the shaft no longer pokes under the shell). Render only (the dome has no collider, as today).
    /// </summary>
    public static void Dome(Out o, Vector3 centre, float R, float rim, Vector2 oculus, float oculusR, float shaftTop)
    {
        float pf;
        var field = KindFor(10f, out pf);
        var l = o.For(field);
        var b = RoundR;
        var sRim = R * Mathf.Asin(Mathf.Clamp01(rim / R));
        var courses = Mathf.Max(3, Mathf.RoundToInt(sRim / pf));
        var hc = sRim / courses;
        var cut = oculusR + b;
        var oc3 = new Vector3(oculus.x, 0f, oculus.y);
        // the crown roundel (course 0): one cut round tile
        {
            var ring = 12;
            var rr = R * Mathf.Sin(hc / R);
            var yy = centre.y + R * Mathf.Cos(hc / R);
            var ap = l.Add(centre + Vector3.up * R, Vector3.down, new Vector2(.5f * pf, .5f * pf));
            var first = -1;
            var prev = -1;
            for (var i = 0; i <= ring; i++)
            {
                var th = 2f * Mathf.PI * i / ring;
                var p = new Vector3(centre.x + rr * Mathf.Cos(th), yy, centre.z + rr * Mathf.Sin(th));
                var id = i < ring ? l.Add(p, (centre - p).normalized, new Vector2(.5f * pf + .5f * pf * Mathf.Cos(th), .5f * pf + .5f * pf * Mathf.Sin(th))) : first;
                if (first < 0) first = id;
                if (prev >= 0) l.Tri(ap, prev, id, Vector3.down);
                prev = id;
            }
        }
        var poly = new List<Vector2>();
        for (var j = 1; j < courses; j++)
        {
            float s0 = j * hc, s1 = (j + 1) * hc;
            float r0 = R * Mathf.Sin(s0 / R), r1 = R * Mathf.Sin(s1 / R);
            float y0 = centre.y + R * Mathf.Cos(s0 / R), y1 = centre.y + R * Mathf.Cos(s1 / R);
            var rm = R * Mathf.Sin((s0 + s1) * .5f / R);
            var n = Mathf.Max(6, Mathf.RoundToInt(2f * Mathf.PI * rm / pf));
            var step = 2f * Mathf.PI / n;
            // stagger: half a tile on alternate rings plus a little drift, so no joint runs ring to ring
            var phase = (j % 2) * .5f * step + Mathf.Repeat(j * .618034f, 1f) * .2f * step;
            var uOff = ((j * 37) % 101) * pf;
            for (var i = 0; i < n; i++)
            {
                var t0 = phase + i * step;
                var t1 = t0 + step;
                var a = new Vector3(centre.x + r0 * Mathf.Cos(t0), y0, centre.z + r0 * Mathf.Sin(t0));
                var bb = new Vector3(centre.x + r0 * Mathf.Cos(t1), y0, centre.z + r0 * Mathf.Sin(t1));
                var cc = new Vector3(centre.x + r1 * Mathf.Cos(t1), y1, centre.z + r1 * Mathf.Sin(t1));
                var d = new Vector3(centre.x + r1 * Mathf.Cos(t0), y1, centre.z + r1 * Mathf.Sin(t0));
                var mid = (a + bb + cc + d) * .25f;
                var into = (centre - mid).normalized;
                var u0 = uOff + i * pf;
                var v0 = j * pf;
                var ua = new Vector2(u0, v0);
                var ub = new Vector2(u0 + pf, v0);
                var uc = new Vector2(u0 + pf, v0 + pf);
                var ud = new Vector2(u0, v0 + pf);
                // the oculus: tiles wholly inside are left out, tiles across its edge clipped to it
                int inside = 0;
                foreach (var q in new[] { a, bb, cc, d }) if (PlanDist(q, oc3) < cut) inside++;
                var near = PlanDist(mid, oc3) < cut + .3f;
                if (inside == 4) continue;
                if (inside == 0 && !(near && EdgeCrosses(a, bb, cc, d, oc3, cut)))
                {
                    var ia = l.Add(a, into, ua);
                    var ib = l.Add(bb, into, ub);
                    var ic = l.Add(cc, into, uc);
                    var id = l.Add(d, into, ud);
                    l.Quad(ia, ib, ic, id, into);
                    continue;
                }
                ClipTile(l, a, bb, cc, d, ua, ub, uc, ud, into, oc3, cut, poly);
            }
        }
        // the oculus: the bullnose round from the shell up into the shaft, the shaft's inside face to its top
        float ps;
        var shaftKind = KindFor(oculusR, out ps);
        var ls = o.For(shaftKind);
        var cols = Mathf.Max(8, Mathf.RoundToInt(2f * Mathf.PI * oculusR / ps));
        var S = RoundSegs;
        int[] prevRound = null;
        int prevBottom = -1, prevTop = -1;
        var shaftN = Vector3.zero;
        for (var k = 0; k <= cols; k++)
        {
            var th = 2f * Mathf.PI * k / cols;
            var e = new Vector3(Mathf.Cos(th), 0f, Mathf.Sin(th));
            var ps0 = oc3 + e * oculusR;
            var ys = SphereY(centre, R, ps0);
            var pSurf = new Vector3(ps0.x, ys, ps0.z);
            var nd = (pSurf - centre).normalized;         // into the shell's mass (away from the room)
            var ctr = pSurf + e * b + nd * b;
            var cur = new int[S + 1];
            var u = ps * k;
            for (var q = 0; q <= S; q++)
            {
                var tau = Mathf.PI * .5f * q / S;
                var dir = Mathf.Cos(tau) * nd + Mathf.Sin(tau) * e;
                cur[q] = ls.Add(ctr - dir * b, -dir, new Vector2(u, ys - b + b * tau));
            }
            if (prevRound != null)
                for (var q = 0; q < S; q++) ls.Quad(prevRound[q], prevRound[q + 1], cur[q + 1], cur[q], ls.n[prevRound[q]] + ls.n[cur[q + 1]]);
            prevRound = cur;
            // the shaft's inside face: from the round's top up to the lid, one facet a column (the column edges at these samples)
            var pBottom = ls.v[cur[S]];
            var bottom = ls.Add(pBottom, -e, new Vector2(u, pBottom.y));
            var top = ls.Add(new Vector3(pBottom.x, shaftTop, pBottom.z), -e, new Vector2(u, shaftTop));
            if (prevBottom >= 0)
            {
                var thm = 2f * Mathf.PI * (k - .5f) / cols;
                shaftN = -new Vector3(Mathf.Cos(thm), 0f, Mathf.Sin(thm));
                ls.n[prevBottom] = ls.n[prevTop] = ls.n[bottom] = ls.n[top] = shaftN;
                ls.Quad(prevBottom, bottom, top, prevTop, shaftN);
            }
            // the next column's quad needs its own two lower vertices with its normal (hard facets): duplicate them
            prevBottom = ls.Add(pBottom, -e, new Vector2(u, pBottom.y));
            prevTop = ls.Add(new Vector3(pBottom.x, shaftTop, pBottom.z), -e, new Vector2(u, shaftTop));
        }
    }

    static float PlanDist(Vector3 p, Vector3 c) { return Mathf.Sqrt((p.x - c.x) * (p.x - c.x) + (p.z - c.z) * (p.z - c.z)); }

    static float SphereY(Vector3 centre, float R, Vector3 p)
    {
        var dx = p.x - centre.x;
        var dz = p.z - centre.z;
        return centre.y + Mathf.Sqrt(Mathf.Max(0f, R * R - dx * dx - dz * dz));
    }

    // whether a circle (plan) crosses any edge of a quad whose corners all lie outside it
    static bool EdgeCrosses(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 oc, float r)
    {
        var q = new[] { a, b, c, d };
        for (var i = 0; i < 4; i++)
        {
            var p0 = new Vector2(q[i].x - oc.x, q[i].z - oc.z);
            var p1 = new Vector2(q[(i + 1) % 4].x - oc.x, q[(i + 1) % 4].z - oc.z);
            var dd = p1 - p0;
            var t = Mathf.Clamp01(-Vector2.Dot(p0, dd) / Mathf.Max(1e-12f, dd.sqrMagnitude));
            if ((p0 + dd * t).magnitude < r) return true;
        }
        return false;
    }

    // A dome tile cut by the oculus (plan circle oc, r): the part outside the circle, its edge along the circle, triangulated on the
    // tile's plane; UVs by the tile's own bilinear map.
    static void ClipTile(Lists l, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, Vector3 nrm, Vector3 oc, float r, List<Vector2> poly)
    {
        var q = new[] { a, b, c, d };
        poly.Clear();
        var flags = new List<int>();   // 0 corner, 1 entering the circle, 2 leaving it
        for (var i = 0; i < 4; i++)
        {
            var p0 = new Vector2(q[i].x, q[i].z);
            var p1 = new Vector2(q[(i + 1) % 4].x, q[(i + 1) % 4].z);
            var in0 = (p0 - new Vector2(oc.x, oc.z)).magnitude < r;
            if (!in0) { poly.Add(p0); flags.Add(0); }
            // crossings of p0 -> p1 with the circle, in order
            var f = p0 - new Vector2(oc.x, oc.z);
            var dd = p1 - p0;
            float A = Vector2.Dot(dd, dd), B = 2f * Vector2.Dot(f, dd), C = Vector2.Dot(f, f) - r * r;
            var disc = B * B - 4f * A * C;
            if (disc <= 0f || A < 1e-12f) continue;
            var sq = Mathf.Sqrt(disc);
            var t1 = (-B - sq) / (2f * A);
            var t2 = (-B + sq) / (2f * A);
            if (t1 > 0f && t1 < 1f) { poly.Add(p0 + dd * t1); flags.Add(1); }
            if (t2 > 0f && t2 < 1f) { poly.Add(p0 + dd * t2); flags.Add(2); }
        }
        if (poly.Count < 3) return;
        // along the circle from each entering crossing to the next leaving one (the arc inside the tile)
        var outPoly = new List<Vector2>();
        var cc = new Vector2(oc.x, oc.z);
        for (var i = 0; i < poly.Count; i++)
        {
            outPoly.Add(poly[i]);
            if (flags[i] != 1) continue;
            var j = (i + 1) % poly.Count;
            if (flags[j] != 2) continue;
            var a0 = Mathf.Atan2(poly[i].y - cc.y, poly[i].x - cc.x);
            var a1 = Mathf.Atan2(poly[j].y - cc.y, poly[j].x - cc.x);
            var da = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            const int Steps = 4;
            for (var k = 1; k < Steps; k++)
            {
                var ang = a0 + da * k / Steps;
                outPoly.Add(cc + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r);
            }
        }
        if (outPoly.Count < 3) return;
        // lift to the tile's plane, UVs by inverse bilinear on the plan quad
        var pn = Vector3.Cross(b - a, d - a).normalized;
        var ids = new int[outPoly.Count];
        for (var i = 0; i < outPoly.Count; i++)
        {
            var p2 = outPoly[i];
            var y = Mathf.Abs(pn.y) > 1e-5f ? a.y - (pn.x * (p2.x - a.x) + pn.z * (p2.y - a.z)) / pn.y : a.y;
            var st = InvBilinear(p2, new Vector2(a.x, a.z), new Vector2(b.x, b.z), new Vector2(c.x, c.z), new Vector2(d.x, d.z));
            var uv = Vector2.Lerp(Vector2.Lerp(ua, ub, st.x), Vector2.Lerp(ud, uc, st.x), st.y);
            ids[i] = l.Add(new Vector3(p2.x, y, p2.y), nrm, uv);
        }
        var order = FrontRoomsStructureProfiles.EarClip(outPoly.ToArray());
        for (var i = 0; i + 2 < order.Count; i += 3) l.Tri(ids[order[i]], ids[order[i + 1]], ids[order[i + 2]], nrm);
    }

    // ---------------------------------------------------------------- the spiral's ribbons (C3)

    /// <summary>
    /// A spiral's ribbon (FrontRoomsStructuresKit.Spiral's guard band) in NEW: a helical band between radii r0..r1 following the
    /// nosing line's (angle, height) samples, from 'below' under the line to 'above' over it; each face one flat facet a tile
    /// column of the tile its radius takes, its courses level (v = y), its raking top and bottom cut across them; the underside
    /// along the helix; the two ends. The band's top is left for the Cap course the kit sweeps on it. Render only.
    /// </summary>
    public static void HelixBand(Out o, Vector3 c, float r0, float r1, IList<Vector2> samples, float below, float above, string own = null, float ownPitch = .15f)
    {
        if (samples == null || samples.Count < 2 || r1 - r0 < 1e-3f) return;
        var pts = new List<Vector2>(samples);
        pts.Sort((p, q) => p.x.CompareTo(q.x));
        float lo = pts[0].x, hi = pts[pts.Count - 1].x;
        if (hi - lo < 1e-3f) return;
        var seed = Mathf.FloorToInt(Mathf.Repeat(c.x * 5.13f + c.z * 2.71f + r0 * 9.1f, 89f));
        var faces = new[] { MakeFace(r1, 1f, 0f, own, own != null ? ownPitch : 0f, seed), MakeFace(r0, -1f, 0f, own, own != null ? ownPitch : 0f, seed + 7) };
        foreach (var f in faces)
        {
            var l = o.For(f.kind);
            var k0 = Mathf.FloorToInt((lo - f.aRef) / f.step + 1e-4f);
            var k1 = Mathf.CeilToInt((hi - f.aRef) / f.step - 1e-4f);
            var bps = new List<float>();
            for (var k = k0; k < k1; k++)
            {
                var ta = Mathf.Max(lo, f.aRef + k * f.step);
                var tb = Mathf.Min(hi, f.aRef + (k + 1) * f.step);
                if (tb - ta < 1e-5f) continue;
                bps.Clear();
                bps.Add(ta);
                bps.Add(tb);
                foreach (var p in pts) if (p.x > ta + 1e-5f && p.x < tb - 1e-5f) bps.Add(p.x);
                bps.Sort();
                var nrm = f.Normal((ta + tb) * .5f);
                for (var q = 0; q + 1 < bps.Count; q++)
                {
                    float ti = bps[q], tj = bps[q + 1];
                    if (tj - ti < 1e-5f) continue;
                    float hi0 = HelixH(pts, ti), hj0 = HelixH(pts, tj);
                    Trap(l, c, f, ti, tj, hi0 - below, hj0 - below, hi0 + above, hj0 + above, nrm);
                }
            }
        }
        // the underside and the ends (the outer face's columns as the sampling)
        float pf;
        var lu = o.For(own ?? KindFor(10f, out pf));
        var fo = faces[0];
        var fi = faces[1];
        var ths = EdgeAngles(fo, lo, hi);
        foreach (var p in pts) if (p.x > lo + 1e-5f && p.x < hi - 1e-5f) ths.Add(p.x);
        ths.Sort();
        int pi0 = -1, po0 = -1;
        var acc = 0f;
        for (var i = 0; i < ths.Count; i++)
        {
            var th = ths[i];
            var y = HelixH(pts, th) - below;
            var a = fi.Point(c, th, y);
            var b = fo.Point(c, th, y);
            if (i > 0) acc += Mathf.Abs(th - ths[i - 1]) * Mathf.Deg2Rad * (r0 + r1) * .5f;
            var ia = lu.Add(a, Vector3.down, new Vector2(acc, 0f));
            var ib = lu.Add(b, Vector3.down, new Vector2(acc, r1 - r0));
            if (pi0 >= 0) lu.Quad(pi0, po0, ib, ia, Vector3.down);
            pi0 = ia;
            po0 = ib;
        }
        foreach (var end in new[] { lo, hi })
        {
            var h = HelixH(pts, end);
            var n = Et(end) * (end <= lo ? -1f : 1f);
            var a = lu.Add(fi.Point(c, end, h - below), n, new Vector2(0f, h - below));
            var b = lu.Add(fo.Point(c, end, h - below), n, new Vector2(r1 - r0, h - below));
            var cc = lu.Add(fo.Point(c, end, h + above), n, new Vector2(r1 - r0, h + above));
            var d = lu.Add(fi.Point(c, end, h + above), n, new Vector2(0f, h + above));
            lu.Quad(a, b, cc, d, n);
        }
    }

    /// <summary>The nosing line's height at an angle (the samples sorted by angle; clamped at the ends).</summary>
    public static float HelixH(IList<Vector2> sorted, float th)
    {
        if (th <= sorted[0].x) return sorted[0].y;
        for (var i = 1; i < sorted.Count; i++)
            if (th <= sorted[i].x)
            {
                var a = sorted[i - 1];
                var b = sorted[i];
                return b.x - a.x > 1e-6f ? Mathf.Lerp(a.y, b.y, (th - a.x) / (b.x - a.x)) : b.y;
            }
        return sorted[sorted.Count - 1].y;
    }

    // ---------------------------------------------------------------- the glass-block floor light (C10)

    /// <summary>
    /// A glass-block paver floor (review D8, SP2 F3's E7; 30_plan S2-08): solid pressed-glass pavers 'pitch' square (6 in) in an
    /// aluminium T-grid, their tops at 'top' with a 4 mm chamfer, each with its prismatic underside (the light below comes up
    /// through it: never a glowing floor, Red: walls never glow); the grid's ribs 8 mm wide 1 mm under the glass and 60 mm deep, an
    /// angle round the field. Key "" the glass, "grid" the aluminium. Render only (the floor's collider is the caller's).
    /// </summary>
    public static void GlassPavers(Out o, float x0, float z0, float x1, float z1, float top, float pitch)
    {
        var glass = o.For("");
        var grid = o.For("grid");
        const float Gap = .008f, Ch = .004f, Under = .028f, Depth = .06f;
        var nx = Mathf.Max(1, Mathf.RoundToInt((x1 - x0) / pitch));
        var nz = Mathf.Max(1, Mathf.RoundToInt((z1 - z0) / pitch));
        var px = (x1 - x0) / nx;
        var pz = (z1 - z0) / nz;
        var up = Vector3.up;
        for (var i = 0; i < nx; i++)
            for (var j = 0; j < nz; j++)
            {
                float ax = x0 + i * px + Gap * .5f, bx = x0 + (i + 1) * px - Gap * .5f;
                float az = z0 + j * pz + Gap * .5f, bz = z0 + (j + 1) * pz - Gap * .5f;
                var c = new Vector3((ax + bx) * .5f, top, (az + bz) * .5f);
                // the top: a centre square and four chamfers down 4 mm to the edge
                var o0 = new[] { new Vector3(ax, top - Ch, az), new Vector3(bx, top - Ch, az), new Vector3(bx, top - Ch, bz), new Vector3(ax, top - Ch, bz) };
                var i0 = new[] { new Vector3(ax + Ch, top, az + Ch), new Vector3(bx - Ch, top, az + Ch), new Vector3(bx - Ch, top, bz - Ch), new Vector3(ax + Ch, top, bz - Ch) };
                var ic = new int[4];
                for (var k = 0; k < 4; k++) ic[k] = glass.Add(i0[k], up, new Vector2(i0[k].x, i0[k].z));
                glass.Quad(ic[0], ic[1], ic[2], ic[3], up);
                for (var k = 0; k < 4; k++)
                {
                    var k1 = (k + 1) % 4;
                    var mid = (o0[k] + o0[k1]) * .5f;
                    var nrm = (new Vector3(mid.x - c.x, 0f, mid.z - c.z).normalized + up).normalized;
                    var a = glass.Add(o0[k], nrm, new Vector2(o0[k].x, o0[k].z));
                    var b = glass.Add(o0[k1], nrm, new Vector2(o0[k1].x, o0[k1].z));
                    var cc = glass.Add(i0[k1], nrm, new Vector2(i0[k1].x, i0[k1].z));
                    var d = glass.Add(i0[k], nrm, new Vector2(i0[k].x, i0[k].z));
                    glass.Quad(a, b, cc, d, nrm);
                }
                // the prismatic underside: a shallow pyramid from the edge (at top - depth of the block) to its point
                var yb = top - Depth + .01f;
                var apex = glass.Add(new Vector3(c.x, yb - Under, c.z), Vector3.down, new Vector2(c.x, c.z));
                for (var k = 0; k < 4; k++)
                {
                    var k1 = (k + 1) % 4;
                    var pa = new Vector3(o0[k].x, yb, o0[k].z);
                    var pb = new Vector3(o0[k1].x, yb, o0[k1].z);
                    var mid = (pa + pb) * .5f;
                    var nrm = (new Vector3(mid.x - c.x, 0f, mid.z - c.z).normalized - up * 1.5f).normalized;
                    var a = glass.Add(pa, nrm, new Vector2(pa.x, pa.z));
                    var b = glass.Add(pb, nrm, new Vector2(pb.x, pb.z));
                    glass.Tri(a, b, apex, nrm);
                }
            }
        // the T-grid: ribs on every paver line, the angle round the field
        for (var i = 0; i <= nx; i++) Rib(grid, new Vector3(x0 + i * px, 0f, z0), new Vector3(x0 + i * px, 0f, z1), top - .001f, Depth, i == 0 || i == nx ? .02f : Gap);
        for (var j = 0; j <= nz; j++) Rib(grid, new Vector3(x0, 0f, z0 + j * pz), new Vector3(x1, 0f, z0 + j * pz), top - .0012f, Depth, j == 0 || j == nz ? .02f : Gap);
    }

    // A rib of the grid: a bar 'w' wide from a to b (plan), its top at 'top', 'depth' deep (top, two faces, the bottom).
    static void Rib(Lists l, Vector3 a, Vector3 b, float top, float depth, float w)
    {
        var d = b - a;
        d.y = 0f;
        var len = d.magnitude;
        if (len < 1e-4f) return;
        var t = d / len;
        var s = new Vector3(t.z, 0f, -t.x) * (w * .5f);
        var up = Vector3.up;
        var y0 = top - depth;
        void EmitFace(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 n)
        {
            var i0 = l.Add(p0, n, new Vector2(Vector3.Dot(p0, t), p0.y));
            var i1 = l.Add(p1, n, new Vector2(Vector3.Dot(p1, t), p1.y));
            var i2 = l.Add(p2, n, new Vector2(Vector3.Dot(p2, t), p2.y));
            var i3 = l.Add(p3, n, new Vector2(Vector3.Dot(p3, t), p3.y));
            l.Quad(i0, i1, i2, i3, n);
        }
        EmitFace(a - s + up * top, b - s + up * top, b + s + up * top, a + s + up * top, up);
        EmitFace(a + s + up * y0, b + s + up * y0, b + s + up * top, a + s + up * top, s.normalized);
        EmitFace(a - s + up * y0, b - s + up * y0, b - s + up * top, a - s + up * top, -s.normalized);
        EmitFace(a - s + up * y0, b - s + up * y0, b + s + up * y0, a + s + up * y0, Vector3.down);
    }

    // ---------------------------------------------------------------- pool edges and tile caps (C6, C8)

    /// <summary>The pool edge a basin's rim is built as (Red's ask 1, decided: SP6 A rim-flow, SP1 and SP2 F0 B roll-out; KIT all
    /// three): RollOut, RimFlow, Recessed, or Tank (a balance tank's open edge 0.6 m over its water: the bullnose coping).</summary>
    public enum Edge { RollOut = 0, RimFlow = 1, Recessed = 2, Tank = 3 }

    /// <summary>
    /// An edge's section in the plumb frame of a path that runs with the water on its left (x 0 on the basin's inside face line,
    /// +x away from the water over the deck, y 0 the deck), and the tile it is drawn in. RollOut is P1's GutterSection (the
    /// flush finger grate 0.20 behind a 50 mm tiled curb with its r 15 bullnose; V as Prop_PoolTile_Gutter's unrolled map) from the
    /// grate's back edge; RimFlow the same edge without the curb (the grate flush with the deck at the water: TM 5-662's deck-level
    /// edge, A.2.3); Recessed P1's recessed channel under its coping; Tank the family's bullnose coping (Prop_PoolTile_Coping).
    /// The pool face runs down to 'drop' (metres under the deck) 1.5 mm proud of the basin's wall (no z-fight), the deck legs
    /// 1.5 mm over the deck.
    /// </summary>
    public static FrontRoomsStructureProfiles.Section EdgeSection(Edge kind, float drop, out string tileKind)
    {
        const float Lift = .0015f, Proud = .0015f;
        const float R = .015f;
        var p = new List<Vector2>();
        var v = new List<float>();
        drop = Mathf.Max(drop, .12f);
        switch (kind)
        {
            case Edge.Tank:
            {
                // 300 mm units: 0.30 over the deck to an r 15 full bullnose nose, its 30 mm face down to the tank's wall
                tileKind = "Coping";
                p.Add(new Vector2(.30f, Lift)); v.Add(0f);
                p.Add(new Vector2(R, Lift)); v.Add(.285f);
                for (var i = 1; i <= 4; i++)
                {
                    var a = Mathf.PI * .5f * i / 4f;
                    p.Add(new Vector2(R - Mathf.Sin(a) * R - Proud, Lift - R + Mathf.Cos(a) * R)); v.Add(.285f + R * a);
                }
                p.Add(new Vector2(-Proud, -.03f)); v.Add(.285f + R * Mathf.PI * .5f + .015f);
                p.Add(new Vector2(-Proud, -drop)); v.Add(.285f + R * Mathf.PI * .5f + .015f + (drop - .03f));
                break;
            }
            case Edge.Recessed:
            {
                tileKind = "Gutter";
                var s = FrontRoomsStructureProfiles.GutterSection(FrontRoomsStructureProfiles.Gutter.Recessed);
                for (var i = 0; i < s.P.Length; i++) { p.Add(new Vector2(s.P[i].x - .02f - Proud, s.P[i].y + Lift)); v.Add(s.V[i]); }
                break;
            }
            default:
            {
                // walked from the deck to the water (the edge's body on the left): the grate, the curb (RollOut) or a flush lip
                tileKind = "Gutter";
                var curb = kind == Edge.RollOut ? .05f : 0f;
                const float Face = -.034f;
                p.Add(new Vector2(.25f, Lift)); v.Add(.20f);
                p.Add(new Vector2(.05f, Lift)); v.Add(.40f);
                if (curb > 0f) { p.Add(new Vector2(.05f, curb)); v.Add(.45f); }
                else { p.Add(new Vector2(.0495f, Lift)); v.Add(.45f); }
                p.Add(new Vector2(-.019f, curb + Lift * (curb > 0f ? 0f : 1f))); v.Add(.519f);
                var top = curb > 0f ? curb : Lift;
                for (var i = 1; i <= 3; i++)
                {
                    var a = Mathf.PI * .5f * i / 3f;
                    p.Add(new Vector2(-.019f - Mathf.Sin(a) * R, top - R + Mathf.Cos(a) * R)); v.Add(.519f + R * a);
                }
                p.Add(new Vector2(Face, top - R - .01f)); v.Add(.519f + R * Mathf.PI * .5f + .01f);
                p.Add(new Vector2(Face, -drop)); v.Add(.519f + R * Mathf.PI * .5f + .01f + (drop + top - R - .01f));
                break;
            }
        }
        // the face line: x 0 is the basin's inside face; the section's water face sits 1.5 mm proud of it (toward -x)
        if (kind == Edge.RollOut || kind == Edge.RimFlow)
            for (var i = 0; i < p.Count; i++) p[i] = new Vector2(p[i].x + .034f - Proud, p[i].y);
        var hard = new bool[p.Count];
        hard[0] = hard[p.Count - 1] = true;
        for (var i = 1; i < p.Count - 1; i++)
        {
            // a crease wherever the section turns more than 30 degrees (the round's points stay smooth)
            var d0 = (p[i] - p[i - 1]).normalized;
            var d1 = (p[i + 1] - p[i]).normalized;
            hard[i] = Vector2.Dot(d0, d1) < .86f || (p[i] - p[i - 1]).sqrMagnitude < 1e-8f;
        }
        return new FrontRoomsStructureProfiles.Section("pool edge " + kind, p.ToArray(), hard, false, v.ToArray());
    }

    /// <summary>A tiled guard's or parapet's Cap course (A.2.4, A.3.3; 30_plan S2-01, S6-02): a double-bullnose cap tile across the
    /// wall's top, 'thick' high, the wall's width plus 3 mm a side, its arrises in the tile's round; centred on x, y 0 at its foot.
    /// Closed: its ends close as the cap's returned ends.</summary>
    public static FrontRoomsStructureProfiles.Section CapSection(float width, float thick)
    {
        var w = width + .006f;
        var r = Mathf.Min(RoundR, thick * .45f);
        var S = RoundSegs;
        var list = new List<Vector2>();
        // counter-clockwise from the bottom middle: bottom right, the right round, the top, the left round, bottom left
        list.Add(new Vector2(0f, 0f));
        list.Add(new Vector2(w * .5f, 0f));
        for (var i = 0; i <= S; i++)
        {
            var a = Mathf.PI * .5f * i / S;
            list.Add(new Vector2(w * .5f - r + Mathf.Cos(a) * r, thick - r + Mathf.Sin(a) * r));
        }
        for (var i = 0; i <= S; i++)
        {
            var a = Mathf.PI * .5f + Mathf.PI * .5f * i / S;
            list.Add(new Vector2(-w * .5f + r + Mathf.Cos(a) * r, thick - r + Mathf.Sin(a) * r));
        }
        list.Add(new Vector2(-w * .5f, 0f));
        var hard = new bool[list.Count];
        hard[0] = hard[1] = hard[list.Count - 1] = true;
        // V: the joint at the hidden bottom; the top and its rounds one tile across
        var v = new float[list.Count];
        var tot = 0f;
        for (var i = 1; i < list.Count; i++) tot += Vector2.Distance(list[i - 1], list[i]);
        tot += Vector2.Distance(list[list.Count - 1], list[0]);
        var acc = 0f;
        for (var i = 0; i < list.Count; i++)
        {
            if (i > 0) acc += Vector2.Distance(list[i - 1], list[i]);
            v[i] = acc / tot * .15f;
        }
        return new FrontRoomsStructureProfiles.Section("tile cap", list.ToArray(), hard, true, v);
    }

    // (s, t) of a point in a quad a, b, c, d (bilinear: a + s (b - a) + t (d - a) + s t (a - b + c - d)), by a few Newton steps.
    static Vector2 InvBilinear(Vector2 p, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        var st = new Vector2(.5f, .5f);
        for (var it = 0; it < 8; it++)
        {
            float s = st.x, t = st.y;
            var e = a + s * (b - a) + t * (d - a) + s * t * (a - b + c - d) - p;
            var ds = (b - a) + t * (a - b + c - d);
            var dt = (d - a) + s * (a - b + c - d);
            var det = ds.x * dt.y - ds.y * dt.x;
            if (Mathf.Abs(det) < 1e-12f) break;
            st -= new Vector2((e.x * dt.y - e.y * dt.x) / det, (ds.x * e.y - ds.y * e.x) / det);
        }
        return new Vector2(Mathf.Clamp01(st.x), Mathf.Clamp01(st.y));
    }
}
