# Glow-ink hand lettering (layers 8 and 9), from 平面视觉

Hand-lettered artwork for `_FR_InkType` layer 8 (forged FLOW) and layer 9 (GROUND scratch cluster). The strings and the shader rules are in `Documentation/research/wallpaper_motion/30_narrative_phosphor.md` §4 A.11.

| File | What |
|---|---|
| `layer8_forged_THIS_WAY_OUT.svg` / `.png` | `THIƧ WAY OUT`, monoline marker, tiles seamlessly in u and v |
| `layer9_scratch_cluster.svg` / `.png` | Scratched cluster: tally (4 gates + 3), `IT ONLY GOES IN`, `SAME ROLL AGAIN`, `COUNTED 41 DOORS`, `R.M. 6/90`, `D.K. 11/90`. Stays inside x 64–715 mm, v 0.24–0.65, and never crosses the tile edge |
| `preview_layer8.png`, `preview_layer9.png` | Pale ZnS:Cu-green previews on black, for review only |
| `inkart.swift` | The generator: `swiftc -O inkart.swift -o inkart && ./inkart <outdir>`. It is deterministic (seeds 1990 and 1993) |

**Format.**
- The SVGs are the source: viewBox `0 0 750 750`, 1 unit = 1 mm, y down, black strokes on transparent.
- Strokes use round caps and joins. Layer 8 has a pen-rest dot at each stroke start.
- The PNGs are 4096 × 4096 px (5.46 px/mm, 4× supersampled), black on transparent. The generator applies the value encoding.

**The hand.**
- **Layer 8, the forger.** A 2.8–3.6 mm marker, one width per line.
  - Letters are single strokes in stroke order. They imitate the printed Helvetica caps but come out narrower.
  - Lines slant forward 3.5–6.5°, the S is mirrored (Ƨ), and every stroke starts with a small pen-rest dot.
  - The baseline drifts smoothly along the line, not letter by letter.
  - There are three phrases per line with uneven gaps, the line period is exactly 750 mm, and the 20 line pitches sum to 750 mm (37.5 ± 2 mm).
- **Layer 9, scratched.** Strokes are 2.5–2.9 mm.
  - Curves break into straight segments (scratched with a key or coin) and corners kink.
  - About half the stroke ends slip past the letter, and some strokes are scratched twice.
  - Each item has its own size, slant and rotation (±6°).
