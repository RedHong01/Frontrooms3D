# Revision preview: every mark, NOW and REVISED (2026-10-30)

Figma RV01–RV06 (`3798:8601` … `3798:8621`). NOW is the live master (instances) or a redraw from its spec or generator at the same scale; REVISED is made from the same geometry under `20_system.md`. Red picks per row; nothing is built yet.

## RV01 · Crosshair marks + key (owner: 交互设计, KeyHud)

| Mark | Revised |
|---|---|
| Dot | No change. |
| Padlock L3·D (18 variants) | Shackle 2.6 → 2.43 px (keyline 4.6 → 4.43); tag body in paper instead of the zone tint (the insert merges; the typed number stays). Asymmetric legs, sizes, motion unchanged. If D1.6 brings floor colours back, only the body fill returns. |
| Key B held (9 variants) | Split ring 1.27 → 2.43 px; tag and tab in paper, number typed on the tag. Silhouette, cut depth, hole, P4 / U·3 motion unchanged. |
| Broken door B·4 | Frame 2 → 2.43 px; mitred corners, square ends, sharp leaf corners. |
| Eye E·1 | Lids 2 → 2.43 px; lids meet in sharp corners instead of round caps. |

## RV02 · Gait, stamina, hold, touch controls (交互设计)

| Mark | Revised |
|---|---|
| Stamina dashes | Yellow → paper; spent white 25 % → empty slots; keyline; gait glyph beside. |
| Hold E (glass) | Desktop bar 120 × 4 → the ring round the dot (dashed track, solid progress, one stem), as on touch. |
| Touch stick | One weight 2.06 pt (was 1.5 / 2 / 3.5); lit run line and thumb in paper, not yellow; dashed = not yet, solid = now. |
| RUN button | On = paper disc, ink word; winded = dashed ring. |
| USE button | Pressed = paper disc instead of yellow. |
| Pause | Keycap box (35 % fill) → disc like USE and RUN. |

## RV03 · Touch legend glyphs (交互设计, FrontRoomsTouchSprites)

One stroke 2.06 pt; no partial alpha (ghost parts dashed); RUN BUTTON = solid disc (no longer a bare ring like USE and MOVE); DRAG ends in a hollow ring; run arc ±60° → ±70°; the mini USE before the phone prompt = the same 22 pt glyph with the 1 pt keyline.

## RV04 · Placard pictograms (平面视觉, tutorial_placard.swift)

| Mark | Revised |
|---|---|
| You are here | Ring 0.6 → 1.03 mm, dot 3.4 → 3.6, outer Ø 12.4 mm. |
| Route head | 64° dart (4.2 × 5.2) → 90° head (3.6 × 7.2); line stays 1.4 mm. |
| Padlock (legend) | w 8 → 11 mm, shackle 0.53 → 1.03 mm; height 6.9 → 9.6 mm (over the 7 mm minimum). |
| Key tag | Ring 0.26 → ≥ 0.6 mm; floor token typed in the insert. |
| North arrow | Ring 0.35 → 0.66 mm; dart → shaft + 90° head. |
| Stair in plan | Treads 0.35 → 0.6 mm; run head 58° → 90°. |
| TV sound arcs | Stroke = gap = 1 s, radii 3.5 / 5.5 / 7.5 s: the HUD glyph's arcs. |
| Steps | Stub at 1 s; prints = the HUD glyph's prints. |
| Legend row | 14 s field per symbol; label 2 s after the field. |

## RV05 · Wallpaper WP03 and the wall arrow (pattern: 平面视觉; print + cue: 游戏视觉)

`ARM_DEG` 55 → 45 in the same generator: chevron stacks and motif-band arrows at 45°, diamonds square-on (49.8 × 49.8 mm), the turned wall arrow becomes the system's 90° head and fits its field (167 mm in 207 mm, today 212 mm). Stripes, inks, band thicknesses, repeat and the cue motion (A + M4 + V1) unchanged. Cost: re-encode the print (K00) from the 45° SVG; update the cue fit; re-cut the placard's WALL ARROWS swatch.

## RV06 · Signs, menus, what stays

- Key map: pixel padlock → the word FIXED (Plex Mono 13 paper; word via 叙事设计).
- Key panel rule: yellow → paper. Threat readout (assist): yellow / muted → paper with keyline.
- Stay as they are: EXIT, stencil floor numbers, plates, NOT AN EXIT, ISA (1990 codes, words first); the settings kit (signed off 10-24); print forms; the wordmark (广告); status tags and shot chips (documents).
- Open: floor notation (00 / 0 / LEVEL 0 / B1 / LEVEL B2) for 信息设计 + 叙事设计; tag shapes wait for the map's D1.6 rule.
