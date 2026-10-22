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
//   fr_resolve         1 px dilation, back-surface image, edge filter -> the camera's resolve target A.
//   fr_blur_h          smudge blur, horizontal: dense separable Gaussian inside one pane (depth tag), only where the
//                      pane is smudged (smoothness below F.soft.w); clean glass stays sharp. A -> B.
//   fr_blur_v          the vertical half, then temporal accumulation while the camera is still (neighbourhood-
//                      clamped), writes _FR_GlassRTReflection. B -> out.
//   fr_parity          debug: primary rays through the same hit shader over the whole screen.
//   fr_gather_vertices / fr_gather_indices   copy a non-readable Unity mesh into the plugin's own pool.
//   fr_layout          one-time layout self-test of every shared struct.
#include <metal_stdlib>
#include <metal_raytracing>
using namespace metal;
using namespace metal::raytracing;

constexpr sampler kRepeat(address::repeat, filter::linear, mip_filter::linear);
// Hit textures: trilinear + 16x anisotropic like the raster (FrontRoomsRenderSetup imports the surface maps with
// anisoLevel 16; QualitySettings anisotropicTextures = ForceEnable at levels 4-5), driven by explicit ray-cone gradients.
constexpr sampler kAniso(address::repeat, filter::linear, mip_filter::linear, max_anisotropy(16));
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
// Lens-border AA domain (fix round 32): c = x / (2 + x) per channel (Reinhard with its knee at 2), and back.
static inline float3 FRLensCompress(float3 x) { x = max(x, float3(0.0f)); return x / (2.0f + x); }
static inline float3 FRLensExpand(float3 c) { c = clamp(c, float3(0.0f), float3(0.999f)); return 2.0f * c / (1.0f - c); }

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

// Texture fetch with explicit UV gradients (one pixel's footprint): anisotropic like the raster.
static inline float4 FRTexG(thread const FRScene& S, uint slot, float2 uv, float2 g1, float2 g2)
{
    return S.texs[slot].tex.sample(kAniso, uv, gradient2d(g1, g2));
}

// The pixel's footprint at a hit (ray cone, Akenine-Moeller et al., RT Gems ch. 20): a circle of diameter coneWidth
// across the ray, i.e. an ellipse on the surface with its long axis (coneWidth / cos) along the ray's projection.
struct FRFootprint { float3 major; float3 minor; };

static inline FRFootprint FRMakeFootprint(float3 n, float3 rayDir, float coneWidth)
{
    float c = max(abs(dot(rayDir, n)), 0.02f);
    float3 ds = rayDir - n * dot(rayDir, n);
    float l = length(ds);
    float3 a = l > 1e-5f ? ds / l : (abs(n.y) < 0.9f ? normalize(cross(n, float3(0, 1, 0))) : float3(1, 0, 0));
    FRFootprint f;
    f.major = a * (coneWidth / c);
    f.minor = cross(n, a) * coneWidth;
    return f;
}

// UV gradients of a map whose UV = (dot(p, t), dot(p, b)) * scale (planar metres, or a mesh UV's local frame).
static inline void FRGrad(thread const FRFootprint& f, float3 t, float3 b, float2 scale, thread float2& g1, thread float2& g2)
{
    g1 = float2(dot(f.major, t), dot(f.major, b)) * scale;
    g2 = float2(dot(f.minor, t), dot(f.minor, b)) * scale;
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

static FRPbrInput FREvaluateMaterial(thread const FRScene& S, thread const FRSurfaceHit& h, float3 rayDir, float coneWidth)
{
    FRMaterial m = S.mats[h.material];
    FRPbrInput o;
    o.alpha = 1.0f;
    o.emission = float3(0.0f);
    o.occlusion = 1.0f;
    float3 nGeo = h.nVert;
    // EmissiveLens: the renderer's own emission this frame (troffer lenses from their property block; live props such as
    // the glow placard from their material instance), in place of the material record's value
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
        FRFootprint fp = FRMakeFootprint(nGeo, rayDir, coneWidth);
        float2 g1, g2;
        FRGrad(fp, t, b, uvPerMetre * m.baseST.xy / tile, g1, g2);
        float4 albedo;
        if (m.flags & FR_MAT_PRINT)
        {
            // FrontRoomsSurface.shader PrintAlbedo: the paper modulation (alpha, beta, gamma) in _BaseMap and the
            // ink of print frame 0 (_PrintTex) through the duotone ramp, in linear light. Known gap (PRINT-1, 30_final
            // §12.5): since main's print driver (FrontRoomsPrintDriver, 2026-10-22 10:20) the raster shades the live
            // flipbook (_FR_Print array, _FR_PrintClock, _FR_PrintWarp) while this hit shader still reads keyframe K00,
            // so after the first keyframe change a reflected wall can show another print than the wall itself.
            float3 paper = FRTexG(S, m.texBase, uv, g1, g2).rgb * float3(0.70f, 0.14f, 0.02f) + float3(0.46f, -0.001f, -0.004f);
            float2 rollUV = uv * m.printST.xy + m.printST.zw;
            float2 ink = FRTexG(S, m.texPrint, rollUV, g1 * m.printST.xy, g2 * m.printST.xy).rg * m.inkGround.w;
            float3 ramp = ink.r < 0.5f ? mix(m.inkGround.rgb, m.inkMid.rgb, ink.r * 2.0f) : mix(m.inkMid.rgb, m.inkDeep.rgb, ink.r * 2.0f - 1.0f);
            float3 inkColour = mix(ramp, m.inkCream.rgb, ink.g * 0.55f);
            albedo = float4(inkColour * paper.x + float3(0.194618f, 0.104616f, 0.023153f) * paper.y + paper.z, 1.0f) * m.baseColor;
        }
        else albedo = FRTexG(S, m.texBase, uv, g1, g2) * m.baseColor;
        float4 mask = FRTexG(S, m.texMask, uv, g1, g2);
        float3 nTS = FRUnpackNormal(FRTexG(S, m.texBump, uv, g1, g2), m.p0.w);

        float2 wearUV; float3 wt, wb;
        FRPlanarFrame(h.pos, nGeo, wearUV, wt, wb);
        float2 w1a, w1b, w2a, w2b;
        FRGrad(fp, wt, wb, float2(1.0f / 8.0f), w1a, w1b);
        FRGrad(fp, wb, -wt, float2(1.0f / 12.8f), w2a, w2b);
        float4 m1 = FRTexG(S, m.texMacro, wearUV / 8.0f, w1a, w1b);
        float4 m2 = FRTexG(S, m.texMacro, float2(wearUV.y, -wearUV.x) / 12.8f, w2a, w2b);
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
        {
            if (m.flags & FR_MAT_LAMP_FIELD)
            {
                // FrontRoomsSurface.shader _FR_LAMP_FIELD (exit-sign diffusers): the lamp weights come from the renderer's
                // property block (FrontRoomsExitSign: lit / failing / dead) when the instance carries them, else the material's.
                float3 w = (h.flags & FR_FLAG_LAMP_WEIGHTS) ? h.emissionOverride.xyz : float3(m.tile.z, m.tile.w, m.emission.w);
                float4 lf = FRTexG(S, m.texLampField, uv, g1, g2);
                o.emission = dot(lf.rgb, w) * lf.a * m.emission.rgb;
            }
            else o.emission = FRTexG(S, m.texEmission, uv, g1, g2).rgb * emissionColor;
        }
    }
    else if (m.kind == FR_KIND_LIT)
    {
        // URP Lit, metallic workflow (LitInput.hlsl InitializeStandardLitSurfaceData).
        float2 uv = h.uv * m.baseST.xy + m.baseST.zw;
        FRFootprint fp = FRMakeFootprint(nGeo, rayDir, coneWidth);
        float3 bt = h.tangentSign * cross(nGeo, h.tangent);
        float2 g1, g2;
        FRGrad(fp, h.tangent, bt, h.uvPerMetre * m.baseST.xy, g1, g2);
        float4 albedo = FRTexG(S, m.texBase, uv, g1, g2) * m.baseColor;
        float metallic = m.p0.y, smoothness = m.p0.x;
        if (m.flags & FR_MAT_METALLIC_MAP)
        {
            float4 mg = FRTexG(S, m.texMask, uv, g1, g2);
            metallic = mg.r;
            smoothness = ((m.flags & FR_MAT_SMOOTH_ALBEDO) ? albedo.a : mg.a) * m.p0.x;
        }
        else if (m.flags & FR_MAT_SMOOTH_ALBEDO) smoothness = albedo.a * m.p0.x;
        float3 n = nGeo;
        if (m.flags & FR_MAT_NORMAL_MAP)
        {
            float3 nTS = FRUnpackNormal(FRTexG(S, m.texBump, uv, g1, g2), m.p0.w);
            float3 b = h.tangentSign * cross(nGeo, h.tangent);
            n = normalize(h.tangent * nTS.x + b * nTS.y + nGeo * nTS.z);
        }
        if (m.flags & FR_MAT_OCCLUSION_MAP)
            o.occlusion = mix(1.0f, FRTexG(S, m.texOcclusion, uv, g1, g2).g, m.p0.z);
        o.albedo = albedo.rgb;
        o.alpha = albedo.a;
        o.metallic = metallic;
        o.smoothness = saturate(smoothness);
        o.normal = n;
        if (m.flags & FR_MAT_EMISSION)
            o.emission = FRTexG(S, m.texEmission, uv, g1, g2).rgb * emissionColor;
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

// Soft shadow of one lamp (AD-2, 2026-10-14). The raster's lamp shadows are URP soft shadows (PCF over a 1024 px spot
// shadow map spread over 162 deg): their penumbra is tens of centimetres wide on the carpet. One ray to the lamp's centre
// gave razor-sharp wedges instead. The lamp is treated as a disc of radius F.soft.x facing the hit:
//   F.soft.y == 1: one ray to a point of the disc picked by a hash of the hit's position (5 mm cells, plus the frame index
//                  while the still-camera accumulation runs): the penumbra is a fine grain locked to the world (it never
//                  crawls over the screen) that averages out when the camera stops;
//   F.soft.y >= 2: that many rays on a ring (0.75 x radius) rotated by the same hash, and F.soft.z more, interleaved,
//                  only where they disagree (the penumbra).
// F.soft.x == 0: one ray to the centre (hard). Returns the visible fraction.
static inline float FRHash(float3 p, uint salt)
{
    int3 c = int3(floor(p * 200.0f));
    uint h = uint(c.x) * 73856093u ^ uint(c.y) * 19349663u ^ uint(c.z) * 83492791u ^ salt * 2654435761u;
    h ^= h >> 13; h *= 0x5bd1e995u; h ^= h >> 15;
    return float(h & 0xffffffu) / 16777216.0f;
}

static float FRSoftShadow(constant FRFrame& F, thread const FRScene& S, float3 origin, float3 lampPos, float3 lightDir,
                          float3 spotDir, float dist, uint type, uint lampIndex, thread uint& shadowRays)
{
    uint base = max(uint(F.soft.y + 0.5f), 1u);
    if (type == FR_LAMP_DIRECTIONAL || F.soft.x <= 0.0f)
    {
        shadowRays++;
        return FRShadowRay(S, origin, lightDir, max(dist - 0.02f, 0.0f));
    }
    // The disc lies in the emitter's plane: a spot's (troffer lens) is perpendicular to its axis, horizontal under the
    // ceiling; a point light's faces the hit. (A disc facing the hit stood up vertically for wall points and put half its
    // samples into the ceiling slab: false shadows on the walls, fix pass run f2.)
    float3 nrm = type == FR_LAMP_SPOT ? normalize(spotDir) : lightDir;
    float3 up = abs(nrm.y) < 0.95f ? float3(0, 1, 0) : float3(1, 0, 0);
    float3 ax = normalize(cross(up, nrm));
    float3 ay = cross(nrm, ax);
    // world-locked hash (harness knob F.fill.z > 0.5: a fixed pattern, no hash)
    uint salt = lampIndex * 977u + (F.jitter.w > 0.5f ? F.shadow.z : 0u);
    float u0 = F.fill.z > 0.5f ? 0.0f : FRHash(origin, salt);
    if (base == 1u)
    {
        // one ray to a uniform point of the disc
        float u1 = F.fill.z > 0.5f ? 0.5f : FRHash(origin + float3(0.0025f), salt ^ 0x9e3779b9u);
        float rr = F.soft.x * sqrt(u1);
        float a = u0 * 6.2831853f;
        float3 d = lampPos + (ax * cos(a) + ay * sin(a)) * rr - origin;
        float dl = length(d);
        shadowRays++;
        return FRShadowRay(S, origin, d / max(dl, 1e-5f), max(dl - 0.02f, 0.0f));
    }
    uint extra = uint(F.soft.z + 0.5f);
    uint total = base + extra;
    float rot = u0 * 6.2831853f;
    float vis = 0.0f;
    uint traced = 0u;
    for (uint k = 0u; k < total; k++)
    {
        // the base rays agree (fully lit or fully shadowed): no penumbra here, stop
        if (k == base && (vis == 0.0f || vis == float(base))) break;
        // a ring of 0.75 x the radius: the base rays evenly spread, the refinement rays interleaved between them
        float a = rot + (k < base ? float(k) * (6.2831853f / float(base))
                                  : (float(k - base) + 0.5f) * (6.2831853f / float(max(extra, 1u))));
        float3 d = lampPos + (ax * cos(a) + ay * sin(a)) * (0.75f * F.soft.x) - origin;
        float dl = length(d);
        vis += FRShadowRay(S, origin, d / max(dl, 1e-5f), max(dl - 0.02f, 0.0f));
        traced++;
    }
    shadowRays += traced;
    return vis / float(max(traced, 1u));
}

#define FR_RANK_MAX 8u

// One lamp's unshadowed contribution at a hit (URP UniversalFragmentPBR's direct term). False when it adds nothing.
struct FRBrdf { float3 n, v, diffuse, specular; float roughness2, roughness2MinusOne, normalizationTerm; };

static inline bool FRLampLight(constant FRFrame& F, thread const FRLamp& L, thread const FRSurfaceHit& h, thread const FRBrdf& B,
                               thread float3& contribution, thread float3& lightDir, thread float& dist)
{
    uint type = uint(L.spotDir.w + 0.5f);
    float atten;
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
        if (distSqr * L.posInvRange2.w >= 1.0f) return false;
        float rd = rsqrt(distSqr);
        lightDir = lv * rd;
        dist = distSqr * rd;
        atten = FRDistanceAttenuation(distSqr, L.posInvRange2.w);
        if (type == FR_LAMP_SPOT) atten *= FRAngleAttenuation(L.spotDir.xyz, lightDir, L.atten.zw);
    }
    float NdotL = saturate(dot(B.n, lightDir));
    if (atten <= 0.0f || NdotL <= 0.0f) return false;
    float3 halfDir = normalize(lightDir + B.v);
    float NoH = saturate(dot(B.n, halfDir));
    float LoH = saturate(dot(lightDir, halfDir));
    float d = NoH * NoH * B.roughness2MinusOne + 1.00001f;
    float specularTerm = B.roughness2 / ((d * d) * max(0.1f, LoH * LoH) * B.normalizationTerm);
    contribution = (B.diffuse + B.specular * specularTerm) * (L.color.xyz * (atten * NdotL));
    return true;
}

static float3 FRShade(constant FRFrame& F, thread const FRScene& S, thread const FRSurfaceHit& h, thread const FRPbrInput& p,
                      float3 rayDir, thread uint& shadowRays)
{
    FRBrdf B;
    B.n = p.normal;
    B.v = -rayDir;
    float oneMinusReflectivity = 0.96f - p.metallic * 0.96f;   // OneMinusReflectivityMetallic, kDielectricSpec (0.04)
    float reflectivity = 1.0f - oneMinusReflectivity;
    B.diffuse = p.albedo * oneMinusReflectivity;
    B.specular = mix(float3(0.04f), p.albedo, p.metallic);
    float perceptualRoughness = 1.0f - p.smoothness;
    float roughness = max(perceptualRoughness * perceptualRoughness, FR_HALF_MIN_SQRT);
    B.roughness2 = max(roughness * roughness, FR_HALF_MIN);
    float grazing = saturate(p.smoothness + reflectivity);
    B.normalizationTerm = roughness * 4.0f + 2.0f;
    B.roughness2MinusOne = B.roughness2 - 1.0f;

    // Global illumination: ambient SH + the zone cube, x material occlusion (no SSAO at hits).
    float NoV = saturate(dot(B.n, B.v));
    float fresnelTerm = FRPow4(1.0f - NoV);
    float3 indirectDiffuse = FRSampleSH(F, B.n);
    float3 indirectSpecular = FREnvironment(F, S, reflect(-B.v, B.n), perceptualRoughness);
    float surfaceReduction = 1.0f / (B.roughness2 + 1.0f);
    float3 envSpec = surfaceReduction * mix(B.specular, float3(grazing), fresnelTerm);
    float3 color = (indirectDiffuse * B.diffuse + indirectSpecular * envSpec) * p.occlusion;

    // Direct lights: every lamp the raster has on this frame. Lamps whose shadow the raster draws this frame (w > 0) are
    // RANKED per hit by their unshadowed contribution (AD-2): the strongest F.shadow.x of them get soft shadow rays; the
    // rest take the average visibility of the traced ones (weighted by contribution). Before 2026-10-14 the budget went
    // to the lamps nearest the CAMERA, in list order, and a shadowed lamp outside it shone through walls at full
    // strength: hard light wedges on the reflected carpet that could switch as the camera moved.
    // Registers: the ranked set holds only luminance + index (compile-time indexed, unrolled); the traced lamps'
    // contributions are evaluated again in the second loop, and the untraced ones are the shadowed sum minus the traced.
    uint lampCount = F.counts.x;
    uint budget = min(F.shadow.x, FR_RANK_MAX);
    float3 shadowOrigin = h.pos + h.nGeo * 0.002f;
    float rankL[FR_RANK_MAX];
    uint rankI[FR_RANK_MAX];
    for (uint j = 0u; j < FR_RANK_MAX; j++) { rankL[j] = -1.0f; rankI[j] = 0u; }
    float3 shC = float3(0.0f);     // every shadowed lamp: unshadowed sum ...
    float3 shCS = float3(0.0f);    // ... and the same weighted by its shadow strength
    for (uint i = 0; i < lampCount; i++)
    {
        FRLamp L = S.lamps[i];
        float3 contribution, lightDir; float dist;
        if (!FRLampLight(F, L, h, B, contribution, lightDir, dist)) continue;
        // no raster shadow this frame, a back-facing geometric normal, or too weak to show (< 0.01 linear before the
        // glass's Fresnel: under 1/255 of the final pixel even at grazing): unshadowed
        float lum = FRLuma(contribution);
        float s = L.color.w;
        if (s <= 0.0f || budget == 0u || dot(h.nGeo, lightDir) <= 0.0f || lum <= 0.01f) { color += contribution; continue; }
        shC += contribution; shCS += contribution * s;
        // (harness knob F.fill.y > 0.5: no ranking: the first `budget` in list order = nearest the camera, as before)
        if (F.fill.y > 0.5f) lum = 1e20f;
        // keep the `budget` strongest (descending): one branch-free bubble pass
        float cL = lum; uint cI = i;
        for (uint j = 0u; j < FR_RANK_MAX; j++)
        {
            bool take = j < budget && cL > rankL[j];
            float tL = rankL[j]; uint tI = rankI[j];
            rankL[j] = take ? cL : tL; rankI[j] = take ? cI : tI;
            cL = take ? tL : cL; cI = take ? tI : cI;
        }
    }
    float visW = 0.0f, visSum = 0.0f;
    for (uint k = 0u; k < FR_RANK_MAX; k++)
    {
        if (rankL[k] < 0.0f) continue;
        FRLamp L = S.lamps[rankI[k]];
        float3 contribution, lightDir; float dist;
        FRLampLight(F, L, h, B, contribution, lightDir, dist);
        uint type = uint(L.spotDir.w + 0.5f);
        float vis = FRSoftShadow(F, S, shadowOrigin, L.posInvRange2.xyz, lightDir, L.spotDir.xyz, dist, type, rankI[k], shadowRays);
        color += contribution * (1.0f - L.color.w * (1.0f - vis));
        shC -= contribution; shCS -= contribution * L.color.w;
        float wgt = F.fill.y > 0.5f ? 1.0f : FRLuma(contribution);
        visW += wgt; visSum += wgt * vis;
    }
    // the untraced shadowed lamps: the traced lamps' visibility here (a point the strong lamps cannot see is most likely
    // shadowed from the weaker ones too; never full light through a wall), or one half when none was traced
    float vbar = visW > 0.0f ? visSum / visW : 0.5f;
    if (F.fill.x > 0.5f) vbar = 1.0f;   // harness knob: the untraced shadowed lamps at full light (the pre-2026-10-14 rule)
    color += max(shC - shCS * (1.0f - vbar), float3(0.0f));
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

// AD-1 (fix round 31, 2026-10-22): the direction of a glass pixel's mirror ray. The prepass normal carries the smudge,
// print and grime micro-normals (FrontRooms/Glass's _SmearNormal map, about one pixel per texel at 1-2 m): one ray along
// it is one random sample of the smudge's glossy lobe. Inside a dark reflected object some of those rays reached the
// floor behind it, and the sparse smudge blur could not average them away: carpet-coloured blocks inside the Relay's boot
// and the test cube (review 1, art AD-1). The ray now follows the pane's mean normal over a 13x13 pixel window (25 taps
// of the same pane, every 3rd pixel): the pane's orientation, the slow roll wave and GD3's bow do not change at that
// scale, the smear's micro-normals and its centimetre wipe streaks average out, so the traced image is coherent. The lobe's width is put back by the smudge blur alone (fr_resolve's
// radius from the smoothness and from the micro-normals' spread). Clean glass is unchanged (its normals are smooth).
static inline float3 FRMeanNormal(constant FRFrame& F, texture2d<float, access::read> gDepth, texture2d<float, access::read> gNormal,
                                  uint2 p, float d, float3 n0)
{
    float tol = 0.02f + 0.01f * d;                 // = FRTagTol(1 + d): the same pane
    int2 hi = int2(F.screen.xy) - 1;
    float3 s = float3(0.0f);
    for (int dy = -6; dy <= 6; dy += 3)
    for (int dx = -6; dx <= 6; dx += 3)
    {
        int2 q = clamp(int2(p) + int2(dx, dy), int2(0), hi);
        float dq = gDepth.read(uint2(q)).x;
        if (!(dq > 0.0f) || abs(dq - d) > tol) continue;
        float3 nq = gNormal.read(uint2(q)).xyz;
        float l = length(nq);
        if (l < 1e-4f) continue;
        nq /= l;
        s += dot(nq, n0) < 0.0f ? -nq : nq;
    }
    float ls = length(s);
    return ls > 1e-4f ? s / ls : n0;
}

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
    float3 n = FRMeanNormal(F, gDepth, gNormal, p, d, normalize(nrm.xyz));   // AD-1: the coherent mirror direction
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

// Every neighbour read of the AA and resolve passes stays inside this frame's dispatch rectangle (the union of this and
// the last frame's receiver rectangles). Outside it the raw / rawAA / resolve targets hold texels from an earlier,
// larger rectangle: Ultra's 1 px dilation copied them as a 1-texel line beside the glass (E5, 2026-10-14).
static inline bool FRInRect(constant FRFrame& F, int2 q)
{
    return q.x >= int(F.rect.x) && q.y >= int(F.rect.y) && q.x < int(F.rect.z) && q.y < int(F.rect.w);
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
    for (int dy = -1; dy <= 1; dy++)
    for (int dx = -1; dx <= 1; dx++)
    {
        int2 q = int2(p) + int2(dx, dy);
        if (!FRInRect(F, q)) continue;
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
    float3 n = FRMeanNormal(F, gDepth, gNormal, p, d, normalize(nrm.xyz));   // AD-1: the same direction as fr_trace
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

// ------------------------------------------------------------------ fr_resolve, fr_blur_h, fr_blur_v
static inline float FRTagTol(float tag) { return 0.02f + 0.01f * max(tag - 1.0f, 0.0f); }

// Smudge blur radius in pixels (design 10 §1.6): the pane's roughness spread over the reflected path, seen from the eye.
// None on clean glass (AD-3, 2026-10-14): real float glass with a light dust film keeps a sharp mirror image; only the
// smudged, printed and grimy parts (prepass smoothness below F.soft.w) blur. fr_resolve averages it over the pane around
// the pixel and drops what stays under 1 px (fix round 31; it was 1.5 px per pixel).
static inline float FRBlurRadius(constant FRFrame& F, float tag, float hitDist, float smooth)
{
    if (hitDist <= 0.0f || smooth >= F.soft.w) return 0.0f;
    float pr = 1.0f - smooth;
    float alpha = pr * pr;
    float d1 = tag - 1.0f;
    // AD-1 (fix round 31): faded in over 0.1 of smoothness below the cut-off instead of starting at full size there, so the
    // radius field has no step at a smudge's rim (a separable blur turns a step in its radius into rectangles)
    float ramp = saturate((F.soft.w - smooth) * 10.0f);
    return min((hitDist * alpha) / max((d1 + hitDist) * F.fog.z, 1e-6f), F.blur.x) * ramp;   // no threshold: averaged first
}

kernel void fr_resolve(uint2 gid [[thread_position_in_grid]],
                       constant FRFrame& F [[buffer(0)]],
                       texture2d<float, access::read> raw [[texture(0)]],
                       texture2d<float, access::read_write> aux [[texture(1)]],
                       texture2d<float, access::read> gDepth [[texture(2)]],
                       texture2d<float, access::read> gNormal [[texture(3)]],
                       texture2d<float, access::write> outA [[texture(4)]])
{
    uint2 p = gid + F.rect.xy;
    if (p.x >= F.rect.z || p.y >= F.rect.w) return;
    float4 c = raw.read(p);
    float gd = gDepth.read(p).x;
    if (c.a <= 1.0f)
    {
        if (gd > 0.0f) { outA.write(float4(0.0f), p); return; }   // a glass pixel whose ray missed: the zone cube
        // 1 px dilation: an MSAA edge sample of the pane whose pixel centre is off the pane
        const int2 nb[4] = { int2(1, 0), int2(-1, 0), int2(0, 1), int2(0, -1) };
        for (uint k = 0; k < 4u; k++)
        {
            int2 q = int2(p) + nb[k];
            if (!FRInRect(F, q)) continue;
            float4 s = raw.read(uint2(q));
            if (s.a > 1.0f) { outA.write(s, p); return; }
        }
        outA.write(float4(0.0f), p);
        return;
    }
    float tag = c.a;
    float tol = FRTagTol(tag);
    float4 ax = aux.read(p);
    float hitDist = ax.x;
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
                    if (!FRInRect(F, q)) continue;
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

    // --- edge filter (FXAA-style luminance contrast inside one tag), High and Ultra
    float lc = FRLuma(c.rgb);
    float lmin = lc, lmax = lc;
    float3 nbSum = float3(0.0f), nbCSum = float3(0.0f); float nbW = 0.0f;
    for (int dy = -1; dy <= 1; dy++)
    for (int dx = -1; dx <= 1; dx++)
    {
        if (dx == 0 && dy == 0) continue;
        int2 q = int2(p) + int2(dx, dy);
        if (!FRInRect(F, q)) continue;
        float4 s = raw.read(uint2(q));
        if (s.a <= 1.0f || abs(s.a - tag) > tol) continue;
        float l = FRLuma(s.rgb);
        lmin = min(lmin, l); lmax = max(lmax, l);
        float w = (dx == 0 || dy == 0) ? 1.0f : 0.5f;
        nbSum += s.rgb * w; nbW += w;
        nbCSum += FRLensCompress(s.rgb) * w;
    }
    if (F.blur.y > 0.0f && nbW > 0.0f)
    {
        // Without TAA the 1-ray mirror is point-sampled: at a high-contrast pixel (lens border, print edge, door
        // frame) blend toward the 3x3 kernel [.5 1 .5; 1 1 1; .5 1 .5] / 7 inside the same pane. Strength per tier.
        float contrast = (lmax - lmin) / max(lmax, 1e-3f);
        float edge = smoothstep(0.15f, 0.60f, contrast) * F.blur.y;
        float lit = smoothstep(1.0f, 2.0f, lmax);
        float inside = smoothstep(0.6f, 1.0f, lmin);
        // inside a lit lens (the whole 3x3 lit) the linear kernel is scaled by F.fogColor.w (C# LensInteriorFilter): it
        // smooths the lens's own structure (less crawl) but also its peaks (the AD-3 core)
        float3 filtered = mix(L, (L + nbSum) / (1.0f + nbW), edge * mix(1.0f, F.fogColor.w, lit * inside));
        // Lit lens BORDER (fix round 32, review 2 art #2). Round 31 switched the filter off wherever the 3x3 held a lit
        // lens (luminance > 1-2), to stop the linear kernel spreading each reflected troffer by a pixel all round (AD-3
        // footprint). That also switched it off INSIDE the lenses and on their borders, and the moving shot's lens crawl
        // rose 0.096 -> 0.136. Now: inside a lens (the whole 3x3 lit) the linear kernel runs as before (every tap is lit,
        // so no pixel crosses the footprint threshold); on the border (lit and dark taps) the same kernel runs in a
        // compressed domain c = x / (2 + x): a 1-px AA that softens the step mostly on the lit side and lifts the dark
        // neighbour of a 4.7 lens only to ~0.5 (below the 0.8 footprint threshold). Clean glass and dark edges keep the
        // linear filter.
        float border = lit * (1.0f - inside);
        if (border > 0.0f)
        {
            float3 cL = FRLensCompress(L);
            float3 soft = FRLensExpand(mix(cL, (cL + nbCSum) / (1.0f + nbW), edge));
            filtered = mix(filtered, soft, border);
        }
        L = filtered;
    }

    // --- the smudge blur radius (aux.w) for fr_blur_h / fr_blur_v: the pane's roughness (smoothness) over the reflected
    // path, AVERAGED over the pane around this pixel (25 taps 4 px apart, Gaussian weights, sigma 5 px).
    // Fix round 31 (AD-1, review 1): a separable Gaussian is exact only while its radius changes slowly. Per pixel the
    // radius jumped (the smear texture is patchy, the hit distance changes at a reflected silhouette, and the 3x3
    // normal-spread term flickered), and the two passes turned every jump into a rectangle: the carpet-coloured blocks
    // and the flat-topped boot inside dark reflected objects. The normal-spread term is gone: the trace follows the mean
    // normal now (no speckle to hide) and a smudge's roughness is its smoothness. Clean glass stays sharp (AD-3): its
    // smoothness is above F.soft.w, so every tap gives 0. aux.xy of the neighbours are read while this kernel rewrites
    // them unchanged (only .w changes), so any read returns the same hit distance and smoothness.
    float radius = 0.0f;
    if (hitDist > 0.0f)
    {
        float rs = 0.0f, ws = 0.0f;
        for (int j = -2; j <= 2; j++)
        for (int i = -2; i <= 2; i++)
        {
            int2 q = int2(p) + int2(i, j) * 4;
            if (!FRInRect(F, q)) continue;
            float tq = raw.read(uint2(q)).a;
            if (tq <= 1.0f || abs(tq - tag) > tol) continue;
            float2 aq = (i == 0 && j == 0) ? ax.xy : aux.read(uint2(q)).xy;
            if (aq.x <= 0.0f) continue;
            float w = exp(-float(i * i + j * j) * 0.32f);
            rs += w * FRBlurRadius(F, tq, aq.x, aq.y); ws += w;
        }
        radius = ws > 0.0f ? rs / ws : 0.0f;
        if (radius < 1.0f) radius = 0.0f;
    }
    aux.write(float4(ax.xyz, radius), p);
    outA.write(float4(L, tag), p);
}

// One half of the smudge blur (AD-1, 2026-10-14): a separable Gaussian (sigma = radius / 2; taps every radius/6 px,
// so at most ~6 a side) over the texels of the same pane (depth tag) that hold a traced reflection. A smudge is
// a glossy mirror: everything inside its lobe mixes, near and far hits alike, so a dark object keeps a dark interior and
// gets a soft silhouette. The 12-tap disc it replaces used a random rotation per pixel: 13 samples of the lobe gave
// salt-and-pepper speckle (carpet specks inside the Relay, a hairy fringe on the test cube). A hit-distance test was tried
// first (fix pass run f1): it kept exactly those specks (a speck is a pixel whose ray reached the far floor, and the test
// kept it from mixing with its near neighbours), so the taps are weighted by distance on the screen only.
// AD-1 (fix round 31): both passes blur with the same SMOOTHED radius: the mean of fr_resolve's radius over a 9x9 pixel
// window (taps every 4 px, this pane only; clean texels count as 0). A separable blur whose radius jumps between
// neighbouring rows or columns leaves stripes and rectangles (review 1, art AD-1: carpet blocks inside the Relay's boot,
// streaks at the cube's lower edge); a radius that varies slowly is blurred correctly by two passes.
static float FRSmoothRadius(constant FRFrame& F, texture2d<float, access::read> src, texture2d<float, access::read> aux, uint2 p, float tag)
{
    float tol = FRTagTol(tag);
    float sum = 0.0f, n = 0.0f;
    for (int dy = -4; dy <= 4; dy += 4)
    for (int dx = -4; dx <= 4; dx += 4)
    {
        int2 q = int2(p) + int2(dx, dy);
        if (!FRInRect(F, q)) continue;
        if (abs(src.read(uint2(q)).a - tag) > tol) continue;
        sum += aux.read(uint2(q)).w; n += 1.0f;
    }
    float r = n > 0.0f ? sum / n : 0.0f;
    return r >= 1.0f ? r : 0.0f;
}

static float4 FRBlurAxis(constant FRFrame& F, texture2d<float, access::read> src, texture2d<float, access::read> aux,
                         uint2 p, int2 axis, float4 c, float radius)
{
    float tag = c.a;
    float tol = FRTagTol(tag);
    float sigma = max(radius * 0.5f, 0.5f);
    float inv2s2 = 0.5f / (sigma * sigma);
    int r = int(ceil(radius));
    int step = max(1, (r + 3) / 6);   // <= ~6 taps a side: sigma / step >= 1.5, smooth for a Gaussian (fix pass: dense taps cost 1 ms at 0.55 m)
    float3 acc = c.rgb; float wsum = 1.0f;
    for (int k = step; k <= r; k += step)
    {
        float w = exp(-float(k * k) * inv2s2) * float(step);
        for (int sgn = -1; sgn <= 1; sgn += 2)
        {
            int2 q = int2(p) + axis * (k * sgn);
            if (!FRInRect(F, q)) continue;
            float4 s = src.read(uint2(q));
            if (s.a <= 1.0f || abs(s.a - tag) > tol) continue;
            if (aux.read(uint2(q)).x <= 0.0f) continue;   // a dilated edge texel or a miss: not this pane's reflection
            acc += s.rgb * w; wsum += w;
        }
    }
    return float4(acc / wsum, tag);
}

kernel void fr_blur_h(uint2 gid [[thread_position_in_grid]],
                      constant FRFrame& F [[buffer(0)]],
                      texture2d<float, access::read> inA [[texture(0)]],
                      texture2d<float, access::read> aux [[texture(1)]],
                      texture2d<float, access::write> outB [[texture(2)]],
                      texture2d<float, access::read> gDepth [[texture(3)]])
{
    uint2 p = gid + F.rect.xy;
    if (p.x >= F.rect.z || p.y >= F.rect.w) return;
    float4 c = inA.read(p);
    float radius = c.a > 1.0f && gDepth.read(p).x > 0.0f ? FRSmoothRadius(F, inA, aux, p, c.a) : 0.0f;   // fr_resolve's, smoothed
    outB.write(radius > 0.0f ? FRBlurAxis(F, inA, aux, p, int2(1, 0), c, radius) : c, p);
}

kernel void fr_blur_v(uint2 gid [[thread_position_in_grid]],
                      constant FRFrame& F [[buffer(0)]],
                      texture2d<float, access::read> inA [[texture(0)]],
                      texture2d<float, access::read> inB [[texture(1)]],
                      texture2d<float, access::read> aux [[texture(2)]],
                      texture2d<float, access::read> gDepth [[texture(3)]],
                      texture2d<float, access::read_write> outTex [[texture(4)]])
{
    uint2 p = gid + F.rect.xy;
    if (p.x >= F.rect.z || p.y >= F.rect.w) return;
    float4 c = inB.read(p);
    if (c.a <= 1.0f || !(gDepth.read(p).x > 0.0f)) { outTex.write(c, p); return; }   // dilated edge texel, miss or none
    float radius = FRSmoothRadius(F, inA, aux, p, c.a);   // the same smoothed field as fr_blur_h (read from the same inputs)
    float4 L = radius > 0.0f ? FRBlurAxis(F, inB, aux, p, int2(0, 1), c, radius) : c;
    // --- temporal accumulation while the camera and the scene are still (no reprojection, clamped to the
    // neighbourhood of the unblurred picture)
    if (F.jitter.w > 0.5f)
    {
        float tag = c.a, tol = FRTagTol(tag);
        float4 c0 = inA.read(p);
        float3 cmin = c0.rgb, cmax = c0.rgb;
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            int2 q = int2(p) + int2(dx, dy);
            if ((dx == 0 && dy == 0) || !FRInRect(F, q)) continue;
            float4 s = inA.read(uint2(q));
            if (s.a <= 1.0f || abs(s.a - tag) > tol) continue;
            cmin = min(cmin, s.rgb); cmax = max(cmax, s.rgb);
        }
        cmin = min(cmin, L.rgb); cmax = max(cmax, L.rgb);
        float4 prev = outTex.read(p);
        if (prev.a > 1.0f && abs(prev.a - tag) <= tol)
        {
            float3 h = clamp(prev.rgb, cmin, cmax);
            L.rgb = mix(h, L.rgb, F.jitter.z);
        }
    }
    outTex.write(L, p);
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
    o[k++] = M.kind; o[k++] = M.flags; o[k++] = M.texBase; o[k++] = M.texOcclusion; o[k++] = as_type<uint>(M.inkCream.z); o[k++] = M.texPrint; o[k++] = M.texLampField;
    FRLamp L = lamp[1];
    o[k++] = as_type<uint>(L.posInvRange2.w); o[k++] = as_type<uint>(L.atten.w);
    FRMeshGPU G = mesh[1];
    o[k++] = G.vertexCount; o[k++] = G.indexCount; o[k++] = G.submeshCount;
    o[k++] = as_type<uint>(F.camPos.w); o[k++] = F.rect.z; o[k++] = as_type<uint>(F.sh[8].y); o[k++] = as_type<uint>(F.envDecode.x);
    o[k++] = as_type<uint>(F.fill.w); o[k++] = F.counts.y; o[k++] = F.shadow.w; o[k++] = as_type<uint>(F.invViewProj[3][2]); o[k++] = as_type<uint>(F.soft.w);
    FRVertexAttr A = va[1];
    o[k++] = as_type<uint>(A.colorR); o[k++] = as_type<uint>(A.v);
    uint sentinel = 0xC0FFEE00u + k + 1u;
    o[k] = sentinel;
}
