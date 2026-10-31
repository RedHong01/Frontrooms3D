# 50 — Door vision lites: period lite forms, and one form per zone

- **Line:** 游戏视觉, session local_5656cffd (account A), door-lite-forms workflow, stage "Research".
- **Date:** 2026-10-30.
- **Status:** RESEARCH + RECOMMENDATION. Nothing built. Main read only; this file is the only write.
- **Red's ask (2026-10-30, verbatim):** "目前在不同门+窗户的样式上，我需要你做research 思考符合那个时代要求的前提下，门上的开窗可以是什么形式的". It came with a screenshot of today's prototype lite (DA-L1, 10 x 30 in, wired). Red's earlier ask (10-03): doors differ by level / zone (lobby, office, run, the real final exit).
- **Builds on:** `10_period.md` (rules, glass, DA-L1), `30_design.md` (LIVE / DARK, PEEK, one-way rule), `40_prototype.md` (the built lite).
- **Frames:** door root as in `door_break/50_rethink.md`. Y is up from the floor. Z runs along the leaf from the hinge. D1.6 map leaves: face Z 0.014–0.995, Y 0.015–2.095. Title / station leaf (`FrontRoomsDoubleDoorKit`): 1.1955 × 2.639 m, Z 0–1.1955 from its hinge edge, Y 0.018–2.657. 1 in = 25.4 mm.
- **Source tags:** [R] read in this pass (PDFs cached by the fetch tool and read as text; nothing downloaded). [S] seen only in a search-engine summary. [K] our own code / docs. [E] estimate. [L] lead, not viewed.

---

## 0. Answer first

**The period gave a DA (double-acting) door four lite families: tall narrow, square / near-square, round, and half / full glass.** A lite in a door that swings both ways had to be ≥ 200 sq in per leaf with its bottom ≤ 48 in (Cal. Title 8 §3235(i), from the 1979 UBC). That rule removes most stock sizes. 4 × 25, 5 × 20, 10 × 10, 12 × 12, 5 × 35 and 6 × 27 are all under 200. The kitchen-door 9 × 14 and the 8 in and 10 in round windows are too. Our door-break window then removes half and full glass from any leaf that can break. What is left is a small, real menu.

| Zone | Form | Visible glass | Where (Z from hinge · Y) | Frame |
|---|---|---|---|---|
| **Level 0 lobby** (title / station pair, walnut, 1.1955 × 2.639 m) | **Tall lobby lite, 10 × 48 in** (480 sq in) | 254 × 1219 mm | **Z 0.471–0.725** (centred, 0.598). Literal-window fallback: Z 0.273–0.527. **Y 1.143–2.362** (45–93 in) | walnut bead |
| **Office** (D1.6 Veneer_DA walnut, Oak_DA) | **DA-L1, 10 × 30** (300), unchanged | 254 × 762 | Z 0.273–0.527 · Y 1.143–1.905 (45–75 in) | walnut / oak bead |
| **Run** (back of house: Ward_DA, Steel_DA) | **Square, 15 × 15** (225) | 381 × 381 | Z 0.1495–0.5305 (centre 0.340) · Y 1.194–1.575 (47–62 in) | pressed-steel kit, 8 screws on face S [E] |
| **Final exit / stair** (C22 steel EXIT door, single-acting, rated) | **Rated narrow lite, 4 × 25** (100), wired | 102 × 635 | Z 0.349–0.451 (centre 0.400) · Y 1.270–1.905 (50–75 in) | 18 ga rated steel kit |
| Alternate A1 (kitchen / pantry / cafeteria DA door, steel leaf) | **Porthole, 16 in clear** (201) | Ø 406 | centre Z 0.327, Y 1.397 · clear Y 1.194–1.600 | round pressed-steel ring, outer Ø 444 |
| Alternate A2 (janitor / mechanical DA door, Steel_DA) | **DA-L1 + louvre below** | lite as office; louvre 254 × 406 | louvre Z 0.273–0.527 · Y 0.330–0.737 (13–29 in) | steel kit + 20 ga sight-proof louvre |

Why this set:
- **Each zone gets a different silhouette, and each one is a real 1990 type.**
  - The lobby lite is the office lite stretched to the tall leaf: same width, same 45 in bottom.
  - The Run's square is the hospital / service door.
  - The exit's slot is the only lite under 200 sq in. It is legal only because the stair door is a rated single-acting door, not a DA door.
- **Each zone also changes what the player sees** (§2.11):
  - The lobby shows the Relay whole.
  - The office shows head and torso.
  - The Run's low square shows a torso whose head is cut off by the top rail.
  - The exit slot shows a line of the stairwell's steady light.

---

## 1. What every form must pass

| Test | Value | Source |
|---|---|---|
| DA view panel area, per leaf | ≥ 200 sq in ("no less than") | Title 8 §3235(i) [R, 10_period P1] |
| Bottom of the view panel | ≤ 48 in (1.219 m) | same |
| Glass | wired named as needing no guard; ours is wired, unbreakable (系统设计's ruling) | same; NOTE_1030 [K] |
| Rated (non-DA) door glazing | ≤ 100 sq in at 90 min / 3 h | NFPA 80 [S, 10_period P8/P9] |
| Cut-out to door edge | ≥ 5 in (non-label), 6 in (label), modern rule | VT elevation form [S] |
| Door-break window (D1.6) | Z 0.600–0.995, Y 0.50–1.70. The generator asserts **frame ≥ 50 mm** from it and **≥ 45 mm from the `_FR_DAMAGE` mask (Z 0.595, Y 0.80–1.40)**, so frame outer Z ≤ 0.550 | `interact_door_lite_common.py` L207–208 [K] |
| Push plate (title leaf) | 4 × 16 in at Y 0.940–1.346, Z 1.0558–1.1574 (1-1/2 in from the meeting edge) | `door_hardware/20_build.md` L14, L25 [K] |
| Locked-door sign (top rail) | plate Y 1.972–2.048, Z 0.273–0.527. The generator asserts ≥ 40 mm above the frame | `30_design.md` §1.2, L221 [K] |
| Kick plate / Ward armour | Y ≤ 0.272 (D1.6), 0.275 (title) / Y ≤ 0.831 (Ward_DA) | 10_period §4.1 [K] |

**The usable box on a breakable leaf.** For glass that goes below Y 1.70, the visible glass must stay in Z 0.150–0.531. The low end is a 5 in hinge stile from the D1.6 face at 0.014, plus the 9.5 mm pocket. The high end is the frame at 0.550. That box is **381 mm (15.0 in) wide**, so a 15 in lite is the widest form a breakable leaf can carry. The same hinge-relative numbers fit the title leaf under the literal window, and they clear its push plate by more than 0.5 m.

---

## 2. The forms

Every row gives the period evidence, the area check, where the form fits, and how it plays. "Relay band" means the part of the Relay seen through the lite with the player 1.5 m in front of the door and the Relay 1 m behind it. The player's eye is at 1.62 m (scale 2.5/1.5 about the eye). The Relay is 2.05 m tall with its eye at 1.60 m. "Peek fill" is the share of the shot's width that is glass at the PEEK pose, 0.100 m from the glass (76° vertical FOV, 16:9, so the shot is 0.278 × 0.156 m).

### 2.1 Tall narrow lites (the DA family)

| Size | Area | Fit (Z · Y) | Relay band / width | Peek fill | Evidence |
|---|---|---|---|---|---|
| 4 × 25 | 100 ✗ | — | — | 37 % | the fire-door default ("many plans default to 4″ x 25″ or 5″ x 20″") [R, 10_period P11]; Steelcraft code N "Narrow Light" [R, P13] |
| **8 × 25** | **200** ✓ (exactly) | Z 0.2984–0.5016 · Y 1.143–1.778 (45–70 in). Frame 79 mm from the window | 0.83–1.88 m (face in, crown cut) / 0.34 m | 73 % | the legal minimum; 10_period §2 [R] |
| **10 × 30** (DA-L1) | **300** ✓ | Z 0.273–0.527 · Y 1.143–1.905. Frame 54 / 49 mm | 0.83–2.10 (head and torso) / 0.42 m | 91 % | 10_period §2; the hardware pass reached the same size [K] |
| 12 × 24 | 288 ✓ | Z 0.2222–0.527 · Y 1.143–1.753 | 0.83–1.84 / 0.51 m | fills | stock vertical family [S P14] |
| **6 × 34** | **204** ✓ | Z 0.3238–0.4762 · Y 1.041–1.905 (41–75 in). Frame 105 mm | 0.66–2.10 / 0.25 m (a sliver) | 55 % | no catalogue page seen [E]. The nearest stock narrow kits, 5 × 35 (175) and 6 × 27 (162) [S P14], are both **under 200**: stock narrow kits were sized for the 100 sq in fire-door cap, so a DA-legal 6 in lite had to be custom |
| **Near-full-height 6 × 60** | **360** ✓ | Z 0.3238–0.4762 · Y 0.381–1.905 (15–75 in) | floor–2.10 / 0.25 m (the whole figure as a sliver) | 55 % | weak for 1990 [E]: tall slot lites read 1960s modernist or post-2000. **Hits the Ward armour** (Y ≤ 0.831) |
| **Lobby 10 × 48** (title leaf only) | **480** ✓ | Z 0.471–0.725 (centred) · Y 1.143–2.362 (45–93 in). Top rail 276 mm | 0.83–2.86 (everything above the knees) / 0.42 m | 91 % | the same family, scaled to an 8'-7" leaf [E]. Architects scaled lites with the leaf; the 10 in "nominal top rail" habit [R P11] is kept (276 mm) |

How they play:
- 8 × 25 is the tensest legal lite: the face fits, the crown does not.
- 6 × 34 and 6 × 60 turn the peek into "peering through a crack": the frame fills 45 % of the shot.
- 10 × 30 is the readable middle.
- At 0.5 m from the glass the horizontal view wedge into the far room is 23° (8 in), 28.5° (10 in) and 17° (6 in).

### 2.2 Square and near-square

| Size | Area | Fit | Relay band / width | Peek fill | Evidence |
|---|---|---|---|---|---|
| 10 × 10, 12 × 12 | 100 / 144 ✗ | — | — | — | "V" vision light, fire-door sizes (10 in max dimension in some ratings) [R P13, S P8] |
| 14 × 16 | 224 ✓ | fits the box (356 mm wide) | — | fills | **Eliason Easy Swing standard window "14" x 16""** [R, L7]. Eliason was founded 1952; the EASY SWING mark was filed 1968 [S, L9] |
| **15 × 15** | **225** ✓ | **Z 0.1495–0.5305 · Y 1.194–1.575 (47–62 in).** Frame Z 0.1305–0.5495: 50.5 mm to the window, 45.5 mm to the mask; hinge stile 126 mm (5 in) | **0.91–1.55: torso only. The Relay's eye line (1.60) is above the glass** / 0.64 m (its whole width) | fills | the widest square the box allows (§1); the Eliason 14 × 16 / 15 × 20 sizes show squares ≥ 200 were catalogue items for DA doors [R L8] |
| **12 × 20** | **240** ✓ | Z 0.2222–0.527 · Y 1.194–1.702 (47–67 in). Frame 54 mm | 0.91–1.76 (chest to just above the eyes) / 0.51 m | fills | 15 × 20 offered by Eliason [R L8]; 24 × 30 in stock lists [S P14] |

A square is short, so with its bottom held at ≤ 48 in its top is below a standing eye. You look slightly **down** into the far room. The Relay shows as a torso, and its head appears only as it comes close. At 0.5 m the view wedge is 42° (15 in): a wide but short window. Across a corridor the lite reads as a **lit square**, which is the Run research's own image: "The goal is a small lit square at the end of a dark corridor" (`room_visuals/03_run_research.md` L29 [K]).

### 2.3 Round porthole

| Size | Area | Fit | Relay band / width | Peek fill | Evidence |
|---|---|---|---|---|---|
| 8 in, 10 in round | 50 / 79 ✗ | — | — | — | **Eliason restaurant series: "Optional, round clear acrylic window set in black rubber molding"; optional "8" round, 10" round, 14" x 16", 15" x 20", 18" x 30"" [R L8]** |
| **16 in clear** | **201.1** ✓ (π·8²) | **centre Z 0.327, Y 1.397 (55 in); clear Z 0.124–0.530, Y 1.194–1.600; cut-out Ø 425; ring outer Ø 444, Z 0.1048–0.5492** (50.8 mm to the window, 45.8 mm to the mask). **Hinge stile only 100 mm (3.9 in)** on D1.6 | 0.91–1.59 (eyes at the rim) / 0.68 m at the equator | fills (the circle covers the whole shot at 0.1 m; the ring shows from about 0.2 m) | round steel kits sold in **12, 18 and 24 in** (Air Louvers VLFR, 18 ga, "1 in visible", for 1-3/4 in doors) [S L11]. A second maker's 18 in frame lists 406 mm clear, i.e. a 16 in porthole [S, page not identified]. Air Louvers dates from 1965 or 1972 (sources disagree) [S L13] |

- **Period read:** the porthole is the kitchen, pantry and food-service door. Title 8 names "kitchen and dining room" as its example of a DA door [R P1]. The traffic-door literature puts these doors in "supermarkets, convenience stores, and restaurants" (US 5,459,972, filed 10 May 1993) [R L10].
- **The legal porthole is unusually big and low.** The common 8 to 10 in round traffic windows fail the 200 sq in rule. A compliant California kitchen door of 1990 needed 16 in, with its bottom at 47–48 in.
- **Fit:** a 16 in round fits a breakable leaf only with a 4 in hinge stile. That is fine on hollow steel and tight on walnut.
- **On wood leaves** a round lite took a metal round kit, not a turned wood bead [E]. So the porthole is a **steel-leaf form**.
- **Cost [E]:** a round cut-out and ring is about +500–900 tris at LOD0 with 24–32 segments. Steel_DA's LOD0 allowance is +650.

### 2.4 Half-lite and full-lite (upper glass on a mid-rail)

| Form | Area | Fit | Verdict |
|---|---|---|---|
| Half-lite (Steelcraft "G" [R P13]) on the title leaf: visible Z 0.171–1.017 (33.3 in), Y 1.143–2.286 (45–90 in); bead to Z 1.036, 20 mm short of the push plate | 1,499 ✓ | **crosses any break window** (literal Z 0.60–0.995, or one anchored at the meeting edge, Z ≥ 0.8005) | only on a pair that can never break; not recommended |
| Full-lite ("FG") on the title leaf: same Z, Y 0.343–2.286 (13.5–90 in); bead 49 mm above the kick plate | 2,548 ✓ | as above | rejected |

- **Period read:** these are lobby and cross-corridor doors. The 1992 San Diego wood-door section has the opening cut at the factory: "Glass Light Openings: Openings shall be factory cut" [R L2].
- **Gameplay:** the Relay is fully visible from anywhere in the room (view wedge 80°), there is nothing left to peek at, and a wired half-glass walnut door reads institutional rather than lobby.
- A 1990 Class-A lobby would use clear tempered glass in aluminium stiles (10_period §5.4). That falls outside our walnut / steel / wired rules.

### 2.5 Twin and stacked lites

| Form | Area | Fit | Verdict |
|---|---|---|---|
| Twin 2 × (6 × 34), side by side: Z 0.1495–0.3019 and 0.3781–0.5305, Y 1.041–1.905; 38 mm of face between the two kits | 204 each ✓ | fits the box | no period DA evidence. "Multiple narrow lights" exist today only as a special option on some steel series [S L15]. The mullion reads as bars, cell-block. Not recommended |
| Stacked 2 × (8 × 25): upper Y 1.270–1.905 (50–75 in), lower Y 0.508–1.143 (20–45 in), Z 0.2984–0.5016 | 200 each | the upper lite alone fails the 48 in bottom; legal only if Title 8's "view panel" is read as both together. **The lower lite hits the Ward armour** | ambiguous law, no period DA evidence. The lower lite shows the Relay's legs, which is a good scare but not a 1990 door. Not recommended |

### 2.6 Lite with a louvre below

| Form | Fit | Evidence | Play |
|---|---|---|---|
| DA-L1 + louvre, visible **10 W × 16 H in** (254 × 406 mm) on the lite's axis: **Z 0.273–0.527, Y 0.330–0.737 (13–29 in)**; frame Y 0.311–0.756, 39 mm above the kick plate | Z clear of the window (frame ≤ 0.546). **Steel_DA only** (it overlaps the Ward armour) | Wood louvres "of matching specie and finish"; metal louvres "20 gauge, galvanized steel, factory primed" or extruded aluminium, clear or colour anodized (San Diego 08150 Wood Doors, [FEBRUARY 1992]) [R L2]; SDI 111-C "Recommended Louver Details for Standard Steel Doors" [R P13] | **Sight-proof inverted-V blades: no view, but light and sound pass.** The far room's light lies in bars on our floor, a low "something is there" cue. Footsteps are louder (an ask for 声音设计; this line makes no sound). DA doors are never rated (Title 8), so a louvre is legal on them. Cost [E] +250–350 tris LOD0 |

---

## 3. Where the lite sits across the leaf

| Convention | Period practice | On our leaves |
|---|---|---|
| Latch / lock side | the norm on latched doors: the lite sits by the lock, ≥ 5 in from the edge [S L14] | **banned on breakable leaves**: the latch side is the break window |
| Centred | the normal look on a DA leaf, which has no lockset [E] | D1.6: centre Z 0.400 is only 0.105 m off the face centre (0.505), so it reads centred. Title leaf: true centre Z 0.598 (10_period §3) |
| Toward the meeting stiles (a pair's lites read together) | seen on pairs [E] | title leaf only; it would cross a meeting-edge break window |
| Hinge side | uncommon [E] | what the literal window forces on the title leaf: centre Z 0.400, lites 1.59 m apart across the pair |

**Rule used in this file.** Forms up to 10 in wide keep the DA-L1 axis (Z 0.400). Wider forms put their latch-side visible edge at Z 0.527–0.531 and grow toward the hinge. On the title leaf, the lobby form is centred at Z 0.598. That is allowed because the title pair has no break code: its kit edge (0.744) is 56 mm short of where a meeting-edge window would start (0.8005).

---

## 4. Frames and beads of the period

| Kit | Period evidence | Our use |
|---|---|---|
| **Mitred solid-wood moldings** in the door's species | "Openings for non-fire rated doors shall be trimmed with solid wood moldings"; "Light frames shall match the specie and finish of the door" (08150, [FEB 1992]) [R L2] | walnut / oak leaves: the built bead (19 mm face, about 6 mm proud) [K] |
| **Pressed / formed steel kit, two-piece, screws on one face** | "20 gauge, galvanized steel, factory primed" light frames (08150) [R L2]; "mitered metal stops" (08110, [FEB 1991]) [R P5]; the two-member clamp kit of US 4,550,542 (1985) [R P15] | Steel_DA (enamel) and Ward_DA (satin, like its armour): the built steel section. Screws: 6 on 10 × 30 (built); 8 on 15 × 15 and 12 × 20 (2 a side) [E]; 8 on the porthole ring at 45° [E] |
| **Rated kit, 18 ga** | "manufacturer's standard frame formed of 18 gauge, cold-rolled steel … approved for use in assemblies of the fire-rating indicated" (08150 2.4 D) [R L2] | the C22 exit door's 4 × 25 |
| **Extruded aluminium frame**, clear or colour-anodized | 08150 2.4 C [R L2] | not needed: dark bronze is the station's language [K] |
| **Black rubber gasket, push-in** (impact / traffic doors) | "clear acrylic set in black rubber molding" (Eliason) [R L8]; the traffic-door window is "secured in place by mechanically fastened means, which may include a surrounding frame" (US 5,459,972) [R L10] | only if 关卡设计 ever adds a stainless or ABS kitchen traffic door. Our walnut / steel leaves use the two kits above |

---

## 5. What a 1990 building put where

| Zone | Real doors, c. 1990 | Lite |
|---|---|---|
| Lobby / reception | tall wood pairs or aluminium storefront; much glass | half / full glass, or a tall lite in a tall leaf |
| Office corridor / suite | wood DA pairs between suite rooms; the corridor doors themselves were often 20-min single-swing doors | tall narrow vision lite (8 × 25 to 10 × 30) |
| Back of house: service, ward, kitchen | steel or laminate DA doors with armour plates; kitchen traffic doors | squares (14 × 16, 15 × 15, 12 × 20), portholes on food service, louvres on janitor / mechanical rooms |
| Stair / exit | **rated single-acting doors, never DA** (Title 8 forbids DA in fire assemblies and panic hardware on DA) [R P1] | **≤ 100 sq in wired**: 4 × 25, 5 × 20, 10 × 10 |

---

## 6. The set

1. **Level 0 lobby (title stream pair + station entrance): tall lobby lite 10 × 48.** Same width and bottom as the office lite, so the family reads as one building. A tall lite in a tall leaf, wired, walnut bead.
   - **Play:** the Relay is visible from the knees up, which teaches the one-way rule early.
   - **Peek:** the title stream doors have none. Their LIVE / DARK logic is unchanged; the DARK pane is just taller.
   - **Station entrance:** its dead skin takes the same outline.
   - **Sign:** if the leaf ever carries the locked-door sign, it moves to the top rail at Y 2.519 (plate 2.481–2.557).
2. **Office (Veneer_DA, Oak_DA): DA-L1 10 × 30, unchanged.** Already built and tested (`40_prototype.md`).
3. **Run (Ward_DA, Steel_DA, the RN-F ward door): square 15 × 15, low.** The hospital / service door, and the "lit square" at the end of a dark corridor.
   - Through the square you see the Relay's torso without its head. That is more dread and less information, which fits a zone where you run rather than read.
   - The peek still works: the glass fills the shot, with the camera at 1.38 m inside the 1.35–1.75 clamp.
4. **Final exit / stair (C22 EXIT door): rated 4 × 25 wired slot.**
   - It is not a DA door, so Title 8's 200 / 48 rule does not apply, and NFPA 80 caps it at 100 sq in. The only sub-200 lite in the game, so its slot shape alone says "rated door, stair, out".
   - Through it the stairwell's **steady** light shows as a bright 4 in line. Red 10-22: the stairwell is the safe zone and must read protected.
   - No peek: 37 % fill, all frame.
   - **This reverses `30_design.md` §1.1 ("C22: none").** Check the door's own geometry and 信息设计's stair plates (Figma 3545:7368) before modelling. If a DA leaf stands in the stair approach, it takes the Run square.
5. **A1, porthole 16 in clear:** for kitchen, pantry or cafeteria DA doors on steel leaves, as a landmark (at most one per floor). Needs a round sight test (§8).
6. **A2, DA-L1 + louvre:** for janitor and mechanical DA doors on Steel_DA. Adds a light and sound leak with no view.

**Not chosen:** 8 × 25 (legal, but too close to the office lite to read as different); 6 × 34 and 6 × 60 (crack-peek; 6 × 60 hits the Ward armour); 12 × 20 (the square reads better); half and full glass (break window, gives everything away); twin and stacked (no period DA evidence).

---

## 7. Render list (the 1.1955 m title leaf; Z from its hinge edge; literal window unless marked)

| # | Form | Visible glass Z | Visible glass Y | Frame outer (19 mm) | Area |
|---|---|---|---|---|---|
| F1 | DA-L1 10 × 30 | 0.2730–0.5270 | 1.1430–1.9050 | Z 0.2540–0.5460, Y 1.1240–1.9240 | 300 |
| F2 | 8 × 25 | 0.2984–0.5016 | 1.1430–1.7780 | Z 0.2794–0.5206, Y 1.1240–1.7970 | 200 |
| F3 | 6 × 34 | 0.3238–0.4762 | 1.0414–1.9050 | Z 0.3048–0.4952, Y 1.0224–1.9240 | 204 |
| F4 | 6 × 60 | 0.3238–0.4762 | 0.3810–1.9050 | Y 0.3620–1.9240 | 360 |
| F4b | **Lobby 10 × 48, centred** | **0.4710–0.7250** | **1.1430–2.3622** | Z 0.4520–0.7440, Y 1.1240–2.3812 | 480 |
| F5 | **Square 15 × 15** | **0.1495–0.5305** | **1.1938–1.5748** | Z 0.1305–0.5495, Y 1.1748–1.5938 | 225 |
| F6 | 12 × 20 | 0.2222–0.5270 | 1.1938–1.7018 | Z 0.2032–0.5460, Y 1.1748–1.7208 | 240 |
| F7 | **Porthole Ø 16 (0.4064)** | centre Z 0.3270 | centre Y 1.3970 | ring Ø 0.4444; cut-out Ø 0.4254 | 201 |
| F8 | Half-lite (title only) | 0.1710–1.0170 | 1.1430–2.2860 | Z 0.1520–1.0360 | 1,499 |
| F9 | Full-lite (title only) | 0.1710–1.0170 | 0.3429–2.2860 | Y 0.3239–2.3050 | 2,548 |
| F10 | Twin 6 × 34 | 0.1495–0.3019 + 0.3781–0.5305 | 1.0414–1.9050 | kits 0.1305–0.3209 / 0.3591–0.5495 | 204 + 204 |
| F11 | Stacked 8 × 25 | 0.2984–0.5016 | 0.5080–1.1430 + 1.2700–1.9050 | | 200 + 200 |
| F12 | **DA-L1 + louvre** | lite as F1; louvre 0.2730–0.5270 | louvre 0.3302–0.7366 | louvre frame Y 0.3112–0.7556 | 300 |
| F13 | **Rated 4 × 25 (C22 door)** | 0.3492–0.4508 | 1.2700–1.9050 | Z 0.3302–0.4698, Y 1.2510–1.9240 | 100 |

- **Checks.** The push plate (Z ≥ 1.0558) is ≥ 0.5 m clear of every literal form; F8 and F9 come within 20 mm of it. The sign (Y ≥ 1.972) is ≥ 48 mm above every frame except F4b, F8 and F9, which are title-leaf only. The kick plate (≤ 0.275) is ≥ 36 mm below F12's frame and F9's bead.
- **Moving to D1.6.** F1–F7 and F10–F13 move to D1.6 with the same Z / Y numbers. Exceptions: F4 and F11 hit the Ward armour, and F7's hinge stile shrinks to 100 mm.
- **Glass and frames for all.** 6.8 mm wired glass, diamond mesh, as built. Walnut bead on walnut, pressed steel on steel.

---

## 8. Consequences (asks; nothing edited)

| Item | Change | Owner |
|---|---|---|
| `interact_door_lite_common.py` | turn the module constants (`LITE_W/H/Z/Y`) into per-leaf parameters; add a round builder (ring + round cut-out, 24–32 segments) and a louvre builder; keep the asserts (≥ 50 / 45 / 40 mm) | 游戏视觉 |
| Sidecar `liteRect` | add `liteShape` (rect / round + radius) | 游戏视觉 |
| Sight rule `PassesPlayerSight` (CONTRACT M-L2) and the peek aim box | a circle test for the porthole; per-form rects | 关卡设计 (map), 交互设计 (peek) |
| Which zone gets which kit (Ward_DA / Steel_DA rows for the Run) | data rows | 系统设计 / 关卡设计 |
| `FrontRoomsDoubleDoorKit` | lobby lite 10 × 48 centred; station skin outline to match | 游戏视觉 |
| C22 exit door lite | 4 × 25 rated slot; check stair plates | whoever owns the C22 door model + 信息设计 |

Open for Red:
1. The set as in §6.
2. Lobby lite centred (Z 0.598, recommended) or at the literal Z 0.400.
3. The exit slot (reverses "no lite on C22").
4. Whether the porthole and louvre alternates are worth their new builders.

---

## 9. Sources

| # | Source | Used for | Tag |
|---|---|---|---|
| P1 | Cal. Code of Regs. Title 8 §3235, https://www.dir.ca.gov/title8/3235.html | DA view panel ≥ 200 sq in, bottom ≤ 48 in, one per leaf, wired glass, "kitchen and dining room", no fire assembly or panic hardware | R (10_period) |
| P5 | San Diego CWP 08110 Steel Doors and Frames [FEBRUARY 1991], https://www.sandiego.gov/sites/default/files/legacy/mwwd/business/cwpspecs/pdf/08110.pdf | "mitered metal stops" | R (10_period) |
| L2 | San Diego CWP 08150 Wood Doors, pages [FEBRUARY 1992], https://www.sandiego.gov/sites/default/files/legacy/mwwd/business/cwpspecs/pdf/08150.pdf | wood moldings matching species; 20 ga galvanized steel or anodized aluminium light frames; 18 ga rated frames; wood / metal louvres; "Glass Light Openings … factory cut"; makers Algoma, Cal-Wood (Timberland), Weyerhaeuser | R (cached by the fetch tool, read as text) |
| P8, P9 | CDF Distributors https://www.cdfdistributors.com/blog/post/vision-panel-fire-rating-requirements-commercial-doors ; SAFTI FIRST https://safti.com/articles/vision-panels-and-the-codes/ | 100 sq in cap on rated doors | S / R (10_period) |
| P11 | iDigHardware, https://idighardware.com/?p=13365 | 4 × 25 / 5 × 20 defaults; 10 in top rail; no lite rule in the 1991 ADAAG | R (10_period) |
| P12 | iDigHardware, "Decoded: Vision Lights", https://idighardware.com/2024/06/decoded-vision-lights/ | "only one of those lights is required to be at the 43-inch height" (ADA 2010, not period); older doors "pre-date the requirement" | R |
| P13 | Steelcraft Tech Data Manual §1, https://allegion.ca/content/dam/allegion-us-2/web-files/steelcraft/technical-documents/Tech_Data_Manual_Section__1__General_Information_110552.pdf | codes F, G, V "Vision Light", N "Narrow Light", FG, FG2; SDI 106-99 nomenclature; SDI 111-C louver details | R (cached, read as text) |
| P14 | stock lite-kit lists, e.g. https://bestaccessdoors.com/content/specs/BA-VLBB.pdf | 5 × 35, 6 × 27, 7 × 22, 12 × 12, 24 × 30 (modern lists) | S |
| P15 | US 4,550,542 "Vision panel frame" (1985), https://patents.google.com/patent/US4550542 | two-member clamp kit | R (10_period) |
| L7 | Eliason Easy Swing (Sweets), https://sweets.construction.com/swts_content_files/55984/P8419.htm | standard window 14 × 16; "Window, clear vinyl" | R (undated listing) |
| L8 | Eliason SCP series (Sweets), https://sweets.construction.com/swts_content_files/55984/P8416.htm | 9 × 14 "set in black rubber molding"; optional 8″ / 10″ round, 14 × 16, 15 × 20, 18 × 30 | R (undated listing) |
| L9 | Eliason trademarks, https://trademark.justia.com/owners/eliason-corporation-2935092 ; maker page https://productguide.myeshowroom.com/manufacturer/eliasontrafficdoors | founded 1952; EASY SWING filed 15 Jul 1968 | S |
| L10 | US 5,459,972 "Impact door construction" (filed 10 May 1993), https://patents.google.com/patent/US5459972 | traffic doors' window "an opening that is filled with a pane of transparent material", held by "mechanically fastened means"; used in "supermarkets, convenience stores, and restaurants"; prior art US 3,979,872, 3,854,263, 4,397,117 | R |
| L11 | Air Louvers VLFR-B round kit, https://www.qualitydoor.com/products/air-louvers-vlfr-b-standard-round-shape-kit-1-visible-for-1-3-4-thick-doors-18ga-accepts-3-16-1-4-glass-mineral-bronze | 12 / 18 / 24 in round, 1 in visible, 18 ga, 3/16–1/4 in glass, 1-3/4 in doors | S (429 on fetch) |
| L13 | Activar Air Louvers, https://www.craftmasterhardware.com/parent-brand/activar-air-louvers/ ; https://4specs.com/s/08/08-1050.html | founding 1965 vs 1972 (conflict) | S |
| L14 | VT Industries flush door elevation form, https://www.vtindustries.com/webres/File/architectural-doors/Order%20forms/VT%20Flush-Door-Elevation-Form.pdf | cut-out ≥ 5 in from edge (non-label), 6 in (label) | S (text layer unreadable) |
| L15 | Allegion KB, https://kc.allegion.com/kb/article/are-multiple-narrow-lights-available | multiple narrow lites = special option | S (403) |
| L16 | Film leads, frames **not viewed** this pass: *Jacob's Ladder* (1990) https://film-grab.com/2021/05/06/jacobs-ladder/ (hospital corridors); *Goodfellas* (1990) https://film-grab.com/2010/08/06/goodfellas-2/ (Copacabana kitchen doors); *Terminator 2*, *The Silence of the Lambs* (1991) https://film-grab.com/category/1991/ (hospital doors); HABS leads in 10_period P23/P24 | for the media stage, before any Figma frame (research rule: lead with real photos) | L |
| K | `10_period.md`, `30_design.md`, `40_prototype.md`, `NOTE_1030_touch_rules_READ_FIRST.md`; `door_break/50_rethink.md`, `52_production.md`; `door_hardware/10_research.md`, `20_build.md`; `room_visuals/03_run_research.md`; `interactables/02_period_hardware.md`; clone `W/proj_doorvision/Tools/Blender/frontrooms_kit/assets/interact_door_lite_common.py` (asserts L201–222) | | K |
