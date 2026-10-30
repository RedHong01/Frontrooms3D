# 61 · V5 picture sync: the shared ad player follows the broadcast (build, 2026-10-29)

**Status: READY, not applied.**
- **Package:** `W/apply/v5_picture_sync.sh`. Its dry run against main gives DRY RUN OK (17:4x).
- **Roslyn** on main + the package: COMPILE OK on macOS and on iOS.
- **Proofs** pass in a clone that equals main + the package:
  - the V5 harness, 41/41 in each of three passes;
  - R3 71/71 desktop and 63/63 phone;
  - EXAMINE 134/134;
  - C26 44/44.

The session lands it. LANDINGS.md and MERGED.md were not touched. (`W = /Users/redwang/FrontRoomsVisualWork`.)

**What was agreed.** 游戏视觉 and 声音设计 agreed the following on 2026-10-22 at 15:2x. It is recorded in `Takeover_2026-10-18/handoff/visual.md` ("V5 sound" and "声音设计 fixed the V5 names") and in `research/screens/NOTE_1022_v5_sound_READ_FIRST.md`:
- **Audio is the clock.** On the visibility edge, a sounding screen seeks the shared AdBank player to `FrontRooms.Audio.FrontRoomsScreenSound.LoopTime`, when that is ≥ 0, read by reflection.
- **Power.** When a screen is powered off or killed (`IsPoweredOn` false), its loop stops; the sound side reads that.
- **Already in main.** 声音设计's side landed at 16:48 as code 2026-10-22.9, on top of 游戏视觉's R3 package (ScreenVideo `4eb52b27`, enum `FrontRoomsScreenSoundRole`, `SoundRole` set through Attach's third argument).
- **Missing until now:** the seek itself.

## 1. What was built (1 file, 游戏视觉's; its `.meta` kept)

| File | Change |
|---|---|
| `Assets/Scripts/Media/FrontRoomsScreenVideo.cs` | Base `4eb52b27` (main since 16:41), payload **`d0705a03`**. Four hunks, 91 diff lines (`W/apply/v5_picture_sync.diff`) |

No sound, map, KeyHud, input or scene file was touched. 声音设计's `FrontRoomsScreenSound.cs` is only read: its `LoopTime` is read by reflection at run time, and its declaration is checked by the package's dry run.

**The hunks** (small and self-contained, so station-look-v1's `PowerOffForGood` rebases with a plain 3-way merge):
1. `FrontRoomsScreenVideo.Update`, on the existing visibility edge: `if (visible && SoundRole != None) bank.SyncToSound(this);`. This is the only change in the screen class.
2. `FrontRoomsScreenAdBank.OnPrepared`: `ServeSoundSeek();` before the first `Play`.
3. `FrontRoomsScreenAdBank.Update`, last line: `ServeSoundSeek();`. It serves a request that is still waiting. It returns at once when none is open.
4. A V5 block at the end of the bank class: `SoundLoopTime`, `SyncToSound` and `ServeSoundSeek`.

### 1.1 Behaviour

**The request.** A sounding screen's visibility edge records one request on its bank. It is served once, when all of these hold:
- the player is prepared;
- the world is not held (`WorldHeld`, C26);
- the broadcast is running (LoopTime > 0, see 1.2).

**The seek.** `player.time` = (LoopTime + one game frame) modulo the clip length. The shorter way round the loop is used to compare. A picture already within one clip frame of the target is not sought.

**Dropped requests.**
- A request is dropped when its screen leaves view.
- The close-up's bank never takes a request and never serves one (R3 `IsCloseUpBank`). The inspected TV plays on from where it is.

**Unchanged.** Nothing else changes:
- a None screen never asks;
- nothing seeks every frame;
- `IsPoweredOn`, `SoundRole`, the enum, `Attach`, and the R3 rule, exemption and clock are as before.

### 1.2 Three details of reading the clock (measured, not new behaviour)

The agreement's words are "seek to LoopTime when it is ≥ 0, on the edge". Taken literally, the screen and its sound fall out of step in the cases the feature exists for. The first build round showed that (§2.4).

1. **The request waits for the broadcast.**
   - The director attaches `FrontRoomsScreenSound` on a pass it makes once a second, and a power-on starts the loop again. So at the title TV's first view and at every power-on, the picture's edge comes before the loop.
   - Seeking only in the edge frame would leave the picture out of step for as long as it stays in view.
   - So the edge's one request is kept, one bool, until the loop runs. It is still one seek per edge.
2. **"Running" means LoopTime > 0, not ≥ 0.**
   - From `start()` until the loop really plays, FMOD reports 0.000. That was 12 frames, about 0.2 s, in the clone.
   - Seeking to that 0 left the picture 0.11–0.14 s ahead: in the first round, A1, B2 and C1 failed.
   - −1 still means "no broadcast". An exact 0 at a loop wrap only waits one frame.
3. **The seek aims one game frame ahead** (`Time.unscaledDeltaTime`, capped at two clip frames).
   - The decoder shows a seek from the next frame on.
   - Without the lead, the picture sat 1.1–1.4 frames behind (A3: median 0.041 s; C2: 0.037 s). With it, the medians are 0.008–0.030 s.

**Reflection, as briefed.**
- The type is `typeof(FrontRoomsScreenAdBank).Assembly.GetType("FrontRooms.Audio.FrontRoomsScreenSound")`, and the property is `LoopTime`.
- It is resolved once into a `Func<float>`, so a read allocates nothing.
- Without the type, the reader returns −1 and nothing seeks.
- The harness checks it against the direct read: 0.042 = 0.042.
- IL2CPP: managed stripping is Minimal (1) on iPhone, and `FrontRoomsAudioCapture` references `LoopTime` directly, so the getter is kept.

### 1.3 Clip length vs the 16 s loop

- **The only ad in main** is `StreamingAssets/FrontRooms_Ad_01.mp4`. It is 16.000 s at 30 fps, 480 frames (ffprobe; the harness reads `length` 16.000 and `frameRate` 30 from the player).
- **The loop** is `FrontRoomsScreenSound.LoopMs` = 16000.
- So LoopTime modulo the clip length is LoopTime itself, and the two loops wrap together.
- The code still takes the modulo, because a clip of another length must not break it.
- **The title TV's clip (Ad_03 in the 10-22 note) is not in main.** See open item 2.

## 2. Proofs (clone `W/proj_r3tv`, work dir `W/v5sync_work`)

**The clone.** It was re-synced to main at 17:02 with `rsync --delete`; the replaced files went to `W/_trash_20261029/proj_r3tv_resync_v5_170233`. That brought in:
- c5e03368 + the working tree;
- R3 4eb52b27 and 声音设计's V5 .9;
- main's `FMOD/FrontRooms/Build` banks, so FMOD runs.

After that, the tools were reinstalled; the R3, C26 and C26V harnesses were kept, and so was the Library. Every Assets file equals main's except the payload and the clone-only tools and harnesses. The clone-only tools include `Assets/Scripts/CloneOnly/FrontRoomsCloneOnlySilence.cs`, installed by the line's tools at 17:17: it sets FMOD to NOSOUND in batch, and never ships.

### 2.1 `FrontRoomsV5SyncPlaytest` (clone-only, ASCII, no tuples; `W/v5sync_work/harness/`)

**Setup.**
- It boots through the title handoff (seed 516574485) and keeps the Relay dormant.
- It uses the **real sound**: FMOD Ready, code 2026-10-22.9, the director running. There is no stand-in LoopTime.
- It runs on real time, with no capture clock: the picture runs on Unity's clock, the broadcast on FMOD's.
- It forces a station floor (`SetForTest(true)`).

**The four Kit_CRTMonitor test screens.** Each is kept in view by its own witness camera. Visibility is driven by moving a screen 40 m up, out of view and past the 18 m gate, and back.

| Screen | Role (Attach's 3rd argument) | Bank | Used for |
|---|---|---|---|
| TT | TitleTV | its own (`file://` URL) | sync on its first view and on/off/on twice; a hold; power off/on |
| NN | None | its own | never sought |
| BC | Beacon, under the map (the playing station TV) | the default bank | sync on power-on and off/on; the R3 close-up |
| SH | TitleTV | BC's bank | its edge inside BC's close-up must not seek |

**d** is picture minus broadcast: `player.time − (LoopTime mod length)`, the shorter way round.
- `player.time` is the start of the frame on screen, so a picture exactly in step reads −1 frame … 0.
- FMOD's position moves in steps of about 21 ms.
- Each edge is sampled over 15 frames, 3 frames after the seek shows. Median and max |d| are given, plus the largest difference between the frame on screen and the broadcast's frame (floor(LoopTime × 30)).
- **The check: median |d| ≤ 1 clip frame (0.033 s).**

**Final runs:** passes 1, 2 and 3 at 17:17, 17:37 and 17:38 were **PASS 41/41 each**. Raw data are in `W/v5sync_work/runs/v5_pass{1,2,3}/` (`frames.tsv` one row per frame, `log.txt`, `result.json`). Medians in seconds; max in brackets:

| Edge | Before the edge | Pass 1 | Pass 2 | Pass 3 | Seeks |
|---|---|---|---|---|---|
| A1 TT first view (the picture came first; the broadcast started after the director's pass) | the picture runs, the broadcast starts later | 0.023 (0.041) | 0.020 (0.034) | 0.016 (0.031) | 1 |
| A2 TT back in view after 3 s | −3.77 … −3.81 s | 0.025 (0.042) | 0.016 (0.034) | 0.029 (0.046)* | 1 |
| A3 TT back in view after 3 s | −3.77 … −3.81 s | 0.023 (0.043) | 0.015 (0.030) | 0.030 (0.047) | 1 |
| A5 TT edge under the pause card: served on the release | −1.64 … −1.66 s | 0.024 (0.043) | 0.022 (0.039) | 0.030 (0.048) | 1, one frame after the release |
| B2 TT powered on (the loop restarts) | picture held | 0.013 (0.031) | 0.010 (0.028) | 0.010 (0.029) | 1 |
| C1 BC (Beacon) powered on, its first view | picture started first | 0.008 (0.023) | 0.008 (0.025) | 0.020 (0.037) | 1 |
| C2 BC back in view after 3 s | −3.12 s | 0.023 (0.038) | 0.023 (0.043) | 0.018 (0.036) | 1 |

\* In pass 3 the A2 seek completed at once, but the decoder kept showing the old frame for 40 frames (about 0.67 s, under load) before the new time showed. Then it was in step (image `v5_sync_edges.jpg`, TT seek 2). Every other seek showed within 0–1 frame.

In every sampled frame of every edge, the frame on screen was within 1 frame of the broadcast's frame. A5 is the exception: 2 frames at its widest, as the broadcast resumes from the pause in the same frames.

**The checks, all passes:**
- **Exclusions:**
  - **None:** NN came into view 3 times with **0 seeks**. Each time it went on from where it was (2.4 s and 5.6 s off the broadcast: left alone).
  - **Not every frame:** bank A had **5 seeks for TT's 5 visibility edges**, and none in 2 s of steady view.
  - **Never while held (C26):**
    - Under the pause card there was no seek, and none on the release for a screen that stayed in view.
    - A5: an edge under the card was held while the card was up, then served once in the frame after the release.
  - **Never the R3 close-up's bank:**
    - E opened BC's close-up (InspectedScreen BC, timeScale 0). SH, a TitleTV on BC's bank, was then powered on and came into view.
    - Bank D had **0 seeks** during the 120 close-up frames and 0 in the 90 frames after Esc.
    - BC's picture played every frame and never stepped back. Bank D in total: 2 seeks for BC's 2 edges.
- **Power** (`IsPoweredOn`, item 3):
  - B1: TT powered off reads `IsPoweredOn` false. Its `FrontRoomsScreenSound` turned off and released its broadcast, and LoopTime is −1, since no sounding screen plays.
  - B2: power on starts the loop again, with one seek, in step. Nothing on our side reads or writes power for V5.
- **Setup:**
  - Roles were set through Attach's third argument; TT, NN and BC are on separate banks and SH shares BC's.
  - LoopTime was −1 before the test screens: main sets no role anywhere.
  - The clip is as long as the loop.
  - FMOD did not fail.

**Drift after a hold release, with no seek** (A4, INFO): TT was in view and in step, then held under the pause card for 2 s and released.
- Median |d| after the release was 0.041, 0.058 and 0.055 s (max 0.058–0.077; the frame on screen was up to 2 frames behind the broadcast's). That is **over one frame**.
- The agreement asks for no re-seek after a release, so none was built (open item 1).
- Under the card, LoopTime stood still (13.312 → 13.312 over 2 s), because the director pauses the SFX bus with Screen under it.

**C3 after the close-up** (INFO, no seek): BC and its exempt broadcast both ran through the close-up; median |d| afterwards was 0.021–0.024 s.

### 2.2 Regressions (the same clone: main + the package; FMOD banks present, silent)

| Suite | Result | At the R3 landing |
|---|---|---|
| `FrontRoomsR3Playtest.RunBatch` (desktop) | **PASS 71/71** | 71/71 |
| `FrontRoomsR3Playtest.RunPhoneBatch` (iPhone 16 Pro profile) | **PASS 63/63** | 63/63 |
| `FrontRoomsInspectPlaytest.RunBatch` (main's EXAMINE playtest) | **PASS 134/134** | 134/134 |
| `FrontRoomsC26Playtest` (clone-only) | **PASS 44/44** | 44/44 |
| `FrontRoomsC26Verify` (clone-only) | 18 passed, 1 failed | 18 / 1, the same check |

The C26Verify failure is the known one: "V2: the scene reloaded (a new game)", 60 §5 item 7. Every screen in these suites is None, so V5 never runs in them.

The R3 landing ran without banks (FMOD failed to load, silent). These runs had FMOD up with NOSOUND output; the counts are the same.

Raw results: `W/v5sync_work/runs/{r3,inspect,c26,c26v}/`.

### 2.3 Roslyn and the package

**Scratch tree** (`W/v5sync_work/scratch_tree`): a fresh APFS copy of main's Assets, Packages and ProjectSettings, taken at 17:18. The package was applied by the script itself (`APPLY_REAL`): MANIFEST OK, and a second run says ALREADY APPLIED. Log: `W/v5sync_work/scratch_apply.log`.

| Check | Result |
|---|---|
| `roslyn_check.py --all` | **COMPILE OK**, 0 errors in 7 assemblies (Assembly-CSharp 125 files, Assembly-CSharp-Editor 69). Output: `W/v5sync_work/roslyn/scratch_mac` |
| `--all --ios` | **COMPILE OK (iOS defines)**, 0 errors. Output: `W/v5sync_work/roslyn/scratch_ios` |

The only warning in the touched file is the base's own `FindFirstObjectByType` (CS0618, line 265).

**Dry run on main:** `bash W/apply/v5_picture_sync.sh` gives **DRY RUN OK** (log `W/v5sync_work/dry_run_main.log`):
- the base `4eb52b27` matches;
- the `.meta` `5b16edd7` is unchanged;
- the payload `d0705a03` matches;
- section 4 finds what the payload reads in main's `Audio/FrontRoomsScreenSound.cs`: namespace `FrontRooms.Audio`, `public sealed class FrontRoomsScreenSound`, `public static float LoopTime`. As NOTEs, it also checks that the sound side still reads `screen.IsPoweredOn` and keys on `SoundRole != None`.

**Negative test** (`W/v5sync_work/scratch_neg.log`): with `LoopTime` renamed in a scratch copy, the dry run REFUSES and writes nothing.

**Format:** v2 (base copy `v5_picture_sync_base/`, `v5_picture_sync.diff`, `_bases/_keep/_expected`), LF only. `W/v5sync_work/build_pkg.sh` rebuilds it from `base/` and `payload/`, and refuses if main's file moved.

### 2.4 The first round (kept for the record)

- **17:11, the payload before §1.2's details:** 36/41.
  - A1, B2 and C1 failed: the seek went to FMOD's 0.000 before the loop ran. For C1 that read as "within a frame", so there was no seek at all.
  - A3 and C2 sat 1.1–1.2 frames behind.
  - The payload was fixed (§1.2).
- **17:18, the fixed payload with the old harness window:** 40/41.
  - A1 was sampled while the decoder still showed the old frame, 13 frames after `seekCompleted`.
  - The harness now waits until the new time shows (at most 90 frames) and reports how long that took. The payload was not changed.

## 3. Images (Shot format, `W/tools/shotframe`)

They are listed in `W/vl_pending/v5_picture_sync.md`. VL numbers are assigned on placement.

| Image | Shows | Verdict |
|---|---|---|
| `W/v5sync_work/images/v5_sync_timeline.jpg` | TT and BC, picture minus broadcast per frame over the whole run (pass 3). Seeks are marked, out-of-view spans barred, holds shaded | PASS |
| `W/v5sync_work/images/v5_sync_edges.jpg` | Every seek, from 0.4 s before to 0.8 s after (pass 3) | PASS |

## 4. Open items

1. **Drift after a hold release (for 声音设计).**
   - After a 2 s pause card, a screen that stayed in view resumes 0.04–0.06 s behind the broadcast (median; up to 2 frames).
   - The likely cause (read from `frames.tsv`, not proven): the decoder and the bus do not stop and resume in the same instant, and Unity's VideoPlayer catches up on resume.
   - The agreement asks for no re-seek, so none is built. If 声音设计 wants one, it is a one-line follow-up: on the release frame, one `SyncToSound` per visible sounding screen when |d| is over a frame. It needs their agreement first.
2. **The title TV's clip (for 声音设计 and 平面视觉 · 广告).**
   - Only `FrontRooms_Ad_01.mp4` (16.000 s, 30 fps) is in main. The 10-22 note names Ad_03 for the title TV, which is not in `StreamingAssets`.
   - If the TitleTV clip is not 16.000 s, the seek still takes LoopTime modulo the clip length, so each edge is right. But the loops drift apart at every wrap while the screen stays in view.
   - Make the title ad 16.000 s (480 frames at 30 fps), or tell 游戏视觉.
3. **Broadcast stalls under load (for 声音设计).** In the first-round run, at about 37 s, LoopTime stood at 9.088 s for 1.27 s while the Beacon's picture ran on.
   - That was load ≈ 68, and nothing in the trace paused it.
   - The picture is not re-synced until the next edge, as agreed.
4. **The decoder's late landing.** A seek can take up to 40 frames to show under load (once in pass 3, see the image); it is usually 0–1.
   - Until it shows, the screen holds its last frame.
   - At a sounding screen's first view or a power-on, the picture starts before the broadcast and then steps once to it. That is a small visible jump, at most the director's one-second pass.
   - If that jump matters, 声音设计 could attach the sound at once instead of on the pass.
5. **Shared banks.** A seek moves every screen on that bank. On a station floor the desks show stills, so nothing else visibly moves. A None screen that shares a sounding screen's bank jumps with it.
6. **Inert until a role is set.**
   - station-look-v1 attaches the station TV with Beacon. It must stack on this payload `d0705a03`, or 3-way rebase onto it: the hunks are small and in separate places.
   - The crt_tube relaunch attaches the title TV with TitleTV.
7. **The first two harness runs may have been audible.** The runs at 17:11 and 17:17 started before the clone-only silence tool reached the clone at 17:17. FMOD may have played the TV loop through the Mac's speakers for about a minute each. The runs from 17:18 on had the tool, and their logs show `[CloneOnlySilence]` (NOSOUND).
8. **WebGL** was not run (it is a separate track). If a WebGL player reports `canSetTime` false, the request is used up without a seek.

## 5. Timings (under load, 1-min load 14–68; other lines ran Unity in proj_hudlive and proj_station_2)

| Run | Wall time |
|---|---|
| Script reimport | 10–15 s |
| V5 harness | 45–60 s |
| R3 desktop / phone | 85 / 80 s |
| EXAMINE playtest | 146 s |
| C26 / C26 Verify | 20 / 30 s |
| Waiting for a Unity slot (VISUAL_UNITY_JOBS = 2) | about 10 min before pass 2, 1 min before the first V5 re-run |
| Roslyn | 2–3 s per assembly |

The job log is `W/v5sync_work/jobs.log`. The scripts are `run_unity.sh`, `chain.sh`, `build_pkg.sh` and `images.py` in `W/v5sync_work/`.

## 6. Re-seek on release (build, 2026-10-29 17:5x–19:09; v5-reseek-release, rule 22)

**Status: READY, not applied.** This section replaces open item 1 (§4) and the "no seek on the release" lines in §1.1 and §2.1.
- **Package:** `W/apply/v5_reseek_release.sh`. Base = main's ScreenVideo `d0705a03` (this report's V5 payload, still main's at 19:09), payload **`ae5eb864`**, `.meta` `5b16edd7` kept. Diff: `W/apply/v5_reseek_release.diff`, 3 hunks, +31 / −2.
- **Dry run on main, 19:09:** DRY RUN OK (log `W/v5reseek_work/dry_run_main.log`).
- **Apply (the 游戏视觉 session):** `bash /Users/redwang/FrontRoomsVisualWork/apply/v5_reseek_release.sh --apply`.
- LANDINGS.md and MERGED.md were not touched. The build was stopped by the account switch at 18:25 and finished on account A (RESUME NOTE #8); work dir `W/v5reseek_work`, clone `W/proj_r3tv`.

**Asked by 声音设计 (10-29):** "Re-seek on release: yes. Please re-seek once, to LoopTime, when the pause card or the C26 hold releases. 40-60 ms with audio ahead is noticeable on the 3/7/10/14 s beat cuts."

### 6.1 What changed (`Assets/Scripts/Media/FrontRoomsScreenVideo.cs` only)

1. `OnGamePaused(false)`: calls `FrontRoomsScreenAdBank.SyncToSoundOnRelease()`.
2. `OnGameInspecting(false)`: calls it too, **before** `SyncCloseUpClocks()`, so the close-up's bank is still marked when the requests are handed out.
3. `SyncToSoundOnRelease()` (new, next to `SyncToSound`):
   - does nothing while the world is still held (the pause card over a close-up);
   - otherwise gives **one** request to each bank that has a sounding screen (`SoundRole != None`) in view;
   - skips the close-up's own bank (R3: it stays on the close-up clock and plays on);
   - skips a bank whose screens in view are all None.

The request is V5's existing slot, served by `ServeSoundSeek` in the bank's `Update`. So the rules are the edge seek's:
- it waits for LoopTime > 0;
- it aims one frame ahead, modulo the clip length;
- it leaves a picture alone that is within one clip frame;
- it is dropped if the screen leaves view;
- an edge request already waiting is the same request.

It fires on two events, never per frame. Nothing else changed: the edge seek, `IsPoweredOn`, `SoundRole`, the enum, `Attach`, and R3's rule, exemption and clock are as before.

### 6.2 Proofs

**The clone.** `W/proj_r3tv` was re-synced at 18:00 to main's then-current files (Game `527ce171`, MapWorld `5c57cfa5`, 声音设计's LoopTime fix .12, FMOD banks). Only content-changed files were copied, and the old copies went to `W/_trash_20261029/proj_r3tv_resync_v5r_180010`. The clone holds the payload, the clone-only silencer (every log shows `[CloneOnlySilence] … NOSOUND: OK`) and the harnesses.

Main moved after 18:00: KeyHud `60f28e7f`, A13's new `Rendering/Exit/*`, and string-only edits in the settings, plus `3DGame.Mobile` (an A26 label). None of these is on the hold or video path. The Roslyn gate below runs on main as it was at 18:5x.

**`FrontRoomsV5SyncPlaytest`, extended with the release cases** (clone-only, ASCII, no tuples; `W/v5reseek_work/harness/`):
- **A4** (TT in view through a 2 s pause card);
- **H1** (the pause card over two sounding banks, TT's and BC's);
- **H2** (TT in view on its own bank through BC's close-up).

| Release check | 18:19 (load ≈ 80–100) | 18:56 (load ≈ 12) | 19:01 (load ≈ 43) |
|---|---|---|---|
| A4: exactly one re-seek on bank A (3 → 4); median \|d\| after (max) | 0.023 (0.042) | 0.023 (0.042) | 0.017 (0.038) |
| A4: NN (None, in view) takes none | ok | ok | ok |
| H1: one re-seek per bank (A 7 → 8, D 2 → 3); TT / BC median | 0.011 / 0.012 | 0.024 / 0.022 | 0.021 / 0.019 |
| H1: bank C (None) takes none | ok | ok | ok |
| H2: close-up release re-seeks TT's bank once (A 8 → 9); median | 0.020 | 0.012 | 0.012 |
| C3/H2: the inspected bank (D) never seeks, with BC and SH, both sounding, in view | ok | ok | ok |
| Bank totals: A 9 ≤ 6 edges + 3 releases; D 3 ≤ 2 edges + 1 release | ok | ok | ok |

- Every re-seek completed 1 frame after the release.
- Before this package, the release left TT 0.041–0.058 s behind (§2.1). It is now 0.011–0.024 s, within one clip frame (0.033 s), in every run. The widest frame difference shown was 2 frames, in the first few frames while the bus resumes.

**Overall:** 50/51 in each run. The one failure was a different **edge** check each time, and each sat just over the one-frame line (0.033 s):

| Run | Failed check | Median \|d\| | Seek timing |
|---|---|---|---|
| 18:19 | C2 | 0.034 s | shown 42 frames late, under load ≈ 100 |
| 18:56 | A3 | 0.044 s | completed 2 frames after the edge, shown 3 later |
| 19:01 | A5 | 0.034 s | |

- A3 runs before the first release of the run, so this package cannot touch it.
- Each of these checks passed in the other two runs.
- The edge path is unchanged from `d0705a03`. This is open item 4 (the decoder's late landing) showing at the line. See 6.3 item 1.

**Regressions:**

| Suite | Result |
|---|---|
| `FrontRoomsR3Playtest.RunBatch` (desktop), 18:20 | **PASS 71/71** |
| `FrontRoomsR3Playtest.RunPhoneBatch`, 18:22 | **PASS 63/63** |
| `FrontRoomsInspectPlaytest.RunBatch`, 18:24 | **PASS 134/134** |
| `FrontRoomsC26Playtest` with the payload, 18:56 / 19:00 / 19:06 | 43/44 each: "after the release the video runs: video frame +0 over 30 frames" (C, C, D) |
| `FrontRoomsC26Playtest` with **main's base `d0705a03`** in the same clone (A/B), 19:05 / 19:08 / 19:08 | 44/44, 44/44, **43/44, the same C failure** |

The C26 failure is a stall that main's base shows too:
- The player reports `isPlaying` while its frame stands still for 30–45 frames. Then it jumps ahead by the stalled time (48 → 68).
- Case C releases `InspectingNow` by reflection, with no `Paused` or `Inspecting` event, so no line of this package runs there.
- Every screen in C26 is None (main sets no role, and no prefab serializes one), so the release call finds nothing to seek in D either.
- The stall did not show in any C26 run before tonight (10-22; 10-29 16:17, 16:23 and 17:24). It started in tonight's machine state: three other Unity editors were running (proj_station_2, proj_hudkey, and an editor open on main since about 18:45) at load 30–45.

Raw data: `W/v5reseek_work/runs/{v5,v5_rerun8,v5_rerun8b,r3,inspect,c26,c26_8b,c26_ab_base,c26_ab_payload,c26_ab_base2,c26_ab_base3}/`. Job log: `W/v5reseek_work/jobs.log`. Unity logs: `W/logs/v5reseek_*.log`.

**Roslyn, on a fresh scratch tree** (`W/v5reseek_work/scratch_tree`): main's sources, Packages and ProjectSettings at 18:5x, including KeyHud `60f28e7f` and A13. The package was applied by its own script (`APPLY_REAL`): MANIFEST OK, log `scratch_apply.log`.

| Check | Result |
|---|---|
| `roslyn_check.py --all` | **COMPILE OK**, 0 errors in 7 assemblies (Assembly-CSharp 127 files, Assembly-CSharp-Editor 69) |
| `--all --ios` | **COMPILE OK (iOS defines)**, 0 errors |

The only warning in the touched file is the base's own `FindFirstObjectByType` (CS0618).

**Negative test** (`W/v5reseek_work/scratch_neg.log`): with `Inspecting` removed from a scratch `FrontRooms3DGame`, section 4 of the dry run REFUSES and writes nothing.

### 6.3 Open items

1. **Edge checks at the one-frame line.** With 声音设计's .12 LoopTime fix and tonight's load, one edge per run lands 1–11 ms past one clip frame. That happened in 3 runs out of 3, each time on a different edge; before .12 it was 0 runs out of 5.
   - The cause is the decoder (late `seekCompleted` or a late first frame), not the target.
   - If 声音设计 hears it, a follow-up could aim 1.5 frames ahead instead of 1. It is not built: it needs their agreement and a fresh run of all the edge cases.
2. **The post-resume video stall (C26 C/D).** It is not this package's, and it shows on the base too. It should be re-checked on an unloaded Mac before anyone treats it as a game bug.
   - If it shows unloaded, it belongs to the C26 resume path (`PrepareIfNeeded` → `Play` after `Pause`), which is 游戏视觉's.
3. **Rebase note.** station-look-v1 (live, `wf_cf57755b-3c4`) must stack on `ae5eb864` once this lands, or 3-way merge (`W/tools/rebase_apply.sh`). Its `PowerOffForGood` hunks are elsewhere in the file.
4. **Tell 声音设计 once it lands.** sound.md says they are waiting for the ping. Then they can listen with `-autopilotStation 4`.
