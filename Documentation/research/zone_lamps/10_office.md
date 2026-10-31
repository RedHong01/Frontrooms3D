# zone_lamps · 10 · Office floors: what lit a US office of 1985 to 1993

游戏视觉, zone-lamps workflow, research stage (office), 2026-10-31. Research only: nothing was built, rendered,
downloaded or changed in main. Main was read only (dataless check on `Documentation/` and `Assets/Scripts/`: none).
Wanted media are listed in `media_candidates.md` (this folder), with sizes from HTTP headers only.

Red, 2026-10-31: "目前不同区域内的灯的样式应该是符合对应区域的，我需要你对年代进行调研 ... 包括run level以及office level都是，都需要对应的调研以及灯具的revise".

**Reused, not redone.** The 10-option lamp study (`~/FrontRoomsVisualWork/lamp_options/docs/`: 10a period products,
10b real looks, 20 options, 30_for_red, SOURCES) already covers the lensed 2×4, the parabolic louvers (L06, L07), the
lensed 2×2 (L08), the 1×4 (L09), the can (L10), ageing (L05) and the luminance-by-angle data. Its source numbers
(S.., B..) are used here unchanged. This file adds what the study did not cover: indirect and furniture-mounted
light, task lights, private-office and conference lighting, office corridors, the exit-way layer, the opal decision,
and the occupant-made changes that make a 1990 office ceiling look tired. New sources are O01 to O26 (section 10).
Brand names appear only as sources. No brand, trade name or label goes on any in-game object.

**Evidence tags.** [P] dated inside or close to 1985–1993. [L] later source describing the same thing. [V] vendor
listing. [D] my arithmetic from sourced numbers. [G] general knowledge, unsourced. [I] my inference. [U] unconfirmed.
Read status in section 10: F = page text read, S = search excerpt only, PART = title or contents only.

---

## 0. Short answer

1. **The VDT era split the office ceiling in two.** Installed stock in 1990 was lensed 2×4 prismatic troffers
   (10a family 1). Floors fitted out for computers from about 1985 got **parabolic louvers**: 2×4 18-cell three-lamp
   (patent filed Nov 1988, S25) or 2×2 nine-cell. GSA's draft guideline of 22 Feb 1991 calls the 2×2 parabolic with
   3–4 in (76–102 mm) louvers its "standard fixture", and allows only parabolic (where PCs are used) or 0.125 in
   (3.2 mm) prismatic acrylic (O12). IES published RP-24 for VDT offices in 1989 (S32).
2. **The best-practice answer of 1989 was indirect light.** Lensed-indirect pendants or partition-top uplights,
   patented from 1979 (O02, O03), bounced light off the ceiling. A Cornell field study at a renovated office of 1988
   (reports Oct 1989 and Sep 1990) found the parabolic group reported tired-eye and focusing problems twice as often;
   by 1990, 75 % of them preferred the indirect system (O09).
3. **Occupants changed their own ceilings.** In that study, **79 of 164 parabolic fixtures (48 %) had been modified
   by the occupants within about two years**, by disconnecting lamps, disconnecting whole fixtures (10), or hanging
   paper baffles; in windowless offices 51 of 67 (over 75 %) (O09). This is the strongest dated source for a
   "tired" office ceiling, and it is specific to the VDT office.
4. **Task light lived in the furniture.** Under-shelf fluorescent strips under overhead bins were "well known and
   preferred in modular offices" by 1983 (O04); a 1979 design hung a task-plus-uplight unit on 60 in (1524 mm) panels,
   80 fc on the desk, 10–15 fc around (O01). Under-shelf units were about 90 % of office task lights sold in 1994 (O14, S).
5. **Private offices, conference rooms, corridors and egress each had their own fixture.** GSA 1991: offices 50 fc,
   conference 30 fc with fluorescent plus dimmable incandescent, internal corridors 20 fc, compact fluorescent in
   downlights, wall fixtures in lobbies and public corridors (O12). A 1993 survey found corridors lit by 1'×4'
   surface bracket units with two F40 cool white lamps at about 8 ft (2.44 m), and one 25 W lamp per exit light (O13).
   NFPA 101-1988 asked for 1 fc (10.76 lx) on the egress floor and an exit sign within 100 ft (30.5 m) of any point
   (exit_sign research S3).
6. **The Office "opal lens" decision rests mostly on the A24 film**, a specific existing work, plus Red's target
   image. Period evidence for opal diffusers as the office default is the weakest of all the lens types (section 2).
   It was never built, and it is still listed as open in the 游戏视觉 handoff (12:08). This is Red's call; section 2
   lays it out.
7. **Office lamps of 1990:** F40T12 cool white (about 4100 K, CRI about 62) was the most used lamp; warm white (3000 K,
   CRI about 50), 34 W energy-saving tubes, and from 1981 T8 tri-phosphor at 3100/3500/4100 K (CRI 75) were all in
   service; GSA's 1991 draft asks for 3500 K CRI 82 (section 3). Mixed colours from spot relamping are period-correct.

## 1. What the project has today (Office)

- **Map Office** (`ZoneTheme.Office`, Standard 2.9 m only, 2'×2' print `Office_Ceiling`, 0.9 m columns with bulkheads,
  module `Office_Bullpen_4x4`): one lens box per 3 m cell, `Troffer_Lens_Cool` at (.90, .96, 1.0) × 6.0 with the V5
  light lead, else `Troffer_Lens` × 5.5 (00_inventory row 5).
- **L03 landed 12:51 today** (`FrontRoomsTrofferKit`, lens 1.8). Its contract to 关卡设计 drives the Office with the
  cool lens at about 1.81 under the 2'×2' print, and keeps 0.138 m from bulkheads for the 12 mm T-bar skirt
  (`~/FrontRoomsVisualWork/apply/lamp_l03_contract/README.md` l.35–37, 62). So without a change, the Office hangs the
  Level 0 air-handling troffer in cool.
- **`FrontRoomsSurfaces.OfficeLouver` returns `Troffer_Lens`.** No louver look exists in code (00_inventory §0.3).
- **Office kit** (`Assets/Scripts/Office/FrontRoomsOfficeKit.cs`): cubicle panels in three heights, desks, CRTs, chairs,
  copier, vending, water cooler, filing, interior window, piles. **No overhead storage bin and no task light.** A
  furniture task light needs a bin (or shelf) first.
- **Exit-way pieces already designed or built:** the 1990 stencil EXIT sign (research/exit_sign, red letters, two
  20 W lamps), the battery twin-head and the 4 ft wraparound (`Kit_EmergencyTwinHead`, `Kit_WrapFixture_4ft`, used by
  Run! B and stairwell A).

## 2. The opal lens decision: where it came from, and what the period says

**The trail in the docs (read only):**

| Date | File | What it says |
|---|---|---|
| 10-01 | `VISUAL_RESEARCH_LOOKDEV.md` l.54, l.122 | "2'×2' ceiling tiles with flat frosted (opal) troffer lenses — the film's fixtures and Red's target both show opal, not louvres"; Office row "flat opal lens + Office zone grade" |
| 10-02 | `research/office_and_film/02_film_shots.md` l.38, F13 | Visual check of BTS photos from the A24 film: "flat opal/frosted white, not prismatic"; GAME LANDING: flat emissive opal, 5–8 % edge falloff, 2 cm white frame |
| 10-02 | `research/office_and_film/10_synthesis.md` decision 4, §6.1 | "Troffer lens: flat frosted opal in the Office, replacing the parabolic louvre. The film (02 F13), Red's target and the BPA real-office baseline agree." Spec `Office_LensOpal`: albedo #EEEDE8, flat emission with 6–8 % falloff into a 20 mm frame band; dead #D9D6CC with two 25 mm tube shadows; 4000 K, #FFF1E0 |
| 10-02 | `OFFICE_LEVEL_FURNITURE_RESEARCH.md` l.29 (Red-approved list) | "Lighting is practical troffers with flat opal lenses at 4000 K ... the Office already uses opal" |
| 10-31 | lamp_options `20_options.md` §5, `30_for_red.md` | Asks Red whether the opal decision still stands; only L06/L07 depend on it |
| 10-31 12:08 | handoff `visual.md` l.1057 | Still listed as a Red decision. No answer recorded up to 12:51 |

**Checking its three reasons against the rules and the period:**

1. **The film (F13).** It is a specific existing work (A24's *Backrooms*, custom LED troffers built for the set,
   02_film_shots F13). The workflow rule is "no copying of specific existing works". Copying its fixture look is the
   thing that rule forbids. It is also not a 1990 fact: the film used two LED tubes per troffer (02 l.38).
2. **Red's target image** (`ref_office_target.png`). This is Red's own target, so it is a valid reason. It is not a
   period source.
3. **The BPA guide.** It is an LED layout guide revised 10/2018, and it specifies "2'x4' lensed troffers"
   (03_ip_canon l.254), not opal. It does not support opal.

**What the period says about opal or white diffusers in offices** (10a family 5): a white pebbled panel was tested in
1981 at 54.6 % efficiency, the least efficient lens in that test (S11). No dated source shows opal as an office default.
GSA's 1991 draft allows only parabolic or 0.125 in prismatic acrylic for standard fixtures (O12). The 1987 NBS test plan
lists acrylic lensed, parabolic and wraparound units, no opal (B02). So for a 1990 office, opal is the least supported
choice; a flat white panel is also the "flat rectangle" Red objected to (30_for_red).

**Question for Red (not decided here):** keep the opal Office (for the target image), or let the Office take a period
fixture from section 9 (O1). Either way, nothing copies the film.

## 3. Lamps and colour, 1985–1993

| Lamp | Size, mm | Colour, CRI | Notes | Source |
|---|---|---|---|---|
| F40T12 cool white (CW) | 1219 × 38, 40 W, 3050–3150 lm | about 4100 K, CRI about 62 | The most used halophosphate lamp (cheapest, most efficient) | S07 [P]; O18 [V]; O19 [L] |
| F40 energy-saving CW | same shape, 34 W | as CW, less light | From the 1970s; NIST tested it in 1993 | S07, B01 [P] |
| F40T12 warm white (WW) | as CW | 3000 K, CRI about 50 | "Blends with incandescent light"; in service at LBL in 1991 | S02, S07 [P]; O19 [L] |
| Daylight F40D | as CW | 6500 K | Bluish; NIST tested one in 1993 | O17, B01 [P] |
| Deluxe CW / WW | as CW | better colour, less light | Rare in offices [U] | S07 [P] |
| T8 tri-phosphor F32T8 | 1219 × 25, 32 W, 2900 lm | 3100 / 3500 / 4100 K, CRI 75 (85 option) | From 1981; GSA's 1991 draft asks 3500 K CRI 82 | S07, B01, O12 [P] |
| U-tube FB40 | 572 long, 38 dia, legs 152 apart | CW | For 2×2 troffers (10a family 6, 7) | S31, B01 |
| Compact twin tube (PL, TT) | about 150–230 long [U] | warm or neutral [U] | 1980–84; in 1985 task-light and 1986–88 downlight designs | S24, O05, S22 |
| Incandescent A-lamp | 75 W common | about 2800 K [G] | Downlights (64 × 75 W cans, 1993), exit signs (25 W) | S09, O13 [P] |

- **How CW looks:** a yellow surplus and a shortage of red and green; usual halophosphate types "make skin tones look
  green-yellowish" (O17). On film, fluorescents had a known green tinge (B51). A 1990 office film was described as "a
  greenish half-world of bad fluorescent lighting" (B29). The project's Office already gets its green-grey from a
  grade, not from tinted lights (10_synthesis decision 5). Keep it that way.
- **Ballasts:** magnetic, 60 Hz (flicker visible on dying lamps, hum); electronic ballasts from the early 1980s
  (S07, S08). A cheap core-coil ballast measured a ballast factor near 0.5 in 1993 (O12): a fixture can be half as
  bright as its neighbour with good lamps.

## 4. Office fixture families: sizes, layers, what they do to the room

Distances below use the game's eye: the lens face is about 1.3 m above the eye (10b §1). A fixture at θ from straight
down is at horizontal distance d = 1.3 m · tan θ [D]: 55° = 1.9 m, 65° = 2.8 m, 75° = 4.9 m, 85° = 15 m. In a 3 m cell
grid, that is "the lamp overhead and its neighbours", "the next ring", "across the room".

Relative face brightness uses the 2008 two-lamp test (S04): lensed T12 at 0° = 1.00.

| Angle | Lensed 2×4 | Parabolic 12-cell |
|---|---|---|
| 0° (overhead) | 1.00 | 0.72 |
| 55° (1.9 m) | 0.60 | 0.60 |
| 65° (2.8 m) | 0.46 | 0.41 |
| 75° (4.9 m) | 0.50 | 0.08 |
| 85° (15 m) | 0.49 | 0.00 |

IES RP-24 (1989) preferred maxima of 850 / 350 / 175 cd/m² at 55 / 65 / 75° (B05, via a 1995 patent) are the reason
the parabolic column falls off.

### F1 · Lensed 2×4 prismatic troffer (installed stock; L03 is this, air-handling)

- **Dates and share:** 10a family 1 and 8. The default in 1990 stock. LBL's 1991 conference room: 11 fixtures at 8 × 9 ft
  (2.44 × 2.74 m), 4-lamp delamped to 2, K-12 lens (S02).
- **Size:** outer 603 × 1213–1218, lens about 566 × 1181, 3.2 thick, cones 4.8 on 45°, housing 110–130 deep [D].
- **Seen from below:** tile, 24 mm T-bar, frame lip, steel door frame (hinges one side, latches the other), lens with
  2–4 soft tube bands, (L03: a dark return channel each long side).
- **Grazing:** the lens stays at about half its overhead brightness to the horizon; frame and T-bar shrink to lines;
  the L03 channel survives on the near side only (30_for_red).
- **Room:** broad, soft spill; walls and the upper ceiling get light; soft shadows; every lamp in a row reads as lit
  or dead from far away. This is why L03 keeps the lamp logic legible.
- **Ageing:** 10a §6 (yellowing, tar film, delamped pairs, dead pairs, black ends, mixed colours, sag).

### F2 · Parabolic louver 2×4, 18 cells, three lamps (L06)

- **Dates:** 3 in aluminium louver tested 1987 (B02) and 1991 (S02); 18-cell three-F40 semi-specular louver in a patent
  filed Nov 1988 (S25, US4907143); low-glare grid patent filed Jul 1990 (B04).
- **Size:** 603 × 1213 outer; louver 76 deep, overall about 114 deep (S02, S14); 3 × 6 cells, about 200 × 200 mm pitch
  [D]; three tubes, one under each column of cells (S25).
- **Seen from below (0–45°):** tile, T-bar, black or white reveal, louver rim, long blades, cross blades, curved
  semi-specular faces, the white or specular reflector, and a tube high in each column. The most detailed close-up of
  any office fixture (L06 renders).
- **Grazing (past about 65°, so beyond the next cell):** a dark grey grate. Blade lips catch a thin light edge; faces
  can show a rainbow sheen (S14, S15). Tubes are hidden. Past 75° (about 5 m) it is near black; a row of lit parabolics
  looks like a row of holes ("dark holes in the ceiling", B03).
- **Room:** a cone of light from "an otherwise dark ceiling" (O09); upper walls and ceiling dim (the cave effect,
  B03, S16); light scallops at the top of walls (O15); harder shadows under desks than a lens. Illuminance was set
  higher than for indirect: 50–70 fc (538–753 lx) at the Cornell site (O09).
- **Ageing:** dust on blade tops, iridescence, louver hanging on its safety cords after relamping (S25), **lamps
  disconnected and paper baffles taped or hung by occupants, whole fixtures off** (O09). See section 6.

### F3 · Parabolic louver 2×2, nine cells, U-tubes (L07)

- **Dates:** 1988 brochure (S21); nine-cell patent filed 1988 (S25); GSA "standard fixture" 22 Feb 1991, louvers 3–4 in
  (O12). Believed the majority of installed 2×2s later (S06, belief only).
- **Size:** 603 × 603; 3 × 3 cells of about 200 mm [D]; louver 76–102 deep; two FB40 U-tubes (legs 152 apart) or
  four 20 W lamps (S25); twin-tube versions 572 long (S25).
- **Seen from below:** as F2, smaller; the U bend is visible at one end, so each fixture has a direction (L07).
- **Grazing and room:** as F2. Half the bright area of a 2×4.
- **Fit:** sits in the Office's 2'×2' print with no half-tile fault (30_for_red); 0.3 m off the Light (L07).

### F4 · Lensed-indirect pendant (uplight hung below the ceiling)

- **Dates:** "Method for indirect lighting", priority 8 Jun 1979, filed 17 Mar 1986, issued 21 Apr 1987 (O02);
  lensed-indirect luminaire filed 28 Dec 1984, issued 17 Feb 1987 (O03). Cornell field study of a lensed-indirect
  system in an office renovated in mid-1988 (O09, O10). GSA 1991 suggests indirect or direct/indirect for open plans (O12).
- **Size and hang:** extruded aluminium housing cut to length; two or three fluorescent lamps side by side (O02, O03).
  Top lens of clear acrylic, flat on top (for dusting), 60° prisms 0.76 mm high on a 1.57 mm pitch, 3.3 mm thick; side
  lenses 3.2 mm with external prisms (O02). Mounted 6–7 ft (1.83–2.13 m) above the floor to the top of the housing, with
  the ceiling 2–3 ft (0.61–0.91 m) above the lens, down to 1 ft (0.30 m) (O02). Ceilings "normally about eight feet,"
  up to nine (2.44–2.74 m); partitions 5–7 ft (1.52–2.13 m) (O02). A later housing is about 3 × 9 in (76 × 229 mm),
  hung at least 12 in (305 mm) below the ceiling (O16 [L]).
- **Seen from below:** an opaque bottom wall (O03), white or dark metal, with the ceiling around it glowing. The light
  source is the ceiling, not the fixture.
- **Grazing:** a row of long thin bodies hung in mid air, each with a faint lit side lens strip, under a bright,
  even ceiling; a brighter patch on the ceiling right above each pendant if poorly spread ("a very bright spot",
  O02; "hot spots", O09).
- **Room:** almost shadowless, even, soft; ceiling brighter than walls; can feel gloomy ("the cloudy day effect",
  O16 [L]); the housings show as dark silhouettes in CRT screens (O16, O15). Illuminance set lower than parabolic:
  30–50 fc (323–538 lx) (O09). Below 9 ft ceilings the pendants become obstructive or need closer spacing (O15 [L]).
- **Ageing:** dust collects on lamps and reflectors (O02), which is why the top lens is flat; a dead pendant leaves
  a dark patch of ceiling, not a dark fixture [I].

### F5 · Furniture-mounted light: task light under the bin, uplight on the panels

- **Dates:** task-and-ambient unit for open plan offices, filed 7 Dec 1979, issued 10 Nov 1981 (O01); under-shelf task
  fixture, priority 10 Jun 1983, issued 11 Nov 1986 (O04); compact-lamp task fixture filed 22 Jan 1985 (O05);
  partition-top lensed uplight "atop a six foot partition wall of a computer terminal work station", filed 1984 (O03);
  task-ambient defined in a 1985 patent as low general light from the ceiling plus local task light (O06). Share:
  under-shelf about 90 % of office task lights sold, and about a third of US office workers in partitioned
  workstations (1994, O14 S).
- **Task-plus-uplight unit (O01):** hooks over 60 in (1524 mm) panels, 30 in (762 mm) wide; front flush with the work
  surface (711 mm high, 762 deep, 1524 long). Downward chamber: one F40 over a parabolic specular reflector with a
  linear prismatic lens. Upward chamber: two F40 with a radial batwing lens. Work surface 80 fc (861 lx), general area
  10–15 fc (108–161 lx).
- **Under-shelf strip (O04):** lengths 18–60 in (457–1524 mm); a fluorescent tube with a reflector; a perforated metal
  panel under about a third of the opening (41 % open) to aim the light; mounted with keyhole slots under the cabinet.
  Other designs used a prismatic lens (O01) or compact PL/TT lamps (O05).
- **Seen from below (standing in the aisle):** nothing. The strip sits under the bin, about 1.2–1.4 m up [U]; from a
  seated eye you see a bright lens line under a dark bin.
- **Grazing:** a thin bright line under each bin, and a bright stripe of desk and back panel below it.
- **Room:** islands of light on the desks; aisles and the ceiling stay dim if the ceiling lights are low or off
  (task-ambient). Veiling reflections and glare on the desk were the known problem ("glare is a major problem resulting
  from standard task lighting fixtures", O04). Systems furniture with fixed task lights often left task light below
  recommendation (O12).
- **Ageing:** dead or missing strips; ballast noise (O04); a desk lamp brought from home [U].

### F6 · Recessed downlights: private offices, conference rooms, lobbies (L10, and compact fluorescent)

- **Dates:** downlight catalogs 1980–88 with sections for wall washing, accent, low voltage, cylinder and HID
  (S22, O25 PART); 64 cans of 75 W in a lounge and snack bar, in use in 1993 (S09); GSA 1991 asks compact fluorescent
  in downlights, and **combined fluorescent and dimmable incandescent in conference and training rooms**, dimmable
  incandescent for computer-graphics workstations (O12).
- **Size:** apertures 4, 5, 6 in (102, 127, 152 mm); black or white baffles or a specular cone (B20).
- **Seen from below:** tile hole, trim ring, ribbed baffle or cone, the lamp face deep inside.
- **Grazing:** a small dark ring; no visible glow past about 45° [I]. A ceiling of only cans looks unlit (L10).
- **Room:** pools of warm light on floor and table, scallops on walls next to them, dark ceiling. On a dimmer, the
  conference room goes to a warm low level for slides or overheads while fluorescents are off [I, from O12's reason].
- **Ageing:** dead bulbs (incandescent fails without flicker), heat stains on the tile around the can (S34).

### F7 · Office corridor fixtures

- **Dates:** corridors in a federal building surveyed Feb 1993: 239 units of 1'×4' surface, bracket-mounted, about
  8 ft (2.44 m) up, two F40T12 CW, 96 W (O13). NBS's 1987 test plan includes 1×4 lensed (L2) and 1×4 and 2×4
  wraparounds (L5, L6) (B02). GSA 1991: internal corridors 20 fc (215 lx); lobbies, atria and public corridors use wall
  fixtures or wall-and-ceiling combinations "as an extension of the architecture" (O12).
- **1×4 lensed lay-in (L09):** about 300 × 1213 [D], two tubes, prismatic lens, along the corridor axis.
- **4 ft wraparound (the kit's `Kit_WrapFixture_4ft`):** lens 254–362 wide, 1219 long (10a family 11); the lens wraps
  the sides, so it is the one fixture that is as bright from the side as from below.
- **Grazing:** wraparounds stay bright at any angle and light the ceiling beside them; a lay-in 1×4 behaves like F1.
- **Room:** a corridor ceiling with a bright stripe or a row of boxes; side walls lit near each unit; 20 fc is under
  half the office level (O12), so office floors read darker in their corridors.
- **Ageing:** yellowed wraparound lenses (polystyrene yellows faster than acrylic, S33); insects in the trough [U].

### F8 · Exit-way lighting (a layer over every office zone)

- **Code of the time:** NFPA 101-1988 §5-8 and §5-10: 1 fc (10.76 lx) at the floor of the egress path; signs visible
  from any direction of exit access; no point more than 100 ft (30.5 m) from a visible sign; letters at least 6 in
  (152 mm), strokes 3/4 in (19 mm); every sign lit by a reliable source (exit_sign S3, via NIST Aug 1990).
  Today's NFPA 101 asks 1.5 h of emergency light, 1 fc average, 0.1 fc minimum, 40:1 max/min (O26 [L]); the 1988
  battery-duration clause is [U] here.
- **Exit signs:** 1985–86 Corps of Engineers drawings: aluminium stencil face, red diffuser, two 20 W lamps (Type 602)
  or two 20 W plus two 5 W DC (Type 604, self-contained battery) (exit_sign S6). In 1993 a federal building had one
  25 W lamp per exit light (O13). No LED sign was in NIST's 1990 sample (exit_sign research).
- **Battery twin-heads:** a 1986 drawing adds six twin-head emergency units to a lobby, "pickup feed from existing hallway
  light", two units "mount above exit light" (stairwell S6). The kit's `Kit_EmergencyTwinHead` is this.
- **Room:** with mains on: the red sign is a small accent (one of the Office's three colour accents, 10_synthesis §6.3);
  heads dark. Mains off: two hard beams per unit along the corridor floor, long shadows, everything else black.

### F9 · Opal or white diffuser (the recorded Office decision)

- **Dates:** white pebbled acrylic tested 1981 (S11); no dated office source (10a family 5).
- **Seen from below:** a white sheet in the frame, no tubes. **Grazing:** an even white rectangle, like F1 without bands.
- **Room:** as F1, about 20 % less light for the same lamps [D, 54.6 vs 67.7 % in S11].
- **Ageing:** yellowing shows most on white [U]; dead units show faint tube shadows (10_synthesis §6.1).

## 5. Layer stacks side by side (from the room upward)

| Family | 0° (under it) | 45° | 75°+ (across the room) |
|---|---|---|---|
| F1 lensed (L03) | T-bar · frame · (dark channel) · bands on prismatic lens | frame lines, lens at 0.79 | lens at 0.5, frame a line, near channel |
| F2/F3 parabolic | rim · blades · curved faces · reflector · tubes | blades, faces bright, tubes partly | dark grate, blade-lip highlights, iridescence |
| F4 indirect pendant | opaque housing underside on a glowing ceiling | side lens strip, housing | thin hung lines under a bright ceiling; hot spot above each |
| F5 under-bin task | bin underside, lens line (seated only) | hidden by bin | hidden; desk stripe visible |
| F6 downlight | ring · baffle rings · lamp face | dark ring, lit cone edge | dark dot |
| F7 wraparound | lens box with lit sides | lit sides | lit sides, ceiling glow beside it |
| F8 exit sign / twin-head | sign underside; heads | red letters, two hot spots | red letters; heads only when lit |
| F9 opal | frame · flat white | flat white | flat white at about half |

## 6. A tired 1990 office ceiling

What sources support, specific to office floors (adds to 10a §6):

| Sign | Evidence | Source |
|---|---|---|
| **Occupant-made changes to parabolics** | 79 of 164 fixtures (48 %) modified within about two years: lamps disconnected, whole fixtures disconnected (10), paper baffles; 51 of 67 in windowless offices | O09 [P] |
| Glare complaints | 905 VDT users in New York State offices: 31 % "much glare", 42 % bothered by screen glare; overhead fixtures named as a source | O11 [P] |
| Old stock under new uses | Fixtures past 15 years count as deteriorated; a 1975 install is 15 in 1990 (S03). LBL delamped more than 15 years before 1991 (S02) | [P, L] |
| Delamping waves | 4 to 2 lamps "the easiest and most economical method" (O08, 1984); people see delamped fixtures as dimmer (O08) | [P] |
| Specular reflector retrofits | A 1986 retrofit replaces each pair with one centred lamp over a mirror-anodised aluminium sheet bent into a V (reflectivity 90.6 %, specularity 86.5 %) (O07); a 1984 insert replaces a removed lamp with specular tape and white paint walls (O08). The inside of the fixture becomes a mirror with one tube and its reflections | [P] |
| Mixed generations | CW, WW, 34 W, and from the early 1990s T8 3500 K side by side; spot relamping leaves a patchwork (S20); lamps measured 4–9 % under rating (O12) | [P, L] |
| Weak ballasts | One fixture at half output beside full ones (ballast factor near 0.5, O12) | [P] |
| Tobacco film | 27 % of private businesses had a smoking policy in 1985 (O20 S); tar on lenses (S25) | [P] |
| Renovation patchwork | Parabolic bays from a tenant fit-out next to lensed bays (10a §2: "the lamp change is the cue") | [I] |
| Dark ceilings over screens | A cone of light from "an otherwise dark ceiling" (O09); dimmed walls (B03) | [P, L] |

**Not supported, do not present as documented:** tape on lenses, removed lenses as a habit, desk lamps brought from
home, dead insects (consumer pages only, 10a S47).

## 7. What each family does with the game's lamp logic [I]

The rules are in 00_inventory §4: one renderer per lamp carries the glow and dies with it; dead, stutter, dip and warn
must stay readable; housings never glow; flash limiter; budget radii.

- **F1/L03:** fits everything as built today.
- **F2/F3 parabolic:** state reads only within about 2.8 m (the next cell). A dead or stuttering parabolic across the
  room is invisible (30_for_red). The light it throws still flickers, so the room shows the stutter on the floor and
  desks instead of on the fixture. Needs ceiling holes (L06/L07, 关卡设计) and a Relay bloom re-check.
- **F4 indirect pendant:** the "lens" is on top, so the player sees the ceiling glow, not a lens. Needs a ceiling glow
  (an emissive patch or decal above each pendant that takes the lamp's level) and a light aimed up or a wide soft
  spot. A pendant body hangs into the air above the chase lane: no collider, kept above 2.03 m headroom (exit_sign
  10_spec l.160; the 1988 clause [U]); with a 2.9 m ceiling a 0.6 m hang puts the body at about 2.2–2.3 m [D].
- **F5 task lights:** each lit desk wants a small light; cheap only as emissive line plus one shared light per pod, or
  baked [I]. Needs an overhead bin kit first. Strong read: "ceiling dark, desks lit".
- **F6 downlights:** warm accents; dead state reads only under it; incandescent never stutters like a tube, so its
  failure mode is On/Off (a design fit for Dim/Off cells).
- **F7 wraparound:** reads at every angle, so stutter and warn read best of all. Already in the kit.
- **F8:** decided pieces; sanctuaries and blackouts already plan around them (00_inventory rows 7, 9, 14).

## 8. What I could not confirm

- The share of lensed versus parabolic versus indirect on US office floors in 1990 (no figure anywhere; S44 not read).
- Fixture types on screen in period films: searches on *Working Girl* (1988) and *Glengarry Glen Ross* (1992) found no
  description of the ceiling fixtures; *Joe Versus the Volcano* (1990) is described only by colour (B29). To view, not
  to download.
- Dated photographs of a 1985–1993 US office whose record describes the fixtures. Records found (DEC/Xerox building
  1980, Gandy 1980) do not mention ceilings (O23, O24).
- Under-shelf lamp wattage and mounting height; indirect pendant row spacing; the 1988 text of the NFPA 101 battery
  duration; CRI 62 for CW from a dated catalog (vendor listing only).
- Whether office floors kept a share of corridor fixtures on a night or emergency circuit [G, common practice].

## 9. Candidate fixtures per zone (3 each, with reasons)

Zones in the office family. O1 and O4 exist today; O2, O3 and O5 are room roles inside Office zones that the map does
not mark yet (they need 关卡设计: a room role, by size or module). L numbers are the lamp study's built kits.
**Baseline for all of them: L03 cool, landed 12:51** (keep if Red wants one lamp everywhere).

### O1 · Open-plan bullpen (Office zone, `Office_Bullpen_4x4`, cubicles and CRTs, 2'×2' print, 2.9 m)

1. **Parabolic 2×2, nine cells, U-tubes (L07).** GSA's 1991 standard fixture for computer floors (O12); fits the
   2'×2' print with no half-tile fault; the clearest silhouette change from Level 0, so the Office reads as a refit.
   Cost: holes, dark at distance, lamp states only near.
2. **Parabolic 2×4, 18 cells (L06).** The dated 1988 three-lamp office fixture (S25); a continuous run of dark grids
   with bright cones under them matches the period's "cave" complaint (B03, O09), plus occupant changes (paper
   baffles, a lamp out) as condition states. Same costs as 1, plus the 2×4 sits over two 2×2 tiles.
3. **Lensed-indirect pendants with under-bin task lights (F4 + F5).** The best-practice office of 1989 (O02, O09):
   ceiling as the light, islands on desks, almost no shadows, a different feel from every other zone. Needs a pendant
   kit, a ceiling glow per lamp, an overhead bin kit and a light budget; most new work of the three.

### O2 · Private or small office (a 1-cell or 2×2-cell room off the bullpen)

1. **Two lensed 2×4s (L03, or L01 if the channel reads as too technical).** A typical 8 × 12 ft private office had two
   2×4 lensed troffers (S03); old stock stays in closed offices when the open floor is refitted (10a §2).
2. **Lensed 2×2 with U-tubes (L08).** Fits the 2'×2' print; smaller lit face suits a small room; dated 2×2 U-tube
   layout 1988 (S25), lensed version [U].
3. **One lensed 2×4 delamped to two outboard tubes (L04) on L03.** The energy-crisis look (S02, O08) in a room the
   player sees up close; a quiet "nobody fixed this" detail.

### O3 · Conference or training room (a 2×3 or 3×3-cell room with a table)

1. **Lensed 2×4 delamped to two (L04 on L03).** LBL's 1991 conference room exactly: 4-lamp K-12 fixtures cut to 2
   (S02).
2. **L03 plus a ring of incandescent cans on a dimmer (L03 + L10).** GSA 1991: conference rooms combine fluorescent and
   dimmable incandescent (O12). Gives a warm pool over the table while the troffers can be Dim or Off: a natural
   home for Dim and Off module states.
3. **Parabolic 2×2 plus compact fluorescent downlights at the walls (L07 + L10 variant).** The 1991 GSA mix for a
   computer-era building (parabolic standard, CFL in downlights) (O12). Highest cost of the three.

### O4 · Office-floor corridor or hall (1-cell circulation inside Office zones)

1. **4 ft wraparound, two lamps, on the axis (`Kit_WrapFixture_4ft`).** The 1993 federal corridor fixture (1×4
   surface, two F40 CW, at 8 ft, O13) and NBS 1987 L5/L6 (B02); already built for Run! B and the stairwell, so no new
   model; reads at every angle, best for stutter and warnings.
2. **1×4 lensed lay-in on the axis (L09).** NBS 1987 L2 and NIST 1993 units (B02, B01); keeps the lay-in ceiling
   language of the bullpen with a narrower light; corridor at 20 fc (O12) reads dimmer than the rooms.
3. **Recessed downlights at nodes (L10 or a CFL version) with L09 between.** GSA 1991 asks for compact fluorescent in
   downlights and wall-type lighting in public corridors (O12); marks junctions and lift-lobby-like nodes as places.

### O5 · Exit-way layer (over O1–O4 and the Office side of the transition)

1. **Incandescent stencil EXIT sign (decided design, research/exit_sign).** NFPA 101-1988 spacing (≤ 30.5 m) and
   letters (152 mm); two 20 W lamps, red (exit_sign S3, S6).
2. **Battery twin-head units (`Kit_EmergencyTwinHead`).** Dark with mains on, two beams in a blackout; the period
   answer for sanctuaries and C22 blackouts (stairwell S6, O26).
3. **Sign with twin-heads mounted above it.** The 1986 drawing: units "mount above exit light, pickup power from same"
   (stairwell S6); one landmark per exit that works in both power states.

### O6 · Office station (`Station_Desk_3x3`) [decided: keep L03]

1. **Keep L03** (station research; E1 needs readable Steady lamps).
2. **L03 plus an under-shelf task light at the counter (F5).** A lit counter with the ceiling around it Dim (O04) [U for
   a security desk].
3. **L03 plus one incandescent can over the counter (L10).** A warm marker for the key spot without touching the
   station's lamp modes.

### O7 · Office ↔ Level 0 transition (renovation seam, V5 light lead)

1. **L03 warm on the Level 0 side, parabolic (L06 or L07) on the Office side.** The lamp change is the refit cue
   (10a §2); fits the renovation keep-out if the hole matches the lens rectangle.
2. **Specular reflector retrofit (one centred tube on a mirror V, in the L03 frame).** The 1986 retrofit (O07) as the
   in-between bay: same frame as Level 0, visibly altered inside.
3. **L03 cool with the L05 condition layer on the Office side.** No new model: the Office side reads as the newer,
   colder relamp with mixed tubes; the cheapest seam.

## 10. Sources (new in this file)

Reused: lamp_options `SOURCES.md` (S01–S47, B01–B69), research/exit_sign/SOURCES.md (S1, S3, S6), research/stairwell
SOURCES (S6). Project files read (read only): `VISUAL_RESEARCH_LOOKDEV.md` l.52–56, 118–124;
`OFFICE_LEVEL_FURNITURE_RESEARCH.md` l.25–35, 83–88; `research/office_and_film/02_film_shots.md` l.36–40, 77–106,
168–172, 339–350; `research/office_and_film/10_synthesis.md` l.1–50, 69–73, 120–141, 1145–1230;
`research/office_and_film/03_ip_canon.md` l.182, 246, 254; `LEVEL_MODULE_SPEC.md` l.72–76;
`Assets/Scripts/Office/FrontRoomsOfficeKit.cs` l.1–60 and kit names; `~/FrontRoomsVisualWork/apply/lamp_l03_contract/README.md`;
handoff `visual.md` l.1057–1067.

| ID | Source | Read | URL |
|---|---|---|---|
| O01 | Wakamatsu, US 4,300,185, Light fixture unit for open plan office, filed 7 Dec 1979, issued 10 Nov 1981, C W Cole & Co | F | https://patents.google.com/patent/US4300185 |
| O02 | Herst and Ngai, US 4,660,131, Method for indirect lighting, priority 8 Jun 1979, filed 17 Mar 1986, issued 21 Apr 1987, Peerless Lighting | F | https://patents.google.com/patent/US4660131 |
| O03 | Herst and Ngai, US 4,644,454, Lensed indirect luminaire having improved light distribution control, filed 28 Dec 1984, issued 17 Feb 1987, Peerless Lighting | F | https://patents.google.com/patent/US4644454 |
| O04 | McCarthy and Eichorn, US 4,622,624, Under shelf task lighting fixture, priority 10 Jun 1983, filed 17 May 1985, issued 11 Nov 1986, Electri Cable Assemblies | F | https://patents.google.com/patent/US4622624 |
| O05 | Pritchett, US 4,617,612, High efficiency task lighting fixture, filed 22 Jan 1985, issued 14 Oct 1986 (PL or TT compact lamps) | F | https://patents.google.com/patent/US4617612 |
| O06 | Ross, US 4,680,677, Freestanding luminaire for open-plan workstations, filed 18 Mar 1985, issued 14 Jul 1987 (task-ambient defined) | F | https://patents.google.com/patent/US4680677 |
| O07 | Pinch and McGrath, US 4,799,134, Optical reflector system for fluorescent lighting fixtures, filed 15 Jul 1986, issued 17 Jan 1989, Energy Reduction Systems | F | https://patents.google.com/patent/US4799134 |
| O08 | Wisniewski, US 4,536,830, Reflector assembly for lamp fixtures, filed 26 Jul 1984, issued 20 Aug 1985 | F | https://patents.google.com/patent/US4536830 |
| O09 | Hedge (Cornell), lensed-indirect vs parabolic study at an office in Webster NY renovated mid-1988; reports Oct 1989 and Sep 1990 | F | https://ergo.human.cornell.edu/lighting/lilstudy/lilstudy.htm |
| O10 | Hedge, Sims, Becker, Ergonomics 38 (1995) 260–280, abstract; publication list with 1989–1991 papers | F (abstract partial) | https://ergo.human.cornell.edu/personal/ahlil.abs.html ; https://www.ergo.human.cornell.edu/lightpubs.html |
| O11 | Sauter and Schleifer, Controlling glare problems in the VDT work environment, Library Hi Tech 3(4), 1 Oct 1985 (NIOSH, 905 users) | F (record) | https://stacks.cdc.gov/view/cdc/247590 |
| O12 | Treado and Collins, NISTIR 4960, March 1993 (= B01), re-read for the GSA draft of 22 Feb 1991: standard fixture, illuminances, source mix by space, 3500 K CRI 82, ballast factor 0.5, lamps 4–9 % low | F (first 100k chars) | https://archive.org/stream/lightingsystemde4960trea/lightingsystemde4960trea_djvu.txt |
| O13 | Fort Bliss HQ lighting retrofit, DTIC ADA330306, Feb 1993 (= S09), re-read for fixture types A–K, corridors, exit lights | F (first 100k chars) | https://archive.org/stream/DTIC_ADA330306/DTIC_ADA330306_djvu.txt |
| O14 | RPI LRC, NLPIP Lighting Answers: Task Lighting for Offices, v1 i3, April 1994 | record F; figures S (PDF 404 on fetch) | https://digitalassets.archives.rpi.edu/node/8634 ; https://www.lrc.rpi.edu/programs/NLPIP/lightingAnswers/pdf/print/LAtask.pdf |
| O15 | Liao, The Task at Hand, Architect, 12 Feb 2015 (RP-24-89 history, cave effect, scallops, indirect, CRTs) | F [L] | https://www.architectmagazine.com/technology/lighting/the-task-at-hand-new-display-technologies-require-a-re-examination-of-office-lighting-strategies_o |
| O16 | US 5,865,528, Indirect light fixture, filed 13 Mar 1997, Precision Architectural Lighting (cloudy day effect, screen silhouettes, 12 in hang) | F [L] | https://patents.google.com/patent/US5865528 |
| O17 | Klipstein, Fluorescent lamps FAQ (= S38), re-read: colour descriptions, end blackening | F | https://donklipstein.com/f-lamp.html |
| O18 | Elliott Electric listing, F48T12CW 4100 K CRI 62 | S [V] | https://elliottelectric.com/P/Item/SYL/F48T12CW/Alternate |
| O19 | US 5,838,101 (later patent): halophosphate WW about 3000 K CRI about 50, CW about 4000 K CRI about 60 | S [L] | https://image-ppubs.uspto.gov/dirsearch-public/print/downloadPdf/5838101 |
| O20 | NCI Smoking and Tobacco Control Monograph 6, ch. 10: 1985 DHHS survey, 27 % of private businesses with smoking policies | S | https://cancercontrol.cancer.gov/brp/tcrb/monographs/6/m6_10.pdf |
| O21 | research/exit_sign/SOURCES.md S1 (NISTIR 4399, Aug 1990), S3 (NFPA 101-1988 §5-8, §5-10), S6 (USACE 40-06-04, 1985–86) | via project | (in that file) |
| O22 | research/stairwell SOURCES S6: USCG Building 877 drawing, 22 May 1986 | via project | (in that file) |
| O23 | Computer History Museum 102764134, DEC negatives: interior shots of the Xerox building with cubicles being set up, March 1980 | F (record) | https://www.computerhistory.org/collections/catalog/102764134 |
| O24 | Gandy, Teleprocessing System Terminal on Desk, A, 1980, USF Digital Commons (CC BY-NC 3.0, 6.6 MB) | F (record) | https://digitalcommons.usf.edu/gandy_commercial/1159 |
| O25 | Lightolier Lytecaster downlights catalog 1988 (= S22): contents only (Wall Washing, Accent, Low Voltage, Cylinder, HID) | PART | https://www.scribd.com/document/349919563/Lightolier-Lytecaster-Downlights-Catalog-1988 |
| O26 | Gibbs, Illuminating the means of egress, CSE, 9 Mar 2020 (current NFPA 101 7.8.1.3, 7.9.2.1) | F [L] | https://www.csemag.com/illuminating-the-means-of-egress/ |

Fetch failures (nothing read): DTIC ADA097119 Task-Ambient Office Lighting (403); DTIC ADA237389 (1991 USAF letter, 403);
Lamptech phosphor page (500); Tempe eMuseum office interior (403); Pacific University cubicles record (certificate error).
