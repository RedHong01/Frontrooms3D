# 50 — Outlets P-4b + P-6 with ivory D: build, materials and measurements

**Status: BUILT AND MEASURED in the clone, 2026-10-21 21:49–22:14. Not landed.** Red picked ivory D on 2026-10-21 (40_final.md, top line). This stage rebased the P-4b + P-6 RenderSetup proposal onto main's current file with D, made the 17 slot materials through RenderSetup's own writer, re-imported the outlet kits and measured them the way 40_final.md §3.4 did.

- **The package** is 25 paths, staged in `W/p4b_d/payload/` (list with sha1s and main's base sha1s: `data/50_payload_manifest.txt`). On main as of 22:14 all 25 bases still match.
- **Landing** is the visual chat's step (claim in `LANDINGS.md` first).
- **Clone:** `W/proj_outlet_int`, re-synced from main with mkclone.sh's steps. Unity was never run on Frontrooms3D. In Red's project only this folder changed: this file, `data/50_*`, `images/50_*`.

`W` = `/Users/redwang/FrontRoomsVisualWork`.

**Load rule.** Every Unity run here ran at a 1-minute load of 160–385, so all of them were **under load**. This file gives no timing figure. Luma values and draw counts do not depend on load. Runs 1 and 2 gave the same luma values to 0.1.

---

## 0. Results in one table

| Check | Result |
|---|---|
| Re-sync + compile | Clone = main (`dfc0086` + today's working tree) + shared tools. Unity batch compile: **0 `error CS`**. Roslyn on main + the patched RenderSetup: 0 errors, macOS and iOS defines (re-run 22:14, after 关卡设计's 22:06 landing) |
| Rebase | v2 anchors all match main's RenderSetup `256cf3a4` (after exit_sign_fix1a/1b). v3 patch → `c2749a57` |
| Material generation | 7 new slots written by the writer, plus their `.meta`. The writer also rewrote the 10 flip slots with 17 extra default lines each: a **defect**, so they were **reverted**. The flag was then set alone (`Material.enableInstancing`): one line per file. Final diff: 24 files. Nothing else changed in `Resources/Surfaces` or `Resources/Rendering` |
| GUIDs | The 7 new `.meta` GUIDs are not referenced in main, and neither are the 10-14 proposal's old GUIDs |
| Kit reimport | 20 kits re-imported. **66 slot bindings, 0 left embedded**. 17 kit `.fbx.meta` files gained remaps (not in the package, §3) |
| Ivory D, Level 0, 0.30 m / eye height | **0.95 / 0.95** the paper (as built; the same as 10-14's override) |
| Ivory D, Office, 0.30 m / eye height | **0.91 / 0.93** the painted wall |
| Steel duplex 0.30 m / 4 m | 0.59 → **0.92** / 0.69 → **0.91** |
| Handy box + EMT 0.42 m / riser 4 m | 0.66 → **0.90** / 0.88 → **0.97** |
| Floor box sides vs carpet | 62 vs 63 → **88 vs 62** |
| Brown on brown | 0.38 (unchanged, as 10-14) |
| P-6 draws, Office view `office_pod` | Outlet draws (R3) 50 → **20** per pass. Frame: draws 4,590 → **4,530**, SetPass calls 1,059 → **1,085** |
| P-6 pixel gate (no outlets in the pixel) | **max 0/255** in both views (bit-identical). Frame with outlets off: max 0 over all 2,073,600 px |

---

## 1. What changed (the patch, v3)

`data/50_rendersetup_p4b_d_patch.py.txt`; diff against main `256cf3a4`: `data/50_rendersetup_p4b_d.256cf3a.diff.txt` (+32 / −11 lines).

### 1.1 Kept from v2

- The 7 SurfaceDefs (5 tint-only plastics and 2 outlet metals), after `Prop_Brass`.
- `instancing = true` on the 10 existing slots: LaminateBeige, PlasticBeige, PlasticBlack, Aluminium, Chrome, Brass, Ceramic, PlasticPutty, CeramicGlaze, AluminiumAnodised.
- The `SurfaceDef.instancing` field, and `m.enableInstancing = d.instancing` in the writer.

The metals are unchanged:
- `Prop_StainlessBrushed`: metallic .6, smooth .55, #C7C7C2;
- `Prop_Galvanized`: metallic .5, smooth .35, tint (.70, .71, .70).

The code comment says #B3B5B3; Unity rounds .70 × 255 = 178.5 to `B2` and prints #B2B5B2.

### 1.2 New in v3

**Ivory D:**
- `Prop_ThermosetIvory` = #A69F8C (.651 .624 .549);
- `Prop_NylonIvory` = #A39C89 (.639 .612 .537).

**Aged and Almond, derived from D as v2 derived them from A.** v2's slots relate to its ivory plate by fixed per-channel ratios in 8-bit sRGB:
- Aged / A: #CDBF98 / #E6DDC2 = .891 .864 .784 (darker and yellower);
- Almond / A: #E3D7BE / #E6DDC2 = .987 .973 .979.

D itself is A × 0.72 on every channel. The same ratios applied to D give:
- `Prop_ThermosetAged` = **#94896E** (.580 .537 .431);
- `Prop_ThermosetAlmond` = **#A49B89** (.643 .608 .537).

Doing the same in linear light changes Aged's blue by one code value (#94896D) and Almond not at all. `Prop_PlasticOrange` (the IG device) stays #D8642A. Smoothness and macro values are v2's.

**A name filter in `EnsureSurfaces(ICollection<string> only = null)`:**
- `null` writes every slot, so `SetUp()` behaves exactly as before.
- It lets a tool rewrite single slots without `SetUp`/`RunBatch`, which would also rewrite Red's tuned post profiles and the glass materials.
- This is the one addition beyond the proposal.

### 1.3 The 25 shipped paths (payload sha1 ← main base sha1)

| Path (`Assets/…`) | Payload | Main base |
|---|---|---|
| `Editor/Rendering/FrontRoomsRenderSetup.cs` | `c2749a57` | `256cf3a4` |
| `Resources/Surfaces/Prop_ThermosetIvory.mat` / `.meta` | `263bd65d` / `e017877d` | new (GUID `0ffa82a3…`) |
| `…/Prop_ThermosetAged.mat` / `.meta` | `4f42497a` / `ea8e6b96` | new (`6a917736…`) |
| `…/Prop_ThermosetAlmond.mat` / `.meta` | `c925619f` / `1d4d5caa` | new (`f37fb78f…`) |
| `…/Prop_NylonIvory.mat` / `.meta` | `f7d4d7a7` / `69926969` | new (`48f7037b…`) |
| `…/Prop_PlasticOrange.mat` / `.meta` | `a247f9b0` / `478c04f1` | new (`2085faad…`) |
| `…/Prop_StainlessBrushed.mat` / `.meta` | `6d9d2b29` / `f99016f3` | new (`abe1665b…`) |
| `…/Prop_Galvanized.mat` / `.meta` | `4f793fa7` / `c71752b8` | new (`e925192b…`) |
| `…/Prop_LaminateBeige.mat` | `e18992fd` | `75f0fa12` |
| `…/Prop_PlasticBeige.mat` | `401010b7` | `7681f76b` |
| `…/Prop_PlasticBlack.mat` | `e9f8b940` | `11d120a7` |
| `…/Prop_Aluminium.mat` | `1d0e8f72` | `b1f5ad6f` |
| `…/Prop_Chrome.mat` | `e418af61` | `9c72c17f` |
| `…/Prop_Brass.mat` | `c6a9b8b1` | `29e88fdb` |
| `…/Prop_Ceramic.mat` | `5591273f` | `8c7e7c16` |
| `…/Prop_PlasticPutty.mat` | `96dc3a98` | `e4137c59` |
| `…/Prop_CeramicGlaze.mat` | `aa8fc52b` | `1bcc9d0d` |
| `…/Prop_AluminiumAnodised.mat` | `951d9e78` | `edb6f01e` |

Each of the 10 flips is main's file with one line changed, `m_EnableInstancingVariants: 0` → `1` (`data/50_flips_final.diff.txt`). Main has no material with instancing on, so a later full `RunBatch` with this patch turns nothing off.

---

## 2. How the materials were made (snapshot log: `data/50_snapshot_log.txt`)

1. **Snapshot first.** sha1 of every file under the clone's `Resources/Surfaces`, `Resources/Rendering` and `Resources/Props/Models` (1,133 files). The lists are identical right after the rsync and after the first compile.
2. **Generate** (clone-only `FrontRoomsP4bGenerate.Generate`). It calls the patched writer `EnsureSurfaces(only)` by reflection for the 17 slots, then saves (`data/50_generate.txt`). The 7 new materials come out exactly as specified: FrontRooms/Surface, `_FR_MESH_UV`, instancing on, the tints above.
3. **Defect: the writer's 10 flips.** Each flip slot changed 18 lines, not 1:
   - the flag line;
   - plus 17 lines of shader properties the old `.mat` files lack (`_FR_PlanarSnap`, `_LampField*`, `_UseLampField`, the print block). Unity writes them as defaults because the writer re-assigns the shader.
   
   They are harmless (defaults, keywords off), but the stage allows only the flag (`data/50_flips_writer_defect.diff.txt`). **Reverted** to main's bytes; the writer's copies are kept in `W/p4b_d/defect_writer_flips/`.
4. **Flip only** (`FrontRoomsP4bGenerate.FlipOnly`): `Material.enableInstancing = true`, then save. All 10 files are byte-equal to main's file plus the one line.
5. **Final diff** before → after: **24 files** (7 `.mat` + 7 `.meta` new, 10 one-line flips). No file was removed, and nothing else changed in `Surfaces` or `Rendering`.

Consequence for later: a full `RunBatch` on main (with or without this patch) will add those default lines to every FrontRooms/Surface material older than the shader's newest properties. That is existing writer behaviour and harmless.

---

## 3. Kit reimport (`data/50_reimport.txt`, `data/50_fbxmeta.diff.txt`)

- **Clone result.** All 20 outlet kits were force-reimported through `FrontRoomsKitImporter`. Every renderer slot that has a `Resources/Surfaces/<slot>.mat` is bound to it: **66 bindings, 0 left embedded**.
- **The FBX slots name the plain metal.** They bind `Prop_Aluminium`, not the new metals. R3 swaps in `Prop_StainlessBrushed` (plates) or `Prop_Galvanized` (handy box, EMT, straps, floor boxes) at draw time.
- **The importer writes the remaps into the FBX `.meta` files.**
  - 17 kit `.fbx.meta` files gained 25 entries: 14 × `Prop_NylonIvory`, 11 × `Prop_ThermosetIvory`. Nothing else changed in them.
  - Unchanged: JackPhone4Prong, ConduitEMT and ConduitStrap, which have no new slot.
  - The files are staged in `W/p4b_d/payload_fbxmeta/` but are **not in the package**, because the brief limits it to RenderSetup and `Surfaces/**` (§6 item 1).
- **No sidecar changed.** No slot name had to change.

---

## 4. Measurements

Seed 2554, map root 1 × 192 m, the 10-14 harness (`data/40_FrontRoomsOutletIntegrateCapture.cs.txt`). Its picks, camera poses and luma method are unchanged; the new groups are in the partial `data/50_FrontRoomsOutletIntegrateCaptureP4b.cs.txt`. Today's map at this seed matches 10-14's: 25 sets, 906 fixtures, 6 handy boxes, 1,596 lamps (91 dead). The clone carries map contract C-1 (clone-only), as the 10-14 clones did. Numbers: `data/50_fixlog.txt`; per-shot stats: `data/50_capture_log.txt`.

### 4.1 Ivory D as built (`p4bivory`), plate luma ÷ wall luma

The pose is 40's: 0.30 m square to the plate, then eye height 1.6 m out. "As built" means the material is not touched in the run.

| Where | 0.30 m: plate / wall | Ratio | Device luma | Eye height: plate / wall | Ratio |
|---|---|---|---|---|---|
| Level 0 Standard, duplex at (232.50, 0.31, 203.92), lamp 1.1 m | 113.7 / 119.7 | **0.95** | 113.1 | 109.3 / 115.1 | **0.95** |
| Same plate, fixtint override D (10-14 method, today) | 114.5 / 120.7 | 0.95 | 114.1 | 108.3 / 114.0 | 0.95 |
| Same plate, 10-14 (`40_fixlog_full.txt`) | 112.9 / 118.9 | 0.95 | 112.2 | 109.3 / 115.1 | 0.95 |
| Office Standard, duplex at (175.65, 0.41, 191.92), lamp 1.1 m | 110.0 / 121.0 | **0.91** | 110.3 | 110.4 / 118.8 | **0.93** |
| Aged #94896E, Office duplex | 89.4 / 119.4 | 0.75 | — | — | — |
| Almond #A49B89, Office JackPhone | 107.1 / 121.5 | 0.88 | — | — | — |
| A #E6DDC2 (v2) on the Level 0 plate, today | 157.1 / 119.5 | 1.31 | 155.8 | 147.3 / 115.1 | 1.28 |
| Photo DSC00161 (40 §3.4) | — | 0.88 | — | — | — |

- The device reads within 0.5 % of the plate.
- Images: `images/50_ivory_d.jpg`.

### 4.2 Metals and brown (`fixmetal`, `fixbrown`)

**Before** = `R.FixPassLook = false`, i.e. `Prop_Aluminium`, the 10-14 integrate look. **P-4b** = the new metal slots.

| What | Before (today) | Main today, anodised (10-14 landing) | 10-14 P-4b v2 | **Today P-4b v3** |
|---|---|---|---|---|
| Steel duplex, Office, 0.30 m | 0.59 | 0.80 | 0.92 | **0.92** |
| Steel duplex, 4 m | 0.69 | 0.77 | 0.92 | **0.91** |
| Handy box + EMT, 0.42 m | 0.66 | 0.71 | 0.97 | **0.90** |
| Handy box riser, 4 m | 0.88 | 0.91 | 1.02 | **0.97** |
| Floor box sides vs carpet (luma) | 62 vs 63 | 72 vs 62 | 112 vs 74 | **88 vs 62** |
| Brown plate + brown device | 0.38 | — | 0.38 | **0.38** |

- **Handy box and floor box are lower than 10-14's P-4b column, but the step is the same.**
  - Today's "before" reproduces the 10-14 *landing* clone exactly (0.66, 0.88, 62 vs 62).
  - 10-14's P-4b column came from the full-state clone. Its baseline was higher: 0.73, 0.94, 81 vs 75.
  - The P-4b step is the same: handy box +0.24, riser +0.09 (10-14: +0.24, +0.08).
  - At 0.42 m the handy-box sample points touch the receptacle faces, and those are now the darker D nylon.
- Images: `images/50_metal_p4b.jpg`.

### 4.3 P-6: draws in two fixed Office views (`data/50_inst.txt`, run 3)

**Views.** The harness's `OfficeWide` rule picked the first view: a pod floor feed with desks, chair, cubicles and plates, from (208.712, 1.62, 202.964). The second is the same spot looking the opposite way.

**Method.**
- Per state: one editor frame with exactly one `Camera.Render`, through the game camera with post on.
- The Render counters are read in the next frame.
- The 17 materials' instancing is toggled in memory: off = main today, on = as built.
- R3 on/off isolates the outlets.

| View | P-6 | R3 calls (instanced + single) | Frame draws | SetPass | Triangles |
|---|---|---|---|---|---|
| office_pod, outlets on | off | 50 (0 + 50) | 4,590 | 1,059 | 609,923 |
| office_pod, outlets on | **on** | **20** (19 + 1) | **4,530** | **1,085** | 609,923 |
| office_pod, outlets off | off | 0 | 4,490 | 1,054 | 606,251 |
| office_pod, outlets off | on | 0 | 4,490 | 1,065 | 606,251 |
| office_back, outlets on | off | 24 (0 + 24) | 3,460 | 1,347 | 160,968 |
| office_back, outlets on | **on** | **13** (11 + 2) | **3,438** | **1,360** | 160,968 |
| office_back, outlets off | off | 0 | 3,412 | 1,344 | 160,296 |
| office_back, outlets off | on | 0 | 3,412 | 1,346 | 160,296 |

**Reading:**
- **Outlet draws.** They draw in two passes. Their frame draws drop 100 → 40 (pod) and 48 → 26 (back), and R3's "draws if all instanced" estimate is met exactly. UnityStats reports 98 instanced draws in 38 instanced batches (pod).
- **SetPass calls go up with P-6.**
  - Outlets add 5 SetPass calls without instancing, 20 with it (pod; back: 3 → 14), because the instanced variant needs its own SetPass.
  - The other renderers on the 10 flipped materials add 11 (pod) and 2 (back) with the same draws and identical pixels.
- **Net.** Frame draws −1.3 % / −0.6 %, SetPass +2.5 % / +1.0 %. Whether that is a CPU win needs a frame time at low load (§6 item 4).
- **Batches** equal draws under the SRP Batcher here.

### 4.4 P-6 pixel gate (`data/50_pixdiff.json`)

**Method.**
- Gate frames: post-processing and dithering off, lamps ticked once per view and then frozen.
- Outlet pixels = where outlets on and off differ (the outlets and their SSAO halo), grown 2 px.

| View | Noise (first state again) | Outlet px (grown) | Max diff outside outlet px | Max diff, outlets off, whole frame | Outlet px that differ |
|---|---|---|---|---|---|
| office_pod | 0 / 0 | 5,948 (21,099) | **0** (0 of 2,052,501 px) | **0** (0 of 2,073,600) | 20 px, max 3/255 |
| office_back | 0 / 0 | 843 (2,034) | **0** (0 of 2,071,566) | **0** (0 of 2,073,600) | 0 |

- **Gate (≤ 1/255 outside outlet pixels): PASS**, bit-identical. Runs 2 and 3 agree.
- **Run 1 is invalid as a gate.** It used post on and re-ticked the lamps per state, so repeat frames differed by up to 15/255 over about 85 % of the frame (film grain and dithering are random per render; Stutter lamps).
- Images: `images/50_instancing_office.jpg` (game look, post on, plus the gate's diff map).

---

## 5. Defects found in this stage

1. **The writer's flips carried 17 default lines each** (§2.3). Reverted, then flipped with the flag alone.
2. **Capture run 1 re-saved 11 materials at editor exit** (clone-only harness).
   - Cause: fixtint's SetColor round trip left float noise (`_BaseColor` .549 → .54899997, .612 → .6119999), and the instancing toggles marked materials dirty. Unity's exit save also wrote the 17 default lines into 9 flipped slots.
   - Fix: all 11 restored to the post-reimport snapshot (sha1 verified); copies in `W/p4b_d/defect_run1_saved/`.
   - The harness now clears the dirty flag of every touched slot before exit (`CleanTouched`). Runs 2 and 3 left every snapshot file unchanged.
3. **No draw counts at first.** In a batch-mode editor the Render counters, UnityStats and CPU markers read 0 (runs 1–2). With the editor profiler on (`ProfilerDriver.enabled`, run 3) they report.

---

## 6. Open issues

1. **The FBX `.meta` remaps are not in the package.**
   - Main's 17 outlet `.fbx.meta` files do not remap ThermosetIvory/NylonIvory, and Unity will not re-import them on its own when the new `.mat` files arrive.
   - R3-drawn outlets bind by name at draw time, so they show D at once. Those are every wall plate, floor box, handy box, EMT and strap.
   - GameObject-instantiated kits keep the FBX preview material until they are re-imported. The one in play is the Office pod panel `Kit_CubiclePanel_Powered` (its `Prop_NylonIvory` slot).
   - Options: add the 17 staged metas to the package (`W/p4b_d/payload_fbxmeta/`, sha1s in `data/50_payload_manifest.txt`), or have Red re-import the outlet kits once in the editor.
2. **Office ivory.**
   - D reads 0.91×/0.93× the Office wall, and on screen it takes the room's green-grey light: a sage plate rather than ivory (`50_ivory_d.jpg`). Aged reads 0.75×.
   - L-1 calibrated Level 0 only. A per-zone ivory would need a renderer change (one material per finish today). Red's call.
3. **The filter hook** in `EnsureSurfaces` is new code beyond the proposal (behaviour-neutral for `SetUp`). Keep it, or ship without it and generate through a clone-only reflection wrapper next time.
4. **P-6 cost/benefit** is unproven: −60/−22 draws against +26/+13 SetPass calls per view. A windowed or autopilot frame time at low load is still missing (every run today was under load).
5. **WebGL.** P-6 is a material flag, so it changes WebGL draws too (R3 runs there with its own caps). Not built or measured.
6. **Galvanized comment.** The comment says #B3B5B3 while Unity shows #B2B5B2 (.70 sits on a rounding edge). It is cosmetic.

---

## 7. Files

**This folder:**
- `50_p4b_ivory_d.md` (this file).
- `images/50_ivory_d.jpg`, `50_metal_p4b.jpg`, `50_instancing_office.jpg`.
- `data/`:
  - the patch: `50_rendersetup_p4b_d_patch.py.txt`, `50_rendersetup_p4b_d.256cf3a.diff.txt`;
  - the package: `50_payload_manifest.txt`, `50_flips_final.diff.txt`, `50_flips_writer_defect.diff.txt`, `50_fbxmeta.diff.txt`, `50_snapshot_log.txt`;
  - results: `50_generate.txt`, `50_flip.txt`, `50_reimport.txt`, `50_fixlog.txt`, `50_capture_log.txt`, `50_inst.txt`, `50_pixdiff.json`, `50_timeline.txt`;
  - tools (clone-only): `50_FrontRoomsP4bGenerate.cs.txt`, `50_FrontRoomsOutletIntegrateCaptureP4b.cs.txt`, `50_capture_hooks.diff.txt` (two hook lines and `partial` in the 10-14 harness), `50_pixdiff.py.txt`, `50_sheets.py.txt`.

**`W/p4b_d/`:**
- `payload/` (25 files), `payload_fbxmeta/` (17);
- `snap/` (sha1 lists), `runs/r1–r3_capture/` (frames and logs);
- `logs/` (Unity logs `c1`–`c7`);
- `defect_writer_flips/`, `defect_run1_saved/`, `expected_flips/`;
- `roslyn_*` (compile checks).

**The clone `W/proj_outlet_int` now holds:**
- main + the package;
- the 17 remapped metas;
- clone-only C-1 in MapWorld;
- the harness files in `Assets/Editor/OutletCensus/`.

Re-sync it before using it as a landing clone.
