# Stairwell (C22): the Figma section

Workflow `c22-stairwell-look`. Stage 3 built it (2026-10-22 morning); **stage 5 (Fix) revised it the same day** (12:50–13:20) after the critics (`50_critic_fixes.md`). **The media stage rebuilt SW01 (2026-10-22, 14:25–14:40)** after Red approved the priority 1 downloads at 13:00 (see "Media stage" below). Written by 游戏视觉 with use_figma. Only this section was written; nothing was placed in the VISUAL VERIFICATION LOG.

File `0tCbAiVUlrPId3RWd9LRif`, page `2099:76` ("Undergoing Game Projects").

| | |
|---|---|
| Section | **FRONTROOMS · STAIRWELL (C22) · VISUAL PROPOSAL** `3484:7309` |
| Where it is | x 25257, y 34400, **8280 × 3880** (bottom at y 38280). The fix stage added a third row; before it, the top-level nodes within 3000 px below and 5000 px to the right were read: none. |
| Slides | **10 frames (SW00–SW09)**, 1920 × 1080, rows of 4, 2040 px apart in x and 1240 in y. Pages read `nn / 10`. |
| Grammar | As HUNTER `2331:852` and ROOM VISUALS `2862:6157`: header meta only (Plex Mono, `Week 6 · Oct 22, 2026`), Bayon titles at y 139 on the 12-col / 72 / 24 grid, content from y 232, the file's P1 styles only (`P1/Display/Title`, `P1/Display/Label`, `P1/Text/Body`, `P1/Text/Statement`, `P1/Header/Meta`). Black chips for content, yellow for status. No captions under images; sources in `SOURCES.md`. **SW01 credits** (media stage): each period photo and document carries its credit on the slide as a black evidence chip (`P1/Display/Label`: survey number · photographer · year, or source · figure · year) or as the Bayon label above it (source · section · page); the row labels name the collection ("HABS/HAER, Library of Congress"). The file has no separate P1 caption style, and no caption lines were added under images. |
| Images | **68** `img:<stem>` slots, every one with exactly one IMAGE fill (audited 2026-10-22 after the media-stage uploads); **no** `media:` placeholders left (the two on SW01 became image slots). |

## Frames

| # | Frame id | Name | What it shows (fix stage) |
|---|---|---|---|
| 01 | `3484:7310` | SW00 · Behind the EXIT | C22 with A13 ("locked until you hold the floor key"), what the stair is for (safe because the exit door shuts and latches once you pass its swing), the two-level map minimum, what 游戏视觉 owns (look, lit EXIT, the two A13 states). A's locked exit, first view in, and the far-door approach |
| 02 | `3484:7311` | SW01 · What 1990 stairs are | **Media stage: leads with the period photos and the code pages.** Row 1 (y 262, h 300): three HABS/HAER photos of 1989–90 (LeBlond fire stair, Vineland steel stair, Arden fire door), NBS BSS 120 Fig. 2.7.5 (1979), the USCG 1986 drawing. Row 2 (y 622, h 180): page crops of UBC 1991 p. 641 (§3306(p)), p. 647 (§3314(b)–(d)), NYC LL5 1973 p. 9 (C26-608.5), and the references labelled "References, not 1990": Kane 405 s (the stair), US tritium sign "photo 2014", *The Exit 8* "2023". Row 3: Stair ID sign / EXIT sign / Re-entry signs / Canon. Lede: "By 1990 code a stair door may lock behind you." Statement: "Real 1989–90 stairs: painted brick, pipe rails, a crossbar fire door." The typed code panel was removed (the page scans replace it) |
| 03 | `3484:7312` | SW02 · A · Painted block + steel pan | Hero: first view in. Top right: **turn back** (the period kit) and the **locked exit** from the maze. Bottom row: **to the far door**, Relay on the landing (1.99 / 6.19), detail. New Look / Kits / Light / Descent / WebGL texts. KPIs: 96K tris (whole stair), 34 draws, exit 5.04, landing body 6.19 |
| 04 | `3484:7313` | SW03 · B · Bare concrete core stair | Same layout; B renamed to the service-core / garage wording. KPIs 75K, 30, 4.29, 5.95 |
| 05 | `3484:7314` | SW04 · C · Pretends to be inside | Same layout; Descent notes the C08 cue layer must be left out of its paper copy. KPIs 40K, 22, 4.38, 9.60 |
| 06 | `3484:7315` | SW05 · Side by side | A / B / C rows × exit from the maze, first view in, **turn back**, **to the far door** |
| 07 | `3484:7316` | SW06 · Measured | Six Relay crops (landing and exit) with the new whole / body ratios; table (walk Y, walk fc-eq p2, exit and landing ratios); three walk-path illuminance crops; texts "The landing" (live test, pale head), "Fixed on the way", "Never dark" (relative wording), "For level design" (exit lamp hold, shut and latch, new-floor lamp hold). Statement: "The exit holds in all three, if its lamps stay lit." |
| 08 | `3484:7317` | SW07 · Your calls | Why A; questions: 1 the look, 2 the numbering scheme, 3 the steel-door and chain sounds, 4 the arrival door gets a stair-ID plate, not a dead EXIT, 5 the media batch. Media list with the Kane times fixed and the HABS/HAER + NARA searches as priority 1 |
| 09 | `3527:7362` | **SW08 · Locked, unlocked** (new) | A13 on the exit: locked at 8.6 m (hero), unlocked, locked at 5 m; the ladder at 12 m straight on and 20 m at 45°; exit lamps dead (Relay 1.75 : 1). Texts: Locked, Unlocked, The sign, The exit lamps, Sound, Owners. KPIs: lock 4.77 (8.6 m), 4.91 (20 m), sign T2 2.44, Relay with dead lamps 1.75 |
| 10 | `3527:7410` | **SW09 · The far door** (new) | From the turn landing (hero), arrival (lever and plate), held (lever down, 2 mm give), the exit door from inside (blank), B and C far doors. Texts: Live, Held, Light, Two levels, Strings, Level design. KPIs: 1 door with trim, 2 mm give, 2 levels, 0 gates |

## Slot → file

`IMG` = `Frontrooms3D/Documentation/research/stairwell/images/`. Each file was uploaded once (upload_assets, raw JPEG POST, HTTP 200 with an imageHash) into its first slot; the hash was then copied to the duplicate slots.

| Stem | File | imageHash | Slots |
|---|---|---|---|
| `stair_dir_a_door` | `IMG/stair_dir_a_door.jpg` (locked) | `9f9698cf…` | `3484:7331` (SW00), `3484:7410` (SW02), `3485:7322` (SW05) |
| `stair_dir_a_landing` | `IMG/stair_dir_a_landing.jpg` | `e4814c2f…` | `3484:7336`, `3484:7396`, `3485:7323`, `3485:8506` |
| `stair_dir_a_far_approach` | `IMG/stair_dir_a_far_approach.jpg` | `d1664ada…` | `3484:7339`, `3484:7401`, `3485:7325`, `3527:7418` |
| `stair_dir_a_turn_back` | `IMG/stair_dir_a_turn_back.jpg` | `bc3c4c55…` | `3484:7407`, `3485:7324`, `3527:7423` |
| `stair_dir_a_relay_stair` | `IMG/stair_dir_a_relay_stair.jpg` | `f9c1d158…` | `3484:7404` |
| `stair_dir_a_detail` | `IMG/stair_dir_a_detail.jpg` | `9589a5d1…` | `3526:7594` |
| `stair_dir_b_{landing,turn_back,door,far_approach,relay_stair,detail}` | `IMG/stair_dir_b_*.jpg` | `b8685f5d…`, `c00fa362…`, `e1a0e4ce…`, `d6daa040…`, `6d3c45ae…`, `c98df13e…` | SW03 `3484:7440`, `7451`, `7454`, `7445`, `7448`, `3526:7597`; SW05 `3485:7331`, `7332`, `7330`, `7333`; SW07 `3485:8509` |
| `stair_dir_c_{landing,turn_back,door,far_approach,relay_stair,detail}` | `IMG/stair_dir_c_*.jpg` | `3eabd2e8…`, `3119db62…`, `c55f9d10…`, `7ca1d512…`, `096fd737…`, `9c9faef0…` | SW04 `3484:7484`, `7495`, `7498`, `7489`, `7492`, `3526:7600`; SW05 `3485:7339`, `7340`, `7338`, `7341`; SW07 `3485:8512` |
| `fig_relay_{a,b,c}_{stair,maze}` | `IMG/figma/fig_relay_*.jpg` (480 × 540) | `4088df93…`, `eed00f66…`, `afc39361…`, `056d81c8…`, `91a5ac89…`, `f24f293c…` | SW06 `3485:7359`, `7364`, `7369`, `7374`, `7379`, `7384` |
| `fig_illum_{a,b,c}_flight` | `IMG/figma/fig_illum_*_flight.jpg` (960 × 540) | `67a3d73d…`, `e9bb78c6…`, `6fa44922…` | SW06 `3485:7413`, `7416`, `7419` |
| `fig_lock_a_{exit_8m,exit_8m_unlocked,door}` | `IMG/figma/fig_lock_a_*.jpg` (16:9 crops) | `1e39f1ae…`, `cbaff939…`, `df508933…` | SW08 `3527:7370`, `7381`, `7384` |
| `fig_ladder_a_{lad12_00_lk,lad20_45_lk}` | `IMG/figma/fig_ladder_a_*.jpg` | `f71094eb…`, `751d4c76…` | SW08 `3527:7375`, `7378` |
| `stair_ladder_a_relay_45_dead` | `IMG/stair_ladder_a_relay_45_dead.jpg` | `00716a7d…` | SW08 `3527:7407` |
| `stair_dir_a_far_door`, `…_held` | `IMG/stair_dir_a_far_door*.jpg` | `cbb7a84d…`, `6a147af0…` | SW09 `3527:7429`, `3527:7432` |
| `fig_far_door_{b,c}` | `IMG/figma/fig_far_door_{b,c}.jpg` | `41578900…`, `a944e56c…` | SW09 `3527:7426`, `3527:7455` |
| `gn_tritium_exit_sign`, `EX8_2_zero` | unchanged (stage 3) | `96ae3379…`, `016258e8…` | SW01 `3484:7368`, `3484:7371` (media stage: 134 × 180) |
| `sw_kane_ff_firedoor_poster` (was `run_research_r05_kane_backofhouse`, `7526e62d…`) | `fig_sw01_kane_0405.jpg` | `4255221b…` | SW01 `3484:7365` (134 × 180) |
| `sw_habs_office_stair_01_leblond_1989` | `fig_sw01_leblond.jpg` | `845cc263…` | SW01 `3552:7362` (426 × 300) |
| `sw_habs_office_stair_02_vineland_fire_stair_1989` | `fig_sw01_vineland.jpg` | `1eaf29a5…` | SW01 `3552:7363` (276 × 300) |
| `sw_habs_office_stair_03_arden_fire_door_1990` | `fig_sw01_arden.jpg` | `6379aff7…` | SW01 `3552:7364` (426 × 300) |
| `sw_bss120_p110_fig2-7-5_wall_paint_slope` | `fig_sw01_bss_fig275.jpg` | `e58721f2…` | SW01 `3484:7374` (the former `media:` frame, 276 × 300) |
| `sw_uscg_1986_emergency_light_units` | `fig_sw01_uscg.jpg` | `2b79da0d…` | SW01 `3552:7365` (276 × 300) |
| `sw_ubc1991_p641_stair_id_sign` | `fig_sw01_ubc_p641.jpg` | `35dc676f…` | SW01 `3552:7377` (426 × 180, 1 px keyline) |
| `sw_ubc1991_p647_exit_signs` | `fig_sw01_ubc_p647.jpg` | `73cf4582…` | SW01 `3552:7378` (426 × 180, keyline) |
| `sw_nyc_ll5_1973_stair_signs` | `fig_sw01_nyc_p9.jpg` | `e64b51ec…` | SW01 `3484:7377` (the former `media:` frame, 426 × 180, keyline) |

**The `figma/` crops** are label-free crops of this stage's own renders (`data/make_fix_images.py.txt`); the numbers sit in the slide chips.

**The `fig_sw01_*` crops** (media stage) are cut from the kept third-party files in `Research/week03/stairwell/` (ledger there), so they are **not** in the repo. They sit in the session scratchpad (`scratchpad/stair_media/figma/`, made by `scratchpad/stair_media/scripts/figcrop.py`: crop, re-encode without metadata, JPEG q90) and were uploaded once each (upload_assets, raw JPEG POST). Crop boxes, in pixels of the kept file (x0, y0, x1, y1), so they can be remade:

| Crop | From (`Research/week03/stairwell/…`) | Box | Size |
|---|---|---|---|
| `fig_sw01_leblond.jpg` | `stills/sw_habs_office_stair_01_leblond_1989.jpg` | 0, 53, 1024, 774 | 1024 × 721 |
| `fig_sw01_vineland.jpg` | `stills/sw_habs_office_stair_02_vineland_fire_stair_1989.jpg` | 0, 63, 825, 960 | 825 × 897 |
| `fig_sw01_arden.jpg` | `stills/sw_habs_office_stair_03_arden_fire_door_1990.jpg` | 0, 48, 1024, 769 | 1024 × 721 |
| `fig_sw01_bss_fig275.jpg` | `stills/sw_bss120_p110_fig2-7-5_wall_paint_slope.jpg` | 47, 40, 656, 702 (left part of the photo: stair, sloped band, sign) | 609 × 662 |
| `fig_sw01_uscg.jpg` | `stills/sw_uscg_1986_emergency_light_units.jpg` | 1040, 150, 2180, 1390 (notes 1–4, unit 5 by the exit light, legend, title block) | 1140 × 1240 |
| `fig_sw01_ubc_p641.jpg` | `stills/sw_ubc1991_p641_stair_id_sign.jpg` | 48, 95, 733, 384 (running head + §3306(p)) | 685 × 289 |
| `fig_sw01_ubc_p647.jpg` | `stills/sw_ubc1991_p647_exit_signs.jpg` | 48, 380, 733, 669 (§3314(b)–(d)) | 685 × 289 |
| `fig_sw01_nyc_p9.jpg` | `stills/sw_nyc_ll5_1973_stair_signs.jpg` | 55, 516, 800, 831 (C26-608.5 (a)–(b)) | 745 × 315 |
| `fig_sw01_kane_0405.jpg` | `clips/sw_kane_ff_firedoor_poster.jpg` | 355, 0, 1159, 1080 (the flight and the top door) | 804 × 1080 |

## Media placeholders

Both filled in the media stage (2026-10-22): `3484:7374` (was `media:sw_bss120_p110_fig2-7-5_wall_paint_slope`) and `3484:7377` (was `media:sw_nyc_ll5_1973_stair_signs`) keep their node ids, lost their "Awaiting download" texts (`3484:7375`, `7376`, `7378`, `7379` deleted) and auto-layout, and are now `img:` slots with the downloaded files (table above).

## Fix-stage edits, node by node

- Section `3484:7309` resized to 8280 × 3880. All `header page` texts set to `nn / 10`.
- SW00: texts `3484:7343`, `7345`, `7347`, `7349`; slot `3484:7339` renamed to `img:stair_dir_a_far_approach`.
- SW01: new code panel `3526:7579` (label + 5 quotes), labels "On disk · references, not 1990" and "Awaiting download · priority 1"; image slots `3484:7365`, `7368`, `7371` resized to 276 × 155 at y 262; chips `3484:7366`, `7369`, `7372` retitled; placeholders `3484:7374`, `7377` moved to y 471; texts `3484:7381`, `7385`, `7387`, statement `7388`.
- SW02–SW04: the door slot became `turn_back`, the detail slot became the locked `door`, the flight slot became `far_approach` (276 × 240), the relay slot 276 × 240, a new `detail` slot (`3526:7594`, `7597`, `7600`) with its chip (`3526:7595`, `7598`, `7601`); chips retitled; Look / Kits / Light / Descent (/ WebGL) texts and KPIs replaced; SW03 retitled "B · Bare concrete core stair".
- SW05: slots in column 3 → `turn_back`, column 4 → `far_approach`; header chip "Turn back"; chip `3485:7328` → "To the far door" (yellow, header row); chips `3485:7336`, `3485:7344` removed.
- SW06: six relay chips, the table, three illuminance chips, four texts and the statement.
- SW07: Why A, Q2–Q4, the Kane media line, "Priority 1 · searches" and its body.
- SW08 `3527:7362` and SW09 `3527:7410`: clones of SW02's layout with their own texts, chips and slots.

## Media stage (2026-10-22): SW01 rebuilt, node by node

Red approved the priority 1 downloads at 13:00. SW01 now leads with the period photos and the code pages; the references stay, labelled "References, not 1990". Grid: 12 columns, 72 margin, 24 gutter; the 3-column spans start at x 72 / 522 / 972 / 1422.

- **Removed:** the typed code panel `3526:7579` (label + 5 quotes; the quotes stay in `01_research.md` §1.5 and SOURCES S33), and the four "Awaiting download" texts `3484:7375`, `7376`, `7378`, `7379`.
- **Row 1, y 262, h 300 (photos and documents):** new slots `3552:7362` LeBlond (x 72, 426), `3552:7363` Vineland (x 522, 276), `3552:7364` Arden (x 822, 426), `3552:7365` USCG (x 1572, 276); `3484:7374` BSS Fig. 2.7.5 (x 1272, 276). Chips (clones of `3484:7366`): `3552:7366` "OH-71-B-8 · Gerhardstein · 1989", `3552:7368` "NJ-979-A-22 · Herr · 1989", `3552:7370` "PA-5585-B-4 · Harris · 1990", `3552:7372` "BSS 120 · Fig. 2.7.5 · 1979", `3552:7374` "USCG · Bldg 877 · May 1986". Row labels: `3526:7593` "Photographed 1989–90 · HABS/HAER, Library of Congress" (x 72, y 232), new `3552:7376` "NBS 1979 · USCG 1986" (x 1272).
- **Row 2, y 622, h 180 (code pages + references):** new slots `3552:7377` UBC p. 641 (x 72), `3552:7378` UBC p. 647 (x 522); `3484:7377` NYC p. 9 (x 972); all three 426 wide with a 1 px ink keyline at 20 %. Labels at y 592: new `3552:7379` "UBC 1991 · Sec. 3306(p) · p. 641", `3552:7380` "UBC 1991 · Sec. 3314 · p. 647", `3552:7381` "NYC Local Law 5 · 1973 · p. 9" ("Sec." because Bayon has no § glyph); `3526:7592` "References, not 1990 · sign photo CC BY-SA 4.0" (x 1422, 386 wide). References 134 × 180: `3484:7365` renamed `img:sw_kane_ff_firedoor_poster` with the 405 s frame (x 1422), `3484:7368` tritium (x 1568), `3484:7371` Exit 8 (x 1714); chips `3484:7366` "Kane 405 s", `3484:7369` "Gazebo 2014", `3484:7372` "Exit 8 2023". (Critic, 2026-10-22: the tritium chip read "Photo 2014" with no author or licence; the CC BY-SA 4.0 photo by Gazebo, `Research/week02/phosphor-narrative/SOURCES.md` row 18, now names its author on the chip and its licence on the label, as the other research slides do, e.g. SD01 `3533:7409`. Chip 116 wide, inside its 134 image.)
- **Row 3, y 832 / 860 (findings):** `3484:7380`/`7381` Stair ID sign, `3484:7382`/`7383` EXIT sign, `3484:7384`/`7385` Re-entry signs, `3484:7386`/`7387` Canon (bodies 426 wide, ≤ 3 lines).
- **Lede** `3484:7364`: "By 1990 code a stair door may lock behind you." (one line). **Statement** `3484:7388`: "Real 1989–90 stairs: painted brick, pipe rails, a crossbar fire door."

## Checks done

- One composition screenshot per slide after the edits (scale 0.42–0.5). One fix round: the SW06 "For level design" line break, the SW05 header chip colour, the bottom-row slots enlarged to 276 × 240.
- Every `img:` slot has exactly one IMAGE fill (60 / 60). No body text overflows its slot (heights 52 or 78 as designed). Fonts: Bayon, Source Serif 4 and IBM Plex Mono only, through the P1 styles.
- **Media stage:** one composition screenshot of SW01 (scale 0.5) after the uploads; no fix needed. Section audit: **68 / 68** `img:` slots with exactly one IMAGE fill, 0 `media:` placeholders. SW01 fonts: Bayon, Source Serif 4, IBM Plex Mono only, all through the P1 styles. Row-3 bodies 52–78 px high; the statement 1281 px wide (one line).

## Not done here

- VL325–VL328 are specified in `W/vl_pending/stairwell.md`, not placed.
- No Slides (P1 deck) mirror.
- **Media stage:** only SW01 was changed. SW07 ("Your calls") still lists the media batch as question 5 and the HABS/HAER + NARA searches as open; SW00 and SW02–SW09 were not touched. The HAER/HABS photos show brick, not the block of look A; nothing on SW02 was changed to say so.
- **Critic stage (2026-10-22, about 15:15 PDT, account A):** SW01 re-audited (11 image slots, one IMAGE fill each; every hash equals the SHA-1 of its `fig_sw01_*` crop or of the tritium / EX8 file; 68 / 68 in the section; no `media:` text). Three stale SW07 texts fixed in place, styles kept, heights unchanged: `3485:7442` "Media · priority 1 approved 10-22 and downloaded" (was "Media to approve · nothing downloaded yet"); `3485:7447` Kane line now "the push-bar door, then a straight concrete stair (403.5–411.5 s). A 5 MB MP4, no GIF." (was "no stair … a 6 MB GIF"); `3485:7452` "Priority 1 fetched about 70 MB and kept about 10 MB." (was the 54 / 15 MB estimate). Question 5 itself is left as asked.
