# Settings

Built 2026-10-24 (settings line 设置菜单与画质预设, on Red's word in that chat: "目前我希望你把这个做到当前的build里"). One settings system on every platform: the same tabs, rows and words on Mac, Windows, iPhone, iPad and both iPhone Duo displays. Research, the frame-rate numbers and the IA: [research/interaction/settings/](research/interaction/settings/README.md). Figma: section `3688:7842` on page 2099:76.

## Desktop (Mac, Windows)

`Esc` pauses and opens the sheet at once: on the desktop the sheet **is** the pause, with no PAUSED card in between, and leaving it resumes the run (Red, 2026-10-24, in the 交互设计 chat). The old card's content lives in the sheet: ESC RESUME is the RESUME button (the old BACK) and `Esc`; R RESTART is the RESTART button and `R`, which asks first (RESTART? and R AGAIN TO RESTART in the key line; a second `R` or click within the window restarts); the controls legend became CONTROLS › KEYBOARD & MOUSE (Red, 2026-10-29), a second-level page: the keyboard and mouse drawn in columns 1–8 with every action on its key, where the player remaps MOVE (W A S D), SPRINT, USE, RESTART and ZOOM (Esc, the wheel, the mouse look and START stay fixed; the arrows and Right Shift stay locked second keys; HOLD E is still the BREAK GLASS row, now named with the bound USE key); see `INTERACTION_DESIGN.md` › Key map. O SETTINGS is gone. The sheet is drawn like the handheld one (the paused frame frosted under black at 86 %; small labels at 75 % paper; RESUME and RESTART keyline buttons, yellow only for focus, per 平面视觉's review `research/desktop_ui/SETTINGS_KIT_REVIEW_1024.md`): SETTINGS, the tabs, the list in columns 1–7 and the focused row's explanation in columns 9–12 (`FrontRoomsDesktopSettingsView`).

| Key | Does |
|---|---|
| `Q` / `E` (or `Tab`), a click on a tab | GRAPHICS · COMFORT · CONTROLS · ASSIST |
| `↑ ↓` (`W S`), hover | choose a row (the 4 px yellow bar; its explanation on the right) |
| `← →` (`A D`) | one step: a segment, a slider stop, a switch |
| `Enter`, a click on the row | flips a switch, runs RESET (press twice) |
| click a segment, click or drag a slider | sets it |
| `Backspace` | the row back to its default |
| `Space`, a click on the preview (GRAPHICS) | the game full screen at the new settings; `Space`, a click or `Esc` comes back |
| `Esc`, RESUME | out of the preview first, then closes the sheet and resumes the run |
| `R`, RESTART | asks first (RESTART?, R AGAIN TO RESTART); a second one within the window starts a new run |
| `H`, `T` | shortcuts kept: HDR RENDER, THREAT READOUT |

## Handheld (iPhone, iPad, Android, iPhone Duo)

The pause button opens the black frosted sheet: on phones too the sheet is the pause (Red, 2026-10-24), with RESUME (the old BACK) and END RUN in its top-right corner; END RUN asks END THIS RUN? (CANCEL · RESTART, QUIT TO TITLE). The touch legend is CONTROLS › GESTURES (`TOUCH_CONTROLS.md` §3.1, §6): the tabs sit in the header beside SETTINGS; one list per tab. Sheets 700 pt wide or more (iPhone, iPad, the Duo's inner display) explain the touched row in a column on the right, with the live preview on GRAPHICS; narrower ones (the Duo's outer display) open the explanation under the touched row. A tap on the preview (or on the explanation's preview line) shows the game full screen; a tap anywhere comes back.

## Rows

A control per kind of row: SWITCH for on/off (the word always shown), SEGMENTS for a few named choices, a SLIDER for anything ordered by amount (Red, 2026-10-24: "这里放这些数字有些不知所云…直接放更简单直的horizontal scroll bar"), a meter for the measured frame rate. A changed row shows a small dot before its control.

| Tab | Row | Control | Values (default first) | Stored as (`PlayerPrefs`) |
|---|---|---|---|---|
| GRAPHICS | QUALITY | segments | desktop LOW · MEDIUM · HIGH · **ULTRA** · CUSTOM; handheld BATTERY · BALANCED · **QUALITY** · ULTRA · CUSTOM (Red, D2 · C) | `FrontRooms.Quality.Preset` |
| | FRAME RATE NOW | meter | measured FPS and ms against the target | — |
| | RESOLUTION | segments | AUTO · FIXED | `FrontRooms.Quality.Resolution` |
| | RENDER SCALE (FIXED only) | slider | 50–100 % in 5 % steps | `FrontRooms.Quality.RenderScale` |
| | UPSCALING (below 100 % only) | segments | SHARP (FSR 1) · SOFT (bilinear) | `FrontRooms.Quality.Upscaling` |
| | FRAME RATE | segments | ProMotion handhelds (iPhone Pro, iPhone Duo, iPad Pro) 30 · 40 · **60** · 120 FPS (Red, D4 · B; Enable ProMotion Support on); other handhelds 30 · **60** FPS; desktop **VSYNC** · 30 · 60 · 120 · MAX | `FrontRooms.Quality.FrameRateFps` (frames per second) |
| | ANTI-ALIASING | segments | HIGH (MSAA 4×) · LOW (2×) · OFF | `FrontRooms.Quality.AntiAliasing` |
| | AMBIENT OCCLUSION | segments | HIGH (full res) · LOW (half res) | `FrontRooms.Quality.AmbientOcclusion` |
| | SHADOWS | segments | HIGH · MEDIUM · LOW | `FrontRooms.Quality.Shadows` |
| | BLOOM | segments | HIGH · LOW | `FrontRooms.Quality.Bloom` |
| | HDR RENDER | switch | ON · OFF (SDR) | `FrontRooms.Display.HDR` |
| | LOW POWER MODE (iPhone, iPad) | segments | FOLLOW · IGNORE | `FrontRooms.Quality.LowPower` |
| | RESET GRAPHICS | button, press twice | the device's default | — |
| COMFORT | CAMERA MOTION | slider | 100 % · 50 % · OFF (iOS Reduce Motion → OFF at first launch) | `FrontRooms.Comfort.CameraMotion` |
| | REDUCE FLASHING | switch | OFF · ON | `FrontRooms.Comfort.ReduceFlashing` |
| CONTROLS | BREAK GLASS | segments | HOLD E (USE) · TAP E (USE) | `FrontRooms.Input.TapToBreak` |
| | touch rows | see `TOUCH_CONTROLS.md` §6 | | `FrontRooms.Touch.*` |
| ASSIST | CAPTIONS | switch | OFF · ON | `FrontRooms.Assist.Captions` |
| | THREAT READOUT | switch | OFF · ON (off by default, Red 2026-10-03; named THREAT READOUT 2026-10-29, 叙事设计: the player text never says "relay") | `FrontRooms.Assist.RelayReadout` (key name kept) |

What the comfort and assist rows do is unchanged: CAMERA MOTION scales every close-up pose, shake, dip and FOV change; REDUCE FLASHING turns lamp strobes into soft dips; BREAK GLASS TAP banks 0.35 s per tap (at most one every 0.3 s); CAPTIONS caption the sounds that hunt you; THREAT READOUT shows the hunter's state, its distance (the HUD calls it IT) and the run's tier (`IT  23 M  /  TIER 2`; the tier rises with new ground explored and is shown nowhere else, Red C04 Q5c).

## Presets (`FrontRoomsQuality`)

| Detail | LOW (handheld: BATTERY) | MEDIUM (BALANCED) | HIGH (QUALITY) | ULTRA (as shipped) |
|---|---|---|---|---|
| Resolution | handheld AUTO, 2.0 Mpx; desktop 70 % | handheld AUTO, 3.2 Mpx; desktop 85 % | AUTO, 3.2 Mpx (desktop 100 %) | 100 % |
| Frame rate | handheld 30 | 60 | 60 | 60 / desktop VSYNC |
| Anti-aliasing | OFF | LOW | HIGH | HIGH |
| Ambient occlusion | LOW | LOW | HIGH | HIGH |
| Shadows | LOW (25 m, 1 cascade, 1024 maps) | MEDIUM (additional-light atlas 2048) | HIGH | HIGH |
| Bloom | LOW | LOW | HIGH | HIGH |

- **RESOLUTION AUTO** gives every handheld screen the same rendered-pixel budget, recomputed on every fold, unfold and rotation: the iPhone Duo's inner display (5.0 Mpx) renders at 80 %, an iPad 11" at 90 %, the outer display and phones at 100 %. Probe v3 (2026-10-24): that brings the inner display back to about the outer display's cost; render scale is the one detail that clearly moves the frame.
- **The shipped look is the reference.** A value equal to the URP asset's own is never written, so the desktop at ULTRA draws exactly as before. Runtime writes to `Assets/Settings/FrontRooms_URP.asset` are undone when play stops (the Editor keeps the file as it was). SSAO's off variant is stripped from builds, so AO has LOW (half resolution) and HIGH only. BLOOM LOW is a runtime override volume (priority 100) that switches off high-quality filtering only.
- **Low Power Mode** (`UnityEngine.iOS.Device.lowPowerModeEnabled`, polled every 2 s): FOLLOW holds the game at 30 FPS and LOW while it is on, without changing what the player picked.
- **Frame rate on ProMotion** (Red 2026-10-24, D4 · B): `appleEnableProMotion: 1` lets an iPhone run above 60 Hz (Unity writes `CADisableMinimumFrameDurationOnPhone`). 40 FPS is 120 ÷ 3, a steady step; iOS drops 120 in Low Power Mode or when hot. Not yet tested on a device (`RED_ACTIONS.md`).
