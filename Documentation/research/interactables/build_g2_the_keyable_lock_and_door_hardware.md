# G2 build note: the keyable lock and door hardware

Status: built 2026-10-03 into the private clone `proj_int` only. Run 1 (12:0x–13:1x) built every asset; its self-check renders were cut off and three of them showed the wrong materials (§8.1). **Run 2 (16:4x–17:xx, "try again")** rebuilt everything, cleaned the topology, refined the lever, re-ran the whole self-check against the final exports and G3's exported key, and finished the renders. Nothing under `Assets/` of the real project was touched, and Unity was not run. Spec: `10_spec.md` §3, §1.2, §2.1, §6.4, §9.2, §10.0, §10.2.

**What was built**
- 11 assets and 7 VARIANTS = 18 FBX files with JSON sidecars, in `scratchpad/proj_int/Assets/Resources/Props/Models/Kit_Lock_*`.
- 12 new modules in `Tools/Blender/frontrooms_kit/assets/`:
  - `interact_lock_common.py`: the helper. It has no `NAME`.
  - `interact_lock_escutcheon.py`, `_cylinder_shell`, `_plug`, `_knob`, `_deadbolt`, `_latch_mortise`, `_latch_bored`, `_rose`, `_lever`, `_strike_mortise`, `_strike_bored`.
- `kitlib.py` and `build_asset.py` were not edited. No new material slots.

**Build command**
```
Blender -b --factory-startup --python Tools/Blender/frontrooms_kit/build_asset.py -- \
  interact_lock_escutcheon interact_lock_cylinder_shell interact_lock_plug interact_lock_knob \
  interact_lock_deadbolt interact_lock_latch_mortise interact_lock_latch_bored interact_lock_rose \
  interact_lock_lever interact_lock_strike_mortise interact_lock_strike_bored \
  --out-root <scratchpad>/proj_int --preview-dir <scratchpad>/interact_previews/G2
```
Every module asserts its key numbers, its LOD0 triangle count (within ±15 % of §9.2) and its Unity-space bounds. The build fails if any check fails.

## 1. Conventions every G2 asset follows

- **Part frame (§1.2).** Unity metres. The origin is the pivot or mount. Front = Unity +Z = kit −Y. For bolts, front is the throw direction. +Y is up.
  - Geometry is written in Unity coordinates and converted once with `U(X, Y, Z) = (−X, −Z, Y)` (the inverse of kitlib's export mapping).
  - Every anchor below was read back from the exported sidecars and matches the spec.
- **Render only.** Every asset calls `kit.no_collider()`, carries the tags `interactable` and `lock_part`, and has no lights.
- **Slots.** The dominant slot comes first, so it becomes submesh 0 (§7.3 item 2).
  - Satin chrome is `Prop_Chrome`. Brass is `Prop_Brass`: the plug, the IC core face, and the `_Brass` VARIANTS.
  - Dark cavities are `Prop_PlasticBlack`: keyway walls, pin chambers, the core groove, dust boxes, screw slots and countersink shadow rings.
  - The deadbolt's hardened inserts are `Prop_Aluminium`.
- **LOD.** `LOD1 = None` on every asset: all are under 1 m, and today's importer would cull them at about 1.4 m (§1.8, §9.2 "no").
  - Each sidecar carries `lodDistances`, `lodRatios` and `lodBudget` (§9.2).
  - Parts that drop at LOD2 (screws, pins, plungers, insert dots) get `obj["fr_lod2_drop"] = True`. **Run 2:** they also get an `fr_lod2_drop` vertex group, because the custom property is lost in `finish()`'s join and the vertex group survives it (the same way kitlib's own `fr_lod1_drop` bookkeeping does). FBX does not export unskinned vertex groups, so Unity sees no change.
- **Wear (§1.8 W2).** Every part carries an `fr_wear` BYTE_COLOR attribute. 1 means untouched.
  - **R** is hand grime: knob crown and band, lever grip, the plate round the knob and the cylinder.
  - **G** is edge wear: the knob crown chamfer, the knurl crests, the plug face (0.80) and keyway mouth (0.65), the collar round, the strike lip and opening edges.
  - **B** is cavity: knurl grooves; every `Prop_PlasticBlack` face is ≤ 0.35.
  - It is painted in the face-corner domain (G1, G3 and G4 use point). Corner keeps the cavity mask sharp at boolean rims, where one vertex is shared by a black wall and a long face triangle. Both domains export as FBX vertex colours; the re-imported FBX has the attribute.
- **Motion meta** is in `kit.meta["motion"]`, copied to the sidecar. Axes are in the part frame and follow Unity's numeric `AngleAxis` convention. **On a mirrored placement (localScale.x = −1), negate the angle** to keep the visual direction.

## 2. Assets

Triangles are LOD0 from the final sidecars. Proud is measured from the leaf face in the test door.

| Asset (+VARIANT) | Tris (budget) | Size (Unity X × Y × Z) | Origin | Anchors (part frame) | Motion |
|---|---|---|---|---|---|
| `Kit_Lock_Escutcheon` (+`_Brass`) | 3,176 (3,000, +5.9 %) | 0.0572 × 0.2032 × 0.008 | back-face centre on the leaf face | `knob_pivot` (0, −0.0315, 0.002); `cylinder` (0, 0.032, 0.002); `keyhole` (0, 0.032, 0.0095); `keyhole_in` (0, 0.032, −0.0905); `keyhole_up` (0, 0.132, 0.0095) | static |
| `Kit_Lock_CylinderShell` (+`_Brass`) | 2,708 (2,600, +4.2 %) | Ø 0.044 × 0.0075 | the keyhole | `keyhole` (0, 0, 0); `keyhole_in` (0, 0, −0.10); `keyhole_up` (0, 0.10, 0); `collar_back` (0, 0, −0.0075) | static |
| `Kit_Lock_Plug` | 1,884 (1,800, +4.7 %) | Ø 0.0115 × 0.030 | the keyhole | `keyhole`, `keyhole_in`, `keyhole_up`; `pin_1`…`pin_6` at Z −0.0040 … −0.0230, Y 0.00384 / 0.00308 / 0.00422 / 0.00270 / 0.00346 / 0.00384 | rotate, axis (0, 0, −1), 0 → 90°, `turnResistDeg` 10 |
| `Kit_Lock_Knob` (+`_Brass`) | 4,608 (4,500, +2.4 %) | Ø 0.054 × 0.063 | spindle on the escutcheon face | `spindle` (0, 0, 0); `face` (0, 0, 0.063); `axis_out` (0, 0, 0.10) | rotate, axis (0, 0, 1), −40 … +40° |
| `Kit_Lock_Deadbolt` | 380 (400, −5.0 %) | 0.0125 × 0.030 × 0.045 | lock front, bolt axis | `bolt_axis` (0, 0, 0); `end_face` (0, 0, 0.025); `throw_dir` (0, 0, 0.10) | slide, axis (0, 0, −1), travel 0.025, rest "thrown" |
| `Kit_Lock_Latchbolt_Mortise` | 600 (600, ±0 %) | 0.0193 × 0.030 × 0.040 (latch + plunger) | lock front, latch axis | `bolt_axis`; `tip` (0.0055, 0, 0.019); `plunger` (0.0105, 0, 0.0025); `throw_dir` | slide (0, 0, −1), travel 0.019, rest "extended" |
| `Kit_Lock_Latchbolt_Bored` (+`_Brass`) | 476 (500, −4.8 %) | 0.016 × 0.020 × 0.030 | faceplate, latch axis | `bolt_axis`; `tip` (0.0043, 0, 0.013); `plunger` (0.009, 0, 0.002); `throw_dir` | slide (0, 0, −1), travel 0.013, rest "extended" |
| `Kit_Lock_Rose` (+`_Brass`) | 2,728 (2,400, +13.7 %) | Ø 0.070 × 0.010 | back-face centre on the leaf face | `spindle` (0, 0, 0.010) | static |
| `Kit_Lock_Lever` (+`_Brass`) | 3,974 (4,000, −0.7 %) | 0.133 × 0.024 × 0.054 | spindle on the rose face | `spindle` (0, 0, 0); `grip_press` (0.070, 0, 0.054); `grip_end` (0.121, 0, 0.033); `axis_out` (0, 0, 0.10) | rotate, axis (0, 0, −1), 0 → 35° (+ = grip end down) |
| `Kit_Lock_StrikeMortise` | 988 (900, +9.8 %) | 0.056 × 0.200 × 0.0254 (incl. boxes) | plate centre on the latch lining; placed `Euler(0, 180, 0)` | `plate`; `latch_opening` (0, −0.0315, 0.0002); `deadbolt_opening` (0, 0.032, 0.0002); `lip_tip` (−0.040, −0.0315, 0.001); `detach_dir` (0, 0, 0.10) | static, `detach: true` |
| `Kit_Lock_StrikeBored` (+`_Brass`) | 674 (600, +12.3 %) | 0.056 × 0.124 × 0.0174 | as above | `plate`; `latch_opening` (0, 0, 0.0002); `lip_tip` (−0.040, 0, 0.001); `detach_dir` | static, `detach: true` |

The plug sidecar also carries `keyway.plugFrame` (the exact keyway polygon in the plug's frame), `cutZ` and `pinTipY`. The bolt sidecars carry `frontOpening`; the strike sidecars carry `openings`.

**Per-asset notes**
- **Escutcheon.** A 2-1/4" × 8" plate, corners R 3.2 mm in plan.
  - A 1 mm front round in 3 segments, with a support ring so the flat face keeps flat normals.
  - A 96-segment knob boss (Ø 0.030 × 0.006) with a 0.6 mm fillet and a Ø 0.020 bore round the Ø 0.019 knob shank (a 0.5 mm dark ring).
  - A 0.3 mm recess ring under the collar (a dark shadow line), with a Ø 13 mm plug clearance hole.
  - Two 48-segment oval-head slotted screws (Ø 7 mm, 1.2 mm dome, 0.8 mm slot) in dark-ringed countersinks.
- **Cylinder shell.** A 96-segment collar with a domed front and a 1 mm round; a Ø 0.035 housing face with a 0.4 mm chamfer.
  - The brass figure-8 IC core sits flush at Z 0, with a dark 0.25 mm groove, the Ø 0.0118 plug bore and the control-lug notch at the top of the upper lobe. No brand.
  - It is symmetric in X, so the §1.2 mirror does not change it.
- **Plug.** A 64-segment brass plug with a 0.3 mm face chamfer: with the 0.15 mm bore clearance it gives a 0.45 mm dark shear line against the core.
  - The §3.2 keyway is broached 0.027 deep with dark walls and a 0.2 mm lead-in chamfer at the mouth.
  - Six Ø 3.0 mm pin chambers are drilled with 118° points into the keyway's top. Six 12-segment domed brass pins hang there, each 0.2 mm above its cut floor.
- **Knob.** A 96-segment lathe: a Ø 0.019 shank with a 3 mm fillet into a Ø 0.054 ball centred 0.047 out; a turned band at Z 0.040–0.054 with a **72-flute straight knurl** (288 vertices round, flutes 0.6 mm deep, ledges at both ends); a 0.8 mm crown chamfer; a 0.9 mm domed face at Z 0.063. The knurl is the UFAS 1984 tactile mark (`02` §3.1).
- **Deadbolt.** A square end with a 1 mm chamfer and 1.1 mm long-edge radii; two Ø 3 mm hardened inserts (24 segments). Authored thrown.
- **Latchbolts.** A rounded 30° bevel facing P (part −X) and a flat face toward S; 0.4–0.5 mm edge rounds in 3 steps. The deadlatch plunger stands beside the latch on the S side, in the latch's rigid mesh (§4 item 6).
- **Rose.** A 96-segment stepped turning: a flange with a 1 mm round, a flat screw ring, a 4.5 mm cove, and a Ø 0.030 boss with a 0.5 mm chamfer; two 48-segment oval-head slotted screws in countersinks.
- **Lever.** A commercial return lever:
  - a 96-segment hub and neck;
  - the grip root is the half-section turned 180° (48 steps), so it reads as a disc in front view;
  - a 30-point section with R 5 mm corners in 6 steps, swept along a straight run, a 16-step R 14 mm bend toward the door and a rounded end;
  - one closed shell (EXACT union of hub and grip).
- **Strikes.** A 1.6 mm plate with a 0.3 mm front chamfer, 0.2 mm proud of the lining; openings 1.25 mm (X) and 1.5 mm (Y) larger than each bolt, with dark dust boxes; a 24 mm lip with R 6 mm corners that curls 0.8 mm; two flat countersunk slotted screws (Ø 8.5 mm).

## 3. Self-check (Blender, scratchpad)

Scripts: `scratchpad/g2work/selfcheck2.py` (checks and renders) and `g2work/g1_integration2.py`. Results: `interact_previews/G2/selfcheck_r2/selfcheck.json` and `g1_integration.json`.
- The scripts load the **exported FBX files** (re-import bounds equal the sidecars).
- They place them in a scratch door root: a 44 mm leaf, X ±0.022, Y 0.015–2.095, Z 0.005–0.995, S = +X.
- Placement uses the §6.4 anchors and the §1.2 face rules: `_s` is `Euler(0, 90, 0)`; `_p` is `Euler(0, −90, 0)` with scale (−1, 1, 1). The plug is the exception (§4 item 1).
- Overlaps are triangle–triangle BVH intersections. Gaps are vertex-to-surface distances.

**(a) Both faces, every part at the §6.4 anchors**
- Locked set: escutcheon, shell, plug and knob on both faces, plus the deadbolt, mortise latch and mortise strike. Free set: rose and lever on both faces, plus the bored latch and bored strike.
- **0 overlapping part pairs** in either set; **0 face-part vertices inside the slab.**
- **Proud:** knob 0.065, lever 0.064, shell and plug 0.0095, escutcheon 0.008, rose 0.010 (limit 0.065).
- **Composed door-root anchors** equal §6.4: `keyhole_s` (0.0315, 1.000, 0.920), `keyhole_p` (−0.0315, 1.000, 0.920), `knob_s` (0.024, 0.9365, 0.920), `lever_s` (0.032, 1.000, 0.920).
- **The lever grip points at the hinge on both faces:** its far end is at door Z 0.799 on `_s` and `_p`.
- **The strikes clear a shut leaf:** 0 overlaps, 2.8 mm gap; the lip stays within 1.0 mm of the lining.

**(b) Key fit, T8**
- **§3.2 blade:** the section, the six cuts and the tip, built as an EXACT intersection of the section prism with the side silhouette, extruded 25 mm into the plug at full insertion (key-local (X, Y, Z) maps to plug (−X, Y, −Z)). The blade's top at cuts 1–5 equals the cut floors to 1 µm.
  - At 0°, 45° and 90° of joint rotation: **blade vs plug 0, blade vs shell 0, plug vs shell 0.** Minimum gap 0.077 mm.
- **Run 2: G3's exported `Kit_Key_Zone`** (2,184 tris, exported 16:45), same three angles:
  - **blade vs plug 0, blade vs shell 0**; every blade vertex is ≥ 0.15 mm from the plug (the keyway offset).
  - The shoulder seats on the plug and core face at Z 0, as §3.3 intends: 21 contact pairs, all coplanar. Lifted 0.02 mm, the shoulder and bow overlap nothing (0 vs plug, 0 vs shell).
  - Pin tips hang **0.2 mm** above G3's blade at cuts 1–5. Cut 6 does not exist on either key (§4 item 3).
- **With the plug mirrored (the §1.2 `_p` rule): 64 overlapping triangle pairs.** See §4 item 1.

**(c) Rotations**
- Knob at −40, −20, +20 and +40°, both faces: 0 overlaps. The shank is 0.2 mm above the boss-bore floor.
- Lever at 10, 20 and 35°, both faces, angle negated on `_p`: 0 overlaps. At 35° the grip end drops to door Y 0.924, and the hub sits 0.4 mm off the rose boss.

**(d) Bolts through their travel** (t = 0, 0.25, 0.5, 0.75, 1)
- Against a stand-in armor front and faceplate cut to the openings in §4 item 6, and against the strike with the door shut: **0 overlaps at every step.**
- Minimum gaps when thrown: deadbolt and mortise latch to the strike opening 1.25 mm; mortise latch to the armor front 0.75 mm; bored latch to the faceplate 0.5 mm; bored latch to its strike 1.5 mm.
- The thrown deadbolt ends at door Z 1.019; its box floor is at 1.0224.
- **Run 2, for G1's question** (steel frames under free doors carry the 0.200 mortise prep): the bored latch run into `Kit_Lock_StrikeMortise`'s upper (deadbolt) opening at Y 1.000 gives **0 overlaps at every step** and a 1.9 mm minimum gap. So G1's recommendation works: the facade can put the mortise strike on every steel frame. There is no lip at Y 1.000, so the "latch retracted within 8° of shut" facade rule below is needed there too.

**(e) Renders.** §5.

**(f) Integration with G1's current exports** (`Kit_DoorLeaf_Steel` 13:02, `Kit_DoorFrame_Steel` 12:59; unchanged since run 1).
- **Swing, 0–95° in 5° steps about the A2 axis**, every locked-set part on both faces, bolts retracted: **0 overlaps with G1's frame.** Minimum knob-to-frame gap when shut: 35.8 mm (P face), 51.1 mm (S face).
- **Hidden and expected when shut:** the plug body runs 30 mm into G1's leaf, behind the escutcheon (G1's leaf has no bores, by §2.3); the bolt tails run into the leaf; the thrown bolts, the strike's back and the dust boxes run into the frame's lining and jamb. The escutcheon's back face is coplanar with the leaf face: lifted 0.02 mm, the escutcheon, shell and knob overlap the leaf **0** times on both faces, so it is contact only.
- **Visible clash, needs G1 (still open):** G1's armor-front openings are 0.0135 × 0.031 and its bored faceplate opening is 0.011 × 0.021, centred on the bolt. Neither includes the deadlatch plunger (X 0.008–0.013 mortise, 0.007–0.011 bored), so the plunger comes straight out of the solid plate. G1 should cut the openings in §4 item 6, or a second 0.006 × 0.019 slot beside each latch opening.
- **Facade rule:** the extended latch intersects the frame or strike at leaf angles of 0–6°. Keep the latch retracted (t = 1) while the leaf is within about 8° of shut; extend it only when the leaf is shut or past 8°. The Open and knob beats in §3.4 already retract it.

**(g) Topology (run 2).** Audit of the re-imported FBX files (`g2work/topo.py`):
- Zero-area triangles: run 1 had 21 (escutcheon), 39 (shell), 24 (plug), 8 (rose) and 84 (lever), all from EXACT booleans and the lever union. **Run 2 has 0 everywhere**: `tidy()` now runs `bmesh.ops.dissolve_degenerate` at 1 µm before triangulating.
- Remaining slivers (< 1 µm high, 0.1–0.3 mm long): 23 (escutcheon), 21 (rose), 3 (bored strike), all on the dark slot floors of the screw heads. Harmless.
- Open boundaries are deliberate and hidden: the deleted screw undersides inside countersinks, the shell and rose backs on the leaf, and the open dust boxes.
- The knob, plug, bolts, latches and lever are closed shells.

## 4. Deviations, interface notes and decisions for others

1. **Never mirror the plug (deviation from §1.2's face rule).**
   - The keyway is chiral. The key arrives as a proper rotation, `LookRotation(−out, up)`.
   - Place `Kit_Lock_Plug` like a text part: `_s` is `Euler(0, 90, 0)` and `_p` is `Euler(0, −90, 0)`, **both with localScale (S_sign, 1, 1)**, so the net world transform is always a proper rotation.
   - With the §1.2 mirror, the key intersects the ward ribs (64 triangle pairs).
   - Every other G2 part is symmetric in X or is meant to mirror (the lever), so they keep §1.2.
2. **Keyway frame.** In the plug's own frame, the keyway is §3.2's key-local keyway mirrored in X, because the key is turned 180° about Y relative to the plug. So §3.2's "rib on the +X wall" is key-local; in the plug it sits on the −X wall. G3's key uses the key-local numbers unchanged (checked in §3 (b)). The exact polygon is in the plug sidecar under `keyway.plugFrame`.
3. **Cut 6 lies inside the tip bevel (spec conflict, for the spec owner).**
   - §3.2 puts cut 6 at Z 0.0230, but the tip bevel starts at Z 0.0215 and is already at Y 0.00282 there, below cut 6's floor (0.00364). Neither G2's test blade nor G3's key has a cut 6.
   - Pin 6 hangs 0.82 mm above the bevel instead of 0.2 mm above a cut. T8 still passes. The pin is inside the dark keyway, so this does not show.
   - To get six visible cuts, start the tip bevel at Z ≥ 0.0234, after cut 6's flat, or move the cuts 1.5 mm toward the shoulder. G2 and G3 must change together.
4. **Deadbolt dust box is 0.023 deep, not 0.015.** The thrown bolt ends at door Z 1.0189 and the plate back is at 0.9994, so a 0.015 box would let the bolt poke 4 mm through its floor. The latch box stays 0.015 (tip at 1.0129, floor at 1.0144). G1 notes that the map's wall end and trims sit at Z 1.0, so only about 2 mm of any box shows until the map stops building its door trims.
5. **The strike face stands 0.2 mm proud of the lining** instead of mortised flush, so it is never coplanar with G1's lining. The lip curls a further 0.8 mm, 1.0 mm total (§3.1: ≤ 1 mm). The S-face latch corner keeps 2.8 mm.
6. **Deadlatch plungers (interface for G1).**
   - The plunger is a tall narrow bar on the S side: 0.005 × 0.018 (mortise) and 0.004 × 0.012 (bored), standing 2.5 / 2.0 mm out with a round nose.
   - It is part of the latch's rigid mesh and slides with it; "fixed" means it has no motion of its own. A separately animated plunger would need its own asset.
   - At the shut door it lands on the strike plate beside the opening, as a real auxiliary latch does.
   - **G1's openings must be** (door-root coordinates; also `frontOpening` in the bolt sidecars):
     - armor-front latch opening: X −0.0070 … +0.0140, Y 0.9200 … 0.9530;
     - deadbolt opening: X ±0.0070, Y 0.9835 … 1.0165;
     - bored faceplate opening: X −0.0057 … +0.0115, Y 0.9895 … 1.0105.
7. **Motion signs.**
   - Plug: + turns the cuts toward part +X, which is the hinge on `_s`. With the text-rule placement, negate on `_p` to keep "toward the hinge"; with `S_sign` = −1, flip again.
   - Knob and lever on `_p` (mirrored): negate the angle.
   - §3.4's real turn direction is UNVERIFIED (`05` §9).
8. **Bevels below §1.8's "≥ 1 mm on hardware".** Plates, roses, collar and deadbolt use 1 mm. The 1.6 mm strike plates use a 0.3 mm chamfer and the latches 0.4–0.5 mm rounds; a 1 mm bevel there would read as a different part.
9. **Segment counts below §1.8's "≥ 48 for parts ≤ 0.07 m"**, only where §9.2's budget forces it, each ≥ 8 px wide at 0.3 m: plug pins 12 (Ø 2.9 mm, inside the dark keyway), deadbolt inserts 24, mortise-strike screws 32, bored-strike screws 24. Roses, knob, collar and boss are 96; escutcheon and rose screws 48.
10. **Kit previews.** `kitlib.preview` clamps the framing radius to 0.15 m (`kitlib.py:848`), so the `_a`/`_b` stills show these parts small. The review images are the close-ups and the self-check renders (§5).
11. **The shell's `_Brass` VARIANT** maps Chrome → Brass, which leaves two `Prop_Brass` submeshes (housing and core). Harmless; the importer maps both.
12. **Shot note for the map chat (pose P).** With the camera on the face normal, the key held cuts-up is seen **edge-on** at 0° (a 2 mm brass line) and reads as a key only after the 90° turn (`e_poseP_key000` vs `e_poseP_key090`). Swinging pose P about 12° toward the hinge round the keyhole's vertical shows the bow at insertion (`e_poseP_offset12_key000`). The alternative is a 10–15° key roll during the approach that settles to 0° before Insert.

## 5. Renders reviewed

`scratchpad/interact_previews/G2/selfcheck_r2/` (run 2, correct slot materials, G3's exported key). The locked-set images were rendered from the run 2 rebuild taken just before the degenerate cleanup; the cleanup only removes zero-area triangles, so they show the final shapes. The free-door images were rendered from the final exports (refined lever). The numeric checks in §3 were re-run on the final exports.
- `e_poseP_key000.png`, `e_poseP_key090.png`, `e_poseP_nokey.png`: head-dip pose P on the S face. Camera 0.45 m out along the face normal and 0.37 m above `keyhole_s`, looking at it, vertical FOV 62°.
- `e_poseP_pface_key000.png`, `e_poseP_pface_key090.png`: the same pose on the P face (mirrored escutcheon and knob, text-rule plug).
- `e_poseP_offset12_key000.png`: the 12° shot suggestion.
- `e_ic_face_0p3m.png`, `e_ic_face_zoom.png`: the IC face at 0.3 m.
- `e_knob_knurl_0p3m.png`, `e_knob_knurl_zoom.png`: the knurl at 0.3 m.
- `e_locked_s_1p5m.png`, `e_locked_p_1p5m.png`: the locked set at 1.5 m, both faces.
- `e_free_s_1m_brass.png`, `e_free_lever_0p3m.png`, `e_free_s_lever35.png`: the Lobby free set (brass VARIANTS), lever at rest and at 35°.

Per-asset close-ups: `scratchpad/g2work/cu/` (run 1; still valid for every part except the lever), `g2work/cu2/` (run 1 lever bend, before the refinement) and `g2work/cu4/lever_final_sheet.jpg` (final lever: front, bend, return). Kit stills (rebuilt in run 2): `interact_previews/G2/Kit_Lock_*_a/_b.png`.

## 6. ESTIMATE dimensions used

**From the spec (ESTIMATE there):** the IC core (lobes Ø 0.0127 at 0.0095 centres, 0.25 mm groove, plug bore Ø 0.0118); the keyway and the bitting; the escutcheon cylinder-to-hub distance 0.0635; the mortise bolt sections.

**Mine:**
- **Cut flanks.** "50° flanks" read as each flank 50° off vertical (100° included). G3 uses the same reading.
- **Escutcheon.** Corner R 3.2 mm; boss bore Ø 0.020; countersink Ø 7.3 mm.
- **Collar.** The domed front falls 1 mm from r 0.0176 to r 0.021.
- **Plug.** Pin chambers Ø 3.0 mm with 118° points reaching Y 0.0022; hemispherical pin tips; pins top out at Y 0.0053.
- **Knob.** Ball centre Z 0.047; 3 mm neck fillet; 0.6 mm band ledges; 0.9 mm face dome; crown chamfer from Z 0.0610 to 0.0617.
- **Deadbolt.** Long-edge radius 1.1 mm; inserts Ø 3 mm at Y ±0.0075.
- **Latches.** Bevel 30° to the bolt axis, as a chord with 0.5–0.6 mm sagitta, a 0.4–0.5 mm tip land and a 0.4–0.5 mm tip radius. Plunger centres X +0.0105 (mortise) and +0.009 (bored).
- **Rose.** Cove R 4.5 mm; screw ring flat from r 0.0199 to 0.034.
- **Lever.** Hub r 0.012 to Z 0.0115; neck Ø 0.0176; section corner R 5 mm; bend R 14 mm on the centre line; 6 mm rounded end.
- **Strikes.** Plate corner R 2 mm. Openings X ±0.0075 × Y ±0.0165 (mortise) and X ±0.0065 × Y ±0.0115 (bored). Lip Y ±0.016 round the latch, from X −0.016 to −0.040; curl 0.8 mm over the outer 14 mm. Screws Ø 8.5 mm flat heads at Y ±0.085 (mortise) and ±0.047 (bored).

## 7. The glass RT track (run 1's relayed request)

Covered by `10_spec.md` §7 and the G14 rows. G2 touches none of its files. What G2 does for it: every asset puts its dominant slot in submesh 0 (§7.3 item 2). The knob and lever are single-slot, so the bridge's "submesh 0 only" limit loses nothing on them.

## 8. Run 2: what changed and why

### 8.1 Self-check render bug (run 1)
Run 1's self-check renders mapped materials by exact slot name. Blender renames a re-imported material that already exists (`Prop_Brass.001`), and the script then fell back to chrome. So in run 1's `e_ic_face_*` and pose P images **the plug and the keyway walls rendered as chrome**, not brass and black. The models were right; the check images were wrong. `selfcheck2.py` strips the suffix. All run 2 images show the correct slots.

### 8.2 Iteration 1: topology
- `interact_lock_common.tidy()` collapses zero-area triangles and sub-micron edges (`DEGENERATE_DIST` 1 µm) before triangulating. Results in §3 (g). Triangle counts dropped by 4–94 per asset; all still within ±15 %.
- `lod2_drop()` adds the `fr_lod2_drop` vertex group (§1).

### 8.3 Iteration 2: form, after the run 2 renders
- **Lever.** The return bend is the 0.3 m silhouette and the section corners carry the highlight. The bend went from 12 to 16 steps and the section corners from 5 to 6 steps (26 → 30 points). That used the budget run 1 left unspent (3,566 → 3,974 tris, −0.7 % of 4,000). Selfcheck (a)–(d) re-run on the final export.
- **IC face, corrected materials (`e_ic_face_zoom`).** Brass plug with the dark ward-ribbed keyway, a dark shear line, the brass figure-8 core with its groove and control-lug notch, chrome housing face and collar. It reads at 0.3 m (`e_ic_face_0p3m`): the "8" and the keyway are clear. No change needed.
- **Pose P (`e_poseP_*`).** The keyway, plug, collar, escutcheon and knob are all in frame on both faces. The P-face image matches the S face mirrored, with the frame on the other side, which confirms the face rules visually. At 90° the key's bow faces the camera; at 0° it is edge-on (shot note §4 item 12).
- Knob, escutcheon, rose, strikes and bolts: no form change. Run 1's close-ups of the current modules show no shading faults. The escutcheon "fan" streak in an early run 1 close-up is gone in the current module (re-rendered: `g2work/cu3/`).

### 8.4 Open issues
1. **G1:** cut the plunger openings (§4 item 6). This is the only visible clash left.
2. **Spec owner + G3:** cut 6 vs the tip bevel (§4 item 3).
3. **Facade (visual chat):** never mirror the plug (§4 item 1); keep the latch retracted within 8° of shut (§3 (f)); negate angles on mirrored placements (§4 item 7); the mortise strike on every steel frame works for the bored latch (§3 (d)).
4. **Pipeline (NEEDS APPROVAL, §8 P-1):** LOD1/LOD2 export and the `fr_lod2_drop` read. Until then, no LODGroup on any G2 part.
5. **In-engine:** T4 (mirrored door) and T5 (head dip) need a capture by the visual chat. Not done here (no Unity in this workflow).
