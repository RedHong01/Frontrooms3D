# 10 — EXAMINE: the close-up inspect system (design)

Status: **DESIGN v1.1, 2026-10-07** (visual chat 游戏视觉). v1.0 (same day) had two critic passes. Every issue is fixed here or answered with evidence in §13. It builds on `01_inventory.md`, `02_game_research.md` and `03_code_survey.md`, cited as `01 §`, `02 §` / `P#` and `03 §` / `M#`.

**Red's request (translated):** every visual asset the player can really look at (example: the Q16 evacuation plan, `research/placard/images/q16_u1_A3_legend_0p6m.jpg`) should be something the player walks up to, interacts with, and sees in a close-up shot. While the close-up is up, the game is paused.

**What was done for v1.1.**
- Re-read in main (read-only, Unity not opened): `FrontRoomsCameraRig.cs` (all 389 lines), `FrontRoomsShotTimings.cs`, `FrontRooms3DGame.cs` (:1067-1093, :1214-1300, :2037-2200, :2303-2330), `FrontRooms3DGame.Mobile.cs` (:127-220), `FrontRoomsMapHunter.cs` (:149-300), `FrontRoomsMapWorld.cs` (:127, :195, :859, :2280, :2712, :2988), `FrontRoomsPostStack.cs`, the two post profiles, `FrontRoomsInput.cs`, `TOUCH_CONTROLS.md` §2-4, `RELAY_PURSUIT_REDESIGN.md` §8-9, the placard spec and build report, and the Blender kit headers (door plate, key tag, cabinet, hook board).
- Checked in the project's URP 17.3 package cache: `PostProcessUtils.cs:48-52, :102-106` and `UniversalRenderPipelineGlobalSettings.asset:22`.
- Measured glyph-to-paper contrast on Red's render (§3.4).
- Files written: this doc and one line in `SOURCES.md` §5. No new images. Figma was not touched. Nothing was downloaded.

**Names.** The feature is **EXAMINE**. In code it is `Inspect` (`Phase.Inspect`, `ShotKind.Inspect`, `FrontRoomsInspect`). The words the player sees are 平面视觉's (§5).

**Tags:** **ESTIMATE** (my number, no source or run), **UNVERIFIED** (not confirmed), **CONTRACT** (an exact change for a file another chat owns), **DECIDE** (Red's call; my default is given).

---

## 0. The short version

1. **The shot type: the camera goes to the object, in a paused world** (`02 §4.1`). It is a real camera pose, not a 2D overlay, because only a real pose keeps the frame, lens, wallpaper, lamp light and glow ink (`02 §0.4`).
2. **Three kinds, one system.**
   - `flat`: wall prints and signs. The camera goes there, square-on.
   - `panel`: the face of a big fixture. The camera goes there, with a small orbit.
   - `object`: hand-sized things. The object comes to the camera and turns (`02 P2`).
3. **v1 targets:**
   - the placard (first, with Q16);
   - the key with its tag, examined in the hand on the **first key of a run** (with D1.5).
   - **The door sign and number plate read in place** (no close-up; v1.0's "door ID" is dropped, §1.3).
   - The key hosts are SHOULD, because their hint is not designed yet.
   - Four live props are the test bed: the clock, vending machine, copier and desk phone.
4. **Pause = `Phase.Inspect` + `Time.timeScale = 0`**, from the E press until the camera starts home. Control returns at once. Rotation is home in **0.15 s** and position in **0.40 s**, both with an ease-out.
5. **The placard framing is Red's own frame** at 16:9: 0.36 m, the plan in the left 62 %, the real wallpaper on the right. On narrower screens the distance grows until the frame fits (0.393 m at 16:10). Below aspect 1.50 the shot switches to a centred composition (§3.2).
6. **Readability is angular**, so it holds on every screen. A cap reads at **≥ 18.6 arcmin**, which is 12 px on the 1080p reference monitor.
   - One zoom step brings the smallest line of the region to 18.6′. It dollies to 0.18 m. On phones, tablets and anything still short, it then narrows the FOV.
   - The texture is never magnified past 2.0×.
7. **Timing:** in 0.40–0.55 s on a sine ease in-out (peak 4.3 m/s, was 8.2 m/s); out as in item 4; Camera motion 50 % = ×1.5 times; Off = a hard cut.
8. **Light: no added lamp.** In a dark cell the close-up lifts exposure by up to +1.0 EV, and adds a little contrast so the glow legend reaches a displayed ratio of 1.30. The lift ramps in and out on its own τ (0.6 s; 1.2 s with Reduce flashing). The lit-state legend stays faint, as the placard spec intends.
9. **Chase rule:** no prompt while the Relay chases, breaks a door, or the player can see it. Only cues the player can perceive count; the silent `SeesPlayer` is never used. Stage 1–2 warnings still allow a close-up and freeze with the world. **DECIDE**.
10. **Esc leaves a close-up** (it does not open the pause card over it). Focus loss still pauses over the close-up.
11. **Hit targets** come from a visual-owned registry of boxes, tested against the aim ray after the physics ray. No colliders and no layers.
12. **Contracts:** map chat C1–C26 (§7.2); the rig and timing changes C16–C19 are small, additive and land first. Touch 7 (T1–T7), sound 5 (S1–S5), 平面视觉 9.
13. **The test that matters most is pause neutrality** (AT-01g). Run with recorded input at a fixed 1/60 step: a 10 s close-up and a 0.2 s close-up must give the same gameplay-state hash afterwards.

---

## 1. Scope

### 1.1 v1 targets (MUST)

| # | Target | Kind | Today | Lands with | Notes |
|---|---|---|---|---|---|
| E1 | **Evacuation placard** `Kit_EvacPlacard` (01 M1) | flat | built in `W/proj_placard`, not in main | Q16 promotion | The showcase. 1 per run at the start door; sanctuary copies later (`RELAY_PURSUIT_REDESIGN.md` §10) |
| E2 | **Zone key + ring + tag** (01 M4) | object | key is a cube; kits in main, unspawned | D1.5 | Examined **in the hand**, on the first key of a run only (§2.5 row K) |

### 1.2 SHOULD, later, and the test bed

| # | Target | Kind | Use now | Ship? |
|---|---|---|---|---|
| S6 | Key hosts `Kit_KeyHookBoard`, `Kit_KeyCabinet` (01 S6) | flat | none until the hint exists. Hook numbers 01–08 (board) and 01–24 (cabinet) do not relate to the 00–99 tag, so a close look teaches nothing yet | DECIDE (default no). The cabinet's 4.6 mm label digits need the zoom (§3.3), or 平面视觉 raises them to ≥ 6 mm |
| T1 | Wall clock `Kit_WallClock` (01 S5) | flat, hung at 2.1 m | **test bed:** the look-up pose | DECIDE (default no) |
| T2 | Vending machine `Kit_VendingMachine` (01 S2) | panel (`window` anchor) | **test bed:** a big fixture with a collider | DECIDE; 平面视觉's $1.45 era check |
| T3 | Copier `Kit_Copier` (01 S3) | panel (`console` faces up) | **test bed:** a face that looks up | DECIDE |
| T4 | Desk phone `Kit_DeskPhone` (01 S4) | object | **test bed:** the object kind before the key exists | DECIDE |
| L1 | Office CRT monitor (01 S1) | panel (`screen`) | no: its E is the power toggle | after 平面视觉 item 3 (LEVITZ must go) and C26 |
| L2 | CRT TV in piles (01 S7) | panel | no: piles turn and invert it | later |
| L3 | A memo or page on `Kit_PaperStack` / `Kit_Binders` | object | no: the pages are blank | if narrative writes one |

Test-bed and SHOULD targets carry `ship: false`. They register only when `FrontRoomsInspect.DevTargets` is on (Editor and dev builds).

### 1.3 Not inspectable: read in place

**The rule for every asset.** A close-up is offered only when both are true:
- the asset carries text or detail that does **not reach 18.6 arcmin** (12 px on the 1080p reference, §3.4) at **0.8 m**, one step inside the prompt range;
- it is **rare** (P11). Control's sequel was faulted for "reading breaks" (`02 §3.14`).

Every inspectable also needs something that reads from the prompt range, so the player knows it is worth the walk (P1). For the placard that is the red bar and the frame shape at 3 m. Its 15 mm title reaches 12 px at 0.86 m: 10.4 px at 1.0 m and 6.5 px at 1.6 m.

**Read in place (no close-up):**

| Asset | Size | Desktop reference | Phone (×0.45, §3.4) | Why |
|---|---|---|---|---|
| **Door sign** (`interact_door_sign.py`) | 17 mm cap at 1.524 m | 11.7 px (18.3′) at 1.0 m; 14.7 px (22.8′) at 0.8 m | 18.4′ at 0.45 m | Reads by stepping up. A close-up would take over the locked-door verb where players aim (v1.0's door ID; §13 A6) |
| **Door number plate** (`interact_door_number_plate.py:10`) | 27 mm digits at 1.000 m | 18.7 px (29.0′) at 1.0 m; 12 px out to 1.56 m | 21.9′ at 0.6 m | Built to read at ≤ 2.7 m by design |
| Scratch-throughs (01 M5) | letters 25–35 mm | 17–24 px at 1 m | — | Also on hold (Q2) |
| EXIT sign | one word | readable at 20 m | — | |
| Everything in `01 §4` (about 90 kits) | — | — | — | Nothing to read, the same on every desk, or it has its own shot (doors, glass) |

A player can stand 0.45 m from a door leaf (body radius 0.30 m), so the phone distances are reachable.

---

## 2. Flow and states

### 2.1 What the player does

1. **Approach.** The frame and red bar read from 3 m. The title reads at about 0.9 m.
2. **Prompt.** One line in the existing prompt slot, like the door prompts.
3. **Enter.** E (desktop), or USE or a tap on the target (touch). The world freezes on that frame.
4. **Close-up.** The camera travels to the framed pose in 0.40–0.55 s. The HUD fades in 0.15 s.
5. **Read.** Optional: a small look (±3°), one zoom step, pan while zoomed, turn an object. A transcript if the setting is on, or the TEXT chip on touch.
6. **Exit.** E, S, Esc, Android Back, touch BACK or a stick pull-back. Control returns at once. Rotation is home in 0.15 s, position in 0.40 s.

### 2.2 When the prompt appears

These **hard** checks hide the prompt. They run every frame from `UpdateAim` (CONTRACT C3–C4).

| Check | Value | Why |
|---|---|---|
| The aim ray from `BaseEye` hits the target's **aim box** before any collider | box hit ≤ physics hit + 0.02 m | The box sits on a wall face, so it must beat the surface behind it. A door or furniture **in front** still wins (`03 §10.3`) |
| Distance from the eye to the box hit | ≤ `reach`, default **1.6 m** (door reach stays 2.4 m) | Red: "walk up to". At 1.6 m the sheet is recognisable (frame, red bar); its title reads at about 0.9 m |
| Eye angle off the face normal | ≤ `approachAngle`, default **60°** | No reading from edge-on |
| `Available()` | true (default) | per-target condition (the key hand-off uses its own gate, C21) |
| A pose solves and its path is clear | §3.2 | Nothing is ever framed through a column or a leaf |
| Game state | `CanShowInspect` (C4): phase Playing, map play, no harness, no glass shot, no climb, no pull-clear, **no Relay block** (§2.5 C) | |

These **transient** checks (`CanPressInspect`, C4) keep the prompt but **swallow E**, so E never falls through to the door or wall behind:
- the E latch (E not yet released since the last exit);
- the 0.30 s cooldown;
- `rig.InShot` (any shot still running, including the Inspect return).

**Aim forgiveness.** Each box is padded by `aimPad`, 0.02 m by default. Small things get at least 0.08 m: the key ring (58 mm) is 2.2° wide at 1.5 m.

**Hysteresis.** Once the prompt shows, it stays until 1.7 m or 65°.

### 2.3 States

| State (game phase) | Time | Camera | Input | Leaves to |
|---|---|---|---|---|
| **Playing**, target aimed | scale 1 | `BaseEye` | normal; prompt shown | E (hard and transient checks pass, re-checked after `relay.Tick`) → Opening |
| **Opening** (`Inspect`, shot weight < 1) | **scale 0** | blends in on the inspect clock | E, S, Esc, Back swallowed for the first 0.20 s; after that they exit | weight 1 → Holding; exit input → Closing; focus loss → Paused |
| **Holding** (`Inspect`, weight 1) | **scale 0** | the framed pose plus look, pan, zoom, turn | Look; Zoom; E, S, Esc, Back, BACK, pull-back exit | exit input → Closing; focus loss → Paused |
| **Paused from Inspect** (`Paused`, `pausedFrom = Inspect`) | **scale 0** | held | the pause card as today. Reached only by focus loss or app pause | Esc, Android Back or the touch resume → the same close-up (`ResumeFromPause`); R → restart |
| **Closing** (`Playing`, shot ending) | **scale 1** | rotation home in 0.15 s, position and FOV in 0.40 s, on game dt | **full control**. E and S latched until released. New close-ups refused until the shot ends | shot ends → Playing; the Relay seen → snaps home in 0.15 s |

The v1.0 diagram `images/d_01_inspect_flow_timeline.jpg` still shows the state chart. Its deltas are listed in §12.

### 2.4 One close-up, in numbers (placard, player 1.5 m from the wall)

`t` is on the inspect clock (§3.5): unscaled seconds, or `captureDeltaTime` steps under a harness.

| t (s) | What happens |
|---|---|
| 0.00 | E down. `UpdateAim` marks the target pending. After `relay.Tick`, the gate is re-checked (C5b). Any glass hold is released. `FrontRoomsInspect.Open` solves the pose and begins `ShotKind.Inspect`. `SetPhase(Inspect)` sets `timeScale = 0`. `Inspecting(true)` and `ShotStarted(Inspect, focus)` fire. The sound snapshot starts (§4.3). The rig ticks once this frame, inside `UpdateMapPlay` |
| 0.00–0.15 | The HUD fades out through `rig.HudFade` (C8 treats Inspect as a HUD phase) |
| 0.00–0.44 | The camera travels about 1.2 m on a sine ease in-out (peak 4.3 m/s). FOV 76 → 61. The shot volume follows the eased weight |
| 0.20 | From here E, S, Esc or Back exits |
| 0.44 → | Holding. In a dark cell the adapt volume ramps with τ 0.6 s. The glass RT converges in about 32 frames if glass is in view |
| exit | `FrontRoomsInspect.Close` → `rig.EndShot`. `SetPhase(Playing)` sets `timeScale = 1`. `Inspecting(false)` fires. The E latch is set, plus the S latch if S exited. The player can move and look now |
| exit + 0.15 | Rotation is home |
| exit + 0.40 | Position and FOV are home. `ShotEnded(Inspect)` fires. A new close-up may open; the 0.30 s cooldown has already passed. The adapt volume keeps decaying on its own τ |

### 2.5 Edge cases

| Row | Case | What happens | Source |
|---|---|---|---|
| **C** | **The Relay** | **No prompt while the player can perceive the threat.** Today (main): `Released && (State == Chase \|\| State == BreakDoor \|\| PlayerSeesRelay)`. In main, `SeesPlayer` turns into `Chase` on the same tick (`FrontRoomsMapHunter.cs:265-283`), and Chase is audible. v2, when it lands: `OnMap && (WarnStage >= 3 \|\| Spotting >= .5f \|\| PlayerSeesRelay \|\| State == BreakDoor)`. Stage 3 is latched from `TargetAcquired` to the end of the chase, and the chime is its cue. This mirrors the rig rule "shots cancel on `TargetAcquired` or `Spotting ≥ 0.5`, not on any glimpse" (`RELAY_PURSUIT_REDESIGN.md` §8). **Never `SeesPlayer`:** in v2 it is silent, so a vanishing prompt would work as a Relay detector. With no prompt, E does what the aim would do without the target (usually nothing on a wall). *Alternatives:* B, always allowed, always paused; C, a setting "Close-ups pause the game: Off" (time runs; the Relay ends the close-up). **DECIDE** | `02 P4`, `§3.6`, `§3.13`; `RELAY_PURSUIT_REDESIGN.md` §8–9 |
| **W** | **Warning, stages 1–2** (lamps flicker; muffled steps) | **Allowed, and fully frozen.** A lamp caught mid-dip stays dark, and the Relay stands where it was. The sound chat is asked to keep the presence hum at its frozen gain −6 dB (S1), so the threat stays present with no new information. After exit the warning carries on from the same point | `RELAY_PURSUIT_REDESIGN.md` §9; `02 §3.9` |
| **K** | **The zone key in the hand** (D1.5, first key of a run only) | The KeyPickup shot (`FrontRoomsShotTimings.KeyPickup`) is not built yet; D1.5 builds it. On the **first key of a run**: <br>• The held pose **holds** at `HoldUntil` until the player moves (stick or keys ≥ 0.2) or looks (≥ 2°), or for 2.0 s at most. <br>• `E · LOOK` shows in the **normal prompt slot**. <br>• E opens the close-up from `Arrive` (0.25 s) to the end of the hold, but only after E has been released since the take (E latch) and only through `CanInspectHeld()` (C21; the same Relay, glass and climb checks). <br>• The map ends its KeyPickup rig shot with no blend, and `FrontRoomsInspect.OpenHeld` moves the key to 0.20 m. <br>• On exit, `FrontRoomsInspect` alone moves the key back to the held pose (0.40 s, inspect clock). The map **holds its key timeline until `HeldReturned`**, then plays the pocket beat. One writer at a time. <br>• `KeyTaken` has already fired at Arrive. <br>**Later keys:** no hold and no hand-off (P11). The HUD already shows the number. **DECIDE** | `FrontRoomsShotTimings.KeyPickup`; `02 P2` |
| **D** | **Door sign and plate** | **Read in place** (§1.3). The door verbs are unchanged on the whole leaf. If Red wants a close-up anyway, the alternative is in §7.2 C22 (sign only, after one rattle, reach 1.2 m) | §13 A6, B2 |
| **H** | **Keys held across enter and exit** | Move, look and sprint are not read while inspecting (`UpdateMapPlay` does not run). On the exit frame W held walks and Shift held sprints; nothing is queued. **The exit E** is swallowed: `UpdateAim` sees the ending shot, and `rig.Consume` returns true. The E latch then needs a release. **The exit S** is latched too: backward movement is ignored until S (or the stick pull-back) is released, so the player does not walk backward during the return. Mouse look moves the base view at once. Two mouse settle frames after any move from Paused into Playing or Inspect | `FrontRoomsCameraRig.Consume` |
| **G** | Glass hold, glass shot, climb, a door shot running | No prompt for glass, climb or pull-clear (hard). During other shots the prompt can show but E is swallowed (transient). A glass hold whose aim slides onto a placard never opens without a fresh E press (`EDown` is an edge) | `03 §4.2` |
| **E** | **Esc in a close-up** | **Exits**, like E and S, once `CancelAfter` (0.20 s) has passed. The pause card opens only from Playing. Players press Esc to leave (Lethal Company prints `[ESC]`, `02 §3.19`), so no card loop | §13 A9 |
| **R** | R in a close-up | Ignored (R restarts only from the pause or caught card) | `03 §14` risk 1 |
| **F** | Focus lost, app paused, a phone call | → Paused with `pausedFrom = Inspect`. Resume returns to the same close-up | `03 §2.1` |
| **B** | Android Back | In Inspect: exits. In Paused: `ResumeFromPause()` (never a raw `SetPhase(Playing)`) | `Mobile.cs:127-133` |
| **M** | Camera motion 100 / 50 / Off | A full pose at every setting (`fullPose`). At 50 % all blend times are ×1.5. At Off it is a **hard cut** both ways, which SH2 (2024) shows reads as deliberate | `02 §3.4` |
| **U** | The target is disabled or destroyed (a chunk unloads, a Level Designer `LiveApplied` rebuild) | `FrontRoomsInspect` raises `AbortRequested`. The game answers with `EndInspect` + `rig.CancelShot(shot, 0)` (C12c) | §13 B10 |
| **X** | Caught during the 0.40 s return | `End()` calls `FrontRoomsInspect.Abort()` and `rig.ResetLayers()` before Caught (C12b), so nothing freezes mid-blend | §13 B10 |
| **A** | Autopilot, baselines | Never opens (the game gates on its own `autopilot \|\| autoBaseline`, C4). The touch playtest keeps it on, for AT-06 | §13 B21 |
| **T** | Title stream, Caught, settings open | No targets in the title stream; no aim in Caught; settings cannot be open in Inspect (`SetPhase` closes them) | `03 §2.2` |

---

## 3. The shot

### 3.1 Three kinds

| Kind | For | The camera | The object | In the close-up | Family (`02 §4.1`) |
|---|---|---|---|---|---|
| `flat` | wall prints, signs, boards, the clock | goes to a near-square pose (≤ 25° off) | stays | ±3° look at base; one zoom step; pan while zoomed | camera goes to the object (SH2 2024, Lethal Company) |
| `panel` | the face of a big fixture | goes to the face | stays | orbit ±20° yaw, ±10° pitch; one zoom step | same, with a little parallax |
| `object` | hand-sized things (key and tag, desk phone) | stays at `BaseEye`; FOV −10 | comes to 0.20–0.50 m in front of the camera | free turn (yaw 360°, pitch ±80°); one zoom step (0.70 × d) | object brought to the camera (Gone Home, RE) |

A 2D page is never used, because it would drop the period object, which *is* the printed sheet (`02 P10`).

### 3.2 The framing solver (`FrontRoomsInspectFraming.Solve`, pure, unit-tested)

**Inputs:**
- the face: centre `C`, outward normal `N`, text-up `U`;
- `size` (w, h) and `margin`;
- the screen rect for composition A (`screen`, viewport units). On phones it is intersected with `Screen.safeArea`;
- FOV `v = BaseFov + fovDelta`, and the aspect, read **every solve** (the Level Designer walker runs at 72°, `FrontRoomsMapWalker.cs:72`);
- the eye `E` (`BaseEye`);
- `minDist`, `maxDist`, `maxAngle` (vertical tilt), `maxYawOff` (default **25°**), `distMaxA` (default **0.42 m**);
- the body band: camera height in **[E.y − 0.55, E.y + 0.10]** (ESTIMATE);
- the near-plane corner radius `r_c`, from the near plane (0.06 m), FOV and aspect: **0.113 m** at 76° and 16:9, **0.133 m** at 21:9. The largest FOV during travel is used.

**Steps:**
1. **Direction.** Start at `D = N`.
   - Turn `D` toward the eye about world up by `min(approach yaw, maxYawOff)`. The camera's swing is then ≤ 35° at the 60° prompt edge (§3.5). cos 25° = 0.91, so at most 9 % of the text width is lost to foreshortening.
   - If `C + D·d` is outside the height band, tilt `D` toward the eye's side, up to `maxAngle`.
2. **Distance.** The fit test projects the 4 corners of the framed rect through the candidate camera. That works for any aspect, FOV, yaw offset or transcript rect.
   - **Composition A** (`dist[0] > 0`): `d = max(dist[0], d_fit(screen rect, margin 0))`.
     - If `d > distMaxA`, use composition B.
     - For the placard, A holds while the aspect is ≥ 1.50: 0.36 m at 16:9 and wider, 0.393 m at 16:10, 0.408 m at 1.54:1 (14-inch MacBook Pro). It becomes B below that: an iPad 11-inch is 1.44, 4:3 is 1.33.
   - **Composition B** (fallback, or no `dist`): full screen (or `FreeScreenRect` with the transcript on), `margin`, `d = max(minDist, d_fit)`.
3. **Blocked path fallback.** If the path is blocked:
   - try `D` turned ±20° (within `maxAngle`);
   - then composition B;
   - if nothing is clear: no prompt.
4. **Rotation.** Look at `C`. Up is world up projected. When `|N·up| > 0.7` (a face that looks up or down), up is the face's text-up `U`, so the copier's keys read the right way.
5. **Path check.**
   - A spherecast from `E` to the pose with radius `r_c + 0.01` (0.123 m at 16:9) must be clear. The rig's own clamp sphere is 0.10 m, which is smaller than the near-plane corner, so it is not enough on its own.
   - The pose keeps ≥ **0.15 m** from any collider face.
   - The registry's own boxes are tested on the same segment, because render-only objects have no colliders.
6. **Pan and look limits are solved here, not at runtime.** A sweep from the pose to the 4 corners of the pan rect (at the zoom distance) and of the ±3° look cone shrinks each limit to the clear part. So the rig's per-frame clamp never fires during pan, and there is no framing jump.
7. **Object kind.** The object's distance is clamped with `Physics.CheckSphere(centre, boundingRadius + 0.02)` along the view axis. If it cannot reach `minDist` (key 0.15 m), there is no prompt.
8. **Cache.** Results are cached per target. They are invalidated when the eye moves > 1 cm, turns > 0.5°, or the aspect, FOV or safe area changes.

**The rig does the travel.** Each frame it clamps the camera against the world again, as a safety net only.

### 3.3 Per-asset numbers

On the desktop reference: 1080p, 16:9, a 24-inch monitor at 60 cm (§3.4). FOV 61° for flat and panel, 66° for object.
- **px/mm** = 1080 / (2 · d · tan(v/2)) / 1000.
- **d zoom** is the angular zoom of §3.4.

| Target | Framed rect (m) | Composition · d base | d zoom (reference) | Camera height · pitch | Fill (16:9) | px/mm base | Notes |
|---|---|---|---|---|---|---|---|
| **E1 placard**, A (Red's frame) | 0.4668 × 0.3138, in the left 63 % | A · **0.36** (16:9, 21:9, phone 2.17) | **0.184** | 1.524 · 0° | 61.9 % w, 74 % h | **2.55** | The wallpaper to the right of the legend is the real WP03 the legend describes |
| E1 placard, A on 16:10 / 1.54 | same | A · **0.393 / 0.408** (fit) | 0.184 | same | 63 % w | 2.33 / 2.25 | v1.0 clipped 1.9 % / 3.2 % off the left edge here (§13 A3) |
| E1 placard, B | same, centred, 8 % margin | B · 0.309 (16:9), 0.319 (iPad 1.44), 0.345 (4:3) | 0.184 | same | 72 % w, 86 % h at 16:9 | 2.97 | Below aspect 1.50, when the legend side is blocked (P2/P3 near a corner), or with the transcript on in B |
| **E2 key + tag** | assembly ≈ 0.13 m long | object · **0.20** | 0.15 | at `BaseEye`; the key comes to the centre | 50 % h | 4.16 | Tag digits 15 mm (`interact_key_tag_rect.py:38`) = **62 px** (96.9′). The tag texture is 3,300 px/m, so it is magnified 1.26× at 1080p. Ask (us): `Prop_KeyTagNo` at 4096² |
| S6 key board | 0.30 × 0.40 | B · 0.39 | angular | ~1.45 · 0° | 86 % h | 2.35 | SHOULD |
| S6 key cabinet (door open) | 0.72 × 0.46 | B · 0.45 | **0.35** (4.6 mm label digits → 18.6′) | ~1.47 · 0° | 86 % h | 2.04 | Digits 9.4 px at base, 14.6′: needs the zoom, or 平面视觉 raises them to ≥ 6 mm |
| T1 wall clock | Ø 0.32, at 2.1 m | 0.59 (tiptoe ≤ +0.10 m) | none (hands are geometry) | 1.72 · **40° up** | ≈ 40 % h | 1.55 | The test case for the height band |
| T2 vending window | from the `window` anchor (ESTIMATE ≈ 0.55 × 0.95) | ≈ 0.9 | 0.5 | ~1.2 | — | — | panel; orbit ±20° |
| T3 copier console | faces up; size to measure | ≈ 0.4 | 0.25 | ~1.3 · ≤ 65° down | — | — | the text-up rule |
| T4 desk phone | 0.20 × 0.09 × 0.32 | 0.50 (object) | 0.35 | at `BaseEye` | 50 % h | 1.66 | |

**Travel time** = `clamp(0.30 + 0.12 × travel_m, 0.40, 0.55)`:
- 0.40 s from 0.7 m;
- 0.44 s from 1.2 m;
- 0.49 s from 1.6 m.

Times are ×1.5 at Camera motion 50 %.

### 3.4 Readability: an angular rule, measured contrast

**The rule** is stated as a visual angle of the cap height, so it carries across screens.

| Angle | Reads | On the 1080p reference |
|---|---|---|
| **≥ 18.6′** | reads | ≥ 12 px |
| 14–18.6′ | with effort | 9–12 px |
| < 14′ | needs the zoom step | < 9 px |

- **Reference screen:** a 24-inch 16:9 monitor at 60 cm. Its height spans 1,678′, so 1 px is 1.554′. `TOUCH_CONTROLS.md` §4 uses the same reference.
- HFES 100 gives about 16′ minimum and 20–22′ preferred (**UNVERIFIED** cite; check the clause). 18.6′ sits between them.
- **Screen classes.** The same framing subtends a different angle on each class, because in-world size scales with screen height:

| Class | Screen height | Angle | × reference |
|---|---|---|---|
| Desktop (any resolution) | 299 mm at 60 cm | 1,678′ | 1.00 |
| Phone (iPhone 16 Pro landscape) | 402 pt × 0.166 mm = 66.7 mm at 30 cm | 762′ | **0.45** |
| Tablet (iPad 11-inch) | 820 pt × 0.193 mm = 158 mm at 40 cm | 1,343′ | 0.80 |

  `Screen.dpi` is not trusted on desktop (UNVERIFIED on macOS), so desktop always uses the reference. The game picks handheld classes with `FrontRoomsHandheld.Active` / `IsTablet`.

**The zoom step (one step, angular).** For the region nearest the screen centre, with smallest cap `c`:
1. `h_angle = c / 18.6′ × A_screen`. This is the visible height at which `c` reads.
2. `h_mag = H_px / (2.0 × texelsPerMetre)`. This is the visible height at the **2.0× texel magnification cap**. It replaces v1.0's 1:1 rule, which shrank the zoom on high-resolution screens: the 2.4 mm line was 12.5′ on a 27-inch 4K monitor at 60 cm and 9.8′ on a 14-inch Retina MacBook Pro at 50 cm. With this rule they become 21′ and 14.8′. The MacBook stays "with effort", because desktop uses the reference angle (`Screen.dpi` is not trusted).
3. `h = max(h_angle, h_mag)`.
   - If `h ≥ 2 · minDist · tan(v/2)`: dolly to `d = h / (2 · tan(v/2))`, FOV unchanged.
   - Else: `d = minDist` (0.18 m) and **narrow the FOV** to `2 · atan(h / (2 · minDist))`, with a floor of 30°. This needs rig contract C17c, because it goes past `MaxFovDeltaDeg` 15.
4. The step is offered only if it is ≥ 10 % closer than the base framing.

| Screen | Placard zoom (2.4 mm cap) | Smallest line | Texel magnification |
|---|---|---|---|
| Desktop reference (all resolutions) | 0.184 m, FOV 61 | 18.6′ (12.0 px at 1080p) | 1080p 0.83×, 1440p 1.11×, 4K 1.66× |
| Phone (render 1,206 px tall) | 0.18 m, **FOV 31.2°** (2.0× cap binds) | 18.2′ | 2.0× (the 2.4 mm cap still covers 14 texels) |
| iPad 11-inch (1,640 px) | 0.18 m, FOV 51.4° | 18.6′ | 1.58× |

On handheld, **pinch is continuous** from base to the zoom step, and double-tap toggles base ↔ zoom. FOV changes take ≥ 0.40 s (`MinFovChangeSeconds`).

**Placard caps, exact from `placard.swift`** (mm → arcmin; px at 1080p in brackets). Desktop base is composition A at 0.36 m (16:9). Phone base is A at 0.36 m. "zoom" is the angular step above.

| Line | Cap (mm) | Desktop base | Desktop zoom | Phone base | Phone zoom | Lit glyph/paper (measured) |
|---|---|---|---|---|---|---|
| EVACUATION PLAN | 15.0 | 59.4′ (38) | 116′ (75) | 26.9′ | 114′ | (title, dark ink) |
| IN POWER FAILURE, WAY ON, EXIT, NO EXIT (glow ink) | 5.2 | 20.6′ (13.2) | 40.3′ (25.9) | 9.3′ | 39.4′ | **1.15–1.16** |
| YOU ARE HERE; footer | 4.4 | 17.4′ (11.2) | 34.1′ (21.9) | 7.9′ | 33.3′ | 3.69 |
| EXIT (plan), AS PRINTED | 4.2 | 16.6′ (10.7) | 32.6′ (21.0) | 7.5′ | 31.8′ | 5.38 / 5.04 |
| Grid bubbles, title block | 3.6 | 14.2′ (9.2) | 27.9′ (18.0) | 6.5′ | 27.3′ | |
| N | 3.2 | 12.7′ (8.2) | 24.8′ (16.0) | 5.7′ | 24.2′ | |
| MATCH LINE — SEE SHEET A-3 | 3.0 | 11.9′ (7.6) | 23.3′ (15.0) | 5.4′ | 22.7′ | |
| SCALE: 3/16" = 1'-0" | 2.8 | 11.1′ (7.1) | 21.7′ (14.0) | 5.0′ | 21.2′ | |
| WALLCOVERING SHOWN AT 1/25 SIZE | 2.4 | 9.5′ (6.1) | 18.6′ (12.0) | 4.3′ | 18.2′ | |

- **Size result.** With the zoom step every line reaches ≥ 18.2′ on every screen class.
- **At desktop base**, the message lines are 16.6–20.6′, with effort to readable. Composition B gives +17 %, or 平面视觉 can raise the 4.2–4.4 mm lines (§7.5).
- **How contrast was measured.** Linear luminance from the sRGB capture; the 90th percentile over the 5th, per line box. Measured on `q16_u1_A3_legend_0p6m.jpg` (lit, Steady cell), my measurement for v1.1. The critic got 1.14–1.16.

**Glow-line contrast per lamp state.** The legend words teach the hint system, so their limit is contrast, not size.

| Lamp state | Glyph/paper | Source | Intended? | Close-up rule |
|---|---|---|---|---|
| Lit (Steady) | **1.15–1.16** | measured above | **Yes.** Placard spec A3: "faintly visible as pale #D6DEBD, with no glow" | No lift, no added contrast. The words stay faint, as on the wall. The transcript leaves them out |
| Own lamp off, 2 lit neighbours (A4, 10 s) | **1.231** linear (built k 0.158) | `placard/20_build.md` §5.3 | The bar is 1.30: a FLAG for Red (k 0.185, a 14 % glow cap, reaches 1.30) | Lift up to +1.0 EV, plus Inspect-only `ColorAdjustments.contrast` up to **+12** (ESTIMATE), scaled by the adapt weight, so the **displayed** ratio is **≥ 1.30** (AT-01j). If Red takes k 0.185, the contrast term may go to 0 |
| Dead area (G = 1) | 1.869 | `20_build.md` §5.3 | yes | passes; the lift only |
| Failing lamp (Ls 0.3) | 0.977 mean | `20_build.md` §5.3 | Breathes: readable about 1 s in every 2 s (spec §4) | frozen at its paused value; whatever that frame holds |

**Transcript rule for glow lines.** They are listed only while their displayed ratio is ≥ 1.30. This is computed from `FrontRoomsPlacardGlow`'s gate G and the cell light W, using the table AT-01e measures. Otherwise the transcript names the swatches without their words, so it never gives away the power-failure reveal.

The diagram `images/d_02_placard_base_vs_zoom.jpg` simulates base vs zoom by scaling Red's render. In game the zoom samples the 6 px/mm texture, so it is sharper than the mock.

### 3.5 Transition

**Clock (one clock everywhere).** `FrontRoomsInspect.Dt = Time.captureDeltaTime > 0 ? Time.captureDeltaTime : Time.unscaledDeltaTime`. This is the same rule as `FrontRoomsZoneReflectionDriver.cs:13`. Under a capture harness, unscaled time is wall time (`FrontRoomsTouchPlaytestDriver.cs:75-78`). This clock drives:
- the rig tick in Inspect (C7);
- `CancelAfter`, the cooldown counter, `Age`;
- damping, adapt and hint fades.

**Curves** (rig contract C17b; v1.0 used the rig's cubic, 3× peak slope):

| Leg | What | Time | Curve | Peak (1.2 m travel, 60° approach) |
|---|---|---|---|---|
| In | position, rotation, FOV on one weight | 0.40–0.55 s | **sine in-out** (peak 1.57× the average) | **4.3 m/s**; swing ≤ 35° → ≤ 125°/s (v1.0: 8.2 m/s, about 410°/s) |
| Out | rotation | **0.15 s** (`RotBlendOut`, the glass `LookBlendBack .15` precedent) | **sine out** (fast start, settles) | ≤ 370°/s for a 35° swing, in the first frames |
| Out | position and FOV | **0.40 s** (`MinFovChangeSeconds` for the 15° FOV change) | sine out | 4.7 m/s at the start, falling to 0 (v1.0: 9 m/s) |

- **Why the split.** Control returns at once. With one cubic weight, v1.0 kept the view ≥ 90 % on the shot for 0.117 s and then whipped home. Now mouse look shows within 1–2 frames.
- **Camera motion 50 %:** v1.0 was identical to 100 %. Now the times are ×1.5: in 0.60–0.83 s; out 0.23 s rotation, 0.60 s position.
- **Camera motion Off:** cut in, cut out.
- **Early exit** after 0.20 s: `rig.EndShot` returns from the current weights.
- **Relay during the return:** the shot uses `cancel = ShotCancel.Relay` (not `PlayerInput`). `CancelForRelay` (`FrontRooms3DGame.cs:1080`) snaps it home in `ChaseFreeSnap` 0.15 s (audit §3.2). `Consume` still swallows E and S without cancelling.
- **Damping in the shot:** zoom 0.25 s (dolly) or ≥ 0.40 s (FOV), pan 0.12 s, turn 0.08 s, base look 0.12 s. All are critically damped on the inspect clock. Each value **snaps** to its target when within 1e-5 m / 1e-3°. G14 accumulates only on a bit-identical `viewProj` (`FrontRoomsGlassRTSystem.cs:1186-1189`).
- **Collision:** §3.2 steps 5–7. The rig's per-frame clamp is the safety net. Its hit buffer goes from 8 to 16 (C17d), so the nearest hit is never dropped.

### 3.6 Controls inside the close-up

| Input | flat | panel | object |
|---|---|---|---|
| Mouse / one-finger drag on the look side (`Look`) | **at base: ±3° look** (damped 0.12 s; inside the solved limits; P8 proposed ±6°); **zoomed: pan** in the face plane, clamped to the framed rect + margin | orbit ±20° yaw, ±10° pitch | turn: yaw unlimited, pitch ±80° (1 look-degree = 1°) |
| `Zoom` (new): scroll, RMB, pinch, double-tap | one step in or out. Scroll: one step per gesture, re-armed after 0.2 s with no scroll (trackpad inertia). Pinch: continuous between base and the step. Double-tap: toggle. The first zoom-in goes to the region nearest the screen centre (for the placard, the legend) | one step | one step (0.70 × d) |
| E, S, Esc, Android Back, touch BACK, left-zone stick pull-back | exit (after 0.20 s) | exit | exit |

The ±3° look keeps the frame alive, so the frozen world does not read as a hang. G14 resets while it moves and re-converges about 0.5 s after the mouse stops.

### 3.7 The look

**Light: no added lamp.** The frozen world light is the light. SH2 (2024) lights its close-up only with James's own flashlight (`02 §3.4`, P6).

| Option | Verdict | Why |
|---|---|---|
| A camera-mounted light (Unlock's `ShotLightIntensity .6`) | **no** | Non-diegetic (no flashlight in FrontRooms). It would wash out the glow ink and costs 1 of 32 WebGL lights |
| A lighter or matches | **no (v1)** | A new mechanic that changes the horror; it would also wash out the glow |
| **Eye adaptation** (exposure + a little contrast, dark cells only) | **yes** | Period-neutral (the eye, not a device). No GPU readback |

**Volumes.** All are runtime-built `VolumeProfile` instances (`ScriptableObject.CreateInstance`, `HideFlags.DontSave`).
- **No asset is written, and `sharedProfile` assets are never touched.** Writing per frame to a shared profile would dirty and save the `.asset` in Play Mode (§13 B4).
- Post variant stripping is off in this project (`UniversalRenderPipelineGlobalSettings.asset:22`, `m_StripUnusedPostProcessingVariants: 0`), so a runtime-only Bokeh profile is safe. If anyone turns stripping on, add a reference asset.
- **Values are base + delta.** URP overrides are absolute. At Open, read the resolved values from `VolumeManager.instance.stack` while the inspect volumes are at weight 0. If the adapt volume is still decaying, keep the last snapshot. Then write `base + delta`.
- Office proves why this is needed: `FrontRoomsPost_Office.asset` sets postExposure 0, CA 0.03 and contrast 4. A fixed "0.15 + lift" would brighten Office close-ups by +0.15 EV.

| Volume (priority) | Weight | Overrides: flat / panel | Overrides: object |
|---|---|---|---|
| `Post / inspect Shot` (2) | the rig's **eased** position weight (`ShotHandle.EasedWeight`, C18) | vignette base **+0.04**; lens distortion **−0.001** (keeps the uber keyword on: rails look straight, and no variant change); CA **max(0.001, base − 0.04)** (keyword kept) | vignette base **+0.08**; LD and CA not overridden |
| `Post / inspect Adapt` (3) | adapt `a ∈ [0, 1]`, its own τ: **0.6 s in and out** (Reduce flashing: 1.2 s), independent of the shot weight | `postExposure` base **+1.0 EV**, `contrast` base **+12** (ESTIMATE) | same |
| `Post / inspect DOF` (2), desktop only | **1** while an object close-up is Active, else 0 | — | Bokeh. Focal length and aperture go by the eased weight from **1 mm, f/32** (CoC ≈ 0.007 px, invisible) to the target. The target is tuned so the background at 3 m blurs ≈ **4–6 px at 1080p**, a ½-inch CCD camcorder look (≈ 3.7 mm, f/4; audit §3.0). Focus = the object distance per frame |

**The adapt target:**
- `W = min(1, L + 0.2 · ΣL_n)` from `map.LampLevel` of the target's cell and its 4 neighbours (read-only).
- `a_target = clamp((0.45 − W) / 0.45, 0, 1)` while the close-up holds; 0 after `Close`.
- It is 0 in a lit cell. That keeps the lit legend as faint as the spec intends.
- ESTIMATE; tune on captures to meet AT-01j.

**No hitch on entry (prewarm).**
- The flat and panel close-up uses the **same keyword set** as play: LD, CA and vignette stay on, and exposure and contrast only change LUT values. So it needs no prewarm.
- The object close-up adds the Bokeh pass. At `MapRunStarted`, `FrontRoomsInspectLook.Prewarm()` turns on the DOF volume for **one frame** at the zero-CoC values. That frame is visually identical to the base (AT-01n).
- It is skipped on WebGL and mobile, which have no DOF.
- Unity 6's `GraphicsStateCollection` warm-up is not used: UNVERIFIED on 6000.3, and not needed.

**Things the look must hold:**
- **Glow ink.** `FrontRoomsPlacardGlow` runs on `Time.deltaTime`, so it holds its value at scale 0. The frozen frame is honest.
- **Grain moves at scale 0.** URP 17.3 seeds grain with `Random.InitState(Time.frameCount)` and restores the RNG state (`PostProcessUtils.cs:48-52, :102-106` in the project's package cache). No grain change. v1.0's AT-01k is closed.
- **G14 glass RT.** It runs on `Time.frameCount` and `realtimeSinceStartup` (`FrontRoomsGlassRTSystem.cs:882-884`).
  - It is per-frame RT while the camera travels.
  - It accumulates to 32 once the pose is bit-identical (the snapping in §3.5).
  - Pan, look and zoom reset it, and it re-converges after.
  - No moving instances (the Relay frozen, doors still), so no extra cost.
- **LOD and shadows.** The pose is inside every LOD0 range (placard LOD0 to 2 m; key and tag to 1.0 m). The placard keeps `ShadowCastingMode.Off`.
- **Lens.** The placard's non-glare lens stays on desktop. Its F0 0.039 reflection of the frozen ceiling is honest. It is skipped on WebGL.

### 3.8 Data: the `inspect` block

It is written by `kitlib.py` from each asset module, and read by `FrontRoomsKitLibrary.Info`. Positions are Unity part-local, converted through `to_unity` like colliders. Unknown keys are ignored by `JsonUtility`.

```json
"inspect": {
  "kind": "flat",
  "verb": "read",
  "face": "face", "faceDir": "face_dir", "up": "",
  "size": [0.4668, 0.3138],
  "margin": 0.08,
  "screen": [0.01, 0.0, 0.63, 1.0],
  "dist": [0.36, 0], "distMaxA": 0.42,
  "minDist": 0.18, "maxDist": 1.0,
  "maxAngle": 25, "maxYawOff": 25, "fovDelta": -15,
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
| `face`, `faceDir`, `up` | anchor names | `face` / `face_dir` if present; else the front face centre (+Z) and +Z; up = +Y |
| `size` | the framed rect (m) | bounds across the face |
| `margin` | per side, a fraction of the size (composition B) | 0.08 |
| `screen` | composition A's viewport rect (x, y, w, h), intersected with the safe area | full screen |
| `dist`, `distMaxA` | [base, zoom] (m), 0 = solve; A falls back to B beyond `distMaxA` | solve; 0.42 |
| `minDist`, `maxDist` | (m) | 0.18, 1.0 |
| `maxAngle`, `maxYawOff` | vertical tilt, horizontal yaw off square-on (degrees) | 25 (40 above 1.9 m), 25 |
| `fovDelta` | degrees | −15 (flat, panel), −10 (object) |
| `reach`, `approachAngle` | prompt range (m), degrees | 1.6, 60 |
| `orbit` | panel orbit (± degrees) | 20 |
| `box` | aim box, part-local. **Turned into a world OBB at query time** from the transform, never cached in `OnEnable` (the placard mount spawns at the origin, then moves the frame) | bounds |
| `aimPad` | (m) | 0.02, at least 0.08 m overall |
| `texelsPerMetre` | the sharpest texture on the face (for the 2.0× cap) | 2,048 |
| `regions` | readable regions in sheet mm, with the smallest cap (drives the angular zoom). The example rect is a placeholder until 平面视觉 exports the real ones | none (the zoom uses the whole face) |
| `dof`, `ship`, `transcript`, `sound` | as named | false, **false**, "", `plastic` |

Notes:
- `ship` defaults to false, so a kit must opt in on purpose.
- **Placard regions:** 平面视觉 exports them from `placard.swift`'s own layout constants. They are never measured by eye (Red's rule).
- **Transcript lines** carry a `glow` flag, used for the rule in §3.4.
- **Fallback catalogue.** `FrontRoomsInspectCatalog` holds blocks for kits that will not be re-exported soon (the test bed). A sidecar block always wins.
- **Pile pieces never register.** `Info.IsPilePiece` is per kit, not per instance, so `FrontRoomsFurniturePile` passes `inspectable: false` to `Spawn` instead.

---

## 4. Pause semantics

### 4.1 Mechanism

**`Phase.Inspect` + `Time.timeScale = 0`**, from the E press until the camera **starts** home. This needs both:
- the phase gate alone leaves the lamp, door, noise and Foley leaks (`03 §2.3`);
- the time scale alone would still run `UpdateMapPlay` with dt 0 and read input.

**Control returns when the camera starts home** (`02 P4` rule 2). The return is live, so the player and the Relay both move. The gate only lets a close-up open with no perceivable chase. A Relay that comes into sight during the return snaps the camera home in 0.15 s (§3.5).
- *Alternative:* resume when the camera is home. It is fully safe, but it adds 0.40 s of dead control. **DECIDE**.

**Should the Esc pause also set `timeScale = 0`?** I recommend it (optional C8b). It closes the same leaks; today a door swing during Esc still sends noise to the Relay. It is the map chat's call.

### 4.2 What stops, and what keeps running

| Stops (scale 0 and the phase gate) | Clock today | Source |
|---|---|---|
| Player move, look, aim, prompt, stamina, sprint | game dt in `UpdateMapPlay` | `03 §2.3` |
| Relay brain and rig; the v2 director, alarm, spotting and dry clock **must use game dt** (a rule for the map chat) | game dt | `03 §3` |
| Lamps: `TickFixtures`, overrides, Warn bursts; no `FixtureChanged` | `Time.deltaTime` | `FrontRoomsMapWorld.cs:842` |
| Doors: swing, unlock delay, `DoorMoved`; **no noise reaches the Relay** | `Time.deltaTime` | `:843` |
| Start-room stream, title stream | game dt | `03 §3` |
| Tier clock, `elapsed`, captions, rattle jolts, hint card timer | game dt | `FrontRooms3DGame.cs:2155-2166` |
| Placard glow, key spin, revisit timer (`Time.time`) | game time | `03 §3` |
| The wallpaper cue clock (when built) **must use game time** | — | `01 §5` |
| Player, Relay and door Foley components | `Time.deltaTime`, early out at 0 | `03 §3` |
| Screen ads: paused **explicitly** on `Inspecting(true)` (C26) | `VideoPlayer` | §7.6 |

| Keeps running | Why |
|---|---|
| The camera rig (ticked by the game on the inspect clock in Inspect) | the travel |
| `FrontRoomsInspect` (look, pan, zoom, turn, object return) and `FrontRoomsInspectLook` (volumes, adapt, DOF) | the close-up |
| HUD fade, touch layer, haptics | already unscaled |
| G14 glass RT, zone reflection fades | frame-based; converges |
| Streaming and dressing (`FrontRoomsMapWorld.cs:832-841`) | They finish queued work, but the player does not move, so nothing new is queued. AT-01g waits for an empty `WorkInFrame` first |
| Film grain | seeded from `Time.frameCount` |
| FMOD runtime | **the sound chat pauses buses with a snapshot** (§4.3) |

### 4.3 Audio (sound chat; events only, no new AudioSources)

**The snapshot keys on `FrontRooms3DGame.Inspecting(bool)` only.**
- It fires true at the E press and false as control returns. On teardown (R restart, OnDestroy) it fires false.
- `ShotStarted/ShotEnded(ShotKind.Inspect)` are for Foley only. `ShotEnded` fires 0.40 s after control returns, and never on an R restart.

Proposed, their call:
- **On `Inspecting(true)`:** start `snapshot:/Inspect`.
  - Pause `bus:/SFX` (Relay steps, door Foley, window stress) and the Relay and `bus:/Subjective` voices.
  - **Keep the room tone (`bus:/AMB`) 10 dB down.** The pause should still sound like the place (Grip on SOMA's Safe Mode, `02 §3.9`).
  - **New in v1.1: hold the Relay presence (the v2 stage-2 coil hum) at its frozen gain −6 dB, with no steps.** A close-up taken during a warning keeps the threat present without new information.
  - Hold the director's tension slide.
- **On `Inspecting(false)`:** stop the snapshot (≤ 0.2 s release).
- **`Paused(bool)`** is wired separately. In Inspect, it fires only on focus loss, while `Inspecting` stays true. So the Inspect snapshot stays on until `Inspecting(false)`.
- **Listener:** the StudioListener sits on the render camera (scene :342; `FrontRoomsSoundDirector.cs:189-197`), so the 3D voices kept on would pan as the camera travels about 1.2 m. Proposal: `attenuationObject = rig.Eye`.
- `AudioListener.pause` stays forbidden (AUDIO_CONTRACT rule 2).

### 4.4 Input lock and resume safety

- **Locked during Inspect:** move, sprint, the world's E, settings, restart. Only Look, Zoom, Use, ShotBack, Pause (= exit) and Back are read.
- **No time jump.** Game time does not advance at scale 0. Across the exit frame `Time.time` advances by less than 2 frames (AT-01h).
- **No dropped or doubled input.** Held keys are read live on the exit frame. The exit E is swallowed. E and S latches need a release. Two mouse settle frames on any move from Paused into Playing or Inspect.
- **One rig tick per frame.** Branches are `if (phase == Inspect) {…} else if (phase == Playing && mapPlay) {…}` (C7). The entry frame ticks the rig inside `UpdateMapPlay` only.
- **No double entry.** The prompt can show during the return, but E is swallowed until the shot ends; then a 0.30 s cooldown on the inspect clock.
- **No stale pause state.** `SetPhase` records `pausedFrom` when entering Paused and resets it when leaving. Every resume goes through `ResumeFromPause()`. A stray `SetPhase(Playing)` while a close-up is live ends the close-up first (C8).

### 4.5 The pause cannot be used to cheat the Relay

| Way to cheat | Why it fails |
|---|---|
| Freeze a chase to think | No prompt in a chase, a door break, or with the Relay in view. The press frame re-checks after `relay.Tick` (C5b). The key hand-off has the same gate (C21) |
| Use the prompt as a detector | The gate reads only cues the player already perceives (§2.5 C). The prompt never vanishes on a silent `SeesPlayer` |
| Bank free movement | The body cannot move at scale 0. Control returns with the world |
| Recover stamina or let a noise fade | Every regen and decay runs on game dt, which is frozen |
| Peek with the close-up camera | The pose is a near-square view of a face the player already sees (the aim box must be hit first), ≤ 1.3 m ahead. The yaw is at most 25° off square-on, and look, pan and orbit are limited and pre-swept. The object kind does not move the camera |
| Stall the director or spotting meter | Both run on game time |
| Spam E | Each cycle costs 0.40 s of live time and gains nothing |
| **The invariant** | **Pause neutrality** (AT-01g) |

---

## 5. UI (平面视觉 designs type and graphics; the map chat builds the HUD; the touch session builds touch UI)

### 5.1 Prompt and hint

- **Prompt:** one line in the existing prompt slot ("Key prompt", Bayon 20 px desktop, 17 pt handheld). The text comes from the target and keeps the `E  ·  ` prefix, so touch strips it.
  - Proposed: `E  ·  READ` for flat targets, `E  ·  LOOK` for panels and objects.
  - The first-key hand-off uses `E  ·  LOOK` **in the same slot** (not beside the key).
- **Inside the close-up:** no permanent UI. One hint line in the prompt slot, at its own alpha (not the HUD fade). It fades in over 0.3 s and out after 3 s. No blinking.
  - **Each hint counts separately** and shows at most 2 times per session where it applies, so the TURN hint is not spent on placards:

  | Hint | Desktop (proposed) | Handheld (proposed) | Shown when |
  |---|---|---|---|
  | back | `E  ·  BACK` | none (the BACK glyph on the button) | every close-up, first 2 |
  | closer | `SCROLL  ·  CLOSER` | `PINCH  ·  CLOSER` | a zoom step exists, first 2 |
  | turn | `MOUSE  ·  TURN` (the cursor is locked, so not "DRAG") | `DRAG  ·  TURN` | object kind, first 2 |

  Minimal HUD: `UI_SYSTEM.md` is world-first, and Lethal Company prints its exit key (`02 §3.19`).
- **Hidden in Inspect:**
  - fade out over 0.15 s through `rig.HudFade`: the room, threat and key panels;
  - off at once: the crosshair, prompt and hold bar;
  - paused with game time: the hint card and captions, which resume with their remainders.
- **Pause card:** one new line, e.g. `E  READ  (PAUSES)`, in 平面视觉's words.

### 5.2 Transcript (accessibility, off by default) and the TEXT chip

- **Setting:** `Text transcripts: Off / On` (C24). SH2 (2024) shows players expect enlargeable transcripts (`02 §3.4`, P10).
- **TEXT chip (handheld only, T6):** in a close-up with a transcript, a chip toggles the panel for that close-up, whatever the setting.
- **When shown:**
  - Desktop: the framing reserves the right 36 % (`FreeScreenRect`), and composition A already leaves it free.
  - Phone: the panel is the right 36 % of the safe area, **y 64–280 pt**, so it ends above the moved BACK button (T2, proposed centre 768, 337). The touch session places both.
  - The panel shows the printed text in reading order by region. Swatches are named, not explained. **Glow lines are listed only while legible** (§3.4).
  - The size is adjustable (2 steps). It is never a 2D replacement for the object, and there is no journal (`02 P9`).

### 5.3 Reduce flashing

No flashes in the close-up. With the setting on:
- the adapt τ is 1.2 s **both in and out** (v1.0 faded the lift with the shot weight on exit: a 2× luminance drop in about 0.2 s);
- the hint fades over 0.6 s;
- FOV zoom changes take ≥ 0.6 s;
- the placard glow already slews at 0.5/s.

---

## 6. Platforms

| | Desktop (reference) | Mobile (iOS, Android) | WebGL |
|---|---|---|---|
| Logic, poses, timings, pause | §2–§4 | identical | identical |
| Prompt and enter | `E  ·  READ` / LOOK; E | USE shows a **READ glyph**; tap USE, or **tap the target** (C15) | as the device |
| Look, pan, turn, orbit | mouse | one-finger drag on the look (right) side | as the device |
| Zoom | scroll (one step per gesture) or RMB | **pinch** (continuous), **double-tap** (toggle) | as the device |
| Exit | E, S, Esc | **BACK** (the USE button moved, T2), stick **pull-back** in the left zone, Android Back | as the device |
| Composition | A at aspect ≥ 1.50 (fit), B below | phone 2.17: A at 0.36 m in the safe area; iPad 1.44: B | as the device |
| Zoom step | dolly to 0.184 m | 0.18 m + FOV 31.2° (phone), 51.4° (iPad) | as the device |
| Look (post) | §3.7 in full | no DOF; Shot and Adapt volumes kept (uber values, free) | no DOF, no lens, no RT; Shot and Adapt kept |
| Gate | — | `#if UNITY_IOS \|\| UNITY_ANDROID`: no DOF volume, no prewarm | `#if (UNITY_WEBGL && !UNITY_EDITOR) \|\| FRONTROOMS_WEBGL_PREVIEW`: no DOF volume, no prewarm; desktop byte-identical (Red's WebGL-only rule) |
| Haptic | — | one light tick on open (T5) | — |
| Transcript | right 36 % | TEXT chip; right 36 % of the safe area, y 64–280 pt | as the device |
| Tag texture | 4096² | **import cap 2048** (iOS and Android tabs) | import cap 1024 |

---

## 7. Ownership and API

### 7.1 Visual-owned (us): the prototype file list

Built in a clone made with `W/tools/mkclone.sh inspect --feature placard`. It ends as a v2 apply package `W/apply/inspect.sh` with `inspect_bases.txt`, `inspect_expected.txt`, `inspect_payload/`, `inspect_base/` and `inspect.diff` (`W/apply/README.md`). The package holds **visual-owned files only**. Contract diffs go to their owners as text.

| # | File | New / changed | What |
|---|---|---|---|
| 1 | `Assets/Scripts/Rendering/Inspect/FrontRoomsInspect.cs` | new | static facade: registry, `Find`, `CanOpen`, `Open`, `OpenHeld`, `Tick`, `Close`, `Abort`, `Dt`, events; the hidden runner's `LateUpdate` (object placement and return, hint alpha) while `Active`, **including Closing** |
| 2 | `.../Inspect/FrontRoomsInspectable.cs` | new | per-instance component: registers in `OnEnable`, leaves in `OnDisable` (raises `AbortRequested` if it is `Current`); world OBB at query time; Scene-view gizmo of the solved pose |
| 3 | `.../Inspect/FrontRoomsInspectFraming.cs` | new | the pure solver of §3.2 and the angular zoom of §3.4 |
| 4 | `.../Inspect/FrontRoomsInspectLook.cs` | new | the three runtime volumes, base snapshot, adapt, DOF focus, prewarm |
| 5 | `.../Inspect/FrontRoomsInspectDef.cs` | new | `[Serializable]` data (§3.8) |
| 6 | `.../Inspect/FrontRoomsInspectCatalog.cs` | new | fallback blocks (test bed); prompt, hint and transcript string ids (strings from 平面视觉) |
| 7 | `Assets/Editor/Rendering/Inspect/FrontRoomsInspectTests.cs` | new | EditMode: solver numbers of §3.3 at 16:9, 16:10, 1.54, 1.44, 4:3, 21:9 and phone; angular zoom table §3.4; `Find` order; zero allocations; static reset |
| 8 | `Assets/Editor/Rendering/Inspect/FrontRoomsInspectLookdev.cs` | new | captures every inspect kit at the game numbers; contrast probe (p90/p5 linear) |
| 9 | `Assets/Editor/Rendering/Inspect/FrontRoomsInspectPlaytest.cs` | new | the AT-01g / AT-05 / AT-11 harness: `FrontRoomsInput.InjectSnapshot`, `captureDeltaTime` 1/60, gameplay-state hash |
| 10 | `Assets/Scripts/Office/FrontRoomsKitLibrary.cs` | changed | `public InspectDef inspect;` in `Info`; `Spawn(…, bool inspectable = true)` attaches `FrontRoomsInspectable` **only when `Application.isPlaying`**, `inspectable`, and `inspect.IsSet` (editor tools and Level Designer saves never get the component) |
| 11 | `Assets/Scripts/Office/FrontRoomsFurniturePile.cs` | changed | passes `inspectable: false` |
| 12 | `Tools/Blender/frontrooms_kit/kitlib.py` | changed | `def inspect(self, kind, **kw)` → `self.meta["inspect"]`; `box`, `face` converted in the sidecar writer |
| 13 | `Tools/Blender/frontrooms_kit/assets/interact_key.py`, `interact_key_ring.py`, `interact_key_tag_rect.py`, `interact_key_tag_round.py`, `interact_key_tag_long.py` | changed | `kit.inspect("object", …)` (with D1.5) |
| 14 | `Tools/Blender/frontrooms_kit/assets/evac_placard.py` (in the Q16 placard track) | changed | `kit.inspect("flat", …)` with the block of §3.8 |
| 15 | `Assets/Resources/Props/Models/<kit>.json` for 13 and 14 | regenerated | sidecars from the kit export |
| — | `interact_key_board.py`, `interact_key_cabinet.py` | later | SHOULD (S6), `ship: false` |

No post asset and no `FrontRoomsRenderSetup` change: the profiles are built at runtime (§3.7). The door sign and plate get no inspect block.

**Merge order** (Roslyn-checked by `rebase_apply.sh`):
1. The rig and timing contracts C16–C19 land first. They are small, additive and compile alone.
2. Then `inspect.sh`. Inspectables register but nothing calls `Find`, so it is harmless alone.
3. Then the game contracts.
4. T3 (zoom input) is a later one-line follow-up to C7.

**Signatures:**

```csharp
public enum InspectKind { Flat, Panel, Object }
public enum InspectSound { Paper, Plastic, Metal, Keys }

public static class FrontRoomsInspect
{
    public static bool DevTargets;              // test-bed kits (ship: false) register only when true; reset in Abort
    public const float SurfaceSlack = .02f;     // a box on a collider face wins up to 2 cm behind the hit
    /// <summary>The inspect clock: captureDeltaTime under a harness, else unscaled.</summary>
    public static float Dt => Time.captureDeltaTime > 0f ? Time.captureDeltaTime : Time.unscaledDeltaTime;

    /// <summary>Nearest available, in-reach, in-angle target whose padded world OBB the ray enters before maxDistance. No allocation.</summary>
    public static bool Find(in Ray ray, float maxDistance, out FrontRoomsInspectable target);
    /// <summary>A pose solves for this eye, aspect, FOV and safe area, and its path is clear (cached).</summary>
    public static bool CanOpen(FrontRoomsInspectable target, FrontRoomsCameraRig rig);
    /// <summary>Begins ShotKind.Inspect (fullPose, ease Sine, rotBlendOut .15, lockMove, lockLook, cancel Relay). Null if it cannot.</summary>
    public static ShotHandle Open(FrontRoomsInspectable target, FrontRoomsCameraRig rig);
    /// <summary>Key hand-off: the held key's root moves to the camera; on close it returns to heldPose, then HeldReturned fires.</summary>
    public static ShotHandle OpenHeld(Transform display, string kit, Pose heldPose, FrontRoomsCameraRig rig);
    /// <summary>Phase.Inspect only, before rig.Tick: look (degrees), zoom (steps; continuous for pinch), toggle.</summary>
    public static void Tick(float dt, Vector2 lookDegrees, float zoomSteps, bool zoomToggle);
    /// <summary>Starts the return (rig.EndShot). The adapt volume decays on its own tau.</summary>
    public static void Close(FrontRoomsCameraRig rig);
    /// <summary>Teardown now (restart, OnDestroy, caught): drops state, volumes to 0, statics reset.</summary>
    public static void Abort();

    public static bool Active { get; }          // Open → the shot's end (true through Closing)
    public static float Age { get; }            // inspect-clock seconds since Open
    public static FrontRoomsInspectable Current { get; }
    public static string HintText { get; }      // "" when none
    public static float HintAlpha { get; }
    public static bool ShowTranscript { get; set; }   // the setting OR the TEXT chip
    public static string TranscriptText { get; }// glow lines only while legible (§3.4)
    public static Rect FreeScreenRect { get; }  // viewport rect free for the transcript (safe area on phones)

    public static event Action<FrontRoomsInspectable> Opened, Closed;
    public static event Action AbortRequested;  // the target went away: the game must EndInspect + CancelShot(shot, 0)
    public static event Action HeldReturned;    // the held key is back at its held pose (the map resumes its timeline)
}

public sealed class FrontRoomsInspectable : MonoBehaviour
{
    public string Id { get; }                   // "Kit_EvacPlacard#3"
    public InspectKind Kind { get; }
    public InspectDef Def { get; }
    public Func<bool> Available;                // null = always
    public Transform Display;                   // object kind: what moves (default: this transform)
    public Pose Face { get; }                   // world, computed at query time
    public Vector2 Size { get; }                // world metres (scale applied)
    public string Prompt { get; }               // "E  ·  READ"
    public InspectSound Sound { get; }

    public static FrontRoomsInspectable Attach(GameObject instance, string kit, InspectDef def);
}

public static class FrontRoomsInspectFraming
{
    public struct Input { public Pose face; public Vector2 size; public float margin; public Rect screen, safeArea; public float fov, aspect, nearPlane;
                          public Vector3 eye; public float minDist, maxDist, maxAngle, maxYawOff, distMaxA, minY, maxY; public float[] dist; }
    public struct Result { public Pose pose; public float distance, angle; public bool ok, compositionB;
                           public Vector2 panMin, panMax, lookMin, lookMax; }
    public struct Zoom { public float distance, fov; public bool offered; }
    public static Result Solve(in Input input);
    /// <summary>The angular zoom of §3.4: smallest cap (mm), screen class angle (arcmin), render height (px), texels per metre.</summary>
    public static Zoom ZoomFor(in Input input, float capMm, float screenArcmin, float viewportHeightPx, float texelsPerMetre);
    public const float ReadArcmin = 18.6f, MaxTexelMagnification = 2f, MinFov = 30f;
    public const float DesktopArcmin = 1678f, PhoneArcmin = 762f, TabletArcmin = 1343f;
}
```

### 7.2 Map chat (关卡设计): CONTRACT requests

Written against main as read for v1.1. The map chat re-reads before patching. These are never edits in Red's project from here.

```csharp
// C1  FrontRooms3DGame.cs:31 — append only (the touch playtest reads phases by name)
enum Phase { Title, Playing, Paused, Caught, Inspect }

// C2  events + state (after :53)
/// <summary>A close-up began (true, at the E press) or gave control back (false, as the camera starts home). False on teardown.</summary>
public static event Action<bool> Inspecting;
public static bool InspectingNow { get; private set; }
FrontRoomsInspectable inspectAimed, pendingInspect; ShotHandle inspectShot;
Phase pausedFrom = Phase.Playing; bool useLatch, backLatch; float inspectCooldown;
bool firstKeyHandOffUsed;   // per run (C21)

// C3  UpdateAim (:1259-1277): ask the registry before Describe
var ray = new Ray(eye.position, eye.rotation * Vector3.forward);
var hitAny = Physics.Raycast(ray, out var hit, Reach, ~0, QueryTriggerInteraction.Ignore);
inspectAimed = null;
if (FrontRoomsInspect.Find(ray, hitAny ? hit.distance + FrontRoomsInspect.SurfaceSlack : Reach, out var t) && CanShowInspect(t))
{ inspectAimed = t; prompt = t.Prompt; }
else if (hitAny) { /* today's Describe / glass block, unchanged */ }

// C4  the gate: hard (hides the prompt) and transient (keeps it, swallows E)
bool HarnessRunning
{
    get
    {
#if UNITY_EDITOR
        return autopilot || autoBaseline;      // the game's own fields; no static the autopilot must set
#else
        return false;
#endif
    }
}
bool CanShowInspect(FrontRoomsInspectable t) =>
    !HarnessRunning && mapPlay && phase == Phase.Playing
    && (glassShot == null || !(glassShot.Active || glassShot.Blending))
    && climbTime < 0f && !pullClear.HasValue && !RelayBlocksInspect()
    && FrontRoomsInspect.CanOpen(t, rig);
bool CanPressInspect() => !useLatch && inspectCooldown <= 0f && (rig == null || !rig.InShot);
// Only cues the player perceives (RELAY_PURSUIT_REDESIGN §8 honest senses). Never SeesPlayer.
bool RelayBlocksInspect() => relay != null && relay.Released
    && (relay.State == HunterState.Chase || relay.State == HunterState.BreakDoor || relay.PlayerSeesRelay);
// v2 (when Spotting / OnMap / stage 3 land):
//   relay.OnMap && (relay.WarnStage >= 3 || relay.Spotting >= .5f || relay.PlayerSeesRelay || relay.State == HunterState.BreakDoor)

// C5  E on a target (:1292, before `if (aimed == null)`); `pressed` is today's (it already ran rig.Consume)
if (inspectAimed != null)
{
    holdProgress = 0f;
    if (pressed && CanPressInspect()) pendingInspect = inspectAimed;   // committed after relay.Tick (C5b)
    return;                                                           // a transient refusal swallows E: never the door behind
}

// C5b UpdateMapPlay, just before `rig?.Tick(dt)` (:1092): commit with this frame's Relay state
if (pendingInspect != null)
{
    var t = pendingInspect; pendingInspect = null;
    if (CanShowInspect(t) && CanPressInspect()) BeginInspect(t);
}

void BeginInspect(FrontRoomsInspectable t)
{
    ReleaseGlass(); holdProgress = 0f;
    inspectShot = FrontRoomsInspect.Open(t, rig);
    if (inspectShot == null) return;
    InspectingNow = true;
    SetPhase(Phase.Inspect);                            // timeScale 0 (C8)
    Inspecting?.Invoke(true);
    Event("inspect", t.Id);
}
void EndInspect(bool byBack)
{
    if (!InspectingNow) return;
    FrontRoomsInspect.Close(rig);                       // rig.EndShot: rotation 0.15 s, position 0.40 s
    InspectingNow = false;                              // before SetPhase, so C8's guard does not recurse
    SetPhase(Phase.Playing);                            // timeScale 1, settle frames
    Inspecting?.Invoke(false);
    useLatch = true; backLatch = byBack;
    inspectCooldown = FrontRoomsShotTimings.Inspect.Cooldown;
}

// C6  Update routing (:2127-2131): replace the Esc toggle line with these two
else if (phase == Phase.Inspect)
{
    if ((input.UseDown || input.ShotBackDown || input.PauseDown) && FrontRoomsInspect.Age >= FrontRoomsShotTimings.Inspect.CancelAfter)
        EndInspect(input.ShotBackDown);                 // Esc exits: no card loop
}
else if (input.PauseDown && (phase == Phase.Playing || phase == Phase.Paused))
{ if (phase == Phase.Playing) SetPhase(Phase.Paused); else ResumeFromPause(); }

// C7  Update (:2155): Inspect first, then else-if Playing, so the rig ticks exactly once per frame
if (inspectCooldown > 0f) inspectCooldown -= FrontRoomsInspect.Dt;
if (phase == Phase.Inspect)
{
    var u = FrontRoomsInspect.Dt;
    var look = Vector2.zero;
    if (mouseSettleFrames > 0) mouseSettleFrames--; else look = input.Look;
    FrontRoomsInspect.Tick(u, look, 0f, false);        // v1; after T3: input.Zoom, input.ZoomToggleDown
    rig?.Tick(u);
}
else if (phase == Phase.Playing && mapPlay) { /* today's branch, unchanged */ }

// C8  SetPhase (:2037-2106)
if (p == Phase.Playing && InspectingNow) { EndInspect(false); return; }   // a stray resume never strands a close-up
if (p == Phase.Paused && phase != Phase.Paused) pausedFrom = phase;       // recorded here, for every caller
var hudPhase = p == Phase.Playing || p == Phase.Inspect;
//  gameplayHudAlpha reset only when entering a hudPhase from a non-hudPhase; room / threat / key panels SetActive(hudPhase)
//  (rig.HudFade, ticked in Inspect, fades them over 0.15 s); crosshair enabled only in Playing; overlay.SetActive(!hudPhase);
//  cursor locked in hudPhase; no pause copy in Inspect.
Time.timeScale = p == Phase.Inspect || (p == Phase.Paused && pausedFrom == Phase.Inspect) ? 0f : 1f;
if (p != Phase.Paused) pausedFrom = Phase.Playing;
if (hudPhase && wasPaused) mouseSettleFrames = 2;   // also on Paused → Inspect
void ResumeFromPause() => SetPhase(pausedFrom);     // Esc (C6), Android Back (C15), the touch card's resume
// C8b (optional, recommended): Paused from Playing also sets timeScale 0 (closes the Esc noise leak).

// C9  R (:2136): && phase != Phase.Inspect
// C10 focus and app pause (:2107-2121): (phase == Phase.Playing || phase == Phase.Inspect) → SetPhase(Phase.Paused)
// C11 UpdateHud (:2170-2219): `hud = play || phase == Phase.Inspect` keeps gameplayHudAlpha; in Inspect the prompt slot shows
//     FrontRoomsInspect.HintText at HintAlpha (not multiplied by HudFade), and the transcript panel shows TranscriptText,
//     in 平面视觉's styles (named so UiFont picks the face, :1482-1487); contextText and holdBar off; captions paused.
// C12 OnDestroy (:2303-2321): Time.timeScale = 1f; if (InspectingNow) { InspectingNow = false; Inspecting?.Invoke(false); }
//     FrontRoomsInspect.Abort();
// C12b End() (:2323), before SetPhase(Caught): FrontRoomsInspect.Abort(); rig?.ResetLayers();   // caught during the return
// C12c Start: FrontRoomsInspect.AbortRequested += () => { EndInspect(false); if (inspectShot != null) rig?.CancelShot(inspectShot, 0f); };
// C13 Mobile.cs:187-190: phase == Phase.Inspect ? MenuState.Inspect : … (before the Caught fall-through)
// C14 Mobile.cs:213-220: Playing with inspectAimed → Visible, UseKind.Read;
//     Inspect → Visible, Interactable = FrontRoomsInspect.Age >= CancelAfter, UseKind.Back
// C15 Mobile.cs:127-133 HandleMobileBack: Inspect → EndInspect(false); Paused → ResumeFromPause() (never SetPhase(Playing)).
//     Mobile.cs:168-177 HandleMobileLookTap: return while rig.InShot (the render camera may sit 0.36 m from a leaf);
//     first try FrontRoomsInspect.Find on the tap ray; if found and CanShowInspect && CanPressInspect → pendingInspect = t.
// C16 FrontRoomsCameraRig.cs:6: append ShotKind.Inspect
// C17 rig ShotSpec.fullPose: pose and FOV not scaled by Camera motion; at 0 < motion < 1 every blend time ×1.5;
//     at motion 0 blendIn = blendOut = rotBlendOut = 0 (a cut).
// C17b rig ShotSpec.ease (ShotEase.Cubic default | ShotEase.Sine): Sine = in 0.5 − 0.5·cos(πw), out 1 − cos(πw/2);
//      ShotSpec.rotBlendOut (0 = blendOut): rotation gets its own weight on the way out; CancelShot shortens both.
// C17c rig ShotSpec.fovMin (absolute degrees, 0 = BaseFov − MaxFovDeltaDeg): honoured for ShotKind.Inspect only (angular zoom).
// C17d rig :88: RaycastHit[8] → [16] (ClampToWorld keeps the minimum).
// C18 rig: ShotHandle.Weight and ShotHandle.EasedWeight (position/FOV weight through the same ease the pose uses).
// C19 FrontRoomsShotTimings.cs: add
public static class Inspect
{
    public const float Reach = 1.6f, ApproachAngle = 60f, ReachHysteresis = .1f, AngleHysteresis = 5f;
    public const float TravelBase = .30f, TravelPerMetre = .12f, TravelMin = .40f, TravelMax = .55f; // sine in-out
    public const float BlendOut = .40f;          // position + FOV, sine out (= MinFovChangeSeconds)
    public const float RotBlendOut = .15f;       // rotation, sine out (glass LookBlendBack precedent)
    public const float HalfMotionTimeScale = 1.5f;
    public const float CancelAfter = .2f, Cooldown = .3f;
    public const float FovFlat = -15f, FovObject = -10f, MinSurface = .15f, MaxRise = .10f, MaxDrop = .55f, MaxYawOff = 25f;
    public const float ZoomSeconds = .25f, ZoomFovSeconds = .40f, PanSeconds = .12f, TurnSeconds = .08f;
    public const float BaseLookDeg = 3f, BaseLookSeconds = .12f, ScrollRearm = .2f;
    public const float AdaptTau = .6f, AdaptTauCalm = 1.2f, AdaptMaxEv = 1f, AdaptContrast = 12f;
    public const float FirstKeyHoldMax = 2.0f, FirstKeyBreakMove = .2f, FirstKeyBreakLookDeg = 2f;
}
// C20 harness: covered by C4 (HarnessRunning). The touch playtest keeps Inspect on (AT-06).
// C21 D1.5 key pickup (first key of a run, !firstKeyHandOffUsed):
//     KeyPickup holds at HoldUntil until move >= FirstKeyBreakMove, look >= FirstKeyBreakLookDeg or FirstKeyHoldMax;
//     prompt "E  ·  LOOK" in the normal slot while it holds. If D1.5 takes the key with E, the take sets useLatch,
//     so E must be released first. On pressed && CanInspectHeld():
//       end the KeyPickup rig shot with blendOut 0; inspectShot = FrontRoomsInspect.OpenHeld(keyRoot, kit, heldPose, rig);
//       InspectingNow = true; SetPhase(Phase.Inspect); Inspecting?.Invoke(true); firstKeyHandOffUsed = true;
//     The key timeline stays held until FrontRoomsInspect.HeldReturned, then resumes at PocketStart (one writer at a time).
bool CanInspectHeld() => !HarnessRunning && mapPlay && phase == Phase.Playing && !useLatch && inspectCooldown <= 0f
    && (glassShot == null || !(glassShot.Active || glassShot.Blending)) && climbTime < 0f && !pullClear.HasValue
    && !RelayBlocksInspect();                // CanPressInspect without rig.InShot (the KeyPickup shot is running)
// C22 NOT IN v1 (door sign/plate read in place). Only if Red picks R5-B:
//     FrontRoomsMapWorld: public bool NeedsKeyHere(Door d) => d != null && !d.open && !d.broken && !IsBeingBroken(d) && LockedHere(d);
//     (`DoorLocked` is an event at :127 and Door has no Locked member, so v1.0's predicate did not compile.)
//     plus a per-door "rattled once" flag; door dressing attaches the SIGN only: Available = NeedsKeyHere(door) && rattled,
//     reach 1.2 m, no zoom. With the key held, NeedsKeyHere is false, so OPEN wins.
// C23 optional: the Level Designer walker (FrontRoomsMapWalker.cs:88-150) uses Find / Open so poses preview in the editor (FOV 72 handled).
// C24 FrontRoomsSettings: TextTranscripts (bool, false); optional CloseUpsPause (bool, true) only if Red picks mode C.
// C25 UpdateMapPlay (:944-): latches, after `local` is read
if (useLatch && !EHeld()) useLatch = false;
if (backLatch) { if (input.Move.y < -.1f) local.y = Mathf.Max(local.y, 0f); else backLatch = false; }
// C26 BLOCKING before any v1 build where a CRT can sit in the close-up ray (owner unclear, §7.6):
//     Media/FrontRoomsScreenVideo.cs:133-141 skips its ray and E while FrontRooms3DGame.InspectingNow or the game is Paused,
//     and pauses every VideoPlayer on Inspecting(true) / Paused(true).
```

### 7.3 Sound chat (声音设计)

| # | Item | What |
|---|---|---|
| S1 | Snapshot | `snapshot:/Inspect` on `Inspecting(bool)` only (§4.3): SFX and Subjective paused, AMB −10 dB, **Relay presence / coil hum held at its frozen gain −6 dB (no steps)**, tension held |
| S2 | Pause | the same handling for `Paused(bool)`. In Inspect it fires only on focus loss, while `Inspecting` stays true |
| S3 | Foley (proposed names) | `event:/Foley/Player/InspectIn` on `Opened`; `event:/Foley/Player/InspectOut` on `Inspecting(false)`; `event:/Foley/Player/KeyTurn` while a `Sound = Keys` object turns (existing `AngularVelocity` parameter). These play on the unscaled side of the snapshot. `ShotStarted/ShotEnded(Inspect)` are Foley hooks only |
| S4 | Listener | proposal: StudioListener `attenuationObject = rig.Eye`, so AMB and fixture hum do not pan during the travel |
| S5 | AUDIO_CONTRACT.md | add `ShotKind.Inspect`, `Inspecting`, `InspectSound`, `HeldReturned`; state "ShotKind is append-only". Not allowed: new AudioSources, `AudioListener.pause` |

### 7.4 Touch session (`Assets/Scripts/Input/*`)

| # | Where | Change |
|---|---|---|
| T1 | `FrontRoomsTouchControls.cs:53` | `MenuState.Inspect` (append). Routing in `Begin` (`:668-720`): <br>• right-zone drag → Look; <br>• two-finger pinch → Zoom (continuous); <br>• double-tap → ZoomToggle; <br>• left zone: a hidden stick whose pull-back past today's S threshold → ShotBackDown (exit, as TOUCH_CONTROLS §2 maps S); <br>• USE (as BACK) → Use (exit); <br>• no pause button, no sprint socket. <br>**Reset the double-tap detector on every MenuState change**, so the tap that opened a close-up never also zooms it |
| T2 | `:55` + `FrontRoomsTouchSprites` + `FrontRoomsTouchControlsView.cs:554` | `UseKind.Read`, `UseKind.Back` (append), glyphs by 平面视觉. In `MenuState.Inspect` the view shows USE whatever `prompt.Visible` says, as BACK, **moved to the lower right** (proposed centre 768, 337 pt on the 874 × 402 phone layout; Ø88 hit spans y 293–381, inside the safe area), clear of the transcript panel (y 64–280). The touch session places it |
| T3 | `FrontRoomsInput.cs` | a new `Zoom` action. Scroll y gives ±1 step per gesture, re-armed after 0.2 s with no scroll. RMB press = toggle. Pinch gives a continuous fraction of a step. `FrameSnapshot` gains `float Zoom; bool ZoomToggleDown;` (ctor params at the end, defaulted). Lands after C7 v1; a one-line C7 follow-up then passes them |
| T4 | `FrontRoomsTouchControlsView.cs:701` | Inspect is not `Washed` (no frost over the close-up) |
| T5 | `FrontRoomsMobileInteractionEvents.cs` | `InspectOpened()` → one light haptic |
| T6 | view + controls | **TEXT chip** in Inspect when the target has a transcript. It raises `TranscriptToggled`, and the game flips `FrontRoomsInspect.ShowTranscript` |
| T7 | layout | the transcript panel rect on handheld: right 36 % of the safe area, y 64–280 pt on the phone (scaled on iPad). The touch session confirms it against its layout |

### 7.5 平面视觉

1. **Verbs:** READ / LOOK (or one verb for all), with the `E  ·  ` prefix, in the normal slot (first key included).
2. **The hint pairs, per platform** (§5.1): the words, and whether each shows.
3. **The transcript panel** (§5.2): face, 2 size steps, contrast, desktop and phone placement. Also the placard's transcript text with narrative (strings from A.12), with its glow lines flagged.
4. **Touch glyphs:** READ, BACK, and the TEXT chip, in the HUD key-glyph style (section 2532:4038).
5. **The pause card line.**
6. **Export the placard's readable regions** (rects in sheet mm and smallest cap) from `placard.swift`'s layout constants.
7. **Optional:** raise the 4.2–4.4 mm lines if Red finds the desktop base small (§3.4).
8. **Key cabinet label digits** 4.6 mm → ≥ 6 mm, only if S6 ships. `Prop_KeyTagNo` type at 4096² (we build the texture; they own the type). The door plate needs nothing: its digits are 27 mm.
9. **With narrative:** confirm the lit-state legend stays faint (placard spec A3 says it is intended), and confirm the transcript rule that hides the glow words until they are legible.

### 7.6 Unclear owner: the CRT screen ads (`Media/FrontRoomsScreenVideo.cs`)

- **The problem.** Its private E path (its own ray from `Camera.main`, `Keyboard.current.eKey`) toggles a monitor during a close-up or a pause. In a close-up, the render camera sits right in front of whatever is framed.
- **The minimum patch (C26)** is **blocking** for any v1 build where a CRT can be in the close-up ray:
  - skip the ray and E while `InspectingNow` or Paused;
  - pause every `VideoPlayer` on true and resume on false.
- **Later (L1):** the power toggle moves onto the shared aim path, and the screen becomes a `panel` target.

---

## 8. Budgets and acceptance tests

### 8.1 Budgets

| Item | Budget | Basis |
|---|---|---|
| `Find` per frame | ≤ **0.02 ms** with 64 live targets (ray vs padded OBB built at query time, no allocation) | ESTIMATE |
| Solve + path check | ≤ **0.03 ms** per aimed frame (1 spherecast + the pan/look sweep; cached) | ESTIMATE |
| `Tick` + look driver | ≤ **0.05 ms** | ESTIMATE |
| GC | **0 B per frame** while aiming and inspecting | |
| GPU, flat or panel close-up | **≤ +0.1 ms** over play at the same view (uber values only, same keywords) | ESTIMATE |
| GPU, object close-up (desktop Bokeh) | **≤ +1.0 ms** at 1440p | ESTIMATE; measure |
| GPU, WebGL and mobile | **+0 passes** | |
| Hitches | no frame > **1.5 × the median** of the previous 60 frames during entry, arrival or exit | §3.7 prewarm |
| Memory | `Prop_KeyTagNo` 4096² BC7 with mips ≈ **21 MB** (22.4 MB) on desktop; iOS/Android import cap 2048 (≈ 5.6 MB); WebGL 1024. Three small runtime profiles; no new render targets except URP's DOF buffers | 4096² × 1 B × 4/3 |
| Data | ≤ 1 KB per sidecar block | |

### 8.2 Acceptance tests

They run in the clone (`W/tools/mkclone.sh inspect --feature placard`, plus the map, touch and sound contract diffs applied **in the clone only**). Clocks: `captureDeltaTime` 1/60 unless a test says real time. Every capture goes to the VERIFICATION LOG section 2595:6093 per `VERIFICATION_LOG.md`.

**AT-01 Placard in the start area (Q16).**

| # | Check | Pass |
|---|---|---|
| a | 100 seeds: the placard registers | 100 / 100 |
| b | A pose solves and its path is clear, from 1.5 m square-on and from 1.5 m at 60° | 100 / 100 (A or B); spherecast radius `r_c + 0.01`; stand-off ≥ 0.15 m |
| c | Prompt range | shows at 1.6 m and 60°; with hysteresis, not at 1.7 m or 65° |
| d | Entry, per aspect | `timeScale == 0` on the commit frame. 16:9: arrives in 0.44 ± 0.02 s on the inspect clock, FOV 61.0 ± 0.1, frame 62 ± 2 % w and 74 ± 2 % h. 16:10: A at 0.393 ± 0.005 m. 1.54: A at 0.408 ± 0.005 m. 21:9: A at 0.36 m. 4:3 and 1.44: B. **In every case all 4 frame corners lie inside the screen (0 px clipped)** |
| e | Type and contrast, on 1080p and 2160p captures and a 2622 × 1206 phone capture | sizes match §3.4 within ±10 % (px and arcmin). Contrast per lamp state (lit, A4, G = 1, failing) measured with the p90/p5 method and logged as the table the transcript rule uses |
| f | Freeze over a 10 s close-up | lamp levels of all built cells, Relay position, state and `StateTime`, door angles, glow emission, `elapsed` and the tier clock unchanged; no `DoorMoved`, noise, `FixtureChanged` or `LampDipped` |
| g | **Pause neutrality** | Recorded input through `FrontRoomsInput.InjectSnapshot` (not the autopilot, which never opens close-ups). `captureDeltaTime` 1/60 in both runs. Open only after `map.WorkInFrame` has been empty for 30 frames. A 10 s and a 0.2 s close-up at t = 20 s. **Equal hash at t = 60 s game time** of player position, Relay position, state and `StateTime`, door angles, lamp levels, `elapsed`, tier and keys |
| h | Exit | control on the exit frame; `timeScale == 1`; **`Time.time` advances < 2 frames across the exit frame**; the exit E opens no door and no new close-up; an S exit does not walk backward while S is held; rotation home in 0.15 ± 0.02 s, position in 0.40 ± 0.02 s; `ShotEnded(Inspect)` once |
| i | Camera motion 100 / 50 / Off | same pose at weight 1; at 50 % times ×1.5 (in 0.66 s from 1.2 m); Off = a cut both ways |
| j | Dark cell | Dead lamp: adapt lift +1.0 ± 0.05 EV. **A4 state: displayed glow ratio ≥ 1.30 in the close-up**. Lit cell: displayed ratio within 2 % of the walk-up view (no lift, no contrast) |
| k | Grain at scale 0 | moves frame to frame (seeded from `Time.frameCount`; a check, no fallback) |
| l | Esc, focus, R | Esc after 0.20 s exits (no card); focus loss → Paused-from-Inspect; Esc → the same close-up; R ignored in Inspect, works from the card; then a Playing → Esc pause → Esc gives Playing (no stale `pausedFrom`) |
| m | Post values | Office lit-cell close-up resolves postExposure 0.00 ± 0.01 and CA ≥ 0.001; the SHA-1 of every `Assets/Resources/Rendering/*.asset` is unchanged after a Play session |
| n | No hitch | arrival frame ≤ 1.5 × the median frame time; the prewarm frame equals the base capture within 1 code value |
| o | One rig tick | a counter shows `rig.Tick` runs exactly once per frame on the entry, hold, exit and return frames |
| p | Motion peaks (60° approach, 1.2 m) | camera ≤ 4.8 m/s and ≤ 380°/s at any frame; mouse look moves the view on the first frame after exit |

**AT-02 Key + tag** (a D1.5 clone).
- First key of a run: the held pose holds up to 2.0 s; a move ≥ 0.2 or look ≥ 2° breaks it; `E  ·  LOOK` shows in the normal slot; the take's E never opens it (latch).
- E → key centred at 0.20 ± 0.01 m, FOV 66. Turn: yaw 360°, pitch ±80°.
- Tag digits (15 mm) **≥ 55 px** at 1080p (62 px expected). Texture magnification ≤ 1.3× at 1080p (≤ 1.0× with the 4096² atlas at 1440p).
- Exit → only `FrontRoomsInspect` moves the key back; the pocket beat starts after `HeldReturned`, never during the return. `KeyTaken` fires exactly once. The tag number and colour equal the matching door's plate.
- A chase or the Relay in view during the hold → no prompt, and E does nothing (`CanInspectHeld`).
- The second key of a run → no hold, no prompt.

**AT-03 Doors stay doors** (D1.5).
- On a locked door, E anywhere on the leaf (sign, plate, lever, centre) gives the rattle and LOCKED, at 2.0 m and at 0.5 m alike.
- With the key held, OPEN everywhere.
- Plate digits ≥ 18 px at 1.0 m in place (1080p). The sign cap ≥ 14 px at 0.8 m.

**AT-04 Test bed today** (`DevTargets` on, a clone of main):
- **Clock:** pitch 40 ± 1° up, camera height = eye + 0.10, d 0.59 ± 0.02.
- **Copier console:** text upright, pitch ≤ 65° down.
- **Vending window:** orbit ±20° stays clear of colliders, with no rig-clamp framing jumps (the pre-swept limits).
- **Desk phone:** object at 0.50 m, turns freely; `CheckSphere` keeps it out of the wall.
- **G14:** with a window in frame, accumulation reaches 32 within 40 frames of the camera holding still; a ±3° look resets it and it re-converges within 0.5 s (30 frames) of the mouse stopping.

**AT-05 Gates.**
- Relay `Chase`, `BreakDoor` or `PlayerSeesRelay` → no prompt, and E opens nothing.
- (v2) `SeesPlayer` with stage < 3, spotting < 0.5 and not visible to the player → the prompt **stays**.
- Warning stages 1–2 → allowed.
- Glass shot, climb or pull-clear → no prompt.
- During any shot, the cooldown or the E latch → the prompt stays, E is swallowed: no rattle, no door.
- A glass hold whose aim slides onto a placard → never opens without a fresh press.
- Autopilot and baselines → never opens.
- A target disabled mid close-up → `AbortRequested`, Playing within 1 frame, `timeScale == 1`.
- Caught during the return → `Active` false, the rig empty.
- The Relay in sight during the return → camera home within 0.15 s.

**AT-06 Touch** (touch playtest harness, iPhone 16 Pro profile, 2622 × 1206; and the iPad profile).
- Tapping the target opens the close-up (through `pendingInspect`). Taps during the return never reach `map.Use`.
- Pinch zooms continuously to the zoom step (FOV 31.2° phone, 51.4° iPad). A double-tap on the target opens the close-up without also zooming it. Inside, double-tap toggles the zoom. Drag pans or turns.
- BACK at its Inspect position, a left-zone pull-back and Android Back each exit after 0.20 s.
- Android Back from Paused-from-Inspect returns to the close-up. No stale `pausedFrom` afterwards.
- The TEXT chip opens the transcript. The panel and BACK do not overlap (hit rects disjoint).
- **Angular checks on the phone capture:** at the zoom step every placard line ≥ 18 arcmin (§3.4 table, ±10 %).
- No frost over the close-up. The Caught card never shows for Inspect.

**AT-07 Budgets.**
- Profiler: `Find`, solve and `Tick` timings and 0 B GC as §8.1.
- Close-up frame time vs play at the same spot: +0.1 ms (flat), +1.0 ms (object, desktop 1440p).

**AT-08 WebGL.**
- The same flow with no DOF volume and no prewarm.
- The desktop capture is byte-identical before and after the WebGL gating.

**AT-09 Sound** (sound chat; FMOD banks synced into the clone, `W/TOOLS.md` §2).
- During the close-up: SFX paused, AMB −10 dB, no Relay steps; at stage 2 the presence hum holds at −6 dB (if S1 is accepted).
- Everything resumes on `Inspecting(false)`, not on `ShotEnded`.

**AT-10 Accessibility.**
- Transcripts on: desktop placard in the left 62 %, transcript in the right 36 %; both size steps fit.
- Glow lines are absent in the lit state and present in the dark A4 state (≥ 1.30).
- Reduce flashing: adapt τ 1.2 s in **and on exit** (the luminance change per 0.1 s is ≤ 8 % in both directions); hint fade 0.6 s.

**AT-11 Rig contract tests** (EditMode, in the style of the map chat's `FrontRoomsGlassShotTests`).
- `fullPose` gives the same pose at motion 0.5 and 1; times ×1.5 at 0.5; a cut at Off.
- Sine in-out peak = 1.57 × the average.
- `rotBlendOut` 0.15 and position 0.40 end on time; `CancelShot` shortens both.
- `ShotEnded` fires once.
- `EasedWeight` equals the camera's pull.
- `fovMin` is honoured for Inspect only.
- A 16-hit buffer keeps the nearest hit.

---

## 9. Build order

Per Red's merge rules (2026-10-07): merge as soon as something is verified, and each package ends with a dry-run-OK apply script.

1. **Map chat, rig and timings first:** C16–C19 (additive; they compile alone). AT-11 runs on them.
2. **Visual, now** (`W/tools/mkclone.sh inspect --feature placard`):
   - files 1–11 of §7.1 and the test bed behind `DevTargets`;
   - EditMode tests and look-dev captures;
   - the contract diffs applied in the clone only, to prove them: AT-04, AT-05, AT-07, AT-11.
   - Package: `W/apply/inspect.sh` (v2). Report its dry-run command.
3. **Map, touch and sound** apply their contracts in main: C1–C15, C20, C24–C26; T1–T7; S1–S5. Then the C7 zoom follow-up after T3.
4. **With Q16:** the placard's sidecar block, regions from 平面视觉, composition A. AT-01, AT-06, AT-10.
5. **With D1.5:** the key hand-off (C21), the key kit blocks, `Prop_KeyTagNo` at 4096². AT-02, AT-03.
6. **Later:** the CRT (L1), any SHOULD Red picks, sanctuary placard copies.

---

## 10. Decisions for Red

My default is first in each row.

| # | Question | Default | Alternative |
|---|---|---|---|
| R1 | Close-ups near the Relay? | **No prompt in a chase, a door break, or with the Relay in view** (perceivable cues only; v2 adds stage 3 and spotting ≥ 0.5). Warnings are allowed and frozen | B: always allowed, always paused. C: a "close-ups pause: Off" setting (live) |
| R2 | When does the world restart? | **When the camera starts back** (control at once; rotation 0.15 s, position 0.40 s, live) | when the camera is home (0.40 s of dead control) |
| R3 | Placard framing | **Your render's frame:** A at aspect ≥ 1.50 (0.36 m at 16:9, 0.393 m at 16:10); centred B below 1.50 | always B (centred 0.31 m, +17 % text size, no wall) |
| R4 | Key and tag | **First key of a run only:** the held pose waits up to 2.0 s with `E · LOOK` | every key (slower pickups); or a hanging-key target before the 0.9 m vacuum |
| R5 | Door sign + number plate | **Read in place** (sign 14.7 px at 0.8 m; plate digits 18.7 px at 1.0 m) | B: a sign-only close-up after one rattle, reach 1.2 m (C22) |
| R6 | Which SHOULD props ship? | **None in v1.** Key hosts wait for a hint design; the clock, vending, copier and phone are test beds | pick any |
| R7 | Should Esc (from Playing) also freeze time? | **Yes** (C8b): closes the noise leak | keep today's Esc |
| R8 | Glow cap (placard build §5.4) | keep k 0.158; the close-up adds dark-state contrast to reach 1.30 displayed | k 0.185 (14 % cap): 1.30 in the world too, and the inspect contrast term can go to 0 |
| R9 | Texture magnification | **≤ 2.0×** in the zoom step on high-resolution screens and phones (14 texels on the smallest cap) | 1:1 only (4K 12.5′, Retina MacBook 9.8′, phone 8.6′ for the smallest line); or re-rasterize the sheet at 10,000 px/m |

---

## 11. Open items and UNVERIFIED

- HFES 100 character-height clause (16′ / 20–22′): UNVERIFIED; the 18.6′ line is my threshold.
- `Screen.dpi` on macOS: not trusted; desktop uses the reference angle.
- `VideoPlayer` behaviour at `timeScale 0` (C26 pauses explicitly anyway).
- My numbers to tune on captures (ESTIMATE): the body height band (−0.55 / +0.10 m), the adapt curve and the +12 contrast, the DOF target, all budgets, the vending and copier region sizes, `distMaxA` 0.42 m.
- `Prop_KeyTagNo` digits exist only as kit constants (15 mm). AT-02 depends on the slot being filled in D1.5.
- v2 hunter fields (`Spotting`, `OnMap`, stage 3, `LockingOn`) are not in main. The gate's v2 line is written against `RELAY_PURSUIT_REDESIGN.md` §8 names.
- The touch files were being edited live. Re-read their lines before the touch patch.
- `UI_SYSTEM.md`'s "hold E reads / Tab opens notes" line is stale against this design (no notes screen, `02 P9`). Flagged for the map chat and 平面视觉, not changed here.

---

## 12. Images (support diagrams, JPG q85; not verification images)

| File | Shows | v1.1 deltas (not redrawn) |
|---|---|---|
| `images/d_01_inspect_flow_timeline.jpg` | states and timeline of §2.3–§2.4 (v1.0) | Esc now exits (no card loop from Inspect); the return splits into rotation 0.15 s and position 0.40 s; the HUD fade is `rig.HudFade` |
| `images/d_02_placard_base_vs_zoom.jpg` | Red's render as base (0.36 m) and a simulated zoom (0.18 m) | the zoom is now 0.184 m on desktop (same picture) |
| `images/d_03_inspect_poses.jpg` | side views: placard 0.36 / 0.18 m, door ID 0.58 m stoop, clock 0.59 m at 40° up, key at 0.20 m; the body band | the door ID pose is **not in v1** (read in place) |

---

## 13. Critic response (v1.0 → v1.1)

Two critics reviewed v1.0: **A** (design and readability, read-only) and **B** (engineering against main, read-only). All 39 issues are accepted. Three get a different fix than the one proposed: A7 (the FOV value), B2 (sign only, not 2 boxes) and B5 (its own DOF volume). One sub-point is not adopted: B5's `GraphicsStateCollection`. Each has evidence below.

### 13.1 Critic A

| # | Issue | Verdict | Fix (where) / evidence |
|---|---|---|---|
| A1 | Chase gate keys on the silent `SeesPlayer` | **Accepted** | The gate uses only perceivable cues: today `Chase \|\| BreakDoor \|\| PlayerSeesRelay` (all in main: `FrontRoomsMapHunter.cs:149-165`); v2 adds `WarnStage >= 3 \|\| Spotting >= .5f` under `OnMap` (§2.5 C, C4, AT-05). In today's main `SeesPlayer` turns into `Chase` on the same tick (`:265-283`), so the leak is a v2 risk. It is closed for both |
| A2 | Key hand-off skips the gate | **Accepted** | `CanInspectHeld()` (C21; `CanPressInspect` without `rig.InShot`); AT-02 chase case |
| A3 | Composition A clips below 16:9 | **Accepted** | `d = max(dist[0], d_fit)` with a corner-projection fit; B below aspect 1.50 (`distMaxA` 0.42 m); safe area on phones (§3.2 step 2, §3.3; AT-01d at 16:9, 16:10, 1.54, 1.44, 4:3, 21:9) |
| A4 | Return feel and peak motion | **Accepted** | (a) rotation 0.15 s, position 0.40 s, sine out (C17b `rotBlendOut`); (b) sine in-out in: 4.3 m/s peak; (c) `maxYawOff` 25°, swing ≤ 35°; (d) motion 50 % = ×1.5 (C17). Cut at Off kept. AT-01h, AT-01p, AT-11 |
| A5 | Legend words limited by contrast | **Accepted** | Measured again: 1.15–1.16 lit (§3.4). Per-state contrast table added. Lit faintness is intended (placard spec A3); the A4 state gets an Inspect-only contrast term to reach 1.30 displayed (AT-01j tightened). The transcript lists glow lines only while legible. R8 links Red's k decision |
| A6 | Door ID takes over the locked-door verb | **Accepted, with a changed alternative** | Default is read in place (§1.3, R5); E3 and its zoom are gone from v1; §7.5 item 8 removed (27 mm already). The alternative C22 is sign-only, after one rattle, 1.2 m, no zoom, `NeedsKeyHere` |
| A7 | Mobile readability | **Accepted, extended** | The rule is angular (18.6′; phone ×0.45, iPad ×0.80). The zoom is angular, with a FOV step past 0.18 m (C17c). Phone: FOV **31.2°**, not the suggested 36°: 36° leaves the 2.4 mm line at 15.6′, while 31.2° reaches 18.2′, where the 2.0× texel cap stops it. Pinch is continuous; TEXT chip (T6); AT-06 angular checks. The same rule lifts the 2.4 mm line from 12.5′ to 21′ on a 27-inch 4K monitor and from 9.8′ to 14.8′ on a 14-inch Retina MacBook (v1.0's 1:1 zoom) |
| A8 | Mobile layout and strings | **Accepted** | (a) BACK moved to the lower right (T2, proposed 768, 337), panel y 64–280 (T7); (b) per-platform hints, `MOUSE · TURN` on desktop (§5.1); (c) left-zone pull-back exits (T1); (d) double-tap reset on MenuState change (T1). AT-06, AT-10 |
| A9 | Esc loop | **Accepted** | Esc exits after `CancelAfter`; the card opens only from Playing; focus loss still pauses over the close-up (§2.5 E, C6) |
| A10 | Reduce flashing breaks on exit | **Accepted** | The adapt volume has its own τ in and out (0.6 / 1.2 s), independent of the shot weight (§3.7, §5.3, AT-10) |
| A11 | Key hand-off window too short | **Accepted** | The first key holds until move, look or 2.0 s; `E · LOOK` in the normal slot; take-E latch (§2.5 K, C21, AT-02) |
| A12 | Dead mouse at base | **Accepted** | ±3° look, 0.12 s damping, pre-swept limits; G14 re-converges about 0.5 s after (§3.6, AT-04) |
| A13 | Hint counting | **Accepted** | Each hint counts separately, 2 showings each (§5.1) |
| A14 | S held on exit | **Accepted** | S latch (C25, §2.5 H, AT-01h) |
| A15 | Stage-2 tension lost | **Accepted as a proposal** | S1: presence hum held at frozen gain −6 dB (the sound chat's call) |
| A16 | Doc fixes | **Accepted** | (1) title readability corrected: 6.5 px at 1.6 m, 12 px at 0.86 m (§1.3, §2.2); (2) AT-02 uses the 15 mm tag digits: ≥ 55 px (62 expected); (3) AT-01g runs on `InjectSnapshot`, not the autopilot; (4) the cabinet needs the zoom (0.35 m) or ≥ 6 mm digits, and the hosts are demoted to SHOULD because the hint is undefined |

### 13.2 Critic B

| # | Issue | Verdict | Fix (where) / evidence |
|---|---|---|---|
| B1 | Resume paths skip `pausedFrom` (softlock) | **Accepted** | `pausedFrom` recorded and reset inside `SetPhase`; `ResumeFromPause()` for Esc, Back and the touch card; the `SetPhase(Playing)` guard ends a live close-up first (C8, C15). `HandleMobileBack` calls `SetPhase(Playing)` from Paused today (`Mobile.cs:132`). AT-01l, AT-06 |
| B2 | Door-ID predicate does not compile | **Accepted; "2 boxes" not adopted** | `NeedsKeyHere` written in C22 (`DoorLocked` is an event at `FrontRoomsMapWorld.cs:127`; `LockedHere` is private at `:2712`). The door ID is out of v1 (A6). The alternative registers the **sign only**, not sign + plate: the plate sits beside the cylinder at 1.0 m, where players aim to try the door, and its 27 mm digits read in place |
| B3 | Two rig ticks on entry; stale Relay state | **Accepted** | C7 `if Inspect … else if Playing`; `pendingInspect` committed after `relay.Tick` and before `rig.Tick` (C5, C5b). AT-01o |
| B4 | Shared-profile writes; absolute values | **Accepted** | Runtime-built profiles, never `sharedProfile` assets; base + delta from the resolved stack; CA and LD floors keep keywords (§3.7). Office values confirmed: postExposure 0, CA 0.03, contrast 4. AT-01m |
| B5 | Prewarm misses the hitch | **Accepted, different fix** | LD −0.001 and CA ≥ 0.001 keep the flat keyword set, so no prewarm is needed there. DOF lives in its **own volume at weight 1**, starting at 1 mm, f/32 (CoC ≈ 0.007 px), instead of adding DOF to every base profile. That keeps the base `.asset` files untouched and covers Office's profile too. `GraphicsStateCollection` is **not adopted** (UNVERIFIED on 6000.3, not needed). Prewarm is skipped on WebGL and mobile. AT-01n |
| B6 | HUD fade claim vs `SetPhase` | **Accepted** | C8 treats Inspect as a HUD phase. `rig.HudFade` (ticked in Inspect) fades the panels over 0.15 s; `ApplyGameplayHudAlpha` already multiplies by it (`FrontRooms3DGame.cs:2260`). The crosshair, prompt and hold bar go at once. The hint has its own alpha (C11) |
| B7 | Clock under the capture harness | **Accepted** | `FrontRoomsInspect.Dt` (§3.5), the same rule as `FrontRoomsZoneReflectionDriver.cs:13`; the cooldown is a counter on that clock |
| B8 | AT-01g cannot run | **Accepted** | `InjectSnapshot` (`FrontRoomsInput.cs:179`), `captureDeltaTime` 1/60, an empty `WorkInFrame` (`FrontRoomsMapWorld.cs:859`) for 30 frames, gameplay-only hash (AT-01g) |
| B9 | Two writers on the key during the return | **Accepted** | The map holds its timeline until `HeldReturned`; the runner's `LateUpdate` drives the return while `Active`, including Closing (C21, §7.1 file 1, AT-02) |
| B10 | Close-ups that end without the game | **Accepted** | `AbortRequested` → `EndInspect` + `CancelShot(shot, 0)` (C12c); `End()` → `Abort()` + `rig.ResetLayers()` (C12b; `End()` only runs from Playing, `:2325`); C12 clears `InspectingNow`. AT-05 |
| B11 | The return ignores the Relay | **Accepted** | `cancel = ShotCancel.Relay` (§3.5); `CancelForRelay` (`:1080`) snaps home in `ChaseFreeSnap` 0.15 s. AT-05 |
| B12 | Transient gate failures fall through to the door | **Accepted** | `CanShowInspect` (hard) and `CanPressInspect` (transient). A transient refusal keeps the target and swallows E (C4, C5). AT-05 |
| B13 | Touch: USE disappears; taps through the shot camera | **Accepted** | C14 sends a visible BACK prompt in Inspect; T2 shows USE in `MenuState.Inspect` regardless; C15 returns from the look-tap while `rig.InShot` and routes taps through the gate. AT-06 |
| B14 | Fixed distance assumes 16:9 | **Accepted** | Merged with A3; FOV read per solve (walker 72°); safe-area rect on phones; AT-01d adds 4:3 and 21:9 |
| B15 | Camera collision margins | **Accepted** | Path spherecast `r_c + 0.01` (0.113 / 0.133 m corner radius); pan and look limits pre-swept; object `CheckSphere`; rig buffer 8 → 16 (§3.2 steps 5–7, C17d). AT-04 |
| B16 | Mouse settle frames on resume to Inspect | **Accepted** | Settle frames on any Paused → Playing or Inspect; C7 skips look while they count (C8, C7) |
| B17 | Compile order across chats | **Accepted** | C7 v1 passes 0 / false; T3 then a one-line follow-up; scroll one step per gesture, re-arm 0.2 s (§7.1 merge order, T3) |
| B18 | Sound hook semantics | **Accepted** | The snapshot keys on `Inspecting(bool)` only; `ShotStarted/ShotEnded` are Foley only; the listener `attenuationObject = rig.Eye` is proposed (§4.3, S1–S4) |
| B19 | CRT private E path | **Accepted** | C26 is blocking before any v1 build with a CRT in the close-up ray (§7.6) |
| B20 | `Spawn` side effects | **Accepted** | Attach only when `Application.isPlaying`; world OBB at query time; `FurniturePile` passes `inspectable: false` (both files are visual-owned: `Office/`). FYI to the map chat: module props gain a component in Play Mode only |
| B21 | Statics survive an R restart | **Accepted** | The gate reads the game's own `autopilot \|\| autoBaseline` (C4); `Abort()` resets all statics, including `DevTargets`; there is no `Enabled` static any more |
| B22 | `EasedWeight`; memory figure | **Accepted** | `ShotHandle.EasedWeight` (C18); 4096² BC7 + mips ≈ 21 MB; iOS/Android cap 2048 (§8.1, §6) |
| B23 | Tests | **Accepted** | AT-01h uses `Time.time` < 2 frames; AT-01k is a check only (grain seeded from `Time.frameCount`, confirmed at `PostProcessUtils.cs:48-52, :102-106`); G14 snapping (§3.5); new AT-01l/m/n/o, AT-05 cases, AT-06 cases, AT-11 rig tests |
