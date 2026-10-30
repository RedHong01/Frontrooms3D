using UnityEngine;

/// <summary>
/// FrontRoomsKeyHud's curves: a pure port of graphic design's motion lab
/// (~/FrontRoomsVisualWork/keyicon_motion/lab_template_v2.html, snapshot 2026-10-14 17:24,
/// md5 cc606573da607768ac1130c5318fc891; line numbers below are that file's).
/// Every value here is in the LAB's convention: 1080p px, SVG y DOWN, rotations in degrees
/// CLOCKWISE on screen. FrontRoomsKeyHud flips them for Unity (y up, z-rotation = -deg).
/// No scene state, no allocation: the harness diffs these 1:1 against the lab's own JS.
/// </summary>
public static class FrontRoomsKeyHudMotion
{
    // ------------------------------------------------------------------ easings (lab 190-196)
    public static float Clamp01(float x) { return x < 0f ? 0f : x > 1f ? 1f : x; }
    public static float Lerp(float a, float b, float t) { return a + (b - a) * t; }
    public static float Eoc(float t) { var u = 1f - t; return 1f - u * u * u; }
    public static float Eio(float t) { if (t < .5f) return 4f * t * t * t; var u = -2f * t + 2f; return 1f - u * u * u / 2f; }
    public static float Eob(float t, float s = 1.7f) { var u = t - 1f; return 1f + (s + 1f) * u * u * u + s * u * u; }
    public static float SpringStep(float t, float z, float f)
    {
        var w = 2f * Mathf.PI * f;
        var wd = w * Mathf.Sqrt(1f - z * z);
        return 1f - Mathf.Exp(-z * w * t) * (Mathf.Cos(wd * t) + z * w / wd * Mathf.Sin(wd * t));
    }

    /// <summary>A rounded rect size: w x h, corner radius r (lab lerpR objects {w, h, rx}).</summary>
    public struct RR
    {
        public float w, h, r;
        public RR(float w, float h, float r) { this.w = w; this.h = h; this.r = r; }
        public static RR Lerp(RR a, RR b, float t) { return new RR(a.w + (b.w - a.w) * t, a.h + (b.h - a.h) * t, a.r + (b.r - a.r) * t); }
    }

    public enum Shape { Rect = 0, Round = 1, Long = 2 }

    // ------------------------------------------------------------------ geometry (lab 179-186, lockGeo 201-206)
    public const float SW = 2.6f, VIS = 4f, KL = 1f;
    public static readonly RR Dot = new RR(16f, 16f, 8f);
    static readonly RR IrisStart = new RR(14f, 14f, 7f);

    /// <summary>The padlock's geometry for one tag shape (lab TAG + SHACKLE + lockGeo).</summary>
    public struct Geo
    {
        public Shape shape;
        public bool round;
        public RR rim, body, ins;
        /// <summary>Shackle centreline half-width a.</summary>
        public float a;
        /// <summary>lab G.top: the rim top where the legs meet it, SVG (negative = up).</summary>
        public float top;
        /// <summary>lab G.yc = top - VIS: the arc's centre line, SVG.</summary>
        public float yc;
        /// <summary>lab G.hide = a + VIS + SW/2 + 1.5 (= a + 6.8).</summary>
        public float hide;
        /// <summary>Insert centre in the 72 x 48 B glyph (lab TAG c).</summary>
        public Vector2 c;
    }

    public static Geo GeoOf(Shape shape)
    {
        // TAG (lab 179-183): rim [x, y, w, h, r], ins [x, y, w, h, r], c.
        float rw, rh, rr, iw, ih, ir, cx, cy, a;
        switch (shape)
        {
            case Shape.Long: rw = 52.44f; rh = 15.18f; rr = 3.45f; iw = 34.5f; ih = 9.66f; ir = 1.035f; cx = 27.22f; cy = 28.738f; a = 10.5f; break;
            case Shape.Round: rw = 26.22f; rh = 26.22f; rr = 13.11f; iw = 17.94f; ih = 17.94f; ir = 8.97f; cx = 27.22f; cy = 34.258f; a = 6.5f; break;
            default: rw = 39.33f; rh = 20.01f; rr = 3.45f; iw = 27.6f; ih = 13.11f; ir = 1.035f; cx = 27.22f; cy = 31.153f; a = 8.5f; break;
        }
        var g = new Geo();
        g.shape = shape;
        g.round = shape == Shape.Round;
        g.rim = new RR(rw, rh, rr);
        g.body = new RR(rw - 2f, rh - 2f, rr - 1f);
        g.ins = new RR(iw, ih, ir);
        g.a = a;
        g.top = g.round ? -Mathf.Sqrt(rr * rr - a * a) : -rh / 2f;
        g.yc = g.top - VIS;
        g.hide = a + VIS + SW / 2f + 1.5f;
        g.c = new Vector2(cx, cy);
        return g;
    }

    // ------------------------------------------------------------------ the padlock's pose (lab closed(), 256)
    /// <summary>
    /// One frame of the padlock, lab convention (P in the lab): x, y the whole lock's offset (y down), rot
    /// degrees clockwise about rp, sc, op; body and insert sizes; shY the shackle's offset (negative = up);
    /// shK the swing (1 closed, -1 open); reveal the typed digits (0, .5 = first, 1 = both); dot the
    /// crosshair dot's alpha; tx the prompt lines' x offset.
    /// </summary>
    public struct LockPose
    {
        public float x, y, tx, rot, rpx, rpy, sc, op;
        public RR body, ins;
        public float shY, shK, reveal, dot;
    }

    public static LockPose Closed(Geo g)
    {
        var p = new LockPose();
        p.sc = 1f; p.op = 1f;
        p.body = g.body; p.ins = g.ins;
        p.shK = 1f; p.reveal = 1f; p.dot = 0f;
        return p;
    }

    /// <summary>lab 357 type2(u, a, b).</summary>
    public static float Type2(float u, float a, float b) { return u < a ? 0f : u < b ? .5f : 1f; }

    /// <summary>L3.D in (lab 414-417), u = 0..1 over 0.5 s.</summary>
    public static LockPose LockIn(Geo g, float u)
    {
        var p = Closed(g);
        var body = RR.Lerp(IrisStart, g.body, Eoc(Clamp01(u / .3f)));
        p.body = body;
        p.ins = RR.Lerp(body, g.ins, Eio(Clamp01((u - .3f) / .35f)));
        p.shY = g.hide * (1f - Eob(Clamp01((u - .62f) / .38f)));
        p.reveal = Type2(u, .8f, .9f);
        return p;
    }
    public const float LockInSeconds = .5f, LockOutSeconds = .32f;

    // ---- D.3 DENY.tugShake (lab 365-371), r = seconds since E, window 0.5 s
    public static float Tug(float s)
    {
        if (s < 0f || s > .2f) return 0f;
        if (s < .05f) { var q = s / .05f; return 2.5f * q * q; }
        if (s < .07f) return 2.5f;
        if (s < .12f) { var q = (s - .07f) / .05f; return 2.5f * (1f - q * q); }
        return -.6f * Mathf.Sin(Mathf.PI * (s - .12f) / .08f);
    }
    public static float Recoil(float s) { return s < .05f || s > .11f ? 0f : .8f * (1f - (s - .05f) / .06f); }
    public static float Shake(float s, float d, float amp = 3.5f, float f = 8f)
    {
        if (s < 0f || s > d) return 0f;
        var q = 1f - s / d;
        return amp * Mathf.Sin(2f * Mathf.PI * f * s) * q * q;
    }
    public const float DenySeconds = .5f;
    public static void DenyTugShake(ref LockPose p, float r)
    {
        p.shY = -Tug(r);
        p.y = Recoil(r);
        p.x = p.tx = Shake(r - .14f, .36f);
    }

    // ---- has key: swingOpen (lab 356), r = seconds since the turn
    public static void SwingOpen(ref LockPose p, float r)
    {
        p.shY = -5f * Eoc(Clamp01(r / .15f));
        p.shK = Mathf.Cos(Mathf.PI * Eio(Clamp01((r - .12f) / .3f)));
    }
    /// <summary>Prompt alpha while it opens (lab 982): 1 - clamp(r / 0.12).</summary>
    public static float OpenText(float r) { return 1f - Clamp01(r / .12f); }
    /// <summary>The swing ends at react 0.5, a 0.06 s beat, then the fall (lab 463-464: tOut = tPress + .5 + .06).</summary>
    public const float OpenReact = .5f, OpenBeat = .06f;

    // ---- F.1 FALL.drop (lab 446), f = seconds into the fall, fallDur 0.55 (lab 462)
    public const float FallSeconds = .55f;
    public static void FallDrop(ref LockPose p, float f)
    {
        p.y = .5f * 560f * f * f;
        p.rot = 10f * Eio(Clamp01(f / .5f));
        p.op = 1f - Clamp01((f - .15f) / .35f);
        p.dot = Clamp01((f - .25f) / .25f);
    }

    /// <summary>fallOut for 'open' (lab 461): the open pose held (open(G, P, 99)), then the drop.</summary>
    public static LockPose OpenFall(Geo g, float f)
    {
        var p = Closed(g);
        SwingOpen(ref p, 99f);
        FallDrop(ref p, f);
        return p;
    }

    /// <summary>famOut for 'deny' (lab 359-363): the build run backwards, u = 0..1 over 0.32 s.</summary>
    public static LockPose FamOut(Geo g, float u) { return LockIn(g, 1f - u); }
    /// <summary>Prompt alpha while leaving after no key (lab 983): 1 - clamp(u / 0.6).</summary>
    public static float FamOutText(float u) { return 1f - Clamp01(u / .6f); }

    /// <summary>The shackle centreline (lab shacklePath, 216-219) as a polyline in LOCK coordinates with
    /// UNITY y up: long leg (-a, T-6) to (-a, T+4), the 48-segment half ellipse with radii (a|k|, a) over
    /// the top, the free leg down to (xr, T-3), xr = -a + 2ak. Writes 51 points; returns the count.
    /// shOffsetUp = the shackle's offset in Unity y (= -shY).</summary>
    public const int ShacklePointCount = 51, ArcSegments = 48;
    public static int ShacklePoints(Geo g, float k, float shOffsetUp, Vector4[] pts, int start)
    {
        var a = g.a;
        var T = -g.top;               // rim top above the centre, Unity y
        var yArc = T + VIS;           // = -yc
        var cx = -a + a * k;
        var n = start;
        pts[n++] = new Vector4(-a, T - 6f + shOffsetUp, 0f, 0f);
        for (var i = 0; i <= ArcSegments; i++)
        {
            var th = Mathf.PI * i / ArcSegments;
            pts[n++] = new Vector4(cx - a * k * Mathf.Cos(th), yArc + a * Mathf.Sin(th) + shOffsetUp, 0f, 0f);
        }
        pts[n++] = new Vector4(-a + 2f * a * k, T - 3f + shOffsetUp, 0f, 0f);
        return n - start;
    }
    /// <summary>The shackle clip (lab lockGlyph 237: clip rect up to G.top + 6), Unity y: nothing below it.</summary>
    public static float ShackleClipUp(Geo g) { return -g.top - 6f; }

    // ------------------------------------------------------------------ broken door B.4 (lab 533-569)
    public static readonly RR Leaf = new RR(11f, 23.5f, .8f);
    public const float LeafCy = 1.25f, KnobX = 3.4f, KnobY = 2.2f, KnobR = 1.1f;
    public const float HingeX = -5.5f, HingeY = -10.5f;
    public const float FrameLength = 64f;
    public const float DoorInSeconds = .8f, DoorReactSeconds = .55f, DoorOutSeconds = .3f;

    /// <summary>B.4 at one frame, lab convention: m leaf morph, fr frame draw, kn knob, th degrees clockwise
    /// round the hinge, dy px down; text alpha.</summary>
    public struct DoorPose { public float m, fr, kn, th, dy, text; }

    /// <summary>doorPhase 'in' + B.4 draw 'in' (lab 551, 565), x = 0..1 over 0.8 s.</summary>
    public static DoorPose DoorIn(float x)
    {
        var d = new DoorPose();
        d.m = Eoc(Clamp01(x / .3f));
        d.fr = Eio(Clamp01((x - .2f) / .35f));
        d.kn = Eob(Clamp01((x - .48f) / .1f));
        var act = x < .55f ? -1f : (x - .55f) * DoorInSeconds;
        if (act >= 0f) { d.th = 12f * SpringStep(act, .3f, 2.4f); d.dy = 1.5f * Eoc(Clamp01(act / .15f)); }
        d.text = Clamp01(x / .6f);
        return d;
    }
    /// <summary>B.4 'react' (lab 566), r = seconds since E, window 0.55 s.</summary>
    public static DoorPose DoorReact(float r)
    {
        var d = new DoorPose { m = 1f, fr = 1f, kn = 1f, dy = 1.5f, text = 1f };
        d.th = r < .09f ? 12f - 9f * Eoc(r / .09f) : 3f + 9f * SpringStep(r - .09f, .3f, 2.6f);
        return d;
    }
    /// <summary>B.4 'hold' (lab 553, 567).</summary>
    public static DoorPose DoorHold() { return new DoorPose { m = 1f, fr = 1f, kn = 1f, th = 12f, dy = 1.5f, text = 1f }; }
    /// <summary>doorPhase 'out' + B.4 'out' (lab 552, 567), x = 0..1 over 0.3 s.</summary>
    public static DoorPose DoorOut(float x)
    {
        var d = new DoorPose();
        d.m = 1f - Eio(Clamp01((x - .5f) / .5f));
        d.fr = 1f - Eio(Clamp01((x - .3f) / .5f));
        d.kn = 1f - Clamp01((x - .3f) / .3f);
        var undo = Eio(Clamp01(x / .5f));
        d.th = 12f * (1f - undo);
        d.dy = 1.5f * (1f - undo);
        d.text = 1f - Clamp01(x / .6f);
        return d;
    }
    /// <summary>The leaf rect (lab doorLeaf 543-548): size, and its centre y (SVG, down) = LEAF_CY * m.</summary>
    public static RR LeafAt(float m) { return RR.Lerp(Dot, Leaf, m); }

    /// <summary>The frame (lab FRAME_D 533) drawn to fr of its length from (-7, 13) SVG, as Unity points
    /// (y up): writes up to 4 points; returns the count (0 when nothing is drawn).</summary>
    public static int FramePoints(float fr, Vector4[] pts, int start)
    {
        var len = Clamp01(fr) * FrameLength;
        if (len <= 1e-4f) return 0;
        var n = start;
        pts[n++] = new Vector4(-7f, -13f, 0f, 0f);
        // up the left side (25), across the top (14), down the right (25)
        if (len <= 25f) { pts[n++] = new Vector4(-7f, -13f + len, 0f, 0f); return n - start; }
        pts[n++] = new Vector4(-7f, 12f, 0f, 0f);
        if (len <= 39f) { pts[n++] = new Vector4(-7f + (len - 25f), 12f, 0f, 0f); return n - start; }
        pts[n++] = new Vector4(7f, 12f, 0f, 0f);
        pts[n++] = new Vector4(7f, 12f - (len - 39f), 0f, 0f);
        return n - start;
    }

    // ------------------------------------------------------------------ P4 pickup (lab 318-333, 706-710, 339)
    public static float Bez(float s, float p0, float p1, float p2) { return (1f - s) * (1f - s) * p0 + 2f * (1f - s) * s * p1 + s * s * p2; }
    /// <summary>lab P4.s with t = lab time from the start of the hold; flight = eio(clamp((t - .5) / .72)).</summary>
    public static float P4S(float t) { return Eio(Clamp01((t - .5f) / .72f)); }
    public const float P4Hold = .5f, P4Flight = .72f;
    /// <summary>lab 321: the game's held swing, 25 deg, 2.2 Hz, decay 2.5/s, from the start of the hold.</summary>
    public static float HeldSwing(float t) { return 25f * Mathf.Exp(-2.5f * t) * Mathf.Sin(2f * Mathf.PI * 2.2f * t); }
    /// <summary>lab 339 P4 text, t = lab time: rule grows at 1.18 (= +0.68 after take-off), label at 1.24, meta at 1.34.</summary>
    public static float P4Rule(float t) { return Eoc(Clamp01((t - 1.18f) / .16f)); }
    public static float P4Label(float t) { return Eoc(Clamp01((t - 1.24f) / .22f)); }
    public static float P4Meta(float t) { return Eoc(Clamp01((t - 1.34f) / .22f)); }
    public const float P4LabelDx = -8f;

    /// <summary>
    /// lab simulate (323-333) for one joint, precomputed into <paramref name="table"/> (dt = 1/480), driven by
    /// the pivot's x(t) = TLx(t) + 35 * scale(t) of the P4 path (lab 710): x0 = glyph top-left x at take-off,
    /// ctrl, x1 the slot's; s0, s1 the scales. Returns the sample count. No allocation (caller's table).
    /// </summary>
    public const float SimDt = 1f / 480f, SimH = 1f / 960f;
    public static int SimulateP4(float x0, float ctrl, float x1, float s0, float s1, float T, float normT, float f, float z, float amp, float[] table)
    {
        // The lab normalises the peak over its window (theatre: 3.6 s of lab time); the table runs on to T so
        // the landing swing decays to nothing (at 6 s: 0.04 deg) before the panel goes to rest. Accumulated in
        // double like the lab's JS (float32 drifts 0.01 deg over the 1,700 steps).
        var n = Mathf.Min(table.Length, Mathf.CeilToInt(T / SimDt) + 2);
        var nNorm = Mathf.Min(n, Mathf.CeilToInt(normT / SimDt) + 2);
        double a1 = 0, w1 = 0, max = 0;
        double dt = 1.0 / 480.0, h = 1.0 / 960.0;
        var o1 = 2.0 * System.Math.PI * f;
        for (var i = 0; i < n; i++)
        {
            var t = i * dt;
            var ax = (PivotX(t + h, x0, ctrl, x1, s0, s1) - 2.0 * PivotX(t, x0, ctrl, x1, s0, s1) + PivotX(t - h, x0, ctrl, x1, s0, s1)) / (h * h);
            var al1 = -o1 * o1 * a1 - 2.0 * z * o1 * w1 + ax;
            w1 += al1 * dt; a1 += w1 * dt; table[i] = (float)a1;
            if (i < nNorm && System.Math.Abs(a1) > max) max = System.Math.Abs(a1);
        }
        var k = max > 0 ? amp / max : 0;
        for (var i = 0; i < n; i++) table[i] = (float)(table[i] * k);
        return n;
    }
    static double PivotX(double t, double x0, double ctrl, double x1, double s0, double s1)
    {
        var u = (t - .5) / .72;
        u = u < 0 ? 0 : u > 1 ? 1 : u;
        var s = u < .5 ? 4 * u * u * u : 1 - System.Math.Pow(-2 * u + 2, 3) / 2;   // P4S in double
        return (1 - s) * (1 - s) * x0 + 2 * (1 - s) * s * ctrl + s * s * x1 + 35.0 * (s0 + (s1 - s0) * s);
    }
    public static float SimAt(float[] table, int n, float t)
    {
        var i = Mathf.Min(n - 1, Mathf.Max(0, Mathf.RoundToInt(t / SimDt)));
        return table[i];
    }
    // JS Math.round rounds .5 up; Mathf.RoundToInt rounds half to even. t / dt lands on .5 only at exact
    // multiples of 1/960 s, which 60 fps clocks never hit (the harness checks it).

    // ------------------------------------------------------------------ U.3 unlock gesture on the panel key (lab 793-803)
    public static float SwingKick(float t, float a, float f = 2f, float d = 4f) { return t > 0f ? a * Mathf.Exp(-d * t) * Mathf.Sin(2f * Mathf.PI * f * t) : 0f; }
    /// <summary>U.3 pose(r): dx, dy glyph px (SVG, y down), sy key fold, swing deg clockwise, rule width factor.</summary>
    public struct KeyGesture { public float dx, dy, sy, swing, rule; }
    public const float GestureSeconds = 2f;   // the kicks have decayed below 0.03 deg by 2 s
    public static KeyGesture UnlockU3(float r)
    {
        var q = new KeyGesture();
        // U.1 pose (lab 797)
        q.dx = r < .1f ? 6f * Eoc(r / .1f) : r < .42f ? 6f : 6f * (1f - Eio(Clamp01((r - .42f) / .18f)));
        q.sy = r < .1f ? 1f : r < .28f ? Lerp(1f, .15f, Eio((r - .1f) / .18f)) : r < .42f ? Lerp(.15f, 1f, Eio((r - .28f) / .14f)) : 1f;
        q.swing = SwingKick(r - .1f, -9f) + SwingKick(r - .6f, 6f);
        // U.3 click (lab 803)
        var c = Clamp01((r - .24f) / .14f);
        q.rule = 1f + .6f * Mathf.Sin(Mathf.PI * c);
        q.dy = -Mathf.Sin(Mathf.PI * c);
        return q;
    }
    public static KeyGesture GestureRest() { return new KeyGesture { sy = 1f, rule = 1f }; }

    // ------------------------------------------------------------------ Read: the look-eye E.1 "Opens from the dot"
    // DECISIONS #17 (Red, 10-15). Port of the lab's p16 LOOK block, from the CURRENT lab (snapshot 2026-10-15 13:53,
    // md5 290e3cee2d39729e85bd4b3206b3e4ce, W/hudkey/lab/lab_template_v2_snap1015_1353.html; the line numbers in
    // this section are that file's): EYE / DOTEYE 974-975, lerpE 976, slowBlink 978, LKT 979, lidD 986,
    // eyeDraw 998-1010, LOOK[0] 'E.1' 1012-1017, lookPose 1048-1062, frameLook 1063-1081, the in-context look
    // scene 799-808. Lab convention: 1080p px round the crosshair point, SVG y DOWN (up < 0 is above).

    /// <summary>
    /// One frame of the eye (lab st): half-width w, upper and lower lid apex up / lo (y down, so up &lt; 0 is
    /// above the aim point), pupil radius pr, corner tension c, gaze pdy (y down), the lids' opacity lidOp and
    /// the whole eye's opacity op (lookPose defaults both to 1).
    /// </summary>
    public struct EyePose { public float w, up, lo, pr, c, pdy, lidOp, op; }

    public static EyePose EyeShape(float w, float up, float lo, float pr, float c, float pdy)
    {
        return new EyePose { w = w, up = up, lo = lo, pr = pr, c = c, pdy = pdy, lidOp = 1f, op = 1f };
    }
    /// <summary>lab EYE (974): the rest pose, an almond 40 x 15 px, pupil r 5.</summary>
    public static readonly EyePose EyeRest = EyeShape(20f, -8f, 7f, 5f, .45f, 0f);
    /// <summary>lab DOTEYE (975): the eye drawn as the dot, a circle of r 8 round a pupil of r 8.</summary>
    public static readonly EyePose DotEye = EyeShape(8f, -8f, 8f, 8f, 1f, 0f);
    /// <summary>LOOK[0].focus (1012): the lids open to -9.5 / +8 and the pupil narrows to r 4.</summary>
    public const float EyeFocusUp = -9.5f, EyeFocusLo = 8f, EyeFocusPr = 4f;

    /// <summary>lerpE (976). The lab's result carries no lidOp / op; lookPose then defaults both to 1.</summary>
    public static EyePose LerpE(EyePose a, EyePose b, float t)
    {
        return new EyePose { w = Lerp(a.w, b.w, t), up = Lerp(a.up, b.up, t), lo = Lerp(a.lo, b.lo, t), pr = Lerp(a.pr, b.pr, t),
                             c = Lerp(a.c, b.c, t), pdy = Lerp(a.pdy, b.pdy, t), lidOp = 1f, op = 1f };
    }

    /// <summary>slowBlink (978): one slow blink, the closure 0..1. Shuts in 220 ms (ease-in), holds 70 ms, opens in 340 ms (ease-out).</summary>
    public static float SlowBlink(float s)
    {
        if (s < 0f || s > .63f) return 0f;
        if (s < .22f) { var q = s / .22f; return q * q; }
        if (s < .29f) return 1f;
        return 1f - Eoc((s - .29f) / .34f);
    }

    /// <summary>The build's length. The lab's mIn is 1.08 (1012); its pose is exactly the rest pose from
    /// 1.05 s on (the blink starts at 0.42 and lasts 0.63), so the build is complete at 1.05 s (grammar sec. 2: total &lt;= 1.05 s).</summary>
    public const float EyeInSeconds = 1.05f, EyeLabIn = 1.08f;
    /// <summary>The blink's start and length inside the build (LOOK[0].inPose: slowBlink(x - .42)).</summary>
    public const float EyeBlinkAt = .42f, EyeBlinkSeconds = .63f;
    /// <summary>The build without the blink (Reduce Flashing; a re-aim inside the re-blink rule): the morph ends at 0.30 s.</summary>
    public const float EyeNoBlinkIn = .3f;
    /// <summary>LKT (979): react .15, back .15, away (the leave) .3.</summary>
    public const float EyeReactSeconds = .15f, EyeBackSeconds = .15f, EyeOutSeconds = .3f;
    /// <summary>Leaving when another state waits (grammar sec. 4, LOOK[0] spec 'Cut to 120 ms').</summary>
    public const float LeaveCutSeconds = .12f;
    /// <summary>A re-blink needs the aim to have left AND this long since the last blink started (grammar sec. 5).</summary>
    public const float EyeReblinkSeconds = 1f;
    /// <summary>Reduce Motion: every state is an alpha cross-fade of this length (grammar sec. 5; lab note 198: 150 ms).</summary>
    public const float FadeSeconds = .15f;
    /// <summary>The crosshair's minimum dwell (hud_build DECISIONS #39 (2), photosafety: nothing on the HUD flashes faster
    /// than 3 Hz): at least this long between the dot opening (a state builds, or resumes from its leave) and closing (its
    /// leave starts), and between a close and the next open, so an aim jittering across a target's edge changes the dot
    /// at most 2.5 open-close cycles a second. Not in the lab: an aim that lasts this long or longer, landing after the dot
    /// has rested this long, plays the lab's curves unchanged. Player events (the unlock, no key, the broken try, the
    /// close-up) are never held by it.</summary>
    public const float MinDwellSeconds = .2f;

    /// <summary>LOOK[0].inPose (1017), x = seconds into the build. blink = false is the no-blink build (Reduce
    /// Flashing, the re-blink rule): the same morph without the lid closure, at rest from 0.30 s.</summary>
    public static EyePose EyeIn(float x, bool blink)
    {
        var st = LerpE(DotEye, EyeRest, Eio(Clamp01(x / .3f)));
        st.lidOp = Clamp01(x / .08f);
        if (blink) st.up = Lerp(st.up, st.lo, SlowBlink(x - EyeBlinkAt));
        return st;
    }

    /// <summary>lookPose 'react' (1053) from the pose <paramref name="s"/> (the lab's is the rest pose):
    /// the focus in 80 ms (ease-out), the eye fading over 50-150 ms. x = seconds since the close-up opened.</summary>
    public static EyePose EyeReact(EyePose s, float x)
    {
        var f = s; f.up = EyeFocusUp; f.lo = EyeFocusLo; f.pr = EyeFocusPr;
        var st = LerpE(s, f, Eoc(Clamp01(x / .08f)));
        st.lidOp = s.lidOp;
        st.op = s.op * (1f - Clamp01((x - .05f) / .1f));
        return st;
    }

    /// <summary>lookPose 'back' (1052, 1060): the rest pose fading in, x = 0..1 over EyeBackSeconds. No blink.</summary>
    public static EyePose EyeBack(float x) { var st = EyeRest; st.op = x; return st; }

    /// <summary>lookPose 'out' (1055-1058) from the pose <paramref name="s"/> (the lab's is the rest pose): the
    /// upper lid shuts onto the lower in 120 ms (in-out), then the eye shrinks back into the dot by 300 ms and the
    /// lids fade over 240-300 ms. x = seconds since the leave started.</summary>
    public static EyePose EyeOut(EyePose s, float x)
    {
        EyePose st;
        if (x < .12f) { st = s; st.up = Lerp(s.up, s.lo, Eio(x / .12f)); }
        else
        {
            var shut = s; shut.up = s.lo;
            st = LerpE(shut, DotEye, Eio(Clamp01((x - .12f) / .18f)));
            st.lidOp = s.lidOp * (1f - Clamp01((x - .24f) / .06f));
            st.op = s.op;
        }
        return st;
    }

    // ---- E.3 "Reading eye" (lab LOOK[2], 1027-1032): a PARAMETER fallback only (DECISIONS #30), used if playtesters misread
    // E.1. As E.1, but the blink settles into a heavy-lidded pose with the gaze lowered onto the thing.
    /// <summary>LOOK[2].rest (1027): lids at -2 / +7.5, the pupil 1.8 px low.</summary>
    public static readonly EyePose EyeRest3 = EyeShape(20f, -2f, 7.5f, 5f, .45f, 1.8f);
    /// <summary>The rest pose of a variant (e3 = E.3).</summary>
    public static EyePose EyeRestOf(bool e3) { return e3 ? EyeRest3 : EyeRest; }

    /// <summary>LOOK[2].inPose (1031-1032): E.1's morph and blink; from 0.71 s the pose settles into EyeRest3 over 0.30 s
    /// (in-out), under the blink's opening. blink = false (ours; the lab has no no-blink E.3): the morph goes straight
    /// to EyeRest3, at rest from 0.30 s.</summary>
    public static EyePose EyeIn(float x, bool blink, bool e3)
    {
        if (!e3) return EyeIn(x, blink);
        if (!blink) { var q = LerpE(DotEye, EyeRest3, Eio(Clamp01(x / .3f))); q.lidOp = Clamp01(x / .08f); return q; }
        var st = LerpE(DotEye, EyeRest, Eio(Clamp01(x / .3f)));
        var b = SlowBlink(x - EyeBlinkAt);
        if (x > .71f) st = LerpE(EyeRest, EyeRest3, Eio(Clamp01((x - .71f) / .3f)));
        st.lidOp = Clamp01(x / .08f);
        st.up = Lerp(st.up, st.lo, b);
        return st;
    }

    /// <summary>lookPose 'react' for a variant: E.3's focus also lifts the gaze (LOOK[2].focus pdy 0).</summary>
    public static EyePose EyeReact(EyePose s, float x, bool e3)
    {
        if (!e3) return EyeReact(s, x);
        var f = s; f.up = EyeFocusUp; f.lo = EyeFocusLo; f.pr = EyeFocusPr; f.pdy = 0f;
        var st = LerpE(s, f, Eoc(Clamp01(x / .08f)));
        st.lidOp = s.lidOp;
        st.op = s.op * (1f - Clamp01((x - .05f) / .1f));
        return st;
    }

    /// <summary>lookPose 'back' for a variant: its rest pose fading in, no blink.</summary>
    public static EyePose EyeBack(float x, bool e3) { var st = EyeRestOf(e3); st.op = x; return st; }

    /// <summary>The prompt alpha per phase (frameLook 1066-1071; the in-context scene 802-807).</summary>
    public static float EyeInText(float x) { return Clamp01(x / .3f); }
    public static float EyeReactText(float x) { return 1f - Clamp01((x - .05f) / .1f); }
    public static float EyeBackText(float x) { return x; }
    public static float EyeOutText(float x) { return 1f - Clamp01(x / .2f); }

    /// <summary>
    /// eyeDraw (998-1010): what is drawn for a pose. lo; up = min(up, lo) (the upper lid never passes the lower);
    /// w = max(w, .01); pupil radius max(pr, 0) (E.1 sets no sx / sy), its keyline + KL, both clipped to the
    /// almond between the lids; the pupil at op, the paper lids at lidOp * op, the lid keylines at lidOp inside
    /// ONE keyline group at kl * op.
    /// </summary>
    public static void EyeDraw(EyePose st, out float w, out float up, out float lo, out float pr)
    {
        lo = st.lo;
        up = Mathf.Min(st.up, lo);
        w = Mathf.Max(st.w, .01f);
        pr = Mathf.Max(st.pr, 0f);
    }

    /// <summary>lidD (986): the lid cubic M(-w, 0) C(-w c, 4a/3) (w c, 4a/3) (w, 0), apex a, sampled as
    /// LidSegments + 1 points in UNITY y up (y = -SVG y). Writes them from <paramref name="start"/>; returns the count.</summary>
    public const int LidSegments = 30;
    public static int LidPoints(float w, float a, float c, Vector4[] pts, int start)
    {
        var cy = -a * 4f / 3f;
        for (var i = 0; i <= LidSegments; i++)
        {
            var t = (float)i / LidSegments;
            var u = 1f - t;
            var b0 = u * u * u; var b1 = 3f * u * u * t; var b2 = 3f * u * t * t; var b3 = t * t * t;
            pts[start + i] = new Vector4(-w * b0 - w * c * b1 + w * c * b2 + w * b3, (b1 + b2) * cy, 0f, 0f);
        }
        return LidSegments + 1;
    }
}
