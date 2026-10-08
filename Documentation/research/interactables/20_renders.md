# 20 — In-engine renders of the interactables kit

Status: **DONE (render stage), 2026-10-07 20:0x.** Resumed at 18:1x after the 17:46 usage stop; the earlier attempt's harness and frames were kept and finished, not redone. This report says what each frame shows, what reads and what does not.

- **Clone:** `W/proj_int` (`W` = `/Users/redwang/FrontRoomsVisualWork`), made at 16:31 on 2026-10-07 from `W/proj_audit`, rsynced from Red's main, local patch script run (a no-op), tools installed (`W/TOOLS.md` §2). Main moved after that only in touch controls, the HUD key glyph and docs (16f520e4, 5e6e7f96, 07d2c843, a5262fb6). None of them feeds these frames. Every `Kit_*` FBX/JSON in the clone equals main's, except `Kit_KeyCabinet.fbx` (§0.3).
- **Harness:** `W/proj_int/Assets/Editor/Rendering/FrontRoomsInteractablesLookdev.cs` (clone-only, never for main). Copy: `harness/FrontRoomsInteractablesLookdev.cs.txt`. Batch: `Unity -batchmode -projectPath W/proj_int -buildTarget OSXUniversal -executeMethod FrontRoomsInteractablesLookdev.RunBatch -intOut <dir> [-intOnly g1,g2] -frGlassRT off -quit`.
- **Final run:** `W/proj_int/Verification/interactables_final/` (389 PNG: 268 frames, 60 leak masks of the named gap cameras and the OLD door's 61 leaking sweep masks; `frames.tsv` with every camera pose, `leaks.tsv`, `rects.json`, `labels.json`, `metrics.json`, `sheets/`). 19:09–19:28, exit 0, 12 groups OK, 0 exceptions (`W/logs/int/run5_final.log`).
- **Post-processing:** `W/int_work/int_post.py` (copy `harness/int_post.py.txt`): contact sheets, leak and readability metrics, JPG q85 copies.
- **Copies in this folder:** `images/int_*.jpg` (contact sheets), `images/renders/int_*.jpg` (every frame), `images/renders/masks/int_*_mask.png` (leak masks, PNG because they are compared pixel for pixel). Sweep masks are not copied: the OLD door's 61 stay in the clone, and the kit doors' 244 were measured but not saved (no leak); every count is in `leaks.tsv`.
- **Nothing in Red's project was changed** except files under `Documentation/research/interactables/` and the verification log rows (§14).

---

## 0. What these frames are, and what they are not

### 0.1 Method

- **Rooms:** `FrontRoomsKitLookdevAccess.Room` (the copy-only KitLookdev variant): each level's own wall, floor and ceiling surfaces, cove base, the metric 0.6 × 1.2 troffer grid (spot lamps, intensity 5, every third one shadowed). Office rooms add `FrontRoomsPostStack.EnsureZoneVolume(room, "Office", …)`.
- **Camera:** the game's post stack (`FrontRoomsPostStack.Ensure` + `ConfigureCamera`), 1920 × 1080, 4× MSAA, rendered twice per frame so volumes and shadows settle. Player frames use the eye at 1.62 m and the 76° vertical FOV. Head-dip frames use `FrontRoomsShotTimings.Unlock` (`FrontRoomsShotTimings.cs:37-40`): eye −0.25, 0.45 m off the door, about 38° down, FOV 62.
- **Reflections (new in this stage):** every frame sets the zone cube the game would use in the camera's room (`FrontRoomsZoneReflection.SetImmediate`; Office rooms → `Refl_Office`, rooms over 4 m → `Refl_Tall`, the rest → `Refl_Level0`, mirroring `FrontRooms3DGame.cs:1101-1123`). The interrupted attempt rendered without it, so glass, brass and chrome reflected nothing. G14 ray-traced glass is off (`-frGlassRT off`), as in every edit-mode tool.
- **Doors:** composed from the kit FBX and sidecar anchors with `10_spec.md` §6.2's raw recipe: frame at the door root with M = (S_sign, 1, 1); the hinge on the A2 axis (+0.0295 toward the swing side, +0.0098 into the opening); the `Leaf rig` under it, mirrored by M; face hardware by §1.2's rules (plug and text parts never mirrored); bolts at the leaf anchors; strike on the frame; the closer linkage solved per angle (circle–circle, §2.4); the 0.10 × 2.08 × 0.96 `Leaf shadow` proxy (§1.7). The map's 0.07 × 0.20 render-only trims stay underneath, as today (codex audit F2).
- **The OLD door** is the map's own construction rebuilt: hinge on the low jamb's centre line, the 0.05 × 2.08 × 0.98 `Door leaf` cube in `Door_Veneer`, the same trims (`FrontRoomsMapWorld.cs` door edge build, `01_inventory.md` §1).

### 0.2 These are look-dev rooms, not the generated map

The map still builds the old cube door, the cube key and the slab pane. Nothing here runs `FrontRoomsMapWorld`; the kit is placed by the harness the way §6.2 tells the map to place it. So these frames show what the kit will look like once the map adopts it (D1.6), in rooms that copy the levels' surfaces and lamps. The window frames use the live facade's member choice and the interim 6 mm slab in `Glass_Window` (`FrontRoomsInteractableKit.Window.cs`), but the harness places them, not the facade.

### 0.3 One asset differs from main: `Kit_KeyCabinet`

The clone's `Kit_KeyCabinet.fbx` was rebuilt at 16:40 from main's own module (`Tools/Blender/frontrooms_kit/assets/interact_key_cabinet.py`; log `W/logs/int/build_keycabinet.log`: 6,804 tris, slots Prop_SteelAlmond, Prop_PlasticWhite, Prop_Chrome, Prop_KeyTagNo). It is 160,748 B, the size of G3's fixed build. Main still holds the faulty 161,628 B build with inside-out corner faces (codex audit `10_review_kits.md` F5). Its JSON is byte-identical. **The key-cabinet frames show the fix, not main.** The merge stage should copy only this FBX and keep main's meta GUID (`13c85fe1e557741c982832fc03970bc8`).

### 0.4 Four slots have no project material (codex audit F4), and it shows

`materials_probe.json` lists what Unity binds for every kit slot:

| Slot (no `Resources/Surfaces/*.mat` in main) | Kits | What the frames show |
|---|---|---|
| `Door_Enamel` | `Kit_DoorLeaf_Steel`, `_SteelLite`, `Kit_WindowFrame_Steel_Enamel` | The FBX's own material: URP/Lit, flat almond 205/197/176 (= `05`'s tested almond), no texture, no wear. The colour is right; the surface is untextured next to textured walls |
| `Prop_KeyTagNo` | 9 key tags, 3 number plates, `Kit_KeyHookBoard`, `Kit_KeyCabinet` | **Blank**: tag inserts, door number plates and board labels show plain paper, no numbers |
| `Prop_SignEngraved` | `Kit_DoorSign` | A plain dark plate; "EMPLOYEES ONLY" is not visible |
| `Run_ExitSign_Dead` | `Kit_ExitSign_Dead` | A plain grey face (exit-sign workflow owns it) |

These are the P-4 surfaces of `10_spec.md` §8. Until they exist, every frame below shows blank tags and plates; that is the material gap, not the models.

### 0.5 The fracture pieces are a stand-in

No fracture set exists in main yet (the glass-destruction track has the crack-graph prototype, not built pieces; main's `FrontRoomsGlassShot.cs` animates the camera beats only). For the break frames I generated a labelled stand-in with the glass track's own prototype (`glass/destruction/04_proto/web_proto_crack_graph.py`, `build_web` + `finalize`), via `W/int_work/fracture_data.py` (copy `harness/fracture_data.py.txt`):
- on the window kit's visible slab (1.391 × 1.642 × 6 mm, `10_spec.md` §5.4), impact at eye height (1.50 m), seed fixed;
- 119 shards, 33 teeth, 11 crushed-core pieces, 588 crack edges (`fracture_eye_centre`); a second layout off-centre for W-OF (`fracture_eye_left`: 95 / 26 / 10);
- teeth kept inside the approved tooth band (jambs |X| ≥ 0.600, head ≥ 1.900, sill ≤ 0.390, window root), so nothing stands in the 1.4 × (0.35–2.0) climb opening;
- a mid-air layout 0.22 s after the shatter (ballistic, away from the striker); on the floor every shard broken again into 1–4 pieces, two thirds on the far side (audit §3.5 "+1.5 Rest"), plus 220 chips.
- Crack lines are a render stand-in: 2.2 mm wide, near-white with a little emission to stand for the lamp glint along a crack. They are not a proposal for the game's crack material.

---

## 1. Verdicts at a glance

| # | Check (task item) | Verdict | Deciding number | Sheet | VL |
|---|---|---|---|---|---|
| 1 | Door gap test, before/after (0) | **PASS** | old door: 4,685 px of bright room through the latch slit, 7,133 px worst sweep; kit door: **0 px in 288 of 292 views**; the 4 others are the 5 cm floor-stress camera (the spec's 3 mm undercut) | `int_gap_before_after.jpg`, `int_gap_sheet_1–3.jpg` | VL155 |
| 2 | Single-acting swing 0–95°, both ways (0, c) | **PASS** (visual) | no visible clipping at 0/5/30/60/95° in 3 cameras × 2 handings × 2 doors; T2's 1° numeric sweep was G1's (Blender) | `int_swing_*.jpg` | VL156 |
| 3 | Locked vs free, 6 / 12 / 20 m, no UI (00) | **FLAG** | locked over free (lower leaf, 6–20 m): L0 lit +1.3–1.4, **L0 dim +0.8**, Office +1.8–1.9, **Run +1.4 → +0.97**, Exit +1.5 stops; T6's bar is +1. Over an open doorway ≥ +1.8 everywhere | `int_readability_sheet.jpg` | VL157 |
| 4 | Door family line-up (a) | **FINDING** | 8 members built; Run's locked leaf matches Run's white wall (−0.1 stop) and reads by its frame | `int_family_sheet.jpg` | VL158 |
| 5 | Head-dip key shot, 5 frames (000) | **FLAG** | at pose P the key is edge-on (a 2 mm line) until it turns; yawed 30° the bow reads | `int_dip_strip.jpg` | VL159 |
| 6 | Lock sequence close-up (b) | **PASS** | key 0/50/100 % (25 mm travel), plug 0/45/90°, deadbolt 25 mm thrown/retracted, all driven by sidecar pivots | `int_lock_sequence.jpg` | VL160 |
| 7 | Level 0 room in situ + key push-in (1) | **PASS** | lever/knob at Y 1.000, 0.08 m from the latch edge; push-in 0.35 / 0.25 m | `int_room_insitu.jpg` | VL161 |
| 8 | Zone keys on hosts (2) | **FLAG** | keys hang and lie correctly; every tag and label is blank (§0.4) | `int_keys_hosts.jpg` | VL162 |
| 9 | Window family intact / broken (aa, 3) | **FINDING** | the intact 6 mm slab barely reads at 1.5 m without RT; broken reads by floor glass and teeth | `int_windows_family.jpg` | VL163 |
| 10 | Window break stages (0000), stand-in | **FINDING** | crack 1 → 2 → 3 → shatter → floor glass read in order at 1.5 m | `int_fracture_stages.jpg` | VL164 |
| 11 | Asset line-up with a 1.80 m reference (4) | **BASELINE** | 48 distinct assets in 4 rows (the other variants differ only by finish) | `int_01_lineup_*_labelled.jpg` | VL165 |

---

## 2. The door gap test (task item 0; `04_door_re8_gap.md`, T1)

**Set-up.** A shut door between a dim near room (lamps ×0.3) and a bright far room (lamps ×3), then the reverse (near ×1.5, far ×0.05). Five doors in the same opening: the OLD primitive, L0-F and OF-K each with the S (pull) face and the P (push, stop) face toward the camera. Twelve named cameras per door, and for each frame a **leak mask**: every renderer black and unlit except the far room, which is white and unlit, post off, 4× MSAA. Any non-black pixel is the far room seen through the door's perimeter. On top, a mask-only **sweep**: inline cameras across every jamb-gap position in 1 mm steps (±0.488…0.500 from the opening centre) at 1.2 and 2.5 m, and the head from 3, 4 and 4.8 m at three lateral offsets: 61 masks per door.

| Camera | OLD (map today) | L0-F S | L0-F P | OF-K S | OF-K P |
|---|---:|---:|---:|---:|---:|
| a front 1.5 m | 6 (sub-pixel, floor corners) | 0 | 0 | 0 | 0 |
| b 30° latch side, 1.5 m | 0 | 0 | 0 | 0 | 0 |
| c 60° latch side | 0 | 0 | 0 | 0 | 0 |
| d 60° hinge side | 0 | 0 | 0 | 0 | 0 |
| e latch edge, 0.4 m | 0 | 0 | 0 | 0 | 0 |
| f hinge edge, 0.4 m | 0 | 0 | 0 | 0 | 0 |
| g head, 0.4 m below it | 0 | 0 | 0 | 0 | 0 |
| h floor, camera 0.15 m high at 1 m | 0 | 0 | 0 | 0 | 0 |
| **i latch slit inline, 1.5 m** | **4,685** | 0 | 0 | 0 | 0 |
| **j hinge slit inline, 1.5 m** | **4,685** | 0 | 0 | 0 | 0 |
| **k head, 3.5 m** | **395** | 0 | 0 | 0 | 0 |
| l floor stress, camera 0.05 m high at 1 m | 0 | 625 | 625 | 624 | 624 |
| sweep, 61 inline masks (worst) | **7,133** | 0 | 0 | 0 | 0 |

(leak px = pixels above 8/255 in the mask; `leaks.tsv`.)

**What reads.**
- The OLD door shows the far room as a full-height bright line at both jambs whenever the eye is in line with the 10 mm slit, and a bar along the head from 3.5 m: exactly `04` §1's fault.
- The kit door shows **no lit line from any player-height camera on either face, either door type**. The path through every gap is L-shaped (3 mm gap, then the 3 mm silencer gap behind the 16 mm stop, `10_spec.md` §1.4), so no straight sightline exists.
- Reverse light (bright near, dark far): the OLD door's inline slit reads as a dark line; the kit door shows no line (`int_gap_before_after.jpg`, bottom row).

**What does not.** A camera 5 cm above the floor sees the 3 mm undercut over the 12 mm saddle as a thin line of light under the leaf (624–625 px, max 0.44 of white: sub-pixel thin). The spec accepts it (`10_spec.md` §1.4 "Floor": a straight line needs a slope ≤ 3.9°, a period ⅛" undercut). From the 1.62 m eye it never shows; a crouch cam lower than about 0.1 m would see it. A light line under a door is also a normal cue. The OLD door has no undercut (its cube reaches the floor), which is why it scores 0 there.

## 3. Single-acting swing, both ways (task item c; T2)

**Set-up.** L0-F and OF-K at 0, 5, 30, 60 and 95°, with the leaf moving toward the camera (S side) and away (P / stop side). Three cameras:
- **oblique**: the player's eye 2.1 m out, 40° toward the latch side;
- **plan** (new in this stage): 2.62 m up in the swing room, 0.8 m out, looking down at the hinge jamb, so the knuckles, the leaf's hinge edge, casing, lining and (OF-K) the closer arm are in one view;
- **stop** (new): on the push side, 0.55 m off the latch jamb at about 40° to the wall, at 0, 30 and 95°.
The OLD door is shown at 95° in the oblique and plan views.

**What reads.**
- The leaf turns about the knuckles (A2 axis), not the wall centre line: in plan, the hinge edge stays tucked against the lining at every angle, the knuckles stay on the pull face, and at 95° the leaf stands clear of the casing and wall. These frames agree with G1's numeric T2 sweep (0–95° in 1° steps, both handings: 0 overlapping triangle pairs at LOD0, minimum 3.00–3.04 mm to the stops and 2.83 mm to the rest of the envelope; `build_g1_door_construction_frames_leaves_hinges_closers_.md` §4 (b)). At LOD1 the decimated knuckles cross the leaf face by up to 0.8 mm (same section), sub-pixel at LOD1 distance; nothing in these LOD0 frames shows it.
- No part passes through the frame, stop, saddle or casing in any frame. The latch is held back within 8° of shut (the facade rule), so at 5° the latch does not cross the strike lip.
- OF-K's closer: the arm and forearm stay connected to the frame shoe at every angle (the elbow is solved per frame), and the body rides the leaf.
- The stop view shows the latch jamb as a stepped section: casing, stop, lining with the strike. At 0° the leaf face sits just behind the stop; at 30° and 95° the stop and the strike are bare.
- The OLD door at 95° for comparison: the leaf pivots on the wall centre line inside the reveal, with no stop, knuckle or casing (`00_map_constraints.md`: "the open leaf passes inside the jamb with no rebate").

**What does not.** The stop is the same walnut (L0-F) or dark bronze (OF-K) as the casing and lining, so in the stop view it reads as a step in shading, not as a separate part. That is correct for period frames; it just means "the stop is visible" has to be read from the step. The 1° numeric overlap sweep (T2) is G1's Blender check (above); this stage checks it in engine, visually, at five angles.

**Both-ways swing.** The kit is built single-acting, as Red chose (`10_spec.md` §0 D1, Option A2). The gameplay change that needs (pull side, the 0.45 m step-back, Relay breaking from the stop side) is the map chat's, listed in `10_spec.md` §6.5; nothing in these renders changes the map's rules.

## 4. Locked vs free from the player's eye (task item 00; `05`, T6)

**Set-up.** A 6 × 26 m corridor per condition. The end wall holds FREE | LOCKED | an open dark doorway (`05`'s method), then the same with the two doors swapped, so the centre slot's lighting bias cancels in the mean. The side walls alternate free and locked at 7, 12, 17 and 22 m (W: F, K, F, K; E: K, F, K, F). Cameras at the eye, 6, 12 and 20 m from the end wall. Conditions: Level 0 lit, Level 0 dim (the last 6 m of lamps removed), Office (zone grade), and, new for T6, **Run** and **Exit**. Luminance is measured on the final frames (post included, sRGB → linear) inside rectangles projected from the door roots (`rects.json`): the lower leaf (0.35–0.85 m), the upper leaf, the frame, the open doorway and the wall.

Stops (log2 of the luminance ratio), from `metrics.json`. (The labels on `int_readability_sheet.jpg` are each frame's own value, first layout only, so they run 0.1–0.4 stop higher than this table.) "Lower leaf" and "upper leaf" are the mean of the two layouts (the centre-slot bias, 0.04–0.37 stop, cancels). "Open doorway" and "wall" are from the first layout.

| Condition | Locked − free, lower leaf (6 / 12 / 20 m) | Locked − free, upper leaf | Locked − open doorway | Locked leaf − wall | T6 (≥ +1 over free, ≥ +1.5 over a doorway) |
|---|---|---|---|---|---|
| Level 0 lit | +1.40 / +1.33 / +1.30 | +1.23 / +1.19 / +1.15 | +4.6 | +0.7 | **pass** |
| Level 0 dim | **+0.82 / +0.80 / +0.77** | +0.76 / +0.76 / +0.75 | +1.9 | +0.4 | **fails the +1 bar by 0.2 stop**; passes the doorway bar |
| Office (grade on) | +1.93 / +1.84 / +1.79 | +1.54 / +1.44 / +1.39 | +5.6 | +0.9 | **pass** |
| Run | +1.43 / +1.12 / **+0.97** | +1.12 / **+0.96 / +0.90** | +4.5 | **−0.1** | **marginal**: under +1 from 12–20 m (upper leaf) and at 20 m (lower leaf) |
| Exit | +1.55 / +1.48 / +1.46 | +1.23 / +1.15 / +1.10 | +4.6 | +0.9 | **pass** |

The interrupted attempt measured the same three original conditions without zone reflections; its numbers agree within 0.05 stop (L0 lit +1.32–1.42, L0 dim +0.81–0.85, Office +1.83–1.96), so the reflection change did not move the readability result.

**What reads.**
- In every condition the locked door is the light rectangle and the free door the dark one, at 6, 12 and 20 m, on the end wall and down both side walls. The open dark doorway is always darkest, so a locked door never reads as an opening.
- At 2–5 m the hardware confirms it: a lever on a round rose (free) against a knob on a tall dark escutcheon plus the stainless kick plate (locked).

**What does not.**
- **Level 0 dim misses T6's +1 stop bar (+0.77 to +0.82).** In the dim end of the corridor the film look's lifted blacks compress every ratio; the almond is still visibly the lighter door and +1.9 stops over an open doorway, so a locked door never reads as a way through. `05`'s stand-in test found +1.05 or more here with the brighter `Painted_Metal` stand-in (0.61 albedo); the kit's FBX almond (205/197/176) is darker. If the bar matters, the P-4 `Door_Enamel` material can lift the almond toward `Painted_Metal` (`00_probe_lockedleaf_DoorEnamelFBX_vs_PaintedMetal_3m` shows both side by side).
- **Run is marginal at 12–20 m** (+0.96 / +0.90 on the upper leaf, +0.97 on the lower leaf at 20 m). The hospital wall (albedo 0.74) is lighter than the almond, so the locked door matches the wall (−0.1 stop) and is told by its dark-bronze frame outline (−3.2 to −3.3 stops against the wall) and by the darker ward door beside it, not by being the brightest thing (as `10_spec.md` §2.2 predicted).
- Exit's aluminium frame is the wall's value (−0.1 to 0.0 stop), so in Exit the frame does not outline the locked door; the leaf (+0.9 over the wall) and the lit vs dead EXIT sign do.

## 5. The door family (task item a)

**Set-up.** One 6 × 12 m room per level (Lobby, Office, Run, Exit), FREE left and LOCKED right in two 1.0 × 2.1 openings 1.8 m apart. Push (P) faces at 3 m and 8 m; pull (S) faces at 3 m (closers on S).

**What reads.**
- **L0:** walnut casing, veneer leaf with a brass lever (free); dark-bronze steel frame, almond enamel leaf, stainless kick plate, knob on escutcheon, sign and number plate (locked).
- **Office:** oak veneer with chrome lever, closer on the pull face (free); the same locked door with a blue number plate and a closer.
- **Run:** the ward door: laminate leaf with the stainless armour plate and push plate on the P face, D-pull and kick plate on S, no latch (free); the locked steel door with a white plate.
- **Exit:** the oak door in the aluminium frame with the crossbar exit device on P and the lit EXIT sign above (free); the locked steel door with the dead sign above (locked).
- At 8 m every pair still separates by value, hardware and (Exit) the lit vs dead sign.

**What does not.** The door sign and number plates are blank and the dead EXIT face is plain grey (§0.4). Run's free ward door reads mid-dark because of its armour plate, so in Run the lower leaf is not the best place to measure (§4 uses the upper leaf too).

## 6. Level 0 room in situ (task item 1)

**Set-up.** A 5 × 6 m Level 0 room with two 1.0 × 2.1 openings into a second lit room, L0-F left and L0-K right, P faces toward the camera, hardware at `ModuleUnits.DoorHandleHeight` 1.0 and 0.08 from the latch edge. Eye 1.62 at 1.5 m and 0.6 m; then the key push-in on L0-K at 0.35 m and 0.25 m from the keyhole along the head-dip line, key at its insertion anchor (shoulder on the core face), plus the same two yawed 30° about the keyhole with the shot light.

**What reads.** At 1.5 m: two complete doors with casing, stops, saddle and hardware; the free lever and the locked knob-and-escutcheon. At 0.6 m: the lever's return, the rose, the escutcheon's two screws, the cylinder collar and the knurled band on the knob. The 0.35 / 0.25 m frames frame the cylinder and knob as the head dip would.

**What does not.** On the head-dip line the inserted key is seen edge-on: its bow is a vertical plane that contains the camera, so it reads as a 2 mm brass line in the keyway (§7). The room is lit by the ceiling grid only; the lock at 1.0 m sits in the lamps' falloff, so without the shot light the brass is dull.

## 7. The head-dip key shot (task items 000 and b)

**Set-up.** L0-K and OF-K, five frames on `FrontRoomsShotTimings.Unlock`'s path: f0 standing (eye 1.62, 1.2 m off the door, looking at 1.3 m); f1 dipping (45 % of the travel, cubic ease); f2 pose P (0.45 m off the door, eye 1.37, about 38° down, FOV 62; key tip 30 mm in front of the keyway, `KeyAtKeyway`); f3 pushed in to 0.42 m, key home; f4 0.30 m, key and plug turned 90°, deadbolt retracted. Then f3 and f4 again with the shot light (`Unlock.ShotLightIntensity` 0.6, read here as 0.6 of illuminance at the lock, §7 note) and yawed 30° about the keyhole toward the door centre.

**What reads.** The move itself: the door fills the frame at f0, the lock moves to the right third through f1–f2, and f4 ends on the lock. When the key has turned 90° (f4) its bow is horizontal and reads face-on as a brass key head in the cylinder, in both levels. With the shot light the escutcheon, collar and knurl read cleanly without lifting the whole wall.

**What does not.**
- **At f2 and f3 the key is edge-on and nearly invisible.** Pose P puts the camera straight out from the lock on the door normal, and the key's bow is a vertical plane through that line. The insert beat (0.55–0.68 s) therefore shows almost nothing until the turn.
- Yawed 30° about the keyhole, the same beats show the bow, the blade and the cuts going in (`int_dip_strip.jpg`, bottom row; `int_lock_sequence.jpg`, row 2).
- **Proposal for the map chat / `FrontRoomsShotTimings.Unlock`:** add a yaw of about 20–30° to pose P (the camera moves toward the door centre, as a person stands slightly to the side of a lock), or roll the key by about 30° before insertion. The yaw keeps the knob out of the key's path. Constants are the map chat's (`10_spec.md` §6.5 item 11).
- **The shot light is not wired in the game:** `ShotLightIntensity` is declared (`FrontRoomsShotTimings.cs:56`) but nothing reads it. A point light of intensity 0.6 at 0.3–0.6 m (what "0.6 intensity" means literally in URP) burns the leaf to white; the interrupted attempt rendered that way. The frames here scale it as intensity = 0.6 × d², i.e. 0.6 illuminance at the lock, about the same as the troffers give a lit door. Whoever wires it should decide which meaning is meant.

## 8. The lock sequence close-up (task item b)

**Set-up.** L0-K, P face, pose P (0.57 m, 38° down, FOV 62), shot light on; the same at 0.25 m on the dip line and 0.25 m yawed 30°. Every part is driven by its own sidecar pivot: the key slides along the keyhole frame's −Z (its `shoulder` → `tip` axis, `Unlock.InsertDepth` 0.025 m), the plug and key turn about the plug's part −Z (`Kit_Lock_Plug` motion: +angle sends the cuts toward the hinge, sign flipped on the P face), the deadbolt slides 0.025 along its `throw_dir`, the mortise latch 0.019 with the knob at 40°. Then the leaf is opened 30° and the latch edge is shot head-on and in profile from above: deadbolt thrown, deadbolt retracted, deadbolt and latch retracted.

**What reads.**
- Key 0 % (tip on the core face, 25 mm of blade showing), 50 % and 100 % (shoulder on the face) are distinct at 0.25 m yawed: the cuts disappear into the keyway one by one. There is no visible intersection of blade and plug at any step.
- Plug 45° and 90°: the key head turns with the plug; the keyway slot turns with it.
- Edge profile: the thrown deadbolt stands 25 mm proud of the faceplate as a square bolt with its two insert dots; retracted it sits flush; the bevelled mortise latch reads and retracts flush with the knob turned.
- Number plate beside the cylinder (Y 1.000, Z 0.790) and the sign above are in frame at pose P.

**What does not.** At pose P (0.57 m, FOV 62) the frame is 0.69 m tall, about 1,580 px/m: the escutcheon is about 240 px tall and a 12.5 mm insertion step is about 20 px, readable but small. The head-on edge view cannot show the bolt throw (it is seen end-on); the profile view does.

## 9. Zone keys on their hosts (task item 2; `10_spec.md` §4)

**Set-up.** Level 0 (Lobby rules): `Kit_KeyHookBoard` (the board), a single `Kit_KeyHook`, and the key lying on a filing cabinet top; the old emissive 0.32 × 0.12 × 0.12 cube at 1.05 m is placed in front for comparison. Office (zone grade on): `Kit_KeyCabinet` (open), a single hook and the key on a desk. Each at 2 m (eye 1.62) and 0.6 m. Hung keys use §4.1's hung pose (ring on the hook, key and tag on the ring, twisted 30°); lying keys lie flat with the ring tilted through the bow hole. **Hosted keys do not spin** (as recommended in `00_map_constraints.md`).

**What reads.** At 2 m the player finds the host, not the key: the dark board and the open cabinet read at once; the single hook reads as a small ring and tag. At 0.6 m the key, ring and tag hang believably, the brass reads under the zone cube, and the key on the cabinet top or desk lies flat on its support. Next to the hosts, the old yellow cube is the loudest object in the room.

**What does not.**
- Every tag insert and board label is blank (`Prop_KeyTagNo`, §0.4), so no zone number reads at any distance.
- On the single hook the round tag hangs in front of the key and hides most of it at 0.6 m.
- The lying key's ring and tag are placed by an approximate pose (G3's contact search is not ported to the harness); they sit on the surface but not by physics.

## 10. The window family, intact and broken (task items aa and 3)

**Set-up.** Per member (W-L0 wood / Lobby, W-OF steel / Office with the raised mini-blind, W-RN enamel steel / Run, W-EX aluminium / Exit): a 5 × 5 m room of that level with the window centred in its north wall (the map's cut: sill 0.35, head 2.0), a Tall Level 0 hall behind; intact (the interim 6 mm slab, `Glass_Window`) and broken (the stand-in teeth and floor glass of §0.5); eye 1.62 at 1.5 m and 40° oblique at 1.9 m.

**What reads.** The four frames are clearly one family: same opening, same stop-and-casing grammar, different material per level (walnut, dark-bronze steel with the blind head, white enamel, clear aluminium). Broken: floor glass on both sides (two thirds beyond the wall), small teeth along the jambs and head, the opening clear for the climb.

**What does not.**
- **The intact pane barely reads.** At 1.5 m and in the oblique the slab shows only a faint reflection of the zone cube; intact and broken are told apart by the floor glass, not by the pane. These frames have G14 RT off; the in-game glass with RT and the reflection setup is the glass track's question (W1 / G14), not the frame's. W-RN's wired glass is not represented (the harness uses `Glass_Window` for all four).
- The teeth are clear glass and are thin from 1.5 m; they read best in the oblique.

## 11. The window break stages (task item 0000, stand-in)

**Set-up.** W-L0 (impact at the centre, eye height) and W-OF (impact off-centre left): crack stage 1, 2, 3, the shatter with pieces mid-air (t + 0.22 s), and after (teeth + floor glass); 1.5 m, 0.6 m at the impact for stages 2–3, oblique for the shatter and after, and a look down at the floor.

**What reads.**
- Stage 1: a crushed core and short radials. Stage 2: radials to about 0.55 m and the first rings. Stage 3: the full web to the frame. The order is clear at 1.5 m with the stand-in crack lines.
- Shatter: pieces spread forward and down from the impact; nothing crosses into the room.
- After: the floor glass reads at once on the near side (bright chips catching the zone cube) and as darker shards beyond the wall; the opening is clear.

**What does not.** These are stand-in pieces and stand-in crack lines (§0.5). The game's shot has two crack beats before the shatter (Crack1 0.35, Crack2 0.70, Shatter 1.0; `FrontRoomsGlassShot.cs`, audit §3.5); "stage 3" here is the full web the shatter starts from, not a third beat.

## 12. Line-up with a 1.80 m reference (task item 4)

**Set-up.** A neutral studio (post off, one soft key and a fill, flat grey ambient, grey floor and backdrop). Four rows, each with a 1.80 m figure and a 0.1 m banded pole: door frames and leaves (9); windows and blinds (6); hosts and signs (6); and the small parts on a 1.0 m plinth with a 0.1 m scale bar in 10 mm bands (28, the single hook repeated): 48 distinct assets. Labelled versions are drawn from `labels.json`.

**What reads.** Relative scale is right everywhere: doors 2.1 m against the 1.80 figure, window frames 1.4 m wide, the board, cabinet and signs at their wall heights, the key 58 mm long against the 10 mm bands.

**What does not.** The lowered blind hangs in the air at its rail height without a window behind it. The single hook is a dot in the hosts row; it is repeated on the plinth.

## 13. Problems found and who owns them

1. **Blank tags, plates and signs** (§0.4): P-4 surfaces `Prop_KeyTagNo`, `Prop_SignEngraved`, `Door_Enamel` (and `Run_ExitSign_Dead`, exit-sign workflow). Owner: visual chat (`10_spec.md` §8 P-4); codex audit F4.
2. **Key edge-on at pose P** (§7): add a 20–30° yaw to the head-dip pose, or roll the key before insertion. Owner: map chat (`FrontRoomsShotTimings.Unlock`), with the visual chat.
3. **Shot light not wired and its unit undefined** (§7). Owner: map chat.
4. **L0 dim readability** below T6's +1 stop (§4). Options: accept (it still reads, and the open doorway is ≥ 1.5 stops darker), or raise the `Door_Enamel` almond when the P-4 material is made. Owner: Red / visual chat.
5. **Intact glass barely reads with RT off** (§10). Owner: glass track (W1 / G14); not a kit problem.
6. **`Kit_KeyCabinet` in main is the faulty build** (§0.3, codex audit F5). Owner: this workflow's merge stage.
7. Floor undercut visible only from a 5 cm camera (§2): accepted by the spec; no action unless a crouch camera goes below about 0.1 m.
8. Not covered by these edit-mode renders: codex audit R2 (`codex_audit/10_review_runtime.md`, VL126: the map's cube zone keys draw magenta after R). The kit key binds the shared `Resources/Surfaces/Prop_Brass` material through `FrontRoomsKitLibrary.Spawn`, not a material `MapWorld` builds, so it should not share that path, but that is UNVERIFIED until a Play-mode restart test runs with the kit key spawned by the map.

## 14. Verification log

Placed in the Figma section **FRONTROOMS · VISUAL VERIFICATION LOG** (`2595:6093`, page `2099:76`) per `Documentation/VERIFICATION_LOG.md` §1, rows added to its §3; the section was grown to 42 rows and the cover (VL000) counts and the D1 / W1 legend entries were updated.

| VL | Frame | Task | Check | Verdict | Images (in slot order, under `images/`) |
|---|---|---|---|---|---|
| VL155 | `2815:6151` | D1 | Kit door closes the gap | PASS | `int_gap_before_after.jpg`, `int_gap_sheet_3.jpg` |
| VL156 | `2815:6167` | D1 | Single-acting swing, no clip | PASS | `int_swing_OF-K_toward_camera_pull.jpg`, `int_swing_L0-F_away_from_camera_push.jpg` |
| VL157 | `2815:6177` | D1 | Locked reads from 20 m | FLAG | `int_readability_sheet.jpg`, `renders/int_03_readability_L0dim_20m.jpg`, `renders/int_03_readability_Run_20m.jpg` |
| VL158 | `2815:6192` | D1 | Door family, free vs locked | FINDING | `int_family_sheet.jpg`, `renders/int_02_family_L0_P_3m.jpg`, `renders/int_02_family_EX_P_8m.jpg` |
| VL159 | `2815:6207` | D1 | Head dip hides the key | FLAG | `int_dip_strip.jpg` |
| VL160 | `2815:6216` | D1 | Lock parts move on pivots | PASS | `int_lock_sequence.jpg`, `renders/int_06_lock_edgeprofile_a_deadbolt_thrown.jpg` |
| VL161 | `2815:6228` | D1 | Kit doors in a Level 0 room | PASS | `int_room_insitu.jpg` |
| VL162 | `2815:6237` | D1 | Keys on their hosts | FLAG | `int_keys_hosts.jpg`, `renders/int_07_keys_L0_board_0.6m.jpg`, `renders/int_07_keys_OF_cabinet_0.6m.jpg` |
| VL163 | `2815:6252` | W1 | Window family in its rooms | FINDING | `int_windows_family.jpg`, `renders/int_08_window_W-OF_broken_oblique.jpg` |
| VL164 | `2815:6264` | W1 | Break stages, stand-in | FINDING | `int_fracture_stages.jpg`, `renders/int_09_fracture_W-L0_crack3_0.6m.jpg`, `renders/int_09_fracture_W-L0_after_floor.jpg` |
| VL165 | `2815:6279` | D1 | Kit line-up at true scale | BASELINE | the four `int_01_lineup_*_labelled.jpg` |

Not logged as separate checks (they are summarised by the sheets above): the other 240-odd frames in `images/renders/` and the leak masks.

## 15. Files

- Harness: `harness/FrontRoomsInteractablesLookdev.cs.txt`; post: `harness/int_post.py.txt`; fracture stand-in: `harness/fracture_data.py.txt`.
- Sheets: `images/int_*.jpg`. Frames: `images/renders/int_*.jpg`. Masks: `images/renders/masks/`.
- Raw run: `W/proj_int/Verification/interactables_final/` (PNG, `frames.tsv`, `leaks.tsv`, `rects.json`, `labels.json`, `metrics.json`, `materials_probe.json`, `int_log.txt`). The interrupted attempt's frames (no zone reflection, the over-bright shot light) are in `W/proj_int/Verification/interactables/` for comparison only.
