# EXAMINE: contract for the map chat (关卡设计)

From the visual chat (游戏视觉), hand-off stage, 2026-10-14.
These are change requests for files the map chat owns. The visual chat never edits them in Red's project.

**Red's ask (10-07):** every visual asset worth a look (first: the Q16 evacuation plan) can be walked up to and read in a close-up shot, and the game pauses while the player reads.

**What the map side does:** add a fifth game phase, `Inspect`, with `Time.timeScale = 0`. The camera rig gets a sine-eased "full pose" shot. The visual side (`Rendering/Inspect/*`) owns the targets, the framing, the look and the close-up hint text.

Design: `../10_design.md` (§2 flow, §4 pause, §7.2 the C-list). Build and test results: `../20_prototype.md`.

## 1. Files, bases and order

| Step | Who | File | Touches | Base sha1 on main 0a9ee7f (checked 2026-10-14 17:1x) |
|---|---|---|---|---|
| 1 | map chat | `map_inspect_1_rig.diff` (13 hunks) | `Assets/Scripts/FrontRoomsShots/FrontRoomsCameraRig.cs` | `53dc51cd3a3fd1886491d931e5e43cdc9ddd5000` |
| | | | `Assets/Scripts/FrontRoomsShots/FrontRoomsShotTimings.cs` | `2ca796cf0883ba1edfb340d126c175a3e073eed8` |
| 2 | visual chat | `bash /Users/redwang/FrontRoomsVisualWork/apply/inspect.sh` | 20 new files under `Rendering/Inspect`, `Editor/Rendering/Inspect`; 3 Office scripts | (in the package) |
| 3 | map chat | `map_inspect_3_game.diff` (24 hunks) | `Assets/Scripts/FrontRooms3DGame.cs` | `70bedd38112e23ceb93373bed44500b702edeb01` |
| | | | `Assets/Scripts/FrontRooms3DGame.Mobile.cs` | `20d2fe8622fb3eff0a2b81d30c829f43f73e64e2` |

- `map_inspect.diff` is steps 1 and 3 in one file (37 hunks), for review only.
- Apply each step from the project root: `patch -p1 --dry-run < <file>`, then `patch -p1 < <file>`.
- All three diffs dry-run clean on main 0a9ee7f plus the working tree at 2026-10-14 17:1x: no fuzz, no offsets. Step 3 also dry-runs clean on top of step 1.
- If a base sha1 differs, re-read the file and re-anchor. Do not force it.
- **Why three steps:** the visual package uses `ShotKind.Inspect`, `ShotSpec.fullPose`, `ShotHandle.EasedWeight` and `FrontRoomsShotTimings.Inspect`, so step 1 must land first. Step 3 calls `FrontRoomsInspect.*`, so the visual package must land before it.
- Until step 1 is in main, `inspect.sh` prints `WAITING` and exits 3 without writing anything.

## 2. Step 1: camera rig and timings (C16–C19)

Additive. Every existing shot keeps the cubic ease and behaves exactly as before. The map suites were byte-identical with and without it (camera rig 33, glass shot 20, door shot 191).

| # | Change | Why |
|---|---|---|
| C16 | `ShotKind` gets `Inspect` appended | Sound and touch read shot kinds by name. Appending keeps every number |
| C17 | `ShotSpec.fullPose`: pose and FOV are not scaled by Camera motion. At 50 % every blend time is ×1.5; at Off the shot cuts both ways | A close-up at half the distance would not frame the plan. Off must still show the plan (a cut, as SH2 2024 does) |
| C17b | `ShotEase { Cubic, Sine }`. Sine: in 0.5 − 0.5 cos(πw); out 1 − cos(πw/2), started over `OutOnset` (2 frames). `rotBlendOut`: rotation returns on its own clock | Peak speed 4.3 m/s instead of 8.2 m/s. The view swings back first (0.133 s), then the body follows (0.40 s). The onset stops the first return frame jumping 6.5 % of the way (now 1.7 %) |
| C17c | `ShotSpec.fovMin`, honoured for `ShotKind.Inspect` only | The zoom step on phones narrows the FOV below today's −15° floor (30.6° on a phone) |
| C17d | rig raycast buffer 8 → 16 hits | A close-up camera near a wall can see more than 8 hits; the nearest must never drop |
| C18 | `ShotHandle.Weight`, `EasedWeight`, `RotationWeight` | The close-up post volume follows the camera's real pull, not the raw weight |
| C19 | `FrontRoomsShotTimings.Inspect` (reach 1.6 m, angle 60°, travel 0.40–0.55 s, out 0.40 / 0.15 s, cancel after 0.20 s, cooldown 0.30 s, FOV −15 / −10, `OutOnset` 1/30 s …) | One place for every EXAMINE timing, next to the other shots. The adapt amounts are look values and stay on the visual side |

## 3. Step 3: game phase, gate, routing (C1–C15, C25)

| # | Change | Why |
|---|---|---|
| C1 | `Phase.Inspect` appended | The touch playtest reads phases by name |
| C2 | `static event Action<bool> Inspecting`, `static bool InspectingNow`, fields for the aimed / pending target, the latches, `pausedFrom` | Sound keys its snapshot on `Inspecting` only (true at E, false as control returns, false on teardown) |
| C3 | `UpdateAim` asks `FrontRoomsInspect.Find` first. An aim box on a wall face wins up to 2 cm behind the physics hit; a door or furniture in front still wins | No colliders or layers for targets. The placard sits on the wall collider, so its box must beat the wall |
| C4 | Hard gate `CanShowInspect` (hides the prompt): Playing, map play, no harness, no glass shot, no climb, no pull-clear, **no Relay block**, a pose solves. Transient gate `CanPressInspect` (keeps the prompt, swallows E): E latch, 0.30 s cooldown, any shot running | **Chase rule** (below). The transient gate stops E falling through to the door or wall behind the target |
| C5, C5b | E on a target marks it pending. It commits after `relay.Tick`, just before `rig.Tick` | The Relay may enter Chase on the same frame. Committing after its tick means a close-up never starts on a chase frame |
| C6 | In Inspect, E, S or Esc exits once 0.20 s have passed. Esc from Playing/Paused goes through `ResumeFromPause()` | Players press Esc to leave (Lethal Company prints `[ESC]`), so no pause-card loop. 0.20 s stops a double-tap from opening and closing at once |
| C7 | `Update`: `if (phase == Inspect) { Tick; rig.Tick(u) } else if (Playing && mapPlay) { today }` | The rig ticks exactly once per frame (checked by a counter). `UpdateMapPlay` does not run in Inspect, so no input, stamina, Relay or lamp clock moves. Zoom input stays 0 until touch T3 (`ZoomInputWired` false) |
| C8 | `SetPhase`: `timeScale` 0 in Inspect (and Paused-from-Inspect), 1 otherwise. `pausedFrom` recorded for every caller. A stray `SetPhase(Playing)` ends a live close-up first. HUD phase = Playing or Inspect | The phase gate alone leaks lamps, doors, noise and Foley (they run on `Time.deltaTime`). The time scale alone would still read input |
| C9 | R ignored in Inspect | R restarts only from the pause or caught card |
| C10 | Focus loss / app pause in Inspect → Paused; resume returns to the same close-up | A phone call never strands the camera |
| C11 | In Inspect the HUD alpha is kept; `rig.HudFade` fades the panels; the prompt slot moves to the bottom band (`InspectHintBottom` 83) and shows `FrontRoomsInspect.HintText` at its own alpha | The hint must not sit on the framed plan. Glyph bottom 77 px from the 1080p edge (≥ the 72 px margin). Placement is **PROPOSED**; 平面视觉 decides it (G3) |
| C12, C12b, C12c | Teardown: `OnDestroy` sets timeScale 1 and fires `Inspecting(false)`; `End()` aborts a close-up before Caught; `AbortRequested` (target unloaded, Level Designer rebuild) ends it at once | No frozen world after a restart, a catch during the return, or a chunk unload |
| C13 | Touch interim: in Inspect the touch layer stays in `MenuState.Playing` | `MenuState.Inspect` lands with touch T1. Until then the touch pause button and the stick pull-back exit (C6) |
| C15 | Android Back exits a close-up; from Paused it calls `ResumeFromPause()`. Taps are ignored while any shot runs; a tap on a target opens it through the same gate | The shot camera may sit 0.36 m from a door leaf, so a tap must not reach the world through it |
| C25 | The exit E and the exit S are latched until released | No door opens on the exit E; the player does not walk backward on the exit S |

### The chase rule (C4, `RelayBlocksInspect`)

- **No prompt** while the player can perceive the Relay: `Released && (State == Chase || State == BreakDoor || PlayerSeesRelay)`.
- **Never `SeesPlayer`.** In the v2 hunter it is silent, so a vanishing prompt would work as a Relay detector.
- Warning stages 1–2 are allowed and freeze with the world. Tested: Relay moved 0.0 mm and its warning clock held over a 5 s close-up; +1.000 s in the 1 s after.
- **v2 line, when the v2 fields land:** `OnMap && (WarnStage >= 3 || Spotting >= .5f || PlayerSeesRelay || State == BreakDoor)`.
- Red decides this rule (`../30_for_red.md` D1). The default is above.

## 4. Not in these diffs

| # | What | State |
|---|---|---|
| C8b | Esc pause from Playing also sets timeScale 0 (closes today's noise leak: a door still swings under Esc) | Optional, recommended. Red's call (D9). One line in `SetPhase` |
| C14 | Touch prompt kinds `UseKind.Read` / `UseKind.Back` | Lands with touch T2 |
| C20 | Harness gate | Covered by C4 (`autopilot || autoBaseline`) |
| C21 | First-key hand-off (D1.5) | **Waits for D1.5's dressed key rig.** Re-checked against main's `FrontRoomsDoorShot.BeginKeyPickup` / `PlaceKey`: while a close-up of the held key is active (`FrontRoomsInspect.Active`, until `HeldReturned`), the DoorShot must hold `keyT` and skip `PlaceKey`; `E · EXAMINE KEY` shows in the held window behind `CanInspectHeld()`. Exact diff after D1.5 |
| C22 | Door-sign close-up | **Not in v1** (signs read in place). Only if Red picks it (D7) |
| C23 | Level Designer walker previews poses | Optional |
| C24 | `FrontRoomsSettings.TextTranscripts` (off by default) | With 平面视觉's transcript panel (G9) |
| C26 | CRT screen ads: skip their own E ray and pause every `VideoPlayer` while `InspectingNow` or Paused | **Blocking before any build where a CRT can sit in the close-up ray.** Owner of `Media/FrontRoomsScreenVideo.cs` unclear (D11) |

Later, not now: a `panel` target with a cursor layer for the computer screens (系统设计's computer UI track, `research/computer_ui/`). `InspectKind.Panel` already exists; the cursor layer is not built.

## 5. Tests

Run in a clone (never in Red's open project). The test files arrive with step 2 (`Assets/Editor/Rendering/Inspect/`).

| After | Run | Pass (2026-10-14 numbers) |
|---|---|---|
| Step 1 | `FrontRoomsCameraRigTests.RunBatch`, `FrontRoomsGlassShotTests.RunBatch`, `FrontRoomsDoorShotTests.RunBatch` | 33, 20, 191 passed: identical to main without the step |
| Step 1 | Roslyn compile of main + step 1 | 0 errors (macOS, iOS) |
| Step 2 | `bash …/apply/inspect.sh` | `DRY RUN OK`, then `--apply` prints `MANIFEST OK`, then a second dry run prints `ALREADY APPLIED` |
| Step 2 | `FrontRoomsInspectSuites.RunBatch` (EditMode `FrontRoomsInspectTests`) | 105 / 105, including the rig contract checks (AT-11: full pose at 50 % and 100 %, ×1.5 times, a cut at Off, sine peak 1.57 × average, 0.15 / 0.40 s ends, `CancelShot` shortens both, `ShotEnded` once, `fovMin` for Inspect only, 16-hit buffer) |
| Step 3 | `FrontRoomsInspectPlaytest.RunBatch` (the real game, seed 516574485, start door opened like a player) | 116 / 116 |
| Step 3 | `FrontRoomsInspectPlaytest.RunNeutralityBatch` | PASS: a 10 s and a 0.25 s close-up give the same state at 40 s game time (player (257.590, 25.900), Relay (275.633, 67.500) Wander 6.100, 1,557 lamps, 115 doors, elapsed 40.016) |
| Step 3 | Map suites: map interaction, fixture tick, captions, relay nav, map verification, level designer, door shot, 20-seed map signatures | Identical to main without EXAMINE (relay nav and map verification differ only in timing fields; map interaction keeps main's own 2 errors / 4 exceptions in both) |
| Step 3 | Play 120 s, console | 0 new errors or exceptions |

Expected behaviour after step 3, in one line each:
- Walk to 1.6 m of the placard and within 60°: `E · READ PLAN`.
- E: the world freezes on that frame; the camera reaches the plan in 0.43 s.
- E, S or Esc: control returns at once; rotation home in 0.133 s, position in 0.400 s.
- During a chase, a door break, or with the Relay in view: no prompt, and E does what it did before.
