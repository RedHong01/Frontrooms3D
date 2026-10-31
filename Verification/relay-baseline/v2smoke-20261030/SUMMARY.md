# Relay v2 define-on smoke, 2026-10-30

Level-design research (关卡设计调研, account A). 关卡设计 gave the HR16 go at 13:2x; catch distance stays at 0.7 m. This is a smoke run: one seed (2554), 8 runs. It shows how the build behaves; it does not grade the v2 gates (§14). Grading them needs more seeds.

## Setup

- Clone `~/FrontRoomsResearchWork/proj66v2`, a copy of main at 13:5x: A40 onboarding, V3a, V3b (Game 15776a87), with `FRONTROOMS_RELAY_V2` in Standalone's defines. Main is never built with the define.
- Every run uses `-autopilotExit`, so floor 1 has its station and onboarding gate.
- Harness r3.1:
  - the verdict counts a Relay released at least once (in v2, `Released` means on the map);
  - state seconds include Away and Withdraw;
  - the `director` block is read from `FrontRooms3DGame.RelayDirector`.
- An r1 of the same 8 runs, before V3b and the verdict fix, gave the same behaviour. Its 4 "FAIL" verdicts were the harness bug, and its JSON were superseded.

## Results (8/8 PASS)

| Run | First call | Visit before catch / Away | Chase | Notes |
|---|---|---|---|---|
| noisy T1 | Device (glass) | 6 s on the map | at 4.4 s, 7.5 m, caught in 1.7 s | lock-on after 3.7 s stage ≥ 1 and 2.1 s stage 2 (hunter) |
| noisy T5 | Device | 7 s | at 4.4 s, 9.9 m, caught in 2.5 s | **lock-on after 2.92 s stage ≥ 1 (G3 asks ≥ 3 s)** and 1.4 s stage 2 |
| evaderv2 T1 | Gate (onboarding) | ~10 s, then Away 231 s (ExitReached), BuildUp to the end | none | no second call in 4 min |
| evaderv2 T5 | Gate | 214 samples on the map | at 50.1 s, 5.8 m, evaded, caught in 3.3 s | G5 0 of 1; the chase cap of 4.4 m/s was held |
| walkaway T1 | Gate | ~10 s, then Away 231 s | none | stage-1 share 68 % of a 9.5 s visit (the G row asks ≤ 40 %; too short to mean anything) |
| closedzone T1 | Gate (Fallback 180 s) | 59 samples | at 14.5 s, 2.1 m, caught in 0.3 s | 0 s PENDING (row: none > 10 s); BreakDoor 3 s |
| relaxsprinter T1 | Gate | ~10 s, then Away 230 s | none | **58.8 s of sprint in RELAX: Attention +0.00, 0 RELAX calls (RELAX gains 0 holds)** |
| quiet T1, 10 min | Gate, then Sweep | Away 340 s, then the Sweep | at 359.8 s, 7.5 m, caught in 1.2 s | **G8: 2 stage-1 entries, 3.3 per 10 min (≥ 1 at T1)**; the Sweep came after RELAX 100 s + dry spell 240 s |

Across all runs:
- Drops 0, FailedTries 0, PendingSecondsTotal 0.
- Withdraw at most 6.2 s; G6 asks Withdraw → Away ≤ 25 s.
- Arms 1 per run. RELAX Attention gain 0.00 everywhere: noisy sprinted 17.6 s in RELAX, closedzone 5.7 s.

## What it says

- **RELAX gains 0, the closed-zone PENDING row, Withdraw time and G8 hold** on this seed. G8 needs a 10-minute run at T1, because the first Sweep comes only after RELAX plus the 240 s dry spell.
- **Every chase that started was caught: 5 of 5.** They started 2.1–9.9 m away and were over in 0.3–3.3 s, including evaderv2's at T5 under the 4.4 m/s cap. G5 (≥ 50 % escapes at T1, ≥ 35 % at T5) looks out of reach in V3a. This is data for V4b (sight, lock-on, chase).
- **G3 is marginal at T5:** one lock-on came after 2.92 s of stage ≥ 1 by the hunter's own WarnStage (2.73 s by the §9.1 sampler). Recheck it when V4b's lock-on lands.
- **A quiet or walk-away player gets one visit, then 4+ minutes of nothing.** After the first send's visit the Relay withdraws, and walking adds no Attention (A36: walk 0), so only the Sweep brings it back. That pacing is by design (§6), but the walk-away G row cannot be measured on a single visit: it needs runs of 10 minutes or more.
