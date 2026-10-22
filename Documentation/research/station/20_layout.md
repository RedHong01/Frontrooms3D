# 20 · The security desk as a level: approach, grammar, plans (叙事设计, 2026-10-22)

**Status: v1.1 (2026-10-22 12:1x), layout logic for 游戏视觉's blockout and 关卡设计's placement.**
- **v1.1:** 叙事 accepts 游戏视觉's light-math amendments (`30_look.md` §4–§5, §7 "For 叙事"). Plans **W′** and **D′** replace W and D (§4.0); G4 gains the measuring rule; G5 is reworded; small lamps stay dark; the guard's chair faces the set in B too; `STAIR A` is final. Nothing here is a Red decision: the direction pick still goes to Red in the joint message.
- Research agent C's full notes (106 sources, four ASCII plans, 14 rules) are in `~/FrontRoomsNarrativeWork/station/C_leveldesign.md`. `L`-numbers here are its S-numbers (`SOURCES.md` §L).
- Lines marked **(inference)** are design reasoning, not source claims. Every number is a starting point to prove in blockout.

**Inputs (decided, not re-opened):**
- tutorial T1–T6 (`research/tutorial/10_proposal.md` §3–§7; rows M3–M8, V1–V5);
- `LEVEL_MODULE_SPEC.md` §2–§9 (3 m cells; Wall/Arch/Open edges; modules author no doors; one KeySpot; aisles ≥ 1.0 m; keep-clear 1.0 m at openings, 1.2 m at doors; player and Relay r 0.30 m; eye 1.62 m);
- RELAY v2 §9 (staged warnings in **your** room; never a radar; dark is never cover; photosafety);
- `01_research.md` and `02_refs.md` in this folder.

## 1. What the station has to do, beat by beat

| Beat | The player | What the layout must give |
|---|---|---|
| Hear | follows a muffled jingle through doorways (M5/S2 beacon field) | sound that comes from the next doorway on the walking route, never through a wall [read L29, L30] |
| See the light | from the last junction, a cold moving light in one doorway | a surface lit by the TV that is visible from the corridor (§2.2) |
| Reveal | at the threshold: the set, the counter, the key, the empty chair | the "station triangle": TV, key board and E1 in one view (§3, G4) |
| Read | the guard's leftovers, the honest plate over the key | the evidence cluster within 2 m of the guard's seat (`01` §5–§6) |
| Take | E · TAKE KEY; the light on the key goes out as the set (beside you, out of view) collapses to a dot; the jingle cuts; silence | cause and effect in one frame; a 2 m clear take zone; no pocket (G5, G7) |
| Leave | back to the EXIT, 2–6 cells on Floor 1, warnings as it nears | two ways out on different walls, both looping to the EXIT; the exits are the brightest shapes once the TV is dead (G2, G8) |

## 2. The approach (outside the room)

### 2.1 Three-stage lure (rule 1, from agent C)

1. **Hear it** from 3+ cells out (the beacon field already covers about 90 m).
2. **See its light** from the last junction, 1–2 cells out.
3. **See the screen** at the threshold.

Sound alone is a weak guide. Light and composition carry the last metres [read L1, L5, L24]. Valve records players ignoring a screen until the lights were cut down to it [read L6, L7].

### 2.2 The light has to leave the room (flag for 游戏视觉)

- V3's TV light reaches about 6 m (SP).
- In a 3-cell-deep room with the set on the far wall, E1 is 8–11 m from the screen. The spill would barely reach the doorway, let alone the corridor floor (inference).
- **Pick one per module:**
  - (a) keep the screen ≤ about 6 m from E1, as in plan W §4.1;
  - (b) frame E1 so that from the corridor you see a **lit surface inside** the room: the wall or ceiling the screen washes;
  - (c) 游戏视觉 adds a weak spill light at E1, synced to the TV's colour and level. That is a light-budget call.
- Prove it with the squint test from the last junction.

### 2.3 Lock before key (Floor 1)

- M4 already places the station 2–6 cells past the EXIT, so the player sees and tries the locked EXIT first [read L42–L44].
- At the take, the HUD panel `STAIR KEY` / `OPENS THE STAIR DOOR` names the door (Red approved this in the 平面视觉 chat).
- **Ask 关卡设计:** turn the module so **E1 faces the beacon route from the start door** (Floor 1), or from the arrival (Floor 2+). Then the frontal or oblique reveal works as drawn.

## 3. The station grammar (fixed in every variant; the dressing swaps)

This follows Burgess's split between a kit's look and its gameplay [read L84].

| # | Rule | Why (source) |
|---|---|---|
| **G1** | **E1, the main way in:** the widest Arch (1.6–1.8 m), never a door, and the brightest opening: the TV light plus the one warm lamp just inside it. The set is visible from E1's threshold, within about ±30° of E1's axis | entrance hierarchy, pools of light [read L20, L23, L83]; one door is never on E1, because a door would block the beacon |
| **G2** | **E2, the second way out:** an Arch of 1.1–1.4 m, or a map door. It is on a different wall from E1, at least one cell away, and lit from the corridor side. **Exactly two openings.** Both feed loops that reach the EXIT. Neither is glass or a fake door | the hunter is sent here after the take; no dead-end trap; two exits, not four, to avoid choice overload [read L56, L32, L37, L66; search L57, L59] |
| **G3** | **The TV is the only moving light on the floor.** Room lamps are Dim or Off, except the warm lamp at E1. Nothing else flickers in view | a tapestry of light, not a flood [read L21, L25–L27]; V3 |
| **G4** | **The station triangle.** From E1 you see the set and the key board. The board is within about 2 m of the screen and ahead of its plane, about ≤ 45° (at most 60°) off its axis, so the screen lights it. A board beside the set on the same wall is behind the screen's plane and gets no light. **Measure from the screen centre, not the set's corner** (v1.1): the board goes on a wall perpendicular to the set's wall, 30–45° off the screen axis, ≤ 2.0 m away, inside the set's inner cone. A corner set fails this (`30_look.md` §4.2: 69°, a third of a Dim lamp's wash) | [read L33, L34, L39, L42]; agent C's own Plan A broke this (§4.4); `30_look.md` §4.2 |
| **G5** | **Cause and effect in one frame** (reworded v1.1). **At the take, the light on the key board drops in view**: one change, ≥ 70 % on the plate (blockout check 3). The set itself may be out of view: a front-lit, wall-hung key sits about 100° from the set at take reach (`30_look.md` §4.2), so the eye gets the light going out on the key and the ear gets the set dying (the jingle cut on the take frame, S4). The panel's green POWER lamp stays lit beside the empty hook: the alarm went somewhere. The hunter is never seen arriving: it is dispatched from ≥ 42 m (M7) | [read L6 cn_213/cn_097, L29, L32]; `30_look.md` §4.2–§4.3 |
| **G6** | **Clearances:** side aisles ≥ 1.0 m (`MinAisle`); the centre aisle and both escape lines 1.2–1.5 m; 1.0 m clear inside each Arch, 1.2 m inside a door swing; nothing taller than about 1.1 m between the board and either exit, so you can see over it | [read L101, L84, L81, L82] |
| **G7** | **No pocket at the take.** The key sits *behind* the counter (`01` §7.1), so the staff strip behind the counter must be **open at both ends** and run through to an exit. Keep a 2 m clear take zone | [read L52, L40, L67] |
| **G8** | **Darker, not dark; no hiding shapes.** After the take the room falls to "Hollywood dark". The two exits become its brightest shapes (squint-test from the board). No dark nook behind a counter, and no cave under a desk | dark is never cover; players trained by Amnesia or Outlast will try to hide [read L22, L37, L67; search L70] |
| **G9** | **The evidence cluster:** log, sign-out sheet, phone, empty radio charger, cup, ashtray, all within 2 m of the guard's seat and lit enough to EXAMINE. **Small lamps stay dark** (v1.1): the phone's line lamps (no call waiting, nobody on hold: nobody is calling the desk any more) and the empty charger's lamp. A winking hold lamp would be a second moving light (G3) and a lure toward an interaction that doesn't exist. The one lit point is the annunciator's steady green POWER jewel | `01` §2, §6; `30_look.md` §4.3 |
| **G10** | **Furniture follows logic and agrees with the light.** Seats face the set and keep the axis aisle open. Rows never cross an escape line. Use **institutional tandem seating in two short rows**, never long rows of identical upholstered armchairs (A24) | [read L13, L33, L61]; `02_refs.md` §3.8 |

## 4. Plans (sketches; 游戏视觉 proves them in blockout)

### 4.0 Amendments accepted (v1.1, 2026-10-22; drawings in `30_look.md` §5)

游戏视觉's light math showed two faults in my plans: W's corner set cannot light its key (G4), and both W's and D's E1 sat too far off the set's face for its wash to read from a grid corridor (§2.2 answer (b)). Its spill-light answer (c) is rejected as a fake pool. Accepted:

| Was | Now | What changes for the player | Story and rules |
|---|---|---|---|
| **W** (set in the NE corner, E1 in the S wall's west cell) | **W′** (`30_look.md` §5.1): set on a wall bracket on the E wall (staff strip's back wall), 20° down; key plate on the N wall at the strip's mouth with the annunciator beside it; **E1 in the W wall's south cell**, set 22° off its axis; E2 at the strip's south end | From E1: the backs of two tandem rows facing a playing set across the room, the counter on the right, the lit key plate with its green dot beyond. "A room built for waiting, where nobody waits." Leaving by E2, you pass under the dead set | G1, G2 (E1 W wall / E2 S wall), G4 (38°, 1.87 m), G6, G7 (strip 1.44 m, open both ends), G10 hold. **Standard ceiling only**; a Low room needs the cart variant (Phase 2) |
| **D** (E1 in the SW corner, oblique two-step reveal) | **D′** (`30_look.md` §5.2): **E1 in the S wall's middle cell**, on the set's face; **the desk moves ~1.6 m east** so the chair and jacket stand against the set's wash as seen from E1 (I take this optional part) | The reveal becomes **"the glow and the empty chair, then the key"**: the chair, back to you, facing a playing set, seen square from the door. The key plate is on the right wall, 32° off the axis | "He watched the set, not the door" is now the first image, not a diagonal glimpse. G4 already passed in D (40°, 1.97 m) and is kept. 2.3 m stays clear between the desk and the E wall for the escape line |
| B: the guard's stool behind the counter, facing the visitors | **The guard's chair turned round to face the set**, its back to the counter and the waiting rows | The same tell as A, in the first room a new player sees | "The watcher faces the wrong way" carries across both directions. In-world it is plain: a bored night guard turns the chair to the TV |
| Key tag `STAIR A` as a placeholder (`30_look.md` S11) | **`STAIR A` is final** for the tag | — | One name per thing (`40_floors_exit_tutorial_strings.md` §5): plan, tag, door plate and stair sign all say STAIR A. The floor token stays on the HUD tag (平面视觉) and waits on C28 |

- **Plans W and D below stay for the record only.** Build W′ and D′.
- **Plan H** is unchanged (E1 already on the axis); it still needs §2.2 proven by the squint test.

Scale: 1 character = 0.3 m across, 1 line = 0.6 m down. One 3 m cell is 10 characters by 5 lines. North is up.

Legend:
- `TV`: the set, with its facing given in the note;
- `K`: the key board, about 1.4 m up;
- `|` or `=`: reception counter, 1.05 m;
- `G`: guard desk, 0.74 m; `g`: the guard's chair or stool;
- `HHH`: a 3-seat tandem beam;
- `L`: the warm lamp; `o`: a dim troffer;
- `~`: TV light; `*`: the sightline from E1;
- `a`: ashtray stand under a NO SMOKING sign; `m`: magazine rack; `>`: E2.

### 4.1 Plan W: Waiting (Level 0), 3 × 2, the set in a corner (superseded by W′, §4.0)

```
   +---------+---------+---------+     N ^
   |  o               ~ ~ ~ ~ [TV|     TV on a corner bracket 2.0 m up, faces SW, tilted down
   |  m          ~ ~ ~ ~ ~ ~  K  |     K on the N wall 1.5 m W of the TV: ~45 deg off its axis (lit)
   |        HHH HHH  ~ ~ ~ |   g |     | counter (x ~7.2 m, y 1.0-4.0): staff strip 1.4 m behind it,
   |      ~ ~ ~ ~ ~ ~ ~ ~  |     |       open at the N end (to K) and the S end (to E2)
   +  a     HHH HHH  ~ ~ * |     +     HHH two short tandem rows facing the corner set
   |     ~ ~ ~ ~ ~ ~ ~ *   |     >     E2 arch 1.2 m, E wall S cell = way out 2
   |   ~ ~ ~ ~ ~ ~ ~ *     |     >
   |  ~ ~ ~ ~ ~ ~ ~*  ~         o|
   |  L   ~ ~ ~ *~ ~             |     L warm lamp just inside E1
   +--      --+---------+---------+    E1 arch 1.8 m, S wall W cell = way out 1
       ~ ~ *~ ~ ~                      E1 -> TV ~9 m, ~8 deg off the screen axis: near face-on
```

- **Read:** from E1 you see the screen almost face-on across the waiting room, the counter on the right, and the key above the counter end.
- **Take:** you step past the counter's north end into the staff strip and face north to the board. The set is about 1.5 m to the right, so the collapse is in view.
- **Leave:** E2 is about 3 m down the staff strip. E1 is visible diagonally across the room over the 1.05 m counter.
- **Light:** E1 is about 9 m from the set, so §2.2 applies. Use (b): the lit back walls framed through E1. Or try the 3 × 2 room turned so E1 sits in the N wall's W cell, about 5.8 m across.
- **Dressing** (`01` §6; G10): two short tandem rows, an ashtray stand with NO SMOKING, a magazine rack, a wall clock over E1, the guard's stool and evidence cluster on the counter. **Not** the existing `L0_WaitingRoom_4x3` look ("chairs face nobody"): the set and the counter make it a different room.

### 4.2 Plan D: Desk (Office), 3 × 3, oblique (agent C's Plan B; superseded by D′, §4.0)

```
   +---------+---------+---------+     N ^
   |=                       [TV  |     TV on a low wall cabinet, NE cell, faces S
   |=   o         o      **  ~~  |
   |=                   *~    ~~K|     K on the E wall ~2 m S of the TV, ~42 deg off its axis (lit)
   |                  ***      ~K|
   +         + GGGGGGG*+        ~+     G guard desk facing the TV: the guard watched the set,
   |              g **~          |       his back to the way in (the "watcher faces the wrong way")
   |             ~ * ~       o   |     the E side is kept clear: the escape line K -> E2
   |  h h h h   ~***~            |     h visitor chairs (4)
   |           ~ * ~             |
   +  h h h h+~** ~    +         +
   |       ~ ~*~ ~               >     E2 arch 1.2 m (or map door), E wall = way out 2
   |    ~ ~ *** ~ o         o    >
   |   ~ ~ ~*~ ~                 |
   |L ~ ~ **~ ~                  |     L warm lamp at E1
   +--      -+---------+---------+     E1 arch 1.8 m, SW corner: oblique entry
                                       = sign-in counter against the W wall (no staff side, no gap)
```

- **Why it works:**
  - the reveal comes in two steps: the glow, then the set and the key;
  - both escape lines are short;
  - the key is lit;
  - the guard's chair faces the set with its back to the door. That last point is the strongest storytelling image in the room [read L33; `02` R-P1, R-P2].
- **Light:** E1 is about 9–10 m from the set, so §2.2 applies. A 2 × 3 Office room keeps the same plan with one row of visitor chairs.

### 4.3 Plan H: Long hall (2 × 4), end-on (agent C's Plan C, a variant for long rooms only)

- An 11 m axis gives the strongest vista. E2 sits beside the key.
- **Amendments:**
  - two short tandem rows, not six rows of four (G10);
  - a spill decision per §2.2, because 11 m is far beyond 6 m;
  - E2's corridor must loop to the EXIT, since most players will leave that way.
- Use it only where a 2 × 4 slot comes up.

### 4.4 Plan X: what not to build (from agent C, plus one fix to C's Plan A)

- **Don't:**
  - seal the TV and the key in a pocket behind an L-shaped counter;
  - run seat rows wall to wall;
  - make one way in the only way out;
  - leave the set edge-on from E1.
- **Fix to C's Plan A:** it hung the key board on the same wall as the set. There it is behind the screen's plane, so the screen cannot light it (G4). Plan W moved the set into a corner, which puts both adjacent walls about 45° off its axis. **That was wrong for points near the corner** (v1.1): the screen stands ~0.57 m off both walls, so a key 1.5 m along a wall is 69° off the axis. W′ fixes it with a wall bracket (§4.0).
- **Don't put the set in a corner** when the key must be lit by it.

## 5. Rejected from the research (and why)

| Proposal | Why it is out |
|---|---|
| "Lamps dip along the hunter's route, far to near" (C's take sequence, step 4) | Red's staged warning (2026-10-03): the lamps dip **in the player's room**, never where it is (RELAY §9.1–§9.2). A route of dips is a radar |
| "One lamp in view per exit, so a dip tells which way it comes" (C's rule 14) | Same reason: directional lamp information is a radar, and v2's lamps are not directional. Direction comes only from the stage-2 steps, by design |
| "A placard names the EXIT at the take" | Done another way: the HUD panel `OPENS THE STAIR DOOR` |
| Slowing the key carrier, or new locks on the way back | Agent C's own rule 13: the way back changes state, not geometry. Nothing new is added |
| A phone bell, an alarm bell, a 1 kHz bars tone at the take | The lock-on chime stays the game's only tonal sound (RELAY §9.4). The take's sound is the set dying (§4.7 of the tutorial) |

## 6. Later (Phase 2; not in V1)

- **Watchclock key stations on corridor walls** (`01` §2): small hinged boxes with a chained key, numbered, along the guard's route to the desk.
  - A period-true trail. Static per seed, so not a radar.
  - It needs a map rule (关卡设计) and a new kit (游戏视觉).
- **A different real post per floor** (`10_directions.md` direction C): warden station, dock or pick-up desk, guard booth. Same grammar, different dressing.

## 7. Questions for other lines

**关卡设计:**
1. Can M4 turn the module so E1 faces the beacon route (§2.3)?
2. Can a module require "no door on E1"? Or can M4 prefer placements where E1's neighbour has the same height, so no map door lands there?
3. Can M4 guarantee both openings join loops that reach the EXIT?
4. On Floor 1, can M7's dispatch point avoid the station-to-EXIT corridor?

(1–3 are an extension of my 09:3x questions: footprint, two openings, the TV-anchor cell for M5.)

**Answered by 关卡设计, 2026-10-22 12:1x: yes to all four.** They become M4/M7 placement rules, written into M4's spec when it is built after C22 (no Red decision):
1. **Rotation:** M4 picks the module's 90° turn so E1's side faces the cell where the beacon route (from the start door on Floor 1, from the arrival door on Floor 2+) first enters the footprint. If no turn works with the other rules, the spot is rejected. The validator logs the entry edge; it must be E1 on 100 % of seeds.
2. **No door on E1:** by placement, not by a module flag. M4 accepts only spots where E1's outside neighbour is in the station's own zone (same height), so E1 stays the module's Arch/Open edge and can never become a door or window. E2 may sit on a zone border.
3. **Loops:** a validator rule. With the station's cells removed, the outside cells of E1 and E2 must connect to each other (a loop round the station) and each reach the EXIT's front cell, with no glass on either path.
4. **Floor 1 dispatch:** M7's arrival candidates (≥ 14 cells / 42 m of walking) exclude every cell within 2 cells of the shortest station→EXIT path; if none qualify, the next ring out is tried before the fallback.

**系统设计:** on Floor 1, does the first W1 burst fire before the player reaches the EXIT? If it doesn't, the tutorial floor never teaches the warning. It depends on M7's ≥ 42 m arrival and the 2–6 cell return.

**Answered by 系统设计, 2026-10-22 12:1x: no.** The Relay is sent 3 s after the take, arrives ≥ 42 m away and hunts at 2.6 m/s against the player's 3.2 m/s, so the first burst comes ≥ 7.9 s after the take, while the walk back plus the unlock takes about 3.5–7.5 s. Only a dawdler meets W1 on Floor 1. 系统设计 asks Red (**C32**, data 22u, in its own chat): for Floor 1's first send only, arrive 1 s after the take, unseen, just inside stage 1 (22–30 m), so the first burst falls 1.3–2 s after the take. **Red chose this (C32 = 1, 系统设计's chat, 2026-10-22, reported by 系统设计; systems.json 22v):** "C32 选 1：第 1 层第一次派怪物改到拿钥匙后 1 秒、在 22–30 m 处出现". The placard's lamp line now pays off on Floor 1; no string change. Fiction check (叙事): honest, because the responder really is near. It also fits this floor's story: the guard was out on rounds, so the building's night staff were already moving close by.

**声音设计** (no chat; inbox): the jingle cuts on the exact frame of the take (S4). Beacon portals are already in S2.

**Red** (via 叙事): the HUD's `LEVEL 0 / THE LOBBY` matches the Backrooms Fandom wiki's "Level 0: The Lobby" (`02_refs.md` §3.11). Recommended: rename the start-room line, e.g. `LEVEL 0 / FRONT ROOMS`, after the game's title and the store. **Decided: `LEVEL 0 / FRONT ROOMS`** (Red, 2026-10-22 12:15, "全部按推荐（Q1–Q7、照片下载、LOBBY 改名）").

**教程设计 / 总控:** the honest warning plate over the key board (`01` §6; the v1 draft wording is retired, the plate string is v2 below). It tells the player that the take is an alarm before they do it. Does that fit the take beat (`10_proposal.md` §4.7), or should the take stay a surprise?

**Answered by 教程设计, 2026-10-22 12:1x: keep it, on the close-reading layer only** (`10_proposal.md` §4.7). It reads only in EXAMINE or within ~1 m; its aim box never takes the key's aim (aiming at the key always gives E · TAKE KEY); it stays after the take; it never glows or changes. Wording v2 by 叙事: `STAIR KEY` / `SILENT ALARM: REMOVAL SIGNALS PANEL` (`01` §6), because nothing rings in the room at the take.

## 8. Blockout checks for 游戏视觉 (prove, don't assume)

1. **Squint tests:** from the last junction (the light reads), from E1 (set, key, E2), and from the board after the take (the exits are brightest).
2. **Light at the board:** the key is readable by the screen's light alone (G4).
3. **One frame at the take:** a capture facing the board shows the light on the plate and key drop ≥ 70 % in one change, with the green POWER lamp still lit (G5 v1.1).
4. **Camera wiggle** at E1 and E2: the counter, a chair back or the door leaf must not hide the key [read L37].
5. **Collision** at r 0.30 m: the staff strip passes through, and no gap fits only the player.
6. **Photosafety:** the TV light under V3's limits; the collapse is one change.
