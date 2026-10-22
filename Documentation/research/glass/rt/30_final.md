# 30 · G14 ray-traced glass: final report (package `glass_rt_g14`, independent verification on main, 2026-10-22)

2026-10-22 · 游戏视觉 · workflow stage "G14 independent verification" (Red allowed workflows on 2026-10-21; the first run was cut by a network outage on 10-21 at 23:44 and resumed here) · **verdict: the package is correct, safe and inert off the Mac on today's main, with GD3's glass live. It still fails its cost bars (main thread, GPU at 1440p and at the break-shot pose), it does not register GD3's cracked stages, and it does not make the universal Mac app trace (RD2). It is not landed: this stage only verified it.**

**Update after fix round 2 (2026-10-22 11:xx, §12): READY to land. Review round 2 found two new art regressions from fix round 1 (holes and missing lamps in the reflection after a stream-in jump; aliased lens edges in motion) and nine engineering items. All are fixed or answered in §12, the package now holds the round-2 plugin `ec31f4fa` (universal), and Red's three open decisions are written up in `40_for_red.md`. Where §0–§11 and §12 differ, §12 is current.**

**Update after fix round 1 (2026-10-22 10:xx, §11): READY to land. The review's correctness defects are fixed and re-checked in the clone (AD-1 blocks, G14V-1, G14V-3, G14V-4, non-tracing Macs, fail-closed at every entry point, the strippers' build step, TLAS reuse). RD2 is folded into the package (20 paths), and its dry run on main is OK. The universal app traces natively, under Rosetta 2 and as a sandboxed store archive. Cost is still over the bars (Red's D1/D6). Where §0–§10 and §11 differ, §11 is current.**

`W` = `/Users/redwang/FrontRoomsVisualWork`. Design: `10_rt_glass_design.md`. Implementation: `20_implementation.md` (§11 lists the 2026-10-14 fix pass). Earlier verification: `25_verify_engineering.md` (E1–E14), `25_verify_art-direction.md` (AD-1–AD-8). Codex audit: `../../codex_audit/10_review_rt.md` (F1–F8), `../../codex_audit/00_main_state.md` (RD2 = COV-1). Stopgap note: `NOTE_1014_v2_stopgap_in_main.md`. The package and `20_implementation.md` point at this file ("30_final.md §6 / §7"); the fix pass that built the package was cut off before it wrote it.

Nothing in Red's project was written except this file and the 13 `rt/images/30v_*.jpg` sheets (claimed in `LANDINGS.md`). No Unity, batch, build or render ran on main. No git command was run. Nothing was downloaded.

---

## 0. Short answer (for Red)

*This is the verification of 07:31. Fix round 1 changed items 4–7 (§11.0) and fix round 2 changed them again (§12.0, the current answer). Item 7 and §1 are rewritten to the current package.*

1. **The package still applies cleanly to today's main.** Dry run on main at 06:53 and again at 07:31 (§7): all 16 bases, 3 deletions and 8 kept `.meta` files match. The only notice is a contract file: GD3 replaced `FrontRoomsGlass.shader`. Its six G14 hook blocks are unchanged, and the hook proof shows they work on GD3's shader (§4).
2. **It works with your real glass.** Since GD3 landed, every map window is GD3's slab on `Glass_Fracture`, a copy of `Glass_Window` with `_RTReceive = 1`. The old hand-off C8 is done by GD3. In the game flow and in the player, 23 of 23, 38 of 38 and 68 of 68 slabs took the RT prepass. In engine: the traced glass sits on the drawn glass (IoU 0.996–0.999). There are 0 % misses. Occluders, a moving Relay, a broken pane and a destroyed mesh are right on the same frame. RT traced at weight 0 changes 0 px. Lamps in the reflection follow on the same frame (r = 1.000).
3. **The red wallpaper is gone.** The V2 stopgap is deleted and the real paper × print shading replaces it. In the warmed harness views the red tint is ≤ 0.008 % while RT changes 245k–901k px. In the 84-frame capture set it is ≤ 0.06 %. Main without the stopgap reached 9.99 %.
4. **It is safe.** An exception inside the RT frame turns RT off cleanly: 1 console entry, frames keep rendering, 0 ZBinningJob errors. WebGL, iOS and Windows player scripts contain no RT code, and WebGL and iOS strip the RT shader passes. The native scene empties at every Play exit. In a 120 s play run there were 0 pink renderers and only the 2 expected exceptions. The arm64 player ran clean under Metal API validation (1,449 frames: walk, streaming and a pane break). Under GPU shader validation it also ran clean over the same frames (§3.1 A1).
5. **It is still too expensive.** G14's own main-thread time is 0.45–0.53 ms per frame when standing or walking, up to 4.8 ms on chunk-streaming frames, and 0.17 ms when no window is in view. The design bar is 0.3 ms. GPU cost in the arm64 player at 1440p: High 5.4–6.0 ms, Ultra 7.4–8.2 ms at 1.5 m, and High 6.8–10 ms, Ultra 9.8–13.8 ms with the pane filling the frame. The bars are 2.0 / 3.5 and 5 / 7 ms. All of this was measured under load, so it is an upper bound (§5).
6. **New open items.**
   - **G14V-1:** GD3's S1 and S2 meshes are made during the hold and never registered with G14. On the S2 beat the cracked pane drops from the traced reflection to the zone cube. There are 0 stale entries at any stage.
   - **G14V-4:** the package resets the RT quality at every Play start. This breaks the G10 proof's "force Off", but not the game.
   - **H1–H3:** two verify harnesses predate GD3 or the batch order and need small updates. These are harness issues, not game defects (§2, §4, §8).
7. **RD2 is in the package (since fix round 1).** The package ships ONE universal plugin (arm64 + x86_64; fix round 2 build `ec31f4fa…`, §12.3) with its `.meta` set to Standalone macOS `AnyCPU` (GUID kept), so the universal Mac app traces on Apple silicon, natively and under Rosetta 2, and Intel Macs fall back cleanly. *(This item read "RD2 is not in this package" at 07:31, when the dylib was arm64-only `518d6769…`; the rest of this paragraph is that morning's probe.)* **A probe shows option (b) needs no downloads:** the package's sources build an x86_64 slice with the installed Xcode, with no warnings, and `lipo` makes a universal dylib (§1.1). It cannot be run here: there is no Intel Mac and no Rosetta. **(Corrected 2026-10-22 in §10: today's universal app has no RT on any Mac, because Unity leaves the ARM64-pinned plugin out of it; Rosetta 2 is installed here; RD2 is now built and run in the clone, natively and under Rosetta.)**
8. **Your decisions, now with numbers (§5.2, §5.3):**
   - **D1, default quality.** Today a standalone player defaults to Ultra. My recommendation is High at every level. Ultra costs 1.9–3.6 ms more at 1440p for no measurable gain in edge crawl or lens shape.
   - **D6, break-shot budget.** At the break pose, RT at 1440p costs 6.8–10 ms on High. A 4-lamp shot budget cuts the trace by 56 %.
   - **D2, player body in dark windows.** A dark window becomes a mirror with nobody in it.

---

## 1. What the package changes (`W/apply/glass_rt_g14.sh`, apply convention v2)

**Current package (fix round 2, §12.3):** 20 paths written, with bases re-recorded on today's main; the universal plugin `ec31f4fa…` and its `.meta` (GUID `9b3bca94…` kept, Standalone macOS `AnyCPU`, editor ARM64) are among them; 9 GUID `.meta` files and 4 contract files are kept; the 3 deletions are **optional** (`--with-deletions`, Red's OK). The table below is the 07:31 verification of the 16-path version (bases of 2026-10-14 18:19, main `0a9ee7f`, payload from `W/proj_rt`, fix pass 30); fix rounds 1 and 2 added the build script, the importer pin, `FrontRoomsGlassRT.cs` and the dylib `.meta` (§11.3, §12.3). Line counts are from that day's `glass_rt_g14.diff`.

| Path | Change | What it does |
|---|---|---|
| `NativePlugin/FrontRoomsMetalGlassRT.mm` (+174 −29), `FRGlassRTScene.mm` (+78 −25), `FRGlassRTScene.h` (+27 −3) | M | Shared 4 MB geometry pages with a pre-allocated spare; lock-free stats; per-camera TLAS ring 3 → 8; reference-counted geometry; BLAS budget split (fracture 24 builds / 50k triangles, streamed map meshes 8 / 10k per frame, E8); atomic `g_SceneReady` (E12); the three-pass resolve (AD-1); refits wait for their half (E12) |
| `NativePlugin/FRGlassRTShared.h` (+23 −8), `FRGlassRTLayout.h` (+6 −6) | M | Material record 256 B with the print layer (`FR_MAT_PRINT`), caster mask 0x20, FRFrame 512 B; layout self-test updated |
| `NativePlugin/FrontRoomsGlassRT.metal` (+380 −123) | M | Paper × print wallpaper (codex F8 / V2); anisotropic ray-cone sampling; per-hit ranked shadow lamps with a 0.2 m disc (AD-2); no blur on clean glass (AD-3); separable Gaussian smudge blur inside the dispatch rect (AD-1, E5); unlit CRT; EXIT-sign lamp field |
| `NativePlugin/tools/frglassrt_validate.mm` (+1 −1) | M | Build-time validator for the new pipelines |
| `Assets/Plugins/macOS/libFrontRoomsMetalGlassRT.dylib` (+ `.meta`) | M (binary) + M | **Now (fix round 2): `ec31f4faf4978bb6c4faef9db8f8e6d62569bbba`, 466,720 B, universal x86_64 + arm64; `.meta` written with GUID `9b3bca94…` kept, Standalone macOS `AnyCPU`, editor ARM64 (§12.3).** Main today: `56a351646e940ae10154c1f89dae46a5fb76c0f3`, 201,280 B, arm64 only. *(07:31: `518d6769…`, 236,624 B, arm64, `.meta` kept with Standalone OSX ARM64.)* |
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
| **Package payload now (fix round 2)** | **`ec31f4faf4978bb6c4faef9db8f8e6d62569bbba`** (slices arm64 `51788054…`, x86_64 `a27271df…`; UUIDs arm64 `979D92B5`, x86_64 `170A9EBA`) | 466,720 | **x86_64 + arm64**, `.meta` Standalone macOS `AnyCPU` |
| Fix round 1 payload (superseded) | `b1c6de26e42821f27f0ce19085049a829f9fdf1a` | 466,416 | x86_64 + arm64 |
| 07:31 payload (superseded) | `518d6769bd67d847c5544c91a51f5f71330c6ae8` | 236,624 | arm64 |
| Rebuilt at 07:31 from that payload's `NativePlugin/` (scratch `W/rt_work/g14v/pb2_1022`) | `518d6769…`, byte-identical (`cmp`) | 236,624 | arm64, `@rpath` |
| Inside the arm64 player built at 07:31 (`Contents/PlugIns/ARM64/`) | `518d6769…` | 236,624 | arm64 (the universal players of §11 and §12 carry the universal file) |
| Main today (before the package) | `56a351646e940ae10154c1f89dae46a5fb76c0f3` | 201,280 | arm64 |
| RD2 probe: x86_64 slice, same sources and flags with `-arch x86_64` (scratch only, `W/rt_work/g14v/rd2_probe_1022`) | `319330e7c46eff4ae9734e10dfd9890529be847f` | 213,104 | x86_64, 47 exported symbols (arm64: 47) |
| RD2 probe: `lipo -create` arm64 + x86_64 | `abe0fbf0cbf0abed98aa1f86baed076d280df4dc` | 466,000 | x86_64 + arm64 |

The validator compiled the embedded MSL with the OS compiler in 338 ms. All 9 pipelines built: trace, trace_aa, resolve, parity, gather ×2, layout, blur_h, blur_v. The GPU layout self-test passed: FRInstance 128, FRMaterial 256, FRLamp 64, FRMeshGPU 48, FRFrame 512, FRVertexAttr 24. Toolchain: Apple clang 21.0.0, macOS SDK 27.0, macOS 27.0.1. No Metal toolchain was needed and nothing was downloaded.

The RD2 probe only shows that option (b) builds locally. The RD2 job (VL307) still has to do three things. First, build both slices in `build_frontrooms_metal_glass_rt.sh`. Second, give the importer `AnyCPU` or x64 for Standalone OSX. Third, confirm that an Intel Mac reports "no Apple9" and stays Off. **(Corrected in §10 and §11: Rosetta 2 is installed on this Mac, so the x86_64 slice and the x86_64 validator do run here; the RD2 build script runs the validator for both slices. All three steps are done and folded into the package, §11.3.)**

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
| **L0 wallpaper red tint** without the stopgap | **Warmed harness views** (r3a, RT High vs Off, R − G rise > 10): v1–v5 0.000 / 0.006 / 0.000 / 0.000 / 0.000 %, o45 0.007 %, o60 0.000 %, dark-beyond 0.008 %, while RT changes 245k–901k px per view. **CxVis set** (84 frames): max 0.06 %, 0 frames over 0.1 %. Main without the stopgap: 3.56 / 9.99 %; with it: 0.02 / 0.01 %. **Corrected in fix round 1 (art review #7, checked against `v2h_out/dump.txt`):** in the CxVis RT High vs Off set, **10 of its 24 glass frames are pixel-identical** (untraced), not 6 of 20. The extra four are the whole break-shot sequence (`79_glass_before_hud`, `80`–`82_glass_hold`, the pane filling the frame at 0.95 m). At the end of the run G14 reports 247,845 of 643,598 glass pixels missing (38.5 %), 1 lamp loaded and 60 meshes pending. Cause: G14 registers and builds only inside a camera render, and CxVis renders twice per capture, so G14 is starved there. This is a harness artefact, not a game defect (every-frame renders trace 437k px with 0 misses; the 600-frame walk has 0 blank frames). The "0 of 20 on 10-21" comparison is void: those runs differ everywhere from lamp flicker (frames 71–78, with no window, differ in 19–33 % of their pixels). So CxVis is no evidence for the tint or the game flow until it renders every frame (H6, §11.6), and there is still no frame of the real break shot traced in a real render loop; the closest are the harness break poses at S0/S1/S2 (`30v_F31_GD3_stages.jpg`). The verdict rests on the warmed harness views, plus the art review's six new poses (≤ 0.017 %) | PASS (warmed views); CxVis evidence withdrawn |
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
| F3 / A7 | major | Main thread 0.45–0.53 ms standing or walking; 1.5 ms p95 and 4.8 ms max while streaming; 12.8 ms once at map start. Bar: 0.3 ms p99. **Fix round 1 (§11):** gather sliced, budget 0.2 ms, O(n²) mesh release gone: CPU time quiet p50 0.42 / p99 0.81, streaming max 1.67 ms; still FAIL, the rest is the per-frame lamp and lens reads | map events (P2 lamp events, 关卡设计 contract) |
| B8 / D6 | major | GPU over the design bars at 1440p and at the break-shot pose (§5) | glass-rt-track P2 (per-hit lamp cull / light grid), then Red |
| G14V-1 | ~~major (look)~~ **FIXED in fix round 1** | GD3 S1/S2 meshes are never registered; S2 pops from traced to zone cube; live and restored windows disagree (§6). Now registered on GD3's `MeshesReady`: S2 beat 437,296 px traced, dark window keeps its mirror (§11.1) | — |
| RD2 / COV-1 | ~~major for players~~ **CLOSED** | The dylib was arm64 only, so the universal app had no RT at all (§10). RD2 is now part of `glass_rt_g14` (universal dylib, importer `AnyCPU`); the universal player and the store archive trace (§11.3) | lands with the package |
| G14V-4 | ~~minor~~ **FIXED in fix round 1** (an API-set quality survives the reset; G10 proof P1 11/11 without a flag) | `ResetStatics` now clears the quality override at every Play start (F1 follow-up). An edit-mode `FrontRoomsGlassRT.Quality = Off` (the G10 proof's force-off) no longer holds. The game is unaffected: settings and `-frGlassRT` still work. Fix: re-apply a requested override after the reset, or make the proof set it on the first Play frame | 游戏视觉 (next package revision or the G10 tool) |
| H1–H3 | ~~minor (harness)~~ **CLOSED** | The package's `FrontRoomsGlassRTVerify.cs` predated GD3 (H1/H2) and registered before the first URP render in batch (H3). The rebuilt package's harness carries the H1/H2 patch, and G14V-3's fix removes H3's cause | — |
| H4 | minor (tool) | `FrontRoomsGlassG10Capture` predates GD3: its "every pane hidden" and pass-2 baselines ignore GD3's slabs | 游戏视觉 (W/tools) |
| G14V-3 | ~~minor~~ **FIXED in fix round 1** (bit re-picked when the pipeline changes) | `PrepassBitFor` is decided once per registration. A receiver registered before URP exists stays on the override prepass, which draws almost nothing of a GD3 slab. Only a harness hits it today | glass-rt-track |
| B1 rubber | flag (new) | Prop_Rubber reflects at 0.44 of its raster luminance (19 px in L0, 1,302 px in the Office) | glass-rt-track |
| A13 | flag | Fracture prepare takes 6 frames (bar 4) | P4 |
| B1/B2/B5/B6 | flag | Wallpaper p95 12.6 %; mirror blocks 92.7–95.5 %; back-image weight not measurable; AA bar not met (no visible crawl) | glass-rt-track |
| AD-3 head-on | flag | Reflected troffers head-on are +22.0 / +24.8 % footprint and −9.0 / −9.4 % core vs a mirror. At 45°: +5.7 % / −3.5 %; at 60°: +7.9 % / −5.9 % (bar +10 % / 5 %). **Fix round 1:** v1 +11.9 / −2.1, v2 +11.1 / −4.5, o45 +2.0 / −2.2, o60 +2.7 / −3.6 % | glass-rt-track |
| AD-1 blocks | ~~major (look)~~ **FIXED in fix round 1** | Art review 1: the smudge blur cut carpet-coloured blocks out of dark reflected objects (VL282 was FAIL, not PARTIAL). Now 0 holes in the cube and the Relay boot, 0 specks (§11.1) | — |
| AD-2 | look | L0 carpet p95 8.4 % (bar 8); Office L0 carpet 8.7 / 27.0 % on main of 09:17 (bar 3 / 8); a noisy shadow-ray cone at the Office wall corner | glass-rt-track (per-hit ranked soft shadows) |
| AD-4 | look | The lit grime band across dark-beyond windows is unchanged and crosses the Relay's boots (listed again after art review 1) | glass track (`FrontRooms/Glass`) |
| H6 | minor (tool) | CxVis renders twice per capture without a G14 warm-up: 10 of 24 glass frames untraced, end-of-run 38.5 % misses (§4). Not evidence for RT until it renders every frame | 游戏视觉 (W/tools) |
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

---

## 11. Fix round 1 (review round 1: engineering and art NOT READY)

2026-10-22 08:41–10:1x · 游戏视觉 · workflow stage "G14 fix round 1" (cut by the account switch at 08:50 and relaunched; §11.7) · **verdict: READY to land as one package. Every correctness defect the two reviewers verified is fixed in the clone and re-checked. RD2 is folded into `glass_rt_g14` (20 paths), and the dry run on main is OK. Cost is still over the design bars, and that is Red's call (D1/D6), not a blocker. Three look items stay open and are listed with owners (AD-2 carpet parity, the AD-3 head-on footprint 1–2 points over its bar, AD-4 on the glass track).**

Clone `W/proj_rt_eng3`. It was re-synced from main at 09:17 (HEAD `d4d0e7a` + working tree) exactly as `mkclone.sh` does, then `glass_rt_g14` + RD2 + this round were applied. At 09:4x a checksum `rsync -n -c` of Assets, Packages, ProjectSettings, NativePlugin and Tools against main listed only the package's own paths, plus one file under `Tools/print/unity_staging`, outside `Assets`. So the clone is current again (engineering #11). Every timing in this section is **UNDER LOAD**: 1-minute load 44–250 (`W/logs/fix31/load.tsv`), with two other visual-line Unity jobs running most of the time.

Nothing in Red's project was written except this section, the corrections marked in §1.1, §4 and §8, and the `rt/images/30v_F31_*.jpg` sheets (claimed in `LANDINGS.md` at 09:25). No Unity, batch, build or render ran on main. No git write command was run. Nothing was downloaded.

### 11.0 Short answer (for Red)

1. **The correctness defects are fixed** (clone, re-checked in fresh runs `r6a`, `r6b`, `s9`, `iso5`, `ps10`, `ps11`, `n1`, `n2`, `g7`):
   - **AD-1:** the smudge blur no longer cuts carpet-coloured blocks out of dark reflected objects. Carpet holes inside the test cube went from 78 to **0**, in the smudged Relay boot from 24 to **0**, and isolated specks in the dark-window Relay from 19 to **0** (`30v_F31_AD1_blocks.jpg`).
   - **G14V-1:** GD3's S1 fins and S2 pieces are now registered when GD3 builds them (`MeshesReady`), so the pane stays traced on the S1 and S2 beat frames: 437,356 → 437,296 px instead of dropping to the zone cube. A dark window keeps its mirror through S0, S1 and S2. There are 0 stale entries (`30v_F31_GD3_stages.jpg`).
   - **G14V-3:** a pane registered before URP's first render no longer stays untraced; its prepass bit is re-picked (S0 at first sight: 2,089 → 437,356 px, no harness repair).
   - **Macs that cannot trace** (Intel, M1/M2, macOS < 14, a build without the plugin) now get quality Off: no RT pass, **0 MB** of targets (was 87 MB at 1080p Ultra), no registrations, and no per-frame native call.
   - **Fail-closed** now covers every entry point: the BuildFrame entry, mid-frame, the render-graph record, the map's `GlassBroken`, `Register` and GD3's `MeshesReady` each fail closed once, with one console entry, and no exception reaches the caller (6 of 6).
   - **G14V-4:** a quality set through the API (the G10 proof's force-off) now survives the reset at Play start.
   - **The strippers:** the build step now calls the same decision the isolation check tests, and that step itself is now tested on stand-in variant lists.
2. **RD2 is in the package.** `glass_rt_g14` now writes 20 paths: the 16 from 10-14, plus the universal build script, the importer pin, the dylib `.meta` (GUID kept, Standalone macOS `AnyCPU`) and `FrontRoomsGlassRT.cs`. Dry run on main at 10:07 (HEAD `18acaa16` + working tree; the 20 bases recorded at 09:52 still match): DRY RUN OK. Scratch tests: MANIFEST OK, then ALREADY APPLIED; a half-applied target, a moved base and a changed `.meta` are refused. `rebase_apply.sh --no-install --ios`: PASS. Roslyn `--all` on main (10:08) + payload: COMPILE OK for macOS and iOS. **Apply command:** `bash /Users/redwang/FrontRoomsVisualWork/apply/glass_rt_g14.sh --apply`, then restart Unity.
3. **The universal app traces on both CPUs and on the store path.**
   - Universal player (`BuildPipeline.BuildPlayer`, URP intact) with dylib `b1c6de26`. Native: arm64 slice `FC491058`. Under Rosetta 2 the process ran translated (`proc_translated 1`) and the plugin loaded (caps `0x30ff`); a translated process can only map the x86_64 slice, whose UUID is `94FAD184` (`dwarfdump`). *(Corrected in round 2, engineering #8: the dyld print in `ps11/stdout` has no line for the plugin, so the load is implied by these two facts, not seen in a dyld line.)* Both are `on: Apple9, High`, with 38 of 38 GD3 slabs on bit 30, 404,179 px traced, a 600-frame walk with 0 blank frames, the break going 436,575 → 0 px on the break frame, and 0 errors.
   - **Store path (engineering #5).** In the clone's Xcode export (`FrontRoomsCloudBuild.BuildMacTestFlight`), the plugin sits in the `CopyPlugIns` phase with `CodeSignOnCopy`. `xcodebuild archive` was run with the TestFlight script's entitlements (App Sandbox, `allow-jit`, Hardened Runtime), without `-allowProvisioningUpdates`, export or upload. Xcode re-signed the plugin with the app's Apple Development certificate, team `T9WR6QR3UY`, with the runtime flag on. `codesign --verify --deep --strict`: valid. The archived, sandboxed app was launched twice. The plain launch gave READY and `[FrontRoomsGlassRT] plugin loaded · caps 0x307f`, so library validation accepts the dylib. The launch with the player smoke inside the sandbox gave **`on: Apple9, High`**, 38 of 38 slabs on bit 30, 404,179 px traced, a walk with 0 blank frames, the break going 436,575 → 0 px, and 0 errors. The distribution export re-signs with the same team, and it was not run.
4. **Cost is still over the bars.** The numbers are in §11.2 and §11.4.
   - **Main thread** (now in the main thread's CPU time, which the machine's load does not inflate):
     - quiet frames p50 0.42 / p99 0.81 ms;
     - streaming frames p95 0.75 / max 1.67 ms (was 4.8 ms).
     - The bar is 0.3 ms. What is left (0.23 ms) is the per-frame lamp and lens reads, which need the map's P2 lamp events (a contract, §11.6).
   - **GPU:** the TLAS is now reused when nothing moved (17 of 22 timed frames at the test window). The acceleration-structure span did not change (1.22 vs 1.21 ms): it is not the TLAS build. The trace is still the per-hit lamp loop.
   - **Which metric each bar uses:** B8 is read on the **added frame time** (whole-frame GPU median, RT − Off, interleaved). The stage spans overlap the opaque raster, so they are an upper bound (§11.4).
5. **Still open, with owners:**
   - **AD-2:** L0 carpet parity is unchanged at 3.3 % / p95 8.4 % (bar 3 / 8 %). The Level 0 carpet seen from the Office is now 8.7 % / 27.0 % on today's main (06:55 main: 6.8 / 19.1).
   - **AD-3:** head-on footprint is +11.1 to +11.9 % (bar +10 %); the core is within its bar.
   - **AD-4:** the lit grime band (glass track) is back on the list.
   - **B4:** the art sign-off is Red's.
   - **D1, D2, D6:** Red's decisions.

### 11.1 Every review item and what was done

| Review item | What was done (files) | Re-check | Verdict |
|---|---|---|---|
| Eng #1 / F3 / A7 main thread | `RegisterBudgetMs` 1.0 → 0.2 ms. The chunk gather is a sliced depth-first walk (≥ 64 nodes per slice, within the budget) instead of an unbudgeted `GetComponentsInChildren`; lights are collected by the same walk. Mesh release no longer scans every mesh (it was O(n²): 4.4 ms on the 4-chunk drop of walk frame 113). A main-thread CPU-time column (`CLOCK_THREAD_CPUTIME_ID`, a new plugin export) is added next to wall time. A lens-level skip was tried and **left off**: it skipped only 22 % of the reads and pulled B3 down to r 0.990 | `r6b` walk, CPU time: all p50 0.43, p95 0.63, p99 0.83, max 1.67 ms; quiet p50 0.42 / p95 0.53 / p99 0.81; streaming p95 0.75 / max 1.67 (was max 4.8). Lamps + lens reads p50 0.23 ms (~390 property-block reads per frame within 34 m) | **FAIL** (bar 0.3 ms). Spikes fixed; the per-frame polling needs the map's P2 lamp events (contract C3/C5/C7) |
| Eng #2 / B8 / D6 GPU | TLAS reuse (`FRGlassRTScene.mm`). When this frame's instance descriptors are byte-identical to the ones the last TLAS was built from, and no acceleration structure was retired since (a generation counter), the trace reads that TLAS with this frame's fresh records and lamps. The owner slot stays out of the ring until every frame reading it is done. An A/B switch `FRGlassRT_SetTlasReuse` and per-mode built/reused counts are in the cost harness. **Metric per bar: §11.4** | `r6a` cost: 17 of 22 timed High frames reused the TLAS, and the rebuild mode built 22 of 22. The AS span is the same either way (v2 1080p 1.22 vs 1.21 ms; v5 1440p 4.06 vs 4.20), and frames with 0 builds also span 0.93–1.3 ms (FRACTURE f+0..2), so the span is not the TLAS build. The A5 lifetime checks stay exact with reuse on: a moved Relay gives 0 px stale, a broken pane 0 px on the break frame | Defect fixed (no rebuild of an unchanged TLAS). Cost still **FAIL** vs B8 (§11.4) |
| Eng #3 / Art #4 / G14V-1 | G14 subscribes to GD3's `FrontRoomsGlassBreakable.MeshesReady` and `StageChanged` (GD3's files are not edited). It queues the slab, S1 and S2 renderers in the window's chunk record and registers them within the frame budget, or at once if a beat comes first. A GD3 working-set Mesh that was refilled is re-uploaded under the same key. The rescan keeps renderers that a sliced walk already passed | `s9`: S1 beat slab/S1/S2 in TLAS 1/1/0, 437,356 px; S2 beat 0/0/1, **437,296 px** (was 2,089); restored S2 437,287 (live = restored); dark window S0 / S1 / S2 437,346 / 437,346 / 437,259 px; stale 0 at every step. `PrepareMeshes` / `RegisterPiece` are still harness-only paths (A13); GD3 does not need them now | **FAIL→FIXED** |
| Eng #4 non-tracing Macs | `Quality` returns Off unless the plugin is loaded on Metal and the hardware qualifies (`CanTrace`); `RequestedQuality` keeps the asked-for value. `Caps` is read once per frame at most, and a hardware "never" is latched (no native call after that). No per-camera component on such a Mac. The renderer feature allocates targets only once `Supported`; until then a warm-up pass carries the plugin event. `PrepareMeshes` and every API call return at once | `n1` (simulated latched "cannot trace") and `n2` (no plugin): Quality Off (requested Ultra), camera RT component False, plugin events +0 over 120 frames, RT targets **0.0 MB**, registrations 0, `PrepareMeshes` / `Register` → −1 | **FAIL→FIXED** |
| Eng #5 store path | The clone's Xcode export (`FrontRoomsCloudBuild.BuildMacTestFlight`) then `Tools/build_mac_testflight.sh`'s steps without upload, without export and without `-allowProvisioningUpdates` (`W/rt_work/fix31/tools/store_archive_check.zsh`) | Export: the plugin is in `CopyPlugIns` with `CodeSignOnCopy`. Archive: SUCCEEDED (Apple Development certificate, team `T9WR6QR3UY`). The plugin is re-signed by the team with the hardened runtime, and the app is sandboxed with `allow-jit`, strict verify valid. Launch: READY and `plugin loaded · caps 0x307f`. The smoke inside the sandbox gives `on: Apple9, High`, 404,179 px, the walk 0 blank, the break 436,575 → 0, 0 errors (`W/rt_work/fix31/store_check/`). The archive lacks the five FMOD banks because the clone's FMOD bank refresh fails (the one build error of every clone build); that is the clone, not G14, and Red's script checks the banks on main's builds | **PASS** (archive and launch; no export, no upload) |
| Eng #6 RD2 staged | Folded into `glass_rt_g14` (§11.3) | Dry run OK on main; scratch apply tests; rebase PASS; Roslyn | **FIXED** |
| Eng #7 / G14V-3 | The prepass bit of every receiver is re-picked when `RenderPipelineManager.currentPipeline` changes (null → URP at the first render), and override-bit receivers are re-checked every 32 frames (async shader compile) | `s9` S0 at first sight: bit **30**, 437,356 px (s7: bit 29, 2,089 px), with no `-g14vRepair` | **FAIL→FIXED** |
| Eng #8 fail-closed coverage | `Register`, `Unregister`, `SetEnabled`, `PrepareMeshes`, `IsPrepared`, `RegisterPiece`, `MeshChanged`, `RefitMesh`, `GlassBroken` and GD3's two handlers each fail closed. The console entry now names the site. The render-graph imports and the frame bookkeeping moved inside the guard | `r6b`: BuildFrame entry 1 entry, 11 of 11 frames live; then, re-armed per site: mid-frame (after the scene update), record, `GlassBroken`, `Register`, `MeshesReady`: each failed closed once, 1 console entry, 0 exceptions reached the caller, 7 of 7 frames live → **5 of 5 PASS** (the first run's "lamps" site never fired, because the broken test pane left no receiver in view; the site moved to a point every traced frame reaches) | **FIXED** |
| Eng #9 load | Main thread now in CPU time (immune to preemption). The GPU cannot be isolated: the player A/B old vs new runs back to back (§11.4) | Every ms here is UNDER LOAD | open (machine) |
| Eng #10 strippers | `OnProcessShader` now calls one `Strip()` built on `WouldStrip` / `WouldStripKeyword`, the same decisions the isolation check prints. The isolation check runs that `Strip()` on stand-in variant lists | `iso5`: STRIP STEP WebGL prepass shader 4/4 removed, WebGL glass `FRGlassRTPrepass` 4/4, WebGL ForwardLit 0/4, iOS glass prepass 4/4, iOS ForwardLit 2/4 (the `_FR_GLASS_RT` ones), macOS 0/4, Win64 0/4. Player scripts: WebGL / iOS / Win64 0 `FRGlassRT_`, macOS 23. Importer: StandaloneOSX `AnyCPU`, editor ARM64, all others off | **FIXED** |
| Eng #11 clone behind main | Re-synced 09:17, checksum-compared 09:4x (only package paths differ) | All runs in this section | **FIXED** |
| Eng #12 docs | §1.1 corrected; §8 rows updated; §0 points here. The memory note on Rosetta is not stale (it says Rosetta was reinstalled 10-14) | — | **FIXED** |
| Eng #13 minor | Pages 3 → 5 over a soak (no range reuse in the allocator): still open (P2 compaction). Rosetta first-use stalls: as measured, leave (RD2-2). The Intel branch has not run on real hardware (RD2-1). The dylib needs macOS 13 while the project targets 12: on 12 the plugin fails to load and RT is cleanly Off (unchanged). The deletions need an explicit OK at landing: they are listed in the package and in §1 | — | noted |
| Art #1 / AD-1 blocks | The mirror ray follows the pane's mean normal over a 13 × 13 px window (25 taps, same pane): the smear's micro-normals no longer send single rays to the floor behind a dark object. The blur radius is averaged over the pane (25 taps, 4 px apart, Gaussian weights) in `fr_resolve`, and smoothed again (9 taps) in both blur passes, so a separable blur never sees a radius step. The radius fades in over 0.1 of smoothness; the per-pixel 1.5 px threshold and the 3 × 3 normal-spread term are gone | `r6a` block-aware metric (`ad1_metric.py`: carpet-coloured px inside the mirror reference's cube silhouette eroded 8 px; per boot, bright px inside the filled silhouette): cube holes 78 → **0** of 6,482, spill beyond R + 1 px 0; smudged boot 24 → **0** of 1,420 (Ultra 0); dark-relay specks 19 → **0** of 42,235 | **FAIL→FIXED** |
| Art #2 / AD-3 | The 3 × 3 edge filter now stays off lit lens borders (linear luminance > 1–2 in the 3 × 3); the normal-spread radius is gone (it re-blurred clean glass) | `r6a`, footprint / core vs a planar mirror: v1 +24.8 / −9.0 → **+11.9 / −2.1 %**, v2 +22.0 / −9.4 → **+11.1 / −4.5 %**, o45 +5.7 / −3.5 → +2.0 / −2.2, o60 +7.9 / −5.9 → +2.7 / −3.6 (bar +10 / 5 %) | **FLAG** (core PASS everywhere; head-on footprint 1–2 points over) |
| Art #3 / AD-2 | Not changed this round | `r6a` L0: carpet 3.3 % / p95 8.4 % (Ultra 3.2 / 7.4), wallpaper 3.4 / 12.6 %, ratio 1.006. `r6b` Office: L0 carpet 8.7 % / 27.0 %, L0 wallpaper 6.8 / 23.8 %, ratio 1.030. Both Office numbers are worse than at 06:55 (6.8 / 19.1, 6.2 / 21.3); the change came with the 09:17 re-sync (main's lamps, W1), not with this round's Metal (the parity kernel shoots primary rays, no glass normals) | **open** (glass-rt-track: per-hit ranked soft shadows) |
| Art #5 AD-4 | Back on the open list (§8, §11.6) | — | open (glass track, `FrontRooms/Glass`) |
| Art #6 sign-off | B4 / VL284 and D2 / VL316 stay with Red; AD-5 measured (Relay −1.5/255 in the lit window; *corrected in round 2: that figure is the harness's dark-relay pose; art review 2 measured the Relay 2 m behind at a LIT window in real game frames at −8.9/255 mean, p5 −36: it reads as a ghost, `W/rt_work/art_r2/sheets/r2art_3_lens_relay.jpg`*) | — | WAIT-RED |
| Art #7 game-flow evidence | §4 and VL315 corrected (10 of 24 CxVis glass frames untraced, including the break shot; end-of-run 38.5 % misses; cause CxVis's two renders per capture) | Break poses traced in a render loop: `s9` D/E frames | corrected; H6 open (CxVis render-every-frame mode, W/tools) |
| Art #8 minor | Not changed: jagged traced edge along the reflected window stop (B6), 1-px dark line at o45 edge-on glass | — | open (flag) |

### 11.2 Numbers (UNDER LOAD)

| Check | Before (06:55 runs) | After (this round) |
|---|---|---|
| A2 IoU prepass/raster v1–v5 | 0.997 / 0.997 / 0.997 / 0.996 / 0.999 | identical |
| A4 RT at weight 0 vs Off | 0 px | 0 px (all five views, barriers on too) |
| A5 Relay moved 0.7 m | 0 stale px | 0 stale px (TLAS reuse on) |
| A6 misses | 0.00 % | 0.00 % |
| A9 cameras | +100 trace events / 100 frames | same; ring busy +34 |
| A8 capacity | High 2,048 / Ultra 2,606, 701 priority kept | same |
| A13 fracture | 6 frames after notBeforeFrame | same (FLAG) |
| B3 lamps same frame | r 1.000 | r 0.997–1.000 (7 of 20 changing lenses) |
| B6 crawl | 0.014 / 0.014 vs 0.017 | 0.014 / 0.014 vs 0.017 |
| Walk main thread (CPU), quiet p50 / p99 | 0.45 / 0.92 (wall) | 0.42 / 0.81 |
| Walk streaming max | 4.82 | 1.67 |
| Walk blank frames | 0 of 581 | 0 of 581 |
| TLAS on a still view (timed High frames) | rebuilt every frame | reused 17 of 22 |
| Universal player, native (`ps10`) | arm64-only player of step 1 | arm64 slice `FC491058`, `on: Apple9, High`, walk 0 blank, break 436,575 → 0, 0 errors |
| Universal player, Rosetta 2 (`ps11`) | `ps7` (pre-fix dylib) | translated (`proc_translated 1`), plugin loaded (caps `0x30ff`), so the x86_64 slice (UUID `94FAD184`, by `dwarfdump`; no dyld line for it), `on: Apple9, High`, walk 0 blank, break 436,575 → 0, 0 errors |
| Store archive, sandboxed (`store_check`) | not run | `on: Apple9, High`, 404,179 px, 0 errors |
| GPU added by this round (player A/B, §11.4) | — | at 1440p, added frame time: v2 High +0.47, Ultra +0.42; v5 High +0.30, Ultra +0.49 ms. Stage spans: High +0.41 (v2 and v5), Ultra +0.38 (v2), **+0.62 (v5)** ms. *(Corrected in round 2, engineering #8: this row read "+0.2 to +0.5 ms", which misses v5 Ultra.)* |

### 11.3 The package (RD2 folded in)

`W/apply/glass_rt_g14.sh` (apply convention v2), rebuilt 09:52 by `W/rt_work/fix31/package/build_package.py`. The 10-14 package is kept in `W/_partial/glass_rt_g14_prev_20261022-095258/`.

| | |
|---|---|
| Writes (20) | `NativePlugin/` FRGlassRTShared.h `fc3dabc5`, FRGlassRTLayout.h `e7ee16a5`, FRGlassRTScene.h `30eb7d63`, FRGlassRTScene.mm `153303f1`, FrontRoomsGlassRT.metal `b9ffd50a`, FrontRoomsMetalGlassRT.mm `e2ab3641`, tools/frglassrt_validate.mm `6cb79b9b`, build_frontrooms_metal_glass_rt.sh `664e26d7` (universal); `Tools/rt/compose_rt_sheets.py` `59faf65b` (new); `Assets/Shaders/GlassRT/FrontRoomsGlassRTPrepass.shader` `0db6d49c`; `Assets/Editor/RT/` WebGLStripper `4c2cf035`, Isolation `8b178cf4`, Verify `3b1a0a92`, Importer `7561f80e`; `Assets/Scripts/Rendering/GlassRT/` Native `96aab6cd`, FrontRoomsGlassRT `0bf31ad6`, System `c19cbfb1`; `FrontRoomsMetalGlassRTRendererFeature.cs` `d205712b`; `Assets/Plugins/macOS/libFrontRoomsMetalGlassRT.dylib` **`b1c6de26`** (466,416 B, x86_64 + arm64; slices arm64 `b5820c6b`, x86_64 `4b2d3a5b`) and its `.meta` `0377540d` (GUID `9b3bca94…` kept, Standalone macOS CPU `AnyCPU`) |
| Bases | main at 09:52 (HEAD `d4d0e7a` + tree); the 16 old bases are unchanged since 10-14; new bases: FrontRoomsGlassRT.cs `921a86c2`, Importer `511c4186`, build script `35fb10d7`, dylib `.meta` `e5fd5626` |
| Deletes (moved to the backup) | `Assets/Shaders/FrontRoomsMetalGlassRTComposite.shader` + `.meta`, `Assets/Scripts/Rendering/MetalGlassReflection.meta` + the empty folder (outside the visual ship paths: **since fix round 2 OPTIONAL, `--with-deletions`, Red's OK at landing**, as in §1) |
| Keeps | 9 GUID metas; contract notices: `FrontRoomsGlassRTCamera.cs`, `FrontRoomsGlass.shader` (now recorded at GD3's `4d378e34`), `FrontRoomsGlassRTStripper.cs`, and new `FrontRoomsGlassBreakable.cs` `51de5ccb` (G14 subscribes to its `MeshesReady` / `StageChanged` and reads its stage renderers) |
| Uses from map code (no edit) | `FrontRoomsMapWorld.LiveApplied`, `StartLampsNorth/South`, `LampLevel`, `NoLamp` (MapWorld `7ef0ca45`) |
| Checks | Dry run on main 10:07 (HEAD `18acaa16` + working tree; the 20 bases recorded at 09:52 still match): **DRY RUN OK**. Scratch (`W/tools/_test/glass_rt_g14_1022/t_apply_script_tests.log`): dry OK → apply MANIFEST OK → again ALREADY APPLIED; half-applied, moved base and changed kept `.meta` REFUSED; the dylib `.meta` after apply keeps GUID `9b3bca94…` with `AnyCPU`. `rebase_apply.sh glass_rt_g14 --no-install --ios`: PASS. Roslyn `--all` on main (10:08, HEAD `18acaa16` + tree) + the 8 payload `.cs`: COMPILE OK macOS and iOS, 7 assemblies, 0 errors (`W/rt_work/fix31/roslyn4_{mac,ios}`) |
| Command | `bash /Users/redwang/FrontRoomsVisualWork/apply/glass_rt_g14.sh --apply`, then restart Unity (an open editor keeps the old plugin) |

### 11.4 Red's open decisions, with this round's numbers

Player A/B at 1080p and 1440p (`W/logs/fix31/ab_players.zsh`). The universal app was run before this round (dylib `c138bf1d`) and after it (`b1c6de26`), alternated old / new / old / new in the quietest minutes of the day (1-minute load 42–58, still **UNDER LOAD**). Each cell is the median of 16 interleaved rounds from the second pair (load 42–44); the first pair agrees within 0.3 ms except the disturbed v5 rounds of the first new run. **Added frame time** = whole-frame GPU, RT − Off. **Stages** = AS + trace + resolve spans.

| Pose, resolution | Added frame time High / Ultra, old → **new** | Stages High / Ultra, old → **new** | B8 bar (added frame time) | Verdict (new) |
|---|---|---|---|---|
| v2 1.5 m, 1080p | 1.43 / 2.90 → **1.28 / 2.71** | 3.17 / 4.72 → **3.35 / 4.91** | — | — |
| v2 1.5 m, 1440p | 2.99 / 5.03 → **3.47 / 5.45** | 5.48 / 7.60 → **5.88 / 7.98** | High 2.0, Ultra 3.5 | FAIL / FAIL |
| v5 0.55 m (pane fills the frame), 1080p | 2.64 / 4.44 → **2.86 / 4.68** | 4.07 / 5.85 → **4.28 / 6.16** | the shot pose vs the +2 ms target | over (+0.86) |
| v5 0.55 m, 1440p | 4.50 / 7.07 → **4.79 / 7.56** | 7.03 / 9.63 → **7.44 / 10.25** | High 5, Ultra 7 | **PASS** / FAIL |

- **Which metric each bar uses.** The design never said. This report now reads B8 on the **added frame time**, because that is what the player waits for: the RT compute overlaps the opaque raster that does not depend on it. The stage spans are upper bounds. In quiet minutes the added time is 55–65 % of the span. The +0.76 ms "whole-frame" figure from `ps6` (load 350–430) was load noise, not overlap.
- **This round's own GPU cost** (*corrected in round 2*): at 1440p +0.30 to +0.49 ms of added frame time (v2 High +0.47, Ultra +0.42; v5 High +0.30, Ultra +0.49), and +0.38 to +0.62 ms of stage span (v5 Ultra +0.62). Most of it is the 25-tap mean normal in the trace (+0.3 ms) and the smoothed blur radius (+0.1 ms). That is the price of AD-1.
- **D1, default quality.** Every standalone Mac player starts at quality level 5, so the app runs **Ultra**. That includes the store archive, whose smoke reports "quality level 5 (Ultra)". Ultra costs **+2.0 ms (v2) and +2.8 ms (v5) at 1440p** over High, and buys nothing measurable: crawl 0.014 vs 0.014, carpet parity 3.2 vs 3.3 %, lens footprint within 1 point. **Recommendation unchanged: High at every level; Ultra only as an explicit setting** (`DefaultQuality()`, a visual file, one line).
- **D6, break-shot budget.** At the shot pose (v5 1080p) RT adds **2.86 ms on High** against the plan's +2 ms. Now that the S1/S2 meshes are registered, the S2 beat costs what S0 costs; it no longer gets cheaper by losing the reflection. Options:
  - **(a)** Accept +0.9 ms over the target for the 1.65 s hold.
  - **(b)** A 4-lamp shot budget: the ablation in §5.1 cut the trace by 56 %, which would put the shot at about +1.6 ms (ESTIMATE).
  - **(c)** Drop to High, which only matters if D1 stays Ultra.
  - **Recommendation: (b) once P2's per-hit lamp cull lands; (a) until then.**
- **D2, player body in dark windows.** Unchanged (§5.3, VL316). One fact is new: the dark window now keeps its mirror through S0, S1 and S2 (`30v_F31_GD3_stages.jpg`), so the missing body shows through the whole break shot at a dark window, not only before it.

### 11.5 Where a reviewer was not followed, and why

- **"Rebuild the TLAS → AS 0.93–1.6 ms."** The reuse is in, but the measured AS span does not move with it (1.22 ms reused vs 1.21 ms rebuilt; 0.93–1.3 ms on frames with no build at all). The span is the encoder's place in the GPU queue under load, not the 2,000-instance build. The reuse stays (it cannot hurt and is exact), but it is not a cost fix. The cost is the trace: ~86 lamps per hit plus shadow rays (§5.1 ablation: 4 lamps −56 %).
- **"Drive lamp and chunk changes from map events (P2)."** Chunk changes are now budgeted and sliced on G14's side. Lamp changes cannot be: the lens glow lives in the map's property blocks, and the experiment that skipped reads (the logical-level test) broke B3. That part stays a map contract (P2 `CopyLampStates`).
- **"Re-measure at load below 32."** The load did not drop below 44 during this round. The main thread is now reported in thread CPU time, which load does not inflate. The GPU A/B old vs new ran in the same minute (§11.4).
- **"The memory note on Rosetta is stale."** It is not: it says Rosetta was reinstalled on 10-14. The stale lines were §1.1 and §8 of this report, now corrected.
- **Art #7 "add a render-every-frame mode to CxVis".** CxVis is a shared tool in `W/tools`, not part of this package. It is recorded as H6 for the 游戏视觉 session. The break poses traced every frame come from `G14VStages` instead.

### 11.6 Still open

| ID | Severity | What | Owner |
|---|---|---|---|
| F3 / A7 | major (cost) | Main thread 0.42 ms p50 CPU, quiet p99 0.81, streaming max 1.67 ms; bar 0.3. Left: lamp + lens reads 0.23 ms p50 | 关卡设计 contract: P2 lamp events (`CopyLampStates`, C3/C5/C7) |
| B8 / D6 | major (cost) | §11.4 | Red (D1/D6), then glass-rt-track P2 (per-hit lamp cull) |
| AD-2 | look | L0 carpet p95 8.4 % (bar 8); Office L0 carpet 8.7 / 27.0 % on today's main | glass-rt-track |
| AD-3 head-on | flag | footprint +11.1 / +11.9 % (bar +10), core within | glass-rt-track |
| AD-4 | look | lit grime band across dark-beyond windows crosses the Relay's boots | glass track (`FrontRooms/Glass`) |
| B4 / AD-6, D2, AD-5 | sign-off | showcase set, player body in dark windows; the Relay reads as a ghost in lit windows (−8.9/255 mean, p5 −36; art review 2) | Red (`40_for_red.md` D2) |
| H6 | tool | CxVis captures without a G14 warm-up and with two renders per capture: no evidence for RT until it renders every frame | 游戏视觉 (W/tools) |
| H4 | tool | G10 proof predates GD3's slabs | 游戏视觉 (W/tools) |
| B1 rubber, B5, B6, A13, E2, E3 | flags | as in §8 | glass-rt-track |
| Eng #13 | minor | geometry pages 3 → 5 over a soak; macOS 12 target vs the plugin's 13; Intel branch unexecuted; Rosetta first-use stalls | glass-rt-track / anyone with an Intel Mac |

### 11.7 How this round ran (two copies, one fix)

- The first launch (08:41) was cut by the account switch. A relaunch then ran as **two concurrent copies of the same stage** (a messaging bug), and both edited `FrontRoomsGlassRT.metal` in the clone. One copy added the mean-normal trace and the pane-averaged radius in `fr_resolve`; the other added the smoothed radius in the blur passes and the lens-edge exclusion. Both were stopped at 09:32.
- **This run** (09:33) diffed every clone RT file against the pre-fix state (`W/rt_work/fix31/base_clone`, 08:41), the 09:15 build `pb2` and the 09:28 build `pb3`, and against the package and main.
- It kept **one coherent state**: Metal `b9ffd50a` (both copies' blur changes together, built as `pb3` = dylib `b1c6de26`), the C# of 09:23, and three small edits of its own:
  - the mid-frame fault site moved so it always fires;
  - `FailClosed` names its site;
  - the cost harness counts TLAS reuse.
- It recompiled (0 `error CS`) and only then measured.
- **Why this Metal state.** Compared on the same crops, the stacked state is the only one without blocks: cube holes `pb2` 57 → `pb3` 0, smudged boot `pb2` 3 → `pb3` 0. It also fixes most of AD-3.
- Runs: `W/logs/fix31/chain4_fix31.zsh` (`chain4.out`).
- Earlier partial outputs (`r4a`, `r4b`, `s8`, `n1`, `n2`, `r4d_out` = the stopped `r5`) are kept. Only `n1`/`n2` and `s8` are quoted, and only where `s9` / this run's code agrees.
- The extra lock files `W/_partial/fix31_yielded_lock_093246` and `rt_work/fix31/OWNER.lockdir` record the hand-over.

### 11.8 Files

- Package: `W/apply/glass_rt_g14*` (builder `W/rt_work/fix31/package/`), previous package `W/_partial/glass_rt_g14_prev_20261022-095258/`, scratch tests `W/tools/_test/glass_rt_g14_1022/`.
- Clone `W/proj_rt_eng3` (clone-only: `Assets/Editor/G14V/*`, `Assets/G14V/*`, FMOD bank build copied in for the archive). The clone's own apply manifest is moved to `W/logs/fix31/applied_manifest_CLONE_proj_rt_eng3_0917.txt` so nobody reads it as main's.
- Logs: `W/logs/fix31/` (`c32`, `r6a`, `r6b`, `s9`, `iso5`, `pb5`, `g7`, `xc2`, `chain4.out`, `load.tsv`, dry runs, `rebase_noinstall.log`, `ab.out`).
- Outputs: `W/rt_work/fix31/` (`r6a_out`, `r6b_out`, `s9_out`, `iso5_out`, `n1_out`, `n2_out`, `ps10_out`, `ps11_out`, `ab*_out`, `g7_out`, `store_check/`, `player_universal/`, `xcode_export/`), tools `tools/{ad1_metric,ad3_metric,compose_f31}.py`, `tools/store_archive_check.zsh`.
- Images (Shot format v1.1): `30v_F31_AD1_blocks.jpg`, `30v_F31_AD3_lens.jpg`, `30v_F31_GD3_stages.jpg`, `30v_F31_walk_main.jpg`, `30v_F31_player.jpg`.
- VL rows: `W/vl_pending/glass_rt_g14.md` (VL281, VL282, VL283, VL312, VL313, VL314, VL315 updated; VL284 and VL316 unchanged).

---

## 12. Fix round 2 (review round 2: engineering READY with 9 items, art NOT READY)

2026-10-22 10:31–11:3x · 游戏视觉 · workflow stage "G14 fix round 2" (session local_5656cffd) · **verdict: READY to land. Both art regressions of fix round 1 are fixed and re-checked in fresh runs (stream-in jump: no shown frame with holes, every lamp present when the reflection shows; lens edges: in-motion crawl back to the pre-fix range), the two further art findings are fixed (VL312's untraced tiles) or corrected in this report (the Relay in lit glass), and all nine engineering items are done. The package holds the round-2 plugin `ec31f4fa` and dry-runs OK on today's main. Cost stays Red's call (`40_for_red.md`).**

Clone `W/proj_rt_eng3`, re-synced from main at 10:33 (HEAD `18acaa16` + working tree, exactly as `mkclone.sh`: rsync of the five folders with the iCloud excludes, `apply_local_patches.py` "nothing to do", `install_tools.py`), then `glass_rt_g14` re-applied (MANIFEST OK) and this round's edits made. The re-sync brought in main's 10:20 print driver (`FrontRoomsPrintDriver.cs`, `FR_Print_HardEdge.png`), the Ad_03 video and the StreamingAssets FMOD banks. The clone's apply manifest is in `W/logs/fix32/`. A lock (`W/rt_work/fix32/OWNER.lockdir`) kept this a single copy. Every timing is **UNDER LOAD** (1-minute load 27–138, `W/logs/fix32/load.tsv`).

Written in Red's project: this section, the corrections marked in §0, §1, §1.1 and §11, `40_for_red.md` (new), the six 07:19 sheets re-framed in the Shot format (unframed originals in `W/_partial/fix32_vl_unframed_105104/`) and the new `images/30v_F32_*.jpg` (claimed in `LANDINGS.md` at 11:06). No Unity, batch, build or render ran on main. No git write command. Nothing was downloaded.

### 12.0 Short answer (for Red)

1. **After a jump the reflection is right when it appears.** Before, after a jump (a run start, a floor change, the art review's teleport) the reflection showed holes for 3–5 s and was lit by 1 of 88 lamps. Now the chunks near the camera are registered on the frame they appear (a 2 ms burst, nearest first), and each camera keeps the old zone-cube reflection until its own traced frame has no holes, then fades in over 12 frames. Three jumps (the art review's window and two windows 126 m and 164 m away where the map was not built): shown after 23–33 frames (0.4–0.55 s at 60 fps), **0 shown frames with holes**, lamps complete (87 = the 86 lit raster lights within 30 m plus the fill light), no flicker. A normal walk never triggers it.
2. **Reflected ceiling lamps no longer shimmer in motion.** Lens borders get a 1-pixel anti-alias in a compressed brightness domain (it does not widen the lamps), and the lens interiors are filtered again. Moving-shot lens crawl 0.136 → **0.104** (pre-fix range 0.089–0.102). The price: the reflected lamps carry 4–5 % less light than a perfect mirror (§12.4).
3. **The engineering items are done:** the plugin's ABI is checked against the loaded plugin (a stale plugin after an apply with Unity open now fails closed with one status line, tested); editor scripts are ASCII and tuple-free; the walk and the jumps log frames-to-registered per chunk; the deletions are optional (`--with-deletions`); this report's top rows match the package; the 07:19 sheets carry the Shot format.
4. **Your three decisions are written up with numbers** in `40_for_red.md`: D1 (default quality, now with projections for 8–20-core M3/M4 GPUs and Retina memory), D6 (break-shot budget), D2 (player body in dark windows).
5. **Still open** (§12.5): AD-3 head-on lens footprint/core 0.2–0.3 points over their bars and the lamps' 4–5 % light loss (LENS-E); AD-2 carpet parity; the new print driver's moving wallpaper is reflected as its static frame K00; cost (D1/D6).

**Apply command:** `bash /Users/redwang/FrontRoomsVisualWork/apply/glass_rt_g14.sh --apply`, then restart Unity. Optional clean-up with Red's OK: add `--with-deletions`.

### 12.1 Every review item and what was done

The art review's text reached this stage cut off after its defect 2; items Art #3 and Art #4 below are taken from its evidence sheets (`W/rt_work/art_r2/sheets/r2art_2_stage_evidence.jpg`, `r2art_3_lens_relay.jpg`), which state them.

| Review item | What was done (files) | Re-check (fresh runs, clone, UNDER LOAD) | Verdict |
|---|---|---|---|
| **Art #1** stream-in jump: holes for 190–310 frames, 1 of 88 lamps (cause: the 0.2 ms budget + weight 1 on every traced frame) | (a) **Burst budget, nearest chunk first** (`FrontRoomsGlassRTSystem.cs`): while a chunk within `StreamGateRadius` (30 m = the lamp radius) of the traced camera has not finished its first scan, the sliced chunk work gets 2 ms instead of 0.2 ms, and the queue is served nearest chunk first (it was FIFO). (b) **Per-camera stream-in gate**: a camera holds weight 0 (today's glass, the zone cube) when a chunk is born within 30 m, or the camera jumps > 8 m into an area the map has not built, until (i) no near chunk is unregistered or unbuilt and no near chunk was born for 8 frames, and (ii) the camera's OWN traced frame after that misses ≤ 0.5 % of its glass px (new plugin export `FRGlassRT_GetCameraCoverage`, per camera; the global stats belong to whichever camera finished last). Then a 12-frame fade-in (the glass shader already blends by `_FR_GlassRTWeight`). 90 traced frames without a clean reading open it anyway (a view whose rays leave the built map). While any camera is held the render thread builds 48 BLASes / 120k triangles per event instead of 8 / 10k. (c) **Harness**: a `jump` phase (three far teleports, the camera rendered every frame from the jump on, no Flush or Settle) and frames-to-registered per chunk | `r8b` jump: J1 (the art review's dead-lamp window): held 25 frames, shown +25, weight 1 at +36; J2 (164 m away, map not built): held 33, shown +33, weight 1 at +44; J3 (126 m, map not built): held 23, shown +23, weight 1 at +34. **Shown frames with misses > 0.5 %: 0, 0, 0. Lamps when first shown 87 / 87 / 86** (lit raster lights within 30 m: 86 / 86 / 85, plus the fill). Re-holds after showing: 0 (the first gate version re-held once in J2 when the map built a ring-2 chunk late: fixed by waiting for unbuilt near squares). Near chunks registered on their first frame (10 and 12 born within 30 m, 0 frames each). Main thread while held: p95 2.3, max 2.9 ms CPU (the burst; 23–33 frames). Walk (`r8b`): 5 chunks born at 47.8–60.9 m, registered in 6–41 frames, **0 holds, 0 burst frames, 0 shown frames with holes, BLANK 0** | **FAIL→FIXED** |
| **Art #2** lens edges alias in motion (crawl 0.096 → 0.136, round 1 switched the edge filter off on lit lenses) | `FrontRoomsGlassRT.metal` `fr_resolve`: on a lit lens BORDER (3×3 max > 1–2, min < 0.6–1.0) the 3×3 kernel runs in a compressed domain c = x / (2 + x): a 1-px AA that softens the step mostly on the lit side and lifts a dark neighbour of a 4.7 lens to ≈ 0.5, below the 0.8 footprint threshold. Inside a lens (the whole 3×3 lit) the linear kernel runs again, scaled by `LensInteriorFilter` (C#, sent in the unused `FRFrame.fogColor.w`; shipped 1.0). Tuned in three runs of the same build | Moving shot v2, 60 frames, reflected lens px crawl: **interior 0 → 0.124, 0.5 → 0.110, 1.0 → 0.100** (t0/t5/t10); final run `r8a` **0.104**; pre-fix r1a–r4a 0.089–0.102; round 1 0.136; raster floor 0.136. AD-3 head-on vs the planar mirror (footprint / core), same three runs: +10.2 / +1.6 %, +10.2 / −1.7 %, +10.2 / −4.0 % (v1); +10.3 / −1.6, +10.3 / −3.5, +10.3 / −5.2 % (v2); `r8a` +10.2 / −4.0 %, +10.3 / −5.3 %. o45 / o60 footprint +1.5 / +2.0 %. The price: the compressed-domain border AA is not energy-conserving, so the reflected lamps lose 4–5 % of their light (lens energy RT / mirror 0.962 / 0.945 at o45 / o60; round 1 0.997 / 0.998), and B2 v1 drops to 85.5 % of blocks. AD-1 blocks: cube 0, boots 0, specks 0 (unchanged) | **FAIL→FIXED** (crawl); AD-3 **FLAG** (+0.2–0.3 points over both bars); lens energy −4–5 % (new, §12.5) |
| **Art #3** (sheet) VL312's lit tiles are untraced: `G14VStages` saved its frames after the hit-id render (debug mode = weight 0) | Clone-only harness `G14VStages`: after each measurement it renders once more normally, so the saved frame is traced (logs the weight) | `s10`: 34 of 34 saved frames at weight 1.000; S1 beat 437,356 px, S2 beat 437,296 px, dark window S0 / S1 / S2 437,346 / 437,346 / 437,259 px, 0 stale (as s9); new sheet `30v_F32_GD3_stages.jpg` (VL312) | **FIXED** (evidence) |
| **Art #4** (sheet) the Relay reads as a ghost in a LIT window (−8.9/255 mean, p5 −36), not −1.5/255 | Report corrected (§11.1 Art #6, §11.6); D2 in `40_for_red.md` says so | — | **corrected** |
| Art: confirmed holds | AD-1 blocks 0; GD3 S1/S2 cracks keep their contrast; dark window keeps its mirror S0–S2; a dead lamp leaves the reflection; L0 red tint ≤ 0.025 % | `r8a` AD-1: cube 0 / boots 0 / specks 0 | holds |
| **Eng #1** D1: every Apple9 Mac runs Ultra; numbers from one M3 Max only | No code change (Red's call). `40_for_red.md` D1 now has the projections by GPU core count (8–10-core M3/M4: ×4–5, i.e. 1440p High 14–24 ms, Ultra 22–38 ms added; Pro ×2–2.9; Max ×1.25–1.33), RT memory per resolution (36 / 44 B per px: 1440p 127 / 155 MiB, 2880 × 1864 184 / 225 MiB, 3456 × 2234 265 / 324 MiB) and a third option: RT on by default only on GPUs with enough cores (a native core-count query, follow-up) | — | answered (ask) |
| **Eng #2** the deletions need Red's OK but `--apply` performs them | `glass_rt_g14.sh`: deletions only with `--apply --with-deletions` (can run after a plain apply); without it they are listed as "left in place" and not checked. Also: they sit outside the visual ship paths of the brief (`Assets/Shaders/`, `Assets/Scripts/Rendering/MetalGlassReflection`), so they stay optional; 0 references (the reviewer's check) | scratch tests §12.3 | **FIXED** |
| **Eng #3** editor scripts: non-ASCII + tuples | `FrontRoomsGlassRTVerify.cs`: 107 non-ASCII lines → 0, every tuple (14 sites: `BitDiff`, `Centroid`, `Crawl`, `Specks`, the window/pose/renderer lists, `Pairs`, the parity configs and the knob backup) → 12 small structs; `FrontRoomsGlassRTIsolation.cs`: 17 → 0 and the tuple loop → two arrays; `Importer` 5 → 0, `WebGLStripper` 1 → 0 | Roslyn `--all` macOS + iOS: 0 errors; `c34` 0 `error CS`; the harness ran (`r8a`, `r8b`) | **FIXED** (the runtime files keep their `·`/`§` in comments and log strings: they compile in main since 10-03 and the rule is for editor scripts) |
| **Eng #4** top of `30_final.md` contradicts the package | §0 item 7, the §1 intro, the dylib row and the §1.1 table rewritten to the current package (universal `ec31f4fa`, `.meta` AnyCPU, 20 paths); the morning's values stay as marked history | — | **FIXED** |
| **Eng #5** VL283/284/313/315/316 sheets without the Shot format | `30v_P1_parity`, `30v_P1_office`, `30v_AD6_showcase`, `30v_H3_prepass_bit`, `30v_V2_red_tint`, `30v_D2_dark_window` wrapped with `shotframe` (same file names; unframed originals moved to `W/_partial/fix32_vl_unframed_105104/`) | — | **FIXED** |
| **Eng #6** no frames-to-registered measurement | `ChunkTimings` (born / registered frame, distance at birth and at registration, renderers, lights, burst) for every chunk's first scan; the walk writes `walk_chunks.tsv`, each jump `jump_<pose>_chunks.tsv` | walk: 6–41 frames, born ≥ 47.8 m; jumps: near chunks 0 frames, all chunks p50 2–44, max 83–163 frames (the far ring at 0.2 ms) | **FIXED** |
| **Eng #7** the size check never asks the plugin | New export `FRGlassRT_GetAbi(sizes, n)` returns ABI 32 and the 7 struct sizes the plugin was built with; `CheckNative` calls it first. An old plugin has no such export: `EntryPointNotFoundException` → "plugin is an old build", RT Off, no other native call | `o1` (new scripts with the OLD plugin `b1c6de26`, what an open editor keeps after an apply): the new scripts with the OLD plugin `b1c6de26` loaded: status "off: plugin is an old build: FRGlassRT_GetAbi …" (one log line, not an error), Quality Off (requested Ultra), no RT component on the camera, over 120 rendered frames 0 plugin events, 0 trace events, 0.0 MB of targets, 0 registrations, `PrepareMeshes` / `Register` → −1, 0 `EntryPointNotFoundException` in the log. (Before: the first call of a new export threw once per session.) | **FIXED** |
| **Eng #8** two report details | §11: "+0.2–0.5 ms" replaced by the per-pose figures (added frame time v2 High +0.47, Ultra +0.42, v5 High +0.30, Ultra +0.49 ms; stage spans up to +0.62 ms at v5 Ultra); the Rosetta line now says the x86_64 load is implied by `proc_translated 1` and caps `0x30ff` (UUID `94FAD184` from `dwarfdump`), with no dyld line | — | **FIXED** |
| **Eng #9** `40_for_red.md` missing | Written: D1, D6, D2, each with what the player sees, options, cost, recommendation and what it blocks | — | **FIXED** |

### 12.2 Numbers (UNDER LOAD)

| Check | Round 1 (`r6a`/`r6b`, 09:17 main) | Round 2 (`r8a`/`r8b`, 10:33 main) |
|---|---|---|
| Stream-in jump, frames until the reflection shows (J1 / J2 / J3) | shown at once with holes; complete after 190–310 frames | 25 / 33 / 23 (weight 1 at 36 / 44 / 34) |
| Shown frames with misses > 0.5 % of the glass (jumps) | (art review 2: 387,530 of 434,712 px at +30) | 0 / 0 / 0 |
| Lamps when the reflection first shows (raster lights within 30 m) | 1 of 88 at +30 | 87 (86) / 87 (86) / 86 (85) |
| Main thread while held after a jump (CPU) | — | p95 2.3, max 2.9 ms for 23–33 frames |
| Walk: holds / shown frames with holes / BLANK | — / not measured / 0 | 0 / 0 / 0 |
| Walk main thread CPU quiet p50 / p99, streaming max | 0.42 / 0.81, 1.67 | 0.345 / 0.73, 1.65 |
| Frames to registered per chunk, walk (born at) | not measured | 6–41 (47.8–60.9 m) |
| Moving shot, reflected lens crawl | 0.136 | 0.104 |
| AD-3 head-on footprint / core v1, v2 | +11.9 / −2.1, +11.1 / −4.5 % | +10.2 / −4.0 %, +10.3 / −5.3 % |
| AD-1 cube holes / boot holes / dark-relay specks | 0 / 0 / 0 | 0 / 0 / 0 |
| A2 IoU v1–v5 / A4 weight 0 / A6 misses | 0.997…0.999 / 0 px / 0.00 % | 0.997 / 0.997 / 0.997 / 0.996 / 0.999 / 0 px / 0.00 % |
| B3 lamps same frame r (changing lenses) | 0.997–1.000 (7 of 20) | 0.998–1.000 (7 of 20) |
| B6 strafe crawl High / Ultra vs unfiltered | 0.014 / 0.014 vs 0.017 | 0.014 / 0.014 vs 0.016 |
| B1 parity L0 carpet mean / p95, wallpaper, ceiling | 3.3 / 8.4 %, 3.4 / 12.6 %, 0.7 % | 3.4 / 8.5 %, 3.5 / 12.9 %, 0.7 % |
| B2 mirror match v1 / v2 / v3 (blocks within 0.9–1.1) | 91.8 / 93.2 / 95.5 % | 85.5 / 93.2 / 95.5 % (interior 0: 88.2 at v1; part of the drop came with the 10:33 re-sync, median ratio 1.041 → 1.032) |
| GPU, player A/B round 1 (`b1c6de26`) vs round 2 (`ec31f4fa`), second pair (load 31–38), added frame time High / Ultra | v2 1440p 4.86 / 7.42, v5 1440p 6.70 / 10.62 ms | v2 1440p 4.58 / 7.01, v5 1440p 6.05 / 10.15 ms (1080p: v2 1.96 / 3.33, v5 3.37 / 5.43); new − old −0.65 to +0.08 ms: no measurable change (`W/logs/fix32/ab.out`, `ab*_out`) |
| Fail-closed: BuildFrame entry + 5 entry points | 1 entry, 5 / 5 PASS | 1 entry, 5 / 5 PASS |
| Old plugin + new scripts (`o1`) | (EntryPointNotFound at the first new export, one console error) | RT Off: "off: plugin is an old build: FRGlassRT_GetAbi", one log line, 0 errors, 0 exceptions, 0 plugin events, 0 MB, 0 registrations |
| Universal player native / Rosetta 2 (`ps12` / `ps13`) | on: Apple9 High, walk 0 blank, break → 0, 0 errors | universal player (`BuildPipeline.BuildPlayer`, URP intact, the plugin `ec31f4fa` in `Contents/PlugIns`): native (`proc_translated 0`) and Rosetta 2 (`proc_translated 1`) both load the plugin past the ABI check (caps `0x307f` / `0x30ff`), `on: Apple9, High`, 38 of 38 GD3 slabs on bit 30, 405,925 px traced, the 600-frame walk 0 blank, the break 436,575 → 0 px on the break frame, 0 errors or exceptions |
| Isolation (`iso6`) | WebGL / iOS / Win64 0 `FRGlassRT_`, strippers as tested | WebGL / iOS / Win64 player scripts 0 `FRGlassRT_` (macOS 25: the two new exports); importer Standalone macOS `AnyCPU`, editor ARM64, all others off; strip step WebGL prepass shader 4/4, glass `FRGlassRTPrepass` 4/4, ForwardLit 0/4; iOS prepass 4/4, `_FR_GLASS_RT` 2/4; macOS 0/4 |
| GD3 stages (`s10`) | S1/S2 traced on their beats, dark window keeps the mirror, 0 stale | 34 of 34 saved frames at weight 1.000; S1 beat 437,356 px, S2 beat 437,296 px, dark window S0 / S1 / S2 437,346 / 437,346 / 437,259 px, 0 stale (as s9) |

### 12.3 The package

`W/apply/glass_rt_g14.sh` (apply convention v2), rebuilt 11:15 by `W/rt_work/fix32/package/build_package.py`; the round-1 package is kept in `W/_partial/glass_rt_g14_prev_20261022-111515/`.

| | |
|---|---|
| Writes (20) | `NativePlugin/` FRGlassRTShared.h `d730fe01`, FRGlassRTLayout.h `e7ee16a5`, FRGlassRTScene.h `30eb7d63`, FRGlassRTScene.mm `153303f1`, FrontRoomsGlassRT.metal `b498922a`, FrontRoomsMetalGlassRT.mm `b426406a`, tools/frglassrt_validate.mm `6cb79b9b`, build_frontrooms_metal_glass_rt.sh `664e26d7`; `Tools/rt/compose_rt_sheets.py` `59faf65b` (new); `Assets/Shaders/GlassRT/FrontRoomsGlassRTPrepass.shader` `0db6d49c`; `Assets/Editor/RT/` WebGLStripper `5edb7b0b`, Isolation `02e94c70`, Verify `38cf7b8d`, Importer `46c637a4`; `Assets/Scripts/Rendering/GlassRT/` Native `2b32b773`, FrontRoomsGlassRT `0bf31ad6`, System `1a8144fc`; `FrontRoomsMetalGlassRTRendererFeature.cs` `d4ced531`; `Assets/Plugins/macOS/libFrontRoomsMetalGlassRT.dylib` **`ec31f4fa`** (466,720 B, x86_64 + arm64; slices arm64 `51788054`, x86_64 `a27271df`) and its `.meta` `0377540d` (GUID `9b3bca94…` kept, Standalone macOS `AnyCPU`, editor ARM64). Changed this round: Shared.h, the .metal, the .mm, the dylib, Native, System, the renderer feature and the four editor scripts |
| Bases | main at 11:15 (HEAD `18acaa16` + working tree); all 20 unchanged since 09:52 |
| Optional deletions | `Assets/Shaders/FrontRoomsMetalGlassRTComposite.shader` + `.meta`, `Assets/Scripts/Rendering/MetalGlassReflection.meta` + the empty folder: only with `--with-deletions` (Red's OK), moved to the backup folder |
| Keeps | 9 GUID metas; contract notices `FrontRoomsGlassRTCamera.cs`, `FrontRoomsGlass.shader` (GD3 `4d378e34`), `FrontRoomsGlassRTStripper.cs`, `FrontRoomsGlassBreakable.cs` `51de5ccb` |
| Uses from map code (no edit) | `FrontRoomsMapWorld.LiveApplied`, `StartLampsNorth/South`, `LampLevel`, `NoLamp`; `MapGrid.ChunkSize` (new: chunk squares for the stream-in gate; chunk roots are found by position, as before) |
| Checks | Dry run on main 11:15: **DRY RUN OK** (deletions "left in place"); with `--with-deletions`: all 20 bases and the 3 deletions match, DRY RUN OK. Scratch tests (`W/tools/_test/glass_rt_g14_fix32/run_tests.zsh`, 13 of 13 PASS): dry OK; apply MANIFEST OK with the deletions left in place; again ALREADY APPLIED; `--with-deletions` afterwards does only the deletions (MANIFEST OK, folder removed); again ALREADY APPLIED; apply + deletions in one go; half-applied, a moved base and a changed kept `.meta` REFUSED; a changed deletion file refused only with the flag; the dylib `.meta` keeps GUID `9b3bca94…` with `AnyCPU`. `rebase_apply.sh glass_rt_g14 --no-install --ios`: PASS. Roslyn `--all` on main + the 8 payload `.cs`: COMPILE OK macOS and iOS defines, 7 assemblies, 0 errors (`W/rt_work/fix32/roslyn_main_{mac,ios}`) |
| Command | `bash /Users/redwang/FrontRoomsVisualWork/apply/glass_rt_g14.sh --apply`, then restart Unity (until the restart the open editor keeps the old plugin; the new scripts detect it by ABI and keep RT off with one status line). Optional, with Red's OK: `… --apply --with-deletions` |

### 12.4 Where a reviewer was not followed, and why

- **Art #1 (b) "hold weight 0 until the misses are ≤ 0.5 % and the lamp gather is complete".** Followed, with one change: the hold starts on a chunk born within 30 m (or a jump into unbuilt map), not on the misses alone. A misses-only trigger would turn RT off for good in any view whose rays legitimately leave the built map, and would react a frame late. The lamp condition is met by construction: a chunk's lights are gathered by its own scan, and the gate waits until every chunk square within the 30 m lamp radius is built and scanned; the jump check compares G14's lamps with the raster's lit lights (equal plus the fill).
- **Art #2 "keep a 1-px AA on lens borders (reduced f…)".** The text was cut off there. There is a three-way trade at a lit lens border: an anti-alias that conserves the lamp's light must push light outward, and anything pushed past 0.8 counts as a wider lamp (AD-3 footprint); an anti-alias that keeps the footprint must take light away (lens energy, AD-3 core); no anti-alias crawls. Round 1 chose no AA; round 2 chooses AA + footprint and gives up 4–5 % of the reflected lamps' light, because the crawl was the visible regression and 4–5 % of a reflection at the pane's ~8 % Fresnel is not. The border AA does not widen the lamps (footprint unchanged by the interior setting, +10.2 / +10.3 %); the remaining AD-3 excess (0.2–0.3 points) and the core (−4.0 / −5.2 % at the shipped interior 1.0) trade against crawl, and the three measured settings are in §12.1 and `30v_F32_lens_aa.jpg`. Interior 0.5 gives core −1.7 / −3.5 % (PASS) with crawl 0.110. We ship 1.0 because the crawl was the visible regression; `LensInteriorFilter` is one line if art prefers 0.5.
- **Eng #1 "possible follow-up: gate on GPU core count".** Not built in this round: it needs a native query of the IORegistry's GPU core count and a test on a small Mac that we do not have. It is option (c) of D1 in `40_for_red.md`.
- **Eng #3 for the runtime files.** The four runtime files keep their `·` and `§` in comments and log strings (as in main since 10-03); the rule (memory `unity-editor-script-ascii-no-tuples`) is about clone editor scripts, which are now ASCII and tuple-free. The runtime keeps its two tuple lists (`freeMaterialRanges`, `lampCandidates`) from main's base.

### 12.5 Still open

| ID | Severity | What | Owner |
|---|---|---|---|
| D1 / D6 / D2 | decision | `40_for_red.md` | Red |
| AD-3 head-on | flag | footprint +10.2 / +10.3 % (bar +10), core −4.0 / −5.2 % (bar 5) at the shipped interior filter 1.0 | glass-rt-track (art may pick 0.5: core passes, crawl 0.110) |
| LENS-E (new) | look | reflected lamps 4–5 % dimmer than the planar mirror (lens energy 0.945–0.962), B2 v1 85.5 % of blocks (bar 95): the price of the round-2 border AA. A softer knee (x / (4 + x) instead of x / (2 + x), one constant in `FRLensCompress`) would roughly halve the loss at a small risk of wider concave lamp corners; not measured | glass-rt-track / art |
| AD-2 | look | L0 carpet parity 3.4 % / p95 8.5 % (bar 3 / 8 %); Office L0 carpet 8.7 / 27.0 % | glass-rt-track (per-hit ranked soft shadows) |
| PRINT-1 (new) | look | Main's print driver (landed 10:20) moves the wallpaper print between 8 keyframes (holds of 45–90 s, 1.2 s blends) and a subliminal drift, through globals `_FR_Print` (Texture2DArray), `_FR_PrintClock`, `_FR_PrintWarp`. G14's hit shader still shades the static `_PrintTex`, which is keyframe K00, so after the first jump a reflected wall shows K00 while the wall itself shows another keyframe. Fix: bind the array and the clock in the material/frame records and sample the same slice and warp (the driver holds K00 whenever a cue can start, so cue frames match). Not a landing blocker: the mismatch is a print pattern change at the reflection's 8 % strength | glass-rt-track, contract with the wallpaper-motion line (动画和动态图形) |
| F3 / A7 | major (cost) | main thread quiet p50 0.345 ms CPU (bar 0.3); lamp + lens reads need the map's P2 lamp events | 关卡设计 contract (P2) |
| B8 | major (cost) | as §11.4; round 2 adds nothing measurable (player A/B round 1 vs round 2, second pair, load 31–38: added frame time new − old −0.65 to +0.08 ms over the four poses and both tiers, within the noise of the Off frame itself) | Red (D1/D6), glass-rt-track (P2) |
| Burst cost | minor | 2 ms main-thread slices for 23–33 frames after a jump (run start, C22 floor change); a loading moment, above A7's 1.5 ms streaming p99 | accepted for now; P2 ChunkHandles would remove the scan |
| H6, H4, AD-4, B1 rubber, B5, B6 edge, A13, E2, E3, Eng #13 | as §11.6 | | as §11.6 |

### 12.6 Files

- Package: `W/apply/glass_rt_g14*` (builder `W/rt_work/fix32/package/`), previous package `W/_partial/glass_rt_g14_prev_20261022-111515/`, scratch tests `W/tools/_test/glass_rt_g14_fix32/`.
- Clone `W/proj_rt_eng3` (clone-only: `Assets/Editor/G14V/*`, `Assets/G14V/*`; this round changed `G14VStages.cs` (traced saved frames, gate-aware warm-up) and `G14VPlayerSmoke.cs` (gate-aware warm-up)).
- Logs `W/logs/fix32/` (`resync.log`, `apply_clone.log`, `plugin_build*.log`, `c33`/`c34`, `t0`/`t5`/`t10`, `r8a`, `r8b`, `s10`, `iso6`, `pb6`, `o1`, `chain*.out`, `load.tsv`); the superseded gate versions `r7*_gatev1`, `r7*_gatev2`.
- Outputs `W/rt_work/fix32/` (`r8a_out`, `r8b_out` with `jump_*.tsv`, `walk_chunks.tsv`; `t0_out`/`t5_out`/`t10_out`; `s10_out`, `iso6_out`, `ps12_out`, `ps13_out`, `o1_out`, `player_universal/`, `pb_new/` = the plugin build + log), tools `tools/` (patch scripts, `compose_f32.py`, `wrap_vl_sheets.py`, `lens_aa_sim.py`).
- Images (Shot format v1.1): new `30v_F32_stream_in.jpg` (VL281), `30v_F32_lens_aa.jpg` (VL282), `30v_F32_GD3_stages.jpg` (VL312); re-framed `30v_P1_parity.jpg`, `30v_P1_office.jpg` (VL283), `30v_AD6_showcase.jpg` (VL284), `30v_H3_prepass_bit.jpg` (VL313), `30v_V2_red_tint.jpg` (VL315), `30v_D2_dark_window.jpg` (VL316).
- VL rows: `W/vl_pending/glass_rt_g14.md` (VL281, VL282, VL283, VL284, VL312, VL313, VL314, VL315, VL316 updated in place; no new number).
- For Red: `40_for_red.md`.
