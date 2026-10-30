# 60 · Floor 1 is calm now: what the copy tells the player (叙事设计, 2026-10-30)

**Why this exists.** Red, 2026-10-30, 关卡设计 chat (verified in transcript d1fc23ab):
- 11:29, the decision: "我不希望在玩家onboarding的时候就被追逐，最开始的阶段应该是玩家开始熟悉游戏的阶段". Red then answered 按探索量延后 and 沿用到 B1.
- 12:16, the direction for the research lines: "他们应该继续调研，但是不要从头开始调研，而是按照现在的进度开始更符合语境的调研".

This continues `40` (floors, EXIT, tutorial strings) and the station research (`station/01`, `station/20`). It starts from neither one again.

**The rule being built.** 关卡设计 is building it as wf_cdecb998-677 (brief `~/FrontRoomsMapWork/integration/s41_onboard/BRIEF.md`; the landing note is still to come).
- Floor 1 sends nothing until either:
  - the player has stood in 150 unique floor-1 cells (outside the start area, the station and X); or
  - 180 s have passed since the start door shut.
- Taking the key no longer sends it. The take is still remembered, but only while the player is on floor 1 when the gate fires (§2, as landed):
  - on v1, the first leg hunts to the take point if the key was taken;
  - on v2, the take is a device call, held until the release.
- The run's first send is the gentle one, wherever it happens: unseen, 22–30 m away, with the lamp warning first. That is on floor 1 after the threshold, or on B1 if the player leaves floor 1 first.

**Verdict: no player-visible string changes.** Nothing the player can read says "it comes when you take the key" or "it comes once you leave the room". Three things change on paper only:
- the take's fiction gets a delay (§2);
- the B1 stairwell placard matters more (§5);
- my design notes that described the old send are amended (§6).

Red has nothing to decide here.

## 1. Audit: floor 1 and the B1 arrival

Every line was checked against two false readings: (a) taking the key brings it at once; (b) leaving the room brings it.

| Copy | Where | Verdict |
|---|---|---|
| `IN CASE OF ALARM` / `ON ALARM, A FLOOR WARDEN CHECKS EVERY ROOM.` | placard, both floor-1 sheets (`sheetA_room`, `sheetA_room_phase0`) | Holds. It names no trigger and no time |
| `1 WALK, DO NOT RUN.` / `2 CLOSE DOORS BEHIND YOU.` | placard | Holds |
| `3 IF THE LIGHTS DIM, LEAVE THE AREA.` | placard | Holds. The take's own dimming is covered in §3 |
| `4 STAIR DOORS ARE LOCKED. KEY AT SECURITY DESK.` / `4 EXIT BY STAIR, ONE FLOOR DOWN.` | placard | Holds |
| `STAIR KEY` / `SILENT ALARM: REMOVAL SIGNALS PANEL` | key-board plate (EXAMINE and close reading only) | Holds (§2). The take still signals, and it is still answered, later. SILENT means nothing rings |
| Desk log, sign-out sheet, register (`station_dar`, `station_sign_out`, `station_register`) | desk paper (blank until 平面视觉's atlas) | Holds. The log's open `0200  TOUR 3` also explains the delay (§2) |
| `LEVEL 0 / FRONT ROOMS`, `E · TAKE KEY`, `STAIR KEY` / `OPENS THE STAIR DOOR`, `LOCKED · NEEDS STAIR KEY`, `E · UNLOCK`, `E · READ SCREEN` | HUD and prompts | Holds |
| THREAT READOUT (`IT  /  …`, `IT  23 M  /  TIER 2`) | assist, off by default | Holds. It shows nothing until the release, so the calm minutes show no IT line |
| The TV ad (WTCZ-TV 61) | the playing station TV | Holds. It has no words about the building |

## 2. The take: the alarm is answered later

The station fiction (`station/01_research.md`, "Why taking it calls the hunter"):
- the key board is a device on the alarm panel, and lifting the key opens its contact;
- the panel drops the desk's circuit, so the set dies;
- the panel sends its responder to the device.

Red's rule keeps every part of that except "at once".
- **The set still dies and the light still falls at the take** (`FrontRoomsStationDress.cs` `OnKeyTaken`, `FrontRoomsStationTvLight.cs`). That is the panel cutting the desk's circuit, as written.
- **The responder still goes to the device:** on v1 the first leg hunts to the take point; on v2 the held device call goes out at the release.
- **It answers late.**
  - Nothing in the copy promised "at once". A silent alarm is answered by whoever the panel calls, from wherever they are.
  - The desk log shows the post has been empty since 0200 (`TOUR 3`, never `COMPLETE`), so nobody at the desk hurries the answer.
  - The hunter is still never the guard (`01`'s rule).

So the plate stays as signed: SILENT says nothing rings, and SIGNALS PANEL says someone is told. Neither says when.

**As landed (A40, 2026-10-30; MapHunter dc88b1df, Game e51939cc, RelayV2 7f56a6fb).**
- The take is answered on floor 1 only if the player is still there when the gate fires: 150 cells, or 180 s of game time (close-ups and pause stop that clock).
- A player who takes the key and reaches the EXIT first meets the gentle first send on B1. On v2 the held call is dropped at the floor change.
- 关卡设计's autopilot exit runs spend about 10 s on floor 1, so this is the common case.

**The plate stays as signed** (叙事设计's call, 10-30; 教程设计's decision stands). It makes two claims, and both come true at every take: removing the key signals a panel, and nothing rings. The set dying and the light falling are the signal's visible half. The plate promises no responder, no time and no place.
- **A player still on floor 1 when the gate fires** sees the answer go to the board.
- **A quick player** meets the first send one floor down. From their side the alarm was answered on B1, and the placard's `ON ALARM, A FLOOR WARDEN CHECKS EVERY ROOM.` holds on any floor. Nothing they can see contradicts the plate.
- **The fallback `STAIR KEY` alone is not needed.** Recheck only if the take ever stops cutting the desk's circuit, because the TV going dead is the plate's one visible proof.

## 3. The take's dimming and placard rule 3

At the take the set goes off and the station light falls. The room drops toward "Hollywood dark", with the two exits brightest (`station/20` G5, G8). A player who has read rule 3 (`IF THE LIGHTS DIM, LEAVE THE AREA.`) will leave the room.
- **Before:** the send followed 3 s later, so leaving was the lesson.
- **Now:** nothing follows. That does not break rule 3. It promises no threat; it only says to leave. Leaving is still right: the exits are lit, and the floor is there to learn.
- **The first real dim** comes later, on floor 1 or on B1: it is the lamp warning of the gentle first send. Rule 3 works again then, with a threat behind it this time.
- **The risk is behaviour, not truth.**
  - The take reads as an alarm, so some players will go straight for the EXIT, 3–6 cells away, and skip the floor-1 learning Red wants.
  - Those players meet it first on B1. Red accepted that (沿用到 B1).
  - How many players do this is for 关卡设计调研's bots and the playtest form to measure; it is not a string question.
- **I would not soften the take.** It is the room's one cause-and-effect beat (G5), and the late answer pays it off.

## 4. What floor 1 tells a player who explores in safety

EGRESS rev 4 says the paper is the plan. The calm minutes let the player test the plan against the floor without being chased.
- **`THIS FLOOR MAY DIFFER FROM PLAN` is now learned at walking pace.** The player finds out the plan is wrong before anything punishes them for trusting it.
- **The legend's `WALL ARROWS, POINT THE WAY OUT`**, the exit signs and the locked EXIT can be learned in order.
- **The desk paper tells the night in the building's own records:**
  - on duty at 2300, two tours complete;
  - lamps out in stock room 3;
  - tour 3 never closed;
  - nobody signed in today (the register).
  - No line addresses the player.
- **No new strings.**
  - The no-pop-up rule holds: the placard is the tutorial.
  - The quiet is the after-hours building. A line that explained it would turn the calm into a timer.

## 5. B1's first encounter

A player who takes the key and goes straight down meets it first on B1, through the gentle send: unseen, 22–30 m away, the lamps dimming first.
- **What frames it is rule 3**, from floor 1's placard, if the player read it. The first dim on B1 is the moment that rule was written for.
- **B1 has no placard yet.** Its stairwell copy by the far door (`sheetA_phase0_B1/`; A35, since B1 has no station) is the last thing a rusher passes before the first encounter, with rules 1–4 in view.
- **Recommendation:** when B-floor placards are packed (游戏视觉, on 关卡设计's anchor), pack the B1 stairwell copy first. The art is signed, and it needs no new strings.
- **The stairwell behind the player is the safe zone** (Red, 10-22: "这种楼梯间其实是安全区"). The copy there must keep reading as protected: no warning words.

## 6. Paper fixes (docs only, done 2026-10-30)

- `40_floors_exit_tutorial_strings.md` v1.4: §9's "the first send waits until the player has left the room … 150 s fallback" is marked superseded.
- `station/20_layout.md` v1.7: the v1.3 send bullet and §4.5's notes on N = 2 and the 150 s fallback are marked superseded and kept as history.
- `station/01_research.md`: "Why taking it calls the hunter" gets the delay (§2).
- **For 教程设计 (its contract):**
  - `tutorial/20_requests.md` already says M7 REPLACED.
  - The key-plate decision (`10_proposal.md` §4.7, N2 note) stands, for the reason in §2.
  - The beat rows that time the first send from the take are 教程设计's to update.
