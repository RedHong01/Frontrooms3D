# Outlets three-view hand-off (for 平面视觉)

From the visual chat (游戏视觉), outlets workflow, fix pass, **2026-10-14**. It covers the 20 outlet kits: wall plates, switches, jacks, the handy box with its EMT conduit and strap, two floor boxes and the powered cubicle panel. 平面视觉 places the K-sheets itself, as new sheets in PROP KIT `2324:852`. We did not edit that section.

## What is here

| Path | What |
|---|---|
| `index.json` → `kits` | One K-sheet record per kit, in the `INDEX_FORMAT.md` fields of the interactables hand-off: `kit`, `title`, `lede`, `dims_mm`, `era`, `materials` (name, hex, label), `variants`, `images`. |
| `index.json` → `assets` + `assetsMeta` | The same 20 kits in the 10-field `assets` format (`name`, `fbx`, `dims_m`, `tris_lod0`, `tris_lod1`, `materials`, `variants`, `level`, `era_note`, `suggested_sheet_scale`). `assetsMeta` holds units, the image pattern, the render clone and what was dropped. |
| `<Kit>_{front,side,top}.png` | Third-angle orthographic views at `suggested_sheet_scale`: 1 PNG px = 1 sheet px, so place them at 100 %. Transparent, with 16 px clear padding. |
| `<Kit>_hero.png` | A 3/4 hero, 1152 × 864, transparent, from the front-left and 22° down. |
| `handoff_manifest.json` | The renderer's record: px sizes, bounds, `sheetPpm`, slots, tags. |

## Things to know

- **Front = the plate face** (the kit's +Z). No kit is turned.
- **Scales:**
  - most plates, jacks and floor boxes are at 2000 px/m;
  - the 1 m conduit tile and the powered panel at 250;
  - the screw and the strap at 8000, so they read.
- **The conduit tile's top view is empty:** the open tube's wall is 1 mm wide, which is 0.25 px at 250 px/m.
- **Materials are the P-4b proposal,** not main yet:
  - ivory thermoset `#E6DDC2`, ivory nylon device `#E2D9BF`;
  - brushed stainless `#C7C7C2` (plates), galvanized `#B3B5B3` (handy box, EMT, strap, floor boxes).
- **The ivory tint is Red's call** (VL277):
  - option C: plate `#B6AF99`, device `#B3AB97`;
  - option D: `#A69F8C` / `#A39C89`.
  - If Red picks one, only the ivory swatch hex changes; the shapes do not.
- **Until P-4b lands,** the game draws the ivory with `Prop_PlasticBeige` / `Prop_PlasticPutty` and every metal with `Prop_AluminiumAnodised`.
- **Colours are not separate kits.** The renderer picks plate and device colours per fixture: ivory, aged, almond, white, brown, grey, stainless, papered or painted over, and an orange IG device. So `Kit_OutletDuplex_Brown` and `Kit_OutletDuplex20_IG` are dropped. Show colours as variant swatches, not as kits.
- **Heights are not in the kit.** The planner places them:
  - receptacles at 0.31 / 0.41 / 0.46 m to centre (1.07 m off-height);
  - switches at 1.22 m on Office walls and 1.12 m on printed Level 0 walls.
  - The 1.12 m keeps the plate below the wallpaper's cue band (1.184–1.391 m). Please confirm it with the cue owner (`30_contract_map.md` §7.2).
- **Kits drawn but not built as GameObjects:** every outlet except the powered panel is drawn by the renderer. There is no prefab to drag into a scene; use the FBX.

## Re-rendering

Same tool as the interactables hand-off: `../../interactables/threeview/FrontRoomsThreeView.handoff.cs.txt`. Copy it to `<clone>/Assets/Editor/Rendering/FrontRoomsThreeView.cs` in a clone that holds the outlet kits (main after `outlets_int`; add the P-1 LODs and the P-4b materials for these exact swatches). Then run:

```
Unity -batchmode -projectPath <clone> -buildTarget OSXUniversal -executeMethod FrontRoomsThreeView.RunBatch \
  -threeViewOut <dir> -threeViewOnly <names from index.json assets> -threeViewMaxPpm 2000 -threeViewOversample 1 -quit -logFile <log>
```

Use `-threeViewMaxPpm 8000` for `Kit_OutletScrew` and `Kit_ConduitStrap`. Our run took 2.5 minutes for the 20 kits under load: 0 failed, 1 warning (the empty conduit top). Log: `W/outlet_fix/logs/threeview.log`.
