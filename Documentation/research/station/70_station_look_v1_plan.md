# 70 · Station look v1: the Front Desk, build plan (游戏视觉, 2026-10-29)

**What this is.** The plan stage of 游戏视觉's `station-look-v1` workflow: the visual side of A26, Floor 1's first room.
- No Unity was run and nothing was built (plan stage). **Build stage, 2026-10-29 16:2x: the 24 plan-check changes are applied in place below; §17 lists each with how it was applied. The build report is `71_station_look_v1_build.md`.**
- This file is the only file written in main.
- The build stage turns it into one package, `W/apply/station_look_v1.sh`. That package lands as **one group** with 关卡设计's switch-on (`FrontRoomsLevelProfile.stationModule` = our asset).
- Red's §16 rule applies: the first room, the start group, M3, M6 and M7 land together after whole-set verification (`61_for_red_first_room.md` top box; `tutorial/20_requests.md` 2026-10-22 21:11).

**Rule for every row.** Build exactly what the decided sources say.
- A departure from them is a defect.
- Where the sources leave a choice, the row says **GAP n** and §16 gives the smallest consistent choice.
- Every row cites its source.

## 0. Sources, keys and bases

**Authority order:** DEC, then 61, then the rest.

| Key | File (all under `Frontrooms3D/Documentation/research/` unless the path is absolute) |
|---|---|
| **DEC** | `station/NOTE_1022_first_room_DECIDED.md`, with all its updates and addenda (15:3x, 15:4x walls 2, 15:5x, 16:0x palette, 16:5x, 18:0x FD-b, 18:1x plate, 18:3x riser, 18:4x final map plan, 20:28 waiver and N = 2, 20:4x R12–R15) |
| **61** | `station/61_for_red_first_room.md` rev 3: the PRODUCTION SPEC. Red decided §14–§17 as recommended (top box) |
| 20L | `station/20_layout.md` v1.6 (G1–G10 §3; FD-b §4.5; checks §8) |
| 30L | `station/30_look.md` (kit rows S01–S40 §1, §10.3; materials §2; V3 light §3.1 and r7 §10.1; take cluster §10.2) |
| PAL | `station/03_palette.md` (叙事's E15 verdicts) |
| PICK | Red's pick image `station/images/first_room_s4_walls2_post_room.jpg` ("这个版本可以", 15:44). Its approved blockout values come from `/Users/redwang/FrontRoomsVisualWork/station_first_room/scripts/station_walls.py`, direction S2 (`mat_paint`, `band_run`, `base_run`, `casing_E1`, `corner_guards_E1`, `reducer_E1`, `header_band_E1`) |
| KP, PH, PLL, NM, NN, NP, RED | `station/NOTE_1022_key_plate.md`, `_photos.md`, `_first_room_placard_layout.md`, `_first_room_map.md`, `_first_room_narrative.md`, `_first_room_placard.md` (平面视觉's 7 points), `_red_first_room.md` |
| 01 | `station/01_research.md` §6 (the station strings, 叙事) |
| 50 | `station/50_critic_fixes.md` §6 (the take, one timing) |
| T10, T20, V4R | `tutorial/10_proposal.md` (§3, §4.2, §4.6, §4.7), `tutorial/20_requests.md` (V1–V6), `tutorial/README_V4_FloorHasStation.md` |
| KIT | `/Users/redwang/FrontRoomsMapWork/integration/s22_a26/KIT_NOTE.md` (Open items; 游戏视觉 1–6 and the 平面视觉, 交互设计 and 声音设计 items) |
| PLAN | `/Users/redwang/FrontRoomsMapWork/notes/floor1_start_group_plan.md` (§4.1–§4.11, §7, §9, §10: Step 0 → 4 × 4) |
| SIGNS | `/Users/redwang/FrontRoomsVisualWork/info_type/stair_signs/out/first_room/README.md` + `layout.json` (信息设计's plate and letters) |
| R3C | `/Users/redwang/FrontRoomsVisualWork/apply/r3_station_tv_contract/README.md` (the R3 run's station-TV contract, same line) |
| MD | `Assets/Scripts/FrontRoomsMap/FrontRoomsRoomModuleData.cs`: `ValidateStation` (l. 443–524), `ValidateMarkers`, `Walk` |
| SS, SG, MC, MW | `FrontRoomsStationSet.cs`, `FrontRoomsStartGroup.cs`, `FrontRoomsMapCarve.cs`, `FrontRoomsMapWorld.cs` (station block l. 1056–1373) |
| PM, PL, PG | `Assets/Scripts/Rendering/FrontRoomsPlacardMount.cs`, `FrontRoomsPlacard.cs`, `FrontRoomsPlacardGlow.cs` |
| MV | `Assets/Editor/FrontRoomsMap/FrontRoomsMapVerification.cs` `StandInStation` (and `FrontRoomsMainScenePlaytest.StandInStation`) |
| LMS | `Documentation/LEVEL_MODULE_SPEC.md` §7–§9 |

**Main bases (sha1, read 2026-10-29 15:3x; no file was dataless):**
- Game `3b125479`; MapWorld `527ddb39`; MD `ecd8221a`; SS `5f66bd52`; SG `5cf8ec66`; MC `4ee62fd9`.
- PM `400225ef`; PL `6ba020d4`; PG `c03448c9`; `FrontRoomsStationPresence.cs` `4c0563dc`; `FrontRoomsDeskStill.cs` `fbea83aa`.
- `FrontRoomsSurfaces.cs` `ab279371`; `FrontRoomsLook.cs` `898dee87`; `FrontRoomsZoneReflection.cs` `040ba42b`; `Inspect/FrontRoomsInspectCatalog.cs` `361f6006`.
- `Media/FrontRoomsScreenVideo.cs` `29e76f79`; `FrontRoomsRoomStream.cs` `4e6e8e7f`; `Assets/Settings/FrontRooms_URP.asset` `18584f2b`; `FrontRoomsTransitionKit.cs` `6ef32ddd`.
- `Assets/Levels/FrontRoomsLevel0.asset` `07514c33` (stationModule empty); MAP_GENERATION.md `91711137`; LEVEL_MODULE_SPEC.md `53ef2ea1`.

**Coordinates.** Module metres from the station's south-west cell corner:
- x runs east 0–12 and z north 0–12; y is height above the floor;
- walls are 0.16 m thick on the cell lines (`ModuleUnits.WallHalf` 0.08), so the inner faces are at x 0.08 / 11.92 and z 0.08 / 11.92;
- ceiling 2.90 m (Standard); props ≤ 2.85 m (`CeilingClearance` 0.05);
- yaw 0 means the front faces +Z, so yaw 180 faces south toward D and yaw 270 faces west.

**Already decided, not re-opened here:**
- footprint **4 × 4** (DEC 18:4x; PLAN §10 "Footprint: 4×4 (游戏视觉's pick)");
- walls 2 · Post Room (DEC 15:4x);
- FD-b (DEC 18:0x; 20L §4.5);
- N = 2 with the key taken (61 top box);
- the light layer: test first, then A on every platform if it leaks (61 top box, §15).

---

## 1. The module asset: `Assets/Levels/Modules/Station_FrontDesk_4x4.asset`

A `FrontRoomsRoomModule` asset, authored like `L0_WaitingRoom_4x3.asset`:
- byte arrays are stored as hex;
- `ModuleEdge` Wall 00 / Open 01 / Arch 02;
- `ModuleLamp` Steady 01 / Dim 05 / Off 06.

It needs a new `.meta` with a GUID that is not in main.
- The markers go in through the **inspector array**. The Level Designer's plan view cannot place the three new kinds (KIT 游戏视觉 1).
- It is held only in `profile.stationModule`, never in the pool (PLAN §4.9; KIT 游戏视觉 1).

| Field | Value | Source |
|---|---|---|
| `notes` | `Floor 1 first room (A26, FD-b): the building's security post at its entrance; walls 2 Post Room via the station skin. Plan: research/station/70_station_look_v1_plan.md` | 61 one-line summary; NN §1 phrasing ("the one *kept* room") |
| width × depth | **4 × 4** | DEC 18:4x; PLAN §10; MD `StationWidth`, `StationMaxDepth` |
| height, theme | `1` Standard, `0` Level 0 (the skin is its own) | MD l. 446 |
| columns, fill | `1` None, `0` None | MD l. 447–448; 30L §7 (no column cutting sight lines) |
| weight, allowRotate | `0`, `0` | MD l. 449–450 |
| minTier, maxTier | 0, 4 (defaults; unused for a station) | — |
| `south` | `00000000` (whole; the start door is the title's) | MD l. 457–458; PLAN §4.1 |
| `east` | `00000000` (whole) | MD l. 459–460 |
| `north` | `00000200`: **E1 = Arch at column 2**, the span fixed at 1.7 m by the map | MD l. 461–466; SG `e1Width` 1.7; KIT 游戏视觉 1 |
| `west` | `00020000`: **E2 = Arch at row 1**, span 1.2 m | MD l. 467–472; SG `e2Width` 1.2; 61 §4.2 (row 1 for G2's 60°) |
| `innerEast` (12) | `010101010101010101010101`, all Open. That includes the edge west of D (index 1, G1) | MD l. 473–474; FD-b has no inner walls (61 §4.4) |
| `innerNorth` (12) | `010101010101010101010101`, all Open. That includes the entrance column, the walk from D to E1 | MD l. 475–477 |
| `lamps` (16, i + 4j) | `05010501` `05050505` `05050505` `05010606` (table below) | 61 §4.4 row 灯; PLAN §4.6; SG `StandInLamp` |
| `props` | §3, P01–P26 | §3 |
| `markers` | 4 rows below | MD l. 487–505 |

**Lamps** (column i west to east, row j south to north; cell centre in metres):

| j \ i | 0 (x 1.5) | 1 (x 4.5) | 2 (x 7.5, axis) | 3 (x 10.5) |
|---|---|---|---|---|
| 3 (z 10.5) | Dim | **Steady** (the placard cell) | **Off** (the axis cell under E1) | **Off** (the TV / riser cell) |
| 2 (z 7.5) | Dim | Dim | Dim | Dim (the counter) |
| 1 (z 4.5) | Dim (E2) | Dim | Dim | Dim |
| 0 (z 1.5) | Dim | **Steady** (beside D) | Dim (D's cell) | **Steady** (beside D) |

- Sources: 61 §4.4 ("D 两边的两格常亮；疏散图那一格常亮；电视那一格和轴线那一格关；其他暗档"); 20L §4.5 Lamps; NP point 5 (a steady lamp on the mount wall); KP addendum 12:1x (≥ 2 Steady on the entrance side).
- MD l. 479–485: no Auto, and 2 Steady on row 0.
- W1 then has 3 Steady + 11 Dim live lamps (≥ 2, `FrontRoomsWarnLamps`).

**Markers** (module metres; `tag` and `host` empty unless given):

| Kind (byte) | x | z | y | yaw | host | Why | Source |
|---|---|---|---|---|---|---|---|
| KeySpot (0) | **10.23** | **10.40** | **1.42** | 270 | `Kit_KeySwitchPlate` | The key on the switch plate's hook, on the riser's west face (x 10.30), at 61's z 10.4 (check C2-7). The plate's sidecar has a **body-only collider box** (backer + box + plate, 0.044 deep), so its footprint ends at x 10.256 and the KeySpot, 0.07 proud, is outside it (check C2-2; the module prop stays `noCollider`). Outside the riser, below 1.8 m; the take point 0.5 m in front is 61's (9.73, 10.40). **GAP 1** | 61 §4.4 key-board row (hook 1.42; KeySpot not in the riser or the board's footprint; ≤ 1.8 m; reachable within 0.9 m of the take point); DEC 18:3x ("the KeySpot per the limits … M6's hunt aimed at the take point"); MD l. 57 (a KeySpot is where the key lies); 30L §9.2 item 8 |
| StationBeacon (2) | 9.35 | 11.32 | 2.46 | 180 | — | The screen's centre after the 20° tilt on the raised hinge (§8; check C1-8). Only its cell (3, 3) is read (`StationBeaconCell`) | 61 §4.4 TV row; MW l. 1204; MV stand-in (9.35, north − 0.4, 2.3) |
| Placard (3) | 5.50 | 11.92 | 1.56 | 180 | — | The north wall face west of E1, facing D. PM adds `FrontRoomsPlacard` Proud 0.5 mm along the facing | 61 §4.4, §8; DEC 20:28 (waiver granted); PLAN §4.8; PL `CentreHeight` |
| StationEntrance (4) | 7.50 | 0.00 | 0.00 | 0 | — | D's centre on the south wall (column 2, south row) | MD l. 496–500; PLAN §4.1; MV stand-in |
| RelayEntry (1) | — | — | — | — | — | none | MD l. 503 |

**ValidateStation, line by line** (MD l. 443–524):

| Rule | This asset |
|---|---|
| 4 × 4 or 4 × 3 | 4 × 4 |
| Standard, Level 0 | yes |
| columns None, fill None, weight 0, never turned | yes |
| south and east whole | yes |
| one north arch at column 2, one west arch at row 1 | yes |
| the inner edge west of D Open | yes |
| the entrance column Open | yes |
| no Auto lamp; ≥ 2 Steady on row 0 | 2 Steady |
| markers: 1 KeySpot / 1 StationBeacon / 1 Placard / 1 StationEntrance (column 2, z ≤ 3) / 0 RelayEntry | yes |
| nothing over 1.50 m within x 6.9–8.1 | no prop has any part in x 6.9–8.1 (§3) |
| no CRTTV in x 6–9 | TV x 9.04–9.66 |
| a hung screen's p.y ≥ 2.15 | TV (`Kit_CRTTV_Station`) p.y 2.33 |
| the general rules | every prop inside 0.08–11.92 and ≤ 2.85 m (**p.y + boundsMax.y**, checked per row: floor-origin wall kits stand at y 0 with their heights baked in, check C2-1); no collider prop in E1 / E2 / D keep-clears; the walk is connected; the KeySpot is reachable within 0.9 m and is not inside P03's (body-only) footprint |

**Before ValidateStation can see any prop, every kit must exist with a sidecar in `Resources/Props/Models`.** A missing footprint is only a warning, and the axis and screen checks then skip that prop (MD l. 511–512).

## 2. The room in metres (what the map builds; reference for every row below)

```
 z12 +----------------------[==E1 x6.65-8.35==]------+########+   EXIT: N = 4 cells due north of E1 (12 m), flat sign facing south
     | clock      PLAN      :  axis x6.9-8.1 :  [TV]  A# riser  #   TV centre x9.35, bottom >= 2.15, 20 deg down
     | x3.0       x5.5      :  (nothing>1.5) :   K    ## x10.3- #   A annunciator z11.40 / K key plate + KeySpot z10.40 (riser W face x10.30)
     |                      :                :    t   ## 12.0   #   t take point (9.73, 10.40)
     |                      :                :        #########     riser S face z9.90: hook H, sign-out S, key cabinet C
 z9  +                      :                :    g   H S  C  |   g guard chair (9.90, 8.80) turned to the TV
     |                      :                :              NB |   NB notice board (E wall, z8.40)
     |                      :                : ==SECURITY==    |   counter x8.90-10.85, z7.20-7.80 (aisle 1.07 m east)
 z6  +                      :                :                 |
 E2  = z3.9-5.1             :                :                 |
     |                      :                :                 |
 z3  +                      :                :                 |
     |       [Steady]       :    D cell      :   [Steady]      |
 z0  +-------[plate]----[ D x6.30-8.70 (stream door, 2.4 m) ]--+
     x0   x5.77-6.23        x7.5                              x12
```

| Element | Metres | Source |
|---|---|---|
| D | the stream's 2.4 m double door, x 6.30–8.70. Leaves 1.12 m, opening into the room to z ~1.14; height 2.62. Header 2.66–2.90 (`DoorHeaderHeight` 0.24) | 61 §4.1; `FrontRoomsRoomStream.cs` l. 19–33; SS `StreamDoorWidth`, `StreamHeaderBottom` |
| South liner (start-area phase) | render-only, in the station wall skin, face at z 0.024 (`FacadeFace` 0.02 + `LinerProud` 0.004), height 2.90. West panel x 0.08–6.30, east panel x 8.70–11.92. The header over D stays bare | SS `Build`; KIT 游戏视觉 5 |
| Static entrance (after `ClearStartArea`) | stand-in leaves 1.12 × 2.62 × 0.05 on the data wall's inner face (z 0.08), jambs and a head in `trim`. It replaces the liner | SS `Build`, `StartAreaCleared`; PLAN §4.7 |
| E1 | north wall, centre x 7.5, clear 1.70 (x 6.65–8.35), arch top 2.20, header to 2.90. Keep-clear `E1Clear` x 6.65–8.35, z 10.92–12 | SG `e1Width`; MW `ArchOpening`; `ModuleUnits.ArchTop`; SS |
| E2 | west wall, centre z 4.5, clear 1.20 (z 3.90–5.10), arch top 2.20. Keep-clear x 0–1.08 | SG `e2Width`; SS |
| Axis | x 7.5. Strip x 6.9–8.1: no part over 1.50 m. Column x 6–9: no CRTTV | MD l. 428; SS `AxisRule` |
| Riser chase | map-built solid, x 10.30–12.00, z 9.90–12.00, floor to 2.98 (2.90 + half slab). Face material = the station wall skin. Registered as architecture. West face x 10.30 (exposed z 9.90–11.92); south face z 9.90 (exposed x 10.30–11.92) | SS `RiserWidth` 1.7, `RiserDepth` 2.1; DEC 18:3x; KIT 游戏视觉 3 |
| Door sweep | x 6–9, z 0–1.28: nothing at any height | SS `DoorSweep` |

---

## 3. Module props (placed by the map's set piece, `FrontRoomsStationSet.StepProps`)

Positions are each prop's pivot in module metres. "Size" is W × H × D. LOD budgets are LOD0 / LOD1 / LOD2 triangles.
- Wall pieces have their back on the wall face and `noCollider` = true unless stated.
- Desk-top items carry no collider (LMS §7).
- Props ≤ 0.3 m cast no shadow (LMS §8).
- Status: **reuse** = an existing kit as it is; **variant** = an existing mesh with another material, or a cut of one; **new** = a new hero model in the Blender kit (`Tools/Blender/frontrooms_kit/assets/<module>.py`, built with `build_asset.py`: FBX + sidecar, LODs, colliders, check render).

### 3.1 The TV and the key-take cluster (tier G: carries G1–G10)

| # | Kit | Status | x / z / y, yaw | Size, details | Materials | LOD0/1/2 | Source |
|---|---|---|---|---|---|---|---|
| P01 | `Kit_TVWallBracket` | **new** (`tv_wall_bracket.py`). **Origin: the wall face at floor level, heights baked in** (host convention) | 9.35 / 11.92 / **0**, 180 | Wall plate (y 2.20–2.40) + a 0.56 × 0.45 platform tilted **20° down**, a strap over the set, and a `set_mount` anchor (the hinge, 0.05 m off the wall, at **y 2.36**, pitched 20°). **Lowest point of bracket + set ≥ 2.19**, top of the set 2.83 ≤ 2.85 (check C1-8 / C2-9) | `Prop_SteelBlack` | 3,000 / 1,000 / 120 | 30L S02, §10.2 (bottom 2.15); 61 §4.4 TV row (20° down); PLAN §4.9 (TV bracket bottom ≥ 2.15) |
| P02 | **`Kit_CRTTV_Station`** | **hero pass as a station-only kit** (`crt_tv_station.py`): a new name containing `CRTTV`, so ValidateStation's screen rules apply and every pile's `Kit_CRTTV` stays as it is (check C2-4). Exactly `Kit_CRTTV`'s bounds (x ±0.31, y 0–0.50, z −0.241…0.2475, knobs and feet inside) and its `screen` anchor (0, 0.284, 0.245) (check C2-9). **No pile block** | module data: 9.35 / 11.63 / **2.33**, 180 (level, so ValidateStation sees x 9.04–9.66 and p.y 2.33). **At run time the dresser re-seats it on P01's `set_mount`** (§8). **GAP 3** | After the tilt: lowest point 2.19, top 2.83, screen centre (9.35, 2.46, 11.32). Hero pass: bezel and mask, tuner and volume knobs, rod socket, rear vents; curved CRT glass with a soft reflection | **beige plastics** `Prop_PlasticBeige` (PAL verdict "CRT TV, desk plastics": putty or beige; DEC 16:0x; check C1-9), `Prop_Rubber`, `Prop_ScreenCRT`, `Prop_GlassCRT` (no brand, no label text) | 9,000 / 3,000 / 400 | 30L S01, §10.3 S01; 61 §4.4 (centre x 9.35, bottom ≥ 2.15, 20° down); MD l. 518–522; PAL |
| P03 | `Kit_KeySwitchPlate` | **new** (`interact_key_switch_plate.py`, same host convention as `interact_key_board.py`: **origin the wall face at floor level, heights baked in**) | 10.30 / **10.40** / **0**, 270 (riser west face) | A 0.30 × 0.40 painted-steel backer (y 1.25–1.65). On it, a 0.15 × 0.22 steel plate on a 4 in box with **one** hook on a plunger switch: `key_hook` at y 1.42, 0.07 proud (x 10.23). A ½ in EMT stub leaves the box downward. A `plate_sign` anchor sits above the plate. **Sidecar collider = the body only (0.044 deep)**, so the footprint stops short of the hook and the KeySpot (check C2-2). Tags `interactable`, `key_host`, `wall_decor`; LOD 3 / 8 / 40 m. p.y + top = 1.65 | backer `Prop_SteelPutty`; plate `Prop_Aluminium`; hook `Prop_Chrome` | 3,500 / 1,200 / 150 | 30L S10, §10.2 S10, §9.2 item 8; 61 §4.4 key-board row (hook 1.42, 1.2 m south of the screen plane) |
| P03a | the plate `STAIR KEY` / `SILENT ALARM: REMOVAL SIGNALS PANEL` | **variant** of `Kit_DoorSign` at 0.30 × 0.10, a child of P03 at `plate_sign` (y ≈ 1.53–1.63) | — | Engraved plastic; art from 平面视觉 (type size by the EXAMINE readability rule). **Close-reading layer only**: readable in EXAMINE or within ~1 m; its aim box never takes the key's aim; it stays after the take; it never glows. Built into P03's mesh (no separate kit) | **its own slot `Prop_StationKeyPlate`** (blank until 平面视觉's art; never `Prop_SignEngraved`, which is `Kit_DoorSign`'s shared P-4 slot, check C2-4) | 664 | 01 §6; KP 1–4; T10 §4.7; 30L S21 |
| P04 | `Kit_FireAnnunciator` | **new** (`fire_annunciator.py`; **origin the wall face at floor level, heights baked in**: body y 1.25–1.75) | 10.30 / **11.40** / **0**, 270 (centre y 1.50, **1.0 m from the key toward the screen plane**; z 11.21–11.59 on the riser face) | 0.38 × 0.50 × 0.10. A **beige cabinet with a beige door**, a narrow door frame, **no glazing**. Nine round frosted Ø 14 mm lenses (zones + TROUBLE), **never lit**. Narrow engraved label strips #C9C3B2. The POWER pilot is an incandescent **green glass jewel with a faint warm filament** (not an LED green): emissive only, steady, no realtime light, no bloom flare. It **stays lit after the take** and never dips in W1. Matte throughout. Bottom knock-out for the conduit. No brand | cabinet **`Prop_SteelBeige`** (new, GAP 8); labels #C9C3B2; jewel **`Prop_JewelGreen`** (new, emissive) | 6,000 / 2,000 / 200 | 30L S12, §4.3, §10.2; PAL (annunciator ADJUST: beige + beige door); DEC 16:0x; PH (conduit from below) |
| P05 | `Kit_ConduitRun_StationKey` | **new**, one static run built from the existing `outlet_conduit.py` / `outlet_conduit_strap.py` geometry (GAP 9). **Origin the wall face at floor level** | 10.30 / 10.40 / **0**, 270 | ½ in EMT: down from P03's box bottom (~1.29) to y 1.00, then 1.0 m north along the riser face at y 1.00 (7 cm over the crash rail's top), then up 0.25 m into P04's bottom knock-out with a connector. 3 straps. Never from the top or the side | `Prop_Aluminium` | ~2,000 / 700 / 0 | 30L S13, §10.2 S13; PH "conduit from below"; 61 §4.4 (导线管 on the riser's west face) |
| P06 | the stair key | **not a module prop.** M3 spawns it at the KeySpot on P03's `key_hook` in D1.6's hung pose (contract note §14). **Its renderers get the Station bit** (§9; check C1-3): M3 parents it under `map.Station`, and until then the dresser finds `Kit_Key*` objects within 0.3 m of the KeySpot | KeySpot (10.23, 1.42, 10.40) | `Kit_KeyRing` + `Kit_Key_Zone_Nickel` + `Kit_KeyTag_Long_White` at **1.5×** (114 × 48 mm). `STAIR A` lettering and the final colour are 平面视觉's | the kits' own | 1,972 + 2,188 + 1,038 = 5,198 | 30L S11, §10.2 S11; 20L §4.0 (`STAIR A` final); T20 V1 |
| P07 | `Kit_KeyCabinet_Putty` | **new** (`key_cabinet_putty.py`, the `Kit_KeyCabinet` box with a **shut door part**, real LOD1/2). **Origin the wall face at floor level, heights baked in** (body y 1.24–1.70) | **11.25 / 9.90 / 0, 180** (the riser's south face, the staff side, off the plate's wall, 1.7 m from the seat) | 0.36 wide × 0.46 tall × 0.084 deep. Door shut, so no key bunches are visible. Door art `KEY CONTROL` / `ALL KEYS MUST BE SIGNED OUT` (平面视觉). **GAP 20** | `Prop_SteelPutty` (#A69E88) | 6,804 / 2,300 / 250 | 30L S14, §10.2 S14; PAL (key cabinet ADJUST: putty or light grey); 01 §6 |
| P08 | `Kit_Clipboard_Wall` (sign-out sheet) | **new** (`clipboard_wall.py`, Wall placement; **origin the wall face at floor level**, board y 1.12–1.44) | **10.72 / 9.90 / 0, 180** (riser south face, on a nail by the cabinet: **1.36 m from the seat**, G9 ≤ 2 m, check C1-10) | 9 × 12.5 in hardboard, steel clip, 3–5 sheets. Header `DATE  KEY NO.  NAME  OUT  IN  INIT.`; the stair key's line is last, its OUT blank (平面视觉's art, 叙事's strings). **EXAMINE-readable** (30L S15) with 01 §6's strings, blocked only on the atlas | `Prop_Hardboard`, `Prop_Chrome`, `Prop_StationPaper` (平面视觉's atlas) | 1,800 / 600 / 60 | 30L S15; 01 §6 |

### 3.2 The counter and the guard (tier G + S)

| # | Kit | Status | x / z / y, yaw | Size, details | Materials | LOD0/1/2 | Source |
|---|---|---|---|---|---|---|---|
| P09 | `Kit_StationCounter_Run120` | **new** (`station_counter.py`) | centre 9.50 / 7.50 / 0, 180 (visitor face south, toward D) | 1.20 m run, **0.60 deep** (z 7.20–7.80). Visitor ledge at **1.07 m**, 0.30 deep (z 7.20–7.50), over the front of the work surface. Staff work surface at **0.74 m running back to the fascia under the ledge, z 7.23–7.80** (30L S05's section inside 61's rect; check C1-14), with knee space under it. Under the ledge: a dead door-release button and an intercom master. Black 0.10 m toe base. Box colliders. **GAP 5, GAP 6** | fascia **`Prop_LaminateGrey`** (new, #8E8E88); ledge top `Prop_WoodOak` + `Prop_PVCEdge`; work surface `Prop_LaminateBeige`; toe `Prop_Vinyl` | 3,500 / 1,200 / 150 | 30L S05, S20, §2.1; 61 §4.4 counter row, §5 (柜台正面 grey laminate); PAL (counter CONFIRMED); PICK (counter front + toe) |
| P10 | `Kit_StationCounter_Run075` | **new** (same module, 0.75 m cut) | centre 10.475 / 7.50 / 0, 180 | 0.75 m run, the same section | as P09 | ~2,500 / 850 / 110 (ESTIMATE) | as P09; GAP 5 |
| P11 | `Kit_StationCounter_End` × 2 | **new** | west end x 8.90, east end x 10.85, z 7.50, 180 | Finished end panels, the staff side **open at both ends** (G7: the strip runs west into the room and east into the 1.07 m aisle) | as P09 | 1,200 / 400 / 60 each | 30L S05; 20L G7; 61 §4.4 |
| P12 | `Kit_SecurityLetters` | **new** (`security_letters.py`; extrude 信息设计's `security_letters.svg` by 6.35 mm, square cut edges) | 9.875 / 7.181 / 0.75 (baseline), 180 | `SECURITY`, TeX Gyre Heros Bold, 4 in caps (ink 758.4 × 104.9 mm), tracking +0.06 em. Pin-mounted 12.7 mm off the fascia, centred on it; cap top ≈ 0.852. Re-extrude only when 信息设计 re-bakes; **never re-letter** | satin brushed aluminium with a horizontal grain (`Prop_Aluminium`; a brushed variant only if the look check asks) | ~1,500 / 500 / 0 (ESTIMATE) | SIGNS §2; DEC 15:3x ("use them as delivered; do not re-letter"); 61 §8 |
| P13 | `Kit_TaskChair_Charcoal` | **variant** of `Kit_TaskChair` (`task_chair.py`, `Prop_FabricChair` → `Prop_FabricCharcoal`), exported by its own module **with the pile block and pile tags stripped** (check C2-4) | 9.90 / 8.80 / 0, **349** (turned to face the TV at (9.35, 11.6), its back to D) | 0.617 × 0.953 × 0.653, black base. Collider | `Prop_PlasticBlack`, `Prop_Chrome`, `Prop_FabricCharcoal` | 4,484 / 1,792 / — | 61 §4.4 chair row ("他看的是电视，不是门"); PAL (task chair ADJUST: grey fabric on a black base); DEC 16:0x |
| P14 | `Kit_JacketDraped` | **new hero** (`jacket_draped.py`, cloth-baked over `Kit_TaskChair`'s back, top 0.953) | 9.90 / 8.80 / 0, 349 (same pivot as P13) | Sloped shoulders, a sleeve over the arm, a blank shoulder patch. No collider | uniform tone #59647A (new `Prop_FabricUniform`) | 8,000 / 2,500 / 300 | 30L S23, §10.3 S23; NN §2 (the jacket) |

### 3.3 The evidence cluster (G9: within 2 m of the guard's seat, lit enough to EXAMINE; small lamps dark)

| # | Kit | Status | x / z / y, yaw | Details | Materials | LOD0/1/2 | Source |
|---|---|---|---|---|---|---|---|
| P15 | `Kit_RegisterBook` | **new** (`register_book.py`) | 9.55 / 7.35 / 1.07 (visitor ledge), 180 (reads from the visitor's side) | Open ledger 0.43 × 0.30, a pen on a bead chain. The open page is the **previous business day's**: headers `DATE  NAME  COMPANY  VISITING  TIME IN  TIME OUT`; daytime visitors all out by 1745; then blank rows; **the last ruled line blank** (that line is you). Initials and generic trades only. EXAMINE only, no text entry | `Prop_Hardboard`/cloth binding, `Prop_StationPaper` (平面视觉) | 2,500 / 800 / 80 | 30L S16; 01 §6; DEC (fiction; 18:0x visitor and live clocks); NN §2 |
| P16 | `Kit_TentCard` | **new** (`tent_card.py`) | 10.20 / 7.35 / 1.07, 180 | `PLEASE SIGN IN` (平面视觉's type) | `Prop_StationPaper` | 300 / 100 / 0 | 30L S22; 01 §6; DEC (`PLEASE SIGN IN`) |
| P17 | `Kit_Clipboard` (the Daily Activity Report) | **new** (`clipboard.py`, DeskTop placement) | 9.25 / 7.55 / 0.74, 0 (read from the staff side) | The log stops at `0200  TOUR 3` with no COMPLETE. The header date is computed from the live clock (the night shift rule). Initials `D.K.`. **EXAMINE-readable** (30L S15; check C1-10), its lines 01 §6's, blocked only on the atlas | as P08 | 1,800 / 600 / 60 | 30L S15; 01 §6 (DAR); NN §2 |
| P18 | `Kit_DeskBlotter` | **new** (`desk_blotter.py`) | 9.80 / 7.52 / 0.74, 0 | A desk blotter, with a typed phone list under the desk glass. **No printed date** | `Prop_Vinyl` (blotter), `Prop_Glass`, `Prop_StationPaper` | 600 / 200 / 0 | 30L §10.3 S39 |
| P19 | `Kit_CoffeeCup` | **new** (`coffee_cup.py`) | 10.00 / 7.68 / 0.745 (on the blotter), 0 | An 8 oz foam cup or a mug, a cold coffee disc and a ring stain | `Prop_Ceramic` | 1,200 / 400 / 0 | 30L S24; NN §2 (the cold cup) |
| P20 | `Kit_Ashtray` | **new** (`ashtray.py`, glass) | 10.22 / 7.66 / 0.74, 0 | Glass desk ashtray (GAP 26) | `Prop_Glass` | 1,500 / 500 / 0 | 30L S25 (A variant); 20L G9 |
| P21 | `Kit_DeskPhone_Corded` | **variant** of `Kit_DeskPhone` + coiled handset and line cords (`desk_phone_corded.py`). A separate name keeps every Office desk phone unchanged; **no pile block, no `pile_piece` tag** (check C2-4) | 10.48 / 7.60 / 0.74, 20 | Putty key set. **Line lamps dark** (no hold lamp) | the kit's own (`Prop_PlasticBeige`, …) | 966 + ~300 / 390 / — | 30L S18, §10.3 S38; 20L G9; KP addendum 12:5x item 5 |
| P22 | `Kit_RadioCharger` | **new** (`radio_charger.py`) | 10.74 / 7.62 / 0.74, 0 | Black drop-in charger, one **empty** well, a **dark** charge lamp, a spare battery beside it | `Prop_PlasticBlack` | 2,000 / 700 / 80 | 30L S19; NN §2 (the radio charger empty) |

### 3.4 Walls (tier S)

| # | Kit | Status | x / z / y, yaw | Details | Materials | LOD0/1/2 | Source |
|---|---|---|---|---|---|---|---|
| P23 | `Kit_NoticeBoard` | **new** (`notice_board.py`; **origin the wall face at floor level**, board y 1.20–1.65) | **11.92 / 8.40 / 0, 270** (the east wall, staff side; the riser's south face now carries the cabinet and the sign-out sheet) | Small cork board, about 0.60 × 0.45 (ESTIMATE), blank-text notices from the atlas. **GAP 22** | **`Prop_Cork` (new)** + `Prop_StationPaper` | 1,500 / 500 / 60 | 30L §10.3 S40; 61 §16 (four small items) |
| P24 | `Kit_WallClock` | **reuse** (`Assets/Resources/Props/Models/Kit_WallClock.*`; live hands per the live-clocks line) | 3.00 / 11.92 / 2.20, 180 | Ø 0.32. **GAP 23**: not over E1 | the kit's own | 538 | 30L S30; MD B8 rule; MainScenePlaytest stand-in (x 3, y 2.2) |
| P25 | `Kit_TVCords` | **new** (`tv_cords.py`; origin the wall face at floor level) | 9.55 / 11.92 / 0, 180 | The set's power cord and RG-59 coax, run down the wall from P01 to P26 / P27, draped over the crash rail | `Prop_PlasticBlack` | ~400 / 150 / 0 | 30L §10.3 S37 |
| P26 | `Kit_OutletDuplex` | **reuse** (`Assets/Resources/Props/Models/Kit_OutletDuplex.*`; **centre-origin**, bounds ±0.057) | 9.60 / 11.92 / **0.30** (plate centre 0.30, check C2-1), 180 | Below the rail (0.78) and above the base (0.10) | the kit's own (ivory) | 2,880 / 954 / 42 | 30L S37; T20 / Red 10-21 "outlets ivory D" |
| P27 | `Kit_JackF` | **variant** of `Kit_JackBNC` (`outlet_jack_f.py`): an F connector for cable TV; centre-origin | 9.75 / 11.92 / **0.30**, 180 | **GAP 34** | as `Kit_JackBNC` | ~2,700 / 1,000 / 56 | 30L S37 ("an F-jack plate") |

### 3.5 Decided story props added by the plan check (check C1-11)

| # | Kit | Status | x / z / y, yaw | Details | Materials | LOD0/1/2 | Source |
|---|---|---|---|---|---|---|---|
| P28 | `Kit_HookHeavy` | **new** (`hook_heavy.py`; origin the wall face at floor level, hook at 1.60) | 10.42 / 9.90 / 0, 180 (riser south face, west end, beside the sign-out sheet: 1.13 m from the seat) | 30L §10.3 S26 as reworded: an **empty heavy cast hook** with the watchclock strap's **buckle loop** left on it (no cut strap). The clock is gone with him | `Prop_SteelBlack` (cast hook), `Prop_Vinyl` (leather loop), `Prop_Chrome` (buckle) | 1,200 / 400 / 0 | 30L S26, §10.3 S26; 01 §2 ("the strongest single prop") |
| P29 | `Kit_BinderPostOrders` | **variant** of `Kit_Binders`' binder (`binder_post_orders.py`): one three-ring binder lying on the work surface, spine `POST ORDERS` from the atlas (no company name) | 9.02 / 7.62 / 0.74, 90 | **No pile block** | `Prop_Vinyl`, `Prop_StationPaper` (spine), `Prop_Paper` | 360 | 30L S17, §7 (atlas list); 01 §2 paperwork, §6 |

**Placement checks (ValidateStation + `Validate`):**
- Nothing stands in x 6.9–8.1.
- The TV is the only CRTTV, at x 9.04–9.66.
- The floor props with colliders are P09–P11 and P13. Every wall kit stands at y 0 (origin on the floor) or, for the centre-origin outlets, at its centre height; `p.y + boundsMax.y ≤ 2.85` holds for every row (highest: P02 2.83, P01 2.40). The walk leaves:
  - a 1.07 m aisle east of the counter to the wall face (1.04 m to the rail face);
  - the strip behind the counter (z 7.80–9.90) open at both ends;
  - the take point → E1 line clear;
  - the room's west half empty for the E2 escape line.
- D's sweep, E1 and E2 keep-clears are empty.
- **Not placed (decided out, or a later phase):**
  - tandem seating, the smoking stand and the magazine rack (Floor 2+ B only; 61 §4.4 lists none for FD-b);
  - the desk lamp (30L S28, tier P2; GAP 24);
  - the trash bin and the paper stack (30L S31). These are not in FD-b's decided lists (GAP 25). The post-orders binder (S17) and the empty watchclock hook (S26) **are placed** (§3.5, check C1-11).

---

## 4. Surfaces and materials (`Assets/Resources/Surfaces`, shader `FrontRooms/Surface` unless stated)

| Asset | New? | Values | Read by | Source |
|---|---|---|---|---|
| **`Station_Wall`** | new | Eggshell latex. **#9AA3A6** (sRGB) as `_BaseColor` on a white base map. `_BumpMap` = `Office_Drywall_N`, strength 0.5. `_TileSize` 1.2 × 1.2. Smoothness ≈ **0.38** (PICK roughness 0.62). No emission (walls never glow). **Clean, as PICK: macro tone, dirt, floor and ceiling grime 0** (check C1-13; GAP 32) | the map's station skin (`MW.StationWallSurface`): every station wall face, the liner, the riser block, the face of D's data wall after the clear | DEC 15:4x; 61 §5 墙面; PICK `mat_paint('#9AA3A6', 0.62)` + `Office_Drywall_N` 1.2 m; KIT 游戏视觉 4 |
| **`Station_Floor`** | new | VCT: `Run_VCT_A/N/S` re-tinted so the field's mean is **#B2AEA3**. `_TileSize` 1.2 (as `Run_Floor`). Smoothness ≈ 0.62 (PICK roughness 0.38) | the map's station skin (`MW.StationFloorSurface`) | DEC 15:4x; 61 §5 地面; PICK `mat_floor_banded(tex='Run_VCT_A.png', tint=(0.66, 0.65, 0.63), rough 0.38)` |
| **`Station_FloorBorder`** | new | The same VCT maps tinted to mean **#3A3A38**, `_FR_MESH_UV` on (its own 0.305 m course) | our trim (§5): **one tile course, 0.305 m from the wall face**, round every wall and the riser | 61 §5 地面边带; PICK `band_in 0.0, band_w 0.305, '#3A3A38'` |
| **`Station_Ceiling`** | **new: an identical copy of `L0_Ceiling`** (same shader, maps and values; GAP 13 revised, check C2-3c) | so the map splits the station's ceiling into its own renderer and `StationShellBuilt` passes it; it then takes the Station mask and the station lamps light it. It looks exactly like Level 0's ("同 Level 0") | the map's station skin (`MW.StationCeilingSurface`) | 61 §5 天花板; KIT 游戏视觉 4 (optional); MW l. 1357–1373 |
| **`Station_CeilingBand`** | new | Gypsum **#D9D6CC**, smoothness 0.15 (PICK roughness 0.85), matte | our trim: the 0.30 m drywall band over E1 and E2 (§5) | 61 §5 天花板 ("门洞上方加一条 0.30 米的石膏板"); PICK `header_band_E1` |
| **`Station_CrashRail`** | new | Vinyl **#3E4650**, smoothness 0.5 (PICK), `Prop_Vinyl` normal | our trim: rail 0.78–0.93 m, 28 mm proud, 10 mm bevel | DEC 15:4x; 61 §5 防撞护墙条; PICK `band_run('s_rail', 0.775, 0.925, 0.028, bevel 0.010)` |
| **`Station_Base`** | new | Black rubber **#1A1918**, smoothness 0.5 (PICK) | our trim: 4 in base (0.10 m, 6 mm proud) and the rubber reducers in E1 and E2 | 61 §5 踢脚; PICK `base_run('rubber')` |
| `Prop_SteelBrown` | **reuse** | #3B2E25 dark bronze (PICK metallic 0.5, roughness 0.45; check the asset's mean at build) | our trim: the pressed-steel cased frames at E1, E2 and the static entrance | 61 §5 门洞; DEC 15:4x |
| **`Station_StainlessGuard`** | new, **a station-only name** (check C2-4): `Prop_StainlessBrushed` is read by `FrontRoomsWallFixtureRenderer.Stainless()` and would re-skin every wall plate that falls back to `Prop_AluminiumAnodised` today | **#A8A9A4**, metallic 1, smoothness 0.68 (PICK roughness 0.32), a brushed normal | our trim: corner guards | 61 §5 门洞护角 (the material named there is not created under that name; recorded as GAP 41); PICK `stainless` |
| **`Prop_LaminateGrey`** | new | #8E8E88 (SP), smoothness 0.5 | P09–P11 fascia | 61 §5 柜台正面; 30L §2.1 |
| **`Prop_SteelBeige`** | new (GAP 8) | Beige enamel: `Prop_SteelPutty`'s maps with `Prop_PlasticBeige`'s mean colour | P04 cabinet and door | PAL (annunciator: beige cabinet, beige door) |
| **`Prop_JewelGreen`** | new | Green glass, emissive (steady), a faint warm filament core; the `Prop_LEDGreen` hue (~125°) but incandescent | P04 POWER jewel | 30L §4.3, §10.2 S12 |
| **`Prop_FabricUniform`** | new | #59647A, uniform serge | P14 | 30L §10.3 S23 |
| **`Prop_PlateBusinessHours`** | new | From SIGNS `business_hours_albedo.png` / `_height.png` (converted to a normal) / `_mask.png` (smoothness ≈ 0.25 on the letters), 4 px/mm, 1832 × 612. Copied into `Surfaces/Textures/` byte-identical; the art itself is never edited | the plate (§6) | SIGNS §1; DEC 15:3x, 18:1x |
| **`Prop_Cork`** | new (check C2-10) | cork #9C7A55, matte (smoothness 0.1), `Prop_FabricCharcoal`'s normal at 0.4 | P23 | 30L S40 |
| **`Prop_StationKeyPlate`** | new slot (check C2-4) | engraved two-ply plastic, blank until 平面视觉's `STAIR KEY` art; #2B2925 cap | P03a | 30L S21; 01 §6 |
| `Prop_StationPaper` | **平面视觉's atlas** (missing today, GAP 27); shipped as a **blank** paper placeholder (no invented type) | Register page, tent card, DAR, sign-out sheet, KEY CONTROL door, phone list, notices | P07, P08, P15–P18, P23 | 30L §7 (平面视觉: one atlas); 01 §6 |
| `Prop_EvacPlan_A_Room` / `_E_Room` and `_A_RoomPhase0` / `_E_RoomPhase0` (`Surfaces/Textures/`) | new textures, packed with `Tools/lookdev/pack_evac_plan.py` from `Tools/print/ink/art_from_graphic/placard/sheetA_room/` and `sheetA_room_phase0/` | `placard_print_lit.png` → A, `placard_glow_mask.png` → E | the station placard instance (§6.1) | 61 §12 (Phase 0 retires on Floor 1; `sheetA_room` replaces it); PLL; 平面视觉 README naming |

---

## 5. Trim set (`FrontRoomsStationTrim`, procedural, render-only, built from `map.StationLayout`)

**What the trim is.** It is built in our Rendering code, not as module props, for three reasons:
- it wraps wall reveals, which a module prop may not enter ("stands in or beyond a wall", MD l. 380);
- its runs follow the map's exact E1/E2 spans (`map.Cache.ArchOverride`), the riser block and the two start-door phases;
- the map does not build any of it (an Arch has no frame, MW `BuildEdge` returns before the frame for `EdgeKind.Arch`).

**How it lives.** It is parented under `map.Station`, so it dies with the set. Its renderers follow `map.Station.Visible`. It has no colliders, so the map's colliders keep every clear width (E1 1.70, E2 1.20). **There is no header panel over D** (check C1-5 / C2-6): the stream door's header (2.66–2.90) stays bare until `ClearStartArea` (KIT 游戏视觉 5; 61 §6). **One renderer per material and per mask**: the room-side runs (Station mask) and the opening parts (both masks) are separate meshes (check C2-10).

| Element | Geometry | Material | Source |
|---|---|---|---|
| Crash rail | Bottom 0.78, top 0.93, 28 mm proud, 10 mm bevel. On every inner wall face, both liner panels (or D's wall after the clear), and the riser's west and south faces, with returns round the riser's outside corner. It ends square at each opening's casing outer edge, and 0.05 m short of D's opening edges | `Station_CrashRail` | 61 §5 ("屋里每面墙和墙垛都有，门洞处收头"; 墙垛 row "护墙条绕过去"); PICK `band_run` |
| Base | 0.10 high, 6 mm proud, on the same runs as the rail | `Station_Base` | 61 §5 踢脚; PICK `base_run` |
| Floor border | One 0.305 m VCT course from each wall face, round the riser too. Field tiles are cut at its inner edge. It stops at D's jambs and runs into the E1/E2 throats as the field (next row) | `Station_FloorBorder` | 61 §5; PICK; GAP 19 |
| Cased frames, E1 and E2 | Pressed steel on **both faces**: face 0.05, proud 0.012; jambs to the arch top 2.20 + face; head over it. Throat liners 0.012 on both jambs and the head soffit (they cover the map's half-and-half skin split in the reveal) | `Prop_SteelBrown` | 61 §5 门洞 ("两面都有"); DEC 15:4x (E1 frame); PICK `casing_E1(face 0.05, proud 0.012)`; GAP 15 |
| Corner guards, E1 and E2 | A 50 × 50 mm stainless angle wrapping each jamb arris **on the station face** (one wing on the face, one in the throat), 3 mm thick, y **0.10–1.32** | `Prop_StainlessBrushed` | 61 §5 门洞护角; PICK `corner_guards_E1` (station face); GAP 14 |
| Throat floor, E1 and E2 | VCT through the 0.16 m wall thickness of the opening | `Station_Floor` | 61 §6 ("地砖铺进门洞"); PICK `throat_floor('s_floor')` |
| Reducer, E1 and E2 | A rubber reducer strip 0.05 × 0.006 at the opening's **outer** (Level 0) face, meeting the carpet | `Station_Base` | 61 §6 ("在门洞外沿用一条橡胶收边条接 Level 0 的地毯"); GAP 16 |
| Ceiling band, E1 and E2 | 0.30 m wide, centred on the wall line, the length of the opening's cell edge (3 m), 0.06 m deep below the ceiling plane | `Station_CeilingBand` | 61 §5 天花板; PICK `header_band_E1`; GAP 13 |

**Seams (61 §6).**
- They occur only at E1, E2 and D. The yellow paper stops at the steel casing's outer edge.
- The station skin covers only the station side of the perimeter walls (the map's per-cell skin override, W1), so no wall shows a seam mid-run.
- The corner posts of the "third skin" are handled by `FrontRoomsTransitionKit` B0: a finish = material + ceiling class, so `Station_Wall` is its own finish automatically.
- Proof (§15 c): no z-fight and no paper end cap inside the station, at the four station corners and at every outside T-junction. Change `FrontRoomsTransitionKit.cs` only if a defect shows (PLAN §7.3; GAP 39).

---

## 6. The riser face, the entrance, the plate and the liner

### 6.1 The placard switch: `FrontRoomsPlacardMount.cs` (KIT 游戏视觉 2; PLAN §4.8)

`Place(map, relay)` gains one branch at its top: `if (map.FloorHasStation && map.Station != null && map.StationAnchor(ModuleMarkerKind.Placard, out pos, out rot, out cell))`. Its spot:
- position = `pos + rot * (0, 0, FrontRoomsPlacard.Proud 0.0005)`;
- rotation = `rot`;
- glow cell = `cell` (the Steady placard cell (1, 3));
- mount = a new `FrontRoomsPlacard.Mount.Station`, appended last with label `ST`, so P1/P2/P3 keep their values.

In that branch:
- **Skip** `map.KeepClearAtStart`: the station's props are authored, and the map does not dress station cells.
- Spawn the frame and the lens (the lens not on WebGL, as today) and **re-parent them under `map.Station.transform`**. They then outlive the stream rooms, which are disposed after the shut ("parented to the station set", PLAN §4.8).
- After `Glow.Bind`, set `Glow.Instance`'s `_BaseMap` / `_EmissionMap` to the station sheet:
  - `sheetA_room` when `map.ExitLocked` (M3's lock is live);
  - otherwise `sheetA_room_phase0`.
  - 平面视觉 README: `sheetA_room_phase0` = first room live before the stair lock; `sheetA_room` = stair lock + station live. **GAP 29.**
- Point the EXAMINE transcript at the same sheet's `examine.json`, regenerated with `Tools/lookdev/inspect_transcript.py`.

**Else: today's path, unchanged**: `PL.Resolve`, P1/P2/P3, `KeepClearAtStart`, the parent, `sheetA_phase0`. With `stationModule` empty, `FloorHasStation` is false and the game is byte-identical.

**It lands in the same landing as the switch-on**, because the liner covers P1 west (KIT 游戏视觉 2; PLAN §4.8 landing rule).

### 6.2 The riser chase (KIT 游戏视觉 3; DEC 18:3x)

- **Face material:** `Station_Wall`. The map draws the block in `stationSkin.wall`, so creating the Surface is enough. The world-projected shader keeps the scaled cube's paint undistorted. The block casts shadows and stays solid to the ceiling: no nook (叙事's condition 1).
- **Dressing:**
  - west face: P03 / P03a key plate, P04 annunciator, P05 conduit;
  - south face: P23 notice board;
  - rail + base + floor border round both exposed faces (§5).
- No corner guard on its outside corner (GAP 14).
- The KeySpot stays outside it (§1).
- Its cell's lamp is Off (§1).

### 6.3 The entrance, the plate and the liner (KIT 游戏视觉 5; DEC (fiction, R2, 18:0x, 18:1x); 61 §4.4, §8)

**Start-area phase** (the stream door is the title's live door):
- the liner is `Station_Wall` (the map passes the skin);
- we add the rail, base and floor border on both liner panels; **no header panel** (the header stays bare, check C1-5);
- the plate at `map.Station.PlateAnchor()`.

**`ClearStartArea` phase.** It is never seen, because it happens only when no chunk round the strip is built (PLAN §4.7). Detected by `map.Station.Entrance.activeSelf` turning true:
- the liner goes;
- the south trim runs move to the data wall's face at z 0.08;
- **the static entrance is dressed as the same door seen from inside**:
  - keep the stand-in leaves (already `Door_Veneer`, the stream door's `darkMat`);
  - add the stream door's pull handles (0.055 × 0.18 at 1.22 m) and its 0.25 m kick plates on the room face, both in the stream's brushed-steel hardware look (`FrontRoomsRoomStream.cs` l. 1245–1305, `doorHardwareMaterial` 0.64 / 0.63 / 0.60, metallic 0.9, smoothness 0.62);
  - **no cased frame** (check C1-6 / C2-7): the live stream door is a flush double door with no jamb casing (RoomStream l. 1276–1280), 61 §5 frames only E1 and E2 and leaves D 不改. The map's 0.07 m stand-in jambs and head are **hidden by deactivating their GameObjects** (`SetVisible` sets `r.enabled` on every renderer in its list, so `r.enabled = false` would not stick);
  - **solid leaves, never glass**;
  - **no sign on the door.** `NO REENTRY` is on the placard plan only. Never LOBBY or STATION.
- GAP 15.

**The plate** `THIS DOOR TO REMAIN UNLOCKED DURING BUSINESS HOURS` (`Prop_PlateBusinessHours` on a 457.2 × 152.4 × 1.6 mm two-ply plate mesh, 3.2 mm corner radius, adhesive, no screws):
- **Start-area phase:** at `PlateAnchor`. Centre x ≈ 5.996, y 1.525, face z 0.024; door-side edge 75 mm west of the opening (x 6.225); facing +Z.
- **After the clear:** at `PlateAnchor` (face z 0.08), **unshifted** (check C1-6): with no frame at D the door-side edge stays 75 mm from the opening, inside SIGNS' 50–100 mm. GAP 18 closed.
- Clear flat wall west of it: 5.7 m (≥ 560 mm needed). The rail (0.78–0.93) and the base are below it; nothing tall is within 50 mm.
- 信息设计 checks lit frames at 1 m and 3 m (SIGNS "Check").
- Sources: SS `PlateAnchor` (`PlateDoorGap` 0.075, `PlateCentreHeight` 1.525); DEC 18:1x; 61 §4.4 门边的牌子, §8; SIGNS §1.

**Liner header** (2.66–2.90 over D): bare until `ClearStartArea` (KIT 游戏视觉 5). GAP 17 closed: no panel.

---

## 7. What the player sees, re-checked against the decided reveal

| Check | Numbers with this plan | Bar and source |
|---|---|---|
| G1 / B8 at the reveal (from the stream room at Space) | The EXIT's flat sign straight ahead through E1. The ray clears E1's 2.20 arch top at fraction 0.70 (PLAN §4.1). Our casing head sits on the wall face above 2.20; nothing within x 6.9–8.1 is over 1.5 m | PLAN §4.1; MD B8 |
| The set from D | Centre x 9.35: **≈ 9°** right of the axis at 11.5 m (≤ 12°) | 61 §4.4; 20L G1 |
| The placard from D | x 5.5: **9.5°** left, face-on | 61 §4.4; DEC 20:28 |
| G4: the key lit by the set | Screen centre (9.35, 2.46, 11.32), key (10.23, 1.42, 10.40): **43.9°** off the screen axis in plan (30–45°); 1.27 m plan / **1.64 m** 3-D (≤ 2.0); **42°** off the tilted normal in 3-D (inside the 100° inner cone). Check 2 (> 50 % on the darkest card, E ≥ 0.3) is proved there in the build (check C1-7) | 20L G4; 30L §4.2 (30–45°, ≤ 2.0 m); 61 §4.4 (~38°, ~1.5 m) |
| G2: the two escape directions at the take | Take point (9.73, 10.40), 61's (9.7, 10.4): E1 2.70 m, E2 11.31 m, directions **66°** apart (≥ 60°) | 20L G2; 61 §4.4 (67°) |
| G6: aisles | Axis strip empty; counter east aisle 1.07 m; staff strip 2.1 m deep, open both ends; D's sweep clear | 20L G6, G7 |
| The door shuts before the counter | Shut at ~4 m; counter front at z 7.20 | 61 §2; 20L FD-b condition 2 |
| The turn to the plate | Facing south from ~4 m in, the plate is ~23° to the right | 20L FD-b condition 2 |
| The take frame | The set is out of view (~100° to the side). The light on the plate and key drops ≥ 70 % on the worst card in one change. The green POWER jewel stays lit 1.0 m to the north | 20L G5; 30L §10.1, §10.6; 50 §6 |

---

## 8. The TV and its light (KIT 游戏视觉 6; T20 V1 / V3; 30L §3.1 + §10.1 r7; 61 §7)

**The set.** P02, placed by the map. The dresser finds it among `map.Station`'s spawned props by kit name. Once spawned:
1. It re-seats the set on P01's `set_mount`: a 20° front-down pitch about the set's back-bottom edge (hinge at **y 2.36**, 0.05 m off the wall face). Proof: rendered bounds of **the set and the bracket together**: min y ≥ 2.15 (2.19 by construction), max y ≤ 2.85 (2.83) (check C1-8 / C2-9).
2. It attaches the picture with **`FrontRoomsScreenVideo.Attach(screenMount, null, FrontRoomsScreenSoundRole.Beacon)`**, the ad path, never `AttachDesk` (V4R §"For V1"; R3C §1). It is the one plain-`Attach` screen under the map: the only moving screen on the floor (T10 §4.4; R3C §2.1). **Beacon is a rule** (NOTE_1029_station_tv_soundrole; check C1-2 / C2-5a): this package **stacks on R3**, which brings the enum and the argument; every other screen stays None.
   - `Attach`'s default `screenAnchor` / `screenSize` (0, 0.248, 0.194) / 0.28 × 0.19 fit `Kit_CRTMonitor`, not `Kit_CRTTV` (screen anchor (0, 0.284, 0.245)).
   - So we attach to a child **"screen mount"** of the set, offset and scaled so the default quad lands on the TV's real screen face (size measured from the hero kit's `Prop_ScreenCRT` faces).
   - **No edit to `FrontRoomsScreenVideo.cs`**, which R3 is changing. GAP 4.
   - R3's READ target is a child of the surface, so it follows (R3C §3).
   - **LODs** (check C2-5b): `AddToMonitorLods` looks for a `LODGroup` on the Attach target itself, which the screen mount has not, so the dresser adds the surface renderer to the set's `LODGroup` itself (every level), still without a ScreenVideo edit for that.
3. **Desk stills:** nothing to build. V4 is in main (`FrontRoomsStationPresence` reads `FloorHasStation`, set before Begin: Game l. 912 < Begin < `MapRunStarted`; KIT 游戏视觉 6). Proof only: every Office desk CRT on floor 1 is still; the station TV plays; the title corridor TV plays.

**The light: `FrontRoomsStationTvLight`.** A sibling component in Rendering, so `ScreenVideo` is not touched. 30L §3.1 allows "owned by `FrontRoomsScreenVideo` (or a sibling)".

| Property | Value | Source |
|---|---|---|
| Type, place | Spot at the screen centre, 0.02 m out along the (tilted) screen normal; it faces south **along D's axis** (≈ 9° off) | 30L §3.1; T20 V1 ("The TV light along the doorway axis"); T10 §4.6; KIT 游戏视觉 6 |
| Cone, range | inner 100°, outer 160°; URP range 9 m | 30L §3.1 |
| Cookie | Cosine falloff with an up-clip: fades from 10° to 30° above the horizontal (no ceiling wash) | 30L §10.1 |
| Intensity | I = 6 × √(Y / 0.140), clamped 4.86–8.4, × **`map.StartLampsNorth`** (0 → 1 over 0.6 s with the swing, so nothing leaks before D opens) | 30L §10.1; 61 §7 (B10); KIT 游戏视觉 6 |
| Colour | The frame's mean linear colour, luminance kept (I_eff = I × 0.71 / Y(colour)), blended **60 %** toward the set's white (0.86, 0.91, 1.00) | 30L §10.1 |
| Driver | A **track baked offline** from `StreamingAssets/FrontRooms_Ad_01.mp4` (sha1 8bde2101): 10 Hz, 160 samples of colour and Y over the 16 s loop, in `Resources/Rendering/StationTvLightTrack.json` (baker in `Tools/lookdev/`). Indexed by the shared bank's `VideoPlayer.time` (`FrontRoomsScreenAdBank.Acquire(DefaultStreamFile, null).GetComponent<VideoPlayer>()`); τ 0.12 s; with `FrontRoomsSettings.ReduceFlashing`, τ 0.5 s. No GPU read-back. Re-bake when the ad changes | 30L §3.1 |
| Photosafety | ≤ 3 swings ≥ 10 % a second; measured ≤ 1 change a second, largest step +77 % at the loop wrap | 30L §3.1; T20 V3 |
| Shadows | Desktop Soft; none on WebGL (V3 row). Do not rely on them to stop leaks: §9 does that | T20 V3; 30L §10.1; GAP 12 |
| The take | On `map.StationKeyTaken` (raised at `KeyTaken`, 0.25 s after E), the light falls (τ 0.12) to 0 by 0.60 s and **stays off** for the floor. The board's displayed drop is ≥ 70 % on the worst card, while the green jewel stays lit. **The set goes off for good in the same handler** (check C1-1 / C2-5c): the dresser calls `FrontRoomsScreenVideo.PowerOffForGood()`, a public call no run has yet (crt-tube's `SetPower` is private; R3C §2.3), so this package adds it on R3's ScreenVideo (stacked on R3, 8 lines: the private `SetPower(false)` and a latch that keeps E from turning it back on). When crt-tube lands, its collapse hooks `SetPower` and plays. **Precondition, like R3: R3 lands before or with this group** | 50 §6; 30L §10.1, §10.4; T10 §4.4, §4.7 step 3 ("the subject is the light on the key"); 20L G5; V4R ("After the key is taken the station TV goes off for good (V1's job)"); 61 §2 (电视当场死掉); R3C §2.3 |
| Gating | On while the beacon plays, within one chunk (24 m) of the player; never part of W1 (the lamps dip, the TV doesn't) | 30L §3.1 |
| Rendering layers | Light mask = **Station only (2)**, when §9 turns layers on | 61 §15; 30L §7, §10.1 |

---

## 9. Light layers (61 §15, decided: test first, then A on every platform if it leaks; 61 §7)

**Step L0: measure (always).** In the clone with the module switched on, in **Play mode and in a Mac build** (Red's words, 61 top box), with masks off:
- **(a) leak out:** frame pairs with the TV light on and off of the Level 0 surfaces behind the station's north, east and west walls and the riser;
- **(b) leak in:** frame pairs with each neighbouring Level 0 lamp on and off, of the station's inner walls.
- The bar (SP, GAP 9) is any card-synced or lamp-synced change over 1 % of the receiving surface's luminance.
- 30L §10.1 found the map's additional lights cast no shadows once the map is built, so a leak is expected.

**Step L1: A, only if L0 leaks** (rewritten by the plan check, C1-4 and C2-3; 61 §7 is the rule).

| Item | Setting | Source |
|---|---|---|
| URP | `Assets/Settings/FrontRooms_URP.asset` `m_SupportsLightLayers: 1`. **Not in this package** (check C2-8: the URP asset is not on the task's list of 游戏视觉 paths). It ships as a separate one-line hunk, `W/apply/station_look_v1_urp.sh`, that runs only with the parent's explicit approval; without it every mask below is inert and the game renders as today | 61 §15 option A; GAP 11 |
| Bit | Rendering layer **bit 1 = "Station"** (a code constant; ProjectSettings is not touched, GAP 10). G14 owns bits 29/30, so there is no clash. Masks are **only changed by bit operations** (`|= 2`, `&= ~1u`), never assigned, so G14's bits survive (check C2-3e) | 30L §7, §10.1; 61 §15; `FrontRoomsGlassRTSystem.cs` l. 31–33, 1395–1408 |
| Station renderers | **Station only (bit 0 cleared, bit 1 set)**: the shell from `map.StationShellBuilt` (wall, floor **and ceiling**: `Station_Ceiling`, §4, makes the map pass it, check C2-3c), the set (`map.Station.Renderers`: liner, riser, static entrance, every module prop as it spawns), the trim's room-side runs, the placard, the plate, the TV and its surface, **and the floor key + tag** (M3's spawn, found at the KeySpot until M3 parents it under `map.Station`; check C1-3 / C2-3b) | 61 §7 ("值班台的墙、家具、电视的光放在 Station 光照层 … Level 0 的灯只照 Level 0"); 20L G4, G5; T20 V1 |
| Opening trims | **both bits** for the frames, guards, throats, reducers and ceiling bands at E1 and E2 | 61 §7 ("门洞 … 两层都照") |
| TV light | **Station only** (light mask 2) | 61 §15; 30L §7 |
| Station-cell lamps | **Only the two entrance lamps light both layers**: the row-0 Steady pair beside D, cells (1, 0) and (3, 0), get `renderingLayerMask |= 2`. **Every other station lamp is Station only** (`|= 2`, `&= ~1u`) (check C1-4a / C2-3d). Found under the chunk root of a `StationShellBuilt` renderer by the map's own names `Fixture (x, y)/Light` | 61 §7 ("门洞和入口两盏灯两层都照") |
| Level 0 lamps | untouched (bit 0), so they no longer reach Station-only surfaces | 61 §7 |
| **Default-layer lights that reach the station today** (check C1-4b / C2-3a) | (1) **the scene's directional fill "Soft ambient direction"** (`FrontRooms3D.unity`, `m_RenderingLayerMask 1`, soft shadows; `FrontRooms3DGame.ApplySceneLighting` l. 496–510): with layers on it would stop lighting the station. Handled by a **contract note to its owner (关卡设计: scene + Game)**: OR bit 1 into its `renderingLayerMask` (harmless elsewhere: only station renderers carry bit 1). Applied in the clone only for the proofs. (2) **Level 0 map lamps** next to the station: kept off the station by design (61 §7). (3) **The title stream rooms' lights** through the open D: they light the leaves (bit 0) but not the station floor near D; measured as an open item with L0's numbers. (4) **The Relay and other dynamic renderers** (default mask 1): inside the station they would be lit only by Level 0 lamps through the walls and not by the station lamps. Contract note to the Relay's owner: OR bit 1 into the creature's renderers; open item with L0's numbers until then | 61 §7; FrontRooms3DGame.cs l. 496–510; FrontRooms3D.unity |
| Cost | Per-pixel layer test; measured as autopilot fps / p99 with layers on and off (seeds 2554, 20388, LMS §8), desktop, the handheld tier and the WebGL preview define | 61 §15 ("每个像素多一点点开销 … 量") |

**The flag lands in the same group as the switch-on.** Proof (a) still checks that with the station off and default masks, frames are pixel-identical (layers on vs off).

---

## 10. The station reflection

The station gets its own cube. That is decided: DEC 15:4x lists "the station's own reflection" for 61 to specify.
- `Refl_Station.exr` in `Resources/Rendering/Reflections/`, captured with main's `Assets/Editor/Rendering/FrontRoomsReflectionCapture.cs` in the clone:
  - at the room's centre, 1.62 m, TV light off (no frozen card);
  - under the room's own lamps, as the other cubes were.
- `FrontRoomsLook.ReflectionZone.Station` is appended last.
- `FrontRoomsZoneReflection.CubeNames` and `ZoneLinear` get a 5th entry, 0.5 (as Level 0's, GAP 31).
- **Inert** until 关卡设计 routes `InFrontRooms` to it at Game l. 2101 (today `Level0`; KIT 游戏视觉 4 "the reflection hook gives Level 0's cube until a station cube exists"). That is a contract note (§14.1 item 5).

---

## 11. Budget (station: ≤ 100k LOD0 triangles, ≤ 120 renderers; 61 §15; PLAN §4.9; 30L §1.5)

| Group | LOD0 tris |
|---|---|
| TV + bracket (P01, P02) | 12,000 |
| Key plate + sign, annunciator, conduit (P03, P03a, P04, P05) | 12,164 |
| Key (P06, M3's spawn) | 5,198 |
| Key cabinet + sign-out clipboard (P07, P08) | 8,604 |
| Counter runs + ends + letters (P09–P12) | 9,900 |
| Chair + jacket (P13, P14) | 12,484 |
| Evidence cluster (P15–P22) | 11,166 |
| Walls: notice board, clock, cords, outlet, F-jack (P23–P27) | 8,018 |
| Placard frame + lens | 1,764 |
| Plate, entrance hardware (no header panel) | ~790 |
| Trim set (rail, base, border, frames, guards, throats, reducers, bands) | ~3,000 (61 §5: ~1,500 + the riser) |
| **Total** | **≈ 85,100 (85 %)** |

| Measure | Estimate | Cap |
|---|---|---|
| Renderers | ≈ 108 (recount, check C2-10): ~76 kit LOD renderers (P28, P29 added) + the floor key's 3 kits × LODs ≈ 6 + placard 2 + plate 1 + TV surface 1 + trim ≈ 11 (one per material **and mask**: room-side Station-only runs vs both-mask opening parts) + entrance 7 + liner/riser 3 + station shell 3–5 (wall, floor, ceiling per chunk). The proof counts everything under `map.Station` plus the key | ≤ 120 |
| Colliders | ≈ 6: counter pieces 4, chair 1, riser 1 (map) | ≤ 40 |
| Realtime lights | 14 station lamps (3 Steady + 11 Dim; the 2 Off cells have no fixture) + 1 TV spot = 15, against 16 for an Auto 4 × 4 room | 30L §3.6 |
| Dress time | Props placed by the map in `dressBudgetMs` slices; the door's hold waits for `StationReady` (≤ 3 s) | PLAN §4.4 |

Measured on the built set in proof (b): `ValidateStation` does not check the budget (MD l. 440–442; KIT 游戏视觉 1).

---

## 12. Reused kits, variants and new hero models

**Reused as they are** (`Assets/Resources/Props/Models/<name>.json` + model; sources in `Tools/Blender/frontrooms_kit/assets/`):
- `Kit_WallClock` (`wall_clock.py`);
- `Kit_OutletDuplex` (`outlet_duplex.py`);
- `Kit_KeyRing`, `Kit_Key_Zone_Nickel`, `Kit_KeyTag_Long_White` (`interact_key_ring.py`, `interact_key.py`, `interact_key_tag_long.py`; the key, spawned by M3);
- `Kit_EvacPlacard` + `Kit_EvacPlacardLens` (`evac_placard.py`, `evac_placard_lens.py`);
- materials `Prop_SteelBrown`, `Prop_SteelPutty`, `Prop_SteelBlack`, `Prop_Aluminium`, `Prop_Chrome`, `Prop_WoodOak`, `Prop_PVCEdge`, `Prop_LaminateBeige`, `Prop_Vinyl`, `Prop_FabricCharcoal`, `Prop_PlasticBlack`, `Prop_PlasticBeige`, `Prop_Glass`, `Prop_Ceramic`, `Prop_Hardboard`, `Door_Veneer` (`Prop_SignEngraved` does **not** exist in main and is `Kit_DoorSign`'s P-4 slot: not used, check C2-4);
- textures `Office_Drywall_N`, `Run_VCT_A/N/S`.

**Hero pass under a station-only name:** `Kit_CRTTV_Station` (`crt_tv_station.py`; exactly `Kit_CRTTV`'s bounds and anchors, ~9,000 LOD0, beige). `Kit_CRTTV` and every pile `Screen` stay byte-identical (check C2-4).

**Variants** (an existing mesh, a new name; **no pile block and no pile tag on any of them**, check C2-4, so `FrontRoomsFurniturePile.Library` never picks them):
- `Kit_TaskChair_Charcoal`;
- `Kit_KeyCabinet_Putty` (+ shut door part, real LODs);
- `Kit_DeskPhone_Corded`;
- `Kit_BinderPostOrders`;
- `Kit_JackF`;
- `Kit_StationCounter_Run075` (the Run120 section cut to 0.75 m).

**New hero models** (new Blender kit modules):
- `Kit_TVWallBracket`;
- `Kit_KeySwitchPlate` (with the 0.30 × 0.10 sign);
- `Kit_FireAnnunciator`;
- `Kit_ConduitRun_StationKey`;
- `Kit_StationCounter_Run120`, `Kit_StationCounter_End`;
- `Kit_SecurityLetters`;
- `Kit_JacketDraped`;
- `Kit_RegisterBook`, `Kit_TentCard`, `Kit_Clipboard` (+ `_Wall`);
- `Kit_DeskBlotter`, `Kit_CoffeeCup`, `Kit_Ashtray`, `Kit_RadioCharger`;
- `Kit_NoticeBoard`, `Kit_TVCords`, `Kit_HookHeavy`.

**Rules for every kit:**
- era 1990 (nothing after 1993), no brand, never LEVITZ, no red in the take frame (30L §2, PAL);
- LOD 3 / 8 / 40 m for things the player handles, LOD1 at about 8 m for furniture (30L §1);
- a check render through `Tools/three_view`.

---

## 13. Code and data files (only 游戏视觉's paths; none in main until the group lands)

| Path | Change |
|---|---|
| `Assets/Levels/Modules/Station_FrontDesk_4x4.asset` (+ `.meta`) | new (§1) |
| `Assets/Scripts/Rendering/FrontRoomsStationDress.cs` | **new.** Hooked with `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` to `MapRunStarted`, `FloorChanged` and `MapRunEnded`, subscribed directly (the WebGL High-stripping rule, V4R). Acts only when `map.HasStation`. It creates "Station dress" under `map.Station` and runs: the trim; the plate; the entrance phases (no header panel; the stand-in jambs and head deactivated after the clear); the TV re-seat + attach (Beacon) + LOD hookup + light + `PowerOffForGood` at the take; the layer tagging (`StationShellBuilt`, new props per frame until `PropsDone`, the key at the KeySpot, the station lamps), by bit operations; visibility mirroring `map.Station.Visible`. Nothing happens with no station |
| `Assets/Scripts/Rendering/FrontRoomsStationTrim.cs` | new (§5) |
| `Assets/Scripts/Rendering/FrontRoomsStationTvLight.cs` | new (§8) |
| `Assets/Scripts/Rendering/FrontRoomsPlacardMount.cs` | the station branch (§6.1); the rest byte-identical |
| `Assets/Scripts/Rendering/FrontRoomsPlacard.cs` | `Mount.Station` appended, `Label` → `ST` |
| `Assets/Scripts/Rendering/Inspect/FrontRoomsInspectCatalog.cs` | EXAMINE entries, **stacked on R3's payload** (GAP 37). The entries: (1) the station placard sheet's transcript; (2) the key-plate sign (close-reading; its aim box never wins over the key); (3) the register page; **(4) the Daily Activity Report and (5) the sign-out sheet** with 01 §6's strings (30L S15 "EXAMINE-readable"; check C1-10). All blocked only on 平面视觉's paper atlas for the printed art. GAP 28 revised |
| `Assets/Scripts/Rendering/FrontRoomsLook.cs`, `FrontRoomsZoneReflection.cs` | `ReflectionZone.Station` + its cube and intensity (§10), inert |
| `Assets/Resources/Surfaces/*.mat`, `Surfaces/Textures/*` | §4 |
| `Assets/Resources/Props/Models/*` | §12 kits (FBX + sidecar + `.meta`) |
| `Assets/Resources/Rendering/StationTvLightTrack.json`, `Rendering/Reflections/Refl_Station.exr` | §8, §10 |
| `Tools/Blender/frontrooms_kit/assets/*.py`, `Tools/lookdev/bake_tv_light_track.py` | §12, §8 |
| `Assets/Settings/FrontRooms_URP.asset` | **not in this package** (check C2-8). `m_SupportsLightLayers` 1 ships as the separate hunk `W/apply/station_look_v1_urp.sh`, for the parent's explicit approval, only if L0 leaks (§9; GAP 11) |
| `Assets/Scripts/Media/FrontRoomsScreenVideo.cs` | **stacked on R3's payload** (check C1-1): `PowerOffForGood()` (public) + its latch, nothing else. Strictly needed: no run has a public power call |

**Not touched:**
- `FrontRoomsScreenVideo.cs` beyond the stacked `PowerOffForGood` hunk (R3 owns the rest);
- `FrontRoomsStationPresence.cs` (R3 edits it);
- `FrontRoomsDeskStill.cs`;
- `FrontRoomsKeyHud*`;
- every map file;
- `FrontRooms3DGame*.cs`;
- `FrontRoomsLevel0.asset`;
- Input;
- sound;
- 平面视觉's art;
- ProjectSettings.

**Editor scripts in the clone:** ASCII only, no C# tuples. `FrontRoomsObjectId.Of(obj)`, never `GetInstanceID`.

---

## 14. Contract notes for other owners (they go into `W/apply/station_look_v1_contract/README.md`)

### 14.1 关卡设计

1. **Switch-on**, in the same landing as this package:
   - `FrontRoomsLevel0.asset` `stationModule` = `Assets/Levels/Modules/Station_FrontDesk_4x4.asset`;
   - together with A26's group: M3, M6, M7 (61 §16; KIT 游戏视觉 2).
2. **The take point.** Our KeySpot is the key's real place on the hook, (10.23, 1.42, 10.40) (GAP 1).
   - PLAN §4.10 says the take point is "projected to the floor on the player's side". Please take `TakeStationKey`'s default point 0.5 m from the KeySpot along its facing (yaw 270, so −X): (9.73, 10.40), which is 61's (9.7, 10.4).
   - Alternatively, M3 passes the player's feet as `at`.
   - Until then the Relay "holds short" at the riser (KIT probe: 0.36 m), so nothing breaks.
   - `-autopilotStation`'s walk passes 2.72 m from the KeySpot, inside its 3 m reach.
3. **M3:** spawn the floor key at `StationAnchor(KeySpot)` on `Kit_KeySwitchPlate`'s `key_hook` (the marker's `host`), in D1.6's hung pose, with the tag at 1.5×, **parented under `map.Station`** so the dresser gives its renderers the Station bit (check C1-3). The key look is §3 P06.
4. **M6:** on `StationKeyTaken` the dresser drops the light **and powers the set off for good** (`PowerOffForGood`, this package, stacked on R3). When crt-tube lands, its collapse plays on that call (50 §6 timing).
4a. **Scene directional (Game / scene owner):** OR bit 1 into "Soft ambient direction"'s `renderingLayerMask` (`ApplySceneLighting`), only needed once the URP flag is on (§9).
4b. **The Relay's renderers (creature owner):** OR bit 1 into their `renderingLayerMask`, once the URP flag is on (§9).
5. **Reflection:** at Game l. 2101, `InFrontRooms` → `ReflectionZone.Station` once `Refl_Station` lands (§10).
6. **Optional:** `StationShellBuilt` could also pass the chunk root, or the station lamps' `Light`s. We search under the chunk root today (§9).
7. **Step 6 docs** (MAP_GENERATION.md from main's 91711137; LEVEL_MODULE_SPEC.md): name the asset, and add the hero cap of 100k / 120 for the station (PLAN §8).

### 14.2 平面视觉 / 信息设计

1. **Geometry deltas for `sheetA_room` / `_phase0`.** Their `geometry.json` has E2 w 1.25; the code is 1.20. The desk rect is [8.90, 7.20, **10.85**, 7.80] if GAP 5 stands. Re-render if they want (PLL: "send the numbers at once").
2. **The `Prop_StationPaper` atlas.** Strings are 叙事's (01 §6); type is 平面视觉's:
   - the register page;
   - `PLEASE SIGN IN`;
   - the DAR (with the computed date);
   - the sign-out sheet;
   - `KEY CONTROL` / `ALL KEYS MUST BE SIGNED OUT`;
   - the `STAIR KEY` plate art;
   - the phone list (no date);
   - blank-text notices;
   - the `STAIR A` tag lettering and its final colour.
3. **Lit frames for 信息设计's check:** the plate at 1 m and 3 m; `SECURITY` from the entry door; the L* 75 letters on the L* 59 fascia (61 §8).

### 14.3 The other lines

- **叙事:** none new. The readable scope is the register + the key plate (GAP 28). The strings stay as signed (01 §6).
- **声音设计:** the KIT 声音设计 items stand. The station TV is attached with `Attach(screenMount, null, FrontRoomsScreenSoundRole.Beacon)` (their rule, NOTE_1029); every other screen stays None.
- **交互设计:** the touch checks:
  - E1 1.70 and E2 1.20 at all three gaits (our trim is render-only);
  - the 1.2 m centre aisle (x 6.9–8.1 empty);
  - the key tap at the KeySpot (10.23, 1.42, 10.40), reachable within 0.9 m from (9.73, 10.40) (KIT 交互设计 2).
- **Same-line runs:**
  - **R3** (station TV EXAMINE) must land **before or with** this package. Without it the TV offers `E  TURN OFF` within 2.4 m (`ScreenVideo.InteractionPrompt`), and the screen centre is about 1.5 m from the eye at the take point (R3C §3).
  - **D1.6** gives the hung key pose.
  - **crt-tube / V2** gives the picture's collapse animation inside `SetPower(false)`; the call itself is ours (`PowerOffForGood`). **R3 is a hard precondition** (enum, Attach argument, ScreenVideo base).

---

## 15. Proofs for the build stage (each image through `W/tools/shotframe`, listed in `W/vl_pending/station_look_v1.md`)

| Proof | What, and the bar |
|---|---|
| (a) contract | `Validate` + `ValidateStation`: **0 errors** on the asset with every kit present (log line; includes P03's KeySpot not inside its footprint and every `p.y + boundsMax.y ≤ 2.85`). `StartGroupTemplate.From(asset, KitFootprint)` is non-null. MapVerification start-group 1600/1600 with `stationModule` = the asset **in the clone only**. With it empty: `map-verification-latest.json` = baseline except the timestamp, and PM / the dresser are inert (pixel-identical start frames) |
| (b) budget and layers | LOD0 ≤ 100k and renderers ≤ 120, counted on the built set. L0 leak pairs (TV on/off, L0 lamps on/off) before and after L1. "No station light on Level 0 cells and vice versa". Autopilot fps / p99 on 2554 / 20388 with layers on and off |
| (c) images, desktop 1920 × 1080 + phone (phone frames compared with Figma 2528:5403) | (1) from the stream room at Space as D opens (EXIT through E1, TV, placard); (2) side by side with PICK at its two cameras (from E1 on the set's axis; from the Level 0 corridor 4.5 m out), pixels sampled for wall, rail, VCT, border and frame against §4; **placard and sign contrast on #9AA3A6 (Weber, against PICK's placard 0.51; DEC 15:4x)**; **the placard's in-game frame through 平面视觉's `ingame_check.py`, with the glow-ink dark check against the Q16 bar of 1.30**; **the TV's blue-violet light on the blue-grey wall on every V3 card** (61 §5 风险) (check C1-12); (3) desk + register + `SECURITY` from D; (4) the TV playing, its light along the axis; (5) the riser face with board, annunciator and conduit, and the key at the KeySpot; (6) the placard on its anchor (+ EXAMINE); (7) the locked main entrance + plate in both phases; (8) E1 / E2 casings, guards, throats, reducers and bands, and the EXIT in view; (9) the take, 7 frames t 0–1.0 s (worst card drop ≥ 70 %, jewel lit, **the set off for good**); **(11) the TV + bracket's rendered min y ≥ 2.15 and max y ≤ 2.85** (check C1-8 / C2-9); (10) a corner / T-junction seam close-up. Bars: 20L §8 checks 1–6 + 30L §10.6 (1c, 2, 3, 5 at 1.2 m, 7), photosafety ≤ 3/s |
| (d) suites in the clone, module off and on | FrontRoomsMapVerification; the station sections of MapInteractionTests; `-autopilotStation` (with the profile set, the run uses our asset: 7/7 station rows); FrontRoomsInspectPlaytest (counts) |
| (e) Roslyn | `python3 W/tools/roslyn_check.py --tree <main + package> --out <dir> --all`, and `--ios`: COMPILE OK |

**VL numbers.** VL421–VL428 were claimed on 10-22 by this line for `station-v1-production` (wf_d182d9f6-79d). Every stage of that run failed on 10-23, and no rows exist, so the parent may reuse them; it places the slides (GAP 36).

---

## 16. Gaps: every choice the docs leave open, and the smallest consistent choice

1. **KeySpot vs the take point.**
   - What the sources say: KIT and the map's stand-in (MV) put the KeySpot at the take point (9.7, 10.4). 61 §4.4 separates the two: the KeySpot is not in the riser or board footprint, ≤ 1.8 m, reachable within 0.9 m of the take point (9.7, 10.4). DEC 18:3x ("KeySpot per the limits; M6's hunt aimed at the take point") and MD l. 57 (where the key lies) agree with 61.
   - Choice: the KeySpot on the hook, (10.23, 1.42, 10.40); the take point 0.5 m in front of it, (9.73, 10.40) = 61's (9.7, 10.4) (check C1-7). It stays an open item for 关卡设计, whose KIT, `TakeStationKey` docstring and task text read the KeySpot as the take point.
   - Follow-up: contract note §14.1 item 2.
2. **The screen's distance from the wall.**
   - 61 §4.4 gives the screen "离墙 0.4 米" with the hook 1.2 m south of the screen plane at z 10.4. `Kit_CRTTV` on a bracket stands its screen 0.55–0.6 m off the wall (30L §4.2).
   - **Withdrawn by the plan check (C1-7).** The hook stays at 61's z 10.4. With the real screen centre (9.35, 2.46, 11.32) G4 measures 43.9° in plan, 42° off the tilted normal and 1.64 m in 3-D, inside 30–45° and 2.0 m; check 2 is proved there.
3. **The 20° tilt.**
   - `ModuleProp` has no pitch.
   - Choice: the module holds the set level at y 2.33, which validates. The dresser pitches it on the bracket's mount (hinge 2.36) at run time. Proof: rendered min y of the set **and the bracket** ≥ 2.15, max ≤ 2.85 (C1-8).
4. **The screen surface on `Kit_CRTTV`.**
   - `Attach`'s default anchor and size are for `Kit_CRTMonitor`.
   - Choice: a scaled "screen mount" child, with no `ScreenVideo` edit while R3 is changing it.
   - Later option: a public anchor/size setter after R3 lands.
5. **Counter length.**
   - 61's x 8.9–11.0 leaves 0.92 m to the east wall face (0.89 at the rail). That breaks 61's own "东边留 1.0 米过道" and G6's 1.0 m.
   - Choice: x 8.90–10.85 (1.95 m), so the aisle is 1.07 / 1.04 m. Send 平面视觉 the rect.
6. **Counter depth.**
   - 61's rect is 0.60 deep; S05's research base is 0.76.
   - Choice: keep 61's rect: ledge 0.30 at 1.07 m, work surface 0.30 at 0.74 m, knee space 0.57. Built as Run120 + Run075 + 2 End panels, since S05 is only made in 1.20 runs.
7. **Counter colours.**
   - PAL's "suggested swatch" (oak fascia, grey top) is a synthesis. Its verdict row CONFIRMED the estimate.
   - Choice: 61 §5 and 30L §2.1: grey laminate fascia #8E8E88, oak ledge top, beige work surface.
8. **Annunciator beige.**
   - PAL gives no number.
   - Choice: `Prop_SteelBeige` = `Prop_SteelPutty`'s maps with `Prop_PlasticBeige`'s mean colour. No new hue.
9. **Light layers.**
   - (i) The conduit is built as one static prop: `ModuleProp` has no scale, and 1 m EMT pieces cannot make a 0.3 m drop.
   - (ii) The leak bar: > 1 % of the receiving surface's luminance (SP).
   - (iii) The masks (rewritten, C1-4 / C2-3): station renderers (shell incl. ceiling, set, props, key) Station only; opening trims both; TV light Station only; **only the two entrance lamps both, the other station lamps Station only**; Level 0 lamps untouched; masks changed by bit operations only. The default-layer lights that would lose the station (the scene directional, the Relay) get contract notes (§9, §14.1 4a/4b). Only if L0 shows a leak.
10. **The "Station" name.**
    - Naming a rendering layer needs ProjectSettings/TagManager, which is not ours.
    - Choice: bit 1 by code constant; no name.
11. **The URP asset's owner.**
    - `Assets/Settings/FrontRooms_URP.asset` is outside the task's listed paths. 61 §15 and PLAN §7.10 call it 游戏视觉's.
    - Choice (C2-8): **not in the package.** A separate one-line hunk, `W/apply/station_look_v1_urp.sh`, for the parent's explicit approval; the package's masks are inert without it.
12. **TV light shadows.**
    - V3 says Soft on desktop and none on WebGL. 30L §3.1 proposed Hard 512 on WebGL. r7 says not to rely on shadows.
    - Choice: the V3 row; layers carry the leak.
13. **Ceiling.**
    - It is decided as Level 0's. **Revised (C2-3c):** `Station_Ceiling` is an identical copy of `L0_Ceiling`, so it looks the same but the map splits it out and the Station mask reaches it.
    - The "0.30 m drywall over the openings" is PICK's ceiling band (0.30 wide on the wall line, 0.06 deep, #D9D6CC), at E1 and E2.
14. **Corner guards.**
    - Choice: on the station face only (PICK), at E1 and E2 (61 §5). None on the riser.
15. **Cased frames.**
    - PICK's section: face 0.05, proud 0.012, both faces, 0.012 throat liners. 61 §5 adds E2.
    - **Revised (C1-6 / C2-7):** the static entrance gets **no** frame (D is the flush stream door, 不改); the stand-in jambs and head are hidden by deactivating their GameObjects.
16. **Reducer position.**
    - Choice: at the opening's outer (Level 0) face, per 61 §6's text. PICK put it at the station face.
17. **The bare header over D (2.66–2.90).**
    - **Closed (C1-5 / C2-6):** no panel; the header stays bare until `ClearStartArea` (KIT 游戏视觉 5; 61 §6 allows a seam at D).
18. **The plate after the clear.**
    - **Closed (C1-6):** with no frame at D, `PlateAnchor` keeps the plate 75 mm from the opening in both phases; no shift.
19. **The border course.**
    - Choice: one 0.305 m course from the wall face (PICK), with its own mesh UVs; field tiles cut at its edge.
    - It runs round the riser and stops at D.
20. **Key cabinet place** (not in FD-b).
    - Choice (revised, C1-10): the riser's south face, x 11.25, door shut, staff side, off the plate's wall (30L §10.2), so the sign-out sheet "by the cabinet" is within G9's 2 m.
21. **The sign-out clipboard.**
    - Choice (revised, C1-10): on a nail by the cabinet, riser south face x 10.72: 1.36 m from the seat (G9).
22. **Notice board** (no size or place given).
    - Choice (revised): the east wall, staff side, z 8.40, about 0.60 × 0.45, facing the staff strip (the riser's south face holds the cabinet group now).
23. **Wall clock.**
    - "Over E1" (30L S30) breaks B8's axis rule.
    - Choice: north wall x 3.0, y 2.2, the map's stand-in spot.
24. **Desk lamp.**
    - T20 V1 lists one; 30L S28 makes it tier P2 and off (G3).
    - Choice: not placed in v1.
25. **Seating and other dressing.**
    - Choice: no visitor seating, trash bin or paper stack (none is in FD-b's decided lists, 61 §4.4). **The post-orders binder (S17) and the empty watchclock hook (S26) are placed** (§3.5, C1-11).
26. **Ashtray.**
    - Choice: the glass desk ashtray (S25's A variant). No smoking stand and no `NO SMOKING` sign, which are B only.
27. **Paper art.**
    - `Prop_StationPaper`, the key-plate art and the tag lettering do not exist yet.
    - Choice: ask 平面视觉. Until then, blank paper with no invented type. The group should not land with a blank register or tent card unless the parent accepts it.
28. **EXAMINE scope.**
    - Choice (revised, C1-10): the key-plate sign, the register, **the DAR and the sign-out sheet** (30L S15 decides the clipboards are EXAMINE-readable), with 01 §6's strings; the printed art waits on 平面视觉's atlas.
29. **The placard sheet.**
    - Choice: `sheetA_room` when `map.ExitLocked`, else `sheetA_room_phase0`. The station branch skips `KeepClearAtStart`.
30. **Placard parent.**
    - Choice: `map.Station`, so it outlives the stream rooms (PLAN §4.8).
31. **Station cube.**
    - Choice: captured at the room centre, 1.62 m, TV off, intensity 0.5. Inert until the game hook.
32. **Wear on `Station_Wall` / `Station_Floor`.**
    - Choice (revised, C1-13): PICK's clean eggshell and VCT: macro tone, dirt and grime 0 ("the one kept room").
33. **Rail and base ends.**
    - Choice: square ends at the casing's outer edge, and 0.05 m short of D's opening (the stream leaves open to 90°).
34. **F-jack.**
    - There is no such kit.
    - Choice: a variant of `Kit_JackBNC`.
35. **Clone.**
    - `W/proj_station` (10-22, main 18acaa16, Unity 6000.3) is listed in the ledger (PROGRESS.json `clones`), so it is not moved.
    - Choice: `mkclone.sh proj_station` makes a fresh `W/proj_station_2`. Use it for every stage; the parent adds it to the ledger's clones.
    - Unity 6000.6.3f1; ≤ 2 visual Unity jobs (`W/tools/unity_jobs.sh` before each launch).
    - Test player id `com.redwang.frontroomsss.test.proj_station_2`, `open -n`, always quit.
36. **VL numbers.**
    - Choice: reuse VL421–VL428 (claimed for the dead station-v1-production run; no rows exist). The parent decides and places.
37. **R3 collision.**
    - R3's payload edits `FrontRoomsScreenVideo.cs`, `FrontRoomsStationPresence.cs` and `FrontRoomsInspectCatalog.cs`.
    - Choice: we touch only `InspectCatalog`, and rebase on R3 after it lands.
38. **WebGL.**
    - Red's A covers all platforms. Nothing else in this package is WebGL-specific: the placard lens stays off on WebGL, as today.
    - The WebGL preview define is checked for cost only.
39. **The third skin's corner posts.**
    - Choice: no `FrontRoomsTransitionKit` change unless proof (c)(10) shows a defect.
40. **Jacket and chair.**
    - Choice: jacket #59647A (30L §10.3); chair charcoal (PAL). PAL is silent on the jacket.
41. **`Prop_StainlessBrushed`** (61 §5 names it). Creating it under that name re-skins every wall plate that `FrontRoomsWallFixtureRenderer.Stainless()` serves. Choice (C2-4): the same values under `Station_StainlessGuard`; the global swap stays a separate decision.
42. **The TV's plastic.** PAL confirms beige or putty CRT plastics; Red's pick image shows the black blockout set, but Red approved the walls, not the props. Choice (C1-9): `Prop_PlasticBeige`.
43. **The power-off call.** No run has a public one (main, R3, crt-tube). Choice (C1-1): `PowerOffForGood()` in this package, stacked on R3's ScreenVideo, 8 lines.
44. **The scene directional and the Relay on Station-only surfaces.** Not ours to change. Choice (C1-4 / C2-3a): contract notes (§14.1 4a, 4b), applied in the clone only for the proofs.

---

## 17. The plan check (2 checkers, 24 changes), applied 2026-10-29 16:2x (build stage)

Both checkers returned CHANGES. Every change is applied in place above; this table says where and how. C1-n = checker 1's n-th change, C2-n = checker 2's.

| # | Change | How it was applied |
|---|---|---|
| C1-1 | The TV off for good at the take | §8 take row, §13, §14.1 item 4, §14.3, GAP 43: the dresser calls `PowerOffForGood()` on `StationKeyTaken`; the call is added on R3's ScreenVideo (stacked), and R3 is a hard precondition |
| C1-2 | Attach passes `FrontRoomsScreenSoundRole.Beacon` | §8 step 2, §14.3: `Attach(screenMount, null, Beacon)`, stacked on R3 |
| C1-3 | The key gets the Station bit | P06, §9 station renderers, §14.1 item 3: M3 parents the key under `map.Station`; until then the dresser finds it at the KeySpot |
| C1-4 | The light-layer scheme follows 61 §7; the default-layer lights are listed | §9 rewritten: only the two entrance lamps both layers; the scene directional, Level 0 lamps, stream lights and the Relay listed and each handled (contract note or open item) |
| C1-5 | No header panel over D | §5 row removed, §6.3, GAP 17 closed |
| C1-6 | The static entrance is the same flush door: no frame, the stand-in jambs hidden, the plate unshifted | §6.3, GAP 15, GAP 18 |
| C1-7 | Keep the take point (9.7, 10.4) | §1 KeySpot (10.23, 1.42, 10.40), §2, P03, §7 G4/G2 recomputed (43.9°, 1.64 m; 66°), GAP 1, GAP 2 withdrawn |
| C1-8 | Bracket bottom ≥ 2.15, not only the set | P01 hinge raised to 2.36 (lowest 2.19, top 2.83), §8 step 1, proof (c)(11) |
| C1-9 | Beige / putty TV plastics | P02 `Prop_PlasticBeige`, GAP 42 |
| C1-10 | G9 and S15: the sign-out sheet within 2 m, the clipboards EXAMINE-readable | P07/P08 to the riser's south face (1.36 m from the seat), P17, §13 InspectCatalog entries (4) and (5), GAP 20, 21, 28 |
| C1-11 | S26 and S17 placed | §3.5 P28 `Kit_HookHeavy`, P29 `Kit_BinderPostOrders`; §3 not-placed list, GAP 25 |
| C1-12 | Three missing checks in proof (c) | §15 (c)(2): Weber contrast on #9AA3A6 vs PICK 0.51; `ingame_check.py` with the Q16 bar 1.30; the TV colour on every V3 card |
| C1-13 | Clean paint, as PICK | §4 `Station_Wall`, GAP 32 |
| C1-14 | The 0.74 work surface runs back under the ledge (z 7.23–7.80) | P09 section; P17–P22 positions moved onto it |
| C2-1 | Floor-origin wall kits at y 0; every origin stated; outlets at centre 0.30 | P01, P03, P04, P05, P07, P08, P23, P25, P26, P27, P28 rows; §1 and §3 checks |
| C2-2 | The KeySpot outside P03's footprint | P03 body-only sidecar collider; §1 KeySpot row; proof (a) |
| C2-3 | Light layers: directional, key, ceiling, entrance lamps, bit operations | §9 rewritten; §4 `Station_Ceiling` (identical copy); GAP 9, 13 |
| C2-4 | Shared names | `Kit_CRTTV_Station`; no pile block on the variants; `Station_StainlessGuard`; `Prop_StationKeyPlate`; §4, §12, GAP 41 |
| C2-5 | Beacon, the LODGroup hookup, the power-off dependency | §8 step 2 and LODs bullet, take row; §13 |
| C2-6 | Drop the D header panel | as C1-5 |
| C2-7 | No frame at D; hide the stand-ins by deactivating their GameObjects | as C1-6 |
| C2-8 | The URP asset is not in the package | §9 URP row, §13, GAP 11: separate hunk for the parent's approval |
| C2-9 | Bracket min y proof; the hero pass keeps `Kit_CRTTV`'s bounds | P02, §8 step 1, proof (c)(11) |
| C2-10 | Counts and materials | §11 renderers ≈ 108; trim split by mask; `Prop_Cork` in §4 |
