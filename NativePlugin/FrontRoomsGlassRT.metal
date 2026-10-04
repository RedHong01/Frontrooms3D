// FrontRoomsGlassRT.metal — kernels of the desktop Metal ray-traced glass reflections (G14 P0 + P1).
// The build script embeds FRGlassRTShared.h followed by this file as ONE string (no #include at
// runtime), validates it with the OS Metal compiler at build time (tools/frglassrt_validate.mm) and the
// plugin compiles it asynchronously at device init (newLibraryWithSource: completionHandler:).
//
// Kernels
//   fr_trace           one mirror ray per raster glass pixel (glass prepass depth + normal), hit shading
//                      (FrontRooms/Surface + URP Lit parity, lamps, shadows, SH ambient, zone cube, fog),
//                      thin-glass see-through of other panes; writes raw radiance + depth tag.
//   fr_trace_aa        Ultra: 3 extra rotated-grid rays on high-contrast reflection pixels (budgeted).
//   fr_resolve         1 px dilation, back-surface image, smudge blur, edge filter, temporal accumulation
//                      while the camera is still (neighbourhood-clamped), writes _FR_GlassRTReflection.
//   fr_parity          debug: primary rays through the same hit shader over the whole screen.
//   fr_gather_vertices / fr_gather_indices   copy a non-readable Unity mesh into the plugin's own pool.
//   fr_layout          one-time layout self-test of every shared struct.
#include <metal_stdlib>
#include <metal_raytracing>
using namespace metal;
using namespace metal::raytracing;

constexpr sampler kRepeat(address::repeat, filter::linear, mip_filter::linear);
constexpr sampler kClamp(address::clamp_to_edge, filter::linear, mip_filter::linear);

typedef intersector<triangle_data, instancing> FRIsect;
typedef FRIsect::result_type FRHit;

#define FR_HALF_MIN 6.103515625e-5f
#define FR_HALF_MIN_SQRT 0.0078125f
#define FR_TEX_WHITE 0u
#define FR_TEX_BLACK 1u
#define FR_TEX_BUMP 2u
#define FR_TEX_GREY 3u

struct FRScene
{
    instance_acceleration_structure tlas;
    device const FRInstance* insts;
    device const FRMeshGPU* meshes;
    device const FRMaterial* mats;
    device const FRTexSlot* texs;
    device const FRLamp* lamps;
    texturecube<float> env;
};

// ------------------------------------------------------------------ small helpers
static inline float3 FRRowMul(float4 r0, float4 r1, float4 r2, float3 p)
{
    return float3(dot(r0.xyz, p) + r0.w, dot(r1.xyz, p) + r1.w, dot(r2.xyz, p) + r2.w);
}

static inline float3 FRNormalToWorld(thread const FRInstance& I, float3 n)
{
    // transpose(world-to-object 3x3) * n: correct for non-uniform and negative scale
    return n.x * I.w2o0.xyz + n.y * I.w2o1.xyz + n.z * I.w2o2.xyz;
}

static inline float FRLuma(float3 c) { return dot(c, float3(0.2126f, 0.7152f, 0.0722f)); }

static inline float FRPow4(float x) { float x2 = x * x; return x2 * x2; }

static inline float3 FRSampleSH(constant FRFrame& F, float3 n)
{
    float3 r = F.sh[0].xyz + F.sh[1].xyz * n.x + F.sh[2].xyz * n.y + F.sh[3].xyz * n.z
             + F.sh[4].xyz * (n.x * n.y) + F.sh[5].xyz * (n.y * n.z) + F.sh[6].xyz * (n.x * n.z)
             + F.sh[7].xyz * (n.x * n.x) + F.sh[8].xyz * (n.y * n.y);
    return max(r, float3(0.0f));
}

// URP PerceptualRoughnessToMipmapLevel (UNITY_SPECCUBE_LOD_STEPS in envInfo.x)
static inline float FRRoughnessToMip(constant FRFrame& F, float perceptualRoughness)
{
    float pr = perceptualRoughness * (1.7f - 0.7f * perceptualRoughness);
    return min(pr * F.envInfo.x, F.envInfo.w);
}

// URP DecodeHDREnvironment
static inline float3 FRDecodeHDR(constant FRFrame& F, float4 e)
{
    float alpha = F.envDecode.w * (e.a - 1.0f) + 1.0f;
    return (F.envDecode.x * pow(max(alpha, 0.0f), F.envDecode.y)) * e.rgb;
}

static inline float3 FREnvironment(constant FRFrame& F, thread const FRScene& S, float3 dir, float perceptualRoughness)
{
    if (F.envInfo.y < 0.5f) return float3(0.0f);
    float4 e = S.env.sample(kClamp, dir, level(FRRoughnessToMip(F, perceptualRoughness)));
    return FRDecodeHDR(F, e);
}

// URP UnpackNormalmapRGorAG + UnpackNormalAG (desktop DXT5nm / BC5)
static inline float3 FRUnpackNormal(float4 packed, float scale)
{
    packed.a *= packed.r;
    float3 n;
    n.xy = packed.ag * 2.0f - 1.0f;
    n.z = max(1.0e-16f, sqrt(1.0f - saturate(dot(n.xy, n.xy))));
    n.xy *= scale;
    return n;
}

// FrontRoomsSurface.shader PlanarFrame
static inline void FRPlanarFrame(float3 positionWS, float3 n, thread float2& uvMetres, thread float3& t, thread float3& b)
{
    t = abs(n.y) > 0.5f ? float3(1, 0, 0) : normalize(cross(float3(0, 1, 0), n));
    b = cross(n, t);
    uvMetres = float2(dot(positionWS, t), dot(positionWS, b));
}

static inline float4 FRTex(thread const FRScene& S, uint slot, float2 uv, float lod)
{
    return S.texs[slot].tex.sample(kRepeat, uv, level(max(lod, 0.0f)));
}

static inline float FRTexWidth(thread const FRScene& S, uint slot)
{
    return float(max(S.texs[slot].tex.get_width(), 1u));
}

// ------------------------------------------------------------------ hit fetch
struct FRSurfaceHit
{
    float3 pos;
    float3 nGeo;      // geometric normal, facing the incoming ray
    float3 nVert;     // interpolated vertex normal (world), facing the incoming ray
    float3 tangent;   // from the triangle's UV derivatives (mesh-UV normal maps)
    float tangentSign;
    float2 uv;
    float colorR;
    float uvPerMetre; // sqrt(uv area / world area) of the triangle
    uint instanceId;
    uint material;
    uint flags;
    float4 emissionOverride;
};

static FRSurfaceHit FRFetchHit(thread const FRScene& S, uint instanceId, uint geometryId, uint primitiveId, float2 bary, float3 rayDir)
{
    FRSurfaceHit h;
    FRInstance I = S.insts[instanceId];
    FRMeshGPU M = S.meshes[I.meshSlot];
    uint base = M.submeshStart[geometryId] + primitiveId * 3u;
    uint i0 = M.indices[base], i1 = M.indices[base + 1u], i2 = M.indices[base + 2u];
    float3 p0 = float3(M.positions[i0 * 3u], M.positions[i0 * 3u + 1u], M.positions[i0 * 3u + 2u]);
    float3 p1 = float3(M.positions[i1 * 3u], M.positions[i1 * 3u + 1u], M.positions[i1 * 3u + 2u]);
    float3 p2 = float3(M.positions[i2 * 3u], M.positions[i2 * 3u + 1u], M.positions[i2 * 3u + 2u]);
    float3 w0 = FRRowMul(I.o2w0, I.o2w1, I.o2w2, p0);
    float3 w1 = FRRowMul(I.o2w0, I.o2w1, I.o2w2, p1);
    float3 w2 = FRRowMul(I.o2w0, I.o2w1, I.o2w2, p2);
    float b0 = 1.0f - bary.x - bary.y;
    h.pos = w0 * b0 + w1 * bary.x + w2 * bary.y;
    float3 e1 = w1 - w0, e2 = w2 - w0;
    float3 c = cross(e1, e2);
    float area2 = length(c);
    h.nGeo = area2 > 1e-12f ? c / area2 : float3(0, 1, 0);
    FRVertexAttr a0 = M.attrs[i0], a1 = M.attrs[i1], a2 = M.attrs[i2];
    float3 nObj = float3(a0.nx, a0.ny, a0.nz) * b0 + float3(a1.nx, a1.ny, a1.nz) * bary.x + float3(a2.nx, a2.ny, a2.nz) * bary.y;
    float3 nW = FRNormalToWorld(I, nObj);
    float nl = length(nW);
    h.nVert = nl > 1e-8f ? nW / nl : h.nGeo;
    if (dot(h.nGeo, rayDir) > 0.0f) h.nGeo = -h.nGeo;
    if (dot(h.nVert, rayDir) > 0.0f) h.nVert = -h.nVert;
    float2 uv0 = float2(a0.u, a0.v), uv1 = float2(a1.u, a1.v), uv2 = float2(a2.u, a2.v);
    h.uv = uv0 * b0 + uv1 * bary.x + uv2 * bary.y;
    h.colorR = a0.colorR * b0 + a1.colorR * bary.x + a2.colorR * bary.y;
    float2 d1 = uv1 - uv0, d2 = uv2 - uv0;
    float uvArea2 = abs(d1.x * d2.y - d1.y * d2.x);
    h.uvPerMetre = area2 > 1e-12f ? sqrt(uvArea2 / area2) : 1.0f;
    float det = d1.x * d2.y - d1.y * d2.x;
    float3 T = abs(det) > 1e-12f ? (e1 * d2.y - e2 * d1.y) / det : float3(1, 0, 0);
    float3 B = abs(det) > 1e-12f ? (e2 * d1.x - e1 * d2.x) / det : float3(0, 0, 1);
    T = T - h.nVert * dot(h.nVert, T);
    float tl = length(T);
    h.tangent = tl > 1e-8f ? T / tl : float3(1, 0, 0);
    h.tangentSign = dot(cross(h.nVert, h.tangent), B) < 0.0f ? -1.0f : 1.0f;
    h.instanceId = instanceId;
    h.material = I.materialBase + geometryId;
    h.flags = I.flags;
    h.emissionOverride = I.emission;
    return h;
}

// ------------------------------------------------------------------ material evaluation
struct FRPbrInput
{
    float3 albedo;
    float alpha;
    float metallic;
    float smoothness;
    float occlusion;
    float3 normal;
    float3 emission;
};

// Texture LOD from the ray cone: footprint (m) x texels per metre.
static inline float FRLod(float coneWidth, float cosHit, float texelsPerMetre)
{
    float footprint = coneWidth / max(cosHit, 0.08f);
    return log2(max(footprint * texelsPerMetre, 1e-6f));
}

static FRPbrInput FREvaluateMaterial(thread const FRScene& S, thread const FRSurfaceHit& h, float3 rayDir, float coneWidth)
{
    FRMaterial m = S.mats[h.material];
    FRPbrInput o;
    o.alpha = 1.0f;
    o.emission = float3(0.0f);
    o.occlusion = 1.0f;
    float cosHit = abs(dot(rayDir, h.nVert));
    float3 nGeo = h.nVert;
    float3 emissionColor = (h.flags & FR_FLAG_EMISSIVE_LENS) ? h.emissionOverride.xyz : m.emission.xyz;

    if (m.kind == FR_KIND_SURFACE)
    {
        // FrontRoomsSurface.shader Frag, line for line.
        float2 metres; float3 t, b;
        if (m.flags & FR_MAT_MESH_UV)
        {
            metres = h.uv;
            t = h.tangent;
            b = h.tangentSign * cross(nGeo, t);
        }
        else FRPlanarFrame(h.pos, nGeo, metres, t, b);
        float2 tile = max(m.tile.xy, float2(1e-3f));
        float2 uv = metres / tile * m.baseST.xy + m.baseST.zw;
        float uvPerMetre = (m.flags & FR_MAT_MESH_UV) ? h.uvPerMetre : 1.0f;
        float tpmBase = FRTexWidth(S, m.texBase) * uvPerMetre * abs(m.baseST.x) / tile.x;
        float lod = FRLod(coneWidth, cosHit, tpmBase);
        float lodMask = FRLod(coneWidth, cosHit, FRTexWidth(S, m.texMask) * uvPerMetre * abs(m.baseST.x) / tile.x);
        float lodBump = FRLod(coneWidth, cosHit, FRTexWidth(S, m.texBump) * uvPerMetre * abs(m.baseST.x) / tile.x);
        float4 albedo = FRTex(S, m.texBase, uv, lod) * m.baseColor;
        float4 mask = FRTex(S, m.texMask, uv, lodMask);
        float3 nTS = FRUnpackNormal(FRTex(S, m.texBump, uv, lodBump), m.p0.w);

        float2 wearUV; float3 wt, wb;
        FRPlanarFrame(h.pos, nGeo, wearUV, wt, wb);
        float macroW = FRTexWidth(S, m.texMacro);
        float4 m1 = FRTex(S, m.texMacro, wearUV / 8.0f, FRLod(coneWidth, cosHit, macroW / 8.0f));
        float4 m2 = FRTex(S, m.texMacro, float2(wearUV.y, -wearUV.x) / 12.8f, FRLod(coneWidth, cosHit, macroW / 12.8f));
        float tone = mix(1.0f, 0.84f + 0.30f * m1.r * m2.r, m.p1.x);
        float dirt = 1.0f - m.p1.y * smoothstep(0.55f, 0.95f, m1.g * 0.6f + m2.g * 0.4f);
        albedo.rgb *= tone * dirt;
        float ring = smoothstep(0.70f, 0.73f, m2.r) * (1.0f - smoothstep(0.73f, 0.80f, m2.r));
        float pool = smoothstep(0.74f, 0.88f, m2.r);
        albedo.rgb = mix(albedo.rgb, albedo.rgb * m.stain.rgb * 1.6f, saturate((ring * 0.9f + pool * 0.35f) * m.stain.w));
        float smoothness = mask.r * m.p0.x;
        float cavity = mix(1.0f, mask.g, m.p0.z);
        float wet = smoothstep(0.35f, 0.85f, m1.b * 0.7f + m2.b * 0.5f) * m.p1.z;
        albedo.rgb *= 1.0f - 0.32f * wet;
        smoothness = mix(smoothness, 0.62f, wet);
        nTS = normalize(mix(nTS, float3(0, 0, 1), wet * 0.7f));
        float wall = 1.0f - saturate(abs(nGeo.y) * 2.0f);
        float y = h.pos.y;
        float ceilingHeight = h.emissionOverride.w > 0.0f ? h.emissionOverride.w : m.p2.z;
        float floorBand = 1.0f - smoothstep(0.0f, m.p2.x, y);
        float ceilBand = smoothstep(ceilingHeight - 1.6f, ceilingHeight, y);
        float streak = m1.a * ceilBand;
        albedo.rgb *= 1.0f - wall * (m.p1.w * 0.35f * floorBand * (0.6f + 0.4f * m1.g));
        albedo.rgb = mix(albedo.rgb, albedo.rgb * m.stain.rgb * 1.5f, wall * saturate(m.p2.y * (streak * 0.8f + ceilBand * 0.15f)));
        o.albedo = albedo.rgb;
        o.metallic = m.p0.y;
        o.smoothness = saturate(smoothness);
        o.occlusion = cavity;
        o.normal = normalize(t * nTS.x + b * nTS.y + nGeo * nTS.z);
        if (m.flags & FR_MAT_EMISSION)
            o.emission = FRTex(S, m.texEmission, uv, lod).rgb * emissionColor;
    }
    else if (m.kind == FR_KIND_LIT)
    {
        // URP Lit, metallic workflow (LitInput.hlsl InitializeStandardLitSurfaceData).
        float2 uv = h.uv * m.baseST.xy + m.baseST.zw;
        float tpm = FRTexWidth(S, m.texBase) * h.uvPerMetre * abs(m.baseST.x);
        float lod = FRLod(coneWidth, cosHit, tpm);
        float4 albedo = FRTex(S, m.texBase, uv, lod) * m.baseColor;
        float metallic = m.p0.y, smoothness = m.p0.x;
        if (m.flags & FR_MAT_METALLIC_MAP)
        {
            float4 mg = FRTex(S, m.texMask, uv, lod);
            metallic = mg.r;
            smoothness = ((m.flags & FR_MAT_SMOOTH_ALBEDO) ? albedo.a : mg.a) * m.p0.x;
        }
        else if (m.flags & FR_MAT_SMOOTH_ALBEDO) smoothness = albedo.a * m.p0.x;
        float3 n = nGeo;
        if (m.flags & FR_MAT_NORMAL_MAP)
        {
            float3 nTS = FRUnpackNormal(FRTex(S, m.texBump, uv, lod), m.p0.w);
            float3 b = h.tangentSign * cross(nGeo, h.tangent);
            n = normalize(h.tangent * nTS.x + b * nTS.y + nGeo * nTS.z);
        }
        if (m.flags & FR_MAT_OCCLUSION_MAP)
            o.occlusion = mix(1.0f, FRTex(S, m.texOcclusion, uv, lod).g, m.p0.z);
        o.albedo = albedo.rgb;
        o.alpha = albedo.a;
        o.metallic = metallic;
        o.smoothness = saturate(smoothness);
        o.normal = n;
        if (m.flags & FR_MAT_EMISSION)
            o.emission = FRTex(S, m.texEmission, uv, lod).rgb * emissionColor;
    }
    else
    {
        o.albedo = m.baseColor.rgb;
        o.metallic = m.p0.y;
        o.smoothness = saturate(m.p0.x);
        o.normal = nGeo;
        if (m.flags & FR_MAT_EMISSION) o.emission = emissionColor;
    }
    return o;
}

// ------------------------------------------------------------------ lighting (URP UniversalFragmentPBR, Forward+)
static inline float FRDistanceAttenuation(float distanceSqr, float invRange2)
{
    float lightAtten = 1.0f / distanceSqr;
    float factor = distanceSqr * invRange2;
    float smoothFactor = saturate(1.0f - factor * factor);
    return lightAtten * smoothFactor * smoothFactor;
}

static inline float FRAngleAttenuation(float3 spotDirection, float3 lightDirection, float2 spotAttenuation)
{
    float atten = saturate(dot(spotDirection, lightDirection) * spotAttenuation.x + spotAttenuation.y);
    return atten * atten;
}

static float FRShadowRay(thread const FRScene& S, float3 origin, float3 dir, float maxDist)
{
    FRIsect isect;
    isect.assume_geometry_type(geometry_type::triangle);
    isect.force_opacity(forced_opacity::opaque);
    isect.accept_any_intersection(true);
    ray r(origin, dir, 0.0f, maxDist);
    FRHit hit = isect.intersect(r, S.tlas, FR_RAYMASK_SHADOW);
    return hit.type == intersection_type::none ? 1.0f : 0.0f;
}

static float3 FRShade(constant FRFrame& F, thread const FRScene& S, thread const FRSurfaceHit& h, thread const FRPbrInput& p,
                      float3 rayDir, thread uint& shadowRays)
{
    float3 n = p.normal;
    float3 v = -rayDir;
    float oneMinusReflectivity = 0.96f - p.metallic * 0.96f;   // OneMinusReflectivityMetallic, kDielectricSpec (0.04)
    float reflectivity = 1.0f - oneMinusReflectivity;
    float3 diffuse = p.albedo * oneMinusReflectivity;
    float3 specular = mix(float3(0.04f), p.albedo, p.metallic);
    float perceptualRoughness = 1.0f - p.smoothness;
    float roughness = max(perceptualRoughness * perceptualRoughness, FR_HALF_MIN_SQRT);
    float roughness2 = max(roughness * roughness, FR_HALF_MIN);
    float grazing = saturate(p.smoothness + reflectivity);
    float normalizationTerm = roughness * 4.0f + 2.0f;
    float roughness2MinusOne = roughness2 - 1.0f;

    // Global illumination: ambient SH + the zone cube, x material occlusion (no SSAO at hits).
    float NoV = saturate(dot(n, v));
    float fresnelTerm = FRPow4(1.0f - NoV);
    float3 indirectDiffuse = FRSampleSH(F, n);
    float3 indirectSpecular = FREnvironment(F, S, reflect(-v, n), perceptualRoughness);
    float surfaceReduction = 1.0f / (roughness2 + 1.0f);
    float3 envSpec = surfaceReduction * mix(specular, float3(grazing), fresnelTerm);
    float3 color = (indirectDiffuse * diffuse + indirectSpecular * envSpec) * p.occlusion;

    // Direct lights: every lamp the raster has on this frame.
    uint lampCount = F.counts.x;
    uint shadowBudget = F.shadow.x;
    float3 shadowOrigin = h.pos + h.nGeo * 0.002f;
    for (uint i = 0; i < lampCount; i++)
    {
        FRLamp L = S.lamps[i];
        uint type = uint(L.spotDir.w + 0.5f);
        float3 lightDir;
        float atten;
        float dist;
        if (type == FR_LAMP_DIRECTIONAL)
        {
            lightDir = L.posInvRange2.xyz;
            atten = 1.0f;
            dist = F.camFwd.w;
        }
        else
        {
            float3 lv = L.posInvRange2.xyz - h.pos;
            float distSqr = max(dot(lv, lv), FR_HALF_MIN);
            if (distSqr * L.posInvRange2.w >= 1.0f) continue;
            dist = sqrt(distSqr);
            lightDir = lv / dist;
            atten = FRDistanceAttenuation(distSqr, L.posInvRange2.w);
            if (type == FR_LAMP_SPOT) atten *= FRAngleAttenuation(L.spotDir.xyz, lightDir, L.atten.zw);
        }
        float NdotL = saturate(dot(n, lightDir));
        if (atten <= 0.0f || NdotL <= 0.0f) continue;
        float3 halfDir = normalize(lightDir + v);
        float NoH = saturate(dot(n, halfDir));
        float LoH = saturate(dot(lightDir, halfDir));
        float d = NoH * NoH * roughness2MinusOne + 1.00001f;
        float specularTerm = roughness2 / ((d * d) * max(0.1f, LoH * LoH) * normalizationTerm);
        float3 contribution = (diffuse + specular * specularTerm) * (L.color.xyz * (atten * NdotL));
        // Shadow rays only where the raster has this lamp's shadow on this frame (w = strength), within the
        // per-hit budget, and only for a contribution that can show (> 0.01 linear before the glass's Fresnel:
        // under 1/255 of the final pixel even at grazing): weak lamps keep their unshadowed light.
        if (L.color.w > 0.0f && shadowBudget > 0u && dot(h.nGeo, lightDir) > 0.0f && FRLuma(contribution) > 0.01f)
        {
            shadowBudget--;
            float vis = FRShadowRay(S, shadowOrigin, lightDir, max(dist - 0.02f, 0.0f));
            contribution *= 1.0f - L.color.w * (1.0f - vis);
            shadowRays++;
        }
        color += contribution;
    }
    return color + p.emission;
}

// ------------------------------------------------------------------ one reflection ray
struct FRTraceResult
{
    float3 radiance;
    float hitDistance;   // 0 = miss
    uint instance;       // hit instance + 1, 0 = miss
    bool miss;           // nothing hit (pure miss, no glass layer in front)
    bool layered;        // passed through another pane
};

static FRTraceResult FRTraceReflection(constant FRFrame& F, thread const FRScene& S, float3 origin, float3 dir,
                                       float coneAtOrigin, float spread, float glassSmooth,
                                       thread uint& shadowRays)
{
    FRTraceResult res;
    res.radiance = float3(0.0f);
    res.hitDistance = 0.0f;
    res.instance = 0u;
    res.miss = false;
    res.layered = false;
    FRIsect isect;
    isect.assume_geometry_type(geometry_type::triangle);
    isect.force_opacity(forced_opacity::opaque);
    isect.accept_any_intersection(false);
    float3 T = float3(1.0f);
    float tMin = 0.0f;
    float tMax = F.camFwd.w;
    uint layers = 0u;
    uint lastGlass = 0xffffffffu;
    for (uint guard = 0u; guard < 8u; guard++)
    {
        ray r(origin, dir, tMin, tMax);
        FRHit hit = isect.intersect(r, S.tlas, FR_RAYMASK_REFLECTION);
        if (hit.type == intersection_type::none)
        {
            if (layers == 0u) { res.miss = true; return res; }
            // behind a pane seen through: the zone cube as the glass would show it
            res.radiance += T * FREnvironment(F, S, dir, 1.0f - glassSmooth) * F.envInfo.z;
            return res;
        }
        FRInstance I = S.insts[hit.instance_id];
        if (I.flags & FR_FLAG_GLASS)
        {
            if (hit.instance_id != lastGlass)
            {
                if (layers >= F.shadow.y)
                {
                    // layer budget spent: this pane shows the zone cube, as the raster glass would
                    res.radiance += T * FREnvironment(F, S, dir, 1.0f - glassSmooth) * F.envInfo.z;
                    res.layered = true;
                    if (res.hitDistance == 0.0f) res.hitDistance = hit.distance;
                    return res;
                }
                FRSurfaceHit gh = FRFetchHit(S, hit.instance_id, hit.geometry_id, hit.primitive_id, hit.triangle_barycentric_coord, dir);
                float c = abs(dot(dir, gh.nGeo));
                float f5 = FRPow4(1.0f - c) * (1.0f - c);
                float Fr = 0.08f + 0.92f * f5;                       // pane reflectance (_PaneF0, two surfaces)
                float transmit = 1.0f - (0.11f + 0.89f * f5);         // Glass_Window alpha: _AlphaFace + _AlphaFresnel F^5
                res.radiance += T * Fr * FREnvironment(F, S, reflect(dir, gh.nGeo), 1.0f - glassSmooth) * F.envInfo.z;
                T *= transmit;
                layers++;
                res.layered = true;
                lastGlass = hit.instance_id;
                if (res.hitDistance == 0.0f) res.hitDistance = hit.distance;
            }
            tMin = hit.distance + 1e-4f;
            continue;
        }
        FRSurfaceHit h = FRFetchHit(S, hit.instance_id, hit.geometry_id, hit.primitive_id, hit.triangle_barycentric_coord, dir);
        float cone = coneAtOrigin + spread * hit.distance;
        FRPbrInput p = FREvaluateMaterial(S, h, dir, cone);
        res.radiance += T * FRShade(F, S, h, p, dir, shadowRays);
        res.hitDistance = hit.distance;
        res.instance = hit.instance_id + 1u;
        return res;
    }
    res.miss = layers == 0u;
    return res;
}

// Fog the reflected path the glass shader does not fog itself: the glass fogs eye depth d1; the
// reflection carries the rest of the path (d1 + d2), as one exp2 fog over the full length.
static inline float3 FRFogRemainder(constant FRFrame& F, float3 L, float d1, float d2)
{
    if (F.fog.y < 0.5f) return L;
    float k = F.fog.x;
    float full = (d1 + d2) * (d1 + d2) - d1 * d1;
    float f2 = exp2(-k * k * max(full, 0.0f));
    return mix(F.fogColor.xyz, L, f2);
}

static inline float3 FRCameraRay(constant FRFrame& F, float2 pixel)
{
    float2 ndc = float2(pixel.x * F.screen.z * 2.0f - 1.0f, 1.0f - pixel.y * F.screen.w * 2.0f);
    float4 h = F.invViewProj * float4(ndc, 0.5f, 1.0f);
    return normalize(h.xyz / h.w - F.camPos.xyz);
}

// ------------------------------------------------------------------ fr_trace
struct FRTraceCounts { uint glass, miss, hit, layer, shadow; };

static void FRTracePixel(uint2 p, constant FRFrame& F, thread const FRScene& S,
                         texture2d<float, access::read> gDepth, texture2d<float, access::read> gNormal,
                         texture2d<float, access::write> rawOut, texture2d<float, access::write> auxOut,
                         texture2d<float, access::write> idOut, thread FRTraceCounts& c)
{
    float d = gDepth.read(p).x;
    if (!(d > 0.0f))
    {
        rawOut.write(float4(0.0f), p);
        auxOut.write(float4(0.0f), p);
        if (F.counts.z == FR_DEBUG_HITID) idOut.write(float4(0.0f), p);
        return;
    }
    c.glass++;
    float4 nrm = gNormal.read(p);
    float3 n = normalize(nrm.xyz);
    float glassSmooth = nrm.w;
    float3 dirC = FRCameraRay(F, float2(p) + 0.5f);
    float3 posC = F.camPos.xyz + dirC * (d / max(dot(dirC, F.camFwd.xyz), 1e-4f));
    float3 dir = dirC, pos = posC;
    if (F.jitter.w > 0.5f)
    {
        dir = FRCameraRay(F, float2(p) + 0.5f + F.jitter.xy);
        float denom = dot(dir, n);
        if (abs(denom) > 1e-4f) pos = F.camPos.xyz + dir * (dot(posC - F.camPos.xyz, n) / denom);
    }
    if (dot(n, dir) > 0.0f) n = -n;
    float3 r = reflect(dir, n);
    float3 origin = pos + n * F.thin.z;
    float t1 = distance(F.camPos.xyz, pos);
    float spread = F.fog.z;
    FRTraceResult tr = FRTraceReflection(F, S, origin, r, spread * t1, spread, glassSmooth, c.shadow);
    if (tr.miss)
    {
        c.miss++;
        rawOut.write(float4(0.0f), p);
        auxOut.write(float4(0.0f, glassSmooth, 1.0f, 0.0f), p);
        if (F.counts.z == FR_DEBUG_HITID) idOut.write(float4(0.0f), p);
        return;
    }
    c.hit++;
    if (tr.layered) c.layer++;
    float3 L = FRFogRemainder(F, tr.radiance, d, tr.hitDistance);
    rawOut.write(float4(min(L, float3(60000.0f)), 1.0f + d), p);
    auxOut.write(float4(tr.hitDistance, glassSmooth, tr.layered ? 2.0f : 0.0f, 0.0f), p);
    if (F.counts.z == FR_DEBUG_HITID) idOut.write(float4(float(tr.instance), 0.0f, 0.0f, 0.0f), p);
}

// One mirror ray per raster glass pixel. Statistics: one atomic per SIMD-group and counter (not per pixel / ray).
kernel void fr_trace(uint2 gid [[thread_position_in_grid]],
                     uint lane [[thread_index_in_simdgroup]],
                     constant FRFrame& F [[buffer(0)]],
                     instance_acceleration_structure tlas [[buffer(1)]],
                     device const FRInstance* insts [[buffer(2)]],
                     device const FRMeshGPU* meshes [[buffer(3)]],
                     device const FRMaterial* mats [[buffer(4)]],
                     device const FRTexSlot* texs [[buffer(5)]],
                     device const FRLamp* lamps [[buffer(6)]],
                     device atomic_uint* stats [[buffer(7)]],
                     texture2d<float, access::read> gDepth [[texture(0)]],
                     texture2d<float, access::read> gNormal [[texture(1)]],
                     texture2d<float, access::write> rawOut [[texture(2)]],
                     texture2d<float, access::write> auxOut [[texture(3)]],
                     texturecube<float> env [[texture(4)]],
                     texture2d<float, access::write> idOut [[texture(5)]])
{
    FRTraceCounts c = { 0u, 0u, 0u, 0u, 0u };
    uint2 p = gid + F.rect.xy;
    if (p.x < F.rect.z && p.y < F.rect.w)
    {
        FRScene S = { tlas, insts, meshes, mats, texs, lamps, env };
        FRTracePixel(p, F, S, gDepth, gNormal, rawOut, auxOut, idOut, c);
    }
    uint g = simd_sum(c.glass), m = simd_sum(c.miss), h = simd_sum(c.hit), l = simd_sum(c.layer), sh = simd_sum(c.shadow);
    if (lane == 0u)
    {
        if (g) atomic_fetch_add_explicit(&stats[FR_STAT_GLASS_PX], g, memory_order_relaxed);
        if (m) atomic_fetch_add_explicit(&stats[FR_STAT_MISS_PX], m, memory_order_relaxed);
        if (h) atomic_fetch_add_explicit(&stats[FR_STAT_HIT_PX], h, memory_order_relaxed);
        if (l) atomic_fetch_add_explicit(&stats[FR_STAT_LAYER_PX], l, memory_order_relaxed);
        if (sh) atomic_fetch_add_explicit(&stats[FR_STAT_SHADOW_RAYS], sh, memory_order_relaxed);
    }
}

// ------------------------------------------------------------------ fr_trace_aa (Ultra)
kernel void fr_trace_aa(uint2 gid [[thread_position_in_grid]],
                        constant FRFrame& F [[buffer(0)]],
                        instance_acceleration_structure tlas [[buffer(1)]],
                        device const FRInstance* insts [[buffer(2)]],
                        device const FRMeshGPU* meshes [[buffer(3)]],
                        device const FRMaterial* mats [[buffer(4)]],
                        device const FRTexSlot* texs [[buffer(5)]],
                        device const FRLamp* lamps [[buffer(6)]],
                        device atomic_uint* stats [[buffer(7)]],
                        texture2d<float, access::read> gDepth [[texture(0)]],
                        texture2d<float, access::read> gNormal [[texture(1)]],
                        texture2d<float, access::read> rawIn [[texture(2)]],
                        texture2d<float, access::write> rawAA [[texture(3)]],
                        texturecube<float> env [[texture(4)]])
{
    uint2 p = gid + F.rect.xy;
    if (p.x >= F.rect.z || p.y >= F.rect.w) return;
    float4 c = rawIn.read(p);
    if (c.a <= 1.0f) { rawAA.write(c, p); return; }
    float tol = 0.02f + 0.01f * (c.a - 1.0f);
    float lmin = FRLuma(c.rgb), lmax = lmin;
    uint2 size = uint2(rawIn.get_width(), rawIn.get_height());
    for (int dy = -1; dy <= 1; dy++)
    for (int dx = -1; dx <= 1; dx++)
    {
        int2 q = int2(p) + int2(dx, dy);
        if (q.x < 0 || q.y < 0 || q.x >= int(size.x) || q.y >= int(size.y)) continue;
        float4 s = rawIn.read(uint2(q));
        if (s.a <= 1.0f || abs(s.a - c.a) > tol) continue;
        float l = FRLuma(s.rgb);
        lmin = min(lmin, l); lmax = max(lmax, l);
    }
    if ((lmax + 1e-3f) / (lmin + 1e-3f) < F.blur.z) { rawAA.write(c, p); return; }
    uint budget = uint(F.blur.w * float((F.rect.z - F.rect.x) * (F.rect.w - F.rect.y)));
    uint used = atomic_fetch_add_explicit(&stats[FR_STAT_AA_PX], 1u, memory_order_relaxed);
    if (used >= budget) { rawAA.write(c, p); return; }

    FRScene S = { tlas, insts, meshes, mats, texs, lamps, env };
    float d = gDepth.read(p).x;
    float4 nrm = gNormal.read(p);
    float3 n = normalize(nrm.xyz);
    float3 dirC = FRCameraRay(F, float2(p) + 0.5f);
    float3 posC = F.camPos.xyz + dirC * (d / max(dot(dirC, F.camFwd.xyz), 1e-4f));
    // rotated-grid 4x pattern; the centre sample stands in for the fourth
    const float2 offs[3] = { float2(0.125f, -0.375f), float2(-0.375f, -0.125f), float2(0.375f, 0.125f) };
    float3 sum = c.rgb;
    float spread = F.fog.z;
    for (uint k = 0; k < 3u; k++)
    {
        float3 dir = FRCameraRay(F, float2(p) + 0.5f + offs[k]);
        float denom = dot(dir, n);
        float3 pos = abs(denom) > 1e-4f ? F.camPos.xyz + dir * (dot(posC - F.camPos.xyz, n) / denom) : posC;
        float3 nn = dot(n, dir) > 0.0f ? -n : n;
        float3 r = reflect(dir, nn);
        float t1 = distance(F.camPos.xyz, pos);
        uint unusedShadow = 0u;
        FRTraceResult tr = FRTraceReflection(F, S, pos + nn * F.thin.z, r, spread * t1, spread, nrm.w, unusedShadow);
        float3 L = tr.miss ? c.rgb : FRFogRemainder(F, tr.radiance, d, tr.hitDistance);
        sum += L;
    }
    rawAA.write(float4(sum * 0.25f, c.a), p);
}

// ------------------------------------------------------------------ fr_resolve
static inline float FRTagTol(float tag) { return 0.02f + 0.01f * max(tag - 1.0f, 0.0f); }

kernel void fr_resolve(uint2 gid [[thread_position_in_grid]],
                       constant FRFrame& F [[buffer(0)]],
                       texture2d<float, access::read> raw [[texture(0)]],
                       texture2d<float, access::read> aux [[texture(1)]],
                       texture2d<float, access::read> gDepth [[texture(2)]],
                       texture2d<float, access::read> gNormal [[texture(3)]],
                       texture2d<float, access::read_write> outTex [[texture(4)]])
{
    uint2 p = gid + F.rect.xy;
    if (p.x >= F.rect.z || p.y >= F.rect.w) return;
    int2 size = int2(raw.get_width(), raw.get_height());
    float4 c = raw.read(p);
    float gd = gDepth.read(p).x;
    if (c.a <= 1.0f)
    {
        if (gd > 0.0f) { outTex.write(float4(0.0f), p); return; }   // a glass pixel whose ray missed: the zone cube
        // 1 px dilation: an MSAA edge sample of the pane whose pixel centre is off the pane
        const int2 nb[4] = { int2(1, 0), int2(-1, 0), int2(0, 1), int2(0, -1) };
        for (uint k = 0; k < 4u; k++)
        {
            int2 q = int2(p) + nb[k];
            if (q.x < 0 || q.y < 0 || q.x >= size.x || q.y >= size.y) continue;
            float4 s = raw.read(uint2(q));
            if (s.a > 1.0f) { outTex.write(s, p); return; }
        }
        outTex.write(float4(0.0f), p);
        return;
    }
    float tag = c.a;
    float tol = FRTagTol(tag);
    float4 ax = aux.read(p);
    float hitDist = ax.x;
    float smooth = ax.y;
    float3 L = c.rgb;

    // --- back-surface image of 6 mm glass (design 10 §1.6)
    if (F.thin.w > 0.5f && hitDist > 0.0f)
    {
        float3 n = normalize(gNormal.read(p).xyz);
        float3 v = FRCameraRay(F, float2(p) + 0.5f);
        float cosI = abs(dot(v, n));
        float sinI = sqrt(saturate(1.0f - cosI * cosI));
        float sinT = sinI / F.thin.y;
        float cosT = sqrt(saturate(1.0f - sinT * sinT));
        float shift = 2.0f * F.thin.x * sinT / max(cosT, 1e-3f);        // along the pane, metres
        float3 u = v - n * dot(v, n);
        float ul = length(u);
        float r0 = (F.thin.y - 1.0f) / (F.thin.y + 1.0f); r0 *= r0;
        float R1 = r0 + (1.0f - r0) * pow(1.0f - cosI, 5.0f);
        float Rb = (1.0f - R1) * (1.0f - R1) * R1;
        float wb = Rb / (R1 + Rb);
        if (ul > 1e-4f && shift > 1e-6f)
        {
            float d = gd;
            float3 pos = F.camPos.xyz + v * (d / max(dot(v, F.camFwd.xyz), 1e-4f));
            float3 virt = pos + v * hitDist - (u / ul) * shift;
            float4 clip = F.viewProj * float4(virt, 1.0f);
            if (clip.w > 1e-4f)
            {
                float2 ndc = clip.xy / clip.w;
                float2 px = float2((ndc.x * 0.5f + 0.5f) * F.screen.x, (0.5f - ndc.y * 0.5f) * F.screen.y);
                float2 base = px - 0.5f;
                int2 q0 = int2(floor(base));
                float2 fr = base - float2(q0);
                float3 acc = float3(0.0f); float wsum = 0.0f;
                for (int j = 0; j < 2; j++)
                for (int i = 0; i < 2; i++)
                {
                    int2 q = q0 + int2(i, j);
                    if (q.x < 0 || q.y < 0 || q.x >= size.x || q.y >= size.y) continue;
                    float4 s = raw.read(uint2(q));
                    if (s.a <= 1.0f || abs(s.a - tag) > tol) continue;
                    float w = (i == 0 ? 1.0f - fr.x : fr.x) * (j == 0 ? 1.0f - fr.y : fr.y);
                    acc += s.rgb * w; wsum += w;
                }
                float3 back = wsum > 1e-4f ? acc / wsum : L;
                L = mix(L, back, wb);
            }
        }
    }

    // --- smudge blur: radius from the pane's roughness, hit distance and view distance (bilateral on the tag)
    float pr = 1.0f - smooth;
    float alpha = pr * pr;
    float d1 = tag - 1.0f;
    float radiusPx = hitDist > 0.0f ? (hitDist * alpha) / max((d1 + hitDist) * F.fog.z, 1e-6f) : 0.0f;
    radiusPx = min(radiusPx, F.blur.x);
    if (radiusPx > 0.75f)
    {
        float3 acc = L; float wsum = 1.0f;
        float rot = fract(52.9829189f * fract(0.06711056f * float(p.x) + 0.00583715f * float(p.y))) * 6.2831853f;
        for (uint k = 1; k <= 12u; k++)
        {
            float rr = radiusPx * sqrt(float(k) / 12.0f);
            float a = float(k) * 2.39996323f + rot;
            int2 q = int2(p) + int2(round(float2(cos(a), sin(a)) * rr));
            if (q.x < 0 || q.y < 0 || q.x >= size.x || q.y >= size.y) continue;
            float4 s = raw.read(uint2(q));
            if (s.a <= 1.0f || abs(s.a - tag) > tol) continue;
            acc += s.rgb; wsum += 1.0f;
        }
        L = acc / wsum;
    }

    // --- edge filter (FXAA-style luminance contrast inside one tag), High and Ultra
    float lc = FRLuma(c.rgb);
    float lmin = lc, lmax = lc;
    float3 nbSum = float3(0.0f); float nbW = 0.0f;
    float3 cmin = c.rgb, cmax = c.rgb;
    for (int dy = -1; dy <= 1; dy++)
    for (int dx = -1; dx <= 1; dx++)
    {
        if (dx == 0 && dy == 0) continue;
        int2 q = int2(p) + int2(dx, dy);
        if (q.x < 0 || q.y < 0 || q.x >= size.x || q.y >= size.y) continue;
        float4 s = raw.read(uint2(q));
        if (s.a <= 1.0f || abs(s.a - tag) > tol) continue;
        float l = FRLuma(s.rgb);
        lmin = min(lmin, l); lmax = max(lmax, l);
        cmin = min(cmin, s.rgb); cmax = max(cmax, s.rgb);
        float w = (dx == 0 || dy == 0) ? 1.0f : 0.5f;
        nbSum += s.rgb * w; nbW += w;
    }
    if (F.blur.y > 0.0f && nbW > 0.0f)
    {
        float contrast = (lmax - lmin) / max(lmax, 1e-3f);
        float edge = smoothstep(0.25f, 0.75f, contrast) * F.blur.y;
        L = mix(L, (L + nbSum) / (1.0f + nbW), edge * 0.5f);
    }

    // --- temporal accumulation while the camera and the scene are still (no reprojection, clamped)
    if (F.jitter.w > 0.5f)
    {
        float4 prev = outTex.read(p);
        if (prev.a > 1.0f && abs(prev.a - tag) <= tol)
        {
            float3 h = clamp(prev.rgb, cmin, cmax);
            L = mix(h, L, F.jitter.z);
        }
    }
    outTex.write(float4(L, tag), p);
}

// ------------------------------------------------------------------ fr_parity (debug)
kernel void fr_parity(uint2 gid [[thread_position_in_grid]],
                      constant FRFrame& F [[buffer(0)]],
                      instance_acceleration_structure tlas [[buffer(1)]],
                      device const FRInstance* insts [[buffer(2)]],
                      device const FRMeshGPU* meshes [[buffer(3)]],
                      device const FRMaterial* mats [[buffer(4)]],
                      device const FRTexSlot* texs [[buffer(5)]],
                      device const FRLamp* lamps [[buffer(6)]],
                      device atomic_uint* stats [[buffer(7)]],
                      texture2d<float, access::write> rawOut [[texture(2)]],
                      texture2d<float, access::write> idOut [[texture(3)]],
                      texturecube<float> env [[texture(4)]])
{
    if (gid.x >= uint(F.screen.x) || gid.y >= uint(F.screen.y)) return;
    FRScene S = { tlas, insts, meshes, mats, texs, lamps, env };
    float3 dir = FRCameraRay(F, float2(gid) + 0.5f);
    FRIsect isect;
    isect.assume_geometry_type(geometry_type::triangle);
    isect.force_opacity(forced_opacity::opaque);
    float tMin = F.camPos.w;
    for (uint guard = 0u; guard < 6u; guard++)
    {
        ray r(F.camPos.xyz, dir, tMin, F.camFwd.w);
        FRHit hit = isect.intersect(r, S.tlas, FR_RAYMASK_PRIMARY);
        if (hit.type == intersection_type::none) break;
        FRInstance I = S.insts[hit.instance_id];
        if (I.flags & FR_FLAG_GLASS) { tMin = hit.distance + 1e-4f; continue; }   // the raster's glass is transparent
        FRSurfaceHit h = FRFetchHit(S, hit.instance_id, hit.geometry_id, hit.primitive_id, hit.triangle_barycentric_coord, dir);
        FRPbrInput pb = FREvaluateMaterial(S, h, dir, F.fog.z * hit.distance);
        uint unusedShadow = 0u;
        float3 L = FRShade(F, S, h, pb, dir, unusedShadow);
        if (F.fog.y > 0.5f)
        {
            float z = hit.distance * dot(dir, F.camFwd.xyz);
            float vis = exp2(-(F.fog.x * z) * (F.fog.x * z));
            L = mix(F.fogColor.xyz, L, vis);
        }
        rawOut.write(float4(L, 1.0f), gid);
        idOut.write(float4(float(hit.instance_id + 1u), hit.distance, float(h.material), 0.0f), gid);
        return;
    }
    rawOut.write(float4(0.0f), gid);
    idOut.write(float4(0.0f), gid);
}

// ------------------------------------------------------------------ geometry gather (non-readable meshes)
struct FRGatherVertexArgs
{
    uint vertexCount;
    uint posStride, posOffset;
    uint nrmStride, nrmOffset, nrmFormat;
    uint uvStride, uvOffset, uvFormat;
    uint colStride, colOffset, colFormat;
};

// formats: 0 absent, 1 Float32, 2 Float16, 3 UNorm8, 4 SNorm8, 5 UNorm16, 6 SNorm16
static inline float FRReadComp(device const uchar* s, uint format, uint i)
{
    switch (format)
    {
        case 1u: return ((device const float*)s)[i];
        case 2u: return float(((device const half*)s)[i]);
        case 3u: return float(s[i]) / 255.0f;
        case 4u: return max(float(((device const char*)s)[i]) / 127.0f, -1.0f);
        case 5u: return float(((device const ushort*)s)[i]) / 65535.0f;
        case 6u: return max(float(((device const short*)s)[i]) / 32767.0f, -1.0f);
        default: return 0.0f;
    }
}

kernel void fr_gather_vertices(uint v [[thread_position_in_grid]],
                               constant FRGatherVertexArgs& A [[buffer(0)]],
                               device const uchar* posStream [[buffer(1)]],
                               device const uchar* nrmStream [[buffer(2)]],
                               device const uchar* uvStream [[buffer(3)]],
                               device const uchar* colStream [[buffer(4)]],
                               device float* outPos [[buffer(5)]],
                               device FRVertexAttr* outAttr [[buffer(6)]])
{
    if (v >= A.vertexCount) return;
    device const float* pp = (device const float*)(posStream + v * A.posStride + A.posOffset);
    outPos[v * 3u] = pp[0]; outPos[v * 3u + 1u] = pp[1]; outPos[v * 3u + 2u] = pp[2];
    FRVertexAttr a;
    a.nx = 0.0f; a.ny = 1.0f; a.nz = 0.0f; a.colorR = 0.0f; a.u = 0.0f; a.v = 0.0f;
    if (A.nrmFormat != 0u)
    {
        device const uchar* s = nrmStream + v * A.nrmStride + A.nrmOffset;
        a.nx = FRReadComp(s, A.nrmFormat, 0u); a.ny = FRReadComp(s, A.nrmFormat, 1u); a.nz = FRReadComp(s, A.nrmFormat, 2u);
    }
    if (A.uvFormat != 0u)
    {
        device const uchar* s = uvStream + v * A.uvStride + A.uvOffset;
        a.u = FRReadComp(s, A.uvFormat, 0u); a.v = FRReadComp(s, A.uvFormat, 1u);
    }
    if (A.colFormat != 0u)
    {
        device const uchar* s = colStream + v * A.colStride + A.colOffset;
        a.colorR = FRReadComp(s, A.colFormat, 0u);
    }
    outAttr[v] = a;
}

struct FRGatherIndexArgs
{
    uint count;
    uint indexSize;    // 2 or 4
    uint srcStart;     // first index in Unity's index buffer
    uint dstStart;     // first index in the plugin's index array
    uint baseVertex;
    uint pad0, pad1, pad2;
};

kernel void fr_gather_indices(uint i [[thread_position_in_grid]],
                              constant FRGatherIndexArgs& A [[buffer(0)]],
                              device const uchar* ib [[buffer(1)]],
                              device uint* outIdx [[buffer(2)]])
{
    if (i >= A.count) return;
    uint idx = A.indexSize == 2u ? uint(((device const ushort*)ib)[A.srcStart + i]) : ((device const uint*)ib)[A.srcStart + i];
    outIdx[A.dstStart + i] = idx + A.baseVertex;
}

// ------------------------------------------------------------------ fr_layout (self-test)
kernel void fr_layout(uint tid [[thread_position_in_grid]],
                      device const FRInstance* inst [[buffer(0)]],
                      device const FRMaterial* mat [[buffer(1)]],
                      device const FRLamp* lamp [[buffer(2)]],
                      device const FRMeshGPU* mesh [[buffer(3)]],
                      constant FRFrame& F [[buffer(4)]],
                      device const FRVertexAttr* va [[buffer(5)]],
                      device uint* o [[buffer(6)]])
{
    if (tid != 0u) return;
    uint k = 0u;
    o[k++] = sizeof(FRInstance); o[k++] = sizeof(FRMaterial); o[k++] = sizeof(FRLamp); o[k++] = sizeof(FRMeshGPU);
    o[k++] = sizeof(FRFrame); o[k++] = sizeof(FRVertexAttr); o[k++] = sizeof(FRTexSlot);
    FRInstance I = inst[1];
    o[k++] = as_type<uint>(I.o2w0.x); o[k++] = as_type<uint>(I.o2w2.w); o[k++] = as_type<uint>(I.w2o1.y);
    o[k++] = I.meshSlot; o[k++] = I.materialBase; o[k++] = I.flags; o[k++] = I.windowId; o[k++] = as_type<uint>(I.emission.w);
    FRMaterial M = mat[1];
    o[k++] = as_type<uint>(M.baseColor.x); o[k++] = as_type<uint>(M.tile.y); o[k++] = as_type<uint>(M.p2.w);
    o[k++] = M.kind; o[k++] = M.flags; o[k++] = M.texBase; o[k++] = M.texOcclusion;
    FRLamp L = lamp[1];
    o[k++] = as_type<uint>(L.posInvRange2.w); o[k++] = as_type<uint>(L.atten.w);
    FRMeshGPU G = mesh[1];
    o[k++] = G.vertexCount; o[k++] = G.indexCount; o[k++] = G.submeshCount;
    o[k++] = as_type<uint>(F.camPos.w); o[k++] = F.rect.z; o[k++] = as_type<uint>(F.sh[8].y); o[k++] = as_type<uint>(F.envDecode.x);
    o[k++] = as_type<uint>(F.fill.w); o[k++] = F.counts.y; o[k++] = F.shadow.w; o[k++] = as_type<uint>(F.invViewProj[3][2]);
    FRVertexAttr A = va[1];
    o[k++] = as_type<uint>(A.colorR); o[k++] = as_type<uint>(A.v);
    uint sentinel = 0xC0FFEE00u + k + 1u;
    o[k] = sentinel;
}
