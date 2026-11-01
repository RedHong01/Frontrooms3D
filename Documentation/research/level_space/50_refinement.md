# Level space · 50 · Refinement: larger spiral stairs, the slide, and a furniture system per room

Date: 2026-10-31. Line: 浴室房间调研 (`bathroom`), K session (Red's API key, Cursor), research sub-chat.

Red, 10-31 ~16:27, on the round-3 sheets (LS5–LS9): "我认为螺旋楼梯也许可以更大一些，并且滑梯的样式我需要你仔细调研。其他的房间本身是ok的，但是屋子里的陈设也就是家具系统需要大范围的更新和增加，这样能够符合房间的内容需求，而不是用现有的很多家具重复利用。" And: "不用评估成本，我需要你开始research细化方案".

Builds on `40_terrain_grammar.md` (types B4, F4; the round-3 rooms), `bathroom_rooms/10_research.md` §2 (the club's period fittings: ladders, gutter, depth marks, guard chair, pace clock, lockers, showers; not repeated here), `office_and_film/22_era_lock.md` (how a kit asset is dated), and the kit library as it stands (`Assets/Resources/Props/Models`, about 200 `Kit_*` models). Research only: no code, no render, no download, no cost estimate. Sources [LS51]–[LS60] in `SOURCES.md`.

## 0. Short answer

1. **Larger spiral stairs: three sizes, three meanings.**
   - **B4-S, today's code spiral** (1 cell, 1.5–2.4 m): equipment; only on short private climbs.
   - **B4-M, a helical stair round an open well** (2 × 2 cells, about 4.2–5.0 m across): a feature stair, no centre post. It goes in the split suite, the file mezzanine and the pool gallery.
   - **B4-L, a circular stair to the egress rule** (inner radius at least twice its width, about 6.6–7.5 m across; 3 × 3 cells in a Tall room): monumental. It belongs in the pool hall or the well.
   
   The difference that makes it read as larger is the open well and the width (1.1 m and up), not only the diameter [LS55].
2. **The slide is a 1980s fibreglass body slide** [LS51]–[LS54].
   - **Construction:** flanged, bolted sections in gelcoat on galvanised steel columns, from a stair tower to a 0.9 m splash pool, with a guard at the top and one at the bottom.
   - **Three styles worth rendering:**
     - **SL-A, translucent tube** (see-through, lit at night, like the 1984 mall slide): the tube becomes a line of light through the building;
     - **SL-B, open flume** in white or ivory gelcoat with one band of period colour: the municipal pool look;
     - **SL-C, open and closed sections mixed**: the period's own documented fault (riders cut at the transition, blind spots under opaque roofs), so the slide itself is "wrong".
   - **Its most uncanny fact:** small indoor slides often started and ended inside the building while the flume ran outside the walls [LS52]. Our tube leaves the room through one wall and comes back through another.
3. **Furniture: from a shared office kit to room programmes.** Every room type gets a programme:
   - what a real room of 1985–93 held (sources §3);
   - the pieces that must be there for the room to read as itself;
   - the clutter of its use;
   - a "state" (left mid-task, cleared, piled).
   
   Existing kits stay where they belong. Each room type also gets its own new pieces (§3.2): about 60 new kit models across 11 room types, ranked by how much each one makes its room legible. The Level Designer's module props become the place where a programme is authored (§3.3).

## 1. Larger spiral stairs

### 1.1 What "spiral" can mean [LS55]

| Term (BS 5395-2:1984 and US codes) | Form | Rule that matters |
|---|---|---|
| Spiral stair | a helix round a central column | US codes of the period allow it as egress only from a space ≤ 250 sq ft for ≤ 5 people; clear width ≥ 26 in; riser ≤ 9.5 in (`40` [LS48]) |
| Helical stair | a helix round a central **void**, no column | a feature stair; "not normally permitted … as means of escape where large numbers of persons are involved" |
| Circular stair (US) | a curved stair whose smaller radius is ≥ twice its width | may serve as an egress stair with normal risers and treads; the grand curved stair of lobbies |

### 1.2 Three sizes for FrontRooms

| Size | Footprint | Geometry | Reads as | Where |
|---|---|---|---|---|
| **B4-S** (today) | 1 cell | post, diameter 1.5–2.4 m, riser ≤ 0.24, one turn per storey | equipment, private | slide-tower core, plant rooms, the casbah's small climbs |
| **B4-M** helical | 2 × 2 cells (6 m) | open well 1.2–1.8 m, stair width 1.1–1.4 m, outside diameter 4.2–5.0 m, riser 0.17, tread 0.28 at the walk line, about 300° per storey (17 risers) | a feature stair: you see through its well to the floor below | S4 split suite, T2 file mezzanine, the pool gallery (P), the club lobby |
| **B4-L** circular | 3 × 3 cells (9 m), Tall rooms only | width 1.5 m, inner radius ≥ 3.0 m (twice the width), outside diameter about 9 m, riser 0.15, tread 0.30–0.33, 180–220° per storey | monumental: the stair is the room | the pool hall's arrival (under the dome), one well floor (O), the station lobby band (`10` J2) |

Details that make a larger stair read as larger:
- a solid curved stringer or balustrade (a helical "ribbon") instead of balusters on a post;
- landings at the top and bottom on the 3 m grid, so the stair splices into the floor (R2);
- treads in the room's own finish (tile in the pool, carpet in Level 0, terrazzo in a lobby);
- light from the well or a ring of lamps on the inner edge.

Rules from `40` still hold:
- the Relay does not use spirals (Red, ~16:15);
- B4-M and B4-L always have a second way onto their upper level (R6);
- a B4-L is wide enough that a player reads it as a route, not as a shortcut.
- **Question for Red:** can the Relay use a B4-L? It is a real stair by the code.

## 2. The slide

### 2.1 What a 1980s slide was [LS51]–[LS54]

- **Material and makers:**
  - fibreglass (or concrete) channels with water circulating down them;
  - WhiteWater began in 1980 and had its own fibreglass plant within five years; ProSlide began in 1986 with a sailboat-builder's fibreglass;
  - by 1983 the US had 29 large water parks [LS54].
- **Two forms:** open flumes and enclosed tubes, often mixed in one run.
  - A 1986 London example: open and closed sections over 110 ft, a 50 ft fall, up to 30 mph, a 3 ft splash pool.
  - Another site had four side-by-side enclosed tubes ridden head first at up to 40 mph [LS51].
- **Sections and joints:** standard flanged joints between sections, or longer "seamless" sections wrapped in fibreglass on site. Small indoor installations started from low platforms [LS52].
- **Indoor installations:**
  - New Zealand's first indoor hydroslides (1981): head first on foam mats into a small pool. The slides started and finished inside the aquatic centre, but the flumes usually ran outside the building envelope [LS52].
  - A Canadian mall in 1984: "climb up, up, way up to the rooftop tower … a safe, see-through fiberglass tunnel of curves and turns and wild dips". The pool was glassed in "so everyone can watch… a human aquarium", and "at night the flumes light up". It closed at the end of 1985 when insurance rose [LS53].
  - World Waterpark (1986): a web of stairs to platforms, slides to 25.3 m, a dark enclosed slide into a 1.2 m splashdown pool (`40` [LS44]).
- **Operations:** a guard at the top regulates riders and one at the bottom clears the pool [LS51].
- **Documented faults** (A&E study, 1986 season) [LS51]:
  - lacerations at the transition from open to closed sections;
  - opaque roofs on closed sections making blind spots where riders stopped and formed chains;
  - abrupt swerves throwing riders against the walls;
  - splash pools without a gentle last slope.
  
  The paper's remedy: all open or all closed, clear roofs, a lip at water level.
- **Colour:** gelcoat in opaque or translucent colours. Today's makers offer RAL-based ranges from traffic white and ivory through turquoise, blues, yellows and reds [LS54]; period photographs are still to find (to verify).

### 2.2 The tower and the splash pool

- **Tower:** galvanised steel columns and grating platforms with a stair, either B4-S round a core or a square switchback tower. A gate and a start tub (water inlet) sit at the top, and the guard's chair or station beside them.
- **Supports:** paired steel columns with saddles under the sections, every 3–4 m of run; bolted flanges show on the outside of every joint.
- **Splash pool:** 0.9 m deep (3 ft) [LS51]; the lip delivers the rider at the water surface; a run-out with steps out at the far end; signs "ONE AT A TIME", "FEET FIRST" (strings by 叙事设计, lettering by 平面视觉).

### 2.3 Three styles to render (round 4)

| Style | Look | What it does in FrontRooms | Palette |
|---|---|---|---|
| **SL-A · translucent tube** | an enclosed tube in translucent gelcoat, lit from inside at night [LS53] | a line of light through rooms; the player sees a silhouette slide past inside it (a sound cue, never a radar) | aqua or teal translucent: matches F1 |
| **SL-B · open flume** | an open U channel in white or ivory with one coloured band; flanges and saddles show | the honest municipal pool; readable, period-true, calm | white or ivory + one band (teal, or the club's rose) |
| **SL-C · mixed open and closed** | open runs that enter opaque tunnels | the period's own fault made visible: the slide is wrong in the way 1986 slides were | ivory open runs, dark opaque tunnels |

**The building fact to use:** the flume leaves through one wall and returns through another [LS52]. In FrontRooms the tube can cross a room you never reach, or pass behind the well's arcade: a structure seen in pieces, joined in the mind.

**The guard (15 §3)** covers *Dream Pools*' slides and those of POOLS or any game: no composition is taken. The slide is built from the 1980s parts above.

## 3. Furniture: room programmes

### 3.1 The problem and the rule

- **The problem:** the round 1–3 rooms reuse the office kit (desk, task chair, CRT, cubicle panel, filing cabinet) in rooms that are not offices: a computer room, a training room, a lounge, a trading floor. Red: the furnishings must fit each room's content, not repeat the existing pieces.
- **The rule: every room type has a programme.**
  - **Signature pieces:** two or three objects that only this room type has, so the player names the room at a glance.
  - **Working set:** the furniture its use needs, in the arrangement the use makes (rows, U, against walls).
  - **Clutter of use:** the small things work leaves behind.
  - **State:** left mid-task, cleared and dust-sheeted, or piled (the existing pile system, `office_and_film/06`).
- Existing kits are used only where they belong. A desk is a desk, but a trading desk is not an office desk.

### 3.2 Programmes per room type (period contents, sources, kits)

Existing kits named as they are; **new** kits in bold.

| Room (round) | Period contents (what the room held) | Signature pieces | Working set | Clutter and state | Existing kits that fit |
|---|---|---|---|---|---|
| **Computer room** (S1) [LS56] | tape drives, disk farms (removable-pack and sealed drives), mainframe chassis, printers, operator terminals and console area, tape library racks with a "cache" rack, power panel, temperature/dewpoint chart recorder, chiller/condenser, fire alarm panel | **Kit_TapeDrive** (reel units behind glass doors), **Kit_DiskDrive** (a row of top-loading packs), **Kit_TapeLibraryRack** | **Kit_MainframeBay** (cabinets with skins), **Kit_LinePrinter**, **Kit_OperatorConsole**, **Kit_CRACUnit** (cooling cabinet), **Kit_ChartRecorder** (wall), **Kit_EPOButton** (red, covered) | fanfold paper boxes, tape reels on a cart (**Kit_TapeCart**), a floor-tile lifter; state: a console left logged in, a tape half loaded | Kit_FireAnnunciator, Kit_Keyboard, Kit_CRTMonitor (console only) |
| **Training room** (S2) [LS57] | overhead projector with transparencies, projection screen, lectern, flip-chart easel, training tables, stacking chairs, TV and VCR on an AV cart, slide projector; LCD overhead panels from the late 1980s | **Kit_OverheadProjector** on **Kit_ProjectorCart**, **Kit_ProjectionScreen** (tripod or pull-down), **Kit_Lectern** | **Kit_TrainingTable** (rows on the tiers), **Kit_StackChair**, **Kit_FlipChartEasel**, **Kit_AVCart** + Kit_CRTTV | transparencies on the projector, a marker tray, handouts on every seat; state: a session left mid-slide | Kit_CRTTV, Kit_WallClock, Kit_HatStand |
| **Sunken lounge** (S3) | the 1970s–80s office lounge and reception seating: low modular sofas, coffee tables, ashtray stands, plants, magazine racks | **Kit_ModularSofa** (corner, armless and end units), **Kit_PlanterFicus** | **Kit_CoffeeTableLow**, **Kit_MagazineRack**, Kit_Ashtray | magazines, cups, an ashtray full; state: cushions slipped, one unit pulled out of line | Kit_Ashtray, Kit_Urn, Kit_CoffeeCup |
| **Split suite** (S4) | an executive suite of a multi-floor tenant: executive desk and credenza, guest chairs, a small conference table, a bookcase wall, the convenience stair | **Kit_ExecutiveDesk** (U or L), **Kit_ConferenceTableRound**, the B4-M stair | **Kit_ExecChair** (high back), **Kit_GuestChair**, Kit_Credenza, Kit_Bookcase | desk set, framed certificates (lettering 平面视觉), a dead plant | Kit_Credenza, Kit_Bookcase, Kit_Hutch_Cherry, Kit_Torchiere, Kit_TableLampPleated, Kit_HatStand |
| **Raised trading floor** (T1) [LS58] | open rows of desks (benches, not cubicles); quote terminals with special keypads; two to four monitors per trader by the late 1980s; trading turrets with 30–300 line buttons, several handsets and speakers; world clocks; paper tickets; even a track carrying papers along the desk tops | **Kit_TradingTurret**, **Kit_QuoteTerminal** (fraction keypad; no brand), **Kit_WorldClocks** (a wall row) | **Kit_TradingDesk** (bench rows with a monitor riser), **Kit_DeskFileTrack**, Kit_TaskChair | phones off the hook, tickets on the floor; state: every screen still on | Kit_DeskPhone, Kit_CRTMonitor, Kit_WallClock |
| **File mezzanine** (T2) [LS59] | lateral files (4–5 drawers, about 130 linear inches each); mobile high-density shelving on floor tracks, hand-cranked; file carts; microfiche readers and carousel files | **Kit_MobileShelving** (track units with crank wheels), **Kit_LateralFile** (4- and 5-drawer) | **Kit_FileCart**, **Kit_MicroficheReader**, Kit_StepStool | record boxes, files out on a cart; state: one shelving aisle left open (the only place to stand) | Kit_FilingCabinet (vertical), Kit_StepStool, Kit_Crate |
| **Loading dock** (U) [LS60] | dock leveler (pull-chain, pit style), laminated rubber bumpers, foam dock seals or a truck shelter, manual overhead doors, wheel chocks (OSHA 1978) and the first vehicle restraints (1980), swing-arm dock lights, pallets and pallet jacks, a compactor | **Kit_DockLeveler**, **Kit_DockSeal**, **Kit_DockLightArm** | **Kit_PalletJack**, **Kit_WheelChock** on a chain, **Kit_Bollard**, **Kit_RollCage** | stretch-wrapped pallets, a broken pallet, a chock left out; state: one door half up | Kit_Pallet, Kit_HandTruck_Cartons, Kit_BumperRail_1200, Kit_TrashBin |
| **Club entry and lockers** (pool, `50` modules) | `bathroom_rooms/10_research.md` §2 | the towel desk, locker banks, benches, gang showers | per §2 | towels, a lost-and-found box; state: lockers open in a row | per §2 (pool line's kits) |
| **Pool hall and level** (P) | `10_research.md` §2: ladders, gutter, depth marks, guard chair, pace clock, lane lines, life ring | **Kit_LifeguardChair** (elevated), **Kit_PaceClock**, **Kit_LaneRopeReel** | **Kit_KickboardRack**, **Kit_DeckLounger** (strap), **Kit_LifeRingPost** | kickboards in the water, a lane line coiled on the deck; state: everything ready, nobody | (pool line) |
| **Slide tower** (Q) | §2.2 | the tower, **Kit_SlideStartTub**, **Kit_SlideFlume** sections (open, closed, translucent) | **Kit_SlideSupport** (paired columns), **Kit_SlideGate**, a guard station | foam mats stacked (head-first slides, 1981 [LS52]); state: water still running | — |
| **Casbah and well** (R, O) | architecture first; furniture only as traces of the club's programme misplaced (18 M6) | a locker bank on a block top, a guard chair facing a wall | — | towels on a stair | pool kits |

### 3.3 How the system works in the map

- **A programme is data.** Per room type it lists its signature pieces, working set, clutter and states, each with:
  - counts;
  - placement rules: against a wall, in rows facing a front, a U round a console, along a track;
  - keep-clears.
  
  It lives with the room module in the Level Designer (module props by kit name, `LEVEL_DESIGNER.md`), so each authored module is a furnished room. Generated rooms of that type draw from the same programme.
- **Variety without repetition:**
  - two or three model variants per signature piece;
  - per-instance state (open or closed, on or off, tidy or disturbed);
  - one "event" per room (`10` L0-5): the console left logged in, the projector still on.
- **Office stays office.** The cubicle field (`FrontRoomsOfficeKit`) keeps furnishing Office zones; the new programmes cover every other room type.
- **Era lock** (`office_and_film/22`): every new kit is dated 1993 or earlier, with no brand on any surface (quote terminals, turrets and drives are generic designs).
- **Owners:**
  - 游戏视觉: kits (Blender kit pipeline, `Tools/Blender/frontrooms_kit`), the programme data format and the dressing code;
  - 关卡设计: module authoring and generator hooks;
  - 平面视觉 and 叙事设计: labels, signs and printed matter.

### 3.4 Order (by legibility, no cost)

1. **The signature pieces of each room** (about 25 kits): TapeDrive, DiskDrive, MainframeBay, OverheadProjector, ProjectionScreen, Lectern, ModularSofa, ExecutiveDesk, TradingTurret, QuoteTerminal, TradingDesk, MobileShelving, LateralFile, DockLeveler, DockSeal, DockLightArm, LifeguardChair, PaceClock, SlideFlume (three styles), SlideStartTub, SlideSupport, B4-M and B4-L stairs.
2. **Working sets** (about 25 kits).
3. **Clutter and states** (about 10 kits plus state variants).

## 4. Next (for Red)

- **Round 4 greybox:** B4-M and B4-L in S4, T2 and P; the three slide styles in Q, with the tube leaving through one wall and returning through another.
- **The furniture programmes:** start with the signature pieces of the computer room, training room, trading floor and dock, the rooms where today's reuse reads most wrong.
- **The question in §1.2:** may the Relay use a B4-L?

## 5. Limits

- Slide colours of the 1980s are inferred from today's gelcoat ranges and one 1984 description (a see-through tube). Period photographs are still to find.
- Computer-room contents come from university machine rooms of the early to mid 1980s. A tenant data room in a 1990 office tower was smaller and newer (to verify).
- Trading-floor details come from New York and Chicago sources; a FrontRooms trading floor is a generic tenant's.
- Kit names are proposals; 游戏视觉 names and builds them.
