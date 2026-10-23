# CueInk prebuild: the wall cue's visual half (A + M4 + V1), inert until the map writes cells

**Status: READY (2026-10-22 20:17).** The package `W/apply/cueink.sh` dry-runs OK on main HEAD db790e30 plus its working tree (Game a2e212ef, MapWorld f742f592). It has no landing condition and no HOLD file. 总控's condition was "lands at once when proven inert", and the proof below passes 63 of 63 checks. The 游戏视觉 session lands it with:

```
bash /Users/redwang/FrontRoomsVisualWork/apply/cueink.sh --apply
```

**Written by:** 游戏视觉, workflow prebuild-integrations (wf_d1d3c10c-3b4), CueInk build stage. Run 3 (account B, 16:47–18:13) built and packaged it, then died during verification (API error before its fourth play pass). Run 4 (account B, 20:03–20:2x) re-synced the clone to main, re-verified it, rebuilt the package and wrote this file.

**Decisions it implements:**
- Red 15:02 (总控 chat): integrate every finished asset now.
- Red 15:04–15:05 (动画和动态图形模型技术 chat, checked by 总控 in transcript 6e9bdaff): the wall cue is **M4 + V1**. With 信息设计's **A**, the set is A + M4 + V1 (C31).
- The spec is `CUE_DRIVER_INTERFACE.md` v0.2.4 (this folder): §1 for the shader and the data, §5 item 14 (state 0 = the live print exactly), §5 item 11 (WebGL: see §6 below).
- The map side is `MAP_ROUTE_SPEC.md` and is not ours. Its contract with this half is `CUEINK_CONTRACT_README.md` plus `CUEINK_CONTRACT_map.diff` (this folder).

## 1. What lands (6 paths, all visual-owned)

| Path | Base on main | After `--apply` | What |
|---|---|---|---|
| `Assets/Resources/Rendering/FrontRoomsSurface.shader` | a82fe187 | **51e43465** | `CueInk` in the print layer, after the ambient flipbook. It reads `_FR_WayCells` with LOAD, using **toroidal addressing**. It shows only where `_FR_WayOrigin.w = 1` **and** `_FR_CueReady = 1`. GLES3 and WebGPU compile it out. No new keyword, no new sampler (it shares `sampler_FR_Print`), and no Properties change. |
| `Assets/Editor/Rendering/FrontRoomsPrintArray.cs` | 6101c4bd | **fda2ac9e** | `BuildCue` (menu FrontRooms > Rendering > Build Cue Array; batch `FrontRoomsPrintArray.BuildCueBatch`). It first checks that C00 = K00 texel for texel, then writes C01..C16 as a 4 × 4 sheet. The `FR_Cue_*` sheets get the print's import rules and print mips. The WebGL, iPhone and Android tabs are 32 px stubs. |
| `Assets/Scripts/Rendering/FrontRoomsCueInk.cs` (+ .meta) | new | **9a6b6818** (+ ba7ce789) | Static binder, not a MonoBehaviour. It owns `_FR_Cue` and `_FR_CueReady`. `Bind()` loads the array in the background; `Unbind()` and `Release()` undo it. `Supported` is false on WebGL, on phones and without the print driver. It also has `Encode` and `TexelIndex` for the writer. Nothing calls it until the map's driver does. |
| `Assets/Resources/Print/FR_Cue_HardEdge.png` (+ .meta) | new | **9ab107f5** (+ d6d3552e) | 8192² sheet, 1,585,101 bytes. It imports as a Texture2DArray of 2048² × 16, RG_BC5_UNorm, 12 mips, trilinear, aniso 16, repeat. That is the print's sampler state and format, so one sampler serves both. |

**Kept, never written** (the script refuses if any changed): `FrontRoomsPrintArray.cs.meta` 2258cf27, `FrontRoomsSurface.shader.meta` 90befc31, `Resources/Print.meta` 4a96d657, `Scripts/Rendering.meta` 28ed1db6, `FR_Print_HardEdge.png.meta` ce025607.

**Untouched:**
- the print sheet (f2ca17d5) and the print driver (FrontRoomsPrintDriver f1df9051);
- every map file, `FrontRooms3DGame*.cs` and the slices in `Tools/print/ink/art_from_graphic/cue_states/flipbook/encoded/` (平面视觉's; read only).

### How it differs from the prototype (`W/proj_pgcue2`, function `CueInk`)

- **Toroidal addressing** (CUE §1), replacing the prototype's window around the player:
  - `texel = (int2)floor((positionWS.xz + n.xz × 0.5 − _FR_WayOrigin.xy) / _FR_WayOrigin.z) & 63`;
  - the origin is the map root (cell (0, 0)'s corner), so 64 × 3 m = 192 m = `ModuleUnits.WorldPeriod`;
  - negative cells wrap correctly (proved in §3, rows T1–T3).
- **Preview-only globals left out:** `_FR_CueSetStride` and `_FR_CueRepeat` are gone (stride fixed at 8, repeat 1).
- **C00 is not in the array.** State 0 never samples it (§5 item 14), so the array holds C01..C16 as 16 slices instead of 17. Slice = state − 1 on +u faces and state + 7 on −u faces. A 17-slice array would need a 17 × 1 sheet, 34,816 px wide.
- **`_FR_CueReady` gate (new).** The map's cells alone do nothing until the visual half has bound the array. The two halves can therefore land in any order, with one compile dependency (§5).

## 2. How it was verified

- **Clone:** `W/proj_cueink` (mkclone; reused, never a `_2`).
  - Run 4 re-synced it at 20:07:58 from main db790e30 plus the working tree (Game a2e212ef, MapWorld f742f592), as mkclone does: rsync `--delete`, local patches, tools, test bundle id `com.redwang.frontroomsss.test.cueink`.
  - The clone-only files then came back from `W/cueink/clone_files`. After that, the clone differs from main only by the 6 payload paths, the harness and the installed tools.
- **Harness (clone only):** `Assets/Editor/CueInkCheck/FrontRoomsCueInkCheck.cs` (960f30fa) plus `FrontRoomsSurfaceBaseline.shader`. The baseline shader is main's a82fe187 byte for byte, except its name, `Hidden/FrontRooms/SurfaceBaselineA82`.
  - Every A/B swaps the shader on the same materials inside one editor tick: new ×2, main ×2, new again. It passes only if main = new on every pixel **and** both noise pairs and the round trip are 0 px.
  - Renders are 1920 × 1080, 4× MSAA, film grain seeded and dithering off, the same on both sides.
  - The clone-only test writer of `_FR_WayCells` is `FillCells` / `FillChecker`. It encodes with `FrontRoomsCueInk.Encode`, as the map driver does.
- **Runs:**
  - Edit batch, 16:58–16:59 (75 s; load 111–165, UNDER LOAD): `W/cueink/logs/edit.log`.
  - Play batch, run 4, 20:14:26–20:15:35 (69 s; load 252 → 186, UNDER LOAD): `W/cueink/logs/play.log`.
  - Reports: `W/proj_cueink/Verification/cueink/report_edit.txt`, `report_play.txt`, `frames.tsv`, `strip.txt`, plus the PNGs.
  - **Edit-batch reuse:** the edit batch was not re-run in run 4. Its inputs are unchanged by sha1: the 6 payload files, the slices, and main's shader a82fe187 / PrintArray 6101c4bd. Main's later changes (Game, MapWorld, audio, the Bayon .meta) touch neither shader compilation nor the texture import.
- **Timings:** every timing below was taken at a 1-minute load over 32, so all are **UNDER LOAD**. They show that things happen, not how fast.

## 3. Checks (run 4: 63 PASS, 0 FAIL; A/B attempts 34 for 34 A/Bs, so no attempt was noisy; stable-render retries 0)

### Inertness (总控's landing condition)

| # | Check | Result |
|---|---|---|
| I1 | At the title, before anything: the array is not loaded, `_FR_WayOrigin` and `_FR_Cue` are unset, `_FR_CueReady` is 0, the binder is not bound | **PASS** (5 checks) |
| I2 | Phones (`FrontRoomsHandheld.Force`): `Supported` is false, `Bind` refuses, nothing loads | **PASS** |
| I3 | **A/B main vs new, 34 renders, every one 0 px different (max 0)** in 3 poses: the title (the Level 0 Lobby replica), Level 0 (the lobby corridor at cell (158, 8), run seed 516574485) and the Office (cell (167, 15), Office/Standard). Per pose: | **PASS 34/34** |
| | • c1: default (the print driver live) | 0 px ×3 |
| | • c2: the print driver live and mid-blend (frames 3, 2.5, 2) | 0 px ×3 |
| | • c3: Reduce Motion (the driver held at K00, clock w 0) | 0 px ×3 |
| | • c4: no print driver (disabled; clock unset) | 0 px ×3 |
| | • c6: the map's cells written (all state 5, and a 5 → 3 fade at k 0.5) with w = 1 but no Bind: the "map lands first" case | 0 px ×6 |
| | • c5: cue bound and ON, every cell at state 0 with message FLOW, each of the 4 dirs: the "both landed, nothing turned" case (§5 item 14) | 0 px ×12 |
| | • c7: bound with w = 0 | 0 px ×3 |
| | • c8: after Unbind and Release (Level 0) | 0 px ×1 |
| I4 | Bind / Release bookkeeping in each pose: `Ready` with origin set; Release puts `_FR_CueReady` back to 0 | **PASS** ×6 |
| I5 | No new shader errors. `FrontRooms/Surface`: hasError False, 0 messages; ForwardLit fragments compile with 0 errors and 0 warnings on Metal macOS, Metal iOS and D3D11 Win64 (5 keyword sets each). The play log has no shader errors and 0 `error CS` | **PASS** |
| I6 | Variant count unchanged: 2,949,139 total / 23,047 used by the scene, the same for the new shader and main's | **PASS** |
| I7 | Compile: Roslyn `--all` on main + payload, macOS and iOS defines, 0 errors (Assembly-CSharp 106 files, Editor 62). The clone with the harness: 106 / 80, 0 errors | **PASS** |

**Cost while inert:**
- **Memory:** 0. The array is a Resources asset that only `Bind` loads (I1). `_FR_CueReady` is set to 0 once per Play, which is not a load.
- **Fragments:** the `_FR_PRINT` variants bind 2 more textures (`_FR_Cue` and `_FR_WayCells`; slots 7 → 9 and 12 → 14 on Metal and D3D11) and grow by 1.4–3.3 KB of code. Per wallpaper fragment the inert path is two uniform tests (`_FR_WayOrigin.w`, `_FR_CueReady`) and an early return.
- **Build data:** the desktop build carries the 2048² × 16 BC5 array, about 85 MB before compression. WebGL, iOS and Android carry 32 px stubs of about 44 KB.

### The cue itself (clone-only writer of `_FR_WayCells`; exact checks at 1 sample per pixel, post-processing off, every image a stable render)

The exact checks start from a background `Bind`:
- the array was not loaded before;
- it was not ready inside the call;
- it was ready after 4 frames / 48 ms (UNDER LOAD, 240).

**Memory once bound:** 2048² × 16 RG_BC5_UNorm, 12 mips = **85.3 MiB**. Total allocated grew by 85.5 MiB. The editor's runtime-size figure is 170.7 MiB, but the editor counts textures twice.

| # | Check | Result |
|---|---|---|
| C1 | Re-baseline: the cue on, every cell at state 0, equals the cue off | **PASS** 0 px |
| C2 | Face sets: the +u and −u pixels are disjoint and both present (+u 142,334 px, −u 58,787 px, overlap 0) | **PASS** |
| C3 | +u is the right wall looking along the route (+u left/right 0/142,334; −u 58,787/0) | **PASS** |
| C4 | Slice mapping is exact. State s on +u reads slice s − 1, and on −u slice s + 7: 16 of 16 combos, 0 wrong px each. The test array is in the cue's format, with only slice e = K00 | **PASS** 16/16 |
| C5 | Cross-fade: k = 0 shows only the from-state (0 wrong px). k = 1 shows the to-state (0 px vs K00). k = 0.5 from state 0 lands in between (mean \|d\| 0.651 against 1.275 at full) | **PASS** ×3 |
| C6 | Message 0 keeps the plain print whatever the states say | **PASS** 0 px |
| C7 | Faces across a perpendicular route keep the plain print: 0 px changed on the +u and −u walls (the end walls change, 157,216 px) | **PASS** |
| T1 | Toroidal: moving the origin by whole 192 m periods (+960/−576 m, and +960/+960 m, which makes the cells negative) gives the same image | **PASS** 0 px |
| T2 | Toroidal: a checker moved by 2 cells (6 m) gives the same image | **PASS** 0 px |
| T3 | Toroidal: a checker moved by 1 cell is its complement: parity 178,832 px + shifted 2,103 px = union 180,935 = the all-cells cue; overlap 0, outside 0 | **PASS** |
| S0 | Strip at the game's 4× MSAA with post on: state 0 with the cue on is the plain print | **PASS** 0 px |
| M1 | **Timed M4 + V1 at 30 fps** (CUE §6 timing; 154 frames, 18 clicks): the walls change on every cross-fade frame and never while a state holds: unexpected changes 0, missed changes 0 | **PASS** |
| R1 | Unbind keeps the array loaded for the next run; Release unloads it and `_FR_CueReady` is 0 | **PASS** |

**Strip** (changed pixels vs the plain print, on the +u / −u faces, Level 0 corridor):

| State | Meaning | +u px | −u px |
|---|---|---|---|
| S0 | live print | 0 | 0 |
| S1 | turn 30° | 73,894 | 28,505 |
| S2 | turn 60° | 84,683 | 31,905 |
| S3 | turn 90° (flow 0) | 89,455 | 33,236 |
| S4 | flow 1 | 91,370 | 33,649 |
| S5 | flow 2 | 103,334 | 35,370 |
| S6 | flow 3 | 100,643 | 36,116 |
| S7 | flow 4 | 99,883 | 36,054 |
| S8 | flow 5 | 89,102 | 32,875 |

**Frames per state at 30 fps (`frames.tsv`):**
- **Stage-1 turn (M4).** Clicks at +0.20, +0.50 and +0.80 s after the warn. Each turn state lasts 9 frames: 2 cross-fade frames (k 121/255 and 243/255), then 7 held.
- **Flow (V1).** One click every 0.25 s, two loops of 6 (S3 → S4 … S8 → S3). Each flow state lasts 7–8 frames: 2 cross-fade, then 5 or 6 held.
- **Turn back.** Steps of 0.15 s.
- **Totals.** Settled frames: S0 22, S1 9, S2 10, S3 23, S4 10, S5 12, S6 10, S7 12, S8 10. Cross-fade frames: 36 (2 per click).

**Images (Shot format):**
- `images/vl390_cueink_inert.jpg` (VL390): main, new and diff for the title, Level 0 (default and print driver live) and the Office. 34/34 pixel-identical.
- `images/vl391_cueink_states.jpg` (VL391): S0–S8 on the Level 0 corridor, the +u / −u face masks and the toroidal checker.

The VL rows wait in `W/vl_pending/prebuild_cueink.md` for placement.

## 4. Earlier runs (why run 4 looks the way it does)

- **Runs 1 and 2 (account A).** Run 1 (wf_12c986d3-ddf) was stopped after 1–2 minutes. Run 2 (wf_f742b0e7-d77, 15:2x–16:17) made the clone (15:26), ported the prototype and wrote the harness. It died when account A hit its limit.
- **Run 3 (account B), first play pass, 17:00.**
  - All inertness A/Bs passed.
  - The face-set and cross-fade checks failed. The masks came from renders taken before the background load had finished (lamps and time had moved). Fix: re-baseline inside the tick.
- **Run 3, 17:50.** The exact checks still lost a few pixels to 4× MSAA edge pixels that two faces share. Fix: 1 sample per pixel for the exact block.
- **Run 3, 17:59.** 58 PASS, 5 FAIL, every failure a harness artifact:
  - one main-side render noise of 52 px, max 2 levels, in an A/B (main vs new was still 0 px);
  - bloom spilling end-wall changes onto the side walls (503 / 525 px);
  - 6 px in 3 slice combos;
  - a 154 px checker overlap;
  - 2 unexpected timed changes.
- **The harness fixes behind run 4:**
  - A/B retry on noise (up to 4 attempts, all logged);
  - stable renders (two consecutive renders must agree);
  - post-processing off for the exact block.

  Run 4 needed none of them in practice: 34 attempts for 34 A/Bs, 0 retries. The payload itself has not changed since 16:56.

## 5. The map side (contract)

`CUEINK_CONTRACT_README.md` and `CUEINK_CONTRACT_map.diff` (this folder) are for 关卡设计 and 系统设计. The diff adds two statements to 关卡设计's `FrontRoomsWayCueDriver.cs` (base `~/FrontRoomsMapWork/proj3`, e119c7c3, 17:21 → 2e2ffa28):
- `FrontRoomsCueInk.Bind();` in its `Bind`;
- `FrontRoomsCueInk.Unbind();` in its `Unbind`.

The diff passes `git apply --check`. Roslyn `--all` on proj3 plus the payload's binder gives 0 errors with macOS and iOS defines.

**The one ordering rule:** those two lines compile only once this package is in main. The visual half is inert, so it lands first.

## 6. Platforms (separate tracks)

- **macOS, Windows, Editor:** the full cue, once the map binds it.
- **WebGL: off, and nothing on WebGL changes.**
  - The shader compiles `CueInk` out on GLES3 and WebGPU. `Supported` is false. The import tab is a 32 px stub of about 44 KB, against about 43 MiB at 1024 RG16 in a heap that holds the whole data file.
  - The print driver's WebGL static gate (frame 0) is untouched.
  - **C08 / CUE §5 item 11 ("WebGL: turn only") is therefore not delivered by this package.** It is the WebGL line's call: lift the shader guard, raise the tab (or build a 6-slice turn-only array, 4–16 MiB) and drop the WebGL line in `Supported`. Turn-only timing is already the driver's (`TurnOnly`).
  - **Limit of the proof:** the editor's GLES3 compile returned 0 bytes for both the new and main shaders (no WebGL shader compiler in this editor). WebGL compilation is covered by the preprocessor guard, not by a WebGL compile or build.
- **iOS, Android: off.** `Supported` reads `FrontRoomsHandheld.Active`, and both tabs are 32 px stubs, so the inert cue adds neither memory nor app size there. The mobile line opts in by removing `CueStubTargets` and the handheld test (2048 = 85.3 MiB once bound, or 21.3 MiB at 1024).

## 7. Open items

1. **Landing order with hunter_b.** `W/apply/hunter_b.sh` also replaces `FrontRoomsSurface.shader` on base a82fe187, and it is not verified. Whichever lands second needs `W/tools/rebase_apply.sh <track>`.
2. **WebGL turn-only (C08)** waits on the WebGL line (§6).
3. **Map half (关卡设计):** land the WayCueDriver with the two calls after this package. Then check, on desktop, that `FrontRoomsCueInk.Ready` turns true a few frames after `MapRunStarted` (README §6).
4. **Desktop build size:** about +85 MB of array data. Worth a line to App Store · 构建 if Mac store size matters.
5. **Harness side effect, clone only:** the shader swap makes Unity re-save `Resources/Surfaces/*.mat` in the clone at exit (it adds default properties). It is never packaged. Run 4 started from main's bytes (resync at 20:07).
