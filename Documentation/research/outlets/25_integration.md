# 25 — Outlets integrate: the kit models in the game, LODs, renders, cost, and the landing package

Status: **READY, 2026-10-14 16:4x.** The landing package dry-runs clean on today's main (`0a9ee7f` + working tree):

```
bash /Users/redwang/FrontRoomsVisualWork/apply/outlets_int.sh            # dry run: DRY RUN OK, 97 / 97 bases match
bash /Users/redwang/FrontRoomsVisualWork/apply/outlets_int.sh --apply    # writes 97 paths; backs main's 3 changed files up first
```

- **It supersedes `outlets_c1`.** Do not run `outlets_c1.sh`; its OfficeKit hunks conflict with the time-sliced dresser.
- **Two more pieces wait for approval** and are not in this package:
  - **P-1:** LOD1 and LOD2 inside the FBX. The package is `W/apply/outlets_lods.sh`. It is stacked on `outlets_int`, so it refuses until `outlets_int` lands.
  - **P-4b + P-6:** five new materials and instancing on nine. This is a RenderSetup proposal, not a package. It also waits on Red's tint pick (§6.6, VL235).
- **The map draws no outlets until 关卡设计 applies the contract** (§8). Only the title corridor and the Office kit draw them until then.
- **One change from the 10-08 package (§5).** The Exit room branch now keeps main's look: one EXIT sign and nothing else. The 10-08 package would also have revived a 10-01 greybox "threshold marker".

Written by the outlets workflow's integrate stage. `W` = `/Users/redwang/FrontRoomsVisualWork`. All work ran in private clones; Unity was never opened on Frontrooms3D. In Red's project only this folder changed (`25_integration.md`, `images/25_*`, `data/25_*`), plus the VL230–235 rows in `Documentation/VERIFICATION_LOG.md`.

**Load rule.** Every millisecond has the 1-minute load average next to it. Above 32 it is marked *under load, re-measure*. Counts (fixtures, draws, triangles, colliders) do not depend on load.

---

## 0. Results in one table

| Check | Result | Load (1 min) |
|---|---|---|
| Apply package `outlets_int` on main `0a9ee7f` + tree | **DRY RUN OK**, 97 / 97 bases. A scratch `--apply` gave MANIFEST OK, then ALREADY APPLIED. 47 new GUIDs, none used in main | — |
| Compile, main + payload (Roslyn, outside Unity) | **0 errors** with the OSX defines and with the iOS defines. Main alone also compiles | — |
| Unity batch compile + Exit check, landing clone (main + `outlets_int` only) | **0 errors.** Exit check PASS: 5 / 5 Exit variants hold **1** EXIT sign, no threshold marker, an outlet set and 0 old plate boxes | 31 → 33 |
| Kits imported | **22** kits. Each has its FBX, sidecar JSON and both `.meta` files. LOD0 only, as main's kitlib builds them | — |
| Contract probe, 100 seeds × 25 chunks | **PASS.** 315,052 faces; 84,809 fixtures (33.9 per chunk, max 68). Every spec §4.4 rule count is 0. Same counts as C1, except the new kits (§3.2) | 43 → 32 (counts) |
| Install per chunk (the map's one call) | hook median **0.197 ms**, p95 0.303; warm median 0.153 ms. 0 bytes of garbage per plan. *Under load, re-measure* (the valid C1 figure is 0.149 ms at load 12–15) | 34 → 51 |
| Colliders and renderers added | **0 and 0** (same build with and without outlets). Rendering adds 0 GameObjects (T-R2, 22 / 22 shots) | — |
| Draws per pass, 1,000 random views | **What lands today** (LOD0 only, no instancing): mean 34.6, p95 60, max 104. **With P-1 + P-6:** mean 13.3, p95 25, max 44 | counts |
| Triangles per view, same views | **What lands:** mean 25.1 k, p95 43.3 k, max 76.2 k. **With P-1:** mean 1.1 k, p95 3.2 k, max 8.5 k | counts |
| Per chunk (seeds 2554 and 20388, 25 chunks each) | 36.5 / 37.2 fixtures (max 49 / 58); 0 renderers; 0 colliders; 1.4–2.1 conduit attachments (tiles and straps) | counts |
| Autopilot, seed 20388, with graphics | outlets **on**: 58.9 fps, p95 16.8 ms, p99 17.0 ms. **Off:** 59.1 fps, p99 17.1 ms. Both PASS | 42 → 31: marginal |
| Autopilot, seed 2554 (re-run with FMOD banks) | **on** PASS 58.0 fps, p99 27.9 ms; **off** PASS 58.7 fps, p99 21.4 ms. *Under load, re-measure* | 28 → 123 → 49 |
| Relay navigation / map interaction / Level Designer / map verification / fixture ticks (10-08, same code) | 60/60, 152/152, 139/139, 100/100 seeds, 28/28. Every suite was run with outlets **on** and **off** | 240–600 (counts) |
| Dress step test (10-08) | 36 ok, 0 failed, on and off | 420–500 |
| Level Designer preview | draws the outlets: 101–778 pixels change per module view between on and off (8 views) | 31 → 46 |
| Renders (§6) | every zone; 23 kit views at 0.30–0.48 m; dead lamps; one chunk top-down; the LOD dolly; the L-1 tint | 10-08: 400–600 (images only) |
| Verification log | **VL230–235** placed (6 checks, 15 images); cover updated | — |

---

## 1. What happened to this stage

| When | What |
|---|---|
| 2026-10-08 00:06–01:53 | First attempt (`W/outlet_int`). It built the kits with and without LODs and fixed the O1 plate normals. It added the planner and renderer changes (§3) and made the first captures. It stopped before the report. |
| 2026-10-08 19:28–20:08 | Second attempt (`W/outlet_int2`). It made a fresh clone and ported the OfficeKit hooks to the time-sliced dresser, which had just merged. It re-made RoomStream on main after the EXIT sign merged at 19:40, then ran the captures and the tests. It built `outlets_int` and `outlets_lods` and tested both on scratch copies. The weekly usage limit hit at 20:08, before the report, the images in `research/` and the VL rows. |
| 2026-10-14 16:17–16:4x | This resume. Machine check: Blender 4.3.0 runs; Unity batch runs (compile 0 errors, 14 s). The dry run passed on today's main. The Exit-branch review (§5) found the revived marker and fixed it. The package was rebuilt, compiled and re-tested. The 10-08 chain had stopped before the Level Designer captures, the autopilot pairs and the 100-seed probe; those ran now. Then this report, the images and VL230–235. |

**Main since the package's first base (`bc0aa79`).** All 97 bases are byte-identical today. Main's other changes do not touch the outlet code paths: the interactables kit finals (FBX/JSON), COV-4 (`FrontRoomsScreenVideo.cs`), the GD3 generator (new files), the cloud build script and ProjectSettings. Roslyn and the Unity compile above include all of them.

**Codex audit.** `codex_audit/20_findings.md` still does not exist. No audit file names an outlet finding (checked 16:19).

---

## 2. What lands (`outlets_int`, 97 paths)

| Paths | Action | Notes |
|---|---|---|
| `Assets/Scripts/Office/FrontRoomsWallFixtures.cs`, `…WallFixtureSet.cs`, `…WallFixtureRenderer.cs` + their `.meta` | add | GUIDs `14ab7ea1…`, `6f896ecc…`, `e2a60678…`. The `.meta` files are written first, so an open Unity cannot mint its own GUIDs |
| 22 kits under `Assets/Resources/Props/Models/` (§3.1): `.fbx`, `.json` and both `.meta` | add | 44 `.meta` first, then each sidecar before its FBX. The FBX `.meta` remaps only to materials main has (P-4b slots dropped, §3.1) |
| `Tools/Blender/frontrooms_kit/assets/outlet_common.py` | replace | The O1 plate field normals (§3.3) |
| `Assets/Scripts/Office/FrontRoomsOfficeKit.cs` | replace (`.meta` kept, `ae758f35`) | Outlet hooks on the time-sliced dresser (§3.4) |
| `Assets/Scripts/FrontRoomsRoomStream.cs` | replace (`.meta` kept, `629abdbd`) | The 45 placeholder boxes with colliders go; `StreamFaces` + `InstallLocal` come in; one Exit sign (§5) |

- **Kept, never written:** both `.meta` files above, and `FrontRoomsMapWorld.cs` (sha1 `11586bbc`, the contract's base; the script only notes a change).
- **Not in it:** the MapWorld contract (§8), P-1 (`outlets_lods`), P-4b/P-6, and the clone-only tests (`Assets/Editor/OutletCensus/*`).
- **Files in the package folder** (`W/apply/`): `outlets_int.sh`, `_bases.txt`, `_expected.txt`, `_keep.txt`, `_payload/`, `_base/` (v2) and `outlets_int.diff`. The 10-08 build is kept in `W/outlet_int2/pkg_outlets_int_1008/`.

**What changes in the game when it lands:**
- **Title corridor:** R3 draws the real kits in every room variant, with no collider and no shadow. Per room (probe, 20 slot sequences): Lobby 6.8 fixtures (steel duplexes), Shift 7.9, Office 15.6 (plates, jacks, switches and pod floor feeds), Run 5.35 (all white), Exit 3.0. The ivory plastics use the existing fallbacks until P-4b: `Prop_PlasticBeige` for plates and `Prop_PlasticPutty` for devices. The orange IG device uses the ivory device material. Frames: `images/25_landing_tr2_sheet.jpg`.
- **Office kit:** each pod gets one `Kit_CubiclePanel_Powered` spine panel and two floor feeds. Stage 2 hides plates behind wall units and powers wall-row desks.
- **Map:** nothing yet; see §8.
- **Cost before P-1:** every kit draws its LOD0 out to 12 m, about 25 k triangles per view (§7.2). P-1 cuts that to about 1.1 k.

---

## 3. What the integrate stage changed

### 3.1 The kits (O1–O3, 22 kits)

| Kit | LOD0 / LOD1 / LOD2 tris | Slots |
|---|---|---|
| `Kit_OutletDuplex` (+ `_Brown`, ceramic) | 2,880 / 954 / 42 | ThermosetIvory, NylonIvory, PlasticBlack, Brass |
| `Kit_OutletDuplex_Steel` | 2,584 / 954 / 42 | Aluminium, NylonIvory, PlasticBlack, Brass |
| `Kit_OutletDuplex20` (+ `_IG`, orange device) | 2,920 / 986 / 42 | ThermosetIvory, NylonIvory (PlasticOrange), PlasticBlack, Brass |
| `Kit_OutletDuplex_Cracked` | 3,416 / 1,125 / 42 | as Duplex |
| `Kit_OutletDuplex_Jumbo` | 2,956 / 954 / 42 | as Duplex |
| `Kit_OutletBare` | 3,508 / 1,144 / 68 | NylonIvory, Aluminium, Rubber, Brass |
| `Kit_OutletScrew` | 266 | ThermosetIvory |
| `Kit_OutletBlank` | 1,412 / 426 / 28 | ThermosetIvory, PlasticBlack |
| `Kit_OutletToggle1` / `Toggle2` | 2,296 / 780 / 40; 3,660 / 1,462 / 56 | ThermosetIvory, PlasticBlack, NylonIvory |
| `Kit_JackPhone` / `JackData` / `JackBNC` | 2,118 / 686 / 36; 2,762 / 1,000 / 68; 2,748 / 1,008 / 56 | ThermosetIvory, PlasticBlack, (NylonIvory / Chrome), Brass |
| `Kit_JackPhone4Prong` | 1,672 / 628 / 38 | Ceramic, PlasticBlack, Brass |
| `Kit_OutletHandyBox` | 3,860 / 1,300 / 80 | Aluminium, NylonIvory, PlasticBlack, Brass |
| `Kit_ConduitEMT` / `Kit_ConduitStrap` | 576 / 220 / 24; 294 / 106 | Aluminium |
| `Kit_FloorBoxTombstone` / `…2` | 3,576 / 1,118 / 84; 5,112 / 1,836 / 92 | Aluminium, NylonIvory, PlasticBlack, Brass |
| `Kit_CubiclePanel_Powered` | 1,460 / 184 | SteelPutty, PlasticBlack, FabricCubicle, NylonIvory |

- **LODs.** The landing FBX hold LOD0 only. LOD1 and LOD2 come with P-1 (`outlets_lods`). It is the same convention as the interactables spec (`research/interactables/10_spec.md` §8): `kitlib.make_lods()` plus hand-built `fr_lods` part sets, and `build_asset.py` calls it. Seven existing kits rebuild byte-identical with the patched kitlib: geometry, normals and JSON (crt_monitor, cubicle_panel, copier, lock_escutcheon + Brass, door_leaf_steel_lite, bar_stool). Unity's importer already reads `lodDistances`, and R3 picks the LOD meshes by name. `Kit_OutletScrew` and `Kit_CubiclePanel_Powered` are not in `outlets_lods`, because their LOD build equals the landing one.
- **Materials.** R3 does not use the FBX's imported materials for plates and devices. It maps each slot through its palette (`PlateMaterial`, `DeviceMaterial`, `SlotMaterial`), with fallbacks until P-4b. The FBX `.meta` files therefore remap only the slots main has. The five new slots (ThermosetIvory/Aged/Almond, NylonIvory, PlasticOrange) were dropped from the remaps, so Unity never points at a material that does not exist.
- **Import check (full clone, 10-08):** all 22 kits imported. Every slot the palette needs resolved, and every material was instanced on the FrontRooms/Surface shader (`data/25_prepare_full_1008.txt`).

### 3.2 Planner and renderer (`FrontRoomsWallFixtures.cs`, `…Set.cs`, `…Renderer.cs`)

Diff against C1: `data/25_c1_to_int.diff.txt`. The structs and the four contract functions are unchanged, so the map contract is unchanged.
- **Tall halls (spec §1.6):** a stainless line becomes a surface **handy box on EMT conduit**. R3 draws the conduit run from the box's `conduit_base` anchor up to the ceiling. The run is 1 m tiles, with the last tile scaled. A strap goes near the box and then every 1.5 m. Each tile and strap is culled and LOD-picked on its own. Probe: 1,027 handy boxes in 100 seeds, replacing 975 steel duplexes.
- **Level 0 phone companions:** a quarter are the brown **4-prong block**, a 1960s–70s relic (146 in 100 seeds).
- **Keep-outs and stage 2** cover a handy box's full depth (55 mm) and its conduit up to the ceiling. A wall unit in front of the run hides all of it.
- **A handy box is never tilted or rolled.** It keeps its cover screws, because it hangs on its conduit.

### 3.3 O1 plate shading (`outlet_common.py`)

The O2 stage found it and fixed it in its own module. This stage applied the same crown normal pass to O1, measured on the exported FBX:

| Kit | Field corners | Before: max off the crown, corners over 1° | After |
|---|---|---|---|
| Duplex, Duplex20, Duplex20_IG | 684 → 936 | 4.83°, 202 | 0.040°, 0 |
| Jumbo | 912 → 1,164 | 4.83°, 218 | 0.034°, 0 |
| Cracked | 823 → 1,157 | 5.52°, 244 | 0.044°, 4 (the crack's own break faces, 90° by design) |

### 3.4 Office kit (`FrontRoomsOfficeKit.cs`)

Main's dresser is time-sliced (Q13, `FrontRoomsDressJob`), so the C1 hooks no longer applied. They now run as `FrontRoomsDressBuild` "then" callbacks on the spawned objects. Stepped and one-call dressing therefore give the same result, and no rng is drawn (diff: `data/25_OfficeKit.0a9ee7f.diff.txt`).
- **Wall units** report their sidecar box as a stage-2 occluder, cords and coils included.
- **A wall row's last post** powers the row: each desk gets a receptacle within 2 m, the middle desk first.
- **A pod's last post** places two floor feeds, one 0.15 m outside each end panel. One spine panel per pod becomes `Kit_CubiclePanel_Powered`, with the same bounds and collider.
- **Dress step test:** 36 ok, 0 failed, with and without outlets.

---

## 4. Tests run on the landing state

All in `W/proj_outlet_int_t` = main + `outlets_int` only, with no P-1 and no P-4b.
- **10-08, on main `bc0aa79` + tree:** every map suite, the dress step test, the T-R2 capture and the 10-seed probe.
- **10-14, on main `0a9ee7f` + tree:** the compile, the Exit check, the 10-seed and 100-seed probes, the autopilot pairs and the Level Designer captures.

Main's only changes in between are listed in §1; none touches these paths.

| Suite | On | Off | Load |
|---|---|---|---|
| Relay navigation | 60/60 hunts arrived, 0 pass-throughs, 0 frames in architecture or furniture | same | 415–425 (counts) |
| Map interaction tests | 152 passed, 0 failed | same | 344–402 |
| Level Designer tests | 139 passed, 0 failed | same | 282–344 |
| Map verification | 100/100 seeds, 924 module rooms placed | same | 282–309 |
| Fixture tick tests | 28 passed | same | 242–291 |
| Dress step test | 36 ok, 0 failed | same | 420–500 |
| T-R2 capture (edit mode, root 4992) | 22 / 22 shots draw outlets; 0 GameObjects, colliders or renderers added; R-4 stage 2: 6 + 4 plates hidden behind units, 0 cuts on drawn plates, pods and wall rows 14/14 and 23/23 powered within 2 m | — | 242–250 |
| Probe, 10 seeds (10-14) | PASS; counts identical to 10-08 | — | 34 → 51 |
| Probe, 100 seeds (10-14) | PASS: 315,052 faces, 84,809 fixtures, 0 on every rule; 208 furniture boxes near plates, all box-only (as in C1); stage 2 hid 277 plates, added 4 receptacles; pods 632/632 and wall rows 109/118 desks powered within 2 m | — | 43 → 32 |
| Exit check (10-14, §5) | PASS | — | 31 → 33 |

---

## 5. The Exit room: one sign, one branch, nothing reverted

The visual chat asked the critics to check three things. RoomStream's EXIT sign merge (10-08) put the Exit-room door sign inside the old outlet branch, because that branch also took Exit. The checks: are there now two Exit-room signs, are there two Exit branches, and does anything revert the exit-sign code?

- **Main today:** `if (Lobby || Shift || Exit) { 3 placeholder plates; if (Exit) the door sign }` … `else if (Exit) { greybox threshold marker }`. The second Exit branch has been dead code since 10-01 20:07, when Exit joined the first branch.
- **The 10-08 package** removed the first branch. Exit then reached its own branch, which held the sign plus the **revived** 10-01 greybox marker: a 2.8 m × 3 cm × 12 cm strip of wall material across the floor at mid-room, with its collider stripped. No one asked for that marker, and the Exit room was verified without it (exit-sign R4, VL211).
- **Fixed today:** the Exit branch holds only `room.exitSigns[ruleIndex] = new[] { BuildDoorExitSign(…) }`, the same single call as main. The dead marker line is gone. `roomstream_patch.py` regenerates the payload from main's file (`data/25_roomstream_patch.py.txt`; diff `data/25_RoomStream.0a9ee7f.diff.txt`).
- **This overrides the outlets spec.** `10_spec.md` §3.5 item 4 (written 10-03, before the EXIT sign track) proposed reviving the marker as "visual-owned and harmless". The EXIT sign track then verified the Exit room without it. If the visual chat wants the marker back, it is one `Box(…)` line plus the collider removal from the 10-08 payload (`W/outlet_int2/pkg_outlets_int_1008/`).
- **Not reverted:** `BuildDoorExitSign`, the Run hanging signs, `exitSigns` and every other exit-sign line are unchanged. The diff touches only the placeholder branch, the Exit branch, `outletMaterial` and the new `StreamFaces`.
- **Checked in Unity** (landing clone, `FrontRoomsOutletIntegrateCapture.ExitCheck`, `data/25_exit_check_1014.txt`):
  - 5 / 5 slots' `Profile props / Exit` hold exactly **1** "exit sign over door" and **no** threshold marker;
  - each holds an outlet set with 1–5 fixtures;
  - 0 "outlet plate" boxes remain anywhere in the scene.

---

## 6. Renders

How they were made:
- **Clone:** `W/proj_outlet_int`, the full state, 2026-10-08 19:54–20:01, load 400–600. It holds main + `outlets_int` + P-1 LODs + the P-4b/P-6 proposal; the images show the state after approval.
- **Capture:** edit-mode `Camera.Render` through the game's URP path with post, 1920×1080, map root 4992, seed 2554.
- **Light:** the game's own lamps only.
- **Harness:** `data/25_FrontRoomsOutletIntegrateCapture.cs.txt`, clone-only.
- **Landing state** (LOD0, fallback materials): `images/25_landing_tr2_sheet.jpg`, from the 10-08 T-R2 run on the landing clone.

### 6.1 Every zone at eye height (VL230)

`images/25_wide_zones_annot.jpg` (sheet) and `25_wide_{L0_Low,L0_Standard,Tall,Office}_2554_annot.jpg` plus `25_stream_{StreamLobby,StreamOffice}_annot.jpg`. Rings name each fixture and its distance.
- **Level 0 Low (2.4 m) and Standard (2.9 m):** duplexes low on the paper, a jumbo, a toggle at 1.22 m. A cracked plate and a scorched duplex sit further off.
- **Tall (5.4 m):** handy boxes with EMT runs to the ceiling.
- **Office:** wall plates with phone and data companions, and a floor feed at a pod end.
- **Title stream:** Lobby has steel duplexes along both walls; Office has plates, a phone/data pair and an IG plate.

Per view: 8.7 fixtures, 13–14 draws, 1.0–1.1 k triangles.

### 6.2 Each kit at 0.30–0.48 m (VL231)

- **Sheets:** `images/25_close_zoom_sheet.jpg` (narrow lens) and `25_close_game_sheet.jpg` (the full frames).
- **Single frames for 10 kits:** `25_close_<kit>.jpg` / `25_zoom_<kit>.jpg`.
- **23 views:** Duplex ivory, aged, brown and papered over; Duplex20; IG; Steel; Cracked; Jumbo; Bare; Blank; Toggle1; Toggle2; JackPhone; JackData; JackBNC; the 4-prong block; the handy box + EMT; the strap; two floor boxes; the screw at 0.14 m; and the powered panel.
- **Placements:** each view is a real placement of that kit in the map, lit by its own troffer.
- **Result:** the slots, ground holes, screws, the toggle bat, the jack cavities and the floor-box lids all hold up.
- **The Bare kit's gap** reads dark brown here, not grey. VL078's FLAG came from the Blender render.

### 6.3 Dead lamps (VL232)

Same plate, three lamp states; mean luma of the plate field vs the paper ring (`data/25_dead_ab_luma.json`):

| State | Plate | Paper | Plate / paper |
|---|---|---|---|
| Lamp as generated | 155 | 112 | 1.39 |
| Its own lamp dead | 121 | 83 | 1.46 |
| 3 lamps dead | 97 | 67 | 1.46 |

The plates darken with the wall (−38 % against −40 %). They do not glow: there is no emissive and no self-light. Natural dead-lamp cell: `images/25_dead_lamp_natural.jpg` (0.42 m and 1.67 m).

### 6.4 One chunk from above (VL233)

`images/25_top_2554_overlay.jpg` + `25_top_2554_crop.jpg`; data `data/25_top_2554_plan.json`.
- **Chunk:** seed 2554, chunk (−1, −2).
- **Contents:** 42 fixtures (6 added by the Office kit), 124 wall faces, 0 hidden, 0 conduit parts.
- **Overlay:** faces by kind, the excluded margins (0.30 m at face ends; 0.20 m beside arches, doors and windows), kit letters, floor feeds, lamps by temperament and dead lamps.
- **Rules hold:** no fixture on a door, window or arch reveal, or inside a margin.

### 6.5 LOD dolly (VL234)

`images/25_lod_dolly_crops.jpg` + `25_lod_dolly_frames.jpg`.

| Distance | LOD | Plate height on screen |
|---|---|---|
| 0.30 m | LOD0 | 262 px |
| 1.63 m | LOD1 | 48 px |
| 3.09 m | LOD1 | 26 px |
| 6.14 m | LOD2 | 13 px |
| 11.08 m | LOD2 | 7 px |

- **No pop:** the face keeps its two slot groups down to LOD2.
- **Needs P-1.** Without it, the landing state draws LOD0 at every distance.

### 6.6 Ivory plate vs the paper, look-dev L-1 (VL235): WAIT-RED

`images/25_l1_plate_tint_option.jpg`, data `data/25_l1_measure.json`. The reference is the spec's calibration photo DSC00161 (`01_period_research.md` §1.1).

| Plate tint | Plate / paper luma | Plate B/R vs paper B/R | Reads at 0.3 m |
|---|---|---|---|
| Photo DSC00161 | **0.88** | 0.52 vs 0.65 | — |
| P-4b `#E6DDC2` | 1.39 (1.46 with lamps dead) | 0.73 vs 0.47 | clean ivory, brighter than the paper |
| L-1 luma fit `#9E9147` | **0.89** (0.89, 0.90 dead) | 0.38 vs 0.47 | olive-green |

- **The fit matches the photo on both numbers**, brightness and relative warmth, but at 0.3 m it reads as an olive plate.
- **Red's call:** keep the clean ivory, take the photo-true olive, or try a darker ivory with the same hue (not rendered).
- **P-4b's RenderSetup lines wait on this pick** (§9).

---

## 7. Cost

### 7.1 Per chunk

From the contract probe (100 seeds) and the capture clone (seeds 2554 and 20388):
- **Fixtures:** mean 33.9 per chunk, max 68 (100 seeds). In the two capture seeds: 36.5 and 37.2, max 49 and 58.
- **Conduit:** 53 and 35 attachments per 25 chunks.
- **Renderers, colliders and GameObjects:** **0**. R3 draws with `Graphics.RenderMeshInstanced` from the chunk's `FrontRoomsWallFixtureSet`. There is no shadow casting, nothing to hit, no nav obstacle, and no block on the Relay's sight ray or the player's E ray.
- **Install time per chunk:** median 0.15–0.20 ms (§0). That is under the 0.5 ms target.

### 7.2 Per view (1,000 random views, 10 seeds, R3 dry run)

| | Landing (`outlets_int`) | With P-1 | With P-1 + P-6 |
|---|---|---|---|
| Fixtures drawn | 8.5 (p95 15) | 8.5 | 8.5 |
| Draws per pass | 34.6 (p95 60, max 104) | 34.6 | **13.3** (p95 25, max 44) |
| Triangles | 25.1 k (p95 43.3 k, max 76.2 k) | **1.1 k** (p95 3.2 k, max 8.5 k) | 1.1 k |
| Culled beyond | 12 m (plates), 15 m (floor boxes) | LOD switches at 1.5 / 4 / 12 m (plates) and 2 / 6 / 15 m (floor boxes) | same |

- **Submit time per camera, real draws** (capture, 42 shots): median 0.11 ms, max 0.13 ms after the first shot (0.61 ms, while the kits load). That is *under load, re-measure* (load 400–600). The valid C1 figure is 0.070 ms.

### 7.3 Frame time (autopilot, `FrontRoomsMainScenePlaytest`, with graphics, 75 s, landing clone)

| Seed | Outlets | Verdict | fps | p95 / p99 ms | Max scene colliders | Load (1 min) |
|---|---|---|---|---|---|---|
| 20388 | on | PASS | 58.9 | 16.8 / 17.0 | 396 | 42 → 26 |
| 20388 | off | PASS | 59.1 | 16.8 / 17.1 | 396 | 26 → 31 |
| 2554 | on | PASS | 58.0 | 16.9 / 27.9 (worst 17.8 s, one stall at a load spike) | 407 (renderers 3,857) | 28 → 123: *under load, re-measure* |
| 2554 | off | PASS | 58.7 | 16.9 / 21.4 (worst 2.4 s) | 401 (renderers 3,886) | 123 → 49: *under load, re-measure* |

- **Colliders.** Seed 20388 has the same collider ceiling with and without outlets (396 / 396). On 2554 the two runs took different routes (the scene renderer peak goes the other way: 3,857 with outlets vs 3,886 without), so their peaks differ by 6. The same-build check in the probe shows 0 colliders and 0 renderers added (5 seeds).
- **Outlet draw cost in play:** 0.3 draws per frame on average (max 8–11) and 190–250 triangles per frame; R3 submit at most 0.23 ms in a frame.
- **Batch mode renders few cameras.** It renders about 0.02–0.09 cameras per frame, so these frame times bound the CPU and map cost, not the full GPU cost. A windowed Play-mode measure is still open (C1 open item 4).
- **The first 2554 pair failed on sound, not outlets:** `BankLoadException … Master.strings.bank`, because the clone had no desktop FMOD banks (`FMOD/FrontRooms/Build/Desktop`, 1.5 MB). Its numbers were on 59.0 fps, p99 19.7 ms; off 57.7 fps, p99 26.9 ms (load 51–74). The banks were copied in and the pair re-ran: the rows above, both PASS, 0 errors. The 2554 milliseconds are *under load*: the machine went from 28 to 123 during the run. The ≥ 55 fps gate holds on both seeds; p99 ≤ 33 ms holds on both seeds, but the 2554 worst frames are load stalls and need a quiet re-measure.

---

## 8. The map contract (request to 关卡设计)

`data/25_contract.0a9ee7f.diff.txt` (122 lines) is made on today's MapWorld (sha1 `11586bbc`). Its changed lines are **byte-identical** to the 10-04 and 10-07 requests; only line numbers moved. `python3 data/20_contract_patch.py.txt <MapWorld.cs>` still produces it.

The shape is the map chat's binding hook:
- **Collect:** `BuildEdge` gains `MapChunk faceData = null, List<FrontRoomsWallFixtures.WallFace> faces = null`, after Codex's `blockA/blockB`. It calls `AddWallFaces` for every built Wall face and every opening face, on both sides. Each face carries its `EdgeId(a, b)` + side, cell, room or −1, zone, theme, height, ceiling and openings. Faces round the start area are skipped.
- **Plan:** `BuildInto` clears `wallFaces` per chunk. It calls `FrontRoomsWallFixtures.Install(chunk.root.transform, wallFaces, Cache.Generator.Seed, data, startArea, chunk.meshes)` once, after `AddZoneGrades` and before `Furnish`.
- **Lifetime:** the set is a component on the chunk root, so `Unregister` frees it. R3 allocates no per-chunk meshes; `chunk.meshes` is passed for the rule.
- **Stage 2** runs from the Office kit, which is visual-owned. It needs no map call (C1 §5, unchanged).
- **Level Designer pin/forbid, later:** spec C-2 (`10_spec.md` §4.3, phase 2) proposes the fields. `ModuleOutletPin[] outletSouth/North/West/East` and `outletInnerEast/North` hold `{ModuleOutlet kind; float along, y}`, plus `outletDensity`, stamped into `MapChunk.outletFace` like the lamps. Not built.
- **Window landing:** it composed cleanly with the window-landing r6 contract on 10-07 (both orders identical). It was not re-checked today, because neither base moved.

---

## 9. Approvals and open items

1. **P-1, LODs in the FBX:** `bash W/apply/outlets_lods.sh`. It is stacked on `outlets_int`, so its dry run refuses until that lands. On a scratch copy with `outlets_int` applied it prints DRY RUN OK; bases are today's `kitlib.py`/`build_asset.py`. It writes 42 paths: kitlib, build_asset, and 20 kits' sidecar + FBX. **NEEDS APPROVAL** (visual chat; the same P-1 the interactables track asked for).
2. **P-4b + P-6, RenderSetup.** `data/21_rendersetup_p4b_p6.ee5c9bb.diff.txt` and `data/20_rendersetup_patch.py.txt` still apply to today's RenderSetup, which is unchanged since 10-08. It also needs the 9 `.mat` files with `m_EnableInstancingVariants: 1`. **NEEDS APPROVAL**, and the ivory tint waits on Red's L-1 pick (§6.6, VL235). Not packaged on purpose.
3. **Sidecar placement:** the outlet kits' sidecars say `"placement": "Floor"` (kitlib's default). The planner places them, so nothing breaks today. The Level Designer's kit list should skip tag `outlet`, or kitlib should map `wall_flush`. Needs visual-chat approval (O1/O2 issue).
4. **Screws at LOD1:** O1 shows a dark hole, O2 a screw-head stand-in. Pick one rule (O2 issue 5).
5. **Estimates still waiting for photos:** the 5-20R arm, the IBM data face and the 4-prong hole pattern. The media leads are in `media_candidates.md`; nothing was downloaded.
6. **Border-wall desks:** 9 of 118 wall-row desks have no receptacle within 2 m (100 seeds), unchanged since C1 §5.
7. **The Bare kit gap** reads dark in game (§6.2). The spec's real fix, a hole in the map wall (§8 item 7), stays a map-chat item.
8. **Windowed frame time** (§7.3).

---

## 10. Verification log

| VL | Node | Check | Verdict | Images (largest first) |
|---|---|---|---|---|
| VL230 | `3117:8102` | Outlets in every zone | PASS | `25_wide_L0_Low_2554_annot`, `25_wide_L0_Standard_2554_annot`, `25_wide_Tall_2554_annot`, `25_wide_Office_2554_annot`, `25_stream_StreamLobby_annot`, `25_stream_StreamOffice_annot` |
| VL231 | `3117:8137` | Every kit in game at 0.3 m | PASS | `25_close_zoom_sheet`, `25_close_game_sheet` |
| VL232 | `3117:8160` | Plates under dead lamps | PASS | `25_dead_lamp_ab`, `25_dead_lamp_natural` |
| VL233 | `3117:8183` | One chunk: placements, rules | PASS | `25_top_2554_overlay`, `25_top_2554_crop` |
| VL234 | `3117:8206` | LOD switch from 0.3 to 11 m | PASS | `25_lod_dolly_crops`, `25_lod_dolly_frames` |
| VL235 | `3117:8229` | Ivory plate vs the paper | WAIT-RED | `25_l1_plate_tint_option` |

- **Section:** FRONTROOMS · VISUAL VERIFICATION LOG (`2595:6093`). It grew to 59 rows (73,240) after a bounds check, and the cover VL000 was recounted: 234 checks, 675 images, 26 tasks; N3 = `VL064–235 · 18 checks`.
- **No new VL093 slide:** the landing T-R2 sheet (`25_landing_tr2_sheet.jpg`) answers VL093's question again with the same verdict.

---

## 11. Clones and work folders

| Path | Size (apparent; APFS clones share blocks) | Can it go? |
|---|---|---|
| `W/proj_outlet_int_t` (landing test clone, main `0a9ee7f` + `outlets_int`) | 4.0 G | After `outlets_int` lands |
| `W/proj_outlet_int` (full-state capture clone) | 4.5 G | After the P-1 / P-4b decisions (re-make with `W/outlet_int2/resync.sh <clone> full`) |
| `W/proj_outlet` (C1 + O1–O3 build clone) | 6.1 G | Yes, now: everything it holds is in `W/apply/outlets_int_payload`, `outlets_lods_payload` and `data/` |
| `W/outlet_int2/roslyn*` (Roslyn scratch, about 20 × 4.5 M) | ~90 M | Yes, now |
| `W/outlet_c1r3` | 72 K | Yes, now (empty) |

Nothing was deleted.

---

## 12. Files

In `Documentation/research/outlets/`:
- **Report:** `25_integration.md` (this file).
- **Images (`images/25_*.jpg`, JPG q85, ≤ 1920 px):**
  - sheets: `wide_zones_annot`, `close_zoom_sheet`, `close_game_sheet`, `dead_lamp_ab`, `dead_lamp_natural`, `top_2554_overlay`, `top_2554_crop`, `lod_dolly_crops`, `lod_dolly_frames`, `l1_plate_tint_option`, `landing_tr2_sheet`;
  - the six annotated zone frames;
  - 10 `close_*` and 10 `zoom_*` single kits.
- **Code and diffs (`data/25_*`):**
  - the three new `.cs` files (`.cs.txt`);
  - `RoomStream.0a9ee7f.diff.txt`, `OfficeKit.0a9ee7f.diff.txt`, `outlet_common.field_normals.diff.txt`, `c1_to_int.diff.txt`;
  - `contract.0a9ee7f.diff.txt` and `roomstream_patch.py.txt`;
  - the P-1 patch and diffs (`p1_kitlib_patch.py.txt`, `p1_kitlib.diff.txt`, `p1_build_asset.diff.txt`);
  - the capture harness, `mkpkg.py.txt`, `spec_int.json`, `spec_lods.json`.
- **Results (`data/25_*`):**
  - probes: `probe10_full_1008.json`, `probe10_landing_1008.json`, `probe10_1014.json`, `probe100_1014.json`;
  - captures and measurements: `capture_shots.json`, `views_{2554,20388}.json`, `top_2554_plan.json`, `capture_tr2_landing_1008.json`, `dead_ab_luma.json`, `l1_measure.json`, `prepare_full_1008.txt`;
  - Unity checks and play runs: `exit_check_1014.txt`, the autopilot reports and playstats;
  - `timeline_with_load.txt`: every Unity run with its start and end load.
- **Package:** `W/apply/outlets_int*` (READY) and `W/apply/outlets_lods*` (P-1, stacked, NEEDS APPROVAL).
- **Verification log:** `Documentation/VERIFICATION_LOG.md`, rows VL230–235.
