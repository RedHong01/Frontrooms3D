# NOTE 2026-10-22 15:19 · A12 is in main; bases keep moving for the next hour (read before packaging D1.6)

From 关卡设计 to the 游戏视觉 session. Written here for the running D1.6 workflow (wf_0f20a2c5-e42), because running workflow stages cannot get messages.

- **A12 double-acting doors LANDED on main at 15:11.** `FrontRoomsMapWorld.DoubleActingDoors` is live.
- New bases:
  - FrontRoomsMapWorld.cs 6041880b
  - FrontRooms3DGame.cs 3b8f1cfd
  - FrontRoomsDoorShot.cs b2014396
  - FrontRoomsMapHunter.cs 080fe83f
  - FrontRoomsKitSeam.cs dba67c53
- So build against main itself (sync the clone's map files from main). Stop treating A12 as pending: no READY-PENDING-A12 is needed.
- **MapWorld and Game move again within about an hour.** 关卡设计's order is: the GD3c glass map hunk, then the WarnBurst / C10 / relay_park hook, then C22.
  - Package against main as it stands at package time, and record those bases.
  - Keep every map-owned change as a contract diff.
  - The 游戏视觉 session rebases at landing time (`W/tools/rebase_apply.sh d16`), on 关卡设计's final bases from LANDINGS.md.
- Red, 14:59 (his own words in the 总控 chat): "然后目前新的门，和门被破坏等一系列的为什么还没有integrate到主游戏里？…" Doors come first. Land as soon as the work is verified.

## Update 15:25: GD3c and the WarnBurst hook are LANDED on main

From 关卡设计 at about 15:2x (see LANDINGS.md):
- **GD3c** is in FrontRoomsMapWorld.cs **9b82c286**: GlassBreakRecord.hold + KeepHold, no ReleaseHold StageFloor drop, a rebuild resumes at max(stage floor, kept), and the first-strike impact. FrontRoomsMapInteractionTests.cs is **8f302e14** (our T1–T3 + header). In Game, only the stale hold-bar comment changed.
- **The WarnBurst hook** came after it: FrontRooms3DGame.cs **380ff7d4**. C10 + relay_park touched sound files only.
- **Current bases:** MapWorld 9b82c286, Game 380ff7d4, DoorShot b2014396, MapHunter 080fe83f. Any map rev 3 delta goes against these. **C22 is next** and moves MapWorld and Game again; read LANDINGS.md before you package.
