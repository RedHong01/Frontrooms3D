# Codex audit · 10 · Review: runtime (compile, tests, console)

Track: **runtime**. Written by the codex-audit workflow. Last pass: **2026-10-07 23:30 – 2026-10-08 00:45, on main `ee5c9bb` (20:22), then re-checked on `22bb75f` (23:46), which landed during the pass and is HEAD now. Both with Red's working tree.** This stage restarted several times after usage limits. It merges five passes:

- 2026-10-04 on main `d610d3a`;
- 2026-10-07 16:33–17:46 on `279c144`;
- 2026-10-07 18:22–20:11 on `a5262fb` / `3ff05ee`;
- 2026-10-07 23:30 on `ee5c9bb` (this pass: re-ran compile, suites, signatures, a 120 s Play run and the print P0 gates; re-read every cited file:line; read Red's editor log of today);
- 2026-10-08 00:1x on `22bb75f` (the same compile, suites, signatures and 120 s Play run; every cited file:line re-checked).

Read-only on Red's project, except this file and new files under `runtime/head_ee5c9bb/` and `runtime/head_22bb75f/`. Unity was never opened on Frontrooms3D. Nothing was committed. No file in Red's project was fixed by this review: every finding is a handoff (§1).

## Where the runs happened

| Clone (W = `/Users/redwang/FrontRoomsVisualWork`) | Code | Used for |
|---|---|---|
| `W/proj_cx` | main `22bb75f` + Red's working tree, re-synced 2026-10-08 00:1x with the TOOLS.md §2 steps (rsync from main, `apply_local_patches.py`: nothing to do, `install_tools.py`). Earlier: `ee5c9bb` (23:30), `3ff05ee` (19:41), `a5262fb` (18:22), `279c144` (16:33) | compile, suites, signatures, 120 s Play runs, print P0 |
| `W/proj_cx_base` | `git archive 7320ed1` (the pre-Codex base) over the `proj_audit` Library | the same suites, signatures and Play runs, as the control |
| `W/proj_cx_fix` | `proj_cx`@`279c144` + the R2 map contract patch (clone only) | proves the R2 fix |
| `W/proj_cx_t` | main `a5262fb` (extras clone) | Relay director policy tests, kit LOD dump |
| 2026-10-04 clones (wiped by the 10-05 reboot; outputs saved in `runtime/play`, `runtime/suites`) | `d610d3a`; `d610d3a` + 8ef5b64's old `DropChunk` line (`proj_cx_repro`); the same + a try/catch (`proj_cx_repro2`); `7320ed1` | the ZBinningJob reproduction |

- The task asked for `proj_landcheck` (synced 17:45 on 10-03, before Codex) as the control. The reboot wiped it. `proj_cx_base` is the same code: `git archive 7320ed1`, Codex's base. Base code never changes, so its 10-07 results stay the control for HEAD.
- Harnesses (clone only, never in Red's project): `FrontRoomsCxSuites` (the project's own edit-mode suites + map signatures for 20 seeds) and `FrontRoomsCxPlayRun` (batch Play Mode with graphics, Metal, M3 Max, through the title into the map, game time fixed at 60 Hz, R restarts the game's own way). Copies: `runtime/harness/*.cs.txt`; installed from `W/tools` (W/TOOLS.md §3).
- The machine was busy during every 10-07 / 10-08 run (load average 45–950, other Unity batch jobs). Counts and pass/fail do not depend on load. Millisecond timings do; this review compares none of them across runs.
- Red's merge rules (a)–(f): this track has no apply package under `W/apply/` (no fix of its own), so there was nothing to dry-run or rebase.

## 0. Short answer (for Red)

- **Main compiles.** 0 errors at `22bb75f` (HEAD) and at `ee5c9bb`. 0 warnings in visual-owned files. The only 4 warnings (player Mac and WebGL) are in the touch file `FrontRoomsTouchControls.cs` (touch session).
- **Every project test passes, same as before Codex.** 9 suites: 468 checks plus the Relay nav test (60 / 60 hunts). The glass shader compile check: 28 / 28 variants OK, byte-identical to `a5262fb`. Same results at `22bb75f`. The print P0 gates on the wallpaper you see today pass (run on `ee5c9bb`; `22bb75f` touches no print or shader file), and give exactly the numbers the print track measured before its merge.
- **The map is deterministic.** 20 seeds × 25 chunks: each seed built twice and each chunk rebuilt once, 0 differences. `22bb75f`, `ee5c9bb` and `a5262fb` build exactly the same map (0 / 500 chunks differ in renderers, lights or colliders).
- **Your game runs clean today.** Two 120 s map runs with G14 on (`ee5c9bb` with a forced R; `22bb75f` with 2 natural catches + a forced R), 7,307 frames each: 0 exceptions from game code, 0 "Collection was modified", 0 ZBinningJob, 0 Render Graph errors, 0 shader errors; every sampled gameplay frame lit (luma 60–93), 0 % black, 0 % magenta (the pink key after a restart is R2, below). Your own editor today (session from 20:38, 2 plays into the map, G14 on): 0 G14, Render Graph, shader or script exceptions, and 0 compile warnings.
- **What the ZBinningJob errors were.** Not a URP or Forward+ bug, and not there before Codex. G14 (promoted by Codex) threw "Collection was modified" inside URP's frame recording. That aborted the frame after Forward+ had started its light job and before the step that finishes it, so the next frames threw ZBinningJob errors and went black. Codex's `9e754a2` removed that one trigger.
- **But nothing stops the next one (R1).** Any other exception in G14's per-frame code would black out your game the same way. The RT review has a verified patch (`10_review_rt.md` F1). It still applies to HEAD.
- **Codex's two fixes are complete.** No other loop in G14 changes the collection it walks. Nothing calls the removed `Ensure()` path. Both warnings it targeted are gone.
- **Not Codex (R2):** after you restart with R (or get caught), the zone keys draw magenta. Still true at HEAD. This pass also measured the pattern: life 2 pink, life 3 fine, life 4 pink. The bug is in `FrontRoomsMapWorld.cs` since `f5e99e10` (2026-10-02). A one-line map contract fixes it; it is verified in a clone and now ships as a `git apply` patch.
- **New at HEAD, not Codex (R4, minor):** `ee5c9bb` changed the print slice `K03.png` in `Tools/print/`, but the shipped print array in `Assets/Resources/Print/` was not rebuilt. Nothing in the game reads that array yet, so you see no change today.

## 1. Findings

| ID | Severity | Finding | Owner → handoff |
|---|---|---|---|
| **R1** | major | ZBinningJob root cause: an exception escaping G14's `TracePass.RecordRenderGraph` aborts URP's record after Forward+ scheduled its ZBinning job. Codex's `DropChunk` trigger is fixed; main (HEAD `22bb75f`) still has no guard, so any future G14 exception blacks out every frame again | visual → **glass-rt-track** (same patch as `10_review_rt.md` F1) |
| **R2** | major | Zone keys draw magenta after R / a caught restart: `MapWorld.BuildMaterials` owns (and later destroys) a material from `FrontRoomsSurfaces.Lit`'s shared static cache. Pre-existing (`f5e99e10`, 10-02), not Codex. Still at HEAD | map (关卡设计) → contract patch `runtime/head_ee5c9bb/patches/R2_key_material_contract.git.diff` |
| **R3** | minor | `VISUAL_CHAT_TASKS.md:28` still says "10 Collection … 29 ZBinningJob (cause under audit)". Red's log had 85 and 253, and the cause is R1. Still at HEAD | docs |
| **R4** | minor | The shipped live-print array `Assets/Resources/Print/FR_Print_HardEdge.png` is stale: its recorded source hash for slice K03 (`e2af19a6…`) no longer matches `Tools/print/patterns/out/hard_edge/encoded/K03.png` at HEAD (`1f048331…`, changed in `ee5c9bb`). No runtime effect today (no script in `Assets` binds `_FR_Print`) | visual → **q1-live-print** |

No fix was applied to Red's project in this review. R1 and R4 do not break the game today. R2 is in a map-owned file, so it goes to the map chat as a contract. R3 is a doc line in the visual chat's task file.

## 2. Checks that passed (no finding)

| Check | Result and evidence |
|---|---|
| **Unity batch compile, main `ee5c9bb`** (`-buildTarget OSXUniversal`) | 0 `error CS`, 0 `warning CS` (`runtime/head_ee5c9bb/compile/unity_batch_compile_excerpt.txt`). Assembly-CSharp was already current: the 23:30 rsync changed no `.cs` file under `Assets`. Same result at `a5262fb`, `3ff05ee`, `279c144` and `7320ed1` |
| **Per-target recompile, `ee5c9bb`** (Unity's own Roslyn + response files, defines swapped; `W/cx_rt/csc_check.py`) | Editor Mac, editor Win, Editor assembly Mac / Win: 0 errors, 0 warnings. Player Mac and player WebGL: 0 errors, 4 warnings, all in `Assets/Scripts/Input/FrontRoomsTouchControls.cs` (CS0067 ×3 at lines 122 / 123 / 125, CS0414 at 174). **0 warnings in visual-owned files** (`runtime/head_ee5c9bb/compile/csc_variants_summary.txt`) |
| **Codex's warning targets** | Red's 10-03 log had CS0618 (`FrontRoomsMapWorld.cs(490,13)`, the obsolete `Ensure()` call) and CS0414 (`FrontRoomsGlassRTSystem.cs(56,17)`, `loggedOn`) (`runtime/compile/red_editor_prev_warnings.txt`). Both are gone at HEAD, and Red's editor today logs 0 `warning CS` |
| **Edit-mode suites, `ee5c9bb`** (the project has no NUnit assemblies; these are its batch test harnesses) | MapVerification 100 / 100; RelayNavTest PASS (60 / 60 hunts, 0 pass-throughs, 0 frames in architecture); MapInteractionTests 143 / 143; LevelDesignerTests 138 / 138; FixtureTickTests 28 / 28; CameraRigTests 25 / 25; CaptionsTests 9 / 9; GlassShotTests 20 / 20; StreamVerification 5 / 5. Identical to `a5262fb`. Pre-Codex `7320ed1`: the same, except MapInteractionTests 136 / 136 (the 7 new tests came with the map chat's `956b786`, not Codex) (`runtime/head_ee5c9bb/suites/suites_head_vs_a5262fb_and_precodex.txt`). The 4 exceptions and 2 errors in the MapInteractionTests console are the tests' own injected faults ("test: a broken glass kit", "tools: forced build failure"), identical in the base |
| **Glass shader compile check, `ee5c9bb`** (`FrontRoomsGlassCompileCheck`) | 28 / 28 variants OK, 0 messages, GLES3x + Metal; byte-identical to the `a5262fb` and `279c144` output (`runtime/head_ee5c9bb/suites/head/glass_compile_check.txt`) |
| **Print P0 gates, `ee5c9bb`** (`FrontRoomsPrintP0Test.RunBatch`, the static `_PrintTex` path the game draws today) | All gates PASS: T1a (worst region mean 0.37 levels, bar ≤ 1), corridor fog on / off, T2 (12 light positions, specular bit-identical, wear masks identical), invariance (0 differing pixels of 4,147,200, HDR and film), print mips (`RG_BC5_UNorm`, the FrontRoomsPrintMips rule), SRP Batcher compatible. 0 exceptions. `print_p0.json` is byte-identical to the q1-live-print track's own pre-merge run (`W/proj_q1`, 18:52), so the merge into main changed nothing these gates see (`runtime/head_ee5c9bb/extras/print_p0.json`, `print_p0_summary.txt`). The Q1b gates were not re-run here: they call `FrontRoomsPrintArray.Build()` first, which would test a rebuilt array, not main's (see R4) |
| **Relay director policy** (`FrontRoomsRelayDirectorPolicyTests.Run`, `a5262fb`; no Relay file changed since) | PASS, 5 checks (`runtime/head_a5262fb/extras/relay_director_policy_tests.txt`) |
| **Map signatures, 20 seeds** (`20261001…10`, 516574485, 4242, 1990, 7, 1955, 1993, 8086, 6502, 68000, 2554; root (4992, 0, 4992)) | Each seed built twice: 0 differences. Each of 500 chunks dropped and rebuilt in place: 0 differences. `ee5c9bb` vs `a5262fb` vs `279c144` vs the 10-04 run on `d610d3a`: 0 / 500 chunks differ (`runtime/head_ee5c9bb/suites/signatures_ee5c9bb_vs_a5262fb.txt`). The window facade r6 in `ee5c9bb` therefore builds the same window parts as r3 on every seed |
| **Signatures vs pre-Codex** | 492 / 500 chunks differ in renderers, 238 in lights, 491 in colliders (same numbers as at `a5262fb`). These are the reviewed Codex map content, not runtime faults: wall blocks re-cut around kit windows, kit window frames + `Glass_Window` slabs, the V5 cool lens and +9 % lamp lead (Red keeps it ON, `RED_DECISIONS.md`), `Prop_Glass` / `Prop_BottleBlue` on `FrontRooms/Glass` (`runtime/head_ee5c9bb/suites/signatures_ee5c9bb_vs_precodex.txt`; owners: `10_review_map-edits.md`, `10_review_glass-look.md`) |
| **Play Mode, main `ee5c9bb`, 120 s, RT High, forced R at 100 s** | 7,307 frames, 1 forced R, 95 chunk roots torn down. G14 `on: Apple9, High`; 81 `Glass_Window` renderers, all on prepass bit 30 with the `FRGlassRTPrepass` pass. 0 Collection, 0 ZBinningJob, 0 Render Graph errors, 0 shader errors, 0 `NullReferenceException` / `MissingReferenceException` / `InvalidOperationException`, 0 `ScreenVideo` messages. Frames luma 62–93, 0 % black, 0 % magenta. One renderer draws pink: the key after R = R2. The only errors are the clone-only three below (`runtime/head_ee5c9bb/play/head_ee5c9bb/`; Unity-log counts and frame stats in `unity_log_counts.txt`) |
| **HEAD moved during this pass: `22bb75f` (23:46)** | It changed `FrontRooms3DGame.cs` (map-owned: the desktop key row is now anchored bottom-left, i.e. the visual review's `vis_F1_key_panel_anchor.contract.diff`, plus an autopilot check that every HUD panel is on screen) and added 11 touch-layer `.meta` files. Re-run on `22bb75f`: Unity compile 0 errors / 0 warnings with Assembly-CSharp rebuilt; per-target Roslyn identical to `ee5c9bb` (4 touch warnings only); all 9 suites identical to `ee5c9bb`; glass compile check byte-identical; signatures 0 / 500 chunks differ from `ee5c9bb`; 120 s Play run: 7,307 frames, 2 natural catches + 1 forced R, 135 chunk roots torn down, 68 `Glass_Window` renderers all with the prepass, 0 Collection / ZBinningJob / Render Graph / shader / `NullReferenceException` / `InvalidOperationException`, sampled frames luma 60–89, 0 % black, 0 % magenta; the new check logs "AUTOPILOT HUD all panels on screen" in all 4 lives; 1 pink renderer = R2 (`runtime/head_22bb75f/`) |
| **Play Mode, earlier heads** | `a5262fb` (120 s, RT High, forced R): 7,307 frames, 0 Collection / ZBinningJob / Render Graph / shader errors, luma 40–91, 0 % magenta, 1 pink renderer = R2 (`runtime/head_a5262fb/play/head_a5262fb/`). `279c144` RT High: 7,307 frames, 0 / 0 / 0; RT off: 2 caught restarts, 0 / 0 / 0, 1 pink renderer = R2 (`runtime/head_279c144/play/`) |
| **The only console errors in every run are clone-only** | 1 FMOD `BankLoadException` (the clones carry no project-root `FMOD/` folder; W/TOOLS.md §2), 1 `ArgumentOutOfRangeException` in `UnityEditor.Search.SearchDatabase.EnumerateAll` (editor search indexing at start-up, not game code), "port 9264 in use" FMOD warnings (parallel Unity jobs). All three also appear in the pre-Codex base runs |
| **Red's own editor, today** | `~/Library/Logs/Unity/Editor.log`, session opened 20:38 (after `ee5c9bb` at 20:22), 12,651 lines: 2 plays that entered the map (`[FrontRooms3D] START` at lines 6659 and 12113), G14 `on: Apple9, High` (line 7002). 0 Collection, 0 ZBinningJob, 0 Render Graph errors, 0 `InvalidOperationException`, 0 `NullReferenceException`, 0 shader errors, 0 `warning CS` / `error CS` (`runtime/head_ee5c9bb/redlog_20261007_session.txt`). The 594 "AudioClip.SetData failed" lines (`FrontRoomsAudio.cs:20`, Unity audio is off) are pre-Codex and not visual; the batch runs of `7320ed1` show them too |
| **Codex's `9e754a2` is complete** (re-read at HEAD; `FrontRoomsGlassRTSystem.cs` md5 `e75df1b1…`, 1,531 lines, unchanged since `75cfdff`) | `DropChunk` iterates a snapshot (`:989`). Every other `foreach` and every caller of `RemoveInst` (`:780`, `:871`, `:989`, `:1004`, `:1050`, `:1106`) is safe: `:870` `GlassBroken` walks `watch.ToArray()`; `:981` and `:1004` walk a separate `gone` list; `:1050` `RefreshStatics` uses an index loop; `:1106` walks `watch` backwards by index, so removing the current element is safe; `:935` `OnMapLost` walks `chunks.Values`, and `DropChunk` never touches `chunks`. `insts` is a `List<Inst>` and `RemoveInst` writes `insts[h] = null` (which bumps the list version), but no `foreach (var i in insts)` (`:945`, `:1137`, `:1505`) calls `RemoveInst` or `Unregister`. Outside the file, `Unregister` is called from `FrontRoomsGlassRT.cs:125` and the editor-only `FrontRoomsGlassRTVerify.cs`, never inside a loop over G14's own collections. The other G14 runtime files (`FrontRoomsGlassRT.cs`, `…Camera.cs`, `…Native.cs`, `FrontRoomsMetalGlassRT.cs`, `…RendererFeature.cs`) have no `foreach` at all; `CountWhere` (`:1502`) only reads three flags |
| **Codex's `75cfdff` is complete** | It removed MapWorld's `FrontRoomsMetalGlassRTController.Ensure()` call (G14 contract C1; a map-owned file, flagged in `10_review_map-edits.md` hunk 2) and the dead `loggedOn` field. At HEAD nothing calls `Ensure()`: the only match for `FrontRoomsMetalGlassRTController` in `Assets/Scripts` is the `[Obsolete]` shim itself (`FrontRoomsMetalGlassRT.cs:19`). G14 still starts from `FrontRoomsPostStack.ConfigureCamera` → `FrontRoomsGlassRT.OptIn` (`FrontRoomsPostStack.cs:74`): every RT-on run, and Red's editor today, report `on: Apple9, High` |
| **The two glass passes agree** | `FrontRooms/Glass` `ForwardLit` and `FRGlassRTPrepass` declare the same 32-line `UnityPerMaterial` CBUFFER, so the SRP Batcher stays on; 0 "not SRP Batcher compatible" lines in any log, Red's included |

## 3. Findings in detail

### R1 · major · ZBinningJob root cause; no guard in main yet

**What Red saw (2026-10-03, after Codex's promotion).** Red's `Editor-prev.log` of that evening (analysis saved in `runtime/redlog/editor_prev_sequence.txt`):

- 85 "InvalidOperationException: Collection was modified" from `FrontRoomsGlassRTSystem.DropChunk` (`…cs:988`) ← `DiffChunks` ← `UpdateScene` ← `BuildFrame` ← `FrontRoomsMetalGlassRTRendererFeature+TracePass.RecordRenderGraph` ← URP `RecordCustomRenderGraphPasses`.
- 253 "InvalidOperationException: The previously scheduled job ZBinningJob writes to the NativeArray ZBinningJob.bins".
- 338 "Render Graph Execution error" = 85 + 253.
- Every ZBinningJob run follows a Collection error: 84 groups of 3 and 1 of 1. 0 ZBinningJob lines without one before them.

**Mechanism** (URP 17.3, `Library/PackageCache/com.unity.render-pipelines.universal@37e0d4fc2503/Runtime`):

1. `UniversalRendererRenderGraph.cs:741` (`OnBeforeRendering`) calls `ForwardLights.PreSetup`, which schedules the Forward+ clustering jobs, ZBinning among them (`ForwardLights.cs:418`).
2. The job is completed only inside the render function of the "Setup Forward lights" pass (`ForwardLights.cs:476` adds the pass, `:508` calls `m_CullingHandle.Complete()`), which runs when the recorded graph executes.
3. G14's `TracePass` records at `BeforeRenderingTransparents` (`UniversalRendererRenderGraph.cs:1287`). When `BuildFrame` throws there (`FrontRoomsMetalGlassRTRendererFeature.cs:143`), URP's `RenderSingleCamera` catches it and resets the graph (`RenderGraph.ResetGraphAndLogException`: "Render Graph Execution error"). The graph never executes, so the ZBinning job is never completed.
4. The next frame schedules ZBinning again into the same `bins` array, and Unity's job safety system throws. That frame's record also aborts, and so on.

**It is not a missing job dependency in G14.** `TracePass` reads only `resources.cameraDepthTexture` and its own imported targets; it never touches URP's light or cluster data (`10_review_rt.md` §2). The ZBinning error is a symptom of the aborted record.

**Proof** (batch Play Mode, `FrontRooms3D.unity`, seed 516574485, 120 s of map at 60 Hz = 7,307 frames each):

| Run | Code | Collection | ZBinningJob | Render Graph errors | Frames |
|---|---|---|---|---|---|
| `repro_olddrop` (10-04) | `d610d3a` + 8ef5b64's `DropChunk` line | 1,699 | 5,126 | 6,825 | black (mean luma 0.0) from map 21 s on; first error at 6.8 s |
| `repro_guard` (10-04) | the same + try/catch around `BuildFrame` | 5,787 (caught) | **0** | 0 | normal, luma 43–91 |
| `base` (10-04) and `base_restart` (10-07) | pre-Codex `7320ed1` (no G14) | 0 | 0 | 0 | normal |
| `cx_rton` ×4 + `cx_rtoff` (10-04) | `d610d3a` (with `9e754a2`) | 0 | 0 | 0 | normal |
| `cx_rton`, `cx_rtoff` (10-07) | `279c144` | 0 | 0 | 0 | normal, luma 26–92 |
| `head_a5262fb` (10-07) | `a5262fb` | 0 | 0 | 0 | normal, luma 40–91 |
| `head_ee5c9bb` (10-07, this pass) | `ee5c9bb` | 0 | 0 | 0 | normal, luma 62–93 |
| `head_22bb75f` (10-08, this pass) | `22bb75f` (HEAD), 3 restarts | 0 | 0 | 0 | normal, luma 60–89 |

Sources: `runtime/play/*/summary.txt`, `runtime/head_279c144/play/*/summary.txt`, `runtime/head_a5262fb/play/head_a5262fb/summary.txt`, `runtime/head_ee5c9bb/play/head_ee5c9bb/`, `runtime/head_22bb75f/play/head_22bb75f/`. Slide: VL085 (`2781:6093`).

So:

- The errors did not exist before Codex (the base has no G14).
- RT off has none.
- `9e754a2` removed the trigger Red hit.

**What is still wrong.** At HEAD `22bb75f`, `TracePass.RecordRenderGraph` still calls `EnsureTargets` / `SendTargets` (`FrontRoomsMetalGlassRTRendererFeature.cs:138–139`) and `BuildFrame` (`:143`) with no guard. The file is unchanged since `8ef5b64` (md5 `c2406ca9…`). The RT review injected a fault into this code: 12 / 12 frames frozen, 9 ZBinningJob errors (VL118, `10_review_rt.md` F1). Design 10 §2.7 asks G14 to "fail closed".

**Fix (one patch, not two).** `runtime/rt/rt_failclosed.diff` (RT review F1; `patch -p0`): wrap `EnsureTargets` / `SendTargets` / `BuildFrame` in try/catch, call `FrontRoomsGlassRTSystem.FailClosed(e)` (one log line, RT Off for the session) and return before any RT pass is added. Verified there with the injected fault: 0 ZBinningJob, frames live. It applies to HEAD cleanly (`patch -p0 --dry-run` on copies of HEAD's two files, 2026-10-08 00:2x; the RT review found the same at 23:2x). My 10-04 `repro_guard` clone proved the same idea against the real `DropChunk` bug. `runtime/head_279c144/patches/R1_tracepass_guard.diff` only points at that patch.

**Handoff:** glass-rt-track (its fix stage owns `FrontRoomsMetalGlassRTRendererFeature.cs` and `FrontRoomsGlassRTSystem.cs`). Not applied here: today's main throws nothing, so it is hardening, not a break.

### R2 · major · Zone keys draw magenta after a restart (map contract)

**What happens.** After R (the game's static restart + `LoadScene`) or a caught restart, the new map's zone keys draw with a destroyed (null) material: Unity's magenta.

- Measured after the first restart on `279c144`, `a5262fb`, `ee5c9bb` (this pass) and the pre-Codex base.
- It alternates: the next map finds the destroyed cache entry and makes a fresh material, and the restart after it hands that one over again. **Measured on `22bb75f`:** life 2 (caught at 61.7 s) key NULL at 62.0 s; life 3 (caught at 83.4 s) key `Map test / key` at 95 s; life 4 (forced R at 100 s) key NULL at 101.5 s (`runtime/head_22bb75f/play/head_22bb75f/log.txt`).

**Evidence** (batch Play Mode, seed 516574485, close-up through a copy of the game camera, 1600 × 900):

| Run | Code | Key before R (first life) | Key 1.5 s after R | Magenta in the after-R close-up |
|---|---|---|---|---|
| `cx_keys` | main `279c144` | `Map test / key` (URP Lit) | **NULL** | 2.37 % of the frame |
| `base_keys` | pre-Codex `7320ed1` | `Map test / key` | **NULL** | 2.37 % |
| `fix_keys` | `279c144` + the contract patch | `Map test / key` | `Map test / key` | 0.00 %; 0 pink renderers in the scan |
| `head_a5262fb` | `a5262fb` (R at 100 s) | `Map test / key` | **NULL** (`BAD MATERIAL … Chunk (12, 0) … /Key`) | 2.36 % |
| `head_ee5c9bb` (this pass) | `ee5c9bb` (R at 100 s) | `Map test / key` | **NULL** (`BAD MATERIAL … Chunk (10, -2) … /Key`) | 2.37 % |
| `head_22bb75f` (this pass) | `22bb75f` (caught 61.7 s and 83.4 s, R at 100 s) | `Map test / key` (life 3, 95 s) | **NULL** in life 2 (`Chunk (12, -2)`) and life 4 | 2.37 % |

- The natural caught restart does the same: in the RT-off run the key in chunk (8, 1) was NULL 0.4 s after restart 1 (map 29.0 s, `runtime/head_279c144/play/cx_rtoff/log.txt` line 5).
- Logs: `runtime/head_279c144/play/{cx_keys,base_keys,fix_keys}/log.txt` ("KEY SHOT", "BAD MATERIAL"), `runtime/head_ee5c9bb/play/head_ee5c9bb/log.txt`.
- Slide: VL126 (`2781:6122`).

**Cause** (re-read at HEAD `22bb75f`; `FrontRoomsMapWorld.cs` md5 `07ce188a…`, unchanged since `b5f381f`, 10-04).

- `FrontRoomsMapWorld.cs:3209`: `keyGlow = Own(FrontRoomsSurfaces.Lit("Map test / key", …));`.
- `FrontRoomsSurfaces.Lit` (`FrontRoomsSurfaces.cs:86–102`) returns a material from a **static** cache (`Cache`, line 17) that outlives the scene. Line 88 hands back the cached one if it is not yet destroyed.
- `Own` (`MapWorld.cs:693`) puts it on the map's list, and `Release()` (`:706`, from `OnDestroy`) destroys every owned material with a deferred `Destroy` (`Kill`, `:740–744`).
- On restart, the new map's `Awake` takes the cached material before the old map's deferred `Destroy` runs. Then it is destroyed under the new map, and every key loses its material.
- Introduced in `f5e99e10` (2026-10-02, `git log -S`). Not Codex.

**Contract patch for the map chat** (`runtime/head_ee5c9bb/patches/R2_key_material_contract.git.diff`; map-owned file, not applied to Red's project). It is a real `git apply` patch against HEAD; `git apply --check` passes on a copy of HEAD's file. The code line is exactly the one verified in `W/proj_cx_fix`; the patch adds two comment lines.

```diff
-        keyGlow = Own(FrontRoomsSurfaces.Lit("Map test / key", new Color(.96f, .87f, .23f), .4f, 0f, new Color(.96f, .87f, .23f) * .8f));
+        keyGlow = Own(new Material(FrontRoomsSurfaces.Lit("Map test / key", new Color(.96f, .87f, .23f), .4f, 0f, new Color(.96f, .87f, .23f) * .8f)));
```

- The same pattern sits on the fallback paths (`MapWorld.cs:3172`, `3190–3192`, `3207–3208`). They run only when a Resources material is missing, and no run hit them. Wrap them the same way when the map chat next touches the method (not in the patch, because no run verified them).
- The visual side cannot fix this in `FrontRoomsSurfaces.Lit`: the material is still alive when the new map takes it.
- The older `runtime/head_279c144/patches/R2_key_material_contract.diff` is a readable note, not a git patch (its hunk header has no line ranges). Use the `.git.diff`.

**Handoff:** 关卡设计 (map chat).

### R3 · minor · The task file still says "cause under audit"

`Documentation/VISUAL_CHAT_TASKS.md:28` (HEAD `22bb75f`) still says "10 'Collection was modified' … and 29 RenderGraph ZBinningJob … (cause under audit)". Red's log had 85 and 253 (`runtime/redlog/editor_prev_sequence.txt`). The cause is R1, and `9e754a2` removed the trigger.

**Fix:** replace the bullet's sub-line with: "85 Collection-was-modified (G14 `DropChunk`, fixed in `9e754a2`) aborted URP's frame record and left Forward+'s ZBinning job uncompleted, which caused the 253 ZBinningJob errors and black frames. Main has no fail-closed guard yet (codex_audit 10_review_runtime R1 / 10_review_rt F1)." Owner: docs (the visual chat's task file).

### R4 · minor · The shipped print array no longer matches its source slice K03

**What.** `FrontRoomsPrintArray.Build()` writes the 8-slice sheet `Assets/Resources/Print/FR_Print_HardEdge.png` from `Tools/print/patterns/out/hard_edge/encoded/K00…K07.png` and records each source's SHA-1 in the importer's `userData` (`FrontRoomsPrintArray.cs:270–283`).

| Slice | SHA-1 recorded in `FR_Print_HardEdge.png.meta:167` (built in `3ff05ee`, 19:22) | SHA-1 of the file at HEAD |
|---|---|---|
| K00, K01, K02, K04–K07 | match | match |
| **K03** | `e2af19a6c70fc45264936d5dfb404f57a8b4ef20` | **`1f048331b53ada33737bb93e9967d7f233d751a5`** |

- `ee5c9bb` (20:22) changed `Tools/print/print_tool.py` (K03 is now "missing plate", from the narrative chat and 平面视觉) and re-encoded `K03.png`. It did not rebuild the sheet, so slice 3 of the shipped `_FR_Print` array is still the old "double repeat".
- **No runtime effect today.** `FrontRoomsSurface.shader` declares `_FR_Print` (`:209`) and samples it only when the global `_FR_PrintClock.w > 0` (`:266`). Nothing under `Assets/Scripts` sets that clock or binds the array: the driver `FrontRoomsPrintDriver.cs` lives only in `Tools/print/unity_staging/`. The game draws the static `_PrintTex` (= K00, unchanged). The P0 gates above test that path.
- It matters when the live print lands: the array would show the old K03, and any Q1b re-run calls `Build()` first, so it would test a different array from the one in main.

**Fix:** the print track rebuilds the array (`FrontRooms > Rendering > Build Print Array`, or `-executeMethod FrontRoomsPrintArray.BuildBatch` in a clone), re-runs its Q1b gates on the new K03, and merges the new sheet + `.meta` through the visual chat. If the K03 change was not meant for the array yet, revert `K03.png` instead. Owner: visual → **q1-live-print**. Not fixed here: binary asset, no runtime break.

## 4. Later commits (after `75cfdff`)

### 4.1 Which Codex errors later commits fixed

None of the runtime errors. The ZBinningJob trigger was already fixed by Codex's own `9e754a2` on 10-03. No later commit, up to HEAD `22bb75f`, touches `FrontRoomsGlassRTSystem.cs` or `FrontRoomsMetalGlassRTRendererFeature.cs`, so the missing guard (R1) is still missing.

### 4.2 Visual-area commits, checked at runtime

| Commit | What | Runtime result |
|---|---|---|
| `9eddc35`, `bf2e883`, `70644f0` (10-04) | G14 prepass pass, baseline markers and RT-only roll gate in `FrontRooms/Glass` | Compile check 28 / 28 OK (`ForwardLit`, GLES3x + Metal), unchanged through HEAD. `FRGlassRTPrepass` compiles and draws at runtime: every `Glass_Window` renderer on prepass bit 30 has the pass (68 at `279c144` / `a5262fb`, 81 at `ee5c9bb`: the walk differs), 0 shader errors in every Play log. Same CBUFFER in both passes. What it does to the picture and its cost: `10_review_rt.md` F2, F3, VL116, VL117, VL119 |
| `663e858`, `1114d6b` (10-04) | Kit importer LOD cull mapping, importer version 2 | Kit LOD dump at `279c144`: window frames (4) and door frames / leaves LOD0→1 at 4.0 m, never culled; mini-blinds 3.0 / 30.0 m; ordinary props unchanged (`runtime/head_a5262fb/extras/kit_lod_dump_279c144.txt`). Matches VL114 (PASS). No import errors. The importer is unchanged since |
| `04c3a6a`, `4448df9`, `b5f381f` (10-04) | `FrontRoomsScreenVideo.Attach` on every Office monitor (`FrontRoomsOfficeKit.cs`); the ads demo scene | 0 `ScreenVideo` / `VideoPlayer` warnings or errors in every Play log, across restarts. The screen's trigger box cannot block the game's use ray: every game raycast passes `QueryTriggerInteraction.Ignore` (`FrontRooms3DGame.cs:1268`, `FrontRoomsMapWalker.cs:140`, `FrontRooms3DGame.Mobile.cs:172`). Player-build material: `10_review_glass-look.md` F6. Serialized RT camera in `FrontRoomsScreenAdsDemo.unity:1997`: `10_review_rt.md` F5 |
| `16f520e`, `5e6e7f9`, `07d2c84`, `a5262fb`, `6c6fe81` (10-07, touch / mobile) | Touch layer, `FrontRoomsHandheld`, HUD branches in `FrontRooms3DGame.cs` / `.Mobile.cs` | Compile 0 errors; suites unchanged; desktop Play runs above. Not visual-owned. Note for the iCloud chat: `16f520e` (re-)added iCloud conflict copies of the FMOD banks (`Assets/StreamingAssets/FMOD/* 2/3/4.bank`) and of `ProjectSettings/ShaderGraphSettings`. They cause no runtime error, but StreamingAssets ships them in every build |
| `3ff05ee` (10-07 19:22) | Q1b hard-edge print: new `_PrintTex` (`Wallpaper_Print_P.png`, GUID kept), `Resources/Print/FR_Print_HardEdge`, `FrontRoomsPrintArray` / `PrintQ1bTest` / `PrintFetch.shader` (editor), `FrontRoomsRenderSetup.cs` + `FrontRoomsPrintP0Test.cs` patches | Compile 0 errors / 0 warnings (all editor code). Glass compile check unchanged. Print P0 gates at HEAD: see §2. Signatures unchanged (the print is a texture, not geometry). 0 texture / shader errors in the Play log. Array provenance: R4 |
| `22bb75f` (10-07 23:46) | Desktop key-row anchor in `FrontRooms3DGame.cs` (applies `10_review_visual.md`'s key-panel contract; map-owned file) + autopilot HUD-on-screen check + touch `.meta` files | Compile 0 / 0; suites, signatures and glass compile check identical to `ee5c9bb`; Play run clean, "AUTOPILOT HUD all panels on screen" in all 4 lives (§2). It fixes a visual-review finding, not a runtime one |
| `ee5c9bb` (10-07 20:22) | Window facade r6 (`FrontRoomsInteractableKit.Window.cs` md5 `58ffb367…`: doc comment, a tools flag `DisableWindowDressForTools`, and the catch path now re-enables the pane renderer), touch view, print tool K03 | Compile 0 errors. Map signatures 0 / 500 chunks differ from `a5262fb`, so r6 hangs the same window parts on every seed. Play run: see §2. K03: R4 |

## 5. Notes for other tracks (not findings of this review)

- **glass-rt-track, cost (`10_review_rt.md` F3).** G14's own stopwatch shows a large first-second spike in `BuildFrame` when the map appears (session max between 14 and 666 ms across runs). All runs ran under heavy load, so I do not report a number. Measure it on an idle machine in the P2 cost stage.
- **glass-rt-track, dead shim.** `FrontRoomsMetalGlassRTController` (`FrontRoomsMetalGlassRT.cs:19`) has no caller left. It can go when the track next touches the file.
- **Harness caveat.** The plugin's per-frame `glassPixels` stat reads 0 on most map frames: it is published only when the frame's stats slot is free, so under load it is not a coverage measure. The RT review measured coverage from pixels instead (VL116).
- **sound chat (pre-existing, not Codex).** Every Play session logs "AudioClip.SetData failed; AudioClip contains no data" from `FrontRoomsAudio.Make` (`FrontRoomsAudio.cs:20`) because Unity audio is disabled (594 lines in Red's log today; the same in the pre-Codex base). Harmless noise; it hides real errors in the console.

## 6. Verification images (Figma, FRONTROOMS · VISUAL VERIFICATION LOG `2595:6093`)

| VL | Slide | Check | Verdict | Images |
|---|---|---|---|---|
| VL085 | `2781:6093` | RT teardown blackout fixed | PASS | `images/vl085_8ef5b64_dropchunk.jpg`, `vl085_8ef5b64_with_guard.jpg`, `vl085_head_rt_high.jpg`, `vl085_head_rt_off.jpg`, `vl085_precodex_7320ed1.jpg` |
| VL126 | `2781:6122` | Zone key turns pink after R | FAIL | `images/key_after_r_head.jpg`, `key_after_r_precodex_7320ed1.jpg`, `key_after_r_contract_patch.jpg` (+ `key_first_life_head.jpg` beside it) |

Both rows are recorded in `Documentation/VERIFICATION_LOG.md` §3. The `a5262fb` and `ee5c9bb` re-runs confirm both verdicts with the same kind of frames, so per §1.1 and Red's merge rule (c) they get no new slide. Their frames stay in `runtime/head_*/play/`.

## 7. Files written by this review

- This file.
- `runtime/head_ee5c9bb/` (this pass): `compile/` (Unity excerpt, per-target Roslyn summary), `suites/` (all suite reports, `map_signatures.tsv`, glass compile check, comparisons with `a5262fb` and `7320ed1`), `play/head_ee5c9bb/` (summary, stats, log, console head, frames), `extras/` (print P0), `patches/R2_key_material_contract.git.diff` + `README.txt`, `redlog_20261007_session.txt`.
- `runtime/head_22bb75f/` (this pass, after HEAD moved): `compile/`, `suites/` (reports, signatures, comparisons with `ee5c9bb`), `play/head_22bb75f/` (summary, stats, log, console head, frames, Unity-log counts).
- `runtime/head_3ff05ee/compile/` (19:41 pass).
- `runtime/head_a5262fb/`: `compile/`, `suites/`, `play/head_a5262fb/`, `extras/` (Relay director policy, kit LOD dump).
- `runtime/head_279c144/`: `play/{cx_rton,cx_rtoff,cx_keys,base_restart,base_keys,fix_keys}/`, `suites/{head,base}/`, `suites/*_vs_*.txt`, `compile/`, `patches/R1_tracepass_guard.diff`, `patches/R2_key_material_contract.diff`.
- 2026-10-04 evidence, unchanged: `runtime/{play,suites,compile,redlog,harness}/`.
- `images/vl085_*.jpg` (5), `images/key_*.jpg` (4).
