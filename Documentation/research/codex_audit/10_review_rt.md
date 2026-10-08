# Codex audit · 10 · Review: G14 ray-traced glass (RT)

Date: 2026-10-07, 16:30–18:20 PDT. Reviewer: the codex-audit RT review stage (re-run after the 2026-10-05 wipe; the 2026-10-04 attempt stopped before it wrote this file).
Read-only on Red's project, except this file, `images/rt_head_*`, `runtime/rt/*` and four rows in `Documentation/VERIFICATION_LOG.md`. Unity was never opened on Frontrooms3D. Nothing was committed.

**What was reviewed:** the CURRENT main. The runs used HEAD `5e6e7f9` (2026-10-07 16:58). Re-checked at 18:15 against HEAD `a5262fb` (17:25):
- `git diff 5e6e7f9 a5262fb` touches no G14 file (scripts, shaders, `Editor/RT`, `Editor/Rendering`, renderer asset, plugin, `NativePlugin/`, `FrontRoomsGlass.shader`, the ads-demo scene). `git status` shows no local edit to any of them. So every result below holds for `a5262fb`.
- The F1/F5 patch (`runtime/rt/rt_failclosed.diff`) applies to `a5262fb` cleanly (`patch --dry-run`). Patched `a5262fb` is byte-identical to the verified clone `W/proj_cx_rt`.

**Re-check 2026-10-07 23:2x against HEAD `ee5c9bb` (20:22)** (this stage's retry; no new Unity run):
- `git diff a5262fb ee5c9bb` touches no G14 file. The three new commits are touch controls, the print array (`FrontRoomsPrintDriver.cs`, `Editor/Rendering/FrontRoomsPrint*`), `FrontRoomsInteractableKit.Window.cs` and docs. `git status` shows no local edit to a G14 file.
- Hashes at HEAD: `FrontRoomsGlassRTSystem.cs` md5 `e75df1b1…` (1,531 lines), `FrontRoomsMetalGlassRTRendererFeature.cs` md5 `c2406ca9…`. Every file:line cited below was re-read at HEAD and still holds.
- `rt_failclosed.diff` (apply with `patch -p0`) applies to HEAD cleanly. HEAD + that patch = `W/proj_cx_rt` byte for byte (System `8d592671…`, Feature `4059937f…`). The visual review's `vis_F2_rt_print_albedo.diff` (`patch -p1`) applies on top of it cleanly, so the two RT patches do not conflict (§4.5).
- Red's own editor ran the map today (§4.5): 0 RT errors.

The RT code itself has not changed since Codex's `75cfdff` (2026-10-03). Three later Codex commits changed the glass shader that RT feeds: `9eddc35`, `bf2e883`, `70644f0` (2026-10-04).

**Where the evidence comes from:**
- **Clones** (`W` = `/Users/redwang/FrontRoomsVisualWork`):
  - `W/proj_cx_rt`: main `279c144` + the audit tools. The RT files equal `5e6e7f9`.
  - `W/proj_cx_rt2`: the same, for the G10 proof and the compile check.
- **Probe:** `CxRtPlayProbe`, clone only. Copy: `runtime/rt/CxRtPlayProbe.cs.txt`. It plays `FrontRooms3D.unity` exactly as Red's game is now, with seed 516574485.
- **Logs:** `W/logs/proj_cx_rt_*.log`. Probe logs: `runtime/rt/play_log_high.txt`, `walk_frames_high.tsv`.
- **Load warning.** The machine was very busy during every run: load average 480–860, with 29 Unity batch jobs and 4 Blender jobs from other workflows. So:
  - G14's own counters (pixel counts, its main-thread stopwatch, error counts) are trustworthy;
  - whole-frame CPU and GPU times are not, and this review does not judge them.

---

## 0. Short answer (for Red)

- **G14 now really draws in your game.**
  - On 2026-10-04 Codex merged the missing glass prepass into `FrontRooms/Glass` (`9eddc35`). Since then, ray-traced reflections replace the old cube reflection on every map window.
  - Head-on at 1.5 m, 71,919 pixels change visibly (≥ 8/255). A second RT Off render changes 0 pixels.
  - So the 2026-10-04 finding "G14 adds nothing" (VL086) is fixed.
  - **But nobody has checked the look or the cost yet:**
    - none of the design's P0/P1 acceptance tests has run on this code;
    - Codex's own shader proof left one failure open. It is still open (§4.3).
- **The main thread pays 2.8 ms per frame whenever a window is in view.** The budget is 0.3 ms.
  - When chunks stream in, it pays up to 20.6 ms.
  - At 60 fps a frame has 16.7 ms, so G14 takes 17 % of the main thread, plus streaming hitches.
- **One crash path is still open.**
  - Codex fixed the bug that blacked out your game on 2026-10-03 (`9e754a2`): 85 + 253 errors and black frames. But it fixed only that one bug.
  - Any other exception in G14's per-frame code still freezes or blacks out the frame and repeats the ZBinningJob errors every frame.
  - I proved this on today's main by injecting a fault.
  - A 20-line guard fixes it. It is verified in a clone (§3 F1).
- **Checked and fine:**
  - the shipped plugin is byte-identical to a fresh build of main's sources (sha1 `56a35164…`);
  - its install name is `@rpath`;
  - the plugin is enabled for the macOS Editor and macOS player only;
  - WebGL, Windows and iOS player scripts contain no RT code;
  - Codex's `DropChunk` fix and its `Ensure()` removal are correct;
  - your own editor log since 2026-10-05 shows 0 RT errors.

## 1. Findings

| ID | Severity | Finding | Owner → handoff |
|---|---|---|---|
| **F1** | major | No fail-closed guard. An exception in `BuildFrame` during URP's record freezes or blacks out every frame (proved by fault injection on today's main) | visual → glass-rt-track; ready patch |
| **F2** | major | G14 is live by default and changes Red's window glass (since `9eddc35`), but none of P0-A1…A13 / P1-B1…B8 has run. The shader proof has 1 open fail | visual → glass-rt-track (verify stage) |
| **F3** | major | Main-thread cost: every traced frame > 1.5 ms (p50 2.76 ms, p99 8.0 ms, max 20.6 ms). Budget ≤ 0.3 ms p99; P0-A7 ≤ 0.5 / 1.5 ms | visual → glass-rt-track (P2) |
| **F4** | minor | Native scene not released at Play-mode exit: 1,646 meshes, 4.95 MB BLAS and 8.31 MB geometry stay in the plugin; 0 plugin events after exit | visual → glass-rt-track |
| **F5** | minor | `OptIn` adds a `FrontRoomsGlassRTCamera` to edit-mode cameras, so it gets saved into scenes (`FrontRoomsScreenAdsDemo.unity:1997`). It is a missing script off macOS | visual (patch) + ads-demo owner (re-save the scene) |
| **F6** | minor | iOS / Android / Windows builds keep G14 shader code. Both strippers act only when the target is WebGL | visual → glass-rt-track |
| **F7** | minor | Codex's G14 task row is wrong in two places: "Metal toolchain unavailable" and "the map opts the camera" | docs |
| **F8** | major | **Same defect as `10_review_visual.md` V2, listed here for this track (one fix, do not count twice).** G14 traces `_FR_PRINT` wallpapers with the paper code as albedo, so Level 0 walls reflect dark red in window glass | visual (fix phase: `vis_F2_rt_print_albedo.diff`) → glass-rt-track (real paper × print in the hit shader) |

Details and evidence follow in §3. Checks that passed are in §2. The list of unfinished work is in §5.

## 2. Checks that passed (no finding)

| Check | Evidence |
|---|---|
| **Dylib = promoted sources** | Rebuilt with main's own `NativePlugin/build_frontrooms_metal_glass_rt.sh` in a scratch copy of `NativePlugin/` (Apple clang 21.0.0, OS Metal compiler; no offline Metal toolchain needed). `frglassrt_validate`: MSL compiled in 3,335 ms, all 7 pipelines OK, layout self-test OK (FRInstance 128, FRMaterial 160, FRLamp 64, FRFrame 496). Output sha1 **`56a351646e940ae10154c1f89dae46a5fb76c0f3`** = main's `Assets/Plugins/macOS/libFrontRoomsMetalGlassRT.dylib` (201,280 B), byte for byte. Repeated at 18:16 from `git archive a5262fb NativePlugin` into the session scratchpad: same sha1 (MSL 1,020 ms, 7/7 pipelines, layout OK) |
| **Install name, no absolute paths (R30)** | `otool -L`: `@rpath/libFrontRoomsMetalGlassRT.dylib`, links only system frameworks (Metal, Foundation, libobjc, libc++, libSystem, CoreFoundation). `strings` finds 0 paths into `/Users`, `/private` or iCloud. arm64 only, minos 13.0. 19 `FRGlassRT_` exports plus `UnityPluginLoad` / `UnityPluginUnload` |
| **Plugin `.meta` platforms** | `libFrontRoomsMetalGlassRT.dylib.meta`: `Any` off; `Editor` on (OS OSX, CPU ARM64); `OSXUniversal` on (ARM64); nothing else; `isPreloaded: 1`. Before Codex it held only a GUID, which meant the default "any platform" importer, so Codex's edit (class E) was right. `FrontRoomsGlassRTImporter.Configure` pins the same settings on every re-import |
| **Other platforms compile, no RT code** | Player-script compile per target in `proj_cx_rt2`: see §2.1 |
| **Camera opt-in at runtime** | `FrontRoomsMetalGlassRTRendererFeature.cs:50–54`: passes are queued only in Play Mode, on Metal, for a `CameraType.Game` Base camera with a `FrontRoomsGlassRTCamera` (`id ≥ 0`), and with quality ≠ Off. In Play the only opted-in camera is the game camera (`FrontRooms3DGame.cs:359`). Edit-mode callers get a component but never trace; that is F5 |
| **ZBinningJob: missing job dependency?** | No. `TracePass` reads only `resources.cameraDepthTexture` (`UseTexture`, line 163) and its own imported targets. It never touches URP's light or cluster data. The ZBinningJob error is a symptom: URP's `ForwardLights.PreSetup` schedules the Forward+ jobs at record time (`ForwardLights.cs:418`), and the `SetupLights` pass completes them only at execute time (`:508`). When an exception aborts the record after `PreSetup`, nothing completes the job, and the next frame's `MemClear(m_ZBins)` (`:404`) throws. Reproduced in F1 |
| **`DropChunk` fix (`9e754a2`)** | `FrontRoomsGlassRTSystem.cs:985–993` iterates a snapshot (`new List<Inst>(c.byRenderer.Values)`) while `RemoveInst` edits the dictionary. Correct. 2026-10-04 runtime repro (VL085): the old line gives 6,825 failed frames in 120 s; HEAD gives 0. Today: 0 "Collection was modified" in 900 + 900 walk frames |
| **`75cfdff` (map-owned `FrontRoomsMapWorld.cs`)** | It removed the `FrontRoomsMetalGlassRTController.Ensure()` call from `Awake`. This is word for word G14 contract C1 (`rt/10_rt_glass_design.md` §2.4). The map chat confirmed it is not theirs and accepted it (`NOTE_map_chat_attribution.md`); `10_review_map-edits.md` hunk 2 agrees. Nothing else calls `Ensure()`: `git grep` finds only the `[Obsolete]` shim itself (`FrontRoomsMetalGlassRT.cs:22`). The system starts from `FrontRoomsPostStack.ConfigureCamera`. Process note: a map-owned file was edited without a contract, but the edit itself is correct and needs no follow-up |
| **SRP Batcher compatibility of `FrontRooms/Glass` after `9eddc35`** | The `UnityPerMaterial` CBUFFER of `ForwardLit` (lines 131–164) and of `FRGlassRTPrepass` (551–582) hold the same 30 fields in the same order. The only difference is two hook-marker comments |
| **Domain reload / callbacks** | Domain reload on entering Play is on (Red's `Editor.log`: "Reloading assemblies for play mode"). `ResetStatics` (`[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`, `FrontRoomsGlassRTSystem.cs:1518`) clears every registry and `started`, and the next `EnsureStarted` pushes `FRGlassRT_ResetScene`. The plugin calls no managed code: no delegate is passed to native, and every `addCompletedHandler` block stays native. So nothing dangles across a reload |
| **Red's editor today** | `~/Library/Logs/Unity/Editor.log` (session 2026-10-05 20:43, two Play entries): `[FrontRoomsGlassRT] plugin loaded · caps 0x31ff · on: Apple9, High, kernel 9 ms (async), layout OK, residency set`; 0 ZBinningJob, 0 "Collection was modified", 0 "Render Graph Execution error", 0 exceptions |

### 2.1 Player-script compile per target (clone `proj_cx_rt2`, `CxRtCompileCheck`)

**How.** `CxRtCompileCheck.Run` (in `runtime/rt/CxRtEditCheck.cs.txt`) calls `PlayerBuildInterface.CompilePlayerScripts` once per target. It does not switch the target and does not build. Then it counts G14 names inside each compiled `Assembly-CSharp.dll`. The clone's G14 files equal HEAD. Log: `W/logs/proj_cx_rt2_compilecheck.log` (lines 3137, 5744, 8273, 9246). Result: `runtime/rt/compile_check.txt`.

| Target | Assemblies | `Assembly-CSharp.dll` | `error CS` | `FRGlassRT_` | `FrontRoomsGlassRTSystem` | `FrontRoomsGlassRTCamera` | `TracePass` |
|---|---:|---:|---:|---:|---:|---:|---:|
| WebGL | 22 | 555,008 B | 0 | 0 | 0 | 0 | 0 |
| StandaloneWindows64 | 23 | 555,008 B | 0 | 0 | 0 | 0 | 0 |
| iOS | 23 | 560,128 B | 0 | 0 | 0 | 0 | 0 |
| StandaloneOSX | 23 | 608,768 B | 0 | 19 | 9 | 3 | 2 |

- **WebGL, Windows and iOS players compile with 0 errors and hold no G14 code:** no P/Invoke entry, no RT system, no camera component, no pass. The renderer feature class compiles there with an empty body, so the shared renderer asset has no missing script.
- **The macOS player holds all of it** (the 19 `FRGlassRT_` P/Invoke names = the 19 dylib exports).
- **WebGL rule.** G14 adds nothing to WebGL scripts. WebGL shaders lose the `_FR_GLASS_RT` variants and the prepass (both strippers act on WebGL). Codex's change to `FrontRooms_URP_Renderer.asset` is one line: the feature (already there before Codex) gets `prepassShader: {guid: 9e07ae8a…}`. The field is serialized on every platform (`Feature.cs:34`), so the shader is referenced on WebGL too, but the WebGL stripper empties it. So the WebGL build is unchanged by this track. On iOS and Windows the same reference keeps the shader: F6.
- **Note.** On Windows and iOS the `FrontRoomsGlassRTCamera` class does not exist. That is why F5 matters.
- The "exit code: 1" at log line 267 is Unity's Android `adb` probe. It is not the compile.

## 3. Findings in detail

### F1 · major · No fail-closed guard around the per-frame RT build

**What.** `TracePass.RecordRenderGraph` calls `FrontRoomsGlassRTSystem.BuildFrame(...)` (`FrontRoomsMetalGlassRTRendererFeature.cs:143`), plus `EnsureTargets` / `SendTargets` (`:138–139`), with no try/catch.
- That code runs inside URP's RenderGraph record of the game camera.
- An exception there aborts the camera's whole frame. It also leaves Forward+'s ZBinningJob uncompleted, so the next frames throw too.
- Design 10 §2.7 asks for the opposite: "fail closed: weight 0, keyword off, one log line, no further native calls".

**What Codex did.** `9e754a2` fixed the one trigger Red hit on 2026-10-03, the `DropChunk` enumeration. It added no guard.

**Evidence on today's main** (clone `proj_cx_rt`, probe phase `fault`, log `runtime/rt/play_log_high.txt` frames 1589–1610):
- **The fault.** I set `FrontRoomsGlassRTSystem.instBuffer` to null with reflection, so `BuildFrame` throws a `NullReferenceException` at `FrontRoomsGlassRTSystem.BuildFrame [0x0041a] ← TracePass.RecordRenderGraph`.
- **12 of 12 renders produced no new frame.**
  - Mean luma was 76.23 on every render, and before (with grain) it varied 76.23–76.91.
  - `rt_head_fault_before.jpg` and `rt_head_fault_during.jpg` are the same file: md5 `198d2cf5…` for both PNGs.
  - A camera that renders to the screen shows black in this state: on 2026-10-04 (VL085) the game camera's frames had mean luma 0.0.
- **Errors.** There were 24 new errors: 12 "Render Graph Execution error", 3 `NullReferenceException` and 9 `InvalidOperationException: The previously scheduled job ZBinningJob writes to … ZBinningJob.bins`. That is the same signature as Red's 2026-10-03 log, where each exception was followed by 3 ZBinningJob errors.
- **Quality stayed High**, so the next frame tries again and fails again.
- **Recovery.** After the buffer was restored, frames rendered again (luma 76.02–76.97).

**The same fault with the guard applied** (clone only; `runtime/rt/play_log_fault_patched.txt`; VL118 `2778:7218`; `images/rt_head_fault_chart.png`):
- 2 errors in all: the `NullReferenceException`, logged once, and `[FrontRoomsGlassRT] off for this session: NullReferenceException while building the RT frame (fail closed, design 10 §2.7)`;
- 0 ZBinningJob errors and 0 Render Graph errors;
- every render is live (luma 76.63–77.48);
- quality is now Off;
- the game camera still had its RT component in Play (`game camera has FrontRoomsGlassRTCamera True`).


**Fix (visual-owned files; patch verified in the clone).** Script: `runtime/rt/rt_failclosed_patch.py.txt`. Diff: `runtime/rt/rt_failclosed.diff`.
- `FrontRoomsMetalGlassRTRendererFeature.cs`: wrap `EnsureTargets` / `SendTargets` / `BuildFrame` in try/catch. On an exception, call `FrontRoomsGlassRTSystem.FailClosed(e)` and return before any RT pass is added. The already-queued `EndPass` keeps the keyword off and the weight at 0.
- `FrontRoomsGlassRTSystem.cs`: add `FailClosed(Exception)`. It logs the exception once plus one error line, then sets quality Off for the session.
- "For the session" relies on domain reload. Red's project reloads on entering Play (`EditorSettings.asset`: `m_EnterPlayModeOptionsEnabled: 1`, `m_EnterPlayModeOptions: 0`), so the next Play starts at the default quality again. `ResetStatics` (`:1518–1529`) does not clear `qualityOverride`. If domain reload is ever turned off, the track should add `qualityOverride = null;` there.

**Handoff.** glass-rt-track should land this first, before any other G14 work. It changes nothing when no exception is thrown. It is not breaking Red's game today, because no trigger is known on HEAD, so the codex-audit fix phase may leave it to that track.

### F2 · major · G14 is live in Red's game and unverified

**What changed after Codex's promotion.**
- On 2026-10-03, main's glass shader had no `FRGlassRTPrepass` pass, so G14 traced every frame but covered 0 px (VL086, FAIL).
- On 2026-10-04, Codex merged the G-1/G-2/G-3 hook from the pre-wipe `proj_rt` copy of the shader (`9eddc35`, Codex session `rollout-2026-10-04T09-26-25…`).
- It then wrapped the new blocks in `[G14-HOOK]` markers (`bf2e883`) and gated the roll attenuation by traced coverage (`70644f0`).

**Evidence that it now draws** (VL116; probe views; `images/rt_head_v2_headon_diff_stats.json`, `rt_head_v5_close55_diff_stats.json`):
- 18 `Glass_Window` slabs sit on prepass bit 30, and `FrontRooms/Glass` lists the passes `[ForwardLit, FRGlassRTPrepass]`.
- **Head-on, 1.5 m, RT High:**
  - GlassDepth covers 434,712 px, equal to the hit pixels;
  - 0 misses, 508,202 shadow rays;
  - High vs Off: 71,919 px differ by ≥ 8/255 (3.5 % of the frame), max 144/255, mean ΔY on those pixels −4.4/255;
  - Ultra: 72,138 px.
- **Close, 0.55 m:** 1,314,057 glass px; 281,980 px differ by ≥ 8/255.
- **Noise control (RT Off rendered twice):** 0 px differ by ≥ 8/255 (max 3/255).
- **What it looks like.** In `rt_head_v2_headon_high.jpg` the troffers behind the player now appear in the pane as soft panels, and the near wall shows as a faint dark band. The look has not been judged against the design (P1-B4).

**Why this is a finding.** Default quality follows the quality level (`FrontRoomsGlassRTSystem.cs:82–83`):
- Red's editor is at level 3 → **High**;
- the macOS player default is level 5 → **Ultra**.

So every Play session and every Mac build now shows unreviewed RT output. Nothing has been run against it:
- no P0-A1…A13 or P1-B1…B8 (design §4.1–4.2);
- no `rt/11_p0_p1_report.md`;
- no `rt/images/P0_*`;
- no player build;
- no Metal validation-layer run;
- the acceptance harness `Assets/Editor/RT/FrontRoomsGlassRTVerify.cs` is in main but has never been run there.

**Two related results:**
- **The shader proof is not clean (§4.3).** The re-run on today's shader gives 154/175. The one real open fail is P5 at the dead-lamp window: a depth tag 0.5 m off is not rejected.
- **The window-landing workflow reports a visible problem.** Its VL107 (W1, queued 2026-10-07): "Intact glass in Play" FAILs. Intact vs broken differ by only 3.2 / 2.8 luma at 1 m, against a bar of 12. That run had G14's prepass in main, so the RT path is the first suspect.

**GPU cost could not be judged today.** G14's stage timers show 11.4 ms (High, head-on) and 15.9 ms (High, 0.55 m). Design §3.1 gives 1.3–2.0 ms typical. But whole-frame RT Off varied from 11.9 to 27.2 ms between two identical renders under this load, so these numbers are not evidence either way. P1-B8 must measure on an idle machine and in a player.

**Handoff.** The glass-rt-track verify stage should run `FrontRoomsGlassRTVerify` (P0/P1) on current main, judge P1-B4 by eye, and close the P5 fail.
- Whether G14 stays on by default until then is Red's call.
- His standing rule (highest spec) and design §2.6 both say on.

### F3 · major · Main-thread cost is 5–10× over budget

**Evidence** (VL117; `images/rt_head_walk_mainthread.png`; `runtime/rt/walk_frames_high.tsv`). The run: 900-frame ping-pong walk at 8 m/s over 91 cells (270 m, 45 chunk roots), RT High (the default). All numbers below are from G14's own stopwatch inside `BuildFrame`:

| Frames | n | p50 | p95 | p99 | max |
|---|---:|---:|---:|---:|---:|
| A window in view (traced) | 727 | 2.78 ms | 5.10 | 8.03 | 20.59 |
| Traced, no mesh upload | 699 | 2.76 | — | 5.23 | — |
| Streaming (≥ 1 mesh upload) | 28 | 6.80 | — | — | 20.59 (registration alone 17.04) |
| No window in view | 173 | 0.14 | — | — | 0.74 |

- **Bars.** Design §3.3: ≤ 0.3 ms p99 every frame and ≤ 1.0 ms on streaming frames. P0-A7: ≤ 0.5 ms non-streaming and ≤ 1.5 ms streaming. **All 727 traced frames exceed 1.5 ms.**
- **Split of a traced frame** (`Breakdown`; scene / liveness / candidates / lamps / submit), head-on: 0.002 / 0.105 / 0.683 / 0.098 / 1.141 ms.
  - The cost is the per-frame scan of all ~3,500 registered instances: `foreach (var i in insts)` (`:1137–1152`) plus `SelectNearest`.
  - The other part is building and submitting the frame packet (`:1194–1227`, about 1,700 instances × 96 B).
  - Streaming adds `Rescan` / `UploadCPU`, with up to 4 × `RegisterBudgetMs` per frame (`:1012`) plus whole-mesh copies. That made 1,259 mesh uploads and BLAS builds on this walk.
- **Same order on a quieter machine.** On 2026-10-04 the cost was p50 1.56, p99 5.66, max 11.8 ms (VL087). The 2026-10-04 autopilot run `runtime/play/cx_rton2` gave p50 0.095, p95 1.61, p99 2.32, max 13.99 ms over 7,200 map frames; most of its frames had no window in view.
- **Small per-frame allocations.** `AmbientSH` allocates `new Vector4[9]` and three `float[9]` per traced frame (`:1399`, `:1405`), and `lampCandidates.Sort(lambda)` wraps a comparer (`:1336`). Together about 0.4 KB per frame, small next to the registration copies.

**Fix (design P2 work, not a quick patch):**
- keep a persistent candidate set updated on chunk add/remove, instead of the full scan;
- refit the TLAS on unchanged membership, or split static and dynamic;
- subscribe to the map's chunk events (contract C3/C5) instead of diff-scanning and classifying by name;
- copy meshes with `AcquireReadOnlyMeshData` or the GPU path;
- write SH into a cached array.

**Handoff:** glass-rt-track.

### F4 · minor · Native scene kept alive after Play-mode exit

**Evidence** (probe exit logging; `play_log_high.txt` last 3 lines):
- **Before exit:** plugin events 1,010, meshes 1,646, BLAS 4.95 MB, geometry pool 8.31 MB, C# camera targets 87.01 MB.
- **Right after Play exits, and again 120 editor ticks later:**
  - the C# camera targets are freed (0 MB);
  - the native side still holds 1,646 meshes, 4.95 MB BLAS and 8.31 MB geometry;
  - 0 plugin events happen after the exit.
- 2026-10-04 run: the same, with 1,637 meshes.

**Why.** Ops are applied only inside a render event (`TraceEvent`, `FrontRoomsMetalGlassRT.mm:399–410`). After exit nothing issues one, so `FreeCamera` / `ReleaseMesh` stay queued. They hold strong (ARC) references to Unity textures and GPU-path vertex buffers (`FRGlassRT_SetTexture` `:953–960`, `UploadMeshGPU` `:862–866`).
- Everything is dropped by the `Reset` op at the next Play's `EnsureStarted`, so it does not grow across sessions.
- The cost is about 13 MB, plus whatever Unity GPU objects the plugin retains, while Red edits.

**Fix:** on `EditorApplication.playModeStateChanged` → `ExitingPlayMode` (editor) and on `Application.quitting`:
1. call `FRGlassRT_ResetScene`;
2. issue one render event: `GL.IssuePluginEvent(EventFunc, …)` or a `CommandBuffer` carrying `FRGlassRT_SubmitWarmup`'s sequence number, so the queue applies.

**Handoff:** glass-rt-track.

### F5 · minor · Edit-mode cameras get a saved RT component

**What.** `FrontRoomsGlassRTSystem.OptIn` (`:193–199`) runs `AddComponent<FrontRoomsGlassRTCamera>()` for any camera given to `FrontRoomsPostStack.ConfigureCamera`, including in edit mode. Edit-mode callers:
- the screen-ads demo scene builder `Editor/FrontRoomsScreenAdsDemoScene.cs:89`;
- `FrontRooms3DGame.CreateCamera` (`:283`, used by Create Scene);
- the Level Designer: `FrontRoomsModulePreview.cs:308` (its eye) and `Editor/FrontRoomsMap/FrontRoomsLevelDesigner.cs:213` (both map-owned callers; the fix is on our side, in `OptIn`);
- four look-dev and capture tools: `FrontRoomsGlassVerification.cs:579`, `FrontRoomsKitLookdev.cs:125`, `FrontRoomsLookdevCapture.cs:52`, `FrontRoomsPrintP0Test.cs:406`.

The component class exists only under `UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX` (`FrontRoomsGlassRTCamera.cs:5`).

**Evidence:**
- **In main.** `Assets/Scenes/FrontRoomsScreenAdsDemo.unity:1997` has `m_Script: {fileID: 11500000, guid: 62e280d753be74a6181a61f7465f64d8}` (`FrontRoomsGlassRTCamera`). It was committed in `4448df9` / `b5f381f` and is listed in EditorBuildSettings, though disabled.
- **Clone check (`CxRtEditCheck`).** A new edit-mode camera given to `ConfigureCamera` gets the component, and the saved scene references its guid once.
- **What it costs.** On a Windows editor, or in an iOS / WebGL / Windows player of that scene, it is a "missing script". The feature already refuses to trace outside Play Mode (`Feature.cs:50`), so the component does nothing useful there.

**Edit-mode check** (`runtime/rt/edit_optin.txt`):
- main as is: `camera has FrontRoomsGlassRTCamera True`, and the saved scene references the guid 1 time;
- with the patch: `False`, 0 times.


**Fix:** the second hunk of `rt_failclosed.diff`: `OptIn` returns when `!Application.isPlaying`. The game camera is configured in `Awake` in Play, so it keeps its opt-in. The ads-demo scene owner must then re-run its builder, or remove the component, and re-save.

### F6 · minor · Non-WebGL, non-Mac builds keep G14 shader code

**What.** Both strippers return at once unless the target is WebGL:
- `Editor/Rendering/FrontRoomsGlassRTStripper.cs:21` (strips `_FR_GLASS_RT` variants);
- `Editor/RT/FrontRoomsGlassRTWebGLStripper.cs:23` (strips the `FRGlassRTPrepass` pass and `Hidden/FrontRooms/GlassRTPrepass`).

So an iOS build (Red ships TestFlight), an Android build or a Windows build keeps code it can never use there:
- the `#pragma multi_compile_fragment _ _FR_GLASS_RT` variants of `FrontRooms/Glass` (`FrontRoomsGlass.shader:105`), which double the glass ForwardLit fragment variants;
- the `FRGlassRTPrepass` pass (`:535`);
- the override prepass shader, kept by the renderer asset's `prepassShader` reference.

None of these platforms has the RT feature (the class body is empty off macOS), so this is dead code. The pixels are not affected.

**Fix:** strip on every target except `StandaloneOSX`. Keep Windows until P3 DXR lands. WebGL and Mac stay exactly as they are.

**Handoff:** glass-rt-track. Tell the touch / mobile session that iOS builds will get smaller.

### F7 · minor · Codex's G14 row is wrong

`Documentation/VISUAL_CHAT_TASKS.md:150` (G14 row, written by Codex) has three problems:
- **"the local Metal toolchain was unavailable".** Wrong. The build script compiles the kernel with the OS Metal compiler at run time. A fresh build today matches main's dylib byte for byte (§2).
- **"The map opts the post-stack camera into `FrontRoomsGlassRT`".** Inaccurate. `FrontRoomsPostStack.ConfigureCamera` opts in every camera it configures (F5), and the map takes no part.
- **"PROMOTED; Metal/runtime acceptance pending".** True, but it should now say that RT is live since `9eddc35` and point to F1–F3.

**Fix:** the docs owner rewrites the row. The G14 row should also point to this file.

## 4. Later commits (after `75cfdff`) touching this track or reviewed with it

### 4.1 Which Codex errors later commits fixed

| Codex error (2026-10-03) | Later commit | State |
|---|---|---|
| G14 traced every frame but drew 0 px: main's glass shader had no `FRGlassRTPrepass` (VL086) | `9eddc35` (Codex, 2026-10-04) | **Fixed.** Glass now changes (F2) |
| Codex promoted G14 without the shader hook its prepass relies on | `9eddc35`, `bf2e883`, `70644f0` | Fixed in code; proof not clean (§4.3) |
| Everything else in this track (no guard, cost, exit, opt-in, strippers) | none | Open: F1, F3–F6 |

### 4.2 `9eddc35` / `bf2e883` / `70644f0` (FrontRooms/Glass G14 hook)

- **Source.** Codex's Oct 4 session took the hook from the pre-wipe `proj_rt` copy of the shader. It said only G-1/G-2/G-3 differed and that a dry-run applied cleanly; I cannot re-check this, because `proj_rt` is gone.
- **Content.** The result matches design §2.1:
  - G-1: the `FRGlassRTPrepass` pass, which writes R32F eye depth and RGBA16F normal + smoothness, clips edge faces and fragments behind the opaque depth, and uses the same grime, roll, palm and crack code as ForwardLit;
  - G-2: `_FR_GlassRTFade`, falling back to 0.15–0.45;
  - G-3: `_FR_GlassRTRollScale`.
- **`bf2e883`** only adds `[G14-HOOK]` markers, so the G10 baseline generator can strip the blocks.
- **`70644f0`** is a real behaviour change. G-3 roll attenuation is now applied only where the RT texel covers this fragment, using the same alpha / depth-tag rule as the reflection resolve. Before, it scaled the roll on every receiver pixel while RT was on.
  - This is consistent with the prepass, which always uses the scaled roll: traced pixels then use the same normal on both sides.
  - It is the change that made Codex's proof pass P2/P3/P5 for most views.
- **SRP Batcher:** fine (§2).
- **WebGL:** the new pass is stripped on WebGL, as before.

### 4.3 G14 hook proof re-run on today's shader (VL119)

The run: `FrontRoomsGlassG10Capture.RunProofBatch` in `proj_cx_rt2`, with `-frGlassRT off`, so the RT feature sets no globals; Codex found that it otherwise contaminates the proof. Settings:
- baseline = main's shader with its 7 hook blocks removed;
- post off, ARGBFloat target, 4× MSAA, exact float compare;
- 15 views.

**Result: 154 / 175 pass.** Metrics: `runtime/rt/g10_proof_metrics_70644f0.txt`. The 21 fails fall into three groups:
- **12 × PROPS, harness fixture.** The harness hides the map panes but not the window kit's `Glass_Window` slab, so "every pane hidden" still shows a receiver. The changed pixels equal P1's pixels in every one of those views.
- **8 × scene not deterministic.** In these views the baseline differs from itself (B ≠ B), or the shader keyword is off, so the hook is compiled out and cannot be the cause.
  - Views: `35_rep_Office_b` and `38_deadlamp_window_oblique50` (B ≠ B and (i)); `window_L0_oblique50` (i), 162 px, max 0.005; `39_dark_beyond` (iii), 174 px, max 0.0003; `42_hutch_cabinet_STAGED` (ii) and (iv), 68 px, max 0.21.
- **1 × open: `38_deadlamp_window_1.5m` P5.** With the depth tag set 0.5 m behind the pane, 428,379 of 444,853 pane pixels still take the RT value. That is exactly as many as P4 (tag = the pane's own depth), so the tag was not rejected.
  - The same view passes P1–P4 and P6.
  - All 9 other window views pass P5.
  - Codex's 2026-10-04 run had the same fail. Its 162/175 evidence lived in `/tmp/frontrooms3d-glass-proof-rollfix-off` and was wiped.
  - Cause not isolated: a harness depth for this pane, or the shader's tag tolerance (`abs(tag − eye) ≤ 0.02 + 0.01 · eye`, read through `half`).
  - **Owner:** glass-rt-track.

### 4.4 `663e858` / `1114d6b` (kit LOD cull) and `04c3a6a` (OfficeKit)

Outside the RT track, and covered by their own reviews:
- **`663e858` / `1114d6b`.** `663e858` is the window-landing r4 patch: the last imported LOD takes the sidecar's `dcull`, and `GetVersion() = 2` forces a re-import. `1114d6b` only records the handoff (`interactables/window_landing/04_promotion.md`, 2 lines). Re-verified today by the glass-look stage: VL114, PASS.
- **`04c3a6a`** adds `FrontRoomsScreenVideo.Attach(monitorObject)` to every Office monitor (`FrontRoomsOfficeKit.cs:674–675`). It is reviewed by the glass-look stage (VL115).
  - RT note: the screen quad is an ordinary `MeshRenderer` under the chunk, so G14 registers it on the next rescan.
  - An Unlit material is traced as emission (`FrontRoomsGlassRTSystem.cs:540`), so the ad also shows in reflections.
  - The video `RenderTexture` takes one texture slot for the session. That is fine.

## 5. Unfinished or unverified G14 work (implement stage interrupted 2026-10-03 17:54)

| Item | State |
|---|---|
| P0-A1…A13, P1-B1…B8 (design §4.1–4.2) | **Not run on the promoted code.** No `rt/11_p0_p1_report.md`, no `rt/images/P0_*` / `P1_*`. The harness `Assets/Editor/RT/FrontRoomsGlassRTVerify.cs` (1,634 lines) is in main, never run there; it still carries the clone-only pane-swap switch (editor only, not in builds) |
| Player build (P0-A12, P1-B8) | None. The macOS player defaults to Ultra (quality level 5), which has never been measured |
| Metal validation layer (P0-A1) | Not run |
| Fail-closed gate, design §2.7 | Missing (F1) |
| Streaming / cost (P2): map events C3/C5/C6/C7, static TLAS, candidate set | Not started; P0 diff-scan and name-based `Classify` still in use (F3) |
| Play-exit release | Missing (F4) |
| Design §4.1 "Delete": `Assets/Shaders/FrontRoomsMetalGlassRTComposite.shader` and the empty `Assets/Scripts/Rendering/MetalGlassReflection/` (+ `.meta`) | Still in main. Both predate Codex (`edfbc92`) and nothing references them (the shader's guid appears only in its own `.meta`). The `[Obsolete]` `FrontRoomsMetalGlassRTController.Ensure()` shim now has no caller and can go too |
| Shader proof | 154/175, 1 open (§4.3); the harness's PROPS check must also hide the kit slab |
| G14b secondary reflections | Design `rt/11_secondary_reflections.md` revised; its check:1 never ran (wiped). Separate workflow (VL110–112 today) |
| Look (P1-B4) and the W1 intact-glass FAIL (VL107) | Not judged; needs eyes on frames |

## 6. Verification images (Figma, FRONTROOMS · VISUAL VERIFICATION LOG 2595:6093)

| VL | Slide | Check | Verdict | Images (`research/codex_audit/images/`) |
|---|---|---|---|---|
| VL116 | `2772:6099` | G14 now traces window glass | FLAG | `rt_head_v2_headon_high.jpg`, `rt_head_v2_headon_off.jpg`, `rt_head_v2_headon_diff_high_vs_off_x32.png` (kept beside: `rt_head_v5_close55_*`, `rt_head_v2_headon_diff_off_vs_off_x32.png`) |
| VL117 | `2772:6116` | G14 main thread on a walk | FAIL | `rt_head_walk_mainthread.png` |
| VL118 | `2778:7218` | An RT exception freezes frames | FAIL | `rt_head_fault_chart.png` (kept beside: `rt_head_fault_before.jpg`, `rt_head_fault_during.jpg`, `rt_head_fault_after.jpg`) |
| VL119 | `2772:6125` | G14 shader hook proof on main | PARTIAL | `rt_head_g10proof_grid.png`, `rt_head_g10proof_sheet.jpg` |

Earlier slides from this track: VL085 (teardown blackout fixed, images lost), VL086 (G14 adds nothing: now superseded by VL116), VL087 (cost on 2026-10-04).

## 7. Files written by this review

- This file.
- `images/rt_head_*`: 15 images and 2 stats JSON files.
- `runtime/rt/`: `CxRtPlayProbe.cs.txt`, `diffs.py.txt`, `walkchart.py.txt`, `g10grid.py.txt`, `play_log_high.txt`, `walk_frames_high.tsv`, `g10_proof_metrics_70644f0.txt`, `rt_failclosed_patch.py.txt`, `rt_failclosed.diff`, `faultchart.py.txt`, `CxRtEditCheck.cs.txt` (also holds `CxRtCompileCheck`), `edit_optin.txt`, `play_log_fault_patched.txt`, `compile_check.txt`.
- No file in Red's project outside `Documentation/` was changed. Nothing was moved to `quarantine/`.
- `Documentation/VERIFICATION_LOG.md`: rows VL116–VL119, the section height (30 rows) and the next free number, plus a cover note.
- Figma: slides VL116–VL119 and the cover counts. The codex-audit legend now reads `codex-audit · VL080–119 · 9 checks`, because the exact list did not fit in 426 px.
