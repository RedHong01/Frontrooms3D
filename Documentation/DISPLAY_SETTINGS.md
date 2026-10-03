# Settings

While the game is paused, the player can change a few display, comfort, input and assist settings:

- Press `Esc` during play to pause.
- Press `O` to open **Settings**.
- `↑ ↓` (or `W S`) choose a row. `← →` (or `A D`) change it; `Enter` or `Space` cycle it (CAMERA MOTION wraps from 100% to OFF, where `← →` stop at the ends).
- Shortcuts: `H` switches HDR and `T` switches the Relay readout.
- `Esc` closes the panel. `R` does not restart the run while the panel is open.

| Row | Values | Stored as (`PlayerPrefs`) | Effect |
|---|---|---|---|
| HDR RENDER | ON / OFF (SDR) | `FrontRooms.Display.HDR` | The camera's render target. If the platform lacks Unity's HDR render texture format, the camera stays in SDR and the row reads OFF (SDR). |
| CAMERA MOTION | OFF / 50% / 100% (default) | `FrontRooms.Comfort.CameraMotion` | Scales every shot pose, shake, dip and FOV change of the camera rig. At OFF, shots play in place and look stays free; the objects still act. |
| REDUCE FLASHING | ON / OFF (default) | `FrontRooms.Comfort.ReduceFlashing` | The lamps lose their on/off strobe: a stutter or a failing ballast's dropout becomes a soft dip to 70%, and a dead tube no longer blinks (the lamp clocks run as before). Exposure flashes, CA pulses and lamp warnings are to read it as they land. |
| BREAK GLASS | HOLD E (default) / TAP E | `FrontRooms.Input.TapToBreak` | Tap mode banks 0.35 s of progress per tap (at most one every 0.3 s) and spends it in real time, so taps are never faster than holding. The prompt reads TAP E. Between taps the progress stays; looking away resets it. |
| CAPTIONS | ON / OFF (default) | `FrontRooms.Assist.Captions` | Read by the captions when they land (audit 2.11). |
| RELAY READOUT | ON / OFF (default) | `FrontRooms.Assist.RelayReadout` | The Relay's state and distance on the HUD. Off by default (Red, 2026-10-03). |

All values except HDR live in `FrontRoomsSettings` (`Assets/Scripts/FrontRoomsSettings.cs`), which other systems read. It raises `FrontRoomsSettings.Changed` after any change. The pause card's break-glass line follows the input setting.
