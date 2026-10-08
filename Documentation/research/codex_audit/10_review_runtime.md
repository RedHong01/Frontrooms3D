# Codex audit · 10 · Review: runtime (compile, tests, console)

Track: **runtime**. Written 2026-10-07 by the codex-audit workflow (continuation run; this stage restarted twice after usage limits, so it merges three passes: 2026-10-04 on main `d610d3a`, 2026-10-07 16:33–17:46 on main `279c144`, and 2026-10-07 18:22–19:xx on main `a5262fb`).

Read-only on Red's project, except this file, `runtime/head_279c144/*`, `runtime/head_a5262fb/*`, eight images in `images/` and two rows in `Documentation/VERIFICATION_LOG.md` (VL085, VL126). Unity was never opened on Frontrooms3D. Nothing was committed. No file in Red's project was fixed by this review: both findings are handoffs (§1).

## Where the runs happened

| Clone (W = `/Users/redwang/FrontRoomsVisualWork`) | Code | Used for |
|---|---|---|
| `W/proj_cx` | main `a5262fb` + Red's working tree, synced 18:22 (before that: `279c144`, synced 16:33) | compile, suites, signatures, 120 s Play runs |
| `W/proj_cx_base` | `git archive 7320ed1` (the pre-Codex base) over the `proj_audit` Library | the same suites, signatures and Play runs, as the control |
| `W/proj_cx_fix` | `proj_cx`@`279c144` + the R2 map contract patch (clone only) | proves the R2 fix |
| `W/proj_cx_t` | main `a5262fb` (extras clone) | Relay director policy tests, print P0 gates, kit LOD dump |
| 2026-10-04 clones (wiped by the 10-05 reboot; outputs saved in `runtime/play`, `runtime/suites`) | `d610d3a`; `d610d3a` + 8ef5b64's old `DropChunk` line (`proj_cx_repro`); the same + a try/catch (`proj_cx_repro2`); `7320ed1` | the ZBinningJob reproduction |

- The task asked for `proj_landcheck` (synced 17:45 on 10-03, before Codex) as the control. The reboot wiped it. `proj_cx_base` is the same code: `git archive 7320ed1`, Codex's base.
- Harnesses (clone only, never in Red's project): `FrontRoomsCxSuites` (the project's own edit-mode suites + map signatures for 20 seeds) and `FrontRoomsCxPlayRun` (batch Play Mode with graphics, Metal, M3 Max, through the title into the map, game time fixed at 60 Hz, R restarts the game's own way). Copies: `runtime/harness/*.cs.txt`; installed from `W/tools` (W/TOOLS.md §3).
- The machine was very busy during every 10-07 run: load average 500–950 (20–30 other Unity batch jobs). Counts and pass/fail do not depend on load. Millisecond timings do; this review compares none of them across runs.

## 0. Short answer (for Red)

- **Main compiles.** 0 errors. 0 warnings in visual-owned files. The only 4 warnings are in the touch file (`FrontRoomsTouchControls.cs`, touch session).
- **Every project test passes, same as before Codex.** 9 suites: 468 checks plus the Relay nav test (60 / 60 hunts). Map interaction has 7 more tests than before Codex (143 vs 136; the map chat added them in `956b786`), all passing. The Relay director policy has 5 / 5.
- **The map is deterministic.** 20 seeds × 25 chunks: each seed built twice and each chunk rebuilt once, 0 differences. Main today builds exactly what main built on 10-04 (0 / 500 chunks differ).
- **Your game runs clean today.** 120 s of map play, RT on and RT off, with caught restarts: 0 exceptions from game code, 0 "Collection was modified", 0 ZBinningJob, 0 Render Graph errors, 0 shader errors.
- **What the ZBinningJob errors were.** They were not a URP or Forward+ bug, and they did not exist before Codex. G14 (promoted by Codex) threw "Collection was modified" inside URP's frame recording. That aborted the frame after Forward+ had started its light job and before the step that finishes it, so the next frames threw ZBinningJob errors and went black. Codex's `9e754a2` removed that one trigger, and it is fixed.
- **But nothing stops the next one (R1).** Any other exception in G14's per-frame code would black out your game the same way. The RT review already has a verified 20-line guard (`10_review_rt.md` F1). One patch, owned by glass-rt-track.
- **Codex's two fixes are complete.** No other loop in G14 changes the collection it walks. Nothing calls the removed `Ensure()` path. Both warnings it targeted are gone.
- **New, and not Codex's (R2):** after you restart with R (or get caught), the zone keys draw magenta. The bug is in `FrontRoomsMapWorld.cs` since `f5e99e10` (2026-10-02). A one-line map contract fixes it, and is verified in a clone.

## 1. Findings

| ID | Severity | Finding | Owner → handoff |
|---|---|---|---|
| **R1** | major | ZBinningJob root cause: an exception escaping G14's `TracePass.RecordRenderGraph` aborts URP's record after Forward+ scheduled its ZBinning job. Codex's `DropChunk` trigger is fixed; main still has no guard, so any future G14 exception blacks out every frame again | visual → **glass-rt-track** (same patch as `10_review_rt.md` F1) |
| **R2** | major | Zone keys draw magenta after R / a caught restart: `MapWorld.BuildMaterials` owns (and later destroys) a material from `FrontRoomsSurfaces.Lit`'s shared static cache. Pre-existing (`f5e99e10`, 10-02), not Codex | map (关卡设计) → contract patch `runtime/head_279c144/patches/R2_key_material_contract.diff` |
| **R3** | minor | `VISUAL_CHAT_TASKS.md:28` still says "10 Collection … 29 ZBinningJob (cause under audit)". Red's log had 85 and 253, and the cause is R1 | docs |

No fix was applied to Red's project in this review. R1 does not break the game today (0 errors in every run on today's main). R2 is in a map-owned file, so it goes to the map chat as a contract.

## 2. Checks that passed (no finding)

| Check | Result and evidence |
|---|---|
| **Unity batch compile, main `a5262fb`** (`-buildTarget OSXUniversal`) | 0 `error CS`, 0 `warning CS`; Assembly-CSharp and -Editor rebuilt (`runtime/head_a5262fb/compile/unity_batch_compile_excerpt.txt`). Same at `279c144` and at `7320ed1` |
| **Per-target recompile** (Unity's own Roslyn + response files, defines swapped) | editor Mac, editor Win, Editor assembly Mac/Win: 0 errors, 0 warnings. Player Mac and player WebGL: 0 errors, 4 warnings, all in `Assets/Scripts/Input/FrontRoomsTouchControls.cs` (CS0067 ×3 at lines 122/123/125, CS0414 at 174). **0 warnings in visual-owned files** (`runtime/head_a5262fb/compile/csc_variants_summary.txt`) |
| **Codex's warning targets** | Red's 10-03 log had CS0618 (`FrontRoomsMapWorld.cs(490,13)`, the obsolete `Ensure()` call) and CS0414 (`FrontRoomsGlassRTSystem.cs(56,17)`, `loggedOn`) (`runtime/compile/red_editor_prev_warnings.txt`). Both are gone at `a5262fb` |
| **Edit-mode suites** (the project has no NUnit assemblies; these are its batch test harnesses) | Main `a5262fb` vs pre-Codex `7320ed1`: MapVerification 100 / 100 both; RelayNavTest PASS both (60 / 60 hunts, 0 pass-throughs, 0 frames in architecture); MapInteractionTests 143 / 143 vs 136 / 136 (the 7 new tests came with the map chat's `956b786`, not Codex); LevelDesignerTests 138 / 138 both; FixtureTickTests 28 / 28; CameraRigTests 25 / 25; CaptionsTests 9 / 9; GlassShotTests 20 / 20; StreamVerification 5 / 5 (`runtime/head_a5262fb/suites/suites_head_vs_precodex.txt`). The 4 exceptions and 2 errors in the MapInteractionTests console are the tests' own injected faults ("test: a broken glass kit", "tools: forced build failure"), identical in the base |
| **Extra visual / Relay tests** | `FrontRoomsRelayDirectorPolicyTests.Run`: PASS, 5 checks (`runtime/head_a5262fb/extras/relay_director_policy_tests.txt`). Glass shader compile check (`FrontRoomsGlassCompileCheck`): 28 / 28 variants OK, 0 messages, GLES3x + Metal, byte-identical at `279c144` and `a5262fb`. {{PRINT_P0}} |
| **Map signatures, 20 seeds** (`20261001…10`, 516574485, 4242, 1990, 7, 1955, 1993, 8086, 6502, 68000, 2554; root (4992, 0, 4992)) | Each seed built twice: 0 differences. Each of 500 chunks dropped and rebuilt in place: 0 differences. `a5262fb` vs `279c144` vs the 10-04 run on `d610d3a`: 0 / 500 chunks differ (`runtime/head_a5262fb/suites/signatures_a5262fb_vs_279c144.txt`) |
| **Signatures vs pre-Codex** | 492 / 500 chunks differ in renderers, 238 in lights, 491 in colliders. These are the reviewed Codex map content, not runtime faults: wall blocks re-cut around kit windows, kit window frames + `Glass_Window` slabs, the V5 cool lens and +9 % lamp lead (Red keeps it ON, `RED_DECISIONS.md`), `Prop_Glass` / `Prop_BottleBlue` on `FrontRooms/Glass` (`runtime/suites/signatures_cx_vs_base.txt`; owners: `10_review_map-edits.md`, `10_review_glass-look.md`) |
| **Play Mode, main `279c144`, 120 s, RT High (default)** | 7,307 frames, 70 chunk roots torn down. 0 Collection, 0 ZBinningJob, 0 Render Graph errors, 0 shader errors, 0 renderers drawing pink. Frames luma 26–92, 0 % magenta (`runtime/head_279c144/play/cx_rton/`) |
| **Play Mode, main `279c144`, 120 s, RT off** | 2 caught restarts, 135 chunk roots torn down. 0 / 0 / 0. One pink renderer after restart 1 = R2 (`runtime/head_279c144/play/cx_rtoff/`) |
| **Play Mode, main `a5262fb`, 120 s, RT High, forced R at 100 s** | 7,307 frames, 1 forced R, 75 chunk roots torn down. 0 Collection, 0 ZBinningJob, 0 Render Graph errors, 0 shader errors, 0 `NullReferenceException` / `MissingReferenceException`. Frames luma 40–91, 0 % magenta. One renderer draws pink: the key after R = R2 (`runtime/head_a5262fb/play/head_a5262fb/`, Unity-log counts in `unity_log_counts.txt`) |
| **The only console errors in every run are clone-only** | 1 FMOD `BankLoadException` (the clones carry no project-root `FMOD/` folder; W/TOOLS.md §2), 1 `ArgumentOutOfRangeException` in `UnityEditor.Search.SearchDatabase.EnumerateAll` (editor search indexing at start-up, not game code), "port 9264 in use" FMOD warnings (parallel Unity jobs). All three also appear in the pre-Codex base runs |
| **Red's own editor** | `~/Library/Logs/Unity/Editor.log` (session 10-05/06, 11,333 lines): 2 Play entries, `[FrontRoomsGlassRT] plugin loaded · on: Apple9, High`, 0 Collection, 0 ZBinningJob, 0 Render Graph errors, 0 exceptions, 0 `warning CS`. It has 0 `[FrontRooms3D] START` lines, so that session never entered the map; the batch runs above cover the map |
| **Codex's `9e754a2` is complete** | `FrontRoomsGlassRTSystem.cs:989` iterates a snapshot. I read every other loop and every call site of `RemoveInst` in the file (1,531 lines). L870 `GlassBroken` walks `watch.ToArray()`; L981 and L1004 walk a separate `gone` list; L1050 `RefreshStatics` uses an index loop (RemoveInst only nulls a slot); L1106 walks `watch` backwards by index, so removing the current element is safe; L935 `OnMapLost` walks `chunks.Values`, and `DropChunk` never touches `chunks`. The other G14 files (`FrontRoomsGlassRT.cs`, `…Camera.cs`, `…Native.cs`, `FrontRoomsMetalGlassRT.cs`, `…RendererFeature.cs`) have no `foreach` |
| **Codex's `75cfdff` is complete** | It removed MapWorld's `FrontRoomsMetalGlassRTController.Ensure()` call (G14 contract C1; a map-owned file, flagged in `10_review_map-edits.md` hunk 2) and the dead `loggedOn` field. Today nothing calls `Ensure()`: the only match is the `[Obsolete]` shim itself (`FrontRoomsMetalGlassRT.cs:19–23`). G14 still starts from `FrontRoomsPostStack.ConfigureCamera` → `FrontRoomsGlassRT.OptIn` (`FrontRoomsPostStack.cs:74`, game camera `FrontRooms3DGame.cs:283/359`): every RT-on run reports `on: Apple9, High` |
| **The two glass passes agree** | `FrontRooms/Glass` `ForwardLit` and `FRGlassRTPrepass` declare the same 32-line `UnityPerMaterial` CBUFFER, so the SRP Batcher stays on; 0 "not SRP Batcher compatible" lines in any log |

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

Sources: `runtime/play/*/summary.txt`, `runtime/head_279c144/play/*/summary.txt`, `runtime/head_a5262fb/play/head_a5262fb/summary.txt`. Slide: VL085 (`2781:6093`).

So:

- The errors did not exist before Codex (the base has no G14).
- RT off has none.
- `9e754a2` removed the trigger Red hit.

**What is still wrong.** Main still calls `BuildFrame` without a guard (`FrontRoomsMetalGlassRTRendererFeature.cs:143`, unchanged since `8ef5b64`; md5 `c2406ca9…`). The RT review injected a fault on today's main: 12 / 12 frames frozen, 9 ZBinningJob errors (VL118, `10_review_rt.md` F1). Design 10 §2.7 asks G14 to "fail closed".

**Fix (one patch, not two).** `runtime/rt/rt_failclosed.diff` (RT review F1): wrap `EnsureTargets` / `SendTargets` / `BuildFrame` in try/catch, call `FrontRoomsGlassRTSystem.FailClosed(e)` (one log line, RT Off for the session) and return before any RT pass is added. Verified there with the injected fault: 0 ZBinningJob, frames live. My 10-04 `repro_guard` clone proved the same idea against the real `DropChunk` bug. `runtime/head_279c144/patches/R1_tracepass_guard.diff` only points at that patch.

**Handoff:** glass-rt-track (its fix stage owns `FrontRoomsMetalGlassRTRendererFeature.cs` and `FrontRoomsGlassRTSystem.cs`). Not applied here: today's main throws nothing, so it is hardening, not a break.

### R2 · major · Zone keys draw magenta after a restart (map contract)

**What happens.** After R (the game's static restart + `LoadScene`) or a caught restart, the new map's zone keys draw with a destroyed (null) material: Unity's magenta.

- Measured after the first restart, on `279c144`, on `a5262fb` and on the pre-Codex base.
- By the same logic it comes back on every second restart after that: the next map finds the destroyed cache entry and makes a fresh material, and the restart after it hands that one over again. This part is not measured.

**Evidence** (batch Play Mode, seed 516574485, close-up through a copy of the game camera, 1600 × 900):

| Run | Code | Key at 10 s (first life) | Key 1.5 s after R at 12 s | Magenta in the after-R close-up |
|---|---|---|---|---|
| `cx_keys` | main `279c144` | `Map test / key` (URP Lit) | **NULL** | 2.37 % of the frame |
| `base_keys` | pre-Codex `7320ed1` | `Map test / key` | **NULL** | 2.37 % |
| `fix_keys` | `279c144` + the contract patch | `Map test / key` | `Map test / key` | 0.00 %; 0 pink renderers in the scan |

- The natural caught restart does the same: in the RT-off run the key in chunk (8, 1) was NULL 0.4 s after restart 1 (map 29.0 s, `runtime/head_279c144/play/cx_rtoff/log.txt` line 5).
- Logs: `runtime/head_279c144/play/{cx_keys,base_keys,fix_keys}/log.txt` ("KEY SHOT", "BAD MATERIAL").
- Slide: VL126 (`2781:6122`).

**Cause.**

- `FrontRoomsMapWorld.cs:3209`: `keyGlow = Own(FrontRoomsSurfaces.Lit("Map test / key", …));`.
- `FrontRoomsSurfaces.Lit` (`FrontRoomsSurfaces.cs:86–102`) returns a material from a **static** cache (`Cache`, line 17) that outlives the scene. Line 88 hands back the cached one if it is not yet destroyed.
- `Own` (`MapWorld.cs:693`) puts it on the map's list, and `Release()` (`:706`, from `OnDestroy`) destroys every owned material with a deferred `Destroy` (`Kill`, `:740–744`).
- On restart, the new map's `Awake` takes the cached material before the old map's deferred `Destroy` runs. Then it is destroyed under the new map, and every key (`:2874`) loses its material.
- Introduced in `f5e99e10` (2026-10-02, `git log -S`). Not Codex.

**Contract patch for the map chat** (`runtime/head_279c144/patches/R2_key_material_contract.diff`; map-owned file, not applied to Red's project):

```diff
-        keyGlow = Own(FrontRoomsSurfaces.Lit("Map test / key", new Color(.96f, .87f, .23f), .4f, 0f, new Color(.96f, .87f, .23f) * .8f));
+        keyGlow = Own(new Material(FrontRoomsSurfaces.Lit("Map test / key", new Color(.96f, .87f, .23f), .4f, 0f, new Color(.96f, .87f, .23f) * .8f)));
```

- The same pattern sits on the fallback paths (`MapWorld.cs:3172`, `3190–3192`, `3207–3208`). They run only when a Resources material is missing, and no run hit them. The diff lists them; wrap them the same way.
- The visual side cannot fix this in `FrontRoomsSurfaces.Lit`: the material is still alive when the new map takes it.

**Handoff:** 关卡设计 (map chat).

### R3 · minor · The task file still says "cause under audit"

`Documentation/VISUAL_CHAT_TASKS.md:28` says "10 'Collection was modified' … and 29 RenderGraph ZBinningJob … (cause under audit)". Red's log had 85 and 253 (`runtime/redlog/editor_prev_sequence.txt`). The cause is R1, and `9e754a2` removed the trigger.

**Fix:** replace the bullet's sub-line with: "85 Collection-was-modified (G14 `DropChunk`, fixed in `9e754a2`) aborted URP's frame record and left Forward+'s ZBinning job uncompleted, which caused the 253 ZBinningJob errors and black frames. Main has no fail-closed guard yet (codex_audit 10_review_runtime R1 / 10_review_rt F1)." Owner: docs (the visual chat's task file).

## 4. Later commits (after `75cfdff`)

### 4.1 Which Codex errors later commits fixed

None of the runtime errors. The ZBinningJob trigger was already fixed by Codex's own `9e754a2` on 10-03. No later commit touches `FrontRoomsGlassRTSystem.cs` or `FrontRoomsMetalGlassRTRendererFeature.cs`, so the missing guard (R1) is still missing.

### 4.2 Visual-area commits, checked at runtime

| Commit | What | Runtime result |
|---|---|---|
| `9eddc35`, `bf2e883`, `70644f0` (10-04) | G14 prepass pass, baseline markers and RT-only roll gate in `FrontRooms/Glass` | Compile check 28 / 28 OK (`ForwardLit`, GLES3x + Metal). `FRGlassRTPrepass` compiles and draws at runtime: 68 `Glass_Window` renderers on prepass bit 30 have the pass (`cx_keys` summary), 0 shader errors in 5 Play logs. Same CBUFFER in both passes. What it does to the picture and its cost: `10_review_rt.md` F2, F3, VL116, VL117, VL119 |
| `663e858`, `1114d6b` (10-04) | Kit importer LOD cull mapping, importer version 2 | Kit LOD dump at `279c144`: window frames (4) and door frames/leaves LOD0→1 at 4.0 m, never culled; mini-blinds 3.0 / 30.0 m; ordinary props unchanged (`runtime/head_a5262fb/extras/kit_lod_dump_279c144.txt`). Matches VL114 (PASS). No import errors |
| `04c3a6a`, `4448df9`, `b5f381f` (10-04) | `FrontRoomsScreenVideo.Attach` on every Office monitor; the ads demo scene | 0 `ScreenVideo` / `VideoPlayer` warnings or errors in the 279c144 Play logs, across 3 restarts. The screen's trigger box cannot block the game's use ray: every game raycast passes `QueryTriggerInteraction.Ignore` (`FrontRooms3DGame.cs:1268`, `FrontRoomsMapWalker.cs:140`, `FrontRooms3DGame.Mobile.cs:172`). Player-build material: `10_review_glass-look.md` F6. Serialized RT camera in `FrontRoomsScreenAdsDemo.unity:1997`: `10_review_rt.md` F5 |
| `16f520e`, `5e6e7f9`, `07d2c84`, `a5262fb` (10-07, touch/mobile) | Touch layer, `FrontRoomsHandheld`, HUD branches in `FrontRooms3DGame.cs` | Compile 0 errors; suites unchanged; desktop Play run above. Not visual-owned. Note for the iCloud chat: `16f520e` (re-)added 15 iCloud conflict copies of the FMOD banks (`Assets/StreamingAssets/FMOD/* 2/3/4.bank`) and 2 of `ProjectSettings/ShaderGraphSettings`; `git ls-files` now lists 112 such copies. They cause no runtime error, but StreamingAssets ships them in every build |

## 5. Notes for other tracks (not findings of this review)

- **glass-rt-track, cost (`10_review_rt.md` F3).** G14's own stopwatch shows a large first-second spike in `BuildFrame` when the map appears: session max 14.0, 15.8, 602.9 ms (10-04, `cx_rton2–4`), 149.7 ms and 31.2 ms (10-07, `279c144`), 41.0 ms in the first second and 600.4 ms by map 38 s (`a5262fb`). All runs ran under load average 200–950, so I do not report a number. Measure it on an idle machine in the P2 cost stage.
- **glass-rt-track, dead shim.** `FrontRoomsMetalGlassRTController` (`FrontRoomsMetalGlassRT.cs:19–23`) has no caller left. It can go when the track next touches the file.
- **Harness caveat.** In the 279c144 RT-on run the plugin's per-frame `glassPixels` stat read 0 on all 7,200 map frames, while `cx_keys` read > 0 on 45. That stat is published only when the frame's stats slot is free, so under this load it is not a coverage measure. The RT review measured coverage from pixels instead (VL116).

## 6. Verification images (Figma, FRONTROOMS · VISUAL VERIFICATION LOG `2595:6093`)

| VL | Slide | Check | Verdict | Images |
|---|---|---|---|---|
| VL085 | `2781:6093` | RT teardown blackout fixed | PASS | `images/vl085_8ef5b64_dropchunk.jpg`, `vl085_8ef5b64_with_guard.jpg`, `vl085_head_rt_high.jpg`, `vl085_head_rt_off.jpg`, `vl085_precodex_7320ed1.jpg` |
| VL126 | `2781:6122` | Zone key turns pink after R | FAIL | `images/key_after_r_head.jpg`, `key_after_r_precodex_7320ed1.jpg`, `key_after_r_contract_patch.jpg` (+ `key_first_life_head.jpg` beside it) |

Both rows are recorded in `Documentation/VERIFICATION_LOG.md` §3. The `a5262fb` re-run confirms VL085 with the same verdict, so per §1.1 it gets no new slide; its numbers are added to the VL085 row.

## 7. Files written by this review

- This file.
- `runtime/head_279c144/`: `play/{cx_rton,cx_rtoff,cx_keys,base_restart,base_keys,fix_keys}/` (summary, stats, log, console head, frames), `suites/{head,base}/`, `suites/*_vs_*.txt`, `compile/`, `patches/R1_tracepass_guard.diff`, `patches/R2_key_material_contract.diff`.
- `runtime/head_a5262fb/`: `compile/`, `suites/` (reports, signatures, comparisons), `play/head_a5262fb/`, `extras/` (Relay director policy, print P0, kit LOD dump).
- 2026-10-04 evidence, unchanged: `runtime/{play,suites,compile,redlog,harness}/`.
- `images/vl085_*.jpg` (5), `images/key_*.jpg` (4).
