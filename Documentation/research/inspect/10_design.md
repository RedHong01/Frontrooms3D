# 10 — EXAMINE: the close-up inspect system (design)

Status: **DESIGN v1, 2026-10-07** (visual chat 游戏视觉). It builds on `01_inventory.md`, `02_game_research.md` and `03_code_survey.md`, which are cited as `01 §`, `02 §` / `P#` and `03 §` / `M#`.

**Red's request (translated):** every visual asset the player can really look at (example: the Q16 evacuation plan, `research/placard/images/q16_u1_A3_legend_0p6m.jpg`) should be something the player walks up to, interacts with, and sees in a close-up shot. While the close-up is up, the game is paused.

**What was done.** Read the three research files, the camera rig, shot timings, glass RT system, post stack, kit library, input snapshot and hunter API in main, the placard spec, build report and generator (`Tools/print/ink/art_from_graphic/placard/placard.swift`, for exact cap heights), and the interactables spec (sizes). Unity was not opened. Figma was not touched. Nothing was downloaded. Files written: this doc, three support diagrams in `images/` (d_01 to d_03, §12) and their credits in `SOURCES.md` §4. The diagrams are not verification images, so the VERIFICATION LOG gets no row.

**Names.** The feature is **EXAMINE**. In code it is `Inspect` (`Phase.Inspect`, `ShotKind.Inspect`, `FrontRoomsInspect`), as `03` already uses. The words the player sees are 平面视觉's (§5).

**Tags:** **ESTIMATE** (my number, no source or run), **UNVERIFIED** (not confirmed), **CONTRACT** (an exact change for a file another chat owns), **DECIDE** (Red's call; my default is given).

---

## 0. The short version

1. **The shot type is "the camera goes to the object", in a paused world** (`02 §4.1`, `r_08`). It is a real camera pose, not a 2D overlay: only a real pose keeps the frame, the lens, the wallpaper, the lamp light and the glow ink (`02 §0.4`). Silent Hill 2 (2024) does the same for a framed wall photo (`02 §3.4`, M4).
2. **Three kinds, one system.** `flat` = wall prints and signs (camera goes there, square-on). `panel` = the face of a big fixture (camera goes there, small orbit). `object` = hand-sized things such as the key and tag (the object comes to the camera and turns, as in Gone Home and RE; `02 P2`).
3. **v1 targets:** the placard (first), the key with its tag, a "door ID" shot that frames the door sign and number plate together, and the key hosts. The placard lands with Q16; the rest land with Red's door/key call D1.5. Four live props are the test bed today: the wall clock, the vending machine, the copier and the desk phone (`01 §0.3`). They ship only if Red says so.
4. **Pause = `Phase.Inspect` + `Time.timeScale = 0`**, from the E press until the camera starts home (`03 §3`). This also closes today's pause leaks: lamps, doors and the door noise that reaches the Relay (`03 §2.3`). Control returns the moment the camera starts back. The camera eases home over 0.40 s while the player already moves.
5. **The placard framing is Red's own frame.** At FOV 61° (76 − 15, the rig's floor) the camera stands **0.36 m** from the sheet. The plan fills the left 62 % of the screen and the real wallpaper shows on the right, next to the legend that explains it. That is the same 2.55 px/mm as Red's reference render. One zoom step dollies to **0.18 m at 1080p** (0.20 m at 1440p, 0.31 m at 4K), so the smallest line (2.4 mm cap) reaches **12 px**. The zoom never magnifies the texture past 1:1.
6. **Timing:** in 0.40–0.55 s (the Unlock travel rule `0.30 + 0.12 × d`, with its floor raised to 0.40 s to keep the shared rule `MinFovChangeSeconds .4`); out 0.40 s; a hard cut at Camera motion Off. That is slower than the references (≤ 0.25 s, `02 §4.2`), but it matches our built shot language.
7. **Light: no added lamp.** The frozen world light is the light. In a dark cell the close-up lifts exposure by up to +1.0 EV, as an eye adjusts. This keeps the glow ink's ratio to the paper (a camera light would wash the glow out; `03 §11`, `02 P6`). No depth of field on flat prints. A mild, camcorder-deep far blur for hand objects only. The G14 glass RT keeps running: with the world frozen and the camera still, it converges to 32 samples.
8. **Chase rule (recommendation):** no prompt while the Relay sees the player, holds lock-on, chases or breaks a door. Warnings (stages 1–2) still allow a close-up, and they freeze with the world. Alternatives in §2.5. **DECIDE.**
9. **Hit targets** come from a visual-owned registry of boxes, tested against the aim ray after the physics ray. No colliders, no layers. All 8 gameplay queries stay as they are (`03 §1.4`, `§10.3`).
10. **Per-kit data** lives in an `inspect` block in the kit sidecar, with defaults derived from the bounds (§3.8). Live kits that will not be re-exported soon get a code fallback catalogue.
11. **Map chat:** 24 contract requests, written as code (§7.2). **Sound:** one snapshot and three Foley events, through events only (§7.3). **Touch:** one menu state, one use glyph, a zoom field (§7.4). **平面视觉:** the verbs, one hint line, the transcript panel and one glyph (§7.5).
12. **The acceptance test that matters most is pause neutrality.** Two runs with the same seed and inputs, one with a 10 s close-up and one with a 0.2 s close-up, must give the same world state hash afterwards (§8.2, AT-01g).

---

## 1. Scope

### 1.1 v1 targets (MUST)

| # | Target | Kind | Today | Lands with | Notes |
|---|---|---|---|---|---|
| E1 | **Evacuation placard** `Kit_EvacPlacard` (01 M1) | flat | built in `W/proj_placard`, not in main | Q16 promotion | The showcase. 1 per run, at the start door; more copies at sanctuaries later (`RELAY_PURSUIT_REDESIGN.md` §10) |
| E2 | **Zone key + ring + tag** (01 M4) | object | key is a cube; kits in main, unspawned | D1.5 | Examined **in the hand**, during the key pickup (§2.5, row K) |
| E3 | **Door ID**: door sign + number plate on one locked door face (01 M2 + M3) | flat | kits in main, unspawned; text slots missing | D1.5 + `Prop_SignEngraved` / `Prop_KeyTagNo` | One target per locked door face, framed together (§3.3). Only while the door is locked (§2.5, row D) |
| E4 | **Key hosts** `Kit_KeyHookBoard`, `Kit_KeyCabinet` (01 S6) | flat | unspawned; number slot missing | D1.5 | Promoted from SHOULD: the empty hook and the numbers are a hint |

### 1.2 Later, and the test bed

| # | Target | Kind | Use now | Ship? |
|---|---|---|---|---|
| T1 | Wall clock `Kit_WallClock` (01 S5) | flat, hung at 2.1 m | **test bed:** the look-up pose (§3.3) | DECIDE (default no) |
| T2 | Vending machine `Kit_VendingMachine` (01 S2) | panel (the `window` anchor) | **test bed:** a big fixture with a collider | DECIDE; needs 平面视觉's $1.45 era check |
| T3 | Copier `Kit_Copier` (01 S3) | panel (the `console` anchor faces up) | **test bed:** a face that looks up | DECIDE |
| T4 | Desk phone `Kit_DeskPhone` (01 S4) | object | **test bed:** the object kind before the key exists | DECIDE; not measured at 0.3 m |
| L1 | Office CRT monitor (01 S1) | panel (`screen`) | no: its E is the power toggle | after 平面视觉 item 3 (LEVITZ must go) |
| L2 | CRT TV in piles (01 S7) | panel | no: piles turn and invert it | later |
| L3 | A memo or page on `Kit_PaperStack` / `Kit_Binders` | object | no: the pages are blank | if narrative writes one (01 §0.4) |

Test-bed targets carry `ship: false`. They register only when `FrontRoomsInspect.DevTargets` is on (Editor and dev builds).

### 1.3 Not inspectable

- Everything in `01 §4` (about 90 kits): nothing to read, the same on every desk, or it already has its own shot (doors, glass).
- **Read in place instead (P1).** These read by walking up, so they get no close-up:
  - The **scratch-throughs** (01 M5): letters 25–35 mm tall give 17–24 px at 1 m (1080p, FOV 76). Their texture (1,365 px/m) would fail a close-up anyway. They are also on hold (Q2).
  - The **EXIT sign**: one word, readable at 20 m.
- **Rule for future assets:** a close-up only when the asset carries text or detail that does **not** read at 1.0 m, **and** it is rare (P11; Control's sequel was faulted for "reading breaks", `02 §3.14`). Every inspectable also needs a title that reads at about 1 m, so the player knows it is worth the walk (P1). The placard's 15 mm title gives 10 px at 1.0 m (`02 §5`).

---

## 2. Flow and states

### 2.1 What the player does

1. **Approach.** The title reads in place at about 1 m.
2. **Prompt.** One line under the crosshair, like the door prompts (`03 §1.2`).
3. **Enter.** E (desktop), or USE / a tap on the target (touch). The world freezes on that frame.
4. **Close-up.** The camera travels to the framed pose in 0.40–0.55 s. The HUD fades in 0.15 s.
5. **Read.** Optional: one zoom step, pan while zoomed, turn an object. A transcript if the setting is on.
6. **Exit.** E, S, Back or the touch USE button. Control returns at once; the camera eases home in 0.40 s.

### 2.2 When the prompt appears

All of these must hold, checked every frame from `UpdateAim` (CONTRACT C3). The cost is in §8.1.

| Check | Value | Why |
|---|---|---|
| The aim ray from `BaseEye` hits the target's **aim box** before any collider | box hit ≤ physics hit + 0.02 m | The box sits on a wall or leaf face, so it must beat the surface behind it. A door or furniture **in front** still wins. `03 §10.3` |
| Distance from the eye to the box hit | ≤ `reach`, default **1.6 m** (door reach stays 2.4 m) | Red: "walk up to". At 1.6 m the title is already readable (`02 §5`). Q3 in `03 §15` |
| Eye angle off the face normal | ≤ `approachAngle`, default **60°** | No reading from edge-on |
| `Available()` | true (default) | Door ID: only while the door is locked (§2.5, row D) |
| A pose solves and its path is clear | §3.2 | Nothing is ever framed through a column or leaf (`03 §4.1` limit 4) |
| Game gate | `CanInspect` (CONTRACT C4) | Phase, Relay, shots, climb, glass, latch, cooldown (§2.5) |

**Aim forgiveness.** Each box is padded by `aimPad`, 0.02 m by default. Small things get at least 0.08 m: the key ring (58 mm) is 2.2° wide at 1.5 m, which is hard to hit.

### 2.3 States

| State (game phase) | Time | Camera | Input | Leaves to |
|---|---|---|---|---|
| **Playing**, target aimed | scale 1 | `BaseEye` | normal; prompt shown | E → Opening |
| **Opening** (`Inspect`, shot weight < 1) | **scale 0** | blends in on unscaled dt | E/S swallowed for the first 0.20 s, then they exit | weight 1 → Holding; E/S → Closing; Esc → Paused |
| **Holding** (`Inspect`, weight 1) | **scale 0** | the framed pose plus pan, zoom, turn | Look = pan / turn; Zoom; E/S/Back exit; Esc pauses | E/S/Back → Closing; Esc → Paused |
| **Paused from Inspect** (`Paused`, `pausedFrom = Inspect`) | **scale 0** | held | the pause card as today | Esc → back to the same close-up; R → restart |
| **Closing** (`Playing`, shot ending) | **scale 1** | blends home over 0.40 s on game dt | **full control**; new close-ups refused until the shot ends (0.40 s). A 0.3 s cooldown from the exit covers the cut at Camera motion Off | shot ends → Playing |

The diagram is `images/d_01_inspect_flow_timeline.jpg`.

### 2.4 One close-up, in numbers (placard, player 1.5 m from the wall)

| t (unscaled s) | What happens |
|---|---|
| 0.00 | E down. `CanInspect` is re-checked. Any glass hold is released. `FrontRoomsInspect.Open` solves the pose and begins `ShotKind.Inspect`. `SetPhase(Inspect)` sets `timeScale = 0`. `Inspecting(true)` and `ShotStarted(Inspect, focus)` fire, and the sound snapshot starts (§4.3) |
| 0.00–0.15 | The HUD fades out (`HudFadeSeconds`) |
| 0.00–0.44 | The camera travels about 1.2 m on the rig's cubic ease in-out; FOV 76 → 61. The post volume follows the same weight |
| 0.20 | From here, E or S exits |
| 0.44 → | Holding. Exposure adapts with τ 0.6 s (dark cells only). The glass RT converges in about 32 frames if glass is in view |
| exit | E: `FrontRoomsInspect.Close` → `rig.EndShot`. `SetPhase(Playing)` sets `timeScale = 1`. `Inspecting(false)` fires. The E latch is set. The player can move now |
| exit + 0.40 | The camera is home and `ShotEnded(Inspect)` fires. A new close-up may open (the 0.30 s cooldown has already passed) |

### 2.5 Edge cases

| Row | Case | What happens | Source |
|---|---|---|---|
| **C** | **Relay chase, sight, lock-on, BreakDoor** | **Recommendation A: no prompt.** The gate hides it when `relay.SeesPlayer` or the state is `Chase` or `BreakDoor` (plus the v2 lock-on hold once it is built). There is no "can't read now" text (minimal HUD). E then does what the aim would do without the target: usually nothing on a wall. Esc still pauses as today. *Why:* a cinematic in a chase takes control away, and a frozen chase invites studying the legend with the Relay 2 m away. **Alternative B:** always allowed, always paused, exactly like Esc. It is literal to Red's words but costs the chase its tone. **Alternative C** (a setting, the Bunker's Shell Shock mode, P5): "Close-ups pause the game: Off". Time runs and `ShotCancel.Relay` ends the close-up the moment the Relay sees the player. Not in v1 unless Red asks. **DECIDE** | `02 P4`, `§3.6`, `§3.13`; `03 §13.1 M5` |
| **W** | **Relay warning, stages 1–2** (your room's lamps flicker; muffled steps) | **Allowed, and fully frozen.** A lamp caught mid-dip stays dark; the Relay stands where it was; its steps pause (sound chat). After exit the warning carries on from the same point. Nothing new happens inside the close-up (P7) | `RELAY_PURSUIT_REDESIGN.md` §8–9; `02 P7` |
| **K** | **The zone key in the hand** (D1.5) | The KeyPickup shot is specified in `FrontRoomsShotTimings.KeyPickup` but **not built yet**: today's cube is vacuumed with no held pose. D1.5 builds it. It lifts the key to the held pose (`Arrive` 0.25 s → `HoldUntil` 0.75 s). **A second E in that window** (accepted from 0.15 s) hands over to the close-up. The map ends its KeyPickup rig shot with no blend. The key's own timeline **must run on game time**, so it freezes. `FrontRoomsInspect` moves the key from the held pose to the centre at 0.20 m (0.40 s, unscaled) and lets the player turn it. On exit the key goes back to the held pose in 0.40 s and the map's timeline resumes at its frozen age: the pocket beat, then the sound. `KeyTaken` has already fired at Arrive, so the HUD shows the key number throughout. If D1.5 keeps the vacuum (no E to take), the window still works, because the shot plays either way. *Alternative:* a camera dip to the key while it still hangs (the Unlock grammar). It is harder: the vacuum takes the key at 0.9 m, so the player has only a 0.9–1.6 m window. **DECIDE** (`02 §7` Q4) | `FrontRoomsShotTimings.KeyPickup`; `02 P2` |
| **D** | **Door sign and plate vs "E · OPEN DOOR"** | One **door ID** target per locked door face: the union of the sign box and the plate box, padded by 0.02 m. **While the door is locked**, E inside that patch = READ; E elsewhere on the leaf = the rattle and the LOCKED prompt, as today. **Once the key unlocks it**, `Available()` turns false and the door verbs win everywhere. A box behind an open leaf never wins, because the leaf collider is hit first | `03 §10.3`, M22; `01 §0.5` |
| **H** | **Keys held across enter and exit** | Move, look and sprint are not read while inspecting (`UpdateMapPlay` does not run). They are read live again on the exit frame: W held walks, Shift held sprints. Nothing is queued. The E that exits is swallowed: the same frame's `UpdateAim` sees the Inspect shot ending, and `rig.Consume` returns true. After that, **E must be released** before it acts again (the E latch). The look delta used for panning never reaches yaw or pitch. Two mouse settle frames on return, as after Esc | `03 §6`; `FrontRoomsCameraRig.Consume` |
| **G** | Glass hold, glass shot, climb, door shot running | No prompt (`rig.InShot`, `glassShot.Active` or `Blending`, `climbTime ≥ 0`, `pullClear`). Leaving a pane already releases its hold (`03 §1.2`) | `03 §4.2`, `§14` risk 11 |
| **E** | Esc inside a close-up | The pause card opens over the close-up. Time stays at 0. Esc again returns to the same close-up. R works from that card (it reloads the scene, and `Awake` resets `timeScale`) | `03 §2.1`, M6, M9 |
| **R** | R inside a close-up | Ignored. Today R restarts in any phase except Playing and Title, so the new phase must be excluded | `03 §14` risk 1 |
| **F** | Focus lost, app paused, a phone call | → Paused with `pausedFrom = Inspect`. On return, Esc continues the close-up | `03 §2.1` |
| **B** | Android Back | Closes the close-up (one step back) | `03 §7` |
| **M** | Camera motion 50 % / Off | A full pose at any setting (`fullPose`). At Off the shot is a **hard cut** both ways, which SH2 (2024) shows reads as deliberate. Shake and offsets stay scaled | `03 §4.1` limit 2; `02 §3.4` |
| **U** | The target's chunk unloads | Not expected: the player does not move, and streaming only drops chunks behind a moving player. If `Current` is disabled anyway, close at once with no blend | ESTIMATE |
| **A** | Autopilot, baselines, capture harnesses | `FrontRoomsInspect.Enabled = false`. A capture `deltaTime × 0` would stall them | `03 §3` (capture row) |
| **T** | Title stream, Caught, settings open | No targets in the title stream (no gate needed); no aim in Caught; no E while settings are open | `03 §2.2` |
| **P** | Prompt flicker at the reach edge | Hysteresis: a prompt shown stays until 1.7 m or 65° | ESTIMATE |

---

## 3. The shot

### 3.1 Three kinds

| Kind | For | The camera | The object | In the close-up | Family (`02 §4.1`) |
|---|---|---|---|---|---|
| `flat` | wall prints, signs, plates, boards, the clock | goes to a square-on pose | stays | pan (while zoomed), one zoom step | camera goes to the object (SH2 2024, Lethal Company) |
| `panel` | the face of a big fixture (vending window, copier console, CRT screen) | goes to the face | stays | small orbit ±20° yaw, ±10° pitch; one zoom step | same, with a little parallax (Signalis close-ups) |
| `object` | hand-sized things (key and tag, desk phone, a memo) | stays at `BaseEye`; FOV −10 | comes to 0.20–0.50 m in front of the camera | free turn (yaw 360°, pitch ±80°); one zoom step | object brought to the camera (Gone Home, RE examine) |

A 2D page is never used. It would drop the period object, which *is* the printed sheet (`02 P10`, `§0.4`).

### 3.2 The framing solver (`FrontRoomsInspectFraming.Solve`, pure, unit-tested)

**Inputs:**
- the face: centre `C`, outward normal `N`, text-up `U`;
- `size` (w, h) and `margin` per side;
- the screen rect the framed rect goes into (default: centred, full screen);
- FOV `v = BaseFov + fovDelta`, and the aspect;
- the eye `E` (`BaseEye`);
- `minDist` and `maxDist`, `maxAngle`;
- the body limits: camera height in **[E.y − 0.55, E.y + 0.10]** (a stoop down, tiptoe up; ESTIMATE).

**Steps:**
1. **Distance.** If `dist[0]` is set, `d = dist[0]` and the framed rect is centred in the `screen` rect (the placard: 0.36 m, centred in the left 63 %). Otherwise `d = max(minDist, fitH, fitW)`, where `fitH = h(1+2m) / (2 · tan(v/2) · screenH)` and `fitW = w(1+2m) / (2 · tan(v/2) · aspect · screenW)`. `screenH` and `screenW` are the fractions of the screen the framed rect may use.
2. **Direction.** Start at `D = N`. If `C + D·d` is outside the height band, tilt `D` toward the eye's side, up to `maxAngle`. If even `maxAngle` cannot reach the band, raise `d` until it does (≤ `maxDist`), or give up: no prompt.
3. **Horizontal fallback.** If the straight path from `E` to the pose is blocked, try `D` turned ±20° toward the eye (within `maxAngle`). Then try the fallback composition: full screen, `margin`, `d` solved (for the placard: centred at 0.31 m). If nothing is clear: no prompt.
4. **Rotation.** Look along `−D`. Up is world up projected, except when `|N·up| > 0.7` (a face that looks up or down): then up is the face's text-up `U`, so the copier's keys read the right way.
5. **Path check.** `rig.ClampToWorld(E, pose)` must return the pose (within 1 mm). That spherecast sweeps the **whole** straight segment, so a clear check means a clear travel. The pose also keeps ≥ **0.15 m** from any collider face (the clamp sphere is 0.1 m; near plane 0.06 m). The registry's own boxes are tested on the same segment, because render-only objects have no colliders.
6. **Cache.** Results are cached per target and invalidated when the eye moves more than 1 cm or turns more than 0.5°.

**The rig does the travel** (straight line, cubic ease in-out on the weight). Each frame the rig clamps the camera against the world again, so pan and zoom can never push it into a wall.

### 3.3 Per-asset numbers

At 1080p and 16:9. FOV 61° for flat and panel, 66° for object.
- **px/mm** = 1080 / (2 · d · tan(v/2)) / 1000.
- **Travel** is from a typical stand to the pose.

| Target | Framed rect (m) | d base | d zoom (1080p / 1440p / 4K) | Camera height · pitch | Fill | px/mm base 1080p | Notes |
|---|---|---|---|---|---|---|---|
| **E1 placard**, Red's composition (default) | 0.4668 × 0.3138 frame, in the **left 62 %** of the screen | **0.36** | 0.18 / 0.204 / 0.306 | 1.524 · 0° (a 0.10 m drop) | 62 % w, 74 % h | **2.55** | Same scale and layout as Red's reference render. The wall to the right of the legend is the real WP03 the legend describes (01 M1). Fallback: centred |
| E1 placard, centred (fallback / alternative) | same, centred, 8 % margin | 0.31 | 0.18 / 0.204 / none | same | 72 % w, 86 % h | 2.97 | `03 §11`. Used when the legend side is blocked (P2 or P3 mounts near a corner) |
| **E3 door ID** | sign 0.254 × 0.076 at 1.524 + plate 0.100 × 0.050 at 1.000 → 0.59 m tall | **0.58** | 0.36 (the plate) | 1.27 · 0° (a 0.35 m stoop) | 86 % h | 1.59 | Sign 17 mm cap = 27 px. **The plate digits need ≥ 7.6 mm cap** for 12 px (平面视觉, `Prop_KeyTagNo`) |
| **E2 key + tag** | assembly ≈ 0.13 m long | **0.20** (object) | 0.15 | at `BaseEye`; the key comes to the centre | 50 % h | 4.15 | The tag texture is 3,300 px/m, so it is magnified 1.26× at 1080p. **Ask (us):** build `Prop_KeyTagNo` at 4096² (≈ 6,600 px/m) so it holds at 1440p (§8.1) |
| **E4 key board** | 0.30 × 0.40 | 0.39 | 0.25 | ~1.45 · 0° | 86 % h | 2.33 | |
| **E4 key cabinet** (door open) | 0.72 × 0.46 | 0.45 | 0.30 | ~1.47 · 0° | 86 % h | 2.03 | |
| T1 wall clock | Ø 0.32, at 2.1 m | **0.59** (forced: tiptoe ≤ +0.10 m) | none (hands are geometry) | 1.72 · **40° up** | ≈ 40 % h (foreshortened) | 1.55 | The test case for the height band and `maxAngle` |
| T2 vending window | from the `window` anchor; size to measure (ESTIMATE ≈ 0.55 × 0.95) | ≈ 0.9 | 0.5 | ~1.2 | — | — | panel; orbit ±20° |
| T3 copier console | faces up; size to measure | ≈ 0.4 | 0.25 | ~1.3 · ≤ 65° down | — | — | the text-up rule (step 4) |
| T4 desk phone | 0.20 × 0.09 × 0.32 | 0.50 (object) | 0.35 | at `BaseEye` | 50 % h | 1.66 | |

**Travel time** = `clamp(0.30 + 0.12 × travel_m, 0.40, 0.55)`:
- 0.40 s from 0.7 m;
- 0.44 s from 1.2 m;
- 0.49 s from 1.6 m.

Diagram: `images/d_03_inspect_poses.jpg`.

### 3.4 Text scale targets

**The rule (1080p, cap height on screen):**
- **≥ 12 px reads**;
- **9–12 px reads with effort**;
- **< 9 px needs the zoom step**.

Every line on an inspect target must reach ≥ 12 px **after** the zoom step at 1080p. Lines that carry the message (title, YOU ARE HERE, EXIT, the legend labels, the footer) should reach ≥ 12 px at base framing at 1440p. ESTIMATE: these are my thresholds, not a published game-text rule; the optional transcript (§5.2) covers players who need more.

**The zoom step never magnifies the texture past 1:1:**
- `d_zoom = max(minDist 0.18 m, H / (2 · tan(v/2) · texelsPerMetre))`.
- It is offered only if `d_zoom < 0.9 × d_base`.
- For the placard (6,000 px/m) this gives 0.18 m at 1080p, 0.204 at 1440p and 0.306 at 4K. So the zoom adapts to the resolution, and the art never goes soft.

**Placard caps, exact from `placard.swift`** (mm → px). The columns are composition A at 0.36 m.

| Line | Cap (mm) | 1080p base | 1080p zoom 0.18 | 1440p base | 1440p zoom 0.204 | 4K base |
|---|---|---|---|---|---|---|
| EVACUATION PLAN | 15.0 | 38 | 76 | 51 | 90 | 76 |
| IN POWER FAILURE, WAY ON, EXIT, NO EXIT (glow ink) | 5.2 | 13.2 | 26 | 17.7 | 31 | 26 |
| YOU ARE HERE; footer | 4.4 | 11.2 | 22 | 14.9 | 26 | 22 |
| EXIT (plan), AS PRINTED | 4.2 | 10.7 | 21 | 14.3 | 25 | 21 |
| Grid bubbles, title block | 3.6 | 9.2 | 18 | 12.2 | 22 | 18 |
| N | 3.2 | 8.1 | 16 | 10.9 | 19 | 16 |
| MATCH LINE — SEE SHEET A-3 | 3.0 | 7.6 | 15 | 10.2 | 18 | 15 |
| SCALE: 3/16" = 1'-0" (regular) | 2.8 | 7.1 | 14 | 9.5 | 17 | 14 |
| WALLCOVERING SHOWN AT 1/25 SIZE (regular) | 2.4 | 6.1 | **12.2** | 8.1 | 14.4 | 12.2 |

- **Result:** with the zoom step every line passes at every resolution.
- **At base framing at 1080p** the message lines are 10.7–13.2 px: readable, slightly under the comfort line. Two things fix that if Red finds it small. Composition B (centred 0.31 m) gives +17 %. Or 平面视觉 can raise the 4.2–4.4 mm lines (§7.5).

The diagram `images/d_02_placard_base_vs_zoom.jpg` shows base vs zoom, simulated by scaling and cropping Red's render. A square-on plane scales exactly, but the in-game zoom samples the 6 px/mm texture, so it is sharper than this mock.

### 3.5 Transition

- **Clock.** Unscaled. The game ticks the rig with `Time.unscaledDeltaTime` in `Phase.Inspect` (CONTRACT C7). The rig stops in any phase other than Playing today (`03 §4.1` limit 1).
- **Curve.** The rig's own cubic ease in-out on the weight, for both position and FOV. No new curve: the research's 0.25 s ease-out return (`02 P3`) is not worth a rig change.
- **In:** 0.40–0.55 s (§3.3).
- **Out:** **0.40 s**. That is the shared `MinFovChangeSeconds` for a 15° change. A 0.25 s return would break that comfort rule, and the Unlock cancel already does.
- **Early exit** after 0.20 s: the game calls `rig.EndShot`, which returns over 0.40 s from the current weight. `rig.Consume` and its 0.25 s cancel are not used.
- **Camera motion Off:** a cut in (`blendIn 0`) and a cut out (`blendOut 0`) (`fullPose`, CONTRACT C17).
- **Collision.** The path check is in §3.2 step 5. The rig's per-frame clamp is the safety net. Rendering: near plane 0.06 m against a ≥ 0.15 m stand-off. The object kind keeps `d − boundingRadius ≥ 0.08 m` while turning.
- **Zoom and pan** inside the shot are damped toward their targets: zoom 0.25 s, pan 0.12 s, turn 0.08 s (critically damped, unscaled). The rig takes the pose as given.

### 3.6 Controls inside the close-up

| Input | flat | panel | object |
|---|---|---|---|
| Mouse / one-finger drag (`Look`) | pan in the face plane, **only while zoomed**, clamped so the view never shows more than the margin past the framed rect | orbit ±20° yaw, ±10° pitch around the focus | turn: yaw unlimited, pitch ±80° (1 look-degree = 1°) |
| Scroll / RMB / pinch / double-tap (`Zoom`, new) | one step in or out. The first zoom-in goes to the region nearest the screen centre (for the placard, the **legend**) | one step | one step (0.70 × d) |
| E, S, Back, touch USE | exit | exit | exit |
| Esc / pause button | the pause card over the close-up | same | same |

**At base framing the mouse does nothing.** The still frame is part of the "paused" read. It also lets the RT accumulate and keeps the text crisp. The hint line teaches the zoom (§5.1).

### 3.7 The look

**Light: no added lamp.** The frozen world light is the light, as SH2 (2024) lights its close-up only with James's own flashlight (`02 §3.4`, P6).

| Option | Verdict | Why |
|---|---|---|
| A camera-mounted light (Unlock's optional `ShotLightIntensity .6`) | **no** | Non-diegetic: FrontRooms has no flashlight. It would wash out the placard's glow ink (`03 §11`), and it costs 1 of 32 WebGL lights |
| A lighter or matches | **no (v1)** | Period-plausible, but it is a new mechanic that changes the horror, and it would also wash out the glow |
| **Eye adaptation (exposure)** | **yes** | Period-neutral (the eye, not a device). It multiplies paper and glow alike in linear light, so the glow ratio before the tonemap (1.23 in the dark cell, `01 M1` FLAG) is unchanged; AT-01j checks the displayed ratio does not drop. It needs no GPU readback |

**Exposure adaptation:**
- The wall light uses the same model as the glow, read from the logical lamp level of the target's cell and its 4 neighbours (`map.LampLevel`, read-only): `W = min(1, L + 0.2 · ΣL_n)`.
- `lift = clamp((0.45 − W) / 0.45, 0, 1) × 1.0 EV`, so it is 0 in a lit cell and +1.0 EV in a dead one.
- It ramps with τ **0.6 s** (Reduce flashing: 1.2 s), unscaled, and fades out with the shot weight.
- ESTIMATE; tune on captures.

**Post volume** `Post / shot Inspect`: global, priority 2. Its weight is the eased shot weight (`ShotHandle.Weight`, CONTRACT C18), read in `LateUpdate` on unscaled time. Profiles `FrontRoomsPost_Inspect` (flat, panel) and `FrontRoomsPost_InspectObject`:

| Override | Base film look (`03 §5`) | flat / panel | object |
|---|---|---|---|
| Exposure (`ColorAdjustments.postExposure`) | +0.15 | +0.15 + lift (driven per frame) | same |
| Vignette | 0.26 | **0.30** | **0.34** (the glass shot's +0.08) |
| Lens distortion | −0.04 | **0** (frame rails stay straight) | −0.04 |
| Chromatic aberration | 0.06 | **0.02** (crisp letter edges) | 0.06 |
| Depth of field | none | **off**: the sheet and the wall are one plane (`02 §4.5`: no game blurs a 2D page) | **Bokeh**, focus = camera → object distance per frame, tuned so the background at 3 m blurs ≈ **4–6 px at 1080p**. A 1990 ½-inch CCD camcorder at this field of view (≈ 3.7 mm lens, f/4, key at 0.20 m) gives ≈ 4 px: deep focus, not macro bokeh (audit §3.0) |
| Bloom, white balance, grain, tonemapping | as base | as base | as base |

**Things the look must hold:**
- **Glow ink.** `FrontRoomsPlacardGlow` runs on `Time.deltaTime`, so it holds its value at scale 0 (`03 §3`). That is right: the frozen frame is honest, and a dead lamp is exactly when the legend reads (`02 P6`). No change.
- **Grain.** UNVERIFIED whether URP's film grain still moves at scale 0 (`03 §16`). If it freezes, a still grain pattern reads as dirt on the lens. Then the inspect volume takes grain from 0.22 to 0.10. This is look-dev check AT-01k.
- **G14 glass RT keeps working.** It is driven by `Time.frameCount` and `realtimeSinceStartup`, not game time (`FrontRoomsGlassRTSystem.cs:882-884`). Here is what happens in a close-up:
  - While the camera travels, `viewProj` changes every frame, so accumulation stays at 0: ordinary per-frame RT, as when walking.
  - Once the camera holds, the world is frozen and the lamp signature is stable, so accumulation climbs to its cap of 32 (`:1186-1189`), and reflections refine over about 0.5 s at 60 fps.
  - Panning resets it, and it refines again.
  - The TLAS still rebuilds around the camera each frame. With no moving instances (the Relay frozen, doors still) no transforms change, so the close-up costs no more RT than play.
  - Nothing to freeze, and no new code. The fog term reads `Time.time`, which holds still, which is correct.
- **No hitch on entry.** URP compiles the DOF and changed uber-post variants, and builds their Metal pipeline states, at first use. So `FrontRoomsInspectLook.Prewarm()` renders one frame with the inspect volumes at weight 0.001 when the map run starts (`MapRunStarted`), behind the start door.
- **LOD and shadows.** The pose is inside every target's LOD0 range (placard LOD0 to 2 m, key and tag to 1.0 m). The placard keeps `ShadowCastingMode.Off` (spec D10).
- **Lens.** The placard's 1.0 mm non-glare lens stays (desktop). At the square-on pose its F0 0.039 reflection shows the frozen ceiling, which is honest. The lens is skipped on WebGL (spec D16).

### 3.8 Data: the `inspect` block

It is written by `kitlib.py` from each asset module, and read by `FrontRoomsKitLibrary.Info` (`03 §10`). Positions are Unity part-local, converted through `to_unity` like colliders. Unknown keys are ignored by `JsonUtility`, so old sidecars and the importer are safe.

```json
"inspect": {
  "kind": "flat",
  "verb": "read",
  "face": "face", "faceDir": "face_dir", "up": "",
  "size": [0.4668, 0.3138],
  "margin": 0.08,
  "screen": [0.01, 0.0, 0.63, 1.0],
  "dist": [0.36, 0],
  "minDist": 0.18, "maxDist": 1.0,
  "maxAngle": 25, "fovDelta": -15,
  "reach": 1.6, "approachAngle": 60,
  "orbit": 0,
  "box": { "c": [0, 0, 0.007], "s": [0.4668, 0.3138, 0.014] },
  "aimPad": 0.02,
  "texelsPerMetre": 6000,
  "regions": [ { "id": "legend", "rect": [0, 0, 0, 0], "capMm": 2.4 } ],
  "dof": false,
  "ship": true,
  "transcript": "placard_q16",
  "sound": "metal"
}
```

| Field | Meaning | Default from bounds |
|---|---|---|
| `kind` | `flat`, `panel` or `object`; empty = not inspectable | (none: opt-in only) |
| `verb` | prompt id: `read` or `look` | `read` for flat, `look` otherwise |
| `face`, `faceDir`, `up` | anchor names | `face` / `face_dir` if present; else the front face centre (`frontAxis` +Z) and +Z; up = +Y |
| `size` | the framed rect (m) | bounds across the face |
| `margin` | per side, a fraction of the size | 0.08 |
| `screen` | viewport rect (x, y, w, h) the framed rect is centred in. With `dist[0]` set, the distance is fixed; with 0, it is solved to fit `screen` with `margin`. The fallback is always full screen, `margin`, solved | full screen |
| `dist` | [base, zoom] (m); 0 = solve | solve |
| `minDist`, `maxDist` | (m) | 0.18, 1.0 |
| `maxAngle` | degrees off square-on | 25 (flat), 40 (centre above 1.9 m) |
| `fovDelta` | degrees | −15 (flat, panel), −10 (object) |
| `reach`, `approachAngle` | prompt range (m), degrees | 1.6, 60 |
| `orbit` | panel orbit (± degrees) | 20 |
| `box` | aim box, part-local centre and size | bounds |
| `aimPad` | (m) | 0.02, at least 0.08 m overall |
| `texelsPerMetre` | the sharpest texture on the face (caps the zoom) | 2,048 (the audit's safe line) |
| `regions` | readable regions in sheet mm, with the smallest cap. The rect in the example is a placeholder until 平面视觉 exports the real ones | none (zoom goes to the centre) |
| `dof`, `ship`, `transcript`, `sound` | as named | false, **false**, "", `plastic` |

Notes:
- `ship` defaults to false, so a kit must opt in on purpose.
- **Placard regions:** 平面视觉 exports them from `placard.swift`'s own layout constants. They are never measured by eye (Red's rule: measure type exactly, never guess).
- **Fallback catalogue.** `FrontRoomsInspectCatalog` holds blocks in code for kits that will not be re-exported soon (the clock, vending machine, copier, phone). A sidecar block always wins over the catalogue.
- **Composite targets** (the door ID) are registered by a helper, not a sidecar (§7.1).
- **Pile pieces never register** (`Info.IsPilePiece`). Piles scale and invert them.

---

## 4. Pause semantics

### 4.1 Mechanism

**`Phase.Inspect` + `Time.timeScale = 0`**, from the E press until the camera **starts** home (`03 §3`). This needs both:
- the phase gate alone leaves the lamp, door, noise and Foley leaks (`03 §2.3`);
- the time scale alone would still run `UpdateMapPlay` with dt 0 and read input.

**Control returns when the camera starts home** (`02 P4` rule 2: resume early). The 0.40 s return is live: the player can already move, and the Relay moves too. With the chase gate (§2.5 C), the Relay is never chasing at that moment.
- *Alternative:* resume only when the camera is home. It is fully safe but adds 0.40 s of dead control. **DECIDE** (`02 §7` Q1).

**Should the Esc pause set `timeScale = 0` too?** I recommend it. It closes the same leaks (a door swing during Esc still sends noise to the Relay today). That is the map chat's call (`03 §15` Q1); it is optional request C8b.

### 4.2 What stops, and what keeps running

| Stops (scale 0 and the phase gate) | Clock today | Source |
|---|---|---|
| Player move, look, aim, prompt, stamina, sprint | game dt in `UpdateMapPlay` | `03 §2.3` |
| Relay brain and rig; the v2 director, alarm and dry clock **must use game dt** (a rule for the map chat) | game dt | `03 §3` |
| Lamps: `TickFixtures`, overrides, Warn bursts; no `FixtureChanged` | `Time.deltaTime` | `FrontRoomsMapWorld.cs:842` |
| Doors: swing, unlock delay, `DoorMoved`; **no noise reaches the Relay** | `Time.deltaTime` | `:843`, `03 §2.3` |
| Start-room stream, title stream | game dt | `03 §3` |
| Tier clock, `elapsed` (the caught card's seconds), captions tick, rattle jolts, hint card timer | game dt | `FrontRooms3DGame.cs:2157-2165` |
| Physics steps (no Rigidbodies exist) | fixed step | `03 §3` |
| Placard glow, key spin, revisit timer (`Time.time`) | game time | `03 §3` |
| The wallpaper cue clock (when built) **must use game time** | — | `01 §5` |
| Player, Relay and door Foley components | `Time.deltaTime`, early out at 0 | `03 §3` |
| Screen ads: paused **explicitly** on `Inspecting(true)` (their default clock is UNVERIFIED) | `VideoPlayer` | §7.6 |

| Keeps running (unscaled or per frame) | Why |
|---|---|
| The camera rig (ticked by the game with unscaled dt) | the travel |
| `FrontRoomsInspect` (pan, zoom, turn, hint) and `FrontRoomsInspectLook` (volume weight, exposure, DOF focus) | the close-up itself |
| HUD fade, touch layer, haptics | already unscaled (`03 §3`) |
| G14 glass RT, zone reflection fades | frame-based; it converges (§3.7) |
| Streaming and dressing | harmless: nothing moves (`03 §3`) |
| FMOD runtime and the sound director | **the sound chat pauses buses with a snapshot** (§4.3) |

### 4.3 Audio (sound chat; events only, no new AudioSources)

They subscribe to `FrontRooms3DGame.Inspecting(bool)` (or `ShotStarted/ShotEnded(ShotKind.Inspect, focus)`). Proposed, their call:
- **On `Inspecting(true)`:** start `snapshot:/Inspect`.
  - Pause `bus:/SFX`: Relay steps, door Foley, window stress.
  - Pause the Relay and `bus:/Subjective` voices.
  - **Keep the room tone (`bus:/AMB`) 10 dB down.** The pause should still sound like the place: Grip on SOMA's Safe Mode, "tense … despite knowing I couldn't get killed" (`02 §3.9`).
  - Hold the director's tension slide.
- **On `Inspecting(false)`:** stop the snapshot (≤ 0.2 s release) and resume the buses.
- Wire the existing `Paused(bool)` the same way. Today nothing listens to it, so the Esc pause quiets nothing (`03 §9`).
- `AudioListener.pause` stays forbidden (AUDIO_CONTRACT rule 2).

### 4.4 Input lock and resume safety

- **Locked during Inspect:** move, sprint, the world's E, settings, restart. Only Look (pan, turn), Zoom, Use, ShotBack, Pause and Back are read.
- **No time jump.** Game time does not advance at scale 0, so the first frame after has an ordinary `deltaTime` (capped at 0.1 s by `Update`). Nothing uses `realtimeSinceStartup` for gameplay; G14 uses it for registration only.
- **No dropped or doubled key events.** Held keys are read live on the exit frame (§2.5 H). The exit E is swallowed by the ending shot. The E latch needs a release before E acts again. Two mouse settle frames.
- **No double entry.** No prompt while the Inspect shot is ending (`rig.InShot`), plus a 0.3 s unscaled cooldown.

### 4.5 The pause cannot be used to cheat the Relay

| Way to cheat | Why it fails |
|---|---|
| Freeze a chase to think | The gate: no prompt in sight, lock-on, chase or BreakDoor (§2.5 C). The press frame re-checks the gate |
| Bank free movement | The body cannot move at scale 0. Control returns with the world (`Closing` is live) |
| Recover stamina or let a noise fade | Every regen and decay runs on game dt, which is frozen |
| Peek with the close-up camera | The pose is a square-on view of a face the player already sees (the aim box must be hit first), ≤ 1.3 m ahead. Pan is limited to the framed rect plus margin. The object kind does not move the camera |
| Stall the director or spotting meter | Both run on game time (a rule for the map chat, §4.2) |
| Spam E | Each cycle costs 0.40 s of live time (the return; 0.30 s cooldown at Camera motion Off), and gains nothing |
| **The invariant** | **Pause neutrality:** for the same seed and the same input script, the world state after "open, wait N s, close" equals the state after "open, close at once". This is AT-01g |

---

## 5. UI (平面视觉 designs the type and graphics; the map chat builds the HUD)

### 5.1 Prompt and hint

- **Prompt:** one line in the existing prompt slot ("Key prompt", Bayon 20 px desktop, 17 px handheld). The text comes from the target. It keeps the `E  ·  ` prefix so touch strips it (`03 §7`, `§8`).
  - Proposed: `E  ·  READ` for flat targets, `E  ·  LOOK` for panels and objects.
  - In the key-pickup window: `E  ·  LOOK` beside the held key, for the first key of a run only.
- **Inside the close-up:** no permanent UI.
  - One hint line in the prompt slot, for the **first 2 close-ups per session**: `E  ·  BACK` plus `SCROLL  ·  CLOSER` (only when a zoom exists at this resolution) plus `DRAG  ·  TURN` (object kind).
  - It fades in over 0.3 s and out after 3 s, on unscaled time. No blinking.
  - Minimal HUD: `UI_SYSTEM.md` is world-first; Lethal Company prints its exit key (`02 §3.19`).
- **Hidden in Inspect:** crosshair, prompt, hold bar, threat, key, room panels, hint card, captions. Captions and the hint card resume with their game-time remainders.
- **Pause card:** one new line, e.g. `E  READ  (PAUSES)`. 平面视觉's words.

### 5.2 Transcript (accessibility, off by default)

- **The setting:** `Text transcripts: Off / On` (CONTRACT C24). SH2 (2024) shows players expect enlargeable transcripts (`02 §3.4`, P10).
- **When it is on:**
  - The framing reserves the right 36 % of the screen (`FreeScreenRect`). Flat targets move into the left 62 %. The placard's default composition already does this, so its transcript sits over the wall area.
  - The panel shows the printed text only, in reading order by region. Swatches are named, not explained (e.g. `WAY ON: [wallpaper swatch]`), so the legend still teaches by picture. Narrative and 平面视觉 confirm the wording.
  - The size is adjustable (2 steps).
  - Never a 2D replacement for the object, and no journal (`02 P9`).

### 5.3 Reduce flashing

There are no flashes in the close-up. With the setting on:
- exposure adapts with τ 1.2 s, not 0.6 s;
- the hint fades over 0.6 s;
- the placard glow already slews at 0.5/s (placard spec §4.5).

---

## 6. Platforms

| | Desktop (reference) | Mobile (iOS, Android) | WebGL |
|---|---|---|---|
| Logic, poses, timings, pause | as §2–§4 | identical | identical |
| Prompt and enter | `E  ·  READ` / LOOK; E | USE shows a **READ glyph**; tap USE, or **tap the target** (tap-to-use, `Mobile.cs:168-177`, must also call `Find`) | as desktop (or touch on phones) |
| Pan, turn, orbit | mouse | one-finger drag (left or right zone) | as the device |
| Zoom | scroll or RMB | **pinch** (±15 % opens or closes the step) or **double-tap** | as the device |
| Exit | E or S | USE (shows a BACK glyph) or **Android Back** | as the device |
| Pause over the close-up | Esc → card with wash | pause button → card with frost (fine over a still frame) | as the device |
| Look | §3.7 in full: Bokeh DOF on objects, lens, RT reflections | **no DOF**; exposure, vignette, LD and CA kept (uber-pass values, free) | **no DOF**; no lens renderer (spec D16); no RT; exposure, vignette, LD and CA kept |
| Gate | — | `#if UNITY_IOS \|\| UNITY_ANDROID` profile swap | `#if (UNITY_WEBGL && !UNITY_EDITOR) \|\| FRONTROOMS_WEBGL_PREVIEW` profile swap; desktop byte-identical (Red's WebGL-only rule) |
| Haptic | — | one light tick on open (`InspectOpened`) | — |
| Transcript panel | right 36 % | right 36 % of the 874 × 402 safe layout | as the device |

---

## 7. Ownership and API

### 7.1 Visual-owned (us)

**Files:**

| File | What |
|---|---|
| `Assets/Scripts/Rendering/Inspect/FrontRoomsInspect.cs` | static facade: registry, `Find`, `CanOpen`, `Open`, `Tick`, `Close`, `Abort`, events |
| `.../Inspect/FrontRoomsInspectable.cs` | per-instance component: registers in `OnEnable`, leaves in `OnDisable`; Scene-view gizmo of the solved pose frustum (for the Level Designer) |
| `.../Inspect/FrontRoomsInspectFraming.cs` | the pure solver of §3.2 |
| `.../Inspect/FrontRoomsInspectLook.cs` | volume weight, exposure lift, DOF focus, prewarm; one hidden runner's `LateUpdate` |
| `.../Inspect/FrontRoomsInspectDef.cs` | `[Serializable]` data (§3.8) |
| `.../Inspect/FrontRoomsInspectCatalog.cs` | fallback blocks; prompt, hint and transcript string ids (strings approved by 平面视觉) |
| `Assets/Editor/Rendering/Inspect/FrontRoomsInspectTests.cs` | EditMode: solver numbers of §3.3, `Find` order, zero allocations |
| `Assets/Editor/Rendering/Inspect/FrontRoomsInspectLookdev.cs` | captures every inspect kit at the game numbers (`03` V8) |
| `Office/FrontRoomsKitLibrary.cs` | `public InspectDef inspect;` in `Info`; in `Spawn`, `FrontRoomsInspectable.Attach` when `inspect.IsSet` and the instance is not a pile piece |
| `Editor/Rendering/FrontRoomsRenderSetup.cs` | `EnsureInspectPost()`: `FrontRoomsPost_Inspect` and `_InspectObject` (+ the WebGL and mobile variants) |
| `Tools/Blender/frontrooms_kit/kitlib.py` | `def inspect(self, kind, **kw)` → `self.meta["inspect"]`; `box`, `face` converted in the sidecar writer (`:797-825`). A shared pipeline file: the visual chat decides |
| `assets/evac_placard.py`, `interact_door_sign.py`, `interact_door_number_plate.py`, `interact_key*.py`, `interact_key_board.py`, `interact_key_cabinet.py` | `kit.inspect(...)` calls |
| `FrontRoomsPlacardMount` (clone) | the placard registers through `Spawn`; no shot light |

**Signatures:**

```csharp
public enum InspectKind { Flat, Panel, Object }
public enum InspectSound { Paper, Plastic, Metal, Keys }

public static class FrontRoomsInspect
{
    public static bool Enabled = true;          // false under autopilot, baselines, capture harnesses
    public static bool DevTargets;              // test-bed kits (ship: false) register only when true
    public const float SurfaceSlack = .02f;     // a box on a collider face wins up to 2 cm behind the hit

    /// <summary>Nearest available, in-reach, in-angle target whose padded box the ray enters before maxDistance. No allocation.</summary>
    public static bool Find(in Ray ray, float maxDistance, out FrontRoomsInspectable target);
    /// <summary>A pose solves for this eye and its path is clear (cached per target per frame).</summary>
    public static bool CanOpen(FrontRoomsInspectable target, FrontRoomsCameraRig rig);
    /// <summary>Begins ShotKind.Inspect on the rig (fullPose, lockMove, lockLook, cancel None), starts the look. Null if it cannot.</summary>
    public static ShotHandle Open(FrontRoomsInspectable target, FrontRoomsCameraRig rig);
    /// <summary>Hand-off from the key pickup: the held key's root moves to the camera; returned to heldPose on close.</summary>
    public static ShotHandle OpenHeld(Transform display, string kit, Pose heldPose, FrontRoomsCameraRig rig);
    /// <summary>While Phase.Inspect: pan / turn / orbit from look (degrees), zoom steps (+in / −out), toggle.</summary>
    public static void Tick(float unscaledDt, Vector2 lookDegrees, float zoomSteps, bool zoomToggle);
    /// <summary>Starts the return (rig.EndShot over Inspect.BlendOut). The look fades with the shot weight.</summary>
    public static void Close(FrontRoomsCameraRig rig);
    /// <summary>Teardown: drop everything now (restart, OnDestroy, target destroyed).</summary>
    public static void Abort();

    public static bool Active { get; }          // Open → the shot's end
    public static float Age { get; }            // unscaled seconds since Open
    public static float Weight { get; }         // eased 0..1 (post, hint)
    public static FrontRoomsInspectable Current { get; }
    public static string HintText { get; }      // "" when none
    public static string TranscriptText { get; }// "" unless the setting is on
    public static Rect FreeScreenRect { get; }  // viewport rect free for the transcript

    public static event Action<FrontRoomsInspectable> Opened, Closed;
}

public sealed class FrontRoomsInspectable : MonoBehaviour
{
    public string Id { get; }                   // "Kit_EvacPlacard#3"
    public InspectKind Kind { get; }
    public InspectDef Def { get; }
    public Func<bool> Available;                // null = always (door ID: () => map.DoorLocked(door))
    public Transform Display;                   // object kind: what moves (default: this transform)
    public Pose Face { get; }                   // world centre; forward = out of the face; up = text up
    public Vector2 Size { get; }                // world metres (scale applied)
    public string Prompt { get; }               // "E  ·  READ" (catalogue strings)
    public InspectSound Sound { get; }

    public static FrontRoomsInspectable Attach(GameObject instance, string kit, InspectDef def);
    /// <summary>One target over several parts on one face (the door ID: sign + plate).</summary>
    public static FrontRoomsInspectable AttachComposite(Transform face, string id, Bounds localRect, InspectDef def, Func<bool> available);
}

public static class FrontRoomsInspectFraming
{
    public struct Input { public Pose face; public Vector2 size; public float margin; public Rect screen; public float fov, aspect;
                          public Vector3 eye; public float minDist, maxDist, maxAngle, minY, maxY; }
    public struct Result { public Pose pose; public float distance, zoomDistance, angle; public bool ok; }
    public static Result Solve(in Input input, float viewportHeightPx, float texelsPerMetre);
}
```

### 7.2 Map chat (关卡设计): CONTRACT requests

Each request is written against today's lines (`03 §13.1`, 18:47); the map chat re-reads before patching. These are never edits in Red's project from here.

```csharp
// C1  FrontRooms3DGame.cs:31 — append only (the touch playtest reads phases by name)
enum Phase { Title, Playing, Paused, Caught, Inspect }

// C2  events + state (after :53)
/// <summary>A close-up began (true, at the E press) or gave control back (false, as the camera starts home). False on teardown.</summary>
public static event Action<bool> Inspecting;
public static bool InspectingNow { get; private set; }
FrontRoomsInspectable inspectAimed; ShotHandle inspectShot; Phase pausedFrom = Phase.Playing; bool useLatch; float inspectReadyAt;

// C3  UpdateAim (:1259-1277): ask the registry before Describe
var ray = new Ray(eye.position, eye.rotation * Vector3.forward);
var hitAny = Physics.Raycast(ray, out var hit, Reach, ~0, QueryTriggerInteraction.Ignore);
inspectAimed = null;
if (FrontRoomsInspect.Find(ray, hitAny ? hit.distance + FrontRoomsInspect.SurfaceSlack : Reach, out var t) && CanInspect(t))
{ inspectAimed = t; prompt = t.Prompt; }
else if (hitAny) { /* today's Describe / glass block, unchanged */ }

// C4  the gate
bool CanInspect(FrontRoomsInspectable t) =>
    FrontRoomsInspect.Enabled && mapPlay && phase == Phase.Playing && !useLatch && Time.unscaledTime >= inspectReadyAt
    && (rig == null || !rig.InShot) && (glassShot == null || !(glassShot.Active || glassShot.Blending))
    && climbTime < 0f && !pullClear.HasValue && !RelayBlocksInspect()
    && FrontRoomsInspect.CanOpen(t, rig);
bool RelayBlocksInspect() => relay != null && relay.Released
    && (relay.SeesPlayer || relay.State == HunterState.Chase || relay.State == HunterState.BreakDoor /* v2: || relay.LockOnHold */);

// C5  E on a target (:1292, before `if (aimed == null)`)
if (inspectAimed != null) { holdProgress = 0f; if (pressed) BeginInspect(inspectAimed); return; }

void BeginInspect(FrontRoomsInspectable t)
{
    if (!CanInspect(t)) return;                         // re-check on the press frame
    ReleaseGlass(); holdProgress = 0f;
    inspectShot = FrontRoomsInspect.Open(t, rig);
    if (inspectShot == null) return;
    SetPhase(Phase.Inspect);                            // timeScale 0 (C8)
    InspectingNow = true; Inspecting?.Invoke(true);
    Event("inspect", t.Id);
}
void EndInspect()
{
    FrontRoomsInspect.Close(rig);
    SetPhase(Phase.Playing);                            // timeScale 1, settle frames
    InspectingNow = false; Inspecting?.Invoke(false);
    useLatch = true;                                    // cleared in UpdateMapPlay when !EHeld()
    inspectReadyAt = Time.unscaledTime + FrontRoomsShotTimings.Inspect.Cooldown;
}

// C6  Update routing (:2127-2131), before the Esc toggle
if (phase == Phase.Inspect && !displaySettingsOpen)
{
    if (input.PauseDown) { pausedFrom = Phase.Inspect; SetPhase(Phase.Paused); }
    else if ((input.UseDown || input.ShotBackDown) && FrontRoomsInspect.Age >= FrontRoomsShotTimings.Inspect.CancelAfter) EndInspect();
}
// the Esc toggle resumes to where it paused from:
else if (input.PauseDown && (phase == Phase.Playing || phase == Phase.Paused))
{ if (phase == Phase.Playing) { pausedFrom = Phase.Playing; SetPhase(Phase.Paused); } else SetPhase(pausedFrom); }

// C7  tick while inspecting (:2153, beside the Playing branch)
if (phase == Phase.Inspect)
{
    var u = Time.unscaledDeltaTime;
    FrontRoomsInspect.Tick(u, input.Look, input.Zoom, input.ZoomToggleDown);   // C-T3 adds Zoom fields
    rig.Tick(u);
}

// C8  SetPhase (:2037-2106)
Time.timeScale = p == Phase.Inspect || (p == Phase.Paused && pausedFrom == Phase.Inspect) ? 0f : 1f;
// overlay: no wash in Inspect (:2054); cursor locked in Playing and Inspect (:2075); no pause copy in Inspect;
// panels and crosshair hidden in Inspect; settle frames when Inspect → Playing (:2093).
// C8b (optional, recommended): Paused from Playing also sets timeScale 0 (closes the Esc noise leak).

// C9  R (:2136): && phase != Phase.Inspect
// C10 focus and app pause (:2107-2121): Inspect → { pausedFrom = Phase.Inspect; SetPhase(Phase.Paused); }; Playing → pausedFrom = Phase.Playing first
// C11 UpdateHud (:2170-2219): Inspect hides prompt, hold bar, hint card, captions; shows FrontRoomsInspect.HintText
//     and TranscriptText in 平面视觉's styles (named so UiFont picks the right face, :1482-1487)
// C12 OnDestroy (:2303-2321): Time.timeScale = 1f; if (InspectingNow) Inspecting?.Invoke(false); FrontRoomsInspect.Abort();
// C13 Mobile.cs:187-190: phase == Phase.Inspect ? MenuState.Inspect : … (before the Caught fall-through)
// C14 Mobile.cs:213-220: USE visible when inspectAimed != null; kind UseKind.Read
// C15 Mobile.cs:127-134: Back in Inspect → EndInspect(); Mobile.cs:168-177 tap-to-use also tries FrontRoomsInspect.Find
// C16 FrontRoomsCameraRig.cs:6: append ShotKind.Inspect
// C17 FrontRoomsCameraRig.cs:26-44, 314-326: ShotSpec.fullPose — pose and FOV not scaled by Camera motion; at motion 0, blendIn = blendOut = 0
// C18 FrontRoomsCameraRig.cs:47-58: public float Weight => weight;
// C19 FrontRoomsShotTimings.cs: add
public static class Inspect
{
    public const float Reach = 1.6f, ApproachAngle = 60f, ReachHysteresis = .1f, AngleHysteresis = 5f;
    public const float TravelBase = .30f, TravelPerMetre = .12f, TravelMin = .40f, TravelMax = .55f; // cubic in-out
    public const float BlendOut = .40f;          // = MinFovChangeSeconds; control returns at its start
    public const float CancelAfter = .2f, Cooldown = .3f;
    public const float FovFlat = -15f, FovObject = -10f, MinSurface = .15f, MaxRise = .10f, MaxDrop = .55f;
    public const float ZoomSeconds = .25f, PanSeconds = .12f, TurnSeconds = .08f;
    public const float AdaptTau = .6f, AdaptTauCalm = 1.2f, AdaptMaxEv = 1f;
    public const float HeldWindowFrom = .15f;   // E accepted in the key-pickup hold from here to PocketStart
}
// C20 autopilot (:2427-…): FrontRoomsInspect.Enabled = false while autopilot or a baseline runs
// C21 D1.5 key pickup: an E in [Inspect.HeldWindowFrom, KeyPickup.PocketStart) → end the KeyPickup rig shot with
//     blendOut 0, freeze the key timeline (game time does it), FrontRoomsInspect.OpenHeld(keyRoot, kit, heldPose, rig),
//     then SetPhase(Inspect) as in BeginInspect
// C22 D1.5 door dressing: FrontRoomsInspectable.AttachComposite(leafFace, "DoorID", signPlateRect, def, () => door.Locked)
// C23 optional: the Level Designer walker (FrontRoomsMapWalker.cs:88-150) uses the same Find / Open, so poses preview in the editor
// C24 FrontRoomsSettings: TextTranscripts (bool, false); optional CloseUpsPause (bool, true) only if Red wants mode C
```

### 7.3 Sound chat (声音设计)

| Item | What |
|---|---|
| Hooks to listen to | `FrontRooms3DGame.Inspecting(bool)`; `FrontRoomsCameraRig.ShotStarted/ShotEnded(ShotKind.Inspect, focus)`; `FrontRoomsInspect.Opened/Closed(FrontRoomsInspectable)` with `.Sound` (Paper, Plastic, Metal, Keys) |
| Snapshot | `snapshot:/Inspect` (§4.3). The same handling for the existing `Paused(bool)`. Esc inside a close-up fires `Paused(true)`, and resuming fires `Paused(false)` while `Inspecting` stays true, so the Inspect snapshot must stay on until `Inspecting(false)` |
| Foley (proposed names) | `event:/Foley/Player/InspectIn` (a cloth lean-in, on `Opened`); `event:/Foley/Player/InspectOut` (on `Inspecting(false)`); `event:/Foley/Player/KeyTurn` (key and ring jingle while an object of `Sound = Keys` turns, using the existing `AngularVelocity` parameter). These play on the **unscaled** side of the snapshot |
| AUDIO_CONTRACT.md | add `ShotKind.Inspect`, `Inspecting`, `InspectSound`; state "ShotKind is append-only" |
| Not allowed | new AudioSources, `AudioListener.pause` |

### 7.4 Touch session (`Assets/Scripts/Input/*`)

| # | Where | Change |
|---|---|---|
| T1 | `FrontRoomsTouchControls.cs:53` | `MenuState.Inspect` (append). Routing in `Begin` (`:668-720`): one-finger drag (either zone) → Look; pinch → Zoom; double-tap → ZoomToggle; USE → Use (exit); pause button stays; no stick, no sprint socket |
| T2 | `:55` + `FrontRoomsTouchSprites` | `UseKind.Read` (append) and its glyph (drawn by 平面视觉); a BACK glyph on USE while in Inspect (`TickUse :546`) |
| T3 | `FrontRoomsInput.cs` | a new `Zoom` action: mouse scroll y (one notch = ±1 step), RMB press = toggle. `FrameSnapshot` gains `float Zoom; bool ZoomToggleDown;` (ctor params at the end, defaulted), filled by the physical, virtual (pinch) and editor sources |
| T4 | `FrontRoomsTouchControlsView.cs:701` | Inspect is not `Washed` (no frost over the close-up) |
| T5 | `FrontRoomsMobileInteractionEvents.cs` | `InspectOpened()` → one light haptic |

### 7.5 平面视觉

1. **Verbs:** READ / LOOK (or one verb for all), with the `E  ·  ` prefix. The first-key line beside the held key.
2. **The hint line** (§5.1): words, and whether it shows at all.
3. **The transcript panel** (§5.2): face, size steps, contrast, placement in the right 36 %, and the placard's transcript text (with narrative; strings from A.12).
4. **The touch READ glyph and BACK glyph**, in the HUD key-glyph style (section 2532:4038).
5. **The pause card line.**
6. **Export the placard's readable regions** (rects in sheet mm and smallest cap) from `placard.swift`'s layout constants, for the sidecar.
7. **Optional:** raise the 4.2–4.4 mm lines if Red finds the 1080p base framing small (§3.4).
8. **The door number plate's digit cap ≥ 7.6 mm** (door ID framing, §3.3), and `Prop_KeyTagNo` type at 4096² (we build the texture; they own the type).

### 7.6 Unclear owner: the CRT screen ads (`Media/FrontRoomsScreenVideo.cs`)

- **The problem.** Its private E path (its own ray from `Camera.main`, triggers on, `Keyboard.current.eKey`) would toggle a monitor during a close-up or the Esc pause. In a close-up the rendered camera sits right in front of whatever is framed (`03 §0.8`).
- **The minimum patch**, by whoever owns the file:
  - subscribe to `Paused` and `Inspecting`;
  - skip the ray and E while either is true;
  - pause every `VideoPlayer` on true and resume on false. Its default clock at scale 0 is UNVERIFIED, so do it explicitly.
- **Later (L1):** the power toggle moves onto the shared aim path, and the screen becomes a `panel` target.

---

## 8. Budgets and acceptance tests

### 8.1 Budgets

| Item | Budget | Basis |
|---|---|---|
| `Find` per frame | ≤ **0.02 ms** with 64 live targets (ray vs padded OBB, no allocation) | ESTIMATE. Live targets: 1 placard + keys + door IDs + hosts in built chunks |
| Solve + path check | ≤ **0.02 ms** per aimed frame (1 spherecast; cached) | ESTIMATE |
| `Tick` + look driver | ≤ **0.05 ms** | ESTIMATE |
| GC | **0 B per frame** while aiming and inspecting | |
| GPU, flat or panel close-up | **≤ +0.1 ms** over play at the same view (only uber-pass values change) | ESTIMATE |
| GPU, object close-up (desktop Bokeh DOF) | **≤ +1.0 ms** at 1440p | ESTIMATE; measure |
| GPU, WebGL and mobile | **+0 passes** | |
| Hitches | no frame > **1.5 × the median** of the previous 60 frames during entry or exit | prewarm (§3.7) |
| Memory | `Prop_KeyTagNo` at 4096² BC7 ≈ +16 MB on desktop (WebGL import tab capped at 1024); no new render targets except URP's DOF buffers | ESTIMATE |
| Data | ≤ 1 KB per sidecar block | |

### 8.2 Acceptance tests

They run in a clone (`W/proj_inspect` = `proj_audit` + the Q16 placard work + the inspect code + the map, touch and sound contract diffs applied in the clone only). Every capture goes to the VERIFICATION LOG section 2595:6093 per `VERIFICATION_LOG.md`.

**AT-01 Placard in the start area (Q16).**

| # | Check | Pass |
|---|---|---|
| a | 100 seeds: the placard registers | 100 / 100 |
| b | A pose solves and its path is clear, from a stand 1.5 m square-on | 100 / 100 (legend side or centred fallback); stand-off to colliders ≥ 0.15 m |
| c | Prompt range | shows at 1.6 m and 60°; not at 1.7 m or 65° (with hysteresis) |
| d | Entry | `timeScale == 0` on the E frame; the camera arrives in 0.44 ± 0.02 s unscaled; FOV 61.0 ± 0.1; the frame fills 62 ± 2 % of the width and 74 ± 2 % of the height (composition A) |
| e | Type, measured on 1080p, 1440p and 2160p captures | matches §3.4 within ±10 %; every line ≥ 12 px after zoom |
| f | Freeze over a 10 s close-up | lamp levels of all built cells, Relay position, state and `StateTime`, door angles, glow emission, `elapsed` and the tier clock are unchanged; no `DoorMoved`, noise, `FixtureChanged` or `LampDipped` events |
| g | **Pause neutrality** | same seed and autopilot input script; a 10 s vs a 0.2 s close-up at t = 20 s → equal world state hash at t = 60 s game time |
| h | Exit | control on the E frame; `timeScale == 1`; the first gameplay `deltaTime` ≤ 0.1 s; the exit E opens no door and no new close-up; the camera is home in 0.40 s; `ShotEnded(Inspect)` once |
| i | Camera motion 0 / 50 / 100 | the same pose at weight 1; motion 0 = a cut both ways |
| j | Dark cell (a Dead lamp) | exposure lift +1.0 ± 0.05 EV; the linear (pre-tonemap) glyph-to-paper ratio within 2 % of the ratio without the lift; the displayed ratio not lower than without it |
| k | Grain at scale 0 | moves, or is reduced to 0.10 (§3.7) |
| l | Esc and R | Esc in the close-up → card, scale 0, Esc → the same close-up; R ignored in Inspect, works from the card |

**AT-02 Key + tag** (a D1.5 clone).
- E in the hold window → key centred at 0.20 ± 0.01 m, FOV 66.
- Turn: yaw 360°, pitch ±80°. Digits on the tag ≥ 29 px cap at 1080p (at a 7 mm digit cap; ESTIMATE).
- Texture magnification ≤ 1.3× at 1080p (≤ 1.0× with the 4096² atlas at 1440p).
- Exit → back to the held pose; the pocket beat plays; `KeyTaken` fires exactly once; the tag number and colour equal the plate of the matching door.

**AT-03 Door ID** (D1.5).
- Locked door: the sign or plate patch gives READ, the rest of the leaf gives LOCKED and the rattle.
- After unlocking, OPEN wins everywhere.
- Pose 0.58 ± 0.02 m, camera height 1.27 ± 0.02 m. Plate digits ≥ 12 px at 1080p.

**AT-04 Test bed today** (`DevTargets` on, main via a clone):
- **Clock:** pitch 40 ± 1° up, camera height = eye + 0.10, d 0.59 ± 0.02.
- **Copier console:** text upright, pitch ≤ 65° down.
- **Vending window:** orbit ±20° stays clear of colliders.
- **Desk phone:** object at 0.50 m, turns freely.
- **G14:** with a window in frame, accumulation reaches 32 within 40 frames of arriving, and there are no hitches (§8.1).

**AT-05 Gates.**
- Relay `SeesPlayer`, `Chase` or `BreakDoor` → no prompt, and E opens nothing.
- Warning stages 1–2 → allowed.
- Glass hold, climb or door shot → no prompt.
- Autopilot and harnesses → never opens.

**AT-06 Touch** (touch playtest harness, iPhone profile).
- Tapping the target opens the close-up. Pinch and double-tap zoom. Drag pans or turns. USE and Back exit.
- No frost over the close-up. The pause button gives frost over the frozen frame.
- The Caught card never shows for Inspect.

**AT-07 Budgets.**
- Profiler: `Find` and `Tick` timings and 0 B GC as §8.1.
- Close-up frame time vs play at the same spot: +0.1 ms (flat), +1.0 ms (object, desktop 1440p).

**AT-08 WebGL.**
- The same flow, with no DOF pass.
- The desktop capture is byte-identical before and after the WebGL gating.

**AT-09 Sound** (sound chat; needs FMOD banks synced into the clone, `W/TOOLS.md` §2).
- During the close-up: SFX paused, AMB −10 dB, no Relay steps.
- Everything resumes on exit.

**AT-10 Accessibility.**
- Transcripts on → the placard sits in the left 62 % and the transcript in the right 36 %. Both size steps fit.
- Reduce flashing → exposure τ 1.2 s.

---

## 9. Build order

1. **Visual, now** (clone on main): data classes, solver, registry, look, catalogue. Test bed T1–T4 behind `DevTargets`. EditMode tests. Look-dev captures. Then the contract diffs, applied in the clone only, to prove them: AT-04, AT-05, AT-07.
2. **Map, touch and sound** apply their contracts in main (C1–C20, C24; T1–T5; §7.3).
3. **With Q16:** the placard's sidecar block, regions from 平面视觉, and the composition. AT-01.
4. **With D1.5:** key hand-off (C21), door ID (C22), hosts, and the text slots (`Prop_SignEngraved`, `Prop_KeyTagNo` 4096²). AT-02, AT-03.
5. **Later:** CRT (L1) after 平面视觉 item 3; any SHOULD Red picks; sanctuary placard copies.

---

## 10. Decisions for Red

My default is first in each row. These replace `02 §7` and `03 §15`.

| # | Question | Default | Alternative |
|---|---|---|---|
| R1 | Close-ups in a chase? | **No prompt** while the Relay sees you, locks on, chases or breaks a door. Warnings allowed and frozen | B: always allowed, always paused. C: a "close-ups pause: Off" setting (live) |
| R2 | When does the world restart? | **When the camera starts back** (control at once, 0.40 s live return) | when the camera is home (0.40 s of dead control) |
| R3 | Placard framing | **Your render's frame:** 0.36 m, the plan in the left 62 %, the real wallpaper beside the legend | centred at 0.31 m (+17 % text size, no wall) |
| R4 | Key and tag | **Examine in the hand:** a second E during the pickup hold | a camera dip to the key where it hangs |
| R5 | Door sign + number plate | **One "door ID" close-up, only on locked doors**, E on the sign/plate patch; the rattle elsewhere | read in place only (the sign's 17 mm caps read at about 1 m); no close-up |
| R6 | Which SHOULD props ship? | **None in v1** except the key hosts. Clock, vending, copier and phone are test beds | pick any |
| R7 | Should Esc also freeze time? | **Yes** (map chat, C8b): closes the noise leak | keep today's Esc |

---

## 11. Open items and UNVERIFIED

- URP film grain and `VideoPlayer` behaviour at `timeScale 0` (AT-01k; §7.6).
- My numbers to tune on captures (ESTIMATE): the body height band (−0.55 / +0.10 m), the exposure lift curve, the DOF target, all budgets, the vending and copier region sizes.
- The ≥ 12 px reading line is my threshold, not a published rule (§3.4).
- The plate digit cap and the tag digit cap are not in main yet (P-4 slots). AT-02 and AT-03 depend on them.
- The v2 lock-on hold is not built. The gate names it as a placeholder.
- The touch files were being edited live (`03 §0`, `§14` risk 17). Re-read their lines before the touch patch.
- `UI_SYSTEM.md`'s "hold E reads / Tab opens notes" line is stale against this design (no notes screen, `02 P9`). Flagged for the map chat and 平面视觉, not changed here.

---

## 12. Images (support diagrams, JPG q85; not verification images)

| File | Shows |
|---|---|
| `images/d_01_inspect_flow_timeline.jpg` | The states of §2.3 and the timeline of §2.4: time scale, camera weight, who has control, which events fire |
| `images/d_02_placard_base_vs_zoom.jpg` | Red's reference render as the base close-up (0.36 m, 2.55 px/mm), and the 1080p zoom step (0.18 m, 5.09 px/mm) simulated by cropping and scaling the same render (soft, because the source is 2.55 px/mm) |
| `images/d_03_inspect_poses.jpg` | Side views with numbers: placard 0.36 / 0.18 m, door ID 0.58 m stoop, clock 0.59 m at 40° up, key at 0.20 m; the body height band |
