// FRGlassRTLayout.h — the GPU layout self-test of FRGlassRTShared.h (R4), shared by the plugin (run once at
// device init, inside Unity's command buffer) and by tools/frglassrt_validate.mm (run at build time).
// The CPU writes element [1] of every shared struct with distinct values; the fr_layout kernel reads the
// fields back by name; any size or offset disagreement between C++ and MSL fails the check.
#pragma once
#import <Metal/Metal.h>
#include <cstring>
#include <string>
#include "FRGlassRTShared.h"

struct FRLayoutBuffers
{
    id<MTLBuffer> inst, mat, lamp, mesh, va, out;
    FRFrame frame;
};

static inline uint32_t FRBits(float f) { uint32_t u; std::memcpy(&u, &f, 4); return u; }

static inline bool FRLayoutCreate(id<MTLDevice> device, FRLayoutBuffers& b)
{
    b.inst = [device newBufferWithLength:sizeof(FRInstance) * 2 options:MTLResourceStorageModeShared];
    b.mat = [device newBufferWithLength:sizeof(FRMaterial) * 2 options:MTLResourceStorageModeShared];
    b.lamp = [device newBufferWithLength:sizeof(FRLamp) * 2 options:MTLResourceStorageModeShared];
    b.mesh = [device newBufferWithLength:sizeof(FRMeshGPU) * 2 options:MTLResourceStorageModeShared];
    b.va = [device newBufferWithLength:sizeof(FRVertexAttr) * 2 options:MTLResourceStorageModeShared];
    b.out = [device newBufferWithLength:4 * 64 options:MTLResourceStorageModeShared];
    if (!b.inst || !b.mat || !b.lamp || !b.mesh || !b.va || !b.out) return false;
    std::memset(b.inst.contents, 0, b.inst.length);
    std::memset(b.mat.contents, 0, b.mat.length);
    std::memset(b.lamp.contents, 0, b.lamp.length);
    std::memset(b.mesh.contents, 0, b.mesh.length);
    std::memset(b.va.contents, 0, b.va.length);
    std::memset(b.out.contents, 0, b.out.length);
    FRInstance* I = (FRInstance*)b.inst.contents + 1;
    I->o2w0.x = 1.5f; I->o2w2.w = 2.5f; I->w2o1.y = 3.5f;
    I->meshSlot = 11; I->materialBase = 12; I->flags = 13; I->windowId = 14; I->emission.w = 4.5f;
    FRMaterial* M = (FRMaterial*)b.mat.contents + 1;
    M->baseColor.x = 5.5f; M->tile.y = 6.5f; M->p2.w = 7.5f; M->kind = 21; M->flags = 22; M->texBase = 23; M->texOcclusion = 24;
    FRLamp* L = (FRLamp*)b.lamp.contents + 1;
    L->posInvRange2.w = 8.5f; L->atten.w = 9.5f;
    FRMeshGPU* G = (FRMeshGPU*)b.mesh.contents + 1;
    G->vertexCount = 31; G->indexCount = 32; G->submeshCount = 33;
    std::memset(&b.frame, 0, sizeof(b.frame));
    b.frame.camPos.w = 10.5f; b.frame.rect.z = 41; b.frame.sh[8].y = 11.5f; b.frame.envDecode.x = 12.5f;
    b.frame.fill.w = 13.5f; b.frame.counts.y = 42; b.frame.shadow.w = 43; b.frame.invViewProj.columns[3].z = 14.5f;
    FRVertexAttr* A = (FRVertexAttr*)b.va.contents + 1;
    A->colorR = 15.5f; A->v = 16.5f;
    return true;
}

static inline void FRLayoutEncode(id<MTLComputeCommandEncoder> enc, id<MTLComputePipelineState> pso, FRLayoutBuffers& b)
{
    [enc setComputePipelineState:pso];
    [enc setBuffer:b.inst offset:0 atIndex:0];
    [enc setBuffer:b.mat offset:0 atIndex:1];
    [enc setBuffer:b.lamp offset:0 atIndex:2];
    [enc setBuffer:b.mesh offset:0 atIndex:3];
    [enc setBytes:&b.frame length:sizeof(FRFrame) atIndex:4];
    [enc setBuffer:b.va offset:0 atIndex:5];
    [enc setBuffer:b.out offset:0 atIndex:6];
    [enc dispatchThreads:MTLSizeMake(1, 1, 1) threadsPerThreadgroup:MTLSizeMake(1, 1, 1)];
}

// Returns an empty string when the layout matches, else the first mismatch.
static inline std::string FRLayoutCheck(const FRLayoutBuffers& b)
{
    const uint32_t* o = (const uint32_t*)b.out.contents;
    const uint32_t expect[] = {
        (uint32_t)sizeof(FRInstance), (uint32_t)sizeof(FRMaterial), (uint32_t)sizeof(FRLamp), (uint32_t)sizeof(FRMeshGPU),
        (uint32_t)sizeof(FRFrame), (uint32_t)sizeof(FRVertexAttr), (uint32_t)sizeof(FRTexSlot),
        FRBits(1.5f), FRBits(2.5f), FRBits(3.5f), 11, 12, 13, 14, FRBits(4.5f),
        FRBits(5.5f), FRBits(6.5f), FRBits(7.5f), 21, 22, 23, 24,
        FRBits(8.5f), FRBits(9.5f),
        31, 32, 33,
        FRBits(10.5f), 41, FRBits(11.5f), FRBits(12.5f), FRBits(13.5f), 42, 43, FRBits(14.5f),
        FRBits(15.5f), FRBits(16.5f),
    };
    const char* names[] = {
        "sizeof FRInstance", "sizeof FRMaterial", "sizeof FRLamp", "sizeof FRMeshGPU", "sizeof FRFrame", "sizeof FRVertexAttr", "sizeof FRTexSlot",
        "FRInstance.o2w0.x", "FRInstance.o2w2.w", "FRInstance.w2o1.y", "FRInstance.meshSlot", "FRInstance.materialBase", "FRInstance.flags", "FRInstance.windowId", "FRInstance.emission.w",
        "FRMaterial.baseColor.x", "FRMaterial.tile.y", "FRMaterial.p2.w", "FRMaterial.kind", "FRMaterial.flags", "FRMaterial.texBase", "FRMaterial.texOcclusion",
        "FRLamp.posInvRange2.w", "FRLamp.atten.w",
        "FRMeshGPU.vertexCount", "FRMeshGPU.indexCount", "FRMeshGPU.submeshCount",
        "FRFrame.camPos.w", "FRFrame.rect.z", "FRFrame.sh[8].y", "FRFrame.envDecode.x", "FRFrame.fill.w", "FRFrame.counts.y", "FRFrame.shadow.w", "FRFrame.invViewProj[3].z",
        "FRVertexAttr.colorR", "FRVertexAttr.v",
    };
    const size_t n = sizeof(expect) / sizeof(expect[0]);
    for (size_t i = 0; i < n; i++)
        if (o[i] != expect[i])
            return std::string("layout mismatch at ") + names[i] + ": GPU " + std::to_string(o[i]) + " vs C++ " + std::to_string(expect[i]);
    if (o[n] != 0xC0FFEE00u + (uint32_t)n + 1u) return "layout probe sentinel missing (kernel did not run to the end)";
    return std::string();
}
