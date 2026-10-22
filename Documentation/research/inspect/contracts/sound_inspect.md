# EXAMINE: contract for the sound chat (声音设计)

From the visual chat (游戏视觉), hand-off stage, 2026-10-14.
A change request for files the sound chat owns. The visual chat never edits them in Red's project.

**What it is:** while the player reads something in a close-up, the world is paused (`Time.timeScale = 0`). The mix should freeze with it, but still sound like the place. Design: `../10_design.md` §4.3 and §7.3.

**What went wrong in v1 (verifier, 2026-10-08):** nothing in the audio listened to the close-up, so stingers and the heartbeat kept going and Tension drifted (0.2354 → 0.2396 in 5 s).

## 1. The diff

`sound_inspect.diff`: code only. No FMOD Studio change, no new event, no new AudioSource, no `AudioListener.pause`.

| File | Base sha1 on main 0a9ee7f (checked 2026-10-14 17:1x) |
|---|---|
| `Assets/Scripts/Audio/FrontRoomsSoundDirector.cs` | `aa64b6ba9caf3fff8f420ca2f25f0ca4d611e4d2` |
| `Assets/Scripts/Audio/FrontRoomsSoundIds.cs` | `dbf9b794d7e1f974f50c995a2589c8799a80a90c` |
| `Documentation/AUDIO_CONTRACT.md` | `3b1e29503898a1f6ae8b43124fc2abd1f8c2ae7a` |

- **Order:** it needs `FrontRooms3DGame.Inspecting` from the map chat's `map_inspect_3_game.diff` (C2), so it is the last step of the merge, after the map chat's step 3 (`map_inspect.md` §1).
- **Apply** from the project root: `patch -p1 --dry-run < sound_inspect.diff`, then `patch -p1 < sound_inspect.diff`. It dry-runs clean on main today (no fuzz, no offsets).

## 2. Events (what to listen to)

| Event / member | Fires | Use |
|---|---|---|
| `FrontRooms3DGame.Inspecting(bool)` | true at the E press; false as control returns (the camera starts home); false on teardown (R restart, Play stopped) | **The only key for the snapshot (S1)** |
| `FrontRooms3DGame.InspectingNow` | true between those two | a query |
| `FrontRooms3DGame.Paused(bool)` | in a close-up, only on focus loss or app pause; `Inspecting` stays true | S2 |
| `FrontRoomsCameraRig.ShotStarted / ShotEnded(ShotKind.Inspect, focus)` | `ShotEnded` fires 0.40 s **after** control returns, and never on an R restart | Foley hooks only, never the snapshot |
| `FrontRoomsInspect.Opened(target)`, `target.Sound` (`InspectSound` Paper / Plastic / Metal / Keys) | at the E press | Foley hook (proposed `event:/Foley/Player/InspectIn`) |
| `FrontRoomsInspect.HeldReturned` | the held key is back in the hand (C21, with D1.5) | Foley hook |

`ShotKind` is append-only: `Inspect` is appended. Read it by name.

## 3. The pause snapshot (S1, S2, S4)

On `Inspecting(true)`:

| Bus | Close-up | Why |
|---|---|---|
| `bus:/SFX/Foley` | paused | Steps and door Foley stop with the world |
| `bus:/SFX/Mechanism` | paused | Doors and locks are frozen |
| `bus:/Music` (Relay stingers) | paused | A stinger would give new information about a frozen Relay |
| `bus:/Subjective` (heartbeat, breath) | paused | The body is frozen too |
| `bus:/SFX/Relay` | held at −6 dB (0.501) | During a stage-1/2 warning the presence drone and coil stay: the threat is still there, with no new steps |
| `bus:/AMB` | held at −10 dB (0.316) | The pause still sounds like the place (SOMA's Safe Mode) |
| Tension, subjective layer | held (`UpdateGlobals`, `UpdateSubjective` skipped) | The director does not drift while time is stopped |
| Listener | attenuation from the eye (the camera's parent) | The camera travels about 1.2 m; distance should not change the mix |

- On `Inspecting(false)`: all of it is undone.
- **S2:** a focus-loss pause inside a close-up pauses everything as today. Its resume goes back to the close-up mix, not the open mix.

**Tested** in clone `W/proj_inspect` with main's FMOD banks (the verifier's harness, real move input, a stage-2 Relay warning): **17 / 17 pass.**
- At the commit frame: Foley, Mechanism, Music and Subjective paused; Relay 0.501; AMB 0.316.
- Over a 5 s hold, Tension stayed at 0.2354 (v1 drifted to 0.2396).
- Stingers and the heartbeat froze after one FMOD update (+21 ms).
- 0 new sound requests while paused. `Inspecting(true)` and `(false)` paired 2 / 2.
- Roslyn: 0 errors with the map contract and the visual package.
- Evidence: `W/inspect_work/fix/runs/v1_verify_sound/` (report, deep snapshot diffs), chart `../images/h_06_pause_summary_chart.jpg`.

## 4. Proposed, your call (not in the diff)

| # | What | Note |
|---|---|---|
| S3 | Foley `event:/Foley/Player/InspectIn` on `Opened`, `InspectOut` on `Inspecting(false)`, `KeyTurn` while a `Keys` object turns (the `AngularVelocity` parameter) | They play on the unscaled side of the snapshot. Paper for the placard |
| S5 | `AUDIO_CONTRACT.md` lines | In the diff: the `Inspecting` event, the bus rule, the Foley hooks, and "`ShotKind` is append-only" |
| — | The −6 dB and −10 dB values | Start values. Tune them by ear |
| — | A CRT ad playing in view during a close-up | Not yours: the screen fix (map contract C26) pauses every `VideoPlayer` |
