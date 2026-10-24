# NOTE 2026-10-22 · EXAMINE transcript region heads + InspectPlaytest vs C7 (inspect_regions)

游戏视觉, workflow inspect-regions (wf_1ff97cae-6c5, build stage, session local_5656cffd, account A). Written 21:5x.
The first build agent was cut off at 21:32; a resumed agent finished the work from its files.
Package: `~/FrontRoomsVisualWork/apply/inspect_regions.sh` (v2). Clone: `~/FrontRoomsVisualWork/proj_insregions` (main 3f277a1e, 21:11).
Not landed yet: 游戏视觉 lands it with a dry run, then `--apply`, then Roslyn.

## 1. The format (decided by 游戏视觉, agreed with 交互设计)

`FrontRoomsInspectCatalog.Transcript(id)` still returns `string[]`, in reading order.

- `\u0001` + text is a glow line. It is unchanged, and it shows only while the ink is legible.
- `\u0002` + name is a region head. The names are `PLAN`, `LEGEND`, `IN CASE OF ALARM` and `TITLE BLOCK`, the words 叙事 signed in `research/narrative/40_floors_exit_tutorial_strings.md` §4. There is one head per examine.json region, in examine.json's order.
- The lines after a head belong to that region, up to the next head. A head never glows, and a marker only ever starts a line.
- Lines before the first head belong to no region. Here that is the sheet's title band, `EVACUATION PLAN · LEVEL 0`, which sits above every region rect (the regions start at y 46 mm).
- Every other line is examine.json's line, byte for byte (sheetA_phase0, md5 ba00f0ab).

The transcript as it now reads (`<H>` = `\u0002`, `<G>` = `\u0001`), 19 entries, 4 of them heads:

```
EVACUATION PLAN · LEVEL 0
<H>PLAN
NO REENTRY (the door into this floor)
YOU ARE HERE
<G>EXIT (over the stair door)
STAIR A
<H>LEGEND
LEGEND: YOU ARE HERE · STAIR (EXIT), ONE FLOOR DOWN
TYPICAL FLOOR · NOT TO SCALE
THIS FLOOR MAY DIFFER FROM PLAN
<H>IN CASE OF ALARM
IN CASE OF ALARM
ON ALARM, A FLOOR WARDEN CHECKS EVERY ROOM.
1 WALK, DO NOT RUN.
2 CLOSE DOORS BEHIND YOU.
3 IF THE LIGHTS DIM, LEAVE THE AREA.
4 EXIT BY STAIR, ONE FLOOR DOWN.
<H>TITLE BLOCK
LEVEL 0 · SHEET A-2 OF 4 · PRINTED 11/89
```

The regions hold 4, 3, 6 and 1 lines. examine.json's `texts` (5/7/12/3) counts drawn text runs, not transcript lines, so the split cannot be read from it directly. Section 3 explains how the split is made.

## 2. Files (main base → package)

| Path | Base | New | Change |
|---|---|---|---|
| `Assets/Scripts/Rendering/Inspect/FrontRoomsInspectCatalog.cs` | 2c483a68 | 7b90acb9 | 4 heads; doc block = the format; written by the generator |
| `Assets/Scripts/Rendering/Inspect/FrontRoomsInspect.cs` | f06f8473 | 2a2af005 | `TranscriptText` now calls the new `TranscriptPlain(lines, glowLegible)`; see below |
| `Assets/Editor/Rendering/Inspect/FrontRoomsInspectTests.cs` | 0e497a1b | 5002dc91 | the line count skips heads; `RegionHeadTests` (5 new checks) |
| `Assets/Editor/Rendering/Inspect/FrontRoomsInspectPlaytest.cs` | aa9346ab | 3870d74e | ZoomInputWired is saved and restored (C7-proof) |
| `Tools/lookdev/inspect_transcript.py` | ABSENT | e4a88169 | new: the generator |

Every `.meta` is kept. The package adds no platform branch, asset or scene, and touches no map, Game or Input file.

**Consumers.** `FrontRoomsInspect.TranscriptText` is the only reader of `Transcript()` in main, and nothing draws it yet. 关卡设计 builds the desktop G9 panel; 交互设计 builds the phone T6/T7.

`TranscriptPlain` never emits a raw marker:
- a head shows as its name after a blank line;
- a head is dropped when every line under it is hidden;
- a glow line shows only while legible.

A panel that draws regions itself (G9 Plex Mono heads) should read the `string[]` directly.

**Playtest.**
- `Main()` saves `ZoomInputWired` (:648) and notes the value in the run's numbers.
- `PlacardScenario` sets it to false at its start (:751), which is the v1 half.
- The wired half keeps `= true` (:831).
- At the end, `RestoreZoomWired()` (:676, and also in `Finish()` on an early end) restores the saved value instead of forcing false.

## 3. The generator

`Tools/lookdev/inspect_transcript.py <examine.json> <FrontRoomsInspectCatalog.cs> [--check] [--print] [--label]` rewrites the catalog's `placard_q16` entry and the doc block (LF).

It splits the lines in one of two ways:
- If every region carries an integer `lines`, it uses those counts.
- Otherwise it uses the fixed structure of sheet A in `tutorial_placard.swift`: the title band is line 0; `LEGEND…` starts the legend; `IN CASE OF ALARM` starts the alarm block; the last ` · SHEET ` line is the title block.

It stops instead of guessing when an anchor is missing, a region id is unknown, the region order is not the reading order, a region is empty, or a marker sits inside a line. All of these stops were exercised.

It runs clean on all six sheet folders: sheetA, sheetA_B1, sheetA_room and sheetA_room_phase0 give 20 entries; sheetA_phase0 and sheetA_phase0_B1 give 19. `--check` on main's catalog plus sheetA_phase0 reproduces 7b90acb9.

`W/placard_a/build/build_payloads.py` (the sheet-A / room packer) now calls the generator: main's copy once this lands, the payload's until then. Future sheets therefore get their heads automatically. The old builder is kept as `build_payloads_pre_regions_2115.py.txt`.

## 4. Tests (clone; every run UNDER LOAD, load 137–271)

| Run | Clone state | Result |
|---|---|---|
| EditMode | as main (no C7) | **111 / 111 PASS** (106 + 5 new), 35 s |
| InspectPlaytest | as main (no C7) | **116 / 116 PASS**, 215 s |
| EditMode | C7 simulated (`examine_c7_game.diff` 7aae2752 → Game 3947e882, clone only) | **111 / 111 PASS**, 15 s |
| InspectPlaytest | C7 simulated | first run 115 / 116 (see 5.1); **rerun 116 / 116 PASS**, 205 s; note "zoom input as the game set it: wired (C7)"; `zoom not wired (v1)` ok |
| EditMode, negative | catalog tampered (PLAN/LEGEND swapped, STAIR A→B, glow marker in a head) | 107 / 111: 4 of the 5 new checks fail as they should (markers, byte for byte, head order, plain text). The position check passes, because the head positions do not change. Catalog restored to 7b90acb9 |

After the runs, the clone's Game.cs is back at main's 938b0ee9. The C7 simulation is not in the package.

**Package checks:**
- The dry run on main (1521cd81 + tree, 21:4x) prints DRY RUN OK, with all 5 bases unchanged.
- Scratch tests with APPLY_REAL under `W/tools/_test/inspect_regions_1022`, T1–T10, all as expected:
  - the apply gives MANIFEST OK, and a second run gives ALREADY APPLIED;
  - the script refuses a changed .meta, a half-applied target, a tampered payload, a moved base, a new path that already exists, a bad target, and a HOLD.
- Roslyn `--all` on main plus the payload: macOS and `--ios` both COMPILE OK (21:4x).
- Re-checked at 21:49, after 关卡设计's 21:48 wayfinding landing (Assembly-CSharp now 111 files): DRY RUN OK, and Roslyn macOS + `--ios` COMPILE OK.

## 5. Open

1. **A flaky lamp check, not caused by C7.** The first C7 playtest failed `lamp dead area: lift 2.12 EV (cap 2.3)`. Its adapt was 0.944, and 1 − e^(−3.49/1.2) = 0.945 is exactly the Reduce Flashing calm tau. The normal tau gives 1 − e^(−3.49/0.6) = 0.997, which is what every other run shows.
   - The editor PlayerPrefs (`unity.Red Wang.FRONTROOMSSS`) are shared by every clone of this product, so another editor's Reduce Flashing toggle can leak into a run.
   - During the rerun, a sampler read `ReduceFlashing = 0` 53 times, and the run passed.
   - C7 is not the cause. After `PlacardScenario`, ZoomInputWired is true in both runs.
   - This pre-existing playtest race is worth pinning later.
2. **`FrontRoomsInspectTests` :774 still asserts `!ZoomInputWired` (v1).**
   - It passes in batch, because the domain is fresh and Game.Awake never runs.
   - Once C7 lands, it would fail if the suite were run inside an editor that has just played. Domain reload happens on entering play mode, not on leaving it.
   - Retire or flip it when C7 lands.
3. **`IN CASE OF ALARM` reads twice.** The head is followed by the printed line with the same words. The signed rule is that heads repeat the sheet's own section words, and the lines stay byte-identical. Whether a panel folds a printed line that equals its head is the G9/T7 call of 平面视觉 / 交互设计.
4. **Process.**
   - The first agent's negative run was killed after it tampered the clone catalog (21:30–21:43). The clone was restored to the payload before anything else ran.
   - The resumed negative EditMode run (16 s, 21:45) overlapped another visual job that started between the slot check and the launch, so three visual Unity jobs ran for about 16 s.
5. C7 itself is 关卡设计's to land, right after this package or in the same round.
