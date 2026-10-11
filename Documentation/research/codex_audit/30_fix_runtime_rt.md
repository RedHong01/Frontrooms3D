# Codex audit · 30 · Fix phase, group "runtime+rt" (G14 ray-traced glass)

Date: 2026-10-08, 19:30–19:50 PDT. Main: **`bc0aa79` (2026-10-08 01:47) + Red's working tree**.
Clone: `W/proj_cxfix_runtime_rt` (made with `W/tools/mkclone.sh`; W = `/Users/redwang/FrontRoomsVisualWork`).
Unity was never opened on Frontrooms3D. Nothing was committed. No file in Red's project was changed, except the docs listed in §7 (this folder only).

## 0. Short answer (for Red)

- **This group changes no game file.** All six findings (F1, F2, F3, F4, F5, COV-1) stay with the **glass-rt-track**. The apply package writes 0 files.
- **Why nothing was fixed here:**
  1. **None of the six breaks or visibly damages your game today.**
     - Your own editor, 2026-10-07 20:38 → 2026-10-08 08:47: 4 Plays with G14 `on: Apple9, High`. 0 RT exceptions, 0 ZBinningJob, 0 Render Graph errors, 0 exceptions of any kind from game code.
     - My two 120 s Play runs on today's main (G14 High): 0 of each too (§3). One of them forces a restart, so the whole map is torn down and rebuilt under RT.
  2. **The glass-rt-track already holds F1, F4 and F5** in its clone `W/proj_rt` (21-file delta on main `ee5c9bb`; report `research/glass/rt/20_implementation.md`). Its verify, fix and merge stages are running now (run `wf_a8b6f723-903`).
     - Its delta rewrites the same two files and the same lines (`FrontRoomsMetalGlassRTRendererFeature.cs`, `FrontRoomsGlassRTSystem.cs`).
     - A second copy of F1/F5 from this group would make its 3-way rebase conflict, and its merge would ESCALATE to a full re-verification.
  3. The review's own handoff for F1 says the audit fix phase can leave it, because no trigger is known on HEAD.
- **What I did change:** five short, marked corrections in `10_review_rt.md`. They fix two wrong claims, as the COV-1 and F2 verifiers asked:
  - Mac players do **not** run G14 today: the plugin is not packaged, so they log `off: plugin not loaded`.
  - The VL107 intact-glass FAIL is **not** an RT symptom.
- **Open risk until the glass-rt-track merges:** no fail-closed guard (F1). If some new exception ever escapes G14's per-frame build, your frames freeze or go black. No known trigger exists today.

## 1. Findings → decision

| ID | Still true at `bc0aa79` + tree? | Change here | Why | Handoff |
|---|---|---|---|---|
| **F1** fail-closed guard | Yes. `FrontRoomsMetalGlassRTRendererFeature.cs:138–143`: `EnsureTargets` / `SendTargets` / `BuildFrame` run inside `TracePass.RecordRenderGraph` with no try/catch. No commit after `75cfdff` touches the file | None | No trigger today (§3). `W/proj_rt` already has it, in the same lines (`FailClosed` at `FrontRoomsMetalGlassRTRendererFeature.cs:163` and `FrontRoomsGlassRTSystem.cs:101`, plus a `failclosed` test in `FrontRoomsGlassRTVerify`) | **glass-rt-track**: land it first in its merge. Its clone still lacks three points of the corrected fix (§2) |
| **F2** RT on by default, unverified | Partly. Editor Play: yes, quality level 3 → High (`FrontRoomsGlassRTSystem.cs:82–83`, `QualitySettings.asset` `m_CurrentQuality: 3`). Mac players: **no**, RT is off there (COV-1) | Docs only (§7) | Verification work, not a code defect. Red's highest-spec rule keeps RT on; the default level is Red's call (D1) | **glass-rt-track** verify stage: P0/P1 on the tree it merges, P1-B4 by eye (start with `P1_close055`), P5 at `38_deadlamp_window_1.5m` from the harness side, PROPS check hides the kit `Glass_Window` slab. Drop VL107 from the evidence |
| **F3** main-thread cost | Yes. The full scan is still at `FrontRoomsGlassRTSystem.cs:1137`. Today's runs, BuildFrame main-thread time per map frame (n 7,200 each): p50 2.32 / 2.32 ms, p99 5.57 / 6.08 ms, max 86.7 / 56.9 ms (budget p99 ≤ 0.5 ms quiet, ≤ 1.5 ms streaming; machine load 240–400, so only roughly comparable) | None | Performance only; no breakage. The glass-rt-track clone has improved it (quiet p99 1.73 ms, streaming p99 10.3 ms) but is still over budget | **glass-rt-track**: P0-A7 (struct-array P/Invoke by pointer first, cached candidates, real slicing), then P2 |
| **F4** release at Play exit | Yes. No `playModeStateChanged` hook in `FrontRoomsGlassRTSystem.cs` | None | Editor memory only (about 5 MB BLAS + 8 MB geometry held until the next Play). Not visible | **glass-rt-track**: `W/proj_rt` has it (`FrontRoomsGlassRTSystem.cs:1869–1918`, pump on `ExitingPlayMode`). Its verify should log meshes 0 / BLAS 0 / Retired 0 after exit |
| **F5** edit-mode opt-in | Yes. `OptIn` (`FrontRoomsGlassRTSystem.cs:193–199`) has no `Application.isPlaying` check. `FrontRoomsScreenAdsDemo.unity:1997` still holds the component (guid `62e280d7…`) | None | No player build includes that scene today: `EditorBuildSettings.asset` has it **disabled** (`enabled: 0`), and the iOS build lists only `FrontRooms3D.unity` (`FrontRoomsMobileBuild.cs:130`). `FrontRooms3D.unity` and `FrontRoomsLevelDesigner.unity` hold no reference | **glass-rt-track**: `W/proj_rt` has the guard (`FrontRoomsGlassRTSystem.cs:220–225`). **Screen-ads demo owner**: after the guard lands, remove the component in a Mac editor and re-save (do not re-run the scene builder) |
| **COV-1** Mac player has no plugin | Yes. The dylib is arm64 only (`file`); the importer pins StandaloneOSX CPU = ARM64 (`FrontRoomsGlassRTImporter.cs:36`). Your Mac player logs `[FrontRoomsGlassRT] off: plugin not loaded` (`~/Library/Logs/Red Wang/FRONTROOMSSS/Player.log:30` and `Player-prev.log:30`) | Docs only (§7) | It fails safe: RT is simply off in the player. Which fix to take is your call (option a: Apple-silicon-only players; option b: universal plugin, recommended). `W/proj_rt` does not address it yet | **glass-rt-track** after your choice. Option (b): `-arch arm64 -arch x86_64` in `NativePlugin/build_frontrooms_metal_glass_rt.sh` step 3, and `AnyCPU` for StandaloneOSX in `FrontRoomsGlassRTImporter.Configure` (never a meta-only edit). Then a player build in a clone: the dylib must be in `Contents/PlugIns` with both slices, and Player.log must say `on: Apple9, Ultra` |

## 2. What the glass-rt-track merge still needs for F1 (checked in `W/proj_rt`, 2026-10-08 19:4x)

`W/proj_rt` has hunks (a) and (b) of `runtime/rt/rt_failclosed.diff`: the try/catch and `FailClosed`. It also has the F5 guard (hunk c). The verifiers' corrected fix asks for three more points:

1. **Move the last record-time FrontRooms call inside the try.** `data.seq = HarnessEmptyPassOnly ? 0ul : frame.traced ? frame.seq : FrontRoomsGlassRTSystem.Warmup();` (`W/proj_rt` `FrontRoomsMetalGlassRTRendererFeature.cs:229`) still runs outside it. Compute the seq inside the try, then use it in the builder.
2. **Gate the public API once failed closed** (needed once GD3 calls it). Add `static bool failedClosed`, set it in `FailClosed`, OR it into `PermanentlyOff`, and make `Register`, `PrepareMeshes`, `IsPrepared`, `MeshChanged` and `RefitMesh` return early. No game code on HEAD calls them.
3. **Optional:** clear `qualityOverride` in `ResetStatics`, so a fail-closed Off does not carry into the next Play if domain reload is ever turned off. Also optional: `FailClosed` writes two console entries (exception + error line); §2.7 asks for one.

## 3. Evidence: today's main runs clean with G14 on

**Compile.** `W/proj_cxfix_runtime_rt`, `-buildTarget OSXUniversal`, `Library/ScriptAssemblies` deleted first so Assembly-CSharp is rebuilt (TOOLS.md, "scripts not attached" workaround): **0 `error CS`, 0 `warning CS`** (`W/logs/cxfix_rrt/c01_compile.log`, 40 s). No file changed, so there is no new warning by definition.

**Play Mode, 120 s of map at 60 Hz, seed 516574485, G14 High** (`FrontRoomsCxPlayRun`, batch with graphics, Metal, Apple M3 Max; machine load 240–400):

| Run | Frames | Restarts | Chunk roots torn down | Collection modified | ZBinningJob | Render Graph errors | NullRef / InvalidOp / MissingRef | Shader errors | Pink renderers | Frames (luma, black, magenta) |
|---|---|---|---|---|---|---|---|---|---|---|
| `rrt_high` | 7,307 | 0 | 75 | 0 | 0 | 0 | 0 / 0 / 0 | 0 | 0 | 7 JPGs: luma 62–89, 0 % black, 0 % magenta |
| `rrt_high_r` (forced R at 60 s) | 7,307 | 2 (1 forced R + 1 caught) | 115 | 0 | 0 | 0 | 0 / 0 / 0 | 0 | 1 = the known zone key after R (`10_review_runtime.md` R2, map contract; not RT) | 8 JPGs: luma 62–115, 0 % black, 0 % magenta |

- G14 status in both: `on: Apple9, High, layout OK, residency set`, `commandBufferErrors 0`, 0 `[FrontRoomsGlassRT] off` lines. 53 and 65 `Glass_Window` renderers, every one on prepass bit 30 with the `FRGlassRTPrepass` pass.
- The restart run tears the whole map down twice (static restart + `LoadScene`, the game's R path) while G14 is tracing. That covers `OnMapLost` / `DropChunk` with RT on, the path that threw on 2026-10-03.
- The only console errors are the clone-only ones every Play run in a clone shows (W/TOOLS.md §2): 1 FMOD `BankLoadException` (clones carry no `FMOD/` banks) with its FMOD error line, and 1 `ArgumentOutOfRangeException` from Unity's QuickSearch indexer at editor start. Warnings: FMOD "port 9264 in use" (parallel Unity jobs), one PanelSettings theme warning and the map's own "module prop would block an opening" notes.
- Same picture as the runtime review's runs on `22bb75f`, `ee5c9bb` and `a5262fb` (`10_review_runtime.md` §2).
- Outputs: `W/proj_cxfix_runtime_rt/Verification/cx_audit/play_rrt_high/` and `play_rrt_high_r/` (`summary.txt`, `console.txt`, `stats.tsv`, JPGs). Logs: `W/logs/cxfix_rrt/p01_play_rthigh.log`, `p02_play_rthigh_restart.log`.

**Red's own editor** (`~/Library/Logs/Unity/Editor-prev.log`, session 2026-10-07 20:38 → 2026-10-08 08:47, after `ee5c9bb`; it spans `22bb75f` and `bc0aa79`): 4 Plays reached the map (`[FrontRoomsGlassRT] map found` at lines 6673, 12127, 18256, 23674), G14 `on: Apple9, High` (lines 7002, 13100, 18530). 0 ZBinningJob, 0 "Render Graph Execution error", 0 `NullReferenceException` / `InvalidOperationException` / `IndexOutOfRange` / `ArgumentException`. The current `Editor.log` (opened 01:14) has no Play yet.

**Edit-mode suites + 20 map seeds** (`FrontRoomsCxSuites`, same clone, `W/logs/cxfix_rrt/s01_suites.log`). No file changed, so this only re-checks today's main against the runtime review's last run on `22bb75f`:

| Suite | `22bb75f` | `bc0aa79` + tree (today) |
|---|---|---|
| MapVerification | 100 / 100 | 100 / 100 |
| RelayNavTest | PASS, 60 / 60 hunts | PASS, 60 / 60 hunts, 0 pass-throughs |
| MapInteractionTests | 143 / 143 | **152 / 152** (+9 tests in the working tree) |
| LevelDesignerTests | 138 / 138 | 138 / 138 |
| FixtureTickTests | 28 / 28 | 28 / 28 |
| CameraRigTests | 25 / 25 | **33 / 33** (+8 tests in the working tree) |
| CaptionsTests | 9 / 9 | 9 / 9 |
| GlassShotTests | 20 / 20 | 20 / 20 |
| StreamVerification | 5 / 5 | 5 / 5 |
| GlassCompileCheck | OK | byte-identical output |
| Map signatures, 20 seeds × 25 chunks | — | built twice: 0 determinism failures; drop + rebuild: 0 failures; **0 / 500 chunks differ from `22bb75f`** (renderers, lights, colliders) |

- No suite threw. The 4 exceptions and 2 errors in the MapInteractionTests console are the tests' own injected faults ("test: a broken glass kit", "tools: forced build failure"), as in every earlier run.
- Copies: `runtime/fix_rt_bc0aa79/` (`suites_bc0aa79_vs_22bb75f.txt`, `signatures_bc0aa79_vs_22bb75f.txt`, both Play `summary.txt` files, `unity_log_counts.txt`).

## 4. Later commits (after `75cfdff`) in this group's files

- No commit after `75cfdff` (2026-10-03 19:41) touches `FrontRoomsMetalGlassRTRendererFeature.cs`, `GlassRT/*`, `Editor/RT/*`, `NativePlugin/*`, `Plugins/macOS/*` or `FrontRoomsPostStack.cs`. `git status` shows no local edit to any of them. So no later commit fixed or changed a Codex error here.
- The G14 shader hook (`9eddc35`, `bf2e883`, `70644f0`) is reviewed in `10_review_rt.md` §4 and `10_review_glass-look.md`; nothing new for this group.

## 5. Files changed vs main

**None**, including `.meta`. The sha1 of every in-scope file was recorded before any work: `W/proj_cxfix_runtime_rt.bases.txt` (28 files: the G14 scripts, editor tools, plugin, native sources, PostStack, the ads-demo scene, `QualitySettings.asset`, `EditorBuildSettings.asset`, the URP renderer asset). The clone held them byte-identical.

## 6. Apply package (convention v2, writes nothing)

- Script: `W/apply/codex_audit_runtime_rt.sh` (dry run by default; `--apply` also writes nothing and prints the sha1 manifest).
- Package files: `codex_audit_runtime_rt_bases.txt` and `_expected.txt` (0 entries by design), `_keep.txt` (the 28 watched files; note only), `_payload/` and `_base/` (empty), `codex_audit_runtime_rt.diff` (empty).
- What it does: it checks that the package is still empty, then reports which watched files moved, and probes the target for each finding (F1 / F4 / F5 landed or open, the ads-demo scene reference count, the dylib slices and the importer CPU). So the visual chat can run it after the glass-rt-track merge to see what landed.
- Dry run on main (`bc0aa79` + tree), 2026-10-08 19:49: all 28 watched files unchanged; F1, F4, F5, COV-1 open; 1 ads-demo reference; `NOTHING TO APPLY: 0 paths`, exit 0. Probe test against `W/proj_rt`: F1, F4, F5 = LANDED; 9 watched files moved.
- Do not run `rebase_apply.sh` on it (no bases by design; it would report GAP).

Command: `bash /Users/redwang/FrontRoomsVisualWork/apply/codex_audit_runtime_rt.sh`

## 7. Docs changed (this folder only)

- `10_review_rt.md`, five marked corrections (`[Correction, fix phase 2026-10-08 …]`):
  - §2.1: the macOS player holds the RT scripts, but not the dylib (COV-1).
  - F2: "the macOS player default is level 5 → Ultra" is true only on paper, because no Mac player loads G14 today. "Every Mac build shows unreviewed RT output" becomes "every Editor Play session".
  - F2: VL107 is not an RT symptom (2.9 / 2.7 before the prepass merge, 2.9 / 2.8 with it). It stays with window-landing.
  - §5: the "Player build" row and the "Look" row, the same two corrections.
- This report.
- `runtime/fix_rt_bc0aa79/` (5 small text files of evidence, §3).
- `images/fix_runtime_rt_baseline_play_rthigh.jpg` (§8).

## 8. Images and Figma

- No visible fix, so there are no before/after frames.
- One evidence sheet of the two baseline runs: `images/fix_runtime_rt_baseline_play_rthigh.jpg` (1920 × 684; 7 frames of `rrt_high` = `A_*`, 8 frames of `rrt_high_r` = `B_r_*`; every frame lit, none black or magenta). The restart run's `closeup_bad_1.jpg` (the pink key, R2) is left out because it is not part of this group.
- No VL slide was added. It holds no fix, and a slide needs a VL row in `Documentation/VERIFICATION_LOG.md`, which is outside this group's write scope. If the final sweep wants a row: check "runtime+rt fix group: G14 High on `bc0aa79`, 120 s Play ×2, console", verdict PASS (0 RT exceptions), the image above.
