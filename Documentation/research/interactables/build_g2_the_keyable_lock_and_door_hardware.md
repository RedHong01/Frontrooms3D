# G2 build note: the keyable lock and door hardware

Status: built 2026-10-03 into the private clone `proj_int` only. Nothing under `Assets/` of the real project was touched, and Unity was not run. Spec: `10_spec.md` §3, §1.2, §2.1, §6.4, §9.2, §10.0, §10.2.

**What was built**
- 11 assets and 7 VARIANTS = 18 FBX files with JSON sidecars.
- Written to `scratchpad/proj_int/Assets/Resources/Props/Models/Kit_Lock_*`.
- 12 new modules in `Tools/Blender/frontrooms_kit/assets/`:
  - `interact_lock_common.py`: the helper. It has no `NAME`.
  - `interact_lock_escutcheon.py`, `_cylinder_shell`, `_plug`, `_knob`, `_deadbolt`, `_latch_mortise`, `_latch_bored`, `_rose`, `_lever`, `_strike_mortise`, `_strike_bored`.
- `kitlib.py` and `build_asset.py` were not edited.

**Build command**
```
Blender -b --factory-startup --python Tools/Blender/frontrooms_kit/build_asset.py -- \
  interact_lock_escutcheon interact_lock_cylinder_shell interact_lock_plug interact_lock_knob \
  interact_lock_deadbolt interact_lock_latch_mortise interact_lock_latch_bored interact_lock_rose \
  interact_lock_lever interact_lock_strike_mortise interact_lock_strike_bored \
  --out-root <scratchpad>/proj_int --preview-dir <scratchpad>/interact_previews/G2
```
Every module asserts the following, and the build fails if any check fails:
- its key numbers;
- its LOD0 triangle count, within ±15 % of §9.2;
- its Unity-space bounds.

## 1. Conventions every G2 asset follows

- **Part frame (§1.2).** Unity metres. The origin is the pivot or mount. Front = Unity +Z = kit −Y. For bolts, front is the throw direction. +Y is up.
  - Geometry is written in Unity coordinates and converted once with `U(X, Y, Z) = (−X, −Z, Y)`.
  - Every anchor below was read back from the exported sidecars and matches the spec.
- **Render only.** Every asset calls `kit.no_collider()`, carries the tags `interactable` and `lock_part`, and has no lights.
- **Slots.** The dominant slot comes first, so it becomes submesh 0 (§7.3 item 2, for the RT bridge).
  - Satin chrome is `Prop_Chrome`.
  - Brass is `Prop_Brass`: the plug, the core face, and the `_Brass` VARIANTS.
  - Dark cavities are `Prop_PlasticBlack`: keyway walls, pin chambers, the core groove, dust boxes, screw slots and countersink shadow rings.
  - The deadbolt's hardened inserts are `Prop_Aluminium`.
  - No new slots were added.
- **LOD.** `LOD1 = None` on every asset: all of them are under 1 m (§1.8, §9.2: "no").
  - Each sidecar carries `lodDistances`, `lodRatios` and `lodBudget`.
  - Parts that drop at LOD2 get `obj["fr_lod2_drop"] = True` before the join. These are screws, pins, the plunger and the insert dots.
  - The custom property does not survive `finish()`'s join. When P-1 lands, the property must be read before the join, as §8 P-1 already describes.
- **Wear (§1.8 W2).** Every part carries a `fr_wear` BYTE_COLOR face-corner attribute. 1 means untouched.
  - **R** is hand grime: knob crown and band, lever grip, the plate round the knob and the cylinder.
  - **G** is edge wear: the knob crown chamfer, the knurl crests, the plug face (0.80) and keyway mouth (0.65), the collar round, the strike lip and opening edges.
  - **B** is cavity: knurl grooves; every `Prop_PlasticBlack` face is ≤ 0.35.
  - Re-importing the FBX in Blender shows that the attribute **survives export**. This is evidence for P-3.
- **Motion meta** is in `kit.meta["motion"]`, copied to the sidecar.
  - The axes are in the part frame.
  - **On a mirrored placement (localScale.x = −1), negate the angle** to keep the visual direction. Unity applies the scale before the rotation. The self-check confirmed this: the lever goes down on both faces only when the angle is negated on `_p`.

## 2. Assets

Triangles are LOD0 from the sidecar. "Proud" is measured from the leaf face in the test door.

| Asset (+VARIANT) | Tris (budget) | Size (Unity X × Y × Z) | Origin | Anchors (part frame) | Motion |
|---|---|---|---|---|---|
| `Kit_Lock_Escutcheon` (+`_Brass`) | 3,206 (3,000, +6.9 %) | 0.0572 × 0.2032 × 0.008 | back-face centre on the leaf face | `knob_pivot` (0, −0.0315, 0.002); `cylinder` (0, 0.032, 0.002); `keyhole` (0, 0.032, 0.0095); `keyhole_in` (0, 0.032, −0.0905); `keyhole_up` (0, 0.132, 0.0095) | static |
| `Kit_Lock_CylinderShell` (+`_Brass`) | 2,751 (2,600, +5.8 %) | Ø 0.044 × 0.0075 | the keyhole | `keyhole` (0, 0, 0); `keyhole_in` (0, 0, −0.10); `keyhole_up` (0, 0.10, 0); `collar_back` (0, 0, −0.0075) | static |
| `Kit_Lock_Plug` | 1,908 (1,800, +6.0 %) | Ø 0.0115 × 0.030 | the keyhole | `keyhole`, `keyhole_in`, `keyhole_up`; `pin_1`…`pin_6` (0, 0.00384 / 0.00308 / 0.00422 / 0.00270 / 0.00346 / 0.00384, −0.004 … −0.023) | rotate axis (0, 0, −1), 0 → 90°, `turnResistDeg` 10 |
| `Kit_Lock_Knob` (+`_Brass`) | 4,608 (4,500, +2.4 %) | Ø 0.054 × 0.063 | spindle on the escutcheon face | `spindle` (0, 0, 0); `face` (0, 0, 0.063); `axis_out` (0, 0, 0.10) | rotate axis (0, 0, 1), −40 … +40° |
| `Kit_Lock_Deadbolt` | 380 (400, −5.0 %) | 0.0125 × 0.030 × 0.045 | lock front, bolt axis | `bolt_axis` (0, 0, 0); `end_face` (0, 0, 0.025); `throw_dir` (0, 0, 0.10) | slide axis (0, 0, −1), travel 0.025, rest "thrown" |
| `Kit_Lock_Latchbolt_Mortise` | 600 (600, ±0 %) | 0.0193 × 0.030 × 0.040 (latch + plunger) | lock front, latch axis | `bolt_axis`; `tip` (0.00555, 0, 0.019); `plunger` (0.0105, 0, 0.0025); `throw_dir` | slide (0, 0, −1), travel 0.019, rest "extended" |
| `Kit_Lock_Latchbolt_Bored` (+`_Brass`) | 476 (500, −4.8 %) | 0.016 × 0.020 × 0.030 | faceplate, latch axis | `bolt_axis`; `tip` (0.0043, 0, 0.013); `plunger` (0.009, 0, 0.002); `throw_dir` | slide (0, 0, −1), travel 0.013 |
| `Kit_Lock_Rose` (+`_Brass`) | 2,736 (2,400, +14.0 %) | Ø 0.070 × 0.010 | back-face centre on the leaf face | `spindle` (0, 0, 0.010) | static |
| `Kit_Lock_Lever` (+`_Brass`) | 3,566 (4,000, −10.8 %) | 0.133 × 0.024 × 0.054 | spindle on the rose face | `spindle` (0, 0, 0); `grip_press` (0.070, 0, 0.054); `grip_end` (0.121, 0, 0.033); `axis_out` | rotate axis (0, 0, −1), 0 → 35° (+ = grip end down) |
| `Kit_Lock_StrikeMortise` | 988 (900, +9.8 %) | 0.056 × 0.200 × 0.0254 (incl. boxes) | plate centre on the latch lining, placed `Euler(0, 180, 0)` | `plate`; `latch_opening` (0, −0.0315, 0.0002); `deadbolt_opening` (0, 0.032, 0.0002); `lip_tip` (−0.040, −0.0315, 0.001); `detach_dir` (0, 0, 0.10) | static, `detach: true` |
| `Kit_Lock_StrikeBored` (+`_Brass`) | 678 (600, +13.0 %) | 0.056 × 0.124 × 0.0174 | as above | `plate`; `latch_opening` (0, 0, 0.0002); `lip_tip` (−0.040, 0, 0.001); `detach_dir` | static, `detach: true` |

**Per-asset notes**
- **Escutcheon.** A 2-1/4" × 8" plate with corners at R 3.2 mm in plan.
  - A 1 mm front round in 3 segments, with a support ring so the flat face stays flat.
  - A 96-segment knob boss with a 0.6 mm fillet and a Ø 0.020 bore for the Ø 0.019 knob shank.
  - A 0.3 mm recess ring for the collar, with a Ø 13 mm plug clearance hole under it.
  - Two 48-segment oval-head slotted screws (Ø 7 mm, 1.2 mm dome, 0.8 mm slot) in dark-ringed countersinks.
- **Cylinder shell.** A 96-segment collar with a domed front and a 1 mm round, and a Ø 0.035 housing face with a 0.4 mm chamfer.
  - The brass figure-8 IC core sits flush at Z 0, with a dark 0.25 mm groove, the plug bore and the control-lug notch.
  - It is symmetric in X, so the §1.2 mirror does not change it.
- **Plug.** A 64-segment brass plug with a 0.3 mm face chamfer, which gives the dark shear line against the core bore.
  - The §3.2 keyway is broached 0.027 deep with dark walls and a 0.2 mm lead-in chamfer at the mouth.
  - Six Ø 3.0 mm pin chambers are drilled with 118° points into the keyway's top.
  - Six 12-segment domed brass pins hang there, each 0.2 mm above its cut floor.
- **Knob.** A 96-segment lathe:
  - a Ø 0.019 shank with a 3 mm fillet into a Ø 0.054 ball centred 0.047 out;
  - a turned band at Z 0.040–0.054 with a **72-flute straight knurl**: 288 vertices round, flutes 0.6 mm deep, run-out ramps at both ends;
  - a 0.8 mm crown chamfer;
  - a 0.9 mm domed face at Z 0.063.
- **Deadbolt.** A square end with a 1 mm chamfer and 1.1 mm long-edge radii. Two Ø 3 mm hardened inserts, 24 segments. Authored thrown.
- **Latchbolts.** Each latch is a rounded 30° bevel facing P (part −X) and a flat face toward S.
  - Edges have 0.4–0.5 mm rounds in 3 steps.
  - Each has a deadlatch plunger beside it on the S side, part of the same rigid mesh (see §4, item 6).
- **Rose.** A 96-segment stepped turning: a flange with a 1 mm round, a flat screw ring, a 4.5 mm cove, and a Ø 0.030 boss with a 0.5 mm chamfer.
  - Two 48-segment oval-head slotted screws, set in countersinks.
- **Lever.** A commercial return lever:
  - a 96-segment hub and neck;
  - the grip root is the 26-point section turned 180° (48 steps), so it reads as a disc in front view;
  - the section is swept along a straight run, a 14 mm bend toward the door and a rounded end;
  - it is joined to the neck by an EXACT union, so it is one closed shell.
- **Strikes.**
  - A 1.6 mm plate with a 0.3 mm front chamfer, standing 0.2 mm proud of the lining.
  - Openings 1.25 mm (X) and 1.5 mm (Y) larger than each bolt, with dark dust boxes.
  - A 24 mm lip with R 6 mm corners that curls 0.8 mm.
  - Two flat countersunk slotted screws (Ø 8.5 mm) in countersinks.

## 3. Self-check (Blender, scratchpad)

The script is `scratchpad/g2work/selfcheck.py`; results are in `interact_previews/G2/selfcheck/selfcheck.json`.
- It loads the **exported FBX files**. Their re-import bounds equal the sidecars.
- It places them in a scratch door root: a 44 mm leaf, X ±0.022, Y 0.015–2.095, Z 0.005–0.995, with S = +X.
- It uses the §6.4 anchors and the §1.2 face rules: `_s` is `Euler(0, 90, 0)`; `_p` is `Euler(0, −90, 0)` with scale (−1, 1, 1).
- Overlaps are triangle–triangle BVH intersections. Gaps are vertex-to-surface distances.

**(a) Both faces, every part at the §6.4 anchors**
- Locked set: escutcheon, shell, plug and knob on both faces, plus the deadbolt, mortise latch and mortise strike.
- Free set: rose and lever on both faces, plus the bored latch and bored strike.
- Results:
  - **0 overlapping part pairs** in either set.
  - **0 face-part vertices inside the slab.**
  - **Proud:** knob 0.065, lever 0.064, shell and plug 0.0095, escutcheon 0.008, rose 0.010.
  - **Composed door-root anchors:** `keyhole_s` (0.0315, 1.000, 0.920), `keyhole_p` (−0.0315, 1.000, 0.920), `knob_s` (0.024, 0.9365, 0.920), `lever_s` (0.032, 1.000, 0.920). All equal §6.4.
  - **The lever grip points at the hinge on both faces:** its far end is at door Z 0.799 on `_s` and on `_p`.
  - **The strikes clear a shut leaf:** 0 overlaps and a 2.8 mm gap. The lip curl stays within 1.0 mm of the lining.

**(b) Key fit, T8**
- The §3.2 blade was built exactly: the section, the six cuts and the tip, as an EXACT intersection of the section prism with the side silhouette.
- It was extruded 25 mm into the plug at full insertion. Key-local (X, Y, Z) maps to plug (−X, Y, −Z).
- The blade's top at cuts 1–5 equals the cut floors to 1 µm.
- Overlaps at 0°, 45° and 90° of joint rotation: **blade vs plug 0, blade vs shell 0, plug vs shell 0**. The minimum blade-to-plug-and-pins gap is 0.077 mm.
- **With the plug mirrored (the §1.2 `_p` rule): 64 overlapping triangle pairs.** See deviation 1.

**(c) Rotations**
- Knob at −40, −20, +20 and +40°, both faces: 0 overlaps. The shank is 0.2 mm above the boss-bore floor.
- Lever at 10, 20 and 35°, both faces with the angle negated on `_p`: 0 overlaps. The grip end drops to door Y 0.924 at 35°. It sits 0.4 mm off the rose boss.

**(d) Bolts through their travel** (t = 0, 0.25, 0.5, 0.75, 1)
- Each bolt was tested against a stand-in armor front and faceplate, cut to the openings in §4 item 6, and against its strike with the door shut. Result: **0 overlaps at every step.**
- Minimum gaps when thrown:
  - deadbolt and mortise latch to the strike opening: 1.25 mm;
  - mortise latch to the armor front: 0.75 mm;
  - bored latch to the faceplate: 0.5 mm;
  - bored latch to its strike: 1.5 mm.
- The thrown deadbolt's end is at door Z 1.019; its box floor is at 1.0224.

**(e) Renders.** These are in `interact_previews/G2/selfcheck/`; see §5.

**(f) Integration with G1's current exports.** These are `Kit_DoorLeaf_Steel` (3,930 tris) and `Kit_DoorFrame_Steel` (3,866) in the clone, as of 12:48; G1 may still iterate. The script is `g2work/g1_integration.py`; results are in `selfcheck/g1_integration.json`.
- **Swing, 0–95° in 5° steps about the A2 axis**, with every locked-set part on both faces and the bolts retracted: **0 overlaps with G1's frame.**
  - Minimum gap from the knob to the frame when shut: 35.8 mm (P face) and 51.1 mm (S face).
- **When shut, these overlaps are hidden and expected:**
  - The plug body runs 30 mm into G1's leaf, behind the escutcheon. G1's leaf has no bores, by §2.3.
  - The bolt tails run into the leaf.
  - The thrown bolts, the strike plate's back and the dust boxes run into the frame's lining and jamb.
  - The escutcheon's back face is coplanar with the leaf face. It faces into the leaf, so it is always culled.
- **Visible clash, needs G1:**
  - G1's armor-front openings are 0.0135 × 0.031 and its bored faceplate opening is 0.011 × 0.021. Both are centred on the bolt, with 0.5 mm round the bolt.
  - Neither opening includes the deadlatch plunger (X 0.008–0.013 mortise, 0.007–0.011 bored).
  - So the plunger currently comes straight out of the solid plate.
  - G1 should cut the openings in §4 item 6, or a second 0.006 × 0.019 slot beside each latch opening.
- **Facade rule:** the extended latch intersects the frame or strike at leaf angles of 0–6°. Keep the latch retracted (t = 1) while the leaf moves within about 8° of shut; extend it only when the leaf is shut or past 8°. The Open and knob beats in §3.4 already retract it.

## 4. Deviations, interface notes and decisions for others

1. **Never mirror the plug (deviation from §1.2's face rule).**
   - The keyway is chiral. The key arrives as a proper rotation, `LookRotation(−out, up)`.
   - So place `Kit_Lock_Plug` like a text part: `_s` is `Euler(0, 90, 0)` and `_p` is `Euler(0, −90, 0)`, **both with localScale (S_sign, 1, 1)**. The net transform in world space is then always a proper rotation.
   - With the §1.2 mirror, the key intersects the ward ribs (64 triangle pairs).
   - Every other G2 part is symmetric in X, or is meant to mirror (the lever), so they keep §1.2.
2. **Keyway frame.**
   - In the plug's own frame, the keyway is §3.2's key-local keyway mirrored in X, because the key is turned 180° about Y relative to the plug.
   - §3.2's "rib on the +X wall" is therefore key-local; in the plug that rib sits on the −X wall.
   - G3's key uses the key-local numbers unchanged. The exact polygon is in the plug sidecar under `keyway.plugFrame`.
3. **Cut 6 lies inside the tip bevel (spec conflict, for G3 and the spec owner).**
   - §3.2 puts cut 6 at Z 0.0230, but the tip bevel starts at Z 0.0215 and is already at Y 0.00282 there, below cut 6's floor (0.00364).
   - So cut 6 does not exist on the blade, and pin 6 hangs 0.82 mm above the bevel instead of 0.2 mm above a cut. T8 still passes.
   - To keep six visible cuts, start the tip bevel at Z ≥ 0.0234, after cut 6's flat. Or move the cuts 1.5 mm toward the shoulder.
4. **Deadbolt dust box is 0.023 deep, not 0.015.**
   - The thrown bolt ends at door Z 1.0189. The plate back is at 0.9994, so a 0.015 box would let the bolt poke 4 mm through its floor.
   - The latch box stays 0.015 deep: the latch tip is at 1.0129 and the floor at 1.0144.
5. **The strike face stands 0.2 mm proud of the lining** instead of being mortised flush, so it is never coplanar with G1's lining. G1's steel-frame strike pocket is welcome but not required.
   - The lip curls a further 0.8 mm, for 1.0 mm total above the lining: within §3.1's ≤ 1 mm.
   - The S-face latch corner keeps ≥ 2.0 mm, and the self-check measured 2.8 mm at the lining.
6. **Deadlatch plungers (interface for G1).** The spec's plunger sits "beside" the latch.
   - It is modelled on the S side, as a tall narrow bar: 0.005 × 0.018 mortise, 0.004 × 0.012 bored. It stands 2.5 mm / 2.0 mm out, with a round nose.
   - It is part of the latch's rigid mesh, so it slides with the latch; "fixed" means no motion of its own. A separately animated plunger would need its own asset.
   - At the shut door it lands on the strike plate beside the opening, as a real auxiliary latch does, never in the opening.
   - **G1's openings must therefore be:**
     - armor-front latch opening: X −0.0070 … +0.0140, Y 0.9200 … 0.9530;
     - deadbolt opening: X ±0.0070, Y 0.9835 … 1.0165;
     - bored faceplate opening: X −0.0057 … +0.0115, Y 0.9895 … 1.0105.
   - All are in door-root coordinates and also stored as `frontOpening` in the bolt sidecars.
7. **Motion signs.**
   - Plug: + = cuts toward part +X, which is the hinge on `_s`. With the text-rule placement, negate on `_p` to keep "toward the hinge"; with `S_sign` = −1, flip again.
   - Knob and lever on `_p` (mirrored): negate the angle.
   - §3.4's turn direction is UNVERIFIED (`05` §9).
8. **Bevel sizes below §1.8's "≥ 1 mm on hardware".**
   - The plates, roses, collar and deadbolt use 1 mm.
   - The 1.6 mm strike plates use a 0.3 mm front chamfer.
   - The latches use 0.4–0.5 mm rounds.
   - A 1 mm bevel on a 1.6 mm plate, or on a 10–12.5 mm latch, would read as a different part.
9. **Segment counts below §1.8's "≥ 48 for parts ≤ 0.07 m".** These were lowered only where §9.2's budget forces it, and each is ≥ 8 px wide at 0.3 m:
   - plug pins: 12 segments, Ø 2.9 mm, inside the dark keyway;
   - deadbolt inserts: 24 segments, Ø 3 mm;
   - mortise-strike screws: 32 segments;
   - bored-strike screws: 24 segments.
   - Roses, knob, collar and boss are 96 segments; the escutcheon and rose screws are 48.
10. **Close-up previews.** `kitlib.preview` clamps the framing radius to 0.15 m, so its `_a`/`_b` stills show these parts as specks. The review images are the close-ups and the self-check renders (§5).
11. **The shell's `_Brass` VARIANT** maps Chrome → Brass. That leaves two `Prop_Brass` submeshes, the housing and the core. This is harmless; the importer maps both.
12. **`fr_wear` domain.**
    - G2 paints the face-corner domain; G1, G3 and G4 paint the point domain.
    - Corner keeps the dark cavity mask sharp at boolean rims. There, one vertex is shared by a black wall and a long face triangle, so a per-point value would smear the mask across the whole face.
    - Both domains export as FBX vertex colours.
13. **Shot suggestion for the map chat (pose P).**
    - With the camera exactly on the face normal, a key held cuts-up is seen **edge-on** at 0°, a 2 mm brass line. It reads as a key only after the 90° turn (`e_poseP_key000.png` vs `_key090.png`).
    - Swinging pose P about 12° toward the hinge, round the keyhole's vertical, shows the bow at insertion (`e_poseP_offset12_key000.png`).
    - The alternative is a 10–15° key roll during the approach that settles to 0° before Insert.

## 5. Renders reviewed

`interact_previews/G2/selfcheck/`:
- `e_poseP_key000.png`, `e_poseP_key090.png` and `e_poseP_nokey.png`: the head-dip pose P. The camera is 0.45 m out along the face normal and 0.37 m above `keyhole_s`, looking at it with a vertical FOV of 62°.
- `e_ic_face_0p3m.png` and `e_ic_face_zoom.png`.
- `e_knob_knurl_0p3m.png` and `e_knob_knurl_zoom.png`.
- `e_locked_s_1p5m.png` and `e_locked_p_1p5m.png`.
- `e_free_s_1m_brass.png`, `e_free_lever_0p3m.png` and `e_free_s_lever35.png`.

Per-asset close-ups are in `scratchpad/g2work/cu/`. The standard kit stills are in `interact_previews/G2/`.

(Render review notes: see the update below.)

## 6. ESTIMATE dimensions used

**From the spec:**
- the IC core: lobes Ø 0.0127 at 0.0095 centres, a 0.25 mm groove, the plug bore Ø 0.0118;
- the keyway and the bitting;
- the escutcheon cylinder-to-hub distance of 0.0635;
- the mortise bolt sections.

**Mine:**
- **Cut flanks.** "50° flanks" is read as each flank 50° off vertical (100° included).
- **Escutcheon.** Corner R 3.2 mm; boss bore Ø 0.020; countersink Ø 7.3 mm.
- **Collar.** The domed front falls 1 mm from r 0.0176 to r 0.021.
- **Plug.** Pin chambers Ø 3.0 mm with 118° points reaching Y 0.0022; hemispherical pin tips; pins top out at Y 0.0053.
- **Knob.** Ball centre Z 0.047; 3 mm neck fillet; 0.6 mm band ledges; 0.9 mm face dome; crown chamfer from Z 0.0610 to 0.0617.
- **Deadbolt.** Long-edge radius 1.1 mm; inserts Ø 3 mm at Y ±0.0075.
- **Latches.**
  - The bevel is 30° to the bolt axis, as a chord with 0.5–0.6 mm sagitta, a 0.4–0.5 mm tip land and a 0.4–0.5 mm tip radius.
  - Plunger centres are X +0.0105 (mortise) and +0.009 (bored).
- **Rose.** Cove R 4.5 mm; screw ring flat from r 0.0199 to 0.034.
- **Lever.** Hub r 0.012 to Z 0.0115; neck Ø 0.0176; section corner R 5 mm; bend R 14 mm on the centre line; 6 mm rounded end.
- **Strikes.**
  - Plate corner R 2 mm.
  - Openings X ±0.0075 × Y ±0.0165 (mortise) and X ±0.0065 × Y ±0.0115 (bored).
  - Lip Y ±0.016 round the latch, from X −0.016 to −0.040.
  - Curl 0.8 mm over the outer 14 mm.
  - Screws Ø 8.5 mm flat heads at Y ±0.085 (mortise) and ±0.047 (bored).

## 7. The glass RT track (Red's relayed request)

Red's request this run was to understand ChatGPT's independent ray-traced glass route and add it as a task.
- That route is described in `10_spec.md` §7. It is tracked as G14, plus the new rows G14-K1…K5.
- G2 touches none of its files.
- What G2 does for it: every asset puts its dominant slot in submesh 0 (§7.3 item 2). The knob and lever are single-slot, so the bridge's "submesh 0 only" limit loses nothing on them.
