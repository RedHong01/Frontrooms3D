// The structures scene's profile library and sweeps (game visual line, structures_assets step 2, package structures_profiles;
// research/structures_assets/20_research.md Part A sections A.2-A.6, Part B sections B.3 F1-F15, B.6; 30_plan.md package P1).
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 2-D sections in metres (x across, + outward from the edge or the axis; y up) and the sweeps that lay them along a path, for
/// scene 4's NEW builder (the staged contract K1 calls them): the handrail tube, the guard tube, the glass shoe and its cap rail,
/// the pool tile nosing L, the carpet bar, the cast abrasive nosing, the steel angle, the T-bar and wall angle, the curb angle,
/// the pour stop, the fascia, the woven-wire channel frame, the cove, the bullnose cap, the three gutter sections, the weir lip,
/// the sealant bead. Dimensions are the period's (1988-91 US practice; the research cites each), never a scale of another.
/// Convention: the solid is on the LEFT as a section is walked (a closed section runs counter-clockwise), so the outward normal
/// is the right-hand normal (dy, -dx). V is metres along the section unless a section maps onto a strip of the structures trim
/// sheet (Prop_StructTrim) or a pool tile piece's unrolled map (Prop_PoolTile_Nosing, _Coping, _Bullnose, _Cove, _Gutter,
/// _WeirLip), whose V the section gives point by point. Sweeps write into plain lists (FrontRoomsStructuresMesh.Append takes
/// them), u = metres along the path from a given start (world-aligned, so tile joints and trim wear stay put).
/// </summary>
public static class FrontRoomsStructureProfiles
{
    /// <summary>A section: points (solid on the left), a crease flag per point, closed or open, V per point.</summary>
    public sealed class Section
    {
        public readonly string Name;
        public readonly Vector2[] P;
        public readonly bool[] Hard;
        public readonly bool Closed;
        public readonly float[] V;

        public Section(string name, Vector2[] p, bool[] hard, bool closed, float[] v = null)
        {
            Name = name;
            P = p;
            Hard = hard ?? new bool[p.Length];
            Closed = closed;
            if (v != null && v.Length == p.Length) V = v;
            else
            {
                V = new float[p.Length];
                for (var i = 1; i < p.Length; i++) V[i] = V[i - 1] + Vector2.Distance(p[i - 1], p[i]);
            }
        }

        /// <summary>The section's V span (for a closed section, back round to the first point).</summary>
        public float Length
        {
            get
            {
                var l = V[V.Length - 1];
                if (Closed) l += Vector2.Distance(P[P.Length - 1], P[0]);
                return l;
            }
        }
    }

    // ---------------------------------------------------------------- the trim sheet (Prop_StructTrim, P3)

    /// <summary>The structures trim sheet's strips, top to bottom, 64 px each on a 2048 x 1024 map (16 strips), 1 m of u across
    /// the map; Tools/lookdev/structures_trim/gen_struct_trim.py draws the same table. Metal per texel (mask B).</summary>
    public enum Strip
    {
        Sealant = 0, ShoeAluminium = 1, PipePaint = 2, AbrasiveNosing = 3, WallAngle = 4, PourStop = 5, FasciaBand = 6, CarpetBar = 7,
        FasciaCap = 8, SteelAngleNosing = 9, BullnoseBlockEnd = 10, CurbAngle = 11, Sawcut = 12, Galvanised = 13, StainlessBrushed = 14, TBarWhite = 15
    }

    public const int StripCount = 16;
    /// <summary>Metres of u across the trim sheet (2048 px: 0.49 mm a texel along a strip).</summary>
    public const float TrimMetres = 1f;

    /// <summary>A strip's V range on the trim sheet (UV, 0 at the bottom), inset half a texel so mips do not bleed.</summary>
    public static Vector2 StripV(Strip s)
    {
        var i = (int)s;
        var v1 = 1f - i / (float)StripCount;
        var v0 = 1f - (i + 1) / (float)StripCount;
        const float Inset = 1.5f / 1024f;
        return new Vector2(v0 + Inset, v1 - Inset);
    }

    // ---------------------------------------------------------------- the library

    /// <summary>A round tube (closed, smooth): r 0.0211 handrails and guard rails (1 1/4 in NPS, 42.2 mm OD), 0.0167 the 1 in
    /// pipe (33.4 mm), 0.0241 the 1 1/2 in pipe (48.3 mm). Segments by size: 12 at 42 mm.</summary>
    public static Section Tube(float r, int segments = 12)
    {
        segments = Mathf.Clamp(segments, 6, 32);
        var p = new Vector2[segments];
        for (var i = 0; i < segments; i++)
        {
            var a = 2f * Mathf.PI * i / segments;
            p[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
        }
        return new Section("tube " + (r * 2000f).ToString("0.0") + " mm", p, null, true);
    }

    /// <summary>Square or rectangular bar, centred, with eased corners (r): pickets 1/2 in square, posts 2 in square tube.</summary>
    public static Section Bar(float w, float h, float r = .001f)
    {
        var list = new List<Vector2>();
        RoundedRect(list, -w * .5f, 0f, w * .5f, h, r, 2);
        for (var i = 0; i < list.Count; i++) list[i] -= new Vector2(0f, h * .5f);
        return new Section("bar", list.ToArray(), null, true);
    }

    /// <summary>
    /// The glass guard's aluminium base shoe (1988 UBC glass guards: structural glass held in a continuous shoe, no posts): outer
    /// 64 W x 105 H, the pocket 24 W (12 mm glass and a 6 mm glazing gasket each side) 80 deep, the lips eased; y 0 on the floor,
    /// x 0 on the glass's plane. Matches Kit_GlassShoe_EndCap (G1-b).
    /// </summary>
    public static Section GlassShoe()
    {
        const float W = .032f, H = .105f, S = .012f, Bot = .025f, E = .003f;
        var p = new[]
        {
            new Vector2(-W, 0f), new Vector2(W, 0f), new Vector2(W, H - E), new Vector2(W - E, H), new Vector2(S + E, H), new Vector2(S, H - E),
            new Vector2(S, Bot), new Vector2(-S, Bot), new Vector2(-S, H - E), new Vector2(-S - E, H), new Vector2(-W + E, H), new Vector2(-W, H - E),
        };
        var hard = new[] { true, true, true, false, false, true, true, true, true, false, false, true };
        return new Section("glass shoe", p, hard, true);
    }

    /// <summary>The glazing gasket in the shoe's pocket each side of the glass (black EPDM, 6 W, its top 2 mm under the lips).</summary>
    public static Section ShoeGasket(float side)
    {
        float x0 = side > 0f ? .006f : -.012f, x1 = side > 0f ? .012f : -.006f;
        var p = new[] { new Vector2(x0, .095f), new Vector2(x1, .095f), new Vector2(x1, .103f), new Vector2(x0, .103f) };
        return new Section("shoe gasket", p, new[] { true, true, true, true }, true);
    }

    /// <summary>The top cap rail over glass: 50 W x 38 H, a half-round top r 25 on a 13 mm cheek, a 16 mm slot (12 mm glass and
    /// 2 mm gaskets) 20 deep from below; y 0 on the glass's top edge. Matches Kit_CapRail_EndCap (G1-b).</summary>
    public static Section CapRail(int segments = 12)
    {
        var list = new List<Vector2>
        {
            new Vector2(-.025f, 0f), new Vector2(-.008f, 0f), new Vector2(-.008f, .02f), new Vector2(.008f, .02f), new Vector2(.008f, 0f), new Vector2(.025f, 0f),
        };
        // the half round from the right cheek over the top to the left cheek (counter-clockwise)
        for (var i = 0; i <= segments; i++)
        {
            var a = Mathf.PI * i / segments;
            list.Add(new Vector2(Mathf.Cos(a) * .025f, .013f + Mathf.Sin(a) * .025f));
        }
        var hard = new bool[list.Count];
        for (var i = 0; i < 6; i++) hard[i] = true;
        hard[6] = true;
        hard[list.Count - 1] = true;
        return new Section("cap rail", list.ToArray(), hard, true);
    }

    /// <summary>
    /// The pool stair nosing L as Prop_PoolTile_Nosing's map has it unrolled (gen_pool_tile.py make_nosing): 50 mm on the tread,
    /// r 8 round, 25 mm down the riser, the piece 3 mm proud of the tread and the riser. Step frame: x 0 on the riser face, + out
    /// over the edge; y 0 on the tread. V: flat tread leg 0-0.042, round 0.042-0.05457, riser leg 0.05457-0.07157 (the map's own),
    /// the two 3 mm lips in the map's joint band.
    /// </summary>
    public static Section PoolNosing(int roundSegments = 4)
    {
        const float T = .003f, R = .008f;
        var list = new List<Vector2>();
        var v = new List<float>();
        // walked with the tile on the left: from the bottom lip of the riser leg, up the riser leg, round the nose, back along
        // the tread leg to its back lip
        list.Add(new Vector2(0f, -.022f)); v.Add(.07457f);
        list.Add(new Vector2(T, -.022f)); v.Add(.07157f);
        list.Add(new Vector2(T, -.005f)); v.Add(.05457f);
        for (var i = 1; i < roundSegments; i++)
        {
            var a = Mathf.PI * .5f * i / roundSegments;   // 0 at the riser side, 90 deg at the tread side
            list.Add(new Vector2(-.005f + Mathf.Cos(a) * R, -.005f + Mathf.Sin(a) * R));
            v.Add(.05457f - .01257f * i / roundSegments);
        }
        list.Add(new Vector2(-.005f, T)); v.Add(.042f);
        list.Add(new Vector2(-.047f, T)); v.Add(0f);
        list.Add(new Vector2(-.047f, 0f)); v.Add(-.003f);
        var hard = new bool[list.Count];
        hard[0] = hard[1] = hard[list.Count - 2] = hard[list.Count - 1] = true;
        return new Section("pool nosing L", list.ToArray(), hard, false, v.ToArray());
    }

    /// <summary>
    /// The aluminium carpet nosing (a stair or ledge edge in a carpeted room, Level 0 and the Office): a ribbed top leg 38 mm on
    /// the carpet, r 4 nose, a 19 mm leg down the face, 3 mm proud; step frame as PoolNosing. V on the trim sheet's CarpetBar.
    /// </summary>
    public static Section CarpetBar(int roundSegments = 3)
    {
        const float T = .003f, R = .004f;
        var list = new List<Vector2> { new Vector2(0f, -.019f), new Vector2(T, -.019f), new Vector2(T, -.001f) };
        for (var i = 1; i < roundSegments; i++)
        {
            var a = Mathf.PI * .5f * i / roundSegments;
            list.Add(new Vector2(-.001f + Mathf.Cos(a) * R, -.001f + Mathf.Sin(a) * R));
        }
        list.Add(new Vector2(-.001f, T));
        list.Add(new Vector2(-.038f, T));
        list.Add(new Vector2(-.040f, 0f));
        var hard = new bool[list.Count];
        hard[0] = hard[1] = hard[list.Count - 2] = hard[list.Count - 1] = true;
        return new Section("carpet bar", list.ToArray(), hard, false);
    }

    /// <summary>A cast abrasive stair nosing (cores and service stairs): 76 W (3 in) on the tread, flush to 1.5 mm proud, eased
    /// edges, a 22 mm return down the riser; step frame as PoolNosing. V on the trim sheet's AbrasiveNosing.</summary>
    public static Section AbrasiveNosing()
    {
        const float T = .0015f;
        var p = new[]
        {
            new Vector2(0f, -.022f), new Vector2(T, -.022f), new Vector2(T, -.002f), new Vector2(-.001f, T), new Vector2(-.074f, T), new Vector2(-.076f, 0f),
        };
        return new Section("abrasive nosing", p, new[] { true, true, false, false, true, true }, false);
    }

    /// <summary>A steel angle (legs a along x, b along y, thickness t), its heel at the origin, legs toward -x and -y (a slab edge's
    /// steel angle nosing, the dock's curb angle, a pour stop): closed, root fillet r t.</summary>
    public static Section Angle(float a, float b, float t)
    {
        var p = new[]
        {
            new Vector2(0f, 0f), new Vector2(0f, -b), new Vector2(-t, -b), new Vector2(-t, -t * 1.6f), new Vector2(-t * 1.6f, -t), new Vector2(-a, -t), new Vector2(-a, 0f),
        };
        System.Array.Reverse(p);   // counter-clockwise
        return new Section("angle", p, new[] { true, true, false, false, true, true, true }, true);
    }

    /// <summary>The ceiling grid's main tee (15/16 in face, 1 1/2 in web with its bulb), face down at y 0, centred.</summary>
    public static Section TBar()
    {
        const float F = .0119f, W = .0381f;
        var p = new[]
        {
            new Vector2(-F, 0f), new Vector2(F, 0f), new Vector2(F, .0025f), new Vector2(.001f, .0025f), new Vector2(.001f, W - .006f), new Vector2(.003f, W - .005f),
            new Vector2(.003f, W), new Vector2(-.003f, W), new Vector2(-.003f, W - .005f), new Vector2(-.001f, W - .006f), new Vector2(-.001f, .0025f), new Vector2(-F, .0025f),
        };
        var hard = new bool[p.Length];
        for (var i = 0; i < hard.Length; i++) hard[i] = true;
        return new Section("t-bar", p, hard, true);
    }

    /// <summary>The ceiling's wall angle (7/8 x 7/8 in), its heel in the wall-ceiling corner at the origin, legs out (+x) and down.</summary>
    public static Section WallAngle()
    {
        const float A = .0222f, T = .0008f;
        var p = new[] { new Vector2(0f, 0f), new Vector2(A, 0f), new Vector2(A, -T), new Vector2(T, -T), new Vector2(T, -A), new Vector2(0f, -A) };
        System.Array.Reverse(p);   // counter-clockwise
        return new Section("wall angle", p, new[] { true, true, true, true, true, true }, true);
    }

    /// <summary>An I-beam (depth d, flange width b, flange tf, web tw), centred on the web, top flange's top at y 0.</summary>
    public static Section IBeam(float d, float b, float tf, float tw)
    {
        float hb = b * .5f, hw = tw * .5f;
        var p = new[]
        {
            new Vector2(-hb, -d), new Vector2(hb, -d), new Vector2(hb, -d + tf), new Vector2(hw, -d + tf), new Vector2(hw, -tf), new Vector2(hb, -tf),
            new Vector2(hb, 0f), new Vector2(-hb, 0f), new Vector2(-hb, -tf), new Vector2(-hw, -tf), new Vector2(-hw, -d + tf), new Vector2(-hb, -d + tf),
        };
        var hard = new bool[p.Length];
        for (var i = 0; i < hard.Length; i++) hard[i] = true;
        return new Section("i-beam", p, hard, true);
    }

    /// <summary>A fascia band (height h, thickness t) on a slab edge with a cap lip on top: the Office's 1.0 m fascia or a papered
    /// Level 0 fascia; its face at x 0, top at y 0, solid behind (-x).</summary>
    public static Section Fascia(float h, float t, float lip = .012f)
    {
        var p = new[]
        {
            new Vector2(-t, -h), new Vector2(0f, -h), new Vector2(0f, -lip), new Vector2(.004f, -lip), new Vector2(.004f, 0f), new Vector2(-t, 0f),
        };
        return new Section("fascia", p, new[] { true, true, true, true, true, true }, true);
    }

    /// <summary>The woven-wire panel's channel frame (1 x 1/2 in, 1/8 in wall), opening toward the mesh (+x), centred on the frame
    /// line.</summary>
    public static Section Channel()
    {
        const float H = .0127f, D = .0254f, T = .0032f;
        var p = new[]
        {
            new Vector2(-H * .5f, -D * .5f), new Vector2(H * .5f, -D * .5f), new Vector2(H * .5f, -D * .5f + T), new Vector2(-H * .5f + T, -D * .5f + T),
            new Vector2(-H * .5f + T, D * .5f - T), new Vector2(H * .5f, D * .5f - T), new Vector2(H * .5f, D * .5f), new Vector2(-H * .5f, D * .5f),
        };
        var hard = new bool[p.Length];
        for (var i = 0; i < hard.Length; i++) hard[i] = true;
        return new Section("channel", p, hard, true);
    }

    /// <summary>A cove (tile cove base r 25 at a wall's foot; the pool's cove r 50 at a basin's floor): the concave quarter round
    /// from the floor (x = r) up to the wall (y = r), with a flat return of 'flat' on each; wall at x 0, floor at y 0.</summary>
    public static Section Cove(float r, float flat = .0f, int segments = 4)
    {
        var list = new List<Vector2>();
        var v = new List<float>();
        if (flat > 0f) { list.Add(new Vector2(r + flat, 0f)); v.Add(0f); }
        for (var i = 0; i <= segments; i++)
        {
            var a = Mathf.PI * .5f * i / segments;           // 0 at the floor, 90 deg at the wall
            list.Add(new Vector2(r - Mathf.Sin(a) * r, r - Mathf.Cos(a) * r));
            v.Add(flat + r * a);                              // arc length, as Prop_PoolTile_Cove's map unrolls it
        }
        if (flat > 0f) { list.Add(new Vector2(0f, r + flat)); v.Add(flat + r * Mathf.PI * .5f + flat); }
        // the solid is the wall's foot: walking from the floor to the wall it lies on the left
        return new Section("cove r " + (r * 1000f).ToString("0"), list.ToArray(), null, false, v.ToArray());
    }

    /// <summary>The outside-corner bullnose trim (Prop_PoolTile_Bullnose's map): a 142 mm leg down the face (x 0, facing +x),
    /// an r 8 round, a 142 mm leg on the top (y 0, facing +y); the arris at the origin. V 0-0.142 face, round, 0.15457-0.29657 top.</summary>
    public static Section BullnoseCorner(int roundSegments = 4)
    {
        const float R = .008f, Leg = .142f;
        var list = new List<Vector2> { new Vector2(0f, -R - Leg), new Vector2(0f, -R) };
        var v = new List<float> { 0f, Leg };
        for (var i = 1; i < roundSegments; i++)
        {
            var a = Mathf.PI * .5f * i / roundSegments;
            list.Add(new Vector2(-R + Mathf.Cos(a) * R, -R + Mathf.Sin(a) * R));
            v.Add(Leg + R * a);
        }
        list.Add(new Vector2(-R, 0f)); v.Add(Leg + R * Mathf.PI * .5f);
        list.Add(new Vector2(-R - Leg, 0f)); v.Add(Leg + R * Mathf.PI * .5f + Leg);
        var hard = new bool[list.Count];
        hard[0] = hard[list.Count - 1] = true;
        return new Section("bullnose corner", list.ToArray(), hard, false, v.ToArray());
    }

    /// <summary>A sealant bead (w wide, tooled concave 'depth' at its middle) in a joint along a corner or across a slab; centred,
    /// its edges at y 0. A movement joint laid over a finished surface lifts its path by a little more than the depth.</summary>
    public static Section Sealant(float w, float depth = .0015f)
    {
        var p = new[] { new Vector2(w * .5f, 0f), new Vector2(w * .25f, -depth), new Vector2(-w * .25f, -depth), new Vector2(-w * .5f, 0f) };
        return new Section("sealant", p, new[] { true, false, false, true }, false);
    }

    /// <summary>
    /// The three period gutter sections (Part A, A.2.3; TM 5-662; Counsilman-Hunsaker), water side toward -x, deck at y 0:
    /// RollOut (a tiled lip 50 mm over the deck at the water, the grate 0.20 behind it), RimFlow (the water to the deck's level,
    /// a 0.20 grate at the edge), Recessed (a channel in the pool wall under a coping, its lip a handhold, 0.30 under the deck).
    /// Open sections: the visible surface of the edge, V in metres (Prop_PoolTile_Gutter's unrolled map).
    /// </summary>
    public enum Gutter { RollOut = 0, RimFlow = 1, Recessed = 2 }

    public static Section GutterSection(Gutter kind)
    {
        Vector2[] p;
        switch (kind)
        {
            case Gutter.RimFlow:
                p = new[]
                {
                    new Vector2(-.02f, -.012f), new Vector2(-.02f, 0f), new Vector2(0f, 0f), new Vector2(0f, -.06f), new Vector2(.20f, -.06f), new Vector2(.20f, 0f), new Vector2(.40f, 0f),
                };
                break;
            case Gutter.Recessed:
                p = new[]
                {
                    new Vector2(-.02f, -.45f), new Vector2(-.02f, -.30f), new Vector2(.04f, -.30f), new Vector2(.04f, -.36f), new Vector2(.18f, -.36f), new Vector2(.18f, -.16f),
                    new Vector2(.03f, -.16f), new Vector2(.03f, -.12f), new Vector2(.22f, -.12f), new Vector2(.22f, 0f), new Vector2(.45f, 0f),
                };
                break;
            default:
            {
                // Prop_PoolTile_Gutter's section, walked from the deck: deck 0.20, the flush finger grate 0.20 (over a channel), the
                // curb's back 0.05 up, its top 0.069 to an r 15 bullnose at the water, the pool face 0.15 down (V as the map)
                const float R = .015f;
                var list = new List<Vector2> { new Vector2(.45f, 0f), new Vector2(.25f, 0f), new Vector2(.05f, 0f), new Vector2(.05f, .05f), new Vector2(-.019f, .05f) };
                var v = new List<float> { 0f, .20f, .40f, .45f, .519f };
                for (var i = 1; i <= 3; i++)
                {
                    var a = Mathf.PI * .5f * i / 3f;
                    list.Add(new Vector2(-.019f - Mathf.Sin(a) * R, .035f + Mathf.Cos(a) * R));
                    v.Add(.519f + R * a);
                }
                list.Add(new Vector2(-.034f, -.115f)); v.Add(.519f + R * Mathf.PI * .5f + .15f);
                var hardR = new bool[list.Count];
                hardR[0] = hardR[1] = hardR[2] = hardR[3] = hardR[4] = hardR[list.Count - 1] = true;
                hardR[4] = false;
                return new Section("gutter " + kind, list.ToArray(), hardR, false, v.ToArray());
            }
        }
        // walked from the deck to the water with the edge's body on the left
        System.Array.Reverse(p);
        var hard = new bool[p.Length];
        for (var i = 0; i < hard.Length; i++) hard[i] = true;
        return new Section("gutter " + kind, p, hard, false);
    }

    /// <summary>A square porcelain weir lip with a drip groove under its nose (Part A, A.6): the water leaves at x 0 going +x, the
    /// lip's top at y 0, 60 deep, the groove 6 x 6 mm 15 mm back under the nose.</summary>
    public static Section WeirLip()
    {
        var p = new[]
        {
            new Vector2(-.15f, 0f), new Vector2(0f, 0f), new Vector2(0f, -.06f), new Vector2(-.009f, -.06f), new Vector2(-.009f, -.054f), new Vector2(-.015f, -.054f),
            new Vector2(-.015f, -.06f), new Vector2(-.05f, -.06f),
        };
        // walked with the lip's body on the left: reverse
        System.Array.Reverse(p);
        var hard = new bool[p.Length];
        for (var i = 0; i < hard.Length; i++) hard[i] = true;
        return new Section("weir lip", p, hard, false);
    }

    // ---------------------------------------------------------------- helpers

    static void RoundedRect(List<Vector2> list, float x0, float y0, float x1, float y1, float r, int seg)
    {
        r = Mathf.Min(r, Mathf.Min(x1 - x0, y1 - y0) * .45f);
        var corners = new[] { new Vector2(x1 - r, y0 + r), new Vector2(x1 - r, y1 - r), new Vector2(x0 + r, y1 - r), new Vector2(x0 + r, y0 + r) };
        var start = new[] { -90f, 0f, 90f, 180f };
        for (var c = 0; c < 4; c++)
            for (var i = 0; i <= seg; i++)
            {
                var a = (start[c] + 90f * i / seg) * Mathf.Deg2Rad;
                list.Add(corners[c] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
    }

    // ---------------------------------------------------------------- sweeps

    /// <summary>How a sweep lays its section and its texture.</summary>
    public struct SweepOptions
    {
        /// <summary>Texture u at the path's start (metres along; pass the world projection of the start for world-aligned joints).</summary>
        public float u0;
        /// <summary>u per metre (1: metres; 1 / TrimMetres on the trim sheet).</summary>
        public float uScale;
        /// <summary>Plumb sections (x horizontal across the path's plan direction, y world up): stringers, shoes, channels on a
        /// slope; false: the section stands square to the path (tubes, rails).</summary>
        public bool plumb;
        /// <summary>Close the ends of a closed section.</summary>
        public bool capStart, capEnd;
        /// <summary>The section's V mapped onto this range (a trim sheet strip); zero: the section's own V (metres).</summary>
        public Vector2 vRange;
        /// <summary>Mirror the section across x (the other hand).</summary>
        public bool mirror;
        /// <summary>Joints sharper than this (degrees) keep a crease (a mitre seam); softer ones are smooth.</summary>
        public float creaseDeg;

        public static SweepOptions Default
        {
            get { return new SweepOptions { u0 = 0f, uScale = 1f, plumb = false, capStart = false, capEnd = false, vRange = Vector2.zero, mirror = false, creaseDeg = 25f }; }
        }
    }

    /// <summary>
    /// Sweeps a section along a path (root-space points; up the section's +y where the path allows, else 'side' gives the frame)
    /// with mitred joints, into the lists (vertices, normals, UVs, triangles; indices continue the lists). Returns the triangles
    /// written. Triangles face along the section's outward normal (Unity's front face).
    /// </summary>
    public static int Sweep(List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs, List<int> tris, IList<Vector3> path, Vector3 up, Section s, SweepOptions o)
    {
        if (path == null || path.Count < 2 || s == null || s.P.Length < 2) return 0;
        var start = tris.Count;
        var m = path.Count;
        var np = s.P.Length;
        // the segments' frames
        var segT = new Vector3[m - 1];
        var segR = new Vector3[m - 1];
        var segU = new Vector3[m - 1];
        for (var j = 0; j < m - 1; j++)
        {
            var t = path[j + 1] - path[j];
            if (t.sqrMagnitude < 1e-12f) t = j > 0 ? segT[j - 1] : Vector3.forward;
            t.Normalize();
            segT[j] = t;
            Vector3 r, u;
            Frame(t, up, o.plumb, out r, out u);
            segR[j] = r;
            segU[j] = u;
        }
        // the section's 2-D normals per point and side (a crease point has one per adjacent segment)
        var mirror = o.mirror ? -1f : 1f;
        var p2 = new Vector2[np];
        for (var k = 0; k < np; k++) p2[k] = new Vector2(s.P[k].x * mirror, s.P[k].y);
        var segs = s.Closed ? np : np - 1;
        var edgeN = new Vector2[segs];
        for (var k = 0; k < segs; k++)
        {
            var a = p2[k];
            var b = p2[(k + 1) % np];
            var d = b - a;
            var nn = new Vector2(d.y, -d.x);
            if (o.mirror) nn = -nn;   // mirroring flips the walk's handedness
            edgeN[k] = nn.sqrMagnitude > 1e-14f ? nn.normalized : Vector2.up;
        }
        // the path's arc length (u), at the centreline
        var along = new float[m];
        for (var j = 1; j < m; j++) along[j] = along[j - 1] + Vector3.Distance(path[j - 1], path[j]);
        var vSpan = s.Length;
        // rings of vertices: for each path point, for each section edge, two vertices (its start and end) so creases can split;
        // smooth points share an averaged normal across the two edges that meet there
        var ringStart = new int[m];
        var crease = new bool[m];
        for (var j = 1; j < m - 1; j++)
            crease[j] = Vector3.Angle(segT[j - 1], segT[j]) > o.creaseDeg;
        for (var j = 0; j < m; j++)
        {
            ringStart[j] = verts.Count;
            // the frame at this point: a segment's, or the mitre between two
            var jb = Mathf.Max(0, j - 1);
            var ja = Mathf.Min(m - 2, j);
            for (var side = 0; side < (crease[j] ? 2 : 1); side++)
            {
                for (var k = 0; k < segs; k++)
                {
                    for (var e = 0; e < 2; e++)
                    {
                        var idx = e == 0 ? k : (k + 1) % np;
                        var q = p2[idx];
                        Vector3 pos, nrm;
                        Place(path, segT, segR, segU, j, jb, ja, crease[j] ? side : -1, q, out pos);
                        // normal: the edge's own, or averaged with the neighbour at a smooth section point
                        var n2 = edgeN[k];
                        var hard = s.Hard[idx];
                        if (!hard)
                        {
                            var other = e == 0 ? (k - 1 + segs) % segs : (k + 1) % segs;
                            var valid = s.Closed || (e == 0 ? k > 0 : k < segs - 1);
                            if (valid) n2 = (n2 + edgeN[other]).normalized;
                        }
                        var fr = crease[j] ? (side == 0 ? jb : ja) : -1;
                        if (fr >= 0) nrm = segR[fr] * n2.x + segU[fr] * n2.y;
                        else nrm = ((segR[jb] + segR[ja]) * n2.x + (segU[jb] + segU[ja]) * n2.y);
                        verts.Add(pos);
                        normals.Add(nrm.normalized);
                        var v = idx == 0 && e == 1 && s.Closed ? vSpan : s.V[idx];
                        if (o.vRange.sqrMagnitude > 0f) v = Mathf.Lerp(o.vRange.x, o.vRange.y, vSpan > 1e-6f ? Mathf.Clamp01(v / vSpan) : 0f);
                        uvs.Add(new Vector2(o.u0 + along[j] * o.uScale, v));
                    }
                }
            }
        }
        // quads between consecutive rings
        var ringSize = segs * 2;
        for (var j = 0; j < m - 1; j++)
        {
            var a0 = ringStart[j] + (crease[j] ? ringSize : 0);   // leave a crease point on its second (outgoing) copy
            var b0 = ringStart[j + 1];                              // arrive at the next point on its first (incoming) copy
            for (var k = 0; k < segs; k++)
            {
                int i0 = a0 + k * 2, i1 = a0 + k * 2 + 1, i2 = b0 + k * 2 + 1, i3 = b0 + k * 2;
                var outward = normals[i0] + normals[i1] + normals[i2] + normals[i3];
                Tri(verts, tris, i0, i1, i2, outward);
                Tri(verts, tris, i0, i2, i3, outward);
            }
        }
        if (s.Closed && o.capStart) Cap(verts, normals, uvs, tris, path, segT, segR, segU, p2, 0, -segT[0], o);
        if (s.Closed && o.capEnd) Cap(verts, normals, uvs, tris, path, segT, segR, segU, p2, m - 1, segT[m - 2], o);
        return (tris.Count - start) / 3;
    }

    static void Frame(Vector3 t, Vector3 up, bool plumb, out Vector3 r, out Vector3 u)
    {
        if (plumb)
        {
            var flat = new Vector3(t.x, 0f, t.z);
            if (flat.sqrMagnitude < 1e-8f) flat = Vector3.forward;
            flat.Normalize();
            r = Vector3.Cross(Vector3.up, flat).normalized;
            u = Vector3.up;
            return;
        }
        var hint = up.sqrMagnitude > 1e-8f ? up.normalized : Vector3.up;
        if (Mathf.Abs(Vector3.Dot(hint, t)) > .999f) hint = Mathf.Abs(t.y) < .9f ? Vector3.up : Vector3.forward;
        r = Vector3.Cross(hint, t).normalized;
        u = Vector3.Cross(t, r).normalized;
    }

    // A section point at path point j: on a segment's frame at the path's ends; at a joint on the mitre plane (the bisector of the
    // two segments), reached along the incoming segment from its own frame (the outgoing one meets the same point). Plumb frames
    // and frames from one 'up' agree at a joint, so both sides of a crease land on the same point.
    static void Place(IList<Vector3> path, Vector3[] segT, Vector3[] segR, Vector3[] segU, int j, int jb, int ja, int side, Vector2 q, out Vector3 pos)
    {
        var m = path.Count;
        if (j == 0) { pos = path[0] + segR[0] * q.x + segU[0] * q.y; return; }
        if (j == m - 1) { pos = path[m - 1] + segR[m - 2] * q.x + segU[m - 2] * q.y; return; }
        var tin = segT[jb];
        var tout = segT[ja];
        var bis = tin + tout;
        if (bis.sqrMagnitude < 1e-8f) bis = tin;
        bis.Normalize();
        // from the incoming frame: the offset point, slid along the incoming direction onto the mitre plane through path[j]
        var off = path[j] + segR[jb] * q.x + segU[jb] * q.y;
        var den = Vector3.Dot(tin, bis);
        var sl = Mathf.Abs(den) > 1e-4f ? Vector3.Dot(path[j] - off, bis) / den : 0f;
        pos = off + tin * sl;
    }

    static void Cap(List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs, List<int> tris, IList<Vector3> path, Vector3[] segT, Vector3[] segR, Vector3[] segU,
        Vector2[] p2, int j, Vector3 outward, SweepOptions o)
    {
        var fr = Mathf.Clamp(j == 0 ? 0 : j - 1, 0, segT.Length - 1);
        var first = verts.Count;
        for (var k = 0; k < p2.Length; k++)
        {
            verts.Add(path[j] + segR[fr] * p2[k].x + segU[fr] * p2[k].y);
            normals.Add(outward.normalized);
            uvs.Add(new Vector2(o.u0 + p2[k].x, p2[k].y));
        }
        var order = EarClip(p2);
        for (var i = 0; i + 2 < order.Count; i += 3) Tri(verts, tris, first + order[i], first + order[i + 1], first + order[i + 2], outward);
    }

    /// <summary>Ear-clipping triangulation of a simple polygon (either winding); returns index triples.</summary>
    public static List<int> EarClip(Vector2[] poly)
    {
        var result = new List<int>();
        var n = poly.Length;
        if (n < 3) return result;
        var idx = new List<int>(n);
        for (var i = 0; i < n; i++) idx.Add(i);
        var area = 0f;
        for (var i = 0; i < n; i++) area += poly[i].x * poly[(i + 1) % n].y - poly[(i + 1) % n].x * poly[i].y;
        var ccw = area > 0f;
        var guard = 0;
        while (idx.Count > 3 && guard++ < 4 * n)
        {
            var clipped = false;
            for (var i = 0; i < idx.Count; i++)
            {
                int ia = idx[(i - 1 + idx.Count) % idx.Count], ib = idx[i], ic = idx[(i + 1) % idx.Count];
                Vector2 a = poly[ia], b = poly[ib], c = poly[ic];
                var cross = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
                if (ccw ? cross <= 1e-12f : cross >= -1e-12f) continue;
                var inside = false;
                for (var q = 0; q < idx.Count && !inside; q++)
                {
                    var iq = idx[q];
                    if (iq == ia || iq == ib || iq == ic) continue;
                    inside = InTri(poly[iq], a, b, c);
                }
                if (inside) continue;
                result.Add(ia); result.Add(ib); result.Add(ic);
                idx.RemoveAt(i);
                clipped = true;
                break;
            }
            if (!clipped) break;
        }
        if (idx.Count == 3) { result.Add(idx[0]); result.Add(idx[1]); result.Add(idx[2]); }
        return result;
    }

    static bool InTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        var d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
        var d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
        var d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
        var neg = d1 < 0f || d2 < 0f || d3 < 0f;
        var pos = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(neg && pos);
    }

    static void Tri(List<Vector3> verts, List<int> tris, int i0, int i1, int i2, Vector3 outward)
    {
        var c = Vector3.Cross(verts[i1] - verts[i0], verts[i2] - verts[i0]);
        if (c.sqrMagnitude < 1e-18f) return;
        if (Vector3.Dot(c, outward) >= 0f) { tris.Add(i0); tris.Add(i1); tris.Add(i2); }
        else { tris.Add(i0); tris.Add(i2); tris.Add(i1); }
    }

    /// <summary>A straight run: the two-point path from a to b (root space).</summary>
    public static int Line(List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs, List<int> tris, Vector3 a, Vector3 b, Vector3 up, Section s, SweepOptions o)
    {
        return Sweep(verts, normals, uvs, tris, new[] { a, b }, up, s, o);
    }

    /// <summary>The world-aligned u at a path's start: its world position projected on the path's first direction (metres), so
    /// a texture's joints along a straight run keep their world places as the builder's planar UVs do.</summary>
    public static float WorldU(Vector3 worldStart, Vector3 direction)
    {
        var d = direction;
        d.y = 0f;
        if (d.sqrMagnitude < 1e-8f) return worldStart.y;
        return Vector3.Dot(worldStart, d.normalized);
    }
}
