# 40 · Floors, the EXIT, the stair and the tutorial: fiction and strings (叙事设计, 2026-10-22)

**Status: v1.2 (2026-10-22 15:2x; §5 safe-zone sign 15:5x): §9 adds the first room (Red, 叙事设计 chat, account A, 15:2x: "按推荐，第一个房间选方案 1"). v1.1 (12:16): Every string here is final: Red answered §8 in the 叙事设计 chat on 2026-10-22 at 12:15 (account B): "全部按推荐（Q1–Q7、照片下载、LOBBY 改名）".**
- **Signed by 叙事:** every string in §3–§6. Peers build on them.
- **Decided by Red (were RED in v1):** B levels (§2, C28); the `FLOOR WARDEN` fine-print line (§3); the CAUGHT subtitle `OCCUPANT ACCOUNTED FOR` (§6); the start-room line `LEVEL 0 / FRONT ROOMS` (§6).
- **Ownership:** 叙事 owns the words. Faces, sizes, separators and layout are 平面视觉's (`type-with-graphic-visual`). Code belongs to its owners.

**Answers these asks:**
- 系统设计's Inbox ask (C22: the EXIT and stairwell fiction, the FLOOR count on CAUGHT, everything the stairwell shows);
- 教程设计's N1 and N2 (`research/tutorial/20_requests.md`);
- 平面视觉's call 4, the DRAFT strings (`research/placard/50_tutorial_placard.md` §6–§7, §11);
- the D13 DRAFT string in 关卡设计's C22 plan (`~/FrontRoomsMapWork/notes/c22_stairwell_plan_v2.md`).

**Rules kept in every string:**
- never RUN or HIDE as advice (`WALK, DO NOT RUN` is the negated period phrase, approved by Red in A.12);
- nothing that implies the dark hides you;
- never the word "relay";
- no live threat information;
- 1990 US building English, all caps, flat;
- no real brands;
- one name per thing.

## 1. The fiction (EGRESS, extended to floors)

- **The building is honest about itself and blind to what it sends.** Its plans, signs and stair hardware tell the truth about doors, keys and levels. They never mention the thing in the halls, except as a fire-safety role if Red picks one (§8, Q2).
- **Every exit leads further in, now literally down.** Each floor has one EXIT: a fire stair, always called STAIR A.
  - The stair serves exactly one floor. Its sign says so: `LEVELS 0 TO B1`.
  - It has two landings and no flights beyond them (Red's stairwell answer, 2026-10-22 13:00, in the 游戏视觉 chat: look A, a walkable two-level stair after C22).
  - Its far door carries the hardware of a grade-level exit, as if it opened onto the street. It opens onto the next floor.
  - The hardware is real. The building's use of it is wrong.
- **Why the stair door is locked:**
  - 1990 buildings locked stair doors for security. The plan says it plainly, `STAIR DOORS ARE LOCKED.` / `KEY AT SECURITY DESK.`, so the lock is not a trick.
  - The guard is gone, but the desk set still plays WTCZ-TV 61 (`STAY TUNED.`; the call sign replaced the real WXRM on 10-22, `screens/30_screen_strings.md` v2.1).
- **Why the alarm never unlocks the stairs** (asked by 游戏视觉; stairwell `10_directions.md` §7):
  - By code a 1990 high-rise unlocks its locked stair doors all at once on a signal from the central control station (UBC 1991 §1807(j) [read, station `SOURCES.md` P01]). The station holds the controls for it, among its twelve required items (§1807(f) [read P01]).
  - That station is a separate, locked room by the fire department's entrance [ESTIMATE: §1807(f) requires FD approval of its location]. It is not the post you start at. The security post you start at (§9) faces the main entrance, locked after hours, and has the annunciator (only its green POWER lamp lit), not the control station's switches. At night nobody is at the controls. The signal never comes. (Reworded 10-22 for option 1, after 总控's consistency check.)
  - Never stated. It is why the plan sends you to a key instead of telling you to wait.
- **Taking the key is an alarm condition.**
  - The key rack is on the panel, like any device. Lifting the key trips it.
  - The panel drops the desk's non-emergency circuit, so the TV dies, and sends its responder to the desk, never to you (`relay_pursuit/30_narrative.md` §1; tutorial `10_proposal.md` §4.1).
- **The stairwell is safe because it is in no alarm zone.**
  - The panel's zones are floors, and a stair enclosure belongs to none of them.
  - The alarm itself does not reset: it is a general alarm, and its level carries down with you. That is why the tier carries over (C22 D13).
- **There is no way out at ground level, and no roof.**
  - LEVEL 0 is the ground floor and the highest one you can reach. Its main entrance is locked after hours (`THIS DOOR TO REMAIN UNLOCKED DURING BUSINESS HOURS`, §9), so the only ways go in and down. Below it are basements, B1, B2, … (Red chose this, Q1 = C28).
  - The stair ID signs say `NO ROOF ACCESS`.
  - The run ends only when you are caught (C22), and the building's last word is about you, not the thing (Q3).

## 2. Floor numbering (Red: B levels, C28, 2026-10-22)

| Floor of the run | **Decided (B)** | Not chosen (count up) |
|---|---|---|
| 1 (start) | `LEVEL 0` | `LEVEL 0` |
| 2 | `LEVEL B1` | `LEVEL 1` |
| 3 | `LEVEL B2` | `LEVEL 2` |
| n | `LEVEL B(n−1)` | `LEVEL n−1` |

- **Why B:**
  - It is building-true for going down: 1990 US elevator panels and stair signs used B, B1, B2 [memory, common practice].
  - It still counts up with depth, so it reads as a score.
  - It stays clear of the Backrooms wiki's numbered level ladder: "Level 1" there is a different world (`stairwell/SOURCES.md` S16, S17). A deep `LEVEL B9` also feels wrong in a good way.
- **Red chose B** (12:15, this line's chat). Every string below is written with B.
- **For 关卡设计:** build the label in one function, `FloorLabel(floor)`: 0 → `LEVEL 0`, n → `LEVEL B` + n. The CAUGHT line, the HUD room text, the key text and any log then change in one place.

## 3. The placard: N1 sign-off (sheet A, sheet A · Phase 0, and the stairwell copies)

**Changes against the DRAFT** (`50_tutorial_placard.md` §7). Everything not listed is signed as drafted.

| Where | DRAFT | Signed | Why |
|---|---|---|---|
| Line 1 | `1 WALK. DO NOT RUN.` | `1 WALK, DO NOT RUN.` | The set phrase with its comma, as in Red's approved A.12 footer ("IN CASE OF FIRE: WALK, DO NOT RUN."; UTEP source, `relay_pursuit/30_narrative.md` §7) |
| Line 4 | `4 STAIR DOORS ARE LOCKED.` / `KEY AT ATTENDANT STATION.` | `4 STAIR DOORS ARE LOCKED.` / `KEY AT SECURITY DESK.` | See the station name below. It is also 3 characters shorter |
| Legend | `ATTENDANT` / `STATION (KEY)` | `SECURITY` / `DESK (KEY)` | Same |
| Plan label | `ATTENDANT STATION` | `SECURITY DESK` | Same |
| Sheet C (later) | `PRINTED FOR LEVEL 3` | `PRINTED FOR LEVEL B3` | Floor scheme (§2) |

**Station name: `SECURITY DESK`, not "attendant station".**
- "Attendant station" is not a 1990 office-floor term; players can't picture it.
- A security desk with a small TV on it, the guard gone and the set still on, is a picture every player already has. It ties the sound (the TV) to the place (a desk) before they hear it.
- Both station modules fit it: the guard desk, and the waiting area's reception counter.
- Internal docs and modules may keep "station" (`Station_*`). Only the player-facing words change.

**Signed as drafted:**
- `EVACUATION PLAN`;
- `IN CASE OF ALARM`;
- `2 CLOSE DOORS BEHIND YOU.` (Red's A.12 wording);
- `3 IF THE LIGHTS DIM,` / `LEAVE THE AREA.` It says "near", never "where", and fits the W1 burst and the Reduce Flashing sag;
- `LEGEND`, `YOU ARE HERE`, `STAIR (EXIT)` / `ONE FLOOR DOWN`, `EXIT`, `STAIR A`;
- the start door: `NO REENTRY` (changed 10-22 from the draft's `NO RE-ENTRY` to the 1973 code spelling, so the placard and the stair plates spell it one way);
- A: `TYPICAL FLOOR`, `NOT TO SCALE`, `THIS FLOOR MAY DIFFER FROM PLAN` (separators are 平面视觉's);
- B: `MATCH LINE — SEE SHEET A-3`, `NOT TO SCALE`, `BEYOND MATCH LINE: NOT TO SCALE`;
- C: `STAIR A · EXIT`, `SHEET C-3 BEYOND THIS EDGE`, `LOCKED DOOR`;
- Phase 0's line 4: `4 EXIT BY STAIR,` / `ONE FLOOR DOWN.`

**Title block** (A.12 form; only the level changes per floor):
- `LEVEL 0 | SHEET A-2 OF 4 | PRINTED 11/89` on Floor 1;
- `LEVEL B1 | SHEET A-2 OF 4 | PRINTED 11/89` on the copy in the stair down to B1.
- **`PRINTED 11/89`, not 03/90** (2026-10-22): under Red's live-clock rule the in-game date is the real month and day in 1990, so a March 1990 print date would lie in the future for anyone playing in January or February. Every printed date is 1989 or earlier (`station/01_research.md` §5). Today's shipped v3 placard keeps 03/90 until sheet A replaces it after C22; no repack. That copy hangs by the far door and shows the floor you are about to enter.

**Fine-print line (Red picked the role name, Q2, 2026-10-22):** `ON ALARM, A FLOOR WARDEN CHECKS EVERY ROOM.`
- **Ship it** on sheet A, sheet A · Phase 0 and the stairwell copies. It is not a numbered line, so the four-line limit stands. It sits near the red block at the zoom-step size; placement and size are 平面视觉's.
- It names a role only. The hunter is never drawn, described or given a body, and the four numbered lines still never name it (placard call 2, Red in the 平面视觉 chat).

**CHIME row** (cut until W3 is live; `10_proposal.md` §5.2). The draft `CHIME: IT HAS SEEN YOU. RUN.` is out. If W3 ships and a line is freed: `IF A CHIME SOUNDS, YOU HAVE BEEN SEEN.` It explains the cue and gives no advice.

**The two v3 legend strings** (in today's game, never reviewed):
- `AS PRINTED`: signed.
- `WALLCOVERING SHOWN AT 1/25 SIZE`: change it only when a wall legend returns with W6.
  - Then use the US drafting form, `WALLCOVERING SHOWN AT 1/2" = 1'-0"`, with the swatch drawn at 1/24. That is 4 % off 1/25, too small to see.
  - A metric ratio on a 1990 US plan is off-era.
  - No repack now: sheet A replaces v3 after C22, and the Alpha is untouched.

## 4. The EXIT, the key and the HUD (N2)

| Item | String | Note |
|---|---|---|
| EXIT sign (C23) | `EXIT` | 游戏视觉's lit kit, unchanged. **v1.2:** the stair door no longer carries the business-hours plate; it moves to the first room's main entrance (§9) |
| Stair letter, maze side of the EXIT door (optional plate) | `STAIR A` | NYC LL5 C26-608.4: stair letters on the occupancy side [read, `stairwell/SOURCES.md` S4]. It matches the plan and the tag |
| Key tag (C18 close-up) | `STAIR A` | Matches the plan, the door and the stair sign |
| HUD key panel | `STAIR KEY` / `OPENS THE STAIR DOOR`, with the floor token on the tag | **Approved by Red 2026-10-22 in the 平面视觉 chat** ("全部按推荐"; as reported by 平面视觉, table in `research/desktop_ui/FIX_CARD_VL217.md` §B). No digits in Bayon (`LEVEL 0 KEY` reads `LEVEL O KEY`) |
| HUD key tag number, KeyHud B | the floor token: `00`, `B1`, `B2`, … | 平面视觉's G2. Two characters up to B9; `B10` and deeper are its layout call |
| Locked EXIT, crosshair, no key | `LOCKED` / `NEEDS STAIR KEY` | **Approved by Red 2026-10-22 in the 平面视觉 chat, with the three rows below** ("全部按推荐"; `FIX_CARD_VL217.md` §B). Correction (09:2x): v1 wrongly said "no string". Main prints `LOCKED  ·  NEEDS THIS ZONE'S KEY` (`FrontRoomsMapWorld.cs:2537`), and the L3·D state has two lines. 叙事 signs 平面视觉's floor-key wording (asked to Red in its chat). It names the thing to fetch in the same words as the HUD panel and the placard |
| Locked EXIT, crosshair, with the key | `E · UNLOCK` / `STAIR KEY` | 平面视觉's wording, signed. Separators are its call |
| Broken door (C11) | `BROKEN` when aimed; `WON'T SHUT` as line 2 on a shut try | 平面视觉's draft, signed |
| EXAMINE verbs | `READ PLAN` / `READ SCREEN` / `READ SIGN` / `EXAMINE KEY` / `TAKE KEY` | 平面视觉's draft, signed. `READ SCREEN` only on READ terminals (C19), never on ambient desk monitors, which carry no text (AT-T19). `READ SIGN` covers the stair plates |
| Phone control legend and stick label (X2, 交互设计; phones only) | `SLOW · UNHEARD` | Signed 2026-10-22. True on phones: the stick's inner-zone creep (1.44 m/s) makes no noise the hunter hears, while a walk is heard (radius 9) and a run more (26) (`FrontRooms3DGame.Mobile.cs` HandheldStepNoise; RELAY :591–592). It names what the hunter perceives about a gait. It is not live threat information, and it doesn't say you can't be seen. It must stay true for any hunter version that ships; reword if the creep ever makes noise |

## 5. The stairwell (everything it shows)

The C22 stairwell is 1 × 2 cells, the exit door above the far door (`stairwell/10_directions.md` §1).

**The stair is a safe zone** (Red, 2026-10-22 15:5x, 游戏视觉 chat, relayed: "选择这套，同时这种楼梯间其实是安全区" / "也就是说怪物是进不来的"). The fiction already says so, with no change to EGRESS or the warden line:
- the panel's alarm zones are floors, and a stair enclosure belongs to none of them, so its responder never enters (§1);
- by code the rated enclosure is the protected exit. ADAAG 1991 §4.3.11.1(5) lets "a portion of a stairway landing within an exit enclosure" serve as an **area of rescue assistance**, behind one-hour fire doors [read: ada.gov, 1991 Standards];
- `ON ALARM, A FLOOR WARDEN CHECKS EVERY ROOM.` holds, because the stair is not a room of the floor.

The sign below says "safe" in the building's own voice.

| Where | String | Source of the form |
|---|---|---|
| ~~Exit landing, stair face of the EXIT door, floor plate at 5 ft~~ | ~~`LEVEL 0`~~ | **Moved into the wall ID sign** (游戏视觉, after Red's 13:00 answer). The level is still read on the stair side, as NYC LL5 C26-608.3 asks |
| Exit landing, stair face of the EXIT door (it locks behind you): the re-entry plate | `NO REENTRY` / `NEAREST REENTRY ON LEVEL B1` | NYC LL5 1973 §C26-608.5(b)(2) wording form, "NO REENTRY, NEAREST REENTRY ON THE ___ AND ___ FLOORS", read by 平面视觉 from the law's p. 9 (placard `SOURCES.md` §7.3 M5). Only the floor below exists, so one level is named. Spelled without a hyphen, as the code does |
| Stencilled numeral on the block wall, **hinge side** (the latch side holds the wall ID sign and the standpipe; lookdev and `stairwell/10_directions.md` §1.4) | `0` (exit landing), `B1` (far landing) | trade practice, 12–24 in [ESTIMATE, `stairwell/01_research.md`] |
| Stair ID sign **on the wall** at each landing, about 5 ft | `STAIR A` / `LEVEL 0` (or `LEVEL B1`) / `LEVELS 0 TO B1` / `NO ROOF ACCESS` | UBC 1991 §3306(p): stair ID, roof access, floor level, upper and lower terminus [read]. Layout per UBC Std 33-2 is unread, so it is 平面视觉's |
| ~~Gates across the up and down flights~~ | ~~`NO ACCESS ABOVE THIS LEVEL`~~ / ~~`NO ACCESS BELOW THIS LEVEL`~~ | **Dropped** (2026-10-22): a two-level stair has no flights beyond its landings, so no gates |
| Far door, stair face | `REENTRY ON THIS FLOOR` | LL5 §C26-608.5(b)(1) wording [read by 平面视觉, M5]. It replaces v1's UBC `ACCESS ONTO FLOOR THIS LEVEL` so both re-entry plates use one code's family |
| Placard copy by the far door | sheet A with `LEVEL B1` | §3 |
| **Exit landing, stair-side wall beside the door** (first thing seen after the door shuts behind you) | `AREA OF RESCUE ASSISTANCE` + the International Symbol of Accessibility | ADAAG 1991 §4.3.11.5 [read, ada.gov]: the sign states those words and shows the symbol. It is lit like an exit sign (steady, never flashing). Lettered by 平面视觉 · 信息设计, mounted by 游戏视觉. **No call box and no posted "wait here" instructions:** help never comes, and the player must go on down, so no string may advise waiting |
| **Far door, floor side** (what you see when you turn round on B1) | `NOT AN EXIT` | OSHA's "Not an Exit" sign, required since 1974 for a door likely to be mistaken for an exit (`wallpaper_motion/30_narrative_phosphor.md` §3.5, ledger §9). Replaces a dead EXIT sign (Red agreed, 13:00). True: up this stair the way is `NO REENTRY`. Not `STAIR A`, which would give B1 two doors called STAIR A. Set by 平面视觉 · 信息设计: one line, 1 in, black on white, never red, so it never reads as an EXIT beacon (board Figma 3545:7368) |

On the next floor every token moves down one: `LEVEL B1`, `NEAREST REENTRY ON LEVEL B2`, `LEVELS B1 TO B2`.

**The quiet wrongness, never stated:**
- every stair is STAIR A and serves one floor;
- the stair has only its two landings: no flight up to a lobby, none down;
- the far door has the grade-level hardware and is not the street;
- the door you came through says `NOT AN EXIT`, and it is right.
- An attentive player sees it; nobody is told.

**Do not add:**
- an EXIT sign inside the stair;
- photoluminescent strips or a running man (post-1990);
- arrows (a sign never knows the route);
- any text about the thing.

**Optional sound line** (a story beat for 声音设计 if it wants one):
- one distant door slam far **below**, at most once per stairwell, never above you and never steps (steps belong to the hunter's family);
- it is all that is left of "the other occupant": Red approved it as Q7's flavour on 2026-10-22 ("全部按推荐"; the screens' `OCCUPANT LOAD 2` frame is gone with the text-free desk screens);
- not directional, not a cue (stairwell research §"distant slams").
- **Taken by 声音设计** (2026-10-22, after C22): it plays only during the walkable descent while the Relay is parked (Dormant), from the library's CC0 stairwell-door slam rendered far away. **Caption: `[DISTANT DOOR SLAMS]`**. It follows the game's caption form (`[DOOR BREAKS]`, `[GLASS SHATTERS]`) but uses a different verb from the Relay's `[DOOR BREAKS]`, and it gives no direction.

## 6. CAUGHT (D13)

| Item | String | Status |
|---|---|---|
| Title | `CAUGHT` | ship |
| Subtitle (Red, Q3) | `OCCUPANT ACCOUNTED FOR` | **ship it**, small, under `CAUGHT`, on desktop and on the touch card |
| Stats line, desktop | `LEVEL B2  /  123 S  /  5 ZONES  /  1 KEY  /  0 DOORS BROKEN` | ship (floor scheme per §2). **No TIER** (v1.1 fix, 12:2x): C04 (Red, 2026-10-21) took TIER off the HUD and CAUGHT; it lives only in the off-by-default readout. v1 copied today's code by mistake (平面视觉's catch). Face: Plex Mono 20, not Bayon (平面视觉, `FIX_CARD_VL217.md`; Bayon prints `1` as `I`) |

- The floor is the first item, the same token the stairs and the placard use.
- Number agreement is fixed (today's line prints `1 KEYS`). `1 ZONE`, `1 KEY` and `1 DOOR BROKEN` in the singular.
- **Touch caught card stats** (交互设计's view; signed 12:2x): `SURVIVED` / `ZONES CROSSED` / **`KEYS TAKEN`** / `DOORS IT BROKE`. `KEYS TAKEN` replaces `TIER REACHED` (C04 keeps TIER off CAUGHT). It mirrors the desktop line's `1 KEY` and the verb of `E · TAKE KEY`, and counts `keysTaken`. Labels stay plural whatever the count (column heads, not sentences). The sentence reads `Caught in Level 0 / Front Rooms.` (proper name kept) or `Caught in Level B1 / The maze.`
- The touch caught card belongs to 交互设计. Its `Where` sentence should read `Level B2 / The maze` from the same `FloorLabel()`.
- **HUD room text:** `LEVEL B1 / THE MAZE`, `LEVEL B1 / LOW ROOMS`, `LEVEL B1 / TALL HALLS` (today `LEVEL 0 / …`).
- **Start rooms: `LEVEL 0 / FRONT ROOMS`** (Red, 2026-10-22; was `LEVEL 0 / THE LOBBY`, the Backrooms Fandom wiki's name for Level 0, `station/02_refs.md` §3.11). Today's code: `FrontRooms3DGame.cs:2487` (desktop HUD, 关卡设计) and `FrontRooms3DGame.Mobile.cs:155` (`MobileZoneName()`, the touch card; a shared hunk, so through 关卡设计 with 交互设计). The card's `SentenceCase()` would print `Level 0 / Front rooms`; it should read `Level 0 / Front Rooms`, because Front Rooms is the store's and the game's name.

## 7. Fit check against the standing rules

| Rule | Check |
|---|---|
| Never RUN/HIDE as advice; never "relay" | Pass. `DO NOT RUN` is negated; no "relay" anywhere |
| Dark is never cover | Pass. Nothing mentions the dark. The warden line says every room is checked, so hiding is no plan either |
| No live threat info / never a radar | Pass. Line 3 says near, not where; signs are static per floor |
| One name per thing | STAIR A (plan, tag, door, stair sign); LEVEL token (placard, plates, HUD, CAUGHT); SECURITY DESK (plan, legend, line 4); REENTRY, one spelling (placard and plates) |
| 1990 era | UBC 1991 and LL5 forms; B levels; no post-1990 signs |
| No real brands | Pass |

## 8. Questions for Red: all answered 2026-10-22 12:15 ("全部按推荐（Q1–Q7、照片下载、LOBBY 改名）", 叙事设计 chat, account B)

1. **Q1, floor numbering:** basements `B1, B2…` (recommended) or `1, 2…`.
2. **Q2, a role name for the hunter:**
   - none (today);
   - `FLOOR WARDEN`, recommended, as one fine-print line only. In real fire plans wardens check every room and account for everyone (OSHA Subpart E appendix; NYC high-rise warden teams, sources below).
   - Never the body, never drawn.
3. **Q3, the CAUGHT subtitle `OCCUPANT ACCOUNTED FOR`:** recommended yes, under `CAUGHT` (it pairs with Q2's warden).
4. The other open items of this line, answered in the same words: glow **B** (walls don't glow; in a blackout only the placard and the EXIT sign glow) **re-confirmed by Red at 12:34** ("Q4 保持 B：墙不发光") after a corrected re-ask: glow A no longer hides the FLOW arrows on the A+M4+V1 geometry (wallpaper line, VL350), so B is kept for cost and simplicity, not for that reason. Then: the scratch-throughs **cut**, **no** T4 figure layer in v1, forged FLOW **retired** (C29; the arrows always tell the truth). Recorded in the handoff §6 and in `wallpaper_motion/30_narrative_phosphor.md`.
5. Also answered: the station media Set 1 download, and the start-room rename to `LEVEL 0 / FRONT ROOMS` (§6).

## 9. The first room (Red, 2026-10-22 15:2x, 叙事设计 chat, account A: "按推荐，第一个房间选方案 1")

Red's 15:14 direction, given in the 游戏视觉 chat, was: the reception or guard room gets its own walls, distinct from Level 0, and is the **first room the player enters**. Red then picked **option 1** of the tutorial note (`station/NOTE_1022_first_room_tutorial.md` §4):
- Floor 1's station is the first room: desk, TV, placard and key;
- **the locked EXIT is in view** through a doorway, about 3–6 cells away;
- the first send waits until the player has left the room, from the far side, with the 150 s fallback kept;
- Floor 2+ stations stay rooms you find by the TV's sound.

The fiction is in `station/NOTE_1022_first_room_narrative.md` §1–§2: the front rooms against the back rooms; the building's front desk at the main entrance; the player as an after-hours visitor.

| Where | String | Note |
|---|---|---|
| HUD, start rooms **and the station room on Floor 1** | `LEVEL 0 / FRONT ROOMS` | Unchanged (Red 12:15). The reception is the last front room, so its HUD line is the same. The back rooms beyond read `LEVEL 0 / THE MAZE` (etc.) |
| **The start door, station face** (it is the building's main entrance, seen from inside: no extra door; 关卡设计 10-22) | `THIS DOOR TO REMAIN UNLOCKED DURING BUSINESS HOURS` | UBC 1991 §3304(c) exception 1 is written for exactly this door, a Group B building's main exit (`station/01_research.md` §4). **Moved here from the stair door.** It explains why the door you came in by locks behind you tonight |
| Desk front lettering (optional) | `SECURITY` | One name per thing (the placard says SECURITY DESK). Never `LOBBY`, never `STATION` |
| Desk tent card | `PLEASE SIGN IN` | As `station/01` §6 |
| Visitor register | `DATE  NAME  COMPANY  VISITING  TIME IN  TIME OUT` | The last ruled line is blank. EXAMINE only, no text entry |
| Key board plate | `STAIR KEY` / `SILENT ALARM: REMOVAL SIGNALS PANEL` | Unchanged. On Floor 1 it is **the last thing read before the first decision**. The placard is read first, in the first sightline (tutorial `10_proposal.md` §3, rows 2 and 4) |
| Placard (sheet A), Floor 1 | `YOU ARE HERE` at the desk; the legend's `SECURITY DESK (KEY)` at the same spot; a short route to `STAIR A` · `EXIT` | The four lines stay, including line 4 `KEY AT SECURITY DESK.` (a building rule sheet holds on every floor). 平面视觉 redraws the route and moves the mount into the room, by the desk |
| Start door (front rooms → station) | `NO REENTRY` (placard plan) | Unchanged. The fire doors of UBC §702(c)2 are the map's doors further into the back rooms (no strings) |

**Unchanged:** the floor tokens; `STAIR A`; the stair signs (§5); CAUGHT (§6); WTCZ-TV 61; the live-clock date rules (`station/01` §5); the IP lines (no showroom, never LOBBY or STATION).

## Sources (new for this doc; the stair and sign facts are `research/stairwell/SOURCES.md` S1, S4)

- **OSHA, 29 CFR 1910 Subpart E, Appendix** (nonmandatory):
  - one warden for each twenty employees;
  - wardens check rooms and enclosed spaces before leaving;
  - everyone is accounted for after evacuation.
  - https://www.osha.gov/laws-regs/regulations/standardnumber/1910/1910SubpartEApp ; mirror https://www.law.cornell.edu/cfr/text/29/appendix-to_subpart_E_of_part_1910
  - [search] 2026-10-22. The text read is the current one; that it existed before 1990 is **not verified**.
- **Fire Engineering, "WTC: Actions of fire safety directors and fire warden teams":** per-floor teams of fire wardens, deputy wardens and searchers in a Local Law 5 high-rise. https://www.fireengineering.com/?p=229344 [search] 2026-10-22. The article's date is not checked. The 1973 law text (https://www.nyc.gov/assets/buildings/local_laws/ll_0573.pdf) names a fire safety director and evacuation supervisors; its warden wording is unread (a PDF download needs Red's OK).
- Backrooms wiki level numbering, the IP-distance reason in §2: `stairwell/SOURCES.md` S16, S17.
