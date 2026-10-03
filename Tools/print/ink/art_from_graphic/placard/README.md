# Evacuation-plan placard (EGRESS, A.12), from 平面视觉

A framed 1990 US evacuation plan for the start door: 432 × 279 mm (17 × 11 in, landscape). The strings are narrative's (`30_narrative_phosphor.md` §4 A.12); the typography and layout are 平面视觉's; the prop is the visual chat's.

| File | What |
|---|---|
| `placard_print_lit.png` | The printed sheet at 6 px/mm (2592 × 1674): black and safety red on off-white. The legend is in pale phosphor ink (#D6DEBD), faintly visible by day |
| `placard_glow_mask.png` | The glow mask at the same size: white = phosphor ink, black = none. Use it as the emission mask, gated by the placard cell's lamp like the walls |
| `placard_lit_preview.png`, `placard_dark_preview.png` | Review renders at 3 px/mm |
| `placard.swift` | The generator: `swiftc -O placard.swift -o placard_gen && ./placard_gen <outdir>` |

**System, shared with the wall ink.**
- **Face:** TeX Gyre Heros Bold (Helvetica Bold stand-in), caps.
- **Title block:** the same as the wall stamp, scaled down: LEVEL 0 | SHEET A-2 OF 4 | PRINTED 03/90, with 0.6 mm rules and 3.6 mm caps. The wall stamp is the next sheet, A-3.
- **Plan:** 1990 US architectural conventions.
  - Poché walls 0.156 m thick and a door that swings out with its arc.
  - 6 m column grid with A/B and 1/2 bubbles, a north arrow and `SCALE: 3/16" = 1'-0"`.
  - `YOU ARE HERE` in red with a dashed red route to `EXIT`.
  - A dash-dot match line marked `MATCH LINE — SEE SHEET A-3`; the area beyond it is left blank.
- **Legend (glows):** `IN POWER FAILURE`, then the InkShape silhouettes at a declared 1/15 size:
  - FLOW chevrons: 400 mm tall, 100 mm stroke, 32.5° arms, 375 mm pitch;
  - HERE: two 100 × 900 mm bars beside a 1.02 m door;
  - STOP: two 600 × 120 mm bars, 375 mm apart.
  - Labels: `WAY ON`, `EXIT`, `NO EXIT`.
- **Footer:** `IN CASE OF FIRE DO NOT USE ELEVATORS`, white on safety red.

The FLOW arm angle is provisional (narrative asks 30–35°; the final angle is the visual chat's). Change `ang` in placard.swift and re-run so the legend always matches the walls.
