# Onboarding as a system: Floor 1 without a chase (A40), and where the first encounter goes (系统设计, 2026-10-30)

*Contextual research, as Red asked on 10-30 ("按照现在的进度开始更符合语境的调研"). It starts from what is in main today and from Red's 10-30 decisions, not from scratch. Data only; the builders and Red decide. Sources: `system_design/systems.json` 2026-10-30a (A36, A40, C32, N1, N6, N7), `RELAY_PURSUIT_REDESIGN.md` (decisions block, §4, §12), LANDINGS 10-29 and 10-30, and 关卡设计's V1 bot note (seed 2554).*

## 1. What changed

- **Before (live since 10-29 19:17):** Floor 1's first send came when the player left the station with the floor key (2 cells out), or 150 s after the start door shut. The EXIT is 7–8 cells from the station, so a prompt player was chased on the way out, before knowing the game.
- **A40 (Red, 10-30, being built):** Floor 1 sends no Relay until 150 explored floor-1 cells (outside the station) or 3 min after the start door shut. The key take no longer triggers the first send; a Relay already out is still called to the station (A14, corrected 10-30). The run's first send keeps C32's gentle arrival wherever it happens: on Floor 1 after the threshold, or on B1 if the player leaves Floor 1 first.

## 2. What Floor 1 teaches without a threat

| Teaches | System | Without the Relay? |
|---|---|---|
| Read the plan; the four rules and the locked stair | I1 placard (sheet A, first room) | yes |
| The station TV calls you; the arrows lead to it, then to the EXIT | N6, N3, W6 | yes |
| Locked EXIT → the floor key → the unlock shot | N1, D8, D9 | yes |
| Close-ups pause the world (EXAMINE, READ) | I2 | yes |
| Doors, glass, climbing | D1, D4 | yes, but their noise means nothing yet |
| Slow walk is silent and a walk is heard (A36) | P1 | **no**: without a listener, only the legend teaches it |
| Lamps dip, then steps, then the chime (the warning ladder) | W1, W2, S6 | **no**: needs the Relay |

So Floor 1 now teaches the **space and the tools**. The **threat grammar** is taught by the run's first send, which is gentle by design (C32: unseen at 22–30 m, stage 1 on arrival, so the lamps dip first).

## 3. Two kinds of first run

| Player | What happens on Floor 1 | First encounter |
|---|---|---|
| **Explorer** (wanders, reads, opens doors) | reaches 150 cells at about 2.5–3 min: a quiet bot made 150 cells in 165 s (seed 2554, V1 note), close to the 3 min clock | Floor 1, gentle, about 3 min in |
| **Direct** (plan → arrows → key → EXIT) | leaves Floor 1 in well under a minute (EXIT 7–8 cells from the station) | B1, gentle, on B1's own timing |

For an explorer the two triggers almost coincide, so the "or 3 min" mainly catches a player who stands, reads or idles. For a direct player, **B1 becomes the real tutorial of the Relay**. B1 has no station, no keys and an unlocked EXIT (A35), so the first encounter there is about escaping, not about the key loop.

## 4. Gaps for the A40 build (关卡设计) — proposals, not rulings on Red's behalf

1. **Which clock counts the 3 min?** Close-ups (EXAMINE, READ) and the settings menu pause the world. If the 3 min runs on unscaled time, a player who reads the plan meets the Relay sooner than one who walks. **Proposal: game time**, paused in close-ups and menus, the same clock as the tier and RELAX.
2. **C32's placement is anchored on the station.** It says "on the station's far side from the EXIT, never within 2 cells of the walk back". After A40 the first send can come anywhere: deep in a Tall hall, near the EXIT, or on B1, which has no station. **Proposal:** re-anchor it on the player and the current route target. The Relay arrives on the player's far side from the FLOW target (the station before the take, else the EXIT). It is never within 2 cells of the player's FLOW route to that target. The ladder (22–30 m, then 18–34 m, then ≥ 14 cells, logged) is unchanged. This keeps C32's intent, "never block the way the arrows point", for any position.
3. **B1-first runs.** On today's Relay (v1), when does B1's first send come for a direct player, and does a lamp dip come before any contact? Unknown; measure it (§5). On v2, B1 arms at its start-door shut with a 60 s RELAX (P2), so the first B1 send comes at least 60 s after arrival.
4. **The walk is heard now (A36).** A direct player may never learn SLOW before B1. The first send's lamp dip is the cue to slow down. Check that the stage-1 band (C32's 22–30 m) is outside a walk's 9 m hearing, so the first warning comes before the Relay can hear plain walking. It is: 22 m is far more than 9 m.

## 5. Measurement plan (关卡设计调研, bots; one round, rule 22)

| # | Measure | Bots | Pass / read |
|---|---|---|---|
| OB1 | Time to the A40 threshold on Floor 1 (cells or clock, whichever first) | follower, explorer, quiet; 100 seeds | median and p10 / p90; which trigger fired |
| OB2 | Share of runs whose first send is on B1 | direct (autopilot exit) + follower | expect most direct runs; report the share |
| OB3 | B1 first-send timing and order: lamp dip → steps → contact | direct | the first lamp dip always precedes contact (C32 honesty) |
| OB4 | The 3 min clock pauses in EXAMINE and the settings menu | scripted: read the plan for 60 s | threshold time grows by the close-up time |
| OB5 | Arrival placement after re-anchoring (if adopted) | all | 0 arrivals within 2 cells of the FLOW route; the ladder log counts |

## 6. What the define flip (C02) should also show

Beyond G1–G8 against the Step 0 matrix, with A40 in place:
- **the first encounter:** its floor (1 or B1), the time after arrival, lamp dip before contact in 100 % of runs;
- **no chase during onboarding:** 0 chases on Floor 1 before the threshold;
- **parity (A36):** the same verdicts with desktop, phone and pad gait mixes, because the walk is now heard on all three.
