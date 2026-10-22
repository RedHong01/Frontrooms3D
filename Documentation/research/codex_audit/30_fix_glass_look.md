# 30 · Codex audit fix group "glass-look" (2026-10-08, finished 2026-10-14)

Fix stage of the codex audit for 3 confirmed findings: F6, F7 and F2.

- **Verified base:** main `bc0aa79` plus the working tree, recorded 2026-10-08 19:38. It held Codex's uncommitted 10-08 16:41 screen fix (`FrontRoomsScreenVideo.cs` `515087bf` plus the new `Resources/Rendering/FrontRoomsScreenVideo.shader`). Red committed that fix in `2ce2087` (10-10).
- **Current base:** main `0a9ee7f` plus the working tree (2026-10-14 16:20). Since 10-08 one more change reached this file: COV-4, the shared CRT quad (`codex_audit_kits_window.sh`, merged 10-14 16:2x, +9 lines in `MakeQuad`). The package was rebased over it (§4).
- **Clone:** `W/proj_cxfix_glass_look` (`mkclone.sh` on 10-08; re-synced from main on 10-14 16:20). Bases: `W/proj_cxfix_glass_look.bases.txt` (10-08 record plus the 10-14 re-check).
- **Apply package:** `W/apply/codex_audit_glass_look.sh` (convention v2). Dry run on main: `DRY RUN OK`.
- **Written into Red's project:** only this report, 3 images under `images/`, and rows VL226–VL227 in `Documentation/VERIFICATION_LOG.md`. No file under `Assets/` was touched. No commit, no push. Unity never ran on main.
- **Machine check (macOS 27.0.1):** Unity 6000.3.10f1 batch mode compiles the clone (exit 0, 0 errors). Blender 4.3.0 runs headless.

## 0. Result in one table

| Finding | Status | Change in this package | Owner of the rest |
|---|---|---|---|
| **F6** CRT screen material in players | **FIXED.** Part of it was fixed by a later commit: `2ce2087` (Codex's 10-08 16:41 fix) loads a custom shader from Resources, so players no longer throw `ArgumentNullException`. This package fixes the rest (see §1) | new `Resources/Media/CRT_AdSurface.mat` (+ `.meta`, `Media.meta`); `MakeMaterial` and `CreateSurface` in `FrontRoomsScreenVideo.cs` | Red: rebuild Mac and iOS (TestFlight) to ship it |
| **F7** ad quad floats after its monitor culls | **FIXED.** No later commit touched it | `FrontRoomsScreenVideo.AddToMonitorLods` (new), called at the end of `CreateSurface` | — |
| **F2** stale zone reflection cubes | **DEFERRED** to the glass owner (handoff in the finding). Still true on main `0a9ee7f` (§3). It does not break the game: no exception, no wrong material, only slightly stale reflections | none | visual (glass owner) |
| COV-5 items 1–3 (from fix group kits+window) | Covered by F6 here | (same as F6) | WebGL player build: combine stage or WebGL track (§8) |

Map-owned problems in this group: **none**. So there is no `contracts/*.diff`.

## 1. F6 · the CRT screen material in players

**State on main (`0a9ee7f`).** `MakeMaterial` now tries, in order: `Resources.Load<Shader>("Rendering/FrontRoomsScreenVideo")`, then `Shader.Find` for `FrontRooms/ScreenVideo`, URP Unlit and `Unlit/Texture`. It chains these with `??`. If all are null it logs an error and returns null. The shader in Resources is kept in every build, so the original crash is gone. Four problems remain:
1. **The look changed.** `FrontRooms/ScreenVideo` is a 1-pass unlit shader. It has no fog and no DepthOnly or DepthNormals pass. So the ads now ignore the scene fog and are missing from SSAO and the depth prepass, in the editor too. At 0.85 m the ×8 difference image is the fog (VL227, left pair).
2. **Half-built quad.** If no shader is found, `CreateSurface` still makes the quad and its trigger collider, with an empty material slot.
3. **`??` on `UnityEngine.Object`.** It skips Unity's destroyed-object test.
4. **Dead fallback.** `Unlit/Texture` is null in every player build checked (Mac 10-04, iOS 10-04).

**Change.**
- New asset `Assets/Resources/Media/CRT_AdSurface.mat`: URP Unlit, Opaque, `_BaseColor` white, no texture, no keywords. Unity made it in the clone (`-buildTarget OSXUniversal`). Being under Resources, it keeps URP Unlit in every build. Always Included Shaders is not used.
- `MakeMaterial`: the template is loaded once per session (static cache). Order: `new Material(template)`, else Codex's `FrontRooms/ScreenVideo` (Resources, then by name), else URP Unlit by name (editor only). Explicit `== null` checks. `Unlit/Texture` is dropped. Returns null if nothing is found.
- `CreateSurface`: makes the material first. If it is null, it logs one warning per session (static flag) and returns before it makes the quad, the mesh filter, the renderer or the collider. `Update`, `BindTexture` and `TogglePower` already skip a screen with no surface. `Start` calls `CreateSurface` again, and the flag keeps that quiet.
- Codex's shader and its `.meta` (GUID `5c45f9d7…`) stay as they are, as fallback 2.
- Claim correction (from the finding): the monitor's body collider still gives focus and the E toggle, and no UI shows an E prompt. Before `2ce2087`, the real effects in players were: no ad texture, one `ArgumentNullException` per monitor, and a half-built quad.
- WebGL note: this puts URP Unlit into WebGL builds too (it was already there through other materials, and Codex's shader is in Resources as well). This is a bug fix on every platform, not a WebGL optimisation. URP Unlit compiles 30 of 30 variants on GLES3/WebGL.

**Evidence (VL227, `3116:7038`).**
- Editor, 0.85 m, test card: before = `FrontRooms/ScreenVideo`, after = URP Unlit template. The ×8 difference is the scene fog. `images/fix_glass_look_f6_look_pair.jpg`.
- Mac development player of a one-monitor probe scene (Metal, Ultra): spawn exception none; material `FrontRooms / CRT ad surface`, shader URP Unlit, supported; video prepared and playing; quad in LOD0 and LOD1. `Player.log`: 0 lines with `Parameter name: shader`. `images/fix_glass_look_f6_player_near.jpg`.
- Build shader table (`globalgamemanagers`, decoder `W/cxfix_glass_look/tools/shtable.py`): `Universal Render Pipeline/Unlit` → PPtr (4, 625) non-null; `FrontRooms/ScreenVideo` → (4, 624) non-null; `Unlit/Texture` → (0, 0) null. Red's 10-04 Mac and iOS builds had no URP Unlit entry at all.
- Shader compile (`ShaderData.Pass.CompileVariant`): URP Unlit 30 of 30 (passes Unlit, DepthOnly, DepthNormalsOnly) and `FrontRooms/ScreenVideo` 18 of 18. Targets: GLES3/WebGL, Metal macOS, Metal iOS. 48 of 48 OK.
- Play Mode 120 s on the real map (seed 516574485, census every 5 s): up to 20 live screens (26 seen), all URP Unlit, 0 empty slots, every quad in every LOD, 0 console lines from the screen code, 0 null-shader lines.

## 2. F7 · the ad quad culls with its monitor

**State on main.** `CreateSurface` parents the quad under the monitor but never adds it to the monitor's LODGroup. A renderer outside every LOD keeps drawing. At High (lodBias 1, FOV 76) the monitor culls at 11.18 m and the quad floats on alone (VL115, FAIL).

**Change.** New `AddToMonitorLods(Renderer)`, called at the end of `CreateSurface`:
- it uses `GetComponent<LODGroup>()` on the monitor root (the kit importer puts the group there); a group further up could belong to something else;
- it adds the quad to every LOD's renderer list (no duplicates) and calls `SetLODs`;
- it does **not** call `RecalculateBounds`, so the group size stays 0.524 m and the import-time switch and cull distances do not move;
- a monitor without a LODGroup (the ScreenAds demo rig) is left alone.

**Evidence (VL226, `3116:7020`, `images/fix_glass_look_f7_cull_sheet.jpg`).** Clone-only harness `CxCrtFix` (successor of CxCrtMini). One `Kit_CRTMonitor`, the game's ambient, fog and Office post, 8 views, screen on vs off, with the shared ad RT and with a bright test card. Pixels more than 8/255 apart inside the quad's box + 4 px:

| Shot (test card) | Before, High | After, High | After, Ultra (lodBias 2) |
|---|---|---|---|
| 9.3 m | 344 (monitor LOD1) | 344 | 344 |
| 14.3 m | **140** (monitor culled) | **0** | 140 (monitor LOD1) |
| 17.3 m | **96** | **0** | 96 (LOD1) |
| 20.3 m | **80** | **0** | 80 (LOD1) |
| 23.3 m | **60** | **0** | 0 (culled at 22.4 m) |

- Near and mid shots (0.85, 0.9, 2.8 m) still draw the quad: 198,033 / 115,367 / 11,015 px.
- Log: quad in LOD0 and LOD1 = True; LODGroup size 0.524 m; LOD0→1 at 3.354 m; cull at 11.179 m (High) and 22.358 m (Ultra). `FrontRoomsGlassRTSystem.InLod0(group, quad)` = True, so the glass RT still registers the quad.
- Owner note, not part of F7: at Ultra, between 18 m (the video's `visibleDistance`) and 22.4 m (the cull), a monitor can show a frozen frame if no nearer screen keeps the shared bank playing. That belongs to the screens rework in `SCREENS_VISUAL_SYSTEM.md`.

## 3. F2 · stale zone reflection cubes (deferred)

Still true on main `0a9ee7f`:
- `capture_manifest.txt` (unchanged since `8ef5b64`) records `FrontRoomsSurface.shader = 079f93ca0983`; today the shader's md5 is `d7e9168ea267…`.
- It lists the lens materials as `Map / Level 0 lens, Troffer_Lens` and the print as `static paper (no _FR_PRINT)`. The game now uses `Troffer_Lens_Cool` with the V5 light lead ON and has the `_FR_PRINT` layer.
- `FrontRoomsMapWorld.cs:3703` still sets `Level0LensEmissionScale = 1.5f`, which `CurrentInputs()` does not record.

Why deferred: the finding's handoff is "visual (glass owner), not this audit's Fix phase", and stale cubes do not break or visibly damage the game today. Exact steps for the owner (all visual-owned; no map edit):
1. Extend `CurrentInputs()` in `Assets/Editor/Rendering/FrontRoomsReflectionCapture.cs` with: the light lead (`FrontRoomsTransitionLightLead.On`, `Borders`, `Soft`, the Level 0 and Office colours and intensities, the Office lens name in use); the md5 of `Troffer_Lens.mat` and `Troffer_Lens_Cool.mat`; the md5 of the wallpaper `.mat` files that enable `_FR_PRINT`; and `Level0LensEmissionScale`, read by reflection (`typeof(FrontRoomsMapWorld).GetField("Level0LensEmissionScale", NonPublic | Static)?.GetRawConstantValue()`, or `missing`, so that `WarnIfStale` fires).
2. Recapture in a fresh `mkclone.sh` clone with `-buildTarget OSXUniversal`. Do not pass `-transitionsOff`, `-lightleadOff`, `-lightleadBorders` or `-lightleadSoft`: the Office cube must carry V5 as Red plays it. Re-check the 4 cell picks (the map changed since `a2c7b35`).
3. Run G10's subset check, promote only the 4 `Refl_*.exr` and the manifest, and keep main's `.exr.meta` (GUIDs and the WebGL RGB9E5 128 px override). Before/after frames go to the VERIFICATION LOG.
4. Recapture again after q1-live-print's print driver merges, and if Red picks a transition variation other than V5.

## 4. Rebase over COV-4 (2026-10-14)

Red's merge rules (a)–(c). There was no apply script for this track in `W/apply` yet (the 10-08 stage stopped at a draft), so the verified 10-08 package was installed as v2 with its 10-08 bases. The dry run then refused, as expected: `FrontRoomsScreenVideo.cs` had moved `515087bf → 37d6d223` (COV-4).

`W/tools/rebase_apply.sh codex_audit_glass_look --ios` → **PASS**:
- 3-way merge clean (COV-4 edits `MakeQuad`; this package edits the fields, `CreateSurface` and `MakeMaterial`); new payload `ea0c85cf`;
- Roslyn, target tree + payload: Assembly-CSharp and Assembly-CSharp-Editor 0 errors, 0 warnings, macOS and iOS defines;
- package installed, own dry run OK. Work dir: `W/tools/_rebase/codex_audit_glass_look/20261014-162021/`.

Re-verification after the clean rebase (rule c: compile plus this track's own gate; no new captures or critics):
- Unity batch compile of the re-synced clone, `-buildTarget OSXUniversal`: before the payload 0 errors, 0 warnings; with the payload 0 errors, 0 warnings (`W/logs/cxfix_glass_look_1014_compile_{before,after}.log`).
- `CxCrtFix` gate (`W/logs/cxfix_glass_look_1014_crt_after.log`): all 16 test-card shots and all 32 screen-off frames are **pixel-identical** to the 10-08 run. The plain shots differ only where the screen draws (max 23/255; their screen-off frames are identical), because the live video sits on another frame. Culled shots 0 px; LOD shots still draw.
- Combined-file check with fix group kits+window's harness `CxFixKwLeak` (`W/logs/cxfix_glass_look_1014_kwleak.log`): 1 quad mesh for 20 screens over 3 spawn/destroy cycles, 2 meshes for 2 sizes, every screen URP Unlit; LOD readout PASS 65 / FAIL 0; shader compile 48 / 48.

## 5. Verification summary

| Check | Where | Result |
|---|---|---|
| Unity batch compile, OSXUniversal | clone, 10-08 and 10-14 | 0 errors; 0 warnings before and after (no new warnings) |
| Roslyn post-apply compile | `rebase_apply.sh`, 10-14 | 0 errors, 0 warnings (macOS + iOS defines) |
| EditMode suites | clone, 10-08 (`W/logs/cxfix_glass_look_suites.log`) | Map 100/100 seeds; Relay nav 60/60; MapInteraction 152/0; LevelDesigner 138/0; FixtureTick 28/0; CameraRig 33/0; Captions 9/0; GlassShot 20/0; StreamVerification and GlassCompileCheck: no exception |
| Validator seeds | same run | Map validator PASS on 100 / 100 seeds (the task asks for 20); 11 determinism seeds (20261001–20261010 + 516574485): second world identical, drop + rebuild of 25 chunks identical |
| Play Mode 120 s console | clone, 10-08 (`cxfix_glass_look_play.log`) | 0 exceptions from game code. 2 environmental: FMOD `Master.strings.bank` not found (the clone has no built banks) and Unity's QuickSearch indexer `ArgumentOutOfRangeException` (editor internal). Neither touches the screens |
| CRT LOD gate (F7) | `CxCrtFix`, 10-08 + 10-14 | PASS (§2, §4) |
| Mac dev player + shader table (F6) | `W/cxfix_glass_look/player/`, 10-08 | PASS (§1) |
| Shader variants (F6) | `CxCrtShaderCompile`, 10-08 | 48 / 48 OK |
| Combined with COV-4 | `CxFixKwLeak`, 10-14 | PASS (§4) |

Note on the suites: the map validator ran 100 seeds; the determinism check ran 11. The map is not touched by this package (no map file, no rng use: `AttachScreen` runs after placement). The EditMode suites, the seeds and the Play run were done on the 10-08 base; per rule (c) they were not repeated after the clean rebase.

## 6. Files changed vs main (incl. `.meta`)

| Action | Path | sha1 before → after |
|---|---|---|
| add | `Assets/Resources/Media.meta` (folder GUID `559c9d02f97c94f6fb076880a2c9569a`) | ABSENT → `fc192900` |
| add | `Assets/Resources/Media/CRT_AdSurface.mat.meta` (GUID `e246c7627984a4a48b9f4bdb207abdb7`) | ABSENT → `0ba14a95` |
| add | `Assets/Resources/Media/CRT_AdSurface.mat` | ABSENT → `b8d02e1a` |
| replace | `Assets/Scripts/Media/FrontRoomsScreenVideo.cs` (+72 / −12 lines) | `37d6d223` → `ea0c85cf` |
| keep | `Assets/Scripts/Media/FrontRoomsScreenVideo.cs.meta` (GUID `93c5f64b…`) | `5b16edd7` |
| keep | `Assets/Resources.meta` | `9744a304` |
| keep | `Assets/Resources/Rendering/FrontRoomsScreenVideo.shader` + `.meta` (GUID `5c45f9d7…`) | `707084cb`, `2491d36b` |

Not touched: every map-owned file, `ProjectSettings/` (no Always Included Shaders), `Library/`, RenderSetup. Nothing listed in `W/LOCAL_PATCHES.md`.

## 7. Apply script

`/Users/redwang/FrontRoomsVisualWork/apply/codex_audit_glass_look.sh` (apply convention v2; files `_bases.txt`, `_keep.txt`, `_expected.txt`, `_payload/`, `_base/`, `.diff`).
- No argument: dry run. It checks every base by sha1, the kept `.meta` GUIDs and every payload file, then prints the actions. It writes nothing.
- `--apply`: backs up main's `.cs` to `W/backup/codex_audit_glass_look_<stamp>/`, writes the 4 files (`.meta` before its asset, the `.cs` last, in place with no temp file under `Assets/`), then prints the sha1 manifest and compares it with `_expected.txt`.
- It refuses (exit 1, nothing written) if a base or a kept `.meta` changed, if an ABSENT path exists, or if a payload changed. If every path already holds the payload it prints `ALREADY APPLIED`.
- Dry run on main `0a9ee7f` + tree, 2026-10-14 16:2x: `DRY RUN OK`. Tested `--apply` on a scratch copy under W: `MANIFEST OK`, then the dry run there said `ALREADY APPLIED`.

Command for the visual chat: `bash /Users/redwang/FrontRoomsVisualWork/apply/codex_audit_glass_look.sh --apply`

## 8. Hand-offs

- **Red:** rebuild Mac and iOS (TestFlight) after the apply. The 10-04 builds still carry Codex's older code.
- **Combine stage / WebGL track (COV-5 item 4):** one WebGL player build of the applied state. Check that the shader table maps `Universal Render Pipeline/Unlit` and `FrontRooms/ScreenVideo` to non-null PPtrs, and that the browser console shows 0 `ArgumentNullException` after an Office room loads. COV-5 item 6 (live video vs a poster on WebGL, a WebGL-only gate in `FrontRoomsOfficeKit.AttachScreen`, :851) stays with the WebGL track.
- **Glass owner:** F2, steps in §3.
- **平面视觉 (send, do not edit their file):** a one-line note for `GRAPHIC_VISUAL_TASKS.md` §3: "`Assets/Resources/Media/CRT_AdSurface.mat` is the material hook for the screen-ad pipeline (URP Unlit, white; the ad RT is bound at runtime). The CRT treatment will be revised later."
- **Screens rework (`SCREENS_VISUAL_SYSTEM.md`):** the 18 m pause vs the 22.4 m Ultra cull (§2 owner note). Drawing the picture through the model's `Prop_ScreenCRT` slot would also make the LOD patch unnecessary.

## 9. Images and VERIFICATION LOG

| VL | Figma | Check | Verdict | Images |
|---|---|---|---|---|
| VL226 | `3116:7020` | CRT ad quad culls with monitor | PASS | `images/fix_glass_look_f7_cull_sheet.jpg` |
| VL227 | `3116:7038` | CRT screen material in players | PASS | `images/fix_glass_look_f6_look_pair.jpg`, `images/fix_glass_look_f6_player_near.jpg` |

Both were placed 2026-10-14 in row 56 of FRONTROOMS · VISUAL VERIFICATION LOG (`2595:6093`); the section did not grow. The cover VL000 was updated from the canvas: 26 tasks, 224 checks, 651 images; the codex-audit legend now reads VL080–154, 226–227 · 18 checks. The player frame with the live ad (`player_near_video.png`) stays local only: the current ad shows a real trademark.

## 10. Clones and work files that can be removed after the apply

Nothing was deleted.
- `W/proj_cxfix_glass_look` (5.1 GB): the verification clone. Remove it once the apply has landed.
- `W/cxfix_glass_look/player/` (0.9 GB): the Mac probe build. Its `probe.txt` and frames are summarised here and in VL227.
- `W/cxfix_glass_look/scratch_target`, `scratch_backup`, `scratch_applied_manifest.txt`: the apply test on a scratch copy.
