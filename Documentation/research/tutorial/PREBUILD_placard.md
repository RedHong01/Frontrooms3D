# Placard sheet A packs: pre-built packages (游戏视觉, 2026-10-22)

Written by the placard stage of the 游戏视觉 workflow `prebuild-integrations`. That is run wf_d1d3c10c-3b4 (session local_a9a1dce2, account B, 16:47–17:0x). It continues the partial work of the runs that died before it: wf_12c986d3-ddf was stopped, and wf_f742b0e7-d77 died at 16:17. Why the packs are being pre-built: Red, 15:02, said "开始把所有能够integrate到主build里的所有已经完成的资产都开始integrate，不用等alpha了".

**Update, 21:00 (§9):** `placard_phase0` was rebuilt on 平面视觉's phosphor-underprint art (be0a97b2 / 2654943a) and the new examine.json (ba00f0ab), then re-measured in game. At (277, 6) it now clears the dark bar: A4 1.466 and dead area 1.310. At (85, 2) it is still under the bar: A4 0.962 and dead area 1.266, about level with v3 at that spot (1.001 / 1.230). Under 平面视觉's landing rule that makes it **READY as a documented interim exception**. Its HOLD file was moved to `W/_partial/`. The table below is the 17:05 state.

**Status, 17:05:**

| Package | Dry run on main | Verdict | What holds it |
|---|---|---|---|
| `W/apply/placard_phase0.sh` | DRY RUN OK | **HOLD: one gate failed** | The landing condition is met: C22 landed 15:39 and G1 15:52. The dark-ratio gate fails (§4): A4 0.920 and dead area 0.724, against a bar of 1.30. Everything else passes. |
| `W/apply/placard_sheet_a.sh` | DRY RUN OK | **HOLD: placeholder art + condition not met** | Four things hold it: the Floor 1 redraw by 平面视觉, the station, the stair lock, and the same dark gate. |

Both scripts refuse `--apply` while their `<track>_HOLD.txt` exists. Dry runs still work.

## 1. What was built, and how

**Art: 平面视觉's hand-off, checked on disk at 16:48 and unchanged since 14:26.** Every md5 below equals the one in `handoff/visual.md` §4 item 3:

| Folder | print | glow | examine.json | Use |
|---|---|---|---|---|
| `sheetA_phase0/` | 9d52bba0 | 0f42609b | fe327e70 (15 lines) | `placard_phase0` (final interim Floor 1 sheet) |
| `sheetA/` | d55a4460 | 99ea4ac6 | cc7edfc5 (16 lines) | `placard_sheet_a` (**placeholder**: see the redraw note) |
| `sheetA_B1/` | cceb8267 | 99ea4ac6 | de2fdeff | packed only (§5) |
| `sheetA_phase0_B1/` | 15092ba4 | 0f42609b | 9acf42c4 | packed only (§5) |

New since 16:47, and not used here: `sheetA_room/` and `sheetA_room_phase0/`. These are 平面视觉's Floor 1 first-room redraw (their LANDINGS CLAIM 16:47). That redraw replaces `sheetA/` and, once A26's room mount lands, Phase 0. **Verify, 17:3x:** 平面视觉 handed the redraw off (LANDED 16:49; rev 2 LANDED 17:01; `50_tutorial_placard.md` §17). On disk: `sheetA_room/` 0c7627f4 / e7e33168 / 36fdbadd, `sheetA_room_phase0/` 635a4e6e / cc60c148 / 02129a8d. Per §16–§17, `sheetA/` is retired for Floor 1, so `placard_sheet_a` is superseded (§8).

**Packing: done the Q16 way.** The tool is `Tools/lookdev/pack_evac_plan.py`. It takes the 2592 × 1674 art, pads it to 2592 × 1676 and packs it to 4096 × 2048 as `Prop_EvacPlan_A.png` (print) and `Prop_EvacPlan_E.png` (glow mask).
- Re-packing main's v3 art gives main's two textures **byte-identical**, so the pipeline is exactly the one behind today's placard.
- Phase 0 round-trip error: print 1.21, mask 0.08 (8-bit levels); the mask covers 0.96 % of the sheet.

**The payload: 7 paths, all of them visual-owned.** Every `.meta` is kept, so GUIDs and import settings are unchanged:

| Path | Change |
|---|---|
| `Assets/Resources/Surfaces/Textures/Prop_EvacPlan_A.png` | the packed print |
| `Assets/Resources/Surfaces/Textures/Prop_EvacPlan_E.png` | the packed glow mask |
| `Assets/Resources/Props/Models/Kit_EvacPlacard.json` | `inspect.regions` = examine.json's 4 regions (plan, legend, alarm, title block), smallest cap 2.8 mm. `texelsPerMetre` 7330 and transcript id `placard_q16` are kept. |
| `Tools/Blender/frontrooms_kit/assets/evac_placard.py` | the same regions in the kit writer, so a re-export keeps them |
| `Assets/Scripts/Rendering/Inspect/FrontRoomsInspectCatalog.cs` | the `placard_q16` transcript, generated from the NEW examine.json (never typed): 15 lines for Phase 0, 16 for sheet A, one glow line (`EXIT …`), last line `LEVEL 0 · SHEET A-2 OF 4 · PRINTED 11/89` |
| `Assets/Editor/Rendering/Inspect/FrontRoomsInspectTests.cs` | the sheet A expectations |
| `Assets/Editor/Rendering/Inspect/FrontRoomsInspectPlaytest.cs` | the sheet A expectations |

- Glow k stays **0.158**: `FrontRoomsPhosphor.PeakEmission` is not touched. It is listed in `_keep.txt`, so if it moves, the dry run prints a NOTE.
- There is no platform branch. The textures keep main's `.meta` (WebGL tab capped at 1024, BC7 / DXT1, ASTC 4×4 / 6×6 on iOS), so every platform gets the same sizes as today.
- No map-owned file is touched. **No contract diff is needed for Phase 0**: G1 already makes `FrontRoomsPlacard`'s west-first rule pick P1 west.

**Stacking.** `placard_sheet_a` accepts either main as it is or main + `placard_phase0`. The stacked base is in `placard_sheet_a_bases_after_phase0.txt`. On top of Phase 0 it writes only the two textures and the catalog.

**Builder** (re-runnable): `W/placard_a/build/`.
- `build_payloads.py` makes the payloads from the base copies plus the art. It does exact replacements and stops if one fails.
- `build_packages.py` writes the v2 package, the base copies and the diff. It stops if main moved from `base/BASES_1530.txt`.
- `chain3.sh` renders, `post.py` frames, and `apply_tests.sh` runs the scratch tests.

**Clone.** `W/proj_placard_a` was made at 15:24 and **re-synced 16:50** from main aca6240b + tree by `W/placard_a/build/resync.sh`, which repeats mkclone's rsync, local patches and install_tools. So C22, G1, G1b and Game fc76109d are in it. Its bundle id is `com.redwang.frontroomsss.test.placarda`.

## 2. Conditions: what flips each HOLD

**placard_phase0.**
- The condition is **met**: C22 is in main (15:39) and G1 is in main (15:52; 关卡设计: "游戏视觉: merge the Phase 0 placard swap now").
- The HOLD is there only for the dark-ratio gate (§4). Any one of these releases it:
  - **(a)** 平面视觉 accepts the dark result after seeing VL386. Then move the HOLD file to `W/_partial/`, dry run, `--apply`, and run Roslyn `--all` for macOS + iOS.
  - **(b)** 平面视觉 regenerates `sheetA_phase0/`. Then rebuild with `W/placard_a/build` and re-render.
  - **(c)** 游戏视觉 changes the placard glow model in a separate package that lands first.
  - **(d)** Red changes k.
- It retires when A26's first-room mount lands. Floor 1 then takes `sheetA_room_phase0/`.

**placard_sheet_a.** All of these must change:
1. 平面视觉's Floor 1 redraw is final (`NOTE_1022_placard_floor1_redraw_READ_FIRST.md`). Rebuild from it; never land d55a4460. **Verify, 17:3x:** it is final (rev 2, 17:01). It hangs in the first room on the M2 mount, not at today's start-door mount. So this package is superseded and will not be released. Its successor packs `sheetA_room/` together with the M2 mount once A26 is in main (§8).
2. The station is live: Floor 1's first room with the security desk and the floor key (关卡设计 M4 `FloorHasStation` + V1).
3. The stair lock is live: something calls `FrontRoomsMapWorld.SetExitLocked(true)` until the key is taken. Today the seam exists, but `ExitLocked` is false on every floor.
4. The dark gate passes or is accepted.

## 3. Checks (re-run 16:51–17:01 on the re-synced clone; seed 516574485)

Every timing is **UNDER LOAD** (1-minute load 84–231).

| # | Check | Phase 0 | Sheet A | Verdict |
|---|---|---|---|---|
| 1 | Art md5s on disk = handoff §4.3 (12 files) | 12/12 | (same set) | PASS |
| 2 | Pack reproduces main's v3 textures byte-for-byte | identical (A + E) | — | PASS |
| 3 | Placard mounts, P1 west after G1 (pre-G1 run at 15:4x: P2) | P1 west, door cell (277, 6), glow cell (276, 6) | same | PASS |
| 4 | Texture import (main's .meta) | A 4096×2048 BC7, 13 mips, 11,184,848 B; E DXT1, 5,592,424 B; WebGL 1024; iOS ASTC 4×4 / 6×6 | same | PASS |
| 5 | Glow k | 0.158 | 0.158 | PASS |
| 6 | EXAMINE EditMode suite (`FrontRoomsInspectTests`) | 106/106 | 106/106 | PASS |
| 7 | EXAMINE play-mode suite (`FrontRoomsInspectPlaytest`): no clipping in 50 approaches, freeze, zoom from the 2.8 mm cap = 12.0 px | 116/116 | (15:4x run, pre-G1: 116/116) | PASS |
| 8 | Transcript shown in game = examine.json, glow line only in the dark (own light 0.96 lit / 0.000 dark) | 15 lines, equal | 16 lines, equal | PASS |
| 9 | TP06: 5.0 mm alarm lines in EXAMINE base ≥ 12 px | 12.56 px (4K 25.15) | 12.54 px | PASS |
| 10 | TP06: white on red, EXAMINE lit | 2.68:1 (4K 2.85) | 2.70:1 | NOTE, not a gate |
| 11 | Dark ratio A4 (own lamp off 15 s, 2 lit neighbours, G 0.390), bar 1.30 | **0.920** | **0.927** | **FAIL** |
| 12 | Dark ratio, dead area (own + 4 neighbours off 15 s, G 0.225), bar 1.30 | **0.724** | **0.724** | **FAIL** |
| 13 | EXIT glow vs the paper above it (A4 / dead) | 1.02 / 0.83 | 1.02 / 0.82 | (info) |
| 14 | Roslyn `--all`, main + payload .cs | macOS 0 errors, iOS 0 errors | macOS 0, iOS 0 | PASS |
| 15 | Dry run on main (aca6240b + tree) | DRY RUN OK | DRY RUN OK | PASS |
| 16 | `rebase_apply.sh --no-install` | PASS (nothing to rebase) | PASS | PASS |
| 17 | Scratch apply tests (`W/placard_a/logs/apply_tests2.log`) | 12/12 as expected | (in the same log) | PASS |

Notes on the checks:
- **Check 7 for sheet A:** it was not re-run on the re-synced clone, because its EXAMINE path is identical to Phase 0's (same kit, regions and suite; only the texture and catalog differ). Its 15:4x run predates G1.
- **Checks 9–10, TP06:** 平面视觉's `ingame_check.py`, as shipped, finds no red block in the P1 frames. Its gate is r > 110, and at P1 the block's red reads r 87–105, because the facade takes its lamp at a grazing angle. So the numbers come from `W/placard_a/build/tp06_measure.py`: the same code with r > 60 and the letter cores taken relative to the block.
  - For comparison, main's v3 footer in the same frame reads 2.75:1, so the drop is the mount's light, not the art.
  - At P2 (the 15:4x run) the shipped tool gave 12.57 px and 3.85:1.
- **Check 17, the 12 scratch tests:** HOLD refuses; `--apply` gives MANIFEST OK, then ALREADY APPLIED; sheet A stacks on Phase 0 (3 paths written); Phase 0 refuses to regress over sheet A; refusals for a changed .meta, a half apply, a tampered payload and a moved text base.
- **Baseline, main's v3 legend** (same seed, spot and harness): A4 **1.430** (PASS; Q16 measured 1.437 there on 10-08, so the harness agrees) and dead area 1.242. The suites fail by design on the edited tests (105/106: the v3 catalog has no transcript).

## 4. The dark-ratio finding

- **Q16 bar:** in the dark, the glowing marks must read at least 1.30× the paper.
  - It is measured with the sheet at 0.6 m, FOV 40, linear HDR, post off.
  - Glyph = the pixels the full glow brightens. Paper = clear paper inside the glyph box.
- **What happens with sheet A:**
  - The glow mask (YOU ARE HERE, the route, the stair door and its EXIT) sits on **dark ink**: red and black.
  - Unlit, those marks read at 0.36 of the paper, which is high contrast (dark on pale).
  - After 15 s with the own lamp off, the glow has charged to G 0.39. That lifts the marks almost exactly to the paper's luminance (0.92), so the route nearly vanishes in luminance.
  - It shows as pale orange or green only by hue, and hue fades in low light.
  - At full charge (G 1) the marks read 1.80 (A4) and 2.22 (dead area).
- **What main does today:** v3's legend glow sits on pale phosphor ink (#D6DEBD), so any glow brightens it: 1.430.
- **Phase 0 as packed therefore lowers dark legibility at the start door,** from 1.43 to 0.92.
- **Options** (the same list as the HOLD file):
  - (a) Accept it: the marks still read by hue on a monitor, and the bar was written for the legend.
  - (b) 平面视觉 prints the glowing marks in pale phosphor ink, or adds a pale under-print.
  - (c) 游戏视觉 changes the glow model (visual-owned), so a mark that starts to glow stops being darkened by its ink.
  - (d) Red raises k. A4 1.30 would need k ≈ 0.265, and the dead area ≈ 0.37. Red set 0.158 on 10-21.
- **Recommendation:** (b) or (c), not (d).
  - Without a decision the v3 placard stays, which is today's state.
  - A symmetric bar (ratio ≥ 1.30 or ≤ 0.77) would pass the dead area (0.724) but still fail A4 (0.92).

## 5. LEVEL B1 copies: packed, not wired (open)

- `W/placard_a/packed/sheetA_B1/` and `sheetA_phase0_B1/` hold the packed textures:
  - sheetA_B1: A fa612861, E f85cf62e;
  - sheetA_phase0_B1: A 231e86a2, E 661bf6fe.
- **Nothing selects a B1 sheet today.** `FrontRoomsPlacardMount` listens only to `MapRunStarted` and mounts the one placard at the title's start door.
  - `FrontRoomsPlacardMount` is visual-owned (`Assets/Scripts/Rendering/`), so the switch is ours. No map contract is needed.
  - 关卡设计's G1b (16:05) supplies the inputs.
- **The B1 package needs all of these:**
  - **Trigger:** subscribe to `FrontRooms3DGame.FloorChanged(map, relay, floor)`. For floor ≥ 2, read `game.StartStairwell` (`NorthDoorCell`, `NorthDoorPoint`), and mount only if `FrontRoomsExitPlanner.StairDoorPlacardWest(map.Cache, door)` is true.
  - **Mount:** spawn a second placard instance beside the north door, on the map side, west first.
  - **Material:** give that instance its own material and textures (`Prop_EvacPlan_B1_A/E`). Today's material is shared.
  - **Transcript:** give it its own EXAMINE transcript id (for example `placard_b1` from `de2fdeff` / `9acf42c4`). This needs a second kit sidecar or a per-instance override, because the sidecar names one transcript.
  - **Keep clear:** `KeepClearAtStart` only applies at the run start, so a floor-start keep-clear needs a map hook. That part may need a contract.
  - **Label:** the printed `LEVEL B1` is wrong on B2 and deeper. Either the copy is shown only on floor 2, or 平面视觉 makes one sheet per level, or a number decal.
  - **Glow:** the same dark-ratio finding applies.
- This is a separate build stage with floor-transition verification (`-autopilotExit`). It is listed for the 游戏视觉 queue.

## 6. Images (VL386–VL387; specs in `W/vl_pending/prebuild_placard.md`)

- `images/VL386_placard_phase0_ingame.jpg` (FLAG). Phase 0 at P1 west. Tiles: reference, lit at 3 m and 1.5 m, EXAMINE lit, dark A4 at 1.5 m, EXAMINE dark A4, EXAMINE dead area, and main's v3 dark A4.
- `images/VL387_placard_sheetA_ingame.jpg` (FLAG). The same set for sheet A (placeholder art).
- **Raw frames** (1920×1080, plus a 4K EXAMINE) for 平面视觉's TP01 / TP06 check: `W/placard_a/renders2/{phase0,sheetA,v3main}/`. Each folder has:
  - `00_*_lit_3m`, `01_*_lit_1p5m`;
  - `02_*_exam_lit`, `03_*_exam_lit_4k`;
  - `04_*_dark_a4_1p5m`, `05_*_dark_a4_3m`;
  - `06_*_exam_dark_a4`, `07_*_exam_dark_a4_4k`;
  - `08_*_dark_dead_1p5m`, `09_*_exam_dark_dead`;
  - `report.txt`, `log.txt`, the transcripts and the suite JSON.
- **Numbers:** `W/placard_a/framed/*.json`.
- The pre-G1 frame from 16:06 (mount P2) is superseded. It is kept as `W/placard_a/framed/VL386_placard_phase0_ingame_preG1_1606.jpg`.

## 7. Files

- **Packages in `W/apply/`:**
  - each has `.sh`, `_bases.txt`, `_expected.txt`, `_keep.txt`, `_payload/`, `_base/`, `.diff` and `_HOLD.txt`;
  - `placard_phase0.sh` c7fe692f, `placard_sheet_a.sh` 63aa6a1f.
- **Payload sha1s:**
  - Phase 0: A c98c516d, E 661bf6fe, Kit json 20535f90, evac_placard.py 74bc6144, Catalog 117fdbb9, Tests 0e497a1b, Playtest aa9346ab.
  - Sheet A: A 26d62be0, E f85cf62e, Catalog 7712222a; the other four are the same as Phase 0.
- **Bases** (main aca6240b + tree, unchanged since 15:29): A 37a86ae7, E 73009611, Kit json bc6d8b35, evac_placard.py 8d96c24c, Catalog 271d628a, Tests 474c2752, Playtest 5aa194bf.

## 8. Verify stage (wf_d1d3c10c-3b4, 17:08–17:3x; every timing UNDER LOAD, 1-minute load 79–408)

**Verdict: NOT READY.** Both packages are correct as built and both stay on HOLD. Nothing was applied to main.

**Re-checked, and they hold:**
- All 12 art md5s on disk match.
- The packer re-run reproduces all 8 packed textures byte-for-byte, and main's v3 pair too.
- Both dry runs are OK on main as it stands at 17:08. Main has moved since the build (sound 17:06, MapWorld 17:11, Game b97aae15 17:19), but none of the 7 paths changed.
- `--apply` refuses on both while their HOLD files exist, and main is unchanged after that check.
- Both catalog transcripts match their examine.json line for line (15/15 and 16/16). The regions equal examine.json's.
- Roslyn `--all` on today's main plus each payload: macOS and iOS, 0 errors.
- No open CLAIM touches any of the 7 paths.
- Text files are LF only.

**Re-run in the clone** (Phase 0 payload, `FrontRoomsPlacardARender`, 17:25–17:29, frames in `W/placard_a/renders_verify/phase0/`):
- Same seed 516574485, but the start door came out at a **different spot**: P1 west, door cell (85, 2), face z 6.03, against the build's (277, 6), z 18.03.
  - `MapRootFor` follows the title stream's door (`doorZ`), and that depends on timing, not only on the seed.
  - So the build's like-for-like comparison holds only within its own chain. Its 3 runs did share (277, 6).
- What still holds at the new spot: G1 (P1 west), EditMode 106/106, the transcript 15/15, k 0.158, and the import settings.
- **The dark check fails again, and harder:** A4 0.493 and dead area 0.695.
  - At full charge, A4 reads only 0.824 there, because the 2 lit neighbours keep the paper at 0.47 of lit.
  - So option (d) is spot-dependent. A4 needs k ≈ 0.265 at (277, 6) but about 0.77 at (85, 2).
  - A symmetric bar (≥ 1.30 or ≤ 0.77) passes (85, 2) but still fails (277, 6) A4 at 0.920. The marks fall into a null zone at partial charge.
- **Art-level check:** the glowing marks' linear albedo is 0.06 of the paper on Phase 0 and sheet A, against 0.66 on v3, where the glow sits on pale ink. That confirms the cause given in §4.

**Corrections made by this stage:**
- `placard_sheet_a_HOLD.txt` now says that 平面视觉's redraw was handed off (16:49, rev 2 17:01), that `sheetA/` is retired for Floor 1, and that this package is superseded by a `sheetA_room` package with the M2 mount.
- `placard_phase0_HOLD.txt` option (d) now carries the spot dependence.
- §1 and §2 above say the same.

**Still open (not small):**
- The LEVEL B1 switch is not built (§5). `NOTE_1022_placard_floor1_redraw_READ_FIRST.md` (15:4x) asked for it once StartStairwell + SeedFor landed (G1b, 16:05).
- In game, white on red reads 2.7:1 at P1, against the 5.9:1 spec. v3 shows the same.

## 9. Re-measure after the phosphor underprint: VL386 rev 2 (2026-10-22 20:23–21:00)

This stage is the 游戏视觉 Measure stage. It started on account B (wf_5bcebf35-fbd, 20:2x) and died at that account's session limit. It was re-run after account switch #5 in session local_5656cffd (account A). Every timing is **UNDER LOAD**: the 1-minute load was 51–389.

**Verdict: READY as a documented interim exception.** 平面视觉 owns the landing rule:
- ≥ 1.30 at both points: READY.
- Under 1.30 but better than the old 0.92: READY as an interim exception.
- Otherwise: HOLD.

At (277, 6) both numbers are at or above the bar. At (85, 2) both are under it, but above the old Phase 0 (and above 0.92) and level with today's v3 there. `placard_phase0_HOLD.txt` moved to `W/_partial/placard_phase0_HOLD_released_*` at 21:00.

**What changed in the package:**
- **Art (平面视觉 option (b), 50_tutorial_placard.md §18):** a 1.5 mm phosphor band (#D6DEBD, v3's legend ink) round every glowing mark, plus the stair block, printed under the red and black. The glow mask is where that pale ink stays visible.
- **Hashes:** print be0a97b2, glow 2654943a. `examine.json` is ba00f0ab since 20:25: the describer now reads `NO REENTRY (the door into this floor)`, per 叙事 40 §4.
- **Payload:** A b08cf901, E 8a0cb96e, catalog 2c483a68. The other 4 paths are unchanged (Kit json 20535f90, evac_placard.py 74bc6144, Tests 0e497a1b, Playtest aa9346ab). No test edit was needed: the suites check the line count, the glow line, the header and the title block, not this line.
- **Bases:** unchanged, all 16 touched and kept paths, re-checked at 21:00 (main HEAD 3f277a1e + tree).

**Checks.** The clone is `W/proj_placard_a`, synced 20:08 from main db790e30 + tree. The run used seed 516574485, k 0.158, 1920×1080 and the VL386 harness.

| Check | Result |
|---|---|
| Dark ratio at (277, 6), the VL386 spot: A4 (own lamp off 15 s, 2 lit neighbours, G 0.39) | **1.466**. Three runs: 1.465, 1.469, 1.466. Was 0.920; v3 1.430. **PASS** |
| Dark ratio at (277, 6): dead area (G 0.225) | **1.310**. Three runs: 1.312, 1.300, 1.310. Was 0.724; v3 1.242. **PASS, at the bar** (the 1.300 run rounds just under it) |
| Dark ratio at (85, 2), the 17:29 spot: A4 | **0.962**. Was 0.493; v3 there 1.001. **Under the bar** |
| Dark ratio at (85, 2): dead area | **1.266**. Was 0.695; v3 there 1.230. **Under the bar** |
| Lit read and TP06: the 5.0 mm alarm lines in the EXAMINE base | 12.56 px (bar 12; 4K 25.15). PASS |
| Transcript in game | 15 lines, with the new describer line. The glow line shows only in the dark. PASS |
| EXAMINE EditMode suite | 106/106. PASS |
| EXAMINE play-mode suite | 116/116. PASS |
| White on red at P1, EXAMINE lit (`tp06_measure.py`, r > 60) | **2.66:1** (2.64–2.68 over 3 runs; 4K 2.83; v3 2.75; the print preview 5.26). Still under 3:1, so 平面视觉 deepens the red (§18) |
| Dry run | Blocked on main by the permission classifier. DRY RUN OK on fresh copies of main's 16 paths at 20:47 (with HOLD) and 21:00 (HOLD released) |
| Scratch apply tests | 12/12 as expected |
| Roslyn `--all`, main + payload .cs | macOS 0 errors, iOS 0 errors |

**How (85, 2) was reached.** The start door's spot follows the title crawl, not only the seed:
- When room 0's door is still shut at the start request, the stream ends there: (85, 2), z 6.03. Otherwise it ends one room on: (277, 6), z 18.03.
- At 17:29 the load clamped the crawl's frame step (0.1 s), so the run started in room 0.
- The clone-only harness copy `W/placard_a/harness_spotpin/` adds an opt-in `-paTitleDt`. It pins the title's frame step until the start request and changes nothing after it. `-paTitleDt 0.3` reproduces room 0 and (85, 2) for both Phase 0 and v3. `0.0575` (35 frames, the same frame count as 17:29) stays at (277, 6).

**Why (85, 2) misses, and what would clear it:**
- **A4 is a lighting problem, not an ink problem.** The two lit neighbours keep the paper at 0.48 of lit there, against 0.26 at (277, 6). So the glow (+0.109 of the lit paper at full charge) is small next to the light already on the sheet.
  - Today's band reads 0.711 of the paper unlit. Clearing 1.30 would need 1.05: a band brighter than the paper, which no ink can do.
  - Even a band as light as the paper (#ECF5D1) reaches only about 1.14.
  - Band width is not a lever. The ratio is a per-pixel mean, and 94 % of the glow pixels are already pure band colour.
  - Only more glow clears it: about 2.4× at today's band (k ≈ 0.37), or about 1.6× with a paper-light band (k ≈ 0.26). That is Red's call (option (d)).
  - v3 fails there too (1.001).
- **The dead area is 3 % short.** The band needs to be about 5 % lighter in luminance: #D6DEBD → about **#DBE3C1** (Y 0.702 → 0.739). That predicts 1.30 at (85, 2), and about 1.34 at (277, 6), which takes that spot off the bar's edge.
  - #DDE5C3 (7 % lighter) gives about 1.32 at (85, 2).
  - The same 7 % lifts A4 at (85, 2) only to about 1.01.
  - The other way to close it is about 7 % more glow (k ≈ 0.170).

**Images and files:**
- `images/VL386_placard_phase0_ingame.jpg` is **rev 2** (FLAG, 12 tiles: both spots, plus the old Phase 0 and v3 at each).
- Rev 1 is kept as `images/VL386_placard_phase0_ingame_0920.jpg`.
- Specs: `W/vl_pending/prebuild_placard.md` (VL386 is still pending placement).
- Numbers: `W/placard_a/framed/VL386_placard_phase0_ingame.jpg.json`.
- Raw frames: `W/placard_a/renders_measure7/` and `renders_measure6/`. Scripts: `W/placard_a/build/chain_measure7.sh` and `post7.py`.

**Apply** (游戏视觉 session; main only through the package):

```
bash /Users/redwang/FrontRoomsVisualWork/apply/placard_phase0.sh            # dry run
bash /Users/redwang/FrontRoomsVisualWork/apply/placard_phase0.sh --apply
```

Then run Roslyn `--all` for macOS + iOS. `placard_sheet_a` stays on HOLD (superseded, §8). Its stacked base file now names the 2c483a68 catalog.
