# 02 — Runtime probe: what the Metal ray-traced glass prototype actually does on Red's Mac

2026-10-03 · visual chat (游戏视觉) · workflow glass-rt-track, runtime-probe stage · **status: DONE**

**What this is.** ChatGPT's hardware ray-traced glass prototype (`edfbc92`, controller edit since committed in `c69c7d7`) was run inside Unity 6000.3.10f1 on Red's M3 Max, in the real game scene, in Play Mode, with a measuring harness. This report gives the numbers, the frames, and a verdict on every suspected defect. It complements `01_code_review.md` (line-by-line review) and `03_research.md` (what production should look like). Where this report and `01` disagree, this report is the runtime evidence.

**Where it ran.** Only in the private clone `proj_rt`. Red's project was not opened in Unity and not edited. The only files written under `Frontrooms3D/` are this report, `images/02_*.jpg` and `harness/02_*`.

---

## 0. Short answer (for Red)

1. **Metal ray tracing works on this Mac through the plugin.** Unity loads the plugin, hands it the Metal device, and Metal says yes to ray tracing. Unity's own flag `SystemInfo.supportsRayTracing` still says **False**. Measured in the Editor before and during Play Mode.
2. **As committed, the prototype draws nothing, and cannot.**
   - In the main game, the controller is never created (0 instances).
   - Where it is created (the standalone map), the pass never runs: 0 render events.
   - Forcing the pass to run crashes Unity on the first frame (segfault in the GPU driver, run 2).
3. **To see any image, the probe had to patch the plugin in the clone.** Four compile/crash patches, plus three fixes that can be switched on and off (struct layout, BLAS index, residency). Every image below that says "prototype" includes those patches. Without the layout and BLAS-index fixes, the trace paints dark glass shapes on the walls (`02_ablation.jpg`).
4. **With those patches, the remaining runtime defects are:**
   - **Wrong field of view.** The reflection covers only 34.6 % of the pane's area. It is 0.589 times the size, centred on the screen. With the field of view fixed, the traced glass matches the pane to IoU 0.94–1.00.
   - **No vertical mirror.** The upright overlap is 0.94–1.00; the flipped overlap is only 0.33–0.67.
   - **The composite is opaque and runs after the grade.** It hides the view through the window. It turns the yellow-green pane neutral blue-grey: mean RGB goes from (111, 110, 77) to (81, 83, 85). It skips ACES, white balance, halation and grain.
   - **Glass is painted over things the trace does not know about.**
     - An unregistered object in front of the pane: 94 % of it is painted over.
     - The Relay after it moves: 177,035 px painted over it, until the next 1 s rescan.
     - A broken pane: 159,533 px of glass still drawn in the empty window.
     - A destroyed mesh: still reflected.
   - **About one fifth of a head-on reflection is blue "sky" inside a closed building.** 16–20 % of head-on glass pixels show it.
   - **A main-thread hitch every second.** A frame with a rescan costs +45 to +96 ms. It is followed by one blank RT frame, so the glass flickers once a second.
5. **The cost is small once it works.**
   - The trace costs about **1–2 ms GPU at 1920×1080** on a quiet GPU. The worst median was 3.2 ms with up to four other Unity processes sharing the GPU.
   - The rebuild that runs every second costs **74–105 ms** on the main thread (median per run; range 39–363 ms), for only 186 objects and 4,056 triangles.
   - So the trace is affordable. The scene management is not.

---

## 1. Setup

| Item | Value |
|---|---|
| Machine | Apple M3 Max, macOS 26.6.2, 128 GB, Metal |
| Unity | 6000.3.10f1, URP 17.3, Forward+, RenderGraph, 4x MSAA, HDR on, post on (ACES + film look) |
| Project | private clone `scratchpad/proj_rt`. The RT files (`.cs`, feature, composite shader, `.mm`, build script, `FrontRoomsMapWorld.cs`) are byte-identical to Red's, except the instrumented `.mm`. |
| Plugin | rebuilt from the clone's `.mm` with the clone's `NativePlugin/build_frontrooms_metal_glass_rt.sh`. Output: arm64, macOS 13.0+, 77,328 bytes, sha1 `beb93f8d…`. Red's committed dylib is 75,744 bytes, sha1 `4bf02a4c…`. |
| Harness | `Assets/Editor/RT/FrontRoomsGlassRTProbe.cs`, clone only. Copy: `harness/02_FrontRoomsGlassRTProbe.cs.txt` |
| Command | `Unity -batchmode -projectPath proj_rt -executeMethod FrontRoomsGlassRTProbe.RunBatch -logFile <f>`, with graphics (Metal and native plugins load) |
| Scene | `Assets/Scenes/FrontRooms3D.unity`, the real game. Same start as the interaction-audit and G11 harnesses: `Random.InitState(4242)`, then the title start, which gives run seed 516574485. The Relay stays dormant (release delay 10⁶ s). |
| Test pane | `Window pane (282,206)-(282,207)`, Level 0, centre (271.50, 1.18, 45.00). It is a Unity Cube scaled 1.40 × 1.65 × 0.03 m, with material `Map test / glass` (URP/Lit, transparent, premultiplied, smoothness 0.9). |
| Camera | the game's first-person camera: FOV 76°, near 0.06, far 80. Rendered with `camera.Render()` into a 1920×1080 4x-MSAA sRGB target, then read back. |
| Load on the Mac | Other workflow tracks were running at the same time. Run 7 had one WebGL brotli compressor, plus one other Unity batch process for the last ~50 s. Run 8 had up to 4 other Unity batch processes. See `harness/02_machine_load.txt`. **GPU times are therefore upper bounds.** |

### 1.1 How the RenderGraph pass was driven (task step 3)

Plain batch rendering without Play Mode cannot test this: the controller is a MonoBehaviour that only lives in Play Mode. The harness does the following:

1. `RunBatch` opens the game scene, sets a `SessionState` flag and calls `EditorApplication.EnterPlaymode()`.
2. An `[InitializeOnLoad]` hook drives a coroutine from `EditorApplication.update`, with a frame counter.
3. It starts a run and places the player.
4. It calls `camera.Render()` into its own RenderTexture. URP runs the full RenderGraph, including the RT feature's `AddRenderPasses`, the unsafe pass, `IssuePluginEventAndData`, the native `RenderEvent` and the composite.
5. It reads back the frame and the controller's private `result` texture (raw float RGBA).
6. It exits Play Mode and quits.

The pass ran. The plugin counted 2 render events per two renders, with 0 skipped, and `GPUStartTime`/`GPUEndTime` were recorded for every trace.

### 1.2 What the harness changed at runtime (clone only, prototype C# unchanged)

| Change | Why |
|---|---|
| Created the controller with `FrontRoomsMetalGlassRTController.Ensure()` | The game never creates it (§3) |
| Called the private `EnsureResult(1920,1080)` once | Breaks the `IsReady` deadlock (R1). Without it, nothing ever runs. |
| Tagged the game camera `MainCamera` after the as-is test | `Camera.main` is null in the game, so registration centres on an arbitrary pane 43.5 m away (§3) |
| Registration radius 18 m → 14 m | At 18 m the 256 cap is hit and 22 nearby renderers are silently dropped. 14 m gives 186 instances, all within-radius renderers registered. One extra test (§6.3) uses the original 18 m. |
| Froze the 1 s rescan (`nextScan = ∞`) and called `RebuildScene` explicitly | So every test controls when the scene is rebuilt. The free-run test (§8.3) restores the 1 s rescan. |
| Film grain off for "analysis" frames (`*_A_*`) | So ON/OFF pixel differences come from the composite only. "Game look" frames keep grain. |

### 1.3 What was patched in the clone's plugin (`harness/02_plugin_probe_instrumentation.diff.txt`)

| Patch | Effect | Without it |
|---|---|---|
| P1, P2 | `using namespace metal::raytracing;`, and rename the variable that shadowed `intersector<>` | The MSL kernel does not compile (R3) |
| P3 | `EnsureDevice` actually compiles the pipeline | The pipeline stays nil (R2) |
| P4 | Skip the dispatch if the pipeline is nil | The segfault of run 2 |
| Flag 2 | Declare residency (`useResource`) for BLASes and vertex/index buffers | R8 |
| Flag 4 | Each TLAS instance uses its own BLAS slot instead of the mesh index | R5 |
| Flag 8 | 96-byte, 16-byte-aligned instance records, matching the MSL struct | R4 |
| Flag 16 | `tanFovY = tan(fov/2)` inside the plugin, so the C# stays as committed | FOV test only (R6) |
| Flag 32 | **Diagnostic:** the kernel writes hit identities instead of colour. R = reflected-hit instance + 1 (0 = miss → sky), G = hit distance, B = first-hit instance × 16 + triangle. | Diagnostic renders only |
| Flag 1 | Records Unity's current command-buffer status at the plugin event | Ordering test |
| Timers | `addCompletedHandler` with `GPUStartTime`/`GPUEndTime`, exported as `FRGlassRT_LastGpuMs`, plus counters through `FRGlassRT_Stat` | — |

After the ablation, the run uses flags 2 + 4 + 8 (= 14). The images label this "prototype as committed*". Flag 16 is added only where a frame says "FOV corrected".

---

## 2. Attempt log (every run)

Runs 1–6 belong to the first attempt at this stage earlier today. That attempt was cut off during run 6, before it wrote this report. Runs 7–8 are this attempt. Logs: `scratchpad/rtprobe/run*.log`, `scratchpad/rtprobe7/run7.log`, `scratchpad/rtprobe8/run8.log`.

| Run | Plugin | Result |
|---|---|---|
| 1 | prototype + timers only | Play Mode works. The controller is absent in the game. After `Ensure()`, `IsReady` stays False and there are **0 render events**: the pass never runs. Rebuild hitch measured. |
| 2 | prototype + timers, gate primed by the harness | **Unity crashes on the first traced frame.** Segv in `-[AGXG15XFamilyComputeContext setComputePipelineState:]` ← `RenderEvent` ← `GfxDevice::InsertCustomMarkerCallbackAndDataWithFlags` ← `ScriptableRenderContext::ExecuteScriptableRenderLoop`. Excerpt: `harness/02_run2_crash_excerpt.txt`. |
| 3 | + P1–P4 | The kernel compiles and traces. Output is garbage until layout and BLAS index are fixed. |
| 4 | + ablation flags | Isolated R4, R5 and R8 one by one (§4.1) |
| 5 | + FOV flag | Complete run. The live Relay caught the player at 0.7 m and froze the game, which spoiled the occluder test. |
| 6 | + static copy of the Relay body, close view | Stopped during the close view, with no crash and no exit message (the earlier attempt's session ended). Its numbers up to that point are used in §8 only. |
| **7** | + hit-identity diagnostic (flag 32), streamed-mesh test | Complete, 1.5 min. Lighter load. **Used for timings.** |
| **8** | + miss-cause walk, 18 m comparison, Relay occlusion with exact silhouettes, all occluder frames FOV-corrected | Complete. **Used for images.** Every pixel count that runs 7 and 8 share is identical between them, so the image results are deterministic. |

The clone compiled with no `error CS` lines in any batch log. Neither run produced a Metal validation message or a GPU error: trace errors 0, every `completed N/N`.

---

## 3. Capability, import settings, and what runs today

| Check | Result (Editor before Play, and in Play Mode) |
|---|---|
| `FRGlassRT_DeviceSupportsRaytracing()` | **1**: Metal device `supportsRaytracing` = YES |
| `UnityPluginLoad` received `IUnityGraphicsMetalV2` | yes |
| `SystemInfo.supportsRayTracing` / `supportsRayTracingShaders` / `supportsInlineRayTracing` | **False / False / False** |
| Runtime MSL compile + pipeline creation | 52–196 ms, once, at device init (a start-up hitch) |
| Plugin importer | `.meta` is only a guid (59 bytes), so Unity defaults apply: Editor ✓, Standalone OSX ✓ (CPU **AnyCPU**), Windows ✗, WebGL ✗. The dylib is **arm64 only**. |
| Main game (`FrontRooms3D.unity`) | **controller NONE.** The game builds the map with `standalone = false`, and `FrontRoomsMapWorld.Awake` calls `Ensure()` only when standalone. 18 panes had `FrontRoomsMetalGlassTarget` at start. |
| `Camera.main` in the game | **null.** The game camera is tagged "Untagged". The controller then centres registration on whichever pane `FindObjectsByType` returns first: `Window pane (296,207)-(297,207)`, **43.5 m from the camera** (57–64 m in earlier runs). |
| Gate as committed | after two renders: `IsReady` False · result texture null · **0 render events** |
| Renderer feature | present and active in the shared renderer. The composite material is created (`Shader.Find` works in the Editor). |
| Render thread | In this Editor batch, the native event ran **on the main thread** (`pthread_main_np` = 1). Player settings have `m_MTRendering: 1`, so in a Mac player it runs on the render thread, while `RebuildScene` mutates the plugin on the main thread. The race in R24 cannot be observed in the Editor. |

**Import settings proposal (no change made).** Set Editor → OS OSX, CPU ARM64, and Standalone OSX → CPU ARM64. Keep all other platforms off. Today an Intel or Universal Mac build would ship a dylib that cannot load. The controller would catch the `DllNotFoundException` and stay inert, so it fails safe, but it is not explicit.

---

## 4. As-is behaviour and the ablation (`02_asis_untagged.jpg`, `02_ablation.jpg`)

**As-is with only the compile patches** (flags 0, camera untagged, gate primed): the trace paints **326,172 px** of flat dark-blue "glass" over the frame. The patches sit on the walls, beside the window and in the middle of the pane (`02_asis_untagged.jpg`).

### 4.1 Ablation at the head-on view (camera tagged, radius 14 m)

| Flags | Fix enabled | RT px | Overlap with the pane under the prototype's own projection | Distinct colours |
|---|---|---|---|---|
| 0 | none (compile patches only) | 69,196 | 0.00 | 1 (glass painted on the walls) |
| 2 | + residency | 69,196 | 0.00 | 1 |
| 4 | + per-instance BLAS | 438,223 | 0.00 | 1 |
| 8 | + aligned records | 300,171 | 0.53 | 2,203 |
| 12 | BLAS index + aligned | **159,533** | **1.00** | 237 |
| 14 | all three | 159,533 | 1.00 | 237 |
| 30 | all three + FOV | **460,554** | 1.00 against the true pane (461,296 px) | 245 |

- **Conclusion.** The struct-layout mismatch (R4) and the BLAS-index bug (R5) are each enough to put glass in the wrong place. Both must be fixed together.
- **Residency (R8)** made no visible difference on this Mac: flags 12 and 14 give identical pixels. It is still required by the Metal API contract.
- **Correction to `01`.** At runtime, flags 0 does not give "no glass found". It gives glass found on the wrong instances, painted onto walls.

---

## 5. Alignment, field of view and vertical mirror (`02_v1…v5_*.jpg`, `02_alignment_*.jpg`)

**Reference masks.** A ray-cast mask of the pane is built at 960×540 from the camera's exact position and basis. A pixel counts when the first collider along its ray is the pane. One mask uses the correct `tan(fov/2)` = 0.781. The other uses the prototype's value, the FOV in radians = 1.326, which is ×1.698 too large.

**Overlays.**
- green = true pane
- yellow = pane under the prototype's projection
- red = RT alpha as committed
- blue = RT alpha with the FOV corrected

| View | Eye → pane | Incidence | True pane px (full res) | RT px as committed | Overlap with prototype projection | Overlap with true pane | Overlap with true pane, FOV fixed | Same, flipped vertically |
|---|---|---|---|---|---|---|---|---|
| v1 head-on, level gaze (pane low in frame) | 1.56 m | 16.5° | 466,832 | 173,568 | **1.000** | 0.372 | **0.997** | 0.328 |
| v2 head-on, at pane centre | 1.56 m | 16.5° | 461,296 | 159,533 | 0.998 | 0.348 | **0.999** | 0.638 |
| v3 50° | 1.56 m | 52.0° | 351,012 | 119,163 | 0.979 | 0.341 | 0.979 | 0.668 |
| v4 steep | 1.56 m | 66.1° | 225,680 | 73,595 | 0.940 | 0.328 | 0.941 | 0.665 |
| v5 close, pane fills 63 % of the frame | 0.75 m | 36.6° | 1,314,464 | 769,476 | 0.999 | 0.586 | **1.000** | 0.438 |

**Suspect 1 (FOV) — confirmed.**
- The traced pane is the true pane shrunk by 1/1.698 = **0.589** towards the screen centre. Area ratio: 159,533 / 461,296 = **0.346**.
- The reflection therefore covers about a third of the glass, in the wrong place.
- With `tan(fov/2)`, the traced mask matches the true pane.
- At 50° and 65°, 7,877 and 13,868 px (2.2 % and 6.1 %) of the reference are not traced. These lie along the near window trim. The trace stops at the trim's inner edge, which is what the screen shows. The collider-based reference sees the pane through the trim (`02_alignment_v4_steep65.jpg`). This is a limit of the reference mask, not an RT error.

**Suspect 2 (vertical mirror) — not a defect in this setup.**
- In v1 the pane sits in the lower half of the frame. The upright overlap is 0.997 and the flipped one only 0.328.
- The composite paints exactly the traced pixels in every view: overlap of "pixels changed by the composite" with the RT alpha = 1.000.
- So the kernel's row order and the composite's `1 − uv.y` cancel out, as `01` predicted. They would not if the pass ever wrote to the backbuffer.

**Blank first frame (R11) — confirmed.** The first render after every rescan has **0** traced pixels, in all 5 views.

**One-frame lag — not observed.**
- Test: move 0.35 m between two renders.
- The single render at the new pose already composites the new pose's trace: overlap 1.000 with trace B and 0.548 with trace A (`02_lag_test.jpg`).
- Reason: when the plugin event runs, Unity's current command buffer is still *NotEnqueued* (status 0). The plugin commits its own command buffer on the same queue, so the GPU runs the trace **before** whatever is still in Unity's buffer, including the composite.
- That ordering is fine for this prototype. It is wrong for a production trace that reads this frame's depth or G-buffer (R16).

---

## 6. What the reflection contains

### 6.1 The composite (suspect 3) — confirmed, and it is opaque (`02_v2_headon_centre.jpg`, `02_reference_vs_rt.jpg`)

- **Written after post.** Inside the traced pixels, the final 8-bit frame equals `sRGB(RT linear value)` to within **0.21–0.26 of 255 levels**, mean over each view. So the RT radiance lands after post: no ACES, no green-yellow white balance, no halation or bloom, no lifted blacks, no grain or vignette.
- **Opaque.** Alpha = 1 (× strength 1), so the view through the window is replaced. At v2 with the FOV fixed, the glass luma drops from 107 to 82. The mean colour goes from (111, 110, 77) to (81, 83, 85): the graded yellow-green turns blue-grey (`02_v2_headon_centre.jpg`, panels 1 vs 3).
- **Weak and dark.** The reflected colour is multiplied by `mix(0.15, 1, Fresnel)`, about 0.18 head-on, and then drawn opaque. The result is a dark mirror, not glass.

### 6.2 Shading (suspect 7) — confirmed

The 11 materials sent to the trace at v1:

- **Every FrontRooms/Surface material is sent as `baseColor` (1, 1, 1).** This covers the Level 0 wallpaper, ceiling and carpet, office wall/ceiling/carpet, the troffer lens and the door veneer. Their textures (`Wallpaper_Chevron_A`, `Carpet_LoopPile_A`, …) are never read.
- **The lenses have emission in their materials, and the trace ignores it.** `Map / Level 0 lens` has `_EmissionColor` peak 2.6 and `Troffer_Lens` 2.2. The per-renderer flicker emission set through MaterialPropertyBlock is also ignored. Lenses appear as dark rectangles.
- **The lamps are ignored.** 62 lamps are enabled within the registration radius (of 1,581 Light components). The trace uses a fixed fake sun from (0.35, 0.82, 0.28) instead.
- **Result.** The floor reflects as flat white and the walls as flat grey (`02_reference_vs_rt.jpg`). A planar-mirror reference camera at the same pane shows the actual hall behind the player: textured carpet, wallpaper, a lit troffer grid and a dark far wall.

### 6.3 Blue sky inside a closed building (suspects 4 and 7) — confirmed, cause found (`02_hits_*.jpg`)

The hit-identity render (flag 32, FOV fixed) shows what every reflection ray hits:

| View | Glass px | **Miss → blue sky** | Carpet | Wallpaper | Cove base | Ceiling | Other glass | Reflected-hit distance, median (max) |
|---|---|---|---|---|---|---|---|---|
| v1 head-on level | 467,484 | **16.1 %** | 51.9 % | 27.4 % | 3.2 % | 0.6 % | 0.8 % | 2.44 m (33.2) |
| v2 head-on centre | 460,554 | **19.8 %** | 42.8 % | 32.1 % | 3.6 % | 0.9 % | 0.9 % | 2.59 m (33.2) |
| v3 50° | 343,489 | 0 % | 26.2 % | 70.9 % | 2.9 % | — | — | 1.99 m (4.7) |
| v4 65° | 212,096 | 0 % | 20.0 % | 75.7 % | 4.2 % | — | — | 1.70 m (2.8) |
| v5 close | 1,314,459 | **4.4 %** | 43.1 % | 46.3 % | 5.5 % | — | 0.7 % | 1.97 m (33.2) |
| v2 at the prototype's 18 m (cap 256 hit) | 460,554 | **19.5 %** | 42.8 % | 32.1 % | 3.6 % | 0.8 % | 0.9 % | 2.60 m |

**Cause, measured by walking the mirror axis behind the camera.**
- The room behind the camera is a hall 25.4 m long with a **5.4 m ceiling**. The ceiling is built from 6 × 6 m chunk pieces.
- Registration keeps a renderer only if its **bounds centre** is within the radius of the pane.
  - Ceiling pieces with centres 4.8, 7.5 and 12.8 m from the pane are registered.
  - The pieces at 18.6 m and 24.4 m are not, and neither is the end wall at 27.1 m.
- Reflected rays that rise towards that ceiling leave the RT scene and return the sky gradient.
- At the prototype's 18 m, the 18.6 m piece still misses by 0.6 m, so the sky remains (19.5 %).
- The other panes are always registered, wherever they are. They hang as floating rectangles in the reflected "sky", because their walls are not registered (orange in `02_hits_v2_headon_centre.jpg`).

### 6.4 Thin glass (suspect 8) — partly

- The pane is a Unity Cube: 12 triangles, 4 on the two broad faces and 8 on the 30 mm edges.
- In all 5 views, every first hit is on the **broad face towards the camera**. The 30 mm edge faces got **0 px**, even at 66°.
- So the "two reflecting faces" of suspect 8 do not show from one side.
- What is missing:
  - no second (back-face) reflection
  - no thickness offset
  - no transmission: the opaque composite removes the view through the glass entirely
- The edge faces are still flagged as glass (the flag belongs to the material), so they would mirror if seen edge-on.

---

## 7. Occlusion, moving objects and lifetime

**Unregistered occluder** (suspect 4; `02_unregistered_occluder.jpg`):
- A door-veneer panel built as a SkinnedMeshRenderer (never registered) stands 0.6 m in front of the pane.
- With the FOV fixed, **137,551 of its 146,601 px (94 %)** are painted over with reflection, because the composite has no depth test.
- With the committed FOV, 65,899 px.
- Note: the scene contains **0 SkinnedMeshRenderers and 0 particle renderers**.

**The Relay** (`02_relay_behind.jpg`, `02_relay_occlusion.jpg`):
- It is built from **16 MeshRenderers**, not a skinned mesh, so a rescan registers it (16/16). This corrects the suspicion that the Relay is skinned.
- **Behind the player:** when it stands 2 m behind the player, it appears in the head-on reflection (46,329 changed glass px).
- **It is only up to date at a rescan.** A static copy of its body (shadows off, so the silhouette is exact) was placed in front of half the pane:

| State (FOV fixed) | Glass painted over the body | Visible glass left without reflection |
|---|---|---|
| right after a rescan | 1,520 px (edge pixels only) | 1,038 px |
| **moved 0.7 m, before the next rescan** | **177,035 px** | **106,022 px** |
| after the next rescan | 1,506 px | 1,228 px |

The TLAS keeps the old pose for up to 1 s. Glass is painted over where the Relay now is, and left empty where it was.

**Broken pane** (`02_ghost_broken_pane.jpg`): `Hold()` breaks the pane and its object is destroyed. The trace still paints **159,533 px** of reflection into the empty frame until the next rescan, then 0.

**Streamed mesh** (suspect 6; `02_stream_test.jpg`): a runtime `Mesh`, built like a chunk shell from `MeshBuilder.ToMesh`, red, 1 m behind the eye.

| Step | Pixels of the reflection that hit it |
|---|---|
| registered | 15,364 (mean row 678) |
| vertices moved +0.5 m, no rescan. Unity **re-allocated** the vertex buffer (native pointer changed). | 15,364 at the **old** place |
| after a rescan | 17,262 (mean row 771) |
| `Destroy(mesh)` + `Destroy(go)`, no rescan | **17,262: a destroyed mesh is still reflected** |
| after a rescan | 0 |

- No GPU errors (4/4 traces completed) and no native error.
- **Verdict on suspect 6.** Unity freeing or re-allocating buffers does **not** fault the GPU: the plugin retains Unity's `MTLBuffer`s. It gives **stale or ghost geometry** for up to one rescan.
- Chunks free their meshes in `Unregister` → `FreeMeshes`, so every chunk drop and live rebuild shows this.

---

## 8. Cost

All GPU numbers are `GPUEndTime − GPUStartTime` of the plugin's command buffer, 20 samples after 4 warm-up frames. Other Unity processes were sharing the GPU (§1), so medians vary run to run. The minimum is the best estimate of the cost on a quiet GPU.

### 8.1 Trace (suspect 10) — measured

1920×1080. Every pixel traces a primary ray; glass pixels trace one reflection ray.

| View (glass share of the screen) | Committed FOV: median ms, runs 6 / 7 / 8 | FOV fixed: median ms, runs 6 / 7 / 8 | Fastest sample |
|---|---|---|---|
| v1 (FOV fixed 22.5 %) | 0.85 / 1.99 / 2.22 | 0.94 / 1.81 / 2.43 | 0.75 |
| v2 (22.2 %) | 0.90 / 1.60 / 2.29 | 1.03 / 1.73 / 3.19 | 0.80 |
| v3 (16.6 %) | 0.89 / 1.53 / 0.81 | 1.34 / 2.37 / 1.38 | 0.77 |
| v4 (10.2 %) | 1.46 / 1.48 / 1.12 | 1.78 / 1.92 / 1.08 | 0.92 |
| v5 close (63.4 %) | 1.92 / 2.50 / 1.35 | — / 2.97 / 1.64 | 1.08 |

- **Expect about 1–2 ms per frame at 1080p** on a quiet GPU. Encode + commit costs 0.04–0.08 ms of CPU.
- The cost hardly follows the glass share, because the primary ray for every pixel dominates. A trace that starts from rasterized glass pixels (`03_research.md` §6) removes that cost.
- Frame-time ON minus OFF, measured as Editor `camera.Render` + readback, was −3.7 to +6.3 ms. That is noise; the Editor cannot resolve this.

### 8.2 Scene rebuild (suspect 5) — measured on the main thread

Rebuild at 14 m: 186 instances, 81 unique meshes / BLAS, 4,056 triangles, 0.34 MB of BLAS, 46 KB TLAS. All rebuilds of runs 5–8 (n = 61):

| Part | Median per run | Range |
|---|---|---|
| `RebuildScene` main-thread wall | 74–105 ms | 39–363 ms |
| BLAS builds, CPU incl. `waitUntilCompleted` | 58–83 ms (≈ 0.7–1.0 ms per BLAS) | 30–114 ms |
| BLAS GPU time (sum) | 28–53 ms (≈ 0.35–0.65 ms per tiny BLAS: per-command-buffer overhead) | — |
| TLAS build | 0.6–1.0 ms CPU, 0.3–0.5 ms GPU | — |
| Managed part (`FindObjectsByType`, registration) | 12–32 ms | — |
| At the prototype's 18 m (256 instances, 123 BLAS, 6,628 tris) | 82–193 ms | — |

The rebuild runs every second, so:

### 8.3 Free run with the 1 s rescan (`02_free_run_chart.jpg`, `02_free_run_blank_frame.jpg`)

| Run | Frames | Rescans | Median frame | Median frame with a rescan | Hitch | Blank RT frame after a rescan |
|---|---|---|---|---|---|---|
| 7 | 200 | 10 | 42 ms | 125 ms | **+82 ms** | **10 of 10** |
| 8 | 200 | 6 | 27 ms | 72 ms | **+45 ms** | 6 of 6 |
| 5 | 200 | 15 | 66 ms | 161 ms | **+96 ms** | 15 of 15 |

Every rescan rebuilds all BLAS: 81 per rescan, 0.63 s of CPU over 10 rescans in run 7. One frame later the glass goes blank, so it flickers at 1 Hz.

**Second camera.** A 960×540 URP camera rendering after the main one re-creates the shared result texture, and the main camera re-creates it again ("NEW" twice per frame). This is R17, now seen at runtime.

---

## 9. Verdict on the ten suspected defects

| # | Suspicion | Runtime verdict | Evidence |
|---|---|---|---|
| 1 | FOV in radians → misaligned | **CONFIRMED.** Reflection at 0.589 scale, 34.6 % of the pane's area. Fixed: overlap 0.94–1.00. | §5, `02_v1…v5_*.jpg`, `02_alignment_v1_headon_level.jpg` |
| 2 | Kernel row 0 + composite flip → vertical mirror | **NOT OBSERVED.** Upright overlap 0.997 vs flipped 0.328 (v1). The composite paints the traced pixels 1:1. Fragile if the pass ever targets the backbuffer. | §5, `02_v1_headon_level.jpg` |
| 3 | Composite after post (no tonemap/grade/bloom) | **CONFIRMED, and opaque.** Frame = sRGB(RT) within 0.21–0.26 levels. Window view lost. Grade lost: (111,110,77) → (81,83,85). | §6.1, `02_v2_headon_centre.jpg`, `02_reference_vs_rt.jpg` |
| 4 | Partial scene, no depth test | **CONFIRMED.** Unregistered occluder 94 % painted over. Moving Relay 177,035 px. 16–20 % sky. Registration centred 43.5 m away in the game (null `Camera.main`). | §3, §6.3, §7, `02_unregistered_occluder.jpg`, `02_relay_occlusion.jpg`, `02_hits_*.jpg` |
| 5 | 1 s main-thread rebuild with `waitUntilCompleted`; unsynchronised globals | **CONFIRMED for the hitch:** +45–96 ms every second, plus a blank frame after each rescan. The **race is not observable in the Editor** (events run on the main thread); it applies to players (`m_MTRendering: 1`). | §8.2–8.3, `02_free_run_chart.jpg` |
| 6 | BLAS hold Unity buffers that may be freed | **CONFIRMED as stale/ghost geometry; NOT as GPU faults.** The edited mesh stays at its old place; the destroyed mesh stays visible; 0 GPU errors. | §7, `02_stream_test.jpg`, `02_ghost_broken_pane.jpg` |
| 7 | Flat shading, no textures/lamps/emission/fog, sky on miss | **CONFIRMED.** All surfaces white or grey, lenses dark, 62 lamps ignored, sky 16–20 % head-on. | §6.2–6.3, `02_reference_vs_rt.jpg`, `02_hits_v2_headon_centre.jpg` |
| 8 | 30 mm cube: two reflecting faces, no thin-glass model | **PARTLY.** Only the camera-facing broad face is ever hit (0 edge-face px at 16–66°). No back reflection, no thickness, no transmission. | §6.4, `02_hits_v4_steep65.jpg` |
| 9 | Feature in the shared renderer runs on WebGL/Windows | **Not runnable here.** Inert by construction today: the importer excludes Windows and WebGL, the controller only exists in the standalone map, and `AddRenderPasses` returns early without it. Importer CPU is AnyCPU for an arm64-only dylib. | §3 |
| 10 | GPU cost unknown | **MEASURED.** Trace ≈ 1–2 ms at 1080p on a quiet GPU (medians up to 3.2 ms under load). Rebuild 74–105 ms median (39–363 ms) of main thread, every second. | §8 |

### 9.1 Other defects observed at runtime (not on the list)

| Id | Observation | Evidence | `01` finding |
|---|---|---|---|
| N1 | The pass never runs as committed: 0 render events | §3 | R1 |
| N2 | Forcing it to run with the committed plugin crashes Unity (driver segv) | run 2, `harness/02_run2_crash_excerpt.txt` | R2 |
| N3 | The kernel does not compile as committed | runs 1–3 | R3 |
| N4 | With only compile fixes, glass is painted onto walls (layout + BLAS index) | `02_asis_untagged.jpg`, `02_ablation.jpg` | R4, R5 |
| N5 | The main game never creates the controller | §3 | — |
| N6 | `Camera.main` is null in the game, so registration centres on an arbitrary pane 43–64 m away | §3 | R12, R13 |
| N7 | The first RT frame after every rescan is blank (1 Hz flicker) | §5, §8.3 | R11 |
| N8 | The 38 panes are always registered, however far away. They eat the 256 cap (22 nearby renderers dropped at 18 m) and float in the reflected sky. | §6.3 | R13 |
| N9 | The radius test on bounds **centres** drops big chunk pieces whose surface is close (6 × 6 m ceiling tiles) | §6.3 | new |
| N10 | A second camera re-creates the shared result texture every frame | §8.3 | R17 |
| N11 | The trace's command buffer runs before Unity's current one: fine now, wrong for depth- or G-buffer-driven tracing | §5 | R16 |
| N12 | Runtime MSL compile: 52–196 ms hitch at device init | §3 | new |
| N13 | Residency: no visible effect on this Mac, still required by the API | §4.1 | R8 |
| N14 | Plugin importer CPU AnyCPU for an arm64-only dylib | §3 | R22 |

---

## 10. What this means for the next stages

These are inputs for the design stage, not decisions.

- **Keep from the prototype.** Getting Unity's device and queue through `IUnityGraphicsMetalV2`, BLAS built straight from Unity's native buffers, a TLAS with 3×4 transforms, and a runtime-compiled MSL intersector. All of these work on this Mac, and the trace itself is cheap.
- **The probe rules out shipping the following.** Each item has its own measurement:
  - **Composite after post:** §6.1.
  - **Re-traced primary visibility:** §7. The glass pixels must come from the raster (glass G-buffer or depth).
  - **Rebuild-everything-every-second:** §8.2–8.3. Rebuild BLAS only when a mesh changes, rebuild only the TLAS every frame (≈ 0.3–0.5 ms GPU here), and never `waitUntilCompleted` on the main thread.
  - **Registration by bounds-centre radius:** §6.3. It needs a geometry-aware set, or the whole streamed neighbourhood. 4,056 triangles at 14 m is tiny; `03_research.md` measured 2,912 instances tracing as fast as 256.
- **Glass-track interface.** The agreed `_FR_GlassRTReflection` (linear HDR RGB + coverage) and `_FR_GlassRTWeight` route the RT radiance through the glass shader before post. That is exactly what §6.1 shows is needed. This probe did not integrate it; that belongs to the implementation stage.
- **Moving and destructible geometry.** The Relay is 16 rigid parts, so a per-frame TLAS transform update covers it; no skinning is needed. Fracture pieces need one BLAS per piece shape, a TLAS transform per frame, and the glass flag per instance (today it is per material). The ghost and stale tests (§7) show that every lifetime event must reach the plugin in the same frame.
- **Contract request for the map chat (proposal, signatures for the design stage to confirm).** MapWorld has no chunk build or release event today.
  - `public event Action<GridCoord, GameObject> ChunkBuilt;` raised at the end of `BuildInto`, after `built[coord] = chunk`.
  - `public event Action<GridCoord, GameObject> ChunkReleasing;` raised at the start of `Unregister`, **before** `FreeMeshes`/`Kill`, for drops, failed builds and live rebuilds. The plugin must drop BLASes that reference those meshes before Unity frees them; the stream test shows what happens otherwise.
  - `public event Action<GridCoord, GameObject> RoomDressed;` raised when furniture for a room has been placed (one room per frame).
  - The existing `GlassBroken` event should remove the pane instance in the same frame. Otherwise the reflection stays for up to 1 s (159,533 px in §7).

## 11. Not covered by this probe (open)

- **Player build behaviour:** render thread, the R24/R25 races, composite shader stripping (R21). Not testable in an Editor batch.
- **WebGL:** whether the unguarded `DllImport`s of the controller link cleanly in a WebGL build. Check the next WebGL build log for `FRGlassRT_` undefined-symbol warnings.
- **GPU times on an idle GPU.** Every run shared the GPU with other workflow tracks; §8 gives the fastest samples as the best estimate.
- **The HDR-off display mode and Office-zone lighting.** Only Level 0 with HDR on was probed.

---

## 12. Frame list (`images/`, JPG q85, 1600 px wide)

| File | Run | What it shows |
|---|---|---|
| `02_asis_untagged.jpg` | 8 | As committed + compile patches, `Camera.main` null: RT OFF / ON / raw RT ×4 / outlines. 326,172 px of glass painted on walls and in the window. |
| `02_ablation.jpg` | 8 | Seven ablation renders (flags 0, 2, 4, 8, 12, 14, 30) with true-pane (green) and prototype-projection (yellow) outlines |
| `02_v1_headon_level.jpg` | 8 | Head-on, level gaze (mirror test). 1 RT OFF · 2 RT ON as committed* · 3 RT ON + FOV fix · 4 raw RT ×4 (FOV fix) · 5 outlines (green truth, yellow prototype projection, red RT, blue RT with FOV fix) · 6 analysis frame |
| `02_v2_headon_centre.jpg` | 8 | Same layout, head-on at the pane centre |
| `02_v3_angle50.jpg` | 8 | Same layout, 50° |
| `02_v4_steep65.jpg` | 8 | Same layout, 65° |
| `02_v5_close60.jpg` | 8 | Same layout, 0.6 m (pane fills 63 % of the frame) |
| `02_alignment_v1_headon_level.jpg` | 8 | Full-size outline overlay, v1 |
| `02_alignment_v4_steep65.jpg` | 8 | Full-size outline overlay, v4 (residual along the trim) |
| `02_reference_vs_rt.jpg` | 8 | Planar-mirror reference camera vs the prototype's RT radiance (FOV fixed, ÷0.184 Fresnel weight) at v2 |
| `02_hits_v2_headon_centre.jpg` | 8 | Hit identities at v2: colour = material the reflection ray hits, magenta = miss → sky, yellow = edge face |
| `02_hits_v4_steep65.jpg` | 8 | Hit identities at 65° |
| `02_hits_v2_radius18.jpg` | 8 | Hit identities at v2 with the prototype's 18 m radius |
| `02_relay_behind.jpg` | 8 | Relay body 2 m behind the player: RT OFF, RT ON (FOV fixed), raw RT ×4 without and with the body |
| `02_relay_occlusion.jpg` | 8 | Relay body in front of half the pane: after a rescan, moved 0.7 m before a rescan, after the next rescan (cyan = body, red = traced glass) |
| `02_unregistered_occluder.jpg` | 8 | Unregistered panel in front of the pane, painted over by the composite (committed and FOV-fixed) |
| `02_stream_test.jpg` | 8 | Runtime mesh behind the camera: registered, edited without rescan, rescanned, destroyed without rescan, rescanned (red = rays that hit it) |
| `02_ghost_broken_pane.jpg` | 8 | Pane intact / broken (game) / broken with RT ON before the rescan / after the rescan |
| `02_lag_test.jpg` | 8 | First render after a 0.35 m move: the composite already uses the new trace |
| `02_free_run_chart.jpg` | 7 | Frame time and traced-glass share over 200 frames with the 1 s rescan |
| `02_free_run_blank_frame.jpg` | 7 | The first frame after a rescan: RT output blank |

The raw data for every frame is in `proj_rt/Verification/rt_probe/` (run 8) and `scratchpad/rtprobe7/run7_out/` (run 7). It comprises:
- PNG frames
- `.f32` raw RGBA readbacks of the result texture, row 0 at the bottom
- `.u8` ray-cast masks at 960×540
- `*_instances.tsv`: TLAS instance → renderer and material
- `frames.txt`: camera pose for every PNG

Metrics are in `harness/02_metrics_run7.json` and `harness/02_metrics_run8.json`.

## 13. Reproduce

1. In a clone (never Red's project), copy `harness/02_FrontRoomsGlassRTProbe.cs.txt` to `Assets/Editor/RT/FrontRoomsGlassRTProbe.cs`.
2. Apply `harness/02_plugin_probe_instrumentation.diff.txt` to `NativePlugin/FrontRoomsMetalGlassRT.mm`.
3. Run `sh NativePlugin/build_frontrooms_metal_glass_rt.sh`, with Unity closed.
4. Run `Unity -batchmode -projectPath <clone> -executeMethod FrontRoomsGlassRTProbe.RunBatch -logFile <log>`. It takes about 1.5 min and writes `<clone>/Verification/rt_probe/`.
5. Run `python3 harness/02_analyze_rt.py.txt <clone>/Verification/rt_probe <out>` (numpy + Pillow) for the metrics and sheets.
6. Run `python3 harness/02_free_run_chart.py.txt <clone>/Verification/rt_probe/free_run_frames.tsv <jpg>` for the chart.

Logs: `harness/02_probe_log_run7.txt`, `harness/02_probe_log_run8.txt`, `harness/02_probe_log_editor_run8.txt`.
