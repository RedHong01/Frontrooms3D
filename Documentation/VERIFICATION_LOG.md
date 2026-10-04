# Visual verification log (游戏视觉)

Red, 2026-10-03 ~18:3x: "I want every visual verification image you make in the future documented in Figma too."

This file holds two things:
1. **§1 The procedure.** Every visual-chat workflow follows it to add a verification slide to Figma.
2. **§3 The index.** One row per check, with the images, the verdict and the Figma slide once it exists.

The backlog collected on 2026-10-03 covers everything since 2026-10-02: 68 checks and 266 images. All rows start as `queued`; none are placed yet.

| What | Value |
|---|---|
| Figma file | `0tCbAiVUlrPId3RWd9LRif` ("Undergoing Game Projects") |
| Page | `2099:76` |
| Section | **FRONTROOMS · VISUAL VERIFICATION LOG**. Not on the canvas yet (read-only check of page 2099:76, 2026-10-03 ~18:4x). The first placing workflow creates it and writes its node id here: `section id: —` |
| Proposed origin | **x 77337, y 2000**: the next slot in the y 2000 row, right of HUD KEY · UI VARIATIONS (2532:4038, x 68657–76937), on the row's 8,680 pitch. Confirm with the bounds check in §1.4 first |
| Slide grammar source | FRONTROOMS · HUNTER · EARLY VISUAL PROPOSALS (`2331:852`), measured 2026-10-03 |
| `$SP` in paths | `/private/tmp/claude-501/-Users-redwang-Desktop-ArtCenter-Fall26T7-EGAM-401A-01-Individual-Game-Project/5656cffd-bc90-45f6-86a3-09b26549df8d/scratchpad` |
| `research/` in paths | `Frontrooms3D/Documentation/research/` |
| Next free VL number | **VL069** |

---

## 1. Procedure: adding a verification slide

### 1.1 What counts

A **verification image** is any image made to answer a visual question, for example:
- before/after pairs;
- contact sheets and review sheets;
- heatmaps, difference images and masks;
- test frames from a harness or from Play mode;
- in-engine and Blender look-dev checks;
- WIP model previews that someone reviews.

**One check = one question with one verdict = one slide.** Several runs of the same question get one slide each only when the verdict changed; otherwise the newest run replaces the older one.

**Not verification images:**
- research and reference media (photos, catalogue scans, trailer or game stills): they go in the research sections;
- raw per-frame captures already summarised by a sheet;
- iCloud conflict copies (`<name> 2.jpg`);
- HUD/vector masters.

### 1.2 Save the images where they last

- Save every image under `Frontrooms3D/Documentation/research/<area>/images/`. Use JPG q85 at ≤ 1920 px wide for frames and sheets. Use PNG for heatmaps, masks, UI and anything compared pixel for pixel.
- Clones and the scratchpad (`$SP`) are temporary. If an image only exists there, copy it into the area's `images/` folder before you log it, and log the copy's path.
- 29 backlog images in 10 rows (R2, F2, F5) still point at `$SP`. The placing workflow copies them to `research/verification_log/media/` first and updates the paths.
- Upload limit: 10 MB per image (`upload_assets`). Every backlog image fits; the largest is 7.8 MB.
- An image under 200 px (a HUD sprite) is upscaled by a whole number with nearest-neighbour before upload, and its chip says the factor (`×8`).

### 1.3 Pick the images and write the row first

- Choose **1–6 images** that answer the question. Prefer sheets and before|after pairs. Skip raw duplicates.
- Put the most telling image in the large slot.
- **Claim a number before touching Figma.** Re-read this file, then append the row to §3 with the next VL number, `Slide` = `—` and `Figma` = `queued`, and save. Then bump "Next free VL number" in the table above.
- Two workflows never share a number: the number fixes the slide's cell (§1.4), so a clash shows up as an occupied cell. If the cell is taken, stop, re-read this file and take the next number.
- Row fields:

| Field | Rule |
|---|---|
| VL | `VL<nnn>`, three digits, never reused |
| Date | the images' file time, `YYYY-MM-DD HH:MM` (a range if they span more than 2 min) |
| Task | the id from `Documentation/VISUAL_CHAT_TASKS.md` (G10, W1, N1, F2, …) |
| Check | the slide title: ≤ 30 characters, sentence case |
| Verdict | a verdict word from §1.6; a split verdict may name both parts (`PASS B / FAIL A`) |
| Question | the lede: one question, ≤ 100 characters |
| Statement | the bottom line: the answer with its deciding number, ≤ 64 characters, one line |
| Notes | the numbers behind the verdict, and the source report |
| Images | full paths, in slot order |

### 1.4 Where the slide goes

1. **Read the figma-use skill first** (`get_figma_skill skill://figma/figma-use/SKILL.md`), and load `figma-generate-design` before the first write. Pass both in `skillNames`.
2. **Find the section by name**, not by position: `FRONTROOMS · VISUAL VERIFICATION LOG` on page `2099:76`. Other chats sometimes move sections.
3. **If the section does not exist yet, create it** at the proposed origin. Before creating it, do the bounds check:
   - list every SECTION on the page **including nested ones** (absolute bounds);
   - include the agreed spots that are not built yet: ROOM VISUALS (x 42617, y 2000), DOOR BREAK · VISUAL PROPOSAL (x 59977, y 2000) and LEVEL TRANSITIONS. LEVEL TRANSITIONS was agreed at x 33937, y 2000, but TOUCH CONTROLS now stands there, so ask the level-transitions workflow where it went;
   - the new rectangle must clear all of them by ≥ 400 px. If it does not, step right by 8,680 until it does;
   - x 16577 below y 23865 belongs to 平面视觉;
   - then write the section id into the table at the top of this file.
4. **Section size:** width 8,280. Height = rows × 1,240 + 80, where rows = ceil(slides / 4).
   - Grow the height when a new row starts.
   - Before growing, re-check the bounds below the section.
5. **Slide cell from the number** (4 across, as in the Hunter section):
   - col = (n − 1) mod 4, row = floor((n − 1) / 4);
   - frame x = 120 + col × 2,040, y = 160 + row × 1,240 (section-relative).
   - Example: VL001 sits at (120, 160) and VL005 at (120, 1400).
6. **Never edit, move or restyle another chat's section or slide.** Only this section and its slides belong to the visual chat.

### 1.5 Slide grammar (from Hunter 2331:852; numbers in px)

- **Frame:** 1920 × 1080, white fill (#FFFFFF), clip content on, named `VL<nnn> · <task> · <check>` (for example `VL019 · G10 · The milky veil is gone`).
- **Grid:** 72 px side margins, content 1,776 wide. Four columns of 426 with a 24 gutter, at x 72 / 522 / 972 / 1422.
- **Text styles:** the file's P1 styles, applied by id through `setTextStyleIdAsync`. All text is #0A0A0A unless it sits on a chip. **No Inter, no SF Pro, no SushiGo styles.**

| Role | Style (id) | Face | Size / leading | Box |
|---|---|---|---|---|
| Running header | P1/Header/Meta (`S:e15151982bb3e6d485ad51045c6714f087f0c2eb,`) | IBM Plex Mono Regular | 13 / 16 | y 28, h 16 |
| Title | P1/Display/Title (`S:4cc59baf3230acd570aeedd2f3163c738d822402,`) | Bayon | 88 / 80, −1 % | x 72, y 139, one line, ends before x 1248 |
| Lede | P1/Text/Body (`S:60138e985390dcae06a002de377d05c5808fc81c,`) | Source Serif 4 | 24 / 26, −1 % | x 1272, y 173, w 576, ≤ 2 lines |
| Chip label | P1/Display/Label (`S:44581afa083a5f9b8da214be893def6dc2449af1,`) | Bayon, UPPER | 20 / 20, +3 % | chip padding 10 / 7, at image x + 16, y + 16 |
| Statement | P1/Text/Statement (`S:8ea64620976dfe88c39216b12a4fa8d0d71a53be,`) | Source Serif 4 | 50 / 42, −2 % | x 72, y 969, w 1776, one line |

- **Running header (the only place for meta):**

| x | Text |
|---|---|
| 72 | `Individual Game Project` |
| 522 | `FrontRooms · Verification log` |
| 972 | the meta line `<date> · <task> · <check> · <verdict>`, e.g. `Oct 3, 2026 · G10 · The milky veil is gone · PASS`. One line, ≤ 90 characters, ends before x 1698 |
| 1722 | `VL<nnn>` (in the page-number slot) |

- **Media band:** y 232–889 (657 high). The slots are plain rectangles, corner radius 0, named `img:<file stem>` (the file name without its extension; stems are unique in the backlog).

| Images | Slots (x, y, w × h) |
|---|---|
| 1 | (72, 232, 1776 × 657) |
| 2 | (72, 232, 876 × 657) · (972, 232, 876 × 657) |
| 3 | (72, 232, 876 × 657) · (972, 232, 876 × 316) · (972, 572, 876 × 317) |
| 4 | (72, 232, 876 × 316) · (972, 232, 876 × 316) · (72, 572, 876 × 317) · (972, 572, 876 × 317). For tall strips or portrait shots, use four columns of 426 × 657 instead (HR15) |
| 5 | (72, 232, 876 × 657) · (972, 232, 426 × 316) · (1422, 232, 426 × 316) · (972, 572, 426 × 317) · (1422, 572, 426 × 317) (HR04) |
| 6 | (72 / 522 / 972 / 1422, 232, 426 × 316) · (72, 572, 876 × 317) · (972, 572, 876 × 317) |

- **Scale mode:** `FIT` for sheets, pairs, heatmaps, charts, crops and anything whose edges are evidence. `FILL` only for a single frame whose crop keeps every bit of the evidence. **Never crop the evidence.**
- **Chips:** optional, 1–4 words, at the image's top-left corner (+16, +16).
  - Use them for what tells two images apart: `BEFORE`, `AFTER`, `1.5 M`, `RUN 3`, `×1.5`.
  - Dark chip (#0A0A0A, white text) on light images; yellow chip (#F4DF3B, #0A0A0A text) on dark images.
- **No captions under images and no source footers.** File stems live in the layer names; paths, numbers and sources live in this file.
- **Say it once.** The lede asks the question and the statement answers it with the deciding number. Neither repeats the title.

### 1.6 Verdict words (header and index)

| Word | Meaning |
|---|---|
| `PASS` / `FAIL` | Measured against a stated bar |
| `FAIL→FIXED` | The slide shows the failed state; a later row shows the fix |
| `FLAG` | Passes, but with a problem that someone owns |
| `RISK` | A known gap that can bite gameplay |
| `PARTIAL` | Part of it passes |
| `FINDING` / `BASELINE` | The state documented, with no gate |
| `PENDING` | Images made, no verdict yet |
| `WAIT-RED` | Red decides |
| `APPROVED` / `ACCEPTED` / `PICK …` / `MERGED` / `DONE` | Decisions and outcomes |
| `SUPERSEDED` | A later run replaces it |

When a later run changes the verdict, add a new row and a new slide. Do not rewrite the old slide.

### 1.7 Build, upload, check

1. **Build the frame in one `use_figma` call** at the cell from §1.4:
   - header, title, lede, statement and chips;
   - one empty slot rectangle per image, named `img:<stem>`;
   - return the frame id and the slot ids, in slot order.
2. **Upload** the images:
   - load the tool with ToolSearch `select:mcp__6e617164-c524-4dd6-b5b8-2cd2ded137b0__upload_assets`;
   - call it with `fileKey` `0tCbAiVUlrPId3RWd9LRif`, `currentPageId` `2099:76`, `count` = the number of images, `nodeIds` = the slot ids in the same order, and `scaleMode` per §1.5;
   - **POST every returned URL** before you call `upload_assets` again: `curl -sS -X POST -H 'Content-Type: image/jpeg' --data-binary @"<absolute path>" "<url>"` (use `image/png` for PNG);
   - at most 60 URLs per call;
   - one call per slide keeps a failed upload easy to redo.
3. **Check once:**
   - take one `screenshot()` of the frame;
   - confirm every slot holds its image, nothing is cropped that matters, and no text is clipped or overlapping;
   - if anything is wrong, fix it and take one more screenshot, then stop;
   - this check screenshot is a working file, not a verification image: do not log it.

### 1.8 Record it

- In §3, set `Slide` to the frame's node id (for example `2601:812`) and `Figma` to `placed <YYYY-MM-DD>`.
- If you changed the section's height or created the section, update the table at the top of this file.
- In the workflow's own report, give the VL number and node id next to each image it cites.
- **Every visual workflow ends with this procedure.** Its last stage logs each check it verified.

---

## 2. Backlog by task (collected 2026-10-03)

Sources searched:
- `research/*/images` (plus `glass/g11_bench`, `glass/destruction/images`, `glass/destruction/build/images` and `build/masks`, `glass/rt/images`, `interactables/window_landing/images`, `interactables/proposal/media` and `media/wip`, `ui_key_icon/images` and `ui_key_icon/design`, `outlets/images`, `room_visuals/images`, `level_transitions/images`, `interaction_audit/images`, `webgl`);
- `$SP/p0port/frames`, `$SP/proj/Verification/print_p0`, `$SP/p0_review`, `$SP/lens_pick`, `$SP/hud`.

| Task | What | Checks | Images | Source report |
|---|---|---|---|---|
| R1 | Interaction + render-quality audit: today's glass, doors, key, Relay break, representative frames (proj_audit, seed 4242) | 8 | 40 | `research/interaction_audit/05_in_engine_evidence.md` |
| D1 | Doors: gap evidence + prototypes A/B (R4), locked-door readability (R5), head-dip framing (R6), DW05 spec renders | 7 | 35 | `research/interactables/04_door_re8_gap.md`, `05_locked_door_type.md`, `proposal/02_figma.md` |
| G11 | Reflection feasibility: planar reflection bench | 1 | 5 | `research/glass/11_reflections_and_raytracing.md` §2.4 |
| G10 | Glass track G1–G4, G6 verification: runs 1–3 (proj_glass) | 9 | 30 | `research/glass/20_verification.md`, `30_final.md` |
| F2 | HUD fixes: crosshair dot, key glyph alpha | 2 | 3 | `Documentation/VISUAL_CHAT_TASKS.md` §0 F2 |
| F5 | Troffer lens scale pick (with the map chat) | 1 | 2 | `Documentation/VISUAL_CHAT_TASKS.md` §0 F5 |
| GD1 | Glass destruction research: today's crack, Blender crack-graph prototype | 2 | 6 | `research/glass/destruction/04_unity_implementation.md`, `10_glass_destruction_plan.md` |
| G14 | Metal RT glass: code review, runtime probe, the G1 hook proof | 7 | 23 | `research/glass/rt/01_code_review.md`, `02_runtime_probe.md`; hook: `research/glass/20_verification.md` §8.2 |
| G14b | Secondary reflections: research + M3 Max bench | 1 | 1 | `research/glass/rt/11b_multibounce_research_bench.md` |
| N2 | Room visuals: current-state captures of every room type | 4 | 17 | `research/room_visuals/04_captures.md` |
| N1 | Level transitions: cut inventory, fixed shots, V5 Light lead, B0 | 4 | 14 | `research/level_transitions/03_cut_inventory.md`, `15_var_lightlead.md`, `20_variation_lightlead.md` |
| R2 | Wallpaper P0 (paper/print split): gates, art sign-off, port | 7 | 24 | `$SP/print_p0_report.md` §4–7, §11; `VISUAL_CHAT_TASKS.md` §2 R2 |
| K1 | Key icon: silhouette test, today's panel, directions on game backgrounds | 3 | 13 | `research/ui_key_icon/01_research.md`, `research/ui_key_icon/design/` |
| W1 | Windows: G4 frame look-dev, window landing runs 1–2 (proj_win) | 7 | 34 | `research/interactables/window_landing/01_integration.md`, `02_tests_frames.md`, `research/interactables/build_g4_windows_frame_family_glass_track_interface_.md` |
| N3 | Power outlets: stage-1 planner contract probe | 1 | 1 | `research/outlets/10_spec.md` (contract probe) |
| GD3 | Staged glass breakage build: BEFORE set, lamp mirror check, determinism, shot prototype | 4 | 18 | `research/glass/destruction/build/01_setup.md`, `logs/08_shot_sway.tsv` |
| **All** | | **68** | **266** | |

**Not collected, and why:**
- **Research and reference media.** These are not verification images; they stay in the research sections: `level_transitions/images/rb_*`, `room_visuals/images/run_ref_*` and `run_research_*`, `outlets/images/01_canon_*`, and `interactables/proposal/media/kane_*` and `a24_*`.
- **Raw captures summarised by a sheet:**
  - GD3's per-frame `before_track_*` and `before_game_*` (about 1,000 JPG);
  - the per-view T1 renders in `print_p0` (only the views shown);
  - W1's single `tf_*_before` and `tf_*_after` frames (the `_pair` files carry both);
  - `room_visuals/images/room_*` beyond the sheets;
  - `level_transitions/images/cut_*` beyond the sheet and five representatives;
  - `v_lightlead_shot1–4`, which are byte-identical to `var_lightlead_shot1–4`.
- **K1 masters and composites:** `ui_key_icon/design/hud` (182), `design/png`, `design/svg` and `design/dropin`. 平面视觉 rebuilds the key-HUD variants natively in `2532:4038`; the K1 slides log only the legibility evidence.
- **iCloud conflict copies** (`* 2.jpg`, `* 2.png`) in `glass/destruction/build/images` and `build/masks`.
- **Outside the search scope, picked up when their workflows log them:**
  - **WebGL:** `research/webgl` has no images. The budget view frames are in `$SP/proj_web/Verification/webgl-budget/views`.
  - **DB1 door break:** `research/door_break/images` is empty while the workflow runs. Blender quick renders sit in `$SP/door_break/{A,B,C}/renders`.
  - **N2 Run! directions:** renders still pending.
  - **D1 G1–G3 build critic renders:** in the build clones' `out` folders.
  - **F1 and F5 ambient:** Red's screenshots, and the map chat's pixel-diff frames.
  - **Older autopilot and audit runs** in `$SP/audit_run*_frames`.

---

## 3. Index

`Slide` = the Figma frame's node id once placed. `Figma` = `queued` or `placed <date>`. Image paths are in slot order: the first image goes in the large slot.

| VL | Slide | Figma | Date | Task | Check | Verdict | Question (lede) | Statement | Notes | Images |
|---|---|---|---|---|---|---|---|---|---|---|
| VL001 | — | queued | 2026-10-02 20:59 | R1 | Level 0 window, hold, break | FAIL | Does the map's pane read as glass, and does breaking it show anything? | The pane is a milky veil, and breaking it just deletes it. | Milky teal veil with no reflection; the 1 s hold changes only the HUD bar; the pane is deleted in one frame with no shards, dust or kick. | `research/interaction_audit/images/02_window_L0_intact_1.5m.png`<br>`research/interaction_audit/images/04_window_L0_intact_oblique.png`<br>`research/interaction_audit/images/06_window_L0_intact_steep.png`<br>`research/interaction_audit/images/14_window_L0_hold_0.5s_hud.png`<br>`research/interaction_audit/images/16_window_L0_break_next_frame.png`<br>`research/interaction_audit/images/20_window_L0_after_1s_oblique.png` |
| VL002 | — | queued | 2026-10-02 20:59 | R1 | Office window reflections | FAIL | What does the pane reflect in an Office room? | Office panes reflect a default sky: flat cyan rectangles. | No probes and no skybox: the panes reflect Unity's default sky cube and read as flat pale-cyan rectangles at 50–60°. | `research/interaction_audit/images/21_window_Office_intact_1.5m.png`<br>`research/interaction_audit/images/23_window_Office_intact_oblique.png`<br>`research/interaction_audit/images/25_window_Office_intact_steep.png`<br>`research/interaction_audit/images/29_window_Office_break_next_frame.png` |
| VL003 | — | queued | 2026-10-02 20:59 | R1 | Probe and clear-glass tests | FAIL | Does a reflection probe or a clear-glass setting fix the pane? | A probe barely helps; clear glass alone turns invisible. | A probe barely changes the 50° view; clear glass plus a probe makes the pane invisible. The pane needs surface detail and a frame. | `research/interaction_audit/images/07_window_L0_EXPERIMENT_with_probe.png`<br>`research/interaction_audit/images/10_window_L0_EXPERIMENT_game_glass_probe_steep.png`<br>`research/interaction_audit/images/09_window_L0_EXPERIMENT_clear_glass_probe_steep.png`<br>`research/interaction_audit/images/11_window_L0_EXPERIMENT_clear_glass_probe_1.5m.png`<br>`research/interaction_audit/images/12_window_L0_EXPERIMENT_prop_glass.png` |
| VL004 | — | queued | 2026-10-02 20:59 | R1 | Free door vs key door opening | FAIL | Does unlocking with a key look different from opening a free door? | A key door opens frame for frame like a free door. | Frame for frame the same 0.55 s hinge swing; no handle, no lock, no camera move. | `research/interaction_audit/images/39_door_normal_before_hud.png`<br>`research/interaction_audit/images/41_door_normal_t0250ms.png`<br>`research/interaction_audit/images/43_door_normal_t0750ms.png`<br>`research/interaction_audit/images/58_door_key_before_hud.png`<br>`research/interaction_audit/images/60_door_key_open_t0250ms.png`<br>`research/interaction_audit/images/62_door_key_open_t0750ms.png` |
| VL005 | — | queued | 2026-10-02 20:59 | R1 | Locked door feedback | FAIL | What does the player see at a locked door? | A locked door looks and reacts like any other door. | It looks like every other door; pressing E changes nothing on screen (DoorLocked reaches only the sound layer). | `research/interaction_audit/images/48_door_locked_before_hud.png`<br>`research/interaction_audit/images/50_door_locked_after_use_hud.png`<br>`research/interaction_audit/images/51_door_locked_0.5s.png` |
| VL006 | — | queued | 2026-10-02 20:59 | R1 | Key on the floor and pickup | FAIL | Does the key read as a key, and is the pickup shown? | The key is a yellow brick that vanishes when you walk by. | A pale yellow brick floating in the room; it vanishes when the player walks within 0.9 m, with no prompt, reach or hand. | `research/interaction_audit/images/52_key_2m.png`<br>`research/interaction_audit/images/54_key_1.0m.png`<br>`research/interaction_audit/images/55_key_pickup_frame.png`<br>`research/interaction_audit/images/56_key_pickup_+2f_hud.png` |
| VL007 | — | queued | 2026-10-02 20:59 | R1 | The Relay breaks a door | FAIL | Is a door break visible to the player? | The Relay's door break is a door swinging open. | The leaf stays still for 2.5 s of blows; 'broken' is an intact leaf swinging open; the capsule rig floats and never touches the door. | `research/interaction_audit/images/66_relay_break_t0000ms.png`<br>`research/interaction_audit/images/71_relay_break_t1000ms_hud.png`<br>`research/interaction_audit/images/70_relay_break_witness_t1000ms.png`<br>`research/interaction_audit/images/75_relay_broken_+0000ms.png`<br>`research/interaction_audit/images/78_relay_broken_+0250ms.png`<br>`research/interaction_audit/images/80_relay_broken_+1000ms.png` |
| VL008 | — | queued | 2026-10-02 20:59 | R1 | Representative render frames | FINDING | Where does the low-end look come from? | The cheap look is content, not the render pipeline. | Content, not the pipeline: primitive keys, doors, panes and Relay, no probes or decals. Autopilot frames carry no post (36 vs 35), so never judge the look from them. | `research/interaction_audit/images/00_start_room_view.png`<br>`research/interaction_audit/images/01_rep_L0.png`<br>`research/interaction_audit/images/34_rep_Office.png`<br>`research/interaction_audit/images/35_rep_Office_b.png`<br>`research/interaction_audit/images/36_rep_Office_b_AUTOPILOT_STYLE_camera.png`<br>`research/interaction_audit/images/37_rep_Dark.png` |
| VL009 | — | queued | 2026-10-02 23:13 | D1 | Door gap today | FAIL | Can you see through a shut door at the frame? | Shut doors leak light through 10–20 mm slits. | Yes: 10 mm slits at both jambs and 20 mm at the head, no stop behind them; the far room shows as a lit line, with its troffer in it. | `research/interactables/images/door_gap_00_sideA_front_1.5m_game.jpg`<br>`research/interactables/images/door_gap_02_crop_hinge_reveal_2x.jpg`<br>`research/interactables/images/door_gap_06_crop_latch_slit_2x.jpg`<br>`research/interactables/images/door_gap_16_sideB_hinge_inline_darknear.jpg`<br>`research/interactables/images/door_gap_16_crop_hinge_slit_2x.jpg`<br>`research/interactables/images/door_gap_20_open_fromA_swing_side_hinge.jpg` |
| VL010 | — | queued | 2026-10-02 23:36–10-03 12:15 | D1 | Gap-free prototypes A and B | PASS | Do the prototype frames close the see-through line? | Both prototypes close the slit from both sides. | The lit line is gone at the hinge and latch jambs, from both sides, in option B (double-acting) and option A (single-acting with stops). | `research/interactables/images/door_gap_proto_cmp_hinge_inline_sideA.jpg`<br>`research/interactables/images/door_gap_proto_cmp_hinge_inline_sideB.jpg`<br>`research/interactables/images/door_gap_proto_cmp_latch_inline_sideA.jpg`<br>`research/interactables/images/door_gap_proto_cmp_latch_inline_sideB.jpg`<br>`research/interactables/proposal/media/ingame_door_gap2_12_today_sideB_hinge_inline.png`<br>`research/interactables/proposal/media/ingame_door_gap2_74_optionA_sideB_hinge_inline.png` |
| VL011 | — | queued | 2026-10-02 23:36 | D1 | Swing clipping, A vs B | PASS B / FAIL A | Does the leaf clip the frame while it swings? | B swings clean both ways; A needs a fixed swing side. | B is clean at 4, 14 and 95° both ways; A is clean from the stop side but passes through the stops when pushed from the wrong side, so A needs a fixed swing side (Red chose A). Long verdict: PASS (B) / FAIL (A, both-ways rule). | `research/interactables/images/door_gap_proto_swing_B_pushA_4deg.jpg`<br>`research/interactables/images/door_gap_proto_swing_B_pushA_14deg.jpg`<br>`research/interactables/images/door_gap_proto_swing_B_95deg.jpg`<br>`research/interactables/images/door_gap_proto_swing_A_pushA_14_95.jpg`<br>`research/interactables/images/door_gap_proto_swing_A_wrong_14deg.jpg`<br>`research/interactables/images/door_gap_proto_swing_A_95_right_vs_wrong.jpg` |
| VL012 | — | queued | 2026-10-02 23:38–10-03 12:15 | D1 | Light leak under the door | PASS | Is the bright patch at the threshold a gap? | The floor patch is a shadow leak; a 0.14 m proxy fixes it. | No: it is the far lamp's shadow leaking through the thin leaf; a 0.14 m shadows-only box in the leaf removes it. Light through walls is the unshadowed-lamp budget (audit F5, Q5). Long verdict: PASS (fix found). | `research/interactables/images/door_gap_shadow_leak_sideA.jpg`<br>`research/interactables/images/door_gap_shadow_leak_sideB.jpg`<br>`research/interactables/proposal/media/ingame_door_gap3_00_floor_leak.png`<br>`research/interactables/proposal/media/ingame_door_gap3_01_floor_shadowproxy.png` |
| VL013 | — | queued | 2026-10-02 23:25–23:28 | D1 | Locked door at 6 / 12 / 20 m | PASS | Can a key door be told from a free door from afar, in every light? | Almond steel reads at 20 m in every light; dark steel fails. | Almond steel leaf with a dark bronze frame reads +1.05 to +1.7 stops off the free door at 6-20 m in lit, dim and Office light. Hardware only (P1) fails past 6 m; dark steel (P3) fails in dim cells. Long verdict: PASS (REC). | `research/interactables/images/05_r1_sheet_L0lit.jpg`<br>`research/interactables/images/05_r1_sheet_L0dim.jpg`<br>`research/interactables/images/05_r2_sheet_Office.jpg`<br>`research/interactables/images/05_r3_sheet_recommended.jpg`<br>`research/interactables/images/05_L0dim_P3_bronze_20m.jpg`<br>`research/interactables/images/05_r3_close_L0lit_recommended.jpg` |
| VL014 | — | queued | 2026-10-02 23:28 | D1 | Head-dip key shot framing | PASS | Does the head-dip pose frame the keyway? | At pose P the keyway sits dead centre. | At pose P (0.57 m from the keyhole, about 38° down, FOV 62) keyway, knob and number plate sit in the frame centre; sign and kick plate stay out. | `research/interactables/images/05_r3_headdip_strip_L0lit.jpg`<br>`research/interactables/images/05_r3_headdip_strip_Office.jpg`<br>`research/interactables/images/05_r3_headdip_P_key_in_L0lit.jpg` |
| VL015 | — | queued | 2026-10-03 17:09 | D1 | Door set spec renders (DW05) | WAIT-RED | How do the four door sets read as models (Level 0 / Office, free / key)? | Four door sets modelled; Red confirms before they land. | Blender look-dev pre-renders for the DW proposal; Red confirms D1.5 before anything lands. | `research/interactables/proposal/media/wip/spec_l0f.png`<br>`research/interactables/proposal/media/wip/spec_l0k.png`<br>`research/interactables/proposal/media/wip/spec_off.png`<br>`research/interactables/proposal/media/wip/spec_ofk.png` |
| VL016 | — | queued | 2026-10-02 23:33 | G11 | Planar reflection bench | DONE | What does a planar reflection of the held pane cost? | A Relay-only planar costs no more than an empty camera. | A 12 m planar costs 12-20 % of a frame; a Relay-only overlay costs about an empty camera (2.5 ms) and is the pick for the hold. Long verdict: DONE (recommendation). | `research/glass/g11_bench/00_main_view.jpg`<br>`research/glass/g11_bench/01_planar_half_view_mirrored.jpg`<br>`research/glass/g11_bench/02_relay_only_planar_half_mirrored.jpg`<br>`research/glass/g11_bench/03_full_planar_with_relay_half_mirrored.jpg`<br>`research/glass/g11_bench/04_main_view_relay_behind.jpg` |
| VL017 | — | queued | 2026-10-03 00:00 | G10 | Run 1: first look | SUPERSEDED | How does the new glass compare with today's on first capture? | Run 1's pane was nearly invisible head-on. | Head-on the new pane was almost invisible (nothing to reflect in Level 0); replaced by runs 2 and 3. | `research/glass/images/01_reflection_proof.jpg`<br>`research/glass/images/02_window_old_vs_new.jpg`<br>`research/glass/images/05_props_old_vs_new.jpg` |
| VL018 | — | queued | 2026-10-03 10:22 | G10 | Run 2: values, props, grime | FAIL→FIXED | Do the run-2 values read as physical glass? | Run 2 reflected a third of real glass and lost the hutch. | Face-on reflectance was 2.4 %, a third of real glass; the hutch and cabinet glass read as wood; the jamb grime read as mould. Long verdict: FAIL (fixed in run 3). | `research/glass/images/g10_01_representative.jpg`<br>`research/glass/images/g10_02_window_L0.jpg`<br>`research/glass/images/g10_05_diagnostics_L0.jpg`<br>`research/glass/images/g10_07_props.jpg` |
| VL019 | — | queued | 2026-10-03 12:09 | G10 | The milky veil is gone | PASS | Does the new pane keep the room behind it clear? | The veil is gone: 2.7–4.2 /255 instead of 27–41. | The pane changes the view behind it by 2.7-4.2 /255, against 27-41 /255 for today's pane. | `research/glass/images/g10r3_02_window_L0.jpg`<br>`research/glass/images/g10r3_03_window_Office.jpg`<br>`research/glass/images/g10r3_04_window_crops_1to1.jpg` |
| VL020 | — | queued | 2026-10-03 12:09 | G10 | Physical reflectance at angle | PASS | Does the pane reflect like real 6 mm glass? | Reflectance tracks real 6 mm glass within 3 points. | 8 / 9.5 / 13.7 / 35.8 % at 0 / 50 / 60 / 75° against real 8.2 / 10.9 / 15.7 / 38.5 %; the near wallpaper shows across the pane at 50–61°. | `research/glass/images/g10r3_05_diagnostics_L0.jpg`<br>`research/glass/images/g10r3_05b_diagnostics_Office_deadlamp.jpg`<br>`research/glass/images/g10r3_full_window_L0_oblique50_AFTER.jpg`<br>`research/glass/images/g10r3_full_window_L0_steep_AFTER.jpg` |
| VL021 | — | queued | 2026-10-03 10:22–12:09 | G10 | Dead-lamp window | PASS | Does a window under a dead lamp reflect the lit room next door? | Under a dead lamp, the lit troffer next door reflects. | A lit troffer from the next cell reflects clearly in the upper pane. | `research/glass/images/g10r3_06_deadlamp_windows.jpg`<br>`research/glass/images/g10r3_full_39_dark_beyond_window_1.5m_AFTER.jpg`<br>`research/glass/images/g10_full_38_deadlamp_window_1.5m_A_before.jpg` |
| VL022 | — | queued | 2026-10-03 10:22–12:09 | G10 | Face-on readability | RISK | Can the player see the pane face-on in an evenly lit room? | Face-on, correct glass is nearly an empty opening. | Barely: a correct pane is close to an empty opening face-on. The frame and stops (W1) and RT (G14) have to carry it; Red tied the glass landing to W1. | `research/glass/images/g10r3_full_window_L0_1.5m_AFTER.jpg`<br>`research/glass/images/g10_full_window_L0_1.5m_N_nopane_after.jpg`<br>`research/glass/images/g10r3_full_window_L0_0.7m_AFTER.jpg`<br>`research/glass/images/g10_full_window_L0_0.7m_N_nopane_after.jpg` |
| VL023 | — | queued | 2026-10-03 10:22–12:09 | G10 | Prop glass on the new graph | PASS | Do the props still read as glass on the new graph? | The hutch and cabinet read as glass again. | The hutch doors and cabinet shelves read as glass again; the vending front is clearer; the bottle is unchanged. | `research/glass/images/g10r3_07_props.jpg`<br>`research/glass/images/g10r3_full_42_hutch_cabinet_STAGED_AFTER.jpg`<br>`research/glass/images/g10_full_42_hutch_cabinet_STAGED_C_after.jpg` |
| VL024 | — | queued | 2026-10-03 10:22–12:09 | G10 | Walls, floors, ambient | PASS | Do the zone cube and ApplyAmbient change the rest of the frame? | Walls and floors move less than 1.5 /255; nothing glows. | Walls and floors move 0.75-1.46 /255 (noise 0.37-1.12); nothing glows, no frozen print shows; ApplyAmbient is at the noise floor. | `research/glass/images/g10r3_01_representative.jpg`<br>`research/glass/images/g10r3_full_01_rep_L0_AFTER.jpg`<br>`research/glass/images/g10_full_01_rep_L0_A_before.jpg` |
| VL025 | — | queued | 2026-10-03 12:09 | G10 | Shards on carpet | PARTIAL | Do the shard materials read as glass on both carpets? | Clear shards read as glass; opaque ones go yellow in Office. | Glass_ShardClear reads as glass on both (0.96-0.98); the opaque Glass_Shard matches Level 0 (0.91) but shows pale yellow chips on Office carpet (1.27). | `research/glass/images/g10r3_08_shards.jpg`<br>`research/glass/images/g10r3_full_50_shards_1m_S1.jpg`<br>`research/glass/images/g10r3_full_51_shards_office_1m_S1.jpg` |
| VL026 | — | queued | 2026-10-03 10:06 | F2 | Crosshair: white square to dot | PASS | Is the white square gone from the crosshair? | The white square is now a plain white dot. | A plain white anti-aliased disc, 32 px drawn at 16 px (1080p), replaces the opaque 32 px square. Long verdict: PASS (DONE). | `$SP/hud/crosshair_old_vs_new.png` |
| VL027 | — | queued | 2026-10-03 00:06–17:22 | F2 | Key glyph white box | MERGED | Does the key glyph have real alpha? | The key glyph now has real alpha. | The original PNG is opaque white; the fixed copy has real alpha and merged with the 18:0x landing. | `$SP/hud/backup/HUD_KeyGlyph_original_white_box.png`<br>`$SP/hud/HUD_KeyGlyph.png` |
| VL028 | — | queued | 2026-10-03 11:06 | F5 | Troffer lens scale | PICK ×1.5 | Which lens scale keeps the far lens white without clipping the near one? | ×1.5 keeps the far lens white and the near lens textured. | At x1 the far lens reads grey; at x2.62 it clips flat white (6.1 % of the ceiling frame). The x1.5 confirmation gave a far-lens centre of 254.5. Long verdict: PICK x1.5 (LAND). | `$SP/lens_pick/map-ceiling_sheet.jpg`<br>`$SP/lens_pick/map-test-north_sheet.jpg` |
| VL029 | — | queued | 2026-10-03 00:00 | GD1 | Today's shader crack | FAIL | Does the current crack read as broken glass? | Today's crack is self-lit lines from one hub, then nothing. | It reads fake: even self-lit lines from one hub, free-floating rings, no depth or crater, and the pane just vanishes. | `research/glass/images/04_crack_palm_hooks.jpg`<br>`research/glass/images/03_window_close_masks.jpg` |
| VL030 | — | queued | 2026-10-03 10:18 | GD1 | Crack graph vs Voronoi | PASS | Does a radial + ring crack graph give believable pieces? | A crack graph gives 125–171 believable pieces. | 125-171 pieces with 25-36 teeth, 6.6k-9.0k tris, 7.5-35 ms in Python; tracks must not cross, long daggers need a secondary crack, holes must be caught. Long verdict: PASS (4 fixes listed). | `research/glass/destruction/images/04_a_plain_voronoi.png`<br>`research/glass/destruction/images/04_b_crack_graph_eye_centre.png`<br>`research/glass/destruction/images/04_c_crack_graph_eye_left.png`<br>`research/glass/destruction/images/04_d_crack_graph_low_right.png` |
| VL031 | — | queued | 2026-10-03 10:48 | G14 | Does the prototype draw? | FAIL→FIXED | Does ChatGPT's Metal RT prototype trace anything? | As shipped: 0 px. Fixed: within 1 px of the pane. | As shipped it finds 0 px of glass; with the layout, BLAS and tan(fov/2) fixes it matches the real pane within 1 px. Long verdict: FAIL as shipped / PASS fixed. | `research/glass/rt/images/01_review_trace_output.jpg` |
| VL032 | — | queued | 2026-10-03 12:03 | G14 | As-is behaviour and ablation | FAIL | What does the prototype paint with only compile fixes? | With compile fixes only, it paints glass on the walls. | 326,172 px of flat dark-blue 'glass' painted on walls and in the window, until the struct-layout and BLAS-index fixes. | `research/glass/rt/images/02_asis_untagged.jpg`<br>`research/glass/rt/images/02_ablation.jpg` |
| VL033 | — | queued | 2026-10-03 12:03 | G14 | Alignment, FOV, flip | FAIL (FOV) | Is the traced reflection where the pane is? | FOV in radians shrinks the reflection to 0.59; no flip. | FOV passed in radians shrinks it to 0.589 scale; fixed, overlap is 0.94-1.00. No vertical mirror (upright 0.997 vs flipped 0.328). Long verdict: FOV FAIL / flip PASS. | `research/glass/rt/images/02_v1_headon_level.jpg`<br>`research/glass/rt/images/02_alignment_v1_headon_level.jpg`<br>`research/glass/rt/images/02_v3_angle50.jpg`<br>`research/glass/rt/images/02_v4_steep65.jpg`<br>`research/glass/rt/images/02_alignment_v4_steep65.jpg`<br>`research/glass/rt/images/02_v5_close60.jpg` |
| VL034 | — | queued | 2026-10-03 12:03 | G14 | Composite and hit shading | FAIL | Does the trace look like the room behind the player? | The composite drops the grade; hits are flat; misses are sky. | Opaque composite after post (the grade is lost); hits are flat white and grey; 16-20 % of rays miss into a blue sky. | `research/glass/rt/images/02_v2_headon_centre.jpg`<br>`research/glass/rt/images/02_reference_vs_rt.jpg`<br>`research/glass/rt/images/02_hits_v2_headon_centre.jpg`<br>`research/glass/rt/images/02_hits_v4_steep65.jpg`<br>`research/glass/rt/images/02_hits_v2_radius18.jpg` |
| VL035 | — | queued | 2026-10-03 12:03 | G14 | Occluders and the Relay | FAIL | Does the trace respect things in front of and behind the pane? | Unregistered objects and the Relay get painted over. | An unregistered panel is 94 % painted over; a moving Relay leaves 177,035 stale px until the next rescan. | `research/glass/rt/images/02_unregistered_occluder.jpg`<br>`research/glass/rt/images/02_relay_behind.jpg`<br>`research/glass/rt/images/02_relay_occlusion.jpg` |
| VL036 | — | queued | 2026-10-03 12:03 | G14 | Streaming and rescan hitch | FAIL | Does the trace survive streaming and breaking? | Every 1 s rescan costs 45–96 ms and a blank frame. | The 1 s rescan costs 45-96 ms plus a blank frame; destroyed and edited meshes stay as ghosts until the rescan. | `research/glass/rt/images/02_stream_test.jpg`<br>`research/glass/rt/images/02_ghost_broken_pane.jpg`<br>`research/glass/rt/images/02_lag_test.jpg`<br>`research/glass/rt/images/02_free_run_chart.jpg`<br>`research/glass/rt/images/02_free_run_blank_frame.jpg` |
| VL037 | — | queued | 2026-10-03 12:10 | G14 | RT hook, identical when off | PASS | Does adding the RT hook change the glass when it is off? | With the keyword off, the glass is identical: 175 / 175. | Behind the _FR_GLASS_RT keyword: 175 / 175 checks identical, run twice. A plain uniform branch failed 49 / 90. | `research/glass/images/g14_hook_proof.jpg` |
| VL038 | — | queued | 2026-10-03 17:05 | G14b | Secondary reflections bench | DONE | What do extra bounces and wider receivers buy on the M3 Max? | Floor receivers, not extra bounces, make the visible change. | Two bounces are subtle (<= 10-16 /255), three never pay; floor receivers are the visible change (a third of the floor moves >= 8 /255) at 3.5 ms 1080p with mirror + blur. Long verdict: DONE (design input). | `research/glass/rt/images/11b_multibounce_sheet.jpg` |
| VL039 | — | queued | 2026-10-03 11:59 | N2 | Every room type today | BASELINE | What does every room type render like now? | 65 frames of every room type as the code renders today. | 65 frames from today's code: map zones, start rooms, the title stream; the title-to-map handoff is seamless. | `research/room_visuals/images/room_contact_sheet_all.jpg`<br>`research/room_visuals/images/room_contact_sheet_map.jpg`<br>`research/room_visuals/images/room_contact_sheet_title.jpg`<br>`research/room_visuals/images/room_start_doorway.jpg` |
| VL040 | — | queued | 2026-10-03 11:59 | N2 | The Run room today | FINDING | How does the Run! room look, and does the player ever see it? | The Run room is red, not dark, and no player reaches it. | It is red, not dark: two EXIT point lights light it; it has the highest black level (0.176); no player reaches it (lobby-only title). | `research/room_visuals/images/room_title-run_wide.jpg`<br>`research/room_visuals/images/room_title-run_arrival.jpg`<br>`research/room_visuals/images/room_title-run_ceiling.jpg`<br>`research/room_visuals/images/room_title-run_sign.jpg` |
| VL041 | — | queued | 2026-10-03 11:59 | N2 | Lamp faults and black level | FINDING | How much do lamp faults move the frame? | Lamp faults dim a corridor by 7–25 %, never to black. | Dead -23 %, failing -7 %, stutter -25 %; neighbours keep the corridor lit, and blacks are lifted in 59 of 65 frames. | `research/room_visuals/images/room_map-dead-lamp_wide.jpg`<br>`research/room_visuals/images/room_map-stutter-lamp_on.jpg`<br>`research/room_visuals/images/room_map-stutter-lamp_off.jpg`<br>`research/room_visuals/images/room_map-failing-lamp_high.jpg`<br>`research/room_visuals/images/room_map-failing-lamp_dropout.jpg` |
| VL042 | — | queued | 2026-10-03 11:59 | N2 | Ceilings across themes | FINDING | Do the ceilings tell the zones apart? | Every ceiling wears the same lens and light. | No: every troffer wears the same frosted lens and the same warm-white light, so Lobby, Shift, Office and Exit ceilings read the same. | `research/room_visuals/images/room_title-lobby_ceiling.jpg`<br>`research/room_visuals/images/room_title-shift_ceiling.jpg`<br>`research/room_visuals/images/room_title-office_ceiling.jpg`<br>`research/room_visuals/images/room_title-exit_ceiling.jpg` |
| VL043 | — | queued | 2026-10-03 11:37–11:54 | N1 | Cut inventory | FAIL | How does the game change looks between zones today? | Every zone change is a hard cut on a 3 m line. | Every change cuts on a 3 m cell line with no transition piece; worst are open edges, flat wall seams, split arches and corner-post z-fight. Long verdict: FAIL (baseline). | `research/level_transitions/images/cut_contact_sheet.jpg`<br>`research/level_transitions/images/cut_open_1.jpg`<br>`research/level_transitions/images/cut_seam_1.jpg`<br>`research/level_transitions/images/cut_arch_1.jpg`<br>`research/level_transitions/images/cut_post_1.jpg`<br>`research/level_transitions/images/cut_wallend_1.jpg` |
| VL044 | — | queued | 2026-10-03 11:53 | N1 | Four fixed comparison shots | PASS | Are the BEFORE shots reproducible for every variation? | The four BEFORE shots repeat byte for byte. | A full run reproduces the four BEFORE frames byte for byte (render all four in one call; subsets shift the grain). | `research/level_transitions/images/shots_before_sheet.jpg`<br>`research/level_transitions/images/cut_plan_1.jpg` |
| VL045 | — | queued | 2026-10-03 17:17 | N1 | V5 Light lead | WAIT-RED | Does putting the light change on the border sell the cut? | Light lead hides the cut in shadow; it does not fix it. | Level 0 border walls drop 39-64 % in luma and the Office beyond reads 1.6-2.2x brighter and cool. It hides the cut, it does not fix it. Long verdict: PASS as briefed (WAIT-RED). | `research/level_transitions/images/v_lightlead_vs_before.jpg`<br>`research/level_transitions/images/var_lightlead_sheet.jpg`<br>`research/level_transitions/images/v_lightlead_shot5.jpg`<br>`research/level_transitions/images/v_lightlead_shot6.jpg`<br>`research/level_transitions/images/var_lightlead_close_arch.jpg` |
| VL046 | — | queued | 2026-10-03 17:17 | N1 | B0 corner posts | PASS | Is the corner-post z-fight stripe gone? | The corner-post stripe is gone. | No two pieces of different finish overlap; the grey stripe on the yellow corner in shot 4 is gone. | `research/level_transitions/images/var_lightlead_close_post.jpg` |
| VL047 | — | queued | 2026-10-03 12:01 | R2 | T1a pipeline parity (gate) | PASS | Does the paper/print split render like today's wallpaper? | Split vs today: ≤ 0.51 levels in every view. | Pooled 0.39 / 0.34 / 0.35 levels over 3 materials x 4 views; every view <= 0.51 (gate 1 /255). | `$SP/proj/Verification/print_p0/T1_Lobby_front2m_ref_nohue.png`<br>`$SP/proj/Verification/print_p0/T1_Lobby_front2m_split_todayNS.png`<br>`$SP/proj/Verification/print_p0/T1_Lobby_front2m_heat_T1a.png`<br>`$SP/proj/Verification/print_p0/T1_Shift_oblique_heat_T1a.png`<br>`$SP/proj/Verification/print_p0/T1_Exit_close05_heat_T1a.png`<br>`$SP/proj/Verification/print_p0/T1_Exit_far_heat_T1a.png` |
| VL048 | — | queued | 2026-10-03 12:02 | R2 | T1a corridor, fog on / off | PASS | Does parity hold down a 25 m corridor? | Down a 25 m corridor, parity holds with fog on or off. | Pooled 0.34 / 0.30 / 0.31 levels; fog on stays within 0.04 of fog off. | `$SP/proj/Verification/print_p0/T1_Lobby_corridor_heat_T1a_fogOn.png`<br>`$SP/proj/Verification/print_p0/T1_Lobby_corridor_heat_T1a_fogOff.png`<br>`$SP/proj/Verification/print_p0/T1_Exit_corridor_heat_T1a_fogOff.png`<br>`$SP/proj/Verification/print_p0/T1_Lobby_corridor_split_todayNS_fogOn.png` |
| VL049 | — | queued | 2026-10-03 12:01 | R2 | T1b hue loss (reported) | ACCEPTED | How far does the new paper move from today's tinted wallpaper? | The pink/slate cast goes neutral: about 2 levels. | About 2 levels: the slight pink/slate cast goes neutral, by design. | `$SP/proj/Verification/print_p0/T1_Lobby_front2m_today_legacy.png`<br>`$SP/proj/Verification/print_p0/T1_Lobby_front2m_new_paperNS.png`<br>`$SP/proj/Verification/print_p0/T1_Lobby_front2m_heat_T1b.png`<br>`$SP/proj/Verification/print_p0/T1_Exit_front2m_heat_T1b.png` |
| VL050 | — | queued | 2026-10-03 12:02 | R2 | T2 lighting independence | PASS | Does the print ever change the lighting? | The print never touches specular: bit-identical. | Specular is bit-identical for print A vs B, static and live, at 12 light positions; only the diffuse changes. | `$SP/proj/Verification/print_p0/T2_light_sweep_sheet.png` |
| VL051 | — | queued | 2026-10-03 12:02 | R2 | Live print path | PASS | Does the live (array) print match the static one? | Live and static print match to 0.035 levels. | Mean 0.035 levels (p99 0.42); the only difference is mip generation. | `$SP/proj/Verification/print_p0/live_static.png`<br>`$SP/proj/Verification/print_p0/live_live_frame0.png`<br>`$SP/proj/Verification/print_p0/live_live_per_roll_phase.png`<br>`$SP/proj/Verification/print_p0/live_live_blend_0.5.png` |
| VL052 | — | queued | 2026-10-03 12:02–12:10 | R2 | Paper relief, raking light | APPROVED | Does the new linen relief read right up close? | Linen relief replaces the chevron relief; Red approved 0.60. | The legacy relief traced the chevron; the new paper shows an even linen-pebble relief under the same ink. Red approved the merge at 13:3x. Long verdict: APPROVED (Red, emboss 0.60). | `$SP/proj/Verification/print_p0/raking_sheet_legacy_vs_new.png`<br>`$SP/p0_review/p0_raking_legacy_left_new_right.jpg`<br>`$SP/p0_review/p0_corridor_today_vs_new.jpg` |
| VL053 | — | queued | 2026-10-03 12:46 | R2 | Port into today's project | MERGED | Does the port apply cleanly on today's files with the approved look? | Ported onto today's files and merged, manifest OK. | Before/after in the port clone show only the approved change; merged 2026-10-03 18:0x, 49 paths, manifest OK. Long verdict: PASS (MERGED). | `$SP/p0port/frames/pair_Lobby_corridor.jpg`<br>`$SP/p0port/frames/pair_Lobby_front2m.jpg` |
| VL054 | — | queued | 2026-10-03 12:26 | K1 | Silhouette test at HUD sizes | PASS | Which key drawing survives at 20-48 px? | Filled silhouettes survive to 20 px; outlines close up. | Filled silhouettes hold down to 20 px; the ring outline closes up at 20-24 px. Long verdict: PASS (filled). | `research/ui_key_icon/images/01_silhouette_sizes.png`<br>`research/ui_key_icon/images/02_silhouette_at_40x22.png` |
| VL055 | — | queued | 2026-10-03 12:26 | K1 | Today's key panel vs the model | FAIL | Does today's HUD key match the key in the world? | Today's HUD shows a white box around a generic key. | A white 40 x 22 box around a generic ring key; our key has a paddle bow, six cuts and a bevelled tip; the label is fixed to LEVEL 0 KEY. | `research/ui_key_icon/images/03_runtime_key_panel_x4.png`<br>`research/ui_key_icon/images/04_figma_ui05_key_panel_x3.png`<br>`research/ui_key_icon/images/05_model_kit_key_zone_side.jpg`<br>`research/ui_key_icon/images/07_model_kit_key_zone_34.jpg`<br>`research/ui_key_icon/images/06_model_key_tags_front.jpg` |
| VL056 | — | queued | 2026-10-03 17:03–17:16 | K1 | Four game backgrounds | WAIT-RED | Which direction reads on lit, dark, Office and wallpaper frames? | Recommended: A, the cut key traced from the model. | A, the cut key traced from the model, in one paper colour; B's lockup for 2x moments; C not used. Long verdict: WAIT-RED (recommend A). | `research/ui_key_icon/design/10_recommendation.png`<br>`research/ui_key_icon/design/05_A_chip.png`<br>`research/ui_key_icon/design/07_B_hud.png`<br>`research/ui_key_icon/design/09_C_hud.png`<br>`research/ui_key_icon/design/hud/A_L_fullframe_dark.jpg`<br>`research/ui_key_icon/design/hud/A_L_fullframe_office.jpg` |
| VL057 | — | queued | 2026-10-03 12:35 | W1 | Landing run 1, today vs kit | SUPERSEDED | Do kit frames land on the map without changing gameplay? | Gameplay unchanged; run 1 frames were lit too dark. | 38,538 / 0 edit checks and 17 / 17 in Play; these frames were too dark (lamp-eye lookup bug), re-shot in run 2. Long verdict: PASS (superseded frames). | `research/interactables/window_landing/images/L0_today_A_oblique_1.2m.jpg`<br>`research/interactables/window_landing/images/L0_kit_A_oblique_1.2m.jpg`<br>`research/interactables/window_landing/images/OF_today_A_oblique_1.2m.jpg`<br>`research/interactables/window_landing/images/OF_kit_A_oblique_1.2m.jpg` |
| VL058 | — | queued | 2026-10-03 17:09–17:13 | W1 | G4 frames, Blender look-dev | PASS | Do the final W-L0 and W-OF frames hold up at 1.5 m and 0.3 m? | Final frames pass every clear-zone and overlap check. | Second-pass checks: 0 clear-zone vertices, 0 escaping rays, 0 slab overlaps on LOD0 and LOD1; triangles within 15 % of the spec. | `research/interactables/proposal/media/wip/Kit_WindowFrame_Wood_faceA_1p5m.png`<br>`research/interactables/proposal/media/wip/Kit_WindowFrame_Steel_faceA_1p5m.png`<br>`research/interactables/proposal/media/wip/Kit_WindowFrame_Wood_stoolA_0p3m.png`<br>`research/interactables/proposal/media/wip/Kit_WindowFrame_Steel_screws_0p3m.png` |
| VL059 | — | queued | 2026-10-03 17:06 | W1 | Both faces, before and after | PASS | Does the window read as a built window from both sides? | Both faces now read as a built window. | W-L0: walnut casing, stool with nosing, apron, stops, 6 mm glass. W-OF: dark-bronze pressed-steel sleeve and channel stop. | `research/interactables/window_landing/images/tf_L0_A_front_1.5m_pair.jpg`<br>`research/interactables/window_landing/images/tf_OF_A_front_1.5m_pair.jpg`<br>`research/interactables/window_landing/images/tf_L0_B_front_1.5m_pair.jpg`<br>`research/interactables/window_landing/images/tf_OF_B_front_1.5m_pair.jpg`<br>`research/interactables/window_landing/images/tf_L0_A_45deg_2.4m_pair.jpg`<br>`research/interactables/window_landing/images/tf_OF_A_45deg_2.4m_pair.jpg` |
| VL060 | — | queued | 2026-10-03 17:06 | W1 | Close-ups at 0.3 m | PASS | Do stop, stool, horn and screws hold up close? | Holds up at 0.3 m; steel screws need light to read. | Geometry holds; the W-OF screw heads are legible only with the inspection light or a contrast stretch. Long verdict: PASS (note). | `research/interactables/window_landing/images/tf_L0_A_close_sill_0.3m_insp_pair.jpg`<br>`research/interactables/window_landing/images/tf_L0_A_close_horn_0.3m_insp_pair.jpg`<br>`research/interactables/window_landing/images/tf_L0_A_close_stop_0.3m_insp_pair.jpg`<br>`research/interactables/window_landing/images/tf_OF_A_close_corner_0.3m_insp_pair.jpg`<br>`research/interactables/window_landing/images/tf_OF_A_close_screws_0.3m_insp_pair.jpg`<br>`research/interactables/window_landing/images/tf_OF_A_close_screws_0.3m_insp_crop_autocontrast.jpg` |
| VL061 | — | queued | 2026-10-03 17:06 | W1 | Tall hall and dead lamp | FLAG | Does the frame still read from the tall hall and under a dead lamp? | Dark bronze under a dead lamp melts into the trims. | W-OF's dark bronze under a dead lamp reads close to today's dark trims. | `research/interactables/window_landing/images/tf_L0_hall_tall_5.0m_pair.jpg`<br>`research/interactables/window_landing/images/tf_OF_hall_tall_5.0m_pair.jpg`<br>`research/interactables/window_landing/images/tf_L0_deadlamp_A_front_2m_pair.jpg`<br>`research/interactables/window_landing/images/tf_OF_deadlamp_A_front_2m_pair.jpg` |
| VL062 | — | queued | 2026-10-03 17:06 | W1 | Broken state, intact vs broken | FLAG | Is the broken window clear, and can intact glass be told from broken? | Broken opens clean, but intact glass is hard to tell apart. | The opening is clear and the frame stays; but at 1.5 m intact vs broken differs by only 5.5 / 6.0 luma (today's milky cube: 13.1 / 10.6). | `research/interactables/window_landing/images/tf_L0_broken_A_45deg_2.4m_pair.jpg`<br>`research/interactables/window_landing/images/tf_OF_broken_B_front_1.5m_pair.jpg`<br>`research/interactables/window_landing/images/tf_L0_broken_A_close_sill_0.3m_insp_pair.jpg`<br>`research/interactables/window_landing/images/tf_OF_broken_A_close_sill_0.3m_insp_pair.jpg` |
| VL063 | — | queued | 2026-10-03 17:06 | W1 | In the real game (Play mode) | PASS | Does the framed window work in the game itself? | In the real game it breaks, stays framed and climbs: 17 / 17. | 17 / 17: the aim ray hits the pane, Hold breaks it, the frame stays, the climb carries the player through. | `research/interactables/window_landing/images/tf_play_L0_play_intact.jpg`<br>`research/interactables/window_landing/images/tf_play_L0_play_broken.jpg`<br>`research/interactables/window_landing/images/tf_play_L0_play_climbing.jpg`<br>`research/interactables/window_landing/images/tf_play_OF_play_intact.jpg`<br>`research/interactables/window_landing/images/tf_play_OF_play_broken.jpg`<br>`research/interactables/window_landing/images/tf_play_OF_play_climbing.jpg` |
| VL064 | — | queued | 2026-10-03 12:52 | N3 | Stage-1 outlet planner probe | PASS | Does the planner place outlets only on real wall faces, by the rules? | Outlets land only on real faces: 0 misses in 20 seeds. | 20 seeds: 62,914 faces with 0 missing or extra; 17,164 / 17,164 fixtures on solid shell; 0 rule violations; rebuilds identical. Long verdict: PASS (timing to re-measure). | `research/outlets/images/10_plan_seed2554.jpg` |
| VL065 | — | queued | 2026-10-03 12:47–12:49 | GD3 | BEFORE vs checklist V1–V10 | FAIL | How does today's breakage score on V1-V10? | Today's break fails 8 of 10 real-glass checks. | V1-V6, V8 and V10 fail on both subjects; the clear zone is empty but there are no teeth (V9). This is the bar the build must clear. Long verdict: FAIL (baseline). | `research/glass/destruction/build/images/before_overview_track_WL0.jpg`<br>`research/glass/destruction/build/images/before_overview_track_WOF.jpg`<br>`research/glass/destruction/build/images/before_sheet_track_WL0_H1.jpg`<br>`research/glass/destruction/build/images/before_sheet_track_WOF_H1.jpg`<br>`research/glass/destruction/build/images/before_overview_game_WL0.jpg`<br>`research/glass/destruction/build/images/before_sheet_game_WL0_H1.jpg` |
| VL066 | — | queued | 2026-10-03 12:49 | GD3 | C2 lamp mirror check | FAIL | Does today's glass show the ceiling lamp a mirror would show? | Today's glass shows no lamp where a mirror shows one. | A perfect mirror puts luma 202 at the lamp point (W-L0); track glass adds +11 and the game pane +36. No lamp image. | `research/glass/destruction/build/images/before_track_WL0_H1_f000_t0.00_C2.jpg`<br>`research/glass/destruction/build/images/before_game_WL0_H1_f000_t0.00_C2.jpg`<br>`research/glass/destruction/build/images/before_track_WL0_H1_f000_t0.00_C2mirror.jpg`<br>`research/glass/destruction/build/images/before_track_WOF_H1_f000_t0.00_C2.jpg`<br>`research/glass/destruction/build/images/before_track_WOF_H1_f000_t0.00_C2mirror.jpg` |
| VL067 | — | queued | 2026-10-03 12:47–12:50 | GD3 | Harness determinism | PASS | Is the GlassBreakLab exact enough for pixel gates? | The lab repeats 640 / 640 images exactly. | 640 / 640 metric images identical across runs; only a map z-fight band (0.86 % of pixels, <= 25 /255) differs across rebuilds, and it is masked. | `research/glass/destruction/build/masks/zfight_before_track_WL0_C3.png`<br>`research/glass/destruction/build/masks/zfight_before_track_WL0_C4.png`<br>`research/glass/destruction/build/images/before_track_WL0_H1_f181_rebuilt_C3.jpg` |
| VL068 | — | queued | 2026-10-03 17:27 | GD3 | Break shot D1 / D2 | PENDING | Does the break shot move the camera without a slow sway? | Shot sheets made; the step-8 verdict is still pending. | Sheets and curves exist; the step-8 report is not written yet. The sway log shows no sustained swing below 1 Hz. | `research/glass/destruction/build/images/08_shot_sheet_D1.jpg`<br>`research/glass/destruction/build/images/08_shot_sheet_D2.jpg`<br>`research/glass/destruction/build/images/08_shot_curves_D1.jpg`<br>`research/glass/destruction/build/images/08_shot_curves_D2.jpg` |
