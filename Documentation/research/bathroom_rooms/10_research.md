# Bathroom rooms · 10 · Research: the place, the 1990 fittings, the water

Date: 2026-10-30 to 10-31. Line: 浴室房间调研 (`local_fe9beaf3`, handoff `Takeover_2026-10-18/handoff/bathroom.md`). System L8, decision A48, schedule R3.

Red, 2026-10-30 23:30, 系统设计 chat: "把浴室房间也加上日程，浴室房间需要先做research，然后这个research也包括水体逻辑（因为是泳池相关的环境）并且思考在游戏中当时年代的泳池陈设有什么".

**Status: research and proposal. Nothing is built.** No game code, no Unity run, no git command, no download. Red's picks are in `20_for_red.md`; the work estimate is in `30_estimate.md`; every source is in `SOURCES.md`.

**Marks.** **SP** = a starting value for playtest. **EST** = my estimate, not measured. **[S12]** = a row in `SOURCES.md`. Code references are to main on 2026-10-30.

---

## 0. Short answer

1. **What it is.** A tenant fitness center on a B level of the office building: locker rooms, gang showers, a 60 ft lap pool. Real office buildings did this, and they put it at the bottom, because 1.4 m of water weighs about five and a half times what an office floor is designed to carry. The Rayburn House Office Building opened in 1965 with its pool in the sub-basement, under the offices. So the game's B levels are where this room belongs. It is the opposite of the Poolrooms: every object has a job, a code and a sign.
2. **What stood in it.** See §2: tile, depth marks, gutter, ladders, guard chair, pace clock, lockers, showers, fixtures, lamps, plant, each with its dating and a reuse or new-kit call.
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

*Being completed from the period survey; see §2 notes in `SOURCES.md`.*

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
- A new Zone, Pool, with a long tail. Natatoriums are very live rooms; [agent numbers pending].
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

**Waves:** shallow-water speed sqrt(g h): 3.2–3.7 m/s at 1.07–1.37 m, so a wake crosses 18 m in about 5 s. A Relay wading in the same pool shows on the surface about as fast as you would hear it. Physically honest, and it stays inside one room, so it is not a radar.

### 3.8 Picture and performance

*Being completed from the technique survey. Verified so far on our own install (Unity 6000.6.3f1, URP 17.6.0, rendering path 2 = Forward+, MSAA 4×, opaque texture off, depth texture on):*

- **URP 17.6.0 ships a Screen Space Reflection renderer feature** (`Runtime/RendererFeatures/ScreenSpaceReflectionRendererFeature/`) with Linear and Hierarchical marching, Quarter to Full resolution and a transparent pass, but all of it sits behind `#if URP_SCREEN_SPACE_REFLECTION`, which is not set by the package's asmdef and not in our Player settings. Unity's forum post (29 May 2026, public 2 July) calls it a preview targeted at Unity 6.7 [S30]. Treat it as unsupported.
- **The decal renderer feature exists in the package but is not on our renderer** (only SSAO is).
- COMMON §1 still says 6000.3.10f1. Red's commit of 10-28 moved the project to 6000.6.3f1.

---

## 4. Risks

*Being completed with the survey notes.*
