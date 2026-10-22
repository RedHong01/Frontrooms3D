# 25 · G14 ray-traced glass: engineering verification (P0 + P1 delta on today's main)

2026-10-14 · visual chat (游戏视觉) · workflow glass-rt-track, stage verify:engineering (third attempt: the 2026-10-08 00:xx and 19:xx attempts were cut off by usage limits before they wrote this file; their outputs were reused where still valid and every gate below was re-run today) · **verdict: correctness, inertness, isolation, lifetime and threading PASS on today's main; cost FAILS the design bars; new defects E1, E2, E3, E5, E8, E12; land E4 (codex F1 follow-ups) and preferably E1 with the merge.**

`W` = `/Users/redwang/FrontRoomsVisualWork`. Design: `10_rt_glass_design.md`. Implementation report: `20_implementation.md`. Codex audit for this track: `../../codex_audit/10_review_rt.md`, `30_fix_runtime_rt.md` (there is no `20_findings.md`). Nothing under `Assets/` of Red's project was touched; this stage wrote only this file, six images and five rows of `Documentation/VERIFICATION_LOG.md`.

---

## 0. Short answer (for Red)

1. **The code that waits in `W/proj_rt` still does what the implement stage said, on today's main (`0a9ee7f` + working tree).** Main's 21 G14 files are byte-identical to the bases recorded in `20_implementation.md` §9, so the promotion list still applies. No apply package exists yet under `W/apply/` (nothing to dry-run; the merge stage builds it).
2. **Correct and safe:** the traced glass sits on the drawn glass (IoU 0.996–0.999), 0 % misses, occluders, a moving Relay, a broken pane and a destroyed mesh are right on the same frame, RT Off is pixel-identical to main as is, WebGL / iOS / Windows players contain no RT code, the Metal validation layer (API + GPU shader validation) reports 0 errors with RT tracing, two cameras / resizes / a camera destroyed mid-flight / 3 Play sessions with domain reload all run with 0 command-buffer errors and the native scene empties at every Play exit.
3. **It also runs in a real Mac player** — but only in an arm64-only build. Today's universal Mac app leaves the arm64 plugin out, so your shipped Mac player has no RT (codex COV-1, your option a/b).
4. **Too expensive, as the implement stage already said.** New today: the main-thread streaming cost has a simple cause and a small fix (E1): every mesh upload copies the whole reused scratch array (up to 65,536 entries) through P/Invoke, 1.5–3.5 ms per call, even for a 96-vertex mesh. GPU: in the player at 1440p the 1.5 m window adds +2.2 ms (High, bar 2.0) and +3.3 ms (Ultra, bar 3.5); the break-shot pose adds +2.1 ms (High) / +3.3 ms (Ultra, the player default) at 1080p against the +2 ms shot budget. The Mac was never idle (load 23–70), so these are upper bounds.
5. **Reflections do not follow props that change without moving** (E2): a CRT you switch off with E stays lit in the window, an EXIT sign's dead/lit state and a glow placard's phosphor fade are not reflected (the reflection keeps the state from when the room streamed in).

---

## 1. How this stage ran

- **Clones (all re-synced from main 0a9ee7f + working tree at 16:20, re-using the 10-08 clones, no new clone):** `W/proj_rt_eng3`, `W/proj_rt_eng`, `W/proj_rt_eng2` (test: main + the 18-path G14 overlay from `W/proj_rt` + 2 deletions), `W/proj_rt_engbase3` (main as is), `W/proj_rt_engplay` (test + player harness). Script: `W/rt_work/eng25b/resync.zsh`. All 18 overlay paths hash-equal to `W/proj_rt`.
- **Plugin:** rebuilt with the clone's own `NativePlugin/build_frontrooms_metal_glass_rt.sh` on macOS 27.0.1: validator OK (7 pipelines, layout self-test FRInstance 128 / FRMaterial 256 / FRLamp 64 / FRFrame 496), output sha1 `c7c3351b898a1c6a59666249169c946341a8fc52` = `W/proj_rt`'s dylib byte for byte; arm64, `@rpath`. `.meta`: Editor (OSX, ARM64) + OSXUniversal (ARM64), preloaded, everything else off.
- **Unity 6000.3.10f1 batch with graphics** works after the macOS upgrade (every run below compiled with 0 `error CS`); Blender 4.3 starts.
- **Harnesses:** the implement stage's `Assets/Editor/RT/FrontRoomsGlassRTVerify.cs` plus the verifier's own clone-only scripts in `W/rt_work/eng25b/harness/` (recovered from the 10-08 transcripts, two new ones today): `RTEngOffCapture`, `RTEngInert`, `RTEngMultiCam`, `RTEngReplay`, `RTEngCoverage`, `RTEngStreamSoak`, `RTEngUploadBench` (new), `RTEngGhost` (new), `RTEngPlayerBuild` (arch switch added), `RTEngPlayerSmoke`. Never promoted.
- **Rendering mode:** `-force-gfx-mt` (a real render thread, as Red's editor and the player) unless stated.
- **Load:** 3–6 other Unity jobs ran from other workflows during every run (load 21–80). Pixel counts, IoUs, error counts and the main-thread stopwatch are trustworthy; GPU times are upper bounds.

| Run | Clone | What | Result folder |
|---|---|---|---|
| m2 | proj_rt_eng3 | full P0/P1 harness, all phases | `W/rt_work/eng25b/m2_out/` |
| b5, b6 / d5 | engbase3 / eng | RT-Off capture, main as is (twice) vs main + G14 | `proj_rt_engbase3/Verification/rt_eng/`, `proj_rt_eng/Verification/rt_eng/` |
| mt2 | proj_rt_eng2 | weight-0 vs Off soak, 120 iterations | `proj_rt_eng2/Verification/rt_eng/mt2_inert.txt` |
| multicam, replay, soak2, cov2 | eng, eng2, eng, engplay | cameras / resize / destroy; 3 Play sessions with domain reload; 3-lap streaming soak; registry coverage | `…/Verification/rt_eng/` |
| isolation | proj_rt_eng2 | player-script compile per target, importer, strippers | `proj_rt_eng2/Verification/rt_p0p1/isolation.txt` |
| val_hi, val_off, val_harness | eng3, engbase3 | Metal API + GPU shader validation (`MTL_DEBUG_LAYER=1 MTL_SHADER_VALIDATION=1`) | `W/logs/eng25b/val_*.log(.gz)`, `W/rt_work/eng25b/val_harness_types.txt` |
| upload bench | proj_rt_eng3 | P/Invoke cost of the CPU mesh upload | `proj_rt_eng3/Verification/rt_eng/upload_bench.txt` |
| ghost 1, 2 | proj_rt_engplay | reflections of props that change without moving | `W/rt_work/eng25b/ghost_run1.txt`, `ghost_run2.txt` |
| player out1, out2 | arm64 player from proj_rt_engplay | player smoke + cost | `W/rt_work/eng25b/player_arm64_out1/`, `…_out2/` |

---

## 2. Gates

| Gate (task text / design §4) | Result today | Verdict |
|---|---|---|
| **Alignment** of the traced mask with the drawn pane (pixel overlay), P0-A2 | m2, prepass vs the glass's own ≥ 50 % MSAA coverage: IoU 0.997 / 0.997 / 0.997 / 0.996 / 0.999 (v1–v5), px > 1 px off the edge 0 / 1 / 39 / 45 / 479 (same as r8 and v1); traced vs prepass IoU 0.989–0.997; misses 0 %. Overlay: `25_E_alignment_overlay.jpg`. Ultra only: a 1-texel line outside the glass, E5 | PASS (High) / FLAG (Ultra) |
| Orientation, P0-A3 | red right of and above green in RT and in the planar mirror; centroids within 5–7 px | PASS |
| **No effect when RT is off** (bit-identical vs the untouched baseline) | `-frGlassRT off`, same seed / pose / 3 frames: main + G14 vs main as is 0 / 0 / 0 px. Main vs itself (b6 vs b5): 125 / 134 / 142 px at 1/255, and d5 equals b5 exactly, so G14 sits on one of main's own two run-to-run states. RT traced at weight 0 vs Off: 0 px in all 5 views and in 120 soak iterations (60 with glass in view, 60 without). `25_E_rt_off_identical.jpg` | PASS |
| **WebGL / Windows provably inert**, P0-A10 | Player-script compile on today's main + delta: WebGL / iOS / Windows64 `Assembly-CSharp.dll` hold 0 `FRGlassRT_`, 0 `FrontRoomsGlassRTSystem`, 0 `FrontRoomsGlassRTCamera`, 0 plugin name; macOS 20 / 10 / 3 / 1. The feature class compiles everywhere with an empty body (no missing script). Importer read back: Editor + StandaloneOSX ARM64, preloaded, Windows/Linux/WebGL/iOS off. WebGL stripper: the delta's WebGL branch is unchanged (strips the prepass shader and the glass `FRGlassRTPrepass` pass, leaves `_FR_GLASS_RT` to the glass track's stripper); iOS also loses `_FR_GLASS_RT`; Windows keeps them (inert, E9) | PASS |
| **Thread safety** (Metal validation, races) | Validation layer with API **and GPU shader validation**, RT tracing (glass 447k px, 0 misses) through views, occluder, Relay, cameras, fracture, stream, broken pane and a 600-frame walk: 0 validation errors, 0 command-buffer errors. The 20 message kinds (all "redundant setting" / "unused binding" performance notices) are the same kinds as with RT Off; two of them name RT resources (the plugin re-binds the raw texture and gather buffers) — harmless. No `waitUntilCompleted` anywhere (grep 0). Code read: ops queue under a mutex, stats atomic, render thread owns Metal state. Two theoretical races noted (E12) | PASS |
| **Lifetime: stream chunks in/out while tracing** | Coverage (natural cadence, no Flush): MISSING 0 and GHOST 0 at start, end of walk, back at the window, 600 frames later (3,574 registered vs 3,558 eligible + 16 hidden priority). 3-lap streaming soak (6 m/s, 100-cell path): registered 3,574 / native meshes 1,638 / BLAS 5.06 MB identical at every lap end, 0 failures, 0 command-buffer errors. Broken pane 437,356 → 0 px on the break frame; destroyed mesh 0 px next frame; Relay moved 0.7 m 0 stale px. But: geometry pool 9.8 → 13.8 → 17.8 → 17.8 MB and the material table 93 → 102 → 111 → 120 over the laps (E3); props hidden at runtime stay traced (E2) | PASS with E2, E3 |
| **Lifetime: exit Play, domain reload** | 3 Play sessions in one editor with domain reload: each traced after 68–69 frames with the same counts, each Play exit released the native scene (0 meshes, 0 retired, 0 MB BLAS / geometry). m2 exit: released after 7 events / 8 editor updates | PASS |
| **Multiple cameras**, P0-A9 | Two opted-in cameras (1920×1080 + 1280×720), 60 frames: +120 trace events, 0 errors; A's frame identical with and without B (0 px); resize 1280×720 → 2560×1440 → 1920×1080, 20 frames each: IoU 0.991–0.996, 0 texels off the glass; camera B destroyed with a frame in flight: 0 errors. Harness game camera + a 960×540 second camera without the component: +100 trace events for 100 frames (game camera only), 0 re-allocations | PASS |
| Occlusion, P0-A5 | unregistered panel in front of the pane: 645 traced px on the panel, 3–7 of them interior (the rest is the 1 px edge ring); prototype 137,551 | PASS |
| Fail closed (codex F1) | fault injected for 12 frames: failed closed once, 2 log entries, 0 ZBinningJob, 0 Render Graph errors, 11 of 11 frames live. Follow-ups missing: E4 | PASS with E4 |
| Fracture API, P0-A13 | 150 ranges prepared 6 frames after `notBeforeFrame` (bar ≤ 4), 24 builds and 0.7–2.2 ms AS per frame; a 20k-triangle refit adds 0.17–1.40 ms (bar 0.3) | FLAG (as in 20 §4.1) |
| **Player smoke**, P0-A12 | Universal Mac app (what Red ships): plugin not packaged, `DllNotFoundException` caught, `off: plugin not loaded`, game runs (10-08 build; nothing that decides this has changed). arm64-only app built today: `on: Apple9, Ultra`, plugin at `Contents/PlugIns/ARM64`, 0 errors or exceptions in two runs, 600-frame walk 0 blank / 0 command-buffer errors, pane break 436,575 → 0 traced px on the break frame. `25_E_player_arm64.jpg` | PASS (arm64) / RT absent in the universal app (E6) |
| **CPU, main thread**, P0-A7 | m2 walk (600 frames at 4 m/s): 0 blank, p50 0.54, p90 5.73, p99 8.95, max 10.44 ms; quiet p99 1.15 (bar 0.5), streaming p99 10.44 (bar 1.5). Player walk (Ultra): p50 0.29 / 0.58, p99 1.82 / 3.54, max 4.12 / 7.14 ms (two runs). Root cause found: E1. `25_E_main_thread.jpg` | FAIL |
| **GPU**, P1-B8 and the shot budget | §6. Player p10, whole frame added by RT at 1440p: v2 +2.19 High / +3.33 Ultra (bars 2.0 / 3.5); v5 +4.25 / +5.25 (bars 5 / 7). 1080p break shot: +2.07 High / +3.29 Ultra vs +2 ms. Stage counters are higher (v2 1440p 4.44 / 5.73 ms). Streaming frames: AS stage 5.7–6.2 ms after the walk (E8) | FAIL (bars not met; idle measurement still owed) |
| Temporal accumulation (not in the design; the implement stage added it) | still camera reaches index 26; a surface whose texture flips red/green every frame with accumulation at 32 still follows every frame (frame-to-frame swing 37.6 → 36.8 of 37.6, 8-bit); a red→green step is 99 % there on the next frame. No ghosting found | PASS |

---

## 3. Issues

Ranked by how much they matter. "Owner" is who fixes it; every RT file is visual-owned.

### E1 · major · Every CPU mesh upload copies the whole reused scratch array (main-thread streaming cost)

- **What.** `FrontRoomsGlassRTNative.FRGlassRT_UploadMeshCPU(int key, Vector3[] positions, VertexAttr[] attrs, int vertexCount, int[] indices, …)` passes the system's reused scratch arrays (`upVertArray`, `upAttrArray`, `upIndexArray` in `FrontRoomsGlassRTSystem.UploadCPU`). They grow to `NextPowerOfTwo` of the largest mesh ever uploaded and never shrink, and the P/Invoke cost scales with the array length, not with `vertexCount`.
- **Evidence** (`RTEngUploadBench`, edit mode, mean of 300 calls): a 96-vertex mesh costs **0.0067 ms** with arrays sized 96, **0.215 ms** with 4,096-entry arrays (a fresh session) and **3.47 ms** with 65,536-entry arrays; a 1,500-vertex mesh with exact arrays 0.087 ms; an empty P/Invoke 0.00003 ms. In the m2 walk the 319 uploads took 501 ms in the native call, **1.57 ms each** (the slowest registration was a 96-vertex carpet block, 2.37 ms). With `MinRegistrationsPerSlice` = 4 a streaming slice therefore costs ≥ 6 ms however small the meshes are — the plateaus in `25_E_main_thread.jpg`, and most of codex F3 / P0-A7.
- **Fix.** Pass pointers for exactly `count` elements: pin once (`GCHandle` or `fixed`, as `BuildFrame` already does for `SubmitFrame`) and declare the imports with `IntPtr` (or `Vector3*`); the same for `FRGlassRT_UploadMeshRanges` (fracture), `FRGlassRT_RefitMesh` (a 20k-vertex refit every frame copies the array) and `FRGlassRT_SetMaterials`. Expected: ~1.5 ms → ~0.01 ms per upload; re-measure P0-A7 after. Owner: glass-rt-track (fix stage). Small, contained change; worth landing with the merge.

### E2 · major (look) · Reflections do not follow props that change without moving

- **What.** Only receivers, glass, "dynamic" instances (renderer name starting with `Key`, children of `Door hinge…`, the Relay, API registrations) are liveness-checked every frame, and only lamp lenses (renderers named `Lens`) re-read emission. Every other static keeps the visibility and material it had when its chunk was scanned: `RefreshStatics` only removes destroyed renderers (32 per frame, round-robin), `Rescan` runs only when a chunk root's direct child count changes, `Describe` reads a material once, `RefreshMaterial` has no caller, and material swaps are watched only on glass.
- **Evidence** (`RTEngGhost`, `25_E_stale_emission.jpg`; mean R−G over the reflected panel, 8-bit):
  - T4 — a static under a chunk root, `renderer.enabled = false` (exactly what `FrontRoomsScreenVideo.TogglePower` does when the player presses E on a CRT): 22.8 with the panel, **22.8 after 3 and after 120 frames** (6.0 without it). Destroyed instead: gone after 1 frame.
  - T2 — emission set by a MaterialPropertyBlock (how `FrontRoomsExitSign` drives its diffusers lit/dead): **93.5 before and after** (raster: green, reflection: red).
  - T3 — material swapped for an instance, then the instance's emission animated (how `FrontRoomsPlacardGlow` drives the EGRESS glow placard): **93.5 unchanged** both times.
  - T1 — a texture whose content changes (the CRT video RenderTexture): follows every frame. Good.
- **Why it matters.** A CRT switched off stays lit in the window behind the player; EXIT signs and the phosphor placard (a story cue) reflect the wrong state. None of these was in the game when the design was written (EXIT sign and placard landed 10-08, CRT power toggle with COV-4).
- **Fix options.** (a) per-frame liveness for the cached static block (a bool check per record, ~2,000 records); (b) a cheap API for content owners — `FrontRoomsGlassRT.SetEnabled(renderer, bool)` / `EmissionChanged(renderer)` — called from `TogglePower`, the exit-sign driver and the placard glow (these files belong to 游戏视觉 / 平面视觉, so the visual chat can add the calls); (c) a "per-frame emission" flag like `EmissiveLens` for renderers that carry `FrontRoomsExitSign` / `FrontRoomsPlacardGlow`. The P2 map events do not cover this. Owner: glass-rt-track.

### E3 · minor · Material and texture tables are never released; geometry pages fragment

- **Evidence** (3-lap streaming soak, same path, snapshots at the start area): materials 93 → 102 → 111 → 113 → 120; geometry 9.8 → 13.8 → 17.8 → 17.8 MB (pages 2 → 5; flat on lap 3) while meshes and BLAS return to 1,638 / 5.06 MB every lap. Textures stayed at 102 on this path.
- **Why.** `MaterialBlock` keys by material instance id and never frees a block; any content that makes a material per object (e.g. `FrontRoomsScreenVideo.MakeMaterial`, one per CRT) adds entries for the session. At 4,096 the block index silently falls back to 0 (a wrong material, no warning). `TextureSlotOf` keeps a native strong reference to every texture it ever saw (a destroyed runtime texture's GPU memory stays alive). Geometry pages are bump-allocated and freed only when every mesh in them is gone.
- **Fix.** Reference-count material blocks and texture slots per instance (release on `RemoveInst`), warn at 90 % of either cap; page compaction can wait for P2. Owner: glass-rt-track.

### E4 · minor · Codex F1 follow-ups are not in the clone

`W/proj_rt` has the try/catch and `FailClosed`, but not the three points of `30_fix_runtime_rt.md` §2:
1. `FrontRoomsMetalGlassRTRendererFeature.cs:229` — `data.seq = … FrontRoomsGlassRTSystem.Warmup()` runs outside the try.
2. No `failedClosed` gate: `Register`, `PrepareMeshes`, `IsPrepared`, `MeshChanged`, `RefitMesh` keep calling the plugin after a fail-closed, and the public `FrontRoomsGlassRT.Quality` setter can switch RT back on in the same session (a future settings menu would), so "off for the session" is not enforced.
3. `ResetStatics` does not clear `qualityOverride` (only matters if domain reload is ever turned off). Also: `FailClosed` writes two console entries; design §2.7 asks for one.

Owner: glass-rt-track; land with the merge (GD3's breakable will be the first caller of the API).

### E5 · minor · Ultra leaves a 1-texel line of stale texels outside the glass

- **Evidence:** m2 Ultra traced radiance has 632 / 632 / 446 / 96 texels more than 1 px outside the glass (v2–v5), always one full row or column at the dispatch-rect border; High 0; identical counts in r8, v1, e1 and m1 (deterministic). `25_E_ultra_stale_texels.jpg`.
- **Why.** The resolve's 1 px dilation reads `rawAA` at the dispatch-rect border, where the AA pass did not write this frame (stale content from an earlier, larger rectangle).
- **Effect.** Off-glass texels are never sampled by the glass shader, so nothing shows today; at a pane edge that coincides with the border a 1-px edge could take a stale colour for a frame. Fix: in the resolve, read `raw` (or nothing) outside the AA grid, or clear `rawAA` over the union rectangle. Owner: glass-rt-track.

### E6 · major for players, Red's call · The universal Mac app ships without the plugin (codex COV-1)

- Unchanged on today's main: the build script compiles `-arch arm64` only and `FrontRoomsGlassRTImporter` pins StandaloneOSX to ARM64, so a universal (x86_64 + arm64) app leaves the dylib out and the player logs `off: plugin not loaded`. It fails safe.
- New evidence for option (a): an arm64-only player (built in a clone with `RTEngPlayerBuild -rtEngArch arm64`) loads the plugin from `Contents/PlugIns/ARM64` and runs RT at Ultra with 0 errors (§2). Option (b) (fat dylib + AnyCPU importer, x86_64 Macs then report "no Apple9" and stay off) still needs its own player test. Owner: Red decides; then glass-rt-track.

### E7 · major · GPU cost over the design bars (known; re-measured)

See §6. Player, 1440p, p10: v2 +2.19 ms High (bar ≤ 2.0) / +3.33 Ultra (≤ 3.5); v5 +4.25 / +5.25 (≤ 5 / ≤ 7). 1080p break shot +2.07 High / +3.29 Ultra against +2 ms — and the player's default is Ultra. m2 ablation at the break-shot pose (editor, p10): shipped 4.43 ms, no shadow rays 3.52, 16 lamps 3.33, 4 lamps 2.32. Same conclusion as `20_implementation.md` §5.3: per-hit lamp cull / light grid first (P2), then D6. An idle-machine player run is still owed before D1/D6. Owner: glass-rt-track P2; Red for D1/D6.

### E8 · minor · BLAS builds on streaming frames reach 5.7–6.2 ms of GPU

After the player walk the AS stage of the last (warm-up) event read 6.24 ms (run 1) and 5.69 ms (run 2); in the m2 walk `gpu_total` p99 6.1 ms, max 11.0 ms. The per-frame cap is 24 builds or ~50k triangles; design §3.1 budgets ≤ 1.0 ms of BLAS GPU on streaming frames. Lower the per-frame triangle budget or spread builds over warm-up events. Owner: glass-rt-track (P2).

### E9 · note · Windows and editor keep the RT shader variants

Windows players keep `_FR_GLASS_RT` variants and the prepass pass (by choice, for P3); they are never drawn (no RT code in the player). Codex F6 is therefore resolved for iOS/Android/Linux only. No pixel effect.

### E10 · note · Run start: reflections fill in over ~5 s

At the first window after a run starts (natural cadence, 10-08 run on the same code) misses fall from 93 % to 0 % only after 330 frames (5.5 s), full registration after 450 frames; main thread 1.5–2.8 ms per frame meanwhile. Misses show the zone cube, so it looks like RT off, not like a defect. E1's fix shortens it. Owner: glass-rt-track.

### E11 · note · A busy TLAS ring reuses last frame's TLAS

When all 8 ring slots of a camera are in flight, `EncodeAccelerationStructures` traces with the previous TLAS, lamps and records (design §1.4 allows it), so "same frame" removal does not hold on that frame. Seen only with the harness's back-to-back renders (ring busy +28 per 100 frames in the cameras phase); 0 in every walk and in the player.

### E12 · note · Two theoretical races (not reproduced)

- `g_SceneReady` (plain `bool`) is read by `FRGlassRT_MeshState(s)` on the main thread and written by device shutdown on the render thread. Make it atomic.
- `RefitMesh` copies new positions with the CPU into the ping-pong half that the frame before last may still be reading. Deforming meshes are not used in the game today; check when one is.
- Also robustness: when the native event returns early (`!s_Metal`, `!g_SceneReady`, no current command buffer) the C# side has already set `_FR_GlassRTWeight` = 1, so the glass would sample last frame's texture. Every other skip path clears the texture to A = 0. Never seen; clear in those paths too.

### E13 · note · Static transforms moved by other code are re-read only round-robin

`RefreshStatics` re-reads 32 statics per frame (~110 frames ≈ 1.9 s for 3,574), so a static that something else moves (no such prop in the game today; the new door-closer kits will animate) lags in the reflection. Classify animated kit parts as Dynamic when they land.

### E14 · hand-offs still open (unchanged)

- Map chat: C2 (`FrontRoomsMapWorld.cs:1490` still adds `FrontRoomsMetalGlassTarget`; harmless hint), C8 (map panes are not `Glass_Window`: in the real build only the window kit's slab receives RT — 404,179 px at v2 instead of 434,712 with the harness swap), the P2 events.
- Screen-ads demo owner: `FrontRoomsScreenAdsDemo.unity:1997` still holds a saved `FrontRoomsGlassRTCamera` (F5).
- `codex_audit_other_visual.sh`'s V2-STOPGAP (flat ink tone in `Describe`) is **obsolete** with this delta: the clone's `Describe` and kernel shade the real paper × print (`MatPrint`, parity wallpaper ratio 1.007 today).

---

## 4. ChatGPT prototype defects (task list) — status on today's main + delta

| # | Suspected defect | Status |
|---|---|---|
| 1 | FOV radians in the tanFovY slot | Gone: no primary rays; the ray origin comes from the raster prepass depth and the GPU matrix. Alignment IoU 0.996–0.999 |
| 2 | Possible vertical mirror | Not present: orientation test correct in RT and mirror |
| 3 | Composite after post | Composite shader deleted; RT radiance enters `FrontRooms/Glass` before transparents and is tonemapped / graded / bloomed with the scene (in the player's change mask the post effects also spread a little of it outside the glass) |
| 4 | Re-traced primary visibility, no depth test | Gone: depth-tested prepass; occluder 3–7 interior px vs 137,551. Skinned meshes and particles are still not traced (the Relay is 16 rigid parts, traced) |
| 5 | 1 s reset + synchronous BLAS on the main thread | Gone: render-thread encode into Unity's command buffer, 0 waits, no resets. Main-thread cost remains (E1) |
| 6 | BLAS on Unity's buffers | Gone: plugin-owned copies (CPU pages, GPU gather once); streaming soak and destroyed-mesh tests clean |
| 7 | Flat shading, sky on miss | Gone: P1 hit shading, parity luminance 1.012, misses 0 %. New gap: E2 |
| 8 | 30 mm cube double reflection | Only the front face enters the prepass; thin-glass back image implemented (not measured, as in 20 §8) |
| 9 | Shared renderer on WebGL/Windows | Provably inert (§2) |
| 10 | GPU cost unmeasured | Measured, over budget (§6, E7) |

---

## 5. Codex audit findings for this track

| ID | State after this verification |
|---|---|
| F1 fail-closed guard | Works (fault test); follow-ups E4 missing |
| F2 live and unverified | Verified here and in 20 |
| F3 main thread | Still over budget; cause found (E1) |
| F4 release at Play exit | PASS (3 sessions, 0 meshes / 0 MB after each exit) |
| F5 edit-mode opt-in | Guard present; the ads-demo scene still needs its re-save (E14) |
| F6 non-Mac shader code | iOS/Android/Linux stripped; Windows kept by choice (E9) |
| F8 wallpaper | Real paper × print; the V2 stopgap is obsolete (E14) |
| §4.3 P5 tag proof | Same-frame proof: P5 0 px changed (tag + 0.5 m rejected), P4 435,162 px |
| COV-1 | Red's a/b; arm64 player works (E6) |

---

## 6. Numbers

**Player (arm64, Ultra default), 16 interleaved rounds, p10, run 1 (run 2 was disturbed by other jobs: load 23 → 54).** Whole frame added by RT = (High or Ultra) − Off, from the plugin's command-buffer timer events; stage = AS + trace + resolve counters.

| View | Res | Off | +High | +Ultra | Stages High | Stages Ultra | Bar |
|---|---|---:|---:|---:|---:|---:|---|
| v2 1.5 m | 1080p | 5.69 | +0.54 | +1.05 | 2.58 (AS 0.93, trace 1.49, resolve 0.11) | 3.32 | — |
| v2 1.5 m | 1440p | 7.55 | +2.19 | +3.33 | 4.44 (0.72 / 3.15 / 0.20) | 5.73 | High ≤ 2.0, Ultra ≤ 3.5 |
| v5 0.55 m | 1080p | 3.96 | +2.07 | +3.29 | 3.08 (0.96 / 1.87 / 0.16) | 4.03 | shot ≤ +2 |
| v5 0.55 m | 1440p | 8.57 | +4.25 | +5.25 | 6.33 (1.15 / 4.18 / 0.34) | 8.25 | High ≤ 5, Ultra ≤ 7 |

Main thread inside the cost loop 0.44–0.62 ms (High), encode 0.23–0.29 ms. Walk (Ultra, 600 frames): p50 0.29, p99 1.82, max 4.12 ms; 0 blank. Memory (one camera): camera targets 71 MB at 1080p (87 MB with the 1440p test target), BLAS 5.1–5.3 MB, geometry 13.7–13.8 MB.

**Editor, run m2 (loaded):** v2 1080p p10 Off 4.58, High 7.36, Ultra 6.58 ms (stages High 3.41, Ultra trace 2.43); v5 1080p Off 20.1, High 36.1 ms — swamped by the other jobs, not used for verdicts. Walk as in §2. Cost ablation at v5 1080p (trace + resolve, p10 / median): shipped 4.43 / 4.87, no shadow rays 3.52 / 4.16, 16 lamps 3.33 / 3.75, 4 lamps 2.32 / 2.84, no back image 4.54 / 4.97 ms.

---

## 7. For the merge stage

- Bases: all 18 modified / added paths and the 3 deletions of `20_implementation.md` §9 still match main (`0a9ee7f` + tree, checked 16:2x today). Build the v2 package from `W/proj_rt` as listed there; there was no package to dry-run.
- Land E4 with it (three small edits) and preferably E1 (P/Invoke by pointer); both are in the two files the package already changes (`FrontRoomsGlassRTSystem.cs`, `FrontRoomsGlassRTNative.cs`, `FrontRoomsMetalGlassRTRendererFeature.cs`). Re-run the walk and the fail-closed phase after.
- Red's editor must restart to load the new dylib.
- After the merge: run `bash W/apply/codex_audit_runtime_rt.sh` (probe only) to see F1/F4/F5 land; tell the ads-demo owner (F5) and the docs owner (F7).

---

## 8. Frames and verification log

All in `rt/images/`, JPG q85, ≤ 1600 px wide. Placed 2026-10-14 in the Figma section FRONTROOMS · VISUAL VERIFICATION LOG (`2595:6093`, page `2099:76`; section grown to 61 rows, 75,720; cover recounted: 242 placed checks, 685 images, 26 tasks; G14 legend `VL031–037, 181–186, 239–243 · 18 checks`). Rows in `Documentation/VERIFICATION_LOG.md` §3.

| VL | Frame | Check | Verdict | Images |
|---|---|---|---|---|
| VL239 | `3124:7230` | Traced glass on today's main | PASS High / FLAG Ultra | `25_E_alignment_overlay.jpg`, `25_E_ultra_stale_texels.jpg` |
| VL240 | `3124:7242` | RT Off leaves frames identical | PASS | `25_E_rt_off_identical.jpg` |
| VL241 | `3124:7253` | Main thread while streaming | FAIL | `25_E_main_thread.jpg` |
| VL242 | `3124:7264` | RT in a macOS player | PARTIAL | `25_E_player_arm64.jpg` |
| VL243 | `3124:7275` | Reflections of changing props | FAIL | `25_E_stale_emission.jpg` |

Every other image in `rt/images/` (01_, 02_, 11*, 20_, 25_AD_) was looked at; the 20_* frames agree with today's run (same IoUs, boundary counts, occluder, Relay, broken-pane and parity numbers; the Ultra line of E5 is visible in `20_P1_v4_steep65.jpg`'s Ultra radiance panel). Note for the G14b track: several of its `11*` images are PNG or wider than 1,600 px (up to 3,552), against the folder's JPG ≤ 1,600 rule.

---

## 9. Work files and clones

- Evidence: `W/rt_work/eng25b/` (harness copies, `m2_out/`, ghost logs and frames, player outputs, `sheets_eng.py`, `resync.zsh`, `val_harness_types.txt`); logs `W/logs/eng25b/` (the 0.9 GB validation log is gzipped).
- Clones of this track that can be removed (nothing was deleted): `W/proj_rt_eng`, `W/proj_rt_eng2`, `W/proj_rt_eng3`, `W/proj_rt_engbase`, `W/proj_rt_engbase3`, `W/proj_rt_engplay`, `W/proj_rt_v`; player builds `W/rt_work/eng25/player/` (10-08 universal, ~0.75 GB) and `W/rt_work/eng25b/player_arm64/` (~0.7 GB). Keep `W/proj_rt` (the payload source) until the merge is applied.
