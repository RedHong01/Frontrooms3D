// FRGlassRTShared.h — every structure shared between the C++ plugin (FrontRoomsMetalGlassRT.mm,
// FRGlassRTScene.mm), the MSL kernels (FrontRoomsGlassRT.metal, compiled at runtime from an embedded
// copy) and, field for field, the C# StructLayout.Sequential mirrors in
// Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRTNative.cs.
//
// Rules (design 10 §1.4, R4):
//  - only float4 / float4x4 / uint4 / 32-bit scalars / device pointers (8 bytes) — no float3, no
//    implicit padding; every struct is a multiple of 16 bytes;
//  - C++ static_asserts pin every size; the plugin's GPU layout self-test (fr_layout) reads one
//    element of each struct back field by field at init and disables RT on any mismatch;
//  - change a struct here, in the .metal reader of fr_layout, and in the C# mirror together.
#ifndef FR_GLASS_RT_SHARED_H
#define FR_GLASS_RT_SHARED_H

#ifdef __METAL_VERSION__
  #include <metal_stdlib>
  using namespace metal;
  #define FR_PTR(T) device const T*
  #define FR_TEXSLOT texture2d<float>
#else
  #include <stdint.h>
  #include <simd/simd.h>
  typedef simd_float4 float4;
  typedef simd_float4x4 float4x4;
  typedef simd_uint4 uint4;
  typedef uint32_t uint;
  #define FR_PTR(T) uint64_t
  #define FR_TEXSLOT uint64_t
#endif

// ---------------------------------------------------------------- instance flags (C# FrontRoomsGlassRTFlags)
#define FR_FLAG_GLASS            (1u << 0)
#define FR_FLAG_FRACTURE_EDGE    (1u << 1)
#define FR_FLAG_SHARD            (1u << 2)
#define FR_FLAG_EMISSIVE_LENS    (1u << 3)
#define FR_FLAG_DYNAMIC          (1u << 4)
#define FR_FLAG_RELAY            (1u << 5)
#define FR_FLAG_DOOR             (1u << 6)
#define FR_FLAG_MESH_UV          (1u << 7)
#define FR_FLAG_SURFACE_PLANAR   (1u << 8)
#define FR_FLAG_DEFORMING        (1u << 9)
#define FR_FLAG_REFLECTION_ONLY  (1u << 10)
#define FR_FLAG_RECEIVER         (1u << 11)

// ---------------------------------------------------------------- instance masks (8 bit) and ray masks
#define FR_MASK_OPAQUE    0x01u
#define FR_MASK_GLASS     0x02u
#define FR_MASK_SHARD     0x04u
#define FR_MASK_DYNAMIC   0x08u
#define FR_MASK_REFLONLY  0x10u
#define FR_RAYMASK_REFLECTION (FR_MASK_OPAQUE | FR_MASK_GLASS | FR_MASK_DYNAMIC | FR_MASK_REFLONLY)
#define FR_RAYMASK_SHADOW     (FR_MASK_OPAQUE | FR_MASK_DYNAMIC)
#define FR_RAYMASK_PRIMARY    (FR_MASK_OPAQUE | FR_MASK_GLASS | FR_MASK_SHARD | FR_MASK_DYNAMIC)

// ---------------------------------------------------------------- material kinds and flags
#define FR_KIND_FLAT     0u   // base colour only
#define FR_KIND_SURFACE  1u   // FrontRooms/Surface (world-planar or mesh-UV metres)
#define FR_KIND_LIT      2u   // URP Lit / Simple Lit (metallic workflow)
#define FR_KIND_GLASS    3u   // see-through glass (FrontRooms/Glass, transparent Lit)

#define FR_MAT_MESH_UV        (1u << 0)
#define FR_MAT_EMISSION       (1u << 1)
#define FR_MAT_METALLIC_MAP   (1u << 2)
#define FR_MAT_SMOOTH_ALBEDO  (1u << 3)
#define FR_MAT_NORMAL_MAP     (1u << 4)
#define FR_MAT_OCCLUSION_MAP  (1u << 5)
#define FR_MAT_ALPHA_CLIP     (1u << 6)

// ---------------------------------------------------------------- lamps
#define FR_LAMP_DIRECTIONAL 0u
#define FR_LAMP_POINT       1u
#define FR_LAMP_SPOT        2u

// ---------------------------------------------------------------- debug modes (FRFrame.counts.z)
#define FR_DEBUG_NONE     0u
#define FR_DEBUG_PARITY   1u   // primary rays through the hit shader over the whole screen
#define FR_DEBUG_HITID    2u   // ids texture: reflected-hit instance + 1 (0 = miss)

// ---------------------------------------------------------------- stats buffer slots (uint32, GPU atomics)
#define FR_STAT_GLASS_PX    0u
#define FR_STAT_MISS_PX     1u
#define FR_STAT_HIT_PX      2u
#define FR_STAT_LAYER_PX    3u   // pixels whose ray passed through another pane
#define FR_STAT_AA_PX       4u   // Ultra adaptive pixels
#define FR_STAT_SHADOW_RAYS 5u
#define FR_STAT_COUNT       8u

// One vertex of the plugin's own geometry copy (24 B). Positions live in a separate packed float3 array
// (12 B per vertex) so the BLAS reads them with stride 12.
struct FRVertexAttr
{
    float nx, ny, nz;   // object-space normal
    float colorR;       // vertex colour R (fracture edge mask), 0 if the mesh has no colours
    float u, v;         // UV0
};

// Per mesh slot (48 B): GPU addresses into one plugin-owned MTLBuffer (or a shared range buffer).
struct FRMeshGPU
{
    FR_PTR(float)        positions;     // packed float3 per vertex
    FR_PTR(FRVertexAttr) attrs;
    FR_PTR(uint)         indices;       // uint32, base vertex already applied
    FR_PTR(uint)         submeshStart;  // first index of each submesh (geometry_id)
    uint vertexCount;
    uint indexCount;
    uint submeshCount;
    uint pad0;
};

// Per TLAS instance (128 B). instance_id of a hit indexes this array.
struct FRInstance
{
    float4 o2w0, o2w1, o2w2;   // rows of object-to-world
    float4 w2o0, w2o1, w2o2;   // rows of world-to-object (normals: transpose of its 3x3)
    uint meshSlot;
    uint materialBase;         // material of submesh k = materialBase + k
    uint flags;                // FR_FLAG_*
    uint windowId;             // destruction contract: window id (or ~0u)
    float4 emission;           // xyz: linear per-renderer emission (EmissiveLens, from the MPB); w: _CeilingHeight (MPB) or 0
};

// Per material (160 B).
struct FRMaterial
{
    float4 baseColor;    // linear rgb, a
    float4 tile;         // FrontRooms/Surface _TileSize.xy; zw unused
    float4 baseST;       // _BaseMap_ST
    float4 emission;     // linear _EmissionColor rgb; w unused
    float4 stain;        // linear _StainColor rgb; w = _StainStrength
    float4 p0;           // smoothness (scale or value), metallic, occlusion strength, bump scale
    float4 p1;           // _MacroTone, _MacroDirt, _WetStrength, _FloorGrime
    float4 p2;           // _FloorGrimeHeight, _CeilingGrime, _CeilingHeight (material), alpha cutoff
    uint kind;           // FR_KIND_*
    uint flags;          // FR_MAT_*
    uint texBase, texMask;          // texture slots (0 = white, 1 = black, 2 = flat normal, 3 = grey)
    uint texBump, texMacro, texEmission, texOcclusion;
};

// Per lamp (64 B).
struct FRLamp
{
    float4 posInvRange2;   // xyz world position (directional: unused), w = URP lightAttenuation.x (1/range^2)
    float4 color;          // linear colour x intensity (VisibleLight.finalColor); w = shadow strength if a shadow ray is allowed, else 0
    float4 spotDir;        // xyz = URP lightSpotDir (-forward); w = FR_LAMP_* type
    float4 atten;          // URP lightAttenuation (x 1/r^2, y fade, z invAngleRange, w add)
};

// Texture table entry (8 B): MTLResourceID of a 2D texture (Metal 3 bindless).
struct FRTexSlot
{
    FR_TEXSLOT tex;
};

// Per-frame constants (setBytes; 496 B).
struct FRFrame
{
    float4x4 invViewProj;  // inverse(GPU projection x view): the matrix the glass prepass rasterised with
    float4x4 viewProj;     // GPU projection x view (screen offsets of the back-surface image)
    float4 camPos;         // xyz camera position; w = near plane
    float4 camFwd;         // xyz camera forward; w = reflection ray length (far plane)
    float4 screen;         // width, height, 1/width, 1/height
    uint4  rect;           // dispatch rectangle x0, y0, x1, y1 (exclusive), in pixels
    float4 fog;            // x = unity_FogParams.x (exp2 density), y = fog on (1/0), z = pixel spread angle (rad), w = time
    float4 fogColor;       // linear fog colour
    float4 sh[9];          // ambient SH fit, basis {1, x, y, z, xy, yz, xz, x^2, y^2}, rgb each
    float4 envDecode;      // ReflectionProbe.defaultTextureHDRDecodeValues
    float4 envInfo;        // x = UNITY_SPECCUBE_LOD_STEPS, y = cube valid, z = glass top-up scale (1 + extra), w = cube mip count - 1
    uint4  counts;         // x lamp count, y quality (1 High, 2 Ultra), z debug mode, w instance count
    uint4  shadow;         // x max shadowed lamps per hit, y glass layers seen through, z frame index, w accumulation index (0 = no history)
    float4 jitter;         // xy sub-pixel jitter (pixels), z history weight 1/(n+1), w accumulate (0/1)
    float4 thin;           // x glass thickness (m), y ior, z ray origin offset (m), w back-surface image on (1/0)
    float4 blur;           // x smudge blur max radius (px), y edge filter strength, z adaptive AA luminance ratio, w adaptive budget fraction
    float4 fill;           // x fill shadow mode (0 none, 1 constant, 2 ray), y constant factor, z fill shadow strength, w directional lamp index (-1 none)
};

#ifndef __METAL_VERSION__
static_assert(sizeof(FRVertexAttr) == 24, "FRVertexAttr layout");
static_assert(sizeof(FRMeshGPU) == 48, "FRMeshGPU layout");
static_assert(sizeof(FRInstance) == 128, "FRInstance layout");
static_assert(sizeof(FRMaterial) == 160, "FRMaterial layout");
static_assert(sizeof(FRLamp) == 64, "FRLamp layout");
static_assert(sizeof(FRTexSlot) == 8, "FRTexSlot layout");
static_assert(sizeof(FRFrame) == 496, "FRFrame layout");
#endif

#endif // FR_GLASS_RT_SHARED_H
