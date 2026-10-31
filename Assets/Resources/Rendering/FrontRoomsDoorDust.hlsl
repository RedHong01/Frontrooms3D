// FrontRoomsDoorDust.hlsl -- shared by FrontRooms/DoorGrit and FrontRooms/DoorHaze (door break dust, production P1 2026-10-30;
// research/door_break/54_dust_grit.md 4.1, 4.6). Dust is seen by scattering, and it scatters FORWARD (Henyey-Greenstein,
// g 0.6, normalised so isotropic = 1: back-lit x10, side x0.4, front x0.16), so the haze glows only against light; the far
// room's lamp leaks through the split, slot or notch as an analytic SHAFT (no shadow map can resolve a 15 mm slot).
#ifndef FRONTROOMS_DOOR_DUST_INCLUDED
#define FRONTROOMS_DOOR_DUST_INCLUDED

// Per door (MaterialPropertyBlock of the procedural draw), world space:
float4 _FR_LeakC;        // xyz the opening's centre, w 1 = on
float4 _FR_LeakU;        // xyz half axis along the split (up the leaf), w the soft edge (m)
float4 _FR_LeakV;        // xyz half axis across it
float4 _FR_LeakN;        // xyz the leaf normal toward the burst side
float4 _FR_LeakLampPos;  // xyz the far lamp
float4 _FR_LeakLampCol;  // rgb lamp colour x the far lamp's level x max(stage curve, blow pulse)

#define FR_DUST_G 0.6
#define FR_DUST_BACK_CAP 6.0

float FRDustHG(float cosTheta)
{
    float g = FR_DUST_G;
    float g2 = g * g;
    return min((1.0 - g2) / pow(max(1e-4, 1.0 + g2 - 2.0 * g * cosTheta), 1.5), FR_DUST_BACK_CAP);
}

// Lambert with a 0.3 wrap (grains tumble; their edge catches light past the terminator)
float FRDustWrap(float ndl) { return saturate((ndl + 0.3) / 1.3); }

uint FRDustHash(uint x)
{
    x ^= x >> 16; x *= 0x7feb352du; x ^= x >> 15; x *= 0x846ca68bu; x ^= x >> 16;
    return x;
}

// In-scattered light of the leak at P (seen along V, P -> camera) for a medium of this albedo: the ray from the far lamp to
// P must cross the leaf's plane inside the opening's rectangle (soft 4 mm edge). About 20 ALU.
float3 FRLeakShaft(float3 P, float3 V, float3 albedo)
{
    if (_FR_LeakC.w < 0.5) return 0;
    float3 L = _FR_LeakLampPos.xyz;
    float3 d = P - L;
    float3 N = _FR_LeakN.xyz;
    float denom = dot(d, N);
    if (denom <= 1e-4) return 0;
    float t = dot(_FR_LeakC.xyz - L, N) / denom;
    if (t <= 0.0 || t >= 1.0) return 0;
    float3 q = (L + d * t) - _FR_LeakC.xyz;
    float su = max(length(_FR_LeakU.xyz), 1e-4);
    float sv = max(length(_FR_LeakV.xyz), 1e-4);
    float u = dot(q, _FR_LeakU.xyz) / (su * su);
    float v = dot(q, _FR_LeakV.xyz) / (sv * sv);
    float soft = max(_FR_LeakU.w, 1e-4);
    float mask = saturate((1.0 - abs(u)) * su / soft) * saturate((1.0 - abs(v)) * sv / soft);
    if (mask <= 0.0) return 0;
    float3 dir = d * rsqrt(max(dot(d, d), 1e-6));
    float r2 = max(dot(d, d), 0.04);
    return albedo * _FR_LeakLampCol.rgb * (mask * FRDustHG(dot(dir, V)) / r2);
}

#endif
