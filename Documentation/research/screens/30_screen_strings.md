# On-screen strings: the station, the store and the slates (narrative)

**Status: strings v2.1, 2026-10-22** (v1 2026-10-07). Written by 叙事设计 for 平面视觉 · 广告's broadcast layer (`AD_TYPE_SYSTEM.md` §3, `SCREENS_VISUAL_SYSTEM.md`) and for the still desk screens (A16).
- **Ownership:** narrative owns every word below. 平面视觉 owns type, layout, CG style and separators: · 广告 for the station, · 信息设计 for office screens.
- **Limits:** v3's CG slots, as measured by 平面视觉 · 广告 (caps; spaces count; inside RP 27.3 title safe, x 66–574 at 640 × 480):

| CG size | Slots | Max characters |
|---|---|---|
| CG 48 | time on the bars, phone | 12 |
| CG 36 | slate headline, store name | 15 |
| CG 30 | call line, market line, date, notice lines | 18 |
| CG 24 | address, town, hours, notice body; multi-line pages up to 8 lines | 22 |
| CG 18 | legal ID, small print | 30 |

- Every string below names its slot and its length.
- **Era lock:** 1990 (≤ 1993).

**What changed:**
- **v2.1:**
  - `WTCZ` is chosen, from 广告's FCC check (`AD_TYPE_SYSTEM.md` §7.1);
  - every string is refit to the v3 slots;
  - §6 follows the terminal spec's glance rule (`computer_ui/20_design.md` §3.5, AT-T19).
- **v2:**
  - WXRM is retired: it is a real, licensed station (WXRM-LP, Cape May Court House NJ, FCC facility 197239, licensed 2017). v1's "no station found" came from a web search, not an FCC check, and was wrong.
  - New: the city of licence, the legal ID, the sign-off sequence and the dealer-tag town.

## 1. The station

| Item | String | Slot, length | Note |
|---|---|---|---|
| Call sign | `WTCZ` | — | "TC" for Tri-County. Clean in FCC prefix queries (FM/LPFM/translators, AM, TV/Class A/LPTV) and in a web search, 2026-10-22 (`AD_TYPE_SYSTEM.md` §7.1). Current records only; 1990 history not checked. Second choice: `WTCU`. W = east of the Mississippi |
| Station logotype | `WTCZ-TV` | — | 7 characters, as before |
| Channel | `61` | — | UHF, the band for small independents in 1990 |
| City of licence | `FAIRVIEW` | — | Never with a state. The USGS counts about 288 US populated places named Fairview, more than any other name, so it points at no one town |
| Legal ID | `WTCZ-TV 61 FAIRVIEW` | CG 24 (19/22) or CG 18; 广告's call | 47 CFR 73.1201 (1983–91): the call letters immediately followed by the city of licence; only the channel number or licensee name may sit between them. Its own line |
| Market line | `TRI-COUNTY` | CG 30, 10/18 | Always outside the legal ID |
| Slogan | `STAY TUNED.` | CG 30, 11/18 | Ordinary on air, but it lands differently in a building you can't leave. Outside the legal ID |

**In the game:**
- The shipped Ad_02 station card still says WXRM.
- It changes only with Red's OK in 平面视觉 · 广告's chat, together with Ad_03.
- No new piece uses WXRM.

## 2. The store

**`FRONT ROOMS FURNITURE`** is two words, the way a 1990 store would set it. The game's stylized name `FrontRooms` stays the title only.
- **Why the game's name works as the store:**
  - The Backrooms photo was the back room of a furniture store.
  - Our title corridor is that store's front rooms, the showroom. The maze is the stock behind it.
  - The ad tells the player where they are without saying so.
- **Slogan:** `ALWAYS MORE IN THE BACK`. It is a warehouse-sale line in 1990, and in the game it is the truth.
  - At 23 characters it is over the CG 24 limit, so set it as two lines, `ALWAYS MORE` / `IN THE BACK` (11 / 11), or at CG 18.
- **Town:** `FAIRVIEW`, the station's city, like any local dealer.
- **Risk:** "Front Room" is a generic phrase that small real shops may use. No national 1990 chain had it, and it is our own title.
- **Not used:** "EVERYTHING MUST GO", because it is Kane Pixels' episode 25 title (IP distance).
- **A24 option A/B/C is Red's** (`CAMPAIGN_PLAN.md` §8.1). These strings stand unless Red picks B (a non-furniture advertiser).

## 3. Ad copy: four cards

| Card | Headline (CG 36, ≤ 15) | Lines (CG 24, ≤ 22) | Notes |
|---|---|---|---|
| 1. Store + sale hook | `FRONT ROOMS` (11) | `BACK ROOM CLEARANCE` (19) | — |
| 2. Product + price | `3-PC SOFA SET` (13) | `NOW $499  WAS $899` (18) / `90 DAYS SAME AS CASH` (20) | Alternative headline: `QUEEN ANNE SET` (14), matching the prop kit's Queen Anne armchair |
| 3. Call to action | `CALL 555-0147` (13) | `OPEN 9 TO 9  7 DAYS` (19) / `FREE DELIVERY` (13) | — |
| 4. Sign-off + dealer tag | `FRONT ROOMS` (11) | `ALWAYS MORE` / `IN THE BACK` (11 / 11) | Tag below |

**Dealer tag** (card 4's last 3–7 s, in the 1990 order: name, address, town, phone, hours; `AD_TYPE_SYSTEM.md` §3.1):

| Slot | String | Length |
|---|---|---|
| Store name (CG 36) | `FRONT ROOMS` | 11/15 |
| Address (CG 24) | `RT. 41 AT MAIN ST.` | 18/22 |
| Town (CG 24) | `FAIRVIEW` | 8/22 |
| Phone (CG 48) | `555-0147` | 8/12 |
| Hours (CG 24) | `OPEN 9 TO 9` | 11/22 |

- **Prices:** the dollars are superscripted by 平面视觉's period rule. `$499` / `$899` can be set as `$499⁰⁰` if the CG style wants cents.
- **Phone:** 555-0147 is in the 555-0100–0199 block reserved for fiction. It is a 7-digit local number, as 1990 local spots used; no area code.
- **Address:** a generic route and street, with no real address. The founding photo's real address is deliberately avoided.
- "90 DAYS SAME AS CASH" and "NO MONEY DOWN" (spare line, 13) are generic period financing phrases, not brands.

## 4. Station states

| State | Lines (slot, length) |
|---|---|
| Station ID | `WTCZ-TV` logotype with the 61 ring; `WTCZ-TV 61 FAIRVIEW` (CG 18, 19); `TRI-COUNTY` (CG 30, 10) |
| Station ID, with slogan | `WTCZ 61` (CG 30, 7); `STAY TUNED.` (CG 30, 11) |
| Stand-by slate | `PLEASE STAND BY` (CG 36, 15); `TECHNICAL DIFFICULTIES` (CG 24, 22) |
| Sign-off, short (small print) | `WTCZ 61 SIGNS OFF AT 2 AM` (CG 18, 25) |

These are generic broadcast phrases; no wording is taken from the research clips.

### 4.1 The sign-off sequence (order from `AD_TYPE_SYSTEM.md` §3.1)

**The night:** Friday 11-30-90.
- This is the broadcast day after the memo thread's last date (D.K. 11/90).
- The station signs off at 2 AM, so the date over the bars is already Saturday.

**Thought For Today** (all four lines fit CG 30, 17/18, 广告's preferred size; the text is our own, attributed to no one):

```
THOUGHT FOR TODAY
THE STEADY WALKER
GOES FARTHER THAN
THE ONE WHO RUNS.
```

- **Lengths:** 17 / 17 / 17 / 17.
- It sounds like a proverb ("slow and steady"), and it is true in play: running is heard. It is not a cue and gives no live information.
- **Alternate:** `LEAVE EVERY ROOM` / `AS YOU FOUND IT.` (16 / 16). It reads as a courtesy line; in a building whose doors you are told to close, it lands colder.

**Sign-off notice** (CG 24, 8 lines × 22, then the legal ID on its own CG 18 line):

```
THIS CONCLUDES OUR
BROADCAST DAY.
PORTIONS OF TODAY'S
PROGRAMS WERE
MECHANICALLY
REPRODUCED.
WE RETURN AT 6 AM.
GOOD NIGHT.
WTCZ-TV 61 FAIRVIEW
```

- **Lengths:** 18 / 14 / 19 / 13 / 12 / 11 / 18 / 11, then 19/30.
- It carries the standards line ("mechanically reproduced"), the return time and the legal ID, in our own words.
- **The 8-line limit** leaves out a home-reception line. If 广告 wants it in place of `GOOD NIGHT.`, use `GOOD RECEPTION, GOOD NIGHT.`. That is 27 characters, so it must be set at CG 18.
- The anthem or scenic film that follows is 声音设计's and 广告's call, with no lyrics on screen.

**Off-air bars:**
- Date `12-01-90` (CG 30, 8).
- Time from `2:00 AM` (CG 48, 7), a running clock, the only place the time shows.
- **In-game:** the loop's clock simply loops. It is a picture, not the game's time, and no game state reads it.
- **Public campaign B3:** the clock runs to the release time, as `CAMPAIGN_PLAN.md` plans.

**Not written yet:** the facilities voicemail script (`CAMPAIGN_PLAN.md` §147). It waits on Red's campaign-execution answer.

## 5. The emergency slate (sets cut to it when the chase wave passes)

**Depends on C09** (screens join the warning language): still open, and 系统设计 recommends not in v1. The strings are refit and stay ready.

**Recommended:**

```
ALARM IN PROGRESS      (CG 30, 17/18)
CLOSE DOORS            (CG 36, 11/15)
BEHIND YOU             (CG 36, 10/15)
```

- **It reads as plain life-safety advice:** shut the door you just came through. That is true in play: a shut door adds 9 m to its path.
- **The last line is a double meaning, and the only one on the slate.** It doesn't say where the hunter is, so it is no radar.
- **It is EGRESS language:** the building's alarm takes over its own sets. It is not the Emergency Broadcast System, and no EBS/EAS wording or tones are used.
  - In any public piece it must stay clearly in-world. The FCC's emergency-alert rules restrict EAS look-alikes (`CAMPAIGN_PLAN.md` hard rules).
- **It is consistent with the placard:** line 2, `CLOSE DOORS BEHIND YOU.`

**Alternatives:**
- `ATTENTION OCCUPANTS` (CG 24, 19) / `LEAVE THIS ROOM` (CG 36, 15) / `CLOSE DOORS BEHIND` (CG 30, 18). This one is more instructive, without the double meaning.
- `ALARM IN YOUR AREA` (CG 30, 18) / `THIS IS NOT A DRILL` (CG 24, 19) / `CLOSE DOORS` (CG 36, 11).

**Rules:**
- Never `RUN` or `HIDE`.
- Nothing that implies the dark hides you (LD R13).
- No arrows or directions, because a TV doesn't know the route.

## 6. Still desk screens in the maze (A16, Red 2026-10-22 via the 教程设计 chat)

**The rule (T3 / A16):**
- Until the floor's key is taken, the security desk's TV is the only screen that moves or makes a sound.
- Desk monitors show a **still, dim office screen**, and they stay still after the take.

**The terminal spec's glance rule** (`computer_ui/20_design.md` §3.5, AT-T19):
- Ambient monitors carry text only at glance size (a cap of 86–120 px, about 6–8 characters) or none.
- They have no prompt and no verb.
- Small text would read as a broken terminal and fake a READ one.

**So, v2.1:**
- **Default:** a dark field with a still cursor, or a blank screen. 平面视觉 · 广告 has already drawn it this way. No words.
- **V4 ships no text (AT-T19).**
- **No glance word in v1.** One word at glance size on a still screen invites E and promises a terminal that isn't there.
- **Offered to the READ-terminal pool** (信息设计, C19/D5/D6): the tenant print job `FIRE DRILL NOTICE  2 PP` / `PRINTING PAGE 2 OF 2` as a LIST page (computer_ui §8.5 asks narrative for a tenant print job). If a later build wants one, the candidates are:
  - `PRINTING` (8): a stuck tenant print job;
  - `READY` (5): a generic 1980s machine prompt word, not tied to a brand.
  - Both avoid the cue and lamp words: not NORMAL, TROUBLE, ALARM, EXIT or LOCKED.

**Dropped:**
- `SYSTEM IDLE` / `OCCUPANT LOAD 1`: small text, and no pre-1991 source for a text blanker (computer_ui §3.5). `OCCUPANT LOAD` is a building-code term (UBC exits chapter), not something a PC would show.
- `PLAN SHEET A-3 OF 4`: small text, and base-building drawings don't belong on tenant PCs. A-3 is also the sheet the placard's MATCH LINE names, which the player can never find.
- `PRINTING SHEET A-1114 OF 4`: an anomaly before T4 (computer_ui §8.5).
- `ALARM LOG 03/90` / `ZONE 04  TROUBLE  ACK`: it shows a zone number before numbering is agreed, and TROUBLE is the sanctuary lamp's word (信息设计's recommendation).

**Not here:** the readable terminals' strings (program name, desk users, the memo pool, the print table) wait for C19 and D5/D6/D11 (`computer_ui/20_design.md` §17).

## 7. What these strings must never do

- Name a real brand, station, call sign, person, address or working phone number. Call signs are checked in FCC records before use, and the check is recorded (query, date, services), as in `AD_TYPE_SYSTEM.md` §7.1.
- Copy EBS, EAS, NWS or research-clip wording.
- Give the hunter's position or distance (all hunter state text is behind an assist).
- Date anything after 1990.

## Sources (v2 and v2.1)

- **47 CFR 73.1201, the legal ID rule:** via `AD_TYPE_SYSTEM.md` §3.1 and `research/ad_campaign/station_1990/NOTES.md` §1 (平面视觉 · 广告's research).
- **Call-sign checks** (WXRM, WZRM, WQRM, WJRM and WVRM taken or not clean; WTCZ and WTCU clean): `AD_TYPE_SYSTEM.md` §7.1, with FCC query URLs `https://transition.fcc.gov/fcc-bin/{fmq,amq,tvq}?call=<CALL>&list=4&size=9`, 2026-10-22.
- **Fairview as the most common US place name** (about 288 USGS populated places): https://mentalfloss.com/posts/most-common-place-name-united-states ; https://en.wikipedia.org/wiki/List_of_places_called_Fairview_in_the_United_States [search] 2026-10-22.
- **The weekday of 1990-11-30, a Friday:** computed.
- **The glance rule for ambient monitors:** `computer_ui/20_design.md` §3.5, AT-T19.
