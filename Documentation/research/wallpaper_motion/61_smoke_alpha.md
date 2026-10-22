# 61 · Smoke in Unity: the live wallpaper print for the Alpha

Status: **PASS** (2026-10-22 09:46–10:05, 游戏视觉 smoke). The package is `W/apply/print_driver_alpha.sh` (v2), as described in `60_landing_alpha.md`. It was applied only to a private clone. Red's project was not touched: no Unity on it, no git writes. Verification slide: **VL351** (`3510:7311`).

| Check | Verdict | Deciding number |
|---|---|---|
| (a) Import + Q1b | PASS | Texture2DArray 2048 × 2048 × 8, RG_BC5_UNorm, 12 mips, 0 import errors; Q1b gates all PASS |
| (b) Play smoke | PASS | title w 1 / y 8 / x 0 on 108 of 108 frames; jump at 60.90 s; warn 1 → w 0 in 1.18 s; quiet slot 4.23 s, then back in 1.18 s; 0 new errors |
| (c) Pre-jump frame vs no package | PASS | 0 px differ (max 0) at run 1 2.5 s and 4.5 s; the print covers 20.0 % of the 4.5 s frame |

## Setup

- **Clone.** `W/proj_print_alpha` was made with `mkclone.sh` (main `d4d0e7a5` + working tree, synced 09:46).
- **Twin without the package.** `W/proj_print_alpha_ref` is an APFS copy of the clone, made before the apply.
- **Apply.** `APPLY_REAL=<clone> APPLY_BACKUP=<clone>/_apply_backup bash W/apply/print_driver_alpha.sh` gave DRY RUN OK, then `--apply` gave **MANIFEST OK** (log `W/logs/print_alpha_smoke/apply.log`).
  - The script writes the applied manifest next to the target (`W/`). It was moved into the clone (`W/proj_print_alpha/print_driver_alpha_applied_manifest.txt`).
  - `W/apply` was unchanged (listing compared before and after).
- **Probe.** A clone-only probe was added to both clones: `Assets/Editor/PrintSmoke/FrontRoomsPrintSmokeProbe.cs` (ASCII, no tuples, reflection only on the driver).
  - It logs `_FR_PrintClock` and `_FR_PrintWarp` every frame (`frames.tsv`) and every map second (`seconds.tsv`), plus every change (`events.txt`).
  - It renders lossless PNG captures through the game camera, using CxPlayRun's target (1600 × 900 sRGB).
- **Concurrency.** Every Unity job went through `W/logs/print_alpha_smoke/run_chain.sh`, which starts a job only while the visual line runs fewer than 2 Unity jobs.

## (a) Import and Q1b

- **Compile.** OSXUniversal: 0 `error CS` (`pkg_compile.log`).
- **Reimport.** `ReimportBatch` forced a reimport of Assets/Scripts (TOOLS.md "Known issue") and of the sheet (`Verification/print_smoke/import_pkg.txt`):
  - importer: Texture2DArray, flipbook 4 × 2, linear, mips, Standalone BC5 max 2048 q100, WebGL RG16 max 1024;
  - asset: **2048 × 2048 × 8, RG_BC5_UNorm, 12 mips**; `Resources.Load` returns the same;
  - **0 errors** during the reimport; no `[PrintArray] … print mips not applied`;
  - `FrontRoomsPrintDriver` resolves with GUID `8c24b48e…`, and `FrontRooms3DGame` resolves too.
- **Q1b batch** (`FrontRoomsPrintP0Test.RunQ1bBatch`, `pkg_q1b.log`):
  - Build Print Array printed "kept" for both `_PrintTex` and the sheet;
  - every hash stayed the same after the batch: sheet `f2ca17d5`, `.meta` `ce025607`, driver `f1df9051`, its `.meta` `aa00cc74`;
  - `[PrintQ1b] texels PASS, reduced PASS, BC5 gate PASS, T2 PASS, crossfade PASS, seams PASS`;
  - output in `W/proj_print_alpha/Verification/print_q1b/`.

## (b) Play smoke

The run used `FrontRoomsCxPlayRun.RunBatch` with these arguments: `-cxSeed 516574485 -cxSeconds 185 -cxRelayDelay 66 -prProbe -prForceReturnAt 150`.
- Game time is fixed at 60 Hz.
- Logs: `pkg_play.log`, `W/proj_print_alpha/Verification/cx_audit/play_pkg/`, `Verification/print_smoke/pkg/`.

| Map s | Event |
|---|---|
| 0–1.8 | Title: 108 frames, every one w 1, y 8, x 0 |
| 60.90 | **Jump 1**, 0 → 1, blend 1.18 s. Seed 1990 predicts a 60.87 s hold |
| 87.72 | **WarnStageChanged 1.** The hold `warn` is set in the next frame |
| 88.92 | **w = 0**, 1.18 s after the hold; frame cut 1 → 0 unseen (w 0) |
| 90.86 | Chase (holds `chase,warn`) |
| 94.41 | Caught → restart. Run 2 starts on K00; `Clear()`, so w returns in 1.18 s with no quiet slot |
| 150.02 | Probe hold set (see below); w 0 after 1.18 s |
| 152.52 | Probe hold released; `resumeIn` 4.22 s |
| 156.76 | **Fade in starts 4.23 s after the clear** (quiet slot); w 1 at 157.94 (1.18 s) |
| 163.97 | Jump 2 (run 2 at 68.05 s) |

- **Errors.** The console held only the expected entries, the same set as the run without the package:
  - the FMOD `BankLoadException` and its bank-open error (clones carry no banks);
  - QuickSearch's `ArgumentOutOfRangeException`;
  - map prop warnings.
- **The quiet slot was timed on the driver's own external hold.** In this run the Relay caught the player before its warning ever cleared, so no natural "warn cleared" happened. At map 150 s the probe called `SetHold("probe", true)`, the API that live cue cells use, and cleared it 2.5 s later. That runs the same `HoldGate` path a cleared warning would. A natural warn clear in Play is still unobserved; the wallpaper line's `GateTest` covers it engine-free.
- **The hold draws are deterministic.** Run 2's first jump at 68.05 s is the 4th seed-1990 draw (58.93 s) plus the 1.18 s fade in plus 7.93 s held by the probe.
  - `CutTo(0)` at the warn hold drew the 3rd value (`jumps.Reset()`), and `MapRunEnded` drew the 4th.
  - So after a hold the wall waits a fresh 45–90 s, as designed.

## (c) A frame before the first jump vs no package

The same capture was taken in both clones: `-cxSeconds 6 -prCapAt 2.5,4.5`, runs `pkg_c2` and `ref_c2`. The autopilot's path is identical in every clone until about map 5 s; after that it splits (the 20 s and 40 s captures of the long runs face different rooms).

| Frame | With package (w 1, slice 0 from the array) vs without (no driver, static `_PrintTex`) |
|---|---|
| run 1 at 2.5 s | **0 px differ, max 0** |
| run 1 at 4.5 s | **0 px differ, max 0**; slice 4 forced in the same frame changes 287,493 px (20.0 %), so the print is on screen |
| title at 1.0 s | 0 px for run-matched pairs (`pkg` vs `ref_c2`, `pkg_c2` vs `ref`) |

In the title frame, 4 floor pixels (1 level) flicker between two runs of either clone (`pkg` vs `pkg_c2` too). That is run-to-run noise, not the print.

**A second render in the same frame is not a pixel test.** URP's dither advances per render, so two renders of one frame differ on about 70 % of pixels by up to 3 levels. Only same-sequence captures across clones are compared here.

## Timings (UNDER LOAD, load average 55–228)

| Job | Time |
|---|---|
| Compile + reimport | 23 s per clone |
| Play, 185 map s | 264 s |
| Q1b | 218 s |
| Play without the package, 45 map s | 113 s |
| Early-capture runs | 38–39 s each |

## Not covered

- Reduce Motion (CAMERA MOTION 0) and the pause card.
- A WebGL build.
- iOS.

## Files

- **Images** (Shot format, `W/logs/print_alpha_smoke/make_images.py`):
  - `images/vl351_before_jump_identical.png`
  - `images/vl351_first_jump.jpg`
  - `images/vl351_hold_timeline.png`
- **Working files in `W/logs/print_alpha_smoke/`:**
  - raw PNGs in `raw/`, numbers in `numbers.json`;
  - chain status in `chain_*.status`;
  - Roslyn checks in `roslyn_*`.
