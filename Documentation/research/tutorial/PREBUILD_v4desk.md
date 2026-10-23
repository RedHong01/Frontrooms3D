# V4 still desk screens · pre-built integration (游戏视觉)

Tutorial T3 (Red approved it in the 教程设计 chat on 10-22: "T3 静止屏"). 总控 chose wiring (a) at 15:3x: the rule is bound to "this floor has a station". Red, 总控 chat 15:02: "开始把所有能够integrate到主build里的所有已经完成的资产都开始integrate，不用等alpha了".

This is the build report of workflow prebuild-integrations (wf_d1d3c10c-3b4), item V4. It was built and verified in the clone `W/proj_v4desk`; the work folder is `W/v4desk` (W = `/Users/redwang/FrontRoomsVisualWork`). The contract for 关卡设计 is `README_V4_FloorHasStation.md` in this folder.

**State: READY.** The package `W/apply/v4_desk_still.sh` dry-runs OK on main db790e30 + working tree (2026-10-22 20:1x; rebuilt and re-run by the verify stage 20:2x–20:3x after one fix, §6). It has no HOLD file: its only condition is its own verification. It stays inert until M4 adds `FrontRoomsMapWorld.FloorHasStation`.

## 1. What it does

- On a floor **with a station**, every Office desk CRT in the maze shows one still frame of 平面视觉 · 广告's v2 art (`Tools/screens/out/still_desk/`, README v2) in place of Ad_03.
  - The station TV stays the only moving screen on that floor.
  - Once the key is taken, V1 turns the TV off for good; the desks stay still.
- On a floor **without a station** nothing changes: the desks play Ad_03 as today.
- **Today no floor has a station.** M4 and V1 are not in main, and `FrontRoomsMapWorld.FloorHasStation` does not exist yet. So the package is inert, and the game shows the same desk screens as main, pixel for pixel (§3 A, VL392).
- The title corridor TV (the title stream rooms) is never affected. The same goes for any screen attached with plain `Attach`: the station TV (V1), and the ScreenAds demo rig.

The README v2 rules, and where each one is kept:

| Rule | How |
|---|---|
| One still texture per monitor; never switch while visible | `FrontRoomsScreenVideo.UpdateDeskStill`. It switches only when the screen is out of the game camera's frustum, or when its lit surface was not drawn last frame. A desk built on a station floor starts still, before its first frame. |
| Shown 1:1, no emission boost | The frame's own texels sit on the existing ad surface material: URP Unlit, white `_BaseColor`, no emission. At 4:3 on the 1.47:1 face the frame is centred, and the spare width clamps to the art's own border colour 9/10/11, so it cannot be seen. |
| Neutral white | Nothing tints it. The texture is sRGB, uncompressed and mip-mapped. |
| Seeded per desk | `FrontRoomsDeskStill.Pick(map seed, floor, the desk's place in the map)`. The shares are cursor 55 %, cursor_low 17 %, blank 13 % and off (dark glass) 15 %. A rebuilt desk shows the same frame. |
| No tube effects | None are added. |
| The station TV is the only moving screen | A still desk never asks the shared decoder to play. When every desk is still, the bank decodes for the station TV only. |

## 2. What was built (visual-owned paths only)

| Path | sha1 | What |
|---|---|---|
| `Assets/Scripts/Rendering/FrontRoomsStationPresence.cs` (new) | 4c0563dc (937815bd before the verify fix, §6) | The reader: `public static bool FloorHasStation { get; }`, namespace-free |
| `Assets/Scripts/Rendering/FrontRoomsStationPresence.cs.meta` (new) | 5c4ab254 | |
| `Assets/Scripts/Rendering/FrontRoomsDeskStill.cs` (new) | fbea83aa | The art, the seeded pick, the 4:3 fit, the @2x rule, the frustum test |
| `Assets/Scripts/Rendering/FrontRoomsDeskStill.cs.meta` (new) | 30e8713d | |
| `Assets/Editor/Rendering/FrontRoomsDeskStillTests.cs` (new) | 3798c473 (a107b33f before §6) | Edit-mode checks S1–S4 (18, with S1f) |
| `Assets/Editor/Rendering/FrontRoomsDeskStillTests.cs.meta` (new) | f7dfe4cb | |
| `Assets/Resources/Rendering/StillDesk.meta` (new folder) | a4d3368f | |
| `Assets/Resources/Rendering/StillDesk/desk_cursor.png` (+ meta) | 941119ff (meta 50aeba4e) | 640 × 480, = the art |
| `Assets/Resources/Rendering/StillDesk/desk_cursor@2x.png` (+ meta) | 69cbfc75 (meta 69b5c649) | 1280 × 960, = the art |
| `Assets/Resources/Rendering/StillDesk/desk_cursor_low.png` (+ meta) | 85a083dc (meta b6c4c491) | 640 × 480, = the art |
| `Assets/Resources/Rendering/StillDesk/desk_cursor_low@2x.png` (+ meta) | c77f207e (meta ca332c56) | 1280 × 960, = the art |
| `Assets/Resources/Rendering/StillDesk/desk_blank.png` (+ meta) | aa6928b1 (meta b850daff) | 640 × 480, = the art |
| `Assets/Resources/Rendering/StillDesk/desk_blank@2x.png` (+ meta) | 1612c06b (meta b782a519) | 1280 × 960, = the art |
| `Assets/Scripts/Media/FrontRoomsScreenVideo.cs` (replaced, meta kept) | c0138606 → c2f1dbd4 | `AttachDesk` + the still mode; the ad mode is unchanged |
| `Assets/Scripts/Office/FrontRoomsOfficeKit.cs` (replaced, meta kept) | cfe0105a → 0bbd50fe | Desk CRTs attach with `AttachDesk` (one line + comment) |

- **Ownership.** `Assets/Scripts/Media/FrontRoomsScreenVideo.cs` is 游戏视觉's: the Codex-audit CRT fixes and C26 landed there through visual packages (MERGED.md 10-14 16:2x, 17:58; 10-22 07:53, LANDINGS line 77). `Assets/Scripts/Office/FrontRoomsOfficeKit.cs` is ours too (Office scripts; outlets_int landed it on 10-21). Everything else is new, under Rendering or Resources/Rendering.
- **No map-owned file is in the package.** `FrontRoomsMap/*`, `FrontRooms3DGame*.cs` and the scene are untouched. The one map change is M4's member, described in the contract README; 关卡设计 needs no diff from us.
- **The reader** (`FrontRoomsStationPresence`, namespace-free) offers `public static bool FloorHasStation { get; }`.
  - It reads `FrontRoomsMapWorld.FloorHasStation` by reflection (a cached `PropertyInfo`, else a `FieldInfo`). While the member is absent, it reads false.
  - It refreshes on `FrontRooms3DGame.MapRunStarted`, and on `FrontRooms3DGame.FloorChanged(map, relay, floor)` (C22, in main since 15:39). Both are subscribed directly since the verify stage (§6); the package's dry run refuses if either is missing.
    - Before §6 it bound FloorChanged by reflection. No other code adds a FloorChanged handler, so the WebGL player's High stripping would have dropped the add accessor and the reader would have stayed on floor 1's answer there.
  - It goes back to false on `MapRunEnded`.
  - It has an editor-only `SetForTest(bool?)` for tests and captures.
  - It has no Set call and no event for the map.
- **The art** is in `Assets/Resources/Rendering/StillDesk/`: the six v2 frames, byte for byte (sha1 checked against `Tools/screens/out/still_desk/` by the package builder). `still_desk_sheet.png` is not imported (review sheet only).
  - Import settings: uncompressed, sRGB, mips, trilinear, clamp, no NPOT scaling.
  - On iOS, Android and WebGL, the @2x files and the flat `desk_blank` import at 32 px, so they cost those builds nothing (the 1x blank is one flat colour, 9/10/11, measured: 1 colour in 307,200 px).
  - Frames load from Resources on first use, so nothing is loaded on a floor without a station.
- **@2x rule.** It is used only where 1x would be magnified on screen: the art's 480 rows drawn taller than 480 px at the closest view (0.6 m, 76°). That needs a window over about 2,370 px tall.
  - Phones (`FrontRoomsHandheld.Active`) and the WebGL player (`#if UNITY_WEBGL && !UNITY_EDITOR`) always take 1x. These are separate tracks: memory first.
- **Platforms.** The still-desk rule is the tutorial's (T3), so once M4 lands it holds on every platform through the same desk path. Only the texture budget differs per platform (above). Today nothing changes anywhere.

## 3. Checks

Renders and play-mode runs: `W/proj_v4desk` = main aca6240b + working tree (Game fc76109d, MapWorld 83c19911), 2026-10-22 17:57–18:14, batch mode, Metal, desktop High, run seed `Random.InitState(4242)` (map seed 516574485). Output: `W/proj_v4desk/Verification/v4desk/{ctrl, patch, stub}`; logs `W/logs/v4desk/v4_ctrl3, v4_patch5, v4_stub7`. Harness `Assets/Editor/Audit/V4/FrontRoomsV4DeskPlaytest.cs` (3e74d732, clone only). The offline checks, the compile and the package ran 20:0x–20:1x on main db790e30 + tree.

All timings are UNDER LOAD (1-minute load 32.5–274 during the Unity runs, 75–150 during this stage). The renders are deterministic captures, not timing data.

### A. No station (today's main) = main: VL392

| # | Check | Numbers | Result |
|---|---|---|---|
| A1 | The reader on today's main | binding absent → false; FloorChanged subscribed; edit-mode S1a–S1e on main (member absent) | PASS |
| A2 | Desk state, control (main's code) vs package | 4 / 4 desks identical in power, lit, texture (the shared ad RenderTexture), `_BaseMap` scale/offset (1, 1)(0, 0), `_BaseColor` white | PASS |
| A3 | The screen's own pixels (witness camera square-on, 800 × 600, 480 rows = 1:1, no post, ad held at frame 90) | 4 / 4 desks: 0 of 330,400 face px differ (crop 700 × 472); bank RenderTexture hash 59780360bba86450 in both | PASS |
| A4 | Around the face (information) | witness frames outside the face differ by ≤ 3 levels (0.00–23.6 % of px): the lamp-lit bezel under the lamps' flicker between the two runs. Game frame (post on): on the screen ≤ 4 levels, mean 0.92, as the film grain everywhere else (mean 1.2, p99 4); the one large difference (max 173) is a far lamp at x 708–945, y 94–243 | not a package effect |

- **A3 method.** The control run's own `cropHash` column was taken before run 3 fixed the witness crop (it had a shifted rectangle). The hashes were recomputed offline from the saved PNGs with the harness's FNV-1a over the fixed crop. That Python hash reproduces the package run's harness hashes exactly (4 / 4), and the control and package hashes are then equal (4 / 4).

### B. Station floor, by the test call `SetForTest(true)`: VL393

Package run: **21 / 21 PASS**.

| # | Check | Numbers | Result |
|---|---|---|---|
| B1 | No switch while visible | the watched desk (in view, lit) kept the ad for 60 rendered frames after the flag; it switched within 1 frame of leaving the view; 0 switches in 294 in-view render pairs | PASS |
| B2 | Every desk in the room still once out of view | 4 / 4 | PASS |
| B3 | Off sets = dark glass | surface not drawn, power off, on every Off set | PASS |
| B4 | 1:1, no boost, neutral | desk_cursor brightest 143/149/152, desk_cursor_low 145/151/155 (art cursor 150/156/160, max 154/160/164); field 8/9/10 (art 9/10/11); channel steps +6/+4 kept | PASS |
| B5 | The cursor where the art puts it, not mirrored | desk_cursor u 0.168, v 0.163 (art 0.172, 0.167); desk_cursor_low u 0.168, v 0.638 (art 0.172, 0.633) | PASS |
| B6 | desk_blank 1:1 | field 8/9/10, brightest 8/9/10 (art: one colour 9/10/11) | PASS |
| B7 | Seeded per desk | a rebuilt desk shows the same frame: 4 / 4 (chunk dropped at 5 chunks away and rebuilt) | PASS |
| B8 | The mix over 100+ real desks | 120 maze desks in 80 Office rooms: cursor 66 (55.0 %), cursor_low 25 (20.8 %), blank 14 (11.7 %), off 15 (12.5 %); targets 55 / 17 / 13 / 15 % | PASS |
| B9 | Built on a station floor = still on arrival | 0 not still; 0 ad renders of maze desks while the flag was on | PASS |
| B10 | Title TV and other screens untouched | a plain `Attach` screen under the map (the station-TV path) keeps the ad; a desk screen outside the map (title stream rooms) keeps the ad; RoomStream title screens still: 0 | PASS |
| B11 | Flag off again | the desk in view stays still while seen (30 frames); out of view every desk is back on the ad (13 / 13), scale/offset reset, the sets the mix had left off powered again (13 / 13) | PASS |
| B12 | 1:1 texel mapping at the game's screen sizes, @2x rule | closest real view 0.91 m at 76°: the face shows 145 rows at 1080p (3.32 art texels per pixel), 193 at 1440p (2.49), 290 at 2160p (1.66), so 1x is never magnified; at the 0.6 m design limit 219 / 438 / 584 rows at 1080p / 2160p / 2880p, so @2x is picked only on the 2880 px window. Forced iPhone and iPad profiles take 1x | PASS |
| B13 | Edit-mode tests (`FrontRoomsDeskStillTests`) | 17 / 17 on today's main (member absent); 17 / 17 with the stub (member present). S2b: 1,000 desks cursor 53.3 %, cursor_low 17.9 %, blank 13.5 %, off 15.3 %; S2c: 100 desks 56 / 20 / 9 / 15; S2d: another seed re-deals 112 / 200, another floor 125 / 200 | PASS |

### C. The real wiring, with a clone-only stub of the member

`W/v4desk/tools/stub.py` adds `FloorHasStation` to the clone's `FrontRoomsMapWorld` and sets it before `Begin`; it is never packaged, and the clone's Game and MapWorld were put back afterwards (fc76109d, 83c19911). Stub run: **21 / 21 PASS**.

| # | Check | Numbers | Result |
|---|---|---|---|
| C1 | The reader binds the member and the event | property bound; FloorChanged subscribed | PASS |
| C2 | Set before Begin: floor 1 | reader true at `MapRunStarted`; desks still from the start 4 / 4; 0 maze desks ever drawn with the ad | PASS |
| C3 | `FloorChanged(2)` without a station, then `FloorChanged(3)` with one | reader false, floor 2: the desk in view stays still until it leaves the view, then all 4 / 4 back on the ad; reader true, floor 3; the mix is dealt per floor (3 of 4 desks show another frame) | PASS |
| C4 | The game's own `ChangeFloor` | floor 1 → 2 (map seed 516574485 → 1872904683), member false: reader false, 7 / 7 desks on the ad; floor 2 → 3 (→ −1310261751), member true: reader true, 9 / 9 desks still on arrival, 0 ad renders | PASS |
| C5 | No switch in view | 0 switches in 74 in-view render pairs | PASS |

### D. Compile, memory, package

| # | Check | Numbers | Result |
|---|---|---|---|
| D1 | Compile on main db790e30 + tree + the package (Roslyn, no Unity) | macOS `--all`: 7 assemblies, 0 errors; iOS defines: 0 errors; WebGL player defines (UNITY_WEBGL, no UNITY_EDITOR): 0 errors; iOS player defines: 0 errors. `W/logs/v4desk/roslyn_main_2010_{osx, ios, webgl, iosplayer}` | PASS |
| D2 | The APIs the code binds to, on main now | `MapRunStarted(map, relay)`, `MapRunEnded()`, `FloorChanged(map, relay, floor)` unchanged in Game a2e212ef; `FloorHasStation` absent in MapWorld f742f592 | PASS |
| D3 | Texture memory | editor measure per frame: 1x 640 × 480 RGB24 + 10 mips 2.74 MiB, @2x 1280 × 960 10.94 MiB; 1x set 8.21 MiB, @2x set 32.82 MiB, loaded only on a station floor. Phones and WebGL load only desk_cursor and desk_cursor_low at 1x (blank and every @2x are 32 px there): 5.48 MiB editor measure, about 3.1 MiB as GPU RGBA32 + mips on a device (estimate) | PASS |
| D4 | The package dry run (main db790e30 + tree, 20:1x) | APIs OK; 21 / 21 bases (19 new paths absent, ScreenVideo c0138606 and OfficeKit cfe0105a as recorded); 5 / 5 kept .meta unchanged; 21 / 21 payloads match; 10 new GUIDs unused in Assets | PASS |
| D5 | Rehearsal on a scratch copy of main's files (20:12) | `--apply`: MANIFEST OK, 21 paths; second run: ALREADY APPLIED; with a HOLD file, `--apply` REFUSED (the dry run still works) | PASS |
| D6 | LF only | 0 CR bytes in the payload's .cs/.meta, the package files and these docs | PASS |

## 4. Package

- **v2 package:** `W/apply/v4_desk_still.sh` (bc1966dd) + `_bases.txt` (15d3f498), `_keep.txt` (36f131a7), `_expected.txt` (f1b10647), `_payload/`, `_base/`, `.diff` (9048de63). Rebuilt by the verify stage (§6); the 20:11 set (a6b0e50c, 12c06c81, c5adda93, 2b782460, 31918c6a) is in `W/_partial/` (`*_202752`, script `v4_desk_still.sh_a6b0e50c`).
- **Paths:** 21 in total. 19 are new (3 scripts, 6 PNGs, 9 metas, the folder meta). 2 are replaced (`FrontRoomsScreenVideo.cs`, `FrontRoomsOfficeKit.cs`). 5 metas are kept.
- **Bases:** recorded 2026-10-22 20:11 and again 20:27 (same values) on main db790e30 + working tree: ScreenVideo c0138606, OfficeKit cfe0105a, every new path ABSENT. No open CLAIM in LANDINGS.md on any of them.
- **Dry run:** DRY RUN OK (20:1x; again after the rebuild, 20:2x).
- **No HOLD file.** The item has no condition beyond its verification: it stays inert until M4.
- **Apply (the 游戏视觉 session):** `bash /Users/redwang/FrontRoomsVisualWork/apply/v4_desk_still.sh` (dry run), then `bash /Users/redwang/FrontRoomsVisualWork/apply/v4_desk_still.sh --apply`. Then run Roslyn macOS + iOS on main, and add the LANDED line and the MERGED.md row. On a base mismatch, run `W/tools/rebase_apply.sh v4_desk_still` first.

## 5. Open items

1. **M4 (关卡设计)** adds `FrontRoomsMapWorld.FloorHasStation` as in `README_V4_FloorHasStation.md`. Until then the package changes nothing on screen.
2. **V1 (the station TV)** must attach with `FrontRoomsScreenVideo.Attach`, never `AttachDesk`. A CRT on the station's own desk that is dressed by the Office kit goes still like any desk on that floor (the intent).
3. **crt-tube-vl245** edits the same `FrontRoomsScreenVideo.cs`. It rebases after V4 lands (`rebase_apply.sh`).
4. **First-frame gap (for crt-tube-vl245).** In the stub run's real floor change to floor 2, the new desks were bound to the shared ad RenderTexture but it was still black in the capture. The decoder had not made a frame in that session (floor 1 was a station floor, so nothing had asked it to play), and the capture waited about 10 frames. This is VL245's known first-frame gap, not a change by this package. In the game the title corridor TV warms the same bank first. Recheck it there.
5. **Optional, desktop build size.** Resources always ship. The six frames add about 18 MB uncompressed (RGB24 + mips) to desktop builds, 14.7 MB of it the @2x set. `desk_blank` (1x and @2x) is one flat colour, so importing it at 32 px on desktop too would save about 6 MB with no visible change. Not done here: it needs a re-import check.
6. **VL392–VL393** wait for placement: `W/vl_pending/prebuild_v4desk.md`.

## 6. Verify stage (2026-10-22 20:2x–20:3x, account B)

The verify stage tried to refute this build. One defect was found and fixed; nothing else changed.

**Fixed: FloorChanged was bound by reflection only (WebGL).** No code in main adds a `FrontRooms3DGame.FloorChanged` handler (grep: only the declaration and the `Invoke` in `ChangeFloor`). The WebGL player strips at High (`managedStrippingLevel` WebGL 3, no link.xml), and High keeps only the event accessors some code calls. So in a WebGL player `GetEvent("FloorChanged")` would most likely find nothing, and the reader would have kept floor 1's answer (and floor number) on every later floor. This is an inference from the stripping rules, not a WebGL build. The fix: `Hook()` now subscribes directly (`FrontRooms3DGame.FloorChanged -= / += OnFloorChanged`), as it already did for `MapRunStarted` and `MapRunEnded`. C22 is in main, so this compiles on today's main; the apply script now refuses if the event is missing.

| # | Check | Numbers | Result |
|---|---|---|---|
| V1 | Dry run on main now (db790e30 + tree: Game 938b0ee9, Mobile 4a7ccc9d) | before and after the rebuild: APIs OK, 21 / 21 bases, 5 / 5 kept, 21 / 21 payloads, 10 new GUIDs unused | PASS |
| V2 | Compile, main now + package (Roslyn) | the 20:11 payload and the fixed one: macOS `--all` 7 assemblies, iOS, WebGL and iOS player defines, 0 errors each (`W/logs/v4desk/roslyn_verify_2030`, `roslyn_verify_2040`) | PASS |
| V3 | Edit-mode tests in the clone, fixed reader (Unity 20:27, 11 s, load 75, UNDER LOAD) | 18 / 18; new S1f: raising FloorChanged through its delegate moves the reader to floor 3; re-hooking keeps 1 listener and resets to floor 1 (`W/proj_v4desk/Verification/desk_still/tests_verify2040`, log `W/logs/v4desk/verify2040_tests.log`) | PASS |
| V4 | No station = main, rechecked offline from the saved PNGs | 4 / 4 faces: 0 differing px over 331,800 (700 × 474) and a 706 × 480 crop; the faces carry the ad (about 23,900 colours each); bank RT identical. Game frame: screen ≤ 4 levels; the max 173 is a fluorescent lamp's flicker phase plus the far doorway's light (x 708–1019, y 94–243), captured 0.18 s apart | PASS |
| V5 | Art | the 6 payload PNGs = `Tools/screens/out/still_desk/` byte for byte; `desk_blank` 1x and @2x are one colour (9, 10, 11), so 32 px on phones / WebGL is the same picture | PASS |
| V6 | Rehearsal of the rebuilt package on a scratch copy (`W/v4desk/rehearsal3`) | `--apply`: MANIFEST OK; second run ALREADY APPLIED; HOLD file: REFUSED | PASS |
| V7 | Title stream desks | the title corridor is a root object ("Title sequence / recycled corridor"), the map is "Level 0 map" under the game: a title desk never finds a FrontRoomsMapWorld parent, so it never goes still (code read; the stub run had 0 title screens left to sample) | PASS |

Notes from the verify stage (not blocking):
- **RT glass reflections (Mac, G14).** `FrontRoomsGlassRTSystem` describes a screen's material once (live kind = visibility only). A desk built still is described with its still frame. A desk that switched in place keeps its old texture in RT reflections until the material is re-described. Under the contract no desk switches in place in play (the answer is set before Begin; NextFloor drops every chunk), so this only matters if the answer ever changes mid-floor; then call `FrontRoomsGlassRTSystem.RefreshMaterial(surfaceMaterial)` on enter/exit.
- **ExitStill with no ad RenderTexture yet** keeps the still texture bound (BindTexture ignores null). Same reach as above: only a mid-floor change back to false.
- The VL393 row in `W/vl_pending/prebuild_v4desk.md` says edit-mode 17 / 17; with S1f it is 18 / 18 (noted there).

Images: `images/VL392_desks_no_station_equal_main.jpg`, `images/VL393_desks_station_still.jpg` (Shot format, made by `W/v4desk/tools/make_images.py` with `W/tools/shotframe/shotframe.py`).
