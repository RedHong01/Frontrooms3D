# 30 · Guidance playtest plan (round 3)

Written 2026-10-30 by 教程设计, for Red to run. It answers `Documentation/Takeover_2026-10-18/reviews/TEACHER_NOTES_GAP_20261029.md`.

**Status: APPROVED by Red, 2026-10-30, 教程设计 chat (transcript 8feee395).** Asked "试玩按这个方案来吗（11-03/04，6–9 位新玩家，问卷按新版改）？", Red: "按这个做". Nothing is printed or booked yet. The snapshot (PT4) still needs its own yes from Red when it is due.

**Decided before this plan:**
- Arrows stay honest. Red, 2026-10-30, 教程设计 chat: "维持说实话" (C29 stands). So the round measures how much players trust them; it does not test a lie.
- B-floor stations are scheduled for the next batch (A49, target 11-03).

## 1. What the round must answer

The teacher's note asks to hear "how players respond to your guidance system". Five questions:

| # | Question | Why |
|---|---|---|
| Q1 | After Floor 1, can a new player say what the goal is? | Baseline: 8 of 9 could not. The teacher expects the tutorial room to do this |
| Q2 | Which cues do players actually use: the plan, the TV, the arrows, the EXIT sign? | Nobody has measured any of them on people |
| Q3 | Do the arrows read as intended, and do players trust them? | The teacher reads them as dreamlike and not fully trustworthy; our design says they never lie. We need players' own reading |
| Q4 | Do players read the warnings, and do catches feel fair? | First chases were caught 8 of 8 by bots |
| Q5 | Do they follow the TV by ear on a B floor? | The far band has never been heard in normal play |

## 2. Who, when, what

| | Plan |
|---|---|
| **Players** | 6 minimum, 9 for a straight comparison with the baseline's 9. First-time players, outside the class, none of the earlier testers |
| **When** | Tue 11-03 evening or Wed 11-04, before Thu 11-05's class |
| **Build** | A fresh snapshot of main, on Red's yes (App Store · 构建). Red's build 20261029-2032 predates A40 |
| **If B-floor stations slip** | Run on 11-04 anyway. Q1–Q4 still work on Floor 1 and the B floors; mark Q5 not measured |
| **Platform** | Mac with headphones for at least 6 players, because the arrows are full there. Up to 3 on iPhone only if a current TestFlight has the phone arrows; otherwise iPhone players mark arrow items N/E |
| **Length** | About 30 min each: 3 setup, up to 15 play, 10 form and interview |
| **Room** | Quiet. Headphones are required, because the TV and the warnings are the point |
| **Dry run** | Before the first player, Red plays 5 min on the same machine: try the locked EXIT, take the key, quit, then open the CSV and check for an `exit` / `tried locked` row. No bot has exercised that row |

## 3. Session flow

1. **Setup (3 min).** Fill in the sheet's date, session no. and platform. Read the opening lines (§4). Ask the sheet's Before-play question.
2. **Play (up to 15 min).** Think-aloud. The moderator stays silent and fills in the observation sheet (§5).
3. **Goal check.** The first time the player is in a stairwell, say: "Quick pause. What are you trying to do right now?" Write the answer word for word. If they are caught on Floor 1 first, ask it on the CAUGHT screen.
4. **Keep playing** until caught or 15 min. A second run is fine if the first ended before 5 min.
5. **Form + interview (10 min).** FR-PT 05 ratings first, then the interview questions out loud, then the two warning questions (§4).
6. **Save data** (§8) before the next player.

## 4. Moderator script

Say these as written. Never name the arrows, the TV's purpose, the key or the hunter before the form.

**Opening:**
> Thanks for trying this. It's an unfinished game, so anything confusing is the game's fault, not yours. Please say out loud what you notice and what you think you should do. I can't answer questions while you play, but I'll write them down. You can stop at any time.

**When asked for help:**
> I can't answer that one. What do you think?

**Nudge ladder.** Use a nudge only after 2 min with no progress. Write each one down: nudges count against the guidance.
1. "What are you trying to do?"
2. "What have you tried?"
3. After 2 more minutes: "Is there anything nearby you haven't looked at?"

Never point at anything, and never give a control.

**After the form, ask:**
- "What did it mean when the lights dipped?"
- "Was there anywhere you felt safe? Why?"
- "Did anything lead you somewhere? What?"
- "If you saw the crosshair turn into an eye, what did you think it meant?"

## 5. Observation sheet (one per player, one page)

Note the time from the first frame of play. Leave a row blank if it never happened.

| Moment | Time | Notes |
|---|---|---|
| Reads the plan (opens it, or stops at it for over 3 s) | | Which lines do they mention? |
| Mentions the TV or its sound | | |
| Tries the locked EXIT before the key | | |
| Takes the key | | |
| Reaction when the TV dies | | |
| Reaches the EXIT with the key; unlock worked first try? | | |
| Goal check, word for word | | |
| First comment on the wallpaper or arrows | | Their words, not ours |
| Follows an arrow on purpose | | |
| Goes against an arrow on purpose | | Why? |
| First comment on the lights dipping | | |
| First sign they know something hunts them | | |
| On a B floor: mentions hearing the TV before seeing it | | |
| Caught: any warning before it? Did they say it was fair? | | |
| Chased into a stairwell: reaction when it stops at the door | | |
| Nudges used (1, 2, 3) | | |

## 6. Measures

These replace `10_proposal.md` §10. SP = a starting value.

| Measure | Target | Baseline | Source |
|---|---|---|---|
| **Q1** Names the goal unprompted at the goal check | ≥ 7 of 9 (≥ 5 of 6) | 1 of 9 | Sheet |
| Clears Floor 1 on the first run | ≥ 90 % (SP; A40 means no chase early on) | — | CSV |
| Takes the key with no nudge | ≥ 80 % (SP) | — | Sheet |
| Reaches the EXIT within 60 s of the take | ≥ 70 % (SP) | — | CSV |
| Unlocks the EXIT on the first try | ≥ 80 % (SP) | — | CSV + sheet |
| **Q2** Opens the plan | ≥ 50 % | — | CSV |
| Cue named most helpful | Report only | — | Form |
| **Q3** Notices the arrows, among those who saw a turn | ≥ 2 in 3 (SP) | — | Form + CSV |
| Trust rating (5-point) | Report the spread, no target. Read Rating 05 only where Rating 03 is 3 or higher: a player who saw no arrows still has to mark it | — | Form |
| The wallpaper's movement read as intended vs glitch | Report only, coded from their words | — | Interview |
| **Q4** First catch follows at least one lamp burst | ≥ 90 % | — | CSV (`w1 burst`; filter on floor, since bursts below LEVEL 0 count too) |
| Says what dipping lamps mean | ≥ 5 of 9 (≥ 4 of 6) | — | Interview |
| Ever mixes up the TV with the hunter | 0 | — | Sheet |
| Understood why they were caught (rating ≥ 4) | ≥ 2 in 3 (SP) | — | Form |
| The crosshair eye read as "being seen" | Report the count (A23) | — | Interview |
| **Q5** Hears the TV before seeing it on a B floor | ≥ 5 of 9 (≥ 4 of 6), if stations landed | — | Sheet |
| Reaches a B-floor station with no nudge | ≥ 60 % (SP), if stations landed | — | CSV + sheet |
| Nudges per player | ≤ 1 on average | — | Sheet |

## 7. Form changes (FR-PT 05 + FR-PT 04)

Replace, don't add: the layout and reader stay as they are. Items marked **new** need 平面视觉 · 印刷与演示 and Red's OK. Today's form asks about the old game, says "Relay" (a name players never see) and has no way to name the plan, TV or arrows.

| Slot | Today | REV |
|---|---|---|
| Rating 01 | I knew where to go next. | Keep |
| Rating 02 | I understood what crossing a door or threshold meant. | **The evacuation plan told me what I needed to know.** |
| Rating 03 | I could tell when a room had changed. | **I noticed arrows in the wallpaper.** |
| Rating 04 | Movement and camera controls felt predictable. | Keep |
| Rating 05 | I knew when sprinting would help me. | **I trusted the arrows in the wallpaper.** |
| Rating 06 | Doors, light, and room details helped me orient myself. | **The sound of the TV led me somewhere.** |
| Rating 07 | I noticed Relay through sound or presence before it reached me. | **I noticed something coming by sound or light before it reached me.** |
| Rating 08 | I understood why I was caught or escaped. | Keep |
| Interview 05 | If you saw the crosshair turn into an eye, what did you think it meant? | **Did the wallpaper ever seem to change? What did you make of it?** The eye question moves to the spoken lines (§4): it is Red's 10-22 check (A23) and must stay |
| Interview 01–04, 06 | | Keep |
| Most helpful cue | DOOR · ROOM DETAIL · SOUND · LIGHT · HUD · RELAY | **PLAN · TV · ARROWS · EXIT SIGN · LIGHT · HUD** |
| Before play | What did you expect to do when the opening began? | Keep |

All new strings are signed by 叙事 (`research/narrative/80_form_and_station_paper_signoff.md`). Rating 07 says "something coming", because "the hunter" is a name players never see, the same fault as "Relay".

The real print + scan test (Red's, `handoff/graphic_print.md` §4) should happen on the new REV, with one blank calibration sheet.

## 8. Data and privacy

- **Consent:** the form's footer line, plus the opening script. Ask separately before any screen or voice recording.
- **No names:** players are P01, P02… The key linking names to numbers is not kept.
- **Outside the repo:** the GitHub repo is public. Keep scans, sheets, notes, recordings and CSVs in `~/FrontRoomsPlaytest/2026-11-04/`.
- **CSV:** since PT1, one `events-yyyyMMdd-HHmmss.csv` per run in `Application.persistentDataPath`, kept up to date at a catch, a floor change, quit, app pause and scene change. Copy the player's files after each session and prefix them with the player's number. Columns: time_s, event, detail, relay_m, floor, session. `session` counts launches and carries over between players on one machine, so write it on the observation sheet when a player starts.

## 9. What each result decides

| Result | Then |
|---|---|
| Goal named by under 5 of 9 | Fix Floor 1 first: what the plan and first room say. Nothing else ships ahead of it |
| Arrows unnoticed by over half | Build L10, a first-arrows moment on Floor 1 |
| Players follow arrows but report distrust | Good news for the teacher's reading. Report it; change nothing |
| Wallpaper movement read as a glitch by most | Raise it with 动画 and 叙事 before adding motion |
| TV not followed by ear on a B floor | 声音设计 raises the far band; check the doorway positioning |
| TV mixed up with the hunter by anyone | Stop and fix before the next round: this breaks one meaning per cue |
| Catches called unfair | With 系统设计: first-chase hold before the v2 flip (BS3) |
| Key or unlock needs nudges | Review the rack's readability and the prompts |
| Testers read the eye as "being seen" | Red's rule (A23): switch E·1 to E·3 (parameters only), via 平面视觉 |

**After the round:** 教程设计 writes up the numbers, and 周志 · 课程交付 makes the one-page "how players responded to the guidance system" for the teacher.

## 10. Requests

| # | Owner | Request | Needed by |
|---|---|---|---|
| PT1 | 关卡设计 | **Done 10-31** (Game f27d4bb4, WayCueDriver c59646c7): events CSV written at a catch, floor change, quit, app pause and scene change; new rows `w1 burst`, `arrows` (stage 1 or chase, per entry, not per wall), `exit` / `tried locked`; `session` column. Checked by bots, except `tried locked`: see the dry run in §2 | 11-02 |
| PT2 | 平面视觉 · 印刷与演示 | §7's REV of FR-PT 05 and FR-PT 04 and the reader template. Moderator sheet FR-PT 06 (§3–§5, Figma 3853:9229). Drafts in Figma section 3852:9229; InDesign build waits on Red's OK in its chat | 11-03 |
| PT3 | 叙事设计 | **Done 10-30.** §7's strings signed and sent to 印刷与演示 | 11-02 |
| PT4 | App Store · 构建 | A Mac snapshot on Red's yes, 11-03, after the batch or at the cut-off | 11-03 |
| PT5 | Red | Book the players and the room, print the forms, run the sessions | 11-03/04 |
| PT6 | 周志 · 课程交付 | The one-page summary after the round | after the round |
