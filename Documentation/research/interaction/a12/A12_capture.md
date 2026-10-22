# A12 capture: double-acting doors in the clone (2026-10-22)

First-person capture of the A12 door rule (doors swing away from the player), recorded in the private clone `/Users/redwang/FrontRoomsVisualWork/proj_a12` before the change lands in main. The leaf is today's cube placeholder; the D1.6 double-acting models are 游戏视觉's.

- **Video:** `/Users/redwang/FrontRoomsInteractionWork/a12/capture/A12_doors_clone_20261022.mp4` (20.5 s, 1280 × 720, 30 fps, H.264; kept outside the repo).
- **Frames:** `W/proj_a12/Verification/a12_capture/frames/` (615 JPG), with `segments.txt` and `capture_log.txt`.
- **Harness:** `W/proj_a12/Assets/Editor/Input/FrontRoomsA12DoorCapture.cs` (clone only, never landed). It plays `Assets/Scenes/FrontRooms3D.unity`, starts a run through the title, holds the Relay until the last segment, and calls the game's own `UseDoor` (the E path) on one map door.
- **Verification log:** VL348, the two sheets in `images/`.

| Time | Frames | What happens |
|---|---|---|
| 0.0–3.9 s | 0–115 | E from the near side: the leaf swings away (it had last gone toward the player); walk through |
| 3.9–6.4 s | 116–192 | Turn round on the leaf's side, inside its sweep, and shut: the game's step aside (kept by Red) |
| 6.4–12.3 s | 193–368 | E from the far side: the leaf turns over and swings away again; walk back |
| 12.3–15.4 s | 369–461 | Keys on: the key turned from the side the leaf last went to; the head dip; the leaf opens away at 1.00 s |
| 15.4–20.5 s | 462–614 | The Relay breaks the door from the far side: the leaf bursts toward the player, away from the Relay |

The capture log confirms each beat: every open turned the swing side away from the opener (−1 → +1, +1 → −1), the key turn answered `KeyTurn` with the swing turned away, and the break left the leaf on the player's side ("toward the player: True").
