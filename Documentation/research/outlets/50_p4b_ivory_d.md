# 50 — Outlets P-4b with ivory D (+ P-6 held): build, fixes and measurements

**Status (package stage after verify round 3, 2026-10-22 08:40): BLOCKED. Both verifiers of round 2 returned NOT READY (look and engineering), so no package was built or changed in this stage; the round-2 set in `W/apply/` stays as it is, on HOLD. Blockers: §00.** Nothing was landed. Main is unchanged (HEAD 0ea38bb7, RenderSetup `256cf3a4`).

**Round 2 status (2026-10-22 08:10–08:20, kept): FIXED AND RE-MEASURED in the clone. Not landed, and not ready to land: the package is a candidate on HOLD until Red answers §0.2.** Red picked ivory D on 2026-10-21 (40_final.md, top line); D's hexes are unchanged here. Round 2 answers the look and engineering verifiers of round 1 (§9). Every verified defect inside this stage's scope is fixed. One verifier claim was wrong: option (c) of look item 1 names the wrong file and owner (§0.1, L1). Two verifier items were cut off in this stage's input (L4 after "What:", E4 after "What: `Surfaces"); the visible parts are answered.

- **Packages (apply convention v2, `W/apply/`).** Rebuilt; the round-1 set is in `W/_partial/apply_outlets_p4b_d_fix1_superseded_1022/`.
  - `outlets_p4b_d.sh`: **44 paths** = the look (27) + the 17 outlet kit `.fbx.meta` remaps, folded in from the old `outlets_p4b_d_fbxmeta` (engineering item 2). No instancing anywhere. **HOLD:** `outlets_p4b_d_HOLD.txt` makes `--apply` refuse until Red answers; dry runs still work. On main 0ea38bb7 + working tree (08:17) it prints **DRY RUN OK** (44/44 bases), and `rebase_apply.sh --no-install` prints **PASS**.
  - `outlets_p6.sh`: **HOLD** (`outlets_p6_HOLD.txt`). Instancing, stacked on `outlets_p4b_d`; rebuilt on the v5 RenderSetup, its 17 material flips byte-identical to round 1.
- **Clone:** `W/proj_outlet_int`, re-synced from main at 08:10 with mkclone.sh's steps (rsync `--delete` of the five folders, `apply_local_patches` "nothing to do", `install_tools`), then v5 + the 7 slots + the 6 maps + the harness. Unity was never run on Frontrooms3D.
- **What changed in Red's project:** only this folder (this file, `data/50_fix2_*`, `images/50_fix2_*`).

`W` = `/Users/redwang/FrontRoomsVisualWork`.

**Load rule.** Stage start: 1-minute load 367. Unity runs: 73–107 (`data/50_fix2_timeline.txt`). All runs are **UNDER LOAD**; this file gives no timing figure. Luma, sha1 and draw counts do not depend on load.

---

## 00. Verify round 3 (08:27–08:36): NOT READY on both lenses, so this stage is BLOCKED

- **Verifiers' files:** look `W/p4b_d_verify3/` (runs `v1`, `q1`; `sheets/`), engineering `W/p4b_d_verify_eng3/` (Roslyn `r_p4b_{osx,ios}`, `r_p6_{osx,ios}`, `gen3.log`, snapshots). Both ran in `W/proj_outlet_int`; their before/after snapshots are equal, nothing was saved. Both runs UNDER LOAD (look 117 / 77, engineering 49–59).
- **Passed by the look verifier (no change needed):** matched D plate and device (device/plate 0.99–1.00 at 0.30 m; 0.92–0.95 from eye, oblique and LOD2; 0.95–0.99 at the dark end); stainless reads as brushed metal (Office 0.85–0.90x at 0.30 m, 0.82–0.83x from eye; Lobby 0.93–0.94x, 0.89–0.90x); the anodised placard frame reads as aluminium (0.72–0.79x the wall); the floor box reads as satin metal, spangle patchwork gone; corrected option (c) is right about the mechanism (line 551; `PaletteKits` never picks finishes).
- **This stage did not package**, by its rule (a lens is NOT READY). `W/apply/outlets_p4b_d*` and `outlets_p6*` are untouched, with their HOLD files. No Unity run in this stage.

### 00.1 Blockers

| # | From | Blocker | Checked in this stage | What clears it | Who |
|---|---|---|---|---|---|
| B1 | look 1 | §0.2 item 1 asked Red again about the Office ivory as if new, and leaned to (c) without its costs | **Confirmed.** 40_final §5 item 3 says "Office can keep A, because L-1 calibrates Level 0 only", in the §5 list Red approved with "全部按推荐" on 10-21. (c)'s side effects checked against `FrontRoomsWallFixtures.cs` lines 550–566. **§0.2 item 1 is rewritten here:** (b) is the route on record, (c) is given with its costs and numbers | (b) is a separate 游戏视觉 package and is not built. Red: may `outlets_p4b_d` land before (b) (D in the Office meanwhile), or does it wait for (b)? | Red |
| B2 | look 2, eng 1 | Aged #9A866F is not the derivation the brief asked for | **Confirmed by recomputation.** v2's A → aged step in OKLCH: L ×0.8984, C ×1.4684, hue −1.07°. On D it gives **#94896D**. #9A866F is the same L and C with the hue set to 72°; as 8-bit it measures **70.9°** (65° → 64.4°, 60° → 59.2°). §1.2 is corrected. The strict rule reads olive in Level 0 (round 1 L2; §4.1 hue 47 on the paper's 47), and no candidate clears the Office | Red: OK the hue turn (#9A866F) as an addition to the rule, or take the strict #94896D, or another §0.2 item 2 candidate. Then one constant and one slot regenerated (§5) | Red |
| B3 | look 3–5 | Three look defects "the report doesn't disclose" | **Not visible.** This stage's input cut the look report off after item 2; the three defects are not answered and not guessed. The verifier's files show what it measured: in `runs/q1/vlook.txt`, D / A / C at the dark end (a failing-lamp and a dead-lamp Level 0 plate, a failing-lamp Office plate: e.g. dead-lamp Level 0 plate/wall D 0.71 at 0.45 m and 0.81 from eye, A 0.89 / 1.03, C 0.75 / 0.86; Office failing lamp D 0.88 / 0.92, hue 123–125 against the wall's 126) and four EMT galvanized variants (as built; v2's tint-only metallic .5 smooth .35; metallic .5 with the maps; metallic .45 with the tint ×1.10). Sheets: `dark_abc.jpg`, `emt_native.jpg`, `emt_variants.jpg`, `placard_aged.jpg`, `metal_steel.jpg`, `eye_crops.jpg`, `eye_zooms.jpg`, `ivory_lit.jpg`, `metal_galv.jpg` | The look verifier's items 3–5 in full, then a fix round | workflow |
| B4 | eng 2 | The HOLD release condition names only §0.2 items 1–2 | **Confirmed.** `outlets_p4b_d_HOLD.txt` names the Office ivory and the Aged pick only; the `.sh` header's "Red still has to answer" line the same. Missing: item 3 (mismatched devices) and item 4 (the 17 `Resources/Props/Models/Kit_*.fbx.meta`, outside the brief's ship list). **§5 step 5 here now names all four** and the LANDINGS.md record | Next fix round: `build_packages.py` writes the HOLD text and the header with items 1–4. Or drop the 17 metas from the package and accept the churn | fix round; Red (items 3, 4) |
| B5 | eng 3 | `outlets_p6` not ready | **Confirmed** (already HOLD). No WebGL OK and no `UNITY_WEBGL` gate in R3; no frame time at load ≤ 32 (1-minute load 46 at 08:39); the pixel gate and draw counts (§4.4–4.5) are the 10-21 v3 set, not re-run on the v5 payload `8180cdb6` | WebGL OK or the gate; then a frame time at low load, a draw re-count and the pixel gate on `8180cdb6` | WebGL track or Red; a later p6 stage |
| B6 | eng 4 (low) | Nobody has shown the 17 remaps stay put on a fresh Library or on main's first refresh after landing | **Mechanism confirmed in main.** `Assets/Editor/Rendering/FrontRoomsKitImporter.cs` lines 44–49: `OnPreprocessModel` loads each slot `.mat` with `AssetDatabase.LoadAssetAtPath` and calls `RemoveRemap` when that returns null; it declares no dependency on the material. In `W/p4b_d_fix2/logs/g2_generate.log` the 17 FBX imports are lines 541–557 and the 7 `.mat` imports lines 613–619 (at SaveAssets): in the clone the materials loaded from artifacts that already existed. In main the 7 `.mat` are new, and their import order against the FBX inside one refresh has not been shown. If an FBX imports first, it drops its NylonIvory / ThermosetIvory remaps, rewrites the landed `.fbx.meta`, and the next import adds them back: the churn E2 was meant to end. *The item was cut off after "would call `RemoveRemap`"; this answers the visible part* | In the clone: main's state with a warm Library (no slot `.mat` imported), Unity closed; write the 44 paths in package order; one batch refresh (no-op method); compare the 17 metas with the payload; refresh again, compare again. Repeat with `Library/` moved to `W/_partial/`. If it fails, an importer-side fix (a dependency on the material, or keep the remap when the `.mat` path already has a GUID) is a 游戏视觉 change outside this stage's ship list | fix round |

### 00.2 Order to clear

1. The workflow re-sends the look verifier's items 3–5 in full (B3).
2. Red answers §0.2: item 1 (land before (b), or wait), item 2 (Aged), item 3 (mismatched devices), item 4 (the 17 metas); item 5 only for `outlets_p6`.
3. A fix round: B3's fixes, the Aged constant, the B6 test, then `build_packages.py` with items 1–4 in the HOLD text and header, then §5 steps 2–5.

VL305 stays WAIT-RED and VL306 HOLD (`W/vl_pending/outlets_p4b.md`, updated in this stage).

---

## 0. Fix round 2

### 0.1 Each verifier item

| # | Item | Verdict | What was done | Evidence |
|---|---|---|---|---|
| L1 | Office ivory D reads sage | **CONFIRMED** (round 1 reproduced it: 0.91x / 0.93x the wall, hue 100 sat .20 vs 104 / .16) | **Red's call; VL305 stays WAIT-RED.** Options (a) and (b) as the verifier wrote them. **Option (c) is wrong as written** and is corrected in §0.2: C-2 is not a finish filter, and the finish split is not 关卡设计's file. Corrected (c) still needs no renderer change | `30_contract_map.md` §5; `Assets/Scripts/Office/FrontRoomsWallFixtures.cs` lines 551, 564, 535 |
| L2 | Aged still olive in the Office; the 72° turn is not the proposal's rule | **CONFIRMED, both parts.** v2's ivory → aged step in OKLCH (L ×0.898, C ×1.468, hue −1.1°) applied to D gives **#94896D** (= v3's #94896E within 1/255), and that reads olive in Level 0. So the 72° turn is an addition | **Measured hue 60 and 65 in both zones** (same plates as round 1). Level 0: hue 38 / 39, browner than the paper (47). Office: 80 / 83, still khaki (72° gives 86). **No hue in 60–90 clears the Office**: its light pulls every warm neutral to hue 80–100, the same cause as L1. Built stays **#9A866F**, pending Red's explicit OK for the turn. The code comment now states the derivation exactly | §1.2, `images/50_fix2_aged.jpg`, `data/50_fix2_capture_c1.txt`, `data/50_fix2_oklch_out.txt` |
| L3 | Galvanized spangle reads as crumpled foil (EMT) and camouflage (floor box) | **CONFIRMED** | **Fixed.** New `Prop_Galvanized_{A,N,S}`, minimized spangle: crystals 2.5–5.0 mm (p1 2.9, p50 3.7, p99 4.6), tilt ≤ 1.2°, smoothness .502 −.039/+.027, albedo .980 −.020/+.012, seamless, metallic .6 kept. Inside the slot's own pixels, fine local contrast drops 30–35 % (EMT 0.6 m 1.11 → 0.72; EMT from eye 1.74 → 1.18, bright shards 3.4 % → 0.4 %; floor box 0.42 m 1.93 → 1.35). The EMT reads as a smooth tube again | §1.3, §4.2, `images/50_fix2_galv.jpg`, `data/50_fix2_galv_metrics.tsv` |
| L4 | The designed 5 % mismatched devices now jump hard in brightness | **CONFIRMED and measured.** White device on an ivory-D plate reads **1.38x** (Level 0) and **1.45x** (Office) at 0.30 m, 1.17x / 1.13x from eye; on an aged plate 1.59x. An ivory-D device in a white plate reads 0.68x. With A on the same plates: 0.98, 0.99, 1.12 and 1.00 | **Finding for Red; part of the D decision.** The white device is `Prop_CeramicGlaze` (#DBD9CC), shared with White plates and at least one other kit, and R3 picks it (`DeviceMaterial(White)`). A softer tell needs a white-device slot plus an R3 pick: a separate package. With A, the tell was invisible in brightness | §0.2 item 3, §4.3, `images/50_fix2_mismatch.jpg` |
| E1 | Changing the `EnsureSurfaces` signature breaks two exit-sign tools at runtime | **CONFIRMED.** Read both call sites (`FrontRoomsExitSignLookdev.cs:55-56`, `FrontRoomsExitSignFix1Capture.cs:101-103`). 23 files across W look it up by name: 16 exit-sign tool copies (one in `_partial/proj_es_final_presync_1022`) and 7 copies of this stage's own harness | **Fixed (patch v5).** `static void EnsureSurfaces()` keeps main's parameterless signature and only calls `EnsureSurfacesOnly(null)`. The filter moved to the new `EnsureSurfacesOnly(ICollection<string> only)`. No same-name overload. `FrontRoomsOutletFix1Gen` and the clone's `FrontRoomsP4bGenerate` now use the new name. Checked in the Editor (Mono): the tools' exact call resolves `Void EnsureSurfaces()` with 0 parameters, and `Invoke(null, null)` runs. The 7 regenerated `.mat` files are byte-identical to round 1. Roslyn: COMPILE OK, macOS and iOS | §2.1, `data/50_fix2_generate.txt`, `data/50_fix2_roslyn.txt` |
| E2 | Landing `outlets_p4b_d` alone leaves main unstable under re-import | **CONFIRMED** (third reproduction: the fresh sync's first import rewrote exactly the 17 staged metas, byte for byte; the verifier's 07:41 clone holds the same bytes) | **Fixed by folding.** The 17 metas are now inside `outlets_p4b_d`: one package, one OK, one pass. Write order: GUID metas → assets → `.fbx.meta` remaps (their target `.mat` files exist by then) → C#. The header says these 17 paths are outside the stage brief (`Resources/Props`), so the OK covers them by name | §3, §5 |
| E3 | The payload is not final | **CONFIRMED** | **Guarded.** `outlets_p4b_d_HOLD.txt` and `outlets_p6_HOLD.txt` make `--apply` refuse. In the scratch test, `--apply` printed REFUSED and the tree stayed unchanged. After Red answers: rebuild by the §5 steps, then move the HOLD file to `W/_partial/` | §5 |
| E4 | The six grain textures are not confirmed as in scope | **Visible part answered.** The brief's ship list is `Assets/Resources/Surfaces/**`; its parenthesis names examples. Main's `Surfaces/Textures` already holds 169 maps, among them the ones RenderSetup's `texture` stems name (`Prop_PlasticWhite_*`, …). 游戏视觉's exit-sign packages, landed 10-21, shipped `ExitSign_Diffuser_*` and `ExitSign_Enamel_*` maps into the same folder | Unchanged: in path scope, following precedent. The visual chat confirms at landing. The maps now total 320 KB (galvanized 234 KB, down from about 670 KB) | `W/apply/exit_sign*_expected.txt` |

### 0.2 Red's calls (landing waits on 1–3)

1. **Office ivory** (rewritten after verify round 3, look item 1). The renderer picks one material per finish (`PlateMaterial` / `DeviceMaterial` take no zone), so D lands in the Office too, where it reads sage: 0.91x / 0.93x the wall, hue 100 sat .20 against the wall's 104 / .16.
   - **On record: (b), Office keeps A.** 40_final §5 item 3 says "Office can keep A, because L-1 calibrates Level 0 only", and Red approved the §5 list with "全部按推荐" on 10-21. So this is the approved route, not a new question. It needs Office ivory and aged slots plus a zone-aware pick in R3 (`FrontRoomsWallFixtureRenderer`): a separate 游戏视觉 package, **not built**. An Office aged comes from A by v2's step, i.e. v2's #CDBF98. A reads 1.33x the Office wall.
   - **The open question for Red:** may `outlets_p4b_d` land first, with D in the Office until (b) lands, or does it wait for (b)?
   - **(a) Accept D in the Office for good.** This overrides the 10-21 approval, so only on Red's word.
   - **(c, corrected) Office finish weights.** Not recommended over (b). The verifier's round-2 text said "ask 关卡设计 … through the C-2 palette filter". That is the wrong mechanism and the wrong owner:
     - C-2 is `FrontRoomsModuleEditing.PaletteKits`, which keeps planner-only kits out of the **Level Designer kit palette** (30_contract_map §5). It never picks finishes.
     - The Office finish split is `FrontRoomsWallFixtures.cs:551`: `rule.office ? c < .55 → Ivory : c < .90 → Almond : White`. That file is in `Assets/Scripts/Office`, which belongs to 游戏视觉. So (c) is a one-line 游戏视觉 change, outside this stage's ship scope, with no renderer change.
     - **Its costs** (verify round 3; lines 550–566):
       - every Office line that reaches line 551 becomes White, at **1.28–1.33x** the wall. Tall-zone lines (line 550), stainless and painted-over plates are not affected;
       - **Almond and aged leave the Office:** the device follows the plate (line 535), and aged comes only from Ivory plates (line 564);
       - **line 566 still swaps 5 % of devices,** so Office white plates get ivory-D devices: **0.68–0.69x at 0.30 m and 0.75x from eye** (§4.3; verifier `r2_mm_White_Ivory_OF`). L4's mismatch comes back as a dark device in a bright plate;
       - Almond is no way out: it also reads sage in the Office (0.88x, hue 110 sat .18 against the wall's 119 / .14; verifier `r2_pal_Almond_OF`).
     - The C-1 probe JSON counts no finishes. It was not run for this option.
2. **Aged.** The brief asks to derive Aged from D the way v2 derived it from A. That rule gives **#94896D** (§1.2); the built **#9A866F** adds a hue turn (target 72°, 70.9° as 8-bit) that nobody has approved (verify round 3: look 2, engineering 1).
   - **OK the hue turn** (built, #9A866F): Level 0 is fixed; the Office stays khaki (hue 86–88 sat .23–.24 against the wall's 115 / .15), from the Office light. Recommended, together with (b) for the Office.
   - Or one of: v2's step alone, #94896D (olive in Level 0); hue 65 / 60, #9C8570 / #9D8471 (Office 3–6° less green, Level 0 browner than the paper); or #9E8C70 (lighter, 0.78x).
   - Each is a one-constant change plus one regenerated slot (§5).
3. **Mismatched devices** (new finding, from D):
   - accept the stronger tell: a white device in a grimy ivory plate reads 1.38–1.59x at 0.30 m and 1.13–1.32x from eye;
   - or ask for a darker white-device slot plus an R3 pick (separate package).
4. **The 17 kit metas** are now part of `outlets_p4b_d`'s own OK (engineering item 2). `Resources/Props` is a 游戏视觉 path, but this stage's brief excluded it.
5. **P-6** (`outlets_p6`): an explicit OK that it applies to WebGL (WebGL track or Red), or an R3 `UNITY_WEBGL` gate, plus a low-load frame time. Not needed for the Alpha.

---

## 1. What the package changes (`outlets_p4b_d`, patch v5)

- Patch: `data/50_fix2_rendersetup_p4b_d_v5_patch.py.txt`.
- Diff against main `256cf3a4`: `data/50_fix2_rendersetup_p4b_d_v5.256cf3a.diff.txt` (32 changed lines; payload `e7e0f7f3`).
- `outlets_p6` adds 38 changed lines on top (`8180cdb6`; `data/50_fix2_rendersetup_p6_on_v5.diff.txt`).

### 1.1 What v5 changes against v4

- **`EnsureSurfaces()` keeps main's signature.** v5 wraps it:
  ```csharp
  static void EnsureSurfaces()
  {
      EnsureSurfacesOnly(null);
  }
  static void EnsureSurfacesOnly(ICollection<string> only)   // main's old body + the `only` filter + the instancing line
  ```
  `SetUp()` still calls `EnsureSurfaces()`, so nothing changes for it.
- **Galvanized maps:** regenerated (§1.3). The SurfaceDef numbers are unchanged: tile .20 m, bump 1, mask scale 1, metallic .6, tint #B3B5B3. So `Prop_Galvanized.mat` is byte-identical; only the three PNGs change, and their metas and GUIDs are kept.
- **Comments only:** Almond/Aged state the derivation exactly (§1.2), and the galvanized comment describes the minimized spangle. Unity compiled v5 with the round-1 comment text (`6b73712d`). The payload `e7e0f7f3` differs from it in 4 comment lines; the reconstruction is checked by sha1.
- **Unchanged from v4:** ivory D #A69F8C, nylon #A39C89, almond #A49B89, orange #D8642A, stainless (grain maps, metallic .7, #D4D4CF). The `instancing` field and writer line are also unchanged, and false everywhere; main has no surface material with instancing on (0 of 98).

### 1.2 Aged and Almond: how each is derived from D

The proposal derived both from ivory A (v2: A #E6DDC2 → aged #CDBF98, almond #E3D7BE). In OKLCH (`data/50_fix2_oklch.py.txt`):

| Step (v2, from A) | L | C | hue | Applied to D (#A69F8C, L .703 C .028 h 90.2) |
|---|---|---|---|---|
| A → almond | ×0.983 | ×0.966 | −6.3° | **#A49B89**: the same as v2's per-channel ratio (.987 .973 .979). Almond is consistent either way |
| A → aged | ×0.898 | ×1.468 | −1.1° | **#94896D**, i.e. v3's #94896E. Reads olive in Level 0 (hue 47 on the paper's 47) |
| A → aged, hue set to 72° | ×0.898 | ×1.468 | to 72° | **#9A866F (built)**; as 8-bit it measures hue **70.9°**, not 72° (verify round 3, rechecked). The hue turn is **an addition to v2's step** (aged urea and phenolic go tan and amber), so the built slot is not the brief's derivation. It needs Red's OK |
| same, hue 65° / 60° | ×0.898 | ×1.468 | to 65 / 60 | #9C8570 / #9D8471 (as 8-bit: 64.4° / 59.2°) |

All aged candidates keep v2's lightness step: aged / ivory luminance 0.715–0.727, against v2's 0.725. Measurements: §4.1.

### 1.3 Metals

- **Stainless:** unchanged from round 1 (`Prop_StainlessBrushed_{A,N,S}`, #4 vertical grain, 0.10 m tile, metallic .7, tint #D4D4CF).
- **Galvanized v2:** minimized spangle (`data/50_fix2_gen_galv_min.py.txt`, log `data/50_fix2_gen_galv_min_log.txt`). 512² over 0.20 m = 0.39 mm per texel:
  - **Crystals:** Voronoi on a jittered 60 × 60 grid, 3.33 mm pitch, wrap-around. Equivalent diameters 2.5–5.0 mm (p1 2.9, p50 3.7, p99 4.6).
  - **Normal:** one tilt per crystal, 0–1.2° (mean 0.6°, max 1.2°).
  - **Smoothness:** .502 ± .026 per crystal, −.008 on boundaries, ±.005 mottle. As 8-bit: .463–.529.
  - **Albedo:** .980 ± .011 per crystal, −.006 on boundaries, ±.002 mottle. As 8-bit: .961–.992.
  - **Tiling:** seams at the interior level (A 1.46 vs 1.46; N 0.46 vs 0.47; S 0.78 vs 0.73).
- **These maps meet the verifier's spec** (2–5 mm, ≤ 1.5°, ≤ ±.04, ≤ ±.02).
- **A map-free slot** (same tint × .98, smoothness .50) reads within 0.04–0.10 lc5 of them at 0.42 m and beyond; the crystals show only closer. Dropping the maps would save 3 textures. The maps are kept, because the verifier asked for minimized spangle, not none.
- **The floor box is aluminium in the research** (`01_period_research.md` §6.1). The minimized slot reads as near-uniform satin metal, sides 0.87x the carpet. A true aluminium slot for floor boxes would be an R3 change (separate package).

### 1.4 The 44 paths (`outlets_p4b_d`; GUID metas, assets, kit metas, C#)

| Path (`Assets/…`) | Payload sha1 | Main |
|---|---|---|
| `Resources/Surfaces/Prop_{ThermosetIvory, ThermosetAged, ThermosetAlmond, NylonIvory, PlasticOrange, StainlessBrushed, Galvanized}.mat.meta` | as round 1 (`e017877d`, `ea8e6b96`, `1d4d5caa`, `69926969`, `478c04f1`, `f99016f3`, `c71752b8`) | new |
| `Resources/Surfaces/Textures/Prop_StainlessBrushed_{A,N,S}.png.meta`, `Prop_Galvanized_{A,N,S}.png.meta` | as round 1 (`0f8441d4`, `5c2aa02a`, `5ed3119c`; `3cdf29e9`, `9b46dfd2`, `a56a8514`) | new |
| `Resources/Surfaces/Prop_*.mat` (7) | as round 1, byte-identical after regeneration (`8277941e`, `a5b1f843`, `c624910c`, `4696aa14`, `2ce99483`, `358cfe47`, `24252510`) | new |
| `Resources/Surfaces/Textures/Prop_StainlessBrushed_{A,N,S}.png` | as round 1 (`8387444c`, `0dacc730`, `e2edb3b3`) | new |
| `Resources/Surfaces/Textures/Prop_Galvanized_{A,N,S}.png` | **new maps** `a627d314`, `f96c7491`, `294d92c3` | new |
| `Resources/Props/Models/Kit_*.fbx.meta` (17: CubiclePanel_Powered, FloorBoxTombstone, FloorBoxTombstone2, JackBNC, JackData, JackPhone, OutletBare, OutletBlank, OutletDuplex, OutletDuplex20, OutletDuplex_Cracked, OutletDuplex_Jumbo, OutletDuplex_Steel, OutletHandyBox, OutletScrew, OutletToggle1, OutletToggle2) | main's file + the importer's remaps (25 entries); sha1s in `data/50_fix2_package_manifest.txt` | 17 bases match main |
| `Editor/Rendering/FrontRoomsRenderSetup.cs` | **`e7e0f7f3`** | `256cf3a4` |

- **Kept:** `FrontRoomsRenderSetup.cs.meta` and `Surfaces/Textures.meta`.
- **New GUIDs:** none of the 13 appears in main.
- **No existing file in `Surfaces` or `Rendering` changes.**

---

## 2. How the materials were made

### 2.1 Round 2

1. **Resync.** The clone was re-synced from main at 08:10 with mkclone.sh's steps.
   - The verifier's harness and its 17 churned metas were saved to `W/p4b_d_fix2/harness_saved_presync/` and `clone_presync_metas/`; those metas equal the staged ones.
   - `Library/ScriptAssemblies` was moved to `W/_partial/outlet_int_ScriptAssemblies_1022_fix2` (TOOLS.md's script-reimport workaround).
2. **Install.** v5 RenderSetup, the 7 slot `.mat`/`.meta` and the 6 maps (galvanized v2) were placed. The harness went back in, plus `FrontRoomsOutletFix2.cs` (new GUID `955d60a3…`, absent from main) and the updated `FrontRoomsOutletFix1Gen.cs`.
3. **Run g2** (`FrontRoomsOutletFix1Gen.GenerateNew`): **0 `error CS`**.
   - `EnsureSurfacesOnly(7 slots)` wrote and saved the 7 materials.
   - Then the exit-sign tools' exact call ran: `GetMethod("EnsureSurfaces", NonPublic|Static)` → `Void EnsureSurfaces()`, parameters 0; `Invoke(null, null)`: **OK**.
   - The full pass sets the 7 slots exactly as the filtered pass did. Its 97 dirty flags were cleared, so nothing of it was saved.
   - **Snapshot diff** over the 1,203 files in Surfaces, Rendering and Props/Models: no Surfaces or Rendering file changed. So all 7 `.mat` are byte-identical to round 1.
   - The only changes were the 17 kit `.fbx.meta` files, rewritten by the first import exactly to the staged bytes (§3).
4. **Run c1** (`Fix2RunBatch`, groups galv, aged, mismatch): 0 `error CS`. Nothing saved; 4 dirty flags cleared at exit.

---

## 3. Kit remaps (now inside `outlets_p4b_d`)

- **Runtime does not need them.** R3 and `FrontRoomsKitLibrary.Spawn` → `MaterialSet` bind slots by name (round 1 showed the pod panel binding `Prop_NylonIvory.mat` in main's landed state).
- **But the importer state needs them.** Once `Prop_ThermosetIvory.mat` / `Prop_NylonIvory.mat` exist, any import of the 17 outlet FBX makes `FrontRoomsKitImporter.OnPreprocessModel` add 25 remaps (14 × NylonIvory, 11 × ThermosetIvory) to their tracked `.fbx.meta` files. That covers a Reimport, a Library rebuild, a kit re-export, or a fresh clone.
- **Reproduced three times, always the same bytes:** round 1's forced import, the verifier's 07:41 clone, and this round's first import after the resync.
- **So they land in the same package and under the same OK** (engineering item 2). The separate `outlets_p4b_d_fbxmeta` package no longer exists.
- **Write order:** the `.fbx.meta` files are written after the `.mat` files they point to, so an open editor never imports a remap to a missing GUID.

---

## 4. Measurements

- **Setup:** seed 2554, map root 192 m, the 10-14 harness plus the verifiers' helpers.
- **Harness:** round 2 partial `data/50_fix2_FrontRoomsOutletFix2.cs.txt`; log `data/50_fix2_capture_c1.txt`.
- **Ratios:** plate luma ÷ wall luma (`VPlate`).
- **Galvanized metrics** (`data/50_fix2_galv_metrics.py.txt`):
  - A slot mask is made per pose: the slot drawn flat magenta from the same camera. It is eroded by 3 px, so silhouettes and edges do not count.
  - The same mask measures this round's frames and the verifier's round-1 frames. The verifier's frames line up: its landed frames differ from this round's by ≤ 0.7/255 mean.
  - **lc5** = mean |L − 5×5 box mean|, fine patchwork. **Shards** = % of slot pixels brighter than 1.35 × the slot median.

### 4.1 Aged (same plates as round 1: Level 0 duplex (163.20, 0.41, 227.92); Office duplex (178.20, 0.41, 167.92); lamp 1.2 m)

| Candidate | Level 0, 0.30 m / eye | Office, 0.30 m / eye |
|---|---|---|
| #94896D (v2's step alone, hue 90) | 0.76 / 0.78, hue 47 sat .45 | 0.75 / 0.79, hue 97 sat .26 |
| **#9A866F (built, hue 72)** | **0.75 / 0.78, hue 41 sat .46** | **0.74 / 0.79, hue 86 sat .24** |
| #9C8570 (hue 65) | 0.75 / 0.78, hue 39 sat .46 | 0.74 / 0.79, hue 83 sat .23 |
| #9D8471 (hue 60) | 0.75 / 0.78, hue 38 sat .46 | 0.74 / 0.78, hue 80 sat .22 |
| #9E8C70 (lighter, hue 79) | 0.78 / 0.81, hue 43 sat .46 | 0.78 / 0.82, hue 85 sat .25 |

- **Reference walls:** Level 0 paper hue 47 sat .50; Office wall hue 115–116 sat .15.
- **Reading:**
  - Turning the hue from 72° to 60° moves the Office reading only 6° (86 → 80). It pushes Level 0 further from the paper (41 → 38): the plate reads brown rather than aged ivory.
  - The Office's green-grey light is what makes these plates khaki, and it does the same to D (L1). An Office-specific answer, (b) or (c), fixes both.
- **The fresh ivory-D device in the aged plate:** 1.16–1.23x at 0.30 m, 1.00–1.02x from eye.
- **Ivory D and Almond:** unchanged from round 1 (§9; Level 0 D 0.95 / 0.95, Office 0.91 / 0.93; almond 0.89x / 0.88x).

### 4.2 Galvanized (zoom frames, inside the slot mask)

| Pose | Fix 1 spangle (verifier frame) | **Fix 2 minimized** | Map-free slot | Main as landed (anodised) |
|---|---|---|---|---|
| EMT riser 0.6 m: lc5 / p99 | 1.11 / 109.0 | **0.72 / 98.7** | 0.68 / 96.5 | 0.69 / 104.1 |
| EMT riser from eye: lc5 / shards | 1.74 / 3.35 % | **1.18 / 0.37 %** | 1.13 / 0.36 % | 1.53 / 9.14 % |
| Office floor box 0.42 m: lc5 | 1.93 | **1.35** | 1.29 | 1.67 |
| Floor box standing 1.3 m: lc5 | 3.44 | **2.26** | 2.16 | 3.14 |
| Floor box 0.42 m (game FOV): top / sides / carpet | 112.0 / 92.1 / 101.5 | **120.0 / 92.3 / 105.9** | 120.0 / 93.0 / 106.1 | 125.5 / 75.9 / 109.4 |
| Handy box + EMT 0.6 m, game FOV: slot p10 / p50 / p90 | 44.3 / 76.8 / 138.3 | **46.9 / 78.5 / 135.3** | 46.2 / 77.0 / 134.1 | 30.6 / 72.3 / 183.6 |

**Poses (= the verifier's):**
- floor box (200.40, 0.00, 182.40), Office, Stutter lamp;
- handy box (191.92, 0.31, 173.40), Level 0 Tall, lamp 1.5 m.

**Reading:**
- The patchwork and the bright shards are gone. The EMT reads as a smooth, uniform tube (as main and v3 do), and the floor box reads as satin metal, not camouflage.
- The slot keeps round 1's brightness: the floor-box sides sit at 0.87x the carpet, against main's 0.69x.

#### Stainless
Unchanged from round 1: the Office plates read 0.84–0.90x at 0.30 m, 0.72–0.75x at 55° and 0.82–0.83x from eye 2 m; the Lobby plates 0.93–0.94x and 0.89–0.90x.

#### Round 1 metals table (stainless rows still valid; the galvanized rows show the round-1 spangle, replaced in round 2)

| What | v3 (tint only, m .6 / .5) | **Fix 1 (grain, m .7 / .6)** |
|---|---|---|
| Office stainless A (175.95, 0.46, 209.92): 0.30 m / 55° R / 55° L / eye 2 m | 0.98 / 0.82 / 0.81 / 0.90 | **0.90 / 0.75 / 0.73 / 0.83** |
| Office stainless B (209.25, 0.41, 230.92): same poses | 0.90 / 0.78 / 0.79 / 0.87 | **0.84 / 0.72 / 0.72 / 0.82** |
| Lobby (Level 0) stainless, two plates: 0.30 m / eye 2 m | 1.03, 1.02 / 0.94, 0.95 | **0.94, 0.93 / 0.90, 0.89** |
| Grain: across-grain gradient on the plate field (zoom, A, 0.30 m) | 0.69, anisotropy 1.07 | **1.02, anisotropy 1.53** |
| Office floor box (200.40, 0.00, 182.40), 0.42 m: sides vs carpet | 100.8 vs 106.5 | **91.6 vs 100.8** |
| Floor box from eye height: top / sides / carpet | 175.3 / 149.5 / 97.7 | **179.9 / 152.7 / 98.2** |
| Floor box top, local contrast (spangle) | 2.7 | **7.8** |
| Handy box + EMT (191.92, 0.31, 173.40), 0.42 m, metal px p10 / p50 / p90 (wall 110) | 53 / 88 / 113 | **46 / 78 / 128** |

**Reading:**
- **Darker than v3, by intent.** Plates sit 0.84–0.94x the wall at 0.30 m and 0.82–0.90x from eye height. That stays at or above the anodised landing's 0.80x and far above the 0.59x gunmetal; the obliques are 0.72–0.75x.
- **The frames now show brushed grain and spangle** (`images/50_fix1_metal.jpg`).
- **Candidates in round 1–2** (r1/r2), Office stainless A at 0.30 m:

  | Metallic | Maps | Bump | Tint | Ratio |
  |---|---|---|---|---|
  | .6 | yes | 2 | #C7C7C2 | 0.88 |
  | .7 | yes | 2 | #C7C7C2 | 0.82 |
  | .7 | yes | 2 | #D4D4CF | **0.90 (built)** |
  | .75 | yes | 1.5 | #C7C7C2 | 0.79 |
  | .85 | yes | 1.5 | #C7C7C2 | 0.73 |
  | 1.0 | yes | 1.5 | #C7C7C2 | 0.62 |
  | .85, smoothness .65 | no | 1 | #C7C7C2 | 0.76 |

  Galvanized: metallic .55, .65 and .8 with spangle, and .8 / .5 without maps. Built: .6.
- **The 10-14 "handy box side" sample** (two points) does not land on the galvanized at this pose: it read 100.5 for every candidate. The table uses the masked metal pixels instead. The 10-21 handy-box ratios (0.90, 0.97) came from that sampler, so treat them with care.
- **The anodised placard frame** (Level 0 and Office) is unchanged by this package (verifier run 5).

### 4.3 Mismatched devices (`FrontRoomsWallFixtures.cs:566`: 5 % white ↔ ivory)

Seed 2554 has 14 such duplexes on ivory plates, 2 on aged plates and 1 on a white plate (out of 906 fixtures). Each row below is one plate, measured with D (built) and with A set in memory (plate #E6DDC2, nylon #E2D9BF, aged #CDBF98).

| Plate + device, where | D: device / plate, 0.30 m / eye | A: same |
|---|---|---|
| Ivory + white, Level 0 (234.60, 0.31, 191.92) | **1.38 / 1.17** | 0.98 / 1.00 |
| Ivory + white, Office (202.05, 0.41, 179.92) | **1.45 / 1.13** | 0.99 / 0.97 |
| Aged + white, Level 0 (167.92, 0.31, 176.25) | **1.59 / 1.32** | 1.12 / 1.09 |
| White + ivory, Office (222.75, 0.46, 182.92) | **0.68 / 0.75** | 1.00 / 0.95 |

**Reading:**
- With A, a mismatched device had the same brightness as its plate, so the designed tell showed only as a slight hue shift.
- With D, the white device (`Prop_CeramicGlaze` #DBD9CC, unchanged) pops as a bright insert. A real white-in-ivory patch job does look like that, but this is stronger than the plate-to-wall steps around it (0.81–0.94x).
- Red's call (§0.2 item 3).

### 4.4 P-6 draws (10-21, run 3; now `outlets_p6`, HOLD)

| View | P-6 | R3 calls (instanced + single) | Frame draws | SetPass | Triangles |
|---|---|---|---|---|---|
| office_pod, outlets on | off | 50 (0 + 50) | 4,590 | 1,059 | 609,923 |
| office_pod, outlets on | on | 20 (19 + 1) | 4,530 | 1,085 | 609,923 |
| office_pod, outlets off | off / on | 0 | 4,490 / 4,490 | 1,054 / 1,065 | 606,251 |
| office_back, outlets on | off | 24 (0 + 24) | 3,460 | 1,347 | 160,968 |
| office_back, outlets on | on | 13 (11 + 2) | 3,438 | 1,360 | 160,968 |
| office_back, outlets off | off / on | 0 | 3,412 / 3,412 | 1,344 / 1,346 | 160,296 |

- **Net:** draws −1.3 % / −0.6 %, SetPass +2.5 % / +1.0 %.
- **The non-outlet renderers on the 10 shared slots add +11 / +2 SetPass with no fewer draws** (verifier item E4). These shared slots appear across the kits: PlasticBlack in 40 kits, Chrome 39, Brass 38, Aluminium 17.
- **Whether that is a CPU win needs a frame time at load ≤ 32.** Every run so far was under load.
- These numbers were taken with v3's tint-only metals. The grain maps change neither draws nor SetPass (same materials, same keywords), but `outlets_p6` should be re-counted when it is taken up.

### 4.5 P-6 pixel gate (10-21)

Gate frames: post and dithering off, lamps frozen. Outside outlet pixels, instancing on vs off: **max 0/255 in both views**. With outlets off, the whole frame: max 0. Inside outlet pixels: 20 px differ by ≤ 3/255 (pod). The gate still holds for `outlets_p6`.

---

## 5. Packages and tests

Results: `data/50_fix2_apply_tests.txt`. Manifests: `data/50_fix2_package_manifest.txt`.

```
bash /Users/redwang/FrontRoomsVisualWork/apply/outlets_p4b_d.sh            # dry run: ON HOLD note + DRY RUN OK (44/44 bases, main 0ea38bb7 + tree, 08:17)
bash /Users/redwang/FrontRoomsVisualWork/apply/outlets_p4b_d.sh --apply    # REFUSED while outlets_p4b_d_HOLD.txt exists
bash /Users/redwang/FrontRoomsVisualWork/apply/outlets_p6.sh               # HOLD; refuses until p4b_d lands, and --apply refuses while outlets_p6_HOLD.txt exists
```

**Gates run:**
- **Roslyn** on main + the payload RenderSetup, `--all`: **COMPILE OK**, macOS and iOS defines, for p4b_d (`e7e0f7f3`) and p6 (`8180cdb6`). Also on the clone tree with the harness.
- **Unity batch** (g2, c1): 0 `error CS`.
- **Rebase:** `rebase_apply.sh outlets_p4b_d --no-install`: **PASS**, nothing to rebase.

**Scratch sequence** (a copy of main's RenderSetup, Surfaces and kit metas; the packages copied to a scratch `APPLY_DIR`, so `W/apply` got no applied manifest):

| Step | Result |
|---|---|
| p4b_d dry run | ON HOLD note, DRY RUN OK |
| p4b_d `--apply` with HOLD | **REFUSED, tree unchanged** (snapshot equal) |
| HOLD moved aside → p4b_d `--apply` | MANIFEST OK |
| p4b_d dry run again | ALREADY APPLIED |
| p6 dry run / `--apply` with HOLD / HOLD aside → `--apply` / again | DRY RUN OK / REFUSED / MANIFEST OK / ALREADY APPLIED |
| p4b_d after p6 | refuses ("partially applied"), as expected for a stacked pair |

**After Red answers (each step in the clone, never in main):**
1. Set the answer in `rendersetup_p4b_d_v5_patch.py`:
   - Aged: `AGED_HEX` / `AGED_TINT`;
   - an Office package or a mismatch package, if Red asks for one: separate stages.
2. Re-sync the clone, patch, then run `FrontRoomsOutletFix1Gen.GenerateNew`. It writes the 7 slots through `EnsureSurfacesOnly`; only the changed slot changes.
3. Copy into `W/p4b_d_fix2/pkg_src/`. Move the two package sets, with their HOLD files, from `W/apply` to `W/_partial/`. Run `build_packages.py`; it writes new HOLD files.
4. Dry run, Roslyn (macOS + iOS), `rebase_apply.sh --no-install`.
5. Only when Red has answered §0.2 items 1–4, and §00 B3 and B6 are cleared: move `outlets_p4b_d_HOLD.txt` to `W/_partial/`. Record in LANDINGS.md who gave each OK (items 1–4, naming the 17 `Resources/Props/Models/Kit_*.fbx.meta` for item 4), and claim right before `--apply`. The round-2 HOLD text and `.sh` header name only items 1–2 (§00 B4); the next `build_packages.py` run must write all four.

---

## 6. Open issues

1. **Red's calls in §0.2:** the Office ivory (land before (b) or wait), the Aged pick, the mismatched devices and the 17 kit metas. Landing waits on these.
2. **Verify round 3 returned NOT READY on both lenses:** blockers B1–B6 in §00. The look report's items 3–5 did not reach this stage (B3).
3. **`outlets_p6` gates:**
   - a WebGL OK, or an R3 `#if UNITY_WEBGL && !UNITY_EDITOR` gate on the instanced path (a separate visual-owned change);
   - a WebGL build with a draw count;
   - a frame time at load ≤ 32, Office pod and back views;
   - possibly fewer flipped shared slots, if the +11 SetPass on non-outlet renderers outweighs the outlet saving.
4. **Floor boxes share the galvanized slot.** They are aluminium in the research. A dedicated slot needs an R3 pick (separate package). The minimized maps make this a small visual gap.
5. **WebGL content.** The six maps (320 KB of PNG) ship in WebGL builds on the default import settings. Any reduction goes on the WebGL tab only.
6. **Galvanized comment.** `#B3B5B3` in the code prints as #B2B5B2, because .70 sits on a rounding edge. Cosmetic.
7. **Clone-only tools that call `EnsureSurfaces(only)` by its old v4 shape:** only this stage's own clones (`p4b_d/overlay`, `p4b_d_fix1/harness_saved`, `p4b_d_verify2/harness_saved`), all superseded. The live clone's copies were updated.

---

## 7. Round 0 (10-21 v3 build), kept for the record

- **Rebase.** v3 rebased the v2 proposal onto main's RenderSetup `256cf3a4` with ivory D (`data/50_rendersetup_p4b_d_patch.py.txt`, diff `data/50_rendersetup_p4b_d.256cf3a.diff.txt`).
- **Generation.** It wrote the 7 slots and 10 instancing flips through RenderSetup's writer.
- **Writer defect.** The writer added 17 default-property lines to each flip slot. These were reverted and flipped with the flag alone (`data/50_flips_final.diff.txt`, `data/50_flips_writer_defect.diff.txt`).
- **Harness defect.** It re-saved 11 materials at exit; they were restored, and the harness now clears dirty flags.
- **Draw counters.** These need the editor profiler on in batch mode (`ProfilerDriver.enabled`).
- **Data:** `data/50_payload_manifest.txt`, `50_snapshot_log.txt`, `50_generate.txt`, `50_flip.txt`, `50_reimport.txt`, `50_fbxmeta.diff.txt`, `50_fixlog.txt`, `50_capture_log.txt`, `50_inst.txt`, `50_pixdiff.json`, `50_timeline.txt`.
- **Images:** `images/50_ivory_d.jpg`, `50_metal_p4b.jpg` (v3 metals, superseded by `50_fix1_metal.jpg`), `50_instancing_office.jpg`.
- **Payload:** `W/p4b_d/payload/` is superseded. Do not land it.

---

## 8. Files

**This folder:**
- `50_p4b_ivory_d.md` (this file).
- Images:
  - round 2: `images/50_fix2_galv.jpg`, `50_fix2_aged.jpg`, `50_fix2_mismatch.jpg`;
  - round 1: `50_fix1_office_ivory.jpg` (still current), `50_fix1_aged.jpg` (superseded by `50_fix2_aged.jpg`), `50_fix1_metal.jpg` (stainless rows current, galvanized rows superseded);
  - round 0: `50_ivory_d.jpg`, `50_metal_p4b.jpg`, `50_instancing_office.jpg`.
- `data/50_fix2_*`:
  - patch and diffs: `rendersetup_p4b_d_v5_patch.py.txt`, `rendersetup_p4b_d_v5.256cf3a.diff.txt`, `rendersetup_p6_on_v5.diff.txt`;
  - maps: `gen_galv_min.py.txt`, `gen_galv_min_log.txt`;
  - colour: `oklch.py.txt`, `oklch_out.txt`;
  - results: `generate.txt`, `capture_c1.txt`, `galv_metrics.tsv`, `apply_tests.txt`, `package_manifest.txt`, `roslyn.txt`, `timeline.txt`;
  - clone-only tools: `FrontRoomsOutletFix2.cs.txt`, `FrontRoomsOutletFix1Gen.cs.txt`, `galv_metrics.py.txt`, `build_packages.py.txt`, `sheets.py.txt`.
- `data/50_fix1_*` and the round-0 files: as listed in §9 and §7.

**`W/`:**
- `apply/outlets_p4b_d*` (with `_HOLD.txt`), `apply/outlets_p6*` (with `_HOLD.txt`): the packages.
- `p4b_d_fix2/`:
  - `pkg_src/`, `rs/` (main, v5, v5 + p6), `tex/` (galvanized v2);
  - `runs/c1/` (frames, masks, logs), `logs/`, `snap/`;
  - `apply_tests/` (scratch target, scratch APPLY_DIR, released HOLD copies), `roslyn2_*`;
  - `harness/`, `harness_saved_presync/`, `clone_presync_metas/`, `50_p4b_ivory_d_round1_copy.md`.
- `_partial/`:
  - `apply_outlets_p4b_d_fix1_superseded_1022/` (the round-1 packages, moved, not deleted);
  - `outlet_int_ScriptAssemblies_1022_fix2`.

**The clone `W/proj_outlet_int` now holds:**
- main (08:10) + `outlets_p4b_d` as packaged: the v5 RenderSetup, 7 slots, 6 maps and the 17 kit metas (written by the import, equal to the payload);
- the clone-only harness in `Assets/Editor/OutletCensus/`, with `FrontRoomsP4bGenerate` pointed at `EnsureSurfacesOnly`.

Re-sync it before using it to verify another landing.

---

## 9. Fix round 1 (07:12–07:33), kept for the record

Items and sections below are as written in round 1; where round 2 changed them, §0 and §1–§6 above win.

### 9.1 Each verifier item (round 1)

| # | Item | Verdict | What was done | Evidence |
|---|---|---|---|---|
| L1 | Office ivory D reads sage | **CONFIRMED.** Reproduced on the same plate: 0.91x / 0.93x the wall, hue 100 sat .20, against the wall's hue 104 sat .16. A reads 1.33x, C 1.03x | **Not fixable in this scope** (§0.2 item 1). VL305 is no longer PASS: it is WAIT-RED | `images/50_fix1_office_ivory.jpg`, `data/50_fix1_capture_r1.txt` |
| L2 | Aged #94896E brings the olive back | **CONFIRMED.** Level 0: hue 47 sat .44 (the paper is 47/.50). Office: hue 98 sat .26 | **Re-derived, rule in §1.2.** Built: **#9A866F**. Alternative: #9E8C70. Five candidates rendered on the same plates. Pick: Red or the visual chat | `images/50_fix1_aged.jpg`, §4.1 |
| L3 | Stainless and galvanized read as painted | **CONFIRMED** | **Fixed.** Procedural grain maps: brushed #4 stainless and galvanized spangle. Metallic up to .7 / .6. The verifier's other option, metallic .85–1 alone, was rendered and rejected: it brings back gunmetal (§4.2) | `images/50_fix1_metal.jpg`, §4.2 |
| L4 | The pod panel's receptacle pops on main as landed | **REJECTED.** I rebuilt main's landed state in the clone. The FBX was imported with main's `.meta` (no remap) before the slot materials existed; then the package was added. The imported FBX holds an embedded `Prop_NylonIvory` (URP Lit). But **every spawned panel binds `Assets/Resources/Surfaces/Prop_NylonIvory.mat`**: all 12 panel renderers in the seed 2554 map (DressJob → Spawn), and a fresh `Spawn`. The reason: `FrontRoomsKitLibrary.MaterialSet` (lines 319–333) swaps each slot by the imported material's name through `FrontRoomsSurfaces.TryGet`. The verifier's URP Lit stand-in skipped that swap. At that pose the device reads luma 15.7, under a desk; the verifier measured 15.3 vs 15.1 itself. Nothing pops | No change needed | `data/50_fix1_capture_r3.txt` (lines `pod …`) |
| E1 | No landing package in the house convention | **CONFIRMED** | **Fixed.** Three v2 packages, each with `_base/`, `_bases`, `_expected`, `_keep`, `.diff`, a backup step and a dry run. Built by `data/50_fix1_build_packages.py.txt` from the outlets_lods template. Tested in a scratch sequence (§5). **No LANDINGS.md claim was made**: that file's rules say claim right before writing into Frontrooms3D and never hold a claim across a long wait, and landing waits on §0.2. The visual chat claims when it lands | `data/50_fix1_apply_tests.txt`, `data/50_fix1_package_manifest.txt` |
| E2 | The hand-off misstated the 17 unshipped `.fbx.meta` remaps | **CONFIRMED** | **§3 corrected:** runtime binds by name (see L4), and the remaps only settle the importer state. I reproduced the churn: a forced import after the package rewrites exactly the 17 staged files, byte-equal, with 25 entries. Shipped as `outlets_p4b_d_fbxmeta` (NEEDS OK). The churn is also written in the `outlets_p4b_d.sh` header, which is the landing note | `data/50_fix1_fbxmeta_churn.txt` |
| E3 | P-6 changes WebGL behaviour unmeasured | **CONFIRMED.** R3 `FrontRoomsWallFixtureRenderer.cs:663` uses `material.enableInstancing && SystemInfo.supportsInstancing`. GraphicsSettings has `m_InstancingStripping: 0` | **Fixed by a split.** `outlets_p4b_d` sets no instancing: the writer line `m.enableInstancing = d.instancing` writes false, which is main's state. So no draw path changes on any platform. P-6 becomes `outlets_p6`, on **HOLD** | §6 item 3 |
| E4 | P-6 side effects on shared slots only partly covered | **CONFIRMED** from the 10-21 data: non-outlet renderers +11 / +2 SetPass with no fewer draws, and no low-load frame time. *The item's text was cut off in this stage's input; this answers what was visible* | Becomes `outlets_p6`'s gate. Load at 07:33 was 182–200, so a low-load frame time cannot be taken today | §4.3, §6 item 3 |

### 9.2 Red's calls as of round 1

1. **Office ivory.** 40_final §5.3 says "Office can keep A", and Red approved it with "全部按推荐". But the renderer uses one material per finish: `PlateMaterial` / `DeviceMaterial` have no zone input. So any ivory lands in the Office too. On screen, D takes the Office's green-grey light and reads sage, a little greener than the painted wall.
   - **(a) Accept D in the Office.** Record it, and land as built.
   - **(b) Office keeps A.** This needs Office ivory slots (RenderSetup/Surfaces) plus a zone-aware pick in `FrontRoomsWallFixtureRenderer`. That is a separate 游戏视觉 package, outside this stage's scope. outlets_p4b_d can still land first, with D everywhere until then.
   - A reads 1.33x the Office wall, i.e. bright. C reads 1.03x, pale sage.
2. **Aged:** #9A866F (built, rule-derived) or #9E8C70 (the verifier's amber example, 0.78x). Either Red or the visual chat can pick. The other one is a one-line change plus regenerating one slot (§5).
3. **P-6** (`outlets_p6`): an explicit OK that it applies to WebGL (WebGL track or Red), or an R3 `UNITY_WEBGL` gate. Also a low-load frame time. Not needed for the Alpha.
4. **`outlets_p4b_d_fbxmeta`:** ship the 17 metas (recommended), or accept the churn. `Resources/Props` is a 游戏视觉 path, but this stage's brief excluded it, so it needs an OK.

---

### 9.3 How round 1 made the materials

1. **Resync.** The clone was re-synced from main: rsync with `--delete` of the five folders, `apply_local_patches` ("nothing to do"), `install_tools`. `Library/ScriptAssemblies` was moved to `W/_partial/` (TOOLS.md's script-reimport workaround). The clone-only harness was restored.
2. **Compile 1, main's state.** **0 `error CS`.** The 17 outlet FBX were re-imported with main's metas and no slot materials. The metas stayed byte-equal to main's.
3. **v3 payload in, candidates measured** (runs r1 and r2, in-memory overrides restored, nothing saved).
4. **v4 RenderSetup in, the 10 shared slots back to main's bytes.** Then `FrontRoomsOutletFix1Gen.GenerateNew` called the patched writer `EnsureSurfaces(only)` for the 7 new slots only (`data/50_fix1_generate.txt`). Snapshot diff over the 1,159 files in Surfaces, Rendering and Props/Models: **exactly the 7 `.mat` changed**, metas kept. Compared with v3:
   - each changed by the instancing line (`1 → 0`);
   - Aged also by `_BaseColor`;
   - the two metals also by textures, tile, bump, metallic, smoothness and tint.
5. **Run r3:** as built, no overrides.
6. **Reimport check** (`data/50_fix1_fbxmeta_churn.txt`, §3).

The v3 harness defect that re-saved materials at exit did not recur: every run clears the dirty flag of every touched material, and the snapshots show no stray change.

---
