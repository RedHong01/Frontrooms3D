# 20 — Run! direction B · EMERGENCY POWER: build log, costs, needs, open issues

Date: 2026-10-08. Companion to `11_run_dir_emergency_power.md` (the values, the measurements, pass / fail).
Clone: `/Users/redwang/FrontRoomsVisualWork/proj_run_emergency_power` (clone-only; nothing was promoted). A second copy, `W/proj_run_emergency_power_x`, ran the matte comparison renders in parallel.
Red's project was not opened in Unity. Writes there: this folder (`research/room_visuals/`: reports, images, `harness/`) and the two rows VL194–195 in `Documentation/VERIFICATION_LOG.md`.

---

## 1. What was built

A clone-only Unity look-dev scene, built in code and rendered in batch (Unity 6000.3.10f1, `-buildTarget OSXUniversal`, URP 17.3 Forward+, 4× MSAA):
- a Level 0 anteroom (3 × 2 cells), a 1 × 9 cell (27 m) back-of-house service corridor and a one-cell Level 0 stub behind the goal door, rooted at world (4992, 0, 4992);
- the hero object, a 1975–1992 wall-mounted twin-head battery emergency unit, modelled in Blender in three parts (cabinet, yoke, sealed-beam lamp head; kitlib conventions, LOD0) and imported with the clone's `FrontRoomsKitImporter`; the rig aims every head through its yoke and tilt;
- a 4 ft prismatic wraparound fixture (Blender kit model);
- generated period textures: painted CMU, service VCT (glossy and matte masks), painted soffit, EXIT face, NOTICE / ELECTRICAL ROOM / STAFF ONLY plates, a NOT AN EXIT stencil, pull-station and extinguisher legends, the unit legend, the sealed-beam lens and two spot cookies;
- a deterministic state rig, `FrontRoomsRunLookdevRig.Evaluate(t)`: armed wraps and pilots, the mains drop at t 0, the 0.35 s black, the staggered filament rise of 8 heads, beam cones, the corridor SH per state, floor-bounce fills;
- box-projected reflection probes baked per state for the glossy waxed VCT (Red's 2026-10-03 decision);
- the local post volume `FrontRoomsPost_Run_B` (priority 2);
- the measurement and export pipeline (ID masks, greyscale and protan copies, contact sheet, 30 fps photosafety timeline, read-distance and outline checks, probe on/off comparison).

Run (one Unity run per probe state; each ~1–2.5 min on this machine):
```
U=/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity
$U -batchmode -quit -projectPath W/proj_run_emergency_power -buildTarget OSXUniversal -executeMethod FrontRoomsRunLookdev.RunBatch -frGlassRT off -runOut <dir> -runState armed   -runShots s1a,s2,c1,x_pull,x_armed_unit,x_unit_armed,x_wrap -runNoTimeline -logFile <log>
$U ... -runState tripped -runShots s1b,s3,c3,c4,x_goal,x_unit_tripped,r_cart,r_truck -runNoTimeline
$U ... -runState none    -runShots c2,timeline
$U ... -runState none    -runShots s1b,x_goal,s1a -runNoTimeline -runNoId      (the "probes off" comparison)
$U ... -runMatte -runState armed|tripped ...                                    (the matte comparison)
```
Then `python3 merge_r7.py <out> <armed> <tripped> <none> [<noref>]` and `W/venv/bin/python measure_b_r8.py <out> <images> run_dir_emergency_power <json>` (copies in `harness/emergency_power/tools/`; `run_r8.sh` and `run_r8m.sh` hold the exact commands). Other switches: `-runHead`, `-runBeam`, `-runGoalGain`, `-runGoalCone`, `-runBounce`, `-runExitE`, `-runWrap`, `-runWrapCone`, `-runSweep "name:key=v,shots=a+b;…"` (light sweeps only; probe changes need their own run), `-runProbeDebug` (probes magenta, default cube green), `-runProbeMul`, `-runMatte`.

### 1.1 Rounds

| Round | When | What changed | Result |
|---|---|---|---|
| r1–r15 | 2026-10-03/04 | First builds in the old scratchpad clone (heads 4 → 10, spill .06 → .02, beam .02 → .05, lift −.01 → 0) | Lost in the 2026-10-05 reboot; the harness, rig, textures and Blender modules were recovered from the transcripts on 2026-10-07 (`W/tools/feature/run_lookdev/emergency_power`, `run_ep_work/recon`) |
| r1 | 10-07 17:13 | Recovered build, first full render in the new clone; G14 RT opt-in removed from the camera (it stalled > 20 min) | Base. The sprinkler main poked through both end walls |
| r2 | 17:37 | Main cut to the wall faces; floor-bounce fill; goal head as a narrow spot (26° / 22°, gain 5) | Goal pool hottest on the floor; cart 1.08, truck 1.63 : 1 |
| r3 | 19:39–20:08 | Glossy waxed VCT (`Run_ServiceVCTGloss_S`) and three box-projected probes per state (Red's 10-03 decision); Surface-shader and URP-asset probe keywords | Stage stopped by the usage limit before the stats and timeline |
| r4 | 10-07 23:34 | Resumed: read-distance shots, probe on/off sweep, charcoal cart | **Probes added nothing** (≤ 9 of 765 levels): the boxes sat exactly on the shell |
| r5 | 23:4x | Probe boxes overhang the shell by 0.16 m, blend 0.15 | Probes reach the floor, but tripped frames showed the armed cube (bluish far walls) |
| r6 + debug | 10-08 00:0x | One probe set per state toggled by `enabled`; magenta / green debug | Debug: probes cover every shell pixel. But probes off rendered identical to probes on: state changes after the first render never reach the atlas |
| r7 | 00:1x–00:2x | One Unity run per state (`-runState`); matte runs on a second copy | Correct per-state reflections: the far floor reflects the lit goal |
| r8 (final) | 00:36–00:39 | Light platinum-grey cart, white cartons | Truck 4.39 : 1 at 8 m; cart still 1.16–1.33 (outline 1.84–2.22) |

Every round's frames were read by eye before the next change.

### 1.2 The probe findings (VL195)

1. **A box probe weighs zero on its own faces.** URP 17.3 weights a probe by min(distance to a box face) / blend distance. A box sized exactly to the room puts every floor, wall and soffit pixel on a face: weight 0, so they all sample the default cube. Fix: the influence box overhangs the shell by more than the blend distance (0.16 m with a 0.15 blend). Cost: box projection lands 0.16 m behind the real faces (a 6 % parallax error across 2.84 m). The Red Ward render (11_run_dir_red_ward.md D9, "the box probes did not reach the Surface walls") almost certainly met the same thing.
2. **Probe state is frozen after the first render in a batch run.** URP 17.3's Forward+ `ReflectionProbeManager` caches each probe by instance id and re-blits `cachedProbe.texture`, the texture it first saw; entries are only evicted when `Time.renderedFrameCount` moves on, which it does not inside one `-executeMethod`. Measured: swapping `customBakedTexture` kept the armed cube in tripped frames (r5); toggling `enabled` and `intensity` changed nothing (r6, probes off = probes on, 0 px over 6 levels). Fix here: one run per state. **For the game:** the rig must not swap a probe's texture in place; use one probe set per state and toggle them, and check in Play mode that the atlas follows (frames do advance there, so it should).
3. With both fixed, the probes move 40.8 % of the goal close-up's pixels and 0.5 % of S1b (the far floor reflecting the lit goal at a grazing angle). Steep views change little: a dielectric floor reflects ~4 % head-on. The direct specular glints of the heads on the wax carry most of the "glossy" read.

---

## 2. Change list (clone-only)

| Path in the clone | Change |
|---|---|
| `Assets/Editor/Rendering/FrontRoomsRunLookdev.cs` | New. The harness (copy: `harness/FrontRoomsRunLookdev_emergency_power.cs.txt`) |
| `Assets/Scripts/Rendering/FrontRoomsRunLookdevRig.cs` | New. The state rig (copy: `harness/emergency_power/FrontRoomsRunLookdevRig_emergency_power.cs.txt`) |
| `Assets/Resources/Lighting/RunBeamCone.shader`, `RunSoffitHalo.shader` | New. Additive beam frustum and wrap soffit scallop (copies in `harness/emergency_power/`) |
| `Assets/Resources/Rendering/FrontRoomsPost_Run_B.asset` | New. The Run B post profile |
| `Tools/Blender/frontrooms_kit/assets/run_twinhead.py`, `run_twinhead_yoke.py`, `run_twinhead_lamp.py`, `run_wrap_4ft.py` | New Blender modules (copies in `harness/emergency_power/blender/`) |
| `Assets/Resources/Props/Models/Kit_EmergencyTwinHead{,_Yoke,_Lamp}.fbx/.json/.meta`, `Kit_WrapFixture_4ft.*` | New, imported by `FrontRoomsKitImporter` (sidecars in `harness/emergency_power/kit_sidecars/`) |
| `Tools/lookdev/gen_run_b.py` | New texture generator (copy in `harness/emergency_power/tools/`) |
| `Assets/Resources/Surfaces/Textures/Run_*`, `Cookie_SealedBeam_S`, `Cookie_SealedBeamCore_S` | New: Block A/N/S, ServiceVCT A/N/S + `Run_ServiceVCTGloss_S`, SoffitPaint A/N/S, ExitSignB A/E, 5 plates, the stencil, UnitLegend, SealedBeamLens A/N/S/E, WrapLens A/N/E, WireGrid |
| `Assets/Resources/Surfaces/Run_{SealedBeamLens, NeonPilot, UnitLegend, WrapLens}.mat` | New kit slot materials |
| `Assets/RunLookdevProbes/RunB_probe{0,1,2}_{armed,tripped}.exr` | New baked cubes (256 px HDR) |
| `Assets/Resources/Rendering/FrontRoomsSurface.shader` | `_REFLECTION_PROBE_BLENDING` and `_REFLECTION_PROBE_BOX_PROJECTION` multi-compiles (diff in `harness/emergency_power/`). **Needed** for the glossy floor |
| `Assets/Settings/FrontRooms_URP.asset` | Reflection-probe blending and box projection on (diff) |
| `Assets/Settings/FrontRooms_URP_Renderer.asset` | The G14 Metal Glass RT feature off (diff): the look-dev scene has no RT glass, and the first render after the opt-in stalled > 20 min in batch |
| `Assets/Editor/Audit/*`, `Assets/Editor/Rendering/{FrontRoomsKitLookdev, FrontRoomsLookdevCapture, FrontRoomsHunterLookdev, …}` | The recovered capture tools from `W/tools` (W/TOOLS.md) |

Nothing here is an apply script: this direction is a pre-render for Red's pick. No codex-audit finding names this track.

---

## 3. Measured costs

| Item | Value |
|---|---|
| Lights in the corridor | Armed 12 (9 wraps, 3 EXIT glows); tripped 15 (8 heads, 3 EXIT glows, 4 look-dev bounce fills). The brief's estimate was 9 / 10: the extras are one more EXIT glow spot (D8) and the bounce fills (D9), which production replaces with baked probes (tripped then = 11) |
| With the anteroom (6) and the stub (1) | Armed 19, tripped 22, blackout 10 |
| Shadowed lights | Armed 5 (2 anteroom + 3 wraps); tripped 10 (2 anteroom + 8 heads) |
| Draws | 683 corridor submeshes before batching; ≈ 87 with static batching (49 static materials + 38 rig-driven renderers) |
| Triangles | 74,018 in the corridor; hero units 4 × 10,498 (cabinet 3,706, two yokes 916, two heads 2,480 each); wraps 200 each |
| Textures | New Run B textures and cookies 81 MB at editor runtime size (28 textures). Plates, the stencil and the EXIT face are RGBA32 (3–8.5 MB each): compress them for production |
| Probes | 3 per state, 256 px HDR, 1 MB each; the Forward+ atlas holds the visible set |
| Per-frame extras | SH sets on the corridor renderers when the state changes; 8 additive beam cones (2.5 m) |
| Render time (look-dev) | 47–137 s per state run on this machine (load 280–380), probe bakes included |

---

## 4. What a real implementation needs, from whom

**Map chat (关卡设计).** The exact proposals are in `10_run_directions.md` §8:
- CR-1, the 1 × 9 straight (module `Run_AlarmCorridor_1x9`, `ModulePlacement.Corridor`);
- CR-2, `ModuleTrigger.Alarm` armed 1.5 m in, plus `RoomReset`;
- CR-3, `ModuleFinish.Run` (B: painted block, service VCT, painted soffit, no tiles);
- CR-4, `ModuleLamp.Off` on all 9 cells, `ChunkBuilt` / `ChunkDropped`, `WarnStage` forwarded, the `LampFx` chase wave and sags skipping these cells;
- CR-5, placement rules;
- CR-6, the latching RN-X goal door and the 3 locked RN-K side doors (z 5 R, 17 L, 23 R) as never-unlockable door edges or non-opening props.
- B adds no other door types and no moving parts.

**Sound chat (声音设计).**
- `Run.MainsDrop` (t 0): a contactor drop-out clunk; every fixture hum stops at once.
- `Run.TransferRelay` × 4 at 0.362 / 0.386 / 0.410 / 0.434 s: small relay pull-in clicks from each unit (already in the foley motif).
- Optional: the units' charger buzz in standby, stopping at t 0.
- The bell stays silent (EGRESS).
- `Run.Reset` (mains return, heads click off, ballasts strike).
- CR-S1, a hard-floor footstep surface (VCT); CR-S2, a hard-corridor reverb.

**Interactables track.** RN-X (wood-grain leaf with the wired lite, crossbar, latch, closer: a blockout here; Red Ward made a clone-only `Kit_DoorLeaf_Ward_Lite` that B can share) and RN-K with sign plates.

**Glass track.** `Glass_Wired` (a URP Lit stand-in plus a wire grid here). The G14 RT path would add true and secondary reflections on the waxed floor; this render shows the URP probe version only.

**Exit-sign track** (`research/exit_sign/20_build.md`). Use its 1990 EXIT family (flag-mounted double-faced and wall signs) instead of these blockout housings. Carry over: light leaves through the faces (face spots, not a point at the housing), and the 0.43 m housing width that 152 mm Heros Condensed letters need (check against that track's model).

**Visual chat (production `FrontRoomsRunRig`).**
- States: armed, tripping (0.06 s drop, 0.35 s black, staggered rise), tripped, reset (heads out, wraps re-strike ≤ 2 flicks each, 0.15 s per cell from the entrance; not rendered here).
- The twin-head unit at LOD0 / LOD1 (the Blender modules are ready to promote through the kit pipeline), the wraparound kit, the pull station, the bell, the extinguisher cabinet, sprinkler main and heads, tray, EMT, bumper rail, the three obstacle props.
- The corridor ambient per state (SH via property blocks works in URP 17.3; or `_FR_AmbientScale` in the Surface shader), and baked tripped/armed lighting (APV scenarios or probe sets) in place of the four look-dev bounce fills.
- **The glossy floor:** the Surface shader needs the probe-blending and box-projection keywords (a visual-owned file), the URP asset needs both settings on, and each Run room needs one probe set per state with boxes that overhang the shell; never swap a probe texture in place (§1.2).
- Light culling (16 m desktop) and the WebGL tier gate (4 wide heads, no cookie, no cones; not rendered).

**Relay rig (visual chat).** The game's prototype Relay is 3.05 m tall and does not fit a 2.9 m soffit; the read here (8.06 : 1) is the brief's proxy.

**平面视觉.** The EXIT face (TeX Gyre Heros Bold Condensed, 152 mm), the plates (Bold), the NOT AN EXIT stencil and the unit legend go through 平面视觉 before anything ships.

---

## 5. Open issues

1. **Goal region: PARTIAL.** The goal (0.178) is brighter than every pool and object, but the near head's own wall scallop (0.43) and floor glint (0.60), 2 m from the camera, are brighter regions. Raising the goal head further blows out the door at 5 m (`x_goal`). Red decides whether "brightest object, second after the fixture's spill" is enough.
2. **Janitor cart: 1.16 : 1 (S1b), 1.33 : 1 at 8 m.** It reads as a light mass with a yellow bucket and a dark bag; its outline separates at 1.84–2.22 : 1, but the brief's mean-vs-ring bar averages the light and dark parts. Options: a single-tone cart, or move it 0.5 m off the wall into the pool centre.
3. **p95/p5 ≥ 8 and saturated red 1–3 % conflict with the pool target** (§4.2 bar 7 in `11`). Red picks: brighter pools, or the targets relaxed. Paint reds cannot reach the WCAG "saturated" ratio; the EXIT glow carries the red (2.66 % red-dominant in C3).
4. **Glossy reflections fill part of the far dark band** (z 20–24 at ~.055 instead of ~.005), because the waxed floor mirrors the lit goal at grazing angles. Still 22 : 1 along the axis. This is physical; it softens the band rhythm near the goal.
5. **Probe parallax:** the 0.16 m box overhang puts reflections 6 % off across the width; RT (G14) removes it.
6. **The probe-state freeze in batch** (§1.2) affects every look-dev harness that changes probes between shots. Red Ward's D9 is probably the box-on-shell issue, not missing keywords alone.
7. **Look-dev stand-ins:** four floor-bounce spots and a hand-set SH bounce model replace baked lighting; production must bake (or use APV scenarios) for armed and tripped.
8. **Wrap cone 172° / 124°** (brief 162° / 96°): the reason was in the lost 10-04 transcripts.
9. **Base is main at 10-07 16:3x:** main's 19:22 hard-edge Level 0 print (Q1b) is not in S2 / C1.
10. **The game's directional fill** ("Soft ambient direction" 0.16, audit F5) is absent here. If the game keeps it, it fills B's dark bands through the soffit.
11. **Not rendered:** the reset; the stage-1 warning flicker; the WebGL tier (P2).

---

## 6. Images

- Shots: `images/run_dir_emergency_power_{s1a, s1b, s2, s3, c1, c2, c3, c4}.jpg` (the same as `images/run_emergency_power_<shot>.jpg`): glossy, final round r8.
- Derived: `…_{s1a, s1b, s2, c4}_grey.jpg`, `…_{s1b, c4}_protan.jpg`, `…_sheet.jpg`.
- Extras: `…_{x_unit_armed, x_unit_tripped, x_armed_unit, x_goal, x_pull, x_wrap, r_cart, r_truck}.jpg`.
- Matte versions: `images/run_emergency_power_{s1a, s1b, s2, s3, c1, c3, c4, x_goal, r_cart, r_truck}_matte.jpg` (same build, r8, matte floor).
- Verification slides (Figma section `2595:6093`, placed 2026-10-08 01:3x):
  - VL194 `2855:6151` Emergency Power bars (PARTIAL): `…_sheet.jpg`, `…_s1b_grey.jpg`, `…_c4_protan.jpg`, `…_v_readdist.jpg`;
  - VL195 `2855:6177` Box probes reach the floor (PASS): `…_v_reflections.jpg`, `…_v_probefix.jpg`.

---

## 7. Resume note (2026-10-07 23:2x → 10-08 01:3x)

The previous stage stopped at 20:08 on the usage limit while r3 was rendering (r3 has the 13 shots but no stats, timeline or sweeps). The resumed stage:
- found no apply package for this track under `W/apply/` (a pre-render; nothing is merged) and no codex-audit finding for it;
- kept the clone (made 10-07 16:3x from main) and its r3 state, measured r3, then ran r4–r8 (§1.1);
- read every round's frames before the next change;
- wrote this folder's reports and images, claimed VL194–195, built and uploaded the two slides, updated the cover and recorded the rows.
