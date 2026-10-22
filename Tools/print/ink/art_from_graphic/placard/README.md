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
| `sheetA_B1/` | 2bcf6c87… | 99ea4ac6… (= LEVEL 0) | 16c7f397… | the stair lock + station live (as `sheetA/`) |
| `sheetA_phase0_B1/` | ce35c086… | 0f42609b… (= LEVEL 0) | e5bfc405… | before the lock (as `sheetA_phase0/`) |

- **Where it hangs:** first at C22's 3 × 3 vestibule, by the next floor's start door; in the walkable stair, by the far door.
- **YOU ARE HERE is true** only on the same mount rule as Level 0: beside that door at P1 west, facing into the floor (G1 for floors 2+, 关卡设计 + 游戏视觉).
- **The plan's NO REENTRY** stays true, because the arrival door locks behind you (C22).
- **More floors:** `./tp_gen <dir> A|A0 handoff level=B2` (any floor token). At LEVEL 0 the output is byte-identical to `sheetA/` and `sheetA_phase0/`.

