# Touch controls · iOS + Android

Status: **revised and verified in the running game, 2026-10-07** (interaction-design chat). Codex's first pass (2026-10-04) is replaced; what was wrong with it is in §8. Everything below is built in `Assets/Scripts/Input/` and checked by a headless touch playtest on the real game and map (§11). Not yet run on a device, and no mobile build exists (builds only when Red asks). The build-side plan stays in [MOBILE_BUILD_PLAN.md](MOBILE_BUILD_PLAN.md).

- Figma: file `0tCbAiVUlrPId3RWd9LRif`, page 2099:76, section **FRONTROOMS · TOUCH CONTROLS · iOS + ANDROID** (`2528:5403`) at x 33937, y 2000.
  - Slides TC01–TC15 (1920×1080, P1 deck system); TC14 MOTION and TC15 FROST added 2026-10-07 from the playtest frames.
  - `TK · Touch kit masters · 1x pt` (2528:5495): the touch components.
  - `TS · Screen masters · 1x pt` (2530:5061): every screen as a component, 1 Figma px = 1 pt (iOS) / 1 dp (Android).
- Research: `Documentation/research/touch/01_shipped_games.md` (how shipped iOS/Android games map first-person verbs) and `02_guidelines_patterns_unity.md` (Apple HIG / WWDC, Android, ergonomics, accessibility, Unity facts). Media is URL-only until Red approves downloads; slides TC02–TC04 hold `media:<key>` slots.

## 1. Rules this design keeps

1. **Desktop does not change.** Mobile is a separate track, like WebGL ([WEBGL_BUILD.md](WEBGL_BUILD.md); Red's rule, 2026-10-02). One switch decides it: `FrontRoomsHandheld.Active` is true in iOS and Android players. In the Editor it is true only with FrontRooms 3D › Mobile › Touch preview (off by default) or inside the touch playtest. It is never true in batch mode or an autopilot run. It replaced `Application.isMobilePlatform`, which is also true for **WebGL on a phone** and would have pulled the WebGL track into the touch path. Editor Play Mode and Mac/Win/WebGL keep reading the same keys and mouse.
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
| Space / Return | Start | Tap anywhere on the title; a text-only TAP TO START prompt sits below the logo and fades out with the title when play begins (touch only). |
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
| LOOK SPEED | 100 % (60 / 80 / 100 / 120 / 150; 100 % = 0.20° per pt dragged) |
| INVERT LOOK | OFF / ON |
| GYRO LOOK | OFF / WHILE TOUCHING / ALWAYS |
| STICK | FLOATING / FIXED |
| SPRINT | SOCKET / TOGGLE BUTTON |
| CONTROLS SIZE | 100 % (80 / 100 / 120 / 140) |
| CONTROLS OPACITY | 34 % (20 / 34 / 50 / 65); 34 % draws the Figma state alphas exactly. It thins only the text-free ghosts (stick, rings, socket, hold track, pause keycap); a control that carries a word (USE, the SPRINT button) stays solid at any setting, so a low setting never makes a verb illegible |
| LEFT-HANDED | OFF / ON (swaps sides) |
| HAPTICS · GAMEPLAY | ON / OFF (door, glass, lock-on, caught) |
| HAPTICS · CONTROLS | ON / OFF (sprint socket, hold ring) — two switches like Alien: Isolation; hidden on iPad (no haptics) |
| INTERACTION ASSIST | OFF / SLOW (look gain ×0.5 within ~4° of a door handle or pane) — **not built yet** |
| EDIT LAYOUT | opens the layout editor (T3) — **not built yet** |

The existing BREAK GLASS row reads HOLD USE / TAP USE on touch. With iOS Reduce Motion on, CAMERA MOTION's first-launch default is OFF.

As built, the settings card is the desktop card's twin: the game's own rows in the left column, TOUCH in the right column (which scrolls with inertia). Tap a row to step it (on/off rows flip); on a stepped row ‹ steps down and the rest of the row steps up. Rows that can't do anything are hidden (the haptic rows on iPad).

## 7. Haptics (event → pattern)

| Event (existing hook) | Pattern |
|---|---|
| Door opens (`OnDoorMoved`) | one light transient (≈0.5) at the latch |
| Locked rattle (`rattleJolts`, Rattle.Jolt1/Jolt2) | two firm transients on the camera jolts |
| Glass hold (`map.Hold` progress) | continuous 0.2→0.6, transients at ⅓ and ⅔, heavy 1.0 on `OnGlassBroken` |
| Sprint | light tick as the thumb lands in the socket; soft double when `winded` |
| Relay lock-on (pursuit redesign cue) | firm double pulse, chase only |
| Caught | 0.8 s heavy fade |

**iPad has no haptics**, so every haptic also exists as sound or picture (the ring, the bar).

As built (2026-10-07), `FrontRoomsMobileHaptics` plays these, with two switches (GAMEPLAY / CONTROLS) and at most one pulse per 30 ms per channel:

| Moment | Channel | Pattern |
|---|---|---|
| USE press, and tapping a door itself | Controls | rigid 0.55 |
| Sprint latches in the socket | Controls | light 0.6 |
| Winded | Controls | soft 0.5, then soft 0.35 at +120 ms |
| Menu chip or settings row | Controls | selection tick |
| A door near the player latches shut | Gameplay | light 0.5 |
| Locked rattle | Gameplay | rigid 0.8 on `Rattle.Jolt1` and `Jolt2` |
| Glass hold | Gameplay | light, rising 0.2 → 0.6, every 20 % of the hold |
| Glass cracks (stage 1, 2) | Gameplay | medium 0.7 / 0.8 |
| Glass shatters | Gameplay | heavy 1.0 |
| Relay lock-on (the chase starts) | Gameplay | medium 0.75 twice, 150 ms apart |
| Caught | Gameplay | heavy 1.0 → medium 0.65 (+180 ms) → soft 0.4 (+400) → soft 0.2 (+650) |

- iOS uses UIKit's feedback generators (impact light / medium / heavy / soft / rigid with intensity, and selection) through `Assets/Plugins/iOS/FrontRoomsHandheldNative.mm`. There are no AHAP files yet; Core Haptics remains the upgrade for a continuous glass buzz.
- Android uses `VibrationEffect.createPredefined` (TICK / CLICK / HEAVY_CLICK) on API 29+. On API 26–28 it plays short amplitude pulses, but only if `hasAmplitudeControl()`. Below that it plays nothing; the build's minSdk is 25.
- It never calls `Handheld.Vibrate`, which is a long buzz. One never-called reference stays in the code so that Unity adds the VIBRATE permission.

Ownership (agreed with 声音设计, 2026-10-03): this touch layer owns the haptic patterns and any AHAP files. Haptics fire from the **same game events** the sound listens to (DoorLatched, the rattle jolts, GlassCracked / GlassBroken, PlayerSprinting / PlayerWinded, Caught, the lock-on cue when it exists), never from FMOD/audio callbacks. Where a haptic must land on an audio transient (e.g. the crack's swap frame), 声音 documents the event time in AUDIO_CONTRACT.

## 8. Implementation status (revised 2026-10-07)

**What was wrong with the first pass** (Codex, 2026-10-04), and what replaced it:

- **Sprint never worked.** The stick read touches in raw screen pixels but sized the ring, socket and dead zone in points. On a 3× iPhone the socket it tested sat about 31 pt above the thumb, inside the ring, instead of at the drawn socket 92 pt up. Reaching for the drawn socket never sprinted, and an ordinary forward push could. Look speed also changed with the screen's pixel density, and the shot-cancel pull-back fired at the dead zone. Everything is now measured in points: `FrontRoomsHandheld.PointScale` is `UIScreen.nativeScale` on iOS (from the plug-in) and the display density on Android, and one geometry, `FrontRoomsTouchLayout`, serves both input and drawing.
- **No real motion.** States switched by linear fades of whole groups, all at one speed. Each element now has its own eased, staggered motion and springs (§10), plus a Reduce Motion version.
- **Builds broke.** The Mac/Win build didn't compile (CS0103 ×24, from an `#if` around a method still in use), and neither did Android (an `AndroidJavaProxy` stored in an `AndroidJavaObject` field).
- **Wrong platform test.** It used `Application.isMobilePlatform`, which is true for WebGL on a phone. It's now `FrontRoomsHandheld.Active` (§1).
- **Settings unreachable.** The touch card held only the nine touch rows. Captions, camera motion, reduce flashing, break glass, HDR and the Relay readout could only be changed with a keyboard. The card now shows the desktop rows plus TOUCH (§6).
- **Haptics.** It only had `Handheld.Vibrate` (a long buzz) and one switch. That's replaced by the patterns in §7.

**What exists now** (all in `Assets/Scripts/Input/` unless noted):

| Part | File | Does |
|---|---|---|
| Platform gate | `FrontRoomsHandheld.cs` | `Active`, point scale, safe area in pixels, iPad (tablet ×1.4 controls), Reduce Motion, haptics support; Editor preview and the playtest's forced profiles (iPhone 16 Pro, Android 20:9, iPad 11") |
| Native (iOS) | `Assets/Plugins/iOS/FrontRoomsHandheldNative.mm` | `nativeScale`, Reduce Motion, iPad check, impact / selection haptics (UIKit only, plain `int` returns) |
| Geometry | `FrontRoomsTouchLayout.cs` | §3 in points: stick, socket (92 up, Ø40, catch 28), USE (Ø72 / hit Ø88), pause (32 / hit 44), zones, settings card; left-handed mirror |
| Input | `FrontRoomsTouchControls.cs` | finger ownership by touch-down zone; floating / fixed stick; socket latch (150 ms dwell, held while the thumb stays forward, released on pull-back or lift, refused while winded); SPRINT button mode; look (0.20° per pt × LOOK SPEED, gyro); USE tap / hold / locked; tap-the-door; pause; menus; settings rows and scroll; writes `FrontRoomsInput`'s virtual snapshot |
| Picture and motion | `FrontRoomsTouchControlsView.cs`, `FrontRoomsTouchMotion.cs`, `FrontRoomsTouchSprites.cs` | the TK / TS masters drawn at device density, every transition in §10 |
| Game side | `FrontRooms3DGame.Mobile.cs` | the host (rows, pause meta, caught stats), prompt → USE verb, restart confirmation, Back, tap-the-door, haptic hooks on the map's and Relay's own events |
| Haptics | `FrontRoomsMobileHaptics.cs`, `FrontRoomsMobileInteractionEvents.cs` | §7 |
| Android Back | `FrontRoomsMobileBackBridge.cs` (Codex's, type fixed) | predictive-back callback → one step back (confirm → settings → pause ↔ play) |
| Playtest | `FrontRoomsTouchPlaytestDriver.cs`, `Assets/Editor/Input/FrontRoomsTouchPlaytest.cs` | §11 |

`FrontRooms3DGame.cs` changed only where 关卡设计调研 agreed: the platform test, the safe-area pixels, the prompt and hint strings shown on touch, and one `OnDestroyMobile()` call. Level profile, map start, autopilot and Relay-facing code are untouched.

**Mobile texture budget (游戏视觉, 2026-10-07).** The Q1b wallpaper print is in main:
- `_PrintTex` on iOS is now ASTC 4×4, about 5.3 MiB (it was uncompressed RGBA32, 32 MiB).
- `Resources/Print/FR_Print_HardEdge`, an 8-frame array, is ASTC 4×4 2048² × 8 on iOS, about 42.7 MiB. It sits in Resources, so it ships in the package.
- iOS was tested (slice 0 matches the static map, 0 of 262,144 samples differ); Android wasn't.
- Mobile import settings are at their defaults and belong to the mobile tier. Change only the iOS/Android tabs (e.g. ASTC 6×6 / 8×8, or 1024²) once device profiling says how much memory a phone can spare.

**Still to do:** run on devices (two thumbs, gyro, haptics, notch and island insets, Android `WindowInsets` and gesture exclusion), the EDIT LAYOUT editor, INTERACTION ASSIST, Core Haptics (AHAP) for the glass, FMOD mobile banks, the mobile render tier, and the export (`Assets/Editor/FrontRoomsMobileBuild.cs` has the menus; nothing has been built).

## 9. Open decisions for Red

1. Sprint default: the socket latch (recommended) or a button?
2. Gyro look off by default (recommended)?
3. Frame target: 60 fps on a recent iPhone, or 30?
4. Landscape only (recommended)?
5. Bluetooth/MFi controllers on mobile in v1?
6. TestFlight / internal testing only, or the stores?
7. Break glass on touch: HOLD USE by default (recommended: a held button is natural on glass; XAG asks for holds under 2–3 s) or TAP USE?
8. Android needs Unity's new Input System project-wide: migrate desktop input too, verified to feel identical (recommended), or ship iOS first and decide later?

## 10. Motion (as built, 2026-10-07)

Rules: every change moves, nothing pops; controls answer within one frame (70–90 ms to full); menus build in reading order; springs (OutBack) only where a thumb lets go or something locks in. All motion runs on the touch layer's own unscaled clock (`FrontRoomsTouchMotion.Now`), so menus move while the game is paused. Curves: OutCubic (arrivals), InCubic (exits), OutBack / OutBackSoft (springs, overshoot constant 1.70 / 1.1), InOutSine (breathing).

**Reduce Motion** (iOS Reduce Motion, Android "remove animations" (animator duration scale 0), or CAMERA MOTION at 0 %): alpha only, every duration capped at 108 ms, no rises, scales, springs, shakes or pops, numbers appear at their value.

| Element | Moment | Motion |
|---|---|---|
| Controls layer | play starts / a menu opens | fade in 180 ms OutCubic / out 120 ms InCubic |
| Stick | thumb down | the live stick appears under the thumb: alpha 90 ms, scale 0.88 → 1 in 140 ms OutBackSoft; the resting ghost fades out 90 ms |
| Stick | thumb up | thumb springs home 160 ms OutBackSoft; live stick fades 180 ms InCubic; ghost returns 220 ms after 60 ms |
| Socket | arming | a yellow arc (Ø66, 3.5 pt, outside the thumb) fills clockwise from 12 o'clock over the 150 ms dwell |
| Socket | latch | the arc closes into the halo; socket and halo pop 1.15 → 1 in 220 ms; thumb tints yellow 120 ms; SPRINT fades in 140 ms rising 6 pt; light haptic |
| Socket | let go | halo fades 150 ms InCubic, thumb tint 150 ms, label 100 ms |
| Stick | winded | tints to muted 200 ms; label becomes WINDED in paper (muted vanished on lit wallpaper); the socket refuses the thumb (drawn back inside the ring) until breath returns. The stick label sits on a solid ink chip (below) |
| USE | appears / goes | 160 ms alpha + scale 0.86 → 1 in 200 ms OutBack / 120 ms InCubic to 0.94 |
| USE | press / release | yellow fill and ink verb in 70 ms, scale 0.92 / fill out 140 ms, scale back 180 ms OutBack |
| USE | verb changes (OPEN ↔ SHUT) | crossfade 120 ms |
| USE | locked press | shake 3 cycles, ±4 pt decaying, 240 ms, with the rattle haptics; the press shows a faint paper flash, never the yellow "go" fill |
| Hold ring | hold | track fades in 120 ms; the arc follows progress (60 ms smoothing), outside the thumb at r 46 |
| Pause keycap | press | scale 0.9 in 70 ms, back in 160 ms OutBackSoft |
| Frost | pause / settings / caught | fades in over the live frame in 240 ms (420 ms when caught) OutCubic, so the room frosts over; out 160 ms (see the backdrop note below) |
| Pause card | opens | title (+40 ms, rises 18 pt), meta (+70, 12), legend rows (+100 / +130, 10), chips RESUME / SETTINGS / RESTART (+160 / +190 / +220, rise 12, scale 0.96 → 1); each 260 ms OutCubic |
| Restart question | opens | RESTART THIS RUN? (+40 ms), CANCEL / RESTART (+50 / +80) |
| Settings card | opens | card fades in 260 ms and scales 0.97 → 1 in 220 ms; rows rise 8 pt, 24 ms apart from +60 ms; CLOSE (+80 ms) |
| Settings row | press / change | pressed tint 60 ms in, 200 ms out; the value crossfades and slides 6 pt in 140 ms; the active row's yellow rule 120 ms |
| Touch column | scroll | inertia (velocity decays with a 220 ms time constant); a scroll bar shows while it moves and fades 400 ms after 600 ms still; a fade into the card where rows continue |
| Caught card | opens | title (+80 ms, drops in 12 pt), the four numbers count up over 600 ms from +200 ms, rule (+300), sentence (+340); TRY AGAIN at +600 ms and ignores taps before then |
| Title | waiting | TAP TO START (paper on the ink chip) fades in after 3 s (600 ms InOutSine), then stays still. It doesn't breathe (平面视觉: no alpha ramp on words; the corridor already moves). Any future "waiting" signal may only be a hard on/off of a solid element, e.g. a paper block cursor 0.6 s on / 0.6 s off, stopped by REDUCE FLASHING |
| Any chip | press | scale 0.94 in 70 ms, back 160 ms OutBackSoft |

**The backdrop behind the cards is frost, not flat off-white** (Red, 2026-10-07: "a frosted wallpaper-texture mask over the actual paused frame, the UI on top").
- On the press, the touch layer renders the game camera once (the world only, no HUD) at half resolution and blurs it: halved down to 1/16 of the screen, then back up to 1/4, bilinear both ways.
- Three layers sit over that, under every card:
  - a paper veil, `WashColor` at 30 % (linear blend, as Unity blends UI: the room shows, and ink still reads at 6 : 1 or better);
  - `Resources/UI/Touch/TouchFrostPaper.png`, baked from the game's own wallpaper maps by `Tools/touch/bake_frost_paper.py`: the paper's texture only (fibres, tobacco stains, a fine matte grain), no print pattern (Red: texture, not pattern); one roll tile is drawn 280 pt wide;
  - the cards.
- Pause ↔ settings keep the frame they froze. The texture is released once the frost has faded out.
- With no camera, it falls back to the old 98 % paper wash.
- On this backdrop the text is solid ink: the pause meta line and the caught labels lose their 60 % / 55 % ink, which on a frosted room measured under 3 : 1. The settings card is opaque on touch, so the frost doesn't show through it like dirt.

**平面视觉's rule for all in-game UI (2026-10-07): no translucent ink or surfaces.**
- Unity blends UI alpha in linear light, so any alpha tint renders lighter than Figma shows.
- Hierarchy comes from face, size and tracking. A secondary tone is a solid colour that is ≥ 4.5 : 1 on both the darkest and the lightest backdrop.
- Approved: solid-ink pause meta and caught labels (about 7.9 : 1 at worst over the frost's L 0.37 floor), and the opaque settings card.

**The stick's state label sits on a solid ink chip**, the same family as the HUD's key prompt and hint card:
- fill `#0E0E0D` at 100 %, square corners;
- 4 pt above and below Bayon 17's caps and 7 pt either side;
- SPRINT in accent yellow, WINDED in paper `#F4F1E8`;
- it fades in and out with the label.
- In code, the chip hugs the caps measured from the glyph quads (`Caps()`), not Bayon's line box.
- If the chip ever crowds the ring, drop the label to Bayon 15 before shrinking the padding.
- It replaces a 1 pt shadow: paper on lit Level 0 paper measured about 1.9 : 1, and shadows aren't part of the system.
- Figma: `Touch / Stick` State=Sprint and State=Winded (2530:3812, 2530:3823) carry the chip, and so does the Sprint screen master through its instance.
- The chip's height is Bayon's cap height (0.714 em) + 8 pt = 20.1 pt at 17 pt. Glyph quads and rasterised glyph bounds both carry padding (runs 11–12 drew 21.3 pt); only their middle is used.

**Text-bearing controls are solid (平面视觉, Q1, 2026-10-07).**
- The rule is about text and the ground under it, so text-free ghosts stay translucent and are scaled by CONTROLS OPACITY, as §1 rule 3 says.
- Anything that carries a word is solid:
  - the USE disc is `#0E0E0D` at 100 % behind its verb (paper; LOCKED in muted, 9.9 : 1);
  - the SPRINT button's disc is solid too (ink, or yellow when on);
  - TAP TO START sits on the stick label's ink chip.
- Figma: `Touch / Use` (2530:3854) and `Touch / Sprint toggle` (2530:3877) discs are solid; the Title screen master (2532:5101) has the prompt on the chip.

## 11. Verification: the touch playtest

`FrontRoomsTouchPlaytest` plays the real game and map in Play Mode on a handheld profile, with multi-finger touches injected on a virtual touchscreen. It checks what each gesture did to the game, and captures composited frames (room, HUD and touch layer, blended exactly) at fixed 1/60 s steps, so every motion frame lands on its intended time.

- **Run it** in a private copy of the project, never in the copy Red has open:
  `Unity -batchmode -projectPath <copy> -buildTarget OSXUniversal -executeMethod FrontRoomsTouchPlaytest.RunBatch -touchProfile iphone|android|ipad -touchIsolatePrefs` (no `-quit`; it exits with 0 when every check passes). From the Editor: FrontRooms 3D › Mobile › Run touch playtest. Output goes to `Verification/touch-playtest/<profile>/`: the frames and `report.json` (checks, frame times, every haptic with channel, style and intensity).
- **Try it by hand** with FrontRooms 3D › Mobile › Touch preview in Play Mode (iPhone 16 Pro). The mouse becomes one finger; set the Game view to 2622 × 1206 for the true layout. The preview is off by default and never applies in batch or autopilot runs.
- **What it checks** (iPhone 16 Pro profile):
  - title tap;
  - floating stick: a push to the ring edge walks at 3.2 m/s and never sprints;
  - socket latch: sprints at 5.5 m/s with the latch haptic; steering keeps it, pull-back ends it;
  - winded: WINDED shows, the soft double haptic plays, sprint stops, and a thumb still in the socket sprints again once breath is back;
  - look: 100 pt = 20°; two thumbs at once;
  - pause, settings (the desktop rows tap, ‹ › step, the TOUCH column scrolls), restart confirm and cancel, resume;
  - Android Back through the bridge's own queue: pause, close settings, cancel restart, resume;
  - tapping a shut door itself opens it (a shut leaf always faces the player); USE then shuts it, the verb crossfades and the latch is felt; a locked door rattles, is felt twice and shakes USE with a paper flash (the harness switches keys on for that step only, since the shipped profile has none);
  - holding USE on a pane breaks it in 1 s, with the rising buzz, two cracks and the heavy hit (the stand matches the desktop autopilot's glass scenario);
  - caught: the stats count up, an early tap is ignored, TRY AGAIN restarts;
  - Reduce Motion: the chips fade in place.
  - Motion checks read node positions: for example, the RESUME chip is still low at f08, nearly home at f18 and home at f30.
  - The Relay is held dormant for these steps (a runtime flag on that run's Relay; no game code changes). The steps teleport the player around a live map, and in run 9 the Relay caught it halfway. The caught step calls the game's `End()` itself.
  - Latest: run 10, 2026-10-07, 63 / 63 checks, 74 frames. Frames are in `~/FrontRoomsVisualWork/touch/run10_iphone`; sheets are in `research/touch/images/tc_*.jpg` (VL128–VL131; Figma TC14, TC15).
- **Two Editor facts** the harness works around, so nobody trips on them again:
  - In batch mode no Game view has focus, so the Input System withholds pointer and touch input from Play Mode. The harness swaps in a copy of the input settings that sends all input to the game (all devices, ignore focus) and restores the original afterwards.
  - `Time.captureDeltaTime` steps game time but not unscaled time, and captures take real seconds. The touch layer therefore reads its own clock (`FrontRoomsTouchMotion.Now`), which the harness steps 1/60 s per frame. In players that clock is plain unscaled time.
- **Desktop unchanged** (关卡设计调研's condition 3, 2026-10-07): `FrontRoomsHandheld.Active` stays false in batch mode and autopilot runs, and the touch layer never appears in their logs. The desktop autopilot (`FrontRoomsMainScenePlaytest.RunBatch -autopilotSeed 2554`, private copy, timeout raised for the machine's load and a separate product name so Red's PlayerPrefs stay untouched) was run three times:
  - A: current main.
  - B: the same copy with only the touch files at their pre-revision state (279c144); FrontRooms3DGame.cs and FrontRoomsSettings.cs match 279c144 exactly.
  - B2: B again.

  | | A · current | B · pre-revision | B2 · pre-revision again |
  |---|---|---|---|
  | Verdict | PASS · ended by time | PASS · ended by time | PASS · ended by time |
  | Errors / caught | 0 / no | 0 / no | 0 / no |
  | Same maze (seed 2554, start door cell 21, 6) | yes | yes | yes |
  | Space · start door opened · left start rooms | 1.8 · 3.62 · 4.87 s | 1.8 · 3.62 · 4.87 s | 1.8 · 3.62 · 4.87 s |
  | Relay released | 10.05 s | 10.22 s | 10.25 s |
  | Cells · zones · doors opened | 71 · 7 · 1 | 78 · 10 · 8 | 69 · 10 · 4 |

  The paths differ after the start because the bot steps on real frame time. B and B2, with identical code, differ as much as A and B, so the touch revision does not change the desktop game. Reports: `~/FrontRoomsVisualWork/touch/autopilot_compare/`.
