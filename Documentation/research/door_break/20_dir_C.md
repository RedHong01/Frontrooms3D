# 20 — Direction C · "Too Big for the Door": pre-render build (DB1)

Status: **PRE-RENDER, done 2026-10-08 01:5x.** Red asked for a pure-visual proposal first; nothing lands in the game until he picks. Nothing in the game, the kit modules, the map or Red's project code was changed. The only files this track writes under `Frontrooms3D` are this report, the images in `images/` (`db_C_*`, `dir_C_*`) and two index rows (`VERIFICATION_LOG.md` VL177, `media_candidates.md` §20). In Figma it placed one verification slide, VL177 (frame `2861:6151`), and updated the log's cover. No media was downloaded.

Brief: `10_design.md` §8.0 (common setup) and §8.3 (Direction C). Words (P, S, BURST, RIP, M, D1, D2, FINAL, S4) as in `10_design.md` §1.1. `$W` = `/Users/redwang/FrontRoomsVisualWork`; this direction's work folder is `$W/door_break/C/`.

History: the first C runs (2026-10-03) lived in the old scratchpad and were wiped by the 2026-10-05 reboot. On 2026-10-07 16:3x the scripts were replayed from the transcripts into `$W/door_break/C/`, and iterations 1–5 ran until the usage limit stopped the stage at 20:06. This run (2026-10-07 23:2x → 10-08) continued from that state: iteration 7 (§5), then the finals.

---

## 0. Short answer for Red

- **It reads like a pane cracking round the door.** Every blow pushes the crack network further out from both head corners, and every stage keeps the one before. D1: 4 cracks (2 per corner, 35–55° up, 0.30–0.50 m). D2: 37 cracks, 10.8 m in total on the S face, with the paper torn in bands along them and a lintel crack joining the corners. FINAL: 70 cracks, 13.3 m, and 10 + 8 drywall plates knocked out (44–217 mm). S4: two holes into the stud cavity, torn paper round the whole head, chunks, strips and gypsum dust on both floors.
- **It reads at 12 m.** At V6 (76°, 1080p) the D2 network spans about **146 × 72 px** (2.53 × 1.25 m of wall at 57.6 px/m), and each torn corner zone is 20–45 px across. B's lock hole is 12 px at 3 m. Distance is this direction's claim, and it holds (`db_C_review_distance.jpg`).
- **The hero is the giant, not the lock.** Giant B in the `press` pose fills the 1.0 m opening, its back jammed in the 2.4 m ceiling with the tiles lifted 22–36 mm round it, both forearms on the leaf at 1.56–1.86 m (`dir_C_D2_V4Q-relay-LB.jpg`). Under L-B the S room's light comes through the open cracks as bright specks round the head: the glass read again.
- **M is the weak stage from S.** On the first blows only the dust shows on the S side (a pale sheet down the leaf and puffs at the head corners); the tile jump is on P. In the game the sound and the camera shake carry M.
- **Steel (L0-K) tells the same wall story.** Its leaf takes an 11–18 mm dish at the shoulder print with star-crazed enamel on P (V4 45°). The leaf bows with its kick plate on, and hangs square at S4.
- **The rules hold in every stage.** The visual leaf stays inside its collider band (worst X +0.0725 m, steel FINAL, against the +0.075 m limit; it was +0.0774 before a fix, §5). After the break nothing stands in the opening below 2.1 m (0 violations at S4). At FINAL, with the leaf still shut, two grit crumbs fall in front of the steel leaf inside the 0.12 m check zone; they are in flight and land on the floor.
- **It is the most expensive direction.** The wall and the ceiling are map geometry, so C needs a map contract (§4, items 1–2): the map leaves a head panel and six tiles out round a breakable door, and the visual chat spawns staged replacements. Plus the leaf stage meshes, the baked debris, the dust and a new Relay pose. Budget estimates are in §4.1.
- **My recommendation:** use C's wall network as the **FINAL → S4 layer on the Low ↔ Standard border doors only** (where the giant is squeezed and the 2.4 m ceiling is right above the door), on top of A's jamb split or B's lock hole for D1–D2. A full C on every door is too much map work for what M and D1 show from S.

---

## 1. What was built

### 1.1 Members and the kit they use

The intact doors are the interactables kit, built read-only from the current modules in `Frontrooms3D/Tools/Blender/frontrooms_kit/assets/` (`interact_door_frame_wood`, `_frame_steel`, `_leaf_veneer`, `_leaf_steel`) into `$W/door_break/C/kit/` (`build_kit.py`, 0 failures). Frames and leaves keep their parts separate (casings, linings, stops, slab, hinge halves), so the stages can deform single parts. Lock parts are the G2 FBX from `Assets/Resources/Props/Models/`.

| Member | Leaf | Frame | Lock | Kit tris (intact) |
|---|---|---|---|---|
| **L0-F** (W-W), Level 0 free door | `Kit_DoorLeaf_Veneer` (veneer slab, brass hardware) | `Kit_DoorFrame_Wood` (walnut casings both faces, saddle) | `Kit_Lock_Rose` + `Kit_Lock_Lever` both faces at (±0.022, 1.000, 0.920), `Kit_Lock_Latchbolt_Bored`, `Kit_Lock_StrikeBored` | leaf 2,464, frame 3,152 |
| **L0-K** (S-S), Level 0 key door | `Kit_DoorLeaf_Steel` (almond enamel, kick plate, EMPLOYEES ONLY sign plate) | `Kit_DoorFrame_Steel` (one-piece hollow-metal frame) | escutcheon, knob, IC cylinder, mortise latch and deadbolt, `Kit_Lock_StrikeMortise` | leaf 3,930, frame 3,866 |

Both doors stand on the Low ↔ Standard border: P = Low 2.4 m (the Relay's side), S = Standard 2.9 m (the player's side in BURST).

### 1.2 Stages (render-side concept geometry; every stage contains the earlier damage)

Tier 1 clock: M at the 0.5 and 1.0 s blows, D1 1.5 s, D2 2.0 s, FINAL 2.4 s, the break 2.5 s (`10_design.md` §2.2). All values are ESTIMATES, cumulative.

| Stage | Wall and ceiling (both members) | Frame | Leaf |
|---|---|---|---|
| **S0** | Intact | Intact kit | Intact kit |
| **M** | P: the 6 tiles nearest the door jump **4–16 mm** (the three along the wall 8–16 mm, the one over the door 16 mm), tilted 0.5–1.2°. S: dust curtains through the 3 mm head gap as a pale sheet down the leaf's S face (fading over 0.5 m), with grit streaks, and three puffs out of the head gap. The wall bulges 0.8 mm toward S | — | — |
| **D1** | **Two cracks from each head corner** at 35–55° above horizontal, 0.30–0.50 m long (S: 4 cracks, 1.9 m; P: 4, 1.3 m). Drywall cracks zigzag at the 1–3 cm scale and run straight at the metre scale. Tiles 5–19 mm up. Puffs at both corners. Wall bulge 4 mm | Racked 3 mm (the latch jamb's top out along the opening), head lifted 2 mm, S head casing pried 1.5 mm | **Shoulder print** 0.35 × 0.5 m centred Y 1.6, Z 0.55 on P. Wood: a 4.5 mm crushed dish, the veneer crazed, no hole. Steel: an 11 mm dish with star-crazed enamel. Bow 8 mm |
| **D2** | The primaries run on to **0.6–1.0 m**; four more radials per corner (down beside the jamb, flat, steep, inward over the head); **two rings between neighbouring radials**, each ending on both (the glass's wedge plates); **forks p = 0.3**; the right corner's inward radial ends on the left's, so a **lintel crack joins the corners**. S: 37 cracks, 10.8 m; P: 27, 6.9 m (P stops at the 2.4 m ceiling). **The wallpaper tears in bands along the cracks** within ~0.38 m of each corner, and peels off whole wedge plates near the corner (the drywall's cream face paper and brown kraft show; white gypsum where chips spall). 16 paper flaps curl off the wall. Tiles 22–36 mm up; the tile over the door cracks across and folds 3°. Wall bulge 14 mm | **Racked 8 mm** (parallelogram), head lifted 5 mm. Wood: the S head casing's latch-side **mitre tip breaks off** and turns 4°, the casing pried 6 mm. Steel: the frame **opens 3 mm at the latch-side head mitre** | Bow **30 mm** at the print. Wood dish 6.1 mm; steel 14.9 mm with more crazing and primer flakes |
| **FINAL** | Rings close into plates and break: S 70 cracks / 13.3 m, P 55 / 8.1 m. **Drywall chunks in the air on both sides: 10 on S, 8 on P**, 44–217 mm, 50–350 mm off the wall and falling; gypsum crumbs and a heavy dust burst. The tile over the door splits and its halves are thrown. Bulge 20 mm | Rack 10 mm, lift 7 mm, pry 8 mm; mitre tip at 7° | Bow 36 mm; wood dish 7.2 mm, steel 17.6 mm |
| **S4** | Holes where the plates came out (the stud cavity shows), the torn network round the whole head, 29 curled flaps on the wall; on both floors: the chunks (70 % face up), 22 wallpaper strips, crumbs and a powder fan. On P the split tile lies on the floor; the rest stay 10–24 mm up and tilted | As FINAL | **Thrown open to 80° into S**, square on its hinges. **Creased across at 1.62 m** (a 2° fold plus a 4 mm ridge; on steel a bent fold line, the leaf still square on its hinges), bow 26 mm, so it stays in its band |

**RIP** (one frame, L0-F D2 from P): the Relay pulls from S, so the wall round the frame bulges toward S (away from you), the same corner cracks show on your P face, and **the latch-side stop splits** across at 1.25 m, its upper piece kicked 2 mm out and turned 1.2°. No shoulder print in RIP (the pull bows the leaf only).

### 1.3 The crack graph (the glass rule on the wall plane)

`scripts/crackgen.py` grows one graph per wall face, like `glass/destruction/10_glass_destruction_plan.md`'s crack graph:
- Cracks start at the two head corners of the casing (Z −0.075 and 1.075, Y 2.175).
- Each crack is a chain of short straight runs (8–28 mm) zigzagging round a slowly drifting heading. It ends at its length, at the ceiling line or domain edge, inside the casing zone, or **on an earlier crack**.
- Stages only **add** segments, widen them (the width tapers from the corner to the tip), tear more paper and knock out more plates.
- Plates are the 3 mm cells between cracks (`cells_*.npy`); the FINAL plates that break out become the chunks (`out_*.npy`).
- Maps per face and stage at 1.5 mm: crack core and lip shadow, torn paper / spalled gypsum, gypsum powder, height. The leaf gets its own maps (print crazing, grain splits, steel star-crazing and primer).

In Blender the damage domain (Z −0.95..2.25, from Y 0.9 to the ceiling) is a 3 mm grid displaced by the maps, so the cracks are real gaps with lit lips; outside the domain the wall is the plain static wall. The stud framing (studs, jack and king studs, header, cripples) stands in the 0.135 m cavity, render-only, so the holes show depth.

### 1.4 The set, the lights and the render (§8.0)

- One 0.16 m wall on a cell line, the 1.0 × 2.1 opening centred on a 3 m cell edge. **P** = Low 2.4 m Level 0 with 2 × 4 tiles in a T-bar grid and 0.6 × 1.2 troffers. **S** = Standard 2.9 m Level 0. Each side 6 × 6 m (Z −1..5), plus a 12 m corridor on S for V6. Textures: a snapshot of `Assets/Resources/Surfaces` in `$W/door_break/C/tex/` (Chevron wallpaper, loop-pile carpet, fissured tiles, door veneer, painted metal).
- **L-A:** both rooms steady, 4 troffers each (area 0.6 × 1.2, colour (1, .96, .88), 38 W). **L-B:** the camera's room at 15 %, the far room steady, plus the breach spot (260 W, 1.2 m behind the door on the far side, 1.8 m up) and a thin room haze. Dust is a volume: the head-gap curtain, the corner puffs, the FINAL burst and the S4 settle.
- **Calibration** to audit frame 77 (`../interaction_audit/images/77_relay_broken_witness.png`): frame 77's lit wallpaper measures **0.165–0.175** mean linear luminance (three patches; Direction B quoted 0.183 for two). The finals at exposure −0.9 EV give **0.169** (S0 V1 0.177 / 0.171, D2 V4 0.163 / 0.165): **−0.01 stop** against 0.170 and −0.12 against 0.183, inside ±0.3. (The first four D2 finals came out at −0.1 EV, **+0.6 stop**; they were re-rendered, §5.)
- **Render:** Blender 4.3, Cycles on Metal (M3 Max), 1920 × 1080, 256 samples (adaptive, threshold 0.01), OpenImageDenoise, AgX (the kit preview's view transform), 2 volume bounces. JPG q85.
- **Scale:** a plain 1.80 m figure stands beside the latch jamb in every V6 frame.

### 1.5 The Relay: Giant B `press`

Built with `creatures/giant_b_night_shift.py` (unchanged since 2026-10-02 20:40, so W1 has not changed the body) by `scripts/build_relay_press.py` → `creature/relay_press3.blend` (15,424 tris), `LENS_GLOW` 3.0. A variant of `low` (`press` → `press3`): the spine folded forward, the pelvis at 1.165 m, back and hood jammed into the lifted tiles (**top 2.427 m** with the tiles at D2 lifted 22–36 mm round it; the brief's ≤ 2.37 m is for an un-lifted 2.4 m ceiling), a driving lunge (front foot under the door, back leg straight). **Forearms braced across the leaf** with the wrists at 1.56 and 1.86 m, elbows out past the jambs. It is placed by measuring the leaf's deformed P face and sliding the body until its forearms touch it: 7.6 mm of overlap removed, 3 contact points within 6 mm, nothing inside the leaf.

The coverall slot colour (#2E3B33) is linearised before rendering (the module writes sRGB values into Base Color; without that AgX turns it pale sage), as Direction B did.

---

## 2. Frames

All in `images/`. Names: `db_C_<member>_<case>_<stage>_<view>_<light>.jpg` (`f50` etc. = a FOV other than 76°; `_relay` = Giant B in frame; `_human` = the 1.80 m figure); strips `db_C_strip_<member>_burst_LA.jpg`; key frames again as `dir_C_<stage>_<view>.jpg`. Views as §8.0; **V4Q** = a far-side quarter view (−2.3, 1.62, 2.45) → (0, 1.55, 0.40) at 62°, the hero angle; **V1P** = V1's eye on the P side (RIP).

**Stage strips** (3840 × 720, six tiles cropped from V1 at 76° round the door: Z 0.5 ± 1.55 m, floor to ceiling; the stage and time in each tile):
- `dir_C_strip.jpg`: both members stacked (3840 × 1444).
- `db_C_strip_L0F_burst_LA.jpg`, `db_C_strip_L0K_burst_LA.jpg`.

**Frames** (26 finals, 1920 × 1080; L0-F unless marked):

| Stage | Frames |
|---|---|
| S0, M, D1, FINAL | `db_C_L0F_burst_<stage>_V1_LA.jpg`; `db_C_L0K_burst_<stage>_V1_LA.jpg` |
| D2 | `db_C_L0F_burst_D2_V1_LA`, `_V6_LA_human` (12 m), `_V2f50_LA`, `_V2Hf50_LA` (V2's eye looking up at the head), **`_V4_LA_relay`, `_V4_LB_relay`, `_V4Qf62_LB_relay`** (the hero); `db_C_L0K_burst_D2_V1_LA`, `_V6_LA_human`, `_V4f45_LA` (the steel dish on P) |
| S4 | `db_C_L0F_burst_S4_V1_LA`, `_V6_LA_human`, `_V2f50_LA`, `_V2Hf50_LA`, `_V5f62_LA`; `db_C_L0K_burst_S4_V1_LA`, `_V6_LA_human` |
| RIP | `db_C_L0F_rip_D2_V1P_LA` (from P) |

Key frames (`dir_C_*`, 22): `S0_V1`, `M_V1`, `D1_V1`, `D2_V1`, `FINAL_V1`, `S4_V1`, `D2_V6`, `S4_V6`, `D2_V2`, `D2_V2H`, `S4_V2`, `S4_V2H`, `S4_V5`, `D2_V4-relay-LA`, `D2_V4-relay-LB`, `D2_V4Q-relay-LB`, `STEEL-D2_V1`, `STEEL-D2_V4f45`, `STEEL-D2_V6`, `STEEL-S4_V1`, `STEEL-S4_V6`, `RIP-D2_V1P`.

What each shows, in short:
- **The strip** reads left to right without labels: dust at the head (M), two thin corner cracks (D1), the torn network (D2), bigger with plates out and dust (FINAL), holes and an open door (S4). Steel follows the same wall story.
- **V6 at 12 m:** both torn corner zones and the lintel crack read at D2; the holes and the open leaf at S4. The 1.80 m figure gives the scale.
- **V2H:** the network in depth: the cracks are gaps with lit lips, the paper flaps stand off the wall at the latch corner, grit falls in front of the leaf.
- **Hero (V4, V4Q):** the giant fills the doorway; its back is in the lifted tiles. Under L-B the near room is dim and the S room's light shows through the open cracks round the head as white specks.
- **V5 at S4:** chunks with the print on their face, curled strips, crumbs and a powder fan on the S floor, the split tile on the P floor.
- **RIP (V1P):** the corner cracks and the torn lintel strip on your P face; the stop split is a 2 mm step that reads only up close.

**Review sheets:** `db_C_review_distance.jpg` (V6 at 12 m, both members, D2 and S4, with ×2 pixel loupes of the door), `db_C_review_r7_fixes.jpg` (iteration 7 before / after: the leaf's shading bands, the steel kick plate).

### 2.1 Verification (VL177)

The review images are logged as **VL177 · DB1 · Crack network reads at 12 m** (Figma frame `2861:6151` in FRONTROOMS · VISUAL VERIFICATION LOG; verdict **FLAG**: every stage reads and the network holds at 12 m; from S, M is dust only, and C needs a map contract). Images, in slot order:
- `images/dir_C_strip.jpg` (VL177, `2861:6163`): both stage strips.
- `images/dir_C_D2_V4-relay-LA.jpg` (VL177, `2861:6164`): the hero, Giant B pressed under the 2.4 m ceiling.
- `images/db_C_review_distance.jpg` (VL177, `2861:6165`): V6 at 12 m, both members, D2 and S4, with ×2 loupes.
- `images/db_C_review_r7_fixes.jpg` (VL177, `2861:6166`): iteration 7 before / after.

The log's cover (VL000) was recounted from the canvas: 211 placed checks, 626 images, 24 tasks; DB1 = VL136, 177, 180.

---

## 3. Piece counts and triangles per stage

From `$W/door_break/C/models/stats.json` (`scripts/models_stats.py`). The concept assets are `$W/door_break/C/models/DB_C_<member>_<case>_<stage>.blend` (13 files, 41–49 MB each, compressed), in the door root frame, Blender = (−X, −Z, Y): the door, the frame, both damaged wall faces with the stud framing, the debris and the overlay tiles; no rooms, lamps, dust or creature. "Plates" = drywall plates between cracks per face (S / P); chunks = plates knocked out; flaps / strips = curled wallpaper on the wall / on the floor; crumbs = gypsum crumbs plus the powder cards. Leaf X max = the visual leaf's furthest point toward S in the shut pose (limit +0.075).

| Stage asset | Leaf + lockset tris | Frame tris | Wall domain tris | Plates S / P | Chunks / tris | Flaps + strips / tris | Crumbs / tris | Tiles tris | Total tris | Leaf X max |
|---|---|---|---|---|---|---|---|---|---|---|
| `DB_C_L0F_burst_S0` | 16,344 | 3,826 | 1,869,604 | 1 / 1 | 0 / 0 | 0 / 0 | 0 / 0 | 192 | 1,890,260 | +0.0220 |
| `DB_C_L0F_burst_M` | 97,952 | 3,826 | 1,869,604 | 1 / 1 | 0 / 0 | 0 / 0 | 0 / 0 | 192 | 1,971,868 | +0.0226 |
| `DB_C_L0F_burst_D1` | 97,952 | 3,826 | 1,869,604 | 1 / 1 | 0 / 0 | 0 / 0 | 0 / 0 | 192 | 1,971,868 | +0.0342 |
| `DB_C_L0F_burst_D2` | 97,952 | 3,878 | 1,842,436 | 21 / 19 | 0 / 0 | 16 / 1,728 | 0 / 0 | 204 | 1,946,492 | +0.0636 |
| `DB_C_L0F_burst_FINAL` | 97,952 | 3,878 | 1,788,036 | 30 / 21 | 18 / 88,764 | 29 / 3,132 | 120 / 1,536 | 204 | 1,983,796 | +0.0738 |
| `DB_C_L0F_burst_S4` | 97,952 | 3,878 | 1,788,036 | 30 / 21 | 18 / 88,764 | 51 / 3,732 | 266 / 3,392 | 204 | 1,986,252 | +0.0724 |
| `DB_C_L0F_rip_D2` | 97,952 | 3,906 | 1,841,998 | 20 / 16 | 0 / 0 | 16 / 1,728 | 0 / 0 | 192 | 1,946,070 | +0.0624 |
| `DB_C_L0K_burst_S0` | 33,270 | 4,854 | 1,869,604 | 1 / 1 | 0 / 0 | 0 / 0 | 0 / 0 | 192 | 1,910,502 | +0.0220 |
| `DB_C_L0K_burst_M` | 125,114 | 4,854 | 1,869,604 | 1 / 1 | 0 / 0 | 0 / 0 | 0 / 0 | 192 | 2,002,986 | +0.0226 |
| `DB_C_L0K_burst_D1` | 125,114 | 4,854 | 1,869,604 | 1 / 1 | 0 / 0 | 0 / 0 | 0 / 0 | 192 | 2,002,986 | +0.0371 |
| `DB_C_L0K_burst_D2` | 125,114 | 4,990 | 1,840,878 | 23 / 20 | 0 / 0 | 15 / 1,620 | 0 / 0 | 204 | 1,976,028 | +0.0669 |
| `DB_C_L0K_burst_FINAL` | 125,114 | 4,990 | 1,795,328 | 32 / 17 | 19 / 69,248 | 29 / 3,132 | 120 / 1,560 | 204 | 2,002,798 | +0.0725 |
| `DB_C_L0K_burst_S4` | 125,114 | 4,990 | 1,795,328 | 32 / 17 | 19 / 69,248 | 51 / 3,732 | 266 / 3,444 | 204 | 2,005,282 | +0.0724 |

Notes:
- The leaf jumps from 16k (S0, the kit) to 98k (wood) and 125k (steel) at M because the concept cuts the slab on a 12 mm grid (4 mm round the print) so it can bow smoothly; the lock parts are the G2 FBX (lever 4.6k, rose 2.7k each).
- The wall domain is a displaced 3 mm grid on both faces (1.8–1.87M tris): render-only. It is the reason a stage file is ~45 MB.
- The 18–19 chunks are solid 13 mm board pieces with their crack outlines (69–89k tris together).
- Steel: the sign plate, number plate and their screws ("other", 3k tris) ride on the leaf.

**Against the budgets (`10_design.md` §6):** everything in the concept is far over (it was built for 1080p renders, not for the game). The game version (§4) needs about 2.5–3.5k tris per leaf stage, 2.5–4k per wall panel face, ≤ 30 debris pieces in flight per side and one floor mesh ≤ 3k tris.

---

## 4. How it would be built for real

Nothing here lands until Red picks. If C (or C's wall layer) is picked:

1. **The wall head panel (map contract).** The wall is map geometry (`FrontRoomsMapWorld`), and an overlay cannot show a hole through it. Proposal for the map chat (a CONTRACT request, not an edit): for every door the Relay can break on a Low ↔ Standard border, the chunk builder leaves the wall faces out over Z −1.0..2.0 (door root), Y 1.6 m to the ceiling on both sides, and spawns the visual chat's `Kit_WallHeadPanel_<S|P>` there. The panel's intact stage must match the map wall exactly: same `FrontRooms/Surface` material, the same world-projected metre UVs (`_TileSize`), the same wear, so the swap is invisible. Its stages `_D1 / _D2 / _Broken / _S4` are **pre-fractured** in Blender from `crackgen.py`'s graph (one graph per door type, or 3–4 seeds chosen by the door's edge hash), with the plates as separate shells (13 mm board, kraft and gypsum edges) and the cracks as real gaps.
2. **The ceiling tiles (map contract).** The same for the six 2 × 4 tiles next to the door on P: the map leaves them out and the door record spawns `Kit_CeilingTile_2x4` instances and a short T-bar patch. The tile jump is a transform on each blow (lift 10–46 mm, tilt 0.5–4°, settle in 0.25 s); the tile over the door has a pre-split `_Cracked` and `_Halves` version. The plenum behind needs a dark card.
3. **Leaf stage meshes.** `Kit_DoorLeaf_Veneer_C_D1 / _D2 / _Broken` and `Kit_DoorLeaf_Steel_C_D1 / _D2 / _Broken`, with the bow, the print dish and (wood S4) the crease baked into the vertices. The bow needs a grid on the faces: about 16 × 32 quads is enough for a smooth 30 mm bow (≈ 2k tris on the two faces), not the 80k-tri concept grid. Outside the deformed area the vertices stay identical to the intact leaf. **Clear the kit's custom normals on the cut faces** (they smeared into tone bands here, §5). Flat add-ons (kick plate, sign plate) need the same grid or they sink into the bowed slab (§5). All within the leaf's ±0.05 m collider band.
4. **Frame.** Rack, head lift and casing pry are transforms or small stage meshes of the separate frame parts; the wood mitre tip is a pre-cut piece; the steel frame's open mitre is a `_D2` stage of the jamb.
5. **Decals (marks).** Hairlines beyond the panel's real gaps, the torn-paper edges and the leaf crazing go into the shared `Door_DamageMarks` atlas (≤ 4 quads per blow, 1 draw, no URP decal feature), as `10_design.md` §4.2.
6. **Debris, baked** (RE7 style, §4.3): the 10 + 8 knocked plates, 22 wallpaper strips and the split tile as transform-keyed throws (≤ 1.5 s), then merged into one floor mesh per side; crumbs and powder as one alpha floor card per side.
7. **Particles** (if the particle module is approved): the head-gap curtain on every blow (a falling sheet hugging the leaf, 0.5 m fade), puffs at the corners from D1, the big burst at FINAL. Without the module: two camera-facing flipbook cards per blow.
8. **The Relay rig beat.** A new `press` pose (back into the ceiling, forearms on the leaf) held through the blows, with the chest snap on `DoorBlow`. The tile jump must sit on the same frame.
9. **Gameplay is untouched.** The collider (0.05 × 2.08 × 0.98 under `Door hinge {a}-{b}`) and its name stay; every new piece is render-only; after the break nothing stands in the opening below 2.1 m except the leaf at 80°.

### 4.1 Costs (ESTIMATES; desktop is the reference, WebGL a separate tier)

| Item | Desktop | WebGL |
|---|---|---|
| Wall head panel per face | intact ≈ 0.1k; D1 ≈ 0.6k; D2 ≈ 2.5–3.5k (20–25 plates with edges); Broken/S4 ≈ 4k | the intact panel + marks only; holes as dark cards |
| Ceiling overlay | 6 tiles + T-bar patch ≈ 0.3k, 1 draw | same |
| Leaf stage meshes | ≈ 2.5k (D1) / 3k (D2) / 3.5k (Broken) veneer; steel +1k | LOD1 ≤ 1.5k |
| Debris | ≤ 30 pieces in flight per side for ≤ 1.5 s; floor mesh ≤ 3k tris | ≤ 12 pieces, 1 draw |
| Marks | ≤ 24 quads, 1 draw (shared atlas) | same |
| Draws per broken door | +5 (2 panels, tiles, floor debris, marks) | +3 |
| Memory | ≤ 2 MB of stage meshes per door type (panels + leaf), shared | half |
| Map work | the contract in items 1–2 (chunk builder + door record) | same |
| Rig | 1 new pose (`press`) + tile-jump sync | same |

C costs more than A and B: two new map holes per door, a new asset family (wall panels) and the ceiling overlay.

---

## 5. Iterations (what I looked at and fixed)

Every frame was opened and judged. Quick passes are 960 × 540 at 64 samples; sheets and crops in `$W/door_break/C/look/` and `review/`.

| Pass | Found | Fix |
|---|---|---|
| it. 1 (2026-10-07 17:1x) | Cracks read as thin scratches; the paper did not tear; the Relay floated in front of the leaf | Crack widths tapered and doubled near the corners; one-sided ragged tears with face paper / kraft / gypsum layers; the Relay placed by measured contact |
| it. 2 (18:2x) | Cracks wandered like roots; stages not cumulative on the leaf | Zigzag runs about a straight metre-scale heading; every mark has a birth stage and only widens |
| it. 3 (18:5x–19:4x) | A hairline seam at the damage domain's edge (V5); the Relay's back below the tiles; peel zones too small for 12 m | The seam traced (ray probes, `review/probe/`) to the solidify rims meeting at the face: the static board laps the domain rimless; `press3` pose with the back in the lifted tiles; peel zones widened |
| it. 4–5 (19:4x–20:1x) | Two corner blotches at 12 m, no single torn zone; S4 leaf 0.2 mm outside its band (+0.0752); the Metal kernel compiler stalled 20–40 min with several jobs | Torn bands along the cracks within a ragged radius per corner and a lintel strip at FINAL; S4 twist 0.016 → 0.009; kernel specialisation off for parallel previews, on (FULL) for the single-queue finals |
| **it. 7 (this run, 23:3x)** | **The leaf went darker with tone steps at Y ≈ 0.6 and 1.1 m from M on**, even with every volume hidden (`db_C_review_r7_fixes.jpg`). Cause: the kit slab's custom split normals, corrupted by the dissolve + bisect grid | Custom normals cleared after the cut; sharp edges re-derived. The M leaf now matches S0 outside the dust |
| | The dust darkened the leaf instead of lighting it (single scattering only); the D2 curtain read as dark strings | 2 volume bounces; softer streaks with a 0.3 base; per-material fine volume step (no tone steps); M curtain ×1.4 and three head-gap puffs |
| | **The steel kick plate sank into the bowed slab** from D1 (only its corners have vertices) | Every leaf part wider or taller than 8 cm cut on a 2 cm grid before the bow |
| | S4 leaf +0.0752 m (still 0.2 mm out) | S4 bow 30 → 26 mm: +0.0724 m |
| finals (23:45 →) | The first four D2 finals were **+0.6 stop** against frame 77 (exposure −0.1); the coverall pale sage | Exposure −0.9 EV (measured 0.174 vs 0.165–0.183); creature colours linearised; D2 re-rendered |

Finals: every final was opened (full frames for the hero, the strips and V2H; sheets and 1:1 crops for the rest). Two late fixes, both re-rendered: the steel FINAL leaf at **+0.0774** (2.4 mm out of its band; the deeper steel dish adds 6 mm on S) → bow held at 31 mm, now **+0.0725**; and the D2 set at the calibrated exposure with the linearised coverall. Render times on the M3 Max: 50 s (S0) to 6.6 min (D2 V1 with the dust volumes) per frame; one steel frame took 33 min while other workflows held the GPU.

---

## 6. Open issues

1. **Map contract needed** (§4 items 1–2): the head panel and the six tiles. Without it, C cannot show holes or a tile jump in the game; overlays alone would give the cracks and torn paper only.
2. **M from S is dust only.** The tile jump is on the Relay's side. If M must read from S, a 1–2 mm leaf jolt with a light pulse along the head gap (Direction A/B's leak cards) is the cheapest add.
3. **Physics honesty.** Diagonal cracks from frame corners are real for frames in stud walls (`02_real_door_failure.md` §2.3); a network this big and torn paper this wide are not 1990 physics: they are the giant's scale. Photos of real corner cracks and lifted lay-in tiles are listed (`media_candidates.md` §20, `dc01`–`dc04`); none are downloaded.
4. **Relay top 2.427 m** (above the brief's ≤ 2.37 m): the back is pressed into the lifted tiles on purpose. If the game's `low` must stay under 2.37 m, the tile lift sells the press without the body crossing the ceiling line.
5. **Concept counts are far over the game budget** (the wall damage domain is a 3 mm displaced grid, ~1.8M tris; the leaf 80k): the game versions are specified in §4.
6. **The pre-split tile over the door, the stud framing and the plenum** are render-only additions; the map has no studs or plenum today.
7. **Lead references are pending Red's approval:** RE7 Jack bursting through the wall (`db03`) and the Lady Dimitrescu doorway duck are listed in `media_candidates.md` §01 (not downloaded). The on-disk Giant B renders named in the brief (`creature_prev/giant_b/Giant_B_NightShift_door_a.png`, `_low_a.png`) were lost with the old scratchpad on 2026-10-05; this direction's own V4 / V4Q frames now show Giant B at the door. On disk and usable: `../interaction_audit/images/70_relay_break_witness_t1000ms.png` (today's Relay at a door) and `77_relay_broken_witness.png` (the exposure target).
8. **Era check:** every part is the 1990 kit (bored lever lockset, mortise lock, enamel hollow-metal door, EMPLOYEES ONLY sign plate, lay-in tiles and T-bar). No brands, no maker marks, nothing printed after 1990. The scale figure is a plain dark mannequin.
9. **Tools:** the crack generator runs on an existing local Python with scipy (ComfyUI's venv, read-only; nothing installed).
10. **Main moved** (Codex 2026-10-03, then about 30 commits since d610d3a). This direction writes nothing into the game, so nothing was merged. It imports the kit modules and the creature module read-only from main. `codex_audit/20_findings.md` does not exist; `00_main_state.md` and the `10_review_*.md` files list no finding for door-break.

---

## 7. Reproduce

```
W=/Users/redwang/FrontRoomsVisualWork; C=$W/door_break/C
B=/Applications/Blender.app/Contents/MacOS/Blender
$B -b --factory-startup --python $C/scripts/build_kit.py                 # intact kit -> kit/*.blend
$B -b --factory-startup --python $C/scripts/build_relay_press.py -- press3 human
cd $C/scripts && for m in "L0F burst" "L0K burst" "L0F rip"; do $C/py crackgen.py $m; done   # maps/
zsh $C/scripts/final.sh "L0F burst D2 V1:76:LA,V6:76:LA:human,V4:76:LA:relay,V4:76:LB:relay,V4Q:62:LB:relay,V2:50:LA" ...
$B -b --factory-startup --python $C/scripts/models_stats.py              # models/stats.json
$C/py $C/scripts/publish.py strips keys distance fixes                    # strips, dir_C_*, review sheets
```

`scene.py -- --member <L0F|L0K> --case <burst|rip> --stage <S0..S4> --jobs <view:fov:light[:relay|human]>,... --res 1920x1080 --spp 256 --models <dir>` builds one stage, renders the views and saves the stage model. Every final writes `renders/final/stats_<member>_<case>_<stage>.json` (leaf band, opening check, plate and debris counts, Relay contact).
