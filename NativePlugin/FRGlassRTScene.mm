// FRGlassRTScene.mm — see FRGlassRTScene.h. Render thread only, except MeshState() and CompleteCompaction().
#import "FRGlassRTScene.h"
#include <algorithm>
#include <cstring>

namespace
{
uint64_t Align(uint64_t v, uint64_t a) { return (v + a - 1) / a * a; }

id<MTLTexture> MakeDefault(id<MTLDevice> device, uint8_t r, uint8_t g, uint8_t b, uint8_t a, NSString* label)
{
    MTLTextureDescriptor* d = [MTLTextureDescriptor texture2DDescriptorWithPixelFormat:MTLPixelFormatRGBA8Unorm width:1 height:1 mipmapped:NO];
    d.storageMode = MTLStorageModeShared;
    d.usage = MTLTextureUsageShaderRead;
    id<MTLTexture> t = [device newTextureWithDescriptor:d];
    uint8_t px[4] = { r, g, b, a };
    [t replaceRegion:MTLRegionMake2D(0, 0, 1, 1) mipmapLevel:0 withBytes:px bytesPerRow:4];
    t.label = label;
    return t;
}

// inverse of the 3x4 affine [A | t] given as rows
void InvertRows(const float4& r0, const float4& r1, const float4& r2, float4& o0, float4& o1, float4& o2)
{
    double a = r0.x, b = r0.y, c = r0.z, d = r1.x, e = r1.y, f = r1.z, g = r2.x, h = r2.y, i = r2.z;
    double A = e * i - f * h, B = -(d * i - f * g), C = d * h - e * g;
    double det = a * A + b * B + c * C;
    if (std::abs(det) < 1e-20) det = det < 0 ? -1e-20 : 1e-20;
    double inv = 1.0 / det;
    double m00 = A * inv, m01 = -(b * i - c * h) * inv, m02 = (b * f - c * e) * inv;
    double m10 = B * inv, m11 = (a * i - c * g) * inv, m12 = -(a * f - c * d) * inv;
    double m20 = C * inv, m21 = -(a * h - b * g) * inv, m22 = (a * e - b * d) * inv;
    double tx = r0.w, ty = r1.w, tz = r2.w;
    o0 = simd_make_float4((float)m00, (float)m01, (float)m02, (float)-(m00 * tx + m01 * ty + m02 * tz));
    o1 = simd_make_float4((float)m10, (float)m11, (float)m12, (float)-(m10 * tx + m11 * ty + m12 * tz));
    o2 = simd_make_float4((float)m20, (float)m21, (float)m22, (float)-(m20 * tx + m21 * ty + m22 * tz));
}
}

bool FRGlassRTScene::Init(id<MTLDevice> dev, std::string& error)
{
    device = dev;
    meshTable = [device newBufferWithLength:sizeof(FRMeshGPU) * kMaxMeshSlots options:MTLResourceStorageModeShared];
    materialTable = [device newBufferWithLength:sizeof(FRMaterial) * kMaxMaterials options:MTLResourceStorageModeShared];
    textureTable = [device newBufferWithLength:sizeof(FRTexSlot) * kMaxTextures options:MTLResourceStorageModeShared];
    if (!meshTable || !materialTable || !textureTable) { error = "scene tables could not be allocated"; return false; }
    meshTable.label = @"FRGlassRT mesh table";
    materialTable.label = @"FRGlassRT material table";
    textureTable.label = @"FRGlassRT texture table";
    std::memset(meshTable.contents, 0, meshTable.length);
    std::memset(materialTable.contents, 0, materialTable.length);
    FRMaterial* mats = (FRMaterial*)materialTable.contents;
    for (uint32_t i = 0; i < kMaxMaterials; i++) { mats[i].baseColor = simd_make_float4(0.5f, 0.5f, 0.5f, 1.0f); mats[i].p0 = simd_make_float4(0.5f, 0, 1, 1); }
    defaults[0] = MakeDefault(device, 255, 255, 255, 255, @"FRGlassRT white");
    defaults[1] = MakeDefault(device, 0, 0, 0, 0, @"FRGlassRT black");
    defaults[2] = MakeDefault(device, 128, 128, 255, 255, @"FRGlassRT bump");
    defaults[3] = MakeDefault(device, 128, 128, 128, 255, @"FRGlassRT grey");
    textures.assign(kMaxTextures, nil);
    FRTexSlot* slots = (FRTexSlot*)textureTable.contents;
    for (uint32_t i = 0; i < kMaxTextures; i++)
    {
        id<MTLTexture> t = i < 4 ? defaults[i] : defaults[0];
        MTLResourceID rid = t.gpuResourceID;
        std::memcpy(&slots[i].tex, &rid, 8);
    }
    for (int i = 0; i < 4; i++) textures[i] = defaults[i];
    meshes.resize(1024);
    if (@available(macOS 15.0, *))
    {
        MTLResidencySetDescriptor* d = [MTLResidencySetDescriptor new];
        d.label = @"FRGlassRT residency";
        d.initialCapacity = 4096;
        NSError* err = nil;
        residencySet = [device newResidencySetWithDescriptor:d error:&err];
    }
    ResidencyAdd(meshTable); ResidencyAdd(materialTable); ResidencyAdd(textureTable);
    for (int i = 0; i < 4; i++) ResidencyAdd(defaults[i]);
    return true;
}

void FRGlassRTScene::Shutdown()
{
    meshes.clear(); pending.clear(); retired.clear(); retiredEntries.clear(); textures.clear();
    resident.clear(); residentList.clear();
    residencySet = nil; meshTable = nil; materialTable = nil; textureTable = nil; buildScratch = nil;
    for (auto& d : defaults) d = nil;
    std::lock_guard<std::mutex> lock(stateMutex);
    meshState.clear();
}

void FRGlassRTScene::ResidencyAdd(id a)
{
    if (!a) return;
    void* k = (__bridge void*)a;
    auto it = resident.find(k);
    if (it != resident.end()) return;
    resident[k] = a;
    residentListDirty = true;
    if (residencySet) { if (@available(macOS 15.0, *)) { [(id<MTLResidencySet>)residencySet addAllocation:(id<MTLAllocation>)a]; residencyDirty = true; } }
}

void FRGlassRTScene::ResidencyRemove(id a)
{
    if (!a) return;
    void* k = (__bridge void*)a;
    auto it = resident.find(k);
    if (it == resident.end()) return;
    resident.erase(it);
    residentListDirty = true;
    if (residencySet) { if (@available(macOS 15.0, *)) { [(id<MTLResidencySet>)residencySet removeAllocation:(id<MTLAllocation>)a]; residencyDirty = true; } }
}

void FRGlassRTScene::AttachResidency(id<MTLCommandBuffer> cb)
{
    if (!residencySet) return;
    if (@available(macOS 15.0, *))
    {
        id<MTLResidencySet> set = (id<MTLResidencySet>)residencySet;
        if (residencyDirty) { [set commit]; [set requestResidency]; residencyDirty = false; }
        [cb useResidencySet:set];
    }
}

void FRGlassRTScene::UseResources(id<MTLComputeCommandEncoder> enc)
{
    if (residencySet) return;
    if (residentListDirty)
    {
        residentList.clear();
        residentList.reserve(resident.size());
        for (auto& kv : resident) residentList.push_back((id<MTLResource>)kv.second);
        residentListDirty = false;
    }
    if (!residentList.empty()) [enc useResources:residentList.data() count:residentList.size() usage:MTLResourceUsageRead];
}

void FRGlassRTScene::Retire(id object, uint64_t serial, uint32_t meshSlot)
{
    if (!object && meshSlot == UINT32_MAX) return;
    retired.push_back({ serial, object, meshSlot });
}

void FRGlassRTScene::Purge(uint64_t doneSerial)
{
    size_t w = 0;
    for (size_t r = 0; r < retired.size(); r++)
    {
        FRRetired& x = retired[r];
        if (x.serial <= doneSerial)
        {
            if (x.meshSlot != UINT32_MAX) freeSlots.push_back(x.meshSlot);
            if (x.object) ResidencyRemove(x.object);
            if (x.object && [x.object conformsToProtocol:@protocol(MTLAccelerationStructure)])
            {
                uint64_t sz = [(id<MTLAccelerationStructure>)x.object size];
                blasBytes = blasBytes > sz ? blasBytes - sz : 0;
            }
            else if (x.object && [x.object conformsToProtocol:@protocol(MTLBuffer)])
            {
                uint64_t sz = [(id<MTLBuffer>)x.object length];
                geometryBytes = geometryBytes > sz ? geometryBytes - sz : 0;
            }
            continue;
        }
        if (w != r) retired[w] = retired[r];
        w++;
    }
    retired.resize(w);
}

uint32_t FRGlassRTScene::AllocSlot()
{
    if (!freeSlots.empty()) { uint32_t s = freeSlots.back(); freeSlots.pop_back(); return s; }
    if (slotHigh >= kMaxMeshSlots) return UINT32_MAX;
    return slotHigh++;
}

void FRGlassRTScene::WriteSlot(uint32_t slot, const FRMeshEntry& e, bool alt)
{
    if (slot >= kMaxMeshSlots) return;
    FRMeshGPU g{};
    uint64_t base = e.buffer.gpuAddress;
    g.positions = alt && e.altPositions ? e.altPositions.gpuAddress : base;
    g.attrs = base + e.attrOffset;
    g.indices = base + e.indexOffset;
    g.submeshStart = base + e.subOffset;
    g.vertexCount = e.vertexCount;
    g.indexCount = e.indexCount;
    g.submeshCount = (uint32_t)e.subStart.size();
    std::memcpy((FRMeshGPU*)meshTable.contents + slot, &g, sizeof(g));
}

int FRGlassRTScene::MeshState(int32_t key) const
{
    std::lock_guard<std::mutex> lock(stateMutex);
    auto it = meshState.find(key);
    return it == meshState.end() ? 0 : it->second;
}

uint32_t FRGlassRTScene::PendingCount() const { return (uint32_t)pending.size(); }

void FRGlassRTScene::AddMesh(std::unique_ptr<FRMeshUpload> u, uint64_t serial)
{
    if (!u || u->key < 0 || !u->buffer || u->subStart.empty()) return;
    if ((size_t)u->key >= meshes.size()) meshes.resize(std::max<size_t>(meshes.size() * 2, (size_t)u->key + 1));
    if (meshes[u->key]) ReleaseMesh(u->key, serial);
    auto e = std::make_unique<FRMeshEntry>();
    e->key = u->key;
    e->buffer = u->buffer;
    e->vertexCount = u->vertexCount;
    e->indexCount = u->indexCount;
    e->attrOffset = u->attrOffset;
    e->indexOffset = u->indexOffset;
    e->subOffset = u->range ? u->rangeSubOffset : u->subOffset;
    e->subStart = u->subStart;
    e->subCount = u->subCount;
    e->deforming = u->deforming;
    e->notBeforeFrame = u->notBeforeFrame;
    uint32_t tris = 0;
    for (uint32_t c : e->subCount) tris += c / 3;
    e->triangles = tris;
    e->slot = AllocSlot();
    if (e->slot == UINT32_MAX) { std::lock_guard<std::mutex> lock(stateMutex); meshState[u->key] = FRMeshEntry::Failed; return; }
    if (e->deforming)
    {
        e->slotAlt = AllocSlot();
        e->altPositions = [device newBufferWithLength:std::max<uint64_t>(12ull * e->vertexCount, 16) options:MTLResourceStorageModeShared];
        std::memcpy(e->altPositions.contents, e->buffer.contents, 12ull * e->vertexCount);
        ResidencyAdd(e->altPositions);
        WriteSlot(e->slotAlt, *e, true);
    }
    if (u->gather)
    {
        e->gatherPending = true;
        for (int i = 0; i < 4; i++) e->gatherStreams[i] = u->streams[i];
        e->gatherIndexBuffer = u->unityIndexBuffer;
        e->gatherVertexArgs = u->vertexArgs;
        e->gatherIndexArgs = u->indexArgs;
    }
    WriteSlot(e->slot, *e, false);
    if (resident.find((__bridge void*)e->buffer) == resident.end()) geometryBytes += e->buffer.length;
    ResidencyAdd(e->buffer);
    e->state = FRMeshEntry::Pending;
    pending.push_back(e.get());
    {
        std::lock_guard<std::mutex> lock(stateMutex);
        meshState[e->key] = FRMeshEntry::Pending;
    }
    meshes[u->key] = std::move(e);
    live++;
}

void FRGlassRTScene::ReleaseMesh(int32_t key, uint64_t serial)
{
    if (key < 0 || (size_t)key >= meshes.size() || !meshes[key]) return;
    FRMeshEntry* e = meshes[key].get();
    pending.erase(std::remove(pending.begin(), pending.end(), e), pending.end());
    Retire(e->blas, serial);
    Retire(e->blasAlt, serial);
    Retire(e->compactTarget, serial);
    Retire(e->altPositions, serial);
    // a shared range buffer is retired by its last range (the retire list drops the residency entry at purge;
    // other ranges still reference the buffer object, so ARC keeps it alive until they go too)
    bool shared = false;
    for (auto& m : meshes) if (m && m.get() != e && m->buffer == e->buffer) { shared = true; break; }
    if (!shared) Retire(e->buffer, serial);
    Retire(nil, serial, e->slot);
    if (e->deforming) Retire(nil, serial, e->slotAlt);
    {
        std::lock_guard<std::mutex> lock(stateMutex);
        meshState.erase(key);
    }
    meshes[key].reset();
    if (live > 0) live--;
}

void FRGlassRTScene::RefitMesh(int32_t key, id<MTLBuffer> positions, uint64_t serial)
{
    if (key < 0 || (size_t)key >= meshes.size() || !meshes[key] || !positions) return;
    FRMeshEntry* e = meshes[key].get();
    if (!e->deforming) return;
    // the refit is encoded in EncodeAccelerationStructures; park the new positions on the entry
    Retire(e->compactTarget, serial);   // deforming meshes are never compacted; defensive
    e->compactTarget = nil;
    e->refitPositions = positions;
}

void FRGlassRTScene::SetTexture(uint32_t slot, id<MTLTexture> texture, uint64_t serial)
{
    if (slot < 4 || slot >= kMaxTextures) return;
    id<MTLTexture> old = textures[slot];
    if (old == texture) return;
    if (old && old != defaults[0]) Retire(old, serial);
    id<MTLTexture> t = texture ? texture : defaults[0];
    textures[slot] = t;
    MTLResourceID rid = t.gpuResourceID;
    std::memcpy(&((FRTexSlot*)textureTable.contents)[slot].tex, &rid, 8);
    ResidencyAdd(t);
}

void FRGlassRTScene::SetMaterials(uint32_t first, const std::vector<FRMaterial>& materials)
{
    if (first >= kMaxMaterials) return;
    size_t n = std::min<size_t>(materials.size(), kMaxMaterials - first);
    std::memcpy((FRMaterial*)materialTable.contents + first, materials.data(), n * sizeof(FRMaterial));
}

void FRGlassRTScene::Reset(uint64_t serial)
{
    for (size_t k = 0; k < meshes.size(); k++) if (meshes[k]) ReleaseMesh((int32_t)k, serial);
    pending.clear();
}

void FRGlassRTScene::CompleteCompaction(int32_t key, uint64_t size)
{
    std::lock_guard<std::mutex> lock(completionMutex);
    completions.push_back({ key, size });
}

void FRGlassRTScene::DrainCompletions()
{
    std::vector<FRCompletionNote> notes;
    {
        std::lock_guard<std::mutex> lock(completionMutex);
        notes.swap(completions);
    }
    for (auto& n : notes)
    {
        if (n.key < 0 || (size_t)n.key >= meshes.size() || !meshes[n.key]) continue;
        FRMeshEntry* e = meshes[n.key].get();
        if (e->compactSizeIndex < 0 || e->state != FRMeshEntry::Built) continue;
        e->compactSizeIndex = -1;
        e->compactedSize = n.compactedSize;
    }
}

MTLPrimitiveAccelerationStructureDescriptor* FRGlassRTScene::Describe(const FRMeshEntry& e, id<MTLBuffer> positions) const
{
    NSMutableArray* geos = [NSMutableArray arrayWithCapacity:e.subStart.size()];
    for (size_t k = 0; k < e.subStart.size(); k++)
    {
        MTLAccelerationStructureTriangleGeometryDescriptor* g = [MTLAccelerationStructureTriangleGeometryDescriptor descriptor];
        g.vertexBuffer = positions;
        g.vertexBufferOffset = 0;
        g.vertexStride = 12;
        g.vertexFormat = MTLAttributeFormatFloat3;
        g.indexBuffer = e.buffer;
        g.indexBufferOffset = e.indexOffset + 4ull * e.subStart[k];
        g.indexType = MTLIndexTypeUInt32;
        g.triangleCount = e.subCount[k] / 3;
        g.opaque = YES;
        [geos addObject:g];
    }
    MTLPrimitiveAccelerationStructureDescriptor* d = [MTLPrimitiveAccelerationStructureDescriptor descriptor];
    d.geometryDescriptors = geos;
    d.usage = e.deforming ? MTLAccelerationStructureUsageRefit : MTLAccelerationStructureUsageNone;
    return d;
}

void FRGlassRTScene::EncodeGathers(id<MTLCommandBuffer> cb, id<MTLComputePipelineState> vtx, id<MTLComputePipelineState> idx, uint64_t serial,
                                   std::vector<id>& keepAlive)
{
    (void)serial;
    std::vector<FRMeshEntry*> todo;
    for (FRMeshEntry* e : pending) if (e->gatherPending) todo.push_back(e);
    if (todo.empty() || !vtx || !idx) return;
    id<MTLComputeCommandEncoder> enc = [cb computeCommandEncoder];
    enc.label = @"FRGlassRT gather";
    for (FRMeshEntry* e : todo)
    {
        id<MTLBuffer> any = e->gatherStreams[0];
        [enc setComputePipelineState:vtx];
        [enc setBytes:&e->gatherVertexArgs length:sizeof(FRGatherVertexArgsCPU) atIndex:0];
        for (int s = 0; s < 4; s++) [enc setBuffer:(e->gatherStreams[s] ? e->gatherStreams[s] : any) offset:0 atIndex:1 + s];
        [enc setBuffer:e->buffer offset:0 atIndex:5];
        [enc setBuffer:e->buffer offset:e->attrOffset atIndex:6];
        [enc dispatchThreads:MTLSizeMake(e->vertexCount, 1, 1) threadsPerThreadgroup:MTLSizeMake(64, 1, 1)];
        [enc setComputePipelineState:idx];
        for (auto& a : e->gatherIndexArgs)
        {
            if (a.count == 0) continue;
            [enc setBytes:&a length:sizeof(a) atIndex:0];
            [enc setBuffer:e->gatherIndexBuffer offset:0 atIndex:1];
            [enc setBuffer:e->buffer offset:e->indexOffset atIndex:2];
            [enc dispatchThreads:MTLSizeMake(a.count, 1, 1) threadsPerThreadgroup:MTLSizeMake(64, 1, 1)];
        }
        for (int s = 0; s < 4; s++) if (e->gatherStreams[s]) keepAlive.push_back(e->gatherStreams[s]);
        if (e->gatherIndexBuffer) keepAlive.push_back(e->gatherIndexBuffer);
        for (int s = 0; s < 4; s++) e->gatherStreams[s] = nil;
        e->gatherIndexBuffer = nil;
        e->gatherPending = false;
    }
    [enc endEncoding];
}

int FRGlassRTScene::EncodeAccelerationStructures(id<MTLCommandBuffer> cb, FRCameraState& cam, const std::vector<FRInstanceIn>& instances,
                                                 const std::vector<FRLamp>& lamps, int32_t frame, uint64_t serial, uint64_t doneSerial,
                                                 id<MTLFence> fence, id<MTLCounterSampleBuffer> counters, uint32_t sampleBase,
                                                 id<MTLBuffer> compactSizes, std::vector<std::pair<int32_t, uint32_t>>& compactQueries,
                                                 uint32_t& instanceCountOut, bool& ringBusy)
{
    instanceCountOut = 0;
    ringBusy = false;
    lastBuilds = 0;
    lastTriangles = 0;

    // finished compactions: swap in the compact BLAS (the old one stays alive until this event completes)
    for (auto& up : meshes)
    {
        FRMeshEntry* e = up.get();
        if (!e || !e->compactTarget || e->compactSerial == 0 || e->compactSerial > doneSerial) continue;
        Retire(e->blas, serial);
        e->blas = e->compactTarget;
        e->compactTarget = nil;
        e->compactSerial = 0;
        e->state = FRMeshEntry::Compacted;
        compacted++;
        std::lock_guard<std::mutex> lock(stateMutex);
        meshState[e->key] = FRMeshEntry::Compacted;
    }

    // ---- choose this event's BLAS builds (budget ~50k triangles, at least one)
    std::vector<FRMeshEntry*> builds;
    uint32_t budget = kTrianglesPerFrame;
    for (FRMeshEntry* e : pending)
    {
        if (e->gatherPending) continue;
        if (e->notBeforeFrame > frame) continue;
        if (!builds.empty() && e->triangles > budget) break;
        builds.push_back(e);
        budget = e->triangles >= budget ? 0 : budget - e->triangles;
        if (budget == 0 || builds.size() >= 64) break;   // ≤ 50k triangles and ≤ 64 builds per frame (~≤ 1 ms GPU)
    }
    std::vector<MTLPrimitiveAccelerationStructureDescriptor*> descs;
    std::vector<MTLAccelerationStructureSizes> sizes;
    uint64_t scratchNeed = 0;
    for (FRMeshEntry* e : builds)
    {
        MTLPrimitiveAccelerationStructureDescriptor* d = Describe(*e, e->buffer);
        MTLAccelerationStructureSizes s = [device accelerationStructureSizesWithDescriptor:d];
        descs.push_back(d);
        sizes.push_back(s);
        scratchNeed += Align(s.buildScratchBufferSize, 256) * (e->deforming ? 2 : 1);
    }
    // refits parked by RefitMesh
    std::vector<FRMeshEntry*> refits;
    for (auto& up : meshes)
    {
        FRMeshEntry* e = up.get();
        if (!e || !e->deforming || !e->blas || !e->buffer) continue;
        if (e->refitPositions) refits.push_back(e);
    }
    std::vector<MTLPrimitiveAccelerationStructureDescriptor*> refitDescs;
    for (FRMeshEntry* e : refits)
    {
        MTLPrimitiveAccelerationStructureDescriptor* d = Describe(*e, e->refitPositions);
        MTLAccelerationStructureSizes s = [device accelerationStructureSizesWithDescriptor:d];
        refitDescs.push_back(d);
        scratchNeed += Align(std::max<uint64_t>(s.refitScratchBufferSize, 16), 256);
    }
    if (scratchNeed > 0 && (!buildScratch || buildScratch.length < scratchNeed))
    {
        if (buildScratch) Retire(buildScratch, serial);
        buildScratch = [device newBufferWithLength:Align(scratchNeed * 3 / 2, 65536) options:MTLResourceStorageModePrivate];
        buildScratch.label = @"FRGlassRT BLAS scratch";
    }

    // ---- this camera's TLAS ring slot
    FRCameraState::Slot& slot = cam.slots[cam.next];
    bool slotFree = slot.serial <= doneSerial;
    if (!slotFree) ringBusy = true;

    MTLAccelerationStructurePassDescriptor* pass = [MTLAccelerationStructurePassDescriptor accelerationStructurePassDescriptor];
    if (counters)
    {
        pass.sampleBufferAttachments[0].sampleBuffer = counters;
        pass.sampleBufferAttachments[0].startOfEncoderSampleIndex = sampleBase;
        pass.sampleBufferAttachments[0].endOfEncoderSampleIndex = sampleBase + 1;
    }
    id<MTLAccelerationStructureCommandEncoder> enc = [cb accelerationStructureCommandEncoderWithDescriptor:pass];
    enc.label = @"FRGlassRT acceleration structures";
    if (!residencySet && !residentList.empty()) [enc useResources:residentList.data() count:residentList.size() usage:MTLResourceUsageRead];

    uint64_t scratchOffset = 0;
    for (size_t i = 0; i < builds.size(); i++)
    {
        FRMeshEntry* e = builds[i];
        id<MTLAccelerationStructure> blas = [device newAccelerationStructureWithSize:sizes[i].accelerationStructureSize];
        if (!blas)
        {
            e->state = FRMeshEntry::Failed;
            std::lock_guard<std::mutex> lock(stateMutex);
            meshState[e->key] = FRMeshEntry::Failed;
            continue;
        }
        blas.label = @"FRGlassRT BLAS";
        [enc buildAccelerationStructure:blas descriptor:descs[i] scratchBuffer:buildScratch scratchBufferOffset:scratchOffset];
        scratchOffset += Align(sizes[i].buildScratchBufferSize, 256);
        e->blas = blas;
        blasBytes += blas.size;
        ResidencyAdd(blas);
        if (e->deforming)
        {
            id<MTLAccelerationStructure> alt = [device newAccelerationStructureWithSize:sizes[i].accelerationStructureSize];
            [enc buildAccelerationStructure:alt descriptor:descs[i] scratchBuffer:buildScratch scratchBufferOffset:scratchOffset];
            scratchOffset += Align(sizes[i].buildScratchBufferSize, 256);
            e->blasAlt = alt;
            blasBytes += alt.size;
            ResidencyAdd(alt);
        }
        else if (compactSizes && compactQueries.size() < compactSizes.length / 4)
        {
            uint32_t q = (uint32_t)compactQueries.size();
            [enc writeCompactedAccelerationStructureSize:blas toBuffer:compactSizes offset:q * 4];
            compactQueries.push_back({ e->key, q });
            e->compactSizeIndex = q;
        }
        e->state = FRMeshEntry::Built;
        builtTotal++;
        lastBuilds++;
        lastTriangles += e->triangles;
        {
            std::lock_guard<std::mutex> lock(stateMutex);
            meshState[e->key] = FRMeshEntry::Built;
        }
    }
    if (!builds.empty())
    {
        std::vector<FRMeshEntry*> keep;
        for (FRMeshEntry* e : pending) if (std::find(builds.begin(), builds.end(), e) == builds.end()) keep.push_back(e);
        pending.swap(keep);
    }

    // refits: ping-pong BLAS + positions so an in-flight trace keeps reading consistent data
    for (size_t i = 0; i < refits.size(); i++)
    {
        FRMeshEntry* e = refits[i];
        id<MTLBuffer> np = e->refitPositions;
        e->refitPositions = nil;
        id<MTLAccelerationStructure> src = e->usingAlt ? e->blasAlt : e->blas;
        id<MTLAccelerationStructure> dst = e->usingAlt ? e->blas : e->blasAlt;
        if (!src || !dst) continue;
        // copy the new positions into the target half of the ping-pong pair
        id<MTLBuffer> target = e->usingAlt ? e->buffer : e->altPositions;
        std::memcpy(target.contents, np.contents, std::min<uint64_t>(np.length, 12ull * e->vertexCount));
        MTLPrimitiveAccelerationStructureDescriptor* d = Describe(*e, target);
        [enc refitAccelerationStructure:src descriptor:d destination:dst scratchBuffer:buildScratch scratchBufferOffset:scratchOffset];
        scratchOffset += Align(std::max<uint64_t>([device accelerationStructureSizesWithDescriptor:d].refitScratchBufferSize, 16), 256);
        e->usingAlt = !e->usingAlt;
        WriteSlot(e->usingAlt ? e->slotAlt : e->slot, *e, e->usingAlt);
    }

    // compactions whose size is known
    uint32_t compactsThisEvent = 0;
    for (auto& up : meshes)
    {
        FRMeshEntry* e = up.get();
        if (!e || e->state != FRMeshEntry::Built || e->compactedSize == 0 || e->compactTarget) continue;
        if (e->compactedSize >= e->blas.size) { e->compactedSize = 0; e->state = FRMeshEntry::Compacted; continue; }
        id<MTLAccelerationStructure> c = [device newAccelerationStructureWithSize:e->compactedSize];
        if (!c) { e->compactedSize = 0; continue; }
        c.label = @"FRGlassRT BLAS (compacted)";
        [enc copyAndCompactAccelerationStructure:e->blas toAccelerationStructure:c];
        e->compactTarget = c;
        e->compactSerial = serial;
        e->compactedSize = 0;
        blasBytes += c.size;
        ResidencyAdd(c);
        if (++compactsThisEvent >= 64) break;
    }

    // ---- TLAS (not for a warm-up event: no instances)
    int usedSlot = -1;
    if (slotFree && !instances.empty())
    {
        if (!slot.tlas)
        {
            MTLInstanceAccelerationStructureDescriptor* d = [MTLInstanceAccelerationStructureDescriptor descriptor];
            d.instanceDescriptorType = MTLAccelerationStructureInstanceDescriptorTypeIndirect;
            d.instanceCount = kMaxInstances;
            d.instanceDescriptorStride = sizeof(MTLIndirectAccelerationStructureInstanceDescriptor);
            MTLAccelerationStructureSizes s = [device accelerationStructureSizesWithDescriptor:d];
            slot.tlas = [device newAccelerationStructureWithSize:s.accelerationStructureSize];
            slot.tlas.label = @"FRGlassRT TLAS";
            slot.descriptors = [device newBufferWithLength:sizeof(MTLIndirectAccelerationStructureInstanceDescriptor) * kMaxInstances options:MTLResourceStorageModeShared];
            slot.records = [device newBufferWithLength:sizeof(FRInstance) * kMaxInstances options:MTLResourceStorageModeShared];
            slot.lamps = [device newBufferWithLength:sizeof(FRLamp) * kMaxLamps options:MTLResourceStorageModeShared];
            slot.capacity = kMaxInstances;
            if (!cam.scratch || cam.scratch.length < s.buildScratchBufferSize)
                cam.scratch = [device newBufferWithLength:std::max<uint64_t>(s.buildScratchBufferSize, 256) options:MTLResourceStorageModePrivate];
        }
        auto* descs2 = (MTLIndirectAccelerationStructureInstanceDescriptor*)slot.descriptors.contents;
        auto* recs = (FRInstance*)slot.records.contents;
        uint32_t n = 0;
        for (const FRInstanceIn& in : instances)
        {
            if (n >= slot.capacity) break;
            if (in.meshKey < 0 || (size_t)in.meshKey >= meshes.size()) continue;
            FRMeshEntry* e = meshes[in.meshKey].get();
            if (!e || !e->blas || (e->state != FRMeshEntry::Built && e->state != FRMeshEntry::Compacted)) continue;
            id<MTLAccelerationStructure> as = e->deforming && e->usingAlt ? e->blasAlt : e->blas;
            MTLIndirectAccelerationStructureInstanceDescriptor& d = descs2[n];
            std::memset(&d, 0, sizeof(d));
            for (int c = 0; c < 4; c++)
                d.transformationMatrix.columns[c] = MTLPackedFloat3Make(in.o2w0[c], in.o2w1[c], in.o2w2[c]);
            d.options = MTLAccelerationStructureInstanceOptionOpaque;
            d.mask = in.mask & 0xffu;
            d.intersectionFunctionTableOffset = 0;
            d.userID = n;
            d.accelerationStructureID = as.gpuResourceID;
            FRInstance& r = recs[n];
            r.o2w0 = in.o2w0; r.o2w1 = in.o2w1; r.o2w2 = in.o2w2;
            InvertRows(in.o2w0, in.o2w1, in.o2w2, r.w2o0, r.w2o1, r.w2o2);
            r.meshSlot = e->deforming && e->usingAlt ? e->slotAlt : e->slot;
            r.materialBase = in.materialBase;
            r.flags = in.flags;
            r.windowId = in.windowId;
            r.emission = in.emission;
            n++;
        }
        if (n > 0)
        {
            MTLInstanceAccelerationStructureDescriptor* d = [MTLInstanceAccelerationStructureDescriptor descriptor];
            d.instanceDescriptorType = MTLAccelerationStructureInstanceDescriptorTypeIndirect;
            d.instanceDescriptorBuffer = slot.descriptors;
            d.instanceDescriptorStride = sizeof(MTLIndirectAccelerationStructureInstanceDescriptor);
            d.instanceCount = n;
            [enc buildAccelerationStructure:slot.tlas descriptor:d scratchBuffer:cam.scratch scratchBufferOffset:0];
            size_t ln = std::min<size_t>(lamps.size(), kMaxLamps);
            if (ln) std::memcpy(slot.lamps.contents, lamps.data(), ln * sizeof(FRLamp));
            slot.serial = serial;
            slot.count = n;
            slot.valid = true;
            usedSlot = (int)cam.next;
            cam.lastSlot = usedSlot;
            cam.next = (cam.next + 1) % 3;
        }
        instanceCountOut = n;
    }
    else if (cam.lastSlot >= 0 && cam.slots[cam.lastSlot].valid)
    {
        usedSlot = cam.lastSlot;
        instanceCountOut = cam.slots[usedSlot].count;
        // the reused slot is read again by this event: keep it out of reuse until this event is done
        cam.slots[usedSlot].serial = serial;
    }
    [enc updateFence:fence];
    [enc endEncoding];
    return usedSlot;
}
