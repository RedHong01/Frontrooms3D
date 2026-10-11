# 21 — Direction C realism: how a 1990 US stud wall really fails round a doorway (DB1)

Status: **RESEARCH, 2026-10-08.** Nothing in the game, the kit, the map or Red's project code was changed, and no media was downloaded. This step wrote only this file, `SOURCES.md` (new, the media ledger for this folder) and a "21" section in `media_candidates.md`. Working notes, the Commons metadata pulls and the query scripts are in `$W/door_break/C/research/` (`$W` = `/Users/redwang/FrontRoomsVisualWork`).

**Red's feedback (2026-10-08, on `images/db_C_L0F_burst_S4_V1_LA.jpg`, translated):** "I need you to improve this broken structure: right now it does not look real enough." Direction C's idea stays: the giant crushes the doorway, the wall gives, and it reads at 12 m. What has to change is the **break structure**. It must become physically right for 1990 US interior construction.

**Built from:** `10_design.md` §1, §3 and §8.3 (= **[10]**), `02_real_door_failure.md` (= **[02]**), `20_dir_C.md` (= **[C]**), the C frames `images/db_C_*` and `dir_C_*`, the C scripts in `$W/door_break/C/scripts/` (`crackgen.py`, `scene.py`, read only), `../level_transitions/02_real_buildings.md` (= **[RB]**, 1990 partitions and trims), `../wallpaper_motion/10_synthesis.md` (wallpaper layers), `Tools/lookdev/gen_surfaces.py` (the game's wallpaper: one roll is 0.75 m wide) and `Tools/Blender/frontrooms_kit/assets/doorway_studs.py` (the kit's framing, read only). Web sources are in §9.

**Tags**
- **[read]**: I opened the page or document and the claim is in it.
- **[search]**: from a search-engine summary; the page itself was not opened or was blocked.
- **ESTIMATE**: my number or inference, no direct source.
- **UNVERIFIED**: nobody has checked it in a photo, a test or a render.
- **RED**: needs Red's decision.

**Words.** **P** = the Relay's side (Low room, 2.4 m lay-in ceiling). **S** = the player's side in BURST (Standard room, 2.9 m). Stages **S0, M, D1, D2, FINAL, S4** as in [10] §1.1. **Board** = 1/2 in gypsum wallboard ("drywall", "Sheetrock"). **Bay** = the space between two studs. **Pop** = a fastener head pushing out through the finish. **Pull-through** = the board tearing over a fastener head, leaving the fastener in the stud. Door root frame as [10] §1.2: Z along the opening (0 → 1.0), Y up, X toward S.

---

## 0. Short answer for Red

1. **Drywall does not crack like glass.** A sheet of glass is one brittle material, so its cracks run free and branch. A drywall wall is a sandwich: two paper skins on a soft, brittle gypsum core, screwed to a wood grid every 406 mm (16 in). The grid decides where it breaks. Cracks run **straight**, along the **tape joints**, along the **fastener rows on the studs** and along the **header line**, plus a few short hairlines out of the two **head corners**. There is no spider web, no forking tree and no radial fan. Real tests on stud walls with door openings record exactly that sequence: hairlines at the opening corners first, then fasteners pulling through the board, then the taped joints failing and whole sheets moving (McMullin and Merrick 2002, CUREE W-15 [read]).
2. **Cracks stay hairline until the board actually breaks.** Corner cracks in racking tests are **0.1–0.6 mm** wide (0.005–0.025 in [search]). They grow to about **300 mm** long and then stop: past about 1 % drift they only get wider (Deierlein and Kanvinde 2003, W-23 [read]). A crack "opens" only when one plate steps out of the wall plane. Then what you see is the **step**, a lit lip on one side and a shadow on the other, not a fat dark line.
3. **The wallpaper hides the hairlines and tears last.** Paper and vinyl wallcovering bridge cracks under about 1 mm. The first things to show are the **wallpaper seams opening** (every 0.75 m in our paper), a crease over the crack and the caulk line at the casing. The paper tears only where the board under it breaks. It tears with a **white fibrous edge**, often takes the board's top paper ply with it (the grey-brown inner plies show), and **curls**.
4. **Holes are bays, not blobs.** When the board breaks out, it breaks between studs and hinges on the fastener line. A hole is a **rectangle 368 mm wide** (the clear gap between two 2× studs at 16 in), straight at the studs and ragged at the top and bottom. Inside it you see the stud faces, screws standing with a collar of gypsum, the header, and the **grey back of the board on the far face**, lit by the room, never flat black.
5. **Every broken edge shows a white band.** The core is 12.7 mm of white gypsum (#E8E4DA) between two paper skins, with paper fringes standing 2–15 mm past the gypsum. That white rim is the brightest thing in the breach, and it is what reads at 3 m.
6. **The debris is the wall itself.** Chunks are 12.7 mm plates with **the room's own wallpaper on one face** and grey back paper on the other. They have straight, crumbly gypsum edges and torn paper fringes. Most of them land **within 0.5 m of the wall base**. White powder spreads out from the base in a fan, sits in the carpet pile and on every top edge (base, casing head, the leaf's top edge), and streaks down the wall under the cracks.
7. **The casing goes at the mitre.** The head mitre opens first (1–6 mm). Then the casing pries off the wall with its finish nails, which either pull through the casing or stay in it pointing out of its back. Under load it splits along the grain at the nail lines.
8. **At 12 m lines cannot read; areas must.** At the game's 76° FOV in 1080p, 1 mm is 0.06 px at 12 m and 0.23 px at 3 m (§10.2). A real crack is invisible at 12 m. The current frames drew cracks 3–25 mm wide so they would read at distance, and that is the main reason they look fake. The honest 12 m read comes from **areas**: wallpaper strips peeling in 0.75 m widths (43 px), the bulge, plates hanging off the wall, bays knocked out (21 px wide, white-rimmed), the dust veil and the powder fan on the floor.
9. **Light does not come through a crack.** A crack through one face opens into a dark cavity. Light from the other room passes only where **both faces are open in the same bay**. The "S room light through the cracks as specks" in the C hero frame is not physical unless the far board is breached in that bay too.
10. **What C keeps.** The giant, the scale, both head corners and the read at 12 m all stay. What changes is the structure: a **grid-driven** break (joints, fastener rows, bays, header) with hairlines that stay thin, area-based damage that reads at distance, and debris that is clearly made of the same wall.

---

## 1. The build-up: a 1990 US interior partition at a door

### 1.1 Section through the wall, S → P

Read left to right as a section at Y ≈ 1.8 m, just outside the latch jamb. Sizes are nominal US sizes with metric values; "kit" means the FrontRooms door kit.

| Layer | What it is in 1990 | Thickness | Colour (sRGB hex) | Source |
|---|---|---|---|---|
| Wallcovering | See §1.7: paper, vinyl-coated paper or paper-backed vinyl in rolls (game: 0.75 m roll); commercial walls Type I/II fabric-backed vinyl, 54 in wide | 0.15–0.3 mm paper; 0.3–0.6 mm vinyl | game print: ground #D2C27C, mid #AC9A52, deep #766A34, cream #E3D594 | `gen_surfaces.py` `wallpaper()`; R15, R16 |
| Sizing or primer | A sealer coat so the paper sticks and can be removed later | film | clear to off-white #E9E6DD | R1 p.163 (sizing for wallpaper adhesion) |
| Board face paper | **Multi-ply**: a cream "manila" top liner over 2–7 filler plies of recycled kraft and boxboard. Wraps round the long edges onto the back | ≈0.3–0.4 mm (ESTIMATE) | top ply cream #D8CFB8; filler plies grey-brown #A4977F | R10, R11 |
| Gypsum core | Set gypsum (calcium sulphate dihydrate ≥ 85 %, cellulose < 5 %), foamed, with small air voids | ≈12 mm (12.7 mm board) | fracture face #E8E4DA (range #DDD8CC–#F0EDE6); crushed powder brighter, #F2F0EA | R21; R1 p.14 (1.8 lb/ft² = 8.8 kg/m²) |
| Back paper | Grey "newsline" stock, rougher than the face. The face paper's wrapped edge shows as a cream strip along each long edge on the back | ≈0.3 mm | grey #9A958A (range #8F8B82–#A09B91) | R11 |
| Cavity | Usually empty in an interior partition (no insulation), dust on the bottom plate | 89 mm (2×4) or 140 mm (2×6), see §1.2 | dark, lit only through openings | ESTIMATE |
| Studs | Kiln-dried softwood (SPF or fir), 38 × 89 mm (2×4) or 38 × 140 mm (2×6, the default here, §1.2), **406 mm (16 in) centres** | 38 mm face | pale straw #D6B98A, aged to #C49E66; end grain darker | brief; [RB]; `doorway_studs.py` |
| Back paper → core → face paper → sizing → wallcovering | **The P face, the same build mirrored.** On P the board runs on above the 2.4 m lay-in ceiling to the top plate, **unpapered and only fire-taped or not taped at all** above the ceiling line (practice, ESTIMATE). That raw board is what the tile jump reveals | as above | raw board: cream face paper, white tape and compound strips, grey screw spots | ESTIMATE |

### 1.2 Framing round the opening

- **King studs** run plate to plate on both sides of the opening. A **jack (trimmer) stud** is nailed inside each king and carries the header. Jacks and kings are face-nailed together with 12d or 16d nails about every 300 mm (12 in), staggered (R24 [search]).
- **Header:** in a non-bearing partition a single flat 2× is legal (PNNL / IRC 2009 [search]). Framers usually built a **double 2× on edge with a 1/2 in (12.7 mm) plywood spacer**, which makes 89 mm (3-1/2 in), the width of a 2×4 wall (R24 [search]). It sits on the jacks; 16d nails through the kings go into its ends. Recommendation: **double 2×8 on edge (184 mm deep)**. In a hole it reads as two boards with a thin plywood line between them, and it matches the C scene's 190 mm block (`scene.py framing()`). ESTIMATE.
- **Cripple studs** run from the header to the top plate and keep the 406 mm layout, so the board has something to fasten to (R24 [search]).
- **Plates:** one bottom plate (cut out across the opening) and a **double top plate** (2 × 38 mm) in wood framing (practice). The C scene has a single 40 mm top plate.
- **Rough opening:** door plus about 50 mm in width and 65 mm in height (R24 [search]). For our 1.0 × 2.1 m opening: header underside ≈ 2.13 m (2.10 + a 19 mm head jamb + about 10 mm of shim gap). ESTIMATE. The C scene puts it at 2.10.
- **The wall-thickness problem (RED).** The brief says 1/2 in board on 2×4 studs. That makes a 114 mm wall (89 + 2 × 12.7). The game's wall is 0.16 m. Two honest ways to fill 0.16 m:
  - **(a) 2×6 studs (38 × 140 mm) with 1/2 in board:** 165 mm. `Kit_DoorwayStuds` already uses 140 mm members and leaves "true 2×4 + blocking" as Red's call (`doorway_studs.py` docstring). The C renders already use a 135 mm cavity.
  - **(b) 2×4 studs with two layers of 5/8 in Type X board each side:** 152 mm. That is a rated corridor or demising wall [RB §2]. Every broken edge then shows **two** boards (31.8 mm of gypsum per face, joints staggered).
  - **Default: (a).** It keeps the brief's 1/2 in board and matches the kit and the C scene. The only visual cost is a deeper cavity.

### 1.3 Board layout (where the joints are)

- **Sheets:** 4 ft (1.22 m) wide × 8, 10 or 12 ft long. The **long edges are tapered** (a shallow recess for the tape). The **ends are cut square**, with bare core at the cut (R10 [search]).
- **Direction:** perpendicular (long edges horizontal) "generally is preferred". On walls it is recommended up to 8 ft 1 in ceilings (USG 1980 p.108, R1 [read]). On our 2.9 m S wall the horizontal joints fall at about 1.68 m and 0.46 m if the top sheet is hung tight to the ceiling first (practice, ESTIMATE).
- **At openings:** keep vertical joints "at least 8 in away from external corners of windows, doors, or similar openings"; if joints occur near an opening, "apply boards so vertical joints are centered, if possible, over opening" (USG 1980 p.122, R1 [read]). Corners of openings are the most stressed points, and a seam right at them "will almost always crack" (R6, R7 [search/read]; ProBuilder 2021 says at least 6 in, R8 [read]).
- **The three real layouts, and the crack each one gives** (all three were common in 1990):

  | Layout | Board round the head | First crack you see | When to use it in C |
  |---|---|---|---|
  | **L1, by the book** | One sheet notched round the opening (an L or U cut) | Short **diagonal hairlines from the notch corners**, under the casing's outer corner | Default for both members |
  | **L2, joint over the opening centre** | A vertical butt joint at Z ≈ 0.5 from the head up | A **vertical crack straight up from the middle of the head**, along the tape | A variant for the steel member |
  | **L3, the shortcut** | A sheet edge flush with the jamb line | The crack **runs straight up along the joint** from the head corner; the tape lifts | Avoid, or use only on P |

### 1.4 Fasteners

- **Spacing on wood framing (USG 1980 p.122, R1 [read]):** wall screws **16 in (406 mm) o.c.** max for framing at 16 in; wall nails **8 in (203 mm) o.c.** Ceilings are tighter: nails 7 in, screws 12 in (USG FAQ, R1b [search]). The brief's "7–8 in" is the **nail** spacing. A 1990 commercial wall would mostly be **screwed** (Type W screws had "virtually eliminated … nail 'pops'", USG 1980 p.50). Nails stayed common in houses.
- **Double nailing** puts pairs 2 in apart at 12 in o.c. in the field (p.114, [read]).
- **Where:** in vertical rows on every stud, cripple and header face, plus along the sheet edges about 10 mm in from the edge (GA-216 practice, not read today). On the studs the rows are 406 mm apart; down each row the screws are 406 mm apart (or nails 203 mm).
- **Heads:** #6 bugle screw head 8.0–8.6 mm (0.315–0.34 in), black phosphate; drywall nail head about 7.5 mm (19/64 in) (R14 [search]). Each head is dimpled 0.5–1 mm below the face and spotted with three coats of compound: a **round spot 30–40 mm across** under the finish (ESTIMATE).
- **Strength of what holds:** nail-pull resistance of 1/2 in board is at least **77 lbf (343 N)**. Flexural strength is at least **107 lbf** across the paper grain and **36 lbf** along it (product data citing ASTM C473, R13 [search]). *Abuse-resistant* boards are classed by the soft-body energy at which the wall section fails: Level 1 at 90 ft-lbf (122 J), Level 3 at 300 ft-lbf (408 J), with a 60 lb bag on 2×4s at 16 in (ASTM C1629 via USG, R19 [search]). Regular 1/2 in board is not classed at all, so it fails below Level 1 (ESTIMATE). One Relay blow (250–550 J, [02] §6) is therefore at least 2–4.5× what breaks a regular wall section: **every Relay blow is a breaking blow for the board in the bays it reaches.**

### 1.5 Joint treatment

- **Paper tape:** 2-1/16 in (52 mm) wide, cross-fibred, with a centre crease, cream/buff #E3DAC6 (R12 [search]). It is bedded in taping compound "not over 1/32 in" under it (p.142, [read]).
- **Coats:** three coats in all. The second is feathered "approx. 2 in" beyond the first, the third "at least two inches" beyond the second (p.143, [read]). A **tapered joint** finishes 250–300 mm wide. A **butt joint** has no taper, so it is feathered "approx. 18 in" (460 mm) wide (p.144, [read]) and stands a little proud: a slight crown you see in raking light. Fastener heads get the same three coats.
- **Colours:** dried ready-mix compound is bright warm white #ECEBE6; taping compound slightly creamier #E6E1D3 (ESTIMATE). Under wallpaper none of this shows until the paper tears.
- **Inside corners** are taped and creased. **Outside corners** get metal bead (§1.6).

### 1.6 Trim, frames and corner bead

- **Wood door (L0-F).** A 3/4 in (19 mm) wood jamb on shims, nailed to the jack studs. The **casing** is nailed to the jamb edge with 4d or 6d finish nails and through the board into the jack and header with 6d or 8d finish nails every 12–16 in (300–400 mm) (R23 [search]). It is **mitred at the two head corners**, glued or not. The kit's casing is 77 mm wide and about 25 mm proud, walnut ([10] §1.2). The casing covers the board's cut edge at the opening, so **a cased wood door has no corner bead**. A caulk or paint line (1–3 mm) runs along the casing's outer edge.
- **Steel door (L0-K).** A 16 ga knock-down drywall frame, set **after** the board, wraps both faces and grips the board edges (USG 1980 p.170 [read]; [RB] §2). If the frame twists, its movement "will tend to pinch gypsum panel face paper and crush core, resulting in unsightly cracks in the finish and loose frames" (p.175, [read]).
- **Corner bead (only where present).** Galvanized steel, 1-1/4 in (32 mm) flanges, a round nose, fastened "9 in o.c. in both flanges" and finished with three coats (p.135, [read]). It sits only on **outside corners**: wall ends, cased openings without doors, soffit arrises ([RB] §3; the wall end in the Level 0 photo `ir01_dsc00161`, §7). It is **not at C's door** unless the door is within about 0.6 m of a wall end or the P-side soffit.

### 1.7 The wallcovering

- **Layer order:** substrate (paper, fabric or non-woven) → printed decoration (on the substrate or on a coating) → acrylic or vinyl coating or a laminated solid vinyl film (R15 [read]; [wallpaper_motion] §1). Machine-printed ink is only a film of colour on the sheet, so **the paper body under the print is white or cream**. That is why a torn edge shows a white fibrous rim.
- **1990 types:**
  - Residential: paper, vinyl-coated (really acrylic-coated) paper, or paper-backed solid vinyl, in rolls 20.5–27 in wide (R15, R16 [search]).
  - Commercial: **Type I** (light duty, a scrim backing), **Type II** (medium duty, an osnaburg backing; the commonest), Type III (heavy, drill cloth), in 54 in rolls. The federal spec CCC-W-408A already used Types I/II/III before 1990. Machine-direction breaking strength is 40 / 50 / 100 lb for Types I / II / III (R16 [search]). The June 1990 *Interiors* Koroseal spread on disk shows such samples with their **white woven backs** (`rb_10`, §7).
- **The game's paper** has a 0.75 m roll (seam shadow every 0.75 m, `gen_surfaces.py` `TILE_M`) and a printed duotone chevron. **Treat it as paper-backed vinyl or vinyl-coated paper** (RED to confirm; default). Then:
  - a tear shows a **white paper edge** (paper body #EDE8DA);
  - where the vinyl face peels, the **backing paper stays on the wall** (off-white #E7E2D3);
  - where the paste bond beats the board's face paper, **the board's top ply comes away with it** and the grey-brown filler plies (#A4977F, fuzzy) show.
  This last one is the familiar wallpaper-removal damage: the tear "exposes the paper layer which needs to be sealed" (R17 [search]).
- **Seams are the weak line.** Expansion and contraction of the wall "opened the seams of the wallpaper" (R27 [search]). A strip lets go first at its seam edge.

---

## 2. How it fails, layer by layer and stage by stage

### 2.1 Two load paths, two kinds of damage

C's giant does two things to the opening at once. They make different damage, and a real break shows both.

| Load | What it does | Damage it makes | Real evidence |
|---|---|---|---|
| **(a) In-plane racking.** The frame turns into a parallelogram (C: 3 → 10 mm at the head) | Shears the board round the opening | Diagonal hairlines from the head corners; cracks along tape joints; pops and pull-through at sheet edges; the casing mitre opening | Racking tests of stud walls with openings: cracks "at the wall penetrations" and over "a few fastener heads" from about **0.25 % drift**; peak load at 1–1.5 %; then fastener pull-through and taped-joint failure (R2 [read], R4 [search]) |
| **(b) Out-of-plane push.** The body drives the leaf, the jambs, the head casing and the wall above the head toward S | Bends the wall toward S. The P face is crushed at the contact points; the S face is stretched | P: crushed core and wrinkled paper at contacts, the casing driven into the board. Cavity: the P board breaks inward between studs. S: the board is pushed off its fasteners from inside, bulges, breaks into plates between studs and **blows out toward the player**, over a larger area than the P contact (an exit-side cone, as on the far side of any brittle plate; R29 [search], ESTIMATE for gypsum) | Firefighter breaches: material comes out "from stud to stud" (R18 Kiurski 2011 [read]); "most drywall is fairly easy to penetrate" [read] |

**What this means for C's numbers.** A 10 mm rack at the 2.1 m head is only a **0.48 % drift**. Racking alone would give hairline corner cracks about 300 mm long at that drift (R3 [read]: 1 in at 0.05–0.1 %, 12 in at 0.3–0.7 %), not holes. **C's holes must come from load (b).** Put them where the body pushes:
- the bays **above the header** between the cripples (Y 2.29 → ceiling);
- the **first bay beside each king stud** at 1.5–2.4 m (shoulder and forearm height for the `press` pose, [C] §1.5).

That also keeps the frame's rack small enough for the leaf collider band ([10] §1.3 item 4).

### 2.2 Stage table (tier 1 clock, [10] §2.2)

Widths are on the S face unless marked. "Drift" is head rack ÷ 2.1 m.

| Stage | Physical state | S face (player) sees | P face (Relay) | Widths and sizes |
|---|---|---|---|---|
| **S0** | Intact | Papered wall, casing, the caulk line at the casing edge | Same | — |
| **M** (0.5, 1.0 s) | Elastic; the wall springs back | Nothing on the paper. **Grit and dust** fall from the head casing's top edge and out of the head gap; at 1 m the **caulk line** at the casing edge may tick open | Paint or compound **crazes over a few fastener heads** near the casing; dust | caulk hairline 0.1–0.3 mm (BRE cat. 0, R22) |
| **D1** (1.5 s; about 0.15–0.25 % drift plus the first push) | First permanent damage. In the board under the paper: **two hairlines from each head corner** (from the board's notch corner under the casing), 25–300 mm long, 35–60° up; **2–4 pops** along the header's fastener line and the king-stud rows | The paper **bridges the hairlines**. You see a faint **crease** over each one in raking light only; the **casing mitre opens 1–2 mm**; the **nearest wallpaper seam ticks open 0.5–1 mm**; with plain paper (not vinyl) a **white hairline tear** may follow a crack | **Crushed lines** along the casing's outer edges where the casing bears into the board: the paper wrinkles in fine folds (2–5 mm pitch, ESTIMATE) | board cracks **0.1–0.6 mm**; crack length 25–300 mm (R3, R5); pops 10–15 mm across, 0.2–1 mm proud (USG: repair only pops that stand ≥ 0.005 in proud, R1 p.356) |
| **D2** (2.0 s; the body drives in) | Cracks run to the **next joint or fastener row and turn along it**. They stop growing in length and start to **widen** (R3: lengths stop near 1 % drift, widths keep growing). **Pull-through starts** on the rows nearest the head. The S board in the bays beside the kings and over the header **bulges and splits into 2–4 plates**, still held by both paper skins. Tape over the joint above the head (L2) or beside it (L3) **cracks along both tape edges and lifts** | **Bulge** 5–15 mm in the bays next to the opening (soft shading); **steps** of 1–5 mm between plates (one lit lip, one shadow); **seams open 2–5 mm** and the strip next to the head corner **lifts at its seam edge** 20–100 mm; paper **tears along plate edges** where the step passes about 2 mm, showing white fibre edges and the grey-brown under-plies; **white powder streaks** run down from the cracks; the head casing lifts 3–8 mm at one end, mitre 3–6 mm, nail shanks showing in the gap | Board crushed under the forearms or shoulders: **dents 5–15 mm deep** with wrinkled, unbroken paper; compound cracks off the casing joint in short slabs; first **inward breaks** in the bay above the head (paper torn round the contact, flaps pushed into the cavity) | cracks 0.5–3 mm plus a 0–5 mm step; tape-edge cracks 0.5–2 mm with the tape lifting 1–5 mm; pull-through craters 10–13 mm |
| **FINAL** (2.4 s) | Peak load and past it. **Whole fastener rows pull through**; plates **break out between studs and hinge on the paper**; the paper (face paper plus wallpaper) **tears last**; some plates drop. The P board in the contact bays is through, so the S board is pushed **from inside** and blows out toward S | **A burst of chunks and powder** out of one or two bays per corner. Plates swing out on paper hinges along a stud or the header; the white core rims flash in the light; a dust cloud rolls out of the bays and down the wall | Inward breaks join up; debris falls **into the cavity** and onto the P floor | plates 100–400 mm; gaps 5–40 mm; bays 368 mm wide |
| **S4** (after) | Rest | **Knocked-out bays**: 368 mm wide, 300–700 mm tall, straight sides at the studs, ragged tops and bottoms. Plates **hanging 10–60°** on paper hinges. **Rows of pull-through craters** on the studs that held. Peeled wallpaper strips curled over the holes. The casing pried 10–30 mm off the wall, maybe split at a nail line. Powder on every top edge and in a fan on the floor | Crushed contact zones, inward-torn bays, compound slabs on the floor | §3 for the debris |

### 2.3 Hairline cracks from the head corners

- **Where they start:** at the corner of the board's notch, which is under the casing, about 45 mm in from the casing's outer corner (ESTIMATE from the kit's 77 mm casing and the rough opening). **What you see is a crack coming out from under the casing's outer corner, not from the mitre tip.**
- **Shape:** gypsum has no grain. Its voids are 0.5–2 mm. The crack is **straight at the 10 mm scale** with a 1–3 mm wander over 100 mm (ESTIMATE), and the face paper's fibres make its edge slightly fuzzy under magnification. **No forks**: a corner crack ends, or it meets a fastener or a joint and turns along it.
- **Angle:** 35–60° above horizontal for racking. An out-of-plane push makes them steeper near the jamb (ESTIMATE). Ferguson's rule (quoted in R6 [search]): an **angled crack is the board itself**; a crack along a seam is a joint failing.
- **Width and length:** 0.13–0.64 mm (0.005–0.025 in) at 0.4 % drift ([search], R5); length about 25 mm at 0.05–0.1 % drift and about 300 mm at 0.3–0.7 %, then constant (R3 [read]). That is BRE category 0–1, "hairline" to "fine" (R22 [search]).

### 2.4 Cracks along tape joints and fastener rows

- **Tape joints** are a line of compound over paper tape. They crack in **two parallel straight lines 52 mm apart** along the tape edges, or down the middle of a butt joint. The tape then **lifts or bubbles** where the bond has gone (R26 [search]).
- **Fastener rows:** the compound spot over each head cracks in a **ring** 10–15 mm across. The head then pushes out (a pop). Under racking "cracking of the paint over a few fastener heads" comes with the first corner cracks (R2 [read]).
- **The header line:** the board over the head is fastened to the header (unless the hanger followed the "don't fasten to the header" advice, R7 [read]). A horizontal row of pops or a horizontal crack can run along it at Y ≈ 2.13–2.32 m.
- **Visual consequence:** the damage is **rectilinear**. It is vertical lines on the stud rows every 406 mm, horizontal lines on the header and the sheet joints, and short diagonals only at the two head corners.

### 2.5 Screw pops → pull-through → plates hanging by paper

1. **Pop.** The board moves relative to the stud. The head rises 0.1–1 mm under its compound spot. Paper over it cracks in a ring.
2. **Pull-through.** The board tears over the head: "fastener heads pulling through the back of the wallboard", or the fastener "cutting of the gypsum, or tearing out through the edge of a wallboard panel" (R2 [read]; R4 [search]). **The screw stays in the stud** with a disc or collar of crushed gypsum and a ring of torn paper under its head. **The board keeps a round crater 10–13 mm across** where the head went through.
3. **Plates.** Between studs the board spans 368 mm. Under the push the **core cracks first**: gypsum is much weaker in tension than in compression (R28 [search]). The **paper on the tension face carries the load** after the core has cracked (layered models need "one curve of the base paper in tension", R28 [search]). So the board breaks into plates that **hinge on their paper** along the stud rows and the header.
4. **Paper last.** The plate swings out on its paper hinge, and the wallpaper over it stretches with it. Only then does the paper tear, leaving **fringes 2–15 mm** past the gypsum edge (ESTIMATE). "Once the paper face is broken … the gypsum behind crumbles under any sideways pressure"; the impact "breaks the paper for an inch in every direction" (repair guidance, R17b [search]).
5. **Plate edges.** Gypsum fractures straight across the thickness, within about ±20° of square (ESTIMATE). The edge is rough and granular, white, with voids 0.5–2 mm. The paper skins are on both faces of the edge.

### 2.6 Crushed gypsum at the impact zone

- On the **struck face** (P, or wherever a body or the casing bears) the core **crushes** rather than cracks. You get a dent 5–15 mm deep. The paper wrinkles in fine parallel folds but does not tear until the dent is deep. The crushed core is a soft white powder that **leaks out of every nearby crack** as white streaks.
- The steel frame does the same to the board edges it grips: it pinches the face paper and crushes the core (USG p.175, [read]).
- In a firefighter breach this crushed material is the first thing that comes out. The drill is "messy" and needs a way to dispose of the debris (R18 Kiurski [read]).

### 2.7 The wallpaper

| Mechanism | What you see | Size |
|---|---|---|
| Bridging | A crease or slight ridge over a crack; no colour change | the crack's length |
| **Seam opening** | The butt seam parts; a pale line shows (primer or sizing, the paper's white edge) | 0.5–5 mm |
| Seam lift | The strip's edge lifts and stands off the wall. The **backing** shows on its underside | 20–100 mm lift |
| **Tear over a plate edge** | A white fibrous edge (paper body), with the print stopping 0.2–1 mm short of it | fibre fringe 0.5–2 mm |
| **Strip peel** | A whole strip (0.75 m wide in the game) lets go from the seam inward, often taking the board's top ply. You see grey-brown fuzzy under-plies where the top ply went, cream face paper where it held, white backing paper where the vinyl face split off | peeled areas 0.1–0.6 m² |
| **Curl** | A freed strip **curls** with its backing on the inside of the curl (ESTIMATE). Tight curls at a torn end, a loose roll on a long strip | curl radius 10–40 mm (ESTIMATE) |
| Delamination on chunks | Chunks keep their wallpaper. The paper usually overhangs the gypsum edge by 2–15 mm | — |

What it is **not**: camouflage-like blotches of brown on cream. The exposed layers have **crisp boundaries**: the edge where the top ply tore, where the vinyl split, where the paste held. Each zone has one flat colour with a fibrous texture.

### 2.8 The casing

1. The **caulk or paint line** at the casing's outer edge ticks open (M).
2. The **head mitre opens** (D1, 1–2 mm). It opens at the outer (heel) end first if the casing rotates about its nails, at the inner (toe) end if the jamb racks (ESTIMATE). Mitres open even without force as wood moves seasonally (R23 [search]).
3. The head casing **lifts at one end** (D2, 3–8 mm). The **finish nails show** in the gap: thin bright or dark shanks, 1.6–2 mm (ESTIMATE for 6d/8d finish nails).
4. The casing **pries off** (FINAL, 10–30 mm). Each nail either pulls through the casing (the casing slides off over the heads, which stay in the wall) or pulls out of the wall (the nail stays in the casing and sticks out of its back 25–40 mm).
5. If bent, the casing **splits along the grain** through its nail line. The split shows **raw wood** lighter than the stain (walnut: raw #9C7A55 under the dark stain). Splinters 20–150 mm long, 1–5 mm thick.

### 2.9 Corner bead (only if present)

- At an outside corner close to the break, the bead takes the hit on its **nose**. The nose **dents and kinks**, the compound over its flanges **cracks off in slabs** and exposes the galvanized, perforated flange (#A7ABAA, spangled). "Dents, crushed nosing and chipped compound at hip height or near doorways" are the usual impact signs (R25 [search]). The bead can also pop off its fasteners (9 in o.c.) and bow out along its length.
- C's test door has no outside corner within reach, so this is a **dress option** only for a breakable door near a wall end or the P soffit.

### 2.10 What a hole exposes

| Part | What it looks like |
|---|---|
| **Studs** | 38 mm faces at 406 mm, pale straw wood, a few screws standing in each with their gypsum collars, pencil or crayon marks, a generic grade stamp (no brand, era lock) |
| **Jack + king at the jamb** | Two studs side by side: 76 mm of wood next to the opening; nail heads every 300 mm, staggered; shims visible as thin wedges between the jack and the jamb back |
| **Header** | Double 2× on edge with a 12.7 mm plywood line (plies visible on its end), 16d nail heads through the king into its end |
| **Cripples** | At layout above the header to the double top plate |
| **The far board's back** | Grey back paper, a **cream strip** where the face paper wraps each long edge, and **"shiners"**: screw tips poking through where a screw missed the stud (ESTIMATE, common practice). Lit from the hole: brightest near the hole, falling off into the bay corners. **Never black** |
| **Cavity floor** | Gypsum crumbs and powder heaped on the bottom plate in each opened bay |
| **Optional** | An NM cable stapled to a stud, an outlet box (only if the map has an outlet there; the 1990 NM jacket was white or cream) |

---

## 3. The debris

### 3.1 Sizes and counts (one face, one knocked-out bay, ESTIMATE)

Impact fragmentation of a brittle plate gives a few big pieces and many small ones (roughly a power law). Here the paper holds the big plates together, so the curve is steeper than for glass.

| Class | Size | Count per opened bay | Share of mass | Shape and faces | Where it ends up |
|---|---|---|---|---|---|
| **Plates** | 100–400 mm | 2–5 | 60–80 % | Polygons with straight-ish fractured edges, 12.7 mm thick; wallpaper on the room face, grey back paper behind; paper fringes; round pull-through craters along one edge | Hanging on paper hinges, or flat at the wall base within 0.3 m (face up or down about 50/50) |
| **Chips** | 15–100 mm | 15–40 | 15–30 % | Chunky; paper on one or both faces or none; bright white edges | Within 0.6 m of the wall base; a few bounce to 1 m |
| **Crumbs** | 2–15 mm | 100–300 | 3–8 % | Granular gypsum, no paper | Along the base line and in the carpet pile |
| **Powder** | < 0.1 mm | — | 1–3 % | Fine white dust | Air (§3.3), then every horizontal surface |
| **Paper bits** | 5–80 mm | 5–20 | ≈0 | Torn wallpaper and face-paper scraps, curled, light | Within 1 m; some cling to the wall below the hole |

### 3.2 What a chunk looks like

- **Thickness** exactly 12.7 mm (two boards per face with wall option (b), §1.2).
- **Room face:** the room's own wallpaper print. The chevron on an S chunk matches the S wall, which is the strongest cue that the chunks came out of that wall. Under the wallpaper, a cream face paper rim.
- **Back face:** grey back paper, or bare gypsum where the back paper stayed on a stud.
- **Edges:** white, granular, slightly crumbled (not serrated or regular), with tiny voids. Paper fringes stand past the gypsum on one or both faces.
- **Marks:** round pull-through craters (10–13 mm) along the edge that sat on a stud; a strip of joint tape and a compound crown on a chunk from a joint.

### 3.3 Powder: fans and settling

- **Composition:** at least 85 % calcium sulphate dihydrate (R21 [search]). It is very white and very matte.
- **Settling:** a 100 µm particle falls at about 0.3 m/s; fine fractions "can remain suspended in air for hours" (NIOSH, MSHA, R20 [search]). **Coarse grit lands within a second; a pale haze hangs for tens of seconds to minutes** (ESTIMATE for a closed room). In the game: a dense puff that drops in 0.5–1 s, then a thin veil that thins over 10–30 s and does not vanish in one frame.
- **On the floor:** a **fan from the wall base**, densest in a 0.1–0.2 m band along the base, thinning out to 0.6–1.5 m (ESTIMATE). Its edge is soft, it whitens the carpet tips (the pile looks frosted) and a ridge collects along the base itself. Not two round blobs in the middle of the floor.
- **On the wall:** **white streaks** running down from cracks and hole bottoms where powder slid down the paper; a powder film on the **top edge of the base, the casing head and the leaf's top edge**.
- **In the cavity:** a heap on the bottom plate in each opened bay.

### 3.4 Wood

- **Casing:** splinters and a split strip (§2.8); finish nails on the floor or sticking out of the casing.
- **Framing:** studs rarely break under body loads. Their **edges splinter** where a plate tore off a screw line (fibres 5–30 mm). At FINAL a 16d nail may withdraw at the header–king joint and leave a 2–10 mm gap (ESTIMATE).
- **Jamb (L0-F only):** A's strike-side split ([02] §2.1) may join C's break. Splinters along the grain, 50–300 mm.

---

## 4. What light does at a breach

1. **A crack in one face is dark.** It opens into the cavity, and the cavity has no light source. Light from the far room comes through only where **both faces are open in the same bay**. Even then it arrives as a **patch bounded by the studs**, not as specks along the crack lines.
2. **The white core rim is the brightest thing in the breach.** Gypsum's albedo is about 0.8–0.9 against about 0.4–0.5 for the wallpaper (ESTIMATE). Wherever a plate edge or a hole edge faces the light, a **12.7 mm white band** shows. At 3 m that is a 3 px rim.
3. **Raking light from the troffers** (above and in front) lights the **top lips** of steps and the **upper edges** of hanging plates, and puts the **lower lips** in shadow. A crack with a step therefore reads as a light-over-dark pair, not as a black line.
4. **The hole interior is a gradient, not black.** Light enters through the hole itself and falls off with depth: bright on the stud faces near the front edge, mid-grey on the far board's back 89–140 mm behind, darkest in the bay corners and under the header. ESTIMATE: 2–4 stops below the lit wall.
5. **Torn paper glows.** Thin paper fringes and the curled wallpaper are slightly translucent. Back-lit (through-hole case, L-B), they glow warm at their edges.
6. **Dust shows the light.** Airborne gypsum is very white and scatters strongly. Every blow throws a puff that **lights up in the troffer cones** and, when both faces are open, in the beam through the hole. Dust also softens the contrast of everything behind it for a few seconds.
7. **Through-holes in L-B.** With the S room dim and the P room lit (or the reverse), a two-face breach throws a **beam** shaped by the bay and the hole outlines. The leaf-gap glow ([02] §4) stays the main light cue before the break.

---

## 5. What the current C frames get wrong, checked against the physics

Measured on `db_C_L0F_burst_S4_V1_LA.jpg`, `_S4_V2Hf50_LA.jpg`, `_S4_V5f62_LA.jpg`, `dir_C_strip.jpg` and in `crackgen.py`. Nothing was re-rendered.

| Red's / the visual chat's point | What the frame does | Why it reads fake | The real thing |
|---|---|---|---|
| Thick dark branching "tree" cracks | `crackgen.py`: crack core 2.6 mm (D1) → 8.5 mm (FINAL) at the corner, a darkened lip band about 7.5 mm (5 map texels at 1.5 mm), gaping +3 to +9 mm per side near the corners; forks with p = 0.3; hairline branches drawn **across intact wallpaper** | Glass crack-graph logic; 5–25× too wide; on a papered wall a crack does not draw on the paper | Hairlines 0.1–0.6 mm under the paper (§2.3); a grid of joint and fastener lines (§2.4); steps, not lines (§4.3) |
| Holes are flat black blobs | Round-topped holes about 0.3 × 0.47 m at both head corners, black inside, one lit shelf | No 12.7 mm white rim; no studs, screws or far board; round outline | Bays 368 mm wide with straight stud sides, white rims, screws with collars, the lit grey back of the far board (§2.10, §4.4) |
| Wallpaper reads like camouflage cloth | Noise-blended cream and brown blotches over 0.6 × 0.8 m zones; no curl | Blotches have no edges and no layer logic; no white tear edges; no backing; no curl | Zones with crisp boundaries: white backing, cream face paper, grey-brown under-plies; white fibre edges; strips peeling from the 0.75 m seams and curling (§2.7) |
| Floor debris like paper flakes | Thin chips with regular serrated white edges and brown-mottled faces; white zigzag ribbons for strips; powder as two round blobs mid-floor | Chips too thin, serration too regular, faces not the wall's own print; strips not paper-like; powder not where gravity puts it | 12.7 mm plates with the chevron on their face and grey backs, crumbly white edges; most within 0.5 m of the base; a powder fan from the base line (§3) |
| Little dust; edges too clean; weak scale | Dust mostly in the air at FINAL; clean plate edges; no screw, stud, tape or bead detail at human scale | No small known-size details to judge scale against | Scale anchors: 406 mm stud rhythm, 52 mm tape, 8 mm screw heads, 12.7 mm board, 0.75 m wallpaper strips; dust film on every top edge (§10) |
| Light through cracks as white specks (hero, L-B) | S-room light shows through the open cracks round the head | Cracks in one face open into a dark cavity | Light only through two-face breaches, shaped by the bay (§4.1) |

---

## 6. Rules for the rebuild (for the visual chat's next C build)

These turn the research into generator rules. They replace `crackgen.py`'s free crack graph and keep C's stage clock, staging and collider rules ([10] §1.3).

1. **Lay out the wall first.** Studs, cripples, header and plates at 406 mm (§1.2). Sheet joints per layout L1 (default) (§1.3). Fastener rows on every framing member, screws at 406 mm on walls (or nails at 203 mm). Wallpaper seams every 0.75 m. Every later crack and break snaps to this layout.
2. **D1 = 4 hairlines + pops.** Two hairlines per head corner from the board's notch corners under the casing: 25–300 mm long, 35–60°, **0.1–0.6 mm wide**, no forks. They live in the board layer and show only as a crease in the wallpaper's normal. Add 2–4 pops on the header and king rows, the mitre at 1–2 mm and one seam at 0.5–1 mm.
3. **D2 = cracks turn and widen; plates form.** Corner cracks end on the nearest joint or fastener row and run along it. Lengths stop; widths grow to 0.5–3 mm with 1–5 mm steps. The bays above the header and beside the kings bulge and split into 2–4 plates along the stud rows and the header line. Tape edges crack. Wallpaper: seams open 2–5 mm and one strip per corner starts to peel and curl.
4. **FINAL/S4 = whole bays.** Knock out 1–2 bays per corner (368 mm wide, 300–700 mm tall), bounded by the studs at the sides; plates hang on paper hinges at 10–60°. **No round holes.**
5. **Every broken edge has three layers.** Paper skin (wallpaper + face paper), 12 mm white core, back paper. Fringes 2–15 mm. The core is the brightest material in the breach.
6. **The wallpaper is its own layer.** It bridges hairlines, opens at seams, tears with white fibre edges, peels in 0.75 m strips with a backing underside and curls. It is never drawn as a dark line or as blotches.
7. **The cavity is built and lit.** Studs with screws and gypsum collars, the header with its plywood line, the far board's grey back with the cream edge wraps, crumbs on the bottom plate. Never a black card.
8. **Debris is cut from the same wall** (the chevron on the face) and is 12.7 mm thick. Distribution as §3.1. Most of it lies within 0.5 m of the base; the powder fans out from the base line.
9. **Dust is staged.** A grit fall on every blow (M on), puffs from new cracks (D1 on), a burst and a hanging veil at FINAL, a powder film on top edges at S4.
10. **Light honestly.** No light through single-face cracks. Through-holes only where both faces are open in the same bay.
11. **The 12 m read is areas** (§10.2): peeled strips, the bulge, hanging plates, white-rimmed bays, the dust veil and the floor fan.
12. **P face:** crushed contact zones (dents, paper wrinkles), inward-torn bays with flaps pushed into the cavity, the raw unpapered board above the 2.4 m tiles.

---

## 7. Media

### 7.1 Real evidence already on disk (credited in `SOURCES.md`)

There is **no real photo or clip of broken drywall on disk**. `Research/week01–03` and every `Documentation/research/*/media` folder were checked on 2026-10-08. What is on disk shows the **intact** build that has to break:

| File | What it proves here |
|---|---|
| `Research/week02/ip-research/stills/ir01_dsc00161_20020612_082113.jpg` (the Level 0 photo, Commons) | Level 0's partitions are **papered drywall**: a residential stripe-and-chevron paper, an outside corner at a wall end (where a corner bead would be), an outlet. The IP's wall is the wall this doc describes |
| `Research/week02/ip-research/stills/ir01_dsc00159_20020612_082017.jpg` (Commons) | Painted drywall arches and soffits with outside corners: where corner bead lives, not at a cased door |
| `Research/week02/kit-references/a24/a24_trailer_0115_blue_painters_tape.jpg` (A24 *Backrooms*, ©) | A papered partition under a lay-in ceiling in the IP's newest canon; the source of `Kit_DoorwayStuds` |
| `../interactables/proposal/media/kane_emg_0053_oak_door_lever.jpg` (Kane Pixels, ©) | The IP's wood door in a papered partition: a flat mitred casing round the opening, the paper running to the casing edge |
| `../level_transitions/images/rb_10_interiors9006_p048_049_vinyl_wallcovering.jpg` (*Interiors*, June 1990, ©) | 1990 commercial vinyl wallcovering samples; a curled sample shows its **white woven backing** |
| `images/db_C_L0F_burst_S4_V1_LA.jpg`, `_S4_V2Hf50_LA.jpg`, `_S4_V5f62_LA.jpg`, `dir_C_strip.jpg` (ours) | The frames Red judged; the diagnosis in §5 |

### 7.2 Wanted (not downloaded)

Listed with page URL, size and what each proves in `media_candidates.md` §21: public-domain FEMA and USAF photos of real broken and removed drywall, CC photos of board layers, peeling paper and casing, the 1980 USG handbook pages, the CUREE W-15 / W-23 reports, and nine internal-viewing clips. The clips are a real person falling through a stud wall, hairline cracks above door heads, a shake-table house, and a fire-service wall breach. **The two most useful** are a clip of a real body going through a stud wall (`dv04`, 31 s) and the W-15 report's damage photos (`dw15`). Together they would settle the bay shape, the plate hinges and the crack widths that are still ESTIMATES.

---

## 8. Open items and UNVERIFIED

1. **No photo was seen of a real body-sized breach** in a papered stud wall. The bay-bounded shape, paper hinges and exit-side blow-out come from the mechanics (R2, R18, R28, R29) and repair or breach practice, not from a photo. The first media to approve is `dv04` / `dw01` (§7.2).
2. The 0.005–0.025 in corner-crack widths are from a search summary of a secondary review (R5). The original source was not identified. W-23's lengths (R3) and W-15's sequence (R2) were read in their abstracts only.
3. Paper thicknesses, the core's fracture angle, curl radii, debris counts and mass shares, and the powder fan size are ESTIMATES.
4. **The game's wallpaper type** (paper vs paper-backed vinyl vs Type II fabric-backed) is not decided anywhere. It changes the tear edge (white fibre vs backing vs woven fabric). Default here: paper-backed vinyl. RED.
5. **Wall build (a) 2×6 vs (b) 2×4 with double 5/8 in board** (§1.2) is RED. Default (a).
6. Whether 1990 hangers fastened the board to the header varies. Both happen; the rows of pops on the header line assume they did.
7. Above-ceiling finish on P (raw, fire-taped or untaped) is practice, not sourced.
8. GA-216 edge distances were not read today.
9. The USG 1980 handbook was read through its OCR text (archive.org). Page numbers are the printed ones quoted in that text. The spacing table on pp.115–117 was garbled in the OCR, so its values come from the p.122 text and USG's FAQ.

---

## 9. Sources

All read or searched on 2026-10-08. "PD" = public domain.

| # | Source | Status | Licence |
|---|---|---|---|
| R1 | United States Gypsum Co., *Gypsum Construction Handbook*, 1980, Building Technology Heritage Library. https://archive.org/details/GypsumConstructionHandbook (text: https://archive.org/stream/GypsumConstructionHandbook/Gypsum%20Construction%20Handbook_djvu.txt). Pages used: 10, 14 (1.8 lb/ft²), 50, 55 (nail pops), 108 (perpendicular preferred), 114 (double nailing), 122 (screws 16 in, nails 8 in on walls; joints ≥ 8 in from opening corners; centred over openings), 135 (corner bead 9 in o.c., three coats), 142–144 (coats, feathering, butt joints 18 in), 163 (sizing for wallpaper), 170–176 (KD frames; "pinch gypsum panel face paper and crush core"), 345–357 (problems: pops ≥ 0.005 in, ridging, cracks at openings) | [read] (OCR text) | Public Domain Mark 1.0 |
| R1b | USG FAQ on fastener spacing (nails 7 in ceilings / 8 in walls; screws 12 / 16 in). https://assemblies-tools.usg.com/content/usgcom/en/resource-center/faqs/848.html | [search] | © USG |
| R2 | K. M. McMullin, D. Merrick, *Seismic Performance of Gypsum Walls: Experimental Test Program*, CUREE-Caltech Woodframe Project Report W-15, 2002. https://purl.stanford.edu/zb066nc1865 | [read] abstract | © CUREE |
| R3 | G. G. Deierlein, A. M. Kanvinde, *Seismic Performance of Gypsum Walls: Analytical Investigations*, CUREE-Caltech W-23, 2003. https://purl.stanford.edu/dh482dr0123 | [read] abstract | © CUREE |
| R4 | "Seismic Damage Thresholds for Gypsum Wallboard Partition Walls", *J. Archit. Eng.* 13(1):22, ASCE, 2007. https://ascelibrary.com/doi/10.1061/%28ASCE%291076-0431%282007%2913%3A1%2822%29 | [search] abstract (page 403) | © ASCE |
| R5 | Review in *Buildings* 2(3):173, MDPI, 2012 (corner cracks 0.005–0.025 in at 0.4 % drift, citing a shear-wall study). https://www.mdpi.com/2075-5309/2/3/173 | [search] (page 403) | CC BY |
| R6 | "Tattletale cracks", *Fine Homebuilding*, 2013-01-02. https://finehomebuilding.com/2013/01/02/tattletale-cracks (M. Ferguson's angle rule quoted in search summaries) | [search] (page 500) | © Taunton |
| R7 | West Fraser, "Tips for Installing Drywall around Openings", 2017-02-06. https://osb.westfraser.com/blog/tips-for-installing-drywall-around-openings | [read] | © West Fraser |
| R8 | G. Davis (IBACOS), "Framing Best Practices That Help Prevent Drywall Cracks", *Pro Builder*, 2021-12-13. https://www.probuilder.com/construction/quality-matters/article/55198431/framing-best-practices-that-help-prevent-drywall-cracks | [read] | © Pro Builder |
| R9 | "Installing Drywall Around Windows and Doors", *JLC*. https://www.jlconline.com/?p=68654 | [search] (page 403) | © JLC |
| R10 | National Gypsum, Gold Bond gypsum board submittal (face paper folded round the long edges, ends square-cut). https://constructioninstruction.com/wp-content/uploads/2020/11/Gold-Bond-Gypsum-Board-Submittal.pdf | [search] | © National Gypsum |
| R11 | Facer-paper patents: cream manila face, grey newsline back, filler plies of kraft and boxboard cuttings. US 4,020,237 https://patents.google.com/patent/US4020237/en ; https://patents.justia.com/patent/20180340297 | [search] | public patents |
| R12 | Paper joint tape 2-1/16 in, cross-fibred, centre crease: National Gypsum ProForm catalogue https://pdf.archiexpo.com/pdf/national-gypsum/proform-paper-joint-tape/61718-133903.html ; USG listing (ASTM C475) https://www.ringsend.com/2-1-16-x-250-sheetrock-drywall-tape-srtape | [search] | © makers |
| R13 | 1/2 in board properties (flexural 107 / 36 lbf, nail pull 77 lbf, hardness 11–15 lbf, ASTM C473/C1396): Gypsum Association properties sheet https://inspectapedia.com/interiors/Gypsum-Board-Properties-Gypsum-Association.pdf ; ICC-ES ESL-1346 https://icc-es.org/wp-content/uploads/report-directory/ESL-1346.pdf | [search] | © GA / ICC-ES |
| R14 | Fastener heads: Grabber ring-shank drywall nails (0.297 in head) https://grabberpro.com/userfiles/documents/pg1000-fastener-solutions/drywall-nails/ps1065-grabber-ring-shank-galvanized-drywall-to-wood-nails.pdf ; Dottie #6 bugle screws (0.315–0.330 in) https://s3.amazonaws.com/s3.supplyhouse.com/product_files/LH-Dottie-DWSBX6158-Product-Overview.pdf | [search] | © makers |
| R15 | "Wallpaper Anatomy" (information credited to Graham & Brown). https://wallpaperinstaller.com/anatomy.html | [read] | © site |
| R16 | Vinyl wallcovering types and backings: Koroseal "wc backings" https://koroseal.com/getmedia/ac363056-da0a-4148-abc4-fcbfc08d1ecf/wc-backings ; Koroseal spec citing CCC-W-408A https://koroseal.com/getmedia/650a6c03-8cf9-4fd5-b92b-772bf0cfedbb/wc-architectural_specification_09_72_00-1 ; Vescom CCC-W-408D test report (breaking strength 40/50/100 lb) https://www.vescom.com/download/10925_Albert%201103%20-%20CCC%20W%20408D%20type%20II.pdf | [search] | © makers |
| R17 | Wallpaper removal tearing the board's face paper: J. Geerling, 2016 https://www.jeffgeerling.com/blog/2016/diy-project-removing-floor-ceiling-mirrors-wall-our-houses-dining-room/ ; *Tampa Bay Times*, "Repair that drywall paper rip", 2013-04-14 https://www.tampabay.com/archive/2013/04/14/repair-that-drywall-paper-rip (text not served) | [search] | © authors |
| R17b | Repair guidance on paper as the tensile skin and core crumbling beyond the paper break: https://compositepaint.com/fix/holes-in-drywall/ ; Gyproc South Africa repair sheet https://www.gyproc.co.za/assets/download/media/3141 | [search] | © authors |
| R18 | *Fire Engineering*: T. Kiurski, "Training Days: Wall Breach Maneuver", 2011-07-19 https://www.fireengineering.com/firefighter-training/training-days-wall/ ; G. Havel, "Construction Concerns: Breaching Walls—Frame and Drywall Board", 2017-04-06 https://www.fireengineering.com/fire-safety/construction-concerns-breaching-walls-frame-and-drywall-board/ ; B. Ward, "Construction Hazards: Breaching HI-Impact XP Wallboard with Fiberglass Mesh", 2009-04-06 https://www.fireengineering.com/firefighting/construction-hazards-breaching-hi-impact-xp-wallboard-with-fiberglass-mesh ; M. Ciampo, "The Punch Technique", 2008-11-01 https://www.fireengineering.com/?p=213364 | [read] | © Fire Engineering |
| R19 | USG, "Abuse and impact resistance" (ASTM C1629 soft-body levels 90 / 195 / 300 ft-lbf, a 60 lb bag on 2×4s at 16 in). https://www.usg.com/content/usgcom/en/blog/abuse-and-impact-resistance-wallboard.html | [search] | © USG |
| R20 | Dust settling: NIOSH slides (100 µm ≈ 0.3 m/s) https://Cdc.gov/niosh/docket/archive/pdfs/NIOSH-219/0219-092310-baron.pdf ; MSHA silica sheet (fines suspended for hours) https://www.msha.gov/sites/default/files/Silica%20Exposure.pdf | [search] | PD (US gov) |
| R21 | USG gypsum panels SDS (CaSO₄·2H₂O ≥ 85 %, cellulose < 5 %). https://www2.pcad.edu/Facilities/health_safety/SDS/Maintenance/US%20Gypsum%20Co/gypsum-panels.pdf | [search] | © USG |
| R22 | BRE, "Assessing cracks in houses" (Digest 251 categories: 0 < 0.1 mm, 1 ≤ 1 mm, 0–2 ≤ 5 mm aesthetic). https://bregroup.com/insights/assessing-cracks-in-houses/ | [search] | © BRE |
| R23 | Casing: *Fine Homebuilding*, "Case a door with mitered trim" https://www.finehomebuilding.com/project-guides/finish-trim-carpentry/case-a-door-with-mitered-trim ; "A guide to mitered joints that won't open" https://www.finehomebuilding.com/project-guides/finish-trim-carpentry/a-guide-to-mitered-joints-that-wont-open ; Home Depot install sheet (8d at 12–16 in) https://images.homedepot-static.com/catalog/pdfImages/73/7340b4f6-c25e-4fc2-9237-2a0515f32e3a.pdf | [search] | © authors |
| R24 | Headers and cripples: PNNL Building America, "Advanced framing: insulated headers" https://basc.pnnl.gov/resource-guides/advanced-framing-insulated-headers ; framing guides (double 2× with a 1/2 in ply spacer; jacks to kings 12d/16d at 12 in) | [search] | PD (US DOE) / © |
| R25 | Corner bead damage: Trim-Tex https://trim-tex.com/blog/a-guide-to-repairing-drywall-corner-bead ; https://compositepaint.com/fix/corner-bead-damage/ | [search] | © authors |
| R26 | Tape bubbling and lifting: https://www.hunker.com/13418498/how-to-fix-a-bubble-in-drywall-tape/ ; *Fine Homebuilding* forum https://finehomebuilding.com/forum/bubbles-in-drywall-tape | [search] | © authors |
| R27 | Wallpaper seams opening with wall movement: Money Pit https://www.moneypit.com/how-repair-wall-cracks-drywall/ ; FHB forum https://finehomebuilding.com/forum/wallpaper-seams | [search] | © authors |
| R28 | Gypsum board in bending (core weak in tension; base paper modelled in tension after cracking): Tokyo Tech / Takenaka study https://t2r2.star.titech.ac.jp/rrws/file/CTT100885751/ATD100000413/ ; "Mechanical Properties of Plasterboards", *J. Mater. Civ. Eng.* https://ascelibrary.com/doi/10.1061/%28ASCE%29MT.1943-5533.0001630 | [search] | © authors |
| R29 | Exit-side cone ("cratering") in brittle media: AFTE glossary https://afte.org/?p=2750 | [search]; transfer to gypsum is ESTIMATE | © AFTE |

Project files read (read-only): `10_design.md`, `02_real_door_failure.md`, `20_dir_C.md`, `media_candidates.md`, `../level_transitions/02_real_buildings.md`, `SOURCES.md` there and in `../interactables/proposal/media/`, `Research/week02/ip-research/SOURCES.md`, `Research/week02/kit-references/SOURCES.md`, `../wallpaper_motion/10_synthesis.md`, `Tools/lookdev/gen_surfaces.py` (`wallpaper_fields`, `TILE_M`), `Tools/Blender/frontrooms_kit/assets/doorway_studs.py` (docstring), `$W/door_break/C/scripts/crackgen.py` (`STAGE`, `face_stage`) and `scene.py` (`framing`, materials).

---

## 10. Visual-cue checklist

### 10.1 Numbered cues

**Thickness and dimensions**
1. Board 12.7 mm (core ≈ 12 mm; each paper skin ≈ 0.3–0.4 mm). Option (b) walls: two 15.9 mm boards per face.
2. Wallpaper 0.15–0.3 mm (paper) or 0.3–0.6 mm (vinyl); roll 0.75 m (game).
3. Stud face 38 mm; studs at 406 mm centres; clear bay 368 mm; cavity 140 mm (2×6, default) or 89 mm (2×4).
4. Jack + king at the jamb: 76 mm of wood. Header: double 2×8 on edge, 184 mm deep, with a 12.7 mm plywood line. Double top plate 76 mm.
5. Tape 52 mm wide; tapered joint finish 250–300 mm wide; butt joint finish about 460 mm wide with a slight crown.
6. Fastener heads 7.5–8.6 mm; compound spots 30–40 mm; pops 10–15 mm; pull-through craters 10–13 mm.
7. Casing (kit) 77 mm × 25 mm; finish nails 1.6–2 mm shanks at 300–400 mm along the casing.
8. Corner bead (only at outside corners): 32 mm flanges, round nose, fasteners 9 in (229 mm) o.c.

**Colours** (sRGB hex, for albedo; ranges are ESTIMATE unless sourced)
9. Gypsum core fracture **#E8E4DA** (range #DDD8CC–#F0EDE6); crushed powder brighter, **#F2F0EA**.
10. Board face paper: cream top ply **#D8CFB8**; grey-brown filler plies **#A4977F** (fuzzy).
11. Board back paper grey **#9A958A**, with a cream wrap strip (#D8CFB8, 20–30 mm) on each long edge.
12. Joint compound **#ECEBE6**; taping compound **#E6E1D3**; paper tape **#E3DAC6**.
13. Wallpaper print: ground #D2C27C / mid #AC9A52 / deep #766A34 / cream #E3D594 (the game's). Torn paper edge **#EDE8DA**; backing paper **#E7E2D3**; woven fabric backing (Type II) **#D9CFB9** with a 0.8–1.2 mm weave.
14. Sizing or primer under the paper **#E9E6DD**, with glossy paste smears.
15. Studs and header: kiln-dried softwood **#D6B98A** (aged **#C49E66**); plywood edge plies alternating light and dark at about 3 mm.
16. Screws black phosphate **#2A2A2A**; nails bright or galvanized **#8D8F8C**; corner bead galvanized **#A7ABAA**.
17. Casing raw wood under the stain: walnut **#9C7A55** (or pine #D9BE8E if the casing is stained pine).
18. Powder on carpet: carpet albedo lifted 10–40 % toward #F2F0EA, densest at the base.

**Roughness** (0 = mirror, 1 = fully matte)
19. Gypsum fracture 0.92–0.97; powder 1.0; board papers 0.85–0.9.
20. Wallpaper: paper 0.8; vinyl 0.45–0.6 (a soft sheen); paste smears 0.4–0.5.
21. Compound 0.85; studs 0.7–0.8; galvanized bead and nails 0.35–0.5; screws 0.5.

**Crack widths by stage** (board layer; the wallpaper shows them only as in §2.7)
22. **M:** board < 0.1 mm (invisible); caulk line 0.1–0.3 mm.
23. **D1:** corner hairlines 0.1–0.6 mm × 25–300 mm; tape-edge hairlines 0.1–0.5 mm; mitre 1–2 mm; one seam 0.5–1 mm.
24. **D2:** cracks 0.5–3 mm plus steps of 1–5 mm; tape lifting 1–5 mm; seams 2–5 mm; mitre 3–6 mm; casing gap 3–8 mm; bulge 5–15 mm.
25. **FINAL / S4:** plate gaps 5–40 mm; bays 368 × 300–700 mm; plates hanging 10–60°; casing off 10–30 mm.
26. **Never** a dark crack line wider than about 3 mm unless the plates on either side have separated or stepped.

**Distribution**
27. Debris: 60–80 % of the mass in 2–5 plates per bay; most within 0.5 m of the base; chips to 1 m; powder fan 0.6–1.5 m out from the base line.
28. Hanging plates hinge on the stud rows or the header line, never on a random edge.

### 10.2 How the cues read at 1 m, 3 m and 12 m

Game camera: 76° vertical FOV, 1080 px tall. Pixels per metre = 691 / distance. So 1 px = 1.45 mm at 1 m, 4.34 mm at 3 m and 17.4 mm at 12 m. Lines need about 1.5 px with good contrast to read steadily under TAA. Areas need about 4 px.

| Cue | 1 m (0.69 px/mm) | 3 m (0.23 px/mm) | 12 m (0.058 px/mm) |
|---|---|---|---|
| Corner hairline 0.3 mm (D1) | 0.2 px: a faint broken line only in raking light, or as the paper crease | Invisible | Invisible |
| Caulk line / seam 0.5–1 mm | 0.3–0.7 px: reads as a thin line | Invisible (seam shadow already in the texture) | Invisible |
| Mitre 1–6 mm | 1–4 px: reads | 0.2–1.4 px: reads only at 6 mm | Invisible |
| Pops / craters 10–15 mm | 7–10 px dots: read, with a ring | 2–3 px: read as a dotted row in raking light | Invisible |
| Steps 1–5 mm with lit lip and shadow | Read clearly | Read as a light-over-dark pair if ≥ 3 mm | Invisible singly; a cluster reads as rough shading |
| White core rim 12.7 mm | 9 px band: reads | 3 px: **reads**, the main 3 m cue | 0.7 px: sparkle only |
| Tape lifting, tape edges (52 mm) | Reads | 12 px: reads as a pale strip when lifted | 3 px: marginal |
| Peeled wallpaper strip 0.75 m wide | Fills the view | 173 px | **43 px: reads** |
| Bulge 5–15 mm across a bay | Soft shading | Soft shading if lit from the side | Reads only as a silhouette change at the head |
| Knocked-out bay 368 × 500 mm | Full detail (studs, screws, far board) | 85 × 115 px | **21 × 29 px: reads**, with a white rim and a mid-grey (not black) inside |
| Hanging plate 200–400 mm | Reads with its paper hinge | 46–92 px | **12–23 px: reads** as a tilted pale shape |
| Dust veil / puffs | Fine particles visible | Soft haze, reads in the troffer cones | **Reads** as a pale veil over the head |
| Powder fan 0.6–1.5 m on the floor | Frosted pile, a ridge at the base | Reads | **35–86 px: reads** |
| Chunks 100–400 mm on the floor | Read (print, fringes, edges) | Read | 6–23 px: read as pale specks at the base line |

**Consequences for C's claim "it reads at 12 m":**
- **M and D1 do not read at 12 m on the wall.** That is honest. There, the dust, the leaf jolt, the light pulse ([10] §4.5) and the sound carry the stage.
- **D2 reads at 12 m** through peeled strips, the bulge silhouette, plates starting to stand off and powder streaks. It does not read through crack lines.
- **FINAL and S4 read strongly** through the bays, hanging plates, the dust and the floor fan.
