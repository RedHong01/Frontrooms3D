# 02 — The Figma section: doors + windows proposal, phase 1

Date: 2026-10-03. Built about 12:55; fix pass about 17:00 (§0). Owner: visual chat, interactables proposal workflow.

This file records what is in Figma. Nothing landed in the game. Red confirms or changes the proposal; phase 2 then fills the reserved slots. `03_for_red.md` is the one-page guide for Red.

**2026-10-07: the section was deleted and rebuilt as `2748:6099` at x 51297, y 2000. Read §R first. The node ids in §0–§10 are the deleted originals; §R.4 maps each one to its new node.**

## R. Rebuilt 2026-10-07 (read this first)

**The section was deleted and has been rebuilt.** The original section `2497:3804` was deleted from the file. It was last at x 33937, y 8918, under TOUCH CONTROLS. The likely cause is Codex's iOS app-icon run on 2026-10-04 (commit 756a031): it put six "ICON · BRAND YELLOW · 1024×1024" frames inside TOUCH CONTROLS 2528:5403 at that spot. Red asked (2026-10-07): "Rebuild it, in a new position that does not conflict."

The node ids in §0–§10 below are the **deleted originals**. §R.4 maps every one of them to its new node.

### R.1 Where it is now

| Item | Value |
|---|---|
| Section | **`2748:6099`** "FRONTROOMS · DOORS + WINDOWS · PROPOSAL (rebuilt 2026-10-07)" |
| Link | https://www.figma.com/design/0tCbAiVUlrPId3RWd9LRif?node-id=2748-6099 |
| Position | **x 51297, y 2000**, 8280 × 3880. The hidden DW01 is parked at (120, 4040) as before, so the footprint is x 51297–59577, y 2000–7120 |
| Why here | This is where the section was first built on 2026-10-03, before someone moved it. It is the column agreed for it, between the planned ROOM VISUALS (x 42617) and DOOR BREAK · VISUAL PROPOSAL (x 59977), both still unbuilt. The column at x 33937 was not used |
| Clearance | All 25 page-level nodes and sections (nested ones too) were listed by `absoluteBoundingBox` before the build and again after it. Nothing lies within 400 px of the footprint. The gap to the two planned sections is exactly 400 px. The nearest built section is HUD KEY 2532:4038, at x 68657 |
| Untouched | The six icon frames and every other section |

| Slide | Frame | Name | Cell |
|---|---|---|---|
| 01 / 13 | `2748:6100` | DW00 · Three boxes | r1 c1 |
| 02 / 13 | `2748:6109` | DW01 · What we take from RE8 · HIDDEN until C1 + C2 arrive | hidden, (120, 4040) |
| 03 / 13 | `2748:6118` | DW02 · Stop behind every gap | r1 c2 |
| 04 / 13 | `2748:6127` | DW03 · The store's own doors | r1 c3 |
| 05 / 13 | `2748:6136` | DW04 · Locked from 20 m | r1 c4 |
| 06 / 13 | `2748:6145` | DW05 · One section eight doors | r2 c1 |
| 07 / 13 | `2748:6154` | DW06 · A lock that takes a key | r2 c2 |
| 08 / 13 | `2748:6163` | DW07 · A key on a hook | r2 c3 |
| 09 / 13 | `2748:6172` | DW08 · A window, not a pane | r2 c4 |
| 10 / 13 | `2748:6181` | DW09 · The glass sits in a pocket | r3 c1 |
| 11 / 13 | `2748:6190` | DW10 · What changes | r3 c2 |
| 12 / 13 | `2748:6199` | DW11 · Your calls | r3 c3 |
| 13 / 13 | `2753:6258` | DW12 · Later and already set | r3 c4 |

Diagrams: D1 `2749:6143`, D2 `2750:6186`, D3 `2751:6155`, D4 `2751:6210` (S0 geometry `2752:6247`), D5 `2751:6251`.

### R.2 How it was rebuilt

- **The original scripts were replayed.** Every `use_figma` write of the figma stage (agent a70db8dd65ac242ff) and the fix stage (a4e2b26f26f207883) of run wf_144d6856-540 ran again, in order, with its code unchanged except for the node ids: A041, A042, A044, A046, A047, A049, A050, A064, A065, A068, A069, A079, A091, A093, B089, B103, B107, B110, B145, B159.
- **How the ids were mapped.** The read-only dumps the old ids came from were replayed too (A096, B009, B010, B083, plus a compact form of B008, whose original output was over the 20 KB limit). Each new result was paired with the original result, id for id. Every id pairing kept the same names, text and structure. No id was guessed.
- **What the rebuild returned matches the original scripts' returns:** the same text heights, the same 11 chip widths in the layout pass, the same D2 label rows and the same DW11 row positions. The one exception is in §R.5.
- **Final audit, the same numbers as the fix pass's own audit:**
  - 604 nodes, 13 frames (DW01 hidden);
  - fonts IBM Plex Mono 65, Bayon 201, Source Serif 4 52, with no Inter;
  - 68 named slots: 48 hold an image (one IMAGE fill each) and 20 are placeholders (15 `img:`, 5 `ref:`).
- **Images.** All 48 were uploaded with `upload_assets` into their named slots: raw PNG bytes, so the layer names stayed, and `scaleMode` FILL, as in the original. Images could not be taken from Figma's history: `figma.getImageByHash` returned null for all 24 original hashes tried.
- **Visual check.**
  - The section and all 12 visible slides were screenshotted (`$W/recovery/dw_rebuild/shots/`).
  - They were compared with the original's screenshots that survive in the old transcripts. DW05 and DW12 differ from the fix stage's own screenshots by a mean of 0.0 and 0.04 per channel. DW03, DW08, DW09 and DW10 differ from its 2000 px check sheet only by resampling at edges.
  - Logged as **VL113** (`2767:6093`) in the verification log.
- **Where the files are.** The replay scripts, the id map (`map.json`, 407 ids), the 48 uploaded crops, the hashes and the screenshots are in `/Users/redwang/FrontRoomsVisualWork/recovery/dw_rebuild/`. The six recovered tiles are also in `proposal/media/rebuild/`, because their sources no longer exist.

### R.3 Images: slot, new node, provenance

Path keys as in §5 (`PM` = `proposal/media/`, `IMG` = `interactables/images/`). Every "byte-identical" image has the same imageHash as its original upload, so it is the same file.

| Slide | Slot | Node | Provenance |
|---|---|---|---|
| DW00 | `dw00_slit` | `2749:6093` | Byte-identical: same imageHash `2cd42a70c333…` |
| DW00 | `dw00_swing_a` | `2749:6102` | Byte-identical: same imageHash `8df28224dc7b…` |
| DW00 | `dw00_swing_b` | `2749:6105` | Byte-identical: same imageHash `37b3f5763ba3…` |
| DW00 | `dw00_locked` | `2749:6108` | Byte-identical: same imageHash `a1598b7a8eed…` |
| DW00 | `dw00_key` | `2749:6111` | Byte-identical: same imageHash `f33ac6dd7ec8…` |
| DW00 | `dw00_window` | `2749:6114` | Byte-identical: same imageHash `62a0192aedec…` |
| DW02 | `dw02_today` | `2749:6137` | Byte-identical: same imageHash `a7ed686847a8…` |
| DW02 | `dw02_option_a` | `2749:6140` | Byte-identical: same imageHash `b0065c38c777…` |
| DW02 | `dw02_leak` | `2749:6144` | Re-cut: same source, same crop code; PNG bytes differ: `PM/ingame_door_gap3_00_floor_leak.png` (700, 560, 1220, 845) at 2× |
| DW02 | `dw02_proxy` | `2749:6147` | Re-cut: same source, same crop code; PNG bytes differ: `PM/ingame_door_gap3_01_floor_shadowproxy.png` (700, 560, 1220, 845) at 2× |
| DW03 | `dw03_a24_door` | `2749:7254` | Byte-identical: same imageHash `93191c39a5d5…` |
| DW03 | `dw03_a24_dark_door` | `2749:7257` | Byte-identical: same imageHash `f984d7f288b0…` |
| DW03 | `dw03_kane_oak_door` | `2749:7260` | Byte-identical: same imageHash `45ddfac9bbbe…` |
| DW03 | `dw03_kane_bolts` | `2749:7263` | Byte-identical: same imageHash `c1246b1f2c15…` |
| DW03 | `dw03_locked_mockup` | `2749:7266` | Re-cut: same source, same crop code; PNG bytes differ: `IMG/05_r3_close_L0lit_recommended.jpg` (180, 120, 1150, 842) |
| DW04 | `dw04_l0lit_6m` | `2749:7269` | Byte-identical: same imageHash `a6fc4dbd8d5f…` |
| DW04 | `dw04_l0lit_12m` | `2749:7272` | Byte-identical: same imageHash `70b77b1d0c94…` |
| DW04 | `dw04_l0lit_20m` | `2749:7275` | Byte-identical: same imageHash `8153e8719448…` |
| DW04 | `dw04_l0dim_6m` | `2749:7278` | Byte-identical: same imageHash `e224d2e1cbf0…` |
| DW04 | `dw04_l0dim_12m` | `2749:7281` | Byte-identical: same imageHash `7dc32898bb49…` |
| DW04 | `dw04_l0dim_20m` | `2749:7282` | Byte-identical: same imageHash `9b24ac13589c…` |
| DW04 | `dw04_office_6m` | `2749:7283` | Byte-identical: same imageHash `75dedd690576…` |
| DW04 | `dw04_office_12m` | `2749:7286` | Byte-identical: same imageHash `0c70bcd1ee86…` |
| DW04 | `dw04_office_20m` | `2749:7287` | Byte-identical: same imageHash `1f4747b68d7f…` |
| DW04 | `dw04_no_p1` | `2749:7288` | Byte-identical: same imageHash `4d833f3206f6…` |
| DW04 | `dw04_no_p3` | `2749:7291` | Byte-identical: same imageHash `dfede3b68f61…` |
| DW04 | `dw04_no_p2p` | `2749:7294` | Byte-identical: same imageHash `0736561d2a9e…` |
| DW05 | `DoorSet_L0-F_persp` | `2750:6093` | Re-cut: same source, same crop code; PNG bytes differ: `PM/wip/spec_l0f.png` (194, 0, 1407, 900) |
| DW05 | `DoorSet_OF-F_persp` | `2750:6104` | Re-cut: same source, same crop code; PNG bytes differ: `PM/wip/spec_off.png` (274, 0, 1487, 900) |
| DW05 | `DoorSet_L0-K_persp` | `2750:6119` | Re-cut: same source, same crop code; PNG bytes differ: `PM/wip/spec_l0k.png` (204, 0, 1417, 900) |
| DW05 | `DoorSet_OF-K_persp` | `2750:6124` | Re-cut: same source, same crop code; PNG bytes differ: `PM/wip/spec_ofk.png` (274, 0, 1487, 900) |
| DW06 | `dw06_dip_start` | `2750:6139` | Byte-identical: same imageHash `6bd2cece1df0…` |
| DW06 | `dw06_dip_mid` | `2750:6142` | Byte-identical: same imageHash `c819a06429af…` |
| DW06 | `dw06_dip_pose_p` | `2750:6145` | Byte-identical: same imageHash `d8a7ffbeb9bb…` |
| DW06 | `dw06_dip_key_in` | `2750:6148` | Byte-identical: same imageHash `4e03b34ebe4d…` |
| DW06 | `dw06_dip_turned` | `2750:6151` | Byte-identical: same imageHash `7ec849461938…` |
| DW06 | `LockSet_Mortise_persp` | `2750:6182` | **Recovered.** `IMG/g2_e_poseP_key090.jpg` (the same render as the lost `G2/selfcheck/e_poseP_key090.png`, a JPEG copy; mean diff 0.71 / 255 against the fix stage's contact sheet), same crop (660, 400, 1060, 696) |
| DW07 | `KeySet_Cabinet_persp` | `2751:6102` | **Substitute.** The 12:31 `G3_cabinet_34.png` is lost. `IMG/g3/G3_cabinet_34_v2.jpg` is the same camera and stand-in colours, re-rendered at 22:50 after the cabinet's corner-arc fix (VL072); 1.2 / 255 against the old 324 px sheet. Same crop (0, 18, 1300, 982) |
| DW07 | `KeySet_Hook_persp` | `2751:6105` | **Recovered.** `IMG/g3/G3_keyhook_close.jpg` (same render as the lost PNG, JPEG copy; 0.55 / 255), same crop (0, 116, 900, 784) |
| DW07 | `KeySet_Desk_persp` | `2751:6108` | **Recovered at 640 px.** The render `G3_flat_desk_close.png` is lost and `G3_flat_desk_close_v2.jpg` is a different re-render (the tag moved). The tile comes from the fix stage's contact sheet `crops1.jpg` (embedded in transcript a4e2b26f26f207883): 640 × 474 JPEG; its label bar hid the top 19 rows, so rows 0–21 repeat row 22 (plain desk surface) |
| DW08 | `Kit_WindowFrame_Wood_persp` | `2751:6127` | Re-cut: same source, same crop code; PNG bytes differ: `PM/wip/Kit_WindowFrame_Wood_faceA_1p5m.png` (0, 4, 1200, 897) |
| DW08 | `Kit_WindowFrame_Steel_persp` | `2751:6130` | Re-cut: same source, same crop code; PNG bytes differ: `PM/wip/Kit_WindowFrame_Steel_faceA_1p5m.png` (0, 4, 1200, 897) |
| DW08 | `Kit_WindowFrame_Alu_persp` | `2751:6137` | **Recovered at 640 px.** The render `G4/Kit_WindowFrame_Alu_faceA_1p5m.png` is lost (no copy). The tile comes from the same `crops1.jpg` sheet: 640 × 476 JPEG, rows 0–21 repeat row 22 (glass, card edge and jambs run straight up) |
| DW08 | `dw08_kane_window` | `2751:6140` | Re-cut: same source, same crop code; PNG bytes differ: `PM/kane_emg_0308_framed_window_1990.jpg` (0, 120, 1290, 1080) |
| DW09 | `Kit_WindowFrame_Steel_close` | `2751:6245` | Re-cut: same source, same crop code; PNG bytes differ: `PM/wip/Kit_WindowFrame_Steel_screws_0p3m.png` (0, 95, 1200, 835), edges extended (§5.3) |
| DW09 | `Kit_WindowFrame_Wood_close` | `2751:6248` | Re-cut: same source, same crop code; PNG bytes differ: `PM/wip/Kit_WindowFrame_Wood_stoolA_0p3m.png` (0, 150, 1200, 810) |
| DW12 | `dw12_peek_lite` | `2753:6290` | Re-cut: same source, same crop code; PNG bytes differ: `IMG/05_r3_close_Office_recommended_lite.jpg` (600, 80, 1300, 601) |
| DW12 | `Kit_ExitSign_persp` | `2753:6293` | **Recovered.** `IMG/g3/G3_exit_sign_close.jpg` (same render as the lost `G3_exit_sign_close.png`, JPEG copy; 1.29 / 255), same crop (140, 0, 1484, 1000) |

Totals:
- 29 byte-identical;
- 13 re-cut from the same sources with the same code. Pixel-equal where a lossless reference exists: Steel close-up and OF-F match the fix stage's own previews at 0.0 / 255. Everything else is within 0.9–1.9 / 255 of the fix stage's JPEG contact sheets;
- 6 recovered: 3 from same-render JPEG copies, 1 substitute render, 2 at 640 px from a contact sheet.

### R.4 Old id → new id (every id this file cites)

| Old (deleted) | New | Node |
|---|---|---|
| `2497:3804` | `2748:6099` | FRONTROOMS · DOORS + WINDOWS · PROPOSAL (rebuilt 2026-10-07) |
| `2497:3805` | `2748:6100` | DW00 · Three boxes |
| `2497:3820` | `2748:6109` | DW01 · What we take from RE8 · HIDDEN until C1 + C2 arrive |
| `2497:3829` | `2748:6118` | DW02 · Stop behind every gap |
| `2497:3838` | `2748:6127` | DW03 · The store's own doors |
| `2497:3847` | `2748:6136` | DW04 · Locked from 20 m |
| `2497:3856` | `2748:6145` | DW05 · One section eight doors |
| `2497:3865` | `2748:6154` | DW06 · A lock that takes a key |
| `2497:3874` | `2748:6163` | DW07 · A key on a hook |
| `2497:3883` | `2748:6172` | DW08 · A window, not a pane |
| `2497:3892` | `2748:6181` | DW09 · The glass sits in a pocket |
| `2497:3901` | `2748:6190` | DW10 · What changes |
| `2497:3910` | `2748:6199` | DW11 · Your calls |
| `2499:3828` | `2749:6117` | ref:re8_steam_castle_hall |
| `2499:3829` | `2749:6118` | placeholder · re8_steam_castle_hall = re8_steam_castle_hall · AWAITING OK · C1 |
| `2499:3832` | `2749:6121` | ref:re8_walkthrough_1-36-39 |
| `2499:3833` | `2749:6122` | placeholder · re8_walkthrough_1-36-39 = re8_walkthrough_1-36-39 · AWAITING OK ·  |
| `2499:3854` | `2749:6143` | diagram:dw02_jamb_section |
| `2499:3855` | `2749:6144` | media:dw02_leak |
| `2499:3858` | `2749:6147` | media:dw02_proxy |
| `2500:4902` | `2749:7266` | media:dw03_locked_mockup |
| `2501:3804` | `2750:6093` | img:DoorSet_L0-F_persp |
| `2501:3815` | `2750:6104` | img:DoorSet_OF-F_persp |
| `2501:3820` | `2750:6109` | img:DoorSet_RN-F_persp |
| `2501:3821` | `2750:6110` | placeholder · DoorSet_RN-F_persp = DoorSet_RN-F_persp · PHASE 2 |
| `2501:3822` | `2750:6111` | chip · Run · free · P2 · ward door, no latch |
| `2501:3825` | `2750:6114` | img:DoorSet_EX-F_persp |
| `2501:3826` | `2750:6115` | placeholder · DoorSet_EX-F_persp = DoorSet_EX-F_persp · PHASE 2 |
| `2501:3827` | `2750:6116` | chip · Exit · free · P2 · push bar, lit EXIT |
| `2501:3830` | `2750:6119` | img:DoorSet_L0-K_persp |
| `2501:3835` | `2750:6124` | img:DoorSet_OF-K_persp |
| `2501:3840` | `2750:6129` | img:DoorSet_RN-K_persp |
| `2501:3841` | `2750:6130` | placeholder · DoorSet_RN-K_persp = DoorSet_RN-K_persp · PHASE 2 |
| `2501:3842` | `2750:6131` | chip · Run · locked · P2 · STAFF ONLY sign |
| `2501:3845` | `2750:6134` | img:DoorSet_EX-K_persp |
| `2501:3846` | `2750:6135` | placeholder · DoorSet_EX-K_persp = DoorSet_EX-K_persp · PHASE 2 |
| `2501:3847` | `2750:6136` | chip · Exit · locked · P2 · dead EXIT sign |
| `2501:3865` | `2750:6154` | img:Kit_Lock_Escutcheon_persp |
| `2501:3866` | `2750:6155` | placeholder · Kit_Lock_Escutcheon_persp = Kit_Lock_Escutcheon_persp · PHASE 2 |
| `2501:3867` | `2750:6156` | chip · Escutcheon · fixed |
| `2501:3869` | `2750:6158` | img:Kit_Lock_CylinderShell_persp |
| `2501:3870` | `2750:6159` | placeholder · Kit_Lock_CylinderShell_persp = Kit_Lock_CylinderShell_ persp · PHASE 2 |
| `2501:3871` | `2750:6160` | chip · Cylinder · fixed |
| `2501:3873` | `2750:6162` | img:Kit_Lock_Plug_persp |
| `2501:3874` | `2750:6163` | placeholder · Kit_Lock_Plug_persp = Kit_Lock_Plug_persp · PHASE 2 |
| `2501:3875` | `2750:6164` | chip · Plug |
| `2501:3877` | `2750:6166` | img:Kit_Lock_Knob_persp |
| `2501:3878` | `2750:6167` | placeholder · Kit_Lock_Knob_persp = Kit_Lock_Knob_persp · PHASE 2 |
| `2501:3879` | `2750:6168` | chip · Knob |
| `2501:3881` | `2750:6170` | img:Kit_Lock_Deadbolt_persp |
| `2501:3882` | `2750:6171` | placeholder · Kit_Lock_Deadbolt_persp = Kit_Lock_Deadbolt_persp · PHASE 2 |
| `2501:3883` | `2750:6172` | chip · Deadbolt |
| `2501:3885` | `2750:6174` | img:Kit_Lock_Latchbolt_Mortise_persp |
| `2501:3886` | `2750:6175` | placeholder · Kit_Lock_Latchbolt_Mortise_persp = Kit_Lock_Latchbolt_Mortise_ persp · PHAS |
| `2501:3887` | `2750:6176` | chip · Latch · 19 mm |
| `2501:3889` | `2750:6178` | img:Kit_Lock_StrikeMortise_persp |
| `2501:3890` | `2750:6179` | placeholder · Kit_Lock_StrikeMortise_persp = Kit_Lock_StrikeMortise_ persp · PHASE 2 |
| `2501:3891` | `2750:6180` | chip · Strike · tears off |
| `2501:3893` | `2750:6182` | img:LockSet_Mortise_persp |
| `2501:3897` | `2750:6186` | diagram:dw06_unlock_timeline |
| `2503:4468` | `2751:6093` | img:KeySet_Rack_persp |
| `2503:4469` | `2751:6094` | chip · Lobby · key rack |
| `2503:4477` | `2751:6102` | img:KeySet_Cabinet_persp |
| `2503:4480` | `2751:6105` | img:KeySet_Hook_persp |
| `2503:4483` | `2751:6108` | img:KeySet_Desk_persp |
| `2503:4487` | `2751:6112` | ref:henryford_tivoli_motel_key |
| `2503:4488` | `2751:6113` | placeholder · henryford_tivoli_motel_key = henryford_tivoli_motel_key · AWAITING OK |
| `2503:4491` | `2751:6116` | img:KeyParts_Lineup_front |
| `2503:4492` | `2751:6117` | placeholder · KeyParts_Lineup_front = KeyParts_Lineup_front · PHASE 2 |
| `2503:4493` | `2751:6118` | chip · 9 tags · same number on the door |
| `2503:4502` | `2751:6127` | img:Kit_WindowFrame_Wood_persp |
| `2503:4505` | `2751:6130` | img:Kit_WindowFrame_Steel_persp |
| `2503:4508` | `2751:6133` | img:Kit_WindowFrame_Steel_Enamel_persp |
| `2503:4509` | `2751:6134` | placeholder · Kit_WindowFrame_Steel_Enamel_persp = Kit_WindowFrame_Steel_Enamel_persp · PHA |
| `2503:4510` | `2751:6135` | chip · Run · white steel · P2 |
| `2503:4512` | `2751:6137` | img:Kit_WindowFrame_Alu_persp |
| `2503:4515` | `2751:6140` | media:dw08_kane_window |
| `2503:4518` | `2751:6143` | ref:sears1993_levolor_miniblinds |
| `2503:4519` | `2751:6144` | placeholder · sears1993_levolor_miniblinds = sears1993_levolor_miniblinds · AWAITING  |
| `2503:4522` | `2751:6147` | ref:us4463535_glass_stop |
| `2503:4523` | `2751:6148` | placeholder · us4463535_glass_stop = us4463535_glass_stop · AWAITING OK · C11 |
| `2503:4526` | `2751:6151` | img:Kit_MiniBlind_Raised_persp |
| `2503:4527` | `2751:6152` | placeholder · Kit_MiniBlind_Raised_persp = Kit_MiniBlind_Raised_persp · PHASE 2 |
| `2503:4528` | `2751:6153` | chip · Office · raised blind |
| `2505:3804` | `2751:6155` | diagram:dw09_elevation |
| `2505:3862` | `2751:6207` | label = Ray tracing aims at this glass |
| `2505:3865` | `2751:6210` | diagram:dw09_jamb_section |
| `2505:3900` | `2751:6245` | img:Kit_WindowFrame_Steel_close |
| `2505:3903` | `2751:6248` | img:Kit_WindowFrame_Wood_close |
| `2505:3906` | `2751:6251` | diagram:dw10_door_plan |
| `2507:3804` | `2751:7368` | placeholder · KeySet_Rack_persp = KeySet_Rack_persp · PHASE 2 |
| `2534:5783` | `2752:6247` | geometry · S0 fallback · 2.4 px/mm |
| `2534:5789` | `2752:6253` | label = With the map's OK |
| `2534:5790` | `2752:6254` | label = If the map says no |
| `2534:5793` | `2752:6257` | label = Dark 12 mm line |
| `2534:5877` | `2753:6258` | DW12 · Later and already set |
| `2534:5882` | `2753:6263` | header page = 13 / 13 |
| `2534:5883` | `2753:6264` | title = Later, and already set |
| `2534:5884` | `2753:6265` | lede = Two calls can wait for Run and Exit. The |
| `2534:5885` | `2753:6266` | statement = None of this blocks phase 2; any line ca |
| `2534:5886` | `2753:6267` | call = 9 · A peek window |
| `2534:5889` | `2753:6270` | call body = Default: red. Or: green, to suit the cya |
| `2534:5894` | `2753:6275` | rule = Also set |
| `2534:5902` | `2753:6283` | default row = Run windows: plain glass. Wired glass wo |
| `2534:5908` | `2753:6289` | default row = Bronze frames would reflect white in ray |
| `2534:5909` | `2753:6290` | media:dw12_peek_lite |
| `2534:5910` | `2753:6291` | chip · Peek window · Office light · mock-up |
| `2534:5912` | `2753:6293` | img:Kit_ExitSign_persp |
| `2534:5913` | `2753:6294` | chip · EXIT · red letters · WIP |

The full map (407 ids, including every node the scripts touched) is `/Users/redwang/FrontRoomsVisualWork/recovery/dw_rebuild/map.json`.

### R.5 Differences from the original that could not be avoided

1. **New node ids** everywhere (§R.4). Links to `node-id=2497-3804` no longer resolve.
2. **Name and position.** The section name now ends in "(rebuilt 2026-10-07)". The section is at x 51297, y 2000 instead of x 33937, y 8918.
3. **KeySet_Cabinet_persp (DW07) is a substitute render.** The original 12:31 render is lost. The tile uses the 22:50 `G3_cabinet_34_v2.jpg`: the same camera and the same stand-in colours, but the cabinet model after its corner-arc fix.
4. **KeySet_Desk_persp (DW07) and Kit_WindowFrame_Alu_persp (DW08) are 640 px JPEG tiles**, not the original 852 px PNGs. They are still above the 426 px slot. In each, the top 22 rows (4.6 %) are row 22 repeated, because a contact-sheet label covered them.
5. **LockSet_Mortise_persp (DW06), KeySet_Hook_persp (DW07) and Kit_ExitSign_persp (DW12) come from JPEG copies** of the same renders. The content is the same; there are JPEG artefacts at full zoom.
6. **13 re-cut tiles** have the same pixels but different PNG bytes, so their imageHashes differ. This is a Pillow version difference.
7. **The DW00 hero chip** "See-through slit · 1.2 m · near room dimmed" is 386 px wide, not 377. It has the same text and style; the difference is font metrics on this machine. It still sits inside its slot. Every other measured width and height matches.
8. **Unchanged on purpose:** the running header still reads "Week 3 · Oct 3, 2026", and all text is word for word the fix pass's, including DW11's 8 calls and DW12.

---

## 0. Fix pass (17:00): what changed, and the review verdicts

A review raised 22 issues. 21 were confirmed and fixed. Two were fixed only in part, with reasons (items 5 and 19). None was rejected outright.

| # | Issue | Verdict | What changed |
|---|---|---|---|
| 1 | DW11 defaults lived only in "speaker notes", which Figma frames do not have | Confirmed | New slide **DW12 "Later, and already set"** (13 / 13). It carries the deferred calls 9–10 and an "Also set" list of 7 one-line defaults: Run plain glass (wired = two-stage break), Exit same break (or a tell), lever vs the film's knob, EMPLOYEES ONLY on both faces, item glint off, ray-traced glass on the visible pane, bronze frames reflecting white. **Lobby glass (clear vs hammered) became call 6** on DW11. The bent-slat fallback went into call 7. The steel-door honesty line is on DW03 |
| 2 | DW01 has no real media | Confirmed | DW01 is **hidden** and parked below the grid until C1 and C2 arrive. The other 12 slides close up. C1 and C2 now head the batch request (`media_candidates.md`). The walkthrough chip says "third-person camera" |
| 3 | DW01 states our spec as RE8 fact | Confirmed | Title "What we take from RE8". Each body is "RE8: … Ours: …", and the RE8 half keeps to what V1/V2 show. The stop line says the mechanism is unconfirmed |
| 4 | Colour-wrong WIP previews (pale steel frames, orange walnut, tan stool) | Confirmed for the doors | Re-rendered in Blender with the spec albedos (`media/SOURCES.md`). The window renders had already used the measured albedos after 12:43; the pinkish stool tile on the slide predated that, and it was replaced too. The lock preview is cropped so its stand-in frame does not show |
| 5 | `Kit_KeyBoard` and `Kit_Keyboard` are one file on macOS | Confirmed (APFS is case-insensitive here: tested) | Deck and notes now use **`Kit_KeyRack`** and the slot `img:KeySet_Rack_persp`. **Not done here:** renaming the module (`interact_key_board.py:31`) and `10_spec.md` (outside this workflow's write scope; the interactables workflow owns them and was building at 16:45). **Not done:** restoring `proj_int`'s keyboard. At 17:10 `proj_int/…/Kit_Keyboard.fbx/.json` already matched the real project byte for byte (md5 7289…3471), so someone restored it; but a G3 build that includes `interact_key_board` was running from 16:45, and restoring before the rename would only delete the clone's one key-board model. See §9 |
| 6 | DW09 presents the S1 stop band as decided | Confirmed | Statement "Real stops need 16 mm inside the opening: one OK from the map chat." The jamb diagram now shows **S0** (no stops, flat reveal, dark 12 mm line, edge shows at an angle) under S1, labelled "If the map says no" / "With the map's OK" |
| 7 | DW10 lede and statement false against `10` §6.5 | Confirmed | Lede "Colliders and names stay; every model is render-only." Statement "Play changes three ways: doors open one way, some doors lock, keys hang on hooks." Map body names the pull event, the Relay break, the lock point at the keyhole. Visual body "All 38 models built in a test copy, none in the game … LOD1 and LOD2 wait for the LOD change." The 0.7K label says "after the LOD change". The LOD change is call 8 |
| 8 | DW11 calls 2 and 3 | Confirmed | Call 2: "into the Standard room (the bigger one), so escapes into Low rooms are pulls". Call 3: "step back 0.45 m (60° if blocked). Cost: escaping from the swing side is slower. Or: a push-only rule." |
| 9 | Call 5 has no alternative | Confirmed | "Default: yes … Or: an unlocked door never shuts fully; it rests ajar at 10°." |
| 10 | DW03 chips are source credits | Confirmed | Chips are claims ("Dark door = open doorway at 12 m", "Six bolts: 3–4 px at 12 m", …). Statement "…Locked: a 1990 steel back door, not seen in the film." The tape slot now holds the in-engine locked-door mock-up |
| 11 | `05_r3_close_L0lit_recommended.jpg` unused | Confirmed | Now on DW03 (`media:dw03_locked_mockup`). The Office lite close-up is on DW12 for call 9 |
| 12 | DW06 strip shows the retired lock; no picture of the proposed lock | Confirmed | First chip "Old mock-up · key in knob", later chips "Key in" / "Turned". `img:LockSet_Mortise_persp` holds the G2 pose-P render (key in, turned), chip "Proposed lock · pose P · WIP" |
| 13 | DW05 "One frame … only the finishes change" overclaims | Confirmed (`10` D5: 2 frame meshes, 3–4 leaves, two locksets) | Title "One section, eight doors". Lede "Same 16 mm stop, 12 mm saddle, 44 mm leaf, hinges and 1.0 m hardware. Frame, leaf and lock vary." |
| 14 | DW05 cell texts are captions under images | Confirmed | Removed. Each chip carries the materials. Slots grew to the half-row grid (426 × 316 / 317) |
| 15 | DW07 shows no key; "Key · 60 mm" | Confirmed | `img:KeySet_Desk_persp` holds the G3 flat-desk render (key, ring, tag "14"). Label "Key · 58 mm" (5 m recognition still holds: 691 × 0.058 / 8 = 5.0 m). The rack stays a placeholder (its stand-in colour is wrong) |
| 16 | DW08 frames float in a studio; Kane not first; stamp half covered | Confirmed | Kane 3:08 first, cropped below the VHS stamp, yellow chip "1990 control room · window into Level 0". Wood, steel and aluminium now show the frame in a wall with the stand-in glass (spec albedos) |
| 17 | DW02 before/after pair reads the same; B prototype unnamed | Confirmed | 2× crop on the threshold. Chips "… · B prototype" and "… · same fix on A" |
| 18 | DW09 screw chip vs render; duplicate label | Confirmed | The slot now shows two stop screws on the jamb at 0.3 m (§5.3); chip "Office · screwed stop · 0.3 m · WIP". The diagram's "30 screws" label became "Ray tracing aims at this glass" |
| 19 | No `_front` slots | Confirmed; deliberate deviation kept | See §6.5. DW05 and DW08 have no room for a 12-slot elevation strip after the captions moved into chips. Equal-scale fronts go to 平面视觉's three-view section in phase 2. For the visual chat to confirm with Red |
| 20 | DW06 facts twice | Confirmed | Part chips "Plug", "Knob", "Deadbolt"; the timeline keeps 90°, 40°, 25 mm. "Latch · 19 mm", "Strike · tears off", "… · fixed" stay: they are not repeated |
| 21 | DW00 lede and hero chip | Confirmed | "…no bead or stool at the glass." Chip "See-through slit · 1.2 m · near room dimmed" |
| 22 | DW04 reject chip | Confirmed | "No · wood door + deadbolt only" |
| 23 | DW11 lede wrap; call 7 mixes two things; P2 calls crowd | Confirmed | Lede "Each call has a default. Confirm or change it. Phase 2 builds what you pick." Run/Exit calls moved to DW12 as 9 and 10 ("EXIT letters: red or green") |
| 24 | DW08/DW09 suggest this kit builds the glass; RT needs missing | Confirmed | DW08 lede "…a pocket for the glass track's pane." RT target on DW09 (diagram label) and DW12 (two rows) |
| 25 | DW02 lede "now" | Confirmed | "In the prototype every gap turns a corner." |

(The list has 25 rows because some review items bundled two fixes.)

## 1. Where it is

| Item | Value |
|---|---|
| File | `0tCbAiVUlrPId3RWd9LRif` ("Undergoing Game Projects"), page `2099:76` |
| Section | **`2497:3804`** "FRONTROOMS · DOORS + WINDOWS · PROPOSAL" |
| Link | https://www.figma.com/design/0tCbAiVUlrPId3RWd9LRif?node-id=2497-3804 |
| Position at 17:10 | **x 33937, y 8918**, 8280 × 3880. Someone moved it after the build (it was placed at x 51297, y 2000). It now sits under "TOUCH CONTROLS · iOS + ANDROID" (2528:5403, ends y 8800) |
| Frames | 13 × 1920 × 1080. 12 visible in 3 rows of 4 at x 120 / 2160 / 4200 / 6240 and y 160 / 1400 / 2640. **DW01 is hidden** at (120, 4040), just below the section's bottom edge (absolute y 12958–14038; that area was empty at 17:10) |

**Overlap check.** All 18 sections on the page were listed by `absoluteBoundingBox` before the fix pass. Nothing overlaps the section or the hidden DW01's parking spot. Nothing outside `2497:3804` was edited.

## 2. Frames

Page numbers run by slide order, out of 13. Slide 02 is hidden, so the visible deck reads 01, 03, 04 … 13.

| Slide | Frame id | Frame name | Title | Grid cell |
|---|---|---|---|---|
| 01 / 13 | `2497:3805` | DW00 · Three boxes | Three boxes | r1 c1 |
| 02 / 13 | `2497:3820` | DW01 · What we take from RE8 · HIDDEN until C1 + C2 arrive | What we take from RE8 | hidden |
| 03 / 13 | `2497:3829` | DW02 · Stop behind every gap | A stop behind every gap | r1 c2 |
| 04 / 13 | `2497:3838` | DW03 · The store's own doors | The store's own doors | r1 c3 |
| 05 / 13 | `2497:3847` | DW04 · Locked from 20 m | Locked, from 20 m | r1 c4 |
| 06 / 13 | `2497:3856` | DW05 · One section eight doors | One section, eight doors | r2 c1 |
| 07 / 13 | `2497:3865` | DW06 · A lock that takes a key | A lock that takes a key | r2 c2 |
| 08 / 13 | `2497:3874` | DW07 · A key on a hook | A key on a hook | r2 c3 |
| 09 / 13 | `2497:3883` | DW08 · A window, not a pane | A window, not a pane | r2 c4 |
| 10 / 13 | `2497:3892` | DW09 · The glass sits in a pocket | The glass sits in a pocket | r3 c1 |
| 11 / 13 | `2497:3901` | DW10 · What changes | What changes in the game | r3 c2 |
| 12 / 13 | `2497:3910` | DW11 · Your calls | Your calls | r3 c3 |
| 13 / 13 | `2534:5877` | DW12 · Later and already set | Later, and already set | r3 c4 |

**To show DW01 when C1 and C2 arrive:** fill its two `ref:` slots, delete the two placeholder texts, set `visible = true`, and reflow the 13 frames by slide order into 4 rows (4, 4, 4, 1). The section must then grow to 8280 × 5120; list the page's sections first, because the area below may be taken by then.

## 3. Grammar (read from HUNTER 2331:852; unchanged)

- **Text styles:** only the file's P1 styles, applied by id: `P1/Display/Title` (Bayon 88/80), `P1/Display/Label` (Bayon 20/20, upper), `P1/Text/Body` (Source Serif 4 24/26), `P1/Text/Statement` (Source Serif 4 50/42), `P1/Header/Meta` (IBM Plex Mono 13/16). New text in the fix pass was cloned from existing nodes, so it carries the same style ids. No Inter.
- **Running header** at y 28 (meta only there). Titles x 72, y 139. Ledes x 1272, y 173, 576 wide, 2 lines (DW10's is 1). Statements x 72, y 969, one line. Content y 232–889.
- **Chips:** auto-layout, h 32, at slot +16 / +16. Black with white text, or the hero chip in yellow. Chips now state claims, not sources.
- **Placeholders:** slot filled #EEECE6 with a centred Bayon 20 slot name in mid grey; "· PHASE 2" for `img:`, "· AWAITING OK · C#" for `ref:`.
- **Diagrams:** #F3F1EA frames, black 2 px lines, Bayon 20 labels, one yellow accent.

## 4. Diagrams

| Slot | Node | Slide | Size | Notes |
|---|---|---|---|---|
| `diagram:dw02_jamb_section` (D1) | `2499:3854` | DW02 | 576 × 657 | 4.5 px/mm, unchanged |
| `diagram:dw06_unlock_timeline` (D2) | `2501:3897` | DW06 | 576 × 444 | 300 px/s, unchanged. It is the only place the motion numbers (90°, 40°, 25 mm, 1.5 mm, 10°) appear |
| `diagram:dw09_elevation` (D3) | `2505:3804` | DW09 | 576 × 657 | 280 px/m. Label `2505:3862` changed from "30 screws on the office face" to **"Ray tracing aims at this glass"** (the 30 dots stay) |
| `diagram:dw09_jamb_section` (D4) | `2505:3865` | DW09 | 576 × 657 | 2.4 px/mm. Top: S1 as before, new label "With the map's OK" (`2534:5789`). **Bottom: the stool inset was replaced by S0** (`2534:5783`, same scale): wall end, 2 mm lining, a dark 12 mm strip where the 6 mm glass meets the reveal, nothing proud of the reveal, and a yellow sight line to the exposed glass edge. Labels `2534:5790`–`2534:5793`: "If the map says no", "No stops · flat reveal", "Edge shows at an angle", "Dark 12 mm line". The stool now shows only in the DW09 close-up render and the DW08 wood render |
| `diagram:dw10_door_plan` (D5) | `2505:3906` | DW10 | 876 × 657 | 200 px/m, unchanged |

## 5. Slot → file map

Path keys: `PM` = `interactables/proposal/media/`, `IMG` = `interactables/images/`, `AUD` = `interaction_audit/images/`, `WIP` = `<scratchpad>/interact_previews/`, `R` = `<scratchpad>/dwfix/render/` (copies of the spec-albedo renders are in `PM/wip/`). Crops are source pixels (x0, y0, x1, y1). Uploads went through the Figma MCP `upload_assets` tool as raw PNG bytes into the named slot. The fix-pass crops are in `<scratchpad>/dwfix/crops/`.

### 5.1 Evidence

As built (see the build record in this file's history), except:

| Slide | Slot | Node | Size | Source | Crop |
|---|---|---|---|---|---|
| DW02 | `media:dw02_leak` | `2499:3855` | 576 × 316 | `PM/ingame_door_gap3_00_floor_leak.png` | **(700, 560, 1220, 845)**, 2× on the threshold |
| DW02 | `media:dw02_proxy` | `2499:3858` | 576 × 317 | `PM/ingame_door_gap3_01_floor_shadowproxy.png` | **(700, 560, 1220, 845)** |
| DW03 | `media:dw03_locked_mockup` (was `media:dw03_blue_tape`) | `2500:4902` | 426 × 317 | `IMG/05_r3_close_L0lit_recommended.jpg` | (180, 120, 1150, 842) |
| DW08 | `media:dw08_kane_window` | `2503:4515` | 426 × 316, **now at x 72, y 232** | `PM/kane_emg_0308_framed_window_1990.jpg` | **(0, 120, 1290, 1080)**, below the VHS stamp |
| DW12 | `media:dw12_peek_lite` | `2534:5909` | 426 × 317 | `IMG/05_r3_close_Office_recommended_lite.jpg` | (600, 80, 1300, 601) |

Unchanged from the build: `media:dw00_*` (6), `media:dw02_today`, `media:dw02_option_a`, `media:dw03_a24_door`, `media:dw03_a24_dark_door`, `media:dw03_kane_oak_door`, `media:dw03_kane_bolts`, `media:dw04_*` (12), `media:dw06_dip_*` (5). That is 34 evidence slots in all (DW01 has none). With the 14 WIP slots, 48 of the 68 named image slots carry a picture; 15 `img:` and 5 `ref:` slots are placeholders. 604 nodes in the section.

### 5.2 Work-in-progress previews (14 `img:` slots, chips end in "· WIP")

| Slide | Slot | Node | Size | Source | Crop |
|---|---|---|---|---|---|
| DW05 | `img:DoorSet_L0-F_persp` | `2501:3804` | 426 × 316 | `R/out/spec_l0f.png` | (194, 0, 1407, 900) |
| DW05 | `img:DoorSet_OF-F_persp` | `2501:3815` | 426 × 316 | `R/out/spec_off.png` | (274, 0, 1487, 900) |
| DW05 | `img:DoorSet_L0-K_persp` | `2501:3830` | 426 × 317 | `R/out/spec_l0k.png` | (204, 0, 1417, 900) |
| DW05 | `img:DoorSet_OF-K_persp` | `2501:3835` | 426 × 317 | `R/out/spec_ofk.png` | (274, 0, 1487, 900) |
| DW06 | `img:LockSet_Mortise_persp` | `2501:3893` | 276 × 204 | `WIP/G2/selfcheck/e_poseP_key090.png` | (660, 400, 1060, 696) |
| DW07 | `img:KeySet_Cabinet_persp` | `2503:4477` | 426 × 316 | `WIP/G3/G3_cabinet_34.png` | (0, 18, 1300, 982), unchanged |
| DW07 | `img:KeySet_Hook_persp` | `2503:4480` | 426 × 316 | `WIP/G3/G3_keyhook_close.png` | (0, 116, 900, 784), unchanged |
| DW07 | `img:KeySet_Desk_persp` | `2503:4483` | 426 × 316 | `WIP/G3/G3_flat_desk_close.png` | (204, 0, 1304, 816) |
| DW08 | `img:Kit_WindowFrame_Wood_persp` | `2503:4502` | 426 × 316, x 522 | `R/out_g4/Kit_WindowFrame_Wood_faceA_1p5m.png` | (0, 4, 1200, 897) |
| DW08 | `img:Kit_WindowFrame_Steel_persp` | `2503:4505` | 426 × 316, x 972 | `R/out_g4/Kit_WindowFrame_Steel_faceA_1p5m.png` | (0, 4, 1200, 897) |
| DW08 | `img:Kit_WindowFrame_Alu_persp` | `2503:4512` | 426 × 316, x 1422 | `WIP/G4/Kit_WindowFrame_Alu_faceA_1p5m.png` | (0, 4, 1200, 897) |
| DW09 | `img:Kit_WindowFrame_Steel_close` | `2505:3900` | 576 × 316 | `R/out_g4/Kit_WindowFrame_Steel_screws_0p3m.png` (copy in `PM/wip/`) | (0, 95, 1200, 835), sides extended, see §5.3 |
| DW09 | `img:Kit_WindowFrame_Wood_close` | `2505:3903` | 576 × 317 | `R/out_g4/Kit_WindowFrame_Wood_stoolA_0p3m.png` | (0, 150, 1200, 810) |
| DW12 | `img:Kit_ExitSign_persp` | `2534:5912` | 426 × 317 | `WIP/G3/G3_exit_sign_close.png` | (140, 0, 1484, 1000) |

**Read these previews with care:**
- **Door and window colours are the measured Unity means** (`10` §7.3), rendered in Blender, not in Unity. Lighting is a studio stand-in.
- **Door previews show frame and leaf only** (plus the closer on the Office pair). Lockset, sign, number plate and kick-plate detail come in phase 2.
- **The cabinet and hook tiles** (DW07) are the build's stand-in colours. They show shape only.
- **Not used, and why:** `G3_board_34.png` / `G3_hung_*.png` (the rack renders pink-brown; the spec is near-black `Prop_WoodDark`, luminance 0.014); the G2 single-part renders (40–90 px parts in a 900 × 700 frame); `G3_tags_front.png` (tags cut off); the Run and Exit door sets (they would need the white Run wall and the cyan Exit light to say anything true; T6 decides).

### 5.3 DW09 screw close-up

The old tile was the corner render, with one faint screw. The jamb screws sit about 0.23 m apart, which at 0.3 m is 670 px in a 1200 × 900 render: too tall for one 1.82 : 1 crop. Two other views were tried and not used: along the head stop (the screws hide in the stop's shadow) and the jamb from 0.6 m (three screws, but only 4–5 px each). The slot uses the 0.3 m render cropped to (0, 95, 1200, 835), with its flat grey left and right edge columns stretched by 74 px each to reach 1.82 : 1. Nothing inside the render was changed. Both screws read. The chip reads "Office · screwed stop · 0.3 m · WIP".

## 6. Reserved slots for phase 2

### 6.1 Placeholders (`img:`, "· PHASE 2")

| Slide | Slot | Slot node | Placeholder text | Chip | Size |
|---|---|---|---|---|---|
| DW05 | `img:DoorSet_RN-F_persp` | `2501:3820` | `2501:3821` | `2501:3822` | 426 × 316 |
| DW05 | `img:DoorSet_EX-F_persp` | `2501:3825` | `2501:3826` | `2501:3827` | 426 × 316 |
| DW05 | `img:DoorSet_RN-K_persp` | `2501:3840` | `2501:3841` | `2501:3842` | 426 × 317 |
| DW05 | `img:DoorSet_EX-K_persp` | `2501:3845` | `2501:3846` | `2501:3847` | 426 × 317 |
| DW06 | `img:Kit_Lock_Escutcheon_persp` | `2501:3865` | `2501:3866` | `2501:3867` | 276 × 204 |
| DW06 | `img:Kit_Lock_CylinderShell_persp` | `2501:3869` | `2501:3870` | `2501:3871` | 276 × 204 |
| DW06 | `img:Kit_Lock_Plug_persp` | `2501:3873` | `2501:3874` | `2501:3875` | 276 × 204 |
| DW06 | `img:Kit_Lock_Knob_persp` | `2501:3877` | `2501:3878` | `2501:3879` | 276 × 204 |
| DW06 | `img:Kit_Lock_Deadbolt_persp` | `2501:3881` | `2501:3882` | `2501:3883` | 276 × 204 |
| DW06 | `img:Kit_Lock_Latchbolt_Mortise_persp` | `2501:3885` | `2501:3886` | `2501:3887` | 276 × 204 |
| DW06 | `img:Kit_Lock_StrikeMortise_persp` | `2501:3889` | `2501:3890` | `2501:3891` | 276 × 204 |
| DW07 | **`img:KeySet_Rack_persp`** (was `KeySet_Board_persp`) | `2503:4468` | `2507:3804` | `2503:4469` (yellow) | 426 × 316 |
| DW07 | `img:KeyParts_Lineup_front` | `2503:4491` | `2503:4492` | `2503:4493` | 426 × 317 |
| DW08 | `img:Kit_WindowFrame_Steel_Enamel_persp` | `2503:4508` (now x 72, y 572) | `2503:4509` | `2503:4510` | 426 × 317 |
| DW08 | `img:Kit_MiniBlind_Raised_persp` | `2503:4526` | `2503:4527` | `2503:4528` | 426 × 317 |

15 slots. To fill one: upload into the slot node, delete the placeholder text, keep the chip.

### 6.2 WIP slots phase 2 must replace

All 14 rows of §5.2. When the final render goes in, take " · WIP" off the chip (the chip is the frame at the slot's +16 / +16).

### 6.3 References waiting for Red's download OK (`ref:`)

| Slide | Slot | Slot node | Placeholder text | Candidate |
|---|---|---|---|---|
| DW01 (hidden) | `ref:re8_steam_castle_hall` | `2499:3828` | `2499:3829` | **C1, first in the batch** |
| DW01 (hidden) | `ref:re8_walkthrough_1-36-39` | `2499:3832` | `2499:3833` | **C2, first in the batch** |
| DW07 | `ref:henryford_tivoli_motel_key` | `2503:4487` | `2503:4488` | C8 |
| DW08 | `ref:sears1993_levolor_miniblinds` | `2503:4518` | `2503:4519` | C10 |
| DW08 | `ref:us4463535_glass_stop` | `2503:4522` | `2503:4523` | C11 |

Nothing was downloaded for this section.

### 6.4 Evidence phase 2 may swap (keep the slot, change the image)

- `media:dw02_option_a` → a T1 frame of the real A2 door, same camera.
- `media:dw03_locked_mockup`, `media:dw04_*`, `media:dw12_peek_lite` → T6 frames with the real kit (plus Run and Exit on DW04).
- `media:dw06_dip_*` → T5 frames with the real lock.

### 6.5 `_front` slots: a deliberate deviation

The task brief asked for `img:<Kit>_persp` and `img:<Kit>_front` slots. This section has one `_front` slot (`img:KeyParts_Lineup_front`). `01` §6 routes the front orthographics to 平面视觉's section 2324:852 (PROP KIT · THREE-VIEW + ERA), which this workflow may not edit. After the fix pass, DW05 and DW08 are full: an elevation strip of 8 doors and 4 windows would push the slots back below their grid cells. Equal-scale fronts would be the fairest free/locked comparison, so **the visual chat should ask Red** whether he wants them here (a 14th slide) or only on the three-view section.

## 7. Sources

- In-engine frames, stills and the fix-pass renders: `PM/SOURCES.md` (a "fix pass" section was added).
- The film and tape videos: `Research/week02/ip-research/SOURCES.md` (approved by Red on 2026-10-02).
- Readability tiles and mock-ups: `IMG/05_r*_metrics.json`, harness `../harness/05_*`.
- Audit frames: `../../interaction_audit/images/`.

## 8. Text on the slides (the fix pass, by slide)

Unchanged text is as in `01` §3. The changed strings are in §0 above, and in `01` §3 where the plan was corrected. DW12's texts: header `2534:5882`, title `2534:5883`, lede `2534:5884`, statement `2534:5885`, calls `2534:5886`–`2534:5889`, "Also set" `2534:5894`, rows `2534:5902`–`2534:5908`, chips `2534:5910`, `2534:5913`.

## 9. Contract request: rename `Kit_KeyBoard` (for the interactables workflow and the visual chat)

- **Problem.** On this Mac's APFS volume, file names ignore case (tested: writing `Kit_Keyboard.txt` overwrote `Kit_KeyBoard.txt`). So the key board `Kit_KeyBoard` (`interact_key_board.py:31`, `10_spec.md` §4.2, §9.3, §10.3) and the existing computer keyboard `Kit_Keyboard` (`keyboard.py:50`, in the real project) are **the same FBX and JSON**. A build without `--out-root`, or landing the kit, would overwrite the computer keyboard prop.
- **State at 17:10.** `proj_int/Assets/Resources/Props/Models/Kit_Keyboard.fbx/.json` matched the real project (md5 identical; json name "Kit_Keyboard", one anchor `keys_centre`). So the clone holds **no** key-board model right now. Two `build_asset.py` runs were going against `proj_int` (one started 16:45:02 and lists `interact_key_board`). `g3/hung_solve.json` still uses the key `Kit_KeyBoard`.
- **Request.** Before phase 2 renders the rack:
  1. `interact_key_board.py`: `NAME = "Kit_KeyRack"` (the module file name may stay or become `interact_key_rack.py`).
  2. `10_spec.md` §4.2, §4.3 (host list `{Kit_KeyRack, Kit_KeyCabinet, Kit_KeyHook}`), §9.3, §10.3 and the map contract's `KeySpot` host list: the same rename.
  3. Rebuild G3 into `proj_int`, then restore `Kit_Keyboard.fbx/.json/.meta` there from the real project (in that order, so the restore is not undone).
  4. Check: `Kit_KeyRack.json` has `hook_0`…`hook_7`; `Kit_Keyboard.json` has `keys_centre` and md5 7289…3471.
- The deck already uses "key rack" and `img:KeySet_Rack_persp`.

## 10. Checks run (fix pass)

- **Structure:** 13 frames (12 visible). Every lede is 2 lines (DW10: 1), every statement 1 line, every call body and rule body ≤ 2 lines, every "Also set" row 1 line (read back from node heights). Every chip fits inside its slot (widest 384 px in a 426 slot).
- **Fonts:** new nodes were cloned from P1-styled nodes; no Inter.
- **Overlaps:** none. Section bounds unchanged (8280 × 3880); the hidden DW01 sits below it in empty canvas.
- **Screenshots:** the section at 3000 px and DW03, DW05, DW08, DW09, DW12 at 1600 px after the images went in.
