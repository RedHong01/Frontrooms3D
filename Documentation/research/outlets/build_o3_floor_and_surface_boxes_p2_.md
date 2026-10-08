# Build O3: floor and surface boxes (P2)

Status: **DONE, 2026-10-07 20:00.** This is the continuation run after the 17:54 usage-limit stop on 2026-10-03, the reboot on 2026-10-05 and the second limit at about 17:46 on 2026-10-07. Build contract: `10_spec.md` §1.1–§1.3, §2.10, §5.0, §5.3, and `01_period_research.md` §6.

- Six assets are built, exported and checked: `Kit_FloorBoxTombstone`, `Kit_FloorBoxTombstone2`, `Kit_OutletHandyBox`, `Kit_ConduitEMT`, `Kit_ConduitStrap` and `Kit_CubiclePanel_Powered`.
- All envelopes are within ±1 mm. The floor boxes are exact to 0.01 mm.
- Every LOD0 lands within ±15 % of its budget. The hand-built LOD1/LOD2 sets also land within ±15 %.
- Iteration 6 (today) closed three gaps in the handy box that Unity's back-face culling would open. Before the fix, the dense ray probe found 17 culled-face hits. After it, it finds 0 on LOD0, LOD1 and LOD2 (53 M rays per LOD).
- Unity was not opened. Nothing under `Frontrooms3D/Assets` was touched. `kitlib.py`, `build_asset.py` and `cubicle_panel.py` are unchanged.
- Verification slides: VL091 and VL092 (earlier build) and **VL134** (today's fix, frame `2791:6103`).

---

## 0. The assets

Sizes are measured on the exported FBX in the clone. W × H × D in mm. Triangles: LOD0 is what ships today. LOD1 and LOD2 are hand-built part sets, built only by the review stub until `kitlib.Kit.make_lods` exists (§4).

| Asset | Module | Measured size (mm) | LOD0 | LOD1 | LOD2 | Budget | LOD dist (m) | Slots |
|---|---|---|---|---|---|---|---|---|
| `Kit_FloorBoxTombstone` | `outlet_floorbox.py` | 111.00 × 67.00 × 76.00, flange 2.00 | 3,576 (+11.8 %) | 1,118 (−6.8 %) | 84 (+5.0 %) | 3,200 / 1,200 / 80 | 2 / 6 / 15 | AL, NI, PB, BR |
| `Kit_FloorBoxTombstone2` | `outlet_floorbox2.py` | 127.00 × 76.00 × 86.00, flange 2.00 | 5,112 (+11.1 %) | 1,836 (+8.0 %) | 92 (−8.0 %) | 4,600 / 1,700 / 100 | 2 / 6 / 15 | AL, NI, PB, BR |
| `Kit_OutletHandyBox` | `outlet_handy_box.py` | box 54.00 × 101.60 × 47.60; cover 55.0 × 102.6; 52.3 proud; z −51.3 … +96.6 with the connector | 3,860 (+13.5 %) | 1,300 (0 %) | 80 (0 %) | 3,400 / 1,300 / 80 | 2 / 6 / 20 | AL, NI, PB, BR |
| `Kit_ConduitEMT` | `outlet_conduit.py` | Ø 17.90 × 1,000.0; axis 11.5 off the wall | 576 (−4.0 %) | 220 (0 %) | 24 (0 %) | 600 / 220 / 24 | 3 / 8 / 24 | AL |
| `Kit_ConduitStrap` | `outlet_conduit_strap.py` | 30.0 × 22.0 × 22.2 proud | 294 (−2.0 %) | 106 (+6.0 %) | – | 300 / 100 / – | 2 / 6 / – | AL |
| `Kit_CubiclePanel_Powered` | `outlet_panel_powered.py` | 1,524 × 1,520 × 67 (= `Kit_CubiclePanel`) | 1,460 = panel 456 + receptacles 1,004 (+11.6 % on 900) | 184 (= the panel's LOD1) | – | panel + 900 / panel LOD1 | panel's `LOD1 = 0.45` | SteelPutty, PB, FabricCubicle, NI |

Slots: AL `Prop_Aluminium`, NI `Prop_NylonIvory` (new, registered with `register_slot` as #E2D9BF, roughness 0.45; **NEEDS APPROVAL P-4b**, not in Unity yet, see §6), PB `Prop_PlasticBlack`, BR `Prop_Brass`.

**Sidecars** (checked on the exported JSON):
- `noCollider: true` and `colliders: []` on all five outlet kits. The powered panel keeps `Kit_CubiclePanel`'s collider exactly (it is furniture).
- Tags: floor boxes `outlet`, `outlet_floor` (no `wall_flush`, as §1.1 says). Handy box `outlet`, `outlet_surface`, `wall_flush`. EMT and strap add `conduit`. Panel: `office`, `panel`, `outlet`, `outlet_surface`, `powered_panel`.
- Anchors (Unity space): floor boxes `cord_in`, `face_a`, `face_b` (FloorBox2 also `cord_in_back`, `face_a_back`, `face_b_back`); handy box `conduit_base` (0, 0.0926, 0), `face_top`, `face_bottom`, `plate_top`; EMT `bottom`, `top`; strap `screw`; panel `top_centre`, `receptacle_front` (−0.30, 0.07, 0.0315), `receptacle_back` (0.30, 0.07, −0.0315).
- `lodDistances`, `lodRatios`, `lodBudget`, `lodHandBuilt: true`, and an `outlet {}` dict for R3 (`plateW`, `plateH`, `proud`, `screws: 0`, `screwsBaked: true`, `family`; floor boxes add `footprint`, `duplexCentreHeight`; the handy box adds `box`, `conduitBase`, `conduitAxis`; EMT adds `tileLength`, `od`, `scaleAxis: "Y"`, `caps: false`; the strap adds `pitch: 1.5`).
- `fr_wear` (BYTE_COLOR, face corner) survives the FBX round trip on every part of every kit.
- Nothing lies behind the wall plane on the wall kits (max y = 0.0000 mm). Nothing lies under the floor on the floor boxes (min z = 0.0000 mm).

## 1. Files

**Modules (in the real project, as the task allows):** `Tools/Blender/frontrooms_kit/assets/`
- `outlet_box_common.py` (helper: no `NAME`, no `build`; O3's own copy of the §1.2 5-15R numbers, asserted at import), `outlet_floorbox.py`, `outlet_floorbox2.py`, `outlet_handy_box.py`, `outlet_conduit.py`, `outlet_conduit_strap.py`, `outlet_panel_powered.py`.
- No module imports the O1 or O2 helpers. `outlet_panel_powered.py` imports `cubicle_panel.build_panel` and `_workstation_lod.lod1_drop` (read-only use).
- Changed today (iteration 6): `outlet_box_common.py` (`sweep()` gains optional `start_tangent` / `end_tangent`; the defaults keep the old result, so the other kits rebuild identically) and `outlet_handy_box.py`. The diff is `data/o3_it6_modules.diff.txt`. Both files are uncommitted in main (`git status`: `M`).
- Side effect to know about: the final build ran from the main kit folder, so Python rewrote two **tracked** bytecode files, `assets/__pycache__/outlet_box_common.cpython-311.pyc` and `outlet_handy_box.cpython-311.pyc`. They match the new sources and are harmless. Suggestion for the visual chat: untrack `__pycache__/` in a later commit.

**Outputs (private clone only):** `/Users/redwang/FrontRoomsVisualWork/proj_outlet/Assets/Resources/Props/Models/<NAME>.fbx` and `.json`, built 2026-10-07 17:43 from the main kit with the task's command (`--no-preview`). The build previews (`_a`, `_b`) are in `/Users/redwang/FrontRoomsVisualWork/outlet_prev/g2/`.

**Work tools** (`/Users/redwang/FrontRoomsVisualWork/o3/tools/`, copies in `data/` as `.py.txt`):
- `o3_rayprobe2.py`: back-face and see-through-leak probe on the exported FBX, judged the way Unity draws it (back faces culled).
- `o3_lods.py`: builds the hand-built LOD1/LOD2 sets with a scratch-only `make_lods` stub and exports `<NAME>_LOD0/1/2.fbx` for review.
- `closeups.py`: Cycles renders of the exported FBX (normal, back-face-red and wear modes).
- `wb_cull.py`: the before/after back-face renders.
- `emt_normals.py`: EMT normal check.
- `o3_final_verify.py` (new today): the read-only checks in §2 on the shipped FBX and JSON. Probe results: `data/o3_probe_summary.txt`, `data/o3_probe_final_handybox.json`; verify output: `data/o3_final_verify_result.txt`.

## 2. Self-checks (§5.3)

| # | Check | Result |
|---|---|---|
| a | Envelopes within ±1 mm | **PASS.** FloorBox 111.00 × 67.00 × 76.00 and FloorBox2 127.00 × 76.00 × 86.00 (0.00 mm off). Handy box: the box is exactly 54.00 × 101.60 × 47.60. The cover is 55.0 × 102.6 (+1.0 / +0.6 on the spec's 54 × 102), because it laps the box by 0.5 mm a side. Proud 52.3 = box 47.6 + raised field 2.8 + device face 1.0 + screw heads 0.9. Strap 30.0 × 22.0 × 22.2 (+0.2 proud). EMT OD 17.90 |
| b | The conduit tile meets `conduit_base` with no gap | **PASS.** A tile placed at `conduit_base` starts at z 92.6 mm. The socket top is at 96.6, so the tube runs 4.0 mm inside the socket. The socket lip's inner radius is 8.85 and the tube's is 8.95, so the lip tucks 0.10 mm under the tube skin (0.081 mm at a chord middle). Probe: 0 leaks and 0 back faces in the connector-joint region on LOD0, LOD1 and LOD2 |
| c | A 0.4 Y-scaled tile still reads as a tube | **PASS.** The normals are radial: max \|n · axis\| = 7.5 × 10⁻⁸, so a Y scale cannot tilt them. Probe "EMT tile scaled 0.4": 0 back faces, 0 leaks on LOD0, LOD1 and LOD2. Render: `images/o3_sheet_closeups_030.jpg` (EMT Y 0.4 next to 1.0) |
| d | The powered panel keeps `Kit_CubiclePanel`'s bounds and collider exactly | **PASS.** `boundsMin` / `boundsMax` are identical in the JSON (±762.0, 0 … 1,520.0, ±33.5 mm). The collider is identical (centre 0, 0.76, 0; size 1.524 × 1.52 × 0.064). LOD1 is identical (184 tris). The receptacle stands 1.50 mm proud of the rail (spec ≤ 2) and 2.0 mm inside the cap's bounds |
| e | Renders at 0.30 m, 1.5 m and 4 m | **PASS.** §3 |
| f | No holes when Unity culls back faces | **PASS after iteration 6.** §2.1 |
| g | Era: no text, logos, dates or UL marks | **PASS.** No text geometry, no decal UVs, no label slot in any module. Plain cast and galvanized finishes. Poke-through fittings (1970s on), handy boxes and EMT (unchanged since the 1950s) and pre-1991 powered panels all fit 1990 (`01` §6) |
| h | No colliders, tags, `fr_wear` | **PASS.** See §0 |
| i | Triangles within ±15 % | **PASS.** LOD0 is asserted in every build; LOD1/LOD2 are asserted whenever the hand-built parts exist (the review stub builds them) |

### 2.1 Ray probe: back faces and leaks

The probe (`o3_rayprobe2.py`, v2 of 2026-10-07) shoots rays from the front hemisphere of the wall or floor (polar 0–82°) and judges each hit the way Unity draws it.
- **Back face:** the first hit is a back-facing triangle, which Unity culls, so the pixel shows what lies behind it.
- **Leak:** the first hit is the wall or floor at a point inside the kit's solid.
- A hit counts only when 4 of 5 rays agree: the ray itself plus four rays shifted 0.03 mm along the diagonals. A lone hit on a shared edge is a tie, not a hole. Hits within 0.6° of edge-on are counted apart as "grazing".
- Dense setting: 41 directions, 0.25 mm steps over the whole kit, 0.1 mm over the device and the joint.

| Run | Kit, region | Rays | Back faces | Leaks | Verdict |
|---|---|---|---|---|---|
| BEFORE (iteration 5 handy box, dense) | whole / device / joint | 23.5 M / 15.7 M / 14.1 M | **1 / 8 / 8** | 0 / 0 / 0 | FAIL |
| FINAL LOD0, dense (shipped FBX) | HandyBox + EMT whole / device / joint | 23.5 M / 15.7 M / 14.1 M | 0 / 0 / 0 | 0 / 0 / 0 | **PASS** |
| LOD1, dense | HandyBox + EMT whole / device / joint | 23.5 M / 15.7 M / 14.1 M | 0 / 0 / 0 | 0 / 0 / 0 | PASS |
| LOD2, dense | HandyBox + EMT whole / device / joint | 23.5 M / 15.7 M / 14.1 M | 0 / 0 / 0 | 0 / 0 / 0 | PASS |
| FINAL LOD0, dense | FloorBox whole / device | 8.9 M / 15.4 M | 0 / 0 | 0 / 0 | PASS |
| FINAL LOD0, dense | FloorBox2 whole / front / back | 11.3 M / 15.4 M / 15.4 M | 0 / 0 / 0 | 0 / 0 / 0 | PASS |
| FINAL LOD0, dense | Strap on EMT; EMT scaled 0.4 | 7.1 M; 22.4 M | 0; 0 | 0; 0 | PASS |
| LOD0, moderate (17 directions, 0.15 mm), 4-of-5 rule (`r10_P`) | Panel receptacle front / back | 2.7 M / 2.7 M | 0 / 0 | – | PASS |
| FINAL LOD0, dense | Panel receptacle front / back | not run: stopped at 20:00 to free the CPU (load average about 800). The panel module has not changed since the moderate run above | – | – | – |
| LOD1 and LOD2, moderate (17 directions, 0.4 / 0.15 mm) | both floor boxes, strap, EMT 0.4 | 1.3–3.9 M per region | 0 | 0 | PASS |

The BEFORE hits all land on one spot: the inside of the box top (z 50.8 mm), reached through a 0.7 mm slot under the cover's punched openings (§2.2, fix 3).

One note on the panel. A moderate run before the 4-of-5 rule found 1 back-face hit at the receptacle's bottom edge (x 316.5, z 48.0 mm). Under the stricter rule it is an edge tie (`r10_P`: 0 hits). The panel module did not change.

### 2.2 Iteration 6 fixes (2026-10-07, handy box only)

1. **Crescent gap under the socket.** The S-neck's last ring took the tilt of the last chord (13.7°). It stopped up to 1.25 mm short of the socket floor on the wall side. With back faces culled, a crescent opened onto the wall. Fix: the neck's last ring is level (end tangent vertical), so it ends 1.0 mm inside the socket all round. The neck now starts 2 mm down in the collar, so ring 1 does not fold. Evidence: `images/o3_hb_crescent_cull_pair.jpg`, `o3_hb_neck_after_c030.jpg`, `o3_hb_joint_after_c010.jpg`.
2. **Open cover lip.** The cover laps the box by 0.5 mm, and that lap was open from below. No viewer ray reaches it (it faces the wall), but it left an open shell that the leak test could not judge. Fix: an annulus in the box-front plane closes it. The box and cover are now one closed shell against the wall. Cover +64 tris (954 → 1,018).
3. **Slot under the openings.** The walls of the punched openings stopped at field − 0.8 mm. That left a 0.7 mm slot under them into the box, which steep views could see through. The 17 BEFORE hits sit right behind it, on the inside of the box top. Fix: the walls now run down to field − 1.55, past the device's back plane, so the ivory backing closes the 0.4 mm gap round each face.
4. The LOD1 neck and the LOD2 socket sweep got the same level end rings, and the LOD2 sweep got an end cap. Before: LOD1 leaked (103 confirmed hits over the whole kit, 745 in the joint region) and LOD2 had 6 back faces. After: 0.

LOD0 3,796 → 3,860 tris (+13.5 % on 3,400, inside ±15 %). Nothing else changed: the other five kits rebuild to identical JSON and the same triangle counts.

## 3. Renders (reviewed)

All renders are Cycles renders of the **exported FBX** (so export, normals and the colour attribute are what is checked), AgX. "Eye" shots: player eye 1.62 m, FOV 76.

| Image | What it shows |
|---|---|
| `images/o3_sheet_closeups_030.jpg` | All six kits at 0.30 m: front, 45° rake and 3/4. The 5-15R faces with 7 mm black slots and brass leaves, the oval-head screw in its 82° countersink, the floor-box flange, the raised cover with its pan-head screws, the knockout slug, the connector and set screw, the strap on EMT, EMT at Y 0.4 and 1.0, the panel receptacle (build of 2026-10-04) |
| `images/o3_floorbox_c030_detail34.jpg`, `o3_floorbox2_c030_detail34.jpg`, `o3_handybox_c030_side.jpg`, `o3_handybox_emt_joint.jpg`, `o3_strap_c030_detail34.jpg`, `o3_panel_powered_c030_detail34.jpg` | The 0.30 m details (VL091) |
| `images/o3_sheet_distance_1080p.jpg` | 1.5 m and 4 m, 1080p, centre crops ×2. At 4 m a floor box reads as a cast hump with two ivory dots (VL092) |
| `images/o3_sheet_lods.jpg` | LOD0 / LOD1 / LOD2 side by side (VL092) |
| `images/o3_check_backfaces_wear.jpg` | Back faces in red on five kits (none; 2026-10-04 build) and the `fr_wear` attribute on the floor box and the handy box |
| `images/o3_eye150_frames.jpg` | Uncropped eye-height frames at 1.5 m: floor box, handy box + EMT, powered panel |
| `images/o3_hb_crescent_cull_pair.jpg` | Iteration 6: the crescent under the socket, before and after, back faces red (VL134) |
| `images/o3_hb_neck_after_c030.jpg`, `o3_hb_joint_after_c010.jpg` | Iteration 6: the neck at 0.3 m and the EMT joint at 0.12 m, after (VL134) |
| `images/o3_hb_final_it6_sheet.jpg` | The shipped handy box: 0.30 m front, rake and 3/4 detail; 1.5 m and 4 m at eye height (720p, crop ×2); back-face test from below at 0.30 m: no red (VL134) |

What the review found, in short:
- **0.3 m.** Slot depth reads as black 7 mm slots, not printed marks. The U ground hole and the screw slots are crisp. The cover's rim and pressed ramp catch the light like real pressed steel. The cast tombstone's R 16 / R 18 shoulders and R 5 edges read as a casting, not a box.
- **1.5 m.** Floor boxes read as cast aluminium humps with an ivory duplex. The handy box reads as a galvanized box with conduit.
- **4 m.** A floor box is a small hump with two ivory dots; the handy box is a grey box on a thin line. This is where LOD2 takes over (6 m).

## 4. LODs

The spec's convention (§1.1, P-1/P-1b): each part carries `obj["fr_lods"]` ("0", "1", "2" or a mix). The LOD1/LOD2 parts are built **only when `hasattr(kitlib.Kit, "make_lods")`**. `kitlib.py` has no `make_lods` today, so every shipped FBX holds LOD0 only; R3 culls at 12 m by itself. The LOD counts in §0 come from `o3_lods.py`, which stubs `make_lods` in the scratch process only. The interactables spec (`../interactables/10_spec.md` P-1) owns the kitlib change, and this group uses the same convention. Nothing here needs a new approval beyond P-1/P-1b.

The powered panel is the exception. It is ≥ 1 m, so it uses kitlib's existing `LOD1 = 0.45` (the panel's own), with every receptacle part dropped at LOD1 (`kit.lod1_drop` plus the shim's face flag). Its LOD1 has the panel's own 184 tris, and the sidecar's bounds and collider are identical.

Importer note. Today's `FrontRoomsKitImporter` (663e858) maps `lodDistances` onto a LODGroup only when the FBX has `_LOD0/_LOD1` children. The five outlet kits have one mesh, so no LODGroup is made and the importer's cull does not apply. R3 (the planner's instanced draw) handles their 12 m cull. The strap's `lodDistances` ends in −1 (no LOD2), which is safe either way.

## 5. ESTIMATES that became numbers

Shared (`outlet_box_common.py`):
- `E_AXIS` 11.5 mm: the EMT axis off the wall. The tube's back is 2.55 mm off the wall; the strap is 22.2 proud; the connector socket just clears the wall. Conduit, strap and connector all use it.
- `GROUND_SIDE` 2.0 and `GROUND_CORNER_R` 0.5 (U ground hole), `SLOT_CORNER` 0.25 (blade-slot corner chamfer), `CONTACT_H` 2.5 (brass leaf height), `PC_HERO` 64 (from the spec).
- `EMT_OD` 17.9 (= 0.706 in, the trade number, not an estimate).

Floor boxes:
- `Kit_FloorBoxTombstone`: housing inset 4.0, top-corner radius 16.0, edge round 5.0, duplex centre height 31.0.
- `Kit_FloorBoxTombstone2`: inset 4.0, top-corner radius 18.0, edge round 5.0, duplex centre height 34.0.

Handy box:
- Box corner radius 6.0. Cover 55.0 × 102.6, corner R 6.5 (real covers run about 58.7 × 104.8; trimmed to keep the envelope).
- Cover profile (inset, height): (0, 0), (0, 0.45), (0.35, 0.8), (2.2, 0.8), (3.6, 2.0), (5.0, 2.8). The field stands 2.8 above the box front.
- Neck radius 9.5, neck length 24.0. Socket radius 11.3, length 18.0. Hex collar 25.4 across flats, 4.8 high. Knockout slug 0.3 proud. Tube 4.0 inside the socket.

Strap:
- Sheet 1.6 thick, foot bend radius 1.6. The 30 × 22 × 22 envelope is the spec's own estimate; real 1/2 in one-hole straps run about 45–50 mm long and 16–19 mm wide, so this one is compact.

Powered panel:
- Receptacle centre 0.30 m along the panel. Bezel 78 × 44 (R 3), 1.0 proud. Insert 72 × 38 (R 2), 0.5 below the bezel face. Height 0.07 m (the rail's centre; the spec says 0.05–0.07).

## 6. Deviations and open items

1. **Segment counts under the spec's 64** (§1.1 asks ≥ 64 for screw heads and curved outlines), all forced by the budgets. Chord errors at 0.3 m (1080p, FOV 76, 2.30 px/mm):
   - FloorBox2 faces, openings and countersinks at 48: 0.035 mm (0.08 px). Its oval-head screw at 40: 0.010 mm (0.02 px).
   - Handy box cover screws at 24: 0.028 mm (0.07 px). Set screw at 16: 0.054 mm (0.12 px). Knockout slugs at 20: 0.137 mm (0.31 px).
   - Strap screw at 16: 0.067 mm (0.16 px).
   - All are under a pixel at 0.3 m. With 64 everywhere, FloorBox2 measured about 6,400 tris (+39 %).
2. **The panel receptacle uses the helper's "mid" face**: 32 segments, 3 mm slots, no brass contacts (to stay at 4 slots and near the +900 budget). It sits 0.07 m off the floor and is normally seen from ≥ 1 m. Chord error at 0.3 m: 0.078 mm (0.18 px). If Red wants the hero face here too, it costs about +1,000 tris and a fifth slot (`Prop_Brass`).
3. **`Prop_NylonIvory` is a new slot (P-4b, NEEDS APPROVAL).** It is not in Unity yet: no material, no RenderSetup entry. Until it lands, the device faces import with a missing material. The spec's fallback is `Prop_PlasticPutty`.
4. **The handy box stands 52.3 mm proud.** The planner must keep furniture off its face (spec §2.9 assumes plates ≤ 7.5 mm).
5. **`placement: "Floor"` in the wall kits' sidecars** is kitlib's default, not a placement rule. The outlet planner places these kits itself, and the `outlet` tags keep the Level Designer from using them as floor props.
6. **Integration is not done here.** The FBX/JSON live only in the clone. Copying them into `Assets/Resources/Props/Models/` is the integrate stage's job (with their `.meta` GUIDs).
7. **Tracked `.pyc` files** in main were rewritten by the build (§1).

## 7. Verification log

| VL | Frame | Check | Verdict |
|---|---|---|---|
| VL091 | `2743:6093` | O3 boxes at 0.3 m (2026-10-04 build) | PASS |
| VL092 | `2743:6275` | O3 at 1.5 m and 4 m, LODs (2026-10-04 build) | PASS |
| VL134 | `2791:6103` | Handy box gaps closed (iteration 6) | PASS |

The cover VL000 was updated in the same pass (19 tasks, 130 checks, 412 images at 18:5x). Later stages have moved it on since; at 19:1x it read 134 checks and N3 "VL064, 088–094, 134–135 · 10 checks".
