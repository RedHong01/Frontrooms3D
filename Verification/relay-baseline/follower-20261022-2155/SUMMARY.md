# Follower bot on main after the wayfinding landing · 2026-10-22 21:55

*关卡设计调研. MAP_ROUTE_SPEC §8 "in play": the follower walks each cell's wall-cue arrow and never plans a route of its own.*

## What ran

- **Build:** main `1521cd81` + 1 dirty path, synced to my clone `~/FrontRoomsResearchWork/proj`.
  - Wayfinding 4f241320 (with S1: no cap on the target pass), WayCueDriver 4a6fb907, WarnLamps 076756c3.
  - Baseline.cs eca67b5e.
- **Command:** `EXTRA="-autopilotExit" Tools/relay_baseline_run.sh <seed> follower 1`. T1, 4 min after release.
- **Trips:** after reaching N the bot walks far routes for 20 s, then follows the arrows back again.

## Results

| Seed | Verdict | Arrow steps | Glass / wall / uphill / loop | Trips reached N | First N after release | Message-0 s | Field max / mean |
|---|---|---|---|---|---|---|---|
| 2554 | PASS (caught) | 37 | 0 / 0 / 0 / 0 | 2 of 3 (the last cut by the catch) | 7.8 s | 0 | 2.70 ms / — |
| 20388 | PASS (caught) | 88 | 0 / 0 / 0 / 0 | 4 of 5 (the last cut by the catch) | 8.2 s | 0 | 5.38 ms / 1.39 ms |

- **Identical to the clone runs** with the 20:2x kit: same steps, same trips. The landing and S1 changed no route these walks used.
- **R8 in-game outlier:** 5.38 ms on 20388, with the machine's load average at 345–396 during the run. The Relay's tick also peaked at 13.4 ms. That is load noise, not the field. 关卡设计's graded R8 (batch, live WorldMap) is a 1.09 ms max, and this run's mean is 1.39 ms.
- **Not covered:** the station (M4 isn't built, so the target is N everywhere), Tall halls (neither seed's walks entered one with message 0), and floors 2+. The seal is off in baselines.
