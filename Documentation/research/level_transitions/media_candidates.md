# Media candidates · Backrooms IPs (for `01_ip_research.md`)

Date: 2026-10-03. **Nothing here has been downloaded.** Red approves the batch; the visual chat asks once (together with `media_candidates_buildings.md`).
Every time below was checked by eye in the YouTube player today (±1 s). The research file says what each moment proves.

## Already on disk (no download needed)

Reuse these first; credits are in the existing ledgers. Full list: `01_ip_research.md` §10.
- 2002 originals: `Research/week02/ip-research/stills/ir01_dsc00161_*.jpg` (three papers, one carpet), `ir01_dsc00159_*.jpg` (arcade).
- Kane FF Level 0 baseline, A24 trailer stills (store, corridor, troffers, showroom), ETB / Backrooms 1998 / POOLS clips: `Research/week02/ip-research/`.
- Kane *Static Dead End* stretched wallpaper and *FF#2* green cracks, Exit 8 clips: `Research/week02/phosphor-narrative/clips/`.
- ETB noclip, Dark Deception exit portal, Exit 8 loop: `Research/week01/assets/clips/`.
- Our own cuts: `images/cut_*.jpg`, `images/shot*_before.jpg` (this folder).

## How to fetch (when approved)

- YouTube: download **only the section**, not the whole video: `yt-dlp --download-sections "*START-END" -f "bv*[height<=1080]" --extractor-args youtube:player_client=web_embedded <URL>` (the `web_embedded` client gives HD; see the ip-research memory). Raw sections stay in the session scratchpad and are deleted after cutting.
- Clips: same pipeline as `ip-research`: H.264 mp4 without audio, GIF 560 px at 12 fps, `_poster.jpg` at the poster time. Run ffmpeg with `-nostdin`.
- Stills: one frame at the given time, JPG q85, max 1920 px wide.
- Proposed home: `Research/week02/level-transitions/{stills,clips}/` with its own `SOURCES.md` (URL, time range, uploader, licence). Copies for this doc go to `images/ip_*.jpg`.
- Licence for all YouTube items: © the channel and, for walkthroughs, the game developer. Short excerpts for course research only, as in `ip-research/`. Never ship them in the game.

Sizes: "raw" is the scratchpad download (deleted after). "Kept" is what stays in `Research/` (clip = mp4 + GIF + poster).

## A. Kane Pixels (priority 1 = needed for the Figma IP slide)

| P | Proposed file | Video (page URL) | Range / time | Raw | Kept | What it proves |
|---|---|---|---|---:|---:|---|
| 1 | `lt_kane_ff3_neck.{mp4,gif}` + `_poster.jpg` (poster 34:25.0) | *Found Footage #3*, https://www.youtube.com/watch?v=acdYs9tPLko | 34:18–34:30 | ≈4 MB | ≈5 MB | **The key frame.** Yellow room → deep recess → doorway whose jambs are white; yellow stops at the outside corner; black base and carpet continue; brighter cooler light beyond (P2, P3, P4) |
| 1 | `lt_kane_ff3_white_office.jpg` | same | 35:04 | ≈0.5 MB | 0.2 MB | the white open-plan office reached through that neck: Kane's own "Office" zone |
| 1 | `lt_kane_ff_firedoor.{mp4,gif}` + poster (6:57) | *The Backrooms (Found Footage)*, https://www.youtube.com/watch?v=H4dGpz6cnHo | 6:40–6:58 | ≈5 MB | ≈6 MB | "Fire exit / Keep clear" sign, concrete stair, push-bar door opens straight into Level 0 (P1 door cut) |
| 1 | `lt_kane_ff_green_pocket.{mp4,gif}` + poster (2:15) | same | 2:08–2:20 | ≈3 MB | ≈5 MB | same paper and carpet, green light: light as the zone code (P6) |
| 1 | `lt_kane_ff_ladder_hole.jpg` | same | 2:02 | ≈0.5 MB | 0.2 MB | ladder into a square hole in a Level 0 wall (P8) |
| 1 | `lt_kane_pit_fall_lower.{mp4,gif}` + poster (6:10) | *Backrooms – Pitfalls*, https://www.youtube.com/watch?v=0XwlWXtpaCM | 5:36–6:14 (cut to 6 s around 5:40 and a still at 6:10) | ≈8 MB | ≈5 MB | fall into a pit; the lower level is the same architecture in grey; the hole seen from below (P8, P5) |
| 1 | `lt_kane_pit_outpost.jpg` | same | 1:52 | ≈0.5 MB | 0.2 MB | Async control room built inside Level 0 (P10 built insert) |
| 1 | `lt_kane_sde_door_light.{mp4,gif}` + poster (2:12) | *Backrooms – Static Dead End*, https://www.youtube.com/watch?v=ZbPaWvqAEq4 | 1:56–2:14 | ≈4 MB | ≈5 MB | an ordinary wooden door; behind it the same yellow under green-dim light (P1 + P6) |
| 1 | `lt_kane_info_dark_pocket.{mp4,gif}` + poster (5:50) | *Backrooms – Informational Video*, https://www.youtube.com/watch?v=ZIFhglHn3W0 | 5:08–5:14 (clip) and 5:50 (poster) | ≈3 MB | ≈5 MB | flashlight finds foreign wallpaper and a house front in the dark: edges never seen (P7) |
| 1 | `lt_kane_info_teal.jpg`, `lt_kane_info_red.jpg` | same | 7:10 and 7:24 | ≈1 MB | 0.4 MB | teal hall and red room: light colour as zone code (P6) |
| 2 | `lt_kane_ff_fall_dark.{mp4,gif}` + poster (4:48) | *Found Footage*, H4dGpz6cnHo | 4:32–4:54 | ≈5 MB | ≈5 MB | static → fall → dark space with a far lit stair (P7 + P9) |
| 2 | `lt_kane_ff_stair.jpg`, `lt_kane_ff_white_rooms.jpg` | same | 5:00 and 5:40 | ≈1 MB | 0.4 MB | stairs lead to a new look (P8) |
| 2 | `lt_kane_pit_tunnel_mouth.jpg` | *Pitfalls*, 0XwlWXtpaCM | 8:40 | ≈0.5 MB | 0.2 MB | round tunnel mouth frames the red street (P9) |
| 2 | `lt_kane_pit_house_corridor.{mp4,gif}` + poster (11:00) | same | 10:42–11:16 (cut to 6 s) | ≈7 MB | ≈5 MB | a house doorway opens straight into a narrow white corridor (P1) |
| 2 | `lt_kane_ff3_hole.jpg` | *FF#3*, acdYs9tPLko | 6:40 | ≈0.5 MB | 0.2 MB | the hole in the basement block wall (way in) |
| 2 | `lt_kane_ff3_skywalk.jpg` | same | 30:30 | ≈0.5 MB | 0.2 MB | skywalk windows onto the red city (P9) |
| 2 | `lt_kane_emg_cavity.{mp4,gif}` + poster (9:44) | *Backrooms – Everything Must Go*, https://www.youtube.com/watch?v=ewZx0bnBb30 | 9:32–9:50 | ≈4 MB | ≈5 MB | squeezing through the stud cavity behind the yellow drywall (P10) |
| 2 | `lt_kane_emg_arch_signs.jpg` | same | 2:32 | ≈0.5 MB | 0.2 MB | Level 0 with sale signs and an arch to stairs (P8, P9) |
| 2 | `lt_kane_ff2_atrium.jpg` | *Found Footage #2*, https://www.youtube.com/watch?v=sA5PxGHqpTo | 6:48 | ≈0.5 MB | 0.2 MB | atrium with missing, stained ceiling tiles (P5) |
| 2 | `lt_kane_ff2_pool.jpg` | same | 12:02 | ≈0.5 MB | 0.2 MB | tiled pool with table lamps (pool look in Kane) |

## B. A24 film

| P | Proposed file | Source (page URL) | Range / time | Raw | Kept | What it proves |
|---|---|---|---|---:|---:|---|
| 1 | `lt_a24_wall_black.{mp4,gif}` + poster (0:19) | A24, *Backrooms* Official Trailer, https://www.youtube.com/watch?v=0HjdiohVOik | 0:12–0:24 | ≈4 MB | ≈5 MB | hand on the basement wall → black → Level 0 (P11) |
| 2 | `lt_a24_dezeen_set_*.jpg` (1–2 images) | Dezeen interview, https://www.dezeen.com/2026/05/29/backrooms-production-design-danny-vermette-interview/ | set photos in the article (pick the portal / shared wall if shown) | ≈0.5 MB | 0.5 MB | the portal built into a shared wall (P10); © A24 / Dezeen, research quote |

## C. Games (walkthrough uploads; © uploader and developer)

| P | Proposed file | Video (page URL) | Range / time | Raw | Kept | What it proves |
|---|---|---|---|---:|---:|---|
| 1 | `lt_etb_door_preview.{mp4,gif}` + poster (22:35) | iStudioGamer, *Escape the Backrooms – Full Game Walkthrough*, https://www.youtube.com/watch?v=-1VNyWyyk6I | 22:30–22:42 | ≈5 MB (4K source; take 1080p) | ≈5 MB | the next level is seen through an open door, then a black load (P9 + P11) |
| 1 | `lt_etb_vent_sign.jpg` | same | 6:25 | ≈0.5 MB | 0.2 MB | painted smiley and arrow point at the vent exit (sign justifies the exit) |
| 1 | `lt_pools_ladder_stairs.{mp4,gif}` + poster (54:50) | iStudioGamer, *POOLS Chapters 1–6*, https://www.youtube.com/watch?v=8D8SumQ0WlQ | 54:38–54:58 | ≈6 MB | ≈5 MB | plaster → ladder → stairs → tile, with light from above: joins at vertical moves (P8, P4) |
| 1 | `lt_complex_elevator.{mp4,gif}` + poster (10:00) | iStudioGamer, *The Complex: Found Footage*, https://www.youtube.com/watch?v=wiAVxk5falU | 9:38–10:06 | ≈5 MB | ≈5 MB | elevator doors open on a hotel corridor that keeps the yellow walls (P4) |
| 2 | `lt_complex_directory.jpg` | same | 4:56 | ≈0.5 MB | 0.2 MB | building directory in a lobby after a dark room (P7, signage) |
| 2 | `lt_itb_hatch.{mp4,gif}` + poster (2:17) | FlyyxGamer, *Inside the Backrooms v0.7*, https://www.youtube.com/watch?v=mevBMJgn1sk | 2:02–2:20 | ≈4 MB | ≈5 MB | red hatch → drop → black → Poolrooms (P8 + P11) |
| 2 | `lt_anemo_elevator.{mp4,gif}` + poster | Full Game Playthroughs, *Anemoiapolis: Chapter 1*, https://www.youtube.com/watch?v=ak_xX9EXWN4 | 42:00–43:10; cut 6 s at the door opening (locate on download) | ≈10 MB | ≈5 MB | elevator box between the mall and the water park |
| 2 | `lt_descent_tube.{mp4,gif}` + poster (26:30) | The Game Archivist, *Backrooms Descent*, https://www.youtube.com/watch?v=NSm65OG5Wdk | 26:18–26:52 (cut to 6 s) | ≈6 MB | ≈5 MB | tile tube to an over-bright pool (P9) |
| 2 | `lt_br98_door_red.{mp4,gif}` + poster (11:10) | MR Red Gaming, *The Backrooms 1998 Full Game*, https://www.youtube.com/watch?v=eOhApKl1MOg | 10:52–11:16 (cut to 6 s) | ≈4 MB | ≈5 MB | white panel door → red-lit room (P1 + P6) |

## D. Level-art practice (stills)

| P | Proposed file | Source (page URL) | Direct URL / how | Size | Licence | What it proves |
|---|---|---|---|---:|---|---|
| 2 | `lt_vdc_trainstation_*.jpg` (3 images) | Valve Developer Community, "Level Transitions", https://developer.valvesoftware.com/wiki/Level_Transitions | the three screenshots on the page (file links on the page) | ≈0.6 MB | VDC page licence, check on page | HL2 changes map in a doorway; the overlap area exists in both maps |
| 2 | `lt_skyrim_kits_*.jpg` (2–3 slides) | Joel Burgess & Nate Purkeypile, GDC 2013 slides, https://www.slideshare.net/slideshow/gdc2013-kit-buildingfinal/17728576 | export the slides on door frames, archways and glue kits (numbers to locate) | ≈1 MB | © Bethesda / authors; research quote | one door-frame size and archways join kits |
| 3 | `lt_portal2_underground.jpg` | VDC, "Underground (Portal 2)", https://developer.valvesoftware.com/wiki/Underground_(Portal_2) | page images | ≈0.3 MB | check on page | eras joined by elevators |

## E. Real-world material (Wikimedia Commons)

| P | Proposed file | Page URL | Direct URL | Size | Licence / author | What it proves |
|---|---|---|---|---:|---|---|
| 2 | `lt_commons_peeling_wallpaper_lally.jpg` | https://commons.wikimedia.org/wiki/File:Just_Peeling_wallpaper_remains_-_geograph.org.uk_-_1818737.jpg | https://upload.wikimedia.org/wikipedia/commons/a/a3/Just_Peeling_wallpaper_remains_-_geograph.org.uk_-_1818737.jpg | 327 KB (683×1024) | CC BY-SA 2.0, David Lally, 2010-04-17 | what "paper stripped to the layer below" looks like (P5 band) |
| 2 | `lt_commons_peeling_stenciled.jpg` | https://commons.wikimedia.org/wiki/File:Peeling_Hand_Stenciled_Wallpaper_(5080295878).jpg | https://upload.wikimedia.org/wikipedia/commons/7/70/Peeling_Hand_Stenciled_Wallpaper_%285080295878%29.jpg | 169 KB (768×1024) | CC BY 2.0, Richie Diesterheft, 2010-10-09 | same; torn edge over a different finish |
| 3 | `lt_commons_peeling_cottage.jpg` | https://commons.wikimedia.org/wiki/File:Jimmy%27s_cottage_-_peeling_paint_and_wallpaper_-_geograph.org.uk_-_2342954.jpg | https://upload.wikimedia.org/wikipedia/commons/6/66/Jimmy%27s_cottage_-_peeling_paint_and_wallpaper_-_geograph.org.uk_-_2342954.jpg | 358 KB (640×492) | CC BY-SA 2.0, Evelyn Simak, 2011-04-01 | paint and paper layers together |

Trade-literature pages (frames, beads, reducers, soffits) are already listed in `media_candidates_buildings.md`; not repeated here.

## Totals

- 37 rows: 17 clips, 15 stills from video, and about 10 web images (Dezeen, VDC, GDC slides, 3 Commons files).
- Priority 1 only: 15 rows (10 clips, 6 stills). Raw ≈ 50 MB (scratchpad, deleted after); kept ≈ 52 MB.
- Everything: raw ≈ 100 MB; kept ≈ 92 MB.
