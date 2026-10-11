# 21 — Direction C: why the break structure reads fake (diagnosis, DB1)

Status: **DIAGNOSIS ONLY, 2026-10-08.** It answers Red's note on `images/db_C_L0F_burst_S4_V1_LA.jpg`: the broken structure does not look real enough. No file was rendered, rebuilt or changed. This file is the only thing written under `Frontrooms3D`. It adds no new images, so it needs no VL slide.

The measurements were made read-only from:
- crackgen's maps (`$W/door_break/C/maps/L0F_burst/`);
- the published finals in `images/`;
- the saved stage model `models/DB_C_L0F_burst_S4.blend` (opened in Blender, never saved).

The scripts and their JSON output are in `$W/door_break/C/look/diag2/` (§6). `$W` = `/Users/redwang/FrontRoomsVisualWork`. Line numbers refer to `$W/door_break/C/scripts/crackgen.py` and `scene.py` as of 2026-10-08 01:39.

Direction C's idea stays: the giant crushes the doorway, the wall gives, and the damage reads at 12 m. This file is about the **break structure** only, which has to become physically real for a 1990 US interior partition.

---

## 0. Short answer

- **The root cause is the brief, not a slip.** `10_design.md` §8.3 asked for "the most literal glass read" ("like a pane cracking around the door"). Its build notes asked for a crack generator "like the glass's crack graph": forks at p = 0.3, later cracks ending on earlier ones. `crackgen.py` built exactly that, and `20_dir_C.md` §1.3 says so in its title: "the glass rule on the wall plane". Every layer was then drawn to support that read: ink-dark lines, a mosaic of free plates, and paper treated as a decal. **Fixing it changes the §8.3 build notes**, so Red has to agree to that. The staging is kept: damage pops further on every blow, and each stage only adds.
- **Five measured faults carry most of the fake read** (details in §2 and §4):
  1. **The cracks are ink.** The map paints a near-black albedo core (base colour 0.010 linear) about 4.75 mm wide with a 13 mm halo, even 0.6 m or more from the corners on intact paper. Rendered: **9.0 mm wide (2.5 px at V1), 58 % darker than the paper beside it**. A real hairline that far from the load is 0.1–0.5 mm, which is 0.03–0.13 px at V1: invisible.
  2. **Every crack is a through-gap at least 3 mm wide.** Plates are cut on a 3 mm raster, so even the far cracks are 3.3–3.8 mm open gaps, and near the corners the gaps are 32 mm wide (median at S4). The FINAL wall is a mosaic of 40 free plates whose edges step every 3 mm, which reads as a "rope" or "comb" edge in close-ups.
  3. **The holes are black cards.** 50 % of the V1 rays through the S holes end on the render-only plenum box (albedo **0.02**). Another 35.5 % end on the back of the far board, inside the hole's own shadow. Rendered median sRGB is (16, 13, 9): **−5.5 stops** below the wallpaper. The rims are square-cut 12.7 mm, which is **1.3 px at V1** and 0.07 px at V6. The ring of board round each hole is painted as torn paper, not white gypsum.
  4. **The torn paper is camouflage.** A soft two-tone noise (scale 24 on a stretched UV) puts **57 % kraft brown** over a "face paper" whose albedo (linear lum 0.43) equals the yellow print's (0.42). Rendered, the torn zone is **0.6 stop darker** than the paper beside it and greyer. Its edge is a sticker outline: a 1–2 px pale line plus a 2 px shadow, with no lift, no fibre and no white backing.
  5. **The debris reads as confetti.** Chunks are 13 mm plates with stair-stepped rims and camouflage faces, scattered a median 0.64 m from the wall. Crumbs are 12–27 mm pebbles from 10 meshes. Floor strips zigzag 9–29 mm sideways per section because of a per-section random offset (`scene.py` L1740), so they look like lightning bolts. The powder peaks **0.45 m out from the wall** (2.0× brighter carpet) instead of at the wall base.
- **The physical target** (§1): a stud partition fails along its **framing and sheet layout**, not as a spiderweb:
  - one or two diagonal cracks from each head corner;
  - straight hinge lines at the king-stud edge and over the header;
  - screw pops on the stud lines;
  - a tape joint if one sits over the door;
  - panels 150–400 mm across that step out of the plane and break out with crumbled white edges, still hanging on their face paper.

  The vinyl wallcovering bridges the fine cracks: they show as puckers, not as lines. Where it does tear, the tear is straight-ish, with a light backing edge and a lifted lip. At 12 m the read must come from those **stepped panels, light tears, holes that show studs, and the dust**, not from drawn lines.
- **Order of work** (§5): F1–F3 (a new drywall fracture rule with no ink) → F4 + F12 (holes, framing, fasteners) → F5–F7 (paper layers, lifted edges, flaps) → F8–F10 (debris, powder, dust) → F11, F13–F15.

---

## 1. The physical target: a 1990 US interior partition at a door

Sourced in `02_real_door_failure.md` §1.1, §2.3 and §7, or standard construction practice. Lines marked ESTIMATE are reasoned from mechanics and still need a photo (§1.2).

**Build (outside in, S face):**
1. **Vinyl wallcovering.** In 1990 commercial work this was a paper- or fabric-backed vinyl, 54" (1.37 m) wide. The print layer is a tough, slightly sheened vinyl film. The backing is off-white paper or a woven or non-woven scrim. It is pasted over a primer/size.
2. **Gypsum board,** 1/2" (12.7 mm) or 5/8" (15.9 mm). Sheets are 4 ft (1,219 mm) wide, hung vertically or horizontally. Around a door the sheet is L-cut at the head corner so that no joint lines up with the jamb. The face paper is ivory or manila and made of several plies; the inner plies are greyer and fibrous. The core is white to light grey, porous and chalky. The back paper is grey kraft.
3. **Fasteners.** Screws (or nails on wood framing) along every stud line, roughly 200–400 mm apart vertically (ESTIMATE).
4. **Framing.** In a 1990 office, steel C-studs (25 ga, galvanised, 41 mm flanges, a track header) were usual behind a hollow-metal knock-down frame (`02` §1.1, L0-K). Wood studs (38 mm faces, a doubled header) are also possible behind a wood frame (`02`: "ESTIMATE: Office partitions as steel studs, Level 0 as wood or steel studs"). Studs are at 16" (406 mm) centres, with king and jack studs, a header and cripples over the door.

**How it fails when the frame is driven toward S and racked (ESTIMATE, mechanics):**
- **Head corners.** The re-entrant corner concentrates stress. One or two diagonal cracks run up at about 40–60°, widest at the corner (a few mm) and tapering to hairlines within 0.3–0.6 m. This is the one part of the current graph that is right (D1).
- **Hinge lines.** The board is screwed to framing that moves unevenly. It breaks over the edge of the king stud (a vertical line) and over the header or top track (a horizontal line). These lines are straight and meet at right angles, so the broken pieces are panels, not wedges.
- **Screw pops and pull-through.** Along the stud lines nearest the frame, the board pulls over its screw heads. Each pop is a round torn-paper disc about 15–25 mm across, raised 1–3 mm, with the screw head in a small crater.
- **Joints.** If a vertical tape joint sits over the door, it opens as one straight line from the head to the ceiling, with the tape's paper edges torn.
- **Breakout.** The panels between the hinge lines tilt out (out-of-plane steps of 10–40 mm that cast shadows), then break out on the tension face. The gypsum crumbles along each break, so a white band of exposed core 5–30 mm wide shows round every hole. The face paper holds pieces hanging by flaps. The hole shows studs, the header and the back of the far board (grey kraft with screw points).
- **The wallcovering** bridges hairlines; they show as diagonal puckers or wrinkles from the corner, not as lines. Over wider cracks it either stretches (creases) or tears. A tear is straight-ish with corners. It exposes the off-white backing or the drywall face paper, the edge lifts 1–5 mm, and fibres show. A torn-off flap is triangular (its tear lines converge), droops under gravity, and carries paste, face paper and gypsum on its back.
- **Debris.** Most of the mass lands within about 0.3 m of the wall base: pieces fall almost straight down and break again on the floor. Sizes run from 300 mm panels down to 1–5 mm grit and powder. The powder is densest against the cove base and fans out. Dust settles on every ledge (the casing head, the cove base, the hinge knuckles, the leaf's top edge) and hangs in the air for tens of seconds (ESTIMATE).

### 1.2 Photos needed before the rebuild (not searched or downloaded; needs Red's approval)

Under the research rule, these frames have to lead with real photos. `media_candidates.md` §20 already lists `dc01`–`dc04`: the corner crack, plaster above a door, and lay-in tiles. Five more wishes:

| Proposed filename | Search | What it proves |
|---|---|---|
| `dc05_drywall_broken_out_at_door_frame.jpg` | forcible-entry training, "wall breach drywall studs", Commons "damaged drywall" | Panels bounded by stud lines, the white crumbled core round each hole, studs and back paper visible inside |
| `dc06_gypsum_debris_on_carpet.jpg` | "drywall debris pile", "demolition drywall dust floor" | The debris spectrum and where the powder lies (at the wall base) |
| `dc07_torn_vinyl_wallcovering_layers.jpg` | "torn vinyl wallcovering drywall paper", "wallpaper torn drywall face paper" | Layer colours at a tear: backing, face paper, kraft plies, gypsum |
| `dc08_drywall_screw_pops.jpg` | "drywall screw pops", "nail pops" | Disc size, spacing and shading of pops |
| `dc09_tape_joint_crack_above_door.jpg` | "drywall crack above door frame tape joint" | A straight joint crack against a diagonal corner crack |

---

## 2. Measurements

### 2.1 Crack graph composition (`graph.json`, L0F burst)

| Face | D1 | D2 (cumulative) | FINAL (cumulative) | Run length p10 / p50 / p90 | Turn per run p50 / p90 |
|---|---|---|---|---|---|
| S | 4 primaries, 1.89 m | 37 cracks, 10.78 m: **16 rings**, 8 radials, 8 primary pieces, 5 forks | 70 cracks, 13.26 m: +5 rings, **+28 "crumb" cracks** | 10.6 / 18.6 / 28.2 mm | 14.8° / 36.5° |
| P | 4, 1.33 m | 27, 6.86 m: 14 rings, 6 radials | 55, 8.07 m: +28 crumbs | 9.5 / 18.2 / 28.3 mm | 14.6° / 38.0° |

Radials plus concentric rings is the signature of a brittle pane under a point load. The steady ±15–37° zigzag every 1–3 cm is a lightning or plaster-crack look.

### 2.2 Crack width: painted core and real gap (maps, S face; profiles every 10 mm along each crack)

`core50` is the width where the painted darkness R ≥ 0.5. `dark20` is the width where R ≥ 0.2 (the halo). `gap` is the opening through the board on the 3 mm plate grid. "Paper" means the intact wallcovering beside the crack; "torn" means inside a torn zone. Values are medians, with p90 where it matters.

| Stage | Distance from head corner | core50 | dark20 (halo) | gap | R max |
|---|---|---|---|---|---|
| D1 | 0–0.1 m, paper | 2.5 mm | 14.8 mm | 0 | 0.97 |
| D1 | 0.3–0.6 m, paper | 2.0 mm | 5.0 mm | 0 | 0.82 |
| D2 | 0–0.1 m, torn | 5.75 (p90 11.4) mm | 16.3 (p90 37.4) mm | **16.5 (p90 37.5) mm** | 0.99 |
| D2 | > 0.6 m, paper | 2.75 mm | 7.9 mm | **3.5 mm** | 0.90 |
| S4 | 0–0.1 m, torn | 9.0 (p90 18.8) mm | 30.9 (p90 50.2) mm | **32.4 (p90 46.2) mm** | 1.00 |
| S4 | 0.1–0.3 m, torn | 7.0 mm | 19.8 mm | 10.5 mm | 0.99 |
| S4 | > 0.6 m, paper | **4.75 mm** | **13.3 mm** | **3.8 mm** | 0.94 |

**Rendered** (V1 at 76°, 1080p; FWHM of the luminance dip across each crack; scale 3.6–3.9 mm/px at the head):

| Frame | Distance, surface | FWHM | Dip vs the paper beside it | Core sRGB luma vs paper |
|---|---|---|---|---|
| D1 V1 | 0–0.3 m, paper | 3.75 px = **14.5 mm** | 30 % | 76 vs 110 |
| D2 V1 | > 0.6 m, paper | 2.25 px = **8.3 mm** | 44 % | 42 vs 78 |
| S4 V1 | > 0.6 m, paper | 2.5 px = **9.0 mm** (p90 11.1) | **58 %** | 31 vs 76 |
| S4 V1 | 0–0.3 m, torn | 4.5 px = 17.7 mm (p90 43.5) | 39 % | 48 vs 81 |

Target (ESTIMATE): beyond about 0.3 m from a corner, an intact-paper crack should not read as a line at all (0.1–0.5 mm is 0.03–0.13 px at V1). Near the corner it should be 0.5–4 mm, visible as a **shadow inside a real gap with lit paper lips**, or as a light tear in the wallcovering.

### 2.3 Holes (S4)

| Measure | Value |
|---|---|
| S holes | 10 knocked plates, equivalent diameters 52–217 mm (184, 169, 112, 114, 92, 88, 59, 217, 53, 52 mm) |
| V1 rays through the S holes (400 rays, first surface hit behind the S face) | **plenum box (albedo 0.02): 200**; P board back (DrywallBack, sRGB 150/138/112, roughness 0.9): 142; ceiling tile: 26; T-bar: 17; flap: 6; miss: 5; P gypsum rim: 3; `wall_P_plenum` quad: 1; **studs or framing: 0** |
| Rendered hole interior (S4 V1, 12 mm inside each edge) | median sRGB **(16, 13, 9)**; p90 (86, 71, 51); 51 % of pixels below sRGB luma 20; linear luminance 0.004 against 0.179 for the wallpaper: **−5.5 stops** |
| Rim (solidify, square-cut 12.7 mm board) | projected **1.26 px at V1**, 4.2–5.2 px at V2H, 0.07 px at V6. A 45° crumbled break would be 2.5 px at V1 and 0.7 px at V6 |
| Outline | quantised to 3 mm cells: one step is 0.76 px at V1 and **1.37 px at V2H** (the visible serration). The large holes are bounded by `ring()` arcs, so they are rounded domes |

Target (ESTIMATE): roughly −2.5 to −4 stops, with structure readable inside (studs, the far board's back paper, screw points) and a white ring of broken core round the hole.

### 2.4 Torn zone colours (S4 V1 unless noted)

| Item | Value |
|---|---|
| Shader split (`scene.py` L429–440) | Noise scale 24 on `UVd`, which is normalised per face (S: 3.201 × 1.998 m; P: 3.201 × 1.5 m). The lattice is about **133 × 83 mm on S and 133 × 63 mm on P**, so the blobs are stretched 1.6–2.1:1 horizontally. Detail 6, map range 0.40–0.56 (soft edge). Fibre noise at scale 700 multiplies by 0.89–1.11 |
| Albedos (linear luminance) | Wallpaper print 0.42. Face paper sRGB (182, 175, 158): **0.43, the same as the print**. Kraft (122, 94, 64): 0.125. Spall gypsum 0.65–0.86. The torn-edge rim is "paper" 1.45, a 45 % gypsum mix. The hole rim is "paper" 1.0: plain torn paper, no gypsum |
| Rendered split | The dark (kraft) cluster is **57 %** of torn pixels at sRGB (82, 61, 37); the light cluster is (108, 91, 68) |
| Torn interior vs the wallpaper 2–6 cm outside it | S4: sRGB (88, 69, 46) vs (110, 87, 38): **−0.59 stop** and desaturated. D2: −0.11 stop |
| Torn area | S face: D1 0.018 m², D2 0.45 m², FINAL 0.99 m², S4 1.00 m². P face: 0.007 / 0.31 / 0.56 / 0.57 m² |
| Height map | −3.5 to +0.4 mm on every stage: bump only, no lifted geometry |
| Roughness | Wallpaper 0.72–0.77 (from `_S` by the `scene.py` rule). Torn, spalled and powdered areas 0.95. Flaps and strips 0.8 on both faces |

Target (ESTIMATE): a torn zone that is mostly off-white backing or face paper reads +0.3 to +1.0 stop **lighter** than this print. The grey-brown plies are small fuzzy patches, not half the area.

### 2.5 Debris, flaps and powder (S4 model probe and V1)

| Item | Value |
|---|---|
| Chunks (S face, 10) | 89–324 mm long, 12.8–13.7 mm thick; **distance from the wall face 0.18–0.95 m, median 0.64 m**; only 3 of 10 within 0.25 m; 840–17,296 tris each (the stair-stepped outline); top face uses `WallDamage_S`, i.e. the torn camouflage |
| Chunks (P face, 8) | 71–310 mm; 0.28–0.93 m from the face |
| Floor crumbs (260) | Max dimension p10 / p50 / p90 = **11.7 / 19.0 / 26.5 mm**, min dimension 11.2 mm (pebbles, not flakes); 10 unique meshes; distance from the face p10 / p50 / p90 = 0.06 / **0.29** / 0.67 m; **26 %** within 0.15 m |
| Floor strips (18) | Width 20–52 mm, length 217–427 mm. **Sideways step per cross-section 9–29 mm (median about 14)** and sideways range 39–85 mm: the lightning bolt. Standing 23–119 mm tall (coiled) |
| Flaps + hanging strips (33) | **0 of 33 have thickness.** Lens-shaped (width ∝ sin^0.6, pointed at both ends), curled 50–130° away from the wall, smooth-shaded, standing out 61–80 mm (median 65 mm) |
| Floor powder, S4 / S0 linear luminance on the S floor at Z 1.2 (V1) | 1.61 at 0.10 m from the wall centre; 1.34 at 0.25; 1.62 at 0.35; 1.68 at 0.45; **2.01 at 0.53 (peak)**; 1.70 at 0.65; 1.43 at 0.80; 1.11 at 1.00. That makes **two round stains 0.45 m out**. (The Z −0.2 samples are hidden or shaded by the open leaf and are not used.) |
| Room haze in L-A frames | Hidden: `A["haze"]` defaults to `"LB"` and `final.sh` passes no `--haze` |

### 2.6 Framing (S4 model)

Studs, kings and jacks are 38 × 135 mm with the `Prop_Studs` pine texture. No US lumber has that section: a 2×4 is 38 × 89 and a 2×6 is 38 × 140. The header is a solid 135 × 190 mm block. The top plate is single and 40 mm. Stud centres are at 0.40 m. The S holes sit between the king stud and the next stud, so no V1 ray reaches framing.

---

## 3. Every C render: what reads fake and why

`dir_C_*` files are byte-identical copies of `db_C_*` files, matched by md5. The F numbers point to §4.

| Render (and its `dir_C_` copy) | What reads fake | Causes |
|---|---|---|
| `db_C_L0F_burst_S0_V1_LA` (`dir_C_S0_V1`) | Nothing in the break: it is the intact baseline. Keep it as the reference for powder and haze deltas | — |
| `db_C_L0F_burst_M_V1_LA` (`dir_C_M_V1`) | Grit specks show as white rain dashes on the upper leaf. The curtain is fine | F10 |
| `db_C_L0F_burst_D1_V1_LA` (`dir_C_D1_V1`) | Two cracks per corner read as silver twigs: a painted dark core with white lip runs and white gypsum chips **under intact wallcovering**, which cannot happen. Measured 14.5 mm wide with a 30 % dip. Rain dashes on the leaf | F1, F2, F5, F10 |
| `db_C_L0F_burst_D2_V1_LA` (`dir_C_D2_V1`) | Two brown-grey camouflage smudges ("moth wings"). Black ink lines run 0.6–1.0 m into intact paper (8.3 mm, 44 % dip). No relief. Leaf-shaped flaps | F1, F2, F3, F5, F6, F7, F11 |
| `db_C_L0F_burst_D2_V2f50_LA` (`dir_C_D2_V2`) | The same at an angle; the lines cross the print like pen strokes. Rain dashes | F2, F5, F10 |
| `db_C_L0F_burst_D2_V2Hf50_LA` (`dir_C_D2_V2H`) | Close-up: torn patches with sticker outlines, black branching lines, pale leaf flaps, vertical white powder streaks, rain dashes on the leaf | F2, F5, F6, F7, F9, F10 |
| `db_C_L0F_burst_D2_V4_LA_relay`, `_V4_LB_relay` (`dir_C_D2_V4-relay-LA`, `-LB`) | P face mostly behind the giant. The visible corner cracks are black twigs. The tile gap shows a black plenum | F2, F4 (plenum albedo) |
| `db_C_L0F_burst_D2_V4Qf62_LB_relay` (`dir_C_D2_V4Q-relay-LB`) | White specks round the head: S light through 3–33 mm through-gaps. That is the glass read again; at D2 a drywall wall leaks light only at an opened corner or joint. Black twig cracks | F1, F3 |
| `db_C_L0F_burst_D2_V6_LA_human` (`dir_C_D2_V6`) | At 12 m: a grey smudge with spider legs. The read rests on 1 px ink lines and the darker torn zone | F1, F2, F5, F12 |
| `db_C_L0F_burst_FINAL_V1_LA` (`dir_C_FINAL_V1`) | Chunks in the air read as paper flakes (camouflage faces, edges under a pixel). Holes are flat black. Ring arcs show as smooth curves. Rain dashes | F1, F4, F8, F10 |
| **`db_C_L0F_burst_S4_V1_LA`** (`dir_C_S4_V1`; Red's frame) | All of Red's points: tree-like black cracks; two flat black domes with no rim; camouflage paper; leaf flaps; pebble crumbs; lightning-bolt strips; powder as round stains 0.45 m out; no dust in the air; white sheets for the tile halves through the door | F1–F10, F12, F13 |
| `db_C_L0F_burst_S4_V2f50_LA` (`dir_C_S4_V2`) | Torn camouflage zone, a black hole, the white tile half on the P floor, an oval scribble on the leaf's S face | F4, F5, F13, F14 |
| `db_C_L0F_burst_S4_V2Hf50_LA` (`dir_C_S4_V2H`) | Close-up: square-cut rims stepping every 3 mm (rope or comb edges, see `look/diag/s4v2h_*hole_x3.png`); black void above a pine king stud; flaps intersecting; vertical "rain" powder streaks; a hairy oval on the leaf | F3, F4, F7, F9, F14, F15 |
| `db_C_L0F_burst_S4_V5f62_LA` (`dir_C_S4_V5`) | Floor: lightning-bolt strips, evenly sprinkled pebble crumbs, camouflage chunk faces with corrugated rims, two round powder stains, clean white tile halves | F8, F9, F13 |
| `db_C_L0F_burst_S4_V6_LA_human` (`dir_C_S4_V6`) | At 12 m: a grey shape with two black dots. No dust in the air; the 1.8 m figure is the only scale | F2, F4, F10, F12 |
| `db_C_L0F_rip_D2_V1P_LA` (`dir_C_RIP-D2_V1P`) | Twig cracks fringed with white "moss" (lip runs and spalls). A clean light rectangle over the casing (the lintel peel rule) | F1, F2, F5, F6 |
| `db_C_L0K_burst_S0_V1_LA` | Baseline | — |
| `db_C_L0K_burst_M_V1_LA` | Rain dashes | F10 |
| `db_C_L0K_burst_D1_V1_LA` | Twigs as on L0F D1 | F1, F2 |
| `db_C_L0K_burst_D2_V1_LA` (`dir_C_STEEL-D2_V1`) | As L0F D2, plus enamel crazing lines on the S face of the leaf | F1–F3, F5–F7, F14 |
| `db_C_L0K_burst_D2_V4f45_LA` (`dir_C_STEEL-D2_V4f45`) | **The steel leaf has a glass star-crack**: 42 radials and 3 rings of flakes round the dish. Twig cracks with white fluff on the P wall | F14, F1, F2, F5 |
| `db_C_L0K_burst_D2_V6_LA_human` (`dir_C_STEEL-D2_V6`) | As L0F D2 V6 | F1, F2, F5, F12 |
| `db_C_L0K_burst_FINAL_V1_LA` | As L0F FINAL | F1, F4, F8, F10 |
| `db_C_L0K_burst_S4_V1_LA` (`dir_C_STEEL-S4_V1`) | As L0F S4 V1 | F1–F10, F12, F13 |
| `db_C_L0K_burst_S4_V6_LA_human` (`dir_C_STEEL-S4_V6`) | As L0F S4 V6 | F2, F4, F10, F12 |
| `dir_C_strip`, `db_C_strip_L0F_burst_LA`, `db_C_strip_L0K_burst_LA` | They inherit their tiles. Read left to right, they tell a pane-cracking story | as the tiles |
| `db_C_review_distance` | The ×2 loupes show the spider or moth read. **The 12 m claim currently rests on the ink lines (F2)**: re-verify it after the fix | F2, F12 |
| `db_C_review_r7_fixes` | Leaf shading bands and the kick plate, not the break structure. On the steel D1 AFTER tile, faint radial crazing shows | F14 (minor) |

---

## 4. Fix list, tied to code

Each item gives what reads fake, the code and parameter that cause it, the measurement, and the fix. Physical targets are ESTIMATE unless §1 cites a source.

### F1 · Crack topology is a glass impact pattern
- **Code:** `crackgen.py` `build_graph` L174–298.
  - Each corner gets 2 primaries, then radials ("down", "flat", "steep", "in") at D2.
  - `ring()` L150–171 adds **two concentric rings between every pair of neighbouring radials** (L258–265, r0 0.07–0.12 m, r1 0.20–0.30 m) and an outer ring at FINAL (L283–289).
  - Forks with p = 0.3 every 0.20 m (L266–276).
  - FINAL adds 14 "crumb" cracks per corner (L290–297).
  - `walk()` L92–126 zigzags every 10–32 mm (`WK` L197: zig 13°, seg 0.010–0.032) round a heading that is pulled back toward its start.
- **Measured:** §2.1 (rings are 43 % of the D2 cracks on S and 52 % on P; turns 15° median, 37° p90).
- **Why it is fake:** radial-plus-ring is the pane's point-load pattern. Gypsum board on studs breaks along its framing and joints. Long runs are straight, with kinks at fasteners and joints, not a steady zigzag.
- **Fix:** replace the glass rule with a **drywall rule** driven by an explicit wall layout:
  - **Inputs:** stud lines (king and jack at the jambs, studs at 406 mm centres), the header or track top, cripples, sheet joints (an L-cut sheet at each head corner; optionally one vertical tape joint over the door), and the screw grid along each stud line.
  - **Per stage, cumulative as now:**
    - D1: one diagonal per corner, 0.15–0.4 m at 40–60°, mostly under intact wallcovering, plus 2–4 screw pops on the nearest stud line.
    - D2: the diagonals run on (to the ceiling on P); hinge cracks along the king-stud edges and the header top near each corner; the joint over the door opens.
    - FINAL: panels between hinge lines break into 2–4 pieces each, 150–400 mm.
    - S4: holes bounded by those lines.
  - Keep crack runs straight within a few mm over 0.1–0.5 m, kinking only at fasteners and joints.
  - **Remove rings, the p = 0.3 forks and the crumb cracks.**
  - Update `10_design.md` §8.3's build notes once Red agrees.

### F2 · Cracks are painted as dark albedo (ink)
- **Code:**
  - `crackgen.py` `STAGE` L333–342 sets the core widths (D1 2.6 mm, D2 7.5 mm, FINAL and S4 8.5 mm).
  - `face_stage` L563–597 draws every active crack with `dark = clip(1.25 − dist/hw)` plus a halo `0.55·exp(−(dist/(2.2·hw+0.8 mm))²)`, with `hw = 0.5·core·wl·(0.6+0.8·nz)`.
  - The width factor never drops below 0.45 × the crack's weight (`near = 0.45 + 0.55·exp(−d/0.30)`, L580; `wv` L583). Far cracks therefore keep a 3–6 mm core, and nothing bridges or hides them.
  - `lip` L641–644 adds white lip runs from D1 on.
  - `scene.py` `wall_damage_mat` L447 mixes the base colour **toward linear (0.010, 0.008, 0.006) by 0.97·R**.
  - `hgt = −dark·1 mm` L649 adds a bump groove.
- **Measured:** §2.2. Far cracks render 9.0 mm wide and 58 % dark; D1 renders 14.5 mm wide.
- **Fix:**
  - **No crack darkens albedo.** Darkness has to come from geometry: a real gap and its occlusion, only where the board is actually open.
  - Where the board is cracked but the wallcovering bridges it, show a **pucker or ridge** in the normal or height map (0.5–1 mm high, 10–40 mm wide, along the crack, plus short diagonal wrinkles fanning from the head corner). Do not draw a line.
  - Where the covering tears, draw a **light** tear (F5, F6).
  - The width taper must reach under 0.5 mm within about 0.3 m of the corner.
  - Re-measure with `measure_render.py`: the far-crack dip should be under 10 % or the crack should not be found at all.

### F3 · Every crack is a through-gap on a 3 mm raster
- **Code:**
  - `crackgen.py` `raster_cells` L302–321 cuts each open crack as 4-connected 3 mm cells (`G` L45). Each cut is at least one 3 mm cell wide; measured across the crack it is 3.3–3.8 mm, because the diagonal fill adds the rest.
  - `open_c` L511 opens primaries, rings, radials, ceiling and crumb cracks from their birth stage.
  - The widening at L518–522 adds up to 3 dilations within `reach` (0.24–0.34 m).
  - `scene.py` `wall_face` L820–896 builds a quad per 3 mm cell, solidified square at L891, so plate edges step every 3 mm.
- **Measured:** gaps of 3.3–3.8 mm even more than 0.6 m from the corners; 16.5 mm (D2) and 32.4 mm (S4) median at the corners; 40 free plates on S at FINAL. One step is 0.76 px at V1 and 1.37 px at V2H; the rope edges are visible in `look/diag/s4v2h_*_x3.png`.
- **Fix:**
  - Build plate outlines as **vector polygons** from the crack polylines, with sub-mm roughness and a crumbled bevel (F4). Do not use raster cells.
  - Open through-gaps only on hinge lines and diagonals near the load, and only from D2. Everything else stays continuous board with a face-paper crack.
  - That also stops the "light through a spiderweb" specks in V4Q.

### F4 · Holes are flat black blobs
- **Code:**
  - (a) `scene.py` `ceiling_tiles` L749: the render-only `plenum` box uses `M["plenum"]`, albedo **0.02** (L356). It catches 50 % of the V1 rays through the S holes, because those rays pass through aligned P holes.
  - (b) The far board's back is `DrywallBack` (L351, sRGB 150/138/112, roughness 0.9). The camera looks up through the hole at the part the hole itself shades. No framing is in line.
  - (c) Rims are `solidify(o, BOARD, …)` (L891, L933; `BOARD` 12.7 mm, L36) with `use_even_offset`: square-cut, 1.26 px at V1.
  - (d) `gypsum_mat` L363–394 squares an 8 mm ambient-occlusion term (L376–380), so every slot wall and rim goes dark. Its noise has no vector input, so it uses Generated coordinates and the porosity scale changes with object size (18 mm on the wall object, about 1 mm on a chunk).
  - (e) `crackgen.py` L657–663 paints the 4–22 mm ring round each hole as torn paper (G = 1.0), not as exposed gypsum (2.0).
  - (f) The knocked plates are cells between `ring()` arcs (L542 picks plates within 0.36 m of a corner, p = 0.8), so the holes are rounded domes.
- **Measured:** §2.3 (−5.5 stops, median sRGB 16/13/9).
- **Fix:**
  - Give the plenum real content and albedo (deck, hanger wires, the backs of the tiles, about 0.15–0.35) and let the P room's light reach it through the missing tile.
  - Place the holes so that they are bounded by stud or header lines and show framing (F12, F15).
  - Bevel every break (30–60°, crumbled), and put a white exposed-core ring 5–30 mm wide round each hole. In the map that is G = 2 (spalled), not 1.
  - Add a ragged face-paper fringe that overhangs the break, and 1–2 hanging pieces per hole held by paper.
  - Replace the squared AO with plain BSDF lighting, or a mild contact term only inside gaps narrower than 5 mm.
  - Map the gypsum noise in object space at a fixed metre scale.

### F5 · Torn wallcovering reads as camouflage cloth
- **Code:**
  - `scene.py` `wall_damage_mat` L429–440: a two-tone noise split (scale 24 on the stretched `UVd`, map range 0.40–0.56) between face paper (182, 175, 158) and kraft (122, 94, 64), times a 700-scale fibre speckle.
  - Powder is mixed in at L446.
  - The edge comes from `crackgen.py` L630–640: `rimw` gives G = 1.45 (a 45 % gypsum mix) in a 1–2 px line, and `outside` gives a shadow of R 0.38 over 2 px.
- **Measured:** §2.4. Kraft covers 57 %, the face paper has the same albedo as the print, and the torn zone is −0.59 stop and grey.
- **Fix:** build the torn zone from **layers in physical order**.
  - Lift the vinyl print to show the **off-white backing** (about 0.65–0.75 albedo; scrim threads if fabric-backed). That is the dominant colour at a tear.
  - Where the paste bond beat the paper, show the drywall face paper (ivory, about 0.55–0.65) with a fibrous torn texture.
  - Show the grey-brown inner plies only as small fuzzy patches along the tear's peel direction (≤ 15–20 % of the torn area).
  - Show white gypsum only where the board chipped or broke.
  - Patch edges are sharp and fibrous, elongated along the peel direction, isotropic in metres. Drive them from **world-metre UVs**, not `UVd`.
  - The torn zone should read lighter than the print (§2.4 target).
  - Keep the vinyl print's slight sheen (roughness about 0.5–0.65) next to the matte backing and paper (about 0.85–0.95), so the layers separate under the troffers.

### F6 · Tear edges are smooth, flat and clean
- **Code:**
  - `crackgen.py` `tear_layers` L371–448: the tear gate is `noise1d` with a 9-sample (36 mm) correlation, so the outlines are smooth lobes.
  - The radius lobes come from `nzl` on 135 mm cells (L613–622).
  - `peel_plates` L465–495 peels whole plates; its lintel rule (L486, and L621 at FINAL) produces the clean rectangle above the casing in RIP V1P.
  - The ragging is one 1.5 mm dilation (L630).
  - The height channel only spans −3.5 to +0.4 mm (§2.4), and `scene.py` uses it as a bump (L458–463). No edge is lifted.
- **Fix:**
  - Tear outlines become straight-ish segments (20–120 mm) with corners, following the board's crack where the covering tore over it.
  - Add a fibre fringe of 1–3 mm.
  - Lift the edge **1–5 mm as geometry** (a thin rolled lip), so it casts a contact shadow under the troffers.
  - Add delamination blisters near the tears and stretch creases where the covering bridges a hinge step.
  - Drop the lintel rectangle.

### F7 · Flaps and hanging strips read as leaves or fabric
- **Code:**
  - `scene.py` `flaps` L1637–1719: zero-thickness 10 × 6 grids; width `wmax·sin(π·t)^0.6` (L1683), pointed at both ends; curl 50–130° away from the wall for every flap (L1665); smooth-shaded (L1717); hinge 6–16 mm off the crack.
  - `strip_mat` L1617–1634: roughness 0.8 on both faces; the back is (196–232) noise.
  - Hanging strips L1755–1777: clean rectangles.
- **Measured:** 0 of 33 have thickness; they stand 61–80 mm off the wall.
- **Fix:**
  - Make each flap a **triangle or trapezoid** whose two tear edges converge where the tear stopped. It is widest at the hinge on the crack.
  - Give it a thickness (solidify a few tenths of a mm, with a light edge).
  - Let it droop: gravity bend plus a single curl at the free edge. Some hang down past the hinge.
  - Back material: backing off-white with paste sheen and **patches of face paper and gypsum torn from the board**.
  - Front: the print with the vinyl's own roughness.

### F8 · Floor debris reads as paper flakes and confetti
- **Code:**
  - `scene.py` `chunks` L924–959: each knocked plate is extruded flat. Its top face uses `WallDamage_<face>` (L926, the camouflage). Its rims step every 3 mm. At S4 it is placed 0.15–0.95 m out (L948), face up 70 % (L950).
  - `rock_mesh` and `crumbs` L1549–1578: 10 convex hulls of 9 points (radius 6–22 mm, squashed 1 : 0.8 : 0.45), 260 on the floor at `|N(0.25, 0.3)| + 0.09` m.
  - `strips` L1722–1753: `tw = rng.normal(0, 0.02)` **per cross-section** (L1740) gives the zigzag; the curl `a = s/R·0.6` (L1737) makes coils up to 119 mm tall.
- **Measured:** §2.5.
- **Fix:**
  - Debris comes from the F1 / F3 panels and breaks again on landing: 2–5 pieces per panel, each with a beveled, crumbled gypsum edge.
  - Most pieces keep the **print** on their face, because the covering tore along the crack. Some show backing or face paper. Undersides show grey back paper.
  - Use a power-law size spectrum: a few 150–300 mm pieces, more at 30–80 mm, many 3–15 mm angular crumbs (many with a paper face), and grit under 3 mm as a floor texture.
  - Put about 60–70 % of the mass within 0.3 m of the wall face (ESTIMATE). A few pieces lean against the cove base or the wall; a sparse throw zone runs to 0.8 m on S, the side the push drives toward.
  - Floor strips are flat irregular polygons with a single gentle curl, not coils. Delete the per-section `tw` offset.

### F9 · Powder: the wrong place, and streaked like rain
- **Code:**
  - `scene.py` `powder_floor` L1581–1611: 6 square cards centred at `x = ±(0.08 + 0.45)` (L1607), radius 0.55 / 0.4, alpha `(1 − r/0.5)^1.5 · noise`. At the wall base that is about 8 % of the card's peak.
  - `crackgen.py` `downsmear` L451–462: noise only along Z (σ = 2 px = 3 mm), thresholded `(x − 0.35)·3`, gives 3–9 mm vertical white lines 0.10 m below spalls and 0.22 m below holes (L647, L663).
- **Measured:** the luminance ratio peaks at 2.0× at 0.53 m; the wall base sits at 1.3–1.6× (§2.5).
- **Fix:**
  - Put a wedge of powder **against the cove base** under each break: densest at 0–0.1 m, fanning out to 0.4–0.6 m, slightly streaked radially.
  - Dust film on the carpet tips; deposits on every horizontal ledge (the casing head top, the cove base top, the hinge knuckles, the leaf's top edge, the saddle).
  - On the wall, a soft, even dust film below the break that fades down. No vertical streaks.

### F10 · Not enough dust in the air; grit reads as rain
- **Code:**
  - `scene.py` `main` L2021 hides the room haze in every L-A frame (`A["haze"]` defaults to `"LB"`, L59; `final.sh` passes no `--haze`).
  - `dust` L1465–1502: no curtain at S4 (`k` 0, L1475). S4 has only a `settle` volume below 1.2 m (density 1.2, L1500–1502). FINAL has 6 puffs (density 5, r 0.28).
  - `specks` L1508–1546 makes 2.2 × 2.2 × 35 mm vertical boxes (`s` L1512) at 45 % opacity (L1542). After scaling they are white dashes 0.8–2 mm wide and 7–44 mm tall, which read as rain on the leaf from M to FINAL.
- **Fix:**
  - FINAL and S4 get a pale gypsum haze round the head and in the doorway. It is densest at the break and settling, still dense at S4 (the seconds after the break; ESTIMATE). Light from the troffers and through the opening shows it.
  - Keep the room haze on in L-A when dust is the story.
  - Specks become points or short flakes (2–8 mm). Use streaks only if a shutter blur is applied to the whole frame.

### F11 · Plate relief is random and does not read
- **Code:** `scene.py` `wall_face` L836–850. `pop` is `U(0.4, 1.4)·SP_["pop"]` (≤ 14 mm) times proximity. Each plate tilts by `ta`, `tb ~ N(0, 1)·0.07 rad`, so neighbours step in random directions. `sep` is 5.5 mm.
- **Fix:** panels **hinge coherently** about their stud or fastener edge, with the edge nearest the frame pushed furthest (out on S; crushed into the cavity on P). Steps between panels are 10–40 mm at FINAL and S4, so they cast shadows under the troffers and read at 12 m.

### F12 · Scale cues are weak
- **Cause:** nothing in the damage has a known size. There are no fasteners, joints, visible studs or visible board thickness. The crumbs are all one size (12–27 mm), and every crack has the same rendered width (about 9 mm). The only references are the wallpaper repeat (0.75 × 1.125 m), the cove base and the 1.8 m figure.
- **Fix:** add drywall features of known size:
  - screw pops (15–25 mm) on stud lines 406 mm apart;
  - a tape joint (sheets 1,219 mm wide);
  - stud flanges 38–41 mm wide inside the holes;
  - the 12.7 or 15.9 mm board edge with its white break ring;
  - the debris spectrum from F8.

  These are what tell a viewer this is a wall and how big the giant is.

### F13 · Ceiling tile halves read as white paper sheets
- **Code:** `scene.py` `tile_broken` L801–816 splits the tile on a straight cut at Z 0.52 (`zm` L803). `tile` L781–790 is a 16 mm box with `M["tile"]` (the painted fissured face) on all six faces, back and edges included.
- **Fix:** a ragged break with a **grey-brown fibrous core** on the break faces, an unpainted grey back, crumbs and fibre dust beside it, and a slight warp.

### F14 · The leaf marks follow the same glass and ink convention (secondary)
- **Code:** `crackgen.py` `leaf_maps` L670–816.
  - Wood struck face: 352 short crazing strokes in the print (`craze_f` L692).
  - Wood far face: 16 tension splits 70–420 mm long plus flake lips (`ten_f` L711). They read as a hairy oval on the open leaf (V2, V2H).
  - Steel: 42 radials and 2–3 rings of flakes per face (`steel_f` L720–746). That is a **star-crack** (V4f45, and faint in the r7 sheet).
- **Real (`02` §2.1, §2.3):** a solid particleboard-core leaf shows short vertical hairlines near the latch stile on the far face, plus a crushed print. A hollow-metal leaf shows a smooth dish that reads in its reflection, with paint cracking only along tight bends and flaking at the dent rim.
- **Fix:** drop the radial and ring sets; keep the dish and the bow. Red's note is about the wall, so this item comes after F1–F10.

### F15 · Framing is neither 1990 wood nor steel
- **Code:** `scene.py` `framing` L640–654 builds 38 × 135 mm "studs" with the pine `Prop_Studs` texture, a solid 135 × 190 header and a single 40 mm top plate.
- **Fix:**
  - L0-K, behind a hollow-metal frame: galvanised 25 ga C-studs with 41 mm flanges and a track header (`02` §1.1).
  - L0-F: wood or steel.
  - Build the cavity and the board layers to a real stud. The map wall is 0.16 m, which fits for example 3-5/8" (92 mm) steel studs with two layers of 5/8" board per face (156 mm), or 2×6 wood (140 mm) with one 1/2" layer per face (165 mm). A 135 mm cavity matches neither (ESTIMATE; `02` §10 item 4 leaves the wall type open). A two-layer board would show two board edges at each break: a strong scale cue.
  - Add screw tips through the back of the far board.
  - This only matters once F4 makes the holes show the cavity.

---

## 5. Order of work, what to keep, what to re-check

1. **F1 + F2 + F3**: the drywall fracture rule, vector plate outlines, and no ink. This is the biggest change to the read, and it changes the §8.3 brief, so Red agrees first.
2. **F4 + F12 + F15**: holes framed by studs, a lit plenum, beveled white breaks, fasteners and joints. These share the layout from F1.
3. **F5 + F6 + F7**: wallcovering layers, lifted edges, triangular drooping flaps.
4. **F8 + F9 + F10**: the debris spectrum and pile, powder at the wall base, dust in the air.
5. **F11, F13, F14**: panel hinging, tile breaks, leaf marks.

**Keep:**
- the cumulative birth-stage rule;
- the leaf band (+0.075 m) and the opening check;
- the exposure calibration (−0.9 EV, frame 77);
- Giant B `press3`;
- the views and the stage clock.

**Re-check after the rebuild** with the §6 scripts:
- far-crack dip under 10 % (or not found);
- the torn zone +0.3 to +1.0 stop against the adjacent print;
- the hole interior about −2.5 to −4 stops, with framing hit by at least some of the rays;
- at least 60 % of debris within 0.3 m of the wall face;
- the floor powder ratio highest at 0–0.15 m.

**Re-verify the 12 m read (V6 loupe) explicitly.** Today it relies on the ink lines, and after F2 it must come from the stepped panels, the light tears, the holes and the dust.

ESTIMATE targets in this file should be checked against the §1.2 photos once Red approves the search.

---

## 6. Reproduce

```
W=/Users/redwang/FrontRoomsVisualWork; C=$W/door_break/C; D=$C/look/diag2
$C/py $D/measure_maps.py L0F_burst           # crack core / halo / gap widths, torn / spall / powder areas -> $D/maps_L0F_burst.json
$C/py $D/measure_render.py                   # rendered crack FWHM + dip, hole colour, torn vs wallpaper, floor powder profile -> $D/render_L0F.json
/Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup $C/models/DB_C_L0F_burst_S4.blend \
    --python $D/probe_model.py -- L0F burst S4 $D/probe_L0F_S4.json   # chunks, crumbs, strips, flaps, framing, tiles, V1 rays through the holes (read-only, never saved)
```

`cam.py` is the camera model (Blender's vertical-fit camera from `scene.py` `VIEWS`). It was checked against the casing corners in S4 V1: predicted (816, 279) and (1104, 279), measured (≈818, 280) and (≈1100, 280).

Crops used: `$C/look/diag/*.png` (the head, the floor and both holes at ×2–×3) and the contact sheets `$D/sh1–sh4.jpg`.
