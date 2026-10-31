# zone_lamps · 10 · Other zones: the period fixture for each, and where L03 already fits

游戏视觉, zone-lamps workflow, research stage "other", 2026-10-31. **Research only.** No game code, no Unity, no git, no
download. Main was read only; none of the files read was dataless (rule 20). Hashes of what I read:
`~/FrontRoomsVisualWork/zone_lamps/10_other_base.txt`.

Red, 2026-10-31 (游戏视觉 chat): "目前不同区域内的灯的样式应该是符合对应区域的，我需要你对年代进行调研，然后思考比如说泳池房间（在这里泳池房间指的是后室概念里的特定泳池level，以及那个年代的泳池会使用的设施）包括run level以及office level都是，都需要对应的调研以及灯具的revise".

**Scope.** The zones of `00_inventory.md` that are not the pool, Run! or the Office (those have their own stages):
the title corridor and start rooms (stream Lobby), Level 0 rooms beyond the ceiling troffer, Low zones (2.4 m), Tall
halls (5.4 m), the security station, Stairwell X, the EXIT approach and vestibule, restrooms, storage and electrical
rooms. L03 (air-handling 2×4 troffer, lens 1.8 as seen) is decided for the Level 0 ceiling and is not reopened here.

**Reused, not redone.** `~/FrontRoomsVisualWork/lamp_options/docs/` (10a families 1–12, 10b, 30_for_red, options
L00–L10; ids `LO:Sxx`, `LO:Bxx`), `research/bathroom_rooms/` (pool club and washroom; D7 still open; ids `BR:Sxx`),
`research/stairwell/` (stairwell A, decided; `SW:Sxx`), `research/exit_sign/` (`ES:Sxx`), `research/station/`,
`research/room_visuals/` and `run_b_production/` (the wraparound and twin-head kits). New sources are `Z01`–`Z28`
(§7).

**Evidence tags.** [P] dated inside or close to 1975–1993. [L] later source describing the same thing or a
long-running line. [V] present-day vendor. [D] my arithmetic from sourced numbers. [U] unconfirmed.
[M] from memory, times not checked. Brand and trade names appear here as sources only; none may appear on an in-game
object.

**Side effect to report.** WebFetch of the scanned MIL-HDBK-1004/4 (1987) PDF left a 1.9 MB binary in this session's
`tool-results/` folder (`webfetch-1793475890188-m4rguu.bin`). It is image-only and was not read or OCR'd. Nothing else
was saved.

---

## 0. Short answer

| Zone | Period answer (c.1990 US office building) | Does L03 fit? | First pick (§4) |
|---|---|---|---|
| Title corridor + start rooms | Lay-in floor of a tenant building: lensed 2×4 troffers. Lobbies proper added downlights and wall fixtures [Z01] | **Yes, must** (start-door seam) | L03, identical to the map |
| Level 0 rooms beyond the troffer | Same troffers; lounges and snack bars on a lay-in ceiling had 75 W incandescent cans [Z02]; EXIT signs and battery units near exits [SW:S6] | **Yes** | L03 + cans only in lounge / waiting modules |
| Low zones (2.4 m) | 8 ft was the *traditional office* ceiling, not only back of house [Z10]; service corridors had wall-bracket or wraparound 2-lamp F40 [Z02, LO:B02] | **Yes** | L03 in Low rooms; wraparound in Low service corridors |
| Tall halls (5.4 m) | Above 16 ft the 1991 GSA draft wants HID, HPS in high bays [Z01]; lobbies used recessed metal-halide downlights with specular cones [Z03, Z04] and HID indirect uplights on walls [Z07] | **Weak** (no dated lay-in at 18 ft found) | Recessed metal-halide downlight with standby quartz lamp |
| Security station | The floor's troffers; the guard post sits in the entrance lobby [station/60] | **Yes** (decided) | L03 unchanged; optional warm task lamp |
| Stairwell X | Wraparound or guarded globe at landings + twin-head battery unit (decided, stairwell A) | **No** (hard slab) | Keep stairwell A |
| EXIT approach / vestibule | Stencil EXIT with two 20 W lamps; a twin-head mounted above the EXIT sign on its circuit (1986 drawing) [ES:S1, SW:S6] | **Yes** on the floor side | L03 held Steady + EXIT + twin-head over the sign |
| Restrooms | GSA 1991: "indirect or down lighting" for toilets [Z01]; older buildings: round surface incandescent drums in toilets and stairs, 1953 stock still in use in 1993 [Z02] | **Partly** | Depends on D7; if built, lensed 2×2 or L03 + no mirror light |
| Storage / electrical | GSA 1991: "industrial type fluorescent" in equipment rooms and closets [Z01]; 300 W pendant incandescent in supply and equipment areas, porcelain lampholders [Z02]; working-space light required by the NEC since 1965 [Z11] | **No** (except tenant storage inside the grid) | Industrial 2-lamp strip with white reflector |

Three things matter most for the revise:
1. **Light per space was lower outside offices.** The 1991 GSA draft: offices 538 lx (50 fc); internal corridors,
   public areas and support spaces 215 lx (20 fc); maintenance areas 538 lx [Z01]. So a real lobby or corridor floor
   was about 0.4 of an office floor. Exit paths only had to reach 10.8 lx (1 fc) [SW:S1, ES:S3].
2. **The source type changes the failure.** Fluorescents flicker, blacken at the ends and die in pairs (LO 10a §6).
   Incandescent lamps live 1,000–2,000 h against the fluorescent's 20,000 h [Z20, LO:S07], so in a neglected building
   the incandescent fixtures are the ones with *dead lamps*, not flicker [D]. HID lamps go out on a dip and need
   8–15 min (metal halide) or 2–15 min (HPS) to restrike [Z06]; period fixtures added a standby quartz lamp for that
   gap [Z03, Z13]. Each type needs its own mapping onto `ModuleLamp` (§5).
3. **What reads from far away.** Lensed and wraparound faces stay bright to the horizon (LO 10a §5). Recessed
   downlights and HID cones go dark past their cut-off, so a hall lit by them shows dark holes and bright floor pools.
   Bare strip tubes are about 2.4 times brighter than a lensed face [D, §2.3].

---

## 1. The zones as the game has them (from `00_inventory.md`)

| # | Zone | Ceiling | Lamp today | Owner of placement / look |
|---|---|---|---|---|
| 1 | Title corridor + start rooms (stream Lobby) | 2×4 print, 2.9 m | Stream pan + prismatic lens + beam + Spot, 12 per room | 游戏视觉 / 游戏视觉 |
| 2 | Level 0 Standard (incl. `L0_WaitingRoom_4x3`) | 2×4 print, 2.9 m | Map lens box, 1 per 3 m cell | 关卡设计 / 游戏视觉 |
| 3 | Level 0 Low (incl. `Low_Storage_2x3`) | same print, 2.4 m | same | same |
| 4 | Level 0 Tall (`Tall_PillarHall_6x5`, 0.9 m columns 70 %) | same print, 5.4 m | same, ×1.6, range 12 | same |
| 6 | Station (FrontDesk 4×4, floor stations) | map ceiling + band, 2.9 m | Level 0 lamps in fixed modes + TV spot | 关卡设计 / 游戏视觉 |
| 7 | Stairwell X (C22 vestibule cell, safe zone) | slab, 2.40 m | map point lamp; desktop wrap + twin-head | 关卡设计 / 游戏视觉 |
| — | EXIT approach (the floor cell outside X's door) | as host | map lamp, exit hold 5×5 Steady | 关卡设计 / 游戏视觉 |
| 13 | Office washroom (planned, D7) | lay-in 2.9 m [U] | none | not designed |
| 12 | Club washroom, lockers, pump room (planned, L8) | hard [U] | none | bathroom_rooms §2 rows 18–24 |
| — | Storage / electrical (`Low_Storage_2x3`; no electrical room module) | Low print | map lamp | 关卡设计 / 游戏视觉 |

"Exit vestibule" in this doc means the C22 vestibule cell X (the stair's own room) and the floor cell in front of its
door. The game never reaches a street exit, so the building's front vestibule is out of scope.

---

## 2. Facts shared by every zone

### 2.1 What a 1991 federal draft asked for, space by space [Z01, P]

NISTIR 4960 (NIST for GSA, March 1993) summarises the GSA draft of 22 Feb 1991 (revisions to PBS P 3430.1A).

| Space | Light level | Fixture or source asked for |
|---|---|---|
| Offices, ADP, training | 538 lx (50 fc) | "louvered parabolic for environments where personal computers are used, or acrylic prismatic lenses" |
| Conference, training | 323 lx (30 fc) | fluorescent plus dimmable incandescent |
| Internal corridors, auditoria | 215 lx (20 fc) | — |
| Public areas, support spaces | 215 lx (20 fc); maintenance areas 538 lx | lobbies, atria, corridors "may include the use of wall fixtures or combinations of wall and ceiling fixtures" |
| Toilets, locker rooms | not given | "indirect or down lighting"; no lamp type given |
| Equipment rooms, closets | not given | "Industrial type fluorescent luminaires" |
| High bay, above 4.8 m (16 ft) | not given | "high pressure sodium, non-color corrected" |
| Downlights | — | compact fluorescent |
| Exit fixtures with HID lamps | — | "a backup incandescent or tungsten halogen lamp" |
| Entrances, exits, loading docks | — | "should have lighting fixtures" |
| Fluorescent colour | — | CRI 82, 3500 K (a 1991 *new* spec: existing stock was cool white, §2.2) |
| Incandescent | — | for dimming and "special architectural effects" only |

IES 1981–87 used illuminance categories; NISTIR quotes C = 100–150–200 lx, D = 200–300–500 lx, E = 500–750–1000 lx
(Table 2). Which category each space took in the handbook was not found readable [U].

### 2.2 Lamps of the era: colour, start, failure

| Lamp | CCT / CRI | Start and restrike | How it fails | Source |
|---|---|---|---|---|
| F40T12 cool white (halophosphate) | 4230 K, Ra 64 | instant-ish; magnetic ballast hum and 120 Hz flicker | black ends, orange end glow, flicker, dead pairs | Z17; LO 10a §3, §6 |
| F40T12 warm white | 2940 K, Ra 51 | same | same | Z17; LO:S02 (in service at LBL 1991) |
| T8 triphosphor (new 1981–) | 3100 / 3500 / 4100 K, Ra 75–85 | electronic ballasts quiet | as above | LO:S07; Z01 |
| Incandescent A19, R, PAR | filament 2,000–3,300 K; general-service lamps about 2,700–2,900 K [U] | instant; no restrike | dead lamp (life 1,000–2,000 h); bulb blackens from evaporated tungsten | Z20 |
| 60 W / 100 W A-lamp | 14.5 / 17.5 lm/W, so about 870 / 1,750 lm; 75 W about 1,170 lm [D] | — | — | Z20 |
| Clear mercury vapour | 6410 K, Ra 17 (green-blue) | 4–7 min warm-up; 3–5 min restrike | dims and shifts; obsolete by MH | Z15, Z17, Z06 |
| Coated mercury vapour | 3600 K, Ra 49 | same | same | Z15, Z17 |
| Metal halide (quartz) | 4200 K, Ra 85 (range 3,000 K up) | warm-up up to 5 min; restrike 8–15 min (1982), 5–10 min cool-down (later) | colour shifts over life; output falls up to 70 % in 10,000 h; may cycle; rarely ruptures, so fixtures carry a glass guard | Z06, Z13, Z16, Z03 |
| High-pressure sodium | 2100 K, Ra 24 (orange) | restrike 2–15 min (1982); about 1 min (later teaching page) | end of life: glows, goes out, cools, restarts, in a loop; turns white-blue to red-orange before going out | Z06, Z14, Z16, Z28 |
| "White" HPS (1986–) | about 2,500–2,700 K, Ra 82–85 | — | — | Z14, Z17 |
| Compact fluorescent PL 13 W twin tube (1980–) | [U] | magnetic; slow to full output [U] | as fluorescent | LO:S24; Z08 |
| MR16 low voltage, 51 mm, 12 V | 2,700 K and up | instant | dead lamp; dichroic colour fringe [U] | Z18 |
| Sealed-beam PAR36 emergency head, 6–12 V | warm [U] | on mains failure | dead battery after years (Run! B kit) | Z21 |

1986 floorspace shares: fluorescent 76.3 %, incandescent 18.8 %, HID 6.2 % [LO:S01, P].

### 2.3 Luminance ladder for the renderer [D]

Ratios to a lensed 2×4 face seen from straight below (2,874 cd/m², 2-lamp T12, LO:S04). With L03 at 1.8 and a linear
emission scale, the right-hand column is the emission that keeps the same ratio. Bloom and the Relay lens check
(lamp_options 30_for_red, "Relay 线") decide how far up the ladder the game can go.

| Surface | Luminance | Basis | × lensed face | Emission at L03 = 1.8 |
|---|---|---|---|---|
| Lensed 2×4 face, 0° | 2,874 cd/m² | LO:S04 measured | 1.0 | 1.8 |
| Same face at 75° | 1,432 | LO:S04 | 0.50 | 0.9 |
| Bare F40T12 tube, 3,150 lm, 38 × 1,219 mm | about 6,900 | Lambertian cylinder, L = Φ / (π² d l) | 2.4 | 4.3 |
| Opal glass drum, 2 × 75 W, 330 mm dish, 55 % out [U] | about 2,400 | Φ / (π × 2πr²) | 0.8 | 1.5 |
| EXIT stencil letters, upper limit | ≤ 1,028 | UL 924-1989 via ES:S4 | ≤ 0.36 | ≤ 0.64 |
| Diffusing globe, 100 W, 110 mm [U] | about 14,600 | Φ / (π × 4πr²) | 5.1 | 9 |
| Frosted 75 W A19, 60 mm | about 33,000 | same | 11 | 21 |
| Parabolic louver at 75° | 230 | LO:S04 | 0.08 | 0.14 |

Read: a bare tube or bare bulb is several times brighter than any lens. In storage rooms that is the period look,
and it will bloom. A drum is about as bright as a troffer face; an EXIT is a third of one.

### 2.4 Code floors that set the dark limit

- **Exit illumination.** UBC 1991 §3313(a): exits lit at ≥ 10.8 lx (1 fc) at the floor whenever occupied; emergency
  source where the exit system serves ≥ 100 people (§3313(b)) [SW:S1, SW:S33, P]. NFPA 101-1988 §5-8: 1 fc at the
  floor of the egress path [ES:S3, P]. Today's NFPA wording (90 min, 0.1 fc minimum, 40:1) is [L].
- **EXIT signs, 1989–90.** Incandescent (two 20–25 W), fluorescent, electroluminescent or tritium; stencil faces;
  152 mm letters, 19 mm strokes [ES:S1, ES:S4, ES:S6, P]. LED signs are not in NIST's 1989–90 test set, so keep them
  out [D].
- **Working space at electrical gear.** Required illumination at switchboards and panelboards "since 1965" [Z11, L];
  the rule sat in NEC 110-16 until it moved to 110-26 in 1999 [Z12, L]. A real electrical room is never unlit.
- **Wet and damp.** Fixtures in wet locations marked "Suitable for Wet Locations", damp "Wet" or "Damp" (NEC 410-4(a),
  edition of the quoted text not dated) [Z26, L]. The tub-and-shower zone rule is a later addition [Z26, L].

---

## 3. Zone by zone

Layer stacks read from the room upward. "Below" is the view from under the fixture; "grazing" is 70–85° from the
vertical, the view down a room.

### 3.1 Title corridor and start rooms (stream Lobby)

**What it is in a real building.** The game's start rooms are 11.5 × 12 m rooms on a 2×4 lay-in grid at 2.9 m, the
first rooms past the station. In a real 1990 tenant building that is a lay-in office floor, not a marble entrance lobby.
A real entrance lobby (USCG Building 877, plan 1986, rehab drawings 1988: lobby first, security office off it
[station/60, P]) is where the GSA draft asks for "wall fixtures or combinations of wall and ceiling fixtures" [Z01].

**Period fixture.** The lensed 2×4 troffer (LO 10a family 1) or its air-handling variant (family 8, L03). The 1986
drawing for Building 877 puts six battery units in the lobby, two "above exit light, pickup power from same"
[SW:S6, P].

**Where L03 fits.** Fully. The inventory's rule holds: the stream lamp must be the map's L03, or the start door shows
two lamps. The difference between the stream and the map can only be *state* (clean, all tubes in) and never *type*.

**What it does to the room.** As lamp_options measured: a row of lensed faces that stays lit to the far wall; 12 per
room at 3.6 × 2.85 m centres (10.3 m² a lamp; the map 9 m²) is sparser than the period 8 × 8 ft layout (2.44 m centres,
5.9 m² a lamp) [LO:S03, D]; the game makes up for it with brighter lamps, not more of them.

### 3.2 Level 0 rooms beyond the troffer (waiting rooms, lounges, exit approaches)

**Period evidence.**
- The same troffers everywhere on a lay-in floor [LO:S02, LO:S03].
- Lounges and snack bars: a federal HQ in 1993 had "sixty-four 75-watt, incandescent, recessed 'canned' lights" on a
  lay-in ceiling in Wing D Rooms 68 and 70, a lounge and a snack bar, put in by an earlier retrofit [Z02, P].
- Conference and training rooms: fluorescent plus dimmable incandescent (GSA 1991) [Z01, P].
- Compact fluorescent downlights were new: a PL 13 W twin-tube wall-wash patented in 1987 [Z08, P]; a CFL downlight
  family dated 1986 in a maker's timeline [LO:S35, snippet]; the 1991 GSA draft asks for CFL in downlights [Z01].

**Recessed incandescent can (L10 in the study).** Aperture 102, 127 or 152 mm (4, 5, 6 in) [Z19, L]. Layers: tile,
white steel trim ring (width [U], about 13–19 mm), black or white ribbed baffle or a specular cone, lamp set back
inside (A19 60 mm, R30/R40 95/127 mm, PAR38 121 mm [D from the eighths-of-an-inch naming]).
- Below: the lamp face, very bright (frosted 75 W about 33,000 cd/m² [D]), inside dark rings.
- Grazing: the trim ring and the dark top of the baffle only; the lamp is hidden. A ceiling of cans reads as a ceiling
  of black dots (lamp_options L10).
- Light: hard-edged floor pools, scallops on walls within about a metre, the ceiling itself unlit, so the upper room is
  darker than under troffers. Warm, about 2,700–2,900 K [U].
- Ageing: dead lamps first (§0 point 2); mixed lamp types after relamping (an R lamp beside an A lamp shows a different
  beam) [U]; a darkened ring of tile round hot cans (heat-driven dust, LO:S34) [V].

**Where L03 fits.** As the base ceiling of every Level 0 room. Cans belong only in rooms whose function in 1990 was
"lounge, snack bar, reception": a waiting module is the one place they are period-correct in Level 0.

### 3.3 Low zones (2.4 m), including `Low_Storage_2x3`

**Period evidence.**
- **8 ft was the normal office ceiling.** A 1998 article on pendant indirect lighting says the fixtures need "roughly
  two extra feet per floor" over "traditional eight-foot ceiling offices" [Z10, L]. The inventory's guess (8 ft =
  back of house) is only half right: in a speculative building of the 1970s–80s an 8 ft lay-in floor *is* the office.
- Corridors in an older federal HQ (1953 stock, still 95 % original in 1993): "Type B", two F40T12 CW lamps, magnetic
  ballast, 96 W, bracket-mounted about 8 ft (2.44 m) above the floor, 239 of them, "primarily in the corridors"
  [Z02, P].
- 2×4 and 1×4 wraparounds and 1×4 lensed units were in NBS's 1987 test plan as normal office stock [LO:B02, P].
  Shipments in 1998: 2×4 74 %, 2×2 16 %, 1×4 8 % of recessed; wraparounds 9 % of fluorescent [LO:S06, L].

**Wraparound, 4 ft, 2-lamp** (`Kit_WrapFixture_4ft`, in main since Run! B: 1,220 × 250 × 80 mm; steel channel, end
caps, prismatic acrylic wrap).
- Layers: tile or slab, steel channel and canopy, ballast inside the channel, two lampholders, two F40T12, curved
  prismatic wrap lens, two end caps.
- Below: a long luminous bottom with two soft tube bands.
- Grazing: the *sides* of the wrap glow; the fixture is the brightest thing down a corridor and lights the ceiling round
  it, a pale halo on the tile that no troffer gives.
- Light: more on the upper walls and ceiling than a troffer, softer shadows, less cave.
- Ageing: yellowed lens (polystyrene faster than acrylic, LO:S33 [V]); dead insects lying in the bottom curve [U]; a
  black-ended or dead tube visible through the lens; a missing or cracked end cap [U].

**At 2.4 m** any fixture sits 0.5 m closer to the eye than at 2.9 m: the face fills more of the view and the floor
pool is tighter. For a lensed troffer that is still the period office look.

**Where L03 fits.** In every Low *room* (the 8 ft office). Not in a Low service corridor or storage room, where the
period answer is a wraparound or a strip.

### 3.4 Tall halls (5.4 m, about 18 ft)

**What a real building had at that height.** No dated example of a 2×4 lay-in grid at 18 ft was found. What the sources
do show:
- **GSA 1991:** above 4.8 m (16 ft), "high pressure sodium, non-color corrected"; HID for high bay; no metal halide in
  office spaces yet [Z01, P]. Our hall is 5.4 m, over that line.
- **Recessed HID downlights, 1981 and 1988 catalogs:** mercury and metal-halide downlights with "low-brightness
  apertures"; Alzak reflectors in specular clear or specular gold; one-piece multi-groove baffles in matte black; a glass
  lamp guard standard on metal halide; damp-listed ballast; 16-gauge (about 1.5 mm) steel frame; "auxiliary lighting"
  on most units [Z03, Z04, P]. The 1981 text says HID downlights reveal form, texture and "sparkle" more than flat
  fluorescent light [Z04].
- **HID indirect uplight on walls:** a 1980 patent for "an office area or the like", HPS 250 or 400 W, sending "90 to
  100%" of its light to the ceiling and upper walls [Z07, P].
- **Pendant indirect fluorescent:** around since the late 1980s but only catching on in general offices by 1998, hung
  305–457 mm (12–18 in) below the ceiling [Z10, L].
- **Incandescent cans in a 12–14 ft auditorium** (64 cans, plus 2×4 lay-ins and strips) in the 1993 federal HQ
  [Z02, P].

**Recessed metal-halide downlight with standby lamp.**
- Layers: tile or gypsum, trim flange, matte black grooved baffle or specular cone, glass lamp guard, the lamp (a
  coated ellipsoid or a PAR38 shape [U]), above it the standby quartz lamp [Z03 "auxiliary lighting"; Z13].
- Below: a small blinding core inside a bright specular cone.
- Grazing: the cone mirrors the dark ceiling, so the aperture reads black; gold cones read amber [U].
- Light: hard, cool (4,200 K) pools on the floor; between pools, darker; column shadows sharp and long. Lamp to lamp,
  slight pink or green differences as they age [Z13].
- Ageing and failure: warm-up up to 5 min with the colour drifting to white; a dip puts it out for 8–15 min, while the
  standby quartz lamp shows a smaller warm pool; at end of life it cycles [Z06, Z13, Z16].

**HID indirect uplight on the columns** (period form: wall-mounted [Z07]; column mounting is my inference [U]).
- Layers from below: the dark underside of a metal box on each column face at about 2.4–3 m [U]; the ceiling above it
  lit in a broad wash.
- Grazing: housings are silhouettes against a bright ceiling.
- Light: the ceiling becomes the light source; floor light is even and shadows soft. HPS makes it all orange (2,100 K,
  Ra 24); metal halide makes it white.
- Ageing: one unit out leaves a dark patch of ceiling over its column; HPS cycling at end of life is a slow on/off
  loop of minutes [Z14, Z16].

**Pendant HID low-bay (HPS, per GSA 1991).**
- Layers: hook or stem from the slab, ballast box, aluminium or prismatic-glass bell reflector [U], the lamp.
- Below: an orange core; grazing: the bell's outside, dark if aluminium, glowing if prismatic glass [U].
- Light: orange, hard; reads as warehouse, not office.

**Where L03 fits.** Only as the cheap fallback: one troffer per cell at 5.4 m keeps the lamp logic untouched but has
no period example and the faces shrink to slots from the floor. The study's L10 can also stands as a cheap stand-in for
a downlight.

### 3.5 Security station

**Period evidence.** The guard post sits at the entrance lobby; the 1986/1988 Building 877 drawings put a security
office off the lobby [station/60, P]. A 1985 security console design exists (filed 2 Dec 1985) [Z09, P]; its text names
no built-in light. The station decision (station-look-v1, A13) is the Level 0 ceiling and troffers plus the TV's own
light, with fixed lamp modes.

**Optional task light.** A balanced-arm desk lamp is a 1930s form (Anglepoise 1935 caption; patents 1934–35) and an
everyday object by 1986 (the Pixar short of that year stars two) [Z22, P/L]. Incandescent, warm, a small hard pool on
the counter, the lamp head itself bright at close range. Under-shelf fluorescent task lights in systems furniture:
no dated source found [U].

**Where L03 fits.** Fully; it is decided. A task lamp is a *dressing* light (station layer), not a map fixture, and must
obey the station rules: TV corner Dim/Off, key cell Off, the TV alone leads the eye (G5).

### 3.6 Stairwell X (safe zone, decided)

Decided 10-22 (stairwell A): `Kit_WrapFixture_4ft` on the slab + `Kit_EmergencyTwinHead` (cabinet 338 × 191 × 114 mm,
PAR36-style heads, 114 mm [D], red AC pilot). Research basis in `stairwell/01_research.md` §1.6: normal light a 2-lamp
wraparound at each landing *or a guarded incandescent or vapour-tight globe in older and service stairs*; ≥ 1 fc by
UBC 3313(a).

New evidence: the 1953 federal HQ lit its stairs and toilets with **round surface incandescent drums** (2 × 75 W: 194
units; 2 × 50 W: 56; 1 × 100 W: 23), still in place in 1993 [Z02, P].

**Round surface drum.** Layers: painted slab, round steel canopy, lampholders (1–3), A-lamps, glass dish (opal or
frosted, diameter [U], about 300–380 mm), centre nut or finial.
- Below: an evenly glowing dish with one or two hot spots over the lamps and a dark centre nut.
- Grazing: a shallow lit lens under a dark canopy.
- Light: a halo on the ceiling round it and soft light (dish about 2,400 cd/m² [D]).
- Ageing: dead insects as dark specks in the dish; one of two lamps dead leaves a lopsided glow; yellowed glass;
  heat-browned paint round the canopy [U].

**Where L03 fits.** Not at all (no grid). Keep stairwell A. Only the walkable stair (two landings, not built) could use
a drum or guarded globe as a second fixture type. BSS 120 warns that any change of light between floors is read
strongly, so whatever is chosen stays the same on every floor [SW:S3].

### 3.7 EXIT approach and vestibule

**Period evidence.** The 1986 drawing puts a twin-head battery unit directly above an EXIT sign, on its circuit
[SW:S6, P]. EXIT signs were stencil-faced, with two incandescent lamps behind a red diffuser [ES:S1, ES:S6, P]. The
federal HQ had 69 EXIT fixtures with one 25 W lamp each, and a 100 W incandescent outlet box at each exterior entrance
[Z02, P]. GSA 1991 asks for a backup incandescent or halogen lamp in any exit fixture lit by HID [Z01, P].

**Stencil EXIT (two lamps) + twin-head above.**
- Layers of the sign: housing, 0.9 mm steel stencil plate, red diffuser 6 mm behind, two 20 W lamps; two hot spots
  show through the letters [exit_sign/10_spec].
- Below: the housing underside; grazing: the face edge-on, a red line.
- Light: very little; red spill on the wall and door head; letters ≤ 1,028 cd/m² [ES:S4].
- Ageing: one of two lamps dead, so one half of the word is dim, a real and common two-lamp fault [D]. Its read
  must stay unambiguous (exit_sign rules).
- Twin-head on mains: heads dark, red pilot lit. On a blackout the heads come on (Run! B behaviour).

**Where L03 fits.** On the floor side, held Steady (the exit hold already does this). Inside X, no.

### 3.8 Restrooms (office washroom, D7; club washroom)

D7 is open; the bathroom research recommends no washroom in v1 (A). Read with that in mind. The club's washroom,
lockers, showers and pump room are the bathroom stage's: vapour-tight fixtures, chloramine corrosion [BR:S45, BR:S48].

**Period evidence.**
- GSA 1991: "For toilets and locker rooms ... indirect or down lighting", no lamp type [Z01, P].
- 1953 federal HQ: round surface incandescent drums in "stairway and toilet areas", including a 3 × 75 W type
  "primarily in toilet areas" [Z02, P].
- Mirror light: a fluorescent bar over the mirror is the familiar form; the only source found is a residential guide
  (vanity light at 6 ft 6 in, 1,981 mm) [Z24, L]. No dated commercial source [U].
- ADA (1991): mirror bottom edge ≤ 1,015 mm (40 in) [Z25, L; 1991 ADAAG wording [M]].

**Where L03 fits.** In a lay-in washroom, yes, though GSA's "down lighting" suggests cans or a 2×2 lens. A mirror light
only makes sense with D7 C (a real mirror), and it doubles itself in the mirror.

### 3.9 Storage and electrical rooms

**Period evidence.**
- GSA 1991: "Industrial type fluorescent luminaires should be used for equipment rooms or closets" [Z01, P].
- 1953 federal HQ, still in 1993: "Type F", pendant-mounted incandescent, one 300 W lamp, in "supply areas, equipment
  areas, and a few offices" (45 units); "Type J", porcelain lampholders in the washer units [Z02, P].
- 8 ft 2- and 3-lamp fixtures in labs and shops at LBL, 1991 [LO:S02, P]; strips 16 % and industrial 10 % of 1998
  shipments [LO:S06, L]. The same HQ's main fixture was a finned 1 × 8 ft surface unit, 305 × 2,438 mm, two F96T12,
  about 12 ft up, in its classrooms and offices (1,328 of them) [Z02, P]: the 8 ft strip family was not only for
  back rooms in an older building.
- Working-space light at panelboards required since 1965 [Z11, L].
- Vapour-tight "jelly jar" globes with cast guards: a long-running industrial line (a 2013 catalog: fluted sides,
  stippled bottom) [Z23, V]. No dated pre-1993 page found [U].

**Industrial strip, 4 ft or 8 ft, 2-lamp, white enamel reflector.**
- Layers: chains or surface mount, steel channel with ballast, white reflector (with or without up-light slots [U]),
  lampholders, two bare F40T12 (1,219 mm) or F96T12 (2,438 mm) tubes, 38 mm.
- Below: two bare tubes, about 6,900 cd/m² each [D], against a bright white reflector.
- Grazing: the reflector edge hides part of the tubes; still bright from far away.
- Light: hard double shadows parallel to the tubes; the ceiling above dark unless slotted.
- Ageing: everything is bare, so every fault shows: black ends, orange-glowing ends, one tube dead, one tube removed,
  dust on the reflector, a chain-hung unit hanging crooked [LO 10a §6; U for the last].

**Bare-bulb porcelain lampholder or 300 W pendant.**
- Layers: porcelain base or a cord-and-shade pendant, one A-lamp, often a pull chain [U].
- Below and grazing: one glaring point (frosted 75 W about 33,000 cd/m²; a 300 W much more) [D].
- Light: one hard shadow per object, steep falloff into dark corners, warm.
- Ageing: dead or blackened bulb [Z20].

**Vapour-tight globe with cast guard.**
- Layers: cast base on a 4 in box, gasket, fluted glass globe, guard bars, A-lamp 100–150 W.
- Below: stippled bottom disc with the guard's bars across it; grazing: fluted globe glowing, striped by bars.
- Light: a point source, so the guard throws radial bar shadows on the ceiling and walls, the strongest "basement"
  signature of the three.
- Ageing: water or insects in the globe, rusted guard [U].

**Where L03 fits.** Only in tenant storage rooms inside a Level 0 Low zone, which shared the floor's lay-in grid. A
real equipment or electrical room had no lay-in ceiling over the gear, and its light is a strip or a bare lamp.

---

## 4. Three candidate fixtures per zone, and why

"Kit" = what exists (main or the lamp study). "Logic" = what changes in the lamp contract (§5). Recommended first.

### 4.1 Title corridor and start rooms

| # | Candidate | Why | Kit / logic |
|---|---|---|---|
| A ★ | **L03, identical to the map** | The only answer that removes the start-door seam; lensed 2×4 is the period default | L03 build in progress; stream `BuildRoom` swaps mesh + lens; `lerp(.02, 2.6, level)` must drop to L03's 1.8 |
| B | L03 in a *front-of-house state*: all tubes in, no stain, cleaner lens | The rooms next to the guard were the kept-up part of the floor; same type, so no seam | L03 + a state flag; stream odds (dead 2 %, unstable 10 %) unchanged |
| C | L03 + a twin-head battery unit over the start door's EXIT | Documented 1986 placement [SW:S6]; ties the first room to the stairwell kit | `Kit_EmergencyTwinHead` exists; dark heads, pilot only; no new light |

### 4.2 Level 0 rooms beyond the troffer

| # | Candidate | Why | Kit / logic |
|---|---|---|---|
| A ★ | **L03 everywhere, plus incandescent cans only in lounge/waiting modules** | Dated: 64 × 75 W cans in a lounge and snack bar on a lay-in ceiling [Z02]; gives one warm room type without breaking the rows | L10 kit exists; light stays at the cell centre; needs a round hole (关卡设计); incandescent mode map (§5) |
| B | L03 everywhere, nothing else | Lowest cost; period-correct for open floors | none |
| C | L03 + compact fluorescent downlights in waiting modules | GSA 1991 wants CFL in downlights [Z01]; 1986–87 dated products [LO:S35, Z08]; cooler than cans, flickers like the rest | new downlight trim with a bent tube; same hole as A |

### 4.3 Low zones (2.4 m)

| # | Candidate | Why | Kit / logic |
|---|---|---|---|
| A ★ | **L03 in Low rooms; wraparound in Low service corridors** | 8 ft is the traditional office ceiling [Z10]; corridors had 2-lamp F40 bracket or wrap units [Z02, LO:B02] | L03 + `Kit_WrapFixture_4ft` (in main); wrap emission on its lens; needs a per-module tag "service" (关卡设计) |
| B | L03 everywhere in Low | Cheapest; still period for an 8 ft office | none |
| C | 1×4 lensed (study L09) or 2×2 lensed (L08) in Low zones | Smaller faces suit a lower, tighter room; 1×4 8 %, 2×2 16 % of 1998 shipments [LO:S06] | study meshes exist; the light stays put (0.15 m / 0.3 m offset noted in 30_for_red) |

### 4.4 Tall halls (5.4 m)

| # | Candidate | Why | Kit / logic |
|---|---|---|---|
| A ★ | **Recessed metal-halide downlight, specular cone, glass guard, standby quartz lamp** | Dated catalogs 1981 and 1988 [Z03, Z04]; HID is the period answer above 16 ft [Z01]; the standby lamp is period hardware and maps onto a dip (§5) | new trim mesh (study L10 base); round hole; HID mode map; colour 4,200 K vs Level 0's warm print |
| B | HID indirect uplight on the columns, ceiling as the source | Dated 1980 office uplight patent [Z07]; the hall's columns carry the light; no fixture face in view, the eerie part | new housing per column face; **the light moves from cell centre to columns** (map contract); 70 % of halls have columns, the other 30 % need wall units |
| C | L03 kept at 5.4 m, wider and dimmer, or L10 cans | Cheapest; lamp logic untouched | none / L10; no dated example of a lay-in grid at 18 ft |

Not proposed: HPS pendants. They are what GSA asked for in high *bays* [Z01], but orange 2,100 K would read as a
warehouse and fight the yellow print.

### 4.5 Security station

| # | Candidate | Why | Kit / logic |
|---|---|---|---|
| A ★ | **L03, unchanged from the decided station look** | Decided (station-look-v1, A13); the station is the floor's own ceiling | none |
| B | L03 + one balanced-arm incandescent task lamp on the counter, left on | Period object [Z22]; a warm pool says "someone worked here" | new prop + a small station-layer light; must stay under the TV light (G5) and outside the key cell |
| C | L03 + two incandescent cans over the counter (front-of-house) | GSA "special architectural effects" [Z01]; reads as a reception desk | L10 kit; two cells change type; breaks "same ceiling as Level 0" a little |

### 4.6 Stairwell X

| # | Candidate | Why | Kit / logic |
|---|---|---|---|
| A ★ | **Keep stairwell A: 4 ft wraparound + twin-head** | Decided 10-22; period basis SW §1.6 | in main |
| B | Round incandescent drum at each landing (walkable stair only) | Dated: 1953 drums in stairways, in use in 1993 [Z02] | new prop; incandescent mode map; same on every floor (BSS 120) |
| C | Vapour-tight guarded globe at landings (walkable stair only) | "Older and service stairs" (SW §1.6) | new prop; cage shadows need a shadowed light (budget) |

### 4.7 EXIT approach and vestibule

| # | Candidate | Why | Kit / logic |
|---|---|---|---|
| A ★ | **L03 held Steady + stencil EXIT + twin-head above the sign** | Documented 1986 placement [SW:S6]; matches the stairwell kit on the other side of the door | all kits exist; exit hold already holds the lamps |
| B | A + one lamp of the EXIT dead (half-lit word) on deeper floors | A real two-lamp fault; an ageing cue for depth | `Kit_ExitSign` emissive split per half; the word must stay legible |
| C | A recessed lensed downlight over the door in a gypsum soffit (front-door language) | "All entrances and exits ... should have lighting fixtures" [Z01] | new trim; a soffit the map does not have today |

### 4.8 Restrooms (only if D7 becomes B or C)

| # | Candidate | Why | Kit / logic |
|---|---|---|---|
| A ★ | **2×2 lensed troffer (study L08) or L03, no mirror light** | Lay-in floor; fits D7 B (no mirror) | study mesh exists; map lamps as normal |
| B | Round incandescent drum on a hard ceiling | Dated: drums in toilet areas, 1953 stock in 1993 [Z02]; ties to the stair (4.6 B) | new prop; incandescent mode map |
| C | Fluorescent bar over the mirror + L03 | The familiar form; only with D7 C (real mirror) | new prop; the bar doubles in the mirror; date [U] |

The club washroom and lockers follow the bathroom stage (vapour-tight).

### 4.9 Storage and electrical rooms

| # | Candidate | Why | Kit / logic |
|---|---|---|---|
| A ★ | **Industrial 2-lamp strip with white reflector, surface or chain-hung** | GSA 1991 for equipment rooms and closets [Z01]; 8 ft units in shops 1991 [LO:S02] | new prop; bare tubes 2.4 × the lens (bloom check); fluorescent modes as today |
| B | Bare-bulb porcelain lampholder or 300 W pendant | Dated: 300 W pendants in supply and equipment areas, porcelain lampholders, 1953 stock in 1993 [Z02] | new prop; incandescent mode map; very bright point (bloom) |
| C | Vapour-tight globe with cast guard | Long-running industrial line [Z23]; the strongest basement read (cage shadows) | new prop; needs a shadowed light for the cage; date [U] |

---

## 5. What each candidate must keep (the inventory's §4), and where it bites

- **One light per cell at the cell centre.** Every candidate keeps it except 4.4 B (column uplights), which moves the
  source. Faking a column uplight while the light stays at the cell centre would be dishonest light.
- **Lens follows the light.** The part that glows (lens, wrap, dish, globe, bulb face, cone glint, EXIT letters) is the
  one renderer that takes `_EmissionColor` × level and `_FR_LampDip`. Housings, trims, guards and reflectors stay unlit.
- **Mode map by source** (proposal for 关卡设计 and 游戏视觉; not decided):

| `ModuleLamp` | Fluorescent (L03, wrap, strip) | Incandescent (can, drum, bare bulb, globe) | Metal halide (4.4 A) |
|---|---|---|---|
| Steady | as today | as today | as today |
| Dim | old tubes | lower-watt or blackened lamp; warmer | aged, colour-shifted |
| Stutter | tube flicker | loose lamp, rarer | **no** (HID does not flicker visibly) [U]; use Dim |
| Failing | black end, orange end glow | **no** period analogue; use Dead | end-of-life cycling, minutes long [Z16] |
| Dead | dead pair | dead lamp, the common case | dead |
| `LampFx.Dip` | dips with the line | sags smoothly and goes orange | goes out; standby quartz lamp shows a warm smaller pool; returns after a long restrike (8–15 min real) [Z06]; the game must compress it |

- **Photosafety.** HID cycling and restrike are slow; nothing new above 3 changes a second.
- **Shadows.** Cage shadows (4.6 C, 4.9 C) and hard downlight pools want a shadowed light; the budget is 1 in 3 within
  9 m.
- **"Darkness is never cover."** An HID hall that goes out for long on a W1 burst would hand the player a dark room. The
  standby lamp is the period answer; its level must stay at or above Dim.
- **Phones and WebGL.** One mesh per lamp, no housing (lamp_options rule). Holes for cans and HID downlights are 关卡设计
  work in the ceiling mesh.
- **Colour.** Level 0 is (1, .96, .88); Office cool. A 4,200 K metal-halide hall and 2,800 K cans are both period, and
  each makes its room a different colour from the Level 0 bulk. That difference is the "zone fits its lamp" read Red asked
  for, and it must stay steady per zone (BSS 120's warning applies to any space the player walks through often).

---

## 6. Not confirmed

- Which IES category each space took in the 1981/1987 handbook (only the category ranges were read).
- A dated pre-1993 photo or catalog page for: a jelly-jar globe, a commercial mirror light, an under-shelf task light,
  HID column uplights in an office lobby, a 2×4 lay-in grid at 18 ft.
- Metal-halide downlight wattages, apertures and mounting heights (the 1988 catalog text is cut before its tables).
- Trim ring widths, drum and globe diameters.
- CCT of 1990 general-service incandescent lamps (filament range only).
- MIL-HDBK-1004/4 (13 Oct 1987) likely has a room-by-room luminaire table; the PDF is scanned images and was not read.
- The exact 1987/1990 NEC wording for working-space illumination and wet/damp fixtures.

---

## 7. Sources

How read: F = page fetched and read; PART = page read, body truncated; SNIPPET = search-result text only; NOT READ.
Nothing was downloaded. Ledgers for reused ids: `~/FrontRoomsVisualWork/lamp_options/docs/SOURCES.md` (LO),
`research/exit_sign/SOURCES.md` (ES), `research/stairwell/SOURCES.md` (SW), `research/bathroom_rooms/SOURCES.md` (BR),
`research/station/60_first_room_research.md` §2 and §9 (station/60).

| Id | Source | Date | How | URL |
|---|---|---|---|---|
| Z01 | Treado and Collins, *Lighting System Design and Evaluation in Federal Office Buildings*, NISTIR 4960, NIST for GSA; §2.2 summarises the GSA draft of 02/22/91 | Mar 1993 | F (first 100,000 characters) | https://archive.org/stream/lightingsystemde4960trea/lightingsystemde4960trea_djvu.txt |
| Z02 | Energy Engineering Analysis Program, Fort Bliss HQ Building, Lighting Retrofit, DTIC ADA330306 (= LO:S09), re-read for fixture types by space | Feb 1993, building 1953 | F (first 100,000 characters) | https://archive.org/stream/DTIC_ADA330306/DTIC_ADA330306_djvu.txt |
| Z03 | Lightolier, Calculite HID Downlighting Catalog (= LO:S23) | 1988 | PART | https://www.scribd.com/document/349164312/Lightolier-Calculite-HID-Downlighting-Catalog-1988 |
| Z04 | Lightolier, Calculite HID Downlighting Catalog (= LO:S23) | 1981 | PART | https://www.scribd.com/document/405682866/Lightolier-Calculite-HID-Downlighting-Catalog-1981 |
| Z05 | Lightolier, Lytecaster Downlights Catalog (= LO:S22): contents list General, Wall Washing, Accent, Low Voltage, Cylinder Downlights, H.I.D. | 1988 | PART (contents only) | https://www.scribd.com/document/349919563/Lightolier-Lytecaster-Downlights-Catalog-1988 |
| Z06 | US 4,398,123, High pressure discharge lamp, Mitsubishi Electric; hot restrike: mercury 3–5 min, metal halide 8–15 min, HPS 2–15 min | filed 15 Dec 1982 | F | https://patents.google.com/patent/US4398123A |
| Z07 | US 4,349,866, Light reflection system with asymmetric reflector assembly, Molnar, General Signal; HID (HPS 250/400 W) indirect wall unit for "an office area or the like" | filed 27 May 1980 | F | https://patents.google.com/patent/US4349866 |
| Z08 | US 4,748,543, Hidden source fluorescent light wash fixture, Swarens; two PL 13 W lamps | filed 29 Jun 1987 | F | https://patents.google.com/patent/US4748543 |
| Z09 | US D298,530, Control console for electronic security system, Mosler | filed 2 Dec 1985 | F (text) | https://patents.google.com/patent/USD298530S/en |
| Z10 | Simpson, "New linear lighting brightens work space", Seattle Daily Journal of Commerce, design '98 | 1998 | F | https://www.djc.com/special/design98/10047022.html |
| Z11 | Stallcup, "Access to Electrical Equipment", EC&M | 1 Oct 2001 | F | https://www.ecmweb.com/content/article/20888715/access-to-electrical-equipment |
| Z12 | EC&M / ECmag working-space history columns; Mike Holt forum (110-16 to 110-26 in 1999) | various | SNIPPET (ECmag page 403) | https://www.ecmag.com/magazine/articles/article-detail/codes-standards-room-breathe-history-working-space-nec-part-2 ; https://forums.mikeholt.com/threads/nec-110-26-history.121781/post-1890345 |
| Z13 | Wikipedia, Metal-halide lamp | read 2026-10-31 | F | https://en.wikipedia.org/wiki/Metal-halide_lamp |
| Z14 | Wikipedia, Sodium-vapor lamp | read 2026-10-31 | F | https://en.wikipedia.org/wiki/Sodium-vapor_lamp |
| Z15 | Wikipedia, Mercury-vapor lamp | read 2026-10-31 | F | https://en.wikipedia.org/wiki/Mercury-vapor_lamp |
| Z16 | Wikipedia, High-intensity discharge lamp | read 2026-10-31 | F | https://en.wikipedia.org/wiki/High-intensity_discharge_lamp |
| Z17 | Wikipedia, Color rendering index (table of sources) | read 2026-10-31 | F | https://en.wikipedia.org/wiki/Color_rendering_index |
| Z18 | Wikipedia, Multifaceted reflector | read 2026-10-31 | F | https://en.wikipedia.org/wiki/Multifaceted_reflector |
| Z19 | Wikipedia, Recessed light (= LO:B20) | read 2026-10-31 | F | https://en.wikipedia.org/wiki/Recessed_light |
| Z20 | Wikipedia, Incandescent light bulb | read 2026-10-31 | F (first 100,000 characters) | https://en.wikipedia.org/wiki/Incandescent_light_bulb |
| Z21 | Wikipedia, Emergency light | read 2026-10-31 | F | https://en.wikipedia.org/wiki/Emergency_light |
| Z22 | Wikipedia, Balanced-arm lamp | read 2026-10-31 | F | https://en.wikipedia.org/wiki/Balanced-arm_lamp |
| Z23 | Crouse-Hinds VAPORGARD catalog excerpt (globes fluted, stippled bottom), hosted by a distributor | 2013 | SNIPPET | https://elliottelectric.com/Media/P21-CRS |
| Z24 | Lighting Research Center, builder's guide, ceiling and wall luminaires (residential; vanity light at 6 ft 6 in) | n.d. | SNIPPET | https://www.lrc.rpi.edu/programs/lightingTransformation/residentialLighting/buildersGuide/luminairesCeilWall.asp |
| Z25 | WBDG / Access Board, ABA toilet room quick guide (mirror bottom ≤ 40 in) | n.d. | SNIPPET | https://dosobo-access.wbdg.org/aba-chapters/figure/603-1-toilet-room-quick-guide/ |
| Z26 | IAEI, NEC Article 410; Mike Holt forum quotes of 410-4(a) | n.d. | SNIPPET | https://iaeimagazine.org/electrical-inspections/nec-article-410-luminaires-lampholders-and-lamps |
| Z27 | MIL-HDBK-1004/4, Electrical Utilization Systems (supersedes NAVFAC DM-4.04) | 13 Oct 1987 | NOT READ (scanned images) | https://everyspec.com/MIL-HDBK/MIL-HDBK-1000-1299/MIL-HDBK-1004-4_24180/ |
| Z28 | Penn State EGEE 102, 8.6 High-intensity discharge (HPS restrike about 1 min) | n.d. | SNIPPET | https://courses.ems.psu.edu/egee102/node/2366 |

Reused ids cited above: LO:S01, S02, S03, S04, S06, S07, S24, S33, S34, S35, B02; ES:S1, S3, S4, S6; SW:S1, S3, S6,
S33; BR:S45, S48. Wanted media: `media_candidates.md` in this folder, section "10_other".
