# Codex audit · 40 · Combine and apply (codex_audit_all)

Stage: combine stage of the Codex audit, run 2026-10-14 16:36–17:05 PDT on main `0a9ee7f` plus the working tree.
Nothing was written under `Assets/` of Red's project. Nothing was committed. Nothing was deleted.

## 0. Result per batch

| Batch (fix group) | Files | Result | Command / reason |
|---|---|---|---|
| glass-look (F6 CRT material, F7 quad in the monitor LODs) | 4 | **READY-TO-APPLY** | in the combined package below; or alone: `bash /Users/redwang/FrontRoomsVisualWork/apply/codex_audit_glass_look.sh --apply` |
| other-visual (V2 stopgap: RT window glass no longer paints the L0 wallpaper red) | 1 | **READY-TO-APPLY** | in the combined package below; or alone: `bash /Users/redwang/FrontRoomsVisualWork/apply/codex_audit_other_visual.sh --apply` |
| kits+window (COV-4, CRT quad mesh leak) | 1 | **APPLIED** (2026-10-14 16:2x by the visual chat, `W/apply/MERGED.md`) | its dry run says `ALREADY APPLIED`; the glass-look payload keeps its `MakeQuad` byte for byte |
| runtime+rt (F1–F5, COV-1) | 0 | **NOT APPLIED** (nothing to apply by design) | all six findings are the glass-rt-track's; its script prints `NOTHING TO APPLY` |
| docs | — | skipped (edited in place on 10-08, as the task says) | `30_fix_docs.md` |
| other-visual V3 (Ad_02 swap for the LEVITZ ad) | 1 | **NOT APPLIED** | waits for Red's V3 choice (a/b/c); own script needs `--red-approved` |

**The one command** (the visual chat runs it; subagents do not write into `Assets/`):

```
bash /Users/redwang/FrontRoomsVisualWork/apply/codex_audit_all.sh --apply
```

Dry run against Red's project at 17:0x: `DRY RUN OK: every base matches (members to apply: codex_audit_glass_look codex_audit_other_visual)` (`apply_all/dryrun_main_final.txt`).

After the apply:
- Red's open editor imports one material and recompiles once.
- F6 reaches players only after Red's next Mac and iOS (TestFlight) builds.
- Red should commit the untracked `Assets/Resources/Rendering/FrontRoomsScreenVideo.shader` and its `.meta` (Codex, 10-08) with the next "1". The package keeps that shader as fallback 2.

## 1. What the package writes

5 paths, in this order (`.meta` before its asset, code last). sha1 = the file after `--apply`.

| Member | Action | Path | sha1 after | Base sha1 |
|---|---|---|---|---|
| glass-look | add | `Assets/Resources/Media.meta` (folder GUID 559c9d02…) | `fc192900` | absent |
| glass-look | add | `Assets/Resources/Media/CRT_AdSurface.mat.meta` (GUID e246c762…) | `0ba14a95` | absent |
| glass-look | add | `Assets/Resources/Media/CRT_AdSurface.mat` (URP Unlit, opaque, white) | `b8d02e1a` | absent |
| glass-look | replace | `Assets/Scripts/Media/FrontRoomsScreenVideo.cs` | `ea0c85cf` | `37d6d223` |
| other-visual | replace | `Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRTSystem.cs` | `e0b81f36` | `154ae4e6` |

Kept and never written (a changed `.meta` refuses the apply): `FrontRoomsScreenVideo.cs.meta` (5b16edd7, GUID 93c5f64b), `Assets/Resources.meta`, `FrontRoomsScreenVideo.shader` + `.meta` (Codex's fallback), `FrontRoomsGlassRTSystem.cs.meta` (7e4f42f4, GUID 4492b1a2).

No map-owned file, no `ProjectSettings`, no `Library/`. The script refuses any such path.

## 2. Bases re-checked against Red's files

Main had not moved since the member stages (HEAD `0a9ee7f`; the tree holds the interactables-kit finals, COV-4 and the GD3 generator, all merged today). Dry runs at 16:36:

| Script | Result |
|---|---|
| `codex_audit_glass_look.sh` | DRY RUN OK (4 paths; rebased today over COV-4 by its own stage) |
| `codex_audit_other_visual.sh` | DRY RUN OK (1 path) |
| `codex_audit_kits_window.sh` | ALREADY APPLIED |
| `codex_audit_runtime_rt.sh` | NOTHING TO APPLY; 28 watched files unchanged |
| `codex_audit_v3_ad02.sh` | DRY RUN OK; needs `--red-approved` |

Every base matched, so no rebase was needed (Red's rule b). The two members touch different files.

## 3. Combined verification (W/proj_cxapply_all)

Clone: `W/tools/mkclone.sh proj_cxapply_all` (main 0a9ee7f + tree, local patch a no-op). Both member scripts were run with `APPLY_REAL=<clone> --apply`: MANIFEST OK for both. So the clone holds exactly the files the package writes. After Unity imported them, all 7 package and kept sha1s were still the same.

Unity 6000.3.10f1 batch mode runs on macOS 27.0.1 (this compile). Blender 4.3.0 runs (`--version`).

| Check | Result | Log |
|---|---|---|
| Compile, `-buildTarget OSXUniversal`, ScriptAssemblies deleted first | **0 errors, 0 warnings** | `W/logs/cxapply_all/c01_compile.log` |
| Play Mode 120 s, G14 High, seed 516574485 | 7,307 frames, 3 restarts after catches, 145 chunk teardowns; G14 "on: Apple9, High", `commandBufferErrors` 0; **0 new exceptions**: only the 2 clone-only ones (FMOD bank, QuickSearch); warnings are the same kinds as in the main and V2-only runs; 1 null-material Key renderer = the known map-owned R2 key | `apply_all/play_all120_summary.txt` |
| Edit-mode suites | MapVerification 100/0, RelayNav 60/60 PASS, MapInteraction 152/0, LevelDesigner 139/0, FixtureTick 28/0, CameraRig 33/0, Captions 9/0, GlassShot 20/0, Stream 5/0; glass compile check byte-identical to main's | `apply_all/suites_log.txt` |
| 20 map seeds | 0 second-world and 0 rebuild determinism failures; `map_signatures.tsv` byte-identical to main and to the V2-only clone | same |
| Capture set (`FrontRoomsCxVisCapture`, RT High, 84 frames): title rooms, the L0/Office border, 4 windows from both faces, glass, Relay | 0 black, 0 magenta frames. Traced red (R−G rise > 10 vs RT Off): main up to **9.99 %** (9 window frames over 0.1 %), combined at most **0.04 %** (0 frames over 0.1 %). 67 of 84 frames identical to the V2-only run, 84 of 84 within mean \|ΔY\| 0.5. Only frame 48 differs by more than 0.1 (the CRT, §4) | `apply_all/cxvis_compare.tsv`, `cxvis_summary.json` |
| CRT quad harness `CxFixKwLeak` (kits+window's, asked of this stage) | 20 monitors × 3 spawn/destroy cycles: 1 quad mesh; 2 sizes: 2 meshes; every screen URP Unlit. LOD readout 65 PASS / 0 FAIL. Shader variants 48/48 OK (GLES3/WebGL, WebGPU, Metal macOS + iOS, D3D, Vulkan) | `apply_all/crt_leak_all.txt` |

Images (Figma **VL244**, `3128:7042`, PASS):
- `images/all_combined_frames_sheet.jpg`: 8 frames of the combined state (title Lobby and Office, L0/Office border and its post, window room and hall faces, hall face at 0.3 m with RT High, the Office zone).
- `images/all_window_hall_red_pair.jpg`: hall face at 0.3 m, main 9.99 % red vs combined 0.01 %.

## 4. New finding: a CRT is black for a moment at first sight (FLAG)

Figma **VL245**, `3128:7056`, FLAG. Image: `images/all_crt_first_sight_strip.jpg`.

- **What:** frame 48 (the CRT close-up, 6 frames after the camera arrives) shows a black screen in the combined state. Screen-box luma: 40.3 with main's screen code, **16.8** combined, 38.1 combined with F7 switched off (a clone-only test). Frame 49, 0.15 s later at the same desk, shows the ad.
- **Cause: F7, working as designed.** F7 now culls the ad quad with its monitor (11.2 m at High). Before F7, far quads kept drawing, counted as visible, and kept the shared ad bank warm. Now the bank prepares only when a monitor is in cull range and in view. Its RenderTexture is black until the first frame decodes.
- **At Ultra nothing changes:** the cull (22.4 m) is beyond the 18 m `visibleDistance`.
- **Not blocking.** F7 removes a visible bug (ad quads floating where culled monitors stood). The gap is a fraction of a second, only at first sight.
- **Owner:** 游戏视觉, screens rework (`SCREENS_VISUAL_SYSTEM.md`). Suggested fix: prepare the bank on `Register` (not on first visibility), and show a dark-tube frame until the first video frame.
- Why the member stages did not see it: glass-look's `CxCrtFix` scene keeps its bank warm, and the V2-only run used main's screen code.

## 5. WebGL player check (COV-5 item 4)

Second clone `W/proj_cxapply_all_wgl` (mkclone + both payloads), started with `-buildTarget WebGL`. Clone-only harness `CxAllWebGLBuild` (copy: `apply_all/CxAllWebGLBuild.cs.txt`):
- it applies the project's own `FrontRooms3DBuild.ApplyWebGLSettings` (Brotli, IL2CPP, High stripping, exceptions None, quality High);
- then three probe-only overrides: development build, compression Disabled (so the data file can be read), explicitly thrown exceptions logged;
- it builds glass-look's one-monitor probe scene (`CxCrtPlayerProbe`). It spawns `Kit_CRTMonitor` through `FrontRoomsKitLibrary.Spawn` + `FrontRoomsScreenVideo.Attach`, the same path the Office kit uses.

Build: Succeeded in 338 s, 320 MB. Its 1 error is the clone-only FMOD bank refresh (no FMOD project folder in clones).

**Shader name table** (`globalgamemanagers` read from `data.unity3d`; extractor `apply_all/webgl_extract.py.txt`, table reader `W/cxfix_glass_look/tools/shtable.py`):

| Shader | PPtr |
|---|---|
| Universal Render Pipeline/Unlit | (4, 694) non-null |
| FrontRooms/ScreenVideo | (4, 693) non-null |
| Unlit/Texture | (0, 0) null (no longer used by the payload) |
| URP/Lit, FrontRooms/Surface, FrontRooms/Glass | non-null |

**In a browser:**
- Setup: the Claude desktop Browser pane (Chrome 152, WebGL 2.0 / GLES3), page served from 127.0.0.1.
- The pane was hidden, which pauses `requestAnimationFrame`. So a page hook drove frames with a MessageChannel and kept the console lines (`apply_all/webgl_probe_console.txt`).
- Result: `Shader.Find URP/Unlit True`, `Resources Media/CRT_AdSurface True`; spawn exception none; material `FrontRooms / CRT ad surface`, shader URP Unlit, supported; collider True; quad in LOD0 and LOD1; video prepared and playing.
- **0 exceptions, 0 "Parameter name: shader".** The only errors are 3 engine-internal hidden URP shaders that WebGL 2 does not support (CoreCopy, StencilDitherMaskSeed, HDRDebugView); this package does not touch them.
- Limit: this is one monitor in a probe scene, not a full Office room in the real game. The video's first decoded frame was not observed (hidden pane, `frame -1`). Live video vs a poster on WebGL stays with the WebGL track (COV-5 item 6).

## 6. The apply script

`/Users/redwang/FrontRoomsVisualWork/apply/codex_audit_all.sh` (copy: `apply_all/codex_audit_all.sh.txt`). Convention v2:
- `codex_audit_all_{bases,keep,expected,members}.txt`, `_payload/`, `_base/` (exact base copies), `.diff`;
- built from the two member packages by `W/cxapply_all/tools/build_package.sh`, which checks every payload and base copy by sha1.

How it works:
- The dry run is the default. `--apply` copies exactly the 5 verified files.
- It backs up main's replaced files to `W/backup/codex_audit_all_<stamp>/` and writes in place (no temp file inside `Assets/`).
- It prints the sha1 manifest and compares it with `codex_audit_all_expected.txt` (MANIFEST OK), then saves `codex_audit_all_applied_manifest.txt`.
- It refuses, with nothing written, on: any base that is neither the recorded base nor the payload; a changed kept `.meta`; a changed payload; a half-applied member; or a path outside the visual chat's scope.
- A member that is already fully applied is skipped. So running a member's own script first is safe.
- If `FrontRoomsGlassRTSystem.cs` moved, it says to retire the V2 stopgap and apply the CRT member alone.

Scratch tests (copies of main's files under `W/cxapply_all/scratch`; `apply_all/apply_script_tests.log`):

| Test | Result |
|---|---|
| fresh `--apply`, then dry run | MANIFEST OK, then ALREADY APPLIED |
| glass-look's own script first, then this one | dry run lists other-visual only; `--apply` MANIFEST OK; then ALREADY APPLIED |
| one glass-look file planted (half applied) | REFUSED, nothing written |
| `FrontRoomsGlassRTSystem.cs` changed (glass-rt-track merged) | REFUSED with the retire-the-stopgap message; ScreenVideo.cs untouched |
| kept `.meta` changed | REFUSED |
| the combined clone | ALREADY APPLIED |

## 7. Hand-offs and notes

- **glass-rt-track:** its merge must delete the whole `V2-STOPGAP` block in `Describe()` (`30_fix_other_visual.md` §4). After this apply, `codex_audit_runtime_rt.sh` will note 1 of 28 watched files moved (this stopgap). That is expected; its probe still reports F1/F4/F5 open.
- **If the glass-rt-track merges first:** this script refuses. Run `codex_audit_glass_look.sh --apply` alone and retire `codex_audit_other_visual`.
- **游戏视觉 (screens):** VL245, CRT black at first sight (§4).
- **WebGL track:** COV-5 item 6, live video vs a poster on WebGL. A WebGL-only gate would go in `FrontRoomsOfficeKit.AttachScreen` (:851).
- **Red:** V3 (LEVITZ ad) is still open: (a) kill switch, (b) `codex_audit_v3_ad02.sh --apply --red-approved`, or (c) trim the first 3 s.
- **Visual chat records:** add a `codex_audit_all` row to `W/apply/MERGED.md` after the apply. `VISUAL_CHAT_TASKS.md` should change other-visual from "unverified" to "verified 10-14, READY". `20_findings.md` was never written; the per-track facts live in the `30_fix_*.md` files and here.
- Not changed by this stage: any map-owned file (no map contract arose), `VISUAL_CHAT_TASKS.md`, and the other chats' sections.

## 8. Figma

| VL | Node | Verdict | Images |
|---|---|---|---|
| VL244 Codex fixes hold together | `3128:7042` | PASS | `images/all_combined_frames_sheet.jpg`, `images/all_window_hall_red_pair.jpg` |
| VL245 CRT dark at first sight | `3128:7056` | FLAG | `images/all_crt_first_sight_strip.jpg` |

- Section FRONTROOMS · VISUAL VERIFICATION LOG grown to 62 rows (76,960). The bounds check found nothing within 400 px below it.
- Cover recounted from the canvas: 26 tasks, 244 checks, 688 images. The codex-audit legend now reads VL080–245 · 22 checks.
- Both rows are recorded in `VERIFICATION_LOG.md`. The next free number is VL246.

## 9. Clones and work files (nothing deleted)

The visual chat can remove these after the apply:
- `W/proj_cxapply_all`: the combined clone (4.3 GB on disk, mostly shared APFS blocks). It holds the clone-only `CxFixKW` harness and a restored payload.
- `W/proj_cxapply_all_wgl`: the WebGL clone (6.1 GB, WebGL Library).
- `W/cxapply_all/webgl`: the probe build (0.3 GB).

Keep these until the apply is recorded: `W/cxapply_all/tools`, `W/cxapply_all/scratch`, `W/logs/cxapply_all`.
