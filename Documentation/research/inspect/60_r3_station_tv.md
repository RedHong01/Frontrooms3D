# 60 · R3: the playing station TV (build + fix round, 2026-10-29)

**Status: READY, not applied.**
- The package is `W/apply/r3_station_tv.sh`. Its dry run against main gives DRY RUN OK (fix round, 16:34).
- Roslyn on main + the package: COMPILE OK on macOS and on iOS.
- Proofs (a)–(e) pass on desktop (71/71) and on phone (63/63), in a clone that equals main + the package.

The session lands it. LANDINGS.md and MERGED.md were not touched. (`W = /Users/redwang/FrontRoomsVisualWork`.)

**Decision.** Red, 10-29, "全部按推荐" (`Takeover_2026-10-18/reviews/DECIDED_VS_LANDED_20261029.md` §6, row R3):

> Only the playing station TV shows the eye. E opens a close-up of the ad screen and the game pauses. Dark maze screens show no eye.

**Spec.** `NOTE_1029_R3_station_tv.md`. That includes 声音设计's two asks, answered yes, and 关卡设计's 15:3x key-taken gate.

**Where it was built.** In the clone `W/proj_r3tv`, work dir `W/r3tv_work`:
- **Build round:** the clone was main c5e03368 + working tree, re-synced at 15:14 after 关卡设计's A26 map landing (MapWorld 527ddb39). The 6.6 Library came from `W/proj_ref66`.
- **Fix round (16:06):** re-synced again with main's later landings: KeyHud (#34 SoundCue, 15:19), `FrontRoomsKeyHudSoundCueTests.cs`, the 3 audio scripts and `FrontRoomsFmodVerify.cs` (15:23), and the FMOD banks (15:30). The clone's old copies are in `W/_trash_20261029/proj_r3tv_resync_160643/`. After that, every Assets file in the clone equals main's, except the 3 payload files and the clone-only harnesses.

> **Rebase note:** the dead `crt_tube` run also touches `FrontRoomsScreenVideo.cs` (the CRT). Its relaunch must rebase on this package. Its Attach call must also pass `FrontRoomsScreenSoundRole.TitleTV` (`station/NOTE_1029_station_tv_soundrole.md`).

## 0. Fix round (after the two verifiers, both PASS)

The verifiers found no correctness defect. Every defect they listed is fixed:

| # | Defect | Fix | Proof |
|---|---|---|---|
| 1 | The proof clone predated main's KeyHud #34 (15:19) and audio (15:23) landings (both verifiers). | The clone was re-synced with those files, the new KeyHud test and the FMOD banks (16:06). Every proof was re-run on it: R3, EXAMINE, C26 and C26 Verify, before and after. | §2: R3 71/71 and 63/63; (e) has the same counts before and after. |
| 2 | The hung-screen window was narrow, and the contract gave no upper height (verifier 1). | The window was measured instead of estimated. Its top is higher than the 2.54 m the verifier estimated: at 20° down from 1.2 m out, **2.70 m solves and 2.75 m does not**. The contract now gives the rule: screen centre ≤ 2.65 m. V1's planned pose is now covered by checks: 2.42 m (the plan as first written) and 2.50 m both pass, and the plan's later update (P02 at p.y 2.33, screen centre 2.46 m) lies between the passing 2.43 m and 2.50 m. | §2 (h); the contract §2. |
| 3 | The eye could show before the ad was on the screen: a bank with no frame yet, or a failed stream (verifier 1). | "Playing the ad" now includes the picture. `IsPlayingStationTv` also needs `FrontRoomsScreenAdBank.HasPicture`: the bank is prepared and has shown a frame, latched, so a pause or the hold never drops it. While the TV waits for that frame its E is still not TURN OFF (`IsStationTv`). A stream that failed (`Failed`) is not playing the ad: no target, and it keeps its own E. | §2 (w): 0 of 3,822 / 3,802 rows had the target without a picture; images `r3_w_*`. |
| 4 | The build stage's own scripts used `rm -rf` on run-output copies (verifier 1). | `chain_after.sh`, `chain_before.sh` and `build_pkg.sh` now move old output to `W/_trash_20261029` and never delete. `build_pkg.sh` also reads main's HEAD from `.git/HEAD` + the ref file, never through git (iCloud-evicted objects). The 15:38 copies that the old script removed cannot be recovered; they were scratch output, superseded by the 15:41 runs. | The scripts in `W/r3tv_work/`; the old versions are in `W/_trash_20261029/r3tv_scripts_pre_fix_161301/`. |
| 5 | The report paired measurements from two different windows (verifier 2). | The harness now prints the clock pair over one window (the first 150 close-up frames), and the whole close-up up to the second capture separately, with its loop wraps. | §2 (b). |
| 6 | The contract said M4 / `FloorHasStation` was not in main (verifier 2). | Corrected. It has been in main since the 15:08 A26 landing (`SetStation`; the game's `PlanStation` / `HoldStation`). It reads false only because no level profile assigns a `stationModule`. | The contract §1. |

## 1. What was built (3 files, all 游戏视觉's; every `.meta` kept)

| File | Change |
|---|---|
| `Assets/Scripts/Media/FrontRoomsScreenVideo.cs` | Base `29e76f79`, payload **`4eb52b27`** (fix round; build round `0821e76c`). The R3 rule, the picture term, the READ target, the C26 exemption, the close-up clock, the hung-screen distance fit, V5 |
| `Assets/Scripts/Rendering/FrontRoomsStationPresence.cs` | Base `4c0563dc`, payload `b6353ae8` (unchanged in the fix round). `StationKeyIsTaken`: the key-taken gate from 关卡设计, 15:3x |
| `Assets/Scripts/Rendering/Inspect/FrontRoomsInspectCatalog.cs` | Base `361f6006`, payload `d832a286` (unchanged). `PromptReadScreen = "E  ·  READ SCREEN"` and the registry label `StationTvKit = "Screen_StationTV"` |

No KeyHud, map, touch or sound file was touched.

### 1.1 The rule: `FrontRoomsScreenVideo.IsPlayingStationTv`

The screen decides by itself, and the map adds nothing. All of these must hold:
- it is **not an Office desk CRT** (`AttachDesk`);
- it is **powered**;
- it is **not a still**;
- it sits **under a `FrontRoomsMapWorld`** (the station is a map room; the title rooms are not);
- the floor **has a station** (`FrontRoomsStationPresence.FloorHasStation`);
- the floor's **key is not taken** (`FrontRoomsStationPresence.StationKeyIsTaken`);
- **it is playing the ad** (fix round): its bank has not failed, and it has put the ad's first frame on the screen (`FrontRoomsScreenAdBank.HasPicture`).

**`HasPicture`.** The player is prepared and has shown a frame: its frame index is set, or the playhead has moved (a fallback for a platform that reports no frame index). It is latched, so a pause, the C26 hold or the TV leaving view never drops it. Only an error that leaves the player unprepared clears it.

**`Failed`.** The last prepare failed (`errorReceived`) and the player is not prepared. It clears on the next successful prepare; `PrepareIfNeeded` retries every 2 s, as before. An error while the player is still prepared changes nothing.

**`IsStationTv`.** The rule without the picture term. While it holds, the screen's own E is never TURN OFF. A TV waiting for its first frame therefore has no eye yet and no TURN OFF; that wait was 2–3 frames (about 0.6 s of real time) in the proof. A failed TV keeps its own E like any other screen. Off a station floor the picture term is never read, because the cheap terms come first.

`StationKeyIsTaken` follows the note: the map's bool `StationKeyIsTaken` is read by reflection whenever the map is handed out (MapRunStarted, FloorChanged), like `FloorHasStation`. The map's `StationKeyTaken(Vector3)` event is subscribed directly and turns it true at once. The event is never read alone, and the value is false at MapRunEnded.

### 1.2 The READ target

While the rule holds, the screen's surface child carries a `FrontRoomsInspectable`, so `FrontRoomsInspect.Current.GetComponentInParent<FrontRoomsScreenVideo>()` finds the screen. The target is:
- **kind and verb:** flat, verb READ, `ship`, sound `plastic`, label `Screen_StationTV`, prompt `E  ·  READ SCREEN` (平面视觉's approved string, FIX_CARD_VL217 §B / DECISIONS #29);
- **framed on the screen face:** the surface quad at `screenAnchor`, `screenSize` (0.28 × 0.19 m on Kit_CRTMonitor), normal = the monitor's +Z, aim box 2 cm deep behind the glass;
- **on EXAMINE's default numbers:** reach 1.6 m, approach 60°, close-up distance 0.18–1.0 m.

When the rule stops holding (power off, a still, no station, key taken, no picture), the component is disabled, which unregisters it: no target, no eye. Power on brings it back at once, because the bank's picture is latched.

The station TV's **own E** (TURN OFF) is off: its E is the close-up. A press the game refuses (the 0.3 s cooldown, a Relay nearby, the first frame not shown yet) therefore never falls through to the power switch, which is EXAMINE C5's rule. A powered-off TV, a TV whose stream failed, a desk and every other screen keep their own E.

### 1.3 Hung screens: `FitCloseUpDistance`, needed for the decided TV

61 decided the station TV's pose: north wall, bottom ≥ 2.15 m, tilted 20° down, on Kit_CRTTV with its screen centre about 0.284 m over its base, so the screen centre sits at about 2.43 m. With EXAMINE's default flat numbers that pose never solves:
- the camera may rise only 0.10 m over the eye (`MaxRise`);
- a 0.3 m fit distance cannot look up 0.7 m;
- so `CanOpen` is false, and there would be **no eye at all**.

So the target takes a base distance (`InspectDef.dist[0]`), computed from the screen itself:

> base distance = (face height over the eye band) / sin(maxAngle − the normal's pitch) + 0.05 m, clamped to 0.18–1.0 m.

The eye band is taken from the map's floor + 1.62 + 0.10. A screen at or below the band gets 0, which is the default solve, so the desk-height proofs below are untouched. The value is recomputed only while the world is not held.

**The window (measured in the fix round, desktop and phone the same, 20° down, from 1.2 m out):** 2.42 m (V1's plan as first written; its 16:xx update puts the centre at 2.46 m), 2.43 m (decided), 2.50 m and the probes 2.54–2.70 m all solve. **2.75 m and higher do not** ("outside the body band"; at 2.90 m the screen is also out of reach). Above about 2.58 m the base distance sits on the 1.0 m clamp, and the solver's own tilt still finds a pose up to 2.70 m. The contract gives 关卡设计 the rule: screen centre ≤ 2.65 m.

### 1.4 The C26 exemption

`HeldFor(bank)` is `WorldHeld`, except for **the close-up's own bank**: the bank of the screen whose READ target is `FrontRoomsInspect.Current` while `InspectingNow`.
- **That bank plays on from where it is.** `Play` after `Pause` resumes; nothing stops it or seeks it.
- **The pause card over the close-up still holds it.**
- **Unchanged:** every other bank, `WorldHeld` itself, and the E ray (off while held and on the release frame).

### 1.5 The clock

- **On the close-up's bank, on the game's `Inspecting` event.** In the frame the close-up opens, the player goes onto `CloseUpClock = UnscaledGameTime`, which runs at timeScale 0. In the frame it ends, it goes back to the **decided clock**: the `timeUpdateMode` the player had when the bank made it (this file never set one).
- **Each bank Update** repeats the same sync as a safety net. It allocates nothing (C26 Verify V5: 0 B over 20,000 calls, before and after).
- **Measured:** the decided clock in this build is already `UnscaledGameTime` (Unity's default; timeReference Freerun, URL source). So on today's banks the switch writes nothing. A bank with a GameTime clock would be switched and restored.

### 1.6 V5 (as agreed with 声音设计, 10-22)

Appended, and nothing else of V5:
- top-level `public enum FrontRoomsScreenSoundRole { None, TitleTV, Beacon }`;
- `public FrontRoomsScreenSoundRole SoundRole`;
- `Attach(monitor, adFile = null, soundRole = None)`. A non-None role is set; None leaves a role given earlier, the way an empty adFile leaves the file.

### 1.7 Shared bank

On a station floor, the TV plays the default ad (`FrontRooms_Ad_01.mp4`), which is the same bank as every desk CRT.
- The desks show their own still textures, so nothing else visibly moves.
- Any other non-still screen on that bank shows the same running RenderTexture. It sits behind the close-up, because the camera frames only the TV face.
- The proof's SHR screen (same bank, outside the map) did exactly that. After the close-up, every bank is back under the plain C26 rules.

## 2. Proofs

**Harness.** `FrontRoomsR3Playtest` is clone-only, ASCII, no tuples, in `W/r3tv_work/harness/`. It boots through the title handoff with seed 516574485, keeps the Relay dormant, and stands in for a station profile with `FrontRoomsStationPresence.SetForTest(true)`. It spawns five Kit_CRTMonitor test screens by the placard:

| Screen | What it is |
|---|---|
| **TV** | under the map, `Attach` |
| **DSKS** | under the map, `AttachDesk` at a spot whose seeded still is Cursor: a still desk screen |
| **DSKD** | under the map, `AttachDesk` whose seeded still is Off: a dark maze screen, left off by the still mix |
| **OTH** | outside the map, on its own bank (the same mp4 by `file://` URL), kept visible by a witness camera |
| **SHR** | outside the map, on the TV's bank |
| **TV2** (fix round, section w) | under the map, `Attach` on a fresh bank of its own (its own `file://` key, the same mp4) |
| **MISS** (fix round, section w) | under the map, `Attach` on a stream file that does not exist |

Game input is `FrontRoomsInput.InjectSnapshot`. The CRT's own E is a real key on a test keyboard, desktop only. The phone run forces the iPhone 16 Pro profile at 1311 × 603.

**Final runs (fix round).** 16:30–16:33 on the re-synced clone: **desktop PASS 71/71, phone PASS 63/63.** The build round's 58 and 52 checks are all still there; the new ones are the V1-pose hung checks (4) and section w (9 on desktop, 7 on phone). Raw output is in `W/r3tv_work/runs/after_r3/<mode>/`: `log.txt`, `result.json`, `frames.tsv` (one row per frame) and `shots.txt`. The runs were under load (1-min load 37–57). An earlier fix-round run (16:18–16:20, 71/71 and 63/63, probes only up to 2.62 m) is in `W/_trash_20261029/r3tv_after_r3_163326_83886/`.

### (a) Aiming: PASS on desktop and phone

- **The TV gets one READ target** (registry 1 → 2 targets). Its face sits on the screen: the centre is 0.000 m off the surface quad, the normal 0.0° off, size 0.280 × 0.190.
- **Eye on the TV.** At 1.2 m:
  - the aim kind is `AimKind.Read` (the game's own `UpdateAim`) and the prompt is `E  ·  READ SCREEN`;
  - key HUD `State == Read`, `EyeVisibleNow`, `Line1Drawn` = `E  ·  READ SCREEN` on desktop and `READ SCREEN` on the phone;
  - phone USE = Read;
  - **no KeyHud change**: HK1 is in main. This now holds on main's 15:19 KeyHud (#34).
- **No eye anywhere else** (aim None, HUD None, eye hidden):
  - the still desk screen;
  - the dark maze screen;
  - the powered-off TV;
  - a playing screen outside the map;
  - the TV itself on a floor without a station. There it keeps its own E: E turns it off and on.
- **(w) Playing the ad** (fix round):
  - **TV2 (fresh bank).** It had no READ target until its bank showed the first frame: 3 frames waiting on desktop (first picture at frame 4, first target at frame 5), 2 on phone (3, 4), about 0.6 s of real time. The target never existed without a picture, and TV2 was never focused for TURN OFF while it waited (0 frames). Once playing: the eye with READ SCREEN.
  - **MISS (missing stream).** The bank failed within 1.0 s of real time. MISS never got a target, there was no eye, and the CRT ray focused it, so it keeps its own E. On desktop its own E turned it off and on again.
  - **The TV across the whole run:** its target never existed without a picture (0 of 3,822 rows on desktop, 0 of 3,802 on phone).
- **(h) Hung poses, 20° down, from 1.2 m out:**

| Screen centre | Base distance | `CanOpen` / eye | Close-up (solved distance, camera height) | |
|---|---|---|---|---|
| 2.43 m (decided) | 0.91 m | yes / yes | 0.921 m, camera 1.72 m, the ad plays | check |
| 2.42 m (V1 plan P02 as first written; the 16:xx update says 2.46 m, between this row's neighbours 2.43 and 2.50 m) | 0.89 m | yes / yes | 0.908 m, 1.72 m, plays | check |
| 2.50 m (V1 read high) | 0.98 m | yes / yes | 0.891 m, 1.72 m, plays | check |
| 2.54, 2.58, 2.62, 2.66 m | 1.00 m | yes / yes | 0.984–1.000 m, plays | INFO |
| **2.70 m** | 1.00 m | yes / yes | 1.000 m, plays | INFO: **the top** |
| **2.75, 2.80, 2.90 m** | 1.00 m | **no / no** ("outside the body band"; 2.90 m also out of reach) | — | INFO |
| 2.25 m, level | 0.92 m | no / no ("outside the body band") | — | INFO |

The numbers are identical on desktop and phone.

### (b) The close-up: the picture keeps running

| | Desktop | Phone |
|---|---|---|
| E opens the close-up (Phase Inspect, timeScale 0) | Inspecting(true) in the E frame 805 | Inspecting(true) in the E frame 790 |
| TV powered after the opening E (the game's E plus a CRT E key in the same frame) | on | on |
| No restart (video frame at E → first close-up frame) | 331 → 331 | 316 → 317 |
| **The first 150 close-up frames** (one window for every number) | clock (unscaled) +2.635 s, real +2.6 s; video +2.667 s, frame 331 → 411, 0 loop wraps; isPlaying every frame; largest step 0.133 s; never backwards | clock +2.555 s, real +2.5 s; video +2.533 s, frame 317 → 393, 0 wraps; isPlaying every frame; largest step 0.100 s; never backwards |
| **The whole close-up, up to the second capture** | clock +6.868 s, real +7.0 s; video +6.900 s, frame 331 → 58, 1 loop wrap (the 16 s clip); never backwards | clock +6.772 s, real +6.8 s; video +6.767 s, frame 317 → 40, 1 wrap; never backwards |
| Every other screen's player (OTH, own bank) | paused at frame 331 throughout, both windows | paused at frame 317 throughout |
| `WorldHeld` | true throughout | true throughout |
| Focused screen | none | none |
| CRT E key inside the close-up | no toggle | not tested (the phone has no CRT E) |
| Two captures of the close-up | 6.18 s apart: video frames 352 vs 58, centre mean abs diff 35.41/255 | 6.09 s apart: 337 vs 40, 32.67/255 |
| Pause card over the close-up (lost focus) | TV paused at once, frame 77 → 77 | TV paused at once, frame 43 → 43 |
| Esc back to the close-up | TV plays at once and goes on from 77 (next frame 78 after 2 frames); OTH still paused | TV plays at once and goes on from 43 (next frame 44 after 2 frames); OTH still paused |

### (c) Esc ends it: PASS on desktop and phone

- **The world comes back** on Esc: Phase Playing, timeScale 1, Inspecting(false) in frame 1269 on desktop and 1252 on phone.
- **The decided clock is back on the exit frame:** `UnscaledGameTime` = decided, `OnCloseUpClock` false, `InspectedScreen` null.
- **No TV toggles on the exit frame.** On desktop the exit frame also carried a CRT E key.
- **OTH plays again** (isPlaying 3 frames after the exit; frame 333 → 334 on desktop, 318 → 319 on phone).
- **The cooldown press does not fall through.** A game E inside the 0.3 s cooldown plus a CRT E key on the TV leaves it on.
- **A second close-up, left with E** (the game's E plus a CRT E key): decided clock back, TV still on.
- **Events pair up:** Inspecting 2 + 2, Paused 1 + 1, FrontRoomsInspect Opened/Closed 2 + 2.

### (d) Power: PASS on desktop and phone

- **Off removes the target and the eye.** Power off is done through `TogglePower` by reflection, standing in for M6's take; there is no public power call yet. The target is disabled at once (registry 2 → 1) and the eye leaves.
- **On brings both back.** Desktop uses the TV's own E key (a dark TV keeps its E); the phone uses reflection. Registry goes 1 → 2 and the eye returns.
- **A CRT E key alone on the playing TV never turns it off.**

### (k) The floor key: PASS on desktop and phone

- **The event.** No profile switches the station on, so `TakeStationKey` refuses; the map's `StationKeyTaken` event was raised by reflection as M3's take would raise it. `StationKeyIsTaken` turned true, the target was removed at once, and there was no eye.
  - The event had 2 handlers: the presence's and the game's own (M6 first leg; the Relay was dormant).
- **The bool alone.** The map's `hasStation`/`stationKeyTaken` were set for one `Refresh` call and restored in the same frame. This reads a take that came before the subscription: no eye.
- **Re-read false:** the eye returns.

### (e) Regressions: the same counts before and after (re-run on the re-synced clone)

"Before" = main's 3 files (`W/r3tv_work/base`, byte-equal to main) with the R3 harness moved out; "after" = the fix-round payload. Both runs are on the 16:06 clone (main + A26 + KeyHud #34 + audio). Results are in `W/r3tv_work/runs/{before,after}_*`; the build round's runs are in `W/_trash_20261029/r3tv_*_16*`.

| Suite | Before (16:14–16:18) | After (16:20–16:24) | Build round (before / after) |
|---|---|---|---|
| `FrontRoomsInspectPlaytest.RunBatch` (main's EXAMINE playtest) | **PASS 134/134** | **PASS 134/134** | 134/134 / 134/134 |
| `FrontRoomsC26Playtest` (clone-only, `W/inspect_work/c26`) | **PASS 44/44** | **PASS 44/44** | 44/44 / 44/44 |
| `FrontRoomsC26Verify` (clone-only) | **18 passed, 1 failed** | **18 passed, 1 failed** (same check) | 18/1 / 18/1 |

The `FrontRoomsC26Verify` failure is the same check before and after: "V2: the scene reloaded (a new game)". It is pre-existing: R from the pause card now reloads the scene, so the 10-22 verifier stops at V2. Its V5 allocation check passes in both: 0 B held and 0 B not held over 20,000 calls.

### (f) Roslyn: main + the package (scratch tree `W/r3tv_work/scratch_tree_fix`)

- **Scratch tree:** a fresh APFS copy of main's Assets, Packages and ProjectSettings at 16:35, with the package applied by the script itself (`APPLY_REAL`): MANIFEST OK, and a second run says ALREADY APPLIED. Log: `W/r3tv_work/scratch_apply_fix.log`.
- **`roslyn_check.py --all`:** **COMPILE OK**. 0 errors in 7 assemblies (Assembly-CSharp 124 files, Assembly-CSharp-Editor 69). Output: `W/r3tv_work/roslyn/fix_scratch_mac`.
- **`--all --ios`:** **COMPILE OK (iOS defines)**, 0 errors. Output: `W/r3tv_work/roslyn/fix_scratch_ios`.
- The only warning in a touched file is the base's own `FindFirstObjectByType` (CS0618).
- **Clone tree + payload + harness** (`W/r3tv_work/roslyn/fixr_clone`): COMPILE OK. Unity batch compile in the clone: 0 `error CS` in every run.

### Package dry run on main

`bash W/apply/r3_station_tv.sh` (16:34) gives DRY RUN OK: 3 bases match, 3 kept `.meta` unchanged, 3 payloads match. The main API it reads is declared:
- `InspectingNow`, `Paused`, `Inspecting`;
- `FrontRoomsInspectable.Configure` / `EnsureRegistered`;
- `FrontRoomsInspect.Current`;
- `FloorHasStation`;
- `StationKeyTaken`;
- `Info.boundsMin`.

The picture term reads only Unity's `VideoPlayer` (`isPrepared`, `frame`, `isPlaying`, `time`), so no new main API is needed. Log: `W/r3tv_work/dry_run_main_fix.log`. Diff: `W/apply/r3_station_tv.diff` (561 lines).

## 3. Images (Shot format, `W/tools/shotframe`)

They are listed in `W/vl_pending/r3_station_tv.md` under "Fix round (PLACE THESE)". VL numbers are assigned on placement. The build-round images with the same names were moved to `W/_trash_20261029/r3tv_images_build_round_163347/`.

| Image | Shows | Verdict |
|---|---|---|
| `W/r3tv_work/images/r3_a_desktop_aim.jpg` | (a) TV eye + E · READ SCREEN; still desk, dark maze screen, TV off: dot only | PASS |
| `W/r3tv_work/images/r3_a_phone_aim.jpg` | the same on the phone, READ on the USE button | PASS |
| `W/r3tv_work/images/r3_b_desktop_closeup_1.jpg` / `_2.jpg` | (b) the close-up at video frames 352 and 58, 6.18 s apart, timeScale 0 | PASS |
| `W/r3tv_work/images/r3_b_phone_closeup_1.jpg` / `_2.jpg` | (b) phone, frames 337 and 40, 6.09 s apart | PASS |
| `W/r3tv_work/images/r3_d_desktop_power_take.jpg` | (d)/(k) power off: no eye; power on: eye back; key taken: no eye | PASS |
| `W/r3tv_work/images/r3_d_phone_power_take.jpg` | the same on the phone | PASS |
| `W/r3tv_work/images/r3_h_desktop_hung.jpg` | (h) 2.43 m and 2.50 m, 20° down: eye + close-up; level 2.25 m: no solve (INFO); the window top is 2.70 / 2.75 m | PASS |
| `W/r3tv_work/images/r3_h_phone_hung.jpg` | the same on the phone | PASS |
| `W/r3tv_work/images/r3_w_desktop_playing_the_ad.jpg` | (w) fresh bank: the eye once the first frame is on the screen; failed stream: dot only, own E kept | PASS |
| `W/r3tv_work/images/r3_w_phone_playing_the_ad.jpg` | the same on the phone | PASS |

## 4. Contract for 关卡设计

`W/apply/r3_station_tv_contract/README.md`, updated in the fix round. There is **no map code and no diff**, so nothing needs a `patch --dry-run`. The screen decides by itself:
- `Attach`, not `AttachDesk`;
- under the map;
- `FloorHasStation`, which has been in main since 15:08 but reads false until a profile assigns a `stationModule`;
- `!StationKeyIsTaken`;
- powered, not a still, and playing the ad (the first frame shown, the stream not failed).

Layout rules for the station:
- one non-desk screen per station floor in the map;
- keep the decided 20° tilt, because a level hung screen does not solve;
- **keep the screen centre at most 2.65 m over the floor**, with a stand spot about 1.2 m out (measured top: 2.70 m solves, 2.75 m does not). Above it the TV has no E at all;
- keep the push-in from the stand spot clear of the counter and the riser.

This matches 关卡设计's note: they land no R3 hook.

## 5. Open items

1. **Station look V1 (游戏视觉)** attaches the real TV with `FrontRoomsScreenVideo.Attach(tv, null, FrontRoomsScreenSoundRole.Beacon)` (`station/NOTE_1029_station_tv_soundrole.md`). Two gaps in main's `Attach`, unchanged here, must be solved there:
   - It sets no `screenAnchor`/`screenSize`. Kit_CRTTV's `screen` anchor is (0, 0.284, 0.245), while the defaults are Kit_CRTMonitor's (0, 0.248, 0.194), 0.28 × 0.19.
   - Its `adFile` only reaches a monitor whose Awake has not run. The harness attached OTH, TV2 and MISS inactive for that reason.
   - Re-run the hung check on V1's real `Kit_CRTTV_Station` pose once it exists. The plan's screen centre (2.46 m in its 16:xx update) is inside the window.
   - V1 stacks `PowerOffForGood()` on R3's `FrontRoomsScreenVideo.cs` payload. It must stack on the fix-round payload `4eb52b27`, not the build round's `0821e76c`.
2. **The hung close-up frames the TV from about 0.9 m**, so the screen fills less of the frame than a desk-height close-up does (image `r3_h_*`). Worth a look by 平面视觉 / Red once V1 exists.
3. **M6 "the take turns the TV off"** needs a public power call from 游戏视觉; the proof used reflection. Not in R3. The take alone already ends the eye through `StationKeyIsTaken`.
4. **`crt_tube`** must rebase on this package and pass `TitleTV` (the note at the top).
5. **Not in this package:** 系统设计's grammar text (`30_crosshair_grammar.md` already carries the C35 line), 声音设计's V5 mix (the Screen bus during an EXAMINE of a sounding screen), and 声音设计's ad level in the close-up.
6. **Unity's VideoPlayer catches up after a resume or a loop wrap** under load: one build-round run showed a 1.3–1.6 s forward jump some frames after `Play`. It is Unity's own behaviour and shows on any bank, the held OTH bank included. Nothing in this package seeks. The fix-round runs showed no such jump: the largest step was 0.133 s on desktop and 0.100 s on phone.
7. **`FrontRoomsC26Verify`** needs a V2 update for the scene-reloading restart. It is clone-only and pre-existing.
8. **When a profile switches A26's station on**, re-run `FrontRoomsR3Playtest` without `SetForTest` on a floor-1 seed.
9. **WebGL:** the picture term falls back to "playing and the playhead moved" when a player reports no frame index. It was not run on a WebGL build here (WebGL is a separate track).

## 6. Timings (fix round; under load, 1-min load 33–100; other lines ran Unity in proj_hudkey and proj5)

| Run | Wall time |
|---|---|
| Script reimport | 15–20 s |
| R3 desktop / phone (with the probes and section w) | 85 s / 81 s |
| EXAMINE playtest before / after | 165 s / 146 s |
| C26 before / after | 20 s / 20 s |
| C26 Verify before / after | 35 s / 30 s |
| Waiting for a Unity slot (VISUAL_UNITY_JOBS = 2) | 6 min before the last R3 run |
| Roslyn | about 2–4 s per assembly |

The job log is `W/r3tv_work/jobs.log`.
