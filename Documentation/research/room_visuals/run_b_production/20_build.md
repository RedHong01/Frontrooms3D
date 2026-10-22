# Run! B · Emergency Power: build (stage 2)

Date: 2026-10-22, 13:09–13:58. 游戏视觉 `run_b` production workflow, stage 2 (build), re-run on account B (session local_a9a1dce2) after the 08:18–08:4x attempt died at the account limit.
- **Main was not touched** except this folder: `20_build.md`, `20_build_manifest.tsv`, `images/20_build_*.jpg`, `tools/*.txt`. No Unity on main, no git writes, nothing deleted.
- **Clone:** `W/proj_runb` (one clone; `W/proj_run_emergency_power` only read).
- **Load:** 1-min load 17–121 during the stage. Every timing below that was taken at load > 32 is marked **UNDER LOAD** and is not a gate. Counts, bytes and pixel checks do not depend on load.
- **Calls 2–8 of `../40_for_red.md` stay open.** Nothing built here answers one (§7).

`W` = `/Users/redwang/FrontRoomsVisualWork`. `M` = Red's `Frontrooms3D`.

---

## 0. Short answer

1. **B is built and runs in the clone.** The production rig `FrontRoomsRunRig`, the dress `FrontRoomsRunDress`, 18 Blender kit modules (20 FBX), 25 textures, 28 materials, the post profile, two shaders, eight per-state probe cubes and the RoomStream switch.
2. **Ward is still the default, and Ward is unchanged by construction.** Every B path in `FrontRoomsRoomStream.cs` is behind `runB` (§3).
3. **Compile:** 0 errors in Unity (OSXUniversal) and in Roslyn `--all`, also with `FRONTROOMS_WEBGL_PREVIEW` defined.
4. **Tests, all pass:**
   - new `FrontRoomsRunRigTests` 10/10 (T2);
   - `FrontRoomsStreamVerification` 5/5;
   - `FrontRoomsExitSignTests` 13/13;
   - build smoke `RUNB_SMOKE PASS`;
   - `FrontRoomsCxPlayRun` 20 s: READY, 0 pink renderers, only the two expected exceptions.
5. **Budgets:**
   - one twin-head unit is 10,128 / 3,266 / 406 triangles (budget 10,700 / 4,260 / 870);
   - every kit is inside its own §1 budget after three trims this stage;
   - new textures are 34.19 MB (budget 40).
6. **Two bugs found and fixed in the build smoke:**
   - the wraparound lens did not glow, because a second UV channel left the lens at UV 0 (§2.4);
   - five kits were over budget.
7. **Open, most important first** (§6):
   - the box probes are wired and switch with the state, but the baked cubes change the armed frame by ≤ 1/255, and batch renders only use them after an environment change. That goes to T8, in Play mode;
   - the tripped stream room needs the planned U3/U4 check (T7);
   - the dress is heavy for T11.

## 1. What the interrupted attempt left, and what this stage did with it

| Left by the 08:18–08:48 attempt | Checked | Kept / redone |
|---|---|---|
| Clone `W/proj_runb`, mkclone 08:07 from main 0ea38bb7; compile log `W/logs/runb_c01_compile.log` (0 `error CS`) | Main has moved to 2c450e11 plus the working tree. The base sha1 of every file B touches is unchanged | Kept. 22 non-B files were copied in from main at 13:13 (touch/mobile input, audio, `FrontRooms3DGame.cs`, `FrontRoomsWarnLamps.cs`, `FrontRoomsPrintDriver.cs`, StreamingAssets). `apply_local_patches.py`: nothing to do |
| 18 Blender modules + `_run_b_common.py`; FBX + sidecars from `blender_hero1-3.log` and `blender_sec1.log` | Some FBX were older than their modules. Five kits were over budget | **All rebuilt** from the current modules (`W/logs/runb/blender_all2.log`). Fire bell, sprinkler head and sprinkler main trimmed (§2.1). Wrap and rail rebuilt again after the UV fix |
| 25 textures from `W/tools/feature/run_b/gen_run_b_prod.py` (08:32) | All present. The sizes match the plan | Kept |
| `RunBeamCone` / `RunSoffitHalo` in `Resources/Rendering`; Surface shader +2 pragmas; RenderSetup +2 lines; URP asset +2 flags | Shader names differ from the look-dev copies (`FrontRooms/RunB/*`), so they do not clash | Kept |
| `FrontRoomsRunRig.cs` (complete), `FrontRoomsRunDress.cs` (complete), RoomStream hunk (complete) | Read line by line. Non-ASCII `§` replaced in the new files and in the added RoomStream lines only (main's own `§` lines kept). `WebGLTier` changed from `const` to `static readonly`, which removes the CS0162 warnings | Kept |
| Clone tools `FrontRoomsRunBSetup.cs`, `FrontRoomsRunBSmoke.cs` (not yet installed) | ASCII, no tuples | Installed (`install_tools.py --feature run_b`). Extended: the scripts re-import, build-smoke renders, the per-state probe bake and probe debug |
| Not written: the T2 test file, materials and post asset, probe cubes, any Unity import or test run | — | Written and run (§2, §4) |

## 2. What is built

### 2.1 Kits (Blender 4.3, main's kitlib, hand-built LOD0/1/2; `W/logs/runb/blender_all2.log`, re-checked in Unity's `setup_report.txt`)

| Kit | LOD0 / LOD1 / LOD2 tris | Budget (10_plan §1) |
|---|---|---|
| `Kit_EmergencyTwinHead` | 3,768 / 890 / 78 | 3,800 / 1,500 / 450 |
| `Kit_EmergencyTwinHead_Yoke` (×2) | 924 / 340 / 48 | 950 / 380 / 60 |
| `Kit_EmergencyTwinHead_Lamp` (×2) | 2,256 / 848 / 116 | 2,500 / 1,000 / 150 |
| **One unit** | **10,128 / 3,266 / 406** | **10,700 / 4,260 / 870** |
| `Kit_WrapFixture_4ft` | 2,216 / 384 / 112 | 2,400 / 960 / 160 |
| `Kit_PullStation` | 1,102 / 402 / 38 | 2,400 / 900 / 150 |
| `Kit_FireBell` | 1,436 / 584 / 112 (was 2,164) | 1,800 / 700 / 120 |
| `Kit_ExtinguisherCabinet` | 2,764 / 1,076 / 242 | 5,500 / 2,200 / 400 |
| `Kit_SprinklerHead_Upright` | 544 / 200 / culled (was 800) | 700 / 220 / 0 |
| `Kit_SprinklerMain_3m` | 400 / 180 / 32 (was 560) | 500 / 200 / 40 |
| `Kit_PipeHanger_Band` | 284 / 72 / culled | 300 / 100 / 0 |
| `Kit_CableTray_3m` | 1,972 / 832 / 120 | 2,600 / 900 / 120 |
| `Kit_BumperRail_1200` | 188 / 100 / 24 | 400 / 140 / 24 |
| `Kit_CornerGuard_1200` | 32 / 24 / 12 | 120 / 40 / 12 |
| `Kit_SafetyPlate_10x7` (+ `_Electrical`, `_Staff` variants) | 212 / 28 / 12 | 300 / 100 / 12 |
| `Kit_StencilNotExit` | 2 (decal quad) | — |
| `Kit_ChairDolly8` | 11,784 / 3,984 / 268 | 12,000 / 4,800 / 900 |
| `Kit_JanitorCart` | 6,587 / 1,962 / 256 | 9,000 / 3,600 / 700 |
| `Kit_HandTruck_Cartons` | 3,944 / 1,512 / 296 | 4,500 / 1,800 / 350 |

**The three trims** keep the 0.2 mm chord rule:
- **Fire bell:**
  - the gong uses the rule's 44 segments, not a forced 48;
  - LOD0 drops the two inner-bowl rings that no reachable view sees (it hangs at 2.3 m);
  - the hidden gong post goes to 8 sides.
- **Sprinkler main:** the coupling has 4 profile rings, not 5.
- **Sprinkler head:** the welded outlet boss has no bevel. Its foot sits in the pipe and its top faces the deck.

**Checks so far:**
- Unity imported every slot onto a `Resources/Surfaces` material; no slot fell back to the FBX material.
- The three-views of the hero kits read correctly (`images/20_build_hero_kits.jpg`).
- The 0.3 m hero close-ups and the LOD-switch frames are T4 / VL338 (stage 6).

### 2.2 Textures, materials, post, cookies
- **Textures:** 25, all BC7 with mips, **34.19 MB** in the editor (budget 40; r8 was 81). The WebGL tab of each is capped at 1024 by the setup tool; the desktop tabs are main's importer.
- **Cookies:** `Cookie_SealedBeam_S` and `Cookie_SealedBeamCore_S`, 256², desktop only.
- **Materials (28, `FrontRooms/Surface` except the stencil):**
  - 7 shell: `RunB_BlockUpper`, `RunB_BlockLower`, `RunB_Soffit`, `RunB_Floor`, `RunB_FloorAccent`, `RunB_FloorLane`, `RunB_FloorWax`;
  - 20 kit slots;
  - `Run_StencilNotExit` (URP Lit, alpha clip).
- **`RunB_CabinetWhite` is unused**, so it is excluded from the package.
- **The floor is Red's glossy waxed VCT:** one world-projected 1.2 m set and four slabs. The lane is ×1.06 smoothness and the wax band ×1.12.
- **`FrontRoomsPost_Run_B`** has the r8 values: lift w 0, contrast +12, saturation −10, temperature −6, post-exposure −0.1, bloom 1.0 / 0.6, vignette .32.

### 2.3 Code (ship paths)

| File | What |
|---|---|
| `Assets/Scripts/Rendering/FrontRoomsRunRig.cs` (new) | `RunLook`, `RunRigState`, `RunRigHost`, `RunRigLayout` with `Corridor1x9()` / `StreamRoom()`. States: Armed / Tripping / Tripped / Resetting. API: `Trip`, `StartReset`, `ResetImmediate`, `ApplyWarn`, deterministic `Evaluate(t)`, allocation-free `Tick`. Events for the sound chat (`StateChanged`, `MainsDropped`, `TransferRelay`, `ResetStarted`); no audio. Per-state probe sets that are toggled and never re-textured. SH ambient through property blocks. The `Run_B` post volume at priority 2. Light culling at 16 m (10 m in WebGL). The WebGL tier sits behind `(UNITY_WEBGL && !UNITY_EDITOR) \|\| FRONTROOMS_WEBGL_PREVIEW` |
| `Assets/Scripts/Rendering/FrontRoomsRunDress.cs` (new) | VCT bands snapped to the world 0.30 m grid, lower tone, cove, bumper rail, soffit, sprinkler main / heads / hangers, tray and trapezes, EMT with straps and handy boxes, fire gear, plates, RN-K side-door props, obstacles (box colliders) |
| `Assets/Scripts/FrontRoomsRoomStream.cs` (hunk, base 4e6e8e7f) | The switch and the host-lamp hook (§3) |
| `Assets/Resources/Rendering/FrontRoomsSurface.shader` (+2 lines, base a82fe187) | `_REFLECTION_PROBE_BLENDING`, `_REFLECTION_PROBE_BOX_PROJECTION` |
| `Assets/Resources/Rendering/RunBeamCone.shader`, `RunSoffitHalo.shader`, `FrontRoomsPost_Run_B.asset` (new) | Beam cone, soffit halo, post |
| `Assets/Editor/Rendering/FrontRoomsRunRigTests.cs` (new) | Gate T2 (§4) |
| `Assets/Editor/Rendering/FrontRoomsRenderSetup.cs` (2 lines, base 256cf3a4) | **Separate package** (`run_b_rendersetup`): sets the two probe flags in `EnsurePipeline` |
| `Assets/Settings/FrontRooms_URP.asset` (2 flags, base 18584f2b) | **Held.** It is outside the ship list (10_plan §7.3) |

Every new file is ASCII only with no C# tuples. No new `.meta` GUID is used in main (checked against all 1,289 GUIDs in main's Assets). No EXIT-sign file is changed: B only calls `Configure`, reads `Level`, and leaves `LockLit` at its default.

### 2.4 Bug found and fixed: the wrap lens did not glow
- **Symptom.** In the first smoke frames the hero wrap's lens was dark olive, with no emission.
- **Cause.**
  - `_run_b_common.mesh_part` created its UV layer by name (`"UVMap"`).
  - kitlib's `_uv_metres` uses `layers.uv.verify()`, which in Blender 4.3 creates `"Float2"`.
  - The join kept both, so the exported mesh had two UV channels. Unity reads channel 0, where the lens sat at (0, 0), on a black emission texel.
- **Fix.** `mesh_part` now uses `verify()`, as main's `_newcase.py` does.
- **Re-check.** Every B kit was checked in Blender: each has one UV channel, and no slot is stuck at UV 0.
- **Other module.** The bumper rail's sweep had the same bug; it has no textures, so nothing showed.

### 2.5 Per-state box probes
- **Baked:** one Unity run per state (`FrontRoomsRunBSmoke.BakeProbesBatch -runbState armed|tripped`). Custom, 256 px, HDR.
- **Importer:** the zone cubes' settings (specular convolution, BC6H, WebGL 128 px RGB9e5).
- **Cubes:**
  - `Rendering/Reflections/RunB_StreamRoom_{armed,tripped}_0`: one box, 11.82 × 3.22 × 12.32 m;
  - `RunB_Corridor1x9_{armed,tripped}_{0,1,2}`: 3.16 × 3.22 × 9.6 m.
- **Size:** 9.53 MB of EXR on disk. At runtime each cube is about 0.5 MB as BC6H.
- **Measured brightness:**

| Cube | Mean |
|---|---|
| Stream room, armed | 0.33 |
| Stream room, tripped | 0.047 |
| Corridor, armed | 0.26 |
| Corridor, tripped | 0.064 |
| Level 0 zone cube, for comparison | 0.27 |

## 3. How B sits on today's RoomStream Run room
- **The switch.** `runLook` is a serialized field with default `Ward`. `FrontRoomsRoomStream.RunLookOverride` is for look-dev and tests, and `ActiveRunLook` reports the result.
- **With Ward, every new line is skipped:**
  - `RuleWall/Floor/Ceiling` fall through to `ProfileMaterial`;
  - `RunMains` returns 1, and `light.enabled = active && 1 > .0005` is the same test as before;
  - lamp odds, the stutter, exit-sign odds, `BuildRoom`, `RefreshRoomMaterials` and `Tick` all take their old branch;
  - no `UnityEngine.Random` call was added.
  - So Ward frames are unchanged by construction.
  - The ExitSign walk (17 Run rooms, hash-dealt temperaments) and the stream suite both pass on Ward.
  - The pixel gate T3 (65 + 10 frames) is stage 6.
- **With EmergencyPower:**
  - **Pool init:** the Run variant of every slot builds the dress, the rig (`HostLamps`), the flag sign and the wall sign over the far door.
  - **Lamps:** each of the stream's 12 lamps shows a `Kit_WrapFixture_4ft` with a soffit halo, in place of the troffer pan and diffuser. The lamp cone is 172° / 124°, 5.5, range 10. All lamps work, and fixture 5 is the stutter tube at 0.78.
  - **Mains:** the lamp output and lens emission are multiplied by `rig.MainsAt(z)`. This is the one hook in the lamp code.
  - **Trip:** once per room instance, when the camera is 1.5 m in, the door behind is shut and the room's lamps are cued. The stream raises `RunTripped(sequence)`.
  - **Recycle:** a recycled slot gets `ResetImmediate()`.
  - **EXIT signs:** dealt Lit or Half, never Loose.
- **The default is not flipped.** That is call 6.

## 4. Tests

| Run | What | Result | Time, 1-min load |
|---|---|---|---|
| `W/logs/runb/c02_setup.log`, `c12_setup2.log` | Import, WebGL caps, materials, post, kit re-import, report | 0 `error CS`; `setup_report.txt`: 34.19 MB, all slots remapped | 21 s at 27, then 12 s at 45 (UNDER LOAD) |
| Roslyn `--all` (`W/tools/_rebase/runb_r4`, `runb_r12`) | All 7 assemblies | 0 errors | — |
| Roslyn with `#define FRONTROOMS_WEBGL_PREVIEW` (`W/tools/_rebase/runb_r1_webgl`) | WebGL-tier compile | 0 errors | — |
| `c03_rigtests.log`, `c28_rigtests2.log`: **`FrontRoomsRunRigTests`** (new, T2) | R1 determinism; R2 photosafety per lamp (trip 0.5, reset 2, stutter 1, 2 Hz warning 2.5 flashes/s; bar ≤ 3); R3 Reduce Flashing (each lamp one monotone change, darkest room level 0.244, monotone re-strike, steady stutter); R4 one-shot trip (1 mains drop, 8 relays); R5 idempotent reset; R6 0 B in `Tick` (armed, tripped, resetting); R7 corridor lights armed 11 (3 shadowed), blackout 2, tripped 10 (8 shadowed); S1 Ward builds no B object; S2 the B stream walk (8 Run rooms, 8 trips, each exactly once, 1.5 m in with the door shut, 43 recycles, rebased; a tripped room shows 6 rig lights, 4 shadowed, and 0 ceiling lamps; rigs ahead are armed; lights, probes and post stay in their slot); W1 | **10 / 10 PASS** | 10 s at 25 and 12 s at 44 (UNDER LOAD) |
| `c04_smoke.log`, `c29_smoke2.log`: build smoke | Stream B (5 rigs, 4 heads, 2 probes, flag sign), Ward, corridor rig + dress | `RUNB_SMOKE PASS` | 8 s at 48 (UNDER LOAD) |
| `c05_streamver.log`: **`FrontRoomsStreamVerification.Run`** | Handoff, profiles, Relay hunt / catch / sprint | **5 / 5 PASS** | 9 s at 74 (UNDER LOAD) |
| `c06_exitsign.log`: **`FrontRoomsExitSignTests -quit`** | T5, D1–D4, R1, R2 ×2, G1–G3 | **13 / 13 PASS** | 13 s at 78–93 (UNDER LOAD) |
| `c07_cxplay.log`: **`FrontRoomsCxPlayRun`** `-cxLabel runb -cxSeconds 20 -cxSeed 516574485` | Play smoke of the game (Ward) | READY at 13:23:44; 1,308 frames; G14 on (High); 0 pink renderers; only the FMOD `BankLoadException` and the QuickSearch `ArgumentOutOfRange` (both expected, TOOLS.md §2) | 88 s at 90 (UNDER LOAD) |
| `c09_threeview.log` | Three-views of the 4 hero kits | 16 PNG | 27 s at 17 |

## 5. Build-smoke frames (not VL frames; Shot format)
- `images/20_build_smoke_streamroom.jpg`: the edit-mode profile preview's Run room. Panels: Ward; B armed; B tripped +3 s (host lamps switched off by hand, as the mains hook does in play); a twin-head close-up.
- `images/20_build_hero_kits.jpg`: three-views of the cabinet, lamp, yoke and wrap, with the wrap and the unit in the room.
- `images/20_build_probe_check.jpg`: the probe plumbing check.

**What the frames show:**
- **Armed B reads as the proposal:** lit wraps on a dark painted soffit with halos, the red sprinkler main, two-tone block, glossy VCT with accent rows, a steady red flag sign, fire gear, side doors and obstacles.
- **Tripped:** the four heads with beams and the red EXIT spill. The room is otherwise dark (see §6.2).

## 6. Open issues (for stage 6 and the orchestrator)

1. **The box probes: wired, but no visible effect yet.**
   - **The wiring works.**
     - Magenta/green test, VL236 method: 23 % of the floor window is solid magenta, 0 % green. So the floor samples the B probe, not the default.
     - The tripped set takes over after `Trip()` in the same run.
   - **What does not show yet:**
     - (a) In a batch edit-mode run, the Forward+ probe atlas only uses the probes after a `RenderSettings` change (reflection intensity 0.3 → 0.301 is enough). Every B frame in §5 therefore has no probes in effect.
     - (b) With that refresh, the baked cubes change the armed frame by at most 1/255 against probes off. This was also checked with uncompressed cubes and with a runtime copy, so the format and the import are not the cause.
     - At this light level the floor's gloss is the direct specular, and the cube reflections are too weak to show.
   - **T8 must:**
     - measure in Play mode, in the tripped state with the host lamps really off;
     - run P0 (is the blending flag needed?);
     - then decide between a probe gain, the plan's fallback (one set, two blended cubes), or dropping the probes.
2. **The tripped stream room is dark between the pools.**
   - With 2 units, U1's −Z head lands mostly on the entry wall.
   - The goal head lands on the open door pair; in play the door is shut.
   - The floor lane gets mostly spill.
   - The aims are starting points. The T7 no-pocket test (every reachable 0.5 m cell ≥ pool max / 40) will most likely call for the pre-planned U3/U4 (+4 shadowed spots, 8 shadowed when tripped).
3. **Cost risk for T11.**
   - **Stream room:** one slot's dress is 328 renderers and 147,101 triangles over all LODs.
   - **Corridor:** 371,940 triangles over all LODs, 270,279 at LOD0 only.
   - **Heavy reused kits:**
     - `Kit_OutletHandyBox`, 3,860 tris (×4 in the stream room);
     - `Kit_ConduitEMT`, 576 per 1 m tile (×36);
     - each side door, about 11,000 (frame, leaf, closer);
     - the chair dolly, 11,784.
   - **Budget:** Ward + 60k tris and + 60 draws at S1b.
   - **Shadow-atlas warning:** the edit preview logged "Reduced additional punctual light shadows resolution by 2 to make 20 shadow maps fit". All five preview rooms are lit at once there, so recheck in the real stream.
   - **Pool init:** B builds its variant in all five slots, so nothing pops in view. The init time is not measured yet.
4. **The plate faces and the unit legend have not been reviewed by 平面视觉 yet.** They must be reviewed before shipping (type rule). Files: `Run_Plate{Notice,Electrical,Staff,Pull,Extinguisher}_A`, `Run_StencilNotExit_A`, `Run_UnitLegend_A`.
5. **Importer rules (RenderSetup, outside the 2-line hunk).**
   - The plates, legend and cookies import with Repeat wrap: main's importer clamps only the ExitSign decals. Expect at most a 1-px edge bleed at far mips.
   - `RunB_VCT_N` and the other normal maps came out BC7, not BC5.
   - Both are the RenderSetup owner's call.
6. **Packaging points (10_plan §7.3), not decided here:**
   - the URP asset's two probe flags;
   - the WebGL keyword stripper, which is not written because its path needs an OK. Without it, WebGL carries the two extra keyword variants.
7. **Not done, by plan:**
   - T3 Ward pixel identity;
   - T4 0.3 m hero / LOD frames;
   - T5 r8 frame match;
   - T6 Relay ≥ 4 : 1 with protan;
   - T9 rendered photosafety timelines;
   - T10 sprint;
   - T11 frame cost;
   - T12 WebGL preview render;
   - the `W/apply/run_b.sh` and `run_b_rendersetup.sh` packages;
   - the VL338–VL341 specs.
8. **Rule slip, reported.**
   - At about 13:30 one 20-s debug run (`c10_debugwrap.log`) started while two other visual-line Unity jobs were running, so it was a third job.
   - Cause: my first job counter matched its own shell line.
   - Every later run waited on an anchored counter (fewer than 2 running).

## 7. Calls 2–8 stay open
- **2 (goal door):** `goalPoint` / `goalNormal` are data. No door is built; the stream's own far door pair is the goal.
- **3 (trip sound):** C# events only (`RunTripped`, `MainsDropped`, `TransferRelay`, `ResetStarted`); no audio.
- **4 (brightest object):** `goalHeadGain` = 5 (r8), one parameter.
- **5 (Office zones):** the look and post are B's own, whatever the host zone.
- **6 (title Run room):** `runLook` stays `Ward`.
- **7 (shared goal treatment):** the wall sign keeps the family default (EmissionGain 1, no light); there is no lit lite.
- **8 (media):** nothing was downloaded.

## 8. Files
- **Full list:** `20_build_manifest.tsv`, LF line endings. 237 rows: 233 new (126 files + 107 `.meta`) and 4 modified, with full sha1, size and target package.
- **Clone-only tools,** with `.txt` copies in `tools/`:
  - `W/tools/feature/run_b/gen_run_b_prod.py`;
  - `.../Assets/Editor/RunB/FrontRoomsRunBSetup.cs` (`RunBatch`, `ReimportScripts`);
  - `.../FrontRoomsRunBSmoke.cs` (`RunBatch`, `RenderBatch [-runbSkipWard]`, `BakeProbesBatch -runbState`, `ReimportCubesBatch`, `ProbeDebugBatch`, `DebugWrap*`).

Key sha1 (clone, 13:56):

| Path | Base (main) | Now |
|---|---|---|
| `Assets/Scripts/FrontRoomsRoomStream.cs` | 4e6e8e7f9a19ea98bc9afb74cdc61373c61fe353 | 0552e1a19a5527e984045bd0db91fccb35bede22 |
| `Assets/Resources/Rendering/FrontRoomsSurface.shader` | a82fe1872b133c2a92e96201cb44828323885cff | 6cd7ee0188988056a515c893d2909ca55f8edf08 |
| `Assets/Editor/Rendering/FrontRoomsRenderSetup.cs` (separate) | 256cf3a449266de795ba4675ec660e9f28af35a6 | 9b9c76ee729299406410df2bd4b44033ce9af61c |
| `Assets/Settings/FrontRooms_URP.asset` (held) | 18584f2bb983c440f38ba4af08f098caf1cc1678 | 70c12d6cd5cc3c1de2300550dac198e688c9d739 |
| `Assets/Scripts/Rendering/FrontRoomsRunRig.cs` | new | 9502f0b54234fb5eecc33e242fa35b630705a482 |
| `Assets/Scripts/Rendering/FrontRoomsRunDress.cs` | new | 568f83d7b90a6e5ec5153b0951e70db0fe916689 |
| `Assets/Editor/Rendering/FrontRoomsRunRigTests.cs` | new | 992e719cbc236be9141c8163bdf12b82d245206d |
| `Assets/Resources/Rendering/RunBeamCone.shader` | new | 76d652f30c6b21be05e648b174b0ad205c4b850c |
| `Assets/Resources/Rendering/RunSoffitHalo.shader` | new | 56c1ff6a27b76f3c517a86acfc2b488cee0e0017 |
| `Assets/Resources/Rendering/FrontRoomsPost_Run_B.asset` | new | f6476f348c89882a846363359f9926913c7b8394 |

**Unchanged and read only:**

| Path | sha1 |
|---|---|
| `FrontRoomsExitSign.cs` | 4848e486 |
| `FrontRoomsPostStack.cs` | 68824661 |
| `FrontRoomsSurfaces.cs` | ab279371 |
| `kitlib.py` | d6de07ab |
| `FrontRoomsHunter.cs` | 57d9fad5 |
| `FrontRoomsKitLibrary.cs` | 1f7dac20 |

All were re-checked against main at 13:56; main HEAD is 2c450e11, and no landing after 13:06 touches them.
