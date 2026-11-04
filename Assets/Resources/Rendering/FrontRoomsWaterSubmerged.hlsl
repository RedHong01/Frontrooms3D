#ifndef FRONTROOMS_WATER_SUBMERGED_INCLUDED
#define FRONTROOMS_WATER_SUBMERGED_INCLUDED
// The pool family's water: what lies under it and the light it throws (research/bathroom_rooms/70_look_build.md,
// "Submerged"; 17_water.md 4.1-4.4 and 7 items 2 and 8). Game visual line. Include after URP's Lighting.hlsl.
//
// Every input is a global set by FrontRoomsWaterSubmerged.cs. With no body of water registered (_FR_SubCount 0: the
// default, and the whole game outside a pool zone) FRSubFind returns body -1 at its first test and nothing else here
// runs, so every surface renders exactly as before.
//
// Under the water (FRSubmergedPBR, replacing UniversalFragmentPBR for that pixel): the colour of the water is what lies
// under it (17 4.2), per colour channel:
//   each light   crosses the surface (1 - F at its angle), bends (Snell: it arrives steeper, carrying cos ta / cos tw of
//                its irradiance per area across the beam) and loses exp(-a d / cos tw) on its way down; a light under the
//                water (a wet niche) loses exp(-a |L - P|) over its whole path; a compact source (<= 2 per room) also
//                carries the caustic of the ripple over its path (FRCausticFloor)
//   the ambient  the diffuse light from above: the LUT's R, the mean over the sky of (1 - F) exp(-tau / cos tw)
//   the glaze    against water a glaze (n 1.52) reflects about a tenth of what it does in air (_FR_SubMode.y)
//   the way out  exp(-a d / cos tv) along the view's refracted path, the trapping (the surface sends p = 0.475 of the
//                upwelling diffuse light back down, W26: 1 / (1 - albedo P(tau)), the LUT's G) and 1 / n^2 (radiance
//                leaving a denser medium); the surface's own 1 - F is the water shader's
// a = (0.29, 0.048, 0.006) per metre (17 4.1) times the zone's absorption scale (Q1: the club x1, the level x3-5).
//
// Above the water, in a pool room (FRSubThrown): the light the wet niches send out of the water, per lamp (it dims and
// dies with its lamp): the exit point on the flat surface solved exactly (Snell, Newton), the spreading of a flat
// interface (dr / dtw), the transmission out, and the long ripple's caustic over the path in air, blurred by the
// source's own size. Only the long cascade (7-30 cm) modulates it: large areas never move faster than its waves.
#include "Assets/Resources/Rendering/FrontRoomsWaterRipples.hlsl"

#define FR_SUB_MAX 12
#define FR_SUB_LAMPS 16
#define FR_SUB_N 1.333
#define FR_SUB_ENC 6.0
#define FR_SUB_LUT_N 64.0

float  _FR_SubCount;                 // bodies registered (0: nothing here runs)
float4 _FR_SubRect[FR_SUB_MAX];      // the water's rectangle (world): x min, z min, x max, z max
float4 _FR_SubRoom[FR_SUB_MAX];      // the room above it (world, the rectangle widened): x min, z min, x max, z max
float4 _FR_SubParam[FR_SUB_MAX];     // x water height (world y), y absorption scale (Q1), z basin albedo (trapping), w room top (world y)
float4 _FR_SubSrc0[FR_SUB_MAX];      // compact source 0 of the floor caustics: position (world), w its radius (m); w 0 = none
float4 _FR_SubSrc1[FR_SUB_MAX];      // compact source 1
float4 _FR_SubAbsorb;                // rgb clear water's absorption per metre; w 1 / n^2
float4 _FR_SubMode;                  // x caustics 0 off, 1 desktop loops, 2 phone flipbook, 3 WebGL scrolled layers; y glaze share under water; z caustic strength; w WebGL scroll clock (s)
float4 _FR_SubLampPos[FR_SUB_LAMPS]; // wet niches: position (world), w body index
float4 _FR_SubLampCol[FR_SUB_LAMPS]; // rgb the glow's intensity (linear, light units x m^2), w the lamp's live share (0 dead .. 1)
float  _FR_SubLampCount;
float4 _FR_SubThrown;                // x gain, y the glow source's radius (m), z -, w -
float4 _FR_CausticFineD;             // the fine loop's four lens distances (m)
float4 _FR_CausticLongD;             // the long loop's four
float4 _FR_CausticTex;               // texels per slice side: fine, long
TEXTURE2D(_FR_SubLut);               // u = tau / (tau + 1): R diffuse entry (1 - F and the path), G trapping P(tau)
TEXTURE2D_ARRAY(_FR_CausticFine);
TEXTURE2D_ARRAY(_FR_CausticLong);

struct FRSub
{
    int    body;      // -1: no water here
    float  waterY;
    float  depth;     // metres under the surface (0 above it)
    float3 a;         // absorption per metre, the zone's scale applied
    float  albedo;    // the basin's albedo, for the trapping
    bool   under;     // under the water of the body
    bool   room;      // above it, inside its room (the thrown light)
    float  pxM;       // metres per screen pixel here (the caustics' footprint)
};

FRSub FRSubFind(float3 P, float pxM)
{
    FRSub s;
    s.body = -1;
    s.waterY = -1000.0;
    s.depth = 0.0;
    s.a = 0.0;
    s.albedo = 0.8;
    s.under = false;
    s.room = false;
    s.pxM = pxM;
    int n = (int)_FR_SubCount;
    [loop] for (int i = 0; i < FR_SUB_MAX; i++)
    {
        if (i >= n) break;
        float4 r = _FR_SubRoom[i];
        float4 p = _FR_SubParam[i];
        if (P.x < r.x || P.z < r.y || P.x > r.z || P.z > r.w || P.y > p.w) continue;
        float4 w = _FR_SubRect[i];
        bool inWater = P.x >= w.x - 0.02 && P.z >= w.y - 0.02 && P.x <= w.z + 0.02 && P.z <= w.w + 0.02;
        s.body = i;
        s.waterY = p.x;
        s.albedo = p.z;
        s.a = _FR_SubAbsorb.rgb * p.y;
        s.under = inWater && P.y < p.x;
        s.depth = s.under ? p.x - P.y : 0.0;
        s.room = !s.under;
        break;
    }
    return s;
}

// Exact unpolarised Fresnel reflectance; ci the cosine in the medium the light comes from, nrel = n(to) / n(from)
float FRSubFresnel(float ci, float nrel)
{
    ci = saturate(ci);
    float g2 = nrel * nrel - 1.0 + ci * ci;
    if (g2 <= 0.0) return 1.0;
    float g = sqrt(g2);
    float a = (g - ci) / (g + ci);
    float b = (ci * (g + ci) - 1.0) / (ci * (g - ci) + 1.0);
    return saturate(0.5 * a * a * (1.0 + b * b));
}

float2 FRSubLut(float tau)
{
    float u = tau / (tau + 1.0);
    return SAMPLE_TEXTURE2D_LOD(_FR_SubLut, sampler_LinearClamp, float2((u * (FR_SUB_LUT_N - 1.0) + 0.5) / FR_SUB_LUT_N, 0.5), 0).rg;
}

// ------------------------------------------------------------------------------------------------ caustics
// weights of the four lens distances (piecewise linear in log D; past the last the last holds)
float4 FRCausticW(float D, float4 Dch)
{
    float l = log2(max(D, 1e-5));
    float4 lc = log2(max(Dch, 1e-5));
    float t0 = saturate((l - lc.x) / max(lc.y - lc.x, 1e-3));
    float t1 = saturate((l - lc.y) / max(lc.z - lc.y, 1e-3));
    float t2 = saturate((l - lc.z) / max(lc.w - lc.z, 1e-3));
    return float4(1.0 - t0, t0 - t1, t1 - t2, t2);
}

// one loop's intensity (mean 1) at uv (patch units): its two slices and their blend, the lens distance, the mip
float FRCausticLoop(TEXTURE2D_ARRAY_PARAM(tex, smp), float2 uv, float3 sl, float4 Dch, float D, float lod)
{
    float4 w = FRCausticW(D, Dch);
    float4 c0 = SAMPLE_TEXTURE2D_ARRAY_LOD(tex, smp, uv, sl.x, lod);
    float4 c1 = SAMPLE_TEXTURE2D_ARRAY_LOD(tex, smp, uv, sl.y, lod);
    float I = dot(lerp(c0, c1, sl.z), w) * FR_SUB_ENC;
    return lerp(1.0, I, saturate(D / max(Dch.x, 1e-5)));                 // below the first distance it fades to flat
}

// The rings' own caustic (17 4.4: "projected from the height field ... including footstep rings"): every ring slot's
// closed form (FrontRoomsWaterRipples.hlsl FRRingsAdd), here its curvature along the radius (h'') and across it (h'/r),
// filtered by the blur (the source's size and the pixel); the lens J = I + D grad(s) has those two eigenvalues, so the
// light a ring sends to the receiver is 1 / |(1 + D h'')(1 + D h'/r)|, clamped where a finite source cannot focus.
float FRRingLens(float2 xz, float D, float blurM)
{
    int n = (int)_FR_RingCount;
    if (n <= 0 || D <= 0.0) return 1.0;
    float cg = _FR_RingConst0.x, km = _FR_RingConst0.y, omm = _FR_RingConst0.z, om3 = _FR_RingConst0.w;
    float gr = _FR_RingConst1.x, sr = _FR_RingConst1.y, nu = _FR_RingConst1.z, ifade = _FR_RingConst1.w;
    float vmax = _FR_RingConst2.x, lutN = _FR_RingConst2.y;
    float lens = 1.0;
    [loop] for (int i = 0; i < 16; i++)
    {
        if (i >= n) break;
        float4 r0 = _FR_Ring0[i];
        float4 r1 = _FR_Ring1[i];
        float2 d = xz - r0.xy;
        float r = max(length(d), 1e-3);
        float t = max(r0.z, 1e-3);
        float kc = 2.5 / max(r1.y, 1e-3), kg = 0.5 / max(r1.x, 1e-3);
        float vf = max((gr + 3.0 * sr * kc * kc) / (2.0 * sqrt(gr * kc + sr * kc * kc * kc)),
                       (gr + 3.0 * sr * kg * kg) / (2.0 * sqrt(gr * kg + sr * kg * kg * kg)));
        if (r0.w <= 0.0 || r > min(vf, vmax) * t + 3.0 * r1.x + 0.05) continue;
        float sc = pow(2.0 / (t * om3), 1.0 / 3.0);
        float X = -(r - cg * t) * sc;
        if (X > 6.0) continue;
        float vol = r0.w / (2.0 * FR_RIPPLE_PI);
        float curv = 0.0, slope = 0.0;
        float bw = saturate((X + 2.5) / 0.7);
        float l2 = blurM * blurM;
        if (bw > 0.0)
        {
            float F = vol * ((1.0 - r1.z) * exp(-0.5 * km * km * r1.x * r1.x) + r1.z * exp(-0.5 * km * km * r1.y * r1.y));
            float A = F * sqrt(2.0 * FR_RIPPLE_PI * km / r) * sc * FRRingAiry(X) * exp(-(2.0 * nu * km * km + ifade) * t) * bw;
            float ph = km * r - omm * t - 0.25 * FR_RIPPLE_PI;
            float H = exp(-km * km * l2 / 24.0);
            float sn, cs;
            sincos(ph, sn, cs);
            curv += -A * km * km * cs * H;
            slope += -A * km * sn * H;
        }
        if (bw < 1.0 && r > cg * t)
        {
            float u = sqrt(saturate((r / t - cg) / (vmax - cg)));
            float2 kk = SAMPLE_TEXTURE2D_LOD(_FR_RingLut, sampler_LinearClamp, float2((u * (lutN - 1.0) + 0.5) / lutN, 0.5), 0).rg;
            [unroll] for (int b = 0; b < 2; b++)
            {
                float k = b == 0 ? kk.x : kk.y;
                float om = sqrt(gr * k + sr * k * k * k);
                float om1 = (gr + 3.0 * sr * k * k) / (2.0 * om);
                float om2 = abs(3.0 * sr * k / om - om1 * om1 / om);
                float F = vol * ((1.0 - r1.z) * exp(-0.5 * k * k * r1.x * r1.x) + r1.z * exp(-0.5 * k * k * r1.y * r1.y));
                float A = F * sqrt(k / (r * t * max(om2, 1e-7))) * exp(-(2.0 * nu * k * k + ifade) * t) * (1.0 - bw);
                float ph = k * r - om * t - (b == 0 ? 0.0 : 0.5 * FR_RIPPLE_PI);
                float H = exp(-k * k * l2 / 24.0);
                float sn, cs;
                sincos(ph, sn, cs);
                curv += -A * k * k * cs * H;
                slope += -A * k * sn * H;
            }
        }
        float det = (1.0 + D * curv) * (1.0 + D * slope / r);
        lens *= 1.0 / max(abs(det), 0.25);
    }
    return min(lens, 6.0);
}

// The ripple's caustic for light that crossed the surface at xz and went pathW metres down through the water, from a
// source of angular radius alphaW (in the water): lens D = pathW (1 - 1/n) times the calm's gain; the source's size
// and the pixel's footprint pick the mip (the loops are box-mipped: a mip is the mean, so a large source sees flat
// light)
float FRCausticFloor(float2 xz, float pathW, float alphaW, float pxM)
{
    float mode = _FR_SubMode.x;
    if (mode < 0.5) return 1.0;
    float m = max(_FR_RippleEnc.z, 0.0);
    float D = pathW * (1.0 - 1.0 / FR_SUB_N) * m;
    float blurM = max(pathW * alphaW, pxM);
    float tF = _FR_CausticTex.x * _FR_RippleGeo.z;                        // texels per metre, fine loop
    float tL = _FR_CausticTex.y * _FR_RippleGeo.w;
    float lodF = log2(max(blurM * tF, 1.0));
    float lodL = log2(max(blurM * tL, 1.0));
    float2 uvA = xz * _FR_RippleGeo.z;
    float I;
    if (mode < 2.5)
    {
        I = FRCausticLoop(TEXTURE2D_ARRAY_ARGS(_FR_CausticFine, sampler_TrilinearRepeat), uvA, _FR_RippleA.xyz, _FR_CausticFineD, D, lodF);
        if (_FR_RippleB.w > 0.5)
        {
            // layer B is the fine field 8/7 larger: its caustic at D is the baked one at D x 7/8
            float2 uvB = float2(0.6 * xz.x - 0.8 * xz.y, 0.8 * xz.x + 0.6 * xz.y) * (_FR_RippleGeo.z * FR_RIPPLE_B_SCALE) + float2(0.37, 0.61);
            float lodB = log2(max(blurM * tF * FR_RIPPLE_B_SCALE, 1.0));
            I *= FRCausticLoop(TEXTURE2D_ARRAY_ARGS(_FR_CausticFine, sampler_TrilinearRepeat), uvB, _FR_RippleB.xyz, _FR_CausticFineD, D * FR_RIPPLE_B_SCALE, lodB);
        }
        float2 uvL = float2(0.8 * xz.x - 0.6 * xz.y, 0.6 * xz.x + 0.8 * xz.y) * _FR_RippleGeo.w + float2(0.19, 0.83);
        I *= FRCausticLoop(TEXTURE2D_ARRAY_ARGS(_FR_CausticLong, sampler_TrilinearRepeat), uvL, _FR_RippleL.xyz, _FR_CausticLongD, D, lodL);
    }
    else
    {
        // WebGL: two scrolled layers of one tile (the phone loop's first slice), crossing slowly (< 4 cm/s)
        float t = _FR_SubMode.w;
        float2 u1 = uvA + float2(0.031, 0.017) * t;
        float2 u2 = float2(-uvA.y, uvA.x) * 1.37 + float2(-0.023, 0.029) * t + 0.5;
        I = FRCausticLoop(TEXTURE2D_ARRAY_ARGS(_FR_CausticFine, sampler_TrilinearRepeat), u1, float3(0.0, 0.0, 0.0), _FR_CausticFineD, D, lodF)
          * FRCausticLoop(TEXTURE2D_ARRAY_ARGS(_FR_CausticFine, sampler_TrilinearRepeat), u2, float3(0.0, 0.0, 0.0), _FR_CausticFineD, D / 1.37, lodF + 0.45);
    }
    I *= FRRingLens(xz, D, blurM);                                         // a footstep, a drip, the Relay wading
    return max(lerp(1.0, I, _FR_SubMode.z), 0.0);
}

// The long ripple's caustic on light leaving the water at xz and travelling pathA metres through the air (lens
// D = pathA (n - 1)), from a source of angular radius alphaA (in the air). Only waves of 7-30 cm: the large areas a
// ceiling shows never move faster than they do (17 4.4 safety: <= 3 Hz over large areas).
float FRCausticThrown(float2 xz, float pathA, float alphaA, float pxM)
{
    float mode = _FR_SubMode.x;
    if (mode < 0.5) return 1.0;
    float m = max(_FR_RippleEnc.z, 0.0);
    float D = pathA * (FR_SUB_N - 1.0) * m;
    float blurM = max(pathA * alphaA, pxM);
    float tL = _FR_CausticTex.y * _FR_RippleGeo.w;
    float lodL = log2(max(blurM * tL, 1.0));
    float2 uvL = float2(0.8 * xz.x - 0.6 * xz.y, 0.6 * xz.x + 0.8 * xz.y) * _FR_RippleGeo.w + float2(0.19, 0.83);
    float I;
    if (mode < 2.5)
        I = FRCausticLoop(TEXTURE2D_ARRAY_ARGS(_FR_CausticLong, sampler_TrilinearRepeat), uvL, _FR_RippleL.xyz, _FR_CausticLongD, D, lodL);
    else
    {
        float t = _FR_SubMode.w;
        I = FRCausticLoop(TEXTURE2D_ARRAY_ARGS(_FR_CausticLong, sampler_TrilinearRepeat), uvL + float2(0.011, 0.006) * t, float3(0.0, 0.0, 0.0), _FR_CausticLongD, D, lodL)
          * FRCausticLoop(TEXTURE2D_ARRAY_ARGS(_FR_CausticLong, sampler_TrilinearRepeat), float2(-uvL.y, uvL.x) * 1.29 + float2(-0.008, 0.010) * t, float3(0.0, 0.0, 0.0), _FR_CausticLongD, D / 1.29, lodL + 0.37);
    }
    return max(lerp(1.0, I, _FR_SubMode.z), 0.0);
}

// ------------------------------------------------------------------------------------------------ under the water
float4 FRSubLightPosition(uint lightIndex)
{
#if USE_CLUSTER_LIGHT_LOOP
    int idx = (int)lightIndex;
#else
    int idx = GetPerObjectLightIndex(lightIndex);
#endif
#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    return _AdditionalLightsBuffer[idx].position;
#else
    return _AdditionalLightsPosition[idx];
#endif
}

// What a light gives a point under the water: the share of its colour that crosses the surface and the water, the
// direction it arrives from in the water, its path in the water and where it entered (xz)
half3 FRSubLightTrans(float3 Lpos, bool underLight, half3 ldir, FRSub sub, float3 P, out half3 lw, out float pathW, out float2 entry)
{
    if (underLight)
    {
        // a wet niche: its whole path is under the water
        pathW = length(Lpos - P);
        lw = ldir;
        entry = P.xz;
        return (half3)exp(-sub.a * pathW);
    }
    float ca = saturate((float)ldir.y);
    if (ca <= 1e-3)
    {
        lw = ldir;
        pathW = 0.0;
        entry = P.xz;
        return half3(0.0, 0.0, 0.0);
    }
    float cw = sqrt(saturate(1.0 - (1.0 - ca * ca) / (FR_SUB_N * FR_SUB_N)));
    lw = half3(ldir.x / FR_SUB_N, cw, ldir.z / FR_SUB_N);
    pathW = sub.depth / max(cw, 0.05);
    entry = P.xz + (float2)lw.xz * pathW;
    float T = 1.0 - FRSubFresnel(ca, FR_SUB_N);
    return (half3)(T * (ca / cw) * exp(-sub.a * pathW));
}

half3 FRSubLight(BRDFData brdf, Light light, float4 lp, InputData inputData, half3 Vw, FRSub sub, bool caustic)
{
    bool underLight = lp.w > 0.5 && lp.y < sub.waterY;
    half3 lw;
    float pathW;
    float2 entry;
    half3 T = FRSubLightTrans(lp.xyz, underLight, light.direction, sub, inputData.positionWS, lw, pathW, entry);
    if (caustic && !underLight && lp.w > 0.5)
    {
        // a compact source of this room (<= 2): its light carries the ripple's caustic
        float4 c0 = _FR_SubSrc0[sub.body];
        float4 c1 = _FR_SubSrc1[sub.body];
        float3 d0 = c0.xyz - lp.xyz;
        float3 d1 = c1.xyz - lp.xyz;
        float srcR = (c0.w > 0.0 && dot(d0, d0) < 1e-4) ? c0.w : ((c1.w > 0.0 && dot(d1, d1) < 1e-4) ? c1.w : 0.0);
        if (srcR > 0.0)
        {
            float toSurface = length(lp.xyz - float3(entry.x, sub.waterY, entry.y));
            T *= (half)FRCausticFloor(entry, pathW, srcR / max(toSurface, 0.05) / FR_SUB_N, sub.pxM);
        }
    }
    const BRDFData noCoat = (BRDFData)0;
    return LightingPhysicallyBased(brdf, noCoat, light.color * T, lw, light.distanceAttenuation * light.shadowAttenuation,
                                   inputData.normalWS, Vw, 0.0h, false);
}

// UniversalFragmentPBR for a point under the water (URP 17 Lighting.hlsl, one light loop of its own); the result is the
// radiance that leaves the water toward the eye, before the surface's own 1 - F (the water shader's)
half3 FRSubmergedPBR(InputData inputData, SurfaceData s, FRSub sub)
{
    BRDFData brdf;
    InitializeBRDFData(s, brdf);
    // glaze against water: n 1.52 against 1.333 reflects about a tenth of what it does against air
    brdf.specular *= (half)_FR_SubMode.y;
    brdf.grazingTerm *= (half)_FR_SubMode.y;
    const BRDFData noCoat = (BRDFData)0;
    half4 shadowMask = CalculateShadowMask(inputData);
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, s);
    uint meshRenderingLayers = GetMeshRenderingLayer();
    // the view, refracted: the ray that leaves this point for the eye runs in the water at Snell's angle
    half3 V = inputData.viewDirectionWS;
    float cv = sqrt(saturate(1.0 - (1.0 - (float)V.y * (float)V.y) / (FR_SUB_N * FR_SUB_N)));
    half3 Vw = normalize(half3(V.x / FR_SUB_N, (half)cv, V.z / FR_SUB_N));
    // the LUT per channel: the diffuse light's entry, and how much of the upwelling light the surface sends back
    float3 tau = sub.a * sub.depth;
    float2 lr = FRSubLut(tau.r);
    float2 lg = FRSubLut(tau.g);
    float2 lb = FRSubLut(tau.b);
    half3 ambT = half3(lr.x, lg.x, lb.x);
    half3 trapP = half3(lr.y, lg.y, lb.y);

    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);
    half3 col = GlobalIllumination(brdf, noCoat, 0.0h, inputData.bakedGI, aoFactor.indirectAmbientOcclusion, inputData.positionWS,
                                   inputData.normalWS, Vw, inputData.normalizedScreenSpaceUV) * ambT;
#ifdef _LIGHT_LAYERS
    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
#endif
        col += FRSubLight(brdf, mainLight, float4(0.0, 0.0, 0.0, 0.0), inputData, Vw, sub, false);

#if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();
#if USE_CLUSTER_LIGHT_LOOP
    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
#ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
            col += FRSubLight(brdf, light, float4(0.0, 0.0, 0.0, 0.0), inputData, Vw, sub, false);
    }
#endif
    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
#ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
            col += FRSubLight(brdf, light, FRSubLightPosition(lightIndex), inputData, Vw, sub, true);
    LIGHT_LOOP_END
#endif
#if defined(_ADDITIONAL_LIGHTS_VERTEX)
    col += inputData.vertexLighting * brdf.diffuse * ambT;
#endif
    col += s.emission;
    // the way out: the view's path in the water, the trapping, and 1 / n^2
    float3 tOut = exp(-sub.a * (sub.depth / max(cv, 0.05)));
    half3 trap = 1.0h / max(1.0h - (half)sub.albedo * trapP, 0.05h);
    return min(col * (half3)tOut * trap * (half)_FR_SubAbsorb.w, HALF_MAX);
}

// ------------------------------------------------------------------------------------------------ above the water
// The light the wet niches of this room throw out of the water onto P (irradiance, URP light units). Each niche is a
// glow source under the water at its lamp (the light it puts into the basin comes back up there, Lambertian about the
// vertical); its rays leave the flat surface where Snell puts them, spread by the flat interface, carry 1 - F out and
// the long ripple's caustic of their path in the air.
half3 FRSubThrown(float3 P, half3 N, FRSub sub)
{
    half3 E = 0.0;
    float hQ = P.y - sub.waterY;
    if (hQ <= 0.005 || _FR_SubThrown.x <= 0.0) return E;
    // the rings' caustic is taken once, at the exit point of the lamp that gives P the most (the others share it)
    float bestE = 0.0;
    float2 bestS = P.xz;
    float bestPath = hQ, bestAlpha = 0.0;
    int n = (int)_FR_SubLampCount;
    [loop] for (int i = 0; i < FR_SUB_LAMPS; i++)
    {
        if (i >= n) break;
        float4 lp = _FR_SubLampPos[i];
        if ((int)(lp.w + 0.5) != sub.body) continue;
        float4 lc = _FR_SubLampCol[i];
        if (lc.w <= 0.001) continue;
        float hL = sub.waterY - lp.y;
        if (hL <= 0.01) continue;
        float2 d2 = P.xz - lp.xz;
        float r = length(d2);
        float2 dirH = r > 1e-4 ? d2 / r : float2(1.0, 0.0);
        // the exit point: hL tan(tw) + hQ tan(ta) = r with sin(ta) = n sin(tw). Newton on t = tan(ta), from the paraxial
        // guess: f(t) = hL tan(tw(t)) + hQ t - r is smooth and nearly linear in t (tan(tw) stays under tan(48.6 deg)), so
        // four steps land on the root everywhere (on sin(tw) the problem is stiff near the critical angle: sub_v1's hard
        // edges, 11-04)
        float t = r / (hQ + hL / FR_SUB_N);
        [unroll] for (int k = 0; k < 4; k++)
        {
            float qa = rsqrt(1.0 + t * t);
            float sw0 = t * qa / FR_SUB_N;
            float cw0 = sqrt(max(1.0 - sw0 * sw0, 1e-6));
            float f = hL * sw0 / cw0 + hQ * t - r;
            float df = hL * qa * qa * qa / (FR_SUB_N * cw0 * cw0 * cw0) + hQ;
            t = max(t - f / df, 0.0);
        }
        float ca = rsqrt(1.0 + t * t);
        float sa = t * ca;
        float u = sa / FR_SUB_N;
        float cw = sqrt(max(1.0 - u * u, 1e-6));
        float T = 1.0 - FRSubFresnel(cw, 1.0 / FR_SUB_N);
        if (T <= 0.0) continue;
        // spreading by the flat interface: E_h = I T sin(tw) / (r dr/dtw)
        float drdt = hL / (cw * cw) + hQ / (ca * ca) * (FR_SUB_N * cw / ca);
        float q = r > 1e-3 ? u / r : 1.0 / (hL + FR_SUB_N * hQ);
        float Eh = cw * T * q / max(drdt, 1e-4);                            // I(tw) = I0 cos(tw): a diffuse glow upward
        float3 w = float3(dirH.x * sa, ca, dirH.y * sa);                    // the ray's direction in the air (up)
        float cosIn = saturate(dot((float3)N, -w));
        if (cosIn <= 0.0) continue;
        float2 S = lp.xz + dirH * (hL * u / cw);
        float pathA = hQ / ca;
        float alphaA = FR_SUB_N * _FR_SubThrown.y * cw / hL;               // the glow's angular radius, out in the air
        float C = FRCausticThrown(S, pathA, alphaA, sub.pxM);
        float e = lc.w * Eh * cosIn / ca;
        E += (half3)(lc.rgb * (e * C));
        float lum = e * dot(lc.rgb, float3(0.2126, 0.7152, 0.0722));
        if (lum > bestE) { bestE = lum; bestS = S; bestPath = pathA; bestAlpha = alphaA; }
    }
    if (bestE > 0.0 && _FR_SubMode.x > 0.5)
        E *= (half)max(lerp(1.0, FRRingLens(bestS, bestPath * (FR_SUB_N - 1.0), max(bestPath * bestAlpha, sub.pxM)), _FR_SubMode.z), 0.0);
    return E * (half)_FR_SubThrown.x;
}

#endif
