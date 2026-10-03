# 03 — Research: what production ray-traced glass reflections should look like in FrontRooms

Date: 2026-10-03. Status: **COMPLETE, revision 2** (re-run of the research step of the RT glass track, G14).
Read-only for the real project: the only files written under `Frontrooms3D/` are this report and `03_bench/` next
to it. Unity was not opened on the real project, and this step started no Unity process on `proj_rt` (the probe
stage was using it).

**What revision 2 adds to revision 1 (committed in `a3615e3`):**
- Rows that were from memory are now sourced: Battlefield V presets, Spider-Man, RE Engine, Cyberpunk 2077.
- New Apple material: WWDC24 residency sets, the hardware-RT test confirmed by Apple staff, the sample pages.
- A second Metal benchmark on Red's M3 Max with the production hit shading: URP-style spot falloff, shadow rays,
  texture fetch, 2 and 4 rays per pixel.
- Section 1 now agrees with the code review (`01_code_review.md`). The design now matches the glass hook **as the
  glass track actually built it**, and the fracture contract in `../destruction/10_glass_destruction_plan.md` §4.7.
- New sections: anti-aliasing of reflections without TAA (§6.2), real roller-wave numbers (§5.3), the lens
  emission colour-space trap (§6.3).
- Fix: the Windows path through Unity's UnifiedRayTracing uses ray-generation shaders, not inline ray queries (§4).

Task: Red asked the visual chat to understand ChatGPT's independent hardware ray-traced glass prototype (commit
`edfbc92`, controller edit in `c69c7d7`) and take it on. This report answers one question: what should the
production version of real-time ray-traced reflections for GLASS look like in this game, at the highest desktop
spec? For each part it gives what Apple, shipped games and Unity say.

Conventions. **MEASURED** = timed on Red's Mac for this report (§2.3, §2.4). **ESTIMATE** = arithmetic or judgement.
**UNVERIFIED** = not confirmed from a source read for this report. Sources are numbered [A..] Apple, [G..] games and
graphics, [U..] Unity, [P..] physics and glass, [D..] project documents, and listed in full in §9. `file:line` paths
are relative to `Frontrooms3D/`.

---

## 0. Short answer (for Red)

1. **Ray tracing on your Mac is real, but only through a native plugin.** Unity 6.3 still reports no ray tracing on
   Metal (G11 and the probe stage both measured `SystemInfo.supportsRayTracing` = False). Metal itself gives a
   plugin hardware ray tracing on M3-class GPUs: Apple calls the M3 "hardware-accelerated ray tracing ... on the
   Mac for the first time" [A8]. So the G11 line "no ray tracing on the M3 Max" is superseded **for desktop Mac
   only**. The WebGL and HDRP conclusions do not change.
2. **It is cheap on the M3 Max.** MEASURED in two standalone Metal benchmarks of an office-like scene (2,912
   instances, 15 M instanced triangles). The case is a held pane that covers a quarter of the screen. Each glass
   pixel traces one reflection ray. Each hit gets 32 lamps with URP-style spot falloff, shadow rays for the 4
   nearest lamps, and a texture-array albedo fetch. That costs **0.7-0.9 ms at 1080p and 0.9-1.4 ms at 1440p**.
   Rebuilding the top-level structure (TLAS) every frame costs **0.2-0.3 ms for up to 1,792 instances and 0.5 ms
   for 4,096**. Scene size barely matters (§2.3, §2.4).
3. **What the production version looks like** (§6):
   - After the opaque pass, rasterize the glass panes into a small "glass G-buffer" (position, normal).
   - On Unity's own Metal command buffer, trace one mirror ray per glass pixel from it.
   - Shade the hit the way the game shades that surface: same world-metre textures (and the moving wallpaper
     print when it lands), same lamps, emissive troffer lenses flickering in sync, same fog.
   - Write `_FR_GlassRTReflection` with the depth tag the glass hook expects. FrontRooms/Glass then uses it
     **before** tonemapping and bloom.
   - No denoiser and no TAA are needed: clean glass is a mirror. SEED says of clear glass "No filtering required"
     [G2]. Capcom says mirrors support "only perfect reflection ... and no denoiser is used" [G16].
4. **Without TAA, a one-ray mirror shows stair-stepped, crawling edges on the brightest things it reflects** (the
   troffer lenses). Four rays per pixel everywhere costs 4-5 ms at 1440p (MEASURED), too much as a default. Use
   ray-cone texture filtering [G19], plus either a cheap edge filter on the reflection buffer (High tier) or extra
   rays only on high-contrast pixels (Cinematic tier) (§6.2).
5. **The prototype proves the plumbing works. It is not a base to ship.**
   - As committed it has never drawn a reflection: five stacked blockers, see `01_code_review.md`.
   - Of the 10 suspected defects, 8 are confirmed. #2 (vertical flip) is not a defect in today's URP setup, but it
     is fragile. #6 is only partly right: ARC keeps Unity's buffers alive.
   - The research adds five more:
     - no residency calls for the acceleration structures and buffers it reads indirectly (Apple: this "can cause
       command buffer failures, GPU restarts, or even image corruption" [A5]);
     - its own command buffers run outside Unity's frame order;
     - render-thread races;
     - moving things update once a second;
     - `supportsRaytracing` is also true on M1/M2, which have no ray-tracing hardware. Apple staff: test
       `MTLGPUFamilyApple9` instead [A15].
6. **What makes interior window glass read as real** (§5):
   - A clean pane reflects about 8 % face-on (two surfaces, ~4 % each) and 16 % at 60°.
   - Against a **lit** room behind the glass, only the troffer lenses show clearly, about twice as bright as the
     room seen through it.
   - Against a **dark** room, the whole lit room appears, like a one-way mirror.
   - The G10 captures agree: a perfect reflection of our evenly lit rooms adds only +2-4/255 face-on [D2]. RT pays
     off at oblique angles, on lamp lenses, on things that move, and in dark-beyond windows.
   - Roller waves in tempered glass bend reflections by tenths of a degree, not degrees [P6][P7]. The glass
     shader's `_RollStrength` 0.02 is 5-15x too strong once the reflection has true parallax.
7. **Windows can feed the same texture.** URP 17.3 can use Unity's `RayTracingAccelerationStructure` on DX12 without
   HDRP. Unity's UnifiedRayTracing library in Core 17.3 (public API) already handles instance tables and vertex
   fetch. Its hardware backend runs DXR ray-generation shaders and needs `SystemInfo.supportsRayTracing` (§4). Testing
   needs a DX12 PC with an RTX 20 / RX 6000-class GPU.
8. **What I need from the other chats** (§6.6):
   - **Map chat:** four small events or queries (chunk built, chunk releasing, room dressed, live lamp list with the
     **linear** lens emission).
   - **Glass track:** three adjustments to its hook:
     - pane-strength Fresnel for the RT term;
     - keep RT under light smudges once the RT pass blurs them;
     - an optional normal prepass.
   - **Destruction track:** the fracture contract already in its plan.

---

## 1. The prototype, checked against the research

What it does (read in full: `NativePlugin/FrontRoomsMetalGlassRT.mm` 763 lines, `Assets/Scripts/Rendering/
FrontRoomsMetalGlassRT.cs` 394 lines, the renderer feature, the composite shader):
- A native plugin gets Unity's `MTLDevice` and queue through `IUnityGraphicsMetalV2`.
- It builds one bottom-level acceleration structure (BLAS) per mesh, straight from Unity's vertex and index
  `MTLBuffer`s, and a TLAS of up to 256 instances.
- It compiles an MSL kernel at runtime.
- Each frame it re-traces a camera ray per pixel and keeps the pixels whose first hit is flagged glass. It reflects
  once and shades the hit with the flat material colour times a fixed "sun", with a blue sky gradient on a miss
  (`.mm:230-245, 296`).
- URP then blends the result over the finished frame (`FrontRoomsMetalGlassRTRendererFeature.cs:17`).

The full code review, with a harness that runs Red's committed dylib, is `01_code_review.md` [D1]. Its verdict:
**as committed, the prototype has never drawn a reflection.** Five blockers are stacked: the pass waits for a texture
only it creates; then a nil pipeline crashes; then 13 MSL compile errors; then an 88- vs 96-byte struct mismatch;
then shared meshes get the wrong geometry. In the main game the controller never even starts (`standalone = false`).
The table below is the research view of the ten suspected defects, kept consistent with [D1].

| # | Suspected defect | Research / code reading | Status |
|---|---|---|---|
| 1 | FOV in radians where `tan(fov/2)` is expected | `FrontRoomsMetalGlassRT.cs:240` passes `camera.fieldOfView * Deg2Rad`; the kernel uses it as tanFovY (`.mm:264-265`). The controller runs under the MapWalker camera (72°, `FrontRoomsMapWalker.cs:68`): 1.257 is written where 0.727 belongs, so the traced pane comes out **0.578x** its real size. The game camera (76°, `FrontRooms3DGame.cs:247`) would give 0.589x. The harness confirms 0.578 and an exact match after the fix [D1 R6, `images/01_review_trace_output.jpg`]. | Confirmed |
| 2 | Possible vertical mirror | [D1]: in today's setup the two flips cancel (URP renders post into an intermediate texture, and Unity flips projection into textures on Metal). It would mirror if the target ever became the backbuffer. Production rays come from the raster's world position, which removes the question (§6.1). | Not a defect today; fragile |
| 3 | Composite after post | `AfterRenderingPostProcessing` blends linear HDR over the graded, tonemapped image, and [D1 R7] found the blend opaque (alpha 1): the view through the window is lost. BFV, Control, Lumen and HDRP all put the traced reflection into the lighting, before post ([G1] "Light Combine" → "Lit Raster result"; [G12]). | Confirmed, worse than suspected |
| 4 | Primary visibility re-traced against a partial scene | The kernel decides "is this pixel glass" with its own camera ray against at most 256 instances within 18 m (`.cs:33-34`), and does no depth test. Probe: 741 of 742 renderers in the game camera's view were not in the RT scene [D1 §3]. Every shipped hybrid renderer uses the **raster** result for primary visibility ("We still rasterize our G-Buffer -- it plays the role of our primary rays" [A3]; SEED "Rasterize primary visibility" [G2]). | Confirmed |
| 5 | Full rebuild every 1 s on the main thread with `waitUntilCompleted` | `.mm:401-408, 483-490`, called from C# on the main thread (`.cs:120`). Probe: a 64-70 ms hitch per rescan in the map test scene [D1]; the probe stage's editor free run logged 95-1,071 ms per rescan (quoted in [D3] §4.7). Unity: `GetNativeVertexBufferPtr`/`GetNativeIndexBufferPtr` "will synchronize with the rendering thread (a slow operation)" [U1]. Apple: builds "run entirely on the GPU timeline with no CPU synchronization" [A1]. | Confirmed |
| 6 | BLAS hold Unity's buffers that Unity may free | ARC keeps strong references (`.mm:699, 702`), so a freed `Mesh` leaks rather than faults [D1]. The real risks are different. A mesh whose CPU data changes gets **new** GPU buffers [U1 `Mesh.GetVertexBuffer`]. Up to 1 s of stale geometry. The plugin's own `Reset` frees BLASes while a trace may still be in flight. Whether Unity 6.3 sub-allocates mesh buffers on Metal (non-zero offset) is UNVERIFIED. | Partly right |
| 7 | Hit shading ignores textures, lamps, emission, fog; blue sky on miss | `.mm:223-243` reads only `_BaseColor`, never `_BaseMap` [D1 R18]. BFV's rule: "Shader output must match!" between raster and ray hits [G1]. | Confirmed |
| 8 | 30 mm cube pane: two faces, no thin-glass model | The first hit wins; the 30 mm edge faces are flagged glass too [D1 R20]. Real thin glass has a front and a back reflection of almost equal strength (§5.1); nothing models that. | Confirmed (quality) |
| 9 | Feature in the one shared URP renderer | `FrontRooms_URP_Renderer.asset:95`. It is inert off macOS (`AddRenderPasses` returns when not ready), but not provably so: the `DllImport`s are unguarded, `OnDisable` makes a native call, and the dylib's `.meta` holds only a `guid` (no importer settings) [D1 R22]. | Inert, not provably (§6.7) |
| 10 | GPU cost unmeasured | Measured here for the production shape of the work (§2.3, §2.4); [D1] measured the prototype's own kernel at 1.0-4.2 ms (1080p, contended). | Done |
| 11 | **New:** no residency calls | The TLAS references BLASes, and the kernel reads vertex and index buffers through raw GPU addresses, but there is no `useResource`/`useHeap`/residency set. Apple: "it is very important that apps flag residency to Metal for all indirectly accessed resources ... This can cause command buffer failures, GPU restarts, or even image corruption" [A5]. WWDC23 binds the TLAS, then calls `useHeap` to "Make the acceleration structures referenced in your instance acceleration structure available on the GPU" [A6]. | Confirmed by code |
| 12 | **New:** own command buffers on Unity's queue | `RenderEvent` (`.mm:583-620`) creates and commits its own command buffer instead of encoding into Unity's current one. Unity's header says to end Unity's encoder and work in the current command buffer [U3]. So ordering against the frame's depth and transparents is not guaranteed (exact behaviour UNVERIFIED). | Confirmed by code |
| 13 | **New:** render-thread races | `FRGlassRT_Reset` (main thread, under a mutex) sets `s_Tlas = nil` (`.mm:668`) while `RenderEvent` (render thread) reads it unlocked. `FRGlassRT_SetOutput` writes `s_OutputTexture` unlocked (`.mm:745`). One shared event-data block is rewritten every frame while the render thread may still read it. | Confirmed by code |
| 14 | **New:** moving things update once a second | Instance transforms are sent only in `RebuildScene` (every `rescanSeconds = 1`, `.cs:35`). A walking Relay or a swinging door jumps in the glass once a second. | Confirmed by code |
| 15 | **New:** wrong hardware test | `supportsRaytracing` (`.mm:331, 652`) is true on every Apple6+ GPU ("Ray tracing in compute pipelines ... Apple6" [A12]), so M1 and M2, which trace in software, pass it too. Apple staff, asked exactly this: "check for support for `MTLGPUFamilyApple9` ... using `supportsFamily:`" [A15]. | Confirmed by docs |

Also: the output is `ARGBFloat` (128 bits per pixel, `.cs:253`). Half float (`RGBA16F`) is enough for HDR radiance
and halves the bandwidth.

Worth keeping from the prototype:
- the `IUnityGraphicsMetalV2` route;
- one BLAS per `Mesh`;
- the per-instance info table with GPU addresses;
- a glass flag per material.

---

## 2. Metal ray tracing on Apple silicon (item 1)

### 2.1 Hardware and families

- **M3 family.** "hardware-accelerated ray tracing comes to the Mac for the first time"; "Rendering speeds are now up
  to 2.5x faster than on the M1 family"; the M3 Max has a 40-core GPU [A8]. Red's machine: Apple M3 Max, 40 GPU
  cores, Metal 4, macOS 26.6.2.
- **What the hardware does** (Apple tech talk, Jedd Haberstro [A7]).
  - "the hardware intersector is able to run each traversal completely independently using fixed function
    hardware".
  - Intersection functions are still shader code, grouped by a "reorder stage", so that divergence is "reduced or
    even completely eliminated".
  - Apple publishes **no rays-per-second figure** (none in [A7] or [A8]). §2.3 measures one.
- **Apple's M3 advice** [A7]:
  - "use the intersector object API whenever possible". The query API "increases the amount of ray trace scratch
    memory that must be read and written, as well as disables the reorder stage".
  - One intersection function per logical routine, not an uber-function.
  - Keep the ray payload small: it "will decrease your shader's latency and potentially increase its thread
    occupancy".
  - Mark geometry **opaque** so the built-in triangle test is used.
- **Hardware RT test.** `supportsRaytracing` is true on most Apple GPUs. Apple staff answered "check for support for
  `MTLGPUFamilyApple9` family of devices using `supportsFamily:`" [A15]. Apple9 = "Apple A17, M3, and M4" [A11].
- **Family table** [A12]:
  - ray tracing in compute and render pipelines from **Apple6**; residency sets from Apple6;
  - "Acceleration structure build options", "Intersection function buffers" and "MetalFX denoised upscaling" from
    **Apple9**;
  - an intersector can traverse 32 levels of instancing, an intersection query 16.
- **WWDC24** (Jacek Ratajewski and Alè, "Port advanced games to Apple platforms" [A14]):
  - "Residency sets vastly simplify adopting Metal ray tracing, as you can now mark all your ray-traced scene
    resources resident as part of a set".
  - Transforms can be given "in row-major order".
  - "direct access to on-chip intersection result storage improves performance by avoiding data copies and potential
    GPU memory spilling".

### 2.2 Acceleration structures: what Apple recommends

| Topic | Apple guidance | For FrontRooms |
|---|---|---|
| Levels | Primitive AS = triangles; instance AS = transformed copies of primitive AS, "to reduce memory usage" [A1]. Multi-level instancing exists [A6]. | Two levels are enough: BLAS per mesh, one TLAS. |
| BLAS per mesh vs merged | A geometry descriptor can hold several geometries, each with its own buffers [A1]. WWDC22 suggests merging overlapping instances into one primitive AS "to reduce the instance count" [A4]. | One BLAS per **Unity `Mesh`**. The map already merges chunk shells per 6 m block and material (`FrontRoomsMapWorld.cs` `BuildInto`/`AddRenderer`). Panes, lenses, door leaves and keys share Unity's cube; props share kit meshes. |
| Refit vs rebuild | "Refitting an existing acceleration structure is much faster than a full rebuild, so we recommend using it" for "deforming or animated models such as skinned characters" [A4]; refit when geometry "only changes slightly" [A6]. Refit needs the `refit` usage flag [A11]. | Refit only **deforming** meshes. The Relay rig is 16 rigid MeshRenderers (probe stage, quoted in [D3] §4.7), so it needs no refit. Rigid moving things (doors, Relay parts, falling glass pieces) need **no** BLAS work: only their instance transform changes. |
| TLAS per frame | "You should also do a full rebuild of the instance acceleration structure ... Doing a full rebuild is fine in this case since there's only one instance acceleration structure and it usually only contains at most a few thousand objects" [A4]. | Rebuild the TLAS every frame from live transforms (0.2-0.5 ms MEASURED, §2.3). This also absorbs the 192 m world rebase (`ModuleUnits.WorldPeriod`) for free. |
| Batching | "up to 2.8 times faster builds when they run in parallel" (several builds on one encoder) [A4]; use a small pool of scratch buffers [A4]; reuse scratch between batches [A6]. | Encode all BLAS builds of a chunk on one encoder, inside Unity's frame (§6.4). |
| Build flags | `refit`, `preferFastBuild`, `preferFastIntersection`, `minimizeMemory`, `extendedLimits` [A11]; per-build options are an Apple9 feature [A9][A12]. | Default (fast trace) for static shells and props; `refit` only on deforming meshes. |
| Compaction | Metal over-allocates; after the build call `writeCompactedSize`, then `copyAndCompact`; "especially valuable for primitive acceleration structures" [A6]. | MEASURED: a compacted BLAS is **46 %** of its built size (§2.3). Compact static meshes a few frames after their build (async size readback). |
| Heaps | AS can live in `MTLHeap`s: "you can simply make a single call to the useHeap: method to reference all of the primitive acceleration structures. We saw a small performance improvement ... by replacing the calls to useResource: with a single call to useHeap:" [A4]. Heap resources are untracked: synchronize builds yourself [A5]. | Allocate BLASes and our own geometry copies from one heap, or keep them in one residency set (§2.6). |
| Vertex formats / primitive data | Half and normalized vertex formats are accepted directly. Per-primitive data: "a 10% to 16% performance improvement" in one Apple test [A4]. | Positions stay float3. A per-primitive material/print index is an option later. |

### 2.3 MEASURED (bench 1): scene size, builds and the plain trace

What it is:
- Not Unity, not the game: a 308-line Objective-C++ program (`03_bench/rt_bench.mm.txt`). It times GPU work with
  command-buffer GPU timestamps (median of 7-22 runs).
- The scene: a 96 m square of 6 m cells. Each cell has floor and ceiling slabs, 0-2 walls, two 0.6 x 1.2 m lens
  boxes, and 6 props drawn from 16 meshes of 2,048-20,480 triangles. In total **2,912 instances, 15.05 M instanced
  triangles, 17 unique meshes (160 k triangles)**.
- A virtual pane 1.5 m in front of the camera covers 25 % or 100 % of the screen. The camera is at 1.6 m eye height
  with a 76° vertical FOV, like the game.
- Each covered pixel traces one reflection ray (max 30 m), fetches the hit triangle for its normal, and loops over 32
  lamps.
- Logs: `03_bench/bench_run1.txt`, `bench_run2_threadgroups.txt`. Other Unity batch jobs were using the GPU, so treat
  values as ±30 %.

| Work | MEASURED (M3 Max) |
|---|---|
| BLAS build, 17 meshes / 160 k triangles, one encoder | 1.8-3.1 ms default; 1.7-2.0 ms `preferFastBuild` |
| BLAS build, one 20,480-triangle mesh | 0.59-0.63 ms; **refit 0.17-0.26 ms** |
| BLAS build, one 12-triangle box (fixed cost of one build pass) | 0.15-0.16 ms |
| BLAS memory before / after compaction | 13.6 MB / 6.2 MB (**46 %**); vertex + index data 2.8 MB |
| TLAS rebuild: 256 / 1,024 / 1,792 / 4,096 / 16,384 instances | 0.18-0.26 / 0.23-0.25 / 0.29-0.30 / 0.52-0.53 / 1.97-2.06 ms |
| Trace, 1080p, glass 25 %, nearest 256 instances, no lamps | 0.32-0.39 ms |
| Trace, 1080p, glass 25 %, all 2,912 instances, no lamps | 0.34-0.48 ms |
| Max ray length 30 m vs 10,000 m | no measurable difference |
| Threadgroup 8x8 vs 16x16 vs 32x32 | no consistent difference (within noise) |

What this means:
- **Throughput.** About 1.0-1.6 billion reflection rays per second with simple shading.
- **Scene size is almost free.** Hardware traversal scales with log(scene size), so **registering the whole
  neighbourhood costs almost nothing**. The prototype's 256-instance cap saves nothing and causes defect 4.

### 2.4 MEASURED (bench 2): production-like hit shading, and supersampling

`03_bench/rt_bench2.mm.txt` reuses the same scene and changes only the shading:
- **URP-style lamps.** Inverse-square falloff with URP's smooth range fade, and the troffer's spot cone (outer 162°,
  inner 96°, pointing down, as in `FrontRoomsMapWorld.cs:1202-1204`).
- **Shadow rays for the N nearest lamps actually in range.** Bench 1 shadowed the first lamps in its list, which
  understated the cost.
- **Albedo from an 8-layer 1024² RGBA8 texture array** with mips. UVs are world-planar at 0.6 m tiles, as
  FrontRooms/Surface does. The mip level comes from a ray cone [G19].
- Per-instance emission for the lenses.
- 1, 2 or 4 rays per pixel (rotated-grid offsets, the 4x MSAA pattern).

Two runs of two passes each; two other Unity batch processes were using the GPU. Log:
`03_bench/bench2_production_shading.txt`.

| Case (all instances, 32 lamps, 30 m rays) | MEASURED range |
|---|---|
| A: 1440p, glass 25 %, bench-1 shading, 4 shadowed (reference) | 0.72-1.11 ms |
| B: A with URP falloff + 4 **nearest** lamps shadowed | 0.80-1.28 ms |
| **C: B + texture-array albedo (the production shape)** | **0.86-1.36 ms** |
| C with 8 nearest shadowed | 1.24-1.79 ms |
| C, 2 rays per pixel | 1.89-3.13 ms |
| C, 4 rays per pixel | 4.31-5.39 ms |
| C at 1080p, glass 25 % | 0.73-0.89 ms |
| C at 1080p, 4 rays per pixel | 2.71-3.05 ms |
| C at 1080p, glass **100 %** (pane fills the screen, the destruction shot) | 1.57-2.44 ms |
| C at 2160p, glass 25 % | 2.11-3.48 ms |

What this means:
- **Real shading adds little.** URP-correct falloff, nearest-lamp shadows and a texture fetch add about **+10-35 %**
  over the bare trace. Production cost for a held pane is **~1 ms at 1080p and ~1-1.4 ms at 1440p**.
- **Shadow rays are the biggest dial.** Going from 4 to 8 shadowed lamps adds 0.1-0.9 ms at 1440p (noisy).
- **Supersampling scales linearly.** 4 rays per pixel everywhere is 4-5x the cost. Spend extra rays only where they
  help (§6.2).
- **Still not measured.** Unity's own GPU load, the glass prepass, and the game's real meshes and textures. Time
  these in a player build with GPU timestamps (open item 4). The G11 editor bench measured a CPU-bound 37-55 ms
  frame, so ~1 ms of GPU is small next to it.

### 2.5 Tracing API: what to use

- **Intersector, not intersection query**, on M3 [A7]. Result fields on a triangle hit: `distance`, `primitive_id`,
  `instance_id`, `triangle_barycentric_coord` (with the `triangle_data` tag). With `world_space_data` you also get
  the object-to-world transform [A2][A6]. Barycentrics interpolate UVs: `uv0*b.x + uv1*b.y + uv2*b.z` [A6].
- **User instance IDs** (`MTLAccelerationStructureUserIDInstanceDescriptor`, read as `user_instance_id`) carry
  "per-instance material ID or per-instance flags" [A2]. This is where the per-piece **glass flag** and the window id
  for destruction live ([D3] §4.7).
- **Instance masks** (8 bits) let one ray type skip a class of objects [A3]. Shadow rays skip glass. Reflection rays
  skip small falling shards ([D3]: "smaller pieces stay out of reflection rays via the instance mask").
- **Shadow rays**: `accept_any_intersection(true)` and a max distance to the light [A3].
- **Alpha**: intersection functions or the query loop do alpha testing [A1][A2]. No alpha-tested surface is known in
  the glass's reflection range; keep everything **opaque** for the hardware path.
- **Payload / occupancy**: combining heavy shading with intersection "may end up with a compute kernel that runs at
  lower occupancy" [A1]. Bench 2 shows the one-bounce shading kernel is fine.
- **Apple samples.** "Accelerating ray tracing using Metal" is tied to WWDC20 10012 and WWDC22 10105.
  "Rendering reflections in real time using ray tracing" ("dynamically generating reflection maps by encoding a
  ray-tracing compute pass") is tied to WWDC24 10089, WWDC22 10101, WWDC21 10286 and WWDC21 10150 [A13]. Their pages
  have no body text beyond that; the code was not downloaded.

### 2.6 Residency and bindless hit shading

- **What needs residency.** Anything the kernel reaches **indirectly**: BLASes referenced by the TLAS, vertex/index
  buffers read through GPU addresses, textures read through resource IDs. Make them resident per encoder with
  `useResource:usage:` / `useHeap:` [A5][A6], or with a **residency set** [A11 `MTLResidencySet`, A14]. Metal's shader
  validation names a missing one by buffer label [A5].
- **Residency sets** (macOS 15+, so fine on Red's macOS 26), per Apple's documentation [A11]:
  - They "Can attach to a command buffer with a single call", which covers every encoder in it. That is exactly
    Unity's current command buffer in our case.
  - You can add allocations "while the GPU is actively running a command buffer that's accessing them".
  - They "don't track hazards", so a BLAS build and the trace that reads it need an `MTLFence` between the
    acceleration-structure encoder and the compute encoder.
  - Their methods "aren't thread-safe": only the render thread may touch the set.
  - **Recommendation:** one residency set holding the BLAS heap, the geometry pool, the material and lamp tables, and
    the texture array. Attach it to Unity's current command buffer in the render event.
- **Metal 3 bindless.** Write `gpuAddress` and `gpuResourceID` straight into a buffer struct ("Metal 3 simplifies
  writing argument buffers by allowing you to directly write into them like any other CPU-side structure" [A5]).
  Apple's hybrid talk: hit shading reads "vertex data and Metal resources from the compute kernel directly", which is
  "achieved with a bindless binding model which in Metal is represented as argument buffers" [A3].
- **The simpler cross-platform choice** for FrontRooms: a **material table + one `Texture2DArray`** of albedo/mask maps
  (§6.3). The Windows path has no bindless in Unity HLSL. On Mac the array is one resource to make resident. Bench 2
  measured this shape.

### 2.7 Unity interop facts that shape the design

- **Render thread.** Native rendering must run on the render thread via `IssuePluginEvent`/`IssuePluginEventAndData`.
  With multithreaded rendering "the rendering API commands run on a separate thread from MonoBehaviour scripts" [U1].
  Multithreaded rendering is on (`ProjectSettings.asset:54 m_MTRendering: 1`).
- **Unity's command buffer.** `IUnityGraphicsMetalV2` exposes `CurrentCommandBuffer()`, `EndCurrentCommandEncoder()`
  and `CommitCurrentCommandBuffer()`. The header says "you should end unity's encoder before creating your own and end
  yours before returning control to unity" (`IUnityGraphicsMetal.h:48`) [U3]. Encoding the AS updates and the trace
  into Unity's current command buffer puts them exactly between the opaque pass and the transparents.
- **Native pointers are slow.** `GetNativeTexturePtr`, `GetNativeVertexBufferPtr`, `GetNativeIndexBufferPtr` and
  `GraphicsBuffer.GetNativeBufferPtr` all synchronize with the render thread [U1]. Call them only when a resource is
  (re)created. So the RT output and the prepass targets must be **persistent RTHandles imported into RenderGraph**,
  not per-frame transient textures.
- **Changed meshes get new buffers.** Changing a mesh's CPU data can re-create its GPU buffers; call `GetVertexBuffer`
  again [U1]. Skinned meshes expose their skinned output with `SkinnedMeshRenderer.GetVertexBuffer()` after requesting
  `GraphicsBuffer.Target.Raw` [U1].
- **Colour space of set colours.** `MaterialPropertyBlock.SetColor` "assumes that the color value is in sRGB space"
  and "converts the value to linear color space if the active project color space is linear" (it is,
  `m_ActiveColorSpace: 1`) [U1]. `Material.SetColor` converts only `[HDR]`/`[Gamma]` properties, and URP/Lit's
  `_EmissionColor` is `[HDR]` [U1]. So the lens brightness the GPU sees is the **linear** conversion of what the map
  passes, not the number itself (§6.3).

### 2.8 MetalFX and Metal 4: not for this pass

- **MetalFX denoised upscaler (WWDC25).** It takes noisy ray-traced colour plus world normals, diffuse and specular
  albedo, roughness, motion vectors and depth, and goes "after jittered rendering, and before post effects" [A9].
  `MTLFXTemporalDenoisedScaler` needs macOS 26 [A11] and Apple9 [A12].
  - It replaces the whole AA/upscale stage with a temporal one. FrontRooms renders with 4x MSAA and no TAA on purpose,
    and mirror glass needs no denoising, so **do not use it here**.
  - It is the tool if Red ever wants path-traced GI.
- **WWDC26 session 359** is about MetalFX neural denoising for path tracing (checked for relevance only) [A10].
- **Metal 4** adds intersection function buffers (DirectX shader-table style) and per-build flags [A9]. Neither is
  needed for an opaque one-bounce trace.

---

## 3. Hybrid ray-traced reflections in shipped games (item 2)

| Game / engine | Which pixels trace, ray budget | Hit shading | Miss / far | Denoise | Glass / transparent |
|---|---|---|---|---|---|
| **Battlefield V** (DICE, Frostbite, 2018) [G1][G3][G4] | Variable-rate tracing per screen tile ("More Rays on Water", "More Rays on grazing angles"). Presets: Low/Medium trace materials with "0.9 smoothness or higher", max ray count 15.0 % / 23.3 % of screen pixels; High/Ultra "0.5 smoothness or higher", 31.6 % / 40.0 % [G3] | Closest-hit shaders generated from the raster shaders ("Shader output must match!"). Hits lit by a loop over point lights, spot lights and reflection volumes from a per-cell light list [G1] | Screen-space march first; rays only where it fails ("SS-Hybridization") | Spatial BRDF filter + own temporal filter + an image filter sized by {angle, roughness} | "windows, cars, tanks, lamp posts, tiles, puddles and weapons" are traced (C. Holmquist [G4]). Particles in a second TLAS with an order-independent any-hit (0.96 → 0.34 ms) [G1] |
| **PICA PICA** (EA SEED, 2018) [G2] | Rasterize primary visibility; trace at half resolution, reconstruct at full | Material layers; shadow ray at the hit | — | Stochastic-SSR-style spatial reuse, colour-box-clamped history, variance-guided bilateral | Glass by refracted rays with IOR transitions and tint. "Clear: No filtering required"; rough glass opens a cone and needs more samples or temporal filtering |
| **Control** (Remedy, Northlight, 2019) [G5][G6][G7] | Separate settings for opaque and **transparent** reflections | (chapter not readable here) | SSR or cubemaps when RT is off [G6] | "denoisers tailored for these effects" [G7 abstract] | "Adding ray-traced reflections to glass is no different than ray tracing a puddle, if the background is opaque. When it's transparent, with detail visible on the other side, in an office for instance, extra work is required". Transparent reflections trace "without blocking the visibility of detail beyond the window" [G5] |
| **Metro Exodus Enhanced Edition** (4A, 2021) [G8] | "We use RTR to fill these gaps where SSR fails"; rays "only ... when absolutely necessary" | "the same, low level of detail, material system as the diffuse Ray Tracing pipeline, but ... the higher quality PBR lighting model to accurately reflect data from analytic light sources"; emissive surfaces add light when hit | — | not stated | not stated |
| **Cyberpunk 2077** (CD Projekt RED + NVIDIA, 2020) [G9] | "used on all surfaces and can trace ranges for up to several kilometers"; present on "both opaque and transparent" surfaces, "tracing a single bounce" | — | — | (NRD per secondary sources, UNVERIFIED) | "glass features transparent reflections" |
| **Marvel's Spider-Man: Miles Morales** (Insomniac, PS5, 2020) [G17] | The 60 fps "Performance RT" mode keeps ray tracing "by adjusting the scene resolution, reflection quality, and pedestrian density" | — | — | — | City windows are the main RT surface |
| **Marvel's Spider-Man 2 PC** (Insomniac / Nixxes, 2025) [G18] | Settings: "higher quality meshes for raytracing" (geometry detail) and an "object range slider to increase the range at which objects are considered for raytracing" | — | — | DLSS Ray Reconstruction combines "denoising of the raytracing features and upscaling" | "ray-traced reflections quickly catch your eye ... as you swing along its many glass surfaces"; separate "ray-traced interiors" option (how they work: UNVERIFIED) |
| **RE Engine** (Capcom R&D, RE:2023 talk) [G16] | "high resolution and low resolution traced specular buffers are separated", by roughness and title budget; earlier versions fell back to "fake specular" above a roughness threshold | not detailed | — | Specular history with colour clamp ("Instead of using history, color clamp is also needed") | "Mirror transparency is handled specially with separate implementation. Only perfect reflection is supported and no denoiser is used." |
| **UE5 Lumen** (Epic) [G10] | Screen traces first; "Max Roughness to Trace" ("Higher values ... greatly increase GPU cost") | Surface Cache "for best performance"; "Hit Lighting for Reflections for higher quality" | falls back to the other tracing methods | temporal | "High Quality Translucency Reflections": "high quality mirror reflections on the front layer of translucent surfaces. Other layers will use the lower quality Radiance Cache"; single-layer water has "reflections forced to mirror" |
| **UE Thin Translucent** shading model (Epic) [G20] | — | "accounts for light bounces from the air into the glass, and from the glass into the air", in one pass | — | — | Shows the white specular and the tinted background together: the thin-glass model FrontRooms/Glass approximates |
| **Unity HDRP** RT reflections [G12] | "Minimum Smoothness"; "Full Resolution: One ray per pixel, per frame", else one ray per four pixels | Light cluster at the hit | "Ray Miss": reflection probes, sky, both, or nothing | Optional spatio-temporal filter | Recursive Rendering is for "multi-layered transparent GameObjects", but "for best performance, use ray-traced reflections" for mirrors and puddles; recursive objects get no other RT effects [G12b] |
| **AMD FidelityFX Hybrid Reflections** (2023) [G13] | SSSR first, hardware rays for the rest, "per-pixel feedback to reduce hybridization cost", FSR 1 upscale of low-res reflections | reshades re-projected pixels | — | FidelityFX reflection denoiser | — |

**Denoisers and TAA.**
- SVGF reconstructs 1-sample path tracing "in approximately 10ms at 1920x1080" with temporal accumulation and a
  variance-guided wavelet filter [G15].
- NVIDIA's NRD needs non-jittered motion vectors, normals, roughness, view Z and hit distance, and does its own
  temporal accumulation. Its sample shows a "denoising-free" glass path; for pure mirrors it recommends "Primary
  Surface Replacement" [G14].
- All of these exist to clean **rough** reflections traced with few rays. Capcom's mirror path and SEED's clear glass
  use none [G16][G2].

**Lessons for FrontRooms glass:**
1. **Raster decides primary visibility.** Rays start from the G-buffer of the surface ([A3][G1][G2][G10]).
2. **Glass is the cheap case: mirror rays are noise-free.** Trace one ray per glass pixel at full resolution (§2.4).
   Keep the half resolution of PICA PICA/HDRP/Spider-Man for a weaker tier only.
3. **Without TAA, the only artefact left is aliasing of reflected detail** (§6.2), not noise. Fix it spatially, not
   temporally: no motion vectors, no ghosting, works with MSAA.
4. **Smudges are the only "rough" part.** Blur them deterministically by roughness and hit distance, as BFV's image
   filter does ({angle, roughness} → kernel size for a unit-length ray [G1]).
5. **Hit shading must match the raster** (BFV [G1]) and needs real lights (Metro's "analytic light sources" [G8];
   Lumen's "Hit Lighting" [G10]). For an office lit by troffers, **emissive lenses + lamp lights at the hit** are the
   content.
6. **Glass with something visible behind it is a special case** (Control's "extra work", Lumen's "front layer").
   - The reflection is **added** over what is seen through the pane and never hides it. FrontRooms/Glass already
     works this way ("Preserve Specular", premultiplied blend).
   - Only the front glass layer needs a mirror trace (Lumen). A second pane seen through the first keeps its cube,
     which is what the glass hook's depth tag does (§6.1).
7. **Cull the ray-traced scene by distance and angular size.** BFV: culling at 4° took BLAS rebuilds from 5,000 to
   400 and instances from 20,000 to 2,800 (64 → 14.5 ms), then 1.15 ms with staggered refits and fast-build flags
   [G1]. Spider-Man 2 exposes the same idea as an "object range" [G18]. Our scene is far smaller (§2.3).

---

## 4. Unity on Windows: a DX12 path that feeds the same texture (item 3)

**Yes, URP 17.3 can do it without HDRP.** The ray-tracing API is pipeline-agnostic:
- **The class.** `RayTracingAccelerationStructure` "enables you to efficiently intersect rays against a subset of the
  Scene geometry using the GPU". It "only applies when SystemInfo.supportsRayTracing is true".
- **Binding.** For ray-tracing shaders use `RayTracingShader.SetAccelerationStructure` or
  `CommandBuffer.SetRayTracingAccelerationStructure`. For compute shaders use
  `ComputeShader.SetRayTracingAccelerationStructure` / `CommandBuffer.SetRayTracingAccelerationStructure`. Or use
  `Shader.SetGlobalRayTracingAccelerationStructure` "for all shader stages" [U1].
- **Two ways to trace:**
  - **Ray-generation shaders** (`RayTracingShader`, `CommandBuffer.DispatchRays`, DXR 1.0).
  - **Inline ray queries** in compute (`RayQuery`; "In DirectX 12 (DX12), this property corresponds to DirectX
    Raytracing (DXR) Tier 1.1 support", `SystemInfo.supportsInlineRayTracing`, `#pragma require inlineraytracing`,
    `UnityRayQuery.cginc`) [U1]. The Ray Tracing API left experimental in 2023.1 [U1].
- **Requirements.** Windows, DX12 as the active API, a DXR GPU. HDRP's list (RTX 20+, RX 6000+) is the practical floor
  (G11 §1.2). The project's `m_BuildTargetGraphicsAPIs` is empty (Unity's defaults). Whether Unity 6.3 defaults to
  DX12 or DX11 on Windows is UNVERIFIED, so set DX12 explicitly for that build.

**Instance management** (6.3 scripting reference [U1]):
- `ManagementMode.Manual`.
- `AddInstance(Renderer, ...)` (the `id` comes back in HLSL as `InstanceID()`), or
  `AddInstance(ref RayTracingMeshInstanceConfig, matrix, ...)` for meshes without a renderer.
- `UpdateInstanceTransform`; `RemoveInstance`.
- `CullInstances` with `RayTracingInstanceCullingConfig` "to add only the instances which match certain criteria".
- After any change, call `Build` / `CommandBuffer.BuildRayTracingAccelerationStructure` "before dispatching any
  shaders" [U1].
- Unity builds and refits the BLASes itself.

**UnifiedRayTracing (URT), public API in Core 17.3** (`Unity.UnifiedRayTracing.Runtime`; installed package source
[U2]):
- `MeshInstanceDesc` (mesh, sub-mesh, transform, mask, `instanceID`, `opaqueGeometry`) and a `Hit` with
  `instanceID`, `primitiveIndex`, `uvBarycentrics`, `hitDistance` and `isFrontFace`.
- `FetchHitGeomAttributes` for positions, normals and UVs (`IRayTracingAccelStruct.cs:8-66`,
  `CommonStructs.hlsl:28-34`, `FetchGeometry.hlsl:65-116`).
- **Correction to revision 1:** URT's **hardware** backend runs a `RayTracingShader` with `DispatchRays`. A
  `.urtshader` imports as both a ComputeShader and a RayTracingShader (`RayTracingContext.cs:122-129`,
  `Hardware/HardwareRayTracingShader.cs:27-116`). It is chosen when `SystemInfo.supportsRayTracing` is true;
  otherwise URT falls back to its compute backend (`RayTracingContext.cs:68, 98-105`).
- "Ray tracing with the UnifiedRayTracing API" is in the 6.3 manual [U1].
- **Recommendation: write the Windows path on URT.** The same HLSL hit shader then also runs, slowly, on the compute
  backend anywhere, which is handy for debugging the hit-shading parity.

**RenderGraph integration (URP 17.3 source [U2]):**
- The legacy builder has `Read/WriteRayTracingAccelerationStructure` and `ImportRayTracingAccelerationStructure`
  (`RenderGraphBuilder.cs:146-163`, `RenderGraph.cs:1268`). URP's `AddComputePass`/`AddUnsafePass` builders do not
  track acceleration structures.
- `ComputeCommandBuffer` wraps `BuildRayTracingAccelerationStructure` and `SetRayTracingAccelerationStructure`
  (`ComputeCommandBuffer.cs:558-600`).
- So: one unsafe pass that reads the glass prepass, writes the output RTHandle, calls `AllowPassCulling(false)`,
  builds the RTAS and dispatches the trace.

**Shared contract.**
- The Windows pass writes the same `_FR_GlassRTReflection` (RGBA16F, with the depth tag) and `_FR_GlassRTWeight`.
- It uses the same glass prepass, material table, lamp list and hit-shading rules as Mac (§6.3). Only the trace
  kernel differs: MSL in the plugin vs HLSL/URT in Unity.
- **Limitation:** there is no DX12 RTX machine here (G11), so this path cannot be tested on this Mac. Until one
  exists, Windows keeps report 11's planar reflection for the held pane (as `../destruction/10` §4.8 says).
- Effort ESTIMATE: 4-6 days plus a test PC.

---

## 5. What makes a reflection in interior window glass read as real (item 4)

### 5.1 How strong a clean pane reflects (physics)

Fresnel for glass n = 1.52, unpolarized, computed here with the Fresnel equations [P1] (ESTIMATE; [P1]: "about 4%" per
surface at normal incidence). A pane has two surfaces. Light reflected inside it adds up incoherently (window glass
is far too thick for interference).

| View angle from the pane normal | One surface | Whole pane (front + back) | Back-surface image alone | Offset of the back image, 6 mm pane | Same, 30 mm prototype cube |
|---|---|---|---|---|---|
| 0° (face-on) | 4.3 % | **8.2 %** | 3.9 % | 0 mm | 0 mm |
| 30° | 4.4 % | 8.4 % | 4.0 % | 3.6 mm | 18 mm |
| 45° | 5.3 % | 9.7 % | 4.4 % | 4.5 mm | 22 mm |
| 60° | 9.2 % | 15.7 % | 6.2 % | 4.2 mm | 21 mm |
| 70° | 17.5 % | 27.5 % | 9.3 % | 3.2 mm | 16 mm |
| 80° | 39 % | 54 % | 12.5 % | 1.8 mm | 9 mm |

Consequences:
- **URP's dielectric F0 of 0.04 models one surface.** A real pane is about **twice** as reflective face-on. UE's Thin
  Translucent model exists for exactly this reason: it "accounts for light bounces from the air into the glass, and
  from the glass into the air" [G20].
  - The glass hook (§6.1) weights the RT radiance with URP's `EnvironmentBRDFSpecular`, i.e. F0 0.04.
  - Recommendation to the glass track: use a pane F0 ≈ 0.078 for the RT term. Schlick with 0.078 gives
    7.8 / 8.0 / 10.7 / 19 % at 0 / 45 / 60 / 70°, close to the exact two-surface curve up to 60°.
- **The second image is almost as strong as the first** (3.9 % vs 4.3 % face-on). On 6 mm glass it is offset by
  2-4.5 mm.
  - Seen from the camera, the two images separate by offset / path length. That is 3 px at 1080p and 4 px at 1440p
    for something 1 m away along the reflected path, and under 1 px beyond about 4 m.
  - So it shows only on near, bright edges: a lens at arm's length, the Relay right behind the player.
  - The prototype's 30 mm "pane" would double things by 15-20 px. That is one more reason to treat the thin box as a
    single sheet (§6.3).

### 5.2 Reflection vs the light behind the glass

Whether you see the reflection or the room behind is decided by the ratio of reflected to transmitted luminance. The
one-way-mirror principle: "The light from the bright room reflected from the mirror back into the room itself is much
greater than the light transmitted from the dark room" [P2].

ESTIMATE for a real office (troffer lens about 2,000 cd/m², assuming ~4,000 lm over a 0.72 m² prismatic lens; a bare
fluorescent lamp is about "12 kcd/m2" [P5]; a lit office wall about 95 cd/m² at 500 lx and 60 % reflectance; an unlit
room seen through glass about 2-5 cd/m²; transmission ≈ 0.9):

| Behind the glass | Seen through it | Reflected wall (8 %) | Reflected troffer lens (8 %) | What reads |
|---|---|---|---|---|
| A lit room | ~86 cd/m² | ~8 cd/m² = **9 %** of the background | ~160 cd/m² = **~2x** the background | Only the troffers (and other bright things) show; the room is a faint ghost |
| A dark room | ~2-5 cd/m² | ~8 cd/m² = **2-4x** the background | ~160 cd/m² = 30-80x | A mirror: the whole lit room, the player's side, the Relay behind you |
| Lit room, pane at 60° | ~80 cd/m² | ~15 cd/m² = 19 % | ~310 cd/m² | Walls start to show at grazing angles |

**The game agrees.** G10 measured that even a perfect cube reflection adds only +2-4/255 to a face-on pane in our
evenly lit rooms ("In these evenly lit rooms a dielectric reflects about 4 % face-on, and the reflected room is about
as bright as the room behind the pane") [D2]. RT will **not** make face-on panes in lit-to-lit windows visible, and
it should not try. It wins:
- in **dark-beyond windows**, which become mirrors of the player's room;
- on **lens reflections at 40-70°**;
- on **things that move**, or that a cube cannot hold (the Relay, an opened door, a lamp dying behind the player).

**Troffers in the game (ESTIMATE, to verify in a capture).**
- Level 0 lens emission is `(1, .96, .84) x 2.6` (`FrontRoomsMapWorld.cs:2043`). It is set through `[HDR]
  _EmissionColor` and then per frame through `MaterialPropertyBlock.SetColor` (`:1288`). Both convert sRGB to linear
  (§2.7), so the GPU sees about **9** (linear) at full level.
- A Level 0 wall 1.5 m sideways from a lamp of intensity 5 receives about 1.3-1.5 (linear irradiance, URP units,
  several lamps). It shows about 0.6 at an albedo of ~0.45.
- Lens/wall ≈ **15x** in the game vs ≈ **20x** in a real office: close enough that the troffers will rank first in
  reflections, as they should. If the conversion assumption is wrong, the lens is 2.6 and the ratio drops to ~4x,
  which would flatten every reflection (open item 5).

### 5.3 What breaks a reflection up (and makes it believable)

**Flatness and waviness.**
- Float glass has "uniform thickness and a very flat surface" [P4].
- Heat-treated (tempered or heat-strengthened) glass has **roll wave**, "a repetitive wave-like departure from
  flatness in glass that results from heat treating glass in a horizontal roller hearth furnace". It shows as
  "alteration of viewed images in transmission or reflection" [P6]. ASTM C1048 does not set a limit for it [P6]; the
  industry measures it in millidiopters (ASTM C1651) [P6].
- One fabricator specifies at most 0.127 mm peak-to-valley for glass up to 10 mm, with ripples "typically 100-300 mm"
  apart [P7].
- Computed from that: the surface tilts at most π x 0.127 / 300 to π x 0.127 / 100 = **1.3-4 mrad**. That bends the
  reflection by 2.7-8 mrad, i.e. **0.15-0.46°**. On long straight reflected lines (troffer rows, the ceiling grid)
  this is a slow 1-5 px wobble as the player walks: subtle and real.
- The destruction plan chose **6 mm annealed float glass** for the window panes (it cracks in stages) [D3]. Annealed
  glass has no roll wave at all, only a slight overall bow from glazing.
- Today's glass shader: `_RollStrength` 0.02 at a 0.37 m period (`proj_glass` `FrontRoomsGlass.shader`, properties and the
  "long, shallow roll" block). That tilts the normal by about 20 mrad, a **2.3°** swing of the reflected ray: 5-15x the tempered-glass
  limit, and infinitely more than annealed glass.
  - On a cube map at infinity this hardly shows.
  - With a traced reflection that has true parallax, it will look like warped plastic.
- **Suggestion:** `_RollStrength` 0.001-0.002 on annealed window panes when RT is on, and 0.003-0.004 only on panes
  meant to read as tempered (door sidelights).
- US codes today require tempered or laminated glass "near doorways", in "large windows" and "windows which extend
  close to floor level" [P3]; what 1990 codes required is UNVERIFIED ([D3]).

**Grime.**
- Smears and prints raise roughness locally (the shader's 0.96 → 0.62). Each smudge blurs the reflection inside its
  outline and adds a little haze, so the reflection stays sharp between smudges. That contrast between sharp and
  blurred patches is a strong "this is a real surface" cue.
- G10 adds a warning: fine bright specks read as **marks on the floor behind** the pane. Smudges read when they are
  soft, directional, at hand height, and catch a lamp's specular [D2]. With RT, a smudge that lines up with a
  reflected troffer is exactly the lamp catch G10 asked for.
- All of this is the glass shader's job. The RT pass only has to deliver a sharp mirror image, plus (with the prepass)
  the roughness-aware blur (§6.3).

**The troffers dominate.** Lenses are the brightest reflected objects, so their reflections must flicker in sync
with the real lamps. The map sets each lens's `_EmissionColor` per frame through a `MaterialPropertyBlock`
(`FrontRoomsMapWorld.cs:1288-1289`), so the hit shader needs the **per-instance current, linear emission**, not the
shared material's value. The prototype reads the shared material and cannot do this.

**The moving wallpaper.** The wallpaper print layer is staged, not merged (`Tools/print/unity_staging/
FrontRoomsPrintDriver.cs`: globals `_FR_Print`, `_FR_PrintClock`, `_FR_PrintWarp`). When it lands, the reflected wall
must show the same print frame as the real wall. A reflection that disagrees with the wall would be noticed. Red may
want that as a deliberate beat one day, but it must never happen by accident.

**Parallax and occlusion.** The reflected room must move correctly as the player walks, and must include what is
behind the player (a lamp going dark, the Relay). A cube map cannot do this. A planar camera can, but only for one
pane (G11 §2.4). RT does it for every pane.

**The player.** A first-person camera with no body casts no reflection: in a mirror-dark window the room appears but
the player does not. The destruction plan offers a "reflection-only body (desktop ray tracing only)" option ([D3]
§3, option b). Red decides (open item 6).

### 5.4 How to judge it (acceptance views)

From G10 and the physics above, RT acceptance frames should be:
- **oblique views** (45-65°) with a lamp lens in the reflection;
- a **staged dark room** behind a lit window (no dark-beyond window exists near the start at seed 4242 [D2]);
- the **Relay crossing behind the player**;
- the destruction shot's **pane filling the frame at 0.55 m** ([D3] §4.7).

Face-on lit-to-lit views are the control case: the RT result there should be nearly invisible. If it is not, the
reflection is too strong.

---

## 6. The production design this research points to

### 6.1 Frame order (desktop, Mac M3+ first)

1. **Opaques** (URP Forward+, 4x MSAA) → resolved `_CameraDepthTexture` (already on).
2. **Glass RT prepass.** A new raster pass at 1x the camera's (scaled) resolution, after opaques and before
   transparents.
   - It draws only the RT-receiving glass renderers (panes and glass pieces) and tests by hand against
     `_CameraDepthTexture`.
   - Two persistent targets: `GlassPos` RGBA32F (world position, w = linear eye depth) and `GlassNrm` RGBA16F (world
     normal, smoothness).
   - Writing world position directly avoids every camera-reconstruction convention, which is what broke the
     prototype (defects 1 and 2).
3. **Trace** (unsafe pass → `IssuePluginEventAndData`). In the native callback, `EndCurrentCommandEncoder`, then on
   Unity's `CurrentCommandBuffer`:
   - (a) an acceleration-structure encoder for pending BLAS builds (budgeted), refits, and this frame's TLAS
     rebuild;
   - (b) an `MTLFence`, the residency set attached to the command buffer, then a compute encoder: one mirror ray per
     glass pixel, hit shading (§6.3), anti-aliasing (§6.2);
   - (c) a small post kernel: roughness-and-distance blur and the optional back-surface image.
4. **The output in the glass track's format.** The glass track built the hook (`proj_glass`
   `FrontRoomsGlass.shader`, its five `[G14-HOOK-BEGIN]`/`[G14-HOOK-END]` blocks). The RT pass writes `_FR_GlassRTReflection` (RGBA16F) and
   `_FR_GlassRTWeight` exactly as that hook reads them:
   - **RGB:** reflected linear HDR radiance with **no Fresnel and no strength applied**.
   - **A:** 0 = no RT. 0 < A ≤ 1 = coverage. **A > 1 = 1 + linear eye depth (m) of the glass surface the texel was
     traced for.** The hook then applies it only to the glass layer at that depth (tolerance 0.02 + 0.01 x depth), so
     a second pane seen through the first keeps its own reflection. The prepass's w gives this value directly.
   - The hook is compiled under a global keyword `_FR_GLASS_RT`, enabled only for the camera the RT pass feeds. A
     stripper removes the keyword from WebGL builds. Each material opts in with `_RTReceive`.
5. **Transparents.** Where `_FR_GlassRTWeight` x coverage > 0, FrontRooms/Glass replaces its environment term with
   the RT radiance, weighted by its own Fresnel and grime, before fog.
6. **Post** (bloom, grade, ACES). The reflections are graded and bloomed with the scene (fixes defect 3). The
   prototype's full-screen composite and its after-post event are deleted.

### 6.2 Where rays start, which pixels trace, and anti-aliasing without TAA

**Which pixels and rays:**
- Every pixel of every visible receiving pane traces. Glass smoothness is 0.62-0.96, well above BFV's 0.5 cut-off
  [G3]. No stochastic sampling is needed.
- Full resolution on Mac M3+ and DXR PCs (§2.4). Half resolution (one ray per 2x2, as PICA PICA/HDRP) only for an
  M1/M2 tier, where Metal RT runs in software. That tier's speed is UNVERIFIED; default it off.
- Reflection rays are 24-30 m long. Beyond that, or on a miss, coverage = 0 and the pane keeps its zone cube (HDRP's
  "Ray Miss: reflection probes" [G12]).
  - Exponential-squared fog (density 0.014, `FrontRoomsLook.cs:20-21`) is 6 % at 18 m and 16 % at 30 m, so a longer
    ray adds little.
  - Bench 1 found no cost difference between 30 m and 10 km rays, so this limit is for looks, not speed.
- **The normal.** Best is the glass shader's **own** perturbed normal (roll, smudge normal, crack facets), written by
  a tiny `FRGlassRTPrepass` pass in FrontRooms/Glass (contract request, §6.6). Fallback without it: the geometric
  normal from the prepass. Real fracture facets come from the destruction track's stage meshes in either case: the
  facets are baked into the geometry [D3].

**Anti-aliasing (new in revision 2).**
- *The problem.* MSAA anti-aliases the **edges of the pane**, not what the pane reflects. A one-ray-per-pixel mirror
  is point-sampled. Every high-contrast edge in the reflection steps and crawls as the player walks: a lens border
  (linear ~9) against a ceiling tile (~0.5), the dark gap between ceiling tiles, a door frame. Shipped games hide this
  with TAA or their denoiser's history. FrontRooms has neither, on purpose. On a mirror-dark window the reflection is
  the whole image, so this is the most visible remaining artefact.
- *Texture detail.* Pick the mip at the hit from a **ray cone**: one trilinear lookup, "a small amount of ray
  storage, and fewer computations than ray differentials" [G19]. Bench 2 includes this; it costs +5-20 %.
- *Geometric edges in the reflection, two tiers:*
  - **High:** an edge-aware filter on the RT buffer (FXAA-style luminance edge search [G21]), only inside one
    pane's depth tag. ~0.1 ms (ESTIMATE). No history.
  - **Cinematic:** adaptive supersampling. After the 1-ray pass, pixels whose 3x3 neighbourhood has a high
    luminance ratio (> 4x) get 3 more rays in the rotated-grid pattern. Bench 2: 4 rays everywhere = 4.3-5.4 ms at
    1440p. Edges are typically 5-15 % of glass pixels (ESTIMATE), so adaptive costs **+0.15-0.6 ms**.
- *Do not* add TAA or a temporal accumulator for this. It would bring back the ghosting that made the project drop
  TAA.

### 6.3 Hit shading spec (one bounce, must match the raster)

**Per instance**, built on the CPU at registration; flags and emission updated per frame:
- material index;
- flags: glass, fracture edge, shard, emissive lens, Relay, door;
- current **linear** emission colour;
- object-to-world matrix;
- geometry offsets into our own position/normal/UV pool.

| Surface hit | Shading at the hit |
|---|---|
| FrontRooms/Surface (walls, floors, ceilings: 83 of 86 surface materials, G11) | Rebuild the UV exactly as the shader does: `PlanarFrame(positionWS, geometric normal)` → metres / `_TileSize` x `_BaseMap_ST` (`FrontRoomsSurface.shader:157-185`). No mesh UVs needed. Albedo and mask (smoothness) from the RT texture array. Macro wear at 8 m / 12.8 m in world space (`:193-197`). Colour `_BaseColor`. When the print layer lands: `_FR_Print` slice, `_FR_PrintClock` and `_FR_PrintWarp` as globals read once per frame |
| URP/Lit props (office kit) | UV0 by barycentric interpolation from our geometry pool; albedo from the texture array; mip level from the ray cone [G19] |
| Troffer lens (`URP/Lit` emissive cube) | Emission = this instance's **current** `_EmissionColor` in **linear** (the value after `MaterialPropertyBlock.SetColor`'s sRGB → linear conversion [U1]), flicker and dimming included |
| Another glass pane | Continue the ray through it once (x 0.9 transmission) instead of reflecting again; at most 2 glass layers |
| Relay parts, doors, keys | Normal materials; transforms live every frame |
| Lighting at every hit | The lamps the map has **on this frame** (lit within `lightRadius` 16 m: a few dozen of ~1,500 lights). Use URP's distance and spot falloff (troffers are 162°/96° spots, range 10 or 12 m, `FrontRoomsMapWorld.cs:1202-1206`) and their current intensity. Shadow rays **only** for lamps whose `Light.shadows` is on this frame (about one in three, `castsShadow` < .34 at `:1215`, within `shadowRadius` 9 m); unshadowed lamps stay unshadowed, exactly as in the raster. Diffuse plus GGX specular, like Metro's PBR hit lighting [G8]. Ambient from the same ambient SH as the raster |
| After lighting | Exp² fog over the **hit distance** only (the glass shader fogs camera-to-glass itself) |
| Not traced | Volumetric beams and particles (transparent; BFV needed a second TLAS for its particles [G1]). Revisit if the beams read as missing in mirror-dark windows |

**Blur and thin glass** (post kernel, before the glass shader reads the texture):
- **Smudge blur** (needs the prepass smoothness). Kernel radius from the pane's roughness at that pixel, the hit
  distance and the view distance, BFV-style ({angle, roughness} LUT scaled by ray length [G1]). Bilateral on the
  depth tag, so panes do not bleed into each other. Deterministic, no history.
- **Today's hook falls back to the zone cube under smudges.** It fades RT out for perceptual roughness 0.15 → 0.45
  (the hook's `smoothstep(0.15h, 0.45h, b.perceptualRoughness)`). A full smudge (smoothness 0.62 = roughness 0.38) keeps only ~14 % of RT.
  - Interim: acceptable, because smudged areas are small and the cube is of the same zone.
  - In a mirror-dark window, though, the smudges would show the static cube while the clean glass shows the live
    room.
  - Once the RT pass delivers the blur, the glass track should move that fade to roughness ~0.45 → 0.7 (contract
    request 7).
- **Back-surface image** (Cinematic option). A second tap shifted by 2·t·tanθt·cosθi / (camera distance + hit
  distance), weighted 3.9 / 8.2 of the total (§5.1), with t = 6 mm (not the 30 mm of the box).

**Parity test** (BFV's "Verifying correctness" [G1]). A debug mode traces **primary** rays through the same hit
shader and compares them per pixel with the raster frame. Every non-zero difference is a hit-shading bug. The same
test would have caught the FOV and flip defects on day one.

### 6.4 Scene management

- **BLAS per Unity `Mesh`**, ref-counted, built from **our own copy** of positions (+ normals/UVs for props) in one
  `MTLHeap`.
  - The copy is made with Unity's `GetVertexBuffer` + a GPU copy, or from CPU mesh data for the map's generated
    shells.
  - This removes defect 6, Unity's sub-allocation risk and the per-registration render-thread syncs.
  - Build on registration, inside Unity's frame, batched on one encoder, at most ~100 k triangles per frame (~1-2 ms,
    §2.3). Compact a few frames later (-54 %).
- **Residency.** One residency set (BLAS heap, geometry pool, tables, texture array), attached to Unity's current
  command buffer each frame, plus a fence between the AS encoder and the trace (§2.6).
- **When to register** (events from the map chat, §6.6):
  - chunk built → shell meshes, panes, doors, lenses;
  - room dressed → its props;
  - chunk releasing → drop its instances at once, free BLASes 3 frames later.
- **TLAS rebuilt every frame** from live transforms.
  - Instances within ~36 m of the camera (12 m pane distance + 24-30 m rays).
  - BFV-style angular-size culling.
  - Capacity **≥ 1,024**, as the fracture contract asks [D3], up to 4,096 (0.5 ms).
  - Glass and pieces first, then nearest.
  - No 1 s rescans, no `FindObjectsByType`.
- **Dynamic things.** A marker component with a static registry (enable/disable registers and unregisters) on doors,
  Relay parts, keys and glass pieces; their transforms are read every frame. The Relay rig is 16 rigid
  MeshRenderers (probe stage via [D3]), so it needs no refit. A future skinned Relay would refit its BLAS every frame
  from `SkinnedMeshRenderer.GetVertexBuffer()` (~0.2 ms for 20 k triangles, §2.3).
- **Destruction** (aligned with [D3] §4.7):
  - The pieces are generated when E is pressed (0.35-1.0 s ahead of the beats), with 18 baked variants per glass type
    as a fallback.
  - All piece BLASes are built in **one batched encoder from index ranges of one buffer**
    (`FRGlassRT_AddMeshRanges`), between 0.70 and 1.0 s, never on a beat frame. 150 one-at-a-time builds at
    ~0.16 ms each would be ~24 ms; batched, they are a few ms spread over frames.
  - Instances per window: stage 1 = 1 (fins + crater), stage 2 = 1 (combined mesh, tilts baked into vertices),
    stage 3 = the **≤ 64 largest pieces**; smaller pieces are masked out of reflection rays. After settling, 2 static
    instances (teeth, floor glass).
  - Flags: per material, bit 0 glass, bit 1 fracture edge, bit 2 shard (instance mask). The window id goes in the
    user instance ID. Pieces use a dedicated `Glass_Fracture` material.
  - Rigid pieces are built once and never refit or rebuilt. Falling pieces only move transforms. No vertex-animation
    textures (the ray tracer would see the pieces at rest).
  - The reflection breaks into facets by construction, which is what cracked glass looks like.
- **Threading.**
  - Main thread → render thread through a lock-protected command queue (add/remove mesh, add/remove instance).
  - A 3-slot ring of per-frame data (camera, lamp list, dynamic transforms, print clock).
  - No `waitUntilCompleted` anywhere; compaction sizes are read back in a completion handler.
  - The residency set is touched only on the render thread (its methods "aren't thread-safe" [A11]).

### 6.5 Tiers and budgets (desktop only; WebGL unchanged)

| Tier | Detection | Trace | Expected GPU (from §2.4) |
|---|---|---|---|
| RT Cinematic (Mac M3/M4, DXR PCs) | `supportsFamily(MTLGPUFamilyApple9)` [A15] / `supportsRayTracing` | Receiving panes within 12 m, full res, 30 m rays, shadow rays for all shadow-casting lamps in range, adaptive 4-ray AA, back-surface image | 1440p ~1.2-2.5 ms; 4K ~2.5-4.5 ms (ESTIMATE) |
| RT High (same hardware, default) | same | Receiving panes within 12 m, full res, 24 m rays, shadow rays for the 4 nearest shadow-casting lamps, ray-cone LOD + edge filter | 1080p ~0.8-1 ms; 1440p ~1-1.5 ms |
| RT Low (M1/M2, software traversal) | `supportsRaytracing` but not Apple9 | Held pane only, half res | UNVERIFIED; measure before enabling |
| No RT (everything else, all WebGL) | — | `_FR_GlassRTWeight = 0`, `_FR_GLASS_RT` off: zone cubes (+ G11 planar for the held pane on desktop) | 0 |

The destruction shot (pane filling the screen at 0.55 m) is the worst case: 1.6-2.4 ms at 1080p with 1 ray
(MEASURED, case I).

### 6.6 Contract requests (nothing filed; exact signatures)

**Map chat** (`FrontRoomsMapWorld.cs`; the map chat confirmed these hook points exist and events are cheap):
1. `public event Action<GridCoord, Transform> ChunkBuilt;` raised at the end of `BuildInto`, after
   `built[coord] = chunk` (`:884`), with `chunk.root.transform`.
2. `public event Action<GridCoord, Transform> ChunkReleasing;` raised at the **start** of `Unregister(chunk)` (`:728`),
   **before** `FreeMeshes` and `Kill`, for drops, failed builds and live rebuilds. This needs the chunk's coord stored
   in `BuiltChunk`; `Transform` may be null for a failed build.
3. `public event Action<GridCoord, Transform> RoomDressed;` raised when `Furnish` (`:1458`) finishes one room (one
   room per frame), with the room's prop root.
4. `public int GetLitLamps(List<FrontRoomsLampSample> into);`, filled each frame from the fixture tick:
   `struct FrontRoomsLampSample { public Light light; public Renderer lens; public Color lensEmissionLinear; public bool shadowsOn; }`.
   `lensEmissionLinear` = the exact value the GPU sees, i.e. `(f.emission * level).linear` for what is passed to
   `MaterialPropertyBlock.SetColor` at `:1288`. Current intensity and colour come from `light`.
5. Keep `FrontRoomsMetalGlassRTController.Ensure()` and the pane `FrontRoomsMetalGlassTarget` (already merged). The
   destruction plan asks to move these to the visual side's render setup, and to put the target on the visible slab
   ([D3] §4.7). Later rename to a neutral `FrontRoomsGlassRT` when the Windows path lands.

**Glass track** (`FrontRooms/Glass`; the hook is built, and these are small adjustments):

6. Keep `_FR_GlassRTReflection` / `_FR_GlassRTWeight`, the `_FR_GLASS_RT` keyword, `_RTReceive` and the depth tag in A
   exactly as built. The RT pass writes that format.
7. Once the RT pass blurs by roughness (needs request 8), move the RT fade under smudges from perceptual roughness
   0.15 → 0.45 to ~0.45 → 0.7, so smudges blur the live reflection instead of swapping to the cube.
8. Optional, for the best result: a pass `Tags { "LightMode" = "FRGlassRTPrepass" }`. It writes world position + eye
   depth (RGBA32F) and the shader's final normal + smoothness (RGBA16F), with the same grime/roll/crack code and no
   blending.
9. Pane-strength Fresnel in the RT branch: F0 ≈ 0.078 (two surfaces, §5.1) instead of URP's 0.04. Also a far smaller
   `_RollStrength` (0.001-0.002 annealed, 0.003-0.004 tempered) when RT is on (§5.3).

**Destruction track:** the fracture contract in [D3] §4.7 stands as written. This design adopts it:
- index-range BLAS builds;
- ≤ 64 piece instances per window;
- a capacity of ≥ 1,024 instances;
- per-material flag bits;
- `Glass_Fracture` material;
- no VAT.

### 6.7 Keeping WebGL and other platforms provably untouched

- Put the whole native layer under `#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX`, and the later Windows layer under
  `UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN`. The WebGL build then does not even contain the P/Invokes. Excluding code
  from a platform does not change that platform's output.
- `AddRenderPasses` returns before enqueueing anything unless the controller is ready (it already does).
- The `_FR_GLASS_RT` variant is stripped from WebGL by the glass track's stripper. With the keyword off, the compiled
  glass code is exactly the code without the hook (per the hook's own comment, proven in the glass track's `logs/g14_hook_proof_*`).
- Give the dylib explicit importer settings: Editor (OSX) + Standalone OSX, ARM64, every other platform off. Today its
  `.meta` has only a `guid`.

### 6.8 Effort (ESTIMATE)

| Work | Days |
|---|---|
| Native plugin rewrite: render-thread queue, ring buffers, own geometry pool + heap, residency set + fences, BLAS lifecycle and compaction, Unity command buffer | 4-6 |
| Glass prepass + RenderGraph passes + persistent RTHandles + depth-tag output | 2 |
| Hit-shading parity (Surface world UVs, Lit UVs, texture array, lamps with URP falloff, shadow rays, linear lens emission, fog, print layer) + parity test | 3-4 |
| Anti-aliasing (ray cones, edge filter, adaptive supersampling) | 1-2 |
| Smudge blur + back-surface image | 1-2 |
| Destruction pieces (index-range builds, flags, masks) + dynamic registry | 1-2 |
| Map events (map chat) | 0.5 |
| Windows path on URT (needs a DX12 RTX/RX 6000 PC) | 4-6 |

---

## 7. What this changes in earlier documents

- **`11_reflections_and_raytracing.md`** §0.1, §1.2, §4.1 "Ray tracing: none possible on the Mac": superseded for
  desktop Mac through a native Metal plugin (this report and [D1]). Unchanged: URP has no built-in RT, HDRP RT is
  DX12-only, WebGL/WebGPU have none.
- **G11's planar camera for the held pane** (§2.4) stays as the fallback for non-RT desktops (including Windows until a
  DX12 RT PC exists) and as the reference image for validating the RT mirror. The Relay-only overlay stays the WebGL
  plan.
- **`20_verification.md` (G10)** §10 predicted that RT will not make face-on panes visible and wins at oblique,
  lamp-lens and dark-beyond views. This research agrees (§5.2) and turns it into the acceptance views (§5.4).
- **Revision 1 of this report**, three corrections:
  - URT's hardware backend uses ray-generation shaders, not inline queries;
  - defect 2 is not a defect in today's setup;
  - defect 6 is "partly right".
  Bench-1 numbers are unchanged.

---

## 8. Open items / UNVERIFIED

1. **M1/M2 speed.** Metal RT runs there without hardware traversal; how slow it is for this scene is unmeasured.
2. **Unity's Metal mesh buffers.** Can `GetNativeVertexBufferPtr` return a shared, sub-allocated buffer (non-zero
   offset)? The own-copy design avoids the question, but it matters if any prototype code is kept.
3. **Command ordering** of the prototype's separate command buffers relative to Unity's current one (defect 12): not
   tested; the production design removes it.
4. **In-engine cost.** Bench 2 covers the production shading shape in a standalone program. Unity's own GPU load, the
   prepass and the real meshes and textures are not in it. Measure in a player build with GPU timestamps.
5. **Lens luminance in the game.** Read back the lens `_EmissionColor` the GPU sees (expected ~9 linear, §5.2), and
   measure lens vs lit-wall radiance in a linear capture.
6. **The player's reflection.** No body mesh means no self-reflection in mirror-dark windows. Red decides; [D3] §3
   option (b) is one answer.
7. **Sources not read in full.**
   - Control's Ray Tracing Gems II chapter [G7] (the preprint is over the fetch limit) and the Lumen SIGGRAPH 2022
     slides [G11] (same).
   - Digital Foundry's BFV interview (page blocked). The BFV presets are now from HotHardware [G3].
   - Spider-Man 2's "ray-traced interiors" technique (only the PlayStation Blog wording) [G18].
   - The RE Engine slides were read through a summary of the Docswell page; no speaker name was given [G16].
8. **Windows.** Unity 6.3's default Windows graphics API; URT hardware-backend performance; no DX12 RTX PC available.
9. **1990 code requirements** for tempered vs annealed interior glass (also open in [D3]). Real roll-wave values for
   6 mm tempered glass beyond one fabricator's limit [P7].
10. **Adaptive AA thresholds** (the 4x luminance ratio, the 5-15 % edge share) are ESTIMATES; tune on the acceptance
    views (§5.4).
11. **Keyword policy, two documents disagree.** [D3] asks the glass shader for "No new keywords: uniform branches
    only, to protect the WebGL variant budget". The glass track's hook adds `multi_compile_fragment _ _FR_GLASS_RT`
    and strips it from WebGL builds, so the WebGL variant count does not change. That meets [D3]'s reason, but the
    glass and destruction tracks should agree on it explicitly.

---

## 9. Sources

**Apple**
- [A1] "Discover ray tracing with Metal", Sean James, Apple, WWDC20 session 10012, 2020. https://developer.apple.com/videos/play/wwdc2020/10012/
- [A2] "Enhance your app with Metal ray tracing", Juan Rodriguez Cuellar, Apple, WWDC21 session 10149, 2021. https://developer.apple.com/videos/play/wwdc2021/10149/
- [A3] "Explore hybrid rendering with Metal ray tracing", Ali de Jong and David Núñez Rubio, Apple, WWDC21 session 10150, 2021. https://developer.apple.com/videos/play/wwdc2021/10150/
- [A4] "Maximize your Metal ray tracing performance", Yi and Dominik ("GPU software engineers", surnames not given in the transcript), Apple, WWDC22 session 10105, 2022 (transcript re-read 2026-10-03). https://developer.apple.com/videos/play/wwdc2022/10105/
- [A5] "Go bindless with Metal 3", Alè Segovia Azapian and Mayur, Apple, WWDC22 session 10101, 2022. https://developer.apple.com/videos/play/wwdc2022/10101/
- [A6] "Your guide to Metal ray tracing", Pawel Szczerbuk, Apple, WWDC23 session 10128, 2023. https://developer.apple.com/videos/play/wwdc2023/10128/
- [A7] "Explore GPU advancements in M3 and A17 Pro", Jedd Haberstro (GPU, Graphics, and Displays Software), Apple tech talk 111375, 2023 (transcript re-read 2026-10-03). https://developer.apple.com/videos/play/tech-talks/111375/
- [A8] "Apple unveils M3, M3 Pro, and M3 Max, the most advanced chips for a personal computer", Apple Newsroom, 30 Oct 2023. https://www.apple.com/newsroom/2023/10/apple-unveils-m3-m3-pro-and-m3-max-the-most-advanced-chips-for-a-personal-computer/
- [A9] "Go further with Metal 4 games", Matias Koskela, Apple, WWDC25 session 211, 2025. https://developer.apple.com/videos/play/wwdc2025/211/
- [A10] "Build real-time neural rendering pipelines with Metal", Apple, WWDC26 session 359, 2026 (scope only). https://developer.apple.com/videos/play/wwdc2026/359/
- [A11] Apple developer documentation, read 2026-10-03: `MTLResidencySet` (macOS 15+) https://developer.apple.com/documentation/metal/mtlresidencyset ; `MTLAccelerationStructureUsage` https://developer.apple.com/documentation/metal/mtlaccelerationstructureusage ; `useResource(_:usage:)` https://developer.apple.com/documentation/metal/mtlcomputecommandencoder/useresource(_:usage:) ; `MTLInstanceAccelerationStructureDescriptor` https://developer.apple.com/documentation/metal/mtlinstanceaccelerationstructuredescriptor ; `supportsRaytracing` https://developer.apple.com/documentation/metal/mtldevice/supportsraytracing ; `MTLGPUFamily.apple9` https://developer.apple.com/documentation/metal/mtlgpufamily/apple9 ; `MTLFXTemporalDenoisedScaler` https://developer.apple.com/documentation/metalfx/mtlfxtemporaldenoisedscaler
- [A12] "Metal Feature Set Tables", Apple, revision of 21 May 2026. https://developer.apple.com/metal/Metal-Feature-Set-Tables.pdf
- [A13] Apple sample-code pages (page text read; code not downloaded): "Accelerating ray tracing using Metal" https://developer.apple.com/documentation/metal/accelerating-ray-tracing-using-metal ; "Rendering reflections in real time using ray tracing" https://developer.apple.com/documentation/metal/rendering-reflections-in-real-time-using-ray-tracing
- [A14] "Port advanced games to Apple platforms", Jacek Ratajewski and Alè, Apple, WWDC24 session 10089, 2024 (transcript read). https://developer.apple.com/videos/play/wwdc2024/10089/
- [A15] "How do I check programmatically if a device supports hardware Raytracing?", Apple Developer Forums thread 744941, answer by an Apple Graphics and Games Engineer, January 2024. https://developer.apple.com/forums/thread/744941

**Games and graphics research**
- [G1] "'It Just Works': Ray-Traced Reflections in 'Battlefield V'", Johannes Deligiannis and Jan Schmid, EA DICE, NVIDIA GTC 2019 session S91023 (slides read in full). https://developer.download.nvidia.com/video/gputechconf/gtc/2019/presentation/s91023-it-just-works-ray-traced-reflections-in-battlefield-v.pdf
- [G2] "Shiny Pixels and Beyond: Real-Time Raytracing at SEED", Johan Andersson and Colin Barré-Brisebois, EA SEED, GDC 2018 (slides read). https://media.contentapi.ea.com/content/dam/ea/seed/presentations/gdc2018-seed-shiny-pixels-and-beyond-real-time-raytracing-at-seed.pdf
- [G3] "NVIDIA GeForce RTX Ray Tracing In Battlefield V Explored Pre And Post Patch", HotHardware, 2018 (DXR preset smoothness thresholds and ray-count percentages, read 2026-10-03). https://hothardware.com/reviews/battlefield-v-ray-tracing-performance?page=2 . Background: "Battlefield 5's ray tracing: the DICE tech interview", Digital Foundry / Eurogamer with Yasin Uludağ (DICE), Nov 2018 (page blocked, not read). https://www.eurogamer.net/digitalfoundry-2018-battlefield-5-rtx-ray-tracing-analysis
- [G4] "Ray tracing comes to Battlefield 5", EA / DICE (Christian Holmquist), 2018. https://www.ea.com/games/battlefield/news/ray-tracing-comes-to-battlefield-5
- [G5] "Control: Multiple Stunning Ray-Traced Effects Raise The Bar For Game Graphics", NVIDIA GeForce news, 2019. https://www.nvidia.com/en-us/geforce/news/control-rtx-ray-tracing-dlss-out-now/
- [G6] "Control Graphics and Performance Guide", NVIDIA, 2019. https://www.nvidia.com/en-us/geforce/guides/control-graphics-and-performance-guide/
- [G7] "Ray Tracing in Control", Juha Sjöholm, Paula Jukarainen, Tatu Aalto (Remedy / NVIDIA), Ray Tracing Gems II ch. 46, Apress 2021 (preprint over the fetch limit; abstract and announcement read). https://developer.download.nvidia.com/ray-tracing-gems/rtg2-chapter46-preprint.pdf ; https://developer.nvidia.com/blog/free-ray-tracing-gems-ii-chapter-covers-ray-tracing-in-remedys-control
- [G8] "In-depth technical dive into Metro Exodus PC Enhanced Edition", 4A Games, 6 May 2021. https://www.4a-games.com.mt/4a-dna/in-depth-technical-dive-into-metro-exodus-pc-enhanced-edition
- [G9] Cyberpunk 2077, NVIDIA GeForce news, Andrew Burnes: "Cyberpunk 2077: Ray-Traced Effects Revealed, DLSS 2.0 Supported, Playable On GeForce NOW", 25 June 2020 https://www.nvidia.com/en-us/geforce/news/cyberpunk-2077-ray-tracing-dlss-geforce-now-screenshots-trailer/ ; "Cyberpunk 2077 Available Now With Stunning Ray-Traced Effects and Performance Accelerating NVIDIA DLSS", 9 Dec 2020 https://www.nvidia.com/en-au/geforce/news/cyberpunk-2077-rtx-dlss-out-now/
- [G10] "Lumen Global Illumination and Reflections in Unreal Engine" and "Lumen Technical Details", Epic Games documentation (UE 5.x, re-read 2026-10-03). https://dev.epicgames.com/documentation/en-us/unreal-engine/lumen-global-illumination-and-reflections-in-unreal-engine ; https://dev.epicgames.com/documentation/en-us/unreal-engine/lumen-technical-details-in-unreal-engine
- [G11] "Lumen: Real-time Global Illumination in Unreal Engine 5", Daniel Wright, Krzysztof Narkowicz, Patrick Kelly, SIGGRAPH 2022 Advances in Real-Time Rendering (not read; file over the fetch limit). https://advances.realtimerendering.com/s2022/SIGGRAPH2022-Advances-Lumen-Wright%20et%20al.pdf
- [G12] HDRP 17.3, Screen Space Reflection override (ray-traced properties) and "Ray-traced reflections", Unity. https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.3/manual/reference-screen-space-reflection.html ; https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.3/manual/Ray-Traced-Reflections.html . [G12b] "Recursive rendering", HDRP 17.3, Unity. https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.3/manual/Ray-Tracing-Recursive-Rendering.html
- [G13] "AMD FidelityFX Hybrid Reflections", AMD GPUOpen, SDK 1.0 2023 (page v1.1.4, 2025). https://gpuopen.com/fidelityfx-hybrid-reflections/
- [G14] "NRD: NVIDIA Real-time Denoisers" README, NVIDIA. https://github.com/NVIDIA-RTX/NRD
- [G15] "Spatiotemporal Variance-Guided Filtering: Real-Time Reconstruction for Path-Traced Global Illumination", Christoph Schied et al., NVIDIA / KIT, High Performance Graphics 2017. https://research.nvidia.com/publication/2017-07_spatiotemporal-variance-guided-filtering-real-time-reconstruction-path-traced
- [G16] "Advances in Ray Tracing" (English edition), Capcom R&D, CAPCOM Open Conference Professional RE:2023, 27 Nov 2023 (RE ENGINE; speaker not named on the page; read through a summary of the slide page). https://docswell.com/s/CAPCOM_RandD/K24Y66-RE2023
- [G17] "Spider-Man: Miles Morales on PS5 has added a 60fps ray-tracing mode", Andy Robinson, Video Games Chronicle, 9 Dec 2020 (paraphrasing Insomniac's mode description). https://www.videogameschronicle.com/news/spider-man-miles-morales-on-ps5-has-added-a-60fps-ray-tracing-mode/
- [G18] "Marvel's Spider-Man 2 PC features and ray-tracing options detailed, out tomorrow", Julian Huijbregts (Communications Manager, Nixxes), PlayStation Blog, 29 Jan 2025. https://blog.playstation.com/?p=400768
- [G19] "Texture Level of Detail Strategies for Real-Time Ray Tracing", Tomas Akenine-Möller, Jim Nilsson, Magnus Andersson, Colin Barré-Brisebois, Robert Toth, Tero Karras (NVIDIA / EA SEED), Ray Tracing Gems ch. 20, Apress 2019 (abstract read). https://www.ea.com/seed/news/texture-level-of-detail-strategies-for-real-time-ray-tracing ; https://research.nvidia.com/publication/2019-03_Texture-Level-of
- [G20] "Shading Models in Unreal Engine" (Thin Translucent), Epic Games documentation. https://dev.epicgames.com/documentation/en-us/unreal-engine/shading-models-in-unreal-engine
- [G21] "Fast approximate anti-aliasing", Wikipedia (FXAA, Timothy Lottes, NVIDIA; whitepaper http://developer.download.nvidia.com/assets/gamedev/files/sdk/11/FXAA_WhitePaper.pdf, not read). https://en.wikipedia.org/wiki/Fast_approximate_anti-aliasing . Survey: "Filtering Approaches for Real-Time Anti-Aliasing", Jorge Jimenez et al., SIGGRAPH 2011 course (not read). https://www.iryoku.com/aacourse/downloads/Filtering-Approaches-for-Real-Time-Anti-Aliasing.pdf

**Unity**
- [U1] Unity 6000.3.10f1 offline documentation (`/Applications/Unity/Hub/Editor/6000.3.10f1/Documentation/en/`): ScriptReference `Rendering.RayTracingAccelerationStructure` (description re-read 2026-10-03), `.AddInstance`, `.CullInstances`, `.UpdateInstanceTransform`, `Rendering.CommandBuffer.BuildRayTracingAccelerationStructure`, `SystemInfo-supportsInlineRayTracing`, `SystemInfo-supportsRayTracing`, `SystemInfo-supportsRayTracingShaders`, `Mesh.GetNativeVertexBufferPtr`, `Mesh.GetNativeIndexBufferPtr`, `Mesh.GetVertexBuffer`, `Mesh-vertexBufferTarget`, `SkinnedMeshRenderer.GetVertexBuffer`, `SkinnedMeshRenderer-vertexBufferTarget`, `GraphicsBuffer.GetNativeBufferPtr`, `Texture.GetNativeTexturePtr`, `GL.IssuePluginEvent`, `Rendering.CommandBuffer.IssuePluginEventAndData`, `Material.SetColor`, `MaterialPropertyBlock.SetColor`; Manual `SL-Pragma-require`, `WhatsNew20231`, `WhatsNewUnity63`, `low-level-native-plugin-rendering-extensions`.
- [U2] Installed packages in the clone `proj_rt`: `com.unity.render-pipelines.core@04ab0eefa0c3` `Runtime/RenderGraph/RenderGraph.cs:1268`, `RenderGraphBuilder.cs:146-163`, `Runtime/CommandBuffers/ComputeCommandBuffer.cs:558-600`, `Runtime/UnifiedRayTracing/IRayTracingAccelStruct.cs:8-66`, `CommonStructs.hlsl:28-34`, `FetchGeometry.hlsl:65-116`, `RayTracingContext.cs:13-129`, `Hardware/HardwareRayTracingBackend.cs:10-13`, `Hardware/HardwareRayTracingShader.cs:6-116`.
- [U3] `IUnityGraphicsMetal.h`, Unity Native Plugin API, 6000.3.10f1 (`Unity.app/Contents/PluginAPI/`, lines 32-49, 73-80).

**Physics and glass**
- [P1] "Fresnel equations", Wikipedia (read 2026-10-03). https://en.wikipedia.org/wiki/Fresnel_equations
- [P2] "One-way mirror", Wikipedia. https://en.wikipedia.org/wiki/One-way_mirror
- [P3] "Tempered glass", Wikipedia. https://en.wikipedia.org/wiki/Tempered_glass
- [P4] "Float glass", Wikipedia. https://en.wikipedia.org/wiki/Float_glass
- [P5] "Orders of magnitude (luminance)", Wikipedia (fluorescent lamp "12 kcd/m2", re-read 2026-10-03). https://en.wikipedia.org/wiki/Orders_of_magnitude_(luminance)
- [P6] "Methods of Measuring Optical Distortion in Heat-Treated Flat Architectural Glass", Glass Technical Paper FB18-08, National Glass Association (NGA), 2023 (first published 2008; read in full). https://glass.org/sites/default/files/2023-06/FB18-08_2023_Methods_Measuring_Optical_Distortion_Heat-Treated_Flat_Glass.pdf
- [P7] "Distortion in Heat-Treated Glass: Understanding Roller Wave, Edge Lift, and Bow Across Severity Levels", AGNORA (fabricator), undated web article (one fabricator's specification; read 2026-10-03). https://agnora.com/distortion-in-heat-treated-glass-understanding-roller-wave-edge-lift-and-bow/

**Project documents**
- [D1] `Documentation/research/glass/rt/01_code_review.md` (G14 review stage, 2026-10-03) and `rt/images/01_review_trace_output.jpg`.
- [D2] `Documentation/research/glass/20_verification.md` (G10 in-engine verification, 2026-10-03), §0, §3.3, §9, §10.
- [D3] `Documentation/research/glass/destruction/10_glass_destruction_plan.md` (2026-10-03), §0, §3, §4.7, §4.8.

**Project files read** (real project at `a3615e3`, read-only; the private clones for package source and the glass
hook): `NativePlugin/FrontRoomsMetalGlassRT.mm`, `NativePlugin/build_frontrooms_metal_glass_rt.sh`,
`Assets/Scripts/Rendering/FrontRoomsMetalGlassRT.cs`, `FrontRoomsMetalGlassRTRendererFeature.cs`,
`Assets/Shaders/FrontRoomsMetalGlassRTComposite.shader`, `Assets/Plugins/macOS/libFrontRoomsMetalGlassRT.dylib.meta`,
`Assets/Settings/FrontRooms_URP_Renderer.asset:95`, `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs` (`:38, 72-91,
251, 294-295, 342-347, 537, 728, 772, 884, 1049, 1181-1215, 1256-1336, 1458, 2043-2059`),
`FrontRoomsMapWalker.cs:68`, `FrontRoomsLevelProfile.cs:37-39`, `FrontRooms3DGame.cs:247`,
`Assets/Scripts/Rendering/FrontRoomsLook.cs:20-50`, `FrontRoomsSurfaces.cs:91-97`,
`Assets/Resources/Rendering/FrontRoomsSurface.shader:1-80, 157-197, 252`, `ProjectSettings/ProjectSettings.asset:50, 54`,
`Tools/print/unity_staging/Assets/Scripts/Rendering/FrontRoomsPrintDriver.cs:1-40`; the glass track's
`proj_glass/Assets/Resources/Rendering/FrontRoomsGlass.shader` (re-read 2026-10-03 at 11:50, 498 lines;
the file is under active edit, so this report cites its `[G14-HOOK]` markers, not line numbers) and
`proj_glass/Assets/Editor/Rendering/FrontRoomsGlassRTStripper.cs` (exists).

**Measurements**: `03_bench/` — bench 1 (`rt_bench.mm.txt`, `bench_run1.txt`, `bench_run2_threadgroups.txt`) and bench 2
(`rt_bench2.mm.txt`, `bench2_production_shading.txt`). Standalone Metal on the M3 Max, macOS 26.6.2, with other Unity
batch processes using the GPU at the same time. No frames were captured for this report; the images in `rt/images/` belong to the review
and probe stages of this workflow.
