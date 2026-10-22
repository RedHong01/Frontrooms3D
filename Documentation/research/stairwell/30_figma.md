# Stairwell (C22): the Figma section

Workflow `c22-stairwell-look`, stage 3 (Figma proposal). Built 2026-10-22 by 游戏视觉 with use_figma. Only this section was written. No other section was touched, and nothing was placed in the VISUAL VERIFICATION LOG.

File `0tCbAiVUlrPId3RWd9LRif`, page `2099:76` ("Undergoing Game Projects").

| | |
|---|---|
| Section | **FRONTROOMS · STAIRWELL (C22) · VISUAL PROPOSAL** `3484:7309` |
| Where it is | x 25257, y 34400, 8280 × 2640 (bottom at y 37040). |
| How the spot was chosen | The bounds of all 49 top-level nodes were read first. The column at x 25257 holds ROOM VISUALS `2862:6157`, DOORS + WINDOWS `2748:6099`, LEVEL TRANSITIONS `2831:6157` and PRINT REFERENCE `3198:7273`, which ends at y 33600. Nothing lies in x 25257–33537 below that. After the build, an overlap check against every other top-level node found none. |
| Slides | 8 frames (SW00–SW07), 1920 × 1080, in 2 rows of 4, 2040 px apart in x and 1240 in y. |
| Grammar | The same as HUNTER `2331:852` and ROOM VISUALS `2862:6157`. The header holds meta only: Plex Mono, `Week 6 · Oct 22, 2026`, page `nn / 08`. Titles are Bayon at y 139 on the 12-col / 72 / 24 grid, and content starts at y 232. All text uses the file's P1 styles (`P1/Display/Title`, `P1/Display/Label`, `P1/Text/Body`, `P1/Text/Statement`, `P1/Header/Meta`); there is no Inter. Chips sit on the images: black for content, yellow for status. There are no captions under images and no source lines; sources are in `SOURCES.md`. Statements sit at y 966. |
| Images | 42 `img:<stem>` slots holding 29 distinct stems, plus 2 `media:<key>` placeholders on SW01. Every `img:` slot has exactly one IMAGE fill (checked after the build). |
| Text | 247 text nodes, all in the P1 styles. |

## Frames

| # | Frame id | Name | What it shows |
|---|---|---|---|
| 01 | `3484:7310` | SW00 · Behind the EXIT | C22 (one EXIT per floor, a stair one floor deeper, a run ends only when caught), what the stair is for (the one safe room; it hides the build), the map minimum (level design, 2–3 days), what 游戏视觉 owns (the look and the C23 sign). A's door, landing and flight. KPIs: 2 cells, 3.5 m, 20 risers, 2.7 s walk |
| 02 | `3484:7311` | SW01 · What 1990 stairs are | Led by real sources on disk: Kane *Found Footage* 6:08–6:56 back of house (fire door with a push bar), a real 1970s US EXIT sign (Commons, CC BY-SA 4.0), *The Exit 8* sign 0 at 0:20. Two `media:` placeholders wait on the download batch (BSS 120 Fig. 2.7.5, NYC LL5 1973). Four facts: the lock is real, UBC 1991 numbers, the period look, canon |
| 03 | `3484:7312` | SW02 · A · Painted block + steel pan | Landing (hero), flight, Relay on the landing, door, detail. Look / Kits / Light / Sound / Descent / WebGL. KPIs: 63K tris, 30 draws, exit 4.87, landing body 4.78 |
| 04 | `3484:7313` | SW03 · B · Bare concrete service stair | Same layout. KPIs: 47K, 25, 4.22, landing body 3.09 (fail) |
| 05 | `3484:7314` | SW04 · C · Pretends to be inside | Same layout; the last block is "Best use" (a later deep-floor event). KPIs: 27K, 20, 4.38, 7.90 |
| 06 | `3484:7315` | SW05 · Side by side | A / B / C rows × door, landing, flight, Relay on the landing, same cameras |
| 07 | `3484:7316` | SW06 · Measured | Six Relay crops (landing and exit per direction) with whole / body ratios, the measurement table, three walk-path illuminance crops, "The landing", "Fixed on the way", "Never dark", "For level design" |
| 08 | `3484:7317` | SW07 · Your calls | Why A; 5 questions with a recommendation each; the A / B / C landing strip with a yellow "My pick" chip on A; the media batch with sizes |

## Slot → file

`IMG` = `Frontrooms3D/Documentation/research/stairwell/images/`. Every file was uploaded once (upload_assets, raw JPEG POST, HTTP 200 with an imageHash), then its hash was copied to the duplicate slots.

| Stem | File | imageHash | Slots |
|---|---|---|---|
| `stair_dir_a_door` | `IMG/stair_dir_a_door.jpg` | `e3c71425…` | `3484:7331` (SW00), `3484:7407` (SW02), `3485:7322` (SW05) |
| `stair_dir_a_landing` | `IMG/stair_dir_a_landing.jpg` | `fad79798…` | `3484:7336` (SW00), `3484:7396` (SW02), `3485:7323` (SW05), `3485:8506` (SW07) |
| `stair_dir_a_flight` | `IMG/stair_dir_a_flight.jpg` | `79595853…` | `3484:7339` (SW00), `3484:7401` (SW02), `3485:7324` (SW05) |
| `stair_dir_a_relay_stair` | `IMG/stair_dir_a_relay_stair.jpg` | `8f02643f…` | `3484:7404`, `3485:7325` |
| `stair_dir_a_detail` | `IMG/stair_dir_a_detail.jpg` | `d0ac1764…` | `3484:7410` |
| `stair_dir_b_landing` | `IMG/stair_dir_b_landing.jpg` | `bd625894…` | `3484:7440`, `3485:7331`, `3485:8509` |
| `stair_dir_b_flight` | `IMG/stair_dir_b_flight.jpg` | `c8a860fd…` | `3484:7445`, `3485:7332` |
| `stair_dir_b_relay_stair` | `IMG/stair_dir_b_relay_stair.jpg` | `af0adf5f…` | `3484:7448`, `3485:7333` |
| `stair_dir_b_door` | `IMG/stair_dir_b_door.jpg` | `548c5856…` | `3484:7451`, `3485:7330` |
| `stair_dir_b_detail` | `IMG/stair_dir_b_detail.jpg` | `b0e4cd07…` | `3484:7454` |
| `stair_dir_c_landing` | `IMG/stair_dir_c_landing.jpg` | `94251d87…` | `3484:7484`, `3485:7339`, `3485:8512` |
| `stair_dir_c_flight` | `IMG/stair_dir_c_flight.jpg` | `017e7d2b…` | `3484:7489`, `3485:7340` |
| `stair_dir_c_relay_stair` | `IMG/stair_dir_c_relay_stair.jpg` | `2c3cbeda…` | `3484:7492`, `3485:7341` |
| `stair_dir_c_door` | `IMG/stair_dir_c_door.jpg` | `7541a9bd…` | `3484:7495`, `3485:7338` |
| `stair_dir_c_detail` | `IMG/stair_dir_c_detail.jpg` | `aa4e94e4…` | `3484:7498` |
| `fig_relay_{a,b,c}_{stair,maze}` | `IMG/figma/fig_relay_*.jpg` (476 × 470) | `07371ef9…` a_stair, `3f167332…` a_maze, `67a1cef7…` b_stair, `9e2221ba…` b_maze, `ae3782a1…` c_stair, `219e469c…` c_maze | `3485:7359`, `7364`, `7369`, `7374`, `7379`, `7384` (SW06, in that order) |
| `fig_illum_{a,b,c}_flight` | `IMG/figma/fig_illum_*_flight.jpg` (956 × 488) | `132f531f…`, `59ff0799…`, `b4a5702c…` | `3485:7413`, `7416`, `7419` (SW06) |
| `run_research_r05_kane_backofhouse` | `Frontrooms3D/Documentation/research/room_visuals/images/run_research_r05_kane_backofhouse.jpg` (ledger `room_visuals/SOURCES.md`, Kane FF 368–416 s) | `7526e62d…` | `3484:7365` (SW01) |
| `gn_tritium_exit_sign` | `Research/week02/phosphor-narrative/stills/gn_tritium_exit_sign.jpg` (Commons, Gazebo, CC BY-SA 4.0) | `96ae3379…`, copied from ROOM VISUALS RV24 `2865:6215` (no upload) | `3484:7368` (SW01) |
| `EX8_2_zero` | `Research/week01/assets/clips/EX8_2_zero.jpg` (*The Exit 8*, 0:20–0:24) | `016258e8…`, already in this file (`Research/week01/assets/clips/figma_image_hashes.json`; no upload) | `3484:7371` (SW01) |

**The `figma/` crops.** These are label-free crops of this stage's own check images, made with PIL in the visual venv. Red's deck rule bans baked labels on slides, so the chips carry the numbers instead.
- `fig_relay_*`: the six panels of `stair_v_relay_readability.jpg`, 480 px wide, rows 86–556 (the title, the panel label and the bottom data line are cut).
- `fig_illum_*`: the three panels of `stair_v_illuminance.jpg`, rows 86–574 (the title, the panel label and the log legend are cut). The fc-eq values are in the chips; the legend (1–60 fc-eq, log) is in the full image.

## Media placeholders (waiting on Red's download approval; `media_candidates.md` §2)

| Frame | Key | Candidate |
|---|---|---|
| `3484:7374` | `media:sw_bss120_p110_fig2-7-5_wall_paint_slope` | P1, NBS BSS 120 p. 110 Fig. 2.7.5 |
| `3484:7377` | `media:sw_nyc_ll5_1973_stair_signs` | P1, NYC Local Law 5 of 1973 |

When Red approves, render each page to JPG outside the repo (`Research/week03/stairwell/stills/`), upload it into its frame (or replace the frame with an `img:` slot of the same size), and add its row to `SOURCES.md`.

## Numbers on the slides (source: `10_directions.md` §2–§6, `data/metrics_final.json`)

- SW02–SW04 KPIs: production estimate per floor period (tris at LOD0, draws once merged), Relay at the exit (whole), Relay on the turn landing (body).
- SW06 table: stair median Y range (landing / flight / detail / relay_stair frames), walk path E p2 to median in fc-eq, Relay whole / body at the exit and on the landing. Landing whole sweep (×3 light): 2.04 / 1.81 / 3.64, rounded on the slide to 2.0, 1.8 and 3.6.
- SW07 media sizes: `media_candidates.md` §2. Priority 1 fetches 7.1 + 41.2 + 0.8 + about 5 MB, about 54 MB. It keeps about 1.7 + 2 + 0.3 + 5 + 6 MB, about 15 MB.

## Checks done

- Composition screenshots of all eight slides at 0.42×. One fix round on SW07: the lede was cut to one line, the A / B / C landing strip was added, and the statement was reworded so it does not repeat the "My pick" chip.
- Every `img:` slot has one IMAGE fill. No section overlap. Fonts: Bayon, Source Serif 4 and IBM Plex Mono only, through the P1 styles.

## Not done here

- The VL325–VL326 check slides (`W/vl_pending/stairwell.md`) are not placed. That belongs to whoever places VL slides, after re-reading `VERIFICATION_LOG.md`.
- No Slides (P1 deck) mirror was made.
