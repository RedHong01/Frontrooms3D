# Level space · 60 · Larger stairs: helical and circular, in numbers

Date: 2026-10-31. Line: 浴室房间调研 (`bathroom`), K session (Red's API key, Cursor), sub-session of `cursor_K_bathroom_1031`.

Red, 10-31 ~16:27, on the round-3 sheets: "我认为螺旋楼梯也许可以更大一些" (the spiral stairs could be bigger).

Builds on, does not repeat:
- `50_refinement.md` §1: the three sizes B4-S, B4-M and B4-L;
- `40_terrain_grammar.md` §5.1–5.3: the vertical module and splice rules R1–R8;
- `20_structural_drama.md` §6: how the greybox stills are made.

Red's decisions kept here:
- No spiral, helical or circular stair is ever used by the Relay (the hunter).
- Every raised area reached by one keeps a second route (R6).
- Darkness is never cover.
- No creator's composition is copied (*Dream Pools*, POOLS, Kane Parsons, the 2026 films). The forms below come from dated real buildings and period code text.

Research and greybox only. Stills are rendered in clone `W/proj_levelspace`; nothing changed in main. New sources are [LS61]–[LS67] (§10); the parent session merges them into `SOURCES.md`.

## 0. Short answer

1. **Real precedents exist at exactly this scale** (§1):
   - P1, Jacobsen's SAS Royal Hotel lobby (1960): 32 risers of 0.15 m, 1.8 m wide round a 2.4 m eye, carpeted, a glass guard.
   - P2, Harrison's Metropolitan Opera lobby (1966): curved concrete stairs in terrazzo and red carpet with a marble border.
   - P3, Niemeyer's Palácio Itamaraty (1970): a concrete helix with no guard at all.
   - P4, Pei's Louvre (1989): a steel helix with stone treads and a glass guard, carrying visitors down into a hall under glass.
2. **B4-M, helical round an open well** (§2): well 1.6 m, clear width 1.3 m, 0.08 m solid ribbons, 4.36 m outside, a 2 × 2 cell module.
   - Riser about 0.17 m; going 0.28 m on a walk line 0.30 m in from the well.
   - It sweeps 219° to a +2.7 gallery, 248° to the +3.0 pool gallery, and 372° over a full 4.0 m storey with the 12 ft mid landing.
3. **B4-L, a circular stair to the egress rule:** inner radius 2.95 m (more than twice its 1.45 m width), 0.10 m ribbons, exactly 9.0 m outside, a 3 × 3 cell module.
   - Riser about 0.15 m; going 0.33 m on the walk line, 0.30 m at the narrow end (the 1976 code asks for 10 in, 0.254 m).
   - It sweeps 168° over one 4.0 m storey and 220° over a 5.4 m Tall room, each with one mid landing.
4. **Two numbers in `50` §1.2 are corrected** (§2.6): "300° per storey" fits a 3.6 m rise, and "180–220°" fits a 5.4 m rise. At +2.7 a B4-L is only a 99° curved flight, so B4-L is not used for galleries.
5. **Splice (R2):** the last riser always sits on a grid radius, so the top landing runs straight to a cell line and meets the upper floor flat. The ribbon continues as the gallery's parapet.
6. **Finish per level** (§3):
   - Level 0: carpet and a plaster ribbon with an oak cap;
   - Office: carpet, a steel band and a glass ribbon, so sight lines survive;
   - pool: porcelain white tile on treads, risers and ribbon, a dark nosing on every step, stainless rails;
   - lobby: terrazzo (not rendered: no terrazzo surface yet).
7. **Light** (§4): step lights in the inner ribbon every third tread, and one light from the well per stair (pendant, downlight, floor uplight, or a laylight over B4-L). The walkable 1.6 m well is never dark.
8. **Placement** (§5):
   - B4-M: the pool gallery (and the club lobby);
   - B4-L: the pool hall's arrival and, on one floor of the pool level, winding round the well itself;
   - Red, 10-31 17:29: no large spiral stair in Level 0, the Office or Run!, so the split suite and the file mezzanine keep their 1-cell B4-S (`40` §6.2) and the station lobby keeps its B5 flared stair.

   Every room keeps a second route for the Relay.
9. **Map data** (§6): three connector types, `SpiralS` 1 × 1, `HelicalM` 2 × 2 and `CircularL` 3 × 3. None of them is ever in the Relay's graph, and the generator checks R6, the mid landing, headroom and lit landings.
10. **Stills** (§7): 13 greybox stills in the game's materials, 10 of them on sheet `W/level_space/sheets/LS10_large_stairs.jpg`. The pool scenes were re-rendered in porcelain white with a dark nosing on every step (Red, 17:19 and 17:24). Plain pale tile had made the steps vanish from the top; with the nosing they read.

## 1. (a) Precedents, 1960–1989

Four dated feature stairs in public interiors, read as text. No image of them is used as a model; the numbers and materials are what FrontRooms takes.

| # | Precedent | Date | Form and size | Finish | Source |
|---|---|---|---|---|---|
| P1 | Arne Jacobsen, SAS Royal Hotel lobby, Copenhagen | opened 1960 | A split-level lobby: two levels 4.88 m apart. A helical stair round an open eye, hung from the upper slab by 16 steel cables (10 on the outer edge every 30°, 6 round the eye every 60°). Two hidden floor supports under the second step stop vibration. 32 steps in seven-eighths of a turn (315°). Width 1.8 m, eye 2.4 m across, so about 6.0 m outside. A welded 20 mm steel plate folded 32 times. | Blue carpet on the treads. Guard 0.90 m: a dark grey rail, white posts every 60°, darkened glass following the helix. At the top the guard runs on round the circular opening. | [LS61] |
| P2 | Wallace K. Harrison, Metropolitan Opera House lobby, Lincoln Center, New York | opened 16 Sept 1966 | A "curvilinear double staircase" in a lobby the full width of the building, from street and subway level to the Grand Tier. Poured-in-place prestressed concrete; boat builders made the formwork. Balconies overlook it. | Terrazzo; the steps covered in deep red woven carpet with an Italian marble border; eight crystal chandeliers hung in the void. | [LS62] |
| P3 | Oscar Niemeyer (building; structure by Joaquim Cardoso), Palácio Itamaraty, Brasília; the stair credited to Milton Ramos | inaugurated 1970; described in April 1969 | A poured-concrete helical stair in the column-free reception hall, with no balustrade and no handrail. A 1969 newspaper calls it "a escada estilo helicoidal, que funciona como âncora de laje" (the helical-style stair that anchors the slab). | Concrete; deep blue carpet on the treads. | [LS63] |
| P4 | I. M. Pei, Louvre Pyramid and Hall Napoléon, Paris | built 1985–89, inaugurated March 1989 | A monumental helical stair from the courtyard level down into the reception hall under the glass pyramid. A box of welded steel plates, supported only at its first and last steps. A lifting platform (the "piston" lift) rises in its centre, so this one has a core, not an open well. Diameter not found (to verify). | Off-white stone treads on a smooth metal helix. The guard is curved laminated glass (two bent 12 mm extra-white sheets) carrying the handrail, with no balusters. | [LS64] |

What FrontRooms takes from them:
- **Riser and eye (P1):** 4.88 m in 32 risers is 0.152 m a riser. The eye is 2.4 m and the width 1.8 m. On a line 0.30 m in from the eye, the going works out at about 0.26 m (our arithmetic). That is the B4-M to B4-L range below, built for real in a hotel lobby of 1960.
- **The first step faces the arrival (P1):** the stair "is positioned with the ideal angle to place the first step in the direction of the access door". FrontRooms places the first riser facing the room's main approach, and the last riser on a grid radius (§2.4).
- **The guard continues round the opening above (P1):** in FrontRooms the ribbon runs on as the gallery's parapet.
- **Solid stair, soft finish (P1, P2, P3):** in all three, concrete or steel is carpeted, and P2 adds terrazzo with a stone border. Level 0 and the lobby take this.
- **The ribbon (P3):** the stringer and the soffit read as one continuous concrete band. That is the "solid curved balustrade" of `50` §1.2.
  - P3's missing guard is the one thing FrontRooms cannot take: voids are guarded (`20` §5 rule 3), and the period code asks for a 42 in guard. In FrontRooms the ribbon is the guard.
- **The arrival under light from above (P4):** a stair carrying you down into a hall under a glazed roof. Daylight falls past it, and the treads are stone. B4-L in the pool hall takes this, with a laylight instead of a pyramid.
- **Light hung in the void (P2):** the chandeliers. In the pool gallery, a light can hang down the well of a B4-M (§4); the Level 0 pendant went with the Level 0 B4-M (Red 17:29).

Period code text for the US setting [LS65], [LS66] (Uniform Building Code 1976, §3305 and §3312, and the same wording in California's code):
- Stairs serving more than 50 people are at least 44 in (1.12 m) wide; risers at most 7.5 in (0.19 m), runs at least 10 in (0.25 m).
- **Circular stairs** "may be used as an exit provided the minimum width of run is not less than 10 inches and the smaller radius is not less than twice the width of the stairway".
- **Spiral stairs** (round a column) only in dwellings, serving at most 400 sq ft: 26 in clear, a 7.5 in run at 12 in from the narrow end, risers up to 9.5 in, 6 ft 6 in headroom.
- **Winders** are measured "at a point not more than 12 inches from the side … where the treads are the narrower". FrontRooms uses this 12 in line as its walk line; so does the current code for circular stairs [LS67].
- At most 12 ft (3.66 m) vertically between landings; a landing at least as long as the stair is wide.
- Handrails on both sides, 30–34 in (0.76–0.86 m) above the nosing.
- Headroom 6 ft 6 in (1.98 m), measured vertically from the line of the nosings.
- Exits lit at not less than one footcandle (about 11 lux) at floor level whenever the building is occupied.

## 2. (b) Geometry in numbers

### 2.1 Conventions

- **Walk line:** 0.30 m (12 in) in from the narrow, inner edge. This is the line UBC 1976 uses for winders and spirals [LS65], and the current code uses for circular stairs [LS67]. The going g is measured on it.
- **Angle per tread:** δ = g / r_w (in radians), with r_w = r_in + 0.30 m.
- **Sweep:** from the first riser to the last riser. The top landing starts at the last riser.
  - With n risers there are n − 1 treads. A mid landing replaces one tread.
  - So sweep = (n − 1 − L)·δ + L·λ, where L = 0 or 1 landings and λ is the landing's angle.
- **Landings:** each is at least as long as the stair is wide, measured on the centre line (UBC §3305(g)). One mid landing whenever the rise is over 3.66 m (12 ft, §3305(i)).
- **Headroom:** at least 2.0 m (UBC: 1.98 m), measured vertically from the nosing line to any soffit above.
- **Guards and handrails:** guards 1.07 m (42 in) above the nosing line on both edges; handrails at 0.86 m (34 in) on both sides.
- **Storey:** one map storey is 4.0 m (`LsFF`). A gallery or mezzanine in a Tall room is +2.7 m; the pool gallery in `P` is +3.0 m.

### 2.2 The two stairs

| | B4-M · helical round an open well | B4-L · circular stair to the egress rule |
|---|---|---|
| Inner radius r_in | 0.80 m (well 1.60 m across) | 2.95 m (≥ 2 × width, UBC §3305(e)) |
| Clear width | 1.30 m | 1.45 m (≥ 44 in) |
| Outer radius r_out | 2.10 m | 4.40 m |
| Ribbons (inner and outer) | 0.08 m | 0.10 m |
| Outside diameter | 4.36 m | 9.00 m |
| Module | 2 × 2 cells (6 × 6 m), 0.82 m of floor round the stair inside the module | 3 × 3 cells (9 × 9 m); the outer ribbon touches the cell lines at four points |
| Riser | 0.167–0.169 m | 0.148–0.150 m |
| Going: inner edge / walk line / centre line / outer edge | 0.20 / **0.28** / 0.37 / 0.54 m | 0.30 / **0.33** / 0.37 / 0.45 m |
| 2R + g on the walk line | 0.61–0.62 m | 0.63 m |
| Angle per tread δ | 14.58° | 5.82° |
| Mid landing (when rise > 3.66 m) | 1.30 m on the centre line = 51.4° | 1.45 m on the centre line = 22.6° |
| Centre post | none: the well is open | none: the well is open, 5.9 m across |
| Code status | a feature stair, not an exit (helical, BS 5395-2 [LS55]) | meets UBC §3305(e) as an exit stair; even so, FrontRooms never makes it the EXIT and the Relay never uses it |

### 2.3 Rise, risers and sweep

| Use | Rise | B4-M | B4-L |
|---|---|---|---|
| Gallery or mezzanine in a Tall room (S4, T2) | 2.7 m | 16 risers of 0.169; 15 treads; **219°** | 18 risers of 0.150; 99°: only a curved flight, not a B4-L use |
| Pool gallery (`P`) | 3.0 m | 18 of 0.167; 17 treads; **248°** | — |
| One map storey (well floor, pool hall arrival) | 4.0 m | 24 of 0.167, landing after 12: 22 treads + 51°, **372°** (335° without the landing) | 27 of 0.148, landing after 13: 25 treads + 23°, **168°** |
| A Tall room's full height | 5.4 m | not used (more than one turn and a half) | 36 of 0.150, landing after 18: 34 treads + 23°, **220°** |

### 2.4 Headroom

- **B4-M below 3.6 m and every B4-L:** these sweep less than one turn, so the stair never passes under itself.
- **B4-M over a full 4.0 m storey (372°):** it passes over its own first 12° at a nosing height of 3.86 m. The clear height there is 3.2 m.
- **Under a gallery:** a +2.7 gallery with a 0.2 m slab has its soffit at 2.5 m. Only treads up to 0.5 m high (the first 3) could pass under it. So B4-M stands beside the gallery, in its own cells, and its landing projects from the gallery edge.
- **Under a full floor:** for a 4.0 m storey, the upper floor and the lower room's 2.9 m ceiling are both open over the whole module, except the top landing. At the upper level the module is a guarded void.

### 2.5 Landings on the 3 m grid and the splice (R2)

- **The connector owns its cells** (2 × 2 or 3 × 3). The first riser stands inside the module, facing the room's main approach (P1). The approach runs on the module's own floor: the 0.82 m border of a B4-M, the well or the free sector of a B4-L.
- **The last riser sits on a cardinal radius** (0°, 90°, 180° or 270°). The top landing then runs straight along the tangent, as wide as the stair (1.30 or 1.45 m). It meets the upper floor flat on a cell line (R2).
  - B4-M: the landing runs 3.0 m from the stair's axis to the module's edge (scenes W, X). Where a wall is near, it widens to the wall.
  - B4-L: the last riser at a tangent point already lies on the module's edge. The landing runs along that edge as a bump-out of the upper floor: 4.5 × 1.55 m in scene Y. Or it runs 4.5 m straight to the next cell line, as in scene V.
- **Guards:** the ribbon continues as a straight solid parapet along every open side of the landing, and on along the gallery edge (P1).
- **Ports** (`40` §5.2): the joint "floor → mezzanine 2.70" now chooses among three connectors by rise, room height and footprint. B4-S takes 1 cell, B4-M 4 cells in a Tall room, B4-L 9 cells in a Tall room or the well.
- **Asymmetry (R4):** the first and last risers are 168–372° apart, so the approach and the exit face different sides of the module. The module sits off the room's centre lines.

### 2.6 Corrections to `50` §1.2

- **B4-M, "about 300° per storey (17 risers)":** with a 0.28 m going on a walk line 0.30 m inside a 1.6 m well, 300° needs about 21–22 risers, a rise of about 3.6 m. The +2.7 gallery takes 219°, the +3.0 pool gallery 248°, and a 4.0 m storey 335° (372° with the 12 ft landing).
- **B4-L, "180–220° per storey":** this holds for a 5.4 m rise (220°). One 4.0 m storey takes 168°. At +2.7 a B4-L sweeps only 99°: a curved flight, so it is not used there.
- **B4-L, "width 1.5 m, inner radius 3.0 m":** with 0.10 m ribbons this gives 9.2 m outside, and the ribbon crosses the 3 × 3 module's cell lines by 0.10 m. Width 1.45 m on a 2.95 m inner radius keeps the code ratio and inscribes the stair in the module exactly (R5). Red can choose (§8).

## 3. (c) Materials per level

| Level and rooms | Treads and risers | Body (soffit, stringer) | Guard | Cap and handrails | Precedent |
|---|---|---|---|---|---|
| Level 0: split suite, the lineup room | L0 carpet, wrapped over the nosings | painted plaster (`Wall_Primer`) | **solid ribbon** in painted plaster, 0.08 m | oak cap; oak handrails both sides | carpet on a solid stair (P1, P2, P3) |
| Office: file mezzanine | office carpet | painted putty steel: soffit plate and a stringer band to 0.10 m above the nosings | **glass ribbon** (clear laminated) on the steel band | steel cap; steel handrails | glass guards in P1 (darkened) and P4 (curved, no balusters) |
| Pool: gallery and hall arrival | porcelain white tile treads and risers, slip-resistant; a dark nosing strip on every tread and landing edge, 50 mm on the tread and 25 mm down the riser (rendered) | porcelain white tile | **solid ribbon in porcelain white tile**, 0.08 m (M) or 0.10 m (L) | stainless cap and stainless tube handrails | P3's continuous concrete ribbon; the period code asks for slip-resistant treads [LS66] |
| Lobby: station band (§5) | terrazzo or honed stone with a contrasting nosing strip | concrete with a terrazzo finish | solid ribbon clad in terrazzo or stone | brass or stainless cap | P2 (terrazzo, a marble border to the carpet), P4 (stone treads) |

**Ribbon, glass or balusters:**
- **Balusters on a post (B4-S, today's spiral)** read as equipment: light, see-through, the steel spiral of a plant room or a slide tower. They stay on B4-S.
- **The solid ribbon** (B4-M, B4-L) runs as one surface from the soffit to the cap, so the helix reads as one form. With the open well and the width, this is what makes the stair read as larger (`50` §1.2). It also carries the step lights (§4).
  - The ribbon is 1.07 m high. A standing player's head and shoulders stay above it (eye 1.62 m), and the Relay (2.05 m) always shows above it.
  - So the stair gives no full cover. This matches the panel studies (`20` §6: at 1.35 m the head and shoulders show).
- **The glass ribbon** is the Office's version, where sight lines are the level's structure (`10` Fig. 2). Sight crosses the stair, bodies do not (R7).

**Pool tile colour (Red, 10-31 17:19 and 17:24):**
- **17:19:** "我希望底色不应该是偏暖的米白，而是和dream pool更相似的瓷白" (the base colour should not be a warm off-white but a porcelain white). In the clone, `Prop_CeramicGlaze` (the harness's `mTile`) is now `_BaseColor` (0.93, 0.95, 0.96), changed from (0.86, 0.85, 0.80); smoothness stays 0.85.
- **17:24, answering §8 question 8:** porcelain risers. The pool stairs are porcelain white on both treads and risers, and only a dark nosing strip marks each step: no teal band, no second tile colour.
- **What that means in the scenes:** every tile surface in scenes X and Y (walls, deck, basin, treads, risers, ribbons, landings, flights) uses `mTile`. No warm tile colour is added.
- **The nosing strip:** 50 mm deep on the tread at its front edge, 25 mm down the riser.
  - Colour: a deep blue-grey, cool, nothing warm: the clone's existing `Station_CrashRail`, `_BaseColor` (0.24, 0.27, 0.31).
  - It runs on every tread and landing edge of B4-M and B4-L, and on every step of the straight pool flights (the Relay's routes).

No terrazzo surface exists in `Assets/Resources/Surfaces` yet. The lobby finish is not rendered; it would need a `Stair_Terrazzo` surface and a brass cap from 游戏视觉.

## 4. (d) Light

What the period did:
- P2 hangs eight chandeliers in the void beside its stair.
- At P4, daylight from the glass roof falls past the stair into the hall.
- The 1976 code asks for at least one footcandle (about 11 lux) at floor level on every exit [LS65].

FrontRooms rules: R8 puts light on the joints (lit landings, lit stair noses), and darkness is never cover. A B4-M's well is a 1.6 m pocket a player can stand in, so it is always lit.

Three devices, all built in the greybox scenes:

1. **Lamps on the inner edge: step lights in the inner ribbon.**
   - A small lens (0.22 × 0.07 m) 0.26 m above every third tread, aimed down at the tread.
   - It marks the noses, and at a distance it draws the helix as a rising line of points.
   - Warm white in Level 0, cool in the Office and the pool.
2. **Light from the well**, one per level:
   - Level 0: withdrawn with the Level 0 B4-M (Red 17:29); it was an opal drum pendant hung down the well's axis.
   - Office: a narrow downlight through the eye.
   - Pool B4-M: an uplight set in the floor of the eye, washing the soffit.
   - B4-L: a 6 × 6 m laylight over its 5.9 m well, plus six floor uplights grazing the inner ribbon.
3. **The room's own lamps:**
   - the troffer grid in Level 0 and the Office; hall spots every 6 m in the pool;
   - one room lamp always over the top landing and one over the mid landing;
   - the bottom step stays in the room's light.

Map rule: like a dome or vault cell (`40` §6.1), a B4-L's well cells switch the map lamp off and carry their own laylight. B4-M modules keep the room's lamps wherever the ceiling is closed.

## 5. (e) Where each goes, and what changes in the room

**Red, 10-31 17:29 (verbatim):** "办公室里/level 0还有level run里都不应该出现大型的螺旋滑梯". Read as large spiral stairs (the slide is pool-only anyway): no B4-M or B4-L in Level 0, the Office or Run!. The S4 and T2 rows below are withdrawn (both modules keep the 1-cell B4-S of `40` §6.2), the well row applies only to a floor of the pool level, and the station lobby keeps the B5 flared stair without B4 geometry. Stills V1 and W1–W4 stay as size studies only.

| Room (module, `40` §6.2) | Stair | Rise, risers, sweep | What changes in the room | Second route (the Relay's) | Still |
|---|---|---|---|---|---|
| ~~**S4 split suite**~~ withdrawn (Red 17:29; keeps B4-S) (T_L0_SplitSuite, Level 0, Tall 5.4) | B4-M, plaster ribbon | +2.7; 16 × 0.169; 219° | The room grows from 4 × 3 to 5 × 3 cells (15 × 9 m). The +1.2 platform shortens to 3 cells. B4-M takes the east 2 × 2 block, rising from the main floor; its landing joins the +2.7 gallery. The gallery and platform edges become the same plaster ribbon. A pendant hangs down the well. | floor → platform flight, then the flight along the west wall to the gallery | W1, W2 |
| ~~**T2 file mezzanine**~~ withdrawn (Red 17:29; keeps B4-S) (T_OF_FileMezzanine, Office, Tall 5.4) | B4-M, glass ribbon on a steel band | +2.7; 16 × 0.169; 219° | The room grows to 5 × 3 cells. The mezzanine covers the west 1.5 cells. B4-M takes the middle 2 × 2 block; its landing runs west onto the mezzanine. One cubicle cluster instead of two; a downlight through the eye. | the straight flight along the north wall (18 risers) | W3, W4 |
| **Pool gallery** (`P`; T_PL_DomeGallery) | B4-M, tiled ribbon | +3.0; 18 × 0.167; 248° | B4-M replaces the post spiral in a 2 × 2 corner of the deck; its landing reaches the gallery in that corner. An uplight in the eye. This also fixes `P`'s single route (`40` §9 limits). | a straight flight at the far end of the gallery | X1 (X2, X3 rendered, not on the sheet) |
| **Pool hall arrival** (new: T_PL_ArrivalHall) | B4-L, tiled ribbon | 4.0; 27 × 0.148, landing after 13; 168° | A Tall hall 8.4 m high (within 7–9 m), 8 × 8 cells. You arrive from a Level 0 corridor onto a balcony at +4.0 along one wall. B4-L stands in a 3 × 3 block under a 6 × 6 m laylight; its top landing is a bump-out of the balcony. The pool lies to one side, never inside the stair's well. | two flights with a landing along the far wall (or STAIR A) | Y1–Y4 |
| **One well floor** (`O`; a floor of the pool level only, Red 17:29) | B4-L round the void | 4.0; 27 × 0.148, landing after 13; 168° | On one floor only, the well's opening is cut to 9 × 9 m and B4-L winds down inside it. Its 5.9 m eye is the well itself, and it lands on the next floor's rim round a 6 × 6 opening. You see down the shaft through the stair. Every other floor keeps the asymmetric outlines of `40` §7.5. | the stairwell (STAIR A) | not rendered |
| **Station lobby band** (T_L0_LobbyStair, `10` J2) | none (Red 17:29): the B5 flared stair stays; the options here are withdrawn | — | The band is 3.6 m (12 ft) and the module 5.4 m. To read as a helix, B4-L needs a 5.4 m climb in a 7–9 m room; at +2.7 it is a 99° curved flight. Two options: keep the B5 flared stair, making its lower part a 90° segment of the B4-L geometry in terrazzo (3 × 3 cells); or, if the station gets a mezzanine, a B4-M in terrazzo with a brass cap, P1's split-level lobby in small. | the B5 stair itself, or STAIR A | not rendered |

What does not change:
- B4-S stays the post spiral for private, short climbs: the slide tower's core, plant rooms, the casbah's small climbs.
- One vertical event per zone (`20` §5 rule 1): a room with a B4-M or B4-L has no other storey connector except its second route.
- The pool's B4-L is the hall's arrival, not a loop. The player can take it down. The Relay comes by the far flights, so the stair gains the player time and never safety (`40` §6.4).

## 6. (f) Map data: three connector types

`40` §6.1 lists the per-cell `connector` values None, Stair, Spiral (hand), Ramp, Bridge and Void. This proposal splits Spiral into three sizes. It is contract work for 关卡设计; 系统设计 records the navigation rule.

| Type | Code | Footprint | Height class | Rise | Riser | Sweep | Guard | Who walks it |
|---|---|---|---|---|---|---|---|---|
| Spiral S | `SpiralS` | 1 × 1 (diameter 1.5–2.4 m) | any room | ≤ 4.0 m | ≤ 0.24 m | about 330° per 4.0 m | balusters on the post | player only |
| Helical M | `HelicalM` | 2 × 2 (outside 4.36 m) | pool level only (Red 17:29): Tall (≥ 5.4 m) rooms or the well at pool depth | 2.4–4.0 m; a mid landing over 3.66 m | 0.165–0.175 m | 219–335° (372° with the landing) | ribbon or glass | player only |
| Circular L | `CircularL` | 3 × 3 (outside 9.0 m) | pool level only (Red 17:29): Tall rooms of 7–9 m or the well at pool depth | 4.0–5.4 m; one mid landing | 0.148–0.150 m | 168–220° | ribbon | player only |

**One record per connector:**
- `anchor` (the south-west cell) and `size` (1, 2 or 3);
- `rotation` (0, 90, 180, 270) and `hand` (L or R, the mirror);
- `yBot`, `yTop` (multiples of 0.30 m); `risers`; `landingAfter` (a riser number, or none);
- `bottomPort`: the module side the first riser faces;
- `topPort`: the cell edge where the straight top landing meets the upper floor;
- `finish` (L0, OF, PL, LB) and `guard` (Ribbon, Glass, Balusters).

**Cells:**
- **Lower level:** every footprint cell is `Connector`. It is walkable for the player through the stair's own collider, a ramp along the nosing line or step offsets (系统设计). It is solid for the Relay.
- **Upper level:** the footprint is `Void`, guarded, except the landing cells. Those are `Floor`, walkable by both. The Relay may stand on a landing; it never goes down from it.
- **Ceiling:** the lower room's ceiling cells over a B4-M or B4-L are open (`ceilingY` = Void). The B4-L's well cells carry their own laylight (§4).

**Navigation rule (Red):** the Relay's graph has no edge into or across `SpiralS`, `HelicalM` or `CircularL` cells, in either direction. When the player takes one, the Relay re-paths by the second route. The stair gains the player time and never safety.

**Generator checks:**
- Errors:
  - R6: every upper area reached by an S, M or L also has a Relay route (Stair, Flight, Ramp or STAIR A);
  - the footprint lies inside one room; M and L only in their height class;
  - `topPort` meets a floor cell at `yTop`; a mid landing whenever the rise is over 3.66 m;
  - headroom ≥ 2.0 m above the nosing line: the upper level is void over the footprint except the landing;
  - a lamp over every landing, and a light in every walkable well (R8).
- Warnings:
  - the connector sits on the room's centre line (R4);
  - the first riser faces a wall instead of the room's approach;
  - two M or L connectors in one zone (`20` §5 rule 1).

## 7. (g) Stills, round 4

Since Red's 17:29 rule, V1 and W1–W4 (Level 0 and the Office) are size studies only; no B4-M or B4-L is placed in those levels.

![LS10: larger stairs, helical and circular](/Users/redwang/FrontRoomsVisualWork/level_space/sheets/LS10_large_stairs.jpg)

How they were made:
- **Clone and scenes:** rendered in clone `W/proj_levelspace`, in stand-in scenes far from the map, under the game's ambient, fog and post. The pool scenes add the pool look of round 3 (`40` §9).
- **Output:** 13 stills, 1920 × 1080.
  - Scenes V and W (6 stills): `W/level_space/renders/levelspace/r4s/`.
  - The pool scenes X and Y (7 stills): `W/level_space/renders/levelspace/r4sXY/`, from the porcelain pass.
  - The superseded warm-tile pool stills moved to `W/_partial/levelspace_r4s_warmtile/`.
  - Sheet: `W/level_space/sheets/LS10_large_stairs.jpg` (10 stills, Shot format).
- **Source:**
  - `Assets/Editor/ZoneLamps/FrontRoomsZoneLampsShots.LevelSpace4.cs` holds scenes V–Y and the helical-stair helpers (`HxStair`: wedge treads, risers, a soffit, ribbons, caps and rails as meshes);
  - four routing lines in `LevelSpace.cs`; option `r4s`.
- **Round-4 runs (17:13–17:18):** three Unity runs for all 13 stills, each `FINISHED fail=0`. Log: `W/level_space/logs/levelspace_r4s.log`.
  - Run 2 re-aimed W1, X1, X2 and Y1–Y4, and widened V.
  - Run 3 added teal risers to the pool stairs (removed again in the porcelain pass) and replaced two views.
- **Porcelain pass (17:25), after Red's 17:19 and 17:24 decisions:** scenes X and Y were re-rendered with option `r4sXY`, in porcelain white tile on treads and risers, with the dark nosing strips added. One Unity run of the two allowed, `FINISHED fail=0`, 7 stills saved; no fix run was needed. Log: `W/level_space/logs/levelspace_r4sXY.log`.
- Every camera is at eye height (1.62 m) above the floor or landing it stands on.

| Still | Scene | Camera | What it shows | On sheet |
|---|---|---|---|---|
| `v1_three_sizes` | V · lineup, Level 0 room 30 × 24 m, 8 m tall | main floor, 16 m back | B4-S (1 cell), B4-M (2 × 2), B4-L (3 × 3) side by side, each climbing 4.0 m to one upper floor. B4-M includes the mid landing (372°); B4-L sweeps 168°. | yes |
| `v2_from_upper_floor` | V | upper floor at +4.0 | Weak: the upper floor's glass guard fills the near view and the stairs below hardly show | no |
| `w1_suite_b4m` | W1 · split suite, 15 × 9 × 5.4 m | main floor | B4-M with its plaster ribbon and oak cap, the pendant down the well, the landing into the +2.7 gallery. The platform's flight (the second route) is behind the camera. | yes |
| `w2_suite_from_landing` | W1 | B4-M top landing | Down the helix and the well to the floor; the platform and the west flight beyond | yes |
| `w3_mezzanine_b4m` | W2 · file mezzanine, 15 × 9 × 5.4 m | office floor | B4-M with glass ribbons on a steel band, the +2.7 mezzanine of files. The straight flight along the wall is the Relay's way up. | yes |
| `w4_mezzanine_from_landing` | W2 | B4-M top landing | Through the glass down the helix; the 1.65 m cubicle panels below | yes |
| `x1_pool_deck_b4m` | X · pool hall, 18 × 15 × 6 m | deck | B4-M in porcelain white tile from the deck to the +3.0 gallery: from outside, the solid ribbon reads as one twisting band | yes |
| `x2_pool_from_landing` | X | B4-M top landing | Better after the porcelain pass: dark nosing strips fan out across the treads, so the steps read from the top. The landing's parapets still crowd the frame. | no (the sheet keeps its layout) |
| `x3_from_the_gallery` | X | gallery at +3.0 | Weak: the gallery's glass guard sits dark across the view | no |
| `y1_arrival_from_top` | Y · pool hall arrival, 24 × 24 × 8.4 m | B4-L top landing, at +4.0 | The arrival: the ribbons sweep down and left round the well, every step marked by its dark nosing; the pool to the right | yes |
| `y2_hall_from_deck` | Y | deck | B4-L under its 6 × 6 m laylight, its porcelain steps marked by dark nosings; the balcony behind. The far flights (the Relay's way) are outside the frame. | yes |
| `y3_from_mid_landing` | Y | mid landing at +1.93 | On the 12 ft landing: across the 5.9 m well to the pool | yes |
| `y4_inside_the_well` | Y | in the well, deck level | The ribbon rising round and over you toward the balcony; uplights on the inner ribbon | yes |

What the greybox showed:
- **A dark nosing strip makes porcelain steps read.**
  - From the top of a pale tile stair the risers face away, so plain treads merge into one surface (run 3, before the porcelain pass).
  - With a 50 mm dark nosing on every tread, the steps read from the top (`y1`, `x2`) and from below (`y2`).
  - This is the period's own safety nosing, and Red chose it over coloured risers (17:24).
- **The solid ribbon reads as one form.** From outside it hides the treads (`v1`, `x1`), which is the intended look. The steps show from below (`y2`) and through glass (`w4`).
- **Glass guards in front of a camera read as dark panels** (`v2`, `x3`). In the game they are seen at an angle and in motion, but the gallery-to-stair view is better given by a landing than by a glass gallery edge.
- **Level 0's plaster ribbon reads very white** under the warm lamps (`w1`, `w2`). Cream paint or the wallpaper's ground colour may sit better (§8).

## 8. Open questions for Red

1. **B4-L width:** 1.45 m on a 2.95 m inner radius (9.0 m outside, exactly 3 × 3 cells; rendered), or `50`'s 1.50 m on 3.0 m (9.2 m, crossing the cell lines by 0.10 m)?
2. **Over the pool hall's B4-L:** the square laylight on the grid (rendered), or the dome of `50` §1.2? A dome straight over a circular stair in a pool hall is the image most likely to read as borrowed from the genre. The laylight keeps it on the grid and closer to P4.
3. **B4-M over a full 4.0 m storey:** keep the 12 ft mid landing (372°, a little over one turn; rendered in V), or drop it (335°)? The period code asks for it.
4. **Pool B4-M:** a solid tiled ribbon (rendered), or glass so that the gallery reads through it?
5. Closed (Red 17:29: no large spiral stair in the Office). **Office B4-M:** a glass ribbon (rendered), or Level 0's plaster ribbon?
6. Closed (Red 17:29: the station lobby keeps its B5). **Station lobby:** keep B5 with a 90° B4-L segment in terrazzo, or give the station a mezzanine and a terrazzo B4-M? Either needs a terrazzo surface.
7. Closed (Red 17:29: no large spiral stair in Level 0). **Level 0 ribbon colour:** white paint (rendered), cream, or the wallpaper's ground colour?
8. **Pool risers: answered by Red, 17:24.** Porcelain white risers; only a dark nosing strip marks each step, with no teal band and no second tile colour (§3). Rendered in the porcelain pass (§7).

## 9. Limits

- **Precedents:** read as text, captions and one measured study (P1). No image was opened or used. P4's diameter and step count were not found.
- **Code text:** the 1976 UBC (a scan), a current California regulation with the same wording, and one current IBC section. The 1988–1991 UBC and BOCA editions are to verify before any rule or sign quotes a number.
- **BS 5395-2:** not re-read here; the online copy returned only its title. Its use stays as in `50` [LS55].
- **The greybox:**
  - walls have no thickness;
  - treads, soffits and ribbons are generated meshes without collision;
  - lamps are lens quads with spot lights; water is a transparent layer;
  - the Relay is not placed in these scenes.

## 10. (h) Sources

| # | Source | Date | What it gives | Status |
|---|---|---|---|---|
| LS61 | Albert Samper and Blas Herrera, "Una escalera suspendida / A suspended stair" (the lobby stair of the SAS Royal Hotel), ARQ (Santiago), 2014, https://doi.org/10.4067/s0717-69962014000200007 ; PDF, https://www.redalyc.org/pdf/375/37532094007.pdf ; Arne Jacobsen, works, https://arnejacobsen.com/works/sas-royal-hotel/ ; BBC Culture, 2018-02-26, https://www.bbc.com/culture/article/20180226-how-do-you-reinvent-a-design-masterpiece | opened 1960; study 2014 | A split lobby 4.88 m high; 32 steps in seven-eighths of a turn; width 1.8 m, eye 2.4 m; a folded 20 mm steel plate; 16 cables (10 every 30°, 6 every 60°) and two hidden supports; blue carpet; a 0.90 m guard with posts every 60° and darkened glass; the first step faces the door | READ |
| LS62 | Metropolitan Opera House: Landmark West!, fact sheet, https://www.yumpu.com/en/document/view/6336665/the-metropolitan-opera-house-landmark-west ; Landmark West, https://www.landmarkwest.org/metopera/ ; *New York Times*, 1966-09-17, "Met as Architecture; New House, Although Technically Fine, Muddles a Dramatic Design Concept", https://www.nytimes.com/1966/09/17/archives/met-as-architecture-new-house-although-technically-fine-muddles-a.html ; *Harvard Crimson*, 1966-09-27, https://www.thecrimson.com/article/1966/9/27/the-new-met-pperhaps-it-was/ | opened 1966-09-16 | A curvilinear double staircase of poured-in-place prestressed concrete (boat builders' formwork); terrazzo; deep red woven carpet with an Italian marble border; eight chandeliers; balconies overlooking | READ (excerpts) |
| LS63 | Palácio Itamaraty: Ministério das Relações Exteriores, https://www.gov.br/mre/en/subjects/itamaraty-palace ; *Elephant*, 2021-08-28, "Milton Ramos, Staircase at Itamaraty Palace, Brasília, 1970", https://elephant.art/milton-ramos-staircase-at-itamaraty-palace-brasilia-1970/ ; "Narrativas sobre o Palácio Itamaraty a partir do jornal Correio Braziliense (1960-1970)", *Anais do Museu Paulista* (SciELO), quoting *Correio Braziliense*, 1969-04-19, https://www.scielo.br/j/anaismp/a/Y6jyh676z4JD7sZQ7n66hSp/?format=pdf&lang=pt | 1969–70 | A column-free hall (structure by Joaquim Cardoso); a poured-concrete helical stair with no balustrade or handrail, carpeted deep blue; "escada estilo helicoidal, que funciona como âncora de laje" | READ (excerpts) |
| LS64 | Louvre Pyramid: NCK (structural engineers), project page, https://nck.ca/en/projects/pyramid-of-the-grand-louvre-and-monumental-spiral-staircase-of-the-louvre-museum/ ; Paris Musées, Serge Sautereau photograph and caption, April 1989, https://www.parismuseescollections.paris.fr/en/node/95401 ; Musée du Louvre, "A pyramid for a symbol", https://www.louvre.fr/en/explore/the-palace/a-pyramid-for-a-symbol ; Macocco (glass), https://www.macocco.com/fr/escalier-helicoidal-monumental-du-musee-du-louvre/escaliers.html ; Stair Parts USA, https://www.stairpartsusa.com/blogs/blog/15650963-i-m-peis-staircase-at-the-louvre | 1985–89 | A helical box of welded steel plates supported only at the first and last steps; a lifting platform in its centre (the "peau d'orange" stair round the "piston" lift); stone treads; a curved laminated glass guard carrying the handrail, no balusters; inaugurated March 1989 | READ (excerpts) |
| LS65 | International Conference of Building Officials, *Uniform Building Code*, 1976 edition, §3305 Stairways, §3312 Exit illumination; scan, https://structuretech.com/wp-content/uploads/2016/11/UBC_1976.pdf | 1976 | Width 44 / 36 in; rise ≤ 7.5 in, run ≥ 10 in; winders measured 12 in from the narrow side; circular stairs as exits (run ≥ 10 in, smaller radius ≥ twice the width); spiral stairs only in dwellings (400 sq ft, 26 in, 7.5 in run at 12 in, risers ≤ 9.5 in); ≤ 12 ft between landings; handrails 30–34 in on both sides; headroom 6 ft 6 in from the nosing line; exits lit to one footcandle | READ (excerpt) |
| LS66 | California Code of Regulations, Title 8, §3231 Stairways (citing Title 24, Part 2, §3305), https://dir.ca.gov/title8/3231.html ; LII copy, https://www.law.cornell.edu/regulations/california/8-CCR-3231 | current text of the UBC-derived rule | The same circular-stair wording; rise 4–7.5 in; run ≥ 10 in; slip-resistant treads; landings; ≤ 12 ft between landings | READ |
| LS67 | International Building Code, §1009.7 Circular stairways, as published by UpCodes, https://up.codes/s/circular-stairways | an edition after 1993; used only for the walk-line convention | Smaller radius ≥ twice the width; tread ≥ 11 in at 12 in from the narrow end, ≥ 10 in at the narrow end | READ |
