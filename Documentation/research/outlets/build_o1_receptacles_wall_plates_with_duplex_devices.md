# Build O1 — receptacles: wall plates with duplex devices

Status: **built and verified, 2026-10-03 (23:xx)**. Spec: `10_spec.md` §1.1–1.5, §5.0–5.1.

- Group O1 of the outlet kit: 7 modules, 9 assets (2 are material variants).
- Built into the private clone `scratchpad/proj_outlet` only. Unity was not run (not on Frontrooms3D, not on the clone).
- `kitlib.py` and `build_asset.py` are unchanged. Only the 8 O1 files in `Tools/Blender/frontrooms_kit/assets/` were written.
- This run continues the attempt that stopped on the usage limit at 17:54. That attempt had written all 8 modules and built 6 of 7 (not `Kit_OutletBare`), with no report. This run re-checked everything, fixed 4 problems and finished the work (§6).

## 1. Assets

| Asset | Module | Size W × H × proud (mm) | Slots (submesh order) | LOD0 / LOD1 / LOD2 tris | vs budget |
|---|---|---|---|---|---|
| `Kit_OutletDuplex` | `outlet_duplex.py` | 69.8 × 114.3 × 6.6 | ThermosetIvory, NylonIvory, PlasticBlack, Brass | **2,880** / 954 / 36 | +10.8 / −4.6 / −10.0 % |
| `Kit_OutletDuplex_Brown` | variant of the above | as Duplex | Ceramic (plate and device share it), PlasticBlack, Brass | as Duplex | |
| `Kit_OutletDuplex_Steel` | `outlet_duplex_steel.py` | 69.8 × 114.3 × 5.7 | Aluminium, NylonIvory, PlasticBlack, Brass | **2,584** / 954 / 36 | +12.3 / +0.4 / −10.0 % |
| `Kit_OutletDuplex20` | `outlet_duplex20.py` | 69.8 × 114.3 × 6.6 | ThermosetIvory, NylonIvory, PlasticBlack, Brass | **2,920** / 986 / 36 | +4.3 / −6.1 / −10.0 % |
| `Kit_OutletDuplex20_IG` | variant of the above | as Duplex20 | ThermosetIvory, **PlasticOrange**, PlasticBlack, Brass | as Duplex20 | |
| `Kit_OutletDuplex_Cracked` | `outlet_duplex_cracked.py` | 69.8 × 114.3 × 6.6 | as Duplex | **3,416** / 1,125 / 36 | +10.2 / +2.3 / −10.0 % |
| `Kit_OutletDuplex_Jumbo` | `outlet_duplex_jumbo.py` | 88.9 × 133.4 × 7.5 | as Duplex | **2,956** / 954 / 36 | +9.5 / −4.6 / −10.0 % |
| `Kit_OutletScrew` | `outlet_screw.py` | Ø 6.6 × 0.99 | ThermosetIvory (R3 recolours it) | **266** | +2.3 % |
| `Kit_OutletBare` (P2) | `outlet_bare.py` | 65.3 × 106.7 × 8.0, box 35.0 behind | NylonIvory, Aluminium, PlasticBlack, Brass | **3,508** / 1,144 / 58 | −2.6 / −12.0 / −3.3 % |

Helper: `outlet_common.py` (no `NAME`, no `build`). It holds the §1.2 numbers, the asserts, the plate, device, screw and LOD builders, and the `fr_wear` painter.

**What each FBX holds today: LOD0 only.** `kitlib.Kit.make_lods` does not exist (P-1 / P-1b are not approved), so the modules build their LOD1 and LOD2 part sets only when it appears (spec §1.1). The LOD1/LOD2 counts above come from the real part sets, built with a scratch-only stub of `make_lods` (`scratchpad/o1/o1_lods.py`, which does not touch `kitlib.py`). They are exported to `scratchpad/o1/lods/<NAME>_LOD0/1/2.fbx` for review only. Until P-1 lands, R3 draws LOD0 out to 12 m (spec §3.6).

Per-part LOD0 split (Duplex): plate 1,486, device 1,182, slot walls and box sheet 110, bore cap 38, brass leaves 64.

Output files (clone): `proj_outlet/Assets/Resources/Props/Models/Kit_Outlet{Duplex,Duplex_Brown,Duplex_Steel,Duplex20,Duplex20_IG,Duplex_Cracked,Duplex_Jumbo,Screw,Bare}.fbx` + `.json`.

## 2. Frame, sidecar, anchors

- Metres, Z up, front −Y (Unity +Z). Origin = the wall-face point at the plate centre. The plate back is the plane y = 0; nothing lies behind it (measured 0.000 mm on all 8 plate assets). `Kit_OutletBare` is the one exception: its box reaches 35.0 mm behind, the spec limit.
- Proud (measured): Duplex 6.6, Steel 5.7, Jumbo 7.5 (= the limit), Screw 0.99, Bare 8.0 (the spec's own Bare number).
- Sidecar: `noCollider: true`, 0 colliders. Tags `outlet`, `outlet_receptacle`, `wall_flush`; never `wall_unit` / `wall_decor`. The screw is tagged `outlet`, `outlet_screw`, `wall_flush` (see §7, issue 8).
- `lodDistances` = [1.5, 4.0, 12.0]. `outlet` = {plateW, plateH, proud, kind, fieldH, seatH, screws, faces, groundDown, basePlateSlot, baseDeviceSlot, slotNote}.
- Anchors (Unity space, mm):

| Anchor | Duplex, D20, Cracked | Steel | Jumbo | Bare |
|---|---|---|---|---|
| `screw_0` (seat centre) | (0, 0, 5.55) | (0, 0, 4.65) | (0, 0, 6.45) | none (no plate) |
| `face_top` | (0, 19.45, 6.6) | (0, 19.45, 5.7) | (0, 19.45, 7.5) | (0, 19.45, 8.0) |
| `face_bottom` | (0, −19.45, 6.6) | (0, −19.45, 5.7) | (0, −19.45, 7.5) | (0, −19.45, 8.0) |
| `plate_top` | (0, 57.15, 0) | (0, 57.15, 0) | (0, 66.7, 0) | (0, 53.35, 0) strap top |

- `Kit_OutletScrew` origin = its seat centre; at roll 0 the slot is horizontal. R3 places it at `screw_0` and rolls it about the plate normal.

## 3. How each asset is built (short)

- **Thermoset plate.** The §1.2 edge profile (7 points, 0 → 5.50 mm) swept round the outline, with 12 segments per corner on the outline. The field crowns from 5.50 to 5.60. The two openings are round-with-flats (Ø 33.3, flats ±14.3, centres ±19.45) at 64 segments per circle, with a 0.5 mm, 3-segment round. The countersink is Ø 7.4 at 82°. The hollow back has a 1.6 mm rim on the wall and a cavity face 3.5 mm up (seen only when the plate is tilted). The cavity face faces the wall, so Unity culls it from the front.
- **Device.** Two 5-15R faces (Ø 32.5, flats ±13.9: 0.4 clearance) stand 1.0 proud of the field, with 0.6 mm 3-segment edges. A slanted gap floor runs from the opening down to the device plane, 1.5 below the field edge. Slots get a 0.15 mm mouth chamfer, black walls down to a black box sheet, and brass leaves (0.4 thick) 3.0 mm in. Ground hole down: two slot "eyes" over an open "mouth".
- **5-20R.** A horizontal 6.4 × 2.0 arm at the neutral slot's centre, pointing to −x (away from hot).
- **Steel.** 0.76 mm sheet with a flat field at 4.70 and a quarter-round formed edge (r 1.5, 3 segments), a straight leg to the wall and 1.0 corners. Same device, 0.9 mm lower.
- **Cracked.** A 0.25 mm V split runs from the countersink to the right edge (zig-zag, exit z = 1.4). The lower side is stepped 0.2 mm, blended out over 6 mm and tapered in over the first 5 mm. A chipped lower-left corner is 5.37 × 4.16 mm, 3.43 deep, with 3 bisected fracture facets plus a flat floor facet.
- **Jumbo.** 88.9 × 133.4. The §1.2 profile is scaled ×1.161 in inset and height, so the crown is 6.5. The corner radius stays 2.0. Same openings, screw and device.
- **Screw.** #6-32 oval head: a sphere cap Ø 6.6 × 1.0 (sphere radius 5.945), 64 rim segments, and a flat-bottomed slot 0.8 × 0.6. The slot runs out through the dome where the dome is lower than its floor.
- **Bare (P2).** The device sits on its steel strap: 33.8 × 106.7, 0.9 thick, resting 1.0 mm off the wall on the box ears. It has plaster-ear score notches at ±47, the empty tapped centre hole (Ø 3.5), and two oval-head box screws in 3.8 × 6 slots (83.3 centres). Side terminals: brass binding heads on the hot side, aluminium on the neutral side, on 8 × 8 clamp plates. The nylon body runs 25 mm back. The 2 × 3 in steel box has a 1.6 wall; its front edge stands 1.0 proud and its back is 35 behind. A ragged dark sheet (0.3 mm proud, gap 4 ± 1 mm) stands in for the hole. **New this run:** a torn lip, 0.5–1.3 mm wide, slopes from the cut edge (0.34) down to the paper (0.08).
- **Wear attribute.** Every part carries `fr_wear` (BYTE_COLOR, face corner): R 0.85 on the field within 4 mm of an opening; G 0.9 on the outer edge crest; B 0.35–0.4 in cavities and the crack; G 0.6 on the fracture faces and the Bare lip. It survives the FBX round trip on all 9 assets.
- **Normals.** Each part gets a WEIGHTED_NORMAL modifier, which `finish()` applies (the interactables G1 recipe). Custom normals survive the FBX round trip on all 9 assets.

## 4. Self-checks (spec §5.1) and asserts

**Asserts in the build** (`outlet_common.py`). Every build runs them, and all pass:
- every §1.2 number against a literal copy of the spec table (±0.0005);
- clearance 0.4, opening height 28.6, opening pitch 38.9, blade pitch 12.7, ground 11.9 below the blade line;
- plate outline ±0.05 and depth; opening centres and sizes;
- face proud 1.0 and face clearance 0.4 on both axes;
- nothing behind y = 0; proud ≤ 7.5;
- seat level with the field (±0.051) and screw crown ≤ 7.5;
- LOD0 triangles within ±15 % (LOD1/LOD2 too, when the part sets are built);
- chip extent 4.5–6.0 × 3.5–4.8;
- Bare: ragged gap 2.5–5.5, strap envelope, towers inside the strap, box ≤ 35.

**Checks on the exported FBX, re-imported** (`scratchpad/o1/o1_verify.py`, result `scratchpad/o1/logs/verify_r7.json`): **9 / 9 pass.**

| Check | Result |
|---|---|
| (a) Plate and device numbers | Sizes as §1; sidecar triangles = FBX triangles; ≤ 4 slots |
| Slot depth (front-facing ray to the first visible face) | 6.35 mm (Duplex, D20, Cracked), 5.45 (Steel), 7.0 (Jumbo, Bare). The plates are 5.7–7.5 mm from a solid wall, so the slot floors stop 0.25 mm in front of it (issue 4) |
| Brass leaves | 3.0 mm below the face on every kit |
| (b) Surprised face | Reads at 0.30 m on every plate (`o1_front030_sheet.jpg`) |
| (c) Screw seat | Probes at r 3.52 mm in 4 directions hit the countersink at 5.577 vs a seat of 5.55 (Steel 4.677 vs 4.65; Jumbo 6.477 vs 6.45). The Cracked plate's +x probe lands in the split, as it should. Screw renders at 0 / 45 / 90° sit flush (`o1_screws_0_45_90.jpg`) |
| (d) Crack has no gap to the wall | 42,120 rays aimed into the split at ±40°: **0 reach the wall behind the plate**. The lowest hit is the groove floor at 2.82 mm. 12 rays near the edge leave past the plate outline and land on the wall beside the plate; that is not a gap |
| (e) Steel edge at 45° light | One thin bright line on the lit edges (`o1_rake030_sheet.jpg`) |
| Era lock | No TR shutters (the slots are open), no USB, no screwless plate, no text, logo, UL mark or date anywhere in the meshes |

## 5. Every ESTIMATE that became a number

**§1.2 ESTIMATEs used exactly as the spec wrote them.** Corner radius 2.0; the thermoset profile and crown 5.60; hollow back rim 1.6 and cavity 3.5; stainless edge r 1.5; countersink Ø 7.4 at 82°; face proud 1.0; device plane 1.5 behind; slot sizes 2.0 × 8.5 / 2.0 × 7.0, ground U Ø 5.0; slot depth 7.0; leaves 0.4 at 3.0; 5-20R arm 6.4 × 2.0.

**O1's own numbers, where §1.2 is silent** (`outlet_common.py`, "O1 design numbers"):

| Number | Value | Why |
|---|---|---|
| Opening / face arcs | 64 per circle (22 segments per arc, 46 points per outline) | Spec §1.1. Round 5 had 51 per circle |
| Corner segments on the inner profile rings | 12 / 12 / 10 / 8 / 4 / 4 / 4 for radii 2.0 / 1.85 / 1.55 / 1.05 / 0.5 / 0.5 / 0.5 (stainless 12 / 12 / 12 / 6 / 4) | Keeps the chord ≤ 0.26 mm. Paid for the 64-segment openings: −210 tris |
| Inner-ring corner radius floor | 0.5 (stainless 0.3) | Rings inset past 2.0 would get a zero radius |
| Device plane | 1.5 below the field **edge** (5.50), not the crown, so 4.00 (stainless 3.20) | Holds the gap floor's slope even |
| Gap floor | a slant from the opening's lower ring (field − 0.5) down to the device plane | Closes the 0.4 gap |
| Seat | 0.05 below the field (5.55) | Inside the ±0.05 tolerance. The 0.4 mm countersink lip reads beside the head |
| Bore | Ø 3.6 at the cone bottom, with a black cap | Seen only when the screw is missing |
| Slot floor | stops 0.25 in front of the wall plane | The wall is solid in the game (issue 4) |
| Leaves | stop 0.5 short of each slot end, 1.2 tall (3.0 → 4.2 below the face) | |
| Field crown points | Steiner grid every 9 mm | |
| Hidden back | 4 segments per corner | Seen only through a tilt gap |
| Box sheet behind the device | 50.8 × 76.2, black, at the slot floor | The 2 × 3 in box |
| Stainless opening edge | 0.35 mm round, 2 segments | Punched sheet, not moulded; spec's 0.5 / 3 is for thermoset |
| Ground U | 16 segments on the semicircle (32 per circle) | Budget; sagitta 0.012 mm = 0.03 px at 0.3 m (issue 3) |
| Countersink | 40 segments | Under the 64-segment screw head 94–96 % of the time |
| Crack | depth 0.55, blend 6.0, taper 5.0, zig-zag path, exit z = 1.4 | `outlet_duplex_cracked.py` |
| Chip | lower-left; planes (1.45, 0.35, −3.9), (0.30, 1.75, −3.3), (0.85, 0.95, −2.3); floor 0.3 → 3.75; cut zone 10.5 mm | Extent 5.37 × 4.16 × 3.43 |
| Jumbo profile | the §1.2 profile ×1.161 (6.5 / 5.6) in inset and height | |
| Screw dome | sphere radius 5.945 from Ø 6.6 and crown 1.0; flat-bottomed slot | |
| Bare | strap 33.8 × 106.7 × 0.9 at 1.0–1.9, r 1.5; ear notches ±47; box screws ±41.65 in 3.8 × 6 slots; centre hole Ø 3.5; body 68 long to −25; box 50.8 × 76.2 inside, wall 1.6, front +1.0, back −35; gap 4 ± 1; sheet 0.3; lip 0.5–1.3 wide, 0.34 → 0.08; terminal heads R 3.6, crown 0.9, band 1.2, slot 1.0 × 0.7, axis 1.5 behind the wall | |
| LOD1 | 2-step edge (profile points (0.95, 3.80) and (4.00, 5.50)), 4 segments per corner, 24-point faces, 0.5 mm slot insets, plain dark screw hole | Spec §1.1 table |
| LOD2 | 8-point outline at full height + one bevel ring; faces as two dark 6-gons, 15.6 × 13.0, 0.3 proud | Spec §1.1 table |

## 6. What this run changed (on top of the 17:44 attempt)

1. **Openings and device faces at 64 segments per circle** (was 51). The inner profile rings now use fewer corner segments, joined by a new zipper bridge (`Mesh.zip`), so every plate stays inside its budget.
2. **Cracked: a shading line along the bottom edge is gone.** The chip's bisect planes used to cut every face that touched the corner zone, including the long bands that run the full width of the bottom edge. That split them and left a faceted line. The cuts now stay inside a 10.5 mm corner zone (the rings get extra columns there). Result: −42 tris.
3. **Bare: torn lip** round the cut-out (+120 tris), so the edge reads torn, not like a grey card.
4. **Sidecar honesty.** kitlib exports a material variant with the base module's meta. The `outlet` block now says `basePlateSlot` / `baseDeviceSlot` with a note, instead of `plateSlot` / `deviceSlot`. `_Brown` maps both ivory slots to `Prop_Ceramic`, and the FBX exporter merges them into **one** submesh (checked). R3 maps by material name, so nothing breaks.
5. LOD1/LOD2 budgets are asserted when the part sets are built (`lod_check`).
6. The verification script now uses a world-space BVH, counts front faces only (Unity culls back faces), and probes the seat in 4 directions. Its round-5 failures were script bugs, not model bugs.
7. Builds now run with `sys.dont_write_bytecode`. My first build at 21:58 had written 7 new `__pycache__/*.pyc` files into the real `Tools/Blender/frontrooms_kit/assets/`; I removed those 7, and nothing else.

## 7. Issues and open items

1. **P-1 / P-1b (NEEDS APPROVAL, visual chat).** No `make_lods`, so each FBX is LOD0 only. The LOD1/LOD2 part sets are written and counted, and turn on by themselves when `make_lods` exists. Convention as `../interactables/10_spec.md` P-1: `<NAME>_LOD0/1/2`, `lodDistances`, per-part `fr_lods`.
2. **P-4b (NEEDS APPROVAL).** `Prop_ThermosetIvory`, `Prop_NylonIvory` and `Prop_PlasticOrange` are registered with `kitlib.register_slot` only. `FrontRoomsRenderSetup` has no material for them yet. R3 (C1's renderer in the clone) maps them to the palette with fallbacks (`Prop_PlasticBeige`, `Prop_PlasticPutty`). A plain importer path would show missing materials until the three `SurfaceDef` lines land.
3. **64-segment rule vs budget.** The openings, faces and screw meet ≥ 64. The ground U (32 per circle) and the countersink (40) do not. Making them 64 would add about 180 tris per plate and push the Duplex past +15 %. Sagitta: 0.012 mm (0.03 px at 0.3 m).
4. **Slot depth.** The spec says 7.0 mm, but a plate whose face stands 6.6 mm off a solid wall can only be 6.35 deep (Steel 5.45). Jumbo and Bare get the full 7.0.
5. **Bare kit size.** The spec's §1.3 size "106.7 × 76 × 8" reads as strap length × box height. The built envelope is 65.3 wide (ragged cut-out plus lip) × 106.7 tall (strap) × 8.0 proud, plus 35 behind.
6. **Bare read at 0.3 m is still UNVERIFIED in Unity** (spec §8 item 7). The dark sheet is flat. Under a frontal light its specular can read grey in the preview. A real hole in the map wall would let the modelled box show; then drop the sheet (`gapSheetH` in the sidecar).
7. **Variant sidecars** carry the base slot names in `outlet` (kitlib limitation, no kitlib edit). Read `slots` for the real materials.
8. **Screw tags.** `Kit_OutletScrew` is tagged `outlet_screw`, not `outlet_receptacle`. It is shared by O2's plates and is not a receptacle. A Level Designer list filtered by `outlet_receptacle` would otherwise offer a loose screw.
9. **5-20R arm size and direction** are still the spec's ESTIMATE; check them against a period photo (`media_candidates.md`).
10. **Preview colours only.** The renders linearise the spec hex colours; the real tint is a Unity look-dev task (L-1: about 11 % darker and warmer than the paper).
11. **Clone re-sync.** At about 22:04 another stage re-created `proj_outlet` from main, which removed the O1 FBX I had built there at 21:58. The final build (this report) went into the new clone at 23:xx. Check that `proj_outlet/Assets/Resources/Props/Models/Kit_Outlet*.fbx` (9 + O2/O3's) is present before any merge.
12. **Main state.** Codex's commits (8ef5b64…75cfdff) touched no outlet file (git diff 7320ed1..HEAD, and codex_audit `00_main_state.md`: "D3 outlets — not touched"). The O1 modules in main are this group's files. Main holds no `Kit_Outlet*` FBX. Nothing here was merged into `Assets/`.

## 8. Previews and close-ups

Kit previews (`build_asset.py`, Cycles, 48 samples): `scratchpad/outlet_prev/g0/<NAME>_a.png` / `_b.png` for the 7 modules.

Close-ups, re-imported from the exported FBX, with back faces culled as in Unity (`scratchpad/o1/o1_render.py`, round 7 in `scratchpad/o1/prev/r7/`):

| Image (kept copy) | What |
|---|---|
| `images/o1_front030_sheet.jpg` | All 8 plate assets, front at 0.30 m, FOV 62 |
| `images/o1_rake030_sheet.jpg` | Steel, Cracked, Bare under 45° raking light at 0.30 m |
| `images/o1_screws_0_45_90.jpg` | `Kit_OutletScrew` at the `screw_0` anchor, rolled 0 / 45 / 90° |
| `images/o1_d150_group.jpg` | Six kits at 1.5 m, eye height 1.62 m, FOV 76, Level 0 paper |
| `images/o1_d400_group.jpg` | The same at 4 m |
| `images/o1_macro_cracked_bottomedge.jpg` | The cracked plate's chipped corner and bottom edge band |
| `images/o1_macro_bare_lip.jpg` | The Bare kit's torn lip, gap and box edge |
| `images/o1_lod_sheet.jpg` | LOD0 / LOD1 / LOD2 part sets |
| `images/o1_kit_previews.jpg` | The standard build stills |

Figma verification log: see §9.

## 9. Verification log

See the VL rows in `Documentation/VERIFICATION_LOG.md` (task N3).
