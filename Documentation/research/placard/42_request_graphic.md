# 42 — Request to 平面视觉: print the placard's wallpaper swatches in the game's colours

From the visual chat (游戏视觉), Q16 fix stage, 2026-10-08. **A request, not a change:** the art stays yours, and the approved v2 ships as it is until you answer.

## What we found

The v2 legend draws the four WP03 swatches (AS PRINTED, WAY ON, EXIT, NO EXIT) in the reference palette of `Tools/print/patterns/hard_edge.py`. The game prints the same pattern on every Level 0 wall through `LobbyPrint` (`FrontRoomsRenderSetup.cs`), via the print's density/cream encoding. So beside the real wall the swatches read as a pink and grey paper the player never sees (in game: VL204, `images/q16_fix_P2_swatch_v2.jpg`).

| Ink | v2 swatch (reference) | The wall in game |
|---|---|---|
| ground | #EEDDCC | #C8B871 |
| cream | #F4F0EC | #D9CA86 |
| grey (also the changed cue band) | #BBBEC0 | #AB9952 |
| pink | #DBAFA5 | #A4934E |
| slate | #9B9BAA | #83763B |
| deep | #887799 | #766A34 |

The right column is the game's own mapping: `hard_edge.encode_table` (density and cream per ink), then `print_tool.shade` with the Lobby palette (#D2C27C / #AC9A52 / #766A34 / #E3D594).

## The candidate we built (for you to adopt, redo or reject)

- `fix_src/candidate_art/legend_swatches_cand.py.txt`: your `legend_swatches.py` with one block added that remaps `PAL` as in the table (`legend_swatches.diff`, 20 lines). Shapes, sizes, the `_cue` and `_cream` masks: unchanged.
- `fix_src/candidate_art/placard_print_lit_CANDIDATE.png` (2592 × 1674, md5 5f71409a…) and `placard_lit_preview_CANDIDATE.png`: `placard.swift` run on those swatches. Everything outside the four swatches is byte-identical to v2, and the glow mask is byte-identical (we regenerated v2 first and got your files back byte for byte).
- Side by side: `images/q16_fix_legend_art_v2_vs_cand.png`; in game beside the wall: `images/q16_fix_P2_swatch_cand.jpg`, `q16_fix_P1_swatch_cand.jpg`.

## What it costs

- In the dark (own lamp off, 2 lit neighbours, k 0.158) the glowing bands sit on darker ink, so the legend reads at **1.37×** the paper at P1 and **1.32×** at P2, against 1.44 and 1.38 with v2. Both still clear the 1.30 bar.
- By day the changed band (grey → #AB9952) is close to the pink band's colour (#A4934E), as it is on the real wall. The turn or flattening still reads by shape, and the overprint's glow carries it in the dark.

## What we need back

- New `placard_print_lit.png` (and previews) at the same path and size, 2592 × 1674. Keep `placard_glow_mask.png` as it is, unless you also change the mask.
- If you prefer other colours (for example the wall *under its lamp*, or a printer's flat approximation of them), any choice is fine as long as it reads as the wall's paper.
- Then the visual chat re-runs `Tools/lookdev/pack_evac_plan.py`, re-renders the three-view (`threeview/index.json` has a `pending` note) and re-checks the dark legibility. No mesh, UV or code change.
- Figma T24 (2504:8) shows v2; update it with the art if you adopt this.
