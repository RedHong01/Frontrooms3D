# Media candidates: inspect / close-up research

Date: 2026-10-07. **Nothing here has been downloaded.** Red approves the batch; the visual chat asks once.

Used by `02_game_research.md`. Each row says what it proves.

## How the rows were checked

- **Steam screenshots (§A).** Viewed as thumbnails in the browser pane, from Steam's public `appdetails` JSON. The direct URL is the 1920×1080 file.
- **Steam trailer times (§B).** Checked by stepping the official Steam trailer in the browser pane in 0.05–1 s steps (hls.js, nothing saved). Times are ±0.05 s where a 0.05 s step was used, otherwise ±0.5 s.
- **YouTube rows (§C).** URL and title checked; times are **UNVERIFIED** unless a time is given.

## Where approved files go

- Home: `Research/week03/inspect-research/{stills,clips}/` with its own `SOURCES.md` (URL, time range, owner, licence, fetch date), as in `Research/week02/ip-research/`.
- Clips follow the ip-research pipeline: H.264 mp4 without audio, a 560 px GIF at 12 fps, and a `_poster.jpg`.
  - For the Steam HLS rows: `ffmpeg -nostdin -ss START -t DUR -i "<m3u8 URL>" -an -c:v libx264 …`. This fetches only the segments needed.
  - Raw files stay in the session scratchpad and are deleted after cutting.
- Licence: © the publisher, shown on each row. Short excerpts for course research only. Never ship them in the game.

---

## A. Steam store screenshots (official, 1920×1080)

The files are about 0.3–0.6 MB each (ESTIMATE). Owner: the game's publisher. Licence: © publisher, store media.

| P | Proposed file | Game · Steam app · screenshot # | Direct URL | What it proves |
|---|---|---|---|---|
| A | `ins_firewatch_map_compass.jpg` | Firewatch · 383870 · #4 | https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/383870/ss_4b9d67ae2af0da570d03731d93b095d0203b973d.1920x1080.jpg | Paper map and compass held in both hands, in the live view. No overlay |
| A | `ins_gonehome_held_paper_1.jpg` | Gone Home · 232430 · #1 | https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/232430/ss_c0864dd9e03481aa1359fccc4cd9ceb0c9abdc66.1920x1080.jpg | A paper ("THE COLISEUM") brought to the camera; the room stays visible and unblurred |
| A | `ins_gonehome_held_paper_3.jpg` | Gone Home · 232430 · #3 | https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/232430/ss_dfd1fda95f56da364e2dc655374318ea00c564f5.1920x1080.jpg | Same grammar: a zine at the camera, about 1/3 of the frame |
| B | `ins_gonehome_held_paper_7.jpg` | Gone Home · 232430 · #7 | https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/232430/ss_33ca03b69812cef8993eea5fac6f904618d5d30b.1920x1080.jpg | A handwritten flyer: handwriting read on the object, no transcript |
| A | `ins_bunker_inventory_live.jpg` | Amnesia: The Bunker · 1944430 · #3 | https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1944430/ss_8c58ab8d9364ba43b2ca9991d430dd927479a795.1920x1080.jpg | The inventory is a panel on the right; the hand and the room stay on the left |
| A | `ins_bunker_wall_map.jpg` | Amnesia: The Bunker · 1944430 · #4 | https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1944430/ss_53781c56e0213c9651fb4d8bc6c2e52bfc16134b.1920x1080.jpg | A large map pinned on a wall above a desk with a clock. A wall map as a place, not a menu (which room, and whether it opens a close-up: UNVERIFIED) |
| A | `ins_signalis_crt_inventory.jpg` | SIGNALIS · 1262350 · #2 | https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1262350/ss_dc87789ecfa2f83dd58ac778866fbf7fdc1ec99a.1920x1080.jpg | The inventory drawn as a CRT device, with USE / COMBINE / INSPECT: a period-styled menu |
| A | `ins_signalis_drawer_closeup.jpg` | SIGNALIS · 1262350 · #9 | https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1262350/ss_5338061a9fa31755789e0a7698fadc2d4ab29b94.1920x1080.jpg | First-person close-up of an open drawer (pistol, document): the camera goes to the object |
| B | `ins_signalis_panel_closeup.jpg` | SIGNALIS · 1262350 · #8 | https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1262350/ss_c7fcc30c5e2cd2ddf44bc01b2d43964abba72076.1920x1080.jpg | First-person close-up of a power panel (120 V / 230 V readouts) |
| B | `ins_alien_tracker.jpg` | Alien: Isolation · 214490 · #3 | https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/214490/ss_90419f0e549d54138abfeb3c7b78e4b5427afdb0.1920x1080.jpg | Motion tracker raised in the hand, in the live view (desktop version of our on-disk iOS still) |
| C | `ins_alien_console.jpg` | Alien: Isolation · 214490 · #1 | https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/214490/ss_e514869415e3bd913be3f6c0e419cea9f0be17a3.1920x1080.jpg | A console with screens in the world. Not the terminal close-up itself: §C asks for that |
| B | `ins_exit8_poster_camera.jpg` | The Exit 8 · 2653790 · #3 | https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/2653790/ss_aab252acfcd45c572e37b7d97c988e5fe9488e0a.1920x1080.jpg | "Security camera in operation" poster, square-on, read in the world |
| C | `ins_exit8_posters_close.jpg` | The Exit 8 · 2653790 · #4 | https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/2653790/ss_d9230054f31e82f937746e80c2ded84b5aae4c85.1920x1080.jpg | Two posters close, no UI |
| C | `ins_soma_omnitool.jpg` | SOMA · 282140 · #1 | https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/282140/ss_5ee5231b847ec7397554af8b2efe25caeef5d7c5.1920x1080.jpg | The omnitool held in hand, live |
| C | `ins_re7_papers_table.jpg` | Resident Evil 7 · 418370 · #5 | https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/418370/ss_26cee827684c0b0ba39764add124c764979022d3.1920x1080.jpg | Readable papers lying in the world ("The Dulvey Daily"). Environment, not the examine screen |
| C | `ins_visage_held_item.jpg` | Visage · 594330 · #9 | https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/594330/ss_900fc1b3abc5526a9f638b94f0532a00ea211700.1920x1080.jpg | An item (crowbar) held in hand with button prompts along the bottom |
| C | `ins_edithfinch_comic.jpg` | What Remains of Edith Finch · 501300 · #4 | https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/501300/ss_7a68ff5a7b3e6c85ab89f9bbf549a0232281bd19.1920x1080.jpg | A story told inside a framed comic page: the readable becomes the scene |

Store screenshots checked and **not** useful for inspect: RE Village (all 10), RE2 (10), RE4 (17), SILENT HILL 2 (10), Dead Space (10), CONTROL (8), Outlast (11), Amnesia: The Dark Descent (6). They are cinematic or combat shots.

## B. Steam official trailers (clip ranges measured 2026-10-07)

Size per clip: raw HLS segments about 2–6 MB, kept mp4 + GIF + poster about 3–6 MB (ESTIMATE). Owner: the publisher. Licence: © publisher, short excerpt for research.

| P | Proposed file | Trailer (Steam movie id) · m3u8 URL | Range / poster | What it proves |
|---|---|---|---|---|
| A | `ins_bunker_note_to_journal.{mp4,gif}` + poster 262.0 | Amnesia: The Bunker – Gameplay Demo (256948231) · https://video.akamai.steamstatic.com/store_trailers/1944430/570228/3612c77639ecdec9b31ae2439f907ee8ee419322/1750837072/hls_264_master.m3u8 | 256.0–275.0 s | Note pinned on a wall. Hand reaches at 258.6 s; the journal panel is fully open by 258.8 s (no fade). The parchment page fills the right half. The hand stays in frame **frozen** 258.8–273 s (Normal mode pauses). It closes with a cut at 273–274 s |
| A | `ins_bunker_no_pause_inventory.{mp4,gif}` + poster 59.0 | Amnesia: The Bunker – Halloween Update Trailer (256978123) · https://video.akamai.steamstatic.com/store_trailers/1944430/622444/226e6d10f4b817273640440456c43792434c70c5/1750742111/hls_264_master.m3u8 | 57.5–62.5 s | Shell Shock: the inventory panel pops in at 58.10–58.15 s (≤ 0.05 s). The trailer card reads "NO PAUSING IN INVENTORY" (60.5–62 s) |
| A | `ins_gonehome_door_note_lift.{mp4,gif}` + poster 22.3 | Gone Home – Launch Trailer (2029688) · https://video.akamai.steamstatic.com/store_trailers/232430/14286/b3ac770001804f8b2ce47a5a412f90a3f07aa25f/1750495838/hls_264_master.m3u8 | 20.5–23.5 s | Note taped to the front door. Lift to the camera 21.90–22.15 s (≈ 0.25 s); it settles by ≈ 22.30 s. The door glass stays visible behind |
| A | `ins_sh2_wall_photo_closeup.{mp4,gif}` + poster 371.0 | SILENT HILL 2 – SH2_Gameplay (257026897) · https://video.akamai.steamstatic.com/store_trailers/2124490/706859/6bdebdc71cd66f548bf088b899863e672ad79b78/1750844635/hls_264_master.m3u8 | 368.0–375.0 s | Framed photo on a wall. Third-person walk-up, then a **hard cut** (370.5→370.6 s) to a square-on fixed close-up lit by the flashlight, with small prompts bottom-right. Hard cut out at 373.4→373.5 s; the gameplay camera re-settles over ≈ 0.8 s. The closest match to Red's request |
| B | `ins_sh2_drain_closeup.{mp4,gif}` + poster 618.5 | same | 616.0–621.0 s | Top-down puzzle close-up of a floor drain, with a cursor |
| B | `ins_signalis_panel_closeup.{mp4,gif}` | SIGNALIS – Gameplay Overview (256910985) · https://video.akamai.steamstatic.com/store_trailers/1262350/504958/d9becccc291a7986cadfd7863426ea3e35d64330/1750657163/hls_264_master.m3u8 | 19.5–23.5 s | First-person close-up of a power panel. Trailer edit, so the timing is not game timing |
| B | `ins_signalis_3d_documents.{mp4,gif}` | same | 56.5–60.5 s | Documents shown as 3D paper turning on black (object brought to the camera, world hidden) |
| C | `ins_deadspace_world_hologram.{mp4,gif}` | Dead Space – Extended Gameplay Walkthrough (256911793) · https://video.akamai.steamstatic.com/store_trailers/1693980/506378/20a5a15db4b08f2be24d972a293661982df3af65/1750725969/hls_264_master.m3u8 | 336.0–344.0 s | A red text hologram in the world, read while Isaac moves and the steam vents run. The RIG menu itself is not found in this trailer yet |

## C. Still needed (no verified source time yet)

| P | What | Where to look | Status |
|---|---|---|---|
| A | RE Village: key item "item get" view and the Maiden ring examine (rotate until the eye faces you) | GameSpot, "Maiden - Full Resident Evil Village Demo (4K)", https://www.youtube.com/watch?v=fYbRaLxahxc | **Age-restricted on YouTube: needs Red's sign-in.** Time UNVERIFIED |
| A | RE7: examine in the live inventory, and a file page | an official Capcom gameplay video; none checked yet | UNVERIFIED |
| A | Alien: Isolation: terminal access (camera to the screen, cursor) | an official gameplay video; the Steam E3 trailer (2032883) was not checked | UNVERIFIED |
| B | RE2 remake: wall map board → map screen | Capcom gameplay video; none checked | UNVERIFIED |
| B | Amnesia: The Dark Descent: note pickup → note view | none checked; the Steam trailer (901419) is probably cinematic | UNVERIFIED |
| B | Control: document pickup → "read" view with redactions | Remedy gameplay video; "What is Control" (256795957) not checked | UNVERIFIED |
| B | Firewatch: walking with the map up (motion) | Campo Santo trailers on Steam ("Trowel", "The Shale Slide") not checked | UNVERIFIED |
| B | P.T.: R3 zoom on the "HELLO" wall text and the bathroom zoom | a full playthrough upload; P.T. is delisted, so no official source | UNVERIFIED |
| C | Outlast: blue "CONFIDENTIAL" document pickup → notes screen | Red Barrels trailer (2029178) not checked | UNVERIFIED |
| C | Silent Hill 2 (2001): paper map with James's red marks; a memo screen | the PS2 manual PDF (silenthillmemories.net) for stills; gameplay upload for motion | UNVERIFIED |
