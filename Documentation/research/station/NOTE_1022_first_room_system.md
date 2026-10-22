# System constraints: "the security desk is the first room" (系统设计, 2026-10-22)

*For 游戏视觉's research run `wf_89885526-506` (Red's new direction at ~15:20 in the 游戏视觉 chat: the security desk gets its own room design and is the first room the player enters). This note lists only the system rules each option must state against, plus the records an option would re-open. The options are the run's job; the decision is Red's; the data (`system_design/systems.json`) is changed by 系统设计 only after Red picks.*

## 1. What is decided today (data 2026-10-22z7)

| Id | Rule | Where |
|---|---|---|
| C22 | Each floor has an EXIT; behind it a stairwell leads one floor deeper; a run ends only when caught | RELAY decisions block |
| A13 (T1 = A) | Each floor's EXIT is locked; its one floor key hangs at the station, the only room with a TV playing; the key opens the EXIT and every locked door on that floor. Floor 1 has a key and no locked inner doors | tutorial `10_proposal.md` |
| A14 | Taking the key kills the TV (no CALLED tick) and sends the hunter to the station; it arrives ≥ 42 m from the player, except C32 | RELAY §5 |
| A15 (E1) | Not sent until the key is taken (fallback 150 s or a third zone); on Floor 1 no chase before the first lamp burst unless within 6 m | RELAY decisions block |
| C32 | Floor 1's first send: 1 s after the take, unseen, 22–30 m, **on the station's far side from the EXIT**, never within 2 cells of the walk back (fallback 18–34 m, then ≥ 14 cells, logged); ≥ 2 Steady lamps by the station entrance | RELAY decisions block |
| A20 / C24 | The arrows point at the station until the key is taken, then at the EXIT; arrows and the TV beacon never pull two ways | RELAY §9.6, `cue_driver/MAP_ROUTE_SPEC.md` |
| N7 | Floor 1 teaches the loop: **the station just past the EXIT**, so the player meets the locked EXIT first, fetches the key, and the lamps dip on the way back | systems.json N7 |
| I1, A17, A18 | The placard (sheet A) by the start door teaches the rules; the controls show only on the title and pause card; the title-corridor TV plays the ad with sound | placard `50_tutorial_placard.md` |
| C01, A25 | RELAX 100 s at T1 after an Away, no jitter; first-run grace 60 s of RELAX from Armed | RELAY §4, §11 |

## 2. What a first-room desk must still satisfy

Each option should say yes or no to each line, and how.

1. **The lock teaches before the key.** N7's order is: see the locked EXIT, then fetch the key. If the rack is in the first room, the player can take the key before ever meeting the lock, and the lock teaches nothing. An option must do one of these:
   - keep the order: the rack is closed until the EXIT has been seen or tried;
   - move the key out of the first room;
   - accept key-first, and say what teaches the lock instead.
2. **The first encounter comes no earlier than today.** C32 sends the hunter 1 s after the take. A take in the first room 10–20 s into the run would bring the first encounter before any teaching and inside the first-run grace. The first send must stay gated by the key take **and** the grace (or E1's fallback). C32's geometry must still be satisfiable: "the station's far side from the EXIT" and "never within 2 cells of the walk back". If the desk is the start, say where the far side is.
3. **One meaning per cue.** If the player starts at the station, A20's "station first" phase is trivially done on Floor 1. The arrows then point at the EXIT, and the TV beacon must not pull back to the desk. A14 kills the TV on the take, so that holds if the take happens there. State whether Floor 2+ also starts at its own desk: if it does, the A20 station phase disappears on every floor.
4. **The title handoff.** Today the start rooms are the title stream (title corridor TV with sound, A18; control in place, the map behind the next shut door). Say whether the desk *is* the title room, which would make the title TV and the station TV one TV with one meaning, or whether it is the first map room behind the title door.
5. **The placard.** Sheet A is drawn for the start door, with the route to the station. A desk-first start changes its route and its spot (平面视觉's art, packed by 游戏视觉).
6. **Safety.** The first room is never an Arrive spot and never inside a send ring. It keeps ≥ 2 Steady lamps (C32) and obeys the photosafety limiter (P4, in main since 10-22).

## 3. Records an option re-opens

| If the option… | it changes… | and needs |
|---|---|---|
| puts the key in the first room | N7, A13's wording, maybe C32 | Red's pick only (the rules above still hold) |
| moves the first send or its distance | C32 (Red, 10-22) | Red re-opening C32 |
| makes every floor start at its desk | A20's station phase, N6, N3 (route) | Red's pick; 关卡设计调研 updates MAP_ROUTE_SPEC |
| merges the title TV and the desk TV | A18, N6, I6 | Red's pick; 广告 for the ad's role |
| changes the placard route | I1 (sheet A), A21 | 平面视觉 re-draws |

系统设计 records Red's pick from Red's own words. It asks Red nothing new unless an option needs one of the decided rules above changed. In that case the decision page should name the rule and the reason.
