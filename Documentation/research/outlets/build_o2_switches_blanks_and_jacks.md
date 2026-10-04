# Build O2 — switches, blanks and jacks

Status: **DONE, 2026-10-03 23:xx** (resumed after the 17:54 usage-limit stop). Build contract: `10_spec.md` §1.1–§1.5, §5.0, §5.2.

- Seven assets built, exported and checked: `Kit_OutletBlank`, `Kit_OutletToggle1`, `Kit_OutletToggle2`, `Kit_JackPhone` (P1); `Kit_JackData`, `Kit_JackBNC`, `Kit_JackPhone4Prong` (P2).
- Every LOD0 lands within ±15 % of its budget. The hand-built LOD1 and LOD2 sets also land within ±15 % (they were up to −40 % before this pass).
- The O2 plate matches O1's plate to **0.008 mm** (about 3,300 rays per kit, 0 outline mismatches).
- Unity was not opened. Nothing under `Frontrooms3D/Assets` was touched. `kitlib.py` and `build_asset.py` are unchanged.

---

## 0. The assets

Sizes are W × H × proud in mm, measured from the exported FBX. Triangles are LOD0 (shipped) / LOD1 / LOD2 (hand-built, not exported yet: §4).

| Asset | Module | Size (mm) | LOD0 | LOD1 | LOD2 | Budget | Slots |
|---|---|---|---|---|---|---|---|
| `Kit_OutletBlank` | `outlet_blank.py` | 69.8 × 114.3 × 5.60 | 1,412 (+8.6 %) | 426 (−5.3 %) | 28 (−6.7 %) | 1,300 / 450 / 30 | TI, PB |
| `Kit_OutletToggle1` | `outlet_toggle1.py` | 69.8 × 114.3 × 18.41 | 2,304 (+9.7 %) | 740 (−7.5 %) | 40 (0 %) | 2,100 / 800 / 40 | TI, PB, NI |
| `Kit_OutletToggle2` | `outlet_toggle2.py` | 115.9 × 114.3 × 18.39 | 3,676 (+5.0 %) | 1,382 (+6.3 %) | 56 (+12 %) | 3,500 / 1,300 / 50 | TI, PB, NI |
| `Kit_JackPhone` | `outlet_jack_phone.py` | 69.8 × 114.3 × 5.59 | 2,126 (−3.4 %) | 694 (−13.3 %) | 36 (−10 %) | 2,200 / 800 / 40 | TI, PB, NI, BR |
| `Kit_JackData` | `outlet_jack_data.py` | 69.8 × 114.3 × 24.47 | 2,762 (−1.4 %) | 1,000 (0 %) | 68 (+13.3 %) | 2,800 / 1,000 / 60 | TI, PB, NI, BR |
| `Kit_JackBNC` | `outlet_jack_bnc.py` | 69.8 × 114.3 × 18.00 | 2,716 (+13.2 %) | 1,008 (+12 %) | 56 (+12 %) | 2,400 / 900 / 50 | TI, PB, CH, BR |
| `Kit_JackPhone4Prong` | `outlet_jack_phone4.py` | 52.0 × 52.0 × 18.00 | 1,672 (−7.1 %) | 628 (−10.3 %) | 38 (−5 %) | 1,800 / 700 / 40 | CE, PB, BR |

Slots: TI `Prop_ThermosetIvory`, NI `Prop_NylonIvory` (both new, registered with `register_slot` as #E6DDC2 / #E2D9BF; NEEDS APPROVAL P-4b), PB `Prop_PlasticBlack`, BR `Prop_Brass`, CH `Prop_Chrome`, CE `Prop_Ceramic` (brown).

**Every sidecar** (checked on the exported JSON):
- `noCollider: true`, `colliders: []`.
- Tags `outlet` + family + `wall_flush`. Families: `outlet_switch` (toggles), `outlet_jack` (four jacks), `outlet_receptacle` (blank: it replaces a receptacle in the plan, §1.4).
- Anchors (Unity space, mm): `screw_0..k` on the screw seats (blank and jacks: x 0, y ±41.65, z 5.49; toggles: y ±30.15, z 5.51–5.52; Toggle2 gangs at x ±23.0) and `plate_top` (0, 57.15, 0). The 4-prong block has `plate_top` (0, 26.0, 0) and no screw anchors: its cover screw is in the mesh, and `outlet.screws = 0` tells R3 not to draw `Kit_OutletScrew`.
- `lodDistances [1.5, 4.0, 12.0]`, `lodRatios`, `lodBudget`, `lodHandBuilt: true`, and `outlet {plateW, plateH, proud, screws, family, …}` for R3.
- Nothing lies behind the wall plane: min h = 0.0000 mm on all seven.
- `fr_wear` (BYTE_COLOR, face corner) is on every corner of every part; custom (weighted) normals survive the FBX round trip on all seven.

## 1. Files

**Modules (new files, in the real project, as the task allows):** `Tools/Blender/frontrooms_kit/assets/`
- `outlet_plate_common.py` (helper: no `NAME`, no `build`), `outlet_blank.py`, `outlet_toggle1.py`, `outlet_toggle2.py`, `outlet_jack_phone.py`, `outlet_jack_data.py`, `outlet_jack_bnc.py`, `outlet_jack_phone4.py`.
- No module imports O1's or O3's helper. `outlet_toggle2.py` imports `outlet_toggle1.toggle_plate` (same group).

**Outputs (private clone only):** `<scratchpad>/proj_outlet/Assets/Resources/Props/Models/<NAME>.fbx` and `.json`, built 22:05–22:31.

**Scratchpad tools** (`<scratchpad>/o2/`): `o2_review.py` (review renders; forces the LOD1/LOD2 parts on), `o2_verify.py` (checks on the exported FBX), `o2_counts.py` (LOD counts only), `sheet.py`, `doc_images.py`, `final2.sh`, `review_cpu.sh`; logs `final2_build.log`, `final2_verify.log`, `verify3.json`.

(§3 lists the images.)

## 2. Self-checks (§5.2)

| # | Check | Result |
|---|---|---|
| a | Plate identical to O1's numbers | **PASS.** 21 shared constants equal O1's (`outlet_common.py`) within 0.05 mm, and 9 equal O3's. On the geometry: front rays at 0.05–0.1 mm steps along z = 0 (past each opening), x = ±25 and the four corner diagonals, against O1's exported `Kit_OutletDuplex` (clone, 21:58). Max height difference **0.0072 mm** (Blank, BNC), 0.0070 (Toggle1, JackPhone), 0.0084 (Data); 3,018–3,308 rays per kit; **0 outline mismatches**. Each module also asserts the outline (69.8 × 114.3 or 115.9 × 114.3) and every profile station to 0.05 mm on its own mesh |
| b | The bat clears the opening at +32° and −32° (roll 180°) by ≥ 0.5 mm | **PASS.** Sliced exactly on the mesh, both rolls: **1.457 mm** to the plate opening (between the switch face and the field), **0.550 mm** to the bat slot (between the slot floor and the face). Asserted in the build |
| c | The 6P cavity takes a 9.85 mm plug block | **PASS.** Cavity 9.90 × 6.80: 0.025 mm a side. BVH test on the exported FBX: a 9.85 × 6.6 × 5.0 block 1 mm into the cavity touches **0** faces; a 9.95 control block touches 12 |
| d | The IBM connector is 32 × 32 and stands ≤ 25 mm proud | **PASS with a note.** Housing body 32.0 × 32.0; proud **24.47** (contact block front). The grip ribs on the two latch sides stand 0.6 out, so the envelope is 33.2 × 32.0; the mounting flange on the plate is 35 × 35 × 1.2 (ESTIMATE, a bezel) |
| e | No text, logos or "CAT" marks | **PASS.** No text geometry or decals exist in any module (no decal UVs, no label slot). No RJ45: the only modular jack is a 6P with a 9.9 mm cavity (RJ45 needs 11.7) |
| — | Proud limits | Toggles 18.41 / 18.39 ≤ 20; Data 24.47 ≤ 25; BNC 18.00, 4-prong 18.00 ≤ 25 (data/jack limit); Blank 5.60 and Phone 5.59 ≤ 7.5 |
| — | Triangles within ±15 % | LOD0 asserted in every build. LOD1/LOD2 asserted whenever the hand-built parts exist (the review build forces them) |

## 3. Renders (reviewed)

FILL-IN

## 4. LOD

- `kitlib.Kit.make_lods` does not exist (checked 22:0x; `kitlib.py` last changed 2026-10-02). So **each FBX holds LOD0 only**, as the spec says. R3 culls at 12 m by itself.
- The hand-built LOD1 and LOD2 part sets are in every module (`obj["fr_lods"]`), built only when `make_lods` exists. `o2_review.py` forces them on to count and render them.
- The interactables answer (`../interactables/10_spec.md` §8 P-1) is the same convention: `<NAME>_LOD0/1/2` in one FBX, `lodDistances` in the sidecar. Codex's commit 8ef5b64…75cfdff added the importer half of P-1 in main (`FrontRoomsKitImporter` reads `lodDistances` when a LODGroup exists). It does nothing for O2 today: our FBX has one mesh, so no LODGroup is made. R3 does not use LODGroups anyway.
- **Changes this pass (to meet ±15 %, each with a visual reason):**
  - LOD1 plates get the same crown points as LOD0 (every 15 mm, +56 tris on a 1-gang plate). Without them the 0.1 mm crown went flat and the field shading stepped at the 1.5 m switch.
  - LOD1 openings get a 2-step round (0.35 / 0.1 / 0 mm at 0 / −0.25 / −0.6), as O1's LOD1 openings, then a wall down to 3.5.
  - LOD1 6P jack: the insert with a 1-step round and side, the dark gap floor, and the cavity with its latch notch and mouth chamfer as a 0.5 mm inset (was a flat ring and one dark quad).
  - LOD2: the screw slots as one dark quad each (the slot LOD1 shows), so the screw line does not vanish at 4 m. Not on the BNC (it would go to +20 %).
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
| LOD1 plate | profile (0,0) (0.95,3.80) (4.00,5.50); 4 segments per corner; no back; screw stand-in: 24-segment head, slot quad 5.6 × 0.8 |
| LOD2 plate | 8-vertex outline (corner chamfer 1.2) on the wall, one bevel ring 3.0 in at 5.55, fan to a 5.60 centre |

## 6. Era lock (22_era_lock.md; 01 §10)

- Toggle, not rocker (Decora existed from 1972–73 but was the commercial minority in 1990). No ON/OFF printing.
- 6P modular phone jack (FCC 1976): current. No RJ45, no CAT or TIA-568 marks.
- IBM Data Connector (1984) and BNC on 10BASE2 (1985): current in 1990.
- The 4-prong block (about 1960) is second-hand stock, inside the ~1955 floor.
- No tamper-resistant shutters, no screwless snap plates, no maker names, no UL mark, no dates, no text of any kind.

## 7. Issues and requests

1. **NEEDS APPROVAL (P-4b):** `Prop_ThermosetIvory` and `Prop_NylonIvory` are new tint-only slots. Until `FrontRoomsRenderSetup` defines them, Unity imports these FBX with missing materials (fallbacks in `10_spec.md` §1.5).
2. **NEEDS APPROVAL (P-1/P-1b):** LOD1/LOD2 export waits on `make_lods` and per-part `fr_lods`. Nothing to change in the modules when it lands.
3. **LOD1 screw rule differs between O1 and O2** (§4). The visual chat should pick one.
4. **Sidecar `placement` is "Floor"** on every O2 (and O1) kit. kitlib maps only `wall_unit`/`wall_decor` to "Wall", and §1.1 forbids those tags (the Level Designer would set them 0.03 m off the wall). R3 ignores `placement`, but the Level Designer may list outlets as floor props. Proposal (NEEDS APPROVAL, kitlib): map `wall_flush` to a new `"WallFlush"` placement.
5. **The standard kitlib previews (`<NAME>_a/_b.png`) are not useful for wall kits.** They stand the asset on a floor at z = 0, and our origin is the plate centre, so the lower half of each plate is under the preview floor. The review set (§3) is the evidence.
6. **The 6P cavity is 5.25 deep, not 12** (§5): the wall-plane rule wins. It reads as a deep black hole at 0.3 m.
7. **`Kit_JackData` face and `Kit_JackPhone4Prong` hole pattern are readings**, not measurements. om08 and om07 in `media_candidates.md` would check them; O2 added five more leads there (om_o2_01…05). Nothing was downloaded.
8. **Machine load** was 300–950 on 16 cores during this pass. The GPU review run stalled for 17 minutes in the Metal queue, so the reviews were rendered on the CPU (32 samples, denoised).
9. The builds rewrite the tracked `__pycache__/*.pyc` files of the O2 modules (Red's repo tracks them). That is a side effect of Blender importing the modules, as for every kit group.

## 8. Verification frames (Figma)

FILL-IN

## 9. Main changed while paused (what I checked)

- `git -C Frontrooms3D log`: base 7320ed1 (18:25, Red's commit, pre-Codex) holds the O2 modules as they were at 17:49. Codex's commits 8ef5b64, daef6c2, 9e754a2 and 75cfdff did **not** touch any `outlet_*` file, `kitlib.py` or `build_asset.py`. My changes today are an uncommitted diff on top of 7320ed1 (8 files, +74 / −18 lines, plus the `.pyc` files).
- No `.meta` question arises: `Tools/` is outside `Assets/`, and the FBX/JSON stay in the clone.
- `proj_outlet` was re-created from main by another outlets workflow at about 22:10 (the old one is `proj_outlet_pre1910`). All seven O2 FBX/JSON in it are from this pass (22:05–22:31). A Unity batch process of that workflow was running on the clone while I built; I ran no Unity.
- Codex audit: FILL-IN
