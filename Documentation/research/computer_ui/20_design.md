# Office terminals: glance and READ

**Status: DESIGN ONLY. Nothing is built.** Spec v1.2, 2026-10-14, 系统设计.
- v1.1 merged three drafts and three reviews.
- v1.2 applies a second review round from two reviewers: one on feasibility and Relay honesty, one on player experience and honesty.
- §20 maps every issue to its fix and explains the fixes that were done differently.
- No project file was changed.
- 2026-10-22: the active area is now **0.276 × 0.207 m**, from Red's CRT-border pick (VL429's "1990 OFFICE MONITOR" tile, 21:23; `research/screens/NOTE_1022_crt_border_READ_FIRST.md`). Every size, reach, distance and code value that follows from it is recomputed (§0, §2, §4, §8, §9, §15, AT-T02, §20). Setup A still holds, but its phone-glance reason is narrower now (§2.2).
- 2026-10-30: Red answered C19, C20, Q3 and the phone close-up. **Amendments 2026-10-30** (below) override §2.4 (phones), §4.1, §7, §8, §11, §12, §14, §15 and §16 wherever they differ.

**File:** `Documentation/research/computer_ui/20_design.md`

**Owners (as agreed; this spec reassigns nothing):**

| Owner | Owns in this feature |
|---|---|
| 系统设计 | this spec: interaction rules, the Relay interplay, honesty rules, tests; the RELAY §8 amendment (§6.6) |
| 游戏视觉 | screen renderer (one live RT per active screen), CRT tube pass, screen material, EXAMINE core extensions, F6/F7; host-station dressing in `Office/*` (Red's path rule) |
| 平面视觉 | screen UI art, type and layout (all type goes through them), including the typeface |
| narrative chat | every string |
| 关卡设计 | `FrontRoomsMap/*`, `FrontRooms3DGame.cs`, `FrontRoomsShots/*`, terminal placement (the host-room rule), C26 |
| touch chat | `Input/*`, `FrontRooms3DGame.Mobile.cs` |
| 声音设计 | sound, through AUDIO_CONTRACT events |
| Red | every DECIDE (§17) |

**One overlap is open, and Red settles it.** C26 belongs to 关卡设计. Its edits sit in `Media/FrontRoomsScreenVideo.cs`, and EXAMINE asks Red who owns that file (EXR D11; the default there is the visual chat). This spec assigns no edit in `Media/`.

**Next step:**
1. 游戏视觉 reviews this spec.
2. 平面视觉 makes the G0 mock frame for D1 and D11.
3. Red answers §17.

## Amendments 2026-10-30 (Red's answers; read before §2, §4, §7, §8 and §11–§16)

Red answered four open points on 2026-10-30. Each was checked in that chat's transcript. Where this list and the sections below differ, this list wins. Sections not named here stand: §6 (the Relay rules), §5 (the close-up pauses), the glance rules, Setup A and the active area.

1. **C19: the era's own interaction.** Red picked neither A nor B: "在这里选择在那个时代的电脑操作系统所使用的交互方式" (游戏视觉 chat, 10:58). The terminal is a DOS text-mode in-house program with IBM CUA keys and a lightbar menu. The full spec is `41_c19_period_interaction.md` (游戏视觉, 10-30), which replaces these parts of this file:
   - §7.1 and §7.3 (controls) → its §5. One verb set: MOVE · SELECT · CANCEL · EXIT · HELP · PAGE. Row 25 names the player's real control. Touch gets CUA short-form soft keys.
   - §8.1 (the shell) and §8.4 (panel types) → its §3.3 and §4.1: the panels are MAIN (the glance), DOC, LIST and HELP.
   - §8.2 (the raster): VGA text mode, 80 × 25 cells of 9 × 16, rasterised as a char grid at 1440 × 800, replaces the 640 × 480 raster and its 40 × 15 grid (its §3.1; O-2).
   - §8.3 (the glance): big type on MAIN is built from full-block glyphs. The headline is 7 rows (cap 58 mm, 28 % of the picture height); the second line is 5 rows (41 mm, 20 %). The headline then reads on desktop to about 3.2 m (its §3.6). §2.3's table is kept as the 25 % / 18 % floor.
   - §4.1 (page memory) → its §4.4: reopening never cuts to a remembered page, and what the glance shows is what E opens.
   - §11 (sounds) → its §4.5: `TerminalKey` and `TerminalRefused`, with O-1 below.
   - §12 (hints), §14 (the "F-key menus" rejection is narrowed to what its §2 rules out) and §15 (sizes: about 47.5 days in all, its §0).
   - §16 (tests): add its AT-C1 to AT-C7 (its §9).
   - §8.6 rule 5 ("no word for leaving the program") is superseded by the period EXIT verb (`F3=Exit`; HELP's `Leave OFFICE MEMO`). The program is named **OFFICE MEMO**, because INTEROFFICE was a real Oracle product. The strings are in `research/narrative/70_terminal_strings.md` (叙事设计, 10-30).
   - **Built, inert:** 游戏视觉's terminal_a landed on 10-30 at 13:49: the 80 × 25 char grid on the CRT tube and the four panels. The READ close-up is 70 % with TEXT on desktop and iPad, and 95 % without TEXT on phones. It waits for 关卡设计's terminal map contract.
2. **C20 = 1: our own 8 × 16 bitmap face**, with no licence (Red, 10:59). 95 ASCII and 14 CP437 glyphs, with an 11-row cap (O-5), so the phone's body text passes 18.6′ (its §6).
3. **Q3 → A38: readable terminals show the look-eye (E·1); dark or still desk screens never do** (Red, 10:59: "推荐：能读的电脑终端也出现眼睛，黑屏和静止画面的显示器永远不出现。"). This amends C35's "only the playing station TV"; see `30_crosshair_grammar.md`.
4. **A39: the phone close-up is full bleed, with no TEXT chip and no transcript panel.** Red in the 交互设计 chat (11:00): "手机上的交互这里就不用显示一旁的这个transcribe了，尽可能让电脑屏幕直接圈屏幕full bleed". Then in the 游戏视觉 chat (11:09): "这里full bleed上下边缘可以保留一点点".
   - On a phone, the 4:3 picture fills the screen's height, with a thin margin top and bottom. It is centred, and the dark tube surround fills the sides. It is never cropped to the width, because that would cut the header and footer rows.
   - BACK is the one overlay. Page and menu input are taps on the picture (TT1′, its §5.4).
   - This replaces §2.4's phone rows (66.3 % fill) and §7.3's TEXT and PAGE chips on phones. Desktop and iPad keep their framing. Other close-ups (the placard, memos) keep TEXT.
5. **Owner answers (系统设计, 10-30; not Red's):**
   - **O-1, with 声音设计:** agreed with its recommendation. There is no refusal tone at first, and §11's ban stands. One short beep is allowed later only if RELAY §14's A/B test shows players never confuse it with the lock-on chime, and only inside the paused close-up. 声音设计 confirms.
   - **O-3, with 关卡设计:** yes. 交互设计 agreed on 10-30 at 13:22, with conditions in `briefs/visual.md`: the pad map; a 44 pt exception for the 30.6 pt items and the 41 pt soft keys; and LB/RB, the d-pad and the stick added to AT-C4. On a terminal S moves down and pad A selects. E, Esc, F3, X, B, MENU and BACK still leave. The movement latches (§4.4 C25 and C25b) cover a held S across opening and closing (AT-C4).
   - O-2 and O-5 belong to 平面视觉 and 游戏视觉, and O-4 (WebGL F-keys) to 游戏视觉 and 交互设计.

**Red's request:** "When the player walks up to an interactable computer, they can directly see the information shown on its screen, and the information inside the computer has its own UI system."

**Baseline.** main **0a9ee7f** (2026-10-14) plus the working tree, re-read for v1.2.

**Short names used in citations:**

| Short | File |
|---|---|
| INV | `research/computer_ui/00_interaction_inventory.md` |
| RES | `research/computer_ui/10_research.md` (its critic section, :1166-1216, overrides its sweeps) |
| EX | `research/inspect/10_design.md` (EXAMINE v1.1) |
| EXP | `research/inspect/20_prototype.md` (EXAMINE fix stage, 10-14) |
| EXR | `research/inspect/30_for_red.md` (EXAMINE's Red list D1–D12, 10-14 17:25: "if you say nothing, the default stands") |
| EXC | `research/inspect/contracts/` (`map_inspect.md`, `touch_inspect.md`, `sound_inspect.md`) |
| RELAY | `RELAY_PURSUIT_REDESIGN.md` |
| SCREENS | `SCREENS_VISUAL_SYSTEM.md` |
| STR | `research/screens/30_screen_strings.md` |
| NP | `research/wallpaper_motion/30_narrative_phosphor.md` |
| MG | `MAP_GENERATION.md` |
| VL | `VERIFICATION_LOG.md` |

Code paths are under `Assets/Scripts/`.

**Tags:**
- **DECIDE**: Red decides. My recommendation comes first, then the alternatives.
- **ESTIMATE**: my number, not measured.
- **UNVERIFIED**: not confirmed.
- **CONTRACT**: a change in a file another chat owns. It is never edited from here.

---

## 0. The short version

1. **One terminal per Office zone** (D3), placed in the chunk that holds the zone's key. Every other monitor is scenery, with no prompt and no verb (D4).
2. **Glance, no button.** The terminal is always on, and its front page reads in place. This is Red's "directly see" (D1).
   - Desktop: the headline reads out to about 2.9 m, past the 2.4 m aim reach. The second line reads to about 2.1 m, past the 1.6 m prompt edge.
   - Phone: the headline reads anywhere in the stand box and out to about 1.25 m from the glass. The second line needs READ.
   - This works because every terminal has the same "Setup A" (§2.2): on the PC case, upright, 3 cm forward, no chair, a clear stand box.
3. **READ, one press.** E or USE opens an EXAMINE close-up of the screen.
   - The world pauses (D2).
   - Every page reads on every device class with no zoom step.
4. **Its own UI system.** One fictional 1990 program has a header bar, a front page that lists the pages, the pages themselves, and a `PAGE n OF m` footer.
   - In v1 the player turns pages. There is no cursor.
   - A menu program with a cursor is D11.
5. **Controls.**
   - Desktop: A/D, ←/→, ↑/↓ and Space/Enter. Each key is its own press.
   - Touch: a PAGE chip, or a tap on either half of the screen.
   - Exits: every close-up exit. A lost WebGL pointer lock pauses the game over the close-up.
6. **Content** is static, true and seed-keyed: memos and a tenant print job (D5).
   - Sets are dealt with no repeats within a run.
   - READ reopens on the page the player left. The glance always shows the front page.
7. **Honest with the Relay.**
   - The screen never reads Relay, director, door or lamp state.
   - Using it adds 0 Attention.
   - Its sounds never use a Relay cue family.
   - The prompt gate hides only for threats the player can perceive, and it comes back only on a change they can perceive (§6.1).
8. **Phases.**
   - Phase A (SEE) needs nothing from EXAMINE.
   - Phase B (READ) lands after all five EXAMINE steps.
   - Touch READ (B-touch) waits for EXAMINE's touch items T1, T2, T6 and T7.
9. **Urgent, and separate from this feature (D0):** builds from HEAD can show the real LEVITZ trademark. Approve the Ad_02 swap or the kill switch.
10. **Size:** about 39 working days across chats (ESTIMATE, §15.3). Of these, 2.25 days are EXAMINE touch items that EXAMINE needs anyway.

---

## 1. What exists today (main 0a9ee7f + working tree)

| Fact | Evidence |
|---|---|
| Every Office desk monitor gets the ad screen | `AttachScreen` (`Office/FrontRoomsOfficeKit.cs:851`) is called at `:875`. The ad is a 0.28 × 0.19 m unlit quad, placed at the code's own anchor (0, 0.248, 0.194) plus 4 mm (`Media/FrontRoomsScreenVideo.cs:27-28`, `CreateSurface`). The sidecar anchor is (0, 0.248, 0.190) (`Kit_CRTMonitor.json`). All monitors share one 640 × 434 ad RT (`:301`). Video decodes within 18 m (`:18`) |
| The only verb is hidden | A raw `Keyboard.current.eKey` read within 2.4 m (`:19`, `:141-142`). No prompt, no sound, no touch. It fires while paused or caught (INV) |
| The game's aim cannot see a monitor | `UpdateAim` (`FrontRooms3DGame.cs:1365`) asks `Describe` (`FrontRoomsMap/FrontRoomsMapWorld.cs:2444`), which answers only for doors, panes and keys. The `KeyOnRay` call (`Game.cs:1388`) is the precedent for a target on the shared aim ray |
| **HEAD builds can show LEVITZ** | The F6 shader was committed in 2ce2087 and loads at `ScreenVideo.cs:222`. `FrontRooms_Ad_01.mp4` is still the only clip, and it plays on map Office monitors (VL224, re-checked 10-14). The title stream never shows it in builds: `RuleForStreamSequence` returns Lobby unless `IsPlayable` (`FrontRoomsRoomStream.cs:414-421`), and only `Editor/FrontRoomsStreamVerification.cs:80` calls `BeginPlayableSequence`. The fix is ready: `W/apply/codex_audit_v3_ad02.sh` (dry run OK on 10-14; it needs `--apply --red-approved`) |
| F7 is still open in main | The ad quad is in no LOD level, so it floats from the 11.18 m cull to 18 m (VL115, at lodBias 1). The fix (`AddToMonitorLods`) is in the glass-look package (VL226 PASS, rebased over COV-4 on 10-14). It lives inside `FrontRoomsScreenVideo`, so a terminal quad does not get it (P10). The terminal needs its own (V3) |
| The shared quad mesh | COV-4 (`ScreenVideo.cs:188-215`) is in the working tree only |
| Nothing gates the ad video by platform | It runs on iOS, Android and WebGL alike (INV) |
| EXAMINE is not in main | `Phase` is `{ Title, Playing, Paused, Caught }` (`Game.cs:31`). EXAMINE lands in 5 steps (header of `W/apply/inspect.sh`; EXR §4 has the same order): 1. `map_inspect_1_rig.diff`; 2. `inspect_data.sh`; 3. `inspect.sh` (WAITING until step 1); 4. `map_inspect_3_game.diff` (C1–C15, C25); 5. `sound_inspect.diff`. The CRT is EX row L1, deferred. EXAMINE's touch items T1–T7 have no diff yet (EXC `touch_inspect.md`; EXR §6) |
| Relay hooks in main | `WarnStage` (`FrontRoomsMap/FrontRoomsMapHunter.cs:167`). `PlayerSeesRelay` (`:173`) is a flat ±60° test with no pitch, reaching out to `SightDistance` 46 m (`:75`, `:1262-1279`). The director policy is in main but not wired (`FrontRoomsRelayDirectorPolicy.cs`; `Game.cs:1344`). `Spotting`, `TargetAcquired`, `OnMap` and stage 3 do not exist yet. RELAY §13 plans a `FRONTROOMS_RELAY_V2` define, but no such define exists in the code |
| Door blows today | A 3D FMOD one-shot with an event max of 36 m (`Tools/audio/fmod_frontrooms.py:722`), captioned within 36 m (`Audio/FrontRoomsSoundDirector.cs:567-580`). The Break event reaches 40 m (`:728`). RELAY §13 audio item 8 (drop blows past 45 m of path) is not built |
| Default quality per platform | iOS and Android Medium (lodBias 0.7). WebGL High (1.0). Standalone Ultra (2.0). (`ProjectSettings/QualitySettings.asset`, `m_PerPlatformDefaultQuality`) |
| The interim typeface lacks glyphs | `VT323-Regular.ttf` has about 570 code points. It has none of ◄ (U+25C4), ► (U+25BA), █ (U+2588), or any box drawing (cmap read 10-14) |
| No pointer-lock handling | `Cursor.lockState` is set only in `SetPhase` (`Game.cs:2205`). `OnApplicationFocus` and `OnApplicationPause` cover focus only (`:2237-2251`) |
| No one can use a computer | INV, "Using a computer" |

---

## 2. Reading depths and where the player stands

### 2.1 What "walk up and directly see" means (DECIDE D1)

**Recommended: glance in place, plus `E · READ SCREEN` for every page.**
- The front page is always legible at headline size. No button is needed, and the camera is never taken.
- Detail pages need the close-up. A screen 0.207 m tall cannot hold body text that reads from where a player stands (RES critic :1168-1169, :1182).

**Alternatives:**
- **(b) Auto close-up.** It opens after the player stands ≥ 0.6 s in a terminal's stand box while aiming at it. Terminals only, and the gate (§6.1) applies. It takes the camera without asking.
- **(c) Glance only.** No close-up, 2 lines per screen.

D1 and D11 come with 平面视觉's G0 mock frame: the front page at the back of the stand box and at 2.4 m, on desktop and on a phone.

### 2.2 The host station: "Setup A"

**Geometry from code (station space: the spine panel at z = 0, the user side +Z):**

| Quantity | Value | Source |
|---|---|---|
| Eye height; body radius | 1.62 m; 0.30 m | `FrontRoomsMap/FrontRoomsModuleUnits.cs:92` |
| Camera | vertical FOV 76°, near plane 0.06 m | `Game.cs:280` |
| Desk front edge | z 0.807 m (deskZ 0.426 + half the depth, 0.381) | `OfficeKit.cs:856-858`; `Kit_OfficeDesk.json` |
| Space in front of the desk, per station | 0.94 m | `OfficeKit.cs:704`, `:741` (`stationD` = desk + 0.94) |
| Spine panel front face | z 0.034 m (panel half-depth 0.0335 about z 0) | `OfficeKit.cs:854`; `Kit_CubiclePanel.json` |
| Monitor today | origin at z 0.326 (deskZ − 0.1). The mesh runs from −0.311 to +0.213 in depth, so its back is at z 0.015: **1.9 cm inside the spine panel at every station today** | `OfficeKit.cs:875`; `Kit_CRTMonitor.json` |
| Screen anchor | sidecar (0, 0.248, 0.190). The ad code hard-codes (0, 0.248, 0.194) and adds 4 mm, so ad quads sit at +0.198 | `Kit_CRTMonitor.json`; `ScreenVideo.cs:27` |
| Monitor collider front | +0.205 m | `Kit_CRTMonitor.json` |
| Screen centre height | 0.988 m on the desk; **1.128 m on the PC case** (case top 0.14 m) | anchor y 0.248; `Kit_PCDesktop.json` |
| PC case depth | −0.287 to +0.217 | `Kit_PCDesktop.json` |
| Chair | on 85 % of stations, 0.15–0.55 m in front of the desk, x ±0.25 m | `OfficeKit.cs:882-886` |

**Setup A (required on every terminal, rule P4):**
- **On the PC case**, so the screen centre is at 1.128 m.
- **Upright: no tilt.**
  - `Kit_CRTMonitor` is one mesh, so a tilt can only pivot about its base.
  - A 10° tilt about the base moves the bounding box's top-back corner to about z −0.05, through the spine panel and into the cubicle behind. It also lifts the screen centre to about 1.158 m.
  - A real tilt-swivel stand needs a separate stand part, with the shell pivoting about the screen centre (§13 E10).
  - v1 does not need it: the upright pose is already inside EXAMINE's body band (below).
- **The host monitor, its PC, keyboard and mouse move 0.03 m toward the user.**
  - The monitor's back goes to z 0.045, 1.1 cm clear of the panel face. That removes today's 1.9 cm intrusion on the one station the player studies up close.
  - The PC's back goes to z 0.069, 3.5 cm clear.
- **The terminal quad sits 4 mm in front of the sidecar anchor:** local z 0.194, station z 0.550, which is **0.257 m behind the desk's front edge**. The terminal reads the anchor from the sidecar, not the ad code's hard-coded value. The monitor collider's face is 1.1 cm in front of the quad.
- The monitor keeps its natural yaw roll (±9°, `:875`). EXAMINE allows up to 25° off square.
- **No chair** at the host station, and a **clear stand box** (rule P5).
- **UNVERIFIED until 游戏视觉 checks the kit:** the 3 cm shift against the keyboard, and the flat quad against the curved glass. Until then, every number in §2.2–§2.4 is both an ESTIMATE and UNVERIFIED.

**Why Setup A:**

| Problem | Without Setup A | With Setup A |
|---|---|---|
| Phone glance | On the desk, with no chair, the eye at the desk edge is 0.86 m away and 47° below the face normal. The headline is 28.2 px, or 19.9′: **only 7 % over the 18.6′ rule, and only where there is no chair**. With a chair (85 % of stations) the eye is 1.2–1.6 m away (RES critic :1189), and the headline is **17.9′ or less, below the rule** | The headline is **22.1–25.5′** across the stand box (§2.3), 19 % over the rule at its back |
| EXAMINE pose height | A screen at 0.988 m is below the body band floor of 1.07 m (eye − 0.55, EX §3.2). The solver must tilt the pose down about 21° (ESTIMATE) | 1.128 m: a level pose, square to the face, inside the band by 5.8 cm |
| Chair in front | At 85 % of stations the eye is pushed back, and the camera path crosses the chair (RES critic :1189) | No chair |

**Stand box:** a floor region kept free of colliders, 0.8 m wide × 0.9 m deep × 1.8 m tall. It starts at the desk's front edge and is centred on the monitor's x.
- **0.9 m deep.** The body centre can stand anywhere from 0.30 m from the desk edge (body touching the desk) to 0.60 m (one step back). The box stays inside the station's own 0.94 m in front of the desk, so it never takes the aisle.
- **0.8 m wide** = the 0.6 m body plus 0.1 m each side, for the approach angle and the camera path.
- **1.8 m tall** = above eye height.
- **"In the stand box"** means the body centre is 0.30–0.60 m from the desk edge. The eye is then 0.74–0.99 m from the glass, and 42° to 30° below the face normal.

### 2.3 Glance numbers (Setup A)

**Method:**
- At 1080p the game camera draws 691 px per radian (1080 / (2 · tan 38°)).
- On-screen size = in-world size × cos(angle off the face normal) / distance.
- **Rule (EX §3.4):** a cap reads on desktop at ≥ 12 px at 1080p, which is 18.6′.
- Handheld classes see the same view at a different angle per desktop pixel (EX §3.4):
  - phone: 762′ / 1080 = 0.706′;
  - iPad 11-inch: 1343′ / 1080 = 1.243′.
- Caps are given as a share of the 207 mm active height (§8.3). **Bold** means the line reads.

| Spot | Eye to glass | Below the face normal | Face, 1080p | Headline (25 %, 51.8 mm) | Second line (18 %, 37.3 mm) | Body (4.2 %, 8.7 mm) | Phone: headline / second | iPad: headline / second |
|---|---|---|---|---|---|---|---|---|
| Body against the desk (0.30 m) | 0.74 m | 41.5° | 144 px | **36.1 px** | **25.9 px** | 6.1 px | **25.5′** / 18.3′ | **44.8′ / 32.2′** |
| Back of the stand box (0.60 m) | 0.99 m | 29.9° | 125 px | **31.4 px** | **22.6 px** | 5.2 px | **22.1′** / 15.9′ | **39.0′ / 28.0′** |
| Prompt edge | 1.6 m | 17.9° | 85 px | **21.2 px** | **15.4 px** | 3.6 px | 15.0′ / 10.8′ | **26.4′ / 19.1′** |
| Aim reach (`Game.cs:186`) | 2.4 m | 11.8° | 59 px | **14.6 px** | 10.5 px | 2.4 px | 10.3′ / 7.4′ | 18.2′ / 13.0′ |
| "Worth the walk" (EX §1.3) | 3.0 m | 9.4° | 47 px | 11.8 px | 8.5 px | 2.0 px | 8.3′ / 6.0′ | 14.6′ / 10.6′ |

**What this gives:**
- **Desktop:** the headline reads out to about 2.9 m. The second line reads out to about 2.1 m.
- **Phone:** the headline reads out to about 1.25 m from the glass. That covers the whole stand box, with 19 % margin at its back. The second line never reads at a glance on a phone (18.3′ at best, with the body against the desk); it is read in the close-up.
- **iPad 11-inch:** the headline reads to about 2.3 m, and the second line to about 1.6 m, just past the prompt edge.
- **Body text never reads at a glance.** It needs READ.
- **At 3 m** the tell is the lit, still field in its own colour. Being on is not a tell by itself, because about 1 set in 3 is on (SCREENS §5).
- AT-T02 measures size and contrast on captures. A glance with the tube at Full must also pass a contrast check.

### 2.4 Close-up numbers

**Framing:**
- EXAMINE flat kind, composition B, FOV 76° − 15° = 61° (EX §3.1).
- Desktop and WebGL: centred on the screen.
- Handheld: centred in the safe area minus the button column (§7.3).
- The framed face is the terminal's own quad, **0.276 × 0.207 m** (4:3, §8.2).

**Fill rule:**
```
fill = min(70 %, 2.0 × RT lines ÷ framing-rect lines)
framing rect = the full height on desktop and WebGL, the safe-area height on handheld
```
- 70 % keeps a ring of desk and bezel in view (EX P2: the object stays physical).
- The second term keeps texel magnification within EXAMINE's 2.0× cap. EXAMINE already builds that cap (EX §3.4; EXP §1, where the key close-up is floored by it). A new def field, `capBaseByTexels`, applies it to the base framing. So no exception to the cap is needed on any device.
- Camera to glass = 0.207 / (fill as a share of the full height × 2 · tan 30.5°) = 0.176 / that share.

| Class (render lines) | RT lines | Face, share of the full height | Camera to glass | Body cap | Magnification |
|---|---|---|---|---|---|
| Desktop 1080p | 960 | 70 % | 0.251 m | 31.5 px = **49′** | 0.79× |
| Desktop 4K (2160) | 960 | 70 % | 0.251 m | 49′ (reference angle) | 1.58× |
| Desktop 5K (2880) | 960 | 66.7 % | 0.264 m | 47′ | 2.0× |
| Phone, iPhone 16 Pro (1206; safe area 1143) | 480 | 66.3 % (70 % of the safe area) | 0.265 m | **21.0′** | 1.67× |
| iPad 11-inch (1640) | 480 | **58.5 %** | 0.300 m | **32.7′** (×0.80 class factor) | 2.0× |
| iPad 13-inch (2064) | 480 | 46.5 % | 0.378 m | 26.0′ (ESTIMATE) | 2.0× |
| WebGL, 1080p canvas | 480 | 70 % | 0.251 m | 49′ | 1.58× |
| WebGL, 1440p canvas | 480 | 66.7 % | 0.264 m | 47′ | 2.0× |

**Checks:**
- **Legibility:** every class passes 18.6′ with no zoom step. No zoom is offered on screens.
- **Stand-off:** stand-off from the monitor collider = camera distance − 0.011 m. That is ≥ 0.240 m, above EXAMINE's 0.15 m (EX §3.2). Every distance is above `minDist` 0.18 m.
- **Pose:** 1.128 m high, level, square to the face. It is inside the body band of 1.07–1.72 m (EX §3.2).
- **Travel:** from the back of the stand box the camera moves about 0.78 m, which takes 0.40 s (EX §3.3 clamp floor).
- **Path:** the camera path's spherecast (0.123 m at 16:9) passes about 0.36 m above the desk top at the desk edge (ESTIMATE from geometry). AT-T05 checks it in 100 seeds.
- **Desktop 16:9:** the face takes 52 % of the screen width. With transcripts on it fits in the left 64 % (EX §5.2).
- **Handheld:** the face never sits under the PAGE or BACK hit circles (§7.3).

---

## 3. Which computers, and placement

### 3.1 Terminals and ambient monitors

| | Terminal | Ambient monitor |
|---|---|---|
| How many | **at most 1 per Office zone** (D3); target ≥ 90 % of Office zones | every other Office desk monitor. About 88 % of stations have one (`OfficeKit.cs:863`) |
| Screen | the program, always on | as SCREENS and D14 decide |
| Verb | `E · READ SCREEN`: a verb plus an object, like EXAMINE's `E · READ PLAN` (EXP §1). 平面视觉 words it | **none** (D4) |
| Power | no power verb; on its own receptacle circuit (§6.3) | — |

**Why one per Office zone:**
- EXAMINE's rarity rule: close-ups stay rare, or reading becomes a chore (EX P11).
- Office is about 16 % of zones (INV). At 90 % coverage, about 1 zone in 7 has a terminal.
- **Terminals met per run are not known yet.** v1 bots are caught in 45 of 52 runs (RELAY §2.6), and many seeds put no Office near the start (INV). L6 measures terminals met per run in the bot baseline before narrative sizes the memo pool (§8.5).

### 3.2 Placement rules

关卡设计 owns the host-room rule (L2). 游戏视觉 builds the station side in `Office/*` (V5).

| # | Rule | Why |
|---|---|---|
| P1 | At most one terminal per Office zone | D3 |
| P2 | **Host chunk:** the chunk that holds the zone's key cell (the zone's cell nearest its site; MG "Zones and heights"). Only that chunk can host the zone's terminal. It is a pure function of seed and zone.<br>**Host room:** among the host chunk's rooms that the map dresses with the Office kit and that lie in the zone (as `Dress` reads it), the one whose centre is nearest the site. Ties go by room index. "Dressed with the Office kit" means fill Office, or Auto in an Office zone, intact, uniform, and not in the start area (`MapWorld.cs` `Furnish`, `Dress`). A pure function of seed, chunk and revision.<br>**Host station:** chosen by the dresser after a layout pre-pass (§3.3). It is the station nearest the site that passes P4–P5, with ties going by station order. Hashed choices only, never `UnityEngine.Random` | Zones cross chunk borders, and a revisit shift rebuilds one chunk while its neighbours stay (MG "Revisits shift"). With one host chunk, a zone can never get two terminals, and no unbuilt chunk is ever needed. The map owns rooms; 游戏视觉 owns stations |
| P3 | **Content key** = hash(seed, the zone id). This is the key `zonesVisited` uses (`Game.cs:201`) | The same key on every visit and after any shift |
| P4 | **Setup A** (§2.2). If the PC kit is missing, there is no terminal, and the miss is logged | The glance and pose numbers assume it |
| P5 | **Stand box clear:** the pre-pass finds no collider in the box except chairs. The host's chair is not spawned, and neither is any chair whose collider overlaps the box. Their random draws are still made (§3.3) | A chair blocks the spot at 85 % of stations |
| P6 | EXAMINE's path check passes from the whole box and from 1.6 m at 60°: spherecast r_c + 0.01, stand-off ≥ 0.15 m. The dresser has no physics at placement time, so AT-T05 checks this over 100 seeds | Otherwise there is no prompt (EX §3.2) |
| P7 | **Never in:** the start area, the title stream, or a furniture pile. Once RELAY v2 map data lands (RELAY §13 steps 8 and 14): never in a trigger room or its 1.5 m armed band, and never in a sanctuary. AT-T01 adds those cases then | RELAY §6 and §10. Reaching a terminal must never arm a room, and a terminal must never mark a safe room |
| P8 | **Never the only source of anything essential.** All v1 content is teaching or flavour | Like RELAY's key rule |
| P9 | Only a `Kit_CRTMonitor` on a desk. Never a pile monitor, and never `Kit_CRTTV` for now. The TV's sidecar has a `screen` anchor at (0, 0.284, 0.245). Two things block it: the screen code hard-codes the monitor's anchor and size (`ScreenVideo.cs:27-28`; SCREENS P6), and TVs spawn only in piles | Piles turn and invert sets (EX L2) |
| P10 | The host monitor gets the terminal **instead of** the ad screen | One picture per screen. A terminal never shows the ad |
| P11 | **Later (v1.1), not v1:** prefer a station whose screen is visible from a doorway. The dresser has no sight test today. AT-T03 decides whether the tell needs it | — |
| P12 | **Success target:** ≥ 90 % of Office zones get a terminal over 100 seeds. A miss (no Office room in the host chunk, or no valid station) is logged once per zone and revision. This is a design-time rule. If misses go over 10 %, 关卡设计 and 游戏视觉 widen the rule (for example, to the second-nearest Office room in the host chunk) and re-run. There is no runtime retry, because the dresser has no channel back to the map | The content is not essential (P8). Office-fill modules such as `Office_Bullpen_4x4` (fill Office) go through the same dresser; modules with fill None have no stations and are never candidates |

### 3.3 Host station: a layout pre-pass and the same random draws

**Why a pre-pass.** `FrontRoomsDressJob` interleaves solving and creating: each `Step` creates what the solver has recorded before it solves more (`Office/FrontRoomsDressJob.cs` header and `:147-157`). So while `BuildStation` runs, the nearest station is not known yet. Earlier stations and their chairs may already exist.

**Rule (CONTRACT 游戏视觉; only `Office/FrontRoomsOfficeKit.cs` changes):**
1. The map calls the new `BeginDress` overload for the host room only (§3.4).
2. **Pre-pass.** The job first runs `Solve` with the same seed, creating nothing.
   - OfficeKit records each station's pose in its own list. The run poses are known where `PlacePods` and `PlaceWallRows` create the runs.
   - The solver's occupancy map holds everything else.
   - It yields per unit as today, so the frame budget holds.
   - Cost (ESTIMATE): the host room's solve time doubles. Only one room per Office zone pays it.
3. It picks the host station (P2) and its stand box (P5).
4. **Real pass.** `Solve` runs again with the same seed and reproduces the pre-pass layout exactly, because the host makes the same `rng` calls (below).
   - Setup A applies when the host's `BuildStation` runs.
   - Every chair, at any station, whose collider overlaps the stand box is drawn but not recorded.
5. `FrontRoomsDressJob.cs` does not change. So `inspect.sh`, which replaces `FrontRoomsDressJob.cs`, `FrontRoomsKitLibrary.cs` and `FrontRoomsFurniturePile.cs` against sha1 bases on 0a9ee7f, keeps its bases. If 游戏视觉 finds it must change `DressJob.cs` after all, the order in §15.4 adds `rebase_apply.sh inspect`.

**The same random draws.** `BuildStation` draws from one `rng` in this order (`OfficeKit.cs:862-888`):
1. side;
2. the 12 % no-monitor roll;
3. inside the monitor branch: onCase; PC yaw (only if onCase); monitor yaw; keyboard x and yaw; mouse yaw; paper (60 %, then z and yaw), phone (45 %, then yaw) and binders (35 %, then yaw);
4. the 85 % chair roll and its three ranges;
5. the bin.

Forcing any of these on the host would shift every later draw, and other stations would dress differently. **So the host makes exactly the `rng` calls it would make unforced.** Overrides come from a separate stream seeded by hash(seed, zone, "terminal"):

| Natural roll on the host | What the host gets |
|---|---|
| side | natural |
| The 12 % roll says "no monitor" (the natural stream skips the whole branch) | The whole branch from the forced stream, in the natural order and with the natural ranges: onCase true; PC yaw ±3°; monitor yaw ±9°; keyboard x ±0.04 m and yaw ±6°; mouse yaw ±30°; paper 60 %, phone 45 %, binders 35 % |
| onCase false (natural branch) | onCase forced true. The PC yaw comes from the forced stream, because the natural stream drew none |
| everything else in the branch | natural |
| chair 85 % and its three ranges | drawn; never spawned |
| bin | natural |

Positions: the host monitor, PC, keyboard and mouse move +0.03 m in z (§2.2). Paper, phone and binders stay where they are.

**Test (AT-T01):**
- Over 100 seeds, every non-host station's dressing signature is byte-identical to today, except for chairs left out of a stand box.
- The real pass reproduces the pre-pass layout.
- The 20 validator-seed map signatures are unchanged.

### 3.4 Identity, persistence and the contract

- **No map-side terminal registry in v1.** A terminal holds no gameplay state. EXAMINE's visual registry holds the hit box (EX item 11). Only one registry exists, which closes RES critic :1196.
- **Revisit shifts.**
  - Only a shift of the host chunk moves the terminal. That chunk gets new rooms and may get a new desk.
  - A neighbour's shift changes nothing (P2).
  - The content set and the page memory follow the content key, not the desk.
- **Contract shape.**
  - The map already finds the Office dresser by reflection (`MapWorld.cs:2004-2016`). It passes `(parent, floor, ceiling, seed, keepClear, obstacles)`, and the dresser gets no map reference.
  - So the map works out the host room itself (P2). For that room only, it calls a new overload, `BeginDress(..., Vector2 terminalSite, int terminalKey)`, bound by reflection like `officeBegin`. The site is in the same chunk-local metres as `floor`.
  - If the overload is missing, the room is dressed as today and has no terminal.
  - Neither side gains a compile dependency on the other. The map never touches spawned furniture.
- **No `TerminalBuilt` event in v1.** If stateful pages come later (§13 E4), the map registry raises it, and nothing else does.

### 3.5 Ambient monitors and frames

- Ambient monitors have **no prompt and no verb** (D4).
- They **never use the terminal's field colour** or its header-bar layout, so the tell stays unique (AT-T19).
- **Any ambient frame that shows text** must meet the glance sizes of §2.3, or carry no text.
- **Era (DECIDE D14).**
  - A 1990 office PC monitor does not show TV. The WXRM broadcast belongs on TV sets.
  - After D0, I recommend that desk monitors show dark glass or static office frames (STR §6).
  - The broadcast moves to TV sets once the screen code reads each kit's anchor and TVs are placed outside piles (P9).
  - **If ambient frames use a dark field** (STR §6's `C:\>_` is grey on black), the terminal uses the blue field (§8.7).
  - **`SYSTEM IDLE`:** a floating-text blanker with no pre-1991 source yet (RES critic gaps). Narrative checks it for the era.
- **To raise with narrative:** the approved frame `ALARM LOG 03/90` / `ZONE 04  TROUBLE  ACK` (STR §6) prints a zone number before any numbering is agreed, and it uses TROUBLE, the word RELAY §10 gives the sanctuary lamp. I recommend dropping it.
- **Handheld and WebGL.** If D14 keeps ads on desk monitors, the ads show a static frame at all times on these tiers, or SCREENS §5's flipbook (a texture array, not an RT). They never freeze because a terminal is near: a freeze would pop, and it would tell the player that a terminal is close. This is conditional item A1; its owner is whoever Red names in EXR D11.

---

## 4. States and flow

### 4.1 Screen states

| State | When | What it shows | Render cost |
|---|---|---|---|
| **TELL** | beyond r_A, out of view, or over the RT budget (§9.2) | one shared static texture: the field and frame, with no per-terminal text | none |
| **ACTIVE** | in view, within r_A, inside the budget | the RT showing **FRONT, always** | one render on entry, then held |
| **READING** | EXAMINE close-up open | the RT, page n | one render at open, one per page turn, and one on exit (back to FRONT) |

**ACTIVE radius r_A** = min(8 m, 0.9 × the host monitor's LOD cull distance at the current quality level). It is read from the monitor's `LODGroup` at attach, so a quality level the player changes is covered too.

| Tier | Default quality (lodBias) | Monitor LOD0→1 / cull | r_A |
|---|---|---|---|
| Mac, Windows | Ultra (2.0) | 6.7 / 22.4 m | 8 m |
| WebGL | High (1.0) | 3.35 / 11.18 m | 8 m |
| iOS, Android | Medium (0.7) | 2.35 / 7.83 m | 7.0 m |

Sources: `QualitySettings.asset`; VL115 measured 3.35 / 11.18 m at lodBias 1, and the other rows scale with lodBias.

- **Why 8 m:**
  - The headline reads from 2.9 m on desktop and 1.25 m on a phone.
  - At walking speed (3.2 m/s) the RT has at least 1.3 s to render before the headline can be read.
- **Why 0.9 × cull:** an RT is never rendered for a monitor about to cull.
- **F7 for terminals:** the terminal quad joins every LOD of the monitor's own `LODGroup`, using the same method as `AddToMonitorLods` (VL226). This is in V3. The quad culls with the monitor at every tier.

**Page memory (a 系统设计 rule; it was D15):**
- The UI runtime keeps a per-run map from content key to the last page read. Restart clears it.
- READ opens on that page.
- **The in-world screen always shows FRONT.** On exit the program returns to its front page, the way a 1990 menu program returns to its main menu. So the glance never changes after a read.
- It is UI state only. No gameplay system reads it, and it takes no Relay, door or lamp input.
- **Why:** S is an exit key next to A and D, which turn pages. With page memory, a stray exit costs one 0.4 s reopen, not your place. And FRONT stays the glance (AT-T02).

### 4.2 Player flow

1. **Notice (about 3 m):** a lit, still field in a colour no other screen uses, at a desk with no chair.
2. **Approach:** the headline reads from 2.9 m on desktop and from 1.25 m on a phone.
3. **Prompt (≤ 1.6 m and ≤ 60° off the face):** `E · READ SCREEN` in the normal prompt slot (EX §5.1).
4. **Open:** E on desktop and WebGL; USE with the READ glyph on touch. A tap on the screen also opens it, where the tap starts in the look zone (EX C15; §7.3). This is EXAMINE's flow, unchanged:
   - the world freezes;
   - the camera travels for 0.40–0.55 s;
   - the HUD fades and captions are hidden (EX §5.1).
5. **Read and page.** On desktop the `A D · PAGE` hint shows on the first 2 opens per session, before `E · BACK` (§12).
6. **Exit:** any exit in §7.2. Control returns at once. Rotation is home in 0.15 s and position in 0.40 s (EX §2.3).

### 4.3 One read, in numbers (desktop, from the back of the stand box)

| t (inspect clock) | What happens | Source |
|---|---|---|
| 0.00 | E down. The gate (§6.1) is checked again after `relay.Tick`. `Phase.Inspect` sets timeScale to 0. `Inspecting(true)` fires. The RT renders the remembered page | EX §2.4, C5b |
| 0.00–0.40 | The camera travels about 0.78 m on a sine ease: 0.30 + 0.12 × 0.78 = 0.39 s, raised to the 0.40 s clamp floor | EX §3.3 |
| 0.20 | Page and exit input are accepted from here on | EX `CancelAfter` |
| any time | A page turn: a hard cut within 1 frame, at least 0.34 s after the last one (§7.1). `PageTurned` fires | §8.4, §11 |
| exit | `EndInspect`. timeScale returns to 1. The page is remembered, and the RT renders FRONT | EX C5 |

### 4.4 Edge cases

| Case | What happens | Source |
|---|---|---|
| A threat the player can perceive | No prompt. E does what the aim does without the target (on a desk, nothing) | §6.1 |
| Warning stage 1 or 2, and no other term | Opening is allowed. Everything freezes during the close-up | EX §2.5 W |
| The terminal's chunk unloads mid-read | `AbortRequested` → `EndInspect` + `CancelShot(shot, 0)` | EX §2.5 U, C12c |
| Caught during the return | `Abort()` + `rig.ResetLayers()` | EX §2.5 X |
| Focus lost or the app paused | Paused with `pausedFrom = Inspect`. Resume returns to the same close-up and page | EX §2.5 F, C10 |
| **WebGL pointer lock lost** (browser Esc, alt-tab) | The same Paused, never Playing (§7.2) | L3 |
| A page key held when the close-up opens | Turns no page until it is released and pressed again | §7.1 |
| A, D or an arrow held at exit | **New latch C25b:** movement ignores that key until it is released, like the S latch (C25) | EX §2.5 H |
| Space or Enter held at exit | Harmless: Start is read only on the title screen | `Game.cs:2257` |
| The press or tap that opened the close-up | Never turns a page (input is swallowed for 0.20 s) | EX `CancelAfter` |
| R or O in a close-up | Ignored | EX §2.5 R |
| Two terminals within r_A on handheld or WebGL | The nearest gets the RT; the other shows TELL | §9.2 |
| Revisit shift of the host chunk | The desk may move. The content set and the page memory stay. A neighbour's shift changes nothing | §3.4 |
| Dark room or dead lamp | The terminal stays on (its receptacle circuit, §6.3). EXAMINE's adapt lift (up to +2.25 EV in a dark cell, EXP §1 V1-3) is off for screens (`adapt = false`) | §9.4 |
| Autopilot or baseline runs | Never opens | EX C4 |
| Handheld before B-touch | Glance only: no prompt and no index | §7.3 |

---

## 5. Pause or live (DECIDE D2, which is EXAMINE's D1 amended)

**Recommended: paused, exactly like every EXAMINE close-up.**

**Why:**
1. **One rule.** Red already asked that close-ups pause (EX header). One rule for every close-up is easier to learn.
2. **Muted phones.** Stage 2 adds the steps to the lamp bursts, which keep coming at 2 dips per burst (RELAY §9.1–9.2). In a close-up the room's lamps are mostly out of frame, so the bursts are hard to see. A muted phone also loses the steps. Live reading would leave a muted phone player with almost no warning.
3. **Reading speed.** The strings are 1990 US English. Slow, dyslexic and non-native readers should not read against a clock.
4. **No new Relay rules.** EXAMINE's gate, freeze, sound snapshot S1 and pause-neutrality test (AT-01g) already cover it.
5. **A paused close-up is neutral, not a safe room.** It advances no clock: not RELAX, not the dry clock, not onMapCap, not Attention decay. The player gains nothing and loses nothing. The live tension stays at the glance, where the world keeps running.

**Costs:**
- The screen UI must work at timeScale 0 (§9.1).
- Captions are hidden during the close-up (EX §5.1). No new caption can arrive while SFX are paused.

**Alternative: a live setting.** This is EXAMINE's mode C, `Close-ups pause: Off` (EX C24), with On as the default. If Red wants it, all of these rules apply:
- **Only after RELAY step 4,** when the v2 hunter is the default. RELAY §13 plans a `FRONTROOMS_RELAY_V2` define for this switch; it is not in the code yet. Under v1 the Relay sees 360° at 12 m with no warning (RELAY §2.6), so live reading would be unfair.
- **Forced exit** on the frame `ThreatPerceivable` (§6.1) turns true. That includes `TargetAcquired`, which must fire while the close-up is open. The camera is home within 0.15 s, and no catch is possible before control returns.
- **No S1 snapshot,** so the stage-2 steps and the groan stay audible.
- **Captions and the marker stay visible.**
  - On desktop the framing keeps the bottom 22 % clear for captions. Caption placement is 关卡设计's (`Game.cs:1984`).
  - The "Visual sound cues" marker stays visible.
  - On handheld, choosing live also turns that marker on, because a close-up hides most lamp bursts and a muted phone loses the steps. The player can turn it off again.
- **No masking.** UI Foley must not mask the Relay's OPEN latch click or the CALLED tick. 声音设计 runs an A/B listening test.
- **Standing still counts.** Reading is standing still, so the freeze rule (×0.5 spotting beyond 6 m) and the dry clock apply as they do anywhere (RELAY §6, §8).
- **Live neutrality test:** 30 s of reading and 30 s of standing still give identical director and hunter traces (AT-T17, AT-T20).
- **Cost:** about 3.5 days (§15.3).

---

## 6. Relay interplay (系统设计 rules)

### 6.1 The prompt gate: it hides only for threats the player perceives, and returns only on a change they perceive

**The inherited gate leaks.** It is EX §2.5 C, built as C4's `RelayBlocksInspect` (EXC `map_inspect.md`): `Released && (Chase || BreakDoor || PlayerSeesRelay)`.

| Fault | Why it leaks | Evidence |
|---|---|---|
| `PlayerSeesRelay` has no light and no pitch | It is a flat ±60° test. It reaches out to `SightDistance` = 2 chunks × 24 m − 2 = **46 m**, but lamps are off beyond 16 m. A Relay in a dead cell nearby also counts. At a terminal the player looks 30–42° down, and the vertical half-FOV is 38°, so a Relay at head height straight ahead can be above the frame and still count. On 16:9 the horizontal half-FOV is about 54°, so a Relay 54–60° off to the side counts too | `MapHunter.cs:75`, `:1262-1279`; `MapWorld.cs:71`; `Game.cs:280`; RELAY A10 |
| `BreakDoor` at any range | In v1 a blow is a 3D FMOD one-shot that is silent past the event max of 36 m and captioned only within 36 m. The gate counts BreakDoor at any range | `fmod_frontrooms.py:722`; `SoundDirector.cs:567-580` |
| v2 `Spotting ≥ 0.5` | The player can perceive it only while the scuff can be heard or the Relay is seen (RELAY §4: "while the stage-2 audio is audible or `PlayerSeesRelay`") | RELAY §4, §8 |
| **No hold** | The prompt returns the moment a term goes false, even when nothing perceivable changed. Spotting drains silently 0.3 s after sight ends (RELAY §8). Stage 3 unlatches at LoseTrack while the chase bed still fades for 3 s (RELAY §9.4) | RELAY §8, §9.4 |

A prompt that vanishes or returns with no cause the player can perceive tells them something about the Relay. That makes the prompt a detector.

**The fixed predicate `ThreatPerceivable`** (CONTRACT 关卡设计, L4). It is one property, read by:
- the terminal's gate;
- every EXAMINE close-up's gate, if Red answers D2 as recommended;
- the live-mode forced exit.

**Hide when any term is true:**
```
v1 (main's hunter, until RELAY step 4):
  Released && ( State == Chase
             || (State == BreakDoor && BlowHeard)
             || RelayOnScreen )

v2 (from RELAY step 4; these terms do not exist in main yet):
  OnMap && ( WarnStage >= 3
          || (Spotting >= 0.5 && (StepsAudible || RelayOnScreen))
          || (State == BreakDoor && BlowHeard)
          || RelayOnScreen )
```
L4 builds the v1 form now and switches to the v2 form inside RELAY step 4.

**Hold: after a term goes false, the prompt stays hidden until something the player perceives changes.**

| Term | Stays hidden until | What the player perceives |
|---|---|---|
| Chase (v1), `WarnStage >= 3` (v2) | 3 s after the chase ends | The chase bed fades over 3 s at LoseTrack (RELAY §9.4). v1's Lost stinger plays at the end |
| Spotting (v2) | `StepsAudible` turns false, or the Relay's steps resume while `StepsAudible` | The steps and hum fade out, or the steps start again |
| BreakDoor | The break is heard (Break event, 40 m), or no blow has been heard for `BlowGap`: the hunter's longest gap between blows + 1 s (ESTIMATE) | The door gives, or the blows stop |
| RelayOnScreen | no hold | The player sees it leave the frame |

**Definitions:**
```
RelayOnScreen = RelayInView(this device's render camera: its FOV, aspect and pitch)
             && RelayLit
RelayInView(frustum) = both Relay points (1.6 m and 1.95 m) project inside the frustum
                    && both are visible from the eye (the existing Visible rays)
RelayLit = the XZ distance is <= 16 m (lamps fade from 13 to 16 m and are off beyond; RELAY A10)
        && W >= W_seen, where W = min(1, L + 0.02 · ΣL_n) over the Relay's cell and its 4 neighbours
           (map.LampLevel; EXAMINE's adapt formula as built, EXP §1 V1-3)
           W_seen: ESTIMATE 0.15, set on captures so that the 1.95 m silhouette
           reaches 3:1 against its background
StepsAudible (v2) = the stage-2 steps and coil hum have gain > 0 under RELAY §9.3
                    (stage >= 2 and F < 24 m, or PlayerSeesRelay).
                    The sound reads this same property for the gain.
BlowHeard = a door blow sounded within the last BlowGap seconds, at a distance the sound plays it:
            v1: straight-line 3D distance <= BlowAudibleMetres = 36 m
                (the FMOD event max and the caption radius)
            after RELAY §13 audio item 8: also PathDistanceToPlayer <= 45 m
```

**Notes:**
- **Never `SeesPlayer`.** In v2 it is silent, so a prompt that reacted to it would be a detector (EX §2.5 C).
- **One property for the gate and the sound.** `BlowAudibleMetres` and `StepsAudible` are 关卡设计 values that 声音设计 reads (S-T2). They cannot disagree.
- **The light term errs on purpose toward keeping the prompt.** A Relay the player cannot make out (in a dark cell, or beyond 16 m) does not hide it. The other error, hiding the prompt for a Relay the player has not noticed, would make the prompt a detector. Keeping the prompt costs nothing: the close-up pauses the world (§5), so nothing can happen while the player reads.
- **Known gap, accepted:** a Relay beyond 16 m, or in a dark cell, can show as a silhouette against a lit screen or another emissive surface. It does not count, so the prompt stays. The same reasoning applies: a paused close-up is neutral.
- **Per device on purpose.** RelayOnScreen uses this screen's frustum. An iPad at 1.44 sees ±48° across, and a 16:9 screen ±54°. A UI gate must match what this screen shows, or the band between the two becomes a detector on the narrower screen. The director keeps a platform-identical test (§6.6).
- Tests are in AT-T15.

### 6.2 Attention and noise

| Act | Attention | Heard by the Relay on the map | Why |
|---|---|---|---|
| Walking up, aiming, glancing | 0 | no | RELAY §6: "looking 0" |
| Opening, paging, closing | 0 | no | Not a noise, not a device, never a `Disturb`, never a trigger |
| Standing at the terminal | 0 | — | The freeze rule applies, as it does anywhere |

Terminal Foley is never routed to `Noise` or to the hunter's hearing (AT-T17).

### 6.3 Warnings, screens and power (DECIDE D8)

**In v1, terminals never react to Relay warnings, and they ignore lamps.** This is the simplest honest rule:
- a screen can never work as a readout of the lights or of the Relay's distance;
- the Relay's sight has no light term, so a lit screen never attracts it (RELAY §8);
- whether screens join the warnings stays Red's SCREENS §4 decision.

**If Red later approves SCREENS §4 for screens:**
- A terminal reacts only to **`WarnBurst`**, the room-set event (RELAY §9.2; built in RELAY step 7; not in main). It fires in audio-only rooms too, so the screen carries exactly the same information as the groan.
- **No per-screen Relay exclusion.** In a close-up the screen is the only thing in view. "The groan plays but the screen does not sag" would mean "it is within 2 cells of you".
- Screen warning effects run on game time, so they freeze in a paused close-up. Nothing scary happens inside a close-up (EX P7).
- The chase-wave slate follows SCREENS §4. The wave is centred on the player's room set (RELAY §9.4).

**Power (fiction for narrative to rule on; recommended).**
- The terminal sits on a receptacle circuit separate from the lighting. 1990 US offices commonly ran fluorescent lighting on 277 V circuits and receptacles on 120 V (UNVERIFIED for this building; narrative checks).
- So a dead or failing lamp does not darken it. Terminals are never in dead-circuit rooms (P7).
- The alternative: the terminal follows its cell's generated lamp temperament (rule 3 below), as long as AT-T02's glance contrast still passes.

**SCREENS §3 amendment (send to 平面视觉; it applies to every screen):**
- **The problem.** SCREENS §3 makes screens sag and roll on `FixtureChanged` / `LampDipped`. But the ink's stage-0 power sags (1.6 s attack) and the chase-wave dips (0.3 s / 1.2 s) fire those same events (RELAY §9.2). A screen that sagged or rolled on them would show the warning signature at stage 0. That breaks rule G7: "ambient keyframes never use cue vocabulary" (RELAY §9.6).
- **The amendment:**
  1. No screen uses the sag, shrink or roll signature, except on `WarnBurst`, and only if Red approves SCREENS §4.
  2. No screen reads `LampLevel` or any Warn override level.
  3. An ambient set may follow only its cell's **generated** lamp temperament. That is the map's own roll without run-time `SetLampMode` modes: `MapWorld`'s private `LampModeOf(cell, withSet: false)`, which 关卡设计 would expose (CONTRACT).
     - **Why not `LampBaseLevel`:** during a warning, RELAY §9.2 puts the burst set's lamps to Steady with `SetLampMode`. The base level therefore changes during warnings, and a screen that followed it would be a readout.
  4. Any change is slow (attack ≥ 1.5 s), with no shrink and no roll.

### 6.4 What a terminal must never show or do

1. No Relay position, distance, direction, state, stage, Attention, Pressure, director phase, budget, call estimate, queued call, sweep target or tier number (RELAY §3.1, §6).
2. **Live clock (since 2026-10-22; it replaces "no live clock").** The header shows local real time as `h:MM AM` / `h:MM PM`, right-aligned, with no seconds and no blinking colon. Dates are the real month and day with the year 1990 and the 1990 weekday (for example `MON 10/22/90`). One shared source, 游戏视觉's `FrontRoomsRealTime`, gives those strings to every clock and screen. Red asked for it in the 游戏视觉 chat on 10-22 (RES :556 note). The old reason, that a player could count down RELAX, is accepted: RELAY §11 explains why it costs little.
3. **No all-clear wording,** fixed or changing: no `NORMAL`, `ALL CLEAR`, `SYSTEMS OK` or `ALL SYSTEMS NORMAL`. A fixed one lies during a hunt; a changing one is a readout.
4. **No change at Withdraw, Away or RESTORE.** "It has gone" is audio only (RELAY §9.4, §9.6).
5. **Nothing that maps threats or safety:**
   - no live door or window state (open, ajar, broken, pried);
   - no foil or devices;
   - no trigger rooms;
   - no Relay entries or dead ends, and no interior walls;
   - no sanctuaries or dead circuits;
   - no sweep targets.

   Broken doors matter here because they are traces of where the Relay walked (RELAY §10).
6. Never the word "relay". Never RUN or HIDE. Nothing that implies the dark hides you or that screen light matters (STR §5, §7; LD R13).
7. No sound in a Relay cue family, and no tonal sound (§11).
8. Never the only source of anything essential (P8).
9. No zone, compartment or desk numbers until one numbering system is agreed (RES "numbering"). The header names the desk's user instead (§8.1).
10. Nothing lies before T4. Any later lie has a tell and is never a building plan (RES :537-539).
11. **Tier-dependent strings** read only the zone's **stored first-generation tier** (RELAY §12), never the current tier, so a revisit never changes them.
12. **Teaching is true for the platform and for the Relay version that ships** (§8.6).

### 6.5 Invariance test

**What stays identical:**
- the glance texture, which is always FRONT, including after any read;
- every page's strings.

**Across what:**
- any Relay position, state and warning stage from 0 to 2;
- any director phase;
- any door state (open, ajar, broken);
- Withdraw, Away and RESTORE;
- a tier change after the zone was generated.

**What the content provider never reads:** hunter, director, door, lamp or current-tier state (AT-T16).

**What the deal of sets depends on:** only the seed and the order in which terminals first become ACTIVE (§8.5).

### 6.6 RELAY §8 amendment: two named sight tests (系统设计 owns RELAY; this text goes into RELAY §8 with this spec)

RELAY §8 defines one `PlayerSeesRelay`, "reused everywhere" and platform-identical. Main builds it as a flat ±60° test with no pitch, reaching 46 m. That one test cannot serve both sides:
- the gate needs the device's own framing and a light term;
- the director must stay platform-identical.

So RELAY gets three named tests that share their parts:

| Test | Definition | Platform | Read by | Why |
|---|---|---|---|---|
| `PlayerSeesRelay` (fixed) | `RelayInView` on a reference frustum: vertical 76°, aspect 16:9 (±53.9° across), the camera's real yaw and pitch. Range `SightDistance`. No light term | identical everywhere | Withdraw and FADE ("never vanish in view"), Arrive ("never appear in view"), the stage-2 footstep rule (RELAY §9.3), `NotifyPlayerSeesRelay` | These rules must hold whenever the Relay might be seen, so a dark Relay still counts; that is their safe side. Platform-identical, so bots and phones meet the same Relay |
| `PlayerPerceivesRelay` (new) | `PlayerSeesRelay && RelayLit` | identical everywhere | Pressure: "+4/s you see it", and the "almost" +5/s term, which then reads "while `StepsAudible` or `PlayerPerceivesRelay`" | RELAY §4: Pressure is fed only by cues the player perceived |
| `RelayOnScreen` (new) | `RelayInView` on this device's own frustum `&& RelayLit` | per device | the terminal gate, every EXAMINE gate (D2), the live-mode forced exit | A UI gate must match this screen |

- In main, nothing reads `PlayerSeesRelay` except the hunter itself and the unwired policy (grep 10-14). So the fix changes no live behaviour.
- 关卡设计 gets one contract for all three (L4).

---

## 7. Input per platform

### 7.1 Controls

| | Desktop (reference) | Touch (iOS, Android; `FrontRoomsHandheld.Active`) | WebGL |
|---|---|---|---|
| Glance | nothing to press | nothing to press | as desktop |
| Open | E (`Input/FrontRoomsInput.cs:256`) | USE with the READ glyph (EX C14, T2). A tap on the screen where it starts in the look zone (EX C15) | E |
| Next page | **D, →, ↓, Space or Enter** | the **PAGE chip**, or a **tap on the right half** of the screen | as desktop |
| Previous page | **A, ←, ↑** | a **tap on the left half** of the screen | as desktop |
| Look | mouse: ±3° (EX flat kind) | a drag that starts on the screen: ±3° | mouse |
| Scroll wheel | nothing in v1. Once EX T3 lands, one scroll step turns one page on targets with pages (down = next) | — | as desktop |
| Zoom | none | none | none |
| Exit | §7.2 | §7.2 | §7.2 |

**Rules:**
- **Each key is its own button.**
  - A press of any page key is one step, even while another page key is held. Chords count every press.
  - A held key never repeats.
  - A key already held when the close-up opens turns no page, because it makes no new press.
  - **Why:** `Move` combines two 2DVector composites (`FrontRoomsInput.cs:232-241`), so its x value can miss a press or fire on a release.
- **Input path.** `FrameSnapshot` gains one field, `int PageStep`: the net page steps this frame (CONTRACT touch chat, TT0).
  - The desktop reader builds it from per-key press edges: D, →, ↓, Space and Enter give +1; A, ← and ↑ give −1.
  - The touch layer writes it from the PAGE chip and the screen taps.
  - In Inspect the game reads only `PageStep` (L3), so the touch stick can never turn a page.
- **Rate.**
  - At most one page turn per 0.34 s.
  - One press inside that window is held and applied when the window ends. Further presses are dropped.
  - **Why:** at most 3 changes per second (WCAG 2.3.1; RELAY §9.5), even when mashed. Four presses per second still turn four pages.
- Pages wrap both ways. Nothing happens before 0.20 s (EX `CancelAfter`).
- **Why these keys:**
  - Arrows and A/D are where players look for "next".
  - ↑/↓ are the 1990 program's own keys.
  - Space and Enter mean "continue". `Start` does nothing outside the title screen.
  - S is not a page key: it exits (`ShotBack`, `FrontRoomsInput.cs:275-277`). W does nothing.
- **Never F1–F12, Tab or Alt+letter:**
  - F5 can reload a WebGL page, and browsers reserve function keys;
  - Mac F-keys need Fn;
  - on Windows, F10 and Alt open the window menu (RES critic gaps).
- Digits are unbound. The FRONT index prints no numbers until D11 (b) wires digit jumps.
- The left mouse button does nothing, so the click that re-locks a WebGL pointer is harmless.
- **Why no cursor in v1:** in a close-up the crosshair fades out, and the centre ray cannot sweep across the screen (RES critic :1192). A cursor needs a virtual pointer plus UV picking: a new layer, already agreed as 游戏视觉's EXAMINE extension. It is D11 (b) and §13 E1.
- **The on-screen footer never names a key.** It shows `PAGE n OF m` between two arrows drawn as shapes (§8.1). Key names live only in the per-platform HUD hint (`A D · PAGE` on desktop). So a touch build never tells the player to "press ESC".
- Gamepad is out of scope. Only the right stick is bound (`:248`; RES critic).

### 7.2 Every way out

| Way out | Mac/Win | WebGL | Phone/iPad |
|---|---|---|---|
| E | yes | yes | — (the touch layer replaces the whole input snapshot, so hardware keys do nothing: `FrontRoomsInput.cs:138`; `FrontRoomsTouchControls.cs:276-286`) |
| S (latched until released, C25) | yes | yes | — |
| Esc | yes | not relied on (the browser may not forward it, and it releases the pointer lock) | — |
| HUD BACK button | — | — | yes (§7.3) |
| A pull-back drag that starts **outside** the screen, in the left zone | — | — | yes |
| Android Back | — | — | yes: it exits, as in EX row B |
| Target gone | `AbortRequested` | same | same |

**Not exits but pauses:**

| Event | Every platform |
|---|---|
| Focus loss, app pause | Paused with `pausedFrom = Inspect` (EX C10). Resume returns to the same close-up and page |
| WebGL pointer lock lost (browser Esc, alt-tab) | the same Paused |

**WebGL pointer lock (CONTRACT 关卡设计, L3).**
- **Today:** main has no pointer-lock handling (§1).
- **In Inspect, on WebGL only:** poll `Cursor.lockState` every frame, behind the WebGL gate in §9.2. A lock that is no longer `Locked` sends the game to Paused with `pausedFrom = Inspect`, as a focus loss does.
  - **Never Playing.** The failure this rule prevents is dropping out of a close-up into a live world with no mouse look, possibly at warning stage 2.
- **One rule for browser Esc and alt-tab.** Alt-tab fires both a focus loss and a lock loss. Whichever arrives first pauses the game; the second finds it already Paused and does nothing. Both orders end in the same state.
- **Resume** re-locks the pointer and returns to the same close-up and page. The player resumes with Esc, which the browser forwards once the lock is gone, or with the card. Paging uses no mouse button, so the click that re-locks cannot turn a page.
- **UNVERIFIED:** whether Unity 6's WebGL player updates `Cursor.lockState` when the browser drops the lock. Prove it in a clone first. The fallback is a WebGL-only `.jslib` listener on `pointerlockchange`.
- **Out of scope:** outside close-ups the game still has no lock-loss rule. That is today's behaviour; it is noted for 关卡设计 in §18.

**Exit latch C25b:** page keys that are also Move keys (A, D, ←, →, ↑, ↓) and are held at exit are ignored by movement until released, like the S latch (C25).

All exits work after 0.20 s. **On every platform, Esc is never the only way out.**

**Words.** The program never prints a word for leaving.
- EXIT is reserved for the placard's grammar (WAY ON / EXIT / NO EXIT).
- BACK is the HUD button.
- The HUD's existing exit hint (`E · BACK`, EX §5.1) does the job.

### 7.3 Touch routing (CONTRACT touch chat, TT1)

**Coordinates.** Points in `FrontRoomsTouchLayout`, with the origin at the bottom left (`FrontRoomsTouchLayout.cs:4`). EXAMINE's touch contract gives y from the top; its numbers are converted here.

**Prerequisites.**
- READ on touch needs EXAMINE's T1 (`MenuState.Inspect`), T2 (USE shown as BACK), T6 (the TEXT chip) and T7 (the panel rect). None has started (EXC `touch_inspect.md`; EXR §6).
- Until they land, a handheld terminal is glance-only. It registers no READ target, and FRONT shows no index.
- Without T1, the interim keeps `MenuState.Playing` in Inspect (EXC C13). Paging reads only `PageStep`, so even then the stick could not turn a page.

**Opening.**
- USE with the READ glyph opens it from anywhere in the stand box.
- A tap on the screen opens it only where the tap starts in the look zone, as for every EXAMINE target (EX C15; `LookTapped` fires only there, `FrontRoomsTouchControls.cs:843`).
- The move zone is not taken in Playing: a player walking past would lose the stick.

**In `MenuState.Inspect`, when the target has pages:**
- **Taps.**
  - The whole face rect takes taps, in both halves.
  - That includes the part of the face inside the read-only top band (`TopBandBottom`, `FrontRoomsTouchLayout.cs:90`). On the 874 × 402 phone the face top is at y 345 and the band starts at y 338.
  - The face rect comes from `FrontRoomsInspect.CurrentFaceViewportRect` (V1), converted to layout points.
  - A tap acts on touch-up, highlights nothing, and writes `PageStep` ±1.
- **What counts as a tap:** a touch that moves less than `TapSlop` 8 pt within `TapMaxSeconds` 0.22 s. This is the existing test (`FrontRoomsTouchControls.cs:116-117`).
- **Double-tap.** The double-tap detector is off on targets with pages, because they have no zoom step. Two quick taps are two page turns, never a ZoomToggle (EX T1).
- **Drags.** A drag that starts on the face looks ±3°. It **never** exits.
- **Pull-back.** The hidden left stick's pull-back exits only when the drag starts **outside** the face.
- **The opening press.** The press or tap that opened the close-up never turns a page (`CancelAfter`; T1 resets detectors on every MenuState change).

**Buttons, placed from the safe area's bottom-right corner:**

| Button | Centre | Hit |
|---|---|---|
| BACK (EX T2) | (Frame.x − SafeRight − 44, SafeBottom + 44): (768, 65) on 874 × 402 | Ø88 |
| PAGE | 100 pt left of BACK: (668, 65) on 874 × 402; (523, 44) on a 667 × 375 phone | Ø88. It shows only when there is more than one page |

**Why relative:** fixed points from the 874-pt layout land off-screen on a 667-pt phone.

**Framing on handheld.** The face is framed in the safe area minus the right 200 pt (the button column), so it never sits under PAGE or BACK.

| Device (layout pt) | Face x | Face y | PAGE hit x | Gap |
|---|---|---|---|---|
| iPhone 16 Pro, 874 × 402 (insets L/R 62, B 21) | 160–514 | 78–345 | 624–712 | 110 pt |
| Phone, 667 × 375 (no insets) | 59–408 | 56–319 | 479–567 | 71 pt |
| iPad 11-inch, 1180 × 820 (inset B 20) | 170–810 | — | 992–1080 | 182 pt |
| 874 × 402 with transcripts on | 124–480 (the left 64 % of the safe area) | 78–345 | 624–712; the panel is x 542–812, y 122–338 | PAGE hit top y 109 is 13 pt below the panel |

Left-handed mirror as in TOUCH_CONTROLS.

---

## 8. The UI system and content

### 8.1 The shell

One fictional office program runs on every terminal. Narrative names it, and it is not a real product.

- **Grammar.** It borrows from IBM CUA (1987) and DOS-era office software, never their names or strings (RES :358-368, :407):
  - **header bar** (row 1): program name · the desk user's name (fictional, never a number; §6.4 item 9) · the live time (`h:MM AM`, §8.6 rule 2);
  - **body**;
  - **footer** (row 15): `PAGE n OF m` between left and right arrows.
- **The arrows are drawn as shapes by 平面视觉, not set as font glyphs.** The interim face, VT323, has no ◄ or ► (§1).
- **The front page is the program's menu, printed.**
  - Phase A: the headline and the second line.
  - Phase B, on platforms where READ is on: the page titles as well, with no numbers until D11 (b).
- **Behaviour:** pages only, with hard cuts. No input fields, selection or menus in v1.
- **No cursor in v1.**
  - No input field exists, so a blinking cursor would invite typing that does nothing.
  - It would also be the only thing on screen that changes over time (§9.1).
  - The cursor layer (E1) may add one, drawn in the screen shader.
- **One field colour on every page.** A page turn then changes only a small text area, so it can never be a flash (WCAG 2.3.1).
- **Is a pager enough for Red's "its own UI system"? DECIDE D11**, with G0's mock frame.
- **Fiction (DECIDE D6):** recommended (a), a tenant office PC with mail, memos and a print queue. Office zones are tenant fit-out, off the base-building plan (NP:79, :211; RES critic :1187). So the building plan stays on the placard.

### 8.2 Raster and grid (one raster, one render path per tier)

| Item | Value | Why |
|---|---|---|
| Logical raster | **640 × 480**, 16 colours (VGA graphics mode, 1987), square pixels | It fits the era. A later plan page can draw bitmaps, which a text grid could not (RES critic) |
| Active area | **0.276 × 0.207 m (4:3)**: the terminal's own quad, a separate shared mesh, not the 0.28 × 0.19 ad quad. It is centred in the kit's 300 × 228 mm bezel opening, so about 12 mm of the kit's own dark glass shows left and right, and about 10 mm top and bottom. That is the computer CRT's underscan border (Red's pick, 2026-10-22, VL429; `research/screens/NOTE_1022_crt_border_READ_FIRST.md`). The quad does not flip U (the ad quad's flip is for the VideoPlayer, `ScreenVideo.cs` `MakeQuad`) | The ad quad is 1.47:1, so a 4:3 RT on it would stretch every glyph 10.7 % across. (The same pick moves the ad picture to this 4:3 area too; until that lands in the kit, the ad quad stays 1.47:1.) The ad shader has only `_BaseMap_ST` and Clamp wrap, so it cannot draw a black border (`Resources/Rendering/FrontRoomsScreenVideo.shader`). Computer CRTs underscan, so there are no TV safe areas (RES critic) |
| RT size | desktop **1280 × 960** (exactly 2×); handheld and WebGL **640 × 480** (1×) | Integer scales only. Desktop gets 2× so the tube's mask and curvature resample cleanly (RES finding 7) |
| Grid | **40 columns × 15 rows** of 16 × 32 logical px | 15 rows is the most that reads on a phone at 66 % fill |
| Rows | 1 header, 11 body, 1 footer, 2 spacers | 11 lines × 38 characters ≈ 70 words, about 20 s of reading |
| Pages per terminal | front page + **at most 3** | Rarity (EX P11): about 1 minute of reading per terminal |
| Not used | VGA text mode, 720 × 400 | It would need a non-integer scale onto the 4:3 RT, and it cannot draw a plan |

If 平面视觉 picks a bitmap face, every size is a whole-number multiple of its pixel.

### 8.3 Legibility envelope

Caps are shares of the 480-line active height. "About N characters" assumes each character is 0.8 cap heights wide (ESTIMATE until the face is picked).

| Text | Cap | Fits | Reads where (§2.3, §2.4) | Why this size |
|---|---|---|---|---|
| **Headline** (front page) | **25 %** (120 px) | about 6 characters, 1 line | desktop to 2.9 m; phone to 1.25 m (the whole stand box); iPad to 2.3 m | It reads at the 2.4 m aim reach on desktop (14.6 px, 22 % margin) and across the stand box on a phone (≥ 22.1′, 19 % margin) |
| **Second line** (front page) | **18 %** (86 px) | about 8 characters, 1 line | desktop to 2.1 m; iPad to 1.6 m; on a phone only in READ | It reads at the 1.6 m prompt edge on desktop (15.4 px, 28 % margin) and on an iPad (19.1′, 3 % margin), so a desktop or iPad player reads both lines before the prompt shows |
| **Body** (every page) | **4.2 %** (a 20 px cap in a 32 px row) | 38 characters × 11 lines | close-up only: desktop 49′, phone 21′, iPad 33′ | Phones need at least 58 % fill to read 15 rows |

- **Front-page height budget (phase B):** header 32 + headline row 150 + second-line row 110 + index 3 × 32 + footer 32 = 420 px. That fits in 480 with two spacer rows.
- Sizes are measured from layout constants, never by eye (Red's rule).

### 8.4 Page templates

| Template | In v1? | Content |
|---|---|---|
| **FRONT** | yes | the headline, the second line, the page index (phase B) and the footer. It is the glance |
| **TEXT** | yes | one memo per page: From / To / Re lines, then the body |
| **LIST** | yes | the print queue as a short table |
| PLAN | later (§13 E2) | a floor plan (bitmap) |
| LOG | later (§13 E3) | alarm history |

**The page model is data:** `PageDef { template, stringIds[], bindings[] }`. `bindings` is empty in v1, so later data pages need no change to the renderer.

### 8.5 v1 content (DECIDE D5)

**A desk set** is FRONT plus 2 pages from this pool.

**Facilities memos (narrative).** Each is at most 11 lines.
- **The v1 pool says WALK, DO NOT RUN, and that breaking glass sounds the alarm.** It has **no door advice.**
  - Under main's v1 Relay, every shut door is a 19.6 m noise. It sends the Relay into Hunt, and the Relay can then break that door (RELAY A3, §2.2).
  - So a memo saying CLOSE DOORS BEHIND YOU would teach v1's beacon.
  - Door memos are written now and merged with RELAY step 4.
- Memos stay consistent with the placard footer (A.12) and never contradict any platform's pause card. They never name keys: the pause cards do that.
- One thread runs between R.M. and D.K., two ordinary memo writers, initials only (叙事设计, 10-22; `research/station/01_research.md` §6). R.M. is facilities or building management: drill notices, posted rules, the plan. D.K. is the night desk, the same initials as the security desk's shift log. They are not another occupant: no lure, nothing about the guard's fate beyond plain duties, no full names. No fixed 1990 dates on screen. Under the live-calendar rule (`research/station/01_research.md` §5), the newest memo is dated this month and the older one four months back, both computed from the live date with 1990 weekdays. A future date, such as the next drill, prints month and day only, never a 1991 year.
- **Pool size:** set after L6 measures terminals met per run. Interim: 8 memos.

**Print queue: a tenant print job,** such as a memo or a form. Narrative writes it.
- **Not `PLAN SHEET A-3 OF 4`.** That is the base building's drawing set (STR §6), which D6 (a) keeps off tenant PCs. A-3 is also the sheet the placard's MATCH LINE names, and the player can never find it.
- **The T2 `PRINTING SHEET A-1114 OF 4` line is dropped from v1.** It is an anomaly before T4 (§6.4 item 10). Narrative may move it to T4+ with a tell.

**The deal: no repeats within a run.**
- A seed-keyed permutation of the sets is dealt to terminals in the order they first become ACTIVE.
- The result is stored by content key for the run, so a revisit shows the same set. Restart deals again.
- If a run meets more terminals than there are sets, a second permutation starts; repeats are accepted from then on.
- **Why:** picking by `contentKey % N` repeats often. With N = 4 sets and 3 terminals in a run, 62 % of runs repeat a whole set (1 − 4·3·2 / 4³).

**Not in v1:**
- **FLOOR PLAN.** It needs `FrontRoomsWayfinding`, Red's exit definition, one numbering system, a PLAN template, and a building-services station under D6 (b) (§13 E2, E7).
- **ALARM HISTORY.** It needs the v2 director's `Called` and `Restore` (RELAY step 2; §13 E3).

### 8.6 String rules (narrative writes; 平面视觉 checks the fit)

1. No real brand, product, station, person, address or working phone number. **Dates are 1990 or earlier** (STR §7). No MS-DOS error text unless narrative rules on it (RES :644).
2. Everything in §6.4: never "relay", no all-clear, no RUN or HIDE, nothing about darkness. The clock is live now (§8.6 rule 2).
3. **Teaching is true for the platform and the Relay version that ships.**
   - Memos never say walking is silent, because on touch the Relay on the map hears the handheld walk gait at 9 m (RELAY §10).
   - No door advice before RELAY step 4 (§8.5).
4. **Memos never describe the warning cues** (lamps, steps, chime). This is inherited from RELAY §10: "The warnings are explained once in the pause-card legend, never live."
5. No word for leaving the program (§7.2).
6. Every string fits its envelope at 平面视觉's real sizes, and every character is in the chosen face (G1).

### 8.7 Colour and type (平面视觉 decides)

**G0 result (平面视觉 · 信息设计, 2026-10-15; Figma section 3218:7277, TM01–TM04):**
- **The face is an 8 × 16 bitmap.** VT323 cannot sit on the 16 × 32 cell at whole pixels (0.4 em advance, 0.56 em cap), so it stays only as a stand-in. An 8 × 16 bitmap face fits exactly: 2× gives the 20 px body cap, 9× the 90 px second line, 12× the 120 px headline.
- **D10 becomes "which 8 × 16 face":** draw our own (no licence question), a permissive one (being checked), or Px437 (CC BY-SA).
- **Recommended field:** VGA blue #0000AA with #AAAAAA / #FFFFFF. Amber is taken by the TROUBLE lamp.
- TM03 confirms the §2.3 reading distances.

**The field colour must be unique to terminals.** It must avoid every colour that already has a meaning (RELAY: "each cue has one meaning"):

| Excluded colours | Already mean |
|---|---|
| Glow-ink greens (#B6F5A0–#C9F7B0) and VGA greens (#00AA00, #55FF55) | building guidance in the dark (NP:336) |
| Ambers and yellows (#AA5500, #FFFF55, P3 amber) | the sanctuary's TROUBLE lamp (RELAY §10) |
| Reds (#AA0000, #FF5555) | the ALARM ZONE card at trigger rooms (RELAY §6) |

- **Candidates left:** the VGA blue field (#0000AA) with grey or white text (#AAAAAA, #FFFFFF), or white phosphor on black.
- **Recommended to 平面视觉: blue.** STR §6's ambient frames (`C:\>_`) are grey on black, and the terminal must not share their field (AT-T19). If D14 drops those frames, white on black is open again.
- With blue, avoid WordPerfect's trade dress: a blue field with a `Doc 1 Pg 1 Ln 1` status line.
- **No meaning by hue alone** on later pages. The redundant words must be neither all-clear words nor cue words: not NORMAL (§6.4 item 3) and not TROUBLE (the sanctuary lamp). For example, ALARM / FAULT / OFF, checked with narrative.

**Face (平面视觉's choice; the licence question is D10).**
- VT323 (OFL, in `Assets/Fonts/Period1990/VT323`) is a DEC VT320 terminal face, a stand-in on an IBM-compatible PC. It also lacks ◄, ► and █ (§1).
- 平面视觉 either approves it as the interim face or draws or picks an OFL IBM-style 8 × 16 or 9 × 16 bitmap face.
- G1 checks every string against the face's cmap. Arrows and blocks are drawn as shapes.
- Px437 IBM VGA (CC BY-SA 4.0) waits for Red's licence answer (D10; RES :1190).

---

## 9. Rendering and platform tiers (游戏视觉)

### 9.1 Render path

- **UI Toolkit renders into `PanelSettings.targetTexture`,** with no extra camera, on every tier.
  - **The targetTexture precedent:** 平面视觉's promo harness (UNorm target, `forceGammaRendering`, tube `_SignalDecode`; RES finding 5).
  - The game's logo panel (`Game.cs:1667-1680`) shows only how a runtime `PanelSettings` is created. It draws to the screen with no targetTexture, no theme and no text, so it proves neither item below.
  - **UNVERIFIED; prove both in a clone first:**
    - runtime text without a theme (it needs a font asset or `textSettings`);
    - whether a targetTexture panel repaints only when something changes.
  - **Fallback for every tier:** a char-grid shader on the same 40 × 15 grid (RES finding 6).
- **Renders happen on demand only:**
  - on entry to ACTIVE (FRONT);
  - at Open (the remembered page);
  - on each page turn;
  - on exit (back to FRONT).
- **Nothing on screen animates in v1:** no cursor and no transitions. So idle frames render nothing except the header clock, which re-renders once a minute (no seconds, no blinking colon). Phase B's timed rules (`CancelAfter`, the 0.34 s rate limit) run on `FrontRoomsInspect.Dt` (EX §3.5).
- **Mips.** The RT has mipmaps, regenerated after each render. At glance distance the 960-line RT shows at 43–132 px, so without mips it would shimmer.
- **No UI Toolkit transitions.** Their time base at timeScale 0 is UNVERIFIED (RES :1058).

### 9.2 Budgets (every RT counted)

| Item | Desktop (Mac, Windows) | iOS, Android | WebGL | Why |
|---|---|---|---|---|
| Terminal RTs | at most 2, at 1280 × 960 | **1**, at 640 × 480 | **1**, at 640 × 480 | Agreed: one live RT per active screen, and one RT at half size on handheld and WebGL. Two terminals are within r_A only where two host chunks meet |
| Ad video RT | 1 shared, 640 × 434 (today) | Per D14. If ads stay on desk monitors, they show a static frame or flipbook at all times (A1). The terminal RT is then the only RT that updates at run time | same; SCREENS §5 already plans flipbooks only on WebGL | The one-live-RT rule counts every RT that updates at run time |
| Memory per terminal RT | 6.25 MiB with mips | 1.56 MiB | 1.56 MiB | 1280 × 960 × 4 B × 4/3; 640 × 480 × 4 B × 4/3 |
| Beyond r_A or over the cap | the shared TELL texture | same | same | No headline reads beyond 2.9 m |
| Render time | ≤ 1.0 ms (ESTIMATE) | ≤ 2.0 ms (ESTIMATE) | ≤ 2.0 ms (ESTIMATE) | Only on activation, open, page turn or exit |
| World draws | +0 | +0 | +0 | The terminal quad replaces the ad quad on the host. WebGL is limited by draw count, at about 3,200 draws (RES :935) |
| GC | 0 B per frame | same | same | EX §8.1 |
| Gates | — | `#if UNITY_IOS \|\| UNITY_ANDROID` | `#if (UNITY_WEBGL && !UNITY_EDITOR) \|\| FRONTROOMS_WEBGL_PREVIEW` | Desktop stays byte-identical (Red's rule) |

### 9.3 Tube pass

- **A reading preset** applies while a screen is framed. It caps scanline, mask, glass reflection and curvature strength.
  - **Pass mark:** glyph-to-field contrast **≥ 4.5:1** after the tube, bloom, grain and glass. It is measured with EXAMINE's p90/p5 linear method (EX §3.4), in HDR and SDR, at every Screen effects level (D9).
  - **Why:** Signalis's 2022 demo preview criticised fuzzy low-resolution text (RES critic; a preview, so weak evidence).
- **At glance range** the tube runs at the player's Screen effects level. AT-T02 checks glance contrast. If it fails, a light reading preset also applies within r_A.
- **Corner mask.** The tube's rounded-corner mask hides the line where the flat quad cuts across the bezel's inner corner at 0.23 m (RES finding 12).
- **Two cheap paths:**

| Path | Chosen by | Used on | Why |
|---|---|---|---|
| A target-3.5 SubShader | GPU capability: Unity picks the first SubShader the GPU supports | WebGL2, Android GLES 3.0 | The promo tube shader is target 4.5 (RES finding 8) |
| The `_TUBE_CHEAP` keyword on the terminal material | platform: set in code behind the iOS/Android gate and behind the WebGL gate (§9.2) | iOS, Android, WebGL | Metal and Vulkan GPUs support target 4.5, so the SubShader choice alone would give phones the desktop pass. The agreed constraint is a cheap tube pass on WebGL and mobile |

- **The cheap pass:** one RT sample with mips, a scanline multiply and the corner mask. No mask resample, no bloom term, no glass reflection. ESTIMATE ≤ 0.3 ms at the phone's resolution; AT-T10 measures it on iPhone and in Chrome.
- The desktop SubShader and the keyword-off path stay byte-identical.

### 9.4 The EXAMINE screen definition

The definition is attached in code, to host monitors only. A sidecar block on `Kit_CRTMonitor` would make every monitor inspectable, and `inspect_data.sh` adds none.

| Field | Value | Why |
|---|---|---|
| kind | `flat` | The CRT face is flat, like a sign. A `panel` kind's ±20° orbit would swing the text around (EX listed L1 as `panel`) |
| verb, prompt | `read`; the object word `SCREEN` (`E · READ SCREEN`, worded by 平面视觉) | EX §3.8 default for flat; prompts are a verb plus an object (EXP §1) |
| **ship** | **true** | `InspectDef.ship` defaults to false, and the registry skips such targets unless `DevTargets` is on (inspect payload `FrontRoomsInspect.cs:193`). On handheld the terminal does not register until B-touch |
| size | 0.276 × 0.207 m | the terminal quad |
| fill | the §2.4 rule | One new field: `capBaseByTexels = true` |
| reach / approachAngle | 1.6 m / 60° | EX defaults. The worst corner of the stand box is about 45° off the face (ESTIMATE) |
| maxYawOff | 25° | Monitors are yawed up to ±9° |
| fovDelta | −15° | EX flat |
| regions / zoom | none | Every page reads at the base framing |
| aim box | 0.276 × 0.207 × 0.03 m, centred 5 mm in front of the quad (local z 0.199), yawed with the monitor | Its front at +0.214 m beats the collider face at +0.205 m within `SurfaceSlack` 0.02 |
| texelsPerMetre (code values) | **4,638** on desktop; **2,319** on handheld and WebGL, chosen by the same platform gate as the RT size | 960 or 480 lines over 0.207 m. The def is built in code, not from a sidecar |
| **adapt** (new field) | **false** | The screen is emissive. EXAMINE's dark-room lift of up to +2.25 EV (EXP §1 V1-3) would bloom the text |
| transcript | the current page's strings, from `IInspectPages.PageTranscript` | EX C24, T6 |
| sound | `Plastic` | beige case (EX `InspectSound`) |

---

## 10. Accessibility

| Need | Rule | Owner |
|---|---|---|
| Can't read the screen | **Text transcripts** show the page strings in the right 36 % of the view, with 2 size steps. The face reframes into the free space. Desktop: the EX C24 setting and the transcript panel in the HUD (L5). Touch: the TEXT chip (T6) and the panel rect (T7) | 关卡设计 (setting, desktop panel), 平面视觉 (style, G9), touch (chip, rect) |
| The CRT look blurs or bothers | **D9:** a new row, `Screen effects: Full / Reduced / Off`. Off removes curvature, scanlines, mask, bloom, glare and roll on every screen. The reading preset always applies in close-ups. Default: Full on desktop, Reduced on handheld and WebGL | 游戏视觉 (shader), 关卡设计 (settings row) |
| Flashing | One field colour and hard cuts. Nothing on the screen blinks in v1; the crosshair eye blinks once on build, at most once per second, and not at all under Reduce Flashing (`30_crosshair_grammar.md` §5). At most one page turn per 0.34 s, so at most 3 changes per second (RELAY §9.5) | all |
| Motion | Hard cuts only, no scrolling text. **Camera motion** Off makes the camera cut in and out (`FrontRoomsSettings.cs:29`; EX §2.5 M). The game has no "Reduce Motion" setting. On handheld, the OS reduce-motion flag only sets the Camera motion default on first launch (`:61`) | — |
| Colour | No meaning by hue alone | 平面视觉 |
| Touch size | the PAGE hit is Ø88 (the minimum is 44 pt); taps work anywhere on the face | touch |
| Timing and holds | single presses and taps only; the world is paused (D2) | — |
| Reading speed | paused: no time limit | — |
| Hearing | no information is sound-only; UI Foley is decoration | 声音设计 |
| Cognitive load | at most 4 pages, one level, a fixed footer, a remembered page | — |

---

## 11. Sound (声音设计; events only, no new AudioSources)

| Event | Raised by | Proposed sound | Rule |
|---|---|---|---|
| `FrontRoomsInspect.PageTurned(target, page)` | the EXAMINE extension | **a soft key press with no sharp transient**, such as a rubber-dome key bottoming out: at least 80 ms, low attack (ESTIMATE). Never a short, dry click | 2D, on the unscaled side of `snapshot:/Inspect` (EXC `sound_inspect.md` S3). 0 Attention. Never a `Disturb` |
| `Opened`, `Inspecting(bool)` | EXAMINE | EX S1–S3, unchanged | — |

**Why soft:** a short, dry, non-tonal click is the family of the CALLED latch tick and the Relay's OPEN latch click. 声音设计 adds the page sound to RELAY §14's A/B listening test, beside the CALLED tick.

**Banned for terminals:**
- **Every Relay cue family on the RELAY §9.4 cue sheet:** ticks, latch clicks, low hums and tones. That means the CALLED dry latch tick, the Relay's OPEN latch click, the stage-2 coil hum, and the only chime.
- **A CRT redraw tick** and **hard-disk seek chatter.** Both are in the tick family.
- **A flyback whine.** It would be a positional tell, and it is wrong for the era: VGA scans at 31.5 kHz, above human hearing.
- **A PC-speaker beep,** because it is tonal.
- **Any positional hum from the terminal.** The Office ambience bed may keep a general, non-positional hum that marks no object.

声音设计 adds the AUDIO_CONTRACT rows and audits the sound families (AT-T17).

---

## 12. Teaching and discovery

1. **World tell:** a lit, still field in a colour no other screen uses, at a desk with no chair. If AT-T03 shows that fewer than 8 of 10 players find it, add a sticky-note prop on the bezel (new 游戏视觉 kit work, ESTIMATE 0.5 day), or P11.
2. **Prompt:** `E · READ SCREEN`. On touch, the READ glyph.
3. **Glance:** the headline says what the screen is about. In phase B the index lists what is inside.
4. **Hint (a 系统设计 rule; counted in V1):**
   - A new EX hint row, `A D · PAGE`, shows on desktop for the first 2 opens per session.
   - On targets with pages it is queued **before** `E · BACK`. Paging is the new verb here, and every close-up and the pause card already teach BACK.
   - Each hint counts separately in EXAMINE's `hintCounts` (inspect payload `FrontRoomsInspect.cs` `QueueHints`).
   - Each shows for 3.6 s (0.3 + 3 + 0.3, `FrontRoomsInspectCatalog`).
   - Touch needs no hint: the PAGE glyph explains itself.
5. **Pause card:** EXAMINE's `E  READ  (PAUSES)` line already covers it (EX §5.1).
6. No popups and no sound tell. The only HUD sign is the crosshair's Read state, the look-eye E·1 (Red, 10-15), shown exactly when `E · READ SCREEN` shows (`30_crosshair_grammar.md`).

---

## 13. Later layers this design keeps possible

| # | Layer | What it needs, and its rules |
|---|---|---|
| E1 | Cursor and selection (D11 option b) | A virtual cursor in panel space, driven by Look deltas. UV from the face plane for taps. Digit jumps. This is 游戏视觉's agreed cursor layer, plus new `FrameSnapshot` fields from the touch chat. Any blinking cursor is drawn in the screen shader, never into the RT |
| E2 | FLOOR PLAN page | **It must be seed-pure.**<br>Reads only: zones, heights and border edges by kind (pure functions of the seed, MG:125); `zonesVisited`; the stored tier.<br>Never reads: `brokenDoors` (`MapWorld.cs:443`), passage state, lamp modes, the hunter or the director.<br>Never shows: foil or devices, Relay entries, dead ends, interior walls, trigger rooms, sweep targets, sanctuaries or dead circuits; keys until D1.5; a route until Wayfinding and the exit definition exist.<br>Under D6 (b) it also needs a building-services station (E7).<br>Test: the plan bitmap hash is identical for any Relay position, state, director phase and door state. Only `zonesVisited` and the stored tier change it. The data provider builds no chunk |
| E3 | ALARM HISTORY page | It needs the director's `Called` (RELAY step 2).<br>**No outcome words** (VERIFIED, RESET, CLEARED) and **no filter that shows only closed encounters.** A line that appears when an encounter ends is a delayed all-clear. For a device call the player never felt, it would also reveal that the Relay came and left, and that RELAX started.<br>A line is written only for what the player perceived at that moment: a device heard, a CALLED tick heard, a warning stage felt.<br>No time, `Estimate`, `Queued` or `AttentionAtCall`.<br>The page fills when the close-up opens and never updates during it.<br>Test: the log hash is unchanged across Withdraw, Away and RESTORE |
| E4 | Per-terminal gameplay state | A map registry (the `WindowBuilt` pattern, `MapWorld.cs:168`), once anything must persist. The map raises `TerminalBuilt`; nothing else does |
| E5 | Live reading | the §5 alternative |
| E6 | Warning language on terminals | the §6.3 rules |
| E7 | Building-services station | its own kit, for plan pages under D6 (b) |
| E8 | Codes and passwords | a soft-key word list, never free text. Free text collides with the game's key bindings, and a phone keyboard would cover the CRT |
| E9 | Char-grid back end | if UI Toolkit costs too much on a tier |
| E10 | A tilt-swivel stand | a separate stand part with the shell pivoting about the screen centre. It adds about 2–3′ to the phone glance (ESTIMATE). It needs 游戏视觉 kit work and a re-run of §2.3–§2.4 |

---

## 14. Rejected ideas

| Idea | Why not |
|---|---|
| DRILL (a terminal that sounds a horn after a delay) | It is a delayed remote lure. It tells the player where the Relay will go without them perceiving it. It lets them schedule encounters and farm RELAX, because devices call even in RELAX (RELAY §6). Its later CALLED tick would also announce that the Relay went Away. If Red wants it, it becomes a RELAY §6 device with its own ruling and bot check |
| Live reading by default | Muted phones miss stage 2, and slow readers read against a clock (§5) |
| A building life-safety PC at a cubicle desk | It contradicts EGRESS: Office zones are tenant fit-out, off the base plan (NP:79, :211; RES critic :1187) |
| An amber terminal field | Amber is the sanctuary's TROUBLE lamp (RELAY §10) |
| A CCTV feed or a live alarm map | It works as radar (RELAY §3.1) |
| Screens that follow generic lamp dips, `LampLevel` or `LampBaseLevel` | They show cue vocabulary at stage 0, or change during warnings (§6.3) |
| A per-screen Relay exclusion | It becomes a proximity detector in a close-up (§6.3) |
| Tilting the one-mesh monitor 10° about its base | Its back goes through the spine panel, and the screen centre rises 3 cm (§2.2) |
| A blinking cursor in v1 | It invites typing that does nothing, and it is the only per-frame change (§8.1) |
| Freezing ambient ads when a terminal is near | A visible pop, and a tell that a terminal is close (§3.5) |
| Taking the move zone for opening taps | A player walking past would lose the stick (§7.3) |
| F-key menus, Tab, Alt menus | They clash with browsers, Mac and Windows (§7.1) |
| PC-speaker beeps, flyback whine, HDD chatter, a positional hum | Relay cue families, or wrong for the era (§11) |
| Every CRT interactive | 88 % of stations have one; constant reading breaks rarity (EX P11) |
| Login and password puzzles | Notes cannot be placed reliably in a procedural map (RES content) |

---

## 15. Ownership, sizing and build order

### 15.1 Work items

| ID | Owner | Item | Phase |
|---|---|---|---|
| V0 | 游戏视觉 | F7 for ambient monitors, through the glass-look package (dry run first; `rebase_apply.sh` if the bases moved; VL226 PASS) | 0 |
| V1 | 游戏视觉 | EXAMINE extension: `IInspectPages`; `FrontRoomsInspect.Page(int)` with the 0.34 s rate limit and one held press; `CurrentPageCount`; `CurrentFaceViewportRect`; `PageTurned`; `InspectDef.adapt`; `capBaseByTexels`; the PAGE hint row, queued before BACK on paged targets and counted in `hintCounts`; the double-tap-off flag for paged targets; the screen definition (§9.4, `ship = true`) | B |
| V2 | 游戏视觉 (if D12) | Terminal UI runtime, `Rendering/Screens/FrontRoomsTerminalScreen.cs`: the run's deal by content key, page memory, FRONT on exit, transcript export | A (FRONT) / B |
| V3 | 游戏视觉 | Renderer: a clone proof of UI Toolkit text; the terminal's own 0.276 × 0.207 quad (no U flip), added to every LOD of the monitor's `LODGroup`; r_A from the `LODGroup`; RT pool with mips; TELL texture; on-demand renders; platform gates | A |
| V4 | 游戏视觉 | Tube: reading preset, Screen effects levels, target-3.5 SubShader, the `_TUBE_CHEAP` platform path | B (A if the glance contrast check needs a preset) |
| V5 | 游戏视觉 | `Office/FrontRoomsOfficeKit.cs` only: the `BeginDress` overload, layout pre-pass, Setup A, the forced stream including the 12 % case, the +0.03 m shift, stand box, chair drops, miss log | A |
| V6 | 游戏视觉 | Clone captures and VERIFICATION LOG entries (AT-T02, T05, T06, T10, T19) | A, B |
| L1 | 关卡设计 | C26, as agreed. **Part 1 (phase 0):** remove the private ray and E read, or move the toggle onto the shared aim path if D4 keeps it; pause VideoPlayers on `Paused` (`Game.cs:45`). **Part 2 (phase B, after EXAMINE step 4):** the same on `Inspecting`, which arrives with `map_inspect_3_game.diff`. The edits inside `Media/FrontRoomsScreenVideo.cs` go to the owner Red names in EXR D11 | 0, B |
| L2 | 关卡设计 | `FrontRoomsMapWorld`: host chunk and host room (P2), content key, the P7 exclusions that exist in main, the reflected `BeginDress` overload call, miss log, a 100-seed report including misses | A |
| L3 | 关卡设计 | `FrontRooms3DGame.cs`, in the EX C7 Inspect branch: `FrontRoomsInspect.Page(input.PageStep)`; scroll pages after EX T3; C25b; the WebGL pointer-lock poll → Paused from Inspect | B |
| L4 | 关卡设计 | `ThreatPerceivable` with its holds; `RelayOnScreen`, `RelayLit`, `StepsAudible` (v2), `BlowHeard` and `BlowAudibleMetres`; the fixed `PlayerSeesRelay` and the new `PlayerPerceivesRelay` (§6.6); replacing C4's `RelayBlocksInspect` if D2 is answered as recommended; exposing the generated lamp temperament (§6.3); tests | B |
| L5 | 关卡设计 | Settings: `Screen effects` (D9); EX C24 `Text transcripts` and the desktop transcript panel in the HUD (EX §5.2, style G9); `Close-ups pause` only if live is chosen | B |
| L6 | 关卡设计 | Tests AT-T01, T04, T07, T08, T15, T16, T17, T21; terminals met per run in the bot baseline | A, B |
| TT0 | touch chat | `FrameSnapshot.PageStep` (constructor parameter last, default 0); the desktop reader's per-key press edges | B |
| TT1 | touch chat | PAGE chip; face-rect tap routing from `CurrentFaceViewportRect`; double-tap off on paged targets; buttons placed from the safe area; Android Back check; AT-T14 | B-touch |
| T1, T2, T6, T7 | touch chat (EXAMINE's touch contract) | `MenuState.Inspect`; USE shown as BACK; the TEXT chip; the panel rect | B-touch (prerequisites) |
| G0 | 平面视觉 | One mock frame of FRONT at the back of the stand box and at 2.4 m, on desktop and on a phone, for D1 and D11 | before Red answers |
| G1 | 平面视觉 | Shell visual system in Figma: field colour (§8.7), face and its glyph coverage, header, FRONT/TEXT/LIST, footer with drawn arrows, TELL frame | A (FRONT, TELL) / B |
| G2 | 平面视觉 | Envelope check from layout constants (§8.3) | A |
| G3 | 平面视觉 | Ambient-frame rule (§3.5), the SCREENS §3 amendment (§6.3), the era note on WXRM (D14) | A |
| G4 | 平面视觉 | HUD words (`A D · PAGE`, `E · READ SCREEN`), PAGE glyph, tube preset approval | B |
| N1–N5 | narrative | Program name and header strings; headlines and second lines (N2); memos (the v1 pool without door advice; door memos held for RELAY step 4); the tenant print job; an audit against §6.4 and §8.6, including the ALARM LOG frame, the `SYSTEM IDLE` era check and the receptacle-circuit fiction | A (N2) / B |
| S-T1 | 声音设计 | `PageTurned` Foley, AUDIO_CONTRACT rows, sound-family audit, the A/B listening test | B |
| S-T2 | 声音设计 | Read `BlowAudibleMetres` for the blow caption radius, and check the FMOD event max against it (`fmod_check contract`). Read `StepsAudible` for the step gain once v2 lands | B |
| A1 | owner per EXR D11 (conditional) | Only if D14 keeps desk-monitor ads: a static frame or flipbook at all times on handheld and WebGL | with D14 |
| R1 | 系统设计 | Spec upkeep; the §6.6 text into RELAY §8; test review; Red Q&A | all |

### 15.2 Signatures (sketch; owners refine)

```csharp
// 游戏视觉 · Rendering/Inspect (EXAMINE extension, V1)
public interface IInspectPages { int PageCount { get; } int Page { get; } void Step(int delta); string PageTranscript { get; } }
public static partial class FrontRoomsInspect {
    public static int CurrentPageCount { get; }          // 0 if the current target has no pages
    public static void Page(int delta);                  // Phase.Inspect, Age >= CancelAfter, >= 0.34 s since the last turn (one press held); wraps
    public static Rect CurrentFaceViewportRect { get; }  // the framed face in viewport units (origin bottom-left); empty when closed
    public static event Action<FrontRoomsInspectable, int> PageTurned;
}
// InspectDef: public bool adapt = true;            // false for screens
//             public bool capBaseByTexels = false; // true for screens: the base fill obeys the 2.0x texel cap

// 游戏视觉 · Office/FrontRoomsOfficeKit.cs (V5). The map binds it by reflection, like BeginDress today.
public static FrontRoomsDressJob BeginDress(Transform parent, Rect floorXZ, float ceilingHeight, int seed,
    Rect[] keepClear, Rect[] obstacles, Vector2 terminalSite, int terminalKey);

// 游戏视觉 · Rendering/Screens (V2/V3; runtime owner per D12)
public sealed class FrontRoomsTerminalScreen : MonoBehaviour, IInspectPages {
    public static FrontRoomsTerminalScreen Attach(GameObject monitor, int contentKey);
}

// touch chat · Input/FrontRoomsInput.cs (TT0)
// FrameSnapshot: public readonly int PageStep;   // net page steps this frame; constructor parameter last, default 0

// 关卡设计 · FrontRoomsMapWorld (L2): a private host-room rule and the reflected overload
// static MethodInfo officeBeginTerminal;   // types: Transform, Rect, float, int, Rect[], Rect[], Vector2, int

// 关卡设计 · FrontRoomsMapHunter (L4)
public bool ThreatPerceivable { get; }       // §6.1, with its holds
public bool RelayOnScreen { get; }           // per device; UI gates only
public bool PlayerPerceivesRelay { get; }    // director; platform-identical
public bool StepsAudible { get; }            // v2; RELAY §9.3 gain > 0; the sound reads it too
public const float BlowAudibleMetres = 36f;  // v1; the blow caption radius and the FMOD event max read it
```

### 15.3 Size in working days (ESTIMATE, SP; RELAY §13 sizes its work the same way)

| Owner | Items | Days |
|---|---|---|
| 游戏视觉 | V0 0.5 · V1 2 · V2 1.5 · V3 3.5 · V4 3 · V5 2 · V6 2 | **14.5** |
| 关卡设计 | L1 0.25 · L2 2 · L3 1.5 · L4 2.5 · L5 1.5 · L6 2 | **9.75** |
| touch chat | TT0 0.5 · TT1 2 · EX T1 1 · T2 0.5 · T6 0.5 · T7 0.25 | **4.75** |
| 平面视觉 | G0 0.5 · G1 2.5 · G2 0.5 · G3 0.5 · G4 0.5 | **4.5** |
| narrative | N1–N5 | **2** |
| 声音设计 | S-T1 1 · S-T2 0.5 | **1.5** |
| 系统设计 | R1 | **1.5** |
| Red | §17 and D0 | **0.5** |
| **Total** | | **≈ 39** |

- EXAMINE's touch items T1, T2, T6 and T7 are 2.25 of these days. EXAMINE needs them anyway; they are counted here because terminals cannot ship on touch without them.
- **Phase A alone** is about 12 days: L2 2, V5 2, V3 3.5, V2 0.5, G0 0.5, G1 1, G2 0.5, G3 0.5, N2 0.5, V6 0.75, L6 0.25.
- **Options:**
  - D11 option (b), the menu program: **+6 days** (cursor layer 2.5, input fields 1, game routing 1, layouts 1, tests 0.5).
  - Live mode: **+3.5 days** (forced exit and framing 1.5, framing variant 0.5, A/B listening 0.5, tests 1).
  - L1 grows from 0.25 to 1 day if D4 keeps the toggle.
  - A1 adds 0.5 day if D14 keeps desk-monitor ads.

### 15.4 Build order

| Phase | Lands | Waits on |
|---|---|---|
| **0 · now, separate from this feature** | **D0:** Red approves the Ad_02 swap or the kill switch. V0 (F7). L1 part 1 (C26 on `Paused`; the private E removed or moved, per D4) | Red, for D0. EXR D11, for who edits the `Media/` file |
| **A · SEE** | L2, then V5. Either alone changes nothing: the map binds the overload by reflection, so terminals appear only when both are in.<br>Then V3, V2 (FRONT with the header, headline and second line; no index, no footer), G0–G3, and N2 once narrative signs it off.<br>No placeholder strings are merged; placeholders live only in the clone.<br>Tests: AT-T01, T02, T10 (including the LOD check at Medium, High and Ultra), T11, T16, T19 | D0 only. **Nothing from EXAMINE.** V5 touches only `FrontRoomsOfficeKit.cs`. `inspect.sh` replaces `DressJob.cs`, `KitLibrary.cs` and `FurniturePile.cs` against sha1 bases on 0a9ee7f, so its bases do not move. If any later change touches one of those three files, run `W/tools/rebase_apply.sh inspect` before EXAMINE step 3 |
| **B · READ** (desktop, WebGL) | First, all five EXAMINE steps in their own order: rig → `inspect_data` → `inspect.sh` → `map_inspect_3_game` → `sound_inspect`.<br>Then `W/apply/terminal.sh` (V1, V2, V4, G4, N1, N3–N5) with the contract diffs L1 part 2, L3, L4, L5 and TT0.<br>Then S-T1 and S-T2.<br>All remaining tests | EXAMINE steps 1–5; D2, D11, D12. **Why after step 5:** EXAMINE's sound contract (S1, S2, the audio pause that fixed EXAMINE's verifier issue V1-1) does not depend on terminals, so it must not wait on terminal decisions |
| **B-touch** | READ on iOS and Android: TT1 | EX T1, T2, T6, T7; phase B |
| **C · per Red** | the D11 (b) cursor; live mode (E5); D8 warnings; the E2 and E3 pages once their systems exist | those decisions and systems |

**How each package lands:**
- Build in a clone (`mkclone.sh`). Phase B builds on top of the EXAMINE clone.
- The visual package is `W/apply/terminal.sh`: a dry run first, `rebase_apply.sh` on a base mismatch, a Roslyn check, then merge at once.
- Contract diffs (L1–L5, TT0, TT1, S-T1, S-T2) are applied in the clone only. Each owner merges their own.
- Verification images go to the FRONTROOMS · VISUAL VERIFICATION LOG (`VERIFICATION_LOG.md`).

---

## 16. Acceptance tests

| # | Test | Pass |
|---|---|---|
| AT-T01 | Placement and determinism, 100 seeds | **Coverage:** ≥ 90 % of Office zones get a terminal.<br>**Never two:** never more than one per zone, including after shifting each chunk of a zone (host and neighbours) once, in every order.<br>**Host chunk:** only the chunk that holds the zone's key cell hosts. Misses are logged.<br>**Exclusions:** none in the start area, the title stream or a pile. Trigger rooms, their 1.5 m band and sanctuaries are added once v2 map data lands.<br>**Mount:** Setup A on every terminal: on the PC case; screen centre 1.128 ± 0.01 m; face upright (0 ± 1°); monitor back ≥ 1 cm clear of the spine panel; monitor, PC, keyboard and mouse at +0.03 m. The stand box is collider-free, and the host has no chair.<br>**Keys:** the same seed gives the same host room and key. The key is unchanged after a shift.<br>**Determinism:** every non-host station's dressing signature is byte-identical to today, except chairs left out of a stand box. The real pass reproduces the pre-pass layout. Host stations that took the 12 % roll get the forced-stream items. The 20 validator-seed map signatures are unchanged |
| AT-T02 | Glance legibility and contrast. Captures with the body 0.30 m and 0.60 m from the desk edge, and with the eye at 1.6, 2.4 and 3.0 m. At 1080p, 2160p, iPhone 2622 × 1206, iPad 2360 × 1640 and WebGL 1080p. 3 seeds. 3 terminals never read, and 3 after a read (open, turn a page, exit) | **Size:** within ±10 % of the §2.3 table. Desktop: the headline is ≥ 12 px at 2.4 m, and the second line ≥ 12 px at 1.6 m. Phone: the headline is ≥ 18.6′ with the body 0.60 m from the desk. iPad: the second line is ≥ 18.6′ there too. The phone headline's last legible distance is reported as a measured number (predicted 1.25 m).<br>**Contrast:** the headline's p90/p5 linear ≥ 4.5:1 at 0.74, 0.99 and 2.4 m, in a lit room and a dark room, with the glass system on, at every Screen effects level.<br>**After a read:** the glance shows FRONT |
| AT-T03 | Tell (playtest, 10 players) | ≥ 8 pick out the terminal from 3 m |
| AT-T04 | Prompt | `E · READ SCREEN` shows within 1.6 m and 60° anywhere in the stand box, and is gone at 1.7 m or 65°.<br>The terminal registers in a player build (`ship = true`).<br>**0 prompts on ambient monitors:** a bot aims at every monitor over 100 seeds.<br>No prompt on handheld before B-touch |
| AT-T05 | Framing at 16:9, 16:10, 21:9, 4:3, iPad 1.44, and phones at 2.17 and 1.78 | Fill as in §2.4, ±3 %. 0 px clipped. With transcripts on, the face is inside `FreeScreenRect`. The camera path is clear for every terminal in 100 seeds (P6). Stand-off ≥ 0.15 m. Pose 1.128 ± 0.01 m, pitch 0 ± 1°. On handheld the face never sits under the PAGE or BACK hit circles |
| AT-T06 | Body legibility, contrast and shape | The smallest cap is ≥ 18.6′ on every class (±10 %). No zoom is offered. Magnification ≤ 2.0×. Contrast ≥ 4.5:1 at every Screen effects level, in HDR and SDR. Glyph aspect 1.00 ± 0.02 (no stretch) |
| AT-T07 | Paging and exits | **Keys:** each press of D, →, ↓, Space or Enter is +1, and each press of A, ← or ↑ is −1. A held key is one step. Chords: with A held, pressing → is +1; pressing A and D in the same frame is net 0; releasing either turns nothing.<br>**Held at open:** a page key held when the close-up opens turns no page until it is released and pressed again.<br>**Rate:** turns are ≥ 0.34 s apart. 4 presses in 1 s give 4 turns by 1.1 s. 10 presses per second give ≤ 3 turns per second.<br>**Rules:** paging wraps both ways. Nothing happens before 0.20 s. READ reopens on the remembered page; restart resets it. After EX T3, one scroll step is one page.<br>**Exits:** every exit in §7.2 returns control within 1 frame. Held A, D or arrow keys at exit do not move the player.<br>**WebGL:** browser Esc gives Paused with timeScale 0 and `pausedFrom = Inspect`. Alt-tab gives the same state, in either event order. Resume returns to the same close-up and page with the pointer locked. The re-lock click turns no page. With Esc blocked, E and S still exit |
| AT-T08 | Pause neutrality with paging (the EX AT-01g method) | A 10 s close-up with 20 page turns and a 0.2 s close-up give equal gameplay hashes at t = 60 s |
| AT-T09 | Timing at timeScale 0 | Renders happen at timeScale 0 and under `captureDeltaTime` 1/60. The USS has no `transition-*`. No RT render happens without an event (activation, open, turn, exit) |
| AT-T10 | Budgets (Profiler and Frame Debugger, on Mac, iPhone and Chrome WebGL) | **RTs:** at most 2 terminal RTs on desktop and 1 on handheld and WebGL. On handheld and WebGL the terminal RT is the only RT that updates at run time.<br>**Cost:** 0 UI renders on idle frames; render time within §9.2; the cheap tube path within its ESTIMATE on iPhone and in Chrome; world draw count unchanged; 0 B GC.<br>**LOD:** the terminal quad culls with its monitor at Medium (7.8 m), High (11.18 m) and Ultra (22.4 m). r_A is 7.0, 8 and 8 m.<br>**Desktop:** the capture is byte-identical before and after the platform gates, the new SubShader and the keyword path |
| AT-T11 | Prerequisites | No `Keyboard.current` read in `Media/FrontRoomsScreenVideo.cs` (grep). The Screen Ads demo rig's own reads (`FrontRoomsScreenAdsDemoRig.cs:31-32`) are demo code and stay; the demo loses its E power toggle with C26, which is noted to its owner. The VL115 re-run passes in main. No build contains the Ad_01 clip (hash from VL224) |
| AT-T12 | Transcript | The transcript equals the page strings. The desktop `Text transcripts` setting shows the panel. On touch (B-touch) the TEXT chip toggles it |
| AT-T13 | Strings | Narrative signs off.<br>A grep finds none of: "relay"; a year after 1990; a zone or desk number; an all-clear word; TROUBLE; EXIT or BACK as a leave word; "walking is silent".<br>A v1 build's memo pool has no door advice.<br>The header clock matches the machine's local time to the minute, in `h:MM AM` form, throughout a 10-minute run; any date reads year 1990 with the 1990 weekday |
| AT-T14 | Touch layout (874 × 402, 667 × 375 and iPad 1180 × 820, right- and left-handed) | The PAGE, BACK, TEXT and transcript rects do not overlap, and none lands off-screen.<br>An 8 pt tap grid over the face reaches paging in both halves and inside the top band.<br>A drag on the face never exits; a pull-back from outside the face does.<br>The opening press never turns a page.<br>4 taps in 1 s turn 4 pages: the double-tap detector is off.<br>USE opens the close-up from anywhere in the stand box |
| AT-T15 | The gate (§6.1) | **Prompt hidden:** v1 Chase; BreakDoor with a blow at ≤ 36 m; the Relay in frame, lit and visible; v2 stage 3; v2 Spotting ≥ 0.5 with the steps audible.<br>**Prompt shown:** a Relay 30 m ahead in the dark; a Relay 8 m ahead and in frame in a dead-area cell; a Relay at head height 10 m ahead while the player aims 40° down at the terminal (above the frame); a Relay 55–60° off-axis on 16:9; a Relay 50° off-axis on iPad 1.44; BreakDoor with blows at 40 m in a straight line; a silent `SeesPlayer`; v2 Spotting ≥ 0.5 with the steps inaudible and out of frame; stages 1–2 (allowed, then frozen).<br>**Holds:** after a chase ends, the prompt returns no sooner than 3 s. After the last audible blow it returns no sooner than `BlowGap`, unless the break is heard. After a Spotting hide it returns only when `StepsAudible` falls or the steps resume.<br>**Every return is perceivable:** in a randomised 30-minute run, every return of the prompt falls within 0.1 s of a logged change the player can perceive (chase bed end, break sound, steps stop or start, the Relay leaving the frame).<br>**Every BreakDoor hide is audible:** each one coincides with an audible, captioned blow |
| AT-T16 | Honesty and invariance (§6.5) | The glance texture hash (FRONT) and every page's strings are identical across Relay position, state and stage 0–2, director phase, any door state, Withdraw, Away, RESTORE, a tier change after generation, and any number of reads.<br>A grep shows the content provider reads no hunter, director, door, lamp or current-tier state |
| AT-T17 | Sound and Attention | No terminal sound event falls in the tick, latch-click, hum or tonal families (audio trace). Terminal events never reach the hunter's hearing.<br>**Only if D2 = live:** 30 s of paging and 30 s idle give identical director and hunter traces (live harness) |
| AT-T18 | Photosafety | A 60 Hz luminance probe over the screen region, while paging at 10 presses per second, shows ≤ 3 flashes in any 1 s (the RELAY §9.5 definition). Nothing on the screen changes without input |
| AT-T19 | Ambient frames | No ambient screen uses the terminal's field colour or header layout. Every ambient text meets glance size, or there is no text. On handheld and WebGL, ambient sets never change state when a terminal comes within r_A |
| AT-T20 | Live mode (only if chosen) | The forced exit starts on the frame `ThreatPerceivable` turns true, including `TargetAcquired`, which fires while the close-up is open. The camera is home within 0.15 s, and no catch is possible before control returns. Stage-2 steps are ≥ 3 dB above room tone. Captions and the marker are visible. The A/B listening test passes. Live neutrality holds (AT-T17) |
| AT-T21 | The memo deal | Over 10,000 simulated runs for each terminal count T = 1–6: no set repeats while T ≤ the number of sets. A revisit shows the same set. Restart deals again. The bot baseline reports terminals met per run (median and p90) before narrative sizes the pool |

---

## 17. DECIDE list for Red

**Red's answers, 2026-10-14:**
- **D1 = glance in place plus `E · READ SCREEN`** (recommended option). Red adds: think through the interaction's motion design with 平面视觉. For example, when the crosshair moves onto a screen, it turns into an eye that blinks, meaning "there is something to look at". This becomes the crosshair state grammar (系统设计 rules, 平面视觉 motion); see `30_crosshair_grammar.md` once it is written.
- **D2 = paused** (recommended option). This also answers EXAMINE's D1 for every close-up, with the `ThreatPerceivable` gate (§6.1).
- **D11 = decide after 平面视觉's G0 mock frame** (queued as GRAPHIC_VISUAL_TASKS §3h).
- **D0 = swap to Ad_02** (Red, 10-14, shown the Ad_01 and Ad_02 file sheets): "保留下面的这些，上面那些只作为历史材料". Keep Ad_02 (Front Rooms Furniture / WXRM-TV 61); keep Ad_01 (LEVITZ) only as historical material, outside the build. **Done 2026-10-14 18:17** (游戏视觉; verified by 系统设计): `Assets/StreamingAssets/FrontRooms_Ad_01.mp4` now holds Ad_02 (sha1 058d2e28…, the same as `Tools/screens/out/FrontRooms_Ad_02`), with the file name and .meta GUID unchanged. Ad_01 is archived at `Documentation/research/codex_audit/history/FrontRooms_Ad_01_LEVITZ.mp4`, with a never-ship README. **Builds made before the swap still carry LEVITZ** (the Mac app, the iOS builds, FRONTROOMSSS-Build11.xcarchive). They need rebuilding, and a TestFlight build that is already uploaded needs a replacement.

**Inherited from EXAMINE's Red list (EXR §5), with their defaults:**
- **EXR D2:** control returns as the camera starts home.
- **EXR D9:** the Esc pause from Playing also stops time.
- **EXR D1** is merged into D2 below, so that Red gives one answer for every close-up.
- **EXR D11** (who edits `Media/FrontRoomsScreenVideo.cs`) also decides who makes C26's file edits and A1 (D4, D14). C26 itself stays with 关卡设计.
- EXR D3 and D4 do not apply: terminals have no zoom and no adapt lift.

| # | Question | Recommendation | Alternatives |
|---|---|---|---|
| D0 | How do we stop LEVITZ shipping? (urgent; separate from this feature; a real trademark must not ship either way, INV, SCREENS P1) | The Ad_02 swap now: `codex_audit_v3_ad02.sh --apply --red-approved` (dry run OK on 10-14) | The kill switch: no ad on desk monitors |
| D1 | What does "walk up and directly see" mean? (with G0's mock frame) | Glance in place (the front page is always legible, no button), plus `E · READ SCREEN`, a paused close-up of every page | Auto close-up after 0.6 s in the stand box (terminals only; the gate applies). Or glance only (2 lines, no close-up) |
| D2 | Close-ups near the Relay, and does the world pause in a close-up? (This is EXR D1, amended; the answer applies to every close-up) | Paused, as every close-up. No prompt while `ThreatPerceivable` (§6.1) is true, for the placard and terminals alike. Warnings 1–2 are allowed and freeze. Until you answer, 游戏视觉 is asked to treat EXR D1's default as interim; L4 replaces C4's gate | Always allowed and paused. Or a `Close-ups pause: Off` setting, only after RELAY step 4, with the §5 rules (+3.5 days). Live by default is not recommended |
| D3 | Which computers can be read? | One terminal per Office zone, in Setup A, in the zone's key chunk, at the station nearest the zone site | One per Office room. One per two Office zones. Every powered monitor |
| D4 | What happens to the hidden E power toggle? (The `Media/` edit goes to whoever you name in EXR D11) | Remove it. Ambient monitors have no verb | A prompted toggle on the shared aim path, on every monitor (0 Attention; L1 grows to 1 day). Or keep it on terminals only |
| D5 | What is on a terminal in v1? | Static pages: FRONT plus 2 pages (facilities memos, a tenant print job), dealt with no repeats within a run | Add FLOOR PLAN now (needs Wayfinding, the exit definition, one numbering and E7). Or add ALARM HISTORY now (needs the director) |
| D6 | What is the machine in the fiction? | (a) A tenant office PC. The plan stays on the placard | (b) A building-services station with its own kit (E7) that can show the plan later. (c) A life-safety PC at a cubicle desk (contradicts EGRESS; not recommended) |
| D8 | Do terminals react to the Relay's warnings? | Never in v1 | If you approve SCREENS §4 for screens: a reaction keyed only on `WarnBurst`, with no per-screen exclusion, plus the chase slate |
| D9 | Add a screen-effects setting? | Yes: Full / Reduced / Off, with the reading preset always on in close-ups | The reading preset only. Or Full / Reduced only |
| D10 | May the game ship a CC BY-SA 4.0 font (Px437 IBM VGA)? (The face itself is 平面视觉's choice) | Not in v1. 平面视觉 uses an OFL face or draws one. Revisit after a licence review | Yes, with attribution and the share-alike terms reviewed |
| D11 | Is a pager enough for "its own UI system"? (with G0's mock frame) | Yes for v1: a printed, unnumbered index on the front page, plus paging. The cursor layer stays possible (E1) | A menu program now, with a virtual cursor, selection and digit jumps (+6 days, new input fields). Or a pager with no index |
| D12 | Who owns the terminal UI runtime (page state, paging, transcript export; later the cursor and picking)? | 游戏视觉, together with the renderer and the agreed cursor layer | 关卡设计. Or the touch chat |
| D14 | What do ambient desk monitors show? | After D0: dark glass or static office frames. The WXRM broadcast moves to TV sets once the screen code reads each kit's anchor and TVs are placed outside piles | SCREENS §5 as written (WXRM on about 1 in 3 desk monitors). Then, on handheld and WebGL, a static frame or flipbook at all times (A1) |

**Closed since v1.1 (no longer Red's):**
- **D7:** inherited from RELAY §10, "The warnings are explained once in the pause-card legend, never live" (§8.6 rule 4).
- **D13:** the fill rule obeys EXAMINE's 2.0× texel cap, which EXAMINE already builds. No exception is needed (§2.4).
- **D15:** page memory is a 系统设计 rule: per run, READ reopens on the remembered page, and the glance always shows FRONT (§4.1).
- **D16:** merged into D2.
- **Old D10, the face choice:** 平面视觉's under the agreed ownership. Only the licence question stays with Red, as D10.

---

## 18. Risks and UNVERIFIED

**Dependencies:**
- **EXAMINE is not merged.** Step 1 of its chain (`map_inspect_1_rig.diff`) has not landed. Phase B builds on the EXAMINE clone until it does.
- **EXAMINE's touch items T1–T7 have not started.** Until they land, handheld terminals are glance-only.
- **Relay v2 terms are not in main:** `Spotting`, `OnMap`, `TargetAcquired`, stage 3 and `StepsAudible`. The define RELAY §13 plans does not exist yet. The v1 form of the gate ships first.
- **`BlowHeard` follows 声音设计's door-blow rule.** If that rule changes, the gate follows it, because both read one value.

**UNVERIFIED:**
- UI Toolkit runtime text without a theme, and whether a targetTexture panel repaints only on change. Both are proven in a clone first; the char-grid shader is the fallback.
- Whether Unity 6's WebGL player updates `Cursor.lockState` when the browser drops the lock. The `.jslib` listener is the fallback.
- The kit fit: the 3 cm shift against the keyboard, and the flat quad 4 mm in front of the sidecar anchor against the curved glass. 游戏视觉 checks both.
- The receptacle-circuit fiction for a 1990 US office (narrative).
- `W_seen` (0.15), set on captures.
- Whether the FMOD blow event's rolloff reaches silence at 36 m (声音设计).

**Risks:**
- **The pre-pass and host overrides** touch station dressing. AT-T01 must show that every other station is unchanged, and that the real pass reproduces the pre-pass.
- **The host room's solve runs twice.** That is one room per Office zone (ESTIMATE: double that room's solve time, still time-sliced).
- **Light threshold.** If `W_seen` is too low, a barely visible Relay can hide the prompt; if too high, a visible one may not. The second error is the safe one (§6.1), so `W_seen` is tuned upward when in doubt.
- **The gap outside close-ups.** The game has no lock-loss rule in Playing. That is today's behaviour, outside this feature, and noted for 关卡设计.
- **The memo pool** is sized before terminals met per run are known. L6 measures first.

**ESTIMATES:**
- the 0.8 width-to-cap ratio;
- render times and the cheap tube cost;
- the days in §15.3;
- the memo count;
- the stand-box angles and the path clearance;
- `BlowGap`;
- the page sound's length.

**Footage still unwatched:** the Alien: Isolation terminal camera, SOMA, and whether Prey or The Bunker pause. Log it to the VERIFICATION LOG before the phase B playtest.

---

## 19. Where the v1.1 must-fix items stand now

| Must-fix item (v1.1 reviews) | Where now |
|---|---|
| The EXAMINE gate leaks (flat frustum, 46 m, no light term; BreakDoor at any range; Spotting without audio) | §6.1 (now with a light term and holds), §6.6, AT-T15, D2 |
| Warnings key on `WarnBurst` only, with no per-screen exclusion | §6.3, D8 |
| No screen follows generic `LampDipped` / `FixtureChanged`; amend SCREENS §3 | §6.3 (now: generated temperament only), G3 |
| Log: no outcome words, no closed-only filter, written at the moment of perception | §13 E3 |
| Plan: seed-pure, never reads `brokenDoors`; hash test | §13 E2 |
| Sound families reserved; redraw tick, whine and HDD chatter dropped | §11 (the page sound is now soft), AT-T17 |
| Paused by default; a paused close-up is neutral; live rules | §5, AT-T20 |
| DRILL cut | §14 |
| String rules: no all-clear, the live clock format (h:MM AM, 1990 dates), no "relay", platform-true teaching, no routine-dip framing | §6.4, §8.6 (now also Relay-version-true), AT-T13 |
| Dead circuits and sanctuaries never shown; placement exclusions | §6.4 item 5, P7 (v2 parts once v2 data lands) |
| Invariance to Withdraw, Away and RESTORE; stored tier only | §6.4 item 11, §6.5, AT-T16 |
| Baseline 0a9ee7f; F6 committed; LEVITZ in HEAD builds | §1, D0, AT-T11 |
| EXAMINE's 5-step package split | §1, §15.4 (terminal package now after step 5) |
| Every live RT counted, including the ad bank | §9.2, A1, AT-T10 |
| iPad magnification above 2.0× | §2.4 fill rule (D13 closed) |
| Target-3.5 path for GLES 3.0, not WebGL-only | §9.3 (plus the `_TUBE_CHEAP` platform path) |
| Days per owner | §15.3 |
| Glance not sequenced behind EXAMINE | §15.4 phase A |
| UI runtime owner as a DECIDE | D12 |
| Same random draws on the host station; signature test | §3.3 (now with the pre-pass and the 12 % case), AT-T01 |
| Persistence matches the map (the desk may move) | §3.4 (now one host chunk) |
| EXAMINE pose for a desk-mounted screen | §2.2 (Setup A, upright), AT-T05 |
| One raster and one render path per tier; mips; UNVERIFIED items named | §8.2, §9.1 |
| Colour exclusions; amber withdrawn | §8.7 |
| WebGL pointer-lock loss | §7.2 (**changed:** it now pauses over the close-up), AT-T07 |
| Ownership slips: captions, one raiser per event, the contract call, line citations | §1, §3.4 (now a reflected overload), §5 |
| Phone glance at the real stand spot | §2.2, §2.3, AT-T02 |
| No F5, F10, F1, Tab or Alt | §7.1 |
| "Reduce Motion" does not exist in the game | §10 |
| No EXIT or BACK as a leave word | §7.2 |
| Touch: whole face rect, both halves, top band; drags never exit | §7.3, AT-T14 |
| Fiction as a DECIDE | D6 (now lettered) |
| "Its own UI system" as a question to Red | D11 (with G0) |
| Page kept across exits within a run | §4.1 (now a 系统设计 rule; the glance stays FRONT) |
| Dates 1990 or earlier | §8.6 |
| Typeface: VT323 is a DEC face | §8.7, D10 (licence only) |
| WXRM on PC monitors is wrong for the era | §3.5, D14 |
| Texel magnification cap on every class | §2.4 |

---

## 20. Review notes (v1.2)

### 20.1 Where each round-2 issue is answered

**Reviewer 1 (feasibility and Relay honesty):**

| Issue | Where |
|---|---|
| The 10° tilt is not buildable, and its pivot is unnamed | §2.2: no tilt, a 3 cm shift, numbers recomputed, UNVERIFIED until 游戏视觉 checks; §13 E10; AT-T01, T02, T05 |
| The dresser cannot pick the host while dressing; the 12 % case is undefined | §3.3 pre-pass and the forced-stream table; V5 limited to `FrontRoomsOfficeKit.cs` |
| The contract-shape analogy was wrong (the static class has no map reference) | §3.4: the map computes the host and calls a reflected overload; §15.2; §15.4 orders L2 then V5 |
| Zones cross chunks; revisit shifts could give two terminals or none | P2: one host chunk per zone; AT-T01 "never two" |
| The 4:3 RT on the 1.47:1 quad stretches glyphs | §8.2: the terminal's own 0.276 × 0.207 quad; AT-T06 glyph aspect |
| F7 returns for terminals; the 8 m claim was wrong per platform | §4.1 r_A per tier and LOD membership (V3); AT-T10 |
| The definition lacked `ship` | §9.4 `ship = true` and code texelsPerMetre values |
| The transcript path is unscheduled | L5 (C24 and the desktop panel), T6 and T7 in §15.1 and §15.3; B-touch |
| Touch paging prerequisites and routing | §7.3 prerequisites, `PageStep`, `CurrentFaceViewportRect`; TT0, TT1 |
| WebGL pointer lock: no detection, live drop, the alt-tab order | §7.2 (L3): poll, Paused from Inspect, one rule; AT-T07 |
| No cheap tube pass on iOS and Android | §9.3 `_TUBE_CHEAP` platform path; AT-T10 |
| The merge chain held back EXAMINE's sound step | §15.4: the terminal package after step 5 |
| C26 and the `Media/` file were reassigned | Header; L1; §17 (EXR D11); A1 |
| No `Inspecting` event in phase 0 | L1 split into parts 1 and 2 |
| `inspect.sh` bases | §3.3 item 5; §15.4 phase A |
| Blink versus render on demand | §8.1: no cursor in v1; §9.1 |
| VT323 lacks the glyphs | §1; §8.1 drawn arrows; §8.7 cmap check in G1 |
| E on an iPad keyboard | §7.2: removed, with the reason |
| P9 mis-stated the TV anchor | P9 reworded |
| The code anchor versus the sidecar anchor | §2.2: the terminal reads the sidecar anchor; numbers use 0.194 and 1.1 cm |
| The stand box versus the 1.29 m row | §2.2 box redefined (0.9 m deep, the body centre 0.30–0.60 m); §0 and §2.3 agree |
| Page memory kills the glance | §4.1: FRONT always in the world; AT-T02 after a read |
| BlowsAudible had no v1 source | §6.1 `BlowHeard`, 36 m; S-T2 |
| The 16 m term's silhouette case | §6.1 notes; §18 |
| P7 used v2 data in phase A | P7, AT-T01 |
| The AT-T11 grep and the demo rig | AT-T11 |
| The title-corridor claim | §1 |
| The tap threshold | §7.3: 8 pt and 0.22 s |
| The touch origin | §7.3 coordinates in layout points |
| The hint counter owner and order | §12, V1 |
| Chords on `Move.x` | §7.1 per-key edges; AT-T07 chords |
| Stale citations (+1 EV, the define, the logo panel) | §1, §4.4, §9.1, §9.4 |

**Reviewer 2 (player experience and honesty):**

| Issue | Where |
|---|---|
| No light term | §6.1 `RelayLit`; AT-T15 dark-cell and lit cases; §18 reworded |
| Falling edges reveal silent changes | §6.1 holds; AT-T15 holds and "every return is perceivable" |
| v1 door blows heard to 36 m, not 45 m of path | §6.1 `BlowHeard`; S-T2; AT-T15 |
| Page memory versus the glance | §4.1; AT-T02, AT-T16 |
| WebGL pointer lock | §7.2; AT-T07 |
| Door memos teach v1's beacon | §8.5, §8.6 rule 3; AT-T13 |
| Touch prerequisites, double-tap, opening tap, 667-pt phones | §7.3; §15.4 B-touch; AT-T14 |
| A second "sees" test beside RELAY §8 | §6.6 RELAY §8 amendment with a consumer list; L4 |
| EXAMINE's re-issued Red list | §17 (EXR D1, D2, D9, D11); D2 merged; D13 closed; +2.25 EV; `E · READ SCREEN` |
| C26 ownership | Header; L1; D4 |
| Phase A showed an unusable index, lacked F7, merged placeholders | §15.4 phase A; §8.1 |
| Content contradictions (NORMAL, A-1114, A-3, the desk number, D6 letters) | §8.5, §8.7, §8.1, D6, §13 |
| Memo repeats | §8.5 deal; AT-T21; §3.1 measurement |
| The numbered index, scroll, ↑/↓ | §7.1, §8.1 |
| Held key at open; mashing | §7.1 per-key edges and rate; AT-T07, AT-T18 |
| The blinking cursor | §8.1 dropped; §9.1 |
| No glance contrast check | AT-T02 contrast; §9.3 |
| §0 versus §2.3, and AT-T02 margins | §0, §2.2, §2.3, AT-T02 |
| The SCREENS amendment and terminal power | §6.3 |
| The page sound sits in the latch family | §11; S-T1 |
| Ambient frames: colour clash, `SYSTEM IDLE`, the ad freeze | §3.5, §8.7, A1, AT-T19 |
| AT-T20 wording, AT-T17 live harness, missing tests | AT-T15–T21 |
| The DECIDE list (D0, D7, D10, D15, the mocks) | §17; G0 |
| The stage-2 argument, blink rate, cull per tier, P12 retry | §5, §8.1, §4.1, P12 |

### 20.2 Fixes done differently, and why

1. **Tilt.** Reviewer 1 offered a stand part or no tilt, plus a forward move of at least 0.08 m. I chose no tilt and a 0.03 m move. The 0.08 m figure was for the tilted case. Upright, the monitor's back needs only about 2.9 cm to clear the panel by 1 cm, and the pose is already inside the body band. The cost is that the phone's second line no longer reads at a glance; the headline still covers the whole stand box. The stand part stays possible as E10.
2. **Touch page events.** Reviewer 1 suggested a `PageTapped(int)` event in `Mobile.cs`. I used one `FrameSnapshot.PageStep` field for keys and taps instead. One path is simpler, and the same field also fixes the chord problem on desktop. With paging reading only `PageStep`, zeroing `frameMove` in Inspect is not needed.
3. **↑/↓ paging.** Reviewer 2 suggested `Move.y` edges. `Move.y` also carries S, which exits, so ↑ and ↓ are read as their own key edges inside `PageStep`.
4. **Opening tap over the whole screen rect.** Rejected. Reviewer 2 offered "or rely on USE", and I took that. Taking the move zone in Playing would steal the stick from every player who walks past a terminal. AT-T14 tests USE from anywhere in the stand box instead.
5. **Marking the remembered page in the FRONT index.** Not done. The option was optional. A FRONT that never changes keeps the glance and the invariance test simple.
6. **The lit term in RELAY §8.** Reviewer 2 offered one fixed test or two named tests. I split it into three named tests that share parts (§6.6). Adding light to the director's own `PlayerSeesRelay` would let Withdraw vanish, and Arrive appear, in front of a player standing in the dark, because those rules need the conservative side.
7. **Ambient screens following `LampBaseLevel`.** Reviewer 2's fix would still leak: RELAY §9.2 sets the burst set's lamps to Steady through `SetLampMode` during warnings, which changes `LampBaseLevel`. Ambient sets follow the generated temperament only (§6.3).
8. **Gating door memos on `FRONTROOMS_RELAY_V2`.** That define does not exist, and a content provider must never read hunter state. So door memos are held until RELAY step 4 instead.
9. **The page sound.** Reviewer 2 suggested a membrane or paper-like sound. Paper is wrong for a screen, so I asked for a soft key press with no sharp transient. The A/B test is added as asked.
10. **P11 (prefer a station visible from a doorway).** It is moved to later. The dresser has no sight test, so building one now is not justified before AT-T03 shows the tell needs it. No reviewer raised it; it changed because of the pre-pass redesign.
