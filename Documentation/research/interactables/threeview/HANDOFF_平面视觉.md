# Hand-off to 平面视觉: interactables kit, as built (2026-10-14)

From the visual chat (游戏视觉), kit-threeview workflow, Figma stage. This page is the cover note; the full reference is `README.md` in this folder.

You own the K-sheets in PROP KIT `2324:852`, and all type and UI. We did not edit that section, and we did not place or re-place any K-sheet. Our own proposal section, DOORS + WINDOWS `2748:6099` (now at x 25257, y 13920), shows the same renders; it is ours, so please only read it.

## 1. Where everything is

| What | Path |
|---|---|
| **index.json** | `/Users/redwang/Desktop/ArtCenter/Fall26T7/EGAM-401A-01 Individual Game Project/Frontrooms3D/Documentation/research/interactables/threeview/index.json` |
| Current models | `assets` block (57 entries, fields `name, fbx, dims_m, tris_lod0, tris_lod1, materials, variants, level, era_note, suggested_sheet_scale`). **Where it disagrees with `sheets` / `kits`, `assets` wins** |
| What changed since you placed K46–K80 | `assetsMeta.changedSincePlaced` (per asset; "VISIBLE:" marks a change you can see) |
| Your 10-04 sheet records | `sheets` (36 records, `order` 1–35 = K46–K80, `order` 36 = the existing K45) and `kits`, unchanged |
| New 1:1 sheet images | `<Kit>_{front,side,top,hero}.png` in this folder: 228 PNGs, transparent, 16 px clear padding, front/side/top at `suggested_sheet_scale` px/m (place at 100 %), hero 1152 × 864 |
| In-place swaps for placed sheets | `png_fix/<Kit>_{top,front,side,persp}.png`: 18 kits, 72 PNGs, **same tool, scale and framing as the `png/` images on your sheets**, so they drop into the existing `img:` slots |
| Group heroes | `heroes/doors_lineup_pull_face.png`, `heroes/lock_exploded.png`, `heroes/keys_board_hung.png`, `heroes/windows_lineup_face_a.png` (+ `_alpha` versions); notes in `heroes/hero_notes.json` |
| The models | `Assets/Resources/Props/Models/Kit_*.fbx` + `.json` in Red's project. Main has held the fix-pass finals since 2026-10-14 (`interactables_kit.sh`, MANIFEST OK) |
| Re-render tool | `FrontRoomsThreeView.handoff.cs.txt` (our copy, with the yaw argument), command in `README.md` §3 |

## 2. Counts

| Item | Count |
|---|---|
| Assets in `assets` | **57** (excluding `Kit_Keyboard`, the old computer prop; the exit signs and stud doorways are listed in §4) |
| Hand-off PNGs | 228 = 57 × front, side, top, hero |
| Suggested sheet scale | 39 assets at 2000 px/m, 16 at 250, 2 at 1000 (key board, key cabinet) |
| Sheets you placed on 10-04 | 35 (K46–K80) |
| Sheets whose images changed | **13** (§3) |
| `png_fix` kits | 18; 4 of them render pixel-identical to your `png/` images (K74, K76, K79, K80: no action) |
| Hero PNGs | 74 = 37 renders × 2 (`<name>.png` on the K-sheet panel #EEECE6 + `<name>_alpha.png`). The 4 group heroes are 4 of the 37, not extra. Corrected 2026-10-14: this row said "66 + 4 group heroes", but the 66 files of that day (33 renders) already included them. The 37 include the 4 gap-close renders of 2026-10-14 (over-edge desk key, its wide view, two 0.3 m window close-ups). Scenes that fill the frame (the 4 hung keys, the desk key, the window close-ups) have an opaque `_alpha` copy |
| Proposal crops | 29 in `heroes/slots/` + 4 `heroes/slots/DW14_*` (ours, for `2748:6099`) |

## 3. K numbering: keep K46–K80, refresh in place

**The 10-03 plan is obsolete.** It said K46–K47 in PROP KIT and K48+ in a "PROP KIT · THREE-VIEW + ERA · CONT." section at x 16577, y ~25500. You placed K46–K80 inside `2324:852` on 2026-10-04 and merged the CONT section on 2026-10-07 (`2648:6093` no longer exists). `K81 · Evacuation plan` (`2948:6863`) is the placard track's sheet.

So this kit needs **no new K numbers.** Refresh the existing sheets by node id. Sheet K(45 + n) is `sheets[]` record `order` n.

| K | Node | Sheet | Assets | Action |
|---|---|---|---|---|
| K46 | `2648:6094` | Wood door frame | Kit_DoorFrame_Wood | **Swap images** (`png_fix`): oak saddle grain now runs along the saddle (small) |
| K47 | `2648:6139` | Steel door frame | Kit_DoorFrame_Steel, _Steel_Alu | **Swap images**: the saddle is dark bronze like the frame. Aluminium stays only on the `_Alu` (Exit) variant, so drop it from the main swatch row |
| K48 | `2653:9007` | Veneer door | Kit_DoorLeaf_Veneer, _Veneer_Oak | **Swap the Oak variant**: grain now vertical (was horizontal). The Veneer stripes are the `DoorVeneer_A` texture, due to be redrawn |
| K49 | `2653:9048` | Steel door | Kit_DoorLeaf_Steel, _Steel_PaintedMetal | none |
| K50 | `2653:9091` | Steel door with lite | Kit_DoorLeaf_SteelLite | none |
| K51 | `2653:9133` | Ward door | Kit_DoorLeaf_Ward | **Swap images**: laminate grain now vertical |
| K52 | `2653:9173` | Surface closer | Kit_DoorCloser_Body, _Arm, _Forearm, _Shoe | **Swap the Shoe**: rounder ear (48 segments), 32-segment pivot screw |
| K53 | `2653:9218` | Exit crossbar | Kit_ExitDevice_Crossbar | **Swap images, resize the three view slots and change the dims label** (steps below): larger latch-end case with a rim-latch head; W × H × D now 909 × 180 × 95 mm (was 877 × 120 × 95) |
| K54 | `2653:9256` | Door sign | Kit_DoorSign | none (face still blank, §5) |
| K55 | `2653:9298` | Zone number plate | Kit_DoorNumberPlate, _Blue, _White | none |
| K56 | `2653:9343` | Exit sign | Kit_ExitSign, _Dead | not ours any more: the exit-sign track's `research/exit_sign/threeview/index.json` |
| K57 | `2653:9384` | Escutcheon | Kit_Lock_Escutcheon, _Brass | **Swap images**: 1.5 mm edge round. `png_fix` has the chrome part; for the Brass variant thumbnail use `Kit_Lock_Escutcheon_Brass_hero.png` here (1152 × 864, the same size as the sheet's persp) |
| K58 | `2653:9425` | IC cylinder | Kit_Lock_CylinderShell, _Brass | none |
| K59 | `2653:9468` | Plug | Kit_Lock_Plug | none |
| K60 | `2653:9506` | Knurled knob | Kit_Lock_Knob, _Brass | **Swap images**: crowned face (sphere R 0.070), still 65 mm proud. Brass thumbnail: `Kit_Lock_Knob_Brass_hero.png` |
| K61–K67 | `2653:9545` … `2653:9776` | Deadbolt, mortise latchbolt, mortise strike, rose, lever, bored latchbolt, bored strike | Kit_Lock_* | none (LOD1 added only) |
| K68 | `2653:9817` | Zone key | Kit_Key_Zone, _Nickel | **Swap images**: the tip bevel starts later, so all six cuts show. Nickel thumbnail: `Kit_Key_Zone_Nickel_hero.png` |
| K69–K72 | `2653:9856` … `2653:9978` | Split ring, three key tags | Kit_KeyRing, Kit_KeyTag_* | none |
| K73 | `2653:10021` | Key board | Kit_KeyHookBoard | **Swap images**: board grain now vertical |
| K74 | `2653:10061` | Key cabinet | Kit_KeyCabinet | none (`png_fix` is pixel-identical) |
| K75 | `2653:10103` | Key hook | Kit_KeyHook | **Swap images**: 48-segment flange, 16-sided wire |
| K76 | `2653:10139` | Wood window | Kit_WindowFrame_Wood | none (pixel-identical) |
| K77 | `2653:10177` | Steel window | Kit_WindowFrame_Steel, _Steel_Enamel | **Swap images** (both): rebuilt from main's 10-04 module, small profile changes |
| K78 | `2653:10218` | Aluminium window | Kit_WindowFrame_Alu | **Swap images**: rebuilt, 2,190 tris with a LOD1 |
| K79, K80 | `2653:10256`, `2653:10294` | Mini-blinds | Kit_MiniBlind_Raised, _Lowered | none (pixel-identical) |

Pixel differences between `png/` and `png_fix/` were measured per view on 2026-10-14; the four "pixel-identical" kits have a maximum difference of 0 in all four views. The node ids above were read from `2324:852` the same day (read-only).

### K53: resize the slots before the swap (added 2026-10-14)

K53 is the only refreshed sheet whose `png_fix` images changed size, because the crossbar grew. Its three view slots are FIT and sized to the old images, so an in-place upload alone would shrink the new views inside the old boxes. The rule in `Tools/three_view/README.md` is that PNGs are 2× the sheet scale (place at 50 %), and "if the asset's size changed, also resize its slots". So each slot is half the new PNG:

| View | Slot node | Old PNG → new PNG (px) | Slot now (w × h) | Slot new (w × h) |
|---|---|---|---|---|
| top | `2653:9226` `img:Kit_ExitDevice_Crossbar_top` | 471 × 80 → 487 × 80 | 235.5 × 40 | **243.5 × 40** |
| front | `2653:9227` `img:Kit_ExitDevice_Crossbar_front` | 471 × 92 → 487 × 122 | 235.5 × 46 | **243.5 × 61** |
| side | `2653:9228` `img:Kit_ExitDevice_Crossbar_side` | 80 × 92 → 80 × 122 | 40 × 46 | **40 × 61** |
| persp | `2653:9239` | 1152 × 864, unchanged | 576 × 432 | unchanged |

One way to keep the sheet's own layout (read from `2653:9218` on 2026-10-14, read-only). The ground line `2653:9229` at y 930 is the PNGs' bottom edge less the 8 px padding, so it stays where it is:
1. front: x 64, y 892 → **877**, 243.5 × 61 (it grows upward; its bottom stays at y 938);
2. side: x 323.5 → **331.5** (keeps the 24 px gutter), y 892 → **877**, 40 × 61; move its label "Side" `2653:9231` from x 331.5 to **339.5** (y 938 stays). Every K-sheet puts the "Side" and "Top" labels at the side slot's x + 8 (K52, K54, K55 and K75 read 2026-10-14), so they follow the side slot;
3. top: x 64, y 828 → **813**, 243.5 × 40 (keeps the 24 px gap above the front); move its label "Top" `2653:9232` from (331.5, 835) to **(339.5, 820)**;
4. ground line `2653:9229`: width 331.5 → **339.5**, so it still runs 16 px past the side view;
5. upload `png_fix/Kit_ExitDevice_Crossbar_{top,front,side,persp}.png` into the four slots (FIT), as for the other sheets;
6. dims label `2653:9233`: "W 877 · D 95 · H 120 mm" → **"W 909 · D 95 · H 180 mm"**.

The label "Front" (x 72, y 938), the scale bar and the hero keep their places. (Corrected 2026-10-14 18:0x: this note kept "Side" in place and moved "Top" only in y; both labels move to x 339.5 with the side slot.)

**If you add sheets** (for example a family line-up from the group heroes): the next free numbers are **K82 and K83**, the two empty cells of the last row of `2324:852`, at (4200, 24960) and (6240, 24960). **K84 and later need a decision first.** The section ends at y 28114 and PROP KIT · REAL vs MODEL `2349:852` starts at y 28576, so a new 1240 px row would overlap it.

## 4. What is not in the 57

- **Exit signs** (`Kit_ExitSign*`): the exit-sign track's kit, with its own index (`research/exit_sign/threeview/index.json`). Red's letter colour (call 10) is still red by default.
- **Stud doorways** (`Kit_DoorwayStuds*`): your existing K45, unchanged.
- **Hinges:** no separate asset. The knuckles are merged into the frame and leaf meshes, on the A2 axis.
- **Glass and its fracture set:** the glass tracks' assets. The window frames only carry the anchors.
- **`Kit_Keyboard`:** the old computer keyboard prop (K10), not part of this kit.

## 5. Unusual things to know before you lay out

1. **Doors, leaves and keys are turned −90° for their front view** (the face, their +X, toward the camera). Main's `Tools/three_view` has no yaw argument, so it renders them edge-on; re-render with `FrontRoomsThreeView.handoff.cs.txt` (README §3). `dims_m` is given in the front image's frame.
2. **Three surfaces do not exist yet:** `Door_Enamel`, `Prop_KeyTagNo` and `Prop_SignEngraved`. So:
   - the locked leaves (`Kit_DoorLeaf_Steel`, flat almond) and `Kit_WindowFrame_Steel_Enamel` render in the FBX's own preview colour, with no texture;
   - the tag inserts, number plates and door-sign faces are blank.
   The strings are in `10_spec.md`: EMPLOYEES ONLY on both faces, STAFF ONLY in Run, and the zone numbers. The lettering is yours once the surfaces are approved. Please do not letter the renders by hand as if the in-game text existed.
3. **Chrome reads bright in the studio light but near black in the game** (critic H1, satin-chrome tune open). The sheets show the studio look.
4. **Name still open:** `Kit_KeyHookBoard` is the file name; "key board" vs "key rack" is Red's call (codex audit F6). K73 says "Key board", and our DW07 numeral says "Key rack".
5. **Run window:** since the fix pass the Run member (W-RN) is the bronze `Kit_WindowFrame_Steel`, matching the Run doors (critic M2). `_Steel_Enamel` (white) is now an alternative with no map member. Worth a note on K77.
6. **iCloud copies:** files named `* 2.png` in `heroes/` (for example `doors_lineup_pull_face 2.png`) are iCloud conflict copies. Ignore them and do not delete them; the iCloud chat owns them.
7. **The key UI (Red's decision, 2026-10-14):** the HUD key is direction B, the key on its ring and tag, on phone and desktop. A locked door shows a padlock in the tag's language: your component set `3113:6999` "Lock glyph · B · padlock", Rect/Round/Long × red/blue/white × closed/open. Directions A, A + chip and C are history only. The kit side that matches it: `Kit_KeyTag_{Rect,Round,Long}` in red (no suffix), `_Blue` and `_White`, and `Kit_DoorNumberPlate*` in the same zone colour and number (`assetsMeta.keyUi`).
8. **Not in the game yet.** The models are in the project, but the map still builds its cube door, cube key and slab pane. The door and key code is the visual chat's, with the map chat.

## 6. What we did in Figma on our side (for reference)

In `2748:6099` we filled 25 reserved slots with these renders, put the final kit names on the chips, and added slide **DW14 · The kit, as built** (`3122:7042`) with the door and window line-ups, the exploded lock and the hung key. Later on 2026-10-14 we filled the last three WIP slots with new renders: the over-edge desk key on DW07 and the two 0.3 m window close-ups on DW09 (`heroes/keys_desk_overedge.png`, `heroes/window_W-OF_stop_close.png`, `heroes/window_W-L0_stool_close.png`). Details: `../proposal/02_figma.md` §P and §P.8.
