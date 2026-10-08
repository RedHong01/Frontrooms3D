# Build O1 — receptacles: wall plates with duplex devices

Status: **built and verified, 2026-10-07 (~19:10)**. Spec: `10_spec.md` §1.1–1.5, §5.0–5.1.

- Group O1 of the outlet kit: 7 modules, 9 assets (2 are material variants).
- Built into the private clone `W/proj_outlet` only (`W` = `/Users/redwang/FrontRoomsVisualWork`). Unity was not run by this stage (not on Frontrooms3D, not on the clone).
- `kitlib.py` and `build_asset.py` are unchanged. Only the 8 O1 files in `Tools/Blender/frontrooms_kit/assets/` were written: `outlet_common.py`, `outlet_duplex.py`, `outlet_duplex_steel.py`, `outlet_duplex20.py`, `outlet_duplex_cracked.py`, `outlet_duplex_jumbo.py`, `outlet_screw.py`, `outlet_bare.py`.
- History: built 2026-10-03 (rounds 1–7); round 8 (LOD2 faces) half-written 2026-10-04 when the usage limit hit; the 2026-10-05 reboot wiped the scratchpad (clone, scratch tools, all images). Today: scratch tools recovered from the transcripts, round 9 (2026-10-07 16:30–17:45) and round 10 (this run, 18:13–19:10). §6 lists what changed.

## 1. Assets

| Asset | Module | Size W × H × proud (mm) | Slots (submesh order) | LOD0 / LOD1 / LOD2 tris | vs budget |
|---|---|---|---|---|---|
| `Kit_OutletDuplex` | `outlet_duplex.py` | 69.8 × 114.3 × 6.6 | ThermosetIvory, NylonIvory, PlasticBlack, Brass | **2,880** / 954 / 42 | +10.8 / −4.6 / +5.0 % |
| `Kit_OutletDuplex_Brown` | variant of the above | as Duplex | Ceramic (plate and device share it), PlasticBlack, Brass | as Duplex | |
| `Kit_OutletDuplex_Steel` | `outlet_duplex_steel.py` | 69.8 × 114.3 × 5.7 | Aluminium, NylonIvory, PlasticBlack, Brass | **2,584** / 954 / 42 | +12.3 / +0.4 / +5.0 % |
| `Kit_OutletDuplex20` | `outlet_duplex20.py` | 69.8 × 114.3 × 6.6 | ThermosetIvory, NylonIvory, PlasticBlack, Brass | **2,920** / 986 / 42 | +4.3 / −6.1 / +5.0 % |
| `Kit_OutletDuplex20_IG` | variant of the above | as Duplex20 | ThermosetIvory, **PlasticOrange**, PlasticBlack, Brass | as Duplex20 | |
| `Kit_OutletDuplex_Cracked` | `outlet_duplex_cracked.py` | 69.8 × 114.3 × 6.6 | as Duplex | **3,416** / 1,125 / 42 | +10.2 / +2.3 / +5.0 % |
| `Kit_OutletDuplex_Jumbo` | `outlet_duplex_jumbo.py` | 88.9 × 133.4 × 7.5 | as Duplex | **2,956** / 954 / 42 | +9.5 / −4.6 / +5.0 % |
| `Kit_OutletScrew` | `outlet_screw.py` | Ø 6.6 × 0.99 | ThermosetIvory (R3 recolours it) | **266** | +2.3 % |
| `Kit_OutletBare` (P2) | `outlet_bare.py` | 65.3 × 106.7 × 8.0, box 35.0 behind | NylonIvory, Aluminium, **Rubber**, Brass | **3,508** / 1,144 / 68 | −2.6 / −12.0 / +13.3 % |

Every count is inside ±15 %.

Helper: `outlet_common.py` (no `NAME`, no `build`). It holds the §1.2 numbers, the asserts, the plate, device, screw and LOD builders, and the `fr_wear` painter.

**What each FBX holds today: LOD0 only.** `kitlib.Kit.make_lods` still does not exist (P-1 / P-1b are not approved; checked 2026-10-07). The modules build their LOD1 and LOD2 part sets only when it appears (`oc.has_lods()`), so today's FBX and sidecars are byte-for-byte the same as without the LOD code. The LOD1/LOD2 counts above come from the real part sets, built with a scratch-only stub (`W/o1/o1_lods.py`, which does not touch `kitlib.py`) and exported to `W/o1/lods_marks/<NAME>_LOD0/1/2.fbx` for review only. Until P-1 lands, R3 draws LOD0 out to 12 m (spec §3.6).

Per-part LOD0 split (Duplex): plate 1,486, device 1,182, slot walls and box sheet 110, bore cap 38, brass leaves 64.

Output files (clone): `W/proj_outlet/Assets/Resources/Props/Models/Kit_Outlet{Duplex,Duplex_Brown,Duplex_Steel,Duplex20,Duplex20_IG,Duplex_Cracked,Duplex_Jumbo,Screw,Bare}.fbx` + `.json`. Final build 2026-10-07 18:4x. A rebuild after the last module edit (19:0x, into `W/o1/out_r10`) gave identical sidecars.

## 2. Frame, sidecar, anchors

- Metres, Z up, front −Y (Unity +Z). Origin = the wall-face point at the plate centre. The plate back is the plane y = 0; nothing lies behind it (measured 0.000 mm on all 8 plate assets). `Kit_OutletBare` is the one exception: its box reaches 35.0 mm behind, the spec limit.
- Proud (measured): Duplex 6.6, Steel 5.7, Jumbo 7.5 (= the limit), Screw 0.99, Bare 8.0 (the spec's own Bare number).
- Sidecar: `noCollider: true`, 0 colliders. Tags `outlet`, `outlet_receptacle`, `wall_flush`; never `wall_unit` / `wall_decor`. The screw is tagged `outlet`, `outlet_screw`, `wall_flush` (§7, issue 8).
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

- **Thermoset plate.** The §1.2 edge profile (7 points, 0 → 5.50 mm) swept round the outline, 12 segments per corner. The field crowns from 5.50 to 5.60. Two round-with-flats openings (Ø 33.3, flats ±14.3, centres ±19.45) at 64 segments per circle, with a 0.5 mm, 3-segment round. Countersink Ø 7.4 at 82°. Hollow back: a 1.6 mm rim on the wall and a cavity face 3.5 mm up; the cavity face points at the wall, so Unity culls it from the front.
- **Device.** Two 5-15R faces (Ø 32.5, flats ±13.9: 0.4 clearance) stand 1.0 proud of the field, with 0.6 mm 3-segment edges. A slanted gap floor runs from the opening down to the device plane, 1.5 below the field edge. Slots get a 0.15 mm mouth chamfer, black walls down to a black box sheet, and brass leaves (0.4 thick) 3.0 mm in. Ground hole down: two slot "eyes" over an open "mouth" (the "surprised face").
- **5-20R.** A horizontal 6.4 × 2.0 arm at the neutral slot's centre, pointing to −x (away from hot).
- **Steel.** 0.76 mm sheet with a flat field at 4.70 and a quarter-round formed edge (r 1.5, 3 segments), a straight leg to the wall and 1.0 corners. Same device, 0.9 mm lower.
- **Cracked.** A 0.25 mm V split from the countersink to the right edge (zig-zag, exit z = 1.4). The lower side is stepped 0.2 mm, blended out over 6 mm and tapered in over the first 5 mm. A chipped lower-left corner, 5.37 × 4.16 mm, 3.43 deep: 3 bisected fracture facets plus a flat floor facet.
- **Jumbo.** 88.9 × 133.4. The §1.2 profile scaled ×1.161 in inset and height, so the crown is 6.5. Corner radius stays 2.0. Same openings, screw and device.
- **Screw.** #6-32 oval head: a sphere cap Ø 6.6 × 1.0 (sphere radius 5.945), 64 rim segments, and a flat-bottomed slot 0.8 × 0.6. The slot runs out through the dome where the dome is lower than its floor.
- **Bare (P2).** The device on its steel strap: 33.8 × 106.7, 0.9 thick, resting 1.0 mm off the wall on the box ears. Plaster-ear score notches at ±47, the empty tapped centre hole (Ø 3.5), two oval-head box screws in 3.8 × 6 slots (83.3 centres). Side terminals: brass binding heads on the hot side, aluminium on the neutral side, on 8 × 8 clamp plates. The nylon body runs 25 mm back. The 2 × 3 in steel box has a 1.6 wall; its front edge stands 1.0 proud, its back is 35 behind. A ragged dark sheet (0.3 mm proud, gap 4 ± 1 mm) stands in for the hole, and a torn lip (0.5–1.3 mm wide) slopes from the cut edge (0.34) to the paper (0.08). **Round 9:** the dark parts (slot walls, box sheet, gap sheet) moved from `Prop_PlasticBlack` to the existing matte `Prop_Rubber` (§6).
- **Wear attribute.** Every part carries `fr_wear` (BYTE_COLOR, face corner): R 0.85 on the field within 4 mm of an opening; G 0.9 on the outer edge crest; B 0.35–0.4 in cavities and the crack; G 0.6 on the fracture faces and the Bare lip. It survives the FBX round trip on all 9 assets. No shader reads it yet (P-3 / W2).
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
- LOD2 marks never stand proud of the device face;
- chip extent 4.5–6.0 × 3.5–4.8;
- Bare: ragged gap 2.5–5.5, strap envelope, towers inside the strap, box ≤ 35.

**Checks on the exported FBX, re-imported** (`W/o1/o1_verify.py` on the clone; result `W/o1/logs/verify_r10_clone.json`): **9 / 9 pass.**

| Check | Result |
|---|---|
| (a) Plate and device numbers | Sizes as §1; sidecar triangles = FBX triangles; ≤ 4 slots |
| Slot depth (front-facing ray to the first visible face) | 6.35 mm (Duplex, D20, Cracked), 5.45 (Steel), 7.0 (Jumbo, Bare). The plates stand 5.7–7.5 mm off a solid wall, so the slot floors stop 0.25 mm in front of it (issue 4) |
| Brass leaves | 3.0 mm below the face on every kit |
| (b) Surprised face | Reads at 0.30 m on every plate (`o1_front030_sheet.jpg`) |
| (c) Screw seat | Probes at r 3.52 mm in 4 directions hit the countersink at 5.577 vs a seat of 5.55 (Steel 4.677 vs 4.65; Jumbo 6.477 vs 6.45). The Cracked plate's +x probe lands in the split, as it should. The screw at its `screw_0` anchor, rolled 0 / 45 / 90°, sits flush and its slot reads (`o1_screws_0_45_90.jpg`, zooms from a 4800 × 2700 band render) |
| (d) Crack has no gap to the wall | 42,120 rays aimed into the split at ±40°: **0 reach the wall behind the plate**. Lowest hit: the groove floor at 2.82 mm. 12 rays near the edge leave past the plate outline and land on the wall beside it; that is not a gap |
| (e) Steel edge at 45° light | One thin bright line on the lit edges (`o1_rake030_sheet.jpg`) |
| (f) LOD pops (round 10) | Area-averaged face radiance vs LOD0 (linear EXR, troffer light, `W/o1/lod_measure.py`): Duplex LOD1 +1.1 %, LOD2 +1.9 %; Bare LOD1 −0.9 %, LOD2 −3.7 %. Plate: Duplex LOD2 +1.7 %, Bare LOD2 −4.4 % (Bare LOD1 is −4.3 %) |
| Era lock | No TR shutters (the slots are open), no USB, no screwless plate, no text, logo, UL mark or date anywhere in the meshes |

## 5. Every ESTIMATE that became a number

**§1.2 ESTIMATEs used exactly as the spec wrote them.** Corner radius 2.0; the thermoset profile and crown 5.60; hollow back rim 1.6 and cavity 3.5; stainless edge r 1.5; countersink Ø 7.4 at 82°; face proud 1.0; device plane 1.5 behind; slot sizes 2.0 × 8.5 / 2.0 × 7.0, ground U Ø 5.0; slot depth 7.0; leaves 0.4 at 3.0; 5-20R arm 6.4 × 2.0.

**O1's own numbers, where §1.2 is silent** (`outlet_common.py`, "O1 design numbers"):

| Number | Value | Why |
|---|---|---|
| Opening / face arcs | 64 per circle (22 segments per arc, 46 points per outline) | Spec §1.1 |
| Corner segments on the inner profile rings | 12 / 12 / 10 / 8 / 4 / 4 / 4 for radii 2.0 / 1.85 / 1.55 / 1.05 / 0.5 / 0.5 / 0.5 (stainless 12 / 12 / 12 / 6 / 4) | Keeps the chord ≤ 0.26 mm; pays for the 64-segment openings (−210 tris) |
| Inner-ring corner radius floor | 0.5 (stainless 0.3) | Rings inset past 2.0 would get a zero radius |
| Device plane | 1.5 below the field **edge** (5.50), so 4.00 (stainless 3.20) | Keeps the gap floor's slope even |
| Gap floor | a slant from the opening's lower ring (field − 0.5) down to the device plane | Closes the 0.4 gap |
| Seat | 0.05 below the field (5.55) | Inside the ±0.05 tolerance; the 0.4 mm countersink lip reads beside the head |
| Bore | Ø 3.6 at the cone bottom, with a black cap | Seen only when the screw is missing |
| Slot floor | stops 0.25 in front of the wall plane | The wall is solid in the game (issue 4) |
| Leaves | stop 0.5 short of each slot end, 1.2 tall (3.0 → 4.2 below the face) | |
| Field crown points | Steiner grid every 9 mm | |
| Hidden back | 4 segments per corner | Seen only through a tilt gap |
| Box sheet behind the device | 50.8 × 76.2, black, at the slot floor | The 2 × 3 in box |
| Stainless opening edge | 0.35 mm round, 2 segments | Punched sheet, not moulded; the spec's 0.5 / 3 is for thermoset |
| Ground U | 16 segments on the semicircle (32 per circle) | Budget; sagitta 0.012 mm = 0.03 px at 0.3 m (issue 3) |
| Countersink | 40 segments | Under the 64-segment screw head 94–96 % of the time |
| Crack | depth 0.55, blend 6.0, taper 5.0, zig-zag path, exit z = 1.4 | `outlet_duplex_cracked.py` |
| Chip | lower-left; planes (1.45, 0.35, −3.9), (0.30, 1.75, −3.3), (0.85, 0.95, −2.3); floor 0.3 → 3.75; cut zone 10.5 mm | Extent 5.37 × 4.16 × 3.43 |
| Jumbo profile | the §1.2 profile ×1.161 (6.5 / 5.6) in inset and height | |
| Screw dome | sphere radius 5.945 from Ø 6.6 and crown 1.0; flat-bottomed slot | |
| Bare | strap 33.8 × 106.7 × 0.9 at 1.0–1.9, r 1.5; ear notches ±47; box screws ±41.65 in 3.8 × 6 slots; centre hole Ø 3.5; body 68 long to −25; box 50.8 × 76.2 inside, wall 1.6, front +1.0, back −35; gap 4 ± 1; sheet 0.3; lip 0.5–1.3 wide, 0.34 → 0.08; terminal heads R 3.6, crown 0.9, band 1.2, slot 1.0 × 0.7, axis 1.5 behind the wall | |
| Bare dark slot | `Prop_Rubber` (existing; tint 0.02, smoothness 0.15) instead of `Prop_PlasticBlack` | Round 9: matte, so the gap and slots have no sheen at 0.3 m. Same render cost; R3 resolves it through `Surface()` |
| LOD1 | 2-step edge (profile points (0.95, 3.80) and (4.00, 5.50)), 4 segments per corner, 24-point faces, 0.5 mm slot insets, plain dark screw hole | Spec §1.1 table |
| LOD2 plate | 8-point outline at full height + one bevel ring | Spec §1.1 table |
| LOD2 faces (round 8) | each face a device-coloured 6-gon, a = 16.6, b = 16.5 (x ±16.6, z ±14.3), 712 mm², 0.7 below the face top | Keeps IG orange and ivory-on-Steel visible past 4 m; round 7 drew no face |
| LOD2 marks (round 10) | three dark marks per face, 0.3 mm in front of the 6-gon: blade quads 3.6 × 10.0 at x −6.35 and 3.6 × 8.5 at x +6.35 (z +3.0), a point-down ground triangle 7.9 × 7.9 at z −8.9; 98 mm² in all (slots + gap ring); 5 tris per face | Area-matched to LOD0 (face disc +1.9 %); replaces round 8–9's single 103 mm² dot, which packed into one 2 px blob, darker than LOD1 at 4 m |
| Bare LOD2 | face 6-gon ×1.044 per axis (775.8 mm², the LOD0 face area) as a prism down to the strap; marks scaled to 55 mm² (slots only, no gap ring) | Round 10: the plate-kit 6-gon left the Bare face −7.6 %; now −3.7 % |

## 6. What changed since the 2026-10-03 report

1. **Round 8 (written 2026-10-04, verified today): LOD2 faces.** Round 7's LOD2 drew each face as a 527 mm² dark 6-gon. Measured, that was −50 % face radiance at the 4 m switch (a dark pop), and the IG orange and Steel's ivory faces vanished. LOD2 now draws a device-coloured 6-gon per face with a small area-matched dark part.
2. **Round 9 (today 16:30–17:45).** Rebuilt all 7 modules and the LOD sets; re-ran every render. Bare: the dark parts moved to matte `Prop_Rubber`, and the LOD2 faces became prisms (a flat 6-gon read −21 % on a bare tower).
3. **Round 10 (this run): LOD2 marks.** The single dark dot per face was replaced by three slot marks (+2 tris per plate, LOD2 = 42). At 4 m the LOD1 → LOD2 switch no longer darkens the face (`o1_lod2_dot_vs_marks.jpg`). Bare's LOD2 face was area-matched (−7.6 % → −3.7 %).
4. **Screw close-up re-rendered** from a 4800 × 2700 band, so the 0 / 45 / 90° zooms show the slot, not pixels.
5. **Images rebuilt** under `research/outlets/images/` (the 2026-10-03 set was lost in the 2026-10-05 wipe), and the two verification slides placed in Figma (§9).
6. **No `.pyc` written.** Every build runs with `sys.dont_write_bytecode`. The two newer `.pyc` files in `assets/__pycache__/` (`outlet_box_common`, `outlet_handy_box`) belong to O3's build, not this one.

## 7. Issues and open items

1. **P-1 / P-1b (NEEDS APPROVAL, visual chat).** No `make_lods`, so each FBX is LOD0 only. The LOD1/LOD2 part sets are written, counted and measured, and turn on by themselves when `make_lods` exists. Convention as `../interactables/10_spec.md` P-1: `<NAME>_LOD0/1/2`, `lodDistances`, per-part `fr_lods`.
2. **P-4b (NEEDS APPROVAL).** `Prop_ThermosetIvory`, `Prop_NylonIvory` and `Prop_PlasticOrange` are registered with `kitlib.register_slot` only. `FrontRoomsRenderSetup` has no material for them yet. R3 maps them to the palette (plate / device materials) with fallbacks. A plain importer path would show missing materials until the three `SurfaceDef` lines land.
3. **64-segment rule vs budget.** Openings, faces and screw are ≥ 64. The ground U (32 per circle) and the countersink (40) are not; making them 64 adds about 180 tris per plate and pushes the Duplex past +15 %. Sagitta 0.012 mm (0.03 px at 0.3 m).
4. **Slot depth.** The spec says 7.0 mm, but a face 6.6 mm off a solid wall can only be 6.35 deep (Steel 5.45). Jumbo and Bare get the full 7.0.
5. **Bare kit size.** The spec's §1.3 size "106.7 × 76 × 8" reads as strap length × box height. The built envelope is 65.3 wide (ragged cut-out plus lip) × 106.7 tall (strap) × 8.0 proud, plus 35 behind.
6. **FLAG: the Bare gap reads grey, not a void** (VL078). The gap is a flat dark sheet in front of a solid wall. Even on matte `Prop_Rubber` it reads mid-grey under the troffer (`o1_bare_gap_pb_vs_rubber.jpg`). Only a real hole in the map wall would let the modelled box show (spec §8 item 7, map chat); then drop the sheet (`gapSheetH` in the sidecar). Bare is P2 and rare.
7. **Variant sidecars** carry the base slot names in `outlet` (kitlib limitation, no kitlib edit). Read `slots` for the real materials.
8. **Screw tags.** `Kit_OutletScrew` is tagged `outlet_screw`, not `outlet_receptacle`: it is shared by O2's plates and is not a receptacle.
9. **New: sidecar `placement` reads `"Floor"`.** kitlib derives `placement` from tags (`wall_unit` / `wall_decor` → Wall, else Floor), and the spec forbids those tags on outlets. If someone drops a `Kit_Outlet*` from the Level Designer's kit list, `FrontRoomsModuleEditing.NewProp` will stand it on the floor. Outlets are placed by the planner, so nothing breaks today. Smallest fix (needs visual-chat approval, kitlib is not mine): in `kitlib.export`, map `wall_flush` to a `"WallFlush"` placement (or have the Level Designer list skip tag `outlet`).
10. **5-20R arm size and direction** are still the spec's ESTIMATE; check them against a period photo (`media_candidates.md`).
11. **Preview colours only.** The renders linearise the spec hex colours; the real tint is a Unity look-dev task (L-1: about 11 % darker and warmer than the paper).
12. **Clone state.** At 18:43 the code stage started a Unity batch capture on `W/proj_outlet` (`FrontRoomsOutletCapture.RunBatch`) while this stage was writing the final FBX there. The FBX content is unchanged from round 9 (identical sidecars, same triangle counts), so the capture sees the same meshes; Unity may re-import them on its next start because the file times changed.
13. **Main state.** Main holds no `Kit_Outlet*` FBX. Codex's commits touched no outlet file (codex_audit `00_main_state.md`: "D3 outlets — not touched"); `20_findings.md` does not exist yet and no `10_review_*.md` names an outlet finding. Red's auto-commits picked up today's module edits; the last edits of this run (comments, the Bare LOD2 face, the LOD2 part name) are in the working tree. Nothing was merged into `Assets/`.

## 8. Previews and close-ups

Kit previews (`build_asset.py`, Cycles, 48 samples): `W/outlet_prev/g0/<NAME>_a.png` / `_b.png` for the 7 modules (round 9 build; LOD0 is unchanged since).

Close-ups, re-imported from the exported FBX with back faces culled as in Unity (`W/o1/o1_render.py`; renders in `W/o1/prev/r9` and `W/o1/prev/r10`). Kept copies in `research/outlets/images/`:

| Image | What | Slide |
|---|---|---|
| `o1_front030_sheet.jpg` | All 8 plate assets, front at 0.30 m, FOV 62 | VL078 `2798:6111` |
| `o1_rake030_sheet.jpg` | Duplex, Steel, Cracked, Jumbo, Bare under 45° raking light at 0.30 m | VL078 `2798:6112` |
| `o1_screws_0_45_90.jpg` | `Kit_OutletScrew` at the `screw_0` anchor, rolled 0 / 45 / 90° (zooms from a 4800 × 2700 band) | VL078 `2798:6113` |
| `o1_macro_cracked_bottomedge.jpg` | The cracked plate's chipped corner and bottom edge | VL078 `2798:6114` |
| `o1_macro_bare_lip.jpg` | The Bare kit's torn lip, gap and box edge | VL078 `2798:6115` |
| `o1_d150_d400_group.jpg` | Six kits (LOD0) at 1.5 m (×4) and 4 m (×8), eye 1.62 m, FOV 76, Level 0 paper | VL079 `2798:6128` |
| `o1_lod2_dot_vs_marks.jpg` | LOD1 \| LOD2 pairs at 4 m: round 9 (one dot) vs round 10 (three marks) | VL079 `2798:6129` |
| `o1_lodpop_sheet.jpg` | LOD0 \| LOD1 at 1.5 m, LOD1 \| LOD2 at 4 m (Duplex, Steel, Bare) | VL079 `2798:6130` |
| `o1_lod_sheet.jpg` | LOD0 / LOD1 / LOD2 part sets of Duplex, Steel, Bare | VL079 `2798:6131` |
| `o1_d150_group.jpg`, `o1_d400_group.jpg` | The two halves of `o1_d150_d400_group.jpg` | — |
| `o1_bare_gap_pb_vs_rubber.jpg` | Bare gap on `Prop_PlasticBlack` vs `Prop_Rubber`, head-on and at 45° | — (issue 6) |
| `o1_kit_previews.jpg` | The standard build stills | — |

## 9. Verification log

Placed 2026-10-07 in FRONTROOMS · VISUAL VERIFICATION LOG (`2595:6093`), in the rows' own cells (row 19); the cover VL000 was updated (N3 now `VL064, 078–079, 088–094, 134–135 · 12 checks`).

| VL | Frame | Check | Verdict | Statement |
|---|---|---|---|---|
| VL078 | `2798:6103` | O1 receptacles at 0.3 m | FLAG | All 9 hold at 0.3 m; the Bare gap reads grey, not a void. |
| VL079 | `2798:6120` | O1 plates at 1.5 m and 4 m | PASS | LOD2 faces within 2 % of LOD0 (Bare −4 %); LODs within ±15 %. |

Rows and numbers: `Documentation/VERIFICATION_LOG.md` §3 (task N3).

## 10. Scratch tools (persistent, `W/o1/`)

`build.sh` (official build wrapper), `o1_lods.py` (LOD part sets via a scratch `make_lods` stub), `o1_verify.py` (FBX checks), `o1_render.py` + `render_all.sh` (close-ups), `lod_measure.py` (LOD radiance), `sheets.py` / `sheets_lib.py` / `sheets_r10.py` (image sheets), `vl_layout.py` (slide slots). Module backups before each round: `outlet_*.r9dot.py.bak`, `outlet_*.r10marks.py.bak`.
