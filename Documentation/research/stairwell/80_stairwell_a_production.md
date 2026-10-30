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
