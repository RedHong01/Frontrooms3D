# 20 — Run! direction A · RED WARD: build log, costs, needs, open issues

Date: 2026-10-07. Companion to `11_run_dir_red_ward.md` (the values, the measurements, pass / fail).
Clone: `/Users/redwang/FrontRoomsVisualWork/proj_run_red_ward` (clone-only; nothing was promoted). A second APFS copy, `W/proj_run_red_ward_x`, ran parallel sweeps.
Red's project was not opened in Unity. Writes there: this folder (`research/room_visuals/`: reports, images, `harness/`) and the three rows VL166–168 in `Documentation/VERIFICATION_LOG.md`.

---

## 1. What was built

A clone-only Unity look-dev scene, built in code and rendered in batch (Unity 6000.3.10f1, `-buildTarget OSXUniversal`, Ultra quality, URP 17.3 Forward+, 4× MSAA):
- a Level 0 anteroom (3 × 2 cells), a 1 × 9 cell (27 m) Run corridor and a one-cell Level 0 stub behind the goal door, rooted at world (4992, 0, 4992);
- the hero object, a 1990 ceiling-hung double-faced EXIT sign, modelled in Blender (`run_exit_sign_hanging.py`, kitlib conventions, LOD0) and imported with the clone's `FrontRoomsKitImporter`;
- the period props as Blender kit models (stretcher, three ganged visitor chairs, folding wheelchair, IV pole, linen cart, the RN-X ward leaf with its wired lite, the wall EXIT sign), main's interactables kit for the frames, steel leaves, crossbar and lock trim;
- a deterministic state rig, `FrontRoomsRunLookdevRig.Evaluate(t)`: armed night switching, the one-step cut at t = 0, lens afterglow, the LED, the corridor ambient by SH, the haze colour, the eye adaptation and the reflection cube per state;
- the local post volume `FrontRoomsPost_Run_A` (priority 2);
- the measurement and export pipeline (ID-mask renders, greyscale and protan copies, contact sheet, trip sequence).

Run: `/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath W/proj_run_red_ward -buildTarget OSXUniversal -executeMethod FrontRoomsRunLookdev.RunBatch -runSeq -runDebug -quit -logFile <log>` (about 3–5 minutes on a loaded machine). Single shots: `-runShots s1b,c4`. Sweeps: `-runLook spotI,spotR,glowI,glowR,amb,ev`, `-runGlowI`, `-runSignBounce armed,tripped`, `-runZoneLinear`, `-runOutDir`. Analysis: `W/venv/bin/python harness/red_ward/tools/analyse_red_ward.py.txt <renders> <images dir> <json> --export` (copy it to a `.py` first).

### 1.1 Rounds

| Round | When | What changed | Result |
|---|---|---|---|
| m1–m5 | 2026-10-04 | First build in the old scratchpad clone: sign spot 3.6, eye adaptation +3, lens and probe sweeps | Lost in the 2026-10-05 reboot; recovered from the transcripts on 2026-10-07 (`W/wf/red_ward_v5/sim`) |
| v5-1 | 10-07 17:11 | Clone rebuilt from main, Blender modules re-run, first full render | Base |
| v5-2 | 17:34 | The brief's sign values (spot 2.4) with +3.585 EV; zone reflection cube instead of the box probes | Grey wall dashes gone; white rim on the sign faces; letters orange |
| v5-3 | 18:21 | Glow point raised above the housing; face hold 1.15 | Letters red again; the rim stayed |
| diag | 18:4x | Same close shot with the glow light off, then with reflections off | The rim goes with the glow light only (`v_rimdiag`) |
| v5-4 | 19:0x | Glow as an upward spot; armed sign bounce 0.2 vs 0.4 | Rim gone; armed face off-white at 0.4; one hard glow disk over each sign |
| v5-5 | 19:16 | Two out-aimed glow spots per sign | Ceiling lobes fore and aft of each face; clean faces |
| v5-6 (final) | 19:52 | Tripped ambient 0.25 and +3.2 EV (sweep against 0.15 / +3.585 and 0.35 / +3.0) | All A targets met; tripped mean equals armed; Relay 6.85 : 1 |

Every round's frames were read by eye before the next change.

---

## 2. Change list (clone-only)

| Path in the clone | Change |
|---|---|
| `Assets/Editor/Rendering/FrontRoomsRunLookdev.cs` | New. The harness (copy: `harness/FrontRoomsRunLookdev_red_ward.cs.txt`) |
| `Assets/Scripts/Rendering/FrontRoomsRunLookdevRig.cs` | New. The state rig (copy: `harness/red_ward/FrontRoomsRunLookdevRig_red_ward.cs.txt`) |
| `Assets/Resources/Rendering/FrontRoomsPost_Run_A.asset` | New. The Run A post profile (copy in `harness/red_ward/`) |
| `Tools/Blender/frontrooms_kit/assets/run_*.py` | New, 9 modules: `run_exit_sign_hanging`, `run_exit_sign_wall`, `run_door_leaf_ward_lite`, `run_stretcher`, `run_visitor_chairs`, `run_wheelchair`, `run_iv_pole`, `run_linen_cart`, `run_props_common` (copies in `harness/red_ward/blender/`) |
| `Assets/Resources/Props/Models/Kit_{ExitSign_Hanging, ExitSign_Wall, DoorLeaf_Ward_Lite, Stretcher, VisitorChair3, Wheelchair, IVPole, LinenCart}.fbx/.json/.meta` | New, imported by `FrontRoomsKitImporter` |
| `Assets/Resources/Surfaces/Textures/RunA_*` | New: `ExitFace_A/_E`, `Plate_{4-11, 4-13, STAFF, LINEN, QUIET}_A`, `WireGrid_A` (from `make_textures_red_ward.py`, TeX Gyre Heros Bold Condensed / Bold) |
| `Assets/RunLookdevProbes/*.exr` | New: 3 probes × 2 states (only the middle probe's pair is used, as the zone cube) |
| `Assets/Resources/Rendering/FrontRoomsSurface.shader` | Reflection-probe blending, box projection and atlas keywords added (diff in `harness/red_ward/`). Not needed by the final frames |
| `Assets/Settings/FrontRooms_URP.asset` | Reflection probe blending and box projection on (diff in `harness/red_ward/`). Not needed by the final frames |
| `Assets/Editor/Audit/*`, `Assets/Editor/Rendering/{FrontRoomsKitLookdev, FrontRoomsLookdevCapture, FrontRoomsHunterLookdev, …}` | The recovered capture tools from `W/tools` (W/TOOLS.md) |

Nothing here is an apply script: this direction is a pre-render for Red's pick.

---

## 3. Measured costs

| Item | Value |
|---|---|
| Lights in the corridor | Armed 18 (5 troffer spots, 4 sign spots, 8 glow spots, 1 wall-sign spot); tripped 13. The brief's estimate was 14 / 9: the extra 4 are the second glow spot per sign (unshadowed, range 2.2) |
| With the anteroom and the stub | Armed 25, tripped 20 |
| Shadowed lights | Armed 17 (6 anteroom, 5 troffers, 5 sign spots, 1 stub); tripped 12 |
| Draws per shot (estimate) | S1a 229 + ~1,200 shadow-caster draws; S1b 224 + ~751; S2 261; S3 157; C3 141; C4 207 |
| Tris in view | S1a 125,624; S1b 125,604; S2 127,026; C3 70,536; C4 98,204 |
| Corridor content | 191 renderers, 153,774 tris |
| New meshes, LOD0 / LOD1 tris | Hanging sign 2,212 / –; wall sign 836 / –; RN-X leaf 4,376 / 1,750; stretcher 7,568 / 3,404; chairs 5,904 / 2,656; wheelchair 8,816 / 3,526; IV pole 2,132 / –; linen cart 4,348 / 2,174 |
| Textures | Scene 54 textures, 491 MB (mostly Level 0 wallpaper maps). New Run A textures 32.4 MB uncompressed (two 1224 × 704 face maps 8.8 MB each, five plates 2.4–4.4 MB, wire grid 0.04 MB) |
| Per-frame extras | One post-exposure float on the Run volume; one SH set per state on the corridor renderers; a reflection-cube swap at the cut |

---

## 4. What a real implementation needs, from whom

**Map chat (关卡设计).** All exact proposals are in `10_run_directions.md` §8:
- CR-1, the 1 × 9 straight (module `Run_AlarmCorridor_1x9`, `ModulePlacement.Corridor`);
- CR-2, `ModuleTrigger.Alarm` armed 1.5 m in, plus `RoomReset`;
- CR-3, `ModuleFinish.Run`;
- CR-4, `ModuleLamp.Off` on all 9 cells, `ChunkBuilt` / `ChunkDropped`, `WarnStage` forwarded, and the `LampFx` layer skipping these cells;
- CR-5, placement rules;
- CR-6, the latching RN-X goal door (6a: a module edge, needs Red's OK; or 6b: a zone border), and the 4 locked RN-K side doors as never-unlockable door edges or non-opening props.
- New from this render: unshadowed map lamps leak light through the 0.16 m walls (D1). A Run corridor beside a Level 0 zone with unshadowed lamps would show warm stripes on its white walls. The map's lamp shadow policy, or a light-blocking rule for Run cells, should cover it.

**Sound chat (声音设计).**
- CR-S1, a hard-floor footstep surface (VCT).
- CR-S2, a hard-corridor reverb.
- CR-S3, `Run.TubesOut` (one contactor clunk, the hum drops out with the tubes) and `Run.Reset` (ballast strikes). The signs are silent.
- CR-S4, the rig's lights kept out of the fixture-hum voices.

**Interactables track.**
- RN-X: `Kit_DoorLeaf_Ward_Lite` (made clone-only here), the crossbar's latch, lever trim and bored latch on the far face, strike, and a real closer (a blockout here).

**Glass track.**
- `Glass_Wired` (a URP Lit stand-in plus a wire grid here).

**Exit-sign track** (`research/exit_sign/20_build.md`, VL145–147).
- That track has since built the 1990 EXIT sign family, including `Kit_ExitSign_Hanging` at 6,502 / 1,999 tris with a true stencil plate, a red diffuser and a lamp chassis. Production should use it, not this clone-only module.
- Carry over three findings from this render:
  - never light a sign with an unshadowed light at or near its housing (the rim, VL167);
  - the two out-aimed glow spots for the ceiling lobes;
  - the face hold and face bounce through the eye adaptation.

**Visual chat (production rig, `FrontRoomsRunRig`).**
- States: armed, tripping, tripped, reset (re-strike over 1.5 s with the stream ballast model; not rendered here).
- The eye-adaptation driver on the Run volume.
- The corridor ambient per state: the SH through a property block works in URP 17.3; a `_FR_AmbientScale` term in the Surface shader is the alternative.
- Zone reflection cubes, armed and tripped.
- The hero props at LOD0 / LOD1, a handrail kit and a smoke-detector kit.
- The WebGL tier gate (P2, not rendered).

**Relay rig (visual chat).**
- The game's prototype Relay, instanced into the look-dev scene, spans 4.2 m of bounds, and its limbs float apart (`c4_gamerig`). It reads 3.15 : 1 in A's red, under the 4 : 1 bar.
- Before A is judged on the real silhouette, the rig needs a check in the real scene.

**平面视觉.**
- The faces and plates use TeX Gyre Heros Bold Condensed ("EXIT", 152 mm cap) and Bold (plates). By the house rule, type goes through 平面视觉 before anything ships.

---

## 5. Open issues

1. **Goal box rule: PARTIAL.** The lite is the brightest object (0.852) and the only white, but the far goal box (34 × 70 px) averages 0.128. The near red wall pools are brighter on average (0.434). Options: a brighter next zone behind the lite, or accept the lite-as-brightest-object reading. Not tuned further: dimming the pools pushes the median under 0.015.
2. **IV pole: 1.67 : 1 on its own** (a thin chrome line against a red-lit wall). It is 2.23 : 1 with its wheelchair, its row in the brief. Option: a black-enamelled stand, or set it against the dark door frame.
3. **Eye adaptation and the ambient are deviations** (D4). Without them, the brief's light values give a black corridor (median 0.0005). Red should judge the adapted look.
4. **Two glow spots per sign** instead of one point (D2): +4 unshadowed lights.
5. **The sign's 3 mm face lip** shows a thin dashed shadow line at 1.9 m. It is invisible at play distances, and the exit-sign track's model replaces this sign anyway.
6. **The game Relay rig** reads 3.15 : 1 and looks broken in the empty scene (§4, Relay rig).
7. **Map lamp leak** through walls (D1; §4, map chat).
8. **No box-projected reflections** on FrontRooms/Surface in this build (D9). The waxed VCT reflects a corridor-centre cube, not box-aligned. Red's 10-03 decision (a glossy waxed floor with a box-projected probe per Run room) needs a Surface-shader box-projection path, or G14b RT.
9. **Protan.** Tripped S1b median 0.0112 under protan simulation: darker, still one white door and readable obstacles (Relay 9.79 : 1).
10. **The game's directional fill** ("Soft ambient direction" 0.16, audit F5) is absent here. If the game keeps it, it fills A's dark through the ceilings.
11. **Not rendered:** the reset; the stage-1 warning flicker; the WebGL tier (P2).

---

## 6. Images

- Shots: `images/run_dir_red_ward_{s1a, s1b, s2, s3, c1, c2, c3, c4}.jpg` (the same as `images/run_red_ward_<shot>.jpg`).
- Derived: `…_{s1a, s1b, s2, c4}_grey.jpg`, `…_{s1b, c4}_protan.jpg`, `…_sheet.jpg`.
- Extras: `…_s3x.jpg`, `…_s3x_armed.jpg`, `…_s2_tripped.jpg`.
- Verification slides (Figma section `2595:6093`, placed 2026-10-07 23:3x):
  - VL166 `2817:6157` Red Ward pre-render bars (PARTIAL): `…_sheet.jpg`, `…_s1b_grey.jpg`, `…_c4_protan.jpg`, `…_v_tripstrip.jpg`;
  - VL167 `2817:6173` Sign rim was the glow light (PASS): `…_v_signrim.jpg`, `…_v_rimdiag.jpg`;
  - VL168 `2817:6183` Tripped fill vs adaptation (PASS): `…_v_ambsweep.jpg`.

---

## 7. Resume note (2026-10-07 23:3x)

The stage stopped at about 20:05, after the reports and the three Figma frames, before the image upload. The resumed stage:
- checked the clone, the reports and the images (all from the final round v5-6, 19:52–19:59) and read the final frames again: nothing was half-written;
- found no apply package for this track under `W/apply/` (a pre-render; nothing is merged) and no codex-audit finding for it;
- uploaded the 7 images into the three half-built slides, checked each slot (one IMAGE fill, FIT, aspect within 0.5 px), took one screenshot per slide, updated the cover and recorded the rows.
No new renders were made.
