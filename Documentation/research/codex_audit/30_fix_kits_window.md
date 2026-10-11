# 30 · Codex audit fix group "kits+window" (2026-10-08)

Fix stage of the codex audit for 4 confirmed findings: F1, F5, COV-4 and COV-5.

- **Base:** main `bc0aa79` plus the working tree, recorded 2026-10-08 19:42. This includes the uncommitted Codex 10-08 16:41 screen-shader fix and the exit-sign package, which landed in main at 19:40.
- **Clones:** `W/proj_cxfix_kits_window` (fixed) and `W/proj_cxfix_kits_window_base` (main as is). Both were made with `mkclone.sh`, then synced to the same main at 19:42. Their sources are byte-identical except for the one fix.
- **Bases:** `W/proj_cxfix_kits_window.bases.txt`.
- **Nothing was written into Red's project** except this report and `fix_kits_window/` (evidence). No commit, no push. Unity was never opened on main.

## 0. Result in one table

| Finding | Status | Change in this package | Owner of the rest |
|---|---|---|---|
| **F1** kit importer LOD cull | **FIXED BY LATER COMMIT `663e858`** (Red's commit, 10-04 09:40, `GetVersion() => 2`). Verified again here on today's main: 23/23 sidecar-driven LODGroups match the spec | none | window-landing: nothing left to land for F1 |
| **F5** key cabinet inside-out corners | **DEFERRED** to interactables-kit. The asset is inert: nothing references `Kit_KeyCabinet`. Main still holds the faulty `f7e570c7…` | none | interactables-kit. Its pending package `W/apply/interactables_kit.sh` (19:35) already replaces it with `29d8332f…` and keeps the `.meta` |
| **COV-4** CRT quad mesh leak | **FIXED** in the clone. Apply script ready (dry run OK) | `Assets/Scripts/Media/FrontRoomsScreenVideo.cs`: +11 lines in `MakeQuad` only | — |
| **COV-5** CRT ads on WebGL | **Code fix is glass-look F6** (parallel batch; it reached the same file). The shader compile on every target, WebGL and WebGPU included, was **verified here (48/48 OK)**. The WebGL player-build check is **left open** (see §4) | none | glass-look F6 (code); combine stage (`codex_audit_all`) for the WebGL build check; WebGL track for live video vs poster |

Map-owned problems in this group: **none**, so there is no `contracts/*.diff`.

Visible changes: **none.** COV-4 changes no pixels, because the quad's geometry is identical (§2). So there are no `images/fix_kits_window_*.jpg` and no new VERIFICATION LOG slide. That matches COV-4's corrected fix: "needs no new VL slide, because it makes no new images".

## 1. F1 · `FrontRoomsKitImporter.cs` LOD cull mapping

**State on main.** `663e858` "Fix prop kit LOD cull mapping" (Codex session, committed as Red on 10-04 09:40) landed window-landing r4's patch:
- the last imported level takes the sidecar's **last** entry; 0, a negative value or `null` means never culled;
- `GetVersion() => 2`.

`1114d6b` records the hand-off in `interactables/window_landing/04_promotion.md`. Nothing after it touches the importer. Main's file sha1 is `08daf35f…`.

**Review of `663e858` (same rigour as Codex's commits):**
- The loop runs over `0…last`. Level `i < last` takes `lodDistances[i]`; the last level takes `lodDistances[^1]`.
- A non-last level with a distance of 0 or less keeps Unity's own height. The last level gets h = 0 (never).
- The guard `lodDistances.Length >= lods.Length` stays, so a 2-entry sidecar on a 3-LOD model falls back to the default spec. No sidecar in main has that shape.
- `JsonUtility` reads the window sidecars' `null` as 0. The readout below proves it: all 4 frames read `[4, 12, 0]`.
- Nothing else changed. The default spec (`.10` / `.02` or `.03`) is untouched.

**Readout on today's main** (clone harness `CxFixKwLeak.LodReadout`, Library as imported by the current importer, no forced re-import; `fix_kits_window/f1_lod_readout.txt`):

| Kits | Sidecar | LOD0 → LOD1 | Cull |
|---|---|---|---|
| 9 × `Kit_DoorFrame_*` / `Kit_DoorLeaf_*` | `[4, 12, -1]` | 4.0 m | never (h 0.0000) |
| 4 × `Kit_WindowFrame_*` | `[4, 12, null]` | 4.0 m | never (h 0.0000) |
| `Kit_MiniBlind_Raised` / `_Lowered` | `[3, 10, 30]` | 3.0 m | 30.0 m (h 0.0331 / 0.0512) |
| 8 × `Kit_ExitSign*` (landed 19:40) | `[5, 0, 0]` | 5.0 m | never |
| 45 other kits with a LODGroup | none | default spec | default spec (unchanged) |

- **23 PASS, 0 FAIL.**
- The audit's probe values hold exactly: `Kit_WindowFrame_Wood` L1 → cull h = 0, and `Kit_MiniBlind_Raised` h = 0.0331.
- All distances are at lodBias 1 (Editor High). Mac/Win builds run Ultra (lodBias 2), which doubles them.

**Not checkable from here:** whether Red's own Library has re-imported at version 2. His editor will do that once, on its next refresh. The `04_promotion.md` line "actual FBX re-import/readout is still pending in the open Editor" can now point to this readout.

## 2. COV-4 · CRT ad quad leaks one Mesh per monitor

**Cause (unchanged since `b5f381f`).** `CreateSurface` calls `MakeQuad`, and `MakeQuad` builds a `new Mesh` for every monitor. `OnDestroy` frees only the material, so every Office chunk teardown leaks one mesh per dressed CRT.

**Fix (minimal; visual-owned).** Every monitor of one screen size now shares one quad:
- the quads live in a `static Dictionary<Vector2, Mesh>` keyed by `screenSize`;
- a Unity null check rebuilds a quad if anything ever destroys it;
- the shared quad is never destroyed per screen.

This is the reviewers' "valid alternative". The diff is `fix_kits_window/cov4_FrontRoomsScreenVideo.diff`: +11 lines, all at the top of `MakeQuad`. I chose it over the per-instance field and `OnDestroy` version for three reasons:
1. **It does not touch `CreateSurface`, `MakeMaterial` or `OnDestroy`,** which the parallel glass-look F6/F7 batch rewrites. I tested a 3-way merge against glass-look's work-in-progress file (sha1 `43b78900…`, 19:45): it is clean in both orders, the two results are byte-identical, and Roslyn compiles the merge with 0 errors.
2. **The G14 RT system keys its BLAS cache by mesh** (`FrontRoomsGlassRTSystem.AcquireMesh`). One shared quad means one BLAS upload instead of one per monitor.
3. **No mesh is ever destroyed while the RT system may still hold it.** The per-instance version would free each quad in `OnDestroy`, possibly before `DropChunk` releases it.

**Evidence: spawn/destroy harness** (`CxFixKwLeak`, empty Play-mode scene, real `Kit_CRTMonitor` + `Attach`; `cov4_leak_before.txt` / `cov4_leak_after.txt`):

| Step | Main (meshes alive) | Fixed (meshes alive) |
|---|---|---|
| 20 monitors spawned (cycle 1) | 20 | 1 |
| all 20 destroyed, +2 frames | 20 | 1 |
| cycle 2 spawned / destroyed | 40 / 40 | 1 / 1 |
| cycle 3 spawned / destroyed | 60 / 60 | 1 / 1 |
| plus a 0.30 × 0.20 screen (2 sizes live) | 62 | 2 |
| after `Resources.UnloadUnusedAssets` | 0 | 2 (kept by the static; reused) |
| one more monitor | 1 | 2 (reused, no new mesh) |

- **Geometry is identical before and after:**
  - verts ±0.14 × ±0.095;
  - UV U-flip `(1,0) (0,0) (0,1) (1,1)`;
  - tris `0,1,2,0,2,3`, normals +Z, readable;
  - local position `(0, .248, .198)`, trigger collider present.
- With the fix, 20 live screens use **1** distinct mesh. Before, they used 20.
- In both runs the material's shader is `FrontRooms/ScreenVideo`: the uncommitted 10-08 fix loads it first, in the editor too (COV-5).

**Evidence: 120 s map run**, seed 516574485, the game's autopilot, with a clone-only sampler every 10 s (`cov4_play120_sampler_*.tsv`):
- **Main:** quad meshes rise 0 → 8 → 9 → 16 while live screens fall to 0. Over 50 chunk teardowns, 16 meshes are orphaned.
- **Fixed:** quad meshes stay at **1** at every sample, with up to 39 live screens, through 120 chunk teardowns and 3 restarts after a catch (scene reloads).

**Merge note.** Glass-look's F6/F7 batch edits the same file. Whichever package lands second refuses on its base check. Then run `W/tools/rebase_apply.sh codex_audit_kits_window`, or the glass-look equivalent. The merge is clean, as tested above.

## 3. F5 · `Kit_KeyCabinet.fbx` inside-out corners (deferred)

- Main still holds `f7e570c7…` (161,628 B), the G3 second run.
- Nothing references the kit (grep: only its own JSON), so nothing in the game shows it today. Per the fix-phase rule, it is **deferred to interactables-kit**.
- Its pending package `W/apply/interactables_kit.sh` (19:35) carries:
  - the FBX `29d8332f…` (proj_int, 10-08 00:39; newer than the `6f35795b…` named in the finding);
  - a new JSON `028d8712…` (changed `hungPose`, `trianglesLod1` 6804, `lodDistances [3, 8, 40]`);
  - it keeps the `.meta` `dd0e0f8e…` (GUID `13c85fe1…`).
- **Two notes for that merge:**
  1. The new JSON's `slots` list a 4th slot, `Prop_KeyTagNo`. No `Prop_KeyTagNo.mat` exists in main or in the package, and RenderSetup has no def for it. The kept `.meta` remaps only 3 slots. So that slot imports with the FBX's own material description. Check it before the kit is spawned anywhere.
  2. Run the fold-back check named in the finding (113 edges on `f7e570c7…`; it must be 0) on `29d8332f…`, not on `6f35795b…`.

## 4. COV-5 · CRT ads on WebGL (code fix = glass-look F6)

- **What is true now.** Red's working tree holds an uncommitted Codex fix (10-08 16:41):
  - `MakeMaterial` loads `Resources/Rendering/FrontRoomsScreenVideo.shader` (`FrontRooms/ScreenVideo`) first, and returns null with an error if it finds no shader;
  - the untracked `.shader` and `.shader.meta` (guid `5c45f9d7…`) are new.
  - So the `ArgumentNullException` is gone on every target once Red commits them. Keep the `.meta` with the shader.
- **What it costs.** In the editor too, the ads now draw with that minimal shader. It has no fog, no SSAO and no depth passes, so the desktop look changed a little.
- **Glass-look's F6 batch handles it.** It runs in parallel and edits the same file (bases `W/proj_cxfix_glass_look.bases.txt`). It adds `Resources/Media/CRT_AdSurface.mat` (URP Unlit) as the **first** choice, with the custom shader as the fallback. It also makes the material first, with a warn-once null guard (no quad and no collider without a material). That restores URP Unlit's fog, SSAO and depth passes in the editor and in every build. **COV-5 items 1–3 are therefore owned and fixed by glass-look F6.** I made no second, conflicting edit: no shader parity edit, because their package keeps the shader as is.
- **Item 4, verified here** (`cov5_shader_compile.txt`, `ShaderData.Pass.CompileVariant`). All **48 of 48** compiles are OK, for vertex and fragment:
  - `FrontRooms/ScreenVideo` (pass `FrontRoomsScreenVideo`) and `Universal Render Pipeline/Unlit` (pass `Unlit`; keywords none, `FOG_EXP2`, `FOG_EXP2 _SCREEN_SPACE_OCCLUSION`);
  - targets: GLES3x / WebGL, WebGPU / WebGL, Metal (OSX and iOS), D3D / Win64, Vulkan / Linux64.
- **Item 4, still open: a WebGL player build of the combined state.** It must check:
  - that the `globalgamemanagers` shader table maps both `Universal Render Pipeline/Unlit` and `FrontRooms/ScreenVideo` to non-null PPtrs;
  - that the browser console shows 0 `ArgumentNullException` after an Office room loads.
  - Why it is not done here: the machine ran at load 470–585, and the material it must check (glass-look's `CRT_AdSurface.mat`) is not in this package. It belongs in the combine stage (`W/proj_cxapply_all`), or to the WebGL track.
- **Item 5:** keep `com.unity.modules.video` in `Packages/manifest.json`. Every target needs it to compile `UnityEngine.Video`.
- **Item 6, WebGL track:** whether WebGL monitors decode video at all (one 640 × 434 RT on a draw- and CPU-bound tier). If not, gate it in `FrontRoomsOfficeKit.AttachScreen` (:851) with `#if !UNITY_WEBGL || UNITY_EDITOR`, WebGL only. That also covers the title corridor (`RoomStream` → `BuildStation`). The OfficeKit change since `75cfdff` (`04c3a6a` attach; `bc0aa79` time-sliced `b.Kit(…, AttachScreen)`) consumes no rng, so it does not touch layout determinism.

## 5. Verification

| Gate | Result |
|---|---|
| Batch compile, `-buildTarget OSXUniversal` | 0 `error CS`, 0 `warning CS`, fixed and main alike (`W/cxfix_kw/logs/A1/A2/B1`). Roslyn outside Unity: OSX and iOS defines, 0 errors, 0 warnings |
| EditMode suites + 20 validator seeds (`FrontRoomsCxSuites`) | **Same as main.** All 10 suites ran with no harness exception, and every report is identical before and after: camera-rig 33/0, captions 9/0, fixture-tick 28/0, glass-shot 20/0, level-designer 138/0, map-interaction 152/0, map-verification 100/0, relay-nav 0 failures, stream 5/0. GlassCompileCheck ran too. MapInteractionTests' 2 errors and 4 exceptions are its own injected failures ("forced build failure", "a broken glass kit"), the same on main. **20 seeds:** 0 second-world and 0 rebuild determinism failures, and `map_signatures.tsv` is byte-identical to main's (`fix_kits_window/suites_*.txt`) |
| 120 s Play Mode console (`FrontRoomsCxPlayRun`, seed 516574485) | **0 new exceptions.** Fixed: 2 exceptions (FMOD `BankLoadException`: clones carry no banks; Unity QuickSearch `ArgumentOutOfRangeException`: editor start). Main: the same 2. Both are the expected pair (TOOLS.md §2). Warnings: only FMOD port-in-use, the PanelSettings theme and 2 map "module prop would block an opening" lines, present in both runs |
| Before/after frames | none: no visible change (§0) |
| Apply script | `bash /Users/redwang/FrontRoomsVisualWork/apply/codex_audit_kits_window.sh` → **DRY RUN OK**. `--apply` tested on a scratch copy: MANIFEST OK, then ALREADY APPLIED. `rebase_apply.sh codex_audit_kits_window --no-install` → PASS, nothing to rebase |

**Notes on the runs:**
- **The two 120 s runs took different paths.** Fixed: 3 catches, 120 chunk teardowns. Main: 0 catches, 50 teardowns. The autopilot is not deterministic run to run: earlier same-code runs in `runtime/play/cx_rton*` gave 0–2 catches and 55–125 teardowns. The fixed run's extra map states are a harder test of the fix, not a regression.
- **Its one "pink" renderer** (`Chunk/Key`, null material, at map 43 s after the first restart) is pre-existing, map-owned, and appears in 7 earlier audit runs.
- **The first fixed-clone run (A1) aborted at exit with code 134** after its outputs were written. Mono printed `Assertion … mono_domain_set` on a job worker thread during the Play-mode domain reload. It did not recur in the same clone's next three runs (A2 play, A3 suites, A4 leak re-run, all exit 0), and it never appeared in main's clone. Nothing in this change runs on a thread.
- **Which payload each run used.** A1 ran on the first payload text, which differs from the final one only in one comment line. A2, A3 and A4 ran on the final payload (`37d6d223…`). A4 repeated the leak, LOD and shader checks on it, with identical results; those are the copies in `fix_kits_window/`.

## 6. Files changed vs main

| Path | Base sha1 (main) | After sha1 | `.meta` |
|---|---|---|---|
| `Assets/Scripts/Media/FrontRoomsScreenVideo.cs` | `515087bfb0a996167e1a27c12823d7d1ec42c222` | `37d6d223e5b9ee3816f3a8f7883c89e0eb5929f1` | `FrontRoomsScreenVideo.cs.meta` kept, sha1 `5b16edd7…`, GUID `93c5f64b49a54463b4a8cea1be0fad04` |

**Package (convention v2).**
- Script: `W/apply/codex_audit_kits_window.sh`
- Data: `…_bases.txt`, `…_keep.txt`, `…_expected.txt`, `…_payload/`, `…_base/`, `…diff`

**Commands:**
- Dry run: `bash /Users/redwang/FrontRoomsVisualWork/apply/codex_audit_kits_window.sh`
- Apply: `bash /Users/redwang/FrontRoomsVisualWork/apply/codex_audit_kits_window.sh --apply`. It prints the sha1 manifest.

**Not in the package:**
- the clone-only harness (`Assets/Editor/Audit/CxFixKW/*`; copies in `fix_kits_window/*.cs.txt`);
- the untracked Codex shader files, which Red commits with his next "1".

## 7. Hand-offs

- **Glass-look F6/F7 (this audit's parallel batch).** Same file, other hunks. The 3-way merge is tested clean. COV-5 items 1–3 are yours.
- **Combine stage (`codex_audit_all`):**
  1. Apply both ScreenVideo packages.
  2. Run one WebGL player build of the combined clone and check the `globalgamemanagers` shader-table mapping (§4 item 4).
  3. Re-run `CxFixKwLeak` on the combined file. Expect 1 mesh for 20 screens, and 2 for 2 sizes.
- **Interactables-kit:** F5 is already in your package. See the two notes in §3.
- **Window-landing:** F1 is done in main (`663e858`). Update `04_promotion.md`: the readout is no longer pending; it is in `fix_kits_window/f1_lod_readout.txt`.
- **WebGL track:** COV-5 item 6 (live video vs poster; a WebGL-only gate).
