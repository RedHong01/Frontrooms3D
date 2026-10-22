# CONTRACT_map.diff: kept glass progress (D-c), for 关卡设计

- **From:** the 游戏视觉 workflow `glass-debris-progress`, 2026-10-22.
- **Owner of the files:** the map line. 游戏视觉 changed them only in its clone `W/proj_gd3c`.
- **Why:** Red, 2026-10-22 13:0x (游戏视觉 chat): "当前玩家如果敲玻璃，如果比如说没有长按足够多的秒数，玻璃会保留被敲碎的进度，而不是恢复成普通的玻璃。" A hold that ends early keeps its progress; the pane does not go back to plain glass.
- **Approvals:** this message is not Red's approval of anything. Red's words above are the request. The map line decides how and when to land it. Per the map line's own queue, that is after the Alpha.

## Bases and checks

| File | Base sha1 | Where |
|---|---|---|
| `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs` | `7ef0ca45` | main `2c450e11` (the map line's recorded base) |
| `Assets/Editor/FrontRoomsMap/FrontRoomsMapInteractionTests.cs` | `d9b51f23` | main `2c450e11` |

- `git apply --check CONTRACT_map.diff` passes on main `2c450e11` (run read-only from `Frontrooms3D/`).
- **Compile:** `W/tools/roslyn_check.py` gives 0 errors in Assembly-CSharp and Assembly-CSharp-Editor. A Unity batch compile of the clone also succeeded.
- **The map's own suite in the clone:** `FrontRoomsMapInteractionTests.RunBatch` gives **176 passed, 0 failed**, for rev 1 (13:31) and again for rev 2 (13:42, UNDER LOAD 269–289; `data/kept_map_interaction_tests.txt`).
- **Play (the game's own E path, harness `FrontRoomsGd3cRepro`):** see `10_design.md` §1, VL360 and VL362. Kept holds: 0.200 / 0.500 / 0.800; three 0.20 s holds reach S1; 0.50 + 0.30 s reaches S2; a chunk rebuild restores every hold.

## The hunks, and why each one exists

| # | File, base line | Change | Why |
|---|---|---|---|
| C1 | MapWorld 354–362 | `GlassBreakRecord` gets `public float hold;` (seconds of E held, the same unit as `Window.hold`) | The `Window` object is new after every chunk rebuild, so progress that lives only on `Window.hold` is lost when the chunk drops. The record (kept by edge) is where the map already keeps what survives a drop. The visual side reads it too: the kept-progress wipe and pattern arming (`10_design.md` §5, §6.4) |
| C2 | MapWorld, a new `KeepHold(Window)` next to `StageFloor` (base 3111), called from `ReleaseHold`, `PauseHold` and `RaiseWindowReleased` (base 1553) | Writes `record.hold = window.hold` into `glassBreaks` when it changed, and only then | Stores the progress at the three moments 关卡设计 asked for (note of 13:3x): a release, a pause (tap mode), and the teardown (a chunk dropped mid-hold, and the test's rebuild right after a hold). Never per `Hold` tick. The record exists from `BeginGlassHold` on. While a pane is aimed at without E, `ReleaseHold` runs every frame, but an unchanged hold writes nothing |
| C3 | MapWorld 3209 (`ReleaseHold`) | delete `window.hold = StageFloor(window.stage);` (and call `KeepHold`, C2) | This line is the bug Red saw: a let-go dropped 0.20 → 0, 0.50 → 0.35 and 0.80 → 0.70 (measured). `PauseHold` already kept the progress; now `ReleaseHold` does too. `GlassHoldReleased` still fires once, as before. Note that `ReleaseHold` is also called every frame while a pane is aimed at without E held; with the line gone that call is harmless |
| C4 | MapWorld 1523 (chunk build) | `window.hold = Mathf.Max(StageFloor(window.stage), record.hold);` | A rebuilt pane resumes exactly where it was left, and never below the stage it reached. Records made before C1 carry `hold` 0, so they keep today's behaviour |
| C5 | MapWorld 3125 (`BeginGlassHold`) | `if (record.stage == 0 && record.hold <= 0f)`: the impact is set only by the first strike | Today the impact moves to every new E-down until Crack1, because the step-in moves the eye: measured 1.335 → 1.407 m on the second press. With progress kept, a pane struck once must keep its impact, or the kept progress (and the wipe that shows it) jumps across the pane. Same rule as "fixed once cracked", one strike earlier |
| — | MapWorld comments | `GlassHoldReleased`, `GlassBreakRecord`, `StageFloor` and `BeginGlassHold` doc comments | They described the old rule |
| T1 | Tests 2014–2015 | "a release drops to 0.35" becomes "a release keeps the progress (0.500, kept in the record)" | The old check asserted the old rule |
| T2 | Tests 2022–2024 | "resuming from 0.70" becomes "resuming from the kept progress" (0.767 in the run) | The same |
| T3 | Tests (new, after the tolerance check) | A 0.2 s knock on a third intact pane is kept (0.200, in the record too), and a second strike elsewhere keeps its impact | Covers C3 below Crack1, and C5 |

## 关卡设计's requirements (note of 13:3x in this folder) and how the diff meets them

| Requirement | How |
|---|---|
| Base `7ef0ca45`; stay out of the lines your pending diffs touch | Base confirmed. Tested with `git merge-file`, read-only copies in `W/gd3c/merge_test/`: against C22 (`FrontRoomsMapWork/proj3`, 3880d866), M2 lamps (`FrontRoomsMapWork/proj4`, 3b1f16c7 at 13:4x, ac26369a at 13:3x) and A12 (`FrontRoomsVisualWork/proj_a12`, 38b2f3ec): **0 conflicts** each, and 0 conflicts chained in your landing order (M2 → A12 → this → C22). Besides the record and Hold / ReleaseHold / PauseHold / StageFloor, the diff touches three more places: the rebuild line in the chunk build (base 1523), `BeginGlassHold` (3125) and `RaiseWindowReleased` (1553). None of your three diffs touches them; the nearest change is 12–25 lines away |
| Write the progress on release, pause and teardown, never per Hold tick | C2 |
| Check all four `ReleaseHold` callers | `FrontRooms3DGame.cs` l.1525 (the aim leaves the pane): the pane now keeps its progress when the player looks away, as intended. l.1596 (E let go): Red's case. `FrontRoomsMapWalker` l.155 / l.174 (the test-scene walker: aim leaves / E up): the same semantics as the game. No call site needs an edit |
| `GlassHoldReleased` fires exactly once per release | The line that raises it is unchanged, and `KeepHold` raises nothing. Clone tests: 176/176 |
| Tests in the diff's test hunk | T1 keeps the progress (0.500); T2: after a hold to 0.75 (0.767 with the test's Dt steps), a rebuild restores exactly that, not Crack2; T3 is new |
| The visual package must be safe to land before this hunk | The visual side never names `GlassBreakRecord.hold` at compile time. It reads it through a cached reflection accessor that returns 0 when the field is missing (`10_design.md` §7). The debris world (D-a, D-b) does not need it at all |

## What is deliberately not in the diff

- **`FrontRooms3DGame*.cs`.** `BeginGlass` already passes `window.hold / HoldSeconds` to the shot, and `FrontRoomsGlassShot.Begin` accepts any progress without replaying beats behind it. The mobile glass haptic fires every 0.2 of progress, as before.
  - One comment is now stale (~line 1528, "Every pane not aimed at has no progress"). Change it on your next pass.
- **`FrontRoomsShots/*`.** The beats (0.35 / 0.70 / 1.00, the sound chat's) do not move.
  - A resume a few frames before a beat lands that beat with almost no camera blend. That is open question Q1 in `10_design.md` §10, with options (a) accept, (b) commit the swing, (c) re-wind; it needs Red.
  - Nothing is changed for it here.
- **No new events.** The visual side reads `GlassBreakRecordOf(window).hold`.

## If you land it

1. Dry-run: `git apply --check` from `Frontrooms3D/`.
2. Run `FrontRoomsMapInteractionTests`. Expect 176/176 if nothing else changed in between.
3. Tell 游戏视觉 the landing time. The visual follow-ups (the pattern started on `WindowBuilt` for `hold > 0`, the wipe, arming) read the new field.
   - Until they land, the contract alone already fixes Red's second point: progress is kept and resumes.
   - The only visible effect below 0.35 s comes with the wipe.
