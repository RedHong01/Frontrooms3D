# Evacuation-plan placard (EGRESS, A.12), from 平面视觉

A framed 1990 US evacuation plan for the start door: 432 × 279 mm (17 × 11 in, landscape). The strings are narrative's (`30_narrative_phosphor.md` §4 A.12); the typography and layout are 平面视觉's; the prop is the visual chat's.

| File | What |
|---|---|
| `placard_print_lit.png` | The printed sheet at 6 px/mm (2592 × 1674): black and safety red on off-white. The legend is in pale phosphor ink (#D6DEBD), faintly visible by day |
| `placard_glow_mask.png` | The glow mask at the same size: white = phosphor ink, black = none. Use it as the emission mask, gated by the placard cell's lamp like the walls |
| `placard_lit_preview.png`, `placard_dark_preview.png` | Review renders at 3 px/mm |
| `placard.swift` | The generator: `swiftc -O placard.swift -o placard_gen && ./placard_gen <outdir> [legend dir]` |
| `legend_swatches.py`, `legend/` | The v2 legend swatches and masks (see below) |
| `v2/` | The 2026-10-07 version (legend swatches in the WP03 reference palette) |
| `v1/` | The 2026-10-03 version |

**v3 (2026-10-08):** the legend swatches now print in the colours the game prints WP03 with, so the legend reads as the wall's own paper.
- The pipeline is unchanged; only the palette remap moved into `legend_swatches.py` (adopted from 游戏视觉's Q16 candidate, `Documentation/research/placard/42_request_graphic.md`).
- `--reference` brings back the v2 palette.
- The output is byte-identical to the candidate (md5 5f71409a…).
- The glow mask is unchanged.
- Dark legibility is 1.37× / 1.32× (P1 / P2), still above the 1.30 floor.

**System, shared with the wall ink.**
- **Face:** TeX Gyre Heros Bold (Helvetica Bold stand-in), caps.
- **Title block:** the same as the wall stamp, scaled down: LEVEL 0 | SHEET A-2 OF 4 | PRINTED 03/90, with 0.6 mm rules and 3.6 mm caps. The wall stamp is the next sheet, A-3.
- **Plan:** 1990 US architectural conventions.
  - Poché walls 0.156 m thick and a door that swings out with its arc.
  - 6 m column grid with A/B and 1/2 bubbles, a north arrow and `SCALE: 3/16" = 1'-0"`.
  - `YOU ARE HERE` in red with a dashed red route to `EXIT`.
  - A dash-dot match line marked `MATCH LINE — SEE SHEET A-3`; the area beyond it is left blank.
- **Legend, v2 (2026-10-07, Red: "按新墙面提示重画", 32_pattern_native_hints.md §5b).** It shows WP03's own elements, not overlay glyphs. The swatches of the paper are at 1/25, rendered by `legend_swatches.py` from the cue-state geometry (`../cue_states/states.py`, `Tools/print/patterns/hard_edge.py`):
  - `AS PRINTED`: the half-drop baseline, printed and not glowing;
  - `IN POWER FAILURE` head;
  - `WAY ON`: the FLOW swatch, with the guide row turned 90°;
  - `EXIT`: the HERE swatch, the pair either side of a 1.0 m door pointing in;
  - `NO EXIT`: the STOP swatch, the row flattened to bars;
  - `WALLCOVERING SHOWN AT 1/25 SIZE`.
- **Legend glow (mask).** The head, the labels and each changed band (`legend/<state>_cue.png`). This is a phosphor overprint on the placard, as in A.12 ("the legend glows"), and does not depend on the walls' §8 option A/B.
  - Under option A as written, only the cream ink glows. In the dark that leaves the stripe rails and no cue (`legend/<state>_cream.png`). This is reported to narrative for §8.
- **Footer, v2:** `IN CASE OF FIRE: WALK, DO NOT RUN.` / `CLOSE DOORS BEHIND YOU.` (A.12, relay v2 §10), white on safety red. The optional stairs line is left out, since the band has no room.

**Regenerate:**
1. `/usr/bin/python3 legend_swatches.py legend`
2. `swiftc -O placard.swift -o placard_gen && ./placard_gen . legend`

`v1/` keeps the first version: the InkShape legend and the elevator footer.

v1's FLOW arm angle (32.5°) is retired together with the InkShape legend. v2 takes every shape from the print geometry, so it matches the walls by construction.

## Sheet A, the tutorial placard (2026-10-22; Red: "A 先上")

Red picked **sheet A** (route diagram) on 2026-10-22. The design is in `Documentation/research/placard/50_tutorial_placard.md`, and the review is Figma `3459:7280` (TP01, TP06) with VL337. The content contract is 教程设计's (`research/tutorial/10_proposal.md` §5.2).

**The v3 files at the top of this folder are untouched** and stay in the game until 游戏视觉 swaps the art. The new art lives in two folders:

| Folder | What | Swap it in when |
|---|---|---|
| `sheetA_phase0/` | Sheet A without the station and the padlock. Line 4: `EXIT BY STAIR, ONE FLOOR DOWN.` | The C22 stairwell (the floor's EXIT) is in main **and** G1 holds (the placard at P1 west on every seed, 关卡设计 `MapRootFor`) |
| `sheetA/` | Full sheet A: the security desk (the station), the padlocked stair door, line 4 `STAIR DOORS ARE LOCKED. KEY AT SECURITY DESK.` | The stair lock and the station are live (tutorial Option A, approved by Red 10-22 in the 教程设计 chat) |

Each folder holds the v3 file names, so `Tools/lookdev/pack_evac_plan.py` reads it unchanged:
- `placard_print_lit.png` and `placard_glow_mask.png` (2592 × 1674, 6 px/mm);
- the previews at 3 px/mm;
- `examine.json`.

**`examine.json`** (for the kit's `kit.inspect`, `research/inspect/10_design.md` §3.8):
- `regions`: plan, legend, alarm block and title block, as `[x, y, w, h]` in sheet mm from the top-left, with each region's smallest cap. They replace the legend rect that was measured by eye on v3.
- `transcript`: the lines in reading order. `\u0001` marks a glow line.
- Keep the kit's `texelsPerMetre` (the packed texture's density).
- The transcript must match the printed sheet, so they ship together. **Strings signed by 叙事 on 2026-10-22** (`Documentation/research/narrative/40_floors_exit_tutorial_strings.md` §3): `1 WALK, DO NOT RUN.`; line 4 `KEY AT SECURITY DESK.`; legend `SECURITY` / `DESK (KEY)`; plan `SECURITY DESK`. Regenerated 2026-10-22 09:1x, then 10:2x for 叙事's `NO REENTRY` (the start-door label in the NYC LL5 1973 §C26-608.5 form, no hyphen). Then 12:3x, for 叙事 v1.1's fine print `ON ALARM, A FLOOR WARDEN CHECKS EVERY ROOM.` (Red, 12:15 in the 叙事 chat), set right-aligned on the `IN CASE OF ALARM` row in Heros Bold 3.6 mm white, not glowing; the alarm region's smallest cap is now 3.6; the transcript has 16 lines (Phase 0: 15). Current: `sheetA/` print md5 **79b46369…**, glow 99ea4ac6… (unchanged), examine 71dccff6…; `sheetA_phase0/` print **1e5d244b…**, glow 0f42609b… (unchanged), examine 8c6107c5…. Superseded: 59705857… / 60fd4cf1… (10:2x), 96f4b0df… / dcf46c7a… (09:1x), b819a8a9… / c3bd0af5… (DRAFT).

**Glow mask:** YOU ARE HERE, the route, the stair door (with the padlock in the full sheet) and its EXIT. Words and the red block do not glow. k stays 0.158; re-check the dark ratio (≥ 1.30) after packing.

**Regenerate:** `swiftc -O tutorial_placard.swift -o tp_gen && ./tp_gen sheetA A handoff && ./tp_gen sheetA_phase0 A0 handoff`. The same tool renders the review options: `./tp_gen <outdir> A|A0|B|C`.

### The stairwell copy: LEVEL B1 (staged 2026-10-22 13:3x, 游戏视觉's request)

The same sheet with `LEVEL B1` in the header and title block (C28 B levels; 叙事 `40_floors_exit_tutorial_strings.md` §3: the next floor's sheet hangs by the far door).

| Folder | Print md5 | Glow md5 | examine.json | Use it with |
|---|---|---|---|---|
| `sheetA_B1/` | 2bcf6c87… | 99ea4ac6… (= LEVEL 0) | 16c7f397… | a B floor with its own station and a locked EXIT (A35) |
| `sheetA_phase0_B1/` | ce35c086… | 0f42609b… (= LEVEL 0) | e5bfc405… | B1 and deeper while the floor has no station of its own, the stairwell copy by the far door included (A35; `level=B2` … for deeper floors) |

**Pairing (A35, 2026-10-29; `research/narrative/40_floors_exit_tutorial_strings.md` v1.3 §3, 4cb71ce5):** each floor hangs the sheet for its own state. LEVEL 0 = `sheetA_room/`. B1 and deeper = `sheetA_phase0_B1/` (no station, no key, an unlocked EXIT in a station run), until that floor gets its own station; then `sheetA_B1/`. The md5s in the later tables (band ink rev 2, WALL ARROWS) supersede the ones in this table.

- **Where it hangs:** first at C22's 3 × 3 vestibule, by the next floor's start door; in the walkable stair, by the far door.
- **YOU ARE HERE is true** only on the same mount rule as Level 0: beside that door at P1 west, facing into the floor (G1 for floors 2+, 关卡设计 + 游戏视觉).
- **The plan's NO REENTRY** stays true, because the arrival door locks behind you (C22).
- **More floors:** `./tp_gen <dir> A|A0 handoff level=B2` (any floor token). At LEVEL 0 the output is byte-identical to `sheetA/` and `sheetA_phase0/`.

### Print date `PRINTED 11/89` (2026-10-22 14:3x)

叙事 40 §3 (540c7c2a): every printed date is 1989 or earlier, because clocks and screens now show the real month and day in 1990 (Red, in the 游戏视觉 chat, 14:1x). So the title block's `PRINTED 03/90` becomes `PRINTED 11/89` on every sheet. Same length, layout unchanged; glow masks unchanged. Current md5s (they supersede every md5 above):

| Folder | Print | Glow | examine.json |
|---|---|---|---|
| `sheetA/` | d55a4460… | 99ea4ac6… | cc7edfc5… |
| `sheetA_phase0/` | 9d52bba0… | 0f42609b… | fe327e70… |
| `sheetA_B1/` | cceb8267… | 99ea4ac6… | de2fdeff… |
| `sheetA_phase0_B1/` | 15092ba4… | 0f42609b… | 9acf42c4… |

The v3 files at the top of this folder (in the game today) keep `03/90` until sheet A replaces them after C22: no repack.

### Floor 1, the first room (2026-10-22 16:49; rev 2 17:0x; FD-b 20:4x)

**HOLD (pack with A26, not before):** the folders now draw **FD-b**, the room 关卡设计's final plan builds (`~/FrontRoomsMapWork/notes/floor1_start_group_plan.md` §4.1 / §4.8) and spec 61 §4.4 draws (`Documentation/research/station/61_for_red_first_room.md`, 20:25). The footprint is 游戏视觉's pick, 4 × 4, pending 关卡设计's Step 0 run; if Step 0 forces 4 × 3, re-render with that geometry (minutes). The FD-a art (VL411) is gone from these folders.

Red's option 1 (15:24, 叙事 chat) makes the security desk the first room behind the start door; walls Post Room (15:44, 游戏视觉 chat). The Floor 1 sheet draws that room. `geometry.json` in each folder is the input.

| Folder | Print | Glow | examine.json | Use it when |
|---|---|---|---|---|
| `sheetA_room/` | 6cfb42dc… | 347b1c78… | de4dc7ad… | the first room + stair lock + station are live (it replaces `sheetA/` on Floor 1) |
| `sheetA_room_phase0/` | 17a46128… | 1b7c50c6… | 1a8a34e5… | the first room is live, before the stair locks (it replaces `sheetA_phase0/`) |

- **Mount:** the room's north wall, west of E1, centre x 5.5 m at 1.56 m, facing the start door (D). The room never rotates (D mid-south on every seed), so plan-up is always north and the EXIT is always straight ahead (E1 north on D's axis, the stair 12 m due north).
- **Geometry (FD-b, metres from the room's SW corner):** room 12 × 12; D south x 7.5 (2.4 m double door); E1 north x 7.5 (1.6–1.8 m, drawn 1.7); E2 west, row 1, y 4.5 (1.1–1.4 m, drawn 1.25); counter [8.9, 7.2, 10.85, 7.8] (east end 11.0 → 10.85 on 2026-10-29, 游戏视觉's station-look-v1); the NE pier [10.3, 9.9, 12, 12], a solid pipe shaft the map builds as architecture, drawn in poché (`solids`).
- **YOU ARE HERE** marks the reader at the placard; the desk symbol sits by the counter. The placard is 5.4 m from the counter: waived for FD-b (平面视觉 to 游戏视觉, 20:2x).
- **Strings:** all signed. 叙事 signed `BEYOND BREAK: NOT TO SCALE` (`research/narrative/40_floors_exit_tutorial_strings.md` §9) on one condition: it is a legend row keyed by the plan's break mark, so it never reads as "break glass". Rev 2 (17:0x) does that: route stub + break ticks, then the words in 5.0 mm caps, in the next free legend row. Like sheet B's match-line note, it replaces sheet A's sheet-wide `TYPICAL FLOOR · NOT TO SCALE` (never on this sheet). Rev 1 (16:49, the note in 2.8 mm fine print): 71098e73… / a0013b42…; glow unchanged.
- **Regenerate:** `./tp_gen <dir> A1|A10 handoff geom=<geometry.json>`. The Level 0 typical-floor sheet (`sheetA*`) lives on as the stairwell copies (`sheetA_B1*`).

### Phosphor underprint (2026-10-22 20:1x; 游戏视觉's dark check)

游戏视觉's in-game dark check failed the packed Phase 0 sheet (VL386: glyph/paper A4 0.920, dead area 0.724, against the Q16 bar of 1.30; main's v3 reads 1.430). The cause: sheet A's glowing marks were printed in dark ink (red, black), so a partial charge (G 0.39 after 15 s) only lifted them back to the paper.

**The fix, as on a real photoluminescent plan,** is to make the pigment the pale ground:
- A band of phosphor ink (v3's legend ink #D6DEBD), 1.5 mm round every glowing mark, is printed first, and the red and black overprint it.
- The stair enclosure is one phosphor block: the safe area, the route's goal.
- The glow mask is wherever the pale ink stays visible. So the route, YOU ARE HERE, EXIT, the padlock and the treads read **dark on a glowing ground** in a blackout, and never vanish into the paper.

By day the band is a faint pale green behind the red route. Red EXIT on the band is 4.3 : 1 (5.3 : 1 on paper).

**Expected dark ratio:**
- About 1.5 at A4 and 1.4 in the dead area: the print-path proxy, calibrated on VL386's two measured points (v3 1.43, Phase 0 0.92).
- On its glyph pixels the visible-ink share is 0.98 (v3 0.87) and the ink is lighter (0.79 of paper; v3 0.66).
- 游戏视觉 re-measures in game after packing.

The generator's `halo=0` reproduces every earlier folder byte for byte (30/30). `examine.json` is unchanged everywhere (same text). The Level 0 and B1 glow masks are identical.

| Folder | Print | Glow | examine.json |
|---|---|---|---|
| `sheetA/` | ee1ba2e2… | e377878f… | cc7edfc5… (unchanged) |
| `sheetA_phase0/` | be0a97b2… | 2654943a… | fe327e70… (unchanged) |
| `sheetA_B1/` | aa949e44… | e377878f… | de2fdeff… (unchanged) |
| `sheetA_phase0_B1/` | 956777d5… | 2654943a… | 9acf42c4… (unchanged) |
| `sheetA_room/` | 456f6c92… (FD-a; now 6cfb42dc, see Floor 1) | bb042281… | 36fdbadd… (unchanged) |
| `sheetA_room_phase0/` | 94cdaca8… (FD-a; now 17a46128, see Floor 1) | 05ae95db… | 02129a8d… (unchanged) |

The room folders stay on HOLD (first-room layout, above).

### Band ink rev 2 (2026-10-22 21:0x; 游戏视觉's in-game run)

Phase 0 landed in main at 21:03 (游戏视觉; seed 516574485, k 0.158, under load). At the usual start spot (277, 6): A4 **1.466**, dead area **1.310** (bar 1.30; one run 1.300). At the dimmer start spot (85, 2), reached by title timing: A4 0.962, dead 1.266 (v3 1.001 / 1.230). It landed as the interim exception.

- **Rev 2:** the band ink goes 5 % lighter, #D6DEBD → **#DDE5C3**. 游戏视觉's measure stage predicts about 1.32 for the dead area at (85, 2) and 1.33 at (277, 6). Red on the band is now 4.54 : 1 (rev 1 4.25).
- **A4 at (85, 2)** needs about 2.4× the glow (k ≈ 0.37). That changes the glow everywhere, so it is not taken. The sheet reads there by its lit neighbours, and v3 had the same gap.
- `phos=0.839,0.871,0.741` reproduces rev 1 byte for byte (30/30); `examine.json` is unchanged everywhere.

| Folder | Print | Glow | examine.json |
|---|---|---|---|
| `sheetA/` | 20166f58… | 9d3a9480… | 1c7eb716… |
| `sheetA_phase0/` | c7db3728… | ed18539e… | ba00f0ab… |
| `sheetA_B1/` | aae90471… | 9d3a9480… | 8b44cad7… |
| `sheetA_phase0_B1/` | 32cb8099… | ed18539e… | d6fce02e… |
| `sheetA_room/` (HOLD) | 73e371ea… | 98c998a0… | de4dc7ad… |
| `sheetA_room_phase0/` (HOLD) | dd93b94e… | c4a1091d… | 1a8a34e5… |

These supersede the md5s in the tables above, including the Floor 1 table's FD-b rev 1 (6cfb42dc / 17a46128).

### Transcript describer (2026-10-22 20:2x; 叙事 40 §4)

`NO REENTRY (the start door, shut)` → `NO REENTRY (the door into this floor)`. That wording is true on every copy, the stair copies included. Only `examine.json` changes; print, glow and previews are byte-identical in all six folders.

| Folder | examine.json |
|---|---|
| `sheetA/` | 1c7eb716… |
| `sheetA_phase0/` | ba00f0ab… |
| `sheetA_B1/` | 8b44cad7… |
| `sheetA_phase0_B1/` | d6fce02e… |
| `sheetA_room/` | de4dc7ad… (HOLD) |
| `sheetA_room_phase0/` | 1a8a34e5… (HOLD) |

### Wall arrows legend row (2026-10-29 11:0x; Red R2, 叙事 40 §3)

A new legend row on the Level 0 sheets: `WALL ARROWS` / `POINT THE WAY OUT`. Wording agreed with 信息设计 and signed by 叙事.
- **Key:** the wallpaper's turned motif at 1/25 (one column of the K00 print, cut from `legend/flow.png`), 15 × 15 mm with a 0.35 mm frame.
- **Not in the glow mask:** walls never glow.
- **One printing on every platform:** turn-only on the phone shows the same turned cells, without the stepping.
- **Place:** the next free legend row (after SECURITY on the full sheet; after STAIR on Phase 0).
- **What changes:** the print inside the legend only (x 302–397 mm), and the transcript's LEGEND line (`… · WALL ARROWS, POINT THE WAY OUT`). The glow masks are byte-identical (ink rev 2).
- **Room sheets:** unchanged (HOLD); the row goes in with the FD-b redraw.

| Folder | Print | Glow | examine.json |
|---|---|---|---|
| `sheetA/` | beb079aa… | 9d3a9480… (same) | 5ec63e36… |
| `sheetA_phase0/` | 58989ea9… | ed18539e… (same) | 7f0a3169… |
| `sheetA_B1/` | 4a2fea23… | 9d3a9480… (same) | cbcfbf91… |
| `sheetA_phase0_B1/` | de71fb94… | ed18539e… (same) | d1f18872… |

### Room sheets: WALL ARROWS row (2026-10-29 15:1x; still HOLD)

关卡设计 landed A26 (switched off) with the FD-b geometry final in code (plan §4.8). It matches each room folder's `geometry.json` (12 × 12 m, mount N x 5.5, D S 7.5, E1 N 7.5 to the EXIT, E2 W row 1).

- The room sheets now carry the `WALL ARROWS` / `POINT THE WAY OUT` row (one row below BEYOND BREAK).
- On both Floor 1 sheets the north arrow and its note move to y 216, the free band above the title block, and the legend region grows to 178 mm.
- The print changes only in the legend column; the glow masks are pixel-identical; the Level 0 folders are byte-identical (12/12).
- Still HOLD: the counter depth from 游戏视觉's module (the drawn counter is spec 61 §4.4's [8.9, 7.2, 11.0, 7.8]), and A26 being switched on.

| Folder | Print | Glow | examine.json |
|---|---|---|---|
| `sheetA_room/` | 6388b5d4… | 98c998a0… | 513f180f… |
| `sheetA_room_phase0/` | be775489… | c4a1091d… | b39cd67f… |


### Room sheets: final counter (2026-10-29 17:5x; still HOLD)

游戏视觉's checked station-look-v1 plan (`Documentation/research/station/70_station_look_v1_plan.md`) gives the counter as **[8.90, 7.20, 10.85, 7.80]**: a 1.20 m run plus a 0.75 m run, with a 1.07 m aisle east of it to the wall face. The riser block **[10.30, 9.90, 12.00, 12.00]** is unchanged and map-built; the KeySpot (10.30, 10.40) is on its west face.

- Both room folders' `geometry.json` now carry the new "desk"; re-rendered with `geom=<folder>/geometry.json`.
- Only the plan's counter and the SECURITY DESK symbol + name changed (the symbol re-centres on the shorter counter, about 3 px at 6 px/mm). The glow masks and `examine.json` are unchanged; the Level 0 folders are byte-identical (12/12).
- Still HOLD: 游戏视觉 packs the room sheets when A26 is switched on (with 关卡设计). If its build stage moves either rectangle, re-render (minutes).

| Folder | Print | Glow | examine.json |
|---|---|---|---|
| `sheetA_room/` | e11ba9c8… | 98c998a0… | 513f180f… |
| `sheetA_room_phase0/` | 316ffef3… | c4a1091d… | b39cd67f… |
