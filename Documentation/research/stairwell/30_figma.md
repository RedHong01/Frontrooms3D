# Stairwell (C22): the Figma section

Workflow `c22-stairwell-look`. Stage 3 built it (2026-10-22 morning); **stage 5 (Fix) revised it the same day** (12:50–13:20) after the critics (`50_critic_fixes.md`). Written by 游戏视觉 with use_figma. Only this section was written; nothing was placed in the VISUAL VERIFICATION LOG.

File `0tCbAiVUlrPId3RWd9LRif`, page `2099:76` ("Undergoing Game Projects").

| | |
|---|---|
| Section | **FRONTROOMS · STAIRWELL (C22) · VISUAL PROPOSAL** `3484:7309` |
| Where it is | x 25257, y 34400, **8280 × 3880** (bottom at y 38280). The fix stage added a third row; before it, the top-level nodes within 3000 px below and 5000 px to the right were read: none. |
| Slides | **10 frames (SW00–SW09)**, 1920 × 1080, rows of 4, 2040 px apart in x and 1240 in y. Pages read `nn / 10`. |
| Grammar | As HUNTER `2331:852` and ROOM VISUALS `2862:6157`: header meta only (Plex Mono, `Week 6 · Oct 22, 2026`), Bayon titles at y 139 on the 12-col / 72 / 24 grid, content from y 232, the file's P1 styles only (`P1/Display/Title`, `P1/Display/Label`, `P1/Text/Body`, `P1/Text/Statement`, `P1/Header/Meta`). Black chips for content, yellow for status. No captions under images; sources in `SOURCES.md`. |
| Images | 60 `img:<stem>` slots, every one with exactly one IMAGE fill (audited after the fix uploads), plus 2 `media:<key>` placeholders on SW01. |

## Frames

| # | Frame id | Name | What it shows (fix stage) |
|---|---|---|---|
| 01 | `3484:7310` | SW00 · Behind the EXIT | C22 with A13 ("locked until you hold the floor key"), what the stair is for (safe because the exit door shuts and latches once you pass its swing), the two-level map minimum, what 游戏视觉 owns (look, lit EXIT, the two A13 states). A's locked exit, first view in, and the far-door approach |
| 02 | `3484:7311` | SW01 · What 1990 stairs are | **Leads with typed code text** (UBC 1991 §3306(p), §3306(c), §3313(a), §3314(b); NYC LL5 C26-608.5) in a light panel. A row labelled "On disk · references, not 1990": Kane 368–416 s ("no stair"), US tritium sign "photo 2014", *The Exit 8* "2023 · device only". The two download placeholders under "Awaiting download · priority 1". Statement: "The code leads until real 1990 photos are approved." |
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
| `run_research_r05_kane_backofhouse`, `gn_tritium_exit_sign`, `EX8_2_zero` | unchanged (stage 3) | `7526e62d…`, `96ae3379…`, `016258e8…` | SW01 `3484:7365`, `7368`, `7371` (resized to 276 × 155) |

**The `figma/` crops** are label-free crops of this stage's own renders (`data/make_fix_images.py.txt`); the numbers sit in the slide chips.

## Media placeholders (waiting on Red's download approval; `media_candidates.md` §2)

| Frame | Key | Candidate |
|---|---|---|
| `3484:7374` | `media:sw_bss120_p110_fig2-7-5_wall_paint_slope` | P1, NBS BSS 120 p. 110 Fig. 2.7.5 |
| `3484:7377` | `media:sw_nyc_ll5_1973_stair_signs` | P1, NYC Local Law 5 of 1973 |

## Fix-stage edits, node by node

- Section `3484:7309` resized to 8280 × 3880. All `header page` texts set to `nn / 10`.
- SW00: texts `3484:7343`, `7345`, `7347`, `7349`; slot `3484:7339` renamed to `img:stair_dir_a_far_approach`.
- SW01: new code panel `3526:7579` (label + 5 quotes), labels "On disk · references, not 1990" and "Awaiting download · priority 1"; image slots `3484:7365`, `7368`, `7371` resized to 276 × 155 at y 262; chips `3484:7366`, `7369`, `7372` retitled; placeholders `3484:7374`, `7377` moved to y 471; texts `3484:7381`, `7385`, `7387`, statement `7388`.
- SW02–SW04: the door slot became `turn_back`, the detail slot became the locked `door`, the flight slot became `far_approach` (276 × 240), the relay slot 276 × 240, a new `detail` slot (`3526:7594`, `7597`, `7600`) with its chip (`3526:7595`, `7598`, `7601`); chips retitled; Look / Kits / Light / Descent (/ WebGL) texts and KPIs replaced; SW03 retitled "B · Bare concrete core stair".
- SW05: slots in column 3 → `turn_back`, column 4 → `far_approach`; header chip "Turn back"; chip `3485:7328` → "To the far door" (yellow, header row); chips `3485:7336`, `3485:7344` removed.
- SW06: six relay chips, the table, three illuminance chips, four texts and the statement.
- SW07: Why A, Q2–Q4, the Kane media line, "Priority 1 · searches" and its body.
- SW08 `3527:7362` and SW09 `3527:7410`: clones of SW02's layout with their own texts, chips and slots.

## Checks done

- One composition screenshot per slide after the edits (scale 0.42–0.5). One fix round: the SW06 "For level design" line break, the SW05 header chip colour, the bottom-row slots enlarged to 276 × 240.
- Every `img:` slot has exactly one IMAGE fill (60 / 60). No body text overflows its slot (heights 52 or 78 as designed). Fonts: Bayon, Source Serif 4 and IBM Plex Mono only, through the P1 styles.

## Not done here

- VL325–VL328 are specified in `W/vl_pending/stairwell.md`, not placed.
- No Slides (P1 deck) mirror.
