# Onboarding and gait baselines, 2026-10-30

Level-design research (关卡设计调研, account A). Red, 2026-10-30, via 关卡设计: continue the research from where it stands, in the current context. All runs are T1, tier-locked, at a fixed 1/60 s step, in clone `~/FrontRoomsResearchWork/proj66` (Unity 6000.6.3f1). The harness is `FrontRooms3DGame.Baseline.cs` r3 (`build.harness = "step0 r3 (2026-10-30)"`).

## What was measured

| Folder | Main at the run | Cases |
|---|---|---|
| `preonboard/` | 12:3x: Relay v2 V2 sight (live), the gait rule (desktop walk heard 9 m a step), V3a with the define off; old M7 gate | 2554 follower (`-autopilotExit`); quiet as walk, warnslow and slow; walkaway; relaxsprinter. The non-follower runs have no `-autopilotExit`, so there is no station and the Relay comes by the old timed release. |
| `./` | 13:0x: A40 onboarding landed (Game e51939cc, MapHunter dc88b1df) | All with `-autopilotExit`, so floor 1 has its station: follower 2554 and 20388; follower 2554 with `-autopilotOnboardOff`; quiet as walk and as warnslow; relaxsprinter; evader06 on 2554 and 20388 |

- `evader06` ran on the final r3 harness, which reads `firstSend.byFirstSend` and the placement on the tick after the release.
- The other runs in `./` predate that fix, so their placement fields are empty. Their placement is quoted below from the game's own log line ("RELAY · first send").

## Findings

1. **An arrow-following player never meets floor 1's onboarding window.** The follower:
   - reaches the station at 9.5 s, takes the key at 13.1 s and reaches N (the EXIT) at 16.6 s;
   - gets floor 1's first send only at the 180 s fallback, with 79 to 97 cells explored.
   
   A real player who follows the arrows is on B1 within about 20 s, so their first encounter is the B1 one. That encounter needs `-autopilotBotAfterExit` (关卡设计, next package) to be measured.
2. **A wandering player reaches 150 cells before the fallback.**
   - quiet: gate Explored at 153.5 s after the start door shut;
   - evader06 on 20388: 129.9 s;
   - relaxsprinter: 124.8 s, because sprinting covers cells faster.
   
   Sprint noise does not bring the Relay early: the gate counts only cells and time.
3. **The first send is placed the same way in every run:** rung 1, 8 to 10 walking cells away, unseen, 12.2 to 22.1 m straight. The warning reaches stage 1 at once on the first sample. `-autopilotOnboardOff` reproduces the old gate exactly: Left at 6.7 s, 2 cells, 12.8 m, chase after 4.6 s.
4. **Warning lead before the first chase:**

   | Gate | Run | Lead |
   |---|---|---|
   | Old gate | follower | 4.6 s |
   | Onboarding | follower parked at N (2554) | 7.3 s |
   | Onboarding | all other runs | 33 to 64 s |

   The parked follower is a harness artefact: baselines keep the EXIT shut, so it waits 3 to 6 cells from the station while the first leg hunts to the key.
5. **Every first chase was a catch: 8 of 8, including both evader06 runs.**
   - Chases started 3.1 to 8.2 m from the player, 6 of 8 inside the 6 m near-grace, at stage 2, and ended in 0.4 to 1.3 s.
   - evader06 (0.6 s reaction) had no time to act: on 2554 the catch came first; on 20388 it was caught 0.3 s into its run.
   - Under v1, once the chase starts, the warning is the only defence. The near-grace keeps the first chase from starting farther out, where it could be escaped.
   - This is data for HR16 (catch distance) and V4b (lock-on, chase).
6. **The desktop walk is heard, but at T1 this rarely matters.**
   - Before onboarding: quiet made 341 walk noises in 3 min and was heard 0 times (85 % of the on-map time at stage 0).
   - After onboarding: quiet was heard 3 times of 404, and one walk-noise hunt led to its catch. warnslow, which creeps while the warning is at stage ≥ 1, was heard 0 times and had no hunt, but was still found by a wandering Relay at 64.4 s.
   - With one seed this is not a finding about the gait.

## Harness r3 (what the reports now carry)

- `gait`: the rule (`-autopilotGait walk|slow|warnslow`), seconds in each `PlayerGait`, and walk noises emitted, heard and hunted. `WalkStepNoise` is the only caller with `AutoCauseOther`, so `otherNoise` counts walk steps.
- `firstSend`:
  - floor, play seconds, seconds after leaving the start;
  - the game's `floorExplored` and `StationFallbackClock` at the send, plus the tier's floor-1 cells;
  - whether the key was taken and N reached before the send;
  - `byFirstSend` (`OnFirstSend`) and the placement: rung, cells, straight metres, unseen, flow;
  - the first stage 1 and stage 2 after the send, the first chase, the warning lead, and how that chase ended.
- `director`, under `FRONTROOMS_RELAY_V2` only: Calls by cause, Drops, FailedTries, PendingSecondsTotal, LockOns, Arms, LastAwayReason, the longest Withdraw, and RELAX's sprint seconds, Attention gain and Attention calls. `flags.v2` is set to `on` or `off`.
- New modes:
  - `relaxsprinter`: sprints in v2's RELAX, which must gain 0; in v1 it sprints at stage 0 with no chase.
  - `walkaway`: while warned, walks away from the Relay and shuts doors behind it. The G row "stage-1 share ≤ 40 %" is reported only.
- Reports from a non-walk gait, or from `-autopilotOnboardOff` (matched in any case), get their own name suffix, in both the JSON and `run.sh`.

## Next

- The B1 first encounter, after 关卡设计 lands `-autopilotBotAfterExit`: follower, quiet and evader06 on 2554 and 20388.
- The define-on 8-run smoke, in a clone only. Its evaderv2 G5 rows wait for 关卡设计's HR16 signal.
