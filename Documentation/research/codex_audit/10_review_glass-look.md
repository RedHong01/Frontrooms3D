# Codex audit · 10 · Review: glass, materials and look

Date: first pass 2026-10-03 22:20–23:1x (stalled before its captures); finished 2026-10-04 06:19–08:3x PDT.
Track: glass + materials + look. Base `7320ed1` (pre-Codex). Codex's last commit `75cfdff`.
Main HEAD now is `1385738` (Red, 08:10). Red's four commits after `75cfdff` (`03c43ed`, `956b786`, `d610d3a`, `1385738`) change no file in this track. Under `Assets/` they touch only the map chat's Relay files and one blind `.meta`.

Red's project was only read. Unity was never opened on it, and nothing was committed.
Captures and compile checks ran in my clone, `<scratchpad>/proj_cx_glass`. The clone equals main for every script, shader, material and setting; it adds only `Assets/Editor/Audit/`.
Files I wrote in Red's project:
- this report;
- `images/cx_*`;
- `F1_kit_importer_lod.diff` (proof only);
- rows VL080 and VL081 in `Documentation/VERIFICATION_LOG.md`.

Reference for "verified": `research/glass/30_final.md` (§4 promotion list, §5 hook, §9 UNVERIFIED) and `research/glass/20_verification.md` (G10 run 3).

---

## 0. Short answer

- **The glass track landed exactly as verified.** I re-hashed today: 24 of 24 files match `30_final.md` §4 and `proj_glass`, and all 22 `.meta` files are byte-identical. GUIDs are unchanged.
- **`FrontRoomsLook.cs` keeps Red's F5 ambient** and its comment: Sky (.20, .19, .15), Equator (.26, .24, .17), Ground (.40, .36, .24) at lines 17–19, comment at 12–16. Only the 3 glass hunks were added.
- **`FrontRoomsRenderSetup.cs` = the P0 port + 2 lines.** L44 is the glass hook, and L317 is N1's `Troffer_Lens_Cool` SurfaceDef. In the clone, RunBatch's glass steps rewrote all 7 glass and lens materials **byte-identically** to main's committed files.
- **Red lost no hand-tuned values in `Prop_Glass` or `Prop_BottleBlue`.** The old values came from RenderSetup's code, and the on-screen blend is the same. The new values are exactly the glass track's G4 result (§2).
- **No other hand-tuned asset changed.** That covers `FrontRoomsPost_Office.asset`, `FrontRoomsPost.asset`, `FrontRooms_URP.asset`, the scene, ProjectSettings and Packages. The renderer asset gained G14's 1-line `prepassShader` field.
- **WebGL is gated correctly, and the shaders compile.** I re-ran the compile check on main's files today:
  - `FrontRooms/Glass` (6 keyword sets × 2 stages) and `ReflectionBlend` compile for GLES3x (WebGL) and Metal with 0 messages;
  - G14's prepass shader compiles for both and is stripped only for WebGL.
- **Red's editor shows no errors from this track.** That holds for both his logs since Codex: 19:06–20:01 and 23:53–05:55, which includes a Play session that entered the map.
- **Findings: 1 to fix, 1 to schedule, 1 for Red, 1 handoff.**
  1. **F1 · MAJOR (latent), CONFIRMED in-engine.** Codex's importer culls window frames and doors at **12.0 m** (24 m on Ultra); the sidecars say "never". Blinds cull at 10 m instead of 30 m. This is the same defect as `10_review_kits.md` F1. This review adds a rendered proof (VL081) and an independent test of the fix. Land window-landing r4's patch before any `Kit_*` FBX merges.
  2. **F2 · MINOR.** The 4 zone cubes are stale. They predate the Level 0 lens ×1.5, the P0 print layer and N1's cool Office. Red keeps V5 on, so recapture now.
  3. **F3 · DECISION.** The glass look is live without Red's sign-off. **New, measured:** the title hook brightens 4 of the 5 title rooms by only 1–2 luma. The **Run** room gains up to 5.8 luma: its red floor and bench metal take Level 0's yellow (VL080).
  4. **F5 · HANDOFF (glass-rt-track).** Main's glass shader has no `FRGlassRTPrepass` pass, so G14 traces no window glass. `proj_rt`'s newest shader (22:42) is still main's verified shader plus G14 hunks only, so it merges cleanly.
- **Retired:** the old F4 ("N1 V5 on with no pick"). Red decided at 22:1x that V5 stays on until he picks (`RED_DECISIONS.md`). Its material checks pass (§1 #15).

---

## 1. What was checked, with the result

| # | Item | Evidence | Result |
|---|---|---|---|
| 1 | 22 promoted glass files + metas | md5 main = `proj_glass` = `30_final.md` §4, re-run 2026-10-04 06:3x: shader `3e4bdc16`, blend `81dc1692`, cubes `d6acc839 / cc315e5a / c0f1b6e7 / 44d83e56`, manifest `f34a1523`, ZoneReflection `42d371db`, Driver `0b3329bb`, GlassPane `b2792eef`, GlassSetup `c43bf659`, ReflectionCapture `ceec26df`, GlassVerification `2aa3e10b`, CompileCheck `0d1144b1`, RTStripper `2e22d16d`, Glass_Window `f6285cd6`, Glass_Edge `0ffba08c`, Glass_Shard `a8ff20b2`, Glass_ShardClear `92a4c8ef`, GlassGrime_M `5ce543f2`, GlassSmear_N `8de9065b`, `pack_glass_grime.py` `fe7682ff`; every `.meta` identical | PASS |
| 2 | `Prop_Glass.mat`, `Prop_BottleBlue.mat` | main `0583e4e9`, `85bf5dbb` = clone = §4 | PASS (property diff: §2) |
| 3 | `FrontRoomsLook.cs` | `git diff 7320ed1 HEAD`: comment L22–24, `FrontRoomsZoneReflection.Set` L40, `Reapply()` L60. F5 values L17–19 and Red's comment L12–16 untouched | PASS |
| 4 | `FrontRoomsRenderSetup.cs` | `7320ed1` md5 `0407996e` = `proj_p0port`. HEAD adds exactly 2 lines: L44 `FrontRoomsGlassSetup.EnsureAll();` (the §5 hook) and L317 `Troffer_Lens_Cool` SurfaceDef | PASS |
| 5 | What RunBatch writes | Clone run of `FrontRoomsGlassCompileCheck` (calls RenderSetup's old `EnsureGlassMaterials`, then `EnsureAll`, then `SaveAssets`). Before and after, md5 of `Prop_Glass`, `Prop_BottleBlue`, `Glass_Window/Edge/Shard/ShardClear` and `Troffer_Lens_Cool` are identical; the grime importer metas are unchanged. Log: `<scratchpad>/proj_cx_glass/Verification/glass/compile_check.txt` | PASS: RunBatch does not move Red's committed glass assets |
| 6 | `FrontRoomsRoomStream.cs` hook | L351–352 = the §4.3 hook (`SetZoneReflection(Level0, 0f)` after `initialized = true`) | PASS as code; look: F3 |
| 7 | Grime textures | metas identical to the clone (BC7 / BC5; WebGL override at half size) | PASS |
| 8 | `Refl_*.exr` | EXR 1536 × 256 strip, half float. Import: cube, specular convolution, mips, sRGB off, 256 px BC6H on desktop, WebGL override 128 px RGB9e5. Metas = clone | PASS (content stale: F2) |
| 9 | Load-time side effects | `[InitializeOnLoad]` only in `FrontRoomsGlassVerification.cs:31` and G14's `FrontRoomsGlassRTVerify.cs:31`. Both hook `EditorApplication.update` only while their own batch run's `SessionState` flag is set. No asset writes on load. Note (no action): after a Play session the static `hasZone` survives in the edit-mode domain, so an editor menu tool that calls `FrontRoomsLook.ApplyAmbient` (RenderSetup L580, look-dev tools) would re-assert the zone cube in the open scene's RenderSettings. Runtime always re-sets it, and the scene file is unchanged in git. One-line hardening for the glass owner: reset `hasZone` in `Release()` | PASS |
| 10 | Hand-tuned assets | `git diff 7320ed1 HEAD`: no change to `FrontRoomsPost_Office.asset` (md5 `2d46ab21` both), `FrontRoomsPost.asset` (`c88c9e52`), `FrontRooms_URP.asset` (`406dc1aa`), `Scenes/`, `ProjectSettings/`, `Packages/`, or any other `Resources/Surfaces/*.mat`. `FrontRooms_URP_Renderer.asset`: +1 line `prepassShader` (G14) | PASS |
| 11 | WebGL gating | `FrontRoomsGlassRTStripper.cs:21` and `FrontRoomsGlassRTWebGLStripper.cs:23` return unless the build target is WebGL; `WouldStrip(StandaloneOSX, …)` = False, `WouldStrip(WebGL, …)` = True (clone run). `FrontRoomsZoneReflection.cs:271`: the "dip" crossfade is used only on `WebGLPlayer` (or with no HDR render texture). Desktop Play uses the "blend" render texture | PASS: desktop unchanged |
| 12 | Metal + GLES3 compile, on main's files today | `compile_check.txt` + `Verification/cx_glass/compile_wrap.txt` (2026-10-04 07:0x): `FrontRooms/Glass` 6 keyword sets × {vertex, fragment} × {GLES3x, Metal}: 24 OK, 0 messages (incl. `_FR_GLASS_RT` and the probe-blending set); `ReflectionBlend` 4 OK; `Hidden/FrontRooms/GlassRTPrepass` 4 OK | PASS |
| 13 | Red's editor since Codex | `Editor-prev.log` 19:06–20:01: 0 glass or zone errors after 19:41. `Editor.log` 23:53–05:55 (6,003 lines; one Play session, `START … map root (192, 0, −192)` at L5794): 0 shader errors, 0 glass, zone-reflection, `Collection was modified` or ZBinningJob lines. The 297 `AudioClip.SetData failed` lines belong to the sound track | PASS |
| 14 | Map window glass | `FrontRoomsInteractableKit.Window.cs:166-190`: the 6 mm slab is `Glass_Window` on a scaled unit cube, shadows off; the map's 30 mm pane renderer is disabled once the slab exists. Matches G10's AFTER | PASS. The double trims belong to window-landing |
| 15 | `Troffer_Lens_Cool.mat` (N1 V5) | md5 `1eecb065` = `proj_trans_lightlead`. It differs from `Troffer_Lens.mat` only in name and `_EmissionColor` (2.2, 2.1, 1.8) → (1.989, 2.121, 2.210). Rec.709 luminance 2.0996 vs 2.0994. RenderSetup L317 reproduces it | PASS. V5 stays on by Red's decision; the cubes follow it (F2) |

---

## 2. `Prop_Glass` and `Prop_BottleBlue`: property diff and verdict

Property by property, `7320ed1` → HEAD (`<scratchpad>/matdiff.py`, re-run 2026-10-04):

| Property | Prop_Glass before → after | Prop_BottleBlue before → after |
|---|---|---|
| Shader | URP/Lit (`933532a4…`) → `FrontRooms/Glass` (`13d6ede3…`) | same |
| `_BaseColor` (sRGB) | (.82, .88, .88, **α .16**) → (.1517, .1718, .1601, α 1) = FaceLinear (.02, .025, .022) | (.36, .58, .80, **α .42**) → (.36, .58, .80, α 1) |
| Coverage | flat α .16 → `_AlphaFace` .12 + `_AlphaFresnel` .88 × F⁵ | flat α .42 → .30 + .70 × F⁵ |
| `_Smoothness` | .92 → .94 | .90 → .90 |
| New glass values | `_PaneF0` .08, `_DustFilm` .12, `_Scatter` .5, `_ReflectionMin` 1, `_AutoEdge` 0, `_RTReceive` 0 | `_PaneF0` .08, `_DustFilm` 0, `_Scatter` 0, `_ReflectionMin` 1, `_AutoEdge` 0, `_RTReceive` 0 |
| Keywords | `_ALPHAPREMULTIPLY_ON _SURFACE_TYPE_TRANSPARENT` → none | same |
| Disabled passes | MOTIONVECTORS, DepthOnly, SHADOWCASTER → none (the glass shader has only `ForwardLit`) | same |
| `_SrcBlend` | 1 → 5 (not read: see below) | 1 → 5 (not read) |
| Queue | 3000 → 3000 | 3000 → 3000 |

**Were Red's tuned values lost? No.**
- The old colours and smoothness are RenderSetup's own `GlassDefs` (`FrontRoomsRenderSetup.cs:392-393`, `7320ed1` L390-391), written by `EnsureGlassMaterials()`.
- The material's git history (`4722918` → `8f5cb75` → `9abf01a`) changes only `_SrcBlend`:
  - 1 at `4722918`;
  - 5 at `8f5cb75`, from RunBatch (`SrcAlpha`, L429; the commit rewrote 52 materials);
  - 1 again at `9abf01a`, from URP's material validation (`_BlendModePreserveSpecular` = 1 → premultiplied One, plus `_ALPHAPREMULTIPLY_ON`).
- **The on-screen blend is unchanged.** It was One / OneMinusSrcAlpha before, and `FrontRooms/Glass` hard-codes `Blend One OneMinusSrcAlpha` (`FrontRoomsGlass.shader:91`). The leftover `_SrcBlend` 5 is not read.

**Are the new values the glass track's intended G4 result? Yes.**
- They are exactly `FrontRoomsGlassSetup.EnsureAll`: `Prop_Glass` L114-123, `Prop_BottleBlue` L137-142.
- They are what G10 run 3 measured (`20_verification.md` §0.4, `g10r3_07_props.jpg`).
- G10's BEFORE copies (`proj_glass/Assets/Editor/Audit/G10/G10Before_*.mat`) equal `7320ed1` property for property. So the before/after sheets compared against Red's real values.

**Revert path, if Red prefers the old prop look (F3):** restore both files from `7320ed1`, and remove the 2 `Ensure(...)` blocks for them in `FrontRoomsGlassSetup.cs`. Otherwise RunBatch moves them back onto `FrontRooms/Glass`.

---

## 3. Findings

### F1 · MAJOR (latent) · CONFIRMED · The kit importer culls window frames and doors at 12 m

**Same defect as `10_review_kits.md` F1.** This section adds an independent clone, a rendered proof and a fix test.

- **Where:** `Assets/Editor/Rendering/FrontRoomsKitImporter.cs:81-87` (Codex, `8ef5b64`).
- **What is wrong:**
  - `lods[i]` takes `lodDistances[i]`.
  - The sidecars hold `[d01, d12, dcull]` (`Kit_DoorFrame_Wood.json:75`: "lodDistances (d01, d12, dcull) … -1 = never cull. LOD2 is not exported until P-1").
  - The FBX files have 2 LODs, so the cull threshold takes d12.
  - 16 kits are affected: 3 door frames, 7 door leaves, 4 window frames and 2 blinds. These are the kits that have both `_LOD1` and `lodDistances`.
- **Measured in my clone** after a forced re-import (`Verification/cx_glass/log.txt`). The editor runs High (lodBias 1, `QualitySettings.asset:7`); Mac/Win players default to Ultra (lodBias 2, `:338`):

| Kit | Cull now (High / Ultra) | Authored | Control |
|---|---|---|---|
| `Kit_WindowFrame_Wood`, `_Steel` | **12.0 m / 24.0 m** | never | — |
| `Kit_DoorFrame_Wood`, `Kit_DoorLeaf_Veneer` | **12.0 m / 24.0 m** | never | — |
| `Kit_MiniBlind_Raised`, `_Lowered` | **10.0 m / 20.0 m** | 30 m | — |
| `Kit_Hutch` (no `lodDistances`) | 60.8 m / 121.6 m | default 2 % | unchanged |

- **Rendered:** the walnut frame alone at FOV 76 is drawn at 6.0 m and 11.7 m, and gone at 12.3 m and 30 m (`images/cx_kit_lod_windowframe_cull.jpg`, VL081).
- **Why Red does not see it yet:**
  - Red's Library imported these FBX files at 18:55–18:57, before Codex's change at 19:02.
  - There is no `GetVersion()` bump, so nothing re-imports.
  - `Editor.log` since 23:53 shows no kit import.
- **What triggers it:** any re-import. The window-landing and interactables-kit merges will copy new `Kit_*` FBX files into Red's open editor. Then every map window frame vanishes beyond 12 m, while the slab and the map trims stay.
- **Fix:** land window-landing r4's patch `<scratchpad>/win_r4/FrontRoomsKitImporter.lodcull.diff`. It adds the last-LOD rule plus `GetVersion() => 2`.
  - I tested the same loop change in my clone (`F1_kit_importer_lod.diff`, proof only, `Verification/cx_glass/fix/log_fix.txt`).
  - Frames and doors: never cull. Blinds: 30 m / 60 m. LOD0→LOD1 switches unchanged (4 m / 3 m). Control unchanged.
  - The 30 m render shows the frame again: 296 frame pixels below luma 40 in the centre crop, against 0 before.
- **When:** in this audit's Fix phase, or in the same merge as the first `Kit_*` FBX, whichever comes first. The file is visual-owned (`Editor/Rendering`).
- **Red's open call stays open:** reading LOD switch distances from the sidecars at all is P-1, DW proposal call (8). If Red declines it, restore `7320ed1`'s importer instead.
- **Owner:** visual. **Handoff:** window-landing (patch owner), interactables-kit (P-1).

### F2 · MINOR · The 4 zone cubes are stale against main

- **Evidence:**
  - `Resources/Rendering/Reflections/capture_manifest.txt` says the cubes were captured 2026-10-03 11:57 with:
    - lens "Map / Level 0 lens, Troffer_Lens";
    - "static paper (no `_FR_PRINT`)";
    - `FrontRoomsSurface.shader = 079f93ca0983`;
    - `info.FrontRoomsMapWorld.cs = 65cd495183d6`.
  - That MapWorld (`proj_glass`) has 0 `Level0LensEmissionScale` and no `lampColor`.
- **Main differs in 3 inputs:**
  1. **Level 0 lens ×1.5:** `FrontRoomsMapWorld.cs:3107`, first in `aba5f77` (12:26).
  2. **P0 print layer:** the Surface shader is now `8e4f3ac0cde2`, with `_FR_PRINT` ×4.
  3. **N1 V5:** the Office lamp is (.90, .96, 1.00) at 6.0 with `Troffer_Lens_Cool` (`FrontRoomsTransitionLightLead.cs:33-35`; MapWorld L3152-3154). `Refl_Office.exr` still holds the warm Office.
- **On screen:** lamp glints in glass and gloss are dimmer than the ×1.5 lens. Office surfaces reflect a warm room under cool light. Reflected wallpaper is static. Glass reflects 8 % face-on and the world uses 0.5 linear, so this is subtle, not broken.
- **Why:** Codex skipped `30_final.md` §4 "After promoting" steps 2–3. Also, `WarnIfStale()` (`FrontRoomsReflectionCapture.cs:189-196, 257-262`) compares the Surface shader and the print, but not lens emission or lamp colour.
- **Fix (visual, glass owner; not this audit's Fix phase):**
  1. Red keeps V5 on until his pick (`RED_DECISIONS.md`), so recapture now in a clone of main. Then run G10's subset check.
  2. Promote the 4 `.exr` files and the manifest; keep main's metas.
  3. Recapture again only if Red's N1 pick changes the Office lamps or lens.
  4. Add `Level0LensEmissionScale`, the Office lamp colour and intensity, and the Office lens name to `CurrentInputs()`.
- **Owner:** visual.

### F3 · DECISION · The glass look is live without Red's sign-off; the Run title room changes most

- **Status before Codex:** `30_final.md` line 3 says "ready to promote, not signed off", and §4 says "Copy after Red approves". §9 lists the title rooms as UNVERIFIED.
- **What is live in Red's game now:**
  - the `Prop_Glass` props (hutch, cabinet, vending, clock, phone) and the bottle;
  - every map window (`Glass_Window` slab);
  - zone reflections on all surfaces at 0.5 linear;
  - all five title rooms (Level 0 cube at 0.5 linear, instead of the scene's default sky at slider 0.3 = 0.073 linear).
- **Measured (this audit, §4, VL080):**
  - Lobby, Shift, Office and Exit gain **+1.1 to +2.1** mean luma. Only 0.7–4.8 % of pixels change by more than 4 levels. The noise floor is 0.43–0.49.
  - **Run** gains **+2.7 to +5.8**, and 13–62 % of its pixels change by more than 4 levels.
  - In Run, the VCT floor and the bench's steel frame pick up Level 0's yellow (floor ΔG +6.1 > ΔR +5.1). The red reads less saturated, and the bench frame reads brass.
- **Red decides** (per part):
  - keep as is;
  - title rooms: drop the RoomStream hook (`FrontRoomsRoomStream.cs:351-352`), or keep it everywhere except Run (a lower Run intensity is a one-line rule in RoomStream, owned by visual);
  - props: §2 revert path;
  - windows: window-landing.
- **Owner:** red-decision.

### F5 · HANDOFF (glass-rt-track) · Main's glass shader has no `FRGlassRTPrepass` pass, so G14 traces no window glass

**Same root as `10_review_docs.md` C3.** Only the glass-shader side is covered here.

- **Evidence:**
  - Main's `FrontRoomsGlass.shader` (`3e4bdc16`, 29,095 B) has 0 `FRGlassRTPrepass` lines.
  - `Glass_Window` (`_RTReceive` 1) gets prepass bit 30 (`FrontRoomsGlassRTSystem.cs:725`). Bit 30 is drawn only with that pass (`FrontRoomsMetalGlassRTRendererFeature.cs:83`).
  - G14 runs in Red's editor: `Editor.log` "plugin loaded · caps 0x30ff" at L693, and "map found" at L5818.
- **The merge is clean.** `proj_rt`'s newest `FrontRoomsGlass.shader` (`4d18787b`, 42,933 B, 22:42) is main's verified shader plus G14 hunks:
  - 255 lines added;
  - 3 lines replaced by `_FR_GLASS_RT`-gated equivalents (roll scale `[G14 G-3]`, fade range `[G14 G-2]`);
  - no glass-track line removed (`<scratchpad>/cx_rt_shader.diff`).
- **After the merge,** re-run the glass proofs: keyword-off bit-identity (175 checks, `FrontRoomsGlassVerification`) and the G10 window views.
- **Owner:** visual. **Handoff:** glass-rt-track.

---

## 4. Title rooms: captured (F3 evidence)

**Method:**
- Clone `proj_cx_glass` = main's files. Edit-mode title preview (one room per `RoomRule`, built by RoomStream's own code). 1920 × 1080, 4× MSAA, full post (`FrontRoomsPostStack.ConfigureCamera`), FOV 76, F5 ambient applied.
- Per shot:
  - **A**: the scene's default reflection (Skybox, intensity 0.3), the pre-Codex title;
  - **A2**: A again, to give the noise floor;
  - **B**: `FrontRoomsZoneReflection.SetImmediate(Level0)` = Custom, `Refl_Level0`, 0.735 gamma = 0.5 linear, which is what the hook does at Play start.
- Edit mode binds the cube asset. Desktop Play binds an HDR copy of the same cube ("blend"), so the content is the same.
- Script: `<scratchpad>/proj_cx_glass/Assets/Editor/Audit/CxGlassTitleReflCapture.cs`. Raw PNGs: `Verification/cx_glass/`. Metrics: `images/cx_glass_title_metrics.json`.

| Room | Mean ΔY (B − A), forward / back / floor | Pixels with \|ΔY\| > 4 | Noise (A vs A2) |
|---|---|---|---|
| Lobby | +1.56 / +1.12 / +1.31 | 2.7 / 0.7 / 1.0 % | 0.45–0.47 |
| Shift | +2.01 / +1.48 / +2.08 | 4.8 / 1.6 / 4.2 % | 0.47–0.48 |
| Office | +1.82 / +1.35 / +1.29 | 3.9 / 2.2 / 1.2 % | 0.44–0.49 |
| **Run** | **+3.65 / +2.73 / +5.79** | **33.3 / 13.2 / 62.2 %** | 0.43–0.47 |
| Exit | +1.84 / +1.42 / +1.96 | 3.0 / 1.3 / 3.2 % | 0.46–0.49 |

Images (all under `research/codex_audit/images/`):
- `cx_glass_title_forward.jpg`, `_back.jpg`, `_floor.jpg`: A | B | 6× |B − A| for all five rooms;
- `cx_glass_title_run_floor_1to1.jpg`, `cx_glass_title_run_bench_1to1.jpg`: 1:1 crops, A | B.

---

## 5. Handoffs per workflow

| Workflow | What to take from this review |
|---|---|
| this audit's Fix phase (visual) | F1: land r4's importer patch (with `GetVersion() => 2`) before or with the first `Kit_*` FBX merge. Nothing else in this track needs a fix now |
| window-landing | F1 patch owner. Keep the slab = `Glass_Window` on a scaled unit cube with shadows off (§1 #14) |
| interactables-kit | F1: after the patch, re-imported kits take the authored LOD distances; dcull is the cull until P-1 exports LOD2 |
| glass-rt-track | F5: merge `proj_rt`'s shader (main + G14 hunks only), then re-run the 175-check proof and G10 |
| q1-live-print | `proj_q1`'s RenderSetup must keep L44 (glass hook) and L317 (`Troffer_Lens_Cool`) when merging |
| visual (glass owner) | F2: recapture the cubes now (V5 is the standing state); extend the manifest keys; add "cubes stale until recapture" to the G1 row. Optional: reset `hasZone` in `FrontRoomsZoneReflection.Release()` (§1 #9) |
| Red | F3: keep or revert the glass look per part; the Run title room is the one that changes visibly (VL080) |

## 6. Verification images (Figma "FRONTROOMS · VISUAL VERIFICATION LOG", section `2595:6093`)

| VL | Slide | Check | Verdict | Images |
|---|---|---|---|---|
| VL080 | `2624:6093` | Title rooms reflect Level 0 | WAIT-RED | `cx_glass_title_floor.jpg` · `cx_glass_title_run_floor_1to1.jpg` · `cx_glass_title_run_bench_1to1.jpg` |
| VL081 | `2624:6105` | Kit frames cull at 12 m | FAIL | `cx_kit_lod_windowframe_cull.jpg` · `cx_kit_lod_readout.png` |

- Both rows are in `Documentation/VERIFICATION_LOG.md` §3 as `placed 2026-10-04`.
- The section grew from 24,880 to 26,120 high (21 rows) for them. Nothing on the page lies below it.
- Next free VL is VL082. VL078 and VL079 stay reserved for N3.

## 7. Files

- This report: `research/codex_audit/10_review_glass-look.md`.
- Images: `research/codex_audit/images/cx_glass_title_{forward,back,floor}.jpg`, `cx_glass_title_run_{floor,bench}_1to1.jpg`, `cx_glass_title_metrics.json`, `cx_kit_lod_windowframe_cull.jpg`, `cx_kit_lod_readout.png`.
- `research/codex_audit/F1_kit_importer_lod.diff`: proof only; land r4's patch.
- Clone (temporary): `<scratchpad>/proj_cx_glass`. It is back to main's importer. Clone-only scripts are in `Assets/Editor/Audit/`:
  - `CxGlassTitleReflCapture.cs`;
  - `CxKitLodReadout.cs` (+ `CxKitLodFixCheck`);
  - `CxCompileWrap.cs`.
- Logs: `cx_capture.log`, `cx_compile.log`, `cx_lodfix.log`; outputs under `Verification/cx_glass/` and `Verification/glass/compile_check.txt`.
- Scripts: `<scratchpad>/matdiff.py`, `<scratchpad>/cx_glass_sheet.py`, `<scratchpad>/cx_rt_shader.diff`.
