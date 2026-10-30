# 51: Door break prototype DB2. What was built, measured and stood in (2026-10-30)

Owner: 游戏视觉 (game visuals), account A, session local_5656cffd, workflow stage "build the prototype". Design: `50_rethink.md` (sections 3 and 4). **Nothing landed in main.** Everything is in the clone `W/proj_doorbreak` (W = `/Users/redwang/FrontRoomsVisualWork`), made with `mkclone.sh` from main `3ea3d7bb` plus `install_tools.py` (clone-only silencer, test bundle id). The 6.6 Library was copied from `W/proj_titletv`.

Status: **READY for the video stage.** One test round was run: Roslyn (mac and iOS), then one dry play of the film harness with 14 stills and stats. One fix was made after the dry play (shadow parity at the swap, and the C4 camera spot), and the dry play was re-run once.

## 1. What was built (prototype scope 50 §4.2)

| # | File | What it does |
|---|---|---|
| 1 | `Tools/Blender/frontrooms_kit/assets/interact_door_break_common.py` | The shared builder, in pure Python, in door-root metres. It holds the cassette window, the seeded split motif, the prisms across the 44 mm, the material split (veneer caps, `Door_CoreWood` core walls, `Door_CoreStile` stile and lock-block walls, `Prop_Chipboard` under the petal), real-geometry splinters, crumbs, the petal, the bore and lock chassis, leak cards, the debris and the Blender export. It reads the D1.6 section from `interact_door_da_common` and never changes it |
| 2 | `Tools/Blender/frontrooms_kit/assets/interact_door_leaf_veneer_da_break.py` | Builds `Assets/Resources/Props/Models/Kit_DoorLeafBreak_Veneer_DA.fbx` and its `.json` sidecar: 44 objects, one FBX, no `_LOD` names. The sidecar carries the budgets, anchors, per-stage strip offsets, the debris rests and the motif |
| 3 | `Tools/Blender/frontrooms_kit/door_break_bake.py` | Builds `Kit_DoorLeafBreak_Veneer_DA_throw0.json`: 30 Hz keys for 31 pieces, the settle time and a contact list (75 contacts for sound ask S3) |
| 4 | `Tools/door_break/make_core_textures.py` | Writes `Door_CoreWood_{A,N,S}.png` (the cross-section strip, 1024 × 512) and `Door_CoreStile_{A,N,S}.png` (the long-fibre split face, 1024²) |
| 5 | `Assets/Scripts/Rendering/Doors/FrontRoomsDoorBreakVisual.cs` | The render-only runtime: the stage clock, a "Break pose" node, the discrete swaps, rock and hit-stop, rose tip and lever droop, the latch riding on the strip, baked debris playback, leak cards, the breach light, and poses (a) and (b) |
| 6 | `Assets/Scripts/Rendering/Doors/FrontRoomsDoorBreakVfx.cs` | Mesh-particle chips and crumbs plus crossed dust cards. It follows the glass VFX pattern: no Particle System module, 1/120 s substeps, keyed random, analytic floor and wall collisions |
| 7 | `Assets/Editor/DoorBreak/FrontRoomsDoorBreakFilm.cs` | The Play-mode film harness. ASCII only, no tuples, `FrontRoomsObjectId.Of`. It also writes the clone-only `Door_CoreWood.mat` and `Door_CoreStile.mat` (FrontRoomsSurface, mesh UVs) before Play |
| 8 | `W/door_break/DB2/run_film.sh` | Runs Blender, the bake and the textures (unless `--skip-assets`), checks the `unity_jobs.sh` slot, then runs Unity on the clone |
| 9 | `W/door_break/DB2/compose_film.py` | `sheet <dir>` builds the contact sheet. `film <frames> <out>` builds the Shot-format title and end cards, the caption strip with a blow tick on every frame, the segment plan and the contact sheet |
| 10 | `W/door_break/DB2/encode.sh` | ffmpeg libx264, CRF 20, preset slow, yuv420p, faststart, 60 fps, silent. Then an ffprobe G6 check |

The clone also gains `Assets/Resources/Surfaces/Door_CoreWood.mat` and `Door_CoreStile.mat`, plus the textures and the FBX and JSON above.

## 2. Measured (generator, dry play 2)

**Triangles per part** (generator; the budgets are 50 §3.7):

| Part | LOD0 | Budget | LOD1 | Budget |
|---|---|---|---|---|
| body (intact leaf: 2,736 / 1,068) | 2,160 | 2,900 | 1,072 | 1,100 |
| cas_M | 504 | 1,000 | 280 | 300 |
| cas_D1 | 728 | 2,200 | 412 | 600 |
| cas_D2 | 1,960 | 4,500 | 900 | 1,200 |
| cas_TORN | 1,516 | 4,000 | 780 | 1,000 |
| debris, 31 pieces (strip 396) | 1,180 | 3,000 | n/a | n/a |
| leak cards (M / D1 / D2) | 372 / 500 / 772 | n/a | the same | n/a |

All parts are inside budget. The body and window seam lines at Y 0.50 and 1.70 match to 3e-10 m.

**Drawn per stage** (run time, LOD0 / LOD1 triangles, renderer × submesh draws):

| Stage | LOD0 tris | LOD1 tris | Draws |
|---|---|---|---|
| M | 3,036 | 1,724 | 7 |
| D1 | 3,388 | 1,984 | 7 |
| D2 | 4,892 | 2,744 | 8 |
| FINAL and BROKEN | 4,856 | 3,032 | 40 |

- A damaged leaf stays under the 6.9k LOD0 budget.
- Draws run over the design's "+2 to 3":
  - a cassette uses up to 6 slots: veneer, core, stile, chipboard, brass, chrome;
  - each of the 31 debris pieces is its own draw until the floor merge, which is production, not prototype.

**Gates (dry play 2):**

| Gate | Result |
|---|---|
| G1 Roslyn | 0 errors on mac and iOS defines, before and after the fix |
| G2 generator | Every part inside budget; seam 3e-10 m |
| G3 timing | Every stage change lands on its blow frame: E@556, M@586, D1@616, D2@646, FINAL@670, BROKEN@676. Nothing changes between blows. Hit-stop held 2 / 2 / 3 frames at D1 / D2 / FINAL |
| G4 the 3 m read | Changed pixels (luma Δ > 6) in the C1 lock box, 127 × 228 px: M→D1 2,915; D1→D2 4,402; D2→FINAL 5,585; FINAL→BROKEN 27,342. All ≥ 400. E→M is logged only, see §4 |
| G5 throw | The ballistic fallback settles 1.07 s after FINAL. 0 keys below the floor, 0 inside the wall slab. Lowest debris Y 0.000. Median rest 0.54 m from the latch jamb; the farthest splinter lands at 2.1 m |
| G6 | Not run in this stage. It belongs to the video stage (`encode.sh`). The planned cut is 3,042 frames, 50.7 s |

**Scene:**
- Seed 516574485 through the title handoff; quality High; Metal; Glass RT off.
- Door (95,-69)-(95,-68), 33.4 m from the start. Zones Low / Standard, both Level 0. More than 7 m clear on both sides.
- Lamps: Relay side +X 0.988, player side 0.962. The burst goes to −X.
- C3 at 3.5 m and C5 at 6.0 m both fit. C4 is clamped to 1.0 m at 65° (see §4).
- The Relay is shown in C3: a copy of the game's rig, posed in the BreakDoor stance.

## 3. What is a stand-in

- **Break driver.** The real `FrontRoomsMapHunter` is held dormant. The harness repeats `TickBreak`'s calls on the same frames: `SwingAwayFrom`, `MarkBeingBroken`, and per blow `SwingAwayFrom`, `JoltForBlow(i, n)`, `Blow(i, n, from)` and the rig's `DoorBlow(i, n)`. At T it calls the **real** `BreakDoor`, so `DoorBrokenFrom`, `DetachStrike` and the map's throw and bounce all run. The visual's `Broken` is subscribed by the harness. In production it gets the door from map ask M1.
- **C1 camera shake.** The harness applies the `FrontRoomsCameraRig.AddTrauma` formula on the moved game camera (Perlin at 18 Hz, linear decay, the DoorBreak blow and break values). The game rig itself is not used.
- **Throw.** The Blender rigid-body bake failed its own gates: keys below the floor, no settle. The script therefore wrote the **scripted ballistics fallback** (method `ballistic`), as 50 §4.3 item 3 allows. The leaf's notch lets the strip through, and a leaf knock is capped at 2.5 m/s.
- **Relay in C3.** It is a copy of the game's Relay rig, ticked in its BreakDoor stance, because the real one is inactive while dormant.
- **Poses.** Pose (a) and pose (b) are render-only. The map's collider keeps its butt-hinge 3° tilt (ask M3).
- **Leak card.** Unlit HDR in the far lamp's colour × level × the stage curve (0 / 0 / 0.15 / 0.40 / 1.0), with a 0.12 s pulse on each blow.
- **Breach light.** One shadowed spot on the push side: low from D1, a flare at the break, then a 1 s fade. Its intensity scale is a guess, uncalibrated against the game's lamps.
- **Dust.** URP `Particles/Simple Lit` on crossed cards with a procedural texture.
- **Geometry simplifications:**
  - Cassette prisms have a square latch end. The intact 1.5 mm arris and the 0.3 mm seams are not rebuilt inside the window. That is a sub-pixel tick at Y 0.50 and 1.70 up close.
  - There is no push-face dish geometry; production does it with the `_FR_DAMAGE` mask.
  - The body has no pivot plates.
  - Interface faces are kept closed rather than deleted.
- **Texture size.** The `Door_CoreWood` strip is 1024 × 512, not the design's 2048 × 256. It gives 1 px/mm along the grain instead of 2 mm per texel.

## 4. Known issues for the video and for production

1. **Shading shift at the first swap (E→M).**
   - When the intact leaf is replaced by body + cas_M, the lock box changes by 16k pixels at luma Δ > 6, though the mean luma moves only +0.7.
   - The UVs are identical: 1,040 of 1,040 shared vertices match. Shadow parity, a fix-round change, did not change the number.
   - The cause is the intact kit's weighted, smoothed face normals against the body's flat ones. The veneer stripes amplify the small lighting gradient.
   - It plays under the blow, the rock, the dust and the shake. Production should build the body through the kit's own `finalize` (WEIGHTED_NORMAL) so the faces keep the intact normals.
2. **C4 at 1.0 m.** The burst-side room is narrow (Low / Standard). The 2.0 m spot was past the cell corner, behind a wall, so the harness now clamps C4 to the farthest clear spot. Here that is 1.0 m.
3. **Debris draws.** There are 31 separate draws after FINAL until the floor merge (production, P1).
4. **The petal reads dark at 3 m.** It is a 110 × 230 mm flap turned away from the lamp. Its lighter crossband and core show only up close (C2).
5. **Expected log noise** (not ours):
   - main's `FrontRoomsMapWorld.PlaceProps` warning (line 3966);
   - the Unity QuickSearch `ArgumentOutOfRangeException` at start.

## 5. How the video stage runs it (token-saving, 50 §4.6)

```
W/door_break/DB2/run_film.sh full --skip-assets        # about 2,150 renders, 1080p JPG q90, 5 to 8 min after startup
W/venv/bin/python W/door_break/DB2/compose_film.py film W/door_break/DB2/out/full W/door_break/DB2/out
W/door_break/DB2/encode.sh W/door_break/DB2/out        # out/door_break_proto_v1.mp4 + the G6 line
```

- Harness entry: `FrontRoomsDoorBreakFilm.RunBatch`.
- Full Unity line: `Unity -batchmode -projectPath W/proj_doorbreak -buildTarget OSXUniversal -executeMethod FrontRoomsDoorBreakFilm.RunBatch -dbMode full -frGlassRT off -logFile W/door_break/DB2/logs/unity_full.log`. There is no `-quit`, no `-audioCapture` and no `-nographics`.
- Frames go to `W/proj_doorbreak/Verification/door_break_film/<mode>/`, which is mirrored to `W/door_break/DB2/out/<mode>/`.
- The dry dump is `W/door_break/DB2/out/dry/`: 14 stills, `contact_sheet.jpg`, `stats.json`, `frames.tsv` and `film_log.txt`.

Segments in the full run:

| Segment | Take and views |
|---|---|
| Take A | T1 from −0.5 to 4.5 s; C1 to C5 every frame, plus 7 C1h holds |
| Orbit | C6, 6 s |
| Pose (b) | Stills of C1h and C4 |
| Take S | T1 slowed, C2 at 1/240 from 2.35 to 2.75 s |
| Take F | T5 (3 blows), C1 |

Look only at `out/contact_sheet.jpg` and the numbers.

## 6. Production still needs (after Red picks; 50 §5)

1. **The body.** It keeps the intact normals (§4.1), the latch-end arris inside the cassettes, and the pivot plates. Interface faces are deleted.
2. **The `_FR_DAMAGE` onset mask** and its shader branch: the push-face dish and bruise, hairlines, and the 8 shared stage materials.
3. **The debris world:**
   - LOD2 cassettes;
   - 3 motifs and 4 throws per family;
   - mirror bursts, which are wired here through the root's −1 X scale;
   - the floor merge, the record and restore on `DressDoor`, and the env-hash check after a shift.
4. **The throw bake.** Either a working Blender rigid-body bake (launch through animated frames, a thicker floor and smaller substeps), or a decision to keep the ballistic bake.
5. **The phone tier:** LOD1-only cassettes, capped VFX, no breach light. The other leaves and frames: Oak, Ward, steel, and the frame strike-zone inserts.
6. **Calibration.** The breach light against the game's lamps, and the leak-card HDR.
7. **Contracts.** The map contract lines M1 to M7 and the sound asks S1 to S5, written as handoff text with no messages sent. The contact list in `throw0.json` is ready for S3.
