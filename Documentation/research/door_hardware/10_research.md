# Door hardware research: c. 1990 US office double doors (10)

2026-10-30 · 游戏视觉 line (account A, session local_5656cffd), workflow stage "Research: period double-door hardware".
Red's ask (2026-10-30, with a close-up of the title double door's two plain block pulls): "然后我需要你research那个年代的双开门把手/门栓结构，对其进行revise".
Scope: research only. Nothing built, nothing in main changed except this file. Leaf textures belong to the `door_wood_walnut` job; door motion, colliders and pivots stay as they are.

**Verdict.** A double-acting (DA) pair has no latch and no bolt (门栓), and a 1990 hardware schedule gives it no pull. It gets a **push plate on every face** and a **kick plate on every face**, and its hinges or floor closer hold it shut. Our two-post block pulls belong to a single-acting push/pull door, and their shape is not a period pull. Recommended set **HW-DA1** (§9): a 4" × 16" × 0.050" satin-stainless (US32D / BHMA 630) push plate with four bevelled edges and six exposed oval-head screws, centred 45" above the floor and 1-1/2" from the meeting edge, plus a 10" × (leaf − 1-1/2") × 0.050" satin-stainless kick plate on each face. Keep the DA spring hinges. Add no latch, bolt, astragal or closer.

---

## 0. Our door as built (FrontRoomsDoubleDoorKit.cs, landed 10-29)

| Item | Value in the kit | Imperial |
|---|---|---|
| Opening (pair) | `Width` 2.400 m, head soffit `HeadBottom` 2.660 m | 94.5" × 104.7" |
| Leaf | `LeafWidth` 1.1955 × `LeafHeight` 2.639 × `LeafThickness` 0.0445 m; bottom at `LeafBottom` 0.018 (12 mm oak saddle + 6 mm) | 47.07" × 103.9" × 1-3/4" |
| Gaps | 3 mm at jambs, head and meeting edge; black brush strips | 1/8" |
| Hinges | 3 DA spring hinges per leaf: barrel r 11 mm, barrel 203 mm, flange 178 mm; centres from `HingeCentres()`: two stacked low, one 0.15 m under the top | 8" barrels |
| Pulls (to replace) | a 0.055 × 0.18 × 0.045 m box per face at y 1.22, 0.10 m in from the meeting edge, on two Ø14 mm posts at ±0.06 m (`LeafHardwareMesh`) | 2-1/8" × 7" block, 48" high |
| Kick plates | 0.25 m high, `LeafWidth − 0.05` wide, **0.004 m thick**, 3 mm over the leaf bottom, both faces | 10" × (leaf − 2") × 5/32" |
| Hardware material | `Door / brushed steel` (0.64, 0.63, 0.60), smoothness 0.62, metallic 0.9 (RoomStream and StationDress) | reads as satin stainless |
| Faces | RoomStream: `Faces.Both`; StationDress static entrance: `Faces.Plus` (+Z, the station side, only) | |
| Colliders | when the caller asks: the slab, **the pull boxes and the kick plates** (`Box(..., colliders)`) | |

Two notes for later sections. The leaf is oversize for a DA door, about 47" × 104" × 1-3/4" in solid walnut veneer, roughly 150–170 lb (ESTIMATE). Stock commercial heights are 7'0"–8'0". The kick plate is three times too thick, since real plates are 0.050" = 1.27 mm.

---

## 1. Sources

R = read directly in this pass. S = only seen through a search-engine summary, not opened; treat as weaker. No files were downloaded on purpose. The fetch tool cached PDFs in the session's tool-results folder, and they were only read.

| # | Source | What it gives | R/S |
|---|---|---|---|
| S1 | City of San Diego, CWP Guidelines **Section 08710 Finish Hardware** (pages dated **February 1992**, December 1994, November 1997), https://www.sandiego.gov/sites/default/files/legacy/mwwd/business/cwpspecs/pdf/08710.pdf | A **period** spec section, inside the era lock (≤ 1993): plate sizes, kick plate rules, 630/613/26D finishes, mounting heights, knob/lever rules, exit device style, approved makers (Rixson 27 floor closers, Von Duprin 3300/9900 rim, Rockwood 70 push plates) | R |
| S2 | War Production Board, **Builders' Hardware Manual**, 1942, https://www.govinfo.gov/content/pkg/GOVPUB-P32_4800-569524a141e8464f757af83e3f80168e/html/GOVPUB-P32_4800-569524a141e8464f757af83e3f80168e.htm | Lineage: "Single double-acting doors, each to have: 1 set floor hinges … two push plates" (≈ 14 × 3-1/2") | R |
| S3 | US Access Board, **ADAAG 1991** (as amended to 2002), https://www.access-board.gov/adaag-1991-2002.html §4.13.8–4.13.11 | Hardware easy to grasp, no twisting the wrist, mounted ≤ 48" AFF; thresholds ≤ 1/2"; interior opening force 5 lbf | R |
| S4 | California Code of Regulations, **Title 8 §3235 Doors** (cites the **1979 UBC**), https://www.dir.ca.gov/title8/3235.html | DA doors: **view panels ≥ 200 sq in, bottom ≤ 48"**; no DA exits over 100 occupants; DA doors never in a fire assembly and **never with panic hardware**; hand hardware centred 30"–44" | R |
| S5 | **Rockwood** catalog p. B2 "Push Plates – .050" Thick" (rev. 10/06), https://absupply.net/images/Rockwood_catalog.pdf | No. 70 / 70RC / 70RE sizes 3×12, 3-1/2×15, **4×16**, 6×16, 8×16, 4×20; four bevelled edges; #6 × 5/8 OH screws; ANSI A156.6 J301; image shows **6 screw holes** (corners + mid-height) | R |
| S6 | **Hager** I-TA01118 "Typical ADA locations for door hardware" (2014), https://hagerco.com/Files/Files/CHD%20support%20docs/ADA-Hardware-Locations.pdf | **Centre of push plate 45"**; centre of pull and push bar 42"; strike for locks and **exit devices 40-5/16"** (exceptions 36–42); deadlock 48"; kick plate "2" less door width … push side"; 3 hinges to 7'6", **+1 per 2'6"** | R |
| S7 | Hager I-TA01244 30S push plate installation, https://cdn11.bigcommerce.com/s-iqhkw55zf5/content/product-attachments/Hager_30S_Push%20Plate_Installation_Instructions.pdf | **Plate edge 1.5" (38 mm) from the door edge**; #6 × 5/8" screws; 34–48" band | R |
| S8 | Hager letter "Variations in US10B Finish" (2016), https://www.hagerco.com/Files/Files/Warranties/variations_in_us10b_finish.pdf | 613 is an oxidised **Category B "unstable"** finish; it keeps changing and never matches between makers | R |
| S9 | iDigHardware (Lori Greene, Allegion): DHI location of pulls, https://idighardware.com/?p=24775; "Decoded: Mounting Heights", https://idighardware.com/2012/06/decoded-mounting-heights-for-door-hardware/ | DHI "Recommended Locations": **pull centre 42"**; code bands; points to ANSI/SDI A250.8 Table 5 | R |
| S10 | DoorwaysPlus, "Specifying double-acting door hardware sets", https://www.doorwaysplus.com/blog/our-blog-1/specifying-double-acting-door-hardware-sets-what-changes-when-the-door-swings-both-ways-453 | DA = centre-hung pivots / floor closer; **no surface closer**; plates and edging **on both faces**; spring latch won't work; standard exit devices incompatible | R |
| S11 | Ives 101, "Door pulls, push bars, push and pull plates", https://www.iveshardware.com/en/resources/training-education/ives-101/operate-the-door/door-pulls-push-bars-push-and-pull-plates.html | Plates where no latch is needed; back-to-back **plate-to-pull clearance 1-1/2"**; accessible "EZ" pulls ≥ 2-1/2" | R |
| S12 | Archtoolbox, "Interior door protection", https://archtoolbox.com/interior-door-protection | Kick 10–16"; mop 4–8"; armor 36/42"; **single: −2"; pairs: −1" per leaf**; 0.050–0.125" | R |
| S13 | CDF Distributors, "Kick plates and protection", https://www.cdfdistributors.com/blog/post/kick-plates-and-protection | Kick 8–10" standard, 12" carts; push side or both sides | R |
| S14 | Rockwood pulls (dealer listings): 107 https://qualitydoor.com/products/rockwood-107-straight-door-pull-8-center-to-center-pull-8-3-4-overall-3-4-diameter-2-5-8-projection-1-7-8-clearance; 111×70C https://doorwaysplus.com/shop/pp111x70c-32d-rockwood-111-x-70c-pull-plate-10-ctc-4-x16-us32d-277; BF157 https://www.craftmasterhardware.com/parent-brand/rockwood/robf157-us10-rockwood-bf157-offset-door-pull | 107: Ø3/4", 8" c/c, 8-3/4" OA, 2-5/8" projection, 1-7/8" clearance. 111: Ø1", 10" c/c, 11" OA, 2" clearance. BF157 offset: Ø1", 10" c/c, 3-1/2" projection, 2-1/2" clearance, 4" offset | S |
| S15 | Ives FB458 manual flush bolt (dealer), https://www.craftmasterhardware.com/parent-brand/ives-glynn-johnson/ivfb458-ives-fb458-manual-flush-bolts | Face 1" × 6-3/4" × 1-1/8" deep, 3/4" throw, Ø1/2" tip, 12" rod (24" option) | S |
| S16 | Von Duprin 9927 surface vertical rod (dealer listings), e.g. https://fergusonhome.com/von-duprin-9927eo/s411276 | Case 2-1/4" sq; centre case 8 × 2-3/4 × 2-3/8"; projection 3-13/16" (depressed 3-1/16"); centreline 39-5/8"; rods 34-15/16" / 31-1/2" | S |
| S17 | Rixson DA centre-hung floor closers (dealer): 30 https://qualitydoor.com/products/rixson-30-double-acting-center-hung-floor-closer-with-centering-arm-weight-up-to-200-lbs-non-handed; 40 https://qualitydoor.com/products/rixson-40-lap-less-all-parts-double-acting-center-hung-floor-closer-body-only-non-hold-open-weight-up-to-300-lbs | 30: ≤ 200 lb, to 4'0" × 8'6"; 40: ≤ 300 lb; 27 is offset-hung single-acting | S |
| S18 | BHMA finish codes: Northwestern 08 7100, https://northwestern.edu/facilities/docs/08-7100-typical-door-hardware-chicago.pdf; National Lock Supply guide, https://nationallocksupply.com/blog/door-hardware-finishes-guide-bhma-codes-605630-and-us-equivalents/ | US3 = 605, US4 = 606, US10B = 613, US26D = 626 (652 on steel), US32D = 630 | S |
| S19 | Schlage finishes guide, https://www.schlage.com/blog/categories/2022/11/door-hardware-finishes-guide.html | Bright brass "associated with the 1980s and 1990s" | S |
| S20 | DHI Glossary (iDigHardware copy), https://idighardware.com/wp-content/uploads/2020/05/DHI-Glossary-of-Architectural-Hardware-Terms.pdf; LCN catalog, https://idighardware.com/wp-content/uploads/2011/09/LCN_General_Catalog.pdf | DA spring hinge and centre pivot definitions; a DA frame has **no stop**, about 95° each way | S |
| S21 | Bommer 3029 DA spring hinge (dealer), https://swingingcafedoors.com/8-bommer-3029-double-action-spring-hinge-saloon-door-hinge | 8" size: 1-7/16"–2" doors; barrels about 1" longer than the 8" flange; a retailer note to set the 3rd hinge **close to the top hinge** | S (403 on fetch) |
| S22 | 1stdibs, Corbin push plate + pull set from the Philadelphia Civic Center (1931), https://www.1stdibs.com/furniture/building-garden/architectural-elements/phila-civic-centre-corbin-art-deco-push-plate-door-pull-set/id-f_11895441 | A real salvaged set, bronze, 14" × 3": push face plate + pull face pull on one swinging door (lineage, not era) | R |
| S23 | BHMA terminology via Builders Hardware, https://buildershardware.com/Resources/Guide-to-Builders-Hardware-Terminology/Architectural-Door-Trim | Kick plate heights 8, 10, 12, 16" typical; A156.6 J300 push plates / J400 pulls | S |

---

## 2. Period baseline (all door types)

### 2.1 Finishes, c. 1990 (BHMA A156.18 numbers; S1, S8, S18, S19)

| US | BHMA | What it is | Where a 1990 office used it | How it wears |
|---|---|---|---|---|
| US32D | 630 | Satin (#4 brushed) **solid** stainless | The 1992 spec's default for locksets, exit devices and hinges ("[630 (brush finished) stainless steel]"); push plates; kick plates ("brushed brass or brushed stainless") (S1) | It can't wear through. Palm zones **burnish** (the grain softens to a brighter, glossier oval), with grey finger haze, fine random scratches and grime in the screw recesses and at the plate perimeter |
| US26D | 626 (652 on a steel base) | Satin chrome **plated** on brass | The commercial default for locks, levers, knobs, closer arms and door holders ("chrome plated bronze with satin finish, US 26D", S1) | Plating wears through at grip points to **yellow brass, through a grey-white nickel ring**; pits and peels on bathroom doors |
| US10B | 613 | Dark oxidised, oil-rubbed bronze | Hinges as an option in S1 ("[613 (oxidized satin bronze, oil rubbed)]"); pairs with dark-bronze anodised (313) aluminium storefront | "Category B, unstable" (S8): never uniform, drifts in colour, **rubs to bright copper-bronze** on touch points |
| US3 / US4 | 605 / 606 | Bright / satin brass, clear-coated | Lobbies and executive floors; S19 ties bright brass to the 80s/90s | The lacquer fails: **brown-black tarnish blotches** with bright rubbed brass where hands go |
| US28 | 628 | Clear anodised aluminium | Cheap plates, thresholds | Dulls and scratches dull-white |

### 2.2 Mounting heights (centrelines above the finished floor)

| Item | S1 (Feb 1992, period) | S6 Hager / DHI-SDI practice | Code |
|---|---|---|---|
| Push plate | **48"** from the door bottom | **45"** | ADAAG 1991: hardware ≤ 48" (S3); Cal. Title 8: hand hardware 30–44" (S4) |
| Pull / pull plate | 42" | 42" (DHI per S9) | — |
| Push bar | 45" | 42" | — |
| Lock / latch (knob or lever) | 38" | 40-5/16" (36–42") | — |
| Exit device (pad / crossbar) | 38-13/16" | 40-5/16" | — |
| Deadlock | 44" strike | 48" | — |
| Top hinge | 5" from the door top to the hinge top | ≤ 11-3/4" from the frame rabbet | — |
| Bottom hinge | 10" from the door bottom | ≤ 13" | — |
| Hinge count | 2 to 5'0" of height, +1 per 2'6" | 3 to 7'6", +1 per 2'6" | — |

Both rules give a 104" leaf **four** hinges.

### 2.3 Knob or lever in 1990

The ADA was signed in July 1990. ADAAG followed in 1991 and asked for hardware "easy to grasp … does not require tight grasping, tight pinching, or twisting of the wrist", mounted ≤ 48" (S3). The February 1992 San Diego spec still writes knob trim as the default, "cast spherical knobs of 2-inch diameter minimum, 2-1/4-inch wrought roses", with levers as a bracketed option "conforming to handicapped person requirements" (S1). A building finished in 1990 therefore most likely has **knobs on office doors and levers only where someone specified accessibility**. On push/pull and DA doors the question does not arise.

---

## 3. (a) Single-acting pairs: active and inactive leaf

This is the default for a corridor pair that must lock: suite entries, conference rooms, storage.

- **Active leaf.** Mortise or bored lockset at 38" (S1), knob Ø2" min on a 2-1/4" rose, or a lever (~4-1/2–5" long) on a rose or escutcheon. Strike in the inactive leaf's edge, not the frame. The existing `Kit_Lock_*` parts cover all of this.
- **Inactive leaf.** **Dummy trim** at the same height (a knob or lever with no spindle), plus two **flush bolts** in its meeting edge.
  - Manual flush bolt: face 1" × 6-3/4" (S15), set in the edge; 3/4" throw, Ø1/2" bolt tip.
  - The top bolt sits near the top of the leaf on a 12" rod (longer rods for tall leaves). The bottom bolt goes into a dust-proof floor strike.
  - Fire-labelled pairs use **automatic or constant-latching flush bolts**, which need a **coordinator** at the head so the inactive leaf closes first.
- **Astragal.** An overlapping astragal screwed to the active leaf's meeting edge on the push face, so the pair can only swing one way.
- **Closers.** On both leaves, regular-arm or parallel-arm (`Kit_DoorCloser_*`, the spec in `interactables/10_spec.md` §2.4). Closer covers were sprayed to match the hardware (S1: "Surface door closers shall be spray painted to match door hardware").
- **Plates and silencers.** Kick plates on the push side at the pair width rule; four rubber silencers in the head for a pair (S1).
- **Reads 1990.** Knobs and roses, the spray-painted closer box, an exposed astragal, a flush-bolt face in the edge, slotted or Phillips screws.
- **Why not ours.** It cannot swing both ways. The astragal and the stops block one direction.

## 4. (b) Double-acting pairs (ours)

- **Function.** No latch, no stop and no astragal. Each leaf swings about 95° each way through a stopless frame (S20). There is no 门栓 by type: a spring latch can't retract when pushed from the far side, and special DA latch or rescue hardware is rare (S10). A DA pair that had to be secured after hours was usually backed by a separate single-acting door (S10).
- **How it stays shut.** Three options, by door size:
  - **DA spring hinges** (Bommer type, S21): cafés, kitchens, light leaves. The barrels sit on both faces, the leaf self-centres, and the hinge size grows with leaf weight and width.
  - **Centre-hung DA floor closer + top pivot**: heavy commercial leaves. Rixson 30 class ≤ 200 lb, up to 4'0" × 8'6"; Rixson 40 class ≤ 300 lb (S17). The 1942 manual already gave each DA door "floor hinges" (S2).
  - **Overhead concealed DA closer** in the head (S10).
  - **Never a surface closer** (S10).
- **Plates.**
  - **Push plates on both faces**: "two push plates" per DA door as early as 1942 (S2). Kick, mop or armor plates **on both faces**, and edge guards on both edges where carts hit (S10).
  - No pulls. A DA door is always pushed. The salvaged 1931 set (S22) is one leaf's pull face + push face, i.e. a single-acting door.
- **Code (period).** Under the 1979-UBC-based California Title 8 (S4), a DA door:
  - needs a **view panel ≥ 200 sq in** with its bottom ≤ 48" above the floor;
  - may not serve as an exit for > 100 occupants;
  - may not be part of a fire assembly;
  - may not carry panic hardware.

  So a DA pair in a 1990 office is a **non-rated interior** pair with a lite: suite to corridor, pantry, mailroom.
- **Sizes.**
  - Push plates 4" × 16" × 0.050": the S1 spec size; S5 also lists 3-1/2 × 15, 6 × 16 and 8 × 16.
  - Kick plates 8–16" high (S23), 10" the common office height (S12, S13).
  - Leaves 1-3/4" thick, 2'6"–4'0" wide, 7'0"–8'0" high (stock).

## 5. (c) Push/pull pairs (single-acting, no latch)

Used for toilet-room vestibules, open-office suite fronts, cafeteria exits on the push side.

- **Push side.** Push plate (as §4), centre 45" (S6) or 48" (S1); or a horizontal push bar at 42–45".
- **Pull side.** Choose one:
  - **straight pull, thru-bolted** (Ø3/4" on 8" c/c, 2-5/8" projection, 1-7/8" clearance (107); or Ø1" on 10" c/c, 11" OA, 2" clearance (111); S14);
  - the same pull **on a 3-1/2 × 15 or 4 × 16 pull plate** (107 × 70C, 111 × 70C);
  - an **offset pull** (Ø1", 10" c/c, 3-1/2" projection, 2-1/2" clearance, 4" offset (BF157); S14).

  Pull centre 42" (S1, S9). Back-to-back mounting keeps 1-1/2" plate-to-pull clearance (S11).
- **Other parts.** Closers and stops on each leaf, kick plate on the push side, mop plate (4–6") on the pull side (S12).
- **Reads 1990.**
  - The pull is a **round bar on round posts, 8–10" centres**, with the bolt heads showing on the push face, and the pull side gets a short pull plate.
  - Long (24–72") ladder or tube pulls, square-section bars and 2-1/2"-clearance "barrier-free" offsets are the later look. The BF/EZ ≥ 2-1/2" clearance is an accessibility option per S11 and S14; dating it to after 1990 is an inference.

## 6. (d) Exit devices on egress pairs

- **Where.** Stair and egress pairs, which are always single-acting, rated and latching. S4 forbids panic hardware on DA doors, and S10 says standard rim and mortise devices don't fit centre-hung DA doors.
- **Pair options.**
  - Two **surface vertical rod** devices: rods to a head latch and a floor latch (S16: centre case 8 × 2-3/4 × 2-3/8", projection 3-13/16", centreline 39-5/8").
  - Or **rim devices on both leaves with a removable mullion**.
  - Or one rim device on the active leaf + flush bolts on the inactive leaf (only where code allows).
- **1990 look.**
  - The 1992 spec still defaults to **"side-mounted crossbars"**, i.e. a tubular bar between two arm housings, 630 stainless, with "investment-cast cases" (S1). It names Von Duprin 3300 / 9900 rim, Sargent 60, Corbin 29 and Monarch 18.
  - Touchpad devices with a grooved case existed alongside them.
  - Modern tells: slim flat touchbars, an LED or electrified signal, a black finish.
- **Heights.** Pad or bar centre 38-13/16" (S1) to 40-5/16" (S6).
- **In our game.** This belongs to the C22 stairwell steel EXIT door (memory: real crash bar, closer, chain). It does not belong on the walnut DA pair.

---

## 7. What reads 1990 and what reads modern

| Reads c. 1990 | Reads modern (avoid) |
|---|---|
| A **small** plate (4 × 16 or 3-1/2 × 15) on a big leaf, with exposed **oval-head screws** (#6, Phillips per S1 "Phillips head screws shall be used at exposed conditions") | Concealed, self-adhesive plates with no screws (S5's "SA" option); Torx security screws |
| Square or radius corners, a **visible bevel** on a 1.27 mm plate | Plates thicker than 1/8", sharp laser-cut edges, frosted or etched logos |
| Satin stainless / satin chrome / dark bronze / polished brass | **Matte black (BHMA 622)**, PVD gold, antimicrobial "BIO" coatings (S5 lists US32DBIO as a current option) |
| Round bar pulls Ø3/4–1" on 8–10" centres, 1-1/2–2" clearance | 24–72" ladder pulls, square tube, flat-bar D-pulls, 2-1/2"+ clearance on every pull |
| Knob trim (Ø2", rose 2-1/4"); levers only where accessibility was asked for | Lever trim everywhere, slim-rose levers, card readers |
| Crossbar exit devices, painted closer boxes | Slim touchbars, electrified hardware, concealed closers on every door |
| Kick plates 10–16" in brushed stainless or brass, screws along both long edges | Full-height armor everywhere; adhesive kick plates |

## 8. How it looks worn (for the 630 set, with other finishes in §2.1)

- **Push plate.**
  - A palm-burnished oval about 70 × 220 mm around the centre: brushed grain partly polished out, a glossier and slightly brighter patch.
  - A grey finger-grease haze at its edges, fine random hairline scratches and rings.
  - Grime packed into the screw recesses and along the bevel, where it meets the wood, as a 0.5–1 mm dark line.
  - The **walnut around the plate darkens and gets an oily sheen** (a hand halo, strongest on the meeting-edge side). That is the leaf texture's job (`door_wood_walnut`), noted here only.
- **Kick plate.**
  - Horizontal scuffs and **black rubber heel streaks** in the bottom 50–180 mm.
  - Dings and a slightly wavy top edge, a bent bottom corner near the meeting edge, and cart scratches in long horizontal lines.
  - A dull mop-water tide line in the bottom 20–30 mm, and dirt at the screw heads.
  - On a DA door **both faces** wear the same way.
- **DA hinges.**
  - Dark grease weep at the barrel ends.
  - Polished rub marks where the barrel cap meets the leaf, and grey dust in the knuckle line.

---

## 9. Recommendation: one hardware set, HW-DA1 ("DA pair, push both faces")

**For:** the title-stream double doors (`Faces.Both`) and the station's static entrance (`Faces.Plus`, +Z face only, the same parts).

| # | Qty per leaf | Item | Spec |
|---|---|---|---|
| 1 | 1 per face | **Push plate** | 4" × 16" × 0.050" (0.1016 × 0.4064 × 0.00127 m). **US32D / BHMA 630** satin stainless, grain **vertical**. Four bevelled edges; square corners (the "No. 70" plate S1 names). Six #6 × 5/8" **oval-head Phillips** screws |
| 2 | 1 per face | **Kick plate** | 10" high × (leaf width − 1-1/2") × 0.050" (0.254 × 1.1574 × 0.00127 m). 630 satin stainless, grain **horizontal**. Four bevelled edges; #6 oval-head Phillips screws in two rows |
| 3 | 3 (existing) | **DA spring hinges** | Keep as built (motion and pivots unchanged); finish 630 to match |
| — | — | **Not in the set** | No pull, no lock or latch, no flush bolt or 门栓, no astragal, no coordinator, no closer, no exit device, no stops. The meeting edge keeps its brush strip |

**Why this set.**

1. **It is what a DA door gets.** "Two push plates" per DA door is in the 1942 federal manual (S2), and current DA practice still puts plates on both faces and no latch (S10). A pull on a DA door would never be used, since every approach is a push.
2. **It is era-true in size, finish and fixing.** The February 1992 spec writes "Push plates shall be 4-inch by 16-inch by 0.050 thick", brushed-stainless kick plates "0.050-gauge, beveled on 4 sides" at "door width less 1-1/2 inches for pairs of doors", 630 as the house finish, and Phillips heads on exposed screws (S1). Square-cornered, screw-fixed and bevelled is the catalogue plate (S5).
3. **It fits a door with no latch.** Nothing on it implies a bolt, so the leaf can swing either way without lying. A flat plate also has no projection to snag as the leaf passes the casing at 95°.
4. **It suits walnut and the scene.**
   - Satin stainless is the one finish that wears by burnishing rather than wearing through, which suits a long-used 1990 building.
   - It matches the already-built DA hinges and the kit's `Door / brushed steel` material, so no material swap is needed.
   - It sits with the station's chrome furniture.
   - Rejected: dark bronze (613) would echo the station's dark-bronze frames, but it is an "unstable" living finish (S8), would pull the eye on every room's door, and needs the hinges refinished. Brass reads hotel/executive lobby, not this corridor.

### 9.1 Build numbers (kit coordinates)

Leaf carrier space as in the kit: x from the hinge edge (0) toward the meeting edge (`along · x`, `along = −hand`), y up from the floor, z through the leaf (0 the centre plane).

| Part | Value |
|---|---|
| Leaf constants | `LeafWidth` 1.1955, `LeafThickness` 0.0445, `LeafBottom` 0.018, `LeafHeight` 2.639 (unchanged) |
| Push plate size | 0.1016 (x) × 0.4064 (y) × 0.00127 (z) |
| Push plate centre | x = along · (1.1955 − 0.0381 − 0.0508) = **along · 1.1066**; y = **1.143** (45" AFF; spans 0.9398–1.3462); z = ±(0.02225 + 0.000635) = **±0.022885** (top face at ±0.02352) |
| Push plate edge | long edge **38.1 mm** (1-1/2") from the meeting edge (S7); spans along · 1.0558 … 1.1574 |
| Push plate bevel | 45° chamfer 1.0 × 1.0 mm on all four edges of the face, 0.27 mm vertical land; corner radius 0.5 mm (sheared). ESTIMATE from the catalogue drawing (S5) |
| Push plate screws | 6 × #6 oval head Ø6.8 mm, dome 1.2 mm, Phillips cross 3.6 × 0.8 mm, 0.9 mm deep (the kit's `interact_door_common.oval_head`, d 0.0068, with a cross instead of a slot). Centres at plate-local x ±0.0413 (3/8" in from the long edges), y ±0.1905 and 0 (1/2" in from the ends, plus mid-height): the S5/S7 hole pattern. ESTIMATE ±2 mm |
| Kick plate size | 1.1574 (x) × **0.254** (y) × **0.00127** (z) (was 1.1455 × 0.25 × 0.004) |
| Kick plate centre | x = along · 0.59775 (centred; 19.05 mm inset each end, clearing the DA barrels that reach about 9.5 mm over the hinge-edge face and the meeting brush); y = 0.021 + 0.127 = **0.148** (bottom 3 mm over the leaf bottom, as now); z = **±0.022885** |
| Kick plate screws | 2 rows × 5: rows 12.7 mm in from the top and bottom edges; columns 25.4 mm from each end at 0.2767 m pitch. ESTIMATE (catalogues don't fix the pitch; ≤ 12" is usual practice) |
| Faces | `Faces.Both`: z −1 and +1. `Faces.Plus`: +1 only (as the kick plates now) |
| Remove | the `door handle / near|far` render boxes and the pull-post cylinders in `LeafHardwareMesh` |
| **Colliders (must not change)** | The two handle boxes and both kick plates carry BoxColliders today when `colliders` is true. Keep those colliders with their present names, centres and sizes as collider-only objects (renderer off), and give the new plates **no** collider. The visible plates move 1.27 mm proud; the old collider volumes stay. Open point for Red: the handle colliders would then be invisible 51 mm bumps; dropping them changes colliders and needs Red's yes |
| Material | Reuse `Door / brushed steel` (630 look). Add a satin #4 grain normal (anisotropic lines about 0.1 mm pitch, vertical on the push plate, horizontal on the kick plate), smoothness 0.62 base → about 0.75 in the burnish oval → about 0.45 in the finger haze. Detail and wear per §8 as a mask, not new hues |
| LOD (ESTIMATE) | LOD0 (holds at 0.3 m): chamfered plate + modelled screws, about 900 tris per push plate, about 1,500 per kick plate. LOD1: chamfered plate, screws in the normal map, ≤ 40 tris. LOD2: one box or quad, 12 tris. WebGL/phones use LOD1 at most (separate tier; desktop is the reference) |
| Untouched | Leaf textures (`door_wood_walnut`), leaf slab mesh, hinges, frame, saddle, brushes, pivots, PlaceLeaf |

---

## 10. Findings outside the set (flagged, not part of HW-DA1)

1. **No view panel.** Under 1979 UBC-based California Title 8, a DA door needs a lite of ≥ 200 sq in with its bottom ≤ 48" (S4), and the 1997 UBC kept 200 sq in (forum quote of ICBO 1997; S only). Our walnut DA leaves have none. A real 1990 DA pair would show, for example, a 10" × 30" lite per leaf at 43–73". This changes the leaf, which is the `door_wood_walnut` job's area, so it goes to Red as a question. It is not built.
2. **Hinge count.** Both period rules (S1, S6) give a 104" leaf **four** hinges; ours has three. A fourth DA spring hinge is visual only (same axis, no motion change). Red's call.
3. **Hinge layout.** The kit sets two hinges low, citing a Bommer layout. A dealer note says to set the third hinge "as close as possible to the top hinge" (S21, search summary only). Check against a real Bommer template before touching anything.
4. **Oversize leaf.** A 47" × 104" solid walnut leaf (≈ 150–170 lb, ESTIMATE) is at or past DA spring-hinge territory. In 1990 it would hang on a centre-hung DA floor closer with a top pivot (Rixson 30 rates 4'0" × 8'6", ≤ 200 lb; Rixson 40 ≤ 300 lb; S17), with a floor-closer cover plate in the saddle. That changes the pivot (centre-hung on the leaf centreline, about 2-3/4" from the jamb), so it is ruled out by "pivots must not change". Recorded only.
5. **Not an exit or fire door.** By S4 the DA pair must not be a rated or egress opening. That fits the title stream; keep the EXIT sign and crash bar on the C22 steel stair door only.

## 11. Media still to pull (not verified in this pass)

No period photograph or film frame of a DA office pair was confirmed this pass, because text fetches can't see images and nothing was downloaded. Leads for the Figma research board, all UNVERIFIED:
- archive photos of pre-renovation 1980s office corridors (e.g. Thomas Jefferson University, "Corridor, 3rd floor of Curtis Building, 1982", https://jdc.jefferson.edu/historical_photos/783);
- Steven Siegel's 1980s New York office lobbies (https://gizmodo.com/picture-of-the-day-january-25-2012-30794743);
- 1988–1993 films with office, hospital or cafeteria swing doors.

Each needs a look, a date check and a SOURCES.md row before it goes on a board.
