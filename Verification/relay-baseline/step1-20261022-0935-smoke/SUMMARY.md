# Relay baseline smoke · "v1 + path hearing (Step 1)" · 2026-10-22

*关卡设计调研. Red's decision C05 = B ("b", in the 关卡设计调研 chat, 2026-10-22 16:35Z): only the 8-run smoke test now. The full matrix needs a new yes after these results.*

## What ran

- **Build:** main `d4d0e7a5` plus the working tree, 8 files dirty: the W1 lamp bridge (`FrontRoomsWarnLamps.cs`), interaction tests, FMOD banks, and this harness with the rule flags.
  - Hunter: `fb2fe3dd`. That is v1 with path hearing and 关卡设计's §9.1 WarnStage fix.
  - Rule flags: E1, the Floor 1 grace and A12 all report `absent`.
- **Where:** private clone `~/FrontRoomsResearchWork/proj`, one Unity batch job.
- **Wall time:** 3.4 min for all 8 cases; every run ended in a catch.
- **Cases:** shiftholder, noisy, closedzone and doorspammer, seed 2554, at T1 and T5. Tier locked, 4 min budget after release.

## Verdict: the harness is ready for the full matrix

- **8 PASS, 0 FAIL, 0 errors.**
- **Every fidelity check holds:**
  - sprint noise counted (28–161 per run);
  - windows broken (noisy: 11 of 12 at T1, 4 of 4 at T5);
  - zone doors shut (closedzone: opened 2, shut 1, in a 5-door zone);
  - one door toggled at 74–75 a minute (doorspammer);
  - tier held;
  - every hunt has a cause.
- **stagesAgree** (the §9.1 sampler against the hunter's WarnStage): 98.6–100 %. 关卡设计's WarnStage fix matches §9.1.

## Numbers (n = 1 seed per cell: orders of magnitude, not findings)

| Bot | T1: on map → caught | T5: on map → caught | Chase start: W / stage / s at stage ≥ 1 | Doors broken |
|---|---|---|---|---|
| shiftholder | 18.4 s | 18.2 s | 12.0 m / 2 / 16.7 s | 0 |
| noisy | 200.3 s | 81.7 s | 10.2 m / 2 / 3.0 s (T1); 9.0 m / 2 / 2.0 s (T5) | 1 (T1), 3 (T5), all in Hunt |
| closedzone | 117.7 s | 48.4 s | 13.2 m / 2 / 7.9 s (T1); 6.0 m / 2 / 11.2 s (T5) | 3 (T1), all in Hunt |
| doorspammer | 213.0 s | 64.1 s | 12.0 m / 2 / 6.2 s | 0 |

What the numbers show:
- **Every chase came from a call**, a heard noise followed by a Hunt. These are noisy bots; the wander encounters were measured on quiet bots in the Oct 4 set.
- **Every chase ended in a catch**, within 0.4–5.4 s. None of these bots evades.
- **G3:** every lock-on started at stage 2. On noisy T5, stage ≥ 1 had run only 2.0 s, under v2's G3 floor of 3 s. v1 has no G3 gate, so this is the gap v2's gate closes.
- **Arrive:** the first release landed at W = 28.2 m (shiftholder), 39 m and 54 m, so two of three seeds' releases are under v2's W ≥ 42 m. shiftholder is at stage 1 the moment the Relay appears.
- **Door noise:** for doorspammer at T1, 255 of 261 toggles were out of range; it was heard 3 times and caught at 213 s. T5 hears more (×1.6) and caught at 64 s.
- **Door breaks:** all 7 were during Hunt. v2's licence rule (BREAK only with a perception licence) targets these.

## Not measured here

Quiet, the evaders (including evaderv2's G5 conditions), staller and edgerunner, and the other 5 seeds. That is the full matrix:
- 6 seeds × 10 modes × T1/T5 = 120 cases;
- `REPEAT_CHECK=1 Tools/relay_baseline_matrix.sh`;
- about 1–2 h at this run's speed, longer for the modes that survive the whole 4 min.

It needs Red's yes, and it should run before E1, the Floor 1 grace and A12 land. The flags would show if they had.

Unity logs for these 8 runs stay in the clone, at `~/FrontRoomsResearchWork/proj/Verification/relay-baseline/logs/2554-*-T{1,5}-4min.log` (3.5 MB, not committed).
