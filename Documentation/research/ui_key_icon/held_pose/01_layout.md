# Held key arrangement (pickup shot → P4): layout

Status: **DONE, layout stage** (2026-10-14, 游戏视觉). Data: [`held_arrangement.json`](held_arrangement.json). Images: [`images/`](images/).
Next stage: the on-screen scale, offset and rotation per screen (1920×1080, 1920×1200, phone). This stage covers the arrangement in its own frame, as an orthographic front view. Perspective and the 27.5° off-axis view are not covered here.

## 0. Short answer

- **Same key and ring for all three tags.** The B glyph puts the ring and both holes in the same place for Rect, Round and Long (`lockup_B` uses `widest_left = 38` for every shape). So only the tag kit changes. Each shape was overlap-tested on its own.
- **Tag:** exactly face-on (identity rotation), number upright. Its ring hole is 12.725 mm left of the anchor and 8.633 mm above it on screen. The anchor sits in the tag's front-face plane.
- **Key:** level, tip right, cuts up, with the ring hole where the glyph has it. It is turned slightly, because the ring could not pass through its hole otherwise (§3):
  - 17.8° about its long axis (top edge toward the lens);
  - 3.1° tip toward the lens;
  - 0.4° tip up.
  - Its ring hole is 3.2 mm in front of the tag's hole.
- **Ring:** passes through the key's hole and the tag's hole. The wire runs at 38.4° to the key hole's axis and 36.6° to the tag hole's axis. (G3's hung pose uses 30°; the measured limit for this wire bundle is about 45°.) Seen from the lens, the ring is an ellipse with minor/major = 0.40. The split sits behind the key bow.
- **Overlap test (Blender BVH, G3 method):**
  - 0 triangle overlaps for ring–key, ring–tag and key–tag, on every shape.
  - Still 0 with the ring inflated by 0.10 mm, so the clearance is at least 0.10 mm everywhere. (0.15 mm inflation gives 10 / 8 overlaps.)
  - The approximate smallest gaps are 0.19 mm (ring–key), 0.21 mm (ring–tag) and 1.65 mm (key–tag).
  - Re-run from the JSON exactly as D1.6 will read it (quaternions and positions): same result.
- **Against the glyph, front view at the same px/mm:**
  - **Tag:** exact. Every bounding edge is within 0.00 mm, and the visible-area IoU is 0.99.
  - **Key:** each outer edge is within −0.29 to +0.58 mm of the glyph's. That is ≤ 0.4 px at 1x, or about 1.2 px at 3x (2.07 px/mm). The height is ×0.972, from the roll; the width is ×1.002.
  - **Ring:** does not match, and cannot (§3). The glyph's open circle on the upper left becomes a narrow loop between the two holes.

| Image | What it shows |
|---|---|
| [`images/HP01_overlay_rect.jpg`](images/HP01_overlay_rect.jpg) | Rect: glyph @1x, 3D front view, 3D with the glyph outline, 3D at 55 % over the glyph, part-mask difference, numbers |
| [`images/HP02_overlay_round.jpg`](images/HP02_overlay_round.jpg) | the same for Round |
| [`images/HP03_overlay_long.jpg`](images/HP03_overlay_long.jpg) | the same for Long |
| [`images/HP04_tradeoff_rect.jpg`](images/HP04_tradeoff_rect.jpg) | three overlap-free solutions: how well the key matches against how far the ring opens |
| [`images/HP05_threading_closeups.jpg`](images/HP05_threading_closeups.jpg) | 3/4 view and close-ups: the ring through the bow hole (front and back) and through the tab hole |
| [`images/HP06_top_depth.jpg`](images/HP06_top_depth.jpg) | top views (lens at the bottom), Rect / Round / Long: the depth layering |
| `images/held_front_<shape>.png`, `images/held_front_id_<shape>.png` | raw 720 × 480 RGBA front renders and the flat ID pass (key red, ring green, tag blue), for pixel comparisons in the next stage |

**Verification log:** **VL270** · K1 · "Held key against the B glyph" · FLAG. Figma slide `3141:7042`, with slots HP01, HP05, HP04. HP02 and HP03 are named in the row's notes; HP06 appears only in this note.

## 1. Frame and anchor

| | |
|---|---|
| Origin | **The anchor.** It is the point that corresponds to glyph point (36, 24), the centre of the 72 × 48 B glyph (1x, top-left origin). It is taken in the plane of the tag's front face |
| +Z | toward the lens: the forward of `LookRotation(lens − held, lens.up)` in `FrontRoomsDoorShot.PlaceKey` |
| +Y | up of the tag number (camera up) |
| +X | **the viewer's left.** The frame faces the camera, so screen right is −X. Every "right" in this note means screen right, which is −X |
| Units | metres; `localEulerDeg` in Unity's convention (`Quaternion.Euler(x, y, z)` = Ry · Rx · Rz); `localRotation` is the authoritative Unity quaternion (x, y, z, w) |
| Use | `world = pose.position + pose.rotation · localPosition`, `rotation = pose.rotation · localRotation`. Or put the three kit roots under a `Held` child set to the pose. The kit origins are the sidecar origins: the key's shoulder, the ring's `hook_contact` and the tag's `hole`. No scale |

Where the anchor sits:
- 12.725 mm right of and 8.633 mm below the tag hole's centre on screen. In the glyph that is (36 − 27.22, 24 − 18.043) px ÷ 0.69 px/mm.
- 2.1 mm in front of the hole's mid-plane (the tag body is 4.2 mm thick).
- For the Rect and Long tags it lands on the tag body. For Round it lands just outside the disc, in the gap between the tab and the disc.

The anchor's depth (the fob face) is the reference plane for `HeldDistance`. Depth ranges relative to it:

| Part | z range |
|---|---|
| tag | −4.2 to 0 mm |
| key | −3.7 to +6.9 mm (the roll tips the bow's foot back and its top forward) |
| ring | −19.1 to +4.5 mm (the far side of the loop runs behind the tag) |

## 2. The 2D composition (source: the B generator)

`research/ui_key_icon/scripts/design.py.txt`, `lockup_B`:
- Scale: KB = 1.38 px/mm on the 2x grid, so 0.69 px/mm at 1x.
- Ring centre at 1x: ((2 + 38 · 1.38) / 2, (2 + 13 · 1.38) / 2) = (27.22, 9.97) px. Ring radius `RING_R` = 11.7 mm, the kit's wire centreline.
- The key's `ring_hole` sits on the ring's right point: (35.29, 9.97) px.
- The tag's hole sits on the ring's bottom point: (27.22, 18.04) px.
- The key is drawn as the side view of `Kit_Key_Zone`, tip right, held level.
- The tag hangs straight down from its hole and shows its number to the lens.
- The ring stroke runs from 103° to 340°. The bow and the tab hide the rest.

The same composition in mm, screen axes (x right, y up), from the tag hole:

| Point | x, y (mm) |
|---|---|
| ring centre | (0, 11.7) |
| key ring_hole | (11.7, 11.7) |
| anchor | (12.725, −8.633) |

The 3D outline here is compared with the master's **outer** edges, not its area or centroid. The glyph is stylised:
- every key edge is snapped to a whole 1x pixel: the bow's left edge 30.81 → 31 px, the hole 35.29 → 35 px, the tip 70.83 → 71 px;
- the cuts are drawn 2.5 × deeper;
- the hole is drawn 1.5 × wider.

The stylisation explains the 3D key's −0.29 mm left edge and −0.14 mm tip. The 1x glyph re-rendered from the generator (`$W/heldkey/glyph_vec.py`, read-only; the masters were not touched) equals the shipped `B_*_red_held@1x.png` to within 1/510 of alpha.

## 3. Why the glyph's ring cannot be built as drawn

- **The ring must run nearly along each hole's axis.** A wire through a hole has to.
  - The ring's cross-section through a hole is two stacked coils: 1.6 × 1.8 mm.
  - The key's hole is Ø 4.8 mm in a 2.2 mm bow, with 0.3 mm chamfers. The tag's hole is a Ø 5.0 mm 16-gon in a 2.4 mm tab.
  - So the wire can lean at most about 45° off the hole's axis before it cuts the plate.
- **The glyph breaks that.** It draws the ring face-on with both parts face-on, so at both holes the wire lies in the picture plane, 90° off both hole axes.
- **The face-on limit:** take both parts facing the lens, with their holes where the glyph puts them (16.55 mm apart on screen). The smallest possible angle is 45° at both holes, with the ring seen edge-on as a straight bar between them. (Derivation in `$W/heldkey/explore1.py`; the grid search confirmed it.) Any opening of the loop costs more angle at a hole, or the key has to turn.
- **The tag stays exactly face-on.** That is the anchor's definition: fob to the lens, number upright. So the key takes the turn: 17.8° about its long axis, which also shows a sliver of the cut edge, plus 3.1° of yaw.
- **Result:** the loop opens to 0.40 at 37–38° per hole. The key's projected height pays 2.8 %; nothing else changes.

This is a held lockup, not a rest pose under gravity. Only the contact constraints are physical: the ring through both holes, no interpenetration, and a clearance of at least 0.10 mm. The tag does hang plumb under its hole.

## 4. Method

All scripts are in `$W/heldkey/` (`$W` = `/Users/redwang/FrontRoomsVisualWork`). Blender 4.3 runs as `-b --factory-startup`, and every check works on the kit FBX files in main, read-only.

1. **Parameterisation** (`heldgeo.py`, mm, in this frame):
   - the tag at the identity, hole at the origin;
   - the key's ring_hole at the glyph position, plus a small screen shift (sx, sy);
   - the key's depth `dz`, and its roll, yaw and in-plane angle;
   - the wire point inside each hole (offset within the hole);
   - the ring: the circle of radius 11.7 mm through both wire points, turned `chi` about their chord;
   - `sigma`: the ring's spin about its normal, which places the split.
2. **Analytic pass-through test.** The bundle's surface (both coils, the full 1.6 × 1.8 mm section) is sampled ±30° around each hole and tested against the plate around the hole (the real hole radius, chamfers and thickness) with a 0.15 mm margin. Each wire centreline must also cross the hole's plane inside the hole.
3. **Solve** (`grid2.py` grid, then `solve2.py`, Nelder–Mead):
   - minimise the ring's mismatch to the glyph circle (symmetric chamfer RMS);
   - subject to the key's landmark RMS (16 outline points, screen projection) ≤ a cap, the pass-through test, and the key in front of the tag (`dz` ≥ 0) with its top edge toward the lens.
   - Caps 0.25 / 0.40 / 0.50 / 0.75 mm give the trade-off below.
4. **Split placement** (`b_final.py`): `sigma` every 5°. The split zone (the two cut ends and the crossover, ring-local 18–52°) must stay ≥ 20° from both holes, and the most hidden option wins: rays toward the lens from the split hit the key or the tag. Chosen: σ = 35°, 100 % hidden behind the bow, 73° from the nearer hole.
5. **BVH overlap test** (`b_final.py`, then `b_verify_json.py` on the written JSON). It follows the G3 harness (`research/interactables/harness/hung_pose_check.py.txt`): LOD0 meshes are converted to Unity space with C⁻¹ and BVH triangle overlaps are counted for ring–key, ring–tag and key–tag. The ring is also tested inflated by 0.05 to 0.25 mm. Approximate min gaps come from nearest-point queries. Each shape gets its own run.
6. **Renders and overlays** (`b_render.py`, `overlay.py`):
   - Cycles orthographic front view from the lens, framed on the 72 × 48 glyph box at 6.9 px/mm (the 1x glyph × 10) and centred on the anchor;
   - a flat ID pass for the part masks;
   - the glyph @1x upscaled ×10 nearest for the overlays, and the generator's vector re-render for the part masks;
   - the masks compare what is visible from the lens, with the glyph's own layer order (ring < tag < key).

Trade-off (Rect; all three are overlap-free with ≥ 0.10 mm clearance, image HP04):

| Key RMS cap | Key roll / yaw | Key in front of tag | Ring minor/major | Wire–hole angle key / tag | Key edges 3D − glyph (L R T B, mm) |
|---|---|---|---|---|---|
| 0.25 mm | 14.1° / 2.7° | 2.5 mm | 0.32 | 39.1° / 37.0° | −0.29 −0.14 −0.14 +0.43 |
| **0.40 mm (chosen)** | **17.8° / 3.1°** | **3.2 mm** | **0.40** | **38.4° / 36.6°** | **−0.29 −0.14 −0.14 +0.58** |
| 0.75 mm | 24.2° / 4.2° | 4.7 mm | 0.52 | 37.3° / 36.8° | −0.29 −0.14 −0.43 +1.16 |

Why 0.40 mm:
- the key's edges stay within 0.6 mm (about 1.2 px at the 3x start size);
- the ring opens a quarter more than at 0.25;
- 0.75 would cost 1.2 mm at the bow's foot and still leave the loop half-closed.

## 5. Results

### 5.1 The arrangement (same for Rect, Round, Long except the tag kit)

| Part | Kit (variants) | localPosition (m) | localEulerDeg | localRotation (x, y, z, w) |
|---|---|---|---|---|
| key | `Kit_Key_Zone` (`_Nickel`) | (−0.025498, 0.019907, 0.002370) | (0.3567, 273.0159, 342.1886) | (0.108778, −0.679604, −0.110192, 0.717052) |
| ring | `Kit_KeyRing` | (0.000461, 0.024770, −0.004402) | (324.2335, 60.7522, 35.0) | (−0.107950, 0.538633, 0.394987, 0.736347) |
| tag | `Kit_KeyTag_{Rect,Round,Long}` (`_Blue`, `_White`) | (0.012725, 0.008633, −0.002100) | (0, 0, 0) | (0, 0, 0, 1) |

Useful points, from the JSON's `points_m` (m, this frame):

| Point | Position (m) |
|---|---|
| key ring_hole | (0.000970, 0.020548, 0.001129) |
| key grip | (−0.007519, 0.020495, 0.001576) |
| key tip | (−0.050463, 0.019752, 0.003686) |
| tag hole | (0.012725, 0.008633, −0.002100) |
| tag number (Rect) | (0.012725, −0.010367, −0.000750) |
| ring centre | (0.008069, 0.017525, −0.007307) |
| ring normal | (0.708, 0.584, 0.396) |
| wire tangent at the tag hole (for the sway) | (0.576, −0.153, −0.803) |

### 5.2 Overlap test (every shape, from the JSON)

| Pair | Overlaps as built | Ring +0.10 mm | Ring +0.15 mm | Approx. min gap |
|---|---|---|---|---|
| ring – key | 0 | 0 | 10 | 0.19 mm |
| ring – tag | 0 | 0 | 8 | 0.21 mm |
| key – tag | 0 | — | — | 1.65 mm |

The ring passes through both holes. The centreline crosses the key's hole plane 0.33 mm from the hole axis and the tag's 0.45 mm from it, against radii of 2.4 and 2.5 mm. See the close-ups in HP05.

### 5.3 Front view against the glyph (6.9 px/mm, anchor at the image centre)

| Shape | Silhouette IoU | Tag IoU, edges | Key IoU, edges L R T B (mm), w × h | Ring |
|---|---|---|---|---|
| Rect | 0.912 | 0.992, 0.00 all | 0.873, −0.29 −0.14 −0.14 +0.58, 1.002 × 0.972 | IoU 0.005; bbox 0.76 × 0.77 of the glyph circle's; left edge +7.8 mm, top −7.0 mm |
| Round | 0.893 | 0.989, 0.00 all | same | same |
| Long | 0.912 | 0.990, 0.00 all | same | same |

- The key's IoU (0.87) is held down by the glyph's stylised 2.5 × cuts and 1.5 × hole. Its outer edges (above) carry the match.
- The rest of the silhouette difference (XOR about 246 mm²) is the ring.

### 5.4 Sway envelope (for D1.6, not part of the static pose)

The tag was turned about its hole on three axes, in 1° steps up to 30°. The table lists the largest angle with no overlap, and what it meets first.

| Axis | Rect | Round | Long |
|---|---|---|---|
| pose Z, in the picture plane: clockwise on screen / anticlockwise | 30 / **8** (key) | 30 / 30 | 30 / **7** (key) |
| pose X: bottom toward the lens / away | 30 / **15** (ring) | 30 / 15 (ring) | 30 / 15 (ring) |
| wire tangent at the hole: − / + | 30 / **11** (key) | 30 / 30 | 30 / **9** (key) |

- **The P4 swing:** 25° at 0.25 s, 2.2 Hz, decay 2.5 / s, so the opposite peak is about 14° and about 7° remains at take-off. In the picture plane that swing would run the wide tags' top corner into the bow.
- **Options for D1.6:**
  - swing about pose X (toward and away from the lens), starting toward the lens;
  - or clamp the in-plane swing at 7° on the key side: the tag rests against the bow, as a real tag does.
- About 7° at take-off is safe on every axis.

## 6. For the next stage (scale, offset, rotation)

- **Offset (orthographic).** The anchor is the glyph's (36, 24) by construction, and the tag lands to within 0.00 mm. So any offset measured in Unity comes from perspective and the off-axis view, not from the layout.
- **The key is 3.2 mm in front of the tag** at its hole (the bow's top and bottom are +6.9 / −3.7 mm). At 0.35 m that makes the key about 1 % larger than the tag's plane.
- **Anisotropy built into the layout:** the key is ×0.972 tall against the glyph; the tag is ×1.000 both ways.
- **The ring** is the one part a single scale cannot reconcile (§3).
- **Colour also changes at the hand-off:** the brass key and chrome ring become the paper `#F4F1E8` key and ring of the glyph.

## 7. Open items

- **平面视觉 (their call; the masters are untouched):** the B glyph's ring is drawn face-on, and the held 3D ring reads as a narrow loop (0.40) between the bow hole and the tab hole (HP01–03, cyan versus magenta). If the P4 hand-off should not visibly reshape the ring, the glyph's ring could follow the 3D. Otherwise the ring changes shape at take-off, and the 720 ms ease-in-out start holds that frame for a moment.
- **D1.6 `FrontRoomsKeyAssembly.PoseHeld`:**
  - use the quaternions;
  - +X is the viewer's left;
  - the sway axis and clamp (§5.4).
- **Not covered here:** perspective, the off-axis foreshortening (27.5°), the phone FOV, and per-screen scale factors. That is the next stage.

## 8. Files

| Path | What |
|---|---|
| `held_pose/held_arrangement.json` | the data: per shape, per part {kit, localPosition, localEulerDeg, localRotation}; the anchor; points; checks; front-view metrics |
| `held_pose/images/HP01–HP06*.jpg`, `held_front*_<shape>.png` | images (§0) |
| `$W/heldkey/heldgeo.py` | frame, glyph composition, parameterisation, analytic pass-through test |
| `$W/heldkey/grid1.py`, `grid2.py`, `refine1.py`, `solve2.py`, `nm.py` | search and solve |
| `$W/heldkey/bkit.py`, `b_check.py`, `b_final.py`, `b_verify_json.py` | Blender BVH tests; results in `final_k0.40.json`, `verify_final.json` |
| `$W/heldkey/b_render.py`, `overlay.py`, `glyph_vec.py`, `tradeoff.py`, `finalize.py` | renders, overlays, glyph re-render, JSON writer |
