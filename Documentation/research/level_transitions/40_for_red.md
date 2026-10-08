# Level transitions · 40 · For Red (one page)

Date: 2026-10-08. Nothing is implemented in your project. Every render comes from private clones. Nothing was downloaded.

## What to look at in Figma

Section **FRONTROOMS · LEVEL TRANSITIONS** (`2831:6157`, page "Undergoing Game Projects", x 33937, y 19200): https://www.figma.com/design/0tCbAiVUlrPId3RWd9LRif/?node-id=2831-6157

Look at these first:
1. **LT13 · Today, pick or closed** (`2856:6716`): your frame and the arch, all three in today's game.
2. **LT12 · My pick** (`2856:6676`): the pick in today's game.
3. **LT09 · Close** (`2856:6538`): the new option, which closes borders with doors.
4. **LT15 · You decide** (`2856:6845`): four questions.

Then LT04–LT08 (one slide per variation), LT10–LT11 (all variations, same camera) and LT14 (costs). LT00–LT03 are the research. LT16–LT17 are backup.

What changed since the first build:
- **The pick is now rendered in today's game.** It uses main 22bb75f with the cool Office colour, the new print and today's doors and windows.
- **A new option: Close.**
- The research slides lost their grey placeholders.
- Every number on the slides was checked again.
- Full list: `30_figma.md` §5. Render report: `16_pick_and_close.md`.

## My recommendation

**Keep the pick:**
- Frame on every border: a casing, portal, steel frame or base.
- Renovation on about 3 in 10 borders.
- The cool Office colour, as it runs now.

**Why:**
- Every change now sits on something built, so nothing is "simply cut off".
- The layout and the open views into the Office stay as they are.
- The renovated borders explain why an office is inside Level 0.

**What the renders also show:**
- **In your own frame the pick is quiet.** Only 1.1 % of the pixels change. The open edge on the right becomes a thin pilaster.
- Up close it reads well: 3.2–12.7 % of pixels change at a crossing.
- **If you want the change gone from the room, choose Close instead.**
  - Close makes every Office | Level 0 border a door or a wall. It is how the IPs do it: "never in the open".
  - Open or arched crossings drop from 1.26 to 0 per chunk. Doors rise from 1.37 to 2.53.
  - In your frame, 8 % of the pixels change. At the arch, 34.5 % change.
  - It costs almost nothing to render. But it changes the layout, and every border becomes a door to open.
- **Cost of the pick near the player:**
  - triangles 145 k → 314 k (×2.2);
  - renderers +9–11 %;
  - 20 more materials;
  - 94 MB of renovation textures (WebGL about 34 MB).
  - Frame alone is cheap: +21–24 % triangles, 0 MB.

## Your decisions

1. **The pick:** Frame + renovation on 3 in 10 + colour as now? Or another mix?
2. **Map rule:** may the map close Office | Level 0 borders with doors and walls (Close)? This changes the layout.
3. **Light:** keep colour only? Or test the soft or dark border rule (LT08)?
4. **Media:** may I download the list below?

Defaults that hold unless you say otherwise (LT16):
- Office is the newer fit-out and owns every frame and base.
- Only Neck would narrow crossings.
- Free posts get a collider.
- The base is on Office walls only.
- Rougher borders at higher tiers.
- Fix the corner flicker in the map first.
- Make the torn paper scraps use today's Level 0 print.

## Media waiting for your approval (priority 1)

- 22 rows, about 26 downloads.
- The clips keep an mp4, a GIF and a poster each. Kept size is about 60 MB.
- The raw downloads are about 82 MB. They are deleted after the clips are cut.
- Full rows, with exact times and how to fetch each one: `media_candidates.md` and `media_candidates_buildings.md`.

| File | Source | Kept size |
|---|---|---|
| `lt_kane_ff3_neck` (clip) | Kane Pixels, *Found Footage #3*, youtube.com/watch?v=acdYs9tPLko, 34:18–34:30 | ≈5 MB |
| `lt_kane_ff3_white_office.jpg` | same, 35:04 | 0.2 MB |
| `lt_kane_ff_firedoor` (clip) | Kane, *The Backrooms (Found Footage)*, youtube.com/watch?v=H4dGpz6cnHo, 6:40–6:58 | ≈6 MB |
| `lt_kane_ff_green_pocket` (clip) | same, 2:08–2:20 | ≈5 MB |
| `lt_kane_ff_ladder_hole.jpg` | same, 2:02 | 0.2 MB |
| `lt_kane_pit_fall_lower` (clip) | Kane, *Pitfalls*, youtube.com/watch?v=0XwlWXtpaCM, 5:36–6:14 | ≈5 MB |
| `lt_kane_pit_outpost.jpg` | same, 1:52 | 0.2 MB |
| `lt_kane_sde_door_light` (clip) | Kane, *Static Dead End*, youtube.com/watch?v=ZbPaWvqAEq4, 1:56–2:14 | ≈5 MB |
| `lt_kane_info_dark_pocket` (clip) | Kane, *Informational Video*, youtube.com/watch?v=ZIFhglHn3W0, 5:08–5:14 | ≈5 MB |
| `lt_kane_info_teal.jpg` + `lt_kane_info_red.jpg` | same, 7:10 and 7:24 | 0.4 MB |
| `lt_a24_wall_black` (clip) | A24, *Backrooms* trailer, youtube.com/watch?v=0HjdiohVOik, 0:12–0:24 | ≈5 MB |
| `lt_etb_door_preview` (clip) | iStudioGamer, *Escape the Backrooms* walkthrough, youtube.com/watch?v=-1VNyWyyk6I, 22:30–22:42 | ≈5 MB |
| `lt_etb_vent_sign.jpg` | same, 6:25 | 0.2 MB |
| `lt_pools_ladder_stairs` (clip) | iStudioGamer, *POOLS*, youtube.com/watch?v=8D8SumQ0WlQ, 54:38–54:58 | ≈5 MB |
| `lt_complex_elevator` (clip) | iStudioGamer, *The Complex*, youtube.com/watch?v=wiAVxk5falU, 9:38–10:06 | ≈5 MB |
| `bthl_usg1980_p22_trims.jpg` | USG Gypsum Construction Handbook 1980, p.22, archive.org/details/GypsumConstructionHandbook | ≈0.5 MB |
| `bthl_usg1980_p37_frames.jpg` | same, p.37 | ≈0.5 MB |
| `bthl_usg1982_p23_p26.jpg` | USG handbook 1982, pp.23 and 26, archive.org/details/USGConstructionHandbook2ndEdition | ≈1 MB |
| `bthl_usg_systemfolder1990_ceilings_*.jpg` (3–4 pages) | USG System Folder 1990, archive.org/details/USGSystemFolder1990 | ≈2 MB |
| `sdi111_frame_profiles.png` | SDI 111 frame details, steeldoor.org (PDF, one page) | ≈1 MB |
| `nara_17427062_ceiling_tile_door_faces_1975.jpg` | NASA, 1975, Wikimedia Commons (public domain; 25 MB original, saved at 1920 px) | ≈1 MB |
| `commons_usfws_drywall_repair.jpg` | USFWS, 2011, Wikimedia Commons (public domain). Use it for the technique only, not its materials | 1.0 MB |

The two most useful are `lt_kane_ff3_neck` and `lt_kane_ff_firedoor`. They are the IP evidence that LT01 still lacks.
