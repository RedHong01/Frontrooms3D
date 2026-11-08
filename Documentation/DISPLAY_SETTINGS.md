# Settings

Built 2026-10-24 (settings line 设置菜单与画质预设, on Red's word in that chat: "目前我希望你把这个做到当前的build里"). One settings system on every platform: the same tabs, rows and words on Mac, Windows, iPhone, iPad and both iPhone Duo displays. Research, the frame-rate numbers and the IA: [research/interaction/settings/](research/interaction/settings/README.md). Figma: section `3688:7842` on page 2099:76.

## Desktop (Mac, Windows)

`Esc` pauses and opens the sheet at once: on the desktop the sheet **is** the pause, with no PAUSED card in between, and leaving it resumes the run (Red, 2026-10-24, in the 交互设计 chat). The old card's content lives in the sheet: ESC RESUME is the RESUME button (the old BACK) and `Esc`; R END RUN is the END RUN button and `R`, which end the run and go straight to the title with no question (Red, 2026-10-30; on the caught card the same key is TRY AGAIN); the controls legend became CONTROLS › KEYBOARD & MOUSE (Red, 2026-10-29), a second-level page: the keyboard and mouse drawn in columns 1–8 with every action on its key, where the player remaps MOVE (W A S D), SPRINT (Ctrl), SLOW (Shift), USE, END RUN (R) and ZOOM (Esc, the wheel, the mouse look and START stay fixed; the arrows, Right Ctrl and Right Shift stay locked second keys; HOLD E is still the BREAK GLASS row, now named with the bound USE key); see `INTERACTION_DESIGN.md` › Key map. O SETTINGS is gone. The sheet is drawn like the handheld one (the paused frame frosted under black at 86 %; small labels at 75 % paper; RESUME and RESTART keyline buttons, yellow only for focus, per 平面视觉's review `research/desktop_ui/SETTINGS_KIT_REVIEW_1024.md`): SETTINGS, the tabs, the list in columns 1–7 and the focused row's explanation in columns 9–12 (`FrontRoomsDesktopSettingsView`).

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
| `R`, END RUN | ends the run and goes straight to the title (no question since 2026-10-30) |
| `H`, `T` | shortcuts kept: HDR RENDER, THREAT READOUT |

## Handheld (iPhone, iPad, Android, iPhone Duo)

The pause button opens the black frosted sheet: on phones too the sheet is the pause (Red, 2026-10-24), with RESUME (the old BACK) and END RUN in its top-right corner; END RUN goes back to the title at once, as on the desktop (Red, 2026-10-30: "在回退标题前不用确认一下"; from 10-24 it asked END THIS RUN? with CANCEL · RESTART and QUIT TO TITLE). The touch legend is CONTROLS › GESTURES (`TOUCH_CONTROLS.md` §3.1, §6): the tabs sit in the header beside SETTINGS; one list per tab. Sheets 700 pt wide or more (iPhone, iPad, the Duo's inner display) explain the touched row in a column on the right, with the live preview on GRAPHICS; narrower ones (the Duo's outer display) open the explanation under the touched row. A tap on the preview (or on the explanation's preview line) shows the game full screen; a tap anywhere comes back.

## Rows

A control per kind of row: SWITCH for on/off (the word always shown), SEGMENTS for a few named choices, a SLIDER for anything ordered by amount (Red, 2026-10-24: "这里放这些数字有些不知所云…直接放更简单直的horizontal scroll bar"), a meter for the measured frame rate. A changed row shows a small dot before its control.

| Tab | Row | Control | Values (default first) | Stored as (`PlayerPrefs`) |
|---|---|---|---|---|
| GRAPHICS | QUALITY | segments | desktop LOW · MEDIUM · HIGH · **ULTRA** · CUSTOM; handheld BATTERY · BALANCED · **QUALITY** · ULTRA · CUSTOM (Red, D2 · C) | `FrontRooms.Quality.Preset` |
| | FRAME RATE NOW | meter | measured FPS and ms against the target | — |
| | RESOLUTION | segments | AUTO · FIXED | `FrontRooms.Quality.Resolution` |
| | RENDER SCALE (FIXED only) | slider | 50–100 % in 5 % steps | `FrontRooms.Quality.RenderScale` |
| | UPSCALING (below 100 % only) | segments | SHARP (FSR 1; STP on a handheld under AUTO once the surfaces write motion vectors) · SOFT (bilinear) | `FrontRooms.Quality.Upscaling` |
| | FRAME RATE | segments | ProMotion handhelds (iPhone Pro, iPhone Duo, iPad Pro) 30 · 40 · **60** · 120 FPS (Red, D4 · B; Enable ProMotion Support on); other handhelds 30 · **60** FPS; desktop **VSYNC** · 30 · 60 · 120 · MAX | `FrontRooms.Quality.FrameRateFps` (frames per second) |
| | ANTI-ALIASING (hidden while STP draws) | segments | HIGH (MSAA 4×) · LOW (2×) · OFF | `FrontRooms.Quality.AntiAliasing` |
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
- **SSAO samples** (技术美术调研 `research/tech_art/20_ranked_changes.md` §3.3, Red 2026-11-04 "全部按推荐"): Medium (8 samples instead of 12); measured 0.001-0.046 % of pixels moved, the noise floor, for about a third of the AO pass. Both renderers have it (desktop: 游戏视觉's yes, 2026-11-04; `FrontRoomsRenderSetup.cs` sets the same). Set in the renderer asset, not at runtime: the build strips the sample counts no renderer uses.
- **Handheld renderer** (same doc §3.13): iPhone and iPad draw with `Assets/Settings/FrontRooms_URP_Handheld.asset` and its renderer, the iPhone's quality level `Handheld` (excluded on Standalone, WebGL, Windows Store; iPhone default 2 → 6). `FrontRoomsQuality.ChooseRenderer` swaps it in at boot when `FrontRoomsHandheld.Active` (the Editor's handheld preview too, swapped back when play stops); the desktop asset is never written for a handheld. Differences from the desktop pair, nothing else: SSAO from Depth, After Opaque (URP then needs no depth prepass on a phone; Glass RT is Mac-only), Medium samples, and a softer AO (intensity 0.8, radius 0.12 m against the desktop's 1.6 and 0.45 m: 游戏视觉's phone SSAO contract, option C, Red 2026-11-07 "全部按推荐"; After Opaque multiplies the lit image and crushed recesses such as the louvre of a back-of-house door and the foot of a pressed door lite, measured louvre at 3 m 0.0785 against the desktop's 0.0748; open corners read a little flatter on phones, a device A/B is in the combined test); one renderer (no water mirror renderer) and the opaque copy at half resolution as before; STP as the asset's upscaler, only so the build keeps STP's compute shaders (URP strips them unless an asset in the build has STP), written to Auto at 100 % because URP runs STP and FSR even at full resolution. Bloom on a handheld starts at a quarter of the resolution with `maxIterations` 5 instead of 6 (the smallest step stays 1/64 of the frame, so the glow reaches as far; about 75 % fewer bloom pixels), through the same override volume. `FrontRoomsHandheldPipeline.Sync` (menu FrontRooms › Rendering › Sync handheld URP asset; also called by `FrontRoomsRenderSetup.SetUp`) copies every other value from the desktop pair. After Opaque can darken more than the depth-normals path: a device A/B is in the combined test.
- **STP** (same doc §3.13): on a handheld under RESOLUTION AUTO below 100 % with UPSCALING SHARP, outside Low Power Mode, STP replaces FSR 1 and MSAA is off (STP does not run with MSAA; ANTI-ALIASING is hidden then). It needs the `MotionVectors` pass in `FrontRooms/Surface` (游戏视觉, in main since 2026-11-04 22:4x, row 7). Unity reports a shader's passes only after it has drawn once, so on a handheld the runner asks again twice a second until it sees the pass, then applies: SHARP turns into STP a moment after the first frames. FIXED keeps FSR 1. Desktop never; STP there would be an opt-in TEMPORAL choice only.
- **Handheld LOD bias** (tech_art 20 §3.7, row 7, with LOD cross-fade): the `Handheld` level runs `lodBias` 1, the distances the kits were authored for (was 0.7: a guest chair switched at 3.5 m instead of 5 m). Desktop ULTRA keeps 2, the Editor and WebGL (High) 1. Cost on a phone: more triangles for the longer stretch at the upper level, same draw count; a device look is in the combined test.
- **Pool water** (游戏视觉's contract `W/apply/pool_water_contract`, 2026-11-04): the desktop asset lists a second renderer, `FrontRoomsWaterReflection_Renderer` (no features, so no SSAO in the oblique mirror), used only by the water's mirror cameras; the default stays index 0. Its opaque copy is at full resolution (`m_OpaqueDownsampling` 0) for the water's refraction on desktop HIGH and ULTRA; the glass crack mirrors get the same sharper copy while a pane is held or cracked. The water reads the preset and never writes it (`FrontRoomsWaterPlanar.CurrentTier`): ULTRA a mirror at ½ resolution every frame, up to two water heights; HIGH ⅓ (¼ above 1440 rows) every second frame, one height; MEDIUM, LOW, Low Power Mode, phones and WebGL the probe; CUSTOM keeps the preset it started from. No WATER REFLECTIONS row for now (`FrontRoomsWaterPlanar.ForceTier` is the hook). Inert until a `FrontRoomsWaterSurface` is in a scene.
- **Low Power Mode** (`UnityEngine.iOS.Device.lowPowerModeEnabled`, polled every 2 s): FOLLOW holds the game at 30 FPS and LOW while it is on, without changing what the player picked.
- **Frame rate on ProMotion** (Red 2026-10-24, D4 · B): `appleEnableProMotion: 1` lets an iPhone run above 60 Hz (Unity writes `CADisableMinimumFrameDurationOnPhone`). 40 FPS is 120 ÷ 3, a steady step; iOS drops 120 in Low Power Mode or when hot. Not yet tested on a device (`RED_ACTIONS.md`).
