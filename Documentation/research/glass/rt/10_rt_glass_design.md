# 10 — Ray-traced glass reflections: production design (G14)

2026-10-03 · visual chat (游戏视觉) · workflow glass-rt-track, design stage · **status: DESIGN** (no code changed)

**Inputs, read in full:**
- `01_code_review.md` (R1–R32), `02_runtime_probe.md` (runs 7–8), `03_research.md` (revision 2, benches 1–2).
- `../destruction/10_glass_destruction_plan.md` §4.4, §4.7 (the fracture contract), §5.2 steps 11–12; `../destruction/04_unity_implementation.md` §1.3, §4, §7 and the G13 row of §12 (RT1–RT9).
- `../../webgl/10_webgl_plan.md` §2.7 (the shared map API), §3.1–3.2 (the Web URP asset), §7.4 (R1–R4 isolation).
- The glass track's clone `proj_glass`: `FrontRoomsGlass.shader` (the five `[G14-HOOK]` blocks), `FrontRoomsGlassRTStripper.cs`, `FrontRoomsZoneReflection.cs`, `Glass_Window.mat`.
- Red's working copy, read only: `FrontRoomsMapWorld.cs`, `FrontRooms3DGame.cs`, `FrontRoomsSurface.shader`, `FrontRoomsLook.cs`, `FrontRoomsPostStack.cs`, `FrontRooms_URP.asset`, `FrontRooms_URP_Renderer.asset`.
- URP 17.3 source in `proj_rt/Library/PackageCache`.

**Line numbers.** The map chat is editing `FrontRoomsMapWorld.cs` right now; it was 2,641 lines at 12:21. MapWorld line numbers below come from that copy, and every hook point is also named by function. ChatGPT's RT files in Red's project still match the baseline hashes (`.mm` 8d69973a, dylib 4bf02a4c). `proj_rt` holds the probe stage's instrumented `.mm` and dylib (beb93f8d).

**Tags.**
- **MEASURED** = timed on Red's M3 Max (02 in-engine, or 03's standalone benches).
- **ESTIMATE** = arithmetic or judgement.
- **UNVERIFIED** = not confirmed.
- **R-numbers** = findings in 01.
- **RT1–RT9** = the destruction plan's G13 row.

---

## 0. For Red

1. **What ChatGPT built.** A native Metal plugin that reaches the real hardware ray tracing in your M3 Max, which Unity itself reports as "not available". Every second, a controller rebuilds a small ray-traced copy of the nearby scene. A pass then paints the traced reflection over the finished frame.
2. **What works.** The plumbing. Unity loads the plugin, Metal reports hardware ray tracing, and with fixes the trace lines up with the real pane to within 1 px. Tracing is cheap: about 1–2 ms per frame at 1080p (MEASURED).
3. **What is wrong.**
   - As committed, it has never drawn a pixel. Five stacked blockers stop it, and forcing it on crashes Unity.
   - Once patched:
     - the reflection is drawn at 0.58× size;
     - it is an opaque dark mirror pasted after the colour grade, which hides the view through the window;
     - 16–20 % of a head-on pane shows blue sky indoors;
     - it paints over anything in front of the glass;
     - it costs a 45–105 ms hitch, plus one blank frame, every second;
     - shading is flat: no textures, no lamps.
4. **What we will build.**
   - Rays start from the glass pixels the game actually drew, one mirror ray per pixel.
   - Each hit is shaded the way the game shades that surface: the same textures, the lamps that are on, the troffer lenses flickering in sync, and fog. The zone cube is used only where nothing is loaded.
   - The result goes into the glass shader before tonemapping. The glass applies its own Fresnel and grime, and the reflection is graded and bloomed with the scene.
   - The scene copy is updated incrementally on the render thread: no rescans, no waits.
5. **What you will see.**
   - The real room behind you in every window: textured carpet and wallpaper, lit lenses flickering, doors swinging, the Relay walking behind you.
   - Face-on in lit rooms it stays faint, as real glass does (+2–4/255, G10). It is strong at 45–65° and in windows with a dark room beyond, like a one-way mirror.
   - The view through the glass stays. No hitch, no flicker.
   - Broken glass reflects in facets (P4).
6. **Where it runs.** Macs with M3/M4 (Apple9 GPU) only, as a setting: Off / High / Ultra.
   - Windows keeps the cube/planar reflection until a DX12 RTX PC exists (P3).
   - WebGL never contains the code, and its output does not change.
7. **What it costs** (ESTIMATES built on MEASURED benches, §3).
   - GPU per frame at 1440p with a window in view: 1.3–2.0 ms (High), 2.2–3.4 ms (Ultra).
   - When a pane fills the screen (the break shot): 3.5–5.2 ms (High).
   - About 125–160 MB of memory at 1440p.
   - At most 0.3 ms of main thread per frame.
   - P0 + P1 take about 11–16 working days.
8. **What I need from others.**
   - **Map chat:** the chunk, room and camera hooks already agreed with the WebGL plan, a lamp-state query, and deleting ChatGPT's two lines (§2.4).
   - **Glass track:** one extra shader pass and two small hook globals (§2.1).
   - **You:** the decisions in §5 (default quality, a body in reflections, the Metal toolchain download).

---

## 1. Architecture

### 1.1 Platform paths

| Platform | Path | Status |
|---|---|---|
| macOS desktop, Apple9 (M3/M4) | Native Metal plugin (`libFrontRoomsMetalGlassRT.dylib`), hardware intersector, encoded into Unity's own command buffer | P0–P2, P4 |
| macOS M1/M2 (Apple7/8) | Same plugin. `supportsRaytracing` is true, but traversal runs on shader cores. Off by default; P2 measures a half-resolution tier | P2 |
| Windows DX12, DXR GPU (RTX 20 / RX 6000+) | Unity `RayTracingAccelerationStructure` plus the UnifiedRayTracing hardware backend (`DispatchRays`). It writes the same `_FR_GlassRTReflection` | P3 (needs a test PC) |
| Windows without DXR, every other desktop | Zone cube, plus G11's planar reflection for the held pane | unchanged |
| WebGL | Zone cube, plus the Relay-only overlay. No RT code is compiled; the RT variants and passes are stripped | unchanged |

**Names stay as they are**, so the WebGL plan's pre- and post-flight checks keep working:
- the dylib `libFrontRoomsMetalGlassRT.dylib`;
- the native prefix `FRGlassRT_`;
- the feature class `FrontRoomsMetalGlassRTRendererFeature` and its guid `8c7c221f…`.

### 1.2 Frame order (desktop Mac, per RT camera)

| # | RenderGraph step | Event | What happens |
|---|---|---|---|
| 1 | URP DepthNormals prepass (SSAO) | prepass | Writes `_CameraDepthTexture`. If SSAO is off, URP moves its depth copy to after opaques, because our pass declares a Depth input (`UniversalRendererRenderGraph.cs:917-922, 954-957`) |
| 2 | Opaques, skybox | — | unchanged |
| 3 | **FRGlassRT Prepass** (raster) | `BeforeRenderingTransparents` | Draws only the receiving glass (§1.3) into `GlassDepth` (R32F, linear eye depth) and `GlassNormal` (RGBA16F: world normal + perceptual smoothness). A transient 1× D32 keeps the front-most layer, and a manual test against `_CameraDepthTexture` keeps only glass that is actually visible |
| 4 | **FRGlassRT Trace** (unsafe pass → `IssuePluginEventAndData`) | same | Native render-thread event, steps (a)–(h) below. Then sets `_FR_GlassRTReflection` and `_FR_GlassRTWeight`, and enables the global keyword `_FR_GLASS_RT` for this camera |
| 5 | Transparents | — | FrontRooms/Glass replaces its environment term with the RT radiance, still weighted by its own Fresnel and grime, then fogs. The prototype's composite is deleted (R7) |
| 6 | **FRGlassRT End** (unsafe) | `AfterRenderingTransparents` | Disables `_FR_GLASS_RT` and sets weight 0, so no other camera samples a stale texture |
| 7 | Post (ACES, grade, halation, grain) | — | Reflections are graded and bloomed with the scene |

**Native event, in Unity's current command buffer** (R16). It starts with `EndCurrentCommandEncoder()` and `CurrentCommandBuffer()`, and never commits:
- (a) Apply queued scene ops: mesh uploads, deferred releases.
- (b) Acceleration-structure encoder: budgeted BLAS builds, refits, compaction copies, and this frame's TLAS build. It updates `MTLFence F`.
- (c) Barrier against Unity's prepass writes: `encodeSignalEvent` then `encodeWaitForEvent` on one `MTLEvent`, on the command buffer between encoders. This works whatever hazard mode Unity's textures use. P0 logs their `hazardTrackingMode`; if all are tracked, this barrier is dropped.
- (d) Compute encoder: wait on F. The residency set is attached to the command buffer. Trace kernel → raw texture.
- (e) Compute: resolve kernel → `_FR_GlassRTReflection`. It does edge AA, the back-surface image, the smudge blur and a 1 px dilation.
- (f) The same event barrier again, so Unity's transparent draw sees our writes.
- (g) GPU timestamps from `MTLCounterSampleBuffer` at encoder boundaries.
- (h) `addCompletedHandler`: publish the "GPU done" frame serial, compacted sizes, timings and command-buffer errors. On any GPU error, RT fails closed.

**Cost of the split (UNVERIFIED, measured in P0).** The RenderGraph compiler may currently merge opaques, skybox and transparents into one native render pass, keeping 4× MSAA colour and depth in tile memory. Steps 3–4 split that pass. The split costs one store and reload of the MSAA attachments: about 236 MB of traffic at 1440p, 0.3–0.8 ms (ESTIMATE). P0 measures an empty unsafe pass at this event.
- If the split costs more than 0.3 ms, fallback F1 is evaluated. F1 traces after transparents, and an additive `FRGlassRTAdd` pass of FrontRooms/Glass applies the same hook math (`+EnvBRDFSpec·w·(rt − env)`). Post already ends the pass there, so the split is free.

### 1.3 Which pixels trace

- **Receivers** are enabled, visible renderers whose material has `_RTReceive = 1`: FrontRooms/Glass `Glass_Window`, and later `Glass_Fracture`. This is the **visible** glass:
  - today's pane cube once the map switches it to `Glass_Window`;
  - the 6 mm slab (`interactables/06` §4.3);
  - the destruction track's stage meshes and pieces.

  A disabled gameplay pane collider is never drawn, so it can never be a target (§4.7). The `FrontRoomsMetalGlassTarget` marker is only a hint: a marked renderer without an RT-capable material is skipped, with one warning.
- **Prepass drawing.**
  - **P0:** a `RendererList` filtered by one rendering-layer bit (bit 30, set at runtime on registered receivers; no project-setting change), drawn with the override shader `Hidden/FrontRooms/GlassRTPrepass`. It writes the geometric normal and the material's clean smoothness.
  - **P1:** the glass shader's own `FRGlassRTPrepass` pass (contract G-1). The normal then includes roll, smudge, bow and crack-facet normals, and the smoothness is per pixel.
- **No primary rays.** The ray origin is rebuilt from `GlassDepth` with `inverse(GL.GetGPUProjectionMatrix(P, true) × V)`. This is the same matrix the prepass rasterised with, using Metal's convention that row 0 is NDC +1. That fixes R6, R9 and suspect 2 together.
  - Validation build only: the prepass also writes world position (RGBA32F), and the error must be ≤ 1 mm at 10 m.
  - Ray origin = surface + 2 mm along the reflected side, so a 6 mm slab never hits its own back face.
- **Resolution and MSAA.**
  - The prepass and trace run at 1× the camera's scaled target size (render scale 1 = native), one sample per pixel.
  - The glass shader runs per pixel and samples the RT texture at the pixel centre; 4× MSAA still smooths the pane's silhouette.
  - Edge pixels whose centre misses the pane get the neighbour's value through the 1 px dilation. The depth tag stops bleeding onto other layers.
  - **Full resolution on High and Ultra.** Half resolution (one ray per 2×2, depth-aware upsample) only on the M1/M2 tier (P2).
- **Dispatch rectangle.** The CPU projects the bounds of visible receivers and dispatches only that screen rectangle. When no receiver is visible (the title corridor, most corridors), nothing is enqueued and the weight is 0. P2 moves this to an indirect dispatch over 8×8 glass tiles.
- **Front-most glass layer only** (Lumen's "front layer"). A pane seen through a pane keeps its zone cube; the hook's depth tag enforces this.

### 1.4 Scene management

**Data ownership: the plugin owns copies; no BLAS ever points into Unity's buffers** (R26, suspect 6).

| Data | Source | When |
|---|---|---|
| Positions, normals, UV0, vertex colour R (fracture edge), 32-bit indices, per submesh | **Readable meshes** (every MapWorld mesh: `MeshBuilder.ToMesh` never calls `UploadMeshData`; the fracture generator's meshes): `Mesh.AcquireReadOnlyMeshData` on the main thread, copied into a new shared `MTLBuffer` (no render-thread sync). **Non-readable meshes** (the 54 kit FBX, `isReadable: 0`): `GetNativeVertexBufferPtr`/`IndexBufferPtr` **once per Mesh per session**, then a GPU gather kernel copies into the pool on the render thread (handles stride, offset, base vertex, 16/32-bit) | At registration |
| BLAS | One per Mesh (key = instance ID + change counter); one geometry descriptor per submesh (R14); built from our pool; compacted a few frames later (bench 1: 46 % of built size) | Built on the render thread, budget ≤ 50k triangles per frame (~0.6–1.0 ms) |
| Material record | Read once per material: shader kind, colours, tiling, grime parameters, flags, texture `gpuResourceID`s (native texture pointer fetched once per texture) | At registration; again on `LiveApplied` |
| Instance record (96 B, shared C++/MSL header, `static_assert`) | Transform rows, geometry base, material base, flags, window id, emission (linear, per frame), fixture id, ceiling height (read once from the MPB `_CeilingHeight`) | Static fields at registration; dynamic fields per frame |

**Registration sources.**
- **P0 (no map edits):** a cheap per-frame diff.
  - Find the map once: `FindAnyObjectByType<FrontRoomsMapWorld>`, retried at most once per second while none is known.
  - Each frame, compare the child list of `map.transform` (the chunk roots, about 25–49) and each chunk root's `childCount` against the previous frame.
  - A new or changed chunk is rescanned incrementally (diff by renderer ID), sliced to ≤ 1 ms per frame. A chunk is also rescanned once 1 s after it appears, to catch dressing.
- **P2:** the map events (`ChunkBuilt` / `ChunkReleasing` / `RoomDressed`, §2.4) replace the diff.
- **Relay:** its 16 rigid MeshRenderers are found once when the map is found, and registered as Dynamic. No refit is needed (02).
- **Doors and keys:** registered as Dynamic.

**Registration rules.**
- Skip renderers that are disabled, `forceRenderingOff`, `ShadowsOnly`, `isPartOfStaticBatch` (R32), particle or trail renderers, or outside the camera's culling mask.
- Under a LODGroup, register LOD0 only (highest spec; all kit meshes together are ~6.9 MB of FBX, ESTIMATE ≤ 150k triangles).
- No `FindObjectsByType` over the scene, and no 1 s rescans.

**Instance flags and masks** (R15; one record per instance, so every fracture piece carries its own).

| Flag bit | Meaning | Instance mask (8 bit) |
|---|---|---|
| 0 Glass | Receiver. In reflection rays it is see-through (×(1−F)·0.9, at most 1 layer on High, 2 on Ultra) | 0x02 GLASS |
| 1 FractureEdge | Use vertex colour R as the green edge mask at hits | — |
| 2 Shard | A small piece (< 4 cm²). Excluded from reflection rays; it still gets reflections on its own pixels | 0x04 SHARD |
| 3 EmissiveLens | Emission from the per-frame lamp table | 0x01 OPAQUE |
| 4 Dynamic | Transform read every frame | 0x08 DYNAMIC |
| 5 Relay · 6 Door · 7 MeshUV · 8 SurfacePlanar | Shading selectors | 0x01 / 0x08 |
| 9 Deforming | BLAS refit when announced | 0x08 |
| 10 ReflectionOnly | Seen only by reflection rays (a possible player body, §5 D2) | 0x10 |

Ray masks:
- Reflection rays use 0x01 | 0x02 | 0x08 | 0x10.
- Shadow rays use 0x01 | 0x08. Glass casts no shadow, matching the raster, where panes have shadows off.

**TLAS policy: around the camera, not around "the nearest pane"** (R12, R13, N9).
- Candidates: every registered instance whose bounds (`bounds.SqrDistance`, never the centre) are within `camera.farClipPlane` (80 m today; it follows the build radius through `LiveApplied`).
- Priority: (0) receivers, fracture pieces, dynamic instances and the Relay; (1) everything else by distance.
- Cap: High 2,048, Ultra 4,096; capacity ≥ 1,024 is the destruction contract's minimum. MEASURED TLAS build: 0.29–0.30 ms at 1,792 and 0.52–0.53 ms at 4,096.
- P0 rebuilds the TLAS every frame on the render thread, with a ring of 3 TLAS and scratch buffers.
- P2 splits it: a static TLAS rebuilt only when membership changes, plus a per-frame dynamic TLAS joined by two-level instancing; or it refits frames where membership is unchanged.
- **World rebase** (192 m, `ModuleUnits.WorldPeriod`): the map root's position is compared each frame. On a change, all static matrices are rewritten (rare); otherwise only dynamic ones.
- **Ray length** = `camera.farClipPlane`. Bench 1 found no cost difference between 30 m and 10 km. A miss then means only "nothing built there".

**Residency and synchronisation** (R8, R25).
- **macOS ≥ 15:** one `MTLResidencySet` holds the BLAS heap, geometry pool, instance, material and lamp tables, and every bindless texture. It is attached to Unity's command buffer each frame and touched only on the render thread.
- **macOS 13–14:** `useHeap` / `useResource` for the same resources on our compute encoder.
- `MTLFence` between our AS encoder and our compute encoder; the event barrier against Unity's encoders (§1.2 c/f).

**Threading and lifetime** (R10, R24, R25, R27).

| Rule | How |
|---|---|
| Main thread never waits on the GPU | No `waitUntilCompleted` anywhere (grep test). If a ring slot is not free yet, that frame reuses the previous TLAS and counts a skip |
| Main → render thread | A mutex-protected op queue (add/release mesh, set material, reset), plus a **3-slot ring** of per-frame data in shared `MTLBuffer`s: instance descriptors, records, lamps, constants. C# writes the slot directly through a `NativeArray` over the native pointer |
| Render thread owns Metal state | Only the render event touches the scene, the residency set and the encoders. C# never reads plugin globals |
| Generation-safe release | Released meshes, BLASes and textures go on a retire list tagged with the frame serial. They are freed only after the completion handler reports that serial done (R25) |
| Per-camera event data | 3 native blocks per camera, freed 3 frames after the last use (R27). Native texture pointers are fetched once per (re)creation (R23) |
| Same-frame removal | Every frame, each receiver and dynamic instance is checked: `renderer == null`, `!enabled` or `!activeInHierarchy` drops it from **this** frame's TLAS. The existing `GlassBroken(Vector3)` drops receivers within 1 m at once. A broken pane leaves the AS on the frame it disappears from the raster (§4.7) |
| Mesh edits | Map meshes are never edited in place: live rebuilds create new meshes and go through `Unregister`. Anything that edits a registered mesh calls `FrontRoomsGlassRT.MeshChanged(mesh)` (rebuild) or is flagged Deforming (refit) |
| Device shutdown, domain reload | The device event drops everything and marks the system not ready. `OnDisable` enqueues `ResetScene` (generation-safe), and the C# side re-registers after the reload |

### 1.5 Hit shading (one bounce; must match the raster, BFV's "Shader output must match!")

| Surface hit | Shading at the hit |
|---|---|
| **FrontRooms/Surface** (walls, floors, ceilings, Office louver lens) | Rebuild exactly what `FrontRoomsSurface.shader` `Frag` does:<br>– `PlanarFrame(positionWS, geometric normal)` → metres / `_TileSize` × `_BaseMap_ST`, or mesh UV0 when `_FR_MESH_UV`;<br>– `_BaseMap` × `_BaseColor`, `_MaskMap` (smoothness, cavity), `_BumpMap` in the planar TBN;<br>– macro wear at 8 m and 12.8 m (tone, dirt), stains, damp;<br>– floor grime and ceiling streaks, using this instance's `_CeilingHeight`;<br>– `_EMISSION` × `_EmissionMap`.<br>The source is `FrontRoomsSurface.shader` `PlanarFrame` and `Frag` (lines 157–252 in Red's copy) |
| **URP/Lit** (kit props, door leaf, keys, Relay parts, Level 0 lens) | UV0 from barycentrics; `_BaseMap` × `_BaseColor`, metallic/smoothness, normal map, `_EmissionMap` × `_EmissionColor` |
| **Troffer lens** | Emission = this fixture's current value, **linear**. `MaterialPropertyBlock.SetColor` converts sRGB to linear on the way to the GPU, while `GetColor` returns the value as passed: MapWorld's own `CheckLampStatesForTools` compares it with the unconverted `f.emission × factor`. So the RT side applies `.linear`: (1, .96, .84) × 2.6 → ≈ 9.2 linear (P1 parity test). This includes flicker, dimming and dead lamps |
| **Another pane** | See-through: continue the ray ×(1 − F(θ))·0.9, plus F × the zone cube. At most 1 layer (High) or 2 (Ultra) |
| **Lights** | The lights enabled this frame (the map lights within `lightRadius` 16 m; ~62 at the probe's pane), cap 128, brute force with a range reject.<br>– URP falloff exactly: `DistanceAttenuation` (`RealtimeLights.hlsl:47-58`) and `AngleAttenuation` (`:62-74`); colour = `light.color.linear × intensity` (URP sets `lightsUseLinearIntensity`).<br>– Lambert diffuse plus GGX specular, as URP's BRDF.<br>– The directional fill (intensity 0.22, soft shadows at strength 0.18, `FrontRooms3DGame.ApplySceneLighting`) is included too |
| **Shadows** | Shadow rays only for lights whose `shadows != None` **this frame**: about one lamp in three within `shadowRadius` 9 m, as in the raster. Unshadowed lamps stay unshadowed. Strength = `light.shadowStrength` (0.92).<br>– **High:** the 4 nearest shadowed lamps; the fill gets a constant factor of 0.82.<br>– **Ultra:** all shadowed lamps, plus a fill shadow ray |
| **Ambient** | SH from `RenderSettings.ambientProbe` (Trilight), as URP `SampleSH`, × cavity. Specular ambient: the zone cube at the hit's roughness mip × the zone intensity. No SSAO at hits (a known difference; parity runs with SSAO off) |
| **Fog** | Exp² as URP (`ShaderVariablesFunctions.hlsl:366-371`), with `unity_FogParams.x` and the linear `unity_FogColor`. The glass shader already fogs the camera→glass distance d1. The RT radiance carries the remainder, `f2 = exp2(−k²((d1+d2)² − d1²))`, so the reflected object gets exactly the fog of its full path length |
| **Miss** | Zone cube (`RenderSettings.customReflectionTexture`, the Blend cube while a fade runs) in the ray direction × the zone's linear intensity, with coverage 1. The hook's `rt − env` then cancels exactly |
| **Not traced** | Particles, volumetric lamp beams (transparent), decals |
| **Moving wallpaper** (when it lands) | Read `_FR_Print`, `_FR_PrintClock`, `_FR_PrintWarp` once per frame, so the reflected print frame is identical (§5 D3) |
| **Texture filtering** | Mip from a ray cone (camera-pixel spread at the glass + distance; bench 2: +5–20 %) |

**Parity mode** (debug; BFV's "Verifying correctness"). Primary rays traced from the camera through the same hit shader, compared per pixel with the raster frame (post off, SSAO off). Every difference beyond tolerance is a hit-shading bug. It is P1's main acceptance test.

### 1.6 Thin glass, smudges and anti-aliasing (no TAA, no temporal accumulation)

- **Pane strength.** The glass shader already uses `_PaneF0` 0.08, the two surfaces of 6 mm glass (8.2 % face-on, 15.7 % at 60°). 03's request for F0 ≈ 0.078 is therefore already met. The RT term is radiance only: no Fresnel, no strength (R19).
- **Back-surface image** (High and Ultra; ~0.02 ms ESTIMATE).
  - In the resolve: `L = wf·L(x) + wb·L(x + Δ)`, with wf, wb the front and back Fresnel shares at θ (n = 1.52; 0.52/0.48 face-on).
  - Δ is the screen projection of 2·t·tan θt·cos θi, t = 6 mm, along the pane's in-plane normal tilt, within the same depth tag.
  - It shows only within ~1 m (2–4 px at 1440p), as 03 §5.1 computed.
  - The 30 mm prototype cube is never used as thickness: only the front face is drawn into the prepass.
- **Smudges** (P1, needs the glass shader's prepass smoothness).
  - Deterministic blur in the resolve. Radius from roughness, hit distance and view distance (BFV's {angle, roughness} rule). Bilateral on the depth tag, so panes never bleed into each other.
  - The hook's fade under grime then moves from roughness 0.15→0.45 to 0.45→0.70 (global `_FR_GlassRTFade`, G-2). Smudges blur the live room instead of swapping to the static cube.
- **Roll wave.** Annealed panes have none. `_RollStrength` 0.02 swings a true-parallax reflection by 2.3°, 5–15× the tempered limit. With RT on, the hook scales it by the global `_FR_GlassRTRollScale` (0.075 → effective 0.0015; G-3). The forward and prepass normals use the same value.
- **Anti-aliasing.** MSAA smooths the pane's edge, not what it reflects, and a 1-ray mirror is point-sampled.
  - **High:** ray-cone texture mips, plus an FXAA-style luminance-edge filter on the RT buffer inside one depth tag (~0.1 ms ESTIMATE).
  - **Ultra:** additionally, pixels whose 3×3 neighbourhood has a luminance ratio above 4× get 3 more rays in the rotated-grid pattern. Capped at 15 % of glass pixels; +0.15–0.6 ms (ESTIMATE).
- **No denoiser and no temporal accumulation.** Clear glass is a mirror: SEED says "No filtering required", and Capcom's mirror path uses "no denoiser". History would bring back the ghosting the project dropped TAA for, and it needs motion vectors the game does not render.

### 1.7 Output contract (exactly what the glass hook reads)

| Item | Value |
|---|---|
| `_FR_GlassRTReflection` | Persistent per-camera RGBA16F, 1×, `enableRandomWrite`, imported into RenderGraph (R29). RGB = reflected linear HDR radiance, no Fresnel or strength, fog remainder applied. **A = 0** (no RT) or **1 + linear eye depth (m)** of the front glass surface (a depth tag, coverage 1) |
| `_FR_GlassRTWeight` | 1 while this camera traced this frame and quality ≠ Off; 0 otherwise, and reset to 0 after transparents |
| `_FR_GLASS_RT` | Global keyword, enabled only between steps 4 and 6 for the RT camera |
| Depth-tag precision | Half float at 30 m has a 0.03 m step against the hook's tolerance of 0.02 + 0.01·depth = 0.32 m. It is safe to 80 m |

### 1.8 Which cameras trace

A camera traces only if all of these hold:
- it has opted in: `FrontRoomsPostStack.ConfigureCamera` calls `FrontRoomsGlassRT.OptIn(camera)`, which adds a small `FrontRoomsGlassRTCamera` component on macOS;
- `cameraType == Game` and `renderType == Base`;
- Play Mode or a player;
- at least one receiver is visible.

Excluded as a result: the Scene view, previews, reflection-probe and zone-cube capture cameras, overlay cameras, and the Autopilot shot cameras (`Camera.CopyFrom` copies no components; the harness can opt them in). The title corridor has no receivers, so nothing is enqueued. Each camera keeps its own state and textures, so a second camera never reallocates the first one's (R17, N10).

### 1.9 Kernel compile and build

- **The offline Metal compiler is not installed on this Mac.** `xcrun metal` reports "missing Metal Toolchain; use: xcodebuild -downloadComponent MetalToolchain" (Xcode 27.0). That is a download, so it is a proposal for Red (§5 D4), not done.
- **Without the download:**
  - The build script embeds `FRGlassRTShared.h` + `FrontRoomsGlassRT.metal` into the dylib as one string.
  - It runs a small host validator (`tools/frglassrt_validate.mm`) that compiles the string with the OS Metal compiler (`newLibraryWithSource`). A shader error then **fails the build**, not the game (R3).
- **At runtime:**
  - The plugin compiles asynchronously at device init (`newLibraryWithSource:options:completionHandler:`, then async pipelines). The 52–196 ms compile (N12) leaves the main and render threads.
  - RT stays off until the pipelines exist (R2: fail closed).
  - A one-time GPU layout self-test compares every shared struct's size and field readback; RT is disabled on any mismatch (R4).
- **Build flags:** `-install_name @rpath/libFrontRoomsMetalGlassRT.dylib`, arm64, macOS 13 minimum. Residency sets go behind `@available(macOS 15, *)`.

### 1.10 Every verified defect, and where it is fixed

| Finding (01/02) | Fix in this design | Phase |
|---|---|---|
| R1 gate never opens; R17 every camera; N10 texture thrash | New feature with per-camera state and opt-in (§1.8) | P0 |
| R2 nil pipeline crash; R3 13 MSL errors; N12 compile hitch | Build-time validation, async compile, fail closed (§1.9) | P0 |
| R4 88/96-byte struct mismatch | Shared header, `static_assert`, GPU self-test | P0 |
| R5 wrong BLAS for shared meshes | One BLAS per Mesh; `accelerationStructureIndex` indexes a per-mesh array | P0 |
| R6 FOV in radians; suspect 2 fragile flip | No primary rays; origin from prepass depth plus the GPU matrix | P0 |
| R7 opaque composite after post; R19 Fresnel floor, double strength; R21 shader stripped | Composite deleted; radiance into the hook before transparents; serialized shader references | P0 |
| R8 residency | Residency set / `useResource` | P0 |
| R9 re-traced primary; suspect 4 | Raster prepass with depth test | P0 |
| R10 1 s rebuild on the main thread; R11 blank frame | BLAS cache, render thread, no waits, no reset writes | P0 |
| R12 `Camera.main`; R13 / N8 / N9 selection | Camera from the pass; distance on bounds around the camera; priorities; ray length = far plane | P0 |
| R14 submesh 0 only; R32 static batching | Per-submesh geometry, base vertex, skip static batches | P0 |
| R15 per-material glass flag | Per-instance flags and masks | P0 |
| R16 own command buffer | Unity's current command buffer, event barriers | P0 |
| R22 not provably inert; R28–R30 leaks and hygiene | `#if` guards, importer pin, `CoreUtils.Destroy`, ImportTexture, caps unified, RGBA16F, CB status check, `@rpath` | P0 |
| R23 native texture pointer per frame; R24 races; R25 use-after-free; R26 stale geometry; R27 event-block free | Pointers once; render-thread-only state; generations; own copies; ring of 3 | P0 |
| R18 placeholder shading; R20 thin glass; R31 hardware gate | §1.5, §1.6, Apple9 gate | P1 (gate in P0) |

---

## 2. Interfaces and contracts

### 2.1 The glass shader hook (glass track; `proj_glass` `FrontRoomsGlass.shader`, built)

**Kept exactly as built:**
- `_FR_GlassRTReflection` (Texture2D, `sampler_FR_GlassRTReflection`);
- `_FR_GlassRTWeight` (float);
- `#pragma multi_compile_fragment _ _FR_GLASS_RT`;
- material `_RTReceive` (ToggleUI);
- the A-channel semantics (0 / coverage / 1 + eye depth);
- the edge and crack fade;
- `EnvironmentBRDFSpecular` weighting with `_PaneF0` 0.08;
- `FrontRoomsGlassRTStripper` (WebGL).

**Requests.** The implement step adds them in `proj_rt`'s copy and writes the exact diff to `rt/harness/glass_hook_diff_for_glass_track.diff.txt`.

| # | Change | Why |
|---|---|---|
| G-1 | New pass `Name "FRGlassRTPrepass"`, `Tags { "LightMode" = "FRGlassRTPrepass" }`. ZWrite On, ZTest LEqual into our depth, no blend, Cull Back. It `clip`s when `_RTReceive < 0.5` or the pixel is behind `_CameraDepthTexture`. It writes SV_Target0 = linear eye depth (R32F) and SV_Target1 = (final world normal with roll, smudge, bow and crack facets; perceptual smoothness) as RGBA16F, using the same grime, roll and crack code as `ForwardLit`. No new keywords | Reflection directions and smudge blur from the real per-pixel surface; bow and facets at P4 |
| G-2 | Inside the `_FR_GLASS_RT` block: `smoothstep(0.15h, 0.45h, …)` → `smoothstep(_FR_GlassRTFade.x, _FR_GlassRTFade.y, …)`, a global float2 set by the RT pass: (0.15, 0.45) in P0, (0.45, 0.70) once the P1 blur runs | Smudges blur the live reflection instead of swapping to the cube |
| G-3 | Inside the `_FR_GLASS_RT` block: roll tilt × `_FR_GlassRTRollScale` (global, set to 0.075 by the RT pass; the same factor in `FRGlassRTPrepass`) | Annealed glass has no roll wave; 0.02 warps a true-parallax reflection |
| G-4 | `FrontRoomsGlassRTWebGLStripper` (RT-owned, `Assets/Editor/RT/`) also removes the `FRGlassRTPrepass` pass and `Hidden/FrontRooms/GlassRTPrepass` from WebGL builds | WebGL variant count and output unchanged |

The glass track's F0 is already right (`_PaneF0` 0.08), so 03's request 9 is closed.

### 2.2 RT API (C#, visual side; used by the destruction track's `FrontRoomsGlassBreakable` and the verify harness)

```csharp
public enum FrontRoomsGlassRTQuality { Off = 0, High = 1, Ultra = 2 }
[System.Flags] public enum FrontRoomsGlassRTFlags : uint
{ None = 0, Glass = 1u << 0, FractureEdge = 1u << 1, Shard = 1u << 2, EmissiveLens = 1u << 3, Dynamic = 1u << 4,
  Relay = 1u << 5, Door = 1u << 6, MeshUV = 1u << 7, SurfacePlanar = 1u << 8, Deforming = 1u << 9, ReflectionOnly = 1u << 10 }
public struct FrontRoomsGlassRTRange { public int indexStart, indexCount, baseVertex; }   // one fracture piece
public static class FrontRoomsGlassRT
{
    public static FrontRoomsGlassRTQuality Quality { get; set; }   // §2.6; forced Off when !Supported
    public static bool Supported { get; }                           // Apple9 + kernel + layout OK (false off-Mac)
    public static string Status { get; }                            // one line: why it is off, or tier + residency mode
    public static void OptIn(Camera camera);                        // from FrontRoomsPostStack.ConfigureCamera
    public static int  Register(Renderer renderer, FrontRoomsGlassRTFlags flags, int windowId = -1); // handle; in the TLAS this frame
    public static void Unregister(int handle);                      // out of the TLAS this frame
    public static void SetEnabled(int handle, bool enabled);        // beat-frame swaps without re-registering
    public static int  PrepareMeshes(Mesh source, FrontRoomsGlassRTRange[] pieces, int notBeforeFrame); // batched BLAS build, one per range; ticket
    public static bool IsPrepared(int ticket);
    public static void MeshChanged(Mesh mesh);                      // rebuild BLAS (in-place edits)
    public static void RefitMesh(Mesh deforming);                   // refit BLAS (Deforming only)
    public static bool TryGetStats(out FrontRoomsGlassRTStats stats); // GPU ms per stage, counts, skips, errors
}
```

On every platform except macOS, every member compiles to a no-op (`Supported` = false). That way, callers such as the destruction track need no `#if`.

### 2.3 Native exports (`FRGlassRT_`; `#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX` on the C# side)

| Group | Exports |
|---|---|
| Plugin | `GetRenderEventAndDataFunc`, `QueryCaps(out caps)` (bits: plugin, Metal3, Apple9, supportsRaytracing, residency sets, kernel ready, layout OK), `CopyLastError(buf, len)` (copied under a lock; R24), `GetStat(i)` |
| Cameras | `AllocCamera(id)`, `FreeCamera(id)` (deferred), `SetCameraTargets(id, depth, normal, raw, out, w, h)` |
| Geometry | `UploadMeshCPU(key, pos, nrm, uv0, colR, vtxCount, idx, idxCount, submeshes, subCount)`, `UploadMeshGPU(key, vb, ib, layout, submeshes, subCount)`, `UploadMeshRanges(firstKey, …, ranges, rangeCount, notBeforeFrame)`, `RefitMesh(key, pos, vtxCount)`, `ReleaseMesh(key)` |
| Materials | `SetMaterial(index, record, textures, texCount)` |
| Frame | `BeginFrame(serial) → FrameSlot*` (pointers into the free ring slot, or null = skip), `EndFrame(serial, instanceCount, lampCount, flags)`, `ResetScene()` |

### 2.4 Map chat contract (exact signatures; one edit serves G14 and the WebGL manager)

These match `webgl/10_webgl_plan.md` §2.7. G14 asks for no second shape.

```csharp
// FrontRoomsMapWorld (global namespace). Compiled on every platform; payloads built only when subscribed.
public static event Action<FrontRoomsMapWorld> Created;    // end of Awake / CreateEmbedded
public static event Action<FrontRoomsMapWorld> Destroyed;  // OnDestroy, before Release()
public Camera ViewCamera { get; set; }                      // FrontRooms3DGame sets it next to map.Begin(playerRoot, false)
public event Action<ChunkHandles> ChunkBuilt;               // end of BuildInto, after built[coord] = chunk
public event Action<ChunkHandles> ChunkReleasing;           // START of Unregister(chunk), before FreeMeshes/Kill
public event Action<RoomHandle> RoomDressed;                // when DressNext/Dress finishes a room
// ChunkHandles / RoomHandle: exactly as webgl/10 §2.7. G14 reads coord, root, block renderers + meshes,
// lens renderers + fixture ids, door-leaf / pane / key renderers; RoomHandle.renderers/meshes/lods.

// RT only (desktop; under G14's approval):
public struct FrontRoomsLampState
{
    public int fixtureId;            // stable while its chunk lives
    public Light light;              // the raster's light (enabled, intensity, colour, range, angles, shadows)
    public Renderer lens;
    public Color lensEmissionLinear; // (f.emission * Mathf.Max(.04f, f.level * held)).linear = what the GPU sees
    public bool lightOn, shadowsOn;  // this frame, after TickFixtures
}
public int CopyLampStates(List<FrontRoomsLampState> into, Vector3 centre, float radius);
```

| # | Request | Where (12:21 copy) | Needed by |
|---|---|---|---|
| C1 | **Delete** ChatGPT's comment and `if (standalone && Application.isPlaying) FrontRoomsMetalGlassRTController.Ensure();` | `Awake`, lines 417–420 | Contract change (RT1). Safe any time after P0: P0 keeps `Ensure()` as an `[Obsolete]` shim that forwards to the self-starting system |
| C2 | **Delete** `pane.AddComponent<FrontRoomsMetalGlassTarget>();` | `BuildInto` window branch, line 1128 | Contract change. Receivers come from `_RTReceive` materials on the visible glass; the marker stays as an inert hint class until then |
| C3 | `Created` / `Destroyed` | Awake / OnDestroy | P2 (P0 finds the map itself) |
| C4 | `ViewCamera` | `FrontRooms3DGame.cs` next to `map.Begin` (line 630) | Optional for RT (it uses the opted-in camera); the WebGL manager needs it |
| C5 | `ChunkBuilt` / `ChunkReleasing` | end of `BuildInto` (`built[coord] = chunk`, line 957); start of `Unregister` (line 801). `BuiltChunk` must carry its coord | P2 (replaces P0's diff) |
| C6 | `RoomDressed` | `DressNext` / `Dress` (lines 1590–1600) | P2 |
| C7 | `CopyLampStates` | filled from `chunk.fixtures` after `TickFixtures` | P1 uses `Light` components plus MPB reads; P2 switches to this |
| C8 (existing, other tracks) | Panes on `Glass_Window`, 6 mm, shadows off (`glass/10_implementation.md` §8); the visible slab (`interactables/06` §4.3) or the window root (destruction §4.4) | `BuildInto` window branch | **For Red to see RT in the game.** Until then the verify harness swaps panes at runtime, as G10 did |

`GlassBroken(Vector3)` exists and is kept. Same-frame removal does not depend on it (§1.4).

### 2.5 Renderer assets and platform isolation

| Step | Rule | Proof |
|---|---|---|
| Feature placement | The feature stays only in `FrontRooms_URP_Renderer.asset` (desktop). The WebGL plan's Phase 1.1 builds `FrontRooms_URP_WebGL_Renderer.asset` from its own feature table (SSAO + far fade) and fails a Web build that lists `FrontRoomsMetalGlassRTRendererFeature` (its R1) | WebGL pre-flight |
| Class compiled everywhere, body guarded | The feature class exists on every platform, so the asset never has a missing script. `AddRenderPasses` and `Create` are empty unless `UNITY_EDITOR_OSX \|\| UNITY_STANDALONE_OSX`, and at runtime they also require Metal | Inspect the compiled code |
| P/Invokes | Only under `#if UNITY_EDITOR_OSX \|\| UNITY_STANDALONE_OSX` (satisfies the WebGL plan's R2; removes them from Windows players too) | P0-A10: WebGL player-script compile has no `FRGlassRT_` imports |
| Importer | Editor (OS OSX, CPU ARM64) + Standalone OSX (ARM64), everything else off, `isPreloaded = true`, pinned by `FrontRoomsGlassRTImporter` through the `PluginImporter` API (R22; WebGL plan R3) | Read back the importer |
| Shaders | `Hidden/FrontRooms/GlassRTPrepass` is referenced by a serialized field on the feature (R21). Its variants and the glass's `FRGlassRTPrepass` pass are stripped from WebGL (G-4); `_FR_GLASS_RT` is stripped by the glass track (the WebGL plan's R4) | Stripper log: WebGL variant count unchanged |
| Windows | Feature inert; no native code; output unchanged until P3 | Windows build diff |

### 2.6 Settings

| | Off | High | Ultra |
|---|---|---|---|
| Trace | — | 1 ray/px, full resolution | 1 ray/px + adaptive 4-ray AA (≤ 15 % of glass px) |
| TLAS cap | — | 2,048 | 4,096 |
| Lights | — | all enabled (≤ 128) | same |
| Shadow rays | — | 4 nearest shadowed lamps; fill × 0.82 | all shadowed lamps + fill ray |
| Glass layers seen through | — | 1 | 2 |
| Edge AA | — | edge filter | edge filter + adaptive rays |
| Back-surface image, smudge blur, ray-cone mips | — | on | on |
| GPU at 1440p, typical (§3) | 0 | 1.3–2.0 ms | 2.2–3.4 ms |

- **Default**, while `Supported`: Ultra at quality level 5 (the Mac player's default), High at levels 3–4, Off at 0–2.
- **Override:** PlayerPrefs `FrontRooms.GlassRT`, and the command line `-frGlassRT off|high|ultra` for captures.
- A menu entry belongs to the game/UI owner (optional request).
- Red's highest-spec rule argues for Ultra; §5 D1 asks him.

### 2.7 Capability gate (fail closed: weight 0, keyword off, one log line, no further native calls)

| # | Check | Off-result |
|---|---|---|
| 1 | Compiled for `UNITY_EDITOR_OSX \|\| UNITY_STANDALONE_OSX` | no code |
| 2 | `SystemInfo.graphicsDeviceType == Metal`, not `-nographics`, Play Mode or player | Off |
| 3 | Dylib loaded (`DllNotFound`/`EntryPointNotFound` caught), `IUnityGraphicsMetalV2` present | Off |
| 4 | `supportsRaytracing && supportsFamily(MTLGPUFamilyApple9)` (Apple staff, forum 744941) | M1/M2: Off (P2 tier); others: Off |
| 5 | Async kernel compile done, layout self-test passed | Off until ready; Off for the session on failure |
| 6 | macOS ≥ 15 → residency set; else `useHeap`/`useResource` | both supported |
| 7 | Per frame: opted-in camera, ≥ 1 visible receiver, no GPU error reported | weight 0 for that frame / session |

### 2.8 The fracture contract (destruction plan §4.7), as this design meets it

| §4.7 need | Here | Phase |
|---|---|---|
| Rays start from the raster glass pixels | Prepass (§1.3); facets come from the stage meshes' normals | P0 |
| S1 = 1 instance (fins + crater); S2 = 1; S3 ≤ 64 largest pieces; smaller pieces masked; after settle = 2 | `Register(…, Glass \| FractureEdge [\| Shard], windowId)`; Shard → mask 0x04 out of reflection rays, still in the prepass | P4 (API in P0) |
| Capacity ≥ 1,024, glass and pieces first, then nearest | 2,048 / 4,096 caps, priority class 0 | P0 |
| Rigid pieces: build once, never refit or rebuild; one batched encoder from index ranges of one buffer, between 0.70 and 1.0 s, never on a beat frame | `PrepareMeshes(source, ranges, notBeforeFrame)` → `UploadMeshRanges`: CPU data (generator meshes are readable), no native pointer fetch, ≤ 50k triangles per frame | P0 API, P4 use |
| Refit only deforming meshes | `RefitMesh` (Deforming flag); none in the plan | P0 API |
| TLAS every frame while anything moves | Rebuilt every frame (P0); static/dynamic split later (P2) | P0 |
| Per-piece flags; window id in the instance user id | Per-instance record: flags + window id; user instance id = record index | P0 |
| Mesh format | Any is accepted (we copy); the generator keeps its single-submesh Float32 format | P0 |
| No VAT on traced pieces | Transforms only; documented in the API | P4 |
| Lifetime: slab out on the 0.70 frame, stage 2 out on the shatter frame; unregister pieces before the floor merge | `SetEnabled` / `Unregister` take effect in the same frame (§1.4) | P0 API, P4 test |
| Crack lines stay raster | Yes; the hook already fades RT on cracks and edges | — |
| Bow | Through the prepass normal (G-1), no BLAS change | P1/P4 |
| Cost in the shot: ≤ +2 ms at 1080p | §3 estimates 2.1–3.1 ms (High). See risk §5 D6 | P4 |

---

## 3. Budgets (M3 Max, 1440p unless stated)

### 3.1 GPU per frame

| Stage | Typical, High (window ~25 % of screen) | Typical, Ultra | Worst, High (pane fills the screen, break shot) | Source |
|---|---|---|---|---|
| Glass prepass | 0.05–0.15 | 0.05–0.15 | 0.15–0.25 | ESTIMATE |
| TLAS build (2,048 / 4,096 instances) | 0.25–0.30 | 0.52–0.53 | 0.25–0.30 | MEASURED bench 1 |
| Trace + hit shading | 0.86–1.36 | 1.24–1.79 (8 shadowed) + 0.1 fill ray | 2.8–4.3 (bench 2 1080p @100 %: 1.57–2.44, × 1.78 px) | MEASURED bench 2 / ESTIMATE scale |
| Adaptive AA (Ultra) | — | +0.15–0.6 | — | ESTIMATE |
| Resolve (edge filter, ghost, blur, dilation) | 0.08–0.15 | 0.1–0.2 | 0.3 | ESTIMATE |
| Event barriers | 0.01–0.05 | 0.01–0.05 | 0.01–0.05 | ESTIMATE |
| BLAS builds on streaming frames | ≤ 1.0 (≤ 50k triangles) | same | same | MEASURED bench 1 scaled (160k triangles = 1.7–3.1 ms) |
| Render-pass split (MSAA store/reload) | 0–0.8 | 0–0.8 | 0–0.8 | UNVERIFIED; P0 measures (§1.2) |
| **Total (excluding split and BLAS)** | **1.3–2.0 ms** | **2.2–3.4 ms** | **3.5–5.2 ms** | |

- At 1080p, worst case on High: 2.1–3.1 ms (trace 1.57–2.44 MEASURED + TLAS + prepass + resolve). The destruction plan's shot limit is +2.0 ms (§5 D6).
- At 4K, typical High: ~2.4–3.7 ms (bench 2: 2.11–3.48 at 2160p for the trace).
- The prototype measured 0.75–3.2 ms at 1080p in-engine for **every-pixel** primary rays (02 §8.1). The new path traces only glass pixels.
- Every bench number is standalone Metal with other Unity jobs on the GPU (±30 %). The authoritative numbers come from P0/P1 in a player build.

### 3.2 Memory (ESTIMATE; P0 logs the real numbers via `GetStat`)

| Item | Size |
|---|---|
| Screen targets per RT camera: GlassDepth R32F + GlassNormal RGBA16F + raw RGBA16F + out RGBA16F (persistent), plus transient D32 | 1080p 58 MB (+8); **1440p 103 MB (+15)**; 4K 232 MB (+33). Prototype: 33 MB ARGBFloat at 1080p for one target |
| BLAS (compacted): kit LOD0 ≤ 150k triangles + shells/lenses/panes of ~25 built chunks (25–125k triangles) + fracture ≤ 21k per window | 8–12 MB (≈ 39 B per triangle compacted; bench 1). Up to ~25 MB transient before compaction |
| Geometry pool (positions, normals, UV0, indices) | 10–15 MB |
| TLAS ring ×3 + scratch, descriptor/record ring ×3, material/lamp tables | 4–8 MB |
| **Total at 1440p** | **≈ 125–160 MB** (Red's Mac: 128 GB unified) |

### 3.3 CPU

| Thread | Work | Budget |
|---|---|---|
| Main, every frame | Liveness check and transforms of ≤ 300 dynamic instances; lamp list (≤ 128); descriptor writes; camera block | ≤ 0.3 ms p99 (ESTIMATE) |
| Main, streaming frames | Registration slices (AcquireReadOnlyMeshData copies, material reads, one native-pointer fetch per new kit mesh per session) | ≤ 1.0 ms per frame, sliced |
| Render thread | Encode AS + 2 compute dispatches + barriers | ≤ 0.15 ms (02 MEASURED 0.04–0.08 ms for the prototype's encode + commit) |
| GPU waits on CPU | none | 0 (grep test) |

### 3.4 How it is measured

1. `MTLCounterSampleBuffer` timestamps at encoder boundaries (AS, trace, resolve), resolved in the completion handler and exported through `FRGlassRT_GetStat`.
2. Whole-frame GPU time with RT Off vs High vs Ultra from `FrameTimingManager`.
3. A macOS arm64 **player build** (non-development) at 1080p and 1440p, on an **idle** machine (02 and 03 shared the GPU with 4–5 Unity jobs).
4. Five views (02's v1–v5), the dark-beyond view and the destruction shot pose.

---

## 4. Phased plan

### 4.1 P0 — correctness and integration (in scope for the next step)

**Files (proj_rt):**

| File | Change |
|---|---|
| `NativePlugin/FrontRoomsMetalGlassRT.mm` | Rewrite: device events, caps, async compile, op queue, ring, render event (§1.2), timers, retire list |
| `NativePlugin/FRGlassRTScene.mm` + `.h` | New: geometry pool, BLAS cache (build, compaction, refit, ranges), TLAS ring, residency |
| `NativePlugin/FRGlassRTShared.h` | New: structs shared by C++ and MSL, `static_assert`s |
| `NativePlugin/FrontRoomsGlassRT.metal` | New: gather, trace, resolve, layout self-test, parity kernels |
| `NativePlugin/tools/frglassrt_validate.mm`, `build_frontrooms_metal_glass_rt.sh` | Embed the MSL, validate at build time, `@rpath`, build all `.mm` |
| `Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRT.cs`, `…Native.cs`, `…Scene.cs`, `…Meshes.cs`, `…Materials.cs`, `…Lamps.cs`, `…Camera.cs` | New: API, P/Invokes (macOS only), registry and diff, uploads, material table, lights/SH/fog/cube, per-camera state |
| `Assets/Scripts/Rendering/FrontRoomsMetalGlassRTRendererFeature.cs` | Same class and guid; the prepass, trace and end passes; inert off-Mac |
| `Assets/Scripts/Rendering/FrontRoomsMetalGlassRT.cs` | Shims only: `FrontRoomsMetalGlassTarget` (hint), `FrontRoomsMetalGlassRTController.Ensure()` `[Obsolete]` forwarding |
| `Assets/Scripts/Rendering/FrontRoomsPostStack.cs` | +1 line in `ConfigureCamera`: `FrontRoomsGlassRT.OptIn(camera);` |
| `Assets/Shaders/GlassRT/FrontRoomsGlassRTPrepass.shader` | New `Hidden/FrontRooms/GlassRTPrepass` |
| `Assets/Editor/RT/FrontRoomsGlassRTImporter.cs`, `FrontRoomsGlassRTWebGLStripper.cs`, `FrontRoomsGlassRTVerify.cs` | Importer pin, WebGL stripping, the acceptance harness (reuse `FrontRoomsGlassRTProbe.cs`) |
| Delete | `Assets/Shaders/FrontRoomsMetalGlassRTComposite.shader`, empty `Assets/Scripts/Rendering/MetalGlassReflection/` |
| Copy read-only from `proj_glass` | `Resources/Rendering/FrontRoomsGlass.shader`, `Resources/Surfaces/Glass_*.mat` + `Textures/GlassGrime_M.png`, `GlassSmear_N.png`, `Scripts/Rendering/FrontRoomsGlassPane.cs`, `FrontRoomsZoneReflection*.cs`, the `FrontRoomsLook.cs` zone part, `Resources/Rendering/Reflections/*.exr`, `FrontRoomsReflectionBlend.shader`, `Editor/Rendering/FrontRoomsGlassRTStripper.cs`, `FrontRoomsGlassSetup.cs` (with their `.meta`s; resolve compile dependencies) |
| **Not touched** | `FrontRoomsMapWorld.cs`, `FrontRooms3DGame.cs` (map chat) |

**Acceptance.** Main scene `FrontRooms3D.unity`, `Random.InitState(4242)` → run seed 516574485, test pane `(282,206)-(282,207)`. Panes are swapped to `Glass_Window` by the harness, using the game camera (76°), 4× MSAA and full post. Frames go to `rt/images/` as JPG q85, ≤ 1600 px wide.

| # | Test | Pass | Frames |
|---|---|---|---|
| P0-A1 | Runs in the main game with MapWorld unchanged | Log `RT on: Apple9, kernel N ms (async), layout OK, residency <mode>, hazard <modes>`; 1 RT event per rendered frame; 0 Metal API/shader-validation errors with `MTL_DEBUG_LAYER=1 MTL_SHADER_VALIDATION=1` | — |
| P0-A2 | Alignment | RT coverage vs the glass's own forward coverage: IoU ≥ 0.995 and boundary ≤ 1 px in v1–v5 (prototype 0.33–0.59). Validation build: origin error ≤ 1 mm at ≤ 10 m | `P0_alignment_v1…v5.jpg` |
| P0-A3 | Orientation | Asymmetric test (red cube behind-left, green behind-right) mirrored correctly in all views | `P0_orientation.jpg` |
| P0-A4 | Transmission kept, reflection graded | Face-on lit-to-lit: mean ΔY RT on − off ≤ +4/255, no pixel darker by > 1/255. RT region ≠ sRGB(RT linear) (it passes ACES) | `P0_transmission_v2.jpg` |
| P0-A5 | Occlusion and lifetime (02's cases) | Unregistered panel in front: **0 px** (02: 137,551). Relay copy moved 0.7 m: correct the same frame (02: 177,035 stale). Broken pane: **0 px** on the break frame (02: 159,533 until a rescan). Destroyed mesh: 0 px next frame (02: 17,262). `MeshChanged` edit: new shape next frame | `P0_occluder.jpg`, `P0_relay_moving.jpg`, `P0_broken_pane.jpg`, `P0_stream_test.jpg` |
| P0-A6 | No sky indoors | Misses ≤ 0.5 % of glass pixels in v1/v2 (02: 16–20 %); a miss shows the zone cube | `P0_hits_v2.jpg` |
| P0-A7 | No hitch, no flicker | 600-frame scripted walk with ≥ 3 chunk builds and drops: 0 blank RT frames (02: 1 per second). RT main thread p99 ≤ 0.5 ms (non-streaming) and ≤ 1.5 ms (streaming frames). Render-thread encode p99 ≤ 0.2 ms. `waitUntilCompleted` count = 0 | `P0_free_run_chart.jpg` |
| P0-A8 | Capacity and priority | With +3,000 synthetic instances, the cap holds, priority-0 instances are all present, and nothing within 20 m is dropped | log |
| P0-A9 | Cameras | Game + second camera + Scene view: RT only on the opted-in camera; 0 texture re-creations over 100 frames; Scene view keyword off | log |
| P0-A10 | Isolation | WebGL player-script compile: no `FRGlassRT_` imports. Importer read back = Editor/Standalone OSX ARM64, preloaded, others off. Stripper removes 100 % of the prepass variants on WebGL | log |
| P0-A11 | GPU timer + pass split | Per-stage ms for v1–v5 at 1080p and 1440p (placeholder shading). Empty unsafe pass at `BeforeRenderingTransparents`: Δ GPU frame time; if > 0.3 ms at 1440p, evaluate F1 (§1.2) | `P0_cost_chart.jpg` |
| P0-A12 | Player smoke test | macOS arm64 player, 3-minute autopilot walk with streaming and one pane break: 0 crashes, 0 command-buffer errors (render-thread races R24/R25 only exist in players) | log |
| P0-A13 | Fracture API | `PrepareMeshes` with 150 ranges finishes in ≤ 4 frames at ≤ 2 ms GPU per frame, none before `notBeforeFrame`. A 20k-triangle Deforming mesh refits at ≤ 0.3 ms | log |

### 4.2 P1 — hit-shading quality (in scope for the next step)

**Files:**
- `FrontRoomsGlassRT.metal`: the hit shader of §1.5, thin glass, smudge blur, AA, parity mode.
- `…Materials.cs`: Surface and Lit parameter extraction, `_CeilingHeight` from the MPB, bindless texture table.
- `…Lamps.cs`: the enabled `Light`s, lens MPB `.linear`, SH, fog, zone cube.
- `proj_rt` copy of `FrontRoomsGlass.shader`: G-1, G-2, G-3, with the diff written to `rt/harness/glass_hook_diff_for_glass_track.diff.txt`.

| # | Test | Pass | Frames |
|---|---|---|---|
| P1-B1 | Parity (primary rays through the hit shader vs raster, frozen frame, post and SSAO off) | Per material class (L0 wallpaper, carpet, ceiling; Office drywall, carpet tile; cove base; door veneer; kit props; lens): mean \|Δ\| ≤ 3 % linear, p95 ≤ 8 %. Lens radiance RT/raster 0.97–1.03 (≈ 9.2 linear at 2.6) | `P1_parity_<class>.jpg` (raster \| RT \| ×8 diff) |
| P1-B2 | Mirror reference | G11 planar-mirror camera vs RT at v2/v3: luminance ratio 0.9–1.1 in ≥ 95 % of 64-px blocks (SSAO-darkened corners excluded) | `P1_reference_vs_rt_v2.jpg`, `_v3.jpg` |
| P1-B3 | Lamps in sync | A stuttering lamp's lens in the reflection vs the real lens: per-frame correlation ≥ 0.99 over 120 frames. A lamp turned off is off in the reflection the same frame | `P1_lamp_sync_chart.jpg` |
| P1-B4 | Art-review set | Oblique 45° and 65° with a lens in the reflection; a staged dark room beyond; the Relay crossing 2 m behind; pane at 0.55 m filling the frame; Office zone; HDR-off display mode. Each as Off / High / Ultra | `P1_oblique45.jpg`, `P1_oblique65.jpg`, `P1_dark_beyond.jpg`, `P1_relay_behind.jpg`, `P1_close055.jpg`, `P1_office.jpg`, `P1_hdr_off.jpg` |
| P1-B5 | Thin glass | Back image at 0.5 m from a lens edge: offset 2–4 px at 1440p, weight 0.45–0.50; < 1 px beyond 4 m | `P1_ghost_close.jpg` |
| P1-B6 | Anti-aliasing | Temporal luminance std along reflected lens edges during a 1 cm/frame strafe: High ≤ 60 %, Ultra ≤ 35 % of the unfiltered 1-ray value (ESTIMATE thresholds, tuned on frames) | `P1_edge_crawl.jpg` |
| P1-B7 | Smudges | A smudge over a reflected lens shows a blurred lens, not the cube (`_FR_GlassRTFade` 0.45→0.70) | `P1_smudge.jpg` |
| P1-B8 | Cost (player, idle, 1440p) | v2 High ≤ 2.0 ms, Ultra ≤ 3.5 ms; pane filling the screen: High ≤ 5 ms, Ultra ≤ 7 ms; 1080p shot pose on High reported against the +2 ms target | `P1_cost_chart.jpg` |

### 4.3 P2 — streaming and performance

**Work:**
- Subscribe to C3/C5/C6/C7 and retire the diff.
- Static/dynamic TLAS split (two-level instancing) or refit on unchanged membership.
- Merge lenses per chunk into one BLAS with per-primitive fixture ids (−1,500 instances).
- Indirect tile dispatch.
- A light grid if > 128 lamps.
- Smaller screen targets (pack depth and normal; ping-pong reuse).
- Dynamic trace resolution if the GPU timer exceeds budget for 30 frames.
- Measure M1/M2 for a half-resolution tier (needs such a Mac).
- `SkinnedMeshRenderer.GetVertexBuffer()` refit for a future skinned Relay.

**Acceptance:**
- 0 diff work with events present.
- Streaming frames: RT main thread ≤ 0.5 ms, BLAS GPU ≤ 1 ms.
- The split saves ≥ 0.2 ms vs P0 at 4,096 instances.
- All P0/P1 frames unchanged (pixel diff ≤ 1/255).

### 4.4 P3 — Windows DXR

**Files:**
- `Assets/Scripts/Rendering/GlassRT/Windows/FrontRoomsGlassRTDxr.cs`: `RayTracingAccelerationStructure` in Manual mode, URT `IRayTracingAccelStruct`, under `#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN`.
- `Assets/Shaders/GlassRT/FrontRoomsGlassRTTrace.urtshader`, plus `FrontRoomsGlassRTHit.hlsl`: a port of the same hit shader. Textures via per-material `Texture2DArray`s, since Unity HLSL has no bindless.
- The same prepass and output; gated on `SystemInfo.supportsRayTracing` with DX12.

**Acceptance** (needs a DX12 RTX 20 / RX 6000 PC):
- P1-B4 frames within ±3/255 mean of the Mac over the glass.
- GPU ≤ 2.5 ms at 1440p typical (ESTIMATE target).
- No Windows pixel changes with RT Off.

Effort: 4–6 days plus the PC (03 §6.8). Until then Windows keeps the planar reflection.

### 4.5 P4 — the fracture hookup (exactly the §4.7 contract)

**Files:**
- The destruction track's `FrontRoomsGlassBreakable` calls `Register`, `SetEnabled`, `PrepareMeshes` and `Unregister` (§2.2, §2.8).
- `Glass_Fracture` material with `_RTReceive = 1`.
- The bow through G-1's prepass normal.

**Acceptance** = destruction plan §5.2 step 12, in `GlassBreakLab`:
- C1/C2 frames, RT on vs off, at 0.70, 0.71, 0.99, 1.00, 1.033, 1.05, 1.10, 1.20, 1.40 and 1.65 s.
- V6 in the traced reflection: the lamp reflection steps ≥ 1 cm at 1 m across cracks.
- **0 px** ghost pane after 1.00 s.
- Traced vs raster glass mask ≤ 1 px on every captured frame.
- Piece BLAS builds logged only between 0.70 and 1.0 s, never on a beat frame.
- ≤ 64 piece instances per window in the TLAS; pieces < 4 cm² masked from reflection rays.
- Frame time with the pane filling the view ≤ +2 ms at 1080p, with the shot budget of §5 D6 if needed.

### 4.6 What the implement step must do now (P0 + P1, in `proj_rt` only)

1. **Baseline.** Start from Red's committed RT files (hashes in `scratchpad/rt_baseline_hashes.txt`; dylib 4bf02a4c), not the probe's instrumented `.mm` (06f38526) or dylib (beb93f8d). Keep `Assets/Editor/RT/FrontRoomsGlassRTProbe.cs` as the harness base.
2. **Copy** the glass-track files listed in §4.1 read-only from `proj_glass`. Do not edit `proj_glass`.
3. **Native.** Write the plugin (§1.2, §1.4, §1.9, §2.3). Build only with the clone's `NativePlugin/build_frontrooms_metal_glass_rt.sh`, only while no Unity runs on `proj_rt`. Check the arm64 slice, `@rpath` and the validator output.
4. **C#.** Write the system, feature and passes (§1.2–§1.8, §2.2), the shims and the `ConfigureCamera` line. Pin the importer by running `FrontRoomsGlassRTImporter` in batch.
5. **Prepass.** Add the override shader (P0), then G-1/G-2/G-3 in the copied glass shader (P1). Write the exact diff to `rt/harness/glass_hook_diff_for_glass_track.diff.txt`.
6. **Hit shading.** Implement §1.5 and §1.6 with the parity mode first, so every material is checked against the raster as it lands.
7. **Run P0-A1…A13, then P1-B1…B8.** Use Unity batch with graphics, never two Unity processes on `proj_rt`, timeout ≤ 600,000 ms, plus one macOS player build for P0-A12/P1-B8. Write frames to `rt/images/` with the names above, and the report to `rt/11_p0_p1_report.md` with every number from §4.1–4.2.
8. **Report blockers.** Report, do not work around, anything that needs MapWorld, the glass track's own clone, a download (§5 D4) or a package install.

---

## 5. Decisions for Red, and open items

| # | Decision / open item | Recommended default |
|---|---|---|
| D1 | Default RT quality on M3/M4 Macs | **Ultra** (your highest-spec rule) if P1-B8 shows ≤ 3.5 ms typical at your resolution; otherwise High |
| D2 | A body in mirror-dark windows (a first-person camera reflects nothing) | Undecided. The `ReflectionOnly` flag and mask 0x10 are reserved; destruction plan §3 option (b) |
| D3 | The moving wallpaper in reflections | Identical to the wall (default); a deliberate "reflection disagrees" beat only if you ask |
| D4 | Download Xcode's Metal Toolchain (`xcodebuild -downloadComponent MetalToolchain`) for offline `.metallib` builds | Not needed: build-time validation plus async runtime compile cover it. Optional, your approval |
| D5 | M1/M2 tier | Off until measured (P2) |
| D6 | Break-shot cost: ESTIMATE 2.1–3.1 ms at 1080p on High vs the destruction plan's +2 ms | Measure in P1-B8. If over, the shot uses a "shot budget" (2 nearest shadowed lamps instead of 4, about −0.2 ms each at full screen): 1.7–2.7 ms (ESTIMATE). If still over, you choose between +2.7 ms for 1.65 s and a cheaper reflection in the shot |
| O1 | Render-pass split cost (§1.2) | P0-A11 measures; fallback F1 is ready |
| O2 | Unity's Metal texture hazard mode | P0 logs it; event barriers make either case safe |
| O3 | Volumetric lamp beams are missing from reflections (transparent, not traced) | Visible only in mirror-dark windows; revisit after P1-B4 |
| O4 | No SSAO at hits | Corners in reflections a little brighter than in the raster (ESTIMATE ≤ 5–10 %); optional Ultra AO ray in P2 if P1-B4 shows it |
| O5 | Keyword policy: the destruction plan asks for "no new keywords", the hook uses `_FR_GLASS_RT` stripped on WebGL | Keep the keyword (WebGL variant count unchanged); the glass and destruction tracks to confirm |
| O6 | Glass-track ownership | G-1…G-3 are requests: the glass track merges them, or accepts `proj_rt`'s diff |

---

## 6. Sources

- **This track:**
  - `01_code_review.md` (R1–R32, the harness, fix diffs);
  - `02_runtime_probe.md` (in-engine numbers, frames `images/02_*.jpg`);
  - `03_research.md` (Apple, Unity and game sources [A1–A15], [G1–G21], [U1–U3], [P1–P7]; benches `03_bench/bench_run1.txt`, `bench2_production_shading.txt`).
- **Destruction:** `../destruction/10_glass_destruction_plan.md` §4.4, §4.7, §5.2; `../destruction/04_unity_implementation.md` §1.3, §4, §7, §12.
- **WebGL:** `../../webgl/10_webgl_plan.md` §2.7, §3.1–3.2, §7.4.
- **Glass:** `../10_implementation.md` §8; `../20_verification.md` (G10); `../11_reflections_and_raytracing.md` (superseded for desktop Mac).
- **Interactables:** `../../interactables/06_period_windows.md` §4.3.
- **Code, Red's working copy (read only, 12:21):**
  - `FrontRoomsMapWorld.cs`: `Fixture` 146; `Awake` 417–420; `Unregister` 801; `BuildInto` 845 / 957; window branch 1128; `TickFixtures` 1387; `CheckLampStatesForTools` 1469 (the `GetColor` comparison at 1486); `DressNext` 1590; `GlassBroken` 2304.
  - `FrontRooms3DGame.cs`: `CreateCamera` 250–262; `ConfigureCamera` 336; `map.Begin` 630; `ApplySceneLighting` 292–306.
  - `FrontRoomsSurface.shader` 9–50, 157–252; `FrontRoomsLook.cs`; `FrontRoomsPostStack.cs`.
  - `FrontRooms_URP.asset` (MSAA 4, HDR, depth texture on); `FrontRooms_URP_Renderer.asset` (`m_CopyDepthMode: 1`, SSAO DepthNormals, the RT feature).
  - Kit FBX `.meta` (`isReadable: 0`).
- **URP 17.3 source** (`proj_rt` PackageCache): `UniversalRendererRenderGraph.cs:917-922, 954-957`; `RealtimeLights.hlsl:47-74`; `ShaderVariablesFunctions.hlsl:366-371`; `UniversalRenderPipeline.cs:465`.
- **Glass track** (`proj_glass`, read only): `FrontRoomsGlass.shader` `[G14-HOOK]` blocks, `_PaneF0`, `_RollStrength`; `FrontRoomsGlassRTStripper.cs`; `FrontRoomsZoneReflection.cs`; `Glass_Window.mat` (`_RTReceive: 1`).
- **Tooling check (this stage):** `xcrun metal --version` → "missing Metal Toolchain" (Xcode 27.0, 27A266a).
