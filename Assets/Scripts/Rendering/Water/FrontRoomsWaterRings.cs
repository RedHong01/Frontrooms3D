// The pool family's water: rings (research/bathroom_rooms/70_look_build.md, "Ripples"; 17_water.md 3.1-3.5, 7 item 4).
// Game visual line.
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rings on the pool family's still water: what a drip, a footstep, a body wading or a plunge does to the surface,
/// in closed form, fed to FrontRoomsWaterRipples.hlsl as a small array (16 / 8 / 4 slots by tier).
///
/// Physics (Tools/lookdev/pool_water/ring_model.py, checked against the exact linear solution there): a disturbance
/// is a hump of volume V and radius a (plus a splash share eps at radius aS) released from rest, the Cauchy-Poisson
/// problem with capillary-gravity dispersion omega^2 = g k + (sigma/rho) k^3. Energy travels at the group speed, whose
/// minimum is about 0.178 m/s (4.4 cm waves): a ring has a calm centre whose rim grows at that speed, and beyond it two
/// trains share every point, long waves whose crests run out through the packet and short ones that race ahead of
/// their crests, the front reaching 0.3-0.45 m/s. The shader evaluates both trains by stationary phase (the wave number
/// at r / t comes from the table built here) and the rim by the Airy function. Amplitude falls as the waves spread,
/// capillaries are damped by the surface film, and every ring fades within 4-8 s.
///
/// Walls: a ring inside a registered water rectangle is mirrored across its four sides and corners (image sources,
/// reflection 0.85 per wall); an image takes a slot only once its waves can have reached the water.
///
/// Hooks (callers never edit this file; owners call them from their own code, or a contract wires them):
///   ReportBody(body, feet, radius)  the player's and the Relay's positions, every frame they move: in water below
///                                   0.45 m the body leaves footstep rings at each stride, deeper it leaves a wading
///                                   wake (a ring every 0.3 m), standing in water it stirs a small ring now and then
///   Footstep(p), Wade(p), Plunge(p, size), Drip(p)   one event at a point on the water
///   RegisterWater(id, rect, waterY)  the rectangles whose walls reflect (the water component or the map)
/// Photosafety: the only waves that move large areas are the plunge's and the wake's long ones, whose periods are
/// 0.4-0.8 s (at most 2.5 Hz); everything faster is small-scale. Reduce Motion (FrontRoomsWaterRipples.ReduceMotion)
/// freezes them: no new rings, the live ones stop where they are and fade out in a second.
/// Nothing runs until FrontRoomsWaterRipples.Tick is called; no other system reads these rings (visual only).
/// </summary>
public static class FrontRoomsWaterRings
{
    public enum Body { Player = 0, Relay = 1, Other = 2 }
    public enum Kind { Drip = 0, Footstep = 1, Wade = 2, Plunge = 3 }

    public const int MaxSlots = 16;
    public const float Gravity = 9.81f;
    public const float SigmaOverRho = 0.0728f / 998f;
    public const float NuEff = 3e-6f;         // viscous damping x3: a used pool's surface film kills capillaries fast
    public const float FadeSeconds = 3f;      // every wave also fades as exp(-t / 3 s)
    public const float VMax = 1.0f;           // the fastest group speed the table holds, m/s
    public const int LutSize = 256;
    public const float WallReflection = .85f;
    public const float DeepWade = .45f;       // water deeper than this at the feet: a wading wake, not footsteps

    struct Hook { public float a, aS, eps, volume, life, agitation; }

    // ring_model.py: radius, splash radius and share, volume calibrated to 17 s3.1's crests (drip 0.4 mm at 0.10 m,
    // footstep 5 mm at 0.30 m, wade 12 mm at 0.50 m, plunge 30 mm at 1.0 m), life, local roughening of the water.
    // The wade is mostly the body's long waves (a 0.18 m: the wake's own lengths, about 1 m at 1.2 m/s) with little
    // splash: a sharp short-wave rim on every wade ring would read as a chain of drawn circles, not a wake.
    static readonly Hook[] hooks =
    {
        new Hook { a = .003f, aS = .0015f, eps = .30f, volume = 7.414e-7f, life = 5f, agitation = 0f },
        new Hook { a = .040f, aS = .008f, eps = .25f, volume = 2.965e-4f, life = 7f, agitation = .6f },
        new Hook { a = .180f, aS = .020f, eps = .04f, volume = 1.010e-2f, life = 6f, agitation = .8f },
        new Hook { a = .300f, aS = .020f, eps = .08f, volume = 9.241e-2f, life = 8f, agitation = 1.2f },
    };

    struct Ring
    {
        public float x, z, born, volume, a, aS, eps, life, agitation;
        public int water;
        public Kind kind;
    }

    struct Water { public int id; public Rect rect; public float y; }

    struct Mover { public bool seen, inWater; public Vector3 last; public float sinceEmit, lastTime, idle; public int side; }

    struct Slot { public Vector4 a, b; public float score; }

    static readonly List<Ring> live = new List<Ring>(64);
    static readonly List<Water> waters = new List<Water>();
    static readonly Mover[] movers = new Mover[3];
    static readonly List<Slot> slots = new List<Slot>(96);
    static readonly Vector4[] up0 = new Vector4[MaxSlots], up1 = new Vector4[MaxSlots];
    static Texture2D lut;
    static float kgm, cgmin, omgm, om3gm;
    static float now, fadeAll = 1f;
    static bool frozen;
    static int budget = -1;
    static uint rng = 2463534242u;

    static readonly int Ring0Id = Shader.PropertyToID("_FR_Ring0");
    static readonly int Ring1Id = Shader.PropertyToID("_FR_Ring1");
    static readonly int RingCountId = Shader.PropertyToID("_FR_RingCount");
    static readonly int RingLutId = Shader.PropertyToID("_FR_RingLut");
    static readonly int RingConst0Id = Shader.PropertyToID("_FR_RingConst0");
    static readonly int RingConst1Id = Shader.PropertyToID("_FR_RingConst1");
    static readonly int RingConst2Id = Shader.PropertyToID("_FR_RingConst2");
    static readonly int RingAiry0Id = Shader.PropertyToID("_FR_RingAiry0");
    static readonly int RingAiry1Id = Shader.PropertyToID("_FR_RingAiry1");

    // Ai(x) on [-2.6, 0]: degree-6 polynomial, highest power first (ring_model.py fit_airy, max error 4e-4);
    // on x >= 0: Ai(0) exp(-p1 x - p2 x^1.5) (max error 1e-2)
    static readonly float[] AiryPoly = { -1.4868102e-03f, -2.6653112e-02f, -8.4315737e-02f, -8.7972256e-03f, -3.4445266e-02f, -2.6576509e-01f, 3.5470350e-01f };
    public const float AiryP1 = .38797f, AiryP2 = .54714f;

    /// <summary>Slots per tier: 16 desktop Ultra, 8 other desktop presets, 4 the phone tier (handheld, WebGL).
    /// The preset belongs to the settings line (FrontRoomsQuality); this only reads it. Set to override (tools).</summary>
    public static int Budget
    {
        get
        {
            if (budget >= 0) return budget;
            if (FrontRoomsTrofferKit.PhoneTier) return 4;
            return FrontRoomsQuality.CurrentPreset == FrontRoomsQuality.Preset.Ultra ? 16 : 8;
        }
        set { budget = Mathf.Clamp(value, -1, MaxSlots); }
    }

    /// <summary>Footsteps from ReportBody (off when the game posts real footstep events through Footstep).</summary>
    public static bool AutoSteps = true;

    public static int LiveCount => live.Count;
    public static int SlotCount { get; private set; }
    public static float GroupSpeedMin => cgmin;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        live.Clear();
        waters.Clear();
        for (var i = 0; i < movers.Length; i++) movers[i] = default(Mover);
        now = 0f;
        fadeAll = 1f;
        frozen = false;
        budget = -1;
        AutoSteps = true;
        SlotCount = 0;
    }

    // ------------------------------------------------------------------------------------------------ water rectangles
    public static void RegisterWater(int id, Rect xz, float waterY)
    {
        UnregisterWater(id);
        waters.Add(new Water { id = id, rect = xz, y = waterY });
    }

    public static void UnregisterWater(int id)
    {
        for (var i = waters.Count - 1; i >= 0; i--) if (waters[i].id == id) waters.RemoveAt(i);
    }

    public static void ClearWater() { waters.Clear(); }

    static int WaterIndexAt(Vector3 p, float above)
    {
        for (var i = 0; i < waters.Count; i++)
        {
            var w = waters[i];
            if (w.rect.Contains(new Vector2(p.x, p.z)) && p.y <= w.y + above) return i;
        }
        return -1;
    }

    // ------------------------------------------------------------------------------------------------ hooks
    public static void Drip(Vector3 p, float strength = 1f) { Spawn(Kind.Drip, p, strength, 1f); }
    public static void Footstep(Vector3 p, float strength = 1f) { Spawn(Kind.Footstep, p, strength, 1f); }
    public static void Wade(Vector3 p, float strength = 1f) { Spawn(Kind.Wade, p, strength, 1f); }

    /// <summary>A body or object falling in: size 1 is a person (about 90 litres moved, 30 mm waves 1 m out).</summary>
    public static void Plunge(Vector3 p, float size = 1f) { Spawn(Kind.Plunge, p, 1f, Mathf.Clamp(size, .2f, 3f)); }

    static void Spawn(Kind kind, Vector3 p, float strength, float size)
    {
        if (FrontRoomsWaterRipples.ReduceMotion || strength <= 0f) return;
        var h = hooks[(int)kind];
        var wi = WaterIndexAt(p, 2.5f);
        if (waters.Count > 0 && wi < 0) return;            // not on any registered water
        var r = new Ring
        {
            x = p.x, z = p.z, born = now, kind = kind, water = wi,
            a = h.a * size, aS = h.aS * Mathf.Sqrt(size), eps = h.eps,
            volume = h.volume * strength * size * size * size,
            life = h.life, agitation = h.agitation * Mathf.Min(strength, 2f)
        };
        if (live.Count >= 48)
        {
            // drop the weakest, oldest
            var worst = 0;
            var ws = float.MaxValue;
            for (var i = 0; i < live.Count; i++)
            {
                var s = Mathf.Pow(live[i].volume, 1f / 3f) * (1f - (now - live[i].born) / live[i].life);
                if (s < ws) { ws = s; worst = i; }
            }
            live.RemoveAt(worst);
        }
        live.Add(r);
    }

    /// <summary>
    /// The player's and the Relay's positions (feet, world space), every frame they can move. In water shallower than
    /// 0.45 m at the feet they leave a footstep ring each stride (0.7 m, feet 0.2 m apart); deeper they leave a wading
    /// wake (a ring every 0.3 m, slightly ahead of the body); standing in water they stir a faint ring every 1.5-3 s.
    /// radius: the body's (0.25 the player, 0.3 the Relay).
    /// </summary>
    public static void ReportBody(Body body, Vector3 feet, float radius = .25f)
    {
        var i = Mathf.Clamp((int)body, 0, movers.Length - 1);
        var m = movers[i];
        var wi = WaterIndexAt(feet, .05f);
        var t = now;
        if (!m.seen || wi < 0 || FrontRoomsWaterRipples.ReduceMotion)
        {
            m.seen = true;
            m.inWater = wi >= 0;
            m.last = feet;
            m.sinceEmit = 0f;
            m.lastTime = t;
            m.idle = 0f;
            movers[i] = m;
            return;
        }
        var w = waters[wi];
        var depth = w.y - feet.y;
        var d = new Vector2(feet.x - m.last.x, feet.z - m.last.z);
        var dt = Mathf.Max(t - m.lastTime, 1e-4f);
        var dist = d.magnitude;
        var speed = dist / dt;
        if (dist > 2f) { m.last = feet; m.lastTime = t; movers[i] = m; return; }     // teleport: no ring
        var dir = dist > 1e-5f ? d / dist : Vector2.zero;
        m.sinceEmit += dist;
        var scale = Mathf.Clamp(radius / .25f, .5f, 2f);
        if (!m.inWater && depth > .02f)
        {
            // stepping in
            Spawn(depth < DeepWade ? Kind.Footstep : Kind.Wade, feet, 1.2f * scale, 1f);
            m.sinceEmit = 0f;
        }
        else if (speed > .15f)
        {
            m.idle = 0f;
            var sp = Mathf.Clamp(speed / 1.4f, .4f, 1.8f);
            if (depth < DeepWade)
            {
                if (AutoSteps && m.sinceEmit >= .7f)
                {
                    m.side = m.side == 0 ? 1 : 0;
                    var lat = new Vector3(-dir.y, 0f, dir.x) * (m.side == 0 ? -.1f : .1f);
                    Spawn(Kind.Footstep, feet + lat, sp * scale * Mathf.Clamp01(depth / .06f + .3f), 1f);
                    m.sinceEmit = 0f;
                }
            }
            else if (m.sinceEmit >= .3f)
            {
                var ahead = new Vector3(dir.x, 0f, dir.y) * (radius * .6f);
                Spawn(Kind.Wade, feet + ahead, sp * scale, 1f);
                m.sinceEmit = 0f;
            }
        }
        else
        {
            m.idle += dt;
            if (m.idle > 1.5f + 1.5f * Rand01())
            {
                Spawn(depth < DeepWade ? Kind.Footstep : Kind.Wade, feet, .15f * scale, 1f);
                m.idle = 0f;
            }
        }
        m.inWater = true;
        m.last = feet;
        m.lastTime = t;
        movers[i] = m;
    }

    public static void Clear()
    {
        live.Clear();
        for (var i = 0; i < movers.Length; i++) movers[i] = default(Mover);
        SlotCount = 0;
        Shader.SetGlobalFloat(RingCountId, 0f);
    }

    static float Rand01()
    {
        rng ^= rng << 13;
        rng ^= rng >> 17;
        rng ^= rng << 5;
        return (rng & 0xFFFFFF) / 16777216f;
    }

    // ------------------------------------------------------------------------------------------------ tick and upload
    /// <summary>Advance to the ripple clock (seconds; frozen under Reduce Motion), choose the slots, upload them.
    /// Called by FrontRoomsWaterRipples.Tick; realDt is unscaled time, for the Reduce Motion fade.</summary>
    internal static void Tick(float clock, float realDt, Vector3 viewer, bool reduceMotion)
    {
        EnsureLut();
        if (reduceMotion)
        {
            frozen = true;
            fadeAll = Mathf.Max(0f, fadeAll - realDt);         // the frozen rings fade out in one second
            if (fadeAll <= 0f) live.Clear();
        }
        else
        {
            if (frozen) { live.Clear(); frozen = false; }
            fadeAll = 1f;
            now = clock;
        }
        for (var i = live.Count - 1; i >= 0; i--)
        {
            var age = now - live[i].born;
            if (age > live[i].life || age < -1f) live.RemoveAt(i);          // expired, or the clock wrapped (24 h)
        }
        slots.Clear();
        var vmaxReach = VMax;
        for (var i = 0; i < live.Count; i++)
        {
            var r = live[i];
            var age = Mathf.Max(now - r.born, 1e-3f);
            var lifeLeft = 1f - age / r.life;
            var fade = Mathf.Clamp01(lifeLeft * 4f) * fadeAll;                 // the last quarter of its life
            var vol = r.volume * fade;
            var dv = new Vector2(r.x - viewer.x, r.z - viewer.z).magnitude;
            var score = Mathf.Pow(Mathf.Max(r.volume, 1e-12f), 1f / 3f) * lifeLeft / (1f + .5f * dv);
            slots.Add(new Slot
            {
                a = new Vector4(r.x, r.z, age, vol),
                b = new Vector4(r.a, r.aS, r.eps, r.agitation * fade),
                score = score
            });
            if (r.water < 0 || r.water >= waters.Count) continue;
            // images across the walls and corners of its water rectangle
            var rc = waters[r.water].rect;
            var reach = vmaxReach * age;
            for (var ix = -1; ix <= 1; ix++)
            {
                for (var iz = -1; iz <= 1; iz++)
                {
                    if (ix == 0 && iz == 0) continue;
                    var px = ix < 0 ? 2f * rc.xMin - r.x : ix > 0 ? 2f * rc.xMax - r.x : r.x;
                    var pz = iz < 0 ? 2f * rc.yMin - r.z : iz > 0 ? 2f * rc.yMax - r.z : r.z;
                    // distance from the image to the rectangle: its waves enter the water only after that
                    var ex = Mathf.Max(rc.xMin - px, 0f, px - rc.xMax);
                    var ez = Mathf.Max(rc.yMin - pz, 0f, pz - rc.yMax);
                    var enter = Mathf.Sqrt(ex * ex + ez * ez);
                    if (enter > reach) continue;
                    var g = (ix != 0 && iz != 0) ? WallReflection * WallReflection : WallReflection;
                    slots.Add(new Slot
                    {
                        a = new Vector4(px, pz, age, vol * g),
                        b = new Vector4(r.a, r.aS, r.eps, 0f),
                        score = score * g * .8f
                    });
                }
            }
        }
        slots.Sort((p, q) => q.score.CompareTo(p.score));
        var n = Mathf.Min(slots.Count, Budget);
        for (var i = 0; i < MaxSlots; i++)
        {
            if (i < n) { up0[i] = slots[i].a; up1[i] = slots[i].b; }
            else { up0[i] = Vector4.zero; up1[i] = Vector4.zero; }
        }
        SlotCount = n;
        Shader.SetGlobalVectorArray(Ring0Id, up0);
        Shader.SetGlobalVectorArray(Ring1Id, up1);
        Shader.SetGlobalFloat(RingCountId, n);
    }

    // ------------------------------------------------------------------------------------------------ dispersion table
    static double Om(double k) { return System.Math.Sqrt(Gravity * k + SigmaOverRho * k * k * k); }
    static double Om1(double k) { return (Gravity + 3.0 * SigmaOverRho * k * k) / (2.0 * Om(k)); }
    static double Om2(double k) { var o = Om(k); var o1 = Om1(k); return 3.0 * SigmaOverRho * k / o - o1 * o1 / o; }
    static double Om3(double k)
    {
        double o = Om(k), o1 = Om1(k), o2 = Om2(k);
        return 3.0 * SigmaOverRho / o - 3.0 * SigmaOverRho * k * o1 / (o * o) - 2.0 * o1 * o2 / o + o1 * o1 * o1 / (o * o);
    }

    static double Branch(double v, double km, bool gravity)
    {
        double lo, hi;
        if (gravity) { lo = 1e-3; hi = km; } else { lo = km; hi = 1e6; }
        for (var i = 0; i < 100; i++)
        {
            var mid = .5 * (lo + hi);
            var above = Om1(mid) > v;
            if (gravity) { if (above) lo = mid; else hi = mid; }
            else { if (above) hi = mid; else lo = mid; }
        }
        return .5 * (lo + hi);
    }

    /// <summary>Builds the wave-number table (k on the gravity and capillary branches against u = sqrt((v - c_gmin) /
    /// (VMax - c_gmin))) and the ring constants, once.</summary>
    public static void EnsureLut()
    {
        if (lut != null) return;
        double lo = 10.0, hi = 2000.0;
        for (var i = 0; i < 100; i++)
        {
            var mid = .5 * (lo + hi);
            if (Om2(mid) < 0.0) lo = mid; else hi = mid;
        }
        var km = .5 * (lo + hi);
        kgm = (float)km;
        cgmin = (float)Om1(km);
        omgm = (float)Om(km);
        om3gm = (float)Om3(km);
        var fmt = SystemInfo.SupportsTextureFormat(TextureFormat.RGHalf) ? TextureFormat.RGHalf : TextureFormat.RGBAHalf;
        var ch = fmt == TextureFormat.RGHalf ? 2 : 4;
        lut = new Texture2D(LutSize, 1, fmt, false, true)
        {
            name = "FR_RingLut", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave
        };
        var data = new ushort[LutSize * ch];
        for (var i = 0; i < LutSize; i++)
        {
            var u = i / (double)(LutSize - 1);
            var v = cgmin + u * u * (VMax - cgmin);
            double kg, kc;
            if (i == 0) { kg = km; kc = km; }
            else { kg = Branch(v, km, true); kc = Branch(v, km, false); }
            data[i * ch] = Mathf.FloatToHalf((float)kg);
            data[i * ch + 1] = Mathf.FloatToHalf((float)kc);
            if (ch == 4) { data[i * ch + 2] = 0; data[i * ch + 3] = Mathf.FloatToHalf(1f); }
        }
        lut.SetPixelData(data, 0);
        lut.Apply(false, true);
        Shader.SetGlobalTexture(RingLutId, lut);
        Shader.SetGlobalVector(RingConst0Id, new Vector4(cgmin, kgm, omgm, om3gm));
        Shader.SetGlobalVector(RingConst1Id, new Vector4(Gravity, SigmaOverRho, NuEff, 1f / FadeSeconds));
        Shader.SetGlobalVector(RingConst2Id, new Vector4(VMax, LutSize, AiryP1, AiryP2));
        Shader.SetGlobalVector(RingAiry0Id, new Vector4(AiryPoly[0], AiryPoly[1], AiryPoly[2], AiryPoly[3]));
        Shader.SetGlobalVector(RingAiry1Id, new Vector4(AiryPoly[4], AiryPoly[5], AiryPoly[6], 0f));
        Shader.SetGlobalFloat(RingCountId, 0f);
    }

    /// <summary>The ring constants as text (tools and logs).</summary>
    public static string Describe()
    {
        EnsureLut();
        return "c_gmin " + cgmin.ToString("0.0000") + " m/s at k " + kgm.ToString("0.0") + " (lambda "
            + (200f * Mathf.PI / kgm).ToString("0.00") + " cm), omega " + omgm.ToString("0.00") + ", omega''' "
            + om3gm.ToString("0.000e0") + ", table " + lut.format + " " + LutSize + ", budget " + Budget;
    }
}
