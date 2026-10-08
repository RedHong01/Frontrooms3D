# 20 — Run! direction C · HOUSE LIGHTS: build log, costs, needs, open issues

Date: 2026-10-08 (final round r9, renders 00:1x). Companion to `11_run_dir_house_lights.md` (every value used, the measurements, pass / fail).
Clone: `/Users/redwang/FrontRoomsVisualWork/proj_run_house_lights` (clone-only; nothing was promoted; no apply script, because this is a pre-render for Red's pick).
Red's project was not opened in Unity. Writes there: this folder only (`research/room_visuals/`: the two reports, `images/run_dir_house_lights_*`, `images/run_house_lights_*`, `harness/`) and rows VL189–VL192 in `Documentation/VERIFICATION_LOG.md` (slides placed in the Figma FRONTROOMS · VISUAL VERIFICATION LOG, frames 2852:6151, 2852:6173, 2852:6185, 2852:6194; the section grew to 49 rows).

---

## 1. What was built

A clone-only Unity look-dev scene, built in code and rendered in batch (Unity 6000.3.10f1, `-buildTarget OSXUniversal`, URP 17.3 Forward+, 4× MSAA, 1920 × 1080, each shot rendered twice):
- a Level 0 anteroom (3 × 2 cells), the 1 × 9 cell (27 m) Run corridor and a one-cell Level 0 stub behind the goal door, rooted at world (4992, 0, 4992);
- C's finishes: a glazed structural-tile wainscot (8 × 16 in units, bullnose cap, cove base), eggshell plaster above, a flat gypsum ceiling, waxed VCT with two charcoal bands;
- **the signature objects as Blender kit models (LOD0)**: the 8 ft two-lamp bare strip and its F96T12 tubes, the magnetic door holder and its armature plate, the red flasher, the RN-F pair leaf with its lite, the pair frame, the regular-arm closer body, and the RN-X goal leaf and frame;
- the props as Blender kit models: the pull station, the floor burnisher, the folded wheelchair, the goal's EXIT sign;
- a deterministic state rig, `FrontRoomsRunLookdevRig.Evaluate(t)`: economy switching, the strike front, the dead tube, the flashers, the holder release, the closer curve and a solved closer arm per leaf, the corridor ambient and a white-room bounce;
- three box-projected reflection probes, one per 9 m compartment, re-baked for every rig state;
- the local post volume `FrontRoomsPost_Run_C` (priority 2);
- the measurement and export pipeline: ID-mask renders, greyscale and protan copies, a contact sheet, the photosafety table and a cost dump.

Run (final): `W/hl_work/run.sh r9 all quick`, which calls
`/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath W/proj_run_house_lights -buildTarget OSXUniversal -executeMethod FrontRoomsRunLookdev.RunBatch -frGlassRT off -runQuick -logFile <log>`.
It renders 23 shots plus 38 ID masks in about 10 minutes at load 300–450. Single shots: `-runShots s1b,c4`. Environment overrides (each one logged): `RUNC_STRIP`, `RUNC_ARMED`, `RUNC_PROXY=0` (the game's Relay rig), `RUNC_BRIEFGLASS=1` (the brief's glass stand-in), `RUNC_SWEEP=a,b` (strip levels), `RUNC_SIGNSWEEP=exit:dead,…`, `RUNC_EXITSMOOTH`, `RUNC_EXITEM`, `RUNC_DEADEM`, `RUNC_PROBES=0`, `RUNC_NOSSAO=1`.
Analysis: `W/venv/bin/python harness/house_lights/tools/post.py.txt <frames> <images dir> <json> --export` (copy it to a `.py` first).

### 1.1 Rounds

| Round | When | What changed | Result |
|---|---|---|---|
| v0 | 2026-10-03 19:31–20:14 | First build in the old scratchpad clone: textures, Blender kit, harness | Session limit before the first full render |
| v0.1 | 10-03 23:45 | URP light layers (corridor / anteroom / stub), quick mode | — |
| v0.2 | 10-04 15:12 | Orange burnisher, flasher light scaled with the strips, P1 flasher-off twin, more masks | Weekly limit |
| — | 10-05 ~01:00 | The reboot wiped the clone | Recovered on 10-07 16:3x from the transcripts (`W/hl_work/rec`); clone rebuilt from `W/proj_audit` (main of 10-07 16:34); Blender modules and the texture generator re-run |
| r1 | 10-07 17:4x | First full render. Strip spots 165° / 120°, tilted 30° toward the walls, level 1.3 | Hard V-shaped cone edges on the doors and cross walls |
| r2 | 18:5x | Spots straight down, 179° / 140°; door laminate satin (smoothness .75, was 1.0: the leaves mirrored the ceiling as light wedges); level 1.0 | Wedges gone |
| r3 | 19:0x | Spots at 2.76 m; probes 512 px; pushed-pair and in-pair C4 variants | The brief's glass stand-in veils the Relay: **1.3 : 1** through the lites. Dark crown band along the ceiling margins |
| r4a | 19:4x | Inner cone 170°; holder grey raised to ~.30 linear (it read black); level sweep .8 / .9 / 1.0 | — |
| r5b | 20:0x | Spots inside the channel line (2.86 m), ceiling bounce lobe .3 → .6, no grime band on the plaster; sweep .75 / .85 | Crown band gone. Session limit |
| r6 | 10-07 23:3x | Level **0.8**; the lites use a copy of the game's own window glass; the brief's Relay proxy | S1b **.330**; Relay **4.75 : 1** through a lite (off axis); S1a .155 (1.7 × Level 0) |
| r7 | 23:4x | EXIT face smoothness .9 → .5 (a strip highlight ate the T: "EXI\|"); new shot X1b (dead tube close) | EXIT reads |
| r8 | 10-08 00:0x | Red-held EXIT glow; cathode colour | Letters still salmon |
| E1 | 00:0x | Armed level .7 (test); the game's Relay rig (reference) | S1a **.116** = the brief's 1.3 × target |
| E2 | 00:1x | Sweep of the EXIT glow (× 3.2 / 1.6 / 0.8) and the cathode glow (× 1.3 / .7 / .35) | The EXIT glow does not change the letters (the face's reflection does); cathode × .7 reads orange |
| E3 | 00:2x | The brief's values as checks: glass stand-in α .25, strip spots at 4.5 | Relay through the stand-in **3.2 : 1**; S1b at 4.5: median **.645**, p95/p5 2.3 (washed out) |
| **r9 (final)** | 00:1x | Armed level **.7**; EXIT face eggshell .25, glow × 1.6; cathode × .7 | All finals in this folder: S1a .116, S1b .327, Relay 5.2 : 1 off axis |

Every round's frames were read by eye before the next change (sheets in `W/hl_work/peek/`).

---

## 2. Change list (clone-only)

| Path in the clone | Change |
|---|---|
| `Assets/Editor/Rendering/FrontRoomsRunLookdev.cs` | New. The harness (copy: `harness/FrontRoomsRunLookdev_house_lights.cs.txt`) |
| `Assets/Scripts/Rendering/Lookdev/FrontRoomsRunLookdevRig.cs` | New. The state rig (copy: `harness/house_lights/FrontRoomsRunLookdevRig_house_lights.cs.txt`) |
| `Assets/Resources/Rendering/FrontRoomsPost_Run_C.asset` | New. The Run C post profile (copy in `harness/house_lights/`) |
| `Assets/Resources/Rendering/RunC_Probes/*.exr` | New: 3 box probes × one bake per rig state (generated; not needed in production as files) |
| `Tools/Blender/frontrooms_kit/assets/run_*.py` | New, 16 modules: `run_strip_fixture`, `run_t12_tube`, `run_mag_holder`, `run_mag_armature`, `run_flasher_lamp`, `run_door_leaf_pair`, `run_door_leaf_x`, `run_door_frame_pair`, `run_door_frame_x`, `run_door_frame_common`, `run_closer`, `run_exit_sign`, `run_pull_station`, `run_burnisher`, `run_wheelchair_folded`, `run_c_common` (copies in `harness/house_lights/blender/`) |
| `Assets/Resources/Props/Models/Kit_{StripFixture_8ft, T12Tube_8ft, MagHolder, MagArmature, FlasherLamp, DoorLeaf_RunPair, DoorLeaf_RunX, DoorFrame_RunPair, DoorFrame_RunX, DoorCloser_Run, ExitSign_RunC, PullStation, FloorBurnisher, WheelchairFolded}.fbx/.json/.meta` | New, built by `Tools/Blender/frontrooms_kit/build_asset.py` and imported by the clone's `FrontRoomsKitImporter` |
| `Tools/lookdev/gen_run_c.py` | New. The texture generator (copy: `harness/house_lights/tools/gen_run_c.py.txt`) |
| `Assets/Resources/Surfaces/Textures/Run_{GlazedTile, Plaster, CeilingGyp, DoorLaminate}_{A,N,S}.png`, `Run_C_{ExitFace_A, ExitFace_E, FirePlate_A, Plaque_A, PullFace_A, WireGrid_A}.png` | New (18 files) |
| `Assets/Resources/Surfaces/Run_C_*.mat`, `Run_{DoorLaminate, Enamel, ExitFace, FlasherDome, MagGrey, Orange, PullFace, TubeEnd, TubeEndDead, TubeGlass, TubeGlassOff}.mat` | New (21 materials, written by the harness) |
| `Assets/Resources/Rendering/FrontRoomsSurface.shader` | Clone-only: box-projected, blended reflection probes (the `_REFLECTION_PROBE_*` multi-compiles, a mini G7), and a `_FR_SOFT_AO` keyword (SSAO at half strength on indirect light only). Diff: `harness/house_lights/FrontRoomsSurface.shader.clone_only.diff` |
| `Assets/Settings/FrontRooms_URP.asset` | Clone-only: reflection-probe blending and box projection on; Light Layers on. Diff in `harness/house_lights/` |
| `ProjectSettings/TagManager.asset` | Clone-only: rendering-layer names `RunCorridor` (bit 1) and `RunStub` (bit 2). Diff in `harness/house_lights/` |
| `Assets/Editor/Audit/*`, `Assets/Editor/Rendering/{FrontRoomsKitLookdevAccess, FrontRoomsLookdevCapture, FrontRoomsHunterLookdev, …}` | The recovered capture tools from `W/tools` (`W/TOOLS.md`) |
| `W/hl_work/{run.sh, post.py, review.py}` | Outside the clone: the run wrapper, the measurement and export script, the review sheets (copies in `harness/house_lights/tools/`) |

The clone's base is main as of 2026-10-07 16:34. Main has moved since. The only rendering change that touches this scene is the Q1b hard-edge wallpaper print (merged 10-07 19:2x): it would change the Level 0 anteroom's wallpaper print in S2, not the corridor.

---

## 3. Measured costs

| Item | Value |
|---|---|
| Corridor lights | Armed **10** (spots under the 5 lit strips); tripped **18** + 4 flasher points (0.8 m range, on only in the flasher's on half) = 22. The brief's estimate was 10 / 22 |
| With the anteroom and the stub | Armed 17; tripped 25, or 29 while the flashers are on |
| Shadowed lights | Corridor: every third strip spot (6 of 18; 3 of them are on when armed). Plus 2 anteroom lamps. Armed 5, tripped 8 |
| Draws per shot (estimate from the harness) | S1a 284 submeshes + 279 depth-normals + 628 shadow-caster draws (5 shadow views) ≈ 1,191; S1b ≈ 1,625 (8 shadow views); S2 ≈ 1,323; S3a ≈ 1,103; C3 ≈ 1,283; C4 ≈ 1,714 |
| Tris in view | S1a / S1b 107,510; S2 127,536; S3a 86,786; C3 62,548; C4 113,386 |
| Corridor content | 196 renderers, 343 submeshes, 129,020 tris (with the proxy Relay) |
| New meshes (LOD0 tris; no LOD1 yet) | Strip fixture 2,904; T12 tube 964 (× 18); mag holder 2,204 (× 6); armature 828 (× 6); flasher 2,974 (× 4); pair leaf 3,188 (× 6); pair frame 1,548 (× 3); closer body 1,024 (× 7); RN-X leaf 4,540; RN-X frame 1,656; EXIT sign 686; pull station 558; burnisher 6,036; folded wheelchair 7,468 |
| New textures | 18 files, 153 MB as uncompressed RGBA32 with mips (the four 2048² door-laminate maps are 67 MB of it; the glazed tile 1600² set 41 MB). The whole look-dev scene uses 46 textures, 246 MB at runtime (mostly the Level 0 maps). Production would compress them and could drop the laminate to 1024² |
| Reflection probes | 3 box probes, 512 px HDR. Production: one armed and one tripped set per Run room (6 cubes), swapped at the trip |
| Per-frame extras | One SH set per state on the corridor renderers (property blocks); per-tube emission blocks during the strike; 12 door transforms and 12 closer arms during the 4 s close |

---

## 4. What a real implementation needs, from whom

**Map chat (关卡设计).** The exact proposals are in `10_run_directions.md` §8:
- CR-1: the 1 × 9 straight (module `Run_AlarmCorridor_1x9`, `ModulePlacement.Corridor`).
- CR-2: `ModuleTrigger.Alarm`, armed 1.5 m in, plus `RoomReset`.
- CR-3: `ModuleFinish.Run`.
- CR-4: `ModuleLamp.Off` on all 9 cells; `ChunkBuilt` / `ChunkDropped`; `WarnStage` forwarded; the `LampFx` layer skips these cells.
- CR-5: placement rules.
- CR-6: the latching RN-X goal door (6a: a module edge, needs Red's OK; or 6b: a zone border).
- **CR-7 (C only): the held door pairs.** `ModuleEdge.DoorPairHeld` on the inner edges at z 9 and 18 and on the entrance. Release on `RoomTriggered`; the rig animates the close (90° → 10° over 3 s, then eased to 0° over 1 s); each leaf counts as `Ajar` while closing and `Closed (unlatched)` after; the player pushes through at a walk or a sprint without Use; the Relay pays +0.4 s per pair.
- New from this render:
  1. **Sight through the pair.** A Relay on the corridor axis is completely hidden by a shut pair's meeting stiles: the lites sit 0.30 m from the meeting edge, so from 12 m they show the corridor 1.2 m off axis. Off axis, the Relay reads 5.2 : 1 through a lite. The sight rule in CR-7 ("the leaves block, except through the lites if the glass track allows") should use the real lite rectangles, not the pair as a whole.
  2. **Light layers.** The look-dev keeps the strips off the anteroom and the map lamps out of the corridor with URP rendering layers (Light Layers on in the URP asset). Without them, unshadowed lights leak through the 0.16 m walls (direction A's D1). Production needs either light layers on Run cells or the map's lamp shadow policy.
  3. **The pull station** sits at z 1.45, not 1.0: the held z 0 leaf covers the right wall from z 0.08 to 1.20.

**Sound chat (声音设计).** All cue times come straight from the rig (see `11` §5, the photosafety table):
- CR-S1: a hard-floor footstep surface `Surface.Vinyl` (VCT).
- CR-S2: a hard, bright corridor reverb.
- §4.7 events: `Run.HolderRelease` × 3 at t = 0 (one per pair); `Run.CloserSweep` per leaf (0–4 s, no latch click); `Run.StripStrike` per economy-off strip at t = 0.1 + z / 8 s (0.66, 1.48, 2.09, 2.91 s); `Run.FlasherTick` at 1 Hz from 0.3 s, synced; `Run.DoorPushThrough` and `Relay.DoorPushThrough`.
- CR-S4: the rig's lights stay out of the fixture-hum voices unless the sound chat wants them.

**Interactables track.**
- RN-X: production needs the latching goal door. The clone-only `Kit_DoorLeaf_RunX` (laminate leaf, lite, kick plate, crossbar on the push face) and `Kit_DoorFrame_RunX` show the target. Main's `Kit_ExitDevice_Crossbar` should replace the module's own crossbar.
- RN-F pair: `Kit_DoorLeaf_RunPair` (no latch, push and kick plates, lite 0.30 m from the meeting edge), `Kit_DoorFrame_RunPair`, and the regular-arm closer. The arm here is a two-bar solver in the rig (`SolveCloser`); the kit could carry it as two parts with pivots.
- `Kit_MagHolder` + `Kit_MagArmature` (the hero object). Production needs the release as an event and LOD1s.

**Glass track.**
- `Glass_Wired` is still missing. This render used a copy of `Glass_Window` (FrontRooms/Glass: face-on alpha .11, pane F0 .08; dust .45, smear .5, prints .8) plus an alpha-tested 12.5 mm wire grid.
- **Keep the face-on alpha near .11.** Through-lite Relay contrast (C4p, same pose): 14.9 : 1 with no glass, **5.4 : 1** with the game's glass, **3.2 : 1** with the brief's α .25 stand-in (E3; fails 4 : 1).

**Exit-sign track** (`research/exit_sign/`). Production should use that track's EXIT family, not this quick clone-only `Kit_ExitSign_RunC`. Two findings carry over:
- a glossy face mirrors a bright corridor over its letters: at smoothness .9 a strip highlight ate the T; at .5 the letters still read salmon (sRGB 206 / 100 / 102);
- the glow level hardly changes the letters in a bright room (E2: × 3.2, 1.6 and 0.8 gave the same face).

**Visual chat (production rig `FrontRoomsRunRig`).**
- States: armed (economy switching), tripping (the strike front, the release, the flashers), tripped, reset (one change per strip back to economy; holders stay released). Reset and the stage-1 warning flicker are not rendered here.
- The strip kit with per-tube emission, the dead tube, and the strip's lights: 2 spots per strip, 179° / 170°, straight down from the channel line.
- **The white-room bounce.** The game has no GI. In a white corridor most of the ceiling's light is interreflection. The rig adds an SH lobe from below (0.6 × the strips' summed level × intensity) plus a flat term (0.06). Without it the ceiling margins read as a dark crown band (r3).
- The corridor ambient: `CustomProvided` SH through a property block works in URP 17.3 (no Light Probe Group fallback was needed). A `_FR_AmbientScale` term in the Surface shader is the alternative.
- **Box-projected reflections** for the waxed floor (Red's 10-03 decision): this needs the Surface-shader box-projection path (the clone-only diff above, a mini G7) and probe blending in the URP asset, or G14b RT later.
- **SSAO in white rooms.** The game's SSAO (1.6, radius 0.45) draws a grey band along every white ceiling joint. The clone-only `_FR_SOFT_AO` keyword applies it at half strength on indirect light only. Production needs a decision (a per-material AO strength, or a lower global SSAO).
- The WebGL tier (one light per strip, no shadows, flashers emissive only): not rendered.

**Relay rig (visual chat).** The renders use the brief's proxy. The game's procedural rig still shows its limbs floating apart in an empty scene (`run_dir_house_lights_v_gamerig.jpg`); it needs a check in the real scene before C is judged on the real silhouette.

**平面视觉.** The faces "EXIT", "FIRE DOORS — DO NOT OBSTRUCT", "C-LINK" and "FIRE / PULL DOWN" are set in TeX Gyre Heros Bold Condensed and Bold (`Assets/Fonts/Period1990/TeXGyre/`). By the house rule, type goes through 平面视觉 before anything ships.

---

## 5. Open issues

1. **C4 as posed cannot be measured.** With the Relay on the axis behind the shut z 18 pair, it is completely hidden by the meeting stiles: 0 px through the lites, 229 px through the 5 mm meeting gap. The off-axis variant passes (5.2 : 1). See `11` §4.
2. **The magnets are hard to see from the corridor.** A holder sits between the held leaf and the wall, so the leaf hides it from the axis. In S2 the z 0 holders are behind the jambs and the z 9 holders are 6 px tall. S3a / S3b (the brief's camera) show the held and swinging leaf but not the holder. The extra S3c / S3e (from beyond the pair) show it. If the magnets matter to the player, the holder could sit on the leaf's push side at the top (a ceiling-mounted or overhead holder), which is visible from the approach.
3. **The brief's S3b instant** (t = +1.0) falls in the flasher's off half (on 0.3–0.8 s, 1.3–1.8 s). S3b2 shows the same camera at t = +1.4 with the flasher on.
4. **Deviations** (`11` §3): strip spot level 0.8 instead of 4.5, cones 179° / 170° straight down from 2.86 m, armed level .7 instead of .9, the game's window glass in the lites, the pull station at z 1.45.
5. **Clone-only engine changes** (box projection, soft AO, light layers): production needs a decision on each (§4, visual chat).
6. **The game's directional fill** ("Soft ambient direction" 0.16, audit F5) is absent here, as the brief asks. C is the least sensitive of the three to it: the corridor is bright anyway.
7. **Not rendered:** the reset, the stage-1 warning flicker, the Relay pushing through a pair in motion, the WebGL tier.
8. **The burnisher's orange** (.88 / .45 / .10) is safety orange, not red, to keep C's palette rule (red only in the flashers and the pull station).

---

## 6. Images

- Shots: `images/run_dir_house_lights_{s1a, s1b, s2, s3a, s3b, c1, c2, c3, c4}.jpg` (the same pixels as `images/run_house_lights_<shot>.jpg`, the stage's own naming).
- Derived: `…_{s1a, s1b, s2, c4, c4b}_grey.jpg`, `…_{s1b, c4, c4b}_protan.jpg`, `…_sheet.jpg`.
- Extras: `…_{s3b2, s3c, s3e, s3d, c4b, c4p, c4p_ng, c4r, x1, x1b, x2, p1, p1off}.jpg`.
- Verification: `…_v_levels.jpg` (armed .9 vs .7; tripped at the brief's 4.5 vs 0.8), `…_v_glass.jpg` (the Relay through the brief's stand-in, the game glass and no glass), `…_v_exitface.jpg` (the EXIT face by round), `…_v_gamerig.jpg` (the game's Relay rig vs the proxy). In Figma: VL189 (the bars: sheet, S1b, C4b, `v_gamerig`), VL190 (`v_glass`, C4p), VL191 (`v_levels`), VL192 (`v_exitface`, X2).
