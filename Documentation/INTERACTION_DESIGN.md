# Interaction design (交互设计): how the player's hands work on the world

Owner: the 交互设计 session (since 2026-10-22; Red: "我需要你给我专门开一个窗口叫做交互设计，负责处理交互类的问题"). This file holds the interaction rules: doors, windows, keys, E prompts and the touch layer. Touch details stay in `TOUCH_CONTROLS.md`. System-level decisions are recorded by 系统设计 in `system_design/systems.json`; this file holds the rules derived from them and how they are built.

Code that other lines own lands through its owner: door code through 关卡设计 (`FrontRoomsMap/*`, `FrontRooms3DGame*.cs`, `FrontRoomsShots/*`), door/lock/key models through 游戏视觉, door Foley through 声音设计.

---

## A12 · Doors swing away from the player (double-acting doors)

**Status 2026-10-22:** **in main since 15:11** (关卡设计's landing, LANDINGS 15:11; see "In main" below). First built and verified in the private clone `W/proj_a12`.

**Red's request (2026-10-22, in the 系统设计 chat):** "当前门在被打开的时候我需要它应该是玩家反方向打开，现在门的打开方向直接是对着玩家打开的这一点不合理，每次玩家都会撞到门".

**Red's decision (2026-10-22, in the 交互设计 chat):** "全部按推荐：A 双向门，保留把手，站在门板那边关门时照现在让一步，Alpha 之后上。" Recorded by 系统设计 as A19 (A12 points to it).

### What was wrong

Map doors were single-acting, with the swing side a 50/50 hash per edge (`FixedSwing`), so about half of all opens came toward the player. A pull stepped the player aside for up to 0.35 s, but under the Relay's sight or a chase the step was skipped and the leaf stopped on the player's body.

### Options Red saw

| Option | What | Cost | Verdict |
|---|---|---|---|
| **A** | Double-acting everywhere: no stops, top/bottom pivots, radius hinge stile, seal fins (the researched Option B, `research/interactables/04_door_re8_gap.md` §5) | map code ~1 day; 游戏视觉 ≈ 1.5 days for 3 frames + 6 leaves | **chosen** |
| B | Free doors double-acting, locked doors single-acting as built | 游戏视觉 ≈ 0.75–1 day | a locked door pulls again on the way back |
| C | Code only, the single-acting look kept | cheapest | the leaf cuts through the stops and the hinge jamb |

Sub-calls, all as recommended: keep the handles (lever on free doors, knob + deadbolt on locked ones; a game liberty, since real double-acting doors rarely latch); shutting from the leaf's side keeps today's step aside; after the Alpha.

### The rules

1. **From the frame, away from the opener.** While a leaf is latched, its swing side becomes the far side of whoever moves it (`FrontRoomsMapWorld.SwingAwayFrom`): every open, a locked rattle (the jolts go away from the trier), a key turn, and a Relay break (the hunter calls it as the break begins).
2. **Opens never pull.** No pull beat, no `DoorPulled` on an open. `door.pull` is false after an open.
3. **A leaf out of the frame goes on away from its opener.** Caught mid-shut, or resting on a body: with the opener behind it (their bearing from the hinge in the door plan short of the leaf's angle), it turns back open; with the opener ahead of it, it runs back through the frame at the open's starting speed (345°/s) and opens to the far side (`DoorMotion.Through`, one `DoorMoved`, no `DoorLatched` on the way, never resting at 0° for more than a frame).
4. **The side is kept.** `doorSides[edge]` remembers where each leaf last went, so open and broken doors are rebuilt on their side through rebuilds and revisit shifts. A door never opened rests on `FixedSwing`.
5. **Doors stay where they are left.** No self-centring and no closer (the Foley rule: normal wooden doors).
6. **Shutting** is unchanged: the leaf returns to the frame on its own side. From the side the leaf is open to, with the body inside its return sweep (≈ 1.35 m from the hinge), `DoorPulled` gives a clear spot and the game steps the player there (≤ 0.35 s at 4 m/s); under the Relay's sight or a chase there is no step and the leaf waits on the body. From the other side the leaf never reaches the player.
7. **Unlock.** The key turn makes the shut leaf swing away from the key's side, so every unlock is the push path: the knob turns at 0.92 s and the leaf leaves the frame at 1.00 s, away from the camera. The 1.55 s pull branch stays in `FrontRoomsDoorShot` for a leaf out of the frame and is never reached from the game.
8. **Faces.** `FaceOf` is geometric: S = the door root's +X face, P = −X (the kit's authored faces; the double-acting rig is not mirrored).
9. **The Relay.** A shut leaf bursts away from the Relay, into the room it breaks into; it shoulders the door in front of the opening. An opened door is never broken: when a leaf opened from the far side swings toward the Relay (it can rest on its body short of 70°), the Relay steps clear of the sweep and walks through once the leaf passes 70°, and a break in progress ends the moment the door is opened. v2's OPEN leaves the door ajar on the side away from the Relay; the player's E on an ajar leaf follows rule 3. Path and hearing costs read only how far a door is open, never its side.
10. **Bodies.** A moving leaf never enters a body: it stops on the player or the Relay and goes on once they are clear (`LimitByBodies`). Both faces of every door already keep a 1.2 m strip clear of collider props, and the 0.98 m leaf cannot reach the side walls.
11. **Both platforms.** It is a map rule, not platform-gated. The touch USE button and tapping a door both go through `UseDoor`, as E does.
12. **The EXIT and the stairwell doors** follow the same rule (关卡设计's C22 builds the EXIT as an ordinary map door).

### Hardware (游戏视觉, D1.6)

Double-acting everywhere as 10-02 Option B: the 3 frames and 6 leaves rebuilt stopless with pivot caps, a radius hinge stile, 3 mm gaps, seal fins on the centre plane, the sleeve, a 12 mm saddle, casing on both faces, LOD0/1/2; no self-centring; surface closers off on map doors; no A2 hinge offset. Lever on free doors; knob + deadbolt on locked doors with the keyhole ≈ 1.0 m on both faces. `DoorFace` geometric, `door.swing` set by the map at each open, the rig turning ±angle about the pivot. The single-acting mode stays a dormant rig parameter.

### Sound (声音设计)

No new layers. `FrontRoomsDoorSound` reads an unsigned hinge angle, so either swing direction works unchanged. A push-through crosses 0° at speed and never rests there, so no LatchStrike or Handle fires at the crossing. The butt hinges become pivots (the creak takes may need a re-audit). Note in `briefs/sound.md` Inbox.

### Verification (clone `W/proj_a12`, 2026-10-22)

- **Suite** (关卡设计's `suite.sh`, run in the clone): MapInteractionTests 177 passed, 0 failed (26 new double-acting checks: opens from alternating sides, rebuilds and shifts, both faces, the turn-over from the leaf's old side and from beside the hinge, the shut step, the resting leaf turning back, the push-through, the Relay's body; the opener is never touched); Relay navigation 60/60 with 23 breaks, all bursting away from the Relay; DoorShotTests 234 passed (every unlock is a push, from either face); glass 20, rig 33, director 5, captions 9, fixture 28, seeds 100/100, designer 140; autopilot 2554, 20388, glass and doors PASS with 0 errors (the leaf-side unlock opens at 1.012 s with the latch drawn, into the cell beyond the player); touch playtest all passed. Roslyn: 0 errors for macOS and iOS.
- **Capture:** VL348 and `research/interaction/a12/A12_capture.md` (first-person video, 20.5 s).
- **Hand-off:** one diff with every test rewrite went to 关卡设计 against the LANDINGS bases (r2 after its W1 hotfix); it landed 2026-10-22 15:11, once Red lifted the Alpha gate.

### In main (关卡设计's landing, 2026-10-22 15:11)

- **What landed:** our r1 code and tests plus 关卡设计's review fixes, 3-way onto M2 and the phone top band, with r2's MAP_GENERATION hunk. MapWorld 6041880b, Game 3b8f1cfd, Hunter 080fe83f, KitSeam dba67c53, DoorShot b2014396, MapInteractionTests 4d3dbe9b, DoorShotTests abc598da, RelayNavTest 8ebd41f4, MAP_GENERATION.md 75802604, LEVEL_MODULE_SPEC.md 53ef2ea1. Review: `~/FrontRoomsMapWork/notes/a12_review_20261022.md` (no blocker or major).
- **What 关卡设计 added to our package:**
  - `SwingAwayFrom` before every `JoltForBlow`, by edge, so the Relay's blows follow rule 1 too;
  - `FrontRoomsMapWorld.DoubleActingDoors => true`, which 关卡设计调研's bots read (Baseline flags);
  - stale comments fixed: Game `OnDoorPulled`, the pull step and `BeginUnlock`; DoorShot `UnlockStepIn` and the pull branch; MapWorld `DoorHandleTurned`, `JoltLeaf` and `NearestClosedDoorForTools`;
  - tests: `RelayRipsHeldLeaf` (6 checks, mutation-proved), the navigation rule `breaksFromSwingSideLatched == 0`, DoorShotTests `PullBranch`.
- **MAP_GENERATION.md** states the same rules: every unlock is a push, the Relay bursts a shut door away from itself, and the autopilot's door case 3 is "unlock (leaf side)" (frames `NN_door_unlock_leafside_tNNN.png`).
- **Checks on the landing:** interaction 190/190, Relay navigation 60/60, DoorShotTests 245/245, `-autopilotDoors` 2554 PASS (the leaf-side unlock is a push); Roslyn macOS + iOS on main.

---

## C14 on phones · Keys are taken on purpose (look + tap)

**Status 2026-10-22:** the touch part is in main (`FrontRooms3DGame.Mobile.cs` handheld block, the touch playtest). It is inert until 关卡设计's one line `map.KeyPickupNeedsE = true` lands in `FrontRooms3DGame.cs`. A12 landed at 15:11 without it; 关卡设计 lands the line last in its post-Alpha chain (after C22), as a `git apply` on the final Game.cs. It applies clean on Game 3b8f1cfd and was verified with A12 in the clone `W/proj_caught` (15:2x). Until it lands, phones keep walk-over keys.

**Decisions:** Red, 2026-10-21, in the 系统设计 chat: "系统设计这边也全部按推荐（包括更正）" (C14 = look + E for every key; phones once the tap can target a key). Red, 2026-10-22, in the 交互设计 chat: "接着做下一项：手机上看着钥匙点按就能拿。"

### The rules

1. **USE:** with the dot on a key, USE says TAKE; a press takes it through `UseDoor` → `map.Use` (`KeyTaken`).
2. **Tap on the key itself** (Apple HIG direct touch), with the dot anywhere: `HandleMobileLookTap` asks `map.KeyOnRay` first (a key is a trigger, which the solid raycast ignores), then `AimCast` for doors.
3. **Finger target:** when nothing usable is under the finger, a key within 22 pt of the tap is taken. Two rings of 8 rays at 11 and 22 pt give Apple's 44 pt minimum.
4. **Same road as E:** a shot that takes USE swallows the tap.
5. **Walking over a key leaves it** (as on desktop since 10-21).

### Verification

Package `~/FrontRoomsInteractionWork/keytap/package/` (KEYTAP_CONTRACT.md). With the line: touch playtest 83/83 in `W/proj_keytap`, 91/91 in `W/proj_broken` (run b3). On main today the key-tap steps skip with a note.

---

## C11 on phones · A broken door: USE says BROKEN

**Status 2026-10-22:** in main.

**Red's request (2026-10-22, in the 交互设计 chat):** "做下一项：手机上对着被撞坏的门时，USE 按钮显示门已经坏了。" The strings were already decided (C11, Red 2026-10-22: "the phone verb is BROKEN"; `research/desktop_ui/FIX_CARD_VL217.md` §B).

### The rules

1. **USE's verb comes from the map's `AimKind`** (H4 in `research/computer_ui/30_crosshair_grammar.md`), never from the prompt's words: Open, Shut, TakeKey → TAKE, Locked, Broken, Glass / GlassTap → hold. `LockedHaveKey` keeps OPEN until the A22 strings land (then UNLOCK).
2. **Finding the broken door:** `Describe` gives a broken door no prompt, so `UpdateAim` leaves `aimed` empty. When nothing usable is aimed (and no close-up target), the handheld block casts the same base-eye ray (`AimCast`, within reach), and only a broken door counts there.
3. **It looks like LOCKED** (both refuse): the verb in muted ink on the solid disc, the ring at 30 %. A press never fills yellow; the ring flashes to full paper.
4. **A press, or a tap on the leaf, is the won't-shut try:** `UseDoor` → `map.Use` → `UseResult.Broken` + `DoorBrokenTried`. There is no door verb and no Relay noise, and the map's 0.55 s guard (`BrokenDoor.Duration`) drops a repeat. A key under or near the finger wins over a broken door. If a later Game.cs aims at broken doors itself (游戏视觉's KeyHud wiring does), `UpdateAim` makes the try and the handheld block does not.
5. **The answer** comes from the map's event, so it obeys the same guard:
   - one soft knock (Gameplay channel, soft 0.6) at +90 ms, duller than the locked rattle's two rigid jolts;
   - USE swings once round its top point, like the leaf on its hinge: B·4's won't-shut curve (Red 10-14), 4° in 90 ms, then 2.6 Hz with damping 0.3, still by 550 ms, only for the door USE names;
   - LOCKED keeps its sideways shake. Reduce Motion: no swing.
6. **Still to come (游戏视觉, after the Alpha):** the crosshair's B·4 door glyph and the BROKEN / WON'T SHUT words under the dot, on both platforms.

### Verification

VL353 (Figma `3520:7362`), re-shot on main 5acc5147 + this landing: touch playtest iPhone 85/85 and Android 20:9 85/85, with 7 broken-door checks. Desktop autopilot 2554 PASS, 0 errors, the same start timings, no touch line in the log. Roslyn: 0 errors for macOS and iOS.

---

## X2 on phones · Hints live on the controls, not on a card

**Status 2026-10-22:** in main (20:1x). Step 1, M1, landed jointly at 17:3x: 关卡设计's Game.cs side and my Mobile.cs hunk took the calm card out. Then came 平面视觉's phone HUD hunks (Game a2e212ef, 938b0ee9), the Bayon "·" fallback (游戏视觉, Bayon-Regular.ttf.meta c68a3a3b), and X2 itself: the Mobile.cs handheld block and the touch view, controls, sprites and playtest driver.

**Why.** The phone used to explain itself with the desktop's calm-hint card: a dark card at the bottom of the screen with a sentence about the stick. It covered the room, it was the only dark card left in the HUD, and it told the player the slow gait's rule once, far from the thumb that uses it. Phones keep a rule desktop does not have: the slow gait is silent to the Relay (TOUCH_CONTROLS §2.1). X2 teaches that rule where it happens, on the stick, and lists the controls on the title and pause screens.

**Decisions:** the spec `~/FrontRoomsInteractionWork/x2/X2_PHONE_HINTS_SPEC.md` v3.3 (5996e632), reviewed by 总控 (v1 → v3), approved to build by 教程设计 (v3.1, 2026-10-22 12:2x), strings signed by 叙事设计 (SLOW · UNHEARD, 10:0x: on the condition that the creep stays silent in any shipped hunter), type and the Figma board `3564:7362` signed by 平面视觉 (~15:20). The calm card's removal lands with 关卡设计 as M1 (one joint landing).

### The rules

1. **No calm-hint card on phones.** The phone hint strings and `CurrentCalmHint` are gone (M1). An empty hint hides the context card, so nothing dark is drawn at the bottom.
2. **One legend on the title and the pause screen**, word for word, IBM Plex Mono 11 caps (iPad 12), glyphs 22 pt centred on the caps:
   - row A: MOVE · DRAG · LOOK · SLOW · UNHEARD · PAST THE RING · RUN (RUN BUTTON in BUTTON mode);
   - row B: USE · DOOR · HOLD USE · BREAK GLASS.
   Ink on the pause frost; on the title, paper with the 1 pt keyline. The title lockup is TAP TO START (Bayon 17 paper, keyline, no chip) centred at master y 306, row A's cap top 16 pt under its baseline, row B a glyph box + 8 pt lower.
3. **The stick's label slot** carries one word at a time, by priority: WINDED (paper) > the running RUN (yellow, STICK mode only) > the RUN preview (paper; STICK mode, thumb ahead between the ring and the line, while it still teaches) > SLOW · UNHEARD (paper, both modes, while it still teaches). Every word is Bayon 17 with the keyline and no chip.
4. **Handover:** a higher word cuts the current one at once and fades in over 0.14 s; an equal or lower word (or none) waits for the current word to fade out (0.10 s) where it stands; the preview becomes the running RUN in place (the colour turns yellow, no fade). SLOW · UNHEARD shows after 0.25 s of continuous Slow and leaves after 0.20 s without, so walk starts never flash it.
5. **Where it draws** is decided at touch-down: above the thumb if the word's rectangle, swept over every thumb position it can show at, clears the screen edges and insets, the HUD's top band, key line and captions, the pause button and (BUTTON mode) the RUN button; otherwise hung under the ring (cap top at origin − 60·Scale − 14, fixed); otherwise hidden for that touch. The x is never clamped.
6. **The teaching words retire.** SLOW · UNHEARD after 1.5 s of counted creep (Slow, moving ≥ 0.5 m/s, in play, not winded) while it shows, and only once the Relay has been released this run. The RUN preview after the first real crossing of the run line or 4 s shown in all (the clock sits beside the flags and restarts when the flag is cleared). The flags are PlayerPrefs `FrontRooms.Touch.Taught.Slow` / `.Run`, saved at the next pause or menu, or when the app is paused. Settings › TOUCH › STICK HINTS (ON / OFF) shows whether a word the current mode can show still teaches; ON clears both flags, OFF sets both.
7. **Name clash:** INTERACTION ASSIST's planned value SLOW is renamed STEADY, so SLOW means only the gait.
8. **Step 2, in 关卡设计's file (Game.cs):** the glass `Flash()` line loses its dark card and yellow rule on phones too (A6): paper with the keyline, centred, sitting on the slot's bottom edge 29 pt up, so a second line grows upward. Phones break the glass flash before TO (平面视觉; the words are unchanged).
9. **The phone HUD under the dot** (same hunk, 平面视觉 2026-10-22): the dashes are 20 × 5 on a 25 pt pitch and the hold bar is 100 × 3 (desktop's sizes at the ×0.85 ramp). The caption card hangs from a fixed top 66 pt under the dot and grows down, 10 + 20 × lines tall. So it stays 5.5 pt under the dashes on every aspect, and two lines end at least 7 pt above a two-line flash (below about 8 pt the instruction reads as a third caption line). Pinned at bottom 99 and growing upward, one line touched the dashes and two covered them.

### Verification

Touch playtest run land3 in `W/proj_x2land` (main after M1 + the Bayon fallback + Game 938b0ee9 + X2, the files that landed): iPhone 16 Pro 153/153, Android 20:9 153/153, iPad 11 153/153. Roslyn on main: 0 errors for macOS and iOS. Desktop autopilot 2554 PASS (ended by time; same maze and EXIT as main; no handheld line in its log). VL415 (the stick's words, Figma 3616:7760), VL416 (title, pause, no calm card, 3616:7771) and VL417 (the phone HUD: sizes, captions, flash, 3616:7782) compare each build frame with the signed board `3564:7362`; the spec masters in `2528:5403` were updated to the build the same day.

---

## EXAMINE on phones · Close-ups by touch

**Status 2026-10-22:** T1–T7 of the touch contract (`research/inspect/contracts/touch_inspect.md`) in main (T1–T5 21:10, T6/T7 after). Open: the zoom works once 关卡设计's C7 line lands (after 游戏视觉's playtest follow-up); the panel's region heads arrive with 游戏视觉's transcript follow-up; the object moves into the left 64 % when the panel opens once 游戏视觉 adds a reframe. T1–T5 were built by 交互设计 on account B (20:2x–20:31), verified and landed on account A.

**Decisions:** the EXAMINE design (`research/inspect/10_design.md`, 游戏视觉) and 平面视觉's G10 (`research/desktop_ui/EXAMINE_TEXT_G9_G10.md`): touch words, not new glyphs; READ as the phone verb for plans, screens and signs (FIX_CARD §B); BACK on the USE disc, moved to the lower right.

### The rules

1. **Open by touch, as by E:** a tap on the target opens it through the game's own gate (`FrontRoomsInspect.Find` → `CanShowInspect` → `CanPressInspect`); aimed at it, USE says READ and a press does the same. The open is felt: one light tick.
2. **A close-up has one control:** USE becomes BACK at the lower right, clear of the framed plan and the transcript panel's space; the stick, pause and RUN button fade out; no frost.
3. **Three ways out:** BACK, the left-zone pull-back (the desktop S) and Android Back.
4. **Look and zoom like a photo:** drag to look, pinch to zoom (continuous), double tap to toggle the zoom. A tap that opened a close-up never also zooms it (the double-tap detector resets per menu state).
5. **Desktop gets the same zoom:** the wheel steps it (one step per gesture), the right button toggles it (with C7).
6. **Text on a phone, on request:** a close-up with a transcript offers TEXT in the pause slot; it opens a reading panel at the right 36 % of the safe area for that close-up only. A drag on the panel scrolls it; heads, lines and glow lines follow 平面视觉's G9.

### Verification

T6/T7 runs r2 (2026-10-24, rebased onto main after the iPhone Duo + corners landings, clone W/proj_t67): touch iPhone 152/158, the 6 failures all in the broken-door block, whose code the package does not touch (today's map hands the stand an edge-on leaf the crosshair loses; the desktop prompt is empty too; followed up separately); EXAMINE focus iPad 39/39, Android 41/41, Duo outer 41/41 (real corners + camera). On the 10-22 main: iPhone 151/151, Android 151/151, iPad 148/148, desktop autopilot 2554 PASS. T1–T5: runs g1 + h1, 2026-10-22, on main after X2 (Game 938b0ee9) + this landing, without 关卡设计's C7 (the zoom then offers no step, so the pinch and double-tap checks skip): touch iPhone 16 Pro 141 / 141 (EXAMINE 10 / 10), and again EXAMINE-only on main after 游戏视觉's placard_phase0 (21:03): 10 / 10; desktop autopilot 2554 PASS; 游戏视觉's FrontRoomsInspectPlaytest 116 passed, 0 failed. With C7 simulated (W/proj_ex_c7) the EXAMINE checks were 12 / 12 on iPhone, pinch and double tap included.
