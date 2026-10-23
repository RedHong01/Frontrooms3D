# CueInk: the map-side contract (游戏视觉 → 关卡设计, 系统设计)

Written 2026-10-22 by 游戏视觉 (workflow prebuild-integrations wf_d1d3c10c-3b4, CueInk build stage; run 3 wrote it at 17:01, run 4 rebased it at 20:1x). Decisions it implements: **C08** (build the wall arrows; WebGL turns only), **C24 = A** with **A20** (the route: the station TV until the floor key is taken, then the EXIT), **C31 = M4 + V1** (Red 15:04–15:05). The data and the timing are in `CUE_DRIVER_INTERFACE.md` (平面视觉 · 信息设计, v0.2.4). Where the arrows point is in `MAP_ROUTE_SPEC.md` (关卡设计调研). If this file and those two disagree, those two decide the behaviour. This file decides only who writes which global, and in what order.

The diff beside this file is `CUEINK_CONTRACT_map.diff`. It adds **two calls** to 关卡设计's own `FrontRoomsWayCueDriver.cs`: base `~/FrontRoomsMapWork/proj3`, sha1 **e119c7c3** (the driver as of 17:21; run 3's diff was on 88b92cee, 16:47). It applies with `git apply --check` (result 2e2ffa28) and compiles (Roslyn `--all`, macOS and iOS defines, against proj3 plus the payload's `FrontRoomsCueInk.cs`: Assembly-CSharp 109 files, 0 errors). If the driver has moved on, add the two lines by hand. Each is one statement, and neither depends on anything else in the driver.

## 1. Who writes what

| Global | Type | Writer | When |
|---|---|---|---|
| `_FR_WayCells` | Texture2D RGBA32, 64 × 64, point, linear | map: `FrontRoomsWayCueDriver` | `Bind`; `Apply(false)` only when dirty; black texture at `Unbind` |
| `_FR_WayOrigin` | float4: map-root world x, z; 3 m; w = 1 | map: `FrontRoomsWayCueDriver.Flush` | first Flush and whenever the root moves; zero at `Unbind` |
| `_FR_Cue` | Texture2DArray 2048² × 16, BC5, print mips | visual: `FrontRoomsCueInk.Bind` | once the array has loaded (in the background) |
| `_FR_CueReady` | float, 1 or 0 | visual: `FrontRoomsCueInk` | 1 at load-and-bind, 0 at `Unbind` / `Release` / each Play |
| print hold | `FrontRoomsPrintDriver.SetHold("cue", anyCellOffZero)` | map (already in the driver) | while any cell is off state 0 |

The shader (`FrontRooms/Surface`, function `CueInk`) shows the cells only where `_FR_WayOrigin.w = 1` **and** `_FR_CueReady = 1`. Everything else stays the visual half's business: the slices, their import and the platform gates.

## 2. Call order (the two new lines are marked NEW)

1. `FrontRooms3DGame.MapRunStarted` → `driver.Bind(map, relay)`. It creates the cells texture, calls `Shader.SetGlobalTexture("_FR_WayCells", tex)`, then:
   - **NEW `FrontRoomsCueInk.Bind();`**
   - It returns false on WebGL, on phones (`FrontRoomsHandheld.Active`) and without the print driver. The driver ignores the result and runs as before; the walls just keep the plain print there.
   - The array (85.3 MiB on desktop) loads with `Resources.LoadAsync`, so the run start does not hitch. The cue shows from the frame it is bound. Before that, every wall is the plain print, whatever the cells say.
2. Every `LateUpdate` → `Step` → `Flush`: cells, origin and hold, unchanged.
3. `FloorChanged` → `ResetForFloor`: unchanged; no call here.
4. `MapRunEnded` / `OnDestroy` → `driver.Unbind()`. Inside `if (Current == this)`, after the origin and the cells are cleared:
   - **NEW `FrontRoomsCueInk.Unbind();`**
   - This sets `_FR_CueReady` to 0. The array stays loaded for the next run, so a restart after a catch does not reload 85 MiB. `FrontRoomsCueInk.Release()` also unloads it (scene teardown or low memory, optional).
5. Tools and captures (`TickForTools`) that need the cue on their first frame call `FrontRoomsCueInk.Bind(true)` (a synchronous load) before the first tick.

## 3. Safe in any landing order (one compile dependency)

**The one rule:** the two NEW calls name `FrontRoomsCueInk`, so they compile only once the visual half (`W/apply/cueink.sh`, which adds `Assets/Scripts/Rendering/FrontRoomsCueInk.cs`) is in main. The visual half is inert, so it lands first; the map driver can land with or without the two calls at any time after.


| In main | What the walls show |
|---|---|
| neither half (today) | the print exactly as before |
| visual half only (`W/apply/cueink.sh`) | the print, pixel-identical to main: nothing calls `Bind`, the array never loads, `_FR_WayOrigin.w` is 0 |
| map driver only, without the two calls | the print: cells written with w = 1, but `_FR_CueReady` is 0 (proved in VL390 rows c6) |
| both, with the two calls | the cue, from the frame the array is bound |
| both, cells all at state 0 | the print, pixel-identical (§5 item 14; VL390 rows c5). Since 17:21 the driver's `ResetCells` keeps the A channel (route dir + message FLOW) on state-0 texels: rows c5 fill all 4096 texels with message FLOW in each of the 4 dirs, so that case is covered |
| WebGL, phones | the print (see §5) |

## 4. Texel format and addressing (what CueInk reads)

- **Texel** (cellX & 63, cellZ & 63) of the map's grid cell (`MapWorld.CellOf`). The origin is the map root, which is cell (0, 0)'s corner: 64 × 3 m = 192 m = `ModuleUnits.WorldPeriod`. A wall belongs to the cell half a metre in front of it (the room it faces).
- **The four channels:**
  - R = from-state and G = to-state (0 = live print, 1–3 = turn 30/60/90°, 4–8 = flow clicks 1–5);
  - B = the cross-fade from → to (byte 0–255);
  - A = route dir (0 +x, 1 +z, 2 −x, 3 −z) + 4 × message (1 = FLOW).
- **Message 0** keeps the plain print whatever R/G say.
- **Faces:** a face whose +u (cross(up, n)) runs along the route reads slices state − 1. A face against the route reads state + 7. A face across the route (|dot| < 0.5) keeps the plain print, and so do floors and ceilings.
- A texel the driver has not cleared after a chunk dropped would show on the cell that is 192 m away. The driver already clears dropped or shifted slots (`PassageRevision`), as `CUE_DRIVER_INTERFACE.md` §1 says.

## 5. Platforms (each its own track; desktop is the reference)

- **macOS, Windows, Editor:** the full cue. BC5 2048² × 16 with print mips, 85.3 MiB of texture memory once bound, 0 while inert. The data in the build is about +85 MB.
- **WebGL:** off, so C08's "WebGL turns only" is not delivered yet. Three things gate it:
  - the shader compiles `CueInk` out on GLES3 and WebGPU;
  - the WebGL tab is a 32 px stub (about 44 KB in the heap, against about 43 MiB at 1024);
  - `Supported` is false.

  The driver's `TurnOnly` on WebGL is harmless meanwhile. To opt in, the WebGL line would:
  - lift the shader guard (+2 texture units);
  - raise the WebGL tab: 16 slices at 1024 RG16 = 42.7 MiB in the heap, or a turn-only array (states 1–3, 6 slices) at 512–1024 = 4–16 MiB;
  - drop the WebGL line in `FrontRoomsCueInk.Supported`.

  The print driver does not run on WebGL. On GLES the cue's sampler comes with its own texture, so that is no obstacle.
- **iOS, Android:** off. `Supported` reads `FrontRoomsHandheld.Active`, and the iPhone and Android tabs are 32 px stubs, so the inert cue adds neither app size nor memory to the phone builds. To opt in, the mobile line would:
  - remove the stub targets in `FrontRoomsPrintArray.CueStubTargets` (2048 automatic = 85.3 MiB once bound, or a 1024 tab = 21.3 MiB);
  - drop the handheld test in `Supported`.

## 6. What the map line checks after adding the two calls

- After a run starts on desktop: `FrontRoomsCueInk.Ready` turns true within a few frames, and `Shader.GetGlobalFloat("_FR_CueReady") == 1`.
- At stage 1, the room set's walls turn in three clicks at +0.20, +0.50 and +0.80 s. Each click is a 0.07 s cross-fade: at 30 fps, 2 cross-fade frames, then the state holds. In a chase the cells step at 4 clicks/s: 7–8 frames per state at 30 fps, 2 of them cross-fade. The visual half's timed proof is in VL391.
- After `MapRunEnded`: `Ready` is false, and the walls are the plain print.
- On a phone profile (`FrontRoomsHandheld.Force`): `Bind` returns false, and nothing loads.

## 7. For 系统设计

There is nothing to change in C08 / A20 / C24 / C31: the cue shows the route field's dir and nothing else. One open item: C08's "WebGL turns only" waits on the WebGL line's memory call (§5).
