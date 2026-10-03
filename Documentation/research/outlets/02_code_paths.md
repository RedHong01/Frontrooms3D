# 02 — Code paths: where outlet placement plugs into the room and wall logic

Power-outlets workflow (N3), 2026-10-03. Read-only study of the real project; no file outside this folder was changed.

**Revision 2 (12:00).**
- Every file:line reference was re-checked against the project as of 11:45. No map, Office or RoomStream file has changed since revision 1. The newest is `FrontRoomsMapWorld.cs` at 09:49.
- New since revision 1:
  - the LOD convention, from the interactables spec that landed at 11:38 (§4.4);
  - the glass RT pass's 256-renderer cap (§4.5);
  - a correction: the Mac/Win players run Ultra, lodBias 2 (§4.4);
  - the Office-zone share of wall faces (§4.1);
  - the module lamp precedent for module outlet data (§2.3);
  - the edge-of-ring rule for floating outlets (§3);
  - an edit-mode submit hook for R3 (§4.5);
  - an unreachable Exit branch in RoomStream (§5);
  - the `Fixture` struct (§8.1);
  - **a clone test of the exact contract code (§8.4, §11)**, which passes.

- All numbers come from the code, or from a census run in the private clone `scratchpad/proj_outlet`.
- The census uses the game's own generator: 3 seeds × 49 chunks.
  - Data: `data/02_face_census.json`. Tool source: `data/02_face_census.cs.txt`.
  - Reproduce it with the command in §10.
- Paths are relative to `Frontrooms3D/`.

## 0. Summary

1. **Map walls are not objects.**
   - `FrontRoomsMapWorld.BuildEdge` merges every wall into combined meshes per 6 m block, per ceiling height and per material.
   - A wall face has no GameObject and no record after the build. The data exists only inside the build loop.
   - Map walls have **no baseboard or cove**. Only door and window frames, and column coves, use the trim material.
2. **One chunk has about 128 wall faces.** Each is a side of a 3 m cell edge that is not Open (range 52–160).
   - That is about **303 m of solid wall at outlet height**.
   - With build radius 2, 25 chunks are alive at once.
3. **87 % of faces are corridor faces.** They belong to no carved room.
   - 6.6 % face into rooms the map dresses.
   - Only **1.5 % face into dressed Office rooms**.
   - So a plan that runs only inside the dress step would miss about 93 % of the walls.
4. **Coplanar wall runs are short.** The mean run is 1.44 faces (4.3 m), and only 9.5 % of runs are 3 faces or more.
   - Only **1.6 % of runs cross a chunk border.**
   - A per-face plan with a world-anchored hash is enough. No cross-chunk spacing logic is needed.
5. **Recommended:** plan **once per chunk, right after the shell is built** (option B).
   - The plan covers every face that looks *into* this chunk's cells.
   - Every input is then the chunk's own data or a border edge that depends only on the seed. So the plan never depends on a neighbour's revision.
   - Add a **dress-time refinement for Office rooms** (option C). It runs inside `FrontRoomsOfficeKit.Dress`, which is visual-owned.
   - The map's only change is one call plus one helper in `BuildInto` (contract request, §8).
   - That exact code was **tested in the clone** (§8.4).
     - 0 face mismatches against the map's own edges over 3 seeds × 25 chunks.
     - Every planned plate sits on solid shell at plate height.
     - A rebuild gives an identical plan.
     - Gathering costs 0.07–0.13 ms per chunk.
6. **Rendering must be batched.** At a code-like density (1 per 3.66 m of wall) that is about 83 outlets per chunk, about 2,070 alive and about 65 within 12 m.
   - One GameObject per outlet breaks the ≤ 120-renderer room budget.
   - It also breaks the WebGL target of about 1,200 renderers alive.
   - Best fit: per-chunk instanced draws with exact distance LOD (R3). Second best: per-block combined meshes (R2).
7. **LOD: use the interactables convention. Until it is approved, outlets get no LODGroup.**
   - The interactables spec (`research/interactables/10_spec.md` §1.8, §8 P-1) sets the convention:
     - `<NAME>_LOD0/_LOD1/_LOD2` in one FBX;
     - per-asset `LOD_DISTANCES = (d01, d12, dcull)` at lodBias 1 and FOV 76°, sent to the sidecar as `lodDistances`;
     - the importer turns those distances into screen heights.
   - That change (P-1) **needs visual-chat approval**. Until then, assets under 1.0 m ship with `LOD1 = None`: no LODGroup, never culled.
   - The interim rule exists because today's importer would cull a 0.114 m plate at 2.4 m.
   - The catch: GameObject outlets (R1) would then draw at every distance. The 12 m cull and the LOD switch should live in the outlet renderer (R3), which reads the same `_LOD0/1/2` meshes.
8. **The title corridor's placeholder outlets have colliders.**
   - There are 3 per Lobby/Shift/Exit room variant, built by `Box`, which adds a BoxCollider.
   - Their centre is 0.32 m, only 2.5 mm above the 0.26 m baseboard top.
   - Office stream rooms use `FrontRoomsOfficeKit.Dress`. Run rooms have no outlets.
   - The same planner can serve the corridor with no contract, because RoomStream is visual-owned.
9. **The glass RT pass would count GameObject outlets against its 256 cap.**
   - `FrontRoomsMetalGlassRT` registers every enabled MeshRenderer within 18 m of the nearest glass target, up to 256 (`Assets/Scripts/Rendering/FrontRoomsMetalGlassRT.cs` 33–34, 145–157).
   - The list is in InstanceID order, not distance order.
   - At 1 outlet per 3.66 m, about 147 outlets lie within 18 m. With R1 they would take over half the cap and could push walls or doors out of the reflections.
   - R3 draws are not renderers, so the RT pass never sees them. At 0.11 m, outlets do not need to be in reflections.
10. **Office outlets are mostly in corridors.**
    - 24 % of wall faces are in Office zones, but only 1.5 % face into dressed Office rooms.
    - So Office outlet rules (denser outlets, data jacks) must run outside the dress step too. That is option B again.

---

## 1. How the map builds walls (`Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs`)

### 1.1 Build order of one chunk

| Step | Where | What |
|---|---|---|
| Stream | `Stream` 688–717 | Nearest ring first, `chunksPerFrame` (1) per frame. A chunk dropped for at least `shiftAfterSeconds` (30 s) gets `Cache.Shift` (703–710), so its interior comes back different. It is always ≥ 24 m away when that happens (704–705). |
| Guard | `Build` 756–770 | `BuildInto` runs inside try/catch. A throw undoes the chunk (`Unregister`) and marks it failed until it leaves range. |
| Chunk root | `BuildInto` 772–781 | `Chunk (x, y) · revision r`, at origin `coord × 24 m`. The root only translates. |
| Cell loop | 803–854 | For each of 64 cells: floor and ceiling (815–819), then the cell's **east and north** edges via `BuildEdge` (821–842), then the column on its SW corner (844–851), then the troffer (853). |
| Shell renderers | 856–867 | One GameObject `Block k · h m` per 6 m block and ceiling class. One renderer per material, carrying a `_CeilingHeight` MaterialPropertyBlock (`AddRenderer` 896–913). Shadows on. |
| Collision | 868–873 | One MeshCollider for the whole chunk shell (`shellColliders`). |
| Key | 875–880 | `SpawnKey` 1950–1974: the one place a key is made. A primitive with its collider killed at once, parented to the chunk root. |
| Relay entries | 881, 1988–2003 | Module markers become world points in `chunk.relayEntries`, exposed by `RelayEntries` (2006–2010). This is the pattern for "a per-chunk list another system reads". |
| Office grade | 882 | `AddZoneGrades`. |
| Furnish | 883, 1458–1466 | Queues rooms for dressing; furniture comes later, one room per frame. |

Edges on the chunk's **east and north border belong to this chunk**. Its west and south border walls are built by the neighbours (comment at 821–822). `MapChunk.west/south` keep copies of those border edges (`FrontRoomsMap.cs` 146–150, 163–164).

### 1.2 `BuildEdge` (945–1054): what a wall is

- **Parameters:**
  - `kind`: Wall / Arch / Door / Window. Open returns at once.
  - Cells `a` and `b`.
  - `start`: the cell corner on the cell line, chunk-local.
  - `along`: +Z for an east edge, +X for a north edge.
  - `height`: the taller of the two sides (827–828).
  - `wallA` and `wallB`: each side's wall material.
  - `mayExtendStart` and `mayExtendEnd`.
- **`across`** (952) points from cell a into cell b.
  - Face A is the plane at −0.08 m (`WallHalf`), facing −across, into a.
  - Face B is at +0.08 m, facing +across.
- **Pieces** (`Piece` 953–970) are boxes in the block's MeshBuilder.
  - Where the two themes differ, the wall is two 0.08 m skins, each in its own room's paper (967–969).
  - Ends extend 0.08 m past a corner unless they stop at an opening or the start area (955–958).
- **Openings:**

  | Opening | Width | Position | Height | Code |
  |---|---|---|---|---|
  | Door | 1.0 m | centred | top 2.1 | 980–985 |
  | Window | 1.4 m | centred | sill 0.35, top 2.0 | 986–992 |
  | Arch | 1.1–1.8 m | off-centre, from the edge hash | top 2.2 | `ArchOpening` 1084–1090; `ModuleUnits` 50–52 |

- **Trim:** door and window frames only (1000–1009).
  - Jambs have a 0.07 face (`TrimFace`) and stand 0.02 proud of each wall face (`TrimProud`, `ModuleUnits` 64).
  - Arches have no trim (998).
- **No baseboard, cove base or chair rail on map walls.** `trim` = `FrontRoomsSurfaces.CoveBase` (2067) is used only for frames and the 0.10 m column cove (`BuildColumn` 1069).
  - The title corridor *does* have a baseboard (§5).
  - Level transitions (N1) may add base trims at zone borders. The outlet planner should therefore read a per-face `baseTop` (0 today).
- **Door and window objects** (1013–1053) are separate GameObjects registered by edge id (`EdgeId` 1111–1116).

### 1.3 Data available per wall face at build time

| Datum | Source | Inside `BuildEdge`? | Note |
|---|---|---|---|
| Edge kind | `data.east/north[index]`, start-area adjusted by `StartAreaEdge` 924–935 (830–831) | yes (`kind`) | West and south border copies: `data.west/south` |
| Start point, direction | `start`, `along` (835, 839) | yes | chunk-local metres, on the cell line |
| Face plane and normal | `across` (952); face = cell line ± `WallHalf` | yes (derive) | normal points into the cell the face serves |
| Length | `MapGrid.CellSize` = 3 (950) | yes | plus 0.08 at extended corners |
| Ceiling of that side | `MapGrid.CeilingHeight(data.height[index])` (810); `data.height` is the zone height (`FrontRoomsMap.cs` 568) | wall height only (the taller side) | 2.4 / 2.9 / 5.4 |
| Theme of that side | `wallA` / `wallB` (829); `Cache.ZoneOf(cell).theme` | as a Material | two skins at theme borders |
| Opening span | door / window centred; arch from `ArchOpening` | yes (`width`, `c`, `sill`, `openingTop`) | |
| Trim | jamb and head boxes | yes | 0.07 face, 0.02 proud |
| Stable id | `EdgeId(a, b)` | yes | add a side bit per face |
| Tier | `data.tier` (`FrontRoomsMap.cs` 158) | via `data` | |
| **Room / module** | `data.rooms` (188); topmost rect that contains the cell (later rooms win); `data.ModuleOf(r)` (195) | **no** | 87 % of faces are in no room |
| **Inner corners** | the cell's own side edges | **no** | derive from the chunk arrays |
| Columns | `data.pillar` (168) | no | not needed: a column face is ≥ 2.47 m from any wall face (`LEVEL_MODULE_SPEC.md` line 61) |
| Lamp temperament | `BuildFixture` 1173–1227: `fixture.mode` from tier + `MapHash(seed, cell, 211)` | same cell iteration, after the edges | deterministic; usable as a wear cue (for example, scorch marks under dead lamps) |
| **Furniture** | Dress, one room per frame, later | **no** | only 6.6 % of faces face a dressable room |

### 1.4 What must be derived, and how cheaply

- **Corners.**
  - For a face of cell c, its two ends are inner corners when c's own side edges at those ends are not Open.
  - All four edges of a cell are in the chunk's own arrays: east `east[k]`, north `north[k]`, west `east[k−1]` or `west[j]` at i = 0, south `north[k−8]` or `south[i]` at j = 0.
  - **No neighbour chunk has to be generated.**
- **Room.** Use the last `data.rooms[r]` that contains the cell. A dressed room also needs `RoomIntact` (204–209) and `Generator.Uniform` (402–412).
- **Arch span.** `ArchOpening` is private in MapWorld, so the map must pass the span. `MapHash` itself is `internal` (`FrontRoomsMap.cs` 213). All game scripts share Assembly-CSharp (no asmdef outside FMOD), so Office code can call `MapHash.Hash` for its own hashes.
- **Cost.** In the census, gathering every face of a chunk through `Cache.Edge` / `Cache.ZoneOf` took **0.41–0.69 ms per chunk**.
  - That is editor Mono, a warm second pass, while 3 other Unity processes ran.
  - Reading the chunk arrays directly, as the contract helper does, measured **0.07–0.13 ms per chunk** in the clone (§8.4).

---

## 2. Rooms, dressing and modules

### 2.1 Which rooms get dressed, and when

- `Furnish` (1458–1466) queues a room only when three things hold:
  - it is one open rectangle (`RoomIntact`);
  - it is one height and theme (`Uniform`);
  - it is not cut by the start area.
- `DressNext` (1469–1482) dresses one queued room per frame. It skips jobs whose chunk was rebuilt meanwhile.
  - While the title streams (`StreamFocus`), a chunk build and a dress never share a frame over 8 ms (`FocusFrameBudgetMs` 411, used at 662).
- `Dress` (1485–1579) does the following, in order:
  - Builds `roomSeed = Hash(seed, coord.x·16 + r, coord.y, 307, revision)` (1491).
  - Builds `columns` (1492, 1731–1744).
  - Builds `KeepClear` strips (1493, 1780–1800). There is one strip per passable boundary cell edge, covering the **whole 3 m**, 1.0 m deep (1.2 at doors).
  - Places module props (1521, `PlaceProps` 1601–1662).
  - Keeps the key and the Relay entries clear (1524–1537).
  - Adds module inner walls as obstacles (1538–1546).
  - Then calls either the Office kit (1549–1558) or a pile (1559–1571).
- The census found 53–58 dressable rooms per 49 chunks, of which only **7–17 are Office rooms**.

### 2.2 `FrontRoomsOfficeKit.Dress` (`Assets/Scripts/Office/FrontRoomsOfficeKit.cs`)

- The map binds the 6-argument overload (276–279) **by reflection** (`ResolveDressers` 1428–1450). It gets `parent` = the **chunk root**, so Dress can find a component on it.
- Dress returns `void`. **It reports nothing about what it placed.**
  - Its occupancy grid, wall sides and placements stay local to `DressRoom` (287–320).
- Dress's view of walls is coarse (contract, 17–27): the floor rect's edges are walls except where a keep-clear strip touches.
  - Every passable cell edge therefore counts as fully non-wall, including the solid wall beside an arch.
  - Dress cannot see door positions or arch spans.
- Furniture against walls, and its offset from the wall face:

| Placement | Code | Offset from wall face | Height | What an outlet there would do |
|---|---|---|---|---|
| Wall units: vending, water cooler, copier, filing-cabinet runs | `PlaceWallUnits` 401–430, `TryWallAt` 472–489 | back 0.03 m off the face (478); 0.5 m service zone in front | 1.15–1.83 m | hidden behind the unit; no intersection, since a plate stands < 0.01 m proud |
| Interior window (decor) | 478, 486 | set 0.02 m **into** the wall | sill 0.90 | no conflict with an outlet at 0.3–0.45 m |
| Cubicle wall rows (1–3 stations, back panel on the wall) | `PlaceWallRows` 500–533 | row origin 0.045 m off the face (526); panel 0.065 thick, 1.57 high | 1.57 m | hidden; **powered-panel hook**: a 1990 panel run takes power from a wall outlet at its end panel through a base feed (to be confirmed by the research files) |
| Free-standing pods | `PlacePods` 537–615 | ≥ 1.05 m off the walls (600–601) | | power from a floor box or power pole, not the wall (a `Kit_FloorBox` hook inside Office/) |
| Station gear: CRT, PC, phone | `BuildStation` 653–688 | on the desk | | cords would go to the panel raceway, not the wall |

- **No furniture intersects a wall-mounted plate.** Everything stands ≥ 0.03 m off the face.
- So option B's furniture-blind outlets are never physically wrong. They can only be hidden, which costs draws for nothing.
- Furniture piles keep a 0.6 m ring off the walls (`PileSpot` 1752–1771), so piles never touch outlets.

### 2.3 Modules and the Level Designer

- **Props** (`ModuleProp`, `FrontRoomsRoomModuleData.cs` 37–44) hold a kit, x, z, y, yaw and `noCollider`.
  - `PlaceProps` spawns them with `FrontRoomsKitLibrary.Spawn` (1648).
  - A prop with `noCollider` is never left out for blocking an opening strip (1622).
  - Any prop with y ≤ 2.05 m adds its footprint to the Office fill's obstacles (1641, 1657–1659). For a pinned outlet that is a 0.07 × 0.01 m rect, plus a 0.45 m reserved halo inside Dress (296). Harmless, but wasteful.
- **Wall-kit snapping in the Level Designer** (`Assets/Editor/FrontRoomsMap/FrontRoomsModuleEditing.cs`). Sidecar `placement` "Wall" (tags `wall_unit` / `wall_decor`, `kitlib.py` 820–821) gets snapped by `SnapToWall` (160+) through `AgainstWall` (130–145). That places the back **0.03 m off the face** (`WallGap` 22).
  - Height is either 0, or **2.1 m** for kits with a `hang` anchor (`HangHeight` 24, `NewProp` 111–113).
  - **Neither fits a flush outlet at 0.3–0.45 m.** Pinning outlets in modules needs an editor change, which is map-owned.
- **Markers** (`ModuleMarkerKind` 55–61) have values that are "only ever appended": KeySpot and RelayEntry. They turn with the module (`Rotated` 269–273).
- **How a module could control outlets** (map-owned data, so contract phase 2):
  - (a) a `ModuleOutlets outlets = Auto` field on `RoomModuleData`, with values Auto / None;
  - (b) pins as props of `Kit_Outlet*` kits, with a new "flush wall" snap: gap = `WallHalf` only, y from a sidecar `mount` anchor.
  - The planner reads `data.ModuleOf(room)` at chunk build time, so pins and None are known before the auto plan runs. No ordering problem with Dress.
  - **Precedent: module lamps.** A module already authors one `ModuleLamp` per cell (`RoomModuleData.lamps`, line 111). The default is `Auto`, which "rolls it from the seed, as everywhere else" (comment, line 29).
    - The stamp copies the lamps into the chunk's per-cell array `MapChunk.lamp` (`FrontRoomsRoomModuleStamp.cs` 96).
    - `BuildFixture` then reads that array during the same cell loop (853).
    - Lamps turn with the module (`RotatedOnce`, `FrontRoomsRoomModuleData.cs` 251).
    - Module outlet data could take the same shape: an `Auto` default, stamped into a `MapChunk` field, read by the planner. For example, one value per room, or one per perimeter edge next to `south/north/west/east` (98).
- **Stamp rules** (`FrontRoomsRoomModuleStamp.cs` 32–101): a module never changes a chunk-border edge. The map may reopen a module wall to reconnect the chunk (`Reconnect` 120–158). So outlets must be planned from the **final** chunk edges, which option B does, never from the module's authored edges.

---

## 3. Determinism, streaming and unregister

- **Borders** are pure functions of seed + world coordinates (`BorderEdge` 349–358, `Resolve` 360–373). So are zones (`Zone` / `ZoneOf` 292–338) and arch spans (`ArchOpening`: seed + cell).
- **Interiors** follow the chunk's revision: maze, rooms, columns and modules (`Generate` 558–642). A rebuilt chunk is identical; a shifted one changes, always out of sight.
- **Key property for option B.** Plan every face that looks *into* chunk C's cells. These include the faces of C's west and south border walls, which the neighbour builds. Then every input is either:
  - C's own data (rooms, modules, its inner edges), or
  - a border edge or zone that depends on the seed alone.
- **Therefore C's outlets never depend on a neighbour's revision.**
  - Option A (per edge in `BuildEdge`) breaks this.
  - Under A, the east or north chunk would build 9.8 % of faces: the faces of border walls that look into the neighbour. Those outlets would sit in the neighbour's room but follow this chunk's lifetime. If the neighbour shifts, they can end up behind its new furniture or in its new module room.
- **Hash.** Use `MapHash.Hash(seed, cell.x, cell.y, salt + side)` with a new salt.
  - Salts already in use: 11–73 in `MapHash` (215–217); 97, 211, 223 and 307 in MapWorld; 3/7/11 inside `ArchOpening`. Pick 409 or above.
  - Leave the revision **out** of the hash. Then a face that did not change keeps its outlet across a shift. A face whose context changed (room, module) re-plans anyway, out of sight.
- **Start area.** Skip a face when its cell **or** the cell behind it is in the start area (`InStartArea` 483–484).
  - That covers every case `StartAreaEdge` rewrites.
    - `StartAreaEdge` turns a raw edge between a map cell and a start-area cell into a plain Wall. The raw arrays do not show that, so skipping is simpler than copying the rule.
    - The cost is a few outlets on the walls round the start area. The census does not model the start area.
  - The stream rooms' walls are RoomStream's own business (§5).
- **Edge of the build ring.**
  - Under option B, a chunk on the ring's west or south edge plans outlets on its west or south border wall. That wall is built by the neighbour outside the ring, so it does not exist yet. Those outlets would float.
  - The player is always at least 2 chunks (48 m) from those faces, and outlets cull at 12 m. So they are never drawn.
  - **Rule: the outlet cull distance must stay under 48 m.**
  - R1 under the interim LOD rule (no LODGroup, never culled) would break this rule. It would draw specks at 48–80 m, out to the 80 m far plane (`FrontRooms3DGame.cs` 247).
- **Unregister** (728–744) kills the chunk root and frees `chunk.meshes`. `RebuildChunk` (319–331) deactivates the root first.
  - Anything the planner installs under the chunk root is cleaned up for free.
  - That includes a MonoBehaviour, which gets OnDisable, and meshes, if added to `chunk.meshes`.
  - **No change to Unregister is needed.**
- **Failure.** `Install` runs inside `Build`'s try, so a throw undoes the whole chunk like any other build error.
- **Edit mode.** The Level Designer preview and the capture harness build maps in edit mode (`BuildForCapture` 523–535, `Release` 559–564). Whatever renders outlets must work without `Update`; see R3's edit-mode row in §4.5.
  - The Level Designer preview (`FrontRoomsModulePreview.cs` 5–19, `[ExecuteAlways]`) builds the **real** `FrontRoomsMapWorld` round the module (277, 297). So option B outlets appear in the designer's preview with no designer change.

---

## 4. Budgets, LOD, culling and batching

### 4.1 Census (game generator, `Assets/Levels/FrontRoomsLevel0.asset` with its modules, tier 1, chunks −3..3 × −3..3)

| | seed 20261001 | seed 2554 | seed 20388 | per chunk (mean) |
|---|---|---|---|---|
| Wall faces (non-Open cell-edge sides) | 6,140 | 6,317 | 6,376 | **128** (52–160) |
| Wall / Arch / Door / Window | 3,579 / 2,071 / 377 / 113 | 3,862 / 1,938 / 417 / 100 | 3,821 / 1,957 / 493 / 105 | 59.8 % / 31.7 % / 6.8 % / 1.7 % |
| Corridor faces (no room) | 5,337 | 5,488 | 5,561 | **87.0 %** |
| Faces into dressable rooms / into dressed Office rooms | 388 / 61 | 439 / 94 | 416 / 126 | 6.6 % / **1.5 %** |
| Faces of border walls the neighbour builds | 596 | 617 | 627 | 9.8 % |
| Solid wall at outlet height (openings and inner-corner insets removed), m | 14,358 | 15,065 | 15,102 | **303 m** (111–389) |
| Solid stretches ≥ 0.6 m | 7,035 | 7,217 | 7,337 | 147 |
| Coplanar runs; mean length; runs crossing a chunk border | 4,273; 1.44; 70 | 4,374; 1.44; 63 | 4,471; 1.43; 78 | 89; 1.44 faces; **1.6 %** |
| Face gathering via Cache (warm, editor) | 0.42 ms | 0.69 ms | 0.41 ms | upper bound |
| Faces in Office zones (rooms and corridors) | 1,026 | 1,654 | 1,820 | **23.8 %** (16.7–28.5 %) |
| Solid wall in Office zones, m | 2,491 | 4,034 | 4,405 | 74 m |
| Dressable rooms / of which Office | 56 / 7 | 58 / 14 | 53 / 17 | 1.14 / 0.26 |

- **Caveats.**
  - The census runs the generator through `FrontRoomsMapCache`, not `FrontRoomsMapWorld`. So it has no start area; the start area removes a few cells near the origin in the title flow.
  - "Dressable" means `RoomIntact` and `Uniform`. The census counts Office rooms by zone theme. It does not count Level 0 rooms whose module sets `fill = Office`, which `Dress` also furnishes with the Office kit (1547–1548).

### 4.2 What a density choice costs

Build radius 2 means 25 chunks are alive. A 12 m radius covers about 0.79 of a chunk's area. The density itself is the spec's decision (research files); these are planning numbers only.

| Density rule (example) | Outlets per chunk | Alive (25 chunks) | Within 12 m (before frustum/occlusion) |
|---|---|---|---|
| One per solid stretch ≥ 0.6 m (upper bound) | 147 | 3,670 | 115 |
| One per 3.66 m of solid wall (a 1.83 m reach rule, as in dwelling codes) | 83 | 2,070 | 65 |
| One per 6 m | 51 | 1,260 | 40 |
| One per 9 m (sparse Level 0) | 34 | 840 | 26 |

- Spec §8 (`LEVEL_MODULE_SPEC.md` 137–147) sets ≤ 120 renderers and ≤ 40 colliders per dressed room.
- The WebGL research (`research/webgl/03_measured_budgets.md` 14–20, 68) measured several things:
  - About 2 batches per in-frustum object (depth-normals prepass + colour), plus shadow casters.
  - About 40 µs per draw in WebGL.
  - A WebGL target of ≤ 600 draws per frame and about 1,200 renderers alive.
- **So one renderer per outlet cannot scale.** Even at 1 per 9 m, 840 outlet renderers would be alive.

### 4.3 Batching state today

- **SRP Batcher is on** (`Assets/Settings/FrontRooms_URP.asset` 72). **GPU Resident Drawer is off** (86).
- Shell renderers and lamp lenses carry MaterialPropertyBlocks (`AddRenderer` 906–911, `WriteEmission` 1332–1338). The WebGL study found that this takes about 1,000 renderers out of the SRP Batcher.
  - **Rule for outlets: no MaterialPropertyBlocks.** Make wear variants separate meshes or materials, or vertex colour.
- `FrontRooms/Surface` has `multi_compile_instancing` in all four passes: Forward 110, ShadowCaster 279, DepthOnly 297, DepthNormals 313. **But 0 of the 86 materials in `Assets/Resources/Surfaces` enable instancing** (`m_EnableInstancingVariants: 0`).
  - `Graphics.RenderMeshInstanced` needs that flag. Turn it on for the outlet slot materials in `FrontRoomsRenderSetup` (visual-owned), or use instancing-enabled clones.
  - The shader side is ready: it uses `UNITY_VERTEX_INPUT_INSTANCE_ID` (120, 132), `UNITY_SETUP_INSTANCE_ID` / `UNITY_TRANSFER_INSTANCE_ID` (139–140, 170).
  - Kit slot materials (`Prop_*.mat`) use the same shader with `_FR_MESH_UV` on (96).
  - **Per-instance variety comes free.** The macro wear is sampled in world space (8 m and 12.8 m, 189–198), so every outlet gets its own grime with no MaterialPropertyBlock.
- **No other culling or batching tool is in use.** The scripts use no `CullingGroup`, no `Camera.layerCullDistances`, no static batching and no `CombineMeshes`. `TagManager.asset` has no custom layers. The only distance logic is the lamps' (`lightRadius`, `TickFixturesNear` 1256–1281).
- Kit FBX meshes import with `isReadable = false` (`FrontRoomsKitImporter.cs` 33).
  - Runtime mesh combining (R2) needs readable outlet meshes, which takes an importer exception.
  - Instanced draws (R3) and GameObjects (R1) do not.
- Kit shadows are off for props ≤ 0.3 m (`FrontRoomsKitLibrary.ApplyMaterials` 218, 236). Outlets fall under that automatically. Colliders come only from sidecar boxes (`AddColliders` 241–251), so `kit.no_collider()` yields none.

### 4.4 LOD: the importer's rule does not fit an outlet

- `FrontRoomsKitImporter.OnPostprocessModel` (67–79) sets **two** LOD levels:
  - LOD0 → LOD1 at 10 % screen height;
  - cull at 3 % for props under 0.6 m, 2 % otherwise.
- Unity's relative height is `size × 0.5 / (distance × tan(FOV/2)) × lodBias`.
  - Game camera FOV is 76° (`FrontRooms3DGame.cs` 247).
  - lodBias differs by target:

    | Target | Quality level | lodBias | Where it is set |
    |---|---|---|---|
    | Editor Play Mode (the desktop reference) | 3, High | 1 | `ProjectSettings/QualitySettings.asset` 7, 195 |
    | Mac/Win player | 5, Ultra | 2 | per-platform default, 301 and 338. `FrontRoomsCloudBuild.cs` 111 calls `SetQualityLevel(3)` in its Standalone path. That sets the editor's current level; whether a player then starts at High instead of its per-platform default is UNVERIFIED |
    | WebGL player | 3, High | 1 | per-platform default, 339; `FrontRooms3DBuild.ApplyWebGLSettings` also calls `SetQualityLevel(3)` at 146 |

  - Revision 1 said the build script sets level 3 for every build. That was wrong: `FrontRooms3DBuild.cs` 146 is inside the WebGL path, and `BuildMac` (58–74) sets no level.
  - The interactables spec reads it the same way: Standalone runs Ultra (`10_spec.md` 232).

| For a 0.114 m single-gang plate | lodBias 1 | lodBias 2 |
|---|---|---|
| LOD0 → LOD1 at 10 % (today's rule) | **0.73 m** | 1.46 m |
| Culled at 3 % (today's rule) | **2.44 m** | 4.88 m |
| Height needed for LOD0 to 2 m / LOD1 to 5 m / cull at 12 m | 3.66 % / 1.46 % / 0.61 % | half of these |

- This is Unity's editor `LODUtility` formula. Verify it in-engine before relying on it.
- The pipeline builds only LOD1 today: `kitlib.make_lod1` (714–760), a collapse-decimate named `<NAME>_LOD0` / `<NAME>_LOD1`, called by `build_asset.py` 51–53 when a module sets `LOD1`.

**The convention to use: the interactables spec** (`research/interactables/10_spec.md`, written 11:38 today).
- **Hero LOD0** (§1.8) must hold up at 0.3 m.
  - Bevel every light-catching edge: ≥ 1.0 mm on hardware, with 2–3 segments.
  - Curved parts ≤ 0.07 m across get ≥ 48 segments, so nothing facets at 0.3 m. The screw heads of a plate fall under this.
- **Every module declares** `LOD1_RATIO`, `LOD2_RATIO` and `LOD_DISTANCES = (d01, d12, dcull)` in metres at lodBias 1 and FOV 76°. It also writes `kit.meta["lodDistances"]`, which `export()` copies into the sidecar.
- **P-1** (§8, **needs visual-chat approval**) does three things:
  - `make_lod1` becomes `make_lods(ratios)`, which writes `<NAME>_LOD0/1/2` into one FBX;
  - parts can be dropped per level (`fr_lod{n}_drop`);
  - the importer sets `screenRelativeTransitionHeight = group.size / (2 · d_i · tan 38°)` from `lodDistances`.
- **Until P-1 lands:** assets under 1.0 m set `LOD1 = None`. They get no LODGroup and are never culled. An outlet plate falls under this rule.
- **What it means for outlets.**
  - R1 under the interim rule draws every outlet at every distance. That costs about 2 draws per outlet in the frustum, and it breaks the edge-of-ring rule in §3.
  - R3 does not depend on P-1 for culling. It does its own exact distance cull, and later its LOD switch, the same on every lodBias.
  - R3 reads the meshes by the P-1 names (`<NAME>_LOD0/1/2`), so the outlet module can declare `LOD_DISTANCES` today.
  - **Until P-1, the FBX holds LOD0 only, so R3 draws LOD0 out to `dcull`.**
    - The interactables spec gives its 58 mm key a budget of 2,400 / 900 / 150 triangles (`10_spec.md` 514).
    - At a similar LOD0, about 65 outlets within 12 m come to about 156 k triangles (ESTIMATE).
    - With P-1 and switches at 1.5 / 4 / 12 m, that falls to about 15–20 k triangles (ESTIMATE).
  - So outlets are a second reason to approve P-1. Do not invent a separate LOD convention for them.
- **Suggested outlet distances** (for the spec to confirm): d01 ≈ 1.5–2 m, d12 ≈ 4–5 m, dcull ≈ 12 m.
  - For comparison, the interactables spec uses 1.0 / 3.0 / 12 m for the 58 mm key and 1.5 / 5 / 20 m for the 30 mm closer shoe (`10_spec.md` 514, 948).
  - As LODGroup heights for a 0.114 m plate, 2 / 5 / 12 m are 3.66 % / 1.46 % / 0.61 % (table above).
  - For a tiny hard-surface part, hand-built LOD1/LOD2 beats decimation. LOD2 could be a plate box with the face printed in its texture. P-1's per-level drop flags allow that.
- **New material slots:** `kitlib.register_slot(...)` (132) adds one without editing kitlib. The importer remaps any slot that has a `Resources/Surfaces/<slot>.mat` (`FrontRoomsKitImporter.cs` 36–46). This is the interactables spec's P-4 route. An outlet's ivory or brown thermoplastic might need one; report it to the visual chat.

### 4.5 Three ways to render the plan

| | R1: one kit GameObject per outlet | R2: combined meshes per 6 m block | R3: per-chunk instanced draws (recommended) |
|---|---|---|---|
| How | `FrontRoomsKitLibrary.Spawn(..., colliders: false)` (no Collider: §8.3 test 2) | Like the shell's `MeshBuilder`; meshes go in `chunk.meshes` | A `FrontRoomsWallFixtureSet` on the chunk root holds matrices per (kit, LOD) and adds itself to a static registry in OnEnable, removing itself in OnDisable. A static submitter draws, for each camera, the outlets within 12 m with `Graphics.RenderMeshInstanced` |
| Renderers alive (25 chunks) | 840–3,670 (§4.2) | ≤ 16 blocks × LODs × slots per chunk | **0** |
| Draws | ~2 per outlet in the frustum (depth-normals + colour); never culled under the interim LOD rule | ~2 per block renderer in the frustum | ≤ kits × LODs × slots per pass for the whole map, for example 4 × 3 × 2 = 24 |
| LOD / cull | LODGroup only after P-1; until then none (§4.4). Variant R1b: a dedicated layer plus `Camera.layerCullDistances` culls at 12 m without LODs, but needs a new TagManager layer and the game camera (map-owned `FrontRooms3DGame.cs` 247), and leaves every renderer alive | per block (6 m); needs its own distance switch | exact per outlet by camera distance; the same on every lodBias |
| Glass RT, 256 cap (`FrontRoomsMetalGlassRT.cs` 33–34, 145–157) | ~60–150 outlets within 18 m of a window enter the list, in InstanceID order | a few block renderers | invisible to it |
| Edit mode, captures, Level Designer | works | works | submit from `RenderPipelineManager.beginCameraRendering` with `RenderParams.camera` = that camera. That callback fires for the Scene view, the Game view, edit-mode `Camera.Render` captures and Play. **UNVERIFIED: test in the clone** |
| Needs | nothing new | readable meshes (importer exception, `FrontRoomsKitImporter.cs` 33) | instancing on the outlet slot materials (§4.3) |
| Material variety | no MaterialPropertyBlocks (SRP Batcher); variants as materials or meshes | same | same; world-space macro wear varies each instance for free |
| WebGL | too many renderers (WebGL target ≈ 1,200 renderers alive) | fine | fine; gate the radius per platform (WebGL-only, never lower desktop) |

- **R3 cost.** Gathering is per chunk: skip whole chunks whose bounds are over 12 m from the camera. That leaves about 4 chunks × ~83 outlets to test per camera per frame, which is well under 0.1 ms (ESTIMATE).
- **Frustum culling.** `RenderParams.worldBounds` covers one call, so frustum culling is per call. Distance culling does most of the work.
- **Pass coverage.** `RenderMeshInstanced` draws the material's passes like a normal renderer (DepthNormals for SSAO, Forward). Shadows stay off through `RenderParams.shadowCastingMode`.

---

## 5. Title corridor (`Assets/Scripts/FrontRoomsRoomStream.cs`, visual-owned)

- **Rooms.** A pool of 5 slots (`MaxRooms` 15), each 11.5 × 12 × 2.9 m (16–18).
  - The side walls are 0.26 thick (34), with inner faces at x = ±5.62.
  - The far end wall holds a 2.4 m double door (`BuildDoor` 1209+).
  - All five rule variants (Lobby, Shift, Office, Run, Exit) are built once per slot at `BuildRoom` (`BuildProfileProps` 1406–1498). The active one is toggled (`RefreshRoomMaterials` 1545–1547).
  - A recycled slot keeps its props (`MaintainPool` 875–920, sequence moved at 900). Variants are seeded by the slot's sequence at build time (comment 1430–1434), which means fixed per slot.
- **Placeholder outlets** (1418–1427), in the Lobby, Shift and Exit variants only:
  - 3 plates per variant, at (x, z) = (−5.614, 2.3), (+5.614, 5.8), (−5.614, 9.1);
  - box 0.012 × 0.115 × 0.07 m, centre y = 0.32;
  - material `Level 0 / outlet plate` (1402), shadows off.
- **They have colliders.** `Box` (1785–1801) adds a BoxCollider (1799–1800).
  - The game's E ray uses `Physics.Raycast` on all layers (`FrontRooms3DGame.cs` 992). It can stop on a plate.
  - That makes 9 colliders per slot and 45 in the pool. All of them must go.
- **Baseboards** (1074–1075) are centred at y 0.18, 0.16 tall, so the top is at 0.26. They stand 0.06 m proud of the wall face.
  - The placeholder plate's bottom (0.2625) sits 2.5 mm above the baseboard top.
  - An outlet's lowest centre here is about 0.26 + clearance + half the plate height. This is why `Face.baseTop` exists.
- **Other variants.**
  - Office (1428–1439) calls the 5-argument `FrontRoomsOfficeKit.Dress` overload. That path places its own columns. A clear centre lane is passed as the only keep-clear strip.
  - **Run** (1440–1492) has no outlets. Its hospital look would want its own rule. Period detail for hospitals (for example, colour-coded emergency-branch receptacles) is UNVERIFIED and left to the research files.
  - **Side finding: an unreachable branch.**
    - The first branch already includes Exit (`rule == Lobby || Shift || Exit`, 1418). So the later `else if (rule == RoomRule.Exit)` (1493–1496) never runs, and its "exit threshold marker" is never built.
    - This is harmless today. Fix it in the same visual-owned edit that replaces the placeholders: either drop the branch, or move Exit out of the first one.
- **Hook.** In `BuildProfileProps`, replace the boxes with `FrontRoomsWallFixtures.Plan` over the slot's faces.
  - Side walls: 12 m each, as four 3 m faces, so the same per-face rule runs.
  - End-wall spans: 4.55 m each side of the door.
  - Pass `baseTop` = 0.26 and seed = the slot sequence at build time.
  - Install under the variant's `props` root, so variant toggling hides them too.
  - **No contract is needed:** the file is visual-owned.

---

## 6. Office dressing: outlets near furniture

What the dress step can add that option B cannot, all inside `Office/` with no map change:

1. **Drop hidden outlets.** Outlets fully behind vending machines, copiers and filing runs are invisible. Fewer instances means fewer draws.
2. **Powered panels.** For each wall row (`PlaceWallRows`), keep or move one outlet behind the row's end panel at base height. The row's base feed reads as plugged in, which is a 1990 open-plan detail to confirm in the research files.
3. **Cords.** Vending machines, copiers and water coolers get a cord or plug to the nearest outlet behind them.
4. **Floor boxes.** Free-standing pods (`PlacePods`) take power from a floor box or power pole under the spine (`Kit_FloorBox…`), not from the wall.

**How Dress reaches the plan without a new map signature.**
- Option B installs a `FrontRoomsWallFixtureSet` component on the chunk root.
- `Dress` receives that root as `parent` (MapWorld 1555), so it can call `parent.GetComponentInParent<FrontRoomsWallFixtureSet>()`.
- It then refines the outlets on its room's faces. To find them, it uses the `room` index or the faces whose plane lies on its floor rect's edges.
- The reflection binding by parameter types (1439–1443) stays untouched.
- In RoomStream's Office variant, `parent` is the variant's `props` object, where the stream installs its own set.
- The look-dev hall calls `Dress` with no set. With no set, Dress does no refinement.
- `floorXZ` and the faces are in the same frame: chunk-local metres, because `parent` is the chunk root.
- **Ordering.** Outlets show unrefined for the few frames until their room is dressed. That is invisible: chunks build ≥ 24 m away, and outlets cull at 12 m. `RebuildChunk` dresses synchronously (330).

---

## 7. Integration options

| | **A. Per face, after each `BuildEdge` call** | **B. Once per chunk, after the shell (recommended base)** | **C. Inside the dress step** |
|---|---|---|---|
| Call site | inside the cell loop, 835–842 (map hot path) | `BuildInto` after the Collision object (873), before the key (875) | `FrontRoomsOfficeKit.Dress` (visual-owned) |
| Coverage | all faces of edges this chunk owns | all faces looking into this chunk's cells | dressed rooms only: **6.6 % of faces (Office 1.5 %)**; corridors (87 %) get nothing |
| Who owns an outlet | the edge's chunk: 9.8 % of outlets sit in the neighbour's rooms but live with this chunk | the chunk whose cell the face serves | the room's chunk |
| Determinism | edge inputs pure, but a border face's room context belongs to the neighbour, whose revision can change without this chunk rebuilding | every input is the chunk's own data or seed-pure borders | `roomSeed` includes the revision; deterministic |
| Data at hand | kind, exact opening span, trim, both materials; no room, no corners | everything in §1.3 except furniture, gathered once from the arrays | floor rect, keep-clear strips (whole 3 m edges), furniture; no arch span, door position or corridor faces |
| Furniture awareness | none | none (but never intersecting, §2.2) | full |
| Streaming cost | 128 calls per chunk inside the chunk-build frame | one call per chunk; gathering **0.07–0.13 ms** measured in the clone (§8.4); stub plan + install 0.13–0.89 ms; R1 would add 34–147 Instantiates | inside Dress's ≤ 3 ms room budget |
| Map change | many lines in `BuildEdge` | **one call plus one ~60-line helper** (contract §8) | none |
| Title corridor | not applicable | same planner, own faces (§5) | Office variant through Dress |

**Recommendation: B + C.**
- B is the sub-logic of the room and wall logic: every wall face of every room and corridor gets its outlets from one pure function of seed + faces.
- C is a refinement layer that only the Office dress step applies, entirely inside Office/.
- "Once per room" is not a separate option. Most faces are in no room, and B already tags each face with its room index.

---

## 8. Proposed API and the exact contract request

### 8.1 New visual-owned file `Assets/Scripts/Office/FrontRoomsWallFixtures.cs`

```csharp
public static class FrontRoomsWallFixtures
{
    /// One wall face as the map sees it at build time. Chunk-local metres, Y up.
    public struct Face
    {
        public Vector3 origin;      // on the face plane (cell line + normal × WallHalf), floor level, at the edge's low-coordinate end
        public Vector3 along;       // +X or +Z
        public Vector3 normal;      // into the cell this face serves
        public float length;        // 3.0
        public FrontRooms.Map.EdgeKind kind;          // Wall, Arch, Door, Window (Open is never listed)
        public float openFrom, openTo, openBottom;    // opening span along the edge; 0.35 sill for a window
        public float ceiling;       // ceiling height of the cell served
        public float baseTop;       // top of any base trim on this face (0 on map walls today; 0.26 in the title stream)
        public bool cornerLow, cornerHigh;            // a wall of the same cell meets this end
        public bool zoneBorder;     // the cell behind belongs to another zone (level-transition trims may land here, N1)
        public FrontRooms.Map.GridCoord cell, behind; // world cells
        public FrontRooms.Map.ZoneTheme theme;
        public FrontRooms.Map.ZoneHeight height;
        public int room;            // index into MapChunk.rooms (topmost containing rect), -1 = corridor
    }

    /// One planned fixture. Chunk-local metres.
    public struct Fixture
    {
        public string kit;          // Kit_Outlet... / Kit_Jack... name from the spec
        public Vector3 position;    // plate back on the face plane, at the plate centre (the kit's origin)
        public Vector3 normal;      // = face normal; the kit's front (Blender -Y, Unity +Z) points along it
        public int face;            // index into faces: Dress finds its room's outlets through Face.room
        public byte variant;        // wear or colour variant: a separate mesh or material, never a MaterialPropertyBlock
    }

    /// Pure and deterministic: no Unity objects, no UnityEngine.Random, no static state.
    /// Hashes use MapHash.Hash(seed, cell.x, cell.y, salt + side), with salts of 409 and up and no revision (§3).
    public static void Plan(int seed, IReadOnlyList<Face> faces, FrontRooms.Map.MapChunk chunk, List<Fixture> into);

    /// Plan, then render it under chunkRoot: render-only (no colliders), shadows off, culled beyond ~12 m.
    public static FrontRoomsWallFixtureSet Install(Transform chunkRoot, IReadOnlyList<Face> faces, int seed,
        FrontRooms.Map.MapChunk chunk, List<Mesh> ownedMeshes);
}
```

- `FrontRoomsWallFixtureSet` (the component `Install` adds to the chunk root) goes in **its own file**, `Assets/Scripts/Office/FrontRoomsWallFixtureSet.cs`.
  - It is a MonoBehaviour that `AddComponent` creates, so its class name must match the file name.
  - It needs `[ExecuteAlways]`, so that OnEnable and OnDisable keep the R3 registry right in edit mode too. The clone test in §8.4 checks this.

The density, heights and margins come from the spec. The code needs these geometric inputs:
- corner inset: `WallHalf` + half the plate + margin;
- jamb clearance: `TrimFace` 0.07 + margin;
- the window span excluded (a 0.30–0.46 m centre overlaps the 0.35 sill);
- `baseTop`.

### 8.2 Contract request to the map chat (`FrontRoomsMapWorld.cs`)

- **Tested in the clone (§8.4).** This exact code was applied to `proj_outlet`'s copy of `FrontRoomsMapWorld.cs`. It compiles and passes on 3 seeds.
- **A direct call is normal here.** The map already calls visual-owned code directly:
  - `FrontRoomsKitLibrary.Spawn` (1648);
  - `FrontRoomsPostStack.EnsureZoneVolume` (1720);
  - `FrontRoomsSurfaces`.
- Only the two dressers are bound by reflection (`ResolveDressers` 1428–1450). If the map chat prefers that soft binding, `Install` can be resolved the same way. Its parameter types are fixed above.

```csharp
// (1) field
readonly List<FrontRoomsWallFixtures.Face> wallFaces = new List<FrontRoomsWallFixtures.Face>();

// (2) BuildInto: after the "Collision" object (line 873), before the key (line 875)
CollectWallFaces(data, wallFaces);
FrontRoomsWallFixtures.Install(chunk.root.transform, wallFaces, Cache.Generator.Seed, data, chunk.meshes);

// (3) helpers: read only this chunk's own arrays (no neighbour chunk is generated)
static readonly GridCoord[] FaceSides = { new GridCoord(1, 0), new GridCoord(0, 1), new GridCoord(-1, 0), new GridCoord(0, -1) };

static EdgeKind OwnEdge(MapChunk data, int i, int j, GridCoord d)
{
    var k = MapGrid.LocalIndex(i, j);
    if (d.x > 0) return data.east[k];
    if (d.y > 0) return data.north[k];
    if (d.x < 0) return i > 0 ? data.east[k - 1] : data.west[j];
    return j > 0 ? data.north[k - MapGrid.ChunkCells] : data.south[i];
}

/// Every wall face that looks into one of this chunk's map cells, including the faces of the
/// west and south border walls the neighbours build. Start-area cells, and faces whose wall
/// backs onto them, are left out (the stream rooms stand there).
void CollectWallFaces(MapChunk data, List<FrontRoomsWallFixtures.Face> into)
{
    into.Clear();
    const int n = MapGrid.ChunkCells;
    var cs = MapGrid.CellSize;
    for (var j = 0; j < n; j++)
    for (var i = 0; i < n; i++)
    {
        var cell = data.Cell(i, j);
        if (InStartArea(cell)) continue;
        var zone = Cache.ZoneOf(cell);
        var room = -1;
        for (var r = data.rooms.Length - 1; r >= 0; r--) if (data.rooms[r].Contains(i, j)) { room = r; break; }
        foreach (var d in FaceSides)
        {
            var behind = cell + d;
            var kind = OwnEdge(data, i, j, d);
            if (kind == EdgeKind.Open || InStartArea(behind)) continue;
            var alongX = d.y != 0;
            var normal = new Vector3(-d.x, 0f, -d.y);
            var line = new Vector3((i + (d.x > 0 ? 1 : 0)) * cs, 0f, (j + (d.y > 0 ? 1 : 0)) * cs);
            float o0 = 0f, o1 = 0f, bottom = 0f;
            if (kind == EdgeKind.Arch) { ArchOpening(d.x + d.y > 0 ? cell : behind, alongX, out var w, out var c); o0 = c - w * .5f; o1 = c + w * .5f; }
            else if (kind == EdgeKind.Door) { o0 = (cs - DoorWidth) * .5f; o1 = (cs + DoorWidth) * .5f; }
            else if (kind == EdgeKind.Window) { o0 = (cs - WindowWidth) * .5f; o1 = (cs + WindowWidth) * .5f; bottom = WindowSill; }
            var t = alongX ? new GridCoord(1, 0) : new GridCoord(0, 1);
            into.Add(new FrontRoomsWallFixtures.Face
            {
                origin = line + normal * ModuleUnits.WallHalf, along = alongX ? Vector3.right : Vector3.forward, normal = normal,
                length = cs, kind = kind, openFrom = o0, openTo = o1, openBottom = bottom,
                ceiling = MapGrid.CeilingHeight(zone.height), baseTop = 0f,
                cornerLow = OwnEdge(data, i, j, new GridCoord(-t.x, -t.y)) != EdgeKind.Open,
                cornerHigh = OwnEdge(data, i, j, t) != EdgeKind.Open,
                zoneBorder = Cache.ZoneOf(behind).id != zone.id,
                cell = cell, behind = behind, theme = zone.theme, height = zone.height, room = room,
            });
        }
    }
}
```

- **No other map change.**
  - `Unregister`, `Drop` and `RebuildChunk` already free everything under the root and in `chunk.meshes`.
  - `Prewarm` (1588–1593) already loads every kit from `AllNames()`.
- **Phase 2, optional, also map-owned:**
  - `RoomModuleData.outlets` (`ModuleOutlets { Auto, None }`), with one enum field in the Level Designer;
  - a "flush wall" case in `FrontRoomsModuleEditing.NewProp` for kits tagged `wall_flush`: gap `WallHalf`, y from a `mount` anchor;
  - optionally, leave `noCollider` wall-flush props out of the obstacle list in `PlaceProps` (1657–1659).
- **Visual-owned changes (no contract):**
  - the new file;
  - the `FrontRoomsOfficeKit.Dress` refinement (§6);
  - RoomStream placeholders → planner (§5);
  - the LOD switch distances: the interactables spec's P-1 (`make_lods` + `lodDistances` in the importer, §4.4), shared with that kit, **needs visual-chat approval**;
  - instancing on the outlet materials in `FrontRoomsRenderSetup`.

### 8.3 Tests in the clone, before the contract is sent

1. **Determinism.** Build, drop and rebuild a chunk: identical plans. Shift a chunk: its faces that did not change keep their outlets. A border face's outlets never change when only the neighbour shifts.
2. **Render-only.** No Collider under any fixture root. `IsArchitecture` (642) and the Relay's sight ray (`FrontRoomsMapHunter.cs` 992) see no change. The Relay nav, the 44 interaction tests and the 85 Level Designer tests pass unchanged. The interaction tests inspect only colliders under `office dressing` (`FrontRoomsMapInteractionTests.cs` 572, 587, 789).
3. **Geometry.** Over 100 seeds, no outlet within the corner inset, a jamb's trim or a window span; none in start-area cells; plate back on the face plane (±1 mm).
4. **Cost.** Install time per chunk, outlets alive, and draws added in the autopilot (`FrontRoomsMainScenePlaytest`, seeds 2554 and 20388). Against spec §8: ≥ 55 fps, p99 ≤ 33 ms.

### 8.4 Clone test, done 2026-10-03 (~11:55)

**Setup.**
- The §8.2 code was applied to `scratchpad/proj_outlet` only.
  - The clone-only patch script is `data/02_contract_patch.py.txt`. It adds a stopwatch, which is test instrumentation and not part of the request.
  - The original is backed up in `scratchpad/outlet_logs/FrontRoomsMapWorld.cs.orig`.
- A stub `FrontRoomsWallFixtures` with a placeholder rule (one hashed outlet on 60 % of faces) is in `data/02_wall_fixtures_stub.cs.txt`.
- The probe is `FrontRoomsOutletContractProbe.Run` (`data/02_contract_probe.cs.txt`).
  - It builds the real map with `BuildForCapture` at root (4992, 0, 4992), with the default profile.
  - It ran in batch mode with `-nographics`, twice, on seeds 20261001, 2554 and 20388.
  - Results: `data/02_contract_probe.json`.

| Check | Result (3 seeds × 25 chunks, both runs) |
|---|---|
| Faces from `CollectWallFaces` vs an independent walk over `Cache.Edge` (all 4 sides of every cell) | 3,231 / 3,193 / 3,236 faces; **0 missing, 0 extra, 0 kind mismatches** (so `OwnEdge` reads the west and south border copies correctly) |
| Arch spans: face opening centre vs `FrontRoomsMapWorld.CrossingPoint` | 1,117 / 969 / 1,014 arches; **0 mismatches**; worst error 0.5 mm (float) |
| Plate centres: ray from 0.30 m in front, along −normal, must hit the shell collider at 0.30 m | 1,944 / 1,860 / 1,887 plates on solid wall at plate height, worst error 0.1 mm; **0 bad** |
| Plates on a face whose wall the unbuilt neighbour owns (ring edge, §3) | 35 / 41 / 39, which is 1.8–2.1 % of plates. All are on the ring's west or south edge, ≥ 48 m from the player |
| Rebuild (`RebuildChunk`) of 3 chunks | **identical faces and plans**; the set registry stays at 25; 0 sets left after the map is destroyed (`[ExecuteAlways]` OnDisable works in edit mode) |
| Colliders added | 0 |
| `CollectWallFaces` time per chunk | **0.07–0.13 ms** in 5 of 6 seed runs; one outlier of 1.8 ms (run 1, seed 20388: GC or machine load, with 3 other Unity processes running) |
| Stub `Install` (AddComponent + copy + plan, no rendering) per chunk | 0.13–0.89 ms; one outlier of 3.2 ms in the same run |

- **Context.** A chunk build costs about 18 ms (`research/webgl/03_measured_budgets.md` 68). The contract adds about 0.2–1 % to it.
- **Not tested yet** (they need the real planner and renderer):
  - a revisit shift and a neighbour-only shift;
  - the Relay nav, interaction and Level Designer test suites;
  - the R3 render path, including its edit-mode submit;
  - the autopilot frame budget.

---

## 9. Cross-dependencies and risks

- **N1 level transitions** may add trims or base pieces at zone borders and change `BuildEdge`.
  - `Face.zoneBorder` and `baseTop` let the planner keep clear.
  - Re-check after N1 lands.
- **LOD2** needs the interactables spec's P-1 pipeline change (§4.4), which needs visual-chat approval.
  - Outlets use the same convention (`<NAME>_LOD0/1/2`, `LOD_DISTANCES`).
  - Until P-1 lands they ship without a LODGroup, and R3 does the culling.
- **R3 in edit mode.** Instanced draws are not GameObjects.
  - Submit them from `RenderPipelineManager.beginCameraRendering`, per camera. That should cover the Scene view, the Level Designer preview and `Camera.Render` captures.
  - Fallback: R1 when `!Application.isPlaying`.
  - UNVERIFIED: test it in the clone with the capture harness.
- **Glass RT cap.** GameObject outlets would compete for the 256 RT instances (§4.5). R3 avoids it. If the glass track later wants outlets in reflections, it would need an instanced-draw path, which is not worth it at 0.11 m.
- **WebGL is a separate track.** Gate the instanced radius per platform (for example, `Application.platform == WebGLPlayer`, as `TickFixturesNearOnly` does at 1263). Never lower desktop.
- **Sound.** If the sound chat wants hum or spark points, expose `FrontRoomsWallFixtureSet` positions the way `RelayEntries` does (2006–2010). There is no audio work in this task.

## 10. Reproduce the census

```
cp Documentation/research/outlets/data/02_face_census.cs.txt <clone>/Assets/Editor/OutletCensus/FrontRoomsOutletFaceCensus.cs
OUTLET_CENSUS_OUT=<out.json> /Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -projectPath <clone> -executeMethod FrontRoomsOutletFaceCensus.Run -logFile <log>
```

- Run it in a private clone only, never on the real project.
- The tool copies `MapHash.Hash` and checks the copy against the real one by reflection. It also copies `ArchOpening`.
- Solid metres subtract opening spans and a 0.08 m inset at each inner corner.
- Timings are editor Mono, second (warm) pass, with other Unity processes running on the machine.

## 11. Reproduce the contract probe (§8.4)

Run this in a private clone only. It patches the clone's `FrontRoomsMapWorld.cs`.

```
python3 Documentation/research/outlets/data/02_contract_patch.py.txt <clone>/Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs
# split data/02_wall_fixtures_stub.cs.txt at "// CLONE-ONLY STUB (outlet workflow). See" into
#   <clone>/Assets/Scripts/Office/FrontRoomsWallFixtures.cs and .../FrontRoomsWallFixtureSet.cs
cp Documentation/research/outlets/data/02_contract_probe.cs.txt <clone>/Assets/Editor/OutletCensus/FrontRoomsOutletContractProbe.cs
OUTLET_PROBE_OUT=<out.json> /Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -projectPath <clone> -executeMethod FrontRoomsOutletContractProbe.Run -logFile <log>
```

- The exit code is 0 on PASS and 1 on FAIL.
- `proj_outlet` keeps the patch and the stub for the next outlet tasks. The real planner replaces the stub there.
