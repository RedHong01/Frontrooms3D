// frglassrt_validate.mm — build-time check of the embedded MSL (design 10 §1.9, R3).
// Compiles the exact string the plugin embeds with the OS Metal compiler (no offline toolchain needed),
// creates every compute pipeline the plugin uses, and runs the fr_layout self-test on the GPU.
// Exit 0 = OK; any compile error, missing kernel or layout mismatch fails the build, not the game.
#import <Metal/Metal.h>
#import <Foundation/Foundation.h>
#include <cstdio>
#include <string>
#include "FRGlassRTEmbedded.h"
#include "../FRGlassRTLayout.h"

int main(int argc, char** argv)
{
    @autoreleasepool
    {
        id<MTLDevice> device = MTLCreateSystemDefaultDevice();
        if (!device) { fprintf(stderr, "frglassrt_validate: no Metal device\n"); return 2; }
        printf("frglassrt_validate: %s, supportsRaytracing %d, Apple9 %d, Metal3 %d\n", device.name.UTF8String,
               (int)device.supportsRaytracing, (int)[device supportsFamily:MTLGPUFamilyApple9], (int)[device supportsFamily:MTLGPUFamilyMetal3]);
        MTLCompileOptions* options = [MTLCompileOptions new];
        options.languageVersion = MTLLanguageVersion3_0;
        options.fastMathEnabled = YES;
        NSError* error = nil;
        CFAbsoluteTime t0 = CFAbsoluteTimeGetCurrent();
        id<MTLLibrary> library = [device newLibraryWithSource:[NSString stringWithUTF8String:kFRGlassRTMSL] options:options error:&error];
        double compileMs = (CFAbsoluteTimeGetCurrent() - t0) * 1000.0;
        if (!library)
        {
            fprintf(stderr, "frglassrt_validate: MSL compile FAILED\n%s\n", error.localizedDescription.UTF8String);
            return 1;
        }
        if (error) printf("frglassrt_validate: compiler warnings:\n%s\n", error.localizedDescription.UTF8String);
        printf("frglassrt_validate: MSL compiled in %.1f ms (%zu chars)\n", compileMs, strlen(kFRGlassRTMSL));
        const char* kernels[] = { "fr_trace", "fr_trace_aa", "fr_resolve", "fr_parity", "fr_gather_vertices", "fr_gather_indices", "fr_layout" };
        id<MTLComputePipelineState> layoutPso = nil;
        for (const char* k : kernels)
        {
            id<MTLFunction> fn = [library newFunctionWithName:[NSString stringWithUTF8String:k]];
            if (!fn) { fprintf(stderr, "frglassrt_validate: kernel %s missing\n", k); return 1; }
            NSError* perr = nil;
            id<MTLComputePipelineState> pso = [device newComputePipelineStateWithFunction:fn error:&perr];
            if (!pso) { fprintf(stderr, "frglassrt_validate: pipeline %s FAILED: %s\n", k, perr.localizedDescription.UTF8String); return 1; }
            printf("frglassrt_validate: pipeline %-20s OK (threadExecutionWidth %lu, maxThreads %lu)\n", k,
                   (unsigned long)pso.threadExecutionWidth, (unsigned long)pso.maxTotalThreadsPerThreadgroup);
            if (std::string(k) == "fr_layout") layoutPso = pso;
        }
        FRLayoutBuffers b;
        if (!FRLayoutCreate(device, b)) { fprintf(stderr, "frglassrt_validate: layout buffers\n"); return 1; }
        id<MTLCommandQueue> queue = [device newCommandQueue];
        id<MTLCommandBuffer> cb = [queue commandBuffer];
        id<MTLComputeCommandEncoder> enc = [cb computeCommandEncoder];
        FRLayoutEncode(enc, layoutPso, b);
        [enc endEncoding];
        [cb commit];
        [cb waitUntilCompleted];   // build tool only: never in the plugin
        std::string mismatch = FRLayoutCheck(b);
        if (!mismatch.empty()) { fprintf(stderr, "frglassrt_validate: %s\n", mismatch.c_str()); return 1; }
        printf("frglassrt_validate: layout self-test OK (FRInstance %zu, FRMaterial %zu, FRLamp %zu, FRMeshGPU %zu, FRFrame %zu, FRVertexAttr %zu)\n",
               sizeof(FRInstance), sizeof(FRMaterial), sizeof(FRLamp), sizeof(FRMeshGPU), sizeof(FRFrame), sizeof(FRVertexAttr));
    }
    return 0;
}
