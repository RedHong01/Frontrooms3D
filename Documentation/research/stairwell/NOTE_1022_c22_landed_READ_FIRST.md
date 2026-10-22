# NOTE 2026-10-22 15:4x · C22 is LANDED on main (read before you build or package stairwell A)

From 关卡设计 to the 游戏视觉 session, about 15:4x. Written here for the running stairwell-a-production workflow (wf_52cdacc1-46a), because running workflow stages cannot get messages.

- **C22 LANDED on main at 15:39.** New bases:
  - FrontRoomsMapWorld.cs **10f71ffc**
  - FrontRooms3DGame.cs **670219ab**
  - FrontRoomsMapHunter.cs **d4bace11**
  - FrontRoomsWarnLamps.cs **28dad4ed**
  - **new** FrontRoomsStairwell.cs **ceafb945**
  - **new** FrontRoomsExitPlanner.cs **5fe3b3fd**
- **Live now:**
  - `ExitDoorBuilt(door, hinge, leaf rig, frame)` fires on every build of the EXIT edge, per 10_directions §7. The crossbar is named "EXIT crossbar". ExitLocked resets without an event.
  - The LIT-locked EXIT sign sits on the frame's exit_sign_p anchor.
  - The stairwell itself has plain materials, waiting for our restyle.
- **So:** make the clone from main (no copying from proj3) and package without a C22 HOLD. Map files stay 关卡设计's (contract diffs only).
- **Known open items that are ours (关卡设计's list):**
  1. Map lamps light the stairwell's inside through its walls until the blackout, because they are unshadowed. This needs our rendering-layer separation (the same idea as the station's light layer, `research/station/50_critic_fixes.md` §4 item 3). Solve it for the stairwell if it is visual-side; otherwise write the exact contract.
  2. EXAMINE's CellLightAt does not know the stairwell lamp (EXAMINE is ours: Rendering/Inspect).
  3. X gets the Level 0 reflection. Give the stairwell its own reflection (zone cube / probe) on the visual side.
- Next from 关卡设计: V1 `_FR_LampDip` (built and green; lands next), then the VL217/M1 UI batch.
