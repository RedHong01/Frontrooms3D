// The structures scene's slide: the fibreglass flume as built (game visual line, structures G2-b, package structures_flume;
// research/structures_assets/20_research.md A.7, 30_plan.md P12, W/structures_assets/g2_review_assets.md section 3).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The SP1 flume in NEW (the staged contract K2 calls it from FrontRoomsStructuresSlide.BuildFlume; TODAY never reaches it).
/// Red's slide stays as it is: the riding line, the ride, the mixed styles and colours (SL-A milky aqua tube, SL-B ivory open
/// trough with a teal band, SL-C ivory trough under a dark cap; level_space 61 section 2.5) and his tile walls carrying it
/// ("我认为应该是让瓷砖作为墙壁去托举滑梯", Red 10-31 17:15). What changes is how it is made, as the period made it (LS70-LS79;
/// US 4,149,710):
///   - the shell has its 8 mm of laminate behind the gel coat (both skins, closed ends), the trough its rolled 70 mm lip
///     flanges, the cap its edge flanges bolted to them; rings only where the line turns, mitred on each joint's bisector
///     (the riding line's own frames), LOD0 / LOD1 / LOD2 = about 60 / 30 / 16 points round the trough;
///   - every joint is a bolted flange pair: two 50 mm ribs 60 mm proud, a grey sealant line between them, 3/8 in stainless
///     hex bolts at 150 mm (heads and washers on the upstream face, nuts, washers and thread on the downstream one);
///   - the start has its flange on the tub's lip; the end a rolled exit lip turned down over the pool's edge;
///   - the riding lane (the polished strip the riders and the water wear along the trough's bottom) and the teal band are
///     v-ranges of the shell, in their own materials;
///   - the bearing pads (10 mm black elastomer) the owners' tiled cradles carry (Cradle).
/// Surfaces (Resources/Surfaces, FrontRooms/Surface on mesh UVs in metres, the pool doors' FRP gel coat maps): Prop_Gelcoat_Ivory,
/// _Track, _TealBand, _SLC_Cap, _SLA_Milky (_FR_TRANSLUCENT), _SLA_Flange; Prop_StainlessSatin (bolts), Prop_LaminateGrey (the grey
/// polyurethane sealant line), Prop_Rubber (the bearing pads).
/// Render only: no collider (the ride is a path and the owners' LowCollider keeps people out of the low runs, as today).
/// </summary>
public static class FrontRoomsStructureFlume
{
    public const int SLA = 0, SLB = 1, SLC = 2;

    /// <summary>One piece of the riding line: its samples i0..i1 on the line (a joint sample belongs to both neighbours), its
    /// style, whether it is a bend bay, its zone (clusters never cross a zone: hall, crossing room, service void) and whether a
    /// player can come within a couple of metres of it (near: the low runs over the deck, the slot by the start room): only
    /// there do its flanges carry the hero bolts (washers, nuts, thread) at LOD0; elsewhere LOD0 is the LOD1 build.</summary>
    public struct Piece
    {
        public int i0, i1, style, zone;
        public bool arc, near;
    }

    /// <summary>What a build made (for the district's cost line and the sheets).</summary>
    public sealed class Result
    {
        public int lod0, lod1, lod2, joints, bolts, clusters, renderers;
        public readonly List<Renderer> shell = new List<Renderer>();
        public override string ToString()
        {
            return "flume NEW: " + clusters + " shell clusters, " + joints + " flange pairs, " + bolts + " bolts; triangles LOD0 " + lod0
                + " / LOD1 " + lod1 + " / LOD2 " + lod2 + "; " + renderers + " renderers";
        }
    }

    // ---------------------------------------------------------------- dimensions (metres; section x across, y up from the riding surface)

    const float T = .008f;                   // the laminate behind the gel coat (LS76: 6-10 mm)
    const float RB = .45f, CB = .45f;        // the trough's bottom: radius and centre height (0.9 inside, Slide.cs ChannelProfile)
    const float Top = .65f;                  // the trough's walls and its lip top
    const float LipOut = .52f, LipT = .012f; // the rolled lip flange: out to x 0.52 (70 mm), 12 mm thick
    const float RA = .60f;                   // SL-A: the 1.2 m bore, its bottom on the riding surface
    const float CapR = .46f;                 // SL-C: the cap's inside radius over the lips (its flanges sit on them)
    const float RibH = .060f, RibW = .050f;  // a flange rib: 60 mm proud, 50 mm along the line (61 section 2.2-2.3)
    const float Gap = .0015f;                // half the sealant line between the two ribs of a pair
    const float BoltPitch = .150f;           // 3/8 in bolts at 6 in round each flange (LS77: up to 60 a flange)
    const float LaneHalf = .32f;             // the riding lane's half-width, in metres of arc from the bottom (FlumeTrack maps)
    const float BandLo = .38f, BandHi = .63f;// the teal band on SL-B's outer skin (heights; Slide.cs bandL / bandR)

    // materials of a section edge
    const int MIvory = 0, MTrack = 1, MBand = 2, MCap = 3, MMilky = 4, MMilkyRib = 5, MSteel = 6, MSeal = 7, MRubber = 8, MCount = 9;
    static readonly string[] MatNames =
    {
        "Prop_Gelcoat_Ivory", "Prop_Gelcoat_Track", "Prop_Gelcoat_TealBand", "Prop_Gelcoat_SLC_Cap", "Prop_Gelcoat_SLA_Milky",
        "Prop_Gelcoat_SLA_Flange", "Prop_StainlessSatin", "Prop_LaminateGrey", "Prop_Rubber"
    };
    // a stand-in colour if a surface is not in this build (logged once): never a missing-material magenta
    static readonly Color[] MatFallback =
    {
        new Color(.96f, .92f, .82f), new Color(.95f, .91f, .81f), new Color(0f, .50f, .52f), new Color(.15f, .24f, .26f),
        new Color(.62f, .86f, .86f), new Color(.52f, .78f, .79f), new Color(.62f, .62f, .61f), new Color(.45f, .45f, .44f),
        new Color(.03f, .03f, .03f)
    };

    static Material[] mats;
    static bool matsPhone;

    /// <summary>The surfaces (their _Phone twins on the phone tier, as FrontRoomsStructuresAssets loads the structures surfaces).</summary>
    static Material Mat(int id)
    {
        var phone = FrontRoomsTrofferKit.PhoneTier;
        if (mats == null || matsPhone != phone)
        {
            mats = new Material[MCount];
            matsPhone = phone;
        }
        if (mats[id] != null) return mats[id];
        Material m = phone ? FrontRoomsSurfaces.TryGet(MatNames[id] + "_Phone") : null;
        if (m == null) m = FrontRoomsSurfaces.TryGet(MatNames[id]);
        if (m == null)
        {
            Debug.LogWarning("[Structures] flume: surface " + MatNames[id] + " is not in this build; a plain stand-in is used.");
            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = MatNames[id] + " (stand-in)" };
            m.SetColor("_BaseColor", MatFallback[id]);
        }
        mats[id] = m;
        return m;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { mats = null; }


    // ---------------------------------------------------------------- sections

    /// <summary>A closed 2-D outline of a solid (counter-clockwise: the solid on the left, its outward normal on the right). Per
    /// point: the material and part of the edge ARRIVING at it (so an arc's points carry the arc's), and its V (metres along the
    /// outline from point 0). Edge k runs from point k to point k + 1 (the last closes to 0).</summary>
    sealed class Outline
    {
        public readonly List<Vector2> p = new List<Vector2>();
        readonly List<int> inMat = new List<int>();
        readonly List<int> inPart = new List<int>();   // 0 inner (the riding side), 1 lip, 2 outer
        public float[] v;
        public float length;

        public int Count { get { return p.Count; } }
        public int EdgeMat(int k) { return inMat[(k + 1) % p.Count]; }
        public int EdgePart(int k) { return inPart[(k + 1) % p.Count]; }
        public void SetEdgeMat(int k, int m) { inMat[(k + 1) % p.Count] = m; }
        public int PointMat(int k) { return inMat[k]; }
        public int PointPart(int k) { return inPart[k]; }

        public void Add(Vector2 q, int m, int pt)
        {
            if (p.Count > 0 && (p[p.Count - 1] - q).sqrMagnitude < 1e-12f) { inMat[p.Count - 1] = m; inPart[p.Count - 1] = pt; return; }
            p.Add(q);
            inMat.Add(m);
            inPart.Add(pt);
        }

        public void Close()
        {
            if (p.Count > 2 && (p[p.Count - 1] - p[0]).sqrMagnitude < 1e-12f)
            {
                inMat[0] = inMat[p.Count - 1];
                inPart[0] = inPart[p.Count - 1];
                p.RemoveAt(p.Count - 1); inMat.RemoveAt(inMat.Count - 1); inPart.RemoveAt(inPart.Count - 1);
            }
            v = new float[p.Count];
            for (var k = 1; k < p.Count; k++) v[k] = v[k - 1] + Vector2.Distance(p[k - 1], p[k]);
            length = v[p.Count - 1] + Vector2.Distance(p[p.Count - 1], p[0]);
        }

        public Vector2 EdgeNormal(int k)
        {
            var d = p[(k + 1) % p.Count] - p[k];
            var n = new Vector2(d.y, -d.x);
            return n.sqrMagnitude > 1e-14f ? n.normalized : Vector2.up;
        }

        /// <summary>The V at the end of edge k (the outline's length for the closing edge).</summary>
        public float VEnd(int k) { return k + 1 < p.Count ? v[k + 1] : length; }
    }

    // Points on an arc (centre c, radius r) from degree a0 to a1 in n steps (the first point skipped when skipFirst); each point
    // carries material m for the edge arriving at it.
    static void Arc(Outline o, Vector2 c, float r, float a0, float a1, int n, int m, int pt, bool skipFirst = false)
    {
        n = Mathf.Max(1, n);
        for (var k = skipFirst ? 1 : 0; k <= n; k++)
        {
            var a = Mathf.Lerp(a0, a1, k / (float)n) * Mathf.Deg2Rad;
            o.Add(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, m, pt);
        }
    }

    static int BottomSteps(int lod) { return lod == 0 ? 8 : lod == 1 ? 5 : 3; }    // a quarter of the trough's round bottom
    static int RoundSteps(int lod) { return lod == 0 ? 3 : lod == 1 ? 1 : 0; }      // a small round (corner, nose, fillet)

    /// <summary>The trough (SL-B; SL-C under its cap): the inner skin from the riding line's bottom centre (V 0) round the left,
    /// the rolled lip, the outer skin, the right side mirrored. Materials: SL-B ivory with the riding lane and the teal band;
    /// SL-C's inside dark (the cap's gel coat, 61: the dark marks the blind spots), its outside ivory.</summary>
    static Outline Channel(int lod, int style, bool fine = false)
    {
        var half = new Outline();
        var inner = style == SLC ? MCap : MIvory;
        // fine (LOD0 of a piece a player can come near: the exit lip, the low runs, the slot): 16 steps a quarter, 0.55 mm chords
        int nb = fine && lod == 0 ? 16 : BottomSteps(lod), nr = RoundSteps(lod);
        const float rIn = .010f, rNose = LipT * .5f, rFil = .008f, ro = RB + T;
        // the inner skin: the bottom quarter (from -90 to -180 deg, V 0 at the bottom centre), the wall, the round onto the lip top
        Arc(half, new Vector2(0f, CB), RB, -90f, -180f, nb, inner, 0);
        if (nr > 0)
        {
            half.Add(new Vector2(-RB, Top - rIn), inner, 0);
            Arc(half, new Vector2(-RB - rIn, Top - rIn), rIn, 0f, 90f, nr, inner, 0, true);
            // the lip top, its rolled nose, its underside, the fillet into the outer wall
            half.Add(new Vector2(-LipOut + rNose, Top), MIvory, 1);
            Arc(half, new Vector2(-LipOut + rNose, Top - rNose), rNose, 90f, 270f, nr + 1, MIvory, 1, true);
            half.Add(new Vector2(-ro - rFil, Top - LipT), MIvory, 1);
            Arc(half, new Vector2(-ro - rFil, Top - LipT - rFil), rFil, 90f, 0f, nr, MIvory, 2, true);
        }
        else
        {
            half.Add(new Vector2(-RB, Top), inner, 0);
            half.Add(new Vector2(-LipOut, Top), MIvory, 1);
            half.Add(new Vector2(-LipOut, Top - LipT), MIvory, 1);
            half.Add(new Vector2(-ro, Top - LipT), MIvory, 1);
        }
        // the outer wall down to the round bottom, the outer bottom quarter to the bottom centre
        half.Add(new Vector2(-ro, CB), MIvory, 2);
        Arc(half, new Vector2(0f, CB), ro, 180f, 270f, nb, MIvory, 2, true);
        // the right half: the left one mirrored, walked back (the edge j -> j + 1 of the left arrives at j's mirror)
        var o = new Outline();
        var last = half.Count - 1;
        o.Add(half.p[0], half.PointMat(1), half.PointPart(1));
        for (var k = 1; k <= last; k++) o.Add(half.p[k], half.PointMat(k), half.PointPart(k));
        for (var k = last - 1; k >= 1; k--) o.Add(new Vector2(-half.p[k].x, half.p[k].y), half.PointMat(k + 1), half.PointPart(k + 1));
        o.Close();
        if (style == SLB) PaintChannel(o);
        return o;
    }

    // SL-B's lane (the inner skin within LaneHalf of arc from the bottom centre) and band (the outer skin between BandLo and BandHi).
    static void PaintChannel(Outline o)
    {
        for (var k = 0; k < o.Count; k++)
        {
            var a = o.p[k];
            var b = o.p[(k + 1) % o.Count];
            var vMid = (o.v[k] + o.VEnd(k)) * .5f;
            var across = Mathf.Min(vMid, o.length - vMid);
            if (o.EdgePart(k) == 0 && across <= LaneHalf + 1e-4f) o.SetEdgeMat(k, MTrack);
            var yMid = (a.y + b.y) * .5f;
            if (o.EdgePart(k) == 2 && yMid >= BandLo && yMid <= BandHi) o.SetEdgeMat(k, MBand);
        }
    }

    /// <summary>SL-A's tube: the outer skin (counter-clockwise) or the inner one (clockwise: the solid lies outside it); V from
    /// the bottom on the riding surface.</summary>
    static Outline Tube(int lod, bool outerSkin, bool fine = false)
    {
        var o = new Outline();
        var n = lod == 0 ? (fine ? 64 : 48) : lod == 1 ? 24 : 12;
        var r = outerSkin ? RA + T : RA;
        for (var k = 0; k < n; k++)
        {
            var a = (-90f + (outerSkin ? 1f : -1f) * k * 360f / n) * Mathf.Deg2Rad;
            o.Add(new Vector2(Mathf.Cos(a) * r, RA + Mathf.Sin(a) * r), MMilky, outerSkin ? 2 : 0);
        }
        o.Close();
        return o;
    }

    /// <summary>SL-C's cap: a half-round lid (inside radius CapR, 8 mm) over the trough, its 12 mm edge flanges on the lips; from
    /// the inside top (V 0) clockwise down the right inside, the right flange, the outside back over the top, the left flange.</summary>
    static Outline Cap(int lod, bool fine = false)
    {
        var o = new Outline();
        int n = lod == 0 ? (fine ? 32 : 24) : lod == 1 ? 12 : 6, nr = RoundSteps(lod);
        float ro = CapR + T, yF = Top, yT = Top + LipT;
        const float rr = .004f;
        var c = new Vector2(0f, Top);
        Arc(o, c, CapR, 90f, 0f, n, MCap, 0);
        o.Add(new Vector2(LipOut - (nr > 0 ? rr : 0f), yF), MCap, 1);
        if (nr > 0) Arc(o, new Vector2(LipOut - rr, yF + rr), rr, -90f, 0f, nr, MCap, 1, true);
        if (nr > 0) { o.Add(new Vector2(LipOut, yT - rr), MCap, 1); Arc(o, new Vector2(LipOut - rr, yT - rr), rr, 0f, 90f, nr, MCap, 1, true); }
        else { o.Add(new Vector2(LipOut, yF), MCap, 1); o.Add(new Vector2(LipOut, yT), MCap, 1); }
        var a0 = Mathf.Asin(LipT / ro) * Mathf.Rad2Deg;
        o.Add(new Vector2(Mathf.Cos(a0 * Mathf.Deg2Rad) * ro, yT), MCap, 1);
        Arc(o, c, ro, a0, 180f - a0, n, MCap, 2, true);
        o.Add(new Vector2(-LipOut + (nr > 0 ? rr : 0f), yT), MCap, 1);
        if (nr > 0) { Arc(o, new Vector2(-LipOut + rr, yT - rr), rr, 90f, 180f, nr, MCap, 1, true); o.Add(new Vector2(-LipOut, yF + rr), MCap, 1); Arc(o, new Vector2(-LipOut + rr, yF + rr), rr, 180f, 270f, nr, MCap, 1, true); }
        else o.Add(new Vector2(-LipOut, yF), MCap, 1);
        o.Add(new Vector2(-CapR, yF), MCap, 1);
        Arc(o, c, CapR, 180f, 90f, n, MCap, 0, true);
        o.Close();
        return o;
    }

    // ---------------------------------------------------------------- meshes

    /// <summary>One mesh in the making: a shared vertex list and a triangle list per material (a submesh each).</summary>
    sealed class Parts
    {
        public readonly List<Vector3> v = new List<Vector3>();
        public readonly List<Vector3> n = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>();
        public readonly List<int>[] t = new List<int>[MCount];
        public int tris;

        public Parts() { for (var i = 0; i < MCount; i++) t[i] = new List<int>(); }

        public int Vert(Vector3 p, Vector3 nrm, Vector2 u) { v.Add(p); n.Add(nrm); uv.Add(u); return v.Count - 1; }

        // A triangle facing 'outward' (either winding given).
        public void Tri(int m, int a, int b, int c, Vector3 outward)
        {
            var f = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
            if (f.sqrMagnitude < 1e-16f) return;
            if (Vector3.Dot(f, outward) >= 0f) { t[m].Add(a); t[m].Add(b); t[m].Add(c); }
            else { t[m].Add(a); t[m].Add(c); t[m].Add(b); }
            tris++;
        }

        // A quad a b c d (in order round it) facing 'outward'.
        public void Quad(int m, int a, int b, int c, int d, Vector3 outward)
        {
            Tri(m, a, b, c, outward);
            Tri(m, a, c, d, outward);
        }

        public Mesh ToMesh(string name, out Material[] used)
        {
            var list = new List<Material>();
            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            if (v.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            var subs = 0;
            for (var i = 0; i < MCount; i++) if (t[i].Count > 0) subs++;
            mesh.subMeshCount = Mathf.Max(1, subs);
            var s = 0;
            for (var i = 0; i < MCount; i++)
            {
                if (t[i].Count == 0) continue;
                mesh.SetTriangles(t[i], s++, false);
                list.Add(Mat(i));
            }
            mesh.RecalculateBounds();
            if (v.Count > 0) mesh.RecalculateTangents();
            used = list.ToArray();
            return mesh;
        }
    }

    /// <summary>A cross-section frame on the line: its point, right (horizontal) and up (square to the line) and u (metres along).</summary>
    struct Ring
    {
        public Vector3 p, r, u, t;
        public float s;
    }

    // The line's frame at arc length s (district metres), interpolated between its samples.
    static Ring At(IList<Vector3> p, IList<Vector3> right, IList<Vector3> up, IList<float> s, float at)
    {
        int lo = 0, hi = s.Count - 1;
        if (at <= s[0]) hi = 1;
        else if (at >= s[hi]) lo = hi - 1;
        else
            while (hi - lo > 1)
            {
                var mid = (lo + hi) / 2;
                if (s[mid] <= at) lo = mid; else hi = mid;
            }
        var f = s[hi] > s[lo] ? Mathf.Clamp01((at - s[lo]) / (s[hi] - s[lo])) : 0f;
        var g = new Ring { p = Vector3.Lerp(p[lo], p[hi], f), r = Vector3.Slerp(right[lo], right[hi], f).normalized, u = Vector3.Slerp(up[lo], up[hi], f).normalized, s = at };
        if (f <= 0f) { g.r = right[lo]; g.u = up[lo]; }
        else if (f >= 1f) { g.r = right[hi]; g.u = up[hi]; }
        g.t = Vector3.Cross(g.r, g.u).normalized;
        if (Vector3.Dot(g.t, p[hi] - p[lo]) < 0f) g.t = -g.t;
        return g;
    }

    // A sample's own ring (the joint samples carry the bisector frame: Slide.cs takes each tangent from the samples either side).
    static Ring Sample(IList<Vector3> p, IList<Vector3> right, IList<Vector3> up, IList<float> s, int i)
    {
        var g = new Ring { p = p[i], r = right[i], u = up[i], s = s[i] };
        var a = p[Mathf.Max(0, i - 1)];
        var b = p[Mathf.Min(p.Count - 1, i + 1)];
        g.t = Vector3.Cross(g.r, g.u).normalized;
        if (Vector3.Dot(g.t, b - a) < 0f) g.t = -g.t;
        return g;
    }

    // A piece's rings at this LOD: a straight cell its two ends (mitred), a bend bay every 5.6 deg at LOD0 (8: the chord's sagitta
    // 3.6 mm on r 3.0, sub-pixel past 2 m), 5 and 3 below.
    static List<Ring> Rings(IList<Vector3> p, IList<Vector3> right, IList<Vector3> up, IList<float> s, Piece piece, int lod)
    {
        var list = new List<Ring>();
        var n = piece.arc ? (lod == 0 ? (piece.near ? 12 : 8) : lod == 1 ? 5 : 3) : 1;
        n = Mathf.Min(n, Mathf.Max(1, piece.i1 - piece.i0));
        for (var k = 0; k <= n; k++)
        {
            var i = piece.i0 + Mathf.RoundToInt((piece.i1 - piece.i0) * (k / (float)n));
            list.Add(Sample(p, right, up, s, i));
        }
        return list;
    }

    static Vector3 Place(Ring g, Vector2 q) { return g.p + g.r * q.x + g.u * q.y; }
    static Vector3 Dir(Ring g, Vector2 d) { return (g.r * d.x + g.u * d.y).normalized; }

    // The smooth or split normal of outline point k on its edge 'edge' (the two edges meeting there average below 35 deg).
    static Vector2 PointNormal(Outline o, int k, int edge)
    {
        var nOwn = o.EdgeNormal(edge);
        var other = edge == k ? (k - 1 + o.Count) % o.Count : (k + 1) % o.Count;
        var nOther = o.EdgeNormal(other);
        return Vector2.Angle(nOwn, nOther) < 35f ? (nOwn + nOther).normalized : nOwn;
    }

    /// <summary>The UV v of an outline point on edge k (0 its start, 1 its end): the riding lane takes its own (across the lane,
    /// metres from -LaneHalf), the rest metres round the outline (times vScale, which closes a tube's seam on whole tiles).</summary>
    static float V(Outline o, int k, int end, int mat, float vScale)
    {
        var v = end == 0 ? o.v[k] : o.VEnd(k);
        if (mat == MTrack)
        {
            var across = v <= o.length * .5f ? -v : o.length - v;
            return across + LaneHalf;
        }
        return v * vScale;
    }

    // vScale for a closed skin: the outline's length on whole 0.6 m tiles of the gel coat maps (no seam at V 0).
    static float SeamScale(Outline o) { var tiles = Mathf.Max(1f, Mathf.Round(o.length / .6f)); return tiles * .6f / o.length; }

    /// <summary>Sweeps an outline through the rings into the parts (each edge its own column of vertices, shared ring to ring).</summary>
    static void Sweep(Parts parts, Outline o, List<Ring> rings, float vScale)
    {
        for (var k = 0; k < o.Count; k++)
        {
            var m = o.EdgeMat(k);
            var k1 = (k + 1) % o.Count;
            var na = PointNormal(o, k, k);
            var nb = PointNormal(o, k1, k);
            var va = V(o, k, 0, m, vScale);
            var vb = V(o, k, 1, m, vScale);
            int pa = -1, pb = -1;
            for (var j = 0; j < rings.Count; j++)
            {
                var g = rings[j];
                var a = parts.Vert(Place(g, o.p[k]), Dir(g, na), new Vector2(g.s, va));
                var b = parts.Vert(Place(g, o.p[k1]), Dir(g, nb), new Vector2(g.s, vb));
                if (j > 0) parts.Quad(m, pa, pb, b, a, Dir(g, o.EdgeNormal(k)));
                pa = a;
                pb = b;
            }
        }
    }

    /// <summary>Closes an outline's end at a ring (the solid's face, facing 'outward' along the line).</summary>
    static void EndFace(Parts parts, Outline o, Ring g, Vector3 outward, int mat)
    {
        var poly = o.p.ToArray();
        var order = FrontRoomsStructureProfiles.EarClip(poly);
        var first = parts.v.Count;
        foreach (var q in poly) parts.Vert(Place(g, q), outward, new Vector2(q.x, q.y));
        for (var i = 0; i + 2 < order.Count; i += 3) parts.Tri(mat, first + order[i], first + order[i + 1], first + order[i + 2], outward);
    }

    /// <summary>Closes the annulus between two closed outlines (SL-A's skins) at a ring.</summary>
    static void EndAnnulus(Parts parts, Outline outer, Outline inner, Ring g, Vector3 outward, int mat)
    {
        var n = outer.Count;
        if (inner.Count != n) return;
        var first = parts.v.Count;
        for (var k = 0; k < n; k++)
        {
            parts.Vert(Place(g, outer.p[k]), outward, outer.p[k]);
            // the inner skin runs the other way round: point k of the outer meets point (n - k) % n of the inner
            parts.Vert(Place(g, inner.p[(n - k) % n]), outward, inner.p[(n - k) % n]);
        }
        for (var k = 0; k < n; k++)
        {
            int a = first + 2 * k, b = first + 2 * ((k + 1) % n);
            parts.Quad(mat, a, b, b + 1, a + 1, outward);
        }
    }

    // ---------------------------------------------------------------- flange ribs and bolts

    /// <summary>A rib's path round the shell (section points, at the shell's outer surface), its outward normals and whether it
    /// closes on itself (the tube); the rib stands RibH proud of it (the cap's 52 mm, so it ends over the cap's flange).</summary>
    sealed class RibPath
    {
        public readonly List<Vector2> p = new List<Vector2>();
        public readonly List<Vector2> n = new List<Vector2>();
        public bool closed;
        public float h = RibH;
        public int mat;
        public float Length
        {
            get
            {
                var l = 0f;
                for (var k = 1; k < p.Count; k++) l += Vector2.Distance(p[k - 1], p[k]);
                if (closed && p.Count > 1) l += Vector2.Distance(p[p.Count - 1], p[0]);
                return l;
            }
        }
    }

    static RibPath ChannelRib(int lod)
    {
        var r = new RibPath { mat = MIvory };
        var nb = BottomSteps(lod) * 2;
        const float ro = RB + T, yTop = Top - LipT;
        r.p.Add(new Vector2(-ro, yTop)); r.n.Add(Vector2.left);
        for (var k = 0; k <= nb; k++)
        {
            var a = (180f + 180f * k / nb) * Mathf.Deg2Rad;
            var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            r.p.Add(new Vector2(0f, CB) + d * ro);
            r.n.Add(d);
        }
        r.p.Add(new Vector2(ro, yTop)); r.n.Add(Vector2.right);
        return r;
    }

    static RibPath TubeRib(int lod)
    {
        var r = new RibPath { mat = MMilkyRib, closed = true };
        var n = lod == 0 ? 48 : lod == 1 ? 24 : 12;
        for (var k = 0; k < n; k++)
        {
            var a = (-90f + 360f * k / n) * Mathf.Deg2Rad;
            var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            r.p.Add(new Vector2(0f, RA) + d * (RA + T));
            r.n.Add(d);
        }
        return r;
    }

    static RibPath CapRib(int lod)
    {
        var r = new RibPath { mat = MCap, h = LipOut - (CapR + T) };
        var n = lod == 0 ? 24 : lod == 1 ? 12 : 6;
        var ro = CapR + T;
        var a0 = Mathf.Asin(LipT / ro) * Mathf.Rad2Deg;
        for (var k = 0; k <= n; k++)
        {
            var a = Mathf.Lerp(180f - a0, a0, k / (float)n) * Mathf.Deg2Rad;
            var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            r.p.Add(new Vector2(0f, Top) + d * ro);
            r.n.Add(d);
        }
        return r;
    }

    /// <summary>
    /// One rib between the line distances d0 and d1 from the joint (the signed side: -1 upstream, +1 downstream): its two flat
    /// faces and its rim (at LOD0 with 4 mm chamfers on the rim's edges). The faces at d0 (the joint side) and d1 (the bolt side).
    /// </summary>
    static void Rib(Parts parts, RibPath rp, IList<Vector3> p, IList<Vector3> right, IList<Vector3> up, IList<float> s, float sJoint, float d0, float d1, int lod)
    {
        var g0 = At(p, right, up, s, sJoint + d0);
        var g1 = At(p, right, up, s, sJoint + d1);
        var side = Mathf.Sign(d1 - d0);
        var c = lod == 0 ? .004f : 0f;
        var n = rp.p.Count;
        var segs = rp.closed ? n : n - 1;
        // the rim: the outer edge path at d0 + c .. d1 - c (full height), chamfers to h - c at d0 and d1
        var rimRings = new List<KeyValuePair<Ring, float>>();
        if (c > 0f)
        {
            rimRings.Add(new KeyValuePair<Ring, float>(g0, rp.h - c));
            rimRings.Add(new KeyValuePair<Ring, float>(At(p, right, up, s, sJoint + d0 + side * c), rp.h));
            rimRings.Add(new KeyValuePair<Ring, float>(At(p, right, up, s, sJoint + d1 - side * c), rp.h));
            rimRings.Add(new KeyValuePair<Ring, float>(g1, rp.h - c));
        }
        else
        {
            rimRings.Add(new KeyValuePair<Ring, float>(g0, rp.h));
            rimRings.Add(new KeyValuePair<Ring, float>(g1, rp.h));
        }
        var along = 0f;
        for (var k = 0; k < segs; k++)
        {
            var k1 = (k + 1) % n;
            var seg = Vector2.Distance(rp.p[k], rp.p[k1]);
            int pa = -1, pb = -1;
            for (var j = 0; j < rimRings.Count; j++)
            {
                var g = rimRings[j].Key;
                var h = rimRings[j].Value;
                var tilt = j == 0 ? -side : j == rimRings.Count - 1 ? side : 0f;
                var na = (Dir(g, rp.n[k]) + g.t * (tilt * (c > 0f ? 1f : 0f))).normalized;
                var nb = (Dir(g, rp.n[k1]) + g.t * (tilt * (c > 0f ? 1f : 0f))).normalized;
                var a = parts.Vert(Place(g, rp.p[k] + rp.n[k] * h), na, new Vector2(along, g.s));
                var b = parts.Vert(Place(g, rp.p[k1] + rp.n[k1] * h), nb, new Vector2(along + seg, g.s));
                if (j > 0) parts.Quad(rp.mat, pa, pb, b, a, Dir(g, (rp.n[k] + rp.n[k1]).normalized));
                pa = a;
                pb = b;
            }
            along += seg;
        }
        // the two flat faces: a strip between the shell (offset 0) and the rim's foot (h - c)
        for (var f = 0; f < 2; f++)
        {
            var g = f == 0 ? g0 : g1;
            var outward = f == 0 ? -side * g.t : side * g.t;
            int pa = -1, pb = -1;
            for (var k = 0; k <= (rp.closed ? n : n - 1); k++)
            {
                var kk = k % n;
                var a = parts.Vert(Place(g, rp.p[kk]), outward, rp.p[kk]);
                var b = parts.Vert(Place(g, rp.p[kk] + rp.n[kk] * (rp.h - c)), outward, rp.p[kk] + rp.n[kk] * (rp.h - c));
                if (k > 0) parts.Quad(rp.mat, pa, pb, b, a, outward);
                pa = a;
                pb = b;
            }
            // the rib's open ends (the trough's and the cap's ribs end under the lip and on the cap's flange): closed
            if (!rp.closed)
                for (var e = 0; e < 2; e++)
                {
                    var kk = e == 0 ? 0 : n - 1;
                    var tan = e == 0 ? rp.p[0] - rp.p[1] : rp.p[n - 1] - rp.p[n - 2];
                    var o = Dir(g, tan.normalized);
                    var a0 = parts.Vert(Place(g0, rp.p[kk]), o, Vector2.zero);
                    var b0 = parts.Vert(Place(g0, rp.p[kk] + rp.n[kk] * rp.h), o, Vector2.up);
                    var a1 = parts.Vert(Place(g1, rp.p[kk]), o, Vector2.right);
                    var b1 = parts.Vert(Place(g1, rp.p[kk] + rp.n[kk] * rp.h), o, Vector2.one);
                    if (f == 0) parts.Quad(rp.mat, a0, b0, b1, a1, o);
                }
        }
    }

    /// <summary>The bolt positions on a rib: mid-height on the rib, every BoltPitch along its path (ends kept 60 mm clear).</summary>
    static List<KeyValuePair<Vector2, Vector2>> BoltSpots(RibPath rp)
    {
        var spots = new List<KeyValuePair<Vector2, Vector2>>();
        var n = rp.p.Count;
        var segs = rp.closed ? n : n - 1;
        var total = rp.Length;
        int count;
        float start, step;
        if (rp.closed) { count = Mathf.Max(4, Mathf.RoundToInt(total / BoltPitch)); step = total / count; start = step * .5f; }
        else { count = Mathf.Max(2, Mathf.RoundToInt((total - .12f) / BoltPitch) + 1); step = (total - .12f) / (count - 1); start = .06f; }
        var acc = 0f;
        var next = start;
        var made = 0;
        for (var k = 0; k < segs && made < count; k++)
        {
            var a = rp.p[k];
            var b = rp.p[(k + 1) % n];
            var seg = Vector2.Distance(a, b);
            while (made < count && next <= acc + seg + 1e-5f)
            {
                var f = seg > 1e-6f ? Mathf.Clamp01((next - acc) / seg) : 0f;
                var nrm = Vector2.Lerp(rp.n[k], rp.n[(k + 1) % n], f).normalized;
                spots.Add(new KeyValuePair<Vector2, Vector2>(Vector2.Lerp(a, b, f) + nrm * (rp.h * .5f), nrm));
                made++;
                next += step;
            }
            acc += seg;
        }
        return spots;
    }

    // A prism of 'sides' sides round 'axis' from 'c' (its foot, on a face) to c + axis * h: its sides and its end (a fan).
    static void Prism(Parts parts, int mat, Vector3 c, Vector3 axis, Vector3 refDir, float radius, int sides, float h, float phaseDeg)
    {
        axis.Normalize();
        var x = (refDir - axis * Vector3.Dot(refDir, axis)).normalized;
        if (x.sqrMagnitude < 1e-6f) x = Vector3.Cross(axis, Vector3.up).normalized;
        var y = Vector3.Cross(axis, x).normalized;
        var top = c + axis * h;
        var foot = new int[sides + 1];
        var head = new int[sides + 1];
        // flat sides: each side its own normal (two vertices per corner per side)
        for (var k = 0; k < sides; k++)
        {
            var a0 = (phaseDeg + 360f * k / sides) * Mathf.Deg2Rad;
            var a1 = (phaseDeg + 360f * (k + 1) / sides) * Mathf.Deg2Rad;
            var d0 = x * Mathf.Cos(a0) + y * Mathf.Sin(a0);
            var d1 = x * Mathf.Cos(a1) + y * Mathf.Sin(a1);
            var nrm = sides <= 8 ? (d0 + d1).normalized : d0;
            var i0 = parts.Vert(c + d0 * radius, sides <= 8 ? nrm : d0, Vector2.zero);
            var i1 = parts.Vert(c + d1 * radius, sides <= 8 ? nrm : d1, new Vector2(1f, 0f));
            var i2 = parts.Vert(top + d1 * radius, sides <= 8 ? nrm : d1, new Vector2(1f, h));
            var i3 = parts.Vert(top + d0 * radius, sides <= 8 ? nrm : d0, new Vector2(0f, h));
            parts.Quad(mat, i0, i1, i2, i3, (d0 + d1).normalized);
        }
        // the end
        var centre = parts.Vert(top, axis, Vector2.zero);
        var ring = new int[sides];
        for (var k = 0; k < sides; k++)
        {
            var a = (phaseDeg + 360f * k / sides) * Mathf.Deg2Rad;
            ring[k] = parts.Vert(top + (x * Mathf.Cos(a) + y * Mathf.Sin(a)) * radius, axis, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
        }
        for (var k = 0; k < sides; k++) parts.Tri(mat, centre, ring[k], ring[(k + 1) % sides], axis);
    }

    /// <summary>One 3/8 in stainless bolt seen from one side of a flange: a washer (25 mm, 2 mm) and the hex head (9/16 in across
    /// flats, 6.4 mm), or on the nut side the washer, the hex nut (8.3 mm) and 6 mm of thread; LOD1 the hex alone; LOD2 nothing.</summary>
    static int Bolt(Parts parts, Vector3 c, Vector3 axis, Vector3 refDir, bool head, int lod)
    {
        if (lod >= 2) return 0;
        const float hexR = .0143f / 1.7320508f;   // circumradius of a 14.3 mm across-flats hex
        var before = parts.tris;
        if (lod == 0)
        {
            Prism(parts, MSteel, c, axis, refDir, .0125f, 8, .0018f, 0f);
            Prism(parts, MSteel, c + axis * .0018f, axis, refDir, hexR, 6, head ? .0064f : .0083f, 0f);
            if (!head) Prism(parts, MSteel, c + axis * (.0018f + .0083f), axis, refDir, .0048f, 6, .006f, 30f);
        }
        else Prism(parts, MSteel, c, axis, refDir, hexR * .9f, 4, head ? .0064f : .0083f, 45f);   // 1-2 px past 5 m: a square reads as the hex
        return parts.tris - before;
    }

    /// <summary>A half flange at a joint on the side 'side' (-1 upstream, +1 downstream): the rib(s) of 'style' and its bolts on
    /// the rib's outer face, heads (upstream) or nuts (downstream; the start's single rib carries heads).</summary>
    static int Half(Parts parts, IList<Vector3> p, IList<Vector3> right, IList<Vector3> up, IList<float> s, float sJoint, int style, float side, bool heads, int lod)
    {
        var bolts = 0;
        var ribs = new List<RibPath>();
        if (style == SLA) ribs.Add(TubeRib(lod));
        else
        {
            ribs.Add(ChannelRib(lod));
            if (style == SLC) ribs.Add(CapRib(lod));
        }
        foreach (var rp in ribs)
        {
            var d0 = side * Gap;
            var d1 = side * (Gap + RibW);
            Rib(parts, rp, p, right, up, s, sJoint, d0, d1, lod);
            if (lod >= 2) continue;
            var g = At(p, right, up, s, sJoint + d1);
            foreach (var spot in BoltSpots(rp))
            {
                Bolt(parts, Place(g, spot.Key), g.t * side, Dir(g, spot.Value), heads, lod);
                bolts++;
            }
        }
        return bolts;
    }

    /// <summary>The sealant between the two ribs of a pair: a grey band round the rims, 2 mm down between them, and the thin line
    /// wiped flush inside the trough (LS77) on the riding side (LOD0).</summary>
    static void Sealant(Parts parts, IList<Vector3> p, IList<Vector3> right, IList<Vector3> up, IList<float> s, float sJoint, int styleA, int styleB, int lod)
    {
        // the band between the rims: the narrower rib's path (both halves' paths where the styles agree)
        var rp = styleA == SLA && styleB == SLA ? TubeRib(lod) : ChannelRib(lod);
        var g0 = At(p, right, up, s, sJoint - Gap);
        var g1 = At(p, right, up, s, sJoint + Gap);
        var n = rp.p.Count;
        var segs = rp.closed ? n : n - 1;
        var h = rp.h - .002f;
        for (var k = 0; k < segs; k++)
        {
            var k1 = (k + 1) % n;
            var a0 = parts.Vert(Place(g0, rp.p[k] + rp.n[k] * h), Dir(g0, rp.n[k]), Vector2.zero);
            var b0 = parts.Vert(Place(g0, rp.p[k1] + rp.n[k1] * h), Dir(g0, rp.n[k1]), Vector2.right);
            var a1 = parts.Vert(Place(g1, rp.p[k] + rp.n[k] * h), Dir(g1, rp.n[k]), Vector2.up);
            var b1 = parts.Vert(Place(g1, rp.p[k1] + rp.n[k1] * h), Dir(g1, rp.n[k1]), Vector2.one);
            parts.Quad(MSeal, a0, b0, b1, a1, Dir(g0, (rp.n[k] + rp.n[k1]).normalized));
        }
        if (lod > 0 || styleA != styleB) return;
        // inside: a 4 mm line 0.8 mm proud of the riding side, across the inner skin of the trough or round the tube
        var inner = styleA == SLA ? Tube(lod, false) : Channel(lod, styleA);
        var w0 = At(p, right, up, s, sJoint - .002f);
        var w1 = At(p, right, up, s, sJoint + .002f);
        for (var k = 0; k < inner.Count; k++)
        {
            if (inner.EdgePart(k) != 0) continue;
            var k1 = (k + 1) % inner.Count;
            var nn = inner.EdgeNormal(k);
            var off = nn * .0008f;
            var a0 = parts.Vert(Place(w0, inner.p[k] + off), Dir(w0, nn), Vector2.zero);
            var b0 = parts.Vert(Place(w0, inner.p[k1] + off), Dir(w0, nn), Vector2.right);
            var a1 = parts.Vert(Place(w1, inner.p[k] + off), Dir(w1, nn), Vector2.up);
            var b1 = parts.Vert(Place(w1, inner.p[k1] + off), Dir(w1, nn), Vector2.one);
            parts.Quad(MSeal, a0, b0, b1, a1, Dir(w0, nn));
        }
    }

    // ---------------------------------------------------------------- the shell, the exit lip, the cap's bolt rows

    // An outline's points moved inward (against their smooth normals) by d: the rolled end's rings.
    static Outline Inset(Outline o, float d)
    {
        var q = new Outline();
        for (var k = 0; k < o.Count; k++)
        {
            var nPrev = o.EdgeNormal((k - 1 + o.Count) % o.Count);
            var nNext = o.EdgeNormal(k);
            var nrm = (nPrev + nNext).normalized;
            var cos = Mathf.Max(.35f, Vector2.Dot(nrm, nNext));
            q.Add(o.p[k] - nrm * (d / cos), o.PointMat(k), o.PointPart(k));
        }
        q.Close();
        q.v = o.v;
        q.length = o.length;
        return q;
    }

    // Sweeps through rings whose outlines differ (same point count, same materials): the exit lip's roll.
    static void SweepVarying(Parts parts, List<Outline> outs, List<Ring> rings, float vScale)
    {
        var o0 = outs[0];
        for (var k = 0; k < o0.Count; k++)
        {
            var m = o0.EdgeMat(k);
            var k1 = (k + 1) % o0.Count;
            var va = V(o0, k, 0, m, vScale);
            var vb = V(o0, k, 1, m, vScale);
            int pa = -1, pb = -1;
            for (var j = 0; j < rings.Count; j++)
            {
                var o = outs[j];
                var g = rings[j];
                // the roll turns the surface toward the line's end: blend the section normal with the ring's tangent
                var turn = rings.Count > 1 ? j / (float)(rings.Count - 1) : 0f;
                var na = Vector3.Slerp(Dir(g, PointNormal(o, k, k)), g.t, turn * turn).normalized;
                var nb = Vector3.Slerp(Dir(g, PointNormal(o, k1, k)), g.t, turn * turn).normalized;
                var a = parts.Vert(Place(g, o.p[k]), na, new Vector2(g.s, va));
                var b = parts.Vert(Place(g, o.p[k1]), nb, new Vector2(g.s, vb));
                if (j > 0) parts.Quad(m, pa, pb, b, a, Vector3.Slerp(Dir(g, o0.EdgeNormal(k)), g.t, turn).normalized);
                pa = a;
                pb = b;
            }
        }
    }

    static Ring Ahead(Ring g, float d) { g.p += g.t * d; g.s += d; return g; }

    /// <summary>The exit lip (LS71's rounded lip; 61 section 2.1): the last module runs on 60 mm past the riding line's end, over the
    /// pool's edge, and its laminate rolls round on itself (a 4 mm bead all round the section) instead of ending as a cut pipe.</summary>
    static void ExitLip(Parts parts, Outline o, Ring end, float vScale, int lod, int mat)
    {
        const float run = .06f, r = T * .5f;
        var rings = new List<Ring> { end, Ahead(end, run - r) };
        var outs = new List<Outline> { o, o };
        var steps = lod == 0 ? 3 : lod == 1 ? 2 : 1;
        for (var k = 1; k <= steps; k++)
        {
            var phi = (75f * k / steps) * Mathf.Deg2Rad;
            rings.Add(Ahead(end, run - r + r * Mathf.Sin(phi)));
            outs.Add(Inset(o, r * (1f - Mathf.Cos(phi))));
        }
        SweepVarying(parts, outs, rings, vScale);
        EndFace(parts, outs[outs.Count - 1], rings[rings.Count - 1], rings[rings.Count - 1].t, mat);
    }

    /// <summary>SL-C's cap bolted down to the trough's lips (LS70: "a canopy or cap ... attached to the flanges"): a hex head and
    /// washer on the cap's flange every 300 mm each side, clear of the joints; LOD0 only.</summary>
    static int CapBolts(Parts parts, IList<Vector3> p, IList<Vector3> right, IList<Vector3> up, IList<float> s, float s0, float s1)
    {
        var made = 0;
        for (var at = s0 + .15f; at <= s1 - .15f + 1e-4f; at += .30f)
        {
            var g = At(p, right, up, s, at);
            for (var side = -1; side <= 1; side += 2)
            {
                Bolt(parts, Place(g, new Vector2(side * .493f, Top + LipT)), g.u, g.r, true, 0);
                made++;
            }
        }
        return made;
    }

    // One piece's shell into the parts at a LOD; end faces where the style changes; the exit lip after the last piece.
    static void ShellPiece(Parts parts, IList<Vector3> p, IList<Vector3> right, IList<Vector3> up, IList<float> s, IList<Piece> pieces, int k, int lod, Result res)
    {
        var pc = pieces[k];
        var rings = Rings(p, right, up, s, pc, lod);
        var first = rings[0];
        var last = rings[rings.Count - 1];
        var startFace = k == 0 || pieces[k - 1].style != pc.style;
        var isLast = k == pieces.Count - 1;
        var endFace = !isLast && pieces[k + 1].style != pc.style;
        if (pc.style == SLA)
        {
            var outer = Tube(lod, true, pc.near);
            var inner = Tube(lod, false, pc.near);
            var vs = SeamScale(outer);
            Sweep(parts, outer, rings, vs);
            Sweep(parts, inner, rings, SeamScale(inner));
            if (startFace) EndAnnulus(parts, outer, inner, first, -first.t, MMilky);
            if (endFace || isLast) EndAnnulus(parts, outer, inner, last, last.t, MMilky);
            return;
        }
        var ch = Channel(lod, pc.style, pc.near);
        Sweep(parts, ch, rings, 1f);
        if (startFace) EndFace(parts, ch, first, -first.t, pc.style == SLC ? MCap : MIvory);
        if (endFace) EndFace(parts, ch, last, last.t, pc.style == SLC ? MCap : MIvory);
        if (isLast) ExitLip(parts, ch, last, 1f, lod, MIvory);
        if (pc.style == SLC)
        {
            var cap = Cap(lod, pc.near);
            var vs = SeamScale(cap);
            Sweep(parts, cap, rings, vs);
            if (startFace) EndFace(parts, cap, first, -first.t, MCap);
            if (endFace || isLast) EndFace(parts, cap, last, last.t, MCap);
            if (lod == 0 && pc.near) res.bolts += CapBolts(parts, p, right, up, s, s[pc.i0], s[pc.i1]);
        }
    }

    // ---------------------------------------------------------------- the build

    // LOD switch heights (screen share): a shell cluster (about 6-9 m) and a flange pair (about 1.3 m).
    static readonly float[] ShellLods = { .30f, .09f, .012f };
    static readonly float[] JointLods = { .10f, .02f, .004f };

    /// <summary>
    /// Builds the flume under 'parent' from the riding line (district metres: its samples p, their right and up frames, s the metres
    /// along) and its pieces (the tub left out). Shell clusters and flange pairs each carry a LODGroup with three levels. The
    /// meshes made are in the result's list (the district frees them with its own).
    /// </summary>
    public static Result Build(Transform parent, IList<Vector3> p, IList<Vector3> right, IList<Vector3> up, IList<float> s, IList<Piece> pieces, List<Mesh> owned)
    {
        var res = new Result();
        if (parent == null || pieces == null || pieces.Count == 0 || p == null || p.Count < 2) return res;
        var root = new GameObject("Flume (NEW: fibreglass shell, flanges, lips)").transform;
        root.SetParent(parent, false);
        // shell clusters: consecutive pieces of one zone, up to about 9 m each
        var clusters = new List<List<int>>();
        List<int> cur = null;
        var len = 0f;
        for (var k = 0; k < pieces.Count; k++)
        {
            var pl = s[pieces[k].i1] - s[pieces[k].i0];
            if (cur == null || pieces[k].zone != pieces[cur[0]].zone || len + pl > 9.2f)
            {
                cur = new List<int>();
                clusters.Add(cur);
                len = 0f;
            }
            cur.Add(k);
            len += pl;
        }
        for (var c = 0; c < clusters.Count; c++)
        {
            var lods = new Parts[3];
            for (var lod = 0; lod < 3; lod++)
            {
                lods[lod] = new Parts();
                foreach (var k in clusters[c]) ShellPiece(lods[lod], p, right, up, s, pieces, k, lod, lod == 0 ? res : new Result());
            }
            var name = "Flume shell " + (c + 1) + " · " + StyleNames(pieces, clusters[c]);
            MakeLodObject(root, name, lods, ShellLods, ShadowCastingMode.On, owned, res, true);
            res.clusters++;
        }
        // flange pairs: the start (the first module's flange on the tub's tiled lip) and every joint between two modules
        for (var k = 0; k < pieces.Count; k++)
        {
            var sj = s[pieces[k].i0];
            var lods = new Parts[3];
            var bolts = 0;
            var near = pieces[k].near || (k > 0 && pieces[k - 1].near);
            for (var lod = 0; lod < 3; lod++)
            {
                lods[lod] = new Parts();
                // a joint nobody comes near draws its LOD1 build at LOD0 as well (heads only, square rims)
                var detail = lod == 0 && !near ? 1 : lod;
                if (k == 0)
                {
                    var b = Half(lods[lod], p, right, up, s, sj, pieces[0].style, 1f, true, detail);
                    if (lod == 0) bolts += b;
                    continue;
                }
                var b1 = Half(lods[lod], p, right, up, s, sj, pieces[k - 1].style, -1f, true, detail);
                var b2 = Half(lods[lod], p, right, up, s, sj, pieces[k].style, 1f, false, detail);
                Sealant(lods[lod], p, right, up, s, sj, pieces[k - 1].style, pieces[k].style, detail);
                if (lod == 0) bolts += b1 + b2;
            }
            res.bolts += bolts;
            var label = k == 0 ? "start flange on the tub" : "flange pair " + k + " (" + StyleName(pieces[k - 1].style) + " | " + StyleName(pieces[k].style) + ")";
            MakeLodObject(root, "Flume " + label, lods, JointLods, ShadowCastingMode.On, owned, res, false);
            res.joints++;
        }
        return res;
    }

    static string StyleName(int style) { return style == SLA ? "SL-A" : style == SLB ? "SL-B" : "SL-C"; }

    static string StyleNames(IList<Piece> pieces, List<int> ks)
    {
        var seen = new List<string>();
        foreach (var k in ks) { var n = StyleName(pieces[k].style); if (!seen.Contains(n)) seen.Add(n); }
        return string.Join(" ", seen.ToArray());
    }

    static void MakeLodObject(Transform root, string name, Parts[] lods, float[] heights, ShadowCastingMode shadows, List<Mesh> owned, Result res, bool shell)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        var levels = new List<LOD>();
        for (var lod = 0; lod < lods.Length; lod++)
        {
            if (lods[lod].tris == 0) continue;
            Material[] used;
            var mesh = lods[lod].ToMesh(name + " LOD" + lod, out used);
            owned?.Add(mesh);
            var child = new GameObject("LOD" + lod);
            child.transform.SetParent(go.transform, false);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = child.AddComponent<MeshRenderer>();
            r.sharedMaterials = used;
            r.shadowCastingMode = shadows;
            levels.Add(new LOD(heights[lod], new Renderer[] { r }));
            res.renderers++;
            if (lod == 0) res.lod0 += lods[lod].tris; else if (lod == 1) res.lod1 += lods[lod].tris; else res.lod2 += lods[lod].tris;
            if (shell && lod == 0) res.shell.Add(r);
        }
        if (levels.Count == 0) return;
        var group = go.AddComponent<LODGroup>();
        group.SetLODs(levels.ToArray());
        group.RecalculateBounds();
        FrontRoomsLodFade.Apply(group);
    }

    // ---------------------------------------------------------------- the owners' tiled cradles and their bearing pads

    /// <summary>Plain mesh lists (district metres) for the owners' builder to append with its own material.</summary>
    public sealed class Lists
    {
        public readonly List<Vector3> v = new List<Vector3>();
        public readonly List<Vector3> n = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>();
        public readonly List<int> t = new List<int>();

        public int Vert(Vector3 p, Vector3 nrm, Vector2 u) { v.Add(p); n.Add(nrm); uv.Add(u); return v.Count - 1; }

        public void Tri(int a, int b, int c, Vector3 outward)
        {
            var f = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
            if (f.sqrMagnitude < 1e-16f) return;
            if (Vector3.Dot(f, outward) >= 0f) { t.Add(a); t.Add(b); t.Add(c); } else { t.Add(a); t.Add(c); t.Add(b); }
        }

        public void Quad(int a, int b, int c, int d, Vector3 outward) { Tri(a, b, c, outward); Tri(a, c, d, outward); }
    }

    /// <summary>A tiled cradle (Red: the tile walls carry the slide; 61 section 2.6: "the saddle follows the lower 130 deg of the
    /// tube"): its seat (tile by its radius, ask 2 A: the tile kind is SeatKind), its body (the 150 mm field) and the two bearing
    /// pads in its seat (10 mm black elastomer, the 5 mm gap the old saddle kept under the trough).</summary>
    public sealed class CradleMesh
    {
        public readonly Lists seat = new Lists(), body = new Lists(), pad = new Lists();
        public string SeatKind = "Field50Mesh";
    }

    /// <summary>The bearing pad's surface (Prop_Rubber; its phone twin on the phone tier).</summary>
    public static Material PadMaterial() { return Mat(MRubber); }

    /// <summary>
    /// The cradle at a joint (the owners' saddle: 0.7 m along the line, centred on the joint, split by the flange pair's slot): its
    /// bottom on the coping or kerb top (district y), 'width' across (the coping's 0.9 or the kerb's 0.6, 10 mm in from each side),
    /// its seat 10 mm under the shell's outer skin for as much of the lower 130 deg as the width holds, bullnosed lips.
    /// </summary>
    public static CradleMesh Cradle(IList<Vector3> p, IList<Vector3> right, IList<Vector3> up, IList<float> s, int joint, int style, float width, float bottomY)
    {
        var cm = new CradleMesh();
        if (p == null || joint < 0 || joint >= p.Count) return cm;
        var shellR = (style == SLA ? RA : RB) + T;
        var cy = style == SLA ? RA : CB;
        const float pad = .010f, half = .35f, rb = .0095f, shoulder = .022f;
        var rs = shellR + pad;
        var w = width * .5f;
        var theta = Mathf.Min(65f * Mathf.Deg2Rad, Mathf.Asin(Mathf.Clamp01((w - shoulder) / rs)));
        var xs = rs * Mathf.Sin(theta);
        var yLip = cy - rs * Mathf.Cos(theta);
        cm.SeatKind = FrontRoomsStructuresAssets.CurveTileKind(FrontRoomsStructuresAssets.CurvePitch(rs));
        var slot = Gap + RibW + .012f;
        var sj = s[joint];
        for (var blk = -1; blk <= 1; blk += 2)
        {
            float d0 = blk < 0 ? -half : slot, d1 = blk < 0 ? -slot : half;
            var rings = new List<Ring>();
            for (var k = 0; k <= 4; k++) rings.Add(At(p, right, up, s, sj + Mathf.Lerp(d0, d1, k / 4f)));
            // the section at each ring: its bottom on the support's top (a plane in the district), the rest in the ring's frame
            var secs = new List<Vector2[]>();
            foreach (var g in rings)
            {
                var yb = (bottomY - g.p.y) / Mathf.Max(.2f, g.u.y);
                var pts = new List<Vector2> { new Vector2(-w, yb), new Vector2(w, yb), new Vector2(w, yLip - rb) };
                for (var k = 1; k <= 3; k++) { var a = (90f * k / 3f) * Mathf.Deg2Rad; pts.Add(new Vector2(w - rb + rb * Mathf.Cos(a), yLip - rb + rb * Mathf.Sin(a))); }
                pts.Add(new Vector2(xs, yLip));
                var na = Mathf.Max(6, Mathf.RoundToInt(theta * Mathf.Rad2Deg / 6f));
                for (var k = 1; k < 2 * na; k++)
                {
                    var a = -90f * Mathf.Deg2Rad + theta - 2f * theta * k / (2 * na);
                    pts.Add(new Vector2(rs * Mathf.Cos(a), cy + rs * Mathf.Sin(a)));
                }
                pts.Add(new Vector2(-xs, yLip));
                for (var k = 0; k < 3; k++) { var a = (90f + 90f * k / 3f) * Mathf.Deg2Rad; pts.Add(new Vector2(-w + rb + rb * Mathf.Cos(a), yLip - rb + rb * Mathf.Sin(a))); }
                pts.Add(new Vector2(-w, yLip - rb));
                secs.Add(pts.ToArray());
            }
            var count = secs[0].Length;
            // the faces round the section (the bottom edge 0 -> 1 rests on the support: left out); sides to the body, the rest seat
            var vAcc = new float[count + 1];
            for (var k = 1; k <= count; k++) vAcc[k] = vAcc[k - 1] + Vector2.Distance(secs[0][k - 1], secs[0][k % count]);
            for (var k = 1; k < count; k++)
            {
                var k1 = (k + 1) % count;
                var side = k == 1 || k == count - 1;
                var L = side ? cm.body : cm.seat;
                var e = secs[0][k1] - secs[0][k];
                var n2 = new Vector2(e.y, -e.x).normalized;
                int pa = -1, pb = -1;
                for (var j = 0; j < rings.Count; j++)
                {
                    var g = rings[j];
                    var a = L.Vert(Place(g, secs[j][k]), Dir(g, n2), new Vector2(g.s, vAcc[k]));
                    var b = L.Vert(Place(g, secs[j][k1]), Dir(g, n2), new Vector2(g.s, vAcc[k + 1]));
                    if (j > 0) L.Quad(pa, pb, b, a, Dir(g, n2));
                    pa = a;
                    pb = b;
                }
            }
            // the block's two ends (tile faces: the end at the slot shows the cradle's section)
            for (var e = 0; e < 2; e++)
            {
                var g = rings[e == 0 ? 0 : rings.Count - 1];
                var sec = secs[e == 0 ? 0 : rings.Count - 1];
                var outward = e == 0 ? -g.t : g.t;
                var order = FrontRoomsStructureProfiles.EarClip(sec);
                var first = cm.body.v.Count;
                foreach (var q in sec) cm.body.Vert(Place(g, q), outward, q);
                for (var i = 0; i + 2 < order.Count; i += 3) cm.body.Tri(first + order[i], first + order[i + 1], first + order[i + 2], outward);
            }
            // the pad in this block's seat: 10 mm between the seat and the shell, 12 mm in from the block's ends and the seat's edges
            PadIn(cm.pad, p, right, up, s, sj + d0 + .012f * (blk < 0 ? 1f : 1f), sj + d1 - .012f, shellR, rs, cy, theta - 4f * Mathf.Deg2Rad);
        }
        // the slot's floor between the blocks: low under the ribs (the pair hangs 68 mm under the shell)
        {
            var g0 = At(p, right, up, s, sj - slot);
            var g1 = At(p, right, up, s, sj + slot);
            var yTop = -T - RibH - .012f;
            var y0 = (bottomY - g0.p.y) / Mathf.Max(.2f, g0.u.y);
            var y1 = (bottomY - g1.p.y) / Mathf.Max(.2f, g1.u.y);
            if (yTop > Mathf.Max(y0, y1) + .005f)
            {
                var top = new[] { Place(g0, new Vector2(-w, yTop)), Place(g0, new Vector2(w, yTop)), Place(g1, new Vector2(w, yTop)), Place(g1, new Vector2(-w, yTop)) };
                var upN = (g0.u + g1.u).normalized;
                int a = cm.body.Vert(top[0], upN, new Vector2(sj - slot, -w)), b = cm.body.Vert(top[1], upN, new Vector2(sj - slot, w));
                int c = cm.body.Vert(top[2], upN, new Vector2(sj + slot, w)), d = cm.body.Vert(top[3], upN, new Vector2(sj + slot, -w));
                cm.body.Quad(a, b, c, d, upN);
                for (var sd = -1; sd <= 1; sd += 2)
                {
                    var o = g0.r * sd;
                    int e0 = cm.body.Vert(Place(g0, new Vector2(sd * w, y0)), o, new Vector2(sj - slot, y0)), e1 = cm.body.Vert(Place(g1, new Vector2(sd * w, y1)), o, new Vector2(sj + slot, y1));
                    int e2 = cm.body.Vert(Place(g1, new Vector2(sd * w, yTop)), o, new Vector2(sj + slot, yTop)), e3 = cm.body.Vert(Place(g0, new Vector2(sd * w, yTop)), o, new Vector2(sj - slot, yTop));
                    cm.body.Quad(e0, e1, e2, e3, o);
                }
            }
        }
        return cm;
    }

    /// <summary>The last module's bearing in its tiled bed (review S-6, REC a; D10): a black elastomer pad 0.30 m long and 0.18 m
    /// wide under its round bottom at each line metre in 'at', from the bed's top (district y) up to the shell's outer skin.</summary>
    public static Lists BedPads(IList<Vector3> p, IList<Vector3> right, IList<Vector3> up, IList<float> s, float bedY, params float[] at)
    {
        var L = new Lists();
        if (p == null || at == null) return L;
        const float hw = .09f, hl = .15f;
        const int steps = 6;
        var ro = RB + T;
        foreach (var a in at)
        {
            var g0 = At(p, right, up, s, a - hl);
            var g1 = At(p, right, up, s, a + hl);
            // the section across: flat on the bed, its top the shell's round bottom
            for (var e = 0; e < 2; e++)
            {
                var g = e == 0 ? g0 : g1;
                var outward = e == 0 ? -g.t : g.t;
                var yb = (bedY - g.p.y) / Mathf.Max(.2f, g.u.y);
                int pa = -1, pb = -1;
                for (var k = 0; k <= steps; k++)
                {
                    var x = -hw + 2f * hw * k / steps;
                    var yt = CB - Mathf.Sqrt(ro * ro - x * x);
                    var ia = L.Vert(Place(g, new Vector2(x, yb)), outward, new Vector2(x, yb));
                    var ib = L.Vert(Place(g, new Vector2(x, yt)), outward, new Vector2(x, yt));
                    if (k > 0) L.Quad(pa, pb, ib, ia, outward);
                    pa = ia;
                    pb = ib;
                }
            }
            for (var sd = -1; sd <= 1; sd += 2)
            {
                var yt = CB - Mathf.Sqrt(ro * ro - hw * hw);
                var y0 = (bedY - g0.p.y) / Mathf.Max(.2f, g0.u.y);
                var y1 = (bedY - g1.p.y) / Mathf.Max(.2f, g1.u.y);
                var o = g0.r * sd;
                int i0 = L.Vert(Place(g0, new Vector2(sd * hw, y0)), o, Vector2.zero), i1 = L.Vert(Place(g1, new Vector2(sd * hw, y1)), o, Vector2.right);
                int i2 = L.Vert(Place(g1, new Vector2(sd * hw, yt)), o, Vector2.one), i3 = L.Vert(Place(g0, new Vector2(sd * hw, yt)), o, Vector2.up);
                L.Quad(i0, i1, i2, i3, o);
            }
        }
        return L;
    }

    // A bearing pad: the band between radius rIn (the shell) and rOut (the seat) over +-halfAngle round the bottom, from line
    // metres s0 to s1: its edges and ends (its faces are pressed against the shell and the seat).
    static void PadIn(Lists L, IList<Vector3> p, IList<Vector3> right, IList<Vector3> up, IList<float> s, float s0, float s1, float rIn, float rOut, float cy, float halfAngle)
    {
        var g0 = At(p, right, up, s, s0);
        var g1 = At(p, right, up, s, s1);
        var steps = 8;
        for (var sd = -1; sd <= 1; sd += 2)
        {
            var a = -90f * Mathf.Deg2Rad + sd * halfAngle;
            var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            var o = new Vector2(-d.y, d.x) * sd;
            var c = new Vector2(0f, cy);
            int i0 = L.Vert(Place(g0, c + d * rIn), Dir(g0, o), Vector2.zero), i1 = L.Vert(Place(g1, c + d * rIn), Dir(g1, o), Vector2.right);
            int i2 = L.Vert(Place(g1, c + d * rOut), Dir(g1, o), Vector2.one), i3 = L.Vert(Place(g0, c + d * rOut), Dir(g0, o), Vector2.up);
            L.Quad(i0, i1, i2, i3, Dir(g0, o));
        }
        for (var e = 0; e < 2; e++)
        {
            var g = e == 0 ? g0 : g1;
            var outward = e == 0 ? -g.t : g.t;
            int pa = -1, pb = -1;
            for (var k = 0; k <= steps; k++)
            {
                var a = -90f * Mathf.Deg2Rad - halfAngle + 2f * halfAngle * k / steps;
                var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var c = new Vector2(0f, cy);
                var ia = L.Vert(Place(g, c + d * rIn), outward, new Vector2(k / (float)steps, 0f));
                var ib = L.Vert(Place(g, c + d * rOut), outward, new Vector2(k / (float)steps, 1f));
                if (k > 0) L.Quad(pa, pb, ib, ia, outward);
                pa = ia;
                pb = ib;
            }
        }
    }
}
