# NOTE 2026-10-22 15:3x · 关卡设计 (map line): constraints for "the station is its own room, and the first room"

For the `station-first-room` synthesis (游戏视觉 wf_89885526-506). This note gives facts from the map code and my landing plan. It is not a recommendation to Red. Line numbers are main's at 15:30. C22 is not in main yet; its kit lands today.

## 1. How a run starts today

**Floor 1 (the title's floor), in main:**
- The title's Lobby corridor is `FrontRoomsRoomStream`, not map. On Space, the game places the infinite seeded map behind the stream's last door.
- `MapRootFor` (`FrontRooms3DGame.cs:793`) shifts the map root by whole world periods, up to 24 rings, until three things are true:
  - the 5-cell row at the door is Standard Level 0;
  - the cell beyond the door cell is open;
  - from there the walk reaches 150 cells (`LeadsIntoMaze`).
- So the map is never edited to fit the door. A part of the map that already fits is picked instead.
- `SetStartArea(rect, doorCell)` (`FrontRoomsMapWorld.cs:805`) reserves the 5 × n strip behind the door:
  - the map builds nothing there and walls it on three sides;
  - `IsBuilt` and `PassageBetween` treat it as no map, so the Relay and the autopilot never route into it.
- The door rules (`UpdateStartRooms`, `FrontRooms3DGame.cs:877`):
  - the door holds until the map around it is built (`ReadyAround`, at most 3 s), then opens;
  - it shuts for good once the player is 4 m past it (`StartDoorShutDistance`);
  - the Relay's release gate waits for that shut;
  - the stream lamps fade, the stream rooms are disposed one a frame, and `ClearStartArea` hands the ground back.

**Floors 2+ (C22, landing today):**
- The start area is X, the 1 × 1 stairwell cell. It is a non-map shell, `FrontRoomsStairwell`, under the game root.
- Its north door opens onto X + (0, 1). `FrontRoomsExitPlanner.SeedFor` searches up to 256 floor seeds for one where that cell is Standard Level 0 and leads into the maze.
- The door rules are the same: hold ≤ 3 s, open, shut and lock 4 m behind the player, then the Relay's release delay.
- C22 already proves the pattern of a room with its own walls that is not map: a shell inset ≥ WallHalf + 0.01 m inside reserved cells, its own lamp, and a door into the map.

**The station today:** not built. The M4 plan (answered to 叙事设计 10-22):
- one station module per floor, stamped by the generator (`RoomModuleStamp`);
- 2 × 2 to 4 × 4 cells, inside one chunk (8 × 8 cells, 24 m), never Tall;
- at least 2 openings on different sides into walkable cells;
- a new `StationBeacon` marker for the TV, and the station key on the module's KeySpot;
- modules author no doors.

## 2. Constraints whatever Red picks

1. **A start area is a Relay-free zone.** No map cells, no nav, no hearing into it.
   - If the station is the start area, it is a safe room: the Relay can never come in, hear the TV in it, or answer the key-take inside it.
   - The tutorial's M6 beat (StationKeyTaken → Noise → Hunt) would then start only once the player leaves.
   - If the station must be able to host a Relay later (a revisit, the floor's last room), it has to be map, not start area.
2. **Map walls can already differ per face.** Walls take each side's material from that side's zone theme (`Theme(zone.theme)` at `FrontRoomsMapWorld.cs:1227–1388`, `eastA/eastB`), so a border wall is two-faced.
   - There are only two themes, `ZoneTheme { Level0, Office }` (`FrontRoomsMap.cs:16`).
   - Giving the station its own walls as map is one of two kinds of change:
   - **(W1) A per-cell skin override** for the stamped station's cells: a station surface set read by the wall, floor, ceiling and lamp-colour lookups, while the zone stays Level 0.
     - The changes are mine (MapWorld) plus 游戏视觉's new `Resources/Surfaces` set.
     - Nothing else that reads `ZoneTheme` changes: sound, wall fixtures, transitions, RT verify.
     - My preference.
   - **(W2) A third ZoneTheme.** Around 20 files read `ZoneTheme` across 4 owners (map, sound, 游戏视觉's transitions/fixtures/RT, the Level Designer). Larger, and it changes theme rolls for every seed.
   - **(W3) A non-map shell like the stairwell.** This works only for a start area (§2.1), and costs a new builder per room size.
3. **Light through walls.** The map's lamps cast no shadows. A Level 0 lamp next to the station lights the station's inside through its walls, and the reverse. The stairwell already shows it (C22 build log). Any of W1–W3 needs 游戏视觉's rendering-layer fix (`50_critic_fixes.md` §4 item 3). That fix is theirs; I only tag the station's cells or renderers.
4. **Determinism.**
   - A stamped station must be a function of the seed (rebuilds, revisit shifts, `NextFloor` all agree), or of a cell the game passes before `Begin` (as `SetExitArea` does for X).
   - Never a rebuild after the player has seen it.
5. **One chunk.** A station stamp must fit in one 8 × 8 chunk (the generator stamps per chunk). On floors 2+, X sits at chunk-local 1..6. A 3-deep station north of X needs X's local y ≤ 4, so the planner's X rule tightens (cheap: one more filter in `ChooseExit`).

## 3. Per option: what the map side has to change

| | Option | Map changes | Notes |
|---|---|---|---|
| (a) | **The station is the start area** (the room the title leads into, before the maze) | Floor 1: the title corridor's last room becomes the station, or a station shell sits between the stream's door and the map. Either way, title code (`FrontRoomsRoomStream`) or a new shell builder (stairwell pattern, W3); `SetStartArea` and `UpdateStartRooms` take a bigger reserved rect; `MapRootFor` tests the station's exit door instead of the stream door. Floors 2+: X grows into a station-sized reserved area | A safe room (§2.1): the Relay can't threaten the key-take inside it. Most rework of the title hand-off (10-02 design: Space = control in place, map behind the next shut door) |
| (b) | **The title's next door opens straight into the station** (the station is the first map room) | `MapRootFor` gains a test: a station stamp fits with the door cell in its room and an exit on another side. The game forces the stamp at that spot before `Begin` (like `SetExitArea`). G1 (placard P1 west of the door) folds into the station's own wall. The door hold/lock rules stay as they are; the 4 m shut lands inside the station | Smallest change to the title hand-off. The station is map, so the Relay can reach it later. **My cheapest option for floor 1.** |
| (c) | **The station is the first must-pass room on the floor-1 route** (a few cells in) | The stamp is placed by a planner like `ChooseExit` (band from the start door, a cut vertex the route must cross); `MapRootFor` unchanged | "Must pass" needs a cut-vertex rule, or the walls force it. More planner work than (b). The player meets Level 0 first, which cuts against "first room" |
| (d) | **Every floor starts at its own reception** | Floor 1 as (a) or (b); floors 2+: `SeedFor` / the C22 planner force a station stamp north of X (the start door then opens into it), X's local y ≤ 4 (§2.5) | The same frame on every floor. Builds on C22's floor start, so it comes after C22 lands. Also needs M3/M4's one-station-per-floor and key rules |

**Tutorial order (C32, "see the lock, then the key"):**
- With (a), (b) or (d), the key is in the first room, so the player holds it before seeing the EXIT's lock.
- C32's order then needs either:
  - the key elsewhere, with the station only teaching; or
  - the station's own door locked from inside (the placard + TV teach, the key opens the way out).
- That is 教程设计's / Red's call. The map can do either: the key spot is a marker, and a station door that needs the floor key is the same lock seam as the EXIT (`SetExitLocked`).

## 4. Order against my landing chain

- None of (a)–(d) touches today's chain: A12 (landed 15:11) → GD3c glass hunk → WarnBurst hook + C10 + relay_park → C22 → key-tap line.
- (b) and (d) build on C22's floor start, SeedFor and SetExitArea, and on M4's station stamp. They fit the post-C22 batch I already have queued (M1/G1/G2, VL217, `_FR_LampDip`, WayCueDriver/C24).
- If Red picks (b) or (d):
  - G1 (placard P1 west via `MapRootFor`) is replaced by the station test.
  - M4 becomes "forced station stamp at the start", not "one station somewhere on the floor".
- Files that would change, all mine: `FrontRooms3DGame.cs` (MapRootFor, the start state), `FrontRoomsMapWorld.cs` (stamp at a given cell, skin override W1), `FrontRoomsMap.cs` (stamp placement), `FrontRoomsExitPlanner.cs` (station-aware SeedFor / X rule, for (d)), plus their interaction tests and `MAP_GENERATION.md`.
- 游戏视觉 owns the station module assets, the surface set, and the rendering-layer light fix.
- If (a) or W3 is picked, the title stream code needs whoever owns `FrontRoomsRoomStream` now.
- **Unity runs:** the station stamp and skin override can be tested headless in my clones (interaction tests + a 100-seed check that the station fits on every seed, like G1).
