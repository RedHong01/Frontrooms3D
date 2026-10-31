# Bathroom rooms · 10 · Research: the place, the 1990 fittings, the water

Date: 2026-10-30 to 10-31. Line: 浴室房间调研 (account B `local_fe9beaf3` to 10-31 00:05, then account A `local_bd00d052`; handoff `Takeover_2026-10-18/handoff/bathroom.md`). System L8, decision A48, schedule R3.

Red, 2026-10-30 23:30, 系统设计 chat: "把浴室房间也加上日程，浴室房间需要先做research，然后这个research也包括水体逻辑（因为是泳池相关的环境）并且思考在游戏中当时年代的泳池陈设有什么".

**Status: research and proposal. Nothing is built.** No game code, no Unity run, no git command, no download. Red's picks are in `20_for_red.md`; the work estimate is in `30_estimate.md`; every source is in `SOURCES.md`.

**Marks.** **SP** = a starting value for playtest. **EST** = my estimate, not measured. **[S12]** = a row in `SOURCES.md`. Code references are to main on 2026-10-30.

---

## 0. Short answer

1. **What it is.** A tenant fitness center on a B level of the office building: locker rooms, gang showers, a 60 ft lap pool. Real office buildings did this, and they put it at the bottom, because 1.4 m of water weighs about five and a half times what an office floor is designed to carry. The Rayburn House Office Building opened in 1965 with its pool in the sub-basement, under the offices. So the game's B levels are where this room belongs. It is the opposite of the Poolrooms: every object has a job, a code and a sign.
2. **What stood in it.** See §2: tile, depth marks, gutter, ladders, guard chair, pace clock, lockers, showers, fixtures, lamps, plant, each with its dating and a reuse or new-kit call. Most of it is fixed by a 1986 Army pool manual and two 1960 trade catalogues: white pool with dark lane lines, depth marks in feet every 25 ft or less, a gutter drain every 10 ft (one per grid cell), 1.9 in stainless ladders, one 6 ft guard chair, louvred steel lockers in green, grey or beige, wet-niche lamps, a pump room with colour-coded pipes and a daily log. NO DIVING in words only: the pictogram is not dated before 1993.
3. **How the water works.** One rule set, chosen so that it breaks nothing in Relay v2:
   - All water is wadeable. The deepest point is 4 ft 6 in (1.37 m). No swimming, no diving, no drowning, no underwater camera. A run still ends only when caught.
   - Wading is half speed and there is no running in it. The Relay wades under the same law, so a walker still outpaces anything but a chase.
   - The Relay enters the water. A monster with no ranged attack that stayed out would turn water into a safe zone, the failure The Last of Us Part II was designed to avoid.
   - Water changes nothing for hearing or warnings: the path field stays as it is. Water adds no loudness. It takes away silence: the slow gait in water is heard at 6 m.
   - Wet prints are for the player's eyes. The Relay never reads them.
   - The water level never changes in v1.
   - The camera stays above the surface, so there is no underwater rendering to build.

---

## 1. What this place is

### 1.1 What earlier research already says (not redone)

- **Poolrooms lore and level art:** `research/level_transitions/01_ip_research.md`. Level 37, Sublimity; POOLS keeps one material family and changes tile colour, water level, light and arch shape, and light from the next space comes first (P7). Those joins are at ladders, stairs and arches.
- **The A24 film:** `research/office_and_film/02_film_shots.md`, T09: a raised tiled basin, a chrome ladder and white plastic patio chairs at trailer 2:06.5. Dwell calls it "a rotting swimming pool". The film's set is the state we must not copy.
- **Small teams:** `research/office_and_film/03_ip_canon.md`: POOLS and Anemoiapolis are Unity, and their realism comes from restraint, real dimensions and photo-referenced materials.
- **Real building joins:** `research/level_transitions/02_real_buildings.md`: a finish never ends on a bare wall. It stops on a frame, a corner bead or a header.

### 1.2 Real buildings: was this ever a thing?

Yes. Four kinds, dated.

| Kind | Example | What the source says | Source |
|---|---|---|---|
| Office building with a pool under the offices | Rayburn House Office Building, Washington, opened 1964–65 | Opened with men's and women's gymnasiums and a swimming pool. It has three levels of underground parking, then a basement and a sub-basement. The gym with its pool is in the sub-basement, with lockers, paddle-ball courts and weight rooms. | [S1] [S2] |
| Older federal office precedents | Cannon House Office Building (1908): basement pool reported in 1907. Pentagon Officers' Athletic Club pool. White House pool, under the press room, 1933–70. | Secondary source, one article. Good for flavour, weak for detail. | [S3] |
| Company fitness center with a pool | Kimberly-Clark, Neenah, 1977 | A $2.5 million health program at its headquarters. The exercise building had a 25 m pool, a 100 m suspended track, exercise rooms and a gym. | [S4] [S5] |
| Company fitness center, no pool confirmed | General Electric, Fairfield, 1981–82: exercise, aerobic, track, medical, lockers, lounge. Mesa Petroleum, Amarillo, 1979: 30,000 sq ft, $2.5 million (search summary only; page returned 403). | | [S6] [S7] |
| Developer club to win office tenants | Birtcher and Koll, Irvine, mid-1980s; clubs opened Feb 1990 and July 1992 | Each developer built a "super health club" to entice tenants to its office buildings, at more than $20 million each. An indoor pool was among the features one project dropped. | [S8] |
| Hotel inside an office complex | Vista International, World Trade Center, 1981 | A 22nd-floor gym with a pool and running track, the largest hotel gym in the city at the time. | [S9] |

**Checked and thrown out.** A search summary said Wells Fargo Plaza, Houston, had a health club from 1982. The page itself says the club was built in 2005–06 [S10]. A search summary tied a lap pool to Park Center, Alexandria, in the 1980s; the source describes a later tenant [S11]. I did not find one named 1980s office tower with a basement pool that I could stand behind. Rayburn is the closest real case. I treat "1980s tenant club with a pool on a B level" as plausible, built from these parts, not as a documented common type. **That is a limit of this research:** the common 1990 pool building was the YMCA, the hotel, the school and the athletic club. An office tower with its own pool was rare and expensive. The fiction carries that well (§1.5): this building has more amenities than it should.

### 1.3 Why it is at the bottom

A pool is heavy. 1.37 m of water loads the slab at 13.4 kPa (281 psf). An office floor is designed for about 2.4 kPa (50 psf) live load [EST: my arithmetic against the common design figure]. A pool above occupied floors needs a structure that spans between supports and carries everything, which costs far more; a pool on grade sits on the ground, and the soil dug out weighs more than the water put back [S12]. So a pool in an office building goes on the lowest slab unless someone pays a lot to do otherwise.

The game's fiction already goes down (C22, C28): LEVEL 0, then B1, B2. That gives this room a reason to exist where the player finds it, and a reason not to exist on LEVEL 0.

**Plant and water go with it.** Sump pits sit at the lowest level, and a sump with only a line-powered pump stops in a power failure [S13]. The EGRESS fiction is a power-failure exit plan. So damp floors, standing water and tide marks on B levels are honest. That is an option for later, not part of v1 (§3.6).

### 1.4 Four origins compared

| Origin | Story | Fits EGRESS | Look | Verdict |
|---|---|---|---|---|
| **A. Tenant fitness center** | The building's owner put a club on a B level to win tenants. Signs say FITNESS CENTER, TENANTS ONLY. | Best: it is one more piece of the building that works perfectly with nobody in it | 1980s renovation: small mosaic, one colour band, vapor-tight lamps, lockers | **Recommended** |
| B. Older bathhouse under the tower | The tower was built over a 1920s athletic club or Turkish bath, kept in service | Good, and it adds depth in time | Terracotta, arches, marble, brass | Rejected for v1: arches and old tile drift straight into the Poolrooms look, and second-hand limits say ≤ about 1955 for fittings |
| C. Hotel-style pool | The complex has a hotel; guests use the pool | Weak: no hotel exists anywhere in the game | Carpeted corridors, resort tile, plants | Rejected: it needs a hotel to explain it |
| D. Plant only | Fire-water tank, sump, boiler rooms, no pool | Honest but dry of meaning | Concrete, pipes | Kept as the pump room next to A, not as the room itself |

### 1.5 The fiction

Rules from `research/narrative/40_floors_exit_tutorial_strings.md` §1: the building is honest about itself and blind to what it sends; every exit leads further in; the clocks are right but the building's hours are not.

- **It is in service.** The pool is clean and still, the pump runs, the lamps work, the log is filled in. Nobody is there. This is the opposite of the film's rotting pool, and it is the same logic as the security desk's TV still playing.
- **The pool log (for 叙事设计).** A clipboard on the wall by the pump room door: pH, chlorine, initials. The last line carries today's date and an hour ago, computed from the live clock as the guard's log is. Somebody tested this water. Nobody can have. The kit exists (`Kit_Clipboard_Wall`). The strings are 叙事设计's.
- **The alarm.** The panel's zones are floors. A real system of the period reports sprinkler water flow as an initiating device, with a retard of up to about 90 s [S14]. So a **waterflow** device is an honest future trigger (§3.6). Showers and pool halls are where smoke detectors are avoided because steam trips them [S15]. So a dead-circuit sanctuary (L3) in a sauna or shower bay would have a real reason. Both are options, not v1.
- **The Relay.** A pool hall is a 5.4 m room, and Tall rooms are where it straightens up to 3.1–3.3 m (`research/hunter/11_squeezed_giant.md`). In water to mid-thigh, it is the one place you see its true size with something to measure it against.

### 1.6 How it joins Level 0, Office and the Station

Order of meeting, from the first hint to the deep end:

1. **A washroom (optional small module, any floor).** Real 1990 office floors have restrooms and the game has none. A 2 × 3 cell tiled room, door with a MEN or WOMEN plate. It seeds the tile family early, and costs no water. It also raises the mirror question (`20_for_red.md`).
2. **The approach (B levels).** The Level 0 paper starts to fail near wet rooms: more peel, more ceiling stain (`Paper_Peel`, `_StainStrength`, `L0_CeilingTile_Loose` exist). Sound changes first: pump hum, a long tail on your own steps. POOLS' rule, light from the next space first, becomes sound from the next space first.
3. **The border.** A finish stops on something built (`02_real_buildings.md`). Level transitions' Frame kit already owns zone borders (A47). Carpet stops at a metal bar, and tile starts inside a steel frame. A42 gives wet-side doors a vision panel, so a player can look through at the pool before going in. Wired glass fits a wet area.
4. **The towel desk (the Station's echo).** A short counter (`Kit_StationCounter_*`), a board of locker keys (`Kit_KeyHookBoard`), a sign-in book (`Kit_RegisterBook`). The same furniture language as the security desk, smaller. No key lives here: B levels have no floor key (Q1 B, Red 10-29).
5. **Lockers, then showers, then the pool.** The real sequence of a club, and a real hygiene rule: you pass the showers to reach the deck.
6. **The EXIT across the water.** STAIR A's door is on the far wall of the pool hall, its sign reflected in the water (§1.8).

### 1.7 Ours, not the Poolrooms

Poolrooms images and the games built on them share a recognisable set: white or pale tile on every surface, arches and curved walls, water crossing doorways, no signs, no fixtures, daylight from nowhere (POOLS and Escape the Backrooms footage, §3.6). We do the reverse.

| Poolrooms | FrontRooms |
|---|---|
| Endless, no purpose | One club with a plan you can read off the evacuation sheet |
| Arches, curves, domes | Rectangles on the 3 m grid, steel frames, one real ceiling height |
| Tile on every surface | Tile to a line. Above it paint or block, with an acoustic deck overhead |
| White, pale blue | One period colour band on a warm off-white (§2) |
| No text | Depth marks, rules board, lockers numbered, NO DIVING |
| Water through doors | Water in a basin, with a curb and a gutter |
| Daylight glow | Vapor-tight lamps, lit from the pool's own wet-niche lights |
| Dream | A building that keeps working with nobody in it |

### 1.8 Fit on the map

Checked against the code on 2026-10-30.

- **Size.** A module cannot leave its chunk, so 8 × 8 cells (24 m) is the limit (`RoomModuleData.MaxCells`, `RoomModuleStamp.Apply`). A hall of **5 × 8 cells** (14.84 × 23.84 m clear) takes a pool of **3 × 6 cells: 9 × 18 m**, with a 3 m deck on every side. 18 m is 59 ft, a 20-yard lap pool. 9 m gives 4 lanes at 2.25 m. Those are real numbers that land on our grid.
- **Height.** Tall, 5.4 m. A Tall room's borders are windows plus one arch (C33). A viewing window into a pool from the corridor is what real natatoriums have, so the map rule becomes true to life.
- **Placement.** `PlaceModules` rolls per room and per revision and needs a generated room big enough, and generated rooms stop at 7 cells. So a pool hall cannot come from it. It must be planned, stable by coordinate, like the station: a group carved into the map's data before `Begin`, as `FrontRoomsStartGroup` does. Revisit shifts must leave it alone (§10's rule for sanctuaries and trigger rooms).
- **The hole.** Floors are one box per cell (`Solid(b, theme.floor, …)`, MapWorld ~2410), so a basin means skipping those cells and building its own mesh. Floors regenerate in place at one root, so nothing lies under a basin except the reserved stair shaft cells X1, X2 (walkable stair, `60_walkable_stair_for_map.md`), which the planner must avoid.
- **Theme.** `ZoneTheme { Level0, Office }` is a byte; append `Bath`. But a station "takes the station's skin (MaterialsOf), its zone stays Level 0", and that is the cheaper path: the group is a skin over cells, not a zone.
- **Route.** On B levels the EXIT is unlocked and there are no keys, and an arrow-follower needs only seconds to reach it (16.6 s on floor 1, measured 10-30). A room off the route goes unseen. So the group must sit on the route. See `20_for_red.md`.

**A 1.4 m hole breaks no Relay rule, because the Relay never leaves its plane.** It walks a flat plane (`step.y = 0`, `target.y = position.y`, `feet.y = world.CellCenter(cell).y`) and its capsule is tested 0.40–1.95 m up. So its logic can stay at y = 0 over a basin, unchanged. Only the rig drops by the local depth. Its logical eye at 1.6 m stays near its real head (3.2 m tall, feet 1.37 m down). Sight still clears the coping: from a 1.6 m eye to a player's eye 0.25–0.55 m above the deck plane, the line is always above the deck. There is no hiding under the coping.

---

## 2. The 1990 pool room

**Evidence base.** One period manual carries most of this: the US Army's *Swimming Pool Operation and Maintenance*, TM 5-662, 28 February 1986 [S40]. Two trade catalogues of 1960 give product sizes [S41, S42], a 1965 tile book gives tile sizes and special shapes [S43], and four archive photographs were checked by eye [P1–P4]. Modern state codes are used only where the period text is silent, and are marked LATER. Anything marked **unverified** is my reading of common practice with no dated source behind it: 平面视觉 and 游戏视觉 should treat those rows as proposals, not facts. The survey's limits are in `SOURCES.md` §2.

**Era rule.** Fiction A: the club was fitted out in the mid-1980s in a building that the game locks to 1990. So a fitting may be any age from about 1960 to 1989, as long as it was still sold or still in service in 1990. Nothing that arrived after 1990 may appear (§2.4).

### 2.1 Our pool in real numbers

The hall and pool sizes come from §1.8. The counts below follow from the period rules. [EST] marks my arithmetic.

| Quantity | Value | Rule it follows |
|---|---|---|
| Water area | 9 × 18 m = 162 m² = 1,744 sq ft | Under 2,000 sq ft: **one** guard chair and **one** rescue set [S40 2-13] |
| Depth | 3 ft 6 in (1.07 m) to 4 ft 6 in (1.37 m) | Training depth: no diving board, NO DIVING is true |
| Depth marks | 3 on each long side (shallow, middle, deep) plus one on each end wall: 8 on the wall, the same 8 on the deck | ≤ 25 ft (7.6 m) apart, numerals ≥ 4 in (102 mm), contrasting, in feet [S40 2-5] |
| Volume | 198 m³ (52,300 US gal) at a mean depth of 1.22 m [EST] | |
| Recirculation | 145 gal/min (550 L/min) for a 6 h turnover [EST] | Turnover every 6 to 8 h [S40 5-5] |
| Filter | 48 sq ft of rapid sand filter: three vertical tanks of 4 ft 6 in (1.37 m) diameter [EST] | 3 gal/min per sq ft [S40 12-7b; S41] |
| Chlorinator | about 18 lb (8 kg) a day [EST] | gal/min ÷ 8 = lb a day [S40 8-2] |
| Underwater lights | about 1,750 W: six 300 W wet-niche lamps, three on each long side [EST] | About 1 W per sq ft of water [S41]; 300 W PAR56 is the common lamp (§2.2, row 16) |
| Gutter drains | one every 10 ft (3.05 m): **one per grid cell** | Gutter drains on 10 ft centres, fall 1/8 in per ft [S40 fig 3-2, 3-3] |
| Water below deck | 0.15–0.2 m | About 6 in between water and deck [S40 3-4 to 3-6, ambiguous]; the 0.2 m climb-out of §3.2 lands inside it |

The 10 ft gutter-drain spacing lands on our 3 m cells within 2 %. That is luck worth keeping: one grate per cell along the gutter, with no special case.

**Layout** (the order of §1.6): towel desk → MEN and WOMEN locker rooms → gang showers → deck → pool. The pump room and a separate chlorine room open off the deck. The guard chair stands at mid-length on a long side, with the phone and the rescue set on the wall behind it. The EXIT is across the water (§1.6).

### 2.2 What stood in it

Call: **reuse** = an existing kit asset (names checked on 10-31); **new** = new kit; **skip** = not in v1.

| # | Item | 1990 form (real spec) | Evidence | Call | v1 |
|---|---|---|---|---|---|
| 1 | Pool shell | White, light blue or aqua. Dark lane lines 8–12 in (0.2–0.3 m) wide that stop 2 m short of the end wall in a 1 m cross bar, with a target on the end wall. Small glazed mosaic (1 × 1 or 2 × 2 in) is **unverified**; the 1965 tile book says tile was "widely used in swimming pools". | [S40 2-8]; FINA rule, LATER [S44]; lane T-ends and end-wall targets seen in a real pool [P1]; [S43 p.14] | new surface: pool mosaic, lane line, target | yes |
| 2 | Deck | Non-slip where wet. Sloped 1/4–1/2 in per ft to drains. Non-skid tile came in waffle, pebble and silicon-carbide faces; quarry tile was a floor tile of the time. Red-brown unglazed quarry 6 × 6 in on a pool deck is **unverified** as a 1980s norm. | [S40 2-11, 3-5]; slope LATER [S45]; [S43 p.12–13] | new surface (colour is decision D8) | yes |
| 3 | Walls | Glazed tile to a line, painted block above. Wall tile 3⅝ × 7⅝ in and 2 × 10 in in the standard series, with cove base and bullnose trim. Glazed structural block in a real basement shower room: dark below, light above [P3]. Cool blues, blue-greens, turquoise; no strong yellow or grey-green, because they change the water's apparent colour. Mint-green walls in a real 1974 school pool [P2]. | [S43 p.6, p.8–9, p.12]; colours: MIL-HDBK, undated [S45]; [P2] [P3] | new surface: wall tile with cove and bullnose; band colour is D8 | yes |
| 4 | Coping and gutter | A recessed overflow gutter all round: drains on 10 ft centres, 1/8 in per ft fall, return inlets 12 in below the water. Skimmers every 15–25 ft were the other type. The roll-out and deck-level gutter are **unverified** for 1990. | [S40 3-2 to 3-6, fig 3-2, 3-3] | new basin-edge kit: coping, gutter lip, gutter grate | yes |
| 5 | Main drain | At the deepest point, grated. A plain flat grate is period-correct: anti-entrapment covers and dual drains came with the 2007 law; a 1987 suction-fitting standard existed. | [S40 10-3a, 2-12]; [S46] | new small prop | yes |
| 6 | Depth marks | Numerals ≥ 4 in, contrasting, in feet, ≤ 25 ft apart, at or above the water line. Tile makers sold special shapes for depth and distance markers. Exact wording (4 FT 6 IN, 4'-6", 4½ FT) is **unverified**. | [S40 2-5]; [S43 p.14]; wording LATER [S44] | 平面视觉: inset tiles on wall and deck | yes |
| 7 | NO DIVING | The 1986 manual lists diving in shallow water among practices to post as prohibited. The slashed-diver pictogram has **no source dated before 1993**, so v1 uses words only: NO DIVING in the deck tile. | [S40 2-3a]; pictogram LATER [S44] | 平面视觉 | yes, words only |
| 8 | Rules board | Topics: no swimming alone, none soon after eating, overexposure, horseplay, diving in shallow water, thunderstorms; shower before entering; no shoes on the deck. Board material and layout **unverified**. | [S40 2-3a, 4-5, 5-3] | new board; strings 叙事设计, set 平面视觉 | yes |
| 9 | Ladders and steps | Stainless steel, 1.9 in (48 mm) outside diameter rails, 3 or 4 treads. Army pools also used painted steel. Recessed steps at the shallow corners are **unverified**. | [S41 F-109, F-110]; [S40 15-13] | new prop, hero LOD0: two ladders at the deep end, corner steps at the shallow end | yes |
| 10 | Guard chair | One per 2,000 sq ft. Seat 6 ft (1.83 m) above the deck. Hardwood seat and footrest (F-107), or steel and aluminium with a fibreglass seat, umbrella rack and life ring (F-504). | [S40 2-13]; [S41] | new prop | yes |
| 11 | Rescue set | Hook pole longer than half the pool's width (so ≥ 4.5 m), hook opening ≥ 18 in; throwing rope ≥ half the width; flutter board about 1 × 3 ft. Ring buoys drawn in the manual. | [S40 2-13b, fig 2-1] | new: ring buoy, pole on wall brackets | yes |
| 12 | Telephone | A telephone in a prominent place. | [S40 2-2d] | wall phone: new variant of `Kit_DeskPhone_Corded` | yes |
| 13 | Pace clock | A large sweep-hand clock on the end wall. First pace clock 1946; electronic timing from 1972; when LED digits replaced sweep hands is **unverified**, so sweep hands are the safe 1990 choice. | [S47] | new face on the `Kit_WallClock` logic: it runs on the live clock like every clock in the building | optional |
| 14 | Lane ropes | Floating ropes on a storage reel. **Unverified** for a club this size. | none | skip (floor lines only) | no |
| 15 | Overhead lamps | Vapour-tight sealed fixtures or metal-halide high bays. Nothing dated for 1990; chloramines corrode fixtures, so sealed and galvanised parts are the reason. | [S48]; [S45] | new: vapour-tight fluorescent, surface-mounted in rows (the hall is 5.4 m, which suits it). Reuse `Kit_EmergencyTwinHead` | yes |
| 16 | Underwater lights | Wet-niche, incandescent, grounded and ground-fault protected (NEC rule from 1968 or 1975, sources disagree; either way long before 1990). 400, 500 and 1,000 W in 1960; a 300 W PAR56 is what today's retrofits replace. | [S40 2-10, 2-12j]; [S41]; [S49] | new: niche ring and lens; each one is a lamp in the room's set (§3.5) | yes |
| 17 | Ceiling | No dropped ceiling over the pool; no open metal slats; galvanised, epoxy or urethane finishes against corrosion. Wood-fibre acoustic deck is **unverified** for 1990. | [S45] | new surface: painted structure or acoustic deck (sound needs it, §3.4) | yes |
| 18 | Air | Dehumidifying plant was in scope in 1986. Modern practice: 50–60 % RH, pool hall at negative pressure, supply on the dry side, return over the wet side. | [S40 1-3b]; LATER [S50] | dressing: supply diffusers in the locker rooms, high return grilles over the deck, a fogged edge on the viewing window | dressing |
| 19 | Lockers | Single tier 12, 15 or 18 in wide, 12–21 in deep, 60 or 72 in tall on 6 in legs (78 in overall). Double tier in 36 in tiers. Six louvres at the top and bottom of each door; 16-gauge doors. Enamel in Willow Green, grey or beige. Master-keyed flat-key locks, combination locks or padlock hasps. A 1960 catalogue: still the form in 1990, colours **unverified** for 1990. | [S42] | new prop in two variants (single and double tier), door open and shut. Club key tags reuse `Kit_KeyTag_*` | yes |
| 20 | Benches | Hardwood bench on steel pedestals between locker banks. **Unverified.** | none | new prop | yes |
| 21 | Gang showers | Wall rows of heads on a tiled or block half wall, or free-standing column showers (from the 1940s, in schools and YMCAs). A central mixing valve against scalding. Floors slope to drains. Push-button metering valves are **unverified**. | [P3]; [S51]; [S40 2-12, 5-3]; [S45] | new: head and valve set, floor drain | yes |
| 22 | Washroom | Exposed manual flush valves (a form sold since 1906), wall-hung fixtures, steel partitions, a push-button warm-air hand dryer (a 1948 form), mirrors. | [S52]; [S53] | new module; mirrors are decision D7 | D7 |
| 23 | Towel desk | Counter, key board, sign-in book, towel stacks, a towel bin. | §1.6 | reuse `Kit_StationCounter_*`, `Kit_KeyHookBoard`, `Kit_RegisterBook`; new towel stack and bin | yes |
| 24 | Pump room | Sand filters, a hair-and-lint strainer before the pump, the pump; pipes in colour: blue supply, green filtered, brown pool return, yellow chlorine, red backwash and sewer. A valve index and a piping diagram on the wall. The daily log on a printed form. A DPD and phenol-red test kit. | [S40 10-4, 15-4, 15-14, 16-2, 16-4, App C] | v1: the door and the log outside it. Room dressing later: filter tank, pump, strainer, valve wheels, pipe runs. Reuse `Kit_Clipboard_Wall`, `Kit_ConduitEMT`, `Kit_TransBucket` | door only |
| 25 | Chlorine room | Gas chlorine in its own fire-resistant room with two exits, the gas mask kept outside it, an ammonia bottle for leak checks. | [S40 8-5 to 8-7] | later: a locked steel door with a sign | later |
| 26 | Pool log values | pH 7.2–7.8; free chlorine 1.0–1.5 ppm ideal, 3.0 max; columns for time, pH, free and combined chlorine, initials. | [S40 5-8, 6-9, 6-11, App B, App C, 15-4] | strings 叙事设计 (§1.5) | yes |
| 27 | Sauna, whirlpool, steam, scale | **Unverified.** | none | skip | no |
| 28 | Exercise room | StairMaster by the late 1980s, Nautilus, Lifecycle; NordicTrack club model 1990. | [S54] | later room | no |
| 29 | Drinking fountain | | | reuse `Kit_WaterCooler` | yes |

Count for v1: **9 reused assets, about 16 new props and 6 new surfaces** (pool mosaic, lane line and target, deck, wall tile with trim, basin edge, ceiling).

### 2.3 Signs and strings

Every sign is the building talking about itself, so each one belongs to 叙事设计 (words) and 平面视觉 (faces, sizes, set); type goes through 平面视觉. The list for v1: FITNESS CENTER · TENANTS ONLY at the entry; MEN and WOMEN plates; SHOWER BEFORE ENTERING (period topic [S40 4-5], wording unverified); POOL RULES; NO DIVING; the 8 depth marks; locker numbers; PUMP ROOM; the pool log. **Open question for the localisation line:** whether world signs are translated or stay English. These signs follow whatever rule the station and EXIT signs follow.

### 2.4 Do not show in a 1990 room

| Thing | Arrived | Source |
|---|---|---|
| Pool lifts, sloped entries | 2010 standard, pools from 2012 | [S55] |
| Anti-entrapment covers, dual main drains, vacuum release | 2007 law, in force 2008 | [S46] |
| LED pool lights | mid to late 2000s | [S49] |
| UV or silver disinfection | rejected by the 1986 manual | [S40 7-12, 7-13] |
| Sensor-operated flush valves and taps; dual flush | date unverified; dual flush 2008 | [S52] |
| Jet-blade hand dryers | Japan 1993; the US ones later | [S53] |
| 1.6 gal toilets, 2.5 gal/min shower heads | 1992 law, effective 1994 (from memory) | unverified |
| ADA-era fittings: roll-in showers, fold-down seats, Braille signs | ADA signed July 1990, new buildings from 1993 (from memory) | unverified |
| Wall-mounted defibrillator | late 1990s (from memory) | unverified |
| Slashed-diver pictogram | no source before 1993 | §2.2 row 7 |
| Cordless or cell phone at the guard chair | the 1986 manual wants a fixed phone | [S40 2-2d] |

### 2.5 Colour and material, as a brief for 游戏视觉

- **Fixed by evidence:** pool interior white or very light blue with dark lane lines; no strong yellow or grey-green anywhere in the hall; sealed lamps; no dropped ceiling over the water.
- **Open (decision D8, best settled on an options sheet as the door veneer was):** the band colour (teal or aqua, mint green as in [P2], or navy on bone), the deck (red quarry or bone non-skid), the locker enamel (beige, grey or green).
- **The Poolrooms guard, in one line each:** tile stops at a line and never covers the ceiling; rectangles on the grid, never an arch (the 1900s natatorium in [P4] is exactly what to avoid); signs and numbers everywhere; water only in a basin.

---

## 3. Water logic

### 3.1 The rules this has to obey

Fixed by earlier decisions, so water may not break them:

| Rule | Source | What it forces |
|---|---|---|
| A run ends only when caught | C22, Red 10-02 | No drowning, no shock, no fall death |
| Dark is never cover; cues are never a radar; no Relay state on the HUD | COMMON rule 12 | Water is never cover. A ripple may not act as a remote sensor |
| Sensory honesty: the hunter gets an estimate, never the player's cell | RELAY §4, §7 | The Relay does not read wet prints unless it sees them |
| Walking escapes anything but a chase: every non-chase speed is below the walk | RELAY §5 | Water must keep that ratio |
| One gait rule on every platform; the slow gait is the quiet verb | A36, Red 10-30 | No platform differences. A change to "slow = silent" needs Red |
| One opening table for hearing and warnings; shutting a door adds 9 m | RELAY §7 | Water does not add a new field |
| Only careful reading pauses the world | A43 | No pause in water |
| No new AudioSources; events only | AUDIO_CONTRACT | Surface and zone labels, append-only |
| Photosensitive limits; Reduce Motion | RELAY §9.5, §9.6 | Caustics are a moving repeated pattern and need the Reduce Motion path |
| WebGL logic identical, only looks degrade | COMMON rule 5 | Nothing below may differ on WebGL |

### 3.2 Player

| Class | Where | Depth | Speed | Run | Heard (path m) | Note |
|---|---|---|---|---|---|---|
| **Wet** | Deck, shower floors, locker aisles near showers | film, ≤ 3 mm | full | yes | as on land: slow 0, walk 9, run 27 | Wet Foley only. Prints start |
| **Ankle** | Overflowing shower bays, the pool's top step | 0.05–0.25 m | × 0.85 SP | yes | slow 3, walk 9, run 27 | A splash on each step |
| **Wade** | The pool | 0.25–1.37 m | × 0.5 SP | no | slow 6, walk 9 | Waist to chest. Both gaits at half speed |

No **Deep** class in v1.

- **Depth.** Shallow end 3 ft 6 in (1.07 m), deep end 4 ft 6 in (1.37 m), a straight slope between. A real training depth: no diving, and NO DIVING is true. Player eye 1.62 m, so the eye is 0.25–0.55 m above the surface and the camera never goes under. A 0.12 m floor clamps the plunge dip.
- **Getting in.** Corner steps at the shallow end (quiet). Or walk off the edge: a plunge, one loud event (§3.4), and you land on your feet.
- **Getting out.** Steps, ladders, or push against any wall: the existing climb (`TryStartClimb`, `ClimbSeconds` 0.6 s) with a bigger lift. The deck stands about 0.2 m above the water. That is the height Naughty Dog found reads as climbable: ledges at 0.25 m, with 0.25–0.75 m ambiguous [S20].
- **Stamina.** No drain, normal refill. Wading is already the cost. A winded player climbing out into a chase would pay twice.
- **Fall, slip, cramp, shock:** none. One way to die.
- **Why × 0.5.** Worked in §3.7. From the pool's centre line it takes 3.4 s to get out at × 0.5, and stage 2 gives 3.6–3.8 s before a hold ends. Below × 0.5 the player who reacts to the steps cannot get out in time.
- **Faster than real, on purpose.** Real people wading at chest depth walk about 2.7 times slower than on land (× 0.37), and below about 0.35 m/s the effort is close to land walking; knee-deep wading costs about the same as walking [S69]. × 0.5 is a game number chosen for the margin above, not a measurement.

### 3.3 Relay

**It enters the water, under the same law.** `k` multiplies every Relay speed in the Wade and Ankle classes, by the cell it stands in. Investigate 2.6 → 1.3, chase 4.4 → 2.2, walker 3.2 → 1.6. So:

- every non-chase speed stays below the walk, in water too (1.3 < 1.6);
- a chase in water is as survivable as on land for distance: both at half speed, a player locked on at 6 m wades 26 m before being caught, and the pool is 18 m long (§3.7);
- what kills a player in the pool is a Relay that stays on the deck, which is a good lesson.

**The three alternatives.** Why they lose:

| Rule | Result |
|---|---|
| **It will not enter** | Water is a free sanctuary. Naughty Dog's enemies stay out because they shoot from the edge; ours has no ranged attack, so nothing punishes staying in. Licea names the failure: Uncharted 4 let you swim away and wait [S20]. A pool is not a sanctuary by the §10 rules, and the validator's sanctuary budget would be broken by every pool |
| **It wades faster (it is 3.2 m tall)** | True to physics, but the walk no longer escapes an investigation in water (2.6 × 0.7 = 1.8 > 1.6). It breaks a fixed rule |
| **It stays out until it locks on** | A rule the player cannot read, and it needs a new state |

**Paths.** The path field is unchanged:

- F, W, hearing and warnings are about distance and openings. Water is +0. Sound crosses a pool as freely as a room.
- Only the route planner changes: a water cell costs `1/k` of its length, so the Relay crosses or walks round by time. It has one number per cell, no new field.
- Basin edges are Open passages for nav. The rig plays a 0.6 s step down or up, as it does at a window (RELAY §5).
- Arrive placement: never a water cell (the rig cannot appear standing in water), and Relay entries stay on dry floor.

**Senses.** Sight, the cone, the meter, the gate and C37 are unchanged. Water never hides anyone. A player's eye stays above the deck plane (§1.8), so no rule needs a depth term. Touch is unchanged. Catch is on the flat, unchanged.

**Warnings.** Stage bands are unchanged and honest. A Relay in the same hall is close. Stage-1 and 2 timing only gets gentler, since it wades slowly.

**A new leak to close.** A reflection shows the Relay where the visibility tests say it cannot be seen. `PlayerSeesRelay` and Arrive's "unseen" tests use rays from the eye. Reflected in the pool, a Relay around a corner can be visible, and "it never appears or vanishes in view" (RELAY §5, §11) can break in the water. Fix: in a room with water, those tests add one more ray from the eye mirrored in the water plane. This is the same class of problem G14 met in glass (`hunter/production_b/10_plan.md`: the Relay is registered in the RT scene through a baked mirror). Being seen in the water first is good horror. Popping in the water is not.

### 3.4 Sound

Rule: **water adds no loudness; it takes away silence.** A flat "water is loud" on top of half speed pays twice. TLOU II's team tried splash audio on enemies and it made combat "exponentially more difficult" [S20]; the Thief guide says splashing draws attention [S21]. So noise stays, but gently.

| Event | Loudness (path m × hearing) | Attention | Why |
|---|---|---|---|
| Wade, walk gait | 9 | 0 | Same as a walk |
| Wade, slow gait | 6 | 0 | **The change.** There is no silent way through water |
| Ankle, slow gait | 3 | 0 | Half way |
| Ankle, run | 27 | +3 | Same as a sprint step |
| Plunge (off the edge) | 18 | +8 | As a barge: one loud mistake |
| Climb out | 6 | 0 | Water pouring off you, as an ease |
| Relay wading | never (RELAY: anything the Relay causes) | 0 | The player hears it under the stage-2 rules |

Foley and FMOD (声音设计 owns):
- `Surface` labels are append-only: 0 Carpet, 1 CarpetTile, 2 Metal, 3 VCT. New: TileWet, Ankle, Wade.
- A new Zone, Pool, with a long tail. Natatoriums are very live rooms: untreated halls run 5–10 s RT60, about 6–8 s at 500 Hz, and treated ones aim for 1.5–2 s [S60, consultant pages, not opened]. Sabine for our hall (14.84 × 23.84 × 5.4 m, 1,910 m³, 1,125 m² of surface) [EST]: bare tile, glass and water (α 0.03–0.05) 9.1–5.5 s; with a painted-over acoustic deck on the ceiling (α ≈ 0.3) 2.4 s; with a new one (α ≈ 0.6) 1.3 s. **SP: a tail of 2.5–4 s**, a club that once treated its ceiling and then painted it. 声音设计 sets the value.
- `Relay/Footstep` gets water takes under the same `PathGain` and muffle. No gap > 1.2 s; wading is continuous, so it helps.
- Stage 2's acceptance (steps ≥ 3 dB over room tone through one shut door) must be re-measured with pump hum and a long tail.
- Captions: [water sloshes], [splash]; direction tags apply.
- Rule 24 holds: all of this is measured by capture, never played.

**Wet prints.** Bare fact: I found no game where an enemy tracks wet footprints. POOLS' puddle trail is a player breadcrumb [S22], and only on Wikipedia.

- The player leaves prints on dry floor for about 20 steps after leaving water, fading over 60–90 s [SP].
- The Relay leaves them too. It is an honest trace, as a broken door is: it says where it has been, never where it is.
- The Relay does not read them. Reading them would give the hunter a position it did not perceive, and it would be a new sense to teach.
- Option for later: it reads prints only inside its cone, as a glimpse. Not v1.

### 3.5 Interaction

| Thing | v1 rule | Why |
|---|---|---|
| Doors | Never stand in water. Basin only, plus films. No door-water rule at all | Saves the whole family: drag, sills, leaf in water |
| Windows | A viewing window on the hall's border. Water never reaches a sill | The Tall rule gives it for free |
| Electric | No shock. Outlets stay out of wet rooms (the planner already skips door and window faces; add wet walls). Wet-niche lights are lamps in the room set | One way to die. Period code keeps receptacles away from a pool (§2) |
| Lamps | Dip with bursts like any lamp. Underwater lights take part | "Your room's lamps" must stay true |
| Water level | Static | v1. Options in §3.6 |
| EXAMINE | Rules board, log, depth marks read as ordinary close-ups. Reading pauses (A43). Looking at the water does not | |
| Touch | Stick and USE as today. Climb-out on walk-into, no new button. Slow gait is the same stick share | Three-platform parity |

### 3.6 What other games do, and what we looked at

Footage was checked by eye through YouTube's storyboard frames (no media played, rule 24); times are ± 10 s.

| Game | Seen | Rule | Lesson |
|---|---|---|---|
| **POOLS** | iStudioGamer 27:00–28:20: dark green tile band under white, stairs, rails; 52:30–53:50: arches over water, chrome ladder, escalator. Alternate run 13:30–14:50: eye just above black water in tiled corridors, round lamps reflected as long streaks | No enemies. Walking in water is slower (Steam page). Numbers unpublished [S22] | Reflected lamps carry the room. Nothing hunts you, so reviewers call it a walking game |
| **Anemoiapolis** | 43:30–44:50: camera at water level in an inner tube; milky turquoise; a slow ride | Swimming listed by the developer. Entities roam, behaviour unstated [S23] | A low eye over water is enough to sell it |
| **Inside the Backrooms** | 9:00–10:20: flat blue pool, a ladder, then a dive into a red screen and a cut | Water hides the level cut | Cheap water reads cheap next to good tile |
| **Escape the Backrooms** | Poolrooms guide 0:27–0:35: shallow teal water across rooms, tile pillars, bright doorways | Clear pools are fine, dark water is the hazard [S24] | This is the canon look. We avoid it: water stays in a basin |
| **The Last of Us Part II** | Flooded City 15:40–16:30: boat through a flooded street. The mall itself not located | Enemies never swim, they walk to the edge and shoot. Below 2–3 m they start to lose you. No splash hearing. Breath runs out before the exits. Air pockets rejected as resets. Ledge 0.25 m [S20] | The best rule set to learn from, and the reason ours must differ: ours has no gun |
| **Amnesia: The Dark Descent** | Text only: I checked one sheet of the Archives walkthrough and found no water; not verified by eye | The Kaernk lives only in water, is known by its splashes, waits when you step out. Sources disagree about whether crates are safe [S25] | Splashes moving toward you are the scare. An unclear safe spot is a design failure |
| **Thief** | Text: the guide in the 1999 demo | Splashing is noise, water is a risky hiding place, drowning is gradual [S21] | The stealth line of thought |
| **SOMA** | Text | Seabed travel was called plodding [S26] | Slow travel with nothing happening bores people. Keep the wade short |
| **Tainted Pools** | Text: store page | A stalker in pipes hears footsteps, sees light [S27] | Pool plus a hunter is a live idea, so check it before release |

**Recurring rule worth stealing:** water is where you are vulnerable, not where you are safe. **Recurring failure:** water as a place to wait things out.

**Later options** (not v1):
- **Rising damp:** water grows with depth, tied to dead sump pumps (B2 stains, B4 ankle water in low rooms). Strong and honest, but furniture, doors and every room type must then handle water.
- **Pump room valve:** the plant drains or fills the pool. It needs a reason to be used, and B levels have no goal to attach it to yet.
- **Waterflow device:** a broken sprinkler head trips the panel: a real initiating device, and a fourth way to call the Relay.
- **Deep end:** surface swimming with the eye fixed above water. All the Relay questions come back.

### 3.7 Worked numbers

From `RELAY_PURSUIT_REDESIGN.md` §5, §7, §8, §9 and `FrontRooms3DGame.cs` (walk 3.2, run 5.5, climb 0.6 s). A calculation, not an engine run. Script kept in my scratchpad; the formulas are the ones below.

**Getting out from the pool's centre line (4.5 m), plus the climb:**

| k | Wade m/s | Out | Across 9 m | Length 18 m |
|---|---|---:|---:|---:|
| 0.6 | 1.92 | 2.9 s | 4.7 s | 9.4 s |
| **0.5** | **1.60** | **3.4 s** | **5.6 s** | **11.2 s** |
| 0.4 | 1.28 | 4.1 s | 7.0 s | 14.1 s |

**Warning before a hold ends, Relay walking straight at the player from the band's edge on dry floor:** stage 1: 8.4 s (T1), 8.25 s (T5). Stage 2: 3.8 s (T1), 3.6 s (T5).

**Reacting at stage 2 from the centre line:** at k = 0.5 the margin is +0.38 s (T1), +0.22 s (T5). At k = 0.4 it is −0.33 s, −0.48 s. With a 0.6 s human reaction delay k = 0.5 goes to −0.22 s and −0.38 s. A lock-on is not a catch, so that is tight, not fatal. A player who reacts to the lamps always has room. **k = 0.5 is the floor, and it is SP.**

**Both wading, same k, player walks straight (T5, 4.4 m/s):** lock-on at 3 m with C37's 1.7 s hold: caught after 17.9 m of wading. At 6 m: 25.9 m. At 9 m: 28.0 m. The pool is 18 m long and 9 m wide, so in water a player reaches an edge first every time.

**Relay stays on the deck at chase speed, player crosses 9 m to the far edge (6.2 s with the climb):** a deck route under about 22 m gets there first. This is the real danger, and the reason a Relay on the deck beats a player in the water.

**Sprint margin:** same-k keeps the land ratios. At k = 0.85: run 4.7 against chase 3.7. **Why no run in the wade:** nothing in the math needs it. It is a feel rule: you cannot run in chest-deep water. The loss of the sprint is what makes the pool the Run! room's opposite: there you must run, here you cannot.

**Waves** (corrected 10-31 from the technique survey). Two kinds, at two speeds:
- **The ripples you see** (wavelengths of 2–30 cm) spread at **0.23–0.69 m/s**, from the capillary-gravity relation [S61, EST]. Rings from a foot or a drip should grow at about 0.3–0.5 m/s; a fast flat ring reads as a shock wave. A wake from a Relay at the far end takes 30–60 s to cross 18 m, so it does not announce the Relay.
- **The long swell** after a big disturbance moves at sqrt(g h), 3.2–3.7 m/s, and sloshes the basin with a period of about 10.4 s along its length and 5.2 s across it (Merian's formula at a 1.22 m mean depth, EST). A slow, low swell after a plunge is honest and is the one water effect that reaches the far end in seconds.
Both stay inside one room, so neither is a radar.

### 3.8 Picture and performance

**Our install, checked on 10-30** (Unity 6000.6.3f1, URP 17.6.0, rendering path 2 = Forward+, MSAA 4×, opaque texture off, depth texture on; presets Low, Medium, High, Ultra, with High the handheld default and Ultra the desktop default; baked reflection cubes `Refl_Level0`, `Refl_Office`, `Refl_Tall`, `Refl_DeadLamp` in `Resources/Rendering/Reflections`):

- **URP 17.6.0 ships a Screen Space Reflection renderer feature** (`Runtime/RendererFeatures/ScreenSpaceReflectionRendererFeature/`) with Linear and Hierarchical marching, Quarter to Full resolution and a transparent pass, but all of it sits behind `#if URP_SCREEN_SPACE_REFLECTION`, which is not set by the package's asmdef and not in our Player settings. Unity's forum post (29 May 2026, public 2 July) calls it a preview targeted at Unity 6.7 [S30]. Treat it as unsupported.
- **The decal renderer feature exists in the package but is not on our renderer** (only SSAO is).
- COMMON §1 still says 6000.3.10f1. Red's commit of 10-28 moved the project to 6000.6.3f1. The technique survey was written against 6000.3; where the version matters I say so below.
- **Render Graph** is on by default from 6.3, and Unity's staff call Compatibility Mode temporary [S62]. Any new pass for water (ripple field, caustics, wet prints) must be written with `RecordRenderGraph`.

**Physics that sets the look** [S61, S63; EST]:
- Water's F0 is 0.02. From a standing eye (1.62 m above the surface) the water reflects 7 % at 3 m, 16 % at 5 m, 38 % at 10 m: close to the player it is a window onto the tile, far away it is a mirror of the hall. **Wading at the deep end the eye is 0.25 m above the surface**, and the water reflects about 50 % at 2 m and 75 % at 5 m (the survey's figures for a 0.22 m eye). So reflection quality matters most exactly when the player is in the water, and so does the reflection leak of §3.3.
- Clear water absorbs red first. A floor 1.4 m down seen from above (2.8 m there and back) keeps 17 % of 700 nm, 54 % of 600 nm, 85 % of 550 nm, 97 % of 450 nm: a mild cyan shift, not a deep blue. A starting absorption for the shader: **(0.25, 0.057, 0.011) per metre** in RGB. The tile's own colour does most of the work.
- Lamps are near and wide, not a sun. Caustics in an indoor pool are soft and dim. Light thrown up from the water onto walls and ceiling is the more telling effect.

**What to build, by tier.** Gating is by preset and platform keyword, so WebGL stays its own track (COMMON rule 5: looks may degrade, logic may not).

| Part | Ultra / High (desktop) | Medium / Low | iPhone / iPad | WebGL2 | Size |
|---|---|---|---|---|---|
| **Reflection** | Box-projected probe `Refl_Pool` (and a dead-lamp variant, as `Refl_DeadLamp` exists), plus a planar reflection camera at ⅓–½ resolution: a second renderer without SSAO, pool-hall layers only, shadows and fog off, LOD capped, oblique clip, rendered only when the basin is in view | Medium: planar at ¼, half rate. Low: probe only | Probe. Planar at ¼ only as an opt-in after an on-device timing | Probe only (a planar camera doubles draws, and WebGL is bound at about 3,200) | probe S, planar M |
| **Reflection, extra** | Mac only: the Metal RT glass plugin's rays on the water, after the planar baseline exists and after a timing on a pool-sized quad. The glass alone costs 5.4–6.0 ms at 1440p High | none | none (hardware RT only on A17 Pro / M3 and newer [S64]) | none | L |
| **Basin interior** | Analytic: in `FrontRooms/Surface`, below `_WaterY`, refract the view ray at the surface and sample the world-projected tile at the displaced point, then Beer-Lambert by path length. No opaque texture, no depth copy, no extra pass. The opaque texture only for legs in the water, if at all | same | same; never turn on the opaque texture for this (it costs a store and reload on tile GPUs [S65]) | same | M |
| **Ripples** | Ring list in the shader (8–16 rings: feet, drips, the Relay) plus a 512 × 1024 height field later, which also bounces off the basin walls | ring list, 8 | ring list, 4–6 | ring list, 4 | S (rings), M (field) |
| **Caustics** | A tiling flipbook projected in the surface shader under `_WaterY`, soft; later, from the height field | flipbook | flipbook, fewer frames | two scrolled layers of one tile | S |
| **Light from the water** | For the nearest 1–4 lamps, the lamp's reflection off the rippled surface thrown on walls and ceiling | a scrolled pattern scaled by ripple energy | same as Medium | static | S–M |
| **Waterline** | In `FrontRooms/Surface`: a wet band and a thin bright meniscus at `_WaterY` plus ripple height; in the Relay's shader: tint and darken below `_WaterY` | same | same | same | S |
| **Wet floor and prints** | Lagarde-style wetness: grout darkens, gloss rises; puddles from a low-spot mask × noise × a per-room `_WetLevel`. Prints as small instanced quads (≤ 20 per actor, fade 60–90 s): one draw, no decal feature, no render texture | same | same | same | S |
| **Underwater** | Never: camera clamped to at least `_WaterY` + 0.1 m and a near plane ≤ 0.1 m over the basin; a cut and tint if something forces it | same | same | same | S |

**Motion and light safety** (RELAY §9.5, §9.6): caustics, ripples and thrown light are moving repeated patterns. With Reduce Motion on, caustics and thrown light freeze, ring speed halves and the swell is off. Brightness swing of thrown light stays under the photosensitive limits already set for lamp bursts.

**Four spikes, before anything else is built** (each answers one risk):
1. **Planar camera on 6000.6.3f1 with Render Graph on:** `RenderSingleCamera` from `beginCameraRendering` against `SubmitRenderRequest` from `LateUpdate` (one frame late; it threw "recursive rendering" from the camera callback in 2022–23 reports [S66]). Game view, a Mac player and an iPad. 2 h.
2. **Analytic basin** in a copy of `FrontRooms/Surface` on a test box: refraction, absorption, flipbook caustics, the probe on the water layer. 3 h.
3. **Ring list and print quads** with feet events. 2 h.
4. **iPhone and iPad GPU time** for probe only, ring list, planar at ¼. 2 h.

Owners: `FrontRooms/Surface`, the reflection cubes and every surface under `Resources/Surfaces` and `Resources/Rendering` belong to 游戏视觉, so the water look is 游戏视觉's work. The planar camera's quality tiers go through 设置菜单与画质.

---

## 4. Risks

Ordered by how badly each would hurt the build. **P** = how likely, **I** = how much it costs if it happens (H/M/L).

| # | Risk | P | I | What to do | Owner |
|---|---|---|---|---|---|
| 1 | **Nobody finds the pool.** B levels have an unlocked EXIT and no key, and an arrow-follower reaches it in seconds (16.6 s on floor 1). A room off the route goes unseen | H | H | Plan the club as a fixed group on the route, with the EXIT across the water (§1.6, D2). Never left to the room roll | 关卡设计 |
| 2 | **The reflection shows what the rules say is unseen.** A Relay reflected in the water can appear or vanish in view, which RELAY §5, §11 forbid. Worse in the wade, where the water is half mirror (§3.8) | H | M | One more ray from the eye mirrored in the water plane in `PlayerSeesRelay` and Arrive's tests, in rooms with water (§3.3). The same class of problem G14 solved in glass | 追猎 (Relay) |
| 3 | **The map cannot hold a basin as it stands.** Floors are one box per cell and regenerate at one root; a module cannot leave its 8 × 8 chunk; generated rooms stop at 7 cells; revisit shifts move rooms | certain | M | A planned group carved before `Begin` (as `FrontRoomsStartGroup`), aligned to one chunk (the 5 × 8 hall fills a chunk's length exactly, so alignment is not free), its own basin mesh, the stair-shaft cells kept clear, shifts told to leave it alone, the validator told about it | 关卡设计 |
| 4 | **Relay v2 is still behind a define.** Runtime v1 plays until bots pass and the flip is said yes. Water rules written for v2 (stage bands, C37 hold) may land in a game still running v1 | M | M | Write the water law once on the shared speed and hearing values both versions read (`k` on speeds, path cost `1/k`, the slow-wade loudness). Check the v1 path once with a bot run | 追猎 |
| 5 | **Player movement is new in three places:** stepping down 1.37 m, half speed by cell, climbing out with a bigger lift. `TryStartClimb` was built for something else | M | M | Depth class from the cell, not from physics; the plunge is a scripted drop with one sound event; the climb-out reuses the 0.6 s climb with a taller lift. Touch: walk into the edge to climb, as on desktop | 交互设计 |
| 6 | **The camera dips under or the near plane cuts the water.** The eye is only 0.25 m above the surface at the deep end | M | M | Clamp the camera at `_WaterY` + 0.1 m over the basin; near plane ≤ 0.1 m there; a caught or knock-down animation over water ends in a cut | 交互设计, 游戏视觉 |
| 7 | **The planar camera does not work under Render Graph on 6000.6** (API marked obsolete and back; recursion errors from the camera callback) | M | M | Spike 1 (§3.8) first. If it fails, the probe is the baseline everywhere and nothing else depends on the camera | 游戏视觉 |
| 8 | **Frame time on iPhone and iPad.** A transparent pool covers a quarter to two fifths of the screen at the edge; MSAA 4× is already on | M | M | Analytic basin (no opaque texture), probe reflection, ring list only; spike 4 times it on a device before the art is built | 游戏视觉, 设置菜单 |
| 9 | **The sound breaks stage 2.** A long tail and pump hum can mask the Relay's steps; stage 2's acceptance is steps ≥ 3 dB over room tone through one shut door | M | H | Re-measure stage 2 in the Pool zone by capture (rule 24: nothing plays); pump hum ducks or stays low in the bands the steps use | 声音设计 |
| 10 | **Water becomes a safe place.** If any rule lets the player wait things out in the pool, the room breaks the hunt (the failure TLOU II avoided) | L | H | The Relay enters under the same law; no hiding under the coping; the pool is not a sanctuary in the §10 sense, and the validator's sanctuary budget does not count it | 追猎 |
| 11 | **Water becomes a death trap.** Below × 0.5 the stage-2 warning is shorter than the way out (§3.7) | M | H | k = 0.5 is the floor; a bot run that starts mid-pool at stage 2 checks the margin; tune k up, never down | 追猎, 交互设计 |
| 12 | **Drift into the Poolrooms look.** White tile everywhere, arches, water through doors | M | M | §1.7 and §2.5 as a checklist on 游戏视觉's options sheet; tile stops at a line; no arch anywhere | 游戏视觉 |
| 13 | **Flicker and motion.** Caustics, ripples and thrown light are moving repeated patterns | M | M | Reduce Motion path (§3.8); thrown light kept inside the burst limits of RELAY §9.5 | 游戏视觉 |
| 14 | **Anachronism.** A modern drain cover, a pool lift, a sensor tap, a slashed-diver sign | M | L | §2.4 as a check on every kit sheet | 游戏视觉, 平面视觉 |
| 15 | **The history is thin.** I found no named 1980s office tower with a basement pool; Rayburn (1965) is the closest real case (§1.2) | certain | L | The fiction carries it: the building has more than it should. Say so if anyone asks; do not invent a precedent | 叙事设计 |
| 16 | **Mirrors.** A washroom without mirrors is wrong for 1990; a live mirror needs a player body and raises the reflection leak again | M | M | Decision D7 | Red |
| 17 | **Scope.** About 16 new props, 6 new surfaces, a shader family, Foley sets and a map group, against R4's five days, with account B limited until Nov 3 and R2 running beside it | H | M | Phase it (`30_estimate.md`): logic and greybox water first, then the art. Pump room, washroom and gym room after v1 | 系统设计 |
| 18 | **Third-party media.** Film stills and game footage are reference, never assets | L | H | Nothing downloaded without Red's OK (rule 7); `media_candidates.md` lists what Red may approve; no game asset is copied | all |
