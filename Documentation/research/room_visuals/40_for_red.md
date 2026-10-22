# Room visuals and the Run! plan: for Red (2026-10-14)

## Where to look

- **Figma:** FRONTROOMS · ROOM VISUALS, section `2862:6157` (x 25257, y 2000), 34 slides.
  - RV00–RV17: every room type as the game draws it today (main `0a9ee7f`).
  - RV18–RV21 and RV33: an index of all visual research.
  - RV22–RV25: Run! research.
  - RV26–RV31: the three Run! directions and my pick.
  - RV32: media waiting for your yes.
- **Check slides:** VISUAL VERIFICATION LOG `2595:6093`. Run! rows are VL166–168, 189–192, 194–195 and 236–238.
- **Files:** this folder.
  - `01_inventory.md` (rooms) and `02_research_index.md` (research). Each starts with a dated table of what changed.
  - `05_captures_1007_1014.md`: the latest frames.
  - `21_run_fix_1014.md`: the last Run! checks.
  - `30_figma.md`: slide and image map.
- **Nothing was changed in your project except this folder and the log rows.** Unity was not opened on Frontrooms3D.

## What changed this week

- The room frames were re-shot on today's main. 61 of 65 are pixel-identical to 7 Oct. Only the title Run and Exit rooms changed: they now show the merged EXIT signs.
- Fixed slides:
  - Doors are single-acting (since 3 Oct).
  - The Exit room's floor strip is never built: that code branch is dead.
  - Office CRTs play screen ads.
  - New lamp states: run-time Off and Reduce flashing.
  - The placard glows only when its own lamp dies.
- All three Run! floors are now to your 3 Oct spec: waxed VCT with box probes that really reach the floor (VL236).
- The Relay was re-checked at its real height, 2.05 m (VL237): **A fails at 2.97 : 1**, B passes at 7.68 : 1, C passes at 9.36 : 1.

## Run! recommendation: B · Emergency Power

- **The look:** step in and the mains drop. 0.3 s of black, then battery twin-heads throw hard warm pools every 6 m. Red stays on the EXIT signs and fire gear.
- **Why:**
  - It is the only direction that keeps the Relay readable at both sizes (8.1 and 7.7 : 1).
  - It is real 1990.
  - It is Dark Deception's grammar: pools and small reds.
  - It asks the least of the map.
- **Its open items:**
  - A near head's spill is brighter than the goal door.
  - The two-tone cart reads only 1.16 : 1.
  - Contrast 8 with 1–3 % red cannot hold together with the pool brightness.
- **Not yet checked in any direction:** a 5.5 m/s sprint pass, the reset, the stage-1 warning, a Relay pushing through a door, and WebGL. These come after you pick.

| | A · Red Ward | **B · Emergency Power** | C · House Lights |
|---|---|---|---|
| Tripped median Y (linear) | 0.015 | 0.003 | 0.326 |
| Relay, proxy / 2.05 m | 6.3 / **3.0 (fail)** | 8.1 / 7.7 | 4.4 / 9.4 off axis; on axis a shut door pair hides it |
| Lights armed / tripped | 18 / 13 | 12 / 15 | 10 / 22 |
| Breaks a rule | red fills the frame (rule 1) | — | red flashers (rule 4) |
| Days to build (estimate) | 6–8 | 8–10 | 10–12 |

## Your decisions

1. **A, B or C?** Or B, with a shared goal sign and C's held doors added later?
2. May a trigger room author its own goal door (map request CR-6a), or must Run! end on a zone border?
3. On the trip: a local sound, or silence (as EGRESS says)?
4. Is "the goal door is the brightest object" enough when a nearby lamp's spill is brighter (B)?
5. Run! in Office zones too, or in Level 0 only?
6. Rebuild the never-reached title-stream Run room to match your pick, or leave it?
7. **The shared goal sign:** the merged EXIT kit with its brightness fix (`exit_sign_fix1`, not merged yet), plus a lit door lite. Yes or no?
8. **The media batch below:** yes or no?

## Media waiting for your download approval (nothing was downloaded)

Ask first: the four photos that fill RV24's empty slots. Each is a Wikimedia Commons 1920 px thumbnail, about 1 MB or less; check the licence per file.

| # | Save as | Page | What it shows |
|---|---|---|---|
| A4 | `run_real_hallway_normal_vs_emergency.jpg` | https://commons.wikimedia.org/wiki/File:Emergency_light_at_hallway_in_building.jpg | one hallway under normal and under emergency light |
| A5 | `run_real_twin_head_unit.jpg` | https://commons.wikimedia.org/wiki/File:Emergency_light_old.jpg | the US twin-head battery unit (B's light source) |
| A9 | `run_real_fire_door_magnet.jpg` | https://commons.wikimedia.org/wiki/File:Wooden_fire_door_magnet.JPG | a fire door on a magnetic hold-open (C's doors) |
| A11 | `run_real_hospital_corridor_old.jpg` | https://commons.wikimedia.org/wiki/Category:Hospital_corridors (pick one US 1970s–90s view) | a real hospital corridor: width, handrails, doors |

The rest of the batch, 23 items in all, is on RV32 and in `media_candidates.md` with direct URLs:
- 8 more Commons photos (A1–A3, A6–A8, A10, A12): about 1 MB each.
- 5 film clips (B1–B5: Jacob's Ladder, The Exorcist III, Terminator 2, Aliens, Alien). Each is cut to a 4–6 s clip of at most 3 MB from an official trailer of 30–80 MB.
- 3 game clips or stills (C2–C4). C1 needs no download: it only logs a missing URL.
- 2 documents to read (NFPA 101, 1988/1991; GDC 2018).

In total: about 12 MB of photos and 15 MB of clips kept.

## Smaller follow-ups (no decision needed)

- The production probe setup is still clone-only: URP probe blending and box projection, the Surface shader's probe keywords, and one probe set per state. It goes into the package for whichever direction you pick.
- The dead Exit-strip code in RoomStream will be removed in the next RoomStream package.
- A module room (waiting room, storage, bullpen, pillar hall) still has no frame of its own.
- The source URL of the Dark Deception clip used in week 1 is still unknown. If you remember where it came from, tell me.
