# zone_lamps · 10 · Run! and back-of-house service corridors, c.1985–1993

游戏视觉, zone-lamps workflow, research stage (run), 2026-10-31. Research only. Nothing was built, rendered or
downloaded, and nothing in main was changed. Main was read only, and the files read were not dataless (rule 20). Their
sha1s are in `~/FrontRoomsVisualWork/zone_lamps/10_run_base.txt`. The colour arithmetic is in `10_run_cct.py` in the
same folder. Wanted media are in `media_candidates.md` (this folder), with sizes taken from HTTP headers only.

Red, 2026-10-31: "目前不同区域内的灯的样式应该是符合对应区域的，我需要你对年代进行调研 ... 包括run level以及office level都是，都需要对应的调研以及灯具的revise".

**Reused, not redone:**
- the lamp study `~/FrontRoomsVisualWork/lamp_options/docs/` (10a families 6, 11, 12; S02, S04, S06–S09, S17–S20, S33, S47);
- `research/room_visuals/` (03 §4, 11/20 Emergency Power, `run_b_production/10_plan.md` and `60_production_66.md`, SOURCES with A4–A6);
- `research/stairwell/01_research.md` and its SOURCES (UBC 1991 S1, the USCG 1986 drawing S6/S48, the HABS/HAER S49–S51).

Their source numbers are used unchanged. This stage's new sources are R01–R22 (§11). Brand names appear only as
sources. No brand, trade name or label goes on any in-game object.

**Evidence tags:**
- [P] dated inside or close to 1985–1993.
- [L] a later source describing the same thing or a long-running line.
- [V] present-day vendor listing.
- [D] my arithmetic from sourced numbers.
- [I] my inference.
- [U] unconfirmed.
- [M] from memory, not checked.

Read status (§11): F = page text read, S = search excerpt only, C = caption or catalogue record only.

---

## 0. Short answer

1. **The Run! B kit is period-right.** Two things in it are dated:
   - The 4 ft two-lamp acrylic wraparound appears in a federal lab test plan of Oct 1987 [R01, P]: a 2×4 four-lamp wraparound "with surface and stem mount hardware" and a 1×4 two-lamp wraparound.
   - A wide-body 1×4 two-lamp wraparound was the 1993 replacement specified for a federal building's corridor fixtures [R02, P].

   The landed `Kit_WrapFixture_4ft` (1235 × 250 × 82 mm, two bands) is that wide-body unit.
2. **The twin-head battery unit is the period answer for corridor emergency light.**
   - A patent filed in 1984 calls the existing designs the "lunch pail" or "bug eye" type: a rectangular box holding the charger, the transfer relay and the batteries, with separate lighting heads, either on the box or remote from it [R04, P].
   - A 1986 drawing adds such units to an existing lobby, fed from the hallway light circuit by surface EMT [R05, P].

   The landed cabinet, PAR-36-class heads, red AC pilot, test button and EMT feed all match.
3. **The colour split is right; the absolute colours are cool.**
   - The period pair is cool-white tubes (4100 K, CRI 62) [R10] against 6 V incandescent sealed beams (about 2700–2900 K [V, I]). That is about 113 mired apart.
   - The landed pair is wraps about 7600 K against heads about 4000 K, about 117 mired apart (`10_run_cct.py`, sRGB light colours linearised).
   - So the warm-pools-after-cool-tubes read is correct. Both ends sit about 3,000 K cooler than the period sources. That is a look decision Red already approved. §8 R3 gives an option.
4. **The wrap reads dimmer than the period fixture.** By arithmetic, a wraparound's face is at least as bright as a lensed troffer's at nadir and much brighter at grazing angles (§5) [D]. The landed wrap lens emission is 1.1, against L03's 1.8 (Red, 10-31) and the stairwell's 2.4 on the same kit. One wrap face level is wanted (§8 R2).
5. **The wrap's emission map is too clean.** Real tubes have dim cathode ends, black ends, mixed colours after spot relamping, and dust and dead insects lying in the lens trough [S18–S20, S47]. A few texture variants deliver most of the "1990 back-of-house" read (§8 R1).
6. **Back-of-house had a wider family than wraps:**
   - bare strips, 4 ft and 8 ft;
   - strips with an industrial reflector;
   - wire guards where carts and ladders hit tubes;
   - gasketed vapour-tight units in wet rooms;
   - incandescent "jelly jars" in mechanical rooms;
   - 1953-era bracket fixtures and 300 W incandescent pendants still running in 1993 [R02, P].

   These are the candidates for the other back-of-house zones (§9).
7. **Code levels:**
   - normal corridor light about 10 fc (108 lx) [R02, P; IES 1981 via R13];
   - emergency light 1 fc (10.8 lx) average and 0.1 fc minimum at the floor for 1½ h [R08; R06 via stairwell S1];
   - EXIT faces at 5 fc [R07, P].

   Emergency light is therefore about 1/10 of normal. B's pools sit at about 0.25 × armed, a readability choice, and are kept.
8. **No EXIT sign in Run! (C41) fits the period** if the corridor is a dead-end service corridor and not exit access. Its side doors then carry NOT AN EXIT (OSHA, since 1974), which B already does.

---

## 1. What a service corridor behind the offices looked like, c.1985–1993

| What | Evidence | Date | Tag |
|---|---|---|---|
| Corridor fixtures 4 ft, two F40T12 **cool white** lamps, magnetic ballast, 96 W, **bracket-mounted about 8 ft (2.44 m) up** in a building with 12–14 ft (3.7–4.3 m) ceilings; 95 % of lighting still as built in **1953**; corridor design criterion **10 fc** | Fort Bliss HQ lighting retrofit study, pp. 2–3 [R02] | Feb 1993 | P |
| The 1993 replacement for those corridor fixtures: "wide body, 1'x4' wrap around fixture ... with two (2) F40T12/RS/SS lamps", surface mounted, electronic ballast, 74 W | same, p. 6 [R02] | Feb 1993 | P |
| Supply and equipment areas: **300 W pendant incandescent**, to be replaced by 2-lamp surface wraparounds; washer rooms: **porcelain lampholders** (bare bulbs), "not recommended for retrofit" | same, pp. 3–4, 7 [R02] | Feb 1993 | P |
| A lab test plan's six standard luminaires: L1/L2 lensed 2×4 and 1×4, L3/L4 parabolic, **L5 2×4 four-lamp acrylic wraparound "with surface and stem mount hardware"**, **L6 1×4 two-lamp wraparound** | NBSIR 87-3656, Table 1 [R01] | Oct 1987 | P |
| 8 ft two- and three-lamp fixtures in labs; 2×4 four-lamp in offices | LBL-31350 [S02] | Sept 1991 | P |
| "All ... buildings surveyed ... had lighting fixtures which were **significantly aged and discolored**"; mostly 4 ft T12 "in various fixture style configurations"; survey legend R / S (surface) / P (pendant) / ST (shop type) / W (wall) / G (globe) | Fort Bragg field notes [R03] | Mar 1995 (buildings older) | L |
| Six twin-head battery units added to an existing lobby; note 3 "pickup feed from existing hallway light using EMT"; note 4 two units "mount above exit light" | USCG Bldg 877 drawing [R05 = stairwell S6/S48] | 22 May 1986 | P |
| Painted brick in two tones, exposed EMT and pipe runs beside a stair, "FIRE DOOR / KEEP CLOSED" stencil | HABS/HAER photos [stairwell S49–S51] | 1989–90 | P |
| A basement corridor with "piping and lights" (caption only; photo not viewed) | HAER CAL,19-LOSAN,82H--28 [R14] | 1995 | L |
| A theatre's basement dressing-room corridor (caption only) | HABS WASH,17-SEAT,11--43 [R15] | July 1991 | P |

**Reading [I]:**
- A 1990 service corridor behind offices is a patchwork. In older buildings the original 1950s–70s surface or bracket fluorescents survive. In newer ones or after a refit there are wide-body wraparounds. Spaces nobody upgraded keep bare-bulb or incandescent pendants. Battery units were added later on surface EMT.
- Finishes: painted block or brick in two tones, VCT, bumper rails, red fire equipment, exposed conduit and sprinkler pipe (room_visuals 03 §4.5, stairwell 01).
- In buildings with a lay-in ceiling in the service corridor (common on tenant floors), the fixture is a 1×4 or 2×4 lensed troffer, often two-lamp (lamp study 10a families 1 and 6).
- B's exposed-soffit corridor is the basement or core-side version. It is plausible for a corridor that carries the sprinkler main.

---

## 2. Fixture families for back-of-house

Dimensions in mm, rounded. Brand-line dimensions are present-day [V] and are used only where the line is long-running.

| # | Family | Dated evidence | Dimensions | Lamps | Where |
|---|---|---|---|---|---|
| F1 | **Wraparound, 4 ft, 2-lamp** (narrow or wide body), surface or stem | L6 1987 [R01]; wide-body 1×4 corridor spec 1993 [R02] | Narrow 1219 × 219 × 71 [V, R17a]; wide body 1219–1235 × 254 × 70–82 [V, R17b]. Landed kit 1235 × 250 × 82 | 2 F40T12 (1219 long, Ø38) [S08] | Service corridors, stairs, storage, workrooms |
| F1b | **Wraparound, 2×4, 4-lamp** | L5 1987 [R01] | about 1235 × 400–430 × 80–100 [U] | 4 F40T12 | Workrooms, wider back-of-house spaces |
| F2 | **Bare strip**, 4 ft or 8 ft, 1- or 2-lamp | 8 ft strips "1953 originals still running" 1993 [S09]; strips 16 % of 1998 shipments [S06] | 4 ft channel about 1232 × 110 × 55–75 [V, R17d]; 8 ft 2438 long [V] | F40T12; 8 ft F96T12 slimline (single pin) or HO [V, R17e] | Mechanical, storage, docks, garages, service corridors in basements |
| F3 | **Strip with industrial reflector** (white enamel, uplight slots) | "Industrial" 10 % of 1998 shipments [S06]; white baked enamel reflectance 88–92 % (an HID patent filed 1979, used here for the finish only) [R18] | about 1232 × 300–330 × 100–150 [U] | as F2 | Shops, warehouses, tall mechanical rooms; chain-hung |
| F4 | **Wire guard** (accessory on F2/F3/F1) | No dated source found [U]. Present-day 4 ft guard 1219 × 330 × 57 [V, R17c] | wire Ø3–4 [U] | n/a | Storage, gyms, cart corridors, stair landings |
| F5 | **Vapour-tight linear**: fibreglass-reinforced polyester housing, gasketed one-piece acrylic diffuser, cam latches | No dated source found [U]; present-day lines describe fibreglass housing and "six ... cam latches" [V, R17f, R17g] | about 1300 × 150–180 × 110–130 [U] | 2 F40T12 or F96 | Docks, garages, wash-down rooms, pump rooms (pool stage) |
| F6 | **Incandescent "jelly jar"**: cast-aluminium base, glass globe, cast or wire guard | Lines still sold with cast guards [V, R17h]; period date [U] | globe Ø100–115 × 150–180; base Ø115–130; drop about 230 [U] | 60–150 W A-lamp, about 2700–2850 K [U] | Mechanical, boiler, electrical rooms, older stairs (stairwell 01 §1.6) |
| F7 | **Lay-in 1×4 / 2×4 lensed troffer, 2-lamp** | 1×4 two-lamp acrylic L2 1987 [R01]; 1×4 8 % of 1998 recessed [S06] | lamp study 10a family 6 | 2 F40T12 | Back-of-house with a grid ceiling (Level 0 Low) |
| F8 | **Wall-bracket fluorescent** | Fort Bliss corridor fixtures, bracket at about 2.44 m [R02] | [U] | 2 F40T12 | Tall old corridors |
| E1 | **Twin-head battery unit** ("bug eye", "lunch pail") | 1984 patent background [R04]; 1986 drawing [R05]; PAR 36 sealed beams or wedge-base lamps [R09] | Landed cabinet 338 × 191 × 114 + heads Ø115. Present-day 324 × 102 × 140 [V, R17i]. PAR 36 = 36/8 in = 114 Ø | 2 × 6 V sealed beam, 6–8 W each in present-day listings [V, R17j]; the 1984 patent's own unit: 4 V 5 W halogen wedge-base lamps, red LED AC pilot, "press to test" switch, pure-lead sealed cells [R04] | Corridors, stairs, exit doors, lobbies |
| E2 | **Remote heads** on one cabinet | "separate lighting heads ... mounted remote from ... a box" (prior art in 1984) [R04] | head as E1 | as E1 | Long corridors fed from one cabinet |
| E3 | **Emergency ballast inside a fluorescent fixture** (one tube on battery) | Present-day product: NiCd, 90 min [V, R17k]. 1990 availability [U] | inside F1/F7 | one tube at reduced output [U] | Option only |
| E4 | **Generator-fed "night lights"** (some normal fixtures on the emergency panel) | High-rise emergency systems must start within **10 s** (UBC 1991 §1807(i)3) [R06] | any normal fixture | as normal | High-rises |

---

## 3. Lamps and colour temperatures of the era

| Lamp | CCT / CRI | Note | Source |
|---|---|---|---|
| F40T12 cool white (CW), halophosphate | 4100 K / 62 | The most used tube; 3050–3150 lm new, 20,000 h. To the eye it reads "plain white" | [R10; S07, S08] |
| Warm white (WW) | 3000 K / 53 | In service at LBL 1991 | [R10; S02] |
| White (W) | 3500 K / n.a. | | [R10] |
| Daylight (D) | 6500 K / 79 | Rare in back-of-house [I] | [R10] |
| Deluxe cool white | 4100 K / 89 | Costlier, dimmer [L] | [R10] |
| T8 32 W (from 1981) | 3100 / 3500 / 4100 K, CRI 75 | New installs only in 1990 [I] | [S07] |
| F96T12 slimline (8 ft) | as CW | Instant start, single pin [V] | [R17e] |
| 6 V sealed beam, PAR-36 class (emergency heads) | about 2700 K listed for a present-day emergency PAR 36 [V, R17j]; period [U] | Small lamps: 6–12 W | [R09, R17j] |
| Same lamp at the end of a 90 min discharge | dimmer and warmer: with the battery at 87.5 % volts, about 0.6 × the light and about −150 K [D, using the usual incandescent rules flux ∝ V^3.4 and CCT ∝ V^0.42; the cut-off voltage is [U]] | Matches the code's decline to 0.6 fc at 90 min [R08] | [D] |
| A-lamp incandescent (jelly jar, pendant) | about 2700–2850 K [U] | 300 W pendants in 1993 supply rooms | [R02] |
| AC pilot | red neon (orange-red) or red LED (the 1984 patent uses a red LED) | Both are period | [R04] |

Ballasts: magnetic until 1990, with a 120 Hz hum. Rapid-start lamps come up in about a second without flicking. Preheat
(starter) fixtures flick before they light [S18, S08]. This bears on the reset (§8 R9).

---

## 4. Layer stacks: from below and at grazing angles

Read from the room upward. "Grazing" means looking down a corridor, 70–85° from straight down.

**F1 Wraparound (the landed kit).**
- From below:
  - acrylic lens, 2.5 mm, prism pattern, curved round the bottom and both sides;
  - two soft tube bands behind the lens, with cathode ends darker for about 40–60 mm [I, S18];
  - end caps of stamped steel following the lens section, with cap screws and a thumb screw holding the lens;
  - the white enamel back pan, hidden by the lens except a thin line at the soffit;
  - the soffit.
- At grazing angles:
  - the lens **sides** are what you see;
  - they stay bright, because the wrap has no cut-off;
  - the end caps are dark lozenges between lit lenses;
  - the lens edge shows a bright rim where it meets the pan.
- Down a corridor, a row of wraps reads as **bright lozenges all the way to the far wall**, unlike a parabolic troffer, which goes black (lamp study 10a §5).

**F2 Bare strip.**
- From below: a pair of bare tubes (Ø38, about 70–90 mm apart [U]), lampholders at the ends, a narrow white channel (about 110 wide) behind them, ballast cover, knockouts.
- At grazing angles: the tubes stand **below** the channel and stay fully visible, at the tube's full luminance. The channel is a thin dark line above them.

**F3 Strip + industrial reflector.**
- From below: tubes against a wide white enamel reflector (about 300 wide), the reflector's inside bright with tube images, and uplight slots as dark (unlit) or bright (ceiling-lit) dashes along the top.
- At grazing angles: the reflector's outside (grey or white enamel, unlit) **hides the tubes** above about 65–75° [U]. You see a dark trough with a bright slit underneath and lit stripes on the ceiling above the slots.

**F4 Wire guard.**
- From below: steel wires across the tubes every 25–50 mm [U]. They read as dark lines over the bright tubes, with clip ends at the channel.
- At grazing angles: a cage silhouette. Fine line shadows on the ceiling within about 1 m [I].

**F5 Vapour-tight.**
- From below: a frosted or prismatic one-piece diffuser, the tubes as one soft blur, a grey fibreglass rim with cam latches every 300–400 mm [U] and a visible gasket line.
- At grazing angles: a heavy grey body with a glowing underside. Condensation or bugs inside show as dark spots.

**F6 Jelly jar.**
- From below: a glass globe with the bulb filament visible (clear) or a hot spot (frosted), behind cast guard ribs (4–6 ribs) on a cast base.
- At grazing angles: a starburst. The ribs throw **radial hard shadows** on the ceiling and the walls near the fixture (point source).

**E1 Twin-head unit (the landed kit).**
- From the front: an almond steel cabinet with louvres and a legend plate, a red AC pilot and a test button. Above it, two yokes and two spun heads with chrome bezels. The sealed-beam lens is fluted.
  - Mains on: the lenses are dull grey-yellow discs, the pilot is red.
  - On battery: the pilot is out and each lens shows a **small hot core** (the filament's image) in a dimmer disc.
- At grazing angles (along the wall): the heads are bright only when you are inside their beam, otherwise dull discs. The hard pool edge on the floor and wall is the main visible element.

---

## 5. What each fixture does to the room

Face luminance by arithmetic [D]. Lambertian; F40T12 3150 lm [S07]; lit length about 1.17 m.

| Fixture | Face luminance | Spill | Shadows |
|---|---|---|---|
| Bare T12 tube | about **7,000 cd/m²** (3150 / (π² × 0.038 × 1.17)) | Wide; tubes hang below the channel, so a band of the ceiling either side of the fixture is lit | Soft along the tube, sharp across it |
| Wraparound, 2 lamps | average about **3,600 cd/m²** over a 0.33 × 1.17 m lens (6300 lm × about 0.7 out [U]); bands about 5,000, sides about 2,000 [I] | Wide, including the ceiling round the fixture (the soffit halo). Lights walls to the top | Soft; a large source 1.2 × 0.25 m |
| Lensed 2×4, 2 lamps (reference) | 2,874 at 0°, 1,432 at 75° [S04] | | |
| Strip + reflector | tubes 7,000 seen from below; reflector inside about 1,000–2,000 [U] | Mostly down; 10–15 % up through slots [U] | Floor bright, ceiling dim: a "cave" |
| Jelly jar 100 W | bulb 2,800 K; globe hot spot | Radial | Hard rib shadows |
| Sealed-beam head | very bright core on axis, dim off axis | Hard-edged pools; dark gaps (code allows 40:1, later editions) [R08] | Hard |

So the wrap's face is about 1.3× a lensed troffer at nadir and about 1.5–2.5× at grazing angles [D]. In a corridor of
wraps the fixtures are the brightest things in view, from every angle.

**Corridor levels [P]:**
- normal: about 10 fc (108 lx) design [R02]; the IES 1981 category for such spaces is said to be 10–20 fc [R13, via a forum, not read];
- emergency: 1 fc average and 0.1 fc minimum initially [R08], 1 fc at the floor [R06].

Normal-to-emergency is therefore about 10 : 1 at the floor.

---

## 6. Ageing

| Fixture | Signs | Source |
|---|---|---|
| Wraparound | Yellowed lens (polystyrene faster than acrylic); "aged and discolored" fixtures in 1995 surveys; dust and dead insects lying in the bottom of the lens (dark specks between the bands); black tube ends; one tube a different colour after spot relamping; a series pair dark or one dim; delamped to one tube; lens dropped out of one end cap [U] | [S33 V; R03 L; S47 V; S18, S19; S20; S17, S02] |
| Strips, reflectors | Every dead or black end shows; missing tubes; dust on the channel top and on reflector slots; guards bent by carts [U] | [S18; I] |
| Vapour-tight | Chalky, yellowed fibreglass; frosted lens; condensation and insects inside [U] | [I] |
| Jelly jar | Blackened bulb, dusty globe, rust on the guard, guards painted over [U] | [I] |
| Twin-head unit | Heads knocked out of aim; dusty lenses; yellowed legend; pilot out (charger or feed failed); a dead battery that gives no light when the mains fail. Monthly and yearly tests are current NFPA; the 1988 wording [U]. Lead-calcium cells last 10 years or more on float | [I; R09 L] |

---

## 7. Code and placement in a service corridor

- **Normal light at exits:** at least 1 fc at the floor while the building is occupied. A battery or generator source is required where an exit serves 100 or more people. High-rise emergency systems start within 10 s (UBC 1991 §3313(a)(b), §1807(i)3) [R06, P].
- **Emergency duration and level:** "a period of 1-1/2 hours"; initial "average of 1 footcandle (10 lx)" and "minimum at any point of .1 footcandle (1 lx) ... at floor level" (NFPA 101 §5-9.2.1, edition not named, pre-2000 numbering) [R08]. The 40:1 ratio and the decline to 0.6 / 0.06 fc are in later editions; the 1990 wording is [U].
- **EXIT faces:** "a reliable light source giving a value of 5 footcandles on the illuminated surface", internally lit where normal light may be reduced (OSHA 1910.37(q)(6)(7), quoted 1991) [R07, P].
  - Signs go at exits and exit-access doors, with no point in an exit-access corridor more than 100 ft (30 m) from the nearest visible sign (UBC 1991 §3314) [R06].
  - "Not an Exit" goes on doors likely to be mistaken for one (OSHA, since 1974) [room_visuals SOURCES].
- **Placement practice:**
  - units near exit doors, at corridor turns and on stairs;
  - added later on surface EMT from the hallway circuit, sometimes over EXIT signs [R05, P];
  - one two-head unit halfway along a corridor with its heads pointing each way [V, R17l];
  - a present-day spacing table gives about 19 m between units at 2.4 m mounting in a 1.83 m corridor [V, R17m].
- **For Run! [I]:**
  - B has 4 units in 27 m (one per 6 m, alternating walls), about three times a code-minimum install. That is a gameplay rhythm and stays.
  - C41 (no EXIT) reads as "a dead-end service corridor that is not exit access". Its side doors carry NOT AN EXIT. That is consistent.

---

## 8. Run! B as landed, against the period

Landed values are from main: `FrontRoomsRunRig.cs` d9bf0668, `Kit_WrapFixture_4ft.json` 4711e291,
`Run_WrapLens_E.png` 88590dc2, `FrontRoomsStairwellDresser.cs` 2abeed3f.

| # | Item | Landed | Period | Verdict |
|---|---|---|---|---|
| — | Wrap type and size | 4 ft 2-lamp prismatic acrylic wrap, 1235 × 250 × 82, surface | Wide-body 1×4 2-lamp wrap (1987, 1993) | **Keep** |
| — | Wrap spacing and mount | 9 on the axis, every 3 m, on the 2.9 m soffit among the main, tray and EMT | 10 fc in a 2.84 m corridor with 2-lamp wraps needs about 5–7 m centres [D: 6300 lm × CU 0.45 × LLF 0.75 / 108 lx ≈ 20 m² per fixture; CU and LLF are [U]]. B's 3 m gives about 20 fc, the top of the 10–20 fc band [R13]. Surface or stem [R01] | **Keep** (a well-lit corridor makes the trip bigger). Stems only for a Tall variant (R5) |
| — | Soffit halo | `RunSoffitHalo` over each wrap | A wrap's side panels light the ceiling round it | **Keep** |
| — | Twin-head cabinet, heads, pilot, test button, EMT feed | 338 × 191 × 114 almond steel, 2 PAR-36-class heads, red pilot, feed EMT to the soffit | "lunch pail / bug eye" box with relay and charger, heads on top (1984); EMT feed (1986) | **Keep** |
| R1 | Wrap emission map | Two even bands running nearly to the caps; one map for all 9 | Dim cathode ends; black ends; mixed tube colours; dust and insects in the trough; yellowing | **Revise (cheap):** 40–60 mm end falloff in the base map, plus 3 variants by instance: (a) one tube with black ends, (b) one tube warmer (about 3500 K, a spot relamp), (c) trough specks and a slightly yellowed lens albedo. Same material, chosen per wrap. No light change |
| R2 | Wrap face level | lens emission 1.1 (F10, "rule 1, 0.8–1.4"); the stairwell's same kit at 2.4; L03 at 1.8 (Red 10-31) | Same fixture, same tubes, same face. A wrap face ≥ a lensed troffer face (§5) | **Revise (Red's eye):** one wrap level for Run! and the stairwell, about 1.6–1.8 to sit with L03. F10's clip was at 2.8, so 1.8 keeps the prisms if bloom stays at threshold 1.0 |
| R3 | Colours | wraps (.90, .95, 1.0) ≈ 7600 K; heads (1, .84, .64) ≈ 4000 K; Level 0 lamp ≈ 5500 K | CW 4100 K; sealed beam about 2800 K; split about 113 mired | **Keep the split** (117 mired). Option for Red, not a recommendation: move both by the Level 0 offset (game lamp ≈ 5500 K stands for CW 4100 K, +63 mired). Wraps then match the Level 0 lamp colour (1, .96, .88) and heads (1, .77, .53) ≈ 3400 K. Cost: the armed corridor loses its cool cue against Level 0 |
| R4 | Stairwell wrap colour | the same kit with warm-white light (1, .955, .9) and lens (1, .97, .92) | WW and CW both in service [S02, S07] | **Keep**: "the safe stair got warm tubes" is period-true. Align only the face level (R2) |
| R5 | Mount in a taller service corridor | n/a (Run! is 2.9 m) | Stem-mount hardware in 1987 [R01]; strips chain-hung | **Future**: if a Run! room ever uses a Tall (5.4 m) cell, hang wraps on stems below the services |
| R6 | Unit count and aim | 4 units / 27 m, flood heads aimed across the axis about 25° off the wall | Fewer units, heads along the path | **Keep** (rhythm, Relay contrast). Optional, Red's call: one "tired" unit (heads × 0.6, about 2600 K, pilot dim) only where the no-pocket rule (pool max / 40) still holds |
| R7 | Trip timing | 0.06 s mains drop, 0.35 s black, heads at 0.362–0.434 s | A relay transfers within a fraction of a second; a 6 V filament warms in about 0.1–0.2 s [U] | **Keep** (design beat; a little long for a real unit) |
| R8 | Pools | about 0.25 × armed | Code about 0.1 × normal | **Keep** (readability, Relay ≥ 4 : 1) |
| R9 | Reset | one flick per wrap, rippling from the entrance, then a rise | Rapid start: a smooth rise in about 1 s; preheat: 1–3 flicks [S18] | **Keep**: B reads as preheat wraps. Note for 声音设计 only: magnetic hum returns with the rise |
| R10 | EXIT | none (C41) | Exit-access corridors must have signs | **Keep**: the dead-end service corridor reading (§7) |
| R11 | Stutter tube | cell 5 at 0.78, both bands together | A failing tube in a series pair flickers, often with orange glowing ends | **Optional**: variant (a) of R1 on the stutter wrap, ends glowing orange at drop-outs. Texture only |

---

## 9. Three candidates for each zone in this family

Owner: 游戏视觉 builds kits and looks. Placement of map lamps is 关卡设计's (LEVEL_MODULE_SPEC). Each candidate keeps
the map lamp rules in `00_inventory.md` §4 (the lens follows the light, the housing stays dark, LampLevel, modes,
photosafety).

Overlap: Z3, Z4 and Z6 are also covered by the parallel `10_other.md` §4.6, §4.3 and §4.9. Both stages agree:
- stairwell A stays;
- a wrap in Low service corridors;
- strip + reflector, bare bulb, or guarded globe in service rooms.

The merge stage should take one list.

**Z1 · Run! corridor, armed (normal light; exposed soffit 2.9 m)**
1. **Keep the wide-body 4 ft 2-lamp wrap**, with R1 + R2. It is dated (1987, 1993), lights the soffit, glares down the corridor, and is already a hero kit.
2. **4 ft 2-lamp bare strip with a wire guard**, on the soffit between the main and the tray. It gives the harshest "basement" read; tubes show every dead end; the guard explains the cart corridor (bumper rails). New kit about 1.5–2.5k tris. Fit: a second Run! room type, so B does not repeat.
3. **8 ft tandem strip with an industrial reflector, chain-hung**. It gives the "cave" (lit floor, dark soffit with slot stripes), which makes the trip less of a jump. For a taller or longer Run! variant only.

**Z2 · Run! tripped (emergency layer)**
1. **Keep the twin-head unit on its cabinet** (E1). Dated 1984/1986; the hero object already built.
2. **One cabinet at the entrance with remote single heads** along the corridor (E2, prior art in 1984), joined by EMT. One "battery box at the door" becomes the story object; pools can fall anywhere; fewer hero cabinets in view. New: a single remote-head kit (reuse the lamp and yoke).
3. **One tube of every 3rd wrap on an emergency ballast** (E3, 1990 date [U]). The tripped corridor stays cool and dim (about 10–15 % of one tube) instead of warm pools. It weakens B's signature, so it is better kept for Z5.

**Z3 · Stairwell X and the walkable stair (safe zone; slab 2.40 m)**
1. **Keep stairwell A** (wrap + twin-head), with R2 for the face level. Decided by Red 10-22.
2. **A guarded incandescent jelly jar on each landing wall** (F6) for the walkable stair's lower landings. Warm 2800 K, radial guard shadows, the "older service stair" (stairwell 01 §1.6). It marks depth without darkness.
3. **A 2 ft 2-lamp wrap on a wall bracket over the door** (F1 short + F8). The landing reads as a different, smaller fitting at the same colour; it frees the slab for the sprinkler head.

**Z4 · Level 0 Low (2.4 m lay-in; `Low_Storage_2x3`, back-of-house rooms)**
1. **1×4 2-lamp lensed troffer** (F7, the 1987 L2 type) in the 2×4 grid. It is the back-of-house version of L03 at half width: same lens, same rules, the same keep-outs inside the lens box.
2. **L03 delamped to 2** (one band dark). Continuity with Level 0 and the cheapest option. "Low = the landlord saved power" (S02, S17).
3. **In storage only: 4 ft wrap or guarded strip on a hard lid** (F1/F4). It marks the room as non-public (Fort Bliss supply areas moved from 300 W pendants to wraps [R02]). Needs a lid module or a grid-hung strip; placement is 关卡设计's.

**Z5 · Sanctuaries (L3, the dead circuit the Relay won't enter)**
1. **Twin-head unit running on battery** (pilot dark, heads lit). The only honest period light with mains off; the 90 min becomes fiction for "this room is on its own".
2. **A normal wrap with one tube on an emergency ballast** (E3) at about 10–15 %. A cool dim glow that reads "alive but not powered" [U for 1990].
3. **A generator "night light"** (E4): one fixture on the life-safety panel, steady at full while its neighbours are dead. Period for high-rises (UBC §1807); it makes the sanctuary findable from afar.

**Z6 · Trigger rooms beyond Run! (L2) and other service rooms (mechanical, electrical, storage, dock)**
1. **Strip with industrial reflector**, chain-hung (F3). Mechanical rooms and docks; the slot stripes and the cave read.
2. **Bare strip with a wire guard** (F2 + F4). Storage and electrical rooms; dead and black tubes show at once, which suits failure states.
3. **Vapour-tight linear or jelly jar** (F5/F6). Janitor, pump and wash rooms; heavy housings, warm or frosted light. Shared with the pool stage's club pump room.

---

## 10. Not confirmed

- A dated 1985–1993 catalogue page for any wraparound, strip, reflector, wire guard, vapour-tight or jelly jar. Only the two federal documents (R01, R02) and present-day long-running lines were read. The 1980s maker catalogues were not found online.
- The NFPA 101-1988 text itself. §5-9.2.1 was read only as quoted in a 2006 post. The 40:1 ratio in 1988–1991 is [U].
- The IES 1981/1987 Handbook illuminance categories. Catalogue records only; 10–20 fc only via a forum post.
- Sealed-beam 6 V lamp colour in 1990. 2700 K is a present-day listing.
- Emergency ballasts (E3) before 1993.
- Typical emergency unit mounting height in 1990. Present-day guides assume 2.29 m.
- No photo of the period was viewed (captions only: R14–R16; A5 and the USCG drawing are on disk).
- Film and TV references are memory only (media list, MC-R8). They are not cited as evidence here.

---

## 11. Sources (new in this stage; accessed 2026-10-31)

| ID | Source | URL | Read |
|---|---|---|---|
| R01 | Treado, *Experimental Plan for Investigation of Lighting and HVAC Interactions*, NBSIR 87-3656, prepared Oct 1987, issued Feb 1988, Table 1 (L5 "2' X 4' ... wrap around diffuser, acrylic; with 4 lamps ... surface and stem mount hardware"; L6 "1' X 4' ... 2 lamp, similar to L5") | https://archive.org/stream/experimentalpla8736trea_0/experimentalpla8736trea_0_djvu.txt | F |
| R02 | Carter & Burgess, *EEAP Fort Bliss Headquarters Building, Lighting Retrofit*, Feb 1993, DTIC ADA330306 (lamp study S09; this stage read pp. 2–7 for corridors and back-of-house) | https://archive.org/stream/DTIC_ADA330306/DTIC_ADA330306_djvu.txt | F (to 100k chars) |
| R03 | *Limited Energy Study ... Red Brick Main Post Area, Fort Bragg*, Field Notes, 27 Mar 1995, DTIC ADA330414 | https://archive.org/stream/DTIC_ADA330414/DTIC_ADA330414_djvu.txt | F (forms garbled) |
| R04 | US 4,600,972, *Emergency lighting apparatus*, filed 23 Aug 1984, issued 15 Jul 1986, Hazenlite Inc. (background: "rectangular box which separately contains charger, transfer relay and batteries", "'lunch pail' or 'bug eye' design") | https://patents.google.com/patent/US4600972 | F |
| R05 | USCG, *Building 877 Install Emergency Light Units*, 22 May 1986 (= stairwell S6/S48, on disk) | https://commons.wikimedia.org/wiki/File:Building_877_Install_Emergency_Light_Units,_May_22,_1986_-_DPLA_-_669ae937f4933f012d083c80b8ab74de.tiff | via stairwell |
| R06 | ICBO, *Uniform Building Code* 1991, §§1807(i), 3313, 3314 (= stairwell S1) | https://archive.org/details/gov.law.icbo.building.1991 | via stairwell |
| R07 | OSHA standard interpretation, 1 Feb 1991, to Isolite Corp. (1910.37(q)(6), (q)(7)); also 6 Jul 1976 | https://osha.gov/laws-regs/standardinterpretations/1991-02-01-0 ; https://osha.gov/laws-regs/standardinterpretations/1976-07-06 | F ; S |
| R08 | NFPA 101 §5-9.2.1 as quoted in a forum post, 25 Apr 2006; later-edition values (40:1, 0.6 / 0.06 fc) via DMF Lighting bulletin, 2018 | https://forum.nachi.org/t/emergency-lighting-commercial/3708/7 ; https://www.dmflighting.com/technical-bulletin-emergency-lighting-requirements/ | F ; S |
| R09 | Wikipedia, "Emergency light" | https://en.wikipedia.org/wiki/Emergency_light | F |
| R10 | D. L. Klipstein, "Colors and Spectral Characteristics of Fluorescent Lamps" (undated; CW 4100 K CRI 62, WW 3000 K CRI 53, D 6500 K CRI 79, deluxe CW CRI 89) | http://donklipstein.com/f-spec.html | F |
| R11 | US Navy, *Application Guidance for Fluorescent Lighting Projects*, DTIC ADA289703, June 1994 (= S08; re-read: no fixture-type text; 2-lamp F40 CW lm/W 62.3 magnetic, 76.3 electronic) | https://archive.org/stream/DTIC_ADA289703/DTIC_ADA289703_djvu.txt | F |
| R12 | Commons search for 1980s fixture photos and drawings: nothing usable beyond R05 | https://commons.wikimedia.org/w/api.php?action=query&list=search&srsearch=fluorescent%20light%20fixture%20wire%20guard&srnamespace=6&format=json | F |
| R13 | IES Lighting Handbook 1981 / 1984 Reference / 1987 Application: catalogue records; 10–20 fc (1981) only via a forum post; NYC DOB memo (1987) citing IES 1981 pp. 2-45/2-46, PDF text unreadable | https://search.worldcat.org/oclc/11759425 ; https://forums.mikeholt.com/goto/post?id=1429108 ; https://www.nyc.gov/assets/buildings/pdf/el3.pdf | C ; S ; not read |
| R14 | HAER CAL,19-LOSAN,82H--27 and --28, Hughes Aircraft Radar Building, "Central basement corridor" and "Piping and lights at west end of basement corridor", David G. De Vries, 1995 | https://www.loc.gov/pictures/collection/hh/item/ca2232.photos.324455p/ | C |
| R15 | HABS WASH,17-SEAT,11--43, Fox Theater, Seattle, basement dressing rooms and corridor, July 1991 | https://www.loc.gov/pictures/collection/hh/item/wa0344.photos.040769p/ | C |
| R16 | HAER ID,1-BOISE,29--32, Boise Project Office, basement corridor, published 1990 | https://www.loc.gov/pictures/collection/hh/item/id0151.photos.184572p/ | C |
| R17a | Narrow wraparound, 48 × 8-5/8 × 2-13/16 in (present-day listing) | https://www.stoneway.com/PHILIPS-LUMINAIRES-OWN232-UNV-1-2-EB-Philips-Day-Brite-OWN232-UNV-1-2-EB-2-Light-Surface-Mount-OW-Series-Fluorescent-Narrow-Wraparound-Fixture-32-Watt-White-Lamp-Not-Included/p2290656 | S |
| R17b | Wide-body wraparound line, 48 × 10 × 2.75 in (present-day LED successor of the line named in R01 and R02) | https://buy.wesco.com/Lighting/Indoor-Fluorescent-Lighting-Fixtures/Fluorescent-Wraparound-Fixtures/ACUITY-BRANDS/LED-Wraparound-120-277-48-in-L-x-10-in-W-x-2-75-in-D-Fixture-LED-Driver/LBL4-LP840/p/19088781935-1 | S |
| R17c | 4 ft wire guard, 48 × 13 × 2-1/4 in | https://lumen.ca/en/products/18-lighting/02-fixtures/06-replacement-lens-wire-guards/p-TElUV0dDVU4=-litwgcun-acuitylithonia-144fn5-wire-guard-48-in-l-x-13-in-w-x-2-14-in-h-white | S |
| R17d | Strip channel sizes: T8 4-lamp 8 ft 96 × 4-3/8 × 2-1/8 in; T12 8 ft strip depth 2.94 in | https://www.elliottelectric.com/Media/TC232MV-LIT ; https://www.homedepot.ca/product/lithonia-lighting-2-light-white-ceiling-commercial-strip-fluorescent-light/1000149997 | S |
| R17e | 8 ft heavy-duty strip, two F96T12/HO | https://rexelusa.com/p/53227/lithonia-lighting/8-heavy-duty-strip-t12/745973790267/un296ho | S |
| R17f | Fibreglass vapour-tight, "six ... cam latches", gasketed diffuser | https://steinerelectric.com/Product/Metalux-VT3-232DR-UNV-EB81-WL-GT4-U-VT3-Gasketed-Light-Fixture2-T8-Fluorescent-Lamp64-W-Fixture120-277-VACBaked-White-Enamel-Housing-463706 | S |
| R17g | Vapour-tight linear T12HO, fibreglass housing, one-piece acrylic diffuser | https://www.borderstates.com/All-Products/Lighting/Lighting-Fixtures-Components/Wet-Area-Vaportight-Light-Fixtures/Wet-Area-Vaportight-Fixtures/Columbia-Lighting-LUN-Series-Vaportite-Linears/Columbia-Lighting-LUN-Series-Vaportite-Linears-T12HO-Fluorescent/p/1489474 | S |
| R17h | Jelly-jar guards, cast and wire | https://www.rexelusa.com/p/82597/stonco/wire-guard-vaporproof-jelly-jar-aluminum-/786034203005/v131 ; https://www.platt.com/p/0002335/appleton/wire-guard-vaporproof-jelly-jar-aluminum-for-a51-fixture/781381110709/appaagu1 | S |
| R17i | Twin-beam unit 12.75 × 4 × 5.5 in | https://www.ontimesupplies.com/tco70012-swivel-head-twin-beam-emergency-lighting-unit-1275w-x-4d-x-55h-white.html | S |
| R17j | PAR 36 emergency lamps: 2700 K listing; 6 V 8 W sealed beam, 50 h | https://www.rexelusa.com/p/724088/satco-72-watt-par36-emergency-building-bulb-045923043345-s4334 ; https://grandbrass.com/8-watt-sealed-beam-par36-50-average-rated-hours-screw-terminal-base-6-volt/ | S |
| R17k | Fluorescent emergency ballast, NiCd, 90 min (present day) | https://www.bulbs.com/product/B50 | S |
| R17l | Kansas State Fire Marshal, *Emergency Lighting* (two-light unit halfway, heads each way) | https://firemarshal.ks.gov/DocumentCenter/View/1517/Emergency-Lighting | S |
| R17m | Emergency unit spacing table, 1.83 m corridor: 18.9 m at 2.4 m mounting | https://tnb.ca.abb.com/en/brands/emergi-lite/modules/Files/el_454_catsheet.pdf | S |
| R18 | US 4,285,034, *Enclosed industrial luminaire*, filed 13 Jun 1979, issued 18 Aug 1981 (white baked enamel 88–92 %) | https://patents.google.com/patent/US4285034 | F |
| R19 | *American Cinematographer*, Dec 1988, *Die Hard* (no interior fixture detail) | https://theasc.com/articles/sophisticated-visuals-on-grand-scale-for-die-hard | F |
| R20 | AFI Catalog, *Gremlins 2: The New Batch* (1990): the office-tower set built on a Burbank stage | https://catalog.afi.com/Catalog/moviedetails/58529 | S |
| R21 | NEC damp/wet location marking (later wording; 1987/1990 text not read) | https://iaeimagazine.org/electrical-inspections/nec-article-410-luminaires-lampholders-and-lamps | S |
| R22 | Project files (read only): `FrontRoomsRunRig.cs` l.95–160, 1017–1030, 1554–1588; `FrontRoomsRunDress.cs` l.14, 386–402, 625–643; `FrontRoomsStairwellDresser.cs` l.32–55, 616–683; `Kit_WrapFixture_4ft.json`; `Run_WrapLens_E.png` (viewed); Blender `run_wrap_4ft.py`, `run_twinhead*.py` (W/run_b66) | main and W | F |
