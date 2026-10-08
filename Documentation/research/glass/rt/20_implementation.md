# 20 · Ray-traced glass reflections (G14): P0 + P1 implementation

2026-10-08 · visual chat (游戏视觉) · workflow glass-rt-track, implement stage (retry after the 2026-10-07 session limit) · **status: IMPLEMENTED in the clone `W/proj_rt`, verified in engine, NOT merged** (promotion list in §9)

`W` = `/Users/redwang/FrontRoomsVisualWork`. Design: `10_rt_glass_design.md` (§ numbers below refer to it unless stated). Earlier reports: `01_code_review.md`, `02_runtime_probe.md`, `03_research.md`. Codex audit for this track: `../../codex_audit/10_review_rt.md` (F1–F8).

---

## 0. Short answer (for Red)

1. **It works in your real game scene, on your M3 Max, with hardware ray tracing.** Rays start from the glass pixels the game actually drew (a raster prepass, depth-tested), never from re-traced camera rays. The traced reflection goes into the glass shader before tonemapping, so it is graded, bloomed and fogged with the scene. MapWorld is not touched.
2. **Correctness tests pass.** The traced glass lines up with the drawn glass (IoU 0.996–0.999 in all five views); 0 % sky misses (the prototype: 16–20 %); a broken pane leaves the reflection on the same frame (0 px; prototype 159,533 px); a moving Relay copy is right on the same frame (0 stale px; prototype 177,035); an occluding panel in front of the glass gets 11 stray interior pixels (prototype 137,551); flickering lamps are in the reflection on the same frame (correlation 1.000 for all 20 lenses); RT traced at weight 0 changes 0 px.
3. **Hit shading matches the raster closely.** Whole-frame parity (primary rays through the hit shader vs the raster, post and SSAO off): mean luminance ratio 1.011. Ceiling and lens pass the design bar; wallpaper and carpet are within 1 % on average luminance but miss the per-pixel bar (mean error 3.5 % / 4.2 %, bar 3 %).
4. **GPU cost (1080p, editor, shared machine, stage counters):** a window at 1.5 m costs 2.3–3.0 ms on High and 2.3–4.0 ms on Ultra. A pane that fills the screen (0.55 m, the break-shot pose) costs 5.2 ms (High) / 6.3 ms (Ultra). That is over the design estimate (1.3–2.0 ms typical, 2.1–3.1 ms break shot) and over the destruction plan's +2 ms shot budget. The ablation (§5.3) shows why: the per-hit loop over all lamps in range (86–89) and the shadow rays. Lamps capped at 4 → 2.7 ms.
5. **Main-thread cost is still over budget while chunks stream:** quiet frames p50 ≈ 0.4–0.7 ms, p99 1.5–1.7 ms (bar 0.5 ms); streaming frames p99 10 ms (bar 1.5 ms). It was p99 8 ms / max 20.6 ms on main (codex audit F3), and the worst 52 ms spike is gone (§5.4). The rest is P2 work (map events instead of scanning, copies off the main thread).
6. **Codex audit findings for this track are fixed in the clone:** F1 fail-closed guard (proved with an injected fault: 2 log lines, 0 ZBinningJob errors, frames keep rendering, RT goes Off), F4 release at Play exit (0 meshes / 0 MB left), F5 no edit-mode opt-in, F6 iOS/Android/Linux strip the RT shader code, F8 the wallpaper is shaded as paper × print (no red walls), the §4.3 "P5 fail" is a harness artefact (0 px when measured within one frame), the dead composite shader and empty folder are deleted.
7. **What you need to decide:** D1 default quality (today: High at quality level 3–4, Ultra at 5); D6 the break-shot budget (§5.3); D2 whether the player should have a body in dark windows. Faint reflections in lit-to-lit windows are physically right (8 % pane F0) but the Relay 2 m behind you is hard to see in a lit window (§4 P1-B4 note).

---

## 1. Starting point, and how this stage ran

- **Main already holds most of G14.** Codex promoted the pre-wipe `proj_rt` copy on 2026-10-03 (`8ef5b64`, `9e754a2`, `75cfdff`) and merged the glass-shader hook G-1/G-2/G-3 on 2026-10-04 (`9eddc35`, `bf2e883`, `70644f0`). Main's G14 has not changed since; `git diff 279c144 ee5c9bb` touches no G14 file (re-checked 2026-10-08 00:0x).
- **This stage's clone.** `W/proj_rt` = main's working tree at 2026-10-07 16:36 (`279c144`, committed in the clone as `1d24171 baseline`) + tools. The 2026-10-04 implement work (lost in the 2026-10-05 wipe) was replayed from its transcripts (`W/rt_recover/`), then 17 patch steps were applied on 2026-10-07/08 (`W/rt_work/patches/p1…p16`, plus the geometry-page prefetch in §3). All of it is the delta in §3.
- **Re-check on today's main.** A fresh clone `W/proj_rt_v` (made with `W/tools/mkclone.sh`, main `ee5c9bb`) + exactly the §9 promotion set was run through the same harness (run v1, §4.3).
- **Runs used in this report** (harness `Assets/Editor/RT/FrontRoomsGlassRTVerify.cs`, `-executeMethod FrontRoomsGlassRTVerify.RunBatch`, Unity 6000.3.10f1 batch with graphics, `-buildTarget OSXUniversal`, main scene `FrontRooms3D.unity`, seed 516574485, test pane `(282,206)-(282,207)`, game camera 76°, 1920×1080, 4× MSAA, full post):

| Run | When | What | Machine load (1-min avg) | Used for |
|---|---|---|---|---|
| r8 | 2026-10-07 23:29–23:32 | all phases, dylib `007ad06b` | 47–70 (285 in the last minute) | every result except the walk; timings |
| r9 | 23:36–23:38 | ablation + walk, final dylib `c7c3351b` | 220–320 | the walk (P0-A7), the ablation |
| v1 | 2026-10-07 23:46 – 2026-10-08 00:14 | all phases on main `ee5c9bb` + the promotion set, up to the Office phase (25-min deadline) | 360–520 | correctness on today's main (§4.3); its ablation |
| r1–r7 | 2026-10-07 17:00–19:23 | development runs | 556–1,000 | not cited for numbers |

- **Load warning.** 15–25 other Unity batch jobs and Blender jobs shared this Mac during every run. G14's own counters (pixel counts, IoUs, its main-thread stopwatch, GPU stage counters at encoder boundaries) are trustworthy; whole-frame GPU times are not. Where a timing matters I give the 10th percentile of interleaved rounds (the least disturbed sample) and the median. An idle-machine player measurement (P1-B8) is still owed (§8).
- Raw outputs: `W/rt_work/r8_out/` (77 frames, `verify_log.txt`, `metrics.tsv`, `walk_frames.tsv`, `lamp_sync.tsv`, `unity_r8.log`), `W/rt_work/r9_out/`, `W/proj_rt_v/Verification/rt_p0p1/` (v1). Sheets: `Tools/rt/compose_rt_sheets.py` (clone) → `rt/images/20_*.jpg`.

---

## 2. The system as built (P0 + P1)

### 2.1 Frame order (desktop macOS, Metal, Play Mode or player, opted-in Base game camera, quality ≠ Off)

| # | Where | What |
|---|---|---|
| 1 | URP DepthNormals prepass | unchanged; gives `_CameraDepthTexture` (full opaque depth) |
| 2 | **FRGlassRT prepass** (raster), `AfterRenderingPrePasses` | Draws only the receiving glass: rendering-layer bit 30 = the glass shader's own `FRGlassRTPrepass` pass (contract G-1: roll/smudge/crack normal and per-pixel smoothness), bit 29 = `Hidden/FrontRooms/GlassRTPrepass` for materials without that pass. Writes GlassDepth (R32F linear eye depth) and GlassNormal (RGBA16F normal + smoothness); a transient 1× D32 keeps the front-most glass; fragments behind opaque depth (+1 mm + 0.05 %) and the 30 mm cube's edge faces are clipped |
| 3 | **FRGlassRT trace** (unsafe pass → `IssuePluginEventAndData`) | Native render-thread event in Unity's own command buffer (§2.2). Then sets `_FR_GlassRTReflection` (RGBA16F; RGB = linear reflected radiance, A = 1 + eye depth tag), `_FR_GlassRTWeight` = 1, `_FR_GlassRTFade` = (0.45, 0.70), `_FR_GlassRTRollScale` = 0.075 and the keyword `_FR_GLASS_RT` |
| 4 | Opaques, skybox, transparents | `FrontRooms/Glass` (main's shader, unchanged) replaces its environment term with the RT radiance where the tag matches, still × its own Fresnel (`_PaneF0` 0.08) and grime, then fogs |
| 5 | **FRGlassRT end**, `AfterRenderingTransparents` | keyword off, weight 0 (no other camera reads a stale texture) |
| 6 | Post (ACES, grade, halation, grain) | the reflection is graded and bloomed with the scene |

The trace used to run at `BeforeRenderingTransparents` (design §1.2). It moved to `AfterRenderingPrePasses` on 2026-10-07 because there it does not split URP's merged opaque + transparent native render pass (the 2026-10-04 run 8 measured 1.07 ms at 1440p for that split). Today's interleaved test cannot separate the two placements from noise (empty-pass p10 within −1.4 to +0.7 ms of RT Off in all four cases, §5.2); the harness keeps a switch (`HarnessLateTrace`) to repeat it on an idle machine.

### 2.2 Native plugin (`libFrontRoomsMetalGlassRT.dylib`, render thread only)

- **No main-thread Metal encoding, no waits.** The C# side pushes ops (add/release mesh, materials, textures, cameras, reset) into a mutex-protected queue and a per-frame packet; the render event applies them, builds BLAS/TLAS, traces and resolves, all inside Unity's current command buffer (`EndCurrentCommandEncoder` + `CurrentCommandBuffer`, never commit). `waitUntilCompleted` appears nowhere (grep: 0).
- **BLAS cache with lifetime safety.** One BLAS per Unity Mesh (key = instance id + change counter), one geometry descriptor per submesh, compaction a few frames later. The plugin owns copies of all geometry: readable map meshes are copied on the main thread into shared 4 MB geometry pages (bump allocator; the next page is now pre-allocated on a GCD worker, §3); non-readable kit meshes are gathered once on the GPU from Unity's buffers, which are released when that command buffer completes. Released meshes, BLAS, pages and textures go on a retire list tagged with the frame serial and are freed only after the completion handler reports that serial done.
- **TLAS every frame** from the packet: ring of 3 slots that grows to 8 if all are still in flight (a frame never reuses an older slot's data). Caps: High 2,048, Ultra 4,096 instances (destruction contract: ≥ 1,024).
- **Residency.** `MTLResidencySet` on macOS 15+ (log: "residency set"), `useResource` fallback otherwise; touched only on the render thread.
- **Hazards.** Unity's camera textures report `hazardTrackingMode` = tracked (log: "hazard depth 2 / out 2"), so the event barriers of design §1.2 c/f are off by default; they are forced on only if any texture is untracked. Forcing them on costs 0.8–5.7 ms of whole-frame GPU (§5.2) and changes 0 px.
- **Timers.** `MTLCounterSampleBuffer` stage-boundary counters (AS, trace, resolve), published by the completion handler. Lock-free stats (atomics) since 2026-10-07: the old stat mutex let a preempted completion thread stall the main thread.
- **Capability gate (fail closed).** Plugin loaded, Metal device, `supportsRaytracing`, `supportsFamily(MTLGPUFamilyApple9)` (M3/M4), Metal3, macOS 14+, async kernel compile done (218 ms in r8, off the main and render threads), and a GPU layout self-test of every shared struct (FRInstance 128 B, FRMaterial 256 B, FRLamp 64 B, FRFrame 496 B). **M1/M2 (Apple7/8) are Off** (design D5, P2 tier). Any GPU command-buffer error or managed exception during the per-frame build turns RT Off for the session (one log line, weight 0, the glass keeps its zone cube).
- **Build-time validation.** `NativePlugin/build_frontrooms_metal_glass_rt.sh` embeds `FRGlassRTShared.h` + `FrontRoomsGlassRT.metal`, compiles them with the OS Metal compiler in `tools/frglassrt_validate`, creates all 7 pipelines and runs the layout test before linking; a shader error fails the build, not the game. arm64, macOS 13 minimum, `@rpath` install name. No Metal Toolchain download was needed (D4 stays optional).

### 2.3 Scene registration (C#, main thread, no MapWorld edits)

- The map is found once; each frame a cheap diff of `map.transform`'s chunk roots and their child counts finds new, changed and dropped chunks. A chunk scan gathers its renderers once and registers them in slices of ~1 ms per frame (at least 4 per slice); dressed rooms are caught by the child-count change.
- Skips: disabled, `forceRenderingOff`, shadows-only, static-batched, particles/trails, layers outside the camera mask; under a LODGroup only LOD0.
- **Receivers** are visible renderers whose material has `_RTReceive = 1` (today `Glass_Window` on the map panes and the window kit's slab); the `FrontRoomsMetalGlassTarget` marker is only a hint. The disabled gameplay pane is never a target.
- **Same-frame liveness.** Every receiver, glass and dynamic instance is checked every frame (`renderer == null`, disabled, inactive, `forceRenderingOff`): it leaves the TLAS on the frame it leaves the raster. Far dynamic instances (> 15 m) are re-read every 8th frame.
- **Static block cache.** Shell, prop and lens records do not move; they are cached around the camera (rebuilt when the registry changes or the camera moves 4 m) and copied after the priority records each frame.
- **Lamps.** The `Light` components enabled this frame within 30 m (≤ 128, nearest first; 85–89 at the test pane), with URP's exact distance/angle attenuation and `color.linear × intensity`, plus the directional fill. Each troffer lens is read with its fixture's lamp every frame within 34 m (lit or not, so stuttering lamps stay in sync), 48 per frame round-robin beyond.

### 2.4 Hit shading (P1, `FrontRoomsGlassRT.metal`)

- **FrontRooms/Surface** rebuilt as `Frag` does: world-planar metre UVs (`_TileSize`, `_BaseMap_ST`) or mesh UV0 (`_FR_MESH_UV`), base × colour, mask (smoothness, cavity), normal map in the planar TBN, macro tone/dirt, floor grime and ceiling streaks with the instance's `_CeilingHeight`, emission. **Wallpaper (`_FR_PRINT`)**: the paper modulation in `_BaseMap` × print frame 0 (`_PrintTex`) through the ink ramp (`_InkGround/Mid/Deep/Cream`), in linear light, exactly as `PrintAlbedo` (codex F8). The raster has no live flipbook driver in main yet, so frame 0 matches what the wall shows.
- **URP/Lit** props, doors, keys, Relay parts: UV0 from barycentrics, base, metallic/smoothness, normal map, emission.
- **Textures**: trilinear + 16× anisotropic from ray-cone gradients (the raster's anisotropic setting), bindless texture table (102 textures, 95–100 materials in the run).
- **Lenses**: emission = this frame's value, linear (lens radiance ratio RT/raster 1.001).
- **Lights**: Lambert + GGX as URP's BRDF. **Shadow rays** only for lamps whose `shadows != None` this frame, against the raster's shadow casters only (new instance mask 0x20: lenses, glass and shadow-less props let light through, as in the shadow maps). High: 4 nearest shadowed lamps, fill × 0.82; Ultra: up to 16, plus a fill ray.
- **Ambient** SH from `RenderSettings.ambientProbe` × cavity; specular ambient from the zone cube. **Fog**: the remainder of the full path's exp² fog. **Miss**: the zone cube in the ray direction (coverage 1, so the hook's `rt − env` cancels exactly).
- **Thin glass**: other panes are see-through in reflection rays (× (1 − F)·0.9, 1 layer on High, 2 on Ultra); the back-surface image of the 6 mm pane (n 1.52) in the resolve; only the front face of the 30 mm cube is ever drawn into the prepass, so no double reflection from the cube.
- **Smudges**: deterministic bilateral blur in the resolve from roughness, hit distance and view distance; the hook's fade moved to (0.45, 0.70) so smudges blur the live room instead of swapping to the cube.
- **Anti-aliasing**: luminance-edge filter inside one depth tag (strength 0.8 High, 0.6 Ultra); Ultra adds up to 3 more rays at high-contrast pixels (≤ 15 % of glass pixels).
- **Temporal accumulation when still** (the task's ask; the design had none): only while the view-projection matrix and a signature of dynamic transforms, near lens emission and lamp colours are unchanged; up to 32 frames; neighbourhood-clamped; resets on any change, so a moving camera or a flickering lamp never ghosts.

### 2.5 API for the fracture (destruction §4.7, P4)

`FrontRoomsGlassRT.Register(renderer, flags, windowId)`, `Unregister`, `SetEnabled` (same frame), `PrepareMeshes(source, ranges, notBeforeFrame)` (one batched BLAS per range from one CPU buffer, never before `notBeforeFrame`), `IsPrepared`, `MeshChanged`, `RefitMesh` (Deforming only), `TryGetStats`; per-instance flags Glass, FractureEdge, Shard (mask 0x04, out of reflection rays), EmissiveLens, Dynamic, Relay, Door, MeshUV, SurfacePlanar, Deforming, ReflectionOnly; window id per instance. Every member is a no-op off macOS. Measured in r8: 150 ranges (3,000 triangles) built over 7 frames from `notBeforeFrame + 3`, 24 builds and 1.4–2.5 ms AS per frame; a 20,000-triangle Deforming mesh refits for +0.36–1.24 ms (median 0.65 ms) of AS time per frame.

### 2.6 Quality and settings

- Off / High / Ultra (§2.6 of the design). Default while supported: Ultra at quality level 5 (the Mac player default), High at 3–4 (the editor), Off at 0–2. Overrides: PlayerPrefs `FrontRooms.GlassRT` and `-frGlassRT off|high|ultra`.
- A camera traces only if `FrontRoomsPostStack.ConfigureCamera` opted it in **in Play Mode or a player** (edit-mode cameras are no longer opted in, codex F5), it is a Base Game camera, and ≥ 1 receiver is visible. The trace is dispatched over the screen rectangle of the visible receivers only; with no receiver in view nothing is enqueued (title corridor, most corridors).

### 2.7 How the WebGL (and other non-Mac) paths stay free of it

- There is still ONE URP renderer asset (`FrontRooms_URP_Renderer.asset`, used by WebGL at quality level 3 too). The feature entry stays in it, provably inert off macOS:
  - the feature class compiles everywhere (so no missing script), but its body, every P/Invoke, the RT system and the camera component compile only under `UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX`;
  - player-script compile per target (isolation check, clone, 2026-10-07 20:06): WebGL, iOS and Windows `Assembly-CSharp.dll` contain 0 `FRGlassRT_` imports, 0 `FrontRoomsGlassRTSystem`, 0 `FrontRoomsGlassRTCamera`; macOS has them (20 / 10 / 3);
  - the dylib importer is Editor (OSX, ARM64) + Standalone OSX (ARM64) only, preloaded, every other platform off (read back);
  - shaders: on WebGL the prepass shader and the glass's `FRGlassRTPrepass` pass are stripped (and the glass track strips `_FR_GLASS_RT` there); on iOS, Android, tvOS, visionOS and Linux the RT stripper now also removes the `_FR_GLASS_RT` variants (codex F6); macOS keeps everything; Windows keeps them for P3 (inert).
- So WebGL output and variant counts are unchanged by G14. When the WebGL plan builds its own `FrontRooms_URP_WebGL_Renderer.asset` (its Phase 1.1), the entry disappears from the Web path entirely; its pre-flight already fails a Web build that lists this feature.

---

## 3. Every file changed or added in `W/proj_rt` (relative to main `ee5c9bb`)

The full text diff (21 files) is saved as `rt/20_code/g14_p0p1_vs_main_ee5c9bb.diff`.

| Path | Change | What it does |
|---|---|---|
| `NativePlugin/FrontRoomsMetalGlassRT.mm` | M | Geometry pages: readable meshes go into ranges of shared 4 MB pages (was one `MTLBuffer` per mesh: 0.46 ms each on the main thread); the next page is pre-allocated on a GCD worker (r8: one 24-vertex upload took 52 ms on a page change; r9: slowest upload 1.7 ms). Lock-free atomic stats (no stat mutex shared with completion threads). Event barriers only when a camera texture is not hazard-tracked (`FRGlassRT_SetBarrierMode` for the harness A/B). Per-camera TLAS ring grows 3 → 8 instead of reusing a slot still in flight. Native submit timing stat. Scene reset also drops the texture table (codex F4). |
| `NativePlugin/FRGlassRTScene.mm`, `FRGlassRTScene.h` | M | Mesh ranges inside shared pages (`posOffset`); a reference count per geometry buffer (a page is retired when its last mesh goes; was an O(n) scan per release); retired pages or textures that are referenced again stay resident; ≤ 24 BLAS builds per frame (≤ ~2 ms; was 64 → 2.4–3.6 ms frames); the ring and the `allTracked` hazard flag. |
| `NativePlugin/FRGlassRTShared.h`, `FRGlassRTLayout.h` | M | Material record 160 → 256 B with the print layer (`printST`, four inks, `texPrint`, flag `FR_MAT_PRINT`); instance mask `FR_MASK_CASTER` 0x20 and shadow rays = casters only; the layout self-test checks the new fields. |
| `NativePlugin/FrontRoomsGlassRT.metal` | M | Anisotropic ray-cone texture gradients (matches the raster's 16× aniso); the wallpaper paper × print sandwich; the stronger 3×3 edge filter at high-contrast pixels; shadow rays use the caster mask. |
| `Assets/Plugins/macOS/libFrontRoomsMetalGlassRT.dylib` | M (binary) | Rebuilt from the sources above with the clone's build script: sha1 `c7c3351b898a1c6a59666249169c946341a8fc52`, 219,472 B (main: `56a35164…`, 201,280 B). Its `.meta` is unchanged (main's GUID `9b3bca94…`, platform settings already correct). |
| `Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRTSystem.cs` | M | Fail closed for the session (codex F1); no edit-mode opt-in (F5); Play-exit release with an editor pump until the native scene is empty (F4); sliced chunk scans with a cursor; the cached static block; lens reads tied to their fixture's lamp; the wallpaper material record; the caster mask; prepass bit chosen by whether the material has the `FRGlassRTPrepass` pass; reused upload scratch (no per-mesh garbage); allocation-free SH and lamp sort; pinned frame packet; harness hooks (`HarnessThrow`, `LampCap`, stats). |
| `Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRTNative.cs` | M | P/Invoke and struct changes matching the native side (Material 256 B, `MaskCaster`, `MatPrint`, new stats `SubmitMs`, `SubmitMsMax`, `Barriers`, `GeoPages`, `GeoSpareHits`, `FRGlassRT_SetBarrierMode`, `SubmitFrame` by pinned pointers). |
| `Assets/Scripts/Rendering/FrontRoomsMetalGlassRTRendererFeature.cs` | M | Trace at `AfterRenderingPrePasses`; try/catch around `EnsureTargets` / `SendTargets` / `BuildFrame` → `FailClosed` (F1); harness switch for the old placement. Same class and script GUID `8c7c221f…`, so the renderer asset is untouched. |
| `Assets/Shaders/GlassRT/FrontRoomsGlassRTPrepass.shader` | M | Occluder test tolerance 1 cm + 0.2 % → 1 mm + 0.05 % (the window stops sit 3 mm in front of the glass; the old tolerance let the stops through). Same as main's glass shader prepass pass. |
| `Assets/Editor/RT/FrontRoomsGlassRTWebGLStripper.cs` | M | Also strips the prepass, the glass prepass pass and the `_FR_GLASS_RT` variants on iOS, Android, tvOS, visionOS and Linux (F6); WebGL, macOS and Windows behave as before. |
| `Assets/Editor/RT/FrontRoomsGlassRTIsolation.cs` | M | Adds iOS to the player-script isolation check and reports the stripper decisions per target. |
| `Assets/Editor/RT/FrontRoomsGlassRTVerify.cs` | M | The acceptance harness: the window kit's glass slab as a receiver; occluder/Relay/broken/stream/dark/Office phases; lamp sync with lag; temporal still + strafe crawl; cameras; capacity; fracture; walk with registration profile; cost with interleaved rounds and p10; placement and barrier A/B; parity per material at High and Ultra; ablation; window pairs and the same-frame tag proof; the fail-closed fault injection; the Play-exit release check. Editor-only, `#if UNITY_EDITOR_OSX`. |
| `Assets/Editor/RT/Proof.meta`, `Assets/Editor/RT/Proof/FrontRoomsGlassMainBaseline.shader` (+ `.meta`) | A | Harness fixture for the optional `shaderproof` phase: the glass shader without its G14 blocks (`Hidden/FrontRooms/GlassMainBaseline`). Editor folder, never in a build. New GUIDs `4817fc9b…` (folder), `267e88a1…` (shader). Optional to promote. |
| `Tools/rt/compose_rt_sheets.py` | A | Builds the `20_*.jpg` sheets and charts from a harness output folder (`W/venv/bin/python Tools/rt/compose_rt_sheets.py <in> <out>`). Outside `Assets`, no `.meta`. |
| `Assets/Shaders/FrontRoomsMetalGlassRTComposite.shader` (+ `.meta`) | D | ChatGPT's after-post composite (design §4.1 "Delete"); nothing references its GUID. |
| `Assets/Scripts/Rendering/MetalGlassReflection.meta` (+ the empty folder) | D | Empty folder from the prototype (design §4.1 "Delete"). |

**Files G14 relies on that are already in main and unchanged here:** `Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRT.cs` (public API), `FrontRoomsGlassRTCamera.cs`, `Assets/Scripts/Rendering/FrontRoomsMetalGlassRT.cs` (shims: the `FrontRoomsMetalGlassTarget` hint MapWorld still adds, and the `[Obsolete] Ensure()`), `Assets/Editor/RT/FrontRoomsGlassRTImporter.cs`, `Assets/Editor/Rendering/FrontRoomsGlassRTStripper.cs` (glass track), `Assets/Resources/Rendering/FrontRoomsGlass.shader` (hook G-1/G-2/G-3), `Assets/Settings/FrontRooms_URP_Renderer.asset` (feature + `prepassShader` reference), `Assets/Scripts/Rendering/FrontRoomsPostStack.cs` (the `OptIn` line), `NativePlugin/build_frontrooms_metal_glass_rt.sh`, `NativePlugin/tools/frglassrt_validate.mm`.

**Glass shader.** No change is needed: main's `FrontRooms/Glass` already carries exactly the interface the brief asks for (`_FR_GlassRTReflection` linear HDR RGB + A tag/coverage, `_FR_GlassRTWeight`, the env/planar term replaced by RT radiance × the shader's own Fresnel and grime) plus G-1…G-3. The clone's copy is byte-identical to main's. The map's panes use it in the test through the harness's clone-only switch (panes swapped to `Glass_Window` at runtime: 18 panes), not a MapWorld change.

---

## 4. Acceptance (design §4.1–4.2)

### 4.1 P0

| # | Test | Bar | Result (r8 unless stated) | Verdict | Frames |
|---|---|---|---|---|---|
| A1 | Runs in the main game, MapWorld unchanged | RT on line; 1 event per frame; 0 validation errors | "on: Apple9, High, kernel 218 ms (async), layout OK, residency set, hazard depth 2 / out 2, event barriers off (tracked), timers stage-boundary counters"; cameras test: +100 trace events for 100 frames; 0 command-buffer errors in every phase. **The Metal validation layer was not run in engine** (only on the standalone smoke harness, 2026-10-04) | PARTIAL | — |
| A2 | Alignment | IoU ≥ 0.995, boundary ≤ 1 px | prepass vs the glass's own forward coverage (≥ 50 % of 4× MSAA): IoU 0.9969 / 0.9967 / 0.9969 / 0.9963 / 0.9986 (v1–v5); pixels more than 1 px off the boundary: 0 / 1 / 39 / 45 / 479 (of 201k–1.31M). Traced vs prepass IoU 0.989–0.997 (the 1 px edge ring is filled by dilation) | PASS (boundary: FLAG at 50°–65° and 0.55 m) | `20_P1_v1…v5_*.jpg` |
| A3 | Orientation | mirrored correctly | red is right of and above green in both the RT and the planar mirror; centroids within 5–7 px | PASS | `20_P0_orientation.jpg` |
| A4 | Transmission kept, reflection graded | mean ΔY ≤ +4/255; "no pixel darker by > 1/255" | mean ΔY High − Off: −0.97 / −1.25 / −1.25 / −4.12 / −0.51 per 255 (p1 −9 to −35, p99 +4.2 to +6.7). RT traced at weight 0 vs Off: 0 px in every view (also with barriers forced on; a second Off frame: 0 px). Pixels darker by > 1/255: 112k–443k: the RT term replaces the brighter zone cube with the real (darker) room, so the second clause cannot hold by design; transmission is unchanged (weight-0 test) | PASS (bar 2: not applicable, see note) | `20_P1_v*.jpg` |
| A5 | Occlusion and lifetime | 0 px / same frame | unregistered panel in front: 28 prepass px and 11 interior traced px on 268,061 panel px (prototype 137,551); Relay copy moved 0.7 m: 75,143 px change the same frame, 0 differ from a fresh registration (prototype 177,035 stale); broken pane: 437,356 → **0 px on the break frame**, receivers 38 → 37 (prototype 159,533 until the next rescan); destroyed mesh: 0 px next frame (prototype 17,262); `MeshChanged` vertex edit: the reflection moves 90.4 px the same frame | PASS (occluder 11 px, edge only) | `20_P0_occluder.jpg`, `20_P0_relay_moving.jpg`, `20_P0_broken_pane.jpg`, `20_P0_stream_test.jpg` |
| A6 | No sky indoors | misses ≤ 0.5 % | 0.00 % in all views, Office and dark-beyond included (prototype 16–20 %) | PASS | — |
| A7 | No hitch, no flicker | 0 blank; main p99 ≤ 0.5 (quiet) / ≤ 1.5 ms (streaming); encode p99 ≤ 0.2 ms; 0 waits | r9, 600 frames at 4 m/s, 30 chunk roots seen, 581 traced: **0 blank**, 0 command-buffer errors, 0 GC frames, `waitUntilCompleted` 0. Main thread p50 0.72 ms, **quiet p99 1.73 ms, streaming p99 10.3 ms, max 10.3 ms**; render-thread encode p50 0.28, p99 1.24 ms. (r8 with the old allocator: quiet p99 1.46, streaming p99 58.6 because of one 52 ms upload.) Main before this stage (codex F3): p50 2.78, p99 8.03, max 20.6 ms | FAIL (main thread and encode over budget; P2) | `20_P0_walk_chart.jpg`, `20_P0_frame_delta_chart.jpg` |
| A8 | Capacity and priority | cap holds, priority all present, nothing within 20 m dropped | +3,000 synthetic instances (6,574 registered): High TLAS 2,048 (cap), Ultra 2,608; 699 priority instances all kept; 0 dropped within 20 m | PASS | — |
| A9 | Cameras | RT only on the opted-in camera; 0 re-creations | second 960×540 camera: no RT component; +100 trace events for 100 frames (game camera only); 0 target re-allocations; Scene view returns before enqueueing | PASS | — |
| A10 | Isolation | no `FRGlassRT_` in WebGL; importer; stripper | §2.7: WebGL/iOS/Windows 0 imports, macOS 20; importer read back correct; stripper removes 100 % of the prepass on WebGL and of prepass + RT variants on iOS | PASS | — |
| A11 | GPU timer + pass split | per-stage ms; split ≤ 0.3 ms or F1 | stage counters per view (§5.1); placement A/B within noise (§5.2); the trace now runs where no split happens | PASS (timer) / FLAG (split not resolvable under load) | `20_P1_cost_chart.jpg`, `20_P1_cost_p10_chart.jpg` |
| A12 | Player smoke test | 0 crashes in a macOS player, 3-min autopilot | **not run** (§8) | OPEN | — |
| A13 | Fracture API | 150 ranges ≤ 4 frames at ≤ 2 ms; 20k refit ≤ 0.3 ms | 7 frames after `notBeforeFrame` (none before), 24 builds and 1.4–2.5 ms AS per frame; refit median +0.65 ms. Inside the destruction window (0.70–1.0 s = 18 frames) but over the design's frame and ms bars | FLAG | — |

### 4.2 P1

| # | Test | Bar | Result | Verdict | Frames |
|---|---|---|---|---|---|
| B1 | Parity (primary rays through the hit shader vs raster; post and SSAO off; linear) | per class mean \|Δ\| ≤ 3 %, p95 ≤ 8 %; lens ratio 0.97–1.03 | 2,072,931 of 2,073,600 px hit; mean luminance raster 0.240, RT 0.243 (**ratio 1.011**). L0 wallpaper (1.27M px): mean 3.5 %, median 1.7 %, p95 13.0 %, ratio 1.006. L0 carpet (404k): 4.2 % / 3.4 % / 13.1 %, ratio 1.039; at Ultra (16 shadowed lamps) 3.0 % / p95 7.3 %. Ceiling (373k): 0.7 % / 0.2 % / 2.8 %, ratio 0.987. Level 0 lens (25k): ratio **1.001**, median 1.1 %, p95 7.7 % (mean 12 % from lens borders). Walnut prop (1.6k): ratio 0.946. Office materials were not in the parity frame | PARTIAL (ceiling, lens PASS; wallpaper, carpet over the per-pixel bar at High) | `20_P1_parity.jpg` |
| B2 | Mirror reference (planar mirror camera vs RT) | ratio 0.9–1.1 in ≥ 95 % of 64-px blocks | v1 89.1 % (median ratio 1.047), v2 94.3 % (1.030), v3 98.5 % (1.015) | PARTIAL (v3 PASS; v1/v2 just under) | `20_P1_v1…v3_*.jpg` (top-right panel) |
| B3 | Lamps in sync | r ≥ 0.99 over 120 frames; off the same frame | 20 lenses in the reflection; the 7 that change ≥ 10 %: same-frame r **1.000** each (1-frame lag would give −0.42 to 0.96); a lamp switched off: reflection 4.058 → 0.087 (2.1 %) on that frame and back the next | PASS | `20_P1_lamp_sync_chart.jpg` |
| B4 | Art-review set | judged by eye | head-on, 50°, 65°, 0.55 m; dark room beyond; Office; the Relay 2 m behind — each Off / mirror reference / High / Ultra / traced radiance. Not judged here (verify:art-direction stage). Note: in a lit-to-lit window the Relay behind the player is clearly in the traced radiance but hard to see in the final frame (8 % F0 against a bright room beyond); in the dark-beyond window the pane turns into a readable mirror. No HDR-off frame was made | PENDING | `20_P1_*.jpg`, `20_P0_relay_moving.jpg` |
| B5 | Thin glass back image | 2–4 px at 0.5 m, weight 0.45–0.50 | implemented in the resolve; **not measured** by the harness | OPEN | — |
| B6 | Anti-aliasing | High ≤ 60 %, Ultra ≤ 35 % of the unfiltered crawl | relative crawl at the 10 % highest-contrast reflection pixels, 1 cm/frame strafe: unfiltered 4.58 %, High 4.39 %, Ultra 4.35 % (−4 % / −5 %). The raster's own pixels in the same frames: 8.67 %. The reflection already crawls half as much as the 4× MSAA raster, so the filter has little left to remove | FLAG (bar not met; no visible crawl) | `20_P1_edge_crawl.jpg` |
| B7 | Smudges | a blurred lens, not the cube | blur and fade (0.45, 0.70) implemented; **not measured** | OPEN | — |
| B8 | Cost (player, idle, 1440p) | v2 High ≤ 2.0, Ultra ≤ 3.5; full pane High ≤ 5, Ultra ≤ 7 | editor, shared machine, p10 of stage counters: v2 1440p High 4.39 ms (AS 0.38, trace 3.76, resolve 0.26), Ultra trace 5.58; v5 1440p High 9.56, Ultra trace 10.50; 1080p v2 High 3.03, v5 High 6.67 ms. **No player / idle measurement** | FAIL (as measured) / OPEN (bar is player + idle) | `20_P1_cost_p10_chart.jpg` |
| — | Temporal accumulation (task) | converges, no ghosting | still camera, clock frozen: index reaches 26 (cap 32), mean \|Δ\| between frames 0.000 → 0.0005; any camera move or lens change resets it | PASS | `20_P1_temporal_still.jpg` |

### 4.3 Re-check on today's main (`ee5c9bb` + the promotion set, clone `W/proj_rt_v`, run v1)

Run v1 (2026-10-07 23:46 → 2026-10-08 00:14; fresh clone, so ~15 min went to compiling shader variants under load 360–520; the harness's 25-min deadline ended it after the Office phase, before walk / pairs / tagproof / fail-closed, which r8/r9 covered with the same code). Today's main differs from the r8 clone in the Q1b hard-edge `_PrintTex`, the window facade r6 and touch/print editor code; none of it is a G14 file. Results:

| Check | r8 (clone at 279c144) | v1 (main ee5c9bb + promotion set) |
|---|---|---|
| Alignment IoU prepass vs raster, v1–v5 | 0.9969 / 0.9967 / 0.9969 / 0.9963 / 0.9986 | 0.997 / 0.997 / 0.997 / 0.996 / 0.999 (same boundary counts 0 / 1 / 39 / 45 / 479) |
| Misses, weight-0 inertness | 0 %, 0 px | 0 %, 0 px |
| Orientation | correct | correct (same centroids) |
| Occluder interior px | 11 | 3 |
| Relay stale px / broken pane / destroyed mesh | 0 / 0 / 0 | 0 / 0 / 0 |
| Parity: whole frame, wallpaper, carpet, ceiling, lens | 1.011; 3.5 % (1.006); 4.2 % (1.039); 0.7 %; 1.001 | 1.012; 3.4 % (1.007); 4.6 % (1.041); 0.7 %; 1.001 — the hard-edge print shades as on the wall |
| Lamps same-frame r, off-frame ratio | 1.000, 2.1 % | 1.000, 2.1 % |
| Temporal: accumulation, crawl unfiltered / High / Ultra / raster | 26; 4.6 / 4.4 / 4.4 / 8.7 % | 26; 4.7 / 4.6 / 4.5 / 7.9 % |
| Cameras, capacity, fracture | pass; 7 frames | pass; 7 frames |
| Play-exit release | 0 meshes, 0 MB | 0 meshes, 0 MB |

So the promotion set behaves the same on today's main. v1's timings are not used (load 360–520), except its ablation, which ran in a quieter minute (§5.3).

---

## 5. Measured numbers

### 5.1 GPU stage time per view (r8, 1080p, stage-boundary counters, one traced frame each)

| View | Glass px | High: AS / trace / resolve = total ms | Ultra total ms (trace) | Main thread ms (High) | Encode ms |
|---|---:|---|---|---:|---:|
| v1 head-on 1.5 m, level gaze (16.5°) | 447,456 | 0.26 / 1.94 / 0.13 = **2.33** | 4.00 (3.64) | 0.33 | 0.18 |
| v2 head-on 1.5 m, pane centre | 434,712 | 0.26 / 2.01 / 0.13 = **2.40** | 3.16 (2.76) | 0.34 | 0.16 |
| v3 50° | 329,319 | 0.29 / 1.89 / 0.18 = **2.35** | 3.63 (3.23) | 0.59 | 0.17 |
| v4 65° | 201,223 | 0.30 / 2.54 / 0.19 = **3.03** | 2.33 (1.83) | 0.38 | 0.17 |
| v5 0.55 m, pane fills the frame | 1,314,071 | 0.25 / 4.61 / 0.34 = **5.20** | 6.31 (5.69) | 0.48 | 0.18 |
| Office v2 | 436,491 | 0.42 / 5.60 / 0.17 = 6.19 | — | 0.37 | 0.18 |
| Office v5 (Ultra) | 1,316,414 | — | 7.91 (7.34) | 0.37 | 0.20 |
| Dark beyond v2 / v3 (Ultra) | 434,712 / 329,319 | — | 3.45 / 3.37 | 0.45 / 0.61 | 0.20 |

Single frames; the Office numbers ran after the load rose. TLAS: 1,995–2,000 instances (High), 85–89 lamps, 0 misses.

### 5.2 Whole-frame cost (r8, interleaved rounds; p10 = least disturbed, median in brackets)

| View | Off | High | Ultra | High stages (AS + trace + resolve) | Empty passes, new placement | Empty passes, old placement | High with barriers forced |
|---|---:|---:|---:|---:|---:|---:|---:|
| v2 1080p | 6.91 (13.32) | 9.32 (16.01) | 14.40 (20.03) | 3.03 | 7.32 | 7.30 | 14.14 |
| v2 1440p | 10.17 (17.33) | 11.23 (20.07) | 13.74 (23.19) | 4.39 | 8.77 | 9.48 | 16.93 |
| v5 1080p | 7.10 (15.84) | 17.49 (22.17) | 21.11 (26.98) | 6.67 | 7.10 | 6.58 | 19.94 |
| v5 1440p | 9.04 (27.75) | 16.77 (46.94) | 18.85 (51.38) | 9.56 | 9.04 | 8.80 | 17.54 |

- The empty-pass columns sit within −1.4 to +0.7 ms of Off: no measurable split cost at either placement on this machine.
- Forcing the event barriers on costs 0.8–5.7 ms (they serialise Unity's opaque pass behind the trace); with tracked textures they are off and the result is bit-identical (0 px).
- Main thread inside the cost phase: 0.49–0.51 ms (High); encode 0.20–0.22 ms.

### 5.3 Where the trace time goes (r9 ablation, v5 0.55 m, 1080p, High, trace + resolve ms, p10 / median of 18 interleaved rounds)

| Variant | p10 | median | Shadow rays |
|---|---:|---:|---:|
| As shipped (all lamps in range, 4 shadowed) | 7.01 | 22.72 | 3,393,490 |
| No shadow rays | 4.63 | 22.78 | 0 |
| Lamps capped at 16 | 4.65 | 23.10 | 3,280,238 |
| Lamps capped at 4 | 2.67 | 21.44 | 140,191 |
| No back-surface image | 6.23 | 26.43 | 3,393,490 |

The load during r9 was 220–320, so its medians are swamped; the p10 column is the usable one. The same ablation in run v1 (current main, a quieter minute) gave p10 / median: shipped 4.99 / 5.99, no shadow rays 3.97 / 4.62, 16 lamps 3.99 / 4.35, 4 lamps 2.79 / 3.10, no back image 4.92 / 6.14 ms (shadow rays 3,420,337 / 0 / 3,290,976 / 173,177). The back-surface image costs ≈ 0.1 ms. At the break-shot pose about a third of the trace is shadow rays and most of the rest is the per-hit loop over ~86 lamps (the range test still visits every lamp). P2: a light grid / per-hit lamp cull by range, then decide D6 (a "shot budget" of 2–4 lamps gives ≈ 2.7 ms).

### 5.4 Main thread and streaming (r9 walk, final dylib)

- 600 frames at 4 m/s, 14-cell ping-pong over 39 m: 30 chunk roots seen, 581 traced frames, 0 blank, 0 GC frames.
- Main thread: p50 0.72, p99 4.01, max 10.34 ms; quiet frames (510) p99 1.73; streaming frames (90) p99 10.34.
- Registration: 321 new CPU uploads, 189.9 ms in total (0.59 ms each; the native call 184.9 ms of it); slowest single registration 1.80 ms (Office_Wall, 360 vertices). Geometry pages: 3, 2 of them taken from the pre-allocated spare.
- Render-thread encode: p50 0.28, p99 1.24 ms. Native submit max 0.048 ms. Static block rebuilds 8, in-place patches 50.
- The plateau in `20_P0_walk_chart.jpg` (~3.3 ms for ~100 frames) is one chunk's sliced registration: ≥ 4 registrations per slice × ~0.6 ms each on this loaded machine. The copies are a few KB each; the time is page-faulting fresh shared memory and scheduling under load 220–320.
- In views (r8, a quiet stretch) the per-frame main-thread cost is 0.31–0.59 ms: liveness 0.06–0.14, lamps + lens reads 0.22–0.38, records 0.03–0.05, constants 0.01, submit 0.01. The lamp/lens part is the largest; the map's `CopyLampStates` (C7) would remove it.

### 5.5 Memory

Not logged during play in this stage. Codex's measurement on main's code (2026-10-07): BLAS 4.95 MB, geometry 8.31 MB for 1,646 meshes, camera targets 87 MB at 1080p (one camera). After Play exit the clone now holds 0 meshes, 0 MB BLAS, 0 MB geometry, 0 retired objects (F4; r8 and r9).

---

## 6. Codex audit findings for this track (`10_review_rt.md`), resolved in the clone

| ID | Finding | Resolution | Evidence |
|---|---|---|---|
| F1 | No fail-closed guard: an exception in `BuildFrame` froze/blacked out frames + ZBinningJob errors | try/catch in the feature → `FailClosed`: one exception line + one "off for this session" line, quality Off, no more native calls; EndPass keeps the keyword off | r8: fault injected for 12 frames → failed closed 1×, 2 errors, 0 ZBinningJob, 0 Render Graph errors, 11 of 11 frames changed while moving (main as is: 24 errors, 12 of 12 frozen) |
| F2 | G14 live and unverified | P0/P1 harness run on the code (§4) | this report |
| F3 | Main thread 5–10× over budget | improved, not closed (§5.4): p50 2.78 → 0.72 ms, p99 8.03 → 4.01 ms, max 20.6 → 10.3 ms | r9 walk |
| F4 | Native scene kept after Play exit | reset + pump on `ExitingPlayMode`; texture table dropped too | r8/r9: released after 3 events / 4 editor updates; 0 meshes, 0 MB |
| F5 | Edit-mode cameras got a saved RT component | `OptIn` returns when not playing | code; the ads-demo scene owner still has to re-save `FrontRoomsScreenAdsDemo.unity` (its line 1997 holds the component) |
| F6 | iOS/Android/Windows keep RT shader code | stripped on iOS, Android, tvOS, visionOS, Linux; Windows kept for P3 | isolation check (§2.7) |
| F7 | `VISUAL_CHAT_TASKS.md` G14 row wrong | docs owner; not touched here | — |
| F8 | `_FR_PRINT` wallpaper traced as red paper code | real paper × print in the hit shader (not the flat ink tone of `vis_F2_rt_print_albedo.diff`, which is not needed with this) | parity wallpaper luminance ratio 1.006 |
| §4.3 | Shader proof P5 fail at the dead-lamp window | not a hook bug: within ONE frame the +0.5 m tag changes 0 px (P4 with the right tag: 435,162 px), also while the zone cube cross-fades; the proof compared frames one tick apart, and the scene itself changes 2,016,814 px between consecutive frames (lamp clocks, grain) | r8 tagproof phase |
| §5 | Delete the composite shader and the empty folder | deleted (promotion list) | — |

Not this track's: the window-landing VL107 "intact glass in Play" contrast fail. The 1.5 m head-on RT frames here change the pane by −1 to −4/255 on average, so RT is unlikely to explain a 12-luma bar there; the window track should re-check with `-frGlassRT off`.

---

## 7. Contracts and hand-offs

**Map chat (MapWorld, owned by the map chat; no edit here).**
- C1 (delete ChatGPT's `Ensure()` call): done in main by Codex `75cfdff`, accepted by the map chat.
- C2 (delete `pane.AddComponent<FrontRoomsMetalGlassTarget>()`, `FrontRoomsMapWorld.cs:1319` today): still requested; harmless until then (the marker is an inert hint).
- C8 (panes on `Glass_Window`, or the visible slab, in the real build): still needed for Red to see RT outside the harness; the harness swaps the 18 panes at runtime.
- **P2 events (exact signatures, as in design §2.4 and the WebGL plan §2.7; one edit serves both):**

```csharp
public static event Action<FrontRoomsMapWorld> Created;    // end of Awake / CreateEmbedded
public static event Action<FrontRoomsMapWorld> Destroyed;  // OnDestroy, before Release()
public event Action<ChunkHandles> ChunkBuilt;               // end of BuildInto, after built[coord] = chunk
public event Action<ChunkHandles> ChunkReleasing;           // START of Unregister(chunk) (drops, failed builds, live rebuilds)
public event Action<RoomHandle> RoomDressed;                // DressNext / Dress finished a room (one room per frame)
public int CopyLampStates(List<FrontRoomsLampState> into, Vector3 centre, float radius);   // after TickFixtures
public struct FrontRoomsLampState { public int fixtureId; public Light light; public Renderer lens;
    public Color lensEmissionLinear; public bool lightOn, shadowsOn; }
```

These replace the per-frame chunk diff, the 1 s dressing catch-up and the lamp/lens reads (§5.4).

**Glass track.** Nothing to merge: the hook in main is what this build uses. G14 sets `_FR_GlassRTFade` to (0.45, 0.70) and `_FR_GlassRTRollScale` to 0.075 while RT is on (both already declared in main's shader).

**Destruction track (P4, exactly §4.7).** The API of §2.5 is in main and verified here: `Register(slab or stage mesh renderer, Glass | FractureEdge [| Shard], windowId)`, `SetEnabled`/`Unregister` same frame (a broken pane leaves the AS on its frame: 0 px), `PrepareMeshes(source, ranges, notBeforeFrame)` between 0.70 and 1.0 s (7 frames for 150 pieces at ≤ 2.5 ms), shard mask 0x04, per-piece flags and window id, ≥ 1,024 instances, the bow through the prepass normal. Open for P4: `Glass_Fracture` with `_RTReceive = 1`, ≤ 64 piece instances per window, and the shot cost (§5.3, D6).

**Touch / mobile session.** iOS builds lose the G14 prepass and `_FR_GLASS_RT` variants (fewer glass variants, no pixel change).

**Screen-ads demo owner.** Re-save `Assets/Scenes/FrontRoomsScreenAdsDemo.unity` (or remove its `FrontRoomsGlassRTCamera` at line 1997) after this lands (F5).

---

## 8. Known gaps

1. **Player build and idle-machine cost (P0-A12, P1-B8)** not done: every timing here is the editor on a Mac shared with 15–25 other Unity jobs. Needed before D1/D6.
2. **Main thread over budget** while streaming (P0-A7, F3): needs the P2 events (C3/C5/C6/C7), `AcquireReadOnlyMeshData` copies off the main thread, and the lamp list from `CopyLampStates`.
3. **Break-shot GPU cost** 5.2 ms (High, 1080p) vs +2 ms: needs per-hit lamp culling / a light grid (P2), then D6.
4. **Metal validation layer** (`MTL_DEBUG_LAYER=1 MTL_SHADER_VALIDATION=1`) not run in engine.
5. **P1-B5 back image, P1-B7 smudges, the HDR-off frame**: implemented or not applicable, not measured.
6. **Parity** for Office materials, cove base, door veneer and kit props is not in the parity frame (Level 0 only); wallpaper/carpet per-pixel error 3.5–4.2 % (High's 4 shadowed lamps vs the raster's shadow maps explains most of the carpet error: 3.0 % at Ultra).
7. **No SSAO at hits** (design O4); volumetric lamp beams are not in reflections (O3).
8. **Memory** not logged in this stage (§5.5).
9. **M1/M2**: Off (P2 tier).
10. **Windows DXR** (P3) not started.

---

## 9. Promotion list (for the merge stage; this stage wrote into Red's project only `Documentation/research/glass/rt/` and the VL rows of `Documentation/VERIFICATION_LOG.md`)

Base: main `ee5c9bb` (re-checked 2026-10-08 00:2x against HEAD `22bb75f`: that commit touches no path below, so the bases hold). For every path, main's working tree = main HEAD = the clone's baseline (blob hashes below). New package convention: `W/apply/README.md` v2 (`glass_rt_g14.sh`, `_bases.txt`, `_expected.txt`, `_payload/`, `_base/`, `.diff`). Source of the payload: `W/proj_rt` (or `W/proj_rt_v`, identical for these paths).

| Op | Path | Main base blob | New blob |
|---|---|---|---|
| M | `NativePlugin/FrontRoomsMetalGlassRT.mm` | `8f77655f` | `5c08e941` |
| M | `NativePlugin/FRGlassRTScene.mm` | `4e7e5b6c` | `cc9d6b47` |
| M | `NativePlugin/FRGlassRTScene.h` | `e51bbe2f` | `0d78ee56` |
| M | `NativePlugin/FRGlassRTShared.h` | `cf33de5a` | `1ac8d62d` |
| M | `NativePlugin/FRGlassRTLayout.h` | `3adbd148` | `2b70ca10` |
| M | `NativePlugin/FrontRoomsGlassRT.metal` | `7beff91b` | `7c38373f` |
| M | `Assets/Plugins/macOS/libFrontRoomsMetalGlassRT.dylib` (binary; `.meta` unchanged, GUID `9b3bca94…`) | `bdd2a39a` (sha1 `56a35164…`) | `41fd8b92` (sha1 `c7c3351b898a1c6a59666249169c946341a8fc52`) |
| M | `Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRTSystem.cs` (`.meta` unchanged) | `3d0f5dbb` | `802d2444` |
| M | `Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRTNative.cs` (`.meta` unchanged) | `bb7d38cd` | `0aa2ea85` |
| M | `Assets/Scripts/Rendering/FrontRoomsMetalGlassRTRendererFeature.cs` (`.meta` unchanged, GUID `8c7c221f…`) | `a227a689` | `b620e844` |
| M | `Assets/Shaders/GlassRT/FrontRoomsGlassRTPrepass.shader` (`.meta` unchanged, GUID `9e07ae8a…`) | `3e0081a2` | `d9cedde7` |
| M | `Assets/Editor/RT/FrontRoomsGlassRTWebGLStripper.cs` | `85b748d8` | `bba22ea7` |
| M | `Assets/Editor/RT/FrontRoomsGlassRTIsolation.cs` | `be53aa08` | `a3c76168` |
| M | `Assets/Editor/RT/FrontRoomsGlassRTVerify.cs` | `ed23e5d7` | `2ed9fb07` |
| A | `Tools/rt/compose_rt_sheets.py` (no `.meta`, outside `Assets`) | — | `6366cbee` |
| A (optional, harness only) | `Assets/Editor/RT/Proof.meta`, `Assets/Editor/RT/Proof/FrontRoomsGlassMainBaseline.shader`, `…shader.meta` | — | `ec5fc334`, `ffb29c99`, `df02d3aa` |
| D | `Assets/Shaders/FrontRoomsMetalGlassRTComposite.shader` + `.meta` | `d22baeb3`, `6cbda0f4` | — |
| D | `Assets/Scripts/Rendering/MetalGlassReflection.meta` + the empty folder `Assets/Scripts/Rendering/MetalGlassReflection/` (`rmdir`) | `b3922c01` | — |

Blob = `git hash-object` (first 8 hex). The payload in `W/proj_rt_v` is byte-identical to `W/proj_rt` for every path above (checked 2026-10-08 00:1x).

- **Never promote:** `Verification/`, `NativePlugin/build/` (build intermediates), `Library/`, any `* 2.*` iCloud copy, any file in `W/LOCAL_PATCHES.md` (none apply today).
- **After the merge:** nothing to run in MapWorld; the dylib must be the committed binary (Unity keeps a loaded dylib for its lifetime, so Red's open editor needs a restart to load it); the ads-demo scene re-save (F5); the docs owner fixes the G14 row of `VISUAL_CHAT_TASKS.md` (F7) and points it here.
- **Rebuild check for the merge stage:** `sh NativePlugin/build_frontrooms_metal_glass_rt.sh` in a clone must print `layout self-test OK (FRInstance 128, FRMaterial 256, …)` and reproduce the dylib's behaviour; byte-identical output is expected with the same Xcode (Apple clang 21).

---

## 10. Frames (all `rt/images/`, JPG q85, 1600 px wide) and the verification log

| Image | Shows |
|---|---|
| `20_P1_v1_headon_level.jpg`, `20_P1_v2_headon_centre.jpg`, `20_P1_v3_angle50.jpg`, `20_P1_v4_steep65.jpg`, `20_P1_v5_close55.jpg` | per view: RT Off (zone cube), planar mirror reference, RT High, RT Ultra, traced radiance High/Ultra (×2 exposure; magenta = miss) |
| `20_P1_office_v2_headon_centre.jpg`, `20_P1_office_v5_close55.jpg` | the same set in the Office zone |
| `20_P1_dark_beyond_v2_headon_centre.jpg`, `20_P1_dark_beyond_v3_angle50.jpg` | staged dark room beyond: Off / High / Ultra (the pane becomes a one-way mirror) |
| `20_P0_relay_moving.jpg` | the Relay 2 m behind: High, traced radiance, Ultra, moved 0.7 m (same frame) |
| `20_P0_occluder.jpg`, `20_P0_broken_pane.jpg`, `20_P0_stream_test.jpg`, `20_P0_orientation.jpg` | P0-A5 / A3 |
| `20_P1_parity.jpg` | raster \| RT primary rays \| ×8 difference |
| `20_P1_lamp_sync_chart.jpg` | lens vs its reflection over 120 frames |
| `20_P1_temporal_still.jpg`, `20_P1_edge_crawl.jpg` | still-camera accumulation; 1 cm/frame strafe (unfiltered / High / Ultra) |
| `20_P0_walk_chart.jpg`, `20_P0_frame_delta_chart.jpg` | r9 walk: main thread and encode per frame |
| `20_P1_cost_chart.jpg`, `20_P1_cost_p10_chart.jpg` | whole-frame GPU, median and p10 |

**Figma (FRONTROOMS · VISUAL VERIFICATION LOG, section `2595:6093`, page `2099:76`), placed 2026-10-08; rows in `Documentation/VERIFICATION_LOG.md` §3; section grown to 47 rows; cover VL000 recounted (183 checks, 555 images) and the G14 legend set to VL031–037, 181–186, 13 checks:**

| VL | Frame | Check | Verdict | Images (slot order) |
|---|---|---|---|---|
| VL181 | `2847:6157` | Traced glass lines up | PASS | `20_P1_v2_headon_centre.jpg`, `20_P1_v3_angle50.jpg`, `20_P1_v4_steep65.jpg`, `20_P1_v5_close55.jpg` |
| VL182 | `2847:6169` | Occlusion and same-frame life | PASS | `20_P0_occluder.jpg`, `20_P0_relay_moving.jpg`, `20_P0_stream_test.jpg`, `20_P0_broken_pane.jpg` |
| VL183 | `2847:6181` | Hit shading vs the raster | PARTIAL | `20_P1_parity.jpg`, `20_P1_lamp_sync_chart.jpg` |
| VL184 | `2847:6191` | Dark room and Office look | PENDING (art-direction stage) | `20_P1_dark_beyond_v3_angle50.jpg`, `20_P1_dark_beyond_v2_headon_centre.jpg`, `20_P1_office_v5_close55.jpg` |
| VL185 | `2847:6202` | Temporal stability | FLAG | `20_P1_temporal_still.jpg`, `20_P1_edge_crawl.jpg` |
| VL186 | `2847:6212` | RT cost on a shared Mac | FAIL | `20_P0_walk_chart.jpg`, `20_P1_cost_p10_chart.jpg` |

Not on a slide: `20_P1_v1_headon_level.jpg`, `20_P1_office_v2_headon_centre.jpg`, `20_P0_orientation.jpg`, `20_P0_frame_delta_chart.jpg`, `20_P1_cost_chart.jpg` (report only). Earlier G14 slides: VL031–037 (review, probe, hook proof), codex-audit VL085–087, VL116–119.
