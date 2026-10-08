# Build O2 — switches, blanks and jacks

Status: **DONE, re-verified 2026-10-07 18:15–19:04 (run r7)**. Build contract: `10_spec.md` §1.1–§1.5, §5.0, §5.2.

- Seven assets built, exported and checked: `Kit_OutletBlank`, `Kit_OutletToggle1`, `Kit_OutletToggle2`, `Kit_JackPhone` (P1); `Kit_JackData`, `Kit_JackBNC`, `Kit_JackPhone4Prong` (P2).
- Every LOD0, LOD1 and LOD2 lands within ±15 % of its budget.
- The O2 plate matches O1's plate to **0.0084 mm** (3,018–3,308 rays per kit, 0 outline mismatches), against a fresh build of O1's `Kit_OutletDuplex` (18:15).
- **New on 2026-10-07: the plate field shading is fixed** (§10, VL135). Field normals were up to 5.2° off the crown and showed as diagonal lines in Unity. They are now 0.04° off, measured on the exported FBX.
- Unity was not run. Nothing under `Frontrooms3D/Assets` was touched. `kitlib.py` and `build_asset.py` are unchanged (last change 2026-10-02, 08031cb).

---

## 0. The assets

Sizes are W × H × proud in mm, measured on the exported FBX. Triangles are LOD0 (shipped) / LOD1 / LOD2 (hand-built, not exported yet: §4). Numbers are from run r7 (2026-10-07).

| Asset | Module | Size (mm) | LOD0 | LOD1 | LOD2 | Budget | Slots |
|---|---|---|---|---|---|---|---|
| `Kit_OutletBlank` | `outlet_blank.py` | 69.8 × 114.3 × 5.60 | 1,412 (+8.6 %) | 426 (−5.3 %) | 28 (−6.7 %) | 1,300 / 450 / 30 | TI, PB |
| `Kit_OutletToggle1` | `outlet_toggle1.py` | 69.8 × 114.3 × 18.41 | 2,296 (+9.3 %) | 780 (−2.5 %) | 40 (0 %) | 2,100 / 800 / 40 | TI, PB, NI |
| `Kit_OutletToggle2` | `outlet_toggle2.py` | 115.9 × 114.3 × 18.39 | 3,660 (+4.6 %) | 1,462 (+12.5 %) | 56 (+12 %) | 3,500 / 1,300 / 50 | TI, PB, NI |
| `Kit_JackPhone` | `outlet_jack_phone.py` | 69.8 × 114.3 × 5.59 | 2,118 (−3.7 %) | 686 (−14.2 %) | 36 (−10 %) | 2,200 / 800 / 40 | TI, PB, NI, BR |
| `Kit_JackData` | `outlet_jack_data.py` | 69.8 × 114.3 × 24.47 | 2,762 (−1.4 %) | 1,000 (0 %) | 68 (+13.3 %) | 2,800 / 1,000 / 60 | TI, PB, NI, BR |
| `Kit_JackBNC` | `outlet_jack_bnc.py` | 69.8 × 114.3 × 18.00 | 2,748 (+14.5 %) | 1,008 (+12 %) | 56 (+12 %) | 2,400 / 900 / 50 | TI, PB, CH, BR |
| `Kit_JackPhone4Prong` | `outlet_jack_phone4.py` | 52.0 × 52.0 × 18.00 | 1,672 (−7.1 %) | 628 (−10.3 %) | 38 (−5 %) | 1,800 / 700 / 40 | CE, PB, BR |

Slots: TI `Prop_ThermosetIvory`, NI `Prop_NylonIvory` (both new, registered with `register_slot` as #E6DDC2 / #E2D9BF; NEEDS APPROVAL P-4b), PB `Prop_PlasticBlack`, BR `Prop_Brass`, CH `Prop_Chrome`, CE `Prop_Ceramic` (brown).

Triangles per slot (LOD0, exported FBX):

| Asset | TI | NI | PB | BR | CH | CE |
|---|---|---|---|---|---|---|
| Blank | 1,384 | – | 28 | – | – | – |
| Toggle1 | 1,660 | 478 | 158 | – | – | – |
| Toggle2 | 2,388 | 956 | 316 | – | – | – |
| JackPhone | 1,664 | 204 | 122 | 128 | – | – |
| JackData | 1,476 | 174 | 992 | 120 | – | – |
| JackBNC | 1,480 | – | 116 | 128 | 1,024 | – |
| 4-prong | – | – | 144 | 364 | – | 1,164 |

**Every sidecar** (checked on the r7 JSON):
- `noCollider: true`, `colliders: []`.
- Tags `outlet` + family + `wall_flush`. Families: `outlet_switch` (toggles), `outlet_jack` (four jacks), `outlet_receptacle` (blank: it replaces a receptacle in the plan, §1.4).
- Anchors (Unity space, mm): `screw_0..k` on the screw seats and `plate_top` (0, 57.15, 0).
  - Blank and jacks: x 0, y ±41.65, z 5.49.
  - Toggle1: x 0, y ±30.15, z 5.52.
  - Toggle2: x ±23.0, y ±30.15, z 5.51 (`screw_0..3`).
  - The 4-prong block has `plate_top` (0, 26.0, 0) and no screw anchors: its cover screw is in the mesh, and `outlet.screws = 0` tells R3 not to draw `Kit_OutletScrew`.
- `lodDistances [1.5, 4.0, 12.0]`, `lodRatios`, `lodBudget`, `lodHandBuilt: true`, and `outlet {plateW, plateH, proud, screws, family, …}` for R3 (toggles add `batAngle 32` and `batClearanceMm {opening 1.457, slot 0.55}`; the 6P jack adds `cavityDepthMm 5.25`).
- Nothing lies behind the wall plane: min h = 0.0000 mm on all seven.
- `fr_wear` (BYTE_COLOR, face corner) is on every corner of every part; custom normals survive the FBX round trip on all seven.

## 1. Files

**Modules (new files, in the real project, as the task allows):** `Tools/Blender/frontrooms_kit/assets/`
- `outlet_plate_common.py` (helper: no `NAME`, no `build`), `outlet_blank.py`, `outlet_toggle1.py`, `outlet_toggle2.py`, `outlet_jack_phone.py`, `outlet_jack_data.py`, `outlet_jack_bnc.py`, `outlet_jack_phone4.py`.
- No module imports O1's or O3's helper. `outlet_toggle2.py` imports `outlet_toggle1.toggle_plate` (same group).
- All eight are committed in main. The 2026-10-07 field-normal change to `outlet_plate_common.py` (+66 lines) was committed by Red's "1" commit **a5262fb** (17:25) before this run verified it; r7 is that verification. The working tree has no O2 changes now (`git status` clean for these files and their `.pyc`).

**Outputs (private clone only):** `W/proj_outlet/Assets/Resources/Props/Models/<NAME>.fbx` and `.json` (W = `/Users/redwang/FrontRoomsVisualWork`). The r7 files were built into `W/o2/stage` and copied into the clone at 18:51 with an atomic rename (a Unity batch of the code task was running on the clone; the `.meta` files and their GUIDs from 17:13 are kept). SHA-1 of the FBX, first 12: Blank d9c5560f07ed, Toggle1 491ef7b8f0fc, Toggle2 b83e9f8a0bf0, JackPhone 3362f43e8bde, JackData 74cffbed08dc, JackBNC 4099bea064fa, 4-prong 1a2b839aa5da.

**Tools** (`W/o2/`; copies as `.txt` in `research/outlets/data/o2_tool_*.txt`, so a wipe cannot lose them again):
- `old_sp/o2/o2_review.py` (review renders; forces the LOD1/LOD2 parts on), `o2_verify.py` (checks on the exported FBX), `sheet.py`, `lod_probe.py`, `crack_probe.py`, `backface_probe.py`, `cull_render.py`. These were rebuilt on 2026-10-07 from the 2026-10-03/04 transcripts (the 2026-10-05 reboot wiped the old scratchpad).
- `tools2/field_dev.py` (field normal vs the analytic crown normal, on the FBX), `field_normals.py`, `normal_dev_render.py`, `gloss_check.py`.
- `final7.sh`: the whole r7 run (O1 reference build, screw, O2 build + previews, reviews, verify, sheets, probes).
- Results: `W/o2/verify7.json` (copy `research/outlets/data/o2_verify_r7.json`), logs `W/o2/f7_*.log`.

## 2. Self-checks (§5.2)

| # | Check | Result |
|---|---|---|
| a | Plate identical to O1's numbers | **PASS.** 21 shared constants equal O1's (`outlet_common.py`) within 0.05 mm, and 9 equal O3's. On the geometry: front rays at 0.05–0.1 mm steps along z = 0 (past each opening), x = ±25 and the four corner diagonals, against O1's exported `Kit_OutletDuplex` (fresh build 18:15). Max height difference **0.0072 mm** (Blank, BNC), 0.0070 (Toggle1, JackPhone), 0.0084 (Data); 3,018–3,308 rays per kit; **0 outline mismatches**. Each module also asserts the outline (69.8 × 114.3 or 115.9 × 114.3) and every profile station to 0.05 mm on its own mesh |
| b | The bat clears the opening at +32° and −32° (roll 180°) by ≥ 0.5 mm | **PASS.** Sliced exactly on the mesh, both rolls: **1.457 mm** to the plate opening (between the switch face and the field), **0.550 mm** to the bat slot (between the slot floor and the face). Asserted in the build |
| c | The 6P cavity takes a 9.85 mm plug block | **PASS.** Cavity 9.90 × 6.80: 0.025 mm a side. BVH test on the exported FBX: a 9.85 × 6.6 × 5.0 block 1 mm into the cavity touches **0** faces; a 9.95 control block touches 12 |
| d | The IBM connector is 32 × 32 and stands ≤ 25 mm proud | **PASS with a note.** Housing body 32.0 × 32.0; proud **24.47** (contact block front). The grip ribs on the two latch sides stand 0.6 out, so the envelope is 33.2 × 32.0; the mounting flange on the plate is 35 × 35 × 1.2 (ESTIMATE, a bezel) |
| e | No text, logos or "CAT" marks | **PASS.** No text geometry or decals exist in any module (no decal UVs, no label slot). No RJ45: the only modular jack is a 6P with a 9.9 mm cavity (RJ45 needs 11.7) |
| — | Proud limits | Toggles 18.41 / 18.39 ≤ 20; Data 24.47 ≤ 25; BNC 18.00, 4-prong 18.00 ≤ 25 (data/jack limit); Blank 5.60 and Phone 5.59 ≤ 7.5 |
| — | Triangles within ±15 % | LOD0 asserted in every build. LOD1/LOD2 asserted whenever the hand-built parts exist (the review build forces them) |
| — | Culled faces and leaks (VL090) | **PASS, r7.** 0 visible back-facing triangles on all 21 LOD sets (`lod_probe`) and on the 7 exported FBX (`backface_probe`); 0 see-through rays from 9,604–76,725 points × 25 directions per LOD set (`crack_probe`) |
| — | Plate field normals (VL135) | **PASS, r7.** Every field corner within **0.04°** of the analytic crown normal on the exported FBX (6 plates, 642–1,266 corners each). Before the fix: max 5.17°, 160–206 corners over 1° per plate |

## 3. Renders (reviewed)

All renders read with the Read tool on 2026-10-07 (r7). Cycles, 64 spp, AgX; O1's `Kit_OutletScrew` (fresh build) at the screw anchors, slot angle random per plate.

**Review set (per kit, `W/outlet_prev/g1/review7/<NAME>_<shot>.png`, 62 files):**
- `front030`: 0.30 m, FOV 62 V, at true 1080p pixel size.
- `rake030`: 0.30 m, a 45° raking key.
- `obl030`: 0.30 m, 35° off axis.
- `tilt030`: the edge from below, to show the plate profile and the hollow back.
- `d150_lod0` / `d150_lod1`: 1.5 m, FOV 76, the LOD switch.
- `d400_lod1` / `d400_lod2`: 4 m, the next switch.

**Sheets:** `W/o2/sheet_<NAME>_r7.jpg` (one per kit: 0.3 m row at 1:1, 1.5 m ×4 and 4 m ×8 nearest-neighbour). Standard kit previews: `W/outlet_prev/g1/<NAME>_a.png` / `_b.png` (contact sheet `W/o2/kitprev_r7.jpg`).

**What I checked on them:**
- Plate: the profile reads as one soft dome edge, not a bevel; the 12-segment corners hold at 0.3 m; the hollow back shows only in `tilt030`.
- Toggle: the bat stands 32° up in a dark slot; the 0.35 gap round the switch face is dark (no wall); the domed tip catches the key light.
- 6P jack: the cavity with its latch notch reads as the familiar phone-jack shape; brass wires show at 0.3 m.
- IBM connector: black housing, beige contact block, brass leaves, the grip ribs on the latch sides.
- BNC: hex nut, washer, sleeve with two bayonet studs, white insulator and brass socket.
- 4-prong: brown block, four holes with the polarising one larger, the slotted cover screw.
- At 1.5 m and 4 m the LOD1 and LOD2 sets hold the silhouette and the dark dots of the device; no pop is visible at true pixel size.
- The r7 renders differ from r6 by at most 0.16 / 255 (mean) at LOD0: the soft review light does not show the field-normal fix. The gloss check in §10 does.

**Images in the docs folder** (`research/outlets/images/`), as placed in Figma:
- `o2_group_hero030.jpg`, `o2_sheet_<NAME>.jpg` (7), `o2_group_kit_previews.jpg`, `o2_group_lod.jpg`: the 2026-10-04 review round (r5). r6 matched them within 0.18–0.45 / 255 (mean per sheet), and r7 differs from r6 by at most 0.16 / 255 at LOD0 and 0.78 at LOD1, so they stay.
- `o2_cull_*` (10), `o2_cull_pairs.jpg`, `o2_6p_floor_pair.jpg`: the culling fix (VL090).
- New 2026-10-07: `o2_field_gloss_before.jpg`, `o2_field_gloss_after.jpg`, `o2_field_gloss_pair.jpg`, `o2_field_ndev_before.jpg`, `o2_field_ndev_after.jpg`, `o2_field_ndev_pair.jpg`, `o2_field_unity_c1_toggle.jpg` (§10).

## 4. LOD

- `kitlib.Kit.make_lods` does not exist (checked 2026-10-07; `kitlib.py` last changed 2026-10-02). So **each FBX holds LOD0 only**, as the spec says. R3 culls at 12 m by itself.
- The hand-built LOD1 and LOD2 part sets are in every module (`obj["fr_lods"]`), built only when `make_lods` exists. `o2_review.py` forces them on to count and render them.
- The interactables answer (`../interactables/10_spec.md` §8 P-1) is the same convention: `<NAME>_LOD0/1/2` in one FBX, `lodDistances` in the sidecar. Main's `FrontRoomsKitImporter` reads `lodDistances` when a LODGroup exists, and its cull mapping was fixed in 663e858 / 1114d6b. It does nothing for O2 today: our FBX has one mesh, so no LODGroup is made. R3 does not use LODGroups anyway.
- **LOD pop (r7, mean |Δ| per pixel /255 between the two levels at the switch distance):** 1.5 m LOD0 → LOD1: 0.68 (Blank), 0.72 (Toggle1, Toggle2), 0.75 (Phone), 1.22 (Data), 0.93 (BNC), 1.61 (4-prong). 4 m LOD1 → LOD2: 0.83–1.75, 4-prong 3.10. The field-normal pass on the LOD1 plates cut the 1.5 m pop by about 30 % (r6: 0.97–1.46).
- **Changes made on 2026-10-03/04 (to meet ±15 %, each with a visual reason):**
  - LOD1 plates get the same crown points as LOD0 (every 15 mm, +56 tris on a 1-gang plate). Without them the 0.1 mm crown went flat and the field shading stepped at the 1.5 m switch.
  - LOD1 openings get a 2-step round (0.35 / 0.1 / 0 mm at 0 / −0.25 / −0.6), as O1's LOD1 openings, then a wall down to 3.5.
  - LOD1 6P jack: the insert with a 1-step round and side, the dark gap floor, and the cavity with its latch notch and mouth chamfer as a 0.5 mm inset.
  - LOD2: the screw slots as one dark quad each, so the screw line does not vanish at 4 m. Not on the BNC (it would go to +20 %).
  - LOD2 6P jack: a nylon insert quad plus the cavity with its notch as one dark 8-gon.
  - LOD1 4-prong holes get a 0.3 chamfer ring.
- **LOD1 screws differ from O1 (decision for the visual chat).** R3 draws `Kit_OutletScrew` at LOD0 only. O2's LOD1 plates carry a screw stand-in (an ivory head disc with a dark slot). O1's LOD1 plates show "a plain dark screw hole" (the spec's words). At 1.5 m that hole is a 7.4 mm dark dot (about 5 px at 1080p), where LOD0 showed an ivory head: a visible pop on every O1 plate. Pick one rule for both groups: O2's stand-in, or draw the screws to 4 m.

## 5. ESTIMATES that became numbers

All mm. "Spec" = taken as printed in §1.2 (itself an ESTIMATE there). "O2" = O2's own reading.

**Plate (shared with O1)**

| Item | Number | Source |
|---|---|---|
| Corner radius; profile; crown | 2.0; (0,0) (0.15,1.20) (0.45,2.60) (0.95,3.80) (1.70,4.70) (2.70,5.25) (4.00,5.50); field 5.50 at the edge, 5.60 at the centre | Spec |
| Field crown shape | (1 − u²)(1 − v²) cushion from the field edge to the centre; crown points every 15 mm (error 0.006 mm) | O2 (O1 uses the same function, points every 9 mm) |
| Field shading normal (2026-10-07) | the analytic gradient of that cushion on every field corner: at most 0.37° from the wall normal | O2 (§10) |
| Inner profile rings | keep a 0.5 corner radius where the inset passes the 2.0 corner (no mitre crease) | O2 = O1 (`R_FLOOR`) |
| Hollow back | rim 1.6 on the wall, cavity face 3.5 | Spec |
| Countersink | Ø 7.4 at the field, a 0.05 lip down to the Ø 6.6 seat, then 82° to a Ø 3.6 bore (dark disc) | Spec (7.4, 82°); O2 = O1 (lip 0.05, bore 3.6) |
| Segments | plate corners 12; countersink rim 64, seat 32, bore 16; openings 6 per corner (r 1.0) | Spec (12, ≥ 64); O2 |
| Opening rounds | 0.5 mm, 3 segments, from the field into the opening | Spec |
| Edge and hand wear (`fr_wear`) | G 0.9 on the outer 1.5 mm; R 0.85 within 9–14 mm of a device; B ≤ 0.35 on dark faces | O2 |

**Toggle**

| Item | Number | Source |
|---|---|---|
| Opening | 10.3 × 23.8, r 1.0 | Spec |
| Screws | z ±30.15 (60.3 apart) | Spec |
| Switch face | 0.35 inside the opening on each side (9.6 × 23.1), corner r 0.6, 0.5 behind the field | Spec (0.5); O2 (0.35, r 0.6) |
| Switch-face edge round | **0.2**, 3 segments. The spec's 0.6 does not fit: a 9.6 wide face with an 8.6 bat slot leaves 0.5 a side | O2 (deviation) |
| Bat slot | 8.6 × 9.0, r 0.5, 0.15 chamfer; floor 1.6 below the pivot | O2 |
| Bat | base 7.5 × 5.0 (r 1.5) to tip 6.0 × 4.0 over 15.6, then a 1.4 domed cap (17.0 total); tip radius **1.95** (2.0 on a 4.0-thick section leaves no straight); pivot 2.0 behind the field; 32° up; 4 segments per corner | Spec (sizes, 17, 2.0, 32°); O2 (1.5, 1.95, cap) |
| Dark gap floor | 3.0 above the wall (the device strap seen through the gap) | O2 |

**6P phone jack**

| Item | Number | Source |
|---|---|---|
| Opening | 14.0 × 12.0, r 1.0 | Spec |
| Screws | z ±41.65 (blank spacing) | Spec |
| Insert | 0.3 inside the opening (13.4 × 11.4), r 0.7, 0.3 round; face 0.3 behind the field | Spec (0.3 behind); O2 |
| Cavity | 9.9 × 6.8 from z −2.4 to +4.4; latch notch 3.2 × 2.0 below it; 0.15 mouth chamfer | Spec (sizes); O2 (placement: cavity + notch centred on the insert) |
| Cavity depth | **5.25, not 12.** The floor stops 0.05 mm in front of the wall plane (nothing may lie behind y = 0, §1.1) | O2 (deviation, forced) |
| Contacts | 4 brass wires Ø 0.45 at 1.0 pitch: out of the top wall 0.9 behind the face, bent 0.3 below the cavity top, then 3.6 long at 20°; 6 segments | Spec (Ø, pitch, 20°); O2 (path) |

**IBM Data Connector** (UNVERIFIED against a photo: `media_candidates.md` om08)

| Item | Number |
|---|---|
| Housing | 32.0 × 32.0 (spec), r 2.0, front at 21.5; 0.8 rim round, 2.0 rim, face sunk 1.5 |
| Flange | 35 × 35, r 2.5, 1.2 high, sunk 0.15 into the crown; plate opening under it 33 × 33 (hidden) |
| Shell seam | 0.4 deep, 12.2–13.0 above the wall |
| Contact block (beige, NI) | 22 × 10, upper half (z +1 to +11), front at 24.3, 0.4 round |
| Recess | 22.8 × 10.8, lower half (z −11.4 to −0.6), down to 12.0 |
| Contacts | 4 brass leaves 1.6 × 0.35 at x ±2, ±6, curling round the block's front; 4 pads on the recess ceiling |
| Grip ribs | 3 per latch side at z −4 / 0 / +4; 0.8 wide, 0.6 proud, 15–19 above the wall |

**BNC bulkhead**

| Item | Number |
|---|---|
| Washer | Ø 13.5 × 0.6 |
| Nut | 1/2 in hex (12.7 across flats) × 2.4, 30° front chamfer to a Ø 12.1 face circle |
| Sleeve | Ø **9.5** (spec), with one thread groove at the nut; front at 18.0; bore Ø 8.3 × 2.6 deep |
| Insulator | Ø 6.6 × 0.5 white (TI); socket Ø 2.0 with a Ø 1.0 hole (brass) |
| Bayonet studs | 2 × Ø 1.3, 1.1 past the sleeve, 2.2 behind the front |
| Segments | **36** on the chrome, not 64: 64 would put LOD0 near 3,100 (+29 %). The Ø 9.5 sleeve chord error at 36 is 0.018 mm (0.05 px at 0.3 m) |

**4-prong block (404A type)** (UNVERIFIED: `media_candidates.md` om07)

| Item | Number |
|---|---|
| Block | 52 × 52 × 18 (spec), plan corner r 6.0, top round 4.0 |
| Cover/base seam | 0.4 deep at 5.2–5.9 above the wall |
| Prong holes | 4: top pair at x ±7.0, z +7.0 (r 1.8); bottom pair at x ±9.5, z −7.0 (r 1.8 and r 2.2, the polarising hole); 0.3 chamfer; brown wall 2.5 deep, then dark; brass springs 3.5 below the face |
| Cover screw | slotted, head Ø 5.5 × 0.8 + 0.35 crown, slot 0.7 × 0.65, in a Ø 7.0 × 1.5 counterbore |

**LOD (hand-built)**

| Item | Number |
|---|---|
| LOD1 plate | profile (0,0) (0.95,3.80) (4.00,5.50); 4 segments per corner; no back; screw stand-in: 24-segment head, slot quad 5.6 × 0.8; field corners take the crown normal (§10) |
| LOD2 plate | 8-vertex outline (corner chamfer 1.2) on the wall, one bevel ring 3.0 in at 5.55, fan to a 5.60 centre |

## 6. Era lock (22_era_lock.md; 01 §10)

- Toggle, not rocker (Decora existed from 1972–73 but was the commercial minority in 1990). No ON/OFF printing.
- 6P modular phone jack (FCC 1976): current. No RJ45, no CAT or TIA-568 marks.
- IBM Data Connector (1984) and BNC on 10BASE2 (1985): current in 1990.
- The 4-prong block (about 1960) is second-hand stock, inside the ~1955 floor.
- No tamper-resistant shutters, no screwless snap plates, no maker names, no UL mark, no dates, no text of any kind.

## 7. Issues and requests

1. **NEEDS APPROVAL (P-4b):** `Prop_ThermosetIvory` and `Prop_NylonIvory` are new tint-only slots. Main still does not define them (no match in `Assets/Editor`, `Assets/Scripts` or `Assets/Resources`, 2026-10-07). Until `FrontRoomsRenderSetup` does, Unity imports these FBX with missing materials (fallbacks in `10_spec.md` §1.5). The code task's proposed patch is `data/20_rendersetup_p4b_p6.diff.txt`.
2. **NEEDS APPROVAL (P-1/P-1b):** LOD1/LOD2 export waits on `make_lods` and per-part `fr_lods`. Nothing to change in the modules when it lands.
3. **LOD1 screw rule differs between O1 and O2** (§4). The visual chat should pick one.
4. **Sidecar `placement` is "Floor"** on every O2 (and O1) kit. kitlib maps only `wall_unit`/`wall_decor` to "Wall", and §1.1 forbids those tags (the Level Designer would set them 0.03 m off the wall). R3 ignores `placement`, but the Level Designer may list outlets as floor props. Proposal (NEEDS APPROVAL, kitlib): map `wall_flush` to a new `"WallFlush"` placement.
5. **The standard kitlib previews (`<NAME>_a/_b.png`) are not useful for wall kits.** They stand the asset on a floor at z = 0, and our origin is the plate centre, so the lower half of each plate is under the preview floor. The review set (§3) is the evidence.
6. **The 6P cavity is 5.25 deep, not 12** (§5): the wall-plane rule wins. It reads as a deep black hole at 0.3 m.
7. **`Kit_JackData` face and `Kit_JackPhone4Prong` hole pattern are readings**, not measurements. om08 and om07 in `media_candidates.md` would check them; O2 added five more leads there (om_o2_01…05). Nothing was downloaded.
8. **The field-normal fix is verified in Blender, not yet in Unity** (§10). This stage may not run Unity. The r7 FBX are in `W/proj_outlet` since 18:51; the next C1/integrate capture of the lone switch (`20_lone_switch_toggle1.jpg` framing) should show a clean plate. **O1's plates have the same fault (measured, not fixed: not my file).** `tools2/field_dev.py` on O1's FBX in `W/proj_outlet` (built 18:46): `Kit_OutletDuplex` and `Kit_OutletDuplex20` max 4.83° off the crown, 202 of 684 field corners over 1°; `Kit_OutletDuplex_Cracked` 244 of 823 over 1°. (The probe does not fit the Jumbo's deeper field.) The same `field_normals()` pass in O1's `outlet_common.py` would fix them; the O1 stage or the integrate stage owns that.
9. **Machine load** was 530–670 on 16 cores during r7 (many workflows at once). The GPU reviews still finished (7 kits in 7 min).
10. **Bytecode (codex-audit HYG-H1):** every r7 Blender call ran with `sys.dont_write_bytecode = True`, so no tracked `.pyc` changed. The `.pyc` files of the O2 modules in main are from 2026-10-03/04.

## 8. Verification frames (Figma)

Section FRONTROOMS · VISUAL VERIFICATION LOG (`2595:6093`, page `2099:76`); rows in `Documentation/VERIFICATION_LOG.md` §3.

| VL | Node | Check | Verdict | Images |
|---|---|---|---|---|
| VL088 | `2630:6099` | O2 switches and jacks at 0.3 m | PASS | `o2_group_hero030.jpg`, `o2_sheet_Kit_OutletToggle2.jpg`, `o2_sheet_Kit_JackData.jpg` |
| VL089 | `2630:6110` | O2 plates at 1.5 m and 4 m | PASS | `o2_group_lod.jpg`, `o2_sheet_Kit_JackPhone.jpg`, `o2_sheet_Kit_JackBNC.jpg` |
| VL090 | `2630:6121` | Culled faces and gap leaks | FAIL→FIXED | `o2_cull_pairs.jpg`, `o2_6p_floor_pair.jpg` |
| VL135 | `2793:6103` | Plate field shading clean | FAIL→FIXED | `o2_field_gloss_before.jpg`, `o2_field_gloss_after.jpg`, `o2_field_unity_c1_toggle.jpg` |

- VL088–VL090 were placed on 2026-10-04. r7 gives the same verdicts and numbers, so their slides keep their images; their index rows now carry a one-line r7 note.
- VL135 was placed on 2026-10-07 at 18:5x in its own cell (x 6240, y 41080). The cover (`2597:6093`) was recounted from the section: 19 tasks, 131 checks, 415 images; the N3 legend now reads "VL064, 088–094, 134–135 · 10 checks".

## 9. Main and the clone (what I checked, 2026-10-07)

- Main HEAD is 6c6fe81 (18:29, Red's "1"). Since 279c144 (2026-10-04 21:30) Red made 5 more "1" commits. The only O2 file among them is `outlet_plate_common.py` (a5262fb, the field-normal fix). `kitlib.py`, `build_asset.py` and the other seven O2 modules are as they were on 2026-10-04.
- Codex's commits (8ef5b64, daef6c2, 9e754a2, 75cfdff) did not touch any `outlet_*` file, `kitlib.py` or `build_asset.py`.
- r7 built straight from main's modules (no clone copy of the Python), so what was verified is what main holds.
- **Codex audit:** `20_findings.md` does not exist yet. `00_main_state.md`, the `10_review_*.md` files and `W/recovery/codex-audit-findings.json` list outlets as "not touched". The only finding that applies is HYG-H1 (tracked `.pyc`), handled as §7 item 10.
- **Clone:** `W/proj_outlet` is shared with the O1, O3 and code stages of this workflow. I copied only the seven O2 FBX/JSON into it and kept their `.meta` files. A Unity batch of the code task was running on it; I ran no Unity.
- No `.meta` question arises in main: `Tools/` is outside `Assets/`, and the FBX/JSON are not in main.

## 10. 2026-10-07: recovery and the plate field fix

**Recovery.** The 2026-10-05 reboot wiped the old scratchpad: the O2 clone outputs, the review tools and every log. On 2026-10-07 the review, verify, sheet and probe tools were rebuilt from the 2026-10-03/04 transcripts (`W/o2/old_sp/o2/`), and r6 (16:35–17:10) rebuilt all seven kits from main and re-ran every check. r6 matched the 2026-10-04 doc sheets within 0.18–0.45 / 255 (mean per sheet; no pixel off by more than 24), so the recovery was faithful.

**The fault.** The C1 code task's Unity capture of the lone switch (`20_lone_switch_toggle1.jpg`, 2026-10-04) shows diagonal lines on the plate field, like folds in paper. Cause: the per-part weighted-normal recipe (shade smooth, sharp at 35°, WEIGHTED_NORMAL face area) smooths each field vertex with its smooth neighbours: the field-edge ring with the 11° shoulder band, the opening rims with their 0.5 round, the countersink rims with the lip. Those vertices took normals tilted up to 5–8°. The large constrained-Delaunay field triangles (up to 813 mm²) spread that tilt across the field in ramps that follow the triangulation. Under a glossy material and a troffer above, the ramps read as lines.

**The fix** (`outlet_plate_common.py`, `finalize()` → `field_normals()`; plates are marked with `fr_field = [W, H]` in `thermoset_plate()` and `plate_lod1()`):
- After the weighted-normal modifier is applied, every corner in the field's own smooth fan takes the **analytic crown normal**, the gradient of the (1 − u²)(1 − v²) cushion. That normal is at most 0.37° from the wall normal.
- Corners across a sharp edge keep theirs. The shoulder band and the rounds now ramp from the field normal to their next ring: narrow, even strips, as on a molded plate.
- No geometry change: LOD0 triangles, bounds, the plate match with O1 and every probe result are identical to r6.
- The normals survive `kitlib.finish()` (join, shade smooth, sharp by the same angle) and the FBX round trip.

**Measured on the exported FBX** (`tools2/field_dev.py`: every TI corner whose face lies inside the field rectangle and on the crown within 0.003 mm):

| Plate | Field corners | Before (r6): max / corners > 1° | After (r7): max / corners > 1° |
|---|---|---|---|
| Blank | 642 | 5.17° / 198 | 0.02° / 0 |
| Toggle1 | 708 | 5.17° / 160 | 0.02° / 0 |
| Toggle2 | 1,266 | 5.18° / 164 | 0.00° / 0 |
| JackPhone | 720 | 5.17° / 206 | 0.02° / 0 |
| JackData | 660 | 5.17° / 202 | 0.04° / 0 |
| JackBNC | 642 | 5.17° / 198 | 0.02° / 0 |

(The 4-prong block has no plate field.) The build log reports the same pass per plate: 1,066–2,122 field corners corrected, largest correction 5.17°. On the hand-built LOD1 plates (2-step edge, 29° shoulder) the largest correction is 23.7°, which is why the LOD1 pop fell by about 30 % (§4).

**Images** (`research/outlets/images/`):
- `o2_field_gloss_before.jpg` / `_after.jpg` (`_pair.jpg` side by side): Cycles gloss check of the exported Toggle1 and BNC, ivory at roughness 0.22, a 0.6 m troffer 1.2 m above and 0.4 m out, camera 0.55 m away, 22° above and 18° to the side. Before: diagonal ramps from the corners and round the screws. After: one clean field.
- `o2_field_ndev_before.jpg` / `_after.jpg` (`_pair.jpg`): the shading-normal tilt, front view, gain ×10 (yellow = tilted). Before: fans from every corner, screw and opening. After: a smooth cushion.
- `o2_field_unity_c1_toggle.jpg`: the C1 Unity frame before the fix (the plate cropped from `20_lone_switch_toggle1.jpg`, ×4, contrast stretched).

This check is VL135 (`2793:6103`).
