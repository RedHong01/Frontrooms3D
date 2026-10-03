# Build G1 — door construction: frames, leaves, hinges, closers

Status: built 2026-10-03 in the private clone `proj_int` (scratchpad). Nothing under `Assets/` of the real project was touched; Unity was not opened. The modules are new files in `Tools/Blender/frontrooms_kit/assets/` (`interact_door_*.py`, `interact_exit_device.py`). Spec: `10_spec.md` §1, §2.1, §2.3, §2.4, §9.1, §10.0–10.1.

Build command (every module, `--out-root` always set):

```
/Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup --python "<project>/Tools/Blender/frontrooms_kit/build_asset.py" -- <module> --out-root <scratchpad>/proj_int --preview-dir <scratchpad>/interact_previews/G1
```

Check scripts (scratchpad `g1/`): `wn_probe2.py` (task 0), `partcount.py`, `lod_metrics.py`, `verify_g1.py` (T1 rays, T2 swing, trim enclosure), `closer_check.py` (linkage + mesh), `p2_check.py` (exit device swing), `render_doors.py` / `scene_lib.py` (close-ups).

---

## 1. Assets and triangle counts

LOD0 and LOD1 are the sidecar numbers (`triangles`, `trianglesLod1`). LOD2 is not exported until P-1; the "projected" number drops the parts flagged `fr_lod2_drop` and collapse-decimates the rest to `LOD2_RATIO` (`g1/lod_metrics.py`). Budget = §9.1.

| Asset | Module | LOD0 (budget, Δ) | LOD1 (budget, Δ) | LOD2 projected (budget) | Slots (submesh 0 first) |
|---|---|---|---|---|---|
| `Kit_DoorFrame_Wood` | `interact_door_frame_wood.py` | 3,152 (2,800, +12.6 %) | 1,260 (1,100, +14.5 %) | 282 (250) | WoodWalnut, WoodOak, Brass |
| `Kit_DoorFrame_Steel` (+ `_Alu`) | `interact_door_frame_steel.py` | 3,866 (3,600, +7.4 %) | 1,430 (1,300, +10.0 %) | 308 (280) | SteelBrown, Rubber, Chrome, Aluminium |
| `Kit_DoorLeaf_Veneer` (+ `_Oak`) | `interact_door_leaf_veneer.py` | 2,464 (2,600, −5.2 %) | 886 (900, −1.6 %) | 122 (120) | Door_Veneer, Brass (Oak: WoodOak, Chrome) |
| `Kit_DoorLeaf_Steel` (+ `_PaintedMetal`) | `interact_door_leaf_steel.py` | 3,930 (4,200, −6.4 %) | 1,572 (1,500, +4.8 %) | 156 (150) | Door_Enamel, Aluminium, Chrome |
| `Kit_DoorCloser_Body` | `interact_door_closer_body.py` | 3,236 (3,000, +7.9 %) | none (no LODGroup until P-1) | 161 (150) | SteelBrown, Chrome |
| `Kit_DoorCloser_Arm` | `interact_door_closer_arm.py` | 1,112 (1,200, −7.3 %) | none | 54 (60) | SteelBrown |
| `Kit_DoorCloser_Forearm` | `interact_door_closer_forearm.py` | 940 (900, +4.4 %) | none | 56 (50) | SteelBrown, Chrome |
| `Kit_DoorCloser_Shoe` | `interact_door_closer_shoe.py` | 570 (500, +14.0 %) | none | 44 (40) | SteelBrown, Chrome |
| `Kit_DoorLeaf_Ward` (P2) | `interact_door_leaf_ward.py` | 4,930 (5,000, −1.4 %) | 1,972 (1,800, +9.6 %) | 196 (180) | WoodLaminate, Aluminium, Chrome |
| `Kit_DoorLeaf_SteelLite` (P2) | `interact_door_leaf_steel_lite.py` | 4,586 (5,200, −11.8 %) | 1,834 (1,900, −3.5 %) | 182 (200) | Door_Enamel, Aluminium, Chrome, Glass |
| `Kit_ExitDevice_Crossbar` (P2) | `interact_exit_device.py` | 3,948 (3,500, +12.8 %) | none | 200 (200) | Aluminium, Chrome |

Every count is inside ±15 % of §9.1. `interact_door_common.py` is the helper (no `NAME`, no `build`).

Every asset: `kit.no_collider()` (sidecar `noCollider: true`, zero colliders), no lights, ≤ 4 slots, tags `interactable` + `door` + one of `door_frame` / `door_leaf` / `closer` / `exit_device` + level, `kit.meta["lodDistances"]` (`[d01, d12, dcull]`, −1 = never cull; JSON-safe), `kit.meta["lodRatios"]`, an `fr_wear` colour attribute on every part (R hand grime, G edge wear, B cavity; white = none; W2/P-3 decides whether Unity reads it). LOD1 is set only on the frames and leaves. Closer parts and the exit device carry `kit.meta["motion"]`.

---

## 2. Task 0 — weighted-normal probe (§1.8)

Scratchpad `g1/wn_probe.py` and `g1/wn_probe2.py` (they import `kitlib` directly; nothing in `assets/`).

| Recipe | Result |
|---|---|
| `WEIGHTED_NORMAL` modifier on a freshly made part (flat-shaded, bevel modifier still pending), then `finish()` | **Lost / scrambled**: 33.8° max deviation from the intended normals. The custom-normal layer survives the join, but `shade_smooth()` + `set_sharp_from_angle()` re-interpret it in new spaces. |
| **Per part, in the module:** apply the part's own modifiers (bevel, booleans) → `shade_smooth()` → `set_sharp_from_angle(SMOOTH_ANGLE)` → add `WEIGHTED_NORMAL` (face area, weight 50, keep sharp) → `finish()` applies it | **Survives exactly**: 0.00° over 384 corners after `finish()`, after the FBX export + re-import, and on LOD0 after `make_lod1()`. |

So every G1 module uses that recipe (`interact_door_common.finalize()`), with no kitlib change: **P-2 is not needed.** The Unity importer imports normals (`FrontRoomsKitImporter.cs`: `importNormals = Import`), so the custom normals reach the game. The only condition: a module's `set_sharp_from_angle` angle must equal its `SMOOTH_ANGLE` (35° here). The LOD1 copy is decimated and gets kitlib's own re-smoothing; it does not keep the weighted normals.

---

## 3. What each asset is (build notes)

All door assets are in DOOR ROOT D (§1.2): origin at the hinge-jamb edge on the wall centre line at floor level, Unity +Z along the opening, +Y up, +X the swing/pull face S, −X the push face P. Anchors below are the sidecar values (Unity, asset-local), checked in the JSON.

### `Kit_DoorFrame_Wood` (L0-F)
- **Section:** walnut ranch casing with a back band, both faces. Profile (u, w) as §2.3: R 3 round-over, a ranch slope, a 1.5 mm quirk, R 3.5 back band. Swept as three mitred legs per face (jambs grain vertical, head grain along the opening, each leg on its own UV offset). Walnut linings: 2.5 mm skins at Z −0.0005→0.002 / 0.998→1.0005 and the head at 2.098→2.1005. Applied stop on the push side (35.5 × 16 mm, R 3 on the leaf-side arris, 1.5 mm chamfer on the room side), mitred at the head and **scribed onto the saddle's bevel**, so no gap opens under it. Oak saddle 0.152 × 0.012 with 1:2 bevels and 1 mm eased arrises.
- **Hinges:** brass frame halves of three butts at 0.254 / 1.0565 / 1.859. Each has knuckles 1/3/5 (22.4 mm with 0.4 mm gaps), domed button tips top and bottom, the frame leaf mortised flush into the lining (it fills a boolean mortise, so no coplanar faces), curl webs under its knuckles, and four slotted countersunk #12 screws whose slots continue 1 mm into the leaf plate.
- **Strike pocket:** a 1.6 mm ANSI pocket (0.032 × 0.124 at Y 1.000) for G2's `Kit_Lock_StrikeBored`, so its plate sits flush.
- **Asserts:** the casing rule (w ≥ 0.0215 over u ∈ [0.0015, 0.0735]: measured minimum 0.0221), envelope bounds (−0.105…0.105, 0…2.175, −0.075…1.075), stop and saddle boxes, and no frame vertex inside the closed leaf.
- **Anchors:** `strike` (0, 1.000, 0.998), `hinge_axis` (0.0295, 0, 0.0098) + `_dir`, `head_dust_a/b` (0, 2.098, 0.05/0.95), `threshold` (0, 0.012, 0.5).

### `Kit_DoorFrame_Steel` (+ VARIANT `Kit_DoorFrame_Steel_Alu`)
- **Section:** one closed 16 ga sheet (1.5 mm), swept continuously round three sides with hairline mitres. The path is P return → P face band (X −0.105) → P soffit → formed stop → rabbet soffit → S face band → S return. Outer bend radii are 3.0 mm convex and 1.5 mm concave (1.5 mm inside bends). The sheet's back clears the map trim face by 3.5 mm (asserted).
- **Cuts:** hinge preps are cut through the soffit and filled flush by chrome frame leaves. The mortise strike prep is a 0.032 × 0.200 hole centred Y 0.968, with a dark-bronze strike box 1.6 mm behind the face. A 1.5 mm sheet cannot hold a 1.6 mm recess, so the hole is cut through and backed by the box.
- **Hardware:** three rubber silencers (Ø 8 × 2.5) on the latch stop at Y 0.35 / 1.30 / 1.90, Z 0.988. Fluted aluminium saddle: 12 flutes, 0.8 mm deep, 6 mm pitch, a 12 mm centre land with three countersunk screws at Z 0.15 / 0.50 / 0.85. Chrome frame hinge halves, as on the wood frame.
- **Anchors:** `strike` (0, 0.968, 0.998), `strike_bored` (0, 1.000, 0.998), `hinge_axis` + `_dir`, `head_dust_a/b`, `threshold`, `closer_shoe` (0.125, 2.130, 0.100), `exit_sign_p` (−0.080, 2.280, 0.500).

### `Kit_DoorLeaf_Veneer` (+ VARIANT `Kit_DoorLeaf_Veneer_Oak`: WoodOak + Chrome)
- **Slab:** lofted 44 mm. 1.5 mm two-segment arrises on every face edge, including top and bottom. The latch stile's arrises open to R 3 between Y 0.85 and 1.20 (hand wear). The hinge edge is square. The latch edge has the 3° bevel (S 0.995, P 0.9927). The edge band's glue lines are 0.3 mm V-grooves at X ±0.0185 on both edge faces. Metre UVs with vertical grain (the 2.08× stretch is gone).
- **Hardware:** bored-latch faceplate (0.0286 × 0.0572 × 0.0015) mortised flush on the bevel at Y 1.000, with a latchbolt opening (0.011 × 0.021) and two slotted screws. Brass leaf halves (knuckles 2/4, plates mortised into the hinge edge, four screws each).
- **Anchors:** `rose_s/p` (±0.022, 1.000, 0.920), `latchbolt` (0, 1.000, 0.9939) + `_dir` (+0.10 Z), `latch_edge_bottom/top` (0, 0.015/2.095, 0.9939), `damage_latch` (0, 1.10, 0.9939), `hinge_axis` + `_dir`, `closer_mount_s` (0.022, 2.0275, 0.545).
- **LOD1:** the leaf drops its flush hinge leaves and faceplate. Their 1.5–3.4 mm recesses are sub-pixel beyond 4 m. Without the drop, kitlib's thin-part protection would leave only the knuckles to decimate, and they would collapse.

### `Kit_DoorLeaf_Steel` (+ VARIANT `Kit_DoorLeaf_Steel_PaintedMetal`)
- **Slab:** square edges with 1.5 mm arrises and a 0.5 mm seam groove at X 0 on both edges. 1 mm recessed channel lines at Y 0.025 / 2.085. The 3° bevel.
- **Kick plates:** stainless, both faces, 1.27 mm, 45° edges, Y 0.018→0.272. The push face spans Z 0.0305→0.9695, clear of the stops (asserted); the pull face spans 0.024→0.976. Six slotted oval-head #6 screws each, built directly as two half-domes with a real slot.
- **Armor front:** 0.032 × 0.2032 × 0.003, mortised on the bevel at Y 0.968, with latch (0.9365) and deadbolt (1.000) openings 0.0135 × 0.031 and two slotted screws.
- **Hinges and bores:** chrome leaf halves. The cylinder and knob bores are not modelled; G2's escutcheon covers them.
- **Material:** `Door_Enamel` is registered with the almond preview colour. Until `Resources/Surfaces/Door_Enamel.mat` exists (P-4), Unity uses the FBX's own almond material. The `_PaintedMetal` variant maps it to the existing `Painted_Metal`, the spec's named fallback.
- **Anchors:** `escutcheon_s/p` (±0.022, 0.968, 0.920), `sign_s/p` (±0.022, 1.524, 0.500), `tagplate_s/p` (±0.022, 1.000, 0.790), `kick_s/p` (±0.0226, 0.145, 0.500), `latchbolt` (0, 0.9365, 0.9939) + `_dir`, `deadbolt` (0, 1.000, 0.9939) + `_dir`, `latch_edge_bottom/top`, `damage_latch`, `hinge_axis` + `_dir`, `closer_mount_s`.

### Closer: `Kit_DoorCloser_Body`, `_Arm`, `_Forearm`, `_Shoe` (PART frames)
- **Body:** origin at the centre of the back face, placed at `closer_mount_s` with `Euler(0, 90, 0)`. 290 × 65 × 50. A full cover with a seam groove 10 mm off the door (top and bottom), cast bullnose end caps with a 0.6 mm lip, two slotted valve screws on the hinge end, a socket adjusting plug on the latch end, a cast spindle boss, a Ø 12.7 spindle (door Y 2.060→2.075), and the bottom spindle cap. Anchor `spindle` (0.025, 0.0405, 0.040) = door (0.062, 2.068, 0.520), asserted.
- **Arm:** origin on the spindle axis at the arm's underside (door 2.068). Bar 10 mm thick, 20→16 mm wide, door Y 2.070–2.080. Hub Ø 30 with a hex arm bolt (top at door 2.089, under the 2.098 head lining). Elbow boss with its rivet under. Anchors `spindle` (0, 0, 0), `elbow` (0.240, 0, 0), `elbow_top` (0.240, 0.012, 0).
- **Forearm:** origin at the elbow on top of the arm (door 2.080). The forged eye climbs in a gooseneck to the Ø 16 tube within 0.08 m, then a hex lock nut, the Ø 12.7 adjusting rod, and the shoe-end fitting with a vertical stud. Anchor `shoe_end` (0.260, 0.050, 0) = the shoe pivot at door 2.130. The underside beyond 0.08 m stays ≥ door 2.1105, asserted, so it passes ≥ 15 mm over the 2.095 leaf top.
- **Shoe:** origin = the shoe pivot, placed at the frame's `closer_shoe` with `Euler(0, 90, 0)`. The back plate lands on the steel face band (door X 0.105), with a 6 mm ear round the pivot, a slotted pan-head pivot screw, and two plate screws.
- **Motion metadata:** `kit.meta["motion"]` on each part gives the solver contract: a = 0.24, b = 0.26, the elbow root farther from the leaf's S plane, and the elbow positions.

### P2
- **`Kit_DoorLeaf_Ward`:** the veneer leaf's slab and chrome hinge halves in WoodLaminate, with no latch.
  - Push face: a stainless armor plate Y 0.018→0.831 (deviation below), a rounded push plate (R 6) Y 0.850→1.256, Z 0.820→0.922.
  - Pull face: a kick plate and a Ø 25 D-pull on 0.254 centres at Y 1.000, Z 0.920, projection 0.064, on Ø 30 roses.
  - Anchors `pull_s`, `push_p`, `armor_p`, `kick_s/p` + the leaf set.
- **`Kit_DoorLeaf_SteelLite`:** the steel leaf with a 0.102 × 0.635 lite centred (Y 1.575, Z 0.755).
  - Cut-out 9.5 mm larger each side, 6 mm `Prop_Glass` slab (`meta["glassSlab"]` = [0.119, 0.652, 0.006]).
  - 45° bevelled low-profile lite kits on both faces (19 mm face, 2.5 mm proud), six slotted screws on the S kit.
  - Asserted clear of the sign, escutcheon and latch stile. Anchor `lite_centre`. Gameplay flag in `meta["gameplayNote"]`.
- **`Kit_ExitDevice_Crossbar`:** origin at the latch-end case mount, door (−0.022, 1.000, 0.900), placed on the P face with `Euler(0, −90, 0)` and scale (−1, 1, 1).
  - Cases 75 × 120 × 95 at part X 0 and 0.800 (door Z 0.900 / 0.100), cast with R 12 front edges, bullnose ends and a cover seam 20 mm off the door.
  - Ø 32 chrome bar, centre 70 mm off the face; chrome collars; four slotted cover screws.
  - 0.095 proud, asserted. Anchors `latch_case`, `hinge_case`, `bar_centre`, `push_point`.

---

## 4. Self-checks

(a)–(d) run on the **exported FBX meshes** re-imported into Blender (`g1/verify_g1.py`), with a stand-in wall (|X| ≤ 0.08 round the 1.0 × 2.1 opening), the map's trims (0.07 × 0.20 jambs, 1.14 × 0.07 head) and a floor. Both handings are tested by mirroring X.

### (a) T1 gap rays (both handings)
Every ray line crosses the wall centre plane X = 0. So rays are cast through sample points in the four perimeter gaps at X = 0, over a grid of directions. Each segment runs from X −0.15 to X +0.15, with a hit anywhere counting as blocked.
- Jambs: azimuths ±25° (beyond ±7.8° the plan section proves the leaf body blocks) × all elevations.
- Head and floor: all azimuths × elevations ±25°.
- Wood LOD0 ran the full 0.5° grid with dense sampling at every hinge, the strike and the silencers.
- Steel and both LOD1s ran a 1° grid with lighter sampling (`--lite`; every hinge edge and knuckle joint, the strike zone, the silencers). The machine was at load average ~700 from the other groups' renders.

| Door | Hinge jamb | Latch jamb | Head | Floor |
|---|---|---|---|---|
| L0-F wood (LOD0) | 14,902,449 rays, **0 through** | 20,776,407, **0** | 2,610,648, **0** | 2,610,648 rays, 54,064 through, all at **elevation ≤ 1.5°** |
| L0-K / OF-K steel (LOD0) | 862,920, **0** | 1,138,320, **0** | 220,320, **0** | 220,320 rays, 5,400 through, all at **elevation ≤ 2.0°** |
| wood LOD1 | 862,920, **0** | 1,138,320, **0** | 220,320, **0** | 5,428 through, ≤ 2.0° |
| steel LOD1 | 862,920, **0** | 1,138,320, **0** | 220,320, **0** | 5,400 through, ≤ 2.0° |

- **Jambs and head: zero see-through.** Every path is L-shaped through the stop, as §1.4 says.
- **Floor:** the 3 mm gap over the saddle is see-through only for sightlines within 1.5–2.0° of horizontal. The saddle's flat runs 52 mm each side of the leaf, which tightens the spec's 3.9° estimate; the steel saddle's 0.8 mm flutes allow the extra half degree. From the 1.62 m eye that means standing ≥ 46 m away; from a 0.55 m duck, ≥ 16 m. The spec accepts this period-true ⅛" gap (§1.4); no sweep was added.
- **LOD1 stays gap-free.** The envelope parts are protected from kitlib's decimation.

### (b) T2 swing (0–95° in 1° steps, both handings, BVH overlap + nearest-distance)
| Door | Overlapping triangle pairs | Min clearance to the stops | Rest of the envelope (linings, casing, saddle, wall) | Silencers (by design) | Hinge (by design) |
|---|---|---|---|---|---|
| L0-F wood | **0** | **3.00 mm** | **2.83 mm** | — | 0.10 mm |
| L0-K / OF-K steel | **0** | **3.04 mm** | **2.83 mm** | 0.50 mm at 0° | 0.10 mm |
| RN-F ward in steel frame | **0** | **3.00 mm** | **2.83 mm** | 0.50 mm at 0° | 0.10 mm |
| RN-K lite in steel frame | **0** | **3.04 mm** | **2.83 mm** | 0.50 mm at 0° | 0.10 mm |
| wood LOD1 | 1,624, all at the hinge barrels | 3.00 mm | 2.73 mm | — | see below |
| steel LOD1 | 4,869, all at the hinge barrels | 3.04 mm | 2.80 mm | 0.50 mm | see below |

- The 2.83 mm envelope minimum is the leaf's hinge-mortise corner passing knuckle 1 at 93°.
- The silencers stand 2.5 mm off the 3 mm stop gap and touch nothing, as on a real frame.

- **Hinge at LOD0:** the 0.10 mm figure is the leaf's S face passing the frame knuckles. A butt hinge's door face is tangent to the knuckle circle by construction. The knuckles are R 0.0074 (spec 0.0075), so it never touches: 0.1 mm, not coplanar, so no z-fight. The knuckle-to-knuckle end gaps are 0.4 mm.
- **Hinge at LOD1:** kitlib's collapse decimation is the only thing that may thin the knuckles, and it moves barrel vertices out to R 0.0077 (wood) / 0.0083 (steel). So at LOD1 the barrels cross the leaf face by up to 0.8 mm. That is render-only, never coplanar, and sub-pixel at LOD1 distances (today's importer switches a 2.2 m frame at ~14 m, or ~28 m on desktop Ultra).
  - Option, a one-line change per module: `kit.lod1_drop()` the knuckles too. That removes the crossing, but the LOD1 counts then fall to about −30 % of §9.1.

### (c) Closer linkage (`g1/closer_check.py`)
- **Analytic, 0–95° in 0.1° steps:** it always solves. The minimum reach margin is 57 mm (never near the straight-arm singularity). The elbow stays on the room side, at least **0.089 m** from the leaf's S plane, with **elbow X ≥ 0.222**; the requirement is ≥ 0.14.
- **Elbow (plan X, Z):**
  - 0° (0.2221, 0.3412), as the spec;
  - 45° **(0.3848, 0.1093)**: the spec prints 0.101; its own numbers give 0.109;
  - 95° (0.2966, −0.0953), as the spec.
- **Meshes, 1° steps** (arm and forearm placed by the solve, body on the leaf, shoe on the frame, steel leaf in steel frame): **zero overlaps** for every pair. Minimum clearances:

| Pair | Min clearance |
|---|---|
| Arm–leaf | 25.0 mm |
| Arm–frame | 8.8 mm (hub bolt under the head lining) |
| Arm–wall | 10.8 mm |
| Arm–body (spindle excluded) | 6.0 mm |
| Forearm–leaf | 15.5 mm (over the leaf top at 95°) |
| Forearm–frame | 11.0 mm |
| Body–frame | 23 mm |
| Shoe–leaf | 15 mm |

### P2 swing with face hardware
- **EX-F:** `Kit_ExitDevice_Crossbar` on the P face of `Kit_DoorLeaf_Veneer_Oak` (§1.2 face rule: `Euler(0, −90, 0)`, scale (−1, 1, 1)) in `Kit_DoorFrame_Steel_Alu`, 0–95°, both handings. **0 overlaps**; minimum 36.4 mm to the frame (at 5°); nothing within 50 mm of the wall (`g1/p2_check.py`).

### (d) Casing encloses the map trims
1,488 sample points just outside every exposed face of the trim stand-ins, 160 rays each, 5 m long:
- wood: **0 visible** from the room (both handings);
- steel: **0 visible** (both handings).

### (e) Renders (Cycles, scratchpad `interact_previews/G1/`; all reviewed)
- **Both faces at 1.5 m:** `G1_wood_s15.png`, `G1_wood_p15.png`, `G1_steel_s15.png`, `G1_steel_p15.png`, `G1_ward_s15.png`, `G1_ward_p15.png`, `G1_lite_s15.png`.
- **Latch edge at 0.3 m:**
  - `G1_wood_latch03.png`: faceplate, slotted screws, latch opening;
  - `G1_steel_latch03.png`: armor front, two bolt openings, edge seam, fluted saddle.
- **Knuckles at 0.3 m:**
  - `G1_wood_knuckle03.png`, `G1_steel_knuckle03.png`;
  - `G1_wood_hingeopen.png`, `G1_steel_hingeopen.png`: the door at 90°, both mortised hinge leaves, real slotted screws at random angles, knuckle joints, button tip.
- **Other close-ups:** `G1_wood_head03.png` (casing mitre, quirk, back band), `G1_lite_lite03.png`.
- **Open 45° and 95°:**
  - doors: `G1_wood_open45.png`, `G1_wood_open95.png`, `G1_steel_open45.png`, `G1_steel_open95.png`;
  - closer: `G1_closer_Kit_DoorLeaf_Steel_00.png`, `_45.png`, `_95.png` and `_closeup.png`.
- **P2:** `G1_exit_device_00.png`, `G1_exit_device_60.png`, `G1_exit_device_closeup.png`.
- **Kit turntables** (`build_asset.py`): `<Name>_a.png` and `<Name>_b.png` for all 11 assets.
- **What the review changed:**
  - the arm's bar top was coplanar with the elbow boss (a dark z-fight patch), so the bar is now 0.5 mm thinner each side;
  - the exit-device cases got a cover seam;
  - the closer body's front radius dropped to 6 mm, and its thread grooves were removed (budget);
  - the lock nut was turned so its flats face up and down;
  - both screw types are built directly, after a boolean version cost 97–171 triangles per screw.

---

## 5. Deviations from the spec (with reasons)

1. **Knuckle R 0.0074, not 0.0075.** The A2 axis is exactly 7.5 mm off the leaf's S face, so an R 0.0075 frame knuckle would rub the leaf face all the way round. 0.1 mm clearance, invisible.
2. **Hinge-leaf plates span X −0.016 → 0.022** (the frame leaf runs on to 0.030 under its barrel), **not −0.024 → 0.022.** At −0.024 the door half would stand 2 mm outside the 44 mm leaf's push face. −0.016 gives the period backsets: 8 mm from the stop on the frame, ~6 mm from the push face on the door (`02` §5.1). Both leaves are 38 mm plus the knuckle.
3. **Segment counts are sized by silhouette error at 0.3 m (1,600 px/m), not a flat ≥ 48.**
   - Values: knuckles 32 (0.035 mm off a circle = 0.06 px), button tips and screws 16 (≈ 0.15 px), roses and collars 32–64, closer hub and bosses 32, spindle 48, knob-sized parts n/a.
   - Cost of 48: on all 9 frame knuckles plus tips and screws, the frames would land 30–45 % over §9.1 with no visible change.
   - The constants `KNUCKLE_SEGS`, `TIP_SEGS` and `SCREW_SEGS` in `interact_door_common.py` are the one place to change them.
4. **Veneer edge bands** are modelled as the band's 0.3 mm glue lines (V-grooves at X ±0.0185 on both edge faces) in the slab section, not as separate meshes. The edge faces already get vertical grain from the box projection, and separate pieces would have added coplanar seams on the faces.
5. **Wood frame strike pocket added.** It is the ANSI 0.032 × 0.124 pocket at Y 1.000, 1.6 mm deep. The spec says the bored strike is "mortised flush" but lists no pocket for this frame.
6. **Steel strike prep is cut through the 1.5 mm sheet with a strike box 1.6 mm behind**, not a 1.6 mm recess.
7. **Closer body front radius 6 mm.** The spec's spindle sits 10 mm from the body front (door X 0.062 in a body ending at 0.072), which leaves no room for a larger radius under the spindle boss and cap.
8. **Ward armor plate 32" (top 0.831), not 34" (0.880).** The spec's push plate starts at 0.850, so a 34" armor plate would run 30 mm under it.
9. **Ward D-pull projection read as 0.064 overall** (bar centre 0.0515 off the face), so it stays inside the 0.065 proud rule.
10. **Exit device is one static mesh.** The bar's 12 mm push travel needs the bar as its own renderer (open item 5). `meta["motion"]` documents the travel.
11. **Extra VARIANT `Kit_DoorLeaf_Steel_PaintedMetal`.** It is the spec's named fallback for `Door_Enamel`, usable today.
12. **LOD1 ratio** is 0.37 on the steel frame and 0.36 on the veneer leaf (spec 0.40), so the LOD1 counts land on §9.1's LOD1 budgets. The frame and leaf envelopes (casing, lining, stop, saddle, slab, kick plates) are added to kitlib's `fr_lod_keep` group, so **LOD1 never shrinks the gap-closing outline**. LOD1 T1 is gap-free at every jamb and the head (§4 (a)).

---

## 6. ESTIMATEs that became numbers

- **Knuckles:** Ø 14.8 mm, 22.4 mm tall, 0.4 mm gaps.
- **Hinge pin button tips:** Ø 9.5 × 3.5 mm, domed.
- **Hinge-leaf width:** 38 mm.
- **Screw heads:**
  - #12 flat Ø 9.5 (hinges); #8 flat Ø 7.6 (faceplate, armor front); #8 flat Ø 8.0 (saddle);
  - #6 oval Ø 6.8 with a 1.2 mm dome (kick, armor and push plates); Ø 6.8 flat (shoe); Ø 5.5 (valve screws, lite kit);
  - slot 0.13·d wide; slots 1 mm deep in flat heads (cut into the plate), about 0.7 mm in ovals; random slot angle.
- **Silencers:** domed.
- **Steel frame bends:** outer radii 3.0 / 1.5 mm.
- **Saddle flutes:** 3 mm wide.
- **Closer:**
  - body cover seam 10 mm off the door; caps 11 mm with a 0.6 mm lip; spindle Ø 12.7 (½");
  - arm bar 9 × 20→16 mm (0.5 mm inside the 10 mm bosses); hub Ø 30 × 16; elbow boss Ø 22;
  - forearm tube Ø 16, rod Ø 12.7, nut 19 mm across flats; stud boss Ø 18;
  - shoe plate 30 × 40 × 4, ear R 11 × 6;
  - the arm lengths stay the spec's a = 0.240, b = 0.260 (UNVERIFIED against an LCN 4010 template).
- **Ward:** push plate corner R 6, D-pull bend R 20, roses Ø 30 × 3.
- **Lite kit:** 2.5 mm proud, pocket 9.5 mm.
- **Exit device:** cases R 12 front edges; bar centre 70 mm off the face; collars Ø 40 × 8.

---

## 7. Open items

1. **Steel frames under free members** (OF-F, EX-F) carry the 0.200 mortise strike prep at Y 0.968, which the free door's 0.124 bored strike at 1.000 does not cover.
   - Recommendation: the facade puts `Kit_Lock_StrikeMortise` on every steel frame. The bored latch at Y 1.000 enters its upper (deadbolt) opening, and the lower opening reads as a frame prepped for a mortise lock, which is plausible.
   - Alternative: G2 adds a 0.200 bored strike.
2. **Strike dust boxes (G2) show at most ~2 mm deep.** The map's wall end face and trims sit at Z 1.0, 2 mm behind the latch lining. Anything deeper is hidden until the map skips its door trims (§6.5 item 9).
3. **Jolts on the `Leaf rig`** (unlock 1.5 mm, Relay blows 4–8 mm) move the leaf's knuckles 2/4 off the frame's 1/3/5 by the same amount. Keep jolts ≤ ~2 mm, or make them a small rotation about the A2 axis instead of a translation (visual chat, §6.3 `FrontRoomsDoorRig`).
4. **Small parts have no LOD1** (closer, exit device) until P-1. Their `LOD1_RATIO` / `LOD2_RATIO` / `LOD_DISTANCES` are declared.
5. **Exit bar push:** needs `Kit_ExitDevice_Crossbar` split into cases plus a bar asset (a new module; not in this group's file list).
6. **Run/Exit members are P2.** Readability in Run and Exit is UNVERIFIED (T6), and no map door uses them yet.
7. **The RT glass track (§7, Red's relayed request):**
   - every G1 asset puts its dominant slot first (§7.3 item 2: submesh 0 = walnut / dark bronze / veneer / enamel / laminate / aluminium), so G14's submesh-0-only tracing shows the right material;
   - frames and leaves export `_LOD0` + `_LOD1`, so G14-K1 (register LOD0 only) applies to them;
   - mirrored doors are negative-scale instances (G14-K4).
8. **`Door_Enamel` does not exist in Unity yet** (P-4). Until it does, the steel leaves render with the FBX's own almond material, or use `Kit_DoorLeaf_Steel_PaintedMetal`.
9. **LOD1 hinge barrels** cross the leaf face by up to 0.8 mm (§4 (b)). Accepted as sub-pixel; the alternative is in §4 (b).
10. **Spec §2.4's 45° elbow** reads (0.385, 0.101); the spec's own linkage gives (0.385, 0.109), and the arm's `meta["motion"]` carries the computed value.
11. **Tags:** VARIANTS share the parent's sidecar meta. `Kit_DoorLeaf_Veneer_Oak` is therefore tagged `lobby` + `office`, and `Kit_DoorFrame_Steel_Alu` is tagged `frame_steel`. For sound (`doorType`), `meta["doorType"]` (wood / hollow_metal) and `meta["frameType"]` (wood / steel) are written; the Alu variant reports "steel".
