# Room visuals: the Figma section

File `0tCbAiVUlrPId3RWd9LRif`, page `2099:76` ("Undergoing Game Projects").

| | |
|---|---|
| Section | **FRONTROOMS · ROOM VISUALS** `2862:6157` |
| Where it is | x 25257, y 2000, 8280 × 11240. It was built on 10-08 at x 42617; someone moved it later, and the VISUAL VERIFICATION LOG now stands at x 42617. Checked 10-14 16:3x: it overlaps no section. DOORS + WINDOWS `2748:6099` starts below it at y 13920, and HUD KEY `2532:4038` is to its right at x 33937. |
| Slides | **34 frames** (RV00–RV33), 1920 × 1080, in rows of 4, 2040 px apart in x and 1240 in y. RV33 (new on 10-14) sits at (2160, 10080) inside the section, so the section did not grow. |
| Grammar | Copied from HUNTER `2331:852`. The header holds the meta only: Plex Mono, `Week 4 · Oct 14, 2026`, page `n / 34`. Titles are Bayon at y 139 on the 12-col / 72 / 24 grid, and content starts at y 232. Text uses the file's P1 styles (`P1/Display/Title`, `P1/Display/Label`, `P1/Text/Body`, `P1/Text/Statement`, `P1/Header/Meta`); there is no Inter. Chips sit on the images. There are no captions under images, no source lines and no doc paths on cards; sources are in `SOURCES.md`. Statements sit at y 966, so they end at 1008, inside the 72 px bottom margin. |
| Images | 144 `img:<stem>` slots holding 113 distinct stems. Every slot has exactly one IMAGE fill (checked 10-14), except the two pillarboxed Kane stills (black SOLID under a FIT image, on purpose). |
| Placeholders | 3 `media:<key>` frames on RV24 (A4, A5, A9). They wait on Red's download approval and carry no file names. |

## Build history

1. **10-08, 01:42–01:51.** First build: 33 frames and 13 slots copied from other sections. The other 60 uploads also landed.
2. **10-08, 19:3x–19:4x.** The last 33 uploads, the duplicate-slot copies, and a screenshot fix round (the ↔ glyph, RV25, RV27, RV29). Facts were updated to main `bc0aa79`.
3. **10-14, 16:3x–17:3x (the fix stage, after the critic).** Everything below. Unity and Blender ran (compile 0 errors; Blender 4.3.0). Red's project was not opened in Unity.

## The 10-14 fix pass, by critic point

| # | Verdict | What changed in Figma |
|---|---|---|
| H1 floors | **Accepted, re-rendered** | A and C re-rendered with live box probes, one state per run (`21_run_fix_1014.md`, VL236). New frames on RV26, RV28 and RV30. RV29 was rebuilt as a 4 × 2 grid of floor crops (A, B glossy, C, B matte × armed, tripped), with "Per direction", "What we learned" and "In main · later" below. The G14b bench image (baked Arial labels) was removed |
| H2 index and inventory | **Accepted** | Docs only: dated delta tables in `01_inventory.md` (Δ1–Δ19) and `02_research_index.md` (Δ-A, Δ-B), and the new capture report `05_captures_1007_1014.md`. Figma: the new slide RV33 |
| H3 doors | **Accepted** | RV10 "Door": single-acting since 3 Oct, 95° to one fixed side in 0.55 s, against stops, latches |
| H4 Exit strip | **Accepted** | RV16: strip chip, text and KPI removed. The Props text lists the Lobby outlets and the wall EXIT sign, and says the coded strip is never built. KPI "EXIT signs 1". New band image of the wall sign over the far door (`es_i05_after_crop`, 620 × 319) |
| H5 EXIT merge | **Accepted, re-captured** | Title Run and Exit frames re-captured on main `0a9ee7f` (VL238). New frames on RV00, RV15, RV16, RV17 and RV22. RV15 gets kit-sign facts (LIT 70 / HALF 15 / LOOSE 15 %, red × level, dim letters). RV17 was rebuilt with two in-game frames (the Run kit sign, the Exit wall sign); the "MISSING" chip is gone. RV20's exit_sign card says "in main since 8 Oct". RV01's Exit-sign column is filled for Title Run and Title Exit |
| M1 chase wave | **Accepted** | RV31 "Why B": "once the game calls its built lamp API" |
| M2 lamp states | **Accepted** | RV07 new block "Outside the roll": mode 5 Off, module Off, Reduce flashing (0.7 hold, dead never blinks), Dip/Sag/Warn built but not called |
| M3 CRT ads | **Accepted** | RV06 Props: CRTs play WXRM screen ads since 4 Oct. RV33 card: screens/ and SCREENS `2800:6096` |
| M4 missing research | **Accepted** | New RV33 "Still more research": landing_audit, cue_driver, dress_hitch, codex_audit, screens / SCREENS 2800:6096, computer_ui, interactables kit in main (10-14), and "UI only, not room visuals" (2532:4038, 3113:6872, 2528:5403, 2889:6157, 2911:6716, 2920:6693) |
| M5 A is red fill | **Accepted (labelled)** | RV26 chip "Pre-render · red-fill control"; the Measured text states the conflict with RV23 and rule 1 and the +3.2 EV hack |
| M6 Relay size | **Accepted, re-measured** | RV26 / RV27 / RV28 c4 slots show the 2.05 m stand-in with its ratio. RV30's last column shows the 2.05 m Relay. RV31's table gives proxy / 2.05 m: A 6.3 / 3.0, B 8.1 / 7.7, C 4.4 / 9.4 (VL237) |
| M7 C's red flashers, plain lites | **Accepted (stated)** | RV28 Light: "red breaks rule 4"; Measured: "the lites are plain glass, not 1990 wired glass" |
| M8 no sprint pass | **Accepted (stated)** | RV31 bottom line: "Not yet checked in any: a 5.5 m/s sprint pass, reset, the stage-1 warning, a Relay pushing a door, WebGL" |
| M9 RV24 media | **Partly accepted** | File names and sizes were removed from the three placeholders (they live in `media_candidates.md`); each now says what it will show and that it waits on Red. Not replaced with on-disk frames: no on-disk photo shows a real 1990 emergency light, alarm or fire door, and Kane or A24 frames would contradict the slide's "real 1990" claim. `40_for_red.md` asks for A4, A5, A9 and A11 first |
| M10 units | **Accepted** | RV15 "0.18 sRGB"; Run! slides say "linear" (RV26–RV28, RV31 "Median Y (lin.)"); RV08 already says sRGB |
| M11 stale frames | **Accepted, re-captured** | Start, Office, Tall and column frames are pixel-identical on main `0a9ee7f` (0.0 % of pixels move), so those slides keep their images. RV00's lede now says 14 Oct. RV11: the placard hangs 15 m into the map, out of these frames |
| M12 merged EXIT kit in the directions | **Rejected for the pre-renders** | The sign is shared, so it does not change the pick, and the kit's LIT letters are still dim (fix1 not merged). RV31 "Borrow later": the merged EXIT kit over the goal in any direction |
| L1 baked labels | **Accepted** | RV17's `es_b08_family_lineup` and RV29's G14b bench were replaced or removed. The new card crops on RV33 exclude their sheets' labels |
| L2 meta lines | **Accepted** | Removed: RV32's "Full rows…" line, RV31's footnote (now the "Not yet checked" line), and "Docs: research/…" on RV18 and RV19. Source and timecode chips on RV22–RV24 now name the content only |
| L3 density | **Partly accepted** | RV16's empty band now holds the sign image. RV01 stays a matrix: a diagram is the right form for a feed table. RV32 stays text: it lists media that, by definition, is not on disk |
| L4 margins | **Accepted** | Every statement moved from y 969 to y 966. RV09's KPI "0.35 M" became "35 CM", so it stays inside the margin |
| L5 ledger | **Accepted** | `SOURCES.md`: the trailers' raw files died with the 10-05 scratchpad wipe (frames survive in `images/`). DD_3_chase is still without a URL: a 10-14 search found no "Enhanced Trailer"; it stays candidate C1 |
| L6 wallpaper names | **Accepted** | RV13 and RV16: "the WP03 print in the Lobby / cold palette" |
| L7 placard wording | **Accepted** | RV21: "its legend glows when its own cell's lamp goes dark" |
| L8 rule 5 | **Accepted (stated)** | RV31: "Rule 5 holds in none"; `40_for_red.md` proposes a shared goal treatment |
| L9 production gap | **Accepted** | RV29 "In main · later": probe blending and box projection off, no probe keywords in the Surface shader |
| L10 module rooms | **Rejected for this pass** | No capture seed frames a module room, and adding one needs a harness change. Listed as a follow-up in `40_for_red.md` and `05_captures_1007_1014.md` §2 |

## Frames

| # | Frame id | Name | What it shows (10-14) |
|---|---|---|---|
| 01 | `2862:6158` | RV00 · Every room, today | 15 room types, 14 Oct frames for Title Run and Exit |
| 02 | `2862:6240` | RV01 · What feeds each room | Matrix; the Exit-sign column is filled for Title Run and Exit |
| 03 | `2862:6249` | RV02 · Changed since 3 October | 3 Oct vs 7 Oct; statement adds the EXIT kit |
| 04–07 | `2862:6276`, `6303`, `6330`, `6357` | RV03–RV06 · Level 0 Standard, Low, Tall, Office | RV06 adds the CRT ads |
| 08 | `2863:6189` | RV07 · Five lamp moods | + "Outside the roll" |
| 09–12 | `2863:6226`, `6253`, `6286`, `6321` | RV08–RV11 | RV10 single-acting doors; RV11 placard placement |
| 13–17 | `2863:6356`, `6389`, `2864:6157`, `6186`, `6219` | RV12–RV16 · Title Lobby, Shift, Office, Run, Exit | RV15 and RV16 rebuilt facts |
| 18 | `2864:6250` | RV17 · The true Exit | Two in-game frames of the merged signs |
| 19–22 | `2864:6270`, `6302`, `6332`, `6364` | RV18–RV21 · Research | No doc paths; RV19 lists the VL236–238 rows |
| 23–26 | `2865:6157`, `6180`, `6207`, `6242` | RV22–RV25 · Run! research | Content chips; RV24 placeholders without file names |
| 27–29 | `2865:6271`, `6310`, `6349` | RV26–RV28 · A, B, C | Probe re-renders, 2.05 m Relay, unit labels |
| 30 | `2866:6157` | RV29 · A glossy floor | 4 × 2 floor-crop grid |
| 31 | `2866:6183` | RV30 · Side by side | Probe re-renders, 2.05 m Relay column |
| 32 | `2866:6219` | RV31 · My pick (Emergency Power) | Proxy / 2.05 m table, gaps line |
| 33 | `2866:6244` | RV32 · Media to approve | 23 candidates |
| 34 | `3122:7271` | **RV33 · Still more research** | 8 cards (new) |

## Slots changed on 10-14

`DOC` = `Frontrooms3D/Documentation/research/room_visuals/images/`. Every stem below is `DOC/<stem>.jpg`, and every one was uploaded once, with HTTP 200 and an imageHash, then copied to its duplicates.

| Stem | Slots |
|---|---|
| `room1014_title-run_wide` | `2862:6226`, `2864:6194`, `2865:6168` |
| `room1014_title-exit_wide` | `2862:6231`, `2864:6227` |
| `room1014_title-run_sign` | `2864:6197`, `3122:7241` |
| `room1014_title-exit_arrival` | `2864:6230` |
| `es_i05_after_crop` (the AFTER half of `research/exit_sign/images/integrate/es_i05_exit_sign_before_after.jpg`, label cut off) | `3122:7238` (RV16), `3122:7244` (RV17) |
| `run_dir_red_ward_{s1b,s1a,s2,c4k}_probes` | `2865:6279`+`2866:6199`, `2865:6284`+`2866:6196`, `2865:6287`+`2866:6191`, `2865:6293`+`2866:6202` |
| `run_dir_emergency_power_c4k` | `2865:6332`, `2866:6210` |
| `run_dir_house_lights_{s1b,s1a,s2}_probes` | `2865:6357`+`2866:6215`, `2865:6362`+`2866:6214`, `2865:6365`+`2866:6211` |
| `run_dir_house_lights_c4bk_probes`, `_c4k_probes` | `2865:6371`, `2866:6216` |
| `run_floor_{a,b,c,b_matte}_{s1a,s1b}` | `3122:7247`, `7250`, `7253`, `7256`, `7259`, `7262`, `7265`, `7268` (RV29, in that order) |
| `card_cue_driver`, `card_dress_hitch`, `card_crt_ad` (label-free crops of `research/cue_driver/images/cue_ingame_A_corridor_keys.jpg`, `research/dress_hitch/images/dh_merge_lookdev_before_after.jpg`, `research/codex_audit/images/fix_other_visual_v3_crt_crop.jpg`) | `3122:7282`, `3122:7285`, `3122:7291` (RV33) |
| removed | `img:es_b08_family_lineup` (RV17), `img:run_emergency_power_x_goal_matte`, `img:run_dir_emergency_power_x_goal`, `img:run_dir_house_lights_x2`, `img:11r2_run_cube_per_state` (RV29) |
| kept as `room1007_*` | every other room frame; the 14 Oct capture of each is pixel-identical (`05_captures_1007_1014.md`) |

Verification slides placed on 10-14 in VISUAL VERIFICATION LOG `2595:6093` (index rows in `Documentation/VERIFICATION_LOG.md`):

| VL | Frame | Check | Verdict | Image slot(s) |
|---|---|---|---|---|
| VL236 | `3124:7042` | Probes reach A and C floors | PASS | `3124:7052` `run_fix_v_probes_ac` |
| VL237 | `3124:7053` | Relay at its true 2.05 m size | PASS B, C / FAIL A | `3124:7063` `run_fix_v_relay_contract` |
| VL238 | `3124:7064` | Room frames, 7 vs 14 Oct | FINDING | `3124:7074` `room1014_v_changed_frames`, `3124:7075` `room1014_contact_sheet_all` |

The VL section was grown by one row, to 74,480, for these slides; the bounds check found nothing below it. The G14 stage then grew it to 75,720. The cover (`2597:6093`) count matches the section, 242 checks and 685 images, and the N2 legend now reads "VL039–042, 166–195, 236–238 · 16 checks".

## Slot → file, 8 Oct map (still valid except the rows changed above)

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

## Open points (10-14)

1. **Red's picks** wait on RV31 (questions 1–7) and RV32 (the media batch). `40_for_red.md` has the list.
2. **A fails the Relay bar at 2.05 m** (2.97 : 1). If Red still wants A, its signs' spot cones need to stop lighting the Relay's upper body. One way is lower sign spots with the Relay's head out of the cone. Not tried.
3. **The probe fix is clone-only.** Production needs probe blending and box projection in the URP asset, the probe keywords in `FrontRoomsSurface.shader`, and one probe set per rig state, toggled and never re-textured. These are visual-owned files and nothing was merged.
4. **Not rendered in any direction:** a 5.5 m/s sprint pass, reset, the stage-1 warning, a Relay pushing through a door pair, and the WebGL tier.
5. **The merged EXIT kit's LIT letters are dim** (0.59 × the ceiling; `exit_sign_fix1` `EmissionGain` is not merged). This shows in RV15 and RV17.
6. **RoomStream's dead Exit-strip branch** (`FrontRoomsRoomStream.cs:1513-1515`) is visual-owned. Delete it, or move the strip into the live branch, in the next RoomStream package.
7. **No module room is framed** (L10).
8. **DD_3_chase has no source URL** (L5; candidate C1).
