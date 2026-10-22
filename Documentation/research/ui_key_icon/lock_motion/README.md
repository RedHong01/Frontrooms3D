# HUD key B: the zone padlock and the key/lock motion (平面视觉, 2026-10-14)

Red, 2026-10-14:
- Direction **B (the key on its tag) is chosen** for phone and desktop. A, A + chip and C stay in Figma as process history, with no new variations.
- At a locked door the UI shows a **padlock, not a key**, and the padlock echoes the tag: a red round tag means a red round padlock.
- Red also asked for **UI motion** (keeping token use low): the key slides in at the bottom left and its tag swings by inertia. At the crosshair, the dot turns into the padlock, which opens like the iPhone unlock (the shackle lifts and swings over to the other side). Red wants several variations to review.

## 1. The padlock (Figma `3113:6999`, 18 variants)

The padlock wears its zone's tag, so the door's lock and the right key match at a glance. This also matches the world, where the locked door's number plate carries the fob's number, colour and shape (`interactables/05_locked_door_type.md` §6.2).

| Part | Rule (1080p px, 1x) |
|---|---|
| Body | The zone's tag body itself, cloned from the B masters: <br>• rect rim 39.33 × 20.01, r 3.45 <br>• round rim Ø 26.22 <br>• long rim 52.44 × 15.18, r 3.45 <br>It has the zone colour (red `#8C0F0D`, blue `#0F2973`, white `#FFFFFF`) inside a 1 px paper rim (`#F4F1E8`), a paper insert, and the typed number in ink (`#1E1D1A`, Courier Prime Bold outlines). |
| Shackle | Steel, drawn in paper like the ring and the key. <br>• Stroke 2.6, centreline half-width: rect 8.5, round 6.5, long 10.5. <br>• 4 px of straight leg shows above the rim. <br>• Long leg (left) goes 6 px into the rim; short leg (right) 3 px. <br>• Drawn behind the body. |
| Glyph box | 80 × 48. The **pivot is the insert centre (40, 32)**, which sits on the crosshair point (960, 540). The crosshair dot (Ø 16) turns into the insert (round Ø 17.94), so the aim point never moves. |
| Open state | The shackle lifts 5 px and swings 180° round the long leg, ending on the left side. |

Boards:
- **KV11** `3113:7000`: the nine tag ↔ padlock pairs.
- **KV12** `3113:7147`: the prompt at the crosshair, with and without the key, on the almond leaf and on dark.

Both are in section `3113:6872` (x 33937, y 423), above HUD KEY `2532:4038`.

**SVG masters** (for 游戏视觉's pipeline, which renders @1x, @2x and @3x and checks edges under Unity's linear blending):
- Files: `lock_motion/svg/Lock_<rect|round|long>_<red|blue|white>_<closed|open>.svg`, 18 of them, 80 × 48.
- Made by `~/FrontRoomsVisualWork/keyicon_motion/export_lock_svg.py`.
- Body: the B masters' own `tag rim`, `tag body`, `insert` and `number` paths, moved by (40 − cx, 32 − cy). The insert lands on (40, 32) within 0.02 px.
- Shackle: a 48-segment polyline, like the masters' ring; within 0.006 px of the Figma arc.
- Keyline (added 10-14): `<g id="keyline" opacity="0.85">` holds an ink copy of the shackle (stroke 4.6) and of the rim (+1 px stroke), behind everything.

### The locked prompt

**Layout**
- The lock replaces the crosshair dot.
- The prompt text keeps every prompt's lines: Bayon 20 paper on baseline **591**, and IBM Plex Mono 13 muted (`#BDBAB0`) on baseline **614**.

**No card** (Red, 14 Oct: "我不希望有背后的这个黑色底").
Paper on the almond leaf (`#D2CAB5` at the crosshair) is only 1.3–1.5 : 1, so a 1 px ink keyline carries the legibility instead:
- `#141414` at 85 % round every paper edge of the padlock (rim, shackle, and the insert while it stands alone), round the crosshair dot, and round the prompt text.
- In Unity: an `Outline` on the Text (effect distance 1), and the keyline baked into the sprites.
- Both prompt lines are paper. The muted `#BDBAB0` is darker than the leaf and reads hollow inside a keyline.
- The review page compares this with a soft shadow (0, 1, blur 1.2, black 80 %) and with nothing.

**Strings**

| Has the key? | Line 1 | Line 2 | Today's string |
|---|---|---|---|
| No | `LOCKED` | `NEEDS KEY 14` | `LOCKED  ·  NEEDS THIS ZONE'S KEY` (`FrontRoomsMapWorld.cs:2455`) |
| Yes | `E  ·  UNLOCK` (proposal) | `KEY 14` | `E  ·  OPEN DOOR` (a locked door with your key isn't `LockedHere`) |

Bayon has no `·`; Figma draws it in IBM Plex Mono.

## 2. Motion variations (review page; Red picks one of each)

- **Review page:** https://claude.ai/artifact/ArLGmLysHRtswHQjWKSthC (private to Red).
- **Source:** `~/FrontRoomsVisualWork/keyicon_motion/lab_template_v2.html` (the v1 file, renamed `lab_template_v1_superseded.html`, has no L3·D and no keyline). `assets.py` builds `out/key_motion_lab.html` with the master paths and frame crops.
- **Units:** times are ms at 1×, distances are 1080p px. Phone uses the same motion at the touch scale.

### Pickup (bottom-left panel: rule 4 × 48 at x 72, glyph at (90, 914), label x 176, baselines 936 / 955)

The swing is a damped oscillator driven by the pivot's horizontal acceleration, scaled to the peak angle in the table. The pivot is the key bow (35, 10 in the glyph) for the ring + tag, and the tab hole (27.22, 18.04) for the tag in P2.

| | Entry | Swing | Text |
|---|---|---|---|
| **P1 Slide in, settle** (Red's brief) | −96 → 90 px in 560 ms: cruise 35 %, then a smootherstep brake | Ring + tag as one: 1.6 Hz, damping 0.11, peak 16° | Rule grows 160 ms at 420. Label 220 ms from −8 px at 500. Meta at 600 |
| **P2 Spring in, double swing** | The whole strip springs in: 2,120 px/s, 2.6 Hz, damping 0.7, overshoots 8 px at 270 ms | Two joints: ring 2.4 Hz / 9°, tag 1.35 Hz / 21°, lagging | Rides the strip 60 ms behind the key |
| **P3 Thrown in, hard stop** | 170 ms at 1,005 px/s, then a 30 ms stop | 1.9 Hz, damping 0.10, peak 26°. The key rocks −3.5° at 4.5 Hz | Rule snaps on. Label pops 112 % → 100 % in 140 ms. Meta types at 16 ms a character |
| **P4 From the pickup shot · PICKED** (Red, 14 Oct) | Starts where the game's pickup shot holds the key: viewport (0.65, 0.30) = (1248, 756) at 1080p, 3× (the 3D key is about 217 px wide there; `FrontRoomsShotTimings.KeyPickup`, `FrontRoomsDoorShot.cs`). At HoldUntil (0.75 s) the 3D key hides and the HUD key takes off from that spot. It reaches the slot at 1× in 720 ms, ease in-out, dipping on a curve. This replaces the old drop out of frame and the 0.25 s panel fade | The held swing (25°, 2.2 Hz, decay 2.5/s from 0.25 s; about 7° left at take-off) keeps decaying, plus a landing swing at 1.7 Hz, damping 0.12, peak 18° | After take-off: rule grows 160 ms at +680. Label 220 ms from −8 px at +740. Meta at +840 |

### Lock at the crosshair

Red, 14 Oct, on L3: "利用好鼠标中心的原点，而不是直接覆盖一个动效在上面". The padlock is built out of the crosshair dot and goes back into it. Five versions of that logic are in review (L3·A–E). The first round's L1 (the dot grows into the insert, the shackle rises) and L2 (drops on, springs open) stay on the page as history.

**Sequence**
1. The aim lands on the door after 350 ms.
2. The lock builds out of the dot.
3. E is pressed 600 ms later. The reaction (no key or has key) plays for 500 ms.
4. The lock holds: 900 ms with no key, 550 ms with the key.
5. It folds back into the dot by running its build backwards: 320 ms (400 ms for L3·C).

With the key, every version opens the same way:
- lift 5 px in 150 ms;
- a 180° swing round the long leg in 300 ms (in-out, drawn as a true rotation: projected width = cos φ);
- as the lock folds back, the open shackle fades (in L3·C the pen undraws it).

Without the key, the prompt's second line also dims to 30 % twice, for 100 ms each time, 200 ms apart.

| | Build (in) | No key |
|---|---|---|
| **L3·A The rim draws round the dot** (460 ms) | Dot → insert 140 ms. A paper rim draws clockwise 185 ms. Colour floods 115 ms. Shackle draws 175 ms | Swings ±7° on the shackle at 7 Hz (decay 6/s). Band flashes paper 120 ms |
| **L3·B A ripple from the dot** (500 ms) | One ring runs from the dot to 135 % of the rim and fades, 300 ms. Colour swells from the insert with a slight overshoot, 225 ms. Shackle rises 200 ms. Digits type at 390 and 450 ms | A ring from the insert to 150 %, 175 ms. Band flashes paper 120 ms. Shake ±1.5 px at 10 Hz |
| **L3·C One line from the dot** (700 ms) | The dot becomes a pen (85 ms) and draws one line: up the long leg and over the shackle (190 ms), round the rim clockwise (210 ms), back to the centre (55 ms), where it swells into the insert (100 ms). Colour fills 85 ms. Digits at 630 and 670 ms | Twists ±6° round the aim point at 8 Hz (decay 7/s). Band flashes paper 120 ms |
| **L3·D The dot opens like an iris · PICKED** (Red, 14 Oct: "我需要L3D"; 500 ms) | The dot swells into the whole tag in paper, 150 ms. The colour closes in from the edge to the insert, like an iris stopping down, 175 ms. Shackle rises 190 ms. Digits at 400 and 450 ms | Two 160 ms blinks (the insert opens back to the whole tag and shuts), 100 ms apart |
| **L3·E Three clicks from the dot** (620 ms) | Beats at 43, 248 and 453 ms, each a 110 % pulse round the aim point for 87 ms. 1: insert + rim outline. 2: colour. 3: the shackle snaps up (back-out) and the number appears. Pairs with three small sounds | Two jumps, 2.4 px up and dropped back, 200 ms apart, each with a 5 % pulse. Band flashes paper 100 ms |

### Build notes for L3·D (for the HUD code)

**Layers, not one sprite.** The iris changes the insert and body sizes continuously, so the lock is drawn as parts. Back to front:
1. keyline (ink, in one 85 % group);
2. shackle;
3. rim (paper);
4. body (zone colour);
5. insert (paper);
6. number (ink).

Rect and long are rounded rects (9-slice): rim r 3.45, body r 2.45, insert r 1.035, all at 1x. Round is circles. The flattened `Lock_*_closed/open.svg` stay the reference for the end states.

**State at progress u (0 → 1 over 500 ms).** Sizes are w × h × r. The rim is always the body + 2 (r + 1).
- **Body** = lerp((14, 14, 7) → body, eoc(clamp(u / 0.3))). At u = 0 the rim is the 16 px crosshair dot.
- **Insert** = lerp(body → insert, eio(clamp((u − 0.3) / 0.35))). Until u = 0.3 it covers the colour, so the tag is all paper.
- **Shackle y offset** = hide · (1 − eob(clamp((u − 0.62) / 0.38))), where hide = a + 6.8: rect 15.3, round 13.3, long 17.3. The shackle is drawn behind the body and clipped below the long leg's foot (rim top + 6).
- **Digits:** the first appears at u 0.8 (400 ms), the second at 0.9 (450 ms).
- **Prompt text alpha** = clamp(u / 0.6).
- **Easings:**
  - eoc(t) = 1 − (1 − t)³
  - eio(t) = t < 0.5 ? 4t³ : 1 − (2 − 2t)³ / 2
  - eob(t) = 1 + 2.7 (t − 1)³ + 1.7 (t − 1)²

**No key (E pressed): D·3 picked (Red 10-14).** The two iris blinks were rejected: "没有明确的communicate 打不开这个概念". Four reactions are in review. Each keeps the build and the has-key swing, and runs in the 500 ms reaction window. They are the `DENY` functions in `lab_template_v2.html`.
- **D·1 Tug.** Two tugs, 220 ms apart. The shackle accelerates up 2.5 px into its stop in 50 ms, holds 20 ms, drops back in 50 ms and clacks 0.6 px past rest over 80 ms. The body dips 0.8 px at each stop.
- **D·2 Shake.** The padlock and both prompt lines shake ±3.5 px at 8 Hz for 400 ms, dying away as (1 − t/400)².
- **D·3 Tug, then shake · PICKED (Red, 14 Oct).** One tug as in D·1 (0–200 ms), then the D·2 shake from 140 ms for 360 ms.
- **D·4 Strain.** The shackle strains up 2 px in 180 ms, then quivers ±0.35 px at 26 Hz against the stop for 180 ms while the body trembles ±0.3 px. It drops in 60 ms and clacks 0.6 px over 80 ms.

The second-line dimming went with the blink.

**Has key (the key starts turning).**
- Lift = 5 · eoc(clamp(r / 0.15)).
- Swing k = cos(π · eio(clamp((r − 0.12) / 0.3))). The free (short) leg sits at x = −a + 2ak, and the arc radii are (a·|k|, a).
- The shackle keeps its 2.6 px width all through the swing. Draw it as a small mesh (a UI Graphic polyline), not as a sprite scaled by k: a scaled sprite thins to nothing when seen edge-on.
- Prompt alpha = 1 − clamp(r / 0.12).

**After it opens: the lock falls. F·1 picked (Red 10-14).** Red: "被打开的锁在打开之后应该掉落（然后渐隐）类似于锁自然坠落的效果". The has-key exit no longer folds back into the dot. After the swing (r = 0.5 s) there's a 60 ms beat, then the padlock falls. These are `FALL` and `fallOut` in the lab. The crosshair dot fades back in at the centre during the fall.
- **F·1 Drop · PICKED** (`FALL.drop`, `fallDur` 0.55; pivot = the lock origin): 560 px/s² (about 70 px in 0.5 s), tipping 10°. Fades from 150 ms over 350 ms. The dot comes back from 250 ms. 550 ms.
- **F·2 Drop and bounce:** falls 46 px onto an unseen floor in 300 ms (1,020 px/s²). One 7 px bounce over 240 ms, rests 100 ms, fades 250 ms. 900 ms.
- **F·3 Slips off and tumbles:** turns 35° round the shackle tip in 150 ms, then falls (600 px/s²) spinning 220°/s. Fades 250–600 ms. 650 ms.
- **F·4 Falls away:** falls (520 px/s²) while shrinking to 70 %, tipping 6°. Fades from 120 ms. 600 ms.

**Leaving (no key).**
- The build runs backwards over 320 ms: u = 1 − t / 0.32.
- After an unlock the lock now falls instead (above).
- The crosshair dot shows again at the end.

**Keyline.**
- Every paper part gets an ink copy 1 px larger behind it, all inside one 85 % group so overlaps don't double up.
- On the crosshair dot and both prompt lines: Unity Outline, #141414 at 85 %, distance 1 (× the touch scale on phone).

**Time.** Unscaled throughout.

## 2b. Broken door at the crosshair (Red, 14 Oct): B·4 picked

Red: "如果门已经被破坏了，在游戏机制上这个门应该无法被关上，那么准星也应该有对应的motion代表门坏了".
- **Permanent:** Red answered 系统设计's Q9 with "门一直坏着": a broken door stays broken for the run.
- **The rule** is owned by 系统设计: `RELAY_PURSUIT_REDESIGN.md` §10 → "Broken doors (Red, 2026-10-14…)".
  - Broken is permanent for the run and the door stays open.
  - At the hanging leaf within 2.4 m, `Describe` returns a broken prompt kind.
  - E returns `UseResult.Broken`, and the HUD plays "won't shut" with no Relay noise.
  - There is no HUD state while the Relay is breaking the door.
  - Touch shows the same state.
- **Crosshair:** built from the dot like the lock. Red, 10-14: "很多图标没有门的廓形". B·4–B·8 therefore draw a real door (frame + leaf + knob); B·1–B·3 are the first, abstract round. The prompt is `BROKEN` / `WON'T SHUT` (proposed), paper with the keyline.
- **Timing:** the build takes 340–500 ms. E opens a 550 ms reaction window, which is also the retrigger guard. Leaving takes 300 ms.

| | In | E (tries to shut) |
|---|---|---|
| **B·4 Hangs off its hinge · PICKED** (`DOOR_BROKEN` 'B·4') | The dot becomes a door (frame U 16 × 26, 2 px; leaf 11 × 23.5; knob), to 440 ms. Then the leaf drops 1.5 px and swings to 12° round its top hinge (2.4 Hz, damping 0.3). This matches the in-game look: open and crooked | Swings up to 3° in 90 ms, hits the frame, falls back (2.6 Hz, damping 0.3) |
| **B·5 Split down the middle** | Door builds. A jagged crack runs down the leaf in 80 ms, the halves part 1.6 px and turn ±5°, and two splinters fly | The halves snap together in 60 ms and spring apart, 300 ms |
| **B·6 Burst at the lock** (recommended) | Door builds. A ragged hole bursts open at the knob in 120 ms, the knob flies off and three splinters spray (400 ms). The leaf hangs 4° loose. This matches the Relay's burst damage | The leaf rattles in the frame: ±1.1 px, ±1.5° at 14 Hz, dying over 320 ms |
| **B·7 Swung open, won't latch** | Door builds, then swings open to 62° on a spring (the leaf narrows and skews on its hinge) and tilts 4° | Swings to 12° in 120 ms, bounces off the frame, swings back open |
| **B·8 Fallen out of its frame** | Door builds. The leaf falls 68° round its bottom corner (accelerating, 280 ms) and bounces 7°, leaving the doorway empty | Lifts 26° in 100 ms, slumps back with a 4° bounce |
| **B·1 The dot cracks** (abstract, first round) | A crack draws across the dot in 100 ms. The halves part 1.8 px and turn ±7° (back-out), 240 ms, and a chip drops | The halves snap together in 70 ms, then spring apart with an overshoot, 380 ms |
| **B·2 The dot sags off its hinge** (abstract, first round) | Dot → a 10 × 18 leaf in 150 ms, then it sags to 22° round its top hinge (2.6 Hz, damping 0.25) | Swings up to 2° in 120 ms and drops back past 22° (2.4 Hz, damping 0.22) |
| **B·3 A ring that won't close** (abstract, first round) | The dot empties into a ring (r 6.9, 2.6 px) over 180 ms, and a 55° gap opens | The gap closes to 6° in 90 ms and springs back open, 320 ms. At 16 px it can read as a loading spinner |

## 2d. Unlock: the key answers in its own place (Red, 14 Oct): U·3 picked

Red first asked for the key to go into the padlock and turn. Those versions (K·1–K·4) are now history. Red then said: "钥匙不需要插在锁上，而是只本来作为collectable的钥匙的位置进行motion design" (with a screenshot of the bottom-left key panel).
- The collected key makes the unlock gesture in its panel, while the crosshair padlock swings open and drops (F·1).
- The variants are `UNLOCK` (`patches/p12_unlock.js`). r = seconds since E; values are in glyph px, about the key tip (71, 10).

| | Key in the panel |
|---|---|
| **U·1 Push and fold** | Push 6 px right in 100 ms. Fold to 15 % height (side-on turn) in 180 ms, unfold in 140 ms. Back in 180 ms from 420 ms. Tag kicks −9° / +6° |
| **U·2 Push and twist** | Push 6 px. Twist −22° round the tip in 200 ms, hold 150 ms, back in 150 ms. Back out from 600 ms. The tag counter-swings to stay hanging |
| **U·3 Push, fold, click · PICKED** | U·1, plus at 240 ms the yellow rule widens 4 → 6.4 px and back (140 ms) and the key bounces 1 px |
| **U·4 Turn in place** | No push. Fold to 15 % in 150 ms, unfold in 170 ms. Tag −5° |

**Conflict with the unlock shot (游戏视觉 DECISIONS #13).**
- The map's unlock shot starts on the same frame (IsHeldForKey) and fades both the crosshair and the key panel within 0.15 s (`ApplyGameplayHudAlpha`: `keyHudGroup.alpha = hud`). They stay hidden until 1.45 s.
- The shot itself shows the real key going in and turning: KeyInsert 0.55, KeyTurn 0.68, commit 0.86 s.
- The options for Red:
  - **(b)** Everything plays after the HUD returns.
  - **(d)** The key panel stays visible during the shot and gestures in sync with the real key; the crosshair returns at 1.45 s with the padlock open, then the drop.
- **(d) is feasible** (游戏视觉, 10-14):
  - The sync uses the map's existing `FrontRoomsDoorShot.Beat` event (KeyInsert 0.55, KeyTurn 0.68, BoltRetract 0.86 s). U·3 starts at KeyInsert, so the click lands at 0.79–0.93 s, around the bolt retract. If the shot ends before BoltRetract, the gesture reverses.
  - Keeping the panel visible needs one line in `ApplyGameplayHudAlpha`, through the HK1 wiring contract: `keyHudGroup.alpha = (keyHud.PanelShotExempt ? gameplayHudAlpha : hud) * keyPanelFade`.
  - The build defaults to (b), with (d) behind a switch. In (b), the gesture starts when the HUD returns (1.45 s).
- **Preview:** the in-context section has a switch, "(d) Panel stays, syncs / (b) All after the shot / No shot", with a dashed placeholder for the 1.45 s game camera. Red decides.

## 2c. In context: desktop and phone mockups (Red, 14 Oct)

Red: "我需要你给我看一下在手机画幅上的交互mock up以及电脑画幅两种让我预览". The review page's first section plays the picked set (P4, L3·D with D·3 and F·1, B·4) full screen, in sync, on two devices.
- **Desktop:** 1920 × 1080.
- **iPhone landscape:** 874 × 402 pt, with the TOUCH_CONTROLS §3 layout: stick at (150, 290), USE Ø72 at (740, 262) showing its verb (LOCKED / UNLOCK / BROKEN).
- **Phone HUD:** the key panel moves to the top left at (78, 72) at 0.85, on the touch type ramp. The crosshair cluster scales from the 16 px dot to the 12 pt one (0.75). The prompt text is 17 / 11 pt on baselines 239 / 259, and on phone the with-key prompt reads `UNLOCK` (no E key).
- **P4 on phone:** it flies from the held spot (0.65, 0.30) = (568, 281) pt at 1.12 to the top-left panel.
- **Source:** `patches/p10_*` in `~/FrontRoomsVisualWork/keyicon_motion`.

## 2e. Look-eye at the crosshair (Red, 15 Oct): E·1 picked

Red's brief (via 平面视觉 · 信息设计): where there is something to look at, the crosshair becomes an eye that blinks. Build it from the dot, which becomes the pupil. It blinks once, slowly, in paper with the keyline. It never stares, pulses or turns red, so it reads as "you can look", not "it sees you".

Red picked E·1 on 15 Oct ("选择这个方案"). E·2 (shuts, then opens once), E·3 (reading eye: heavy-lidded, gaze lowered) and E·4 (one lid over the dot) stay on the review page as history, with X·0, the staring red eye to avoid.

**Source.** `LOOK` 'E·1' (`inPose`, `rest`, `focus`), `lookPose`, `eyeMake`, `eyeDraw`, `lidD`, `lerpE`, `slowBlink`, `lookTimes`, `LKT`, `EYE`, `DOTEYE` in `lab_template_v2.html` (patches `p16_look.js`, `p16_apply.py`, `p17_apply.py`). In-context scene: "Look at a plan" (desktop and phone). Review page v16.

**Geometry** (1080p px round the crosshair point, SVG y-down; × 0.75 on phone):

| Part | Rule |
|---|---|
| Lids | Two cubic arcs from (−w, 0) to (w, 0), controls (−w·c, 4a/3) and (w·c, 4a/3), so the apex is a (upper `up`, lower `lo`). Paper `#F4F1E8`, stroke 2, round caps |
| Pupil | The dot itself, `#FFFFFF`, an ellipse of r `pr` at (0, `pdy`), clipped to the opening between the lids |
| Keyline | `#141414` in one 85 % group, behind: the lids at stroke 4, the pupil at r + 1 |
| DOTEYE | w 8, up −8, lo 8, pr 8, c 1: a circle, so the first frame is the dot |
| REST | w 20, up −8, lo 7, pr 5, c 0.45: a 40 × 15 px almond, the upper lid slightly lowered |
| FOCUS | up −9.5, lo 8, pr 4 |

**Timing** (unscaled time; revised 15 Oct to 系统设计's `computer_ui/30_crosshair_grammar.md` §2, patch p18, review page v17):
- **In,** 1.05 s:
  - DOTEYE → REST in 0.30 s (eio). It reads as an eye by 0.25 s. The lids fade in over 0.08 s.
  - Prompt alpha = clamp(t / 0.30): the prompt arrives with the eye.
  - One slow blink from 0.42 s: the upper lid closes onto the lower in 0.22 s (ease-in, s²), holds 0.07 s, opens in 0.34 s (eoc).
  - Envelope (grammar §2, amended 10-15): readable as an eye ≤ 0.25 s, prompt in ≤ 0.30 s; one slow blink ≤ 0.63 s starting ≤ 0.45 s; total ≤ 1.05 s. A switch during the blink cuts it like any leave.
- **Rest:** still. One blink per build. A re-blink needs the aim to leave **and** ≥ 1.0 s since the last blink started; otherwise the eye builds without a blink.
- **E,** only on `InspectOpened`: focus in 80 ms (eoc). The eye and the prompt fade over 50–150 ms, with the close-up's HUD fade.
- **Back from the close-up,** still aimed: fades in at REST in 0.15 s, no blink.
- **Leaving,** 0.30 s: shuts in 0.12 s (eio), then shrinks into DOTEYE by 0.30 s (pupil 5 → 8). The lids fade over 0.24–0.30 s.
  - When another state is waiting, the same curve is cut to 0.12 s.
  - Re-aiming mid-leave resumes from the current point.
- **Reduce Flashing:** no blink; the eye opens straight to rest in 0.30 s.
- **Reduce Motion:** an alpha cross-fade ≤ 0.15 s (≤ 0.108 s on touch).
- **First key of a run** (EXAMINE §2.5 K): during the held-key pause the dot becomes the eye at the crosshair, with `E · EXAMINE KEY` in the normal slot. There is no glyph in the prompt slot. P4 takes off when that hold ends, not at a fixed 0.75 s.

**Prompt.** EXAMINE G1's verb + object (`E · READ PLAN`, `E · READ SIGN`, `E · EXAMINE KEY`, `E · READ SCREEN`). On phone the E is dropped and the USE verb is READ or EXAMINE. Strings with Red.

**Hand-offs (15 Oct):**
- 游戏视觉: DECISIONS #16. Draw the lids as UI line meshes; clip the pupil analytically between the lid curves.
- 系统设计: the trigger and honesty gate, priority against LOCKED and BROKEN, touch, and Flashing, all in `computer_ui/30_crosshair_grammar.md`.

## 3. Open points

- `doorsNeedKeys` is off in Level 0, so LOCKED can't be reached in the shipped profile yet.
- With the key, the game starts the key shot and hides the prompt (`IsHeldForKey`). The unlock motion should start on that same frame.
- All HUD key and lock motion runs on **unscaled time** (游戏视觉, 10-14). Otherwise it stops mid-swing whenever the game is paused (EXAMINE, the pause menu).
- Strings `E  ·  UNLOCK` / `KEY 14` are proposals for Red.
- Sprites: 游戏视觉's key-icon pipeline makes the B and padlock PNGs (@1x, @2x, @3x). The padlock's pivot is (40, 32) in the 80 × 48 box, which is (0.5, 0.333) in Unity's bottom-left pivot.
- Done 10-14, after the L3·D pick: KV12 has no card; the keyline is in the 18 Figma components and in the SVG masters (`<g id="keyline">`).
- P4 hand-off needs the map side: hide the 3D key at HoldUntil and drop the pocket fall. 游戏视觉 sent this to level design on 10-14. The pose check waits for a held-position frame from D1.6 KeyAssembly, because the in-game key is still a cube.
- No VL entry yet: nothing is in the game to verify. One gets added when the game draws the padlock.
