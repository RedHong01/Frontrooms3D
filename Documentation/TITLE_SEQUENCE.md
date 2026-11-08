# 3D title sequence

Line 游戏视觉 (game visual). Rewritten in place on 2026-11-04 by the title_stream workflow (stages Survey, Concepts, Build, Renders,
Package; relayed from account G to B to A). The 10-03 text described a three-room pool, a 256 m rebase and a glide-to-anchor
handoff, none of which exist any more. Sections 1 and 2 describe the title as it ships. Sections 3 to 7 describe the title rooms
package, which is **held for Red's pick**. Section 8 is the question to Red, in Chinese.

Red's ask, 2026-11-04 09:13:40, in this line's chat (verified in transcript 3e92790a, a plain user turn):
"然后我需要你在最开始无限流房间里，去构思新的房间 Office/run/pool/等已经被设计好了之后我希望你在这个无限流房间的里增加上这些不同的房间作为最开始无限流房间的设计"

- **Done:** three title flows, built behind one switch in the clone W/proj_doorlean and filmed from the game camera (MP4s and a
  Shot-format sheet). Packaged as `W/apply/title_stream_rooms`: dry run OK on main, held.
- **Not done:** nothing has landed in main's code. Frame time on a phone has not been measured (it has never been measured for
  today's title either). Two pieces are not built: the club and Descent's Run! trip.
- **Reused:** each family's landed kits, lamps and finishes, the walnut double door, the T6 TV and the logo.

## 1. The title as it ships (main, `FrontRoomsRoomStream.cs` 32482f23)

- **Corridor.** The title is a stream of room copies along one axis. Each room is 11.5 m between wall centres, 12 m long and
  2.9 m high, and room n spans z 12n-6 to 12n+6. The stream keeps a pool of five rooms: at most two behind the camera and at least
  two ahead. The oldest room moves to the front once it is two shut doors behind the camera. When |z| passes 192 m the world
  rebases by a multiple of 192 m, so every world-projected print must divide 192 m.
- **Camera.** The camera crawls at 1.15 m/s at an eye height of 1.62 m (FOV 76), so a room passes every 10.43 s. Door n opens at
  camera z = 12n + 2, about 1.7 + 10.43 n seconds in. At Space the crawl eases out over 0.6 s.
- **Doors.** Every room ends in the walnut double-acting pair (`FrontRoomsDoubleDoorKit`). Red, 10-29: "为什么现在门还是悬空的，
  两处还是有缝隙". It has a real frame, casing and saddle, three spring hinges a leaf and 3 mm gaps. A door opens when the camera is
  within 4 m, swings to 88 deg in 0.9 s and snaps shut 4 m behind the camera. Its wired-glass lites stay dark until the room behind
  is connected. Sound: `FrontRoomsSoundDirector` Mode.Stream finds each door by its hinge names (声音设计).
- **Lamps.** Each room has twelve L03 troffers at lens 1.8 (Red 10-31: "选择L03但是我希望灯罩亮度可以是1.8"). The centre column of
  four casts soft shadows. Every lamp runs the lamp logic: waiting, strike flickers, rise, then steady, unstable or dead. Up to 36
  lights are lit, 12 of them shadowed.
- **Rooms 0 and 1.** Room 0 holds the logo. Room 1 holds the T6 title TV, a Kit_CRTTV on a wall bracket on its end wall (Red 10-22,
  教程设计: "T6 标题电视带声音").
- **Logo.** The 3D title opens in the empty corridor. It has no room labels, instructions, hunter, keys, notes or gameplay panels.
  The brand logo is native SVG `VectorImage` artwork, centred, and it fades in after a short hold (`LogoDelay` 0.7 s, fade 4 s).
  The complete wordmark includes the solid final S. The two offset S paths are separate vector assets that start on top of that S
  and move outward in turn, following `FrontRoomsRoomStream.FirstDoorProgress`, but never over a partly revealed mark. UI Toolkit
  sets opacity per element rather than on the lockup as a group, so the two S stay hidden until the reveal is complete. Then the
  relay clock catches up with the first door at the door's own 0.9 s pace: the first door opens at about 1.7 s, the reveal ends at
  4.7 s, and the relay plays from 4.7 to 5.6 s as the camera crosses the first threshold. The mark fades only at the Space handoff,
  over 0.55 s. The PNG and shader path remain as an older-platform fallback.
- **Handoff in place** (关卡设计's `FrontRooms3DGame.cs`; memory frontrooms-title-handoff):
  1. Space gives control in the room the camera is in. `FirstClosedDoorSequence` names the terminal: the camera's room if its far
     door is shut, else the next shut one.
  2. `EndStreamAt` freezes the stream, and the map attaches behind the terminal door on a Standard Level 0 row. On floor 1 the
     station, Station_FrontDesk_4x4, sits at the door cell.
  3. Once the player is out of the start area and 4 m from the door, `CloseTerminalDoor` shuts and locks it. Red's rule:
     "只有起始房间是锁死不能回头的".
  4. Every stream Light then fades over 1.2 s and is destroyed, and the rooms are disposed one a frame.
- **Boundary recovery.** The camera can cross a room boundary between two frames. The stream advances `currentSequence` through
  every connected room whose `endZ` it has crossed, so the pool stays eligible for recycling and nothing is allocated.

## 2. What today's title costs

Measured 2026-11-04 in the clone on an M3 Max: desktop Ultra at 1080p, medians of 12 frames a view.

- **Desktop:** in the R2-R8 views, at most 0.9k draws, 87 SetPass and 0.18M triangles. From room 1 at the reveal of room 2 it is
  1.03k draws and 0.25M triangles. 36 lights are lit, 12 of them shadowed.
- **Phone tier:** at most 0.8k draws and 0.17M triangles.
- **Start-up:** the title room is ready 36 ms after launch, behind the startup curtain.
- **Space to play:** about 120 ms.

## 3. Title rooms (package `title_stream_rooms`, HELD for Red)

### 3.1 The switch

The switch is `FrontRoomsRoomStream.Programme`, a static of type `TitleProgramme { Today, Tour, Drift, Descent }`.

- **Default Today:** every hook returns at once. Today builds no vignettes, keeps every legacy profile variant and adds no new rule
  for lights. Landing the package as it is changes nothing in the shipped title.
- **WebGL** is forced to Today (`UNITY_WEBGL && !UNITY_EDITOR`), because it is a separate track.
- Tools and tests set the switch before the title is built. Other statics:

| Static | Value | What it does |
|---|---|---|
| `PoolStopsReady` | on | The pool look is in main (pool_zone_look, pool_tile and pool_water landed 2026-11-04 12:52-13:03). |
| `DriftSeed` | 0 | A new draw at every launch. |
| `FamilyShadows` | on | 技术美术调研 row 4, per room family. |
| `FamilyShadowsInToday` | off | Applies row 4 to Today's Lobby rooms as well. |

### 3.2 The three flows

Every flow keeps rooms 0 and 1 as today (the logo with its relay-S, then the T6 TV). Room n's door opens at
(12 (n - 1) + 2) / 1.15 s, so R2 is revealed at 12.2 s and R8 at 74.8 s.

| Flow | Rooms after R1 | Loop | Feels like |
|---|---|---|---|
| **TF-1 TOUR (REC)** | OFFICE, L0, RUN!, L0, WAITING, L0, POOL, L0, repeat | 8 rooms, 83.5 s, the same every launch | a guided tour of the building, with a Level 0 breath between stops |
| TF-2 DRIFT | a seeded shuffle bag of OFFICE, RUN!, WAITING, POOL and LOW, with one or two Level 0 rooms between; every family once a bag, never within five rooms of itself. Seed 1, as filmed: R2 POOL, R5 WAITING, R7 LOW, R9 OFFICE, R12 RUN! | a new order every launch | a dream; never the same twice |
| TF-3 DESCENT | OFFICE, RUN!, LOW, WAITING, POOL, DARK POOL, then WAKE (a Level 0 room lit at once as its door opens), repeat; no Level 0 breaths | 7 rooms, 73.0 s | a film going down, then waking |

### 3.3 The vignettes

Each family has one vignette, built once at load into a bank. A vignette is moved into a stream slot while the slot is sealed (two
shut doors ahead) and keeps the slot's shell, its walnut pair and its door wall. Every vignette is drawn at the stream's own
module. It is a vignette of its family, not a new design of it. Code: `Scripts/Rendering/Title/FrontRoomsTitleVignettes.cs`.
Since 2026-11-05 the rooms draw what section 3.6 says (the renovation: the pool rooms, the bases, the rails).

| Family | What the room is | Lamps and shadows |
|---|---|---|
| OFFICE (O-C, Red 10-31 "办公室选择O-C") | The office kit's own cubicle farm (seed 285): 4 pods and 16 stations, 8 a side, round a 1.6 m aisle on the axis. W2 chairs, binders, a cooler, a copier and filing; dark CRT faces; no columns. Office carpet tile and walls. | O-C pendants. At most 4 shadowed, over the furniture. |
| RUN! (Run! B "emergency power", `research/level_space/71_run_room.md`) | A 4-cell corridor from the landed Run! B dress and rig, white tubes and the red tube. Armed and inert: no trip, nothing hunts, and `RoomRule` stays Lobby. Its strips strike cell by cell toward the far door. | The rig's 4 strips, shadowed at 172 deg (F7). |
| WAITING (the BS4 waiting area, station hero 11-03) | Two tandem rows facing across the lane, a magazine rack, a coat stand, a smoking stand, the guard's counter, a wall clock and a dark wall TV. | The slot's L03, shadowed only over the seats. |
| POOL (the pool level: `research/bathroom_rooms` D1, the pool look's PL_Chain) | Floor -0.15 m, ankle water at -0.03 m, the band at 1.2-1.5 m, porcelain tile (Red 10-31 "瓷白"). Dry tiled sills at both doors, so the walnut pair never stands in water. Tile, zone look (PoolLevel) and water (FrontRooms/Water, Ankle) come from the pool look, reached by name. | The slot's L03, one in three dead, no shadows. |
| LOW (Red 10-31 "低矮区按推荐") | A 2.4 m soffit stepping up to the door head, with steel-and-ply storage along the walls. | Eight 4 ft wraps, at most 4 shadowed. |
| DARK POOL (Descent only) | The pool room with every overhead dead. | Four wet niches glowing under the water: stand-in niches (P2-A is still Red's pick). |

**Not built:**

- the club: no locker or bench kits exist yet;
- Descent's Run! trip set piece: photosafety and sound come first;
- the stairwell: it is a 3 m safe zone;
- the Tall and pool halls: they are 5.4 m high and `RoomHeight` is a constant.

### 3.4 What a programme changes (never with Today)

- **The bank:** one vignette per family. The legacy profile variants are not built, which drops 4 hidden variants a slot (up to
  147 MiB on phones).
- **Lights off behind a shut title door:** at most the current room and the next one are lit, so at most 24 lights instead of 36.
  At Space they come back on.
- **Row 4 of 技术美术调研 (`FamilyShadows`):** Level 0 and pool rooms cast no shadows. Office, Low and the waiting room cast only
  from the lamps over their furniture: at most 4 a room, each a tight 120/90 deg, 7 m cone. Run! keeps its rig's strips.
- **Row 16, option D** (Red, 14:14 in the 技术美术调研 chat: "按推荐"): every vignette lamp gets URP's per-light data with
  `softShadowQuality` = Low set explicitly, through the map's `FrontRoomsMapWorld.ShippedShadowFilter` (关卡设计, 14:19), so the
  filter every lamp ships with lives in one place. This is what the shipped player draws.
- **The airlock handoff** (section 3.5).
- **Reflections:** the title camera takes each family's reflection zone as it enters the room.
- **Rebase:** the 192 m rebase waits until only a Level 0 room can be seen.
- **At the start-door shut:** `ReleaseTitleLampData` removes the family lamps' URP data before the game destroys the stream's
  Lights. URP's `RequireComponent` refuses `Destroy(light)` next to that data (tech_art contract G2).

What stays as it is in every programme:

- `RoomRule`: title rooms keep Lobby, so the Hunter's `RuleAt == Run`, the map's Run! finish and the legacy cycle see nothing new.
- The 12 m module and its door lines.
- The walnut pairs and their 0.9 s swing.
- The 1.15 m/s crawl and its ease-out.
- Rooms 0 and 1: the logo, the relay-S and the TV.
- The handoff calls: `StartRunInPlace`, `EndStreamAt`, `CloseTerminalDoor` and the body doors.
- Every sound hook (hinge and lamp names).

### 3.5 The handoff in a family room (the airlock)

1. Space in a family room gives control in that room.
2. The sealed room past it is a Level 0 room. Tour and Drift guarantee this by construction; Descent turns that sealed room to
   Level 0 unseen.
3. That Level 0 room's far door is the start door. The player walks through the family room's door and one Level 0 room (12 m) to
   the start door.

The station, its placard and the facade therefore meet a Level 0 wall, exactly as today. Two checks cover this:

- **Play:** 8 CxPlayRun runs with a 25 s map. All gave rc 0 and 0 errors, and every Space in a family room started the player in a
  Level 0 room.
- **Edit-mode smoke:** 16 Space moments x 7 loops. The start door was always in a Level 0 room, and the start rows never passed 16
  (the strip has 20).

The autopilot cannot push the body-door start door, so no play run reached the start-door shut (Today's runs cannot either).

### 3.6 The renovation of 2026-11-05 (`title_renovation`): what each room draws now

Red, 2026-11-05, in the 夸张结构调研与设计 chat after walking scene 4 (plain turns): 13:1x "我看了一下，这个瓷砖非常work，因此我需要你在
无限流房间里利用这个瓷砖来做新无限流房间的纹理材质和水光。水色需要最深"; 13:3x "我需要你翻新无限流房间然后直接合并到主项目里".
无限流房间 is the whole stream, as in Red's 11-04 "最开始无限流房间". 游戏视觉 agreed at 13:3x; their notes
(`W/level_space/title_pool/visual_reply.txt`) set the rules below. Built by the level_space line (workflow `title_pool`: audit,
design, build), landed on Red's word (no sheet waits for Red). Audit and design: `W/level_space/title_pool/audit.txt`, `design.txt`.

With a programme only. Today, WebGL and the legacy playable cycle are unchanged.

| Room | What it draws now | What changed |
|---|---|---|
| LEVEL 0 (R0 logo, R1 TV, every breath, the terminal / airlock room, Descent's WAKE) | L0 paper, carpet and lay-in ceiling, twelve L03, the Level 0 cube | The stream's raised rails are gone (a dark 16 cm band 10 cm over the carpet). The game's Level 0 walls have no base. |
| OFFICE | As 3.3 | The rails are gone. The game's Office base is laid at the floor: Cove_Base, 0.10 m tall, 6 mm proud, along both long walls and along the end walls from the door casings. |
| RUN! B | As 3.3 | Nothing: it was current. |
| WAITING | As 3.3 | Its look was current. Its lamps now strike with at most one flick each (photosafety, below). |
| LOW | As 3.3 | The rails are gone. The game's Low rooms are Level 0 theme, with no base. |
| POOL | Red's tile (DishDeep at dish tone 1.5, the game's default since 12:49) | The lamps held in the glaze, 游戏视觉's water light, caustic nets and the deepest water (below). Its lamps strike with at most one flick each. |
| DARK POOL (Descent) | As 3.3 | The deepest water. The four niches are wet niches while the water is on, and strike with at most one flick. No probe: its cube Refl_PoolDead holds the niches. |

The pool pieces:

- **Lamps held in the glaze** (`FrontRoomsTitlePoolGlaze`, scene 4's GZ-A). The lit pool vignette carries one realtime box probe:
  512 px, HDR, its box the room's inner faces (3 cm short of the end faces), time-sliced one face a frame. It is captured once
  per attach, the first frame the room is drawn behind its still shut door (row 1: the camera is one room back; in Tour about
  12 s before the reveal). For the probe's own six face renders only, the slot's live L03 are on at their steady output and a
  copy of each live lens glows at (tint.linear x 8).linear. Scene 4 uses 30 for its compact lenses; the L03 lens is about 0.63 m2 and the room hangs twelve. The
  copies are drawn with the L03 lens's own material (made flat), so their shader variant ships in every player; they sit on
  layer 27, out of every other camera, and are on only while a capture renders. The tile and the room's shell faces take the probe
  through `Renderer.probeAnchor` at the room's centre; both walnut door lines are anchored off the axis and keep today's cube.
  Unity gives a renderer every probe whose box its bounds touch, and the neighbouring slots' carpet and ceiling boxes reach 2 cm
  past the door lines. So the box stops 3 cm short of the end faces. With the box on the faces, the Level 0 rooms on both sides
  took the probe in the second loop, and their carpet's damp patches showed bright lamp images. The probe's brightness follows
  the room's lamps (smoothed over 0.25 s): the glaze is dark while the room is dark and comes up with the strike. Desktop High,
  Very High and Ultra only. Medium, iPhone and iPad have realtime probes off and keep Refl_PoolLevel.
- **Water light.** `FrontRoomsWaterSubmerged.SetWaterLight(Recommended)` while any title pool water is on (its room connected).
  It goes back to Today when none is on, when the start door shuts for good, and when the stream goes (a capture run that
  walked on past the start door saw Today the moment it shut). The game's own pool keeps Today until Red picks. The water's
  room now reaches the walls past the sills (`roomMargin`), as scene 4's bodies do, so the end walls take the water light too.
  In calm ankle water this light is a steady wash on the walls and the ceiling: the water and the basin's net move, and the
  mirror's sheen stays under the film grain (a still camera in the pool: temporal spread 1.6 / 255 on the walls, the grain's).
- **Caustic nets** from the two live L03 that light the water's centre most (scene 4's score), radius 0.25 m (the troffer's lens).
- **The deepest water:** `absorbScale` 12 on both pool waters, scene 4's key K 9 ankle value. The bounce follows it.
- **The strike** (`StrikeCapped`): in the two pool rooms and the waiting room each lamp strikes with at most one flick. Its own
  delay, period and rise are unchanged. Under the water light the white basin, the sills and the walls follow every flick of a
  strike.

**Photosafety** (rule 11; the math of `Tools/lookdev/flash_check.py`, general and red rules, on each whole film and again on
windows round its worst seconds; the limit is 3 flashes in any second). Main before the renovation: Tour desktop 4.0 (FAIL) at
the pool reveal (77.2 s) and 3.0 at the waiting room's strike (55.3 s). With it: Tour desktop 2.5 (the pool reveal 2.5 in
three windows, the waiting room 0.5-1.0); Tour phone 3.0 (40.0 s: the camera passing Run! B's ELECTRICAL ROOM door, a room
the renovation does not touch; the pool 2.5, the waiting room 0.5); Descent desktop 3.0 whole and 2.5 in its windows (88.0 s,
the Office pendants' strike in R9; both pools 0.5); Descent phone 2.0 whole, at most 2.5 in a window; a still camera in the
pool after Space 0.0. The red rule reads 0.0 in every film. The deepest water darkens the basin under the strike, and the
capped strike keeps the reveal at 2.5.

**Cost** (Editor batch on the M3 Max, desktop Ultra at 1080p, NOW and NEW bracketed under the same load; rule 25: no player
number). Draws fall by 4-12 a view where the rails go (views that see the Office through a door gain 3-4: six base strips
for two rails); SetPass +2 in the pool (the probe's cube breaks a batch), 0 elsewhere. GPU, synchronised at 7680 x 4320 under
load: the pool views +19-25 ms (+24-32 %, about +1.3 ms at 1080p by pixels: the water light, the caustic nets and the box
probe); every other room stays inside the spread between two NEW runs. The probe: 16.8 MB on desktop (a 512 px HDR cube),
none on phones. One capture per attach behind a shut door (Tour 63.7 s for a reveal at 75.7 s, Descent 42.9 s, the second
loop 147.2 s): the ask 0.2-4.7 ms of CPU, six faces in 8 frames, the worst of those frames 19-28 ms against a 16.7 ms median
in 13 captures without a film (one late frame at most). One capture of 20 stalled the Editor for 2 s (the design stage saw
0.8 s once on a fresh Editor: a first-use shader compile). The logo's contrast over the pool room falls from 3.7 to 3.1:1 (the
far wall behind the lockup takes the water light); the stream's lowest stays Run! at 3.0-3.1:1.

Code: `Scripts/Rendering/Title/FrontRoomsTitlePoolGlaze.cs` (new), `Scripts/Rendering/Title/FrontRoomsTitleVignettes.cs`,
`Scripts/FrontRoomsRoomStream.TitleRooms.cs`. `FrontRoomsRoomStream.cs` is not touched: rows 1 and 9 of 技术美术调研 (door
culling, no beams, dust only through `SetRoomDust`) stay as they are, and no pool room is dusty. Programme stays Tour. Every
vignette lamp still goes through `FrontRoomsMapWorld.ShippedShadowFilter`, and `ReleaseTitleLampData` still strips URP's light data
before the stream's Lights go. The start door still opens from a Level 0 room.

Evidence: `W/level_space/sheets/title_renovation.jpg` (NOW | NEW, with `_fit.txt`), the film
`W/level_space/renders/title_renovation/title_renovation_sbs.mp4` (NOW | NEW, one Tour loop), the renders and flash logs in
`W/level_space/title_pool/` (`renders/after_*`, `logs/flash_after_*.txt`), the build log `W/level_space/title_renovation_build_log.txt`.

## 4. Numbers

Clone W/proj_doorlean on an M3 Max, desktop Ultra at 1080p. Counts are medians of 12 frames a view; a single frame varies by
±10-20 % because of time-sliced work.

| | TODAY | TF-1 TOUR (REC) | TF-2 DRIFT (seed 1) | TF-3 DESCENT |
|---|---|---|---|---|
| Desktop draws, worst R2-R8 view | 0.9k | 3.0k (Office) | 2.5k (Office) | 3.1k (Office) |
| Desktop triangles, worst | 0.18M | 1.03M | 1.01M | 1.12M |
| SetPass, worst | 87 | 75 | 75 | 84 |
| Lights lit (shadowed) | 36 (12) | at most 24 (4) | at most 24 (4) | at most 24 (8 once, in Run!) |
| Phone tier: draws / triangles, worst | 0.8k / 0.17M | 2.6k / 0.63M | 2.3k / 0.62M | 2.8k / 0.64M |
| GPU proxy: median / worst room (ms) | 42 / 44 | 38 / 47 (Run!, +11 % on TODAY) | 37 / 48 (Office, +14 %) | 42 / 49 (Office, +18 %) |
| Vignettes built at load | 0 | 66 ms (Office 6.7, Run! 12.9, waiting 2.5, pool 38.7) | 74 ms | 69 ms |
| Title room ready | 36 ms | 94 ms | 103 ms | 99 ms |
| Space to play (UNDER LOAD) | 120 ms | 166 ms | 105 ms | 121 ms |
| Logo contrast, lowest room (mean over the lockup) | 6.7:1 | 3.1:1 (Run!) | 5.4:1 (pool) | 3.0:1 (Run!) |

Notes on the table:

- **GPU proxy** is a sync render at 7680x4320 taken at load average 20-120 (UNDER LOAD), so it ranks the rooms but is not a frame
  time.
- **Why the Office views peak:** in TF-1's TV room (2.76k draws) the Office and Run! dress behind shut doors is still drawn.
  技术美术调研's row 1 (door culling, approved and packaged as `ta_door_culling`) stops that. The merge with these title rooms is
  ready (section 7).
- **Logo contrast by room:** Level 0 6.7-7.5:1, Office 7.0-7.8, waiting 8.4-8.7, Low 11-12, dark pool 17, pool 3.7 (TF-1) to 5.5,
  Run! 3.0-3.1 (its brightest 10 % about 1.9:1).
- **Not measured:** phone frame time (it has never been measured for today's title), and texture memory per family.

## 5. What the renders show, and how main differs

**Films.** 45.03 s each at 30 fps, silent, from the game camera with the game's own logo. In 45 s the camera reaches rooms 0-4.
They carry the FRONTROOMSSS tone mark (`W/tools/recording_mark`).

**Sheet** (`research/title_stream/images/title_flow_renders.jpg`, WAIT-RED). Columns TODAY | TF-1 | TF-2 | TF-3. Rows:

- R2 reveal behind the logo;
- R2-R8 at +2 m;
- the handoff: Space, then the airlock and start door;
- phone R2;
- COST.

**The package differs from the rendered build** (TitleRooms 090a526e, Vignettes 8a9399d3, RoomStream 1cf9d6d1) in four ways:

- (a) The vignette lamps filter Low explicitly (`FrontRoomsMapWorld.ShippedShadowFilter`). In the renders they carried URP's
  default data, which is the pipeline's High, on at most 4 lamps a room. 技术美术调研 measured High against Low at the noise floor.
- (b) Two hooks were added: `ReleaseTitleLampData` and `TitleVignetteMoved`. They have no effect on any frame shown.
- (c) `PoolStopsReady` is on by default. The renders ran with it on.
- (d) Comments.

**Main's pool look moved after the clone's copy of 12:50:**

- `pool_zone_look_tune` set PoolLevel thrownLight from 0 to 1.
- `water_light` landed; its defaults are the same as before.
- 设置菜单 added the water mirror renderer at 13:23. At Ultra in main the title's pool water draws the planar mirror. The pool look
  measured it at about +1.8 ms at 1440p on desktop (UNDER LOAD). The renders show the probe path.

The combined test frames the pool room in main.

## 6. Files

Package `W/apply/title_stream_rooms`, 6 paths, **held** (`title_stream_rooms_HOLD.txt`; notes for the lander in
`title_stream_rooms_README.txt`):

| Path | Main | Package |
|---|---|---|
| `Assets/Scripts/FrontRoomsRoomStream.cs` | 32482f23 | b67034b1 (hooks: +93 / -14 lines) |
| `Assets/Scripts/FrontRoomsRoomStream.TitleRooms.cs` + `.meta` (GUID 22f7d4c5…) | absent | 7b0eaa69 (743 lines) |
| `Assets/Scripts/Rendering/Title.meta` (GUID 49d302ba…) | absent | feb628d6 |
| `Assets/Scripts/Rendering/Title/FrontRoomsTitleVignettes.cs` + `.meta` (GUID ededbfe3…) | absent | 0e0c98b8 (849 lines) |

**Gate.** All passed on 2026-11-04 19:15-19:24, after the evening's power loss (the battery ran out; the Mac restarted at 19:03).
Load average 46-89 then: any time taken from these runs is UNDER LOAD.

- `fr gate --staging` macOS + iOS: 0 errors, 3 overrides; both response files list the 3 package files.
- Dry run: `DRY RUN OK` on main.
- Rehearsal on a scratch copy of main: `MANIFEST OK`, then `ALREADY APPLIED`.

The script's API rows (10) check at apply time the map's `FrontRoomsMapWorld.ShippedShadowFilter`, which the vignette lamps call,
and the pool look's names: `FrontRoomsZoneLookVolume.Ensure`, `FrontRoomsWaterSurface.Kind` / `kind` / `roomHeight`,
`FrontRooms/Water`, `Prop_PoolTile_Field`, `FrontRoomsPost_PoolLevel` / `PoolDark` and the PoolLevel / PoolDead reflection zones.

**Once Red answers** (section 8): `zsh W/title_stream/tools/repack.sh Tour|Drift|Descent [--no-run] [--low]` sets the switch's
default (one line; `--no-run` and `--low` are answers 2 and 4), takes the RoomStream that fits main at that time, gates, rebuilds the
package and dry-runs it; then CLAIM, `--apply`, LANDED. Checked in its `--check` mode at 19:20-19:23 for Tour, Tour `--no-run --low`
and Descent `--no-run`: gate OK each.

**技术美术调研's rows in the same file.** `FrontRoomsRoomStream.cs` also carries tech_art's rows 16 (`ta_lamp_lightdata`), 1
(`ta_door_culling`) and 9 (beams, in progress), none landed at 19:3x. The package is built against main's RoomStream as it is now
(32482f23) and rebases over theirs when they land first: over rows 16 + 1 (81a23e5c) it takes `W/title_stream/stacked_on_ta`
(3 conflicts resolved by hand, `TitleVignetteMoved` implemented by the door culling; gated 19:17); row 16 alone merges clean; row 9
gave 5 conflicts in a trial merge and will need the same hand rebase once it is final. `repack.sh` handles the first two and stops on
conflicts.

**Harness.** Clone only, never packaged, in W/proj_doorlean:

- `Editor/TitleStream/FrontRoomsTitleStreamSmoke.cs` (edit-mode smoke, Office seed search);
- `Editor/TitleStream/FrontRoomsTitleCapture.cs` (stills, films, counts, logo mirror, Space and walk injection);
- `Scripts/CloneOnly/FrontRoomsTitleProgrammeArgs.cs` (the `-titleProgramme`, `-titlePool` and `-titleDriftSeed` arguments).

**Evidence** (`research/title_stream/`):

- `images/title_flow_renders.jpg`, with `_fit.txt` (ALL LABELS FIT) and `_numbers.tsv` (every figure);
- `video/title_flow_compare.mp4`: the four flows side by side;
- `video/title_flow_tour.mp4`: TF-1, desktop;
- `video/title_flow_tour_phone.mp4`: TF-1, phone tier 1920x884.

The full-resolution films of TODAY, TF-2 and TF-3 are in W/title_stream/films (`*_mark.mp4`), which is not an archive. The working
record is in W/title_stream: `survey.md`, `concepts.md`, `journal.txt`, `logs/`. The combined-test rows and the VL spec are in
`W/vl_pending/title_stream.md`.

## 7. Contracts and follow-ups (none blocks the landing)

| Owner | What | Where |
|---|---|---|
| 关卡设计 | C1: the start rooms take each stream room's reflection (diff on `FrontRooms3DGame.cs` 030c4a82, `patch` clean, gated with the package, 4 overrides; inert with Today; our package first). C2: no hunk needed now (the player's rings in a pool start room come through pool_water's `ReportBody`, already in main). C3: the HUD keeps LEVEL 0 / FRONT ROOMS. | `W/title_stream/contracts/map.md`, `CONTRACT_map.diff` |
| 声音设计 | S1: footsteps per family. S2: lights go off behind a shut title door. S3: a room tone per family through `TitleRoomConnected`. | `W/title_stream/contracts/sound.md` |
| 平面视觉 · 广告 | The white lockup over Run! (3.05:1 mean, 1.86:1 over its brightest tenth) and the pool (3.65-5.5:1); their call whether it needs anything. | `W/title_stream/contracts/graphic.md` |
| 游戏视觉 (us) | Row 1 door culling with the title rooms: `stacked_on_ta` holds `FrontRoomsRoomStream.cs` merged over `ta_lamp_lightdata` + `ta_door_culling` (3 conflicts resolved, `TitleVignetteMoved` implemented; `fr gate` with the package's other two files OK, 19:17). `W/title_stream/tools/repack.sh` picks it when main's RoomStream is 81a23e5c. Row 9 (beams): a hand rebase when it is final. | `W/apply/title_stream_rooms_README.txt` |
| 游戏视觉 (us) | Direct calls into the pool look instead of names (one hunk); the club once its kits exist. | - |
| App Store · 构建 | Re-record the corridor film after Red's pick (store hold). | `Documentation/AppStore/Video/README.md` |
| WebGL line | WebGL keeps Today. | - |

## 8. 给 Red 的选择：标题无限流里的新房间

**怎么看、怎么回。**

1. 先看对比视频 `research/title_stream/video/title_flow_compare.mp4`。四种标题并排放，同一时刻，45 秒，没有声音。
2. 再看总图 `research/title_stream/images/title_flow_renders.jpg`。它有每个房间的画面、按 Space 之后的画面、手机画质和成本。
3. 回"全部按推荐"就可以。也可以逐条回，比如"1 选 TF-2，其余按推荐"。

你选之前，游戏里什么都不会变。

**背景。** 你 11-04 09:13 说："然后我需要你在最开始无限流房间里，去构思新的房间……增加上这些不同的房间作为最开始无限流房间的设计"。

- **现在的标题：** 一条一直往前走的走廊，每个房间都是空的 Level 0 黄房间。
- **做成标题房间的家族：**
  - 办公室：O-C 吊灯，16 个工位；
  - Run!：B 应急电源走廊，红灯管；
  - 候车室：BS4 等候区；
  - 泳池：瓷白瓷砖，脚踝深的水；
  - 低矮储藏室；
  - 暗泳池。
- **怎么做的：** 每个房间都用已经落地的模型、灯和材质，只是放进标题走廊的房间尺寸（11.5 × 12 × 2.9 米）。
- **不变的东西：** 门，走速（每秒 1.15 米），Logo 和它的 relay-S，第二个房间的电视。
- **还没做的：** 俱乐部更衣室（还没有柜子的模型），TF-3 里的 Run! 警报那一段。
- **WebGL：** 网页版保持现在的标题。

三种流程都做好了，也拍好、打好包了，只等你选一个。

**问题 1：标题怎么走？**

| 选项 | 玩家看到和感觉到的 | 成本 |
|---|---|---|
| **TF-1 巡游（推荐）** | 前 12 秒和现在一样（Logo、电视）。第二扇门打开，是一间暗的办公室，吊灯一盏一盏亮起来。回到黄房间。然后是一条自己一格一格亮起来的白色走廊（Run!）。黄房间，空的候车室，黄房间。1 分 15 秒时，是有水的瓷砖泳池。两个家族房间之间总有一个黄房间"喘口气"。8 个房间一圈，83.5 秒，每次启动都一样。 | 电脑：办公室那一屋的绘制次数大约是现在的 3 倍（3.0k 对 0.9-1.0k），三角形大约 4 到 6 倍。GPU 估算最差多 11%（Run!）。亮着的灯反而比现在少（最多 24 盏，现在 36 盏）。启动多 66 毫秒，在启动遮罩后面，看不到。手机：办公室那一屋 2.6k 次绘制（现在 0.8k）。手机帧率没测过（现在的标题也从没在手机上测过）。 |
| TF-2 漂流 | 前 12 秒一样。之后每扇门后面是随机的家族房间，中间隔一到两个黄房间。每次启动顺序都不一样，像做梦。 | 和 TF-1 差不多，GPU 估算最差多 14%（办公室）。第一个家族房间可能就是泳池或 Run!，也就是最亮、Logo 最难看清的房间。 |
| TF-3 下沉 | 前 12 秒一样。之后不再回到黄房间：办公室、Run!、低矮储藏室、候车室、泳池、暗泳池。最后一扇门打开，是一个已经亮着的黄房间（"醒来"），然后重新开始。7 个房间一圈，73 秒。最像电影。 | 三个里最重：两个有家具的房间会前后同时看到，GPU 估算最差多 18%（办公室），有一屋会有 8 盏投影的灯。 |
| 保持现在（TODAY） | 全是黄房间。 | 零。但没有做到你要的东西。 |

- **推荐 TF-1，原因有三：**
  - 它保住 FrontRooms 的样子：一半时间还是无尽的黄房间，每个家族房间在黄房间的衬托下都像一个惊喜。
  - 顺序固定，所以 App Store 视频在 100 秒内能放完全部四种房间。
  - 三个里面成本最稳。
- **TF-1 的缺点：**
  - 每次启动都是同样四个房间、同样顺序。看几次之后像导游带参观，不像梦。
  - 泳池在很深的地方，1 分 15 秒就在标题里给玩家看到了，有一点剧透。
  - 白色 Logo 在 Run! 白墙前的对比只有约 3:1，在泳池前约 3.7:1（黄房间约 7:1）。还能读，但弱了。要不要处理，交给平面视觉·广告决定。
  - 办公室那一屋在手机上的绘制次数是现在的 3 倍多，手机帧率要在合并测试里实测。技术美术的第 1 行（门没开的房间不绘制，你已经批准）落地后，这个峰值会降下来。
- **TF-2 的缺点：** 没人能保证玩家前 30 秒看到什么。有人第一眼是安静的办公室，有人第一眼就是泳池。
- **TF-3 的缺点：** 黄房间是品牌的房间，在 TF-3 里 12 秒之后就没了，一圈 7 个房间里只在"醒来"时出现一次。它对手机也最重。

**问题 2：标题里放 Run! B 走廊吗？**

- **背景：** 10-31 在系统设计里你回了"全部按推荐"，其中 C40 是"标题的 Run 房间保留 Ward"。这次你要把设计好的房间放进标题，和 C40 不一样，所以要你确认一次。
- **它在标题里是什么样子：** 标题里的 Run! B 是"待命"状态。灯管亮着，不会触发警报，不会有东西追你。房间规则还是 Lobby，所以猎手和地图都不受影响。
- **推荐：** 放。
- **缺点：** 它的白墙最亮，Logo 对比最低（约 3:1）。
- **如果不放：** TF-1 变成办公室、候车室、泳池三站。这要改一行代码。

**问题 3：在家族房间里按 Space，起始门在哪里？**

- **背景：** 按 Space 后，你就在当前房间开始游戏。前面那扇门是"起始门"，走出去以后它会锁上（你说过"只有起始房间是锁死不能回头的"）。门外接的是 1 楼前台，那里的墙、门框和门牌都是按 Level 0 的墙做的。
- **推荐，已经做好："过渡间"。** 比如在办公室或泳池里按 Space，你站在那个房间里。穿过它的门，是一个黄房间，这个黄房间的远门才是起始门。所以起始门永远在 Level 0 房间里，地图那边一点不用改。
  - 缺点：在家族房间按 Space，要多走一个房间（12 米）才到起始门。
- **另一种：家族房间自己的门就是起始门。** 少走 12 米。但门外的墙、门框和门牌要和办公室、泳池的墙对上，需要关卡设计改立面。这个还没做。

**问题 4：TF-1 的第三站放候车室还是低矮储藏室？**

- **背景：** 候车室就是 BS4 的等候区，玩家后面在地图里会真的走到。如果你觉得在标题里先出现一次是重复，可以换成低矮储藏室：天花板压到 2.4 米，4 英尺的环形灯。
- **推荐：** 候车室。空着的一排排座椅，在标题里很有"后室"的感觉。

**你选完之后：**

1. 把默认值改成你选的那个（一行），通过编译检查，然后合进主工程。
2. 在合并测试里看实机，包括手机。
3. App Store 的标题视频等你选完再重录（商店暂停期间不上传）。
