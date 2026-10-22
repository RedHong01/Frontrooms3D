# 40 · Floors, the EXIT, the stair and the tutorial: fiction and strings (叙事设计, 2026-10-22)

**Status: v1.**
- **Signed by 叙事:** every string in §3–§6 that is not marked RED. Peers can build on them.
- **RED:** strings that wait on a decision Red makes in the 叙事设计 chat (§8). Until then they ship as written in the "until Red" column.
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
  - Its far landing has the code's basement barrier gate (UBC 1991 §3309(e)). By code that gate marks the grade-level exit, so the stair "believes" its bottom door is the street. It opens onto the next floor.
  - The hardware is real. The building's use of it is wrong.
- **Why the stair door is locked:**
  - 1990 buildings locked stair doors for security. The plan says it plainly, `STAIR DOORS ARE LOCKED.` / `KEY AT SECURITY DESK.`, so the lock is not a trick.
  - The guard is gone, but the desk set still plays WXRM-TV 61 (`STAY TUNED.`).
- **Taking the key is an alarm condition.**
  - The key rack is on the panel, like any device. Lifting the key trips it.
  - The panel drops the desk's non-emergency circuit, so the TV dies, and sends its responder to the desk, never to you (`relay_pursuit/30_narrative.md` §1; tutorial `10_proposal.md` §4.1).
- **The stairwell is safe because it is in no alarm zone.**
  - The panel's zones are floors, and a stair enclosure belongs to none of them.
  - The alarm itself does not reset: it is a general alarm, and its level carries down with you. That is why the tier carries over (C22 D13).
- **There is no ground floor and no roof.**
  - LEVEL 0 is the highest floor you can reach. Below it are basements, B1, B2, … (RED, Q1).
  - The stair ID signs say `NO ROOF ACCESS`.
  - The run ends only when you are caught (C22), and the building's last word is about you, not the thing (Q3).

## 2. Floor numbering (RED, Q1)

| Floor of the run | Recommended (B) | Alternative (count up) |
|---|---|---|
| 1 (start) | `LEVEL 0` | `LEVEL 0` |
| 2 | `LEVEL B1` | `LEVEL 1` |
| 3 | `LEVEL B2` | `LEVEL 2` |
| n | `LEVEL B(n−1)` | `LEVEL n−1` |

- **Why B:**
  - It is building-true for going down: 1990 US elevator panels and stair signs used B, B1, B2 [memory, common practice].
  - It still counts up with depth, so it reads as a score.
  - It stays clear of the Backrooms wiki's numbered level ladder: "Level 1" there is a different world (`stairwell/SOURCES.md` S16, S17). A deep `LEVEL B9` also feels wrong in a good way.
- **Until Red answers, use B.** Every string below is written with B. Count-up only swaps the token.
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
- `LEGEND`, `YOU ARE HERE`, `STAIR (EXIT)` / `ONE FLOOR DOWN`, `NO RE-ENTRY` (the start door), `EXIT`, `STAIR A`;
- A: `TYPICAL FLOOR`, `NOT TO SCALE`, `THIS FLOOR MAY DIFFER FROM PLAN` (separators are 平面视觉's);
- B: `MATCH LINE — SEE SHEET A-3`, `NOT TO SCALE`, `BEYOND MATCH LINE: NOT TO SCALE`;
- C: `STAIR A · EXIT`, `SHEET C-3 BEYOND THIS EDGE`, `LOCKED DOOR`;
- Phase 0's line 4: `4 EXIT BY STAIR,` / `ONE FLOOR DOWN.`

**Title block** (A.12 form; only the level changes per floor):
- `LEVEL 0 | SHEET A-2 OF 4 | PRINTED 03/90` on Floor 1;
- `LEVEL B1 | SHEET A-2 OF 4 | PRINTED 03/90` on the copy in the stair down to B1. That copy hangs by the far door and shows the floor you are about to enter.

**Optional fine-print line (RED, Q2):** `ON ALARM, A FLOOR WARDEN CHECKS EVERY ROOM.`
- It is not a numbered line, so the four-line limit stands. It sits near the red block at the zoom-step size; placement is 平面视觉's.
- Ship it only if Red picks the role name.

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
| EXIT sign (C23) | `EXIT` | 游戏视觉's lit kit, unchanged |
| Stair letter, maze side of the EXIT door (optional plate) | `STAIR A` | NYC LL5 C26-608.4: stair letters on the occupancy side [read, `stairwell/SOURCES.md` S4]. It matches the plan and the tag |
| Key tag (C18 close-up) | `STAIR A` | Matches the plan, the door and the stair sign |
| HUD key panel, main's A panel until KeyHud B | `STAIR KEY` | 平面视觉's interim, signed: no digits in Bayon (`LEVEL 0 KEY` reads `LEVEL O KEY`) |
| HUD key tag number, KeyHud B | the floor token: `00`, `B1`, `B2`, … | 平面视觉's G2. Two characters up to B9; `B10` and deeper are its layout call |
| Locked EXIT, crosshair | (no string: padlock and rattle) | the live grammar |

## 5. The stairwell (everything it shows)

The C22 stairwell is 1 × 2 cells, the exit door above the far door (`stairwell/10_directions.md` §1).

| Where | String | Source of the form |
|---|---|---|
| Exit landing, stair face of the EXIT door (it locks behind you), floor plate at 5 ft | `LEVEL 0` | NYC LL5 C26-608.3, floor numbers on the stair side [read] |
| Same door, re-entry plate below it | `NO RE-ENTRY` / `RE-ENTRY AT LEVEL B1` | LL5 C26-608.5: say whether re-entry is provided and on which floor [read]. `NO RE-ENTRY` is trade wording [ESTIMATE] |
| Stencilled numeral on the block wall, latch side | `0` (exit landing), `B1` (far landing) | trade practice, 12–24 in [ESTIMATE, `stairwell/01_research.md`] |
| Stair ID sign at each landing, about 5 ft | `STAIR A` / `LEVEL 0` (or `LEVEL B1`) / `LEVELS 0 TO B1` / `NO ROOF ACCESS` | UBC 1991 §3306(p): stair ID, roof access, floor level, upper and lower terminus [read]. Layout per UBC Std 33-2 is unread, so it is 平面视觉's |
| Gate across the up flight (exit landing) | `NO ACCESS ABOVE THIS LEVEL` | building voice [ESTIMATE wording] |
| Gate across the down flight (far landing; UBC 3309(e) barrier) | `NO ACCESS BELOW THIS LEVEL` | building voice [ESTIMATE wording] |
| Far door, stair face | `ACCESS ONTO FLOOR THIS LEVEL` | UBC 1991 App. Ch. 1 §125 item 11 [read] |
| Placard copy by the far door | sheet A with `LEVEL B1` | §3 |

On the next floor every token moves down one: `LEVEL B1`, `RE-ENTRY AT LEVEL B2`, `LEVELS B1 TO B2`.

**The quiet wrongness, never stated:**
- every stair is STAIR A and serves one floor;
- the well looks endless (the periodic shaft), but both gates close it;
- the far door has the grade-level hardware and is not the street.
- An attentive player sees it; nobody is told.

**Do not add:**
- an EXIT sign inside the stair;
- photoluminescent strips or a running man (post-1990);
- arrows (a sign never knows the route);
- any text about the thing.

**Optional sound line** (a story beat for 声音设计 if it wants one):
- one distant door slam far **below**, at most once per stairwell, never above you and never steps (steps belong to the hunter's family);
- the "other occupant" (`OCCUPANT LOAD 2`) is a floor ahead of you;
- not directional, not a cue (stairwell research §"distant slams").

## 6. CAUGHT (D13)

| Item | String | Until Red |
|---|---|---|
| Title | `CAUGHT` | stays |
| Subtitle (RED, Q3) | `OCCUPANT ACCOUNTED FOR` | not shown |
| Stats line, desktop | `LEVEL B2  /  123 S  /  TIER 2  /  5 ZONES  /  1 KEY  /  0 DOORS BROKEN` | ship it (floor scheme per §2) |

- The floor is the first item, the same token the stairs and the placard use.
- Number agreement is fixed (today's line prints `1 KEYS`). `1 ZONE`, `1 KEY` and `1 DOOR BROKEN` in the singular.
- The touch caught card belongs to 交互设计. Its `Where` sentence should read `Level B2 / The maze` from the same `FloorLabel()`.
- **HUD room text:** `LEVEL B1 / THE MAZE`, `LEVEL B1 / LOW ROOMS`, `LEVEL B1 / TALL HALLS` (today `LEVEL 0 / …`). The start rooms keep `LEVEL 0 / THE LOBBY`.

## 7. Fit check against the standing rules

| Rule | Check |
|---|---|
| Never RUN/HIDE as advice; never "relay" | Pass. `DO NOT RUN` is negated; no "relay" anywhere |
| Dark is never cover | Pass. Nothing mentions the dark. The warden line, if used, says every room is checked |
| No live threat info / never a radar | Pass. Line 3 says near, not where; signs are static per floor |
| One name per thing | STAIR A (plan, tag, door, stair sign); LEVEL token (placard, plates, HUD, CAUGHT); SECURITY DESK (plan, legend, line 4) |
| 1990 era | UBC 1991 and LL5 forms; B levels; no post-1990 signs |
| No real brands | Pass |

## 8. Questions for Red (asked in the 叙事设计 chat, 2026-10-22)

1. **Q1, floor numbering:** basements `B1, B2…` (recommended) or `1, 2…`.
2. **Q2, a role name for the hunter:**
   - none (today);
   - `FLOOR WARDEN`, recommended, as one fine-print line only. In real fire plans wardens check every room and account for everyone (OSHA Subpart E appendix; NYC high-rise warden teams, sources below).
   - Never the body, never drawn.
3. **Q3, the CAUGHT subtitle `OCCUPANT ACCOUNTED FOR`:** recommended yes, under `CAUGHT` (it pairs with Q2's warden).
4. The other open items of this line (glow A/B, scratches, the T4 figure layer, forged FLOW vs A20) are in the same message. They are recorded in the handoff (`Takeover_2026-10-18/handoff/narrative.md`) once answered.

## Sources (new for this doc; the stair and sign facts are `research/stairwell/SOURCES.md` S1, S4)

- **OSHA, 29 CFR 1910 Subpart E, Appendix** (nonmandatory):
  - one warden for each twenty employees;
  - wardens check rooms and enclosed spaces before leaving;
  - everyone is accounted for after evacuation.
  - https://www.osha.gov/laws-regs/regulations/standardnumber/1910/1910SubpartEApp ; mirror https://www.law.cornell.edu/cfr/text/29/appendix-to_subpart_E_of_part_1910
  - [search] 2026-10-22. The text read is the current one; that it existed before 1990 is **not verified**.
- **Fire Engineering, "WTC: Actions of fire safety directors and fire warden teams":** per-floor teams of fire wardens, deputy wardens and searchers in a Local Law 5 high-rise. https://www.fireengineering.com/?p=229344 [search] 2026-10-22. The article's date is not checked. The 1973 law text (https://www.nyc.gov/assets/buildings/local_laws/ll_0573.pdf) names a fire safety director and evacuation supervisors; its warden wording is unread (a PDF download needs Red's OK).
- Backrooms wiki level numbering, the IP-distance reason in §2: `stairwell/SOURCES.md` S16, S17.
