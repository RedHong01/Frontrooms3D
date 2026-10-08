# On-screen strings: the station, the store and the slates (narrative)

Status: strings v1, 2026-10-07. Written by the narrative chat for 平面视觉's screen slice (`Documentation/SCREENS_VISUAL_SYSTEM.md`).
- **Ownership:** narrative owns every word below; 平面视觉 owns type, layout, CG style and separators. Where a line shows two spaces, that is a break or separator for 平面视觉 to style. I don't specify `·`.
- **Limits:** every line is checked against the 4:3 title-safe limits (headline ≤ 14, support ≤ 28, CG tag ≤ 32, emergency slate ≤ 20 × 3 lines).
- **Era lock:** 1990 (≤ 1993).

## 1. The station

| Item | String | Note |
|---|---|---|
| Call sign | `WXRM` | W because the founding photo's region (Wisconsin) is east of the Mississippi. A web search found no TV or radio station with these letters (WLVL and WTSH are real radio stations and were rejected). A check in the FCC licensing database (LMS) is still advised |
| Channel | `61` | UHF, the band for small independents in 1990 |
| ID form | `WXRM-TV 61` | |
| Market line | `TRI-COUNTY` | Generic period phrase; no real city |
| Slogan | `STAY TUNED.` | Ordinary on air, but it lands differently in a building you can't leave |

## 2. The store

**`FRONT ROOMS FURNITURE`** is two words, the way a 1990 store would set it. The game's stylized name `FrontRooms` stays the title only.
- **Why the game's name works as the store:**
  - The Backrooms photo was the back room of a furniture store.
  - Our title corridor is that store's front rooms, the showroom. The maze is the stock behind it.
  - The ad tells the player where they are without saying so.
- **Slogan:** `ALWAYS MORE IN THE BACK`. It is a warehouse-sale line in 1990, and in the game it is the truth.
- **Risk:** "Front Room" is a generic phrase that small real shops may use. No national 1990 chain had it, and it is our own title.
- **Not used:** "EVERYTHING MUST GO", because it is Kane Pixels' episode 25 title (IP distance).

## 3. Ad copy: four cards (v0 structure)

| Card | Headline (≤ 14) | Support (≤ 28) | CG tag (≤ 32) |
|---|---|---|---|
| 1. Store + sale hook | `FRONT ROOMS` | `BACK ROOM CLEARANCE` | — |
| 2. Product + price | `3-PC SOFA SET` | `NOW $499  WAS $899` / `90 DAYS SAME AS CASH` | — |
| 3. Call to action + phone | `CALL 555-0147` | `OPEN 9 TO 9  7 DAYS` / `FREE DELIVERY` | — |
| 4. Sign-off + address tag | `FRONT ROOMS` | `ALWAYS MORE IN THE BACK` | `RT. 41 AT MAIN ST.  555-0147` |

- **Prices:** the dollars are superscripted by 平面视觉's period rule. `$499` / `$899` can be set as `$499⁰⁰` if the CG style wants cents.
- **Phone:** 555-0147 is in the 555-0100–0199 block reserved for fiction. It is a 7-digit local number, as 1990 local spots used; no area code.
- **Address:** a generic route and street, with no real address. The founding photo's real address is deliberately avoided.
- **Alternative product for card 2:** `QUEEN ANNE SET` (14). It matches the prop kit's Queen Anne armchair.
- "90 DAYS SAME AS CASH" and "NO MONEY DOWN" (spare support line) are generic period financing phrases, not brands.

## 4. Station states

| State | Lines | Limit check |
|---|---|---|
| Station ID | `WXRM-TV 61  TRI-COUNTY` | CG 22/32 |
| Station ID, with slogan | `WXRM 61` / `STAY TUNED.` | headline 7/14, CG 11/32 |
| Stand-by slate | `PLEASE STAND BY` / `TECHNICAL DIFFICULTIES` | support 15 and 22/28 |
| Sign-off | `THIS CONCLUDES OUR BROADCAST DAY` | CG 32/32 |
| Sign-off, short | `WXRM 61 SIGNS OFF AT 2 AM` | CG 25/32 |

These are generic broadcast phrases; no wording is taken from the research clips.

## 5. The emergency slate (sets cut to it when the chase wave passes)

**Recommended:**

```
ALARM IN PROGRESS
CLOSE DOORS
BEHIND YOU
```

- **Lengths:** 17 / 11 / 10 characters.
- **It reads as plain life-safety advice:** shut the door you just came through. That is true in play (a shut door costs the Relay 2.5 s).
- **The last line is a double meaning, and the only one on the slate.** It doesn't say where the Relay is, so it is no radar.
- **It is EGRESS language:** the building's alarm takes over its own sets. It is not the Emergency Broadcast System, and no EBS wording or tones are used.
- **It is consistent with the placard:** `CLOSE DOORS BEHIND YOU`, from the A.12 footer, which was verified as standard US fire-safety guidance in `relay_pursuit/30_narrative.md` §7.

**Alternatives** (all ≤ 20):
- `ATTENTION OCCUPANTS` / `LEAVE THIS ROOM` / `CLOSE DOORS BEHIND`. This one is more instructive, without the double meaning.
- `ALARM IN YOUR AREA` / `THIS IS NOT A DRILL` / `CLOSE DOORS`.

**Rules:**
- Never `RUN` or `HIDE`.
- Nothing that implies the dark hides you (LD R13).
- No arrows or directions, because a TV doesn't know the route.

## 6. Office monitors (optional, useful)

The office CRTs can show the building's own machine voice when they aren't on WXRM. These lines are all static and deterministic, and none of them gives live threat information.

| Screen | Lines | Why |
|---|---|---|
| DOS prompt | `C:\>_` (blinking cursor) | Generic; no real command output |
| Print queue | `PRINT QUEUE  1 JOB` / `PLAN SHEET A-3 OF 4` / `PRINTING` | The same drawing set as the wallpaper's title block and the placard |
| Print queue, from T2 | `PRINTING SHEET A-1114 OF 4` | The Exit 8-style anomaly, matching the T2 stamp |
| Idle screen | `SYSTEM IDLE` / `OCCUPANT LOAD 1` | The floating text of a period screen blanker (no brand); `OCCUPANT LOAD 2` from T4, matching the stamp |
| Alarm log | `ALARM LOG 03/90` / `ZONE 04  TROUBLE  ACK` | The panel's history, static. Never live, never the player's zone |

These depend on the tier only if the screen generator can read the tier. Otherwise use the T0 lines.

## 7. What these strings must never do
- Name a real brand, station, person, address or working phone number.
- Copy EBS, NWS or research-clip wording.
- Give the Relay's position or distance (all Relay state text is behind an assist).
- Date anything after 1990.
