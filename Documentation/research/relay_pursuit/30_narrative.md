# Narrative: the alarm that sends it

Status: proposal, revision 3, 2026-10-03. Written by the narrative chat ("Design the narrative of the phosphor wallpaper print") for the Relay pursuit redesign.
- **Main document:** `Documentation/RELAY_PURSUIT_REDESIGN.md` **v2**, owned by the systems chat (系统设计).
- **Scope:** fiction only. No state, timing or code change.
- **Direction:** EGRESS, which Red chose on 2026-10-03 (`research/wallpaper_motion/30_narrative_phosphor.md` §4). B/C/D stay as alternates in §3.
- **Revision 3 aligns with v2 §13 step 12:**
  - one stage-1 reading;
  - the withdraw rule;
  - EAR GROUND and ALARM ROUTE are out;
  - the chase wave is centred on the player's room set;
  - the Relay-wake print layer is out;
  - the lock-on cue is a door-holder release plus one damped chime;
  - the 待核 items are checked in §7.

## 1. Premise (EGRESS)

**The fiction:**
- The building has a fire-alarm system. The Relay is the stage of it that trips: the thing the building sends to clear a zone.
- When it is absent, it is not hiding: the panel is on standby.
- It comes when a device in a zone trips. Devices call to the **device**, never to you (v2 §6).
- It leaves when the zone is reset.

**The pairing:** the wallpaper is the same building's exit plan, so the two systems belong together. The plan shows occupants the way out; the alarm sends something to sweep them out.

## 2. State ↔ fiction (EGRESS, v2 loop)

| State | Fire-alarm reading | What the player sees and hears |
|---|---|---|
| AWAY | Standby, normal condition | Lamps keep their own temperaments (steady, stutter, failing, dead) |
| CALLED | An initiating device trips in a zone: smashed glass, a foiled window, an alarm push-bar door, a detector room. Noise adds to the panel's Attention | Optional, Red's Q3: one dry relay click from the ceiling; the device's own sound where it is |
| ARRIVE | The alarm relay pulls in somewhere out of sight, and the responder enters the floor | Nothing at its position. The player learns only through the staged warning (§5) |
| INVESTIGATE | Alarm verification: the panel re-checks the zone before full alarm | It walks the tripped zone and listens, with error. Stages 1 and 2 when it is near (§5) |
| CHASE | Full alarm, locked to your compartment | The stage-3 lock-on cue (§5), then the chase wave, **centred on your room set** (never on its cell) |
| SEARCH | A room-by-room sweep, as a fire warden checks every room | It searches nearby rooms, then gives up |
| WITHDRAW | Reset / restoral | **It leaves the way it came if that way keeps clear of you, otherwise by the farthest way out.** Optional (Q3): RESTORE, the room's hum returns |
| Caught | The occupant is accounted for | Optional Caught line: `OCCUPANT ACCOUNTED FOR` (map chat's HUD; pending Red) |

What EGRESS fixes is the Relay's **role** (the alarm's responder), not its body. The best match among the open looks is hunter direction B "Night Shift" (the electrician with a lens face, `research/hunter/10` §4), but the fiction works with all four.

## 3. Alternates

**B SUB-PATTERN:** it is what puts people into the paper.

| State | Reading |
|---|---|
| AWAY | It stays behind the paper, out of reach of our side |
| CALLED | The pattern is wounded: torn paper, broken glass |
| Near (stages 1–2) | The kept in your room's paper press forward in fear, and your lamps falter |
| WITHDRAW | Back behind the paper once it has fed |
| Caught | You join the sub-pattern |

**C BLAZES:** it is "IT" in the wanderers' notes. There is no explanation, only rules they learned.

| State | Reading |
|---|---|
| AWAY | `IT LEAVES WHEN THE LIGHTS COME BACK` |
| CALLED | `IT COMES FOR GLASS` |
| Near (stages 1–2) | `WHEN YOUR LIGHTS GO IT'S CLOSE` and `THEN YOU HEAR IT WALK` |
| WITHDRAW | The lights come back |
| Caught | A new set of initials, maybe |

**D DRAG LINE:** it is the hunt's hound. This fixes its role, not its look.

| State | Reading |
|---|---|
| AWAY | Kennelled |
| CALLED | The huntsman puts hounds into cover (*draw a covert*, to verify); glass is the cry of the find (*view halloo*, to verify) |
| Near (stages 1–2) | The hunt drawing your covert; then the hound on the line, heard before seen |
| SEARCH | A **check**, after losing the line (verified) |
| WITHDRAW | Taken home (*blowing for home*, to verify) |
| Caught | A run ended |

## 4. Trigger rooms and devices (v2 §6, §10)

**Rules:**
1. **Readable before entry.** Every device or trigger room has one 1990 object visible from outside it, or a sound heard before entry.
2. **The wallpaper never marks a trigger room.** That would make it radar.
3. **Inner-zone trigger rooms (TO CONFIRM with the level-design chat).** Doors and windows exist only on zone borders, so a trigger room inside a zone must be read through an arch or open-edge sightline.

| Device (EGRESS) | Read before you commit | 1990 basis (§7) |
|---|---|---|
| Alarmed exit door | Push-bar door with `EMERGENCY EXIT ONLY — ALARM WILL SOUND` | Exit-alarm push bars |
| Detector room | A ceiling smoke detector with a red-banded base and an `ALARM ZONE` card | Smoke detectors in commercial buildings |
| Electrical room | `ELECTRICAL ROOM — AUTHORIZED PERSONNEL ONLY`, panelboards, transformer hum | The building's wiring |
| Foiled window | Silver foil tape round the glass perimeter, plus a magnetic contact on the sash | Intrusion-alarm foil and contacts |
| Any window (smashed) | The glass itself | Breaking glass is heard; foil makes it a device |
| Dead-circuit room (a sanctuary, v2 §10) | An amber TROUBLE lamp, a detector hanging by its wires, a bell with no clapper, an OUT OF SERVICE tag, a placard `ALARM CIRCUIT OUT OF SERVICE · NO DETECTION IN THIS ROOM` | A panel trouble signal on a dead zone |

**Alternates:**
- **B:**
  - a room with the wallpaper torn off in strips;
  - an older, yellower paper;
  - a water-stained room;
  - windows.
- **C:**
  - an abandoned camp;
  - tallies on a door frame;
  - a tin-can tripwire;
  - a copier warming up;
  - a phone off the hook.
- **D:**
  - furniture piles (cover);
  - Office cubicle islands;
  - Tall halls (in view);
  - windows.

## 5. Staged warning (EGRESS, v2 §9)

The lamps never change where the Relay is. Every warning happens around **you**.

| Stage | Game | Fire-alarm reading |
|---|---|---|
| 1 | Your room's lamps burst irregularly, as a group, in a smooth sag under 3 Hz | **Pre-alarm:** the panel switches your compartment to emergency power and tests it (待核, §7). This is the only stage-1 reading. |
| 2 | Its muffled steps, more muffled through walls | The sweep is on your floor: a warden checking rooms |
| 3 | It sees you: the lock-on cue, then Chase | Alarm verified; full alarm locked to your compartment |

**Lock-on cue (v2 §9.4):**
- A magnetic **door-holder release**: a low thunk, "the building just acted";
- plus **one damped chime strike**. It is the only pitched chime in the game.

**Other cues outside the ladder (Red's Q3):**
- **CALLED:** one dry relay click from the ceiling.
- **The device's own sound** where it is: door-horn chirp, detector chirp, foil bell tap.
- **RESTORE:** the room's hum returns after it leaves.

**The layers** (each tells one thing only):
- **Your room's lamps** = it is near you.
- **The wallpaper** = where to go.
  - It is being redesigned as pattern-native cues, `research/wallpaper_motion/32_pattern_native_hints.md`.

The Relay-wake print layer and the ink's ALARM ROUTE / EAR GROUND are **not in v2**. None of the stages is a stealth rule (LD R13).

## 6. Writing rules

1. **Dark is never cover (LD R13).** No line, sign or prop may say or imply that darkness hides you or it. Its sight has no light term. Your lamps burst because the panel is testing your zone.
2. **The staged warning is a warning, never a stealth rule.** All Relay state text is hidden from the HUD (assist toggle only), so these stages are the player's only proximity tells. They stay local and honest.
3. **Silent building-wide alarm.** No bell. A device's own local sound is the player's feedback for what they did. Sound is the sound chat's call.
4. **Period.** 1990 hardware, US English caps on signs, no real brands, printed dates ≤ 1990.
5. **The fiction lives in objects and signs.** The HUD never names the system.
6. **One placard, not two.** v2's start-room placard wording ("IN CASE OF FIRE: WALK, DO NOT RUN. CLOSE DOORS BEHIND YOU.") goes on the Red-approved evacuation plan (`30_narrative_phosphor.md` A.12) as its footer. It hangs on the map side of the start door, because the title stream rooms stay free of notes. Sanctuary rooms may carry their own copy.

## 7. Verification of the 待核 items (checked 2026-10-03)

Every item existed in the US by 1990. Three exact wordings, and one sound, stay unverified.

| Item | Verdict | Evidence |
|---|---|---|
| Magnetic door holders released by the alarm | **VERIFIED** (by 1965; code-driven by the 1980s) | Patent US3204154A (1962/65): holds the door until "a signal from a fire detection system": https://patents.google.com/patent/US3204154A/en. NFPA 101 moved door holders to smoke-detector release: https://fireengineering.com/fire-safety/door-holders-for-fire-doors. An HHS decision cites the 1985 LSC §13-3.4.4: https://webharvest.gov/peth04/20041029111740/http://www.hhs.gov/dab/decisions/CR1009.html |
| The release's low "thunk" | **UNVERIFIED** | No source describes the sound. The magnet lets go, then the closer and latch make the noise. Treat it as Foley design, not fact |
| Alarm verification (the panel re-checks a detector) | **VERIFIED** (trade topic 1986–88) | Fire Journal, March 1986: https://firedoc.nist.gov/article/qXcxXYQBWEcjUZEYvA4Z. Reset-and-recheck, patent US4568924 (1986): https://patents.google.com/patent/US4568924A/en |
| Pre-alarm stage | **VERIFIED as a concept by 1985** | Patent US4556873 (1985), "pre-alarm means": https://patents.google.com/patent/US4556873A/en. Addressable panels date from the mid-1980s: https://en.wikipedia.org/wiki/Fire_alarm_control_panel. A US product with pre-alarm by 1990 is not confirmed |
| Amber TROUBLE lamp + buzzer; supervisory signals | **VERIFIED, dated April 1990** | Fire Engineering 1990-04-01, "an amber light and audible signal at the panel": https://www.fireengineering.com/firefighting/increasing-your-fire-alarm-literacy-3/ |
| Coded chimes in hospitals | **VERIFIED** (looking back, not dated) | Older health-care occupancies used coded signals, with bells or chimes: https://www.csemag.com/?p=15206 |
| Window foil tape + magnetic reed contacts | **VERIFIED** (patent filed 1987) | US4808973A: openings "often" protected by bonded metal tape; reed switches: https://patents.google.com/patent/US4808973A/en. That the foil was "silver" is UNVERIFIED (snippet only) |
| Combination fire + burglar panels | **VERIFIED** (by 1971 / 1981) | https://patents.google.com/patent/US3603973A/en and https://patents.google.com/patent/USD258578S/en |
| Alarmed push-bar exit devices | **VERIFIED** (1977; common by 1986) | Detex US4006471 and Emhart US4631528: https://patents.google.com/patent/US4006471A/en, https://patents.google.com/patent/US4631528A/en. The exact pre-1990 sign text `ALARM WILL SOUND` is UNVERIFIED |
| Glass-break detectors | **VERIFIED** (shock by 1978, acoustic by 1986–87) | US4091660 and US4668941: https://patents.google.com/patent/US4091660A/en, https://patents.google.com/patent/US4668941A/en |
| "WALK, DO NOT RUN" | **VERIFIED as standard US wording** (current) | https://www.utep.edu/ehs/emergency-action-guide/evacuation-procedures.html. Pre-1990 exact wording: UNVERIFIED (old NYC theatre notices, snippet only) |
| "CLOSE DOORS BEHIND YOU" | **VERIFIED** (USFA, undated) | https://www.usfa.fema.gov/gallery/pictographs/pictograph13.html |
| The elevator line | **Partly VERIFIED.** NYC code wording in force by 1990: `IN CASE OF FIRE, USE STAIRS UNLESS OTHERWISE INSTRUCTED` | https://nyc-laws.readthedocs.io/en/latest/nycadmincode/t27/c01/sch06/art09/. The common sign `IN CASE OF FIRE DO NOT USE ELEVATORS` is not code text |

**Consequences for the strings:**
- **The placard footer stands:** `IN CASE OF FIRE: WALK, DO NOT RUN.` / `CLOSE DOORS BEHIND YOU.`
- **The optional elevator line** should use the code wording, `IN CASE OF FIRE, USE STAIRS UNLESS OTHERWISE INSTRUCTED`, or be dropped.
- **The lock-on thunk** is a design choice: the door-holder release is real, its sound is invented.

**Still open from revision 2:**
- Hunting terms (B–D alternates only): *draw a covert*, *view halloo*, hound *music*, *blowing for home*. Verified: **check** and **goes to ground** (https://en.wikipedia.org/wiki/Fox_hunting); drag hunting (https://en.wikipedia.org/wiki/Drag_hunting).
- Emergency-lighting tests: monthly 30 s and annual 1.5 h (NFPA 101 §7.9.3, current edition; the 1990 wording is not verified; https://www.inspectpoint.com/resources/articles/emergency-lighting-and-exit-sign-testing-requirements).
