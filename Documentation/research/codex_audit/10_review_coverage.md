# Codex audit · 10 · Review: coverage (completeness critic)

| | |
|---|---|
| Date | 2026-10-08, 00:40–01:50 PDT (retry; the two earlier attempts of this stage stopped on usage limits before writing anything) |
| Track | coverage: does every path, hunk and claim of Codex's work have a review behind it, and is the evidence strong enough |
| Inputs | `00_files.tsv` (337 rows, `7320ed1` → `75cfdff`), `00_main_state.md`, the seven `10_review_*.md`, `NOTE_map_chat_attribution.md`, `RED_DECISIONS.md`, `git log 75cfdff..HEAD` |
| Reviewed at | main HEAD **`22bb75f`** (2026-10-07 23:46). Red's working tree also has uncommitted edits by other tracks (office-dress-hitch merge in `Office/*` + `MapWorld.cs`, Relay work in `MapHunter.cs`); those are not reviewed here |
| Red's project | Read only. Unity was not opened on it or on any clone. Nothing committed, moved or deleted. Files written: this report and `coverage/*.txt` |
| Work dir | `W/cov/` (`W` = `/Users/redwang/FrontRoomsVisualWork`): HEAD sources by `git archive`, Roslyn outputs, patch dry-runs |
| `20_findings.md` | does not exist yet. The status table in §6 is written so the report stage can copy it |

---

## 0. Short answer

- **Every one of the 335 changed paths has a review behind it.** 146 of the 337 rows in `00_files.tsv` are named in a review. The other 191 are covered by group checks: the 212 interactables kit files (sha1, GUIDs, tris, LOD, slots, sidecar types), the glass files (hashes, shader compile), and G14 sources (byte-identical dylib rebuild, per-target compile). All 11 MapWorld hunks and every hunk in the 16 modified files were examined.
- **Seven gaps were real:**
  1. Three reviews (kits, map-edits, docs) ran on `75cfdff` and were never re-run at HEAD. I re-checked their findings at HEAD (§6).
  2. Nobody compiled for Linux, Android or iOS at HEAD. The runtime review's "player" compile kept the `UnityEditor` references, so it could not catch editor API in game code.
  3. Nobody checked GUID references across the whole project at HEAD.
  4. Nobody looked at player **packaging** or at the player logs on this Mac.
  5. Nobody measured what Codex's map content adds to draw load.
  6. Codex's later commits `0d5bf54`, `c1f2d31` and `04c3a6a` changed `ProjectSettings`, `Packages` and build scripts. No review read those changes.
  7. The map contract patches MAP-2 and MAP-3 were never compiled.
- **I closed the gaps.** Results:
  - **Compile.** All 6 player targets compile at HEAD with the `UnityEditor` references removed: Mac, Win64, Linux64, iOS, Android, WebGL. 0 errors.
  - **GUIDs.** 1,136 GUID references, 0 unresolved (1 FMOD icon reference aside). 0 duplicate GUIDs in 1,182 metas. 0 GUID changes in the modified metas since `7320ed1`.
  - **Load paths.** Every `Resources.Load` and `Shader.Find` target exists, except glass-look F6's URP/Unlit.
  - **Editor API.** 0 unguarded uses in runtime scripts.
  - **Map contracts.** MAP-2 and MAP-3 still apply to HEAD and compile.
- **New findings:**
  - **COV-1 · major.** G14 never runs in a macOS **player**. The dylib is arm64-only, its importer pins CPU ARM64, and the build scripts make universal (x86_64 + arm64) apps, so Unity leaves the plugin out. Evidence:
    - Red's own Mac player log: `[FrontRoomsGlassRT] off: plugin not loaded`.
    - The 10-04 app bundle holds no GlassRT file.
    - This also corrects `10_review_rt.md`, which says every Mac build shows RT at Ultra.
  - **COV-2 · major, latent, not Codex.** The menu item "FrontRooms 3D/Build macOS" sets both URP pipeline slots to null before it builds. That gives a build without URP and leaves Red's editor without URP.
  - **COV-3 · minor.** The only Mac player on disk crashes at launch: `sharedassets0.assets is corrupted`. No working Mac player of Codex-era code exists.
  - **COV-4 · minor.** Every Office CRT ad leaks its quad mesh when its chunk unloads (Codex `04c3a6a`).
  - **COV-5 · minor.** Glass-look F6 also hits WebGL. The ads have no WebGL gate, and the video module now ships to every target.
  - **COV-6 · minor.** Codex's map content adds **+4.0 % renderers** (+2,819 over 500 chunks, up to +45 per chunk). Lights and colliders are unchanged. Nobody measured the draw cost; it matters for the draw-bound WebGL tier.
  - **COV-7 · decision.** The desktop stamina bar went from 6 px to 5 px tall in Codex's iPhone HUD commit, ungated. Still at HEAD.
- **Later commits that fixed Codex errors:**
  - `663e858`: the 12 m kit cull (kits F1).
  - `9eddc35`: the missing prepass. G14 now traces windows, which also makes docs claim C3 true.
  - `22bb75f`: the hidden desktop key panel (visual V1, caused by Codex's `c1f2d31`).
  - `44fffd1`: the `.pyc` ignore rule (docs H1, half fixed).

  Every other review finding is still open at HEAD (§6).

## 1. Coverage matrix (`00_files.tsv` groups × reviews)

✔ = examined with its own evidence · ○ = touched or cited only · — = not examined. Depth notes say what was actually run.

| Group (rows) | kits | map | docs | glass | rt | runtime | visual | What was actually done / what was missing |
|---|---|---|---|---|---|---|---|---|
| Glass G1–G6 (47, V) | — | ○ | ✔ | ✔ | ○ | ✔ | ✔ | md5 of all 24 files + metas; `ForwardLit` 60 variants × 5 targets byte-identical; RenderSetup glass steps re-run; 28-variant compile check at `ee5c9bb`; frames. **Not done:** `FrontRoomsReflectionBlend` compiled only for GLES3 and Metal; the code of `ZoneReflection`, `ZoneReflectionDriver` and `GlassPane` is checked by hash only; `Tools/lookdev/pack_glass_grime.py` was never read |
| G14 RT (35: 30 W, 2 E, 3 X) | — | ○ | ✔ | ○ | ✔ | ✔ | ○ | dylib rebuilt byte-identical; per-target compile at `279c144`; fault injection; walk timings; proof re-run. **Not done:** the content of `Hidden/FrontRooms/GlassRTPrepass` (compiled for Metal in Red's editor only); `Editor/RT/FrontRoomsGlassRTIsolation.cs` (I read it: batch only, no load hook); **player packaging** (COV-1) |
| N1 transitions (7 + RenderSetup L317 + 9 MapWorld hunks) | — | ✔ | ✔ | ○ | — | ✔ | ✔ | hunk diff against the clone; Python B0 model; map signatures (20 seeds); 4 fixed shots. **Not done:** MAP-2 notch never captured; MAP-3 never run under its flags; patches never compiled (now compiled, §4.6) |
| W1 window models + sidecars (22) | ✔ | — | ✔ | ✔ | — | ○ | ✔ | sha1, GUIDs, sidecar parse, LOD probe (8 kits), forced re-import at version 2 (VL114), distance frames |
| Window facade (2) + MapWorld hunk 6 | ✔ | ✔ | ✔ | — | ○ | ✔ | ✔ | r3 code read (kits §3); hunk 6 (map). At HEAD the facade is **r6** (`ee5c9bb`). Its only re-checks are the runtime map signatures (0/500 chunks differ) and window-landing's own merge. Nobody re-scored the 25 critic issues against r6 |
| Interactables kits G1–G3 (212) | ✔ | — | ○ | — | — | ○ | ○ | group checks only: sha1 = `proj_int`, fresh GUIDs, tris ±15 %, LOD groups, missing slots (F4). 3 sidecars parsed in Unity, 5 FBX LOD-probed. No kit rendered: they are inert, so this is acceptable |
| LOD glue (KitImporter, KitLibrary) (2) | ✔ | — | — | ✔ | — | ✔ | ✔ | F1 probed; `663e858` re-imported and rendered (VL114) |
| MapWorld (1 row, 11 hunks) | ○ | ✔ | — | — | ✔ | ✔ | ○ | all hunks; determinism (signatures, 2 builds + rebuild). **After `75cfdff` nobody re-read it.** `b5f381f` (+48 lines, Relay path field) does not overlap any Codex hunk (I read the diff) |
| RoomStream (1, V+O) | — | ✔ | — | ✔ | — | ○ | ✔ | glass hook (F3), `a7b0dbb` line |
| Docs + lock + webgl plan (3) | ○ | ○ | ✔ | — | ○ | ○ | — | all claims checked at `75cfdff`; **never re-checked at HEAD** (§6) |
| Other session (2) | — | ✔ | — | — | — | — | — | out of scope (confirmed not Codex) |

### 1.1 Commits after `75cfdff`

| Commit | Touches | Reviewed by | Note |
|---|---|---|---|
| `663e858`, `1114d6b` | KitImporter LOD fix + handoff note | glass-look (VL114), runtime (LOD dump), visual (distance frames) | complete. I read `1114d6b`: accurate |
| `9eddc35`, `bf2e883`, `70644f0` | `FrontRooms/Glass` G14 hook | glass-look (60 variants × 5 targets), rt (proof VL119, frames VL116), runtime (Play runs) | complete |
| `04c3a6a`, `4448df9`, `b5f381f` (Media, OfficeKit, ads demo scene) | CRT ads | glass-look F6/F7, visual V3, rt F5 | **COV-4, COV-5 missed**; `Packages/manifest.json` (+video module) and `EditorBuildSettings.asset` (+demo scene, disabled) were read by nobody (§4.7) |
| `c1f2d31` | iPhone HUD, build scripts, ProjectSettings | visual V1 | stamina height (COV-7) and the build-script and settings edits were read by nobody (§4.7) |
| `0d5bf54`, `990c84c`, `a50abb4`, `756a031`, `279c144` | mobile builds, ProjectSettings, `FrontRoomsLevel0.asset` re-serialised, app icons in `Resources/Brand`, FMOD banks | none | I read the settings and the level asset (§4.7). The FMOD banks belong to the sound chat and are not reviewed here |
| `44fffd1`, `6d26911`, `66e0bb9`, `f04acc5`, `86cdc5c` | Unreal migration (outside `Assets`, plus one editor exporter), `.gitignore` | none | no load hook, editor only; `.gitignore` gains `__pycache__/` (docs H1, half) |
| `6ca3b3c`, `b5f381f` (MapHunter, PathField) | Relay Step 1 (map-owned) | none | not Codex's promotion; no overlap with Codex's MapWorld hunks |
| `16f520e` … `6c6fe81`, `ee5c9bb`, `22bb75f` | touch, print array, facade r6, key-panel fix | runtime (compile, suites, Play runs), visual (touch commits change nothing on desktop) | `16f520e` added 15 tracked FMOD bank conflict copies (4.4 MB) to `StreamingAssets`; the iCloud chat owns those |

## 2. Paths, hunks and claims that no review examined

| Item | Risk | What I did |
|---|---|---|
| `Assets/Shaders/GlassRT/FrontRoomsGlassRTPrepass.shader` (`Hidden/FrontRooms/GlassRTPrepass`) | low | Read it: 91 lines, `#pragma target 3.5`, one pass. Since `9eddc35` every receiver uses `FrontRooms/Glass`'s own pass (runtime run: 68 on bit 30, **0 on override bit 29**), so this shader is unused today. It still ships in iOS and Windows builds through the renderer asset (rt F6). Nobody compiled it, or `ReflectionBlend`, for D3D11, Vulkan or iOS Metal. Handoff: glass-rt-track. Add those targets to `FrontRoomsGlassCompileCheck` |
| `Assets/Editor/RT/FrontRoomsGlassRTIsolation.cs` | none | Read it: batch only. It writes `Verification/rt_p0p1/isolation.txt` when run. No load hook |
| `ZoneReflection`, `ZoneReflectionDriver`, `GlassPane` code | low | Read the lifetime code: static RTs and material, released on `Application.quitting` and reset on `SubsystemRegistration`; a Single scene load forgets the zone. No leak across R restarts |
| `Tools/lookdev/pack_glass_grime.py`, `NativePlugin/FRGlassRTLayout.h`, `GlassRTNative.cs` | none | Covered by result: the textures are hash-verified, the dylib rebuilds byte-identical, and the per-target compile passes |
| Codex's `"placement": "Wall"` (6 sidecars) | none | `"Wall"` is a palette value the Level Designer handles (`FrontRoomsModuleEditing.cs:109-111`; `LevelDesignerTests` 138/138 at HEAD) |
| `ProjectSettings`, `Packages`, build scripts after `75cfdff` | see §4.7 | read |
| Codex claims in later docs (`04_promotion.md` via `1114d6b`) | none | accurate when written |

## 3. Reviews whose evidence is thin

| Review | Thin point | Effect | Closed here? |
|---|---|---|---|
| kits (`75cfdff`) | Unity probe on 8 of 59 kits; 56 sidecars "parsed" only via Red's log having 0 JSON errors plus a static type check. §3.2 scores the critics against **r3**; main runs **r6**. "0.5 mm coplanar trims are fine on Metal (reversed Z)" is reasoned | F2's z-fight risk and the r6 critic status are unproven. Visual saw the trim as a second casing step in stills; nobody looked in motion | partly (§6 status) |
| map-edits (`75cfdff`) | No Unity run. MAP-2 rates (19.7 % / 13.1 %) come from `b0_model.py`, which was lost in the 10-05 wipe. MAP-3 test failure reasoned. Patches never compiled | MAP-2 cannot be re-derived; MAP-2 and MAP-3 handoffs were unproven | **yes for the patches:** they apply to HEAD and compile (§4.6). MAP-2 still needs the two-visit capture |
| docs (`75cfdff`) | Never re-checked; its scratch evidence (`scratchpad/ca/`) was wiped | Its §4 replacement text was never applied | status in §6 |
| glass-look (`a5262fb`) | RenderSetup "byte-identical" run predates `3ff05ee`, which patched RenderSetup. Shader checks cover `FrontRooms/Glass` only. F6 build evidence is Mac + iOS | small: runtime's `ee5c9bb` compile check re-runs the glass steps (Prop_Glass, BottleBlue, Glass_Window still on `FrontRooms/Glass`). WebGL: COV-5 | partly |
| rt (`ee5c9bb`) | Timings under load 480–860 (stated). The per-target compile ran at `279c144`. **States "no player build" and "every Mac build now shows unreviewed RT output … the macOS player defaults to Ultra"** | wrong for players: G14 never loads there (COV-1) | **yes** (COV-1); compile re-done at HEAD |
| runtime (`22bb75f`) | "Player Mac / WebGL" compile only swaps defines and keeps the editor reference assemblies (`W/cx_rt/csc_check.py` docstring: "approximation: same reference assemblies"). 1 seed, 1 route; no whole-frame timing | editor-only API in game code would have passed | **yes:** 6 targets, editor refs removed, 0 errors (§4.1) |
| visual (`6c6fe81`) | 84 stills, 1 seed; 7 of 12 window close-ups inspected by eye; no motion or flicker; MAP-2 revisit and the §3.8 start-area corner not shot | the z-fight and notch risks are open | no (needs captures; owners in §6) |

## 4. Categories nobody looked for, and what I found

### 4.1 Player compile on every target (Linux, Windows, iOS, Android, WebGL, Mac)
- **How.** `W/cov/player_compile.py` (copy: `coverage/player_compile.py.txt`). It uses Unity's Roslyn with W/proj_audit's `Assembly-CSharp.rsp` options (synced 00:29 to `22bb75f`). Sources: HEAD via `git archive` (73 game scripts). **All 80 `UnityEditor` / `*.Editor` / test reference assemblies removed**, and the editor defines replaced by each target's set. Android also gets `UnityEngine.AndroidJNIModule`, which a real Android compile references.
- **Result** (`coverage/player_compile_6targets_22bb75f.txt`): Mac, Win64, Linux64, iOS, Android, WebGL: **0 errors each**. Warnings: 4 on Mac, Win64, Linux64 and WebGL, all in the touch file `FrontRoomsTouchControls.cs` (touch session's). 0 on iOS and Android.
- A static scan agrees: 0 `UnityEditor` uses outside `#if UNITY_EDITOR` in runtime scripts. The FMOD `InternalsVisibleTo` string is the only hit.
- **Limit:** player-specific `UnityEngine` reference assemblies and IL2CPP AOT issues are not modelled.

### 4.2 GUID references across the project
- `coverage/guid_check_22bb75f.txt`: 1,315 YAML and meta files at HEAD; 1,182 asset GUIDs plus all package GUIDs from `proj_audit/Library/PackageCache`.
- 1,136 references: **0 unresolved** (only FMOD's own `FMODEventPlayable.cs.meta` icon reference, pre-existing).
- **0 duplicate GUIDs.**
- **0 GUID changes** in the metas modified since `7320ed1`. Nothing was moved or deleted under `Assets/`.
- The scene reference to `FrontRoomsGlassRTCamera` in `FrontRoomsScreenAdsDemo.unity` resolves; its off-Mac missing script is rt F5.

### 4.3 Resources and `Shader.Find` load paths
- Every literal target exists:
  - `Surfaces/Glass_Window`, `Rendering/FrontRoomsReflectionBlend`, `Rendering/FrontRoomsPost*`, `Brand/*`, `Fonts/*`, `Lighting/VolumetricBeam`, `Audio/door-creak`;
  - `Hidden/FrontRooms/GlassRTPrepass`, which the renderer asset references.
- The one exception is `FrontRoomsScreenVideo.cs:210` URP/Unlit, which is in no build (glass-look F6).
- `Resources.Load<VideoClip>("Videos/…")` has no folder, but the code falls back to the `StreamingAssets` URL by design.

### 4.4 Load-time and edit-mode hooks
- No new `[InitializeOnLoad]` came from Codex's 10-03 files beyond the two gated harnesses (docs §3).
- `04c3a6a`'s `FrontRoomsScreenAdsDemoScene.CreateIfRequested` acts only when a request file exists.
- `FrontRoomsPlayModeShaderCompile` is pre-Codex.

### 4.5 IL2CPP stripping against reflection
- iOS stripping is Low; WebGL is High (`FrontRooms3DBuild.cs:140`).
- Codex's code uses no reflection, and `DressWindow` is a compile-time call, so it is safe to strip.
- In the iOS build of 10-04, `BuildStation` (reached only through `Dress`) is in `global-metadata.dat`. So the map's reflection-bound `FrontRoomsOfficeKit.Dress` survived Low stripping.
- **Note for office-dress-hitch (not Codex):** the uncommitted MapWorld now binds `BeginDress` / `BeginBuild` by reflection. They are kept only while `FrontRoomsDressJob.cs:199/201` stays reachable. Check them in a WebGL High-stripping build.

### 4.6 Map contract patches at HEAD
- `coverage/map_contract_patch_check.txt`: the three diffs in `10_review_map-edits.md` dry-run cleanly on HEAD's files: MAP-1 (moot, Red keeps V5 on), MAP-2, MAP-3.
- HEAD with MAP-2 + MAP-3 applied compiles: `W/tools/roslyn_check.py`, Assembly-CSharp 73 files + Editor 51 files, 0 errors, 0 warnings.
- Behaviour is still unproven: MAP-2 needs the two-visit capture, MAP-3 the flagged test run.

### 4.7 ProjectSettings, Packages and build scripts changed after Codex
| Change | Commit | Effect on desktop | Verdict |
|---|---|---|---|
| `productName` FrontRooms3D → FRONTROOMSSS; Standalone/WebGL id `com.redwang.frontroomsss` | `0d5bf54`, `c1f2d31` | PlayerPrefs move to a new domain, in the editor too. I read both plists: only default values were stored (captions 0, camera motion 100, reduce flashing 0); no HDR or `FrontRooms.GlassRT` preference was lost | no effect |
| `defaultScreenWidth/Height` 1920×1080 → 1280×720 | `0d5bf54` | the WebGL build path writes the shared field; both Mac entry points set 1920×1080 again at build time | no effect via the scripts |
| `webGLInitialMemorySize` 128 → 256, WebGL2 API, IL2CPP Master | `0d5bf54` | WebGL only | WebGL track to confirm |
| `com.unity.modules.video` added | `04c3a6a` | needed by the CRT ads; ships to every target, WebGL included | COV-5 |
| `EditorBuildSettings`: ads demo scene added, disabled | `4448df9` | not in builds | fine |
| `FrontRoomsLevel0.asset` re-serialised (tiers, column fields) | `0d5bf54` | map signatures `d610d3a` = HEAD on 500 chunks (runtime) | no behaviour change |
| `activeInputHandler: 2` (Both) | unchanged | legacy input still works | fine |
| `FrontRooms3DBuild.BuildMac` nulls the URP slots | pre-existing (`c8349b3`); `c1f2d31` edited the next line | see COV-2 | finding |

### 4.8 Player packaging and player runs (nobody looked)
Red's Mac has two Mac player logs from 2026-10-04, both from Codex-era code, and one Mac app in `Builds/Mac`. See COV-1 and COV-3. Evidence: `coverage/mac_player_g14.txt`.

### 4.9 Draw load of Codex's map content (the 120 s run)
The runtime review's 120 s runs record G14's own timers only, and under heavy load. I used its load-independent signature tables instead (COV-6, `coverage/render_counts_precodex_vs_head.txt`).

### 4.10 Runtime object lifetime in Codex's code
- Window facade meshes are cached statically (`Window.cs:229`).
- Zone reflection resources are static and released.
- The CRT ad quad is not released (COV-4).

## 5. Findings

| ID | Severity | Finding | Owner → handoff |
|---|---|---|---|
| **COV-1** | major | G14 never loads in a macOS player: arm64-only dylib + importer pins CPU ARM64 + universal Mac builds; corrects `10_review_rt.md` F2/§5 | visual → glass-rt-track; Intel support is Red's call |
| **COV-2** | major (latent, not Codex) | "FrontRooms 3D/Build macOS" sets the URP pipeline to null in Red's editor and builds without URP | Red |
| **COV-3** | minor | The only Mac player on disk crashes at launch (`sharedassets0.assets is corrupted`); no runnable Codex-era Mac player exists | glass-rt-track (P0-A12) / Red |
| **COV-4** | minor | Each CRT ad leaks its quad `Mesh` when its Office chunk unloads (`04c3a6a`) | visual → this audit's Fix phase, with glass-look F6/F7 (same file) |
| **COV-5** | minor | Glass-look F6 also hits WebGL: no WebGL gate on the ads; video module ships to every target | visual (F6 fix covers it) → WebGL track decides on ads |
| **COV-6** | minor | +4.0 % renderers from Codex's map content (+2,819 / 500 chunks, max +45 per chunk); never measured as draw cost | visual → WebGL track; window-landing ("T" removes the trims) |
| **COV-7** | decision | Desktop stamina bar 24×6 → 24×5 px, ungated, from Codex's iPhone HUD commit `c1f2d31` | Red, then map contract (`FrontRooms3DGame.cs`) |

### COV-1 · major · G14 never runs in a macOS player
- **Evidence** (`coverage/mac_player_g14.txt`):
  - `Assets/Plugins/macOS/libFrontRoomsMetalGlassRT.dylib` is `Mach-O … arm64` only. The build script uses `-arch arm64` (`build_frontrooms_metal_glass_rt.sh:42, 50`).
  - Its `.meta` sets `OSXUniversal: CPU: ARM64`. `FrontRoomsGlassRTImporter.Configure` re-pins that on every import (`FrontRoomsGlassRTImporter.cs:34, 36`).
  - Red's Mac app `Builds/Mac/FrontRoomsss.app` (2026-10-04 17:25, a URP build) is a universal binary (`x86_64` + `arm64`). Its `Contents/PlugIns` holds only the FMOD and Burst bundles, and the bundle contains **0** files named `*GlassRT*` or `*MetalGlass*`.
  - `~/Library/Logs/Red Wang/FRONTROOMSSS/Player-prev.log` (a Mac player run on 2026-10-04 19:52, Apple GPU family 9) has at line 30: `[FrontRoomsGlassRT] off: plugin not loaded: FrontRoomsMetalGlassRT …`. That is `DllNotFoundException`, caught at `FrontRoomsGlassRTSystem.cs:111`.
- **What it means:**
  - In the editor, G14 runs at High.
  - In every Mac player built by the current scripts, it is off. The fail-safe works: one log line, no exception. But the desktop build Red would ship differs from the reference he sees in the editor.
  - `10_review_rt.md` says "every Play session and every Mac build now shows unreviewed RT output" and "the macOS player defaults to Ultra (quality level 5), which has never been measured". For players, both are wrong.
  - P0-A12 (player build) would fail as things stand.
- **Fix (Red picks one; glass-rt-track implements and verifies):**
  - (a) Apple-silicon-only Mac builds. Add `UnityEditor.OSXStandalone.UserBuildSettings.architecture = UnityEditor.Build.OSArchitecture.ARM64;` to both Mac entry points (`FrontRooms3DBuild.BuildMac`, `FrontRoomsCloudBuild.ApplyStandaloneSettings`). This drops Intel Macs.
  - (b) Keep universal builds. Build the dylib with `-arch arm64 -arch x86_64` and pin CPU `AnyCPU` **in `FrontRoomsGlassRTImporter.Configure`**. A meta-only edit would be reverted on the next import. Intel Macs then load the plugin, and the runtime capability check fails closed where Metal RT is missing.
  - **Verify:** build a Mac player in a clone outside iCloud. Check `Contents/PlugIns/libFrontRoomsMetalGlassRT.dylib` and `Player.log` showing `on: Apple9, Ultra`. Then run the 120 s autopilot in the player (P0-A12, P1-B8). Do this after rt F1 (fail-closed guard) has landed.
- **Not a Fix-phase item:** nothing breaks or throws today.

### COV-2 · major (latent) · "Build macOS" removes URP from the build and from Red's editor (not Codex)
- **Evidence:**
  - `Assets/Editor/FrontRooms3DBuild.cs:57-62`: `[MenuItem("FrontRooms 3D/Build macOS")] … GraphicsSettings.defaultRenderPipeline = null; QualitySettings.renderPipeline = null;`, then `BuildPipeline.BuildPlayer`. Nothing restores either value.
  - The cloud entry point says the opposite: "Preserve the authored URP pipeline. The older build entry point cleared it for a pre-URP artifact" (`FrontRoomsCloudBuild.cs:99`).
  - Introduced in `c8349b3` (2026-09-30, the pre-URP prototype). Codex's `c1f2d31` edited the line after it (product name) and left it.
- **What happens if Red uses that menu:**
  - The player has no URP, so every `FrontRooms/*` and URP material is broken.
  - The open editor switches to the built-in pipeline at once, and the change is saved with the project settings.
  - `GraphicsSettings.asset` is unchanged in git, so it has not happened since `7320ed1`.
- **Fix (Red; the build script is neither visual- nor map-owned):** delete lines 61–62, or route `BuildMac` through `FrontRoomsCloudBuild.ApplyStandaloneSettings`.

### COV-3 · minor · No runnable Mac player of Codex-era code
- **Evidence:**
  - `~/Library/Logs/Red Wang/FRONTROOMSSS/Player.log` (2026-10-04 20:47), launching `Builds/Mac/FrontRoomsss.app`: `The file '…/Data/sharedassets0.assets' is corrupted! … [Position out of bounds!]`, followed by a 21-frame native stack.
  - `sharedassets0.assets.resS` is exactly 524,288 B.
  - `Builds/` is git-ignored and sits in the iCloud-synced Desktop folder.
  - The only Codex-era player that started was the 19:52 `/private/tmp` build, which is now wiped. It stopped at the title (`READY`, line 329) and never entered the map.
- **Effect:** no review has seen Codex's work run in a desktop player. Glass-look F6 and COV-1 are inferred from build contents and the start-up log.
- **Fix:** with COV-1, rebuild the Mac player outside iCloud (for example under `W/`) using the URP-preserving entry point, then run it into the map.

### COV-4 · minor · CRT ad quad meshes leak on chunk unload
- **Evidence:**
  - `FrontRoomsScreenVideo.cs:172`: `filter.sharedMesh = MakeQuad(...)`. `MakeQuad` (`:184-186`) builds a new `Mesh` for every monitor.
  - `OnDestroy` (`:90-96`) destroys only `surfaceMaterial`.
  - `FrontRoomsMapWorld.Unregister` frees only `chunk.meshes` (`FreeMeshes(chunk)`, `:926`), and no runtime script calls `Resources.UnloadUnusedAssets`.
  - So every Office chunk teardown or revisit shift leaves one orphan `Mesh` per dressed monitor (88 % of stations) until the next scene load (R or a catch).
  - Added by Codex in `04c3a6a` (10-04). No review read it.
- **Size:** 4 vertices each plus Unity object overhead, so small per mesh. It grows for as long as one life lasts. WebGL has a fixed heap.
- **Fix (visual-owned, same file as glass-look F6/F7, so it can join that Fix-phase patch):** share one static quad per `screenSize`, or add `if (filter != null && filter.sharedMesh != null) Destroy(filter.sharedMesh);` to `OnDestroy`.
- **Verify:** a 120 s autopilot with `Resources.FindObjectsOfTypeAll<Mesh>()` counted for meshes named "FrontRooms CRT screen quad": flat after the fix, rising before.

### COV-5 · minor · F6 also applies to WebGL builds
- **Evidence:**
  - `FrontRoomsOfficeKit.cs:675` attaches the ad on every target.
  - `FrontRoomsScreenVideo.cs:296` gates only the clip choice (`#if !UNITY_WEBGL || UNITY_EDITOR`). On WebGL the player streams `StreamingAssets/FrontRooms_Ad_01.mp4` by URL.
  - `MakeMaterial` (`:210-211`) needs URP/Unlit, which no build contains (glass-look F6).
  - `Packages/manifest.json` gained `com.unity.modules.video` in `04c3a6a`.
  - No WebGL build exists after 10-01, so this is reasoned from code.
- **Fix:** glass-look's F6 material template under `Resources/Media/` fixes every platform. The WebGL track decides whether WebGL Office monitors play video at all (decode plus a 640 × 434 RT update; the WebGL tier is draw- and CPU-bound).

### COV-6 · minor · Codex's map content adds 4 % renderers; draw cost never measured
- **Evidence** (`coverage/render_counts_precodex_vs_head.txt`, from the runtime review's own signature tables; 20 seeds × 25 chunks, pre-Codex `7320ed1` vs `ee5c9bb`, which equals HEAD):
  - renderers 70,947 → 73,766 (**+2,819, +4.0 %**), mean +5.6 per chunk, max +45;
  - per 25-chunk build: mean 3,547 → 3,688, max +326;
  - lights and colliders unchanged (31,941 and 5,607).
- **Causes:** kit window frames (2 sides × 2 LODs), the `Glass_Window` slab, map trims still drawn under each frame (kits F2), and B0 re-cut boxes.
- **Effect:** desktop is the top-spec reference, so no action there. The WebGL tier is per-draw bound (about 3,200 draws). The window kit's `WebGLTier` forces LOD1 and turns off frame shadows, but it keeps frame, slab and trims.
- **Fix / handoff:** the WebGL track measures draws and frame time with and without kit windows. Window-landing's "T" contract removes the hidden trims, about 3 boxes per window.

### COV-7 · decision · Desktop stamina bar is 1 px shorter since Codex's iPhone HUD commit
- **Evidence:**
  - `FrontRooms3DGame.cs:1879` at HEAD: `new Vector2(24f, 5f)` for both HUDs.
  - `7320ed1` L1798 had `new Vector2(24f, 6f)`.
  - Codex `c1f2d31` changed it without the `mobileHud` gate it used for the position on the same line.
  - The visual review (V1, "Who") asked the owner whether this was meant; nobody answered, and `22bb75f` fixed V1 but not this.
- **Fix (if Red wants the old bar; map-owned file, so a contract):** `mobileHud ? new Vector2(24f, 5f) : new Vector2(24f, 6f)`.

## 6. Every earlier finding at HEAD `22bb75f`

| Finding | State at HEAD | Evidence |
|---|---|---|
| kits F1 (12 m cull) | **FIXED** `663e858` | VL114 |
| kits F2 / MAP-4 (trims under kit frames) | open | `MapWorld.cs:1342` still ignores `DressWindow`'s bool; window-landing "T" contract not applied |
| kits F3 (windows before doors) | open, decision | — |
| kits F4 (4 missing slots) | open | none of `Door_Enamel`, `Prop_KeyTagNo`, `Prop_SignEngraved`, `Run_ExitSign_Dead`, `Prop_Putty` `.mat` is in `Resources/Surfaces` |
| kits F5 (faulty `Kit_KeyCabinet`) | open | sha1 `f7e570c7` unchanged |
| kits F6 (`Kit_KeyHookBoard` name) | open, decision | — |
| kits F7 / docs D1, D2, D4 | open | `VISUAL_CHAT_TASKS.md` at HEAD: L44 still says "fresh Unity meta files" and "no other missing production paths"; L160 (G14) still says "The map opts the post-stack camera" and "the local Metal toolchain was unavailable" |
| docs C3 ("G14 targets the visible slab" false) | **now TRUE** since `9eddc35` | rt VL116 |
| docs D3 / MAP-6 (N1 gate) | superseded | the N1 row (L124) records Red's 22:1x decision to keep V5 ON; the code comment "provisional N1 pick" is now roughly right |
| docs D5, D6 | open | — |
| docs D7 / runtime R3 (10 / 29 counts) | open | L28 still says "10 … 29 … (cause under audit)" |
| docs H1 (`.pyc` tracked) | **half fixed** `44fffd1` | ignore rule added; 156 `.pyc` still tracked, so Blender runs still dirty git |
| docs H2 (broad ignore rules) | open | — |
| docs H3 (iCloud copies) | **worse** | 114 tracked "` N.`" copies, including 15 FMOD banks (4.4 MB) in `StreamingAssets` that ship in every build (`16f520e`); the iCloud chat owns them |
| MAP-1 (V5 on) | closed by Red | `RED_DECISIONS.md` |
| MAP-2, MAP-3 | open | the patches still apply and compile (§4.6) |
| MAP-5 | open | — |
| glass-look F2 (stale cubes), F3 (look sign-off) | open | manifest still dated 11:57 on 10-03 |
| glass-look F5b / rt F2 (RT live, unaccepted) | open **in the editor only**; off in players (COV-1) | — |
| glass-look F6, F7 (CRT ads) | open | `ScreenVideo.cs` has no `CRT_AdSurface` and no `SetLODs` |
| rt F1 / runtime R1 (no fail-closed guard) | open | no `FailClosed` in `FrontRoomsMetalGlassRTRendererFeature.cs` |
| rt F3, F4, F6 | open | — |
| rt F5 (edit-mode opt-in) | open | `OptIn` has no `isPlaying` gate |
| rt F7 | open | = docs D1 |
| runtime R2 (magenta key after R) | open, map contract | `Own(new Material(` absent at HEAD |
| runtime R4 (print K03) | open | — |
| visual V1 (key panel) | **FIXED** `22bb75f` | anchor gated by `mobileHud`; autopilot "HUD all panels on screen" in 4 lives (runtime) |
| visual V2 / rt F8 (RT paints print red) | open | no `_FR_PRINT` in `FrontRoomsGlassRTSystem.cs` |
| visual V3 (LEVITZ ad) | open | `OfficeKit.cs:675` still attaches (HEAD and working tree) |

## 7. Verification images

None. This stage ran no renders or captures; its evidence is text. Nothing goes to the Figma section "FRONTROOMS · VISUAL VERIFICATION LOG", and no VL row was claimed. Captures still owed by owners:
- COV-1: a Mac player frame at a window, RT on vs off (glass-rt-track);
- MAP-2: the two-visit chunk-line junction (map chat / level-transitions);
- kits F2: the trim step in motion at 0.3 m (window-landing).

## 8. Files

- **This report.**
- **`coverage/player_compile_6targets_22bb75f.txt`**, `coverage/player_compile.py.txt`: §4.1.
- **`coverage/guid_check_22bb75f.txt`**: §4.2.
- **`coverage/map_contract_patch_check.txt`**: §4.6.
- **`coverage/mac_player_g14.txt`**: COV-1, COV-2 and COV-3 evidence (plugin, importer, bundle listing, player logs, build script lines).
- **`coverage/render_counts_precodex_vs_head.txt`**: COV-6.
- **Work dir `W/cov/`:**
  - `head_src/` and `head_yaml/`: HEAD exports;
  - `player_head/`: per-target rsp and logs;
  - `rc_map23/`: MAP-2/3 compile;
  - `patchcheck/`: diffs and patched copies.
