# W2 three-view hand-off (for 平面视觉 · 印刷与演示)

From the visual chat (游戏视觉), W2 office props workflow, build stage, **2026-10-22**; `Kit_Torchiere` re-rendered in fix round 1 (08:16) and **all four kits re-rendered in fix round 2 (13:34–13:35)**, same renderers and scales. Four kits: the brass torchiere and the armless task chair (both replaced in place, same names), plus two new kits, the navy chair and the old black armed chair.

PROP KIT `2324:852` belongs to 平面视觉. K40 (`Kit_Torchiere`, node 2328:1066) and K04 (`Kit_TaskChair`, node 2325:947) now show models that no longer exist and need these renders. `Kit_TaskChair_Navy` and `Kit_TaskChair_Armed` are new and have no sheet yet. We did not edit Figma.

## What is here

| Path | What |
|---|---|
| `index.json` → `kits` | One K-sheet record per kit in the `INDEX_FORMAT.md` fields (`kit`, `title`, `lede`, `dims_mm`, `era`, `materials`, `variants`, `images`), plus `sheetPpm`, `px` and `tags`. It follows the same schema as `research/outlets/threeview/index.json`. |
| `index.json` → `assets` + `assetsMeta` | The same kits in the `assets` format, with `tris_lod2` added. `assetsMeta` holds the units, the image pattern and the render clone. |
| `<Kit>_{front,side,top}.png` | Third-angle orthographic views at 250 px/m. 1 PNG px = 1 sheet px, so place them at 100 %. Transparent, with 16 px clear padding. |
| `<Kit>_hero.png` | A 3/4 hero from the front-left, 1152 × 864, transparent. |
| `handoff_manifest.json` | The renderer's record of the files above: px sizes, bounds, slots and triangle counts. |
| `2x/` | The same views at 500 px/m (place at 50 %) with that renderer's `manifest.json`, for print. |

## Things to know

- **Every kit is furniture (≥ 0.75 m),** so all four share one scale, 250 px/m. That makes sizes comparable sheet to sheet.
- **Front is the kit's +Z.** For the chairs that is the seat edge. Since fix 2 the torchiere's dimmer knob sits on the right side of the cup as seen from the front (where Sears N has its switch), so it shows in profile in the front view. No kit is turned.
- **The torchiere is off.** The frosted glass bowl is drawn opaque (`Prop_FrostedGlass`), and nothing is emissive; in the game its outside takes the room light through tilted normals, which a studio render shows only as an even white bowl. A lit variant would be a separate kit, decided later by Red.
- **Fix 2 shapes:** the bowl has 6 bands and 5 ledges of 8 mm (the notches show in the outline) and its glass lip shows 4.5 mm above the brass band; the armless chairs show 2 cm of chrome piston (was 6 cm).
- **Materials.** Since fix round 1 (2026-10-22) the torchiere has two slots of its own, `Prop_BrassPolished` and `Prop_FrostedGlass` (see `../20_build.md` §12, §14); the chairs use existing slots. Swatch hex = the linear average of the albedo texture × `_BaseColor`:
  - polished brass `#F2B852` (fix 2, warmer after the game's grade; fix 1 `#F2DE9E`; `Prop_Brass` `#B08A4A`);
  - frosted bowl `#EBE7DE` (was `Prop_PlasticWhite` `#D9D5C8`);
  - grey fabric `#5A646E`;
  - navy fabric `#23304A`;
  - black fabric `#1C1C1E`;
  - black plastic `#1A1A1A`;
  - chrome `#D8D8D8`.
- **Variants:** `Kit_TaskChair` lists `_Navy` and `_Armed` as its variants. The navy chair is the same mesh as `Kit_TaskChair` with one slot swapped. The armed chair is the pre-W2 black chair, unchanged.

## Re-rendering

The renderer is `../../../interactables/threeview/FrontRoomsThreeView.handoff.cs.txt`. Copy it into a clone that holds the W2 kits, as `Assets/Editor/Rendering/FrontRoomsThreeView.cs`, then run:

```
Unity -batchmode -projectPath <clone> -buildTarget OSXUniversal -executeMethod FrontRoomsThreeView.RunBatch \
  -threeViewOut <dir> -threeViewOnly Kit_Torchiere,Kit_TaskChair,Kit_TaskChair_Armed,Kit_TaskChair_Navy \
  -threeViewMaxPpm 2000 -threeViewOversample 1 -quit -logFile <log>
```

For the `2x/` set, leave out the last two flags. Fix 2's runs took 15–20 s under load (1-minute load 42–52), with 0 failures. Clone: `W/proj_w2`. Logs: `W/w2_props/logs/115_threeview_2x.log`, `116_threeview_1x.log`.
