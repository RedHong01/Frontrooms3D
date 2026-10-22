# Codex audit · 20 · Findings (final report)

| | |
|---|---|
| Date | 2026-10-14, 17:0x–18:xx PDT (report stage of the codex-audit run; planned for 2026-10-07, delayed by the 10-08 usage limit and the 10-12 macOS 27 upgrade) |
| What was audited | Codex's commits in Red's project on 2026-10-03: `8ef5b64` "1", `daef6c2`, `9e754a2`, `75cfdff` (base `7320ed1`), plus Codex's later visual-area commits (`9eddc35`, `bf2e883`, `70644f0`, `04c3a6a`, `b5f381f`, `c1f2d31`, `2ce2087`) and every later commit that fixed or changed a Codex error |
| Main checked | HEAD `0a9ee7f` (2026-10-10) plus Red's working tree on 2026-10-14 (it holds the interactables-kit finals, COV-4 and the GD3 generator, all merged 10-14 and not yet committed) |
| Read-only | Unity was never opened on `Frontrooms3D`. Nothing was committed. Nothing in Red's project was deleted. The only writes are under `Documentation/research/codex_audit/`, one bullet in `Documentation/VISUAL_CHAT_TASKS.md` §000 and rows in `Documentation/VERIFICATION_LOG.md` |
| Inputs | `00_main_state.md` (provenance), the seven `10_review_*.md`, the five `30_fix_*.md`, `40_applied.md`, the verifiers' notes (`W/recovery/codex-audit-findings.json` and the run journal), and the checks this stage ran (§7) |

`W` = `/Users/redwang/FrontRoomsVisualWork`. Paths without a root are under `Documentation/research/codex_audit/`.

---

## 0. Short answer

- **51 findings were raised; two verifiers checked each one. 29 are confirmed, 22 were dropped** (§4 gives each skeptic's reason). One new FLAG came from the combine stage (§3).
- **Status of the 29 confirmed findings on today's main:**

| Status | Count | Findings |
|---|---:|---|
| FIXED BY LATER COMMIT / MERGE | 3 | K-F1 (`663e858`), V1 (`22bb75f`), K-F5 (interactables-kit merge, 10-14) |
| FIXED, APPLIED | 7 | COV-4 (10-14); DOC-D1, DOC-D2, DOC-D4, DOC-D5, DOC-D7, RT-F7 (task file, 10-08) |
| FIXED, READY TO APPLY | 4 | GL-F6, GL-F7, V2 (stopgap), COV-5 (items 1–4): one command, §0.1 |
| DEFERRED to a workflow | 6 | RT-F1, RT-F2, RT-F3, RT-F4, RT-F5 → glass-rt-track; GL-F2 → glass owner (visual) |
| CONTRACT to the map chat | 3 | R2, MAP-2, MAP-3 (`contracts/` C1–C3, verified in a clone today) |
| RED DECISION | 6 | V3, COV-1, COV-2, COV-7 (then contract C4), HYG-H1, HYG-H3 |

- **What Codex got right:** the glass track (47 files, 24 of 24 md5s equal the verified final), the window kit models, the `DropChunk` fix (`9e754a2`), the `Ensure()` removal (`75cfdff`, G14 contract C1 word for word), the plugin `.meta` platforms, the B0 corner fix Red asked for at 13:09 in a consistent build, and the 10-04 G14 prepass merge into `FrontRooms/Glass`. Main compiles with 0 errors, every project suite passes, and the map is deterministic.
- **What was wrong:** unfinished clone work promoted as final (G14, window facade r3, the interactables pre-renders, the V5 pre-render variant), a kit importer that would cull door and window frames at 12 m, a CRT ad with a real brand and a player-build material bug, a desktop HUD key panel pushed off screen, and task-file rows that misstated all of this.

### 0.1 The one command waiting to be applied

```
bash /Users/redwang/FrontRoomsVisualWork/apply/codex_audit_all.sh --apply
```

It writes 5 paths (GL-F6, GL-F7, V2 stopgap; COV-5 rides on GL-F6). Last dry run on main `0a9ee7f` + tree: `DRY RUN OK: every base matches`. Details and its six refusal tests: `40_applied.md`. After it: Red's editor imports one material and recompiles once; F6 reaches players with the next Mac and iOS builds; commit the untracked `Assets/Resources/Rendering/FrontRoomsScreenVideo.shader` + `.meta` (Codex, 10-08) with the next "1".

---

## 1. How to read this file

- **IDs.** Three reviews used the same numbers, so this file prefixes them:
  - `K-` = kits + window review (`10_review_kits.md`): K-F1, K-F5;
  - `RT-` = G14 review (`10_review_rt.md`): RT-F1 … RT-F7;
  - `GL-` = glass-look review (`10_review_glass-look.md`): GL-F2, GL-F6, GL-F7;
  - DOC/HYG (`10_review_docs.md`), MAP (`10_review_map-edits.md`), R (`10_review_runtime.md`), V (`10_review_visual.md`), COV (`10_review_coverage.md`) keep their own IDs.
  - V2 and RT-F8 are one defect, counted once (V2).
- **Status words:** FIXED (+ applied / ready), FIXED BY LATER COMMIT `<sha>`, DEFERRED to `<workflow>`, CONTRACT to map chat, RED DECISION.
- **File:line** is today's file unless a sha is given.

---

## 2. Confirmed findings (29)

### 2.0 Table

| ID | Track | Sev | File:line (today) | Claim (short) | Owner | Status |
|---|---|---|---|---|---|---|
| K-F1 | kits + window | major | `Editor/Rendering/FrontRoomsKitImporter.cs:80–92` | Importer culled door/window frames at 12 m, blinds at 10 m (spec: never / 30 m) | visual | FIXED BY LATER COMMIT `663e858` |
| K-F5 | kits + window | minor | `Resources/Props/Models/Kit_KeyCabinet.fbx` | Inside-out corner faces (black notches) | visual | FIXED BY interactables-kit merge 10-14 |
| DOC-D1 | docs | major | `VISUAL_CHAT_TASKS.md` G14, W1.5 rows | False claims: "Metal toolchain unavailable", "the map opts the camera", "RT targets the slab" | docs | FIXED, applied 10-08 |
| DOC-D2 | docs | major | `VISUAL_CHAT_TASKS.md` "Clone merge audit" | "Fresh Unity metas" false; "no other missing paths" hid two owed pieces | docs | FIXED, applied 10-08 |
| DOC-D4 | docs | major | `VISUAL_CHAT_TASKS.md` W1.4, W1.7 | Described r3 behaviour main did not have; W1.7 still gated on DW while kit windows were live | docs | FIXED, applied 10-08 |
| DOC-D5 | docs | minor | `VISUAL_CHAT_TASKS.md` G1–G10, GD3, D1 | 7 contradicting rows; Red's 13:3x decision, G1 caveat and reflectance numbers deleted; GD3 status changed | docs | FIXED, applied 10-08 |
| DOC-D7 | docs | minor | `VISUAL_CHAT_TASKS.md` §000 | "10 / 29 exceptions": Red's log had 85 / 253 | docs | FIXED, applied 10-08 |
| HYG-H1 | docs | minor | `.gitignore` | Python bytecode tracked (153 then, **156** today); every Blender run dirties git | Red | RED DECISION |
| HYG-H3 | docs | minor | `Documentation/ICLOUD_NOSYNC_TASK.md` | iCloud conflict copies still there (736 outside `Library/`, 114 tracked); task not carried out | Red / iCloud chat | RED DECISION |
| MAP-2 | map edits | major | `Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs:1309–1338` | B0.1 posts on chunk lines read the neighbour's interior: notches/z-fights after a revisit shift | map | CONTRACT C3 |
| MAP-3 | map edits | minor | `FrontRoomsMapWorld.cs:1782–1792, 3355` | With `-lightleadBorders`/`-lightleadSoft`, the "pure" `LampModeOf` generates chunks; a map test fails | map | CONTRACT C2 |
| R2 | runtime | major | `FrontRoomsMapWorld.cs:3757` | Zone keys draw magenta after R / a caught restart (pre-existing, `f5e99e10`) | map | CONTRACT C1 |
| V1 | visual | major | `Scripts/FrontRooms3DGame.cs:2011` | Desktop held-key panel 118 px above the screen (Codex `c1f2d31`) | map | FIXED BY LATER COMMIT `22bb75f` |
| V2 | visual (= RT-F8) | major | `Scripts/Rendering/GlassRT/FrontRoomsGlassRTSystem.cs:472–511` | G14 traces print wallpapers' paper code as albedo: a dark-red slab in window reflections | visual | FIXED (stopgap), READY; real fix DEFERRED to glass-rt-track |
| V3 | visual | major | `Assets/StreamingAssets/FrontRooms_Ad_01.mp4` via `Scripts/Office/FrontRoomsOfficeKit.cs:851` | Every Office CRT shows the real brand "LEVITZ"; title cut off; type never went through 平面视觉 | Red → 平面视觉 | RED DECISION (a/b/c) |
| RT-F1 | G14 | major | `Scripts/Rendering/FrontRoomsMetalGlassRTRendererFeature.cs:138–143` | No fail-closed guard: any G14 exception freezes/blackens frames | visual | DEFERRED to glass-rt-track |
| RT-F2 | G14 | major | `FrontRoomsGlassRTSystem.cs:71–83` | G14 changes every map window in Editor Play by default; acceptance never ran on the promoted code | visual | DEFERRED to glass-rt-track (verify ran 10-14) |
| RT-F3 | G14 | major | `FrontRoomsGlassRTSystem.cs` (per-frame scan, uploads) | Main-thread cost 5–10× over budget | visual | DEFERRED to glass-rt-track |
| RT-F4 | G14 | minor | `NativePlugin/FrontRoomsMetalGlassRT.mm` | Native scene kept after Play exit | visual | DEFERRED to glass-rt-track |
| RT-F5 | G14 | minor | `FrontRoomsGlassRTSystem.cs:193` (`OptIn`) | Edit-mode cameras get a component that is a missing script off macOS | visual | DEFERRED to glass-rt-track (+ ads-demo scene re-save) |
| RT-F7 | G14 / docs | minor | `VISUAL_CHAT_TASKS.md` G14 row | Codex's G14 row wrong and out of date | docs | FIXED, applied 10-08 |
| GL-F6 | glass-look (CRT) | major | `Scripts/Media/FrontRoomsScreenVideo.cs:217–225` | CRT ad material from shaders missing in player builds: exception per monitor, no ad, no collider | visual | FIXED, READY (partly fixed by `2ce2087`) |
| GL-F7 | glass-look (CRT) | major | `FrontRoomsScreenVideo.cs:163` (`CreateSurface`) | Ad quad not in the monitor's LODGroup: floats where a culled monitor stood | visual | FIXED, READY |
| GL-F2 | glass-look | minor | `Resources/Rendering/Reflections/capture_manifest.txt` | Zone reflection cubes stale (before `_FR_PRINT`, lens ×1.5, V5 lamps); staleness check blind to those | visual (glass owner) | DEFERRED to glass owner |
| COV-1 | G14 / players | major | `Editor/RT/FrontRoomsGlassRTImporter.cs:34–36` | G14 never loads in a macOS player: arm64-only dylib in universal apps | visual; Intel support is Red's call | RED DECISION (a/b), then glass-rt-track |
| COV-2 | player builds | major (latent) | `Editor/FrontRooms3DBuild.cs:57–62` | Menu "Build macOS" nulls URP in the build and in Red's editor (not Codex) | Red | RED DECISION |
| COV-4 | CRT ads | minor | `FrontRoomsScreenVideo.cs:190` (`MakeQuad`) | One leaked quad mesh per dressed monitor per teardown | visual | FIXED, APPLIED 10-14 |
| COV-5 | CRT ads / WebGL | minor | `FrontRoomsScreenVideo.cs`; `Packages/manifest.json:6` | GL-F6 also hits WebGL; no WebGL gate; video module for every target | visual | FIXED (items 1–4), READY; item 6 DEFERRED to WebGL track |
| COV-7 | HUD | decision | `FrontRooms3DGame.cs:2006` | Desktop stamina segments 24 × 6 → 24 × 5 px, ungated (Codex `c1f2d31`) | Red, then map | RED DECISION, then CONTRACT C4 |

### 2.1 Kits and window kit

**K-F1 · major · importer cull.** FIXED BY LATER COMMIT `663e858` (Red, 10-04 09:40; `1114d6b` records it).
- Claim: Codex mapped `lodDistances[i]` to LOD `i`. Sidecars hold `[d01, d12, dcull]` and the FBX files have 2 LODs, so the cull took d12: door frames, leaves and window frames culled at 12 m, blinds at 10 m. `GetVersion()` was not bumped.
- Today: `FrontRoomsKitImporter.cs:80–92` maps the last LOD to the last authored distance; `GetVersion() => 2` (line 16).
- Evidence: VL081 (FAIL, 10-04) → VL114 (PASS: frames drawn to 25.5 m). The kits+window fix stage read 23 of 23 sidecar-driven LODGroups at spec on main + tree (doors and window frames 4 m, never culled; blinds 3 m / 30 m; exit signs 5 m, never). The interactables-kit merge re-checked all 57 kits (`research/interactables/40_merged.md` §4).
- Nothing left for window-landing or interactables-kit (P-1 is implemented).

**K-F5 · minor · `Kit_KeyCabinet` inside-out corners.** FIXED by the interactables-kit merge (10-14 16:2x; working tree, not yet committed).
- Main now holds FBX sha1 `29d8332f…`, JSON `028d8712…`; GUID `13c85fe1…` kept (`research/interactables/40_merged.md` row 67). VL219 PASS covers the merge.
- Its new JSON adds a 4th slot `Prop_KeyTagNo`, which has no material (dropped K-F4, §4).

### 2.2 Docs and hygiene

All five DOC findings and RT-F7 were corrected in `VISUAL_CHAT_TASKS.md` on 2026-10-08 by the docs fix stage (`30_fix_docs.md`: 16 exact-match edits, sha1-checked; Red's 13:3x decision, the G1 caveat and the reflectance numbers restored word for word). Status FIXED, applied.

| ID | What was corrected |
|---|---|
| DOC-D1 | G14 row replaced (Red's 09:5x origin and Owner restored; promoted while WIP; live since `9eddc35`; open items listed). W1.5 kept and annotated |
| DOC-D2 | Clone merge audit bullet: metas are Codex's uuid4 (`75e18a03`, `af825826`); the two owed pieces named (G14 shader hunks, landed 10-04; window map contract R1/R2 (+T), still owed) |
| DOC-D4 | W1.4 and W1.7 corrected (r3 compiled, never run; trims still drawn under every kit frame; kit windows live since 10-03 19:10) |
| DOC-D5 | G1 restored; G2/G3/G4/G6/G10 statuses; GD3 sentence and Owner restored; D1.4 note on the 53 inert pre-renders |
| DOC-D7 | 85 / 253 with the cause |
| RT-F7 | G14 row (same edit as D1) |

**HYG-H1 · minor · tracked bytecode.** RED DECISION.
- `44fffd1` added `__pycache__/` to `.gitignore` (line 96), but the files already tracked stay tracked: **156** `.pyc` today (153 on 10-04). Each Blender run by a workflow rewrites some of them.
- Recommended default: one commit with `git rm -r --cached` on the tracked `__pycache__` folders (Red runs it; this audit never touches git). Until then, Blender runs set `PYTHONDONTWRITEBYTECODE=1`.

**HYG-H3 · minor · iCloud leftovers.** RED DECISION (owner: the iCloud chat).
- 736 `* N.*` conflict copies outside `Library/`/`Temp/` today (534 under `Documentation`, 141 `Builds`, 35 `Assets`, 16 `UserSettings`, 4 `ProjectSettings`); 114 of them are tracked by git (some in `StreamingAssets/FMOD`, which ships in every build).
- `ICLOUD_NOSYNC_TASK.md` (10-03 12:26) was never carried out. Its dylib install-name item is stale: the G14 dylib already uses `@rpath` (`10_review_rt.md` §2).
- Recommended default: move the project out of iCloud (or `.nosync`) as that task says, drop the stale item, then remove the copies after a diff check. Nothing was deleted here.

### 2.3 Runtime

**R2 · major · zone keys magenta after a restart.** CONTRACT C1 (`contracts/C1_R2_key_material.md`).
- Not Codex: `f5e99e10` (2026-10-02); the pre-Codex base shows it too.
- Cause: `FrontRoomsMapWorld.cs:3757` owns a material that `FrontRoomsSurfaces.Lit` keeps in a static cache; the old map's deferred `Destroy` kills it after the new map has taken it. Alternates: life 2 pink, life 3 fine, life 4 pink.
- Still live on 2026-10-14: the apply stage's 120 s run on main + payload logged the null-material key (`40_applied.md` §3). One-line contract, verified in a clone (§7.3).

### 2.4 What Red sees (visual)

**V1 · major · desktop key panel off screen.** FIXED BY LATER COMMIT `22bb75f` (10-07 23:46).
- Codex's iPhone HUD commit `c1f2d31` anchored `HUD / Key` top-left for every target but kept the desktop offset (72, 118).
- Today `FrontRooms3DGame.cs:2011` gates the anchor on `mobileHud` (exactly `vis_F1_key_panel_anchor.contract.diff`). `22bb75f` also added "AUTOPILOT HUD all panels on screen", which printed in all 4 lives of the runtime review's run.
- The same commit's ungated stamina height is COV-7.

**V2 · major · G14 paints Level 0 wallpaper red in window reflections.** FIXED (stopgap), READY TO APPLY; real fix DEFERRED to glass-rt-track.
- `Describe()` (`FrontRoomsGlassRTSystem.cs:472`, Surface branch) hands `_BaseMap` to the hit shader as albedo. With `_FR_PRINT` that texture is the paper modulation code (mean sRGB 195/15/51). `L0_Wallpaper`, `L0_Wallpaper_Shift`, `Exit_Wallpaper`.
- Visible since `9eddc35` (10-04) made G14 trace window glass. Red's editor runs RT High.
- Stopgap (`V2-STOPGAP` block, 10 lines): trace a flat ink tone. Red pixels in the four hall-side window shots 7.67 / 3.56 / 5.08 / 9.99 % → 0.00 / 0.02 / 0.00 / 0.01 %; ≤ 0.04 % on all 84 frames; about 2 % darker than the raster wall. VL153 (FAIL) → VL223 (PASS), VL244.
- In `codex_audit_all.sh` (§0.1), or alone `codex_audit_other_visual.sh --apply`.

**V3 · major · LEVITZ ad on every Office CRT.** RED DECISION.
- `FrontRooms_Ad_01.mp4` (md5 `f9ec1412…`, unchanged since `04c3a6a`) plays on every map Office monitor (`FrontRoomsOfficeKit.cs:851`, used at `:875`) and in the title corridor's Office rooms (`FrontRoomsRoomStream.cs:1440`). 0–3 s "LEVITZ", a real US furniture chain; 3–6 s "FRONTROOMS FURNITURE" cut at both edges; Codex's ffmpeg type.
- Rules it breaks: no trademarks (`room_visuals/01_inventory.md:147`, `interactables/02_period_hardware.md:347`); period ads are research only (`Research/week02/furniture-ads/SOURCES.md:5`, quoted by `SCREENS_VISUAL_SYSTEM.md` P1); type goes through 平面视觉.
- Options: (a) kill switch: comment out the `Attach` call at `:851`; (b) **recommended**: 平面视觉's Ad_02 (fictional "Front Rooms Furniture", 640 × 480, 16 s) under the same file name, no code change: `bash /Users/redwang/FrontRoomsVisualWork/apply/codex_audit_v3_ad02.sh --apply --red-approved` (dry run OK 10-14); (c) trim the first 3 s of Ad_01. VL154 (FAIL), VL224 (WAIT-RED).

### 2.5 Map edits (map-owned file; contracts only)

**MAP-2 · major · chunk-line posts depend on the neighbour's interior.** CONTRACT C3 (`contracts/C3_MAP2_chunk_line_posts.md`).
- Codex's B0.1 (`CornerReach` → `PostPieceAt` → `Cache.Edge`, `FrontRoomsMapWorld.cs:1309–1338`) reads all four pieces round a post. On a chunk line one of them is the other chunk's interior wall, which a revisit shift reshuffles (`shiftAfterSeconds` 30). This breaks the generator's rule "neighbours agree no matter which is built first" (`FrontRoomsMap.cs:246–251`).
- Measured today on main (20 seeds; every other chunk shifted and rebuilt): **115 of 240** still-built chunks no longer match their own rebuild (0 of 240 with B0 off). In the post squares: **90 notches and 9 z-fights** on 5,600 chunk-line posts (0 / 0 in a fresh build). Each build also generated 429 chunks of neighbour data beyond the 500 built (30 with B0 off).
- Corrected evidence (verifier): the model's 19.7 / 13.1 % notch rates and the 26–28 % overlap rates were per-configuration and overweighted mixed posts; real rates are lower. The early-tier point is not a defect (dropped).
- **Do not send** the "+1 reach on every chunk-line post" patch from `10_review_map-edits.md` §4: it brings back the 13:09 seam on 1,315 of 5,600 chunk-line posts (measured).
- C3 (verified): 0 notches, 0 of 240 chunks inconsistent, 24 residual z-fight posts (0.4 %), +154 static paper strips; full fix = the verifiers' post-seam table (map chat's call).

**MAP-3 · minor · `LampModeOf` generates chunks under the border flags.** CONTRACT C2 (`contracts/C2_MAP3_lamp_border_pure.md`).
- The V5 mirror (`:3355`) calls `BorderThemes` → `Cache.Edge` → `Cache.Get`. Off by default.
- Measured today with the border rule forced on: 800 chunks generated by 640 far-cell predictions; `FrontRoomsMapInteractionTests` FAIL 151 / 1 ("LampModeOf a far cell … generates no chunk"). With C2: 0 generated, PASS 152 / 0, every one of 15,970 built lamp modes identical to main.
- C2 **replaces** `research/level_transitions/contracts/lightlead_lampmodeof_pure.diff` (same idea, without the border refinement); send only C2.

### 2.6 G14 ray-traced glass (all DEFERRED to glass-rt-track)

The runtime+rt fix group changed no game file by design (`30_fix_runtime_rt.md`): the glass-rt-track holds fixes for most of these in `W/proj_rt`, and a second copy would make its 3-way rebase conflict. Its engineering verify ran on 2026-10-14 (`research/glass/rt/25_verify_engineering.md`, VL239–243).

| ID | Claim and evidence | What glass-rt-track's merge must bring |
|---|---|---|
| RT-F1 major | `TracePass.RecordRenderGraph` calls `EnsureTargets`/`SendTargets`/`BuildFrame` (`FrontRoomsMetalGlassRTRendererFeature.cs:138–143`) with no try/catch. Fault injection on main: 12 of 12 frames frozen, 9 ZBinningJob errors (VL118). Root cause of Red's 85 + 253 errors on 10-03 (dropped R1, §4). No trigger is known on today's main | `FailClosed` + the E4 follow-ups: `Warmup()` inside the try; a `failedClosed` gate on Register / PrepareMeshes / IsPrepared / MeshChanged / RefitMesh; reset `qualityOverride` in `ResetStatics`; one log line. **Land first.** |
| RT-F2 major | RT High in Red's editor (quality level 3), unaccepted since `9eddc35` | Its 10-14 verify: correctness PASS (IoU 0.996–0.999), RT Off pixel-identical (VL240), player run arm64 OK (VL242); cost FAIL. Red's highest-spec rule keeps it ON (dropped D1) |
| RT-F3 major | Main thread 5–10× over budget (p50 2.3 ms, p99 up to 6 ms, streaming spikes to 20–86 ms) | E1: each mesh upload copies the whole 64k scratch array (1.5–3.5 ms per call). Walk p99 8.95 ms vs 1.5 (VL241) |
| RT-F4 minor | Native scene kept after Play exit (≈ 5 MB BLAS + 8 MB geometry) | `proj_rt`'s ExitingPlayMode release pump; its verify shows 0 meshes / 0 BLAS / 0 retired after exit (PASS) |
| RT-F5 minor | `OptIn` adds `FrontRoomsGlassRTCamera` to edit-mode cameras; saved in `FrontRoomsScreenAdsDemo.unity:1997`; a missing script off macOS | `proj_rt`'s isPlaying guard; then remove the component in a Mac editor and re-save the demo scene (do not re-run its builder) |

### 2.7 Glass look and CRT ads

**GL-F6 · major · CRT ad material in players.** FIXED, READY (in §0.1).
- Codex's `MakeMaterial` used `Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture")`; neither was in player builds. The 10-08 Mac player logged 46 `ArgumentNullException` lines.
- Partly fixed by Codex's own later change `2ce2087` (10-08 16:41, committed 10-10): `FrontRooms/ScreenVideo` from Resources; the exception is gone. Left over: no fog, no depth/normals passes, a half-built quad on failure, `??` on `UnityEngine.Object`.
- Package: `Resources/Media/CRT_AdSurface.mat` (URP Unlit template) first, Codex's shader as fallback 2, explicit null checks, no quad without a material. Mac dev player: 0 "Parameter name: shader"; 48 / 48 variants compile (GLES3/WebGL, Metal macOS + iOS, WebGPU, D3D, Vulkan). VL227.

**GL-F7 · major · ad quad outlives its culled monitor.** FIXED, READY (in §0.1).
- The quad now joins every LOD of the monitor's LODGroup without changing the group's size. Culled shots at 14.3–23.3 m: 140 / 96 / 80 / 60 px → 0. VL115 (FAIL) → VL226 (PASS).
- Side effect: NEW-1 (§3).

**GL-F2 · minor · stale zone reflection cubes.** DEFERRED to the glass owner (visual).
- `capture_manifest.txt` is from 10-03 11:57: shader md5 `079f93ca` (today `d7e9168e`), lenses `Troffer_Lens` (game uses `Troffer_Lens_Cool`), "static paper (no `_FR_PRINT`)"; `Level0LensEmissionScale = 1.5` is not recorded, so `WarnIfStale` cannot see it.
- Steps (all visual-owned): `30_fix_glass_look.md` §3 (extend `CurrentInputs()`, recapture with V5 ON in a clone, G10 subset check, promote only the 4 `.exr` + manifest, recapture again after the live print lands).

**COV-4 · minor · CRT quad mesh leak.** FIXED, APPLIED 10-14 16:2x (`W/apply/MERGED.md`). One shared quad per screen size; 20 → 40 → 60 leaked meshes over 3 cycles became 1.

**COV-5 · minor · WebGL.** FIXED (items 1–4), READY: GL-F6's material path; the WebGL player probe found URP Unlit (4, 694) and `FrontRooms/ScreenVideo` (4, 693) in the shader table, 0 exceptions in the browser (`40_applied.md` §5; one monitor in a probe scene, not a full Office room). Item 5: keep the video module (no change). Item 6 (live video or a poster on WebGL; a gate would go in `FrontRoomsOfficeKit.AttachScreen`, `:851`, `#if !UNITY_WEBGL || UNITY_EDITOR`): DEFERRED to the WebGL track.

### 2.8 Coverage findings

**COV-1 · major · no G14 in macOS players.** RED DECISION, then glass-rt-track.
- The dylib is arm64-only; `FrontRoomsGlassRTImporter.cs:34–36` pins CPU ARM64; the Mac apps checked so far are universal (x86_64 + arm64), so Unity leaves the plugin out. Player logs: `off: plugin not loaded`. Fails safe.
- No build entry point under `Assets/Editor` sets a Mac architecture (grep, 10-14), so the new TestFlight path (`Tools/build_mac_testflight.sh` → `FrontRoomsCloudBuild.BuildMacTestFlight`, 10-10) most likely makes a universal app too. Not checked: its output went to `/tmp`. Check the next one with `lipo -archs` on the app's executable and look for `libFrontRoomsMetalGlassRT` in `Contents/PlugIns`.
- glass-rt-track proved an arm64-only app runs RT (`on: Apple9, Ultra`, 0 errors; VL242).
- (a) arm64-only Mac builds (drops Intel Macs); (b) **recommended**: a universal dylib (`-arch arm64 -arch x86_64`) and CPU `AnyCPU` set **in `FrontRoomsGlassRTImporter.Configure`** (a meta-only edit is reverted on import); Intel Macs then fail closed at the capability check.

**COV-2 · major, latent · "Build macOS" nulls URP.** RED DECISION (build script, neither visual- nor map-owned).
- `FrontRooms3DBuild.cs:57–62` sets `GraphicsSettings.defaultRenderPipeline` and `QualitySettings.renderPipeline` to null, then builds; nothing restores them. The cloud entry point says the opposite (`FrontRoomsCloudBuild.cs:99`). From `c8349b3` (09-30), not Codex. Not used since `7320ed1` (GraphicsSettings unchanged in git).
- Recommended default: delete lines 61–62 (or route `BuildMac` through `FrontRoomsCloudBuild.ApplyStandaloneSettings`). Until then never use that menu; snapshot builds go through `W/tools/build/snapshot_build.sh`.

**COV-7 · decision · desktop stamina height.** RED DECISION, then CONTRACT C4.
- Codex's `c1f2d31` changed 24 × 6 → 24 × 5 px on every target (`FrontRooms3DGame.cs:2006`) while gating the position on the same line.
- Recommended default: (a) 6 px on desktop, 5 px on phones (`contracts/C4_COV7_stamina_height.diff`).

---

## 3. New finding from the combine stage (not counted in the 29)

**NEW-1 · FLAG · a CRT is black for a moment at first sight (after GL-F7).** Owner: 游戏视觉 (screens rework, `SCREENS_VISUAL_SYSTEM.md`).
- GL-F7 now culls the ad quad with its monitor (11.2 m at High), so far monitors no longer keep the shared ad player warm. The first frame after a monitor comes into range shows a black RenderTexture until the video decodes (≈ 9 frames, 0.15 s). Screen luma 40.3 (main) / 16.8 (combined) / 38.1 (combined, F7 off). At Ultra nothing changes.
- Not blocking: F7 removes a visible bug. Suggested fix: prepare the bank on `Register`, and show a dark-tube frame until the first video frame. VL245 (`3128:7056`).

---

## 4. Dropped findings (22) and the skeptics' reasons

Each was checked by two verifiers; one confirmed the facts, the other refuted it as an error to fix in Red's game. The facts often hold; the reasons say why the item was not kept as a finding. Where a dropped item still matters, its owner is named.

| ID | Claim (short) | Why dropped | Still matters for |
|---|---|---|---|
| K-F2 | `DressWindow`'s bool is ignored; the map's 3 trim boxes stay under every kit frame (0.5 mm apart) | Facts true, nothing Red can see. The fix as written would cause a regression (bare wall cuts beyond the cull, before `663e858`) and does not apply to main alone. It is window-landing's contract T, which Red decides | window-landing (T), Red |
| K-F3 | Windows went live before the doors and the critics' fixes ("one grammar" break) | A look and sequence decision already routed to Red (D1.3/D1.5, window-landing `05_for_red.md`); option (b) named a test switch that main lacks | Red, window-landing |
| K-F4 | 19 kits use 4 slots with no material (`Door_Enamel`, `Prop_KeyTagNo`, `Prop_SignEngraved`, `Run_ExitSign_Dead`) | Inert kits; P-4 needs Red's approval; no placeholders in main by rule. Today 3 slots remain (18 kits): exit-sign merged `Run_ExitSign_Dead` | interactables-kit (P-4), Red |
| K-F6 | Key board shipped as `Kit_KeyHookBoard`; the proposal calls it `Kit_KeyRack` | A naming decision for Red, not a defect | Red, interactables-kit |
| K-F7 | Task-file rows misstate the kit/window state | Duplicate of DOC-D2/D4; one part wrong (r3 was compiled in a clone) | — (fixed with the DOC items) |
| DOC-D3 | N1 row drops "Red confirms first"; a comment calls V5 "the provisional pick" | Red decided at 22:1x to keep V5 ON (`RED_DECISIONS.md`), so the proposed fix was out of date | level-transitions (comments, `lightlead_comments_visual.diff`) |
| DOC-D6 | G1 promoted without its three "after promoting" steps | True, but the owed steps are tracked as GL-F2 and in the G1 row restored by DOC-D5 | glass owner |
| HYG-H2 | Unanchored `[Bb]uild/`, `[Ww]ebGL/` rules ignore research folders | True, but a hygiene choice with no effect on the game; Red's `.gitignore` | Red (optional) |
| HYG-H4 | Codex stripped Unity's trailing spaces from 3 metas | Part true, prediction wrong: Unity rewrote them and Red committed it (`03c43ed`) | — |
| MAP-1 | V5 light lead on by default before Red's pick | Red decided 22:1x: keep V5 ON | level-transitions (pick) |
| MAP-4 | Direct, compile-time `DressWindow` call instead of the window contract | Facts true; headline and fix out of date. The map chat considers the call inside the try/catch correct; window-landing's r6 contract (R1/R2, +T) replaces it | window-landing → map chat |
| MAP-5 | Main's transition kit is V5's; other variants define a different class; one B0 rule must be chosen | Correct, but a planning note, not an error in the game | level-transitions |
| MAP-6 | Codex's N1 notes misstate the state; hook markers renamed | True; partly overtaken by Red's decision | level-transitions (comments) |
| R1 | ZBinningJob root cause; no guard | Same item as RT-F1 (one patch), described the past; Red's runs since 10-05 show 0 errors | glass-rt-track (RT-F1) |
| R3 | Task file still says "cause under audit" | A dated history entry written after Codex; the open item is RT-F1 (corrected anyway by DOC-D7) | — |
| R4 | Shipped print array slice K03 stale vs its source | Intended and recorded (`wallpaper_motion/32_pattern_native_hints.md` §9.6); rebuilding now would ship reserved frames; reverting K03 would undo 平面视觉's approved change | q1-live-print |
| RT-F6 | Strippers act only on WebGL; iOS/Android/Windows keep dead RT variants | Dead code with no pixel change | glass-rt-track (optional) |
| D1 | Should G14 stay ON before acceptance? | Red already decided: highest spec, "including glass ray tracing"; design §2.6 matches | — |
| GL-F5b | G14 traces every window slab, look never accepted | Duplicate of RT-F2 | glass-rt-track |
| GL-F3 | Glass look live without Red's sign-off (props, slabs, zone cubes, title) | Red's merge-as-you-go order allows it; the glass track was verified; a look review is Red's | Red (look sign-off) |
| COV-3 | The only Mac player on disk crashes; no Codex-era player ever ran into the map | Out of date: two newer Codex-era players exist and one ran into the map (10-08); the crashed app is old and git-ignored | — (COV-1 covers the RT part) |
| COV-6 | Codex's map content adds +4.0 % renderers; draw cost unmeasured | Overstated: drawn renderers +1.1 % (+156 of 14,286); window-landing measured draws; WebGL is gated (`Window.cs:166`) | WebGL track |

---

## 5. Later commits that fixed or changed Codex errors (after `75cfdff`)

| Commit | Date | What it did to a Codex error | Checked by |
|---|---|---|---|
| `9eddc35`, `bf2e883`, `70644f0` | 10-04 | Codex merged G14's `FRGlassRTPrepass` pass into `FrontRooms/Glass`: fixes "G14 traces nothing" (VL086), makes DOC-D2's owed shader piece land, and exposes V2 | glass-look (60 variants × 5 targets), RT (VL116, VL119), runtime (Play runs) |
| `663e858`, `1114d6b` | 10-04 | Kit importer cull fix + importer version 2: fixes K-F1 | VL114, runtime LOD dump, fix stage 23 / 23 |
| `04c3a6a`, `4448df9`, `b5f381f` | 10-04 | Codex added the CRT ads: source of V3, GL-F6, GL-F7, COV-4, COV-5, RT-F5 (demo scene) | glass-look, visual, coverage |
| `c1f2d31` | 10-04 | Codex's iPhone HUD: source of V1 and COV-7 | visual, coverage |
| `44fffd1` | 10-04 | `.gitignore` gains `__pycache__/`: HYG-H1 half fixed | coverage |
| `ee5c9bb` | 10-07 | Window facade r6 replaced Codex's r3 copy (merged by the visual chat; window-landing) | runtime (0 / 500 chunks differ), docs |
| `22bb75f` | 10-07 | Desktop key panel anchor: fixes V1 | runtime, visual |
| `2ce2087` | 10-10 | Codex's own 10-08 fix: `FrontRooms/ScreenVideo` shader from Resources: GL-F6's exception gone (rest in the package) | glass-look fix stage |
| working tree 10-14 | 10-14 | interactables-kit finals (fixes K-F5), COV-4, GD3 generator | MERGED.md |

No later commit touched `FrontRoomsGlassRTSystem.cs` or `FrontRoomsMetalGlassRTRendererFeature.cs` after `75cfdff`, so RT-F1…F5 and V2 are open on main. MapWorld changed in `b5f381f`, `bc0aa79` and `2ce2087`, none in Codex's hunks.

---

## 6. For each resuming workflow

### For window-landing
- Nothing left from K-F1 (`663e858`).
- Your r6 map contract (`research/interactables/window_landing/FrontRoomsMapWorld.window-kit.r6.diff`, R1 + R2; `…r6-trims.diff` = T) **still applies to today's MapWorld** (`git apply --check` on a copy, 10-14) and **does not conflict** with this audit's C1–C3 (all four applied in sequence on a copy). Send it with them; tell the map chat the order does not matter.
- T stays Red's call (dropped K-F2 / MAP-4): with T, the 3 hidden trim boxes per window (1,233 in 10 seeds) and 273 shadow renderers go.
- Open from the critics: VL107 (intact glass does not read as glass), K-F3's "one grammar" look question (Red).
- The task-file W1.4/W1.7 rows were corrected 10-08 (DOC-D4); when R1/R2 land, mark W1.4 done.

### For interactables-kit
- K-F5 is resolved by your 10-14 merge (cabinet `29d8332f`, GUID kept).
- Still open (dropped K-F4): 3 slots without a material in 18 kits: `Door_Enamel` (3), `Prop_KeyTagNo` (14, including the cabinet's new 4th slot), `Prop_SignEngraved` (1). P-4 needs Red's approval; no placeholders in main. Until then no game code may spawn `Kit_DoorSign` or a tag.
- K-F6 naming (`Kit_KeyHookBoard` vs `Kit_KeyRack`) is Red's pick; nothing renamed.
- `Kit_MiniBlind_Lowered` (Codex's uuid4 metas) is unused by code; keep its GUIDs.
- Your open map items (`40_merged.md` §7: hinge pivot A2, head dip numbers, door/key rigs) are not this audit's contracts.

### For glass-rt-track
- RT-F1 first (with E4), then RT-F2…F5 and COV-1 per §2.6 / §2.8.
- **V2:** if `codex_audit_all.sh` has run, your merge must **delete the whole `V2-STOPGAP` block** in `Describe()` and add the real paper × print path (`FR_KIND_SURFACE`, `NativePlugin/FrontRoomsGlassRT.metal` ~210–262, print slots in the RT material table). A 3-way rebase with your `proj_rt` copy conflicts on exactly that block, so `rebase_apply.sh` will ESCALATE (it cannot double-apply silently). Afterwards: one `_FR_PRINT` branch in `Describe()`; window frames 55 / 60 / 65 / 70 and 56 / 59 / 66 / 69 at ≤ 0.1 % red and the traced wall within ≈ 5 % of the raster wall. If you merge first, the combined script refuses: then apply `codex_audit_glass_look.sh` alone and retire `codex_audit_other_visual`.
- After `codex_audit_all.sh`, `codex_audit_runtime_rt.sh` will report 1 of 28 watched files moved (the stopgap). Expected.
- Keep main's folder GUIDs and the `DropChunk` fix (`9e754a2`).
- E2 (reflections do not follow props that change without moving) hits the CRT power toggle, the exit-sign states and the placard glow: decide with those owners whether to re-register on change.
- Rewrite the G14 task row when you merge (DOC-D1 left it accurate as of 10-08).
- Optional: RT-F6 (strip RT variants on iOS/Android/Windows).

### For exit-sign
- Merged 10-08 (76 paths, main's GUIDs kept for Codex's `Kit_ExitSign`/`_Dead`). The dropped K-F4 part for `Run_ExitSign_Dead` is resolved (the material now exists).
- Nothing from this audit is open for you. G14 E2 (dead/lit state not reflected in traced glass) is glass-rt-track's.

### For glass-break-build (GD3)
- DOC-D5 restored your row and "The glass lands together with W1"; the generator merged 10-14.
- When `FrontRoomsGlassBreakable` lands: hook onto the `Glass_Window` slab and the window root (W1.5); the window contract's R2 makes the pane's RT hint follow the visible glass; G14's fracture API (P0-A13) is FLAG in glass-rt-track's verify (150 ranges prepared 6 frames late, bar ≤ 4).
- Nothing else from this audit.

### For power-outlets
- No finding of this audit is yours. `outlets_int` shares no path with `codex_audit_all` (checked 10-14), so the two apply in either order.
- COV-6's note (Codex's map content +1.1 % drawn renderers) matters for the WebGL draw budget, not for you.

### For placard
- Merged 10-08. Nothing from this audit is open for you.
- G14 E2: the placard's phosphor glow is not reflected in traced glass (glass-rt-track).

### For office-dress-hitch
- Merged 10-08. The CRT fixes (COV-4 merged; GL-F6/F7 in `codex_audit_all.sh`) touch only `FrontRoomsScreenVideo.cs`, not your files.
- If the WebGL track gates the ads (COV-5 item 6), the gate goes in `FrontRoomsOfficeKit.AttachScreen` (`:851`), one of your merged files: route it through the visual chat.
- NEW-1 (CRT black at first sight) is 游戏视觉's screens work.

### For q1-live-print
- Dropped R4 stands as your note: slice K03 of `Resources/Print/FR_Print_HardEdge.png` is the old frame (meta sha1 `e2af19a6…` vs source `1f048331…`). Do not revert K03 (平面视觉 approved it, §9.6); rebuild the array with the driver promotion, after `print_tool.py raw_keyframes()` stops emitting `K02_half_drop` / `K07_turned`.
- When the live print lands: G14 must trace it too (V2's real fix adds print slots), and the zone cubes need a recapture (GL-F2 step 4).

### For level-transitions
- MAP-1/DOC-D3: Red keeps V5 ON; do not propose turning it off.
- **C2 replaces your `contracts/lightlead_lampmodeof_pure.diff`.** Do not send yours (both touch the same hunk).
- **MAP-2 / C3 is "the map's fix first"** that your `16_pick_and_close.md` §4 asks for. If your pick (Frame + renovation) brings its own corner rule, it must keep C3's property: a chunk-line post reads only pure data and its own chunk's edges (your frame and renovation kits read neighbour arms today).
- MAP-5: main's `FrontRoomsTransitionKit` is V5's (GUID `b215858f…`); merge into it, never copy a clone's kit over it.
- MAP-6: your `lightlead_comments_visual.diff` (visual-owned comments and `// TRANSITION` markers) is still yours to land.

---

## 7. Checks this stage ran (2026-10-14)

### 7.1 Environment
- Unity 6000.3.10f1 batch mode runs on macOS 27.0.1: 10 batch runs in this stage's clone (each a fresh compile when the map files changed), 0 `error CS`. Blender 4.3.0 runs (checked by the combine stage today).
- Clone `W/proj_cxreport` (`mkclone.sh`, main `0a9ee7f` + tree; local patch a no-op). One new clone.

### 7.2 Contracts
- Built against today's files by `W/cxreport/tools/make_contracts.py`; `git apply --check` passes for C1, C2, C4 alone and for C1 → C2 → C3 → C4 in order; window-landing's r6 diffs apply on the same base and together with C1–C3.
- Roslyn compile of all four on main's tree (macOS and iOS defines): 0 errors, 0 warnings.

### 7.3 Clone runs (main as is vs main + C1–C4)
Harness `FrontRoomsCxMapContracts` (clone only; copy in `contracts/harness/`).

| Check | Main as is | Main + C1–C4 |
|---|---|---|
| T1: still-built chunks ≠ their own rebuild after neighbour shifts (20 seeds) | 115 / 240 (B0 off: 0) | **0 / 240** |
| T1: chunk data generated beyond the 500 built | 429 | 30 (= B0 off) |
| T1b: chunk-line posts (5,600): notch / z-fight after shifts | 90 / 9 | **0 / 24** (24 also in a fresh build) |
| T2: far-cell `LampModeOf`, border rule on: chunks generated | 800 | **0** |
| T2: built lamps predicted wrong (15,970) | 0 | 0; every lamp mode identical to main |
| T3: `FrontRoomsMapInteractionTests`, border rule on | FAIL 151 / 1 | **PASS 152 / 0** |
| Project suites (default flags) | — | MapVerification 100 / 0, RelayNav 60 / 60, MapInteraction 152 / 0, LevelDesigner 139 / 0, FixtureTick 28 / 0, CameraRig 33 / 0, Captions 9 / 0, GlassShot 20 / 0, Stream 5 / 0 (same as main + payload, `40_applied.md` §3) |
| Map signatures, 20 seeds | — | 0 determinism failures; 422 of 500 chunks change wall/collider boxes (C3 at mixed chunk-line posts); counts and lights unchanged |
| R2 key after R (Play, RT High, 30 s, forced R at 12 s) | NULL (apply stage run on main + payload, 17:0x) | **`Map test / key` (URP Lit)**; 0 pink renderers; only the clone-only FMOD and QuickSearch exceptions |
| MAP-2 frame: seed 20261002, post (0, 4), after the shift | full-height slot (158,417 px differ from the rebuilt view) | corner whole (0 px differ); VL272 |

Logs: `W/logs/cxreport/`; outputs `W/proj_cxreport/Verification/cx_contracts/{main,main2,patched,patched2,sim3,final}/`, `…/cx_audit/`.

---

## 8. Map contracts

`contracts/README.md` lists them: C1 (R2), C2 (MAP-3), C3 (MAP-2, after C2), C4 (COV-7, after Red's pick). V1's contract was applied by `22bb75f`. The window contract (R1/R2/T) is window-landing's and is listed there for completeness.

## 9. Red decisions

In plain words, with recommended defaults: `00_main_state.md` §0.2.

## 10. Verification images

| VL | Check | Verdict |
|---|---|---|
| VL080 | Title rooms reflect Level 0 | WAIT-RED |
| VL081 → VL114 | Kit frames cull at 12 m → drawn past 12 m | FAIL → PASS |
| VL085 | RT teardown blackout fixed | PASS |
| VL086 → VL116 | G14 adds nothing → now traces window glass | FAIL → FLAG |
| VL087, VL117 | G14 main thread | FAIL |
| VL115 → VL226 | CRT quad outlives monitor → culls with it | FAIL → PASS |
| VL118 | An RT exception freezes frames | FAIL |
| VL119 | G14 shader hook proof on main | PARTIAL |
| VL126 | Zone key turns pink after R | FAIL |
| VL151 | Main vs pre-Codex, 84 frames | FLAG |
| VL152 | Key panel off-screen | FAIL (fixed by `22bb75f`) |
| VL153 → VL223 | G14 paints wallpaper red → red slab gone | FAIL → PASS |
| VL154, VL224 | CRT ad shows a real brand; LEVITZ or Ad_02 | FAIL, WAIT-RED |
| VL227 | CRT screen material in players | PASS |
| VL244 | Codex fixes hold together | PASS |
| VL245 | CRT dark at first sight | FLAG |
| VL272 | Notch at a chunk line (MAP-2; this stage, `3150:7042`) | FAIL main / PASS C3 |
| VL273 | What main ships for N1 (the visual review's 10-07 sheet, logged by this stage, `3152:7042`) | FINDING |

All rows are in `Documentation/VERIFICATION_LOG.md` §3. This stage placed VL272 and VL273 (section grown to 69 rows, 85,640) and recounted the cover from the canvas: 27 tasks · 272 checks · 733 images; codex-audit legend VL080–273 · 24 checks.

## 11. Files

- This file; `00_main_state.md` (updated §0); `contracts/` (README, C1–C4 `.md` + `.diff`, `harness/`).
- Work files outside the project: `W/cxreport/` (tools, base copies, patched copies, diffs), `W/logs/cxreport/`, clone `W/proj_cxreport`.
