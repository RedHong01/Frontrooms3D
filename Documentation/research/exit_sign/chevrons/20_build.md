# EXIT chevrons: build (`_ChevL` / `_ChevR` of the hanging signs)

Date: 2026-10-22, 07:49–08:3x. Status: **BUILD DONE, verified in the clone.** Package, critic and merge follow (workflow `exit-sign-chevrons`).
Spec: `10_spec.md` (this folder). Decision: Red, C23, 2026-10-22: "C23 按游戏视觉的两步方案：现在门上常亮一块，楼梯间做完后在出口区域里挂箭头吊牌". Red, 2026-10-22: start every planned step now, whatever it costs in days.
Clone: `W/proj_chev` (W = `/Users/redwang/FrontRoomsVisualWork`), made with `W/tools/mkclone.sh proj_chev --feature exit_sign` from main `0ea38bb7` + working tree at 07:49, compiled with `-buildTarget OSXUniversal`: 0 `error CS`.
Written into Red's project: only this folder (`20_build.md`, `images/ch_b01`–`ch_b07`, `threeview/`, `code/`, `diffs/`, `data/`). Nothing else in main changed. No git write, no download, nothing deleted.
**Every Unity run was UNDER LOAD** (1-min load 76–591, `data/runs.txt`); no timing below is a performance number.

---

## 0. The short version

1. **Four kits, built as the spec says.** `Kit_ExitSign_Hanging_ChevL`, `Kit_ExitSign_Hanging_ChevR`, `Kit_ExitSign_HangingFlush_ChevL`, `Kit_ExitSign_HangingFlush_ChevR`. The chevron is a punched knockout: the face texture's scored outline cut through the 0.9 mm plate with the letters' 0.2 chamfer and R 0.5 notch, plus three broken hold-tab stubs (1.5 × 0.35 mm) on the score's tab gaps. One physical end is open on both faces. No new material, texture, slot, shader, RenderSetup hunk or driver change.
2. **Geometry and budgets pass.** One open chevron per face, at the same physical end on both faces; hole vertices within 0.00003 mm of `hole_polys`; stubs on the texture's tab centres (0.000 mm); every cut wall faces into its hole; `_ChevR` = `_ChevL` turned 180° about Y (plate vertices identical). LOD0 / LOD1: Hanging 6,786 / 2,088 (parent 6,502 / 1,999, Δ 284), Flush 6,538 / 2,028 (parent 6,254 / 1,940, Δ 284), all inside the spec's caps. 0 bad loop normals on 8 LODs. The closed parents rebuild byte-identical (sidecars = main, mesh hashes = main's modules).
3. **Unity: the kit library attaches `FrontRoomsExitSign` to all four; every temperament reaches the chevron.** New test file `FrontRoomsExitSignChevronTests`: 6 / 6 PASS (sidecar contract, rays through LOD0 + LOD1 on both faces, the map's pick rule over 1,152 cases, spawn / LODs / materials / names, Lit / HalfA / HalfB / Loose / Battery / Dead with the property block on both LODs, the driver on all 12 exit-sign kits and no other). `FrontRoomsLevelDesignerTests` 140 / 140. `FrontRoomsExitSignTests` 12 / 13: **D3 counts exactly 8 Kit_ExitSign\* kits**; there are now 12 (behaviour correct). A one-line hunk, `diffs/FrontRoomsExitSignTests_D3.diff`, for the visual chat to land after the lit-lock edits of that file.
4. **The chevron's colour is right and the lamp states read on it** (CH-9 PASS): LIT 0.22 × the I and 0.43 × the E (field 0.42), graded body sRGB (134.5, 35.0, 21.8), hue 7.0°, saturation 0.84 (predicted (140, 36, 23), 6.8°); either HALF and LOOSE 0.48–0.52 of LIT; BATTERY 1.77 × the E, the brightest hole.
5. **The distance read by luminance fails as written, and F1 would not fix it** (CH-10, VL335 WAIT-RED). Under the game's ambient the white plate sits at raw Y 0.037 and the chevron at 0.078, so the spec's box ratio is 1.03–1.33 at the game's vFOV 76 (bars 5 / 3 / 2). F1, emulated, gives 1.23–1.63. The chevron reads by colour (red channel 1.5–2.6, ΔE76 2–12 at vFOV 76): its shape at 3 and 6 m, a red tick at 9 m, a hint at 12 m. Red's call: keep (default) or a heavier chevron through 平面视觉.
6. **LOD1 and shadow unchanged** (CH-12 PASS): the chevron is identical in LOD0 and LOD1 at 5.5 m; the sign's shadow differs from the closed sign's by at most 1/255 on 31 px (raw).
7. **Three-views** of the 4 kits and `threeview/index.json` in the closed signs' schema. VL specs VL333–VL336 in `W/vl_pending/exit_chevrons.md`.

---

## 1. What was built

### 1.1 Blender modules (clone copies under `Tools/Blender/frontrooms_kit/assets/`; copies in `code/blender/*.txt`, diffs in `diffs/`)

| File | Change | sha1 (clone) | Base sha1 (main) |
|---|---|---|---|
| `exit_sign_geom.py` | `TAB_EDGES (0, 2, 4)`, `TAB_W 1.5`, `TAB_PROUD 0.35`; `chevron_tab_stubs(side)` (edge, midpoint, direction, inward normal, the stub's 4 points); `_insert_stubs`; `hole_polys("left"/"right")` inserts the stubs after the fillet. `"closed"` output byte-identical | `b500b3cb9d9f8ffb01a596ddef04c1cd13a37248` | `40128b9413649e549dd0c56e8e97dff798d9f017` |
| `exit_sign_common.py` | `exit_meta(kit, faces, chevrons="closed", chevron=None)`: with `chevron` it adds `chevronBack`, `pointLocal`, `closedKit`, `mirrorKit`, `tabStubs` right after `chevrons`; without it the block is the closed signs' block, key order unchanged | `8d6a633a5e40375138e8f9a8fbee6b9371c95eeb` | `c1c666be1204ff91ad722c6e930293e05360bc7b` |
| `exit_sign_hanging.py` | `build_hanging(..., chevron=None)`; `CHEVRON_FACES = {"L": front left / back right / +X, "R": the mirror}`; the faces get their side; tag `exit_chevron`; the kit name must end in `_ChevL` / `_ChevR` (asserted); the budget check against the parent target is unchanged | `65edcabb3a89b7a736b324fe3463858fd5b19080` | `88dad4ece04209e3cfa44ddddd4037b97fe86f9d` |
| `exit_sign_hanging_chevl.py` (new) | `NAME = "Kit_ExitSign_Hanging_ChevL"`, `build_hanging(kit, DROP, BUDGET, chevron="L")`, the parent's BUDGET, LOD1 ratio, SMOOTH_ANGLE | `3fe92d75af5995743b2dd2fd41440099b1e59e6b` | — |
| `exit_sign_hanging_chevr.py` (new) | the same, `"R"` | `bbe11c8a0f9fd9b043aba21d50525ead79a04aec` | — |
| `exit_sign_hanging_flush_chevl.py` (new) | `Kit_ExitSign_HangingFlush_ChevL`: `FLUSH_DROP`, `collars=False`, `ceiling_m=2.4`, the flush parent's constants | `284e262a797893a51d654ff95dbbad3db63b16ec` | — |
| `exit_sign_hanging_flush_chevr.py` (new) | the same, `"R"` | `ca25320f9c99787aefaa8dbe3f32573e91516c65` | — |

`kitlib.py`, `build_asset.py`, `exit_sign_hanging_flush.py` and `Tools/lookdev/gen_exit_sign.py` are untouched. A material VARIANT cannot change a mesh, hence one module per kit.

Build: `Blender -b --factory-startup --python-expr "import sys; sys.dont_write_bytecode = True" --python W/chev_work/tools/chev_build.py -- --kit <kit dir> --out <dir> --report <json> <modules>` (`code/tools/chev_build.py.txt`). It runs `build_asset.py`'s exact per-module steps (build, finish, LOD1, export, variants) and also dumps mesh hashes and the plate parts before the join. The exported FBX and sidecars are byte-identical to what is in the clone.

### 1.2 Unity assets (`Assets/Resources/Props/Models/`, new `.meta` written by Unity; every GUID grepped against main's `Assets` and `ProjectSettings`: 0 hits)

| File | sha1 | GUID |
|---|---|---|
| `Kit_ExitSign_Hanging_ChevL.fbx` / `.json` | `26b46ba47d0f977f5dbb710d964ad2049a3a290d` / `e4f95cc43b11bbf833eff8bfad99d42f4039aac3` | `aba7dcad0f0ae4437a8aed266500b821` / `cfed1d2f2e65d46ada5ed59b6342da0e` |
| `Kit_ExitSign_Hanging_ChevR.fbx` / `.json` | `84c84b022aa8a902b3f9bba2ed7c43ba0ed0feef` / `ed2949de05d567a8950c10864f1c6c9e47dadeb2` | `beef9950124cb4a62a99f42d5d61b4ba` / `f4949daa51dd44374854226437e726a5` |
| `Kit_ExitSign_HangingFlush_ChevL.fbx` / `.json` | `b818f46ac8ffe5f51cd9263a3084469b8c94af53` / `1b24e710d7a117d728ef233083e3bb2bedf41645` | `6b61769a8936549e6b173258ef727d7f` / `a74e42f1cc8f14f8f817572abb6a8ae1` |
| `Kit_ExitSign_HangingFlush_ChevR.fbx` / `.json` | `19d3ed3820b90d8cb5f044163177ef4c0eff7b77` / `aae33a6c5c1dfb969a8c13c11ffb2dec254d32e3` | `a602af8c749e74818899d6f198defdcc` / `9a07b5475239f46e2b5cf286e0de9447` |

The `.fbx.meta` files carry the importer's 4 material remaps (ExitSign_Housing, ExitSign_Face, ExitSign_Diffuser, Prop_Chrome, main's material GUIDs). All `.meta` sha1s: `data/payload_SHA1SUMS.txt`.

Sidecar (`Kit_ExitSign_Hanging_ChevL.json`): tags `exit_sign`, `ceiling`, `exit_chevron` → placement `Ceiling`; `minCeiling` 2.65 (flush 2.27, the parents'); `lodDistances [5, 0, 0]`; slots the parent's 4; `noCollider`; anchors the parent's 9; `exitSign` = the parent's block plus `"chevrons": "left", "chevronBack": "right", "pointLocal": [1, 0, 0], "closedKit": "Kit_ExitSign_Hanging", "mirrorKit": "Kit_ExitSign_Hanging_ChevR", "tabStubs": 3`.

### 1.3 Editor files

| File | Role | sha1 | GUID |
|---|---|---|---|
| `Assets/Editor/Rendering/FrontRoomsExitSignChevronTests.cs` (+ `.meta`) | **mergeable** test. ASCII only, no C# tuples, no menu item, no InitializeOnLoad. `-executeMethod FrontRoomsExitSignChevronTests.RunBatch -quit [-escTestOut dir]`, default `Verification/exit_sign/chevrons/tests`. Uses only `Configure`, `Tick`, `SelfTick`, `State`, `Level`, `Weights`, `EmissionGain` of the driver (all at HEAD and in the lit-lock working tree) | `4e05ef2be36da34b55dd7f4f83440f76da3c6cbb` (meta `362605643098b584a78ac8ac9a84fb85670f65b4`) | `7de6aa69cf4134c2dabc03dd549752c4` |
| `Assets/Editor/Rendering/FrontRoomsExitSignChevronCapture.cs` | **clone-only** capture harness, from `../fix1/code/FrontRoomsExitSignFix1Capture.cs.txt` (copy in `code/unity/`) | — | `464225726384d4497a97aa7d056ffd49` (unused in main) |

**Not touched:** `FrontRoomsExitSign.cs`, `FrontRoomsExitSignTests.cs` (exit-sign-lit-lock tonight), `FrontRoomsRenderSetup.cs` (outlets_p4b, d16), the shader, every `.mat` and texture, RoomStream, map files.

---

## 2. Geometry (Blender) — CH-1, CH-2, CH-3

| # | Check | Result |
|---|---|---|
| CH-1 | Parents unchanged: `exit_sign_hanging` + `exit_sign_hanging_flush` (+ `_Dead`) exported with main's modules and with the edited ones | **PASS.** LOD0 / LOD1 mesh hashes (positions 1e-6, indices, UVs 1e-6, material indices and names) equal for all 4 assets; sidecars byte-identical to each other and to main's (`data/ch1_parents.txt`) |
| CH-2 | Geometry of the 4 kits | **PASS.** Per plate: 5 holes (E X I T + one chevron), the chevron loop has 20 points (6 + 2 from the R 0.5 fillet + 3 × 4 stub points); the front hole of `_ChevL` at local X +0.13191, the back one at the same end; `_ChevR` at −0.13191 on both. Hole vertices against `hole_polys(side)`: max 0.000027 mm (flush 0.000011); stub points max 0.000026 mm; stub base centre = the texture's tab centre (0.000 mm; reader (−127.06, −28.58), (−128.29, 14.29), (−139.36, 14.29) for ‹). 76 / 76 cut walls per plate face into their hole; 0 flat faces facing in. Plate 556 triangles (closed: 414). Loop normals: 0 bad of 13,350 / 6,264 / 12,902 / 6,084 loops (LOD0 / LOD1, `data/lodcheck_chev.txt`) |
| CH-3 | `_ChevR` vs `_ChevL` turned 180° about Y | **PASS.** Plate vertices (640 per kit) match exactly (max 0.0 m), hanging and flush |
| Budgets | §4.1 caps | **PASS.** Hanging 6,786 ≤ 6,802, LOD1 2,088 ≤ 2,100; Flush 6,538 ≤ 6,554, LOD1 2,028 ≤ 2,040; Δ 284 per sign (the spec's estimate, exactly) |

`data/ch2_ch3.json` holds every number; the checker is `code/tools/ch2_geometry.py.txt`.

The `offset_poly` risk of spec §8 did not happen: with the miter offset, the stub's support ring (−0.35 mm) becomes a 0.8 × 0.35 notch and the chamfer ring (−0.2 mm) a 1.1 × 0.35 notch; nothing folds.

---

## 3. Unity — import, attach, temperaments, regressions (CH-4 … CH-7, CH-13)

`FrontRoomsExitSignChevronTests` (run `t05`, `data/tests_chevron.json`): **6 / 6 PASS**.

| # | Test | Result |
|---|---|---|
| CH-4 | Sidecar contract | PASS. All 4: tags exactly {exit_sign, ceiling, exit_chevron}, Ceiling, minCeiling = the closed parent's (2.65 / 2.27), lodDistances [5, 0, 0], the parent's 4 slots, no collider, front / back / pointLocal / closedKit / mirrorKit / tabStubs consistent with the kit and its mirror. None of the 4 in the Level Designer palette (118 kits listed) |
| CH-5 | Rays through the imported LOD0 and LOD1 meshes, from 0.5 m off each face, at 3 stroke points of each chevron and at the E | PASS, 112 rays. The open end hits `ExitSign_Diffuser` (front-facing), the closed end `ExitSign_Face`, the E the diffuser, on both faces of all 4 kits and both LODs. `_ChevL`: front (+Z) reads ‹EXIT, back EXIT›, both local +X; `_ChevR`: front EXIT›, back ‹EXIT, both local −X |
| CH-6 | The map's pick rule (spec §3.2 `ChevronKit`, a reference copy in the test) | PASS. 1,152 cases (yaw 0–345 step 15 × 8 directions × parent 0 / 90° × ceilings 2.9 / 2.4 / 2.1 m): 382 chevron kits, all pointing within 45° of the route; 770 none (straight-through or too low); the §3.2 table reproduced exactly; a spawned `_ChevL` under a parent at 90° and yaw 30° points where the rule says |
| CH-7 | Spawn | PASS. `FrontRoomsExitSign` attached; 2-level LODGroup, LOD0 → LOD1 at screen height 0.0783 (flush 0.0443) = the parent's, LOD1 never culls (0); ExitSign_Diffuser on LOD0 and LOD1; the 4 real `.mat` materials (no FBX fallback); triangles = the sidecar's; object names clean (T11) |
| CH-7b | Temperaments through the driver on each kit | PASS. Lit (1, 1, 0) Level 1, no property block; HalfA (0, 1, 0) 0.541; HalfB (1, 0, 0) 0.459; Loose drop-out after 489 ticks (0.072, 1, 0) 0.575; Battery (0, 0, 0.25) 0.166; Dead (0, 0, 0) 0. Every non-Lit state writes `_LampField = Weights × EmissionGain` on both diffuser slots (LOD0, LOD1), so the chevron, which is the same diffuser, follows; gain 0.7 checked |
| CH-13a | The driver on every `Kit_ExitSign*` kit and no other | PASS. 12 exit-sign kits get it, 133 other kits none |

**CH-13 regressions** (same clone):
- `FrontRoomsLevelDesignerTests`: **140 / 140 PASS**; "palette leaves out ceiling kits — 8 ceiling kits in the library, none in All" (was 4).
- `FrontRoomsExitSignTests` (main's working-tree file, with the lit-lock G3): **12 / 13**. G3 LIT lock and every other test pass. **D3 fails on a hard-coded count**: `Require(signs.Count == 8, …)` (line 337); there are now 12 `Kit_ExitSign*` kits, all with the driver, and 133 other kits without. That file belongs to exit-sign-lit-lock tonight, so it is not edited here. The fix is one line, `diffs/FrontRoomsExitSignTests_D3.diff` (`== 8` → `== 12`), made against main's current file (sha1 `4132f23c20ed2a47de5f886c071fff5831a25dc7`). CH-13a is the same check with the new count. **The visual chat must land that hunk with (or right after) the chevron package**, or D3 stays red.
- Roslyn (`W/tools/roslyn_check.py --all`): main's tree + the new test file, 0 errors, macOS and iOS defines; the clone tree, 0 errors on both.

---

## 4. Look-dev and measurements (CH-8 … CH-12)

### 4.1 Method

- **Harness:** `FrontRoomsExitSignChevronCapture` (clone-only), the fix1 capture path: graded frames into an ARGBHalf target with the harness's own sRGB encode (VL268), lens distortion held at 0 for the measured `_m` frames, the full game post for the `_g` deliverables; raw HDR with post off; an unlit ID pass. Edit mode, `-quit`. Glass RT was left on (no stall).
- **Rooms:** hall 5.4 × 28 m (Lobby surfaces, troffer grid) with `_ChevL` hung at z 14.4, yaw 0, midway between two troffer rows so both faces get the same light (`cap_r2`); the first run (`cap_r1`) had the sign 0.16 m behind a row, so its front face was lit and its back face in shade. Dark = troffer lights off and lenses unlit, the game's trilight ambient on, **never a sign light**. Line-up room 7 × 8 m; shadow room 4 × 4 m with one troffer 0.6 m off the sign.
- **Measure** (`code/tools/measure_chev.py.txt`, from fix1's `measure.py`): each diffuser pixel is assigned to the hole it is seen through (its ray meets the plate's front plane inside that hole polygon, reader frame of the face the camera is on). Body = the hole's pixels eroded 1 px (letters also minus the hot spots); hue / saturation of the mean graded sRGB. Contrast = (L_plate − L_chevron) / L_plate on relative luminance, plate = face pixels in the chevron's box + 4 mm, 2 px away from the hole; also the UL form |ΔL| / max(L). CH-10 boxes as the spec says: the projected chevron bounding boxes (both ends), dilated 2 px, letter pixels masked, mean raw Y; plus the red channel and ΔE76 of the boxes' graded means.
- **Fixed on the way:** the capture first wrote `camera.projectionMatrix` after clearing the target texture, so its aspect was the batch default 4:3 (fix1's harness does the same, see §6). The measure rebuilds the x scale from vFOV and 1920 × 1080; the harness now writes the matrices while the target is set.
- Verdicts: `code/tools/summary_chev.py.txt` → `data/summary.json`. Boards: `code/tools/boards_chev.py.txt`.

### 4.2 Results

| # | Test | Result |
|---|---|---|
| CH-8 | Hero 0.3 m, front and 35°: `_ChevL` both faces, `_ChevR` front, LIT and DEAD (`images/ch_b01_hero_knockout.jpg`) | **PASS.** The hole, its chamfer and cut wall, the diffuser moving behind the hole at 35°, the three stubs at vFOV 5 (on the arm edges where the score leaves its tabs); DEAD reads as a dark maroon hole like the letters. No z-fight, no light at the cut. The unused knockout stays a faint scored outline (texture) |
| CH-9 | Chevron light, dark, 1.5 m (`images/ch_b04_states.jpg`) | **PASS** (front / back): LIT chevron raw Y 0.0787 / 0.0777 = **0.217 / 0.236 × the I**, **0.428 / 0.498 × the E** (bar 0.35–0.55; field 0.42); graded body **sRGB (134.5, 35.0, 21.8), hue 7.0°, sat 0.84** (bar hue 0–9°, sat ≥ 0.80; predicted (140, 36, 23), 6.8°). HALF A 0.48 / 0.49, HALF B 0.52 / 0.52, LOOSE (lamp A out after 489 ticks) 0.52 of the LIT chevron (bar ≥ 0.4). BATTERY **1.77 / 1.70 × the E** (bar ≥ 1.5), the brightest hole (E 0.060, T 0.052, X / I 0.019, chevron 0.106). Max/min over all holes 8.9 (LIT), 13.0–17.3 (HALF, LOOSE) (bar ≤ 40) |
| CH-10 | Direction read: dark 3 / 6 / 9 / 12 m front and back; lit (`images/ch_b03_distance_ladder.jpg`) | **FAIL as written; F1 does not fix it.** See §4.3 |
| CH-11 | Two faces, one world: `_ChevL` at yaw 0 from +Z and −Z (`images/ch_b02_two_faces.jpg`) | **PASS.** The lit chevron's world hit is +0.134 m on X from both faces; image left from the front, image right from the back, and world +X projects to the same side in each frame. `_ChevR`: −0.134 m |
| CH-12 | LOD1 and shadow (`images/ch_b05_lod_shadow.jpg`) | **PASS.** Forced LOD0 vs LOD1 at 5.5 m, dark: chevron raw mean 0.0780 / 0.0780 (vFOV 14), 0.0791 / 0.0791 (vFOV 76). Shadow, closed `Kit_ExitSign_Hanging` vs `_ChevL` in the same pose: raw frames outside the sign (+3 px) differ on 31 of 2,058,892 px by at most 1/255 (all next to the silhouette, wall and ceiling: screen-space AO round the hole), the floor view on 0 px. Graded frames are not compared: their film grain changes per render (noise floor 2/255 between two identical scenes) |
| — | Lit-room lamp states (`ch_b04`, bottom row) | Every state renders through the chevron: LIT 0.080, HALF A 0.039, HALF B 0.042, LOOSE 0.042, BATTERY 0.108, DEAD 0.0015 (raw Y), hue 6.8–9.7° |
| — | Line-up with the closed signs (`images/ch_b06_lineup.jpg`) | The six hanging kits side by side, front and back, lit and dark: the chevron kits match the closed ones in every part except the punched end |

### 4.3 CH-10: the luminance direction read

| Dark, vFOV 76 | 3 m | 6 m | 9 m | 12 m |
|---|---|---|---|---|
| chevron pixels (front) | 32 | 10 | 4 | 3 |
| raw Y open / closed, front (bar) | 1.26 (≥ 5) | 1.33 (≥ 3) | 1.08 (≥ 2) | 1.14 |
| raw Y open / closed, back | 1.22 | 1.03 | 1.18 | 1.09 |
| red channel open / closed, front / back | 2.62 / 2.56 | 2.47 / 1.77 | 1.83 / 1.73 | 1.92 / 1.54 |
| ΔE76 open vs closed box, front / back | 11.8 / 12.2 | 7.8 / 8.5 | 11.0 / 1.8 | 7.1 / 5.3 |
| **F1 emulated** (gain 1.75), raw Y front / back | 1.62 / 1.58 | 1.63 / 1.23 | 1.28 / 1.32 | 1.35 / 1.21 |
| narrow-FOV frames (33–47 px chevron), raw Y / red | 1.36 / 3.42 | 1.35 / 3.30 | 1.35 / 3.28 | 1.34 / 3.18 |

- **Why it fails.** The bar assumed a black plate. In the game a "dark" room still has the trilight ambient (`FrontRoomsLook`, equator 0.26 / 0.24 / 0.17), so the white enamel plate sits at raw Y 0.037 and the chevron hole at 0.078. The box is 45 % hole, so even an infinitely sharp image gives 1.36. Reaching 5 would need a chevron of raw Y ≈ 0.40, brighter than the I (0.36). Rec. 709 Y also weighs red at 0.21: the chevron differs from the plate mainly in chroma.
- **F1 was tested before deciding**, without touching a texture: EmissionGain 1.75 puts the chevron at F1's target (0.35 × I; the letters brighten too and are masked out of these numbers). It moves the ratios from 1.03–1.33 to 1.21–1.63: still a FAIL at every distance. In a lit room between troffer rows it would push the chevron's luminance (0.064 → about 0.11) past the plate's (0.09), so the lit contrast would fall from 0.30 toward 0. **F1 is therefore not built** (the spec's rule: F1 only if it fixes the failure; its gates would also cost a shared-texture replace).
- **What the eye gets** (`ch_b03`): at 3 and 6 m the ‹ shape reads; at 9 m a red tick at the open end; at 12 m (3 px) a hint. That is the "short range, not a radar" of C23, and the spec's §2 screen-size prediction.
- **Lit rooms** (narrow-FOV frames, `data/summary.json` → `CH-10.lit`): chevron hue 7.0–7.4°, sat 0.83–0.84 everywhere (bar ≤ 10°). Luminance contrast depends on the light on the face: **0.79** with a lit face (the E in the same frames 0.54, UL form) → PASS; **0.30** with both faces between troffer rows (the E 0.35) → FAIL, the lit letters fail the same bar there; the shaded back face of `cap_r1`: plate 0.038 darker than the chevron 0.065, UL form 0.42 (the E 0.69). At vFOV 76 the 32-px chevron mixes with the plate (hue 10.2–12.6°).
- **Open for Red / 平面视觉 (VL335 WAIT-RED):** (a) keep as built, period-honest, reads by colour (recommended); (b) a heavier chevron (`CH_*` in `exit_sign_geom.py` and the score in every `ExitSign_Face_*`: a texture item, spec §2); (c) revise CH-10 to a chroma bar (for example the red-channel ratio). F1 is not an option that changes the result.

---

## 5. Three-views

`FrontRoomsThreeView -threeViewOnly <the 4 kits> -frGlassRT off` (run `v01`) → `threeview/png/` (16 PNGs + `manifest.json`) and `threeview/index.json` (schema `frontrooms.kit-sheets.interactables/1`, the same top-level keys, sheet keys and kit keys as `../threeview/index.json`, plus `minCeiling_m` and a `chevron` record per kit). Two sheets: hanging (`_ChevL` primary, `_ChevR` its mirror variant) at 500 px/m, flush at 2000 px/m.
- Checks: 16 px clear margin on every orthographic view; 0 magenta; 0 opaque black except 167 px on each flush front, the same pixels as the closed `Kit_ExitSign_HangingFlush` front (the dark gap under its canopy). 4 / 4 kits, none failed.
- Studio colours (no HDR, no post): open chevron #a90704 (mean of its red pixels on the flush `_ChevL` front), letters #e10e07; swatches as the closed kits (housing #e6e2d4, face #e1dccd, diffuser #6c0403, chrome #d8d8d8, glow #ff0d06).
- Board: `images/ch_b07_threeviews.jpg`. Builder: `code/tools/build_index_chev.py.txt`.

---

## 6. Deviations and notes

| Item | Asked | Built | Why |
|---|---|---|---|
| LOD2 | the stage text asked for "LOD0/1/2" | **LOD0 + LOD1, no LOD2** | Spec §4.1 (the accepted design): the sign family has no LOD2; a 300-triangle LOD2 cannot hold the stencil holes, and two signs in one room should switch alike. `lodDistances [5, 0, 0]`, never culled |
| Look-dev tool | `FrontRoomsExitSignLookdev -esShots` | `FrontRoomsExitSignChevronCapture` | Spec §6: the existing look-dev renders graded frames into ARGB32, which clips at 1.0 before bloom and ACES (VL268); the 60_fix1 method needs the HDR path. The new harness is the fix1 harness with the chevron rooms |
| CH-10 bars | luminance ratios 5 / 3 / 2 | measured, FAIL, reported with chroma numbers and the F1 emulation | §4.3 |
| Lit room | one lit room | two lightings (`cap_r1` face lit, `cap_r2` between rows) | the contrast depends on the light on the face; both are reported |
| Capture matrices | — | fixed | §4.1 |

---

## 7. Findings for other owners

1. **`FrontRoomsExitSignTests` D3 counts 8 exit-sign kits** (§3). One-line hunk `diffs/FrontRoomsExitSignTests_D3.diff` for the visual chat, after exit-sign-lit-lock lands its edits of that file.
2. **fix1's capture harness writes 4:3 matrices** (`../fix1/code/FrontRoomsExitSignFix1Capture.cs.txt`, `Raw()` writes the JSON after `cam.targetTexture = null`). Any measure that unprojects rays with them (fix1's `ring_mask`, `target_only`, `letter_split`) used an x scale 0.75 of the true one. Their ID-mask numbers (hue, saturation, contrast, raw letter means) are unaffected; the T2-Run ring and the letter split were drawn from x-compressed rays. Worth a re-measure if those numbers are reused.
3. **Graded frames carry per-render film grain** (2/255 between identical scenes). Pixel-identity gates must use raw frames.
4. **CH-10 as specified cannot pass under the game's ambient** for any period-plausible chevron (§4.3). The bar needs Red's or 平面视觉's call.

---

## 8. Merge manifest (for the package stage; nothing applied to main)

Staged byte-for-byte in `W/chev_work/payload/` (paths as in main; `data/payload_SHA1SUMS.txt`):
- **M** (3, base sha1s in §1.1, re-checked against main at 08:2x): `Tools/Blender/frontrooms_kit/assets/exit_sign_geom.py`, `exit_sign_common.py`, `exit_sign_hanging.py`.
- **A** (4 + 16 + 2): the 4 new modules; `Assets/Resources/Props/Models/Kit_ExitSign_Hanging(_Flush)_ChevL/_ChevR.{fbx,json}` + their 8 `.meta`; `Assets/Editor/Rendering/FrontRoomsExitSignChevronTests.cs` + `.meta`.
- **Hunk after lit-lock:** `diffs/FrontRoomsExitSignTests_D3.diff`.
- **Do not merge:** `FrontRoomsExitSignChevronCapture.cs` (clone-only), the W/tools files, `Verification/`, any `__pycache__`.
- **Before applying:** convention v2 package `W/apply/exit_sign_chevrons`, dry run first, `W/tools/rebase_apply.sh exit_sign_chevrons` on a base mismatch; after applying, Unity re-imports 4 FBX and compiles once; run `FrontRoomsExitSignChevronTests` and `FrontRoomsExitSignTests` (with the D3 hunk) on a fresh clone.
- **For the level-design chat** (after the merge, through the visual chat): the four names, the tag `exit_chevron`, `pointLocal` in the sidecar, the pick rule of spec §3.2 (the test holds a reference copy, `FrontRoomsExitSignChevronTests.ChevronKit`), spawn at `MapGrid.CeilingHeight(cell)` with `colliders: false`, Hanging ≥ 2.65 m, HangingFlush ≥ 2.27 m, no window or glass along `rot * pointLocal` before the route opening, temperament Lit 70 / Half 15 / Loose 15 through `Configure`, at most one LOOSE per room, no sign light.

---

## 9. Verification log (pending placement)

VL333–VL336, specs in `W/vl_pending/exit_chevrons.md`:
- **VL333** PASS — Chevron is a punched knockout (`ch_b01_hero_knockout.jpg`).
- **VL334** PASS — Both faces point the same way (`ch_b02_two_faces.jpg`, `ch01_two_face_rule.png`).
- **VL335** WAIT-RED — Chevron reads at 6 m (`ch_b03_distance_ladder.jpg`, `ch_b04_states.jpg`, `ch02_knockout_and_light.png`).
- **VL336** PASS — Chevrons keep LOD1 and shadow (`ch_b05_lod_shadow.jpg`, `ch_b07_threeviews.jpg`, `ch_b06_lineup.jpg`).

---

## 10. Where things are

- This folder: `20_build.md`, `images/ch_b01`–`ch_b07` (+ the spec's `ch01`, `ch02`), `threeview/`, `code/{blender,unity,tools}/` (`.txt` copies), `diffs/` (3 module diffs + the D3 hunk), `data/` (summary, measurements, test results, capture logs and shot lists, sha1 lists, run timings).
- Clone `W/proj_chev` (keep until the package is applied). Work dir `W/chev_work/`: `b1/` (Blender builds, base and edited), `cap_r1/`, `cap_r2/` (frames, raw HDR, IDs), `threeview/raw/`, `tests_r*/`, `reg_exitsign/`, `payload/`, `kit_base/` (main's kit, for CH-1), `tools/`. Logs `W/logs/chev/` (`c01_compile`, `t01`–`t05`, `k01`–`k02`, `v01`); mkclone log `W/logs/mkclone_proj_chev.log`.
