# Rounded display corners and the HUD

**Status 2026-10-24 07:0x** (交互设计): option 1 built and landed (§4). Red asked on 2026-10-24 at 05:3x, sending a screenshot of the Duo's inner display. The UI sits a little too close to the edges: Apple's device is a rounded rectangle, so the top UI can't be laid out as on a square screen. Red asked whether rounded-rectangle screens have their own safe-area rules, and how most other vendors handle them.

Sources are in [SOURCES.md](SOURCES.md). The images are in `images/` (Shot format).

## 1. Short answer

**No platform puts the rounded corners into the safe area.**
- iOS safe areas cover the bars, the sensor housing or camera, and the home indicator.
- Android's insets cover the bars and display cutouts.
- Corners are a separate measurement on both platforms (S1, S6).

**Both platforms set the same minimum.** A content rectangle's corner must stay inside the arc at the arc's 45° point. That means an inset of r · (1 − sin 45°) ≈ 0.293 r from each edge.
- On iOS 26+, UIKit's corner-adapted layout regions return exactly this value. We measured it on four screens (§2), and the formula holds to 0.2 pt.
- Android's `RoundedCorner` sample tests a corner button against the same 45° point (S6).

**That minimum is a floor, not a margin.** At that inset the content's corner touches the curve. Everyone who designs for these screens adds space on top of it:
- Apple's design system wants capsules "with extra margin" near the screen edge on iPhone, and concentric shapes on iPad and Mac (S3).
- Android's full-bleed sample pads by the whole radius (S6).
- TV and console games keep a 90 % title-safe area, 5 % per side (S8, S9).
- Many games let the player calibrate a safe-area box (S10–S12).

**FrontRooms today sits on the floor.** A corner pushes the HUD only 4 pt (round controls) or 6 pt (text blocks) inside the arc.
- On the inner display the pause keycap's corner is **3.5 pt** from the curve and the zone text **6.9 pt** (rc01).
- Red's read is right: that is the minimum, not a margin.

## 2. Measured: what iOS itself recommends near the corners

The probe app is `~/FrontRoomsInteractionWork/duo/probe_margins/main.m`. It is a plain UIKit app with the status bar hidden, like the game. It logs and draws the safe area, the layout margins and the iOS 26 `UIViewLayoutRegion` safe-area and margins regions, with and without corner adaptation. It ran on the iOS 27.1 / 26.2 simulators on 2026-10-24. All values are in points.

| Screen, pose | Corner radii | Safe area | Margins | Corner-adapted (H) | Corner-adapted (V) |
|---|---|---|---|---|---|
| iPhone Duo outer, upright (interface 1) | 8 / 59 (radii read 0 at launch) | B 34, R 84 | L 20 | safeArea L 2.3 | T 17.3 |
| iPhone Duo outer, turned over, landscape (3) | TL/TR 59, BL/BR 8 | L 84, B 34 | R 20 | safeArea R 17.3 | T 17.3 |
| iPhone 17 Pro, portrait | 62 | T 62, B 34 | L/R 16 | L/R 18.0 | — |
| iPhone 17 Pro, landscape (3) | 62 | L/R 62, B 20 | L/R 78 | — | T 18.0 |
| iPad Pro 13 (M5), windowed | 32 (window) | B 20 | L/R 20 | margins L 66 (window controls) | T 43 |

How to read the table:
- **The formula holds on every screen.** 0.2929 × 59 = 17.3, × 8 = 2.3, × 62 = 18.2 (iOS reports 18.0), × 32 = 9.4 (9.5).
- **Corner adaptation also clears the iPadOS window controls.** That is the 66 × 43 on the iPad.
- **The Duo inner display was not measured.** The spare simulator boots folded, and Device Hub's fold can't be scripted. By the same formula, its 55 pt corners give 16.1 pt.
- **Picture:** rc02 shows the regions on the Duo outer display. The blue box (margins, vertical adaptation) just touches both top curves.

The Apple evangelist on the iPhone Duo forum points full-bleed UIKit layouts at `margins(cornerAdaptation:)` and `reservedRegions(kind:options:)`, to keep controls clear of the bezels and the system clusters (S2). So the corner-adapted margin is what Apple itself suggests for this device. As the table shows, it is only the 45° floor plus the ordinary 16–20 pt margin on the other axis.

## 3. What platforms and vendors do

| Who | Rule near rounded corners | Source |
|---|---|---|
| Apple, UIKit (iOS 26+) | Safe-area, margins and readable-content regions with corner adaptation (horizontal or vertical). Measured: 0.293 r on the adapted axis | S1, S2, §2 |
| Apple, design system (WWDC25) | Concentric shapes: the inner radius is the parent's radius less the padding. On iPhone, controls near the edge are capsules "with extra margin"; on iPad and Mac, concentric shapes aligned with the window edge | S3 |
| Apple, handheld games (Meet with Apple 2025) | Safe areas are guides, not the game's margins. Fill the screen with the world; anchor each HUD part to its nearest edge at a consistent distance on every device; 44 pt targets | S4 |
| Apple HIG, Layout | Respect safe areas, margins and guides. On tvOS, keep 60 pt top and bottom and 80 pt at the sides against overscan | S5 |
| Android (API 31+) | `WindowInsets.getRoundedCorner()` gives each corner's radius and centre. The sample moves a corner button inside the 45° point; a full-bleed layout pads by the whole radius on each side | S6 |
| Samsung One UI, foldables | Fill both screens with no letterboxing, and keep continuity across the fold. The pages found publish no corner number | S7 |
| Xbox | Title-safe is the inner 90 % (1720 × 1000 at 1080p), action-safe 95 % | S8, S9 |
| Games | A player-adjustable safe area: Minecraft Bedrock's "Change screen safe area" in Video settings, console safe-zone calibrations, an itch.io game's calibration mode for curved edges | S10–S12 |
| Huawei / Xiaomi / OPPO / vivo | Their docs cover cutouts and avoid areas (for example HarmonyOS `getWindowAvoidArea`). No published rule for corners was found | S13 |

**The pattern is the same everywhere.**
1. Measure the real corner from the OS; never assume a square screen.
2. Treat the 45° point as the floor.
3. Add the platform's normal margin on top. That margin is 16–20 pt on iOS phones (16 / 24 dp on Android), and 5 % of the screen on TVs.
4. Where devices vary a lot, let the player adjust it.

## 4. Options for FrontRooms (for Red)

Today, a HUD part that meets a rounded corner moves just until it is 4–6 pt inside the arc. The four options below are what the player would see.

### Option 1 (recommended): the HUD keeps 16 pt from the curve

**Built and landed 2026-10-24 07:0x** (VL435; `TOUCH_CONTROLS.md` §3.1). Measured in the touch harness on the inner display: the zone text went from 26.8 / 16.0 to 28.9 / 27.4 pt (left / top) and the pause keycap from 16.1 / 21.2 to 25.6 / 28.5 pt (right / top), each 16 pt from the 55 pt curve. Outer, turned over: the zone is 12.6 pt lower and still 12 pt past the camera; pause draws 18.8 pt in along the 59 pt corner. Red can still pick another option; the gap is one constant (`FrontRoomsHandheld.HudCornerGap`).

- **The rule.** A corner-anchored HUD part (zone text, pause, settings BACK, the threat line) sits on the corner's diagonal, with its own corner 16 pt from the curve. That gives an inset from both edges of r − (r − 16)/√2 = 0.293 r + 11.3 pt. It is Apple's corner-adapted margin plus the same 16 pt gap that iOS keeps from a straight edge.
- **How it looks.** The HUD follows the curve with an even gap. It no longer kisses it.
- **Numbers.**
  - Duo inner (55 pt corners): **27.4 pt** from both edges, so the pause moves about 11 pt in and 6 pt down, and the zone text about 1 pt in and 11 pt down.
  - Duo outer far side (59 pt): 28.6 pt. The hinge side (8 pt) does not change.
  - iPhone 17 Pro (62 pt): 29.5 pt, so the pause moves about 11 pt in and 6 pt down. iOS reports the real radius at run time.
  - iPad (about 18 pt): the pause moves about 4 pt in.
- **What does not move.** The straight-edge margins, and the big round controls: the stick and USE are larger than the corners, so the rule never touches them.
- **Cost.** One rule in `FrontRoomsHandheld`: the corner gap goes from 4–6 pt to 16 pt, and boxes move along the diagonal instead of only sideways. Then the layout tests and touch runs, and the Figma masters (2528:5403, DUO FLOWS 3655:7752) redrawn at the new spots. 平面视觉 signs the iPhone master change.

### Option 2: concentric shapes, the iOS 26 look

- **The rule.** As option 1, and the square pause keycap and BACK also get corners concentric with the display. Their radius becomes the display radius less the inset, about 27 pt, which turns the 32 pt keycap almost into a circle.
- **How it looks.** It reads most like Apple's own iOS 26 controls. But the HUD's square keycap language changes.
- **Cost.** Option 1 plus a visual-system change, which is 平面视觉's call (the keycap sprites and HUD style).

### Option 3: keep today's minimum

- **How it looks.** No change: the keycap stays 3.5 pt from the curve.
- **Cost.** Nothing, but Red already finds it too tight.

### Option 4 (add-on): a player slider

- **The rule.** Settings › TOUCH › SCREEN EDGE adds 0–24 pt to every edge-anchored part, like Minecraft's screen safe area or a console safe-zone calibration.
- **When.** It can come after option 1, for odd devices.
- **Cost.** One settings row, one PlayerPrefs value and a playtest check.

**What any option blocks.** Nothing else waits on this. Option 1 should land before the next Figma pass, so the masters show the final spots.

## 5. Implementation notes (option 1)

- **The rule in code.** `FrontRoomsHandheld.ClearCircle` and `ClearRect` take a corner gap of 16 pt for HUD parts. `ClearRect` gets a corner mode that moves a corner-anchored box along the diagonal (both axes) instead of only sideways, so a block whose top already sits at the top margin is not pushed 28 pt across.
- **Android.** Read `WindowInsets.getRoundedCorner()` (API 31+) so rounded Android phones get the same rule. Today `FrontRoomsHandheld` reads no corners on Android, so they count as square. This is a JNI read cached per configuration, with a fallback of 0.
- **Harness.** Give the iPhone 16 Pro profile its real corners (62 pt; iOS 26 reports 62 for the 17 Pro) and the iPad and Android profiles theirs, so the touch runs test what devices actually draw.
- **Tests.** Every corner-anchored part must keep at least 16 pt from the arc on every profile, and nothing may move on square screens.
