# NOTE · The station as the first room: tutorial constraints (for station-first-room, wf_89885526-506)

Written 2026-10-22 16:4x by FrontRooms 总控, tracking the 教程设计 line on account A (no 教程设计 chat there). The synthesis stage reads this before writing `61_for_red_first_room.md`. Sources: `research/tutorial/10_proposal.md` (approved by Red 10-22), `research/tutorial/01_research.md` R1–R10, `research/tutorial/20_requests.md`, `system_design/systems.json`.

## 0. Red's words (游戏视觉 chat, ~15:20, verified in its transcript)

> 同时我需要你调研，当前接待室/警卫室应该是有一个单独的房间墙体设计，而不是直接利用 level 0，要做区分，并且这个房间从关卡设计的角度来说，应该是玩家第一个进入的房间，这样能够让玩家首先对游戏有所认识，这也是从教程设计的角度来说

Two asks: (1) the reception / guard room gets **its own wall design**, distinct from Level 0; (2) it is **the first room the player enters**, so the player meets the game there first.

## 1. What stays true (Red-approved; reopen only by asking Red, and name it in 61)

| Decision | Content | Status with "station first" |
|---|---|---|
| T1 = A (A13) | Every floor's EXIT is a locked stair door; its one floor key hangs at the station, where the floor's one TV plays | **Kept.** The room changes place, not role |
| T5 (A17) | No text card; controls on title + pause | Kept |
| T6 (A18) | Title TV plays with sound | Kept; it teaches the jingle's source once (R3) |
| T3 (A16) | Desk CRTs on a station floor are still; the station TV is the only moving screen | Kept. V4 is already gated on "this floor has a station" (20_requests, 16:0x) |
| Sheet A placard (A21) | The rule sheet: four lines + floor number | Kept. **The first room is the natural mount** (see §3) |
| A20 | Route → station before the take, → EXIT after | Kept |
| C22 / C23 | EXIT → stairwell → next floor; LIT-locked EXIT sign | Kept |
| T2 = E1 (A14) + first-approach grace | Floor 1: the hunter is not sent until the take; its first chase needs a W1 burst first | **Kept in principle, timing must change** (§2.3) |
| C32 (Red 10-22) | Floor 1's first send: 1 s after the take, arriving 22–30 m away on the station's far side from the EXIT | **Reopened** if the take happens in the first room (§2.3) |

## 2. What "station first" changes (each is a conflict the research must resolve)

### 2.1 Lock before key (R5) on Floor 1
Today the route passes the lit, locked EXIT before the station (§3 step 4, §4.3: station 2–6 cells **past** the EXIT). If the key sits in the first room, the player has the key before ever meeting the lock. The lesson "I need something for this door" becomes "I have a key, where is its door?".
- That still works: L2 reads from the key tag `STAIR A` and the placard's line 4. But R5 says to show lock and key both, lock first where possible.
- **Constraint:** either keep the locked EXIT visible before the take, for example from the station's doorway, or accept key-first on Floor 1 and say so in 61.

### 2.2 The beacon lesson (R3, R4)
Floor 1 was where the player follows the TV by ear through doorways (§3 step 3). With the TV in the first room, the player meets the source up close first. R3's "teach its sound next to its source once" gets stronger, but nobody practises following the sound until Floor 2.
- **Constraint:** Floor 2+ must keep the beacon walk (station 10–20 cells off the EXIT route, §4.3). Otherwise the beacon never does its job.
- **Recommendation:** "first room" is Floor 1 only. On Floor 2+ the station stays a found room. The one rule (key at the TV) still holds on every floor, so R6 is not broken.

### 2.3 The first send (E1, C32) would come too early
If the player takes the key in the first room, a send 1 s later at 22–30 m means the hunter is out within seconds of the start, before the player has read the placard, learned the controls or seen the maze. The first catch must always follow a warning (grace), and W1 needs ≥ 2 live Steady lamps in the player's room set.
- **Constraint:** the first send waits until the player has **left the first room** (for example the station door or start door shuts behind them, or they are ≥ N cells out), with the 150 s fallback kept. It still arrives on the far side from the player's route, never on the walk to the EXIT.
- This reopens C32's "1 s after the take" for Floor 1. **Red must answer that**, numbered 1/2/3 with how each feels to the player.

### 2.4 The title → map handoff
Today the start rooms are the title stream and carry no signs. Control returns in place on Space, and the map lies behind the next shut door (start door, P1 west placard mount).
- "First room" can mean (a) the last title-stream room, restyled as the reception, or (b) the first map room behind the start door.
- **Constraint:** whichever it is, the placard (G1) and the TV must be in that room's first sightline after control returns. The start door's NO RE-ENTRY rule and the title teardown at the first seal (C22 D-rules) must still hold.
- Owner of the choice: 关卡设计 (title handoff + map) with 游戏视觉 (look). 61 should recommend one.

### 2.5 Its own walls (not Level 0)
Today `Station · Waiting` is a Level 0 theme module and `Station · Desk` an Office theme (§4.2).
- A distinct room theme needs a new wall finish / module theme in the Level Designer, and a seam rule where it meets Level 0.
- `LEVEL_MODULE_SPEC.md` (关卡设计) and the kit (游戏视觉) both change.
- Era lock 1990, no brands; the room must still read as part of the same building (EGRESS: the building is honest about itself).

### 2.6 Metrics (§10) to restate
"Reach the station within 90 s" becomes trivial. Replace it with "reach the EXIT within N s of the take" and "unlock on the first try". Keep "name the goal after Floor 1" (≥ 7/9) and "first catch after a W1 burst" (≥ 90 %).

## 3. What gets better (say so in 61)
- **R1 goal first, at once:** the first room shows the TV, the key on its rack, the placard and, ideally, the locked EXIT through a doorway. That is the whole loop in one view.
- **The placard has a home:** reading it in a quiet, lit, safe first room fits EXAMINE's pause. The start-door mount P1 west was a compromise.
- **R2 rehearse once:** the key take happens where nothing can go wrong yet.

## 4. Options for the research (tracker's sketch, not a decision)

| | Floor 1 first room | Lock before key | First send | Cost notes |
|---|---|---|---|---|
| **1 · Station first, EXIT in view (recommended if geometry allows)** | Station + placard + TV; the locked EXIT visible through its doorway at ~3–6 cells | Kept (seen first, taken second) | When the player leaves the room (door shut / ≥ N cells), far side, 150 s fallback | Map: the station placed as the start group plus an EXIT sight line; C32 retime |
| 2 · Station first, EXIT found later | Station + placard + TV; the EXIT elsewhere, found by the route after the take (A20) | Lost on Floor 1 (key first) | As 1 | Map: simpler placement; still a C32 retime |
| 3 · Reception first, key not here | Reception look + placard + the title TV; the key's station elsewhere on the floor | Kept | Today's E1 / C32 unchanged | Smallest change. Meets "own walls + first room" but not "the station is the first room" |

Floor 2+: the station stays a found room (§2.2) in every option.

## 5. Owners if Red picks
- 关卡设计: placement / start group, title handoff, first-send gate (M7), LEVEL_MODULE_SPEC theme.
- 游戏视觉: room walls and look (V1), lamps (≥ 2 Steady), the TV light along the doorway axis (§4.6), the key light as the subject of the take (§4.7).
- 平面视觉: placard mount in the room (G1 moves).
- 叙事: reception vs guard post fiction; the plate (`STAIR KEY` / `SILENT ALARM…`) is unchanged.
- 声音: the S1 loop is unchanged; Floor 1 becomes near-field; S2 `Screen/Beacon` still needed for Floor 2+.
- 系统设计: record changes to C32 / E1 / N6 / N7 only after Red answers.
- 教程设计 (总控 on account A): update `10_proposal.md` §3, §4.3, §5.3, §6, §10 and `20_requests.md` M2/M4/M7/V1 once Red picks.
