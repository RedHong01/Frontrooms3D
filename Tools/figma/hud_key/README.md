# HUD key · Figma build (平面视觉)

These scripts rebuild every key-icon HUD variation natively in Figma: file 0tCbAiVUlrPId3RWd9LRif, page 2099:76, section **2532:4038** "FRONTROOMS · HUD KEY · UI VARIATIONS (平面视觉)", at x 68657, y 2000.

The source is the game-visual chat's key-icon pipeline:
- the SVG masters in `Documentation/research/ui_key_icon/design/svg`;
- the layout and states in its `composites.py`;
- the PNG reference set in `design/hud`.

Direction A (cut key) is the recommended one; see `ui_key_icon/02_design.md`.

## What is in the section

| Frame | Content |
|---|---|
| KV01 | Five glyphs × held / no key / used, at ×3 |
| KV02 | Held on the four game frames |
| KV03 / KV04 | Four states, on lit and on dark |
| KV05 | B · tag by zone |
| KV06 | A + chip and C · plate by zone |
| KV07 | The number (label faces), 1x and 2x |
| KV08 / KV09 / KV10 | 2x views |
| KV-LIB `2579:4775` | The 33 SVG masters as components, in 4 variant sets, plus the crosshair component |
| KV-SRC `2532:4039` | The four game frames and the crosshair PNG, used as image fills |
| 20 full frames | `<dir>_fullframe_<bg>`, the full 1080p screens |
| 114 twins | One component per 1x PNG in `design/hud`, with the same name. The boards show them through clipped views; 2x views are scaled instances. |

## Fidelity rules

- **Blending.** Unity, and the composites, blend the overlay canvas in linear light; Figma blends in sRGB.
  - `fits.py` fits an sRGB alpha for every semi-transparent element, per crop, on that crop's own background pixels:
    - the other-zone state: glyph 40 %, label 70 %;
    - C plate 90 / 60 %;
    - the missing outline, 70 %.
  - Each twin stores these as instance or layer overrides. The components keep the engine values.
- **Opacities inside a glyph.** A used tag or chip fill is 55 % over the paper rim. It is flattened to the exact linear-light mix, e.g. #A4827B for red.
- **Text placement.**
  - Text sits on the composite's pen position and baseline. The baseline offset is measured in Figma by flattening an 'H' at the given size and line height.
  - Figma kerns and the composites don't, so labels can differ by under 1 px.
- **Source Serif 4.** Figma forces automatic optical size; only `wght` can be set. Unity draws the variable font's default instance, opsz 20, so the 50 px room name is outlined from 20 px text scaled ×2.5.
- **Label digit sizes.**
  - Courier Prime Bold is 24.63 px and Plex Mono 20.45 px, as in `composites.py` on 2026-10-03.
  - Unity legacy Text only renders whole sizes (25 / 20). The game-visual chat will round them in its next pass.

## Re-sync after the game-visual chat changes the SVGs or constants

1. Set `FRONTROOMS_ICONLIB_ROOT` to the external `keyicon_design/` folder, then run `python3 plan.py plan.json`. It runs their `composites.py` with recording stubs and never writes to their folders.
2. Run `python3 fits.py plan.json plan_fit.json`.
3. Run `python3 layout.py plan_fit.json layout.json`.
4. Changed SVGs: re-import them with `upload_assets` (multipart, filename = layer name). Then swap them into the KV-LIB variant sets, or rebuild the sets.
5. Changed crops: rebuild the affected twins with `crops.js` (`prelude.js` + DATA). The board instances follow automatically.

**Dependencies:**
- Python with numpy and PIL.
- The game-visual chat's `keyicon_design/` (composites.py, iconlib.py), supplied through `FRONTROOMS_ICONLIB_ROOT`.

**Figma IDs:** `figma_ids.json` (backgrounds, library and components) and `twin_ids.json` (twin name → component ID).

**Verification, 2026-10-03.**
- 8 twins sampled across all directions, states, zones and labels: mean |Δ| 0.24–0.54 per channel (of 255) against the PNGs. Only edge anti-aliasing differs.
- Full frames: the HUD, crosshair and room header land within 1 px of the reference.
