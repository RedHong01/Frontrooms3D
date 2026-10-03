# Level transitions · 02 · How real buildings change finish (US commercial, 1975–1993)

Date: 2026-10-03. Research only. Nothing in the project changed.
Question from Red: between Level 0 and Office, "the wall's texture and type are simply cut off". How do real buildings of the era change finish so that the change reads as **built**, not as a texture seam?
Scope of this file: real US commercial interiors of 1975–1993 (era lock: now = 1990, newest design 1993, second-hand back to ~1955; see `office_and_film/22_era_lock.md`). The Backrooms IPs are covered in a sibling file; the in-engine audit is in `logs/` and `images/cut_*.jpg`.

Images for this file are in `images/` with the prefix `rb_`. `rb_01`–`rb_12` are real 1990 trade-magazine pages, re-used from disk (no downloads). `rb_20`–`rb_21` are FrontRooms-scale detail sheets drawn from the numbers below; they support the photos and do not replace them. Wanted new media is listed in `media_candidates_buildings.md`.

Brand names below (Armstrong, USG, Interface, Fry Reglet and others) are research context only. Era lock: no trademarks in the game.

---

## 0. The answer on one page

**Rule 1. A finish never ends on a bare plane.** In a real building every change of finish sits on a built object: a door frame, a corner, a metal bead or reveal, a header or soffit, or a floor strip. FrontRooms ends the finish on a mathematical line.

**Rule 2. A finish belongs to a room, not to the wall behind it.** A corridor is finished as one space, from end to end. The finish changes only where you pass through something: a door, a cased opening, a portal. In FrontRooms the finish follows the cell behind the wall, so a straight corridor wall changes paper mid-run (audit: 0.61 flat seams per 24 m chunk).

**Rule 3. Each layer stops on its own stop, made by its own trade.** Paper or paint stops at the frame face, a bead, or a corner. The base stops at the frame. The carpet stops under the closed door leaf or at a strip. The ceiling stops at a wall angle or a soffit face. These stops are 0–0.16 m apart, so a real threshold is a small cluster of parallel lines, not one cut.

**Rule 4. The threshold is thick.** The 0.16 m wall depth is where the transition lives: frame throat, saddle, jamb return, header. FrontRooms already has this depth; it is not used.

**Rule 5. The newer layer owns the joint.** New work is laid against old work: new frames and base stop the old paper; new paint covers old paper; new carpet butts old carpet with a seam or a strip. If Office is the 1990 fit-out and Level 0 the older shell, the Office side should own every frame, base end, reducer and reveal, and Level 0 should be the layer that gets cut, wrapped or painted over.

**Rule 6. Change leaves evidence.** A moved wall leaves a scar on the floor, the ceiling and the walls it touched. Renovation layers are the cheapest way to make a zone change look like history instead of a bug.

Where the rules apply in the current map (per 24 m chunk, mean over 1620 chunks, from the audit's `logs/counts.md`):

| Edge on a zone border | Count per chunk | What a real 1990 building puts there | Detail sheet |
|---|---:|---|---|
| Height step anywhere (any theme) | 10.7 | gypsum soffit/fascia face with corner bead; lay-in ceilings die into it on wall angle | §6, rb_21 C |
| Theme border, solid wall | 2.3 | demising wall: two independent faces; fine as built, except at its two ends | §2, §4 |
| Theme border, door | 1.4 | steel frame (2 in face) both sides; carpet change under the closed leaf; base stops at frame | §3, rb_20 A |
| Theme border, open edge (no wall) | 0.9 | cased opening or portal: two pilasters + header + floor strip | §2, rb_20 B |
| Theme border, arch | 0.4 | cased opening: jamb wrapped by ONE finish with corner beads, saddle in the throat | §3, rb_20 B |
| Theme border, window | 0.3 | borrowed-light frame (steel), each face stops at the frame | §3 |
| Flat seam (paper switches mid-plane) | 0.6 | never happens; real fix is a pilaster, a reveal, or moving the change to a frame | §4, rb_20 C |
| Seam hidden in an inside corner | 2.8 | normal and correct in real buildings (two rooms meet) | §4 |
| Floor cut length | 4.6 m | carpet seam under door, reducer or saddle; never mid-room without a border | §5, rb_21 A |
| Ceiling cut length | 2.7 m | header, gypsum band, or soffit; never two grids tee to tee | §6, rb_21 B |

---

## 1. What the cut looks like now (why it reads as a bug)

Evidence: the audit's frames `images/cut_seam_1.jpg`, `cut_door_1.jpg`, `cut_arch_1.jpg`, `cut_step_1.jpg`, `cut_floor_1.jpg`, `cut_ceiling_1.jpg`, `cut_post_1.jpg`, `cut_wallend_1.jpg`, `cut_open_1.jpg`, and Red's sheet `glass/images/g10_01_representative.jpg`.

What a real builder would never leave:
1. **Paper and paint meet on a flat plane** with no bead, no reveal, no trim (`cut_seam_1`, `cut_open_1`).
2. **The jamb of a doorless opening is half paper, half paint.** Each 0.08 m skin keeps its own finish, so the split runs down the middle of the jamb (`cut_arch_1`, shot1 in `shots.json`).
3. **Carpet changes on a line inside the opening** with no seam detail, no strip, no height change (`cut_floor_1`).
4. **Two ceiling grids meet tee to tee** (2×4 to 2×2) with no header (`cut_ceiling_1`).
5. **Wall ends are split** paper/paint down the end face (`cut_post_1`, `cut_wallend_1`; 0.17 per chunk) and coincident faces z-fight at posts (3.2 per chunk).
6. **No base on walls.** Real 1990 offices have 4 in (0.10 m) rubber or vinyl cove base on every wall. FrontRooms has the 0.10 m cove only on columns. The base is itself a transition device: it ends against the frame and wraps outside corners.

---

## 2. Tenant boundaries and demising walls

**Primary image:** `rb_01_interiors9006_p125_tenant_portal.jpg` (Interiors, June 1990, p.125: a law firm's 47th-floor elevator lobby leading to its reception). The lobby is landlord finish: pale stone floor with a dark border and inset squares, painted portals with wood plinths. The suite starts at a dark wood portal carrying the firm name. The floor changes to blue carpet exactly on the portal line. Every bay of the lobby is framed by a pilaster pair and a header, so the change has a frame to sit in.

### What the period did
- **The wall line is the property line.** The 1980 office measuring standard (ANSI/BOMA Z65.1-1980) measures rentable area "to the center of demising walls separating rentable areas" [S1]. The landlord or each tenant finishes its own face. FrontRooms walls are already centred on cell lines with two 0.08 m skins, so a solid border wall is correct as built.
- **Building standard vs tenant upgrade.** A lease "work letter" listed the landlord's standard items: partitions per linear foot, doors, hardware, ceiling, lights, floor covering [S17]. A typical building standard: ceiling at 8 ft 6 in (2.59 m), 2×4 ft "non-directional fissured" lay-in tile on 15/16 in grid; suite entry a solid-core wood door in a metal frame, double doors off lobbies [S17]. Tenants paid to upgrade. So the visible boundary between "standard" and "upgrade" is always at a door or portal, because that is where the contract changes.
- **Rated corridors.** Under the Uniform Building Code of the period, exit corridors above a small occupant load were 1-hour construction with protected door openings (UBC 1991 §3305(g)–(h) [S18]; in practice 20-minute self-closing doors). Corridor walls ran to the structure, so the corridor ceiling and the suite ceiling are **two separate ceilings**, each stopping at the wall.
- **Partition depth.** A standard partition: 3-5/8 in steel studs with one layer of 5/8 in gypsum each side = 4-7/8 in (0.124 m). Two layers each side (rated or acoustic demising wall) = 6-1/8 in (0.156 m) [S4, S5 for components]. **FrontRooms' 0.16 m wall is a heavy demising or corridor partition**, which is the right type for a zone border.

### At FrontRooms scale
- A solid border wall needs nothing new on its faces. It needs a decision at its **ends**: where it meets another wall it dies into that wall (inside corner, finish hidden, correct); where it stops free (a wall end) the end face gets ONE finish wrapped round two corner beads, or a 0.16 m pilaster cap.
- An **open** border edge (0.9 per chunk) is not real. The real equivalent is a portal (`rb_01`): two pilasters 0.16 m deep × 0.10–0.20 m wide, a header 0.30–0.60 m deep across the opening, a floor stop on the portal centre line. This turns an open edge into an arch-like opening with a clear owner.
- A **door** border edge is a suite entry. The real "tell" of a suite entry is a different door from the building standard: wood veneer leaf, a full-height sidelight in the same steel frame, a sign plate. FrontRooms already has `Door_Veneer` and `Kit_InteriorWindow`.

---

## 3. Door frames, casings, cased openings

### What the period did
- **Hollow metal frame.** Standard US steel frame profile: 2 in (51 mm) face, 5/8 in (16 mm) stop [S11]. "Slip-on drywall" knock-down frames go into finished drywall openings and wrap both faces; their throat is 1/8 in (3 mm) wider than the wall [S11]. Joints and screws stay visible on KD frames [S11]. USG's 1980 handbook lists steel frames "designed for installation after the walls are in place" [S4, p.37]. MasterFormat (pre-2004 numbers): 08110 Steel Doors and Frames.
- **The frame is the stop for both faces.** Paper is trimmed tight to the frame face; paint is cut in to it; the base stops against it. One frame, painted one colour on both sides, ends two different finishes at once. This is the single most common transition in a 1990 office.
- **Wood casing** (finish carpentry, 06200): 2-1/4 in (57 mm) wide, about 11/16 in (17 mm) thick, mitred at the head. Used on wood frames, in older or lower-cost interiors and in homes.
- **Rated doors** carry a label on the hinge jamb and a closer; corridor doors were self-closing [S18].
- **Cased opening without a door.** The drywall returns into the opening with metal corner bead on both arrises [S4, p.22: "protecting external corners"]. The jamb and head soffit get ONE finish, usually the paint of the more public side or a neutral paint; wallcovering stops at the corner bead. Metal trims are made for use "at window and door jambs, at internal angles and at intersections where panels abut other materials" [S5, p.23].
- **Control joints** run "from door header to ceiling" in long partitions [S5, p.26]. A full-height frame counts as a control joint [S6]. Where they are missing, the classic crack runs diagonally from the frame head corner: a free, era-correct renovation mark.
- **Floor at the door.** Carpet seams at doorways are centred under the door when it is closed, and seams never run perpendicular to a doorway [S2 §5.2; guide specs]. So the floor change line is the leaf line, not the wall centre and not a random line.

### At FrontRooms scale (see `rb_20` A and B)
| Element | Real | FrontRooms | Today in code |
|---|---|---|---|
| Frame face | 2 in = 0.051 m | 0.05 m | `TrimFace` 0.07 |
| Frame stands proud of wall | about 1/2 in = 0.013 m (backbend) | 0.012 m | `TrimProud` 0.02 |
| Frame throat | wall + 1/8 in | 0.163 m | wall + 0.04 |
| Stop | 5/8 in = 0.016 m, 0.016 deep | 0.016 × 0.016 | none |
| Door | 3 ft × 7 ft = 0.914 × 2.134 m | 1.0 × 2.1 | 1.0 × 2.1 |
| Header over door, Low room 2.4 m | 1 ft (0.30 m) at an 8 ft ceiling | 0.30 m | 0.30 (wallpaper) |
| Header over door, Standard 2.9 m | | 0.80 m | 0.80 (drywall) |
| Floor change line | under closed leaf | leaf plane (stop side), not wall centre | wall centre |
| Base | 4 in = 0.102 m, stops at frame | 0.10 m, ends 0.005 m off the frame | missing on walls |

Rule for arches (cased openings): jamb and head soffit painted, one colour, corner bead on the two arrises (a 0.012 m bevel or a 0.006 m raised round), each wall face's finish stops at its corner bead. Never split the jamb.

---

## 4. Wall terminations

### What the period did
- **Outside corners** get metal corner bead under the finish [S4]. Wallcovering wraps the corner; specs avoid seams near outside corners. Where traffic hits corners, a **corner guard** runs "from top of base to wainscot cap or ceiling in a continuous length" [S8]. Vinyl wall protection was on the market from 1969 and matching vinyl wallcovering from 1981 [S13]. Typical stainless shields are 2 in wings × 48 in high (0.05 × 1.22 m) [S26]. The wallcovering spec of the period cross-references corner guards [S10].
- **Inside corners** are where two rooms' finishes meet naturally. A paperhanger also puts the **kill point** (the one place the pattern cannot match) in the least visible corner, often behind the door or over it [S19].
- **Ending a finish on a flat wall** is done with a metal stop, not a cut: J- or L-shaped casing bead (USG No. 200 series "casing for gypsum panels"; No. 400 "reveal type all-metal trim") [S4, p.22], or an aluminium reveal (Fry Reglet, in business since 1949) [S14]. A reveal is a 1/2–3/4 in (13–19 mm) recessed slot. The two finishes stop into the slot and its shadow hides any mismatch.
- **Horizontal stops.** A chair rail (about 0.8–0.9 m above floor) or a wainscot cap (wainscot up to about 1.2 m) splits a wall: durable vinyl below, paint above. Guide specs pair "wainscot caps and corner guards" as the standard trim for wallcovering [S8]. In late-1980s homes the wallpaper **border** strip (0.15–0.23 m tall) did the same job at chair-rail or ceiling height (practice; candidate media in `media_candidates_buildings.md`).
- **Columns and pilasters.** Columns are furred with drywall and corner beads; the column face is its own finish surface. A column or pilaster is the most natural place for a finish to change along a long wall. In high-rise offices partitions meet the exterior wall at a mullion on the 5 ft planning module (practice); FrontRooms' 3 m cell is almost exactly two 5 ft modules (3.05 m).
- **Wallcovering stock.** Commercial vinyl wallcovering came 52–54 in wide (1.32–1.37 m) in three duty types; Type II (medium, about 13 oz/yd²) for corridors (Federal spec CCC-W-408; revision D dated 1994) [S8, S9]. Joints are vertical only and double-cut [S10]. Residential rolls were narrower (about 0.53 or 0.69 m).
- **Spec section numbers** of the period: 09250 Gypsum Board (beads, reveals, control joints), 09950 Wall Coverings [S10], 09900 Painting, 10260 Wall and Corner Guards.

### At FrontRooms scale (see `rb_20` C and D)
- **Flat seam fix, three options, best first:**
  1. Move the change to the nearest frame, corner or column (finish follows the room, Rule 2). Zero new geometry.
  2. A pilaster at the seam: 0.16 m wide, 0.05 m proud, full height, painted, on a 0.10 m base. Reads as a column wrap or the end of a demising wall.
  3. A vertical reveal: 0.013 m wide × 0.013 m deep slot, dark, full height. Cheapest built object; reads as deliberate.
- **Wall ends** (`cut_post_1`, `cut_wallend_1`): one finish wraps the 0.16 m end face; two corner beads; base wraps round. Or a 0.16 × 0.16 m pilaster cap 0.02 m proud.
- **Corner guards** on Office outside corners: 0.05 m wings, 1.22 m high (or base to ceiling), clear or beige vinyl. They tell the player "this side is the newer, contract-grade build".
- **Wallpaper strips.** The L0 repeat is 0.75 × 1.125 m. If paper seams are ever drawn, 0.75 m strips put exactly 4 strips on a 3 m cell and land seams on repeat edges (straight match). One kill-point mismatch per room, in the corner nearest the door, is period-correct and costs nothing.

---

## 5. Floor transitions

**Primary images:** `rb_02_interiors9006_p079_carpet_bands.jpg` (1990 fibre ad: an open office where a blue-grey field, a dark blue border band, a light pinstripe and a terracotta circulation band meet in straight seams; no strips, the change is drawn by bands). `rb_05_interiors9010_p049_carpet_three_formats.jpg` (1990: one loop carpet "in broadloom, 6-foot rolls … or … modular", sold because "it disguises the seams when installed"). `rb_04_interiors9006_p002_sheet_vinyl_inlay.jpg` (1990 sheet vinyl: colour changes by inlaid bands and a wood-look border). `rb_11` and `rb_12` (1990 vinyl tile and 1/8 in rubber floor, "asbestos-free").

### What the period did
- **Carpet to carpet.** A seam, not a strip. CRI's installation standard (first issued 1982; revised 1984, 1986, 1988, 1991…) says: run seams the length of the area, along traffic, not across light, and "not perpendicular to doorway openings" [S2 §5.2]. At doorways the seam is centred under the closed door (guide specs citing CRI) [S2]. A colour change inside one room is made with an inlaid band or border (`rb_02`, `rb_01` stone border).
- **Formats and module.** Broadloom mostly 12 ft wide (3.66 m), so seams fall every 3.66 m [S22]. 6 ft (1.83 m) rolls with attached cushion. Carpet tiles: 18 × 18 in (0.457 m) free-lay tiles from 1973, popular in 1980s open offices for access to wiring [S12]; 24 in and 50 cm tiles also existed.
- **Carpet to hard floor.** "Where carpet transitions to other floor coverings, the carpet edges are required to be protected or covered with appropriate transition moldings" [S2 §5.3]. From 1991 the ADA standard required exposed carpet edges to be fastened and trimmed along their full length [S3, 4.5.3]. Level changes: up to 1/4 in (6 mm) vertical; 1/4–1/2 in (6–13 mm) bevelled no steeper than 1:2; more needs a ramp [S3, 4.5.2]. A vinyl or rubber reducer is about 1-5/8 to 2-3/8 in wide (41–60 mm), 1/8–1/4 in high (3–6 mm) [S23]. Metal carpet bars (aluminium or brass) were the other option.
- **Thresholds and saddles.** CRI defines the threshold as "the raised material beneath a door", also called door sill or saddle [S2, definitions]. ADA limit: 1/2 in (13 mm) high, bevelled [S3]. Marble or aluminium saddles were usually cut to the jamb depth (practice).
- **Base.** 4 in rubber or vinyl cove base, top-set; CRI recommends cove or toe base with glue-down carpet [S2 §5.3]. The base meets the frame and stops; at a zone change the base colour may change at that frame.
- **Spec sections:** 09650 Resilient Flooring (base and reducers), 09680 Carpet, 08710 Door Hardware (thresholds).

### At FrontRooms scale (see `rb_21` A)
| Joint | Real | FrontRooms | Where |
|---|---|---|---|
| Carpet seam | butt seam, no height step | a 0.003 m dark line, no geometry | door and arch lines between two carpets |
| Reducer | 41–60 mm × 3–6 mm, sloped | 0.05 × 0.006 m wedge, rubber colour | carpet ↔ VCT or vinyl |
| Saddle | jamb depth × ≤13 mm, bevelled 1:2 | 0.163 × 0.010 m, bevel 0.02 each side, stone or aluminium | under doors and arches |
| Border band | 0.10–0.30 m inlay | 0.15–0.30 m band of the other carpet, 0.6 m back from the line | open edges, portals |
| Broadloom seams | every 3.66 m | every 3.66 m, along the long axis of the room, drifting against the 3 m grid | Level 0 |
| Carpet tile | 0.457 / 0.61 / 0.50 m | 0.6 m (current) | Office |
| Floor change line | under the closed leaf | leaf plane | doors |

---

## 6. Ceiling changes

**Primary images:** `rb_03_interiors9006_p131_layin_ceiling_tray.jpg` (1990 executive office: 2×2 lay-in grid with parabolic troffers running to the window wall; inset: a drywall tray with cove light, i.e. a ceiling step built as an object). `rb_08_interiors9006_p117_soffit_band_floor_border.jpg` (Norwest Center banking hall, Minneapolis, shown in June 1990: a soffit band with a dark reveal and clerestory over the teller line; stone border band on the floor). `rb_09_interiors9006_p118_typical_office_floor.jpg` ("one of 15 typical office floors": lay-in grid, troffers, carpet, the core wall). `rb_06`, `rb_07` (1990 ads selling different 2×2 tile designs: tile type was a design choice per room).

### What the period did
- **Suspended grid.** 15/16 in (24 mm) exposed tee; 2×4 ft (0.61 × 1.22 m) and 2×2 ft (0.61 m) lay-in panels; a 7/8 × 7/8 in (22 mm) wall angle round the room [S16]. Grid layout is centred so that border panels are not less than half width [S15]. Building standard of the 1980s: 2×4 fissured at 8 ft 6 in [S17].
- **Two grids never meet tee to tee.** They are separated by a partition, a gypsum header or band, or a soffit, each grid stopping on its own wall angle (practice; see candidate USG and Armstrong catalogue pages).
- **Height step.** A gypsum soffit (furr-down, bulkhead) with metal corner bead at its lower arris; the lower ceiling dies into its face on wall angle; the face is painted ceiling white, not papered. Corridors were often lower than offices because ducts ran over them (practice).
- **Tall spaces.** The edge of a two-storey lobby or banking hall is drawn by a fascia band, a reveal at the lower ceiling height, or a clerestory (`rb_08`). The lower ceiling height is carried round the tall room as a datum line.
- **Trays and coffers** with a cove light mark a "special" room inside a standard ceiling (`rb_03` inset).
- **Spec sections:** 09510 Acoustical Ceilings; 09250 Gypsum Board for soffits.

### At FrontRooms scale (see `rb_21` B and C)
| Situation | Real detail | FrontRooms |
|---|---|---|
| Same height, 2×4 ↔ 2×2 (Level 0 ↔ Office) | gypsum band between two wall angles | 0.15–0.30 m flat white band on the zone line, 0.022 m wall angle on each side; or drop it 0.05 m as a shallow header |
| 2.4 ↔ 2.9 at an opening | 0.5 m soffit face | 0.5 m painted fascia over the opening, corner bead on the lower arris (0.012 m round), wall angle on the 2.4 side |
| 2.9 ↔ 5.4 at an opening | fascia + datum | 2.5 m face; a 0.05 m reveal or ledge at 2.9 m carries the low ceiling line round the tall room |
| Beams between columns | drywall-wrapped beam | exists (`BulkheadDepth` 0.35); the same object can carry zone changes |
| Border tiles | ≥ half tile | room clear width 3n − 0.16 m gives 0.52 m border tiles on a 0.6 m module: fine |
| Old vs new tiles | yellowed vs white | tile tint ±5–8 % on a renovated band (§7) |

---

## 7. Renovation layers

**On-disk image:** `Research/week02/kit-references/a24/a24_trailer_0115_blue_painters_tape.jpg` (the A24 film's set: painter's tape and a half-finished wall, the look of work in progress).

### What the period did
- **Paint over wallpaper.** Cheap and common; the seams show through and the paper can bubble; a shellac or oil primer is needed or the print bleeds through latex (Wallcovering Installers Association, 1999) [S20]. At FrontRooms scale: Office green-grey paint over Level 0 paper shows vertical seams every 0.75 m and a faint ghost of the print in raking light.
- **Patched drywall.** Joint compound under paint "flashes": a duller patch with a halo, worst in raking light [S21]. GA-214 (1990) set the industry's "levels of finish" for exactly this [S7]. At scale: rectangles 0.3–0.6 m, a lighter or duller sheen, sometimes left in white primer.
- **A moved partition.** Leaves: a 0.16 m strip on the floor where the track was (no carpet, adhesive, or a carpet patch of another dye lot), a line of new white tiles in an old ceiling, a vertical 0.16 m scar where it met a remaining wall (the old finish in a strip, screw holes, a gap in the base), and abandoned outlet boxes covered over (the 1997 spec says "install covering over abandoned outlet boxes") [S10]. Control-joint cracks run from frame head corners [S5, S6].
- **Different carpet run under a missing wall.** A 0.16–0.6 m patch strip of another carpet across the room, seamed both sides.

### At FrontRooms scale
These marks can sit **next to** a zone border, not on it. They explain the border: "this used to be a wall; one side was redone". They cost decals and a few thin boxes, not new topology.

---

## 8. The kit at FrontRooms scale (all numbers in metres)

Read with `images/rb_20_fr_scale_plan_details.jpg` and `images/rb_21_fr_scale_section_details.jpg`.

| # | Object | Size (FR) | Material idea | Goes on | Frequency per chunk |
|---|---|---|---|---|---|
| K1 | Steel door frame | face 0.05, proud 0.012, throat 0.163, stop 0.016 | `Painted_Metal`, one colour both sides | every door and window on a border | 1.4 + 0.3 |
| K2 | Cased-opening jamb | corner bead 0.012 round on both arrises; jamb + soffit one paint | Office paint | every arch on a border | 0.4 |
| K3 | Portal | 2 pilasters 0.16 deep × 0.10–0.20 wide; header 0.30–0.60 deep | paint, base wraps | open border edges | 0.9 |
| K4 | Saddle | 0.163 × 0.010, bevel 1:2 | stone or aluminium | doors, arches, portals on a floor change | 2.7 |
| K5 | Reducer | 0.05 × 0.006 wedge | rubber | carpet ↔ hard floor | as needed |
| K6 | Carpet seam line | 0.003 dark line | decal | carpet ↔ carpet, at leaf line | 2.7 |
| K7 | Cove base | 0.10 × 0.006 | rubber, Office brown/black | all Office walls; stops at K1/K2 | all |
| K8 | Reveal | 0.013 × 0.013 slot | dark | flat seams, wall ends | 0.6 + 0.2 |
| K9 | Pilaster cap | 0.16 × 0.05 proud | paint | flat seams, wall ends | 0.6 + 0.2 |
| K10 | Corner guard | wings 0.05, height 1.22 or full | clear or beige vinyl | Office outside corners | many |
| K11 | Soffit fascia | 0.5 (2.4/2.9) or 2.5 (2.9/5.4) | ceiling white, bead on arris | height steps at openings | 5.8 |
| K12 | Grid band | 0.15–0.30 wide | ceiling white | same-height grid change | 2.7 m |
| K13 | Wall angle | 0.022 × 0.022 | white metal | every ceiling edge | all |
| K14 | Datum reveal | 0.05 at 2.9 m | dark | tall rooms beside standard rooms | 1.0 |
| K15 | Renovation decals | seams at 0.75, patches 0.3–0.6, scar 0.16 | decal set | 0.5–1.5 m from a border | optional |

---

## 9. Input for the plan (not a decision)

1. **Fix the rule before the art.** Assign each face's finish by the space it faces, and put every change on K1/K2/K3/K8/K9. This removes all flat seams and split jambs. It is a map contract change (the map chat owns `FrontRoomsMapWorld.cs`).
2. **Doors first.** K1 + K6 + K7 cover 1.4 border doors per chunk and every non-border door. Highest value for least geometry.
3. **Open edges become portals (K3).** 0.9 per chunk; this is the case in Red's screenshot.
4. **Ceiling headers (K11, K12).** Height steps are the most frequent event (10.7 per chunk); a soffit face with a bead reads as built immediately.
5. **Renovation marks (K15) as the "story" variant.** Good for a "Level 0 painted over by the office fit-out" read.

Pre-render variations should test, at least: (a) frame-only, (b) portal + saddle, (c) reveal/pilaster for flat seams, (d) soffit + grid band, (e) paint-over-paper renovation band.

---

## 10. Sources

Accessed 2026-10-03. Text sources were read, not downloaded as media. Licences: the codes, standards and catalogues are cited for facts; the Interiors pages are copyright of their publisher and are used as research evidence only (same terms as `Research/week02/furniture-ads/SOURCES.md`).

| # | Source | URL | Use | Status / licence |
|---|---|---|---|---|
| S1 | ANSI/BOMA Z65.1-1980 method, quoted in Illinois Admin. Code Title 44 | https://ilga.gov/ftp/JCAR/AdminCode/044/044050000C03100R.html | demising wall = centre line | public law text |
| S2 | CRI 104, Standard for Installation of Commercial Carpet (orig. 1982; 2019 text) | https://carpet-rug.org/wp-content/uploads/2019/03/CRI-104-STANDARD-For-INSTALLATION-of-COMMERCIAL-CARPET.pdf | seams §5.2, transitions §5.3, threshold definition | © CRI; quoted |
| S3 | 1991 ADA Standards (ADAAG 4.5.2, 4.5.3, 4.13.8), published 26 Jul 1991 | https://www.access-board.gov/adaag-1991-2002.html ; https://www.ada.gov/law-and-regs/design-standards/1991-design-standards/ | level changes, carpet edge trim, ½ in thresholds | US government, public domain |
| S4 | USG, Gypsum Construction Handbook, 1980 (Building Technology Heritage Library) | https://archive.org/details/GypsumConstructionHandbook | trims p.22, corner bead, frames p.37 | Public Domain Mark 1.0 |
| S5 | USG, Gypsum Construction Handbook, 2nd ed., 1982 | https://archive.org/details/USGConstructionHandbook2ndEdition | trim p.23, control joints p.26 | CC BY-NC-ND 3.0 |
| S6 | Gypsum Association GA-216 control joints (summaries) | https://www.nationalgypsum.com/ngconnects/blog/building-knowledge/guidelines-best-practices-installing-drywall-control-joints ; https://www.wconline.com/articles/84638-all-things-gypsum-br-proper-use-of-control-joints | 30 ft spacing, frame = joint | © publishers |
| S7 | GA-214 Levels of Finish, first published 1990 | https://www.wconline.com/articles/89568-recommended-levels-of-finish-revised-and-expanded | patch and finish levels | © W&C |
| S8 | UFGS 09 72 00 Wallcoverings (USACE/NAVFAC, Aug 2026) | https://www.wbdg.org/FFC/DOD/UFGS/UFGS%2009%2072%2000.pdf | types and weights, 52/54 in widths, corner guards and wainscot caps | US government, public domain |
| S9 | FS CCC-W-408D, Wallcovering, Vinyl Coated (14 Jan 1994) | https://everyspec.com/FED_SPECS/C/CCC-W-408D_NOTICES-1and2_9845/ | the federal vinyl wallcovering spec of the period | public domain |
| S10 | Univ. of Arizona Design Standards, Section 09950 Wall Coverings (05/97) | https://dc.ufs.arizona.edu/sites/default/files/2025-05/Section-09950-Wall-Coverings-05.97.pdf | old section number, vertical double-cut joints, outlet boxes, corner guards | © UA; quoted |
| S11 | Steel Door Institute: SDI 111 details; "KD vs welded frames" | https://steeldoor.org/wp-content/uploads/2020/02/SDI_111.pdf ; https://steeldoor.org/kd-vs-welded-frames/ | 2 in face, 5/8 in stop, slip-on drywall frames, throat | © SDI |
| S12 | Interface, Inc. history | https://en.wikipedia.org/wiki/Interface,_Inc. ; https://www.georgiaencyclopedia.org/articles/business-economy/interface-inc | 18 in carpet tiles from 1973, 1980s offices | CC BY-SA (Wikipedia) |
| S13 | Construction Specialties, innovations timeline | https://www.c-sgroup.com/corporate/innovations | vinyl wall protection 1969, wallcovering 1981 | © C/S |
| S14 | Fry Reglet profile (LBM Journal) | https://www.lbmjournal.com/resources/partner-content/article/15785244/elevate-drywall-details-with-fry-reglets-advanced-trim-reveal-systems | aluminium reveals, firm since 1949 | © LBM Journal |
| S15 | Armstrong, Suspended Ceilings Installation Instructions | https://www.armstrongceilings.com/content/dam/armstrongceilings/commercial/north-america/installation-and-maintenance/suspended-ceilings-installation-instructions.pdf | border panels ≥ ½; ASTM C636 | © Armstrong; read via search summary (site refused connection) |
| S16 | USG Donn suspension accessory data (7/8 × 7/8 in wall angle) | https://www.usg.com/content/dam/USG_Marketing_Communications/united_states/product_promotional_materials/finished_assets/donn-suspension-systems-accessory-data-AC2396.pdf | wall angle size | © USG; via search summary |
| S17 | Lease "building standard" clauses; work-letter glossary | https://www.lawinsider.com/clause/ceiling-heights ; https://www.creherald.com/glossary/workletter/ | 8 ft 6 in, 2×4 fissured, 15/16 grid; suite doors | © publishers |
| S18 | Indiana DHS order citing 1991 UBC §3305(g)–(h), §3318(e) | https://www.in.gov/dhs/files/final-order/Non-Final-Order-Cause-No-97-18F-Paoli-Wee-Care.pdf | rated corridors, protected openings | public record |
| S19 | Fine Homebuilding, "Wallpaper the right way" | https://www.finehomebuilding.com/project-guides/painting/wallpaper-the-right-way | kill point | © Taunton |
| S20 | Christian Science Monitor, 6 Oct 1999, "The wallpaper's got to go…" | https://www.csmonitor.com/1999/1006/p12s6.html | painting over paper: seams, bubbles, primer | © CSM |
| S21 | JLC, "Preventing drywall patches from telegraphing through" | https://www.jlconline.com/how-to/interiors/preventing-drywall-patches-from-telegraphing-through_o/ | flashing | © JLC |
| S22 | Broadloom widths (secondary) | https://www.flooringclarity.com/what-is-broadloom-carpet-types-uses-installation/ | 12 ft standard width | © publisher; secondary |
| S23 | Vinyl carpet reducer listings (catalogue sizes) | https://www.ceramed.ca/en/products/core-flooring-vinyl-carpet-reducer-1-black-from-1-8-3-2-mm-to-1-4-6-4-mm-x-2-3-8-x-12-3101 | reducer widths and heights | © retailer; modern sizes, same as period |
| S24 | *Interiors* (Billboard Publications), June 1990 and October 1990, via USModernist Library | https://www.usmodernist.org/index-int.htm | rb_01–rb_12 | © publisher; on disk since 2026-10-02 (Red-approved download), research use only |
| S25 | Building Technology Heritage Library (archive.org) | https://archive.org/details/buildingtechnologyheritagelibrary | catalogue candidates | per item |
| S26 | Stainless corner shield 2 × 48 in (retail listing) | https://www.lowes.com/pd/Prime-Line-Stainless-Steel-Corner-Shield-with-Fasteners-2-x-48-in-6-Pack/5015069221 | corner guard size | © retailer |

### Image ledger (this folder)

| File | Source page | Shows |
|---|---|---|
| `rb_01_interiors9006_p125_tenant_portal.jpg` | Interiors June 1990 p.125 | lobby → suite: portals, stone border, carpet starts at the portal line |
| `rb_02_interiors9006_p079_carpet_bands.jpg` | Interiors June 1990 p.79 (fibre ad, top photo) | carpet colour change by bands in an open office |
| `rb_03_interiors9006_p131_layin_ceiling_tray.jpg` | Interiors June 1990 p.131 | 2×2 lay-in + troffers; drywall tray with cove light |
| `rb_04_interiors9006_p002_sheet_vinyl_inlay.jpg` | Interiors June 1990 p.2 (flooring ad, library card cropped off) | inlaid bands and border in sheet vinyl |
| `rb_05_interiors9010_p049_carpet_three_formats.jpg` | Interiors Oct 1990 p.49 (carpet ad) | broadloom / 6 ft roll / tile; "disguises the seams" |
| `rb_06_interiors9010_p002_ceiling_panel_designs.jpg` | Interiors Oct 1990 p.2 (ceiling ad, card cropped off) | 2×2 ft panel designs |
| `rb_07_interiors9006_p047_ceiling_tile_change.jpg` | Interiors June 1990 p.47 (ceiling tile ad) | patterned tiles vs a standard lay-in ceiling |
| `rb_08_interiors9006_p117_soffit_band_floor_border.jpg` | Interiors June 1990 p.117 (left photo) | soffit band, reveal, clerestory; floor border |
| `rb_09_interiors9006_p118_typical_office_floor.jpg` | Interiors June 1990 p.118 (two left photos) | a typical 1990 office floor |
| `rb_10_interiors9006_p048_049_vinyl_wallcovering.jpg` | Interiors June 1990 pp.48–49 | 1990 commercial vinyl wallcovering samples |
| `rb_11_interiors9010_p005_vinyl_floor_tile.jpg` | Interiors Oct 1990 p.5 | 1990 vinyl floor tile |
| `rb_12_interiors9010_p020_rubber_flooring.jpg` | Interiors Oct 1990 p.20 | 1990 rubber floor, ".125 in", "asbestos-free" |
| `rb_20_fr_scale_plan_details.jpg` | drawn for this file | K1–K3, K7–K10 in plan at FrontRooms scale |
| `rb_21_fr_scale_section_details.jpg` | drawn for this file | K4–K6, K11–K15 in section/elevation |
