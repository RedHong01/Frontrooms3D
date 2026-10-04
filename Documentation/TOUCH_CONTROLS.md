# Touch controls · iOS + Android

Status: **design proposal, 2026-10-03** (interaction-design chat "iOS 和 Android 触控设计"). Nothing is implemented. Red asked for research, a touch UI in the desktop UI's design language, and a design plan in Figma.

- Figma: file `0tCbAiVUlrPId3RWd9LRif`, page 2099:76, section **FRONTROOMS · TOUCH CONTROLS · iOS + ANDROID** (`2528:5403`) at x 33937, y 2000.
  - Slides TC01–TC13 (1920×1080, P1 deck system).
  - `TK · Touch kit masters · 1x pt` (2528:5495): the touch components.
  - `TS · Screen masters · 1x pt` (2530:5061): every screen as a component, 1 Figma px = 1 pt (iOS) / 1 dp (Android).
- Research: `Documentation/research/touch/` (01 shipped games, 02 guidelines/patterns/Unity, SOURCES.md).

## 1. Rules this design keeps

1. **Desktop does not change.** Mobile is a separate track, like WebGL ([WEBGL_BUILD.md](WEBGL_BUILD.md); Red's rule, 2026-10-02). Touch, haptics and the mobile render tier compile only for `UNITY_IOS || UNITY_ANDROID`; Editor Play Mode and Mac/Win/WebGL keep reading the same keys and mouse.
2. **Same design language.** Every touch element is built from parts the desktop HUD kit already has (UI MOCKUP 2256:8 · UI02):
   - the 4 px yellow rule `#F4DF3B` marks what matters;
   - the keycap outline (2 px paper `#F4F1E8`) becomes the USE button and the pause button;
   - the hold ring (paper @30 % track + 3.5 px yellow arc) becomes the stick's sprint arc and USE's hold progress;
   - the yellow key chip (`Overlay / Key chip`) becomes the tappable chip; a pressed USE fills yellow the same way;
   - faces stay Bayon (labels), IBM Plex Mono (meta), Source Serif 4 (zone, hint).
3. **World first.** The HUD keeps to the top band and around the dot. Controls are ghosts until touched; USE exists only while the dot is on something.
4. **Thumbs don't scale.** Control sizes are fixed in pt/dp on every device; only type and HUD scale.

## 2. Input mapping

| Desktop | Verb | Touch |
|---|---|---|
| WASD | Move | Left thumb, floating stick: it centres where the thumb lands (left half, below the top band). |
| Mouse | Look | Right thumb drags anywhere that isn't a button. |
| Shift (hold) | Sprint | Push the stick **past its ring inside the forward ±45° arc**. Letting it back inside walks. Optional SPRINT toggle button (setting). |
| E | Door / key | **USE** button, shown only while `UpdateAim` has a target; its label is the verb (OPEN / SHUT / TAKE / LOCKED). |
| Hold E | Break glass | Hold USE; the ring fills **outside** the thumb (r 46 pt) and the bar under the dot. TAP mode (existing setting) = repeated taps on USE. |
| Walk into frame | Climb | Unchanged (stick into the broken frame). |
| S | Cancel a shot | Pull the stick back (same meaning as S). |
| Esc | Pause | Pause button top-right; Android back; app losing focus (already `OnApplicationFocus`). |
| O | Settings | SETTINGS chip on the pause screen; rows are tappable. |
| R | Restart | RESTART / TRY AGAIN chip. |
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
| Stick (resting ghost) | centre (150, 290) | base Ø120, sprint arc r 76, thumb Ø52 |
| USE | centre (740, 262) | Ø72 visual / Ø88 hit, hold ring r 46 |
| Hint card | bottom centre, 452 wide | dark card + yellow rule |
| Top band | y 0–64 | read only; the only control is pause |

Android 20:9 (915×412 dp): insets from `WindowInsets`; keep the stick's touch area ≥ 24 dp from the long edges, where the back gesture lives (both edges in landscape), and exclude at most the stick band from system gestures (Android limits exclusion to 200 dp per edge). iPad 11" (1180×820 pt): same control sizes; HUD uses the tablet ramp; margins 32–40 pt.

## 4. Type ramp (handheld) — PROVISIONAL, for 平面视觉 review

Rule: up to 24 px, keep the desktop's **angular** size at each device's viewing distance — phone ×0.85 (≈30 cm, 460 ppi @3x), iPad ×0.95 (≈40 cm, 264 ppi @2x) against a 24" 1080p monitor at 60 cm. Larger type is compressed so the zone name never covers the room. Figma text styles `Touch/Phone/*`, `Touch/Tablet/*`.

| Role | Face | Desktop px | Phone pt | iPad pt |
|---|---|---|---|---|
| Meta | IBM Plex Mono | 13/16 | 11/14 | 12/15 |
| Label / prompt | Bayon (+3 %, upper) | 20/20 | 17/17 | 19/19 |
| Context / hint | Source Serif 4 | 24/26 | 20/22 | 23/25 |
| Zone name | Source Serif 4 | 50/42 | 26/24 | 36/32 |
| Screen title | Bayon | 88/80 | 48/44 | 64/58 |

Unity mapping: a handheld `CanvasScaler` in Constant Physical Size (or a pt-based scale factor = `Screen.dpi / 163` on iOS, `/160` on Android), so 1 UI unit = 1 pt/dp; desktop keeps `ScaleWithScreenSize` 1920×1080.

## 5. Components (TK · Touch kit masters)

- `Touch / Stick` — State = Idle (ghost) / Walk / Sprint (arc + thumb yellow, label SPRINT) / Winded (muted, label WINDED).
- `Touch / Use` — State = Open / Shut / Take / Locked / Hold / Tap mode / Pressed.
- `Touch / Prompt` — Tap (mini-USE glyph replaces the keycap E) / Hold (ring glyph) / Locked (no glyph, muted).
- `Touch / Pause`, `Touch / Chip` (text property Label), `Touch / Sprint toggle` (Off / On / Winded).
- `HUD·T / Zone`, `HUD·T / Key`, `HUD·T / Hint card` (Size = Phone / Tablet), `HUD·T / Crosshair`, `HUD·T / Stamina`, `HUD·T / Hold bar`.
- Device overlays (iPhone island + home indicator, Android punch-hole + handle, iPad home indicator) and safe-area guides.

## 6. Settings (new TOUCH section, PlayerPrefs like `FrontRoomsSettings`)

| Row | Values (default first) |
|---|---|
| LOOK SPEED | 5 (1–10) |
| INVERT LOOK | OFF / ON |
| GYRO LOOK | OFF / WHILE TOUCHING / ALWAYS |
| STICK | FLOATING / FIXED |
| SPRINT | PAST THE RING / TOGGLE BUTTON |
| CONTROLS SIZE | 100 % (80–140) |
| CONTROLS OPACITY | 60 % |
| LEFT-HANDED | OFF / ON (swaps sides) |
| HAPTICS | FULL / LIGHT / OFF |
| EDIT LAYOUT | opens the layout editor (T3) |

The existing BREAK GLASS row reads HOLD USE / TAP USE on touch. With iOS Reduce Motion on, CAMERA MOTION's first-launch default is OFF.

## 7. Haptics (event → pattern)

| Event (existing hook) | Pattern |
|---|---|
| Door opens (`OnDoorMoved`) | one light transient (≈0.5) at the latch |
| Locked rattle (`rattleJolts`, Rattle.Jolt1/Jolt2) | two firm transients on the camera jolts |
| Glass hold (`map.Hold` progress) | continuous 0.2→0.6, transients at ⅓ and ⅔, heavy 1.0 on `OnGlassBroken` |
| Sprint | light tick crossing the ring; soft double when `winded` |
| Relay lock-on (pursuit redesign cue) | firm double pulse, chase only |
| Caught | 0.8 s heavy fade |

iOS: Core Haptics (AHAP) via a small native plugin, falling back to UIImpactFeedbackGenerator. Android: `VibrationEffect` (primitives on API 30+, waveforms below).

Ownership (agreed with 声音设计, 2026-10-03): this touch layer owns the haptic patterns and any AHAP files. Haptics fire from the **same game events** the sound listens to (DoorLatched, the rattle jolts, GlassCracked / GlassBroken, PlayerSprinting / PlayerWinded, Caught, the lock-on cue when it exists), never from FMOD/audio callbacks. Where a haptic must land on an audio transient (e.g. the crack's swap frame), 声音 documents the event time in AUDIO_CONTRACT.

## 8. Implementation plan (not started)

- **T0 Facade** — `Assets/Scripts/Input/FrontRoomsInput.cs` (static): `Move`, `LookDelta`, `SprintHeld`, `UsePressed/UseHeld`, `BackPressed` (shot cancel), `PausePressed`, `StartPressed`, settings navigation. On desktop it returns exactly today's `Input.*` reads. `FrontRooms3DGame` (owned by 关卡设计) swaps its direct reads for the facade; acceptance = autopilot seeds 2554/20388 identical.
- **T1 Touch layer** — `FrontRoomsTouchControls` (EnhancedTouch, finger ownership by touch-down zone, floating stick, look curve, USE bound to `UpdateAim`'s `aimed/aimedHold`, pause, safe area).
- **T2 Menus** — chips, tappable settings rows, tap to start; prompt strings swap the "E" token for the touch glyph (strings from `MapWorld.Describe`, owned by 关卡设计).
- **T3** — haptics, TOUCH settings, layout editor.
- **T4** — `FrontRooms3DBuild.BuildIOS/BuildAndroid` (landscape only, IL2CPP ARM64, defer system gestures on all edges, hide home indicator), mobile quality level + URP asset (visual/WebGL chats), device test on Red's iPad + a phone. FMOD (声音, ping when T4 starts): a Mobile platform in the generated Studio project with mobile encoding, FMOD for Unity's iOS/Android entries pointed at the Mobile bank folder, desktop settings untouched, init + captured run verified on device or simulator.

## 9. Open decisions for Red

1. Sprint default: push past the ring (recommended) or a button?
2. Gyro look off by default (recommended)?
3. Frame target: 60 fps on a recent iPhone, or 30?
4. Landscape only (recommended)?
5. Bluetooth/MFi controllers on mobile in v1?
6. TestFlight / internal testing only, or the stores?
