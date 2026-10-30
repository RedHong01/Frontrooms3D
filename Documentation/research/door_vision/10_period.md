# Door vision lites: period rules, style and structure (10_period)

- **Line:** 游戏视觉, session local_5656cffd (account A), door-vision-panel workflow, stage "Research: period vision lites".
- **Date:** 2026-10-30.
- **Status:** RESEARCH + RECOMMENDATION. Nothing is built. Main is read only; this file is the only write.
- **Red's ask (2026-10-30):** every double-acting (DA) door gets a period vision lite, including the map doors; the player can peek through it the way the EXAMINE close-ups work; the Relay can show through it; the Relay does not see the player through it by default. Red also asked for the lite's style and structure to be researched from the period rules, and for our doors to be revised to match.
- **Scope of this file:** the real 1979–1993 rules, sizes, positions, glass, frames, ageing and look, and ONE lite spec for our leaves. What shows through a lite when the next room is not loaded is a separate stage. The Relay's sight is also a separate item: a contract for 关卡设计 / 系统设计.
- **Units:** 1 in = 25.4 mm. Door-root frames as in `door_break/50_rethink.md`: Y up from the floor; Z along the opening from the hinge (pivot axis), with the D1.6 latch end at Z 0.995.
- **Source tags:** [R] = read directly in this pass. [S] = seen only in a search-engine summary (the page blocked the fetch); treat as weaker. [K] = read in our own code or docs. No files were downloaded. Four PDFs were cached by the fetch tool and read as text.

---

## 0. Answer first

| Item | Recommendation | Why (short) |
|---|---|---|
| Lite | **10 × 30 in visible glass (254 × 762 mm), 300 sq in**, one per leaf, square corners | A DA door needs **≥ 200 sq in per leaf with its bottom ≤ 48 in** (Cal. Title 8 §3235(i), from the 1979 UBC). 10 in wide is the narrowest lite a peek camera can look through without the frame filling the shot (§8.4) |
| Height | **Bottom of the visible glass at 45 in (1.143 m), top at 75 in (1.905 m), centre at 60 in (1.524 m)** | Inside Title 8's 48 in limit. The centre sits in the 60–66 in band the trade uses. The player's eye (1.62 m) and the Relay's eye (1.60 m) are both inside the glass |
| Across the leaf | **D1.6 leaves (0.99 m): lite centre at Z 0.400**, so the visible glass spans Z 0.273–0.527 and the kit spans Z 0.254–0.546. **Title / station leaf (1.1955 m): centred on the leaf, Z 0.598 from its hinge edge** | One rule: keep **≥ 0.449 m of leaf between the kit and the latch or meeting edge**, then get as close to the leaf centre as that allows. This leaves 54 mm of intact face outside the door-break window (Z 0.600) and 312 mm clear of the HW-DA1 push plate |
| Glass | **1/4 in polished wired glass** (6.8 mm actual), polished both faces, **diamond mesh** (ASTM / Fed. Spec. mesh m1), about 1/2 in (12.5 mm) pitch, 0.46 mm wire, a slight green body tint, about 80 % light transmission and 9 % reflection | Title 8 names "wired glass" as the type that needs no guard. A February 1992 municipal spec calls for exactly "1/4-inch … polished both sides, mesh m1 (diamond)". Wired glass cracks and holds, as the door-break brief requires |
| Frame, steel leaves | A pressed-steel lite kit on both faces: **19 mm sight face, 45° bevel, mitred corners, about 2.5 mm proud**, painted with the door. Screws (six oval-head) on face S only | As our existing `Kit_DoorLeaf_SteelLite` kit section. The 1991 spec asks for "mitered metal stops" |
| Frame, wood leaves | **A solid-wood bead in the leaf's own species** (walnut on Veneer_DA and the title pair, oak on Oak_DA): mitred, the same 19 mm sight face, a 45° bevel at the glass, about 6 mm proud (ESTIMATE), pinned (no visible screws). Ward_DA (laminate): the steel kit, finished to match its satin armor plates | Wood stops in matching species are the stock detail on non-rated architectural wood doors. Laminate-faced doors use paint-grade stops or metal kits |
| Both faces | Both faces are identical (a DA leaf has no "secure side"). Only the screw heads tell face S from face P | DA doors are pushed from both faces |

The current `Kit_DoorLeaf_SteelLite_DA` (a 4 × 25 in lite at Y 1.575, Z 0.755) breaks three rules. It is only 100 sq in, half of the DA minimum. It sits inside the door-break window (Z 0.70–0.81 is inside Z 0.60–0.995, and Y 1.26–1.89 overlaps Y 0.50–1.70). Its glass is clear rather than wired. It should be replaced by this spec rather than kept as a variant (§9.4).

---

## 1. The period rules (what a 1990 California building had to do)

### 1.1 California Title 8 §3235(i): double-acting doors [R P1]

California's General Industry Safety Orders, amended 1977, 1979 and 1983, cite "the Uniform Building Code, 1979 Edition". For doors that swing both ways the text is:

- doors "swinging both ways and located between rooms such as kitchen and dining room … shall be provided with view panels";
- "One shall be provided for each door of swinging double doors": so a lite **in every leaf** of a pair;
- "The bottom of the view panel or window shall be no more than 48 inches above the floor";
- "The size of the view panel or window shall be no less than 200 square inches";
- "Guards shall be placed over windows which are not of the shatter-proof or wired glass type": so **wired glass is a named, accepted type**;
- "Windows, if used, shall be kept free of dirt or other obstruction to vision": the rule assumes dirt and taped-over glass happen (§7);
- DA doors may not serve more than 100 occupants as exits, may not be "part of a fire assembly", and may not carry panic hardware.

**Consequence for us:** a DA door is never a rated fire door. The NFPA 80 caps on fire-door glazing (§1.4) do not apply, so a 300 sq in lite is legal. The 4 × 25 in "narrow lite" familiar from stair doors is **not** legal on a DA door.

### 1.2 The Uniform Building Code [S P2, P3]

- Secondary sources quote the later UBC / 2001 CBC §1003.3.1.5: double-acting doors "should be provided with view panels of not less than 200 square inches". One municipal checklist gives the bottom at 43 in, citing §1106.10.7 (the 2001 CBC accessibility chapter) [S P2]. The full UBC text could not be fetched (the forum pages returned 403).
- The same forum reports that the 2006 IBC and 2007 CBC no longer address DA doors [S P3]. **So the 200 sq in rule belongs to exactly our era**, from the 1979 UBC through the 1997 UBC.

### 1.3 Safety glazing: CPSC 16 CFR 1201 (1977) [R P4]

- This federal standard covers glazing in "doors" for "residence or … recreational, school, public, or other buildings".
- It exempts "Wired glass used in doors or other assemblies to retard the passage of fire, where such door or assembly is required by a … fire ordinance".
- **Honest note:** strictly, a non-rated DA door glazed after 1977 should carry Category I/II safety glazing. The February 1992 San Diego glazing spec uses **1/4 in clear tempered** for interior "Door lites" (its Type D) and keeps wired glass (its Type I) for rated work [R P6].
- **Why wired glass is still right for us:**
  - Title 8 names it as an acceptable DA view-panel glass.
  - The same 1992 spec lists its polished wired glass as "complying with ANSI Z97.1", the voluntary safety-glazing standard of the day [R P6].
  - Older buildings kept their pre-1977 wired lites into the 1990s.
  - The model codes "effectively banned" wired glass only in 2006 [R P10].
- So in 1990 wired glass is current, common and period-correct. Clear tempered glass is the "newer building" alternative (§5.4).

### 1.4 Fire-door glazing caps (context only) [S P8, P9]

- NFPA 80 limits fire-protective glazing to **100 sq in** in 90-minute and 3-hour doors. In horizontal exits the limit also has a 10 in maximum dimension.
- It limits each light to **1,296 sq in**, with no dimension over 54 in, in 20- and 45-minute doors.
- This 100 sq in cap is where the stock narrow lites come from: 4 × 25 in, 5 × 20 in and 10 × 10 in are each exactly 100 sq in (§2).
- None of this binds a DA door (§1.1).

### 1.5 Accessibility: ADAAG 1991 versus ADA 2010 [R P11, P12]

- The "bottom of at least one glazed panel … 43 inches maximum" rule is **2010 ADA §404.2.11** (from ICC A117.1). It exempts lites whose bottom is above 66 in.
- Lori Greene (Allegion) notes that this requirement "was not in the previous edition of the ADAAG" [R P11]. **The 1991 ADAAG had no vision-lite rule.**
- In 1990 the binding height is therefore Title 8's **48 in**.
- The ICC A117.1 commentary uses **43–51 in** as a wheelchair user's eye level [R P12]. A lite with its bottom at 45 in still gives a seated viewer glass at eye level.

### 1.6 Two period municipal specs, inside the era lock [R P5, P6]

City of San Diego CWP guideline sections, the same family as the February 1992 hardware section 08710 already used for HW-DA1:

- **08110 Steel Doors and Frames, pages dated [FEBRUARY 1991]:** "Glazed openings in doors shall be of sizes indicated, and shall include mitered metal stops. Glazed openings in fire-rated doors shall meet UL requirements. … Glass shall be 1/4-inch clear, [tempered plate] [wire] glass."
- **08800 Glazing, pages dated [FEBRUARY 1992]:**
  - "Type I - Clear, Polished Wired Glass: Type II (patterned and wired glass, flat), Class 1 (translucent), Quality q3 (glazing); complying with ANSI Z97.1; 1/4-inch thick; of form 1 (wired, polished both sides), mesh m1 (diamond)."
  - Approved wire-glass makers: AFG Industries, Guardian Industries, Hordis Brothers, Pilkington Sales (North American).

---

## 2. Lite sizes and shapes common c. 1990

| Family | Typical visible sizes | Area | Where it lived | On a DA door? |
|---|---|---|---|---|
| Narrow lite ("N", Steelcraft door-type code "Narrow Light" [R P13]) | 4 × 25, 5 × 20, 3 × 33 in; "many plans default to 4″ x 25″ or 5″ x 20″" [R P11] | 100 sq in | stair and corridor fire doors, storerooms, offices | **No**: half the 200 sq in minimum |
| Small square ("V", "Vision Light" [R P13]) | 10 × 10 in (10 in is the max dimension in some fire doors), 12 × 12 in | 100–144 sq in | fire doors in horizontal exits, offices | **No** |
| Vertical vision lite | 8 × 25 in (exactly 200 sq in), **10 × 30 in (300)**, 12 × 24 in (288) | 200–300 sq in | DA doors, hospital and kitchen traffic doors, classroom doors | **Yes**, the DA family |
| Porthole / square traffic window | about 12–14 in square or round | 144–196 sq in | restaurant kitchen impact doors | Borderline: a 14 in square is 196 sq in, just under 200 |
| Half glass ("G") / full glass ("FG") | e.g. 20 × 30 in or larger | 600+ sq in | lobbies, cross-corridor doors | Legal, but it clashes with the break window and push plates, and gives too much see-through for our pacing |

Notes:
- 5 × 35, 6 × 27, 7 × 22 and 24 × 30 in are other stock kit sizes on today's lists [S P14]. These lists are modern and only show the family.
- An industry rule of thumb: on a 7'-0" door with a narrow lite, a **"10″ nominal top rail"** sits above the glass [R P11].
- The 1985 lite-frame patent describes frames shipped as **eight linear sections** with corners joined on site, earlier **one-piece welded** frames, and earlier "wide, flat steel faces" [R P15] (§6.1).

**Why 10 × 30 in over 8 × 25 in:** both are legal and both read as a tall "slot". 8 × 25 is the legal minimum and looks almost like a narrow lite. 10 × 30 adds the peek width (§8.4), and a 30 in height spans both standing and seated eyes (§3). The prior hardware pass reached the same size independently: "a 10″ × 30″ lite per leaf at 43–73″" (`door_hardware/10_research.md` §10 item 1 [K]).

---

## 3. Position and height

| Rule or habit | Value | Source |
|---|---|---|
| Title 8 bottom of the view panel | ≤ 48 in (1.219 m) | [R P1] |
| Trade habit for the lite centre | 60–66 in (1.52–1.68 m) | DoorwaysPlus guide, quoted in `interactables/02_period_hardware.md` §4.3 [K] |
| ADA 2010 (not period) | bottom ≤ 43 in (1.092 m) | [R P11] |
| Wheelchair eye (A117.1 commentary) | 43–51 in (1.09–1.30 m) | [R P12] |
| Our player eye / Relay eye / Relay height | 1.62 / 1.60 / 2.05 m | `FrontRoomsModuleUnits.cs` L92–94 [K] |

**Pick: glass from 45 in to 75 in (Y 1.143–1.905), centre 60 in (Y 1.524).**
- The bottom passes Title 8 with 3 in to spare.
- The centre is the low end of the trade band.
- The player's eye is 0.096 m above the centre, so a peek at standing height looks through the middle of the glass.
- Seated eyes (43–51 in) see glass from 45 in up.
- **Alternative if Red wants the modern ADA rule too:** drop everything 2 in (Y 1.092–1.854). Every clearance below still holds.

**Horizontal position.** Real lites sit centred in the leaf or toward the lock side. Lock-side lites sit behind the lockset, about 5 in in from the edge. We cannot use the lock side: on D1.6 the latch side is the door-break window. So:

- **D1.6 leaves:** the kit's latch-side edge stops at Z 0.546, 54 mm outside the break window's Z 0.600. The lite centre lands at Z 0.400. That is 0.10 m toward the hinge from the face centre (Z 0.505), which reads as nearly centred.
- **Title pair (FrontRoomsDoubleDoorKit, 1.1955 m leaves):** the same ≥ 0.449 m rule allows the true centre (Z 0.598 from the hinge edge). That gives the classic look of two lites, one centred in each leaf, 1.198 m apart.
  - These leaves have no break code today. If 关卡设计 ever breaks them with a cassette anchored at the meeting edge (the D1.6 logic), the lite is already clear.
  - **Literal fallback:** if the orchestrator wants the D1.6 numbers on every leaf ("Z 0.60 from the hinge"), use centre Z 0.400 on the title leaf too. It is legal, but the two lites then sit 0.40 m from the outer jambs.

---

## 4. The lite on each of our leaves (all dimensions)

### 4.1 D1.6 map leaves: Veneer_DA (walnut), Oak_DA, Ward_DA, Steel_DA and its PaintedMetal variant

Door root: Z from the pivot axis, Y from the floor. Leaf envelope: Y 0.015–2.095, face from Z 0.014 to the latch end at Z 0.995 [K].

| Part | Z | Y | Size |
|---|---|---|---|
| Visible glass | 0.273 – 0.527 | 1.143 – 1.905 | 254 × 762 mm (10 × 30 in) |
| Glass blank (visible + 3/8 in a side) | 0.2635 – 0.5365 | 1.1335 – 1.9145 | 273 × 781 × 6.8 mm |
| Kit / bead outer edge (19 mm sight face) | 0.254 – 0.546 | 1.124 – 1.924 | 292 × 800 mm |
| Leaf cut-out (under the kit) | per kit; the existing steel kit uses visible + 9.5 mm a side | | 273 × 781 mm |
| Anchor `lite_centre` (exists on SteelLite) | 0.400 | 1.524 | |

| Clearance | Value |
|---|---|
| Kit to the door-break window (Z 0.600, Y 0.50–1.70) | **54 mm** of intact face. The kit is clear in Z, so overlapping in Y does not matter |
| Kit to the latch end (Z 0.995) | 449 mm |
| Kit to the hinge stile face (Z 0.014) | 240 mm |
| Kit top to the leaf top (Y 2.095) | 171 mm, a 6.7 in top rail (trade habit about 10 in on a 7'-0" door; ours is a 6'-10" leaf) |
| Kit bottom to the lockset (rose / escutcheon at Z 0.920, Y 0.968–1.000) | clear (different Z) |
| Kit bottom to the kick plates (Y ≤ 0.272) / Ward armor (Y ≤ 0.831) | 852 mm / 293 mm |
| **Conflict:** the locked-door sign (`sign_s/_p` at Y 1.524, Z 0.500; plate 254 × 76 mm, so Z 0.373–0.627) | **The lite covers it.** Move the sign to the top rail: **centre Y 2.010, Z 0.400** (plate Y 1.972–2.048, Z 0.273–0.527), 48 mm above the kit and 47 mm below the leaf top. The 10 in plate exactly matches the 10 in lite width, a common "sign above the lite" arrangement. The anchor is in our Blender kit (`interact_door_da_common.leaf_anchors`); the map's facade only reads it |

### 4.2 Title / station pair: FrontRoomsDoubleDoorKit (walnut, 1.1955 × 2.639 m leaves)

Carrier frame: Z from the leaf's hinge edge. Y from the floor; the leaf runs Y 0.018–2.657 [K].

| Part | Z (from hinge edge) | Y |
|---|---|---|
| Visible glass | 0.471 – 0.725 (centre 0.598) | 1.143 – 1.905 |
| Kit / bead outer | 0.452 – 0.744 | 1.124 – 1.924 |
| HW-DA1 push plate (each face) | 1.0558 – 1.1574 | 0.940 – 1.346 |
| Kick plate | full width less 1-1/2 in | 0.021 – 0.275 |

| Clearance | Value |
|---|---|
| Kit to the push plate | 312 mm (in Z; they overlap in Y, which is fine) |
| Kit to the meeting edge (1.1955) | 451.5 mm (the same ≥ 0.449 m rule as D1.6) |
| Kit top to the leaf top | 733 mm. A tall leaf keeps its lite at eye height, not at the leaf's middle |
| Kick plate top to the kit | 849 mm |

The leaf is a C# box mesh (`LeafMesh()`), so the lite needs a cut-out loft plus the bead and glass parts. That is the design and production stages' job. Main is untouched here.

---

## 5. Glazing: 1/4 in polished wired glass

### 5.1 What it is

| Property | Value | Source |
|---|---|---|
| Thickness | 1/4 in nominal; **6.8 mm (17/64 in) actual** | General Glass product pages [R P16]; TGP WireLite "17/64″ (6.8 mm)" [S P17] |
| Faces | **polished both sides** ("form 1") | 1992 spec [R P6] |
| Mesh | **diamond (m1)** in the 1992 US spec; General Glass sells "(1/2″) diamond" and square ("Georgian"); square Georgian wires are "spaced 12.5mm apart" with "a gauge of 0.46mm" | [R P6], [R P16], 1988 patent [R P18] |
| Light transmission / reflection | **80 % / 9 %** for 1/4 polished wire (1/4 diamond *cast* 84 % / 8 %) | GGI data sheet, read as text [R P19] |
| Body colour | clear soda-lime: **"almost colorless with slight green tint"** (a Mississippi Glass "Misco" diamond-wire sample, Corning Museum of Glass) | [S P20] |
| Strength | weaker than plain glass: "wired glass is actually weaker than unwired glass" | [R P10] |
| How it breaks | cracks, and "the wire prevents the glass from falling out of the frame even if it cracks"; "the wire being effective in preventing people or objects falling completely through the glass when the glass is broken" | [R P10], [R P18] |
| Era | invented 1892 (Shuman); US wire-glass makers in 1992: AFG, Guardian, Hordis, Pilkington; Mississippi Glass sold "Misco" diamond netting | [R P10], [R P6], [S P20] |

### 5.2 Diamond or square?

- **Diamond (m1)** is the US look: Mississippi "Misco", and the 1992 California spec's own choice. **Square "Georgian"** is the Pilkington (UK) product, also sold in the US.
- **Recommendation: diamond** for a 1990 American office.
- Our GD3c `Wired6` profile is "PARKED, data only", with `wireGrid = 0.0125` square and `hangsOnWire = true` (`FrontRoomsGlassProfileData.cs` L150–156 [K]).
  - Diamond = the same 12.5 mm grid turned 45°.
  - It needs a pattern or rotation field. The crack-hang logic should not care.
- The exact rhombus proportions of US diamond wire are **UNVERIFIED**. General Glass says only "1/2″". Check against a photo in the media stage.

### 5.3 How it reads (optics of the spec)

- **Two faces, two reflections.** Both faces are polished, 6.8 mm apart, so a bright lamp shows a faint double image. Reflection is about 9 % (P19). In a dim room behind a lit corridor the lite turns into a mirror, the classic "you can't see in until your face is on the glass" moment.
- **The mesh is sub-pixel at gameplay range.** Our camera is 76° vertical FOV at 1080 p, about 691/D px per metre (D in metres).

  | Distance | Mesh pitch | 0.46 mm wire | How it shows |
  |---|---|---|---|
  | 3 m | 2.9 px | 0.11 px | a uniform about 7 % darkening plus moiré risk. Coverage = 1 − (1 − 0.46/12.5)² = 7.2 % |
  | 1 m | 8.6 px | 0.32 px | the grid is visible |
  | 0.3 m | 29 px | 1.1 px | a crisp grid |
  | 0.10 m (a peek) | 86 px | 3.2 px | a coarse foreground grid; the period look is a soft mesh in front of a sharp far room |

  The mesh therefore needs a filtered (mip-biased or analytic) wire term, not an alpha-tested texture.
- **Wire colour.** The wire is bright steel when new. It reads dark against a lit far side and glints when lit from the viewer's side.

### 5.4 The alternative (not recommended)

1/4 in clear tempered glass is what a newer 1990 building's interior door lites carried (San Diego Type D [R P6]). It has no mesh, and on breaking it dices instead of cracking and holding. It loses the period read and the door-break "cracks and holds" beat.

---

## 6. Frames: lite kits and stops

### 6.1 Steel leaves

- **Hollow-metal doors in 1991:** factory-glazed openings "shall include mitered metal stops" [R P5].
  - The common build: a fixed stop on one face and a removable screw-applied stop on the other, or a two-piece pressed-steel "lite kit" that clamps the door from both faces.
  - A 1984/85 frame patent describes the kit: "a first member mounts on one face of the door and a second on the opposite face, with flanges overlapping the opening's edges" [R P15]. It is bolted through the cut-out, made of steel or stainless, and can have angled faces "to give a beveled look".
  - The same patent names the prior designs it improves on: one-piece welded frames, "wide, flat steel faces", and a 1976 patent (US 3,969,857) with screws inside the cut-out.
- **Sizes (modern dealer rule):** cut-out ≈ visible + 1-1/2 in each way; glass ≈ visible + 3/4 in [R P14b].
- **Ours:** `Kit_DoorLeaf_SteelLite` already models this [K]: a 19 mm face, 2.5 mm proud, 45° bevels at the sight and outer edges, six slotted oval-head screws on face S, and a 9.5 mm glass pocket.
  - Keep that section.
  - Change the glass to 6.8 mm (the kit's inner lip moves from X ±0.0031 to ±0.0035).
  - Paint the kit with the door (Door_Enamel). Painted kits match the door; they are not chrome.

### 6.2 Wood leaves (walnut Veneer_DA, the walnut title pair, Oak_DA)

- On non-rated architectural wood doors the lite is held by **wood stops ("beads") in the face species**: mitred, one side fixed at the factory with brads, the other side tacked in after glazing, with glazing compound or tape [S P21].
- Laminate-faced doors get paint-grade (poplar) stops [S P21] or a metal kit. **Ward_DA (laminate): use the steel kit**, finished like its satin armor plates.
- Our bead (ESTIMATE until a profile drawing is read): 19 mm sight face (the same envelope as the steel kit, so cassettes and LODs share one cut-out), a 45° chamfer at the glass, about 6 mm proud of the face, mitred corners, pins hidden. It uses the leaf's walnut or oak with its grain running along each bead, and the leaf's finish.
- Rated wood doors (45 min and up) used metal kits even on wood. Ours are DA, so never rated (§1.1), and a wood bead is right.

### 6.3 DA symmetry

- A DA leaf has no secure or pull side, so the trim is the same on both faces.
- On the steel kit, screws show on one face only (S). Keep that; it is how kits were sold.
- The wood bead shows no fasteners on either face.

---

## 7. How lites age (for the surface pass)

| Effect | Where | Evidence |
|---|---|---|
| **Rust at the wire ends**: a brown bloom along the bottom bead line, where cleaning water sits | glass edge, under or just inside the stop | "Care should also be taken not to subject exposed wire along the cut edge directly to water or to moisture build-up which could precipitate rusting" (GGI) [R P19]; "Wired glass can be damaged due to the spread of rust" [S P22] |
| **Edge cracks** from rust jacking and from cutting (cut wire bends and leaves hairline edge cracks) | short cracks running in from an edge, held by the wire | [S P22] |
| **An old crack kept in service** (wired glass holds, so nobody replaced it) | a corner crack, 1–3 lines | [R P10], [R P18]; it fits our break (§9.3) |
| **Dirt line** in the inside corner of the stop, and a dried glazing-compound bead | perimeter | Title 8 had to order windows "kept free of dirt" [R P1] |
| **Tape ghosts / taped paper** ("KEEP CLOSED", "EMPLOYEES ONLY", "WET FLOOR") | on the glass at eye level | the same Title 8 line ("obstruction to vision") [R P1]; prop text is our call |
| **Smudges**: hand height (pushed doors), forehead and nose height (people peering) | about 45–55 in and 60–66 in | ESTIMATE from use. The forehead smudge is a good free storytelling mark for a peek lite |
| **Scratches** from carts and keys | lower half of the glass | ESTIMATE |
| **Paint on the glass** along a painted steel kit's sight edge | sight line | ESTIMATE (repainting over a kit) |
| **Worn finish** at the bead's corners (wood) | corners | ESTIMATE |

Pick two or three of these per door, not all of them; our "say it once" rule applies.

---

## 8. How they read: photos, film, and the peek

### 8.1 Photo pass: honest status

- This pass did **not** confirm a period (1980–93) photo of a DA door lite. Library of Congress and Corning pages returned 403 on fetch, and the HABS and Commons searches found no caption that names a wired-glass door lite.
- Leads for the media stage (unviewed unless marked):

  | Lead | What it might show |
  |---|---|
  | HABS NJ-5, Atlantic City City Hall, photo 25: "MAIN DOOR TO POLICE DEPARTMENT, DETAIL OF TWO-WAY DOOR HINGE" (P23) | a two-way (DA) door |
  | HABS CO-172 (Fitzsimons General Hospital) corridor photos (P24) | hospital corridor doors |
  | idighardware "WW: the art of wired glass" | broken wired glass, five credited photos; the page was read, the images not viewed (P25) |
  | Wikimedia Commons "Category:Wire mesh glass" | 8 files, mostly modern; read as a list (P26) |

- Search terms that would work better on Flickr or Commons: "wire glass door" with "hospital" or "school", "swinging doors window kitchen 1980s".
- Per the research rule, any Figma frame for this work leads with these photos plus the Title 8 text, not with diagrams.

### 8.2 What the rules and optics predict a 1990 DA door lite looks like

- **From across a room:** a dark vertical slot at head height, darker than the leaf. Behind it, a strip of the next room's light. The mesh is invisible (§5.3).
- **At about 2 m:** a faint grid texture and the room's reflection laid over the far side. The far side shows only if it is brighter than our side.
- **Face to the glass:** the reflection drops away and the far room appears through a soft mesh, framed by the stop or kit at the edges.

### 8.3 The horror beat: the Relay in the lite

- **Standing 1.5 m in front of the door**, with the Relay 1 m behind it: the lite (Y 1.143–1.905, eye 1.62) shows Y 0.83–2.10 at the Relay's plane. That is head and torso.
- **Peeking from 0.10 m:** the whole Relay fits, with room to spare.
- This is the classic "shape in the window" shot, and it works at our heights with no special casing.

### 8.4 Peek camera geometry (for the interaction stage)

- At 76° vertical FOV and 16:9, the horizontal half-angle tangent is 1.389.
- **A 254 mm (10 in) lite fills the screen's width at 0.091 m from the glass**; at a 50° inspect FOV, at 0.153 m. A 102 mm (4 in) lite would need the camera at 0.037 m, inside the 0.06 m near plane. **The 10 in width is what makes an EXAMINE-style peek shot possible.**
- **View wedge into the next room:**

  | Camera position | Wedge (horizontal × vertical) |
  |---|---|
  | 0.5 m from the glass (walking up) | about 29° × 74° |
  | 0.10 m (a peek) | about 104° wide |

  So a peek exposes most of the next room. That is the input for the "room behind is not loaded" stage.

---

## 9. Recommendation and its consequences

### 9.1 The one spec

> **DA-L1 vision lite.**
> - **Glass:** 10 × 30 in visible (254 × 762 mm), square corners, one per leaf, both faces identical. 1/4 in (6.8 mm) polished wired glass, diamond mesh m1 at 12.5 mm, 0.46 mm wire, a slight green tint, about 80 % transmission and 9 % reflection.
> - **Height:** bottom of the visible glass at 45 in (Y 1.143), top at 75 in (Y 1.905).
> - **Across the leaf:** the kit or bead edge stays ≥ 0.449 m from the latch or meeting edge, then the lite goes as close to the leaf centre as that allows. **D1.6: centre Z 0.400. Title pair: centre Z 0.598 (true centre).**
> - **Frame:** steel and laminate leaves get a pressed-steel kit (19 mm face, 45° bevels, 2.5 mm proud, six oval-head screws on face S), finished like the leaf's own metal. Wood leaves get a solid bead in the leaf's species (19 mm face, 45° chamfer, about 6 mm proud, mitred, no visible fasteners).
> - **Locked-door sign:** moves to the top rail above the lite (centre Y 2.010, Z 0.400).

### 9.2 What changes elsewhere

| Item | Change | Owner |
|---|---|---|
| `Kit_DoorLeaf_SteelLite_DA` | superseded: its 4 × 25 lite at Z 0.755 is illegal on a DA door and sits in the break window. Rebuild it as `Steel_DA` + DA-L1. Move the old file to `_partial` when production lands; never delete it | 游戏视觉 |
| `Kit_DoorLeaf_Veneer_DA`, `_Oak_DA`, `_Ward_DA`, `_Steel_DA` (+ PaintedMetal) | each gains a lite: cut-out, kit or bead, glass submesh (glass slot plus a `Glass_Wired` look), anchor `lite_centre` (0, 1.524, 0.400) | 游戏视觉 (Blender kit) |
| `interact_door_da_common.leaf_anchors` | `sign_s/_p` moves from (Y 1.524, Z 0.500) to (Y 2.010, Z 0.400) | 游戏视觉; tell 关卡设计, whose facade mounts signs at the anchor |
| FrontRoomsDoubleDoorKit (title stream, station entrance) | lite centred on each leaf; walnut bead | 游戏视觉 (Rendering/ is ours) |
| Colliders | the 0.05 × 2.08 × 0.98 leaf collider is solid, so **the Relay's sight ray stops at it by default**. That already satisfies "Relay can't see through" (the existing SteelLite docstring flags it) | contract to 关卡设计 / 系统设计; never edit map files |
| GD3c `Wired6` | un-park it for door lites; add a diamond (45°) pattern field; size the budgets for a 0.25 × 0.76 m pane | glass track (游戏视觉) |
| G14 RT glass | every DA leaf in view adds a small RT pane on desktop. The perf check is the tech stage's | 游戏视觉 tech stage |

### 9.3 Door-break interplay (production `wf_2028e947-01b`)

- **The lite stays outside the cassette window.** With 54 mm of intact face between them, the cassette boundary vertices and the lite cut-out never meet. The `_FR_DAMAGE` onset mask (0.40 × 0.60 m at the latch, `50_rethink.md` §3.8) also stays off the kit.
- **Steel family P3:** "SteelLite: the steel cassettes, cut to avoid its lite" (`50_rethink.md` §3.5) is no longer needed. The lite is no longer in the window, so one steel cassette set serves both leaves.
- **Real behaviour:** at BROKEN the top pivot tears and the leaf leans about 110 mm on its bottom pivot (Red's 1a). A racked leaf stresses its glass at the corners. **Wired glass would crack from one or two corners and hold, with no fall-out.**
  - Suggest one pre-baked "racked" crack state for the lite at BROKEN, using GD3c's crack graph with `hangsOnWire`.
  - Earlier stages leave the lite intact, because the blows land at the latch.
- Shattering the lite (the windows' look) would be wrong for wired glass.

### 9.4 Open items (for the design stage; Red decides only where marked)

1. **Mesh pattern:** diamond (recommended, US 1990) or square Georgian (what `Wired6` has now). Red, if he wants a say; otherwise diamond.
2. **Height:** period 45–75 in (recommended) or the modern ADA 43–73 in.
3. **Title pair position:** centred (recommended) or the literal Z 0.400.
4. **Ward_DA** gets the steel kit (recommended) or a paint-grade bead.
5. The wood bead profile is an ESTIMATE. Read a manufacturer stop-profile drawing before modelling (the VT Industries drawing returned 404 this pass).
6. The diamond rhombus proportions are UNVERIFIED (§5.2).
7. Period photos still need finding (§8.1).

---

## 10. Sources

| # | Source | Used for | Tag |
|---|---|---|---|
| P1 | California Code of Regulations, Title 8 §3235 Doors (cites the 1979 UBC; amendments 1977, 1979, 1983), https://www.dir.ca.gov/title8/3235.html | DA view panels ≥ 200 sq in, bottom ≤ 48 in, one per leaf, wired glass named, kept free of dirt, no fire assembly or panic hardware | R |
| P2 | City of Everett, WA, accessibility checklist citing §1003.3.1.5 / §1106.10.7, https://lfportal.everettwa.gov/WebLink/0/doc/435325/Page24.aspx | later UBC/CBC: 200 sq in, bottom 43 in | S (the portal needs cookies) |
| P3 | The Building Code Forum, "Double acting doors", https://www.thebuildingcodeforum.com/forum/threads/double-acting-doors.1530 and "California code for door vision panel", https://www.thebuildingcodeforum.com/forum/threads/california-code-for-door-vision-panel.7012 | 2001 CBC §1003.3.1.5; dropped in the 2006 IBC / 2007 CBC | S (403) |
| P4 | 16 CFR 1201.1 (CPSC safety glazing, issued 6 Jan 1977), https://www.law.cornell.edu/cfr/text/16/1201.1 | scope ("public, or other buildings"); wired-glass fire-door exemption | R |
| P5 | City of San Diego CWP Guidelines, Section 08110 Steel Doors and Frames, pages [FEBRUARY 1991], https://www.sandiego.gov/sites/default/files/legacy/mwwd/business/cwpspecs/pdf/08110.pdf | "mitered metal stops"; "1/4-inch clear, [tempered plate] [wire] glass" | R (cached PDF, read as text) |
| P6 | City of San Diego CWP Guidelines, Section 08800 Glazing, pages [FEBRUARY 1992], https://www.sandiego.gov/sites/default/files/legacy/mwwd/business/cwpspecs/pdf/08800.pdf | Type I polished wired, "form 1 (wired, polished both sides), mesh m1 (diamond)", ANSI Z97.1; makers; interior door lites = Type D tempered | R (cached PDF, read as text) |
| P7 | City of San Diego CWP Section 08710 Finish Hardware [FEB 1992] (via `door_hardware/10_research.md` S1) | HW-DA1 lineage | K |
| P8 | CDF Distributors, "Vision panel fire rating requirements", https://www.cdfdistributors.com/blog/post/vision-panel-fire-rating-requirements-commercial-doors | 100 sq in (max 10 in) at 60/90 min; 1,296 sq in at 20/45 min | S |
| P9 | SAFTI FIRST, "Vision panels and the codes", https://safti.com/articles/vision-panels-and-the-codes/ | wired glass is "fire protective", size-limited; ADA 404.2.11 | R |
| P10 | Wikipedia, "Wired glass", https://en.wikipedia.org/wiki/Wired_glass | invented 1892; the wire holds cracked glass; weaker than unwired; the IBC "effectively banned" it in 2006 | R |
| P11 | iDigHardware (Lori Greene), "Is there a requirement for the location of a glass lite…", https://idighardware.com/?p=13365 | 43 in rule "was not in the previous edition of the ADAAG"; 4 × 25 / 5 × 20 defaults; 3 × 31 with a 10 in top rail on a 7'-0" door | R |
| P12 | iDigHardware, "Decoded: Vision Lights" (2024), https://idighardware.com/2024/06/decoded-vision-lights/ | ADA 2010 §404.2.11 / A117.1 §404.2.10; the 66 in exception; wheelchair eye 43–51 in | R |
| P13 | Steelcraft (Allegion), Tech Data Manual Section 1 (2016), https://allegion.ca/content/dam/allegion-us-2/web-files/steelcraft/technical-documents/Tech_Data_Manual_Section__1__General_Information_110552.pdf | door-type codes F, G, V "Vision Light", N "Narrow Light", FG | R (cached PDF, read as text) |
| P14 | Search summaries of lite-kit stock lists (SAFTI GPX, Best Access Doors, Triton), e.g. https://bestaccessdoors.com/content/specs/BA-VLBB.pdf | 5×35, 6×27, 7×22, 12×12, 24×30 stock sizes | S |
| P14b | DoorwaysPlus, "Steel door lite kits on hollow metal doors…", https://www.doorwaysplus.com/blog/our-blog-1/steel-door-lite-kits-on-hollow-metal-doors-the-cutout-and-glass-size-decisions-that-bite-you-at-installation-196 | cut-out ≈ visible + 1-1/2 in; glass ≈ visible + 3/4 in | R |
| P15 | US 4,550,542 "Vision panel frame" (filed 9 Aug 1984, issued 5 Nov 1985), https://patents.google.com/patent/US4550542 | two opposing steel frame members, one per face; earlier one-piece welded frames and wide flat faces; US 3,969,857 (1976) | R |
| P16 | General Glass International, Georgian Polished Wire https://www.generalglass.com/?p=720 and Diamond Polished Wire https://www.generalglass.com/?p=714 | 1/4 in (6.8 mm); "(1/2″) diamond"; "not designed for impact-rated safety glazing" | R |
| P17 | TGP FireLite / WireLite listing (search summary), https://construction.jidipi.com/d00095624/en/fireglass-wirelite-door-glass | 17/64 in (6.8 mm) wired glass | S |
| P18 | WO 1988/006096 A1 "Fire-resistant safety glass" (published 25 Aug 1988), https://patents.google.com/patent/WO1988006096A1/en | conventional wired glass: wire "gauge of 0.46mm", "wires spaced 12.5mm apart"; the wire holds broken glass | R |
| P19 | GGI Wire Glass Data Sheet (2022), https://www.generalglass.com/wp-content/uploads/2022/08/GGI-Wire-Glass_Data-Sheet.pdf | 1/4 polished wire: VLT 80 %, reflection 9 %; the edge-rust warning | R (cached PDF, read as text) |
| P20 | Corning Museum of Glass, "3/8 Millite Misco" sample (Mississippi Glass), https://glasscollection.cmog.org/objects/29956/38-millite-misco | diamond wire; "almost colorless with slight green tint" | S (403) |
| P21 | VT Industries wood-stop profile drawings (search summary), https://vtindustries.com/webres/File/architectural-doors/Accessories%20%26%20Transoms/Standard-Lite-Woodstop-Profile-Drawings.pdf | wood stops: one side fixed with brads at the factory, the other tacked; poplar stops on laminate doors | S (404 on fetch) |
| P22 | Central Glass / Syracuse Glass, Polished Wired Glass product info (2001), https://syracuseglass.com/E-DOCS/Fire%20Rated%20Glass/EDOCS/Polished%20Wired%20Glass%20Product%20Info.pdf | edge rust spreads and damages the glass; cutting bends the wire and leaves hairline edge cracks | S |
| P23 | HABS NJ-5, Atlantic City City Hall, photo 25, https://www.loc.gov/resource/hhh.nj0005.photos/?sp=25 | lead: a two-way door | unviewed (403) |
| P24 | HABS CO-172, Fitzsimons General Hospital, https://www.loc.gov/resource/hhh.co0549.photos/?sp=4 | lead: hospital corridor | unviewed |
| P25 | iDigHardware, "WW: the art of wired glass" (2013), https://idighardware.com/2013/01/ww-the-art-of-wired-glass/ | broken wired-glass photos (credited, not to be copied) | R (text only) |
| P26 | Wikimedia Commons, Category:Wire mesh glass, https://commons.wikimedia.org/wiki/Category:Wire_mesh_glass | 8 files, modern | R (list) |
| K | Our code and docs: `Tools/Blender/frontrooms_kit/assets/interact_door_da_common.py`, `interact_door_leaf_*_da.py`, `interact_door_leaf_steel_lite.py`, `interact_door_sign.py`, `interact_door_push_plate.py`; `Assets/Scripts/Rendering/FrontRoomsDoubleDoorKit.cs` L31–59; `Assets/Scripts/FrontRoomsMap/FrontRoomsModuleUnits.cs` L92–94; `Assets/Scripts/FrontRooms3DGame.cs` L479 (FOV 76, near 0.06); `Assets/Scripts/Rendering/Glass/FrontRoomsGlassProfileData.cs` L150–156; `research/door_break/50_rethink.md`; `research/door_hardware/10_research.md`; `research/interactables/02_period_hardware.md` | | K |
