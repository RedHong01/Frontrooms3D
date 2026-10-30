# 50 · Settings strings: 叙事设计's review (2026-10-29)

**Asked for by** 设置菜单与画质预设 (account A `local_a77b85b3`) on 2026-10-24 09:42, through this line's brief Inbox: every row label and inline explanation in `Assets/Scripts/FrontRoomsSettingsMenu.cs` (the `Get(Key, Kind, LABEL, EXPLANATION, …)` calls), plus the sheet lines in `FrontRoomsDesktopSettingsView.cs` and `Input/FrontRoomsTouchControlsView.cs`. Read on main as of 2026-10-29 (files dated 10-24).

**叙事设计 writes docs only.** These are corrections for the owners to apply; nothing here was edited in code. Faces and sizes stay with 平面视觉.

**Verdict:** the set is clear and plain. Fix the two rule breaks (R1, R2) and the three over-long phone explanations (L1–L3). The rest are small clarity fixes (C1–C3); everything not listed is signed as built.

## 1. Rule breaks: the word "relay" (all player-visible strings avoid it)

The standing string rule is never the word "relay" (`40_floors_exit_tutorial_strings.md`, rules; the tutorial rules Red approved on 10-22). The hunter is never named to the player. The placard gives a role in fine print (`FLOOR WARDEN`), and the out-of-world UI already calls it **it** (`DOORS IT BROKE` on the caught card).

| # | Where | Now | Change to | Owner |
|---|---|---|---|---|
| R1a | `FrontRoomsSettingsMenu.cs:326`, label | `RELAY READOUT` | `THREAT READOUT` | settings line |
| R1b | same row, explanation | `Shows the Relay's state and distance on the HUD. An assist; off by default.` | `Shows what hunts you, its state and its distance, on the HUD. An assist; off by default.` (88) | settings line |
| R1c | `FrontRooms3DGame.cs:2553`, the older pause-card row | `RELAY READOUT` | `THREAT READOUT` | 关卡设计 |
| R1d | `FrontRooms3DGame.cs:3617`, the assist's state line | `RELAY  /  HUNT` (state name), `RELAY  /  BREAKING DOOR` | `IT  /  HUNT`, `IT  /  BREAKING DOOR` (same separator) | 关卡设计 |
| R1e | `FrontRooms3DGame.cs:3625`, the assist's distance | `RELAY  12 M` | `IT  12 M` | 关卡设计 |
| R2 | `FrontRoomsSettingsMenu.cs:312`, HAPTICS · GAMEPLAY | `Vibration for doors, glass, the Relay's lock-on and being caught.` | `Vibration for doors, glass, being seen and being caught.` (56). "Lock-on" is design jargon; the player feels it as being seen (the chime) | settings line |

Code identifiers (`Key.RelayReadout`, `SetRelayReadout`, PlayerPrefs keys, tooltips, logs) keep their names: only text the player can see changes.

## 2. Length: the phone shows explanations under the row, about 140 characters

| # | Row | Now (length) | Change to (length) |
|---|---|---|---|
| L1 | QUALITY, handheld | `Sets every detail below at once. BATTERY saves power and heat, BALANCED keeps the frame steady, QUALITY keeps the detail, ULTRA draws it all at full size. A change reads CUSTOM.` (177) | `Sets every detail at once. BATTERY saves power, BALANCED holds the frame, QUALITY keeps detail, ULTRA draws it all. A change reads CUSTOM.` (138) |
| L2 | RENDER SCALE | `Draws the picture at this share of the screen's width and height, then scales it up. Lower is smoother and softer; text and the HUD stay sharp.` (143) | `Draws the picture at this share of the screen, then scales it up. Lower is smoother and softer; text and the HUD stay sharp.` (124) |
| L3 | FRAME RATE, ProMotion | `Higher is smoother and uses more battery and heat. 40 FPS is a steady middle step on this display; iOS drops 120 in Low Power Mode or when hot.` (143) | `Higher is smoother but costs battery and heat. 40 FPS is a steady middle step here; iOS drops 120 in Low Power Mode or when hot.` (128) |

The desktop QUALITY explanation (158) opens in the side detail pane, not under the row, so it can stay.

## 3. Clarity

| # | Row | Now | Change to | Why |
|---|---|---|---|---|
| C1 | CAMERA MOTION | `How much shots, shakes and dips move the camera. …` | `How much close-ups, shakes and dips move the camera. OFF keeps the view still; everything still happens.` (104) | "Shots" reads as gunfire; the scripted camera moves are close-ups (the key, the unlock) |
| C2 | CAPTIONS | `Captions for the sounds that hunt you, with where each one comes from.` | `Captions for its steps and the other sounds that matter, with where each one comes from.` (88) | Sounds don't hunt; the caption list also holds doors, glass and the far stair slam (`[DISTANT DOOR SLAMS]`) |
| C3 | Desktop detail pane meta (`FrontRoomsDesktopSettingsView.cs:580`) | `FRAME COST     HIGH: THE BIGGEST LEVER` | `FRAME COST     HIGH: CHANGE THIS FIRST` | Says what to do with the fact; "lever" is a designer's word. Optional |

## 4. Signed as built (no change)

- Tabs `GRAPHICS` / `COMFORT` / `CONTROLS` / `ASSIST`; every option word (`BATTERY` … `ULTRA`, `CUSTOM`, `FOLLOW` / `IGNORE`, `FLOATING` / `FIXED`, `HOLD E` / `TAP E`, `HOLD USE` / `TAP USE`, `WHILE TOUCHING`, …).
- `REDUCE FLASHING`: "Lamp strobes become soft dips and dead tubes stop blinking. It reduces flashing; some bright changes remain." It promises no more than it does.
- The key and gesture rows, including `SLOW · UNHEARD` / "Keep the stick inside its ring: you walk slowly, and slow steps are not heard." (true while the creep is silent; `40` §4).
- `SPRINT` "Hold it while you move to run." describes a control; it is not advice (the never-RUN rule covers advice only).
- The sheet lines `TAP THE PICTURE TO SEE IT FULL SCREEN`, `PREVIEW  ·  TAP ANYWHERE TO RETURN`, `CLICK THE PICTURE OR PRESS SPACE TO SEE IT FULL SCREEN`, `CLICK, SPACE OR ESC TO RETURN TO SETTINGS`, `END RUN`, `QUIT TO TITLE`, `TRY AGAIN`, `RESTART`, `TAP TO START`.
- Real product and tech names in settings (`iPhone Duo`, `iOS Low Power Mode`, `FSR 1`) are fine: settings are out of the 1990 world.

## 5. Addendum 2026-10-29 20:1x: the tier joins the readout (Relay v2 V1)

Asked for by 关卡设计 (account A `local_91e132eb`). Red's C04 Q5c (2026-10-21) moved TIER off the always-on HUD and the caught card into the off-by-default readout; V1 built it (`FrontRooms3DGame.cs` f0c6ce55, in main).

| # | Where | Now | Verdict |
|---|---|---|---|
| T1 | `FrontRooms3DGame.cs:3979`, the readout's distance line (only while the assist is on and it is released; tier 1–5) | `IT  23 M  /  TIER 2` | **Signed as built.** On the IT line the tier reads as its level. The `  /  ` separator is the HUD's own. TIER stays the one name (LEVEL names floors). Three-digit distances fit (upper right, Overflow). |
| T2 | `FrontRoomsSettingsMenu.cs:326`, THREAT READOUT explanation (settings line) | `Shows what hunts you, its state and its distance, on the HUD. An assist; off by default.` (88) | Change to `Shows what hunts you, its state, its distance and its tier, on the HUD. The tier rises as you explore. An assist; off by default.` (129, under the phone's 140). The player meets TIER only here, so the row says what moves it. True to the redesign's §12 (1 + new cells ÷ 150, never falls); "explore" leaves out walking back. Label unchanged. |
