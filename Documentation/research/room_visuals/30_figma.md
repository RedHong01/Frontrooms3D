# Room visuals: the Figma section

File `0tCbAiVUlrPId3RWd9LRif`, page `2099:76` ("Undergoing Game Projects").

| | |
|---|---|
| Section | **FRONTROOMS · ROOM VISUALS** `2862:6157` |
| Where it is now | x 25257, y 2000, 8280 × 11240. It was built on 10-08 at 01:42 at x 42617, y 2000 (the spot agreed with 平面视觉). Someone moved it later. The VISUAL VERIFICATION LOG (`2595:6093`) now sits at x 42617. I left it where it is. On 10-08 at 19:4x it overlaps no other section. Below it is DOORS + WINDOWS (`2748:6099`, from y 13920). To its right is HUD KEY (`2532:4038`, x 33937). |
| Slides | 33 frames, 1920 × 1080, in 9 rows of 4, 2040 px apart in x and 1240 px apart in y. |
| Grammar | Copied from HUNTER `2331:852`. The running header (Plex Mono, meta only) and page numbers sit in the header. Titles are Bayon at y 139 on the 12-col / 72 / 24 grid. Content starts at y 232. Sentences are Source Serif 4. The file's P1 text styles are used (`P1/Display/Title`, `P1/Display/Label`, `P1/Text/Body`, `P1/Text/Statement`, `P1/Header/Meta`). There is no Inter. Chips sit on the images. There are no captions under images and no source footers. Sources are in `SOURCES.md`. |
| Images | 135 `img:<file stem>` slots, 92 distinct images. Every slot holds exactly one IMAGE fill (checked 10-08 19:4x). |
| Placeholders | 3 `media:<key>` frames on RV24, for candidates waiting on Red's download approval. |

## Build history

1. **10-08, 01:42–01:51 (the first attempt).** It built all 33 frames and filled 13 slots by copying images from other sections. It then asked for upload URLs for the next 60 slots. The session ended at the POST step. Those 60 images reached Figma anyway: they were there when I resumed.
2. **10-08, 19:3x–19:4x (this pass).** I checked all 33 frames against current main (`bc0aa79`). Then:
   - I uploaded the last 33 images with `upload_assets`: raw JPEG POSTs, all 33 returned HTTP 200 with an imageHash.
   - I copied each stem's hash into its 29 duplicate slots.
   - I resized two slots to their images' aspect: `gn_tritium_exit_sign` to 716 × 493 and `q16_b_front` to 349 × 240.
   - I pillarboxed the two 1514 × 1080 Kane stills: a black fill under the image, set to FIT.
   - I took a screenshot of every slide and fixed what they showed:
     - **The ↔ glyph.** It rendered as an emoji box in 4 texts. I replaced it with an en dash.
     - **RV25.** I cut its lede to 2 lines.
     - **RV27.** "22 : 1" now stays on one line (no-break spaces).
     - **RV29.** I moved its bench chip off the bench image's own labels.
   - I updated the facts that changed since the 7 Oct frames:
     - **RV06:** Q13 is fixed. A room dresses at 3 ms a frame since `bc0aa79`.
     - **RV11 and RV21:** the EGRESS placard has been on the start door's map side since 8 Oct (Q16, `bc0aa79`).
     - **RV02:** the statement now lists the placard.
     - **RV28 and RV29:** they now carry the House Lights probe caveat. See open points.

## Frames

| # | Frame id | Name | What it shows |
|---|---|---|---|
| 01 | `2862:6158` | RV00 · Every room, today | 15 room types as thumbnails with IN PLAY / NEVER REACHED / missing chips (main `ee5c9bb`, seed 4242, 7 Oct) |
| 02 | `2862:6240` | RV01 · What feeds each room | Matrix: 15 rooms × 11 research tracks; black square = in the game, outline = proposal |
| 03 | `2862:6249` | RV02 · Changed since 3 October | 3 Oct vs 7 Oct: wallpaper (WP03 hard edge), window (kit frame + glass), lens (prismatic × 1.5) |
| 04 | `2862:6276` | RV03 · Level 0 · Standard | Frames, surfaces, ceiling, light, fog / ambient, props, problems, KPIs |
| 05 | `2862:6303` | RV04 · Level 0 · Low | same layout |
| 06 | `2862:6330` | RV05 · Level 0 · Tall | same layout |
| 07 | `2862:6357` | RV06 · Office | same layout |
| 08 | `2863:6189` | RV07 · Five lamp moods | 5 moods + odds by tier + lens glow rule |
| 09 | `2863:6226` | RV08 · When a lamp fails | Before / during each fault, with frame means |
| 10 | `2863:6253` | RV09 · Columns and bulkheads | Rule, sizes, finish, bulkheads, problems |
| 11 | `2863:6286` | RV10 · Zone borders | Door, window, theme line; links to LEVEL TRANSITIONS `2831:6157` |
| 12 | `2863:6321` | RV11 · Start rooms | Handoff, the start door, the seam, the placard |
| 13 | `2863:6356` | RV12 · Title · Lobby | |
| 14 | `2863:6389` | RV13 · Title · Shift | never reached |
| 15 | `2864:6157` | RV14 · Title · Office | never reached |
| 16 | `2864:6186` | RV15 · Title · Run today | never reached; the salmon room |
| 17 | `2864:6219` | RV16 · Title · Exit | never reached |
| 18 | `2864:6250` | RV17 · The true Exit | missing; exit-sign track (clone, not merged) and the exit doors on paper |
| 19 | `2864:6270` | RV18 · Research in Figma | 8 cards linking to sections 2312, 2324, 2349, 2331, 2407, 2397, 2831, 2748 |
| 20 | `2864:6302` | RV19 · More research in Figma | 7 cards: 2441, 2471, 2528, 2235:735, 2245:8, 2256:8, 2595:6093 |
| 21 | `2864:6332` | RV20 · Research on disk | 8 cards: office_and_film, interaction_audit, glass, glass/rt, glass/destruction, exit_sign, outlets, door_break |
| 22 | `2864:6364` | RV21 · More on disk | placard, inspect, webgl, room_visuals; stale top-level specs |
| 23 | `2865:6157` | RV22 · Run! has no plan | Week 1 clip vs today's Run room; the four old looks |
| 24 | `2865:6180` | RV23 · What the games do | ETB, Kane × 2, A24, Backrooms 1998, Dark Deception, with measured values |
| 25 | `2865:6207` | RV24 · What 1990 does | Tritium EXIT, Scandinavian Star, 3 media placeholders, 4 fact blocks |
| 26 | `2865:6242` | RV25 · Rules for a fair run | Chase numbers, value-only comparison, 7 rules |
| 27 | `2865:6271` | RV26 · A · Red Ward | Pre-render, light, floor, measured, KPIs |
| 28 | `2865:6310` | RV27 · B · Emergency Power | same layout |
| 29 | `2865:6349` | RV28 · C · House Lights | same layout |
| 30 | `2866:6157` | RV29 · A glossy floor | B matte vs glossy, C waxed, G14b bench; per-direction floor status; RT later |
| 31 | `2866:6183` | RV30 · Side by side | 3 × 4 grid: from Level 0, armed, tripped, look back |
| 32 | `2866:6219` | RV31 · My pick | Emergency Power; why; borrow later; 7 questions; metrics table |
| 33 | `2866:6244` | RV32 · Media to approve | 23 candidates (A1–A12, B1–B5, C1–C4, D1–D2) |

## Slot → file

`DOC` = `Frontrooms3D/Documentation/research/room_visuals/images/`. A stem with no other path is `DOC/<stem>.jpg`.

### Current frames (7 Oct, main `ee5c9bb`, seed 4242)

| Stem | Slots |
|---|---|
| `room1007_map-l0-standard_wide` | `2862:6166`, `2862:6284` |
| `room1007_map-l0-standard_detail` | `2862:6260`, `2862:6287` |
| `room1007_map-l0-low_wide` | `2862:6171`, `2862:6311` |
| `room1007_map-l0-low_detail` | `2862:6314` |
| `room1007_map-l0-tall_wide` | `2862:6176`, `2862:6338` |
| `room1007_map-l0-tall_ceiling` | `2862:6341` |
| `room1007_map-l0-pile_wide` | `2862:6344` |
| `room1007_map-office_wide` | `2862:6181`, `2862:6365` |
| `room1007_map-office_detail` | `2862:6368` |
| `room1007_map-lamp-steady_detail` | `2862:6272`, `2863:6197` |
| `room1007_map-lamp-stutter_detail` | `2863:6200` |
| `room1007_map-lamp-failing_detail` | `2863:6203` |
| `room1007_map-lamp-dead_detail` | `2863:6206` |
| `room1007_map-lamp-dim_detail` | `2862:6186`, `2863:6209` |
| `room1007_map-failing-lamp_high` / `_dropout` | `2863:6234` / `2863:6237` |
| `room1007_map-stutter-lamp_on` / `_off` | `2863:6240` / `2863:6243` |
| `room1007_map-dead-lamp_wide` | `2862:6191`, `2863:6246` |
| `room1007_map-dead-lamp_detail` | `2863:6249` |
| `room1007_map-column-office_wide` | `2862:6196`, `2863:6261` |
| `room1007_map-column-l0_wide` | `2863:6264` |
| `room1007_map-column-office_detail` | `2863:6267` |
| `room1007_map-border-door_wide` | `2862:6201`, `2863:6294` |
| `room1007_map-border-window_wide` | `2863:6299` |
| `room1007_map-border-window_detail` | `2862:6266` |
| `room1007_map-border-theme_wide` | `2863:6302` |
| `room1007_start_doorway` | `2862:6206`, `2862:6288`, `2863:6329` |
| `room1007_start_wide` / `_back` | `2863:6334` / `2863:6337` |
| `room1007_title-lobby_title` | `2862:6211` |
| `room1007_title-lobby_wide` / `_doorway-opening` / `_detail` | `2863:6364` / `2863:6367` / `2863:6370` |
| `room1007_title-shift_wide` | `2862:6216`, `2863:6397` |
| `room1007_title-shift_doorway` / `_detail` | `2863:6400` / `2863:6401` |
| `room1007_title-office_wide` | `2862:6221`, `2864:6165` |
| `room1007_title-office_detail` / `_doorway` | `2864:6168` / `2864:6169` |
| `room1007_title-run_wide` | `2862:6226`, `2864:6194`, `2865:6168` |
| `room1007_title-run_sign` / `_props` | `2864:6197` / `2864:6200` |
| `room1007_title-exit_wide` | `2862:6231`, `2864:6227` |
| `room1007_title-exit_threshold` / `_doorway` | `2864:6230` / `2864:6233` |
| `room_map-l0-standard_detail`, `room_map-border-window_detail`, `room_map-lamp-steady_detail` (3 Oct) | `2862:6257`, `2862:6263`, `2862:6269` |
| `room_contact_sheet_all` | `2864:6382` |

### Run! research and pre-renders

| Stem | Slots | File, if not `DOC/<stem>.jpg` |
|---|---|---|
| `ETB_3_run` | `2864:6319`, `2865:6165` | `Research/week01/assets/clips/ETB_3_run.jpg` |
| `DD_3_chase` | `2865:6203` | `Research/week01/assets/clips/DD_3_chase.jpg` |
| `run_ref_etb_levelbang_0094`, `run_ref_a24_trailer_0027_run`, `run_ref_backrooms1998_0076_red` | `2865:6188`, `2865:6197`, `2865:6200` | |
| `run_ref_kane_ff_0370_tunnel`, `run_ref_kane_ff_0489_silhouette` | `2865:6191`, `2865:6194` (FIT on black) | |
| `gn_tritium_exit_sign` | `2865:6215` | `Research/week02/phosphor-narrative/stills/gn_tritium_exit_sign.jpg` |
| `run_research_r04_chase_numbers`, `run_research_r02_value_only` | `2865:6250`, `2865:6253` | |
| `run_dir_red_ward_s1b` / `s1a` / `s2` / `s3` / `c4` | `2865:6279`+`2866:6199` / `2865:6284`+`2866:6196` / `2865:6287`+`2866:6191` / `2865:6290` / `2865:6293`+`2866:6202` | |
| `run_dir_emergency_power_s1b` / `s1a` / `s2` / `s3` / `c4` | `2865:6318`+`2866:6209` / `2865:6323`+`2866:6208` / `2865:6326`+`2866:6205` / `2865:6329` / `2865:6332`+`2866:6210` | glossy versions |
| `run_dir_emergency_power_c3`, `_x_goal`, `_v_reflections` | `2866:6227`, `2866:6168`, `2864:6329` | |
| `run_emergency_power_x_goal_matte` | `2866:6165` | the kept matte version |
| `run_dir_house_lights_s1b` / `s1a` / `s2` / `s3c` / `c4b` / `c4` / `x2` | `2865:6357`+`2866:6215` / `2865:6362`+`2866:6214` / `2865:6365`+`2866:6211` / `2865:6368` / `2865:6371` / `2866:6216` / `2866:6171` | |
| `11r2_run_cube_per_state` | `2866:6174` | `research/glass/rt/images/11r2_run_cube_per_state.jpg` |

### Research index cards

| Stem | Slot | File |
|---|---|---|
| `office_round2` | `2864:6340` | `Frontrooms3D/Verification/kit_lookdev/office_round2.jpg` |
| `01_rep_L0` | `2864:6343` | `research/interaction_audit/images/01_rep_L0.png` |
| `g10r3_01_representative` | `2864:6346` | `research/glass/images/g10r3_01_representative.jpg` |
| `20_P1_v2_headon_centre` | `2864:6349` | `research/glass/rt/images/20_P1_v2_headon_centre.jpg` |
| `04_b_crack_graph_eye_centre` | `2864:6352` | `research/glass/destruction/images/04_b_crack_graph_eye_centre.png` |
| `es_b09_run_room` | `2864:6355` | `research/exit_sign/images/build/es_b09_run_room.jpg` |
| `es_b08_family_lineup` | `2864:6258` | `research/exit_sign/images/build/es_b08_family_lineup.jpg` |
| `20_duplex_0p3m_seed2554` | `2864:6358` | `research/outlets/images/20_duplex_0p3m_seed2554.jpg` |
| `db_A_L0F_burst_FINAL_V2_LA` | `2864:6361` | `research/door_break/images/db_A_L0F_burst_FINAL_V2_LA.jpg` |
| `q16_b_front` | `2864:6372` | `research/placard/images/q16_b_front.jpg` |
| `d_03_inspect_poses` | `2864:6375` | `research/inspect/images/d_03_inspect_poses.jpg` |

### Copied from other Figma sections (imageHash copy, no upload)

| Slot | Stem | Source |
|---|---|---|
| `2864:6278` | `fig-2312-852` | IP RESEARCH, `media:ir01_dsc00159` (`2320:2064`) |
| `2864:6281` | `fig-2324-852` | PROP KIT, `img:Kit_OfficeDesk_persp` (`2325:879`) |
| `2864:6284` | `fig-2349-852` | REAL vs MODEL, IBM PS/2 Model 30 photo (`2349:1036`) |
| `2864:6287` | `fig-2331-852` | HUNTER |
| `2864:6290` | `fig-2407-852` | WALLPAPER PRINT |
| `2864:6293` | `fig-2397-852` | 1990 FURNITURE MEDIA (T24 evacuation plan) |
| `2864:6296` | `fig-2831-6157` | LEVEL TRANSITIONS |
| `2864:6299` | `fig-2748-6099` | DOORS + WINDOWS |
| `2864:6310` | `fig-2441-3804` | RELAY PURSUIT |
| `2864:6313`, `2865:6218` | `fig-2471-3804`, `fig-gn_scandinavian_star_1990-04` | GLOW INK; the Commons photo `Research/week02/phosphor-narrative/stills/gn_scandinavian_star_1990-04.jpg` |
| `2864:6316` | `fig-2528-3900` | PATTERN CUES |
| `2864:6326` | `fig-2256-8` | UI MOCKUP |

### Media placeholders (waiting on Red's download approval; see `media_candidates.md`)

| Frame | Key | Candidate |
|---|---|---|
| `2865:6221` | `media:run_real_twin_head_unit` | A5 |
| `2865:6225` | `media:run_real_hallway_normal_vs_emergency` | A4 |
| `2865:6229` | `media:run_real_fire_door_magnet` | A9 |

When Red approves the batch, upload each file into its frame (or replace the frame with an `img:` slot of the same size) and add its row to `SOURCES.md`.

## Open points

1. **The House Lights floor probably shows no probe.** C's three box probes are sized exactly to the corridor's inner faces: 2.84 × 2.9 × 9.16 m, blend 0.25 (`harness/FrontRoomsRunLookdev_house_lights.cs.txt`, lines 999–1002). They are also re-baked and swapped inside one Unity run. Emergency Power found two things (VL195): URP gives a probe zero weight on its own box faces, and a batch run keeps the first probe texture it sees. So C's waxed floor most likely reflects the default cube. Its armed / tripped probe states are probably not real either. This was not re-measured. RV28 and RV29 say so. The fix is to grow the boxes 0.16 m past the shell and render one Unity run per rig state, as B does. Then re-render S1b and X2.
2. **Red Ward does not meet the 10-03 floor decision.** It uses one corridor cube per state with no box projection (D9), and it has no matte copy. RV26 and RV29 say so.
3. **The G14b ray-traced path** will add true and second reflections on the waxed floor later. RV29 says so, with the bench numbers.
4. **The room frames are from 7 Oct** (`ee5c9bb`). Main has moved once since then, to `bc0aa79` on 10-08 at 01:47. That commit added the placard, time-sliced dressing, and changes to the pile, Office kit and kit library. The shells, finishes and lamps are unchanged. The props in the Tall pile and Office frames (RV05, RV06, RV09) may now differ a little. They were not re-captured. The placard is not in RV11's frames.
5. **No VL row was added.** This pass made no new verification images. The Run! verification rows are VL166–168 (A), VL189–192 (C) and VL194–195 (B).
