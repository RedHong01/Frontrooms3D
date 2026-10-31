# Annunciator label atlas: cell layout for 平面视觉

游戏视觉, package `station_labels` (2026-10-31). Asked by 平面视觉 on 10-31 at 01:2x. 叙事 signs the words (Inbox: A by level, B by area).

## The atlas

| | |
|---|---|
| File | `Assets/Resources/Surfaces/Textures/Prop_AnnunciatorLabel_A.png` (keep its `.meta`) |
| Size | **2048 × 1024 px**, RGB, sRGB |
| Scale | **6 px/mm** (0.1667 mm a pixel) in both directions, so the strips are not stretched. Drawing at 12 px/mm and halving works |
| Ground | **#C9C3B2**, the label colour the material had before. The texture now carries the colour and the material's `_BaseColor` is white, so your art shows as drawn |
| Material | `Prop_AnnunciatorLabel` (FrontRoomsSurface, mesh UVs, smoothness 0.3, no normal map) |
| Kit | `Kit_FireAnnunciator` (30L S12). Only its UVs changed. A static mesh compare in Blender found every vertex and face in the same place in LOD0, LOD1 and LOD2 |
| Today | Blank ground everywhere (`Tools/lookdev/gen_annunciator_label.py`), so the panel looks as it did before |

## Cells

Rects are in px from the atlas's top-left corner: x, y, width, height. UV rects are u0, v0, u1, v1, with v pointing up.

| Cell | On the panel | Rect (px) | Size | UV rect |
|---|---|---|---|---|
| `strip1` | label beside lens 1 (top) | 64, 44, 900, 96 | 150 × 16 mm | 0.031250, 0.863281, 0.470703, 0.957031 |
| `strip2` | lens 2 | 64, 184, 900, 96 | 150 × 16 mm | 0.031250, 0.726562, 0.470703, 0.820312 |
| `strip3` | lens 3 | 64, 324, 900, 96 | 150 × 16 mm | 0.031250, 0.589844, 0.470703, 0.683594 |
| `strip4` | lens 4 | 64, 464, 900, 96 | 150 × 16 mm | 0.031250, 0.453125, 0.470703, 0.546875 |
| `strip5` | lens 5 | 64, 604, 900, 96 | 150 × 16 mm | 0.031250, 0.316406, 0.470703, 0.410156 |
| `strip6` | lens 6 | 64, 744, 900, 96 | 150 × 16 mm | 0.031250, 0.179688, 0.470703, 0.273438 |
| `strip7` | lens 7 | 64, 884, 900, 96 | 150 × 16 mm | 0.031250, 0.042969, 0.470703, 0.136719 |
| `strip8` | lens 8 | 1084, 44, 900, 96 | 150 × 16 mm | 0.529297, 0.863281, 0.968750, 0.957031 |
| `strip9` | lens 9 (bottom): **TROUBLE** | 1084, 184, 900, 96 | 150 × 16 mm | 0.529297, 0.726562, 0.968750, 0.820312 |
| `power` | the **POWER** strip by the green jewel | 1084, 324, 360, 96 | 60 × 16 mm | 0.529297, 0.589844, 0.705078, 0.683594 |
| `lens` | all nine frosted lenses | 1084, 464, 96, 96 | (16 × 16 mm) | 0.529297, 0.453125, 0.576172, 0.546875 |

The order runs top to bottom on the panel. Strips 1–8 are the eight zones and strip 9 is TROUBLE. Which zone word goes on which strip is 叙事's call, and swapping words between strips changes only the art.

## How a cell lands on the strip

- **One cell is one strip face, read from the room:** left to right, top up, not mirrored. The cell's edges are the strip's edges.
- **Keep words at least 1.5 mm (9 px) inside the cell.** The 0.3 mm bevel round the strip uses the outer 2 px of the cell, and the strip's 1.2 mm sides collapse onto the cell's border pixels. Keep those border pixels at the ground colour.
- **Where the lens sits, seen from the room:** each lens is about 17 mm off its strip's **right** end. The green POWER jewel is about 9.5 mm off the POWER strip's **left** end. Align the words to suit.
- **`lens` stays plain ground** (#C9C3B2): it colours the nine frosted domes. Everything outside the cells is never sampled. Keep it at the ground colour too, so lower mip levels do not pick up ink.
- **Scale check:** at the closest the player can stand (about 0.35 m, FOV 76°), a 4K screen shows about 3.95 px/mm, a 1440p screen 2.6 and a MacBook Pro 4.1. At 6 px/mm the atlas stays sharper than the screen for all of them. The panel normally sits 49° off the take view, 1 m from the key.

## Delivering the art

Replace the PNG at the same path, the same size (2048 × 1024, RGB, sRGB). Keep the `.meta` (GUID `6749b3a2…`). Nothing else changes, and the cells match the kit's sidecar (`Kit_FireAnnunciator.json`, `labelAtlas`). For a drawing guide with every cell outlined and named, run `python3 Tools/lookdev/gen_annunciator_label.py --template <png> --out <scratch png>`. The template is not for the game.

## Also in this package: the STAIR A desk fob

- **Where it prints.** `STAIR A` is drawn into `Prop_KeyTagNo_A` by our generator `Tools/lookdev/gen_keytag_atlas.py`, to your spec of 10-31: Courier Prime Regular, ink #171717, one line, centred, no tracking, advance = 85 % of the window.
- **Its band.** Every one of the atlas's 100 cells is already in use, so the band sits outside them. It is column 9, centred on the row 8 / row 9 boundary: x 3686.4–4096 and y 3627.0–3745.8 px, in the 4096² atlas. That area is blank margin that no other tag, plate or label crop reaches. The tag reaches it with `_BaseMap_ST` (1, 1, 0.9, −0.85).
- **Its proportions.** The long tag's window is 50 × 14 mm, but it shows a crop of the full cell width × 0.29. That stretches the atlas 3.57 % wider than tall. The lettering is drawn pre-squeezed by the same amount (8.192 px/mm across, 8.485 px/mm up), so on the tag it reads true:
  - at 1×: em 10.12 mm, cap 5.87 mm = 0.419 of the window height
  - at the fob's 1.5×: cap 8.80 mm in a 75 × 21 mm window
- **The floor token** stays on the HUD tag only.
