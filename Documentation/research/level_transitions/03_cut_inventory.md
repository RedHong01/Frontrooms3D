# Level transitions · 03 · Cut inventory

Where and how FrontRooms cuts between looks today. 2026-10-03.
Read-only on Red's project. All renders come from the private clone `proj_trans` (same Scripts, Levels and Surfaces as the real project, md5-checked today).
Evidence: `images/cut_*.jpg`, sheet `images/cut_contact_sheet.jpg`. Fixed shots: `shots.json`, `images/shot1..4_before.jpg`, sheet `images/shots_before_sheet.jpg`.

## 0. Short answer

- Every look change happens on a 3 m cell line, with **no transition piece at all**: no casing, no threshold strip, no bulkhead, no baseboard, no paint break. Wall paper, carpet, ceiling grid, ceiling height and lamp level all switch on the same line.
- The map has two themes (Level 0, Office) and three heights (Low 2.4, Standard 2.9, Tall 5.4). Office is always Standard.
- **The edge kind on a border is chosen by height only** (`FrontRoomsMap.cs:360-373`). An Office | Level 0 border at the same height (Standard | Standard) is treated like the inside of one zone. So it gets the normal maze grammar: open, arch or wall. A room can even straddle the line and stay one open space (`FrontRoomsMap.cs:369`, `:625-630`).
- Over 20 seeds x 81 chunks (1620 chunks): **56 % of chunks contain an Office | Level 0 border**, with **5.24 theme-border edges per chunk** (9.4 in a chunk that has one). 24 % of those edges have no wall, door or trim at all (open 17 %, arch 7 %).
- **Worst on screen**, in order: (1) open edges, where floor, ceiling and both walls switch on one line in the open; (2) flat wall seams, where one wall plane changes paper mid-plane; (3) arches, whose jambs and head are half wallpaper, half drywall; (4) corner-post z-fight stripes. Doors and windows are the least bad: the trim frame hides the reveal.
- The title stream to map handoff is Level 0 to Level 0 by design (`FrontRooms3DGame.cs:667-672`), so it is mostly clean. RoomStream rule changes (Shift, Office, Run, Exit) are **not reachable in the current game**: the title is Lobby only.

## 1. How the map dresses each part, per zone

Zone = Voronoi cell of a per-chunk site (`FrontRoomsMap.cs:292-338`). Theme = Office only when the zone is Standard and wins the office roll (`:306-307`). Materials come from `FrontRoomsSurfaces.Room(rule, slot)`: Level 0 = Lobby (`L0_Wallpaper`, `L0_Carpet`, `L0_Ceiling`), Office = `Office_Wall`, `Office_Carpet`, `Office_Ceiling` (`FrontRoomsMapWorld.cs:2044-2060`, `FrontRoomsSurfaces.cs:20-30`).

| Part | Material | Height | Code |
|---|---|---|---|
| Floor slab, 3 x 3 m per cell | cell's theme floor | 0 | `FrontRoomsMapWorld.cs:817` |
| Ceiling slab, 3 x 3 m per cell | cell's theme ceiling | cell's zone height | `:818` |
| Wall on a cell edge, 0.16 m, centred on the line | same theme both sides: one box; different themes: two 0.08 m skins, each in its own room's paper | max of the two sides | `:827-831`, `:961-969` |
| Wall ends | every piece that reaches a corner runs 0.08 m past it (butt overlap, no mitre); pieces that end at an opening do not | | `:953-959`, spec `LEVEL_MODULE_SPEC.md:29` |
| Arch (doorless doorway) | wall skins only, **no trim** | top 2.2 m (or 0.2 under a lower ceiling), width 1.1-1.8 m, off-centre | `:975-979`, `:993-998`, `:1084-1090` |
| Door, window | wall skins + trim frame (`Cove_Base`, 0.07 face, jamb 0.20 deep, covers the reveal) | door 1.0 x 2.1; window 1.4, sill 0.35, top 2.0 | `:980-992`, `:1000-1009` |
| Door leaf / glass | `Door_Veneer` / map glass, same on both sides | | `:1013-1053`, `:2067-2070` |
| Column + cove base | cell's theme wall + `Cove_Base` 0.1 m | cell height | `:1064-1069` |
| Office bulkhead | Office wall, 0.35 m deep, Office only | under the ceiling | `:1070-1076` |
| Troffer lens + spot light | lens: `Troffer_Lens` for both themes (OfficeLouver now returns it); light colour (1, .96, .88) for both; intensity Level 0 5.0, Office 5.5 | at the cell's ceiling | `:1173-1227`, `FrontRoomsSurfaces.cs:43-47` |
| Lamp temperament | per cell from seed and tier, not from zone | | `:1217-1223` |
| Office post grade | a local Volume over Office cells, 2.5 m blend | camera position based | `:1686-1728`, `FrontRoomsPostStack.cs:41-62` |
| Wall grime band | `_CeilingHeight` per 6 m block and height class; a border wall sits in the **taller** side's block | | `:783-795`, `:836`, `:840`, `:889`, shader `FrontRoomsSurface.shader:219-223` |
| Baseboard | **none** on any map wall (only the column cove) | | grep: `CoveBase` used only at `:1069`, `:2067` |

Where edges come from: `Resolve` (`FrontRoomsMap.cs:360-373`). Different heights: door (Low | Standard) or window (any Tall side) if the border needs an opening or rolls one, else wall. Same height: open inside a room, else arch / open / wall by the height's grammar. **Theme is never checked.** Columns and modules only go into uniform rooms (same height and theme: `:402-412`, `:529-531`, `:435`), so they never sit on a border.

## 2. Cut inventory

Per chunk = mean over 1620 chunks (20 seeds x 81 chunks, chunks -4..4 round (0, 0)). A chunk is 24 x 24 m, 64 cells, 128 edges; each edge is counted once. Counted by `FrontRoomsTransitionAudit.CountBatch` straight from the generator (`logs/counts.md`, `logs/counts_per_chunk.csv`, `logs/counts_per_seed.md`). Seed range = lowest and highest seed mean.

Severity on screen: 5 = reads as a bug at normal walking distance; 1 = reads as plausible building; 0 = invisible.

| # | Cut type | What happens | Code | Per chunk | Chunks with >= 1 | Seed range | Severity | Evidence |
|---|---|---|---|---:|---:|---|---:|---|
| A | **Open edge** on an Office \| Level 0 border (maze edge or a room that straddles the zone line) | Nothing is built. Floor, ceiling grid and both side walls switch on one straight line across the walking path. 3 m of floor cut + 3 m of ceiling cut each. | `FrontRoomsMap.cs:369-372`, `FrontRoomsMapWorld.cs:949`, `:817-818` | 0.89 | 28.3 % | 0.54-1.20 | **5** | `cut_open_1..5`, `cut_floor_1..2`, `cut_ceiling_1..2`, shot2, shot4 |
| B | **Arch** on an Office \| Level 0 border | No trim. Jambs and head are the two 0.08 m skins: a vertical seam runs down the middle of each jamb and across the soffit. Floor cut on the wall centre line inside the opening. Avg 1.45 m of floor cut. | `FrontRoomsMapWorld.cs:975-979`, `:998` | 0.37 | 21.0 % | 0.22-0.54 | **4** | `cut_arch_1..5`, shot1 |
| C | **Flat wall seam**: a wall line runs straight across the zone change with no wall meeting it there | Paper changes mid-plane, on the cell line, at full height. Usually comes with A (corridor walls). | split skins `:961-969`, no corner piece | 0.61 | 26.7 % | 0.37-0.88 | **4** | `cut_seam_1..4`, shot2 |
| D | **Corner-post z-fight**: at an L or T on the border, two wall pieces of different paper share one coplanar 0.16 m post face | A 0.08-0.16 m vertical stripe of the other paper, full wall height, that swims as the camera moves. | overlap `:953-959` + skins `:967-969` | 3.17 | 55.3 % | 2.15-4.30 | **3** | `cut_post_1..2` |
| E | **Split wall end**: a free wall end (cap) on the border | The 0.16 m end cap is half wallpaper, half drywall. | `:967-969` | 0.17 | 12.2 % | 0.02-0.33 | 2 | `cut_wallend_1..3` |
| F | **Door** on a theme border (always Level 0 Low \| Office, so also a 0.5 m height step) | Trim covers the reveal. Wall paper, carpet (under the leaf), ceiling grid and height all change in the door plane. No threshold strip. | `FrontRoomsMap.cs:364-368`, `FrontRoomsMapWorld.cs:980-1039` | 1.37 | 35.9 % | 0.84-2.02 | 2 | `cut_door_1..4`, shot3 |
| G | **Window** on a theme border (Level 0 Tall \| Office) | Trim frame; paper and floor change at the glass. Tall side shows 3.4 m of wall over the head. | same | 0.33 | 11.6 % | 0.05-0.95 | 2 | `cut_window_1..3`, `cut_step_4` |
| H | **Solid wall** between themes | Two skins; each room sees only its own paper. Reads as a real wall. Only its ends (D, E) leak. | `:972` | 2.28 | 53.0 % | 1.63-3.06 | 1 | `cut_wall_1..3` |
| I | Inside-corner seam: the change lands where a perpendicular wall meets | Hidden in the corner. Looks real. | | 2.76 | 51.7 % | 1.89-3.74 | 0 | |
| J | **Height step through a door or window** (any theme) | The only face that closes the step is the wall over the opening: 0.8 m of header on the 2.9 m side and 0.3 m on the 2.4 m side of a door; 3.4 m over a window on the 5.4 m side. Ceiling height jumps in the door plane. No soffit, no bulkhead. | `FrontRoomsMapWorld.cs:827-828`, `:993-996` | 5.81 (Low-Std 4.11, Std-Tall 1.04, Low-Tall 0.66) | 89.4 % | 4.90-6.85 | 2 | `cut_step_1..4`, shot3 |
| | of which same theme, height only | Level 0 Low \| Standard doors, Level 0 Tall windows | | door 2.74, window 1.37 | | | | `cut_step_1..3` |
| K | Height step behind a plain wall | Each side sees its own ceiling; the step is invisible. | `:972` | 4.94 | | | 0 | |
| L | **Grime band reference** on height-border walls (code finding, no close frame yet) | The whole wall is in the taller side's block, so its ceiling-streak band is placed for the taller ceiling. On the low side of a Low \| Standard wall it sits 0.5 m too high; beside a Tall hall (5.4 m) the band is hidden above a 2.4 / 2.9 m ceiling, so that face has no streaks while its neighbours do. | `:836`, `:840`, `:795`, shader `:220` | 10.74 edges (7.53 Low-Std, 3.21 with a Tall side) | 92.5 % | | 1 | not framed |
| M | Floor material change | Always on a cell line: 3 m at an open edge, 1.1-1.8 m in an arch, 1.0 m under a door leaf. Loop pile to carpet tile, no threshold. | `:817` | 4.58 m | 50.2 % | 3.12-6.19 m | part of A, B, F | `cut_floor_1..2` |
| N | Ceiling material change at the same height | 2 x 4 grid to 2 x 2 grid on the cell line. Nothing marks it. | `:818` | 2.67 m | 28.3 % | 1.63-3.59 m | part of A | `cut_ceiling_1..2` |
| O | Lamps | Same lens and colour on both sides; Office lamps are 10 % brighter. Light changes per cell. The Office post grade blends over 2.5 m, but by camera position: the whole screen shifts as you walk, it is not a place in the world. | `:1190`, `:1205`, `:1210`, `:1720` | per cell | | | 1 | |
| P | Columns, bulkheads | Never on a border (uniform rooms only). | `FrontRoomsMap.cs:529-531` | 0 | 0 % | | 0 | |
| Q | Baseboards | The map has none, so nothing stops or continues at a border. The title stream rooms do have them (see R). | | 0 | | | 0 | |
| R | **Title stream to map handoff** | Once per run. The door row is forced to Level 0 Standard; the terminal room is Lobby, which uses the same L0 materials, so paper, carpet and ceiling continue. Real changes: wall 0.26 m to 0.16 m; the stream's 0.16 m baseboards stop at the door; a 2.4 m double door among 1.0 m map doors; Lobby lamps 5.2 vs map 5.0. The start area's west, east and south walls take the outside cell's paper and height (`StartAreaEdge`), so behind the stream rooms they can be Office or another height. | `FrontRooms3DGame.cs:538-575`, `:654-682`; `FrontRoomsMapWorld.cs:924-935`; `FrontRoomsRoomStream.cs:502-537`, `:593-601`, `:1074-1075` | 1 per run | | | 1 | `cut_handoff_1..4` |
| S | **RoomStream rule change** (Lobby, Shift, Office, Run, Exit) | **0 per run today.** The title is Lobby only (`FrontRoomsRoomStream.cs:406-411`); `BeginPlayableSequence` is only called by `Editor/FrontRoomsStreamVerification.cs:80`. If it comes back: wall, floor, ceiling, lamp colour and intensity all switch in the door plane (Run 0.6 vs Office 6.0). The door wall's 0.04 m returns stay in the near room's paper; its far-side reveals take the next room's paper (`:1532-1536`). The two rooms' floor and ceiling slabs overlap 0.04 m, coplanar, across the door line (`:1070-1071`); no flicker shows in our frames. | `FrontRoomsRoomStream.cs:1056-1127`, `:1209-1299`, `:1351-1377` | 0 | | | 3 if played | `cut_stream_1..7` |

Shares of the 5.24 theme-border edges per chunk: wall 43.5 %, door 26.1 %, open 17.0 %, arch 7.0 %, window 6.3 %. Frameless crossings (open + arch): 1.26 per chunk, in 32.0 % of chunks. Office is 16.3 % of all cells.

### Worst on screen, and why

1. **A, open edges (and rooms that straddle the line).** The cut crosses the walking path and lands on floor, ceiling and both walls at once, with nothing that could be a doorway. It is the right half of Red's screenshot (`shot4_before`). 0.89 per chunk, 28 % of chunks.
2. **C, flat wall seams.** A long corridor wall changes paper on a hard vertical line. Comes with A. 0.61 per chunk.
3. **B, arches.** The opening looks like a cut through a sandwich: half paper, half drywall on every jamb and on the head. It is the doorway in Red's screenshot (`shot1_before`). 0.37 per chunk.
4. **D, corner-post z-fight.** Thin, but the most common artefact (3.17 per chunk, 55 % of chunks) and it moves with the camera.
5. Then F/G/J doors, windows and height steps: plausible, because a framed opening is where buildings change finish. Their weak point is that the wall plane, the floor and the ceiling all change exactly in the door plane, with no casing on the wall field, no threshold and no soffit.

## 3. The nine cases asked for

(a) **Zone border with a door.** Only Low \| Standard borders get doors, so a theme-border door is always Level 0 Low \| Office. The wall is two skins (each room's paper), framed by `Cove_Base` trim that covers the reveal. Floor, ceiling grid and ceiling height change in the door plane; the floor cut runs under the leaf. Header over the door: 0.8 m of drywall (Office side), 0.3 m of wallpaper (Low side). `cut_door_1..4`, shot3.

(b) **Zone border with a window.** Same as (a) with a 1.4 m glass. On the Tall side the wall over the head is 3.4 m of paper. `cut_window_1..3`.

(c) **Solid wall seen from both sides.** Two 0.08 m skins; from each side you see only your own paper. Clean, except where the wall ends (E) or turns (D). `cut_wall_1..3`.

(d) **Wall plane across a zone change, T-junctions, corners.** The paper switches on the cell line. With a perpendicular wall there, the seam sits in an inside corner and hides (I). Without one, it is a flat seam (C). At L and T posts on the border, two papers can share one coplanar post face and z-fight (D). Free ends show a split cap (E). `cut_seam_1..4`, `cut_post_1..2`, `cut_wallend_1..3`, shot2.

(e) **Ceiling height step.** Steps exist only behind walls, doors and windows (no open or arch edge between heights in 1620 chunks: the counter's `step_UNEXPECTED` key never fired). The face that closes a step is the wall itself, raised to the taller side (`:827-828`). Through a door the step reads as a 0.5 m (Low-Std), 2.5 m (Std-Tall) or 3.0 m (Low-Tall) jump right at the door plane. No soffit, no bulkhead. `cut_step_1..4`, shot3.

(f) **Floor material change.** Per-cell slabs, so the change is always on a cell line, butt-jointed, no threshold. 4.58 m of floor cut per chunk. `cut_floor_1..2`.

(g) **Arch or open edge between zones.** Both exist and are the worst cases (A, B). Open: 0.89 per chunk. Arch: 0.37 per chunk. `cut_open_1..5`, `cut_arch_1..5`.

(h) **Title stream to map handoff.** Level 0 Lobby to Level 0 Standard by design, same materials. Small cuts: wall thickness, baseboards end, door type, lamp level. The start area's side and back walls take the neighbouring cell's paper and height. `cut_handoff_1..4` (Play mode, run seed 516574485).

(i) **RoomStream room-to-room.** Not seen in today's game. In the preview every surface and the light switch at the door plane; Run is the strongest jump (0.6 vs 6.0 intensity). `cut_stream_1..7` (edit-mode preview rooms, with Play's far-side reveal paper restored; see §5).

## 4. Fixed comparison shots

Every variation renders these four, from the same harness, unchanged. Full definitions: `shots.json`. BEFORE frames: `images/shot1_before.jpg` .. `shot4_before.jpg`, sheet `images/shots_before_sheet.jpg`. Plan with the four frusta: `images/cut_plan_1.jpg`.

All four: run seed **516574485** (the run that `Random.InitState(4242)` gives before Space; the G10 audit seed). Map root (4992, 0, 4992). Eye 1.62 m. Vertical FOV 76. 1920 x 1080, 4x MSAA, the game's camera and post. World = map + (4992, 0, 4992).

| Shot | Shows | Eye, map space (m) | Cell, zone | Yaw / pitch (deg) | Open doors | Boundary |
|---|---|---|---|---|---|---|
| shot1 | Doorway, Level 0 to Office (B) | (794.0, 1.62, 606.1) | (264, 202) Level 0 Standard | -10.36 / 6.2 | none | Arch on Z = 609.0 between (264, 202) and (264, 203) Office, centre X 793.47, top 2.2 m; split jambs; floor cut in the opening. Right: arch (264, 202) \| (265, 202) Office on X = 795. Camera 2.9 m from the arch. |
| shot2 | Continuous wall planes across a zone change (A + C) | (766.75, 1.62, 584.9) | (255, 194) Level 0 Standard | -3.0 / 2.0 | none | Open edge on Z = 588.0, X 765-768, (255, 195) Level 0 \| (255, 196) Office. Both corridor walls switch paper on that line (flat seams); floor and ceiling switch too. Camera 3.1 m from the cut. |
| shot3 | Ceiling height step (F + J) | (760.25, 1.62, 597.3) | (253, 199) Office Standard | 5.29 / 0.0 | (253, 199) to (253, 200) | Door on Z = 600.0, centre X 760.5, Office 2.9 m \| Level 0 Low 2.4 m. Leaf pushed into the Low room. 0.8 m drywall header; paper, carpet, grid and height change in the door plane. Camera 2.7 m from the door. |
| shot4 | Wide, two zones in one frame (A + B + H) | (793.5, 1.62, 601.5) | (264, 200) Level 0 Standard | 0.0 / 1.15 | none | Red's screenshot (G10 frame 01). 7.5 m ahead: shot1's arch. Right: open edge (265, 201) \| (266, 201) Office on X = 798, and split walls. |

**Harness.** `FrontRoomsTransitionAudit.ShotsBatch` in `Assets/Editor/Audit/FrontRoomsTransitionAudit.cs` (clone `proj_trans`; copy at `harness/FrontRoomsTransitionAudit.cs.txt`, md5 `76a624e3217630e637bdb381af7ca5b1`). It opens `Assets/Scenes/FrontRooms3D.unity` in edit mode, builds the map with `FrontRoomsMapWorld.BuildForCapture` (5 x 5 chunks, rooms furnished), restores the scene's own ambient and fog, opens the listed doors, stands the player at the eye (lamps light and shadow for that point; lamp clock frozen at 0), and renders with the scene's "First-person camera".

```
/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity -batchmode \
  -projectPath <your clone> -executeMethod FrontRoomsTransitionAudit.ShotsBatch -quit \
  -logFile <log> -shots <path>/shots.json -out <dir> -tag <variation>
```

Writes `<out>/shot<k>_<tag>.jpg` and `.txt` (pose, eye cell zone, lamp count). Needs graphics (no `-nographics`). `harness/run_unity.sh.txt` waits until no other Unity has the clone open. **Render all four in one call.** URP seeds film grain with `Time.frameCount`, so `-only` subsets shift the grain (up to 13/255, mean 0.5/255). A full run reproduces the BEFORE frames byte for byte (shot1, 2, 4 identical over three runs; shot3 identical between full runs).

## 5. Method

- **Code reading**: `FrontRoomsMapWorld.cs` (2090 lines), `FrontRoomsMap.cs`, `FrontRoomsModuleUnits.cs`, `FrontRoomsRoomStream.cs`, `FrontRooms3DGame.cs`, `FrontRoomsSurfaces.cs`, `FrontRoomsPostStack.cs`, `FrontRoomsSurface.shader`, `Editor/Rendering/FrontRoomsRenderSetup.cs`, `LEVEL_MODULE_SPEC.md`.
- **Counts**: `CountBatch` asks the generator itself (`FrontRoomsMapCache.Get`) for each chunk's zones and edge kinds. Seeds: 516574485, 2554, 20388, 20261001, 1, 7, 42, 99, 123, 777, 1001, 1990, 2026, 3141, 4242, 5555, 8080, 9001, 12345, 31337. Post, seam and end-cap states are derived from which edges meet at each corner and which themes face each post face (harness `PostState`, `Junctions`).
- **Evidence frames**: `EvidenceBatch` scores every border within 3 chunks of each seed's centre (seeds 516574485, 2554, 20388), keeps the best 3 per type from different seeds, and renders A (Level 0 or low side), B (other side), W (wide), plus F (floor), C (ceiling), S (step side) or Z (close) views. Eye 1.62 m, 1.5-4 m from the cut, cleared of walls and props by sphere and capsule tests. Frame list: `logs/evidence_frames.txt`.
- **Handoff**: `FrontRoomsTransitionHandoff.RunBatch` (Play mode, the game's own title handoff, `Random.InitState(4242)` gives run seed 516574485, map root (-576, 0, -576), door cell (277, 198) Level 0 Standard, door opened 1.4 s after the walk-up). `logs/handoff_frames.txt`.
- **Stream**: `ExtrasBatch` renders the edit-mode preview rooms. The preview rooms are not in the stream pool, so their far-side reveals keep their own paper; the harness sets them to the next room's paper first, as `RefreshRoomMaterials` does in Play. `logs/stream_frames.txt`.
- **Plan**: top-down render with ceilings hidden, overlay by `harness/plan_overlay.py.txt`.

## 6. Notes for the variation work

- Every fix that touches walls, openings, floors, ceilings or edge rules lives in `FrontRoomsMap/*`, which the map chat owns. Changes there are **contract requests** with an exact proposal, never direct edits.
- The rule that matters most is one line: `Resolve` ignores theme. A theme border at the same height is built like the inside of a zone.
- D and L need close frames before and after any fix; the fixed shots will not show them well.
- Image index: 49 cut frames in `images/`, all JPG q85, 1920 x 1080 (plan included).
