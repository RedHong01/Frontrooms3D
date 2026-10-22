# 30 · G14 ray-traced glass: final report (package `glass_rt_g14`, independent verification on main, 2026-10-22)

2026-10-22 · 游戏视觉 · workflow stage "G14 independent verification" (Red allowed workflows on 2026-10-21; the first run was cut by a network outage on 10-21 at 23:44 and resumed here) · **verdict: the package is correct, safe and inert off the Mac on today's main, with GD3's glass live. It still fails its cost bars (main thread, GPU at 1440p and at the break-shot pose), it does not register GD3's cracked stages, and it does not make the universal Mac app trace (RD2). It is not landed: this stage only verified it.**

`W` = `/Users/redwang/FrontRoomsVisualWork`. Design: `10_rt_glass_design.md`. Implementation: `20_implementation.md` (§11 lists the 2026-10-14 fix pass). Earlier verification: `25_verify_engineering.md` (E1–E14), `25_verify_art-direction.md` (AD-1–AD-8). Codex audit: `../../codex_audit/10_review_rt.md` (F1–F8), `../../codex_audit/00_main_state.md` (RD2 = COV-1). Stopgap note: `NOTE_1014_v2_stopgap_in_main.md`. The package and `20_implementation.md` point at this file ("30_final.md §6 / §7"); the fix pass that built the package was cut off before it wrote it.

Nothing in Red's project was written except this file and the 13 `rt/images/30v_*.jpg` sheets (claimed in `LANDINGS.md`). No Unity, batch, build or render ran on main. No git command was run. Nothing was downloaded.

---

## 0. Short answer (for Red)

1. **The package still applies cleanly to today's main.** Dry run on main at 06:53 and again at 07:31 (§7): all 16 bases, 3 deletions and 8 kept `.meta` files match. The only notice is a contract file: GD3 replaced `FrontRoomsGlass.shader`. Its six G14 hook blocks are unchanged, and the hook proof shows they work on GD3's shader (§4).
2. **It works with your real glass.** Since GD3 landed, every map window is GD3's slab on `Glass_Fracture`, a copy of `Glass_Window` with `_RTReceive = 1`. The old hand-off C8 is done by GD3. In the game flow and in the player, 23 of 23, 38 of 38 and 68 of 68 slabs took the RT prepass. In engine: the traced glass sits on the drawn glass (IoU 0.996–0.999). There are 0 % misses. Occluders, a moving Relay, a broken pane and a destroyed mesh are right on the same frame. RT traced at weight 0 changes 0 px. Lamps in the reflection follow on the same frame (r = 1.000).
3. **The red wallpaper is gone.** The V2 stopgap is deleted and the real paper × print shading replaces it. In the warmed harness views the red tint is ≤ 0.008 % while RT changes 245k–901k px. In the 84-frame capture set it is ≤ 0.06 %. Main without the stopgap reached 9.99 %.
4. **It is safe.** An exception inside the RT frame turns RT off cleanly: 1 console entry, frames keep rendering, 0 ZBinningJob errors. WebGL, iOS and Windows player scripts contain no RT code, and WebGL and iOS strip the RT shader passes. The native scene empties at every Play exit. In a 120 s play run there were 0 pink renderers and only the 2 expected exceptions. The arm64 player ran clean under Metal API validation (1,449 frames: walk, streaming and a pane break). Under GPU shader validation it also ran clean over the same frames (§3.1 A1).
5. **It is still too expensive.** G14's own main-thread time is 0.45–0.53 ms per frame when standing or walking, up to 4.8 ms on chunk-streaming frames, and 0.17 ms when no window is in view. The design bar is 0.3 ms. GPU cost in the arm64 player at 1440p: High 5.4–6.0 ms, Ultra 7.4–8.2 ms at 1.5 m, and High 6.8–10 ms, Ultra 9.8–13.8 ms with the pane filling the frame. The bars are 2.0 / 3.5 and 5 / 7 ms. All of this was measured under load, so it is an upper bound (§5).
6. **New open items.**
   - **G14V-1:** GD3's S1 and S2 meshes are made during the hold and never registered with G14. On the S2 beat the cracked pane drops from the traced reflection to the zone cube. There are 0 stale entries at any stage.
   - **G14V-4:** the package resets the RT quality at every Play start. This breaks the G10 proof's "force Off", but not the game.
   - **H1–H3:** two verify harnesses predate GD3 or the batch order and need small updates. These are harness issues, not game defects (§2, §4, §8).
7. **RD2 is not in this package.** The dylib is arm64 only (`518d6769…`), so your universal Mac app still has no RT on Intel and falls back cleanly. **A probe shows option (b) needs no downloads:** the package's sources build an x86_64 slice with the installed Xcode, with no warnings, and `lipo` makes a universal dylib (§1.1). It cannot be run here: there is no Intel Mac and no Rosetta. **(Corrected 2026-10-22 in §10: today's universal app has no RT on any Mac, because Unity leaves the ARM64-pinned plugin out of it; Rosetta 2 is installed here; RD2 is now built and run in the clone, natively and under Rosetta.)**
8. **Your decisions, now with numbers (§5.2, §5.3):**
   - **D1, default quality.** Today a standalone player defaults to Ultra. My recommendation is High at every level. Ultra costs 1.9–3.6 ms more at 1440p for no measurable gain in edge crawl or lens shape.
   - **D6, break-shot budget.** At the break pose, RT at 1440p costs 6.8–10 ms on High. A 4-lamp shot budget cuts the trace by 56 %.
   - **D2, player body in dark windows.** A dark window becomes a mirror with nobody in it.

---

## 1. What the package changes (`W/apply/glass_rt_g14.sh`, apply convention v2)

The bases were recorded on 2026-10-14 18:19 on main `0a9ee7f` + working tree. The payload comes from `W/proj_rt` (fix pass 30). The package writes 16 paths, deletes 3 and keeps 8 `.meta` files with main's GUIDs. Line counts are from `glass_rt_g14.diff`.

| Path | Change | What it does |
|---|---|---|
| `NativePlugin/FrontRoomsMetalGlassRT.mm` (+174 −29), `FRGlassRTScene.mm` (+78 −25), `FRGlassRTScene.h` (+27 −3) | M | Shared 4 MB geometry pages with a pre-allocated spare; lock-free stats; per-camera TLAS ring 3 → 8; reference-counted geometry; BLAS budget split (fracture 24 builds / 50k triangles, streamed map meshes 8 / 10k per frame, E8); atomic `g_SceneReady` (E12); the three-pass resolve (AD-1); refits wait for their half (E12) |
| `NativePlugin/FRGlassRTShared.h` (+23 −8), `FRGlassRTLayout.h` (+6 −6) | M | Material record 256 B with the print layer (`FR_MAT_PRINT`), caster mask 0x20, FRFrame 512 B; layout self-test updated |
| `NativePlugin/FrontRoomsGlassRT.metal` (+380 −123) | M | Paper × print wallpaper (codex F8 / V2); anisotropic ray-cone sampling; per-hit ranked shadow lamps with a 0.2 m disc (AD-2); no blur on clean glass (AD-3); separable Gaussian smudge blur inside the dispatch rect (AD-1, E5); unlit CRT; EXIT-sign lamp field |
| `NativePlugin/tools/frglassrt_validate.mm` (+1 −1) | M | Build-time validator for the new pipelines |
| `Assets/Plugins/macOS/libFrontRoomsMetalGlassRT.dylib` | M (binary) | `518d6769bd67d847c5544c91a51f5f71330c6ae8`, 236,624 B, arm64. Main today: `56a351646e940ae10154c1f89dae46a5fb76c0f3`, 201,280 B, arm64. `.meta` kept (GUID; Editor + Standalone OSX ARM64) |
| `Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRTSystem.cs` (+827 −187) | M | Fails closed for the session and gates the API (F1, E4); no edit-mode opt-in (F5); releases at Play exit (F4); sliced scans; live props (CRT, EXIT sign, glow placard, API registrations) checked every frame, statics re-checked 256 per frame (E2); reference-counted material blocks and texture slots (E3); pinned-pointer uploads (E1); **the V2-STOPGAP block deleted** and a real `MatPrint` record added; quality override reset at every Play start (F1 follow-up; see G14V-4) |
| `Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRTNative.cs` (+50 −12) | M | P/Invoke by pointer (`Pins`), new stats, budget setter |
| `Assets/Scripts/Rendering/FrontRoomsMetalGlassRTRendererFeature.cs` (+33 −7) | M | Trace after the prepasses (`AfterRenderingPrePasses`); warm-up inside the try; `FailClosed` (F1) |
| `Assets/Shaders/GlassRT/FrontRoomsGlassRTPrepass.shader` (+1 −1) | M | Occluder tolerance 1 mm + 0.05 % |
| `Assets/Editor/RT/FrontRoomsGlassRTWebGLStripper.cs` (+35 −11), `FrontRoomsGlassRTIsolation.cs` (+6 −2) | M | iOS, Android, tvOS, visionOS and Linux strip the RT passes and `_FR_GLASS_RT` (F6); the isolation check covers iOS |
| `Assets/Editor/RT/FrontRoomsGlassRTVerify.cs` (+1194 −84) | M | The acceptance harness (every P0/P1 phase, plus the fix-pass phases oblique / darkrelay / moving / backimage / hdroff / officeparity) |
| `Tools/rt/compose_rt_sheets.py` | A | Sheets from a harness output folder (outside `Assets`) |
| `Assets/Shaders/FrontRoomsMetalGlassRTComposite.shader` (+ `.meta`) | D (moved to the backup folder) | ChatGPT's unreferenced after-post composite |
| `Assets/Scripts/Rendering/MetalGlassReflection.meta` + empty folder | D | Empty prototype folder |

Not touched: `FrontRoomsMap/*`, `FrontRooms3DGame*.cs`, `FrontRoomsPostStack.cs`, `FrontRooms/Glass`, GD3's `Rendering/Glass/*`, the URP assets and ProjectSettings. WebGL and iOS stay free of G14 (§3, A10).

### 1.1 The plugin: rebuild, sha1s, RD2 probe

| Dylib | sha1 | Size | Arch |
|---|---|---:|---|
| Package payload | `518d6769bd67d847c5544c91a51f5f71330c6ae8` | 236,624 | arm64 |
| **Rebuilt today** from the package's `NativePlugin/` with the repo's `build_frontrooms_metal_glass_rt.sh` (scratch copy `W/rt_work/g14v/pb2_1022`) | **`518d6769bd67d847c5544c91a51f5f71330c6ae8`, byte-identical (`cmp`)** | 236,624 | arm64, `@rpath` |
| Inside the arm64 player built today (`Contents/PlugIns/ARM64/`) | `518d6769…` | 236,624 | arm64 |
| Main today (before the package) | `56a351646e940ae10154c1f89dae46a5fb76c0f3` | 201,280 | arm64 |
| RD2 probe: x86_64 slice, same sources and flags with `-arch x86_64` (scratch only, `W/rt_work/g14v/rd2_probe_1022`) | `319330e7c46eff4ae9734e10dfd9890529be847f` | 213,104 | x86_64, 47 exported symbols (arm64: 47) |
| RD2 probe: `lipo -create` arm64 + x86_64 | `abe0fbf0cbf0abed98aa1f86baed076d280df4dc` | 466,000 | x86_64 + arm64 |

The validator compiled the embedded MSL with the OS compiler in 338 ms. All 9 pipelines built: trace, trace_aa, resolve, parity, gather ×2, layout, blur_h, blur_v. The GPU layout self-test passed: FRInstance 128, FRMaterial 256, FRLamp 64, FRMeshGPU 48, FRFrame 512, FRVertexAttr 24. Toolchain: Apple clang 21.0.0, macOS SDK 27.0, macOS 27.0.1. No Metal toolchain was needed and nothing was downloaded.

The RD2 probe only shows that option (b) builds locally. The RD2 job (VL307) still has to do three things. First, build both slices in `build_frontrooms_metal_glass_rt.sh`; the validator itself stays arm64. Second, give the importer `AnyCPU` or x64 for Standalone OSX. Third, confirm that an Intel Mac reports "no Apple9" and stays Off. This machine cannot run x86_64 code (no Rosetta since macOS 27).

---

## 2. How this verification ran

| Item | Value |
|---|---|
| Clone | **`W/proj_rt_eng3`** (Library from 10-14, used by the 10-21 run). Re-synced on 2026-10-22 06:55 from main `dfc0086` + working tree, exactly as `mkclone.sh` does: rsync of Assets, Packages, ProjectSettings, NativePlugin and Tools with the iCloud excludes; `apply_local_patches.py` printed "nothing to do"; then `install_tools.py`. The clone-only harnesses were moved to `W/_partial/g14v_clone_only_1022_065545/` before the rsync and copied back after it. Main contract state in the clone: Game `b789cd15`, Game.Mobile `1f6124fa`, MapWorld `7ef0ca45` (outlets C-1), ShotTimings `865e3e10`, CameraRig `a77221fb` (EXAMINE step 1), RoomStream `4e6e8e7f`, PostStack `68824661`, `FrontRoomsGlass.shader` `4d378e34` (GD3), `FrontRoomsGlassBreakable.cs` `51de5ccb`, inspect.sh (Rendering/Inspect), outlets C-2 |
| Since the sync | 关卡设计 landed Game `93a58d76` and `FrontRoomsWarnLamps.cs` (W1 warning lamp dips) at 07:09. These are not in the clone. They are not G14 bases. The dips go through the map's lamp overrides, which G14 reads on the same frame (B3), but they were not re-run here |
| Apply | `APPLY_REAL=W/proj_rt_eng3 APPLY_BACKUP=W/backup/g14_clone_1021 glass_rt_g14.sh --apply` printed "MANIFEST OK" (`W/logs/g14v/apply_clone_1022.log`). The clone's manifest was moved to `W/logs/g14v/applied_manifest_CLONE_proj_rt_eng3_1022.txt`, so nobody reads it as main's |
| Compile | Forced reimport of `Assets/Scripts` and `Assets/Editor/RT` (clone-only `G14VReimport`), with `Library/ScriptAssemblies` moved to `W/_partial/`: **0 `error CS`**, 0 `warning CS` (`W/logs/g14v/c02.log`). Roslyn on main's tree with the 6 payload `.cs` files: **COMPILE OK** for macOS and for iOS defines, 7 assemblies, 0 errors (`W/rt_work/g14v/roslyn_1022_{mac,ios}`) |
| Runs | Unity 6000.3.10f1 batch with graphics, `-buildTarget OSXUniversal`, `-force-gfx-mt`, scene `FrontRooms3D.unity`, seed 516574485, test window (282,206)-(282,207) at (271.5, 1.18, 45.0), 1920×1080 4× MSAA. One Unity process on the clone at a time, each with a wall-clock limit (`W/logs/g14v/chain5.zsh`, `chain6.zsh`, `run.zsh`). Logs are in `W/logs/g14v/<id>.log`, outputs in `W/rt_work/g14v/<id>_out/` |
| Load | **UNDER LOAD for every timing**: 1-minute load 103–401 (`W/logs/g14v/load.tsv`), with 2–4 Unity processes from other sessions. Pixel counts, IoUs, error counts and registry counts are exact; every ms is an upper bound |

**Harness corrections (clone only; recorded as open item H; the package's harness should take them before the next run):**
- **H1/H2 (GD3).** `SwapPanes` swapped GD3's slab from `Glass_Fracture` to `Glass_Window`, which dropped `_PaneFromUV`. `GlassOf` looked for the glass under the pane collider, but GD3 hangs it on the window root. Unpatched, the harness saw raster coverage 0 and prepass 0 at the test window (`W/rt_work/g14v/r1a_invalid/`, 10-21). The patch is in `W/rt_work/g14v/harness_patch/harness_g14v.diff`: skip panes whose material already has `_RTReceive`, and look from the window root.
- **H3 (batch order).** In a batch editor, and in a `-batchmode` player, nothing renders through URP until a harness renders. Until then `Shader.globalRenderPipeline` is empty, URP-tagged SubShaders are skipped, and `FrontRooms/Glass` reports 1 pass. A harness that calls `FrontRoomsGlassRTSystem.Flush()` before its first render makes G14 register the panes at that moment. `PrepassBitFor` then finds no `FRGlassRTPrepass` pass and puts them on the override prepass (bit 29) **for the session**, and the override draws almost nothing of GD3's slab.
  - Measured today: 27 of 38 slabs on bit 29 with 2,089 traced px; after one URP render, bit 30 and 449,504 px (`s7` census and round trip).
  - The game's own flow never hits this: the play run, CxPlayRun and the player put every slab on bit 30.
  - The harness runs below re-pick the bit after warm-up (`-g14vRepair`, 76 receivers repaired), and the player smoke renders once through URP first (38 of 38 on bit 30).
  - G14 could re-pick a receiver's bit when `Shader.globalRenderPipeline` changes (G14V-3).

---

## 3. Acceptance P0-A1…A13 and P1-B1…B8 (design §4.1–4.2) on today's main

These runs are fresh, from 2026-10-22 07:00–07:30 on the re-synced clone:

| Run | What it covered |
|---|---|
| `r3a` | views, orient, occluder, relay, dark, lamps, temporal, cameras, capacity, fracture, cost, parity, oblique, darkrelay, moving, backimage, hdroff (187 s) |
| `r3b` | stream, ablation, broken, office, officeparity, walk, pairs, tagproof, failclosed (66 s) |
| `s7` | GD3 stages, standing cost, break-shot pose |
| `p2` | CxPlayRun, 120 s |
| `v2h` / `v2o` | CxVis set with RT High / RT Off |
| `iso2` | isolation |
| `g3` / `g4` / `g5` | G10 proof |
| `gh2` / `sk2` | E2 changed props, E3 3-lap stream soak |
| `pb2` / `ps3` | arm64 player build and smoke |
| `ps4` / `ps5` | player under Metal API validation, and API + GPU shader validation |

`val1` (editor shader validation, 10-21) is cited with its date.

### 3.1 P0

| # | Test | Bar | Result 2026-10-22 | Verdict |
|---|---|---|---|---|
| A1 | Runs in the main game | RT on; 1 event per frame; 0 validation errors | Status: `on: Apple9, High, kernel 4 ms (async), layout OK, residency set, hazard depth 2 / out 2, event barriers off (tracked), timers stage-boundary counters`. The cameras phase logs +100 trace events per 100 frames. 0 command-buffer errors in every run, editor and player. **Metal API validation, player `ps4`:** 1,449 frames (warm-up, Off/High/Ultra, cost rounds at 1080p and 1440p, a 600-frame streaming walk, a pane break), 0 validation messages, no abort, 0 errors. **API + GPU shader validation:** **player `ps5`** (API + GPU shader validation over the same 1,449 frames, 779 s; a traced frame takes 34–90 ms under validation): 0 validation messages, no abort, 0 errors, 0 CB errors; editor `val1` (10-21): 757 frames (warm-up 210, v1 High/Ultra, mirror reference, Play-exit release), 0 validation messages, then the harness's 45-min deadline (a traced frame takes 51–144 ms under shader validation) | PASS |
| A2 | Alignment | IoU ≥ 0.995; boundary ≤ 1 px | Prepass vs the glass's own ≥ 50 % MSAA coverage: **0.997 / 0.997 / 0.997 / 0.996 / 0.999** (v1–v5). Px more than 1 px off the edge: 0 / 1 / 39 / 45 / 479. Traced vs prepass: 0.995 / 0.994 / 0.993 / 0.989 / 0.997. Identical to 10-07, 10-14 and 10-21, now on GD3's slab | PASS (boundary FLAG at 50–65° and 0.55 m, as before) |
| A3 | Orientation | mirrored correctly | Red is right of and above green in RT and in the planar mirror; centroids within 6.5 / 7.7 px | PASS |
| A4 | Transmission kept | mean ΔY ≤ +4/255; weight 0 = Off | ΔY High − Off: −1.01 / −1.25 / −1.19 / −4.09 / −0.51 per 255. RT traced at weight 0 vs Off: **0 px** in all 5 views, also with the event barriers forced on; a second Off frame also 0 px | PASS |
| A5 | Occlusion and lifetime | 0 px, same frame | Occluder: 640 traced px on the panel, 6 interior (prototype 137,551). Relay copy moved 0.7 m: 71,048 px change on that frame, **0** differ from a fresh registration. Broken pane: 437,356 → **0 px on the break frame**, receivers 76 → 74. Runtime mesh: 49,185 px registered; `MeshChanged` moves it 90.55 px on the same frame; 0 px on the frame after `Destroy` | PASS |
| A6 | No sky indoors | misses ≤ 0.5 % | 0.00 % in every view, including dark-beyond and Office | PASS |
| A7 | No hitch | 0 blank; main p99 ≤ 0.5 quiet / 1.5 ms streaming; encode p99 ≤ 0.2 ms; 0 waits | 600 frames at 4 m/s, 30 chunk roots, 581 frames with a receiver: **0 blank**, 0 CB errors, 0 GC frames. Main thread: p50 0.46, p95 0.69, p99 1.42, max 4.82 ms. Quiet frames (561): p50 0.45, p95 0.68, p99 0.92. Streaming frames (39): p95 1.53, p99 4.82. Encode p50 0.19, p99 0.67 ms (UNDER LOAD 290–380) | **FAIL** (main thread and encode over budget) |
| A8 | Capacity and priority | cap holds; priority kept; nothing within 20 m dropped | +3,000 instances (6,612 registered): High TLAS 2,048 (the cap), Ultra 2,606; 701 priority instances kept; 0 dropped within 20 m | PASS |
| A9 | Cameras | RT only on the opted-in camera; 0 re-creations | Second 960×540 camera: no RT component; +100 trace events per 100 frames; 0 target re-allocations; "ring busy" +28 (the second camera's renders find the ring slot in flight; no frame is lost: trace events = frames); the Scene view returns before enqueueing | PASS |
| A10 | Isolation | no RT in WebGL; importer; strippers | Player-script compile: WebGL / iOS / Windows64 `Assembly-CSharp.dll` hold 0 `FRGlassRT_`, 0 plugin name, 0 `FrontRoomsGlassRTSystem`, 0 `FrontRoomsGlassRTCamera`; macOS 21 / 1 / 12 / 3. Importer: Editor (OSX, ARM64) + StandaloneOSX (ARM64), preloaded, everything else off. Strippers: WebGL removes the prepass shader and the glass's `FRGlassRTPrepass` pass and keeps ForwardLit; iOS also removes the `_FR_GLASS_RT` variants; macOS keeps everything; Windows keeps them (inert, E9) | PASS |
| A11 | GPU timer + pass split | per-stage ms; split ≤ 0.3 ms | Stage counters in every view (§5). Empty RT passes at the new placement vs Off (medians): −0.04 / −0.01 / +0.02 / −0.36 ms (v2 1080p, v2 1440p, v5 1080p, v5 1440p). The old placement: +1.03 / +0.65 / +0.26 / −0.59 | PASS (timer, split within noise) |
| A12 | Player smoke | 0 crashes, 0 CB errors; 3-min autopilot with streaming and a break | arm64 player built in the clone with `BuildPipeline.BuildPlayer` (URP intact, 714.6 MB; the one build error is FMOD's bank refresh, because clones carry no FMOD project). Plugin `518d6769` packaged at `Contents/PlugIns/ARM64`. Smoke `ps3` (65 s): default quality Ultra (level 5); 38 of 38 GD3 slabs on bit 30; 404,179 glass px traced; a 600-frame streaming walk with 0 blank, main p50 0.45, p99 1.53 ms; break: 436,575 → 0 px on the break frame; 0 errors or exceptions, 0 CB errors. `ps4` (78 s, Metal API validation) and `ps5` (779 s, API + GPU shader validation) gave the same result | PARTIAL (no crash in 922 s of scripted player time over three runs, with streaming and a break; the 3-min game autopilot is not available in a player) |
| A13 | Fracture API | 150 ranges ≤ 4 frames at ≤ 2 ms; 20k refit ≤ 0.3 ms | 150 ranges prepared 6 frames after `notBeforeFrame` (none before), 24 builds per frame, AS 1.3–1.6 ms. Refit above the plain TLAS frame: −0.14 / 0.04 / 0.53 / −0.07 / −0.14 / −0.11 ms | FLAG (frames) / PASS (refit, median −0.09) |

### 3.2 P1

| # | Test | Bar | Result 2026-10-22 | Verdict |
|---|---|---|---|---|
| B1 | Parity (primary rays through the hit shader vs raster) | per class mean ≤ 3 %, p95 ≤ 8 %; lens 0.97–1.03 | Whole-frame luminance ratio **1.006**. L0 wallpaper: mean 3.5 %, p95 12.6 %, ratio 1.006 (the real print, no stopgap). Carpet: 3.3 % / 8.4 %, ratio 1.017 (Ultra 3.2 % / 7.5 %). Ceiling: 0.7 % / 2.8 %, ratio 0.987. Lens ratio 1.001. Walnut 0.946. Prop_Rubber 0.437 (19 px). Office view: ratio 1.024; L0 wallpaper 6.2 % / 21.3 %, carpet 6.8 % / 19.1 %, Office_Wall 0.907, Prop_SteelBrown 1.193, Prop_Rubber 0.443 (1,302 px) | PARTIAL (ceiling and lens PASS; carpet within 0.4 points; wallpaper p95 over; Prop_Rubber reflects at 44 % of its raster luminance, a new small FLAG) |
| B2 | Mirror reference | ratio 0.9–1.1 in ≥ 95 % of 64-px blocks | v1 92.7 % (median 1.037), v2 94.3 % (1.023), v3 95.5 % (1.016) | PARTIAL (v3 PASS) |
| B3 | Lamps in sync | r ≥ 0.99 over 120 frames; off on the same frame | 20 lenses in the reflection. The 7 that change ≥ 10 % each have same-frame r **1.000** (a 1-frame lag would give −0.41 to 0.998). A lamp switched off: 4.706 → 0.090 (1.91 %) on that frame | PASS |
| B4 | Art-review set | by eye | Made fresh (VL284 `30v_AD6_showcase.jpg`, VL316 `30v_D2_dark_window.jpg`): 45° and 60° with troffer rows, dark-beyond Off / High / Ultra, the Relay 2 m behind in a dark window plus a 30-frame crossing, Office, a 60-frame move, HDR off | PENDING (Red) |
| B5 | Thin-glass back image | 2–4 px at 0.5 m (1440p), weight 0.45–0.50 | At 0.75 m / 52°: best shift 1.00 px at 1080p = 1.33 px at 1440p (≈ 2 px at 0.5 m by the optics). The harness's weight fit (0.024) does not isolate the back image | FLAG (offset as predicted; weight not measurable this way) |
| B6 | Anti-aliasing | High ≤ 60 %, Ultra ≤ 35 % of the unfiltered crawl | 1 cm/frame strafe: unfiltered 0.017, High 0.014, Ultra 0.014 (82 %). The raster's own pixels: 0.083, 5× the reflection | FLAG (bar not met; no visible crawl) |
| B7 | Smudges | a blurred lens, not the cube | 60° view: 19 smudged lens pixels, all traced (the live room, blurred) | PASS (small sample) |
| B8 | Cost (player, idle, 1440p) | v2 High ≤ 2.0, Ultra ≤ 3.5; full pane High ≤ 5, Ultra ≤ 7 ms | arm64 player, RT stages p10 / median: **v2 High 5.43 / 5.98, Ultra 7.41 / 8.21; v5 (pane fills the frame) High 8.18 / 9.99, Ultra 11.77 / 13.75 ms** (`ps3`, load ≈ 290). In a quieter minute (`ps4`, load 140–200): v2 High 5.47 / 5.62, Ultra 7.39 / 7.73; v5 High 6.82 / 7.04, Ultra 9.76 / 9.99 | **FAIL** (as measured under load; the bar is for an idle machine) |
| — | Temporal accumulation | converges, no ghosting | Still camera: index 26, mean \|Δ\| 0.001. A texture flipping every frame still follows (swing 37.8 → 37.0 at accumulation 32). A red → green step shows on the next frame | PASS |

---

## 4. The other checks this stage was asked for

| Check | Result | Verdict |
|---|---|---|
| **F1 fail closed** (a fault injected in `BuildFrame` for 12 frames) | Failed closed once; quality is now Off; **1 console entry** (`[FrontRoomsGlassRT] off for this session: InvalidOperationException while building the RT frame`); 0 ZBinningJob errors, 0 Render Graph errors; **11 of 11 frames changed** while the camera moved (mean luma 72.8 → 72.9–84.3). Main without the guard: 24 errors and 12 of 12 frames frozen | PASS |
| **F3 main thread** (budget 0.3 ms) | Standing at the window, 300 still frames: p50 0.53, p95 0.80, p99 0.90 ms. Facing away (no receiver in view): p50 0.17, p95 0.22, p99 0.28 ms. Walk: quiet p50 0.45 / p95 0.68 / p99 0.92; streaming p95 1.53 / max 4.8 ms. 120 s autopilot (7,200 map frames): p50 0.35, p95 0.73, p99 1.51, max 12.8 ms (the max is at map second 0–1). The cause is no longer uploads (319 uploads, 0.48 ms native in total, slowest 0.011 ms). It is the per-frame liveness, lamp and lens reads (breakdown: live 0.06–0.16, lamps 0.20–0.45 ms). These are P2 map events (C3/C5/C6/C7). Load 270–340 | **FAIL** (only facing away meets 0.3 ms) |
| **GPU cost** 1080p / 1440p | §5 | FAIL against the B8 bars |
| **L0 wallpaper red tint** without the stopgap | **Warmed harness views** (r3a, RT High vs Off, R − G rise > 10): v1–v5 0.000 / 0.006 / 0.000 / 0.000 / 0.000 %, o45 0.007 %, o60 0.000 %, dark-beyond 0.008 %, while RT changes 245k–901k px per view. **CxVis set** (84 frames): max 0.06 %, 0 frames over 0.1 %. Main without the stopgap: 3.56 / 9.99 %; with it: 0.02 / 0.01 %. Note: CxVis captures each window 12.6–14.6 s into the run without a G14 warm-up. Today 6 of its 20 window frames were not traced at all (RT High = Off pixel for pixel, win1/win2), against 0 of 20 on 10-21. The verdict therefore rests on the warmed harness views. The walk shows no untraced frames (0 blank in 581) | PASS |
| **120 s CxPlayRun, G14 on** | `on: Apple9, High`; **0 pink renderers**. Console: the 2 expected exceptions (FMOD `BankLoadException`, QuickSearch `ArgumentOutOfRangeException`), FMOD's file error and 6 warnings (4 of them FMOD's "port 9264 in use", because another session's FMOD listens). 0 command-buffer errors; 23 GD3 slabs on bit 30, 0 on 29. GPU per map frame: p50 1.9, p95 3.5 ms. Today's route had 0 catches and never put glass pixels on screen (10-21: 2 catches, glass in 12 s), so this run proves stability and coverage, not the look | PASS |
| **G10 proof `-frGlassRT high`** (`g3`) | 131 checks, 45 failed. As W/TOOLS.md §7.11 says, with the live RT on the proof cannot drive the hook's globals: the RT pass owns them, and its End pass turns the keyword off after every render. P1 "the pane changes" gives 0 px in 11 of 11 views | FINDING (negative control, not a gate) |
| **G10 proof without a flag** (`g4`) | Logs `live Glass RT quality High (forced off for the proof)`: the force-off did not hold (**G14V-4**). The result is the same as `g3` | FINDING (G14V-4) |
| **G10 proof with `-frGlassRT off`** (`g5`): the real hook proof on GD3's shader | 175 checks, 47 failed. **P1 the pane takes the red RT input: PASS in 11 of 11 views** (e.g. 435,182 of 452,354 pane px, 0 outside). **P2 orientation, P3 depth tag 1000 m, P4 own-depth tag, P6 render scale 0.75: PASS.** The failures are the proof harness, not the hook: (a) 12× "props, every pane hidden, keyword on": the proof hides the old pane cubes but not GD3's slabs, so the differing pixels are exactly the pane's (435,182 = P1); (b) 33× (ii)–(iv) and (i): the same 6,420 pane px (max \|Δ\| 4.8 × 10⁻⁷) with the keyword off, because the pane was rebuilt by streaming between pass 1 and pass 2; (c) 1× P5 at the dead-lamp double window, the known case the verify harness's tagproof phase clears (P5 0 px there) | PASS (hook) / FLAG (G10 proof predates GD3: H4) |
| **GD3 stages × G14** | §6 | PASS (0 stale entries) / FAIL (S1/S2 not registered) |

---

## 5. Cost for Red's decisions

### 5.1 Numbers (ms; RT stages = AS + trace + resolve; p10 / median of 16–18 interleaved rounds; UNDER LOAD)

| Pose, resolution | Player High | Player Ultra | Editor High | Editor Ultra (median) | Bar (B8) |
|---|---|---|---|---|---|
| v2 1.5 m, 1080p | 2.97 / 3.32 | 4.39 / 4.86 | 3.80 / 4.77 | 5.56 | — |
| v2 1.5 m, 1440p | 5.43 / 5.98 | 7.41 / 8.21 | 5.34 / 6.34 | 7.83 | High 2.0, Ultra 3.5 |
| v5 0.55 m (pane fills the frame), 1080p | 3.99 / 4.35 | 5.77 / 6.68 | 7.01 / 8.42 | 10.90 | — |
| v5 0.55 m, 1440p | 8.18 / 9.99 | 11.77 / 13.75 | 11.92 / 12.68 | 18.68 | High 5, Ultra 7 |
| quieter minute (player `ps4`): v2 / v5 1440p | 5.47 / 5.62 · 6.82 / 7.04 | 7.39 / 7.73 · 9.76 / 9.99 | — | — | — |
| break pose 0.55–0.8 m looking down, 1080p, one frame each (`s7`) | — | — | S0 18.2 · S1 11.0 · S2 2.7 | S0 21.1 · S1 15.6 · S2 3.6 | — |
| Office window 0.55 m, 1080p, one frame (r3b) | — | — | — | 17.1–21.0 | — |

- Whole frame in the player (p10): v2 1440p Off 8.28, High 10.19, Ultra 12.55 ms; v5 1440p Off 8.22, High 14.07, Ultra 17.69 ms. In the quieter minute: v5 1440p Off 7.20, High 11.57, Ultra 14.49 ms.
- Where the trace goes (r3b ablation, v5 1080p High, trace + resolve p10 / median):

  | Variant | ms |
  |---|---|
  | As shipped | 5.85 / 6.33 |
  | No shadow rays | 3.99 / 4.40 |
  | 16 lamps | 4.51 / 5.13 |
  | **4 lamps** | **2.55 / 2.96** |
  | No back image | 5.80 / 6.19 |

  Most of the trace is the per-hit loop over ~86 lamps plus the shadow rays.
- Sheet: `30v_cost_D1_D6.jpg`.

### 5.2 D1 (default quality) and D6 (break-shot budget)

**D1.** Today's rule maps quality level 5 to Ultra and levels 3–4 to High. A standalone Mac player starts at level 5, so the shipped app runs **Ultra**. Ultra costs **+1.9 to +3.6 ms** over High at 1440p. What Ultra buys:
- traced AA and 8 instead of 6 shadowed lamps;
- crawl 0.014 vs 0.014 (B6), i.e. no difference;
- carpet parity 3.2 % vs 3.3 %;
- lens footprints about the same.

**Recommendation: High at every level; Ultra only as an explicit setting.** The default lives in `FrontRoomsGlassRTSystem.DefaultQuality()`, a visual file.

**D6.** At the break pose the RT costs 6.8–10 ms at 1440p on High in the player, and up to 18 ms in single editor frames looking down at a lit ceiling. Office windows cost about 3× a Level 0 window. The options, with numbers:
- **(a)** Accept it. The whole frame is ~11.6–14 ms at 1440p High on this M3 Max, even under load.
- **(b)** A shot budget of 4 lamps during the hold: −56 % trace (5.85 → 2.55 ms at 1080p).
- **(c)** Drop Ultra → High for the shot. This matters only if D1 stays Ultra.

**Recommendation: D1 = High now, and (b) once P2's per-hit lamp cull lands.** One caveat: the shot is cheap at S2 today only because the S2 pieces are not registered (G14V-1). Once they are registered, S2 costs about what S0 costs.

### 5.3 D2: the player has no body in dark windows

A dark-beyond window becomes a readable mirror (`30v_D2_dark_window.jpg`). With RT High, the room behind, its lamps and the Relay copy 2 m behind show in the pane, but nothing stands where the player stands, because the camera has no body in the reflection rays. In a lit-to-lit window the 8 % pane reflection hides this. In a dark window at 1.5 m head-on, the empty spot is plain once you know it is missing. The Relay itself reads only faintly in the final frame: its body is −1.5/255 against RT Off, and its boots read best.

This is Red's call (D2). A body proxy in reflections (a capsule or the planned first-person body, reflection-only, on the Dynamic mask) costs about one more dynamic instance per frame. Leaving it out keeps the "vampire" look.

---

## 6. GD3 stages × G14 (glass S0–S2 live in main since 10-21 21:35)

Clone-only harness `G14VStages`, test window at 1.5 m, High. The beats are driven through `map.BeginGlassHold` / `map.Hold`. For each step it records:
- which GD3 renderers are drawn, and which of them are in G14's TLAS submit list (hit-id mode);
- their prepass bit, traced and prepass pixels, and TLAS size;
- stale entries, i.e. TLAS records whose renderer is destroyed, disabled or inactive.

Output: `W/rt_work/g14v/s7_out/`.

| Step | GD3 on screen | In TLAS (slab / S1 / S2) | Traced px | TLAS | Stale |
|---|---|---|---|---|---|
| S0 | slab | yes / – / – | 437,356 | 2,005 | 0 |
| E-down +1, +5, +20 frames | slab (S1 and S2 meshes built in slices, hidden) | yes / no / no | 437,356 | 2,005 | 0 |
| S1 beat (and +1, +3, +30) | slab + S1 fins | yes / **no** / no | 437,356 | 2,005 | 0 |
| S2 beat (and +1, +3, +30) | S2 pieces | no / no / **no** | 2,089 (another window) | 2,004 | 0 |
| S3 beat (and +1, +3, +30) | nothing | no / no / no | 2,089 | 2,004 | 0 |
| S2 restored after its chunk dropped and was rebuilt | S2 pieces | no / no / **yes** | 437,287 | 2,015 | 0 |

- **No stale acceleration-structure entries at any step.** The slab leaves the TLAS on the S2 frame, nothing of the window is left at S3, and there are 0 CB errors.
- **Coverage gap (G14V-1).** The S1 fins and the S2 pieces are created under the window root during the hold, after G14 scanned the chunk. G14 rescans a chunk only when its root's direct child count changes, plus once 1 s after the chunk appears, so these meshes are never registered.
  - At S2 the cracked pane drops from the traced reflection to the zone cube on the beat frame: a visible pop exactly in the break shot.
  - After a chunk rebuild GD3 restores S2 inside `WindowBuilt`, before the scan, so the same pane then traces (437,287 px). Live and restored windows disagree.
  - **Fix** (both files are 游戏视觉's): GD3 calls `FrontRoomsGlassRT.Register(renderer, Glass | FractureEdge, windowId)` for its stage renderers on `MeshesReady`. That matches design §4.7 P4: `PrepareMeshes` before the beats, never on a beat frame. Alternatively, G14 rescans a window root when GD3 raises `MeshesReady` / `StageChanged`.
  - The hook already keeps traced light off cracks and crushed glass, so registering the pieces is safe.
- Break-shot pose costs are in §5.1. At S2 nothing traces, so the break shot is cheaper than S0 today, but for the wrong reason.

---

## 7. Contracts and the re-check

- **The re-check right before this report:** the dry run on main at 2026-10-22 07:31 (`W/logs/g14v/dry_main_final_1022.log`) printed "DRY RUN OK": all 16 bases and 3 deletions match, all 16 payload files match, the kept `.meta` files are unchanged, and the only NOTE is the GD3 shader below; main still ships the old dylib `56a35164…`. No `rebase_apply.sh` was needed.
- **`Assets/Resources/Rendering/FrontRoomsGlass.shader` moved** (`72e0ddedd…` → `4d378e34…`, GD3 `gd3_stages`).
  - The six G14 hook blocks are unchanged: receiver flag, keyword, globals G-2/G-3, roll attenuation, and the RT radiance swap (which now also fades out on GD3's cracks and crush).
  - The seventh, the `FRGlassRTPrepass` pass, gained GD3's `_PaneFromUV` path: pane metres from UV0, hash keys, bow and crush in the traced normal; fracture faces, crushed glass and outer edges are clipped.
  - The G10 proof (`g5`, P1–P4, P6) and the alignment numbers above were measured on this shader. **The package needs no change for it.**
- Other kept contract files are unchanged: `FrontRoomsGlassRT.cs`, `FrontRoomsGlassRTCamera.cs`, `FrontRoomsGlassRTImporter.cs`, `FrontRoomsGlassRTStripper.cs`, `build_frontrooms_metal_glass_rt.sh`, and every kept `.meta`.
- **Map chat:**
  - C1 is done.
  - C2 (`FrontRoomsMetalGlassTarget` on the pane cube) is harmless twice over, since GD3 disables the cube renderer.
  - **C8 is done by GD3.**
  - The P2 events (Created, Destroyed, ChunkBuilt, ChunkReleasing, RoomDressed, CopyLampStates) are still what F3 needs.
  - The W1 warning dips (07:09) use the map's lamp overrides, which G14 already reads every frame.
- **GD3 (visual):** register the stage meshes (§6).
- **Screen-ads demo owner:** re-save `FrontRoomsScreenAdsDemo.unity` (F5).
- **RD2 job:** universal dylib plus importer `AnyCPU` for Standalone OSX (§1.1).

---

## 8. What still fails, and open defects

| ID | Severity | What | Owner |
|---|---|---|---|
| F3 / A7 | major | Main thread 0.45–0.53 ms standing or walking; 1.5 ms p95 and 4.8 ms max while streaming; 12.8 ms once at map start. Bar: 0.3 ms p99 | glass-rt-track P2 + map events |
| B8 / D6 | major | GPU over the design bars at 1440p and at the break-shot pose (§5) | glass-rt-track P2 (per-hit lamp cull / light grid), then Red |
| G14V-1 | major (look) | GD3 S1/S2 meshes are never registered; S2 pops from traced to zone cube; live and restored windows disagree (§6) | 游戏视觉 (GD3 + G14) |
| RD2 / COV-1 | major for players | The dylib is arm64 only, so the universal app has no RT on Intel. The x86_64 slice builds locally (§1.1) | RD2 job (VL307) |
| G14V-4 | minor | `ResetStatics` now clears the quality override at every Play start (F1 follow-up). An edit-mode `FrontRoomsGlassRT.Quality = Off` (the G10 proof's force-off) no longer holds. The game is unaffected: settings and `-frGlassRT` still work. Fix: re-apply a requested override after the reset, or make the proof set it on the first Play frame | 游戏视觉 (next package revision or the G10 tool) |
| H1–H3 | minor (harness) | The package's `FrontRoomsGlassRTVerify.cs` predates GD3 (H1/H2) and registers before the first URP render in batch (H3). Patch: `W/rt_work/g14v/harness_patch/` | 游戏视觉 (next package revision) |
| H4 | minor (tool) | `FrontRoomsGlassG10Capture` predates GD3: its "every pane hidden" and pass-2 baselines ignore GD3's slabs | 游戏视觉 (W/tools) |
| G14V-3 | minor | `PrepassBitFor` is decided once per registration. A receiver registered before URP exists stays on the override prepass, which draws almost nothing of a GD3 slab. Only a harness hits it today | glass-rt-track |
| B1 rubber | flag (new) | Prop_Rubber reflects at 0.44 of its raster luminance (19 px in L0, 1,302 px in the Office) | glass-rt-track |
| A13 | flag | Fracture prepare takes 6 frames (bar 4) | P4 |
| B1/B2/B5/B6 | flag | Wallpaper p95 12.6 %; mirror blocks 92.7–95.5 %; back-image weight not measurable; AA bar not met (no visible crawl) | glass-rt-track |
| AD-3 head-on | flag | Reflected troffers head-on are +22.0 / +24.8 % footprint and −9.0 / −9.4 % core vs a mirror. At 45°: +5.7 % / −3.5 %; at 60°: +7.9 % / −5.9 % (bar +10 % / 5 %) | glass-rt-track |
| E2 statics | note | A generic static hidden at runtime leaves the reflection after up to 14 frames (256-per-frame cursor; measured 9). CRTs, EXIT signs and the placard are per-frame live props | by design |
| E3 pages | note | Geometry pages 3 → 5 (13.8 → 17.8 MB) over a 3-lap soak. Materials are flat at 98; meshes (1,677) and BLAS (5.15 MB) are identical at every lap end | P2 compaction |

---

## 9. Verification log, images and files

Images, all in `rt/images/` (JPG q85, ≤ 1,600 px wide, made 2026-10-22 by `W/rt_work/g14v/compose_g14v2.py`):

| Image | Shows |
|---|---|
| `30v_E_fixes_table.jpg` | E1–E8, F1, F3 |
| `30v_P0_walk_chart.jpg` | the walk: main thread and RT GPU per frame |
| `30v_AD1_speckle.jpg` | smudge speckle after AD-1 |
| `30v_AD3_lens.jpg` | clean glass vs a planar mirror |
| `30v_P1_parity.jpg` | hit shading vs the raster, Level 0 |
| `30v_P1_office.jpg` | Office window parity |
| `30v_AD6_showcase.jpg` | the shots RT is for |
| `30v_GD3_stages.jpg` | GD3 stages S0–S3 vs G14 |
| `30v_H3_prepass_bit.jpg` | H3 and G14V-4 |
| `30v_player.jpg` | the arm64 player |
| `30v_cost_D1_D6.jpg` | the cost table |
| `30v_V2_red_tint.jpg` | the L0 wallpaper red tint |
| `30v_D2_dark_window.jpg` | the dark window without the player |

VL slide specs are in `W/vl_pending/glass_rt_g14.md`: VL281–VL284 filled with fresh frames, plus VL312–VL316. The 游戏视觉 main session places the rows in `Documentation/VERIFICATION_LOG.md` and the Figma slides.

Work files:
- `W/logs/g14v/`: every Unity log, `run.zsh`, `chain5.zsh`, `chain6.zsh`, `load.tsv`, the apply logs and the clone manifest.
- `W/rt_work/g14v/`: harness outputs `r3a_out` … `ps5_out` (10-21's `r2a_out` … for comparison); clone-only harnesses in `harness/`; the harness patch; the plugin rebuild `pb2_1022/`; the RD2 probe `rd2_probe_1022/`; the player `player_1022/`; analysis scripts `walkstats.py`, `redtint.py` and `compose_g14v2.py`.
- The 10-21 draft `30_final_draft.md` is superseded by this file.

Clone `W/proj_rt_eng3` holds the applied package plus the clone-only scripts (`Assets/Editor/G14V/`, `Assets/G14V/`) and can be removed after the merge. `W/proj_rt`, the payload source, was not touched.


---

## 10. Universal plugin (RD2)

2026-10-22 07:35–08:08 · 游戏视觉 · workflow stage "RD2: universal plugin" · Red's RD2 = (b), 2026-10-21 · **verdict: done and verified in the clone, not landed. The G14 plugin now builds as one universal dylib (arm64 + x86_64). The universal Mac app ships it and traces on Apple silicon. Under Rosetta 2 the x86_64 slice loads and runs cleanly. The arm64 slice is byte-identical to the package's arm64 dylib.**

Nothing in Red's project was written except this section, one pointer in §0 item 7, and `rt/images/30v_RD2_universal.jpg`. The work was claimed in `LANDINGS.md`. No Unity, batch, build or render ran on main. No git write command was run. Nothing was downloaded.

### 10.0 Short answer (for Red)

1. **The plugin now builds for both CPUs.** `build_frontrooms_metal_glass_rt.sh` builds the two slices with the same flags and joins them with `lipo`. The result is `c138bf1df3448d4cd000e90fd9fc7e5c7a223652`, 466,000 B, x86_64 + arm64.
   - The arm64 slice is **byte-identical** to the G14 package's arm64-only dylib `518d6769…` (`cmp`). Apple-silicon behaviour cannot change.
   - The x86_64 slice needed no source change. Nothing was downloaded: the installed Xcode was enough.
2. **RD2 matters on Apple silicon too, not only on Intel.** The importer pinned the plugin to ARM64, and Unity leaves an ARM64-only plugin out of a universal (x64ARM64) app completely.
   - The Alpha snapshot `Builds/Latest/FRONTROOMSSS_Mac_20261021-2221.zip` contains **no** `libFrontRoomsMetalGlassRT.dylib` (`unzip -l`). Today's shipped Mac app has no RT on any Mac.
   - With RD2 the plugin is set to `AnyCPU` for Standalone macOS. The universal app ships it at `Contents/PlugIns/`.
3. **The universal player works on both CPUs.** It was built in the clone with `BuildPipeline.BuildPlayer` (URP intact, x64ARM64) and run twice with the scripted smoke (walk, streaming, a pane break). Both runs had 0 errors, 0 exceptions and 0 crashes.
   - **Native (arm64):** dyld loads the plugin's arm64 slice; caps `0x30ff`; RT on (Apple9, High).
   - **Rosetta 2 (x86_64):** dyld loads the x86_64 slice; caps `0x30ff`; RT stays **on**. The process is x86_64, but the GPU is still the M3 Max.
   - **A real Intel Mac** reports "no Apple9" and stays on the raster path. That gate is unchanged and is the same branch M1/M2 take. This machine cannot execute it.
4. **Correction.** Rosetta 2 *is* installed on this Mac (package `com.apple.pkg.RosettaUpdateAuto`, installed 2026-10-14). §0 item 7 and §1.1 said it is not.
5. **WebGL and iOS are unchanged.** They still contain no G14 player code and still strip G14's shader passes.
6. **To land:** 4 files, all together, after `glass_rt_g14` (its dylib is the arm64 base). The payload is staged in `W/rt_work/rd2/payload/` (§10.6).

### 10.1 Results per item

| # | Item | Result | Verdict |
|---|---|---|---|
| 1 | Universal build, `@rpath`, x86_64 compile, runtime capability guard | **Build:** both slices, `-arch arm64` / `-arch x86_64`, identical flags, `lipo -create`; install name `@rpath/libFrontRoomsMetalGlassRT.dylib` in both slices. **Compile:** 0 warnings, 0 source changes. **Exports:** the same 47 in each slice. **Signing:** the linker signs only arm64 ad hoc, so the script signs the x86_64 slice ad hoc too, as Unity's own universal player and FMOD's bundles do. **Validator:** runs arm64 and, when Rosetta is installed, also x86_64; both compile the embedded MSL and build 9 of 9 pipelines; the GPU layout self-test is OK in both (FRInstance 128, FRMaterial 256, FRLamp 64, FRMeshGPU 48, FRFrame 512, FRVertexAttr 24). **Reproducible:** a rebuild from a scratch copy gives the same sha1. **Guard:** `InitDevice` already returns before any acceleration structure, kernel compile or scene allocation unless `supportsRaytracing`, `MTLGPUFamilyApple9`, `MTLGPUFamilyMetal3` and macOS 14 are all present; `TraceEvent` and the mesh queries check `g_SceneReady`; C# `PermanentlyOff` stops per-frame native calls | **PASS** |
| 2 | `lipo -info` lists both slices; the arm64 slice behaves identically | `lipo -info`: `x86_64 arm64`. The arm64 slice is 518d6769, byte-identical. **Re-runs** with the universal dylib: verify subset views + oblique + dark (57 frames, VL282/VL284/VL316 sources); the G10 proof with the live RT off and on (twice). **Geometry:** glass px, IoU (prepass/forward and traced/prepass) and miss % are identical to step 1 in all five views. **Pixels** are not bit-identical in any run, including RT Off frames, which the plugin does not draw: the zone lamps flicker in real time (e.g. lamp level 0.971 vs 0.995 at the same view). A control run with the arm64-only dylib differs from step 1 by as much; universal vs control differs *less* (§10.3). **G10 hook proof:** P1 pane pixels identical to step 1 in 10 of 11 views. Verdict flips (1–4 per run) move between views from run to run, sit mostly outside the pane at max\|Δ\| ≤ 0.008, and also occur with the live RT off | **PASS** (identical by construction; frame differences are run-to-run noise, measured against a control) |
| 3 | Importer: Standalone OSX universal, GUID kept, Editor unchanged | `.meta`: `OSXUniversal CPU: ARM64 → AnyCPU`. GUID `9b3bca949509f45ffaec699963f56221` kept. Editor (OS OSX, CPU ARM64) unchanged; every other platform off; preloaded. `FrontRoomsGlassRTImporter.cs` re-pins the importer on every dylib reimport, so its StandaloneOSX pin moves to `AnyCPU` too; otherwise the next reimport would put ARM64 back. Read back by the isolation run: `StandaloneOSX True (CPU AnyCPU) · editor True (OS OSX, CPU ARM64)` | **PASS** |
| 4 | Universal player, native and under `arch -x86_64` | Player 754 MB, `Contents/MacOS/FRONTROOMSSS` and `UnityPlayer.dylib` x86_64 + arm64; `Contents/PlugIns/libFrontRoomsMetalGlassRT.dylib` = `c138bf1d…`, untouched by Unity; `codesign --verify --deep --strict` OK. **Native** (`ps6`, 68 s): process Arm64, `proc_translated 0`, dyld loads the arm64 slice (UUID B2DC743E), caps `0x30ff`, `on: Apple9, High`. **Rosetta** (`ps7`, 116 s): process X64, `proc_translated 1`, dyld loads the x86_64 slice (UUID 2417B4FB), caps `0x30ff`, `on: Apple9, High` (kernel 579 ms). **Both runs:** 38/38 GD3 slabs on bit 30; 404,179 glass px traced; 600-frame walk 0 blank, 0 CB errors; break 436,575 → 0 px on the break frame; 0 errors or exceptions in `Player.log`; exit 0; no crash report | **PASS** (RT on in both) |
| 5 | iOS and WebGL still strip G14 | Isolation run `iso3`. Player scripts for WebGL / iOS / Windows64 hold 0 `FRGlassRT_`, 0 plugin name, 0 `FrontRoomsGlassRTSystem`, 0 `FrontRoomsGlassRTCamera`; macOS 21 / 1 / 12 / 3. Importer: WebGL, iOS, Win64 and Linux off. Strippers: WebGL removes the prepass shader and the glass's `FRGlassRTPrepass` pass and keeps ForwardLit; iOS also removes the `_FR_GLASS_RT` variants; macOS keeps them. Same as step 1 (`iso2`), except the importer line | **PASS** |
| 6 | This section | Appended 2026-10-22 | done |

Compile: Roslyn on main as of 08:09 (HEAD `0ea38bb7` + working tree), plus the package's 6 `.cs` files and the RD2 importer: **COMPILE OK** for macOS and for iOS defines, 7 assemblies, 0 errors (`W/rt_work/rd2/roslyn_{mac,ios}`). In the clone, Unity compiled the changes with 0 `error CS`. Every timing in this section is **UNDER LOAD** (1-minute load 220–742, `W/logs/rd2/load.tsv`).

### 10.2 The changed files (clone `W/proj_rt_eng3`)

| Path | Main now (base) | After glass_rt_g14 | RD2 |
|---|---|---|---|
| `NativePlugin/build_frontrooms_metal_glass_rt.sh` | `35fb10d7` | unchanged | `664e26d7`: universal slices + `lipo`, x86_64 validator under Rosetta (skipped, with a note, when Rosetta is absent; it never prompts for an install), x86_64 slice signed ad hoc, per-slice sha1 printout |
| `Assets/Plugins/macOS/libFrontRoomsMetalGlassRT.dylib` | `56a35164` (arm64, 201,280 B) | `518d6769` (arm64, 236,624 B) | **`c138bf1d`** (x86_64 + arm64, 466,000 B; slices arm64 `518d6769`, x86_64 `b06642d3`) |
| `Assets/Plugins/macOS/libFrontRoomsMetalGlassRT.dylib.meta` | `e5fd5626` | kept | `0377540d`: `OSXUniversal CPU: AnyCPU`, GUID kept |
| `Assets/Editor/RT/FrontRoomsGlassRTImporter.cs` | `511c4186` | kept | `7561f80e`: StandaloneOSX pin `ARM64 → AnyCPU`, plus the comment |

The native sources (`FrontRoomsMetalGlassRT.mm`, `FRGlassRTScene.*`, `FRGlassRTShared.h`, `FRGlassRTLayout.h`, `.metal`, the validator) are the package's, unchanged. The clone-only `G14VPlayerSmoke.cs` also logs the process architecture and `sysctl.proc_translated`, and reads the plugin caps without throwing. ASCII only; it is not part of the payload.

The x86_64 slice adds 229 KB to the app.

### 10.3 arm64 parity in detail (editor, clone)

Three runs are compared: the universal dylib (U = `e1`), a control with the arm64-only dylib put back for one run (A = `c1`), and step 1 (`r3a`). The table counts pixels that differ by ≥ 2/255, summed over the frames of each class (1920×1080).

| Frames | U vs step 1 | A (control) vs step 1 | U vs A |
|---|---:|---:|---:|
| Final frames, RT Off (9; the plugin draws nothing) | 7,093,462 | 6,807,723 | 6,642,829 |
| Final frames, RT High / Ultra (18) | 14,286,020 | 13,763,082 | 13,166,819 |
| Traced radiance `*_rt_*` (18) | 527,881 | 475,201 | 241,271 |
| Raster mirror reference (12; no RT) | 4,429,681 | 4,243,113 | 4,063,116 |

- Every class, including frames G14 does not draw, differs between any two runs by the same order. Universal vs control is the smallest of the three.
- glass px, both IoUs and miss % match exactly in all three runs. 85 of 170 metrics match exactly between U and A; the rest are lamp-dependent photometry and GPU timings.
- **G10 hook proof** (PASS/FAIL counts):

  | Live RT | Step 1 | Control A | Universal U |
  |---|---|---|---|
  | Off | `g5` 128 / 47 | — | `e2` 124 / 51 |
  | High | `g3` 86 / 45 | `c3` 86 / 45 | `e3` 84 / 47, `e3b` 85 / 46 |

  - The flipped checks are different in every run:
    - `e2` (RT off, so the plugin traces nothing): 4 checks at `window_L0_0.7m`, 1,496 px outside the pane, max\|Δ\| 0.0035.
    - `e3`: `window_L0_0.7m` (2,244 of 2,638 px outside the pane, max 0.0024) and `Office_oblique50` P3 (84 px, max 0.008).
    - `e3b`: `38_deadlamp` P3 (204 px, all outside the pane, max 0.0004).
  - Neither the RT output nor the hook writes outside the pane. This is the proof harness's scene noise, recorded as H5.
- **Player:** the RT contribution (High − Off) in the universal app vs the arm64-only player of step 1 differs by ≥ 2/255 in 0.095 % of the RT pixels (max 2). Mean RT gain −1.32 vs −1.32 /255.

### 10.4 Players: native vs Rosetta (ms, p10 / median, UNDER LOAD)

| | arm64-only player, step 1 (`ps3`) | Universal, native (`ps6`) | Universal, Rosetta 2 (`ps7`) |
|---|---|---|---|
| Load (1 min) | ≈ 290 | 350–430 | 430–710 |
| RT stages v2 1080p High / Ultra | 2.97 / 3.32 · 4.39 / 4.86 | 3.04 / 3.74 · 4.58 / 4.80 | 3.21 / 3.61 · 4.38 / 4.61 |
| RT stages v2 1440p High / Ultra | 5.43 / 5.98 · 7.41 / 8.21 | 5.35 / 5.48 · 7.34 / 7.50 | 5.42 / 5.63 · 7.33 / 7.57 |
| RT stages v5 1440p High / Ultra | 8.18 / 9.99 · 11.77 / 13.75 | 7.62 / 8.19 · 10.87 / 12.17 | 17.21 / 20.63 · 27.14 / 30.49 (Off 21.6: a load spike, see below) |
| Walk main thread p50 / p99 / max | 0.45 / 1.53 / 8.2 | 0.53 / 1.84 / 6.2 | 0.75 / 4.37 / 64.2 |
| Break frame: traced px before → on the break frame; encode | 436,575 → 0; 0.55 | 436,575 → 0; 1.96 | 436,575 → 0; **102.4** (one frame) |
| Errors / exceptions / CB errors | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 / 0 |

- The GPU work is the same on both CPUs: the same kernels on the same GPU.
- **v5 1440p under Rosetta:** the whole frame with RT Off also jumped from ~8 to 21.6 ms in that round. That is the machine, not RT; the 1-minute load reached 710.
- **CPU side under Rosetta:** about 1.4× the native main-thread time, plus first-use translation spikes (64 ms in the walk, 102 ms encode on the break frame).
- **RT contribution, x86_64 vs arm64 in the same app:** 0.56 % of the RT pixels differ by ≥ 2/255 (max 3). The RT Off frames differ by as much (26,172 px ≥ 2/255): x86_64 and arm64 CPU float paths diverge slightly across the whole scene, not in RT.
- Frames and logs: `W/rt_work/rd2/{ps6_out,ps7_out}/` hold `player.log` (with the dyld image list), `player_smoke.txt`, `stdout.txt`, and `player_v2_{off,high,ultra,change_mask}.png`. Sheet: `30v_RD2_universal.jpg`.

### 10.5 Open items from RD2

| ID | Severity | What | Owner |
|---|---|---|---|
| RD2-1 | note | The Intel branch ("no Apple9" → Off, one status line, no per-frame native calls) is not executed on real hardware: there is no Intel Mac here, and Rosetta exposes the M3's Apple9 GPU. The branch is the one M1/M2 take and is unchanged. A standalone x86_64 `dlopen` test resolves 23 of 23 exports with no device (caps 0, inert). The old arm64-only file fails under x86_64 with "incompatible architecture" | anyone with an Intel Mac (one launch: `Player.log` should say `off: not an Apple9 GPU`) |
| RD2-2 | minor | Under Rosetta, RT stays on. The CPU side is ~1.4× native, with first-use translation spikes up to 102 ms. This only happens if a player forces "Open using Rosetta". Recommendation: leave it as is | 游戏视觉 (if Red wants Rosetta capped at Off/High: one check of `sysctl.proc_translated` in `DefaultQuality()`) |
| RD2-3 | process | RD2 is staged, not packaged: 4 files plus bases in `W/rt_work/rd2/payload/` and `payload_bases.txt`. It must land **together** and **after** `glass_rt_g14` (dry run on main 08:08: all 16 bases still match, GD3 shader NOTE only). The importer change alone, with an arm64-only dylib, would ship a dylib that cannot load under x86_64 (clean Off, but pointless) | 游戏视觉 (fold into `glass_rt_g14` or a stacked package; Red approves the landing) |
| RD2-4 | major for the Alpha / any Mac build | Every universal Mac build made before RD2 lands has no RT at all (snapshot 20261021-2221). After landing, the next snapshot should show `lipo -info Contents/PlugIns/libFrontRoomsMetalGlassRT.dylib` = `x86_64 arm64` | App Store · 构建 (check after landing) |
| H5 | minor (tool) | The G10 proof flips 1–4 checks from run to run at max\|Δ\| ≤ 0.008, mostly outside the pane, also with the live RT off. This is scene noise from real-time lamp flicker | 游戏视觉 (W/tools) |

COV-1 / RD2 in §8 is closed by this payload once it lands.

### 10.6 Files

- **Payload:** `W/rt_work/rd2/payload/` (the 4 files), `payload_bases.txt`, and `rd2.diff` (text files).
- **Clone:** `W/proj_rt_eng3` = main 06:55 + `glass_rt_g14` + RD2 + clone-only harnesses. The pre-RD2 copies are in `W/backup/rd2_clone_1022/`.
- **Logs:** `W/logs/rd2/`:
  - `plugin_build_universal.log`;
  - `e1`, `e2`, `e3`, `e3b`, `c1`, `c3`, `iso3`, `pb3`;
  - `chain_rd2.zsh`, `control_rd2.zsh`, `chain.out`, `control.out`, `load.tsv`;
  - `dry_main_g14_0808.log`.
- **Outputs:** `W/rt_work/rd2/`:
  - harness and player outputs `e1_out`, `e2_out`, `e3_out`, `e3b_out`, `c1_out`, `c3_out`, `iso3_out`, `ps6_out`, `ps7_out`;
  - the universal player `player_universal/FrontRooms3D_rd2.app`;
  - the standalone loader test `loadtest/`, the reproducibility build `repro/` and the thin slices `dylib/`;
  - comparison tables `cmp_*.tsv`, `metcmp_*.tsv` and `rtdelta.txt`;
  - tools `tools/{pixcmp,metcmp,rtdelta,compose_rd2}.py`.
- **VL:** VL314's spec in `W/vl_pending/glass_rt_g14.md` now also covers the universal player (image `30v_RD2_universal.jpg`). No new VL number was used.
