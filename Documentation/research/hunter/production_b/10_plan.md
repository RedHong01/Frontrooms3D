# Hunter B "Night Shift": production plan (H1–H4)

Status: PLAN, 2026-10-22 08:1x. Written by 游戏视觉 (workflow `hunter-b-night-shift`, stage 1). No asset, code or clone exists yet: everything below is to be built.

Load average was 526 at the start of this stage and 135 at the end (UNDER LOAD). This stage measured no timings. Every time estimate below is a plan, not a measurement.

**Decided (Red, 2026-10-21, "全部按推荐"):**
- The body is **B "Night Shift"**, built as the squeezed giant (`11_squeezed_giant.md`).
- **Fallback A "Floor Sample"** if B's doorway-at-3 m panel reads as a TV head (`10_hunter_directions.md`, line 3).
- Red, 2026-10-22: start every planned step now.

**Still OPEN, not decided here:**
- the long craned neck, or a face about 0.3 m higher;
- the catch distance, 0.7 m or about 1.3 m (map-owned);
- whether every door is a squeeze (a gameplay slow-down).

Everything below is built so that every one of these choices stays possible (§10).

**Inputs read in full:**
- `10_hunter_directions.md`: §1 pillars, §2 shared numbers, §4 B, §7.3 the B blockout, §7.6 panels, §8 scores and carry-over traits, §9 hand-offs, critic notes C1–C6.
- `11_squeezed_giant.md`.
- `03_gameplay_tech.md`: §2.2, §3, §4, §5, §8.
- `04_period_wardrobe.md`: §2, §4.3, §9.
- `Tools/Blender/frontrooms_kit/creatures/giant_b_night_shift.py`, the giant pass with its POSE parameter sets.
- `Assets/Scripts/FrontRoomsRelayRig.cs` and every caller of it.
- `FrontRoomsMapHunter.cs`: DoorSqueezeAt, Arrive, catch.
- `FrontRooms3DGame.cs`: CreateHunter, UpdateRelayRig.
- The GlassRT registry, `FrontRoomsSurface.shader`, the RenderSetup `SurfaceDef` list, and the lamp-override interface in MapWorld.
- `AUDIO_CONTRACT.md`.
- `RELAY_PURSUIT_REDESIGN.md` §8–§9: "dark is never cover", the staged warning, photosafety.
- Figma HUNTER section 2331:852 (HR05, HR11 and HR16 looked at), read-only.
- `W/TOOLS.md`.

---

## 1. The design

### 1.1 Silhouette

The figure is the building's night electrician, grown about 1.75 times too big for the building. It is a heavy adult scaled up, never thin.

- **Front.** A rounded **shrug**. The coverall's trapezius rides up into one curve that is pressed flat against the ceiling tiles. Below the shrug, a dark hood hangs on a bowed bull neck of the same twill, and the hood's opening is drawn tight round a strip of lit ceiling lens. Elbows splay. Long forearms hang to mid-thigh, about 0.38 of the standing height, which is ordinary human reach. A tool belt with a pouch on the right hip and a key ring on the left make the outline asymmetric.
- **Side.** The spine pitches 8 → 22 → 38 → 72° up its four segments (the std pose), so the shrug leans out over the hips. The neck leaves the front of the shrug and bows down. The hood pitches 20–26° down, like a head bowed to read a meter.
- **Back** (the Search and turn-away read). The action-back yoke seam, two pleats pulled open, and strain creases fanned over the shoulder blades where the tiles hold the shrug down.
- **The tell.** Its face is a light. Behind the opal you can just see one tube band, and a strip of a face pressed against the diffuser from inside. The face is visible only in Chase or close up.

At 20 m with the lens dark, the cut-out is a heavy block jammed under the ceiling line. That ceiling contact is the shape no person makes (C4's "plainest outline" problem is answered by the squeeze, not by props). With the lens lit, it is the only moving light in the place.

### 1.2 The hood and the flat lens, built so they never read as a TV head

The critic pass already narrowed the lens to the troffer's 1 : 2 and took the rim away (C1, §4 Risks). Production adds rules that every build and every render is checked against:

1. **Proportion.** The lit opening is 0.20 × 0.40 m, the ceiling lens's 1 : 2, with the long axis vertical. That is no screen's landscape or 3 : 4 shape. A portrait 1 : 2 can read as a **phone**, so the cold read in §6 T1 counts "phone" as a fail too.
2. **No bezel.** Nothing frames the lens except the hood's own drawn band. The pleated twill band (9 pleats, from the giant module) overlaps the lens edge by 10–20 mm, so the lit outline has about 6–9 puckered notches and no straight edge longer than about 60 % of a side. Check: RMS deviation of the lit outline from its best-fit rectangle ≥ 8 mm, and no ring around the lens brighter than the hood.
3. **Recessed, not proud.** The hood front is cut flat and the lens sits 10–20 mm inside it (§4 B "Key features"; the blockout's lens stood proud, C5). At three-quarter views the fabric hides a sliver of the lens. A screen is flush or proud.
4. **Light that is a lamp, not a picture.**
   - The lens is a cut strip of the game's own prismatic troffer lens (`gen_surfaces.py lens()`: 5 px prism, three tube bands). It takes the window u 0.17–0.50 of that lens: one full tube band off-centre plus half of the middle band at one edge.
   - Luminance across the lens varies by at least 2.5 : 1 (bands against gaps), and the ends fall off like a troffer's.
   - It shows no image. The one face-shadow tell lies over a band.
   - A screen is uniform, centred and shows a picture. This lens is none of the three.
5. **Soft hood.**
   - The hood shell's superellipse exponent drops from the module's 2.4 to ≤ 2.1, so no flat top plane or box corner is left.
   - It keeps the parka peak behind the crown, a gathered hem, and the drawcords hanging plumb with metal aglets.
   - The hood is never level and frontal while moving: pitch ≥ 14° down in every pose, roll 5–9° in low and door.
   - Only at the catch does it look straight at you, and that is the reveal.
6. **No stalk.** The neck is the hood's own twill: a tapered sleeve 0.38 → 0.23 m wide, with the hood's centre seam running down it into the rolled-up coverall collar. It reads as a bowed bull neck, never as a stand or a hose under a box.
7. **Never combine** B's lens with A's suit, a box, a speaker, a respirator or white Tyvek (§4 Risks).

The doorway-at-3 m panel (§6 T1) is the pass/fail test. Its result decides B or the fallback A.

### 1.3 Proportions in the four poses (squeezed giant, 11 §Poses; giant module values)

The skeleton is one fixed set of bones:
- spine (pelvis → shrug) 1.42 m;
- thigh 0.76 m, shin 0.68 m;
- upper arm 0.60 m, forearm 0.64 m;
- shoulders 0.92–0.97 m across the cloth;
- hood about 0.40 W × 0.46 D × 0.55 H m, round a 0.20 × 0.40 m lens;
- gloves about 0.33 m (× 1.65 of a person's), boots about 0.45 m (× 1.45);
- standing height ≈ 3.25 m.

| Pose | Where | Top | Lens centre: **long neck** (default) / **face +0.3** | Half-width | Visible neck (long) | Notes |
|---|---|---|---|---|---|---|
| `low` | 2.4 m Low ceiling | ≤ 2.37, shrug pressed to 2.355 | 1.42 / 1.72 | ≤ 0.80 | 0.83 m | Deep bow-legged squat, arms dangling between the knees, hood cocked (roll 9°). In Chase, knuckles on the carpet every second stride (§3.4) |
| `std` | 2.9 m Standard / Office ceiling | 2.80–2.86, shrug pressed to 2.845 | 1.78 / 2.08 | ≤ 0.80 | 0.94 m | The hunting pose. In Listen the right glove rests flat on a ceiling tile, the electrician feeling the ceiling; it drops in Hunt |
| `door_in` / `door_mid` / `door_out` | through a 1.0 × 2.1 m door | inside \|y\| < 0.15 of the door plane: ≤ 2.08 and \|x\| ≤ 0.48 | 1.45 (door_in) in both options: the lintel decides | — | 0.40 m | Turned 80° sideways, leading with the right shoulder, hood ducked, right glove on the jamb casing, left glove flat on the wall behind. door_mid and door_out are new keys (§3.2) |
| `tall` | 5.4 m Tall zone | 3.10–3.30 (module 3.27) | 2.92 in both options | ≤ 0.85 | 0.26 m | The reveal: nearly upright, slight stoop, hood up on the shrug, lens pitched 14° down at you |

- **Navigation is unchanged.** The probe stays r 0.30 between 0.40 and 1.95 m, and the sight ray stays at 1.60 m. The size is visual only.
- **The neck telescopes:** 0.94 m visible in std, 0.26 m in tall.
  - It is built as a slide plus a stretch:
    - `neck_01` slides up to 0.40 m back into the rolled collar (hidden inside the torso shell);
    - `neck_02` and `neck_03` stretch at most 1.25 × along their own axis.
  - So twill texel density changes by at most 25 %. The envelope test (§6 T5) checks that no neck vertex leaves the torso hull.
  - With face +0.3 the neck is ≤ 0.60 m and needs no stretch.

### 1.4 Materials: three, plus a cloth sheen (H3)

| Material (slot, already in `kitlib.SLOTS`) | Covers | Value (sRGB) | Smoothness | Maps, desktop / WebGL import | Notes |
|---|---|---|---|---|---|
| `Creature_TwillSpruce` | coverall, hood, neck sleeve, collar; gloves (#1C1C1E, 0.15) and boots, belt and pouch (#151413, 0.40) by mask | #2E3B33; knees and seat at smoothness 0.35 | 0.15 | 4096² A / N / S; WebGL 2048² | Baked normals: twill weave, action-back pleats, strain creases, elbow bunching, zip placket, pocket flaps, seams, glove fingers, boot tread. 6.6 : 1 against Level 0 paper |
| `Creature_LensOpal` (emissive) | the lens | #E8E4D8 albedo, prism normal | 0.30 | 512 × 1024 A / N / E (the emission map holds the 1.5 tube bands, the end falloff and the face-shadow tell); WebGL 512 × 1024 | Emission only through a per-material `MaterialPropertyBlock` (`SetPropertyBlock(block, lensIndex)`); the full level is ≤ 0.6 × `Troffer_Lens` (§1.5). 9.2 : 1 against the twill by albedo alone |
| `Creature_TapeSilver` (trim atlas) | duct-tape bands at the wrists and ankles (#B8B8B4, dielectric, 0.50), the name patch (#DCD8CC, 0.45, with the stock number), the brass key ring (#B08A4A, metallic), cord aglets and zip pull (metallic) | as listed | as listed | 1024²; WebGL 512² | Tape 5.9 : 1 against the twill; patch 8.2 : 1 |

- **Every creature `SurfaceDef` sets these to 0:** macroTone, macroDirt, stain, floor grime and ceiling grime. Those terms are world-projected, so on a moving body they would swim across the cloth (the same trick `Troffer_Lens` already uses).
- **Sheen.** The twill gets a fabric sheen at grazing angles. It is lit by real lights, not a glow, and is added as a new `_FR_SHEEN` local keyword in `FrontRoomsSurface.shader`, **off by default**, so every other material stays bit-identical. It answers 03 §3.2's rim-term ask (the Amnesia rim) in a physically plausible way.
- **Retired:**
  - `Creature_LensDim`: state now comes through the property block.
  - The blockout's separate `Prop_FabricChair` / `Prop_Vinyl` / `Prop_Paper` / `Prop_Brass` slots: they are merged into the three atlases.
- **#A99E78 is retired (carry-over trait).** The production body has no mid-tone. The game still passes `C("A99E78")` to `Configure` (map-owned `FrontRooms3DGame.CreateHunter`), but only the legacy fallback rig uses it (§5).

**The three value bands:** dark twill ≤ #3B; pale trims and lens albedo #B8–#E8; near-black gloves and boots. No information is carried by hue.

### 1.5 The lens light per Relay state

The lens is driven only by the rig's `MotionState`, the time spent in that state, and the door-blow index. **It never reads distance to the player, the warning stage, the path distance or the light around it.** A state light, never a radar.

The game maps HunterState to MotionState: Chase → Run; BreakDoor → BreakDoor; Hunt, Wander and moving Search → Walk; Search → Search; everything else → IdleListen.

"Full" is the lens emission at 0.6 × `Troffer_Lens` (2.2, 2.1, 1.8): (1.32, 1.26, 1.08) × the emission map. The colour is a warm white (about 3500 K), the same in every zone: it matches Level 0's lamps and stands out warm in the Office's cool light.

| Rig state | Lens (× full) | Pattern | Own light (desktop) | Reduce Flashing |
|---|---|---|---|---|
| IdleListen (Listen; every arrival) | **0.06 standby**, flat: no tube bands, the filaments idling | Steady. Coming from a lit state, a 1.0 s phosphor decay to 0.06 | none | the same |
| Search | 0.06 base. 2–3 **starter attempts** in 2.5 s, each on a latched head snap: up to 0.35 in 0.12 s, back in 0.35 s | ≤ 1.2 attempts/s = ≤ 2.4 transitions/s | none | one 0.5 s swell to 0.25, then hold |
| Walk (Hunt, Wander) | **0.25 steady**, tube bands visible | steady | point light r 1.6 m at 0.25 × the Chase intensity: lights the chest and gloves | the same |
| Run (Chase) | **Strike to 1.0**: 0.25 → 0.9 (0.08 s) → 0.4 (0.12 s) → 1.0. That is 3 transitions, then steady | the strike | point light r 3 m, unshadowed, 0.25 m in front of the lens, colour (1, 0.93, 0.80). Ramps with the lens; at 1 m ahead the carpet gets ≤ 50 % of a troffer pool | 0.5 s fade 0.25 → 1.0 |
| BreakDoor | 1.0 steady; each blow is a −8 % shudder (below the 10 % flash threshold); on the final blow it drops to 0.2 for 0.3 s and re-strikes (2 transitions) | Light leaks round the door gap and moves with the hammering head | as Chase | no shudder; the final blow is a 0.5 s dip |
| Stagger | drops to 0.1 in 0.05 s, holds 0.3 s, re-strikes (2 transitions) | — | follows the lens | 0.5 s dip to 0.5 |

- **Photosafety limiter.** No more than 3 transitions of 10 % or more of full in any sliding 1 s window. Extra transitions are slew-limited into fades, which also covers fast state changes such as a Search pulse followed by the Chase strike. With `FrontRoomsSettings.ReduceFlashing` on, no envelope is faster than 0.5 s (the same as `MapWorld.ReducedFlashingEnvelopeSeconds`).
- **Bloom.** At 3 m in Chase, the lens's p99 pixel value stays under the troffers' p99 in the same frame (§4 Risks: "no white blob").
- **Changed from 10 §4 (Listen "off").** Listen is now a 6 % standby glow. Red's rule that darkness is never cover would otherwise make a Listen Relay in a dead cell invisible. It is constant, carries no state other than Listen, and is uniform with no bands, so it never makes two eye-dots. In a lit cell it is lost under the pale albedo, so arrivals still read as the 10 §4 "pale panel in a dark hood" (§10 C1).
- **WebGL** (gated `#if UNITY_WEBGL && !UNITY_EDITOR`): the Chase light only; no Walk light.

### 1.6 The stock number and the other visual carry-over traits (§8.3)

1. **The same stock number on every re-arrival.**
   - The blank oval name patch carries an embroidered rental-garment number where a name would be. It is institutional, not an identity (04 §2.2).
   - There is one body asset, so every arrival shows the identical patch.
   - The string proposed here is **`114`**: chain-stitch, ink #1D1D1C on #DCD8CC. The string is ours; the lettering goes through 平面视觉 (`type-with-graphic-visual`).
   - It is a close-range discovery: about 0.04 m glyphs, ≈ 10 px at 3 m, readable at the catch.
   - If the fallback to A happens, the same string moves to A's swing tag.
2. **A relay click at the arrival point.**
   - On the body: none. The Relay arrives in Listen and the lens is on standby.
   - Off the body: the arrival troffer strike (map contract M5) and the starter-click audio (声音设计).
3. **Troffers that buzz up or strike within a cell.**
   - *Strike*: one-shot, at arrival only (M5).
   - *Buzz up as it passes*: **audio only**. Red's staged-warning decision is that lamps never change where the Relay is, so the body changes no lamp level.
4. **Latched Listen and Search against fluid Hunt and Chase.**
   - Listen and Search hold poses and snap. The head turns are quantised to 15° (as today) and timed as holds of 0.4–0.9 s with 0.08 s snaps.
   - Each Search snap carries one lens starter pulse and one audio click. The click is one grammar across body, light and sound.
   - Hunt and Chase blend continuously. The switch from latched to fluid is itself the cue.
5. **Footsteps from the gait phase.** `Step` is raised when a foot locks under the foot IK (§3.4); its position is the planted boot's ball.
6. **#A99E78 is retired** (§1.4).

### 1.7 How it squeezes through doors and under Low ceilings

**Low ceilings (2.4 m).**
- In both Low and Standard zones the shrug is **pinned** to the ceiling, 0.02 m under it (§3.3).
- The walk's bob goes into the knees, not the shoulders. That "a body that would be taller" read is pillar 2 of `11_squeezed_giant.md`.
- Entering a lower zone it folds fast (τ 0.25 s). Rising into a taller one it unfolds slowly (τ 1.0 s).
- In a Tall zone it rises fully to 3.27 m: the reveal.

**Doors (1.0 × 2.1 m, whether open or broken).** The squeeze is a three-key sequence driven by position, so it plays right at any speed:

| Key | Where the nav axis is (s along the heading; door plane = 0) | What the body does |
|---|---|---|
| door_in | s ≈ −0.62 | Turned 80°, right shoulder first. The hood ducks under the lintel and through. The right glove grips the jamb casing |
| door_mid | s ≈ 0 | Torso sideways in the opening. Spine bent sideways (roll −12 → −70° up the chain). Both shoulders compressed. Trailing knee down |
| door_out | s ≈ +0.6 | Hood rising on the far side. The trailing hip and boot pull through over the threshold. The left glove pushes off the wall behind |

- If the map slows the Relay at doors (open item 3), the same sequence simply plays slower.
- At full Hunt speed, 2.6 m/s, it is a 0.9 s duck-and-twist; at Chase, 4.2 m/s, a 0.6 s lunge.
- The visual body always squeezes, whatever the gameplay answer. A 2.86 m figure cannot pass a 2.10 m door upright.

---

## 2. Armature (H1)

The armature is hand-built in Blender, not with Rigify. The left side is built and then mirrored, so left and right bone lengths are identical (§9 item 6).

**Bind pose.** A neutral, symmetric "build" pose sits halfway between the extremes:
- the spine at half the std curve;
- the arms in a 35° A-pose;
- the knees bent 15°;
- the neck modelled at 0.60 m visible, with 0.20 m more already inside the collar.

All seven pose keys are bone poses of this one mesh. Every pose parameter set from the giant module is reused through its own fixed-length IK solver, so the poses are already in bone form.

**36 bones**: `root` (no weights) plus 35 deform bones.

| # | Bone(s) | Parent | Notes |
|---|---|---|---|
| 1 | `root` | — | Floor pivot. It carries the visual-only door yaw (±80°) and the body's offset from the nav axis |
| 2–5 | `pelvis`, `spine_01`, `spine_02`, `chest` | chain | The four spine segments of the module |
| 6 | `shrug` | chest | The pressed top. Translation and scale under the ceiling replace the blockout's `press_parts` vertex squash |
| 7–9 | `neck_01`, `neck_02`, `neck_03` | chain from shrug | Telescope: neck_01 slides ≤ 0.40 m into the collar; 02 and 03 stretch ≤ 1.25 × |
| 10 | `head` | neck_03 | Hood shell and lens, rigid (1 influence). A `lens` locator (not a bone) carries the emission pivot and the light |
| 11–12 | `cord_L`, `cord_R` | head | Drawcords: procedural pendulum, plumb |
| 13–26 | `clavicle`, `upperarm`, `forearm`, `forearm_twist`, `hand`, `fingers` (one curl group), `thumb`, each `_L` / `_R` | chain | The twist bones stop the long forearm wrapping like a sweet wrapper under the tape cuffs; the thumbs grip the jamb |
| 27–34 | `thigh`, `shin`, `foot`, `toe`, each `_L` / `_R` | pelvis | Two separate leg chains (C5) |
| 35–36 | `keyring`, `pouch` | pelvis | Dangles (pendulum), mainly sound sources |

**Influences.**
- ≤ 4 on cloth at the shoulders, hips, knees and elbows.
- 1 on rigid parts: the hood shell, lens, boot soles, keys, pouch body and tool handles.
- 2 elsewhere.
- No blend shapes, cloth, hair or physics (03 §4).

**Mapping to the rig's serialised fields** (the names stay; §5):
- rootBone → root, pelvis → pelvis, spine → spine_01, chest → chest, neck → neck_01, head → head;
- armL/R → upperarm_L/R, forearmL/R → forearm_L/R, handL/R → hand_L/R;
- thighL/R → thigh_L/R, shinL/R → shin_L/R, footL/R → foot_L/R.

Unity's left is −X facing +Z, as the legacy rig assumes. The FBX export goes through a new `creature_rig.py` helper rather than an edit to `kitlib.py`:
- `object_types {ARMATURE, MESH}`;
- `add_leaf_bones=False`, no baked animation;
- `FBX_SCALE_ALL`, forward −Z / up Y;
- primary and secondary bone axes chosen for Unity.

Additive rotations are applied in **bind-local space** (bind rotation × offset), so the imported bone rolls do not matter.

---

## 3. Blend scheme and runtime motion (H1)

### 3.1 Pose data

- One JSON `Resources/Creatures/Relay_NightShift_Poses.json` (a TextAsset, parsed once at `EnsureRig`) holds, per key:
  - the local rotation of every bone;
  - the pelvis and root offsets;
  - the neck slide and stretch;
  - the shrug press.
- The keys are `low`, `std`, `tall`, `door_in`, `door_mid`, `door_out`, plus additive keys for `listen_std` (the glove on the tile), `breakdoor` (the blow) and `stagger`.
- Every room and door key exists twice: **neck long** and **face +0.3**.
- A serialised `faceLift` (0..1, default 0, meaning the 11 default) blends between them. Red's neck answer becomes one number, and nothing is rebuilt.

### 3.2 Weights from `CeilingHeight` and `DoorSqueeze` (the API is unchanged)

- **Smoothed height.** h follows `CeilingHeight`, critically damped: τ 0.25 s going down, 1.0 s going up.
- **Room weights:**
  - `low = 1 − smoothstep(2.4, 2.9, h)`;
  - `tall = smoothstep(4.0, 5.2, h)`;
  - `std = 1 − low − tall`.
- **Room blend.** A sign-aligned nlerp of the three keys per bone, plus the lerped pelvis and neck offsets.
- **Door depth** d = `DoorSqueeze` (0 at 0.6 m out, 1 on the door line; `MapHunter.SqueezeReach`).
  - The phase sign σ is latched per crossing from the sign of dd/dt once d > 0.05: rising means approaching, falling means leaving.
  - Door weight `wd = smoothstep(0, 0.35, d)`.
  - The door key is `door_in → door_mid` by `smoothstep(0.35, 1, d)` while approaching, and `door_mid → door_out` while leaving.
  - Final: `slerp(room, door, wd)` per bone. The root yaw goes to 80° by wd.
- **Early warning ("lintel probe", rig-only).**
  - The std hood is 0.78 m ahead of the axis, so the head reaches the door plane before d rises above 0 (the map's reach is 0.6 m).
  - At 10 Hz while moving, two forward rays 1.5 m long are cast from the hood: one at the pose top and one at 1.0 m, against environment colliders.
  - A hit high but not low means a door head ahead, and it starts door_in from the estimated distance. A shut door hits both rays, so it never triggers.
  - Map contract M2 (SqueezeReach 1.2 m) would make the probe a backup only.

### 3.3 Constraints, after the pose and the gait layer

1. **Ceiling pin** (whenever `low + std ≥ 0.5`).
   - After forward kinematics, the `shrug_top` locator is measured, and the pelvis is moved, clamped to ±0.12 m, so that the top sits at h − 0.02 (the press is a 0–0.03 m compression of the `shrug` bone).
   - The legs are then re-solved by two-bone IK to the planted feet, so the knees absorb the move.
   - `PressesCeiling` is set true while the top is within 0.03 m of h, and false in Tall.
2. **Foot IK.** Analytic two-bone IK from thigh to shin. A foot in stance is locked in world space, so no foot slides.
3. **Head aim.**
   - Listen and Search: yaw toward `SetListenTarget`, latched in 15° steps (as today).
   - Walk: forward.
   - Run: head-lock toward `Camera.main` within ±70° yaw and ±30° pitch. That is direction only, never brightness, so it is not a radar.
4. **Camera clearance** (the catch, at either distance).
   - When the camera is within 1.5 m of the head in Run or IdleListen, the neck slides and pitches back so that the lens stays ≥ 0.30 m in front of the camera and no vertex comes within 0.15 m of it. The game's near plane is 0.06 m.
   - This makes the 0.7 m catch work even though the std hood is 0.78 m ahead of the axis, which is what HR16 warned about.
   - With the camera within 1.5 m in Run, both gloves rise toward the frame edges: the reach.
5. **Arrival.** A transform jump of more than 3 m between ticks, or re-activation, resets the gait phase, the contacts and the door latch. It snaps to the Listen pose with the lens on standby.

### 3.4 The gait and state layer (additive, procedural: option A, 03 §5.1)

**Cadence follows the body's own measured ground speed v** (as `FrontRoomsRelaySound` already measures it): step frequency = v ÷ the step length for the pose. The rig no longer infers speed from `speedScale`.

| Pose | Hunt (2.6 m/s) | Chase (4.2 m/s) |
|---|---|---|
| std | heavy bent-knee walk, 1.14 m steps, every 0.44 s (Fr ≈ 0.44 at hip 1.56: a real walk for a giant) | run, 1.45 m steps |
| low | deep-squat waddle, 0.9 m steps | a **gait-category change** (L8): a tripod gallop, the right knuckles planting on every second stride |
| tall | free upright walk | upright run |

**In the door keys** the gait amplitude drops to 30 %: a shuffle-through.

**State offsets:**
- **BreakDoor.** The forearm and gloved-fist hammer lands at 1.6–1.9 m on the leaf's upper panel, identical on every blow (copy-paste, 02 §2.6). The final blow carries through into door_in. Reach: the leaf face is about 0.425 m ahead of the axis at the 0.45 m standoff, checked in the clone (M8).
- **Stagger.** The hood jerks, the arms go loose, and the lens drops out (§1.5).

### 3.5 Contact events from real contact

**`CeilingBrush(Vector3)`**
- Raised only while `PressesCeiling` and moving.
- It fires at each step's bob peak, when the pin's compression is highest, with at least 0.5 s between events (about 2/s, as the hook's doc says).
- Point: `shrug_top` projected onto the ceiling plane.

**`Squeeze(Vector3, float)`**
- At 10 Hz while d > 0, overlap spheres sit at the leading shoulder (r 0.12), the hood (r 0.18) and the hips (r 0.15). They test environment colliders only (the player's layer is excluded, triggers ignored).
- The event is raised at the first real contact, and again each time `DoorSqueeze` grows by 0.3. Strength = `DoorSqueeze`; the point is `ClosestPoint` on the collider that was hit.
- If no collider is found, it falls back to today's rule (d crossing a threshold, point at 1.5 m on the axis).

**`Step(int, Vector3)`**: from the foot lock (§1.6 trait 5).

The rig carries no colliders (03 §4).

---

## 4. Budgets per LOD (H1, H3)

The mesh is imported from one FBX, with children `*_LOD0/1/2` giving a Unity LODGroup automatically. All LODs share one armature, so exactly one SkinnedMeshRenderer is active per LOD.

| | LOD0 (hero) | LOD1 | LOD2 |
|---|---|---|---|
| Triangles | **≤ 12 k** (cap 15 k, G4.1) | ≈ 5.5 k (45 %) | ≈ 2.2 k |
| Used | ≤ 8 m (screen height ≥ 0.27 at vFOV 76°) | 8–20 m (≥ 0.11) | > 20 m. **Never culled**: the last LOD's transition is 0, because a lit lens at 30 m must still draw (dark is never cover) |
| Bones / influences | 36 / ≤ 4 | 36 / ≤ 2 except shoulders and hips | 36 / ≤ 2 |
| Materials (draw calls) | 3 | 3 | 3 (the lens must keep its own submesh for the property block) |
| Shadows | cast | cast | off (lamps are off past 16 m) |
| Detail | the hood, lens, gloves and upper body get the UV share (the catch shows 1.07–2.17 m); the legs are cheap (03 §2.2) | silhouette and the three value bands | silhouette and lens |

**Textures:**
- desktop: twill 4096² A/N/S, lens 512 × 1024 A/N/E, trim 1024²;
- WebGL import overrides (WebGL tab only): 2048 / 512 × 1024 / 512.
- The memory of 4K on iOS is flagged to App Store · 构建 (handheld only, through its own platform tab, never Mac or Windows).

**Runtime budget (desktop, to be measured in the clone):**
- rig tick ≤ 0.25 ms on the main thread, covering the pose blend, two IK legs, the pin, the head aim and 3–5 physics queries at 10 Hz;
- zero allocations per frame after `EnsureRig`;
- GPU skinning (Batched); the Walk and Run point light adds one unshadowed Forward+ light.

The autopilot gate is unchanged: average ≥ 55 fps and p99 ≤ 33 ms (03 §4). Timings taken at load > 32 are marked UNDER LOAD and compared only as relative cost against the legacy rig in the same run.

**Bounds.** `localBounds` covers every pose (x ±1.0, y 0–3.4, z ±1.2); `updateWhenOffscreen` stays off.

**Prewarm.** `Relay_NightShift.shadervariants` is warmed in `EnsureRig`, which fixes the first-render spike of spec §8.

**WebGL tier** (`#if UNITY_WEBGL && !UNITY_EDITOR`, plus the WebGL quality level's own `maximumLODLevel = 1`): CPU skinning of LOD1 at most, no Walk light, sheen kept (it is cheap). Mac and Windows are untouched (`webgl-changes-webgl-only`).

---

## 5. Rig swap (H2): what `FrontRoomsRelayRig` keeps and what changes inside

### Kept, byte-identical in signature

The checks are reflection-tested against the base `64b9de9c` (§6 T6).

- `public sealed class FrontRoomsRelayRig : MonoBehaviour`, `[ExecuteAlways]`, on the scene's `Hunter`.
- `enum MotionState` with the same members in the same order.
- `event Action<int, Vector3> Step`, `bool RaisesSteps => true`.
- `void SetListenTarget(Vector3?)`, `void DoorBlow(int, int)`.
- `float CeilingHeight {get; set;}` (default 2.9), `float DoorSqueeze {get; set;}`.
- `event Action<Vector3> CeilingBrush`, `event Action<Vector3, float> Squeeze`, `bool PressesCeiling {get; set;}`.
  - The production body overwrites `PressesCeiling` on every tick.
  - Callers that never set it are unaffected (none in main sets it).
- `Body`, `Head`, `ArmLeft`, `ArmRight`, `LegLeft`, `LegRight`, `State`.
- `Configure(Material, Material, Material)`, `EnsureRig()`, `TickAnimation(float, MotionState, bool, float)`.
- Every serialised field name, so `FrontRooms3D.unity`'s serialised Hunter still loads.

### Changes inside

- `EnsureRig` looks for `Resources/Creatures/Relay_NightShift`.
  - When it is found, and the serialised `useProductionBody` is true (default), it instantiates the body as the child `RELAY BODY / Night Shift`.
  - The legacy `RELAY RIG / bones` hierarchy is set inactive at runtime. It is never destroyed, so the scene file never changes.
  - In edit mode the body is a `HideFlags.DontSave` preview, so `[ExecuteAlways]` never dirties the scene.
  - Bone references for the production path are runtime-only fields. The serialised ones keep pointing at the legacy rig.
  - If the prefab is missing, the result is exactly today's primitive rig.
- `TickAnimation` runs §3 for the production body; the legacy code path stays verbatim for the fallback.
- `Configure` stores the materials for the legacy rig only. The production body uses its three `Creature_*` materials from `Resources/Surfaces`.
- The new logic lives in new visual-owned files, to keep the `FrontRoomsRelayRig.cs` diff small:
  - `Assets/Scripts/Rendering/Relay/FrontRoomsRelayBody.cs` (pose blend, constraints, contacts);
  - `FrontRoomsRelayPoseSet.cs` (JSON);
  - `FrontRoomsRelayLens.cs` (lens, light, photosafe limiter).
  - All new C# is ASCII only, with no tuples. New `.meta` GUIDs are checked as unused in main.
- **Unchanged consumers:**
  - `FrontRoomsRelaySound` and `FrontRoomsSoundDirector` find the rig and use `Step` only.
  - `FrontRooms3DGame` calls `CreateHunter`, `UpdateRelayRig` and `DoorBlow`.

### Glass RT

`FrontRoomsGlassRTSystem.OnMapFound` registers `GetComponentsInChildren<MeshRenderer>`. A SkinnedMeshRenderer would vanish from the ray-traced glass reflections.

The fix is a separate minimal hunk:
- register the Relay through the existing `Deforming` path, using a baked mirror (`SkinnedMeshRenderer.BakeMesh` of LOD1 into a pooled mesh at ≤ 15 Hz, only while G14 is on);
- flag the lens `EmissiveLens`.

It is re-based after `glass-rt-g14-universal` lands, because that workflow is changing this file tonight.

### Ship set (visual-owned paths only)

- `Assets/Scripts/FrontRoomsRelayRig.cs`.
- `Assets/Scripts/Rendering/Relay/*` (new).
- `Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRTSystem.cs` (hunk).
- `Assets/Resources/Creatures/**` (new): the FBX, the poses JSON, the shader variants.
- `Assets/Resources/Surfaces/Creature_{TwillSpruce,LensOpal,TapeSilver}.mat` and `Surfaces/Textures/Creature_*`.
- `Assets/Resources/Rendering/FrontRoomsSurface.shader` (`_FR_SHEEN` hunk).
- `Assets/Editor/Rendering/FrontRoomsRenderSetup.cs`: **only** the three Creature_* `SurfaceDef` lines, as their own minimal hunk. `outlets_p4b` and `d16` change this file tonight.
- `Assets/Editor/Rendering/FrontRoomsRelayRigTests.cs` (new).
- `Tools/Blender/frontrooms_kit/creatures/relay_night_shift.py` and `creature_rig.py` (new).

The package is a v2 apply package `hunter_b`. Its gates are Roslyn `--all --ios` (COMPILE OK, because the rig is in Assembly-CSharp on every platform) and the tests in §6. Every hunk is re-based at package time with `W/tools/rebase_apply.sh`, and every base sha1 below is re-checked.

### Base sha1s (main `0ea38bb7`, 2026-10-22 08:1x; git status clean for these paths)

| Path | sha1 | Owner |
|---|---|---|
| `Assets/Scripts/FrontRoomsRelayRig.cs` | 64b9de9cd82c500d46b8be8fcf4c8a57012cd2c9 | ours |
| `Assets/Editor/Rendering/FrontRoomsRenderSetup.cs` | 256cf3a449266de795ba4675ec660e9f28af35a6 | ours (busy tonight) |
| `Assets/Resources/Rendering/FrontRoomsSurface.shader` | a82fe1872b133c2a92e96201cb44828323885cff | ours |
| `Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRTSystem.cs` | e0b81f36… (full: e0b81f363e60a92fbe4825f1be87a14731b91690) | ours (glass-rt workflow busy) |
| `Tools/Blender/frontrooms_kit/creatures/giant_b_night_shift.py` | 420daf754ed4543b81d20a21bb0f1e3909068f82 | ours (read as the source of the poses) |
| `Tools/Blender/frontrooms_kit/kitlib.py` / `creature_lib.py` | d6de07ab… / 77498b10… | ours (not edited) |
| `Assets/Scripts/FrontRoomsMap/FrontRoomsMapHunter.cs` | fb2fe3dd7a1b48e30f8e828f5605c2c2d910f325 | map (contracts only) |
| `Assets/Scripts/FrontRooms3DGame.cs` | 88c93b093bdcd2f515e230504a7aaaa2b1bf66fc | map (contracts only) |
| `Assets/Scripts/FrontRoomsHunter.cs` (catchDistance) | 57d9fad57af1bcf2539870627f8d623edea8c13d | map |
| `Assets/Scenes/FrontRooms3D.unity` (serialised Hunter; `catchDistance: 0.7` at l.1072) | 0fb29ab1adf81fb0a115666f9e49055bdfe1c1ae | not touched |

---

## 6. Readability test matrix

**Camera.** The game's own camera: vertical FOV 76° today, near plane 0.06 m, eye 1.62 m. Every panel is also rendered at the planned 72°.
- At 1080p, one metre at distance d covers 691 ÷ d px (743 ÷ d at 72°).
- The std figure, 2.86 m tall, is ≈ 659 px at 3 m, 329 px at 6 m, 165 px at 12 m and 99 px at 20 m.
- The lens is 46 × 92 px at 3 m and 7 × 14 px at 20 m.

**Tooling.** The rooms come from HunterLookdev and the KitLookdevAccess builders, plus one Play-mode capture through the game. The VL numbers are allocated here; their specs go to `W/vl_pending/hunter_b.md`, and the images to `production_b/images/`.

| Test | What | Pass bar | VL |
|---|---|---|---|
| **T1 Doorway at 3 m: the B-or-A gate** (runs first, §9 S2) | 10 §7.6 panel 3, with the room behind lit and the near side dim. (a) The **lightbox**: std pose standing in the lit room 1.5 m behind the door, framed by it. (b) The **squeeze**: door_in, hood through toward the camera. Each in lens Listen, Walk and Run, both neck options, at 76° and 72°. A's giant blockout (`giant_a_floor_sample.py`) in the same panels as a reference | **Cold read** by 3 independent critics who get no pitch or name, only "what is this figure's head or face?". **FAIL** if 2 of 3 say screen, TV, monitor, display, phone or tablet in any panel. Supporting measures, which do not decide alone: lit-outline RMS deviation ≥ 8 mm; no ring brighter than the hood; luminance across the lens ≥ 2.5 : 1; aspect ≥ 1.8 : 1 portrait | VL342 |
| **T2 The 2.05 m contrast bar, per room type** | The VL237 C4 method. A flat unlit ID render gives the mask; contrast = mean linear Y inside the mask against a 12 px ring outside it, as a plain ratio, with the WCAG form and a protan copy reported too. The mask is **clipped to the 0–2.05 m contract band** (the map's Relay body), and the full figure is reported alongside. Rooms: Level 0 Std / Low / Tall lit; Office lit (Office grade, fog 0.018); Level 0 dead cell and Office dead cell (lamp Dead or Off, neighbours lit); Run! B Emergency Power (when its kit has landed, else its look-dev harness); start room. × Listen / Walk / Run × glance-back at 12 m and 6 m: 48 measurements | **≥ 4 : 1** (plain) in every cell. Cells predicted at risk: Office lit and dead cells in Listen. Repairs, in order: sheen strength, lens standby level (≤ 0.10), Walk light, twill value (≤ #3B). Any cell still failing goes to Red as a reported exception, never a quiet pass | VL343 |
| **T3 Silhouettes at 6 / 12 / 20 m** | Black cut-outs at true screen size, front, back and side. Poses low, std, tall, door_mid. Lens dark and Walk-lit. Plus a 2–3-frame lamp-flash strip at 12 m (G3.3) and the 20 m dead-corridor read (lens only) | The head-and-shoulder block is identified in the flash strip by a critic cold read. Overlap (IoU) with `ref_human` scaled to the same height ≤ 0.60 from the front (the ceiling-pressed shrug is the distinct shape). At 20 m in Walk the lit lens is ≥ 6 × 12 px | VL344 |
| **T4 The catch** | {0.7, 1.3 m} × {neck long, face +0.3} × {std, low}, in Run with the lens at full: 8 frames. Frame band at 76°: 1.07–2.17 m (0.7) and 0.60–2.64 m (1.3) | The lens is wholly in frame with its centre in the middle third. The face-shadow tell is visible. No vertex is within 0.15 m of the camera (camera clearance, §3.3). No TV or phone read (T1 cold read repeated). Expected: face +0.3 needs the 1.3 m catch (std lens at 2.08 m with its top out of the 0.7 m frame); the long neck works at both | VL345 |
| **T5 Pose envelope and the squeeze sequence** | Blender diagnostics (the module's `log` lines) and Unity `BakeMesh` bounds for every key and both neck options. A door crossing sampled at s = −1.2 … +1.2 m every 0.1 m. A Low → Std → Tall walk-through | The 11 limits per pose (§1.3 table). Door slab ≤ 2.08 m and \|x\| ≤ 0.48. No neck vertex outside the torso hull. Pin error ≤ 0.01 m. Foot slide ≤ 0.02 m per stance | VL346 |
| **T6 Lens, photosafety, no radar, API** (EditMode tests, shipped) | Scripted state sequences, normal and Reduce Flashing. The same script with the camera at 2 m and at 30 m. Reflection over the public API against base 64b9de9c. Event rates | ≤ 3 transitions per sliding 1 s window. With Reduce Flashing, no envelope under 0.5 s. Lens and light series identical at 2 m and 30 m. Every base signature present. `CeilingBrush` ≤ 2/s and only while `PressesCeiling`. `Squeeze` at the start and every +0.3. `Step` alternates feet at the plant position. Bloom: lens p99 < troffer p99 | VL347 (timeline plot + states contact sheet) |
| **T7 In game** | `FrontRoomsInteractionAudit` (Relay door break, seed 516574485) and `FrontRoomsCxPlayRun -cxSeconds 120 -cxRelayDelay`. Scripts are force-reimported first (TOOLS known issue), and the run is aborted if READY is missing at about 60 s. The map's own suites: interaction tests, RelayNavTest, CxSuites signatures | No new exceptions beyond the two expected ones. Nav signatures and determinism unchanged (the body is visual only). Logged event counts. Relay cost measured against legacy (UNDER LOAD flagged) | in VL343 / VL347 |

Two independent critics review VL342–VL347 before packaging (an adversarial pass, as in C1–C6).

---

## 7. Sound hand-off (H4): the final body for 声音设计

This goes out as a note through `SendMessage`, or appended to the Inbox section of `Takeover_2026-10-18/briefs/sound.md` if the session is not running. 声音设计 owns all audio; there are no AudioSources on our side (`AUDIO_CONTRACT.md`).

| Hook (unchanged API) | When it fires | What makes the sound | Identity proposed for 声音 |
|---|---|---|---|
| `Step(foot, pos)` | At the foot lock, from the gait phase. Hunt about every 0.44 s; Chase faster; cadence follows the measured ground speed. The position is the boot's ball | Rubber-soled work boots about 0.45 m long under a ~3.2 m heavy body. Brass key ring and tool pouch on the belt | A low, heavy rubber thud with a sole scuff on carpet; the keys jingle once per step (a metronome in Hunt); the pouch thumps in Chase. Low squat or door shuffle → `RelayGait.Drag`. No wooden or asymmetric step (01 §6) |
| `CeilingBrush(pos)` | At each step's bob peak while pinned, ≥ 0.5 s apart; never in Tall | The 65/35 twill shrug and hood dragging across mineral-fibre acoustic tiles in an exposed T-bar grid | A dry fibrous rasp, plus the tile lifting and dropping in its grid (a hollow tick or clack) and the occasional grid rattle |
| `Squeeze(pos, 0..1)` | At first real contact with the jamb, then every +0.3 of `DoorSqueeze` | Twill shoulder and hip pressing the door casing (real wooden doors, `door-foley-wooden-realism`); the canvas glove gripping the casing; keys and tool handles knocking the frame; the boot toe catching the threshold | A casing creak under load, a cloth drag, a glove squeak, a metal tick. Intensity from the float |
| `DoorBlow` (map) | Each blow, identical; the last one carries through | Canvas-sleeved forearm and gloved fist on the leaf's upper panel | A doubled heavy thump, the same every time |
| Rig `State` (read-only, existing) | Each state change | The lens is a fluorescent lamp: magnetic-ballast hum and the glow-switch starter | The ballast hum is its voice: quiet in Listen, steady in Walk, rising in Run. A **starter click** on every Search pulse, the Chase strike, the final blow's re-strike and Stagger (`RelayClicks`, `RelayPresence` with `RelayState`). Volume follows physical 3D attenuation only, never a distance code |
| Arrival (map `Arrived`, plus M5) | Step-in | The struck troffer | Starter click and cycle at that lamp (the relay click). The body itself is silent on arrival |

Optional, if 声音 wants the hum to follow the light exactly: a read-only `LensLevel01` property. It would be additive and change no existing member. It is proposed only, not built unless they ask.

---

## 8. Contracts for the map (关卡设计): notes only, none edited

The map files are touched only as clone patches, written up as `production_b/20_map_contract.md` in a later stage.

| # | Ask | Why | If it never lands |
|---|---|---|---|
| M1 | `CeilingHeight` = the lowest ceiling over the Relay's cell and the next cell along `Heading` within 1.0 m (one line in `UpdateRelayRig`) | The hood is 0.78 m ahead of the axis: crossing from Std to Low, the shrug would clip the 2.4 m ceiling for about 0.3 s | The fast 0.25 s fold, plus an upward hood ray if the ceilings have colliders (checked in the clone) |
| M2 | `MapHunter.SqueezeReach` 0.6 → 1.2 m (one constant), and feed broken-window crossings into `DoorSqueeze` (G1.6; window head 2.00 m, so the door keys lower by 0.12 m when d comes from a window) | The giant's head reaches the door before d > 0, and windows have no squeeze at all | The lintel probe (§3.2) for doors; windows clip |
| M3 | Catch distance 0.7 or 1.3 m: **Red's open call**, one serialised number | — | The rig handles both (camera clearance) |
| M4 | `Arrive` also tests the visual top (CeilingHeight − 0.05, the pressed shrug) for visibility (G1.8) | The shrug at 2.3–2.85 m shows over cubicle panels when the 1.6 m and 1.95 m points do not | Pop-in over cubicles |
| M5 | At `Arrived` (step-in), strike one Dead or Off lamp within one cell: a starter-cycle override, ≤ 3 transitions/s; with Reduce Flashing, a 0.5 s fade-up. It stays lit afterwards, as a trace | The B arrival trait (§4, §9 item 3). It is a one-shot brightening, not a dimming at the Relay, but Red should confirm it does not break "no lamp change where it is" (§10 C2) | There is no arrival trace; the audio click still plays |
| M6 | Drop `C("A99E78")` from `CreateHunter` when convenient | Retire the mid-tone | No effect: only the legacy rig uses it |
| M7 | Door slow-down: **Red's open call** | — | The rig plays at any speed |
| M8 | Check the 0.45 m BreakDoor standoff against the giant's blow, in the clone | G2.4 | Hand a pose offset back to the map |

---

## 9. Production order and gates

All work happens in **one clone**, `W/tools/mkclone.sh proj_hunterb`, compiled with `-buildTarget OSXUniversal`, with `Assets/Scripts` force-reimported before the first Play run. Never two Unity processes on it. Blender runs as `Blender -b --factory-startup --python … -- …`, building into the clone. Python is `W/venv/bin/python`. No downloads.

| Stage | Work | Gate |
|---|---|---|
| S1 | Clone, compile, baseline `HunterLookdev -hunterOnly Relay_Current`. Re-check the base sha1s | 0 `error CS`; READY line present |
| **S2 (B0)** | Build the hero hood and lens first: high-poly hood, recessed prismatic lens, emission map with the tell, bakes. Set them on the giant B blockout body; render T1 with A as reference; run the cold reads | **T1.** PASS: continue with B. FAIL: per Red's 2026-10-21 rule, switch to **A Floor Sample**, tell Red at once with VL342, and carry over everything that is not B's mesh (armature, blend scheme, rig code, tests, contracts). The shared skeleton is why the fallback stays cheap |
| S3 | Production body in the bind pose; the 36-bone armature, mirrored; skin weights; seven pose keys × two neck options; LOD1 and LOD2; FBX and poses JSON | T5 |
| S4 | Retopology and bakes; the three texture sets; Creature_* `SurfaceDef` hunk; `_FR_SHEEN` hunk; RenderSetup run **in the clone only** | Look-dev contact sheet; no world-space swimming |
| S5 | Rig code (§3, §5), GlassRT hunk, EditMode tests | Roslyn `--all --ios` COMPILE OK; T6; existing suites green |
| S6 | T2, T3, T4, T7; critic loop on VL342–VL347; fixes | Every bar in §6, or an explicit exception for Red |
| S7 | Sound note (H4), map contract note, v2 package dry run, docs and VL specs | `DRY RUN OK`; the 游戏视觉 session claims it in `LANDINGS.md` and applies (Red: verified packages land at once; keep main compiling for the Alpha) |

---

## 10. What stays open for Red

**The three open calls.** Each is one number or flag, and nothing is rebuilt:
1. **The neck.** Keep the long craned neck (default, `faceLift = 0`), or the face 0.3 m higher (`faceLift = 1`). They are coupled: face +0.3 needs the 1.3 m catch; the long neck works with both. T4 shows all four combinations.
2. **The catch distance**, 0.7 or about 1.3 m (map-owned, M3). Camera clearance makes both work.
3. **Every door a squeeze** (the gameplay slow-down, M7). The body always squeezes physically; only the time it takes changes.

**Small confirmations.** These defaults are applied because the rules require them; Red can overrule each one:
- **C1:** Listen lens at a 6 % standby glow instead of off, because of the darkness rule.
- **C2:** the arrival troffer strike (M5), which brightens a lamp near the Relay once and never dims one.
- **C3:** the patch string `114`. The lettering is 平面视觉's.
- **C4:** the Walk self-light on desktop, a 1.6 m glow. 10 §9 Q2 had asked whether a Relay seen by its own light is acceptable; the darkness rule answers yes.

## 11. Risks

- **The TV or phone read** fails T1, and the fallback A applies. S2 runs first so that no body work is wasted.
- **The Office in Listen** may miss 4 : 1 on the whole-body mask. It is predicted, measured and reported, never quietly passed.
- **The neck telescope** may deform badly or poke out of the torso: checked by T5.
- **Concurrent edits.** RenderSetup (`outlets_p4b`, `d16`), GlassRT (`glass-rt-g14-universal`) and the surface shader may all change tonight. Each of our changes is a separate minimal hunk, re-based at package time.
- **Map contracts M1, M2 and M4 may never land.** The rig-only fallbacks are listed in §8.
- **Load (135–526 tonight).** First renders compile shaders synchronously, so allow 45 minutes or more. Every timing is marked UNDER LOAD.
