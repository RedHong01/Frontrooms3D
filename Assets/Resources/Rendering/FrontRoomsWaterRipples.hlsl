#ifndef FRONTROOMS_WATER_RIPPLES_INCLUDED
#define FRONTROOMS_WATER_RIPPLES_INCLUDED
// The pool family's water: the constant ripple, its roughness, and the rings (research/bathroom_rooms/70_look_build.md,
// "Ripples"; 17_water.md 2.2, 3, 7 items 3-4). Game visual line. Include after URP's Core.hlsl.
//
// Every input is a global set by FrontRoomsWaterRipples.cs (Tick) and FrontRoomsWaterRings.cs; with nothing set
// (_FR_RippleA.w == 0, _FR_RingCount == 0) the functions return flat, calm water, so including this changes nothing.
//
//   FRRipple r = FRWaterRipples(positionWS.xz);     // fragment stage, uniform control flow (it uses ddx / ddy)
//   float3 n = FRRippleNormal(r.slope);              // the surface normal the pixel shows
//   float2 alpha = FRRippleAlpha(r, alpha0);         // GGX roughness along r.axis (the view's long footprint) and across
//
// What a pixel cannot show becomes roughness (the game has MSAA and no TAA): the waves finer than its footprint (long
// along the view, so lamp reflections stretch into streaks), the waves too fast for the bake's frame step, what
// blending two frames loses, and the rings' crests finer than a pixel. r.cov carries all of it (slope covariance,
// rad^2, world xz); r.slope is only what the pixel resolves.
//
// The constant ripple (Tools/lookdev/pool_water/gen_pool_ripples.py): capillary-gravity, isotropic, looping exactly.
//   fine  5 mm - 7 cm, patch 1.2 m (desktop 512^2 x 64 slices over 0.50 s; phone tier 256^2 x 32 over 0.32 s), sampled
//         twice on desktop: A as baked, B scaled 8/7, turned by the 3-4-5 angle and played at 0.8613 x (the phase
//         speeds stay right and A + B never repeats exactly); the phone tier samples A alone (4 fetches fewer)
//   long  7 - 30 cm, patch 2.4 m, 128^2 x 128 slices over 2.56 s, turned by the 4-3-5 angle
// Every patch divides 192 m along both world axes, so the room stream's origin rebase never moves the pattern.
// A slow, faint mask (calm patches, 17 3.3 item 4) scales the whole ripple by 1 +- _FR_RippleEnc.w (desktop; 0 on phones).
// Rings (FrontRoomsWaterRings.cs): Cauchy-Poisson packets in closed form, a calm centre whose rim grows at the minimum
// group speed (0.178 m/s), the gravity crests running out through the packet and the capillaries racing ahead.

TEXTURE2D_ARRAY(_FR_RippleFine);  SAMPLER(sampler_FR_RippleFine);
TEXTURE2D_ARRAY(_FR_RippleLong);  SAMPLER(sampler_FR_RippleLong);
TEXTURE2D(_FR_RippleLut);         SAMPLER(sampler_FR_RippleLut);
TEXTURE2D(_FR_RingLut);           SAMPLER(sampler_FR_RingLut);

float4 _FR_RippleA;      // fine layer A: slice 0, slice 1, blend, 1 when loaded
float4 _FR_RippleB;      // fine layer B: slice 0, slice 1, blend, 1 when used (desktop; the phone tier samples A only)
float4 _FR_RippleL;      // long layer:   slice 0, slice 1, blend, -
float4 _FR_RippleEnc;    // slope at texel value 1 (rad): fine, long; gain (1 = calm, 2.0 deg rms); patch mask depth
float4 _FR_RippleVar;    // per-axis slope variance (rad^2): fine resolved, long resolved, fine sub-band, long sub-band
float4 _FR_RippleLoss;   // frame-blend loss D: fine, long; mask clock (s); -
float4 _FR_RippleGeo;    // texels per metre: fine, long; 1 / fine patch, 1 / long patch
float4 _FR_RingConst0;   // c_g min (m/s), k at it (rad/m), omega there (rad/s), omega''' there
float4 _FR_RingConst1;   // g, sigma / rho, nu_eff, 1 / fade time
float4 _FR_RingConst2;   // fastest group speed in the table (m/s), table size, Ai(x >= 0) p1, p2
float4 _FR_RingAiry0;    // Ai(x) on [-2.6, 0]: x^6 .. x^3
float4 _FR_RingAiry1;    // x^2, x, 1, -
float4 _FR_Ring0[16];    // per slot: centre x, centre z, age (s), volume (m^3, faded)
float4 _FR_Ring1[16];    // per slot: radius a, splash radius, splash share, agitation
float  _FR_RingCount;

#define FR_RIPPLE_LUT_N 19.0
#define FR_RIPPLE_B_SCALE 0.875          // 1 / (8 / 7)
#define FR_RIPPLE_PI 3.14159265

struct FRRipple
{
    float2 slope;       // dh/dx, dh/dz the pixel resolves
    float3 cov;         // unresolved slope covariance xx, zz, xz (rad^2)
    float2 axis;        // the footprint's long axis in world xz (unit)
    float  height;      // ring height (m); the constant ripple's is not baked (under 0.3 mm)
    float  agitation;   // 0 calm; about 1 where a ring has just stirred the water
};

float3 FRRippleNormal(float2 slope)
{
    return normalize(float3(-slope.x, 1.0, -slope.y));
}

// GGX alphas along r.axis and across it (Beckmann's per-axis slope variance alpha^2 / 2)
float2 FRRippleAlpha(FRRipple r, float alpha0)
{
    float2 ax = r.axis;
    float2 pe = float2(-ax.y, ax.x);
    float vA = r.cov.x * ax.x * ax.x + r.cov.y * ax.y * ax.y + 2.0 * r.cov.z * ax.x * ax.y;
    float vP = r.cov.x * pe.x * pe.x + r.cov.y * pe.y * pe.y + 2.0 * r.cov.z * pe.x * pe.y;
    return sqrt(alpha0 * alpha0 + 2.0 * max(float2(vA, vP), 0.0));
}

// The pixel footprint on the water (metres per pixel along its long and short axes) from the screen derivatives
void FRRippleFootprint(float2 xz, out float2 axis, out float lMaj, out float lMin)
{
    float2 dx = ddx(xz);
    float2 dy = ddy(xz);
    float c00 = dx.x * dx.x + dy.x * dy.x;
    float c11 = dx.y * dx.y + dy.y * dy.y;
    float c01 = dx.x * dx.y + dy.x * dy.y;
    float h = 0.5 * (c00 + c11);
    float disc = sqrt(max(h * h - (c00 * c11 - c01 * c01), 0.0));
    float l1 = h + disc;
    float l2 = max(h - disc, 0.0);
    lMaj = sqrt(l1);
    lMin = sqrt(l2);
    float2 e = abs(c01) > 1e-14 ? float2(c01, l1 - c00) : (c00 >= c11 ? float2(1.0, 0.0) : float2(0.0, 1.0));
    axis = e * rsqrt(max(dot(e, e), 1e-30));
}

// Unresolved share of a layer's per-axis variance for a footprint (texels): rg fine, ba long. Under one texel the
// bilinear reconstruction shows what the texels hold, so the share fades to 0 there.
float2 FRRippleLutShare(float lMajTex, float lMinTex, bool longLayer)
{
    float2 l2 = clamp(log2(max(float2(lMajTex, lMinTex), 1e-4)) * 2.0, 0.0, FR_RIPPLE_LUT_N - 1.0);
    float4 t = SAMPLE_TEXTURE2D_LOD(_FR_RippleLut, sampler_FR_RippleLut, (l2 + 0.5) / FR_RIPPLE_LUT_N, 0);
    float2 s = longLayer ? t.ba : t.rg;
    return s * saturate(float2(lMajTex, lMinTex));
}

// Calm patches: smooth value noise over 3.2 m cells and 24 s, wrapping every 60 cells (192 m) and 256 time cells
float FRRippleHash(float3 p)
{
    p = frac(p * float3(0.1031, 0.1030, 0.0973));
    p += dot(p, p.yzx + 33.33);
    return frac((p.x + p.y) * p.z);
}

float FRRippleMask(float2 xz, float t)
{
    float3 x = float3(xz / 3.2, t / 24.0);
    float3 i = floor(x);
    float3 f = x - i;
    f = f * f * (3.0 - 2.0 * f);
    float3 w = float3(60.0, 60.0, 256.0);
    float3 i0 = i - w * floor(i / w);
    float3 i1 = (i0 + 1.0) - w * floor((i0 + 1.0) / w);
    float n000 = FRRippleHash(float3(i0.x, i0.y, i0.z)), n100 = FRRippleHash(float3(i1.x, i0.y, i0.z));
    float n010 = FRRippleHash(float3(i0.x, i1.y, i0.z)), n110 = FRRippleHash(float3(i1.x, i1.y, i0.z));
    float n001 = FRRippleHash(float3(i0.x, i0.y, i1.z)), n101 = FRRippleHash(float3(i1.x, i0.y, i1.z));
    float n011 = FRRippleHash(float3(i0.x, i1.y, i1.z)), n111 = FRRippleHash(float3(i1.x, i1.y, i1.z));
    float n = lerp(lerp(lerp(n000, n100, f.x), lerp(n010, n110, f.x), f.y),
                   lerp(lerp(n001, n101, f.x), lerp(n011, n111, f.x), f.y), f.z);
    return n * 2.0 - 1.0;
}

float FRRingAiry(float x)
{
    if (x >= 0.0) return 0.35503 * exp(-_FR_RingConst2.z * x - _FR_RingConst2.w * x * sqrt(x));
    float p = _FR_RingAiry0.x;
    p = p * x + _FR_RingAiry0.y;
    p = p * x + _FR_RingAiry0.z;
    p = p * x + _FR_RingAiry0.w;
    p = p * x + _FR_RingAiry1.x;
    p = p * x + _FR_RingAiry1.y;
    p = p * x + _FR_RingAiry1.z;
    return p;
}

// Adds every ring slot: slope (filtered by the footprint along the ring's radius), its sub-pixel crests as roughness,
// height and the agitation of the water near a fresh disturbance.
void FRRingsAdd(float2 xz, float2 axis, float lMaj, float lMin, inout float2 slope, inout float3 cov,
                inout float height, inout float agitation)
{
    int n = (int)_FR_RingCount;
    if (n <= 0) return;
    float cg = _FR_RingConst0.x, km = _FR_RingConst0.y, omm = _FR_RingConst0.z, om3 = _FR_RingConst0.w;
    float gr = _FR_RingConst1.x, sr = _FR_RingConst1.y, nu = _FR_RingConst1.z, ifade = _FR_RingConst1.w;
    float vmax = _FR_RingConst2.x, lutN = _FR_RingConst2.y;
    [loop] for (int i = 0; i < 16; i++)
    {
        if (i >= n) break;
        float4 r0 = _FR_Ring0[i];
        float4 r1 = _FR_Ring1[i];
        float2 d = xz - r0.xy;
        float r = max(length(d), 1e-3);
        float t = max(r0.z, 1e-3);
        float2 dir = d / r;
        float pr = r1.x + 0.05 * t + 0.03;                                // the stirred patch spreads at 5 cm/s
        if (r1.w > 0.0 && r < 4.0 * pr) agitation += r1.w * exp(-0.5 * r * r / (pr * pr)) * exp(-t / 1.5);
        // the ring's fastest front: the group speed of its shortest strong waves (splash, k = 2.5 / aS) or of its
        // longest (body, k = 0.5 / a), whichever is faster; nothing reaches beyond it
        float kc = 2.5 / max(r1.y, 1e-3), kg = 0.5 / max(r1.x, 1e-3);
        float vf = max((gr + 3.0 * sr * kc * kc) / (2.0 * sqrt(gr * kc + sr * kc * kc * kc)),
                       (gr + 3.0 * sr * kg * kg) / (2.0 * sqrt(gr * kg + sr * kg * kg * kg)));
        if (r0.w <= 0.0 || r > min(vf, vmax) * t + 3.0 * r1.x + 0.05) continue;
        float sc = pow(2.0 / (t * om3), 1.0 / 3.0);
        float X = -(r - cg * t) * sc;
        if (X > 6.0) continue;
        float ca = dot(axis, dir);
        float cp = axis.x * dir.y - axis.y * dir.x;
        float lr2 = lMaj * lMaj * ca * ca + lMin * lMin * cp * cp;      // footprint^2 along the radius
        float vol = r0.w / (2.0 * FR_RIPPLE_PI);
        float eta = 0.0, dr = 0.0, vr = 0.0;
        float bw = saturate((X + 2.5) / 0.7);                             // 1 at the rim, 0 beyond X = -2.5
        if (bw > 0.0)
        {
            float F = vol * ((1.0 - r1.z) * exp(-0.5 * km * km * r1.x * r1.x) + r1.z * exp(-0.5 * km * km * r1.y * r1.y));
            float A = F * sqrt(2.0 * FR_RIPPLE_PI * km / r) * sc * FRRingAiry(X) * exp(-(2.0 * nu * km * km + ifade) * t) * bw;
            float ph = km * r - omm * t - 0.25 * FR_RIPPLE_PI;
            float H = exp(-km * km * lr2 / 24.0);
            float sn, cs;
            sincos(ph, sn, cs);
            eta += A * cs;
            dr += -A * km * sn * H;
            vr += 0.5 * A * A * km * km * (1.0 - H * H);
        }
        if (bw < 1.0 && r > cg * t)
        {
            float u = sqrt(saturate((r / t - cg) / (vmax - cg)));
            float2 kk = SAMPLE_TEXTURE2D_LOD(_FR_RingLut, sampler_FR_RingLut, float2((u * (lutN - 1.0) + 0.5) / lutN, 0.5), 0).rg;
            [unroll] for (int b = 0; b < 2; b++)
            {
                float k = b == 0 ? kk.x : kk.y;
                float om = sqrt(gr * k + sr * k * k * k);
                float om1 = (gr + 3.0 * sr * k * k) / (2.0 * om);
                float om2 = abs(3.0 * sr * k / om - om1 * om1 / om);
                float F = vol * ((1.0 - r1.z) * exp(-0.5 * k * k * r1.x * r1.x) + r1.z * exp(-0.5 * k * k * r1.y * r1.y));
                float A = F * sqrt(k / (r * t * max(om2, 1e-7))) * exp(-(2.0 * nu * k * k + ifade) * t) * (1.0 - bw);
                float ph = k * r - om * t - (b == 0 ? 0.0 : 0.5 * FR_RIPPLE_PI);
                float H = exp(-k * k * lr2 / 24.0);
                float sn, cs;
                sincos(ph, sn, cs);
                eta += A * cs;
                dr += -A * k * sn * H;
                vr += 0.5 * A * A * k * k * (1.0 - H * H);
            }
        }
        slope += dr * dir;
        cov += vr * float3(dir.x * dir.x, dir.y * dir.y, dir.x * dir.y);
        height += eta;
    }
}

FRRipple FRWaterRipples(float2 xz)
{
    FRRipple o;
    o.slope = 0.0;
    o.cov = 0.0;
    o.height = 0.0;
    o.agitation = 0.0;
    float lMaj, lMin;
    FRRippleFootprint(xz, o.axis, lMaj, lMin);
    FRRingsAdd(xz, o.axis, lMaj, lMin, o.slope, o.cov, o.height, o.agitation);
    if (_FR_RippleA.w <= 0.0) return o;

    // the constant ripple: fine A, fine B (scaled 8/7, turned 3-4-5, its own clock), long (turned 4-3-5)
    float2 uvA = xz * _FR_RippleGeo.z;
    float2 uvB = float2(0.6 * xz.x - 0.8 * xz.y, 0.8 * xz.x + 0.6 * xz.y) * (_FR_RippleGeo.z * FR_RIPPLE_B_SCALE) + float2(0.37, 0.61);
    float2 uvL = float2(0.8 * xz.x - 0.6 * xz.y, 0.6 * xz.x + 0.8 * xz.y) * _FR_RippleGeo.w + float2(0.19, 0.83);
    float2 gA = lerp(SAMPLE_TEXTURE2D_ARRAY(_FR_RippleFine, sampler_FR_RippleFine, uvA, _FR_RippleA.x).rg,
                     SAMPLE_TEXTURE2D_ARRAY(_FR_RippleFine, sampler_FR_RippleFine, uvA, _FR_RippleA.y).rg, _FR_RippleA.z) * 2.0 - 1.0;
    // layer B: desktop only (_FR_RippleB.w 1); the phone tier samples A alone at full weight
    bool useB = _FR_RippleB.w > 0.5;
    float2 gB = 0.0;
    if (useB)
        gB = lerp(SAMPLE_TEXTURE2D_ARRAY(_FR_RippleFine, sampler_FR_RippleFine, uvB, _FR_RippleB.x).rg,
                  SAMPLE_TEXTURE2D_ARRAY(_FR_RippleFine, sampler_FR_RippleFine, uvB, _FR_RippleB.y).rg, _FR_RippleB.z) * 2.0 - 1.0;
    float2 gL = lerp(SAMPLE_TEXTURE2D_ARRAY(_FR_RippleLong, sampler_FR_RippleLong, uvL, _FR_RippleL.x).rg,
                     SAMPLE_TEXTURE2D_ARRAY(_FR_RippleLong, sampler_FR_RippleLong, uvL, _FR_RippleL.y).rg, _FR_RippleL.z) * 2.0 - 1.0;
    float2 sB = float2(0.6 * gB.x + 0.8 * gB.y, -0.8 * gB.x + 0.6 * gB.y);
    float2 sL = float2(0.8 * gL.x + 0.6 * gL.y, -0.6 * gL.x + 0.8 * gL.y);
    float mask = _FR_RippleEnc.w > 0.0 ? FRRippleMask(xz, _FR_RippleLoss.z) : 0.0;     // off on the phone tier
    float m = (1.0 + _FR_RippleEnc.w * mask) * _FR_RippleEnc.z * (1.0 + 0.8 * o.agitation);
    float wF = useB ? 0.70710678 : 1.0;
    o.slope += m * ((gA + sB) * (wF * _FR_RippleEnc.x) + sL * _FR_RippleEnc.y);

    // roughness: the footprint's share of each layer, the sub-bands, the frame-blend loss of what stays resolved
    float tF = _FR_RippleGeo.x, tL = _FR_RippleGeo.y;
    float2 fa = FRRippleLutShare(lMaj * tF, lMin * tF, false);
    float2 fb = useB ? FRRippleLutShare(lMaj * tF * FR_RIPPLE_B_SCALE, lMin * tF * FR_RIPPLE_B_SCALE, false) : fa;
    float2 fl = FRRippleLutShare(lMaj * tL, lMin * tL, true);
    float vF = _FR_RippleVar.x, vL = _FR_RippleVar.y;
    float fBb = useB ? _FR_RippleB.z : _FR_RippleA.z;
    float lossF = _FR_RippleLoss.x * vF * 2.0 * (_FR_RippleA.z * (1.0 - _FR_RippleA.z) * (1.0 - 0.5 * (fa.x + fa.y))
                                                  + fBb * (1.0 - fBb) * (1.0 - 0.5 * (fb.x + fb.y)));
    float lossL = _FR_RippleLoss.y * vL * 4.0 * _FR_RippleL.z * (1.0 - _FR_RippleL.z) * (1.0 - 0.5 * (fl.x + fl.y));
    float iso = _FR_RippleVar.z + _FR_RippleVar.w + lossF + lossL;
    float vMaj = 0.5 * vF * (fa.x + fb.x) + vL * fl.x + iso;
    float vMin = 0.5 * vF * (fa.y + fb.y) + vL * fl.y + iso;
    float2 ax = o.axis;
    o.cov += (m * m) * (vMaj * float3(ax.x * ax.x, ax.y * ax.y, ax.x * ax.y) + vMin * float3(ax.y * ax.y, ax.x * ax.x, -ax.x * ax.y));
    return o;
}

#endif
