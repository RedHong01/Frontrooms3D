# EXIT sign: build (model, textures, materials)

Date: 2026-10-07, 18:1x–19:3x. Status: **BUILD DONE, round 3 verified in the clone.** Integrate, critic and merge follow (`exit-sign` workflow).
Spec: `10_spec.md`. Clone: `W/proj_exitsign` (W = `/Users/redwang/FrontRoomsVisualWork`). Nothing was written into Red's project outside `Documentation/research/exit_sign/` and `Documentation/VERIFICATION_LOG.md` (rows VL145–147).
Images: `images/build/es_b01`–`es_b10`. Code copies and diffs: `build/code/`, `build/diffs/`. Figma: VL145–147 (`2804:6151`, `2804:6175`, `2804:6196`).

---

## 0. The short version

1. **Four mounts, eight assets, one module family.** `Kit_ExitSign` (wall) and `Kit_ExitSign_Hanging` (double face, 0.40 m drop) are P1. `Kit_ExitSign_HangingFlush` and `Kit_ExitSign_End` are P2. Each has a `_Dead` variant. The face is a real stencil: a 0.9 mm plate with EXIT cut through it (6 in letters, 3/4 in strokes), a white spacer and a red diffuser 6 mm behind it.
2. **Budgets met.** LOD0 / LOD1 triangles: wall 3,136 / 984; hanging 6,502 / 1,999; flush 6,254 / 1,940; end 5,758 / 1,822 (spec 3,500 / 6,500 / 5,800 / 6,000 ± 15 %). `lodDistances` `[5, 0, 0]`: LOD1 never culls.
3. **No mirrors.** New surfaces `ExitSign_Housing`, `ExitSign_Face`, `ExitSign_Diffuser`, `ExitSign_Diffuser_Dead` take smoothness from masks (means 0.50 / 0.41 / 0.54). `Run_ExitSign` goes from 1.0 to 0.38. Emission only on the lit diffuser, shaped by a lamp-field map under a new keyword `_FR_LAMP_FIELD`.
4. **Acceptance in Unity (round 3):** T1 mirror PASS; T2 lit level 1.87 × the lit wall (bar 1.5–2.5) PASS; T3 dead contrast 0.88 (UL ≥ 0.5) PASS; T4 hot spots E/X/T 0.50 / 0.59 / 0.56 against NIST 0.54 / 0.65 / 0.53, all within 0.08, PASS; T6 hero close-up PASS; T7 distance PASS (12 m is soft but readable).
5. **Round 3 fixed a shadow leak.** Shadow-map rays slipped through the 0.4 mm frame seam and crossed the open interior. A closed 12-triangle lamp chassis and an inward diffuser back now make every sign a solid shadow caster (VL146).
6. **Merge is ready as a file list plus two diffs** (§7). Both diffs apply cleanly to main at `3ff05ee`, including the Q1b print change in `FrontRoomsRenderSetup.cs`. Main's four GUIDs for `Kit_ExitSign(_Dead).fbx/.json` are kept.

---

## 1. Inputs, recovery and the state of main

- **This stage ran three times.** 2026-10-04 (two build agents, files lost with the 2026-10-05 wipe of `/private/tmp`), 2026-10-07 16:30–17:46 (stopped by the usage limit while Unity round 2 ran), and now. I continued from the 10-07 attempt; nothing was redone from scratch.
- **Recovered by transcript replay** (RECOVERY rule 4b, `W/exitsign_recover/`): `exit_sign_geom.py`, `exit_sign_common.py`, the four asset modules, `gen_exit_sign.py`, `exit_sign_measure.py`, `FrontRoomsExitSignLookdev.cs`, `es_preview.py`, and the RenderSetup and shader patches. The 10-07 attempt re-ran every verification on the recovered files (rounds R1 and R2, §3), so nothing rests on a verdict from the lost files.
- **Clone:** `cp -Rc W/proj_audit W/proj_exitsign`, then `rsync` from main (iCloud copies `* [0-9].*` excluded). `apply_local_patches.py` found nothing to do (main compiles for desktop). Every Unity run used `-buildTarget OSXUniversal`.
- **Base:** main at `16f520e` (recorded in `W/exitsign_base/`, 15 files with SHA-1; copy in `build/logs/`). At 19:3x main is at `3ff05ee`. Fourteen of the 15 base files are unchanged. The one that moved is `FrontRoomsRenderSetup.cs`: the Q1b print stage changed its texture importer (`FrontRoomsPrintArray`, committed in `3ff05ee`). My diff is against the base and still applies cleanly to main's current file (`patch --dry-run`, 19:3x). The clone does not contain Q1b, so the merge stage must compile and re-run a short look-dev on a fresh clone of current main (§7).
- **No scipy in `W/venv`.** The generator and the measure script use small numpy stand-ins for `ndimage.binary_erosion/dilation` and the blur (checked: the wrap/reflect blur matches to 6e-8).
- **codex-audit** (`20_findings.md` does not exist; read `00_main_state.md` and `10_review_kits.md`):
  - F4 (minor): `Kit_ExitSign_Dead` points at `Run_ExitSign_Dead`, which has no material. **Resolved:** the rebuilt `_Dead` uses `ExitSign_Diffuser_Dead` (a real material). No sidecar references `Run_ExitSign*` any more.
  - The `null` in `lodDistances` (read as 0 by JsonUtility): **resolved**, the sidecars now say `[5.0, 0.0, 0.0]`.
  - §3.7 importer LOD mapping: fixed in main (`663e858`, `1114d6b`). With 2 LODs and `[5, 0, 0]`, LOD0 → LOD1 at 5 m and the last LOD takes the last entry, 0 = never cull. The clone runs main's fixed importer.
  - §3.3 name clash `Kit_ExitSign*`: resolved by keeping the names and main's GUIDs.

---

## 2. What was built

### 2.1 Blender modules (`Tools/Blender/frontrooms_kit/assets/`, built with the clone's `build_asset.py`)

| File | What |
|---|---|
| `exit_sign_geom.py` (new, no bpy) | Every number: housing, frame, plate, letters, chevrons, screws, lamp field. The texture generator and the measure script import the same file, so holes and print cannot drift apart |
| `exit_sign_common.py` (new) | Shared construction: face frame (profile with roll, bead, lip; UVs unfolded by arc length), stencil plate (through-holes, 0.2 chamfer, R 0.5 stamped reflex corners, cut walls facing into each hole), spacer, diffuser, **diffuser back** and **lamp chassis** (round 3), wall pan with keyholes, double-face ring, canopy, stems/collars/nuts/washers, screws, knockout, sidecar `exitSign` block, budget check |
| `interact_exit_sign.py` (rewritten; `NAME = "Kit_ExitSign"`) | Wall sign, VARIANT `Kit_ExitSign_Dead` |
| `exit_sign_hanging.py` (new) | `Kit_ExitSign_Hanging` (+ `_Dead`) |
| `exit_sign_hanging_flush.py` (new) | `Kit_ExitSign_HangingFlush` (+ `_Dead`): the hanging build with a 25 mm drop, no collars |
| `exit_sign_end.py` (new) | `Kit_ExitSign_End` (+ `_Dead`): flag mount, faces ±X |

`kitlib.py` is untouched. The two hidden parts are kept whole in LOD1 by giving them the vertex group `fr_lod_keep`, which kitlib already uses for thin and decal parts (the join merges groups by name).

Build command (used for round 3):
```
cd W/es_work && /Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup \
  --python-expr "import sys; sys.dont_write_bytecode = True" \
  --python W/proj_exitsign/Tools/Blender/frontrooms_kit/build_asset.py -- \
  interact_exit_sign exit_sign_hanging exit_sign_hanging_flush exit_sign_end --out-root W/proj_exitsign --no-preview
```
The clone's `build_asset.py` is byte-identical to main's; it is used so that the clone's new modules are found. Previews: the same command with `--out-root W/es_work/r3/prevroot --preview-dir W/exitsign_prev`.

### 2.2 The assets (`Assets/Resources/Props/Models/`)

| Asset | Mount, origin | LOD0 / LOD1 tris | Tags → placement | Key anchors (Unity m) |
|---|---|---|---|---|
| `Kit_ExitSign` (main GUIDs `3892f75e…` / `112bc21d…`) | wall; bottom-centre of the back plane, +Z = room | 3,136 / 984 | `exit_sign`, `wall_decor` → Wall | `hang` (0,0,0), `face` (0, .106, .0595), `diffuser` z .0526, `lamp_a` (+.0544, .1432, .03), `lamp_b` (−.048, .1432, .03), `glow` (0, −.02, .06), `mount_l/_r` (±.0445, .14, 0) |
| `Kit_ExitSign_Dead` (main GUIDs `70298f9a…` / `cf96d527…`) | same | same | same | same |
| `Kit_ExitSign_Hanging` (+ `_Dead`) | ceiling plane; sign hangs to −Y; faces ±Z; drop 0.400 (housing 2.288–2.500 under 2.9 m) | 6,502 / 1,999 | `exit_sign`, `ceiling` → Ceiling | `centre` (0, −.506, 0), `face_front/back` z ±.035, `stem_l/_r`, `lamp_a/_b`, `glow` (0, −.94, 0) = world 1.96 m |
| `Kit_ExitSign_HangingFlush` (+ `_Dead`) | ceiling; drop 0.025 (for Low 2.4 m) | 6,254 / 1,940 | same | `glow` (0, −.44, 0) = world 1.96 m |
| `Kit_ExitSign_End` (+ `_Dead`) | wall plane at the housing bottom; projects along +Z to 0.361; faces ±X | 5,758 / 1,822 | `exit_sign`, `wall_decor` → Wall | `centre` (0, .106, .188), `face_pos_x/_neg_x`, `lamp_a/_b`, `glow` |

- Every asset: `kit.no_collider()`, 4 slots `ExitSign_Housing`, `ExitSign_Face`, `ExitSign_Diffuser` (or `_Dead`), `Prop_Chrome`; `lodDistances` `[5.0, 0.0, 0.0]`; `exitSign` sidecar block (faces, opening 0.320 × 0.186, letter 0.1524, stroke 0.01905, slots, chevrons `closed`, the lamp field).
- Names: no part, asset or anchor-bearing object name contains wall, floor, carpet, ceiling, seal or partition (round 3 renamed the End's `wall plate` part to `mount plate`). In the FBX the only children are `<NAME>_LOD0` and `<NAME>_LOD1`.
- Normals: Blender loop-normal check, 0 bad loops on all 8 LODs (`W/es_work/r3/lodcheck_r3.txt`). Unity probe: 0 NaN tangents and 0 tangents within 60° of the normal on all 76 submeshes (`W/es_work/unity_r3/probe.txt`).

### 2.3 Textures (`Assets/Resources/Surfaces/Textures/`, generated by `Tools/lookdev/gen_exit_sign.py`, numpy + Pillow, fixed seeds)

| Stem | Size | Content |
|---|---|---|
| `ExitSign_Face_A / _N / _S` | 2048 × 1024 (5.9 × 4.8 px/mm) | White enamel (230, 226, 212), heat browning over the lamps, dust on the lower lip, 10 fingerprints round the screws, 3 chips, 2 dents; scored closed chevrons with knockout tabs; bead and border lines on the geometry. `_S` R mean 0.41 (max 0.60), G cavity |
| `ExitSign_Diffuser_A / _S / _E / _F` | 1024 × 512 (3.2 px/mm) | Red acrylic (108, 41, 37) with frosted mottle, 8 dead insects along the bottom (3 visible through the holes, 5 behind the plate), dust gradient. `_S` R 0.55 (0.40 under dust), G cavity 0.69. `_E` = LIT field × red. `_F` linear: R lamp A, G lamp B, B DC pair, A transmission |
| `ExitSign_Enamel_A / _N / _S` | 1024², tiling 0.40 m | White enamel with orange peel; `_S` R mean 0.50, capped at 0.60 in round 3 |

- Lamp field (LIT, on the texture): E/X/I/T 0.51 / 0.59 / 1 / 0.58; spread 0.54–0.66; max/min 9.2. HALF (A out): 0.10 / 0.17 / 0.89 / 0.48. BATTERY: 0.17 / 0.05 / 0.05 / 0.14.
- GPU memory (BC7/BC5 with mips): about 14.7 MB for the whole family.
- Import: decals (`ExitSign_Face*`, `ExitSign_Diffuser*`) clamp; `_F` and `_S` linear; `_N` normal maps (RenderSetup's importer rules).

### 2.4 Materials and shader (clone; merged by diff, §7)

- `FrontRoomsRenderSetup.cs`: `SurfaceDef.lampField`; four SurfaceDefs (Housing tile 0.40, metre UVs; Face 0.346 × 0.212 decal; Diffuser 0.320 × 0.186 decal, emission `(3.2, .013, .006)`, `lampField = "ExitSign_Diffuser_F"`; Diffuser_Dead with no emission); `Run_ExitSign` `smooth = .38f`; `EnsureSurfaces` sets `_LampFieldMap`, `_LampField (1,1,0,0)` and the keyword only where `lampField` is set; the importer clamps `ExitSign_Face*` and `ExitSign_Diffuser*`.
- `FrontRoomsSurface.shader`: `[Toggle(_FR_LAMP_FIELD)] _UseLampField`, `_LampFieldMap` (own sampler: sharing `sampler_EmissionMap` failed on Metal, because the keyword path strips `_EmissionMap`), `_LampField` in `UnityPerMaterial` (SRP batcher layout identical in every pass), `#pragma shader_feature_local_fragment _FR_LAMP_FIELD`; emission = `dot(field.rgb, _LampField.rgb) × field.a × _EmissionColor`. With the keyword off, the shading code is unchanged; the CBUFFER grows by one float4.
- Generated materials (clone, Unity 6000.3.10f1): `ExitSign_Housing.mat` (`b9104065…`), `ExitSign_Face.mat` (`283ad3e8…`), `ExitSign_Diffuser.mat` (`9a5638b0…`, `_EMISSION` + `_FR_LAMP_FIELD`), `ExitSign_Diffuser_Dead.mat` (`5f884d2f…`, no emission). Look-dev log confirms each one's mask, keywords and tile.

---

## 3. Rounds

| Round | When | What was checked | Found | Fixed |
|---|---|---|---|---|
| 10-04 b1–u2 (lost, replayed) | 2026-10-04 05:4x–08:3x | Blender previews, first Unity captures | Metal HLSLcc error: the lamp field shared `sampler_EmissionMap`; at diffuser smoothness 0.72 a DEAD sign seen from below showed the troffer highlight across whole letters | own sampler; frosted diffuser 0.55 (0.40 under dust) + cavity occlusion |
| R1 | 10-07 16:39–17:20 | Rebuild from the replayed files; textures; Blender textured previews; Unity look-dev 30 shots + probe | Unity probe: 152 face vertices with a tangent within 60° of the normal (frame skirt/lip and hole walls shared one UV across depth) | — |
| R2 | 10-07 17:38–17:47 | Same set + SSAO attribution shot + LOD0/LOD1 A/B shots | tangents clean; T2–T4 pass; **shadow dashes** on the Run ceiling and lines under the wall sign; dark pan underside (cause unknown) | frame and plate UVs unfolded by arc length (`frame_uv_insets`, plate `uv_off`) |
| **R3** | 10-07 18:5x–19:2x | Rebuild + Unity look-dev 33 shots + probe + Blender normal check | shadow solid; underside = room light (plain box identical); enamel smoothness max 0.655 on 0.06 % of texels | lamp chassis + diffuser back; enamel `_S` capped at 0.60; `wall plate` → `mount plate` |

Look-dev tool: `Assets/Editor/Rendering/FrontRoomsExitSignLookdev.cs` (clone-only; copy in `build/code/`). It builds a 7 × 8 m Lobby room (lit sign, dead sign, today's code-built Run sign, troffers 1.25 m off the wall), a 3 × 34 m Run corridor (two hanging signs, RoomStream's red lights at 1.96 m, one in three troffers lit) and a line-up room. Raw HDR shots (no post) feed `Tools/lookdev/exit_sign_measure.py`, which projects the letter polygons of `exit_sign_geom.py` into the image.
Command: `W/es_work/run_unity.sh r3` (look-dev, then `FrontRoomsExitSignProbe.Run` with `-nographics`).

---

## 4. Acceptance (spec §12)

| # | Test | Result | Evidence |
|---|---|---|---|
| T1 | Mirror | **PASS.** Effective smoothness (mask R × 1.0): face mean 0.41 / max 0.60; housing 0.50 / 0.60; diffuser 0.54 / 0.55. `Run_ExitSign` reads 0.38. A DEAD sign 1 m under a lit troffer shows no troffer image; today's face at 1.0 shows a hot blob, at 0.38 it is gone | `es_b03`, VL145 |
| T2 | Lit level, Lobby, 3 m | **PASS.** Letter mean 0.409 ÷ lit wall 0.219 = **1.87** (bar 1.5–2.5). In the graded frame the letters read salmon-red (tonemap); no bloom at this level | `es_b02` last tile |
| T3 | Unlit contrast (UL 924) | **PASS.** (0.694 − 0.083) ÷ 0.694 = **0.88** (bar ≥ 0.5) | `es_b03` |
| T4 | Hot spots | **PASS.** E/X/I/T 0.495 / 0.587 / 1 / 0.562 vs 0.544 / 0.651 / 1 / 0.531 (worst X, −0.064; bar ±0.08); spread 0.52–0.64 (bar 0.4–0.75); max/min 8.99 (UL ≤ 40) | `unity_r3/measure.json` |
| T5 | Photosafety | not this stage (the driver is integrate's) | — |
| T6 | Hero 0.3 m, front and 35° | **PASS.** Frame seam, bead, screw slots, hole walls with chamfer, the diffuser moving behind the holes, the white spacer at grazing angles, insects at the bottom edge; no z-fighting, no stretch | `es_b01` |
| T7 | Distance in a Run room | **PASS.** 6 m legible; 12 m soft but readable (letters 8.8 px through the post stack); 30 m a red bar; never culled | `es_b04` |
| T8 | Regression | not this stage (integrate) | — |
| T9 | Era | Ready for critic: no brand, no LED, no date, no running man; built stencil legend, red; both chevrons closed | `es_b01`, `es_b10` |
| T10 | RT | not this stage (request to the RT owner, spec §7.3) | — |
| T11 | Names | Assets and parts clean; spawned labels are integrate's | §2.2 |

Extra checks: shadow solid (VL146); LOD0 vs LOD1 at 5.5 m / 6 m only lose the screws (`es_b07`); every state renders through the keyword (LIT, HALF, LOOSE frame, BATTERY, DEAD; `es_b02`); hanging back face reads correctly and HALF is the same physical lamp from both sides (`es_b09`).

---

## 5. Findings

1. **Shadow leak through the seam (fixed, VL146).** The ShadowCaster pass culls back faces. The interior had no surface facing outward from inside (the plate has no back, the diffuser was one outward quad), so a ray that slipped through the 0.4 mm frame seam crossed the whole sign. Under the Run room's red light (0.33 m below the sign) the ceiling shadow showed two rows of light dashes; on the wall under the Lobby sign it showed thin lines. Fix: a closed lamp chassis (12 tris, inset 8 mm from the pan, 2 mm behind each diffuser) and an inward-facing diffuser back (2 tris per face), both on `ExitSign_Housing`, both kept in LOD1. The 2 mm gap keeps the chassis out of a depth fight with the diffuser at distance.
2. **The dark underside is the room, not the sign (VL147).** A plain box of the same size and material in the same place reads (32, 28, 14) against the sign's (32, 27, 14). Down-facing surfaces get only the ambient ground colour, and SSAO takes about a third of that (R2 noSSAO shot: 61 vs 40). Owner: the look (`FrontRoomsLook` ambient), if Red wants brighter undersides on every prop.
3. **Faint horizontal lines on the wall under the wall sign remain** after the fix. They are the sign's shadow edges from the eight troffers at different distances (multi-light penumbra), not a leak. Not an asset issue.
4. **At 3 m in a lit room the LIT letters read salmon-red, not saturated red** (graded frame; raw HDR is pure red). That is the tonemap on a bright saturated red, the way a camera sees a real sign. Q15 (tonemap highlight shoulder) is where this would change.
5. **The kit pipeline's generic preview frames ceiling-origin assets badly** (the hanging and flush signs fall below its floor; `W/exitsign_prev/Kit_ExitSign_Hanging_*.png` show an empty stage). The textured Blender previews (`W/es_work/prev_r1`, `b1_sheet.jpg`) and the Unity captures cover them. Pipeline note, not a defect of the assets.

---

## 6. Deviations from the spec

| Item | Spec | Built | Why |
|---|---|---|---|
| Canopy (hanging) | 127 × 70 × 16 | **250** × 70 × 16 | A 127 mm canopy cannot reach two stems 210 mm apart |
| Bead | 4 × 1.2 at 6 from the edge | 3.5 × 1.0 at 8–11.5 | The Ø6.2 face screws need the outer 1.5–7.7 mm flat |
| Frame front | depth 64 | front flat at 62.5; bead 63.5; screw domes 64.0 | Same envelope, measured to the flat |
| Diffuser smoothness | 0.72 (0.45 dusty) | **0.55** (0.40 dusty), cavity G 0.69 | At 0.72 a DEAD sign seen from below showed the troffer across whole letters (10-04 round) |
| End stand-off | wall canopy 127 × 70 | stand-off 127 × **50** × 12 on a 70 × 150 × 3 plate | 70 is almost the 76 mm housing depth; 50 reads as a bracket |
| Enamel `_S` | mean 0.50 | mean 0.50, **capped 0.60** | T1 bar for the housing |
| Lamp chassis, diffuser back | — | added (14 tris per face pair) | Solid shadows (§5.1) |
| Door frame anchor `exit_sign_p` | move Y 2.280 → 2.180 | **not moved** | It lives in `interact_door_frame_steel.py:148` (not `interact_door_common.py`) and in the sidecars `Kit_DoorFrame_Steel.json` and `Kit_DoorFrame_Steel_Alu.json`, which the interactables track is still rebuilding. No code reads the anchor today. Handed to integrate (§8) |
| LOD2 | 200 / 300 tris | none | Pipeline item P-1 is not approved |

---

## 7. Merge manifest (for the merge stage; verified in the clone, nothing applied to main)

**Copy as files** (clone → main; every `.meta` with it unless noted):
1. `Tools/Blender/frontrooms_kit/assets/`: `exit_sign_geom.py`, `exit_sign_common.py`, `exit_sign_hanging.py`, `exit_sign_hanging_flush.py`, `exit_sign_end.py` (new); `interact_exit_sign.py` (replaces G3's; diff `build/diffs/interact_exit_sign.py.diff`).
2. `Tools/lookdev/gen_exit_sign.py`, `Tools/lookdev/exit_sign_measure.py` (new).
3. `Assets/Resources/Props/Models/`: `Kit_ExitSign.fbx/.json`, `Kit_ExitSign_Dead.fbx/.json` (replace; the clone's `.meta` files carry **main's GUIDs** and add the material remaps; or keep main's `.meta` and let the importer re-remap); `Kit_ExitSign_Hanging`, `_Hanging_Dead`, `_HangingFlush`, `_HangingFlush_Dead`, `_End`, `_End_Dead` (`.fbx`, `.json` and their new `.meta`).
4. `Assets/Resources/Surfaces/Textures/ExitSign_*.png` (10 files) + `.meta`.
5. `Assets/Resources/Surfaces/ExitSign_Housing.mat`, `ExitSign_Face.mat`, `ExitSign_Diffuser.mat`, `ExitSign_Diffuser_Dead.mat` + `.meta` (they reference the texture GUIDs of item 4 and main's shader GUID `38dd6d7d…`).

**Apply as diffs** (against base `16f520e`; `patch --dry-run` passes on main's current files at `3ff05ee`, 19:3x, which include the Q1b importer change):
6. `build/diffs/FrontRoomsRenderSetup.cs.diff` (4 hunks).
7. `build/diffs/FrontRoomsSurface.shader.diff` (5 hunks).

**Edit by hand:**
8. `Assets/Resources/Surfaces/Run_ExitSign.mat`: `_Smoothness: 1` → `_Smoothness: 0.38`, nothing else (the clone's regenerated copy also serialises default print and lamp-field properties; do not copy it).

**Do not merge:** every other `.mat` in the clone's `Resources/Surfaces` (EnsureSurfaces re-serialised them with default properties); `Assets/Editor/Rendering/FrontRoomsExitSignLookdev.cs` and `Assets/Editor/ExitSignProbe/` (clone-only tools; copies in `build/code/`); `Verification/`; `__pycache__`; any iCloud copy `* [0-9].*`; any file listed in `W/LOCAL_PATCHES.md`.

**Before merging:** re-check the 15 base hashes (`W/exitsign_base/SHA1SUMS.txt`; `FrontRoomsRenderSetup.cs` already differs because of Q1b, so apply the diff, never copy the clone's file), make a fresh clone of current main, apply items 1–8 there, compile with `-buildTarget OSXUniversal` and re-run `FrontRoomsExitSignLookdev.RunBatch` + `exit_sign_measure.py` (T2–T4 must match §4), read `codex_audit/20_findings.md` if it exists by then, and keep main's GUID for every path main already has.

---

## 8. Left for integrate, critic and merge

- **Integrate (visual chat files):** `FrontRoomsExitSign.cs` driver (spec §8.6); RoomStream Run block → two `Kit_ExitSign_Hanging` with the existing red lights at the `glow` anchor (world 1.96 m) and `SignOdds`; Exit room wall sign; `FrontRoomsKitLibrary.Spawn` attaches the driver for tag `exit_sign`; labels `"exit sign A/B"`, `"exit sign over door"`; T5, T8, T11.
- **Door anchor:** `interact_door_frame_steel.py:148` `exit_sign_p` (−0.080, 2.280, 0.500) → (−0.080, 2.180, 0.500) and the same Y in `Kit_DoorFrame_Steel.json` and `Kit_DoorFrame_Steel_Alu.json` (sidecar-only; no FBX change). Coordinate with the interactables track's rebuild.
- **RT (G14) owner:** classify `FrontRoomsExitSign` renderers as `EmissiveLens` and scale by `Level` (spec §7.3). Until then a DEAD or flickering sign glows in the traced reflection (T10).
- **平面视觉:** review the built legend (spec §5; `es_b10` shows the face texture, `es_b01` the cut plate).
- **WebGL track:** the keyword and one float4 in `UnityPerMaterial` compile on every platform; the WebGL track may strip the keyword. Nothing on desktop was lowered.
- **Red's calls** (spec §13): letter colour, options S1/O1, C-1, the shader keyword, media batch E1–E12.

---

## 9. Verification log

| VL | Node | Check | Verdict | Images |
|---|---|---|---|---|
| VL145 | `2804:6151` | EXIT sign family passes | PASS | `images/build/es_b01_hero_0p3m.jpg`, `es_b03_mirror_t1_t3.jpg`, `es_b02_states.jpg`, `es_b04_run_distance.jpg` |
| VL146 | `2804:6175` | Sign shadow is solid now | PASS | `images/build/es_b05_shadow_leak_r2_r3.jpg` |
| VL147 | `2804:6196` | Dark underside is the room | FINDING | `images/build/es_b06_underside_vs_plain_box.jpg` |

The section grew to 37 rows (45,960); the cover reads 21 tasks · 143 checks · 461 images, and its legend has a sixth row with `ES · VL145–147 · 3 checks`. Supporting images not on a slide: `es_b07_lod0_lod1.jpg`, `es_b08_family_lineup.jpg`, `es_b09_run_room.jpg`, `es_b10_textures.jpg`.

## 10. Where things are

- Report and images: this folder, `images/build/`, `build/code/` (`.py.txt`, `.cs.txt`), `build/diffs/`.
- Clone: `W/proj_exitsign`. Base copies: `W/exitsign_base/`. Work files: `W/es_work/` (`unity_r1–r3/`, `r3/`, `prev_r1/`, `tools/`), Blender kit previews `W/exitsign_prev/`, logs `W/logs/exitsign/`.
- Round-2 sources for comparison: `W/es_work/r2_src/`.
