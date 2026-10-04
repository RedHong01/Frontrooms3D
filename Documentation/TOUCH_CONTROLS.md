# Touch controls · iOS + Android

Status: **design proposal, 2026-10-03** (interaction-design chat "iOS 和 Android 触控设计"). Nothing is implemented. Red asked for research, a touch UI in the desktop UI's design language, and a design plan in Figma.

- Figma: file `0tCbAiVUlrPId3RWd9LRif`, page 2099:76, section **FRONTROOMS · TOUCH CONTROLS · iOS + ANDROID** (`2528:5403`) at x 33937, y 2000.
  - Slides TC01–TC13 (1920×1080, P1 deck system).
  - `TK · Touch kit masters · 1x pt` (2528:5495): the touch components.
  - `TS · Screen masters · 1x pt` (2530:5061): every screen as a component, 1 Figma px = 1 pt (iOS) / 1 dp (Android).
- Research: `Documentation/research/touch/01_shipped_games.md` (how shipped iOS/Android games map first-person verbs) and `02_guidelines_patterns_unity.md` (Apple HIG / WWDC, Android, ergonomics, accessibility, Unity facts). Media is URL-only until Red approves downloads; slides TC02–TC04 hold `media:<key>` slots.

## 1. Rules this design keeps

1. **Desktop does not change.** Mobile is a separate track, like WebGL ([WEBGL_BUILD.md](WEBGL_BUILD.md); Red's rule, 2026-10-02). Touch, haptics and the mobile render tier compile only for `UNITY_IOS || UNITY_ANDROID`; Editor Play Mode and Mac/Win/WebGL keep reading the same keys and mouse.
2. **Same design language.** Every touch element is built from parts the desktop HUD kit already has (UI MOCKUP 2256:8 · UI02):
   - the 4 px yellow rule `#F4DF3B` marks what matters;
   - the keycap outline (2 px paper `#F4F1E8`) becomes the USE button and the pause button;
   - the hold ring (paper @30 % track + 3.5 px yellow arc) becomes the stick's sprint socket and USE's hold progress;
   - the yellow key chip (`Overlay / Key chip`) becomes the tappable chip; a pressed USE fills yellow the same way;
   - faces stay Bayon (labels), IBM Plex Mono (meta), Source Serif 4 (zone, hint).
3. **World first.** The HUD keeps to the top band and around the dot. Controls are ghosts until touched; USE exists only while the dot is on something.
4. **Thumbs don't scale, grip does.** Control sizes are set in pt/dp, not HUD units: phone primary hit areas ≈ 15 mm (Xbox Accessibility Guidelines 107; USE hit 88 pt = 14.6 mm), iPad controls start at 1.4× (XAG: 24 mm on tablets). Type and HUD follow the ramp in §4.

## 2. Input mapping

| Desktop | Verb | Touch |
|---|---|---|
| WASD | Move | Left thumb, floating stick: it centres where the thumb lands (left half, below the top band). |
| Mouse | Look | Right thumb drags anywhere that isn't a button. |
| Shift (hold) | Sprint | **Socket latch**: slide the thumb up into the socket above the stick (Ø40 ring centred 92 pt up, a 12 pt gap past the base ring) and hold it there ≥ 150 ms; sprint then stays on while the thumb stays in the upper half; pulling back or lifting ends it. Light haptic tick + yellow socket ring on engage. Never an edge push: Alien: Isolation's edge auto-sprint broke stealth (research 01 §3), and here sprint is loud. Optional SPRINT toggle button (setting). |
| E | Door / key | **USE** button, shown only while `UpdateAim` has a target; its label is the verb (OPEN / SHUT / TAKE / LOCKED). **Tapping the door itself also works** (Apple HIG: tap objects instead of a selection button): a touch < ~200 ms that moves < ~3 mm on the look side, raycast from the tap point within reach. No auto-open: shutting a door on the Relay is a choice. |
| Hold E | Break glass | Hold USE; the ring fills **outside** the thumb (r 46 pt) and the bar under the dot. TAP mode (existing setting) = repeated taps on USE. |
| Walk into frame | Climb | Unchanged (stick into the broken frame). |
| S | Cancel a shot | Pull the stick back (same meaning as S). |
| Esc | Pause | Pause button top-right; Android back (target API 36 no longer delivers `onBackPressed`/`KEYCODE_BACK`: register a predictive-back `OnBackInvokedCallback`, test how Unity surfaces it); app losing focus (already `OnApplicationFocus`). |
| O | Settings | SETTINGS chip on the pause screen; rows are tappable. |
| R | Restart | TRY AGAIN chip on the caught screen; RESTART on the pause screen asks to confirm (XAG 115: no one-tap destructive actions). |
| Space / Return | Start | Tap anywhere on the title; a TAP TO START chip fades in after ~3 s idle (touch only). |
| H, T (settings shortcuts) | — | Not needed: rows are tappable. |

A finger that starts on USE may keep dragging to look (so the dot can stay on a pane while holding). A finger that starts in the look zone never presses USE (presses register on touch-down only).

## 3. Layout (phone, iPhone 16/17 Pro landscape, 874×402 pt)

Safe-area insets in landscape: L 62, R 62, B 21, T 0 (verify per model at runtime via `Screen.safeArea`).

| Element | Position (pt) | Size |
|---|---|---|
| Zone block (rule + meta + zone name) | top-left (78, 16) | rule 3×40 |
| Key held | under the zone block (78, 72) — moved off the bottom-left, which belongs to the left thumb | 150×22 |
| Pause | top-right (762, 12) | 32 visual / 44 hit |
| Crosshair | centre | Ø12 white |
| Prompt / hold bar / stamina | under the dot: +13 / +47 / +21 (sprint, no prompt) | bar 100×3, stamina 5 × 20×5 |
| Stick (resting ghost) | centre (150, 290) | base Ø120, sprint socket Ø40 centred 92 up, thumb Ø52 |
| USE | centre (740, 262) | Ø72 visual / Ø88 hit, hold ring r 46 |
| Hint card | bottom centre, 452 wide | dark card + yellow rule |
| Top band | y 0–64 | read only; the only control is pause |

Android 20:9 (915×412 dp): insets from `WindowInsets`; keep the stick's touch area ≥ 24 dp from the long edges, where the back gesture lives (both edges in landscape), and exclude at most the stick band from system gestures (Android limits exclusion to 200 dp per edge). iPad 11" (1180×820 pt): controls at 1.4× (stick base Ø168, USE Ø101 / hit Ø123), same corner positions; HUD uses the tablet ramp; margins 32–40 pt. iPadOS 26+ windowing and foldables mean the layout must re-flow on every size change.

## 4. Type ramp (handheld) — reviewed by 平面视觉, 2026-10-03

Rule: up to 24 px, keep the desktop's **angular** size at each device's viewing distance — phone ×0.85 (≈30 cm; 1 pt ≈ 0.166 mm on an iPhone 16 Pro, measured ×0.83), iPad ×0.95 (≈40 cm; 1 pt ≈ 0.193 mm, ×0.96) against a 24" 1080p monitor at 60 cm (1 px = 0.277 mm). Larger type is compressed so the zone name never covers a 402 pt-tall screen. Figma text styles `Touch/Phone/*`, `Touch/Tablet/*`.

| Role | Face | Desktop px | Phone pt | iPad pt |
|---|---|---|---|---|
| Meta | IBM Plex Mono | 13/16 | 11/14 | 12/15 |
| Label / prompt | Bayon (upper) | 20/20 | 17/17 | 19/19 |
| Caption (subtitles) | Source Serif 4 | 20/23 | 17/20 | 19/22 |
| Context / hint | Source Serif 4 | 24/26 | 20/22 | 23/25 |
| Zone name | Source Serif 4 | 50/42 | 26/26 | 36/32 |
| Screen title | Bayon | 88/80 | 48/44 | 64/58 |
| Key numeral | Courier Prime Bold | 25 | 21 | 23 |

Review rules (平面视觉):
- **Tracking 0** on every role: the HUD is Unity legacy `Text`, which has no letter-spacing. Revisit only if mobile moves to TextMeshPro.
- **11 pt is the floor** (iOS Caption 2 / Android label-small) on every device. Captions are accessibility text: ≥ 17 pt on phones.
- **Zone block lockup**: rule height = meta LH + zone LH (phone 14 + 26 = 40; iPad 15 + 32 = 47).
- **Numerals never in Bayon** in the HUD (it draws 1 as I and 0 as O). Key numbers use Courier Prime Bold at ×1.232 the label size (sized to Bayon's cap height); legacy Text sizes are integers. Settings values with digits use IBM Plex Mono (the HUD's number face) at the label size, phone 17 / iPad 19, so Plex's cap height (0.698 em) matches Bayon's (0.714 em); 15 only for a value alone in its own column (平面视觉).
- Figma previews Source Serif 4 with automatic optical size; Unity draws the variable font's default instance (opsz 20) at every size, so the 26/36 pt zone names preview slightly narrower and higher-contrast than devices. For pixel-true zone names, outline 20 pt text scaled up. Bayon's synthesised bold (threat state) can't be shown in Figma.

Unity mapping: a handheld `CanvasScaler` with 1 UI unit = 1 pt/dp; desktop keeps `ScaleWithScreenSize` 1920×1080. `Screen.dpi / 163` gives ≈ 0.94 pt per unit on current iPhones (≈153 pt/in), so read `UIScreen.nativeScale` through a small plugin (or accept UI ≈ 6 % small); Android uses `dpi / 160`.

Assets: phones are @3x, but `HUD_KeyGlyph` and the crosshair exist only at 1x/2x. Ask 游戏视觉 for @3x or vector sprites before T1.

## 5. Components (TK · Touch kit masters)

- `Touch / Stick` — State = Idle (ghost) / Walk / Sprint (thumb in the socket, yellow socket ring = Apple's sprint halo, label SPRINT) / Winded (muted, label WINDED).
- `Touch / Use` — State = Open / Shut / Take / Locked / Hold / Tap mode / Pressed.
- `Touch / Prompt` — Tap (mini-USE glyph replaces the keycap E) / Hold (ring glyph) / Locked (no glyph, muted).
- `Touch / Pause`, `Touch / Chip` (text property Label), `Touch / Sprint toggle` (Off / On / Winded).
- `HUD·T / Zone`, `HUD·T / Key`, `HUD·T / Hint card`, `HUD·T / Captions` (Size = Phone / Tablet), `HUD·T / Crosshair`, `HUD·T / Stamina`, `HUD·T / Hold bar`.
- Device overlays (iPhone island + home indicator, Android punch-hole + handle, iPad home indicator) and safe-area guides.

## 6. Settings (new TOUCH section, PlayerPrefs like `FrontRoomsSettings`)

| Row | Values (default first) |
|---|---|
| LOOK SPEED | 5 (1–10) |
| INVERT LOOK | OFF / ON |
| GYRO LOOK | OFF / WHILE TOUCHING / ALWAYS |
| STICK | FLOATING / FIXED |
| SPRINT | SOCKET / TOGGLE BUTTON |
| CONTROLS SIZE | 100 % (80–140) |
| CONTROLS OPACITY | 60 % |
| LEFT-HANDED | OFF / ON (swaps sides) |
| HAPTICS · GAMEPLAY | ON / OFF (door, glass, lock-on, caught) |
| HAPTICS · CONTROLS | ON / OFF (sprint socket, hold ring) — two switches like Alien: Isolation; hidden on iPad (no haptics) |
| INTERACTION ASSIST | OFF / SLOW (look gain ×0.5 within ~4° of a door handle or pane) |
| EDIT LAYOUT | opens the layout editor (T3) |

The existing BREAK GLASS row reads HOLD USE / TAP USE on touch. With iOS Reduce Motion on, CAMERA MOTION's first-launch default is OFF.

## 7. Haptics (event → pattern)

| Event (existing hook) | Pattern |
|---|---|
| Door opens (`OnDoorMoved`) | one light transient (≈0.5) at the latch |
| Locked rattle (`rattleJolts`, Rattle.Jolt1/Jolt2) | two firm transients on the camera jolts |
| Glass hold (`map.Hold` progress) | continuous 0.2→0.6, transients at ⅓ and ⅔, heavy 1.0 on `OnGlassBroken` |
| Sprint | light tick as the thumb lands in the socket; soft double when `winded` |
| Relay lock-on (pursuit redesign cue) | firm double pulse, chase only |
| Caught | 0.8 s heavy fade |

iOS: Apple's official `Apple.CoreHaptics` Unity plug-in (AHAP), checking `supportsHaptics` — **iPad has no haptics**, so every haptic must also exist as sound or picture (the ring, the bar). Android: `performHapticFeedback` constants first, `VibrationEffect` primitives only where `arePrimitivesSupported`; no buzzy fallback, never `Handheld.Vibrate`.

Ownership (agreed with 声音设计, 2026-10-03): this touch layer owns the haptic patterns and any AHAP files. Haptics fire from the **same game events** the sound listens to (DoorLatched, the rattle jolts, GlassCracked / GlassBroken, PlayerSprinting / PlayerWinded, Caught, the lock-on cue when it exists), never from FMOD/audio callbacks. Where a haptic must land on an audio transient (e.g. the crack's swap frame), 声音 documents the event time in AUDIO_CONTRACT.

## 8. Implementation plan (not started)

- **T0 Facade** — **Android blocker:** Unity 6.3 does not support Active Input Handling = *Both* on Android (the project is on Both). So the facade is built on **Input System actions** with today's desktop bindings, and the project switches to *Input System Package (New)* — a desktop-wide change that must feel identical (autopilot + Red's hands-on check); open question 8. `Assets/Scripts/Input/FrontRoomsInput.cs` (static): `Move`, `LookDelta`, `SprintHeld`, `UsePressed/UseHeld`, `BackPressed` (shot cancel), `PausePressed`, `StartPressed`, settings navigation. On desktop it returns exactly today's `Input.*` reads. `FrontRooms3DGame` (owned by 关卡设计) swaps its direct reads for the facade; acceptance = autopilot seeds 2554/20388 identical. Hooks as of 2026-10-03 (from 关卡设计; ping before editing, it keeps these names stable):
  - E goes through `EDown()` / `EHeld()` (which also carry an editor-only autopilot override): the facade goes inside those two, override untouched.
  - Stick input must land in `local`: the glass shot reads `moveIntent` (local.sqrMagnitude > .01) before any lock, so any stick movement ends the glass soft lock (1.10–1.30 s) and lets go of the pane between tap-mode strikes — intended.
  - S is read twice: as movement and as `rig.Consume(ShotInput.Back)` during a shot (cancel after 0.2 s). Pull-back on the stick feeds both; the coming door shots (unlock push-in) use the same cancel.
  - Touch look feeds the same place as the mouse: yaw/pitch (mouse ×2.1) and `lookDegrees` (|dx|+|dy| per frame), which ends the glass soft lock past 3°.
  - `FrontRoomsSettings.TapToBreak` may default ON for touch (open question 7).
- **T1 Touch layer** — `FrontRoomsTouchControls` (EnhancedTouch, finger ownership by touch-down zone, floating stick, look curve, USE bound to `UpdateAim`'s `aimed/aimedHold`, pause, safe area).
- **T2 Menus** — chips, tappable settings rows, tap to start; prompt strings swap the "E" token for the touch glyph (strings from `MapWorld.Describe`, owned by 关卡设计).
- **T3** — haptics, TOUCH settings, layout editor.
- **T4** — `FrontRooms3DBuild.BuildIOS/BuildAndroid` (landscape only, IL2CPP ARM64; iOS: defer **bottom-edge** system gestures during play only and leave `hideHomeButton` off, since hiding it disables deferral; Android: Unity ≥ 6000.3.13f1 for GameActivity 4.4 or the Activity entry point, target API 36 (required since 31 Aug 2026), immersive transient bars, `Application.targetFrameRate = 60` because Unity's mobile default is 30), mobile quality level + URP asset (visual/WebGL chats), device test on Red's iPad + a phone — **Unity's Device Simulator fakes only one finger and no gyro**, so two-thumb input is only tested on devices (iPhone with Dynamic Island, iPad 11", a 20:9 Android at API 36). FMOD (声音, ping when T4 starts): a Mobile platform in the generated Studio project with mobile encoding, FMOD for Unity's iOS/Android entries pointed at the Mobile bank folder, desktop settings untouched, init + captured run verified on device or simulator.

## 9. Open decisions for Red

1. Sprint default: the socket latch (recommended) or a button?
2. Gyro look off by default (recommended)?
3. Frame target: 60 fps on a recent iPhone, or 30?
4. Landscape only (recommended)?
5. Bluetooth/MFi controllers on mobile in v1?
6. TestFlight / internal testing only, or the stores?
7. Break glass on touch: HOLD USE by default (recommended: a held button is natural on glass; XAG asks for holds under 2–3 s) or TAP USE?
8. Android needs Unity's new Input System project-wide: migrate desktop input too, verified to feel identical (recommended), or ship iOS first and decide later?
