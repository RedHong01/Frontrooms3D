# zone_lamps · 10 · Pool: period lamps for the pool family

游戏视觉, zone-lamps workflow, research stage (pool), 2026-10-31. Research only: main was read only (no Unity, no git;
the `Documentation` files read were not dataless), nothing was downloaded on purpose, nothing was rendered.

Red, 2026-10-31 (游戏视觉 chat): "目前不同区域内的灯的样式应该是符合对应区域的，我需要你对年代进行调研，然后思考比如说泳池房间（在这里泳池房间指的是后室概念里的特定泳池level，以及那个年代的泳池会使用的设施）……都需要对应的调研以及灯具的revise".

**Reused, not redone.** `research/bathroom_rooms/` (pool club L8: 10_research §1.7, §1.8, §2.2 rows 15–18, §2.4, §3.8;
SOURCES S40–S50, P1–P4; ids `BR:Sxx`), the lamp study `~/FrontRoomsVisualWork/lamp_options/docs/10a` (families 9, 11, 12;
lamp data S07, S08; ids `LO:Sxx`), `research/stairwell/SOURCES.md` S5, S12 (egress lighting; ids `ST:Sx`), Run! B's
twin-head kit (`room_visuals/run_b_production/10_plan.md` §1.1), and `00_inventory.md` (zone #11–12, §4 lamp logic).
The sibling `10_other.md` covers the Tall hall's HID; its `Z` ids are not these. This file's sources are `PL01…`.

**Labels.** PERIOD = dated 1993 or earlier. LATER = after 1993, used only for physics or as-found states. READ = page
opened and read (often through a fetch summary). SURVEY = search summary only. [V] read by eye from a photo.
[EST] my arithmetic. [D] design inference. [U] unverified.

**Process notes.** Two pages could not be opened: the city archive copy of the 1990 NEC (certificate error; a
workaround was refused, so its text is SURVEY only) and the Fandom Level 37 page (402). Reading the 1970 Seattle pool
brochure through WebFetch made the tool cache the PDF (1.1 MB) in this session's `tool-results` folder, outside the
project; the two Commons photos were viewed in the browser pane and their screenshots sit in the same cache. Nothing
went into `Frontrooms3D/` or `Research/`.

---

## 0. Short answer

1. **A c.1990 pool hall had four light layers, not one lamp.** Overhead (sealed HID or sealed fluorescent), a wall or
   ceiling wash (indirect HID or a continuous wall band), the pool's own underwater lamps, and battery egress units.
   Today the map would hang a 2×4 lens box every 3 m over the water: no real pool looked like that.
2. **Code fixed the frame by 1990.** Fixtures over the water or within 5 ft (1.52 m) of it: 12 ft (3.66 m) up, or, indoors,
   totally enclosed, GFCI-protected and at least 7 ft 6 in (2.29 m) above the maximum water level [PL01, SURVEY].
   Underwater lamps: top of lens at least 18 in (457 mm) below the normal water level, at most 150 V, GFCI above 15 V
   (since 1975), wet-niche lamps relamped by lifting them to the deck on a coiled cord [PL02, PL03, PL07, PERIOD].
3. **Underwater: incandescent, 300–500 W, 120 V, warm.** 300 W at 10 ft (3 m) centres flush in the walls (Ontario, 1975);
   24 dry-niche 500 W lamps in a 43 × 118 ft pool (Seattle, 1970); 500 W 120 V wet-niche lamps at the 1996 Olympic pool
   [PL08, PL09, PL02]. Our 18 m pool at 3 m centres = 6 lamps per long wall, one per map cell.
4. **Overhead: dated evidence for five looks.** Sealed corrosion-resistant fixtures at 30 fc (Ontario 1975); continuous
   linear "beam fixtures" the length of the pool, switched in 3 levels, with skylight panels (Seattle 1970); a continuous
   wall band washing the upper walls (Allentown postcard, 1974); enclosed bowl fixtures between beams (JCC San Francisco,
   photo 2001); metal halide over or through a dropped ceiling, and metal halide bounced off the ceiling (as found in old
   pools, install dates unknown) [PL08, PL09, PL24, PL18, PL19].
5. **HID is a different animal.** A momentary dip puts mercury and metal halide out for minutes (about 5 min in a 1974
   patent; up to 15 min for 1980s probe-start metal halide), so fixtures carried a small auxiliary incandescent or
   halogen lamp for the gap [PL11, PL16, PL17]. Sodium lamps cycle at end of life, about a 10-minute cycle [PL12].
   This changes how `Dip`, `Stutter` and `Failing` should read in a pool hall (§7).
6. **The Poolrooms feeling is sourceless light + tile + water + repetition** [PL25, PL26]. Period hardware that makes
   light without a visible source exists and is documented: the indirect up-wash onto a white ceiling, the continuous
   wall band, flush sealed lenses in a suspended ceiling, and the pool's own underwater lamps at night. None needs any
   creator's image (§6).
7. **Two conflicts to flag.** (a) `bathroom_rooms` §1.7 argues "do the reverse of the Poolrooms" and recommends D1 = A
   (tenant club); Red's 10-31 words name the Backrooms pool level itself. The candidates in §8 work for both; D1 sets the
   mix. (b) Row 17 there ("no dropped ceiling over the pool", MIL-HDBK, undated) is contradicted by period practice: the
   1975 Ontario guide recommends a suspended acoustical ceiling with a heated space above, and Niles West's 1960 pool had
   a dropped ceiling [PL08, PL18]. A suspended ceiling over the water is period-possible.
8. **The pool hall needs its own rig, not per-cell map lamps.** A 5 × 8-cell hall would get 40 lens boxes from the map
   rule; a real hall of this size had about 8–14 HID heads or about 22 sealed 8 ft fluorescents [EST, §3.0], plus 12
   underwater lamps. Like Run! B, map cells inside the group go `Off` and a pool rig owns the lights (关卡设计 contract).

---

## 1. Sources

| Id | Source | Date | What it gives | Status |
|---|---|---|---|---|
| PL01 | NEC Art. 680-6(b) text as quoted from a City of Zephyrhills, FL archive copy, https://archives.ci.zephyrhills.fl.us/WebLink/0/doc/62374/Page6.aspx and /Page7.aspx ; forum restatement of the current rule, https://forums.mikeholt.com/threads/lighting-over-swimming-pools.47141/latest | 1990-era per one summary, 1999 per another | Indoor exception to the 12 ft / 5 ft rule: totally enclosed, GFCI, bottom ≥ 7 ft 6 in above max water level; GFCI for fixtures 5–10 ft from the pool unless high enough | SURVEY: the pages refused a TLS check; edition (1990 vs 1999) not settled |
| PL02 | J. R. Knisley, "Wiring wet-niche fixtures at the 1996 Olympics Aquatic Center", EC&M, https://www.ecmweb.com/content/wiring-wet-niche-fixtures-1996-olympics-aquatic-center | 1996-03-01 (1993 NEC era) | 680-20(a)(1)–(4): GFCI over 15 V, ≤ 150 V, top of fixture ≥ 18 in below normal water, low-water cut-off for lamps that need submersion; 680-21 boxes ≥ 8 in above water, ≥ 4 ft from the pool; 52 wet-niche 500 W 120 V lamps; stainless forming shells | READ |
| PL03 | G. Anchales, "Hazardous swimming pools: retrofitting for safety", IAEI, https://iaeimagazine.org/1999/99may/hazardous-swimming-pools-retrofitting-for-safety-2 | 1999-05 | Art. 680 from 1962; potting 1968; GFCI on underwater lamps over 15 V from the 1975 NEC; old lamps leak current | READ |
| PL04 | R. Milatovich, "Fountains: history and interpretations of Article 680", IAEI, https://iaeimagazine.org/1999/99march/fountains-history-and-interpretations-of-article-680-swimming-pools-fountains-and-similar-installations/ | 1999-03 | 150 V wording 1975/1981; 1987 and 1990 changes (boxes "submersible", cords "water-resistant") | READ |
| PL05 | J. McNicol, "Inside the NEC", WaterShapes, https://watershapes.com/?p=29838 | 1999-02-01 | 1999 NEC forming shells, positive locking device marked NEW (PL02 already cites it in 1996: conflict, minor) | READ |
| PL06 | UL 676 scope, https://standards.globalspec.com/std/14545546/ulc-676 | LATER | Cord long enough to lift the lamp out of the niche to the deck | SURVEY |
| PL07 | US 4,433,366, C. E. Wade, "Pool light mounting structure", https://patents.google.com/patent/US4433366 | filed 1982-09-30, granted 1984-02-21 | Wet niche in the wall, heavy glass or plastic lens in an elastomer seal, decorative outer ring, clamp band; relamp by raising the assembly above the water; leaked water turns to steam and can break the lens | READ |
| PL08 | Ontario Dept of Education, *Physical and Health Education Facilities for Secondary Schools*, ERIC ED109770, https://archive.org/details/ERIC_ED109770 | 1975 | Pool: 30 fc (32 dekalux) general overhead; "Sealed fixtures must be corrosion-resistant"; underwater 300 W at 10 ft (3 m) centres, flush with walls; switches in the control centre; surface glare misleads swimmers at turns; suspended acoustical ceiling with heated space above; white surfaces; RH 62–65 %; condensation "critical". Gyms: mercury vapour | READ (OCR via fetch). Canadian, North American practice |
| PL09 | City of Seattle Parks and Recreation, *Medgar Evers Memorial Pool* dedication brochure, https://archives.seattle.gov/digital-collections/media/collectiveaccess/images/1/6/8/2/61550_ca_object_representations_media_168226_original.pdf | 1970-04-26 | Underwater 24 dry-niche 500 W lamps; overhead "2 beam fixtures the length of the pool, 3 light levels, PA speakers housed in fixtures"; skylights: 2 fibreglass panels 31 ft above the deck; ceiling 17 ft (5.18 m) deck to bottom of beam; board-formed concrete; pool 43 × 118 ft, 3–12 ft deep; stainless deck drain | READ (text decoded from the PDF) |
| PL10 | BEGA 9909 underwater floodlight, technical sheet, https://archive.org/details/bega-9909-schwimmbadleuchte | 1985-12 | Front ring Ø 263 mm, 3.6 kg, stainless 1.4301, anodised reflector, safety glass; 200 W 30 V halogen (Osram 64382), about 4,800 lm, 1,000 h, via isolating transformer; 700 mm below the water | READ. West German: the low-voltage counter-example to US 120 V |
| PL11 | US 4,005,331, V. Horowitz / Current Industries, https://patents.google.com/patent/US4005331 | filed 1974-10-08, granted 1977-01-25 | Mercury lamps "in gymnasiums, and in stadiums" go dark for minutes after "even a momentary interruption"; hot re-fire about 5 min; an incandescent lamp lights meanwhile | READ |
| PL12 | US 4,207,500, Area Lighting Research, https://patents.google.com/patent/US4207500 | filed 1978-12-14, published 1980-06-10 | HPS end-of-life cycling: arc voltage climbs (about 165 V), lamp goes out, about 3 min to cool, one cycle about 10 min | READ |
| PL13 | Lamptech, GE Multi-Vapor MV-400/BD, https://www.lamptech.co.uk/Spec%20Sheets/D%20MHQ%20GE%20MV400BD.htm | sample made 2000; GE metal halide from 1962 | 400 W clear, 4,000 K, Ra 62, 36,500 lm at 100 h, 27,000 lm mean; ED-37 bulb, 292 mm long, light centre 178 mm | READ |
| PL14 | Wikipedia, "Mercury-vapor lamp", https://en.wikipedia.org/wiki/Mercury-vapor_lamp | read 2026-10-31 | Clear Ra 15–25, greenish; coated Ra 50–60; warm-up 4–7 min; halogen backup lamps; US ballast ban 2008 | READ |
| PL15 | Wikipedia, "Sodium-vapor lamp", https://en.wikipedia.org/wiki/Sodium-vapor_lamp | read 2026-10-31 | HPS on sale 1965 (Lucalox); end-of-life cycling, white or bluish at strike then red-orange before going out | READ |
| PL16 | Philips pulse-start MH sheet, https://images.philips.com/is/content/PhilipsConsumer/PDFDownloads/United%20States/ODLI20150514_001-UPD-en_US-P-5574-D.pdf | LATER | "Standard switch start" metal halide: 4 min start, 15 min restrike | SURVEY |
| PL17 | J. R. Knisley, "Benefits of a metal-halide system", EC&M, https://www.ecmweb.com/lighting-control/article/20897653/benefits-of-a-metal-halide-system | 2013 | Up to 15 min to full light after an interruption; a short dip loses the arc; probe-start lamps shift colour; arc-tube rupture, enclosed luminaires | READ, LATER |
| PL18 | F. Gonzales, "From dim to dazzling", Construction Specifier, https://www.constructionspecifier.com/from-dim-to-dazzling-bright-ideas-illuminate-old-pool/ | 2025-12-31 (pool built 1960) | As found: metal halide over the pool above a dropped ceiling, relamped from a catwalk; glass falling into the pool means draining 190,000 gal; 10–15 min restrike; too dim. New: indirect light off the dropped ceiling | READ (vendor author). MH install date unknown |
| PL19 | YMCA case studies, Access Fixtures, https://accessfixtures.com/new-led-natatorium-lighting-for-ymca-pool-area/page/2 ; also Waukesha school, 42 × 1,000 W MH indirect (search summary) | c.2011 on, vendor | Boise YMCA: MH indirect "to reflect light from the ceiling to minimize glare", ceiling and walls darkened, "cave like"; a 400 W MH shining into the pool = glare; 30 wall-mounted 400 W MH, one working; CRI about 60, green cast | SURVEY, LATER |
| PL20 | R. Robledo, "Illuminating Safely", Pool & Spa News, https://www.poolspanews.com/?p=19403 | 2011-01-14 | Codes of the 1970s–80s were in watts, some only 3 fc; lights on rafters over the water discouraged: guard glare, bulbs breaking into the pool, hard relamping so fixtures stay burned out; perimeter fixtures aimed at the water instead | READ (its IES numbers look garbled; not used) |
| PL21 | C. Riley, "Underwater Pool Lighting", Counsilman-Hunsaker, https://counsilmanhunsaker.com/underwater-pool-lighting/ | 2017-05-18 | Older codes 1.0 or 0.5 W/sq ft; lamps on opposing long walls, not competition end walls; light steps, ledges, slope breaks | READ |
| PL22 | State rules: Nevada NAC 444.142, https://www.law.cornell.edu/regulations/nevada/NAC-444-142 ; Louisiana LAC 51:XXIV.715, https://regulations.justia.com/states/louisiana/title-51/part-xxiv/chapter-7/section-xxiv-715 | LATER | 0.5 W (10 lamp lumens) per sq ft; incandescent assumed 20 lm/W | SURVEY |
| PL23 | Pyle-National, *Vaportight industrial lighting fixtures*, https://archive.org/details/VaportightIndustrialLightingFixtures | 1938 | Galvanised bodies, cast aluminium or bronze guards, cork gaskets; globes about 5⅜ in (60 W), 6⅜ in (100 W), 9⅜ in (200 W) long; clear, amber, blue, green, ruby | READ (OCR via fetch). The form is old; nothing dates the 1990 product |
| PL24 | Photos viewed 2026-10-31 in the browser pane: BR:P2 William Allen High School pool, Allentown PA, postcard 1974, https://commons.wikimedia.org/wiki/File:1974_-_William_Allen_High_School_-_Swimming_Pool_Allentown_PA.jpg ; BR:P1 JCC San Francisco natatorium, HABS photo 2001, https://commons.wikimedia.org/wiki/File:Swimming_pool._Whole_room_view_of_the_natatorium_as_seen_from_shallow_end_of_pool._-_Jewish_Community_Center_of_San_Francisco,_3200_California_Street,_San_Francisco,_San_HABS_CAL,38-SANFRA,204-38.tif | 1974; 2001 | P2: a continuous dark band at about ¾ wall height round the hall, light scalloped up the wall and onto the ceiling between pilasters, small round ceiling lights. P1: enclosed glass-bowl fixtures on short stems between painted beams, in rows | [V], public domain |
| PL25 | Backrooms Wiki (Wikidot), "Level 37 – Sublimity", egglord, rev. 52, https://backrooms-wiki.wikidot.com/level-37 | 2026-08-18 | White ceramic tile, lukewarm blue-green water that ripples by itself, light from irregular angles, some areas fully dark, calm with unease | READ (CC BY-SA; genre text only) |
| PL26 | Wikipedia, "The Backrooms", https://en.wikipedia.org/wiki/The_Backrooms ; "Liminal space (aesthetic)", https://en.wikipedia.org/wiki/Liminal_space_(aesthetic) | read 2026-10-31 | Poolrooms = a labyrinth of white-tiled indoor pools; the 2019 post's fluorescent hum; Kane Parsons' 2026 film has a Poolrooms scene; *The Poolrooms* film announced for Dec 2026; liminality = emptiness, transit, nostalgia | READ |
| PL27 | Northeastern News, profile of Jared Pike's "Dream Pools" (Instagram since 2020), https://news.northeastern.edu/?p=270492 ; Designboom, https://www.designboom.com/?p=1047890 | 2025-05-19 | The pool-render series credited as a source of the image type: white tile, blue-green water | SURVEY. **Study only: never copy his compositions** |
| PL28 | Chloramines and condensation: Construction Specifier, "Circulating good indoor air quality", https://www.constructionspecifier.com/circulating-good-indoor-air-quality/ ; https://www.poolspamarketing.com/trade/features/indoor-air-quality-solving-natatorium-air-dispersion-and-condensation-challenges/4/ | 2013; n.d. | Chloramines heaviest in the breathing zone just above the water; humid air condenses on cool surfaces | SURVEY, LATER |
| PL29 | Optics: Fresnel reflectance of water computed for n = 1.333; Snell's window, https://en.wikipedia.org/wiki/Snell%27s_window ; caustics, https://en.wikipedia.org/wiki/Caustic_(optics) | n/a | Reflectance by angle, 48.6° critical angle, ripple caustics | [EST]; pages not opened (standard optics) |
| PL30 | Translucent panels at pools: Seattle 1970 (PL09); Breuer, Duluth pool with translucent Herculite panels, mid-1950s, https://breuer.syr.edu/browse/projects-by-name/list/project-name/60941 ; Kalwall natatorium, https://www.kalwall.com/metea-valley-natatorium/ | 1950s, 1970; LATER | Diffuse daylight through translucent panels | SURVEY |

Reused: BR:S40 (TM 5-662, 1986: ¶2-7, 2-8, 2-10, 2-12h/j, 15-22: light so "all surface and bottom areas" are visible
to guards; light colours help; underwater lamps grounded; NEC 680 (1981 ed.) cited; lighting tested every 6 months),
BR:S41 (1960 wet-niche 400/500/1,000 W, about 1 W/sq ft), BR:S45, BR:S48, BR:S49, BR:S61, BR:S63 (water F0 0.02),
LO:S07/S08 (F40T12 3,050–3,150 lm; cool white most used), LO:S09 (8 ft strips), ST:S5 and ST:S12 (NFPA 101-1988 egress
light, twin-heads, 90 min, 1 / 0.1 fc).

---

## 2. What code and practice fixed

| Rule | Value | Since | Source |
|---|---|---|---|
| Underwater lamps grounded; Article 680 exists | — | 1962 | PL03 |
| Cord ends potted in the fixture | — | 1968 | PL03 |
| GFCI on underwater lamps over 15 V | branch-circuit GFCI | 1975 NEC | PL03; BR:S49 says 1968 or 1975 |
| Underwater lamp supply | ≤ 150 V between conductors | 1975 wording, "or less" 1981 | PL04, PL02 |
| Top of underwater lens | ≥ 18 in (457 mm) below normal water level | in force 1996; earlier date [U] | PL02 |
| Wet-niche relamping | lift the lamp to the deck on its cord; forming shell in the wall | practice 1982; code wording LATER | PL07, PL06 |
| Junction box for a wet niche | ≥ 8 in (203 mm) above water, ≥ 4 in (102 mm) above deck, ≥ 4 ft (1.22 m) back | 1993 NEC era | PL02 |
| Overhead fixtures over / within 5 ft (1.52 m) of the water | ≥ 12 ft (3.66 m) above max water | by 1990 [U] | PL01 |
| Indoor exception | totally enclosed + GFCI + bottom ≥ 7 ft 6 in (2.29 m) above max water | 1990 or 1999 [U] | PL01 |
| Fixtures 5–10 ft (1.52–3.05 m) from the water | GFCI unless mounted high enough | same | PL01 |
| Lighting level, indoor pool | 30 fc (323 lx) general overhead | 1975 guide | PL08; many codes only 3–10 fc (PL20, PL22) |
| Underwater density | 0.5 or 1.0 W per sq ft of water (≈ 5.4 or 10.8 W/m²) | codes of the 1970s–80s | PL21, PL22; BR:S41 |
| Visibility test | a 6 in black disk at the deepest point seen from the deck up to 10 yd | 1986 | BR:S40 ¶2-7 |
| Egress light | 1 fc average, 0.1 fc minimum, 90 min on battery | NFPA 101-1988 | ST:S5, ST:S12 |

**What these numbers mean for us.** Our hall (14.84 × 23.84 m, ceiling 5.4 m, water at about deck level) clears 2.29 m
everywhere, so a sealed fixture may hang over the water by code. The real limit was relamping: anything over the water
needed a catwalk, a lift on a covered pool or a drained pool, so designers kept heads over the deck or aimed them up from
the walls (PL18, PL20). That is the logic to show: **the lamps avoid the water.**

---

## 3. The fixtures

### 3.0 How many, for our hall [EST]

Floor 353.8 m², pool 9 × 18 m = 162 m² (1,744 sq ft). Target 30 fc = 323 lx (PL08). Lamp lumens = E × A / (CU × LLF);
room cavity ratio ≈ 5 × 5.4 × 38.7 / 353.8 ≈ 3.0.

| System | CU × LLF (assumed) | Lamp lumens needed | Count |
|---|---|---|---|
| Enclosed 400 W metal halide, direct (36,500 lm) | 0.60 × 0.65 | ≈ 293,000 lm | **8** heads |
| Indirect metal halide off a white ceiling (400 W) | 0.35 × 0.65 | ≈ 502,000 lm | **14** heads (or 5 × 1,000 W) |
| Sealed 8 ft 2-lamp F96T12 cool white (≈ 12,600 lm, lamp lumens [U]) | 0.55 × 0.75 | ≈ 277,000 lm | **22** fixtures |
| Underwater 300 W (3,840 lm, LATER listing) at 0.5 / 1.0 W/sq ft / Ontario 10 ft centres / Seattle density | — | — | **3 / 6 / 12 / 14** lamps (12 = one per 3 m cell on each long wall) |

### 3.1 F1 · Enclosed, gasketed HID low-bay (metal halide, mercury or sodium)

- **What and when.** A ballast housing on a hook or short stem, a reflector, a gasketed lens; a large HID lamp. Mercury in
  gyms by 1975 (PL08, PL11); metal halide commercial from the 1960s (PL13); HPS from 1965 (PL15); HID 6.2 % of lit US
  commercial floorspace in 1986 (LO:S01). Enclosed bowls between beams over a pool: JCC SF [V, PL24]. No dated 1985–93
  catalog page of a pool-rated HID unit was found [U].
- **Size.** Reflector Ø 400–560 mm, overall 600–750 mm with ballast housing [U]; flat tempered lens Ø 430–560 mm [U];
  400 W ED-37 lamp Ø ≈ 117 mm × 292 mm, light centre 178 mm (PL13). Mount: deck edges only, about 4.8–5.0 m to the lens.
- **From below.** Hook and conduit → finned grey cast-aluminium ballast box → reflector exterior (spun aluminium, dull,
  or ribbed prismatic glass that glows faintly) → gasket ring with spring clips → flat glass lens with water spots and
  dust → specular reflector facets → the lamp: a clear lamp shows a tiny arc tube (≈ 20 mm, [U]) far too bright to look
  at, inside a faint glass envelope; a coated lamp shows a softly glowing egg. **Grazing:** the lens edge catches a
  line of light, the reflector rim reads as a dark disc; the prismatic version glows along its ribs and throws a halo on
  the structure above; the aluminium version leaves the ceiling dark above it.
- **Light.** Hard pools on deck and water, sharp column and rail shadows, dark ceiling (direct units) or a soft halo
  (prismatic). On the water, each head is mirrored as a long, broken streak (§5). Metal halide 4,000 K, Ra 62 (PL13);
  clear mercury blue-green, Ra 15–25 (PL14); HPS orange [U for K and Ra].
- **Ageing.** Lamp-to-lamp colour scatter in metal halide (pink, green, blue lamps side by side) and colour drift with
  hours (PL17); lumen drop (36,500 → 27,000 lm mean, PL13); HPS cycling (PL12, PL15); burned-out heads over the water
  left dead because relamping is hard (PL19: one of 30 working; PL20); white bloom on aluminium, rust bleed at steel
  screws from chloramines (PL28, BR:S48).

### 3.2 F2 · Indirect HID up-light on the wall (ceiling as the lamp)

- **What and when.** A box on the wall with its opening facing up, aimed at a light ceiling, so the ceiling becomes the
  source and the water carries no fixture images. Documented in pools as found later: Boise YMCA ("to reflect light from
  the ceiling to minimize glare"), Waukesha (42 × 1,000 W), Niles West's renovation (PL19, PL18). Whether pools had this
  **in 1990** is [U]; the 1987 office patent US 4,660,131 (indirect pendant) shows the idea in the period (office stage).
- **Size.** Box ≈ 450 × 450 × 250 mm [U] at 3.0–3.7 m on the long walls, one per 6 m [EST].
- **From below.** A dark underside (painted steel or cast aluminium, maybe a perforated tray) and no lamp: you see only
  the lit ceiling above, a bright oval 2–3 m across over each box fading into the next [D]. **Grazing:** the top glass
  glows as a thin line; the upward glass collects dust and dead insects, which print soft shadows into the ceiling's hot
  spot [D].
- **Light.** The most even and least "located" light a real pool had: soft shadows, no streaks on the water, a bright
  ceiling. Ageing darkens ceiling and walls and the room becomes a cave (PL19). This is the period route to the Poolrooms'
  sourceless light.

### 3.3 F3 · Continuous wall band (valance) round the hall

- **What and when.** A continuous fascia at about ¾ wall height, lamps behind it throwing light up and down the wall,
  scalloped between pilasters; small round ceiling lights besides [V, PL24: Allentown, 1974]. Lamp type not visible
  [U]; fluorescent is the likely load for a continuous band [D].
- **Size.** Fascia ≈ 150–200 mm tall [V/EST], continuous along both long walls; in our 5.4 m hall at ≈ 3.8–4.0 m [D].
- **From below and grazing.** A dark horizontal line ringing the room; above it, bright scallops climbing the tile and
  the ceiling edge; below it, softer scallops. No lamp is ever seen. The upper hall glows; the middle of the ceiling is
  darker [V].
- **Ageing.** Dead tubes leave dark gaps in the ring of scallops; a stuttering tube flickers one scallop [D].

### 3.4 F4 · Sealed linear fluorescent (vapour-tight) in rows, or in the beams

- **What and when.** A gasketed housing with a prismatic acrylic or polycarbonate lens and latches, 4 ft or 8 ft, T12
  lamps. Ontario 1975 demands "sealed … corrosion-resistant" fixtures (PL08); Seattle 1970 ran "2 beam fixtures the length
  of the pool" in 3 switching levels (PL09). The fibreglass-reinforced polyester housing form is sold today; its 1980s
  date is [U].
- **Size.** 4 ft ≈ 1,300 × 160 × 120 mm, 8 ft ≈ 2,500 × 160 × 120 mm [U]; F40T12 1,219 × 38 mm, F96T12 2,438 mm (LO:S08,
  LO:S09). Two long rows over the deck, or pendants at ≈ 4.5 m [D].
- **From below.** Housing (grey or off-white, matte) → poured gasket line → latches as dark ticks every ≈ 300 mm →
  prismatic lens, smooth outside, prisms inside → two soft lamp bands → dark lamp ends at both ends. **Grazing:** the lens
  side glows brighter than its underside; housing ends opaque.
- **Light.** Long soft rows, cool white (≈ 4,100 K [U]), diffuse shadows; mirrored in the water as long lines (§5). It
  shares Level 0's tube language, so the map's fluorescent modes read true.
- **Ageing.** Condensation pooled in the low end of the lens, dead insects as silhouettes, lens yellowing, gasket squeezed
  out, chalky housing; dark tube ends, delamped gaps (LO:10a §6) [U for each in pools].

### 3.5 F5 · Flush, sealed lenses in a suspended ceiling

- **What and when.** A suspended acoustical ceiling over the pool was recommended in 1975 (PL08) and existed at Niles
  West (PL18), so flush fixtures in it are period-possible. A sealed, gasketed lay-in or recessed unit for wet rooms in
  1990 is [U]; Niles West shows the as-found alternative, HID above the dropped ceiling.
- **Size.** On our 0.6 m grid: 600 × 600 or 600 × 1,200 [D]; or round recessed lenses Ø ≈ 200–300 mm [U].
- **From below.** A white ceiling with flat white squares in it; at 5.4 m the lens reads as a bright, flat, edge-less tile.
  **Grazing:** frames vanish; only brightness differs. With a white tile room this is the cleanest "light from nowhere".
- **Ageing.** Rust stains round grid tees, sagging wet tiles, brown water rings (`Ceiling_Fissured` stain work exists)
  [D].

### 3.6 F6 · Wet-niche underwater lamp (and the dry-niche window)

- **What and when.** A niche (forming shell) set in the pool wall, a sealed lamp unit with a heavy glass lens in an
  elastomer seal and a stainless face ring, fed by a coiled cord so it can be lifted to the deck (PL07, 1982). 300 W
  (PL08, 1975) and 500 W 120 V (PL02, PL09) incandescent; 400/500/1,000 W in 1960 (BR:S41). Dry niche: the lamp sits
  behind a glass port and is changed from a tunnel behind the wall (PL09). LED pool lamps are mid-2000s (BR:S49): never
  in our room.
- **Size.** PAR56 lamp Ø 178 × 127 mm (LATER listing); face ring Ø 263 mm on a 1985 unit (PL10), US commercial rings
  ≈ 260–300 mm [U]; lens Ø ≈ 230 mm [EST]; top of lens ≥ 457 mm below water (PL02); 3 m centres on both long walls (PL08).
- **From above, steep.** Stainless ring (bright, scratched, specular) → seal ring (black) → flat glass lens → the lamp's
  fluted or stippled face → a warm hot centre. **From across the pool (grazing).** Above about 75° from the vertical the
  water reflects 21–58 % (§5), so each lamp reads as a soft, warm oval smeared by ripples, sitting under the reflections
  of the overhead lamps.
- **Light.** 2,750 K lamps (LATER listing) glow cyan-white through water and white tile [D: water absorbs red first,
  BR:S61]. They light the floor in bright, moving caustic nets, the walls from below, and, when the overheads are low,
  the ceiling with moving ripples (§5). A dead lamp is a dark port with a chrome ring catching the overheads.
- **Ageing.** Scale crust at the lens edge, iron stains from screws, an algae line, a fogged lamp with water inside,
  a lens cracked by steam (PL07); all lamps on one side out together when their GFCI trips (one branch per side [D],
  PL03).

### 3.7 F7 · Vapour-tight globe ("jelly jar") with cast guard

- **What and when.** A galvanised or cast body, a glass globe on a gasket, a cast guard (PL23, 1938 form). Wet, damp
  and plant rooms: showers, pump room [D; no dated 1990 photo].
- **Size.** Globe ≈ 162 mm long for 75–100 W, 238 mm for 200 W (PL23); body Ø ≈ 125 mm [U]. Lamp 100 W A19 (2,700 K [U]).
- **From below.** Guard bars across a bright globe; the filament visible through a clear globe. **Grazing:** the cage
  throws its bar shadows across the ceiling and walls. **Ageing:** water and dead insects in the globe, rust bleed under
  the box, fogged glass.

### 3.8 F8 · Battery twin-head and EXIT (reuse)

Egress light at 1 fc for 90 min (ST:S5). In a pool hall it matters more than anywhere: when the mains dip, metal halide
stays dark for minutes (PL11, PL16), and the underwater lamps are never on emergency power [D]. Reuse
`Kit_EmergencyTwinHead` (mains on: heads dark, red pilot), as in Run! B and the stairwell.

### 3.9 F9 · The auxiliary restrike lamp inside an HID fixture

A small incandescent or halogen lamp in the same housing that lights while the HID lamp cools and restrikes (PL11, 1974;
PL14). Seen from below: inside a dark HID fixture, one small warm point. It is the fixture "waiting". [U: whether pool
units carried it; whether battery-backed.]

### 3.10 F10 · Translucent fibreglass panels (skylight)

Seattle 1970: two fibreglass panels 31 ft (9.45 m) above the deck (PL09); translucent panels at pools since the 1950s
(PL30). Diffuse, even light from above. **Not possible on a B level** as daylight. A backlit panel would be fiction
without a period basis [D]: keep it out unless Red wants a deliberate impossibility.

---

## 4. Lamps of the era

| Lamp | Colour | Ra | Start / restrike | Output, life | Ageing look | Source |
|---|---|---|---|---|---|---|
| Mercury, clear, 400 W | blue-green; K [U] | 15–25 | 4–7 min / ≈ 5 min | [U] | slow fade; coated lamps whiter (Ra 50–60) | PL14, PL11 |
| Metal halide, clear, probe start, 400 W | 4,000 K | 62–65 | ≈ 4 min / up to 15 min | 36,500 lm initial, 27,000 mean | lamp-to-lamp scatter, drift, non-passive failure | PL13, PL16, PL17 |
| HPS 400 W | orange, ≈ 2,000–2,100 K [U] | low [U] | [U] / ≈ 1–3 min | [U] | cycling ≈ 10 min, white then red-orange then out | PL12, PL15 |
| F40T12 / F96T12 cool white | ≈ 4,100 K [U] | ≈ 62 [U] | about 1 s | 3,050–3,150 lm (F40) | dark ends, stutter, hum, yellowed lens | LO:S07, LO:S08 |
| PAR56 300 W 120 V incandescent | 2,750 K | ≈ 100 | instant, filament lag | 3,840 lm, 2,000 h | dead lamp, water ingress | PL02, listing (LATER) |
| PAR56 / reflector 500 W 120 V | ≈ 2,800 K [U] | ≈ 100 | instant | [U] | as above | PL02, PL09 |
| A19 100 W (globes) | 2,700 K [U] | ≈ 100 | instant | ≈ 1,700 lm [U] | blackened bulb | — |
| Halogen auxiliary | ≈ 3,000 K [U] | ≈ 100 | instant, times out | [U] | — | PL11, PL14 |
| Low-voltage halogen 200 W 30 V (European pool) | ≈ 3,000 K [U] | ≈ 100 | instant | 4,800 lm, 1,000 h | — | PL10 |

---

## 5. How light behaves in a pool hall

- **The water is a mirror at low angles.** Fresnel reflectance of water [EST, PL29]: 2.0 % straight down, 2.8 % at 45°,
  6.0 % at 60°, 13 % at 70°, 21 % at 75°, 35 % at 80°, 58 % at 85°, 80 % at 88°. Standing on the deck, the far half of the
  pool is a mirror of the ceiling and its lamps; the near water is clear. Wading, the eye 0.25 m above the water sees
  mostly reflection beyond 2 m (BR §3.8). Overhead heads become long streaks broken by ripples; that streak is what
  guards fought (PL19, PL20) and what POOLS' footage carries (BR §3.6).
- **Snell's window.** Under water, the sky is a 97° cone (critical angle 48.6°); outside it the surface mirrors the pool.
  A player who sinks under sees the lamps only inside that circle [PL29].
- **Caustics.** Light refracted through a rippled surface focuses into moving nets: on the pool floor and walls from
  overhead lamps, and on the ceiling and upper walls from the underwater lamps (light leaving the water upward) [PL29].
  With bright overheads the ceiling nets are faint; with the overheads out, they become the room's main moving light [D].
  Soft and dim indoors, not sun-sharp (BR §3.8). Needs the Reduce Motion path (BR §3.1).
- **Tile.** Glazed tile is specular: each lamp leaves a small highlight on every tile, so a tiled wall under rows of lamps
  sparkles in a grid; under indirect light it goes flat and milky [D]. Unglazed deck tile stays matte.
- **Air.** RH 62–65 % aimed for in 1975 (PL08), 50–60 % later (BR:S50). Condensation on cold glass and on lenses; a
  faint haze over the water where chloramines collect (PL28). Visible beams only under strong HID heads, and only
  faintly [D]. A fogged viewing window is the honest place for it (BR row 18).
- **Corrosion.** Chloramines attack aluminium, steel and brass: white bloom, rust streaks, green rings (PL28, BR:S48).
- **Glare.** Glare off the water hides swimmers at turns (PL08); lamps were kept off the water's mirror angles. A hall
  whose lamps glare into the water reads neglected or badly designed [D].

---

## 6. The Poolrooms, as a genre idea, carried with 1990 hardware

**What makes it read** [PL25, PL26, PL27]: tile on (almost) every surface, white or pale; water that is shallow,
lukewarm, blue-green and always moving; light that comes from nowhere you can point at, with some areas fully dark;
rooms repeating without purpose; calm on top, unease underneath; the liminal set (empty, made for passing through,
half-remembered).

**How an original FrontRooms pool space carries it with real, dated hardware.**

| Genre trait | Period device that produces it | Evidence |
|---|---|---|
| Sourceless, even light | Indirect up-wash onto a white ceiling (F2); continuous wall band (F3); flush sealed lenses in a white suspended ceiling (F5) | PL19, PL18, PL24, PL08 |
| The water glows | Wet-niche lamps at 3 m centres, warm lamps turned cyan by water and tile (F6); at night the pool is the light | PL08, PL09, PL02 |
| Light from irregular angles, dark pockets | Indirect boxes one per 6 m leave darker bays; dead heads over the water stay dead (relamping is hard) | PL19, PL20 |
| Moving light on tile | Caustic nets on floor, walls and ceiling | PL29 |
| Endless repetition | The 3 m grid: one underwater lamp per cell per wall, identical rings, identical boxes | PL08 (10 ft = our cell) |
| Calm with unease | A building still working: lamps restriking, the pump, the log (BR §1.5) | BR |

**Guard (never copy).** No composition, colour scheme or layout of any creator's pool images (PL27), POOLS, Kane
Parsons' adaptation or the two 2026 films. No arches, domes or skylit vaults (BR:P4 is the counter-example). Our rooms
stay rectangles on the grid with signs; how much tile covers the walls and whether water crosses doorways are Red's D1
and D8 (BR 20_for_red).

---

## 7. What this means for the map's lamp logic (`00_inventory.md` §4)

| Logic | Fluorescent (F3, F4, F5) | HID (F1, F2) | Underwater (F6) | Globe (F7) |
|---|---|---|---|---|
| `Steady` | as Level 0 | steady, colour per lamp scattered (MH) | steady warm | steady |
| `Dip` (W1 burst) | dips with the line | a deep dip puts it **out**; the auxiliary lamp lights; it restrikes over minutes, coming back dim and cool before reaching colour (PL11, PL16, PL17); the game must compress 5–15 min [D] | follows the line at once (filament lag) | as underwater |
| `Stutter` | true (starter, tube) | not period-true; use `Failing` | — | — |
| `Failing` | tube dying | HPS cycling, ≈ 10 min a cycle (PL12); MH colour drift and dimming | lamp out | lamp out |
| `Dead` | delamped or dead tube | head burned out and left (PL19) | dark port; a whole side if its GFCI trips [D] | dead bulb |
| Groups | as map | one circuit per wall side [D] | one GFCI branch per side [D] | — |
| Emergency | — | twin-heads carry the hall during a restrike (F8) | never on emergency [D] | — |

- **Lens follows the light** still holds: the glowing part is the lens, bowl or the lit ceiling patch; housings stay dark.
- **Photosafety:** HID restrike is slow and safe; HPS cycling is slow; only caustics move fast (Reduce Motion).
- **Placement:** pool-group cells take map lamp `Off`, and a pool rig (like `FrontRoomsRunRig`) owns overheads, wall
  boxes, underwater lamps and twin-heads; 关卡设计 owns the group and the contract.

---

## 8. Candidates: three per zone

Zones of the pool family (BR §1.6 layout): **P1** pool hall overhead (5.4 m); **P2** pool basin; **P3** locker rooms
and gang showers (2.4–2.9 m, wet); **P4** club entry, towel desk and corridor (2.9 m, dry, lay-in); **P5** pump and
chlorine rooms (2.4 m, damp, corrosive); **P6** tiled approach and wading connectors (only if Red takes the genre side of
D1).

### P1 · Pool hall, overhead

| # | Candidate | Why | Cost and risk |
|---|---|---|---|
| P1-A | **Indirect metal halide boxes on both long walls** (F2), ≈ 3.4 m up, one per 6 m (8 boxes), white painted structure or acoustic deck above, plus F8 twin-heads | Period hardware that makes the Poolrooms' sourceless light; no streaks on the water so reflections stay clean; ageing (dark ceiling, dead box = dark bay) is a free variation; restrike beat | 1990 date for pool use [U]; needs a bright ceiling material and GI/probe work; few lamps to model |
| P1-B | **Enclosed metal halide low-bays over the deck edges** (F1), 8 heads on 2 rows, clear 4,000 K with lamp-to-lamp colour scatter, auxiliary restrike lamp inside | The most common tall-wet-room answer of the era; strongest horror beat (out, small warm point, slow cold return); matches the Tall hall's HID if `10_other.md` picks it | Point sources and streaks on the water are the opposite of sourceless; glare must be placed off the main sight lines |
| P1-C | **Sealed 8 ft fluorescent rows** (F4), 2 continuous lines over the deck (≈ 22 fixtures) or in beam pockets as Seattle 1970, plus the F3 wall band variant | Shares Level 0's tube language and existing lamp modes (stutter, failing) with no new state machine; continuous lines repeat endlessly in the water's mirror; cheapest | Less special as a room; long mirrored lines read "gym" more than "dream" |

### P2 · Pool basin, underwater

| # | Candidate | Why | Cost and risk |
|---|---|---|---|
| P2-A | **Wet-niche 300 W, 3 m centres, both long walls (12 lamps)**, stainless ring Ø ≈ 270 mm, top of lens 457 mm down | Matches the 1975 guide exactly and lands one lamp per map cell; even glow, steady caustics | 12 lights under water: budget as one baked pattern + few real lights on mobile [D] |
| P2-B | **Wet-niche 500 W, 6 m centres (6 lamps) plus extra lamps at the deep end and the steps** | Fewer, brighter pools of light with dark gaps between them: the "irregular angles" read; deep end lit as code practice wanted (PL21) | Less even; dark gaps hide the floor, which a guard would have flagged (BR:S40 ¶2-7) |
| P2-C | **Dry-niche lamps behind glass ports, serviced from a tunnel behind the pool wall** (Seattle 1970) | Real 1970 hardware; adds a service tunnel along the pool where you look into the lit water through ports: a strong, original space | A new room off the pump room; only worth it if 关卡设计 wants a route |

### P3 · Locker rooms and gang showers

| # | Candidate | Why | Cost and risk |
|---|---|---|---|
| P3-A | **Sealed 4 ft 2-lamp fluorescent, surface-mounted** (F4) over aisles and showers | One fixture family for the whole club; fluorescent modes for free | Plain |
| P3-B | **Vapour-tight globes with cast guards** (F7) in the shower bays, F4 over locker aisles | Cage shadows on wet tile, warm 2,700 K against cool aisles: the wettest room reads different from the dry ones | Two kits |
| P3-C | **The Level 0 lamp (L03) in the dry locker aisles, F7 globes in the showers only** | Continuity: the club is a tenant fit-out inside the same building; showers are the change | The L03 lens in a wet room is a stretch [D] |

### P4 · Club entry, towel desk, corridor

| # | Candidate | Why | Cost and risk |
|---|---|---|---|
| P4-A | **L03 as Level 0** | Decided lamp; no seam at the club door; station rules reuse | None |
| P4-B | **L03 plus recessed incandescent cans over the towel desk** (LO family 9) | A club reception wanted warmth; a warm pool at the counter says "staffed" | One extra light (TV-light budget rules, as the station) |
| P4-C | **2 × 2 lensed U-tube troffers** (LO family 6) through the club | Reads as a later fit-out, distinct from Level 0 before you reach the water | Needs the 2 × 2 kit |

### P5 · Pump and chlorine rooms

| # | Candidate | Why | Cost and risk |
|---|---|---|---|
| P5-A | **Vapour-tight globes with cast guards** (F7) on conduit (`Kit_ConduitEMT`) | Plant-room cliché that is also correct; cage shadows over pipes; warm against the pool's cyan | — |
| P5-B | **8 ft industrial strips with bare tubes** (LO family 12) | Back-of-house standard (LO:S09); bare tubes show every dead end | Corrosion would argue against bare strips by water [D] |
| P5-C | **Sealed 4 ft fluorescent** (F4) | Correct for a corrosive, damp room; same kit as P3 | Plain |

### P6 · Tiled approach and wading connectors (genre side only)

| # | Candidate | Why | Cost and risk |
|---|---|---|---|
| P6-A | **Flush sealed lenses in a white suspended ceiling** (F5), one per cell | Light without fixtures, on our grid; the most "dream" with period parts | Wet-rated lay-in for 1990 [U] |
| P6-B | **Continuous wall band** (F3) at ¾ height, both walls | Dated 1974 pool look; the corridor glows from its upper walls with no visible lamp | New linear kit |
| P6-C | **No overhead: wet-niche lamps in the wading channel walls + EXIT and twin-heads** | Corridors lit from the water only, some dark: the canon's "areas fully dark" with real hardware | Navigation in the dark needs 教程设计's read; Relay visibility rules (BR §3.3) |

**My lean (for the options stage, not a decision):** P1-A with P1-B's restrike as the event (the wall boxes are MH
too, so they share it), P2-A, P3-B, P4-A, P5-A, P6 only if D1 moves toward the genre.

---

## 9. Open and unverified

1. The exact 1987 and 1990 NEC 680-6(b) wording (PL01 is SURVEY; edition unclear). A 1990 NEC copy would settle it.
2. Indirect HID in pools **before 1993**: all evidence found is as-found later (PL19); one dated period photo is needed.
3. A dated 1985–93 catalog page for a pool-rated enclosed HID or a fibreglass vapour-tight fluorescent.
4. US wet-niche ring and lens sizes of the period (Ø 263 mm is a 1985 German unit).
5. Fixture type of the Allentown wall band (fluorescent assumed) and of the JCC bowls (HID or incandescent).
6. IES Lighting Handbook 1981/1987, sports chapter: indoor pool illuminance and underwater lamp-lumen guidance.
7. Media wanted are listed in `media_candidates.md` (this folder), § Pool. Nothing downloaded.
