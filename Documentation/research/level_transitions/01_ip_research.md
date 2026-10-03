# Level transitions · 01 · How Backrooms IPs change from one level to another

Date: 2026-10-03. Research only. Nothing in the project changed. No media was downloaded.
Question from Red: between Level 0 and Office "the wall's texture and type are simply cut off". How do the existing Backrooms IPs handle the change from one level or area to another, and what does general first-person level art do?
Scope of this file: the IPs (Kane Pixels, the A24 film, the wiki canon, the games) and game level-art practice. Real buildings are in `02_real_buildings.md`. The audit of our own cuts is in `03_cut_inventory.md`, `logs/counts.md` and `images/cut_*.jpg`.

**How the evidence was taken.** I watched each video in a browser and read single frames at exact times (YouTube player time, ±1 s). Nothing was saved to disk. Every timestamp below is something I saw on screen, unless a line says "reported". Wanted clips and stills for Figma are listed in `media_candidates.md`. Media already on disk is listed in §10.

---

## 0. The answer on one page

1. **Nobody in the Backrooms canon changes the wall finish on a bare line in the open.** Every change I found sits on a built object or on a gap in perception: a door, a stair, a ladder, a hole, a corner, a dark stretch, or a black cut. Our open edges and flat seams (0.89 + 0.61 per 24 m chunk) do the one thing no IP does.
2. **Kane Pixels uses five devices, in this order of frequency:** (a) an ordinary door or doorway (fire door, wooden door, cased opening); (b) a vertical move (stairs, ladder, a hole you fall through); (c) darkness, so the edge is never seen; (d) a change of light colour while the surfaces stay the same; (e) a black or static cut (editing only).
3. **The closest match to our problem is Kane's *Found Footage #3* at 34:21–34:28.** A yellow room has a deep recess; inside it, a doorway leads to a white corridor. The white finish wraps the whole jamb, the yellow stops at the outside corner, and the black base and the carpet run straight through. The new space also has its own brighter, cooler light. This is the "newer layer owns the joint" rule from `02_real_buildings.md`, done on screen.
4. **The original 2002 photo already mixes three wallpapers in one frame.** Each paper sits on its own partition and changes at a wall end or a corner. One carpet, one ceiling grid and one light colour hold the frame together (`ir01_dsc00161`).
5. **The wiki canon describes gradual transitions as well as hard ones.** Fandom's Level 0 exit list includes hallways that "gradually transition" into Level 1's garages. Wikidot's Red Rooms announce themselves by wallpaper peeling to show the next colour underneath, and by the carpet changing. Fandom's Level 37 leads into its sub-level "without one taking notice". The A24 production designer describes the same idea: the deeper you go, the less the place is "remembered".
6. **The games mostly cheat with loading screens.** Escape the Backrooms, Inside the Backrooms, The Complex and Anemoiapolis use a door, vent, hatch, elevator or water slide, then a short black frame, then the new level. That works for level-based games. FrontRooms is one streamed map, so a black cut only fits the title handoff (which already uses it: map behind the next shut door).
7. **POOLS is the best counter-example.** It keeps one material family (ceramic tile) and changes tile colour, water level, light and arch shape. Joins happen at ladders, stairs and arches, and light from the next space spills through first.
8. **Game level-art practice agrees.** Valve's "seamless transition" is an area that exists in both maps (a door or hallway). Bethesda's Skyrim kits use one door-frame size and archways as universal connectors, and "glue kits" for transitions. Resident Evil turns the door into the transition itself. Portal 2 uses elevators between eras. Resident Evil Village links strongly themed strongholds through a neutral village hub.
9. **For FrontRooms the evidence points to four families worth pre-rendering:** a frame that owns the joint (door, cased opening, neck), a corner rule with shared constants (base, carpet or ceiling continue), a stripped-paper decay band, and a dark or re-lit buffer cell. §9 maps them to our edge kinds and to `shots.json`.

---

## 1. Our problem in numbers (from the audit)

Means per 24 m chunk over 1620 chunks (20 seeds), from `logs/counts.md` and `03_cut_inventory.md`:

| Office \| Level 0 border | Per chunk | Chunks with one | IP verdict |
|---|---:|---:|---|
| Theme-border edges, all kinds | 5.24 | 55.9 % | — |
| Solid wall (two skins) | 2.28 | 53.0 % | fine as built, except free ends |
| Door | 1.37 | 35.9 % | the IP default; needs a frame that owns the joint |
| Open edge (no wall) | 0.89 | 28.3 % | never seen in any IP |
| Arch | 0.37 | 21.0 % | the one IP case (Kane FF#3) wraps the whole jamb in one finish |
| Window | 0.33 | 11.6 % | seen as a "preview frame" (Kane FF#3 skywalk, Pitfalls tunnel) |
| Flat seam (paper switches mid-plane) | 0.61 | 26.7 % | never seen in any IP or in the 2002 photo |
| Height-border edges | 10.74 | 92.5 % | stairs and steps are a canon transition device |

---

## 2. Pattern catalogue (what the IPs actually do)

| # | Pattern | What the player sees | What hides or justifies the seam | Best evidence | Fit for FrontRooms |
|---|---|---|---|---|---|
| P1 | Door cut | A normal door; the far side is a different level | The door leaf and frame; you see the new space only after it opens | Kane FF 6:52→6:57; Static Dead End 1:58–2:10; Backrooms 1998 10:55→11:05; wiki Level 1 "pushable doors" | High. 1.37 border doors per chunk already. They need a frame that owns the joint. |
| P2 | Neck / cased opening | A recess or short passage between two looks | The jamb belongs to one side; the change sits on an outside corner | Kane FF#3 34:21–34:28; ETB white connector 22:30; Valve "area in common" | High. Direct fix for arches (0.37) and open edges (0.89). |
| P3 | Corner rule | Several finishes in one view, each on its own wall | Changes only at wall ends and corners; shared carpet and ceiling | 2002 photo Dsc00161; Kane FF#3 (base and carpet continue) | High. Removes flat seams (0.61). |
| P4 | Shared constants | Something stays the same across the line | Continuity of base, carpet, ceiling grid or light colour | 2002 photo; Kane FF#3; POOLS (tile module); The Complex 10:00 (yellow walls in hotel) | High. Cheap; it lowers the contrast of every cut. |
| P5 | Decay or gradient band | The old look degrades before the new look starts | Peeling paper shows the next layer; carpet changes texture | Wikidot Red Rooms; Fandom Level 0 exit 1; Fandom Level 37→37.1; A24 "less remembered"; Kane Pitfalls lower level | High for the "Level 0 stripped to Office drywall" story. |
| P6 | Light leads | Light colour changes before or instead of the surfaces | Light falls off softly; surfaces can stay the same | Kane FF 2:09–2:21 green pocket; Informational Video 7:08–7:24 teal and red; Static Dead End 2:10 green | High. We already have lamp temperaments. |
| P7 | Dark buffer | A dark stretch between two lit looks | You never see the edge in light | Kane FF 4:40–4:52; Pitfalls 8:02–8:25; FF#3 19:20; The Complex 4:46; Wikidot Blackout Zones; Fandom The Void | Medium-high. A dead or dim lamp on the border cell. |
| P8 | Vertical move | Stairs, ladder, hole, slide or elevator | The view turns up or down; the levels never share a wall plane | Kane FF 1:58 ladder, 4:56 and 6:44 stairs; Pitfalls 5:40 fall; Fandom Level 1 "flight of stairs"; Portal 2 elevators | Medium. Our height steps (10.7 per chunk) could carry zone changes with a soffit or a few steps. |
| P9 | Preview frame | You see the next look through a frame before you enter | The frame (door, tunnel mouth, window) crops the edge | ETB 22:35; Pitfalls 8:40; FF#3 30:30; Backrooms Descent 26:30 | Medium. Our 0.33 border windows per chunk; arches seen head-on. |
| P10 | Built insert | A man-made room set into the Backrooms | Construction makes the edge plausible (glass, steel, studs) | Kane Pitfalls 1:40–1:55 Async control room; Static Dead End 1:40 steel walkway; Everything Must Go 9:32–10:32 stud cavity; A24 "two portals in a shared wall" | Medium. Office as a 1990 fit-out built into Level 0. |
| P11 | Black or static cut | The picture goes black or tears, then a new place | Editing, a loading screen or a "noclip" | Kane FF 0:30; A24 trailer 0:19–0:22; ETB 6:32, 22:38, 38:04, 1:14:26; Inside the Backrooms 2:14 | Low inside the map. Already right for the title handoff. |
| P12 | Glitch as content | Stretched wallpaper, objects half sunk in the floor | The error is the point, and it covers a whole room | Kane Static Dead End 2:36–4:00; A24 set design (raked floors, half-height doorways) | Low. Our cut already reads as a bug; a glitch must be total and deliberate, never a single edge. |

---

## 3. Kane Pixels, *Backrooms* web series (YouTube @kanepixels)

Episode list and dates: Wikipedia, "Backrooms (web series)" (25 episodes, 2022-01-07 to 2026-08-18). Kane Parsons made the first video in Blender and After Effects (Wikipedia). Publish times below are YouTube's (US Pacific).

### 3.1 *The Backrooms (Found Footage)*, 2022-01-07, 9:14, https://www.youtube.com/watch?v=H4dGpz6cnHo

| Time | What is on screen | Transition device |
|---|---|---|
| 0:28 → 0:30 → 0:32 | Outdoor film shoot → black frame with a static line → camera on the Level 0 carpet | P11 noclip cut |
| 1:56–2:06 | A steel ladder on a yellow wall rises into a square hole near the ceiling | P8 vertical exit, inside Level 0 |
| 2:09–2:21 | Same wallpaper and carpet, but the room is lit green; then a dark corridor toward a lit doorway | P6 light pocket, then P7 dark lead-in |
| 2:30–2:52 | Back to normal Level 0 light | — |
| 4:32 → 4:36 → 4:40 | Yellow room → VHS static → the camera tumbles into the dark | P11 + P8 fall |
| 4:44–4:52 | A very dark open space; far away, a lit wooden stair and a blue door | P7 dark buffer + P9 preview |
| 4:56–5:20 | Wooden stair with a handrail, climbing | P8 stairs |
| 5:30–5:56 | White rooms with a timber floor, a window hatch, a trunk | new look, entered by stairs |
| 6:00–6:30 | Dark area → grey concrete service corridor with hazard signs, filing cabinets | entered through an opening in a dark wall (6:04) |
| 6:40 | Green "Fire exit / Keep clear" sign over a grey steel door | signage announces the door |
| 6:44–6:50 | Concrete stair up to the door | P8 |
| 6:52 → 6:55 → 6:57 | Grey door with a push bar → dark as it opens → yellow Level 0 room, the door edge still in frame | **P1 door cut, the cleanest hard cut in the series** |
| 8:40–8:50 | Treetops from above: the camera falls out of the sky | P11 exit |

How well it works: very well. Each change of look is tied to a thing you can name (a ladder, a stair, a fire door). The only "impossible" cut is the noclip, and it is hidden by static.

### 3.2 *Backrooms – Informational Video*, 2022-02-12, 8:01, https://www.youtube.com/watch?v=ZIFhglHn3W0

| Time | What is on screen | Device |
|---|---|---|
| 2:00 | Async team in suits, Level 0 | — |
| 4:56–5:04 | A dark square hole in a yellow wall | P8/P9 |
| 5:12–5:54 | Inside, only a flashlight circle: a leafy tapestry wallpaper, curtains, a wheelbarrow, then a half-timbered house front, all in darkness | **P7: darkness means the edges of the foreign look are never seen** |
| 7:08–7:16 | A dark hall lit by teal strip lights | P6 light code |
| 7:24 | A room lit by red troffers | P6 light code |

### 3.3 *Backrooms – Pitfalls*, 2022-05-01, 14:04, https://www.youtube.com/watch?v=0XwlWXtpaCM

| Time | What is on screen | Device |
|---|---|---|
| 0:25–1:25 | Async suit room (white) → corridor → blue-lit Threshold machine | P11 machine "portal" |
| 1:40–1:55 | A control room with glass and desks **built inside the yellow Level 0**: Async's outpost | **P10 built insert** |
| 3:55–5:40 | Room 14D: rows of square pits in the carpet | P8 |
| 5:10 | An open wooden door at the far end, green light behind it | P1 + P6 |
| 5:40 → 5:55 → 6:10 | Marvin falls; the lower level has the same architecture but is grey and darker; looking up, the square hole in the ceiling | **P8 fall + P5 re-palette of the same architecture** |
| 6:40–7:40 | Grey rooms with dark wood door frames; an orange-lit room | P6 |
| 7:50–7:58 | A long narrow corridor with something red at the end | P9 |
| 8:02–8:25 | Black | P7 |
| 8:40 | A round tunnel mouth framing a red-lit street | **P9 preview frame** |
| 9:40–10:25 | Red-lit suburban house; its door opens on blue-painted walls | P1 |
| 10:45–11:15 | Inside the house, a plain doorway leads straight into a narrow white corridor | P1 (door frame as the only joint) |
| 11:30–12:00 | A room with older, stained wallpaper | P5 |
| 13:30–13:58 | The square pit seen from below; a rescue line comes down | P8 |

### 3.4 *Backrooms – Found Footage #2*, 2022-08-21, 13:23, https://www.youtube.com/watch?v=sA5PxGHqpTo

| Time | What is on screen | Device |
|---|---|---|
| 1:00 → 1:20 | A blue-taped square on a garage floor → looking up at recessed downlights | P8 fall through a "null zone" |
| 2:00–5:40 | Cream rooms with recessed can lights; stacked chairs, a sofa | new look, no visible edge (arrival by falling) |
| 6:42–7:12 | A tall atrium; ceiling tiles missing and stained | P5 decay |
| 8:50–9:20 | Rooms with a hardwood floor and white balusters: a house inside the Backrooms | P1 doorways |
| 9:35–10:20 | Green-lit room with dark plant growth | P6 |
| 11:38–12:22 | White tiles with a green band, a drained pool with ladders, table lamps on the floor | pool look; P8 ladder |
| 12:56–13:00 | Green cracks spread over the walls (clip on disk) | P12 |

### 3.5 *Backrooms – Found Footage #3*, 2024-09-13, 45:01, https://www.youtube.com/watch?v=acdYs9tPLko

| Time | What is on screen | Device |
|---|---|---|
| 2:00, 6:00 | Apartment basement stair, cinder-block basement | real world |
| 6:40 | A square hole in the basement wall (the way in) | P8/P11 |
| 7:20–8:40 | White classroom and office wing, a glass partition, a corridor to a lit room | new look, entered through the hole |
| 9:00 | A door whose glass shows a brick wall behind it | door to nowhere |
| 19:00 → 19:20 | White corridor → dark corridor leading to a lit yellow room | P7 |
| 20:10 | Ski footage: the tape was recorded over | P11 (tape gap) |
| 20:30–22:10 | Beige walls, green floor, a bench, a counter with windows | — |
| 29:40 | Long orange corridor toward light | P9 |
| 30:15–30:30 | Skywalk windows onto a red-lit city | **P9 preview frame** |
| 33:30–34:16 | Classic yellow Level 0 | — |
| **34:18–34:28** | **A yellow wall with a deep box-shaped recess. In it, a smaller doorway. The jambs of that doorway are white; the yellow stops at the outside corner. Beyond is a white corridor with its own brighter, cooler light. The black base and the olive carpet run through without a break.** | **P2 neck + P3 corner rule + P4 shared constants** |
| 34:32–35:04 | White corridor → white open-plan office with desks and columns | the Office look in Kane's own world |

### 3.6 *Backrooms – Static Dead End*, 2025-02-12, 4:02, https://www.youtube.com/watch?v=ZbPaWvqAEq4

| Time | What is on screen | Device |
|---|---|---|
| 1:40–1:55 | Room 14D, now with a steel walkway and railings over the pits | P10 built insert |
| 1:58–2:02 | An ordinary dark-wood door with a frame in a yellow wall is opened | P1 |
| 2:10–2:26 | Behind it, the same yellow walls under green-yellow, dimmer light | **P6: the door marks a light zone, not a material zone** |
| 2:36–2:42 | Wallpaper stretched into horizontal streaks (clip on disk, 2:33.5–2:39.5) | P12 |
| 3:30–4:00 | Tables, slabs and a ladder half sunk into the floor and walls | P12 |

### 3.7 *Backrooms – Everything Must Go*, 2026-08-17, 16:04, https://www.youtube.com/watch?v=ewZx0bnBb30

| Time | What is on screen | Device |
|---|---|---|
| 2:26 → 2:32 | VHS tracking noise → Level 0 with hanging "Everything must go" sale signs; an arch with stairs beyond | P8 + P9 |
| 9:32–10:32 | The camera squeezes between the back of the yellow drywall and raw wood studs and plywood | **P10: the space between looks is the wall cavity** |
| 12:30–14:30 | Pitch dark; only a flashlight circle on pale partitions | P7 |

### 3.8 What Kane does, in short

- The yellow look is never cut in the open. It ends at a door, a hole, a stair, a corner, or in the dark.
- When two looks share a doorway (FF#3 34:21), **the new look owns the jamb**, the old look stops at the outside corner, and base and carpet continue.
- Light colour is a zone code of its own (green, teal, red, orange, blue). It often changes without any material change (FF 2:12, Static Dead End 2:10).
- Lower or deeper levels re-use the same architecture with a new palette (Pitfalls 5:55).
- Man-made inserts (Async rooms, walkways) look built, so their edges look intended.

---

## 4. A24, *Backrooms* (2026), dir. Kane Parsons

Released 2026-05-29 (Wikipedia, "Backrooms (film)"). Production designer Danny Vermette.

| Source | What it shows or says | Device |
|---|---|---|
| Trailer, https://www.youtube.com/watch?v=0HjdiohVOik (A24, 2026-03-31) 0:04–0:08 | The furniture showroom | real world |
| same, 0:12–0:19 | Dark back room; Clark puts his hand on the wall | — |
| same, 0:20–0:21 → 0:22 | Black → Clark crouched on the yellow carpet | P11 |
| same, 0:28–0:56 | Large yellow rooms, long corridor (stills on disk at 36 s and 44 s) | — |
| Wikipedia plot (reported) | Clark finds a glowing opening at the breaker box in the store basement and falls through | P8 + P11 |
| Dezeen, 2026-05-29 (reported) | Two portals were built in a shared wall between the basement set and the Backrooms set, one for actors and one for camera; about 30 wallpaper and carpet combinations were tested | P10 shared wall |
| Motion Picture Association interview, 2026-06 (reported) | Vermette: the deeper you go, the less things are remembered and the less they keep their original state | **P5 depth gradient** |
| Dezeen and Wikipedia (reported) | Sets used deliberate errors: doorways halfway up walls, raked floors, furniture sinking into carpet | P12 |

What we can use: the portal is a real hole in a shared wall, and depth is shown by loss, not by a new style.

---

## 5. The original photos (2002), on disk

`Research/week02/ip-research/stills/ir01_dsc00161_20020612_082113.jpg` and `ir01_dsc00159_20020612_082017.jpg` (Wikimedia Commons, "Copyrighted free use"; HobbyTown USA Oshkosh, under construction, 2002-06-12).

- **Dsc00161 (the Level 0 photo) has three wallpapers.** Left wall: small dot pattern with a wooden chair rail. Back partition: vertical stripe. Right pillar: chevron. Far left: a plainer yellow wall. Each finish covers one whole partition and changes at a wall end or a corner. One carpet, one lay-in ceiling with troffers and one warm light tie the frame together. (P3 + P4)
- **Dsc00159 shows an arcade:** a wall of half-height arched openings divides two areas; a stripe-papered column on the right; one carpet throughout. (P2 as a repeated frame)
- The place was a store being fitted out. A finish that stops at a wall end, with the next room half finished, is what the real Backrooms photo shows. (P5 "renovation" reading)

---

## 6. Wiki canon

### 6.1 Backrooms Wiki on Wikidot (CC BY-SA 3.0), accessed 2026-10-03

| Page | Authors (page credits) | What it says about getting in, out or across |
|---|---|---|
| Level 0, https://backrooms-wiki.wikidot.com/level-0 | DivineAtlas, DrAkimoto, Robert Goerman | Entry: mostly by "falling out of reality". Exit: find a flickering wall and throw yourself through it, to Level 1. **Red Rooms:** the colour shifts to red, wallpaper peels to show crimson underneath, and the carpet gets "thick, sticky, and very coarse" as you get closer: a gradual warning. **Blackout Zones:** unlit, recessed floors, rough walls. |
| Level 1, https://backrooms-wiki.wikidot.com/level-1 | Praetor3005, DivineAtlas | Other levels share "thresholds" with it. Most exits "take the form of doors"; single or double push doors, sometimes with an exit sign over them. |
| Level 37 "Sublimity", https://backrooms-wiki.wikidot.com/level-37 | egglord (critique Sariastuff; images u/AnarkyMusic) | Like leads to like: water leads to water levels; porcelain-tiled areas of another level lead here; corridors lead to similar corridors. |

### 6.2 Backrooms Wiki on Fandom (CC BY-SA), accessed 2026-10-03

| Page | Author credit | What it says |
|---|---|---|
| Level 0 "The Lobby", https://backrooms.fandom.com/wiki/Level_0 | Jamie | Five exits: wandering far enough, so that the hallways **gradually transition** into Level 1's garages; breaking a wall; breaking the floor; a rare emergency exit; greenhouse-like glass sliding doors. |
| Level 1, https://backrooms.fandom.com/wiki/Level_1 | VerySpecific, 20Tom07 | Entry from Level 0 **by walking up a flight of stairs**. Exits by staircases or hallways, or an unlocked door. |
| Level 4 "The Abandoned Office", https://backrooms.fandom.com/wiki/Level_4 | Jamie (revisions Egglord) | **The office level is entered from Level 3 through a metal door labelled "office sector"**, or from long, dry corridors of Level 37; some areas of Level 47 "seamlessly" lead to it. Exits: doors, trapdoors, ceiling hatches, emergency exits. |
| Level 37 "The Poolrooms", https://backrooms.fandom.com/wiki/Level_37 | (see page) | Wading through dark, dingy areas may lead to sub-level 37.1, "typically without one taking notice". Long corridors may lead to Level 4's workrooms. |
| Noclipping → "The Void", https://backrooms.fandom.com/wiki/Noclipping | (see page) | The in-between space of noclipping: lightless, empty, boundless. |

What the canon gives us: **the office is reached through a labelled door or a long dry corridor**, and a level can drift into its neighbour slowly. Both are diegetic justifications for what we need.

---

## 7. Games

Ratings: how well the seam is hidden and justified, 1 (bare cut) to 5 (invisible and believable). All walkthrough times are from the named upload; versions may differ.

| Game (developer, date) | Source and times | What the player sees at the boundary | What hides or justifies it | Rating |
|---|---|---|---|---|
| **Escape the Backrooms** (Fancy Games + Blackbird Interactive, publisher Secret Mode; EA 2022-08-11, 1.0 2025-10-23) | Walkthrough by iStudioGamer, https://www.youtube.com/watch?v=-1VNyWyyk6I (2026-08-15): 6:05 pit room; 6:25 a smiley face and arrow painted over a small floor-level vent; 6:32 black; 6:40 Level 1 garage. 22:32–22:35 a blue door in a white corridor opens on a pipe tunnel; 22:38 black; 22:41 Level 2. 37:58 → 38:04 black → 38:10 office. 1:14:16 painted-cloud "Fun" room → 1:14:26 black → 1:14:32 white-tile Poolrooms. Also the noclip into Level 0, EA trailer 0:01–0:06 (clip on disk). | Door, vent or hole, then a black load | The opening frames the cut; a painted sign points to it; the next level is sometimes seen through the open door first (22:35) | 3 |
| **The Backrooms 1998** (Steelkrill Studio, publisher Feardemic; EA 2022-05-25, 1.0 2025-02-20) | Walkthrough by MR Red Gaming, https://www.youtube.com/watch?v=eOhApKl1MOg (2025-02-25): 1:10 real world → 1:25 Level 0. 10:55 a white panel door in yellow paper → 11:05–11:15 red-lit room with a drawn arrow. 26:55–27:01 crates → one green VHS glitch frame (27:00) → a room with stacked chairs. | Doors and VHS glitches | A heavy camcorder filter and darkness over everything; light colour marks the danger room | 3 |
| **Inside the Backrooms** (MrFatcat with Dropsiick; EA 2022-06-20) | Walkthrough v0.7 by FlyyxGamer, https://www.youtube.com/watch?v=mevBMJgn1sk (2026-08-06): 1:52 roller shutter with a hand-print panel; 2:04 a red round hatch is opened; 2:08–2:14 drop and black; 2:17 white-tile Poolrooms. 9:10–9:18 dive under the pool water (blue); 9:22 dark red; 9:25 white tiled station corridor. 1:51:50 → 1:52:02 dark corridor → stairwell → office. | Hatch, dive or stair, then black | Water and falling hide the cut | 3 |
| **POOLS** (Tensori; 2024-04-26) | Walkthrough by iStudioGamer, https://www.youtube.com/watch?v=8D8SumQ0WlQ (2026-03-02): 53:20 beige plaster arcade over water → 54:35 corridor with a red-lit room → 54:42 ladder → 54:50 stairs up into plaster corridor with a skylight → 54:56 tiled arches over water → 55:20 tiled hall with tall windows → 57:00 dark green tile band with white above. Trailer clip on disk. | Ladders, stairs, arches, skylights | One material family (tile) everywhere; changes are in colour, water and light; light from the next space comes first | 5 |
| **Anemoiapolis: Chapter 1** (Andrew Quist; Feb/Mar 2023) | Playthrough by Full Game Playthroughs, https://www.youtube.com/watch?v=ak_xX9EXWN4 (2025-02-02): 41:00 black → 42:00 inside a wood-panelled elevator → 43:00 doors open on an indoor water park → 44:10–52:00 lazy river with inner tubes through rock, tile and plaster. | Elevator, then a water slide | The elevator is a closed box; the river carries you through several looks | 4 |
| **The Complex: Found Footage** (IsarL; 2022-08-18) | Walkthrough by iStudioGamer, https://www.youtube.com/watch?v=wiAVxk5falU (2026-04-09): 4:34 yellow → 4:46 dark room with a far lit area → 4:56–5:00 checker-floor lobby with a "DIRECTORY" board. 9:20 pool tiles → 9:40 steel elevator doors → 10:00 doors open on a hotel corridor with patterned carpet and wainscot; the walls are still yellow-beige. | Dark buffer, then an elevator | Darkness, an elevator, a building directory; walls keep the yellow family | 4 |
| **Backrooms Descent: Multiplayer Horror** (Sushi Studio; 2023-10-28) | Walkthrough by The Game Archivist, https://www.youtube.com/watch?v=NSm65OG5Wdk (2023-11-07): 0:30–2:00 an orange-and-white research base with a round gate → 2:30 Level 0. 25:00–26:00 dark tunnel → 26:20 vaulted tile pool tunnel → 26:30–26:40 a tile tube looking out at bright light → 26:50 open-air pool. | Gate, tunnel mouth | Over-bright exit hides the far side until you are through | 3 |
| **The Exit 8** (Kotake Create, publisher Playism; 2023-11-29), not a Backrooms game | Steam page; clips on disk (`EX8_*`, `gn_exit8_*`) | The same corridor resets around a corner and down a stair | The bend: you never see the reset happen | 5 (for loops) |

Steam store descriptions confirm: Escape the Backrooms says teammates "may no-clip into other areas of the map"; Backrooms Descent advertises "non-euclidean" portals; POOLS has no UI, music or monsters and is built in six chapters.

What the games add beyond Kane: a closed box (elevator, vent, slide) is the cheapest way to change everything at once. A sign or directory makes the change feel planned.

---

## 8. General level-art practice for transitions (first-person)

| Game or source | What it does | Source |
|---|---|---|
| **Half-Life 2** (Valve, 2004) | A "seamless" transition copies the same brushes into both maps around a doorway or hallway; the player loads behind a door and the far side looks unchanged. `d1_trainstation_01` changes level in a doorway. | Valve Developer Community, "Level Transitions", https://developer.valvesoftware.com/wiki/Level_Transitions |
| **Skyrim / Fallout 3 kits** (Bethesda) | One uniform door-frame size lets designers go "from kit to kit without unique pieces"; an archway that follows the full curve of walls, ceiling and floor covers gaps between rotated pieces; small "glue kits" exist only to join other kits. | Joel Burgess, "Skyrim's Modular Approach to Level Design", Game Developer, 2013-05-01, https://www.gamedeveloper.com/design/skyrim-s-modular-approach-to-level-design (GDC 2013, with Nate Purkeypile) |
| **Resident Evil** (Capcom, 1996) | Each room change is a door-opening animation that hides the load and builds tension. | Stephen Trinh, "Resident Evil – Loading Screens and Doors", Game Developer, 2020-03-04, https://www.gamedeveloper.com/design/resident-evil---loading-screens-and-doors |
| **Resident Evil 7 / Village** (Capcom, 2017 / 2021) | RE7 scales back to one estate with distinct houses (main house, old house, guest house, boathouse). Village links four strongly themed strongholds through one village hub, described as "a theme park of horror"; Heisenberg's factory has no snow. | Wikipedia, "Resident Evil 7: Biohazard" and "Resident Evil Village" |
| **Alien: Isolation** (Creative Assembly, 2014) | The station is divided into sections connected by trams and elevators; every section shares one lo-fi, 1970s vision of the future. | Wikipedia, "Alien: Isolation" |
| **Portal 2** (Valve, 2011) | Each aesthetic theme (Overgrown, Clean, Underground…) has its own rules. In Old Aperture "level transitions take place in metal frame elevators"; going up moves you forward in time, 1950s → 1970s → 1980s. | Valve Developer Community, "Underground (Portal 2)", https://developer.valvesoftware.com/wiki/Underground_(Portal_2) |
| **Dishonored 2, "A Crack in the Slab"** (Arkane, 2016) | Two maps of the same mansion (two times) run at once; the player switches between them in place. Same plan, two finishes. | Gamereactor, Alex Hopley, 2024-09-08, quoting designer Thomas Boucher, https://www.gamereactor.eu/the-time-hopping-level-in-dishonored-2-took-inspiration-from-one-great-bioshock-2-moment-1428993/ |
| **Control** (Remedy, 2019) | Brutalist rules keep space readable; the strange collides with the mundane (Kasurinen); trees and other anchors orient the player in revisited zones. The Ashtray Maze is modular architecture that shifts around the player (GDC 2020 talk by Anne-Marie Grönroos, reported). | Game Developer, "Exploring the world-driven game design of Control", 2020-04-09, https://gamedeveloper.com/design/exploring-the-world-driven-game-design-of-i-control-i- ; Game Informer, Elise Favis, 2019-03-27 |
| **Trades (real world)** | Paperhangers place the final mismatched seam behind or above a door, where it is least noticed. Floors change at reducers, T-mouldings and carpet-to-hard-surface strips. Full period detail in `02_real_buildings.md`. | This Old House, "The best techniques for hanging wallpaper"; Flooring Clarity, "Types of flooring transitions" |

The common rule: **the transition is a piece of the kit, not a property of the texture.** Every studio above owns a door, frame, arch, elevator or glue piece whose only job is to join two looks.

---

## 9. What this means for FrontRooms (input for the plan, not a decision)

### 9.1 Where each pattern lands on our edges

| Our edge (per chunk) | First choice | Second choice | Evidence |
|---|---|---|---|
| Door 1.37 | P1: a frame that owns the joint (casing both faces, jamb lined by the Office side, carpet change under the closed leaf) | P6: light changes at the door | Kane FF 6:52; Static Dead End 1:58; Skyrim door frames; RE doors |
| Open edge 0.89 | P2: turn into a cased opening or a short neck (0.6–1.0 m) | P3 + P4: move the change to the next corner, keep base and carpet | Kane FF#3 34:21; Valve hallway overlap; 2002 photo |
| Arch 0.37 | P2: one finish wraps the whole jamb and head | P9: frame a view into the other zone | Kane FF#3 34:25; Dsc00159 arcade |
| Window 0.33 | P9: a framed preview with its own frame and sill | — | FF#3 30:30; Pitfalls 8:40 |
| Flat seam 0.61 | P3: pilaster or reveal, or no change until a corner | P5: stripped band | 2002 photo; Wikidot Red Rooms |
| Wall free end and posts (0.17 split ends, 3.17 post z-fights) | P10/P3: an end cap or corner guard owned by one side | — | EMG 9:32 (raw wall edge); `02` Rule 3 |
| Height step 10.74 | P8: soffit or bulkhead at the step; a zone change may ride on it | 1–2 steps or a ramp | Fandom Level 1 stairs; Kane stairs; Portal 2 elevators |
| Lamps on border cells | P7: dead or dim lamp on the border cell | P6: the Office lamp colour starts 1 cell early | Kane FF 4:44; The Complex 4:46; FF 2:12 |

### 9.2 Families worth pre-rendering (for the variation agents)

1. **"The frame owns it"** (P1 + P2): casing and lined jambs on every border opening; open edges become cased openings; Office side owns frame, base end and reducer. Shots: shot1 (arch), shot3 (door), shot4 (wide).
2. **"Corner rule + shared constants"** (P3 + P4): walls change only at corners or capped ends; carpet, base or ceiling grid continue 1 cell past the wall change, then change at a strip. Shots: shot2 (corridor across an open edge), shot4.
3. **"Stripped to drywall"** (P5): 1–2 cells on the Level 0 side where the paper is torn off and the Office drywall shows, with glue shadows and a ladder or rolls (era-safe props). It tells the story "the 1990 fit-out is eating Level 0". Shots: shot2, shot4.
4. **"Dark or re-lit buffer"** (P6 + P7): the border cell's lamp is dead or dim, and the far zone's light colour starts at the opening. Cheap; uses existing lamp temperaments. Shots: shot1, shot4.

Not recommended inside the map: P11 black cuts (break the streamed map) and P12 glitches (our seam already reads as a glitch).

### 9.3 Era and ownership notes

- Everything above is era-safe for 1990 (fire doors with push bars, steel frames, reducers, soffits, painted signs). Signs must not carry trademarks or printed dates after 1990.
- Doors, openings, walls, floors and ceilings live in `FrontRoomsMap/*` (map chat). Any of these families is a **contract request**, not a direct edit. Lamp level per border cell is also a map decision; lamp look is the visual chat's.

---

## 10. Media already on disk (reuse; credit from the existing ledgers)

| File | Ledger | What it shows here |
|---|---|---|
| `Research/week02/ip-research/stills/ir01_dsc00161_20020612_082113.jpg` | `ip-research/SOURCES.md` (Commons, copyrighted free use) | three papers, one carpet (§5) |
| `Research/week02/ip-research/stills/ir01_dsc00159_20020612_082017.jpg` | same | the arcade (§5) |
| `Research/week02/ip-research/stills/ir04_kane_ff_level0.jpg`, `clips/ir02_kane_ff_walk.*` | same (Kane FF 48 s; 44–49 s) | baseline Level 0 |
| `Research/week02/ip-research/stills/ir03_a24_store_exterior.jpg`, `ir04_a24_corridor.jpg`, `ir04_a24_troffers.jpg`, `ir05_a24_showroom_chairs.jpg` | same (A24 trailer 2 s, 44 s, 36 s, 97 s) | real store vs Level 0 (§4) |
| `Research/week02/ip-research/clips/ir06_etb_l0_vhs.*`, `ir06_backrooms1998.*`, `ir06_pools.*` | same | the games' looks (§7) |
| `Research/week02/phosphor-narrative/clips/gn_kane_ep24_stretched_wallpaper.*` | `phosphor-narrative/SOURCES.md` (Static Dead End 2:33.5–2:39.5) | P12 |
| `Research/week02/phosphor-narrative/clips/gn_kane_ep16_green_cracks.*` | same (FF#2 12:56.5–13:00.5) | P12 |
| `Research/week02/phosphor-narrative/clips/gn_exit8_rules_sign.*`, `gn_exit8_power_outage.*` | same (IGN Exit 8 trailer) | The Exit 8 (§7) |
| `Research/week01/assets/clips/ETB_1_noclip.*` | `week01/assets/clips/for-slides/INSERT_VIDEOS_INSTRUCTIONS.md` (ETB EA trailer 0:01–0:06) | P11 noclip |
| `Research/week01/assets/clips/DD_4_portal.*` | same (Dark Deception trailer 0:39–0:42) | a glowing exit portal: the primary reference's own transition |
| `Research/week01/assets/clips/EX8_1_loop.*` | same (Exit 8 Steam trailer 0:00–0:06) | the corridor loop |
| `Frontrooms3D/Documentation/research/level_transitions/images/cut_*.jpg`, `shot*_before.jpg` | this folder (`03_cut_inventory.md`) | our problem |

New media wanted: `media_candidates.md` (37 rows, 15 of them priority 1; the top six are the Kane FF#3 neck, the Kane FF fire door, the Kane FF green pocket, Pitfalls lower level, the POOLS ladder-and-stair sequence and the ETB door preview).

---

## 11. Sources ledger (accessed 2026-10-03)

| # | Source | URL | Date | Licence / status | Used for |
|---|---|---|---|---|---|
| S1 | Kane Pixels, *The Backrooms (Found Footage)* | https://www.youtube.com/watch?v=H4dGpz6cnHo | 2022-01-07 | © Kane Parsons; YouTube standard licence; frames viewed, not saved | §3.1 |
| S2 | Kane Pixels, *Backrooms – Informational Video* | https://www.youtube.com/watch?v=ZIFhglHn3W0 | 2022-02-12 | same | §3.2 |
| S3 | Kane Pixels, *Backrooms – Pitfalls* | https://www.youtube.com/watch?v=0XwlWXtpaCM | 2022-05-01 | same | §3.3 |
| S4 | Kane Pixels, *Backrooms – Found Footage #2* | https://www.youtube.com/watch?v=sA5PxGHqpTo | 2022-08-21 | same | §3.4 |
| S5 | Kane Pixels, *Backrooms – Found Footage #3* | https://www.youtube.com/watch?v=acdYs9tPLko | 2024-09-13 | same | §3.5 |
| S6 | Kane Pixels, *Backrooms – Static Dead End* | https://www.youtube.com/watch?v=ZbPaWvqAEq4 | 2025-02-12 | same | §3.6 |
| S7 | Kane Pixels, *Backrooms – Everything Must Go* | https://www.youtube.com/watch?v=ewZx0bnBb30 | 2026-08-17 | same | §3.7 |
| S8 | Wikipedia, "Backrooms (web series)" | https://en.wikipedia.org/wiki/Backrooms_(web_series) | rev. as of 2026-10-03 | CC BY-SA 4.0 | episode list, dates, Blender |
| S9 | Taylor Holmes, "Explaining the Backrooms YouTube videos…" (3 parts) | https://taylorholmes.com/2026/05/29/explaining-the-backrooms-youtube-videos-prior-to-watching-the-movie/ , https://taylorholmes.com/?p=33643 , https://taylorholmes.com/2026/06/02/explaining-the-backrooms-youtube-episodes-18-22/ | 2026-05/06 | © author; summary only | episode context (secondary) |
| S10 | The Ghost in My Machine, Backrooms timeline posts | https://theghostinmymachine.com/2022/06/13/backrooms-found-footage-an-updated-timeline-continued-exploration-of-kane-pixels-backrooms-universe/ | 2022-06-13 | © author; summary only | Pitfalls context (secondary) |
| S11 | A24, *Backrooms* Official Trailer | https://www.youtube.com/watch?v=0HjdiohVOik | 2026-03-31 | © A24 | §4 |
| S12 | Wikipedia, "Backrooms (film)" | https://en.wikipedia.org/wiki/Backrooms_(film) | rev. as of 2026-10-03 | CC BY-SA 4.0 | release, plot, designer |
| S13 | Dezeen, Danny Vermette interview | https://www.dezeen.com/2026/05/29/backrooms-production-design-danny-vermette-interview/ | 2026-05-29 | © Dezeen; paraphrased | portals in a shared wall; 30 combos |
| S14 | Motion Picture Association, "How Production Designer Danny Vermette Made *Backrooms* Real" | https://www.motionpictures.org/2026/06/how-production-designer-danny-vermette-made-backrooms-real-portals-platforms-practical-terror/ | 2026-06 | © MPA; paraphrased | depth = less remembered |
| S15 | Wikimedia Commons, Dsc00161 / Dsc00159 | see `Research/week02/ip-research/SOURCES.md` | 2002-06-12 | copyrighted free use | §5 |
| S16 | Backrooms Wiki (Wikidot): Level 0, Level 1, Level 37 | https://backrooms-wiki.wikidot.com/level-0 , /level-1 , /level-37 | rev. as of 2026-10-03 | CC BY-SA 3.0 | §6.1 |
| S17 | Backrooms Wiki (Fandom): Level 0, Level 1, Level 4, Level 37, Noclipping/The Void | https://backrooms.fandom.com/wiki/Level_0 (and siblings) | rev. as of 2026-10-03 | CC BY-SA | §6.2 |
| S18 | Escape the Backrooms, Steam | https://store.steampowered.com/app/1943950/ | 1.0 2025-10-23 | store page | §7 |
| S19 | iStudioGamer, *Escape the Backrooms – Full Game Walkthrough* | https://www.youtube.com/watch?v=-1VNyWyyk6I | 2026-08-15 | © uploader / Fancy Games | §7 |
| S20 | Wikipedia, "Escape the Backrooms"; Secret Mode news | https://en.wikipedia.org/wiki/Escape_the_Backrooms , https://wearesecretmode.com/news/escape-the-backrooms-release-date | — | CC BY-SA 4.0 / © | dates |
| S21 | The Backrooms 1998, Steam | https://store.steampowered.com/app/1985930/ | 1.0 2025-02-20 | store page | §7 |
| S22 | MR Red Gaming, *The Backrooms 1998 Full Game* | https://www.youtube.com/watch?v=eOhApKl1MOg | 2025-02-25 | © uploader / Steelkrill | §7 |
| S23 | Inside the Backrooms, Steam | https://store.steampowered.com/app/1987080/ | EA 2022-06-20 | store page | §7 |
| S24 | FlyyxGamer, *Inside the Backrooms v0.7 – Full Walkthrough* | https://www.youtube.com/watch?v=mevBMJgn1sk | 2026-08-06 | © uploader | §7 |
| S25 | POOLS, Steam | https://store.steampowered.com/app/2663530/POOLS/ | 2024-04-26 | store page | §7 |
| S26 | iStudioGamer, *POOLS Chapters 1–6 – Full Game Walkthrough* | https://www.youtube.com/watch?v=8D8SumQ0WlQ | 2026-03-02 | © uploader / Tensori | §7 |
| S27 | GameTyrant, "Anemoiapolis: Chapter 1 launches tomorrow"; itch.io page | https://gametyrant.com/news/unique-liminal-space-horror-game-anemoiapolis-chapter-1-launches-tomorrow , https://q-andrew.itch.io/anemoiapolis | 2023-02 | © | developer, date |
| S28 | Full Game Playthroughs, *Anemoiapolis: Chapter 1* | https://www.youtube.com/watch?v=ak_xX9EXWN4 | 2025-02-02 | © uploader | §7 |
| S29 | gg.deals / RAWG, *The Complex: Found Footage* | https://gg.deals/game/the-complex-found-footage/ | 2022-08-18 | listing | developer, date |
| S30 | iStudioGamer, *The Complex: Found Footage – Full Game* | https://www.youtube.com/watch?v=wiAVxk5falU | 2026-04-09 | © uploader | §7 |
| S31 | Backrooms Descent: Multiplayer Horror, Steam | https://store.steampowered.com/app/2232180/ | 2023-10-28 | store page | §7 |
| S32 | The Game Archivist, *Backrooms Descent – Full Game* | https://www.youtube.com/watch?v=NSm65OG5Wdk | 2023-11-07 | © uploader | §7 |
| S33 | The Exit 8, Steam | https://store.steampowered.com/app/2653790/The_Exit_8/ | 2023-11-29 | store page | §7 |
| S34 | Valve Developer Community, "Level Transitions" | https://developer.valvesoftware.com/wiki/Level_Transitions | rev. as of 2026-10-03 | VDC content licence | §8 |
| S35 | Valve Developer Community, "Underground (Portal 2)" | https://developer.valvesoftware.com/wiki/Underground_(Portal_2) | same | same | §8 |
| S36 | Joel Burgess, "Skyrim's Modular Approach to Level Design" | https://www.gamedeveloper.com/design/skyrim-s-modular-approach-to-level-design ; slides https://www.slideshare.net/slideshow/gdc2013-kit-buildingfinal/17728576 | 2013-05-01 | © | §8 |
| S37 | Stephen Trinh, "Resident Evil – Loading Screens and Doors" | https://www.gamedeveloper.com/design/resident-evil---loading-screens-and-doors | 2020-03-04 | © | §8 |
| S38 | Wikipedia, "Resident Evil 7: Biohazard", "Resident Evil Village", "Alien: Isolation" | https://en.wikipedia.org/wiki/Resident_Evil_7:_Biohazard , https://en.wikipedia.org/wiki/Resident_Evil_Village , https://en.wikipedia.org/wiki/Alien:_Isolation | rev. as of 2026-10-03 | CC BY-SA 4.0 | §8 |
| S39 | Gamereactor, Alex Hopley, Dishonored 2 "A Crack in the Slab" | https://www.gamereactor.eu/the-time-hopping-level-in-dishonored-2-took-inspiration-from-one-great-bioshock-2-moment-1428993/ | 2024-09-08 | © | §8 |
| S40 | Game Developer, Control world design (Kasurinen, Maggs) | https://gamedeveloper.com/design/exploring-the-world-driven-game-design-of-i-control-i- | 2020-04-09 | © | §8 |
| S41 | Game Informer, Elise Favis, "The mesmerizing art behind Control" | https://www.gameinformer.com/2019/03/27/the-mesmerizing-art-behind-control | 2019-03-27 | © | §8 |
| S42 | GameSpot, *How Control's Most Ambitious Level Was Created* (Audio Logs) | https://www.youtube.com/watch?v=YJsXZhSsaUk | c. 2020 | © | Ashtray Maze (not yet watched) |
| S43 | This Old House, "The best techniques for hanging wallpaper" | https://www.thisoldhouse.com/walls/the-best-techniques-for-hanging-wallpaper | — | © | kill point behind or above a door |
| S44 | Flooring Clarity, "Types of flooring transitions" | https://www.flooringclarity.com/types-of-flooring-transitions/ | — | © | reducers, carpet strips |

## 12. Limits

- Frames were read at the player's stream quality (360p–720p). Fine detail such as trim profiles is not confirmed; the media candidates would let us check it.
- The Kane Pixels Fandom wiki was behind a bot check, so episode context comes from Wikipedia and two fan blogs (S9, S10), and from the videos themselves.
- Walkthrough uploads are not official. They show one version of each game; patches may have moved exits.
- The A24 film itself was not viewed; §4 relies on the trailer and on published interviews.
