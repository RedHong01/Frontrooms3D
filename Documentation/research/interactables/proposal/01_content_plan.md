# 01 — Content plan: the doors + windows proposal (策划案), phase 1

Date: 2026-10-03. Owner: visual chat, interactables proposal workflow.

Red asked for this order: research first, then a Figma proposal, then he confirms, then we build. This plan is the slide-by-slide script for the Figma section. Nothing here lands in the game.

> **Fix pass, 2026-10-03, about 17:00.** A review found 22 issues; the Figma section and this plan were corrected (`02_figma.md` §0 has every change and its verdict). The deck now has **13 slides**: DW12 "Later, and already set" is new, and **DW01 is hidden** until the RE8 media (C1, C2) arrive. This plan now matches the slides. Red's one-page guide is `03_for_red.md`.

**Inputs, read in full:**
- `../00_map_constraints.md`, `../01_inventory.md`, `../02_period_hardware.md`, `../03_readability_placement_shots.md`, `../04_door_re8_gap.md`, `../05_locked_door_type.md`, `../06_period_windows.md`, `../10_spec.md`;
- `../images/`, `../harness/`;
- `../../interaction_audit/10_audit_report.md` §2–§4 and `FrontRoomsShotTimings.proposal.cs.txt` (class `Unlock`).

**Phase 1** uses evidence that exists today. Every model picture is a named, empty slot that phase 2 fills with pre-renders. At the time of writing, no `interact_*.py` module and no Cycles preview exists yet: `Tools/Blender/frontrooms_kit/assets/` has no `interact_*`, `scratchpad/interact_previews/G2/` is empty, and `scratchpad/interactables_prev/` does not exist.

**Changes from the suggested 9-slide order (DW00–DW08):** the material needs 12 slides.

| Here | Suggested | Why it changed |
|---|---|---|
| DW00 | DW00 | — |
| DW01 | DW01 | — |
| DW02 | DW02 | — |
| DW03 "The store's own doors" | DW03 (IP half) | The IP and era grounding now has real film and tape frames of doors, and needs its own slide |
| DW04 "Locked, from 20 m" | DW03 (test half) | The 6/12/20 m test grid needs the whole slide to stay legible |
| DW05 | DW04 | — |
| DW06 "A lock that takes a key" | DW05 (lock half) | Lock parts, motion and the head-dip timeline fill one slide |
| DW07 "A key on a hook" | DW05 (key half) | Key, tag and host is its own design, with its own distance numbers |
| DW08 "A window, not a pane" | DW06 (family half) | — |
| DW09 "The glass sits in a pocket" | DW06 (section half) | Red's window request ("real structure, not one pane") is answered by the section. It must be big |
| DW10 | DW07 | — |
| DW11 | DW08 | — |

---

## 1. Section, grammar and constants (read before building)

### 1.1 Where it goes

- **File** `0tCbAiVUlrPId3RWd9LRif`, page `2099:76`.
- **New section** "FRONTROOMS · DOORS + WINDOWS · PROPOSAL", 8280 × 3880. That is 12 frames of 1920 × 1080 in 3 rows of 4, laid out like HUNTER 2331:852:
  - frame x = 120, 2160, 4200, 6240;
  - frame y = 160, 1400, 2640.
- **Candidate spot:** x 7897, y 19137, under "RELAY PURSUIT · AUDIT + REDESIGN" (2441:3804, which ended at y 18977 in the 10:59 page dump).
  - Positions drift. List every section by absoluteBoundingBox (nested ones too) right before placing, and move the spot if anything overlaps.
  - Never edit another chat's section.
- **Frame names:** "DW00 · Three boxes", "DW01 · What RE8 does", and so on (§2).

### 1.2 Slide grammar (copied from HUNTER 2331:852; read these nodes first)

| Element | Value | Copy from |
|---|---|---|
| Running header | 5 slots at y 28, x 72 / 522 / 972 / 1422 / 1722: "Individual Game Project" · "FrontRooms · Doors + windows · proposal" · "Week 3 · Oct 3, 2026" · "Red Wang" · "NN / 13". IBM Plex Mono 13/16 (P1 Meta). Meta lives only here | 2331:881–885 |
| Title | x 72, y 139, h 80. Bayon 88/80 (P1 Title). No eyebrow above it | 2331:886 |
| Lede | x 1272, y 173, 576 × 52 (2 lines). Source Serif 4 24/26 (P1 Body; stands in for ABC Arizona). Every lede below is ≤ 96 characters | 2354:1006 |
| Statement | x 72, y 969, 1776 × 42. Source Serif 4 50/42 (P1 Statement). One line, ≤ 70 characters | 2354:1013 |
| Content | y 232–889. Half rows are 232–548 and 572–889 | 2345:852–870 |
| Chip | Inside the media, at +16 / +16, h 32, padding 10 / 7. Bayon 20/20, upper, +3 % (P1 Label). White on black; the slide's hero chip is yellow with black text | 2345:853 (yellow), 2345:862 (black) |
| Rule block | label (Bayon 20) → head (Bayon 88/80) → body (24/26, 2 lines) | HR03 2336:859 / 2336:860 / 2336:861 |
| Media slot | A rounded-rectangle named by its slot, with the image as a fill. Radius as 2345:852 | 2345:852 |

**Grid:** 12 columns, 72 margin, 24 gutter. Column k starts at x = 72 + 150·(k − 1), and a span of n columns is 150·n − 24 wide: 1 = 126, 2 = 276, 3 = 426, 4 = 576, 6 = 876, 8 = 1176, 12 = 1776.

**Text styles:** use the file's P1 text styles if they exist (P1/Text/Title, Label, Body, Statement, Meta). Read them before building, because Red retunes them by hand. Never use Inter. Read `get_figma_skill skill://figma/figma-use/SKILL.md` before any write.

**Rules from Red that shape every slide:**
- no captions under images, no meta lines on cards, no source footers (sources are in §5 and in the ledgers);
- real margins on all four sides;
- meaning lives in 88 Bayon, 50 Serif or big media;
- say it once.

### 1.3 Slot naming and placeholders

| Prefix | Meaning | Phase-1 fill |
|---|---|---|
| `media:dwNN_*` | Evidence that exists now (in-engine frames, film and tape stills) | the cropped image |
| `img:<Kit_or_Set>_persp` / `_front` / `_close` | Model pre-renders, filled in phase 2 | Placeholder (below) |
| `ref:*` | A reference that needs Red's download OK (`../proposal/media_candidates.md`) | Placeholder (below) |
| `diagram:dwNN_*` | A support diagram, drawn in Figma from the numbers in §4 | the diagram |

- **Placeholder:** fill with the light "hero panel" colour of K16 (read node 2326:1004). Centre one Bayon 20 line in mid grey: the slot name without its prefix.
  - Add "· PHASE 2" for `img:`.
  - Add "· AWAITING OK · C#" for `ref:`, with the candidate number from `media_candidates.md`.
  - Keep the slot's chip.
- **WIP previews:** if Cycles previews exist when the section is built (`scratchpad/interact_previews/G1…G4/` or `scratchpad/interactables_prev/`), they may fill an `img:` slot. Their chip then ends in "· WIP".
- **Uploads:** images go in with the Figma MCP `upload_assets` tool (load it with ToolSearch), into the named slots.

### 1.4 Paths used below

- `IMG` = `Frontrooms3D/Documentation/research/interactables/images/`
- `PM` = `Frontrooms3D/Documentation/research/interactables/proposal/media/`. New for this plan:
  - five stills cut from Red-approved videos;
  - seven full-resolution in-engine frames.
  - Ledger: `PM/SOURCES.md`.
- `AUD` = `Frontrooms3D/Documentation/research/interaction_audit/images/`
- `W2` = `Research/week02/` (course folder). Its stills are credited in `W2/…/SOURCES.md`.

Crops are given in source pixels as (x0, y0, x1, y1). Every crop's aspect matches its slot within 1 %. Every crop of an image that exists today was checked in a rough mock-up render (`scratchpad/dw_plan/mock/`). The RE8 crop is computed only, because that image does not exist yet.

---

## 2. The deck at a glance

| Slide | Frame name | Title (Bayon 88) | What it argues |
|---|---|---|---|
| 01 / 13 | DW00 · Three boxes | THREE BOXES | Today the door, window and key are each one Unity cube. You can see through shut doors |
| 02 / 13 | DW01 · What we take from RE8 (hidden until C1 + C2) | WHAT WE TAKE FROM RE8 | RE8 shows a deep, dark surround round a thick leaf. We take the frame, not the castle |
| 03 / 13 | DW02 · Stop behind every gap | A STOP BEHIND EVERY GAP | Red's Option A closes every slit. In-engine before and after |
| 04 / 13 | DW03 · The store's own doors | THE STORE'S OWN DOORS | In the IP the door is plain wood. The locked door is the store's steel back door |
| 05 / 13 | DW04 · Locked from 20 m | LOCKED, FROM 20 M | The tested pairing reads at 6, 12 and 20 m in three lights. The rejected ones do not |
| 06 / 13 | DW05 · One section eight doors | ONE SECTION, EIGHT DOORS | Each level has a free and a locked member. The section is shared; frame, leaf and lock vary |
| 07 / 13 | DW06 · A lock that takes a key | A LOCK THAT TAKES A KEY | Mortise lock, separate moving parts, and the head-dip timing |
| 08 / 13 | DW07 · A key on a hook | A KEY ON A HOOK | Key, ring and tag hang on a host. The host is what you find from afar |
| 09 / 13 | DW08 · A window, not a pane | A WINDOW, NOT A PANE | A window family per level, with real stops, a stool and blinds |
| 10 / 13 | DW09 · The glass sits in a pocket | THE GLASS SITS IN A POCKET | Section and fit. The opening and colliders stay as they are |
| 11 / 13 | DW10 · What changes | WHAT CHANGES IN THE GAME | Who does what, budgets, and what stays the same |
| 12 / 13 | DW11 · Your calls | YOUR CALLS | 8 calls that block phase 2, each with a default |
| 13 / 13 | DW12 · Later and already set | LATER, AND ALREADY SET | 2 calls that can wait for Run and Exit, and 7 small defaults Red can see and veto |

---

## 3. Slides

### DW00 · THREE BOXES (01 / 13)

- **Lede:** "Door, window and key are each one Unity cube. No stop, no lock, no bead or stool at the glass."
- **Statement:** "A shut door still shows the next room."

| Slot | x, y, w × h | Source | Crop | Chip |
|---|---|---|---|---|
| `media:dw00_slit` (hero) | 72, 232, 876 × 657 | `PM/ingame_door_gap_16_sideB_hinge_inline_darknear.png` (1920 × 1080) | (562, 0, 1362, 600) | yellow "SEE-THROUGH SLIT · 1.2 M · NEAR ROOM DIMMED" |
| `media:dw00_swing_a` | 972, 232, 201 × 316 | `PM/ingame_door_gap_18_open_fromA_t0.13s.png` | (662, 0, 1349, 1080) | "PUSHED FROM A" |
| `media:dw00_swing_b` | 1197, 232, 201 × 316 | `PM/ingame_door_gap_21_open_fromB_t0.13s.png` | (617, 0, 1304, 1080) | "PUSHED FROM B" |
| `media:dw00_locked` | 1422, 232, 426 × 316 | `AUD/48_door_locked_before_hud.png` | (600, 273, 1320, 807). This keeps the prompt and drops the HUD title | "LOCKED = ONE LINE OF TEXT" |
| `media:dw00_key` | 972, 572, 426 × 317 | `AUD/52_key_2m.png` | (640, 300, 1280, 776) | "KEY = GLOWING CUBE · 2 M" |
| `media:dw00_window` | 1422, 572, 426 × 317 | `AUD/04_window_L0_intact_oblique.png` | (700, 80, 1660, 794) | "WINDOW = ONE 30 MM BOX" |

**Claims and sources** (speaker notes, not on the slide):
- **Slits.** Hinge 10 mm, latch 10 mm, head 20 mm, floor 0. The reveal behind them is bare wall. Measured: `04` §1.1, `images/door_gap_capture_log.txt`, `00` "Door slits".
  - The far troffer shows through the hinge slit in frame 16 (`04` §1.4).
  - The hero frame is from the dark-near harness setup (near lamps at 8 %), which makes the slit easy to see.
- **Swing.** 95° both ways, away from whoever opens, in 0.55 s (`01` §1.4; `MapWorld` `SwingAway`).
- **Locked.** The only cue is the prompt plus an FMOD one-shot. There is no lock to put a key into (audit F1, frame 48).
- **Key.** An emissive yellow cube, 0.32 × 0.12 × 0.12 m, spinning 90°/s. It is picked up within 0.9 m horizontally (`01` §3.2–3.3; audit F10, F14).
- **Window.** A cube, 1.4 × 1.65 × 0.03, alpha 0.28, with no stop and no sill (`01` §2.2; audit F3).

### DW01 · WHAT WE TAKE FROM RE8 (02 / 13; hidden until C1 + C2 arrive)

- **Lede:** "RE8 shows a thick leaf in a deep, dark surround. Its gaps read as dark lines, never light."
- **Statement:** "We copy the frame, not the castle."

Layout: two RE8 references are stacked in columns 1–4. Six HR03-style rule blocks sit in columns 5–12 (2 columns × 3 rows).

| Slot | x, y, w × h | Source | Chip |
|---|---|---|---|
| `ref:re8_steam_castle_hall` | 72, 232, 576 × 316 | C1 (Steam screenshot, 1920 × 1080; crop to 1.82 when it arrives: (0, 13, 1920, 1067)) | "DEEP SURROUND, DARK EDGE LINES" |
| `ref:re8_walkthrough_1-36-39` | 72, 572, 576 × 317 | C2 (walkthrough 1:36:38.9; same crop) | "HAND PUSHES, LEAF SWINGS AWAY · THIRD-PERSON CAMERA" |

Rule blocks have no label line: head Bayon 88/80, body Serif 24/26. They sit at x 672 and 1272 (width 576), with heads at y 232 / 451 / 670 and bodies 88 px below each head.

| Head | Body | What we build (`10` §1.4) |
|---|---|---|
| DEEP FRAME | "RE8: a deep moulded surround. Ours: casing on both faces, a lined reveal." | casing 75.5 mm, 25 mm proud; 2 mm lining |
| THICK LEAF | "RE8: the edge shows as it swings. Ours: a 44 mm leaf with edge bands." | 44 mm leaf, 1.5 mm arrises, edge bands |
| THRESHOLD | "RE8: a dark threshold board. Ours: a 12 mm saddle under the leaf." | saddle 0–0.012, 1:2 bevels |
| PUSH, IT SWINGS | "RE8: the hand pushes at the latch side; the leaf swings away. Ours: hardware at 1.0 m." | lock at Y 1.000, Z 0.920 |
| DARK GAP LINE | "RE8: no light at the edges (how is unconfirmed). Ours: a 16 mm stop behind every gap." | stop 16 × 35.5 mm on the push side |
| NOT COPIED | "RE8's raised panels belong to a castle. Our 1990 door is flush." | flush 1955–93 commercial leaf |

The first build stated our numbers (44 mm, 12 mm, 1.0 m, 16 mm) as RE8 facts. RE8's stops, rebates and leaf sizes are UNVERIFIED (`04` §2.3, `10` §13 item 1), so every block now says what RE8 shows, then what we build.

**Claims and sources:**
- **V1 and V2 were seen by the research agent** (`04` §2.1). The Steam main-hall screenshot shows a deep surround, a dark perimeter and hinge plates. In the walkthrough frames at 1:36:37.5–1:36:40.4, the hand pushes at the latch stile and the leaf swings away.
- **Principles** come from `04` §2.2 and `10` §1.1.
- **UNVERIFIED** (say this aloud if asked): whether RE8 doors swing away from both sides, and whether its frames have true stops (`04` §2.3). Option A is Red's choice either way (`10` §13 item 1).
- **"Not copied":** RE8's raised panels belong to a château. The 1990 US commercial door is flush (`10` §2.1 "Why no panelled doors"; `02` §2.1). HR03 "Not ours to use" also rules out borrowed iconography (`05` §3).
- **Media:** C1–C3 in `media_candidates.md`. Until Red approves, both slots are placeholders.

### DW02 · A STOP BEHIND EVERY GAP (03 / 13)

- **Lede:** "Your Option A: one swing direction, a real 16 mm stop. In the prototype every gap turns a corner."
- **Statement:** "No straight line gets through, from either side."

| Slot | x, y, w × h | Source | Crop | Chip |
|---|---|---|---|---|
| `media:dw02_today` | 72, 232, 276 × 621 | `PM/ingame_door_gap2_12_today_sideB_hinge_inline.png` | (803, 0, 1123, 720). The slit is at x 962–964 | "TODAY · HINGE JAMB · 1.2 M" |
| `media:dw02_option_a` | 372, 232, 276 × 621 | `PM/ingame_door_gap2_74_optionA_sideB_hinge_inline.png` (same camera) | (803, 0, 1123, 720) | yellow "OPTION A · PROTOTYPE" |
| `diagram:dw02_jamb_section` | 672, 232, 576 × 657 | drawn (§4 D1) | — | — |
| `media:dw02_leak` | 1272, 232, 576 × 316 | `PM/ingame_door_gap3_00_floor_leak.png` | (700, 560, 1220, 845), 2× on the threshold | "LIGHT LEAKS THROUGH THE LEAF · B PROTOTYPE" |
| `media:dw02_proxy` | 1272, 572, 576 × 317 | `PM/ingame_door_gap3_01_floor_shadowproxy.png` | (700, 560, 1220, 845) | "+ SHADOW BOX IN THE LEAF · SAME FIX ON A" |

**Claims and sources:**
- **Red chose Option A** (`10` §0 D1; `VISUAL_CHAT_TASKS.md` W6): single-acting, with real 16 mm stops.
- **The A2 hinge.** Three 4-1/2" butts. The pivot moves 29.5 mm toward the swing side and 9.8 mm into the opening, and the collider's closed pose does not change (`10` §1.3).
- **Gap closure.**
  - When shut, the hinge, latch and head gaps are 3 mm, closed by a stop that overlaps 13 mm. Every path is L-shaped (`10` §1.4).
  - The floor gap is 3 mm above a 12 mm saddle. Seeing through it needs a sightline under 3.9°, which means standing about 23 m away, where it is sub-pixel (`10` §1.4).
- **Swing clearance** is at least 2.8 mm everywhere from 0 to 95°, computed in 0.1° steps (`10` §1.5, D3).
- **Prototype evidence.** The prototype is boxes, not final meshes. Frames 12 and 74 share one camera (`PM/SOURCES.md`; `04` §5.4, §6.2).
- **Shadow leak.** A door-shaped patch is the far lamp's shadow leaking through the thin leaf. A shadows-only box removes it (`04` §1.5; `10` §1.7: 0.10 m, named `Leaf shadow`).
  - The pair was captured on the retired B prototype. The fix is the same for A.
  - Light through walls from unshadowed lamps is audit F5, not a door fault. Say so if Red asks.
- **Test T1** re-runs the ray harness on the real meshes, both handings and both faces (`10` §11). Phase 2 replaces the prototype frame with a T1 frame.

### DW03 · THE STORE'S OWN DOORS (04 / 13)

- **Lede:** "In the film and in Kane's tapes the door is plain wood: flush leaf, dark frame, knob or lever."
- **Statement:** "Free: the IP's wood door. Locked: a 1990 steel back door, not seen in the film."

| Slot | x, y, w × h | Source | Crop | Chip |
|---|---|---|---|---|
| `media:dw03_a24_door` (hero) | 72, 232, 876 × 657 | `PM/a24_trailer_0127_wood_door_knob.jpg` | (60, 21, 1441, 1057). The letterbox is cut off | yellow "THE IP'S DOOR: WOOD, DARK FRAME, KNOB" |
| `media:dw03_a24_dark_door` | 972, 232, 426 × 316 | `PM/a24_trailer_0139_dark_door_level0.jpg` | (100, 200, 1200, 1016) | "DARK DOOR = OPEN DOORWAY AT 12 M" |
| `media:dw03_kane_oak_door` | 1422, 232, 426 × 316 | `PM/kane_emg_0053_oak_door_lever.jpg` | (330, 310, 970, 786) | "KANE: OAK LEAF, LEVER, WOOD CASING" |
| `media:dw03_kane_bolts` | 972, 572, 426 × 317 | `PM/kane_emg_0125_door_six_bolts.jpg` | (330, 300, 970, 776) | "SIX BOLTS: 3–4 PX AT 12 M" |
| `media:dw03_locked_mockup` (was the blue-tape still) | 1422, 572, 426 × 317 | `IMG/05_r3_close_L0lit_recommended.jpg` | (180, 120, 1150, 842): the wood door and the almond locked mock-up in game light | "LOCKED: ALMOND STEEL, DARK FRAME · MOCK-UP" |

Chips state claims, not sources; times and credits live in `PM/SOURCES.md`. The blue-tape still left the slide; its point (blue marks openings, so it is not our lock colour) stays in the notes below.

**What each frame proves** (speaker notes):
- **1:27.** A flush brown wood leaf with a visible edge, a round knob and a dark casing. It swings open into Level 0. This is the IP's own door, and it matches the free door (`05` §3: `02_film_shots.md` F09 "plain wood-veneer interior door … dark frame, brass lever"; `03_ip_canon.md:187`).
- **1:39.** Inside a Level 0 room, a dark door in a deep reveal reads as a dark rectangle. This is HR03 rule 03 ("Doorway first … a dark shape at 12 m"). It is why the locked door must **not** be dark: it would look like an open doorway, or like the Relay standing in one (`05` §1 point 2, §3).
- **0:53.** Kane's survey still: a light oak flush door, a lever and a wood casing in Level 0.
- **1:25.** Kane marks a special door with six bolts on its latch side: hardware. DW04 shows that hardware is 3–4 px at 12 m and cannot carry the read. We use the leaf's value instead (`05` §2 pixel budget).
- **1:55.** Blue painter's tape marks openings in the film, and `Kit_DoorwayStuds` already uses it (R44 2350:1286). So blue is not our "locked" colour (`05` §3).

**IP and era grounding** (speaker notes; the Figma sources, read by `05` §3):
- **IR01 "One bad photo"** (2320:2056): it began as a real room, so the door is real commercial hardware at real size.
- **IR04 "Yellow by accident"** (2320:2141): "Keep the light neutral. Put the yellow in the walls." An off-white enamel door carries its own value.
- **IR05 "Copy, paste, pile"** (2320:2170): one key-door model and one sign text, copied.
- **IR06 "The games"** (2320:2195): "We add a choice at every exit." Free against locked is that choice, so it must read before you walk up.
- **HR03** (2331:873):
  - rule 02 "FROM 1990": an ordinary 1990 building difference, so no chains, boards, red paint or glowing panels;
  - rule 03, as above;
  - "Not ours to use".
- **K00** (2324:853): the kit has no door yet. Both door types become new kit entries, dated in the era table (`05` §4).
- **R44** (2350:1286): the blue tape.
- **The back of house is the IP's threshold.** In the A24 film the way in is found in the store's utility space, "while checking the breaker box" (Wikipedia, read by `05` §3).
- **Period practice** (`05` §4; `02` §3, §11):
  - hollow-metal doors go to "back entries, corridors, and anywhere security is a concern";
  - the storeroom function (F86) is locked from outside and opens with a key;
  - knobs stayed on service doors before the ADA. ADAAG was published on 26 Jul 1991, so 1990 is pre-ADA.
- **Era table rows to add** to `22_era_lock.md` when the models land (`05` §4):
  - veneer office door, 1960–2000;
  - storeroom door, 1960–2000; the plate is current in 1990.

**Honesty notes:**
- The A24 door at 1:27 has a **knob**. Our free door uses a **lever**, so the two types differ at 2–5 m (bar against dot). F09 and Kane 0:53 both show levers. If Red prefers the film's knob, the free/locked hardware cue weakens; value still carries the read beyond 6 m. This is now a visible row on DW12 ("Also set").
- **No IP frame shows a steel back door** (wanted: C7). The locked door is grounded in 1990 practice, not in a film frame.

### DW04 · LOCKED, FROM 20 M (05 / 13)

- **Lede:** "Mock-ups at real size in game light. Left: wood door. Centre: locked. Right: open doorway."
- **Statement:** "The locked door is 2–3× brighter than the wood door, in every light."

The grid is 4 columns × 3 rows of 426 × 200 tiles: x = 72 / 522 / 972 / 1422, y = 232 / 456 / 680. Every tile is a native 1080p crop of 640 × 300 (aspect 2.133).

**Tiles 1–3: the chosen pairing (REC), `IMG/05_r3_sheet_recommended.jpg`**
- Columns: 6 m (220–860), 12 m (870–1510), 20 m (1520–2160).
- Rows: Level 0 lit (60–360), Level 0 dim (740–1040), Office (1420–1720).
- Crop = (column x0, row y0, column x1, row y1).

| Row \ column | 6 m (x 72) | 12 m (x 522) | 20 m (x 972) | Rejected (x 1422) |
|---|---|---|---|---|
| Level 0 lit (y 232) | chip yellow "LEVEL 0 · 6 M" | chip "12 M" | chip "20 M" | P1, `IMG/05_r1_sheet_L0lit.jpg` (870, 60, 1510, 360). Chip "NO · WOOD DOOR + DEADBOLT ONLY" |
| Level 0 dim (y 456) | chip "LEVEL 0 · DIM" | — | — | P3, `IMG/05_r1_sheet_L0dim.jpg` (870, 990, 1510, 1290). Chip "NO · DARK STEEL · DIM" |
| Office grade (y 680) | chip "OFFICE GRADE" | — | — | P2p, `IMG/05_r2_sheet_Office.jpg` (1520, 990, 2160, 1290). Chip "NO · KIT PUTTY · 20 M" |

Slot names: `media:dw04_l0lit_6m`, `media:dw04_l0lit_12m`, … `media:dw04_office_20m`, then `media:dw04_no_p1`, `media:dw04_no_p3`, `media:dw04_no_p2p`.

**Claims and sources** (`05` §5; values in `IMG/05_r1_metrics.json`, `05_r2_metrics.json`, `05_r3_metrics.json`):
- **Locked against free, bias-corrected stops** (lit / dim / Office): **+1.5 / +1.05 / +1.7**. That is 2.1–3.3× the luminance, hence "2–3×".
- **Locked against an open dark doorway:** +5.1 / +2.25 / +6.6 stops.
- **The rejects:**
  - P1 (the veneer door plus a deadbolt): 0.0 stops; it is the same door.
  - P3 (dark bronze steel): only +0.3 stops above an open dark doorway in a dim cell, so it fails HR03 rule 03.
  - P2p (the kit's putty paint): −0.1 stop against the wall, so it melts into the Office wall at 20 m.
- **Why value, not colour.** The Office grade cuts saturation by 22 %. Hardware is 3–7 px at 6–12 m.
  - Pixels per metre at 1080p = 691 / d (`03` §1.1).
  - The leaf is 56 × 120 px at 12 m (`05` §2).
- **Dark frame.** Without it the almond leaf bleeds into the pale Office wall (`05` §5.3 item 4; compare `05_r2_sheet_Office.jpg` rows 1 and 2).
- **Mock-ups, not models.** There is no reflection probe, so the metals render dark (`05` §9 items 1 and 3). Test T6 re-runs this with the real kit and adds Run and Exit (`10` §11). Phase 2 swaps these tiles for T6 frames.

### DW05 · ONE SECTION, EIGHT DOORS (06 / 13)

- **Lede:** "Same 16 mm stop, 12 mm saddle, 44 mm leaf, hinges and 1.0 m hardware. Frame, leaf and lock vary."
- **Statement:** "Lobby and Office ship first; Run and Exit wait for their map doors."

Cells are 4 columns (x 72 / 522 / 972 / 1422) × 2 rows, on the half-row grid. (`10` D5: 2 frame meshes, 3–4 leaves and two locksets, so "one frame" was wrong.)
- Row FREE: slot y 232, 426 × 316.
- Row LOCKED: slot y 572, 426 × 317.
- No text under the slots (Red's rule). Each chip names the member and its materials. Slots take phase-2 renders at slot aspect; §6 has the camera.

| Cell | Slot | Chip (member · materials) | Kit assets in the render (`10` §2.1) |
|---|---|---|---|
| Lobby free (L0-F) | `img:DoorSet_L0-F_persp` | "LOBBY · FREE · WALNUT, VENEER, BRASS · WIP" | `Kit_DoorFrame_Wood`, `Kit_DoorLeaf_Veneer`, `Kit_Lock_Rose_Brass` + `Kit_Lock_Lever_Brass` (both faces), `Kit_Lock_Latchbolt_Bored_Brass`, `Kit_Lock_StrikeBored_Brass` |
| Office free (OF-F) | `img:DoorSet_OF-F_persp` | "OFFICE · FREE · OAK LEAF, CLOSER · WIP" | `Kit_DoorFrame_Steel`, `Kit_DoorLeaf_Veneer_Oak`, rose + lever (chrome), bored latch and strike, `Kit_DoorCloser_Body/Arm/Forearm/Shoe` |
| Run free (RN-F, P2) | `img:DoorSet_RN-F_persp` | "RUN · FREE · P2 · WARD DOOR, NO LATCH" | `Kit_DoorFrame_Steel`, `Kit_DoorLeaf_Ward`, closer |
| Exit free (EX-F, P2) | `img:DoorSet_EX-F_persp` | "EXIT · FREE · P2 · PUSH BAR, LIT EXIT" | `Kit_DoorFrame_Steel_Alu`, `Kit_DoorLeaf_Veneer_Oak`, `Kit_ExitDevice_Crossbar`, rose + lever, closer, `Kit_ExitSign` |
| Lobby locked (L0-K) | `img:DoorSet_L0-K_persp` | "LOBBY · LOCKED · ALMOND STEEL, KNOB · WIP" | `Kit_DoorFrame_Steel`, `Kit_DoorLeaf_Steel`, the mortise set (escutcheon, knob, cylinder shell, plug, deadbolt, mortise latch, mortise strike; chrome), `Kit_DoorSign` "EMPLOYEES ONLY", `Kit_DoorNumberPlate` |
| Office locked (OF-K) | `img:DoorSet_OF-K_persp` | "OFFICE · LOCKED · PLUS A CLOSER · WIP" | as L0-K, plus the closer |
| Run locked (RN-K, P2) | `img:DoorSet_RN-K_persp` | "RUN · LOCKED · P2 · STAFF ONLY SIGN" | as OF-K; sign cell 1. Option: `Kit_DoorLeaf_SteelLite` |
| Exit locked (EX-K, P2) | `img:DoorSet_EX-K_persp` | "EXIT · LOCKED · P2 · DEAD EXIT SIGN" | `Kit_DoorFrame_Steel_Alu`, `Kit_DoorLeaf_Steel`, mortise set, `Kit_ExitSign_Dead`, number plate |

"WIP" = a phase-1 Blender preview in the measured Unity colours (`10` §7.3), frame and leaf only (`PM/SOURCES.md`). Run and Exit stay placeholders: a preview would need the white Run wall and the cyan Exit light to say anything true.

**Claims and sources:**
- **Shared grammar** (`10` §2.1):
  - the §1.4 section: 2 mm lining, 16 mm push-side stop, 12 mm saddle, 44 mm leaf;
  - three butts at 0.254–0.368 / 1.0565–1.1705 / 1.859–1.973 m;
  - hardware centred at Y 1.000, Z 0.920 on both faces;
  - kick plates 3 mm above the leaf bottom;
  - signs at Y 1.524 (60");
  - number plate at Y 1.000, Z 0.790.
- **The value rule** (`10` §2.2):
  - free leaves are luminance 0.17–0.19;
  - locked leaves are almond enamel, about 0.56 (`Door_Enamel`, a new surface; fallback `Painted_Metal`, 0.61);
  - steel frames are `Prop_SteelBrown`, 0.03.
- **Which member the map picks.** Office if the Standard side is Office, otherwise Lobby. FREE or LOCKED comes from the per-door lock flag. An unlocked locked door keeps its locked model (`10` §2.1; `00`).
- **P1 and P2.** Lobby and Office are P1 (the map builds those doors today). Run and Exit are P2: no map doors exist there yet (`10` §2.1).
  - **UNVERIFIED:** Run and Exit readability. The Run wall (0.74) sits above the almond leaf, so the bronze frame must draw the outline. Test T6 decides (`10` §2.2).
- **Title-stream double doors** (2.4 m) would need a pair version: P3, not in this proposal (`10` §2.1).

### DW06 · A LOCK THAT TAKES A KEY (07 / 13)

- **Lede:** "Key cylinder at 1.0 m, knob below, a deadbolt and a latch. Each moving part is its own model."
- **Statement:** "Head dips, key goes in, turns 90°, bolt slides, door pops ajar."

**Row 1: the head-dip framing strip.** Source `IMG/05_r3_headdip_strip_L0lit.jpg` (3240 × 380). Five tiles of 640 × 360 at x = 0 / 650 / 1300 / 1950 / 2600, y 10–370. Slots are 336 × 189 at y 232, x = 72 / 432 / 792 / 1152 / 1512.

| Slot | Crop | Chip |
|---|---|---|
| `media:dw06_dip_start` | (0, 10, 640, 370) | "OLD MOCK-UP · KEY IN KNOB" |
| `media:dw06_dip_mid` | (650, 10, 1290, 370) | "HEAD DIPS" |
| `media:dw06_dip_pose_p` | (1300, 10, 1940, 370) | yellow "POSE P · 0.58 M" |
| `media:dw06_dip_key_in` | (1950, 10, 2590, 370) | "KEY IN" |
| `media:dw06_dip_turned` | (2600, 10, 3240, 370) | "TURNED" |

**Row 2, left (columns 1–8): eight part slots of 276 × 204.** They sit at x = 72 / 372 / 672 / 972 and y = 445 / 673. The chips name the part; the timeline alone carries the motion numbers (say it once).

| Slot | Chip | Motion (`10` §3.1) |
|---|---|---|
| `img:Kit_Lock_Escutcheon_persp` | "ESCUTCHEON · FIXED" | static; 57 × 203 mm plate |
| `img:Kit_Lock_CylinderShell_persp` | "CYLINDER · FIXED" | static; the IC figure-8 core face |
| `img:Kit_Lock_Plug_persp` | "PLUG" | rotates 0 → 90° with the key |
| `img:Kit_Lock_Knob_persp` | "KNOB" | ±40°; 54 mm ball with a knurled band; 65 mm proud |
| `img:Kit_Lock_Deadbolt_persp` | "DEADBOLT" | slides 25 mm |
| `img:Kit_Lock_Latchbolt_Mortise_persp` | "LATCH · 19 MM" | slides 19 mm |
| `img:Kit_Lock_StrikeMortise_persp` | "STRIKE · TEARS OFF" | static; detaches on the Relay break |
| `img:LockSet_Mortise_persp` | yellow "PROPOSED LOCK · POSE P · WIP" (phase 1: the G2 pose-P render, key in and turned) | the set on a steel leaf, seen from pose P with the key in |

**Row 2, right (columns 9–12):** `diagram:dw06_unlock_timeline` at 1272, 445, 576 × 444 (§4 D2).

**Claims and sources:**
- **The lock** (`10` §0 D6, §3.1): a mortise lock with an interchangeable-core cylinder at 1.000 m, a knob below it on a tall escutcheon, a 25 mm deadbolt and a deadlocking latch.
  - Keying both faces is a game liberty: real storeroom hardware is keyed on one side only (`02` §3.3).
  - No brand marks anywhere. Interchangeable-core cylinders with the figure-8 face are 平面视觉's binding era note (`10` intro, R10).
- **Key and keyway.** One profile serves both the key and the plug: a 25 mm blade = `Unlock.InsertDepth`, and six pin tips (`10` §3.2).
- **Pose P** (`10` §3.4; audit §3.2; `FrontRoomsShotTimings.proposal.cs.txt` `Unlock`):
  - keyhole at (±0.0315, 1.000, 0.920);
  - camera 0.45 m out along the door normal, eye dropped 0.25 m to 1.37 m;
  - that puts the camera about 0.58 m from the keyhole, pitched about 39° down;
  - FOV 76 → 62.
  - At 1.0 m the dip reads strongest: about 39° down, against about 21° for a lock at 1.2 m (`03` §3.3).
- **The mock-up strip** used `05`'s earlier storeroom knob, with the keyway in the knob. The spec now puts the cylinder above the knob. So the strip proves the framing, not the parts.
  - Test T5 replaces it with the real parts (`10` §11).
  - The mock key and fob turn as one block. In the real shot the ring and tag hang under gravity (`05` §7).
- **Choice for Red** (DW11, call 4): deadbolt plus knob, instead of `05`'s storeroom knob (`10` §12 item 6).

### DW07 · A KEY ON A HOOK (08 / 13)

- **Lede:** "Brass key, split ring, plastic tag with a typed number. Hung still, under a steady lamp."
- **Statement:** "You spot the rack from the doorway. The key is the reward."

**Row 1: four set renders, 426 × 316 each,** at y 232, x = 72 / 522 / 972 / 1422.

| Slot | Chip | Render content (`10` §4) |
|---|---|---|
| `img:KeySet_Rack_persp` | yellow "LOBBY · KEY RACK" | `Kit_KeyRack` (renamed from `Kit_KeyBoard`, which is the same file as the computer keyboard `Kit_Keyboard` on macOS; `02_figma.md` §9) with `Kit_KeyRing` + `Kit_Key_Zone` + `Kit_KeyTag_Rect` hung on `hook_6`; the other hooks empty |
| `img:KeySet_Cabinet_persp` | "OFFICE · KEY CABINET" | `Kit_KeyCabinet` (door open about 175°), key hung on `hook_r1_c4` |
| `img:KeySet_Hook_persp` | "ONE HOOK · 1 KEY IN 4" | `Kit_KeyHook` at 1.476 m, with a red or blue tag only |
| `img:KeySet_Desk_persp` | "DESK · KEY LIES FLAT · WIP" in phase 1 (the G3 flat-desk render); the final render keeps "TAG OVER THE EDGE" | key flat on a 0.74 m desk top, tag hanging over the front edge |

**Row 2:**

| Slot or text | x, y, w × h | Content |
|---|---|---|
| `ref:henryford_tivoli_motel_key` | 72, 572, 426 × 317 | C8. Chip "THE HENRY FORD · MOTEL KEY 1955–80" |
| `img:KeyParts_Lineup_front` | 522, 572, 426 × 317 | Key, ring, the 3 tag shapes × red / blue / white, and one door number plate with the same number. Chip "9 TAGS · SAME NUMBER ON THE DOOR" |
| Distance numerals | 972–1848, y 572–889 | A Bayon 20 label "RECOGNISED UP TO, 1080P" at y 572. Three columns at x 972 / 1272 / 1572 (276 wide): a Bayon 88/80 numeral at y 612, then a Bayon 20 label at y 704. **"5 M"** / "KEY · 58 MM"; **"7 M"** / "LONG TAG · 76 MM"; **"26 M"** / "KEY RACK · 0.30 M" |

**Claims and sources:**
- **Recognition distance** = 691 × size / (8 px) at 1080p with FOV 76°, by Johnson's criteria (`03` §1.1):
  - a 58 mm key, 5.0 m (the spec key, `10` D8);
  - the 76 mm long tag, 6.6 m;
  - the 57 mm rectangular tag, 4.9 m;
  - a 0.30 m rack, 25.9 m;
  - a 0.36 m cabinet, 31 m.
- **The key** (`10` §3.3): brass, 58 mm long, a 25 mm blade, and its origin at the shoulder on the turning axis. One bitting for every zone; the tag is the identity.
- **Tags** (`10` §4.1):
  - a plastic tag with a paper insert (平面视觉's era note R10);
  - 3 shapes × red / blue / white = 9 identities, with existing slots;
  - the number is typed in Courier Prime;
  - the locked door's number plate shows the same number and colour;
  - no yellow, orange or manila (`03` §1.2: "Put the yellow in the walls").
- **Hosts** (`10` §4.2–4.3):
  - rack in the Lobby, cabinet in the Office, a single hook for at most 1 key in 4;
  - hook heights 1.40–1.55 m;
  - `key_hook` within 0.55 m of floor you can stand on, against the 0.9 m pickup;
  - wired to the map's existing `KeySpot` marker and its `host` field (`FrontRoomsRoomModuleData.cs:66-78`).
- **No spin, no emission.** The glint is the lamp's highlight on bevels of 0.4 mm and more. The key cell's lamp is forced Steady (`10` §4.3; `03` §1.4 P1–P6).

### DW08 · A WINDOW, NOT A PANE (09 / 13)

- **Lede:** "One window per level, with the doors' casing and 16 mm stops, and a pocket for the glass track's pane."

**Fix-pass layout:** Kane 3:08 leads (row 1, x 72), then wood, steel and aluminium (x 522 / 972 / 1422), each rendered in a wall with the stand-in glass. The white-steel placeholder moved to row 2, x 72. The pane itself belongs to the glass-destruction track (Red's decision vi).
- **Statement:** "Wood in Level 0, bronze steel in the Office, like the doors."

**Row 1: the four members, 426 × 316 each,** at y 232, x = 72 / 522 / 972 / 1422.

| Slot | Chip | Member (`10` §5.1; `06` §3) |
|---|---|---|
| `img:Kit_WindowFrame_Wood_persp` | "LOBBY · WOOD + STOOL" | W-L0 back-office light: walnut liner, ranch casing both faces, through-stool with horns, apron, 16 mm wood stops |
| `img:Kit_WindowFrame_Steel_persp` | "OFFICE · BRONZE STEEL" | W-OF borrowed light: pressed steel, integral stop on the hall face, screwed stop on the office face (30 screws) |
| `img:Kit_WindowFrame_Steel_Enamel_persp` | "RUN · WHITE STEEL · P2" | W-RN: the W-OF mesh in white enamel |
| `img:Kit_WindowFrame_Alu_persp` | "EXIT · ALUMINIUM · P2" | W-EX: clear-anodised wrap casing, snap-in bevelled beads, black gaskets |

**Row 2:** y 572, 426 × 317.

| Slot | x | Source | Crop | Chip |
|---|---|---|---|---|
| `media:dw08_kane_window` | 72, row 1 | `PM/kane_emg_0308_framed_window_1990.jpg` | (0, 120, 1290, 1080). It starts below the burnt-in VHS stamp, so no part of the stamp shows | yellow "1990 CONTROL ROOM · WINDOW INTO LEVEL 0" |
| `ref:sears1993_levolor_miniblinds` | 522 | C10 | — | "SEARS 1993 · 1-INCH BLINDS" |
| `ref:us4463535_glass_stop` | 972 | C11 | — | "USG PATENT 1982 · GLASS STOP" |
| `img:Kit_MiniBlind_Raised_persp` | 1422 | phase 2 | — | "OFFICE · RAISED BLIND" |

**Claims and sources:**
- **Kane 3:08.** A 1990 control room, its VHS stamp reading 06/19/1990, looks into Level 0 through a deep-framed interior window. That is exactly the map's case: a room looking into a tall hall.
- **Where windows are** (`06` §1.1, §1.4): only on edges where a Tall zone meets a Low or Standard zone. So every map window has a Level 0 tall hall on one side. Run and Exit have no map windows yet (P2).
- **Period** (`06` §2):
  - borrowed lights are fixed glass, with no sash and no hardware. Wood, hollow-metal and aluminium families existed;
  - stops are screwed on the secure side at 9" centres or less;
  - 1/4" float glass was ordinary;
  - 1" aluminium mini-blinds held 70–80 % of the US market in 1981. Sears sold Levolor 1-inch blinds in 1993.
- **The blind is raised only,** Y 2.006–2.195, above the head line. It is never lowered on a breakable map window (`10` §5.1).
- **Family split** (`06` §3.0): wood = the building's own rooms, like the free door; steel = the fit-out, like the key door.
- **Budgets** (`10` §9.4), LOD0 / 1 / 2: wood 2,600 / 1,000 / 200; steel 3,600 / 1,300 / 220; aluminium 2,800 / 1,000 / 200; raised blind 2,400 / 800 / 120.

### DW09 · THE GLASS SITS IN A POCKET (10 / 13)

- **Lede:** "All render-only. The stops need a 16 mm band inside the 1.4 × 1.65 m opening; no collider moves."
- **Statement:** "Real stops need 16 mm inside the opening: one OK from the map chat."

The S1 band breaks `00`'s binding rule ("once the pane is gone, they must stay outside the 1.4 × (0.35–2.0) opening") until the map chat agrees (`10` §6.6 item 2). So the jamb diagram now shows S0 too: "If the map says no" (flat reveal, dark 12 mm line, the glass edge shows at an angle).

| Slot | x, y, w × h | Content | Chip |
|---|---|---|---|
| `diagram:dw09_elevation` | 72, 232, 576 × 657 | §4 D3 | — |
| `diagram:dw09_jamb_section` | 672, 232, 576 × 657 | §4 D4 | — |
| `img:Kit_WindowFrame_Steel_close` | 1272, 232, 576 × 316 | Phase 2: a corner at 0.3 m with the stop screws, glazing tape and 6 mm stand-in glass (G4 self-check render f) | "OFFICE · SCREWED STOP · 0.3 M · WIP" (phase 1: two jamb screws at 0.3 m) |
| `img:Kit_WindowFrame_Wood_close` | 1272, 572, 576 × 317 | Phase 2: the stool horn and apron at 0.3 m | "LOBBY · STOOL + APRON" |

**Claims and sources:**
- **Opening and colliders.** The opening is 1.4 wide, sill 0.35, head 2.0. The pane collider is 1.4 × 1.65 × 0.03 and is kept. Glazing and frame are render-only (`00` Windows; `10` §6.7).
- **The S1 stop band** (`10` §5.2; `06` §4.1). The wall cut *is* the opening, so real stops must stand 16 mm into it.
  - Exposed glass is 1.367 × 1.617 (the sight line at X ±0.6835, Y 0.3665–1.9835).
  - This needs the map chat's rule clarification (`10` §6.6 item 2).
  - Fallback S0 is a flush reveal with a dark gasket line, and loses the structure.
- **Glass.** A visible 6 mm slab, 1.391 × 1.642, centred (0, 1.175, 0). Its edge sits 12 mm (jambs) or 12.5 mm (head and sill) behind the stop, on 3.5 mm setting blocks. 3 mm of tape sits between glass and stop (`10` §5.4).
- **Teeth.** The tooth band (jambs 0.600–0.6835, head 1.900–1.9835, sill 0.3665–0.390) applies only if the map chat agrees. Otherwise teeth are clipped at the stop line (`10` §5.4).
- **Climb.** It starts within 0.95 m, lasts 0.6 s, lifts 0.35 and ducks 0.55. Nothing visible sits inside the clear zone (`00`; `10` §10.4 check a).
- **Out of this kit** (Red's decision vi): the pane, cracks, shards, teeth and floor glass belong to the glass-destruction track. This kit gives only the pocket and the interface (`10` §5.3–5.4).
  - The RT glass target moves from the disabled pane cube to the visible slab (`10` §7.4, task G14-K5).

### DW10 · WHAT CHANGES IN THE GAME (11 / 13)

- **Lede:** "Colliders and names stay; every model is render-only."
- **Statement:** "Play changes three ways: doors open one way, some doors lock, keys hang on hooks."

| Element | x, y, w × h | Content |
|---|---|---|
| `diagram:dw10_door_plan` | 72, 232, 876 × 657 | §4 D5 |
| Rule block MAP | 972, 232, 876 wide | Head "MAP CHAT". Body: "Doors open one way: a step-back, a pull event, the Relay breaks that way, hinge +29.5 mm. A lock flag, the lock point at the keyhole, keys on hosts." |
| Rule block VISUAL | 972, 388 | Head "VISUAL CHAT". Body: "All 38 models built in a test copy, none in the game. No colliders, no lights. LOD1 and LOD2 wait for the LOD change." |
| Rule block SOUND | 972, 544 | Head "SOUND CHAT". Body: "Wood and steel door sounds, a knob turn, the deadbolt at 0.86 s, a pull. Window frames say wood or steel for the climb." |
| Budget numerals | x 972 / 1272 / 1572 (276 wide), y 724 | Bayon 88/80 numeral, then a Bayon 20 label 8 px below. **"41K"** / "TRIS · NEAREST LOCKED DOOR"; **"0.7K"** / "TRIS PAST 12 M / AFTER THE LOD CHANGE" (two lines); **"0"** / "COLLIDERS OR LIGHTS ADDED" |

The first build's lede ("names and events stay as they are") and statement ("one thing changes play") were false against `10` §6.5: a new pull event, `LockPoint` at the keyhole (so `DoorUnlocked`'s point moves), new knob constants, a per-door lock flag, keys on hosts with the key cell's lamp held Steady, and the Relay breaking toward the swing side. The 0.7K figure needs P-1, which is now DW11 call 8.

Rule blocks use head Bayon 88/80 + body 24/26, 2 lines, 156 px apart; they have no label line.

**Claims and sources:**
- **Map-side changes, in build order** (`10` §6.5):
  1. Option A: a fixed swing side, pulls, the Relay breaking toward the swing side, and `S_sign`;
  2. the A2 pivot;
  3. dress doors, and disable (not destroy) the `Door leaf` renderer;
  4. the per-door lock flag (W5) and the member level;
  5. `LockPoint` moves to the keyhole (±0.0315);
  6. jolts go on the `Leaf rig`;
  7. the window root, the frame spawned before the `brokenWindows` return, and the visible slab;
  8. the key as an unscaled empty, its host, the procedural fallback, and the key cell's lamp held Steady;
  9. optional: drop the trims;
  10. the Level Designer palette skips `interactable`;
  11. the knob constants for the shots.
  - **Size:** the map chat's own estimate is "about an evening plus the takeover" for Option A (`00`).
- **Rule clarifications** (`10` §6.6): door stops inside the opening, the window stop band and tooth band, and the proud limits (locksets ≤ 0.065; closer arms up to 0.20 above 2.06 m; exit device 0.095).
- **Visual chat** (`10` §6.1, §6.3, §8):
  - the facade `FrontRoomsInteractableKit.DressDoor` / `DressWindow` / `DressKey`;
  - the render-only components `FrontRoomsDoorRig`, `FrontRoomsDoorCloserLinkage` and `FrontRoomsKeyAssembly`;
  - pipeline items that NEED APPROVAL: P-1 (LOD2 and switch distances), P-2 (weighted normals), P-3 (vertex-colour wear), P-4 (new surfaces `Door_Enamel`, `Prop_SignEngraved`, `Prop_KeyTagNo`, `Run_ExitSign_Dead`).
  - **Asset count** (`10` §9): 38 assets. The 6 for Run and Exit are `Kit_DoorLeaf_Ward`, `Kit_DoorLeaf_SteelLite`, `Kit_ExitDevice_Crossbar`, `Kit_ExitSign`, `Kit_WindowFrame_Alu` and `Kit_MiniBlind_Lowered`, so 32 come first. All 38 are built in the private clone `proj_int` (as of 17:10 the key board's FBX there was overwritten by the computer keyboard restore: the name clash in `02_figma.md` §9).
- **Sound chat** (`10` §12): `doorType` and `frame` tags, `Mechanism/Lock/KnobTurn` (NEW), `BoltRetract` kept at 0.86, a pull event, no `Unlatch` on the latchless Run ward doors.
- **平面视觉** (not on the slide, in phase 2): the `Prop_KeyTagNo` and `Prop_SignEngraved` art, and the R10 three-views.
- **Budgets** (`10` §9.2):
  - The heaviest door (OF-K, within 1.5 m) is about 41k tris and about 21 renderers. Beyond 12 m: 3 renderers, about 0.7k tris, but only once P-1 lands.
  - Until then, small parts ship without a LODGroup. The importer culls at 3 % of screen height, which is about 1.4 m for a knob (`10` §1.8).
  - Desktop is the reference. WebGL tiers are the WebGL track's call (the memory rule: never lower desktop for WebGL).
- **What must not change** (`10` §6.7):
  - the leaf collider (0.05 × 2.08 × 0.98) and its closed pose;
  - the pane collider;
  - `doorByCollider` and `windowByCollider`;
  - the names `Door hinge*`, `Door leaf`, `Window pane {a}-{b}`, `Key · zone {id}`;
  - no hazard names, no colliders and no Light components on kit parts;
  - event points at floor level.

### DW11 · YOUR CALLS (12 / 13)

- **Lede:** "Each call has a default. Confirm or change it. Phase 2 builds what you pick."
- **Statement:** "Phase 2: rendered models in every slot, in-game tests, three-views."

Eight blocks, in 2 columns (x 72 and 972, 876 wide) × 4 rows (y 232 / 404 / 576 / 748).
- Each block is a head (Bayon 88/80, h 80), then a 2-line body (24/26, h 52) 88 px below the head's top. The block is 140 tall, with 32 between rows.
- There is no separate label line: four rows leave no room for HR03's label. The number goes in the head instead, e.g. "1 · WHICH DOORS LOCK". The longest head, "2 · WHICH WAY THEY OPEN", is about 840 px wide.
- Order: column 1 holds 1–4, column 2 holds 5–8.

| # | Head (after "N · ") | Body (default first) | Source |
|---|---|---|---|
| 01 | WHICH DOORS LOCK | "Default: about 1 door in 3, fixed per seed. Or: only doors leading deeper, or only doors into Office." | `00` (`lockedDoorShare` ≈ 0.35); `05` §8 item 1 |
| 02 | WHICH WAY THEY OPEN | "Default: into the Standard room (the bigger one), so escapes into Low rooms are pulls. Or: a hash per door." | `04` §6.3 item 1; `10` §6.5 item 1. The first wording ("taller room, where there is space to swing") mixed up ceiling height and floor space |
| 03 | PULLING A DOOR | "Default: step back 0.45 m (60° if blocked). Cost: escaping from the swing side is slower. Or: a push-only rule." | `04` §6.3 item 2 and its "Gameplay consequences". The 60° open is the fallback inside the step-back, not an alternative |
| 04 | THE LOCK | "Default: deadbolt + knob, the sliding bolt you asked for. Or: a storeroom knob with the key in the knob." | `10` §12 item 6, §0 D6 |
| 05 | OPENED, THEN SHUT | "Default: yes, it still looks locked but opens on E. Or: an unlocked door never shuts fully; it rests ajar at 10°." | `05` §8 item 2; `10` §12 item 5 |
| 06 | LOBBY GLASS | "Default: clear, so you see the hall and the Relay through it. Or: hammered glass in some Lobby windows hides them." | `06` §3.1, §6 Red ask 3; `10` §12 item 4. It changes what the player sees through windows, so it is a call, not a quiet default |
| 07 | OFFICE BLINDS | "Default: raised 1-inch blinds on some Office windows, one bent slat each. Or: all slats straight, or no blinds." | `06` §3.2, §6; `10` §5.1, §12 item 3 |
| 08 | THE LOD CHANGE | "Default: approve it, so every model ships LOD0, 1 and 2. Or: small parts ship with no LODs and cost draw calls." | `10` §8 P-1, D13, §9.2. Red's highest-spec LOD0/1/2 order depends on it |

**The smaller defaults and the two Run/Exit calls moved to DW12** (below). The first build kept them as speaker notes, which Figma frames do not have, so Red could not see or veto them.

**What phase 2 adds** (for the statement and the notes):
1. **Pre-renders** for every `img:` slot, from the built kit (§6).
2. **In-engine tests** in a private clone (`10` §11), whose frames replace the prototype and mock-up tiles on DW02, DW04 and DW06:
   - T1, gap rays on real meshes;
   - T2, swing clip;
   - T5, the head dip with real parts;
   - T6, readability with the real kit, plus Run and Exit.
3. **Three-views** of the new kit assets, handed to 平面视觉 for the PROP KIT · THREE-VIEW + ERA section (2324:852) in its K-slide format, with `_front` / `_side` / `_top` slots (`10` §10.5 item 4; R10).
4. **The approved media batch** (`media_candidates.md`) dropped into the `ref:` slots.
5. **Era table rows** in `22_era_lock.md` (`05` §4).

### DW12 · LATER, AND ALREADY SET (13 / 13; new in the fix pass)

- **Lede:** "Two calls can wait for Run and Exit. The rest are set unless you say so."
- **Statement:** "None of this blocks phase 2; any line can still change."

Left column (x 72, 876 wide): two call blocks as on DW11 (heads at y 252 / 416), then two slots of 426 × 317 at y 572.

| # | Head (after "N · ") | Body | Source |
|---|---|---|---|
| 09 | A PEEK WINDOW | "Default: no glass in locked doors. Or: a wired-glass slot in Run doors; you see the Relay, it can’t see you." | `05` §6.4; `10` §12 item 2 |
| 10 | EXIT LETTERS | "Default: red. Or: green, to suit the cyan Exit." | `10` §2.4, §12 item 1 |

| Slot | x, y, w × h | Source | Chip |
|---|---|---|---|
| `media:dw12_peek_lite` | 72, 572, 426 × 317 | `IMG/05_r3_close_Office_recommended_lite.jpg` (600, 80, 1300, 601) | "PEEK WINDOW · OFFICE LIGHT · MOCK-UP" |
| `img:Kit_ExitSign_persp` | 522, 572, 426 × 317 | phase 1: `WIP/G3/G3_exit_sign_close.png` (140, 0, 1484, 1000); phase 2: `Kit_ExitSign` lit and `_Dead` on a wall | "EXIT · RED LETTERS · WIP" |

Right column (x 972): head "ALSO SET" (Bayon 88/80, y 252), then seven one-line rows (Serif 24/26, 44 px apart from y 340):
1. "Run windows: plain glass. Wired glass would need a two-stage break." (`06` §3.3, §6 Red ask 4)
2. "Exit windows break like the rest. A different break could be a tell." (`06` §3.4, §6 Red ask 5)
3. "Free doors get a lever, though the film’s door at 1:27 has a knob." (the DW03 honesty note; value still carries the read past 6 m)
4. "Locked doors say EMPLOYEES ONLY on both faces; STAFF ONLY in Run." (`05` §6.2; `10` §2.4)
5. "Item glint option: off. Keys catch the lamp; they never glow." (`03` P5)
6. "Ray-traced glass aims at the visible pane, never the hidden box." (`10` §7.4; task G14-K5. Without it every window drops out of the trace)
7. "Bronze frames would reflect white in ray tracing until that is fixed." (`10` §7.3 item 3, a code read, UNVERIFIED; task G14-K3)

The bent-slat fallback is inside DW11 call 7 ("all slats straight"). The steel-door honesty note is on DW03's statement.

---

## 4. Diagram specs (support only: drawn in Figma, black 2 px lines, Bayon 20 labels, one yellow accent = light / sight path)

All numbers are metres in the door or window root frame of `10` §1.2 / §5.1 unless marked mm.

**D1 `diagram:dw02_jamb_section`** (576 × 657): a plan section at the hinge jamb, in two states stacked.
- **Top, "TODAY"** (y 0–316), at 2.5 px/mm:
  - wall end 160 mm thick (X ±0.080);
  - map trim (render-only, grey) 70 mm face × 200 mm deep (X ±0.100);
  - leaf 50 mm thick (X ±0.025), starting 10 mm from the wall end.
  - A yellow straight line runs through the 10 mm slit from room to room.
  - Labels: "10 MM SLIT", "NOTHING BEHIND IT".
- **Bottom, "OPTION A"** (y 341–657):
  - 2 mm lining;
  - casing on both faces (75.5 mm face, 25 mm proud);
  - stop on the push face, 16 mm proud × 35.5 mm (X −0.0605 → −0.025, Z 0.002 → 0.018);
  - leaf 44 mm (X ±0.022) with a 3 mm gap to the lining (Z 0.002 → 0.005) and a 3 mm silencer gap to the stop;
  - hinge knuckle Ø 15 mm on the swing face at (X +0.0295, Z +0.0098).
  - The yellow path is L-shaped and ends at the stop.
  - Labels: "3 MM GAP", "16 MM STOP", "44 MM LEAF", "KNUCKLE", "SWING SIDE", "PUSH SIDE".
- **Bottom line** (Bayon 20): "HEAD: SAME STOP · FLOOR: 3 MM OVER A 12 MM SADDLE · SWING CLEARS ≥ 2.8 MM, 0–95°".
- Source: `10` §1.4–1.5; `04` §1.2.

**D2 `diagram:dw06_unlock_timeline`** (576 × 444): time runs left to right, 0–1.55 s, about 330 px/s, with a tick every 0.1 s. Six rows, labelled in Bayon 20 on the left:

| Row | Beats |
|---|---|
| CAMERA | 0.00–0.45 travel to pose P (FOV −14°); 1.15–1.55 return |
| KEY | 0.30–0.55 approach; 0.55–0.68 in 25 mm (pin catch at 0.60); 0.68–0.90 turn 90° (first 10° slow); 0.90–1.15 out |
| PLUG | 0.68–0.90, 0 → 90°; back with the key |
| DEADBOLT | 0.86, retracts 25 mm in 0.08 s |
| KNOB + LATCH | 0.92–1.00 turn 40°, latch 19 mm; 1.20–1.35 return. Marked "NEW" (proposed constants) |
| LEAF | 0.86 shift 1.5 mm; 1.00–1.25 ajar 10° + 1° overshoot |

- A vertical yellow line at 0.86 is labelled "COMMIT · DOOR UNLOCKED".
- Footnote label: "PULL SIDE: AJAR COMES TOWARD THE CAMERA; START THE RETURN AT 1.00 OR USE 5°".
- Source: `FrontRoomsShotTimings.proposal.cs.txt` `Unlock`; `10` §3.4.

**D3 `diagram:dw09_elevation`** (576 × 657): the W-OF front elevation (face A, office side) at 330 px/m, floor at the bottom.
- the opening 1.40 × 1.65 (sill 0.35, head 2.00), outlined in grey and labelled "OPENING + COLLIDER · UNCHANGED";
- the face band to X ±0.775, the head band to 2.075, the sill band 0.2745–0.3505;
- the stop line at X ±0.6835, Y 0.3665–1.9835, as a black line; the glass inside it in light grey, labelled "EXPOSED GLASS 1.367 × 1.617";
- the 16 mm band between the opening and the stop line, labelled "16 MM STOP BAND · MAP OK NEEDED";
- the tooth band as dashed yellow (jambs to 0.600, head to 1.900, sill to 0.390), labelled "TEETH · ONLY IF THE MAP AGREES";
- 30 screw dots on face A (8 per jamb, 7 on head and sill);
- the raised blind as a block at Y 2.006–2.195, labelled "BLIND · ALWAYS ABOVE 2.0";
- a climb arrow over the sill, labelled "CLIMB · SILL 0.35".
- Source: `10` §5.2, §5.4, §5.1; `06` §3.2, §4.4.

**D4 `diagram:dw09_jamb_section`** (576 × 657): a plan section through one jamb of W-OF at 2.4 px/mm, with face B (hall) on the left and face A (office) on the right.
- wall ±80 mm;
- the map trim as a hidden grey ghost at ±100 mm, labelled "MAP TRIM · HIDDEN";
- the steel sleeve: face band 0.6995 → 0.775 at Z ±0.105, return to the wall, soffit at X 0.6995;
- the integral stop on face B (Z −0.022 → −0.006) and the channel stop on face A (Z 0.006 → 0.022), with one oval-head screw;
- glass 6 mm (Z ±0.003), with its edge at X 0.6955 and the stop line at 0.6835, labelled "12 MM BITE";
- 3 mm tape lines;
- the pane collider as a dashed ghost 30 mm box to X 0.700, labelled "COLLIDER · STAYS".
- **Inset at the bottom** (W-L0 stool): stool 25 mm thick to Z ±0.121 with half-round nosing, horn to X 0.800, apron below.
- Source: `10` §5.1–5.2, §5.4; `06` §3.1–3.2, §4.2.

**D5 `diagram:dw10_door_plan`** (876 × 657): a plan of one map door at 200 px/m.
- **Low room (2.4 m)** on the left, labelled "PUSH SIDE · STOPS".
- **Standard room (2.9 m)** on the right, labelled "SWING SIDE · DOOR OPENS HERE".
- **The leaf:** shut (black) and at 95° (grey dashed), with its 1.0 m sweep arc and the 1.2 m keep-clear strip.
- **A player dot** inside the arc on the swing side, with an arrow back 0.45 m, labelled "PULL · STEP BACK 0.45 M IN ≤ 0.35 S".
- **A Relay dot** on the push side, with an arrow into the Standard room, labelled "RELAY BREAKS IT INTO THE FAR ROOM".
- **Hinge inset** (top right, 10:1): the old pivot on the centre line, the new pivot 29.5 mm toward the swing side and 9.8 mm into the opening, and the collider box drawn in the same place before and after. Labelled "HINGE MOVES · COLLIDER STAYS".
- Source: `04` §6.3; `10` §1.3, §6.2, §6.5; `00` Door slits (Option A cost).

---

## 5. Sources by slide (the ledger the notes point to)

| Slide | Research | Media ledger |
|---|---|---|
| DW00 | `04` §1.1–1.4; `01` §1.4, §2.2, §3.2–3.3; audit F1, F3, F10, F14 | `PM/SOURCES.md` (in-engine); audit `images/frames.txt` |
| DW01 | `04` §2.1–2.3; `10` §1.1, §1.4, §2.1 | `media_candidates.md` C1–C3 |
| DW02 | `10` §0, §1.3–1.5, §1.7, §11; `04` §1.5, §5.4, §6.2 | `PM/SOURCES.md` |
| DW03 | `05` §1, §3, §4; `02` §3, §11; Figma IR01/04/05/06, HR03, K00, R44 (all read by `05`); `22_era_lock.md` | `PM/SOURCES.md`; `W2/kit-references/SOURCES.md`; `W2/ip-research/SOURCES.md` |
| DW04 | `05` §2, §5; `03` §1.1, §1.7 | `IMG/05_r*_metrics.json`; `harness/05_*` |
| DW05 | `10` §2.1–2.4, §9.1; `05` §6 | — (phase 2 renders) |
| DW06 | `10` §0 D6, §3.1–3.4; audit §3.2; `FrontRoomsShotTimings.proposal.cs.txt`; `05` §7; `03` §3.3; `02` §4 | `IMG/05_r3_headdip_strip_L0lit.jpg` (`05` §7) |
| DW07 | `10` §3.3, §4; `03` §1.1–1.5, §2.2–2.5; `02` §8–9 | `media_candidates.md` C8 |
| DW08 | `06` §0–§3; `10` §5.1, §9.4 | `PM/SOURCES.md`; `media_candidates.md` C10–C11 |
| DW09 | `10` §5.2–5.4, §6.6–6.7, §7.4; `06` §4; `00` Windows | — |
| DW10 | `10` §6, §8, §9, §12; `00`; `04` §6.3; `05` §8 | — |
| DW11 | `10` §12; `05` §8; `06` §6; `04` §6.3; `00` | — |

---

## 6. Slot map (phase 2 fills these; phase 1 shows placeholders)

**Render rules for phase 2:**
- `_persp`: a 3/4 view from the swing (S) face at 1.62 m eye height, framed so the subject fills about 70 % of the slot height. Use the same light warm-grey studio ground as the PROP KIT section so the decks match. slot aspect for every slot (door sets are 426 × 316 / 317 since the fix pass).
- `_front`: an orthographic front view for the three-views.
- `_close`: 0.3 m hero detail.

Door sets are built from the module FBXs with `10` §6.2's offsets, for the S face, in Blender (Cycles) or in the clone's look-dev room. Never in the real project.

| Slot | Slide | Size | Contents | Priority |
|---|---|---|---|---|
| `img:DoorSet_L0-F_persp` | DW05 | 426 × 316 | `10` §2.1 L0-F row | P1 |
| `img:DoorSet_OF-F_persp` | DW05 | 426 × 316 | OF-F row | P1 |
| `img:DoorSet_L0-K_persp` | DW05 | 426 × 317 | L0-K row | P1 |
| `img:DoorSet_OF-K_persp` | DW05 | 426 × 317 | OF-K row | P1 |
| `img:DoorSet_RN-F_persp` | DW05 | 426 × 316 | RN-F row | P2 |
| `img:DoorSet_RN-K_persp` | DW05 | 426 × 317 | RN-K row | P2 |
| `img:DoorSet_EX-F_persp` | DW05 | 426 × 316 | EX-F row | P2 |
| `img:DoorSet_EX-K_persp` | DW05 | 426 × 317 | EX-K row | P2 |
| `img:Kit_Lock_Escutcheon_persp` | DW06 | 276 × 204 | the asset alone | P1 |
| `img:Kit_Lock_CylinderShell_persp` | DW06 | 276 × 204 | the asset; IC face toward the camera | P1 |
| `img:Kit_Lock_Plug_persp` | DW06 | 276 × 204 | the asset; the keyway cavity visible | P1 |
| `img:Kit_Lock_Knob_persp` | DW06 | 276 × 204 | the asset; knurl visible | P1 |
| `img:Kit_Lock_Deadbolt_persp` | DW06 | 276 × 204 | the asset, thrown | P1 |
| `img:Kit_Lock_Latchbolt_Mortise_persp` | DW06 | 276 × 204 | the asset | P1 |
| `img:Kit_Lock_StrikeMortise_persp` | DW06 | 276 × 204 | the asset | P1 |
| `img:LockSet_Mortise_persp` | DW06 | 276 × 204 | escutcheon + shell + plug + knob on a `Kit_DoorLeaf_Steel` patch, key in, seen from pose P | P1 |
| `img:KeySet_Rack_persp` | DW07 | 426 × 316 | `Kit_KeyRack` + ring + key + rect tag on `hook_6` | P1 |
| `img:KeySet_Cabinet_persp` | DW07 | 426 × 316 | cabinet + hung key on `hook_r1_c4` | P1 |
| `img:KeySet_Hook_persp` | DW07 | 426 × 316 | `Kit_KeyHook` + ring + key + red tag | P1 |
| `img:KeySet_Desk_persp` | DW07 | 426 × 316 | key flat on a desk top, tag over the edge (an existing desk kit asset) | P1 |
| `img:KeyParts_Lineup_front` | DW07 | 426 × 317 | key, ring, 3 tag shapes × 3 colours, number plate; one number on all | P1 |
| `img:Kit_WindowFrame_Wood_persp` | DW08 | 426 × 316 | the frame with a 6 mm stand-in slab | P1 |
| `img:Kit_WindowFrame_Steel_persp` | DW08 | 426 × 316 | the frame, face A, stand-in slab | P1 |
| `img:Kit_WindowFrame_Steel_Enamel_persp` | DW08 | 426 × 316 | the variant | P2 |
| `img:Kit_WindowFrame_Alu_persp` | DW08 | 426 × 316 | the frame | P2 |
| `img:Kit_MiniBlind_Raised_persp` | DW08 | 426 × 317 | the blind over a steel frame | P1 (option) |
| `img:Kit_WindowFrame_Steel_close` | DW09 | 576 × 316 | a corner at 0.3 m: screws, tape, glass edge | P1 |
| `img:Kit_WindowFrame_Wood_close` | DW09 | 576 × 317 | stool horn + apron at 0.3 m | P1 |
| `img:Kit_ExitSign_persp` | DW12 | 426 × 317 | `Kit_ExitSign` (lit) and `Kit_ExitSign_Dead` on a wall | P2 |
| `ref:re8_steam_castle_hall` | DW01 | 576 × 316 | C1 | needs OK |
| `ref:re8_walkthrough_1-36-39` | DW01 | 576 × 317 | C2 | needs OK |
| `ref:henryford_tivoli_motel_key` | DW07 | 426 × 317 | C8 | needs OK |
| `ref:sears1993_levolor_miniblinds` | DW08 | 426 × 317 | C10 | needs OK |
| `ref:us4463535_glass_stop` | DW08 | 426 × 317 | C11 | needs OK |

The `_front` renders for the three-views (every P1 asset in `10` §9) go to 平面视觉's section, not to this one.

**Evidence that phase 2 may replace** (keep the slot name and swap the image):
- `media:dw02_option_a` → a T1 frame of the real A2 door in the same camera;
- `media:dw04_*` → T6 frames with the real kit;
- `media:dw06_dip_*` → T5 frames with the real lock.

---

## 7. Before handing over (Red's four passes)

1. **Say it once.**
   - Chips do not repeat the lede.
   - No slide has both a chip and a label for the same fact.
   - DW03 carries the film frames, and they appear nowhere else.
   - The "today" frames appear only on DW00, except the matched today/A pair on DW02.
2. **Enlarge.** Every slot fills its grid cell, and the meaning sits in 88 Bayon, 50 Serif or the media.
3. **Current design.** Option A, A2 hinges, the mortise lock with deadbolt and knob, and the S1 stop band are proposals.
   - Nothing says "both ways" except DW00 (today).
   - Option B is mentioned nowhere on slides; it is retired (`10` §1.1).
4. **Plain words.** No file names, code names or test IDs on slides. The one exception is the asset slot names inside phase-1 placeholders.
5. **Last check.** List the page's sections by bounds before placing, so nothing overlaps.

---

## 8. Open points the builder should not paper over

- **After the fix pass, 14 `img:` slots hold WIP previews** in the measured colours (`02_figma.md` §5.2) and 15 are placeholders. All 5 `ref:` slots wait for Red's OK, and DW01 is hidden until C1 and C2 arrive. Do not fill slots with lookalike images or AI art.
- **The shadow pair (DW02) shows the retired B prototype.** The point is the leaf's shadow, which is the same on A.
- **The head-dip strip (DW06) shows the older knob-keyway mock-up.** Its first chip says "OLD MOCK-UP · KEY IN KNOB", and DW06 now also shows the proposed lock (WIP).
- **RE8's single swing and true stops are UNVERIFIED by a text source** (`04` §2.3). DW01 claims only what V1 and V2 show.
- **Run and Exit readability is UNVERIFIED until T6** (`10` §2.2). Their cells stay marked P2.
