# 01 · Tutorial research: how horror games teach, guide and hand out goals

Written 2026-10-22 by 教程设计 (the tutorial design session). Research only: nothing in the game was changed.

**Why.** After the 10-22 class, Red asked for a tutorial that gives players somewhere to go and does **not** pop up a text block at each step. Red also asked for a full survey of how mainstream AAA horror games run their tutorials today. The FrontRooms proposal built on this is `10_proposal.md`; the per-line asks are `20_requests.md`.

**Corpus.**
- A 4-agent research workflow run by 关卡设计 on 10-22 (`wf_98230184-c3f`): AAA horror tutorials; spatial guidance by light, sound and landmarks; keys, collectables and interaction flow; diegetic maps, signs and placards. 64 findings with URLs.
- I re-opened the 15 sources this proposal leans on hardest (§9) and added the sound-propagation and wall-acoustics sources.

**Labels.**
- **[read]**: the primary page was opened and read (by me or by a workflow agent).
- **[search]**: only a search-engine summary was read, because the page was blocked, paywalled or too large.
- **[memory]**: general knowledge, not checked.

Source IDs (T1…) are in `SOURCES.md`. No media was downloaded. Quotes are paraphrased.

---

## 0. The short version

1. **AAA horror is not text-free.** It shows a one-line control prompt the first time a verb is needed. What it avoids is text that tells you **where to go**. Direction comes from the world: blood trails, voices, maps, light (T20, T21, T27, T31).
2. **The goal is handed over in the world within minutes, in one line, and then the player is left alone.** RE2's gas-station key, Outlast's dying officer naming the security room, The Bunker's dying soldier, Amnesia's note to yourself (T6, T11, T13, T30, T33).
3. **The core loop is rehearsed once in a tiny, safe space before the big map opens.** RE2's gas station (key on the wall, locked door next to it), The Bunker's trench prologue, SOMA's apartment, RE7's guest house (T11, T30, T35, T26).
4. **The threat is heard or glimpsed before it can hurt you, and its signal stays honest.** Silent Hill 2's radio and the creature fading into fog, the poison zombie's wail in HL2, Mr. X's footsteps, The Bunker's Beast in the walls (T1, T22, T40, T41).
5. **Persistent hunters are introduced in stages.** Alien: Isolation unlocks the alien's behaviours only after the player has escaped it several times, and a menace gauge sends it away after a peak (T8). RE7's director said fear fades once players understand an enemy (T12). So reveal its rules one at a time.
6. **Lock before key.** RE2's suit doors, RE7's animal doors and Unexplored's generated cycles show the locked door first, so the key search already has a purpose (T10, T11, T44).
7. **Contrast carries information, quantity does not.** An evenly lit space tells the player nothing (Valve's lighting playtests, Rogers' squint test). An even fluorescent office is the worst case, which is our office (T7, T47, T48).
8. **Expect the first cue to fail and stack two or three.** Valve added birds, crows, smoke and a klaxon when players missed a landmark or a gate that had opened (T1).
9. **A sound beacon needs a source you can see, sound that comes through the doorway rather than the wall, and readable distance.** Naughty Dog built portal propagation so a player could tell someone was in the next room and be led into spaces (T2). Outer Wilds players blamed a nearby rock for a far signal until distance was made readable (T3).
10. **One in-world sign can carry the rules if it holds at most four short imperatives, sits on the path everyone walks, and sits next to a progress number.** The Exit 8 (T4, T60). Signs alone fail in complex buildings: Control added a map after testers got lost (T5).
11. **No game found uses a literal fire evacuation plan as its tutorial, or a TV as its main navigation beacon.** The nearest are The Bunker's wall map, The Exit 8's notice, P.T.'s and Silent Hill's radios and Lethal Company's horn. Both of Red's ideas are open ground, not copies.

---

## 1. Our own evidence (why this is urgent)

- **8 of 9 playtesters could not name the goal** (systems.json, C22 rationale).
- Round-two runs were fast and aimless: about 160–170 s, 7 to 15 zones crossed, 0 or 1 door used (`GRAPHIC_VISUAL_TASKS.md`, playtest deck; testers not named here).
- One tester could not tell whether the monster hunts by sight or by sound; another did not understand the stamina bar (`briefs/course.md`, items the reader missed).
- **What the game teaches today:**
  - one timed text card, "Shift to sprint…", for 9 s (`FrontRooms3DGame.cs` `CalmHint`);
  - crosshair prompts (E · OPEN, E · TAKE KEY);
  - the evacuation placard at the start door, with EXAMINE.
- **What it cannot teach yet:**
  - keys open nothing (D9: `doorsNeedKeys` off);
  - the EXIT is being built (N1, C22);
  - the placard's glow legend points at wall arrows that are not in main (N4, W6 CLONE_ONLY).

So the tutorial problem is first a **goal** problem, and only then a teaching problem.

---

## 2. How mainstream AAA horror runs its first 10–30 minutes

| Game | How the goal arrives | How verbs are taught | First threat | Text on screen | IDs |
|---|---|---|---|---|---|
| Dead Space (2008, remake 2023) | Radio/video calls; blood leads to the control-room door | Diegetic holograms; a tip written in blood next to the first weapon; on-demand floor locator line | Crew killed while Isaac is separated from them | One-line holographic prompts | T20, T21 |
| Silent Hill 2 (2001, remake 2024) | The wife's letter; a map found on the car seat | Little; the map annotates itself in red | Creature fading into fog; the radio's static warns before the first monster | None beyond the letter and map notes | T22, T23 |
| Resident Evil 7 (2017) | The wife's email and video | VHS tapes on a TV replay the same house from another body, revealing a hidden route; "no basic hints" | Mia; a hatchet lies close at hand | One-line prompts | T12, T24, T26 |
| Resident Evil 2 (2019) | A wounded man; find the gas-station key | Key on the wall round the corner from the first zombie; it opens the locked door | First zombie in a tiny loop; Mr. X only after a long build | Objective line plus prompts | T11, T25, T40 |
| Alien: Isolation (2014) | Companion voice | Map consoles on walls; the hunter's behaviours unlock as you survive | Alien held back for about an hour, glimpsed long before any fight | Prompts plus voice | T8, T27, T28 |
| Amnesia: The Bunker (2023) | A dying soldier: the exit is sealed, find dynamite and a detonator | A separate trench prologue is the tutorial; a wall map in the safe room | The Beast heard in the walls first | Short text in the prologue | T13, T30, T31 |
| Outlast (2013) | A dying officer: the entrance unlocks from the security room | Playtests showed players were not scared early enough; content was moved and door and camcorder controls simplified | Chris Walker ambush | One-line prompts | T6 |
| Alan Wake 2 (2023) | The case board | The diegetic board failed playtests until new clues were forced into view | Slow first act | Short clue cards | T32 |
| Amnesia: The Dark Descent (2010) | A note the hero wrote to himself | One framing card before play: don't fight, run | Shadow, never in a cutscene | Start card plus the note | T33, T34 |
| P.T. (2014) | None | None: one looping corridor; a radio as the clue channel | Lisa | None | T36 |
| RE4 (2023) vs Village (2021) | A clear path through the village vs a siege with no readable end | Gradual ramp vs a fight that comes before any practice | — | Prompts plus cutscenes | T37 |
| SOMA (2015) | A threat-free apartment | Inspect objects freely before any danger | Only after the scan | None to one line | T35 [search] |
| The Evil Within (2014) | — | Watch the butcher work, then sneak; frequent prompts | Sadist | Frequent prompts | T38 |
| The Callisto Protocol (2022) | Linear prison | Diegetic HUD; audio-led | — | Prompts | T39 [search] |

**Patterns.**
- **Goal in one line, in the world, early;** then silence.
- **Rehearse the loop once, small and safe;** then open the map.
- **Hear or glimpse the threat first;** let it grow in stages (Alien, Mr. X).
- **Prompts for verbs are accepted; direction text is not.** Our rule can be: a verb may get a one-time prompt at the crosshair (we have that); direction never gets text.
- **Counter-examples.** Total opacity works only in one tiny corridor that a community solves together (P.T.). A threat with no readable way out reads as confusion, not fear (the Village siege critique).
- **Developers' own evidence.** Outlast moved content because testers were not scared early (T6). Alan Wake 2 rebuilt its diegetic board because testers missed updates (T32). Alien's lead said scripting the alien would kill the tension (T27). Frictional's Grip says never reveal the monster in a cutscene (T34). RE7 says fear drops once an enemy is understood (T12).

---

## 3. Guidance without text: light, landmarks, sound

### 3.1 Light hierarchy
- Valve's playtesters walked toward lit areas. Removing scattered lights and keeping one warm glow down a street pulled players forward (T7).
- Robert Yang (GDC 2018): contrasting key lights on the critical path, and light the same kind of object the same way every time (T47).
- Scott Rogers: squint at the screen and the main path should be the brightest area; after the fireworks Disneyland dims everything except the exit (T48).
- **For us:** an even grid of fluorescent tubes is the "too many lights" case. A TV's cold, moving light in an otherwise ordinary room is exactly the single focal light these sources ask for.

### 3.2 Stack redundant cues; players ignore HUD markers
- HL2 commentary (T1):
  - birds fly toward the Citadel so players notice it;
  - when some players missed that a canal gate had opened, more cues were added;
  - a crate was used as a sign because players had already learned to read crates;
  - players under fire ignored HUD indicators and watched the enemy;
  - the poison zombie is heard wailing before it is seen.
- **For us:** sound + light + motion at the beacon, and a loud change when it switches off.

### 3.3 Landmarks and cognitive maps
- Oueijan (thatgamecompany, GDC 2021): players get lost when their mental map stops matching what they see. Use Lynch's paths, landmarks, districts, edges and nodes; test without HUD (T49).
- HL2 Episode Two: the goal is glimpsed early through a hole in a wall; landmarks first, a voice second, an aid last (T46).
- **For us:** a procedural backrooms maze removes landmarks on purpose. One strong landmark per floor (the TV room, then the lit EXIT) is the minimum.

### 3.4 Sound beacons and propagation
- **The Last of Us** (T2, [read]): the directors asked audio for a way to tell that someone is in the next room, and to lead the player into spaces. The team built a portal-based propagation model; in a room with two doors you can tell which door an enemy is nearer.
- **Rainbow Six Siege** (T14, [read]): maps are split into spaces joined by connection points (doors, windows, breakable walls). Sound takes the best open route, and that route sets where the sound seems to come from.
- **Wwise rooms and portals** (T15, [search]): sound in another room is positioned at the portal; a diffraction angle drives obstruction (a low-pass); with no portal path the sound goes through walls, attenuated.
- **Steam Audio** (T16, [read] by the workflow): pathing along hallways, round corners and through doors and windows; plug-ins exist for Unity and FMOD Studio.
- **Wall acoustics** (T17, [read]): for a single wall, transmission loss rises by about 6 dB per doubling of frequency (the mass law). Walls are low-pass filters, so "far away and behind walls" really does sound like bass without words.
- **Outer Wilds' Signalscope** (T3, [read]): players aimed at the rock in front of them and assumed the signal came from it. Fixes: readable distance, separate channels per signal type, and a tutorial that teaches "this sound comes from this object".
- **Silent Hill's radio** (T41, [read] by the workflow): static rises near enemies but gives no direction, so it builds dread but does not guide. In Silent Hill f it mostly flags nearby radios, which makes it a point-of-interest detector.
- **P.T.'s radio** (T36): the broadcast is the clue channel and changes every loop.
- **Lethal Company's horn** (T42, [search]): a beacon that guides lost players home and also draws some monsters, so using it is a risk decision.
- **For us:** the TV beacon must
  - (a) be taught next to its source once;
  - (b) arrive through doorways, not walls;
  - (c) carry distance in its muffling;
  - (d) never be confused with the Relay's cues (separate "channel": a broadcast, not steps, hum or chime).

### 3.5 Paint vs diegetic cues
- RE4 remake's yellow paint started a public debate in 2023. One developer reported adding paint after a tester was stuck for 20 minutes (T43, [search]).
- Silent Hill 2's 2024 remake said no paint: map notes, blood and roadblocks instead (T43).
- Mirror's Edge reserves one colour for the route; its designer later doubted red, which usually means stop (T50).
- **For us:** use period "paint":
  - the EXIT sign (C23, decided);
  - the key fob colour;
  - the TV's glow;
  - the phosphor ink once it ships.

### 3.6 On-demand aids (the last rung)
- Dead Space's locator line (T20, T21).
- TLOU Part II's optional camera nudge and audio pings, off by default (T51).
- **For us:** an aid is an accessibility option, never the default.

### 3.7 Studies
- Marples et al. (I3D 2020): 134 people in 30 similar mazes. Spatial audio and subtle lighting each changed how people navigated, and cues **competed** with one another (T52).
- One MSc study found green corridors chosen most and red least (T53, small sample).
- **For us:** all beacons must point the same way, so the TV room and the EXIT route must never pull against each other at the same moment.

---

## 4. Keys, collectables and the interaction flow

| Pattern | Examples | IDs |
|---|---|---|
| **Lock before key.** The player meets the lock first, so the key has a purpose | RE2 suit doors; RE7 animal doors; Village's empty eye socket; Unexplored's path A to a locked goal, path B to the key | T10, T11, T44, T45 |
| **Pairing printed on both halves** keeps random placement readable | RE2 suit symbols; RE7 animals; Bunker dog tag ↔ named locker; Lethal Company door code | T13, T44, T54 |
| **One key, many locks, or one key for all** | RE2 Spade opens 3 doors; Returnal's one key fits any lock | T44, T55 |
| **The map remembers** (tried doors, items seen, rooms emptied) | RE2 red/blue rooms; SH2 red marks crossed out | T22, T25 |
| **A count makes the objective legible** | Lethal Company scan; Escape the Backrooms' four key slots; Phasmophobia's board | T54, T56, T57 |
| **Picking up a key is an event** | RE7's Crow Key starts a boss fight; SH2 remake pickups play a sting and a distortion | T44, T58 |
| **Procedural games standardise the vocabulary** | Returnal door colours; Unexplored stores key→lock edges and uses them for its help hint | T9, T55 |
| **Randomness without a hint frustrates** | Backrooms games with random keycards; players lean on walkthroughs | T56, T59 |

Two notes for us:
- **Unexplored keeps the key-to-lock edge in the level data and uses it for "Pray for help"** (T9, [read]). Our generator can also know which key the player needs next, so the beacon is always the right one.
- **The Bunker shows the limit:** an unannotated wall map, by design, made for a hard game (T13). Our testers are already lost, so our plan must carry more than The Bunker's.

---

## 5. Diegetic maps, signs and placards

- **The Exit 8** (T4, T60, [read]):
  - a notice on the wall beside the start holds the whole rule set in four short imperatives, next to a large exit-number sign;
  - a wrong call resets the number to 0;
  - in the prototype the turn-round happened at the sign, and tester feedback moved it.
  - Players accepted one sign read once as fair.
- **Platform 8** (T61, [read] by the workflow): an in-train display that normally names the next stop switches to a short command when a deadly anomaly appears. The developer added such lines only where testers got stuck.
- **Control** (T5, [read]):
  - Remedy planned navigation by in-world signs only; testers got lost and a map was added;
  - computer screens in the first office drew eyes but said nothing useful, so they were replaced with legible lockdown warnings;
  - red/green door lights became yellow/red plus a padlock for colour-blind players.
- **Amnesia: The Bunker** (T13, T31): the master map hangs in the safe room and cannot be carried; each area's sub-map hangs in a small office at that area's entrance.
- **Silent Hill 2** (T22, T62): building maps on the lobby noticeboard, readable only with light; the hero marks locked doors in red.
- **Alien: Isolation** (T28): wall consoles reveal map sections; nothing appears that could not have existed on the 1979 film set.
- **Portal** (T63): a lit sign at each chamber entrance shows the number, progress and pictograms of what is inside.
- **Half-Life 2's lambda** (T64): one repeated mark near caches teaches "look here".
- **Backrooms games' EXIT-sign levels** (T65, [search]): players learn "sign ahead, carry on; no sign, turn".
- **No game was found that uses a fire evacuation plan as its tutorial.**
  - Real evacuation plans already have every part we need: you-are-here, route, exits, a legend, numbered instructions.
  - NYC Local Law 5 (1973) required the diagram to show **where it is posted** (placard `10_spec.md` S13).

---

## 6. Cutscenes and the camera

- Dead Space's producer set the rule "no HUD, no cutscenes" (T20).
- Grip (Frictional) argues that a monster reveal in a cutscene relaxes the player, and that the first reveal belongs to play (T34).
- RE7, Alien: Isolation and SOMA keep the camera in first person through their openings (T26, T27, T35).
- **For us:**
  - the camera stays with the player;
  - our only "micro-cutscenes" are the existing rig shots: the unlock head-dip, the key pickup and EXAMINE;
  - the Relay is never introduced by a cutscene.

---

## 7. What FrontRooms takes from this (design rules for `10_proposal.md`)

| # | Rule | From |
|---|---|---|
| R1 | **Goal first.** By the end of the first minute the player can say what they are looking for, from the world alone | §2 patterns; our 8/9 |
| R2 | **Rehearse the loop once, small and safe**, before the Relay can hunt | RE2, Bunker, SOMA |
| R3 | **One landmark per floor that the player can hear before they can see it**; teach its sound next to its source once | TLOU, Outer Wilds, L4D |
| R4 | **Sound arrives through doorways**, muffled by distance and shut doors; muffling is the distance readout | TLOU, R6, Wwise, mass law |
| R5 | **Lock before key where possible**; show the pairing on both halves (door and tag) | RE2, RE7, Unexplored |
| R6 | **One sign, at most four imperatives, on the path everyone walks, beside a progress number**; repeat it on every floor | Exit 8, Portal |
| R7 | **Changes of state are loud.** When the beacon's job is done, it switches off visibly and audibly | HL2 gate, Alan Wake 2 |
| R8 | **The hunter is heard before it is seen, and its rules unlock in stages**; never a cutscene | SH2, Alien, Grip |
| R9 | **Verbs may get a one-time prompt; direction never gets text.** Aids are optional assists | AAA patterns, TLOU2 |
| R10 | **Cues never compete**: the beacon and the EXIT route never pull against each other at the same time, and the beacon never sounds like a Relay cue | Marples; RELAY §9 |

These rules sit under FrontRooms' own hard rules:
- cues never a radar;
- no cue points at glass;
- darkness is never cover;
- no Relay state on the HUD;
- photosafety;
- era lock 1990.

---

## 8. Gaps

- GDC Vault was in maintenance. The TLOU *Aural Immersion*, Hitman portal-occlusion, Dead Space UI and Alien: Isolation talks were read only through write-ups and abstracts.
- Some pages returned 403 (PC Gamer, Automaton, Audiokinetic, Prima). Those claims are marked [search].
- No primary source was found for where Alien: Isolation hands over the motion tracker, or for RE2's exact door wording.
- Not covered:
  - Signalis, Mouthwashing, Iron Lung, Content Warning, Inside the Backrooms;
  - Kane Pixels' Backrooms as a navigation case.
  - A follow-up can add them if Red wants.
- The academic cue-strength numbers (ICCS 2025) were not read in full.

## 9. What I re-verified myself (2026-10-22)

T1 (HL2 nodes cn_134, cn_097, cn_127, cn_039, cn_217), T2 (TLOU, 2014-09-06), T3 (Outer Wilds, 2016-09-23), T4 (Exit 8), T5 (Control, 2020-06-18), T6 (Outlast, 2015-01-29), T7 (L4D lighting, 2009-07-08; the page names no studio and no headlights), T8 (Alien AI, 2020-05-20), T9 + T10 (Unexplored; the A/B-path description is T10's, not T9's), T11 (RE2 gas station), T12 (RE7 GDC, 2017-03-06), T13 (Bunker map, 2023-06-07), T14 (R6 Siege, 2020-06-11), T17 (CBD-239, 1985).

Two corrections to the workflow corpus:
- T7 does not name Valve or car headlights.
- T9 does not describe the lock-on-one-arc rule; T10 does.
