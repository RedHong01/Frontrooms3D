# NOTE 2026-10-22 15:19 · 关卡设计 is landing CONTRACT_map.diff now (read before you touch the map contract)

From 关卡设计 (account A, `local_91e132eb…`) to the 游戏视觉 session, about 15:2x. Written here for the running glass-debris-progress workflow (wf_614f2c13-e72), because running workflow stages cannot get messages.

- 关卡设计 is landing **this folder's current `CONTRACT_map.diff` (sha1 7b404ddd, 13:43)** onto main now. It goes 3-way onto A12 with 0 conflicts and is under test in their clone with the WarnBurst hook, C10 and relay_park. They also rewrite the stale comment in Game, "Every pane not aimed at has no progress".
- **If this workflow produces a revision of the map contract (rev 3), write it as a DELTA against main AFTER 关卡设计's "GD3c … LANDED" line in `Documentation/Takeover_2026-10-18/LANDINGS.md`.** Do not write it against MapWorld 7ef0ca45 or the 13:43 diff. Name it `CONTRACT_map_rev3_delta.diff` and leave `CONTRACT_map.diff` as it is.
- **Bases move:**
  - A12 landed 15:11: FrontRoomsMapWorld.cs 6041880b, FrontRooms3DGame.cs 3b8f1cfd, FrontRoomsDoorShot.cs b2014396, FrontRoomsMapHunter.cs 080fe83f, FrontRoomsKitSeam.cs dba67c53.
  - MapWorld and Game move again with GD3c, the WarnBurst hook and C22 in the next hour.
  - Package the visual half against main at package time. Re-check every base by sha1 and read LANDINGS first.
- Test the visual half against main WITH the landed GD3c map hunk once it is in (sync your clone's map files from main). Until then, keep using the contract applied in your clone.

## Update 15:25: GD3c and the WarnBurst hook are LANDED on main

From 关卡设计 at about 15:2x (see LANDINGS.md):
- **GD3c** is in FrontRoomsMapWorld.cs **9b82c286**: GlassBreakRecord.hold + KeepHold, no ReleaseHold StageFloor drop, a rebuild resumes at max(stage floor, kept), and the first-strike impact. FrontRoomsMapInteractionTests.cs is **8f302e14** (our T1–T3 + header). In Game, only the stale hold-bar comment changed.
- **The WarnBurst hook** came after it: FrontRooms3DGame.cs **380ff7d4**. C10 + relay_park touched sound files only.
- **Current bases:** MapWorld 9b82c286, Game 380ff7d4, DoorShot b2014396, MapHunter 080fe83f. Any map rev 3 delta goes against these. **C22 is next** and moves MapWorld and Game again; read LANDINGS.md before you package.
