# Level transitions · 30 · Figma section

Built 2026-10-07 by the Figma stage (run `wf_65f056a8-162`). **Rebuilt 2026-10-08 by the fix stage** after the critic review: 13 slides became 18, new renders over today's game were added, and every critic issue was checked (§5).

## Where

- File `0tCbAiVUlrPId3RWd9LRif` ("Undergoing Game Projects"), page `2099:76`.
- Section **FRONTROOMS · LEVEL TRANSITIONS**, id **`2831:6157`**, at **x 33937, y 19200**, now **8280 × 6360** (5 rows of 4).
  - The agreed spot (x 33937, y 2000) is taken by TOUCH CONTROLS (`2528:5403`). WALLPAPER PRINT (`2407:852`) and PATTERN CUES (`2528:3900`) sit below it in the same column, down to y 18435. So the section sits lower in its own column.
  - Before growing it from 5040 to 6360, I listed every section on the page (nested ones included) and every top-level node in x 33000–43500 below y 18000. Nothing lies below y 24240 in the section's column, so the new bottom (y 25560) overlaps nothing.
- Grammar copied from HUNTER (`2331:852`):
  - 1920 × 1080 white frames, 4 per row, 120 px apart.
  - The running header holds the meta (P1/Header/Meta, Plex Mono), now dated "Week 3 · Oct 8, 2026" and paged "NN / 18".
  - Titles use P1/Display/Title (Bayon 88/80) at y 139. Ledes use P1/Text/Body (Source Serif 4, 24/26) at x 1272, bottom on y 225. Statements use P1/Text/Statement (50/42) at y 969. Labels and chips use P1/Display/Label (Bayon 20).
  - Content sits from y 232 on the 12-col / 72 / 24 grid. Every text node is linked to a P1 style (0 unlinked segments). Faces used: Bayon, Source Serif 4 and IBM Plex Mono only. No Inter.
  - **No digit "1" in any Bayon text** (Bayon draws it like an "I"). The scan found one ("G10") and changed it to "Your glass audit sheet".
- Chips on images only point to evidence. No captions, no meta lines on cards, no source footers. Sources are in `SOURCES.md`.
- Slide layouts:
  - research slides: HUNTER's hero 876 × 657 plus four 426 × 316;
  - option slides: a hero of 1176 × 672 plus two 576 × 324;
  - comparison slides: six 576 × 324 in a 3 × 2 grid.

## Frames

| # | Frame id | Name | What it shows |
|---|---|---|---|
| 1 | `2856:6151` | LT00 · No transition | Red's G10 sheet; his frame as the game runs today; three cut types. Lede: a frameless crossing is in 32 % of chunks |
| 2 | `2856:6197` | LT01 · Never in the open | 2002 photos Dsc00161 and Dsc00159; Kane FF 0:48; Kane FF walk GIF; ETB Level 0 GIF |
| 3 | `2856:6243` | LT02 · How games join two looks | ETB noclip, POOLS, Kane Static Dead End, Backrooms 1998 (a boarded door) GIFs; A24 tape still |
| 4 | `2856:6289` | LT03 · It stops on something | Interiors 6/90 pp.125, 117 (uncropped), 79, 47; our-scale panels B (cased arch) and C (portal) at full resolution |
| 5 | `2856:6338` | LT04 · Frame | Hero: the new camera 3.4 m from the open edge, Frame over today's game; cased arch close-up (Oct 3); steel door frame over today's game |
| 6 | `2856:6378` | LT05 · Renovation | Shot 1 (chip "Every border dressed for this test"); the door from the Level 0 side; the arch reveal close up |
| 7 | `2856:6418` | LT06 · Neck | Shots 1–3 |
| 8 | `2856:6458` | LT07 · Drift | The patch option, shots 1, 4 ("The line at right becomes stripes") and 2; statement: a tier 4–5 effect |
| 9 | `2856:6498` | LT08 · Light lead | Border rule; soft rule; the game today (colour only) |
| 10 | `2856:6538` | LT09 · Close | New option: the map closes theme borders. The arch becomes a door; your frame; plan NOW vs CLOSED |
| 11 | `2856:6578` | LT10 · Your frame, six ways | Shot 4 (Oct 3 build): Before, Frame, Renovation, Neck, Drift, Light lead at 576 × 324 |
| 12 | `2856:6627` | LT11 · The arch, six ways | Shot 1, the same six |
| 13 | `2856:6676` | LT12 · My pick | The pick over today's game: shot 2 (portal + renovation), the dressed arch (shot 9), the framed open edge (shot 8). The lede states the pick |
| 14 | `2856:6716` | LT13 · Today, pick or closed | Today / pick / closed for your frame and the arch, all over today's game |
| 15 | `2856:6765` | LT14 · What each costs | Seven columns (Pick, Frame, Renovation, Neck, Drift, Light lead, Close): thumbnail, cost, needs |
| 16 | `2856:6845` | LT15 · You decide | Four questions at Statement size: the pick, the map rule, light, media. Two images (pick, closed) |
| 17 | `2856:6890` | LT16 · Backup · More questions | Who owns the joint, colliders (Neck, free posts), base, tiers, corner posts, paper scraps |
| 18 | `2856:6933` | LT17 · Backup · Media to approve | The 22 priority-1 rows (about 26 files, 60 MB), with sources and sizes, and the era note on the 2011 drywall photo |

Removed on 2026-10-08: the 13 frames of 10-07 (`2831:6158`, `2831:6182`, `2831:6208`, `2831:6249`, `2832:6157`, `2832:6188`, `2832:6219`, `2832:6250`, `2832:6281`, `2833:6157`, `2833:6188`, `2833:6240`, `2833:6277`) and their 7 grey `media:` placeholders.

## Slot → file map

Slots are rectangles named `img:<file stem>` (FILL), 69 in all, 52 unique images. Repeated stems share one image hash. `images/` = `Documentation/research/level_transitions/images/`.

| Stem | File | Slides |
|---|---|---|
| g10_01_representative | `Documentation/research/glass/images/g10_01_representative.jpg` | LT00 |
| pick_now_shot4, pick_now_shot1 | `images/pick_now_shot4.jpg`, `_shot1.jpg` | LT00, LT13; LT08, LT13 |
| cut_open_1, cut_seam_2, cut_arch_1 | `images/cut_*.jpg` | LT00 |
| ir01_dsc00161_20020612_082113, ir01_dsc00159_20020612_082017, ir04_kane_ff_level0 | `Research/week02/ip-research/stills/` | LT01 |
| ir02_kane_ff_walk, ir06_etb_l0_vhs | `Research/week02/ip-research/clips/*.gif` + posters (fills copied from IP RESEARCH `2320:2088`, `2320:2203`) | LT01 |
| ETB_1_noclip | `Research/week01/assets/clips/ETB_1_noclip.gif` + `.jpg` poster | LT02 |
| ir06_pools, ir06_backrooms1998 | `Research/week02/ip-research/clips/*.gif` + posters (`ir06_backrooms1998` from `2320:2209`) | LT02 |
| gn_kane_ep24_stretched_wallpaper | `Research/week02/phosphor-narrative/clips/` GIF + poster | LT02 |
| a24_trailer_0115_blue_painters_tape | `Research/week02/kit-references/a24/` | LT02 |
| rb_01…, rb_08…, rb_02…, rb_07… | `images/rb_*.jpg` | LT03 |
| rb_20b_arch_cased_opening, rb_20c_open_edge_portal | `images/` (new crops) | LT03 |
| pick_frame_shot8, pick_frame_shot3 | `images/` (new) | LT04, LT14; LT04 |
| v_frame_shot6 | `images/v_frame_shot6.jpg` | LT04 |
| v_renovation_shot1 (= var_renovation_shot1, same bytes), v_renovation_shot5, v_renovation_shot7 | `images/` | LT05 |
| var_neck_shot1..4 | `images/` | LT06, LT10, LT11, LT14 |
| v_drift_patch_shot1, _shot2, _shot4 | `images/` | LT07, LT10, LT11, LT14 |
| var_lightlead_shot1, var_lightlead_shot4 | `images/` | LT08, LT10, LT11, LT14 |
| v_lightlead_soft_shot1 | `images/` (new crop) | LT08 |
| close_shot1, close_shot4, close_plan_shot4 | `images/` (new) | LT09, LT13, LT14, LT15 |
| shot1_before, shot4_before | `images/` | LT10, LT11 |
| var_frame_shot1, _shot4, var_renovation_shot1, _shot4 | `images/` | LT10, LT11, LT14 |
| pick_mix_shot1, _shot2, _shot4, _shot8, _shot9 | `images/` (new) | LT12, LT13, LT14, LT15 |

**Uploads:** 20 new images with the Figma MCP `upload_assets` tool (the plan drawing twice: its first version was replaced), raw bytes, so every slot kept its `img:` name (checked: 0 renamed). The other slots reuse hashes already in the file. `pick_mix_shot8` and `pick_frame_shot8` are byte-identical (that crossing is not dressed), so they share a hash.

## Checks

- 18 frames; 69 slots, every one with an IMAGE fill. The GIF slots carry two fills (poster under the GIF), copied from slots that already worked; VIDEO fills were left out (the plugin API cannot set them).
- Nothing reaches below y 1020 except the statement, and nothing passes x 1848.
- Scan for jargon on the slides ("B0", "X 798", "luma", "MAP-n", "UV1", "LampModeOf", "E1", "V1–V5"): 0 hits.
- Fonts: Bayon, Source Serif 4 and IBM Plex Mono only; every text segment linked to a P1 style.
- Screenshots looked at: the whole section, LT00, LT03, LT09, LT12, LT14, LT15 and LT16. Fixed after the first look: the plan image (redrawn at 16:9 with its own labels, chip removed), chips over the two diagram panels (removed), LT14 widows, LT15 count text, the G10 chip.
- **Verification log:** VL201 (`2858:6157`, "Pick over today's game", FLAG) and VL202 (`2858:6173`, "Closed borders", WAIT-RED) in FRONTROOMS · VISUAL VERIFICATION LOG, row 50, no growth. The cover now reads 200 checks, 592 images, 24 tasks; the N1 legend reads `VL043–046, 137–139, 201–202 · 9 checks`. Rows recorded in `Documentation/VERIFICATION_LOG.md` §3.
- The renovation row queued in `W/recovery/PENDING_VL.md` item 5 is still queued for the final sweep.

## Critic issues: verdict and fix

| # | Issue | Verdict | What changed |
|---|---|---|---|
| 1 | The pick has no picture; columns use different light | **Right** | Rendered the pick, Frame alone and today's game over main 22bb75f (`16_pick_and_close.md`). LT12, LT13 and LT14 show it. LT10 and LT11 say "Oct 3 build: old print and warm Office light" |
| 2 | Frame is invisible in Red's frame | **Right** (measured again: 1.1 % over today's main) | LT04's hero is a new camera 3.4 m from the open edge, over today's game; LT11 shows the arch large; LT13 has the matched pair. The slides say plainly that the pick is quiet in his frame |
| 3 | A whole option family is missing (map rule) | **Right** | Built and rendered "Close" in the clone (`-closeBorders`) with counts over 1,620 chunks: frameless crossings 1.26 → 0, doors 1.37 → 2.53. New slide LT09; question "Map rule" on LT15 |
| 4 | Drift reads as a glitch; chip overclaims | **Right** | Patch images; chip "The line at right becomes stripes"; statement "A tier 4–5 effect" |
| 5 | Before thumbnails too small and repeated | **Right** | Before only on LT00 and the two six-way slides; option slides are hero + two; light lead and "now" merged into LT08. The 7 × 4 grid became two 3 × 2 slides at 576 × 324 (not "2 shots, larger columns": seven columns cannot get wider) |
| 6 | The decision slide hides its meaning | **Right** | Split into LT12 (the pick, stated in the lede) and LT15 (four questions at Statement size); the rest moved to backup LT16 |
| 7 | Questions about unseen options; collider claim | **Right** | Soft frame on LT08; "one base in both zones" marked "not rendered" on LT16; "no new colliders" removed; free-post colliders on LT14 and LT16 |
| 8 | Bayon "1" reads as "I" | **Right** | No digits in titles; every Bayon text scanned |
| 9 | Grey boxes; the key evidence is missing | **Right** | Grey boxes removed; on-disk media enlarged; the list lives only on LT17. The key Kane FF#3 frame still needs Red's download yes |
| 10 | LT02 title does not match its media | **Right** | Retitled "How games join two looks"; added the Backrooms 1998 boarded door |
| 11 | Panels that show no change | **Right** | LT05 uses the door from Level 0 and the arch close-up; LT08 uses the soft rule and today's game |
| 12 | LT09 overclaims the corner fix | **Right** (codex audit `10_review_map-edits.md`, MAP-2) | The claim left the slides; LT16 says the fix holds on first build and a revisit can bring the flicker back |
| 13 | 56 % vs 32 % | **Right** (`03_cut_inventory.md`: frameless crossings in 32.0 % of chunks) | LT00 lede uses 32 % |
| 14 | LT12 text-only; 22 rows = 23 files | **Partly right** | Marked as backup. The file count is higher than the critic said: the USG System Folder row is 3–4 pages too, so about 26 files |
| 15 | LT05 shows the worst case | **Right** | Chip "Every border dressed for this test" |
| 16 | Neck "0 MB" leaves out +8 MB in memory | **Right** | LT14: "+8 MB textures in memory" |
| 17 | Jargon on slides | **Right** | Removed (0 hits in the scan) |
| 18 | SOURCES.md gaps | **Right** | Interiors rows now link the issue PDF (https://www.usmodernist.org/I/I-1990-06.pdf). No on-disk ledger has the ETB Early Access trailer URL; the row now says so and points to the Steam page |
| 19 | Small research images | **Right** | rb_20 replaced by two full-resolution panels; rb_08 shown whole (200 × 672, no crop) |
| 20 | Lede ends in a one-word line | **Moot** | Lede rewritten |
| 21 | "Now" images are soft crops | **Right** | Replaced by full 1920 renders from main |
| 22 | Era note for the 2011 drywall photo | **Right** | Added to LT17 and `media_candidates_buildings.md` |

## Not done here

- No downloads. No change to Red's project code. Writes in Red's project: this folder (reports, images, code copies, logs) and the two VL rows in `Documentation/VERIFICATION_LOG.md`.
- No other chat's Figma section was edited. In the verification log only VL201, VL202 and the cover's counts and N1 legend changed.
