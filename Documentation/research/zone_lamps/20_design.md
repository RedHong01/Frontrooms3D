# zone_lamps · 20 · Design: fixture options per zone, built and rendered

游戏视觉, zone-lamps workflow, design + build stage, 2026-10-31. **Options only: nothing landed in main.** Main was read
only (no Unity on main, no git; the dataless check on `Assets/`, `Documentation/` found none). Nothing was downloaded.
Everything was built and rendered in two private clones, `~/FrontRoomsVisualWork/proj_zonelamps` (mkclone from main
4cfff50c + a 6000.6.3f1 Library from `proj_unlock` by `cp -Rc`) and `proj_zonelamps_b` (an APFS copy of it for a second
render queue; a Unity project opens once at a time). At most two Unity jobs ran on the visual line. Nothing made a
sound (rule 24: `FrontRoomsCloneOnlySilence`, batch mode, no `-audioCapture`). The renders are deliverables for Red's
choice, not tests.

Red, 2026-10-31 (游戏视觉 chat): "目前不同区域内的灯的样式应该是符合对应区域的，我需要你对年代进行调研，然后思考比如说泳池房间（在这里泳池房间指的是后室概念里的特定泳池level，以及那个年代的泳池会使用的设施）包括run level以及office level都是，都需要对应的调研以及灯具的revise".

**Built on.** `00_inventory.md` (zones, the one-lamp-everywhere finding, the lamp logic every map fixture keeps),
`10_office.md`, `10_pool.md`, `10_run.md`, `10_other.md` (period evidence; their source ids O.., PL.., R.., Z.. are used
here unchanged), and the 10-option lamp study `~/FrontRoomsVisualWork/lamp_options` (its generator, texture painter,
harness, kits L06 / L07 / L09 and its trim sheet are reused, not redone). Level 0 keeps **L03** (Red 10-31 12:3x, landed
12:51 as `FrontRoomsTrofferKit`, lens 1.8); every "today" render below is main's L03 as landed.

---

## 0. Short answer: one recommendation per zone

| Zone | Recommended | Why in one line | Main cost | Renders |
|---|---|---|---|---|
| **Office bullpen** | **O-A · parabolic louver 2'×2', 9 cells, U-tubes** (lamp study L07) | GSA's 1991 "standard fixture" for computer floors (O12), sits in the Office's 2'×2' print with no half-tile fault, and the change of fixture is the refit cue between Level 0 and the Office | a ceiling hole per lamp (关卡设计's ceiling mesh), lamp state reads only within about one cell | `renders/office/L07/` |
| **Pool hall, overhead** | **P1-A · indirect metal-halide wall boxes** (new kit `ZL_HIDUP`) | the period device that makes the Poolrooms' "light from nowhere" with real 1990 hardware (PL18, PL19): the ceiling is the lamp, the water carries no fixture images | a pool rig (8 boxes, 16 lights), a bright-ceiling patch on each box's lens renderer | `renders/pool/P1A/` (STAND-IN ROOM) |
| **Pool, water side** | **P2-A · wet-niche 300 W at 3 m centres** (new kit `ZL_NICHE`) + **caustics** | the 1975 school guide's exact rule (PL08), one lamp per map cell on each long wall; caustics on the basin floor (from the overheads) and on the ceiling (from the underwater lamps) | 12 lights on the basin's own rendering layer, 2 additive caustic materials, 0 extra lights for caustics | `renders/pool/P2A/` |
| **Run! armed** | **R-A · keep the landed wrap, revised** (R1 emission variants + R2 face 1.8) | the Run! B kit is already period-right (R01, R02); what was off was the too-clean map and a face dimmer than L03 | 4 small materials + 4 emission maps (2.7 MiB), one constant | `renders/run/RA/` |
| **Run! tripped** | **T-A · keep the landed twin-heads** | dated 1984/1986 (R04, R05), decided by Red 10-21; T-C is the period answer kept for sanctuaries | none | `renders/run/TA/` |
| **Tall hall (5.4 m)** | **T-A · recessed metal-halide downlight**, 10 in specular cone, coated lamp (new kit `ZL_MHDL`) | HID above 16 ft is GSA 1991 practice (Z01); recessed MH downlights with specular cones are in 1981 and 1988 catalogs (Z03, Z04); the hall reads as dark holes over bright floor | a round hole per lamp, an HID mode map (no stutter; a dip puts it out) | `renders/tall/MHDL/` |
| **Low (2.4 m)** | **Lo-A · keep L03**, and **Lo-C · the 4 ft wrap only in Low service / storage modules** | 8 ft was the traditional office ceiling (Z10), so L03 stays; the wrap marks back-of-house where a module says so (Z02, LO:B02) | a module tag "service" (关卡设计); the wrap kit is in main | `renders/low/base/`, `renders/low/WRAP/` |
| Title corridor, start rooms, station, Level 0 Standard | **L03** (landed / decided) | the start-door seam and the station look are decided | none | not re-rendered |
| Stairwell X | **stairwell A** (decided) + R2's face level | the same wrap as Run!: one wrap face for both | one constant | not re-rendered |

Render root: `~/FrontRoomsVisualWork/zone_lamps/renders/` (one folder per zone and option; contact sheets in
`renders/_sheets/`). Every option of a zone shares its room, cameras, exposure and ambient; only the fixtures (and, where
the fixture family needs it, its own lights) change. Section 8 lists every file.

---

## 1. Rules every option keeps (the inventory's §4)

- **One lens renderer per lamp carries every glowing part** (lens, louver blades, reflector, tubes, lamp, and the lit
  ceiling patch of an indirect fixture as submesh 1 on an additive material). The map's `TickFixtures` and the Run!
  rig drive it through the existing `_EmissionColor` block, so flicker, dim, dead, the W1 warning bursts and the C22
  blackout reach all of it. Housings are unlit, shadows off, no colliders, LOD 4 m / 12 m, culled at 46 m.
- **The lights are the game's.** Map zones keep the map's Light at the cell (position, colour, intensity, cone, the
  1-in-3 shadow rule, the 16 m radius). Stand-in rigs (pool, Run!) define their own lights per fixture family, the same
  for every option of that family's comparison where the family allows it.
- **Phones:** one mesh per lamp on the lens material (frame in emission-free swatches), no housing. Kits with a
  ceiling patch keep it on phones (one more draw); WebGL takes the phone mesh.
- **Desktop hero LOD0 holds at 0.3 m:** every new kit was rendered from 0.35 m (`p4_hero_035m`), at 1 px/mm on the
  trim sheet and geometry for every part the eye meets at that range (screws, latches, guard wires, gaskets, fins).
- **No text, label, brand or logo** on any part or map. No copy of any creator's pool images (10_pool §6 guard).

---

## 2. Office (Office zones, `Office_Bullpen_4x4`, 2'×2' print, 2.9 m)

Today the map Office hangs L03 with the cool lens (`Troffer_Lens_Cool`, V5 light lead); the "parabolic louver" in the
code never existed (`00_inventory` §0.3). Renders: `renders/office/<option>/`, same Office room (cell 91, -54 of seed
516574485, ten Office lamps in the eye view), the map's lights unchanged.

| | O-A · Parabolic 2'×2', 9 cells (L07) ★ | O-B · Parabolic 2'×4', 18 cells (L06) | O-C · Lensed-indirect pendant (`ZL_PEND`, new) |
|---|---|---|---|
| Period | 9-cell U-tube patent filed 1988 (S25); GSA "standard fixture" 22 Feb 1991, louvers 3–4 in (O12); IES RP-24 1989 (S32) | 18-cell three-lamp patent filed Nov 1988 (S25, US4907143); 3 in louver tested 1987 and 1991 (B02, S02) | lensed-indirect patents 1979/1984, issued 1987 (O02, O03); Cornell field study 1989–90: 75 % preferred it to parabolics (O09) |
| What the player sees | from below: rim, blades, curved semi-specular faces, two U-tubes; past one cell a dark grate; a bright cone on the desks and a dim ceiling ("cave", B03) | the same as O-A in 2'×4' bays; continuous rows of dark grids | thin 8 ft bodies hung at 2.30 m (bottom), a bright even ceiling with a soft patch over each; almost no shadows; "cloudy day" (O16) |
| Lamp logic | all of it on the louver's lens renderer; a dead or stuttering lamp reads only within about 2.8 m, the floor shows the flicker | as O-A | lens = side lens strips + top lens + the ceiling patch (submesh 1, `ZoneLamps/GlowAdd`): a dead lamp leaves a dark patch of ceiling, which reads from across the room |
| Tris (lens / housing L0 / L1 / L2 / phone) | 460 / 208 / 136 / 40 / 58 | 340 / 212 / 152 / 40 / 70 | 8 / 776 / 328 / 96 / 20 |
| Textures (own, desktop = iOS) | 3.33 MiB + shared trim 4.00 | 6.67 MiB + trim | 1.67 MiB + trim; the glow has no texture |
| Draws per lamp, desktop / phone | 2 / 1 (the hole goes into the ceiling mesh) | 2 / 1 | 3 / 2 |
| Needs | a 2'×2' hole per lamp (关卡设计), 0.30 m off the Light along Z (L07 rule) | a 2'×4' hole; the 2×4 spans two 2'×2' tiles | headroom: the body bottom at 2.30 m, above the 2.03 m clause; no collider over the chase lane; the glow additive shader would be new in main |
| Risk | the Office reads darker from far away (by design); bloom re-check on the bare tubes | as O-A, and the half-tile fault of a 2×4 in a 2×2 print | the most new work; pendants hang into every view; GlassRT does not see the glow (transparent) |

**Recommendation: O-A.** It is the dated computer-floor fixture, it fits the print, and against L03 in Level 0 it makes
the Office a different floor at a glance. Optional later, from the same evidence: an occupant condition layer (paper
baffles, a lamp unhooked, 48 % of fixtures modified within two years at the Cornell site, O09) on 1 in 5 lamps. O-C is
the alternative if Red wants the Office to feel soft and sourceless rather than dark-ceilinged.

Not rendered, kept for later: under-bin task lights (F5, O04) need an overhead-bin kit first; private offices and
conference rooms (O2, O3) need room roles the map does not mark yet.

---

## 3. Pool rooms (STAND-IN ROOM; no pool zone exists in the map)

**The stand-in.** Built in the clone only, at (3000, 0, 3000) in the game scene (same ambient, haze, post as the map),
labelled STAND-IN ROOM in every render note: a 14.84 × 23.84 m hall, 5.4 m to a white painted structure with three
downstand beams; 6 in glazed wall tile to 2.4 m, painted block above; unglazed 6 in deck tile; a 9 × 18 m basin in pale
glazed tile, 1.1 m shallow end sloping to 2.4 m, four dark tile lane lines with T ends; a glazed coping; two stainless
ladders; four battery twin-head units on the end walls (mains on: heads dark, red pilot); water as a transparent
premultiplied surface with ripple normals and a box-projected realtime reflection probe that only the water samples.
It is a rectangle on the grid, tiled, with water, and copies no creator's composition (10_pool §6). How much of the
genre side (water crossing doorways, tile everywhere) the real L8 takes is still Red's D1 / D8 (bathroom_rooms
20_for_red).

### 3.1 Overhead (renders `renders/pool/P1A|P1B|P1C/`, water layer fixed at P2-A)

| | P1-A · Indirect MH wall boxes ★ (`ZL_HIDUP`) | P1-B · Enclosed MH low-bays (`ZL_MHLB`) | P1-C · Sealed 8 ft fluorescent rows (`ZL_VT8`) |
|---|---|---|---|
| Period | wall boxes aimed at a white ceiling "to reflect light from the ceiling to minimize glare" (PL19); office HID uplight patent 1980 (Z07); pool use in 1990 [U] | the common tall-wet-room answer; mercury in gyms by 1975, MH from the 1960s (PL08, PL11, PL13); enclosed bowls between beams (PL24) | sealed corrosion-resistant fixtures at 30 fc (PL08, 1975); continuous "beam fixtures" the length of the pool (PL09, 1970) |
| Layout | 8 boxes, 3.4 m up on the long walls, one per 6 m bay | 8 heads over the deck edges, lens at 4.8 m | 16 units, two per bay over the deck edges, bottom at 4.6 m on stems |
| Lights (this render) | 8 up-spots (lit ceiling and upper walls) + 8 wide bounce spots from the ceiling patches, no shadows | 8 down-spots 124°, hard shadows, lamp-to-lamp MH colour scatter | 16 wide down-spots, 1 in 3 shadowed, cool white |
| What the player sees | dark bronze pods, a bright scalloped ceiling, no lamp in view, clean water | point sources and long broken streaks in the water; dark ceiling between heads | long soft rows mirrored in the water; reads "school gym" |
| Lamp logic | lens = top glass + the ceiling patch (submesh 1): a dead box is a dark bay, the period's ageing read (PL19); HID mode map (§6) | lens = reflector interior + coated lamp; dip puts it out, slow restrike (§6) | fluorescent modes as today (stutter, failing, dead pair) |
| Tris (lens / housing L0 / L1 / L2 / phone) | 4 / 588 / 108 / 66 / 10 | 832 / 2816 / 706 / 190 / 398 | 24 / 862 / 476 / 120 / 22 |
| Textures | 0.83 MiB + trim | 3.33 MiB + trim | 1.67 MiB + trim |
| Hall total, desktop | 8 × 3 draws, 16 lights (0 shadowed) | 8 × 2 draws, 8 lights (8 shadowed) | 16 × 2 draws, 16 lights (6 shadowed) |

**Recommendation: P1-A**, with P1-B's restrike as its event: the boxes are metal halide too, so a mains dip puts the
whole ceiling out for a long restrike while the battery twin-heads carry the hall (10_pool §7, PL11, PL16). It is the
one real 1990 device that makes the genre's sourceless light, and its ageing (dark bays) is a free variation.

### 3.2 Water side: underwater lamps and caustics (renders `renders/pool/P2A|P2B|P2C/`, overheads at P1-A)

| | P2-A · Wet niche 300 W, 3 m centres ★ (`ZL_NICHE`) | P2-B · Wet niche 500 W, 6 m + deep end + steps | P2-C · Dry-niche ports (`ZL_PORT`) |
|---|---|---|---|
| Period | 300 W at 10 ft centres flush in the walls (PL08, 1975); wet niche lifted to the deck on its cord (PL07, 1982); top of lens ≥ 457 mm down (PL02) | 500 W 120 V (PL02, PL09) | 24 dry-niche 500 W lamps behind glass, serviced from a tunnel (PL09, 1970) |
| Layout | 12, both long walls, lens centre 0.70 m below the water | 9: 3 per long wall, 2 on the deep end wall, 1 at the shallow-end steps | 12 at the P2-A positions |
| Fixture | stainless face ring Ø 273 with its pilot screw, black seal, PAR56 fluted face | as P2-A | 320 mm square cast frame, 12 bolts, thick glass, the lamp 30 mm behind |
| Tris (lens / housing L0 / L1 / L2 / phone) | 160 / 2222 / 528 / 48 / 48 | as P2-A | 2 / 328 / 192 / 40 / 10 |
| Lights | 12 spots on the basin's own rendering layer (bit 7), no shadows | 9 (brighter, darker gaps) | 12 (narrower) |
| Note | even glow, one lamp per map cell | the "irregular angles" read; dark gaps a guard would have flagged (BR:S40) | only worth it with a service tunnel room behind the wall (关卡设计) |

**Caustics (all three).** Two additive materials (`FrontRooms/ZoneLamps/CausticAdd`, one tileable net, two scrolled
layers, world-projected): on the basin floor driven by the overhead level (light refracted down through the surface),
and on the ceiling over the pool driven by the underwater lamps (faint with the overheads on, the room's main moving
light with them off: `p6_night`, `p7_night_ceiling`). 0 lights, 2 draws, one 512² map (0.33 MiB). Reduce Motion sets
the speed to 0 (a still net).

**Recommendation: P2-A** with both caustic layers. The lamps follow the line at once (filament lag only), are never on
emergency power, and a tripped GFCI takes one long wall out together (one branch per side, [D] from PL03).

### 3.3 Pool club rooms (not rendered; picks carried from 10_pool §8)

Locker rooms and showers: sealed 4 ft fluorescent over aisles + vapour-tight guarded globes in the shower bays (P3-B).
Entry and towel desk: L03 as Level 0 (P4-A), so there is no seam at the club door. Pump and chlorine rooms: guarded
globes on EMT (P5-A). These wait for the L8 layout (D1–D4, D8).

---

## 4. Run! (Run! B corridor rig, `RunRigLayout.Corridor1x9`, built standalone at (-3000, 0, 3000))

Renders `renders/run/<option>/`. The rig, its dress, its own post volume and its lights are main's (landed 10-31); only
the fixtures change, and the armed options keep the wrap lights.

### 4.1 Armed (normal power)

| | R-A · Landed wrap, revised ★ | R-B · Bare 4 ft strip + wire guard (`ZL_STRIP`) | R-C · Strip + white enamel industrial reflector (`ZL_INDR`) |
|---|---|---|---|
| Period | 4 ft two-lamp acrylic wraparound, federal test plan Oct 1987 (R01); wide-body 1×4 wrap specified 1993 (R02) | bare strips and wire guards in service corridors (10_run §2, F2 + F4) | strip with reflector and uplight slots (F3); GSA 1991 "industrial type fluorescent" for equipment rooms (Z01) |
| Change | R1: cathode ends dim 40–60 mm on every wrap, and three by-instance variants (a: one tube with black ends, b: one tube warmer, a spot relamp, c: dust specks in the trough and a yellowed lens); R2: wrap face 1.1 → 1.8 (L03's level; the stairwell takes the same) | the wrap is replaced; the tubes are the lens (drive 2.6: a bare T12 is about twice a wrap face, 10_run §5) | the wrap is replaced; tubes + the reflector's lit inside are the lens, the uplight slots print two dashed stripes on the soffit (submesh 1); soffit halos off |
| What it does | the same corridor, now aged and as bright as L03 | the harshest "basement" read; every dead or black end shows | a "cave": lit floor, dim soffit with slot stripes; the trip is less of a jump |
| Tris (lens / housing L0 / L1 / L2 / phone) | unchanged (kit 2216 / 384 / 112) | 64 / 2274 / 822 / 14 / 42 | 100 / 486 / 206 / 50 / 80 |
| Textures | +4 emission maps and 1 albedo, 0.67 MiB each (2.67 MiB beyond R1, which replaces the landed map) | 0.83 MiB + trim | 1.67 MiB + trim |

**Recommendation: R-A** for Run! B. R-B is the right fixture for a second Run! room type (so B does not repeat); R-C
only for a taller or longer service variant (chain-hung).

### 4.2 Tripped (mains dropped)

| | T-A · Twin-head units, as landed ★ | T-B · Remote heads, one cabinet at the entrance | T-C · One tube in every third wrap on an emergency ballast |
|---|---|---|---|
| Period | "lunch pail / bug eye" units, patent filed 1984 (R04); added to a lobby on surface EMT, 1986 (R05) | heads remote from the battery box, prior art in 1984 (R04) | emergency ballasts in a normal fixture (E3; 1990 date [U]) |
| Build | landed | each unit's cabinet hidden; the heads, yokes and feed stay on a junction-box bracket (`ZL_RPLATE`, 136 / 78 / 38 tris); one cabinet at the entrance; EMT runs along both walls | heads off; wraps 0, 3, 6 lit on one tube (`ZL_WrapLens_OneTube`) at 0.12 of a wrap's light; the rig's ambient set to its black-gap value |
| Lights | landed (8 heads) | the same 8 heads | 3 wrap lights at 12 %, heads off |
| Read | warm hard pools after cool tubes, the decided signature | the same pools; one "battery box at the door" becomes the story object | cool, dim, alive-but-not-powered; no pools |

**Recommendation: keep T-A** (decided 10-21). T-C belongs to the sanctuaries (L3: a dead circuit where only battery
units or an emergency ballast glow); T-B is a variant for a second room.

---

## 5. Other zones from the inventory

### 5.1 Tall hall (`Tall_PillarHall_6x5`, 5.4 m, about 18 ft)

Renders `renders/tall/<option>/` (cell 73, -77 of seed 516574485; 35 hall lamps in the eye view).

| | T-A · Recessed MH downlight ★ (`ZL_MHDL`) | T-B · HID uplights on columns and walls (`ZL_HIDUP`, tall variant) | T-C · L03 kept (today) |
|---|---|---|---|
| Period | catalogs 1981 and 1988: specular cones, glass lamp guard, optional standby lamp (Z03, Z04); GSA 1991 HID above 16 ft (Z01) | wall HID uplight "for an office area or the like", 1980 patent (Z07) | no dated lay-in grid at 18 ft found (10_other §3.4) |
| Fixture | 10 in aperture, white enamel flange Ø 330, Alzak cone, coated ED-28 lamp base up, retaining ring and screws, a standby quartz capsule (not driven) | the pool's box, mounted 1.8 m under the ceiling on every column face and wall the hall's lamp rays meet (41 in this hall) | landed |
| Light | the map's, unchanged | the map's cell lights kept as the ceiling's bounce; the cell lenses removed; the ceiling patches static in this study | landed |
| Tris (lens / housing L0 / L1 / L2 / phone) | 574 / 1752 / 480 / 48 / 188, plus the hole panel 160 | 4 / 588 / 108 / 66 / 10 per box | landed |
| Textures | 3.33 MiB + trim | 0.83 MiB + trim | — |
| Needs | a Ø 280 hole per lamp (关卡设计's ceiling mesh), the odd-row 0.15 m shift (L10 rule), an HID mode map (§6) | **the light moves** from the cell to the columns (map contract), the boxes need their own lamp state (the map drives only the cell lens) | none |

**Recommendation: T-A.** It keeps one light per cell at the cell, so the map's lamp logic is untouched apart from the
HID mode map, and it gives the hall its own read: dark holes overhead, bright pools below, a cooler 4,000 K colour. T-B
is the more atmospheric hall but changes the map contract.

### 5.2 Low zones (2.4 m, incl. `Low_Storage_2x3`)

Renders `renders/low/<option>/` (cell 96, -69 of seed 516574485).

| | Lo-A · L03 kept ★ | Lo-B · Lensed 1'×4', two lamps (L09) | Lo-C · 4 ft wraparound on the grid (`Kit_WrapFixture_4ft`) ★ in service modules |
|---|---|---|---|
| Period | 8 ft was the "traditional" office ceiling (Z10, 1998) | NBS 1987 L2 type (LO:B02); 8 % of 1998 shipments (LO:S06) | 2-lamp F40 wraps and bracket units in service corridors (Z02, 1993; LO:B02) |
| Tris | landed | 26 / 336 / 172 / 24 / 42 | 2216 (LOD0 on the lens renderer, 3 submeshes) / phone 112 |
| Textures | — | 3.33 MiB + trim | 0 new (in main), R1 map if R-A lands |
| Needs | none | 0.15 m off the Light along X | a module tag "service" (关卡设计); surface-mounted, no hole |

**Recommendation: Lo-A everywhere, Lo-C in modules tagged service / storage.** Lo-B is the alternative if Red wants every
Low room to read tighter.

### 5.3 No change (period answer is already in place)

- **Title corridor and start rooms:** L03 in the stream (landed 12:51), so the start door has one lamp type.
- **Station:** L03 (station-look-v1, decided). Optional, not built: one warm can over the counter (L10) as a staffed marker.
- **Stairwell X:** stairwell A (decided 10-22). Only align its wrap face with R2 (2.4 → 1.8) so Run! and the stair show the same fixture at the same level.
- **EXIT approach:** L03 held Steady + stencil EXIT + a twin-head above the sign (the 1986 placement, SW:S6); all kits exist.
- **Restrooms (D7), storage and electrical rooms:** not in the map yet; picks in 10_other §4.8–4.9 (lensed 2×2 or L03; industrial strip with reflector = `ZL_INDR`).

---

## 6. Lamp-state mapping per source (proposal for 关卡设计 and 游戏视觉; not decided)

| `ModuleLamp` / `LampFx` | Fluorescent (L03, louvers, wraps, strips, VT8, pendant) | Metal halide (MH downlight, wall boxes, low-bays) | Incandescent (underwater lamps, cans, globes) |
|---|---|---|---|
| Steady / Dim | as today | as today; Dim = an aged lamp, colour shifted | as today; Dim = warmer |
| Stutter | tube flicker | not period: use Dim | rare: use Dead |
| Failing | black end, orange end glow | end-of-life cycling, minutes long (Z16, PL12) | use Dead |
| Dead | dead pair | burned out and left (PL19) | dead lamp, the common case |
| `Dip` (W1) | dips with the line | **goes out**; the standby quartz lamp (MHDL) or the twin-heads carry the space; returns over a compressed restrike, dim and cool first (PL11, PL16, PL17) | follows the line at once |

Photosafety: HID restrike and cycling are slow; only caustics move fast (Reduce Motion stills them). "Darkness is never
cover": an HID space that goes out on a W1 burst must keep the standby lamp or the twin-heads at or above Dim.

---

## 7. What it costs to land the recommended set (estimate, not measured)

| Item | Owner | Size |
|---|---|---|
| O-A in the Office: L07 kit (in the lamp study), a 2'×2' hole per Office lamp in the ceiling mesh, `BuildFixture` takes the zone's kit | 游戏视觉 kit + 关卡设计 holes | the L03 landing again, for one more kit |
| P1-A + P2-A + caustics: a `FrontRoomsPoolRig` (like the Run! rig) owning boxes, underwater lamps, twin-heads and caustics; pool cells' map lamps Off; the HID mode map | 游戏视觉 rig + 关卡设计 contract | after D1–D4, D8; about Run! B's size |
| R-A: four materials + maps, one constant (wrap face 1.8) shared with the stairwell | 游戏视觉 | small |
| Tall T-A: MHDL kit + a round hole per hall lamp + the HID mode map | 游戏视觉 + 关卡设计 | medium |
| Low: a "service" module tag and the wrap on it | 关卡设计 tag, 游戏视觉 placement | small |
| New in main beyond meshes: the additive glow shader (only if O-C or P1-A lands) and the caustic shader (pool) | 游戏视觉 | two small shaders |

**Frame time was not measured** (Red 10-31: no tests while building). Structural costs are in each table: triangles,
draws, lights, shadowed lights, texture memory. The pool rigs are 16–28 lights in one hall, which wants the same
cull-by-viewer rule as the Run! rig; on phones the underwater lamps should become one baked pattern plus 2–3 real lights.

---

## 8. Files

**Renders** (`~/FrontRoomsVisualWork/zone_lamps/renders/<zone>/<option>/`, JPG q95, desktop 1920×1080 High, phone
2622×1206 Medium + phone meshes):

| Pose | File | What |
|---|---|---|
| Eye | `p1_eye.jpg`, `p1_eye_phone.jpg` | player's eye 1.62 m, level, the game's FOV (76° desktop, 92° phone profile), desktop and phone tier |
| Phone close | `p3_three_quarter_phone.jpg` (map zones), `p6_night_phone.jpg` (pool water) | the phone meshes, no housings |
| Under | `p2_under.jpg` | straight up under the fixture |
| Three-quarter | `p3_three_quarter.jpg` | the fixture from about 3 m (narrower FOV in the tall hall and the pool) |
| Hero | `p4_hero_035m.jpg` | 0.35 m (up to 0.55 m for the wall box) from the fixture: LOD0 |
| Off / extras | `p5_off.jpg` (map zones: the lamp `ModuleLamp.Off`), `p5_water_mirror.jpg`, `p2_niche_across.jpg`, `p3_niche_close.jpg`, `p6_night*.jpg`, `p7_night_ceiling.jpg`, `p2_mid.jpg` (tripped) | |

Contact sheets: `renders/_sheets/{office,tall,low,pool_overhead,pool_water,run_armed,run_tripped}_sheet.jpg`.
Per-run logs and shot tables: `renders/_work/<zone>_<option>_<desk|phone>/` (`log.txt`, `shots.tsv`, `stats.json`).

**Kit and tools** (`~/FrontRoomsVisualWork/zone_lamps/`): `kit/zl_build.py` (Blender 4.3 generator, reuses the lamp
study's `build_lamps.py` helpers), `kit/zl_tex.py` (texture painter, reuses `gen_lamp_textures.py`), `kit/out/`
(FBX + layouts + `zl_build_report.json`), `kit/tex/`, `kit/unity/unity_report.json` (as imported), `kit/smoke/`
(Workbench geometry checks), `render_tools/queue.sh`, `render_tools/zl_sheets.py`. In the clones:
`Assets/Resources/Props/ZoneLamps/`, `Assets/Resources/ZoneLamps/*.shader`, `Assets/Editor/ZoneLamps/`
(`FrontRoomsZoneLampsSetup.cs`, `FrontRoomsZoneLampsShots.cs`).

---

## 9. Not confirmed / honest limits

- The pool hall is a stand-in; its ambient is the game's warm Level 0 trilight, so the tile reads warmer than a real
  pool would. The real L8 room waits for Red's D1–D4, D8.
- P1-A's period use in pools in 1990 is [U] (documented in pools as found later, PL18, PL19); P1-B and P1-C are dated.
- Wall-box pods render as dark bronze; at 0.35 m the game's environment reflection shows strongly on them at grazing
  angles (no probe roughness convolution in the clone).
- T-B (tall uplights) and the pool boxes' ceiling patches are static in these renders: the map drives only the cell lens.
- Light distributions are the map's / rig's (a parabolic or an MH downlight would throw a narrower cone in reality);
  only the fixtures change, by the brief.
- The MH downlight's standby quartz lamp is modelled but not driven; the HID mode map is a proposal.
- At 0.35 m the shared trim sheet's enamel and zinc rows show their paint-wave normals as a faint wavy pattern at
  grazing angles (MH downlight flange, port frame). A landing would put these housings on the landed troffer's trim
  sheet; smoothing those two rows is a small texture fix.
- The pool stand-in reuses main's battery twin-head kit, whose legend plate is main's decided artwork (Run! B); no new
  text was drawn.
- R-C's reflector inside was toned down after the first pass (it bloomed white at the wrap's 1.8); it now sits at
  about a fifth of the tubes, as 10_run §5 gives.
- The clone's copy of the lamp study's prefabs pointed at a trim material GUID that the study clone had since
  re-made; they were re-pointed to the clone's `LampOpt_Trim` before the final office and Low renders (no visual change
  intended).
