# Codex audit · 30 · Fix phase, group "other-visual" (V2, V3)

Date: 2026-10-08 19:34–20:10 PDT (first run, cut off by the weekly limit before this report) and **2026-10-14 16:18–16:40 PDT (re-run, this report)**.
Main: **`0a9ee7f` (2026-10-10) + Red's working tree** (it holds the 10-14 merges: interactables kit finals, COV-4, GD3 generator).
W = `/Users/redwang/FrontRoomsVisualWork`. Clones: `W/proj_cxfix_other_visual` (main + the V2 payload) and `W/proj_cxfix_ov_base` (main only). Both were made on 10-08 and re-synced from main on 10-14 (rsync, `apply_local_patches.py`: "nothing to do", `install_tools.py`).
Unity was never opened on Frontrooms3D. Nothing was committed. No file under `Assets/` of Red's project was changed. Writes in Red's project: this folder (report, images, stats) and two rows plus one sentence in `Documentation/VERIFICATION_LOG.md`.

## 0. Short answer (for Red)

- **V2 (red slab in ray-traced window reflections): fixed, verified, ready to merge.**
  - One file: `Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRTSystem.cs`, +10 lines in `Describe()`. Its `.meta` is not touched.
  - Effect: with RT High, the four hall-side window shots go from 7.67 / 3.56 / 5.08 / 9.99 % red pixels to 0.00 / 0.02 / 0.00 / 0.01 %. Nothing else changes.
  - It is a stopgap. Walls in the reflection get one flat ink tone (about 2 % darker than the real wall), not the printed pattern. The real fix (paper × print in the Metal hit shader) belongs to **glass-rt-track**, and their merge must delete this block (§4).
  - **Command for the visual chat:** `bash /Users/redwang/FrontRoomsVisualWork/apply/codex_audit_other_visual.sh --apply` (dry run OK on 10-14 16:3x).
- **V3 (the LEVITZ ad on every Office CRT): not changed. Red decides.**
  - Still true on main: `FrontRooms_Ad_01.mp4` shows the real LEVITZ brand for 3 s of every 12 s, and the "FRONTROOMS FURNITURE" title is cut off at both edges.
  - Option (b) is ready as a separate package: `bash /Users/redwang/FrontRoomsVisualWork/apply/codex_audit_v3_ad02.sh --apply --red-approved` (only after Red picks (b)). It swaps in 平面视觉's finished Ad_02 under the same file name. No code changes.
  - Options (a) and (c) are in §5.
- **No map-owned file is involved**, so there is no contract patch (`contracts/` gets nothing from this group).

## 1. Findings → decision

| ID | Still true at `0a9ee7f` + tree? | Change here | Why | Handoff |
|---|---|---|---|---|
| **V2** G14 traces `_FR_PRINT` wallpaper with the paper code as albedo | **Yes.** `FrontRoomsGlassRTSystem.cs` sha1 `154ae4e6…` (md5 `e75df1b1…`) is unchanged since 75cfdff. `Describe()` still sets `r.texBase = Slot(m, "_BaseMap")` for every FrontRooms/Surface material. The Metal hit shader (`NativePlugin/FrontRoomsGlassRT.metal`, sha1 `0c212e3e…`, unchanged) uses it as albedo. `L0_Wallpaper`, `L0_Wallpaper_Shift`, `Exit_Wallpaper` and `Wallpaper_Paper_M.png` are unchanged since bc0aa79. No commit after 75cfdff fixed it | **Stopgap applied in the clone** (§2), packaged | It visibly damages Red's reference tier now: RT High is the default on his M3 Max (Editor.log 10-08: `on: Apple9, High`, `map found` ×4). A wrong material colour, not taste | **glass-rt-track**: real paper × print in `FR_KIND_SURFACE` + print slots in the RT material table; their merge deletes the stopgap (§4) |
| **V3** Codex's CRT ad shows the real LEVITZ brand; title clipped | **Yes.** `FrontRooms_Ad_01.mp4` md5 `f9ec1412…` (sha1 `f1699126…`) unchanged since 04c3a6a. `FrontRoomsOfficeKit.cs:851` `AttachScreen` → `FrontRoomsScreenVideo.Attach`, used at `:875` for every station monitor. Plays in map Office zones and in the title corridor's Office rooms (`FrontRoomsRoomStream.cs:1440`) | **None** (Red decides) | The verifiers' corrected fix: no audit-side edit until Red picks. The feature itself was Red's request (10-04); only the content is wrong | **Red** picks (a), (b) or (c) (§5). Content: **平面视觉** (GRAPHIC_VISUAL_TASKS §3, §3a "Held for Red" item 1). Tube layer and F6: **游戏视觉** |

Corrections to the V3 finding, as both verifiers asked (for the findings ledger):
- The "research only, must not ship" rule is in `Research/week02/furniture-ads/SOURCES.md:5`, quoted by `SCREENS_VISUAL_SYSTEM.md` P1. `GRAPHIC_VISUAL_TASKS.md` §3 only lists it as P1.
- "No trademarks" is in `room_visuals/01_inventory.md:147` and `interactables/02_period_hardware.md:347`; `22_era_lock.md` §2 does not say it literally.
- Scope is wider than the finding: also the title corridor's Office rooms (RoomStream:1440). The editor-only demo scene (`Assets/Editor/FrontRoomsScreenAdsDemoScene.cs:51`) also attaches it; that is harmless.

## 2. The V2 change

Package `W/apply/codex_audit_other_visual.sh` (apply convention v2). Payload sha1 `e0b81f36…` = main's file + this block, inserted after the `_FR_MESH_UV` line in the Surface branch of `Describe()`:

```csharp
            // V2-STOPGAP: remove when MatPrint lands (glass-rt-track); codex audit V2, research/codex_audit/30_fix_other_visual.md.
            // _FR_PRINT wallpapers: _BaseMap holds the paper modulation code (FrontRoomsSurface.shader,
            // FR_PAPER_*), not a colour; traced as albedo it reads red-magenta (texel mean 195/15/51).
            // Until the hit shader composes paper x print, trace the print's mean ink tone, flat.
            if (m.IsKeywordEnabled("_FR_PRINT"))
            {
                r.texBase = Native.TexWhite;
                var ink = Vector4.Lerp(Lin(m, "_InkGround", new Color(.8235f, .7608f, .4863f)), Lin(m, "_InkMid", new Color(.6745f, .6039f, .3216f)), .55f);
                r.baseColor = new Vector4(r.baseColor.x * ink.x, r.baseColor.y * ink.y, r.baseColor.z * ink.z, r.baseColor.w);
            }
```

- It is the reviewed patch `vis_F2_rt_print_albedo.diff`, with the marker comment the verifiers asked for.
- **Scope: RT only.** `Describe()` is called only from the Metal RT material build. Raster, RT Off, WebGL and iOS never run it. Mac/Win raster is untouched.
- Inks come from each material, so `Exit_Wallpaper` keeps its cyan inks. The `_BaseColor` multiply is kept, as in raster (`PrintAlbedo(uv) * _BaseColor`).
- Accuracy (measured 10-08 on the same, unchanged textures and materials): the raster wall's mean albedo for `L0_Wallpaper` is (0.523, 0.430, 0.148) linear; the flat ink is (0.517, 0.421, …): luminance −2.1 % (L0), −2.2 % (Exit). The optional cream term (+0.126 × 0.55 toward `_InkCream`) would close part of that gap. I left it out: it is under 2.5 %, only in reflections, and the block is temporary.

## 3. Evidence (re-run on main `0a9ee7f` + tree, 2026-10-14)

All runs: `-buildTarget OSXUniversal`, Metal, Apple M3 Max, batch with graphics. Logs in `W/logs/cxfix_ov/` (`q01_*`, `q_fix_*`, `q_base_*`); the 10-08 run's logs (`b0*`, `p0*`) are in the same folder.

**Machine check (macOS 27.0.1):** Unity 6000.3.10f1 batch compile runs (`Exiting batchmode successfully`); Blender 4.3.0 runs (`-b --factory-startup`).

**Compile.** `Library/ScriptAssemblies` deleted first so Assembly-CSharp and -Editor are rebuilt (both show `Csc … Assembly-CSharp.dll`): fix **0 `error CS`, 0 `warning CS`**; base 0 / 0. No new warning.

**Apply package checks.**
- Dry run against Red's project: `DRY RUN OK` (base sha1 `154ae4e6…`, kept `.meta` `7e4f42f4…` = GUID `4492b1a2…`).
- `W/tools/rebase_apply.sh codex_audit_other_visual --no-install`: **PASS**, nothing to rebase.
- `--apply` on a scratch copy (`APPLY_REAL`): `MANIFEST OK`; a second run prints `ALREADY APPLIED`.

**Project suites + 20 validator seeds** (`FrontRoomsCxSuites.RunBatch`), fix vs base:

| Suite | Fix | Base |
|---|---|---|
| MapVerification | 100 / 0 | 100 / 0 |
| RelayNavTest | PASS · 60/60 hunts | PASS · 60/60 hunts |
| MapInteractionTests | 152 / 0 | 152 / 0 |
| LevelDesignerTests | 139 / 0 | 139 / 0 |
| FixtureTickTests | 28 / 0 | 28 / 0 |
| CameraRigTests | 33 / 0 | 33 / 0 |
| CaptionsTests | 9 / 0 | 9 / 0 |
| GlassShotTests | 20 / 0 | 20 / 0 |
| StreamVerification | 5 / 0 | 5 / 0 |
| GlassCompileCheck (shaders) | 28 OK | 28 OK |
| Map signatures, 20 seeds | 0 determinism failures; 0 rebuild failures | same |

The two `map_signatures.tsv` files are byte-identical (1,501 lines). Console: 4 `InvalidOperationException` inside MapInteractionTests in both (the suite's own negative tests).

**120 s Play Mode, seed 516574485, G14 High** (`FrontRoomsCxPlayRun`):

| Run | Frames | Exceptions | G14 | Notes |
|---|---|---|---|---|
| fix | 7,307 | 2: `BankLoadException` (clones carry no FMOD banks) + `ArgumentOutOfRangeException` (QuickSearch at editor start). Both clone-only, W/TOOLS.md §2 | `on: Apple9, High`, `commandBufferErrors 0`, 68 `Glass_Window` renderers on prepass bit 30 | The autopilot was caught 4 times, so the map was torn down and rebuilt 4 times under RT: 0 RT exceptions. 1 pink renderer after a restart = the known zone key after R (`10_review_runtime.md` R2, map contract; not RT). 29 FMOD "port 9264 in use" warnings (parallel Unity jobs) |
| base | 7,307 | the same 2 | `on: Apple9, High`, `commandBufferErrors 0`, 46 renderers | 0 catches |

**0 new exceptions.** The catch count is gameplay variance between two parallel runs (frame timing), not the patch: `Describe()` only fills the RT material table.

**Window frames** (`FrontRoomsCxVisCapture`, 84 frames; base RT High, fix RT High, fix RT Off; frame timings identical). Red = pixels whose R−G rose by more than 10 against RT Off:

| Frame | Main, RT High (10-14) | + V2, RT High (10-14) | 10-08 run (bc0aa79) |
|---|---|---|---|
| 55 win1 OF hall 0.3 m | 7.67 % | 0.00 % | 7.57 → 0.00 % |
| 60 win2 L0 hall 0.3 m | 3.56 % | 0.02 % | 3.59 → 0.02 % |
| 65 win3 OF hall 0.3 m | 5.08 % | 0.00 % | 5.10 → 0.00 % |
| 70 win4 L0 hall 0.3 m | 9.99 % | 0.01 % | 9.98 → 0.00 % |
| 56 / 59 / 66 / 69 (1.5 m) | 2.70 / 3.23 / 2.35 / 1.05 % | ≤ 0.01 % | ≤ 0.01 % |

- Acceptance (R−G rise > 10 on ≤ 0.1 % of pixels): **met on all 84 frames**. The highest red in any fix frame is 0.04 % (frame 57, the same in main).
- Fix vs main: 65 of 84 frames have equal luma; 77 within mean |dY| 0.5. The 7 above 0.5 are all window views (0.52–1.08). The held-pane glass frames 79–82 move by 0.001–0.018.
- Stats: `fix_other_visual/v2_stats_1014.{tsv,json}`, `v2_summary_1014.json` (10-08: `v2_stats.{tsv,json}`, `v2_summary.json`; script `v2_stats.py.txt`).

**Images** (`research/codex_audit/images/`; from the 10-08 run, whose inputs are unchanged; the 10-14 frames match them within 0.1 % red):
- `fix_other_visual_v2_pair_win4_L0_hall_head_0.3m.jpg` (main vs fix) — **VL223** `3117:7009`
- `fix_other_visual_v2_pair_win2_L0_hall_1.5m.jpg` — VL223
- `fix_other_visual_v2_redmask_sheet.jpg` (red mask, 4 hall shots) — VL223
- `fix_other_visual_v2_win{1..4}_*.jpg` (RT Off | main | fix triptychs; not on the slide)
- `fix_other_visual_v3_crt_ingame_pair.jpg` (LEVITZ vs Ad_02 on the Office CRT) — **VL224** `3117:7020`
- `fix_other_visual_v3_crt_crop.jpg` (×3 crop) — VL224
- `fix_other_visual_v3_ad02_file_sheet.jpg` (Ad_02 at 1 fps) — VL224

The V3 Ad_02 frames were captured on 10-08 in the base clone with a clone-only dwell in the capture harness (the decoder plays ≥ 30 frames before frame 48). That harness edit is gone after the 10-14 re-sync.

## 4. Handoff rule for glass-rt-track (their merge MUST do this)

`20_findings.md` does not exist yet, so the rule lives here and in the apply script's header.

1. **When their paper × print fix merges, delete the whole `V2-STOPGAP` block.** Their `Describe()` adds its own `_FR_PRINT` block (`Native.MatPrint`, `printST`, inks, `texPrint`). Their Metal side decodes the paper from `texBase`, so it needs the real `_BaseMap` slot and the plain `baseColor`. With both blocks, walls would trace roughly ink², about (0.27, 0.18, 0.02): dark olive-brown.
2. **The merge cannot slip through silently.** Tested 10-14: `git merge-file --diff3` of `W/proj_rt` (sha1 `eba3a9d7…`, 10-07 23:28) against this payload over base `154ae4e6…` gives **1 conflict**, at exactly these two blocks. So `rebase_apply.sh` reports ESCALATE. Resolve it by taking theirs and dropping the stopgap.
3. **Their verification:** grep `Describe()` for exactly one `_FR_PRINT` branch; re-run the window set (frames 55/60/65/70 and 56/59/66/69); acceptance R−G rise > 10 on ≤ 0.1 % of pixels, and the traced wall mean within about 5 % of the raster wall.
4. **If glass-rt-track merges first,** do not run this package. Its dry run will refuse on the changed base; retire it.

## 5. V3: options for Red (unchanged; nothing applied)

| Option | What | Cost | How |
|---|---|---|---|
| **(a)** Kill switch | Comment out `FrontRoomsScreenVideo.Attach(monitor)` in `AttachScreen` (`FrontRoomsOfficeKit.cs:851`). Monitors go back to `Prop_ScreenCRT` in map Office zones and the title corridor's Office rooms | Removes a feature Red asked for (and its E power toggle); parks glass-look F6/F7. Layout unchanged (`Attach` uses no rng). The mp4 still ships in builds, unplayed | One-line visual-owned edit; no package made (Red has not picked it) |
| **(b)** Ad_02 in its place | 平面视觉's `Tools/screens/out/FrontRooms_Ad_02/FrontRooms_Ad_02.mp4` (fictional "Front Rooms Furniture", WXRM-TV ident, 640×480 4:3, 16 s, sha1 `058d2e28…`) copied over `Assets/StreamingAssets/FrontRooms_Ad_01.mp4`. `.meta` and GUID kept; `DefaultStreamFile` unchanged | None in code. The 4:3 clip is pillar-boxed (FitInside) in the 640×434 target. COV-4 (shared quad per screen size) does not interact with it | **Ready:** `bash /Users/redwang/FrontRoomsVisualWork/apply/codex_audit_v3_ad02.sh --apply --red-approved`. Dry run OK 10-14; scratch `--apply`: MANIFEST OK, then ALREADY APPLIED; it refuses without `--red-approved` |
| **(c)** Trim | Re-encode Ad_01 without its first 3 s (cards 2–4, 9 s loop) | Removes the trademark; the clipped title stays until (b) | Not made; only if Red asks |

Evidence: VL224 (`3117:7020`). Ad_02 plays on the Office CRT and reads at the ×3 crop.

## 6. Files changed vs main (if the visual chat runs the V2 package)

| Path | Change | sha1 before → after |
|---|---|---|
| `Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRTSystem.cs` | +10 lines (V2-STOPGAP block) | `154ae4e6587b938f01c7ae5098d3668678e2e50c` → `e0b81f363e60a92fbe4825f1be87a14731b91690` |
| `Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRTSystem.cs.meta` | **not written**; checked as kept (GUID `4492b1a2ebeda4d59acb4842d508438a`) | `7e4f42f47d605bd3cf65e17b2426361eb05142e0` (unchanged) |

V3 package (only after Red picks (b)): `Assets/StreamingAssets/FrontRooms_Ad_01.mp4` `f169912626b5bf89a526cae18e8912ce0497fe7e` → `058d2e28e45cc6df3c88ab2680ea9b6bb2a7fa6c`; its `.meta` `843450b7…` kept.

Bases of every real-project file this group read or may touch: `W/proj_cxfix_other_visual.bases.txt` (re-recorded 10-14 16:23 on `0a9ee7f`; the 10-08 block follows it).

## 7. Housekeeping

- **Clones that can be removed after the V2 merge** (not deleted): `W/proj_cxfix_other_visual` (4.5 GB) and `W/proj_cxfix_ov_base` (5.5 GB). Keep `W/cxfix_ov/` (4 MB: stats script, images, the 10-08 transcript digest).
- `W/proj_cxfix_other_visual.CLAIM` marks the two clones as this stage's; it can go with them.
- `Documentation/VISUAL_CHAT_TASKS.md` still says "other-visual … package exists, unverified". That file is the visual chat's; it should now read "verified 10-14, READY".
