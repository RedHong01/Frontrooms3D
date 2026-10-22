# NOTE 2026-10-22 13:3x: the map owner's requirements for the kept-progress hunk (from 关卡设计, via the 游戏视觉 session)

Every stage of `glass-debris-progress` must follow this when it writes or checks `CONTRACT_map.diff`.

- **Base:** main `FrontRoomsMapWorld.cs` sha1 **7ef0ca45**. 关卡设计 is not editing Hold / ReleaseHold / PauseHold / StageFloor / GlassBreakRecord. Its three pending MapWorld diffs leave those lines alone: C22 (proj3), M2 lamps (proj4), A12 (proj_a12). Keep the hunk inside those functions and the record so it 3-way merges cleanly.
- **Where progress is written:** write the progress into `glassBreaks` (the GlassBreakRecord) when a hold is **released or paused**, and when the window is **torn down (WindowReleased)**. Never on every Hold tick.
- **ReleaseHold callers** (all four change behaviour with no edit at the call site; check each still makes sense):
  - `FrontRooms3DGame.cs` l.1525: the aim leaves the pane;
  - `FrontRooms3DGame.cs` l.1596: E let go;
  - `MapWalker` l.155 and l.174.
- **`GlassHoldReleased` keeps firing exactly once per release**, as now (声音设计 listens to it).
- **Tests:** put the new expectations in the diff's test hunk, in `FrontRoomsMapInteractionTests.cs`:
  - ~l.2008–2014, "a release drops to 0.35": now a release must KEEP the progress;
  - ~l.2018–2025, "a rebuild … resuming from 0.70": after a hold to 0.75, a rebuild must restore **0.75**, not Crack2.
- **Landing:** 关卡设计 lands the map hunk after the Alpha, in its queue M2 → A12 → this glass hunk → sound → C22. The visual package must be safe to land before it.
