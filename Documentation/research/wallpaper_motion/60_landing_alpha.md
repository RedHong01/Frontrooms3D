# 60 · Landing the live wallpaper print for the Alpha (array rebuild + print driver)

Status: **READY, not applied** (2026-10-22 09:4x). Package `W/apply/print_driver_alpha.sh` (apply convention v2), built by 游戏视觉's print-driver-alpha build stage. Unity has not been run. A separate smoke and verify run follows; land only after it passes.

Approval: Red, 2026-10-22 09:18, in the wallpaper chat (transcript 6e9bdaff, AskUserQuestion). To "墙纸环境动态（印花驱动）什么时候合进游戏？" Red picked "合进今天的 Alpha".

Not approved, and not in this package:
- the wall-cue motion (A+M4+V1);
- CueInk;
- the glow-ink importer (`Tools/print/unity_staging/Assets/Editor/Print/FrontRoomsPrintImporter.cs` stays staged).

## 1. What lands

The script writes 4 paths, in this order: data before code, and each `.meta` before its file.

| # | Path | Action | sha1 before | sha1 after |
|---|---|---|---|---|
| 1 | `Assets/Resources/Print/FR_Print_HardEdge.png.meta` | replace: only the Provenance record in `userData` | `d87f6714…` | `ce0256072b4e66f590e3dfcfecadbd8abde71398` |
| 2 | `Assets/Resources/Print/FR_Print_HardEdge.png` | replace: the 4 × 2 sheet from K00..K07 (10-22) | `6507b0c6…` | `f2ca17d5efdf50ed8367a5ba5666ad45a6b0aa38` |
| 3 | `Assets/Scripts/Rendering/FrontRoomsPrintDriver.cs.meta` | add: GUID `8c24b48e207244a190158379ea4279d3` | ABSENT | `aa00cc7425ab952da2287adbc0617fdfd8a7e6e8` |
| 4 | `Assets/Scripts/Rendering/FrontRoomsPrintDriver.cs` | add: the wallpaper line's staged driver, verbatim | ABSENT | `2859c5074c00d5f6a75886e4fb69f04d658e5eaa` |

Bases were recorded on main `d4d0e7a5` + working tree. The GUID `8c24b48e…` matches nothing in main's 1,291 `.meta` files or any other file under `Assets`, `ProjectSettings`, `Packages` or `W/apply`.

Kept, never written. The dry run checks each of these:

| Kept path | On change |
|---|---|
| `Wallpaper_Print_P.png` (`_PrintTex`, the static K00) and its `.meta` | refuse |
| `Assets/Resources/Print.meta`, `Assets/Scripts/Rendering.meta` | refuse |
| `FrontRoomsPrintArray.cs`, `FrontRoomsSurface.shader` | note |
| the 8 encoded slices, the staged driver | note |

The dry run also refuses if main loses any of these:
- `FrontRooms3DGame.MapRunStarted`, `MapRunEnded` or `Paused`;
- `FrontRoomsMapHunter.StateChanged`, `Caught` or `WarnStageChanged`;
- `HunterState.Chase` or `BreakDoor`;
- `FrontRoomsSettings.CameraMotionPercent`;
- the shader's `_FR_Print` / `_FR_PrintClock` reads;
- `FrontRoomsPrintArray`'s 4-column `Print/FR_Print_HardEdge` import.

It also refuses if a second `FrontRoomsPrintDriver` class appears.

Apply (游戏视觉 main session, after the smoke + verify run):

```
bash /Users/redwang/FrontRoomsVisualWork/apply/print_driver_alpha.sh            # dry run: DRY RUN OK
bash /Users/redwang/FrontRoomsVisualWork/apply/print_driver_alpha.sh --apply    # MANIFEST OK
```

## 2. A: the array (`FR_Print_HardEdge.png`)

**How it was built.** `W/print_driver_alpha/scripts/build_sheet.py` (numpy 2.0.2, Pillow 10.4.0) lays the slices out exactly as `FrontRoomsPrintArray.Build`:
- slice k sits at column k % 4, row k / 4 counted from the top, so the sheet is 8192 × 4096;
- each texel is (R, G, 0) of the slice;
- the PNG is 8-bit RGB (colour type 2, no alpha, not interlaced). Its only chunks are IHDR, IDAT and IEND: no gAMA, sRGB or iCCP, so there is no colour management.

The shipped sheet has the same format. The script also runs the C# loops literally on bottom-up rows: Build's `y0 = (rows-1-k/4)·2048` for writing and `SliceOf` for reading. Both match the top-down build. The result is 37/37 checks PASS (`W/print_driver_alpha/out/sheet_report.json`).

**Slice proof.** Each new tile is decoded from the written PNG and compared with its source slice and with the same tile of the shipped sheet.

| Tile | Source | Source sha1 | New tile vs source | New tile vs shipped tile |
|---|---|---|---|---|
| 0 | K00 | `c809120a` | identical (R, G; B 0) | identical |
| 1 | K01 | `8d585317` | identical | identical |
| 2 | K02 (plate shift) | `b414721c` | identical | differs: 1,192,268 texels (28.4 %) |
| 3 | K03 (missing slate plate) | `1f048331` | identical | differs: 1,226,516 texels (29.2 %) |
| 4 | K04 | `ccf3bc54` | identical | identical |
| 5 | K05 | `fae64414` | identical | identical |
| 6 | K06 | `2852a0a4` | identical | identical |
| 7 | K07 (block swap) | `61b2acd4` | identical | differs: 3,321,704 texels (79.2 %); new tile 7 == shipped tile 2 exactly |

Every source slice is 2048² RGBA8 with B = 0 and A = 255. So each tile also equals its source as full RGBA texels once the dropped alpha (always 255) is restored.

**_PrintTex stays.** `Wallpaper_Print_P.png` equals K00's R and G with B 0, and new tile 0 equals `_PrintTex` texel for texel. The Q1b slice-0 identity therefore holds, and `_PrintTex` is not rebuilt. If someone runs FrontRooms > Rendering > Build Print Array later, it finds the same texels and prints "kept" for both textures.

**Two independent builds agree.** A second builder (`W/print_alpha`, retired, see §6) produced the same sheet sha1 `f2ca17d5` and the same `.meta` `ce025607` from the same inputs. The wallpaper line's own pack (`Tools/print/patterns/out/hard_edge/FR_Print_HardEdge.png`, RGBA) has the same R and G in every tile.

**The .meta.** `W/print_driver_alpha/scripts/update_meta.py` replaces only the three slice sha1s in the Provenance record (`userData`):

| Slice | Old sha1 | New sha1 |
|---|---|---|
| K02 | `61b2acd4…` | `b414721c…` |
| K03 | `e2af19a6…` | `1f048331…` |
| K07 | `3bea4b9e…` | `61b2acd4…` |

Everything else is unchanged:
- only line 169 changes, and it keeps its length, so the YAML folding stays as it was;
- the GUID (`e1a07c79…`) is the same;
- every importer setting, and every line before `userData`, is byte-identical;
- the LUT sha1 is still `353e29eb`.

The record now matches the slices on disk, so the importer does not see stale sources.

**Why the rebuild matters beyond the art.** Q1b's reduced-tier check (`FrontRoomsPrintQ1bTest`, part 2) compares each sheet slice with `SlicePaths()`. Since the 09:05 slice rebuild, the shipped sheet fails that check on slices 2, 3 and 7. After this landing it matches again.

## 3. B: the driver (`FrontRoomsPrintDriver.cs`)

**Source.** The payload is the wallpaper line's staged file, verbatim: `Tools/print/unity_staging/Assets/Scripts/Rendering/FrontRoomsPrintDriver.cs`, sha1 `2859c507`, 2026-10-22 09:34. This is their fixed final version after the adversarial review wf_1691c1a5-666 (18 findings, 3 confirmed). It replaces `ef19ac88`; the diff is `W/wallpaper_check/driver_ef19ac88_to_new.diff`.

Three things come from the wallpaper line's file, not from 游戏视觉:
- the WebGL gate;
- Reduce Motion;
- the three review fixes.

We added nothing to the file. An earlier plan forced w = 0 on WebGL; it was dropped because the staged file already keeps WebGL static.

**What it does in the game:**
- **Boot.** `RuntimeInitializeOnLoadMethod(AfterSceneLoad)` creates one DontDestroyOnLoad "FrontRooms Print" object, only when `Resources.Load<Texture2DArray>("Print/FR_Print_HardEdge")` exists and no driver exists yet. It binds `_FR_Print` once.
- **WebGL player.** Under `UNITY_WEBGL && !UNITY_EDITOR`, `Boot()` returns at once. No driver exists, `_FR_PrintClock` stays unset (w = 0), and the shader's `UNITY_BRANCH` on `_FR_PrintClock.w > 0` skips both array fetches. So the WebGL wall is the static K00, as it is in main today. The editor's Play mode with the WebGL target still runs the driver.
- **Motion.**
  - Holds last 45–90 s, then a 1.2 s smoothstep blend moves to the next slice (adjacent slices only; 7 → 0 wraps).
  - Ambient jumps run only inside a map run, i.e. while it holds a Relay (fix 1). The title stream stays on K00.
  - The seeds are fixed at 1990, so the first jump comes at least 45 s into a run.
- **Cue rule holds.**
  - "warn" is held while `WarnStageChanged` reports stage ≥ 1.
  - "chase" is set on `HunterState.Chase` and cleared on any state other than `BreakDoor`. A door break keeps the previous hold, so a break during a Hunt no longer moves the walls (fix 3: no radar leak).
  - "reduce" is held while CAMERA MOTION is 0.
  - Any hold fades w to 0 over 1.2 s, then cuts the frame to 0 unseen. When the last hold clears, the wall waits a 3–6.5 s quiet slot and then fades back in.
- **Freeze.**
  - `Caught` sets `Frozen`. The next `MapRunStarted` clears it.
  - The pause card stops print time through `FrontRooms3DGame.Paused` and its own `paused` flag (fix 2).
  - Close-ups run at timeScale 0, so `Time.deltaTime` is 0.
  - `OnMapRunEnded` resets the frame to 0, the jumps and the beat, so every run starts on K00 (fix 1).
  - `Held => Gate().Held` no longer throws before the first Update.
- **Reduce Motion.** `ReduceMotion => FrontRoomsSettings.CameraMotionPercent == 0`, which is main's existing COMFORT · CAMERA MOTION row. On a handheld, the OS Reduce Motion switch starts that row at OFF on first launch. The `FrontRooms.ReduceWallMotion` PlayerPrefs key is gone.
- **Globals written.** The driver writes `_FR_Print` (Texture2DArray), `_FR_PrintClock` (x frame, y count, z roll phase 0, w live mix) and `_FR_PrintWarp`. `FrontRoomsSurface.shader` (`a82fe187`) reads `_FR_Print` and `_FR_PrintClock` in `PrintInk` under `_FR_PRINT`. That keyword is enabled on `L0_Wallpaper`, `L0_Wallpaper_Shift` and `Exit_Wallpaper`, and papered outlet plates use the wall's own material, so they move with it. `OnDisable` zeroes the clock and the warp.
- **No per-frame allocation.** The Update path is value types and static reads. `SetHold` reuses interned literals. Event delegates are allocated once per run.

**APIs it calls, checked in main's files (not the docs):**

| API | Declared at | Raised at |
|---|---|---|
| `FrontRooms3DGame.MapRunStarted` | `FrontRooms3DGame.cs:39` | :770 |
| `FrontRooms3DGame.MapRunEnded` | :41 | :2666, OnDestroy |
| `FrontRooms3DGame.Paused` | :45 | :2314; :2663 on teardown |
| `FrontRoomsMapHunter.StateChanged` | :210 | :1315 |
| `FrontRoomsMapHunter.Caught` | :212 | :354 |
| `FrontRoomsMapHunter.WarnStageChanged` | :215 | :1259 |
| `HunterState { …, Chase, BreakDoor, … }` | `FrontRoomsHunter.cs:9` | — |
| `FrontRoomsSettings.CameraMotionPercent` | `FrontRoomsSettings.cs:29`; loaded in `FrontRooms3DGame.Awake`, before the AfterSceneLoad boot | — |

## 4. Checks (all on Red's main as of 09:3x, without Unity)

| Check | Result |
|---|---|
| Roslyn `--all` macOS (`W/tools/roslyn_check.py --override …=payload`) | `COMPILE OK: FMODUnity 36 files, FMODUnityEditor 31 files, FMODUnityHaptics 1 files, FMODUnityResonance 2 files, FMODUnityResonanceEditor 2 files, Assembly-CSharp 102 files, Assembly-CSharp-Editor 62 files — 0 errors` (Assembly-CSharp 0 warnings) |
| Roslyn `--all --ios` | `COMPILE OK (iOS defines): … Assembly-CSharp 102 files, Assembly-CSharp-Editor 62 files — 0 errors` |
| WebGL player defines (Assembly-CSharp without `UNITY_EDITOR*` / standalone defines, with `UNITY_WEBGL`, `PLATFORM_WEBGL`) | rc 0, 0 errors, no diagnostic in the driver |
| `GateTest` (engine-free, on the payload build) | ALL PASS (262 checks); fade to K00 1.22 s, quiet slot 4.25 s, fade back 1.22 s |
| `MotionTest` | ALL PASS; 16 jumps in 60 s at 2–3 s holds, max step 0.0208/frame, 7 → 0 wrap clean, crawl 0.072 m/s |
| Sheet build + proofs | 37/37 PASS |
| Dry run against main | `DRY RUN OK: every base matches; run with --apply to perform the actions above.` |
| `W/tools/rebase_apply.sh print_driver_alpha --no-install` | `PASS print_driver_alpha: nothing to rebase (every touched base matches); dry run OK.` |
| Scratch rehearsal (`W/tools/_test/print_driver_alpha_1022/run_tests.sh`) | 36 PASS, 0 FAIL |

What the rehearsal covered:
- DRY RUN OK, then MANIFEST OK, then ALREADY APPLIED for both the dry run and `--apply`; the landed driver is byte-identical to `2859c507`; exactly 4 files were written; both backups equal main's files.
- Each of these refuses: half-applied, moved base, path taken, kept `.meta` changed, `_PrintTex` changed, `CameraMotionPercent` / `WarnStageChanged` / `Paused` / the shader clock missing, a duplicate driver class, a tampered payload. Each refusal wrote nothing.
- A moved slice is a NOTE only.
- No manifest landed in `W/apply`.

Timings were measured UNDER LOAD (load average 112–238):

| Step | Time |
|---|---|
| Sheet build + proofs | 6 s |
| Roslyn per platform | 7–8 s |
| Rehearsal | 12 s |

## 5. Open points

1. **`_FR_PrintWarp` is ignored until P1.** The driver writes the subliminal drift (4 mm at 0.15 rad/s) and the Crawl beat, but `PrintUV` returns `rollUV` unchanged. Only the slice jumps are visible.
2. **WebGL is static by the wallpaper line's gate**, and WebGL is its own track. The 8-slice array is still in the WebGL build's Resources, as it has been since the 10-07 Q1b merge: 1024² RG16 per slice. Nothing loads it there.
3. **Reduce Motion has no row of its own.** It follows CAMERA MOTION OFF. The settings menu was not changed.
4. **Not run in Unity yet.** The smoke + verify run must cover:
   - the sheet reimport (the Texture2DArray is 2048 × 2048 × 8 on desktop, and `ApplySheetMips` writes no error);
   - the Q1b batch;
   - a map run that shows a jump after 45–90 s and a hold to K00 at warning stage ≥ 1;
   - WebGL staying static (if a WebGL build is made).
5. **iOS.**
   - The driver runs on iOS players as designed.
   - Loading the array adds about 42.7 MiB of texture memory (the wallpaper line's figure). It was never loaded before.
   - `_PrintTex` and the array both use the default iOS tab, but slice-0 parity on iOS has not been measured.
6. **Play-mode captures and tests now see a live print inside map runs.** The jumps are deterministic (seed 1990), and none comes earlier than 45 s into a run.
7. **A run that starts without a teardown resets the frame to 0 at `OnMapRunStarted`.** If the wall was mid-cycle and visible, this is a visible cut. In main, a new run follows a scene teardown (`OnDestroy` raises `MapRunEnded`), so the risk is low.

## 6. Process notes

- The first plan added a w = 0 WebGL gate on top of the staged `ef19ac88`. A second builder in a workflow 游戏视觉 has since stopped (work dir `W/print_alpha`, driver `f109ac1e`, GUID `95e87b82…`) wrote that version into `W/apply/print_driver_alpha*` at 09:29–09:31.
  - Its files were moved, not deleted, to `W/apply/_retired/print_driver_alpha_oldgate_20261022/`.
  - Its rehearsal folder is `W/tools/_test/print_driver_alpha_1022_oldgate/`.
- The intermediate payload with `ef19ac88` verbatim is kept in `W/print_driver_alpha/superseded_ef19ac88/`.
- Build files:
  - scripts, outputs and logs in `W/print_driver_alpha/{scripts,out,logs}`;
  - the staged package copy in `W/print_driver_alpha/pkg/`;
  - Roslyn work dirs in `W/print_driver_alpha/roslyn_{osx,ios,webgl_player}`.
