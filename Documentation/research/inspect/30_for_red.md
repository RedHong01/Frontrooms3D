# EXAMINE: close-ups that pause the game (for Red)

Visual chat (游戏视觉), 2026-10-14. Your ask (10-07): every asset worth a look, starting with the evacuation plan, can be walked up to and seen in a close-up, and the game pauses while you look.

Built and tested in a clone of main 0a9ee7f. Nothing in your project's `Assets/` was written. Details: `20_prototype.md` (tests), `10_design.md` (design), `contracts/` (other chats).

## 1. What it looks like

Walk to 1.6 m of the plan: `E · READ PLAN` shows. Press E: the world freezes on that frame, and the camera moves to the plan in 0.43 s. It frames the plan the way your render did (the plan on the left 62 %, the real wallpaper on the right). E, S or Esc gives control back at once; the view swings home in 0.13 s and the body in 0.40 s.

| Frame | Shows | Figma |
|---|---|---|
| `images/h_01_closeup_placard_1080p.jpg` | the plan close-up in game, 1080p | VL246 |
| `images/h_02_closeup_legend_4k_crop.jpg` | the legend at 4K, native pixels | VL246 |
| `images/h_03_closeup_key_turned_1080p.jpg` | the key on its tag (direction B), turned by mouse | VL246 |
| `images/h_05_transition_sheet.jpg` | walk up, in, hold 10 s, out (10 frames) | VL247 |
| `images/f_01_lamp_states_P2_vs_red.jpg` | your frame vs lit / own lamp off / dead area | VL249 |
| `images/f_02_zoom_legend_framed.jpg` | the zoom step (off in v1, see D3) | VL250 |
| `images/f_04_key_fill_v1_vs_fix.jpg` | key close-up: 29 % → 46 % of the frame | VL252 |
| `images/h_06_pause_summary_chart.jpg` | the pause test in one chart | VL248 |

All slides are in Figma, FRONTROOMS · VISUAL VERIFICATION LOG, VL246–254.

Numbers at 1080p: plan title 37 px; the 4.2–4.4 mm lines 10.5–11 px; the 2.4–3.6 mm lines 6–9 px (they need the zoom). In a dark cell the close-up lifts exposure up to +2.25 EV: the paper reads sRGB 115–124 instead of about 70, and the glow legend shows (1.54–1.61:1). Lit, the legend stays faint (1.14:1), as the placard spec intends.

## 2. How the pause works

- From the E press until the camera starts home, the game is in a new phase, `Inspect`, with `Time.timeScale = 0`. The Relay, lamps, doors, stamina, timers and the director all stop.
- The mix freezes with it: steps, doors, stingers and the heartbeat pause; the Relay's hum stays 6 dB down; the room tone stays 10 dB down.
- **Tested:** a 10 s close-up and a 0.25 s close-up leave the game in exactly the same state 40 s later (same player and Relay positions, 1,557 lamps, 115 doors). During a Relay warning: the Relay moved 0.0 mm in 5 s, then carried on the moment you left.
- Focus loss in a close-up opens the pause card; resume goes back to the close-up. R does nothing in a close-up.

**Chase rule (D1):** no prompt while the Relay chases you, breaks a door, or is in your view. Only cues you can perceive count. The silent "it sees you" flag is never used, so the prompt can't be used as a Relay detector. A stage 1–2 warning still allows a close-up, and it freezes with the world.

## 3. Which assets get it

| When | Asset | Note |
|---|---|---|
| **Now (v1)** | Evacuation plan `Kit_EvacPlacard` | The only one that ships (`ship: true`) |
| With D1.5 | The first key of a run, on its tag (direction B) | All 9 key-tag kits carry the data now (`ship: false`); the hand-off waits for D1.5's key rig |
| Test bed only | door sign, key on its tag, clock, vending machine, copier, desk phone | Editor and dev builds (`DevTargets`) |
| Read in place, no close-up | door sign, door number plate, EXIT sign, scratch-throughs | Large enough to read by stepping up |
| Later | office CRTs as a "panel" target with the 1990 office UI (系统设计's computer-screen track), TVs in piles, key boards and cabinets, memo pages, sanctuary placard copies | The `panel` kind exists in code; the cursor layer is not built |

## 4. Merge (in order)

| Step | Who | Command or file | State |
|---|---|---|---|
| 0 | visual chat | `bash /Users/redwang/FrontRoomsVisualWork/apply/inspect_data.sh`, then `--apply` | **READY**: dry run OK today. 17 text files (Blender kit modules and 11 sidecars with the close-up data). No behaviour change |
| 1 | map chat | `contracts/map_inspect_1_rig.diff` | dry run OK on main |
| 2 | visual chat | `bash /Users/redwang/FrontRoomsVisualWork/apply/inspect.sh`, then `--apply` | **WAITING** for step 1 (prints `WAITING`, exit 3, writes nothing) |
| 3 | map chat | `contracts/map_inspect_3_game.diff` | dry run OK on main |
| 4 | sound chat | `contracts/sound_inspect.diff` | dry run OK on main |
| later | touch session, 平面视觉 | `contracts/touch_inspect.md`, `contracts/graphic_inspect.md` | requests, not started |

Read-me files for each chat: `contracts/map_inspect.md`, `sound_inspect.md`, `touch_inspect.md`, `graphic_inspect.md`.

## 5. Your decisions

Each has a recommended default. If you say nothing, the default stands.

| # | Question | Default (recommended) | Other option |
|---|---|---|---|
| D1 | Close-ups near the Relay | No prompt during a chase, a door break, or with the Relay in view; warnings allowed and frozen | Always allowed and paused; or a setting that keeps time running |
| D2 | When does control come back? | When the camera starts home (at once) | When it is home (0.40 s with no control) |
| D3 | Zoom before the touch zoom input (T3) exists | No zoom in v1; the smallest lines stay 6–9 px at 1080p | W / stick forward = closer, until T3 |
| D4 | Exposure lift in a dark cell | Up to +2.25 EV (0 when lit) | Less (the design's first guess was 1.0 EV; a dark plan then reads darker) |
| D5 | The plan's spot P2 sits under a dead lamp at this seed | Keep it: a dark mount fits the place, and the close-up now reads | Ask the map side to keep that lamp alive |
| D6 | Angle on prints seen from the side | Keep up to 25° of the side view | Square up to 10–12° (a faster swing in) |
| D7 | Door sign and number plate | Read in place, no close-up | A sign close-up after one rattle, reach 1.2 m |
| D8 | Keys | First key of a run only, the key on its tag | Every key (slower pickups) |
| D9 | Should the Esc pause also stop time? | Yes (today a door still swings under Esc and makes noise) | Keep today's Esc |
| D10 | Other props (clock, vending, copier, phone, key boards) | None in v1; test bed only | Pick any |
| D11 | Who fixes the CRT screens (they must pause and ignore E in a close-up) | The visual chat, in its next CRT package (its COV-4 fix to that file is already in main) | The map chat |
| D12 | Fetch research clips (`media_candidates.md` §D) | Yes for rows 1–2 (Silent Hill 2 wall photo, Gone Home note lift; official Steam media) | Also row 3 (RE Village; needs your YouTube sign-in) |

## 6. Still open

- Phone capture and the touch requests T1–T7 are not done. On a phone today (after step 3) you tap the plan to open it and use the stick pull-back, the pause button or Android Back to leave.
- The first-key hand-off (C21) needs D1.5's key rig first.
- CRT screens (D11) must be fixed before any build where a CRT can be in a close-up.
- Blank faces: the door sign's text and the tag digits (`Prop_SignEngraved`, `Prop_KeyTagNo`) do not exist yet.
- 平面视觉 still owns the prompt words, the hint style, and the transcript panel (G1–G13). The door-sign prompt is 2.2:1 on a light sign, below 3:1 (G6).
- Clones that can be removed (about 31.6 GB): `proj_inspect_fix`, `proj_inspect_fix_base`, `proj_inspect_play`, `proj_inspect_v1`, `proj_inspect_v1_play`, `proj_inspect_v1_warn`, `proj_inspect_v1_base2`, `proj_inspect_v1_now`. Kept: `proj_inspect`, `proj_inspect_base`. Nothing was deleted.
