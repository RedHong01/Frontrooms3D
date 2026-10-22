# NOTE 2026-10-22 13:4x: stairwell sign art from 平面视觉 · 信息设计 (for the stairwell A production)

The art and spec are ready, outside main (after the Alpha; per 叙事's final §5, 4e69ee5b). Nothing has landed in FR3D.

- **Spec:** `/Users/redwang/FrontRoomsVisualWork/info_type/stair_signs/SPEC.md` (v1.1): sizes, placements in our look-dev frame, material, height-map scale.
- **Art:** `/Users/redwang/FrontRoomsVisualWork/info_type/stair_signs/out/` (stairs 0–2 baked).
  - `stencil/` holds the tokens: 2 px/mm, plus `_bleed` and `.svg`.
  - `plates/` holds `<name>_albedo / _height / _mask` PNGs at 4 px/mm.
  - `layout.json` records every run.
  - Generator: `stair_signs.swift`; `./stair_signs <out> 20` bakes stairs 0–19, about 16 MB.
- **Figma:** review board 3545:7368 (SG01 the set, SG02 the three walls). Legibility check VL371 3543:7368. The lit in-game check is ours once the stair lands.

**Changes to our placeholders (apply in the A production):**
1. **Stencil floor token:**
   - On the block, hinge side, in Heros Bold stencil-cut, **cap 381 mm (15 in), not our 0.46 m**. B10+ uses Heros Bold Condensed at 13.5 in.
   - Black enamel #1E1E1C. Baseline 1.20 m; the door-side edge 120 mm off the frame's outer edge; longer tokens grow toward the corner; max 640 mm wide.
   - The paint follows the block normal and the mortar joints (use the `_bleed` overspray mask).
2. **Stair ID sign:**
   - On the wall, latch side, centre 1.524 m, our slot x −0.80. **305 × 356 mm** (12 × 14 in; matches our 0.30 × 0.36).
   - Engraved two-ply plastic, white on black, 4 zinc pan-head screws. Files `stair<k>_exit_id`, `stair<k>_far_id`.
3. **Exit door, stair face:** NO REENTRY / NEAREST REENTRY / ON LEVEL B1. Black on white, **279 × 127 mm** (11 × 5 in), centred on the leaf at 1.524 m, bonded (no screws in a fire door). File `stair<k>_exit_door`.
4. **Far door, stair face:** REENTRY / ON THIS FLOOR. White on black, 279 × 127 mm, same place. File `stair<k>_far_door`.
5. **Far door, floor side (arrival door on B1):** NOT AN EXIT, **305 × 76 mm** (12 × 3 in), black on white, never red. File `not_an_exit`.
6. **Optional:** STAIR A letter plate, 152 × 152 mm, on the maze side of the EXIT door only (never on the far door). File `stair_letter_A`.
- **No floor plate on the leaves:** the level lives on the wall sign. Our 0.30 × 0.20 leaf placeholders become 0.279 × 0.127.

**Material:**
- Two-ply 1.6 mm. Letters are engraved 0.3 mm through the cap to the core; a 1.0 mm 45° bevel outlines the core.
- Height PNG: 255 = 3.2 mm off the surface (face 128, engraved floor 104, screw crowns ~221).
- Smoothness about 0.55 on the cap face and 0.25 on the engraved floor and bevel (`_mask`).

**Per floor:** the tokens move down one per floor. Either bake 20 stairs and load the current set, or set the tokens at runtime from `layout.json` (TMP with TeX Gyre Heros Bold, or glyph quads from `glyphs_*.json` for the stencil).
- 游戏视觉's recommendation, to settle with 关卡设计: **runtime from layout.json.** One shell for every floor fits the seal-frame jump (the shell is identical apart from its tokens); there are no 16 MB of baked sets; and B10+ is unbounded.
- Questions or changes go to 平面视觉 · 信息设计 (local_4e6edeb5-baa1-40d4-9034-0b07606e28f2).
