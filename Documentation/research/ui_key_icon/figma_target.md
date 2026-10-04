# Where the KEY ICON block goes in Figma: UPDATED 2026-10-03 16:5x (supersedes the earlier version)

**DO NOT BUILD ANY FIGMA BLOCK in this workflow.** Not inside UI MOCKUP 2256:8, and not the x 68657 fallback.

Red asked 平面视觉 to regenerate every current key-HUD UI variant in Figma with NATIVE layers, in a new section "FRONTROOMS · HUD KEY · UI VARIATIONS". Our files are its source:
- the 33 SVG masters in `design/svg/`, imported as components;
- the layout constants in `composites.py`;
- the four background frames (lit, wallpaper, office, dark).

A second block from us would duplicate it.

What the Figma stage does instead:
1. Write `03_figma.md` as a **hand-off note for 平面视觉**:
   - the final list of SVG masters (file → state/size/zone);
   - the final `composites.py` constants: row top, label baseline, full-form box, the 4 px rule, the KEY + Courier Prime Bold numeral sizing against Bayon cap height;
   - the opacities, with a note that Unity blends in linear space and Figma in sRGB;
   - the background frames;
   - the recommended direction and the states.
2. Make sure every file it lists exists and is final.

The critic and fix stages review the design files and this hand-off, not Figma.

## 03_figma.md must list exactly (平面视觉's request, 2026-10-03 17:0x)
1. **The final variant list:** one row per rendered variant, as direction × state × zone × label × background (lit / wallpaper / office / dark) × 1x/2x, with each row's file path(s).
2. **Every crop box** used for the composites (background frame, x, y, w, h in px), so 平面视觉 can fit the equivalent alpha per background and per crop.
3. **Every constant** from `composites.py`: row top 940, label baseline 958, full form from 914 at 48 high, the 4 px rule, glyph sizes, the KEY + numeral sizing against Bayon's cap height, spacing, colours, opacities as linear values (40 / 55 / 60 / 70 / 90 % etc.), and the states (other at 40 % / 70 %, used, missing hint), with their final values.

Mark the file FINAL at the top only after the fix stage. The visual chat will then ping 平面视觉.
