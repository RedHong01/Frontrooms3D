# 31 · Security desk: the Figma proposal section (游戏视觉, 2026-10-22)

Workflow `station-look`, stage 3 (Figma proposal), run on account B after account switch #2. Built with use_figma in the stairwell proposal's grammar (`research/stairwell/30_figma.md`, SW00–SW09).
- Only this section was written in Figma. No other section was touched, and nothing was added to the VISUAL VERIFICATION LOG (VL354–VL358 were placed by stage 2).
- No Unity was opened, there were no git writes and no downloads. The only repo file written is this one.

File `0tCbAiVUlrPId3RWd9LRif`, page `2099:76` ("Undergoing Game Projects").

| | |
|---|---|
| Section | **FRONTROOMS · SECURITY DESK (STATION) · PROPOSAL** `3532:7362` |
| Where it is | x 25257, y 38680, 8280 × 3880 (bottom at y 42560). |
| How the spot was chosen | All 51 top-level nodes were read first. The column at x 25257 holds ROOM VISUALS, DOORS + WINDOWS, LEVEL TRANSITIONS, PRINT REFERENCE and STAIRWELL `3484:7309`, which ends at y 38280. Nothing lies in x 25257–33537 below that, so the section sits 400 px under the stairwell. After the build, an overlap check against every other top-level node found none. |
| Slides | 10 frames (SD00–SD09), 1920 × 1080, in rows of 4 (4 / 4 / 2), 2040 px apart in x and 1240 in y, as in the stairwell section. |
| Grammar | The header holds meta only (Plex Mono, `FrontRooms · Security Desk`, `Week 6 · Oct 22, 2026`, page `nn / 10`). Titles are Bayon at y 139 on the 12-col / 72 / 24 grid, and content starts at y 232. Ledes are one line at x 1272, y 173. Statements sit at y 966. All text uses the file's P1 styles (`P1/Display/Title`, `P1/Display/Label`, `P1/Text/Body`, `P1/Text/Statement`, `P1/Header/Meta`); there is no Inter. Chips sit on the images: black for content, yellow for status (`Pre-render · blockout kits`) and for our picks (SD09). There are no captions under images and no source lines; sources are in `SOURCES.md` and below. Ink is plain #0A0A0A (rich black is InDesign-only). Nothing reaches outside the 72 px margins. |
| Type detail | Digits and "·" inside Bayon text (titles, labels, chips, KPI values) are set in IBM Plex Mono Medium at the same size, per `SHOT_FORMAT.md` and `INFO_TYPE_SYSTEM.md` (Bayon draws 1 and 0 as I and O and has no "·"). The 12 KPI values that are digits only (SD00 ×4, SD02 ×3, SD03 ×3, SD07 ×2) are therefore wholly Plex Mono, and Figma shows them without an attached style (size 88 / line 80, as the Title style). Prime marks in Bayon use the ASCII apostrophe (`W' rev 3`); body text uses ′. KPI values drop the space before % so mono digits fit the 225 px KPI pitch. |
| Images | 41 `img:<stem>` slots holding 29 distinct stems. Every slot has exactly one IMAGE fill (checked after the build). |
| Text | 408 text nodes: Bayon, Source Serif 4 and IBM Plex Mono only. |

## Slides

| # | Frame id | Name | What it shows |
|---|---|---|---|
| 01 | `3532:7363` | SD00 · The security desk | Cover. W′ rev 3 reveal (hero), D′ rev 3 reveal, the key lit by the set. What it does (tutorial T1 = A), Red's ask, who does what, what Red decides. KPIs: 8 plans built, 6 checks, 2 modules, 21 new kits |
| 02 | `3532:7377` | SD01 · A guard's post, 1990 | Led by the four research photos on disk (fire panel, watchclock station, 1985 pocket TV, a modern annunciator marked "not 1990"), each with a credit chip. The post, why it is empty, his own TV, what we build from it (ledge 1.07 m, work top 0.74 m, no low section, putty panel wired from below, one key on its own board) |
| 03 | `3532:7385` | SD02 · A · The Post | D′ rev 3: reveal (hero), junction, take before / after, the set alone, leaving by E2. Story / Look / Palette / Light / Plan / Cost. KPIs: 87K hero tris, set gives the key 82%, take drop 82%, 2 days after B |
| 04 | `3532:7393` | SD03 · B · After-hours reception | W′ rev 3: reveal (hero), 1.5 m out through E1, take before / after, the set alone, the staff strip from E2. Same six blocks. KPIs: 94K, 86%, 86%, 11 hero days |
| 05 | `3532:7401` | SD04 · C · The night posts | The same take cluster in Level 0, Office and the long hall. Floor 1 reception, B1 guard post, B2 floor warden, B3 receiving desk; why later, cost, risk; the watchclock station photo as the later trail |
| 06 | `3532:7409` | SD05 · Three plans | W′ rev 3, D′ rev 3, H: the view 1.5 m out through E1 above a vector plan of each, drawn from the blockout geometry at 22 px/m (cells, walls with E1 / E2, lamp modes, the set and its 100° / 6 m inner cone, key, panel with its green lamp, key cabinet, furniture, the E1 sight line, a 3 m bar). Legend under W′ |
| 07 | `3532:7417` | SD06 · One moving light | The three calls, each as one statement: put E1 on the set's axis (no spill); key on the side wall, 38° off; only the green lamp is lit. Evidence pairs: set alone / tutorial lamps; junction 23° off vs 1° off (2× crops); corner set 68°, 45 % vs wall bracket 38°, 86 %. The set's light spec; lamps and the V3 luminance fix |
| 08 | `3532:7425` | SD07 · The kit | Take cluster (stand-ins), the real panel, counter and turned chair, tandem rows. New grammar kits with budgets, new story kits, existing kits, variants and later. KPIs: 2 blockout days, 12 hero days, B 94K, A 87K, 100K hero-room cap |
| 09 | `3532:7433` | SD08 · Six checks | 8 plans × checks 1–6 (§8 of `20_layout.md`, bars of `30_look.md` §9.1): verdict word (PASS quiet grey, FAIL / FLAG ink) plus the number. Our three plans as black row chips. W′ rev 3 take before / after; the literal G5, FLAG, staff strip, how it was built |
| 10 | `3532:7441` | SD09 · Your calls | Why B and A; the B / A / H strip with yellow "Our pick" on B and A; no decision needed; next. Four calls, each with the options as the player feels them, cost, recommendation and what it blocks: (1) which room on which floors, (2) the screen through the doorway (E1 within 12°), (3) what you see at the take, (4) build order |

## Slot → file

`W` = `/Users/redwang/FrontRoomsVisualWork`. Render slots are label-free JPEGs (1440 × 810, q87) made from the r6 raw renders in `W/station_work/runs/r6/<plan>/<shot>.png` (eye 1.62 m, vFOV 76, the game's post, card 2 of the ad where a card applies), stored in `W/station/figma/` (outside the repo). `_x2` files are 2× centre crops (960 × 540 of the 1920 × 1080 frame) of the junction view, and their chips say so. Photo slots are metadata-free board copies, long edge 1600, from `W/station/board_stills/` (`NOTE_1022_photos.md`); they stay out of the repo, the game and marketing. Every file was uploaded once (upload_assets, raw JPEG POST, HTTP 200 with an imageHash), then its hash was copied to the duplicate slots.

| Stem | Source | imageHash | Slots |
|---|---|---|---|
| `st_Wp3_reveal` | r6 `Wp3/reveal.png` | `62fd9043…` | `3533:7362` (SD00), `3535:7376` (SD03), `3539:7503` (SD09) |
| `st_Dp3_reveal` | r6 `Dp3/reveal.png` | `8782ad99…` | `3533:7373` (SD00), `3533:7424` (SD02), `3539:7508` (SD09) |
| `st_H_reveal` | r6 `H/reveal.png` | `167671aa…` | `3539:7513` (SD09) |
| `st_Wp3_take_wide_before` | r6 `Wp3/take_wide_before.png` | `11d7491d…` | `3533:7376` (SD00), `3535:7384` (SD03), `3535:7416` (SD04), `3538:7391` (SD06) |
| `st_Wp3_take_wide_after` | r6 `Wp3/take_wide_after.png` | `8eb2d42f…` | `3535:7387` (SD03) |
| `st_Dp3_take_wide_before` | r6 `Dp3/take_wide_before.png` | `21cb958b…` | `3533:7432` (SD02), `3535:7421` (SD04) |
| `st_Dp3_take_wide_after` | r6 `Dp3/take_wide_after.png` | `d9e46a63…` | `3533:7435` (SD02) |
| `st_H_take_wide_before` | r6 `H/take_wide_before.png` | `e1c37c59…` | `3535:7424` (SD04) |
| `st_Wp3_take_before` | r6 `Wp3/take_before.png` | `d5ef2a4f…` | `3538:7409` (SD07), `3539:7482` (SD08) |
| `st_Wp3_take_after` | r6 `Wp3/take_after.png` | `d82761e0…` | `3539:7487` (SD08) |
| `st_W_take_wide_before` | r6 `W/take_wide_before.png` (corner set, as drawn) | `88595d42…` | `3538:7386` (SD06) |
| `st_Dp3_approach_far_c2` | r6 `Dp3/approach_far_c2.png` (junction 4.5 m) | `8f74a42b…` | `3533:7429` (SD02) |
| `st_Wp_approach_far_c2_x2` | r6 `Wp/approach_far_c2.png`, 2× crop | `808739af…` | `3538:7376` (SD06) |
| `st_Wp3_approach_far_c2_x2` | r6 `Wp3/approach_far_c2.png`, 2× crop | `dcabc9f9…` | `3538:7381` (SD06) |
| `st_Wp3_approach_near_c2` | r6 `Wp3/approach_near_c2.png` (1.5 m out) | `1bd26c0c…` | `3535:7381` (SD03), `3536:7362` (SD05) |
| `st_Dp3_approach_near_c2` | r6 `Dp3/approach_near_c2.png` | `685ef4d8…` | `3536:7411` (SD05) |
| `st_H_approach_near_c2` | r6 `H/approach_near_c2.png` | `f695a4ca…` | `3536:7458` (SD05) |
| `st_Wp3_beacon_tvonly` | r6 `Wp3/beacon_tvonly.png` (all lamps off) | `234a3f78…` | `3535:7390` (SD03), `3538:7362` (SD06) |
| `st_Dp3_beacon_tvonly` | r6 `Dp3/beacon_tvonly.png` | `e765f617…` | `3533:7438` (SD02) |
| `st_Wp3_beacon_lamps` | r6 `Wp3/beacon_lamps.png` (tutorial V1 lamps) | `42bfc0dc…` | `3538:7373` (SD06) |
| `st_Wp3_wig_e2_0` | r6 `Wp3/wig_e2_0.png` (from E2) | `c34b8174…` | `3535:7393` (SD03) |
| `st_Wp3_exit_e2` | r6 `Wp3/exit_e2.png` | `a79bb2b6…` | `3538:7419` (SD07) |
| `st_Wp3_exit_e1` | r6 `Wp3/exit_e1.png` | `354c47de…` | `3538:7422` (SD07) |
| `st_Dp3_exit_e2` | r6 `Dp3/exit_e2.png` | `53a1edd4…` | `3533:7441` (SD02) |
| `photo_faraday_panel` | `faraday_panel.jpg` (0cf53e16): Faraday Fire Watch IV, Commons, CC BY-SA 3.0 + GFDL | `5afbc6bf…` | `3533:7395` (SD01), `3538:7414` (SD07) |
| `photo_detex_station_chain` | `detex_station_chain.jpg` (33c51673): Paul Sableman, Commons, CC BY 2.0 | `7062bfad…` | `3533:7400` (SD01) |
| `photo_watchman_fd40a_1985` | `watchman_fd40a_1985.jpg` (97cad427): Joe Haupt, Commons, CC BY-SA 2.0 (uploader to recheck before any public use) | `ef7a5b4b…` | `3533:7405` (SD01) |
| `photo_annunciator_2020` | `annunciator_2020.jpg` (76dd5291): Commons, CC0; labelled "not 1990" | `64d76fb7…` | `3533:7410` (SD01) |
| `photo_detex_station_open` | `detex_station_open.jpg` (cc123d3e): Commons, CC BY-SA 3.0 | `430af88c…` | `3535:7441` (SD04) |

Not used: `watchman_fd210.jpg` (same point as the 1985 set) and `office_terminal_1980.jpg` (400 px, licence to recheck).

## Plan diagrams (SD05)

Frames `3537:7362` (W′ rev 3), `3537:7404` (D′ rev 3), `3537:7446` (H), legend `3537:7484`. Drawn as editable vectors at 22 px/m, north up, from the r6 harness geometry in `data/blockout_metrics.json` (`plans.<plan>.harness`: room size, opening centres and widths, screen centre and axis, key position, lamp string) and `data/props_<plan>.tsv` (prop positions; footprints from the kit sidecars in `30_look.md` §1).
- The lavender wedge is the set's inner cone (100°, so full light to 50° off the axis) out to 6 m, the "about 6 m" of V3, clipped to the room. It is a plan schematic: the bracket sets tilt 20° down.
- Lamps: black troffer = Steady, outline = Dim, none = Off (module lamp strings SDDSOO, DSSDDDDOO, SSDDDDOO, rows from the south).
- The dashed line runs from E1's centre to the screen centre (1.3°, 1.8° and 0.1° off E1's axis).

## Numbers on the slides

All from `30_look.md` §1–§4 and §9 and `data/blockout_metrics.json` (run r6); kit counts and budgets from `30_look.md` §1.4–§1.6; facts on SD01 from `01_research.md` §0–§3; posts on SD04 from `10_directions.md` direction C.
- Take drop = the fall of the light on the key board in one change (`c3_board_drop_E`): W′ rev 3 −86 %, D′ rev 3 −82 %, H −83 %, W as drawn −45 %.
- Key lit by set = the set's share of the board's light on the average card (`c2_board_tv_share`): 86 % (W′ rev 3), 82 % (D′ rev 3).
- SD07 hero days: 12.4 rounded to 12; SD03: 10.7 rounded to 11.

## Checks done

- Composition screenshots of all ten slides. One fix round:
  - KPI values crowded in mono → 1–3 characters, no space before %;
  - the KPI row moved to y 892 on SD02 / SD03;
  - SD01 photos raised to 480 px;
  - the plans were redrawn at 22 px/m so H fits inside the bottom margin;
  - the legend was set in two fixed rows;
  - every lede was cut to one line, so it clears content starting at y 232;
  - non-breaking spaces were put between numbers and units.
- Every `img:` slot has one IMAGE fill. No section overlap. Fonts: Bayon, Source Serif 4 and IBM Plex Mono only. Text ink #0A0A0A, white and quiet grey #6E6E6E only.

## Open points carried on the slides (no one was messaged)

- **For 叙事 (layout calls are theirs):**
  - G1's set-to-E1 angle tightened from ±30° to about 12° (W′ rev 3 / D′ rev 3);
  - the staff strip: 2.0 m leaves 1.1 m beside the turned chair, while G6's 1.2 m escape line needs 2.2 m or the chair pushed in.
  - SD09 call 2 says narrative still has to accept the 12° rule.
- **For Red (SD09):** four calls. The joint note `40_for_red_draft.md` is the next stage and goes to 叙事 for review first.

## Not done here

- The two critics (era and look; gameplay and tutorial) and their fixes.
- `40_for_red_draft.md`.
- No Slides (P1 deck) mirror.
- No VL slides: VL354–VL358 are already placed by stage 2.

## r7 update (游戏视觉, fix stage, account A, 2026-10-22 15:1x–15:2x)

The section kept its place, its 10 frames and its grammar. Only slot images, texts and the SD05 plan vectors changed. Nothing outside this section and the VISUAL VERIFICATION LOG was touched.
- **Images:** 24 new uploads (raw JPEG POST, HTTP 200), copied by hash into 34 slots, every slot renamed to its stem; every `img:` slot holds exactly one IMAGE fill (FILL, as before). Label-free r7 slot JPEGs (1440 × 810, q87; `_x2` = 960 × 540 centre crop) in `W/station/figma_r7/` (outside the repo) from `W/station_work/runs/r7b/<plan>/<shot>.png`; hashes in `W/station/figma_r7/hashes.tsv`. Kept from r6 on purpose: `st_Wp_approach_far_c2_x2` (SD06, the 23° comparison) and `st_W_take_wide_before` (SD06, the corner set). The annunciator photo on SD01 is now the board copy with the street address blurred (hash 1d3028ba).
- **Texts changed:**
  - SD00: KPIs 10 plans / 7 checks / 2 modules / 25 new kits; "What you decide" without the Floor 1 placement.
  - SD01: statement "A floor’s night post: a desk someone just left." (叙事 accepted E5); Faraday chip "era unverified" and credit "BrokenSphere · 2008 · CC BY-SA 3.0" (checked on Commons); annunciator credit "address blurred"; the 21 in set explained.
  - SD02 / SD03: D′ rev 4 / W′ rev 4 blocks; B's identity (the floor's reception, now the night guard's post); palettes marked as estimates (E15); "Drop seen" KPIs 81 % / 73 % (displayed board, worst card); key lit by set 83 % / 86 %.
  - SD04: Detex credit "Degen Earthfast · 2014 · CC BY-SA 3.0".
  - SD05: W′ rev 4 / D′ rev 4 labels; the plan vectors redrawn from the r7 harness geometry at 22 px/m (W′: guard chair at the counter's north end, row along the S wall + beam table, cabinet on the strip's E wall, (1,1) Dim lamp, annunciator 1.0 m from the key; D′: E2 in the N wall's west cell, desk and chair moved, two sled chairs + side table, ledge and cabinet moved, the fourth task chair removed).
  - SD06: annunciator words (no glass, round lenses never lit); the set's light (cookie, 60 % to white, luminance kept); "Lamps and walls" (shadows did not stop the light: a light layer).
  - SD07: the real panel chip "Reference · era unverified"; story kits with S37–S40 and the empty heavy hook; white STAIR A fob; cabinet shut.
  - SD08: retitled "Checks after the critics"; the table now has rows W′ rev 3 (r6, before), W′ (1,1) Off, W′ E2 Steady, **W′ rev 4**, W′ 31 seeds, D′ 31 seeds, **D′ rev 4**, **H** and columns 1a junction, 1c exits seen, 2 key lit on the darkest card, 3 drop seen on the worst card, 5 paths ≥ 1.2 m, 7 W1 seen (verdict words PASS / N/A quiet grey, FAIL / FLAG / PARTIAL ink); cells moved to the verdict + 72 px so PARTIAL clears them.
  - SD09: call 1 B for Level 0 floors, A for Office floors, Floor 1's first room waits for its own research; call 2 "Floor 1’s exits · on hold" (Red's 15:20 first-room direction); call 3 "Stop the set’s light at the walls" (the light layer on every tier); "No decision needed" lists our three visual calls and 叙事's acceptances; statement "B for Level 0, A for the offices, C later."
- **Checks:** a composition screenshot of all ten slides; one fix round (SD02 Look / Palette and SD03 Story / Look cut to three lines; SD08 lede to one line); a mistaken overwrite of SD06's title was restored the same minute.
- **Not done:** no reference cards with film time codes were ever on the board (叙事 15:08: drop time codes); `02_refs.md` still carries [memory] times, which is 叙事's file.
- **15:3x, after Red's 15:24 pick (first room = option 1):** SD00 "What you decide", SD04's Floor 1 post ("front desk"), SD09's "Why B and A", the B chip ("Level 0 floors"), calls 1, 2 ("Floor 1’s exits · decided") and 4 (take cluster first) and the statement ("Floor 1 the front desk; B, A deeper; C later.") were rewritten to match `40_for_red_draft.md`. One screenshot of SD09 after the edit.
