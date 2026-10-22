# NOTE 2026-10-22 15:3x · placard packs: the Floor 1 sheets are NOT final (read before packing)

From 平面视觉 (`local_658709b9…`) to the 游戏视觉 session, about 15:3x. Written here for the running prebuild-integrations placard stage (wf_f742b0e7-d77), because running workflow stages cannot get messages.

- Red picked first-room option 1 (15:24): Floor 1's station is the first room. So the **Floor 1 placard changes**: YOU ARE HERE goes at the desk, the route to STAIR A · EXIT becomes short, and the placard is mounted inside the room, by the desk.
- **`sheetA/` and `sheetA_phase0/` (the Floor 1 route) are NOT final.** 平面视觉 redraws both within about an hour of the first room's geometry being picked (desk, mount wall, start door and stair door, in cells). Do not pack them as final.
  - Build the packing pipeline and test it on them if useful, but mark those two packages **PLACEHOLDER ART**.
  - Each gets its own HOLD file that names this note.
- **`sheetA_B1/` and `sheetA_phase0_B1/` are unchanged.** They can be packed as final and land once the stairwell is live.
- Unchanged: the glow (k 0.158; walls never glow) and line 4.
- 平面视觉 checks the in-game render with `~/FrontRoomsVisualWork/placard_tutorial/ingame_check.py`. Run it on your renders and report its result.
- Mount constraints for the new Floor 1 placard are in `research/station/NOTE_1022_first_room_placard.md`. They go to the station-first-room run, not to this stage.

## CORRECTION 15:4x from 平面视觉 (this overrides the Phase 0 line above)

- **`sheetA_phase0/` is FINAL as the interim Floor 1 sheet. Pack it as final**: print 9d52bba0, glow 0f42609b, examine.json fe327e70 (the placard_q16 transcript comes from it).
  - It is a texture swap only (D17) into the existing start-door mount (FrontRoomsPlacardMount).
  - **Landing condition: in the same landing as 关卡设计's G1.** Phase 0 draws YOU ARE HERE west of the start door, but today FrontRoomsPlacardMount picks west or east per seed. Without G1, the dot is on the wrong side on the east seeds.
  - So the package keeps a HOLD file naming G1. If G1 is not in main, the v3 placard stays.
  - Lifetime: when A26's first room (the M2 mount by the desk) lands, Floor 1 switches to the sheet 平面视觉 redraws from the first-room geometry.
- **`sheetA/` (the full sheet with the station and the padlock) is still NOT final**: it waits for the redraw (PLACEHOLDER, HOLD).
- **`sheetA_B1/` and `sheetA_phase0_B1/` can go now, since C22 is in main (15:39).** Find out how a B1 floor's placard is chosen today: FrontRoomsPlacardMount, NextFloor and the floor label. Then pack them so B1+ floors show them.
  - If that needs a floor-aware switch in a visual-owned file, build it.
  - If it needs a map-owned file, write a contract diff for 关卡设计.
- **Check:** render in game the EXAMINE close-up, the lit view and a dark cell for the glow. Run `~/FrontRoomsVisualWork/placard_tutorial/ingame_check.py` (TP06) and report its result. The 游戏视觉 session sends the renders to 平面视觉 and logs the VL (VL386–VL387).

## Update 15:5x: G1 timing, and how B1+ sheets are selected (关卡设计 + 游戏视觉)

- **G1 is not in C22.** It is under test in 关卡设计's proj2 and lands in about 15 minutes; its LANDINGS line starts with "G1". The Phase 0 swap lands right after it. G1 makes MapRootFor / RootFor prefer a door cell whose west edge is Open, and the verification fails any seed without one; on those seeds FrontRoomsPlacard's "west first if Open" rule always picks P1 west.
- **B1+ sheets: nothing selects them today.** FrontRoomsPlacardMount listens only to MapRunStarted and mounts at the title's start door (Floor 1).
- **Decided (游戏视觉, 15:5x):** on floors 2+, the sheet hangs on the MAP SIDE beside the floor's start door (the stairwell's north door), west first, like P1. It is not inside the stairwell.
- **How to wire it:**
  - Subscribe to `FrontRooms3DGame.FloorChanged(map, relay, floor)` (static, raised after NextFloor) and pick the sheet by `FrontRooms3DGame.FloorLabel(floor)` (LEVEL B1…).
  - Read the door from `FrontRoomsStairwell.NorthDoorCell / NorthDoorPoint`, through `public FrontRoomsStairwell StartStairwell`, which 关卡设计 adds in its next Game landing. Do not derive it from ExitCell.
  - 关卡设计 is asked to add the Open-west rule to SeedFor as well.
- **Ownership:** check who owns FrontRoomsPlacardMount. If it is map-owned, write a contract diff; if it is visual-owned, put the change in the placard package.
- The B1 package keeps a HOLD until StartStairwell and the SeedFor rule are in main. Until then floors 2+ show no placard, as today.
