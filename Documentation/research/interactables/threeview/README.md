# Interactables three-view hand-off (for 平面视觉)

From the visual chat (游戏视觉), 2026-10-03. Red asked for the new door models, the key models and the key UI to be documented in Figma. This folder is the model half: three-views, heroes and sheet data for every new interactable kit. The key UI is a separate hand-off (`research/ui_key_icon/03_figma.md`).

**平面视觉 places the sheets** (agreed, `VISUAL_CHAT_TASKS.md` R10 and W1.6). The game-visual hand-off supplies the three-view PNGs and index; it does not edit the Prop Kit layout.

## 1. What is here

| Path | What |
|---|---|
| `index.json` | One record per K-sheet (`sheets`, 36) and per kit (`kits`, 62), in the fields of `INDEX_FORMAT.md`. Also `heroes` and `proposalSlots` (ours) |
| `png/<Kit>_<top\|front\|side\|persp>.png` | 248 transparent PNGs, 16 px clear padding, rendered at 2× the sheet scale. `png/manifest.json` = the renderer's own record (px/m, sizes, slots, tags) |
| `heroes/` | Grouped hero shots (doors, lock, keys, windows). `<name>.png` sits on the hero-panel colour #EEECE6; `<name>_alpha.png` is transparent where the scene allows |
| `heroes/slots/` | Ready-made crops for **our** proposal section 2748:6099 (rebuilt 2026-10-07; was 2497:3804; not for the K-sheets) |
| `FrontRoomsThreeView.interactables.cs.txt` | The three-view renderer with the interactables patch (`-threeViewYaw`, 4000/8000 px/m steps, tight shadow range) |
| `FrontRoomsInteractableHeroes.cs.txt` | The hero renderer |
| `tools/` | `build_index.py` (writes `index.json`), `sheets_meta.py` (titles, ledes, era notes), `swatch.py`, `postcrop.py`, `slots.py`, `contact.py` |

## 2. Source of the models

- The 62 kits are in the real project now: `Assets/Resources/Props/Models/Kit_*` (Codex copied them from our build clone in commit `8ef5b64`, 2026-10-03 19:10).
- Every FBX and sidecar in main is byte-identical to the build clone, except six sidecars (4 window frames, 2 mini-blinds). There, main says `placement: "Wall"` where the clone said `"Floor"`; geometry and slots are the same.
- Everything here was rendered from a fresh copy of main (`index.json` → `source`), so the surfaces and the wallpaper are what the game shows today.
- `Kit_Keyboard` is an old prop and is not part of this set.

## 3. The sheets

36 sheets. 35 are new; `doorway_studs` is the existing **K45 · Stud doorway** (2328:1258). Its PNGs are refreshed here, and the models have not changed since 2026-10-02.

Suggested numbering follows `sheets[].order`: K46 = `doorframe_wood` … K80 = `miniblind_lowered`. Agreed placement:
- K46 and K47 go in the two empty cells of 2324:852, at (4200, 13800) and (6240, 13800).
- K48 onward go in the new section **"PROP KIT · THREE-VIEW + ERA (CONT.)"**, at x 16577, y 29300. The earlier y≈25500 area contains existing magazine samples and is reserved.

| Family | Sheets (id → primary kit; variants or parts) |
|---|---|
| Doors | `doorframe_wood`; `doorframe_steel` (+ `_Alu`); `doorleaf_veneer` (+ `_Oak`); `doorleaf_steel` (+ `_PaintedMetal`); `doorleaf_steellite`; `doorleaf_ward`; `door_closer` (4 parts: Body, Arm, Forearm, Shoe); `exit_crossbar`; `door_sign`; `door_numberplate` (red; + Blue, White); `exit_sign` (+ `_Dead`); `doorway_studs` (K45) |
| Lock | `lock_escutcheon`, `lock_cylinder`, `lock_rose`, `lock_lever`, `lock_latch_bored`, `lock_strike_bored` (each chrome + `_Brass`); `lock_knob` (+ `_Brass`); `lock_plug`, `lock_deadbolt`, `lock_latch_mortise`, `lock_strike_mortise` |
| Keys | `key_zone` (brass + `_Nickel`); `key_ring`; `key_tag_rect`, `key_tag_round`, `key_tag_long` (red + Blue, White); `key_board` (`Kit_KeyHookBoard`); `key_cabinet`; `key_hook` |
| Windows | `window_wood`; `window_steel` (dark bronze + `_Enamel`); `window_alu`; `miniblind_raised`; `miniblind_lowered` |

## 4. Fields (INDEX_FORMAT.md → index.json)

Every `sheets[]` record and every `kits{}` record carries the agreed fields:

| Agreed field | In index.json |
|---|---|
| `kit` | `kit` (on a sheet = its primary kit) |
| `title`, `lede` | `title`, `lede`. Ledes are written to fit 576 px of Source Serif 24/26 in 2 lines; please re-measure on the canvas |
| `dims_mm {w, d, h}` | `dims_mm`, from the sidecar bounds. `dims_label` is the sheet string `W … · D … · H … mm` |
| `era {from, to, label}` | `era.from`, `era.to`, `era.label` = `Timeless` or `Period`. `era.chip` = the exact chip text your sheets use (`Current stock`, `Second-hand`, `Timeless`). `era.spanLabel` = the big span (`1955–90`). `era.note` + `era.sources` = why, from `02_period_hardware.md`, `06_period_windows.md` and `10_spec.md` |
| `materials [{name, hex}]` | `materials[].name` (= Unity slot), `.hex`, plus `.label` (swatch caption) and `.source`. P-4 slots carry `p4: true` and `targetHex` (§6) |
| `variants` | `variants` (kit names) and `variantImages` (their persp PNGs) |
| `images {top, front, side, persp}` | `images`, plus `figmaSlots` = the layer names `img:<Kit>_<view>` |

Extra fields you may use: `sheetPpm`, `scaleBar`, `tris` (LOD0/LOD1 and the spec budget), `parts`/`partsDims` (closer), `members` (which door/window set uses the kit), `placement`, `tags`, `anchors`.

## 5. Scale, views, placing the PNGs

- **Place every PNG at 50 %.** It then reads at `sheetPpm` on the sheet.
- **The ladder gains two steps.** Furniture keeps your rule: any side ≥ 0.75 m → 250 px/m. Small parts step up to 1000, 2000, then 4000 and 8000 px/m. The 4000 and 8000 steps are new: K01–K45 stop at 2000, and a 12 mm plug or a 7 mm hook would be specks at 2000.
- If you would rather cap the ladder at 2000, re-render through `Tools/three_view` (it stops at 2000), or scale the PNGs down.
- **One scale bar per sheet.** A 1 m bar cannot fit at 8000 px/m, so each sheet has its own bar in `scaleBar` (`label`, `metres`, `sheetPx`): 1 m at 250, 20 cm at 1000, 10 cm at 2000, 5 cm at 4000, 25 mm at 8000.
- **Views** are third-angle, like K45:
  - front = camera on +Z;
  - side = the kit's right;
  - top = from above, front toward the image bottom;
  - persp = 3/4 from the front-left, 22° down, 1152 × 864 (4:3, fits the 576 × 432 hero panel at 50 %).
- **viewYaw −90** on door frames, door leaves and keys. Their show face is +X (the door's pull face, the key's flats), so the renderer turns them to face the camera:
  - door fronts are the pull-face elevation, hinge at image left;
  - key fronts show the tip at image right, cuts up;
  - their dims are in that turned frame (W = opening width).
- **Ground line.** The PNG's bottom edge (less the padding) is the kit's lowest point, not the floor.
  - `aboveFloor_m > 0` means it hangs on a wall at that height (key hosts, windows, blinds).
  - Lock parts, keys, tags, signs and closer parts are drawn about their own pivot. Do not draw a floor line under them.

## 6. Materials and the four P-4 slots

- **Swatch rule** (same as K01–K45):
  - take the plain sRGB mean of the `_BaseMap`, and convert it to linear;
  - multiply by `_BaseColor`, also converted sRGB → linear;
  - convert the product back to sRGB.
- Slots already on your sheets reuse your exact hex. The rule gives the same values, which we checked on Walnut, Oak, Brass and Aluminium.
- **P-4 slots** have no Unity surface in main yet, so Unity draws the FBX's preview colour. `hex` is what renders today; `targetHex` is the spec target. Please show `targetHex` and mark the swatch as pending:

| Slot | Used by | Renders today | Target |
|---|---|---|---|
| `Door_Enamel` | steel leaves (almond enamel) | probe colour | #cdc5b0 |
| `Prop_KeyTagNo` | tag and number-plate paper inserts, board labels | blank paper | #dcd8cc, numbers "00"–"99" in Courier Prime |
| `Prop_SignEngraved` | door sign face | #796961 (preview bug, see `index.json`) | #30241e with ivory engraved letters |
| `Run_ExitSign_Dead` | the dead EXIT sign | — | #dfc6bf, emission off |

## 7. Era notes (binding, R10)

- **Cylinders:** interchangeable-core cylinders, figure-8 core face, unbranded.
- **Exits:** push bars. Our exit device is the 1950s–80s crossbar, `Second-hand`.
- **Key tags:** plastic ring tags with a paper insert, numbers typed in Courier Prime (VT323 only for screens).
- **Blinds:** 1-inch aluminium mini-blinds.
- **Never:** keypads, card readers or LED lock indicators. None of the kits has one.
- Each sheet's `era.note` gives the reasoning and the source section, so the era panel can say why.

## 8. Things to know before you lay out

- **`Kit_DoorLeaf_Veneer` looks striped** on the front view. That is the game's own `DoorVeneer_A.png`, a straight rift grain about 22 mm apart, not a render fault. `_Oak` is the smoother one.
- **`Kit_ExitSign` may change.** The exit-sign workflow is re-modelling it now (Red's request). If its sidecar date in main is later than this index, ask us for a re-render before you place K-exit_sign.
- **Inserts are blank.** Tag inserts, number-plate inserts, board labels and the door sign face carry no text yet; that waits on the P-4 atlases (§6).
- **`Kit_MiniBlind_Lowered` is 2.40 m wide** on purpose: it is decor for the interior window, not for the 1.55 m frames.
- **The heroes' window pane is the glass track's.** The window heroes use `Glass_Window` from main; it is not part of the window kit.

## 9. Heroes

- **The four group heroes** (`heroes[].groupHero`):
  - `doors_lineup_pull_face`: the 8-door family, L0-F … EX-K, each in its level's wall and floor;
  - `lock_exploded`: the mortise lock on its axes, with the key;
  - `keys_board_hung`: zone key, ring and tag hung on the key board, on Level 0 wallpaper;
  - `windows_lineup_face_a`: W-L0, W-OF (+ raised blind), W-RN, W-EX.
- **Also:** both faces of every door set (`doorset_<code>_<pull|push>`), the push-face lineup, a front elevation of the lock, the key on the hook and in the cabinet, a key-parts lineup at one scale, and one 3/4 hero per window.
- Use them on the sheets if a variant thumbnail needs context; they are not required by the K-sheet format.

## 10. Re-rendering one kit

1. Copy main to a clone. Never render in Red's open project. For example: `cp -Rc` of a clone with a warm Library, then rsync main's `Assets`, `Packages` and `ProjectSettings` into it.
2. Copy `FrontRoomsThreeView.interactables.cs.txt` to `<clone>/Assets/Editor/Rendering/FrontRoomsThreeView.cs`.
3. Run `Unity -batchmode -projectPath <clone> -executeMethod FrontRoomsThreeView.RunBatch -threeViewOut <dir> -threeViewOnly Kit_A -threeViewYaw Kit_A=-90 -quit` with graphics. Pass the yaw only for door frames, door leaves and keys.
4. `upload_assets` with `nodeIds` = the existing `img:<Kit>_<view>` rectangles. Then re-run `tools/build_index.py` with `/usr/bin/python3`.

## 11. Placement record

平面视觉 placed the complete K46–K80 set in Figma on 2026-10-04, page `2099:76`, file `0tCbAiVUlrPId3RWd9LRif`:

- K46 `2648:6094` and K47 `2648:6139` are in PROP KIT `2324:852` at local `(4200, 13800)` and `(6240, 13800)`.
- K48–K80 are inside PROP KIT `2324:852` (8280 × 26200, K00–K80), with the four-column grid from `kplan.json`. The CONT section `2648:6093` no longer exists; someone merged it into 2324:852 (平面视觉, 2026-10-07). Formatting fixes from 平面视觉 on 2026-10-07:
  - header "Week 3 · Oct 4, 2026";
  - era axis 1950/2000;
  - era labels right-aligned at x 1824;
  - K49/K52 ledes cut to 2 lines;
  - variant thumbnails raised above their white frames;
  - page totals 81.
- All 164 primary and variant image slots were uploaded from `png/` and checked against the sheet grammar; runtime/model acceptance remains with the game-visual workflow.
