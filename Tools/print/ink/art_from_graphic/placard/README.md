# Evacuation-plan placard (EGRESS, A.12), from 平面视觉

A framed 1990 US evacuation plan for the start door: 432 × 279 mm (17 × 11 in, landscape). The strings are narrative's (`30_narrative_phosphor.md` §4 A.12); the typography and layout are 平面视觉's; the prop is the visual chat's.

| File | What |
|---|---|
| `placard_print_lit.png` | The printed sheet at 6 px/mm (2592 × 1674): black and safety red on off-white. The legend is in pale phosphor ink (#D6DEBD), faintly visible by day |
| `placard_glow_mask.png` | The glow mask at the same size: white = phosphor ink, black = none. Use it as the emission mask, gated by the placard cell's lamp like the walls |
| `placard_lit_preview.png`, `placard_dark_preview.png` | Review renders at 3 px/mm |
| `placard.swift` | The generator: `swiftc -O placard.swift -o placard_gen && ./placard_gen <outdir> [legend dir]` |
| `legend_swatches.py`, `legend/` | The v2 legend swatches and masks (see below) |
| `v1/` | The 2026-10-03 version |

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
