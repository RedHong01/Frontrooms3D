# 72 · Floor-station hero kits: the six BS4 stand-ins (游戏视觉, 2026-11-03)

**What this is.** The build doc of 游戏视觉's workflow `visual-bs4-hero-kits` (session local_1fc9f09c, account G, started 2026-11-03 17:20): the six kits that BS4 stood in with existing kits when it landed the two floor-station modules (`Station_Waiting_3x2`, `Station_Desk_3x3`, LANDINGS 10-31 11:56). One section per workflow stage; this text is the **Plan stage**. §8 is the stage log; the Kits, Stations and Package stages append to it.

- **The ask.** Queued when BS4 landed: "stand-ins for tandem beam / magazine rack / smoking stand / guest chair sled / counter ledge / credenza (hero kit pass later = queue)" (`handoff/archive/visual_history.md`, 10-31 11:57), held for quota at 13:22. Red's bar for every visual task: "在做视觉的时候，我希望你构思都以最高规格的去进行构思和落地……还有模型的制作。" (10-03, VISUAL_STANDARD §1.1) and "任何视觉相关的我需要你给我保证最高的推理量" (11-01, RULES 6). Account G resumed the paused work under Red's 11-03 words as recorded in `handoff/visual.md` §1 ("我刚切换成了ultra 的计划，我希望你能够全量继续刚刚另一个账号暂停的所有子工作"); they are quoted from that file and were not re-checked in a transcript here.
- **Done in this stage:** the inventory, the period design and parts lists, budgets, the module swap checked against a Python mirror of the module rules, and the builder split. **Not done:** no Unity, no Blender, no download, nothing landed. Main was read only.
- **Reused, not redone:** `01_research.md` §1, §2 and §6; `03_palette.md` (叙事's E15 verdicts) and its full notes; `30_look.md` rows S04, S05, S08, S09, S25 and S29, §2 and §5; `70` §12 and `71` §2.2 (kit rules, the LOD lesson); the blockout envelopes in `W/station_work/FrontRoomsStationLookdev_r6.cs.txt`; `SOURCES.md` §E and §P. No new source was read. **[memory]** marks typology that is unverified recall.
- **Base sha1 of every file read for the plan:**
  - `Station_Waiting_3x2.asset` 02932259 (the BS4 payload);
  - `Station_Desk_3x3.asset` 91c5a1ce (BS5's 12:44 edit moved the visitor chairs and the cabinets 3 cm off the crash rail);
  - `FrontRoomsRoomModuleData.cs` 1200a918, `FrontRoomsStationSet.cs` b4d25030, `FrontRoomsKitImporter.cs` e0191ea7;
  - `kitlib.py` d6de07ab, `build_asset.py` cd0ef1ee, `_station_common.py` 49dd683f, `station_counter.py` 384eb4ca.
  - The clone `W/proj_bstations` holds the same bytes (FRSYNC 11-02 10:57).
  - Read in the plan's second pass (18:04–18:50, after the first pass's agent died): `task_chair.py` f55d64cc, `_newcase.py` a2dfcc89, `outlet_common.py` db0ca46e, `credenza.py` 4df7da02, `office_desk.py` efcb5289, `interact_key_board.py` 2392aa17, `FrontRoomsStationTrim.cs` d618387a, `03_palette.md` and its full notes `~/FrontRoomsNarrativeWork/station/D_palette.md` §1, §3. All equal in the clone.
- **Scripts:** `W/bs4_hero/tmp/inventory.py` (the modules as built, with each prop's box) and `W/bs4_hero/tmp/plan_check.py` (the planned swap against the module rules).
- **Why a new file:** `progress.py docs find` for "hero kits", "tandem beam seating" and "station kit build" (11-03 18:2x) found no doc on these kits. `70`/`71` are the Floor 1 station's plan and build log; this file extends the same topic folder with the next build.

## 0. Short answer

| # | Kit (names) | Module, where | Replaces | LOD0 / LOD1 / LOD2 target (cap) | `30_look` budget | Slots (LOD0 draws) | Builder |
|---|---|---|---|---|---|---|---|
| 1 | `Kit_TandemBeam3` and `Kit_TandemBeam2Table` (+ `_Navy`, `_Charcoal` variants) | Waiting: row A (3 seats, faces E) and row B (2 seats + beam table, S wall) | P27–P29 and P30–P31, five `Kit_TaskChair_Navy` | 9,000 / 3,000 / 600 (10,500); 8,000 / 2,700 / 500 (9,500) | S08: 6,000 / 2,000 / 250; table 2,000 / 700 / 100 | 3; 6 | A |
| 2 | `Kit_SmokingStand` + `Kit_SignNoSmoking` | Waiting: S wall, x 4.60 | nothing (absent in BS4) | 3,000 / 1,000 / 150 (4,000); sign 200 / 60 / 12 | S25: 2,500 / 800 / 100 | 5; 2 | A |
| 3 | `Kit_MagazineRack` | Waiting: N wall, x 2.10, 0.98–1.64 m | nothing (absent) | 3,000 / 1,000 / 150 (3,500) | S29: 3,000 / 1,000 / 120 | 3 | A |
| 4 | `Kit_GuestChairSled` (+ `_Burgundy`) | Desk: W wall, the two visitor chairs | P23–P24, two `Kit_TaskChair_Navy` | 5,000 / 1,700 / 350 (5,300) | S09: 3,500 / 1,200 / 150 | 3 | B |
| 5 | `Kit_StationCounter_Ledge120` | Desk: W wall middle cell, with the register and tent card | nothing (absent) | 3,000 / 1,200 / 150 (3,500) | S05: 1,800 / 600 / 80 | 4 | B |
| 6 | `Kit_Credenza_Laminate` | Desk: N wall under the key cabinet | nothing (absent since the set went on the bracket) | 3,500 / 1,300 / 200 (5,300) | S04: 1,108 / 738 (a slot swap) | 4 | B |

- **Why above `30_look`'s budgets:** those were set before the hero bar (VISUAL_STANDARD §1.1: real hardware, bevels, holds up at 0.35 m). The single-prop hero bar is about 5,300 (§1.4). A 3-seat beam is three seat positions on one frame, 3,000 a position, where the stand-in chairs spend 5,168 each. Every kit stays at or under its cap.
- **Station LOD0** (stand-ins out, targets in): Waiting **85,934 → 83,294**; Desk **69,606 → 77,264**. Both stay under the 100k station cap (`30_look` §1.5, `70` §11).
- **New textures:** one 1024² cover atlas, about 1.3 MiB with mips (placeholder art until 平面视觉's covers). Every other new material reuses maps already in `Resources/Surfaces`.
- **No inspect block anywhere:** the stand-ins (`Kit_TaskChair_Navy`) carry none, and none of the six is interactable. The register on the ledge keeps its own `inspect_note`.
- **Markers, lamps, edges and arch widths do not move.** Each replaced kit takes its stand-in's place and stays inside its envelope.
- **The take frame (C3) shows none of the new kits** in either module.

## 1. Inventory: every instance, today and planned

**Cameras** (from `W/vl_pending/bs4_station_modules.md` and `bs5_station_floors.md`; eye 1.62 m, 76° vertical FOV, 16:9, so 108° across):

| Camera | Where |
|---|---|
| C1 | E1's outside cell, 4.5 m out on E1's axis |
| C2 | Just inside E1 (0.5 m in), looking along the set's axis |
| C3 | The take, 0.8 m from the key plate |
| C4 | Just inside E2, looking back into the room |

Angles in the tables below are left (+) or right (−) of the view axis. LOD is the level the planned `lodDistances` give at that distance.

### 1.1 Station · Waiting (`Station_Waiting_3x2`, Level 0, 9 × 6 m)

**Fixed in the module:**
- E1: W wall cell 2, arch 1.70 m centred at z 4.80. E2: S wall cell 3, arch 1.20 m.
- Entry strips (Validate): x 0–1.08 × z 3–6, and x 6–9 × z 0–1.08.
- TV push-in: x ≈ 7.0–8.3, z 4.3–5.3, below 2.46 m.
- Lamps `010505010506`: the W column Steady, cell (2,1) Off.

| P | Today | x / z / y, yaw | Coll. | Box today (module m) | Planned hero | Hero box (envelope) | Seen from |
|---|---|---|---|---|---|---|---|
| P27–P29 | `Kit_TaskChair_Navy` ×3 (5,168 tris each) | 3.60 / 1.50, 2.15, 2.80 / 0, yaw 90 | yes | x 3.24–3.90, z 1.19–3.11, top 0.93 | `Kit_TandemBeam3` at 3.60 / 2.15 / 0, yaw 90 (seats at z 2.80 / 2.15 / 1.50) | x 3.28–3.90, z 1.15–3.15, top 0.84 | C2: 41° right, 4.1 m, LOD0, the rows' backs. C4: 38° left, 4.2 m. Not in C1 (behind the jambs) or C3 |
| P30–P31 | `Kit_TaskChair_Navy` ×2 | 1.20, 1.85 / 0.50 / 0, yaw 0 | yes | x 0.89–2.16, z 0.14–0.80 | `Kit_TandemBeam2Table` at 1.20 / 0.50 / 0, yaw 0 (table at x 0.55, seats at 1.20 / 1.85) | x 0.20–2.21, z 0.18–0.80, top 0.84 | Out of frame in both (the frame is ±54° across): C2 81° right, 4.4 m, seen on turning right; C4 61° left, 6.3 m |
| — | smoking stand: W′ "a" (`30_look` §5.1), not placed in BS4 | — | — | — | `Kit_SmokingStand` at 4.60 / 0.42 / 0, yaw 0 | x 4.43–4.76, z 0.26–0.58, top 0.62 | C2: 47° right, 6.0 m, LOD1, at the frame's right edge. C4: 64° left, 2.9 m, out of frame (seen on turning left, LOD0) |
| — | its sign (01 §6 `NO SMOKING`) | — | — | — | `Kit_SignNoSmoking` at 4.60 / 0.08 / 0, yaw 0 (plate centre 1.55 m) | x 4.47–4.73, on the S wall | as the stand, 0.34 m behind it |
| — | magazine rack: W′ "m"; the rev 3 blockout had it on the W wall at z 1.50 | — | — | — | `Kit_MagazineRack` at 2.10 / 5.92 / 0, yaw 180 (pockets 0.98–1.64 m) | x 1.80–2.41, z 5.81–5.92 | C1: through E1's left edge, 9° left, 6.7 m, LOD1. C2: 34° left, 1.9 m, LOD0, the first thing on the left inside E1 |

**Unchanged:** P01–P26; P32 `Kit_TrashBin` (2.60 / 0.25), P33 `Kit_HatStand` (0.40 / 2.55), P34 `Kit_WallClock` (W wall z 1.70, y 2.20).

**Markers unchanged:**
- KeySpot 7.13 / 5.852 / 1.4215, yaw 180, host `Kit_KeySwitchPlate`;
- StationBeacon 8.316 / 4.80 / 2.461, yaw 270;
- Placard 5.00 / 5.92 / 1.56;
- StationArch 0 / 4.80.

### 1.2 Station · Desk (`Station_Desk_3x3`, Office, 9 × 9 m)

**Fixed in the module:**
- E1: S wall cell 3, arch 1.70 m centred at x 7.80. E2: E wall cell 2, arch 1.20 m.
- Entry strips: x 6–9 × z 0–1.08, and x 7.92–9 × z 3–6.
- TV push-in: x 7.3–8.3, z ≈ 7.0–8.3.
- Lamps `050101050505050506`: the whole W column is Dim.

| P | Today | x / z / y, yaw | Coll. | Box today | Planned hero | Hero box | Seen from |
|---|---|---|---|---|---|---|---|
| P23–P24 | `Kit_TaskChair_Navy` ×2 | 0.49 / 1.70, 2.40 / 0, yaw 90 | yes | x 0.13–0.79, z 1.39–2.71, top 0.93 | `Kit_GuestChairSled` ×2 at the same positions | x 0.19–0.76, z 1.41–1.99 and 2.11–2.69, top 0.84 (backs 8 cm off the rail face) | C4: 15–19° left, 8.2–8.4 m, LOD1. Out of frame in C2 (75–81° left) |
| — | sign-in ledge: D′ rev 4 "the ledge in the W wall's middle"; BS4 placed none | — | — | — | `Kit_StationCounter_Ledge120` at 0.08 / 4.50 / 0, yaw 90 (origin on the wall face) | x 0.08–0.41, z 3.90–5.10, top 1.07 | C4: straight ahead, 8.2 m, LOD1 |
| — | the register and tent card on it (the existing kits, as in Waiting) | — | — | — | `Kit_RegisterBook` 0.245 / 4.72 / 1.07, yaw 90; `Kit_TentCard` 0.28 / 4.14 / 1.07, yaw 90 (both no collider) | on the ledge top | read from the visitor's side (E) |
| — | credenza: under the set in D′; dropped when BS4 hung the set on the bracket | — | — | — | `Kit_Credenza_Laminate` at 6.36 / 8.651 / 0, yaw 180 | x 5.60–7.12, z 8.40–8.90, top 0.741 | C1: 6° left, 13.2 m. C2: 10° left, 8.3 m, LOD1, over the desk and behind the jacket, under the key cabinet. C3: behind the player |

**Unchanged:** P01–P22; P25 `Kit_NoticeBoard`; P26–P27 `Kit_FilingCabinet`; P28 `Kit_HatStand`; P29 `Kit_WallClock`.

**Markers unchanged:**
- KeySpot 8.852 / 7.13 / 1.4215, yaw 270;
- Beacon 7.80 / 8.316 / 2.461, yaw 180;
- Placard 4.90 / 8.92 / 1.56;
- StationArch 7.80 / 0.

### 1.3 Rules every placement keeps (checked, `tmp/plan_check_out.txt`)

**The module rules:**
- inside the walls (box ≥ 0.07 from the wall lines);
- under 2.85 m;
- no collider in an entry strip;
- no collider in the screen's or the beacon's push-in;
- `Walk` with the 0.3 m capsule: every opening seeded, every cell reached;
- no collider in E1's arch span or E2's cell × 1.08 m (`StationSet.BuildFloor`, so `PropsLeftOutCount` stays 0).

**Result:** 0 errors, 0 warnings, walk OK, in both modules, today and planned.

**Station trim (BS5) clearances:**
- crash rail 0.78–0.93 m, 28 mm proud; base 0–0.10 m, 6 mm proud.
- The trim runs behind every prop: `FrontRoomsStationTrim` (d618387a) cuts it only at E1 and E2. A kit against a wall therefore stands clear of it or is scribed round it (the ledge, §2.5).
- Every floor kit stands ≥ 0.03 m off the rail face, or passes under it (credenza top 0.741 < 0.78; its back 15 mm off the wall clears the 6 mm base).
- Wall kits mount above it (the rack's bottom at 0.98 m).

**Envelopes are hard limits for the builders:** footprint within ±0.02 m, height not above the envelope.

## 2. The kits

**Every kit:**
- era 1988–91, nothing after 1993, no brand, no lettering of ours (type is 平面视觉's);
- no red in the station palette (`30_look` §2);
- authored with the house machinery (`kitlib.Kit`, `_station_common` Unity-space helpers, `build_asset.py`).

**Hero bar** (VISUAL_STANDARD §1.1: holds up at 0.3 m):
- real hardware geometry;
- a bevel on every visible hard edge (≥ 1 mm; laminate 1.5 mm);
- the G1 weighted-normal recipe on hard parts (`outlet_common.finish_part`: smooth, sharp by angle, `WEIGHTED_NORMAL` face area 50, keep sharp);
- tube facets invisible at 0.35 m: ≥ 16 sides for tubes ≥ 19 mm, ≥ 12 for rods ≥ 6 mm, 8 for wire ≤ 4 mm; bends sampled every 15°.

**LODs are hand-built part sets** (`obj["fr_lods"]`, P-1b), never a collapse decimation of hard surfaces (`71` §2.2: the collapse folded the counter's boxes into triangles):
- LOD1 keeps the silhouette, with fewer sides and no sub-5 cm hardware;
- LOD2 is boxes and 6-sided tubes.

**Sidecar:**
- no pile block (`_station_common.no_pile`, check C2-4; the stand-in chairs had one);
- a `station` block from `station_meta`, with its `line` then set to "游戏视觉 bs4-hero-kits" (the helper writes station-look-v1);
- `lodDistances` and `budget` from `_station_common.lods`;
- no `inspect` block (§0).

**Slots:** at most 4 per kit (kitlib's rule) except where a period part needs its own finish; the two exceptions (the beam table, the stand) are counted in §5.

### 2.1 Tandem beam seating: `Kit_TandemBeam3`, `Kit_TandemBeam2Table` (builder A)

**Period** (`03_palette` B rows and its full notes; `SOURCES` §E):

| Source | What it shows |
|---|---|
| E02, *Contract* Sep 1987 | Beam seating with black urethane arms and base, 2–5-seat units, optional beam-mounted tables, recoverable upholstery |
| E12, Dec 1986 | Beam-mounted tandem systems with optional arms, plastic-laminate beam tables and an ashtray |
| E11, Jan 1992 | Polypropylene shells with upholstered cushions; 2–4-seat beams; tables in place of seats; arms clamped to the beam |
| E13, May 1991 | Steel beams in chrome, epoxy or metallic |
| E45, E46 | Older fibreglass tandem on a black steel beam |

- **Verdict** (叙事's E15): steel CONFIRMED, finish chrome or black; seats slate blue or charcoal, with navy as a variant. Its full notes (`D_palette.md` §1) say navy "is a period colour, but it is **not attested on beam seating**", and a 1986–89 refit takes "slate blue, charcoal or dark grey".
- **Colour (changed in the plan's second pass):** the kits are authored in **slate** (`Prop_VinylSlate`), with `_Navy` (today's stand-in colour) and `_Charcoal` as variants for the sheet (§7). The first pass had navy as the base. Slate follows the evidence, and it leaves the guard's navy jacket (P26, the false-occupancy silhouette) as the only navy mass in the room.
- **Name:** `30_look` S08 planned a separate `Kit_TandemBeamTable` (a beam end table). Here the table takes a seat's place on the beam, as the period systems did ("tables in place of seats", E11; "beam tables", E12). So row B is one kit, `Kit_TandemBeam2Table`, with one beam and one collider.
- **Layout** (`20_layout` G10, `30_look` §10.3): two short rows, never a showroom; one row faces the set, the other runs along the S wall with the beam table.
- **Form:** "a 1986–89 refit":
  - a black epoxy steel beam on two T-legs;
  - black urethane arm caps;
  - fully upholstered recoverable vinyl seat and back pads on black polypropylene shells;
  - row B with a laminate beam table and a built-in ashtray in place of its W seat.

**Dimensions** (seat pitch from the stand-ins; the rest ESTIMATE / [memory] for 1980s public seating):

| Part | Inches | Metres |
|---|---|---|
| Seat pitch | 25.6 | 0.65 (the stand-ins' own spacing) |
| Overall L × D × H | 79.1 × 24.4 × 33 | 2.01 × 0.62 × 0.84 |
| Seat top (front, uncompressed) | 17.5 | 0.445 |
| Seat cushion W × D × T | 19.7 × 18.5 × 3 | 0.50 × 0.47 × 0.075 |
| Back pad W × H × T, bottom at 0.49 m, 12° recline (top 0.83 with its shell) | 19.7 × 13.4 × 2.5 | 0.50 × 0.34 × 0.064 |
| Arm cap top | 25 | 0.635 |
| Beam (rectangular tube, 76 mm tall), centre at 0.29 m, z −0.03 | 2 × 3 | 0.051 × 0.076 |
| Beam table top, top at 0.47 m (18.5 in) | 19.7 × 18 × 1.125 | 0.50 × 0.457 × 0.029 |

**Stack-up at a seat (fixed in the second pass: the first pass's beam at 0.34 ran through the seat pan, and its 0.38 m back rose above the 0.84 envelope):**
- beam top 0.328;
- bracket flat bar 0.328–0.334, then an 11 mm boss;
- PP pan 0.345–0.357;
- cushion 0.357–0.432 (75 mm boxing) plus the 8 mm crown, so the front reads 0.44–0.445;
- back pad 0.49–0.82, its shell lip to 0.83, under the 0.84 envelope.

**Frame:** origin on the floor under the middle position's centre. Front +Z (where people face). Positions at x −0.65 / 0 / +0.65 (the table at −0.65 in the 2Table kit). Footprint x ±1.005, z −0.32…+0.30.

**Parts, down to the hardware:**
- **Beam:** 51 × 76 mm tube, 4 mm corner radius, 2.00 m long, black PP end plugs (2 mm flange, recessed face).
- **T-legs** at x ±0.325, under the inner arms, in both kits (second pass: legs at ±0.65 sat on the outer seats' brackets; the beam now runs 0.675 m past each leg, as beam seating does):
  - a 51 mm square upright with 3 mm corner radius, from the foot to the beam's underside (0.252);
  - a 25 × 51 mm floor foot 0.60 m long, z −0.30…+0.30, with end caps;
  - two triangular 6 mm gussets per leg with 2 mm weld beads;
  - two levelling glides per foot (Ø 38 × 12 mm black nylon, chamfered, a 4 mm steel stem showing);
  - a 6 mm U-saddle clamping the beam, with 2 hex bolts (17 mm A/F heads, chamfered) and nuts. The inner arm post rises from the same saddle.
- **Seat brackets** (one per position):
  - a C-clamp round the beam (2 bolt heads each side);
  - a 50 × 6 mm flat bar forward under the seat pan;
  - a 38 × 9.5 mm back upright bent up behind the seat (r 25 mm, 12° recline), bolted to the back shell (2 hex heads).
- **Arm posts and caps:**
  - posts are 38 × 9.5 mm flat bar: the inner ones from the leg saddles, the outer ones from a beam clamp (2 bolt heads) at ±0.975;
  - caps are black urethane, 50 W × 45 H × 380 L mm, top at 0.635, with a rounded nose (r 22), a 2 mm crown, all edges r 3–5 and the under-channel over the post.
  - Tandem3: 4 arms at x ±0.325, ±0.975. 2Table: 3 arms at −0.325, +0.325, +0.975 (no arm at the table's outer end).
- **Seat cushion:**
  - top crowned 8 mm;
  - waterfall front (r 30 mm, rolling 40 mm down);
  - 75 mm boxing;
  - welt cord Ø 6 mm on the side and back top seams;
  - the black PP pan showing as a 12 mm band under the edge.
- **Back pad:** crowned 6 mm, welt Ø 6 mm on the front seam. Its black PP outer shell (12 mm, 6 mm lip round the pad) carries 4 domed pan-head screws (Ø 9 mm) on the rear.
- **Wear:** seat sag 6–8 mm, and 12 mm on row A's middle seat. One front welt on row A's S seat opened over 25 mm (a split seam; geometry, not a decal).
- **Beam table** (2Table only):
  - oak-grain laminate top (`Prop_WoodOak`, grain along x), on a seat bracket with a 113 mm riser (the beam top 0.328 to the top's underside 0.441);
  - black PP T-mould on 4 sides (3 mm bead);
  - a flush round ashtray at the front centre: chrome spun steel, Ø 127 mm (5 in), 32 mm deep, 3 mm rolled rim, three U-notch rests at 120°;
  - grey ash in the bowl (`Prop_AshSand`). No butts here, to keep the slot count down.

**Slots:**
- Tandem3: `Prop_SteelBlack` (beam, legs, brackets, bolts), `Prop_PlasticBlack` (caps, shells, plugs, glides), `Prop_VinylSlate`. 3 draws.
- 2Table: those three plus `Prop_WoodOak`, `Prop_Chrome` (the ashtray) and `Prop_AshSand`. 6 draws at LOD0, over kitlib's 4: the oak top, the chrome bowl and its ash are the period beam table (E12). LOD1 drops the ash (5); LOD2 has 3 slots.

**Collider:** one box, x ±1.005, y 0–0.84, z −0.32…+0.30 (as the stand-in chairs' boxes, joined).

**Sidecar metadata:**
- Supports: `seat_1/2/3` (x, 0.445, +0.04) size (0.44, 0.40); in 2Table, `table` (−0.65, 0.47, 0) size (0.46, 0.42).
- Anchors: `sit_n`, `back_top_n` (x, 0.83, −0.29); 2Table adds `ashtray` (−0.65, 0.47, +0.08).
- Tags `station`, `seat`, `waiting`.
- `lodDistances` [6, 15, 60]; `SMOOTH_ANGLE` 35 (upholstery stays smooth by its 15° tessellation).

**Variants**, for the colour choice in §7:
- `VARIANTS = {"Kit_TandemBeam3_Navy": {"Prop_VinylSlate": "Prop_VinylNavy"}, "Kit_TandemBeam3_Charcoal": {"Prop_VinylSlate": "Prop_VinylCharcoal"}}`;
- the same two for 2Table (`Kit_TandemBeam2Table_Navy`, `Kit_TandemBeam2Table_Charcoal`).

### 2.2 Smoking stand and sign: `Kit_SmokingStand`, `Kit_SignNoSmoking` (builder A)

**Period:**
- `01` §2: an ashtray under a NO SMOKING sign is the period irony. Workplace smoking rules were spreading in 1986–90 (P67–P69: the 1986 surveys, NY State 1987, the GSA 1986 rule), while the lobby ashtray often stayed.
- `30_look` S25: a floor stand of about 0.6 m, chrome with a black bowl, and butts.
- Blockout envelope: Ø 0.34, 0.64 m tall.
- Typology [memory]: the US office "smoking stand" of the 1950s–80s, a weighted round base, a chrome column and a top bowl with cigarette rests.

**Stand dimensions:** 24.4 in (0.62 m) tall overall.

**Stand parts:**
- **Base:** black enamelled spun-steel dome, Ø 13 in (0.33 m) × 1.75 in (0.044 m), with a 6 mm rolled bead edge, on a foot ring (Ø 0.31, 6 mm, inset 10 mm) that reads as the dark gap under the rim. Second pass: the ring is in the base's `Prop_SteelBlack`, not `Prop_Rubber`, because no camera sees its face (one draw less).
- **Base collar:** chrome, Ø 54 × 22 mm, with two turned beads.
- **Column:** chrome tube, 1.25 in (31.75 mm) OD, 0.50 m long.
- **Top collar:** chrome, Ø 44 × 18 mm, into the bowl's socket.
- **Bowl:** black enamelled spun steel, Ø 10.25 in (0.26 m) at the rim, 2.75 in (0.07 m) deep. Its chrome rolled rim (bead Ø 9 mm) has three U-notch rests (12 mm wide, 10 mm deep) at 120°.
- **Ash insert:** a removable chrome pan, Ø 0.22 m, 25 mm deep, 5 mm lip. In it:
  - an ash bed about 6 mm deep with two soft heaps (no finger dents: those belong to a sand urn, not a bowl stand);
  - 4 butts, Ø 7.8 mm: 25–40 mm of paper and a 20 mm cork-tipped filter; two stubbed and bent 20–40°, one long with a grey ash tip;
  - one bent, burnt paper match.

**Stand slots:** `Prop_Chrome`, `Prop_SteelBlack` (base, foot ring, bowl, the match head), `Prop_AshSand`, `Prop_Paper` (butt paper, the match stick), `Prop_Cork` (filter tipping).
- LOD0 5 draws, over kitlib's 4: the butts need paper and cork. LOD1 drops the butts and the match (3); LOD2 is 3 lathes of 8 sides (2).
- Large lathes 40 sides at LOD0, 20 at LOD1.

**Stand metadata:** collider box (0, 0.31, 0) size (0.33, 0.62, 0.33); anchor `bowl` (0, 0.60, 0); tags `station`, `smoking`; `lodDistances` [4, 10, 40].

**Sign:**
- **Plate:** engraved two-ply plastic, 10 × 3 × 1/8 in (0.254 × 0.076 × 0.0032 m), eased edges (0.8 mm, 2 segments).
- **Fixing:** two chrome oval-head screws (Ø 7 mm, slotted, domed), 15 mm in from the ends.
- **Frame:** origin on the wall face at floor level, heights baked in (the host convention of `interact_key_board.py`); plate centre at 1.55 m; front +Z.
- **Slots:** `Prop_SignNoSmoking` (decal 0–1 on the face), `Prop_Chrome`. No collider; tags `wall_decor`, `station`, `sign`; `lodDistances` [3, 8, 25].
- **Art:** the lettering is art for 平面视觉 · 信息设计 (signs and plates are theirs since the 10-15 split; §3). Until it exists the plate is the blank cap colour.

### 2.3 Magazine rack: `Kit_MagazineRack` (builder A)

**Period:**
- `30_look` S29: a wall rack, 3 pockets, blank-title covers; §2.1 `Prop_Chrome`.
- `research/level_space/62_furniture_l0_office.md` LS98: a wall literature rack 0.61 × 0.10 × 0.91 m, under the same name `Kit_MagazineRack`, with a floor-stand variant. This kit is that row's wall rack, 0.66 m tall to fit the station wall above the rail; a floor stand would come later as `Kit_MagazineRack_Floor`.
- `02_refs` §3.8: a magazine rack belongs to the waiting variant.
- Typology [memory]: welded chrome-plated steel-wire contract racks.

**Dimensions:**
- Overall 24 × 26 × 4.25 in (0.61 × 0.66 × 0.108 m): 2 columns × 3 tiers.
- Mounted above the crash rail: bottom 0.98 m, top 1.64 m.
- Pocket pitch 8.5 in (0.216 m); pocket floor 2.75 in (0.07 m) deep; front lip 3.5 in (0.089 m) high.

**Wire** (all chrome, welded):
- **Frame:** 1/4 in (6.35 mm) rod. It makes the top and bottom rails, the two side ladders bent to the pocket profile (back, floor, lip, as a J per tier) and the centre divider.
- **Lips:** 3/16 in (4.76 mm), two horizontal wires per tier across the full width.
- **Back grid:** 9 ga (3.76 mm) at 25 mm spacing.
- **Pocket floor trays:** two 9 ga wires per tier, 35 and 70 mm off the grid, welded to the side ladders. The back 35 mm stays open (second pass: a full-depth floor would run through the magazine below).
- **Fixings:** optional 1.5 mm weld fillets at the frame joints; two 20 × 30 × 2 mm keyhole tabs on the top rail with chrome pan-head screws (Ø 8 mm). No bumpers: they would sit unseen behind the bottom rail, and they cost a slot.

**Magazines** (4 in 6 pockets: top-left, middle-right, bottom-left, bottom-right):
- 8.5 × 11 in (0.216 × 0.279 m), 4–6 mm thick;
- each stands on its tray and leans back about 13° until its top rests on the back grid. A lower magazine's top passes behind the tray above, so no wire crosses a magazine;
- from the front, a lower-tier magazine shows the 0.127 m band between its own lip (0.089 m) and the lip above (0.216 m): about 45 % of the cover, the title band. The top tier shows all of its cover above the lip and stands about 50 mm over the top rail (to 1.70 m);
- one turned 3° in its plane, the cover's top corner curled 3 mm;
- cover faces on `Prop_MagazineCover` (one 2 × 2 atlas cell each), page edges on `Prop_Paper` (3 sides).

**Frame:** origin on the wall face at floor level, heights baked in; front +Z; x ±0.305, z 0–0.108; wire 0.98–1.64 m, the top magazines to 1.70 m (no collider, so the envelope is visual only).

**Slots:** `Prop_Chrome`, `Prop_MagazineCover`, `Prop_Paper`. 3 draws.
- LOD1: wires 4 sides, every second back wire, the magazines as single quads.
- LOD2: the frame outline as 6 thin boxes and 4 magazine quads.

**Metadata:** no collider (wall decor above the rail); anchors `pocket_1…6` (each pocket floor's centre); tags `wall_decor`, `station`, `paper`; `lodDistances` [3, 8, 30].

### 2.4 Sled guest chair: `Kit_GuestChairSled` (builder B)

**Period** (`03_palette` A row "sled-base guest chairs CONFIRMED", with its full notes §3c):

| Source | What it shows |
|---|---|
| E02, Sep 1987 | Sled base in chrome or baked enamel |
| E10, Sep 1989 | A line with a four-leg or sled base, chrome or 26 colours |
| E18, Oct 1986 | The same line, 26 colours plus chrome |
| E19, Sep 1986 | Sled-base guest chairs in the palette verdict |
| E01, 1984–85 dealer catalogue | Sled-base side-chair colours |

- **Palette synthesis:** "chrome sled base, burgundy or teal seats".
- **Layout** (`30_look` §10.3): two sled chairs on the W wall, facing the room.
- **Colour rule** (`30_look` §2): the red family stays out, and teal may appear only as one small accent. Two teal chairs are that accent.

**Form:** a contract sled-base guest chair with arms.
- **Side loops:** each side is one 1 in (25.4 mm) OD, 16 ga chrome tube bent into a closed loop: floor runner, 2 in (51 mm) centreline-radius bend, front post, arm, rear post, back into the runner.
- **Cross tubes:** two 3/4 in (19 mm) chrome tubes under the seat (front, rear) join the loops.
- **Back frame:** two 3/4 in chrome uprights rise from the rear cross tube to carry the back.

**Dimensions:**

| Part | Inches | Metres |
|---|---|---|
| Overall W × D × H | 23 × 22.5 × 33 | 0.584 × 0.572 × 0.838 |
| Seat height (front) | 18 | 0.457 |
| Arm height | 25.5 | 0.648 |
| Runner length | 20 | 0.51 |
| Seat pad W × D × T | 19 × 17.5 × 3 | 0.483 × 0.445 × 0.076 |
| Back pad W × H × T, centre at 0.68 m, 10° recline (top 0.835 with the shell lip; second pass, the first pass's 0.70 rose above the 0.838 envelope) | 18.5 × 12 × 2.25 | 0.47 × 0.305 × 0.057 |

**Parts:**
- **Seat:** crowned 8 mm top, waterfall front, Ø 5 mm welt on the top seam except the front, 8 mm sag.
- **Back:** crowned 6 mm, welt on the front seam, black PP outer shell with a 5 mm lip, bolted to the uprights (2 chrome bolt heads each).
- **Seat pan:** black PP, on four 25 × 40 × 3 mm chrome tabs welded to the cross tubes (4 bolt heads under the seat).
- **Welds:** 2 mm beads at the cross-tube joints.
- **Glides:** 4 black nylon snap-on sled glides, 44 × 13 × 16 mm, wrapping the runner 60 mm from each end, so the runner sits 6 mm off the floor.

**Frame:** origin on the floor at the footprint centre; front +Z; x ±0.292, z −0.30…+0.27. At the stand-ins' 0.49 the back stands 8 cm off the rail.

**Slots:** `Prop_Chrome`, `Prop_VinylTeal`, `Prop_PlasticBlack`. 3 draws. Tube sides 16 / 8 / 6 for LOD0 / LOD1 / LOD2.

**Metadata:**
- collider box (0, 0.42, −0.015) size (0.584, 0.84, 0.57);
- support `seat` (0, 0.457, +0.02) size (0.44, 0.40);
- anchors `sit` (0, 0.457, 0.02), `back_top` (0, 0.835, −0.27);
- tags `station`, `seat`, `office`;
- `lodDistances` [5, 12, 50].

**Variant:** `VARIANTS = {"Kit_GuestChairSled_Burgundy": {"Prop_VinylTeal": "Prop_VinylBurgundy"}}`.

### 2.5 Sign-in ledge: `Kit_StationCounter_Ledge120` (builder B)

**Period:**
- `01` §1: a transaction ledge of ~42 in (1.07 m), guides 42–48 in (P33, P34), ledge depth 10–14 in.
- No accessible low section on a 1990 counter (pre-ADAAG, P20).
- `03_palette`: reception counter CONFIRMED, oak or walnut fascia, grey laminate with wood trim (E02, E10, E19).
- Colour-through laminate was popular with designers in the 1980s (P36), so its edges show no dark core line.
- `30_look` S05: the ledge alone against a wall, plan D's sign-in counter.

**Section:** the same visitor-ledge section as `Kit_StationCounter_Run120`. Read `station_counter.py`'s constants; do not edit that file.

**Dimensions:** 47.2 × 42.1 × 13 in (1.20 × 1.07 × 0.33 m).
- **Oak top:** 32 mm, y 1.038–1.07; from the wall face (z 0) to the nosing (z 0.33).
- **Nosing:** dark PVC bullnose, the Run120 profile (a 12 mm half-round over the 32 mm edge).
- **Ends:** 2 mm PVC edge band.
- **Wall joint:** a 12 × 25 mm oak cleat under the top at the wall (y 1.013–1.038) carries it. On top the oak meets the wall with a 1 mm scribe gap and nothing proud, so the register's back edge (15 mm off the wall) lies flat.
- **Base body:** grey colour-through laminate, z 0.035–0.27, so it clears the 28 mm crash rail.
  - The fascia has a 3 mm shadow gap under the top and one centre reveal (4 × 2 mm, as Run120).
  - 19 mm end panels from the wall face to the fascia, their front edges in the same grey (colour-through: no dark line). They are scribed round the station trim, which runs on behind the body (§1.3): a 32 × 160 mm notch for the crash rail (y 0.775–0.935, z 0–0.032) and a 10 × 105 mm notch for the base (y 0–0.105, z 0–0.010). So no gap shows at the ends.
  - Black vinyl toe, 0.10 m tall, recessed 0.05 m.

**Frame:** origin on the wall-face plane at floor level, centred on the run (the host convention); front +Z into the room.

**Slots:** `Prop_WoodOak` (grain along x, `fr_grain` "x"), `Prop_PVCEdge`, `Prop_LaminateGrey`, `Prop_Vinyl`. 4 draws.

**Metadata:**
- collider box (0, 0.535, 0.165) size (1.20, 1.07, 0.33);
- support `ledge` (0, 1.07, 0.175) size (1.16, 0.28);
- anchors `register` (−0.22, 1.07, 0.165) and `tent_card` (0.36, 1.07, 0.20), where §4 puts the two kits;
- tags `station`, `counter`;
- `lodDistances` [4, 10, 60];
- `station_meta` origin "wall face plane at floor level, centred on the run".

### 2.6 Laminate credenza: `Kit_Credenza_Laminate` (builder B)

**Period:**
- `30_look` S04 and §2.2: beige laminate, chrome pulls.
- E04 (*Contract* Nov 1989): a desk, credenza, file and pedestal line in six standard laminate colours, so laminate credenzas were standard contract case goods.
- It is the companion of `Kit_OfficeDesk`: the 30 × 60 in steel-frame pedestal desk of c. 1985–98 (`office_desk.py`), beige laminate top at 0.741 m.
- **New geometry under the planned name, not a slot swap of `Kit_Credenza`.** That mesh has a cherry ogee top, routed V-groove fronts and brass bail pulls, none of which laminate can have. (`30_look` S04 and the stage brief call it a variant; the name stays, the mesh is new.)
- **Edges match the desk (second pass):** `Kit_OfficeDesk` has a rounded laminate self-edge, the light edge band of Red's target office image (`office_desk.py`, `EDGE = LAM`). The first pass gave the credenza a dark PVC T-mould and dark edge bands; it is self-edged now, so the two read as one suite, and it drops a slot.

**Dimensions:** 60 × 20 × 29.2 in (1.524 × 0.508 × 0.741 m); the top at the desk's 0.741.

**Construction:**
- **Pedestals** at both ends (0.40 m wide): a 6 in box drawer over a file drawer. Two hinged doors between them.
- **Top:** 1-1/8 in (29 mm) beige HPL with a rounded self-edge (2.5 mm, 2 segments, as the desk), overhanging the sides by 12 mm and the front by 15 mm.
- **Case:** 19 mm laminate-faced sides, self-edged on the front edges.
- **Fronts:** flush overlay with 3 mm reveals, self-edged (1.5 mm eased). Dark quads behind the shut lines (`_newcase.shadow`, in `Prop_Vinyl`) make the reveals read under flat top light.
- **Pulls:** chrome wire D-pulls, 8 mm wire on 4 in (102 mm) centres, 32 mm projection, Ø 12 mm round rosettes; horizontal on the drawers, vertical on the doors.
- **Locks:** chrome lock cores (Ø 19 mm, keyway slot) on the two box drawers.
- **Hinges:** concealed, none visible.
- **Base and back:** black vinyl toe kick, 3 in (76 mm) high, 2 in (51 mm) recess, returning on the sides; tempered hardboard back recessed 10 mm.

**Slots:** `Prop_LaminateBeige`, `Prop_Chrome`, `Prop_Vinyl` (toe kick, reveal shadows), `Prop_Hardboard` (back). 4 draws.

**Metadata:**
- collider box (0, 0.3705, 0) size (1.524, 0.741, 0.508);
- support and anchor `top` (0, 0.741, 0), support size (1.48, 0.46);
- tags `station`, `case_goods`, `office`;
- `lodDistances` [5, 12, 60];
- origin on the floor, centred; front +Z.

## 3. Materials and textures

All are `Resources/Surfaces/<slot>.mat` on `FrontRooms/Surface` with `_FR_MESH_UV`. The kit library remaps each slot by name. Each new `.mat` gets a `.mat.meta` with a fresh GUID. Colours are SP (starting points), tuned on the renders.

| Material | Builder | Values | Maps |
|---|---|---|---|
| `Prop_VinylSlate` | A | `_BaseColor` #566679 (`03_palette` slate blue; the authored colour of both beam kits), `_Smoothness` 1 (the `_S` map's mean is 0.40), metallic 0, `_TileSize` 1 | As `Prop_Vinyl`, by GUID: `_BumpMap` `Prop_Vinyl_N` bd17a764f591e40dbaa68a4d708fa48b, `_MaskMap` `Prop_Vinyl_S` a6042614b72a94baaa3de6587d13f73f, `_MacroMap` `MacroWear_M` f777b2e3dd7d240d390e2b2cd59cbb9e. `_BaseMap` none: the tint carries the colour, as `Prop_FabricUniform` tints `FabricCharcoal`'s maps |
| `Prop_VinylNavy` | A | #26324A (`30_look` §2.1 SP; `Prop_FabricNavy_A` mean #222F4A), for the `_Navy` variants | as Slate |
| `Prop_VinylCharcoal` | A | #3C3F44 SP (`03_palette` charcoal / dark grey), for the `_Charcoal` variants | as Slate |
| `Prop_AshSand` | A | #8C877C, `_Smoothness` 0.08, `_BumpScale` 0.6, tile 0.25 m | `_BumpMap` `Prop_FabricCharcoal_N` 53385237db6b54a81b6d4b89f6a36b94 (granular) |
| `Prop_SignNoSmoking` | A | #2B2925 cap (as `Prop_StationKeyPlate`'s placeholder), smoothness 0.35, tile 1 | none until 平面视觉 · 信息设计's `Prop_SignNoSmoking_A.png` (1024 × 256 over the 0.254 × 0.076 m face) |
| `Prop_MagazineCover` | A | white × base map, smoothness 0.6 (coated stock), tile 1 | `Prop_MagazineCover_A.png` **1024²**, 2 × 2 cells of 512². That is 2,370 px/m on a cover, above the 1,975 px/m a 1080p frame shows at 0.35 m (vFOV 76°); the first pass's 512² gave 1,185. Placeholder: 4 flat period colour fields (no lettering, no red, no glow-ink yellow-green, no amber). `.png.meta` copied from `Prop_StationKeyPlate_A.png.meta` with a fresh GUID |
| `Prop_VinylTeal` | B | #2C6264 (`Prop_FabricTeal_A` mean #2E6A69) | as Slate |
| `Prop_VinylBurgundy` | B | #5C2430 | as Slate |

- **Reused unchanged:** `Prop_SteelBlack`, `Prop_PlasticBlack`, `Prop_Chrome`, `Prop_WoodOak`, `Prop_PVCEdge` (the ledge only), `Prop_LaminateGrey`, `Prop_LaminateBeige`, `Prop_Vinyl`, `Prop_Hardboard`, `Prop_Paper`, `Prop_Cork`. `Prop_Rubber` is no longer used. The shader ignores vertex colour, so wear goes into geometry and the shared maps.
- **Slots registered in Blender:** kitlib's `SLOTS` has every reused slot but `Prop_Cork` and `Prop_LaminateGrey`, which `_station_common.register()` adds. Each builder registers its new slots with `kitlib.register_slot` and the preview colour of the `.mat` (sRGB to linear).
- **New texture memory:** about 1.3 MiB (the 1024² cover atlas with mips, BC7). The sign face adds about 0.3 MiB when its art lands.
- **Art requests** (faces and sizes are 平面视觉's, strings 叙事's):
  - the `NO SMOKING` plate (01 §6), to 平面视觉 · 信息设计;
  - the four magazine covers (no real titles), in their own atlas rather than the station paper atlas. That atlas's README lists both as open; covers are coated stock with their own sheen, which the paper material does not have.
  - Both are diegetic text, so language copies follow the WorldText convention landed 11-03: `Resources/WorldText/<code>/Surfaces/Textures/<name>_A`, listed under `swap` in `manifest.json`.

## 4. The module swap (contract to 关卡设计; the Stations stage tries it in the clone, the Package stage writes `W/apply/bs4_hero_contract/`)

**Station_Waiting_3x2: 34 props before and after.**
- Keep P01–P26 and P32–P34 (trash bin, hat stand, clock) where they are. The five chair entries P27–P31 are rewritten in place, so no other entry moves and the diff stays five entries (second pass; the first pass renumbered P32–P34):
- P27 becomes `Kit_TandemBeam3` x 3.6, z 2.15, yaw 90, y 0, collider.
- P28 becomes `Kit_TandemBeam2Table` x 1.2, z 0.5, yaw 0, y 0, collider.
- P29 becomes `Kit_SmokingStand` x 4.6, z 0.42, yaw 0, collider.
- P30 becomes `Kit_SignNoSmoking` x 4.6, z 0.08, yaw 0, no collider.
- P31 becomes `Kit_MagazineRack` x 2.1, z 5.92, yaw 180, no collider.
- Update the module's `notes` sentence ("Existing kits stand in for…").

**Station_Desk_3x3: 29 props become 33.**
- Keep P01–P22.
- P23–P24 become `Kit_GuestChairSled` at x 0.49, z 1.7 / 2.4, yaw 90, collider.
- P25–P29 unchanged.
- P30 `Kit_StationCounter_Ledge120` x 0.08, z 4.5, yaw 90, collider.
- P31 `Kit_RegisterBook` x 0.245, z 4.72, y 1.07, yaw 90, no collider.
- P32 `Kit_TentCard` x 0.28, z 4.14, y 1.07, yaw 90, no collider.
- P33 `Kit_Credenza_Laminate` x 6.36, z 8.651, yaw 180, collider.

**Unchanged in both:** markers, lamps, edges, edge records, `allowRotate` 0, the GUIDs.

**Checks 关卡设计 runs after our kits land:**
- `FloorStationTemplate.From` non-null for both themes (Validate + ValidateFloorStation, 0 errors);
- `PropsLeftOutCount` 0;
- the fit table on floors 2–5, which matches today's (props never change the cell-level fit).

## 5. Cost

| Module | LOD0 today → planned | LOD1 | LOD2 (LOD1 where none) | Floor props with colliders | Kit LOD renderers |
|---|---|---|---|---|---|
| Waiting | 85,934 → 83,294 | 36,779 → 36,534 | 25,880 → 25,192 | 12 → 10 (5 chairs → 2 beams + the stand) | ±0 (15 out, 15 in) |
| Desk | 69,606 → 77,264 | 30,326 → 33,692 | 23,298 → 24,124 | 8 → 10 | +11 (6 out, 17 in) |

- **LOD0 draws** (submeshes):
  - Waiting: the five chairs make 15; in come the two beams 9 (3 + 6), the stand 5, the sign 2 and the rack 3, so 19 (+4).
  - Desk: the two chairs make 6; in come the sled chairs 6, the ledge 4, the register 4, the tent card 1 and the credenza 4, so 19 (+13).
- **Mesh memory** is an ESTIMATE until Unity reports it: (vertices × 48 B + triangles × 6 B) per LOD. Each sheet's COST row says which.
- **Measured numbers come later:** the Kits stage reports tris per LOD as exported; the Stations stage reports the built-set totals (budget 100k LOD0, 120 renderers).

## 6. Builder split (no shared files)

Both builders read, and never edit: `kitlib.py`, `build_asset.py`, `_station_common.py`, `station_counter.py`, `task_chair.py`, `_newcase.py`, `outlet_common.py`, `office_desk.py`, `interact_key_board.py`. Each writes only its own asset scripts in the clone (`W/proj_bstations/Tools/Blender/frontrooms_kit/assets/`) and its own output root, given to `build_asset.py --out-root`, which mirrors the project tree. The Stations stage copies both roots into the clone (no path in common) and runs the one Unity import.

**Builder A: the Waiting kits** (tandem beams, smoking stand + sign, magazine rack). Output root `W/bs4_hero/build_a`.

| File | What |
|---|---|
| `assets/_station_waiting_common.py` | `register()`: `_station_common.register()` (for `Prop_Cork`) plus its 6 new slots. Helpers copied, not imported: `task_chair.py` `_soft_block`, `_rrect`, `_fillet_path`; `outlet_common.finish_part` (G1). Hand-LOD helpers (`fr_lods`); `hero_preview()` (§6 renders); `station_block()` (§2 sidecar) |
| `assets/tandem_beam.py` | `NAME = "Kit_TandemBeam3"`; `VARIANTS` `_Navy`, `_Charcoal` |
| `assets/tandem_beam_table.py` | `NAME = "Kit_TandemBeam2Table"`; `VARIANTS` `_Navy`, `_Charcoal` |
| `assets/smoking_stand.py` | `Kit_SmokingStand` |
| `assets/sign_no_smoking.py` | `Kit_SignNoSmoking` |
| `assets/magazine_rack.py` | `Kit_MagazineRack` |
| `build_a/Assets/Resources/Props/Models/` | `.fbx` + `.json` of 9 names: `Kit_TandemBeam3`, `_Navy`, `_Charcoal`; `Kit_TandemBeam2Table`, `_Navy`, `_Charcoal`; `Kit_SmokingStand`; `Kit_SignNoSmoking`; `Kit_MagazineRack` (18 files) |
| `build_a/Assets/Resources/Surfaces/` | `Prop_VinylSlate`, `Prop_VinylNavy`, `Prop_VinylCharcoal`, `Prop_AshSand`, `Prop_SignNoSmoking`, `Prop_MagazineCover`: `.mat` + `.mat.meta` (12 files); `Textures/Prop_MagazineCover_A.png` + `.png.meta` (2) |

**Builder B: the Desk kits** (sled guest chair, sign-in ledge, laminate credenza). Output root `W/bs4_hero/build_b`.

| File | What |
|---|---|
| `assets/_station_desk_common.py` | `register()`: `_station_common.register()` (for `Prop_LaminateGrey`) plus `Prop_VinylTeal`, `Prop_VinylBurgundy`. Its own copies of the same helpers (no file shared with A); `hero_preview()`; `station_block()` |
| `assets/guest_chair_sled.py` | `NAME = "Kit_GuestChairSled"`; `VARIANTS` `_Burgundy` |
| `assets/station_counter_ledge.py` | `Kit_StationCounter_Ledge120` (the visitor-ledge constants copied from `station_counter.py`) |
| `assets/credenza_laminate.py` | `Kit_Credenza_Laminate` (may import `_newcase` read-only for `plate`, `shadow`, `eased`) |
| `build_b/Assets/Resources/Props/Models/` | `.fbx` + `.json` of 4 names: `Kit_GuestChairSled`, `Kit_GuestChairSled_Burgundy`, `Kit_StationCounter_Ledge120`, `Kit_Credenza_Laminate` (8 files) |
| `build_b/Assets/Resources/Surfaces/` | `Prop_VinylTeal`, `Prop_VinylBurgundy`: `.mat` + `.mat.meta` (4 files) |

**Both builders:**
- **Blender 4.3.0 headless,** one module per run, no sound, no `.pyc` in the clone:
  `PYTHONDONTWRITEBYTECODE=1 /Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup --python /Users/redwang/FrontRoomsVisualWork/proj_bstations/Tools/Blender/frontrooms_kit/build_asset.py -- <module> --out-root /Users/redwang/FrontRoomsVisualWork/bs4_hero/build_<a|b> --preview-dir /Users/redwang/FrontRoomsVisualWork/bs4_hero/renders/kits/previews_<a|b> --samples 64 > /Users/redwang/FrontRoomsVisualWork/bs4_hero/logs/builder_<a|b>_<module>.log 2>&1`
  Never the clone's `Verification/` folder, never `Assets/` in the clone directly.
- **`.mat` files** are written as text from `Prop_Vinyl.mat` (shader 38dd6d7df75f84369b8237ac8848106c, keyword `_FR_MESH_UV`, `_MeshUV` 1): a new `m_Name`, `_BaseMap` cleared, `_BaseColor` and the floats of §3. `Prop_AshSand`, `Prop_SignNoSmoking` and `Prop_MagazineCover` set their maps as §3 says. `.meta` files get fresh GUIDs (`uuid4().hex`), LF only.
- **Renders** (`hero_preview`, Cycles, AgX, 64+ samples, JPG q92) into `W/bs4_hero/renders/kits/<Kit>/`. Cameras fixed per kit and the same for every column: R1 player eye 1.62 m at 3 m, 3/4 front; R2 3/4 back at 1.5 m; R3 hero 0.35 m on the named hardware (LOD0); R4 LOD1 at its `d01` and LOD2 at its `d12`. Preview materials take the `.mat` colour, smoothness and the `_N` map, so Blender reads like the game. An optional silent turntable MP4 (`ffmpeg -an`).
- **One Shot-format sheet per builder** (`W/tools/shotframe/shotframe.py grid`, then the fit check): `W/bs4_hero/sheets/kits_waiting.jpg` (A) and `kits_desk.jpg` (B). Columns: TODAY (the stand-in at the same camera, or an "ABSENT IN BS4" tile), the kit in its REC colour, then the variants. Rows R1–R4 and COST (tris LOD0 / LOD1 / LOD2, MiB, draws). REC on the authored colour. Chip `WAIT-RED` where §7 leaves a choice. English only. Each sheet gets a `<sheet>_fit.txt` ending `ALL LABELS FIT`, checked as `W/zone_lamps/render_tools/zl_option_sheets.py` does (import it read-only). Phone tier and off state are Unity rows: the Stations stage.
- **Disk first:** one journal line per export: the kit, the sha1 of the `.fbx` and `.json`, tris per LOD, slots, and the resume point.
- **No Unity.** The Stations stage imports both roots in the clone and writes the `.fbx.meta` / `.json.meta` files.

## 7. Choices left for Red (shown on the Stations sheet, REC marked; the contract carries the REC name, a pick is a one-word change)

| Choice | Options | REC (plan) and honest flaw |
|---|---|---|
| Tandem upholstery | T-A navy (`_Navy`, today's stand-in colour) · T-B slate (`Prop_VinylSlate`, the authored kits, `03_palette`'s verdict) · T-C charcoal (`_Charcoal`, the verdict's other colour) | **T-B** (second pass; the first pass recommended navy): the attested 1986–89 refit colour, and the guard's navy jacket stays the room's one navy mass, so the false-occupancy silhouette keeps its read. Flaw: slate #566679 sits nearer the blue-grey walls (#9AA3A6) than navy does, so the rows read by value and by their black frames, not by hue. Check it in the set's lavender light |
| Sled seats | S-A teal (`Prop_VinylTeal`, the authored kit) · S-B burgundy (`_Burgundy`) | **S-A**: one teal accent, as `30_look` §2 allows, and no red near the ALARM ZONE card's colour. S-B is shown because the palette names it, but it is in the red family §2 keeps out. Flaw of S-A: teal is the decade's stereotype colour; it stays one small accent (two chairs), with no mauve in the room |
| Magazine rack wall | M-A N wall x 2.10 · M-B W wall z 1.45 (the rev 3 blockout's spot) | **M-A**: seen inside E1 and through E1's left edge, lit by the Steady cell. Flaw: a chrome glint at the edge of the junction frame (C1), to check against the squint test |
| Credenza | C-A N wall under the key cabinet · C-B none (BS4 as is) | **C-A**: gives the key-control wall a base and the desk its companion. Flaw: a new mass 10° left of the set from E1, partly behind the desk and the jacket |

## 8. Stage log

### Plan (2026-11-03 17:18–18:50, two passes; the first pass's agent died after the layout check)

- **Read:** RULES, VISUAL_STANDARD, research/station 01, 03, 30, 70, 71 (sections), the BS4 contract and generator, kitlib, the station kit scripts, the module code.
- **Wrote:** `W/bs4_hero/journal.txt`, `tmp/inventory.py` (+ `inventory_main.txt`), `tmp/plan_check.py` (+ `plan_check_out.txt`), this file.
- **Numbers:** as §0 and §5. The planned swap passes the mirrored rules in both modules (0 errors, walk OK).
- **Second pass (18:04–18:50), from the journal's resume point:** checked the clone's inputs against main (all equal; main's five `alarm_exit_* 2.py` are iCloud copies and never sync); ran `docs find`; corrected the first pass where it would not have built or did not follow the evidence:
  - tandem colour → slate, with navy and charcoal variants (03 / `D_palette` §1);
  - tandem legs moved to ±0.325 (they sat on the outer seat brackets), the beam lowered to 0.29 and the back to 0.34 m (the seat pan and the 0.84 envelope);
  - sled back centre 0.68 (the envelope);
  - rack trays open at the back (the magazines below ran through them), no bumpers (3 draws), the top magazines to 1.70 m;
  - stand's foot ring in `Prop_SteelBlack` (5 draws), an ash bed instead of sand dents;
  - ledge end panels scribed round the trim, the cleat under the top;
  - credenza self-edged to match `Kit_OfficeDesk` (4 slots);
  - cover atlas 1024² for the 0.35 m bar;
  - the Waiting swap rewrites P27–P31 in place;
  - the builders write to their own output roots.
  - `plan_check.py` was not re-run: no position, footprint or collider changed in the second pass.
- **Open, not this package:**
  - `Prop_LaminateGrey` is a flat colour (smoothness 0.5, no micro-texture): a matte HPL texture would help every counter piece.
  - The Floor 1 counter kits (`Run120` 1,956 / `Run075` 688 / `End` 500 tris) sit below their 3,500 / 2,500 / 1,200 hero budgets. A follow-up pass in place can reuse this ledge's section (same planes, same supports, the SECURITY letters' fascia plane kept).
- **Pointers:**
  - The station look capture harness is clone-only, `W/proj_station_2/Assets/Editor/StationLook/FrontRoomsStationLookProof.cs` (`71` §2.4).
  - The BS4/BS5 frame specs are in `W/vl_pending/bs4_station_modules.md` and `bs5_station_floors.md`.

### Kits · builder B (2026-11-03 18:34–21:15: the first agent died, relays at 19:43 on account B and after the Mac's 20:18 reboot)

- **Built** with the house machinery (`kitlib.Kit`, `build_asset.py --out-root W/bs4_hero/build_b`, hand-built `fr_lods`, the G1 weighted-normal recipe), Blender 4.3.0 headless, no `.pyc` left in the clone (the caches of two early runs moved to `W/_partial/bs4_hero_builder_b_pycache_1103`). Scripts in `W/proj_bstations/Tools/Blender/frontrooms_kit/assets/`: `_station_desk_common.py`, `guest_chair_sled.py`, `station_counter_ledge.py`, `credenza_laminate.py`. Output root `W/bs4_hero/build_b` (mirrors the project tree): 8 model files, 4 surface files. No Unity, no `.fbx.meta` (the Stations stage imports both roots).
- **Exported** (vertices counted as an importer splits them; MiB is an ESTIMATE, all LODs, vertices × 48 B + triangles × 6 B):

| Kit | tris LOD0 / LOD1 / LOD2 | target (cap) | vertices (as split) | mesh MiB est | LOD0 draws | bounds (Unity, m) | fbx / json sha1 |
|---|---|---|---|---|---|---|---|
| `Kit_GuestChairSled` | 5,176 / 1,192 / 360 | 5,000 / 1,700 / 350 (5,300) | 4,207 / 1,109 / 368 | 0.30 | 3 | x -0.295..0.295, y 0.000..0.839, z -0.300..0.270 | f58034d3 / 7bcd64ec |
| `Kit_StationCounter_Ledge120` | 2,044 / 156 / 36 | 3,000 / 1,200 / 150 (3,500) | 1,740 / 276 / 72 | 0.11 | 4 | x -0.600..0.600, y 0.000..1.070, z -0.000..0.330 | 6f55666d / af1dc5a6 |
| `Kit_Credenza_Laminate` | 3,254 / 662 / 36 | 3,500 / 1,300 / 200 (5,300) | 3,062 / 820 / 72 | 0.20 | 4 | x -0.762..0.762, y 0.000..0.741, z -0.254..0.271 | 898b46a2 / 3debad5d |

- **Hashes:** variant `Kit_GuestChairSled_Burgundy` fbx c6b5f139 / json 029944c8; `Prop_VinylTeal.mat` 97b3dda8 (.meta 6e07d7d1), `Prop_VinylBurgundy.mat` 2b862cff (.meta 8a525c2f); scripts: _station_desk_common 9a1b7ac6, guest_chair_sled e4313e68, station_counter_ledge fad8d274, credenza_laminate 21bad084; render/desk_render 3e3d6034, render/kit_cost 806fc0f9, render/kits_desk_sheet 6c3975d0; stand-in Kit_TaskChair_Navy: 5168 / 1601 / 420 tris, 0.37 MiB est

- **Against the plan (§2.4–2.6), build facts:**
  - **Chair:** the seat pan rides on two chrome flat-bar seat rails (25 × 4.8 mm) over the cross tubes, not on four 40 mm tabs: the cross tubes sit at the posts, 0.50 m apart, against a 0.43 m pan, so tabs cannot reach it. Seat raked 3°; rear posts raked 8° forward (the loop solved for the −0.30 m envelope); back uprights S-bent from the rear cross tube up behind the shell, black end plugs. The welt is a 3 mm bulge ring in the pad's own surface, not a separate 5 mm cord (about 650 tris less; reads the same at 0.35 m). LOD0 5,176 is 176 over the 5,000 target, under the 5,300 cap; LOD2 360 against 350.
  - **Ledge** 2,044 and **credenza** 3,254 are under their targets with every part of §2.5–2.6 built (nothing dropped for tris). The ledge's bullnose cut ends take a 1 mm bevel and the 2 mm PVC end bands 0.8 mm (the band cannot take 1 mm on both arrises).
  - **D-pull bends** every 30°, not 15°: at an 8 mm centreline radius the silhouette error is 0.27 mm, about half a pixel at 0.35 m in 1080p. The 15° rule is kept on the chair's 51 mm bends.
  - **Vinyl tile 0.45 m** for `Prop_VinylTeal` and `Prop_VinylBurgundy`, as builder A's slate: at `Prop_Vinyl`'s 1 m tile the grain cells read as 2–4 cm wrinkles at 0.35 m.
  - The credenza's back takes the house `Prop_Hardboard`, which is the chipboard albedo tinted brown: it shows as a mottled board in the turntables and faces the wall (15 mm off) in the module.
- **Checks:**
  - shading of the exported FBX (`W/bs4_hero/tmp/b2/g1check2.py`: faces wider than 1 cm whose corner normals leave the face normal by more than 2°): two faults found and fixed (the ledge's oak cleat 28.9° and its vinyl toe 10.3°, neither had been through G1). Now: flat laminate ≤ 1.6°, the cleat under the top 4.4°, the credenza's toe returns 3.4°; the upholstery and PP shells are meant to shade round.
  - FBX export warnings: none (the ledge LOD1's n-gon caps, which blocked its tangents, are triangulated).
  - Bounds within the §1.2 envelopes (±0.02): chair x ±0.295 (glides), top 0.839; credenza pulls 17 mm past the top's front edge, z 8.380 in the module against the 8.40 envelope (the collider stays the 0.508 box).
  - Every render rebuilt the kit from its script and matched the exported triangle counts.
- **Renders** in `W/bs4_hero/renders/kits/<Kit>/` (Cycles 64 spp, AgX, troffer lens 40 W/m², exposure 0 everywhere; `render/desk_render.py` → `_station_desk_common.hero_preview`):
  - R1 eye 1.62 m at 3 m, R2 1.5 m, R3 hero 0.35 m on LOD0, in the Station_Desk_3x3 shell at the module poses, columns TODAY / REC / variant or dressed;
  - R4 LOD1 at d01 and LOD2 at d12 (studio, game vFOV 76, 1080p-density crops);
  - turntable frame sets `tt035_00–07` (0.35 m on the named hardware, vFOV 76, 1080p), `tt150_*`, `tt400_*`, each on the game's LOD at that distance (the ledge's 4 m set is LOD1, its d01 being 4.0 m), each with a `.txt` of its camera;
  - `<Kit>_turntable.mp4`: 72 frames at 24 fps, 1280 × 720, h264, no audio stream.
  - Times: chair 669 s, ledge 796 s, credenza 867 s.
- **Sheet:** `W/bs4_hero/sheets/kits_desk.jpg` (2560 × 7073, Shot format, `WAIT-RED`), with `kits_desk_fit.txt` ending ALL LABELS FIT. Its rows are R1–R4 and COST; its columns are TODAY, REC and the variant or dressed view.
- **Look, at 1:1 and 2–3× crops:**
  - chair: the loops and bends read round at 0.35 m; the bend highlights are the troffer grid. The welt, the shell lip, the seat rails and the glides all read. LOD1 at 5 m and LOD2 at 12 m keep the silhouette.
  - ledge: the scribe round the crash rail reads; the register and the tent card sit flat on the anchors.
  - credenza: the pulls, the rosettes, the lock core and keyway, and the 3 mm reveals read at 0.35 m.
- **Open:**
  - `Prop_LaminateGrey` and `Prop_LaminateBeige` are flat colours with no micro-texture; `Prop_LaminateGrey` was already open from the plan.
  - Phone tier, off state and frame time are Unity rows for the Stations stage.

### Kits · builder A (2026-11-03 18:36–22:48: the first agent died; relays at 19:43 and after the Mac's 20:18 reboot on account B, and at 22:26 on account G after B's limit)

- **Built** with the house machinery (`kitlib.Kit`, `build_asset.py --out-root W/bs4_hero/build_a`, hand-built `fr_lods`, the G1 weighted-normal recipe), Blender 4.3.0 headless with `sys.dont_write_bytecode` (the byte caches of the early runs moved to `W/_partial/bs4_hero_builder_a_pycache_1103`; none written since). Scripts in `W/proj_bstations/Tools/Blender/frontrooms_kit/assets/`: `_station_waiting_common.py`, `tandem_beam.py`, `tandem_beam_table.py`, `smoking_stand.py`, `sign_no_smoking.py`, `magazine_rack.py`; the six surfaces and the cover atlas from `W/proj_bstations/Tools/lookdev/gen_bs4_waiting_surfaces.py`. Output root `W/bs4_hero/build_a` (mirrors the project tree): 18 model files, 14 surface files. No Unity, no `.fbx.meta` (the Stations stage imports both roots).
- **Exported** (vertices counted as an importer splits them; MiB is an ESTIMATE, all LODs, vertices × 48 B + triangles × 6 B; draws = used slots per LOD):

| Kit | tris LOD0 / LOD1 / LOD2 | target (cap) | vertices (as split) | mesh MiB est | draws per LOD | bounds (Unity, m) | fbx / json sha1 |
|---|---|---|---|---|---|---|---|
| `Kit_TandemBeam3` | 10,099 / 2,268 / 264 | 9,000 / 3,000 / 600 (10,500) | 9,689 / 2,613 / 528 | 0.66 | 3 / 3 / 3 | x -1.002..1.002, y 0.000..0.840, z -0.314..0.300 | 566ead1a / d0691a55 |
| `Kit_TandemBeam2Table` | 8,991 / 2,034 / 228 | 8,000 / 2,700 / 500 (9,500) | 8,734 / 2,412 / 456 | 0.60 | 6 / 5 / 3 | as Tandem3 | 68fb2173 / dcbc688a |
| `Kit_SmokingStand` | 3,876 / 796 / 112 | 3,000 / 1,000 / 150 (4,000) | 3,609 / 849 / 98 | 0.24 | 5 / 3 / 2 | x -0.165..0.165, y 0.000..0.615, z -0.165..0.165 | cb305c04 / 59ab9613 |
| `Kit_SignNoSmoking` | 166 / 10 / 2 | 200 / 60 / 12 | 172 / 20 / 4 | 0.01 | 2 / 1 / 1 | x -0.127..0.127, y 1.512..1.588, z 0.000..0.005 | 068fe3fc / ef953078 |
| `Kit_MagazineRack` | 2,876 / 744 / 80 | 3,000 / 1,000 / 150 (3,500) | 2,994 / 1,192 / 160 | 0.22 | 3 / 2 / 2 | x -0.305..0.305, y 0.980..1.690, z 0.000..0.110 | b75f2f83 / 57bffdce |

- **Hashes:** variants `Kit_TandemBeam3_Navy` 08a72c24 / b97d507c, `Kit_TandemBeam3_Charcoal` 77adeb1a / 4c7f1d93, `Kit_TandemBeam2Table_Navy` 2e3b2a16 / 1de98c1b, `Kit_TandemBeam2Table_Charcoal` 6affae03 / 57e22a2b; surfaces (`.mat` / `.mat.meta`): `Prop_VinylSlate` ff4d342c / de5383f2, `Prop_VinylNavy` 7592b755 / 9e91c567, `Prop_VinylCharcoal` 16fdc19b / 3148c645, `Prop_AshSand` 3e795c9d / 6afcf434, `Prop_SignNoSmoking` c6cdf83f / 7e83e297, `Prop_MagazineCover` a2b4b13f / cde8eacd, `Textures/Prop_MagazineCover_A.png` 5270aa44 / 45a835c6; scripts: _station_waiting_common 78f311c2, tandem_beam 1f4570f9, tandem_beam_table 7d5d06fd, smoking_stand 5bb58800, sign_no_smoking c2a7b9be, magazine_rack 642911f6, gen_bs4_waiting_surfaces 3e12cdf1; render/render_kits b97c5859, final_jobs 43999f41, run_final ba455716, run_final2 cf78583e, run_final3 583aa7fc, mesh_stats 3a85ad62, sheet_waiting 22b97b16. Stand-in `Kit_TaskChair_Navy` (fbx a58f764a, = main): 5,168 / 1,601 / 420 tris, 0.37 MiB est, 3 / 3 / 3 draws.
- **Against the plan (§2.1–2.3, §3), build facts:**
  - **Over target, under cap:** Tandem3 LOD0 10,099 (target 9,000), 2Table 8,991 (8,000), stand 3,876 (3,000); sign and rack are under target, and every LOD1 and LOD2 is. Nothing of §2.1–2.3 was dropped for tris. The 21:07 hero pass gave the seat brackets' C-clamp cheeks (50 × 66 × 6 plates on the beam) a 1 mm bevel: +192 tris on each beam kit.
  - **`SMOOTH_ANGLE` 50 in every A kit** (§2.1 said 35): two-segment bevels and 8–12-sided wire shade round, hex flats and 90° creases stay sharp; the parts and `kit.finish` use the same angle, which is what the G1 probe proved survives the join and the FBX round trip.
  - **Vinyl tile 0.45 m** for `Prop_VinylSlate`, `_Navy` and `_Charcoal` (§3 said 1): at `Prop_Vinyl`'s 1 m tile the grain reads as 3 cm wrinkles at 0.35 m. Builder B took the same tile for teal and burgundy.
  - **Tandem:** waterfall front r 40 (§2.1: r 30); seat sag 7 / 12 / 6 mm; the opened welt is the S seat's side welt near the front (row A's yaw 90 puts local +x to the south).
  - **Beam table:** the top's underside is a dark phenolic backer (`Prop_PlasticBlack`), not oak: the first look showed oak under the table from the seats.
  - **Stand:** the first look read the ash bed as a clean white plate, so it gained two tapped ash columns, five charred paper flecks (`Prop_SteelBlack`) and three tobacco crumbs (`Prop_Cork`) by the crushed butts; the unseen foot ring went from 32 to 16 sides. Still 5 slots.
  - **Rack:** lip wires at +0.045 / +0.085 (§2.3: +0.089); the magazines lean about 18° (§2.3: about 13°), the bottom against the lip wires, the top on the grid; the J bends are sampled every 30° on their 8 mm radius (as builder B's D-pulls: 0.27 mm off the arc, about half a pixel at 0.35 m in 1080p). A flipped curled-corner cell on the top-left cover (a black L at 0.35 m) was fixed in `mesh_part` (per-face directions).
  - **Sign:** corners r 2; `rr_outline` with 0 corner steps (LOD1's sharp corners) divided by zero on the first build after the reboot, fixed in the common file.
- **Checks:**
  - **Reproducible:** the six scripts rebuilt all nine FBX into `W/bs4_hero/tmp/repro` (22:28): every LOD identical in positions, corner normals, triangles and used slots (`tmp/repro/geom_sig.py`), and the five JSON sidecars byte-identical. So `build_a` is exactly what the generators make (the sign's 20:27 build predates the last edit of the common file and is unchanged by it).
  - **Shading** of the exported FBX (builder B's `tmp/b2/g1check2.py`, read-only, and `tmp/repro/flatcheck.py`; log `logs/g1check_a.log`): every LOD0 face wider than 1 cm whose corner normals leave the face normal by more than 2° is on a part curved by design (cushion waterfalls, back-shell corner fillets and sides, arm-cap undersides, the back uprights' bends, the ashtray and stand lathes, the oak top's hole wall, which is a 28-gon cylinder under the ashtray's flange). Flat steel panels ≤ 2.5°, sign plate 0.4°, rack tabs 2.1°.
  - **Sidecars:** `lodDistances`, budgets, colliders, supports, anchors and tags as §2.1–2.3; no pile, no inspect; `station.line` "游戏视觉 bs4-hero-kits"; bounds within the §1.1 envelopes (±0.02).
  - **Surfaces:** the 7 new `.meta` GUIDs are fresh, unique (also against builder B's 2) and absent from the clone; every GUID the six `.mat` files cite resolves (the shader, `Prop_Vinyl_N`, `Prop_Vinyl_S`, `MacroWear_M`, `Prop_FabricCharcoal_N` in the clone; the cover atlas in `build_a`); LF only.
- **Renders** in `W/bs4_hero/renders/kits/<Kit>/<slate|navy|charcoal|today|new>/`, each folder with a `shots.txt` of every camera. `render/render_kits.py` imports the exported FBX and rebuilds each slot as `FrontRooms/Surface` reads its `.mat` (BaseMap × BaseColor, MaskMap.r × `_Smoothness`, BumpMap, UV in metres / `_TileSize`); Cycles Metal, AgX; jobs from `render/final_jobs.py`, run one Blender at a time by `render/run_final*.sh` (resumable):
  - stills in a closed station room (Station_Wall / Floor / crash rail / base, troffers on a 2.4 m grid), 64 spp, exposure −2.0: R1 3/4 at 3 m from eye 1.62 (vFOV 40); R2 3/4 back or side at 1.5 m (vFOV 62); R3 hero 0.35 m on LOD0 (vFOV 76), two or three per kit on the named hardware; R4 LOD1 at d01 and R5 LOD2 at d12 at the game's vFOV 76 (tandem 6 / 15 m, stand 4 / 10 m, rack 3 / 8 m); `f150_*` / `f400_*` at four azimuths, 1.5 and 4 m, game vFOV, 48 spp, on the LOD the game shows there; TODAY renders the stand-ins at the same cameras;
  - turntable frame sets in the studio, exposure −1.7: `tt035_00–07` (0.35 m on the named hardware: the Tandem3's +x end arm, the table's ashtray, the stand's bowl, the sign plate, the rack's top-left pocket; vFOV 76, 1080p, 64 spp, LOD0), `tt150_*` (1.5 m) and `tt400_*` (4 m, zoomed to frame the kit, so LOD0), both 40 spp, each with a `.txt` of its camera. The first pass (21:40–21:52) turned the rig the wrong way: azimuth +90 landed on the kit's −x, and one Tandem3 frame sat inside a seat back. `render_kits.py` b97c5859 turns it by −az; all five sets were re-rendered (22:26–22:48) and checked frame by frame;
  - `<Kit>_turntable.mp4`: 24 fps, 1280 × 720, h264 crf 18, `ffmpeg -an` (`ffprobe`: no audio stream in any): Tandem3 and 2Table a 4 s spin in the studio (8e694db0, 7fd2316f); stand with its sign, sign, and rack a 3–4 s orbit of ±60–70° in the room (ba470915, d12eb69d, a9f3977c).
- **Sheets** (Shot format; labels checked with `zl_option_sheets.fit_report`; `render/sheet_waiting.py`):
  - `W/bs4_hero/sheets/kits_waiting.jpg` (2559 × 3480, `WAIT-RED`, the tandem colour): TODAY (the navy task chairs) | T-B slate REC | T-A navy | T-C charcoal; rows row A 3 m, row B 3 m, back 1.5 m, hero seat, hero table, LOD1 6 m, LOD2 15 m, COST (tris and draws per LOD, mesh MiB);
  - `kits_waiting_props.jpg` (2559 × 3435, `PENDING`, no choice): stand + sign and rack against their absent TODAY;
  - both `_fit.txt` end ALL LABELS FIT. Their meta carries the module: Station_Waiting_3x2 tris 85,934 / 36,779 / 25,880 today → 86,102 / 34,626 / 24,466 with the five kits (§5 planned 83,294 at LOD0; the three kits over target make the +2,808); LOD0 draws 15 → 19, as §5 planned.
- **Look, at 1:1:**
  - tandem: at 0.35 m the urethane caps, the beam's end plug with its two bolt heads, the bevelled clamp cheeks, the waterfall front and the split welt read; from behind at 1.5 m the shells' lips, their four screws, the uprights' bolts and the leg gussets; the 1.5 and 4 m sets keep the beam, the T-legs and the arm rhythm; LOD1 at 6 m and LOD2 at 15 m keep the silhouette;
  - table: the flush ashtray's rolled rim and three rests, the T-mould bead and the oak grain hold at 0.35 m;
  - stand: the rests, the butts' cork tipping, the burnt match, the ash columns and flecks, the dome's bead and the chrome collars;
  - sign: the eased plate edges and the two slotted oval heads;
  - rack: the wire reads round at 0.35 m, the tab screws, the curled corner, every magazine behind its lips.
- **Open:**
  - the sign plate and the four covers stay blank until 平面视觉 · 信息设计's art lands (§3);
  - phone tier, off state, frame time and the in-module look (T-B against the walls in the set's light, §7) are Unity rows for the Stations stage.

### Stations (2026-11-03 22:50–23:35, account G, relay of account B's run)

- **What this stage did:** the eight kits in the two modules, seen in the real game in the clone, against TODAY (the BS4 stand-ins): the one Unity import of both builders' outputs, the §4 swap in the clone's module assets with the edit-mode checks, and Play-mode frames at the station cameras on desktop and the phone tier. No Blender run, nothing written under main, no git write, no sound (the clone's silencer and `m_DisableAudio 1`; product name private).
- **Clone `W/proj_bstations`, re-synced from main at 23:08.** The Play-mode frames need the game as main has it. The game is one compile unit, and main had moved since the clone's 11-02 sync:
  - 59 script files, among them `FrontRooms3DGame`, `FrontRoomsMapWorld`, `FrontRoomsRoomModuleData` (1200a918 → c9ab627b) and the zone lamps' `FrontRoomsLampKit` (the map's lamps, landed 11-03 17:16 and 18:47);
  - 2,096 resources (`Props/Lamps`, `WorldText`, fonts) and `Settings/FrontRooms_URP_Renderer.asset`.
  - Method: `rsync --checksum` of `Assets/{Scripts, Editor, Levels, Resources, Settings, Plugins, StreamingAssets, Scenes}` and `link.xml`, without `--delete`. Afterwards a checksum dry run finds the clone's `Assets` equal to main's except the silencer.
  - The 42 clone-only Editor files of earlier stages were moved, not deleted, to `W/_partial/proj_bstations_cloneonly_20261103_2306` (README there). ProjectSettings were left as they were. The modules and the profile were already main's bytes.
- **Import** (one Unity run, `W/bs4_hero/logs/u_check_today.log`, 0 compile errors): `build_a` and `build_b` copied in (44 files, none there before). Unity wrote the 26 `.fbx.meta` / `.json.meta` with fresh GUIDs, each unique in the clone. `W/bs4_hero/stations/out/check_today.txt` ends ALL CHECKS PASS:
  - every new kit is read by `FrontRoomsKitLibrary` (sidecar tris, slots, `lodDistances`, footprint, no pile);
  - each has a three-level `LODGroup` whose screen heights equal the sidecar's `lodDistances` (`FrontRoomsKitImporter`);
  - every slot is remapped to `Resources/Surfaces/<slot>.mat`;
  - no model carries a collider.
- **Mesh memory measured by Unity** (vertex streams + index buffers, all LODs; it replaces §5's estimate):

| Kit | tris LOD0 / LOD1 / LOD2 (imported) | draws per LOD | mesh MiB | `.fbx.meta` / `.json.meta` sha1 |
|---|---|---|---|---|
| `Kit_TandemBeam3` (`_Navy`, `_Charcoal` the same) | 10,099 / 2,268 / 264 | 3 / 3 / 3 | 0.661 | 307f14a1 / 9dffee80 |
| `Kit_TandemBeam2Table` (variants the same) | 8,991 / 2,034 / 228 | 6 / 5 / 3 | 0.597 | c28fd7fd / 4b4cac30 |
| `Kit_SmokingStand` | 3,876 / 796 / 112 | 5 / 3 / 2 | 0.243 | 095d31f3 / 743ab730 |
| `Kit_SignNoSmoking` | 166 / 10 / 2 | 2 / 1 / 1 | 0.010 | 44aaa09e / 591fffe4 |
| `Kit_MagazineRack` | 2,876 / 744 / 80 | 3 / 2 / 2 | 0.220 | 462dbddf / e1bfaddd |
| `Kit_GuestChairSled` (`_Burgundy` the same) | 5,176 / 1,192 / 360 | 3 / 3 / 2 | 0.299 | f79343a3 / 58fc4283 |
| `Kit_StationCounter_Ledge120` | 2,044 / 156 / 36 | 4 / 4 / 3 | 0.108 | a808e370 / c778cd14 |
| `Kit_Credenza_Laminate` | 3,254 / 662 / 36 | 4 / 4 / 2 | 0.204 | 086bc12a / a2e75e19 |
| stand-in `Kit_TaskChair_Navy` (for comparison) | 5,168 / 1,601 / 420 | 3 / 3 / 3 | 0.372 | (main) |

  Variant metas: `Kit_TandemBeam3_Navy` 10c5d6e6 / 9be578d6, `_Charcoal` 2afad5f5 / e2cd6e04; `Kit_TandemBeam2Table_Navy` 2e3b2a16 / 2985a053, `_Charcoal` 75b90d6a / 8d89338f; `Kit_GuestChairSled_Burgundy` c0cd2cf1 / ccd60785. The GUIDs are listed in `check_today.txt`.
- **The swap in the clone** (§4; `W/bs4_hero/stations/make_hero_modules.py`, a text edit of main's bytes, so the contract diff holds only the five Waiting entries, the Desk's two chairs and four appended entries, and the two notes):
  - `Station_Waiting_3x2.asset` 02932259 → b2ad6cf2, `Station_Desk_3x3.asset` 91c5a1ce → d553dfbd. Copies are in `stations/modules_today/` and `modules_hero/`. Unity left the bytes as written; the clone ends on the hero swap.
  - Edit-mode check (`check_hero.txt`, ALL CHECKS PASS): `Validate` gives 0 errors and 0 warnings on both modules. `FloorStationTemplate.From` is OK on the asset copy and through the profile's `FloorStationData` for Level0 and Office, with E1 / E2 as before.
  - Props LOD0 / LOD1 / LOD2 from the imported meshes: Waiting 85,918 / 36,763 / 25,864 → 86,086 / 34,610 / 24,450, colliders 12 → 10. Desk 69,606 / 30,326 / 23,298 → 76,414 / 30,994 / 23,866, colliders 8 → 10, props 29 → 33.
- **In the game** (Play mode in batch, the clone-only harness `Assets/Editor/StationHero/FrontRoomsStationHeroProof.cs`, the station-look-v1 harness's machinery taken to the B floors):
  - Run seed 516574485; floors changed as the blackout's end does it (`ChangeFloor`); world text in English (`-frLanguage en`; the clone's private prefs held Russian, and the first trial pass was discarded for it).
  - Floor 2 holds Station · Waiting at cells (90, −62) and floor 6 Station · Desk at (100, −29), the same in every pass, so TODAY and HERO frames share their cameras exactly.
  - `PropsLeftOutCount` 0 in all four passes. Waiting places 34 props. Desk places 29 today and 33 with the hero kits.

| Built set (desktop) | props LOD0 / LOD1 / LOD2 as built | LOD0 under `map.Station` (props, dress, trim, TV) | active renderers | LOD0 draws of the swapped props |
|---|---|---|---|---|
| Waiting today | 86,016 / 36,765 / 25,866 | 86,580 | 98 | 15 (5 chairs) |
| Waiting hero | 86,184 / 34,612 / 24,452 | 86,748 (cap 100,000) | 98 (cap 120) | 19 |
| Desk today | 69,672 / 30,328 / 23,300 | 70,236 | 80 | 6 (2 chairs) |
| Desk hero | 76,512 / 30,996 / 23,868 | 77,076 | 92 | 19 |

- **Frames** (`W/bs4_hero/stations/out/<today|hero>_<desktop|phone>/`, PNG, each pass with `frames.txt`: camera, vFOV, lodBias, and per kit the LOD it shows, its distance and whether it is seen, judged by colliders and not by the frustum alone; plus `measure.txt` and `log.txt`):
  - the 72 §1 cameras C1–C4 from the game's own eye (the camera stands 1.65 m over the floor), vFOV 76. C1 stands 4.5 m out on E1's axis, unblocked in both modules. Each module also gets eye views of every kit at about 2.5 m (and the W wall, M-B's spot).
  - desktop 1920 × 1080 at quality Ultra, lodBias 2, preset Ultra.
  - phone tier 2622 × 1206 with the handheld layer forced (iPhone 16 Pro), quality Medium (the iPhone default, lodBias 0.7) and preset High.
  - on desktop only: 0.35 m views at the builders' R3 cameras in each kit's own space (LOD0), and the off state (C2 after the key take, the set off for good). In the off state the frame's mean luminance drops 6 % in Waiting and 3 % in Desk.
  - in the hero pass, the choices: T-A navy and T-C charcoal by slot swap at C2, rows A and B and the 0.35 m view; M-B, the rack moved to the W wall at z 1.45; S-B burgundy sled chairs.
  - Each pass took 1.5–2 min. The machine was under load, so frame time was not measured.
  - Two rounds: the first marked a kit as in frame by the frustum alone, which listed kits behind walls in C1. The harness then gained the collider test, and the second round (23:27–23:32) re-made every frame. The runs are deterministic: the measures equal the first round's. The first round's logs are kept in `stations/out/round1/`.
- **Sheets** (Shot format, `WAIT-RED`, `W/bs4_hero/stations/stations_sheet.py`, labels checked with `zl_option_sheets.fit_report`, both `_fit.txt` ALL LABELS FIT): `W/bs4_hero/sheets/stations_waiting.jpg` (2559 × 6726, 327010b4) and `stations_desk.jpg` (2559 × 5094, acc154b8).
  - Columns: TODAY desktop, HERO desktop (REC), TODAY phone, HERO phone. Rows: C1–C4 and the eye views.
  - Then the 0.35 m and off-state pairs; then the choices in the room's light (T-B / T-A / T-C, M-A / M-B; S-A / S-B, C-A / C-B).
  - COST last: props as built, the set's LOD0 and renderers, placed and left out, the change; per kit tris per LOD, draws, Unity MiB.
- **Look, at 1:1 in the game's light:**
  - the tandem rows read as period beam seating (T-legs, arm caps, the beam table's flush ashtray), clear of the walls and on the floor. T-B slate reads blue on the green-grey walls and lighter than the navy stand-ins. T-A navy reads as a second navy mass, and T-C charcoal by value only.
  - the stand sits under its plate on the south wall; the plate is blank until its art.
  - the rack hangs above the crash rail, the first thing on the left inside E1 (M-A); at M-B it is not seen from E1 or E2.
  - the sled chairs, the ledge with the register and the tent card (PLEASE SIGN IN), and the credenza under the key cabinet read as planned. The ledge's end panels pass round the rail.
  - Under the Dim west lamps the teal and the beige laminate read darker and greener than their albedos.
  - **Flaw:** the sled chairs' chrome runners break into a beaded highlight at 2–3 m: specular aliasing on a 6 px tube of `Prop_Chrome` (smoothness 0.85, shared with every chrome kit). At 0.35 m the tubes are clean.
  - **Phone tier:** at lodBias 0.7 the rows show LOD1 from 4.2 m and LOD2 past about 10 m, the sled chairs LOD1–2 from E2. The stand-ins did the same at their own distances.
- **Open, for the Package stage or later:**
  - the contract to 关卡设计 (`W/apply/bs4_hero_contract/`, the diff of `stations/modules_today` → `modules_hero`);
  - 游戏视觉's package of the 13 kits and 7 surfaces with the Unity-written metas from the clone;
  - the `Prop_Chrome` specular aliasing (a material or shader item, not this package);
  - the plate and cover art from 平面视觉 · 信息设计;
  - frame time, desktop and phone, in the combined test (`W/vl_pending/bs4_hero_kits.md`).

### Package (2026-11-04 01:15–06:15, account G; the Mac idled on a 1–4 % battery and slept 04:00–05:46, so the stage ran in two bursts)

- **What this stage did:** 游戏视觉's apply package `bs4_hero_kits` and the contract `bs4_hero_contract` to 关卡设计, land-ready and not landed (the parent session lands). No Unity run; one Blender run (the reproducibility rebuild below, into scratch); nothing written under main; no git write; no sound.
- **Inputs checked first:**
  - the clone's 44 copied files equal `build_a` / `build_b` (`tmp/stations_copied.txt`), and each has the `.meta` Unity wrote at the 23:09 import;
  - main has not moved under the package since the Stations stage: `Assets/Scripts`, `Editor`, `Resources/Surfaces`, `Resources/Rendering`, `Settings`, `Props/Models` (by size) and `Tools/Blender/frontrooms_kit` equal the clone (apart from our new files), `Levels` too apart from the two modules the clone holds swapped; 0 iCloud placeholders in main's `Assets`.
- **Reproducible:** builder B's three scripts rebuilt their four FBX into `W/bs4_hero/tmp/repro_b`: the geometry signature (builder A's `tmp/repro/geom_sig.py`: positions, corner normals, triangles, used slots) is IDENTICAL for all 12 LOD objects; the four JSON sidecars and both `.mat` are byte-identical. `gen_bs4_waiting_surfaces.py` regenerated the six surfaces and the atlas into scratch: the PNG and five `.mat` byte-identical; `Prop_MagazineCover.mat` differs only in the atlas GUID it cites (a fresh root; a re-run in the project keeps its GUIDs). With builder A's 22:28 check, every shipped file is what its generator makes.
- **The package** `W/apply/bs4_hero_kits.sh` (apply convention v2, made by `W/tools/mkapply.py`): 81 new paths, nothing replaced, nothing kept, no C#.

| Group | Folder | Files |
|---|---|---|
| Kits | `Assets/Resources/Props/Models/` | 13 × (`.fbx`, `.fbx.meta`, `.json`, `.json.meta`) = 52: `Kit_TandemBeam3`, `Kit_TandemBeam2Table` (each + `_Navy`, `_Charcoal`), `Kit_SmokingStand`, `Kit_SignNoSmoking`, `Kit_MagazineRack`, `Kit_GuestChairSled` (+ `_Burgundy`), `Kit_StationCounter_Ledge120`, `Kit_Credenza_Laminate` |
| Surfaces | `Assets/Resources/Surfaces/` | 8 × (`.mat`, `.mat.meta`) = 16: `Prop_VinylSlate`, `Prop_VinylNavy`, `Prop_VinylCharcoal`, `Prop_AshSand`, `Prop_SignNoSmoking`, `Prop_MagazineCover`, `Prop_VinylTeal`, `Prop_VinylBurgundy` |
| Atlas | `Assets/Resources/Surfaces/Textures/` | `Prop_MagazineCover_A.png` + `.meta` = 2 |
| Generators | `Tools/Blender/frontrooms_kit/assets/`, `Tools/lookdev/` | 10 asset scripts (`_station_waiting_common`, `tandem_beam`, `tandem_beam_table`, `smoking_stand`, `sign_no_smoking`, `magazine_rack`, `_station_desk_common`, `guest_chair_sled`, `station_counter_ledge`, `credenza_laminate`) + `gen_bs4_waiting_surfaces.py` = 11 |

  - Writes the 35 `.meta` first, then the assets, then the scripts. The 35 GUIDs are fresh: none of main's 3,220, none twice.
  - Every GUID the `.mat` and `.fbx.meta` files cite (78 references) resolves: 16 in main (the `FrontRooms/Surface` shader, `Prop_Vinyl_N`, `Prop_Vinyl_S`, `MacroWear_M`, `Prop_FabricCharcoal_N` and the 11 reused materials of the FBX remaps), the rest inside the package.
  - 22 API rows: the dry run refuses if one of those 16 GUIDs leaves main, or `Prop_Vinyl.mat` (both generators' template), `FrontRoomsKitImporter`'s use of the sidecar `lodDistances`, or the helpers the generators call (`kitlib.register_slot`, `_station_common.station_meta` and `lods`, `_newcase.plate`).
  - Text files LF only.
- **Gate (rule 25):** no C# in the package or the contract, so no Roslyn pass. DRY RUN OK on main (05:55). Rehearsal with `APPLY_REAL` on a scratch copy (main's 3,220 Asset metas, the API files and both modules): DRY RUN OK, `--apply` MANIFEST OK (81 paths), then ALREADY APPLIED. Six refusal cases pass (`tmp/pkg/negative_tests.sh`): a GUID already used, a new path already taken, an API gone, a tampered payload, the wrong target, half applied. The same 81 files imported in the clone with 0 errors (Stations stage, ALL CHECKS PASS).
- **The contract** `W/apply/bs4_hero_contract/` (`README.md`, `CONTRACT_map.diff` 55f636ea, 5 hunks): §4's swap against main's `Station_Waiting_3x2.asset` 02932259 and `Station_Desk_3x3.asset` 91c5a1ce (unchanged since 10-31). `patch -p1 --dry-run` passes on main's bytes after our package; the patched files are b2ad6cf2 / d553dfbd, the modules Unity validated in the Stations stage; `-R` undoes it.
  - Order: our package first. With the contract alone, `Validate` only warns "no footprint known (asset missing?)" and the station places nothing for the new names.
  - The README gives the hunks, an unchanged / new table, the numbers, the checks 关卡设计 runs (`FloorStationTemplate.From` for both themes, `PropsLeftOutCount` 0 with 34 and 33 placed, BS1's fit table unchanged, `play:2554:Exit`; their BS4 switch test and the designer views need no edit) and Red's four open choices with the one-word or one-entry change each needs.
  - M-B (the rack on the W wall at z 1.45, yaw 90) passes the Python mirror of the module rules (errors 0, warnings 0, walk OK); Unity's `Validate` ran on M-A only.
- **To copy into main** (the parent's step, one LANDED row): this file, and the five sheets with their fit files into `research/station/sheets/`: `hero_kits_waiting.jpg`, `hero_kits_waiting_props.jpg`, `hero_kits_desk.jpg`, `hero_stations_waiting.jpg`, `hero_stations_desk.jpg` (the same bytes as `W/bs4_hero/sheets/kits_*.jpg` and `stations_*.jpg`, each `_fit.txt` ending ALL LABELS FIT).
- **Combined test** (after the package and the contract land; frames and numbers as `W/vl_pending/bs4_hero_kits.md`):
  - import: the 13 kits with 0 console errors, each a three-level `LODGroup` at its sidecar's `lodDistances`, every slot on its `Resources/Surfaces` material;
  - 关卡设计's BS4 switch test and the station views green with the hero modules;
  - on B floors: `PropsPlaced` 34 (Waiting) and 33 (Desk), `PropsLeftOutCount` 0, LOD0 under `map.Station` about 86,748 and 77,076 (cap 100,000), renderers 98 and 92 (cap 120);
  - the frames C2, C4, the eye views, the off state, the sled runners in motion, the phone tier (LOD1 from about 4 m, LOD2 past about 10 m, no pop walking in);
  - frame time at each station on desktop and phone (not measured in the clone).
- **Open:**
  - the landing (CLAIM, `--apply`, LANDED, MERGED row), then 关卡设计's contract;
  - Red's four choices (T, S, M, C; §7), each a follow-up diff from us if Red's pick differs from REC;
  - the plate and cover art from 平面视觉 · 信息设计;
  - the `Prop_Chrome` specular aliasing at 2–3 m (a material item, not this package).
