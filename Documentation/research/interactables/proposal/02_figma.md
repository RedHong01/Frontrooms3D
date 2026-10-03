# 02 — The Figma section: doors + windows proposal, phase 1

Date: 2026-10-03, about 12:55. Owner: visual chat, interactables proposal workflow.

This file records what was built in Figma from `01_content_plan.md`. Nothing landed in the game. Red confirms or changes the proposal; phase 2 then fills the reserved slots.

## 1. Where it is

| Item | Value |
|---|---|
| File | `0tCbAiVUlrPId3RWd9LRif` ("Undergoing Game Projects"), page `2099:76` |
| Section | **`2497:3804`** "FRONTROOMS · DOORS + WINDOWS · PROPOSAL" |
| Link | https://www.figma.com/design/0tCbAiVUlrPId3RWd9LRif?node-id=2497-3804 |
| Position | x 51297, y 2000, 8280 × 3880 (ends x 59577, y 5880) |
| Frames | 12 × 1920 × 1080, 3 rows of 4, at x 120 / 2160 / 4200 / 6240 and y 160 / 1400 / 2640 inside the section (as HUNTER 2331:852) |

**Overlap check.** Every section on the page (nested ones too) and every top-level node were listed by `absoluteBoundingBox` right before placing, and the script refused to place on any overlap. The spot was free, so the section sits exactly at the task's x 51297, y 2000.
- The nearest existing section is "Week 3 · DEVELOPER DIARY #3" (2440:3804), which ends at x 37657.
- The planned LEVEL TRANSITIONS (x 33937) and ROOM VISUALS (x 42617) columns were not on the page yet. ROOM VISUALS would end at x 50897, which leaves a 400 px gap.
- A re-check after the build found 0 overlaps and no stray page-level nodes.

**Note on the plan.** `01` §1.1 named a candidate spot at x 7897, y 19137. The task gave x 51297, y 2000, which overrides it. That spot is now taken by "GLOW INK · NARRATIVE RESEARCH" (2471:3804) anyway.

## 2. Frames

| Slide | Frame id | Frame name | Title (Bayon 88) |
|---|---|---|---|
| 01 / 12 | `2497:3805` | DW00 · Three boxes | Three boxes |
| 02 / 12 | `2497:3820` | DW01 · What RE8 does | What RE8 does |
| 03 / 12 | `2497:3829` | DW02 · Stop behind every gap | A stop behind every gap |
| 04 / 12 | `2497:3838` | DW03 · The store's own doors | The store's own doors |
| 05 / 12 | `2497:3847` | DW04 · Locked from 20 m | Locked, from 20 m |
| 06 / 12 | `2497:3856` | DW05 · One frame eight doors | One frame, eight doors |
| 07 / 12 | `2497:3865` | DW06 · A lock that takes a key | A lock that takes a key |
| 08 / 12 | `2497:3874` | DW07 · A key on a hook | A key on a hook |
| 09 / 12 | `2497:3883` | DW08 · A window, not a pane | A window, not a pane |
| 10 / 12 | `2497:3892` | DW09 · The glass sits in a pocket | The glass sits in a pocket |
| 11 / 12 | `2497:3901` | DW10 · What changes | What changes in the game |
| 12 / 12 | `2497:3910` | DW11 · Your calls | Your calls |

Ledes, statements, chips and rule texts are exactly the strings in `01` §3.

## 3. Grammar used (read from HUNTER 2331:852 before writing)

- **Text styles:** only the file's P1 styles, applied by id, so Red's hand retunes flow through.
  - `P1/Display/Title`: Bayon 88/80, −1 %. Used for titles, rule heads and numerals.
  - `P1/Display/Label`: Bayon 20/20, +3 %, upper. Used for chips, diagram labels and placeholder labels.
  - `P1/Text/Body`: Source Serif 4 24/26. Used for ledes and bodies.
  - `P1/Text/Statement`: Source Serif 4 50/42.
  - `P1/Header/Meta`: IBM Plex Mono 13/16. Used for the running header only.
- **Fonts in the section:** Bayon, Source Serif 4 and IBM Plex Mono. No Inter (checked by reading back every text run).
- **Running header** at y 28, x 72 / 522 / 972 / 1422 / 1722: "Individual Game Project" · "FrontRooms · Doors + windows · proposal" · "Week 3 · Oct 3, 2026" · "Red Wang" · "NN / 12".
- **Titles** are in sentence case, as in the Hunter section. Bayon draws them in capitals.
- **Placement:** title x 72, y 139; lede x 1272, y 173, 576 wide (all 12 ledes are 2 lines); statement x 72, y 969, 1776 wide (all one line); content from y 232; the 12-col / 72 / 24 grid.
- **Chips:** horizontal auto-layout, padding 10 / 7 / 10 / 5, h 32, at slot +16 / +16, radius 0. Black (#0A0A0A) with white text, or the slide's hero chip in yellow (#F4DF3B) with black text.
- **Media slots:** rectangles, radius 0, named by slot. Images are fills with scale mode FILL. Every crop matches its slot's aspect within 0.3 %, so FILL shows the whole crop.
- **Placeholders:** the slot rectangle filled with K16's hero panel colour (#EEECE6, read from 2326:1004). A centred Bayon 20 label in mid grey sits on top, named `placeholder · <slot>`, with the slot name without its prefix.
  - The suffix is "· PHASE 2" for `img:` slots and "· AWAITING OK · C#" for `ref:` slots.
  - Long names wrap at underscores.
  - The slot's chip stays.
- **Diagrams:** frames filled #F3F1EA (as HR02's diagram), with geometry as one imported SVG group (`geometry · <scale>`). Lines are black 2 px, labels Bayon 20, and there is one yellow accent per diagram (the light or sight path, the commit line, or the tooth band).
- **No** captions under images, no meta lines on cards, no source footers. Sources stay in the ledgers (§7).

## 4. Diagrams

| Slot | Node | Slide | Size | Scale used | Change from `01` §4 |
|---|---|---|---|---|---|
| `diagram:dw02_jamb_section` (D1) | `2499:3854` | DW02 | 576 × 657 | **4.5 px/mm** | `01` asked for 2.5 px/mm. At that scale the 3 mm gap was 7.5 px and the L path did not read on a projected slide, so the view is zoomed to X ±64 mm. Both casings and today's map trims fall outside the crop; the wall end, lining, stop, leaf, knuckle and both yellow paths are drawn exactly. Added labels: "Wall end", "50 mm leaf" |
| `diagram:dw06_unlock_timeline` (D2) | `2501:3897` | DW06 | 576 × 444 | **300 px/s** (0–1.55 s) | `01` said about 330 px/s; 300 leaves room for the row labels. Beat labels are shortened so none overlap: "Reach" for approach, "90°" on the plug row, "-14°" with a hyphen. KNOB + LATCH bars are dashed and marked "New" (proposed constants) |
| `diagram:dw09_elevation` (D3) | `2505:3804` | DW09 | 576 × 657 | **280 px/m**, floor at y 640 | `01` asked for 330 px/m, but that cannot fit the raised blind's top (2.195 m) with the floor in view. Added label: "30 screws on the office face" (the 30 dots) |
| `diagram:dw09_jamb_section` (D4) | `2505:3865` | DW09 | 576 × 657 | 2.4 px/mm; stool inset 1.3 px/mm | As planned. The inset is a vertical section through the Lobby stool (stool, half-round nose, aprons, wood stops, glass on its setting block) |
| `diagram:dw10_door_plan` (D5) | `2505:3906` | DW10 | 876 × 657 | 200 px/m; hinge inset 10:1 (2 px/mm) | As planned. The keep-clear strip is drawn on the swing side only (`FrontRoomsModuleUnits.DoorClearDepth` 1.2). The inset collider box is drawn 50 mm deep |

## 5. Slot → file map (filled in phase 1)

Path keys: `PM` = `interactables/proposal/media/`, `IMG` = `interactables/images/`, `AUD` = `interaction_audit/images/`, `W2` = `Research/week02/` (course folder), `WIP` = `<scratchpad>/interact_previews/`. Crops are source pixels (x0, y0, x1, y1).

**Uploads.** Every file went in through the Figma MCP `upload_assets` tool with `nodeIds` = the slot, as raw PNG bytes. Raw bytes keep the slot's layer name; multipart uploads rename the layer. The cut crops sit in `<scratchpad>/dwfig/crops/<slot stem>.png`, and the crop list is in `<scratchpad>/dwfig/crops.tsv`.

### 5.1 Evidence (33 slots)

| Slide | Slot | Node | Size | Source | Crop |
|---|---|---|---|---|---|
| DW00 | `media:dw00_slit` | `2499:3804` | 876 × 657 | `PM/ingame_door_gap_16_sideB_hinge_inline_darknear.png` | (562, 0, 1362, 600) |
| DW00 | `media:dw00_swing_a` | `2499:3813` | 201 × 316 | `PM/ingame_door_gap_18_open_fromA_t0.13s.png` | (662, 0, 1349, 1080) |
| DW00 | `media:dw00_swing_b` | `2499:3816` | 201 × 316 | `PM/ingame_door_gap_21_open_fromB_t0.13s.png` | (617, 0, 1304, 1080) |
| DW00 | `media:dw00_locked` | `2499:3819` | 426 × 316 | `AUD/48_door_locked_before_hud.png` | (600, 273, 1320, 807) |
| DW00 | `media:dw00_key` | `2499:3822` | 426 × 317 | `AUD/52_key_2m.png` | (640, 300, 1280, 776) |
| DW00 | `media:dw00_window` | `2499:3825` | 426 × 317 | `AUD/04_window_L0_intact_oblique.png` | (700, 80, 1660, 794) |
| DW02 | `media:dw02_today` | `2499:3848` | 276 × 621 | `PM/ingame_door_gap2_12_today_sideB_hinge_inline.png` | (803, 0, 1123, 720) |
| DW02 | `media:dw02_option_a` | `2499:3851` | 276 × 621 | `PM/ingame_door_gap2_74_optionA_sideB_hinge_inline.png` | (803, 0, 1123, 720) |
| DW02 | `media:dw02_leak` | `2499:3855` | 576 × 316 | `PM/ingame_door_gap3_00_floor_leak.png` | (480, 380, 1440, 907) |
| DW02 | `media:dw02_proxy` | `2499:3858` | 576 × 317 | `PM/ingame_door_gap3_01_floor_shadowproxy.png` | (480, 380, 1440, 907) |
| DW03 | `media:dw03_a24_door` | `2500:4890` | 876 × 657 | `PM/a24_trailer_0127_wood_door_knob.jpg` | (60, 21, 1441, 1057) |
| DW03 | `media:dw03_a24_dark_door` | `2500:4893` | 426 × 316 | `PM/a24_trailer_0139_dark_door_level0.jpg` | (100, 200, 1200, 1016) |
| DW03 | `media:dw03_kane_oak_door` | `2500:4896` | 426 × 316 | `PM/kane_emg_0053_oak_door_lever.jpg` | (330, 310, 970, 786) |
| DW03 | `media:dw03_kane_bolts` | `2500:4899` | 426 × 317 | `PM/kane_emg_0125_door_six_bolts.jpg` | (330, 300, 970, 776) |
| DW03 | `media:dw03_blue_tape` | `2500:4902` | 426 × 317 | `W2/kit-references/a24/a24_trailer_0115_blue_painters_tape.jpg` | (1100, 0, 1920, 610) |
| DW04 | `media:dw04_l0lit_6m` | `2500:4905` | 426 × 200 | `IMG/05_r3_sheet_recommended.jpg` | (220, 60, 860, 360) |
| DW04 | `media:dw04_l0lit_12m` | `2500:4908` | 426 × 200 | `IMG/05_r3_sheet_recommended.jpg` | (870, 60, 1510, 360) |
| DW04 | `media:dw04_l0lit_20m` | `2500:4911` | 426 × 200 | `IMG/05_r3_sheet_recommended.jpg` | (1520, 60, 2160, 360) |
| DW04 | `media:dw04_l0dim_6m` | `2500:4914` | 426 × 200 | `IMG/05_r3_sheet_recommended.jpg` | (220, 740, 860, 1040) |
| DW04 | `media:dw04_l0dim_12m` | `2500:4917` | 426 × 200 | `IMG/05_r3_sheet_recommended.jpg` | (870, 740, 1510, 1040) |
| DW04 | `media:dw04_l0dim_20m` | `2500:4918` | 426 × 200 | `IMG/05_r3_sheet_recommended.jpg` | (1520, 740, 2160, 1040) |
| DW04 | `media:dw04_office_6m` | `2500:4919` | 426 × 200 | `IMG/05_r3_sheet_recommended.jpg` | (220, 1420, 860, 1720) |
| DW04 | `media:dw04_office_12m` | `2500:4922` | 426 × 200 | `IMG/05_r3_sheet_recommended.jpg` | (870, 1420, 1510, 1720) |
| DW04 | `media:dw04_office_20m` | `2500:4923` | 426 × 200 | `IMG/05_r3_sheet_recommended.jpg` | (1520, 1420, 2160, 1720) |
| DW04 | `media:dw04_no_p1` | `2500:4924` | 426 × 200 | `IMG/05_r1_sheet_L0lit.jpg` | (870, 60, 1510, 360) |
| DW04 | `media:dw04_no_p3` | `2500:4927` | 426 × 200 | `IMG/05_r1_sheet_L0dim.jpg` | (870, 990, 1510, 1290) |
| DW04 | `media:dw04_no_p2p` | `2500:4930` | 426 × 200 | `IMG/05_r2_sheet_Office.jpg` | (1520, 990, 2160, 1290) |
| DW06 | `media:dw06_dip_start` | `2501:3850` | 336 × 189 | `IMG/05_r3_headdip_strip_L0lit.jpg` | (0, 10, 640, 370) |
| DW06 | `media:dw06_dip_mid` | `2501:3853` | 336 × 189 | `IMG/05_r3_headdip_strip_L0lit.jpg` | (650, 10, 1290, 370) |
| DW06 | `media:dw06_dip_pose_p` | `2501:3856` | 336 × 189 | `IMG/05_r3_headdip_strip_L0lit.jpg` | (1300, 10, 1940, 370) |
| DW06 | `media:dw06_dip_key_in` | `2501:3859` | 336 × 189 | `IMG/05_r3_headdip_strip_L0lit.jpg` | (1950, 10, 2590, 370) |
| DW06 | `media:dw06_dip_turned` | `2501:3862` | 336 × 189 | `IMG/05_r3_headdip_strip_L0lit.jpg` | (2600, 10, 3240, 370) |
| DW08 | `media:dw08_kane_window` | `2503:4515` | 426 × 317 | `PM/kane_emg_0308_framed_window_1990.jpg` | (0, 0, 1310, 975) |

### 5.2 Work-in-progress model previews (10 `img:` slots)

These are the interactables workflow's Blender self-check renders, as they existed at 12:2x–12:4x. Each chip ends in "· WIP". Phase 2 replaces every one of them.

| Slide | Slot | Node | Size | Source | Crop |
|---|---|---|---|---|---|
| DW05 | `img:DoorSet_L0-F_persp` | `2501:3804` | 426 × 240 | `WIP/G1/G1_wood_open95.png` | full frame, padded to 16:9 with #EEECE6 side bands |
| DW05 | `img:DoorSet_L0-K_persp` | `2501:3830` | 426 × 240 | `WIP/G1/G1_steel_s15.png` | full frame, padded to 16:9 with #EEECE6 side bands |
| DW05 | `img:DoorSet_OF-K_persp` | `2501:3835` | 426 × 240 | `WIP/G1/G1_closer_Kit_DoorLeaf_Steel_00.png` | (0, 200, 1200, 875) |
| DW07 | `img:KeySet_Cabinet_persp` | `2503:4477` | 426 × 316 | `WIP/G3/G3_cabinet_34.png` | (0, 18, 1300, 982) |
| DW07 | `img:KeySet_Hook_persp` | `2503:4480` | 426 × 316 | `WIP/G3/G3_keyhook_close.png` | (0, 116, 900, 784) |
| DW08 | `img:Kit_WindowFrame_Wood_persp` | `2503:4502` | 426 × 316 | `WIP/G4/Kit_WindowFrame_Wood_b.png` | (60, 70, 810, 626) |
| DW08 | `img:Kit_WindowFrame_Steel_persp` | `2503:4505` | 426 × 316 | `WIP/G4/Kit_WindowFrame_Steel_b.png` | (60, 70, 810, 626) |
| DW08 | `img:Kit_WindowFrame_Alu_persp` | `2503:4512` | 426 × 316 | `WIP/G4/Kit_WindowFrame_Alu_b.png` | (60, 70, 810, 626) |
| DW09 | `img:Kit_WindowFrame_Steel_close` | `2505:3900` | 576 × 316 | `WIP/G4/Kit_WindowFrame_Steel_corner_0p3m.png` | (0, 120, 1200, 778) |
| DW09 | `img:Kit_WindowFrame_Wood_close` | `2505:3903` | 576 × 317 | `WIP/G4/Kit_WindowFrame_Wood_stoolA_0p3m.png` | (0, 150, 1200, 810) |

**Read these previews with care:**
- **The colours are Blender stand-ins, not the Unity surfaces.** The steel frames render lighter than `Prop_SteelBrown` (luminance 0.03), and the wood stool close-up renders tan.
- **The door previews carry no lockset, sign or plate yet.** Those parts are still being built in G2 and G3.
- **The three DW08 frames use the `_b` renders** (one studio, one camera), so wood, bronze steel and aluminium read as one family. The `_a` renders made wood and steel look the same.
- **The previews live in the session scratchpad,** which is temporary. Phase 2 re-renders to the §6 rules of `01` anyway.

**Previews I did not use, and why:**
- `G3_board_34.png` for `img:KeySet_Board_persp`. It was placed, then taken out: the stand-in renders the board pink, while the spec is a near-black painted board (`Prop_WoodDark`, luminance 0.014). A wrong value would mislead on the slide that argues "you spot the board from the doorway".
- The G2 lock parts (`Kit_Lock_*_a/_b.png`). Each part is only 40–90 px across in a 900 × 700 frame. Filling a 276 × 204 slot would need about a 4× upscale, which is too soft for a deck built to the highest spec.
- `G3_tags_front.png` for `img:KeyParts_Lineup_front`. The tags are cut off at the frame edge, and the key and the number plate are missing.
- `Kit_MiniBlind_Raised_a.png` is nearly empty. `Kit_MiniBlind_Raised_blind_1p5m.png` shows white light-card artefacts behind the glass.

## 6. Reserved slots for phase 2

### 6.1 Placeholders (18 `img:` slots, "· PHASE 2")

To fill one, upload the render into the slot node. Then delete the placeholder text node, and keep the chip.

| Slide | Slot | Slot node | Placeholder text | Chip | Size | Render (`01` §6) | Priority |
|---|---|---|---|---|---|---|---|
| DW05 | `img:DoorSet_OF-F_persp` | `2501:3815` | `2501:3816` | `2501:3817` | 426 × 240 | `10` §2.1 OF-F row | P1 |
| DW05 | `img:DoorSet_RN-F_persp` | `2501:3820` | `2501:3821` | `2501:3822` | 426 × 240 | RN-F row | P2 |
| DW05 | `img:DoorSet_EX-F_persp` | `2501:3825` | `2501:3826` | `2501:3827` | 426 × 240 | EX-F row | P2 |
| DW05 | `img:DoorSet_RN-K_persp` | `2501:3840` | `2501:3841` | `2501:3842` | 426 × 240 | RN-K row | P2 |
| DW05 | `img:DoorSet_EX-K_persp` | `2501:3845` | `2501:3846` | `2501:3847` | 426 × 240 | EX-K row | P2 |
| DW06 | `img:Kit_Lock_Escutcheon_persp` | `2501:3865` | `2501:3866` | `2501:3867` | 276 × 204 | the asset alone | P1 |
| DW06 | `img:Kit_Lock_CylinderShell_persp` | `2501:3869` | `2501:3870` | `2501:3871` | 276 × 204 | IC face to camera | P1 |
| DW06 | `img:Kit_Lock_Plug_persp` | `2501:3873` | `2501:3874` | `2501:3875` | 276 × 204 | keyway cavity visible | P1 |
| DW06 | `img:Kit_Lock_Knob_persp` | `2501:3877` | `2501:3878` | `2501:3879` | 276 × 204 | knurl visible | P1 |
| DW06 | `img:Kit_Lock_Deadbolt_persp` | `2501:3881` | `2501:3882` | `2501:3883` | 276 × 204 | thrown | P1 |
| DW06 | `img:Kit_Lock_Latchbolt_Mortise_persp` | `2501:3885` | `2501:3886` | `2501:3887` | 276 × 204 | the asset | P1 |
| DW06 | `img:Kit_Lock_StrikeMortise_persp` | `2501:3889` | `2501:3890` | `2501:3891` | 276 × 204 | the asset | P1 |
| DW06 | `img:LockSet_Mortise_persp` | `2501:3893` | `2501:3894` | `2501:3895` (yellow) | 276 × 204 | set on a steel leaf patch, key in, from pose P | P1 |
| DW07 | `img:KeySet_Board_persp` | `2503:4468` | `2507:3804` | `2503:4469` (yellow) | 426 × 316 | board + ring + key + rect tag on `hook_6` | P1 |
| DW07 | `img:KeySet_Desk_persp` | `2503:4483` | `2503:4484` | `2503:4485` | 426 × 316 | key flat on a desk, tag over the edge | P1 |
| DW07 | `img:KeyParts_Lineup_front` | `2503:4491` | `2503:4492` | `2503:4493` | 426 × 317 | key, ring, 3 tags × 3 colours, number plate; one number on all | P1 |
| DW08 | `img:Kit_WindowFrame_Steel_Enamel_persp` | `2503:4508` | `2503:4509` | `2503:4510` | 426 × 316 | the variant | P2 |
| DW08 | `img:Kit_MiniBlind_Raised_persp` | `2503:4526` | `2503:4527` | `2503:4528` | 426 × 317 | the blind over a steel frame | P1 (option) |

That is 18 slots: 17 that were never filled, plus `KeySet_Board_persp`, which went back to a placeholder (§5.2).

### 6.2 WIP slots that phase 2 must replace (10)

These are all ten rows of §5.2. When the final render goes in, also take " · WIP" off the chip: the chip node is the frame at the slot's +16 / +16.

| Slot | Chip node |
|---|---|
| `img:DoorSet_L0-F_persp` | `2501:3812` |
| `img:DoorSet_L0-K_persp` | `2501:3832` |
| `img:DoorSet_OF-K_persp` | `2501:3837` |
| `img:KeySet_Cabinet_persp` | `2503:4478` |
| `img:KeySet_Hook_persp` | `2503:4481` |
| `img:Kit_WindowFrame_Wood_persp` | `2503:4503` |
| `img:Kit_WindowFrame_Steel_persp` | `2503:4506` |
| `img:Kit_WindowFrame_Alu_persp` | `2503:4513` |
| `img:Kit_WindowFrame_Steel_close` | `2505:3901` |
| `img:Kit_WindowFrame_Wood_close` | `2505:3904` |

### 6.3 References waiting for Red's download OK (5 `ref:` slots)

| Slide | Slot | Slot node | Placeholder text | Candidate (`media_candidates.md`) |
|---|---|---|---|---|
| DW01 | `ref:re8_steam_castle_hall` | `2499:3828` | `2499:3829` | C1 (crop to 1.82 when it arrives: (0, 13, 1920, 1067)) |
| DW01 | `ref:re8_walkthrough_1-36-39` | `2499:3832` | `2499:3833` | C2 (same crop) |
| DW07 | `ref:henryford_tivoli_motel_key` | `2503:4487` | `2503:4488` | C8 |
| DW08 | `ref:sears1993_levolor_miniblinds` | `2503:4518` | `2503:4519` | C10 |
| DW08 | `ref:us4463535_glass_stop` | `2503:4522` | `2503:4523` | C11 |

Nothing was downloaded for this section.

### 6.4 Evidence that phase 2 may swap (keep the slot, change the image)

- `media:dw02_option_a` (`2499:3851`) → a T1 frame of the real A2 door, same camera.
- `media:dw04_*` (12 tiles, `2500:4905`–`2500:4930`) → T6 frames with the real kit, plus Run and Exit.
- `media:dw06_dip_*` (5 tiles, `2501:3850`–`2501:3862`) → T5 frames with the real lock.

### 6.5 `_front` slots

`01` §6 sends the `_front` three-view renders to 平面视觉's PROP KIT · THREE-VIEW + ERA section (2324:852), not to this one. The only `_front` slot here is `img:KeyParts_Lineup_front` (§6.1).

## 7. Sources

- In-engine frames and film/tape stills: `PM/SOURCES.md`.
- The blue-tape still: `W2/kit-references/SOURCES.md`. The film and tape videos: `W2/ip-research/SOURCES.md` (approved by Red on 2026-10-02).
- Readability tiles: `IMG/05_r*_metrics.json`, harness `../harness/05_*`.
- Audit frames: `../../interaction_audit/images/` (frames.txt there).
- Claims per slide: `01` §3 and §5.

## 8. Other changes from `01`, and why

1. **Rule rows moved down 12–20 px** to clear the lede, which ends at y 225:
   - DW01 heads at 252 / 466 / 680 (plan: 232 / 451 / 670);
   - DW10 heads at 248 / 400 / 552 (plan: 232 / 388 / 544);
   - DW11 rows at 252 / 416 / 580 / 744 (plan: 232 / 404 / 576 / 748).

   With the plan's numbers, the Bayon heads touched the title and the lede, and read as one block. Everything still ends above y 889.
2. **DW10 budget numerals** stay at y 724, with labels at y 812.
3. **Run and Exit P2 chips** stay as written. The Exit window chip reads "Exit · aluminium · P2 · WIP", because a built preview exists even though the member is P2.

## 9. Checks run

- **Structure:** 12 frames; 71 named slots. 43 slots carry images (33 evidence + 10 WIP), 18 are `img:` placeholders, 5 are `ref:` placeholders and 5 are diagrams. 588 nodes in all.
- **Overlaps:** 0 with other sections after the build. Nothing was edited outside `2497:3804`.
- **Screenshots** of every slide at 1400 px after the images went in, plus the section at 2400 px. Label collisions were fixed in D2, D3 and D5.

## 10. What Red should look at first

- DW02: the in-engine before/after and the zoomed jamb section.
- DW04: the 6/12/20 m value test.
- DW11: the eight calls. Each has a default.
