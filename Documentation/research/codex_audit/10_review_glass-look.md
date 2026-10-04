# Codex audit · 10 · Review: glass, materials and look

Date: 2026-10-03, 22:20–23:5x PDT. Track: glass + materials + look.
Base `7320ed1` (pre-Codex). HEAD `75cfdff`. Red's project was only read. Unity was never opened on it, and nothing was committed.
Captures ran in a private clone, `<scratchpad>/proj_cx_glass` (Red's working tree, Library seeded from `proj_glass`).
The only files I wrote in Red's project are this report, the images in `images/`, and the VL row in `Documentation/VERIFICATION_LOG.md` (§6).

Reference for "verified": `research/glass/30_final.md` (§4 promotion list, §5 hook, §6 map contract, §9 UNVERIFIED) and `research/glass/20_verification.md` (G10 run 3).

---

## 0. Short answer

- **The glass track landed exactly as verified.** All 22 promoted files and their 22 `.meta` files are byte-identical to `proj_glass`. So are the 2 overwritten materials and the grime tool. The md5s match `30_final.md` §4: 24 of 24. GUIDs are unchanged.
- **`FrontRoomsLook.cs` keeps Red's F5 ambient** and its comment (lines 12–19). Only the 3 glass hunks were added.
- **`FrontRoomsRenderSetup.cs` = the P0 port + 2 lines.** The base file is byte-identical to `proj_p0port` (md5 `0407996e`). Codex added the glass hook (line 44) and an N1 SurfaceDef (line 317). Nothing else changed.
- **Red lost no hand-tuned values in `Prop_Glass` or `Prop_BottleBlue`.** The old values were written by RenderSetup's code, not by hand. The blend mode on screen is the same as before. The new values are exactly the glass track's planned result. Details in §2.
- **No other hand-tuned asset changed.** `FrontRoomsPost_Office.asset`, `FrontRoomsPost.asset`, `FrontRooms_URP.asset`, the scene, ProjectSettings and Packages are untouched. The only other settings change is G14's 1-line `prepassShader` field in the URP renderer.
- **WebGL is gated correctly.** Both shader strippers act only when the build target is WebGL. The zone-reflection "dip" runs only in a WebGL player. The WebGL texture sizes are WebGL-only importer overrides.
- **Both glass shaders compile for Metal and WebGL (GLES3).** The shader files and the URP package are byte-identical to the ones that passed the 11:57 compile check. Red's Editor.log has no shader errors.
- **Real problems found: 2 to fix, 2 for Red to decide, 1 handoff.**
  1. **MAJOR, latent:** Codex's LOD code in `FrontRoomsKitImporter.cs` culls every door frame, door leaf, window frame and blind at the wrong distance. Window frames would vanish at 24 m instead of about 117 m. It takes effect on the next re-import of those FBX files, and the interactables merge will trigger exactly that (§3 F1).
  2. **MINOR:** the 4 zone reflection cubes are stale. They were captured at 11:57, before the Level 0 lens ×1.5 (12:26), the P0 print shader (17:27) and N1's cool Office light. Codex skipped `30_final.md` §4 step 3, "recapture" (§3 F2).
  3. **DECISION:** the glass look is live in Red's game, but Red never signed it off. That includes the title rooms, which were never captured before. This audit has now captured them (§4) (§3 F3).
  4. **DECISION:** N1's cool Office lens and lamps are on by default with no pick from Red (§3 F4).
  5. **HANDOFF:** G14 cannot trace window glass, because main's glass shader has no `FRGlassRTPrepass` pass (§3 F5).

---

## 1. What was checked, with the result

| # | Item | Evidence | Result |
|---|---|---|---|
| 1 | 22 promoted glass files + metas | md5 main vs `proj_glass`: shader `3e4bdc16`, blend `81dc1692`, cubes `d6acc839 / cc315e5a / c0f1b6e7 / 44d83e56`, manifest `f34a1523`, ZoneReflection `42d371db`, Driver `0b3329bb`, GlassPane `b2792eef`, GlassSetup `c43bf659`, ReflectionCapture `ceec26df`, GlassVerification `2aa3e10b`, CompileCheck `0d1144b1`, RTStripper `2e22d16d`, Glass_Window `f6285cd6`, Glass_Edge `0ffba08c`, Glass_Shard `a8ff20b2`, Glass_ShardClear `92a4c8ef`, GlassGrime_M `5ce543f2`, GlassSmear_N `8de9065b`, `pack_glass_grime.py` `fe7682ff`; every `.meta` identical too | PASS: equals `30_final.md` §4 |
| 2 | `Prop_Glass.mat`, `Prop_BottleBlue.mat` | main `0583e4e9`, `85bf5dbb` = clone = §4 | PASS (values: §2) |
| 3 | `FrontRoomsLook.cs` | `git diff 7320ed1 HEAD`: +comment L22–24, `FrontRoomsZoneReflection.Set` L40, `Reapply()` L60. AmbientSky (.20,.19,.15), AmbientEquator (.26,.24,.17), AmbientGround (.40,.36,.24) at L17–19 with Red's comment L12–16. File = `promote_src` merge (`9e8c78f9`) | PASS |
| 4 | `FrontRoomsRenderSetup.cs` | base `7320ed1` md5 `0407996e` = `proj_p0port` = `proj_p0port_v`. HEAD diff = 2 added lines: L44 `FrontRoomsGlassSetup.EnsureAll();` (§5 hook; comment shortened) and L317 `Troffer_Lens_Cool` SurfaceDef (N1) | PASS for glass; L317 belongs to N1 (F4) |
| 5 | What RunBatch writes | `EnsureAll` (GlassSetup L67–143) touches only its 6 materials and the 2 grime importers. It runs after the URP Lit pass, so `Prop_Glass` / `Prop_BottleBlue` end on `FrontRooms/Glass` (`research/glass/logs/compile_check_fixpass.txt` lines 1–3). The committed `.mat` files equal that output (`_SrcBlend` 5 left by the Lit pass, `_Smoothness` .94 from SetGlass). L317 re-creates `Troffer_Lens_Cool` with emission (1.989, 2.121, 2.210), within 0.0004 of the committed asset | PASS |
| 6 | `FrontRoomsRoomStream.cs` hook | L351–352 is exactly the §4.3 hook (`SetZoneReflection(Level0, 0f)` after `initialized = true`) | PASS as code; look: F3, §4 |
| 7 | Grime textures | importer metas identical to the clone (BC7 / BC5; WebGL override half size) | PASS |
| 8 | `Refl_*.exr` | EXR 1536 × 256 strip (6 × 256), half float, PIZ. Import: cube, specular convolution, mips on, sRGB off, default 256 px CompressedHQ (BC6H on desktop). WebGL override 128 px RGB9e5. The 4 metas differ only in GUID / sprite ids | PASS |
| 9 | Load-time side effects in editor scripts | `[InitializeOnLoad]` exists only in `FrontRoomsGlassVerification.cs:31` (and G14's `FrontRoomsGlassRTVerify.cs:31`). Both only hook `EditorApplication.update` when a `SessionState` flag set by their own batch run is on. No asset writes on load. `WarnIfStale()` runs only from the menu or GlassSetup | PASS |
| 10 | Hand-tuned assets | `git diff --stat 7320ed1 HEAD` and `git status` show no change to `FrontRoomsPost_Office.asset` (md5 `2d46ab21` both), `FrontRoomsPost.asset`, `FrontRooms_URP.asset`, `Scenes/`, `ProjectSettings/`, `Packages/`, or any other `Resources/Surfaces/*.mat` (91 tracked). `FrontRooms_URP_Renderer.asset`: +1 line `prepassShader` (G14) | PASS |
| 11 | WebGL gating | `FrontRoomsGlassRTStripper.cs:21` and `FrontRoomsGlassRTWebGLStripper.cs:23` return unless `activeBuildTarget == WebGL`. `FrontRoomsZoneReflection.cs:271` uses the dip only on `RuntimePlatform.WebGLPlayer`. Texture and cube WebGL sizes are WebGL importer overrides | PASS: desktop unchanged |
| 12 | Metal + GLES3 compile | `logs/compile_check_fixpass.txt` (11:57, after the final shader's 11:50 save): `FrontRooms/Glass` 6 keyword sets × 2 stages × {GLES3x, Metal} OK, messages 0; `ReflectionBlend` OK. Main's shader bytes, `Packages/manifest.json` (`28042516`) and `packages-lock.json` (`d448bcec`) equal `proj_glass`'s. Red's Editor.log (50,551 lines): no "Shader error" / "Shader warning" lines, no FallbackError, no glass or zone-reflection warnings | PASS |
| 13 | Red's Play Mode with HEAD | Editor.log lines 39,670–50,551: 0 exceptions from glass, zone reflection, `FrontRoomsKitLibrary` or the window facade | PASS (no runtime errors) |
| 14 | Map window glass | `FrontRoomsInteractableKit.Window.cs:65-69`: the map's 30 mm `TransparentGlass` pane renderer is disabled once the 6 mm slab exists. The slab is a scaled unit cube with `Glass_Window` and no shadow (L166-190). This matches the G10 AFTER state (`20_verification.md` §1 B) | PASS: no double glass. The double trims are window-landing's |

---

## 2. `Prop_Glass` and `Prop_BottleBlue`: property diff and verdict

Property by property, `7320ed1` → HEAD (script `<scratchpad>/matdiff.py`):

| Property | Prop_Glass before → after | Prop_BottleBlue before → after |
|---|---|---|
| Shader | URP/Lit (`933532a4…`) → `FrontRooms/Glass` (`13d6ede3…`) | same |
| `_BaseColor` (sRGB) | (.82, .88, .88, **α .16**) → (.1517, .1718, .1601, α 1) = FaceLinear (.02, .025, .022) as gamma | (.36, .58, .80, **α .42**) → (.36, .58, .80, α 1) |
| Coverage | flat α .16 → `_AlphaFace` .12 + `_AlphaFresnel` .88 × F⁵ | flat α .42 → .30 + .70 × F⁵ |
| `_Smoothness` | .92 → .94 | .90 → .90 |
| New glass values | `_PaneF0` .08, `_DustFilm` .12, `_Scatter` .5, `_ReflectionMin` 1, `_AutoEdge` 0, `_RTReceive` 0 | `_PaneF0` .08, `_DustFilm` 0, `_Scatter` 0, `_ReflectionMin` 1, `_AutoEdge` 0, `_RTReceive` 0 |
| Keywords | `_ALPHAPREMULTIPLY_ON _SURFACE_TYPE_TRANSPARENT` → none | same |
| Disabled passes | MOTIONVECTORS, DepthOnly, SHADOWCASTER → none (the glass shader has only `ForwardLit`) | same |
| `_SrcBlend` | 1 → 5 (unused: see below) | 1 → 5 (unused) |
| Queue | 3000 → 3000 | 3000 → 3000 |

**Were Red's tuned values lost? No.**
- The old colours and smoothness are RenderSetup's own code: `GlassDefs`, `FrontRoomsRenderSetup.cs:392-393` (`7320ed1` L390-391), written by `EnsureGlassMaterials()`.
- The material history has only three commits, and the only change in them is `_SrcBlend`:
  - `4722918`: 1;
  - `8f5cb75`: 5. That is RunBatch: one commit rewrote 52 materials, and `EnsureGlassMaterials` writes `SrcAlpha` = 5 (L429).
  - `9abf01a`: 1 again. That is URP's material validation. `_BlendModePreserveSpecular` = 1 makes it premultiplied (One) and adds `_ALPHAPREMULTIPLY_ON`, which RenderSetup never sets.
- **The blend on screen is unchanged.** The old effective blend was One / OneMinusSrcAlpha (premultiplied). `FrontRooms/Glass` hard-codes `Blend One OneMinusSrcAlpha, One OneMinusSrcAlpha` (`FrontRoomsGlass.shader:91`). So the leftover `_SrcBlend` 5 in the new file is not read by anything.

**Are the new values the glass track's intended result? Yes.**
- They are exactly `FrontRoomsGlassSetup.EnsureAll`: `Prop_Glass` L114-123, `Prop_BottleBlue` L137-142.
- They are the values G10 run 3 measured (`20_verification.md` §0.4: hutch and cabinet glass read as glass again; `g10r3_07_props.jpg`).
- G10's BEFORE used copies of these two materials (`proj_glass/Assets/Editor/Audit/G10/G10Before_*.mat`), and those copies equal `7320ed1` property for property. So the before/after sheets compared against Red's real current values.

**Revert path, if Red prefers the old prop look:** `git show 7320ed1:Assets/Resources/Surfaces/Prop_Glass.mat` (and `Prop_BottleBlue.mat`), plus removing the 2 `Ensure(...)` blocks for them in `FrontRoomsGlassSetup.cs`. Otherwise RunBatch moves them back onto `FrontRooms/Glass`. This is F3's decision.

---

## 3. Findings

### F1 · MAJOR (latent) · `FrontRoomsKitImporter.cs` culls kit doors, windows and blinds at the LOD1→LOD2 distance

- **Where:** `Assets/Editor/Rendering/FrontRoomsKitImporter.cs:76-91` (Codex, `8ef5b64`).
- **Defect:** the loop maps `lodDistances[i]` onto `lods[i]`. The sidecars give `[d01, d12, dcull]`. `Kit_DoorFrame_Wood.json:75` says so: "lodDistances (d01, d12, dcull) m at lodBias 1, FOV 76; -1 = never cull. LOD2 is not exported until P-1." The FBX files have 2 LODs, so the last LOD (the cull threshold) gets d12 instead of dcull.
- **Affected:** 16 kits that have `_LOD1` and `lodDistances` (scan of `Resources/Props/Models`):
  - 3 door frames and 7 door leaves `[4, 12, -1]`;
  - 4 window frames `[4, 12, null]` (the null reads as 0; Red's log shows no JSON errors);
  - 2 mini-blinds `[3, 10, 30]`.
- **Effect at FOV 76:**

| Kit | Cull distance now (lodBias 1 / Ultra 2) | Should be | Old default (2 % height) |
|---|---|---|---|
| Window frames, doors | 12 m / 24 m | never | ≈1.83 m frame: ≈58.6 m / ≈117 m |
| Blinds | 10 m / 20 m | 30 m / 60 m | — |

  The window frames are live in every map window (W1.4 facade). Beyond 24 m the frame would vanish while the glass slab and the wall opening stay.
- **Why it is not visible yet:** every window and door FBX was imported at 18:55–18:57, before Codex's importer change (file time 19:02:49). Red's log shows only `Kit_MiniBlind_Lowered.fbx` imported later (lines 39,443 and 39,628), and blinds are not spawned yet.
- **What triggers it:** any re-import: a Library rebuild, Reimport All, another machine, or **the interactables-kit or window-landing merge of new FBX files.** Both are resuming now.
- **Fix (2 lines, visual-owned file):** the last LOD takes the last array entry, and the others take theirs by index:

  ```csharp
  var d = authored.lodDistances; var n = lods.Length;
  for (var i = 0; i < n; i++) { var distance = i == n - 1 ? d[d.Length - 1] : d[i]; /* unchanged */ }
  ```

  Optional: `context.DependsOnSourceAsset(sidecar)`, so a sidecar edit re-imports its FBX.
- **Check:** re-import `Kit_WindowFrame_Wood` and `Kit_MiniBlind_Lowered` in a clone and read `LODGroup.GetLODs()`. Expected last thresholds: 0 (never cull) for the frame, and `group.size`/(2·30·tan 38°) for the blind.
- **Owner:** visual. Fix it in this audit's Fix phase, before the interactables-kit merge, and tell interactables-kit (P-1 is their spec item).

### F2 · MINOR · The 4 zone cubes are stale against main

- **Evidence:**
  - `Assets/Resources/Rendering/Reflections/capture_manifest.txt` says the cubes were captured at 11:57 with `FrontRoomsSurface.shader = 079f93ca0983`, "static paper", and lens "Map / Level 0 lens, Troffer_Lens".
  - Main now differs in 3 inputs:
    1. **Level 0 lens ×1.5.** `Level0LensEmissionScale = 1.5f` first appears in `aba5f77` (12:26). It is absent in `c69c7d7` (10:01). Now at `FrontRoomsMapWorld.cs:3107`.
    2. **P0 print layer.** The Surface shader is `8e4f3ac0cde2` (`df4cb03`, 17:27) and contains `_FR_PRINT` 4 times.
    3. **N1 V5.** Office lamps are (.90, .96, 1.00) at 6.0 and the lens is `Troffer_Lens_Cool` (`FrontRoomsTransitionLightLead.cs:33-35`), but `Refl_Office.exr` holds the warm Office.
- **On screen:**
  - lamp glints in glass are dimmer than the ×1.5 lens;
  - Office glass, VCT and metal reflect a warm room under cool light;
  - the wallpaper in reflections is static paper.

  Glass reflects 8 % face-on and the world uses 0.5 linear, so this is subtle, not broken.
- **Why:** Codex skipped `30_final.md` §4 "After promoting" steps 2–3. `WarnIfStale()` would also report only 2 of the 3 (Surface shader + print). It does not track lens emission or lamp colour.
- **Fix:**
  1. After Red's N1 pick, so it is captured once: run *Capture zone reflection cubemaps* in a clone of main, then G10's subset check.
  2. Promote the 4 `.exr` files and the manifest. Keep main's metas.
  3. Add `Level0LensEmissionScale`, the Office lamp colour and intensity, and the Office lens material name to `CurrentInputs()` (`FrontRoomsReflectionCapture.cs:188-196`).
  4. Add "cubes stale until recapture" to the G1 row in `VISUAL_CHAT_TASKS.md`.
- **Owner:** visual (glass); waits for F4's decision.

### F3 · DECISION · The glass look is live without Red's sign-off

- **The status was "not signed off".** `30_final.md` line 3 says "ready to promote, not signed off. Red has to judge the frames in §2", and §4 says "Copy after Red approves".
- **What changed in Red's game is wider than the glass verification covered:**
  - the hutch, cabinet, vending, clock and phone glass (`Prop_Glass`) and the water bottle (`Prop_BottleBlue`);
  - every map window (6 mm `Glass_Window` slab);
  - zone reflections on all surfaces at 0.5 linear;
  - **every title room**, which now reflects the Level 0 cube at 0.5 linear instead of Unity's default sky at slider 0.3. `30_final.md` §9 lists the title rooms as UNVERIFIED.
- **This audit captured the title rooms** (§4).
- **Red decides:** keep, or revert per part:
  - props: §2 revert path;
  - title rooms: delete `FrontRoomsRoomStream.cs:351-352`;
  - windows: window-landing.
- **Owner:** red-decision.

### F4 · DECISION · N1 "light lead" V5 is on by default: cool Office lens and lamps, with no pick

- **Evidence:**
  - `FrontRoomsTransitionLightLead.cs:21`: `On = !HasArg("-transitionsOff") && !HasArg("-lightleadOff")`;
  - `:33-35`: Office (.90, .96, 1.00) at 6.0, lens `Troffer_Lens_Cool`;
  - MapWorld `BuildMaterials` hunk 11 (`00_main_state.md` §3.6).
- **The material itself is sound:**
  - `Troffer_Lens_Cool.mat` md5 `1eecb065` = `proj_trans_lightlead`;
  - it differs from `Troffer_Lens.mat` only in `_EmissionColor` (2.2, 2.1, 1.8) → (1.989, 2.121, 2.210);
  - the Rec.709 luminance is the same: 2.0996 both;
  - same shader and textures.
- **The problem is that it is live.** Red has not picked a transition (Figma "FRONTROOMS · LEVEL TRANSITIONS"), and the old N1 row said "Red confirms before anything is implemented".
- **Fix if Red has not picked:** make `On` opt-in (`HasArg("-lightleadOn")`). That is one line in a visual-owned file; MapWorld does not change. Keep `Troffer_Lens_Cool.mat` and the L317 SurfaceDef, which are inert when off.
- **Owner:** red-decision; then the N1 owner. MapWorld hunks go to the map chat as a contract (see the map-track review).

### F5 · HANDOFF (glass-rt-track) · Main's glass shader has no `FRGlassRTPrepass` pass, so G14 traces no window glass

- **Evidence:**
  - main `FrontRoomsGlass.shader` (3e4bdc16, 29,095 B) has 0 `FRGlassRTPrepass` lines;
  - `Glass_Window` (`_RTReceive` 1) gets prepass bit 30 (`FrontRoomsGlassRTSystem.cs:707`), and bit 30 is drawn only with that pass (`FrontRoomsMetalGlassRTRendererFeature.cs:83, 165-171`);
  - the trace pass still turns on `_FR_GLASS_RT` with weight 1 whenever a receiver is visible (`:147`, `:222`). So on Mac the glass draws its RT variant with coverage 0, which falls back to the cube term (shader L453-484).
- **Not confirmed on screen:** G14's runtime is Play-mode only, and I did not capture it.
- **The G14 shader merges cleanly onto the verified shader.** `proj_rt`'s `FrontRoomsGlass.shader` (`4dfc2012`, 42,863 B) is main's verified `3e4bdc16` plus 4 hunks, all tagged `[G14 G-1/G-2/G-3]`: the globals, the roll scale, the fade range and the new pass. It removes no glass-track line. So glass-rt-track can take it when its verify passes.
- **Then re-run the glass track's proofs:**
  - keyword-off bit-identity (175 checks, `FrontRoomsGlassVerification`);
  - the G10 window views.
- **Owner:** glass-rt-track.

---

## 4. Title rooms: captured (F3 evidence)

CAPTURE_RESULTS_PLACEHOLDER

---

## 5. Handoffs per workflow

| Workflow | What to take from this review |
|---|---|
| this audit's Fix phase (visual) | F1 importer fix (2 lines) before any kit FBX merge; verify on 2 kits in a clone |
| interactables-kit | F1: after the fix, re-imported kits get the authored LOD distances; until P-1 exports LOD2, dcull is the cull for LOD1 |
| window-landing | Keep the slab = `Glass_Window` on a scaled unit cube with shadows off (§1 #14). The window frames depend on F1 |
| glass-rt-track | F5: merge `proj_rt`'s shader (main + G-1/2/3 only), then re-run the 175-check proof and G10 |
| q1-live-print | `proj_q1`'s RenderSetup already carries L44 (glass hook) and L317; keep both when merging |
| visual (glass) | F2 recapture after the N1 pick; manifest keys; G1 row note |
| Red | F3 sign-off of the glass look (props, windows, title rooms §4); F4 N1 pick or switch-off |

## 6. Verification images

VLOG_PLACEHOLDER

## 7. Files

- This report: `research/codex_audit/10_review_glass-look.md`.
- Images: `research/codex_audit/images/cx_glass_title_*.jpg` and `cx_glass_title_metrics.json`.
- Clone (temporary): `<scratchpad>/proj_cx_glass`. Capture script `Assets/Editor/Audit/CxGlassTitleReflCapture.cs` (clone-only). Raw PNGs in `Verification/cx_glass/`. Log `cx_capture.log`.
- Scripts: `<scratchpad>/matdiff.py` (material property diff), `<scratchpad>/cx_glass_sheet.py` (metrics and sheets).
