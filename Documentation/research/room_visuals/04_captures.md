# 04 — Current-state captures: every room type as it renders today

Status: DONE (2026-10-03, re-shot at 11:58). **65 frames** and 3 contact sheets.
Made in a private clone. Red's project was not opened. Nothing in it changed except
this folder (`images/room_*`, `harness/`, this file).
The real project's `Assets/Scripts`, `Shaders`, `Resources`, `Levels`, `Scenes`, `Settings` and
`Editor/Rendering` were checksum-identical to the clone at 11:45, so these frames are today's code.

![All 65 frames](images/room_contact_sheet_all.jpg)

- Map rooms and the start rooms: [`images/room_contact_sheet_map.jpg`](images/room_contact_sheet_map.jpg)
- Title stream, live and the five profiles: [`images/room_contact_sheet_title.jpg`](images/room_contact_sheet_title.jpg)

---

## 0. Short answer: what the frames show

1. **In play, the only stream room a player ever sees is Lobby.**
   `FrontRoomsRoomStream.Initialize` sets `lobbyOnlyTitle = true`. Shift, Office, Run and Exit
   rooms switch on only after `BeginPlayableSequence()`. Only the editor test
   `Assets/Editor/FrontRoomsStreamVerification.cs:80` calls it. The stream Relay's Run alarm
   (`FrontRoomsHunterBrain`, `FrontRoomsHunter.cs:123`) is also only built by that test (line 82).
   So **no player has seen a Run! room yet.** The Shift, Office, Run and Exit frames come from the
   same builder (`BuildEditorPreview` → `BuildRoom`), rendered in Play Mode with the game's camera.
2. **The Run room is not dark. It is red.** It has 12 troffers: 7 are dead and 5 burn at 40%.
   Its lamps are weak (intensity 0.6 × 0.40 × 0.901 = **0.22** each, range 6.5 m), so its two red
   EXIT-sign point lights (3.6, range 9.5 m, soft shadows) light the whole room.
   - `title-run_wide`: mean value 0.283 (sRGB luma, 0–1). Lobby is 0.320, Shift 0.230.
   - Its darkest 5% of pixels sit at **0.176**: the **highest black level of all 22 wide and
     arrival frames** (next: the pile, 0.163). `title-run_arrival` (far door shut, as in play) gives 0.175.
   - The 5 lamps at 40% still show **bright lenses**: the lens glow follows the lamp level only
     (`ApplyFixtureVisual`: emission colour × 1.05 at 40%), not the profile's 0.6 intensity.
     So the Run ceiling shows lit panels that throw almost no light.
3. **There is no true exit or ending door.** The game's phases are Title, Playing, Paused
   and Caught (`FrontRooms3DGame.cs:31`). No room, door or trigger ends a run. The "Exit" profile
   is a Level 0 room with cold-cyan paper, a green-grey carpet and a 2.8 m floor strip. In play
   it is followed by a Lobby "breather" room (`RuleForProfileIndex`); the frames now show it.
4. **Blacks are lifted almost everywhere.** In 59 of 65 frames, less than 0.05% of pixels are
   darker than 0.08. The 6 exceptions all have Office material in view (black cabinets, column
   shadows, the Office side of a window or an arch): 0.8–6.9% of pixels.
   A dead lamp lowers its corridor from 0.334 to 0.257 (−23%), not to black.
5. **Lamp faults barely move the frame.** A failing lamp: 0.348 at its high, 0.325 in a dropout
   (−7%). A stutter lamp: 0.284 on, 0.214 on an off flash (−25%, the biggest change). Its lens
   visibly goes out, but 2–4 neighbour lamps keep the corridor lit.
6. **All map lamps have the same warm-white colour** (1, .96, .88), `FrontRoomsMapWorld.cs:1205`,
   in both themes. The Office reads green-grey because of its materials and its local grade
   volume (`FrontRoomsPost_Office`), not because of its lamps.
7. **Every troffer wears the same frosted lens.** `FrontRoomsSurfaces.OfficeLouver` returns
   `Troffer_Lens` (`FrontRoomsSurfaces.cs:43–47`); the stream's "parabolic louvers in the Office"
   comment (`FrontRoomsRoomStream.cs:1537`) is stale. At eye height the Lobby, Office and Exit
   ceilings read the same (`title-*_ceiling`).
8. The Level 0 map, the start rooms and the title Lobby share one paper, one carpet and one
   ceiling print. So the handoff from title to map is seamless (`start_doorway`).
9. **Office zones are always Standard height** (`FrontRoomsMap.cs:306`: the Office theme is only
   rolled for Standard zones). There is no Office Low or Office Tall to capture.

---

## 1. How the frames were made

- **Where:** private clone `/private/tmp/claude-501/…/scratchpad/proj_rooms`.
  Unity 6000.3.10f1, batch mode with graphics, Metal, Apple M3 Max, quality "High", URP asset
  `FrontRooms_URP` (4x MSAA, HDR, render scale 1). Fog exp² 0.014.
- **Harness:** [`harness/FrontRoomsRoomVisualsCapture.cs.txt`](harness/FrontRoomsRoomVisualsCapture.cs.txt)
  (a copy of the clone's `Assets/Editor/Audit/FrontRoomsRoomVisualsCapture.cs`). It follows the
  interaction-audit harness (`../interaction_audit/05_in_engine_evidence.md`): enter Play on
  `Assets/Scenes/FrontRooms3D.unity`, drive the game through its own fields and methods.
- **Camera:** the game's own `First-person camera`, with its URP camera data (post on,
  dithering on). Rendered twice per frame into a 1920x1080 sRGB target with 4x MSAA.
  The HUD is a Screen Space Overlay canvas, so it is not in the frames.
- **Lens:** the game uses 76° vertical FOV. Captures use **72° vertical** for wide, arrival and
  doorway frames, and **50° vertical** for detail and ceiling frames. FOV is restored after each.
- **Eye:** 1.62 m (`ModuleUnits.PlayerEye`). Three frames read 1.65 m (`start_wide`,
  `map-column-l0_detail`, `map-dead-lamp_detail`): the player stands on a 3 cm higher floor.
- **Time:** `Time.captureDeltaTime = 1/60` from the first frame. Game time moves exactly
  1/60 s per frame. The whole run takes 22.2 s of game time, about 1 minute of wall time.
- **Seed:** `Random.InitState(4242)` right before `RequestTitleStart` gives **run seed
  516574485**, generation tier 1. Map root (−576, 0, −576) (a multiple of 192 m). Start door
  cell (277, 198). The run starts in stream room 1, at game time 3.93 s. The run is
  deterministic: five re-runs gave the same cells; the last two gave identical brightness values.
- **Relay:** held dormant (`releaseDelaySeconds` 3 → 1e6). It is in no frame.
- **Map frames:** the harness moves the player (`playerRoot`) and sets the game's `yaw` and
  `pitch`, then waits until `map.Settled`. Map lamps light and cast shadows by distance from
  the player, so the player always stands where the camera is.
- **Title-profile frames:** `FrontRoomsRoomStream.BuildEditorPreview` is called in Play Mode
  on a new object at world **(4992, 0, 4992)** (26 × 192 m, so every print lands as in the
  edit-mode preview at the origin). Room *i* spans local z 12i…12i+12, centreline x 0. Doors are
  open. The harness adds **room 5, a Lobby breather** (the room that follows Exit in play), with
  the stream's own `BuildRoom` (by reflection) and its far door shut. Lamp states come from
  `ActivateRoomLightImmediately`: steady 100%, unstable held at 40%, dead off, rolled from the
  sequence numbers 0–5. Only the camera moves there; the player and the map stay put, so no
  map chunk is near the preview. Far plane 80 m for these.
- **Where each camera stood:** [`harness/room_capture_frames.txt`](harness/room_capture_frames.txt)
  (world and map-local or preview-local position, forward vector, yaw, pitch, FOV, far plane,
  and the notes below). The same rows as a table: [`harness/room_capture_recipes.tsv`](harness/room_capture_recipes.tsv).
  Run log: [`harness/room_capture_log.txt`](harness/room_capture_log.txt).
  Brightness table: [`harness/room_capture_luma.txt`](harness/room_capture_luma.txt).
  Pitch is positive when the camera looks up.

### How each map spot is chosen (the rule, so a re-shoot finds the same kind of spot)

| Frame family | Rule |
|---|---|
| Room wide (`map-l0-*`, `map-office`) | First cell of the wanted zone (theme + height) in rings round the start door cell + (0, 3), up to 70 cells. Walk there, settle. Then among built cells within 12: free floor, a straight open view of at least 2 cells, the next 2 cells in the same zone. Score = view length (max 6) + 0.4 × open sides of the next 2 cells + lamp score of the 3 cells (steady 1, stutter 0.9, dim 0.3, failing 0.2, dead −1.5); Office +6 if kit dressing is within 5 m. Camera 1.1 m back from the cell centre, looking 3 cells ahead at 1.45 m (Tall: 3.0 m). |
| Wall detail | Origins: the wide frame's feet, then free cells of the same zone within 2 cells. From each, 16 rays at 1.0 m; nearest hit on the shell mesh. Camera 1.25 m off the wall and 1.0 m back along it, looking 0.9 m along it at 0.45 m. Tier 1 "strict+clear": plain wall from −0.5 to +2.4 m along it, the wall belongs to the zone, no door leaf or pane within 2.2 m of the eye or the look point, no furniture bounds within 1.0 m of the look point, 1.1 m of the eye, 0.8 m of the mid-point. Tier 2 drops the furniture test; tier 3 is the old rule (plain wall over 1.6 m, wide feet only). **All four used tier 1.** |
| Tall ceiling | From the wide cell centre, looking 2.5 m past the next cell at 5.2 m. |
| Pile | Nearest built `furniture pile (…)`. If none, walk to Level 0 zones (Tall first, up to 10). Camera on the most open of 16 bearings, 1.6–2.5 m beyond the pile's bounds, looking at its centre at 0.8 m. |
| Column | Columns (`MapChunk.pillar`) in built chunks of the theme; Office ones with a bulkhead first, then the nearest. Camera 3.2 or 2.6 m out on the bearing (of 8) with a clear line to the column and the most open space behind it. Detail: 1.25 m off the face, turned 28°, looking at the cove (0.35 m) or, in the Office, at the bulkhead (2.1 m). |
| Zone border | First edge of the kind within 70 cells: Window (camera on the non-Tall side), Door (camera on the Standard side), or a theme seam (Open or Arch between Level 0 and Office at equal height; camera on the Level 0 side). Camera 1.5–2.6 m back from the crossing point. |
| Dead lamp | Built lamps of mode 3. Score = Level 0 +3, not Tall +4, darker neighbours (dead 1, dim 0.6, failing 0.4, lit −0.5) × 1.5, view length × 0.4. Camera one cell before the lamp, looking 2 cells past it at 1.55 m, so its lens is at the top of the frame. |
| Failing lamp | Same placement, mode 2, fewer lit neighbours preferred. The lamp's own clock is moved to a moment its formula reaches: level ≥ 0.66 ("high"), then < 0.04 ("dropout"). |
| Stutter lamp | Same placement, mode 1. "On": its next stutter pushed 100 s away, level ~0.94–1.0. "Off": one of its own stutters started now; the frame is taken on the first off flash (level 0.05). |
| Lamp strip | One lamp of each mode (Level 0 Standard first, then nearest), camera 1.7 m to the side, looking at the lens. Stutter: on an off flash. Failing: clock moved to a level of 0.42–0.48. |

---

## 2. Re-shoot after changes

Copy the harness into a **clone, never the real project**:

```
cp harness/FrontRoomsRoomVisualsCapture.cs.txt <clone>/Assets/Editor/Audit/FrontRoomsRoomVisualsCapture.cs
/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity -batchmode \
  -projectPath <clone> -executeMethod FrontRoomsRoomVisualsCapture.RunBatch -logFile <log>
```

It writes `<clone>/Verification/room_visuals/NN_<type>_<view>.png`, `frames.txt`,
`recipes.tsv` and `log.txt`, then quits. It needs graphics (no `-nographics`). One Unity per clone.
Then convert, build the sheets and measure (Pillow; on this Mac use `/usr/bin/python3`):

```
/usr/bin/python3 harness/export_room_captures.py <clone>/Verification/room_visuals images
/usr/bin/python3 harness/luma_room_captures.py images > harness/room_capture_luma.txt
```

If the map generator changes, the same seed can give other cells. The rules above then
find the same *kind* of spot. Compare by rule, not by cell number.

---

## 3. The set

Names are `images/room_<type>_<view>.jpg` (JPG q85, 1920x1080).
"ML" = map-local eye position (map root at world −576, 0, −576).
"PL" = preview-local eye position (root at world 4992, 0, 4992).

### A. Title stream, live (Lobby replicas, before Space)

What is behind it (`FrontRoomsRoomStream.cs`):
- Room 11.5 × 12 m, ceiling 2.9 m, walls 0.26 m. Rooms run along world x = 256.5. The
  camera crawls at 1.15 m/s. A door opens when the camera is within 4 m of it, over 0.9 s.
- 12 troffers per room: 3 columns × 4 rows, each one 0.6 × 1.2 m grid cell, a
  `Painted_Metal` pan and a `Troffer_Lens` lens. Spot 162°, inner 96°, range 10 m.
  The 4 centre-column lamps cast soft shadows (strength 0.92). A thin volumetric frustum
  hangs under each lens (density 0.012, 1 m long).
- Double door: 2.4 m opening, two leaves 1.12 × 2.62 × 0.08 m in `Door_Veneer`, bar handles
  and kick plates in brushed steel on both faces, 88° swing. Baseboard 0.08 × 0.16 m in `Cove_Base`.
- Lamps strike one by one after the door opens: 0.4–2.2 s delay, 0–5 flickers, 0.45–1.6 s rise.
  An unstable lamp then sways between 16% and 46% with dropouts to 6% (`TickUnstableLamp`).

| Frame | What | Camera |
|---|---|---|
| [`room_title-lobby_title.jpg`](images/room_title-lobby_title.jpg) | t = 1.0 s. Room 0, its far door shut 4.8 m ahead. | world (256.5, 1.62, 1.17), yaw 0, pitch 0 |
| [`room_title-lobby_doorway-opening.jpg`](images/room_title-lobby_doorway-opening.jpg) | t = 2.18 s. First door half open (progress 0.52). Room 1 still dark. | world (256.5, 1.62, 2.53) |
| [`room_title-lobby_doorway.jpg`](images/room_title-lobby_doorway.jpg) | t = 3.93 s. Door open 1.3 s; room 1's ballasts striking. | world (256.5, 1.62, 4.55) |

### B. Start rooms and the handoff (after Space)

The stream ends at its first shut door (room 1's far door, world z 18). The map is attached
behind it. The stream rooms become the map's start area. The door opens once the player is
within 4 m of it.

| Frame | What | Camera |
|---|---|---|
| [`room_start_wide.jpg`](images/room_start_wide.jpg) | Right after Space. The player stands where the title camera was. Start door 13.4 m ahead, shut. | ML (832.5, 1.65, 580.6), yaw 0 |
| [`room_start_doorway.jpg`](images/room_start_doorway.jpg) | 3.4 m before the start door, 1.5 s after it swung open. The map's first cell (277, 198) is Level 0 Standard: same paper, carpet and ceiling. | ML (832.5, 1.62, 590.6), yaw 0, pitch −2.5 |
| [`room_start_back.jpg`](images/room_start_back.jpg) | Same spot, looking back down the stream rooms. | yaw 180, pitch −1.1 |

### C. The map (seed 516574485)

What is behind it (`FrontRoomsMapWorld.cs`, `FrontRoomsMap.cs`, `FrontRoomsModuleUnits.cs`,
`Assets/Levels/FrontRoomsLevel0.asset`):
- Cells 3 m, chunks 24 m, build radius 2 chunks (far plane 46 m). Walls 0.16 m on cell lines.
- Heights: Low 2.4 m, Standard 2.9 m, Tall 5.4 m (zone shares 35 / 55 / 10%). Office share
  30%, and only of Standard zones.
- **Level 0:** `L0_Wallpaper` (`Wallpaper_Chevron_A`, tile 0.75 × 1.125 m), `L0_Carpet`
  (`Carpet_LoopPile_A`, 1 m), `L0_Ceiling` (`Ceiling_Fissured_A`, 1.22 m). Lens
  "Map / Level 0 lens": URP Lit, emission (1, .96, .84) × 2.6. Lamp 5.0.
- **Office:** `Office_Wall` (`Office_Drywall_A`, 1.22 m), `Office_Carpet`
  (`Office_CarpetTile_A`, 1.22 m), `Office_Ceiling` (`Office_Ceiling2x2_A`, 1.22 m). Lens
  `Troffer_Lens` (emission 2.2, 2.1, 1.8). Lamp 5.5. A local grade volume per Office area.
  Rooms are dressed by `FrontRoomsOfficeKit`.
- **Lamps (both themes):** one troffer per cell (lens 0.6 × 1.2 m, 0.3 m off centre). Spot
  162° / 96°, colour (1, .96, .88), range 10 m (Tall: 12 m, intensity × 1.6 = 8.0).
  Lit only within 16 m of the player, faded over 3 m. About one lamp in three may cast soft
  shadows, and only within 9 m.
- **Lamp temperaments** (tier-1 odds 62 / 20 / 10 / 5 / 3%):
  steady 0.98 ± 0.02; stutter 0.97 with 0.15–0.85 s bursts (95% / 5%) every 5–20 s;
  failing 0.25–0.70 slow wave with dropouts to 5%; dead 0 with rare 0.06–0.18 s blinks
  every 8–28 s; dim 0.42 ± 0.03. A dark lens keeps 4% of its glow.
- **Openings:** arches 1.1–1.8 m wide, top 2.2 m. Doors (1.0 × 2.1 m, leaf 0.05 m,
  `Door_Veneer`, `Cove_Base` frame 0.07 m, no handle) only on Low↔Standard borders. Windows
  (1.4 m wide, sill 0.35, top 2.0, glass URP Lit transparent (.75, .85, .88) α 0.28) only on
  borders with a Tall zone. At a theme border of equal height the wall is split into two halves,
  each faced in its own room's material.
- **Columns:** on the 6 m grid. 0.6 m (Level 0) or 0.9 m (Office, Tall), faced in the wall
  material, on a 0.10 m `Cove_Base` cove. Office columns carry 0.35 m bulkheads.
- **Furniture piles:** Level 0 rooms of 4+ cells, chance 35% (Tall rooms 60%).
- **Air:** fog exp² 0.014, colour (.16, .15, .11). Trilight ambient. Reflection
  intensity 0.3 and no reflection probes (see the interaction audit, §A).

| Frame | What | Spot (map cells) | Camera |
|---|---|---|---|
| [`room_map-l0-standard_wide.jpg`](images/room_map-l0-standard_wide.jpg) | Level 0, 2.9 m | cell (280, 199), looking −X, 5-cell view; own lamp steady | ML (842.6, 1.62, 598.5), yaw 270, pitch −1.0 |
| [`room_map-l0-standard_detail.jpg`](images/room_map-l0-standard_detail.jpg) | Chevron paper, cove base, loop-pile carpet; a wall return at the end | same zone, 1 cell back | ML (838.3, 1.62, 594.5), yaw 327, pitch −27, 50° |
| [`room_map-l0-low_wide.jpg`](images/room_map-l0-low_wide.jpg) | Level 0, 2.4 m, with columns | cell (272, 202), looking +Z, 6-cell view | ML (817.5, 1.62, 606.4), yaw 0, pitch −1.0 |
| [`room_map-l0-low_detail.jpg`](images/room_map-l0-low_detail.jpg) | Paper and carpet under the low ceiling | same | ML (814.3, 1.62, 607.4), yaw 213, pitch −27, 50° |
| [`room_map-l0-tall_wide.jpg`](images/room_map-l0-tall_wide.jpg) | Level 0, 5.4 m hall | cell (282, 207), looking +Z, 8-cell view | ML (847.5, 1.62, 621.4), yaw 0, pitch +7.8 |
| [`room_map-l0-tall_detail.jpg`](images/room_map-l0-tall_detail.jpg) | Wall foot of the hall; the border window far left | same zone | ML (847.3, 1.62, 625.3), yaw 213, pitch −27, 50° |
| [`room_map-l0-tall_ceiling.jpg`](images/room_map-l0-tall_ceiling.jpg) | The 5.4 m ceiling and its troffer field | same cell | pitch +33, 50° |
| [`room_map-l0-pile_wide.jpg`](images/room_map-l0-pile_wide.jpg) | Furniture pile "CentreSculpture", 5.3 × 4.2 × 4.0 m | centre cell (276, 164), Level 0 Tall | ML (833.4, 1.62, 488.9), yaw 338, pitch −7.3 |
| [`room_map-office_wide.jpg`](images/room_map-office_wide.jpg) | Office, 2.9 m, kit dressing | cell (273, 208), looking +X, 6-cell view | ML (819.4, 1.62, 625.5), yaw 90, pitch −1.0 |
| [`room_map-office_detail.jpg`](images/room_map-office_detail.jpg) | Drywall (orange-peel bump) and carpet tiles, no furniture | same zone | ML (819.5, 1.62, 634.7), yaw 57, pitch −27, 50° |
| [`room_map-column-l0_wide.jpg`](images/room_map-column-l0_wide.jpg) | 0.6 m Level 0 column | corner (258, 220), Standard | ML (777.0, 1.62, 661.2), yaw 248, pitch −3.0 |
| [`room_map-column-l0_detail.jpg`](images/room_map-column-l0_detail.jpg) | Column foot and cove | same | yaw 276, pitch −39, 50° |
| [`room_map-column-office_wide.jpg`](images/room_map-column-office_wide.jpg) | 0.9 m Office column with an east bulkhead | corner (250, 210), Standard | ML (748.8, 1.62, 627.0), yaw 23, pitch +5.0 |
| [`room_map-column-office_detail.jpg`](images/room_map-column-office_detail.jpg) | Column head and bulkhead | same | yaw 51, pitch +16, 50° |
| [`room_map-border-window_wide.jpg`](images/room_map-border-window_wide.jpg) | Window from an Office room into a Level 0 Tall hall | edge (281, 206) → (281, 207) | ML (844.5, 1.62, 618.4), yaw 0, pitch −2.8 |
| [`room_map-border-window_detail.jpg`](images/room_map-border-window_detail.jpg) | Frame, sill and pane at an angle | same | yaw 308, pitch −30, 50° |
| [`room_map-border-door_wide.jpg`](images/room_map-border-door_wide.jpg) | Door from Level 0 Standard to Level 0 Low, shut | edge (277, 202) → (276, 202) | ML (833.6, 1.62, 607.5), yaw 270, pitch −3.3 |
| [`room_map-border-door_open.jpg`](images/room_map-border-door_open.jpg) | Same door 1 s after `map.Use` | same | same |
| [`room_map-border-door_detail.jpg`](images/room_map-border-door_detail.jpg) | Latch jamb, leaf and frame (no handle exists) | same | yaw 258, pitch −17, 50° |
| [`room_map-border-theme_wide.jpg`](images/room_map-border-theme_wide.jpg) | Arch from Level 0 into an Office room, same height | edge (278, 203) → (278, 204) | ML (835.4, 1.62, 609.4), yaw 0, pitch −3.3 |
| [`room_map-border-theme_detail.jpg`](images/room_map-border-theme_detail.jpg) | Where chevron paper meets drywall | same | yaw 308, pitch −13, 50° |
| [`room_map-dead-lamp_wide.jpg`](images/room_map-dead-lamp_wide.jpg) | One cell before a dead lamp (lens at top). In its 3 × 3 cells: steady 2, failing 2, dead 2. | lamp cell (260, 184), Level 0 Standard, looking −X | ML (785.0, 1.62, 553.5), yaw 270, pitch −0.4 |
| [`room_map-dead-lamp_detail.jpg`](images/room_map-dead-lamp_detail.jpg) | The dead lens from 1.5 m | same | pitch +40, 50° |
| [`room_map-failing-lamp_high.jpg`](images/room_map-failing-lamp_high.jpg) | Failing lamp at level 0.664 (light 3.32) | lamp cell (279, 186), Level 0 Standard, looking +Z | ML (838.5, 1.62, 555.9), yaw 0, pitch −0.4 |
| [`room_map-failing-lamp_dropout.jpg`](images/room_map-failing-lamp_dropout.jpg) | Same lamp 0.05 s of lamp clock later, level 0.034 (light 0.17) | same | same |
| [`room_map-stutter-lamp_on.jpg`](images/room_map-stutter-lamp_on.jpg) | Stutter lamp between stutters, level 0.94 (light 4.71) | lamp cell (256, 168), Level 0 Standard, looking −Z; 3 × 3 cells: steady 1, stutter 1, failing 1, dead 1 | ML (769.5, 1.62, 509.1), yaw 180, pitch −0.4 |
| [`room_map-stutter-lamp_off.jpg`](images/room_map-stutter-lamp_off.jpg) | Same lamp on an off flash of one of its own stutters, level 0.05 (light 0.25) | same | same |
| [`room_map-lamp-steady_detail.jpg`](images/room_map-lamp-steady_detail.jpg) | Steady lens, level 0.996 | cell (256, 170) | pitch +36.5, 50° |
| [`room_map-lamp-stutter_detail.jpg`](images/room_map-lamp-stutter_detail.jpg) | Stutter lens on an off flash, level 0.05 | cell (256, 168) | same |
| [`room_map-lamp-failing_detail.jpg`](images/room_map-lamp-failing_detail.jpg) | Failing lens, level 0.42 | cell (257, 169) | same |
| [`room_map-lamp-dead_detail.jpg`](images/room_map-lamp-dead_detail.jpg) | Dead lens, light off | cell (256, 169) | same |
| [`room_map-lamp-dim_detail.jpg`](images/room_map-lamp-dim_detail.jpg) | Dim lens, level 0.45 | cell (259, 173) | same |

### D. The five title-stream profiles (generator preview, Play Mode)

Same room shell, door and troffers as in A. Light output = intensity × level × 0.901
(`diffuseCoefficient` 0.82 gives a 0.901 scale). All lenses are `Troffer_Lens` (§0.7).

| Profile | Wall | Floor | Ceiling | Tube colour | Intensity / range | Dead / unstable odds | Lamps in this preview (lit / 40% / dead) | Props |
|---|---|---|---|---|---|---|---|---|
| Lobby | `L0_Wallpaper` | `L0_Carpet` | `L0_Ceiling` | (1, .96, .88) | 5.2 / 10 m | 2% / 10% | 11 / 1 / 0 | 3 outlet plates (0.07 × 0.115 m boxes at 0.32 m) |
| Shift | `L0_Wallpaper_Shift` (same print, tint .90/.88/.80, stain 0.45) | `L0_Carpet_Shift` | `L0_Ceiling_Shift` | (.86, .93, .78) | 4.2 / 10 m | 10% / 32% | 6 / 4 / 2 | 3 outlet plates |
| Office | `Office_Wall` | `Office_Carpet` | `Office_Ceiling` | (.93, .96, 1) | 6.0 / 10 m | 3% / 6% | 12 / 0 / 0 | Office kit dressing; a 2.8 m centre lane kept clear |
| Run | `Run_Wall` (`Run_HospitalWall_A`) | `Run_Floor` (`Run_VCT_A`) | `Run_Ceiling` (`Office_Ceiling2x2_A`, tint 1.04) | (.88, .92, .94) | 0.6 / 6.5 m | 70% / 45% | 0 / 5 / 7 | 2 handrails at 0.92 m, 3 teal waiting chairs, a gurney, an IV pole, 2 hanging EXIT signs at z 3.4 and 9.2 (2.36 m high) with red point lights (1, .10, .06), 3.6, 9.5 m, soft shadows 0.85. All primitive boxes and one cylinder. |
| Exit | `Exit_Wallpaper` (`Wallpaper_Chevron_Cold_A`) | `Exit_Carpet` (loop pile tinted .66/.72/.74) | `L0_Ceiling` | (.62, .92, .90) | 4.4 / 10 m | 0% / 5% | 12 / 0 / 0 | 3 outlet plates and a 2.8 × 0.12 m floor strip in the wall material |
| (breather, room 5) | as Lobby | | | | | | 10 / 2 / 0 | as Lobby; far door shut |

Camera recipe, the same for every profile (PL; room *i* starts at z = 12i):

| View | Eye | Look-at | FOV |
|---|---|---|---|
| `_wide` | (0, 1.62, 12i + 0.9) | (0, 0.85, 12i + 12), pitch −4.0; all doors open | 72° |
| `_arrival` | (0, 1.62, 12i + 2.0), the stream's Entry anchor | (0, 1.25, 12i + 12), pitch −2.1; **this room's far door shut**, as in play until the camera is within 4 m | 72° |
| `_doorway` | (0, 1.62, 12i − 3.2), in the previous room | (0, 1.25, 12i + 6), pitch −2.3, through the open double door | 72° |
| `_detail` | (−1.2, 1.62, 12i + 3.6) | (−5.6, 0.45, 12i + 2.3) — Office: eye (1.2, 1.62, 12i + 3.0) → (5.0, 0.6, 12i + 6.0), a dressing column blocks the left | 50° |
| `_ceiling` | (0, 1.62, 12i + 2.5) | (−0.6, 2.9, 12i + 4.6), pitch +30.4 | 50° |

| Frame | What |
|---|---|
| [`room_title-lobby_wide.jpg`](images/room_title-lobby_wide.jpg) / [`_detail`](images/room_title-lobby_detail.jpg) / [`_ceiling`](images/room_title-lobby_ceiling.jpg) / [`_arrival`](images/room_title-lobby_arrival.jpg) | Lobby as built by the generator. The detail shows one outlet plate at 0.32 m. Lobby's doorway view is the live title frame in A. |
| [`room_title-shift_wide.jpg`](images/room_title-shift_wide.jpg) / [`_detail`](images/room_title-shift_detail.jpg) / [`_doorway`](images/room_title-shift_doorway.jpg) / [`_ceiling`](images/room_title-shift_ceiling.jpg) / [`_arrival`](images/room_title-shift_arrival.jpg) | Shift: dirtier paper, greener tubes, 2 dead and 4 failing lamps. The darkest profile (wide 0.230, detail 0.161). |
| [`room_title-office_wide.jpg`](images/room_title-office_wide.jpg) / [`_detail`](images/room_title-office_detail.jpg) / [`_doorway`](images/room_title-office_doorway.jpg) / [`_ceiling`](images/room_title-office_ceiling.jpg) / [`_arrival`](images/room_title-office_arrival.jpg) | Office: drywall, blue-grey carpet tiles, cubicles, a vending machine, a copier. |
| [`room_title-run_wide.jpg`](images/room_title-run_wide.jpg) / [`_detail`](images/room_title-run_detail.jpg) / [`_doorway`](images/room_title-run_doorway.jpg) / [`_ceiling`](images/room_title-run_ceiling.jpg) / [`_arrival`](images/room_title-run_arrival.jpg) | Run: red-lit pale corridor, VCT floor, 2'×2' ceiling, handrails. The doorway frame is the arrival from the Office. `_arrival` is the first thing a player would see: far door shut, one EXIT sign overhead, the second one over the far door. |
| [`room_title-run_sign.jpg`](images/room_title-run_sign.jpg) | The hanging EXIT sign. Eye (0.7, 1.62, 42.0) looking at (0, 2.36, 39.4), 50°. |
| [`room_title-run_props.jpg`](images/room_title-run_props.jpg) | Gurney and IV pole. Eye (0.9, 1.62, 40.4) looking at (3.4, 0.55, 43.3), 50°. |
| [`room_title-exit_wide.jpg`](images/room_title-exit_wide.jpg) / [`_detail`](images/room_title-exit_detail.jpg) / [`_doorway`](images/room_title-exit_doorway.jpg) / [`_ceiling`](images/room_title-exit_ceiling.jpg) / [`_arrival`](images/room_title-exit_arrival.jpg) | Exit: cold-cyan paper, green-grey carpet. Its far door now opens onto the Lobby breather (room 5), as in play. |
| [`room_title-exit_threshold.jpg`](images/room_title-exit_threshold.jpg) | The floor "threshold marker". Eye (0, 1.62, 50.8) looking at (0, 0.05, 53.9), 50°. It is hard to see: it is wall paper on the floor. |

---

## 4. Brightness, measured on the JPGs

sRGB luma (0.2126 R + 0.7152 G + 0.0722 B), 0–1, on each frame scaled to 480 × 270
(`harness/luma_room_captures.py`). "p5" = value of the darkest 5% of pixels.

| Wide / arrival frame | Mean | p5 | p95 | Mean RGB |
|---|---|---|---|---|
| title-lobby_wide | 0.320 | 0.155 | 0.485 | .36 / .32 / .18 |
| title-lobby_arrival | 0.312 | 0.155 | 0.481 | .36 / .31 / .18 |
| title-shift_wide | 0.230 | 0.139 | 0.335 | .25 / .23 / .12 |
| title-shift_arrival | 0.227 | 0.140 | 0.316 | .25 / .23 / .11 |
| title-office_wide | 0.286 | 0.143 | 0.472 | .29 / .29 / .23 |
| title-office_arrival | 0.272 | 0.142 | 0.467 | .28 / .28 / .21 |
| **title-run_wide** | **0.283** | **0.176** | 0.409 | **.41 / .26 / .17** |
| **title-run_arrival** | **0.264** | **0.175** | 0.366 | **.38 / .24 / .15** |
| title-exit_wide | 0.242 | 0.153 | 0.375 | .22 / .26 / .16 |
| title-exit_arrival | 0.241 | 0.153 | 0.380 | .22 / .25 / .16 |
| start_wide | 0.225 | 0.128 | 0.394 | .26 / .22 / .11 |
| map-l0-standard_wide | 0.334 | 0.118 | 0.585 | .39 / .33 / .17 |
| map-l0-low_wide | 0.348 | 0.140 | 0.647 | .39 / .35 / .20 |
| map-l0-tall_wide | 0.349 | 0.146 | 0.570 | .37 / .36 / .21 |
| map-office_wide | 0.276 | 0.073 | 0.480 | .25 / .29 / .22 |
| map-dead-lamp_wide | 0.257 | 0.122 | 0.420 | .29 / .26 / .13 |
| map-failing-lamp_high | 0.348 | 0.140 | 0.543 | .40 / .35 / .18 |
| map-failing-lamp_dropout | 0.325 | 0.139 | 0.540 | .37 / .33 / .17 |
| map-stutter-lamp_on | 0.284 | 0.130 | 0.481 | .33 / .29 / .15 |
| map-stutter-lamp_off | 0.214 | 0.129 | 0.375 | .25 / .22 / .10 |

All 65 frames: [`harness/room_capture_luma.txt`](harness/room_capture_luma.txt).

---

## 5. Not captured, and limits

- **The true exit / ending door does not exist** in code (§0.3). Nothing to capture.
- **Shift, Office, Run and Exit stream rooms in play:** not reachable (§0.1). The preview
  holds unstable lamps at a steady 40%. In a live stream they would strike, flicker and sway
  between 16% and 46% (`TickUnstableLamp`). That motion is not in these stills.
- **Office Low / Office Tall do not exist** (§0.9).
- **The glass** is still the milky material from the interaction audit. A pane is in the two
  `map-border-window` frames and at the far left of `map-l0-tall_detail`.
- The **Relay** is in no frame. The **HUD** is not drawn.
- The `_arrival` frames close the far door by setting the two hinge rotations to 0 (the
  stream's `ApplyDoorPose` at progress 0). Nothing else in the room changes.
- Room 5 (the breather) is built by the harness with the stream's own `BuildRoom`; its lamps
  roll from sequence 5. In play the breather would be a later sequence number with other rolls.
- These frames are the desktop reference (Editor, Metal). They say nothing about WebGL.

---

## 6. What changed since the 11:30 set

Same seed, same cells, same values for every frame that kept its rule. Added or changed:

- **Added (12):** `map-stutter-lamp_on` / `_off`; `title-<profile>_ceiling` × 5;
  `title-<profile>_arrival` × 5.
- **Re-framed (4):** the wall-detail rule now avoids door leaves, panes and furniture
  (§1 table). `map-l0-standard_detail` and `map-office_detail` no longer show a door leaf
  (the Office one also no longer shows a vending machine). `map-l0-low_detail` and
  `map-l0-tall_detail` moved slightly.
- **Exit frames:** room 5 (Lobby breather) closes the clear-colour slot behind the Exit room's
  far door. `title-exit_wide` mean 0.238 → 0.242 (partly the new measuring method).
- **Corrected facts:** Office zones are Standard only (code), not "not captured"; all lenses are
  the frosted `Troffer_Lens`; brightness is now measured at 480 × 270 with the script in
  `harness/`, so some values differ from the 11:30 table in the third decimal.
