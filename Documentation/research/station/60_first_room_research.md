# 60 · The first room: research synthesis (游戏视觉, 2026-10-22)

Workflow `station-first-room` (`wf_89885526-506`), synthesis stage, account A, session `local_5656cffd…`; **rev 2** by the fix stage (`wf_0ec92ae1-5a3`, account B, 17:xx) after two critics (sources, decision). English, for the lines. The Chinese page for Red is `61_for_red_first_room.md` (production spec plus the four open asks). The fix log is §12.

- **Inputs:** four sweeps (real rooms, game first rooms, level and tutorial order, wall directions), every `NOTE_1022_first_room_*.md` in this folder (DECIDED, map, narrative, placard, system, tutorial) and `NOTE_1022_red_first_room.md`, read at 15:5x.
- **Nothing was downloaded.** No Unity, no git, no messages to other sessions. Notes for other lines are in §8 and in 61 §11.
- **Tags:** [read] = page fetched and read; [search] = snippet only; [memory] = from memory, not confirmed; ESTIMATE = a planning number, not a measurement. No film or game time codes appear here except those already in the project's own media ledgers.

## 0. Short answer

1. **Red asked (15:14–15:20, 游戏视觉 chat)** for the reception / guard room to have its own walls, distinct from Level 0, and to be the first room the player enters, for the level and the tutorial.
2. **Both halves are now decided by Red:**
   - **Structure, 15:24** (叙事设计 chat, "按推荐，第一个房间选方案 1"): Floor 1's station is the first room. The locked EXIT is in view through a doorway 3–6 cells away. The first send waits until the player has left the room. Floor 2+ stations stay rooms you find by ear.
   - **Walls, 15:44** (游戏视觉 chat, "这个版本可以" on `images/first_room_s4_walls2_post_room.jpg`): **walls 2 · Post Room**, on every station on every floor (叙事 R1). The sweep had recommended walls 1 (oak wainscot). Red chose walls 2. It is the cheapest of the three and reuses existing maps.
3. **The lines agree on the handoff:** the first room is the **first map room behind the start door D** (关卡设计's pick, confirmed by 叙事 R2). The research found no reason against it (§5).
4. **61 is the production spec and the one page for Red.** It carries all four asks still open: 40's decision 1 (Floor 2+ directions, plus the "walls 2 on every station" confirm), decision 2 (the light layer on every tier), decision 3 (the build order, and one joint landing for the first room) and **N** (A26 units: N = 1 or 2), with the key-requirement sub-question. Recommendations: B on Level 0 floors and A on Office floors, yes to the same walls; C then A; build order 1 with one joint landing; **N = 1, keep the key requirement (A15)**.
5. **The room geometry is not settled (§6.1, rev 2).** 关卡设计's A26 start-group design draft (`~/FrontRoomsMapWork/notes/a26_facts/design_hybrid.md`, 16:48; final plan not out) puts the room at 4 × 4 cells (4 × 3 fallback), D in column 2 of 0–3, **E1 on the north wall opposite D, the EXIT straight north** with its flat sign facing the room, E2 on the west wall's row 1, the TV off the axis column. Our FD-a (3 × 2, E1 east, EXIT east) contradicts it and rested on premises the code does not support. 61 §4 now carries **FD-b**, a sketch inside 关卡设计's envelope: TV on the north wall 1.8 m east of the axis (G1 8.8° from D), a solid corner chase whose west face carries the key board (G4 38°, 1.52 m), the counter 7.2 m from D. A plain rectangle fails G1 + G4 in that envelope (computed, §6.1); it would have passed in the old 3 × 2 with D in the east cell, so FD-a's "a plain rectangle cannot" was wrong. The post wall is a design choice, not period evidence (§2.3). FD-b waits on 关卡设计's final plan, 叙事's re-acceptance and the blockout.

## 1. What Red said, and where it is recorded

| When (PDT) | Where | Red's own words | What it decided | Recorded in |
|---|---|---|---|---|
| 15:14–15:20 | 游戏视觉 chat | "同时我需要你调研，当前接待室/警卫室应该是有一个单独的房间墙体设计，而不是直接利用 level 0，要做区分，并且这个房间从关卡设计的角度来说，应该是玩家第一个进入的房间，这样能够让玩家首先对游戏有所认识，这也是从教程设计的角度来说" | The ask (this run) | `NOTE_1022_red_first_room.md` |
| 15:24 | 叙事设计 chat (transcript 23146057, user turn 22:24:44Z) | "按推荐，第一个房间选方案 1" | Option 1 of `NOTE_1022_first_room_tutorial.md` §4 plus 叙事's narrative recommendations in the same message | `NOTE_1022_first_room_DECIDED.md`; `20_layout.md` v1.3; `narrative/40` §9; systems.json A26, N7 |
| 15:33 | 信息设计 chat | "457 毫米可以，就按这个尺寸" | BUSINESS HOURS plate final at 457.2 × 152.4 mm, on D's station face, centre 1525 mm | DECIDED note, update 15:3x |
| 15:44 | 游戏视觉 chat (transcript 5656cffd, user turn 22:44:10Z, with a screenshot of `first_room_s4_walls2_post_room.jpg`) | "这个版本可以" | Walls 2 · Post Room for the station. "Every station on every floor" is 叙事's R1, still a confirm item for Red (61 §14) | DECIDED note, 15:4x; tutorial `20_requests.md` V1 walls line |
| 15:47 | 游戏视觉 chat (with the stairwell VL325 sheet) | "选择这套，同时这种楼梯间其实是安全区" | Stairwell look A, and the stairwell is a safe zone. Not this run's scope, but it fixes the run's other safe boundary (§3, principle 6) | stairwell line |
| 15:02 | 总控 chat (transcript 12f7eb38) | "开始把所有能够integrate到主build里的所有已经完成的资产都开始integrate，不用等alpha了" | The Alpha gate is gone; the first room's timing is set by the build order (61 §16) | `handoff/visual.md` §6 |

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
| W-a | Wood-veneer flush panelling or wainscot, oak or ash, to 48 in (1.22 m) with a cap | warm, solid, front of house | *Interiors* Oct 1990 PDF p.22 (printed p.20), a "stained-oak" security kiosk (on disk); Fine Hardwoods Selectorama on ash (archive.org) [read] | Walls 1 (not picked) |
| W-b | Type II textured vinyl wallcovering, 52–54 in rolls | woven or ribbed texture, one plain colour | Koroseal ad, *Interiors* June 1990 PDF pp.48–49 (ad pages, no folio; on disk); CCC-W-408A types I/II/III = 7/13/22 oz/yd², NBS TN 984 (1978) [read] | Walls 1 and 3 (not picked) |
| W-c | Vinyl-faced gypsum panels and relocatable partitions on a 24 in module | factory finish, joints on the ceiling grid | USG System Folder 1990: Ultrawall, Textone [read]. Colour names are not in the OCR | Cheaper all-grid alternative (not built) |
| W-d | Fabric-wrapped acoustic panels | soft, executive | *Interiors* Oct 1990 PDF p.28 (printed p.26; text only); UBC 1991 §4205 Class I | Not for a guard post |
| W-e | Painted block, two-tone | back of house | HAER OH-71-B-8 (Nov 1989) and NBS BSS 120 Fig. 2.7.5 (both on disk, black and white) | Kept for the stairwell (C22 A), on purpose |
| W-f | Stone or tile base and wainscot | lobby class | `rb_01`, *Interiors* June 1990 PDF p.125 (no folio on the page; on disk) | Not picked. Quarry tile is in the reserved red family |
| W-g | Wired glass in walls and doors | the one see-through | UBC 1991 §3305(h)2, §4306(g) [read]; *Interiors* June 1990 PDF p.24 (on disk) | Not in V1: no glass on station walls (M4 (ii), `50` §5) |
| W-h | Trim: 4 in rubber cove base, rails, corner guards | a finished room | `level_transitions` K7/K10, S13 (vinyl wall protection sold from 1969) | **In walls 2:** black base, crash rail, stainless guards |

**What the sources do and do not show for walls 2 (rev 2).**
- **Page numbers:** this file now cites *Interiors* as "PDF p.N (printed p.M)" wherever a folio is printed on the page; ad and full-bleed pages carry no folio and are cited by PDF page only. Checked on the page images: June 1990 PDF p.110 carries the folio "106 INTERIORS JUNE 90"; Oct 1990 PDF p.22 carries "20", PDF p.28 "26", so Oct PDF p.20 is printed p.18.
- *Interiors* June 1990 PDF p.110 (printed p.106) shows a design firm's reception: a **concrete block masonry wall** "painted very pale gray", with the firm's logo, on a granite floor (on disk). It shows that pale painted walls existed in a 1990 reception. It is painted block, not painted gypsum, and the floor is granite, not VCT. Painted block stays the stairwell's signal (§4), so walls 2 is our design, not a copy of this page. Red picked walls 2 from the blockout frame, whose source tile is this page.
- Vinyl wall protection was sold from 1969 (`level_transitions` S13): the one sourced fact for the rail. "A crash rail at chair-back height is the institutional answer to chair scuffs" is [memory: 0.75–0.9 m scuff band].
- "Paint, a crash rail and VCT are the vocabulary of 1990 institutional waiting rooms (clinics, DMVs)" is [memory]; no page read shows it.
- P31 (Chipman, SecurityInfoWatch, 2008) wrote "Consoles were rarely things of beauty." It is about **consoles**, not rooms, and it does not support the wall choice. (`NOTE_1022_first_room_DECIDED.md` 15:5x quotes it for the rooms; that is 叙事's note and is corrected here, not there.)

**The 48-inch code line** (UBC 1991, printed pp.691–694 = PDF 720–723, read on the page):
- Table 42-B: enclosed stairs need Class I finish, other exitways Class II, rooms Class III.
- §4204(a) exception 1 lets Class III wainscot run up to 48 in in exitways.
- §3305(a): a lobby built to corridor standard is not an intervening room.
- This explained walls 1's layering. Walls 2 is paint over gypsum, Class I [memory: painted gypsum is Class I], so it needs no such split.

### 2.3 Ceiling, floor, desk, threshold, and a real "first room"

- **Ceiling:** a 2×4 lay-in at 8 ft 6 in was the 1980s building standard (`level_transitions` S17). A gypsum soffit or tray over a desk is period-true (*Interiors* June 1990 PDF p.131, no folio, `rb_03`). **Walls 2 keeps the Level 0 ceiling** (one building).
- **Floor:**
  - vinyl tile, ⅛ in rubber and inlaid sheet vinyl are all on disk (*Interiors* Oct 1990 PDF p.5, stone-look vinyl tile, no folio; Oct 1990 PDF p.20 (printed p.18), ⅛ in rubber floor; June 1990 PDF p.2, inlaid sheet vinyl);
  - at a change of floor: a reducer 41–60 mm wide and 3–6 mm high (`level_transitions` S23); ADAAG 1991 §4.5 (P20); the carpet seam centred under the closed door (S27).
- **Desk:**
  - The One Cambridge Court kiosk had "security monitors set into the interior counter of the kiosk below sightlines" (*Interiors* Oct 1990 PDF p.22, printed p.20, on disk).
  - **Union Trust (corrected, rev 2):** HABS MI-273-6/-7 is **a 1979 photo of a 1929 lobby**. The NPS National Historic Landmark nomination's photo sheet (https://npgallery.nps.gov/GetAsset/0b241f78-be1c-4c05-a5a4-6fb9ababcf67, p.4) shows the same view captioned "Interior, Lobby, Security, Photo credit: Allen Stross, HABS, 1979" (the OCR garbles the name); the building is the 1928–29 Guardian (Union Trust) Building. The console stands in front of the lobby's monumental mosaic feature wall, which is the lobby's, not a wall belonging to the desk, and a "MICHIGAN NATIONAL BANK" sign is in frame. Only the item pages and the NPS sheet were read; nothing was downloaded.
  - A real console of the period: design patent D298530 (filed 2 Dec 1985, granted 15 Nov 1988).
  - **The post wall (61 §4) is a design choice** to meet G1 and G4 in one room, **not period evidence**. 20_layout v1.4 §4.5 copied the old "1990 lobby desk with its own back wall" claim; 叙事 is told in 61 §11.
- **Threshold:**
  - UBC 1991 §3305(h)1: a corridor door is a 20-minute smoke-and-draft door, gasketed, self-closing.
  - §4306(l): the "FIRE DOOR / DO NOT OBSTRUCT" sign.
  - HABS PA-5585-B-4 (Oct 1990, on disk): a flush door with a small lite, a stencil, a surface closer.
- **A real first room:**
  - USCG Building 877 plan, 22 May 1986 (on disk, `sw_uscg_1986_emergency_light_units.jpg`): the lobby (LOBBY 877) is the first space inside; "SEC. OFFICE" is a small separate room off it, beside the entrance doors. Note 2 reads "PICKUP FROM SECURITY OFFICE FUSE".
  - UBC 1991 §1807(f) (P01, read on archive.org) **does not put the control station in the lobby**: "The location of the central control station shall be approved by the fire department." It is a room of at least 96 sq ft and the section covers only Group B-2 high-rises with floors over 75 ft. In our fiction the control station is a separate locked room by the fire entrance with nobody at it, which is why the stair doors never unlock in an alarm (叙事 R10, `narrative/40` §1); that location is our story, not the code text.
  - NYC LL5's lobby fire command station (P05) is the fire safety director's station, not a guard's room.
  - Ward (1985): the console should see the main entrance (P30, [search]).
  - Together these support **"the guard post sits at the entrance lobby"**. Making it the first room the player enters, and its finish, are our choices.

## 3. Findings: how games set the first room apart (sweep 2)

Fourteen games and one web series (Kane Pixels) were read. Ids `S2-nn` are in `SOURCES.md` §F. Stay times are mostly [memory]. Quotes that could not be found again are marked [unverified] (rev 2).

| Game | First room | How it is set apart | What it teaches | Threat |
|---|---|---|---|---|
| Kane Pixels, *Backrooms* (web series) | Async's control room inside the Backrooms, "a sort of vestibule" (S2-45, a fan timeline on The Ghost in My Machine; not in the Wikipedia article S2-46) | a built, institutional room among the yellow rooms | contrast only (not a game) | the yellow rooms beyond |
| *The Exit 8* | the Exit 0 corridor, the rules notice beside you | not by material: by the notice and the number sign | four rules on a wall sign; failure resets to 0 (S2-13, S2-14) | anomalies |
| *P.T.* | a small concrete room, one door to the hall | bare concrete against a furnished hall | nothing explicit (S2-17) | later |
| *Iron Lung* | the submarine, the only room | cramped, no viewport | a note on the floor (S2-20, S2-21) | sensors, then an eye |
| *Portal* | the Relaxation Vault | "visually unique room with memorable objects"; a force field became glass after playtests (S2-01, S2-03) | one thing at a time; the clipboard | later chambers |
| *Half-Life* | tram, then the Sector C lobby, a guard at the reception desk | an intact working facility (S2-04 to S2-07) | the world and controls, no exposition | after chapter 2 |
| *Control* | the Oldest House lobby; the first door opens on a security office | the security screens "stood out like a beacon", were then rewritten as legible warnings (S2-08) | navigation; signs alone failed, a map was added | after the weapon pickup |
| *Alien: Isolation* | the *Torrens*, then a save-console room | a different ship as the tutorial (S2-22) | save, locker, map consoles | glimpsed about 1 h in; criticised as slow (S2-24) |
| *SOMA* | a 2015 flat, then a clinic | a time jump as the threshold (S2-26) | free inspection of objects | after PATHOS-II |
| *Amnesia: The Bunker* | a trench prologue; the Admin Office hub | "your safe room – your home base" (S2-30, [unverified]: GamingBolt's Bunker pieces dated 03-16 and 03-22 do not contain it) | map, save lamp, generator | the Beast in the walls |
| *Outlast* | a locked front, a window, the reception desk | a forced threshold (a window climb, no door closing behind); lights out | night vision; documents at the desk | testers "weren't scared early enough"; Red Barrels took a big chunk of the first section out of Act 1, kept it for Act 3, and added more scares early (S2-34) |
| *FNAF* | the security office is the game | two doors, cameras, power | the predecessor's voicemail (S2-36) | night 1 [memory] |
| *Signalis* | **counter-example:** red "permeates every single room", safe rooms included (S2-38, [unverified]: URL not recorded, not found again) | marked by function, not colour | save and storage | soon |
| *Lethal Company* | the ship | a lit interior with a door you shut (S2-40) | the terminal | Eyeless Dogs "can enter" |
| *Resident Evil 7* | the guest house prologue | a separate building; the door locks behind you [search] (S2-43) | mechanics; a VHS on a static-only TV (S2-44) | Mia, during the escape |

**Ten principles, and how the decided first room keeps them:**

1. **Make the first room a different, memorable place so it can stand for "normal"** (Portal, Half-Life, SOMA, RE7, Alien). Kept by walls 2: paint, rail, VCT; no Level 0 paper, carpet or base inside.
2. **Put the change at a threshold** (Outlast's forced entry, P.T.'s one door, Portal's vault; Kane's door cut, `level_transitions/01_ip_research.md` P1). **A door that locks behind you** is sourced for RE7 only, and only as a search snippet [search] (S2-43). Kept: D shuts and locks 4 m in. Every seam with Level 0 sits in E1, E2 or D, never mid-wall (§6.3).
3. **One in-world rule text where the player starts, read in safety** (Exit 8, FNAF, Portal, Iron Lung). Kept: placard sheet A on the north wall facing D. No threat while reading, because the Relay is not released until the player leaves (M7).
4. **A single moving screen draws every player, so what it shows must be true** (Control, Outlast, RE7). Kept: the station set is the only moving image (A16). Its death at the take is the lesson. Watch for the title TV sharing the view when D opens (§8, 教程设计).
5. **Bring the threat early** (Outlast: testers "weren't scared early enough", so a chunk of Act 1 was moved to Act 3 and scares were added early; Alien: criticised as slow). The first room is not a safe room (叙事 condition 3). Kept: the first send on leaving (N), 150 s fallback; with N = 1 a walker's first lamp dip lands on the walk to the EXIT (61 §17).
6. **Make the safe boundary honest and visible** (Lethal Company, FNAF, the Bunker). The room is map, so the Relay can reach it later; it is not a sanctuary. The only sanctuary is the stairwell (Red 15:47). Never imply a safe room the code does not keep.
7. **Small room, everything in one view, short stay** (Portal, Iron Lung, Exit 8, Control). 关卡设计's draft is 4×4 cells (4×3 fallback), bigger than we wanted; from the visual side 4×3 reads better (the set 8.6 m from D instead of 11.6 m). From D: the set, the key board, the placard; through E1, straight ahead, the lit EXIT. Stay about 20–60 s (ESTIMATE, tune in playtest).
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

Three Blender blockouts used the r7 plan W′ rev 4, one camera set and one exposure as a "today" Level 0 baseline. **W′ rev 4 is the Floor 2+ waiting room** (tandem seats, the set on the east wall); the Front Desk has never been modelled. The numbers compare the wall directions between rows only. They are not in-game values: this is Blender, not the game's post. "Weber" is the placard's luminance contrast against its wall, (L_placard − L_wall) / L_wall: larger means it stands out more. It has no pass line; it only ranks the directions.

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
- **What walls 2 risks, and how the spec answers it (61 §5):**
  - **Close to the Office theme.** Office is warm greige #B5AE9D with carpet; the station is cool blue-grey #9AA3A6 with VCT, a navy rail and a dark border course. Never place a station in an Office zone without the rail and the border.
  - **Close to the unused hospital level ("Level !", `Run_Wall` / `Run_Floor`: white paint, VCT, red exit signs).** The station's VCT is tinted warm (#B2AEA3), has a dark border and no red. Run is not in the map today.
  - **The least "reception" of the three.** The desk, the SECURITY letters, the register and the plate carry the role.
  - **Blue-grey under the set's lavender light** may read violet: #9AA3A6 is L* 66.4, a* −2.6, b* −2.7 by itself. Check the lit wall in Unity under V3 r7 on every card. Flag any a* above +2 (a violet cast) [ESTIMATE threshold].

**Shared by all directions:** the Level 0 2×4 ceiling and troffers, the same counter and props, a black rubber base 0.10 m, and trims from the Frame kit family. The station owns the trim at its openings (`02_real_buildings` Rule 5). Red's 10-21 words were "全部按推荐" on `level_transitions/40_for_red.md`, a plan whose default is that the newer fit-out owns the joint (and which notes Red can reverse it); Red did not state the rule separately. Painted block is left out on purpose: it is the stairwell's "you have left the floor" signal (C22 A).

## 5. Level and tutorial order (sweep 3): the record

### 5.1 Facts from the code (main, read only; rev 2 cites symbols, with `FrontRooms3DGame.cs` at sha1 fc76109d and `FrontRoomsRoomStream.cs` at 4e6e8e7f)

- **Control returns in the title room**, a 12 × 11.5 m Lobby replica (`FrontRoomsRoomStream` RoomLength / RoomWidth). The map is attached behind the first shut door ahead (`EndStreamAt`; `FrontRooms3DGame.StartRunInPlace`).
- **D always faces north.** The title camera starts at z 0 looking +z, so a room behind D has the same orientation on every run. 关卡设计's A26 draft fixes the station's rotation (entrance south, E1 north).
- **D is the stream's double door:** a 2.4 m opening with two 1.12 m leaves that stand open 1.12 m into the room (`FrontRoomsRoomStream` DoorWidth, DoorLeafWidth, DoorLeafPivotX). The leaves are title-stream objects.
- **The start area is safe ground:**
  - the Relay ticks only after D shuts (the relay gate in `FrontRooms3DGame.Update`, "Dormant until the door back to the stream rooms has shut"); v1 is then released after `releaseDelaySeconds` = 3 s, 9–15 cells away (`FrontRoomsMapHunter` SpawnMinCells / SpawnMaxCells);
  - W1 lamps never dim in the start area (`FrontRoomsWarnLamps` class comment and room walk);
  - the Relay never routes into it.
- **D shuts once the player is 4 m past it** (`StartDoorShutDistance` = 4, used in `UpdateStartRooms`).
- **Chunk fit (corrected, rev 2):**
  - D's cell is always chunk-local x 5 (`TitleCenterX` 256.5 m = cell 85; `MapRootFor` moves the root by whole 192 m periods). D's row is local 2 or 6 (rooms 12 m long from `firstStart` = −6 m), each about half of all starts; the autopilot's default Space gives local 6.
  - The rev 1 text said a 3 × 2 room at local x 4–6 "fits every start". **Wrong:** on local-6 starts the north wall of a 2-deep room lies on the chunk's north border, and that wall carried the TV, the placard and the post wall. The module stamp cannot author chunk-border edges (each border gets one seeded required gate plus random openings, `FrontRoomsMap` border pass), and M4 (ii) forbids a station edge there.
  - It also said the rule "keeps D in the middle cell". **Wrong:** local x 3–5 passes the same x rule with D in the east cell, and there a plain 3 × 2 rectangle meets G1 + G4 (set about 6° east of D's axis, key on the east wall: about 45°, 1.3 m).
  - **Superseded:** 关卡设计's A26 draft writes the station through a generator overlay that can author both sides of a chunk border, so the chunk constraint no longer applies (`design_hybrid.md` §0, §3.1).
- **The wall around D** takes the stream's Lobby material on both faces (`FrontRoomsRoomStream` facade build), so the room's south wall needs a station-side lining; in the A26 draft the map adds it (B3a).
- **The HUD room line** reads `inStartRooms ? FloorLabel(1) + " / FRONT ROOMS" : ZoneName(zone)` (the `roomText` line in `FrontRooms3DGame`). The desk cells need the FRONT ROOMS line, not the zone name; the A26 draft adds `InFrontRooms`.

### 5.2 The four orders (history; diagram `images/first_room_s3_level_orders.jpg`)

| Order | First 10 s | Problem | Cost (ESTIMATE) | Result |
|---|---|---|---|---|
| (a) the desk IS the start area | the title shot is the desk | no lamp dip possible there; key at second 2; reopens the 10-02 title design | 6.5–8.5 line-days, high risk | rejected |
| **(b) D opens straight into the desk (first map room)** | D opens on the playing set within 3 s; D slams at 4 m; the locked main entrance behind you | a fixed 20 s opening for veterans; the beacon walk is first practised on B1 | +1–2 d map, +0.5 d visual at level scale, about 0.5 d graphic, 0.25 d each 叙事 / 系统, 0.1 d 教程 | **= Red's option 1, decided 15:24** |
| (c) a one-cell lobby, then the desk | the first space is a corridor | needs 3 rows (half the starts) and forced walls | about 4.5–5 d | rejected |
| (d) every floor starts at its desk | — | removes the beacon's job and A20's desk phase; the hunter goes to the arrival point | reopens T1 / A13 / A20 | rejected |

**Costs in this table are sweep 3's guesses (rev 1).** 关卡设计's own estimate for (b) as it now designs it (A26 start group with a generator overlay) is about 40–55 agent hours, the P0 harness first (`design_hybrid.md` §8).

**Images (rev 2):** the sheet in `images/` is now the rev 2 render: (B) drawn in the A26 envelope, the pre-15:24 route relabelled as history (§10).

### 5.3 系统设计's six points, kept

1. **Lock before key:** kept by sight. The lit, locked EXIT is seen through E1 before the take. Placard line 4 and the key HUD `OPENS THE STAIR DOOR` (A22) back it up.
2. **First encounter no earlier than today:** gated by the take (A15; 关卡设计's B1 drops it, 61 §17 asks) **and** leaving the room (N), with the 150 s fallback and the Floor 1 grace. With N = 1 a walker's first burst comes on the walk to the EXIT. The far side is away from the player's route to N (A26 draft: west, beyond E2).
3. **One meaning per cue:** on Floor 1 the station phase of A20 is done at once, and the set dies at the take, so nothing pulls back. Floor 2+ is unchanged.
4. **Title handoff:** first map room behind D. The title TV may share the view with the set for 1–3 s when D opens (§8, 教程设计).
5. **Placard:** north-wall mount facing D (FD-b: west of E1). `YOU ARE HERE` = the reader at the placard, the desk as a symbol (平面视觉 17:0x). The EXIT on a fixed side: straight ahead, up on the sheet (A26 B12: sheet A, no sheet C).
6. **Safety:** the desk cells and the E1→EXIT path are never an arrival spot. ≥ 2 Steady lamps on the entrance side, and the E1→EXIT lamps held Steady on Floor 1. Photosafety P4 applies.

## 6. What the decisions mean for production (detail in 61)

### 6.1 The room's geometry (rev 2: FD-a withdrawn, FD-b proposed inside 关卡设计's A26 draft)

**Why FD-a is withdrawn.** Rev 1 drew FD-a (3 × 2 cells, D mid-south, the set on D's axis, E1 east, the EXIT east, a post wall at x 3.3) and called its frame "fixed by the code". It was not:
- the chunk premises were wrong (§5.1);
- the EXIT door is always on X's south edge (`FrontRoomsExitPlanner` ExitPlan: N = X + (0, −1)); its flat sign faces south, and a corridor running past N shows the end-on blade. FD-a's door at the east end of an east corridor, seen head-on from E1, cannot be built;
- D is a 2.4 m double door with 1.12 m leaves swinging into the room; FD-a drew a 1.0 m leaf, put the counter 2.2 m from D (1.08 m clear past the leaf tips against G6's 1.2 m) and the plate on the leaves' meeting stiles (title-stream objects).

**关卡设计's A26 draft (`design_hybrid.md`, 16:48; the final plan `floor1_start_group_plan.md` is not out).**
- 4 × 4 cells (12 × 12 m) recommended, 4 × 3 fallback; the P0 feasibility harness (5–7 h) picks.
- Rotation fixed: entrance south, E1 north. D in column 2 of 0–3 (e = 2), so the whole south wall is stream facade.
- E1: north wall, opposite D, arch 1.6–1.8 m on the axis. The EXIT straight north, d = 3–6 cells (9–18 m) from the north wall line to the EXIT door line, flat sign facing the room.
- E2: west wall, row 1, arch 1.1–1.4 m.
- Contract B8: E1 opposite D; the axis clear above 1.55 m; the TV off the axis column (a bracket set at ≥ 2.15 m on the axis would block the eye-to-sign ray, which rises from 1.62 to 2.29 m).
- B3a: the facade liner by the map, in station skin. B3c: the plate on the liner beside D, not on the leaves (信息设计 to confirm). B10: the TV light scaled by `map.StartLampsNorth`, so nothing leaks through the facade during the hold.
- Lamps: ≥ 2 Steady next to D; the TV's cell and its neighbour Off; the rest Dim; the line E1o..N Steady.

**A plain rectangle in that envelope (checked by calculation).** The TV must be ≥ 1.5 m off the axis (off the axis column) and within 12° from D (≤ 2.47 m off the axis at 4 × 4, ≤ 1.83 m at 4 × 3). G4 (≤ 2.0 m, 30–45°) allows at most about 1.41 m of side offset between screen and key. The nearest side wall (east) is 4.5 m from the axis, ≥ 2.0 m from any legal TV spot. So no plain 4 × 4 or 4 × 3 rectangle meets G1 + G4; e = 3 does not help (the TV would then have to go west, away from every near wall). A key-board wall inside the room is needed.

**FD-b (游戏视觉 sketch, not built or measured).** Room coordinates: x 0–12 m west to east, y 0–12 m south to north, axis x 7.5.

| Element | Where (4 × 4) | Check |
|---|---|---|
| D | south wall, x 6.3–8.7, two leaves open to y 1.12 | nothing within 2.4 m of D (G6 keep-clear past the leaves) |
| Set | north wall, centre x 9.3 (1.8 m east of the axis, outside the axis column), bracket bottom 2.10 m, screen 0.4 m off the wall, 20° down | G1 8.8° from D's threshold (7.5° from 2 m behind it); 11.6 m from D |
| Corner chase (the post wall, revised) | a solid full-height block in the NE corner, west face at x 10.3, 2.1 m south from the north wall; painted, rail returned round it | no bay, so no nook (replaces 叙事's condition 1, which asked for a Dim lamp in a cell the A26 draft keeps Off) |
| Key board, annunciator, conduit | the chase's west face, hook 1.42 m, 1.2 m south of the screen plane | G4: 38° off the screen axis, 1.52 m |
| Counter | x 8.9–11.0, y 7.2–7.8, front (SECURITY) faces D | 7.2 m from D, 19° east of the axis; staff side open west to the room and east through a 1.0 m aisle (G7) |
| Guard's chair | about (9.9, 8.8), turned to the set, back to D | "he watched the set, not the door" |
| Placard | north wall, centre x 5.5 (2.0 m west of the axis), 1.56 m, facing D | 9.5° from D; plan-up = north = the EXIT straight ahead |
| Plate | the south liner east of D, centre about x 9.2, 1525 mm | seen about 23° right of straight back when D slams at 4 m |
| Take spot | about (9.7, 10.4) | 2.7 m to E1, 11.4 m to E2; escape directions 67° apart (G2 ≥ 60°) |
| Lamps | Steady in the two cells beside D and in the placard's cell; the TV's cell and the axis cell Off; the rest Dim | 平面视觉 point 5 (a Steady lamp on the mount); G5 (the TV alone lights the key) |

- **4 × 3:** the set at x 9.1 (G1 10.6°, 8.6 m from D), the chase face at x 10.1 (G4 still 38°, 1.52 m), the counter near y 4.5–5.1. From the visual side 4 × 3 reads better (the set a third closer).
- **D's slam** now lands about 4 m in, before the counter: 叙事's condition 2 becomes "the door slams on the walk in; turning round shows D and the plate beside it".
- **The chase is a module prop**, not a cell edge (cells run at 3 m). 关卡设计 must accept it for Dress and Relay nav (FD-a's post wall was mid-cell too, and that was never asked).

**What waits on whom:** 关卡设计's final plan and its harness (4 × 4 or 4 × 3); 叙事's re-acceptance of FD-b (20_layout v1.4 §4.5 was written on FD-a); 平面视觉's re-render of `sheetA_room` (delivered on FD-a, VL411 PASS) if FD-b holds; the 游戏视觉 blockout with both signs.

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
| Corner guards | **new** `Prop_StainlessBrushed` (the name the code already asks for, `FrontRoomsWallFixtureRenderer.Stainless`; not in main's `Resources/Surfaces`, so today it falls back to `Prop_AluminiumAnodised`), 0.05 m wings, 0.10–1.32 m, at the arch arrises (room side) | #A8A9A4, metallic 1.0 |
| Ceiling | `L0_Ceiling` and `Troffer_Lens` (exist), plus a 0.30 m gypsum header band over E1 / E2 | as Level 0; band #D9D6CC |
| Threshold | the VCT runs through the throat; a rubber reducer 0.05 × 0.006 m at the outer face, where the Level 0 carpet starts | — |
| D | the stream's double door, unchanged (title code untouched). The BUSINESS HOURS plate goes on the liner beside D (east side), centre 1525 mm (A26 B3c; 信息设计 to confirm) | plate: 信息设计's delivered art |
| Counter front | **new** `Prop_LaminateGrey` | about #8E8E88 |
| South wall lining | a 0.02 m station-skin liner on the facade plane, built by the map with the door chunk (A26 B3a), so the title code is unchanged | `Station_Wall` |

**Texture memory:** no new maps if the existing drywall and VCT maps are reused; two new materials (`Prop_StainlessBrushed`, `Prop_LaminateGrey`). The WebGL and phone caps follow the WebGL-only rule: the WebGL import tab only, never Mac or Win. **Draws:** about 4–6 extra per station chunk; about 1.5k triangles of trim (sweep 4).

### 6.3 Seams, light and sound

- **Seams:**
  - The station's own faces exist only inside its cells (关卡设计's per-cell skin override, W1). The outside faces stay Level 0.
  - The yellow paper stops at the frame's outer face.
  - Mid-wall seams are not allowed.
- **The hold (first 10 s):** map lamps cast no shadows, so the set behind the shut D could wash the title room during the ≤ 3 s hold, the game's first image. The set's light is scaled by `map.StartLampsNorth` (A26 B10), whatever the light-layer decision.
- **Light layer:** `m_SupportsLightLayers` must be on (`FrontRooms_URP.asset` line 76; the shader supports it at `FrontRoomsSurface.shader` 147, 352–375).
  - The station's shell and props, the set's light and the station lamps go on "Station".
  - The doorway pieces and the two Steady entrance lamps go on Default|Station.
  - Level 0 lamps stay on Default.
  - The tier scope is Red's decision 2, now asked in 61 §15.
- **Footsteps:** hard floor in the station cells. Cosmetic only; noise stays tied to gait (声音设计).

## 7. What changes in work already done

| Work | Change |
|---|---|
| `30_look.md` §2 rule "the station adds no new shell surface" | **Replaced:** every station uses walls 2. B and A differ in furniture and story only (叙事 R1) |
| `20_layout.md` v1.4 §4.5 (叙事's) | Written on FD-a, including the Union Trust "desk with its own back wall" claim. To be redone on FD-b once 关卡设计's plan is out; the three conditions restated (61 §4.5). W′ / D′ unchanged inside, with walls 2 |
| r7 blockouts (W′ rev 4, D′ rev 4, H) | Stay the baseline for Floor 2+. **Re-run checks 2 and 3 with walls 2:** the wall now changes how bright the key looks (key-light share and drop); G4 still holds. Check 1c (exits brightest after the take) may improve, because the walls are brighter than the paper |
| C32 / N7 / A13 / A26 (systems.json) | Already recorded by 系统设计 (z8/z13): the station is the first room, the send waits until the player is ≥ N cells out (N ≤ 2), the start door's shut does not count. **N and the key requirement are open** (61 §17). C32's answer still says "door shut or N cells" against A26: a note for 系统设计 |
| Floor 1 placement (M4, C24) | Floor 1: 关卡设计's A26 start-group planner and generator overlay before `Begin`; the EXIT straight north 3–6 cells beyond E1 (walk from D's cell 7–10). C24's 14–18-cell band is replaced on Floor 1 |
| Tutorial `20_requests.md` / `10_proposal.md` | V1 walls line: "a 6 in navy-charcoal rubber base" should read **"a 6 in navy-charcoal crash rail at 0.78–0.93 m and a 4 in black rubber base"** (wording fix for 总控). M2 mount: north wall facing D. M7: N and the key requirement (61 §17). `10_proposal` §3 row 1 ("control in place in the first room") is the one wrong line; line 157 is already right |
| Placard sheet A, Floor 1 | Delivered on FD-a by 平面视觉 (17:0x: `sheetA_room/`, `sheetA_room_phase0/` rev 2, VL411 PASS; YOU ARE HERE = the reader at the placard, the desk as a symbol). On FD-b: the EXIT straight ahead, the mount west of E1; a re-render (minutes) |
| `40_for_red_draft.md` | Not sent separately any more: its decisions 1–3 are now 61 §14–§16, updated for walls 2 (15:44) and the lifted Alpha gate (15:02). A banner at its top says so |
| Figma station section `3532:7362` | Not touched. The new board is beside it (§9) |

## 8. Open questions per line (no messages sent; also in 61 §11)

- **关卡设计:**
  - Publish the final A26 plan and the P0 harness result (4 × 4 or 4 × 3); FD-b follows it.
  - Accept the corner chase as a module prop (Dress, Relay nav), or say which cell-edge alternative you prefer.
  - B1 goes to Red in 61 §17: our recommendation is N = 1 and keeping the key requirement (A15). With N = 2, M7's "N < EXIT distance − 1" removes d = 3, which on door-row-6 starts leaves only d = 4.
  - The skin override with the Surfaces names in §6.2; the HUD line; the hold scaling hook (`StartLampsNorth`) that 游戏视觉 reads.
- **叙事设计:**
  - Re-accept the layout on FD-b, with conditions 1 and 2 restated (61 §4.5).
  - Correct `20_layout.md` v1.4 §4.5: the Union Trust photo is 1979, the building 1929, and the wall is the lobby's feature wall, not the desk's.
  - Square "after-hours visitor" (61 §0; `narrative/40` around line 217; `01` §4 "It is night") with `01` §5, "the clocks are right, the hours are not".
  - Note: `NOTE_1022_first_room_DECIDED.md` 15:5x quotes P31 for rooms; Chipman's line is about consoles.
- **教程设计 (总控):**
  - Fix `10_proposal.md` §3 row 1 only (control returns in the title room; D opens on the desk within ≤ 3 s).
  - Decide the title-TV point (61 §13).
  - Add a B1 metric, "desk found within 120 s".
  - The V1 wording fix in §7.
- **系统设计:** record N and the key requirement after Red answers (61 §17); align C32's "door shut or N cells" with A26.
- **平面视觉:** re-render `sheetA_room` for FD-b if it holds (mount west of E1, EXIT straight ahead); check the SECURITY brushed-aluminium letters (Heros Bold, 4 in) against the counter front `Prop_LaminateGrey` #8E8E88: aluminium #B8B8B4 is L* 74.7 against 58.9, low contrast.
- **信息设计:** confirm the BUSINESS HOURS plate on the liner beside D (A26 B3c, your vision-panel clause); lit blockout frames with both signs mounted follow the FD-b blockout.
- **声音设计:** hard-floor footsteps in station cells (cosmetic). The S1 loop is near-field on Floor 1. S2 `Screen/Beacon` is still needed for Floor 2+.
- **游戏视觉 (us):**
  - The FD-b blockout with the chase and both signs, once 关卡设计's plan is out.
  - Walls 2 as Surfaces, plus the two new materials.
  - The set's light scaled by `StartLampsNorth`; the rendering-layer fix per Red's decision 2.
  - Re-run checks 2 and 3 with walls 2.
  - Place the VL rows in `~/FrontRoomsVisualWork/vl_pending/station_first_room.md`.

## 9. Download candidates for Red (none downloaded)

Ranked. "Size" is from the item page or an estimate where marked.

| # | File (save as) | Source URL | Licence | Size | Why |
|---|---|---|---|---|---|
| 1 | `fr_uscg877_1988_lobby_walls_1st_floor.tif` (+ floors 2–6, lighting) | https://commons.wikimedia.org/wiki/File:Building_877_Rehabilitation_of_Lobby_Walls_and_Miscellaneous_Repairs_Architectural_1st_Floor_Plan,_Interior_Elevations_and_Details,_July_13,_1988_-_DPLA_-_89ca64a9238a0f3036098a01c0062a96.tiff (NARA 167077797; 167077799; 167077803) | public domain (NARA) | 911,316 + 592,136 + 615,336 bytes (1-bit TIFF) | The same lobby as the 1986 plan on disk, with its security office, two years later: real interior elevations of 1988 lobby walls. Content not seen (title only) |
| 2 | `fr_habs_mi273_7_union_trust_security_desk.jpg` (+ -6) | https://www.loc.gov/pictures/item/mi0024.photos.089977p/ and …089978p/ | no known restrictions (loc.gov) | about 120 KB each (`pv`) | **A 1979 photo (Allen Stross, HABS) of the 1929 Guardian (Union Trust) lobby:** the console stands in front of the lobby's mosaic feature wall. A "MICHIGAN NATIONAL BANK" sign is in frame (a real brand: research board only, never in game). Not period evidence for the post wall |
| 3 | `fr_nasa_grc_1986_c_02341_lobby.jpg` | https://www.nasa.gov/image-detail/grc-1986-c-02341 | NASA, generally not copyrighted | 3108 × 2450 px (file size unknown) | Title "Various Shots of Security Locations"; caption "Michelle Schwab greets visitors in the Administration Building lobby in 1986" (NASA/Martin Brown). The caption names no guard and no logbook: the closest-dated real lobby reception desk |
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

**Recommended set:** 1–3. They cost under 2.5 MB together (#3 by its pixel size, an estimate) and are rights-clear. They fill the one real gap: a real 1985–93 photo or drawing of an entrance guard desk that shows its wall. #2 is older (1979) and stays useful only as "a console against the lobby's main wall".

**Public-repo note:** the frames in `images/` include © 1990 *Interiors* page crops, and the GitHub repo is public. `level_transitions` set the precedent; Red or 总控 should decide whether such crops stay in the repo.

## 10. Files

**Docs:** this file; `61_for_red_first_room.md`; `SOURCES.md` §F (appended).

**Images in `images/`, sweep 4 frames:**
- `first_room_s4_walls1_lobby_post.jpg`
- `first_room_s4_walls2_post_room.jpg`
- `first_room_s4_walls3_night_reception.jpg`
- `first_room_s4_walls_compare.jpg`
- `first_room_s4_walls_sources.jpg`

**Rev 2 frames** (re-framed from the same raw renders with the Shot tool, `~/FrontRoomsVisualWork/station_first_room/fix/frames_rev2/`, script `fix/py/make_frames_rev2.py`): APPROVED on walls 2 and the comparison, SUPERSEDED on walls 1 and 3, BASELINE on the sources board (INFO is not a §5 chip); page labels in "PDF p.N (printed p.M)" form; the sentence says the numbers come from plan W′ rev 4; meta `VL PENDING` with the spec in `~/FrontRoomsVisualWork/vl_pending/station_first_room.md` (the next free VL number has to be claimed in `VERIFICATION_LOG.md`, outside this run's write scope).

**The level-orders sheet** (rev 2, `fix/frames_rev2/first_room_s3_level_orders.jpg`): order B redrawn in 关卡设计's envelope (4 × 4, E1 north, the EXIT straight north), "today" relabelled as the pre-15:24 state, the timeline's N label fixed.

The swap state in `images/` is reported in §12.

**Figma** (file `0tCbAiVUlrPId3RWd9LRif`, page `2099:76`):
- **Section:** "FRONTROOMS · STATION · FIRST ROOM" `3585:7488`, at x 33937, y 38680, 8280 × 2640. It sits 400 px right of the station section `3532:7362`, in the same row. No overlap with any other top-level node; the VL log starts at x 42617.
- **Slides:** seven, 1920 × 1080, in the station section's grammar:
  - P1 styles only: `P1/Display/Title`, `P1/Display/Label`, `P1/Text/Body`, `P1/Text/Statement`, `P1/Header/Meta`;
  - digits and "·" in Bayon set in Plex Mono Medium;
  - chips black for content and yellow only for the one open decision, N.

| # | Frame | Content |
|---|---|---|
| 01 | `3585:7489` | **FR00 · The first room:** today's shell against walls 2; Red's ask, the two decisions, the one question |
| 02 | `3585:7514` | **FR01 · A guard's room, 1990** (real media first): USCG Bldg 877 lobby plan 1986, *Interiors* June 1990 PDF p.110 (printed p.106), the Oct 1990 kiosk (PDF p.22, printed p.20), HABS PA-5585-B-4 (photographed 1990) |
| 03 | `3585:7551` | **FR02 · How games mark a first room:** Kane Pixels Level 0 still and the Exit 8 rules-notice poster (both on disk, research only); four principles; keep-clear list |
| 04 | `3585:7582` | **FR03 · Three walls, one picked:** today / W1 / W2 / W3 from E1 and from Level 0 at 4.5 m, with dE; Red's pick on W2 |
| 05 | `3585:7623` | **FR04 · Walls 2, as specified** (rev 2 title; nothing is built): take view, corridor view, the PDF p.110 source; wall, rail, floor and opening values |
| 06 | `3585:7651` | **FR05 · The first room in the run:** rev 2 redraws the plan as **FD-b** in 关卡设计's A26 envelope (editable vectors, north up: 4 × 4 room, D as a 2.4 m pair, the corner chase, set, key, counter, placard, plate, E1 north, E2 west row 1, the EXIT straight north with its flat sign facing the room, Steady lamps, N = 1 cells in yellow, the Relay's far side); the four-orders sheet |
| 07 | `3585:7724` | **FR06 · Who changes what:** the four lines that must agree, plus visual, graphic, sound and "not blocked" |

- **Slot images:** 20 slots, 16 distinct files, each uploaded as a raw JPEG POST (HTTP 200 with an imageHash). Every slot holds one IMAGE fill.
  - The files are label-free board copies in `/Users/redwang/FrontRoomsVisualWork/station/figma_first_room/` (outside the repo, metadata-free, long edge ≤ 1600).
  - Their sources: the sweep 4 raw renders, the *Interiors* crops, the stills on disk, and the evidence part of the s3 orders sheet.
- **Checks:** one composition screenshot of the section, then one fix round:
  - the FR05 plan notes moved off the title and the plan;
  - the orders chip moved to the image's lower left;
  - the FR06 chip reworded.
  - The final section screenshot passed.

**Scratch:**
- `/Users/redwang/FrontRoomsVisualWork/station_first_room/` (sweep scripts, renders, `metrics_final.json`);
- the sweeps' full texts in `synth/sweep_*.md`;
- the re-frame script `synth/make_frames_decided.py`.

## 11. Limits

- The FD-b geometry is a sketch inside 关卡设计's draft envelope. It has not been built or measured, and the envelope itself waits on 关卡设计's final plan.
- The Blender numbers are not in-game numbers.
- The 1988 USCG sheets, the 1980 desk sheet and the NASA photo were seen by title or caption only.
- Most game stay times are [memory].
- The colour of walls 2 under the set's light is not checked in Unity.

## 12. Fix log (rev 2, fix stage `wf_0ec92ae1-5a3`, account B, 17:0x–17:4x)

Two critics (sources: 21 items; decision: 14 items) both said NOT READY. Every item was checked; all were accepted, two with a qualification (marked). Main stayed read-only outside `research/station/`; no Unity, no git, no downloads, no messages. Copies of the rev 1 files and the old frames are in `/Users/redwang/FrontRoomsVisualWork/_partial/station_first_room_fix_1710/`.

**Sources critic**
1. Union Trust is a 1979 photo of a 1929 lobby, and its wall is the lobby's: accepted. loc.gov returned 403 to the re-check; the NPS photo-sheet caption was confirmed by a search snippet ("… Lobby, Security, Photo credit: [OCR: Alien Stress], HABS, 1979"). Fixed in §0, §2.3, §6.1, §9 #2, SOURCES FR16 / FR24, 61 §3 / §4.5 / §18, FR01. 叙事 told to fix 20_layout v1.4 (§8).
2. Outlast reported backwards: accepted; the article was re-read (testers "weren't scared early enough"; a chunk of Act 1 kept for Act 3; scares added early). Fixed in §3 table and principle 5, 61 §3, FR02; URL in S2-34.
3. UBC §1807(f) does not put the station in the lobby: accepted (`01_research.md` line 94 agrees: a ≥ 96 sq ft room approved by the fire department, B-2 over 75 ft). R10 applied in 61 §3, reworded so the fire-entrance location is our story, not the code text. FR01's statement is now "the guard post sits at the entrance lobby". Note for 叙事: `01_research.md` line 34 still calls it "a lobby room".
4. Page numbers: accepted **with a qualification**. Checked on the page images: June PDF 110 = printed 106; Oct PDF 22 = 20 and PDF 28 = 26, so Oct PDF 20 = 18. The other cited June pages (2, 24, 48–49, 79, 125, 131) and Oct p.5 are ads or full-bleed photos with **no folio**, so they are cited "PDF p.N" only; a printed number cannot be read off them. "Painted block" now goes with every citation of p.106. Fixed in 60, 61, SOURCES (FR05–FR10, FW01–FW02), the frames, FR01 and FR04.
5. Memory stated as fact: accepted; tagged [memory] in 61 §3, FR01's caption rewritten.
6. P31 is about consoles: accepted (61 §0, §2.2 here). The same misuse in `NOTE_1022_first_room_DECIDED.md` (叙事's phrasing note) is flagged in §8, not edited.
7. R7–R11 not applied: accepted. R7 (the clock line), R8 (POWER lamp), R9 (not safe), R10 (UBC) are in 61 §2 / §3; R7's plate position follows A26 B3c (beside the door). R11 is applied and then restated for FD-b (61 §4.5). The "after-hours visitor" question goes to 叙事 (61 §0, §11).
8. Stale placard items: accepted. 61 §8 uses 平面视觉's 17:0x solution (YOU ARE HERE = the reader at the placard). The 17:0x provisional layout itself is superseded by FD-b; `NOTE_1022_first_room_placard_layout.md` has an addendum saying so.
9. Lamp-dip timing: accepted ("离开后约 1.3–2 秒", 61 §2, §17).
10. Open asks incomplete, 40 stale: accepted. 61 now carries all four asks (§14–§17) with full detail; `40_for_red_draft.md` has a banner (not sent separately; stale lines named).
11. Fire-door tile: accepted; the fill is now a CROP on the door (left third of HABS PA-5585-B-4), caption "photographed 1990".
12. Board opens on renders; FR04 "as built": accepted; FR00 now leads with the USCG 1986 plan; FR04 is "Walls 2, as specified".
13. Kane quote source: accepted; moved to S2-45 with the URL and date the critic found (not re-fetched by the fix stage).
14. Unrecoverable quotes, missing URLs: accepted; S2-30 and S2-38 marked [unverified]; URLs added to S2-03 and S2-08 from the critic's check.
15. NASA caption: accepted; re-read (caption, credit, 3108 × 2450 px).
16. Door closing behind: accepted; RE7 alone, [search].
17. Old frames in main: accepted; re-framed (§10) and swapped in `images/` at 17:32, the old ones moved to `_partial`.
18. Code line numbers: accepted; symbols plus sha1 (Game fc76109d, RoomStream 4e6e8e7f, ExitPlanner c3523662).
19. `Prop_Stainless`: accepted; `Prop_StainlessBrushed` (new; the code's name) and `Prop_LaminateGrey` (new).
20. Red's 10-21 seam words: accepted; reworded (§4, 61 §6).
21. "15 games": accepted; 14 games and one web series.

**Decision critic**
1. Geometry against 关卡设计's A26 draft: accepted. 61 §4 is now a proposal (FD-b) drawn inside the draft and waiting on its final plan; B1 is put to Red (61 §17); 叙事 and 平面视觉 are warned (61 §4.5, §11; §8 here). **Qualification:** the fix stage cannot message 关卡设计, so "settle before 61 goes out" is left to the 游戏视觉 session: get 关卡设计's yes on FD-b's envelope reading (or its final plan) before sending 61.
2. Forced premises: accepted; corrected in §5.1 and §6.1. The plain rectangle was tested by calculation: it passes in the old 3 × 2 with D in the east cell (the critic was right) and fails in the A26 4 × 4 / 4 × 3 envelope.
3. EXIT orientation: accepted; adopted 关卡设计's layout (EXIT straight north, flat sign facing the room); FR05 redrawn.
4. D drawn wrong: accepted; a 2.4 m pair with 1.12 m leaves, nothing within 2.4 m of D (counter 7.2 m in), the plate on the liner (B3c, 信息设计 to confirm).
5. N section: accepted; A26 units (N = 1 or 2; option 3 removed), owners' positions, M7's N < d − 1 rule, the corrected timing, runners, E2 leavers, dawdlers, the key requirement; "一定看得到" removed.
6. R7–R11: as sources item 7.
7. What exists vs what must be built, and the landing trap: accepted; 61 §1 table, one joint landing (first room + start group + M3 + M6 + M7) in 61 §16; "Alpha 之后" removed.
8. 关卡设计's cost: accepted; 40–55 h quoted as theirs, our numbers tagged, the fallback described as the player feels it.
9. Red's asks mis-stated: accepted; header lists the four asks; the 15:44 row says only "这个版本可以" and the walls, with "every floor" as R1's confirm item.
10. Light leak in the first 10 s: accepted; 61 §2, §7 and §15 (`StartLampsNorth`, A26 B10).
11. Images: as sources item 17.
12. Overclaimed sources: accepted (as sources items 1, 2, 3, 4, 5, 16).
13. Numbers from W′ rev 4; Weber: accepted; said in 61 §3 and §4 here; Weber defined, no pass line.
14. Minor: accepted; the 系统设计 row now asks to align C32's "door shut or N cells"; the 平面视觉 row records `sheetA_room` delivered (VL411); the 教程 row names §3 row 1 only; FR05's N text drops option 3.

**Figma (file `0tCbAiVUlrPId3RWd9LRif`, section `3585:7488`), rev 2:** 40 text nodes edited in P1 styles (Bayon labels with digits and "·" in Plex Mono Medium); FR00 gained the USCG plan tile (`3604:14`, chips `3604:15`, `3604:17`); FR01 fire-door fill cropped; FR05's orders image replaced (imageHash 85d27248…); FR05's plan redrawn as FD-b (`3605:8`, chip `3605:76`); the FD-a plan frame `3585:7665` hidden and renamed "withdrawn". One section screenshot checked per slide after the edits.
