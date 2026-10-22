// FrontRoomsGlassFractureCore.cs -- GD3 step 3 (plan 10 sec 2.6): the portable building blocks of the crack-graph
// generator. Pure C# (no UnityEngine), so the same code runs in the Editor, the players (Mono/IL2CPP) and WebGL.
//
// Everything here mirrors Tools/Blender/glass/crack_graph_ref.py (the REFERENCE) operation for operation, in the
// same evaluation order, so the C# port reproduces the reference's doubles. Read 02_pattern.md sec 2.6 and
// build/03_generator.md before changing anything: a reordered sum or a different rounding changes the pattern.
using System;
using System.Collections.Generic;

/// <summary>A 2D point in slab-local metres (double precision, like the Python reference).</summary>
[Serializable]
public struct GlassV2
{
    public double x, y;

    public GlassV2(double x, double y)
    {
        this.x = x;
        this.y = y;
    }

    public override string ToString() => "(" + x.ToString("R") + ", " + y.ToString("R") + ")";
}

/// <summary>
/// The reference RNG: PCG32 (XSH RR) seeded through splitmix64 from (seed, stream, entity keys)
/// (crack_graph_ref.py "Portable RNG"). A struct, so keyed draws allocate nothing.
/// </summary>
public struct GlassRng
{
    const ulong PcgMul = 6364136223846793005UL;
    const ulong KeyStart = 0x6A09E667F3BCC908UL;
    ulong state, inc;

    public static ulong SplitMix64(ulong x)
    {
        x = unchecked(x + 0x9E3779B97F4A7C15UL);
        var z = x;
        z = unchecked((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL);
        z = unchecked((z ^ (z >> 27)) * 0x94D049BB133111EBUL);
        return z ^ (z >> 31);
    }

    static ulong Mix(ulong h, long v) => SplitMix64(h ^ unchecked((ulong)v));

    /// <summary>mix_key(*vals) of the reference (no seed masking: used for fork track ids and the golden seeds).</summary>
    public static ulong MixKey(long a, long b, long c) => Mix(Mix(Mix(KeyStart, a), b), c);
    public static ulong MixKey(long a, long b) => Mix(Mix(KeyStart, a), b);

    static GlassRng FromKey(ulong s)
    {
        var r = new GlassRng();
        r.inc = unchecked((SplitMix64(s ^ 0xDA3E39CB94B95BDBUL) << 1) | 1UL);
        r.state = 0UL;
        r.NextU32();
        r.state = unchecked(r.state + s);
        r.NextU32();
        return r;
    }

    // Rng(seed, *keys): s = mix_key(seed & MASK32, *keys)
    public static GlassRng Make(uint seed) => FromKey(Mix(KeyStart, seed));
    public static GlassRng Make(uint seed, long k1) => FromKey(Mix(Mix(KeyStart, seed), k1));
    public static GlassRng Make(uint seed, long k1, long k2) => FromKey(Mix(Mix(Mix(KeyStart, seed), k1), k2));
    public static GlassRng Make(uint seed, long k1, long k2, long k3) => FromKey(Mix(Mix(Mix(Mix(KeyStart, seed), k1), k2), k3));
    public static GlassRng Make(uint seed, long k1, long k2, long k3, long k4) =>
        FromKey(Mix(Mix(Mix(Mix(Mix(KeyStart, seed), k1), k2), k3), k4));
    public static GlassRng Make(uint seed, long k1, long k2, long k3, long k4, long k5) =>
        FromKey(Mix(Mix(Mix(Mix(Mix(Mix(KeyStart, seed), k1), k2), k3), k4), k5));
    public static GlassRng Make(uint seed, long k1, long k2, long k3, long k4, long k5, long k6) =>
        FromKey(Mix(Mix(Mix(Mix(Mix(Mix(Mix(KeyStart, seed), k1), k2), k3), k4), k5), k6));

    public uint NextU32()
    {
        var old = state;
        state = unchecked(old * PcgMul + inc);
        var xs = (uint)(((old >> 18) ^ old) >> 27);
        var rot = (int)(old >> 59);
        return (xs >> rot) | (xs << ((-rot) & 31));
    }

    /// <summary>(u32 &gt;&gt; 8) / 2^24: exact in float and double.</summary>
    public double U01() => (NextU32() >> 8) * (1.0 / 16777216.0);

    public double Uniform(double a, double b) => a + (b - a) * U01();

    /// <summary>Inclusive, lo + u32 % (hi - lo + 1), as the reference.</summary>
    public int RandInt(int lo, int hi) => lo + (int)(NextU32() % (uint)(hi - lo + 1));
}

/// <summary>
/// Math used by the generator. Every transcendental call goes through here, so the whole port can switch its math
/// source in one place (build/03_generator.md sec 4: Mono, IL2CPP and Apple libm differ by 1 ulp now and then).
/// Python semantics where they differ from C#: hypot, %, round(x, n).
/// </summary>
public static class GlassMath
{
    public const double TwoPi = 2.0 * Math.PI;
    const double DegToRad = Math.PI / 180.0;   // CPython: Py_MATH_PI / 180.0

    public static double Sin(double x) => Math.Sin(x);
    public static double Cos(double x) => Math.Cos(x);
    public static double Log(double x) => Math.Log(x);
    public static double Exp(double x) => Math.Exp(x);
    public static double Atan2(double y, double x) => Math.Atan2(y, x);
    public static double Sqrt(double x) => Math.Sqrt(x);

    /// <summary>math.radians: x * (pi / 180).</summary>
    public static double Radians(double deg) => deg * DegToRad;

    /// <summary>
    /// math.hypot(x, y) as CPython 3.11+ computes it for two finite arguments: scale to [0.5, 1) by the larger
    /// exponent, square and sum losslessly (Dekker products, fast two-sum), take the root and apply CPython's one-step
    /// differential correction. MEASURED: bit-identical to Python 3.11.9 (Blender) on 200,000 pairs
    /// (build/03_generator.md sec 4.2); Python 3.9's older hypot differs from it by 1 ulp in 38 % of those pairs.
    /// </summary>
    public static double Hypot(double x, double y)
    {
        x = Math.Abs(x);
        y = Math.Abs(y);
        var max = x > y ? x : y;
        if (max == 0.0 || double.IsInfinity(max) || double.IsNaN(max))
            return double.IsNaN(x) || double.IsNaN(y) ? double.NaN : max;
        if (x == 0.0 || y == 0.0)
            return max;
        return VectorNorm2(x, y, max);
    }

    // CPython 3.11 Modules/mathmodule.c vector_norm(), specialised to n = 2 (literal port; see build/03_generator.md sec 4.2).
    static double VectorNorm2(double x0, double x1, double max)
    {
        int maxE;
        Frexp(max, out maxE);
        if (maxE < -1023)
        {
            // subnormal: CPython rescales by DBL_MIN and recurses; window-sized geometry never gets here
            const double DblMin = 2.2250738585072014e-308;
            return DblMin * VectorNorm2(x0 / DblMin, x1 / DblMin, max / DblMin);
        }
        var scale = Ldexp(1.0, -maxE);
        double csum = 1.0, frac1 = 0.0, frac2 = 0.0, hi, lo, smHi, smLo;
        DlMul(x0 * scale, x0 * scale, out hi, out lo);   // lossless squaring
        DlFastSum(csum, hi, out smHi, out smLo);          // lossless addition
        csum = smHi;
        frac1 += lo;
        frac2 += smLo;
        DlMul(x1 * scale, x1 * scale, out hi, out lo);
        DlFastSum(csum, hi, out smHi, out smLo);
        csum = smHi;
        frac1 += lo;
        frac2 += smLo;
        var h = Math.Sqrt(csum - 1.0 + (frac1 + frac2));
        DlMul(-h, h, out hi, out lo);
        DlFastSum(csum, hi, out smHi, out smLo);
        csum = smHi;
        frac1 += lo;
        frac2 += smLo;
        var x = csum - 1.0 + (frac1 + frac2);
        h += x / (2.0 * h);                               // differential correction
        return h / scale;
    }

    // dl_fast_sum: Dekker (5.4); needs |a| >= |b|
    static void DlFastSum(double a, double b, out double hi, out double lo)
    {
        var x = a + b;
        var z = x - a;
        var y = b - z;
        hi = x;
        lo = y;
    }

    // dl_split + dl_mul (CPython 3.11, no fma): Dekker (5.5), (5.6), (5.12)
    static void DlMul(double x, double y, out double hi, out double lo)
    {
        var t = x * 134217729.0;   // Veltkamp constant 2^27 + 1
        var xh = t - (t - x);
        var xl = x - xh;
        t = y * 134217729.0;
        var yh = t - (t - y);
        var yl = y - yh;
        var p = xh * yh;
        var q = xh * yl + xl * yh;
        var z = p + q;
        var zz = p - z + q + xl * yl;
        hi = z;
        lo = zz;
    }

    public static double Frexp(double d, out int e)
    {
        if (d == 0.0 || double.IsInfinity(d) || double.IsNaN(d))
        {
            e = 0;
            return d;
        }
        var bits = BitConverter.DoubleToInt64Bits(d);
        var exp = (int)((bits >> 52) & 0x7FF);
        if (exp == 0)
        {
            // subnormal: normalise
            d *= 18014398509481984.0;   // 2^54
            bits = BitConverter.DoubleToInt64Bits(d);
            exp = (int)((bits >> 52) & 0x7FF) - 54;
        }
        e = exp - 1022;
        bits = (bits & unchecked((long)0x800FFFFFFFFFFFFFUL)) | (1022L << 52);
        return BitConverter.Int64BitsToDouble(bits);
    }

    public static double Ldexp(double x, int e)
    {
        // exact for the ranges used here (powers of two times a normal number)
        while (e > 1000) { x *= 1.0715086071862673e301; e -= 1000; }      // 2^1000
        while (e < -1000) { x *= 9.332636185032189e-302; e += 1000; }     // 2^-1000
        if (e >= -1022)
            return x * BitConverter.Int64BitsToDouble((long)(e + 1023) << 52);
        return x * BitConverter.Int64BitsToDouble((long)(e + 1023 + 54) << 52) * 5.551115123125783e-17;   // 2^-54
    }

    /// <summary>Python's float %: the result takes the sign of the divisor.</summary>
    public static double PyMod(double a, double b)
    {
        var m = a % b;   // C# % on doubles is the exact truncated remainder (fmod)
        if (m != 0.0)
        {
            if ((b < 0.0) != (m < 0.0))
                m += b;
        }
        else
        {
            m = b < 0.0 ? -0.0 : 0.0;
        }
        return m;
    }

    static readonly double[] Pow10 =
    {
        1e0, 1e1, 1e2, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9, 1e10, 1e11, 1e12, 1e13, 1e14, 1e15
    };

    static readonly ulong[] Pow10U =
    {
        1UL, 10UL, 100UL, 1000UL, 10000UL, 100000UL, 1000000UL, 10000000UL, 100000000UL, 1000000000UL,
        10000000000UL, 100000000000UL, 1000000000000UL, 10000000000000UL, 100000000000000UL, 1000000000000000UL
    };

    /// <summary>
    /// Python's round(x, nd) for 0 &lt;= nd &lt;= 15: the exact binary value of x is rounded half-to-even to nd
    /// decimals, and the result is the double nearest that decimal. No allocation.
    /// </summary>
    public static double PyRound(double x, int nd)
    {
        long q;
        if (!PyRoundScaled(x, nd, out q))
            return x;
        var r = q / Pow10[nd];
        if (r == 0.0 && x < 0.0)
            return -0.0;
        return r;
    }

    /// <summary>
    /// The integer q = round_half_even(x * 10^nd) on the exact value of x. False when x is not finite, is an
    /// integer-valued double too large to matter, or |q| would not fit (the caller then keeps x).
    /// </summary>
    public static bool PyRoundScaled(double x, int nd, out long q)
    {
        q = 0;
        if (double.IsNaN(x) || double.IsInfinity(x) || nd < 0 || nd > 15)
            return false;
        if (x == 0.0)
            return true;
        var bits = BitConverter.DoubleToInt64Bits(x);
        var neg = bits < 0;
        var exp = (int)((bits >> 52) & 0x7FF);
        var mant = (ulong)(bits & 0xFFFFFFFFFFFFFL);
        int e2;
        if (exp == 0)
            e2 = -1074;
        else
        {
            mant |= 1UL << 52;
            e2 = exp - 1075;
        }
        // x = mant * 2^e2 exactly. N = mant * 10^nd as a 128-bit integer (hi, lo).
        ulong hi, lo;
        Mul64(mant, Pow10U[nd], out hi, out lo);
        ulong qq;
        if (e2 >= 0)
        {
            // x * 10^nd is an integer; only small integers fit a long
            if (e2 > 10 || hi != 0)
                return false;
            if (lo > (ulong.MaxValue >> e2) >> 1)
                return false;
            qq = lo << e2;
        }
        else
        {
            var k = -e2;
            if (k >= 128)
            {
                qq = 0;   // far below half a unit
            }
            else
            {
                // quotient = N >> k, remainder = N mod 2^k, half = 2^(k-1)
                ulong qhi, qlo;
                Shr128(hi, lo, k, out qhi, out qlo);
                if (qhi != 0 || qlo > long.MaxValue)
                    return false;
                qq = qlo;
                // remainder compare: rem = N - (q << k)
                ulong bhi, blo;
                Shl128(qhi, qlo, k, out bhi, out blo);
                ulong rhi, rlo;
                Sub128(hi, lo, bhi, blo, out rhi, out rlo);
                ulong hhi, hlo;
                Shl128(0, 1, k - 1, out hhi, out hlo);
                var cmp = rhi != hhi ? (rhi < hhi ? -1 : 1) : (rlo != hlo ? (rlo < hlo ? -1 : 1) : 0);
                if (cmp > 0 || (cmp == 0 && (qq & 1UL) == 1UL))
                    qq++;
            }
        }
        q = neg ? -(long)qq : (long)qq;
        return true;
    }

    static void Mul64(ulong a, ulong b, out ulong hi, out ulong lo)
    {
        ulong aL = a & 0xFFFFFFFFUL, aH = a >> 32, bL = b & 0xFFFFFFFFUL, bH = b >> 32;
        var ll = aL * bL;
        var lh = aL * bH;
        var hl = aH * bL;
        var hh = aH * bH;
        var mid = (ll >> 32) + (lh & 0xFFFFFFFFUL) + (hl & 0xFFFFFFFFUL);
        lo = (ll & 0xFFFFFFFFUL) | (mid << 32);
        hi = hh + (lh >> 32) + (hl >> 32) + (mid >> 32);
    }

    static void Shr128(ulong hi, ulong lo, int k, out ulong rhi, out ulong rlo)
    {
        if (k == 0) { rhi = hi; rlo = lo; return; }
        if (k >= 64) { rhi = 0; rlo = k >= 128 ? 0 : hi >> (k - 64); return; }
        rlo = (lo >> k) | (hi << (64 - k));
        rhi = hi >> k;
    }

    static void Shl128(ulong hi, ulong lo, int k, out ulong rhi, out ulong rlo)
    {
        if (k == 0) { rhi = hi; rlo = lo; return; }
        if (k >= 64) { rlo = 0; rhi = k >= 128 ? 0 : lo << (k - 64); return; }
        rhi = (hi << k) | (lo >> (64 - k));
        rlo = lo << k;
    }

    static void Sub128(ulong ahi, ulong alo, ulong bhi, ulong blo, out ulong rhi, out ulong rlo)
    {
        rlo = unchecked(alo - blo);
        rhi = unchecked(ahi - bhi - (alo < blo ? 1UL : 0UL));
    }

    /// <summary>Python int(round(x)) for the piece key: round half to even, like C# Math.Round's default.</summary>
    public static long RoundHalfEven(double x) => (long)Math.Round(x, MidpointRounding.ToEven);
}

/// <summary>
/// Python's set iteration order for small sets of small non-negative ints (CPython setobject.c: hash(i) = i,
/// 8-slot minimum table, LINEAR_PROBES 9, PERTURB_SHIFT 5, resize at fill*5 &gt;= mask*3). The face walk of the
/// reference discovers faces in this order, and the order reaches the output (stage-2 chips), so the port keeps it.
/// </summary>
public sealed class GlassPySet
{
    int[] table = new int[8];
    int mask = 7, fill;
    public int Count => fill;

    public void Clear()
    {
        if (table.Length != 8)
            table = new int[8];
        for (var i = 0; i < table.Length; i++)
            table[i] = -1;
        mask = 7;
        fill = 0;
    }

    public GlassPySet()
    {
        Clear();
    }

    public void Add(int key)
    {
        if (!Insert(table, mask, key, true))
            return;
        fill++;
        if (fill * 5 < mask * 3)
            return;
        // set_table_resize(so, used > 50000 ? used * 2 : used * 4)
        var minused = fill > 50000 ? fill * 2 : fill * 4;
        var newsize = 8;
        while (newsize <= minused)
            newsize <<= 1;
        var old = table;
        table = new int[newsize];
        for (var i = 0; i < newsize; i++)
            table[i] = -1;
        mask = newsize - 1;
        for (var i = 0; i < old.Length; i++)
            if (old[i] >= 0)
                InsertClean(table, mask, old[i]);
    }

    // set_add_entry: returns false when the key is already present
    static bool Insert(int[] t, int mask, int key, bool checkDup)
    {
        var hash = (ulong)key;
        var i = hash & (ulong)mask;
        var perturb = hash;
        while (true)
        {
            var probes = (i + 9 <= (ulong)mask) ? 9 : 0;
            var j = i;
            do
            {
                if (t[j] < 0)
                {
                    t[j] = key;
                    return true;
                }
                if (checkDup && t[j] == key)
                    return false;
                j++;
            } while (probes-- > 0);
            perturb >>= 5;
            i = (i * 5 + 1 + perturb) & (ulong)mask;
        }
    }

    static void InsertClean(int[] t, int mask, int key)
    {
        var hash = (ulong)key;
        var i = hash & (ulong)mask;
        var perturb = hash;
        while (true)
        {
            if (t[i] < 0)
            {
                t[i] = key;
                return;
            }
            if (i + 9 <= (ulong)mask)
            {
                for (var j = 1; j <= 9; j++)
                    if (t[i + (ulong)j] < 0)
                    {
                        t[i + (ulong)j] = key;
                        return;
                    }
            }
            perturb >>= 5;
            i = (i * 5 + 1 + perturb) & (ulong)mask;
        }
    }

    /// <summary>Appends the members in iteration (slot) order.</summary>
    public void CopyTo(List<int> dst)
    {
        for (var i = 0; i < table.Length; i++)
            if (table[i] >= 0)
                dst.Add(table[i]);
    }
}

/// <summary>2D polygon helpers on CCW polygons (crack_graph_ref.py "2D polygon helpers"), allocation-free on pooled lists.</summary>
public static class GlassPoly
{
    public static double Area(List<GlassV2> poly)
    {
        var s = 0.0;
        var n = poly.Count;
        for (var i = 0; i < n; i++)
        {
            var p = poly[i];
            var q = poly[(i + 1) % n];
            s += p.x * q.y - q.x * p.y;
        }
        return 0.5 * s;
    }

    public static double Area(List<GlassV2> verts, int v0, int vn)
    {
        var s = 0.0;
        for (var i = 0; i < vn; i++)
        {
            var p = verts[v0 + i];
            var q = verts[v0 + (i + 1) % vn];
            s += p.x * q.y - q.x * p.y;
        }
        return 0.5 * s;
    }

    public static GlassV2 Centroid(List<GlassV2> poly)
    {
        var a = Area(poly);
        var n = poly.Count;
        if (Math.Abs(a) < 1e-18)
        {
            double sx = 0.0, sy = 0.0;
            for (var i = 0; i < n; i++)
                sx += poly[i].x;
            for (var i = 0; i < n; i++)
                sy += poly[i].y;
            return new GlassV2(sx / n, sy / n);
        }
        double cx = 0.0, cy = 0.0;
        for (var i = 0; i < n; i++)
        {
            var p = poly[i];
            var q = poly[(i + 1) % n];
            var f = p.x * q.y - q.x * p.y;
            cx += (p.x + q.x) * f;
            cy += (p.y + q.y) * f;
        }
        return new GlassV2(cx / (6.0 * a), cy / (6.0 * a));
    }

    /// <summary>Keep a*x + b*y &lt;= c (Sutherland-Hodgman, one plane). dst is cleared first.</summary>
    public static void ClipHalf(List<GlassV2> poly, double a, double b, double c, List<GlassV2> dst)
    {
        dst.Clear();
        var n = poly.Count;
        if (n == 0)
            return;
        for (var i = 0; i < n; i++)
        {
            var p = poly[i];
            var q = poly[(i + 1) % n];
            var dp = a * p.x + b * p.y - c;
            var dq = a * q.x + b * q.y - c;
            if (dp <= 0.0)
                dst.Add(p);
            if ((dp < 0.0 && 0.0 < dq) || (dq < 0.0 && 0.0 < dp))
            {
                var t = dp / (dp - dq);
                dst.Add(new GlassV2(p.x + (q.x - p.x) * t, p.y + (q.y - p.y) * t));
            }
        }
    }

    /// <summary>Drop repeated and exactly collinear vertices. dst is cleared first; src is not changed.</summary>
    public static void Clean(List<GlassV2> poly, List<GlassV2> pts, double epsLen = 1e-9, double epsCross = 1e-13)
    {
        pts.Clear();
        for (var i = 0; i < poly.Count; i++)
        {
            var p = poly[i];
            if (pts.Count == 0 || Math.Abs(p.x - pts[pts.Count - 1].x) > epsLen || Math.Abs(p.y - pts[pts.Count - 1].y) > epsLen)
                pts.Add(p);
        }
        while (pts.Count > 1 && Math.Abs(pts[0].x - pts[pts.Count - 1].x) <= epsLen && Math.Abs(pts[0].y - pts[pts.Count - 1].y) <= epsLen)
            pts.RemoveAt(pts.Count - 1);
        var changed = true;
        while (changed && pts.Count > 3)
        {
            changed = false;
            var n = pts.Count;
            for (var i = 0; i < n; i++)
            {
                var a = pts[(i - 1 + n) % n];
                var b = pts[i];
                var c = pts[(i + 1) % n];
                var cr = (b.x - a.x) * (c.y - b.y) - (b.y - a.y) * (c.x - b.x);
                var dot = (b.x - a.x) * (c.x - b.x) + (b.y - a.y) * (c.y - b.y);
                if (Math.Abs(cr) <= epsCross && dot > 0.0)
                {
                    pts.RemoveAt(i);
                    changed = true;
                    break;
                }
            }
        }
    }

    public static bool IsConvex(List<GlassV2> poly, double eps = 1e-12)
    {
        var n = poly.Count;
        if (n < 3)
            return false;
        for (var i = 0; i < n; i++)
        {
            var a = poly[(i - 1 + n) % n];
            var b = poly[i];
            var c = poly[(i + 1) % n];
            var cr = (b.x - a.x) * (c.y - b.y) - (b.y - a.y) * (c.x - b.x);
            if (cr < -eps)
                return false;
        }
        return Area(poly) > 0.0;
    }

    public static double Orient(GlassV2 a, GlassV2 b, GlassV2 c) => (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);

    static bool OnSeg(GlassV2 a, GlassV2 b, GlassV2 c) =>
        Math.Min(a.x, b.x) - 1e-12 <= c.x && c.x <= Math.Max(a.x, b.x) + 1e-12 &&
        Math.Min(a.y, b.y) - 1e-12 <= c.y && c.y <= Math.Max(a.y, b.y) + 1e-12;

    /// <summary>True when two segments that share no endpoint touch or cross anywhere.</summary>
    public static bool SegmentsCross(GlassV2 p1, GlassV2 p2, GlassV2 p3, GlassV2 p4, double eps = 1e-14)
    {
        var d1 = Orient(p3, p4, p1);
        var d2 = Orient(p3, p4, p2);
        var d3 = Orient(p1, p2, p3);
        var d4 = Orient(p1, p2, p4);
        if (((d1 > eps && d2 < -eps) || (d1 < -eps && d2 > eps)) && ((d3 > eps && d4 < -eps) || (d3 < -eps && d4 > eps)))
            return true;
        if (Math.Abs(d1) <= eps && OnSeg(p3, p4, p1))
            return true;
        if (Math.Abs(d2) <= eps && OnSeg(p3, p4, p2))
            return true;
        if (Math.Abs(d3) <= eps && OnSeg(p1, p2, p3))
            return true;
        if (Math.Abs(d4) <= eps && OnSeg(p1, p2, p4))
            return true;
        return false;
    }

    /// <summary>diameter(poly): (d, i, j) of the first farthest pair (i &lt; j).</summary>
    public static double Diameter(List<GlassV2> poly, out int bi, out int bj)
    {
        var best = 0.0;
        bi = 0;
        bj = 0;
        var n = poly.Count;
        for (var i = 0; i < n; i++)
            for (var j = i + 1; j < n; j++)
            {
                var d = GlassMath.Hypot(poly[i].x - poly[j].x, poly[i].y - poly[j].y);
                if (d > best)
                {
                    best = d;
                    bi = i;
                    bj = j;
                }
            }
        return best;
    }

    static int ComparePoint(GlassV2 a, GlassV2 b)
    {
        if (a.x < b.x) return -1;
        if (a.x > b.x) return 1;
        if (a.y < b.y) return -1;
        if (a.y > b.y) return 1;
        return 0;
    }

    static double Cross(GlassV2 o, GlassV2 a, GlassV2 b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);

    static readonly Comparison<GlassV2> PointOrder = ComparePoint;

    /// <summary>convex_hull(points): sorted(set(points)), monotone chain. pts is sorted in place (scratch).</summary>
    public static void ConvexHull(List<GlassV2> pts, List<GlassV2> lower, List<GlassV2> upper, List<GlassV2> dst)
    {
        dst.Clear();
        pts.Sort(PointOrder);
        // set(): drop exact duplicates (sorted, so they are adjacent)
        var w = 0;
        for (var i = 0; i < pts.Count; i++)
            if (w == 0 || ComparePoint(pts[w - 1], pts[i]) != 0)
                pts[w++] = pts[i];
        pts.RemoveRange(w, pts.Count - w);
        if (pts.Count < 3)
        {
            dst.AddRange(pts);
            return;
        }
        lower.Clear();
        upper.Clear();
        for (var i = 0; i < pts.Count; i++)
        {
            var p = pts[i];
            while (lower.Count >= 2 && Cross(lower[lower.Count - 2], lower[lower.Count - 1], p) <= 0)
                lower.RemoveAt(lower.Count - 1);
            lower.Add(p);
        }
        for (var i = pts.Count - 1; i >= 0; i--)
        {
            var p = pts[i];
            while (upper.Count >= 2 && Cross(upper[upper.Count - 2], upper[upper.Count - 1], p) <= 0)
                upper.RemoveAt(upper.Count - 1);
            upper.Add(p);
        }
        for (var i = 0; i < lower.Count - 1; i++)
            dst.Add(lower[i]);
        for (var i = 0; i < upper.Count - 1; i++)
            dst.Add(upper[i]);
    }

    /// <summary>Intersection area of two convex CCW polygons (validation).</summary>
    public static double ConvexOverlapArea(List<GlassV2> a, List<GlassV2> b, List<GlassV2> s0, List<GlassV2> s1)
    {
        s0.Clear();
        s0.AddRange(a);
        var n = b.Count;
        for (var i = 0; i < n; i++)
        {
            var p = b[i];
            var q = b[(i + 1) % n];
            var ex = q.x - p.x;
            var ey = q.y - p.y;
            ClipHalf(s0, ey, -ex, ey * p.x - ex * p.y, s1);
            var t = s0;
            s0 = s1;
            s1 = t;
            if (s0.Count < 3)
                return 0.0;
        }
        return Math.Max(0.0, Area(s0));
    }

    public static double DistPointSeg(GlassV2 q, GlassV2 a, GlassV2 b)
    {
        var ex = b.x - a.x;
        var ey = b.y - a.y;
        var l2 = ex * ex + ey * ey;
        var t = l2 <= 0.0 ? 0.0 : Math.Max(0.0, Math.Min(1.0, ((q.x - a.x) * ex + (q.y - a.y) * ey) / l2));
        return GlassMath.Hypot(q.x - a.x - t * ex, q.y - a.y - t * ey);
    }

    /// <summary>_ray_rect_exit: distance along (dx, dy) from (ox, oy) to the rectangle's boundary.</summary>
    public static double RayRectExit(double ox, double oy, double dx, double dy, double x0, double x1, double y0, double y1)
    {
        var t = double.PositiveInfinity;
        if (dx > 1e-15)
            t = Math.Min(t, (x1 - ox) / dx);
        else if (dx < -1e-15)
            t = Math.Min(t, (x0 - ox) / dx);
        if (dy > 1e-15)
            t = Math.Min(t, (y1 - oy) / dy);
        else if (dy < -1e-15)
            t = Math.Min(t, (y0 - oy) / dy);
        return Math.Max(t, 0.0);
    }
}
