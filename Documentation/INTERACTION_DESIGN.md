# Interaction design (交互设计): how the player's hands work on the world

Owner: the 交互设计 session (since 2026-10-22; Red: "我需要你给我专门开一个窗口叫做交互设计，负责处理交互类的问题"). This file holds the interaction rules: doors, windows, keys, E prompts and the touch layer. Touch details stay in `TOUCH_CONTROLS.md`. System-level decisions are recorded by 系统设计 in `system_design/systems.json`; this file holds the rules derived from them and how they are built.

Code that other lines own lands through its owner: door code through 关卡设计 (`FrontRoomsMap/*`, `FrontRooms3DGame*.cs`, `FrontRoomsShots/*`), door/lock/key models through 游戏视觉, door Foley through 声音设计.

---

## A12 · Doors swing away from the player (double-acting doors)

**Status 2026-10-22:** decided; built and verified in the private clone `W/proj_a12`; lands in main after the Alpha, through 关卡设计.

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
- **Hand-off:** one diff with every test rewrite goes to 关卡设计 against the LANDINGS bases; it lands after the Alpha.
