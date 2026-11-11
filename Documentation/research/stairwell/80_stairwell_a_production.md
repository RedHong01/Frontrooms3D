# 80 · Stairwell A production, step 1 (look A + the safe zone) · build + one test round, 2026-10-30

**Status: see the RESULT block at the end (filled from the test round).**
- Package: `W/apply/stairwell_a.sh` (convention v2; layout of `a13_exit_chain.sh`). Dry run + rehearsal: see the end.
- Clone: `W/proj_stairA2` = main 3ea3d7bb (synced 10-29 21:25; main unchanged since) + the payload + clone-only harnesses
  (`Assets/Editor/Audit/StairA/FrontRoomsStairAProbe.cs`, A13's `Assets/Editor/Audit/A13/` with one line: the dresser attached).
- Source: the rescued 10-22 code (`W/stairwell_a/rescued_1022_1603`, copied, the folder untouched), re-based on main,
  A13 (landed 10-29 18:54) and D1.6 (landed 10-29 20:07). Unity 6000.6.3f1. ASCII, no tuples, no GetInstanceID, LF.
- Red's decisions it implements: 10-22 13:00 look A ("全部按推荐"); 19:59 the remaining five items; 22:47 on VL325
  "选择这套，同时这种楼梯间其实是安全区" + "也就是说怪物是进不来的" (the safe zone).

## 1. What it is (what the stairwell looks like now)

The one-cell C22 stairwell X (the map line's `FrontRoomsStairwell`) and the EXIT door's stair side, dressed in look A:
- **Shell:** 8 in block in warm putty eggshell (#DACFBC, `Run_Block` 0.4 m world tile) over a tan semi-gloss band to
  1.1 m (#A39A86) with its 12 mm dark cut-in line (#4A4238); painted slab (#D2CEC4); grey porch-enamel landing fill
  (#BDBEB8); hollow-metal frames (#5F5A50).
- **EXIT door (map leaf + frame, finishes only):** putty steel enamel leaf (#B7B09E, L* 72) and frame (#5F5A50);
  three ball-bearing hinge knuckles; 10 in brushed kick plate and the STAIR A letter plate on the push (maze) face; on
  the pull (stair) face the regular-arm closer (solved every frame from the swing) and NO REENTRY / NEAREST REENTRY ON
  LEVEL {next}. The crossbar, the LIT sign and A13's chain stay the map's and `FrontRoomsExitChain`'s (not duplicated).
- **Signs (information-design line's art, SPEC v1.2/v1.3):** stair ID signs on the latch-side walls (south: LEVEL
  token(k); north: LEVEL token(k + 1); both LEVELS token(k) TO token(k + 1), NO ROOF ACCESS), tokens baked per floor
  from the glyph atlases (0, B1, B2 ... condensed from B10); **sign 7 AREA OF RESCUE ASSISTANCE** (#004A8C, white legend
  + ISA) in its white steel box over the south ID sign, box bottom 1.753 m, lit steady on the emergency circuit (stays
  lit through the floor change's blackout); the far (north) door: REENTRY ON THIS FLOOR + a lever on its stair face,
  **NOT AN EXIT** (black on white, never red) on its corridor face; the sealed south leaf: knuckles, closer, NO REENTRY.
- **Light:** a 4 ft two-lamp wraparound (Run! B kit) on the slab with two soft-shadowed spots (2.2 each, range 4.2 m);
  the twin-head battery unit (Run! B kit) high on the hinge-side wall, heads dark, red AC pilot lit (mains: it goes
  dark in the blackout); EMT feed to the slab. The map's unshadowed stairwell lamp is switched off on desktop; phones
  and WebGL keep it (no extra light, half-resolution plates, a fixed reflection cube).
- **Reflection:** the stairwell's own probe (realtime, rendered once when first lit; a fixed cube on the reduced tier),
  importance 10, so X no longer takes Level 0's reflection.

## 2. The safe zone (Red 10-22): how it reads protected

1. **Its lights follow only the stairwell's LampLevel** (the C22 blackout at the floor change, 0.3 s each way after the
   door has latched) and Visible (X's chunk built). They are not map fixtures, so `FrontRoomsWarnLamps` never selects
   them: W1 bursts pick the player's room's map fixtures (`LiveLampsOf` -> `world.LampLevel(c)`), and X has no fixture
   (SetExitArea leaves its lamp out). No WarnBurst, no M1 hold, no M2 strike hold can reach them.
2. **The map's lamps no longer light the inside.** They are unshadowed 10-12 m spots and lit X straight through its walls
   (C22 note item 1), so a W1 dip in N showed inside. URP light layers are off (FrontRooms_URP.asset; Red's station
   call: off unless a leak is seen), and with them off URP writes no light layer mask (ForwardLights.cs:697), so a
   rendering-layer split cannot work today. Instead `FrontRoomsSurface.shader` gets a material keyword `_FR_OWN_LIGHTS`
   (shader_feature, only the Stair_*Inside / planar Stair_* materials): additional punctual lights with range over 6 m
   are skipped (URP 17 stores 1/range^2 in attenuation.x on every platform). Every other material compiles and renders
   exactly as before. The two leaves get a 0.8 mm own-lights skin on their stair face (the cube leaf is one renderer).
3. **No CueInk** inside: no stair surface has the print layer (CueInk lives in `_FR_PRINT`).
4. **Threshold:** the heavy putty steel door, its closer and latch; no vision panel in either door, so no Relay
   sightline in. The map side (A27) keeps the Relay out; nothing here changes that.
- Proof numbers: section 5.

## 3. Budgets (desktop, per stairwell, measured in play)

See the RESULT block.

## 4. Left for the walkable stair (60_walkable_stair_for_map.md) and after

- The walkable two-level shell (`FrontRoomsStairShell`, flights, landings, pickets, rails, standpipe + hose valve,
  turn-landing light) is not built: this package dresses today's one-cell X. Everything here is reusable there: the
  materials, the own-lights gate, the plates and their per-floor bake, sign 7, the closer, the skins, the wraparound
  and twin-head. The dresser keys on `FrontRoomsStairwell` part names; the walkable shell replaces that hook.
- **Not done here:** the painted stencil floor tokens (Heros stencil 15 in, `out/stencil`, needs a runtime rasteriser
  or a baked set per token); the LEVEL B1+ evac-plan placard by the far door (A35: sheetA_phase0_B1 per floor; needs
  the PlacardMount per-floor pick); EXAMINE's CellLightAt for the stair (`StairwellLampAt` is public for Rendering/Inspect).

## 5. RESULT (2026-10-30 10:58) · READY, not applied

**Package:** `W/apply/stairwell_a.sh` 3b152507 (bases ab1b5586, expected 1184b87a, keep d590bffd, diff e62e131b), 147 paths
(146 new + `FrontRoomsSurface.shader` replaced, its .meta kept), payload 8.9 MB. Bases = main at 10:57 (Game c931f58e,
MapWorld 77489d77, Stairwell ceafb945, ExitChain 28a6d000, shader 51e43465). Main moved overnight only in files the
package does not touch (Settings, ScreenVideo, Input, Game): no rebase needed.
- `bash /Users/redwang/FrontRoomsVisualWork/apply/stairwell_a.sh` -> **DRY RUN OK** (147 bases, 1 kept .meta, 147 payload
  files, 74 new GUIDs unused in Assets/, section 5 API all declared).
- Rehearsal on a cp -Rc scratch of main (`W/stairwell_a/work/scratch_main`): **MANIFEST OK** (147 paths, 1 kept .meta
  unchanged); a second run says ALREADY APPLIED.
- Apply: `bash /Users/redwang/FrontRoomsVisualWork/apply/stairwell_a.sh --apply`
- Key sha1s: Dresser 3f562e96, Kit 9768419b, Signs 4d57aaa3, DressOwner a1affcea, shader d748e5d3.

**Roslyn (main + the 4 C# files):** macOS COMPILE OK, iOS COMPILE OK, 0 errors, no warning from the new files
(`W/stairwell_a/work/roslyn_main_{mac,ios}/`).

**Test round** (clone W/proj_stairA2, Unity 6000.6.3f1, silent; logs `W/logs/stairA2/`):
- r1 (setup): the clone had no FMOD banks (FMOD/FrontRooms/Build/Desktop): the only error, so the verdict FAILed with
  exits 27/27, station 15/15, doors 2/2 ok. Banks copied from proj_titletv (= main's, md5 checked).
- r2: `-autopilotExit -autopilotStation 4 -autopilotSeed 2554` **PASS** (errors 0; EXIT checks 27/27, station 15/15,
  doors 2/2), floors 1 -> 4 (LEVEL 0 -> B3): every floor's EXIT built and dressed (4 builds, 4 shells), sealed, changed
  and the next floor started in the stairwell.
- One fix (rule 23): plate uploads 4 -> 2 px/mm on desktop, 2 -> 1 px/mm reduced (memory, see 3) + the leaves' stair-face
  skins (found by r1's trace). r4 = the same exit run after it: **PASS** (errors 0, 27/27, 15/15, 2/2, floors 1 -> 4,
  changes 61 / 50 / 45 ms).
- A13's own ladder (its harness, hall, poses and `measure_ladder.py`; only the dresser attached): PASS 3/3.
- MapInteractionTests / MapVerification not run: the dresser attaches only at FrontRooms3DGame.MapRunStarted, which no
  edit-mode suite raises; the exit run is the covering suite.

**Safe-zone proof (r4):**
- The stair's own lights: sum 4.40 (2 x 2.2) on every one of 6,976 lit stairwell-frames; 0 changes not explained by
  LampLevel. The only level changes are the two floor-change blackouts (frames 422-441, 1929-1948: 1.0 -> 0 in 0.3 s
  after the latch, then back), as C22 D9 designs; the map's point lamp was never on while the spots stood.
- The game's own W1 burst ran while the player stood in X (62 frames, t 21.47-22.49 on floor 1): the own lights read
  4.40 on all 44 full-level frames, then the blackout.
- Our W1 trace (FrontRoomsWarnLamps at stage 2 in N's room, 5 bursts, N's lamps down to 0.55 of base) with an inside
  camera, t 4.2-19.9 s, 102 samples per gate state: **inside, as shipped: 0.14910-0.14920 mean Y (0.07 % spread; 14
  samples inside dips 0.14910-0.14920)**; the same frames with the gate off (main's shader path): 0.14910-0.18280
  (21 %), following the maze in N (control 0.021-0.184). Samples outside the window were camera-state artefacts (the
  title glide, the unlock shot), listed in the TSV.
- Lights reaching X's centre: only the two own spots (range 4.2 m, passing the gate); the map's lamps (10-12 m) are gated out.

**Lock contrast (A13's ladder, desktop LOCKED, leaf / lock):** 5 m 3.97, 8.6 m 4.65, 12 m 4.22, 20 m 5.23, 20 m 45 deg
5.26, 30 m 5.53 (A13 on main's veneer: 2.01 / 2.41 / 2.11 / 2.77 / 3.11 / 3.24; study A: 4.77 / 4.77 / 5.04 / 4.91 /
6.00 / 3.22). Phone: 3.93 / 5.38 / 4.26 / 5.50 / 6.96 / 5.12. Leaf mean Y 0.27-0.29 (study A's putty .29). Lock pixels
unchanged (781 / 242 / 124 / 46 / 40 / 19 desktop). In play, floor 1's EXIT is a dark dead end: same-pose putty vs
veneer there +38 % leaf Y (whole-leaf means, `probe_report.txt`).

**Budgets (desktop, one stairwell + its EXIT door's dressing, before the seal):** 54 renderers (28 dress), LOD0
31,729 tris (21,657 dress; the biggest: the twin-head unit 10.5k, the far door lever + rose 6.7k, the closer 6.2k), 2 realtime soft-shadowed spots
(the map's point lamp off), 1 realtime reflection probe (64 px, rendered once), 28 materials. After the seal the sealed
leaf's dress adds ~6.3k tris. Plates: ~21 MB a stair at 2 px/mm (6 sets x 3 RGBA32 + mips), two stairs live on floors 2+
~37 MB; 4 px/mm would have been ~86 / ~150 MB. Reduced tier: the map's unshadowed 1.5 m lamp, no probe render, 1 px/mm
plates (~5 MB a stair).

**Images (Shot format):** `W/stairwell_a/images/` stairA_from_exit_door.jpg, stairA_door_from_inside.jpg,
stairA_signs_reading.jpg, stairA_lock_contrast.jpg, stairA_safe_zone_lamp_trace.jpg. Raw: `W/proj_stairA2/Verification/stairA/`
(play_station_on_r4/, a13_ladder/ladder/). VL rows: batched by the line (not placed).

## 6. Step 2 · the walkable stair (stages STAIR author, package and render, 2026-11-10)

**Status: landed 15:57 with 关卡设计's map half (15:55), `walkableStair` off; rendered in Unity 16:18-16:32 (6.9); a follow-up package
of ours is staged (6.10).** Sections 6.1-6.8 are the author's and package stages' record, kept as written. Red, 2026-11-10 11:44:30 (with
`images/stair_dir_a_far_approach.jpg`): "之前在做的这个楼梯间去哪了？为什么没有继续推进了"; 12:02:49: "现在就开"; 11:11:45: "目前先别做平面视觉相关的工作，
把额度用在所有3d视觉相关的" (3D only: the stencil numeral and the B1+ placard wait). The map half is 关卡设计's (`60` v1.1, their
interface `FrontRooms.Map.IFrontRoomsStairShell`, agreed 12:1x); ours is the shell. Contract for them:
`W/apply/stairwell_walkable_contract/README.md` (rendered). Section 4 above listed what this step builds; it is built here.

### 6.1 The shell (`Assets/Scripts/Rendering/Stairwell/FrontRoomsStairShell.cs`)
- A new builder, not a dresser: the map calls `FrontRoomsStairShellHost.Create(game root, X1's door-edge centre, +z)` (registered at
  load; behind the profile flag `walkableStair`, default off: with it off nothing of this exists and C22's vestibule is dressed by
  step 1 exactly as before). It implements `IFrontRoomsStairShell` and `IFrontRoomsStairShellVisible`, plus `SetLampLevel` / `LightAt`
  (agreed, not yet in their staged file) and the static `LightAtAny` for EXAMINE.
- Frame (Root-local): origin X1's door-edge centre on the exit floor, +z into the stair, +x the EXIT's latch side. Exit landing y 0,
  turn -1.75, far -3.50; inner faces x +-1.33, z 0.17 .. 5.83 (inset 0.11, 0.06 thick: C22's vestibule numbers, so step 1's EXIT
  closer shoe lands on this exit frame); clear 2.66 x 5.66 m; landings 1.57 m; flight 1 on x 0.21 .. 1.33 down +z, flight 2 on
  x -1.33 .. -0.21 down -z; well 0.42 m; slab at +3.30; seal plane z 1.40 (60 s3, s4, s6.1).
- Walls: CMU in `Stair_BlockWalk` over `Stair_BandWalk` to 1.1 m over the walking line, following both flights and wrapping the landings
  (BSS 120 fig. 2.7.5; 10_directions 2.1), the 12 mm cut-in line on top; built as one mesh per material with planar metre UVs in the
  shell's own frame. **Why new materials:** step 1's Stair_Block / Stair_Band project in world space and carry world-space macro wear,
  so the variant-B move (+3.50 m with the player) would slide the CMU courses half a course (17.5 courses of 0.20 m) and shift the
  tone in the frame of the change; the Walk pair is the same material in mesh UV with no macro wear, so nothing on the walls moves.
- The far-door wall: the far opening (1.00 m) right under the exit opening (1.12 m, C22's south doorway); its corridor thickening
  (z -0.08 .. 0.11, to 3.06 m over the far landing) closes X1's south line on the new floor as C22's JoinNextFloor end wall does, its
  faces take N-prime's paper (`SetFarWallPaper`), its reveal framed on the corridor side in the map's trim.
- Doors: the static shut EXIT leaf on C22's south-hinge frame (Kit_DoorLeaf_StairWide, knuckles, closer, NO REENTRY; shown by
  `ShowExitLeafShut`); the far door hinged on the +x jamb, the landed Kit_DoorLeaf_StairNorth unmirrored (pulled from the stair),
  95 deg into the stair over the far landing (60 s6.6), levers both faces (the stair one moves for the hold pose), closer on the stair
  face, REENTRY / NOT AN EXIT plates; Open / Hold / Close on FrontRoomsShotTimings.Open as C22's north door, the leaf yields at a
  body (交互设计 11-10). Its hinge is named "Door hinge stairwell ..." so the sound layer attaches its door sound as on C22's.
- Colliders (60 s6.3): one ramp per flight on the nosing line (3.30 m at 32 deg, run one going past the last nosing so it meets the
  landing at its level), boxes for both landings (the exit one reaches under the EXIT's line: the doorway has floor), sloped guard boxes
  on the well-side stringers, the 42 in landing guard and the well guard, the barrier, the hose stations and the riser; the walls and
  the shaft floor (far landing) from the shell's boxes. The visible steps carry none.
- Light (safe zone): three Run! B 4 ft wraparounds (on the slab over the exit landing, on the far wall at 2.55 m over the turn landing,
  under the exit landing over the far landing), two soft-shadowed spots 0.6 m apart each (170 / 120 deg, range 5.6 m under the 6 m
  own-lights gate; 60 s6.7's six), one plain spot each on the reduced tier; the twin-heads at both door landings (red pilot = mains);
  all follow only `SetLampLevel` and the camera's distance (off beyond 14 m); no map fixture, so no warn burst reaches them. Two box
  probes (realtime once on desktop, the fixed cube on phones and WebGL). Every inside surface own-lights.
- Signs: the stair ID plates (step 1's FrontRoomsStairSigns, per floor from `SetFloor`: token(k) at the exit landing, token(k + 1) at
  the far landing) and sign 7 at both door landings; the stencil numeral is a hook only (`StencilPainter` + anchors on the hinge side
  of both doors) for 平面视觉.

### 6.2 The kits (Blender, `Tools/Blender/frontrooms_kit/assets/_struct_stair.py` + `structures_stair_*.py`; frames in the module)

| Kit | 1988-91 construction (cited) | Size | LOD0 / LOD1 / LOD2 |
|---|---|---|---|
| Kit_StairFlight_SteelPan x2 | steel pan stair: C10 x 15.3 channel stringers (254 x 66, toes out), 12 ga formed pans with 38 mm concrete fill, the pan's turned lip as the nosing, closed plate risers (01_research 1.1; NAAMM pan detail); UBC 1991 3306(c) rise <= 7 in, run >= 11 in; 3306(b) 44 in | 10 x 175 / 9 x 280, 1.12 m | 4,636 / 704 / 356 |
| Kit_StairLanding_SteelPan x2 | 50 mm fill on a 3 mm pan, C10 header on the flight edge, C6 joists | 2.66 x 1.57 m | 540 / 120 / 36 |
| Kit_StairGuard_Flight x2 | UBC 1991 1712: guard at handrail height along flights (34-38 in, 3306(i)), 1/2 in square pickets closing a 4 in sphere, bottom rail 2 in over the nosing line (6 in triangle); 1-1/4 in NPS top rail (42.2 mm), 1-1/2 in newels; hand-worn bare-steel top | 0.91 m over the nosings | 4,108 / 536 / 72 |
| Kit_StairGuard_Landing_1540 / _420 | UBC 1991 1712: 42 in (1,067 mm) at landings | 1.54 / 0.42 m | 2,848 / 432 / 96; 588 / 84 / 24 |
| Kit_StairRail_Wall x2 | UBC 3306(i) / ADAAG 1991 4.26: grip 1-1/4 to 2 in, 1-1/2 in clear of the wall, ends returned; ADAAG 4.9.4(2) 12 in level at the top, a tread + 12 in at the bottom; G1's bracket and return (P4) | 0.91 m | 8,396 / 2,504 / 864 |
| Kit_StandpipeHoseValve x2 | UBC 1991 3805(c) / NFPA 14-1990 Class I: a 4 in riser, a 2-1/2 in angle hose valve at every floor-level landing, outlet 3-5 ft over the floor; reducing tee, grooved couplings, red handwheel, NST cap and chain | outlet 1.37 m | 9,564 / 2,272 / 788 |
| Kit_StairBarrier_2660 | ADAAG 1991 4.4.2: a cane-detectable barrier where the headroom under the stair drops below 80 in (z 3.72) | 2.66 m wall to wall | 2,304 / 496 / 192 |

The riser runs between the valve kits (C#, 114.3 mm, sleeved through the exit landing and the slab, wall straps); a sprinkler head at
the top of the shaft (NFPA 13-1989).

### 6.3 Materials (`Resources/Surfaces`, FrontRooms/Surface, `_FR_OWN_LIGHTS`, mesh UV, no world macro wear)
`Stair_SteelEnamel` #47443E, `Stair_RailEnamel` #3E3B36, `Stair_RedEnamel` #8E1E17 on main's PaintedMetal A/N/S (colour = target over
the map's mean, the rule of Stair_DoorSteelInside; normal 0.30-0.35 and a roughness floor for the 11-10 09:52 speckle note);
`Stair_RailWorn` #A8A9A4 metallic 0.85; `Stair_ValveBrass` tarnished cast brass (factor .51 / .43 / .29, smoothness .38);
`Stair_PrimerChip` #6A3323; `Stair_BlockWalk` / `Stair_BandWalk` (6.1). Textures +0 MB. No `_Phone` materials: the maps are main's
shared PaintedMetal / Stair_Block maps phones already load for step 1; the phone and WebGL tier is the kits' LODs by distance, three
plain spots, fixed probes and 1 px/mm plates. Generator: `W/structures_assets/stairwell/
tools/make_stair_walk_mats.py`.

### 6.4 Look (the author's own looks, Blender Cycles of LOD0; Unity frames come with the render stage)
`images/stair_walk_kits.jpg` (+ `_fit.txt`, ALL LABELS FIT): five assembled views at 3 m, four heroes at 0.35 m (nosing, guard top,
rail end, hose valve), six kits alone at 3-4.4 m, COST / PERIOD / STATE. Fixed on the way: r1 the nosing chips read as flat orange
rectangles and the valve as new yellow brass; r2 the chips (2-5 cm flakes) read as stickers; r3 = thin bare-steel wear streaks under
the lip's top edge along both walk lines with 1-5 mm primer flecks, tarnished brass; the look-dev's CMU courses run horizontal.

### 6.5 Cost (desktop, one shell)
LOD0 about 117 k tris (new kits 60.2 k; landed kits it places 54.6 k: wraparounds, twin-heads, door leaves and frames, locks, closers);
our estimate was 85-95 k: the wall rails (8.4 k each, G1's brackets 5.8 k) and the hose stations (9.6 k each) are over their plan
lines and kept at hero detail (both are passed within a metre). LOD1 34 k. Textures +0 MB; 8 materials. Lights: desktop 6 soft-shadowed
spots, reduced 3 plain; probes 2. Draws and frame time: measured at the render stage.

### 6.6 Gate, files, next (resume)
- Gate 14:5x on main eb220af8: `W/structures_assets/stairwell/gate` (shell + STAND-IN interface = 关卡设计's 0def9eda + the two agreed
  members), `gate_theirs` (their file verbatim), `gate_harness` (+ harness): FR GATE OK, macOS + iOS 0 errors each.
- Package `W/structures_assets/pkg/stairwell_walkable` (60 files, `GUIDS.txt`); it needs their `IFrontRoomsStairShell.cs` in main
  first. Clone-only harness `W/structures_assets/stairwell/harness/Assets/Editor/StairWalk/FrontRoomsStairWalkHarness.cs`.
- Next (render stage, on AC power, slot order G2-d, STAIR, G3, G4): apply package + their interface + harness in the clone; stills,
  walk (3.2 / 5.3 m/s down, 3.2 up), door, change, light film, reduced; NEW off = today (flag off: the vestibule frames equal main's);
  flash_check of the change and the turn light; drop the DRAFT marks; then the package stage.

### 6.7 Package (stage STAIR package, 2026-11-10 15:1x; the render stage was blocked on power, so no Unity frame yet)
- **Render blocked (15:08):** the MacBook has been on battery since 14:19 (pmset: USB-C unplugged); our Unity runs only on AC, so the
  render stage started no Unity, took no slot and left the clone alone. Everything below is file work and Roslyn; the look is the
  Blender sheet of 6.4 only.
- **`W/apply/stairwell_walkable`** (convention v2, `W/tools/mkapply.py`, 15:14): 59 paths, all new (bases ABSENT), 25 new GUIDs
  (unique, none of main's 4,288), no CR. Step 5 of its script is the land-order lock: it refuses, writing nothing, until
  `Assets/Scripts/FrontRoomsMap/IFrontRoomsStairShell.cs` (关卡设计's) is in main with `IFrontRoomsStairShell`,
  `IFrontRoomsStairShellVisible` and `FrontRoomsStairShellHost.Create`. Dry run on main 15:14: steps 1-4 OK, step 5 REFUSED (by design).
  Rehearsal `W/_rehearsal/stairwell_walkable_151443` (main's metas + their 0def9eda, read only): DRY RUN OK, MANIFEST OK 59, ALREADY APPLIED.
  Their file moved to 548ed75f at 15:17 (S1 in it, the three guard strings unchanged): `gate_theirs` re-run 15:20 on main eb220af8,
  FR GATE OK macOS + iOS 0 errors, 2 overrides.
- **Metas without an import:** `W/structures_assets/stairwell/tools/make_full_kit_metas.py` writes the full importer metas by main's
  `FrontRoomsKitImporter` rule (template = a fresh kit meta of this Unity; `externalObjects` = the sidecar slots with a Surfaces material,
  sorted by name; LOD screen percentages by LOD count) after proving it on five of main's kits (byte-equal). The 8 FBX metas keep the
  author's GUIDs; the guid-only originals are in `W/_partial/stair_pkg_metas_guidonly_20261110_1513`.
- **Gate 15:15-15:16 on main eb220af8 (main_fp ec612c2cdb15, moved since 14:57):** `gate_theirs` (their file verbatim) and `gate`
  (stand-in with S1) FR GATE OK, macOS + iOS 0 errors, 2 overrides each; `gate_harness` OK at 14:58.
- **Land order (agreed with 关卡设计 15:1x):** their `IFrontRoomsStairShell.cs` + host, then `stairwell_walkable`, then their map half
  with `walkableStair` off; our render stage on AC; `walkableStair` on together in the combined test. Contract:
  `W/apply/stairwell_walkable_contract/README.md` (still DRAFT: it drops the mark after the render).

### 6.8 Combined-test rows (the one test at the stopping point, rule 25; flag on in a test profile)
| # | Where | Do | Expect |
|---|---|---|---|
| ST1 | any floor, flag OFF | reach the EXIT, open it | C22's vestibule as today (stairwell_a's dress, its light); no shell (`FrontRoomsStairShell.Alive` 0) |
| ST2 | EXIT, flag ON | open the EXIT, step in | the exit landing in look A: CMU over the tan band, dark pan flight going down +z, picket guard, wall rail, red standpipe + valve by the door, wraparound on the turn wall |
| ST3 | flight 1 + 2 | walk down at walk speed, then run (Shift = slow walk) | no stick, no hop, no fall through; head clears the slab; the feet follow the nosings |
| ST4 | turn landing | stand under the wraparound | steady light, no flicker, no warn burst in the stair (safe zone, own lights only) |
| ST5 | the change | keep walking past flight 2's foot | nothing on the walls slides or pops at the +3.50 move (no black frame unless the map half's seal asks for one) |
| ST6 | far landing | step onto it | the far door opens by itself, silent hold, no USE prompt, the leaf waits at the body and never pushes; the new floor beyond |
| ST7 | far door | walk out, look back | the leaf closes and latches; the corridor face papered as the floor (thickening, no gap at X1's south line) |
| ST8 | iPhone / iPad | ST2-ST6 | same systems (three-platform parity); 3 plain spots, no hitch at the change |
| ST9 | walk back up | turn round on flight 2 before the change | climbs without a stall (the harness checks 3.2 m/s up) |

### 6.9 Render (Unity, 2026-11-10 16:18-16:31)

Clone `W/proj_unlock` = main with both landings (关卡设计's map half 15:55, our `stairwell_walkable` 15:57), synced by a targeted copy
(the 75 landed files, sha1-checked; all 455 main .cs equal), Unity 6000.6.3f1 edit-mode batch, silent, private product name, 0 error CS.
Clone-only harnesses in `W/structures_assets/stairwell/harness/` (map harness on a real map floor: TODAY = C22's vestibule, NEW = the
walkable stair from the factory; walk harness: the shell between plain cells). Renders `W/structures_assets/stairwell/renders/unity/`.

- **NEW off = today:** the map harness on C before the landings and after them (flag off): 0 px over 1/255 in the vestibule at all 8
  cameras, desktop and reduced (`renders/unity/newoff.tsv`).
- **Walk:** at the game's own grounded stick (-1 m/s, `FrontRooms3DGame.cs`) the player is airborne 0.22 s (Walk 3.2) and 0.50 s (Run 5.5)
  at the top of each flight; with -6 m/s every walk passes (0.05 s). Up passes at every stick. Suggestion S4 to 关卡设计 (README 4.7).
- **Guards** hold (4 pushes), **far door** passes 交互设计's rules (silent 1.5 mm hold, no prompt, 95 deg, latch, body yield).
- **Change:** no blackout on desktop; with the camera moved with the shell 0.3-1.3 % of pixels move by more than 2/255 (edges); the film
  shows no pop. A harness artefact was found and fixed by the look: plain-cell lamps of 5 m range passed the 6 m own-lights gate and lit
  the shell's walls through them (map lamps are 10-12 m and are skipped).
- **flash_check** (area rule, worst second): SetLampLevel film 0.0, turn film 2.0, film of the change 4.0 in the stair part (3.5 with the
  stair stick), caused by the turn landing's wraparound on the far end wall in the line of sight down flight 1: a follow-up moves it
  (slab underside or side wall) and re-measures. The far door opening onto the harness's plain lit cell also counts 4.0; the game's
  N-prime is re-measured in the combined test.
- **Look:** look A reads in Unity (CMU + band and its cut-in along the flights, pan treads with the dark lip, picket guards with newels,
  rails on returned G1 brackets, red riser and brass valve, plates); the nosing hero at 0.35 m framed a riser plate and is re-rendered on
  AC (camera fixed). Import changed no landed .meta.
- **Sheets:** `images/stair_walk_then_now.jpg` (PASS), `images/stair_walk_heroes.jpg` (PARTIAL), `images/stair_walk_gate.jpg` (FLAG);
  film `W/structures_assets/stairwell/renders/unity/walk2/stair_walk_change_film.mp4` (silent, 60 fps).
- **Follow-up package (ours):** the turn light's position, the far-door thickening's top from `FarCeiling` (关卡设计's ask), the nosing hero: 6.10.

### 6.10 Follow-up package (staged 2026-11-10 16:54, not landed)

`W/apply/stairwell_walkable_light_farwall.sh` (convention v2, `W/tools/mkapply.py`): one path, `Assets/Scripts/Rendering/Stairwell/
FrontRoomsStairShell.cs` f69ba1cd (main) -> 5eed32cd, its .meta kept; dry run on main OK; fr gate `W/structures_assets/stairwell/gate_fix`
FR GATE OK macOS + iOS 0 errors, 1 override. 关卡设计's interface and files are untouched; the landed package is not rebuilt.
- **The turn landing's light (ours, from 6.9's flash count).** The wraparound leaves the far end wall for the +x side wall (flight 1's
  outer wall) over the turn landing: 2.55 m over the landing as before, its lamp along the landing (z 4.38-5.62), its lens toward the
  well, the two spots 0.6 m apart along the lamp, aimed down and out over the landing and a little toward the far wall, which they wash
  (the 10-22 rule: the wall you walk toward is washed). Walking down flight 1 now shows the fixture's end and the far wall washed from
  the side, where the first build put the whole 1.2 m lens face in the line of sight and left the end wall dark round it. The look of the
  10-22 frames changes there: the light at the end of flight 1 is a raking wash on the end wall, not the lamp face-on. The slab's
  underside was ruled out: 5.05 m over the turn landing, and the stair's spots stay under the 6 m own-lights range. Where the flashes
  came from (my look at the stick -6 film): the worst second held three events at the end of flight 1 (the lamp leaving the top of the
  frame, bright side walls against the dark end wall) and four in the turn on the landing (the harness camera turns at up to 150 deg/s,
  twice the LW0 check's 76); a second window over the limit, 0.0-0.7 s, is the step from the EXIT onto the exit landing (3.5 at S4's
  stick, 4.0 at the game's), where the camera yaws toward flight 1 across the dark end wall and the face-on lamp. Both are the same
  contrast; the AC render films four placements side by side (built: +x wall aimed at the far wall; +x wall aimed at the flights; the -x
  wall; the landed far end wall) and the built one stays only if its stair part counts 3 or fewer.
- **The far-door wall's top (关卡设计's ask a).** `FarWallTopFor(ceiling) = max(ceiling + 0.16, door head + 0.05)`; `BuildFarWall()`
  builds the three boxes with the shell and again whenever `FarCeiling` is set to a new height: the same renderer takes a new mesh
  (N-prime's paper and its `_CeilingHeight` block stay), the same three box colliders are resized. A Standard N-prime gives 3.06 m as
  before, so nothing moves on a Standard floor.
- **Not rendered yet (the Mac on battery since 16:35; our Unity runs on AC only).** On AC: `zsh W/structures_assets/stairwell/tools/
  run_stair_fix_chain.sh` (nosing hero on the landed shell; the follow-up shell and the clone-only variant harness into the clone; the
  film of the change at the game's stick and at S4's, and the turn film, for the new placement and the landed one side by side; stills
  of both; the flash count on the stair part). It is offered for landing only if the stair part counts 3 or fewer.

### 6.11 Package (stage STAIR package, 2026-11-10 17:06-17:1x; the Mac on battery, no Unity)

- **In main, inert:** 关卡设计's map half (15:55, their `IFrontRoomsStairShell.cs` 4efde379 with `FarCeiling` and
  `IFrontRoomsStairShellVisible`) and our `stairwell_walkable` (15:56, 59 paths). `bash W/apply/stairwell_walkable.sh` on main 17:06:
  ALREADY APPLIED (every path holds the payload; the step-5 guards find their file). `walkableStair` stays off until the combined test.
- **Their side since the render:** S4 landed 17:00 (`stair_ground_stick`: `StairStickSpeed = -6f` while grounded with the feet in
  X1-X2 or the start shell, -1 elsewhere; `FrontRooms3DGame.WalkableStair.cs` b33bdd19, `FrontRooms3DGame.cs` 05ce8bb5 line 2494):
  the harness's -6 row (PASS, 0.05 s airborne at Walk and Run) is now the game's stair row. S2 is in (`SetLampLevel` where C22 drives
  its vestibule's level) and the game sets `FarCeiling` from N-prime's zone at every change.
- **The follow-up (6.10) is held:** `W/apply/stairwell_walkable_light_farwall.sh` (shell f69ba1cd -> 5eed32cd): dry run on main 17:06
  OK; fr gate re-run 17:08 on main after S4 (main_fp 9fbdd81bb246): FR GATE OK, macOS + iOS 0 errors, 1 override. It is offered for
  landing only after the AC re-measure counts its stair part at 3 or fewer.
- **Flash, recounted on the render's frames (no new render):** the stair part of the film of the change is over 3 in two windows, not
  one: 0.0-0.7 s (stepping from the EXIT onto the exit landing as the camera yaws toward flight 1) and 1.5-2.2 s (the end of flight 1
  into the turn); 3.5 each at S4's stick, 4.0 at -1. Both come from the same contrast, the dark end wall round the face-on turn wrap
  against the lit side walls, which the follow-up takes away (the lamp on the +x wall, its spots washing the end wall). The harness
  camera turns at up to 150 deg/s (the LW0 check films 76).
- **Static EXIT leaf:** no vision lite. On seed 20261001 TODAY's sealed leaf shows the dead wired lite (the map's EXIT has one), the
  NEW frames the plain static leaf; if a walkable floor's EXIT carries a lite, the static leaf takes the same dead lite
  (stairwell_a's `ExitLiteHole` / `HangExitLite`) in a later follow-up (read in the combined test).
- **Sheets (staged in `images/`, each with `_fit.txt`, ALL LABELS FIT, looked at whole):** `stair_walk_then_now.jpg` PASS (TODAY |
  NEW at the 10-22 cameras on a real map, phone row, COST, CAMERAS); `stair_walk_heroes.jpg` PARTIAL (the 0.35 m nosing re-render
  waits for AC; its tile is the map NEW detail at 1 m, cropped x2); `stair_walk_gate.jpg` FLAG, rebuilt 17:11 (walk with S4 landed,
  the two flash windows, a NEXT tile: what is held and what runs on AC); `stair_walk_kits.jpg` (Blender, 6.4).
- **Combined-test rows (6.8 stays; changed and new rows, flag on in a test profile):**

| # | Where | Do | Expect |
|---|---|---|---|
| ST3 (with S4) | flight 1 + 2 | walk down at Walk, then Run (Shift = slow walk) | no hop at either flight's top (S4's -6 m/s stick), no stick, no fall through; the camera follows the nosings |
| ST10 | flight 1, turn landing | walk down, turn on the landing | no flashing (photosensitive limit 3 in any second): the follow-up's turn light, filmed and counted on AC before it lands |
| ST11 | far landing | let the far door open onto the real N-prime | film it and count (the harness's plain cell counted 4.0); the new floor's lamps round N-prime as the map half holds them |
| ST12 | a floor whose N-prime is not a Standard zone (SeedForStair fallback) | look back at the far door from N-prime | with the follow-up landed: the papered thickening reaches that zone's ceiling, no gap over it |
| ST13 | exit landing, looking back | read the static EXIT leaf | if the map's EXIT has a vision lite, note it (the static leaf has none yet) |
| ST14 | desktop, iPhone, iPad | stand at the exit landing, the turn and the far landing | draws and frame time read in play (the render was an edit-mode batch); desktop 6 soft-shadowed spots, phones 3 plain |

- **To land (docs, the parent; copy from `W/structures_assets/stairwell/to_main/`):** this file (main's 5beb3d10 = lines 1-124 of
  the staged copy) and 8 new image files; sha1s in `W/structures_assets/stairwell/docs_manifest.txt`.
- **Resume on AC (the parent's Unity queue):** `cd W/structures_assets/stairwell && zsh tools/run_stair_fix_chain.sh` (nosing hero on
  the landed shell; the follow-up shell and the clone-only variant harness into the clone; films of the change at the game's stick and
  at S4's, and the turn film, for four placements; stills; the stair-part flash count in `walk3/flash_summary.tsv`); then the heroes
  sheet (`STAIR_NOSING_FIXED=1 STAIR_HEROES_STATUS=PASS`), the gate sheet with walk3's numbers, the follow-up's land decision.

### 6.12 Re-measure on AC (stage STAIR REMEASURE, 2026-11-10 17:51-18:2x)

Clone `W/proj_unlock` synced from main at 17:59 by a targeted copy (no iCloud placeholder read): our follow-up `FrontRoomsStairShell.cs`
5eed32cd (landed 17:17, 6.10), 关卡设计's S4 (`FrontRooms3DGame.WalkableStair.cs` b33bdd19, `FrontRooms3DGame.cs` 05ce8bb5, 17:00) and
the day's LW3 / key I files; all 456 main .cs equal in the clone. Unity 6000.6.3f1 edit-mode batch, silent, private product name,
0 error CS, four jobs 18:04-18:18 (the Mac went on battery at 18:18:15; a fifth job, below, waits for AC). Clone-only harness changes
(`W/structures_assets/stairwell/harness/`, Roslyn on the clone's whole tree 0 errors): the walk uses the game's body (1.75 m) and the
game's own stick rule `s4` (StairGroundStick: `StairStickSpeed`, read from `FrontRooms3DGame` by reflection = -6.0, while the feet are in
X1-X2 or the start area, -1 elsewhere); the far door's hold is held 3 s and watched as the sound layer watches a "Door hinge"
(`FrontRoomsDoorSound`: Quaternion.Angle from shut, MoveStart read by reflection); the map harness asks the real map for a prompt on
the far door; an option holds N-prime's lamp dark until the far door opens and brings it up over 0.6 s (the game's StartLampsNorth);
two more nosing cameras. Renders `renders/unity/walk4` (walk harness) and `renders/unity/remeasure` (map harness).

| Check | Result |
|---|---|
| Film of the change, stair part (0-3.47 s), flash_check area rule, worst second, limit 3 | **3.0 PASS** at the game's S4 stick (3.5 before the follow-up); 2.5 at the old -1 stick (4.0 before). The same frames as the 16:32 film: the camera path equals it to 0.000 m at all 321 frames. Window A (the EXIT onto the exit landing): the face-on lens is gone, 7 -> 1 events; window B (the end of flight 1 into the turn): the far wall is washed, 7 -> 3. The worst second is now 2.30-3.27 s, turning onto flight 2 (the riser and the upper +x corner): the same 6 events as before the follow-up, so not the moved light |
| Film of the change, whole (to 5.35 s) | 4.0 with N-prime lit from the start, 4.5 with N-prime's lamp held dark and brought up over 0.6 s from the open (the game's sequence). Its window, 3.27-4.13 s, is the approach to the far door (the closer, the REENTRY plate, the rescue-area sign and the STAIR A B1 placard crossing the upper middle while the eye turns toward the door) and the dark leaf swinging across the lit opening; the same events as 16:32. Not ours to move in the harness: filmed on the real N-prime in the combined test (ST11) |
| Walk 3.2 / 5.5 m/s down, 3.2 / 5.5 up, the game's S4 | PASS: airborne 0.05 s at most, no stuck, no progress drop, on the far landing at the end. Reference, -1 everywhere: down FAIL 0.22 / 0.50 s (as at 16:32) |
| Guards (4 pushes, 1.5 s at 3.2 m/s) | PASS: exit landing edge 0.10 m, flights stop at x +-0.56, turn landing 0.05 m, drops <= 0.04 m |
| Far door: silent hold | PASS: held 3.0 s, FarDoorMoved 0 and FarDoorLatched 0 (the game's StairFarDoorMoved / Latched come only from these); the hinge reads 0.000 deg to FrontRoomsDoorSound (the 0.088 deg give moves at 0.87 deg/s at most, MoveStart 6 deg/s): no swing loop, no unlatch one-shot. Open 95 deg, latch, one beat each as C22's north door (open Moved 1, shut Moved 1 + Latched 1; the hinge is named "Door hinge stairwell far (walkable)", so the sound layer's door sound takes it); the leaf waits for a body (36 deg) and goes on |
| Far door: no USE prompt | PASS on the real map (seed 20261001, X1 (3, -4)): 28 shell colliders, map Describe words 0, Kind none; 9 aim rays from the far landing at the leaf and lever within the game's Reach 2.4 m hit the shell, 0 words, FrontRoomsInspect hits 0; no FrontRoomsInspectable, AudioSource or FMOD emitter on the shell. Control: the map's own EXIT door, aimed from N, says "E . OPEN DOOR" |
| NEW off = today | 0 px over 1/255 at all 8 cameras on desktop; reduced 0 px in the vestibule, the maze view of the EXIT 2 px at 2/255 (as at 16:27) |
| Change (+3.50 m, the camera moved with the shell) | 1,815-6,347 px of 518,400 over 2/255 (0.35-1.2 %, edges) |
| Turn on the turn landing, 76 deg/s | 2.5 (2.0 before): the pickets against the wall the moved wrap now lights; under 3 |
| SetLampLevel 1-0-1 film | 0.0 |

- **The follow-up holds:** its land condition (the stair part 3 or under) is met on main's code; no further package. 关卡设计's S4 is
  the game's row now.
- **Look (my own, every frame):** the exit landing and flight 1 show the turn wrap edge-on on the +x wall over the turn landing, washing
  the far wall's top (look A's lamp on the wall you walk toward is now its light on that wall, not its lens); the +x wall right under the
  wrap is darker than before (its own light reaches it at a grazing angle): the 0.45 m rail end shows its upper wall dark. The 0.35 m
  nosing re-render with the 16:35 camera shows the tread tops from above and behind, the lip a thin line; two front views added: the
  centre line (0.39 m) and the +x walk line (0.35 m), where the kit wears the lip's face through to bare steel. The fill reads with its
  trowel swirl; the lip, riser and stringers read dark (the stair's dark brown-grey enamel, #47443E, at the game's exposure), and the
  worn streaks read darker than the enamel, not brighter: bare steel (metallic 0.85) shows its probe, and this edit-mode batch probably
  never renders the shell's box probes (the shell renders them once at runtime from its Tick). A harness job that renders each probe
  with a cube camera (`STAIRWALK_PROBES=cube`, gated) waits for AC; until then the worn lips are read in play (ST15).
- **Sheets re-made (`images/`, ALL LABELS FIT, looked at whole):** `stair_walk_gate.jpg` FLAG (the film at its events, windows A and B
  before and after the move at the same frames, the turn, NEW off, walk / flash / next panels; FLAG = the far-door part over 3 in the
  harness, for the combined test), `stair_walk_heroes.jpg` FLAG (the nosing above and on the walk line, the hardware heroes, the 3 m
  views with the moved wrap, phone frames; FLAG = the worn lips read dark here), `stair_walk_then_now.jpg` PASS (its NEW column
  re-rendered: the 16:32 frames showed the wrap on the far wall).
- **Combined-test rows (changes and new; the rest of 6.8 / 6.11 stays):**

| # | Where | Do | Expect |
|---|---|---|---|
| ST10 (measured) | flight 1, turn landing | walk down, turn on the landing | measured on AC in the harness: stair part 3.0, turn 2.5 (limit 3); in play nothing flashes |
| ST11 (sharpened) | far landing | arrive, let the far door open onto the real N-prime, walk out | count it (harness 4.0 lit / 4.5 with the game's lamp rise: the approach to the signed door + the leaf over the opening) |
| ST15 | flights, close | look at the nosings on the walk lines (0.3 m in from each side) | the worn streaks read as bright bare steel against the dark enamel, not as dark marks |
| ST16 | turn landing, +x wall | stand under the turn wrap, look at the wall rail | the wall under the wrap is not a dark band (its scallop, the bounce from the landing) |

- **Resume (Unity, on AC only):** `cd W/structures_assets/stairwell && LABEL=STAIR-remeasure METHOD=FrontRoomsStairWalkHarness.RunBatch
  TAG=stair_rm_e STAIRWALK_OUT=$PWD/renders/unity/walk4 STAIRWALK_DO=stills,reduced STAIRWALK_ONLY=hero_nosing_walk,hero_nosing_front,hero_nosing,hero_rail_end,hero_valve,hero_guard_top,exit_landing,turn_landing
  STAIRWALK_PROBES=cube STAIRWALK_TURNLIGHTS=built STAIRWALK_NONOISE=1 zsh tools/run_stair_unity.sh` (stills prefixed `probecube_`;
  if the worn lips read bright with the probes rendered, the heroes sheet takes them and loses its FLAG).
