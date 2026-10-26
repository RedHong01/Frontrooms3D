# 30 · G14 ray-traced glass: final report (package `glass_rt_g14`, independent verification on main, 2026-10-22)

2026-10-22 · 游戏视觉 · workflow stage "G14 independent verification" (Red allowed workflows on 2026-10-21; the first run was cut by a network outage on 10-21 at 23:44 and resumed here) · **verdict: the package is correct, safe and inert off the Mac on today's main, with GD3's glass live. It still fails its cost bars (main thread, GPU at 1440p and at the break-shot pose), it does not register GD3's cracked stages, and it does not make the universal Mac app trace (RD2). It is not landed: this stage only verified it.**

**Update after the package stage (2026-10-22 14:29–14:5x, §16): READY to land.** Review round 5 found no payload defect (engineering READY; art NOT READY on text only), and this stage fixed every text, sheet and process item of both lenses (§16.1). The package `W/apply/glass_rt_g14.sh` (apply convention v2) is refreshed from the verified clone: 20 paths, the universal plugin `fa0232d2` (arm64 + x86_64, minos 12.0) with its `.meta` (GUID `9b3bca94…` kept, Standalone macOS `AnyCPU`), Red's D1 (b) default (High at quality level 3 and up, Ultra only when asked for), bases recorded on main `6521e6ad` at 14:40. The 13 `APPLY_REAL` scratch tests pass, the dry run on main prints DRY RUN OK (plain and with `--with-deletions`), and Roslyn `--all` on main + the 8 payload `.cs` files is COMPILE OK with macOS and iOS defines. The round-2 package is in `W/apply/_retired/glass_rt_g14_20261022r2/`. No Mac store build with this dylib goes to players until D1 (c) lands. Where §0–§15 and §16 differ, §16 is current.

**Update after fix round 4 (2026-10-22 13:38–14:1x, §15): Red's 12:36 answers (D1 (b) now, (c) next; D6 (a) now, (b) with P2; D2 (b) in `g14-followup`) are recorded, and D1 (b) is applied in the clone: a player at quality level 5 now starts at High (`ps16` native, `ps17` Rosetta: "on: Apple9, High", 0 errors). The payload changes 10 paths. Review round 4's text and evidence items are fixed: cost as 21-run ranges, with the quiet runs shown and the left-out runs named; five sign-off sheets re-composed and one new (`30v_F34_player_cost.jpg`); the two stale VL314 sheets moved to history. The harness was fixed and extended: the back-image fit, Off frames for the move, the saved Relay frame, restart and key pickup at a dark-beyond window, and the AD-2 step at a dark-beyond window. The package was NOT refreshed in that round: review round 5 came first, then the package stage (§16, done). Where §0–§14 and §15 differ, §15 is current.**

**Update after fix round 3 (2026-10-22 13:2x, §14): the three code blockers of review round 3 are fixed in the clone and pass their new tests (scene restarts with the gate held and open; a presented key pickup and a chunk rebuild; a seqlock stress test), the plugin is rebuilt for macOS 12.0 (dylib `fa0232d2`, every newer API behind a compiler-enforced availability guard), and the round-3 text and evidence errors are corrected here, in `40_for_red.md` (rewritten: cost as ranges with FAIL verdicts, D2 for lit windows too) and in two re-composed sign-off sheets. The package was NOT refreshed in that round (it is refreshed in §16). Where §0–§13 and §14 differ, §14 is current.**

**Update after fix round 2 (2026-10-22 11:xx, §12; superseded by §13–§16): READY to land at that time. Review round 2 found two new art regressions from fix round 1 (holes and missing lamps in the reflection after a stream-in jump; aliased lens edges in motion) and nine engineering items. All are fixed or answered in §12, the package now holds the round-2 plugin `ec31f4fa` (universal), and Red's three open decisions are written up in `40_for_red.md`. Where §0–§11 and §12 differ, §12 is current.**

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
5. **It is still too expensive.** G14's own main-thread time is 0.45–0.53 ms per frame when standing or walking, up to 4.8 ms on chunk-streaming frames, and 0.17 ms when no window is in view. The design bar is 0.3 ms. GPU cost in the arm64 player at 1440p: High 5.4–6.0 ms, Ultra 7.4–8.2 ms at 1.5 m, and High 6.8–10 ms, Ultra 9.8–13.8 ms with the pane filling the frame. The bars are 2.0 / 3.5 and 5 / 7 ms. All of this was measured under load. *(Corrected in §14.3: these are single loaded runs, not bounds; over interleaved runs the added frame time spreads by about ±40 % and every cell fails its bar.)* *(That last clause is corrected in §15.3 / §16.2: over 21 runs v5 1440p High is 4.72–13.05 ms, and 4.72–4.99 in the 8 quiet runs, so it is at the 5 ms bar when the machine is quiet and over it in disturbed runs; v2 1440p High (2.06–6.13, bar 2.0) and every Ultra cell fail.)*
6. **New open items.**
   - **G14V-1:** GD3's S1 and S2 meshes are made during the hold and never registered with G14. On the S2 beat the cracked pane drops from the traced reflection to the zone cube. There are 0 stale entries at any stage.
   - **G14V-4:** the package resets the RT quality at every Play start. This breaks the G10 proof's "force Off", but not the game.
   - **H1–H3:** two verify harnesses predate GD3 or the batch order and need small updates. These are harness issues, not game defects (§2, §4, §8).
7. **RD2 is in the package (since fix round 1).** The package ships ONE universal plugin (arm64 + x86_64; fix round 2 build `ec31f4fa…`, §12.3) with its `.meta` set to Standalone macOS `AnyCPU` (GUID kept), so the universal Mac app traces on Apple silicon, natively and under Rosetta 2, and Intel Macs fall back cleanly. *(This item read "RD2 is not in this package" at 07:31, when the dylib was arm64-only `518d6769…`; the rest of this paragraph is that morning's probe.)* **A probe shows option (b) needs no downloads:** the package's sources build an x86_64 slice with the installed Xcode, with no warnings, and `lipo` makes a universal dylib (§1.1). It cannot be run here: there is no Intel Mac and no Rosetta. **(Corrected 2026-10-22 in §10: today's universal app has no RT on any Mac, because Unity leaves the ARM64-pinned plugin out of it; Rosetta 2 is installed here; RD2 is now built and run in the clone, natively and under Rosetta.)**
8. **Your decisions, now with numbers (§5.2, §5.3):**
   - **D1, default quality.** Today a standalone player defaults to Ultra. My recommendation is High at every level. Ultra costs 1.9–3.6 ms more at 1440p for no measurable gain in edge crawl or lens shape. *(Decided and corrected, §15: Red chose (b) at 12:36, and it is applied in the clone, so the default is High. Over 21 runs Ultra − High is 1.8–10.2 ms at 1440p, not 1.9–3.6.)*
   - **D6, break-shot budget.** At the break pose, RT at 1440p costs 6.8–10 ms on High. A 4-lamp shot budget cuts the trace by 56 %. *(Decided and corrected, §15: (a) now, (b) with P2. Over 21 runs v5 1440p High is 4.7–13.1 ms: at the 5 ms bar in quiet runs, over it in disturbed ones. The 1080p shot is over its +2 ms target by 0.8–3.3 ms.)*
   - **D2, player body in dark windows.** A dark window becomes a mirror with nobody in it. *(Decided, §15: (b), a reflection-only body proxy in the `g14-followup` package, for lit windows too.)*

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
| Load | **UNDER LOAD for every timing**: 1-minute load 103–401 (`W/logs/g14v/load.tsv`), with 2–4 Unity processes from other sessions. Pixel counts, IoUs, error counts and registry counts are exact; every ms is an upper bound *(corrected in §14.3: a loaded single run is not a bound; timings are quoted as ranges over runs)* |

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
- Sheet: `30v_cost_D1_D6.jpg`. *(Moved to history in fix round 4: `W/_partial/fix34_vl_sheets_prev/`. It called every ms an upper bound and quoted single values. The current sheet is `30v_F34_player_cost.jpg`, §15.)*

### 5.2 D1 (default quality) and D6 (break-shot budget)

**D1.** Today's rule maps quality level 5 to Ultra and levels 3–4 to High. A standalone Mac player starts at level 5, so the shipped app runs **Ultra**. Ultra costs **+1.9 to +3.6 ms** over High at 1440p. *(Corrected, §14.3 and §15.3: Ultra − High is 1.8–10.2 ms at 1440p over 21 runs, and 1.8–3.0 ms in the 8 quiet runs. Red chose High at every level (D1 (b)) at 12:36; it is applied in the clone.)* What Ultra buys:
- traced AA and 8 instead of 6 shadowed lamps;
- crawl 0.014 vs 0.014 (B6), i.e. no difference;
- carpet parity 3.2 % vs 3.3 %;
- lens footprints about the same.

**Recommendation: High at every level; Ultra only as an explicit setting.** The default lives in `FrontRoomsGlassRTSystem.DefaultQuality()`, a visual file.

**D6.** At the break pose the RT costs 6.8–10 ms at 1440p on High in the player *(corrected, §15.3: 4.7–13.1 ms over 21 runs, and 4.7–5.0 ms in the quiet ones; Red chose (a) now and (b) with P2)*, and up to 18 ms in single editor frames looking down at a lit ceiling. Office windows cost about 3× a Level 0 window. The options, with numbers:
- **(a)** Accept it. The whole frame is ~11.6–14 ms at 1440p High on this M3 Max, even under load.
- **(b)** A shot budget of 4 lamps during the hold: −56 % trace (5.85 → 2.55 ms at 1080p).
- **(c)** Drop Ultra → High for the shot. This matters only if D1 stays Ultra.

**Recommendation: D1 = High now, and (b) once P2's per-hit lamp cull lands.** One caveat: the shot is cheap at S2 today only because the S2 pieces are not registered (G14V-1). Once they are registered, S2 costs about what S0 costs. *(Corrected: G14V-1 has been fixed since fix round 1 (§8, §11): the S1 and S2 pieces are registered, so the S2 beat costs about what S0 costs. The current shot cost is §15.3 / §16.2: 2.78–5.31 ms at 1080p High over 21 runs.)*

### 5.3 D2: the player has no body in dark windows

A dark-beyond window becomes a readable mirror (`30v_D2_dark_window.jpg`). With RT High, the room behind, its lamps and the Relay copy 2 m behind show in the pane, but nothing stands where the player stands, because the camera has no body in the reflection rays. In a lit-to-lit window the 8 % pane reflection hides this. *(Corrected in §14.1 R3-T2 / R3-T6: it does not hide the Relay: in a lit window the Relay 2 m behind reads as a ghost where the player's reflection should be, −2.1/255 over its 44,003-px silhouette, p5 −8; art review 3: −5.7/255, p5 −24 over the 32,820 px that visibly change.)* In a dark window at 1.5 m head-on, the empty spot is plain once you know it is missing. The Relay itself reads only faintly in the final frame: its body is −1.5/255 against RT Off, and its boots read best. *(Corrected in §14.1 R3-T6: the −1.5/255 was averaged over a hit-id mask of 278,560 px, 64 % of the glass; over its silhouette the Relay reads −2.4/255 against the same view without it, p5 −12, and −9.0/255 against RT Off.)*

This is Red's call (D2). A body proxy in reflections (a capsule or the planned first-person body, reflection-only, on the Dynamic mask) costs about one more dynamic instance per frame. Leaving it out keeps the "vampire" look. *(Decided (b), §15: Red chose the reflection-only body proxy at 12:36, for lit and dark windows, built in the `g14-followup` package; VL316 and VL284 are checked and signed off on that build.)*

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
   - **Which metric each bar uses:** B8 is read on the **added frame time** (whole-frame GPU median, RT − Off, interleaved). The stage spans overlap the opaque raster, so they are larger than the added time *(wording corrected in §14.3: no figure here is a bound for this machine)* (§11.4).
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
| v5 0.55 m (pane fills the frame), 1080p | 2.64 / 4.44 → **2.86 / 4.68** | 4.07 / 5.85 → **4.28 / 6.16** | the shot pose vs the +2 ms target | over (+0.86; *corrected in §14.3: 3.1–5.3 ms across runs; current, §15.3: 2.78–5.31 ms over 21 runs, 2.78–2.96 in the quiet runs, over +2 by 0.8–3.3*) |
| v5 0.55 m, 1440p | 4.50 / 7.07 → **4.79 / 7.56** | 7.03 / 9.63 → **7.44 / 10.25** | High 5, Ultra 7 | ~~PASS~~ **FAIL** / FAIL *(corrected in §14.3: High 6.0–13.1 ms across runs; current, §15.3: 4.72–13.05 ms over 21 runs and 4.72–4.99 in the quiet runs, so High is at the bar when the machine is quiet and over it in disturbed runs; 4.79 lies inside the quiet range)* |

- **Which metric each bar uses.** The design never said. This report now reads B8 on the **added frame time**, because that is what the player waits for: the RT compute overlaps the opaque raster that does not depend on it. The stage spans are larger than the added time. In quiet minutes the added time is 55–65 % of the span. The +0.76 ms "whole-frame" figure from `ps6` (load 350–430) was load noise, not overlap.
- **This round's own GPU cost** (*corrected in round 2*): at 1440p +0.30 to +0.49 ms of added frame time (v2 High +0.47, Ultra +0.42; v5 High +0.30, Ultra +0.49), and +0.38 to +0.62 ms of stage span (v5 Ultra +0.62). Most of it is the 25-tap mean normal in the trace (+0.3 ms) and the smoothed blur radius (+0.1 ms). That is the price of AD-1.
- **D1, default quality.** Every standalone Mac player starts at quality level 5, so the app runs **Ultra**. That includes the store archive, whose smoke reports "quality level 5 (Ultra)". Ultra costs **+2.0 ms (v2) and +2.8 ms (v5) at 1440p** over High *(corrected in §14.3: 2.0–10.2 ms at 1440p across runs; current, §15.3: 1.77–10.17 ms over 21 runs, 1.77–3.01 in the quiet runs; Red chose D1 (b), now in the package)*, and buys nothing measurable: crawl 0.014 vs 0.014, carpet parity 3.2 vs 3.3 %, lens footprint within 1 point. **Recommendation unchanged: High at every level; Ultra only as an explicit setting** (`DefaultQuality()`, a visual file, one line).
- **D6, break-shot budget.** At the shot pose (v5 1080p) RT adds **2.86 ms on High** against the plan's +2 ms. Now that the S1/S2 meshes are registered, the S2 beat costs what S0 costs; it no longer gets cheaper by losing the reflection. Options:
  - **(a)** Accept +0.9 ms over the target for the 1.65 s hold.
  - **(b)** A 4-lamp shot budget: the ablation in §5.1 cut the trace by 56 %, which would put the shot at about +1.6 ms (ESTIMATE). *(Corrected in §16.2: from the 21-run shot range 2.78–5.31 ms the estimate is +1.6 to +3.5 ms at 1080p; Red chose (a) now and (b) with P2.)*
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
| PRINT-1 (new) | look | Main's print driver (landed 10:20) moves the wallpaper print between 8 keyframes (holds of 45–90 s, 1.2 s blends) and a subliminal drift, through globals `_FR_Print` (Texture2DArray), `_FR_PrintClock`, `_FR_PrintWarp`. G14's hit shader still shades the static `_PrintTex`, which is keyframe K00, so after the first jump a reflected wall shows K00 while the wall itself shows another keyframe. Fix: bind the array and the clock in the material/frame records and sample the same slice and warp (the driver holds K00 whenever a cue can start, so cue frames match). Not a landing blocker: the mismatch is a print pattern change at the reflection's 8 % strength. *(Review 3, art run a3: cutting the print to K4 changes the walls by 6.0/255 mean, p95 15.9, while the traced radiance changes by 0.55: the reflection stays on K00. The stale shader comment is fixed in fix round 3, §14.1 R3-T5.)* | glass-rt-track, contract with the wallpaper-motion line (动画和动态图形) |
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

---

## 13. Package stage after review round 3 (2026-10-22 12:19): BLOCKED

This is the workflow's package stage (游戏视觉 workflow, session `local_a9a1dce2`, account B). It is BLOCKED because the engineering lens of review round 3 is NOT READY on correctness defects in the payload code. I re-read the code behind each defect in `W/apply/glass_rt_g14_payload/` and in main myself, and all three are real (§13.1).

What this stage did:
- One plain dry run on main at 12:18: DRY RUN OK, every base matches (`W/logs/package_g14/dry_main_1218.log`). Main is at `c462951a`, and its working tree changes nothing in the package paths.
- It wrote this section and the banner at the top of this file.

What it did **not** do:
- It did not refresh `W/apply/glass_rt_g14*`. That package is still the round-2 package with dylib `ec31f4fa`, and it must not be applied.
- It did not touch `40_for_red.md` or `W/vl_pending/glass_rt_g14.md`.
- It did not back the package up into `_retired`.
- It did not run Unity. The clone `W/proj_rt_eng3` is left in the round-2 package state; per the art round-3 check at 12:14, it matches the payload sha1 for sha1.
- No git command, nothing downloaded.

Load at 12:18: 1-minute average 65.

The sources for this section are the round-3 results in the workflow journal (engineering `aafe6450`, art `a5abfb0d`). Neither stage wrote a `RESULT.md`; their locks are `W/rt_work/review3_eng/OWNER.lockdir`, `W/rt_work/rev3_art/OWNER.lockdir`. The art evidence sheets are in `W/rt_work/rev3_art/sheets/`.

### 13.1 Blockers in the payload code (fix in the clone, re-verify, re-package)

| ID | Severity | Defect (checked in the payload by this stage) | Fix | Test that must pass |
|---|---|---|---|---|
| R3-B1 (eng R3 #1) | major | **A stale held gate keeps the burst BLAS budget on for the rest of the session.** `FrontRoomsGlassRTSystem.cs` `ReleaseCamera` (l. 349–355) removes the camera and frees its native slot, but never removes `gates[state.id]`. Camera ids are never reused (`nextCameraId++`). `UpdateBlasBurst` (l. 2030) turns the burst on while *any* gate is held, and it runs only from `GateWeight`. `ResetStatics` runs only at SubsystemRegistration and at Play exit. A new camera's gate starts held and opens only on one of its own traced frames that shows glass (frames without glass do not count, not even towards the timeout). The desktop restart (`FrontRooms3DGame.cs:2434`, `SceneManager.LoadScene` from pause or caught) destroys the camera. If its gate is held at that moment (a restart before the first window is in view, or within 8 frames of a near birth), that gate stays held forever, so `FRGlassRT_SetBuildBudget(120000, 48)` stays on instead of the plugin default (10k triangles / 8 builds), and 48 m streaming builds 12× the triangles per event. No harness restarts the scene with RT on | In `ReleaseCamera`: `gates.Remove(state.id)`, then recompute the burst. Make `UpdateBlasBurst` callable from there, and call it on quality Off and on fail-closed too | A new **restart** phase in the jump harness: `SceneManager.LoadScene` while the gate is held, and again while it is open. Assert that the burst is off once the new camera opens, that the gate count equals the live cameras, and that walk GPU AS time is unchanged against `r8b` |
| R3-B2 (eng R3 #3) | moderate | **Every direct child of the map root counts as a chunk.** `DiffChunks` (l. 1689–1711) makes a `ChunkRec` for any child of `mapRoot`. Main's `FrontRoomsMapWorld.TakeKey` re-parents a presented key to the map root (`FrontRoomsMapWorld.cs:3807`). The key is within 30 m, so it is a "near birth": on every presented key pickup the gate drops the reflection to the zone cube for about 8 frames, waits about 3 for coverage, then fades back over 12. When the key is destroyed, `DropChunk` (l. 1721) removes its rounded coordinate from the plain `HashSet` `chunkCoords`. That coordinate is usually a real neighbouring chunk, which `CountNearUnbuilt` then counts as unbuilt for good (every later jump near it holds the gate). Live today in the editor with the placeholder key (`ShowPlaceholderKey`); live in players as soon as a dressed key (`IFrontRoomsKeyRig`) lands | Count only real chunk roots (name prefix `Chunk `, or a root exactly on the 24 m grid), and reference-count `chunkCoords`. The reference count also fixes `RebuildChunk` (Level Designer `ReplaceModule`), which drops the old root after the new one is added | A **key pickup** phase with the placeholder key: 0 near births and 0 holds from the pickup; `NearUnbuilt` is 0 after the key is destroyed; and a Level Designer `ReplaceModule` leaves the coordinate counted as built |
| R3-B3 (eng R3 #4) | minor (same rebuild) | **The coverage slot is not a correct seqlock.** In `FrontRoomsMetalGlassRT.mm`, `PublishCoverage` (l. 194–202) stores −1 and the data with relaxed order and no release fence. `FRGlassRT_GetCameraCoverage` (l. 911–920) has no acquire fence before its second frame read. On arm64 the gate can then read the glass and miss counts of one frame under another frame's number | A release fence after the −1 store and an acquire fence before the re-check (or seq_cst throughout). Rebuild the universal dylib with the repo script; the validator must pass for both slices, with no downloads. The new dylib sha replaces `ec31f4fa` everywhere | Validator both slices; `o1`-style old-plugin check; `ps12` / `ps13` players (native and Rosetta) as in §12 |

The R3-B3 dylib rebuild also carries the comment fix of R3-T5 (the shader source is embedded in the dylib).

### 13.2 Must be fixed before landing: text and evidence (no payload change)

| ID | From | What is wrong | Fix |
|---|---|---|---|
| R3-T1 | eng R3 #2 | **`40_for_red.md` understates the cost.** It quotes single values from fix31's quietest run and calls them "upper bounds": l. 7 and 22; l. 49, where D1 (b) saves "1.4–2.8 ms"; l. 64, "4.79 ms at 1440p (bar 5 ms: within)". This file repeats them at l. 558: v5 1440p High **PASS**. The same harness gives very different numbers (table below). | Quote ranges and drop "upper bounds" (run-to-run spread is about ±40 %). v5 1440p High is **FAIL** (4.8–8.2 ms against a 5 ms bar). The 1080p break shot is +1.4–1.6 ms over its +2 ms target, not +0.9. D1 (b) saves up to 4.7 ms. Redo the D1 projection table (base M3/M4 at 1440p High rises to roughly 18–34 ms) |
| R3-T2 | art R3 #3 | **D2 text is wrong for lit windows.** `40_for_red.md:81` says that in a lit window "the missing body hardly shows". A body-sized figure 2 m behind reads at −5.7/255 mean, p5 −24 (`rev3_art/a3_out` TW_relay2m against the same pose without it, 32,820 px). Art review 2 measured −8.9/255, p5 −36. Head-on, the Relay's ghost sits exactly where the player's own reflection should hide it | Rewrite D2's "What the player sees" so that D2 covers lit windows too, before Red decides |
| R3-T3 | art R3 #1 | **Red's sign-off sheets show the pre-fix build.** `images/30v_AD6_showcase.jpg` (VL284) and `30v_D2_dark_window.jpg` (VL316) are run `r3a` frames with dylib `518d6769`, from before both fix rounds. Their numbers are stale: "19 specks" is now 0, "lens crawl 0.09" is now 0.104. The showcase keeps a hand-drawn title, caption and yellow tile labels under the Shot header. Neither sheet has RT Off tiles | Re-compose both with `shotframe grid` from `W/rt_work/fix32/r8a_out` (o45/o60 High+Off, darkrelay High/Off/radiance, moving f30, the back image, dark-beyond v2 Off/High) plus `W/rt_work/rev3_art/a3_out`. `r8a` has no HDR-off tile: re-run that phase, or label the old tile with its build. Update the VL284 / VL316 rows |
| R3-T4 | art R3 #2 | **VL283 claims "no straight-edged wedge" in the v5 radiance. The landing build has one.** `r8a_out/v5_close55_rt_high.png` has a straight diagonal step from the pillar base with a stippled shadow-ray edge: +21 % linear in about 4 px at (752,354), where the planar mirror shows −3.6 %, and +8.5 % at (819,342). That fails AD-2's bar (no straight-edged step above 3 %). It hides at the 8 % lit-window strength in stills, but the stipple will shimmer when accumulation resets | Correct the VL283 row and keep it PARTIAL. AD-2 stays open (glass-rt-track: per-hit ranked soft shadows) |
| R3-T5 | art R3 #4 | **A stale comment in the payload.** `FrontRoomsGlassRT.metal:264–266` says the live print flipbook "has no runtime driver yet". That is false since main's 10:20 print driver: `a3` logged the print clock live (w 1.00). Cutting the print to K4 changes the walls by 6.0/255 mean (p95 15.9), while the traced radiance changes by 0.55, because the reflection stays on K00 | Fix the comment in the R3-B3 rebuild (or with PRINT-1). Add these numbers to §12.5 PRINT-1. Not a landing blocker in itself: the reflection is about 8–11 % strong, and the zone cube does not follow the print either |
| R3-T6 | art R3 #5 | **Weak Relay evidence.** The "−1.5/255 over its body" figure for the Relay in a dark window (VL284, VL316, §5.3, `r8a` DARK RELAY) is averaged over 278,560 px: 64 % of the 437,356-px glass and several times the Relay's silhouette. The DL_a_relay2m pose puts the Relay copy behind a wall (the radiance differs in only 573 px) | Measure over the Relay's silhouette mask. Stop citing DL_a_relay2m as dead-lamp-window evidence |

**Added frame time on the M3 Max player.** Whole-frame GPU median, RT minus Off, 16 interleaved rounds, in ms. This stage recomputed every value from each run's `player_smoke.txt`. All runs were UNDER LOAD (31–44).

| Pose | fix31 build: fix31 `ab2_new` / fix32 `ab2_old` / `ab1_old` | shipped round-2 build: fix32 `ab2_new` / `ab1_new` | bar |
|---|---|---|---|
| v2 1.5 m, 1080p, High | 1.28 / 1.87 / 1.78 | 1.96 / 2.15 | — |
| v2 1.5 m, 1440p, High | 3.47 / 4.86 / 5.54 | 4.58 / 4.54 | 2.0 → FAIL |
| v5 0.55 m, 1080p, High | 2.86 / 3.36 / 4.18 | 3.37 / 3.56 | +2 shot target → over by 1.4–1.6 |
| v5 0.55 m, 1440p, High | 4.79 / 6.70 / 8.16 | **6.05 / 6.82** | 5 → **FAIL** |
| v5 0.55 m, 1440p, Ultra | 7.56 / 10.62 / 14.80 | 10.15 / 11.54 | 7 → FAIL |
| Ultra − High (D1 (b) saving), shipped build | | v2 1080p 1.37–1.59, v2 1440p 2.44–3.29, v5 1080p 2.06–2.49, v5 1440p 4.11–4.71 | |

fix31 `ab1_new` (v5 High +8.11 / +9.19) is left out as a disturbed run.

### 13.3 Landing conditions (Red / the 游戏视觉 session, not code)

- **R3-L1 (eng R3 #5), default quality.**
  - The package keeps Ultra at quality level ≥ 5 (`FrontRoomsGlassRTSystem.cs:184`), which is the option this report recommends against.
  - The App Store · 构建 line is waiting for this dylib. If the package lands before D1 is answered, every M3/M4 store build ships Ultra: +7.0–11.5 ms at 1440p on this M3 Max (UNDER LOAD).
  - Either land with D1 answered, or hold store and snapshot builds from main until it is.
- **R3-L2 (eng R3 #5), minimum macOS.**
  - The app's `macOSTargetOSVersion` is 12.0. Both slices of `ec31f4fa` have `LC_BUILD_VERSION` minos 13.0 (checked with `otool` by this stage; the build script passes `-mmacosx-version-min=13.0`). Main's current arm64-only dylib `56a35164` is also 13.0.
  - Before RD2, the universal app left the dylib out (ARM64 importer). This mismatch therefore reaches store validation for the first time.
  - On macOS 12 the plugin would not load. The fail-closed path should turn RT Off; that is untested.
  - Options:
    - (a) build the plugin with minimum 12.0. RT itself is already gated at runtime (`@available(macOS 14.0)` caps). Needs a check that no 13+ API is used unguarded.
    - (b) raise the app's minimum to 13.0. That is ProjectSettings, not visual-owned: Red's call with App Store · 构建.
  - Recommendation: (a), in the same rebuild as R3-B3.

### 13.4 Still holding (round-3 reviews, not re-run here)

- Dry run OK on `c462951a`, plain and `--with-deletions`: 20 bases, 13 keeps, 3 deletions.
- The clone equals the payload on all 20 paths.
- Roslyn `--all` on main + the 8 payload `.cs` files: 0 errors, macOS and iOS defines (`W/rt_work/review3_eng/r3_main_{mac,ios}`).
- `ec31f4fa`: x86_64 + arm64, 25 exports in each slice, ad-hoc signed per slice. It rebuilds byte-identical from the payload, and the layout self-test passes on both slices.
- The `.meta` GUID `9b3bca94…` is kept, with AnyCPU.
- `ps12` / `ps13`: `on: Apple9`, 38 of 38 slabs, walk 0 blank, break → 0 px, 0 errors.
- `o1`: an old plugin with the new scripts turns RT Off.
- `iso6`: 0 `FRGlassRT_` in the WebGL, iOS and Win64 assemblies.
- Fail-closed: 6 of 6.
- Jump and walk TSV numbers hold.
- Package scratch tests: 13 of 13 PASS.
- Art, from its own `a3` run:
  - coverage: 21 poses at weight 1.000, 0 misses, 86–88 lamps;
  - red tint ≤ 0.020 %, colour shift ≤ 4.3/255;
  - the dead lamp goes out in the reflection;
  - GD3 S1/S2 are traced;
  - stream-in: 0 shown frames with misses above 0.5 %;
  - AD-1: 0 holes.

### 13.5 Next (fix round 3, then review, then package)

1. Fix the code in `W/proj_rt_eng3`: one Unity at a time, at most 2 on the visual line. Re-check that the clone matches the payload and main's bases first.
   - R3-B1 and R3-B2 in `FrontRoomsGlassRTSystem.cs`.
   - R3-B3 + R3-T5 + R3-L2 (a) in the plugin sources and the build script, then the universal rebuild.
2. Re-run `r8a`, `r8b` (with the new restart and key-pickup phases), `iso`, `o1`, `ps12` / `ps13`, and the player A/B. Roslyn `--all` macOS + iOS.
3. Fix R3-T1–T4 and R3-T6 in `40_for_red.md`, this file and `W/vl_pending/glass_rt_g14.md`. Re-compose the VL284 / VL316 sheets with `shotframe`.
4. Re-package in the v2 convention. Back the current package up into `W/apply/_retired/glass_rt_g14_20261014/`. Record the bases from main at that moment, run the `APPLY_REAL` scratch test, then the dry run on main.

---

## 14. Fix round 3 (review round 3: engineering and art NOT READY)

2026-10-22 12:25–13:2x · 游戏视觉 · workflow stage "G14 fix round 3" (session `local_a9a1dce2`, account B) · **verdict: the three code blockers of review round 3 (R3-B1, R3-B2, R3-B3) are fixed in the clone and pass their new tests; R3-L2 (a) is done (the plugin is built for macOS 12.0, with every newer API behind an availability guard the compiler enforces); R3-T1 to R3-T6 are corrected in this file, `40_for_red.md`, the VL rows and two re-composed sheets. The package was NOT refreshed: it is still the round-2 package (dylib `ec31f4fa`) and must not be applied. Next: review round 4 on the clone, then the package stage (§13.5 step 4).**

*(Review round 4 corrected parts of this section, marked in place. §15 is current.)*

Clone `W/proj_rt_eng3`, held by `W/rt_work/fix33/OWNER.lockdir`. At 12:2x the clone matched the package payload on all 20 paths (sha1 for sha1), and the plain dry run on main passed (every base matches, `W/logs/fix33/dry_main_start.log`; main HEAD `c462951a`). The clone was not re-synced: against main's working tree it differs only in the package paths, the 3 optional deletions, the 12:23 sound landing (`FrontRoomsSoundDirector/SoundIds/Fmod.cs`) and the StreamingAssets FMOD banks, none of which G14 touches. Every timing is **UNDER LOAD** (1-minute load 30–79 for the editor runs and 45–155 for the player A/B (279 at the stage start), `W/logs/fix33/load.tsv`).

Written in Red's project: this section, the banner at the top, correction marks in §0 item 5, §2 (the load row), §5.3, §11.0, §11.4 and §12.5, `40_for_red.md` (rewritten), the re-composed `images/30v_AD6_showcase.jpg` (VL284) and `images/30v_D2_dark_window.jpg` (VL316) (the 11:0x versions moved to `W/_partial/fix33_vl_sheets_prev/`, this round's first drafts to `W/_partial/fix33_vl_sheets_draft1312/`; copies of the 12:20 `30_final.md` and the 11:16 `40_for_red.md` in `W/_partial/fix33_docs_prev/`), and the new `images/30v_F33_restart_key.jpg` (VL281). Claimed in `LANDINGS.md`. No Unity, batch, build or render ran on main. No git write command. Nothing was downloaded.

### 14.0 Short answer (for Red)

1. **A restart no longer leaves the loading budget stuck on.** Before, restarting a run while a window's reflection was still loading left the 12× BLAS budget (120k triangles / 48 builds per event) on for the rest of the session. Now a destroyed camera takes its gate with it. Tested with two scene reloads (one while the gate was held, one while it was open): the new camera's reflection opens after 23 frames, then 0 of 120 frames have the burst on, the plugin's budget is back to 10k / 8, and there are no orphan gates. A walk in the restarted session costs the same GPU AS time as the first walk.
2. **A presented key no longer counts as a map chunk.** Picking up the placeholder key 15 m from the camera: 0 near births, 0 gate holds, 117 traced frames all at weight 1 (no drop to the zone cube), and after the key is destroyed no built square within 60 m is counted as unbuilt. A chunk rebuild (the Level Designer's path) keeps its square counted.
3. **The coverage hand-over between the GPU's completion threads and the main thread is a proper seqlock.** A stress test of round 2's protocol on this Mac accepted 2,511,987 mixed readings in 10 s; the new one accepted 0.
4. **The plugin declares macOS 12.0** (was 13.0) on both slices, matching the app; it loads and stays Off on macOS 12/13. New dylib `fa0232d2`.
5. *(Corrected in §15.3: the ranges are now over all 21 runs of the landing GPU code, with the quiet runs shown and the left-out runs named. v5 1440p High is 4.7–13.1 ms: at the bar when the machine is quiet (4.7–5.0), over it in disturbed runs. So by the rule below it is not a clean FAIL.)* **The cost text is corrected** (§14.3): ranges over runs, FAIL where the range is over the bar (v5 1440p High is a FAIL: 6.0–13.1 ms over the 13 runs of the landing build, 4.8–8.2 ms over the fix31 build's runs, against 5 ms). `40_for_red.md` is rewritten on these numbers, and D2 now covers lit windows.

### 14.1 Every review-3 item and what was done

| ID | What was done (clone `W/proj_rt_eng3`, file:line) | Test (fresh runs, UNDER LOAD) | Verdict |
|---|---|---|---|
| **R3-B1** stale held gate keeps the burst BLAS budget on | `FrontRoomsGlassRTSystem.cs`: `ReleaseCamera` (l. 358–369) removes `gates[state.id]` and calls `UpdateBlasBurst()`; `UpdateBlasBurst` (l. 2097) is now `internal`, also called from the `Quality` setter when it is set Off (l. 144) and from `FailClosed` (l. 206). It counts a held gate only while quality is not Off, nothing failed closed, its camera traced within `GateStaleFrames` (30, l. 2087) and its area has not been quiet for more than `GateBurstQuietFrames` (90, l. 2090: a camera that only waits for its first glass frame, e.g. after a run start in a corridor without windows, needs no burst); the native call is guarded. Harness accessors `BlasBurst`, `GateCount`, `LiveCameras`, `OrphanGates`. Plugin: two new stats `StStreamTriBudget` / `StStreamBuildBudget` report the budget in force (`FrontRoomsMetalGlassRT.mm` l. 75, set by `FRGlassRT_SetBuildBudget` and at load; C# `FrontRoomsGlassRTNative.cs` l. 114) | New **restart** phase (`FrontRoomsGlassRTVerify.cs` `Restart` l. 3265, `StartRun` l. 3231), run `r9b`: **R1** far jump into unbuilt map (gate held, burst on, native budget 120000 / 48), then the game's R restart (restart flag + `SceneManager.LoadScene`): after the reload 0 gates, 0 orphans, burst off, budget 10000 / 8; the game restarted itself (seed 1218941869); new camera 2 held 23 frames (burst on in 12 of them: its own loading), opened at +23; over the next 120 frames **burst 0, budget off the default 0, orphan gates 0, gates ≤ live cameras always** (1 / 1). **R2** the same with the gate open (title path, harness seed 516574485): new camera 3, open at +23, **burst 0, budget off default 0, orphans 0**. **walk2** in the restarted session vs the first walk (same seed and path): GPU AS on traced frames p50 1.975 → 2.035 ms, p95 3.426 → 2.777, mean 2.103 → 2.006; BLAS builds per event max 8 → 8, native budget off the default 0 → 0 frames, burst flag 0 → 0 frames, BLANK 0 → 0 | **FIXED** |
| **R3-B2** every child of the map root counts as a chunk; `chunkCoords` a plain set | `FrontRoomsGlassRTSystem.cs`: `IsChunkRoot` (l. 1716) accepts only the map's own chunk roots: local position exactly on the 24 m grid with y = 0 **and** a name starting with `Chunk ` (MapWorld.BuildInto, `FrontRoomsMapWorld.cs:1156–1158`, read-only); a presented key (`TakeKey`, `:3807`) and the edit-mode capture eye (`:830`) are not chunks. Each child is classified once (its id is remembered; `Transform.name` allocates). `DiffChunks` (l. 1724) compares the records with the chunk roots it saw (l. 1757), not with every child, so a non-chunk child can no longer hide a dropped chunk. `chunkCoords` is a reference count (`Dictionary<long, int>`, l. 1919; `AddCoord` / `RemoveCoord` l. 1768–1773; `DropChunk` l. 1779), so `RebuildChunk` / `ReplaceModule` (new root before the old one leaves) keeps its square; `OnMapLost` clears the counts (l. 1681). Harness accessors `ChunkCoordRefs`, `NonChunkRoots` | New **keypickup** phase (`KeyPickup` l. 3124), run `r9b`, at an intact Level 0 window (284,206)-(284,207), v2 1.5 m: the nearest lying key (zone (35,25), 15.0 m from the camera, inside the 30 m gate radius) taken through MapWorld's own `TakeKey` with the editor placeholder key on, so the door shot presents it and MapWorld re-parents it to the map root: **key at the map root for 56 frames, destroyed at +56; near births 0, gate holds 0, 117 traced frames with glass, min weight 1.000, 0 frames below weight 1; chunks 25 → 25; 1 non-chunk child seen (the key); NearUnbuilt max 0; built squares within 60 m counted as unbuilt: 0**; the square the round-2 code would have dropped with the key, (36,25), keeps 1 reference. **RebuildChunk** of the neighbour chunk (36,25), 24.8 m away: square references 1 before, 1 after (round 2: 0), NearUnbuilt 0, 0 built squares uncounted; the rebuild itself is a near birth and holds the gate 11 frames, as designed | **FIXED** |
| **R3-B3** the coverage slot is not a seqlock | `FrontRoomsMetalGlassRT.mm`: per-slot sequence counter `g_CovSeq` (l. 197–219): writer `WriteCoverageSlot` (l. 204) sets the counter odd (relaxed) + `atomic_thread_fence(release)` (l. 209), stores the data, then even (release); writers (completion handlers, `FreeCamera`, `ResetScene`) serialized by a mutex the reader never takes. Reader `FRGlassRT_GetCameraCoverage` (l. 943): counter (acquire), data, `atomic_thread_fence(acquire)` (l. 953), counter again; valid only when equal and even. The frame number is no longer the sequence (two renders of one camera in one Unity frame share it). `FreeCamera` (l. 1022) and `ResetScene` clear through the writer | Stress test outside Unity (`W/rt_work/fix33/tools/seqlock_stress.cpp`, the two protocols copied, 10 s each, writer thread vs reader thread): **arm64 native: round 2 accepted 2,511,987 torn readings of 88,210,017 valid; round 3: 0 of 3,780,048. x86_64 under Rosetta 2 (TSO): 0 and 0.** Validator: both slices OK (pipelines + layout self-test, arm64 native and x86_64 under Rosetta). The gate still opens on its own coverage reading: jumps J1/J2/J3 shown at +25/+33/+23 frames, 0 shown frames with holes (as round 2) | **FIXED** |
| **R3-T5** stale "no runtime driver yet" comment | `FrontRoomsGlassRT.metal` l. 264–268 now says the raster shades the live flipbook since main's 10:20 print driver while the hit shader reads K00 (PRINT-1). In the same rebuild (the MSL is embedded in the dylib) | — | **FIXED** |
| **R3-L2 (a)** dylib minos 13.0 vs app 12.0 | `build_frontrooms_metal_glass_rt.sh` l. 78–79: `-mmacosx-version-min=12.0` for both slices and `-Werror=unguarded-availability -Werror=unguarded-availability-new` (was `-Wno-unguarded-availability-new`). The round-2 sources at 12.0 gave 23 availability diagnostics per slice (macOS 13: `gpuAddress`, `gpuResourceID` / `MTLResourceID`, `setVertexFormat:`, `MTLAccelerationStructurePassDescriptor`, `accelerationStructureCommandEncoderWithDescriptor:`, `MTLLanguageVersion3_0`, `MTLGPUFamilyMetal3`; macOS 14: `MTLAccelerationStructureInstanceDescriptorTypeIndirect`, `MTLIndirectAccelerationStructureInstanceDescriptor`), all reachable only after the runtime CapOS14 gate but invisible to the compiler. Now: `FRGlassRTScene.h` l. 185, 208, 239 mark `Init`, `EncodeAccelerationStructures` and `Describe` `API_AVAILABLE(macos(14.0))`; their call sites sit in `@available(macOS 14.0, *)` (`FrontRoomsMetalGlassRT.mm` l. 241 the MSL 3.0 compile, l. 309 `Init`, l. 543 and l. 607 the encoders); `FRGlassRTScene.mm` l. 201, 325, 348 guard `gpuAddress` / `gpuResourceID` in `WriteSlot`, `SetTexture`, `Reset`; the Metal3 family probe sits in `@available(macOS 13.0, *)` (l. 300). `InitDevice` still returns before any of it without CapOS14, so on macOS 12/13 the dylib loads, reports no CapOS14 and RT stays Off with one status line | Build: 0 diagnostics with the errors on. **`otool -l` LC_BUILD_VERSION: arm64 minos 12.0 (sdk 27.0), x86_64 minos 12.0 (sdk 27.0).** Audit (`W/rt_work/fix33/minos12_audit.md`, `tools/minos_audit.py`): 83 / 84 imported symbols per slice; strong imports needing macOS > 12.0: **0**; weak: `MTLResidencySetDescriptor` (15.0) and `MTLAccelerationStructurePassDescriptor` (13.0), both used only under their guards, plus `__availability_version_check`; the 10 sent selectors that need macOS 13+ on every class that declares them (`accelerationStructureCommandEncoderWithDescriptor:`, `gpuAddress`, `gpuResourceID`, `setVertexFormat:`, the residency-set ones) each sit in an `@available` block or an `API_AVAILABLE(macos(14.0))` function; receiver-dependent selectors are checked per receiver by the compiler (0 diagnostics); libc++ availability markup enforced at 12.0 (0 errors). The player built this round: app `LSMinimumSystemVersion` 12.0, app binary minos 12.0 both slices, `UnityPlayer.dylib` 12.0, FMOD bundles 11.0–11.5, G14 plugin 12.0. **Not run on macOS 12** (no such Mac or VM here): the macOS 12 load is a static result | **DONE** (static) |
| **Universal rebuild** | `NativePlugin/build_frontrooms_metal_glass_rt.sh` (repo script) | `W/logs/fix33/plugin_build.log`: validator OK on arm64 and on x86_64 under Rosetta 2 (all 9 pipelines, layout self-test OK: FRInstance 128, FRMaterial 256, FRLamp 64, FRMeshGPU 48, FRFrame 512, FRVertexAttr 24); `lipo` x86_64 + arm64; ad-hoc signed per slice (arm64 linker-signed, x86_64 `codesign -s -`); 25 `FRGlassRT_` exports per slice (unchanged); **`fa0232d2ecd5f91c70476ea9532ec889464365dd`, 499,712 B**; slices arm64 `efece4c3` (UUID 77BA3A94), x86_64 `ddc76f16` (UUID 7D0F35D5); rebuilt from a fresh copy of `NativePlugin/` with the same script: byte-identical (`fa0232d2`, `W/rt_work/fix33/repro/`). ABI stays 32 (no struct or export signature changed) | **PASS** |
| **R3-T1** cost understated, "upper bounds" | §14.3 below; correction marks in §0 item 5, §11.0, §11.4; `40_for_red.md` rewritten: every timing a range over runs, FAIL where over the bar, no "upper bound", the D1 projection table recomputed from the ranges, D1 (b) saving as the measured Ultra − High range | — | **FIXED** (text) |
| **R3-T2** D2 says the body hardly shows in lit windows | `40_for_red.md` D2 rewritten for dark AND lit windows, with this round's silhouette numbers and art reviews 2 and 3; §5.3 marked | — | **FIXED** (text) |
| **R3-T3** sign-off sheets show the pre-fix build | `30v_AD6_showcase.jpg` (VL284) and `30v_D2_dark_window.jpg` (VL316) re-composed with `shotframe grid` from this round's `r9a` (o45 / o60 Off + High, the move f30, the back image, HDR off Off + High: the phase was re-run) and `r9c` (the Relay in a dark and in a lit window: Off, High, radiance, silhouette mask), plus art round 3's `a3` lit-window Relay pair (labelled with its build `ec31f4fa`). Every evidence tile has its RT Off tile beside it; no hand-drawn title, caption or tile label; numbers come from the run's metrics. *(Corrected by review 4: the move and back-image tiles had no Off tile, two tiles were round-2 frames, and two numbers were typed by hand. All six sheets were re-composed in fix round 4, §15.1 A-3/A-4/A-6.)* | — | **FIXED** |
| **R3-T4** VL283 claims no straight-edged wedge | VL283 row corrected and kept **PARTIAL**: the v5 radiance has a straight diagonal step with a stippled shadow-ray edge. Art review 3: +21 % in about 4 px at (752,354), planar mirror −3.6 %; +8.5 % at (819,342). This round's 3×3-patch measure at the same points (`tools/wedge_step.py`): High r8a +13.8 / +2.2 %, r9a +6.9 / +6.6 %; Ultra r8a +15.1 / +5.5 %, r9a +14.2 / +5.3 %. The stipple moves between runs (the shadow-ray hash), the step stays; every run is over AD-2's 3 % bar. AD-2 stays open (per-hit ranked soft shadows, glass track) | — | **corrected** (stays PARTIAL) |
| **R3-T6** Relay evidence over 64 % of the glass | The harness now measures the Relay over its silhouette: the px where the reflection rays hit the copy, found by swapping only the copy's albedo to magenta with accumulation off (same geometry and shadows; a px counts only when its traced green drops AND its red + blue rise; other px that changed by > 0.02: 4,008 dark, 4,043 lit) (`RelaySilhouette`, `FrontRoomsGlassRTVerify.cs` l. 2643). Run `r9c`, v2 1.5 m head-on, the copy 2 m behind: **dark window: 44,036 px, final frame with − without the Relay −2.4/255 mean, p5 −12, p95 +19; with the Relay − RT Off −9.0/255, p5 −13.6. Lit window: 44,003 px, −2.1/255 mean, p5 −8, p95 +14; with − Off −5.6/255, p5 −11.5.** The silhouette agrees with the hit-id count of the same short run (45,076 px). Two earlier mask versions this round are superseded: radiance change when the copy is hidden (`r9a`: 126,829 / 98,035 px, it also caught the copy's shadow and the accumulation state) and the magenta swap with an either-channel rule (`r9c_v2a_out`: 70,831 / 50,441 px, shadow-ray stipple on the floor). Why the round-2 figure was wrong: its hit-id mask maps the trace's instance index to the C# submission list, which goes out of step once earlier phases have registered and dropped instances (`r9a`, the full run: 278,560 px, as `r8a`; `r9c`, darkrelay alone: 45,076 px at −9.29/255 against RT Off). `DL_a_relay2m` is no longer cited anywhere as dead-lamp-window evidence (its Relay copy stands behind a wall: 573 px differ) | — | **FIXED** |
| **R3-L1** default Ultra | *(Wrong: Red's note was written at 12:36, and this round missed it. Resolved in fix round 4: D1 (b) is applied, §15.1 E-1.)* Not changed (Red's D1; `NOTE_1022_red_decisions.md` does not exist at 13:2x). `40_for_red.md` D1 now says it blocks the landing in practice: answer D1 first, or hold store and snapshot builds from main until it is | — | **open (Red)** |

### 14.2 The other checks (fresh runs, clone, UNDER LOAD)

| Check | Round 2 (`r8a`/`r8b`, ps12/ps13, o1, iso6) | Round 3 (`r9a`/`r9b`/`r9c`, ps14/ps15, o2, iso7) |
|---|---|---|
| A2 IoU traced / prepass v1–v5, misses | 0.995 / 0.994 / 0.993 / 0.989 / 0.997, 0 % | identical: 0.995 / 0.994 / 0.993 / 0.989 / 0.997, 0 % |
| B3 lamps same frame (changing lenses) | r ≥ 0.998 (7 of 20) | r ≥ 0.998 (7 of 20) |
| B6 strafe crawl High / Ultra | 0.014 / 0.014 | 0.014 / 0.014 |
| Moving lens crawl / reflected floor / raster floor | 0.104 / 0.023 / 0.136 | 0.103 / 0.023 / 0.136 |
| Lens energy RT / mirror o45 / o60 | 0.962 / 0.945 | 0.962 / 0.947 |
| B1 L0 carpet parity mean / p95 (bar 3 / 8 %) | 3.4 / 8.5 % | 3.0 / 7.7 % (the same code: run-to-run range 3.0–3.4 / 7.7–8.5 %) |
| AD-1 dark-relay specks (hit-id mask) | 0 | 0 (r9a), 1 (r9c) |
| HDR-off camera | not in r8a | traces: 437,356 px, 355,132 px changed by RT (r9a) |
| Broken pane | 437,356 → 0 px on the break frame | 437,356 → 0 px |
| Jumps J1 / J2 / J3: shown at, holes, lamps | +25 / +33 / +23, 0 / 0 / 0, 87 / 87 / 86 | +25 / +33 / +23, 0 / 0 / 0, 87 / 87 / 86 |
| Main thread while held after a jump (CPU) | p95 2.35, max 2.93 ms *(J1 only; J1–J3: p95 2.28–2.35, max 2.64–2.93)* | p95 2.48, max 3.35 ms *(corrected, review 4: 2.48 is J1 only; J1–J3 p95 2.26–2.57, max 2.91–3.35)* |
| Walk: holds / burst frames / shown with holes / BLANK | 0 / 0 / 0 / 0 | 0 / 0 / 0 / 0 (walk2: 0 / 0 / 0 / 0) |
| Walk main thread CPU quiet p50 / p99, streaming max | 0.345 / 0.73, 1.65 ms | 0.45 / 0.91, 2.08 ms (walk2 0.41 / 0.86, 1.01). Every stage rose together (lamps +44 %, live +24 %, records +32 %, scene +26 %, GPU total +26 %), and the lamp and record code did not change: load, not code. Range over the four walks of rounds 2–3: quiet p50 0.35–0.45 ms (bar 0.3: FAIL) |
| Fail-closed: BuildFrame entry + 5 entry points | 1 entry, 5 / 5 PASS | 1 entry, 5 / 5 PASS |
| Exit release after Play | released | released (r9a, r9b, r9c) |
| Players: universal app, native (`proc_translated 0`) / Rosetta 2 (`proc_translated 1`) | on: Apple9 High, 38 / 38 slabs, 405,925 px, walk BLANK 0, break → 0, 0 errors | **ps14 / ps15** (`BuildPipeline.BuildPlayer`, URP intact, the plugin `fa0232d2` in `Contents/PlugIns`): caps `0x30ff` both, "plugin loaded", past the ABI check, **on: Apple9, High**, 38 / 38 GD3 slabs on bit 30, 405,925 px traced, the 600-frame walk 0 blank, the break 436,575 → 0 px on the break frame, **0 errors or exceptions** both. As before, no dyld line names the plugin (it is opened by Unity after start-up) |
| Old plugin + new scripts | `o1` (b1c6de26): RT Off, "plugin is an old build" | **`o2` with main's current plugin `56a35164`** (what Red's open editor keeps after the apply): status "off: plugin is an old build: FRGlassRT_GetAbi", one log line, 0 exceptions, Quality Off (requested Ultra), no RT component, over 120 frames 0 plugin events, 0 trace events, 0.0 MB, 0 registrations, `PrepareMeshes` / `Register` → −1 |
| Isolation (strippers) | WebGL / iOS / Win64 0 `FRGlassRT_` | **iso7**: WebGL / iOS / Win64 player scripts 0 `FRGlassRT_` (macOS 25); importer Standalone macOS AnyCPU, editor ARM64, all others off; strip step WebGL prepass 4/4, glass `FRGlassRTPrepass` 4/4, ForwardLit 0/4; iOS prepass 4/4, `_FR_GLASS_RT` 2/4; macOS 0/4 |
| Roslyn `--all` | clone and main + 8 payload `.cs`: 0 errors | **clone: COMPILE OK macOS and iOS defines (7 assemblies, 0 errors); main + the 8 payload `.cs` from the clone as overrides: COMPILE OK macOS and iOS, at 12:50 (`c462951a` + tree) and again at 13:2x (`2c450e11` + tree, after Red's commit and the 13:03 交互设计 landing)**; Unity compile of the clone `c35`: 0 `error CS` |
| Editor scripts ASCII, no tuples | yes | `FrontRoomsGlassRTVerify.cs`: 0 non-ASCII lines, no tuples (the ValueTuple keys of `BuiltChunk.keys` are read by reflection, `GetField("Item1")`) |

### 14.3 Cost (R3-T1): ranges over runs, never single values, never bounds

*(Corrected in fix round 4, §15.3, after review 4 #4 and #5:*
- *The pool is now 21 runs, adding review round 4's eight quiet ABBA runs q1–q8. The 13-run columns below left out the quiet case.*
- *Runs of the same GPU code left out of the pool are now named: fix32 ps12 (native) and ps13 (Rosetta), both disturbed smoke runs; fix33 ps15 (Rosetta), inside the ranges.*
- *The sentence on where the top of each range comes from is reworded: it was wrong for v2 1080p, and the load figures were wrong.*
- *The open item "round 3 vs round 2 not proven" is closed: no round-3 GPU regression.)*

Added frame time = whole-frame GPU median, RT − Off, 16 interleaved rounds per run, in the universal macOS player at 1080p / 1440p targets. Each cell is min–max over the runs of one build. Round 2 and round 3 share the GPU code (fix round 3 changed CPU-side code, an MSL comment and availability guards), so the last column pools them. Run list and per-run values: `W/rt_work/fix33/ab_table.tsv` (`tools/ab_table.py`). Load at the runs: 1-minute load 45–155 (ab1–ab5, 13:03–13:11; `W/logs/fix33/ab.out`); fix32's runs 31–54; ps14 30–34.

| Pose, resolution | fix31 build `b1c6de26` (3 runs) | round 2 `ec31f4fa` (7 runs) | round 3 `fa0232d2` (6 runs) | landing build, pooled (13 runs) | Bar |
|---|---|---|---|---|---|
| v2 1.5 m, 1080p (0.0 = RT and Off medians equal within noise in one run) | High 1.3–1.9 · Ultra 2.7–3.5 | High 0.5–2.1 · Ultra 2.1–3.7 | High 0.0–1.1 · Ultra 1.4–2.5 | High 0.0–2.1 · Ultra 1.4–3.7 | — |
| v2 1.5 m, 1440p | High 3.5–5.5 · Ultra 5.5–8.4 | High 2.1–6.1 · Ultra 4.4–9.6 | High 2.1–5.5 · Ultra 4.5–9.3 | High 2.1–6.1 · Ultra 4.4–9.6 | High 2.0 / Ultra 3.5: **FAIL / FAIL** |
| v5 0.55 m (break-shot pose), 1080p | High 2.9–4.2 · Ultra 4.7–7.1 | High 3.1–4.5 · Ultra 5.2–7.1 | High 3.2–5.3 · Ultra 5.3–9.4 | High 3.1–5.3 · Ultra 5.2–9.4 | shot target +2 (High): **over** |
| v5 0.55 m, 1440p | High 4.8–8.2 · Ultra 7.6–14.8 | High 6.0–11.3 · Ultra 10.2–20.2 | High 6.4–13.1 · Ultra 11.2–22.9 | High 6.0–13.1 · Ultra 10.2–22.9 | High 5 / Ultra 7: **FAIL / FAIL** |
| Ultra − High (D1 (b) saving), per run | | | | 1080p 1.3–4.1 · 1440p 2.0–10.2 | |
| RT-off whole frame, 1440p (scale) | | | | 7.3–18.1 | |

- **Verdicts against B8 (added frame time):** v2 1440p High 2.1–6.1 ms vs 2.0 ms **FAIL**, Ultra 4.4–9.6 ms vs 3.5 **FAIL**; v5 1440p High 6.0–13.1 ms vs 5 ms **FAIL** (it was called PASS in §11.4 from a single quiet run), Ultra 10.2–22.9 ms vs 7 **FAIL**; the shot pose v5 1080p High 3.1–5.3 ms vs the +2 ms target: over by 1.1–3.3 ms.
- **D1 (b) saving (Ultra − High, per run):** 1080p 1.3–4.1 ms, 1440p 2.0–10.2 ms. It was given as "1.4–2.8 ms" from one run.
- **Round 3 vs round 2 in the same minutes (pairs ab1–ab5: ab1–ab3 old first, ab4–ab5 new first):** new − old added frame time High: v2 1080p −1.11 to +0.28, v2 1440p −0.65 to +0.83, v5 1080p −0.02 to +1.12, v5 1440p +0.07 to +2.56 ms (5 of 5 pairs positive at v5 1440p; the reverse-order pairs ab4 and ab5, new first, +0.64 and +0.07). The RT-off frame itself was slower in the round-3 runs as well (v5 1440p Off new − old +0.22 to +8.09 ms), and the GPU code is the same in both builds (the MSL differs by a comment; fix round 3 changed CPU code and availability guards only), so this reads as machine drift between runs, not an RT cost of round 3. It is not proven at this load: review round 4 may re-run the pairs in a quieter minute.
- **Projection for an 8–10-core M3/M4** (×4–5, low end × 4 and high end × 5): 1440p High 8–65 ms, Ultra 17–115 ms of added frame time. The full table is in `40_for_red.md`.

### 14.4 Still open

| ID | Severity | What | Owner |
|---|---|---|---|
| D1 / D6 / D2 | decision | `40_for_red.md` (rewritten this round) *(decided 12:36, recorded in fix round 4: D1 (b) now, (c) in `g14-followup`; D6 (a), (b) with P2; D2 (b) in `g14-followup`)* | Red |
| R3-L1 | landing condition *(resolved in fix round 4: D1 (b) applied, §15.1 E-1)* | the package keeps Ultra at quality level ≥ 5: land with D1 answered, or hold store and snapshot builds from main | Red / 游戏视觉 session |
| Review round 4 | gate | this round's code (R3-B1/B2/B3, R3-L2 (a)) has to pass a review before the package is rebuilt *(done: engineering READY, art NOT READY; fix round 4 in §15; review round 5 next)* | workflow |
| Package | — | *(fix round 4: 10 paths, adding `FrontRoomsGlassRT.cs`; current list with sha1s in §15.5)* still round 2 (`ec31f4fa`); rebuild after review round 4: back up into `W/apply/_retired/glass_rt_g14_20261014/`, re-record the bases, `APPLY_REAL` scratch test, dry run on main (§13.5 step 4). The payload changes: `FrontRoomsGlassRTSystem.cs`, `FrontRoomsGlassRTNative.cs`, `FrontRoomsGlassRTVerify.cs`, `FrontRoomsMetalGlassRT.mm`, `FRGlassRTScene.mm`, `FRGlassRTScene.h`, `FrontRoomsGlassRT.metal`, `build_frontrooms_metal_glass_rt.sh` and the dylib `fa0232d2` (its `.meta` unchanged) | package stage |
| macOS 12 run | minor | the macOS 12 load is a static result (no macOS 12 Mac or VM here) | — |
| AD-2 | look | the v5 straight-edged step (R3-T4) and L0 carpet parity 3.0–3.4 % (bar 3) | glass track |
| PRINT-1 | look | the reflection keeps keyframe K00 while the wall moves: cutting the print to K4 changes the walls by 6.0/255 mean (p95 15.9) and the traced radiance by 0.55 (art review 3) | glass track, with 动画和动态图形 |
| LENS-E, AD-3 head-on, F3 / B8 cost, burst cost, H6, H4, AD-4 | as §12.5 | | as §12.5 |

### 14.5 Files

- Stage folder `W/rt_work/fix33/` (lock `OWNER.lockdir`): runs `r9a_out`, `r9b_out` (restart, key pickup, `keypickup.tsv`, `walk_frames.tsv`, `walk2_frames.tsv`), `r9c_out` (Relay silhouette v2), `r9c_v2a_out` (the superseded either-channel mask), `iso7_out`, `ps14_out`, `ps15_out`, `o2_out`, `ab{1,2,3,4,5}_{old,new}_out`, `ab_table.tsv`, `repro/` (the byte-identical rebuild), `player_universal/` (the round-3 universal player); `minos12_audit.md`, `minos12_probe/` (the round-2 sources' 23 diagnostics, disassembly); round-2 copies of the changed sources (`System.cs.round2`, `Native.cs.round2`, `Verify.cs.round2`, `round2_native/`); Roslyn outputs `roslyn_{clone,main}_{mac,ios}`.
- Tools `W/rt_work/fix33/tools/`: `patch_verify_r33.py`, `patch_verify_r33b.py` (harness), `minos_audit.py`, `seqlock_stress.cpp`, `wedge_step.py`, `ab_table.py`, `compose_f33.py` (sheets), `fill_40.py` (`40_for_red.md`).
- End of the stage (13:2x): main HEAD `2c450e11` (Red's commit); the plain dry run still passes, every base matches (`W/logs/fix33/dry_main_end.log`).
- Logs `W/logs/fix33/`: `chain_fix33.zsh`, `chain.out`, `load.tsv`, `c35`, `r9a`, `r9b`, `r9c`, `iso7`, `pb7`, `o2`, `plugin_build.log`, `ab_players.zsh`, `ab_players_rev.zsh`, `ab.out`, `seqlock_*.txt`, `roslyn_*.log`, `dry_main_start.log`, `dry_main_end.log`.
- Images (Shot format): `30v_AD6_showcase.jpg` (VL284) and `30v_D2_dark_window.jpg` (VL316) re-composed; `30v_F33_restart_key.jpg` (VL281) new.
- VL rows: `W/vl_pending/glass_rt_g14.md` (VL281, VL282, VL283, VL284, VL314, VL316 updated in place; no new number).

---

## 15. Fix round 4 (review round 4: engineering READY with 7 items, art NOT READY with 10)

2026-10-22 13:38–14:1x · 游戏视觉 · workflow stage "G14 fix round 4" (workflow `wf_f882db1c-d69`, session `local_a9a1dce2`, account B).

**Verdict:**
- Red's 12:36 answers to D1, D6 and D2 are recorded, and D1 (b) is applied in the clone: High at every quality level from 3 up, Ultra only when asked for. A universal player at quality level 5 now starts at High, natively and under Rosetta 2.
- Every text and evidence item of review round 4 is fixed, or answered with evidence where the reviewer's premise did not hold (§15.4).
- The harness was fixed and extended: the back-image fit, Off frames for the move, the saved Relay frame, restart and key pickup at a dark-beyond window, and the AD-2 step at a dark-beyond window. The affected checks were re-run.
- The payload now changes 10 paths.
- **The package was NOT refreshed and must not be applied:** review round 5 comes next, then the package stage (§15.5).

**Setup.**
- Clone `W/proj_rt_eng3`, held by `W/rt_work/fix34/OWNER.lockdir`.
- At 13:42 the clone differed from the round-2 payload in exactly the 9 paths of §14.4 (sha1 check). Nothing else had changed.
- Main HEAD was `2c450e11` (Red 12:55) throughout. The plain dry run of the (round-2) package on main at the end still passes: every base matches (`W/logs/fix34/dry_main_end.log`).
- Every timing is **UNDER LOAD**: 1-minute load 33–60 for the Unity runs, 37–41 for the players (`W/logs/fix34/load.tsv`, `chain.out`, `chainb.out`).
- Unity ran only on the clone, one process at a time; the chain started each job only when fewer than 2 visual-line Unity processes ran (`chain_fix34.zsh` wait loop). `W/logs/fix34/load.tsv` (30 s samples, this stage's job included) shows 3 at four samples: 13:48:34 (during `c36`, started 13:48:25 with 1 other running); 13:50:05 and 13:50:35 (during the first `r10b`, started 13:49:45 with 1 other running; that run is void as evidence, §15.4); 13:52:35 (three other sessions' jobs, while the player smoke `ps17` ran, which is outside the cost pool). No counted evidence is affected. *(The 14:10 text named only 13:48:34; completed by the package stage, §16.1 E5-3.)*

**Written in Red's project:**
- this section, the banner, and correction marks in §0 item 8, §5.1, §5.2, §14 (header, §14.0 item 5, §14.1 R3-T3 and R3-L1, §14.2 jump row, §14.3, §14.4);
- `40_for_red.md`, rewritten as the record of Red's decisions;
- re-composed `images/30v_AD6_showcase.jpg` (VL284), `30v_D2_dark_window.jpg` (VL316), `30v_F33_restart_key.jpg` (VL281), `30v_P1_parity.jpg` and `30v_P1_office.jpg` (VL283);
- the new `images/30v_F34_player_cost.jpg` (VL314).

The previous versions of those five sheets, and the stale VL314 sheets `30v_cost_D1_D6.jpg` and `30v_player.jpg`, were moved to `W/_partial/fix34_vl_sheets_prev/` (history). Copies of the 13:20 `30_final.md`, the 13:17 `40_for_red.md` and the VL file are in `W/_partial/fix34_docs_prev/`. The writes are claimed in `LANDINGS.md`. No Unity, batch, build or render ran on main. No git write command was run. Nothing was downloaded.

### 15.0 Short answer (for Red)

1. **Your D1/D6/D2 answers are in.**
   - D1 (b) is applied: a Mac player at quality level 5 now starts at High. Proof: player `ps16` logs "quality level 5 (Ultra) … default RT quality High" and ends "on: Apple9, High" with 0 errors; Rosetta 2 (`ps17`) gives the same.
   - No store build with the G14 dylib goes out until D1 (c) lands in `g14-followup`.
   - D6 (a) needs nothing now. D2 (b) is the `g14-followup` proxy. VL284 and VL316 are signed off on that build.
2. **Cost, corrected again.** Ranges over all 21 player runs of the landing GPU code, with the 8 quiet runs shown on their own:
   - v5 1440p High 4.7–13.1 ms: at the 5 ms bar when the machine is quiet (4.7–5.0), over it in disturbed runs;
   - v2 1440p High 2.1–6.1 (bar 2.0) and every Ultra cell: FAIL;
   - the 1080p break shot: over its +2 ms target by 0.8–3.3 ms;
   - D1 (b) saves 1.8–10.2 ms at 1440p.
3. **The sign-off sheets now show what their text says:**
   - every RT tile has its Off (or image-off) frame beside it;
   - the Relay figures come from saved frames that recompute exactly;
   - the restart and key-pickup tiles sit at a window with a dark room beyond, so a drop to the zone cube would be plain. None happens: 117 traced key frames at weight 1 with 0 misses; after each restart, 121 traced frames with 0 misses.
4. **AD-2 at a dark-beyond window** (new case):
   - the straight-edged step is in the traced light (+13.6 % at (752,354));
   - in the final frame RT adds 0.4 of an 8-bit level across it (29.5 → 29.9/255). *(Reason corrected in §16.1 A5-1: not a 7 % Fresnel scale, which would keep the relative step. The pane's final value here is mostly a base that RT does not set: 27.8/255 at that point with RT Off. Inside the pane the final frame fits 0.0138 + 0.0138 × the traced radiance (linear, R² 0.008), so the radiance carries about 14 % of the final value and its +13.6 % step moves the final frame by about 2 %.)*
   - AD-2 stays open.
5. **The back-image fit was broken** (every run since r3a logged a residual of ~10¹⁰). Fixed:
   - a single 1-px shifted copy explains 9 % of the back image's change, against 0.8 % for a typical shift;
   - the render is deterministic, so the rest is not noise: the back image is real, but one global shift does not describe it;
   - "as the optics predict" is withdrawn.

### 15.1 Every review-4 item and what was done

Engineering = E-n, art = A-n (review numbers).

| ID | What was wrong | What was done (clone `W/proj_rt_eng3`, file:line) | Evidence (fresh runs, UNDER LOAD) | Verdict |
|---|---|---|---|---|
| **E-1 / A-1** (payload) | Red's D1 (b) was not applied: `DefaultQuality()` still returned Ultra at level ≥ 5, and the §14.4 list left the change out | `FrontRoomsGlassRTSystem.cs` l. 191–194: the last `return` is now `level >= 3 ? High : Off` (comment cites D1 (b)). `FrontRoomsGlassRT.cs` l. 6–7: the header now says High at level 3 and up, Ultra only when asked for. The payload changes 10 paths (§15.5). The rule "no Mac store build with the G14 dylib until D1 (c)" is recorded here, in `40_for_red.md` and in VL314 | **Roslyn `--all`:** clone COMPILE OK with macOS and iOS defines (7 assemblies, 0 errors; `W/logs/fix34/roslyn_clone_{mac,ios}.log`, re-checked after the harness edit, `roslyn_clone_mac2.log`); main `2c450e11` + the 8 payload `.cs` from the clone: COMPILE OK macOS and iOS (`roslyn_main_{mac,ios}.log`). Unity compile `c36`: 0 `error CS`. **Player `pb8`** (`BuildPipeline.BuildPlayer` from the clone-only `G14VPlayerBuild`, URP intact, x64ARM64, bundle id `com.redwang.frontroomsss.test.rteng3` per RULE 18; the same `errors=1` build-report line as fix33's `pb7`): **`ps16` native** "quality level 5 (Ultra) … default RT quality High"; WARM 404,179 px; 38 GD3 slabs on bit 30; 405,925 px traced; the walk at High, 0 blank; the break 436,575 → 0 px; **DONE "0 errors … on: Apple9, High"**. **`ps17` Rosetta 2** (`proc_translated 1`): the same lines. No test player left running | **FIXED** |
| **E-2 / A-1** (text) | The docs said Red had not answered; `40_for_red.md` still asked | `40_for_red.md` rewritten as the record of the decisions: D1 (b) now, (c) in `g14-followup` with store builds held; D6 (a) now, (b) with P2; D2 (b) in `g14-followup`, (c) later. R3-L1 is marked resolved in §14.1 and §14.4. The banner, §0 item 8 and §5.2 carry the decisions | — | **FIXED** (text) |
| **E-3 / A-10** | VL314 cited `30v_cost_D1_D6.jpg` ("every ms is an upper bound", single values, S2 "not registered", no Shot frame) and `30v_player.jpg` (plugin `518d6769`, "Ultra (default, level 5)"); §0 item 8 and §5.2 kept "1.9–3.6" and "6.8–10" unmarked | Both sheets moved to `W/_partial/fix34_vl_sheets_prev/`. New Shot-format sheet `30v_F34_player_cost.jpg`: `ps16`'s v2 Off / High (default) / Ultra (opt-in) / change mask, the 21-run range table drawn from `cost34.json`, and the extra runs. §0 item 8, §5.1 and §5.2 carry correction marks | — | **FIXED** |
| **E-4** | The published ranges left out the undisturbed case (review 4's 8 ABBA runs, RT-off frame 6.9–7.3 ms at 1440p) | All ranges are now over the 21-run pool (`W/rt_work/review4_eng/ab_pool21.tsv`, recomputed with `W/rt_work/fix34/tools/cost34.py` → `cost34.tsv` / `.json`), with the 8 quiet runs as their own column: §15.3, `40_for_red.md`, VL314, the new sheet. The projection is redone from the 21-run ends | v5 1440p High 4.72–13.05 (quiet 4.72–4.99): at the bar quiet, over it disturbed, so not a clean FAIL by the doc's rule. Shot 1080p High 2.78–5.31, over +2 by 0.8–3.3. Ultra − High at 1440p 1.77–10.17. RT-off frame at 1440p 6.90–18.12. Base M3/M4: 1440p 0.55 m High 19–65, Ultra 30–115; 1080p 0.55 m High 11–27 | **FIXED** (text) |
| **E-5** | The 13-run pool left out three runs of the landing build without saying so; "the top of each range comes from runs where the machine was 1.5–2× slower (load 100–155)" was false for v2 1080p, and the load figure was wrong | Every left-out run is named with its values and reason (`cost34.py` `LEFT_OUT`; §15.3, `40_for_red.md`). The sentence is reworded. The slow runs (RT-off 16.7–18.1 ms at v5 1440p: fix33 ab1_new, ab3_old, ab3_new) started at load 47–115. The v2 1080p top (High 2.15, Ultra 3.73) is fix32 ab1_new, whose RT-off frame was a normal 4.34 ms. ab2_old ran at load 122–155 with a near-normal RT-off frame (8.6 ms) | fix32 ps12: v2 / v5 1440p High +18.75 / +20.77, RT-off 23.3 / 23.9 ms. ps13: +7.24 / +12.26, RT-off 15.0 / 16.4 ms. fix33 ps15: inside every range | **FIXED** (text) |
| **E-6** | §14.2 gave "p95 2.48" (J1 only) for the main thread while held after a jump | §14.2 marked: round 3 J1–J3 p95 2.26–2.57, max 2.91–3.35 ms. Round 2: 2.28–2.35, max 2.64–2.93 (`r9b` / `r8b` metrics `jump.*.cpu_hold_*`) | — | **FIXED** (text) |
| **E-7** | "Round 3 vs round 2 not proven" could be closed | §14.3 marked closed. In review 4's four ABBA pairs, round 3 − round 2 (added High) was −0.22 to −0.01 ms at v5 1440p, −0.08 to +0.04 at v5 1080p, −0.33 to +0.25 at v2 1440p and −0.36 to +0.32 at v2 1080p; the RT-off frame differed by −0.25 to +0.37 ms (`cost34.json` `pairs`). There is no round-3 GPU regression; fix33's "5 of 5 positive" was machine drift. Fix round 4 changed no GPU code (the dylib is still `fa0232d2`) | — | **CLOSED** |
| **A-2** | Red's D2 moves the VL284 / VL316 sign-off to the `g14-followup` proxy build, but VL316 and its sheet kept the yellow WAIT-RED chip (SHOT_FORMAT §5: open decisions only) | VL316 and its sheet now carry PENDING, "sign-off on the g14-followup proxy build"; VL284 carries the same note | — | **FIXED** |
| **A-3** | The VL283 sheets were 07:0x frames (r3a/r3b, dylib `518d6769`), repeated "no straight-edged wedges", kept hand-drawn titles, captions and yellow labels, had no Off tile, quoted stale numbers (3.3 / 8.4 %), and the Office sheet contradicted itself (8.7 / 27.0 vs 6.8 / 19.1 %); no sheet showed the step | Both re-composed with `shotframe` (`W/rt_work/fix34/tools/compose_f34.py`). **`30v_P1_parity.jpg`**: r9a parity raster / RT / ×8 difference; v5 lit planar mirror / Off / High / radiance and a 160×90 crop around both points (luma stretched p1–p99, grey, labelled); v5 at a dark-beyond window Off / High / radiance and crop (r10a); the dark final crop as rendered and stretched; the Off and planar-mirror crops stretched. **`30v_P1_office.jpg`**: r9b Office parity raster / RT / ×8 difference, v2 and v5 mirror / Off / High / radiance, v5 Ultra. The numbers come from the metrics: L0 carpet 3.0 / p95 7.7 % (ratio 1.006, wallpaper 3.5 %); Office ratio 1.030, L0 carpet 8.7 / p95 27.1 %, wallpaper 6.8 / 23.8 % (the old inner caption's 6.8 / 19.1 % was main 06:55's Office) | step values: `W/rt_work/fix34/step34.json` (`tools/step34.py`) | **FIXED** |
| **A-4** | `30v_AD6_showcase.jpg` said "with RT Off beside each", but the move and back-image tiles had no Off and no mode, and two tiles were round-2 frames (`ec31f4fa`); the back-image tile was a final frame showing nothing; the fit was broken (`base0` never set: at shift (0,0) `den` = 0 and the loop continued) | **Harness:** `Moving` (l. 2765) saves `P1_moving_off_f{00,15,30,45,59}` after the move (Off has no history; the crawl is measured before any quality change). `BackImage` (l. 2824): the residual is now the best shift's own unexplained share over the same px (l. 2854ff), plus the median over all 288 shifts and a render-noise floor (the image-off frame rendered twice, l. 2846). **Sheet:** 20 tiles, each with its mode; Off / High pairs for 45° and 60°, the move, HDR off and the Relay; the back image as radiance off / on and an on − off difference ×32; the art round-3 `a3` tiles (`ec31f4fa`) dropped; header reworded | `r10a` moving: lens crawl 0.106, reflected floor 0.022, raster floor 0.136. **`r10c` back image** (b5_back40, 52°, 0.75 m): best shift (1, 0) px, weight 0.0245 (the log prints 0.024); **residual 0.908 (the fit explains 0.092; median over all shifts 0.992)**; on − off rms 0.006 linear; render noise 0 (deterministic). The PNG model check (`tools/backimage34.py`): the shifted-copy model explains 0.052, an added-copy model 0.007 | **FIXED** (and a claim withdrawn) |
| **A-5** | The `30v_F33_restart_key.jpg` tiles were lit windows (about 8 % reflection), had no Off and no radiance tile, and the two key frames could not be compared; `Restart` logged no misses after the reopen | **Harness** (`FrontRoomsGlassRTVerify.cs` is a payload file, so this is a payload change, editor-only, ASCII, no tuples): `KeyPickup` (l. 3186) puts a black, unregistered (raster-only) room beyond the pane. It saves the RT Off frame (what a drop to the zone cube looks like) and a before frame with radiance, then the final and radiance frames at +1, +30, the first frame the key is at the map root, 8 frames later, and the frame it is gone (`keypickup_saved.tsv`). It counts frames with misses and the max miss share. `Restart` (l. 3360) puts the same room beyond the new run's window. It saves the first held frame (zone cube) and the first open frame, each with radiance, the +120 frame with radiance, and the Off frame. It logs misses over the 120 frames after the reopen; the pass rule now also needs a max miss share ≤ 0.5 % (l. 3436) | **`r10b`** (clean re-run; §15.4 on the first one): **key pickup** 16.2 m away: 0 near births, 0 holds, 117 traced frames, min weight 1.000, 0 frames with misses, chunks 25 → 25, 0 uncounted squares, PASS; REBUILD refs 1 → 1, PASS. **R1** (held, game's restart seed 834265813): opened at +22, then 121 traced frames, **0 with misses**, burst 0, budget off default 0, orphans 0, PASS. **R2** (open): opened at +23, the same, PASS. Held frames show the zone cube (weight 0); open frames are traced | **FIXED** |
| **A-6** | `compose_f33.py` typed "a3: −5.7/255, p5 −24" and "weight 1.000" by hand; moving floor 0.022 on the sheet vs 0.023 in the text (metric 0.0225); meta stamp "13:1x" | `compose_f34.py` reads every number from metrics, logs or the tools' JSON, and rounds half up on the metric's decimal string, as the log prints it. The `a3` tiles are gone. The stamp is checked against `YYYY-MM-DD HH:MM`. This round's moving floor is 0.0224 → 0.022 (r9a's 0.0225 → 0.023), quoted per run | — | **FIXED** |
| **A-7** | The lit-window Relay figure (−2.08 / p5 −7.92) was measured on a frame that was never saved; the tile showed the accumulated frame (−2.37, p5 −9.8) | `RelaySilhouette` (l. 2664) saves the measured frame (`<key>_measured_high.png`) and its radiance. The sheets show the measured frames. `tools/relay34.py` recomputes the figures from the saved PNGs and mask | **r10a dark:** 43,979 px, −2.44/255, p5 −12.42 (logged −2.4403, −12.4224); vs Off −9.07. **Lit:** 43,989 px, **−1.08/255, p5 −7.64** (logged −1.0752, −7.639); vs Off −4.92. The accumulated lit frame gives −2.07, p5 −11.28. So the lit figure moves between runs and with accumulation: −1.1 to −2.1/255 over r9c and r10a. *(Package stage, §16.1 A5-2: the signed mean cancels brighter and darker parts of the silhouette and understates the ghost. Over the silhouette with accumulation on, as in the game: dark mean \|ΔY\| 6.84/255, p5 −12.4 / p95 +19.4, 26.0 % of px change by more than 8 levels; lit 5.52, p5 −11.3 / p95 +13.5, 18.7 %; lit with accumulation off 4.85, p5 −7.6 / p95 +13.6, 13.5 %. Noise outside the silhouette 0.86–0.93.)* | **FIXED** |
| **A-8** | The VL316 tile "DARK BEYOND V3 50 DEG HIGH: no player in the mirror": at 50° the player's mirror point falls about 1.15 m off the pane centre | The V3 pair is dropped from VL316. "Nobody at the camera" is now shown only at v2, 1.5 m head-on, where the mirror point is the pane centre | — | **FIXED** |
| **A-9** | VL283 said the step "hides at the ~8 % lit-window strength"; no close frame at a dark window had been checked | **Harness:** `DarkBeyond` (l. 1463) also does v5 (0.55 m, the break-shot pose): Off / High / Ultra, radiance, grain-off analysis frames, the planar mirror. VL283 now covers both window kinds | **r10a**: the radiance step at (752,354) / (819,342) is +13.6 / +6.2 % (Ultra +14.2 / +5.7); lit r9a +6.9 / +6.6 %; over runs r8a–r10a +2 to +14 %. In the final frame along the step's direction (50°): dark High 29.5 → 29.9/255 (+2.4 %; Off 27.8 → 27.8). At (819,342) dark High 31.1 → 32.6 with Off 28.8 → 30.5: that edge is the same with RT Off. Lit High 109.6 → 110.3 (Off 108.5 → 108.9); lit Ultra 109.3 → 110.4. So RT adds under one 8-bit level across the step at this pose in both window kinds. The planar mirror: +3.2 / +1.4 %. The dark-beyond v5 radiance equals the lit one within 0.4/255 mean. *(Package stage, §16.1 A5-1: the reason is the non-RT base, not a Fresnel scale; §15.0 item 4 and §15.4 carry the correction.)* | **corrected** (AD-2 stays open; §15.4) |

### 15.2 The checks re-run, and why only these

| Run (clone, `W/logs/fix34/`) | Phases | Load | Result |
|---|---|---|---|
| Roslyn `--all` | clone; main `2c450e11` + 8 payload `.cs` | — | COMPILE OK, macOS and iOS, both |
| `c36` | Unity compile (`G14VReimport`) | 55–60 | 0 `error CS` |
| `r10a` | `dark` (now with v5), `darkrelay`, `moving`, `backimage` | 55–60 | as §15.1. Dark-beyond v2 / v3 / v5: 0 misses. Exit release: released |
| `r10c` | `backimage` (with the noise floor) | 44–47 | as §15.1 |
| `r10b` (clean) | `walk`, `keypickup`, `restart` (+ `walk2`) | 33–47 | as §15.1. Walk: quiet main thread p50 0.354 ms (walk2 0.373), GPU AS p50 2.129 → 1.658 ms, BLAS builds per event 8 → 8, burst frames 0 / 0, BLANK 0 / 0, holds 0 / 0. Exit release: released |
| `pb8` / `ps16` / `ps17` | universal player, native and Rosetta 2 smoke | 37–44 | as §15.1 E-1. Added frame time inside every 21-run range (v5 1440p High 4.80 / 4.97) |
| dry run on main | the round-2 package | — | DRY RUN OK, every base matches |

Why only these: D1 (b) changes one return value on the CPU. The harness edits are editor-only. No GPU code, MSL, plugin, stripper or importer changed (the dylib is still `fa0232d2`). So `r9a`'s other phases, the `r9b` jumps / stream / broken / office, `iso7`, `o2`, the seqlock test and the macOS 12 audit stand as in §14.2. D1 (b) does not touch the stripping or the old-plugin path. The walk main thread over rounds 2–4 is quiet p50 0.35–0.45 ms, against a 0.3 ms bar: still FAIL (F3).

### 15.3 Cost: 21-run ranges (replaces §14.3's table)

Added frame time = whole-frame GPU median, RT − Off, 16 interleaved rounds per run, universal macOS player. Pool, left-out runs and method: `40_for_red.md` "The numbers behind D1 and D6". Data: `W/rt_work/fix34/cost34.tsv` / `.json`.

| Pose, resolution | High, 21 runs | High, 8 quiet | Ultra, 21 runs | Ultra, 8 quiet | Bar | Verdict |
|---|---|---|---|---|---|---|
| v2 1.5 m, 1080p | −0.04–2.15 | 0.96–1.50 | 1.44–3.73 | 2.31–2.85 | — | — |
| v2 1.5 m, 1440p | 2.06–6.13 | 3.17–3.52 | 4.36–9.64 | 5.06–5.62 | 2.0 / 3.5 | FAIL / FAIL |
| v5 0.55 m, 1080p (shot) | 2.78–5.31 | 2.78–2.96 | 4.58–9.45 | 4.58–4.78 | +2 (High) | over by 0.8–3.3 |
| v5 0.55 m, 1440p | 4.72–13.05 | 4.72–4.99 | 7.59–22.93 | 7.59–8.00 | 5 / 7 | High at the bar quiet, over disturbed; Ultra FAIL |
| Ultra − High (D1 (b) saving) | 1080p 1.31–4.14 · 1440p 1.77–10.17 | 1440p 1.77–3.01 | | | | |
| RT-off whole frame, 1440p | 6.90–18.12 | 6.90–7.33 | | | | |

- **Projection** (low end × low factor, high end × high factor; not measured):
  - base M3/M4 (×4–5): 1080p High 0–11 / 11–27, 1440p High 8–31 / 19–65, 1440p Ultra 17–48 / 30–115 ms (1.5 m / 0.55 m);
  - Pro (×2–2.9): 0–6 / 6–15, 4–18 / 9–38, 9–28 / 15–67;
  - Max (×1.25–1.33): 0–3 / 3–7, 3–8 / 6–17, 5–13 / 9–30.
- **D1 (b) is applied, so the default cost is the High column.** Ultra is opt-in. D1 (c) (RT off by default under 16 GPU cores) answers the base-M3/M4 projection.

### 15.4 Where a reviewer was not followed, and why; process notes

- **A-9's premise, "in dark-beyond windows the reflection is at full strength":** measured, it is not.
  - *(Corrected by the package stage, §16.1 A5-1. The 14:10 text said that at a dark-beyond window the reflection is all that shows, at the pane's Fresnel strength, and compared 0.013 (final) with 0.197 (radiance). A uniform scale cannot shrink a relative step, and that ratio compares a tonemapped, graded frame with scene radiance.)* What the frames show: the pane's final value at this pose is mostly a base that RT does not set. With RT Off the point (752,354) is already 27.8/255; over the pane the mean final value is 0.0164 linear with RT Off and 0.0160 with High. Inside the pane (1,317,793 traced px, grain-off frames) the final frame fits 0.0138 + 0.0138 × the traced radiance (linear, R² 0.008), so the traced radiance carries about 14 % of the final value on average.
  - So the +13.6 % radiance step moves the final frame by about 2 % there: 29.5 → 29.9/255 measured, 0.4 of an 8-bit level.
  - The row and the sheet now show this case. AD-2 stays open: the step is over its 3 % bar in the radiance, it will show wherever the reflection is brighter, and it will shimmer when accumulation resets.
  - No code change: the fix is the glass track's per-hit ranked soft shadows.
- **A-4 offered two fixes (Off tiles, or a corrected header):** both were done, and the fit was fixed too.
  - The fixed fit does not support "as the optics predict", so that claim is withdrawn rather than restated.
  - The best shift's size (1 px at 1080p) does agree with the thin-plate offset: about 4.5 mm for a 6 mm pane at 52°, which is about 0.3–2 px for surfaces 10–1.5 m away along the ray.
  - The best shift explains only 9 % of the change, and the rest is not render noise (the image-off frame renders identically twice). The second image's offset varies with depth, so one global shift cannot describe it.
- **E-4:** done as asked. This round's two player runs (`ps16`, `ps17`) are reported as extra runs inside the ranges and were not added to the reviewed 21-run pool.
- **Process note: the first `r10b` is not evidence.**
  - I edited `FrontRoomsGlassRTVerify.cs` (the back-image noise floor) while `r10b` ran on the clone. Unity recompiled at the Play exit, so the domain reload reset the harness's static log, and the edit-mode exit line overwrote `verify_log.txt` and `metrics.tsv` with one line.
  - In that run, every phase passed in the Unity log (`W/logs/fix34/r10b_first.log`).
  - `r10b` was re-run clean (above). The first run's folder is `W/rt_work/fix34/r10b_first_out`.
  - No clone file was edited during any later run.
- **H7 (new, minor, editor only):**
  - In that first `r10b`, the forced recompile at the Play exit came before the editor updates that pump the plugin's release. The new domain then logged "released False … native meshes 1646, BLAS 5.1 MB, geometry 17.8 MB".
  - `ExitingPlayMode` had already called `FRGlassRT_ResetScene` (`FrontRoomsGlassRTSystem.cs` l. 2911); the retire needs one more render event.
  - So when a script recompiles at the moment Play ends, the native scene stays held in the editor process until the next Play start resets it. Not checked further.
  - Possible fix (glass track): pump the release from `AssemblyReloadEvents.beforeAssemblyReload`.
  - Players are not affected (no domain reload).
- **Housekeeping:** this round's own first sheet drafts (`W/rt_work/fix34/sheets_draft/`, regenerated output of `compose_f34.py`) were removed with `rm` once, against the move-to-`_partial` rule. The final drafts are kept.

### 15.5 Still open, and the next steps

1. **Review round 5** on the clone. The payload changes 10 paths against the round-2 payload (clone sha1):
   - `Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRTSystem.cs` `736cce1c`
   - `Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRT.cs` `2deaf988` (new this round: D1 (b) comment)
   - `Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRTNative.cs` `00257b34`
   - `Assets/Editor/RT/FrontRoomsGlassRTVerify.cs` `a2aa1887`
   - `NativePlugin/FrontRoomsMetalGlassRT.mm` `12ce551d`
   - `NativePlugin/FRGlassRTScene.mm` `e5ee4065`
   - `NativePlugin/FRGlassRTScene.h` `5df6b4d8`
   - `NativePlugin/FrontRoomsGlassRT.metal` `563733bc`
   - `NativePlugin/build_frontrooms_metal_glass_rt.sh` `7a088a76`
   - `Assets/Plugins/macOS/libFrontRoomsMetalGlassRT.dylib` `fa0232d2` (`.meta` unchanged)

   The other 10 payload paths are unchanged.
2. **Package stage** (as §13.5 step 4; *done, §16: the backup went to `W/apply/_retired/glass_rt_g14_20261022r2/`*):
   - back the current package up into `W/apply/_retired/glass_rt_g14_20261014/`;
   - rebuild the payload with the 10 changes;
   - record the bases from main at that moment;
   - run the `APPLY_REAL` scratch test, then the dry run on main.
   - Map-owned files and ProjectSettings stay out.
3. **`g14-followup`** (after G14 lands):
   - D1 (c): RT on by default only at ≥ 16 GPU cores, with an IORegistry query and a forced-core-count test. No store build with the G14 dylib before it.
   - D2 (b): the reflection-only body proxy, with silhouette art by 游戏视觉. VL284 and VL316 are signed off on this build.
4. **Glass track:**
   - P2's per-hit lamp cull with D6 (b);
   - AD-2 (per-hit ranked soft shadows);
   - PRINT-1;
   - LENS-E;
   - F3, the main thread at 0.35–0.45 ms against 0.3;
   - H7.
5. **Static only:** the macOS 12 load (no macOS 12 Mac here).

### 15.6 Files

- **Stage folder `W/rt_work/fix34/`** (lock `OWNER.lockdir`):
  - runs `r10a_out`, `r10b_out`, `r10b_first_out`, `r10c_out`, `ps16_out`, `ps17_out`, `player_universal/` (the D1 (b) player);
  - `pre/` (the fix-33 copies of the three edited C# files);
  - `cost34.tsv` / `.json`, `cost_pool21.tsv`, `step34.json`, `relay34.json`, `backimage34.json`;
  - `sheet_tiles/` (crops and difference tiles), `sheets_draft/`;
  - Roslyn outputs `roslyn_{clone,main}_{mac,ios}`, `roslyn_clone_mac2`.
- **Tools** `W/rt_work/fix34/tools/`: `cost34.py`, `step34.py`, `relay34.py`, `backimage34.py`, `compose_f34.py` (all sheets), `patch_30final_34.py` (this file's edits); text in `W/rt_work/fix34/text/`.
- **Logs** `W/logs/fix34/`: `chain_fix34.zsh`, `chain_fix34b.zsh`, `chain.out`, `chainb.out`, `load.tsv`, `c36`, `r10a`, `r10b`, `r10b_first`, `r10c`, `pb8`, `roslyn_*.log`, `dry_main_end.log`.
- **Images (Shot format):**
  - re-composed: `30v_AD6_showcase.jpg` (VL284), `30v_D2_dark_window.jpg` (VL316), `30v_F33_restart_key.jpg` (VL281), `30v_P1_parity.jpg`, `30v_P1_office.jpg` (VL283);
  - new: `30v_F34_player_cost.jpg` (VL314);
  - history in `W/_partial/fix34_vl_sheets_prev/`: the previous five, `30v_cost_D1_D6.jpg`, `30v_player.jpg`.
- **VL rows:** `W/vl_pending/glass_rt_g14.md` (VL281, VL283, VL284, VL314 and VL316 updated in place; no new number).

---

## 16. Package stage after review round 5 (engineering READY, art NOT READY on text only): READY

2026-10-22 14:29–14:5x · 游戏视觉 · workflow stage "package" (workflow `wf_f882db1c-d69`, session `local_a9a1dce2`, account B). Lock `W/rt_work/package5/OWNER.lockdir`.

**Verdict:**
- Review round 5 found no payload defect. Engineering: READY with 3 text items. Art: NOT READY with 10 items, every one a text, sheet or process item. This stage fixed all 13 (§16.1). None of them touches the payload.
- **The package is refreshed and READY to apply** (§16.3): `W/apply/glass_rt_g14.sh`, apply convention v2, 20 paths from the verified clone `W/proj_rt_eng3`. It carries Red's D1 (b) default as the one-line return in `FrontRoomsGlassRTSystem.DefaultQuality()` (`return level >= 3 ? FrontRoomsGlassRTQuality.High : FrontRoomsGlassRTQuality.Off;`, as `NOTE_1022_red_decisions.md` gives it) and the universal plugin `fa0232d2`. Bases were recorded on main `6521e6ad` (Red 14:11) at 14:40.
- **Tests** (§16.4): 13 of 13 `APPLY_REAL` scratch tests PASS; the dry run on main prints DRY RUN OK, plain and with `--with-deletions`; Roslyn `--all` on main + the 8 payload `.cs` files: COMPILE OK with macOS and iOS defines.
- **No Unity, build or player ran in this stage.** Three other visual-line Unity jobs were running when it started (`proj_runb`, `proj_lt`, `proj_station`), and none was needed: the payload is byte-identical to the clone state that fix round 4 ran (`r10a`, `r10b`, `r10c`, `ps16`, `ps17`) and review round 5 checked. Every timing quoted here comes from those rounds and is **UNDER LOAD**.
- **Store builds:** until D1 (c) lands in `g14-followup`, no Mac store build that contains this dylib goes to players (Red's D1). Snapshots and class builds are fine.

**Commands** (for the 游戏视觉 session; never run `--apply` from a workflow stage):

```zsh
bash /Users/redwang/FrontRoomsVisualWork/apply/glass_rt_g14.sh                          # dry run: must print DRY RUN OK
bash /Users/redwang/FrontRoomsVisualWork/apply/glass_rt_g14.sh --apply                  # then restart Unity (an open editor keeps the old plugin)
bash /Users/redwang/FrontRoomsVisualWork/apply/glass_rt_g14.sh --apply --with-deletions # optional clean-up (3 unreferenced paths), Red's OK
```

On a base mismatch, run `W/tools/rebase_apply.sh glass_rt_g14` first.

**Written in Red's project:**
- the new top banner, this section, and correction marks in §0 item 5, §5.2, §5.3, §11.4, the §15 setup, §15.0 item 4, §15.1 (A-4, A-7, A-9), §15.4 and §15.5;
- the review-round-3 BLOCKED banner was removed from the top (the package is refreshed); §13 keeps that record;
- `40_for_red.md`: the package status, the D6 (b) estimate, the Relay figures and the restart / key wording;
- the re-composed sheets `images/30v_F33_restart_key.jpg` (VL281), `30v_P1_parity.jpg` and `30v_P1_office.jpg` (VL283), `30v_AD6_showcase.jpg` (VL284), `30v_F34_player_cost.jpg` (VL314) and `30v_D2_dark_window.jpg` (VL316).

The previous versions of those six sheets are in `W/_partial/package5_vl_sheets_prev/`. Copies of the 14:10 `30_final.md`, the 14:04 `40_for_red.md` and the VL file are in `W/_partial/package5_docs_prev/`. The writes are claimed in `LANDINGS.md`. No git write command was run. Nothing was downloaded.

### 16.1 Every review-5 item and what was done

Engineering = E5-n, art = A5-n (review numbers). All are text, sheet or process items; the payload is unchanged by them.

| ID | What was wrong | What was done | Evidence |
|---|---|---|---|
| **E5-1** | `40_for_red.md` l. 78 gave the D6 (b) estimate as "+2.0 to +3.4 ms at 1080p", from fix round 3's 13-run low end, not from the 21-run pool; §11.4 l. 569 still said "+1.6 ms" with no mark | Recomputed from `cost34.json` with the input range and the share named (§16.2). `40_for_red.md` and §11.4 carry it | `W/rt_work/package5/measure_p5.json` `d6b` (`tools/measure_p5.py`): +1.6 to +3.5 ms at 1080p (+1.6 to +1.9 in the 8 quiet runs), ESTIMATE |
| **E5-2** | Correction marks still pointed at the superseded §14.3 ranges: §0 item 5 ("every cell fails its bar"), §11.4 ("3.1–5.3", "6.0–13.1; 4.79 was the quietest single run", "2.0–10.2"), and §5.2's G14V-1 sentence had no mark | Each mark now points at the §15.3 values: v5 1440p High 4.72–13.05 ms (4.72–4.99 quiet: at the bar quiet, over it disturbed); the shot 2.78–5.31; Ultra − High at 1440p 1.77–10.17 (1.77–3.01 quiet); G14V-1 fixed since fix round 1 (§8, §11) | — |
| **E5-3** | The §15 setup named only 13:48:34 as a moment with 3 Unity processes on the visual line | All four samples of `W/logs/fix34/load.tsv` are listed: 13:48:34 (`c36`), 13:50:05 and 13:50:35 (the void first `r10b`), 13:52:35 (three other sessions' jobs during `ps17`). Each of the stage's jobs started with at most 1 other running (the chain's wait loop) | `load.tsv`, `chain.out`. No counted evidence is affected |
| **A5-1** | The reason given for disagreeing with art-4 #9 was wrong: "the final frame holds about 7 % of the traced radiance (0.013 vs 0.197) … so the +13.6 % step becomes 0.4 of an 8-bit level". A uniform scale cannot shrink a relative step, and the ratio compared a graded frame with scene radiance | Restated as the non-RT base (present with RT Off) plus the traced radiance's small weight, in §15.0 item 4, §15.1 A-9, §15.4, the VL283 row and the VL283 sheet sentence. "Fresnel strength" and "the reflection is all that shows" are gone. AD-2 stays open | r10a v5 dark-beyond, grain-off frames: RT Off at (752,354) 27.8/255; pane mean 0.0164 linear Off, 0.0160 High; fit inside the pane (1,317,793 traced px) final = 0.0138 + 0.0138 × radiance (linear, R² 0.008): the radiance carries about 14 % of the final value, so its +13.6 % step moves the final frame by about 2 % (measured 29.5 → 29.9/255) (`measure_p5.json` `ad2`) |
| **A5-2** | The signed mean understated the Relay ghost (it cancels brighter and darker parts of the silhouette), and the sheets showed the accumulation-off frames, which the player never sees | The figures are now mean \|ΔY\|, p5 / p95 and the share of silhouette px that change by more than 8 levels, on the frame with accumulation on (as in the game). VL284 and VL316 sheets show those frames; `40_for_red.md`, §15.1 A-7 and both VL rows quote them | r10a, vs the same view without the copy. **Dark** (43,979 px): accumulation on: mean \|ΔY\| 6.84, p5 −12.42, p95 +19.35, 26.0 % over 8 levels (off: 6.84, −12.42, +19.21, 26.0 %). **Lit** (43,989 px): on: 5.52, p5 −11.28, p95 +13.49, 18.7 % (off: 4.85, −7.64, +13.57, 13.5 %). Noise outside the silhouette 0.86–0.93 (`measure_p5.json` `relay`) |
| **A5-3** | Captions were cut on `30v_P1_parity.jpg` (including the AD-2 headline 29.5 → 29.9), `30v_D2_dark_window.jpg` and three tiles of `30v_F34_player_cost.jpg` (including "on: Apple9, High"); the "V5 DARK RT HIGH +2.4 %" tile showed the grain-on frame while the number came from the grain-off one | New composer `W/rt_work/package5/tools/compose_p5.py`: every caption and title is checked against shotframe's own fit rule, and shotframe's cut helpers are wrapped so any cut stops the script. Shorter data strings; the v5 dark Off / High tiles are now the grain-off frames, labelled GRAIN OFF; the High-radiance crop no longer carries the Ultra numbers | all six sheets composed with 0 cut captions |
| **A5-4** | The VL314 change mask is Ultra vs Off but was captioned "405,925 px traced", a different quantity, with no legend; the cyan outside the glass is grain | Tile "ULTRA VS OFF >= 2/255: 246,329 in glass / 87,657 out", plus a full-width CHANGE MASK strip: grain (a second Off frame vs the first changes 811,788 px) and High's traced count 405,925 px | `ps16_out/player_smoke.txt` |
| **A5-5** | §5.3 still read as Red's open call; the VL316 row asked "should the player have a body in it (D2)?" for dark windows only, and its title differed from the sheet's | §5.3 marked "decided (b), §15". VL316 is titled "Windows without the player", like its sheet, and its question is now the check of the `g14-followup` proxy in lit and dark windows | — |
| **A5-6** | The Office sheet filed Office parity under AD-2 | Named as its own open item, OFFICE-PARITY (glass track; ratio 1.030, L0 carpet 8.7 %; it came with main's lamps, not with G14); added to §16.5 | `30v_P1_office.jpg` sentence, VL283 row |
| **A5-7** | The back-image weight read 0.025 on the sheet, 0.024 in the log and docs, while the metric is 0.0245; a raster metric sat on a radiance tile | The sheet and docs quote the metric's own 0.0245 (the log prints 0.024). The raster-floor crawl 0.136 moved to the move's RT Off tile; the radiance tile says "traced, High" | `r10c_out/metrics.tsv` |
| **A5-8** | "So the reflection is the whole picture" (VL281 sheet, `40_for_red.md` l. 94, the VL281 row) is not true | Reworded with what the frames show: at that window RT High differs from the zone cube by a mean 4.4/255 over the pane, and plainly in the reflected lamp rows. The verdict (no drop, no holes) stands | r10b key "before" frame vs its RT Off frame: mean \|d\| 4.41/255 over 437,356 pane px; lamp rows (traced radiance ≥ 200/255) 2,966 px at 87.5 vs 38.2/255; restart R1 +120: 4.44/255, lamp rows 1,993 px at 81.3 vs 41.7 (`measure_p5.json` `restart_key`) |
| **A5-9** | Main moved to `6521e6ad` (Red 14:11); the Roslyn proof and the end dry run were on `2c450e11` | Bases recorded on `6521e6ad` at 14:40 (every one equals the round-2 record: main's moves touched no package, kept or deletion path); the dry runs and Roslyn redone on it | §16.4 |
| **A5-10** | The VL file said "JPG q85, ≤ 1,600 px" | Reworded: the Shot-format sheets are shotframe output, JPG q92 (1920 px wide for every sheet re-composed since fix round 3); the unframed 07:19 sheets are q85, ≤ 1,600 px | measured from the files |

### 16.2 Numbers for Red (unchanged code, so the fix-round-4 ranges stand; UNDER LOAD)

Added frame time = whole-frame GPU median, RT − Off, 16 interleaved rounds per run, universal macOS player, this Mac (M3 Max, 40-core GPU). Ranges over 21 runs (`W/rt_work/fix34/cost34.json`), 8 quiet runs on their own; never single values, never bounds.

| Pose, resolution | High (the default now), 21 runs | High, 8 quiet | Ultra (opt-in), 21 runs | Bar High / Ultra | Verdict |
|---|---|---|---|---|---|
| v2 1.5 m, 1080p | −0.04–2.15 | 0.96–1.50 | 1.44–3.73 | — | — |
| v2 1.5 m, 1440p | 2.06–6.13 | 3.17–3.52 | 4.36–9.64 | 2.0 / 3.5 | FAIL / FAIL |
| v5 0.55 m, 1080p (the break shot) | 2.78–5.31 | 2.78–2.96 | 4.58–9.45 | +2 (High) | over by 0.8–3.3 |
| v5 0.55 m, 1440p | 4.72–13.05 | 4.72–4.99 | 7.59–22.93 | 5 / 7 | High at the bar quiet, over disturbed; Ultra FAIL |

- **D1 (b) saves** 1.31–4.14 ms at 1080p and 1.77–10.17 ms at 1440p (1.77–3.01 quiet). The frame without RT takes 6.90–18.12 ms at 1440p.
- **Projection** (not measured): base M3/M4 at 1440p, 0.55 m: High 19–65 ms, Ultra 30–115 ms. D1 (c) answers it.
- **D6 (b) estimate, recomputed (E5-1):** the 21-run shot range 2.78–5.31 ms × (1 − 0.564 × s). 0.564 is r3b's 4-lamp cut of trace + resolve (5.85 → 2.55 ms, v5 1080p High). s is the share of the RT stages the cut acts on, measured at v5 1080p High in the four universal players `ps14`–`ps17`: 0.750–0.772 if it covers trace + resolve, as the ablation measured (→ +1.57 to +3.06 ms), or 0.622–0.646 if it covers the trace only (→ +1.77 to +3.45 ms). So **+1.6 to +3.5 ms at 1080p, ESTIMATE** (+1.6 to +1.9 in the quiet runs). It assumes the added frame time shrinks in proportion to the stages. Red chose (a) now and (b) with P2.
- The walk's main thread is still over its bar: quiet p50 0.35–0.45 ms against 0.3 (F3, FAIL).

### 16.3 The package (`W/apply/glass_rt_g14*`, apply convention v2)

Built by `W/rt_work/package5/tools/build_pkg_p5.py` (and `write_diff_p5.py` for the review diff). The round-2 package (dylib `ec31f4fa`, Ultra at quality level 5) was moved whole into `W/apply/_retired/glass_rt_g14_20261022r2/` (with `RETIRED.txt`); nothing was overwritten or deleted.

| Path | Base (main `6521e6ad`) | Payload | Changed since round 2 |
|---|---|---|---|
| `NativePlugin/FRGlassRTShared.h` | `0c2bde87` | `d730fe01` | — |
| `NativePlugin/FRGlassRTLayout.h` | `ab63ddf7` | `e7ee16a5` | — |
| `NativePlugin/FRGlassRTScene.h` | `2d69f87a` | `5df6b4d8` | yes |
| `NativePlugin/FRGlassRTScene.mm` | `b035ceb2` | `e5ee4065` | yes |
| `NativePlugin/FrontRoomsGlassRT.metal` | `0c212e3e` | `563733bc` | yes |
| `NativePlugin/FrontRoomsMetalGlassRT.mm` | `7d8984f0` | `12ce551d` | yes |
| `NativePlugin/tools/frglassrt_validate.mm` | `f446bdb5` | `6cb79b9b` | — |
| `NativePlugin/build_frontrooms_metal_glass_rt.sh` | `35fb10d7` | `7a088a76` | yes |
| `Tools/rt/compose_rt_sheets.py` | ABSENT | `59faf65b` | — |
| `Assets/Shaders/GlassRT/FrontRoomsGlassRTPrepass.shader` | `f2fab79c` | `0db6d49c` | — |
| `Assets/Editor/RT/FrontRoomsGlassRTWebGLStripper.cs` | `fdf5bc05` | `5edb7b0b` | — |
| `Assets/Editor/RT/FrontRoomsGlassRTIsolation.cs` | `9489ba07` | `02e94c70` | — |
| `Assets/Editor/RT/FrontRoomsGlassRTVerify.cs` | `27ab0931` | `a2aa1887` | yes |
| `Assets/Editor/RT/FrontRoomsGlassRTImporter.cs` | `511c4186` | `46c637a4` | — |
| `Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRTNative.cs` | `261f7a5f` | `00257b34` | yes |
| `Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRT.cs` | `921a86c2` | `2deaf988` | yes |
| `Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRTSystem.cs` | `e0b81f36` | `736cce1c` | yes (D1 (b) at l. 191–194) |
| `Assets/Scripts/Rendering/FrontRoomsMetalGlassRTRendererFeature.cs` | `ea0ffa28` | `d4ced531` | — |
| `Assets/Plugins/macOS/libFrontRoomsMetalGlassRT.dylib` | `56a35164` | `fa0232d2` | yes |
| `Assets/Plugins/macOS/libFrontRoomsMetalGlassRT.dylib.meta` | `e5fd5626` | `0377540d` | — |

- **The plugin:** `fa0232d2ecd5f91c70476ea9532ec889464365dd`, 499,712 B, universal (x86_64 + arm64), `LC_BUILD_VERSION` minos 12.0 / sdk 27.0 on both slices (checked with `lipo` / `otool` on the payload copy). Its `.meta` keeps GUID `9b3bca949509f45ffaec699963f56221`: Editor ARM64, Standalone macOS `AnyCPU`, every other platform off. The build script is in the payload; the plugin's intermediate `NativePlugin/build/` folder is not.
- **D1 default in the payload:** `return level >= 3 ? FrontRoomsGlassRTQuality.High : FrontRoomsGlassRTQuality.Off;` exactly as Red's note gives it; overrides `-frGlassRT off|high|ultra` and the `FrontRooms.GlassRT` setting still win. No other default was changed.
- **Kept, never written** (re-recorded on `6521e6ad`, unchanged): 9 GUID `.meta` files (a changed one refuses the apply) and 4 contract files (`FrontRoomsGlassRTCamera.cs`, GD3's `FrontRoomsGlass.shader`, `Editor/Rendering/FrontRoomsGlassRTStripper.cs`, `FrontRoomsGlassBreakable.cs`; a change only prints a notice).
- **Optional deletions** (only with `--with-deletions`, moved into the backup folder): `Assets/Shaders/FrontRoomsMetalGlassRTComposite.shader` + `.meta`, `Assets/Scripts/Rendering/MetalGlassReflection.meta` (and the empty folder).
- **Ownership:** every written path is visual-owned (NativePlugin/, the dylib + `.meta`, `Scripts/Rendering/GlassRT`, the RT renderer feature, `Shaders/GlassRT`, `Editor/RT`, `Tools/rt`). No map-owned file (`FrontRoomsMap/*`, `FrontRooms3DGame*.cs`) and no ProjectSettings.
- **Off the Mac:** WebGL, iOS and Windows stay free of G14 (review 5: `iso7` holds, 0 `FRGlassRT_` in their player scripts; the stripper files are unchanged since round 2).
- **Package files** (sha1): `glass_rt_g14.sh` `d70813f0` (the round-2 script with a new header; behaviour unchanged, `bash -n` OK), `_bases.txt` `120c939b`, `_expected.txt` `10e0104c`, `_keep.txt` `40425ba7`, `_delete.txt` `62ac5cbe`, `.diff` `d856ff9c` (informational). Text files LF only.

### 16.4 Tests (no Unity)

| Test | Result | Log |
|---|---|---|
| 13 scratch tests, `APPLY_REAL` on copies of main's touched, kept and deleted paths (`W/tools/_test/glass_rt_g14_pkg5/run_tests.zsh`) | 13 / 13 PASS: dry run OK; apply MANIFEST OK with the deletions left in place; again ALREADY APPLIED; deletions done (composite moved, empty folder removed); again ALREADY APPLIED; apply + deletions in one go; half-applied refused; moved base refused; changed `.meta` refused; changed deletion not deleted blind; the same without the flag applies | `W/logs/package5/scratch_tests.out`, `W/tools/_test/glass_rt_g14_pkg5/tests.log` |
| Dry run on main `6521e6ad`, plain | DRY RUN OK: 20 bases, 13 kept files and 20 payload files match; no backup folder made | `W/logs/package5/dry_main_1.log`, `dry_main_end.log` |
| Dry run on main, `--with-deletions` | DRY RUN OK (3 deletions match) | `W/logs/package5/dry_main_del.log` |
| Roslyn `--all`, main + the 8 payload `.cs` as overrides, macOS defines | COMPILE OK, 7 assemblies, 0 errors (Assembly-CSharp 102 files, Editor 62) | `W/logs/package5/roslyn_main_mac.log` |
| The same with `--ios` | COMPILE OK (iOS defines), 0 errors | `W/logs/package5/roslyn_main_ios.log` |
| Clone = payload, sha1 for sha1 | 20 / 20; the clone differs from the round-2 payload in exactly the 10 paths of §15.5 | — |

### 16.5 Still open

1. **Landing:** the 游戏视觉 session runs the dry run, then `--apply` (§16 commands), then restarts Unity. Re-check the bases first if main moved.
2. **`g14-followup`** (after G14 lands): D1 (c), RT on by default only at 16 GPU cores and up (IORegistry query, forced-core-count test; no store build with the G14 dylib before it); D2 (b), the reflection-only body proxy (silhouette art by 游戏视觉). VL284 and VL316 are checked and signed off on that build.
3. **Glass track:** P2's per-hit lamp cull with D6 (b); AD-2 (per-hit ranked soft shadows); OFFICE-PARITY (the Office hit shader is brighter than the raster, ratio 1.030; new name for an old finding); PRINT-1; LENS-E; F3 (the walk's main thread 0.35–0.45 ms against 0.3); H7.
4. **Static only:** the macOS 12 load (no macOS 12 Mac here).

### 16.6 Files

- **Stage folder `W/rt_work/package5/`** (lock `OWNER.lockdir`): `measure_p5.json`; `sheet_tiles/`, `sheets_draft/`, `sheets_final/`; Roslyn outputs `roslyn_main_{mac,ios}/`.
- **Tools** `W/rt_work/package5/tools/`: `measure_p5.py` (Relay, AD-2 fit, restart / key, D6 (b) estimate), `compose_p5.py` (all six sheets, cut-checked), `build_pkg_p5.py`, `write_diff_p5.py`, `patch_30final_p5.py`, `patch_docs_p5.py`; this section's text in `W/rt_work/package5/text/`.
- **Logs** `W/logs/package5/`.
- **Package** `W/apply/glass_rt_g14*`; the round-2 package in `W/apply/_retired/glass_rt_g14_20261022r2/`.
- **Images (Shot format, re-composed):** `30v_F33_restart_key.jpg` `907ae4e6` (VL281), `30v_P1_parity.jpg` `1674d0be` and `30v_P1_office.jpg` `3086f91b` (VL283), `30v_AD6_showcase.jpg` `dd9f613e` (VL284), `30v_F34_player_cost.jpg` `ee9fd762` (VL314), `30v_D2_dark_window.jpg` `a28c7322` (VL316); the previous ones in `W/_partial/package5_vl_sheets_prev/`.
- **VL rows:** `W/vl_pending/glass_rt_g14.md` (VL281, VL283, VL284, VL314 and VL316 updated in place; no new number).

## 17. D1 (c) gate (2026-10-24 11:39–12:1x, 游戏视觉 workflow, Build stage; every timing UNDER LOAD, load average 13–148)

**Short answer:** RT glass is now on by default only on GPUs with at least 16 cores (M3/M4 Pro and Max and up). Smaller GPUs, and any Mac whose core count cannot be read, start with today's glass (RT Off); `-frGlassRT` and the `FrontRooms.GlassRT` setting still turn RT on at any core count, and Ultra stays explicit-only. It is built and verified in the clone `W/proj_g14gate` (main `4049c70b`). **It is not in main yet**: the store rule ("no Mac store build with the G14 dylib until D1 (c) is in main") holds until the payload lands. Scope is D1 (c) only (no D2).

- **The rule** (`FrontRoomsGlassRTSystem.DefaultQuality()`, now `DefaultFor` / `GatedDefault`): an explicit `-frGlassRT` value, then the `FrontRooms.GlassRT` setting, then High only if quality level ≥ 3 **and** cores ≥ 16 (`DefaultHighMinCores`); Off otherwise. Unknown cores = Off (fail closed).
- **The core count:** the IORegistry property `gpu-core-count` of the first `AGXAccelerator` service (subclasses match, e.g. `AGXAcceleratorG15X`), read from C# by P/Invoke into IOKit and CoreFoundation (`IOServiceMatching`, `IOServiceGetMatchingService`, `IORegistryEntryCreateCFProperty`, a `CFNumber` type check, `CFNumberGetValue`, `CFRelease` / `IOObjectRelease`). Read-only, no user client, **no dylib rebuild or re-sign** (the plugin stays `fa0232d2`). Read once per process. Any failure (no service: Intel / AMD; no property; not a number; an exception; a sandbox denial) gives unknown, never throws. Compiled only under the file's existing `UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX` guard.
  - Account B's `W/proj_rt_fu` dylib exports `FRGlassRT_GPUCoreCount` and is universal with minos 12.0, but it is only ad-hoc (linker) signed and is not main's `fa0232d2`. Not used.
- **Test override:** `-frGlassRTCores <n|unknown>` (0, a negative number or text = unknown) and `FrontRoomsGlassRTSystem.HarnessCoreCount` (reset at every Play start). Harmless in store builds.
- **One startup line** (from `EnsureStarted`, once per session): `[FrontRoomsGlassRT] GlassRT default: cores=<n|unknown> source=<IORegistry|override|none> default=<High|Off> (level <q>)`. `default` is the gated default before any explicit switch; the status line and `Quality` show what actually runs.

| Check | Result | Evidence |
|---|---|---|
| EditMode test `FrontRoomsGlassRTCoreGateTest.RunBatch` (batch, no Play Mode, no PlayerPrefs write) | **95 / 95 PASS.** Cores 8 / 10 / 14 / 16 / 40 / unknown at level 3, 4, 5 → Off / Off / Off / High / High / Off; levels 0–2 → Off at every count; the same 36 cases again through `DefaultQuality()` with the static override and `QualitySettings` set; `-frGlassRT high` at 8 cores → High (also at level 0 / unknown), `ultra` → Ultra, `off` at 40 → Off, the setting likewise, never Ultra by default; the `-frGlassRTCores` parser; fail-closed reads (missing class, missing key, string key) → unknown | `W/g14gate/gate_test.txt`, `W/logs/g14gate/t1.log` (Unity 21 s) |
| Real query on this Mac (Apple M3 Max) | **40**, equal to `ioreg -l \| grep gpu-core-count` (40); first call 574 µs including the P/Invoke binding | same |
| Player smoke, universal test player (`G14GatePlayerBuild`, bundle id `com.redwang.frontroomsss.test.g14gate`, started directly, every player quit; clone-only probe `-g14gateProbe` starts a run, waits for the plugin, logs, quits) | default: `cores=40 source=IORegistry default=High (level 5)`, **on: Apple9, High**. `-frGlassRTCores 8`: `default=Off`, Quality Off, nothing traces. `8` + `-frGlassRT high`: `default=Off`, **on: Apple9, High**. `unknown`: Off. `16`: High. Rosetta 2 (`arch -x86_64`): `cores=40 source=IORegistry`, on: Apple9, High. 0 exceptions, 0 fail-closed | `W/g14gate/s1, s1b, s2–s6/player.log` |
| The same player **inside the App Sandbox** (copy ad-hoc signed with the store's entitlements from `Tools/build_mac_testflight.sh`: app-sandbox, network.client, allow-jit; no hardened runtime, no upload) | Sandbox active (a write outside the container: `UnauthorizedAccessException`; data path in `~/Library/Containers/…test.g14gate`). **The IORegistry read is allowed:** `cores=40 source=IORegistry default=High`, on: Apple9, High. `8` → Off; `8` + `high` → High | `W/g14gate/sb1–sb3/player_stdout.log` |
| Roslyn `--all`, main `4049c70b` + the 3 payload `.cs` | **COMPILE OK**, macOS and `--ios`, 7 assemblies, 0 errors (clone: the same) | `W/tools/_rebase/g14gate/main_{mac,ios}`, `clone_{mac,ios}` |

**Still open:** (1) landing: payload `W/g14gate/payload/` (2 changed files + the new test and its `.meta`; bases `736cce1c` / `2deaf988` in `W/g14gate/base/`), no apply package made in this stage. (2) The sandbox run used an ad-hoc signature without the hardened runtime; the team-signed store archive was not built (the hardened runtime does not restrict IORegistry reads; if a read were ever denied, the default is Off, which is safe). (3) No 8- or 10-core Mac here: the small GPUs are covered by the forced count only. (4) At Off nothing traces, so the plugin's layout self-test does not run (status stays "waiting: layout self-test"); it runs once RT is switched on (s3 / sb3: on within 1–3 s). (5) There is no RT switch in the settings UI, so a store player on a small GPU can turn RT on only through `-frGlassRT` or the `FrontRooms.GlassRT` value; a menu toggle is outside this scope.

**Package (2026-10-24 12:2x, 游戏视觉 workflow, Package stage; review 12:16 READY, no defect, payload unchanged):** `W/apply/glass_rt_g14_gate.sh` (apply convention v2): 4 paths, `FrontRoomsGlassRTSystem.cs` `736cce1c` -> `41364f7c`, `FrontRoomsGlassRT.cs` `2deaf988` -> `b951084f` (comment only), new `Editor/RT/FrontRoomsGlassRTCoreGateTest.cs` `b81e5d79` + `.meta` (GUID `304a16ca`, unused in main); kept: both `.meta`, the GlassRT and Editor/RT folder metas, the plugin `fa0232d2` + `.meta`. Bases = main `4049c70b` + tree. Dry run on main: DRY RUN OK; `rebase_apply.sh --no-install`: PASS (nothing to rebase); scratch tests `W/tools/_test/glass_rt_g14_gate_1024/t_apply_script_tests.log` (MANIFEST OK, then ALREADY APPLIED; half applied, moved base, changed kept `.meta`, changed plugin, GUID clash, new path taken, tampered payload, HOLD, bad target REFUSED); Roslyn `--all` main + payload COMPILE OK macOS and `--ios` (`W/logs/pkg_g14gate_1024/`; ref = the clone's editor compile, `W/tools/_rebase/g14gate_verify/ref`). Apply: `bash /Users/redwang/FrontRoomsVisualWork/apply/glass_rt_g14_gate.sh --apply`. The store rule lifts when it is applied. Not blocking (review): `CFNumberGetValue` declares its `CFNumberType` argument as `int` (64-bit in CoreFoundation); it reads correctly (arm64, Rosetta, sandbox) and a bad value would fail closed; `IntPtr` in a later pass.
