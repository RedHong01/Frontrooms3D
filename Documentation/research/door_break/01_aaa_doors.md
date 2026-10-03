# 01 — How shipped games break a door in stages (door break, DB1)

Status: RESEARCH, 2026-10-03. Nothing in the game, the real project or any clone was changed. No media was downloaded. Wanted media is listed in `media_candidates.md` (section "01").

Task (Red, 2026-10-03 ~12:05, translated): "When a door is being broken, I want it to be like the glass shattering: make different models that answer each damage state of the door. First research how AAA games generally do it, then give me a pure-visual proposal in Figma; implement after I confirm."

This file answers the first part: **how shipped games (and one film) show a door being broken, stage by stage.** It does not repeat:
- AAA destruction in general (pre-fracture, swaps, debris budgets): `../glass/destruction/01_conference_destruction.md` and `10_glass_destruction_plan.md`.
- How real 1990 doors fail, and which blow shows which state: the sibling report `02_real_door_failure.md`. I use its words: **P** (push side, where the stops are), **S** (swing side), **BURST** (the Relay on P drives the leaf into S), **RIP** (the Relay on S pulls the leaf toward itself), and the states **MARK, D1, D2, FINAL, BROKEN** (`02` §7.1).

**Binding inputs:** `../interactables/00_map_constraints.md`, `../interactables/10_spec.md`, `../interactables/05_locked_door_type.md`, `../interaction_audit/10_audit_report.md` §3.7, `Assets/Scripts/FrontRoomsShots/FrontRoomsShotTimings.cs` (`DoorBreak`), `../hunter/11_squeezed_giant.md`.

**Tags**

| Tag | Meaning |
|---|---|
| **[CODE]** | I read the shipped game's own source code (Amnesia and Penumbra are open source). |
| **[DOC]** | Official engine or developer documentation (Valve Developer Community). |
| **[TALK]** | A conference talk: its slides, or a report of it. |
| **[FOOTAGE]** | I watched a public video in the built-in browser and read single frames. Timestamps are given. Nothing was saved. |
| **[WIKI]** | A fan wiki (Fandom). Good for "what happens", weak for "why". |
| **[PRESS]** | A press article, review or guide that I opened and read. |
| **[SEARCH]** | Taken from a search-engine summary. The page itself was not opened. |
| **UNVERIFIED** | Not confirmed from a source I read. |
| **PROPOSAL** | My suggestion for FrontRooms. Not a decision. |

Full source list with URLs: §8.

---

## 0. Short answer for Red

1. **Your idea, "a different model for each damage state", is exactly what two shipped horror games do for doors.**
   - **Left 4 Dead (Valve, 2008):** each breakable door has its intact model plus **three damage models** (`damage1`, `damage2`, `damage3`). The door swaps to the next model as it loses health, plays a "pound" sound on every hit, and spawns broken pieces at the end [V1].
   - **Amnesia: The Dark Descent (Frictional, 2010):** each breakable door has its intact mesh plus **two damage meshes**. At each health threshold it swaps the mesh and plays a damage sound and a dust/splinter particle. At zero it spawns a pre-broken door made of pieces and throws them [F1].
   - So the shipped norm for a monster breaking a door is **2–3 damage models, then a break into pieces.** That is 3–4 visible states before the door is gone.
2. **The swap always lands on a hit.** In Amnesia, the monster's "BreakDoor" animation has an impact frame, and the damage is applied on that frame. The mesh swap, the sound and the particles all fire together [F2]. Nobody grows damage smoothly between hits. This matches `02` §8 item 1: damage on a blow, never between blows.
3. **The stages are a countdown the player watches from the other side.** In horror, the door breaks for the monster, not for the player. Every hit is loud, the damage grows, and the player has a few seconds to run or hide. L4D, Amnesia, Friday the 13th: The Game and The Shining all use the door this way [V1][F2][J1][S4].
4. **What changes per stage, in the games and film I could check:**
   - **holes that let light through** (L4D: the lit corridor shows white through a torn hole in a dark door [L1]);
   - **the weapon or a body part through the door before the door gives** (The Shining: the axe blade, then the face, then the hand reaching for the lock [S4]; Silent Hill: Homecoming: a knife through a closed elevator door [SH1]; The Evil Within: a chainsaw driven into a hiding place [T1]);
   - **splinters bent toward the victim** and cracks that run along the wood grain [S4].
5. **The final break is one hard event, hidden by debris.** Amnesia throws the pre-broken pieces outward from the door's centre, plus the door's own speed [F1]. Battlefield: Bad Company 2 hides each wall swap behind a smoke particle [B1]. Amnesia: The Bunker's official clip hides its door break in a cloud of dust [F4]. Resident Evil 7's Jack bursts through a wall in one frame, knocks the camera, then stands back-lit in the hole, and the hole stays open as a new path [C4][C2].
6. **The best single reference is film: The Shining (1980), the bathroom door.** About five readable states in roughly 40 s: a split in the panel, the blade through, the panel splitting into long strips along the grain, a hole you can see through, the face in the hole, then the hand reaching through to the lock [S4]. Kubrick swapped the first, weak prop door for a real, stronger one so the break would be slow [S1][S2]. The stages were the point.
7. **Where AAA and real doors disagree, decide on purpose.** Games punch **holes through the leaf** because holes read: light, a glimpse of the monster. Real 1990 solid-core and hollow-metal doors **fail at the latch and the jamb, and the leaf stays whole** (`02` §0, §4; I saw the same in a real ram clip, [R1]). FrontRooms can follow the real failure (`02` §7.2) and still borrow AAA's tricks: light along the latch edge, splinters and dust toward the player, a loud hit on every blow, and one hero "peek" through the lock hole (§6).

---

## 1. The door destruction methods, in one table

| Method | How the stages exist | Shipped examples | Fits FrontRooms? |
|---|---|---|---|
| **A. Damage models swapped on health thresholds, then gibs** | Intact model, then 2–3 full damage models, then pre-made pieces | Left 4 Dead 1/2 [V1]; Amnesia: The Dark Descent [F1] | **Yes.** It is Red's idea, and our blows are already discrete events |
| **B. One-step break into pieces** | Intact, then pieces (no in-between) | Penumbra [F3]; Source `func_breakable` boards [V3]; Amnesia: The Bunker (explosive) [F4]; RE7's scripted wall [C4] | For the **final** state only |
| **C. Holes cut where each hit lands** | Each hit cuts a new hole in a planar surface | Rainbow Six Siege barricades [U1][U2]; Battlefield 1 doors [SEARCH] | No: the Relay's blows land in the same place, and our leaf is not a barricade |
| **D. Baked simulation played as an event** | Fracture offline, simulate many times, keep the best, play it back | Resident Evil 7: boarded windows smashed by an enemy, tables, explosions [C1] | Yes, for the **throw** of BROKEN if physics looks wrong |
| **E. Scripted burst** | A one-off set piece, no stages | RE7 Jack's wall [C4]; The Evil Within, Sadist through a large door [T1]; TLOU2 Rat King through walls [TL1] | For one hero moment only; the Relay breaks doors systemically |
| **F. Binary open** | The door just pops open | Dying Light 2 bash [D2]; Hitman breaching charge [H1] | No |

Most horror monsters use **A** (systemic, readable) and **E** (one scare). Action games use **C** (information: peek holes) and **F**.

---

## 2. Stage counts and what sells them, game by game

| Game / film | Who breaks it | Visible states before the break | What changes per state | Sync | Debris | What sells it | Evidence |
|---|---|---|---|---|---|---|---|
| **Left 4 Dead 1/2** | Common infected (clawing, pounding); Tank | **4**: intact + `damage1`–`damage3` | Whole model swapped. Torn holes in the leaf; the lit space behind shows through; later the infected show through | `pound` sound per attack; swap on health | A debris model on break, faded after 10 s | Light and bodies seen through the holes | [DOC] [FOOTAGE] |
| **Amnesia: The Dark Descent** | Grunt and Brute, a looping "BreakDoor" animation | **3**: intact + 2 damage meshes | Mesh swap at `HealthDamage1` / `HealthDamage2`, with a damage sound and particle | Damage applied on the animation's impact frame | A pre-broken door entity; pieces thrown with an impulse | The hit-hit-hit rhythm heard while hiding | [CODE] |
| **Penumbra** | — | **1**: intact | Break only | `OnDamage` / `OnDeath` | Break entity, break sound and particle | — | [CODE] |
| **Amnesia: The Bunker** | The Beast; the player (brick, sledgehammer, explosive) | Two hits with a brick or sledgehammer [SEARCH]; damage states UNVERIFIED | — | — | Explosive case: blast, then a corridor full of dust, then the door is gone | Dust hides the swap; the noise draws the Beast | [FOOTAGE] [PRESS] |
| **The Shining (film)** | Jack, a fire axe | **~5** | Split, blade through, strips along the grain, see-through hole, face hole, hand through to the lock | Cuts between inside and outside on each blow | Splinters bent inward toward Wendy | The weapon comes first; the hole is next to the lock; a slow, real door | [FOOTAGE] [PRESS] |
| **Rainbow Six Siege** (barricades) | Attackers' melee, bullets, shotguns, charges | Continuous: every hit cuts a hole; 3 melee hits destroy it | A hole cut where hit; boards fall; the fabric stays nailed to the frame | Per hit | Boards crumble off with a distinct sound | Peek holes for information | [WIKI] [TALK] |
| **Resident Evil 7** | Jack (scripted) | **0**: one burst | The wall bursts in one frame | The camera is knocked; Jack is revealed back-lit | Plaster, lath, dust; the hole stays, with light through broken lath | Surprise; silhouette; a new path | [FOOTAGE] [PRESS] [TALK] |
| **Resident Evil 2 (2019)** | Zombies at windows | Banging first, then a breach | No damage stage; the zombie bangs on the glass, then breaks in | Banging announces the breach | — | A warning beat; player-placed boards stop it | [WIKI] |
| **Resident Evil 4 (2023)** | Dr. Salvador (Chainsaw Man) | UNVERIFIED | He eventually breaks down the door of the house you lock yourself in | — | — | — | [PRESS] |
| **Resident Evil Village** | Lady Dimitrescu; Lycans | No door breaking found | Lady D **ducks** through doorways (2.9 m tall; an early concept art showed it). Lycans come round shelf-barricaded doors | — | — | The doorway duck as a chase beat | [WIKI] [PRESS] |
| **Outlast** | Chris Walker | UNVERIFIED | He can bash doors down barehanded and break reinforced glass | Closed doors slow enemies | — | — | [WIKI] |
| **Friday the 13th: The Game** | Jason | Hits needed vary by lock, barricade bar and Jason version; visual states UNVERIFIED | A barricaded door needs more hits | — | — | Counselors inside hear each hit | [WIKI] [PRESS] |
| **The Evil Within** | Sadist | **0** (scripted) | Breaks through a large door in chapter 6; drives his chainsaw into hiding spots | — | — | The weapon through the barrier | [WIKI] |
| **Silent Hill** series | Pyramid Head; the Bogeyman | No door breaking found | Homecoming: the Bogeyman's knife comes through a closed elevator door | — | — | The weapon through the door | [WIKI] |
| **The Last of Us Part II** | Rat King | Walls, no stages found | It breaks through walls during the chase; a door barred with a fire axe | — | — | "Pseudo-dead ends": narrow escapes | [PRESS] [WIKI] |
| **Alien: Isolation** | — | No door breaking found | The Alien uses vents and passes overridden doors | — | — | — | [SEARCH] |
| **Dying Light 2** | The player | 0 | A sprint bash pops a locked door open | — | — | — | [PRESS] |
| **Battlefield** series | Players (explosives, melee) | BC2: per wall part; BF6: surfaces degrade before they break | BC2: a part disappears behind smoke and becomes a hole. Hardline: breaching hammers knock doors down | Each part is one event | BF6: rubble persists | Audio and visual feedback on every hit | [WIKI] [DOC] |
| **Doom** | — | No door breaking found | The Dark Ages: enemy armour heats up and glows, then shatters (a progress read, not a door) | — | — | — | [SEARCH] |
| **Dead Space (2023)** | — | No door breaking found | Creatures "peel" in layers (skin, fat, muscle, bone) so damage reads without a health bar | — | — | Layered, readable damage | [PRESS] |
| **Hitman** (2016–2021) | The player | 0 | A near-silent breaching charge opens locked doors; a crowbar pries doors | — | — | — | [WIKI] |
| **Control** | — | No door-specific staging found | See `../glass/destruction/01_conference_destruction.md` §2.4 for its system | — | — | — | — |
| **Half-Life 2** | The player | Boards: one at a time; doors: none | Boards nailed across openings are `func_breakable` brushes, each one breaks into wood gibs. HL2's rotating doors do not break; breakable doors came with Left 4 Dead | — | Wood gibs | — | [DOC] |
| **Condemned** (2005) | The player, a fire axe | UNVERIFIED | An axe can chop down certain doors | — | — | — | [PRESS] |

---

## 3. The details, source by source

### 3.1 Left 4 Dead 1 and 2 (Valve, Source): one model per damage state [V1][V2][L1]

- **The data.** A breakable door is a `prop_door_rotating` whose model carries a `door_options` block with:
  - `pound`: the sound "when infected attack the door" (example `Doors.Wood.Pound1`);
  - `damage1`, `damage2`, `damage3`: the model names of the three broken stages (example `props_downtown/door_interior_128_01_DM01_01`, `_DM02_01`, `_DM03_01`);
  - a `break` block that spawns a **debris model** with health 100 and a fade time of 10 s.
- **Each damage model is a whole door model with its own sounds.** Valve's wiki warns that every damage model needs its own `door_options`, or a door that starts damaged plays no sound.
- **Bullets.** In L4D2, `dmg.bullets 0` is used only on the last damage model, so stray bullets do not finish a door.
- **A creak before a break.** The `PressureDelay` key exists so creaking and groaning sounds can play before something breaks under pressure.
- **Skipping stages.** The `Break` input skips the damage models and spawns the gibs of the current model. So the stages are a normal path, not a must.
- **What it looks like** [FOOTAGE, "Left 4 Dead 2 Facts #73 – Common Infected vs Doors", 0:34–0:56]:
  - 0:35: an intact dark double door in a hospital corridor;
  - 0:42: a jagged hole torn in the upper half of one leaf. The lit corridor behind shows **white through the hole** against the dark door;
  - 0:46–0:52: the holes are larger, and an infected is visible through them;
  - 0:54: the way is open.
  - The video's caption adds that infected may run straight through an open door, and smash a closed one.
- **Breakable walls use the same idea** [V2]. A `func_breakable` wall hides a pre-made "broken edge" model **inside** it. When the wall breaks, the model is revealed, and decals switch on so the edge of the hole does not look straight.

**What sells it:** holes as light shapes in a dark door, then bodies seen through them. The rhythm of the pound sound.

### 3.2 Amnesia: The Dark Descent (Frictional, HPL2): two damage meshes and a thrown broken door [F1][F2]

From the released source (`LuxProp_SwingDoor.cpp`):
- **Entity variables:** `DamageMesh1`, `DamageMesh2`, `HealthDamage1`, `HealthDamage2`, `BrokenEntity`, `DamageSound`, `DamagePS`, `BreakSound`, `BreakPS`, `BreakImpulse`.
- **Three mesh states:** an array of three mesh entities: intact, damage 1, damage 2. When health crosses a threshold, the door shows the next mesh and plays `DamageSound` and the particle system `DamagePS`.
- **The break.** At zero health, the door spawns `BrokenEntity` (a pre-broken door made of separate bodies) at its own transform. Each piece gets an impulse **along the line from the door's centre to the piece's centre**, times `BreakImpulse`, **plus the door's own velocity.** Then `BreakSound` and `BreakPS` play.

From the Grunt enemy (`LuxEnemy_Grunt.cpp`):
- When the path is blocked by a closed door, the Grunt enters a **BreakDoor** state. It turns to the door and plays the "BreakDoor" animation.
- **The damage is applied on the animation's impact frame** (an animation "special event" calls `Attack` with the door-break damage).
- When the animation ends, it checks: if it sees the player, it switches to alert; if the door is broken, it walks on; otherwise it plays the animation again.
- While bashing, its field of view is multiplied by 4. It is watching while it bashes.

**What sells it:** the loop. Hit, sound, hit, sound, a new crack each threshold, then the door blows inward in pieces that keep the door's motion. The player usually only **hears** it from a hiding place.

### 3.3 Penumbra and Amnesia: The Bunker (Frictional) [F3][F4][F5][F6]

- **Penumbra (HPL1)** doors have only a break: a break sound, a break entity and a break particle [F3]. Amnesia added the two damage meshes, so the staged door was a deliberate step up between Frictional's games.
- **The Bunker (2023).** Its "Shell Shock" update (2023-10-25) replaced sturdy metal doors with wooden doors the monster can smash through [F5]. Guides say a thrown brick or a sledgehammer opens a wooden door in two hits [F6, SEARCH].
- **Frictional's own clip** "Breaking down doors" [F4, FOOTAGE]:
  - 0:20–0:23: the player rolls an explosive barrel up to a closed wooden door at the end of a corridor;
  - 0:25: the blast and sparks;
  - 0:26: dust fills the corridor;
  - 0:27 on: the doorway is empty, seen through the settling dust.
  - So the swap from door to no-door happens **inside the dust**.

### 3.4 The Shining (Kubrick, 1980): the bathroom door [S1][S2][S3][S4]

Frames read from the official Movieclips upload "Here's Johnny! Scene (7/7)" [S4]:

| Time | View | The door |
|---|---|---|
| 0:31–0:40 | outside | Jack talks at the closed bathroom door |
| 1:16–1:22 | outside | the first swings of the axe |
| 1:24–1:28 | inside | a white-painted **panelled** door; the lock-side panel already shows vertical splits |
| 1:30 | inside | **the axe blade bursts through the panel**, next to Wendy |
| 1:36, 1:40, 1:46 | inside | the blade again on each blow, then pulled back |
| 1:48 | inside, close | the panel has split **along the grain** into long vertical strips; a slit of light |
| 1:50–1:52 | inside | the hole is wider. **Splinters are bent inward, toward Wendy.** Jack is visible through it |
| 1:56–2:01 | outside | Jack chops more |
| 2:03–2:07 | inside | **the face in the hole**, framed by jagged strips. The stiles and rails around it are intact |
| 2:08 | inside | **his hand reaches through, down to the lock** below the hole |
| 2:10 | inside | Wendy cuts the hand; blood on the door face |

From the first blade (1:30) to the hand (2:08) is about 40 s of screen time.

- **Why it works:**
  - **The weapon arrives before the hole.** The blade inside the room is the first "break".
  - **The hole is placed with a purpose:** next to the lock, so the hand can reach it.
  - **The material is honest:** a thin panel splits along the grain, the frame of the door (stiles and rails) stays.
  - **Point of view alternates** inside and outside on almost every blow, so we see both the cause and the damage.
- **The door was made slower on purpose.** Kubrick first shot with a fake door, but Nicholson (a former volunteer fire marshal) tore through it too quickly [S1]. A real door replaced it to slow the action down [S2]. A press report says about 60 doors were used over 3 days [S3, SEARCH; UNVERIFIED].

### 3.5 Rainbow Six Siege (Ubisoft Montreal, RealBlast) [U1][U2][U3]

- **No real doors.** Defenders cover doorways with **barricades**: wooden boards held by a rope net, about 2.5 s to put up [U3]. The fabric is nailed to the frame, and a gap at the bottom lets drones through [U2].
- **Stages are holes.** Bullets cut small holes (about 20 rifle shots destroy one); shotguns cut big ones; **three melee hits** destroy it; a vault or rappel through a big enough hole finishes it. The fabric stays on the frame afterwards. Destroying it makes a distinct noise as the boards crumble [U2].
- **From the GDC 2016 talk** [U1, slides]:
  - barricades are one of the destruction "gameplay elements" with trapdoors, floors and breachable walls;
  - AI sight and sound pass through partly broken walls;
  - **"pre-destruction":** the destruction is computed ahead and revealed **at the end of an animation**;
  - debris is pre-made, instanced and recycled, never cut at runtime.

**For us:** Siege's holes are information for players, and its cutter suits a flat barricade. The useful lesson is **pre-destruction synced to the end of an animation**.

### 3.6 Resident Evil 7, 2 (2019), 4 (2023) and Village (Capcom, RE ENGINE)

- **RE7's breakables talk (CEDEC 2017)** [C1, TALK, via the CGWorld report]:
  - Baked breaks: **boarded windows smashed by an enemy bursting through**, tables smashed with a shovel from several angles, and building explosions.
  - Workflow: fracture in Maya with the Pull Down It plug-in, with material settings for **wood splintering** (256 pieces in the window example); simplify the background to collision; give the character capsules on its joints and **make the fist capsule slightly larger to sell the hit**; simulate many times and keep the best; bake to animation.
  - Tables looked dull at first. Extra rigid "frames" were added to throw the big pieces upward.
  - Polygon count after a break is about **3×** the intact count. Cut faces were filled by hand where needed, and UVs unwrapped by script.
  - Real-time breaks (Havok) were held to **about 50 pieces** per object.
- **Jack's wall entrance** [C4, FOOTAGE, 0:09–0:24]:
  - 0:09.6–0:10.0: a corridor, the right wall intact;
  - 0:10.3: **the wall bursts in one frame**: plaster chunks, lath strips and dust fly at the camera;
  - 0:10.6: the camera is knocked and Ethan's arm comes up;
  - 0:11.5: Jack stands in the debris, **silhouetted against the corridor lamp**;
  - 0:15–0:24: the hole stays: broken horizontal lath with bright light through it.
  - A guide confirms the player then goes **through the room Jack broke open** [C2]. The break makes a new path.
  - At the end of the game, the mutated Jack's giant face and hands burst through a boathouse wall [C3]. It is the face-in-the-hole beat again.
- **RE2 (2019)** [C5]: zombies **bang on certain windows** before they break in. Wooden boards placed by the player stop the breach. Twelve windows can be boarded; only nine are ever breached. The banging is the warning.
- **RE4 (2023)** [C6]: lock yourself in the house in the village and the Chainsaw Man appears and eventually breaks down the door. Its damage states are UNVERIFIED (my frame check of a fan clip was inconclusive).
- **Village** [C7][C8]: Lady Dimitrescu (2.9 m with hat and heels) **ducks through doorways**. Art director Tomonori Takano drew that duck in early concept art and wanted it in the game and the first trailer. I found no source for her breaking doors. In the village siege, shelves barricade doors and the Lycans come in another way.

**For us:** Capcom bakes the hero breaks, enlarges the hitter to sell contact, and keeps the hole as a path. The doorway duck is the squeezed giant's `door` pose (`../hunter/11_squeezed_giant.md`).

### 3.7 Outlast and Friday the 13th: The Game

- **Outlast** [O1][O2]: closing doors slows enemies. Chris Walker can bash doors down barehanded and break reinforced glass. In Whistleblower he is heard screaming and bashing a door nearby. On higher difficulty, the sound of a door shutting gets louder. **What the bash looks like is UNVERIFIED** (my two frame checks were night-vision clips that showed nothing useful).
- **Friday the 13th: The Game** (Gun Media / IllFonic, 2017) [J1][J2]:
  - Counselors barricade doors (an achievement for it); Jason breaks them down (another achievement).
  - Hits needed depend on the lock, the barricade bar and the Jason version. Several versions need fewer hits; combat stance breaks doors faster.
  - Gun Media's April 2016 footage showed Jason axing through a door [J2]. Its visual stages are UNVERIFIED (media candidate).

### 3.8 The rest of Red's list

- **Alien: Isolation** [A1, SEARCH]: player reports say the Alien uses vents and gets past overridden doors. No door breaking found.
- **The Last of Us Part II** [TL1][TL2][TL3]: the Rat King breaks **through walls** during the chase, which slows it down; the player reaches a door with a fire axe through it. Lead designer Richard Cambier's "pseudo-dead ends" kept narrow escapes possible. In the Jackson attack, infected break through a wall with a Bloater's help. No staged door found. Its breakable glass is covered in `../glass/destruction/01_conference_destruction.md` §2.8.
- **Dying Light 2** [D2]: a sprint bash pops a locked door open. One step.
- **Half-Life 2** [V1][V3]: boards are `func_breakable` brushes that break into wood gibs, one board at a time. `func_breakable` has an `OnHealthChanged` output that passes the health as a percentage, so designers could script stages by hand. HL2's rotating doors do not break; the Valve wiki dates breakable doors to Left 4 Dead.
- **Battlefield** [B1][B2][B3]:
  - Bad Company 2: a building is a group of wall and roof parts. When one part is destroyed, it vanishes behind a smoke particle and a hole takes its place. After enough parts (a developer gave about 26 for one building), the collapse animation plays.
  - Bad Company 1: wooden doors could be destroyed with the knife. Hardline: breaching hammers knock doors down. Battlefield 1: big explosions destroy lockable metal doors; melee destroys wooden barricades.
  - Battlefield 6 (2025): surfaces **visually degrade before they break**; rubble persists; every hit should be readable by sight **and** sound.
- **Doom** [DM1, SEARCH]: no door breaking found. The Dark Ages' enemy armour heats up and glows harder with each hit, then shatters. It is a clean progress read.
- **Dead Space (2023)** [D1]: no door breaking found. Its "peeling" creatures are built from bone outward and lose skin, fat and muscle in chunks, so the player reads the damage without a health bar. That is the layered idea: veneer, then core.
- **Hitman** [H1]: a near-silent breaching charge opens locked doors and safes; a crowbar pries doors. Binary.
- **Control:** no door-specific staging found. See the conference report §2.4.
- **The Evil Within** [T1]: the Sadist breaks through a large door (scripted) and drives his chainsaw into lockers and beds where Sebastian hides.
- **Silent Hill 2 (2024)** [SH1]: no door breaking found. In the original, James hides in a closet and watches Pyramid Head through it. Homecoming's Bogeyman drives his knife through a closed elevator door.
- **Condemned** [CN1]: an axe chops down certain doors (a tool gate for the player).

---

## 4. What holds across the references

1. **Two to three damage states, then pieces.** L4D: 3 damage models. Amnesia: 2 damage meshes. Siege and Battlefield go per hit, but they are action games about information. **3–4 visible states before the break is the horror norm.**
2. **Swaps land on the hit.** Amnesia applies damage on the animation's impact frame [F2]. Siege reveals pre-computed destruction at the end of an animation [U1]. L4D plays a pound per attack [V1]. FrontRooms already fires one `DoorBlow` per blow (`FrontRoomsMapHunter.cs:985-994`).
3. **Every swap is covered.** A sound and a particle on every stage (Amnesia), smoke over the swap (Bad Company 2), dust (The Bunker), decals on the hole edge (L4D walls).
4. **Holes are for light and for sight.** L4D's holes glow with the room behind. The Shining's holes show the blade, then the face. RE7's hole shows lath against light. In a dark Backrooms corridor, light through damage is the strongest read.
5. **The attacker shows before the door gives.** The blade (The Shining), the knife (Homecoming), the chainsaw (The Evil Within), the hand at the lock (The Shining), Jack's face (RE7). A hole sized for a body part is a stage in itself.
6. **The break is one frame, then a reveal.** RE7: burst, camera knock, back-lit silhouette. FrontRooms already has the reveal: `DoorBreak.RevealHold` 0.3 s with the Relay back-lit.
7. **The damage stays.** RE7's hole becomes a path. Amnesia leaves the broken pieces. Battlefield 6 leaves rubble. Siege leaves the fabric on the frame. Nobody heals a broken door.
8. **Telegraph first.** RE2 zombies bang on the window before breaking it. Amnesia's Grunt plays the full animation before the first damage. L4D's pound plays on every hit. The player hears the stages before seeing them.
9. **Make contact look bigger than it is.** Capcom enlarged the fist capsule to sell the hit [C1]. That fits a squeezed giant with no room to wind up.
10. **Material truth is a choice.** The Shining's door is a thin panel door, so it holes. L4D's hospital doors hole. Real solid-core and steel doors do not (§5).

What I did **not** find in any reference: a door whose damage grows smoothly between hits, or a door that is fully intact one frame and gone the next with no debris.

---

## 5. Real doors against the games (short; the full study is `02`)

- The sibling report `02_real_door_failure.md` is the source for real failure. Its summary: a real door fails at the **latch**, then the **strike and jamb**. Wood splits along the grain; steel bends and dents. Light leaks at the latch edge, not through the leaf (`02` §0, §4).
- Two real clips I read agree:
  - **A firefighter's ram on the lock side of a wood entry door** (passage lock and deadbolt) [R1, FOOTAGE]: several blows from 0:00 to 0:05 show no visible damage on the leaf. At about 0:06 the whole leaf swings open, intact. The frame gave, not the leaf.
  - **A slow-motion explosive breach of a white six-panel door** [R2, FOOTAGE, 320 × 240]: at 0:07 the charge fires at the lock; from 0:13 to 0:21 long strips **along the grain** fly out; at 0:25–0:35 the leaf still hangs on its hinges with the lock area blown out.
- A 2018 training-door patent describes the locked inward door the same way: repeated blows break the door or its lock, and its test strip of wood **cracks a little more with each blow until it breaks** [R3]. That is the staged rhythm, in real wood.
- **So:** the games' through-holes are a readability device. They match **panel doors and hollow-core doors**, not the FrontRooms leaves (44 mm solid-core veneer for free doors, `interact_door_leaf_veneer.py`; hollow-metal for locked doors, `10_spec.md` §3).

---

## 6. What this means for FrontRooms (PROPOSALS for the visual chat and Red)

The timing skeleton is already in the game: one `DoorBlow` per blow, a leaf jolt that grows per blow (`MapWorld.JoltForBlow`), a camera shake per blow that grows from 0.2° to 0.6° within 8 m, and the break's throw, bounce and crooked hang (`FrontRooms3DGame.cs:640-648`, `FrontRoomsMapWorld.cs:1255-1272`, `FrontRoomsShotTimings.cs` `DoorBreak`). `Damage1At` and `Damage2At` exist but nothing uses them yet. **What is missing is exactly the stage models.**

1. **Use the Amnesia / L4D structure: intact, two damage models, a broken model with pieces.** That matches `02` §7.1 (MARK, D1, D2, FINAL, BROKEN) and the spec's planned `_Dmg1` / `_Dmg2` (`10_spec.md` §3, audit §3.7). Use `02` §7.2 for what each state shows on each door type (W-W, W-S, S-S) in BURST and RIP.
2. **Swap on the blow frame, never between blows.** Use `02` §7.1's count-back rule (D1 on blow N−2, D2 on N−1). Every swap fires with the blow's sound and a burst of dust and splinters (Amnesia's `DamageSound` + `DamagePS`). The sound chat already receives `BlowIndex` / `BlowCount`.
3. **MARK blows still show something.** L4D pounds and Amnesia plays the full animation on every hit. Ours: the existing jolt, a light pulse along the latch edge (`02` §4), dust from the head, and a decal-level mark. No model swap.
4. **Cover each swap.** Dust and splinters at the latch on D1 and D2 (Amnesia, The Bunker). At BROKEN: the strike strip and pieces leave in the throw's direction, with the leaf's speed added (Amnesia's impulse rule: outward from the break point, plus the leaf's own motion).
5. **Light is the hole.** For solid-core and steel leaves, follow `02` §4: the glow line and corner wedges at the latch edge. It is our version of L4D's lit hole. It only works if the far room is brighter than the near one, so the lamp look (visual chat) matters as much as the models.
6. **One hero "peek", if Red wants The Shining.** Two honest options:
   - **W-S doors (Office):** at D2 the bored lockset loosens, and at BROKEN it is knocked out, leaving a 54 mm round hole lit from behind (`02` §4 and §7.2). A small, period-true peephole next to the lock, like The Shining's.
   - **An art-direction exception:** a fist-size hole near the latch at D2 on the veneer door, through which the Relay's knuckles or one eye show. It is not period-true for a solid-core leaf, so it needs Red's yes. It also needs the Relay rig to put a hand or face at that spot (owner: visual chat, Relay rig).
7. **Splinters toward the player.** In BURST, damage faces the player on S: split veneer, a lifted casing, splinters bent toward S (The Shining's inward-bent strips). In RIP the player sees the stop line and a slot (`02` §7.2).
8. **The break: one frame, then the reveal.** It is RE7's grammar, and our numbers already exist: a 0.12 s throw, the 1.0° camera shake decaying over 0.3 s, then `RevealHold` 0.3 s with the Relay back-lit in the opening. The broken door and its debris **stay** for the rest of the run, like RE7's hole.
9. **Keep it like the glass, in the way that matters.** Red asked for "like the glass". The glass plan pops its cracks on beats, with no growth in between, and leaves teeth in the frame and glass on the floor (`../glass/destruction/10_glass_destruction_plan.md` §0). The wooden door does the same: cracks pop **along the grain** on each stage blow (wood's version of glass radials); splinter "teeth" stay at the strike and the hinge stile; debris stays on the floor. Steel does it with dents instead of cracks.
10. **Budgets from shipped work.** RE7's real-time breaks are held to about 50 pieces, and its polygon count triples after a break [C1]. The audit plans 8–15 splinter mesh particles at the break. Both fit a desktop hero door. WebGL stays a separate, cheaper tier.
11. **Hard limits (unchanged, `00` and `02` §7.3).** The 0.05 × 2.08 × 0.98 leaf collider and the names stay. All damage geometry and debris is render-only. Nothing hangs in the opening after the break. Changes to map files (`FrontRoomsMapWorld.cs`, the hunter) are contract requests.

**For the Figma proposal (next step):** one row per door type (W-W, W-S, S-S), five columns (intact, D1, D2, BROKEN from S, BROKEN from P), each led by its real reference frame (The Shining, L4D, RE7, the ram clip) once Red approves the media batch.

---

## 7. Open items and UNVERIFIED

- **Outlast's door bash**, **RE4 (2023)'s Chainsaw Man door**, and **Friday the 13th's door states**: the visual stages are UNVERIFIED. Each has a clip on the media list.
- **Amnesia: The Dark Descent on screen:** the code is certain (2 damage meshes + broken entity). I did not find a clear, bright capture; the one fan clip I checked was too dark.
- **The Bunker's two-hit doors** come from search summaries of guides; the Prima page refused my reader.
- **The Shining's "60 doors over 3 days"** is a press claim seen only as a search summary.
- **RE7 CEDEC 2017** details come from a machine summary of the Japanese CGWorld page. The PSVR budget figure in that summary is not used here.
- **Alien: Isolation, Doom and the Outlast Trials** points come from search summaries of player forums and reviews.
- **Silent Hill 2 (2024) and Control:** I found no staged door breaking. That is "not found", not "does not exist".
- **Battlefield 1 doors:** "shoot holes in doors" appeared only in a search summary; Battlefield 6's door handling is not covered by the EA article I read.
- My frame reads are from compressed web video at small sizes. Timestamps are approximate (±1 s).
- No frame or clip was saved. Every image in a future Figma frame needs Red's approval of the media batch first.

---

## 8. Sources

Read on 2026-10-03 unless marked. "Browser" means read in the built-in browser because the fetch tool was refused.

**Engine documentation and source code**
- **[V1]** `prop_door_rotating` (sections "Custom Door Models" and "L4D Series Doors"), Valve Developer Community — https://developer.valvesoftware.com/wiki/Prop_door_rotating (browser)
- **[V2]** L4D Level Design / Breakable Walls, Valve Developer Community — https://developer.valvesoftware.com/wiki/L4D_Level_Design/Breakable_Walls (browser)
- **[V3]** `func_breakable`, Valve Developer Community — https://developer.valvesoftware.com/wiki/Func_breakable (browser)
- **[F1]** `LuxProp_SwingDoor.cpp`, Amnesia: The Dark Descent source (Frictional Games, GPL-3.0) — https://github.com/FrictionalGames/AmnesiaTheDarkDescent/blob/master/amnesia/src/game/LuxProp_SwingDoor.cpp
- **[F2]** `LuxEnemy_Grunt.cpp`, same repository — https://github.com/FrictionalGames/AmnesiaTheDarkDescent/blob/master/amnesia/src/game/LuxEnemy_Grunt.cpp
- **[F3]** `cGameSwingDoor` (Penumbra's HPL1 engine, as kept in ScummVM) — https://doxygen.scummvm.org/d6/dcf/_game_swing_door_8h_source.html

**Talks**
- **[U1]** *The Art of Destruction in Rainbow Six: Siege*, Julien L'Heureux (Ubisoft Montreal), GDC 2016, slides — https://media.gdcvault.com/gdc2016/Presentations/LHeureux_Julien_Art_Of_Destruction.pdf (text extracted locally from the fetched PDF)
- **[C1]** 「壊れ物への取り組み いかにベイクを美しく魅せるか」 (Approach to breakables), Capcom (Resident Evil 7), CEDEC 2017, CGWorld report, Sept 2017 — https://cgworld.jp/feature/201709-cedec2017-capcom.html

**Official developer pages and clips**
- **[F4]** *Amnesia: The Bunker – Breaking down doors*, Frictional Games (official YouTube channel), 2022 — https://www.youtube.com/watch?v=adCV8RXTWdo [FOOTAGE]
- **[U3]** *Behind the Wall: Tools of Defense*, Ubisoft, 2014-11-18 — https://www.ubisoft.com/en-us/game/rainbow-six/siege/news-updates/2RyPUypY9DdzDH2433hqTs/behind-the-wall-series-tools-of-defense
- **[B2]** *Battlefield Labs: Destruction*, The Battlefield Team (EA), 2025-04-22 — https://www.ea.com/games/battlefield/battlefield-6/news/battlefield-labs-destruction
- **[B3]** *Get Up and Close with the Melee Combat of Battlefield 1*, EA, 2016 — https://www.ea.com/games/battlefield/news/melee-combat-of-battlefield-1
- **[D1]** *Inside Dead Space: the new Necromorph nightmare*, EA SEED, 2022-10-14 (Mike Yazijian, Pierre-Vincent Belisle, Motive) — https://www.ea.com/seed/news/inside-dead-space-2-new-necromorph-nightmare

**Film**
- **[S1]** *The Shining (film)*, Wikipedia (Production: the door scene) — https://en.wikipedia.org/wiki/The_Shining_(film)
- **[S2]** Heather E. Schwartz, *Jack Nicholson accidentally destroyed this prop in The Shining*, Looper, 2021-07-07 — https://looper.com/454749/jack-nicholson-accidentally-destroyed-this-prop-in-the-shining
- **[S3]** *60 doors were harmed in the making of The Shining*, The Digital Fix — https://www.thedigitalfix.com/the-shining/60-doors-were-harmed-making [SEARCH; the page refused my reader]
- **[S4]** *The Shining (1980) – Here's Johnny! Scene (7/7)*, Movieclips (official upload; © Warner Bros.) — https://www.youtube.com/watch?v=WDpipB4yehk [FOOTAGE]

**Footage (fan uploads; for frame reading only)**
- **[L1]** *Left 4 Dead 2 Facts #73 – Common Infected vs Doors*, Andre Ng, ~2017 — https://www.youtube.com/watch?v=UHE_5DaZXk4
- **[C4]** *Resident Evil 7 – Jack's Wall Entrance Scene*, Achraf Kennedy, ~2024–25 — https://www.youtube.com/watch?v=5xEJMY0EcFE
- **[R1]** *W Tool Battering Ram Lock Side*, WeddleToolCompany, ~2012 — https://www.youtube.com/watch?v=bAyNzsuWBgw
- **[R2]** *Flat Door Bang – Slow.mpg* (ALS flat charge, slow motion), Huard Harral, ~2009 — https://www.youtube.com/watch?v=L9G8Vabnu8g
- Checked, not usable: *Amnesia Door Attack* (too dark) https://www.youtube.com/watch?v=6dzCSa5yDug; *Outlast Chris Walker Knock Door grille* (night vision) https://www.youtube.com/watch?v=uJzDoIh-jds; *Friday the 13th the game, quickest way to break open doors* (too dark) https://www.youtube.com/watch?v=MFALQP3u1DQ; *Can you stop the Chainsaw Man from breaking the door?* (inconclusive at low size) https://www.youtube.com/watch?v=Se9tHEvff40; *R6 Siege Barricade Melee Hacks* https://www.youtube.com/watch?v=Fk6dHH_lE28.

**Wikis**
- **[U2]** *Rainbow Six: Barricade*, Tom Clancy Wiki (wiki.gg) — https://tomclancy.wiki.gg/wiki/Rainbow_Six:Barricade
- **[C5]** *Wooden Boards*, Resident Evil Wiki — https://residentevil.fandom.com/wiki/Wooden_Boards (browser)
- **[C7]** *Lady Dimitrescu*, Wikipedia — https://en.wikipedia.org/wiki/Lady_Dimitrescu
- **[O1]** *Chris Walker*, Outlast Wiki — https://outlast.fandom.com/wiki/Chris_Walker (browser)
- **[O2]** *Outlast*, Outlast Wiki — https://outlast.fandom.com/wiki/Outlast (browser)
- **[J1]** *Friday the 13th: The Game*, Friday the 13th Wiki — https://fridaythe13th.fandom.com/wiki/Friday_the_13th:_The_Game (browser)
- **[T1]** *Sadist*, The Evil Within Wiki — https://theevilwithin.fandom.com/wiki/Sadist (browser)
- **[SH1]** *Pyramid Head*, Silent Hill Wiki — https://silenthill.fandom.com/wiki/Pyramid_Head (browser)
- **[TL2]** *Rat King (The Last of Us)*, Wikipedia — https://en.wikipedia.org/wiki/Rat_King_(The_Last_of_Us)
- **[TL3]** *Through the Valley*, The Last of Us Wiki (search result text) — https://thelastofus.fandom.com/wiki/Through_the_Valley
- **[B1]** *Destruction*, Battlefield Wiki (includes Den Kirson's description of Bad Company 2 buildings) — https://battlefield.fandom.com/wiki/Destruction (browser)
- **[H1]** Hitman Wiki search: *Breaching Charge Mk III*, *Remote Breaching Charge*, *ICA Titanium Crowbar* — https://hitman.fandom.com/wiki/Breaching_Charge_Mk_III (browser, search snippets)
- **[CN1]** *Condemned: Criminal Origins*, Wikipedia — https://en.wikipedia.org/wiki/Condemned:_Criminal_Origins

**Press and guides**
- **[C2]** *Resident Evil 7: How to Get Out of the House*, Twinfinite — https://twinfinite.net/guides/resident-evil-7-how-to-get-out-of-house/ (browser)
- **[C3]** Krista Martinez, *Top 10 Resident Evil 7 Best Moments*, Gamers Decide, updated 2020-04-15 — https://gamersdecide.com/articles/re7-best-moments
- **[C6]** Gabriela Jessica, *Resident Evil 4 Remake: How To Survive The Village Fight*, GameLuster, 2023-04-03 (updated 2023-08-02) — https://gameluster.com/resident-evil-4-remake-survive-village-fight-guide/
- **[C8]** *Resident Evil Village: The Village (first visit)*, Ludo Guide — https://www.ludo.guide/guide/resident-evil-village/the-village-first-visit
- **[F5]** *Amnesia: The Bunker gets terrifying Halloween update*, Downright Creepy, 2023-10-25 — https://downrightcreepy.com/?p=10020895
- **[F6]** Guides on breaking wooden doors in The Bunker: Prima Games https://primagames.com/tips/how-to-break-open-wooden-doors-and-padlocks-in-amnesia-the-bunker and LevelUp (2022-12-29) https://www.levelup.com/en/news/717905/Amnesia-The-Bunker-shows-another-gameplay-encouraging-creative-problem-solving [SEARCH for the hit counts]
- **[J2]** *Friday the 13th reveals new footage of Jason Voorhees hitting a door*, bit-tech, 2016-04-26 — https://bit-tech.net/news/gaming/fridaythe13thtrailer/1
- **[TL1]** Jamie Sharp, *How to kill the Rat King in The Last of Us Part 2*, KeenGamer, 2020-06-30 — https://www.keengamer.com/articles/guides/how-to-kill-the-rat-king-in-the-last-of-us-part-2/
- **[D2]** Christopher Livingston, *Dying Light 2 has another way to open locked doors: bash through 'em*, PC Gamer, 2022-02-10 — https://www.pcgamer.com/dying-light-2-lockpick-bash/ (browser)
- **[A1]** Alien: Isolation Steam discussions on doors and vents — e.g. https://steamcommunity.com/app/214490/discussions/0/1697169163418152915 [SEARCH]
- **[DM1]** *Doom: The Dark Ages hands-on*, GamerBraves — https://gamerbraves.com/doom-the-dark-ages-hands-on-medieval-mayhem-meets-demon-slaying-excellence [SEARCH]
- **[R3]** US 10,147,337 B2, *Simulated forcible entry of doors using battering rams*, 2018-12-04 — https://patents.google.com/patent/US10147337

**Project files read (read-only)**
- `Documentation/research/glass/destruction/01_conference_destruction.md`, `02_glass_in_games_and_real_glass.md` (the CEDEC lead), `10_glass_destruction_plan.md`
- `Documentation/research/door_break/02_real_door_failure.md` (sibling, read for terms and mapping)
- `Documentation/research/interactables/00_map_constraints.md`, `05_locked_door_type.md`, `10_spec.md`
- `Documentation/research/interaction_audit/04_aaa_references.md`, `10_audit_report.md` §3.7, `FrontRoomsShotTimings.proposal.cs.txt`
- `Documentation/research/hunter/11_squeezed_giant.md`
- `Assets/Scripts/FrontRoomsShots/FrontRoomsShotTimings.cs`, `Assets/Scripts/FrontRoomsMap/FrontRoomsMapHunter.cs:132-135, 985-994`, `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs:76-82, 1255-1272, 2196-2207`, `Assets/Scripts/FrontRooms3DGame.cs:624, 640-648`
- `Tools/Blender/frontrooms_kit/assets/interact_door_leaf_veneer.py` (docstring: 44 mm solid-core leaf)
- `Research/week02/*/SOURCES.md`: no door-break media on disk.
