# Relay pursuit v2: Step 0 baseline conclusions

*系统设计, 2026-10-07. A code auditor and a data auditor checked the reports independently (workflow `relay-baseline-audit`), and an analyst wrote the tables. I then recomputed the headline numbers from the JSON myself, and they match: 62 real hunts, 16 of 31 walking chases with no cause, 51 of 86 re-arrivals under 42 m, 45 of 52 runs caught, the stage-0 shares, and the evader escapes. The design doc is `Documentation/RELAY_PURSUIT_REDESIGN.md` (§2.6 and §14 cite this file).*

Source: the 54 JSON reports and MATRIX_RESULT.txt in `Verification/relay-baseline/`. I computed every aggregate below from the JSON fields named in each table, as plain sums or means over seeds. `n` is the number of runs. The two code and data audits from 2026-10-07 decide which fields are used.

## 0. What these numbers are

- **Relay measured:** the v1 hunter. `FrontRoomsMapHunter.cs` has no committed change between df4cb03 and 04c3a6a, so the Relay logic should match 04c3a6a.
- **Harness measured:** an older build. The reports were written 12:22–13:00 on Oct 4, before commit 04c3a6a at 13:34. In that build, sprint noise skipped the counters. 52 of the 54 reports also predate the start-exit-depth fix (MATRIX_RESULT.txt).
- **What the ladder is:** the warning stages are hypothetical. They are the v2 bands (§9.1) computed on a v1 Relay that never goes Away. They show how often the v2 ladder *would* have fired with v1 movement. They do not show how v2 will behave.
- **Small samples:** n = 3 seeds per cell (2 for doorspammer). Treat every figure as a baseline order of magnitude, not a pass or fail.

## 1. Exclusions and relabels

| Item | Decision | Why |
|---|---|---|
| 7-doorspammer-T1/T5 | Excluded from everything | 2 toggles in 192 s; released at 174.5 s, so only about 18 s of exposure |
| doorspammer 2554/20388 | Kept, read as "toggles nearby doors" | Rotates doors every 20 s, not one door; T5 runs last only 12–15 s |
| noisy | Kept, read as "sprints and shuts doors" | windowsTried = 0 in all 6 runs, so glass was never tested |
| closedzone | Kept, read as "sprints in a small zone by the start" | doorsShut = 0 in all 6 runs; says nothing about closed zones |
| edgerunner 2554/20388 | Kept for Relay behaviour | Older start-exit depth; edgeStalls = 0, so A7b is not caused by edge running |
| evader03/06 before the first chase | Not independent samples | Identical to quiet until the first chase (same routes, same first chase times) |
| Sprint fields, `huntsByCause.other` | `other` read as **sprint (inferred)** in noisy, shiftholder and closedzone | Sprint noise reached the Relay but not the counters |
| `hunts`, `huntsPerMinute`, `huntsByCause.chaseLost` | Corrected: real hunts = hunts − breakAttemptsByState[Hunt]; real chaseLost = chasesLost | A door-break resume was counted as a new hunt. The identity holds in all 54 runs |
| `callsByCause.*`, `wanderEncounters`, `chaseLog.noiseBefore` | Not used | Units mixed, double counting, unheard noise counted. Chase origin comes from `chaseLog.from` instead |
| glass fields | Not used | Never tested |
| `dormantShare`, `wStage0OfRun` | Not used | Mostly start-room time |
| frame-time perf, `bot.sprintBursts` | Not used | Batch editor under load with screenshots; burst counter split at every cell |
| T1 label | Read as "starting tier 1" | Tier rises stay on. Several T1 runs reach T5 (tierLog) |

After the exclusions, **52 runs** remain, with 5,059 s of on-map exposure (sum of `secondsAfterRelease`). Bots are grouped as *walking* (quiet, evader03, evader06, staller, edgerunner, doorspammer) or *sprinting* (noisy, shiftholder, closedzone).

## 2. G1 Room to explore: warning-stage-0 share

Fields: `wStage0/1/2`, normalised to on-map samples. Means are over seeds, with min–max in brackets. Stage 2 includes chase time, because there is no stage 3 in the sampler.

| Bot | Tier | n | Exposure s (mean) | Stage 0 | Stage 1 | Stage 2 | Caught |
|---|---|---|---|---|---|---|---|
| quiet | T1 | 3 | 94 | 0.69 (0.47–0.83) | 0.10 | 0.21 | 3/3 |
| quiet | T5 | 3 | 88 | 0.71 (0.48–0.84) | 0.10 | 0.19 | 3/3 |
| evader03 | T1 | 3 | 165 | 0.82 (0.76–0.86) | 0.09 | 0.09 | 2/3 |
| evader03 | T5 | 3 | 125 | 0.71 (0.55–0.81) | 0.12 | 0.16 | 3/3 |
| evader06 | T1 | 3 | 143 | 0.76 (0.69–0.83) | 0.13 | 0.11 | 3/3 |
| evader06 | T5 | 3 | 88 | 0.73 (0.55–0.84) | 0.10 | 0.17 | 3/3 |
| staller | T1 | 3 | 116 | 0.55 (0.29–0.70) | 0.20 | 0.25 | 2/3 |
| staller | T5 | 3 | 115 | 0.55 (0.30–0.70) | 0.20 | 0.25 | 2/3 |
| edgerunner | T1 | 3 | 213 | 0.90 (0.80–1.00) | 0.08 | 0.02 | 1/3 |
| edgerunner | T5 | 3 | 214 | 0.86 (0.78–1.00) | 0.13 | 0.02 | 1/3 |
| doorspammer | T1 | 2 | 92 | 0.82 (0.74–0.91) | 0.09 | 0.08 | 2/2 |
| doorspammer | T5 | 2 | 14 | 0.33 (0.21–0.45) | 0.31 | 0.36 | 2/2 |
| **noisy** (sprint + shut doors) | T1 | 3 | 74 | **0.74 (0.62–0.93)** | 0.09 | 0.17 | 3/3 |
| **noisy** (sprint + shut doors) | T5 | 3 | 51 | **0.73 (0.57–0.93)** | 0.13 | 0.15 | 3/3 |
| shiftholder | T1 | 3 | 55 | 0.53 (0.00–0.88) | 0.38 | 0.08 | 3/3 |
| shiftholder | T5 | 3 | 47 | 0.49 (0.00–0.89) | 0.39 | 0.12 | 3/3 |
| closedzone (sprint by start) | T1 | 3 | 14 | 0.29 (0.07–0.56) | 0.21 | 0.50 | 3/3 |
| closedzone (sprint by start) | T5 | 3 | 13 | 0.30 (0.07–0.57) | 0.22 | 0.49 | 3/3 |

Pooled over all samples (weighted by `onMapSamples`):

| Group | T1 stage 0 | T5 stage 0 | n |
|---|---|---|---|
| walking | 0.81 | 0.78 | 17 each |
| sprinting | 0.64 | 0.60 | 9 each |

- With v1 movement, the noisy bot's stage-0 share is already above the G1 floors (60 % at T1, 35 % at T5).
- **Caveat:** survival censoring. Every noisy run ended in a catch at 37–128 s, so each run is short and ends on an approach. The v1 Relay is also always on the map, and v2 has an Away state.
- **Caveat:** distances are measured cell to cell, about ±3–4 m. The path field lets the route cross an intact window for +9 m, which the Relay cannot do. `fUnreachedShare` is 0.07–0.54 (falls into stage 0).
- Below 35 % are closedzone (start-zone sprint, about 14 s runs) and doorspammer T5 (n = 2, about 14 s). Both are short-run artefacts, not stable findings.

## 3. G2 Cause: why the Relay came

**Real hunts (corrected).** The reports say 110. The real number is 62. 48 of the reported hunts were door-break resumes, not new hunts.

| Cause of a new hunt | Count | Note |
|---|---|---|
| door noise heard | 32 | `huntsByCause.door` |
| sprint noise heard | 21 | Inferred from `other`; all of them in sprinting bots; not logged |
| re-hunt after a lost chase | 9 | `chasesLost` |
| glass | 0 | Never tested |

**How each chase started** (`chaseLog.from`, 54 chases in 52 runs):

| Chase started from | Walking (31) | Sprinting (23) | Total |
|---|---|---|---|
| Hunt or Search (a call came first) | 15 | 23 | 38 (70 %) |
| Wander or Listen (no call at all) | 16 | 0 | **16 (30 %)** |

- For walking players, half the chases (16/31) have no cause the player could name. The Relay wandered into its 12 m, 360° sight.
- Door noise reached the Relay less often at T1 than at T5. This counts quiet, evaders and edgerunner only (`noiseHeard.door` / `noiseEmitted.door`):

| Tier | Door noises heard | Out of range |
|---|---|---|
| T1 | 14/83 (17 %) | 58/83 |
| T5 | 23/65 (35 %) | 37/65 |

This fits T5's hearing × 1.6. The T1 runs also climb tiers, so the contrast is diluted.

## 4. G3 Warnings before chases

**Not measurable from these reports.** The ladder is sampled every 0.25 s, but no sample is tied to a chase start. So "≥ 3 s at stage ≥ 1 and ≥ 1 s at stage 2 before each lock-on" cannot be checked.

What is known (`chaseLog.straightAtStart`):

| Bots | Median start distance | Range |
|---|---|---|
| walking | 5.9 m | 3.7–11.3 m |
| sprinting | 7.7 m | 2.2–12.0 m |

v1 has no gate before a chase.

## 5. G5 Escape rate (evader bots)

Only chases where the bot actually began evading and the chase lasted more than 0.6 s are counted. Escaped means the chase ended `lost` (no chase ended `relayed`).

| Bot | Tier | Evaded chases > 0.6 s | Escaped | Rate | G5 target (at 0.6 s) |
|---|---|---|---|---|---|
| evader03 | T1 | 3 | 2 | 67 % | – |
| evader03 | T5 | 3 | 1 | 33 % | – |
| evader06 | T1 | 3 | 1 | **33 %** | ≥ 50 % |
| evader06 | T5 | 3 | 0 | **0 %** | ≥ 35 % |

- The quiet bot meets the same first chases and is caught 6/6 times. Evading changes the outcome, but rarely.
- **Caveat:** these are v1 rules, not G5 conditions: 5 s stamina (G5 uses 2 s), chase 4.2 m/s at T1 and about 5.4 m/s at T5 (v2 caps at 4.4), and no corner or door rule.
- **Caveat:** n = 3 per cell. Some chase events are shared between evader03 and evader06 (for example 2554 T1 at 11.9 s).
- Three evader chases were caught in about 0.5 s (`seconds` 0.52–0.55), before or just after the reaction time.

## 6. G8 Quiet player

| Quiet bot | n | Caught | First chase s (`firstChaseAfterRelease`) | Stage ≥ 1 share of on-map time | Chases per 10 min |
|---|---|---|---|---|---|
| T1 | 3 | 3/3 | 11.9, 74.3, 187.4 | 0.17–0.53 | 6.4 |
| T5 | 3 | 3/3 | 10.6, 46.4, 195.5 | 0.16–0.52 | 6.8 |

Chases per 10 min are pooled: chases ÷ `secondsAfterRelease`.

- The v1 Relay does not leave a quiet player alone. It finds them through wandering and re-arrivals, and catches them.
- G8 counts stage-1 *entries* per 10 min. The reports do not log entries, so the G8 number itself is unmeasured.

## 7. Catches

- **Runs:** 45 of 52 runs ended in a catch (47/54 including the excluded pair).
- **Chases:** 45 of 54 chases ended `caught`, 9 `lost` and 0 `relayed`.

The 7 runs that ran to the time limit:

| Run | Tiers |
|---|---|
| 2554-evader03 | T1 |
| 7-staller | T1, T5 |
| 2554-edgerunner | T1, T5 |
| 20388-edgerunner | T1, T5 |

Time from release to the catch (caught runs only):

| Bot | T1 mean (range) | T5 mean (range) |
|---|---|---|
| quiet | 94 s (15–188) | 88 s (15–201) |
| noisy | 74 s (37–128) | 51 s (37–62) |
| shiftholder | 56 s (18–115) | 47 s (18–89) |
| closedzone | 14 s (10–20) | 13 s (10–20) |

The high catch rate comes from the game rules: 360° sight at 12 m, chase 4.2 or 5.4 m/s against a 3.2 m/s walk, and bots that do not flee. It describes the bots, not players.

## 8. Leash re-arrivals (A7a / A7b)

Fields: `relayLog`, cell-to-cell distances. These counts and distances are trusted.

| Kind | n | Straight distance before (mean) | Walk after (mean, range) | Walk after < 42 m | Walk after ≤ 30 m |
|---|---|---|---|---|---|
| A7a (45 s quiet / 30-cell leash) | 39 | 61.7 m | 36.1 m (21.7–48.0) | 26 | 11 |
| A7b (unbuilt cell) | 47 | 58.6 m | 41.5 m (27.0–69.0) | 25 | 6 |

Re-arrival rate, pooled per 10 min of exposure:

| Group | T1 | T5 |
|---|---|---|
| walking | 10.6 | 13.4 |
| sprinting | 1.4 | 0 |
| quiet bot only | 19.1 | 20.5 |

- With the sprinting bots, the Relay hunts 83–93 % of the time and never leashes.
- **51 of 86 re-arrivals (59 %) land closer than 42 m by walking.** That would break the v2 Arrive rule W ≥ 42 m, since W ≤ F.
- **17 of 86 (20 %) land at ≤ 30 m.** The player would be at stage 1 the moment it appears.
- First release (`releaseWalkMetres`): 27.0–66.7 m walking (mean 41.0). 30 of 54 releases are under 42 m.

## 9. Door breaks

Fields: `doorsBroken`, `doorsBrokenByState`. These are trusted, and every break attempt succeeded.

| Group | Tier | Doors broken | During Hunt | During Chase | Per 10 min |
|---|---|---|---|---|---|
| walking | T1 | 10 | 7 | 3 | 2.5 |
| walking | T5 | 19 | 16 | 3 | 5.9 |
| sprinting | T1 | 18 | 16 | 2 | 25.1 |
| sprinting | T5 | 10 | 9 | 1 | 18.0 |

The totals do not compare cleanly across tiers, because T5 sprint runs were shorter. Across all runs: 57 doors broken, 48 during Hunt and 9 during Chase.

## 10. Tiers

- The tier changes only chase speed, hearing, door-break time and search time. Silent bots therefore give identical T1/T5 pairs: staller, closedzone up to the first chase, and the edgerunner 2554/20388 pairs. That is expected, not a bug.
- Any T1/T5 difference is diluted, because T1 climbs. For example, 7-quiet/evader-T1 reach tier 5 at 151.9 s of play, and 7-noisy-T1 reaches it at 128.5 s.

## 11. Untested by this baseline

- Glass (window smashing).
- A sealed zone.
- Spamming a single door.
- Sprint as a logged cause.
- The G3 gate.
- Stage-1 entry counts (G8).
- Frame time.

## 12. What this means for v2

1. **Diagnosis.** The problem is sudden, lethal contact, not a Relay that is always close. A quiet walking player sits at warning stage 0 for about 70–80 % of on-map time even with v1. Yet the quiet bot meets 6.4 chases per 10 min. Chases start at a median of 5.9 m, and 45 of 54 end in a catch. The feeling of "always running" comes from contact that has no warning and no cause, plus a chase you can rarely escape. v2 aims at exactly this: the G3 gate (every lock-on comes after warnings), cone sight with a spotting meter (no instant 360° contact at 6 m), Away/RELAX (real time off), and the escape rules (0.7 s hold, 2.0 s lose, 4.4 m/s cap).
2. **G2, cause.** 16 of 31 walking chases start from Wander or Listen, with no call. For a quiet player, v2's director and Away must bring this close to 0.
3. **Re-arrivals.** 59 % of v1 re-arrivals land under 42 m by walking, and 20 % at 30 m or less. v2's Arrive rule (W ≥ 42 m, §5) therefore has teeth. Step 2 must check that the built area at buildRadius 2 always offers a candidate at W ≥ 42 m. If not, the Relay stays Away longer; it never places closer.
4. **Doors.** 57 doors were broken and every attempt succeeded; 48 of them were during Hunt. The landed fix already stops breaks in Wander and Search. v2's licence rule (BREAK only with a perception licence, otherwise a WALL) handles the Hunt breaks.
5. **G5, escape.** With a 0.6 s reaction, the evader escaped 1 of 3 chases at T1 and 0 of 3 at T5. That confirms the T5 hopelessness the audit predicted. The fixed hold, the 2.0 s lose threshold and the 4.4 m/s cap are needed. Re-measure with at least 10 evaded chases per tier.
6. **The bands.** The v2 bands, applied to v1 movement, already leave stage 0 at 0.81 (walking) and 0.64 (sprinting) at T1. So 30 / 18 m are not so wide that the warning is always on.
7. **Not yet measured:** glass, sealed zones, single-door spam, sprint as a logged cause, the G3 gate, and G8 stage-1 entries. The fixes below must land before the same bots can grade v2.

## 13. Measurement fixes before the bots grade v2

关卡设计 owns the bots (`FrontRooms3DGame.Baseline.cs`, `Tools/relay_baseline_*.sh`). The list was sent to them on 2026-10-07. Re-run the matrix in a private clone, not on main while Red's editor is open.

1. Provenance: build from a tagged commit. Write the git SHA and a dirty flag into every JSON. Keep the run logs. Run one case twice and compare behaviourHash. Rerun all 54 cases on the same build, edgerunner included.
2. Sprint noise: route every sprint footstep through AutopilotNoise. Fail the run if a Hunt has cause Unknown or if a sprinting bot ends with bot.sprintNoises = 0. Check with a shiftholder rerun first.
3. Hunt counting: do not count the return from BreakDoor to Hunt as a new hunt. Log noise-driven hunts and re-hunts after LoseTrack separately. Keep a hunt log (time, cause, heard distance).
4. Call log instead of callsByCause: record every call with time, cause, heard / out of range / ignored, and the state it led to. Link each chase to the call that led to it, and count only noises the Relay actually heard. A wander encounter is a chase from Wander or Listen with no call active.
5. G3 per lock-on: at each Chase or TargetAcquired, write the current stage and how long stage >= 1 and stage 2 have each been on continuously. Also log the ladder state at every Arrive and re-arrive (W, F, stage).
6. G8 and G1 counters: count stage-1 entries per 10 min. Report stage shares both whole and without the last few seconds before a catch (survival censoring). Add v2 Away time. Drop dormantShare and wStage0OfRun.
7. Noisy bot glass: route the bot to the nearest reachable intact window by walking path, not only one on its current cell's edge. Count glassBroken from broken panes. Fail the run if windowsBroken = 0.
8. Closedzone: choose a zone with at least 2 open border doors, or open them first. Fail the run if doorsShut = 0.
9. Doorspammer: stop the 20 s clock while the bot stands at its door toggling it. Keep its route out of the start-door cell. Pick a reachable door. Fail the run on fewer than N toggles per minute, or if it touches more than one door.
10. Edgerunner: check that edgeStalls > 0 or that the unbuilt-cell leash fired because of the bot's position. Otherwise do not attribute any A7b to it.
11. G5 conditions: run the evaders with the v2 2 s stamina bar, the 4.4 m/s chase cap and the 0.6 s reaction. Count only chases where evasion began. Log whether sight was broken, at which corner or door, and the outcome. Use enough seeds for at least 10 independent evaded chases per tier.
12. Tiers: add a tier-lock option so T1 runs stay at T1, or report metrics split by time at each tier. Start the run budget at release so exposure is equal.
13. Ladder field: do not let an intact window count as passable (or report it separately). Note that distances are cell to cell (about ±3–4 m).
14. Fold mode-fidelity checks into the PASS verdict. Turn off screenshots in baseline runs (or skip their frames). Remove frame-time and max/p99 from the baseline, keeping relayTick and pathField timings only. Fix the sprint-burst counter (or drop it).
15. Use more seeds: at least 6 per mode and tier for G1 and G5. n = 3 cannot separate T1 from T5 or 0.3 s from 0.6 s.
