# NOTE · The station as the first room: narrative constraints and ideas (叙事设计, 2026-10-22)

> **DECIDED 2026-10-22 15:2x, in the 叙事设计 chat (account A), Red: "按推荐，第一个房间选方案 1".** That is option 1 of the tutorial note: station first, EXIT in view, the first send after the player leaves the room, Floor 2+ stations found by ear. The narrative recommendations below came in the same message, so they stand: the front-desk fiction, the visitor role, and the main-entrance plate moved from the stair door. Strings: `narrative/40_floors_exit_tutorial_strings.md` §9.

For 游戏视觉's research run `station-first-room` (`wf_89885526-506`). The synthesis stage reads this before it writes `60_first_room_research.md` and `61_for_red_first_room.md`.
- Red's words were checked in the 游戏视觉 transcript (5656cffd, 22:14Z = 15:14 PDT).
- The structural rules are in the 系统设计 and tutorial notes next to this one. **This note covers only the fiction, the strings, and the narrative reading of the options.**
- The options and the decision belong to the run and to Red.

## 1. The fiction that fits Red's ask: the front rooms and the back rooms

> **Walls decided 2026-10-22 15:4x:** Red picked **walls 2 · Post Room** in the 游戏视觉 chat ("这个版本可以", relayed by 游戏视觉), against the sweep's oak-wainscot "Lobby Post".
> - The finish: cool blue-grey eggshell paint #9AA3A6; a 6 in navy-charcoal crash rail at 0.78–0.93 m; VCT #B2AEA3 with a dark border; a dark-bronze cased frame with stainless corner guards.
> - Per R1 it is the same on every station.
> - **The fiction holds and gets more period-true.** The first room is no grand lobby. It is the building's **security post at its entrance**: the one room that is *kept* (painted, railed, tiled) while the back rooms are only papered. Real guard posts of the CRT era were function rooms, "rarely things of beauty" (agent A, P31, post-era trade retrospective).
> - Paint, a crash rail and VCT are also the vocabulary of 1990 institutional waiting rooms (clinic, DMV), so direction B on Floor 2+ reads truer in these walls.
> - The two lines below that said "front-of-house wallcovering, trim" and "lobby security desk" are read through this: same role, plainer finish.


- **The title already names the first room.** The front rooms are where a building receives people: the reception, the lobby desk, the guard's post. The back rooms are storage and service space: Level 0's yellow paper and the stock rooms. The founding photo was a furniture store's back room.
- **So "its own walls, distinct from Level 0" is the story itself.**
  - The first room is the one *kept* room: paint, a crash rail, tile, and a desk built to face visitors (Red's walls 2 · Post Room; it was "front-of-house wallcovering, trim" before the pick).
  - Every door out of it leads into the unfinished building. The yellow paper begins at the threshold.
- **The HUD already says it:** Red approved `LEVEL 0 / FRONT ROOMS` for the start rooms (10-22).
- **Refined 10-22 after 关卡设计's plan.** The station is the first *map* room behind the start door. So **the start door, seen from inside the station, is the building's main entrance**: it locks behind you, carries the business-hours plate on its station face, and is `NO REENTRY` on the placard. There is no extra door. The fire-door point below now belongs to the map's doors further into the back rooms.
- **The door between them is code-true.**
  - UBC 1991 §702(c)2 puts a retail building's stock rooms behind a one-hour separation with self-closing fire doors (`01_research.md` §4).
  - That explains why the start door swings shut, latches and won't reopen (`NO REENTRY`, unchanged). It is the fire door between front and back.
- **Period type for the first room: the security post at the building's entrance** (`01` §1, types 1 and 4, lobby desk and guard post; in Red's plain Post Room finish).
  - It is the most normal place in a 1990 building for a guard's post: facing the entrance.
  - The annunciator really sat there, in the fire department's entry path (`01` §2, P13).
  - B's identity ("the floor reception the guard took over") becomes "**the building's front desk, where the night guard sits**". That is more period-true, not less.
  - Deeper floors keep B, A and C from `10_directions.md` as smaller night posts in the back rooms.
- **The main entrance is locked after hours.**
  - The UBC 1991 §3304(c) exception-1 plate, `THIS DOOR TO REMAIN UNLOCKED DURING BUSINESS HOURS`, belongs on exactly these doors: the main exit of a Group B building.
  - **Proposed:** move the plate there from the stair door (`40` §4). The stair door keeps its lit EXIT sign and its `STAIR A` plate.
  - The main doors are solid or have a vestibule, **never glass to an outside**: the game has no outside. The way back out is closed, honestly, so the only ways are in and down.

## 2. The player's role and the guard's story, from the first second

- **The player is an after-hours visitor.**
  - A `PLEASE SIGN IN` tent card and the visitor register sit on the desk. The register's last line is blank: that line is you (no text entry; EXAMINE only).
  - The CAUGHT subtitle `OCCUPANT ACCOUNTED FOR` (Red, 10-22) closes that loop at the end of the run.
- **The first image of the game is the empty post:**
  - the chair turned to the set;
  - the jacket;
  - the cold cup;
  - the log that stops at `0200 TOUR 3` (`01` §6);
  - the radio charger empty.
- The guard went into the back rooms on his round and did not come back. The player follows him in.
- No cutscene, no text card, and no body. The absence is the hook (`02_refs.md` §2).
- **The plate as the last thing read before the first decision** (the placard comes first, in the first sightline; corrected 10-22 after 总控's check): `STAIR KEY` / `SILENT ALARM: REMOVAL SIGNALS PANEL` (decided, unchanged).
  - If the key is in the first room, the building warns you honestly before your first decision. You take the key anyway, the set dies, and the responder is called.
  - That is the cleanest possible lesson in cause and effect, and it stays EGRESS-true.

## 3. The tutorial note's three options, read for the story

| Option (tutorial note §4) | Story | Narrative view |
|---|---|---|
| **1 · Station first, EXIT in view** | You arrive at the front desk. STAIR A's locked door is visible from it (a real lobby layout: the stair door near the entrance). You read the rules, take the key, the set dies, you walk to the stair, and the lamps dip | **Preferred.** The whole loop fits in the first minutes, in the one kept room. The guard's absence, the honest plate and the lock-before-key (seen first, taken second) all work. Floor 2+ stations become the posts you find by ear, in the back rooms |
| 2 · Station first, EXIT found later | The same start, but the stair lies somewhere in the back rooms | Works for the fiction. It loses "the lock is seen first", and puts more weight on the placard's line 4 and the HUD's `OPENS THE STAIR DOOR` |
| 3 · Reception first, key not here | A front desk with no stair key: its hook is empty on the board | Possible, but it needs one more story beat (why the key is elsewhere: "signed out on his round"?), and the station is not the first room. Red asked for the station to be first |

**Whichever option is picked:**
- The first send waits until the player has left the first room (tutorial note §2.3). The fiction allows it: the panel signals at the take, and its responder is far away.
- The first room is never an Arrive spot.

## 4. Strings affected (all 叙事's; type through 平面视觉)

| Item | Under option 1 | Note |
|---|---|---|
| HUD start-room line | `LEVEL 0 / FRONT ROOMS` | Already approved; it now names the reception |
| Placard (sheet A) | `YOU ARE HERE` at the desk; the legend's `SECURITY DESK (KEY)` at the same spot; a short route to `STAIR A` | 平面视觉 redraws the route and the mount (inside the room, by the desk). The four lines and line 4 `KEY AT SECURITY DESK.` stay: a building rule sheet holds on every floor |
| Main entrance plate | `THIS DOOR TO REMAIN UNLOCKED DURING BUSINESS HOURS` | Moves here from the stair door (proposal, §1) |
| Desk front lettering (optional) | `SECURITY` | One name per thing: the placard says SECURITY DESK. Never `LOBBY` (the Backrooms wiki's Level 0 name), never `STATION` (Escape the Backrooms' sign) |
| Register, tent card, log, plate, sign-out sheet | as in `01` §6 | Unchanged |
| Start door (to the back rooms) | `NO REENTRY` on the placard plan | Unchanged; it is now also the fire door of §1 |

## 5. Keep clear of (IP and canon)

- **A24's *Backrooms*:** its doorway is in a furniture **showroom**. Our first room is the *building's* front desk, never the store's sales floor:
  - no furniture vignettes, price tags or mascot;
  - FRONT ROOMS FURNITURE appears only in the TV ad (A24 option A, Red).
- **The Backrooms wiki's "The Lobby" (Level 0):** never use the word LOBBY on a sign. The room is not yellow (Red's own ask), so it won't read as the wiki's Level 0.
- ***Severance*'s lobby:** no huge white symmetric hall with a lone desk.
- **FNAF's office:** you leave the post; you never sit at it.
- **Kane Pixels:** no recorder, timestamp or NO SIGNAL at the desk.

## 6. What does not change

- G1–G10 (`20_layout.md` v1.2) for the room's interior. In particular G2's two ways out, because the hunter is still sent here after the take. The locked main entrance does not count as one.
- The plate's wording, the floor tokens, `STAIR A`, the stair signs (`40` §5), WTCZ-TV 61, and the live-clock date rules (`01` §5).
- Dark is never cover, never a radar, and no RUN or HIDE as advice.
