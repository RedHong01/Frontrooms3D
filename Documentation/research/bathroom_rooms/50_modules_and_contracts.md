# Bathroom rooms · 50 · Room modules as data, and contract diffs for 关卡设计

Date: 2026-10-31. Line: 浴室房间调研, K session (Red's API key, Cursor, `cursor_K_bathroom_1031`). Stage 3 of the kickoff ("room modules as data, plus contract diffs for 关卡设计").

Decided by Red on 10-31 (handoff §6): D1 all (the club on B2 leading into a pool level), D2–D12 按推荐, palette F1, structural moves M1–M7, a fixed dark zone, water Q1–Q4 (wrong-turquoise in the level, P4c and ray-traced water in v1, the dark zone's grade follows the niche lamps). Measured against `Documentation/LEVEL_MODULE_SPEC.md` (§2 grid, §3 architecture, §4 ceiling, §9 modules) and `30_estimate.md` §2 (P1 map, P2 player, P3 Relay). Machine-readable catalogue: `pool_modules.json` (same folder).

**This file proposes; 关卡设计 decides.** Map files, `ModuleUnits`, the generator and the Relay are 关卡设计's; the player's water classes are 交互设计's; looks, kit and shaders are 游戏视觉's (card bathroom-109).

## 0. Short answer

1. **Fifteen modules** in two new themes, all inside §2's grid and §9's limits (≤ 8 × 8 cells, never across a chunk, ceilings 2.4 / 2.9 / 5.4 m): three club modules (entry, lockers with gang shower, the 5 × 8 Tall pool hall that fills one chunk) and twelve pool-level modules carrying the seven structural moves (§1).
2. **Placement copies the floor stations (§9):** the club is a planned group on B2 (`poolClub`, weight 0, never random); past its far door the zone theme turns to `PoolLevel` and the generator carves from the pool-level module pool; the dark zone is a planned sub-zone at that zone's far end (`poolDarkZone`).
3. **Twelve contract diffs** (C1–C12, §3). The new data are per-cell water class and floor offset, per-module water height, a round-headed arch edge, weir edges, an observation-window edge, Pool-zone column and ceiling rules, and the planned placements. The Relay and the player read the water class.
4. **Red, 10-31 ~15:36: the dark zone is truly one level down in v1.** The fire stair (M3) is walkable: its flight runs down into the water to a sunken basement (floor about -2.52 m, water surface -1.5 m, so 1.02 m deep, within D6), and the dark zone lies there. This needs a sunken region in the map and the Relay pathing across height (C9, revised); about +22–36 PAH (`30_estimate.md` §5).

## 1. Module catalogue

Units as §1 of the spec; cells 3 m; "Ankle" = 0.12 m of water over a floor at -0.15 (surface -0.03); "Wade" = basin cells 1.07 to 1.37 m deep; "Wet" = dry walking on wet tile.

| Id | Footprint (cells) | Ceiling | Theme | Water | Edges | Move | Key contents (kit names are proposals for 游戏视觉) |
|---|---|---|---|---|---|---|---|
| PC_Entry | 2 × 2 | Standard 2.9 | PoolClub | Dry | S: club entrance from the B2 corridor (door or arch by §3); N: arch to PC_Lockers | M6 | towel desk (`Kit_TowelDesk`), sign blanks (平面视觉), troffers per cell |
| PC_Lockers | 2 × 3 | Standard 2.9 | PoolClub | Wet (shower cell Wet + drain) | S: arch from entry; N: arch onto the hall deck | M5 M6 | locker banks, benches, gang shower in the NE cell behind a glass-block half wall, floor drain; sealed strips + vapour-tight globes |
| PC_Hall | 5 × 8 (one chunk) | Tall 5.4 | PoolClub | basin 3 × 6 Wade; deck Wet | S: arch from lockers (SW cell); N: round-headed arch, the far door | M1 M4 M5 M7 | basin with gutter, coping, ladders, guard chair, ring buoy, rules board and depth marks (平面视觉), a beach entry at the shallow end, an observation window in the east basin wall, a suspended-ceiling soffit over the south deck; pool rig P1/P2 (Red's pick); no columns |
| PL_Turn | 2 × 2 | Standard 2.9 | PoolLevel | Ankle | S, N: arches (rect or round-headed) | M5 | tile climbs to the ceiling, one sign blank left behind, valance light |
| PL_Chain | 2 × 2 | Standard 2.9 | PoolLevel | Ankle | 2–3 round-headed arches | M5 M6 | the repeated room; waterline course; valance or flush lenses |
| PL_OfficeCeiling | 6 × 6 | Standard 2.9 | PoolLevel | Ankle; one side BeachEntry | arches on 2–4 sides | M1 M2 M7 | the Level 0 ceiling grid with troffers per cell, 1 in 3 dead, 4–6 missing tiles as holes; Custom columns halving from 6 m to 3 m |
| PL_PillarHall | 6 × 6 | Low 2.4 | PoolLevel | Ankle | arches on 2 sides | M2 | a column on every 3 m corner; flush lenses on irregular cells; one third unlit |
| PL_BasinRoom | 4 × 4 | Tall 5.4 | PoolLevel | basin 2 × 2 Wade; deck Ankle | arches on 2–3 sides | M7 | ladder, an empty guard chair facing a wall, depth marks that lie; flush lenses; wet-niche lamps |
| PL_FireStair | 1 × 4 | Standard 2.9 (stair cells open below) | PoolLevel | top Dry; lower 2 cells Wade | S: fire door (the tower's); N: gate below the waterline | M3 | concrete flight, pipe rail, EXIT sign over the drowned gate (平面视觉), tile below the waterline, stair block above |
| PL_Gallery | 1 × 5 | Low 2.4 | PoolLevel | Dry | ends: arches; one long side: ObsWindow edges | M4 | square windows and round portholes into a lit basin, pipes along the ceiling, no overhead light |
| PL_PoMo | 4 × 4 | Tall 5.4 | PoolLevel | Ankle | N: wide round-headed arch with keystone and pilasters; others arches | M5 | glass-block wall lit from behind, an oculus, 1980s laminate trims |
| PL_Leisure | 4 × 5 | Tall 5.4 | PoolLevel | upper 2 rows Ankle; Weir; lower 3 rows Wade | arches | M7 | the weir with a falling sheet, recessed steps, a slide tube entering through the north wall over a catch pool |
| PL_Court | 2 × 4 | Tall 5.4 | PoolLevel | Ankle to Wade | one short side: clear glass back wall onto a viewing cell | M6 | the racquetball court turned aerobics room, red court lines under water; no mirrors until D7 |
| PL_Weird | 2 × 3 | Standard 2.9 | PoolLevel | Ankle | arches | M6 | the club sequence misassembled: a locker bank in water, a shower row on an open wall, a guard chair facing a corner |
| PL_Dark* | as PL_Chain / PL_OfficeCeiling / PL_BasinRoom | as source | PoolDark | Ankle / Wade | as source | M1 M3 | the same modules with every overhead dead, wet-niche lamps and glowing windows only; grade "night" (16, Q4) |

## 2. Placement and the route

- **B2 only** (D2 A, adjusted for D1 c). The level profile names the club group (`poolClub`: PC_Entry → PC_Lockers → PC_Hall), placed once on B2 like `stationWaiting` / `stationDesk` (weight 0, never in the pool, never turned at random). Its entrance faces the B2 route.
- **Past the far door the zone theme is `PoolLevel`:** generated rooms in that zone pick only PL_* modules (weights in `pool_modules.json`); every cell is at least Ankle, openings are arches, and the map's own troffers are off inside pool rigs (zone_lamps `10_pool.md` §0.8).
- **The EXIT moves into the Pool zone** (D2 adjusted): never in the club, never in the dark zone; placed by the existing EXIT rule inside the zone.
- **The dark zone** (`poolDarkZone`) is a planned sunken sub-zone one level down (Red, v1): 3–6 PL_Dark* rooms on a floor offset of about -2.52 m, reached by walking down PL_FireStair at the Pool zone's far end (C9).
- **Variety without repetition fatigue:** PL_Chain repeats on purpose (the canon's repetition); the rarer modules (PL_Court, PL_Leisure, PL_PoMo) each appear at most once per run.

## 3. Contract diffs (for 关卡设计; numbered for its replies)

| # | Spec section | Change | Why | Cost note |
|---|---|---|---|---|
| C1 | §9, §2 | Zone/module themes `PoolClub`, `PoolLevel`, `PoolDark` (beside Level 0 and Office); each theme's module pool and look hooks | the pool family is a zone, not a room | enum + profile |
| C2 | §9 (new data) | Per-cell `water` {Dry, Wet, Ankle, Wade} and `floorY`; per-module `waterY`; `MapWorld.WaterAt(cell)` for the player (P2) and the Relay (P3) | 30 P1–P3 | the core of P1 |
| C3 | §3, §6 | Basin cells: no per-cell floor, basin + deck + gutter meshes, sunken -1.07 to -1.37 | P1 as estimated | as estimated |
| C4 | §3 (new) | BeachEntry cells (floor sloping +0.05 → -0.15 across a cell) and Weir edges between two water heights (M7) | leisure-pool geography | new; a weir edge is a step down for the player and the Relay |
| C5 | §3 (new piece) | Round-headed arch edge: width 1.4–2.4 m, crown ≤ ceiling − 0.3, springing = crown − width/2; trim hooks (keystone, pilasters) for 游戏视觉; allowed on PoolClub/PoolLevel edges | M5 (postmodern 1980s), Red's pick | like §9's `archWidth` records |
| C6 | §3 columns | Pool-zone column rule: Custom grids that subdivide 6 m → 3 m toward a room's interior (PL_OfficeCeiling, PL_PillarHall); faces stay ≥ 2.4 m apart and ≥ 1.06 m from crossing lines | M2 | rule + validator |
| C7 | §4 | Pool-zone ceilings per module: suspended 0.6 grid with troffers (M1: 1 in 3 dead, missing tiles as holes), tiled ceiling, or the pool rig; map lamps Off inside a rig | M1, zone_lamps §0.8 | profile + lamp mode |
| C8 | §9 placement | `poolClub` (planned group on B2) and `poolDarkZone` (planned sub-zone); EXIT rule inside the Pool zone | §2 above | like floor stations |
| C9 | §3 (new), §7, §8 | **Revised by Red (v1):** a sunken region: the dark zone's cells sit on a floor offset of about -2.52 m with a water surface at -1.5 m (1.02 m, wading); PL_FireStair's cells step down (0.18 × 0.28 m risers) from base level into it, the gate open or broken; chunks build both heights; the Relay paths across the stair (stair cells as links), sight and hearing across the stairwell; the EXIT stays on base level | M3, Red's dark zone | about +22–36 PAH: P1 +8–14, P3 +10–16, P2 +2–4, validators +2 |
| C10 | §3 (new edge) | ObsWindow edge on gallery | basin edges: solid for collision, render-only glass with an underwater view (游戏视觉's shader); decide whether sight passes (recommend: blocked unless the basin behind is a real room) | M4 | small for the map |
| C11 | §7 Relay | Speed factor k per water class, path cost 1/k, Arrive never on water, no door leaves on Ankle/Wade edges, the mirrored ray for reflections (10 §3.3), hearing values (D4: slow wade 6 m, ankle 3 m, plunge 18 m) | D3, D4, 30 P3 | as estimated |
| C12 | §9 checks | Validators: every Pool-zone room reachable by wading (D6); the pool is not a sanctuary (10 §3.6); the EXIT reachable without the dark zone; no door leaf in water | rules | small |

## 4. What 交互设计 needs (P2, from C2)

Wet, Ankle and Wade classes from `WaterAt`: speed × 0.85 in Ankle, × 0.5 in Wade, no run in Wade; the plunge as a scripted drop with one event; the climb-out with a taller lift in 1.0 s (Red, 2026-11-01: "1.0 秒（推荐）"; the window climb stays 0.6 s); the camera clamp over a basin (near plane ≤ 0.1 m); BeachEntry and Weir as walkable slopes and steps (C4); touch parity (walk into the edge). Unchanged from `30_estimate.md` P2 except C4.

## 5. Open for 关卡设计

- Whether the Pool zone is all of B2 past the club, or a bounded cluster of chunks (recommendation: a bounded cluster, 3–5 chunks, so B2 keeps its EXIT distance budget).
- Whether PL_Court's glass wall blocks the Relay's sight (sensory honesty: it should not, so the court is a real room behind clear glass).
- C9: Red wants the dark zone physically below in v1 (10-31 ~15:36). Open for 关卡设计: how the chunk stream builds the second height, and whether the sunken region may span more than one chunk.
