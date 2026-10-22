# 60 · The first room: research synthesis (游戏视觉, 2026-10-22)

Workflow `station-first-room` (`wf_89885526-506`), synthesis stage, account A, session `local_5656cffd…`. English, for the lines. The Chinese production spec for Red is `61_for_red_first_room.md`.

- **Inputs:** four sweeps (real rooms, game first rooms, level and tutorial order, wall directions), every `NOTE_1022_first_room_*.md` in this folder (DECIDED, map, narrative, placard, system, tutorial) and `NOTE_1022_red_first_room.md`, read at 15:5x.
- **Nothing was downloaded.** No Unity, no git, no messages to other sessions. Notes for other lines are in §8 and in 61 §10.
- **Tags:** [read] = page fetched and read; [search] = snippet only; [memory] = from memory, not confirmed; ESTIMATE = a planning number, not a measurement. No film or game time codes appear here except those already in the project's own media ledgers.

## 0. Short answer

1. **Red asked (15:14–15:20, 游戏视觉 chat)** for the reception / guard room to have its own walls, distinct from Level 0, and to be the first room the player enters, for the level and the tutorial.
2. **Both halves are now decided by Red:**
   - **Structure, 15:24** (叙事设计 chat, "按推荐，第一个房间选方案 1"): Floor 1's station is the first room. The locked EXIT is in view through a doorway 3–6 cells away. The first send waits until the player has left the room. Floor 2+ stations stay rooms you find by ear.
   - **Walls, 15:44** (游戏视觉 chat, "这个版本可以" on `images/first_room_s4_walls2_post_room.jpg`): **walls 2 · Post Room**, on every station on every floor (叙事 R1). The sweep had recommended walls 1 (oak wainscot). Red chose walls 2. It is the cheapest of the three and reuses existing maps.
3. **The lines agree on the handoff:** the first room is the **first map room behind the start door D** (关卡设计's pick, confirmed by 叙事 R2). The research found no reason against it (§5).
4. **61 is therefore a production spec, not a decision page.** It asks Red one new thing: **N, what counts as "leaving the first room"** for the first send. Recommendation: the first cell outside E1 or E2.
5. **New finding for the layout (§6.1):** with D in the south wall's middle cell, a plain rectangle cannot meet both G1 measured from D and G4. Two things force this. D's cell is fixed by the title code. The no-chunk-border rule keeps the room at local x 4–6. The fix is a short full-height **post wall** (a wing wall from the north wall) that carries the key plate. Real 1990 lobby desks stood in front of their own feature wall (§2.3). It goes to 叙事 (layout owner) and to the next 游戏视觉 blockout.

## 1. What Red said, and where it is recorded

| When (PDT) | Where | Red's own words | What it decided | Recorded in |
|---|---|---|---|---|
| 15:14–15:20 | 游戏视觉 chat | "同时我需要你调研，当前接待室/警卫室应该是有一个单独的房间墙体设计，而不是直接利用 level 0，要做区分，并且这个房间从关卡设计的角度来说，应该是玩家第一个进入的房间，这样能够让玩家首先对游戏有所认识，这也是从教程设计的角度来说" | The ask (this run) | `NOTE_1022_red_first_room.md` |
| 15:24 | 叙事设计 chat (transcript 23146057, user turn 22:24:44Z) | "按推荐，第一个房间选方案 1" | Option 1 of `NOTE_1022_first_room_tutorial.md` §4 plus 叙事's narrative recommendations in the same message | `NOTE_1022_first_room_DECIDED.md`; `20_layout.md` v1.3; `narrative/40` §9; systems.json A26, N7 |
| 15:33 | 信息设计 chat | "457 毫米可以，就按这个尺寸" | BUSINESS HOURS plate final at 457.2 × 152.4 mm, on D's station face, centre 1525 mm | DECIDED note, update 15:3x |
| 15:44 | 游戏视觉 chat (transcript 5656cffd, user turn 22:44:10Z, with a screenshot of `first_room_s4_walls2_post_room.jpg`) | "这个版本可以" | Walls 2 · Post Room for every station | DECIDED note, 15:4x; tutorial `20_requests.md` V1 walls line |
| 15:47 | 游戏视觉 chat (with the stairwell VL325 sheet) | "选择这套，同时这种楼梯间其实是安全区" | Stairwell look A, and the stairwell is a safe zone. Not this run's scope, but it fixes the run's other safe boundary (§3, principle 6) | stairwell line |

## 2. Findings: real rooms, 1985–90 (sweep 1)

Sweep 1 read UBC 1991, NBS and USG texts, loc.gov and NARA item pages, and media already on disk. The question was what makes a 1990 front-of-house room read differently from the back of the house.

### 2.1 Level 0, measured (what the station must not look like)

- **Walls:** `L0_Wallpaper` is the yellow chevron paper, floor to ceiling, in one flat pattern. Its colours are ground #D2C27C, mid #AC9A52, deep #766A34 and cream #E3D594. One roll is 0.75 × 1.125 m (`research/room_visuals/01_inventory.md:107`). The walls have no base (`level_transitions/02_real_buildings.md` §1.6).
- **Floor and ceiling:** carpet #897853; lay-in ceiling #CCC6B4 with troffers.
- **Office shell:** walls #B5AE9D, carpet #535A61.
- **Colours reserved elsewhere** (`30_look.md` §2):
  - red (alarm);
  - amber (trouble lamp);
  - the glow yellow-green;
  - cherry and walnut veneer (A24's store);
  - mauve with teal as a pair.

### 2.2 Wall systems of the period

| # | System | Reads as | Source | Use for FrontRooms |
|---|---|---|---|---|
| W-a | Wood-veneer flush panelling or wainscot, oak or ash, to 48 in (1.22 m) with a cap | warm, solid, front of house | *Interiors* Oct 1990 printed p.20 (PDF p.22), a "stained-oak" security kiosk (on disk); Fine Hardwoods Selectorama on ash (archive.org) [read] | Walls 1 (not picked) |
| W-b | Type II textured vinyl wallcovering, 52–54 in rolls | woven or ribbed texture, one plain colour | Koroseal ad, *Interiors* June 1990 pp.48–49 (on disk); CCC-W-408A types I/II/III = 7/13/22 oz/yd², NBS TN 984 (1978) [read] | Walls 1 and 3 (not picked) |
| W-c | Vinyl-faced gypsum panels and relocatable partitions on a 24 in module | factory finish, joints on the ceiling grid | USG System Folder 1990: Ultrawall, Textone [read]. Colour names are not in the OCR | Cheaper all-grid alternative (not built) |
| W-d | Fabric-wrapped acoustic panels | soft, executive | *Interiors* Oct 1990 PDF p.28 (text only); UBC 1991 §4205 Class I | Not for a guard post |
| W-e | Painted block, two-tone | back of house | HAER OH-71-B-8 (Nov 1989) and NBS BSS 120 Fig. 2.7.5 (both on disk, black and white) | Kept for the stairwell (C22 A), on purpose |
| W-f | Stone or tile base and wainscot | lobby class | `rb_01`, *Interiors* June 1990 p.125 (on disk) | Not picked. Quarry tile is in the reserved red family |
| W-g | Wired glass in walls and doors | the one see-through | UBC 1991 §3305(h)2, §4306(g) [read]; *Interiors* June 1990 p.24 (on disk) | Not in V1: no glass on station walls (M4 (ii), `50` §5) |
| W-h | Trim: 4 in rubber cove base, rails, corner guards | a finished room | `level_transitions` K7/K10, S13 (vinyl wall protection sold from 1969) | **In walls 2:** black base, crash rail, stainless guards |

**Paint, crash rail and vinyl tile (walls 2) are period-true for a plain post.**
- *Interiors* June 1990 p.110 shows a reception with a wall "painted very pale gray" (on disk; Red picked walls 2 from this page's frame).
- Vinyl wall protection was sold from 1969 (`level_transitions` S13). A crash rail at chair-back height is the institutional answer to chair scuffs [memory: 0.75–0.9 m scuff band].
- 叙事's reading after the pick: CRT-era guard posts were function rooms, "rarely things of beauty" (P31, post-era trade retrospective). Paint, a crash rail and VCT are the vocabulary of 1990 institutional waiting rooms [memory].

**The 48-inch code line** (UBC 1991, printed pp.691–694 = PDF 720–723, read on the page):
- Table 42-B: enclosed stairs need Class I finish, other exitways Class II, rooms Class III.
- §4204(a) exception 1 lets Class III wainscot run up to 48 in in exitways.
- §3305(a): a lobby built to corridor standard is not an intervening room.
- This explained walls 1's layering. Walls 2 is paint over gypsum, Class I [memory: painted gypsum is Class I], so it needs no such split.

### 2.3 Ceiling, floor, desk, threshold, and a real "first room"

- **Ceiling:** a 2×4 lay-in at 8 ft 6 in was the 1980s building standard (`level_transitions` S17). A gypsum soffit or tray over a desk is period-true (*Interiors* June 1990 p.131, `rb_03`). **Walls 2 keeps the Level 0 ceiling** (one building).
- **Floor:**
  - vinyl tile, ⅛ in rubber and inlaid sheet vinyl are all on disk (*Interiors* Oct 1990 pp.5 and 20; June 1990 p.2);
  - at a change of floor: a reducer 41–60 mm wide and 3–6 mm high (`level_transitions` S23); ADAAG 1991 §4.5 (P20); the carpet seam centred under the closed door (S27).
- **Desk:**
  - The One Cambridge Court kiosk had "security monitors set into the interior counter of the kiosk below sightlines" (*Interiors* Oct 1990 p.20, on disk).
  - The Union Trust lobby desk stood in front of the lobby's one feature wall (HABS MI-273-7, seen once, not downloaded).
  - A real console of the period: design patent D298530 (filed 2 Dec 1985, granted 15 Nov 1988).
  - **These are the precedent for the post wall in §6.1:** the desk has a wall of its own behind it.
- **Threshold:**
  - UBC 1991 §3305(h)1: a corridor door is a 20-minute smoke-and-draft door, gasketed, self-closing.
  - §4306(l): the "FIRE DOOR / DO NOT OBSTRUCT" sign.
  - HABS PA-5585-B-4 (Oct 1990, on disk): a flush door with a small lite, a stencil, a surface closer.
- **A real first room:**
  - USCG Building 877 plan, 22 May 1986 (on disk, `sw_uscg_1986_emergency_light_units.jpg`): "SEC. OFFICE" is a small separate room off Lobby 877, beside the entrance doors. The lobby's emergency lights are fed "from security office fuse".
  - UBC 1991 §1807(f) puts the central control station in the lobby (P01).
  - NYC LL5's lobby command station (P05).
  - Ward (1985): the console should see the main entrance (P30, [search]).
  - Together these support "the guard's room is the first room past the main entrance". The finish is our pick.

## 3. Findings: how games set the first room apart (sweep 2)

Fifteen games were read. Ids `S2-nn` are in `SOURCES.md` §F. Stay times are mostly [memory].

| Game | First room | How it is set apart | What it teaches | Threat |
|---|---|---|---|---|
| Kane Pixels, *Backrooms* | Async's control room inside the Backrooms, "a sort of vestibule" (S2-46) | a built, institutional room among the yellow rooms | contrast only (not a game) | the yellow rooms beyond |
| *The Exit 8* | the Exit 0 corridor, the rules notice beside you | not by material: by the notice and the number sign | four rules on a wall sign; failure resets to 0 (S2-13, S2-14) | anomalies |
| *P.T.* | a small concrete room, one door to the hall | bare concrete against a furnished hall | nothing explicit (S2-17) | later |
| *Iron Lung* | the submarine, the only room | cramped, no viewport | a note on the floor (S2-20, S2-21) | sensors, then an eye |
| *Portal* | the Relaxation Vault | "visually unique room with memorable objects"; a force field became glass after playtests (S2-01, S2-03) | one thing at a time; the clipboard | later chambers |
| *Half-Life* | tram, then the Sector C lobby, a guard at the reception desk | an intact working facility (S2-04 to S2-07) | the world and controls, no exposition | after chapter 2 |
| *Control* | the Oldest House lobby; the first door opens on a security office | the security screens "stood out like a beacon", were then rewritten as legible warnings (S2-08) | navigation; signs alone failed, a map was added | after the weapon pickup |
| *Alien: Isolation* | the *Torrens*, then a save-console room | a different ship as the tutorial (S2-22) | save, locker, map consoles | glimpsed about 1 h in; criticised as slow (S2-24) |
| *SOMA* | a 2015 flat, then a clinic | a time jump as the threshold (S2-26) | free inspection of objects | after PATHOS-II |
| *Amnesia: The Bunker* | a trench prologue; the Admin Office hub | "your safe room – your home base" (S2-30) | map, save lamp, generator | the Beast in the walls |
| *Outlast* | a locked front, a window, the reception desk | a forced threshold; lights out | night vision; documents at the desk | testers "weren't scared early enough", so content moved earlier (S2-34) |
| *FNAF* | the security office is the game | two doors, cameras, power | the predecessor's voicemail (S2-36) | night 1 [memory] |
| *Signalis* | **counter-example:** red "permeates every single room", safe rooms included | marked by function, not colour (S2-38) | save and storage | soon |
| *Lethal Company* | the ship | a lit interior with a door you shut (S2-40) | the terminal | Eyeless Dogs "can enter" |
| *Resident Evil 7* | the guest house prologue | a separate building; the door locks behind you [search] (S2-43) | mechanics; a VHS on a static-only TV (S2-44) | Mia, during the escape |

**Ten principles, and how the decided first room keeps them:**

1. **Make the first room a different, memorable place so it can stand for "normal"** (Portal, Half-Life, SOMA, RE7, Alien). Kept by walls 2: paint, rail, VCT; no Level 0 paper, carpet or base inside.
2. **Put the change at a threshold and close it behind the player** (RE7, Outlast, P.T., Portal; Kane's door cut, `level_transitions/01_ip_research.md` P1). Kept: D shuts and locks 4 m in. Every seam with Level 0 sits in E1, E2 or D, never mid-wall (§6.3).
3. **One in-world rule text where the player starts, read in safety** (Exit 8, FNAF, Portal, Iron Lung). Kept: placard sheet A on the north wall facing D. No threat while reading, because the Relay is not released until the player leaves (M7).
4. **A single moving screen draws every player, so what it shows must be true** (Control, Outlast, RE7). Kept: the station set is the only moving image (A16). Its death at the take is the lesson. Watch for the title TV sharing the view when D opens (§8, 教程设计).
5. **Keep the first room safe, but bring the threat soon after the player leaves** (Outlast, Alien). Kept: the first send on leaving, 150 s fallback. The first lamp dip lands on the walk to the EXIT (N, 61 §13).
6. **Make the safe boundary honest and visible** (Lethal Company, FNAF, the Bunker). The room is map, so the Relay can reach it later; it is not a sanctuary. The only sanctuary is the stairwell (Red 15:47). Never imply a safe room the code does not keep.
7. **Small room, everything in one view, short stay** (Portal, Iron Lung, Exit 8, Control). 3×2 cells. From D: the set, the key board, the placard; through E1, the lit EXIT. Stay about 20–60 s (ESTIMATE, tune in playtest).
8. **Mundane first, strange after** (Control S2-11, Half-Life, SOMA, Exit 8 S2-16). The most ordinary 1990 room in the game. The strangeness begins at the threshold. Walls never glow (Q4 = B).
9. **Teach one thing at a time, and playtest the first room first** (Portal S2-01, S2-03; Control S2-08; Exit 8 S2-16). The room teaches L1–L3 only: the EXIT, the key at the TV, look + E.
10. **Keep clear of the owners:**
    - Kane: no red-alarm control room, no blast doors, recorder or timestamp at the desk.
    - FNAF: the player never sits at the post.
    - No *Severance* white hall, and no A24 showroom.

**Gaps:**
- Fandom wikis returned 402; Prima and GameSkinny returned 403.
- No source describes an Async lobby.
- No first-hand Valve statement on the tram ride.

## 4. Wall directions (sweep 4) and Red's pick

Three Blender blockouts used the r7 plan W′ rev 4, one camera set and one exposure as a "today" Level 0 baseline. The numbers compare between rows. They are not in-game values: this is Blender, not the game's post.

| Measure | Today (Level 0 shell) | W1 Lobby Post | **W2 Post Room (picked)** | W3 Night Reception |
|---|---|---|---|---|
| Doorway against the Level 0 paper at 4.5 m, ΔE76 | 19 | 30 (oak frame not counted) | **31** | 49 |
| Room brightness from E1, mean Y | 0.208 | 0.172 | **0.215** | 0.080 |
| Frame that changes when the set dies, from E1 | 10 % | 19 % | **16 %** | 23 % |
| Placard against its wall, Weber contrast | 0.34 | 1.00 | **0.51** | 3.79 |
| Pattern behind the key, TV off, CV (lower is calmer) | 0.39 | 0.25 | **0.26** | 0.28 |
| Light on the wall at the take, TV on, Y | 0.228 | 0.161 | **0.211** | 0.069 |
| Cost (ESTIMATE, 游戏视觉) | — | about 4.5 d, two new 2048 sets (about 33 MiB desktop) | **about 2.5 d, 0–16 MiB** (§6.2: 0 new maps if the existing ones are reused) | about 3 d, about 16 MiB, plus lamps |

- **What the sweep recommended:** walls 1, for the strongest "reception" read and the 48-inch code line.
- **What Red picked:** walls 2.
- **Why walls 2 works:**
  - **It is the brightest of the four.** "Dark is never cover" matters most in the room that teaches the game.
  - **It is as far from the paper as walls 1** (ΔE 31 against 30).
  - **It is the cheapest:** paint over the existing drywall normal, plus the existing VCT maps.
  - **It is the plainest:** 叙事 accepted it as more period-true for a guard's post.
- **What walls 2 risks, and how the spec answers it (61 §4):**
  - **Close to the Office theme.** Office is warm greige #B5AE9D with carpet; the station is cool blue-grey #9AA3A6 with VCT, a navy rail and a dark border course. Never place a station in an Office zone without the rail and the border.
  - **Close to the unused hospital level ("Level !", `Run_Wall` / `Run_Floor`: white paint, VCT, red exit signs).** The station's VCT is tinted warm (#B2AEA3), has a dark border and no red. Run is not in the map today.
  - **The least "reception" of the three.** The desk, the SECURITY letters, the register and the plate carry the role.
  - **Blue-grey under the set's lavender light** may read violet: #9AA3A6 is L* 66.4, a* −2.6, b* −2.7 by itself. Check the lit wall in Unity under V3 r7 on every card. Flag any a* above +2 (a violet cast) [ESTIMATE threshold].

**Shared by all directions:** the Level 0 2×4 ceiling and troffers, the same counter and props, a black rubber base 0.10 m, and trims from the Frame kit family. The station owns the trim at its openings (`02_real_buildings` Rule 5; Red 10-21: the newer fit-out owns the joint). Painted block is left out on purpose: it is the stairwell's "you have left the floor" signal (C22 A).

## 5. Level and tutorial order (sweep 3): the record

### 5.1 Facts from the code (main, read only, line numbers as of 15:30)

- **Control returns in the title room**, a 12 × 11.5 m Lobby replica (`FrontRoomsRoomStream.cs:16–18`). The map is attached behind the first shut door ahead (`:490`; `FrontRooms3DGame.cs:627–778`).
- **D always faces north.** The title camera starts at z 0 looking +z (`FrontRooms3DGame.cs:453`), so a room behind D has the same orientation on every run. M4's rotation rule is not needed on Floor 1.
- **The start area is safe ground:**
  - the Relay ticks only after D shuts (`:1138–1147`);
  - W1 lamps never dim in the start area (`FrontRoomsWarnLamps.cs:11, :219`);
  - the Relay never routes into it.
- **D shuts once the player is 4 m past it** (`StartDoorShutDistance`, `:155, :918`).
- **Chunk fit:**
  - D's cell is always chunk-local x 5 (`TitleCenterX` 256.5 m = cell 85). D's row is local 2 or 6.
  - A room 3 wide (local x 4–6) and 2 rows deep fits every start. 3 rows fit only when D's row is local 2.
  - 50 §5 M4 (ii) forbids a station perimeter edge (other than E2) on a chunk border. That rules out local x 5–7 and keeps D in the **middle** cell.
- **The wall around D** takes the stream's Lobby material on both faces (`FrontRoomsRoomStream.cs:604–611`), so the room's south wall needs a station-side lining.
- **The HUD room line** reads `inStartRooms ? … : ZoneName(zone)` (`:2499`). The desk cells need the FRONT ROOMS line (Red approved `LEVEL 0 / FRONT ROOMS`), not the zone name.

### 5.2 The four orders (history; diagram `images/first_room_s3_level_orders.jpg`)

| Order | First 10 s | Problem | Cost (ESTIMATE) | Result |
|---|---|---|---|---|
| (a) the desk IS the start area | the title shot is the desk | no lamp dip possible there; key at second 2; reopens the 10-02 title design | 6.5–8.5 line-days, high risk | rejected |
| **(b) D opens straight into the desk (first map room)** | D opens on the playing set within 3 s; D slams at 4 m; the locked main entrance behind you | a fixed 20 s opening for veterans; the beacon walk is first practised on B1 | +1–2 d map, +0.5 d visual at level scale, about 0.5 d graphic, 0.25 d each 叙事 / 系统, 0.1 d 教程 | **= Red's option 1, decided 15:24** |
| (c) a one-cell lobby, then the desk | the first space is a corridor | needs 3 rows (half the starts) and forced walls | about 4.5–5 d | rejected |
| (d) every floor starts at its desk | — | removes the beacon's job and A20's desk phase; the hunter goes to the arrival point | reopens T1 / A13 / A20 | rejected |

**Note:** the updated sheet with the 15:24 header is in scratch: `/Users/redwang/FrontRoomsVisualWork/station_first_room/s3_level/first_room_s3_level_orders.jpg`. The copy in `images/` is the earlier render, and its (B) note still says "C32 timing unchanged". It should be replaced by the session (I did not overwrite it, §10).

### 5.3 系统设计's six points, kept

1. **Lock before key:** kept by sight. The lit, locked EXIT is seen through E1 before the take. Placard line 4 and the key HUD `OPENS THE STAIR DOOR` (A22) back it up.
2. **First encounter no earlier than today:** gated by the take **and** leaving the room (N), with the 150 s fallback and the Floor 1 grace. The first burst comes on the walk to the EXIT. C32's far side is beyond E2 (west), away from the E1→EXIT path (east).
3. **One meaning per cue:** on Floor 1 the station phase of A20 is done at once, and the set dies at the take, so nothing pulls back. Floor 2+ is unchanged.
4. **Title handoff:** first map room behind D. The title TV may share the view with the set for 1–3 s when D opens (§8, 教程设计).
5. **Placard:** north-wall mount facing D. `YOU ARE HERE` at the desk. The EXIT on a fixed side (east, on the right of the sheet).
6. **Safety:** the desk cells and the E1→EXIT path are never an arrival spot. ≥ 2 Steady lamps on the entrance side, and the E1→EXIT lamps held Steady on Floor 1. Photosafety P4 applies.

## 6. What the decisions mean for production (detail in 61)

### 6.1 The room's geometry: the post wall (new finding)

**Fixed by the code and the decisions:**
- **Room:** 3 × 2 cells (9 × 6 m), local x 4–6, two rows north of the facade.
- **D (the start door = the building's main entrance):** the south wall's middle cell. Never rotated.
- **E1:** the east wall, toward the EXIT, which is always east, 3–6 cells, in a straight sight line.
- **E2:** the west wall (the Relay's far side).
- **Placard:** the north wall, facing D.

**The conflict:**
- **G1 (from D):** the set must be within about 12° of D's axis. That puts it on the north wall within ±1.27 m of x 4.5.
- **G4:** the key board must be on a wall perpendicular to the set's wall, 30–45° off the screen axis and ≤ 2.0 m from the screen. In a 9 m room the side walls are 4.5 m away. **A plain rectangle cannot do both.**

**Proposed fix (FD-a, for 叙事 to accept and 游戏视觉 to prove in blockout).** Room coordinates: x 0–9 m west to east, y 0–6 m south to north; D at x 4.5 on the south wall.
- **Set:** on the north wall on D's axis (x ≈ 4.5), the screen plane about 0.4 m off the wall, bracket bottom 2.10 m, 20° down, facing south.
  - About 5.5 m from D, inside the about-6 m V3 throw.
  - **G1 passes from D (about 0°),** and the set's light runs along D's axis (tutorial V1).
- **Post wall:** a full-height wing wall from the north wall southward, at x ≈ 3.3, about 2.1 m long (to y ≈ 3.9), 0.12 m thick.
  - It is a module wall, painted like the room, with the rail running round it.
  - The key plate, annunciator and conduit go on its east face. The hook (1.42 m high) sits about 1.3 m south of the screen plane and 1.1–1.2 m west of the axis.
  - From the screen centre that is about 41° off the axis and about 1.75 m away, ahead of the screen plane. **G4 passes.**
- **Counter:** runs east–west, facing D, its front about 2.2 m from D. The SECURITY letters are on its front.
  - The staff zone behind it is open at both ends: east into the room, and west through a ≥ 1.0 m gap between the counter and the post wall's south end, toward E2. **G7 passes.**
- **Guard's chair:** behind the counter, turned to face the set, its back to D. This is D′'s reveal: "he watched the set, not the door".
- **Placard:** on the north wall east of the set (x ≈ 6–7), 15–20° right of D's axis, not under the set.
  - It is reachable from the staff zone or from the open floor east of the counter's end.
- **E1:** the east wall, Arch 1.6–1.8 m, the EXIT 3–6 cells east in a straight line.
- **E2:** the west wall, Arch 1.1–1.4 m.
  - At the take (about x 4.0, y 4.2) the two escape directions are more than 100° apart. **G2 passes.**

**For the blockout to resolve:**
- **The 4 m door shut against the counter.** The counter front at about 2.2 m stops the straight walk, so D shuts only once the player goes round the counter's end, and the slam lands there. Move the counter north, or accept it.
- **The bay behind the post wall** (about 3.3 × 2.1 m) must not read as a hiding nook (G8). Furnish it with the key cabinet and coat hook, keep it open to the south, and light it Dim.
- **Placard "within 1.5 m of the desk"** (平面视觉 rule 1) holds only if the guard's work top runs along the north wall or the counter sits deeper. Otherwise, ask 平面视觉 to accept the north wall near the desk.
- **Every number above is a sketch,** not a measurement.

### 6.2 Walls 2 as a surface set

These are the blockout values. The final values are checked in Unity.

| Part | Material (new unless marked) | Value |
|---|---|---|
| Wall field | `Station_Wall`: eggshell paint, maps `Office_Drywall_N` (exists, 1024), world-projected at about 1.2 m | #9AA3A6 (L* 66), smoothness about 0.38 |
| Floor field | `Station_Floor`: `Run_VCT_A/N/S` (exist, 2048), tinted ×(0.66, 0.65, 0.63) | mean #B2AEA3, smoothness about 0.62 |
| Floor border course | `Station_FloorBorder` on a 0.305 m strip (one 12 in tile) along the inner wall faces | #3A3A38 |
| Crash rail | `Station_CrashRail`, 6 in vinyl, 0.775–0.925 m, 0.028 m proud, 0.010 m bevel; on every inner run; ends returned at openings | #3E4650 |
| Base | black rubber cove base, 0.10 m (4 in): `Prop_Rubber` if it measures near #1A1918, else new `Station_Base` | #1A1918 |
| Frames at E1 / E2 | pressed-steel cased frame from the Frame kit, `Prop_SteelBrown` (exists), 0.05 m face, 0.012 m proud, both faces | #3B2E25 dark bronze |
| Corner guards | `Prop_Stainless`, 0.05 m wings, 0.10–1.32 m, at the arch arrises (room side) | #A8A9A4, metallic 1.0 |
| Ceiling | `L0_Ceiling` and `Troffer_Lens` (exist), plus a 0.30 m gypsum header band over E1 / E2 | as Level 0; band #D9D6CC |
| Threshold | the VCT runs through the throat; a rubber reducer 0.05 × 0.006 m at the outer face, where the Level 0 carpet starts | — |
| D's station face | solid flush leaves (never glass), with the BUSINESS HOURS plate centred at 1525 mm; the leaf must take 457 mm | material: 游戏视觉 kit, with 叙事's door-hardware notes |
| South wall lining | a station-side shell over the Lobby facade, ≥ WallHalf + 0.01 m inside (the C22 stairwell pattern), so the title code is unchanged | `Station_Wall` |

**Texture memory:** no new maps if the existing drywall and VCT maps are reused. The WebGL and phone caps follow the WebGL-only rule: the WebGL import tab only, never Mac or Win. **Draws:** about 4–6 extra per station chunk; about 1.5k triangles of trim (sweep 4).

### 6.3 Seams, light and sound

- **Seams:**
  - The station's own faces exist only inside its cells (关卡设计's per-cell skin override, W1). The outside faces stay Level 0.
  - The yellow paper stops at the frame's outer face.
  - Mid-wall seams are not allowed.
- **Light layer:** `m_SupportsLightLayers` must be on (`FrontRooms_URP.asset` line 76; the shader supports it at `FrontRoomsSurface.shader` 147, 352–375).
  - The station's shell and props, the set's light and the station lamps go on "Station".
  - The doorway pieces and the two Steady entrance lamps go on Default|Station.
  - Level 0 lamps stay on Default.
  - The tier scope is `40_for_red_draft.md` decision 2, still Red's.
- **Footsteps:** hard floor in the station cells. Cosmetic only; noise stays tied to gait (声音设计).

## 7. What changes in work already done

| Work | Change |
|---|---|
| `30_look.md` §2 rule "the station adds no new shell surface" | **Replaced:** every station uses walls 2. B and A differ in furniture and story only (叙事 R1) |
| `20_layout.md` v1.3 | Add the module **Station · Front Desk** (FD): G1 measured from D; the post wall; the plate on D's room face; the slam at 4 m lands on that plate. W′ / D′ unchanged inside, with walls 2 |
| r7 blockouts (W′ rev 4, D′ rev 4, H) | Stay the baseline for Floor 2+. **Re-run checks 2 and 3 with walls 2:** the wall now changes how bright the key looks (key-light share and drop); G4 still holds. Check 1c (exits brightest after the take) may improve, because the walls are brighter than the paper |
| C32 / N7 / A13 / A26 (systems.json) | Already recorded by 系统设计 (z8): the station is the first room, and the send waits until the player leaves. **N is open** (61 §13). The "1 s after the take" wording becomes "1 s after leaving" |
| Floor 1 placement (M4, C24) | Floor 1: forced stamp at D before `Begin` (like `SetExitArea`); the `MapRootFor` desk test; the EXIT band 3–6 cells through E1 on a fixed side. C24's 14–18-cell band is replaced on Floor 1 |
| Tutorial `20_requests.md` | V1 walls line: "a 6 in navy-charcoal rubber base" should read **"a 6 in navy-charcoal crash rail at 0.78–0.93 m and a 4 in black rubber base"** (wording fix for 总控). M2 mount: north wall facing D. M7: the cell gate (61 §8) |
| Placard sheet A, Floor 1 | Redraw: `YOU ARE HERE` at the desk, EXIT on the right (east), `NO REENTRY` at D on the south edge |
| `40_for_red_draft.md` | Its decision 1 sub-question ("the same walls on every floor?") stays the one confirm item, recommended yes. Its decisions 2 and 3 are unchanged |
| Figma station section `3532:7362` | Not touched. The new board is beside it (§9) |

## 8. Open questions per line (no messages sent; also in 61 §10)

- **叙事设计:**
  - Accept FD-a (the post wall) or propose another way to meet G1 and G4 from D.
  - Write FD into `20_layout.md` (v1.4).
  - Review 61's story and string parts (your 15:45 offer).
- **关卡设计:**
  - Confirm the chunk-fit facts with the 100-seed check (3 × 2 at local x 4–6 on every start).
  - Forced stamp at D; the `MapRootFor` desk test; the EXIT planner band (3–6 cells through E1, east) and its fallback ladder in M8.
  - Hold the E1→EXIT lamps Steady on Floor 1.
  - The M7 cell gate (61 §8).
  - The skin override with the Surfaces names in §6.2.
  - The HUD line FRONT ROOMS in the desk cells (`:2499` plus the Mobile hunk).
  - A skin id in M4 for every station.
  - Your own estimate for W1 (sweep 4 guessed 1–2 d).
- **教程设计 (总控):**
  - Fix the (a)-style wording in `10_proposal.md` §3 row 1 and line 156: control returns in the title room, D opens on the desk within ≤ 3 s, and D seen from inside is the main entrance.
  - Decide the title-TV point: put the title TV out of view once D opens, or let the desk set (heard through D during the hold) carry A18.
  - Add a B1 metric, "desk found within 120 s".
  - The V1 wording fix in §7.
- **系统设计:** record N after Red answers; the C32 wording "1 s after leaving the room"; no other rule changes.
- **平面视觉:**
  - Sheet A geometry in cells: D south middle, the mount on the north wall east of the set (x ≈ 6–7 m), the desk in front of the north wall, E1 east (the EXIT on the right), E2 west.
  - Say whether the mount may sit more than 1.5 m from the counter if the counter stays near D.
  - Check the SECURITY brushed-aluminium letters (Heros Bold, 4 in) against the counter front `Prop_LaminateGrey` #8E8E88. Aluminium #B8B8B4 is L* 74.7 against the laminate's 58.9, so the contrast is low. Darken the counter front or confirm in the signs-mounted frames.
- **信息设计:** lit blockout frames with both signs mounted, for your check. These are not made yet; they need the FD blockout first (§10).
- **声音设计:** hard-floor footsteps in station cells (cosmetic). The S1 loop is near-field on Floor 1. S2 `Screen/Beacon` is still needed for Floor 2+.
- **游戏视觉 (us):**
  - The FD blockout with the post wall and both signs.
  - Walls 2 as Surfaces.
  - The south-wall lining.
  - D's room face.
  - The rendering-layer fix.
  - Re-run checks 2 and 3 with walls 2.
  - Swap the re-framed images (§10).

## 9. Download candidates for Red (none downloaded)

Ranked. "Size" is from the item page or an estimate where marked.

| # | File (save as) | Source URL | Licence | Size | Why |
|---|---|---|---|---|---|
| 1 | `fr_uscg877_1988_lobby_walls_1st_floor.tif` (+ floors 2–6, lighting) | https://commons.wikimedia.org/wiki/File:Building_877_Rehabilitation_of_Lobby_Walls_and_Miscellaneous_Repairs_Architectural_1st_Floor_Plan,_Interior_Elevations_and_Details,_July_13,_1988_-_DPLA_-_89ca64a9238a0f3036098a01c0062a96.tiff (NARA 167077797; 167077799; 167077803) | public domain (NARA) | 911,316 + 592,136 + 615,336 bytes (1-bit TIFF) | The same lobby as the 1986 plan on disk, with its security office, two years later: real interior elevations of 1988 lobby walls. Content not seen (title only) |
| 2 | `fr_habs_mi273_7_union_trust_security_desk.jpg` (+ -6) | https://www.loc.gov/pictures/item/mi0024.photos.089977p/ and …089978p/ | no known restrictions (loc.gov) | about 120 KB each (`pv`) | A real lobby security desk in front of a feature wall: the precedent for the post wall. Photo date not in the record |
| 3 | `fr_nasa_grc_1986_c_02341_security.jpg` | https://www.nasa.gov/image-detail/grc-1986-c-02341 | NASA, generally not copyrighted | unknown (the page refused curl) | A 1986 lobby desk with its sign-in logbook: the closest-dated real post |
| 4 | `fr_uscg702_1980_lobby_desk_details.tif` | Commons, same NARA series (NARA 167076113) | public domain | 785,540 bytes | Interior elevations of a lobby desk, 1980 (title only) |
| 5 | `fr_haer_ca174_a8_hughes_lobby_desk.jpg` | https://www.loc.gov/pictures/item/ca2225.photos.324261p/ | no known restrictions | about 0.1–0.3 MB (ESTIMATE) | A lobby receptionist's desk (documented 1995; the interior is older) |
| 6 | `fr_ubc1991_ch42_p720-723.png` (and pp.664–666, 730–731) | https://archive.org/details/gov.law.icbo.building.1991 | CC0 | about 0.25 MB per page | Code pages for the wall-finish classes and doors. The whole PDF exists only in an old session scratchpad, which can be wiped |
| 7 | `fr_usg_1990_ultrawall_textone_p4.jpg` (2–3 pages) | https://archive.org/details/USGSystemFolder1990 | CC BY-NC-ND 3.0 | about 0.5 MB each (not the 220 MB PDF) | Period partition and panel finishes |
| 8 | `fr_patent_D298530.pdf` | https://patents.google.com/patent/USD298530S/en | US patent drawings [memory: public domain] | about 0.3 MB (ESTIMATE) | A real 1985–88 security console |
| 9 | `fr_habs_hi165_8_guard_station.jpg` | https://www.loc.gov/pictures/item/hi0754.photos.048331p/ | no known restrictions | not checked | A guard station with wired-glass sash (1994 record) |
| 10 | `fr_usgs_menlo_park_office_1989.jpg` | USGS Loma Prieta collection (public domain) | public domain | 6.0 MB original; take the 1920 px version | A real US office interior on 17 Oct 1989 |
| 11 | `g2_hl1_sectorc_lobby.jpg` | https://combineoverwiki.net/images/c/c6/C1a0_lobby.jpg | © Valve, hosted under fair use; research board only | 150 KB, 1280 × 960 | The guard at the reception desk as the first room |
| 12 | `g2_portal_vault.jpg` | https://combineoverwiki.net/wiki/Relaxation_Vault | © Valve; research only | about 150–600 KB (ESTIMATE) | The "visually unique" first room |
| 13 | `g2_control_lobby.jpg` | https://store.steampowered.com/app/870780/ | © Remedy; research only | about 150–600 KB (ESTIMATE) | The security office as the first door |
| 14 | other game stills (Alien *Torrens* and Sevastopol save room, the Bunker admin map, Iron Lung, RE7 guest house, FNAF office) | Steam store pages listed in sweep 2 (`SOURCES.md` §F) | © publishers; research only | about 150–600 KB each (ESTIMATE) | Lower priority: the board already leads with Kane and Exit 8 stills on disk |

**Recommended set:** 1–3. They cost under 2.5 MB together and are rights-clear. They fill the one real gap: a real 1985–93 photo or drawing of a lobby guard desk that shows its wall.

**Public-repo note:** the frames in `images/` include © 1990 *Interiors* page crops, and the GitHub repo is public. `level_transitions` set the precedent; Red or 总控 should decide whether such crops stay in the repo.

## 10. Files

**Docs:** this file; `61_for_red_first_room.md`; `SOURCES.md` §F (appended).

**Images in `images/`, sweep 4 frames:**
- `first_room_s4_walls1_lobby_post.jpg`
- `first_room_s4_walls2_post_room.jpg`
- `first_room_s4_walls3_night_reception.jpg`
- `first_room_s4_walls_compare.jpg`
- `first_room_s4_walls_sources.jpg`

They still carry WAIT-RED chips. **Re-framed copies** with APPROVED (walls 2 and the comparison) and SUPERSEDED (walls 1 and 3) are in `/Users/redwang/FrontRoomsVisualWork/station_first_room/synth/frames_decided/`, made from the same raw renders with the Shot tool. My attempt to move the old frames to `_partial` and copy the new ones in was refused by the permission system, so **the session should swap them** (SHOT_FORMAT §5 does not allow WAIT-RED on a decided slide).

**The level-orders sheet:** `images/first_room_s3_level_orders.jpg` is the earlier render. The 15:24 version is `/Users/redwang/FrontRoomsVisualWork/station_first_room/s3_level/first_room_s3_level_orders.jpg`. Swap it in the same step.

**Scratch:**
- `/Users/redwang/FrontRoomsVisualWork/station_first_room/` (sweep scripts, renders, `metrics_final.json`);
- the sweeps' full texts in `synth/sweep_*.md`;
- the re-frame script `synth/make_frames_decided.py`.

## 11. Limits

- The FD-a geometry is a sketch from code facts and the G rules. It has not been built or measured.
- The Blender numbers are not in-game numbers.
- The 1988 USCG sheets, the 1980 desk sheet and the NASA photo were seen by title or caption only.
- Most game stay times are [memory].
- The colour of walls 2 under the set's light is not checked in Unity.
