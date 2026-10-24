# Touch controls · iOS + Android

> **DONE, 2026-10-08 18:1x (interaction-design chat): Red's run redesign is in main and verified.** Push the stick past the ring to run; three gaits (slow / walk / run) that the Relay hears (§2, §2.1).
> - Verified: touch playtest run 19, 72 / 72 (§11); desktop autopilot seed 2554 with the new hooks, PASS with 0 errors and the same start timings (§11, run C); Mac and iOS compile.
> - Figma: TC16 RUN PAST THE RING, the five stick states, TC08, TC14, the flow board (S03 SLOW, S05 RUN, renumbered S01–S16); verification VL218.
> - It is ready for a mobile build when Red asks. (A "DONE" line written here at 17:21 by the Codex build session came before the code existed; it is replaced by this one.)
>
> 已完成（18:1x）：「推出圆盘即奔跑 + 慢走/正常/奔跑三档步态（Relay 能听到）」已进入 main 并通过验证：触控 playtest 72/72，桌面 autopilot 2554 PASS，Mac/iOS 编译通过。可按 Red 的要求出包。

Status: **revised and verified in the running game, 2026-10-07** (interaction-design chat). Codex's first pass (2026-10-04) is replaced; what was wrong with it is in §8. Everything below is built in `Assets/Scripts/Input/` and checked by a headless touch playtest on the real game and map (§11). Not yet run on a device, and no mobile build exists (builds only when Red asks). The build-side plan stays in [MOBILE_BUILD_PLAN.md](MOBILE_BUILD_PLAN.md).

- Figma: file `0tCbAiVUlrPId3RWd9LRif`, page 2099:76, section **FRONTROOMS · TOUCH CONTROLS · iOS + ANDROID** (`2528:5403`) at x 33937, y 2000.
  - Slides TC01–TC16 (1920×1080, P1 deck system); TC14 MOTION and TC15 FROST added 2026-10-07 from the playtest frames; TC16 RUN PAST THE RING (`2907:6556`, at section x 4200, y 6720) added 2026-10-08.
- Figma flow board: section **FRONTROOMS · TOUCH FLOWS · EVERY SCREEN · iOS + ANDROID** (`2889:6157`) at x 86017, y 2000 (2026-10-08).
  - S01–S16 cover every phone screen, as live instances of the TS masters with the state switched; S14 and S15 are edited copies. S03 SLOW was added 2026-10-08 and the later screens renumbered (WALK S04, RUN S05 … CAUGHT S16).
  - Lanes: START, USE, MENUS, END. Arrows carry the gesture.
  - Under each screen, its WAY BACK. iOS has no system Back, so every screen below the title has one on screen.
  - `TK · Touch kit masters · 1x pt` (2528:5495): the touch components.
  - `TS · Screen masters · 1x pt` (2530:5061): every screen as a component, 1 Figma px = 1 pt (iOS) / 1 dp (Android).
- Research: `Documentation/research/touch/01_shipped_games.md` (how shipped iOS/Android games map first-person verbs) and `02_guidelines_patterns_unity.md` (Apple HIG / WWDC, Android, ergonomics, accessibility, Unity facts). Media is URL-only until Red approves downloads; slides TC02–TC04 hold `media:<key>` slots.

## 1. Rules this design keeps

1. **Desktop does not change.** Mobile is a separate track, like WebGL ([WEBGL_BUILD.md](WEBGL_BUILD.md); Red's rule, 2026-10-02). One switch decides it: `FrontRoomsHandheld.Active` is true in iOS and Android players. In the Editor it is true only with FrontRooms 3D › Mobile › Touch preview (off by default) or inside the touch playtest. It is never true in batch mode or an autopilot run. It replaced `Application.isMobilePlatform`, which is also true for **WebGL on a phone** and would have pulled the WebGL track into the touch path. Editor Play Mode and Mac/Win/WebGL keep reading the same keys and mouse.
2. **Same design language.** Every touch element is built from parts the desktop HUD kit already has (UI MOCKUP 2256:8 · UI02):
   - the 4 px yellow rule `#F4DF3B` marks what matters;
   - the keycap outline (2 px paper `#F4F1E8`) becomes the USE button and the pause button;
   - the hold ring (paper @30 % track + 3.5 px yellow arc) becomes the stick's run line (paper dashes, yellow once you run) and USE's hold progress;
   - the yellow key chip (`Overlay / Key chip`) becomes the tappable chip; a pressed USE fills yellow the same way;
   - faces stay Bayon (labels), IBM Plex Mono (meta), Source Serif 4 (zone, hint).
3. **World first.** The HUD keeps to the top band and around the dot. Controls are ghosts until touched; USE exists only while the dot is on something.
4. **Thumbs don't scale, grip does.** Control sizes are set in pt/dp, not HUD units: phone primary hit areas ≈ 15 mm (Xbox Accessibility Guidelines 107; USE hit 88 pt = 14.6 mm), iPad controls start at 1.4× (XAG: 24 mm on tablets). Type and HUD follow the ramp in §4.

## 2. Input mapping

| Desktop | Verb | Touch |
|---|---|---|
| WASD | Move | Left thumb, floating stick: it centres where the thumb lands (left half, below the top band). |
| Mouse | Look | Right thumb drags anywhere that isn't a button. |
| Shift (hold) | Sprint | **Push past the ring** (Red, 2026-10-08; it replaced the socket latch). The thumb's distance from the stick's centre picks one of three gaits (§2.1). It runs once the thumb crosses the **run line**, 24 pt past the ring (84 pt from the centre), only within ±70° of straight ahead: one rigid haptic click, the line and thumb turn yellow, RUN shows. Back inside the ring (or a pull back, or lifting) walks again; between the ring and the line the gait holds, so a thumb resting near the edge never flickers. Alien: Isolation's sprint fired at the stick's edge itself and broke stealth (research 01 §3); ours needs a deliberate push past it, and the quiet gait is the inner circle. Optional RUN button (setting). |
| E | Door / key | **USE** button, shown only while the dot is on something; its label is the verb, keyed on the map's `AimKind` (OPEN / SHUT / TAKE / LOCKED / BROKEN / BREAK or TAP; 2026-10-22, H4). **A broken door** (the Relay broke it; it stays open for the run) shows **BROKEN** (C11, Red 2026-10-22): a press, or a tap on the leaf, is the won't-shut try (`UseResult.Broken`: no door verb, no noise, the map's 0.55 s guard); USE swings once and one soft knock is felt (§7, §10). **Keys on purpose (C14 on phones):** once 关卡设计's `KeyPickupNeedsE` line lands (with A12), USE says TAKE with the dot on a key, and a tap on the key, or within 22 pt of it (the 44 pt finger target), takes it; until then phones keep walk-over keys. **Tapping the door itself also works** (Apple HIG: tap objects instead of a selection button): a touch < ~200 ms that moves < ~3 mm on the look side, raycast from the tap point within reach. It takes the same road as E, the game's `UseDoor`: a shot that takes USE swallows the tap, and with the zone's key a locked door turns its key and starts the unlock shot (关卡设计, 2026-10-08). No auto-open: shutting a door on the Relay is a choice. |
| Hold E | Break glass | Hold USE; the ring fills **outside** the thumb (r 46 pt) and the bar under the dot. TAP mode (existing setting) = repeated taps on USE. |
| Walk into frame | Climb | Unchanged (stick into the broken frame). |
| S | Cancel a shot | Pull the stick back (same meaning as S). |
| Esc | Pause | Pause button top-right; Android Back steps back one screen everywhere (settings → pause → play; the restart question → pause; caught → a new run once TRY AGAIN is live; at the title, the root, it leaves the app); Android back (target API 36 no longer delivers `onBackPressed`/`KEYCODE_BACK`: register a predictive-back `OnBackInvokedCallback`, test how Unity surfaces it); app losing focus (already `OnApplicationFocus`). |
| O | Settings | SETTINGS chip on the pause screen; rows are tappable. Way back: the BACK chip (top-right of the card, drawn above it), a tap on the frost outside the card, or Android Back. |
| R | Restart | TRY AGAIN chip on the caught screen; RESTART on the pause screen asks to confirm (XAG 115: no one-tap destructive actions). |
| — | Quit to title | QUIT TO TITLE chip on the pause screen (Red's request, built by Codex; wording and treatment by 平面视觉, 2026-10-08) returns to the authored title corridor after a fresh scene reset. It sits centred 16 pt under RESUME / SETTINGS / RESTART on the solid ink chip with paper caps (an exit, not a go), 109 pt wide. It ends the run without asking today (open question 10). |
| Space / Return | Start | Tap anywhere on the title. Under the logo, TAP TO START (Bayon 17 paper with the keyline, no chip, centred at master y 306) and the two legend rows (§2.2) fade in 3 s into the title, and on a cold start never before 1 s after the loads are in (§3.2); they fade out with the title when play begins (touch only). A tap before the loads are in does nothing. |
| H, T (settings shortcuts) | — | Not needed: rows are tappable. |

### 2.1 Three gaits and what the Relay hears (Red, 2026-10-08)

Red: "跑步-声音最大，正常行进-中等声音，慢慢走-几乎没有声音" (run loudest, walk medium, slow almost silent), and it changes how the Relay tracks you. Figma TC16 "RUN PAST THE RING" (`2907:6556`).

| Gait | Thumb (from the centre) | Speed | The Relay hears (tier 1, walking path) | Footsteps (声音设计) |
|---|---|---|---|---|
| Slow | 7–36 pt (inside the slow ring) | 1.44 m/s (0.45 × walk) | nothing; it can still see you | the walk step at −15 dB |
| Walk | 36 pt out to the run line | 3.2 m/s, the desktop walk | each step: `relay.Noise` radius 9 → 12.6 m; a shut door (+9 m) all but hides it | as before |
| Run | past the run line, ahead ±70° | 5.5 m/s for 5 s of stamina, the desktop sprint | each step: the sprint noise, radius 26 → 36.4 m | the run step |

- One speed per gait, not analog: the stick sends move 0.45 or 1.0 toward the thumb. The slow/walk edge moves ±2 pt (hysteresis).
- Winded: the line refuses the thumb (drawn back to the ring) until breath is back; a thumb still past the line then runs again, with its click.
- Game side (`FrontRooms3DGame.Mobile.cs`): `FrontRooms3DGame.PlayerGait` (`FrontRoomsGait { Still, Slow, Walk, Run }`, set every frame next to `PlayerSprinting`; the sound chat reads it); `HandheldStepNoise()` makes the walk noise once per step. In Editor builds it goes through `AutopilotNoise(AutoCauseOther, …)` so a baseline can name the hunt (关卡设计调研). The walk radius is a const in Mobile.cs, owned by this chat (关卡设计 agreed, 2026-10-08).
- **Taught on the stick (X2, §2.2):** the label slot says SLOW · UNHEARD after 0.25 s of slow, until the player has crept for 1.5 s with the Relay out.
- **Handheld only.** Desktop walking stays silent and desktop has no slow gait (open question 9). RELAY_PURSUIT_REDESIGN v2's "walking is quiet / walk gain 0" now holds on desktop only; 关卡设计调研 passed this to 系统设计 for the director's Attention gains.

A finger that starts on USE may keep dragging to look (so the dot can stay on a pane while holding). A finger that starts in the look zone never presses USE (presses register on touch-down only).

### 2.2 Hints on the controls (X2, 2026-10-22)

The phone no longer explains itself on a card. The rules are in `INTERACTION_DESIGN.md` (X2 on phones); the spec is `~/FrontRoomsInteractionWork/x2/X2_PHONE_HINTS_SPEC.md` v3.3 (Figma board `3564:7362`, signed by 平面视觉).

- **No calm-hint card** (M1, landed jointly with 关卡设计): the phone hint strings and `CurrentCalmHint` are gone; an empty hint hides the context card.
- **One legend, title and pause**, IBM Plex Mono 11 caps (iPad 12), glyphs 22 pt centred on the caps, gaps 6 / 26 pt: row A `[move] MOVE  [drag] DRAG · LOOK  [slow] SLOW · UNHEARD  [run] PAST THE RING · RUN` (`[button] RUN BUTTON` in BUTTON mode), row B `[use] USE · DOOR  [hold] HOLD USE · BREAK GLASS`. Ink on the pause frost; paper with the keyline on the title.
- **The stick's label slot**, one word at a time: WINDED > the running RUN (yellow, STICK mode) > the RUN preview (paper, STICK mode, thumb ahead between the ring and the line) > SLOW · UNHEARD (after 0.25 s of slow; leaves 0.20 s after). Bayon 17 with the keyline, no chip. Placement is decided at touch-down: above the thumb if the swept word clears the edges, insets, the HUD's top band and captions, pause and the RUN button; else hung under the ring; else hidden for that touch.
- **They retire:** SLOW · UNHEARD after 1.5 s of counted creep (moving ≥ 0.5 m/s in Slow, in play, not winded) once the Relay has been released this run; the preview after the first real crossing or 4 s shown. PlayerPrefs `FrontRooms.Touch.Taught.Slow` / `.Run`, saved at the next pause or menu or when the app pauses. STICK HINTS (§6) brings them back.

### 2.3 EXAMINE close-ups by touch (contract T1–T5, 2026-10-22)

The rules are in `INTERACTION_DESIGN.md` (EXAMINE on phones); the contract is `research/inspect/contracts/touch_inspect.md`.

- **Opening:** tap the target (a plan, a screen, a sign), or aim at it and press USE, which then says **READ** (C14 / G10). The open is felt: one light tick (Gameplay 0.45).
- **In a close-up** (`MenuState.Inspect`): USE is **BACK**, moved to the lower right (`L.InspectBackCenter`: 28 pt × Scale right of USE and 44 pt × Scale above the safe bottom, kept inside the safe area by its hit radius (`KeepInsideSafe`), then clear of cameras and rounded corners (`ClearCircle`); (768, 337) from the top-left on the phone, Ø88 hit inside the safe area; on the iPad (×1.4) the disc sits 11 pt off the glass). The stick, pause and RUN button ease out (0.12 s); there is no frost.
- **Leaving:** BACK, the left-zone pull-back (the hidden stick's S) or Android Back.
- **Look and zoom:** the right zone drags to look; a two-finger pinch zooms continuously (160 pt × Scale of spread per zoom step); a double tap (two taps within 0.3 s and 40 pt) toggles the zoom. The double-tap detector resets on every menu state, so the tap that opened a close-up never also zooms it; single taps in a close-up never reach the game.
- **Desktop (same input, T3):** the mouse wheel gives ±1 zoom step per gesture (re-armed after 0.2 s of real time without scrolling) and the right button toggles. The zoom works once 关卡设计's C7 line (`ZoomInputWired`, `Tick(…, input.Zoom, input.ZoomToggleDown)`) is in `FrontRooms3DGame.cs`; until then the close-up offers no zoom step and these inputs do nothing.
- **TEXT and the transcript panel (T6/T7, 平面视觉 G9/G10 in `research/desktop_ui/EXAMINE_TEXT_G9_G10.md`):** a close-up whose target has a transcript (`FrontRoomsInspectCatalog.Transcript`) shows a **TEXT** chip over the panel's right end, where pause was when G10 was signed (`L.TextChipCenterBase`: (778, 28) from the top-left on the phone, hit 44; pause is hidden in a close-up). Like the HUD it gives way to a rounded corner (16 pt) or a cutout (12 pt), down and in, eased (`L.TextChipCenter`, `ClearCornerBlock`). Off it is the paper word with the HUD keyline and no fill; on it is a paper chip with the word in ink. A tap turns `FrontRoomsInspect.ShowTranscript` on or off for that close-up only (leaving the close-up undoes the chip's toggle).
- **The panel** (`L.TranscriptPanel`): the right 36 % of the safe area, from 64 pt under the top (and at least 12 pt under the TEXT chip) to 13 pt above BACK's hit circle (phone x 542–812, y 64–280 from the top); a reading surface (#0E0E0D at 97 %); 16 pt padding; region heads (lines starting U+0002, 游戏视觉's format) in IBM Plex Mono 11/14 caps; lines in Source Serif 4 17/20 (iPad 12/15 and 19/22: the ramp's meta and caption rows, §4); 12 pt between regions and 4 pt between lines; each text sits on its line box as on the board (Unity's first line is raised by half its natural-minus-pitch); glow lines (U+0001) in phosphor ink #D6DEBD with a 2 pt rule at the left, shown only below the ink gate (own light < 0.15). A drag that starts on the panel scrolls it; a drag elsewhere looks; a 12 pt fade where the text is cut, no scroll bar.
- **Open with 游戏视觉:** the close-up frames its object only when it opens, so turning the panel on mid-close-up does not yet move the object into the left 64 % (asked: a reframe when `ShowTranscript` changes); the region heads arrive with 游戏视觉's transcript follow-up (until then the panel shows the lines without heads).

## 3. Layout (phone, iPhone 16/17 Pro landscape, 874×402 pt)

Safe-area insets in landscape: L 62, R 62, B 21, T 0 (verify per model at runtime via `Screen.safeArea`).

| Element | Position (pt) | Size |
|---|---|---|
| Zone block (rule + meta + zone name) | top-left (78, 16). From 10-04 (c1f2d31a) to 10-22 the phone layout anchored it, the key line and the Relay readout to the bottom edge, so all three sat below the screen; 关卡设计 fixed the anchors (Game 5acc5147) and the playtest now checks the label is on screen | rule 3×40 |
| Key held | under the zone block (78, 72) — moved off the bottom-left, which belongs to the left thumb | 150×22 |
| Pause | centre 28 pt from the window's right edge (the bleed, Red 2026-10-24), 34 pt under the safe top: (846, 34) from the top-left on the phone; moves only for a camera or a rounded corner (§3.1) | 32 visual / 44 hit |
| Crosshair | centre | Ø12 white |
| Prompt / hold bar / stamina | under the dot (top edges): +13 / +47 / +55.5. The stamina dashes have their own line, centred 58 pt under the dot, always (平面视觉, 2026-10-22; Game b97aae15): at +21 they crossed the prompt whenever you were winded at a door. −48 is the hold bar's line (winded + hold to break glass is the chase escape) and −71 would enter the caption card. On phones the glass hold fades this whole group with the takeover, so the bar never shows beside the dashes; USE's ring carries the hold | bar 100×3, stamina 5 × 20×5 on a 25 pitch (row 120): desktop's 120×4 and 24×6 / 30 at the ×0.85 ramp (平面视觉, 2026-10-22; Game 938b0ee9; the build had ramped only the height) |
| Captions | centred, hanging from a fixed top 66 pt under the dot and growing downward: 5.5 pt under the dashes on every aspect, and two lines still end above a two-line flash (平面视觉, 2026-10-22; Game 938b0ee9). Until then the card sat on bottom 99 (MobileMotionVariations/SAFE_AREA_LAYOUT.md, 10-04) and grew upward, so one line touched the dashes and two covered them | 214 wide, 10 + 20 × lines tall (1–2 lines: 30 / 50) |
| Stick (resting ghost) | centre (150, 290) | base Ø120, slow ring Ø72, run line r 84 (±70° ahead), thumb Ø52 |
| USE | centre (740, 262) | Ø72 visual / Ø88 hit, hold ring r 46 |
| Stick label slot | above the thumb (+33·Scale + 14), or hung under the ring (cap top at origin − 60·Scale − 14), decided at touch-down (§2.2) | Bayon 17 + 1 pt keyline |
| Title lockup | TAP TO START centred at master y 306; legend row A's cap top 16 pt under its baseline, row B a glyph box + 8 pt lower | Plex Mono 11, glyphs 22 |
| Hint card | none on phones since X2 (§2.2): no calm hint. The slot carries only the glass flash line: paper with the keyline, no card (A6, X2 step 2), centred and sitting on the slot's bottom edge 29 pt up (8 above the iPhone's 21 pt safe area), so a second line grows upward; two captions end at least 7 pt above a two-line flash, so the instruction never reads as a third caption line; phones break the glass flash before TO (平面视觉, 2026-10-22) | 410 wide |
| Top band | y 0–64 | read only; the only control is pause |

Android 20:9 (915×412 dp): insets from `WindowInsets`; keep the stick's touch area ≥ 24 dp from the long edges, where the back gesture lives (both edges in landscape), and exclude at most the stick band from system gestures (Android limits exclusion to 200 dp per edge). iPad 11" (1180×820 pt): controls at 1.4× (stick base Ø168, USE Ø101 / hit Ø123), same corner positions; HUD uses the tablet ramp; margins 32–40 pt. iPadOS 26+ windowing and foldables mean the layout must re-flow on every size change.

### 3.1 iPhone Duo: full bleed, and only what meets a camera gives way (Red, 2026-10-24)

Red, on the Duo's outer display: everything stays full bleed; content that would collide with the camera is pushed away, and content that doesn't stays where it is, in every rotation and on the outer display alone. When a rotation or a scroll brings the camera onto something, it moves there with a motion transition. The pause button sits at the right bleed; the pause card is centred on the whole screen; settings is one black frosted sheet whose list runs to the bottom edge.

- **What iOS 27.1 reports (Duo simulator, measured):** outer display 678 × 466 pt @3. At launch its front camera is only a safe-area band along the edge it sits on (B 82 with the camera bottom right, T 82 turned over), with no region and no corner radii. Once iOS settles it moves the band to the side (L 84 turned over) and reports the camera as an occlusion region (37 × 37 pt) inside an 84 × 82 pt keep-out box (a second region), plus the corner radii (59 / 8). The home inset is 34. Inner display 951 × 669 pt @3, safe B 34, corners 55; its FaceTime camera is an occlusion region (58 × 37 pt at the top, landscape) that is active only while the camera runs; the fold is a division region, inactive when flat.
- **The display shape** (`FrontRoomsHandheld.Shape`): cutouts = the occluding elements themselves (of nested regions, the innermost: the camera, not its keep-out box) + the outer camera before iOS reports it (Ø37 pt, centre 47.83 pt from the native top and right edges, mapped per interface orientation); corners = iOS's, or until then the outer display's 8 pt hinge side / 59 pt far side; the **bleed safe area** = iOS's with every band a camera explains given back, on whichever edge iOS put it (the bottom keeps the home inset, 34). Only on the Duo's own displays: other phones keep the insets their signed masters use. Everything lays out on the bleed safe area.
- **Giving way** (`ClearRect` / `ClearCircle`): 12 pt clear of a cutout, 4 pt (controls) or 6 pt (text) inside a rounded corner; the in-play HUD's corner parts keep **16 pt from the curve** (`HudCornerGap`, Red 2026-10-24 on the inner display: the HUD sat on the curves; `research/interaction/duo/rounded_corners`): pause slides along the corner's diagonal, and the zone text and threat readout move down and in along the corner (`ClearCornerBlock`: away from the top edge at most to the diagonal inset r − (r − 16)/√2, then sideways only as far as still needed), the key line moving with the zone. Settings' BACK chip, anchored in the top-right corner, moves down and in the same way (`ClearCornerBlock`), and the SETTINGS heading keeps the same 16 pt while it gives way sideways (2026-10-24 08:2x, finishing option 1; it had kept the old 6 pt). Text moves sideways toward the middle; a settings row narrows from the side that meets the camera (its label and value keep their own edges); a round control slides along the corner's diagonal or out the shorter way. What meets nothing does not move.
- **Who gives way:** pause, USE / inspect BACK, RUN, the stick's rest and a floating stick's ring; the settings heading, BACK and every row, where it is scrolled to; the HUD's zone, key, threat readout and flash line, cleared from the text they draw (per frame, so a late corner radius, the inner camera turning on or a longer zone name all count). The stick's word keeps out of cutouts at touch-down.
- **Measured places:** outer, camera bottom right: nothing on the play screen moves (pause stays at the bleed, 8 pt corner); settings rows that reach the camera end 12 pt before it. Turned over: the zone and SETTINGS move right to 12 pt past the camera (the zone also 12.6 pt down for the 59 pt corner); pause draws in along the 59 pt corner to 16 pt from it (19 pt from the bleed spot). Inner (55 pt corners): the zone text sits 28.9 pt from the left and 27.4 pt from the top (was 26.8 / 16), the pause keycap 25.6 pt from the right and 28.5 pt from the top (was 16.1 / 21.2), each 16 pt from the curve (VL435).
- **Settings:** the veil turns black at 86 % over the blurred frame; the list runs to the bottom edge with no fade, its last row scrolling to the home inset + 8 pt; the heading keeps 10 pt under the safe top. A tall window shows every row and does not scroll.
- **Field of view:** views squarer than 16:9 keep 16:9's horizontal field (`FrontRoomsHandheld.AspectFov`, through the camera rig): 76° draws 87.6° on the outer display, 88.7° on the inner, 100° (the cap) upright. Wider views and desktop draw it as authored.
- **Logs:** `[HandheldNative]` (UIKit's view, safe area, radii, regions) and `[HandheldLayout]` (what the layout is built from) print one line per fold, rotation or resize.
- **Figma:** `DUO FLOWS` (section `3655:7752`, page 2099:76, under TOUCH FLOWS): every Duo pose and state, with what gave way.

### 3.2 Handheld cold start: the logo first, then the room (Red, 2026-10-24)

Red: no long black screen on opening the app; show the logo at once, the game once its graphics are in, and TAP TO START and the small text only once everything is loaded.

- **The launch screen** (iOS, `Assets/Editor/FrontRoomsLaunchScreen.cs`, a post-build step): black, with the white FRONTROOMS wordmark (`Assets/Editor/iOS/LaunchLogo@2x/@3x.png`, rendered from `Resources/Brand/FrontRoomsLogo_Left.svg`) exactly where the title's vector lockup draws it before the trailing S relay (`ApplyMobileSafeAreaLayout`): centred across, its 965 × 192 box 14 pt above the middle, 540 pt wide or the width less 32 when narrower. It replaces Unity's black 1 × 1 launch image; iOS shows it from the tap. (Red, 07:05: the first version centred it, 14 pt below the game's logo; fixed, measured identical in landscape, VL436.)
- **Duo inner display held upright:** with `requiresFullScreen` on (since 06:34) iOS letterboxes the landscape-only game there, its 951 × 669 window scaled to the width (0.70) between black bars, while it draws the launch screen upright at full size. So the storyboard follows that letterbox in any portrait view: the logo box is 0.56782 × the width (380 pt) and its lift 0.025926 × its width (9.85 pt), exactly the letterboxed game's. Hidden helper views switch it on, since B = max(0, width − height) is 0 only in portrait (Red ~07:11: "现在这个内屏的位置是不对的"; 07:4x). Laid out in UIKit from the compiled storyboard on the Duo simulator: upright 379.67 pt, 9.83 pt above the middle; landscape 540 pt, 14.00 pt. The letterboxed game draws it at 148–489 pt across and 12.7 pt (painted) above the middle, which the storyboard matches.
- **The first frame** is the same picture: the logo on the startup curtain (black, in the logo's UI Toolkit panel). Awake builds only what the title needs; the room draws behind the curtain.
- **Once the app is on screen and the room has drawn three frames** (its shaders are compiled by then) the curtain eases out over 0.6 s: the held room behind the logo. Then the map's first-use loads (`FrontRoomsMapWorld.Prewarm`) run while nothing moves, and the title starts: the stream rooms, the door and the S relay. TAP TO START and the legend come at the later of 3 s into the title and 1 s after the loads; a tap before does nothing.
- **Only on a handheld's cold start.** A retry (scene reload) and desktop start as before. The log prints each startup step: `READY · … · startup · look + fonts … ms · camera … · materials … · hud … · title room …` and `READY · handheld start · room shown at … s · map prewarm … ms`.
- **Measured (iOS 27.1 Duo simulator):** before, black from 0.3 s to 12.7 s after the tap, the room appearing with the prompts already on. After, the logo from 0.3 s. On the first launch after install (cold shader cache) the app came on screen at 8.7 s and the title started at 10.6 s; on the next launch the room showed at 3.0 s and the title at 4.5 s (Awake: materials 525 ms, title room 448 ms; map prewarm 914 ms after the reveal) (VL434).

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

- `Touch / Stick` (2530:3828) — State = Idle (ghost) / Slow (slow ring lit) / Walk (slow ring dashed, run line dashed) / Run (thumb past the yellow run line, label RUN) / Winded (muted, label WINDED).
- `Touch / Use` — State = Open / Shut / Take / Locked / Hold / Tap mode / Pressed.
- `Touch / Prompt` — Tap (mini-USE glyph replaces the keycap E) / Hold (ring glyph) / Locked (no glyph, muted).
- `Touch / Pause`, `Touch / Chip` (text property Label), `Touch / Run button` (Off / On / Winded; it was `Touch / Sprint toggle`).
- `HUD·T / Zone`, `HUD·T / Key`, `HUD·T / Hint card`, `HUD·T / Captions` (Size = Phone / Tablet), `HUD·T / Crosshair`, `HUD·T / Stamina`, `HUD·T / Hold bar`.
- Device overlays (iPhone island + home indicator, Android punch-hole + handle, iPad home indicator) and safe-area guides.

## 6. Settings (new TOUCH section, PlayerPrefs like `FrontRoomsSettings`)

| Row | Values (default first) |
|---|---|
| LOOK SPEED | 100 % (60 / 80 / 100 / 120 / 150; 100 % = 0.20° per pt dragged) |
| INVERT LOOK | OFF / ON |
| GYRO LOOK | OFF / WHILE TOUCHING / ALWAYS |
| STICK | FLOATING / FIXED |
| RUN | STICK (push past the ring) / BUTTON (the PlayerPrefs key keeps its old name, `TouchSprintSocket`) |
| STICK HINTS | ON / OFF (X2): ON while a word the current mode can show still teaches (SLOW · UNHEARD; in STICK mode also the RUN preview). ON clears both taught flags, OFF sets both. The only row that can change by itself (a word retires); shown on the next open |
| CONTROLS SIZE | 100 % (80 / 100 / 120 / 140) |
| CONTROLS OPACITY | 34 % (20 / 34 / 50 / 65); 34 % draws the Figma state alphas exactly. It thins only the text-free ghosts (stick, rings, run line, hold track, pause keycap); a control that carries a word (USE, the RUN button) stays solid at any setting, so a low setting never makes a verb illegible |
| LEFT-HANDED | OFF / ON (swaps sides) |
| HAPTICS · GAMEPLAY | ON / OFF (door, glass, lock-on, caught) |
| HAPTICS · CONTROLS | ON / OFF (the run click, USE, menus) — two switches like Alien: Isolation; hidden on iPad (no haptics) |
| INTERACTION ASSIST | OFF / STEADY (look gain ×0.5 within ~4° of a door handle or pane; renamed from SLOW in X2, so SLOW means only the gait) — **not built yet** |
| EDIT LAYOUT | opens the layout editor (T3) — **not built yet** |

The existing BREAK GLASS row reads HOLD USE / TAP USE on touch. With iOS Reduce Motion on, CAMERA MOTION's first-launch default is OFF.

As built, the settings card is the desktop card's twin: the game's own rows in the left column, TOUCH in the right column (which scrolls with inertia). Tap a row to step it (on/off rows flip); on a stepped row ‹ steps down and the rest of the row steps up. Rows that can't do anything are hidden (the haptic rows on iPad).

## 7. Haptics (event → pattern)

| Event (existing hook) | Pattern |
|---|---|
| Door opens (`OnDoorMoved`) | one light transient (≈0.5) at the latch |
| Locked rattle (`rattleJolts`, Rattle.Jolt1/Jolt2) | two firm transients on the camera jolts |
| Glass hold (`map.Hold` progress) | continuous 0.2→0.6, transients at ⅓ and ⅔, heavy 1.0 on `OnGlassBroken` |
| Run | a rigid click as the thumb crosses the run line; soft double when `winded` |
| Relay lock-on (pursuit redesign cue) | firm double pulse, chase only |
| Caught | 0.8 s heavy fade |

**iPad has no haptics**, so every haptic also exists as sound or picture (the ring, the bar).

As built (2026-10-07), `FrontRoomsMobileHaptics` plays these, with two switches (GAMEPLAY / CONTROLS) and at most one pulse per 30 ms per channel:

| Moment | Channel | Pattern |
|---|---|---|
| USE press, and tapping a door itself | Controls | rigid 0.55 |
| The thumb crosses the run line (or RUN goes on) | Controls | rigid 0.8 |
| Winded | Controls | soft 0.5, then soft 0.35 at +120 ms |
| A close-up opens (`FrontRoomsInspect.Opened`, EXAMINE T5) | Gameplay | light 0.45 |
| Menu chip or settings row | Controls | selection tick |
| A door near the player latches shut | Gameplay | light 0.5 |
| Locked rattle | Gameplay | rigid 0.8 on `Rattle.Jolt1` and `Jolt2` |
| A broken door won't shut (`DoorBrokenTried`) | Gameplay | soft 0.6 at +90 ms, as USE's swing meets the frame: duller than the locked rattle |
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
| Geometry | `FrontRoomsTouchLayout.cs` | §3 in points: stick (slow ring 36, ±2 hysteresis; run line 84; thumb reach 88), USE (Ø72 / hit Ø88), pause (32 / hit 44), zones, settings card; left-handed mirror |
| Input | `FrontRoomsTouchControls.cs` | finger ownership by touch-down zone; floating / fixed stick; three gaits and the run line (§2.1: in at the line ahead ±70°, out inside the ring or on a pull-back or lift, refused while winded); RUN button mode; look (0.20° per pt × LOOK SPEED, gyro); USE tap / hold / locked; tap-the-door; pause; menus; settings rows and scroll; writes `FrontRoomsInput`'s virtual snapshot |
| Picture and motion | `FrontRoomsTouchControlsView.cs`, `FrontRoomsTouchMotion.cs`, `FrontRoomsTouchSprites.cs` | the TK / TS masters drawn at device density, every transition in §10 |
| Game side | `FrontRooms3DGame.Mobile.cs` | the host (rows, pause meta, caught stats), prompt → USE verb, restart confirmation, QUIT TO TITLE (Codex), Back, tap-the-door, haptic hooks on the map's and Relay's own events, `PlayerGait` and the handheld walk noise (§2.1) |
| Haptics | `FrontRoomsMobileHaptics.cs`, `FrontRoomsMobileInteractionEvents.cs` | §7 |
| Android Back | `FrontRoomsMobileBackBridge.cs` (Codex's, type fixed) | predictive-back callback → one step back (confirm → settings → pause ↔ play) |
| Playtest | `FrontRoomsTouchPlaytestDriver.cs`, `Assets/Editor/Input/FrontRoomsTouchPlaytest.cs` | §11 |

`FrontRooms3DGame.cs` changed only where 关卡设计调研 agreed: the platform test, the safe-area pixels, the prompt and hint strings shown on touch, and one `OnDestroyMobile()` call. Level profile, map start, autopilot and the Relay are untouched. The gaits (2026-10-08) add four one-line hooks, agreed with 关卡设计 (the file's owner) and 关卡设计调研: `PlayerGait = GaitOf(…)` after `PlayerSprinting = sprinting;`, `if (!sprinting) HandheldStepNoise();` in the footstep block, and `PlayerGait = FrontRoomsGait.Still;` at the two stamina resets.

**Mobile texture budget (游戏视觉, 2026-10-07).** The Q1b wallpaper print is in main:
- `_PrintTex` on iOS is now ASTC 4×4, about 5.3 MiB (it was uncompressed RGBA32, 32 MiB).
- `Resources/Print/FR_Print_HardEdge`, an 8-frame array, is ASTC 4×4 2048² × 8 on iOS, about 42.7 MiB. It sits in Resources, so it ships in the package.
- iOS was tested (slice 0 matches the static map, 0 of 262,144 samples differ); Android wasn't.
- Mobile import settings are at their defaults and belong to the mobile tier. Change only the iOS/Android tabs (e.g. ASTC 6×6 / 8×8, or 1024²) once device profiling says how much memory a phone can spare.

**Still to do:** run on devices (two thumbs, gyro, haptics, notch and island insets, Android `WindowInsets` and gesture exclusion), the EDIT LAYOUT editor, INTERACTION ASSIST, Core Haptics (AHAP) for the glass, FMOD mobile banks, the mobile render tier, and the export (`Assets/Editor/FrontRoomsMobileBuild.cs` has the menus; nothing has been built).

## 9. Open decisions for Red

1. ~~Sprint default: the socket latch or a button?~~ Decided by Red, 2026-10-08: push past the ring (§2), with a RUN button as an option.
2. Gyro look off by default (recommended)?
3. Frame target: 60 fps on a recent iPhone, or 30?
4. Landscape only (recommended)?
5. Bluetooth/MFi controllers on mobile in v1?
6. TestFlight / internal testing only, or the stores?
7. Break glass on touch: HOLD USE by default (recommended: a held button is natural on glass; XAG asks for holds under 2–3 s) or TAP USE?
8. Android needs Unity's new Input System project-wide: migrate desktop input too, verified to feel identical (recommended), or ship iOS first and decide later?
9. Desktop parity for the gaits (§2.1): should desktop get the slow gait (e.g. hold Ctrl) and the walk noise, so both platforms share one stealth rule? Today desktop walking is silent, so a phone player is heard where a desktop player is not.
10. QUIT TO TITLE ends the run like RESTART, but RESTART asks first (XAG 115: no one-tap destructive actions). Ask the same way, e.g. LEAVE THIS RUN? CANCEL / QUIT (平面视觉's suggestion; recommended)?

## 10. Motion (as built, 2026-10-07)

Rules: every change moves, nothing pops; controls answer within one frame (70–90 ms to full); menus build in reading order; springs (OutBack) only where a thumb lets go or something locks in. All motion runs on the touch layer's own unscaled clock (`FrontRoomsTouchMotion.Now`), so menus move while the game is paused. Curves: OutCubic (arrivals), InCubic (exits), OutBack / OutBackSoft (springs, overshoot constant 1.70 / 1.1), InOutSine (breathing).

**Reduce Motion** (iOS Reduce Motion, Android "remove animations" (animator duration scale 0), or CAMERA MOTION at 0 %): alpha only, every duration capped at 108 ms, no rises, scales, springs, shakes or pops, numbers appear at their value.

| Element | Moment | Motion |
|---|---|---|
| Controls layer | play starts / a menu opens | fade in 180 ms OutCubic / out 120 ms InCubic |
| Stick | thumb down | the live stick appears under the thumb: alpha 90 ms, scale 0.88 → 1 in 140 ms OutBackSoft; the resting ghost fades out 90 ms |
| Stick | thumb up | thumb springs home 160 ms OutBackSoft; live stick fades 180 ms InCubic; ghost returns 220 ms after 60 ms |
| Stick | slow | the slow ring (Ø72) goes from 25 % dashes to a solid 60 % ring in 100 ms, back in 140 ms |
| Run line | reaching | ahead of the ring the thumb follows the finger to just over the line (88 pt), and the dashed line firms from 35 % to 75 % as it nears |
| Run line | crossing | the line turns solid yellow and the thumb yellow in 120 ms; thumb pops 1.15 → 1 and the line 1.04 → 1 in 220 ms; RUN fades in 140 ms rising 6 pt above the thumb (after the preview it is already there and turns yellow in place); one rigid click |
| Run line | back inside the ring | yellow out 150 ms InCubic, label 100 ms |
| Stick | winded | tints to muted 200 ms; label becomes WINDED in paper with the keyline (muted vanished on lit wallpaper); the run line refuses the thumb (drawn back to the ring) until breath returns |
| Stick label | SLOW · UNHEARD / the RUN preview (X2) | in 140 ms OutCubic rising 6 pt; SLOW only after 0.25 s of slow. A higher word cuts the current one at once (alpha 0 that frame) and fades in 140 ms; an equal or lower word waits for the current one to fade out (100 ms InCubic), frozen where it stands |
| USE | appears / goes | 160 ms alpha + scale 0.86 → 1 in 200 ms OutBack / 120 ms InCubic to 0.94 |
| USE | press / release | yellow fill and ink verb in 70 ms, scale 0.92 / fill out 140 ms, scale back 180 ms OutBack |
| USE | verb changes (OPEN ↔ SHUT) | crossfade 120 ms |
| USE | camera takeover (a door, unlock or glass shot) | fades with the desktop HUD (the rig's `HudFade`, 150 ms) over 120 ms and comes back with it, so it never shows a verb a press wouldn't do; presses still go to the rig. Not under a holding thumb: the glass hold is a takeover too, and its ring is the hold's feedback (关卡设计, 2026-10-08) |
| USE | locked press | shake 3 cycles, ±4 pt decaying, 240 ms, with the rattle haptics; the press never fills (no yellow "go"; a paper tint greyed the solid disc and took LOCKED to 2.6 : 1): its text-free ring flashes to full paper instead |
| USE | broken try (the map answers Broken for the door USE names) | swings once round its top point, the bottom inward: out to 4° in 90 ms, then it meets the frame and falls back, a 2.6 Hz swing with damping 0.3, still by 550 ms (B·4's won't-shut, `BrokenDoor.Duration`). The verb stays muted and the press never fills, as for LOCKED. Reduce Motion: no swing; the ring flash and the knock carry it |
| Hold ring | hold | track fades in 120 ms; the arc follows progress (60 ms smoothing), outside the thumb at r 46 |
| Pause keycap | press | scale 0.9 in 70 ms, back in 160 ms OutBackSoft |
| Frost | pause / settings / caught | fades in over the live frame in 240 ms (420 ms when caught) OutCubic, so the room frosts over; out 160 ms (see the backdrop note below) |
| Pause card | opens | title (+40 ms, rises 18 pt), meta (+70, 12), legend rows (+100 / +130, 10), chips RESUME / SETTINGS / RESTART (+160 / +190 / +220, rise 12, scale 0.96 → 1), QUIT TO TITLE below (+250, same chip motion); each 260 ms OutCubic |
| Restart question | opens | RESTART THIS RUN? (+40 ms), CANCEL / RESTART (+50 / +80) |
| Settings card | opens | card fades in 260 ms and scales 0.97 → 1 in 220 ms; rows rise 8 pt, 24 ms apart from +60 ms; BACK (+80 ms) |
| Settings row | press / change | pressed tint 60 ms in, 200 ms out; the value crossfades and slides 6 pt in 140 ms; the active row's yellow rule 120 ms |
| Touch column | scroll | inertia (velocity decays with a 220 ms time constant); a scroll bar shows while it moves and fades 400 ms after 600 ms still. Since 2026-10-24 the list runs on under the bottom edge with no fade (Red), so a row cut by the edge says it goes on |
| Give way | a camera or a rounded corner meets it (a rotation, a fold, a row scrolling past) | eases to its cleared place in 280 ms OutCubic, and back when it no longer has to; snaps off screen and under Reduce Motion (§3.1) |
| Caught card | opens | title (+80 ms, drops in 12 pt), subtitle OCCUPANT ACCOUNTED FOR (+160 ms, the same drop), the four numbers count up over 600 ms from +200 ms, rule (+300), sentence (+340); TRY AGAIN at +600 ms and ignores taps before then |
| Title | waiting | TAP TO START (paper with the keyline) and the legend rows fade in 3 s into the title, on a cold start no sooner than 1 s after the loads (§3.2) (600 ms InOutSine), then stay still. It doesn't breathe (平面视觉: no alpha ramp on words; the corridor already moves). Any future "waiting" signal may only be a hard on/off of a solid element, e.g. a paper block cursor 0.6 s on / 0.6 s off, stopped by REDUCE FLASHING |
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

**Since X2 (平面视觉, 2026-10-22) the stick's label has no chip:** every slot word is Bayon 17 paper (RUN yellow) with a 1 pt `#141414` keyline at `FrontRoomsHudStyle.KeylineAlpha` (Unity `Outline`, distance 1 × Scale), and TAP TO START is drawn the same way. Before X2 it sat on a solid ink chip:
- fill `#0E0E0D` at 100 %, square corners;
- 4 pt above and below Bayon 17's caps and 7 pt either side;
- RUN in accent yellow, WINDED in paper `#F4F1E8`; it rides above the thumb;
- it fades in and out with the label.
- In code, the chip hugs the caps measured from the glyph quads (`Caps()`), not Bayon's line box.
- If the chip ever crowds the ring, drop the label to Bayon 15 before shrinking the padding.
- It replaces a 1 pt shadow: paper on lit Level 0 paper measured about 1.9 : 1, and shadows aren't part of the system.
- Figma: `Touch / Stick` State=Run and State=Winded (2530:3812, 2530:3823) carry the chip, and so does the Run screen master through its instance.
- The chip's height is Bayon's cap height (0.714 em) + 8 pt = 20.1 pt at 17 pt. Glyph quads and rasterised glyph bounds both carry padding (runs 11–12 drew 21.3 pt); only their middle is used.

**Text-bearing controls are solid (平面视觉, Q1, 2026-10-07).**
- The rule is about text and the ground under it, so text-free ghosts stay translucent and are scaled by CONTROLS OPACITY, as §1 rule 3 says.
- Anything that carries a word is solid:
  - the USE disc is `#0E0E0D` at 100 % behind its verb (paper; LOCKED in muted, 9.9 : 1);
  - the RUN button's disc is solid too (ink, or yellow when on);
  - TAP TO START was on the stick label's ink chip until X2; it is now paper with the keyline.
- Figma: `Touch / Use` (2530:3854) and `Touch / Run button` (2530:3877) discs are solid; the Title screen master (2532:5101) has the prompt on the chip.

## 11. Verification: the touch playtest

`FrontRoomsTouchPlaytest` plays the real game and map in Play Mode on a handheld profile, with multi-finger touches injected on a virtual touchscreen. It checks what each gesture did to the game, and captures composited frames (room, HUD and touch layer, blended exactly) at fixed 1/60 s steps, so every motion frame lands on its intended time.

- **Run it** in a private copy of the project, never in the copy Red has open:
  `Unity -batchmode -projectPath <copy> -buildTarget OSXUniversal -executeMethod FrontRoomsTouchPlaytest.RunBatch -touchProfile iphone|android|ipad -touchIsolatePrefs` (no `-quit`; it exits with 0 when every check passes). From the Editor: FrontRooms 3D › Mobile › Run touch playtest. Output goes to `Verification/touch-playtest/<profile>/`: the frames and `report.json` (checks, frame times, every haptic with channel, style and intensity).
- **Try it by hand** with FrontRooms 3D › Mobile › Touch preview in Play Mode (iPhone 16 Pro). The mouse becomes one finger; set the Game view to 2622 × 1206 for the true layout. The preview is off by default and never applies in batch or autopilot runs.
- **What it checks** (iPhone 16 Pro profile):
  - title tap;
  - slow: near the centre the gait is Slow in the touch layer and the game, about 1.44 m/s, and no walk noise reaches the Relay;
  - walk: out to the ring walks at 3.2 m/s, and each step is a noise at radius 9; past the ring but short of the line still walks;
  - run: across the line runs at 5.5 m/s with exactly one rigid click on the controls channel; between the ring and the line it holds; back inside the ring it walks; a strafe past the line never runs;
  - winded: WINDED shows, the soft double haptic plays, the run stops, and a thumb still past the line runs again once breath is back;
  - look: 100 pt = 20°; two thumbs at once;
  - pause, settings (the desktop rows tap, ‹ › step, the TOUCH column scrolls), QUIT TO TITLE hit-test and its dark chip drawn (pixel-sampled against the frost), restart confirm and cancel, resume;
  - every way back: BACK is drawn on the card (its pixel colour is sampled, since a hidden chip once passed the tap test), BACK and a tap on the frost outside the card return to pause, Back on the title leaves the app, Back on the caught card waits 0.6 s and then retries (a retry goes straight back into play; only a fresh launch shows the title);
  - Android Back through the bridge's own queue: pause, close settings, cancel restart, resume;
  - tapping a shut door itself opens it (a shut leaf always faces the player); USE then shuts it, the verb crossfades and the latch is felt; a locked door rattles, is felt twice and shakes USE with a paper flash (the harness switches keys on for that step only, since the shipped profile has none); holding the zone's key, a tap on that door starts the unlock shot, which ends by itself;
  - HUD: the level label is on screen, in the top-left (2026-10-22; it had sat below the screen on phones since 10-04);
  - a broken door (the map's own BreakDoor, from the far side; never C22's EXIT door, and the doors and locked steps skip it too, since a stand inside the stairwell changes the floor): the stand is confirmed by the game's own crosshair (the gameplay eye `rig.BaseEye` through `AimCast`; the key steps use `KeyOnRay`), re-aimed once or moved to the next spot, because after C22 a leaf could sit near edge-on and the 0.25 m stand check let the 0.30 m controller shift; USE says BROKEN; a press is the map's Broken answer once, and a second press 0.25 s in is dropped; the door stays broken and open; USE swings once (≥ 3°) and rests; the press's click, then one soft knock about 0.09 s later; past 0.55 s a press tries again; a tap on the leaf is the same try;
  - keys (when `KeyPickupNeedsE` is on for handhelds; skipped with a note until 关卡设计's line lands): USE says TAKE on a key and takes it, a tap on the key takes it, walking over leaves it, a tap up to 22 pt off still takes it;
  - holding USE on a pane breaks it in 1 s, with the rising buzz, two cracks and the heavy hit (the stand matches the desktop autopilot's glass scenario);
  - takeovers: USE fades with the rig's HUD fade during the unlock shot and stays under the thumb that holds glass;
  - caught: the stats count up, an early tap is ignored, TRY AGAIN restarts; the subtitle sits under CAUGHT at master y 97; the third number is KEYS TAKEN (keys taken this run); the block below the subtitle is 10 pt lower (numbers 135, labels 164, rule 196, sentence 235) and TRY AGAIN with it, still ≥ 8 pt above the safe bottom; the sentence keeps a floor token's capitals ("Level B1 / The maze") and the start rooms' name ("Level 0 / Front Rooms");
  - Reduce Motion: the chips fade in place.
  - X2 hints (2026-10-22): no calm card in play; the title lockup (TAP TO START at master y 306, both legend rows on screen with ≥ 8 pt above the home inset, keyline on); the pause legend word for word; SLOW · UNHEARD after its dwell and never in 20 walk starts; it does not retire before the Relay is released, and does after 1.5 s of counted creep; the RUN preview in the reach band, turned yellow in place at the crossing and retired by it; a higher word cuts in (RUN full within 0.14 s); back in the slow band the yellow RUN only fades out; BUTTON mode with a fixed stick hangs SLOW under the ring; CONTROLS OPACITY never thins the words; a stick started high (132 pt × Scale under the top) puts every word below; STICK HINTS steps OFF / ON / OFF and its flags; the preview retires after 4 s shown, and STICK HINTS back ON gives it a fresh 4 s; the Relay's real release. The creep checks first face the longest free walk (a creep into a wall rightly never counts), and every stick offset scales with the controls (× 1.4 on iPad).
  - Phone HUD (2026-10-22, with 关卡设计's Game hunk): tired at a door, the dashes sit under the prompt; they are 20 on a 25 pitch, their row 1.2× the hold bar, the bar 3 to their 5; with a caption up, the card hangs 5.5 pt under the dashes, one line 32 tall; two captions during the glass flash, the 52 pt card ends above the flash text; the glass flash line is paper with the keyline, no dark card.
  - Motion checks read node positions: for example, the RESUME chip is still low at f08, nearly home at f18 and home at f30.
  - The Relay is held dormant for these steps (a runtime flag on that run's Relay; no game code changes). The steps teleport the player around a live map, and in run 9 the Relay caught it halfway. The caught step calls the game's `End()` itself.
  - EXAMINE TEXT (T6/T7, 2026-10-22, on a test transcript in 游戏视觉's format, put back after): the TEXT chip shows in the pause slot when the close-up has a transcript; TEXT opens the panel at the right 36 % of the safe area, ending above BACK's hit circle; on, the chip is paper with the word in ink; four region heads in Plex Mono and the lines in Source Serif; the glow line only below the ink gate, with its rule; a drag on the panel scrolls it and a drag beside it does not; TEXT again closes it; leaving the close-up turns the chip's transcript off.
  - EXAMINE (contract T1–T5, 2026-10-22): the stand is 1 m square to the nearest close-up target, turned 12° so it lands in the look zone, the camera settled two frames, read in viewport coordinates and aimed at the aim box (`BoxCentreWorld`), with the floor ray skipping the player's own capsule; a tap opens the close-up with one light tick; USE is BACK at `L.InspectBackCenter`; no frost, pause and stick gone; a pinch zooms continuously and a double tap toggles (when the zoom is wired, C7); BACK, the pull-back and Android Back each leave; a double tap on the target opens it without zooming it; aimed square, USE says READ and its press opens the close-up. `-touchFocus examine` runs only these checks after setup (for quick rounds; a full run lands).
  - Latest: T6/T7 runs r2 (2026-10-24, rebased onto main after the iPhone Duo + corners landings, clone W/proj_t67): touch iPhone 152/158, the 6 failures all in the broken-door block, whose code the package does not touch (today's map hands the stand an edge-on leaf the crosshair loses; the desktop prompt is empty too; followed up separately); EXAMINE focus iPad 39/39, Android 41/41, Duo outer 41/41 (real corners + camera). On the 10-22 main: iPhone 151/151, Android 151/151, iPad 148/148, desktop autopilot 2554 PASS. Before that, runs g1 + h1, 2026-10-22, on main after X2 (Game 938b0ee9) + this landing, without 关卡设计's C7 (the zoom then offers no step, so the pinch and double-tap checks skip): touch iPhone 16 Pro 141 / 141 (EXAMINE 10 / 10), and again EXAMINE-only on main after 游戏视觉's placard_phase0 (21:03): 10 / 10; desktop autopilot 2554 PASS; 游戏视觉's FrontRoomsInspectPlaytest 116 passed, 0 failed. With C7 simulated (W/proj_ex_c7) the EXAMINE checks were 12 / 12 on iPhone, pinch and double tap included. Before that, run land3, 2026-10-22, in `W/proj_x2land` = main after M1 + the Bayon "·" fallback + 平面视觉's phone HUD hunks (Game 938b0ee9) + X2, the files that landed at 20:1x: iPhone 16 Pro 153 / 153, Android 20:9 153 / 153, iPad 11 153 / 153; Roslyn on main 0 errors for macOS and iOS; desktop autopilot 2554 PASS (ended by time; the same maze and EXIT as main; no handheld line in its log). VL415–VL417 (Figma 3616:7760, 3616:7771, 3616:7782). Before that, runs c5–c6, 2026-10-22, on main after C22 (Game 670219ab) + the caught-card landing: iPhone 96 / 96 with 关卡设计's `KeyPickupNeedsE` line (the configuration that failed 6 broken-door checks in its run before the stand fix), Android 20:9 86 / 86 without it; desktop autopilot 2554 PASS. Before that, run r1, 2026-10-22, 85 / 85 checks on iPhone and 85 / 85 on Android 20:9, 85 frames, on main 5acc5147 + the touch landing (VL353, Figma 3520:7362): the level label on screen, the broken door, the key-tap steps skipped until 关卡设计's line. With that line (W/proj_broken, run b3), 91 / 91. Before that, run 22, 2026-10-08, 77 / 77 checks, 79 frames: USE fades with a camera takeover (alpha 0 during the key turn) and stays under a holding thumb (alpha 1 through the glass takeover). Run 21, 75 / 75 checks, 78 frames, on main with 关卡设计's door-shot build: the look tap goes through `UseDoor` like E, and with the zone's key a tap on a locked door starts the unlock shot, which ends by itself in 1.5 s. Run 20 (iPhone and Android 20:9, 73 / 73 each) checked QUIT TO TITLE and the hint wrap. Run 19, 2026-10-08, 72 / 72 checks, 77 frames (the run line and three gaits; VL218, sheet `research/touch/images/tc_run_gaits.jpg`, frames `~/FrontRoomsVisualWork/touch/run19_iphone`). Before that, run 18, 2026-10-08, 66 / 66 checks, 76 frames (the way-back fixes). Before that, run 16, 2026-10-07, 63 / 63 (the ink chips, the solid USE disc and the static title prompt). Frames are in `~/FrontRoomsVisualWork/touch/run16_iphone`; sheets are in `research/touch/images/tc_*.jpg` (VL128–VL132, VL144; Figma TC14, TC15).
  - The label audit adds two captures, 07z_label_*_dark: the same frame pushed down 6 EV by a temporary post volume, as a dark room for 平面视觉.
- **Two Editor facts** the harness works around, so nobody trips on them again:
  - In batch mode no Game view has focus, so the Input System withholds pointer and touch input from Play Mode. The harness swaps in a copy of the input settings that sends all input to the game (all devices, ignore focus) and restores the original afterwards.
  - The game lays the phone HUD out against the batch window's overlay scale (about 0.8), which put left-anchored HUD ~220 pt in. When the capture first switches the canvases to the profile's own pixels (as on the device, ×3), it calls the game's `ApplyMobileSafeAreaLayout(true)` again (2026-10-22), so frames match the device.
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

  Run C (2026-10-08, the three gaits): a fresh copy of main plus the four game-file hooks. PASS · ended by time, 0 errors, not caught, seed 2554; space 1.8, start door opened 3.64, left the start rooms 4.89, start door shut 7.24 s; Relay released 10.23 s; 57 cells, 6 zones, 4 doors. The start timings match A, B and B2; the later path is inside their spread. No handheld line appears in its log. Report: `~/FrontRoomsVisualWork/touch/autopilot_compare/C_gaits/`.

  The paths differ after the start because the bot steps on real frame time. B and B2, with identical code, differ as much as A and B, so the touch revision does not change the desktop game. Reports: `~/FrontRoomsVisualWork/touch/autopilot_compare/`.
