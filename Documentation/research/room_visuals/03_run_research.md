# 03 — Run! research: what a Run! room is and how it should look

Date: 2026-10-03. Status: DONE (Run! research step of the room-visuals workflow, task N2 in `Documentation/VISUAL_CHAT_TASKS.md`).
Read-only. Nothing in the real project was changed. Only this folder was written (this file, `media_candidates.md`, `SOURCES.md`, `images/run_research_r06–r07*.jpg`, `images/run_ref_*.jpg`).

**How this was made**
- Code read in the private clone `scratchpad/proj_rooms` (same code as the real project, see `01_inventory.md` header). Paths below are relative to `Frontrooms3D/`.
- Docs read: `README.md`, `LEVEL_DESIGN_GUIDE.md`, `Documentation/LEVELS_AND_ENTITIES.md`, `LIGHTING_SPEC.md`, `VISUAL_RESEARCH_LOOKDEV.md`, `BACKROOMS_VISUAL_SPEC.md`, `MAP_GENERATION.md`, `LEVEL_MODULE_SPEC.md`, `LEVEL_DESIGNER.md`, `RACE_SLICE.md`, `RELAY_PURSUIT_REDESIGN.md`, `research/relay_pursuit/20_level_design.md` and `30_narrative.md`, `research/wallpaper_motion/30_narrative_phosphor.md` and `20_level_design_phosphor.md`, `research/hunter/03`, `04`, `10`, `research/interactables/06`, `10`, the sound docs, and this folder's `01_inventory.md`, `02_research_index.md`, `04_captures.md`.
- Figma read only (no writes): DESIGN PLAN DP01 (2245:9) and the Week 1 slide "Different rooms, different rules" (2235:735).
- **No media was downloaded.** Every frame here comes from media already on disk:
  - `Research/week01/assets/clips/` (ETB_3_run, DD_3_chase);
  - `Research/week02/ip-research/` and `phosphor-narrative/`;
  - the raw trailer files of the IP-research download Red approved on 2026-10-02 (kept in the session scratchpad that made `Research/week02/ip-research/`; credited from that folder's `SOURCES.md`).
  - Wanted new media is listed in `media_candidates.md` for Red to approve in one batch.
- Numbers on frames come from a small script (`scratchpad/run_research/measure.py`): sRGB → linear, Rec. 709 luminance **Y** (0 = black, 1 = white), percentiles p5 / p50 / p95, and the share of pixels that are **saturated red** by the WCAG rule R/(R+G+B) ≥ 0.8.
- Claims I took only from a search summary, without opening the page, are marked **[search]**. Claims I could not confirm are marked **UNVERIFIED**. Starting numbers for playtest are marked **SP**.

---

## 0. Short answer (for Red)

1. **Run! is not in the game today.** It exists only as the title stream's 4th profile. The stream is forced to Lobby-only, so no player has seen it (`FrontRoomsRoomStream.cs:316`; `01_inventory.md` §3.4). The map, which is the game now, has **no Run zone and no plan for one** (`ZoneTheme { Level0, Office }`, `FrontRoomsMap.cs:16`).
2. **What Run! has always meant in the design:** "A red corridor. The hunter behind. Nothing to read." Tell: the lights turn red. Counter: speed, and the window you learned to break (Week 1, Figma 2235:735). In code, entering a Run room alarms the Relay: it appears behind the door you just shut and starts breaking it (`FrontRoomsHunter.cs:202-219`).
3. **Run! has a natural home in the new pursuit design.** Red chose EGRESS on 2026-10-03: the Relay is what a fire-alarm panel sends; CHASE = full alarm. Red also asked for rooms that call it when you enter. So my proposed definition is:
   > **A Run! room is a back-of-house egress corridor. You can read it before you enter. Stepping in trips the alarm: the corridor drops to emergency power, the Relay is called, and the right move is to run its length to a door you can shut.**
   - It is a trigger room (`ModuleTrigger.Alarm`, proposed in `RELAY_PURSUIT_REDESIGN.md` §9), not a whole level. Choosing this is Red's call (§1.4).
4. **Canon and IPs disagree on light, and the numbers show it** (§3, §6):
   - **Escape the Backrooms, Level !**: every troffer lens is red. 22–68 % of the frame is saturated red. Median Y 0.002–0.004. Value contrast p95/p5 only 1.2–2.2. It reads "danger" instantly but is almost one value.
   - **Kane Pixels, Found Footage**: the flight runs from light into dark. The creature reads as a dark shape in a lit door gap (4.9–7.5 : 1). The goal is a small lit square at the end of a dark corridor.
   - **A24 trailer**: the run happens in full yellow light (median Y 0.35).
5. **Real 1990 has almost no red light.** In a real corridor the only red is the EXIT sign's letters. In a power cut, white battery lamps throw pools of light. Code allows a 40 : 1 bright-to-dark ratio, so pools with dark gaps are correct (§4). Fire alarms in 1990 used bells or horns and red or white flashing lamps; xenon strobes were only becoming required (ADA 1990).
   - So "red everywhere" is **canon**, and "dark with white pools and red signs" is **1990**.
6. **Our Run room today is neither.** Median Y 0.065. Its darkest 5 % sit at 0.027, the highest black level of all rooms. 0 % saturated red. Mean colour 105 / 65 / 43: salmon, not red, not dark.
7. **The gameplay sets hard limits on the look** (§2):
   - sprint 5.5 m/s for 5 s = **27.5 m**; Relay chase 4.2 m/s (tier 1) to 5.4 m/s (tier 5); sight 12 m;
   - one lamp per 3 m cell; lamps beyond **16 m are switched off**, so the far end of a corridor is black unless something is **emissive**;
   - a Standard room module can be at most **4 cells = 12 m** long today. A 24–27 m Run corridor needs a map contract request.
8. **Seven rules for any Run! look** (§5):
   1. value carries the warning, red only confirms it;
   2. the Relay must read as a dark shape against something brighter behind you (≥ 4 : 1);
   3. red stays steady; nothing red flashes over a large area;
   4. any flash is white, small, ≤ 1 per second, synced and paired with a sound;
   5. the brightest thing in the corridor is the door you can shut;
   6. darkness sits between pools, never in pockets (dark is never cover);
   7. 1990 US hardware only: red EXIT letters, no green running man, no brands.
9. **Three directions to render** (§7), split by where the red comes from:
   - **A "Canon Red"**: red lenses, a white hospital corridor;
   - **B "Emergency Power 1990"**: dead tubes, white twin-head pools, red EXIT glows, fire doors;
   - **C "Full Alarm Switch"**: a normal-looking corridor that flips to B's emergency state when you enter.
   - My recommendation, for Red to accept or reject: **C on B's palette**. It is the most readable, the most 1990, and it fits EGRESS.

---

## 1. What Run! means in our game

### 1.1 In code (title stream only)

| Item | Value | Where |
|---|---|---|
| Profile order | Lobby, Shift, Office, **Run**, Exit, then Lobby. Run is profile index 2 of the furnished cycle | `FrontRoomsRoomRule.cs`; `FrontRoomsRoomStream.cs:389-403` |
| In play? | **No.** `lobbyOnlyTitle = true` (`:316`). Only an editor test starts the playable sequence (`Assets/Editor/FrontRoomsStreamVerification.cs:80`) | `01_inventory.md` §0 |
| Room | 11.5 × 12 × 2.9 m box, same as every profile; one door at the far end | `01_inventory.md` §3.0 |
| Tubes | colour (.88, .92, .94), intensity 0.6, range 6.5 m; 70 % dead, 45 % of the rest failing | `:1357, 1371, 1377, 1387` |
| Red | two hanging EXIT signs at z 3.4 and 9.2 m, 2.36 m up, each with a red point light (1, .10, .06), 3.6, range 9.5 m, soft shadows | `:1474-1491` |
| Props | 2 handrails at 0.92 m, 3 teal vinyl chairs, a gurney turned −14°, an IV pole. All boxes and one cylinder | `:1440-1473` |
| Game role | the old stream Relay brain alarms on entry: once the door behind you is shut, the Relay appears on its far side and starts breaking it, so you hear it right behind you (`FrontRoomsHunter.cs:122-126, 202-219`). Only the editor test uses this brain; the game uses `FrontRoomsMapHunter` | `01_inventory.md` §3.4 |

### 1.2 In the design documents

| Source | Date | What Run! is there |
|---|---|---|
| Week 1 slide "Different rooms, different rules" (Figma 2235:735) | 09-24 | **"Level ! · Run"**: "A red corridor. The hunter behind. Nothing to read." Tell: the lights turn red. Counter: speed, and the window you learned to break. Note: the still on the slide appears to be ETB's colourful playroom (1.0 trailer about 1:32), not the red corridor; the clip slot `S12-C_ETB_3_run` is the red corridor (1:34–1:39) |
| `RACE_SLICE.md` | 09-2x | main-route roles `Start → Shift → Office → Run → Exit` |
| DESIGN PLAN DP01 (Figma 2245:9) | 10-01 | the "train of rooms": Lobby, Lobby, Shift, Office, **Run**, Next. "A sprint (5.5 m/s) always beats its chase (4.2 m/s), so getting away is never a choice." The plan moves to "a field of rooms" (the map) |
| `LEVELS_AND_ENTITIES.md` | 10-01 | stream sequence 5: "Utility pipes, junction boxes, warning bars and a red service cue turn movement into the decision". "Entering Run always wakes the Relay" |
| `LIGHTING_SPEC.md` | 10-01 | "Red Run": red #D8493D, 1.15, range 6.2, "threat colour with deeper shadows" (not what the code does) |
| `VISUAL_RESEARCH_LOOKDEV.md` §1 | 10-01 | the wiki canon (long white hospital corridor, dim red from hanging exit signs, chairs and beds); the utility-room look "was not the canonical look and is replaced" |
| `research/interactables/10_spec.md` D5; `06_period_windows.md` §3.3 | 10-02 | reserved Run doors **RN-F** (ward door, push/pull, no latch) and **RN-K** ("STAFF ONLY" steel door), window **W-RN** (white enamel steel, wired glass). Run hospital wall albedo luminance **0.738**, the brightest wall in the game |
| `RELAY_PURSUIT_REDESIGN.md` §4.2, §9 | 10-03 | **instant-trigger rooms**: "readable before entry", each with a reward (shortcut, key, new zone). `ModuleTrigger { None, Alarm, Loud }`. The visual chat must supply the readable props |
| `research/relay_pursuit/30_narrative.md` (EGRESS, Red's pick) | 10-03 | CHASE = "Full alarm, locked to your compartment". The lock-on cue's preferred sound is a **magnetic door-holder release**. Rules: dark is never cover; the building-wide alarm is **silent** (no bell); 1990 hardware |
| `research/wallpaper_motion/30_narrative_phosphor.md` §4 | 10-03 | in CHASE "the lamps drop to emergency level so the paper can be read"; the ink switches to ALARM ROUTE (doors you can shut). Office is "tenant fit-out" and carries no ink |

**Reading.** Every document agrees on four things: a **corridor**, **red as the warning**, **the Relay behind you**, and **running to the next door**. They disagree on everything visual. None of them came from research. The newest design (EGRESS) gives the red a reason: it is the building in alarm.

### 1.3 In the map (what exists today)

- **Zones:** `ZoneTheme { Level0, Office }` × `ZoneHeight { Low 2.4, Standard 2.9, Tall 5.4 }`. No Run theme, no plan for one in `MAP_GENERATION.md` or `LEVEL_MODULE_SPEC.md`.
- **Lamps:** one troffer per 3 m cell (`FrontRoomsMapWorld.cs:853, 1173-1221`), a 162° downward spot. Lamps beyond `lightRadius` **16 m** are off; one lamp in three casts shadows within 9 m (`FrontRoomsLevel0.asset:48-49`). Module lamps can be `Auto, Steady, Stutter, Failing, Dead, Dim, Off` (`FrontRoomsRoomModuleData.cs:30`).
- **Openings:** arches 1.1–1.8 m wide, 2.2 m high, inside zones. Doors 1.0 × 2.1 m only on zone borders. Windows only on borders with a Tall side.
- **Room sizes:** carved rooms are Low 2–3, Standard 2–4, Tall 5–7 cells per side (`FrontRoomsLevel0.asset:30-37`). Modules are 1–8 cells per side, but a module is placed only into a carved room it fits. **So the longest straight a Standard module can have today is 4 cells = 12 m.**
- **Trigger rooms:** proposed only (`ModuleTrigger`, `RoomTriggered(kind, source, tag)`, `RELAY_PURSUIT_REDESIGN.md` §9). Not built.
- **Chase look:** the planned lamp-override layer has `LampFx { Dip, Sag, Warn }`. The chase wave (CHASE only) dips lamps to emergency level as it passes (`20_level_design_phosphor.md` §4, §11.1).

### 1.4 What a Run! room should be (the choice for Red)

| Option | What it is | Fits the brief | Cost and owner | My view |
|---|---|---|---|---|
| **R1 Alarm corridor (trigger room)** | A back-of-house corridor module. Readable from outside (red EXIT sign, fire doors on hold-opens, "ALARM WILL SOUND" bar). Stepping 1.5 m in trips the alarm: emergency power, Relay summoned. Reward: a long straight shortcut to a door you can shut | red corridor ✓, hunter behind ✓, entering certain rooms calls it ✓ (Red, 10-03), EGRESS ✓ | Map: `ModuleTrigger.Alarm` (planned), a long-corridor carve or a module-size exception (contract request). Visual: props, emergency look, a "tripped" state. Sound: device chirp, mag release | **Recommended** |
| R2 Run zone theme | A third `ZoneTheme` (hospital/service), with its own finishes and doors (RN-F, RN-K) | red ✓, corridor only if the generator makes long straights | Map: new theme through every system (Foley surface, reflection zone, kits). Large | Later, if R1 works |
| R3 Chase overlay | Run! is not a place. Any room in CHASE takes the Run! look (lamps to emergency, EXIT signs glow) | red ✓, hunter behind ✓, but it is everywhere, so it stops being special | Visual + map lamp override (planned) | Use as the "tripped" state inside R1 |
| R4 Stream only | Keep Run as a title-stream profile | not reachable | none | No |

---

## 2. Gameplay needs that constrain the look

| Need | Number | Source | What it means for the look |
|---|---|---|---|
| Sprint | 5.5 m/s for 5 s, refills after 1 s at 1 s/s → **27.5 m** per full sprint | `FrontRooms3DGame.cs:153-155` | A Run straight should be about one sprint long: **24–27 m (8–9 cells)**. The goal must read from its start |
| Walk | 3.2 m/s | same | after the sprint the Relay gains 1.0 m/s (tier 1) to 2.2 m/s (tier 5) |
| Relay chase | 4.2 m/s (scene `chaseSpeed`), tier 5 × 1.29 = 5.4 m/s | `FrontRooms3D.unity:1069`; `MAP_GENERATION.md` §6 | from 12 m behind, a full sprint ends with the Relay 18.5 m back (tier 1) or 12.5 m back (tier 5). `images/run_research_r04_chase_numbers.jpg` |
| Relay sight | 12 m, 360°, no reaction time today; proposed ±70° cone, about 0.5 s to notice | scene `sightRange: 12`; `RELAY_PURSUIT_REDESIGN.md` §6 | The Relay must be readable at 12 m behind you: about 125 px tall on a 1080 p screen (`research/hunter/03` §2.2) |
| Door break | 2.5 s at tier 1 (1.3 s at tier 5); doors are single-acting and open away from you | `30_narrative_phosphor.md` §4; `RELAY_PURSUIT_REDESIGN.md` §9 | **Shutting a door is the escape verb.** The door at the end must be the most legible object in the corridor |
| Reading distance while sprinting | 1.5 s of warning at 5.5 m/s = **8 m** | derived | obstacles (chairs, gurneys) must read at ≥ 8 m; nothing small or dark on a dark floor |
| Lamp rhythm | one lamp per 3 m cell: **1.07 Hz** walking, **1.83 Hz** sprinting, **2.67 Hz** for the 8 m/s chase wave | `r04` | spatial rhythm stays under the 3 Hz photosafety line; a pool every 2 cells (6 m) gives 0.9 Hz at sprint |
| Light radius | lamps beyond **16 m** are off | `FrontRoomsLevel0.asset:48` | the far half of a 27 m corridor is unlit. **The goal must be emissive** (EXIT face, lit vision panel, lit doorway) to read past 16 m |
| Fog | exp², density 0.014 → 11 % at 24 m, 16 % at 30 m | `FrontRoomsLook.cs:20-21` | fog does not hide the goal; the light radius does |
| Sign size | an EXIT letter 6 in (152 mm) at 30 m ≈ 4 px tall; at 12 m ≈ 10 px (76° FOV, 1080 p) | NFPA 101 §7.10 [search]; derived | at range the sign reads as a red mark, not a word. Shape and position carry it, not the letters |
| Corridor width | one cell = 3 m, walls 0.16 m → **2.84 m clear** | `LEVEL_MODULE_SPEC.md` §2 | close to a real hospital corridor of 8 ft = 2.44 m (§4.4). A 1-cell corridor reads as institutional without new geometry |
| Arches | 1.1–1.8 m wide, 2.2 m high | `RELAY_PURSUIT_REDESIGN.md` §9 | "readable before entry" must work through a narrow arch: put the device and the sign on the corridor's axis, visible from 2 m outside at 1.6 m eye height |
| Trigger arming | ≥ 1.5 m inside the room | same | the threshold strip can show the corridor's state (armed) safely |
| Photosafety | no lamp changes more than 3 times a second; Reduce Flashing setting | same §7.4 | see §5.6 |
| Dark is never cover | the Relay's sight has no light term (LD R13) | `30_narrative.md` §6 | the Run! dark must not look like a hiding place |
| Relay look still open | four directions A–D; current body #2B2928, head #D8D4C8 | `research/hunter/10`; `04` §9 | design for a **dark body with a pale accent**. It needs a brighter background to read |

---

## 3. Canon and IP references

### 3.1 The Backrooms wiki: Level ! "Run For Your Life" (text canon)

From the Fandom Backrooms wiki page (fetch refused with HTTP 402; content read through search summaries **[search]**) and a fan copy (A-Sync wiki, read):
- a hallway about **10 km** long, built like a **hospital**;
- **white** walls, floors and ceilings;
- **dim red light from exit signs hanging from the ceiling** throughout; one summary says the signs flicker and hang about 10 m apart **[search]**;
- a horde chases you **from the moment you enter**; it adapts to how fast you run;
- **chairs, hospital beds** and sometimes entities block the way;
- side doors are **locked**;
- the exit is a **fire exit door** at the far end; you must open it fast.

What it gives us: the **hospital corridor**, the **hanging EXIT signs as the red source**, **obstacles to steer round**, **locked side doors**, and a **fire exit door as the goal**.

### 3.2 Escape the Backrooms (2022–), Level ! "Run For Your Life!"

Frames: `images/run_research_r01_canon_vs_current.jpg` (top), `r02_value_only.jpg` (top), `r03_chase_refs.jpg` (top), `run_ref_etb_levelbang_0094.jpg`. Source: ETB 1.0 launch trailer, 1:34–1:38 (VHS stamp PM 0:31:17–0:31:36).

- **Light:** every 2'×4' troffer lens is red. The red **is** the ceiling. Small EXIT signs hang over the centre line at the troffer rows. Walls and floor get almost nothing.
- **Space:** a long straight hospital corridor with doors on both sides, handrails, a reflective floor (the only floor read is the red lenses reflected in it).
- **Play** (guides, read): chased by Partygoers and a Smiler; run in a straight line, dodge chairs, cabinets, tables, mattresses, monitors and falling medical equipment, jump; sprint does not run out; the exit is an open door at the end.
- **Measured** (§6): median Y 0.002–0.004; p95/p5 contrast 1.2–2.2; **22–68 % of pixels saturated red**; mean colour about (42, 2, 2).
- **Value-only view** (`r02`): all the information is in the troffer row and the EXIT sign. The walls, the doors and the other player almost vanish.
- **Lesson:** it reads as danger at once and draws a perfect leading line (the lens row). But it is nearly one value. A dark Relay in it would be invisible, and so would the obstacles. If that field flashed, it would break the WCAG red-flash rule.

### 3.3 Kane Pixels, "The Backrooms (Found Footage)" (2022)

Frames: `images/run_research_r05_kane_backofhouse.jpg` (6:08–6:56), `r06_kane_silhouette.jpg` (8:07–8:29), `run_ref_kane_ff_0370_tunnel.jpg`, `_0398_fire_exit.jpg`, `_0489_silhouette.jpg`, `_0506_lit_end.jpg`.

Two sequences matter.
- **Back of house (6:08–6:56):** out of the dark, the camera finds a **brightly lit concrete service tunnel** framed in a black wall. There is a green running-man sign over it, a "DANGER · HIGH VOLTAGE" placard and two figures inside. It runs through it (motion blur). Then a **white office corridor with filing cabinets**, a grey **door with a panic bar** under a "Fire exit · Keep clear" sign, a **dark concrete stairwell**, and a panic-bar door back into Level 0.
  - Measured: p95/p5 contrast **8.1–9.5**; 0 % red. The lit space is the corridor; the dark is everything around it.
  - The signs are UK/ISO style (green running man). **Not 1990 US:** take the spaces and the panic bars, not the signs.
- **The flight (8:00–8:29):** running through lit Level 0, the camera ducks into a **dark room**. The creature stands in the **lit gap of the doorway** behind. It runs on down a **dark corridor with one small lit square at its end**. The creature lunges under a lit troffer.
  - Measured: the lit door gap p90 Y 0.36–0.41 against the dark body: **4.9–7.5 : 1**. The dark corridor's whole frame sits at p95/p5 1.4, but its one lit square is the goal.
- **Lesson:** the threat reads as a **dark shape against the light behind you**, and the goal reads as **the only light ahead**. No colour is needed.

### 3.4 Other Backrooms work

- **The Backrooms 1998** (Steelkrill Studio, trailer 1:16 and 2:24–2:28; `run_ref_backrooms1998_0076_red.jpg`, `r07` bottom left): a red lamp glowing at the end of a dark corridor, red-lit rooms under a VHS haze. Measured: p95/p5 1.25–1.29, mean (51, 38, 34): a muddy red-brown, not saturated. A single red source at the end of a corridor reads as "something is there", not as a route.
- **A24, "Backrooms" (2026) trailer** (0:27; `run_ref_a24_trailer_0027_run.jpg`, `r07` top left): the protagonist breaks into a run across fully lit Level 0. Median Y 0.35, contrast 10.6. The film keeps the run in the light.
- **POOLS** (Tensori): no chase. Not used.

### 3.5 Chase games

- **Dark Deception** (Glowstick Entertainment; Red's primary reference; `r03` bottom, `run_ref_darkdeception_chase.jpg`): narrow hotel and marble corridors. Wall sconces make warm **pools**, small red wall lamps mark rhythm, and a carpet runner or checker strip draws the **centre line**. The monkeys come up the lit axis. Measured: 0 % red; median Y 0.003–0.012; the brightest 0.5 % at Y 0.37–0.49 (the sconces). It is dark, but the pools carry value.
- **Mirror's Edge** (DICE, 2008) "Runner Vision": route objects turn **red** as you approach, so the path reads at running speed. In Catalyst it became a red guide line ("follow the red", EA, **[search]**). Lesson: red works as a **route colour** when it is small and on objects, against a neutral world.
- **Left 4 Dead** (Valve, 2008) alarm cars: alarmed cars are a distinct colour, and their glass has a **blinking** variant (`prop_car_glass`); touching one triggers a horde (Valve Developer Wiki **[search]**). This is the closest game match to an EGRESS trigger room: a readable object, a light and a sound that go together, and a choice.

### 3.6 Late-80s and early-90s film chase spaces (period lighting language)

No frames are on disk. Clips are listed in `media_candidates.md`. What each is expected to show (from memory; timestamps **UNVERIFIED** until the clips are approved and checked):
- **Jacob's Ladder** (1990): a gurney ride down a hospital corridor at night. It shows the hospital, the gurney and the corridor's rhythm.
- **The Exorcist III** (1990): the night hospital corridor in one long static take. Fluorescent and nurse-station light, and dread from distance, not darkness.
- **Terminator 2** (1991): the Pescadero hospital escape. White institutional corridors, locked doors, a pursuer in fluorescent light.
- **Aliens** (1986) and **Alien** (1979): emergency red light and strobes during the countdowns. This is the film origin of "red light = run".

### 3.7 What the references agree and disagree on

| Question | ETB | Kane | A24 | Dark Deception | Real 1990 (§4) |
|---|---|---|---|---|---|
| Where the red comes from | every lens | none | none | small wall lamps | EXIT letters only |
| Where the threat reads | barely (all dark red) | dark in a lit gap | in the light | up the lit axis | — |
| Where the goal reads | the lens row's end | the lit square ahead | — | the pool ahead | EXIT sign over the door |
| Contrast p95/p5 | 1.2–2.2 | 8–9.5 (back of house) | 10.6 | 1.6–2.7 | up to 40 : 1 allowed in a power cut |

**Takeaway.** Use red for **signal**, not for **fill**. Keep a **neutral or white light** that makes pools and silhouettes. Make the **goal** the brightest, most emissive thing.

---

## 4. Real 1990 references (US)

Real photos on disk: only `Research/week02/phosphor-narrative/stills/gn_tritium_exit_sign.jpg` (a self-luminous EXIT sign, about the 1970s, CC BY-SA 4.0), `gn_photoluminescent_exit_sign.jpg` (modern, for physics only) and `gn_scandinavian_star_1990-04.jpg` (the 1990 ferry fire that led to low-level escape lighting). Everything else is in `media_candidates.md`.

### 4.1 EXIT signs
- **Light source in 1990:** mostly **incandescent**, often a pair of lamps behind the face. They were dim and warm. Fluorescent signs existed. LED signs were new. Tritium (self-luminous) signs had been used since the 1970s (Wikipedia, Exit sign).
- **Colour:** in the US "red or green, but traditionally red". **Red is required** in New York City, Chicago/Illinois and Rhode Island. Some cities required green (Wikipedia). The green running man (ISO) was not used in US buildings in 1990.
- **Code numbers (current NFPA 101 §7.10; the 1990 wording is UNVERIFIED) [search]:** letters at least **6 in (152 mm)** high with **3/4 in (19 mm)** strokes. No point in an exit-access corridor more than **100 ft (30 m)** from the nearest sign.
- **For us:** a hanging double-faced sign with red letters on a pale face, lit from inside, warm and slightly uneven. Its light reaches only about 1–2 m around it; the red spill on walls is faint. The current `Run_ExitSign` uses Arial Bold; the face must come from the period type kit (`FONTS_PERIOD_1990.md`).

### 4.2 Emergency lighting units
- **Look:** a battery box with **two adjustable lamp heads** ("twin-head", often called bug-eyes), wall-mounted high near doors and corners. Heads were sealed-beam incandescent (PAR 36 type) (Wikipedia, Emergency light).
- **Behaviour:** they switch on by themselves when mains power fails and must run **90 minutes** (Wikipedia).
- **Code numbers (current NFPA 101 §7.9.2.1; 1990 wording UNVERIFIED) [search]:** initial **1 ft-candle (10.8 lux) average** and **0.1 fc minimum** along the egress path, declining to 0.6 / 0.06 fc after 90 min; **maximum-to-minimum ratio no more than 40 : 1**. A normal corridor is lit at roughly 10–20 fc (ESTIMATE), so emergency power is about **1/10 to 1/20** of normal: 3–4 stops darker.
- **Tests:** monthly 30 s and yearly 90 min (current NFPA 101; EGRESS already uses this as the "sag" fiction).
- **For us:** the honest 1990 emergency look is **white, hard-edged pools** from small heads at about 2.4 m, aimed down the corridor and at doors, with deep gaps between them (40 : 1 allowed). It is **not** red.

### 4.3 Fire alarm signals
- **Audible:** bells and electromechanical horns. "The majority of audible notification appliances installed prior to 1996 produced a steady sound"; the temporal-three pattern came in 1996 (Wikipedia, Fire alarm notification appliance). Hospitals used chimes (EGRESS notes; UNVERIFIED).
- **Visual:** 1970s–80s visual signals were mostly **red or white incandescent** flashing lamps; horn/strobe combinations existed from 1976. The **ADA (July 1990)** pushed clear xenon strobes (15 cd, about 1 flash per second) into new and retrofitted buildings (Wikipedia).
- **Sync and epilepsy:** early-1990s strobes caused seizures in some people. Codes later required strobes in one field of view to flash **in sync** (NFPA 72: more than two visible strobes must be synchronised) [search].
- **For us:** a 1990 corridor in alarm has **steady** sound (or none: EGRESS says the building-wide alarm is silent) and, at most, **one small flashing lamp at about 1 Hz**, red-lensed or clear. A whole corridor pulsing red is a film language, not a 1990 one.

### 4.4 Fire doors, panic bars, hold-opens, hospital corridors
- **Panic bar:** a spring-loaded bar across the door at hip height. Invented after the 1883 Victoria Hall disaster (UK patent 1892) and the 1903 Iroquois Theatre fire; Von Duprin sold the first by 1908. Bars can be fitted with alarms (Wikipedia, Crash bar). EGRESS's alarm door uses the sign "EMERGENCY EXIT ONLY — ALARM WILL SOUND".
- **Magnetic hold-open:** an electromagnet on the wall or floor holds a fire door open; when the alarm panel cuts power, the door **closes by itself**. A person can always pull it free (Wikipedia, Electromagnetic door holder [search]). This is EGRESS's preferred lock-on sound.
- **Hospital corridors [search]:** corridors where beds move are **8 ft (2.44 m)** wide. Floors are split into **smoke compartments**, with no point more than **200 ft (61 m)** from a smoke-barrier door. Cross-corridor doors come in pairs, often held open magnetically and self-closing (current NFPA 101; 1990 values UNVERIFIED).
- **Wired glass** in fire doors and corridor lights: polished wired glass was no longer made in the US after a 1992 ruling (`research/interactables/06_period_windows.md` §2.3). So in 1990 it is current and common.
- **For us:** cross-corridor double doors on hold-opens, with wired-glass vision panels, are the most "1990 hospital" object we could add. When the alarm trips they **swing shut on their own**. Behind you, that is a door between you and the Relay (EGRESS's ALARM ROUTE). Ahead of you, it is a door you push through at a run.

### 4.5 Safety colours and back-of-house finishes
- **OSHA 29 CFR 1910.144 (since 1974):** red = fire protection equipment, danger, stop; yellow = caution and physical hazards (tripping, striking against). The page does not mention black/yellow stripes.
- **Back of house (ESTIMATE, to confirm with photos):** painted concrete block, VCT floor, bumper rails and steel corner guards on walls where carts run, exposed conduit, red pull stations and bells, fire extinguisher cabinets, stencilled "FIRE DOOR · KEEP CLOSED", OSHA "NOT AN EXIT" signs (since 1974; `30_narrative_phosphor.md` §3).
- **For us:** red appears only on **fire equipment** (pull station, bell, extinguisher cabinet, EXIT letters). Yellow appears only on **hazards** (a step, a bumper, a door swing). That gives the corridor small, honest red accents without red light.

### 4.6 The 1990 egress disaster
- MS *Scandinavian Star*, 7 April 1990, 159 dead. Smoke hid exits at ceiling level. Rules for low-level escape lighting on ships followed in 1992–93 (`30_narrative_phosphor.md` §3). For us, this is the reason exit guidance sits **low and on the walls** (the glow ink), not only overhead.

---

## 5. What makes a chase space readable and fair

### 5.1 Value before colour
- The Level Design Book: light the critical path first, then encounters; "brightly lit doorways help players understand the flow" (citing Magnar Jenssen) (book.leveldesignbook.com, Lighting, read).
- GDC 2018, "Level Design Workshop: Invisible Intuition" (David Shaver, Robert Yang): blockmesh and lighting guide players without waypoints, through contrast and leading lines [search; talk not watched].
- Our own hunter research: "no important information in hue"; test every Relay concept as a greyscale thumbnail (`research/hunter/03` §3).
- Measured lesson (§3.7): ETB's red field has a value contrast of 1.2–2.2; Kane's back-of-house corridor has 8–9.5. **Rule: the Run! frame in greyscale must still show the route, the obstacles, the door and the Relay.**

### 5.2 Leading lines
- ETB: the lens row. Dark Deception: the carpet runner and checker strip. Kane: the tunnel's lamp row.
- Ours: the troffer row exists on every cell. In Run! the **line of pools and hanging signs** should run down the centre, and the handrails at 0.92 m give two side lines at hand height.

### 5.3 Lamp rhythm
- One lamp per 3 m cell passes at 1.83 Hz when sprinting (`r04`). Pools every 2 cells pass at 0.9 Hz. That is a pulse you feel without any flashing.
- Keep any spatial rhythm the camera sweeps through **under 3 Hz**.

### 5.4 Colour as a warning
- Red works best when it is **rare and on objects**: Runner Vision's red objects, L4D's alarm car glass, the real EXIT letters.
- When red is the fill (ETB), the warning is instant, but everything else is lost.
- Our world is already warm and yellow (Level 0) or olive (Office). A **white** emergency light with **red** signs separates Run! from both by value *and* hue.

### 5.5 Sound and light together
- In real alarms, sound and light are one signal. Codes sync the strobes (§4.3).
- L4D's alarm car: blinking glass and siren together.
- Our plan already pairs them: every lamp dip raises `LampDipped(cell, pos)` so fixtures can buzz or tick (`RELAY_PURSUIT_REDESIGN.md` §7.1). The lock-on cue is the mag-release thunk (EGRESS).
- **Rule for Run!:** every light change has a sound partner (ballast clunk, relay click, door-holder release). Nothing flickers silently, so the player learns that the light is the **alarm**, not a radar for the Relay.

### 5.6 Photosafety
- WCAG 2.3.1 (read): no more than **3 general flashes and 3 red flashes** in any 1 s. A "red flash" is any opposing change involving **saturated red** (R/(R+G+B) ≥ 0.8). The flashing area must stay under about **25 % of a 10° field** (341 × 256 px at 1024 × 768).
- ETB frames are 22–68 % saturated red. **A pulsing red field would fail the red-flash rule.**
- **Rule:** red light, if any, is **steady**. Flashing elements are **white or warm, small, ≤ 1 Hz, synced**. Under Reduce Flashing they go steady.

### 5.7 Colour vision
- About **8 % of men** have a colour-vision deficiency, almost all red-green (PMC review, NEI figures [search]).
- For a protan player red light looks **dark**, so a red-lit corridor is simply dark. Value (§5.1) and shape (the EXIT box, the door) must carry the read.

### 5.8 Fairness checklist for any Run! design
1. From 2 m outside the entrance, at 1.6 m eye height, you can see **the device** (bar, sign or detector) and **the far door**.
2. In greyscale, the door at the end is the brightest object, or the second after a lamp.
3. Glancing back at 12 m, the Relay's dark body sits against a surface at least **4 : 1** brighter.
4. Every obstacle reads at **8 m** while sprinting, and none needs a jump (we have no jump).
5. A full sprint (27.5 m) reaches the door, or a door, from the trigger point at tier 1.
6. No dark pocket you could mistake for a hiding place.
7. No flash above 3 Hz, no red field flashing, Reduce Flashing honoured.

---

## 6. Measurements (all frames)

Y = linear luminance (0–1). Contrast = (p95 + 0.05) / (p5 + 0.05). "Sat. red" = pixels with R/(R+G+B) ≥ 0.8 (WCAG). Mean = sRGB 0–255.

| Frame | Y p5 | Y p50 | Y p95 | Y p99.5 | Contrast | Sat. red | Mean RGB |
|---|---|---|---|---|---|---|---|
| ETB Level ! 1:34.6 | 0.0012 | 0.0038 | 0.0212 | 0.152 | 1.39 | **67.7 %** | 42 / 2 / 2 |
| ETB Level ! 1:35.2 | 0.0009 | 0.0031 | 0.0599 | 0.140 | 2.16 | 54.9 % | 42 / 1 / 1 |
| ETB Level ! 1:36.6 | 0.0003 | 0.0016 | 0.0084 | 0.124 | 1.16 | 26.1 % | 26 / 2 / 2 |
| ETB Level ! 1:37.1 | 0.0003 | 0.0018 | 0.0090 | 0.128 | 1.17 | 21.9 % | 26 / 2 / 2 |
| Backrooms 1998 1:16 | 0.0153 | 0.0209 | 0.0341 | 0.139 | 1.29 | 0 % | 51 / 38 / 34 |
| Backrooms 1998 2:24 | 0.0154 | 0.0226 | 0.0316 | 0.113 | 1.25 | 0 % | 65 / 32 / 32 |
| Kane FF 6:10 tunnel | 0.0037 | 0.0125 | 0.387 | 0.698 | **8.14** | 0 % | 49 / 51 / 46 |
| Kane FF 6:28 white corridor | 0.0059 | 0.0519 | 0.483 | 0.831 | **9.52** | 0 % | 83 / 82 / 77 |
| Kane FF 6:38 fire exit door | 0.0169 | 0.0460 | 0.279 | 0.344 | 4.92 | 0 % | 86 / 89 / 86 |
| Kane FF 8:07.5 silhouette | 0.0026 | 0.0035 | 0.118 | 0.418 | 3.20 (door gap 4.9) | 0 % | 25 / 24 / 9 |
| Kane FF 8:09 silhouette | 0.0025 | 0.0032 | 0.0445 | 0.380 | 1.80 (door gap 7.5) | 0 % | 17 / 17 / 8 |
| Kane FF 8:26 lit end | 0.0024 | 0.0056 | 0.0242 | 0.084 | 1.41 | 0 % | 18 / 20 / 4 |
| A24 trailer 0:27 run | 0.0062 | **0.350** | 0.547 | 0.751 | **10.63** | 0 % | 156 / 146 / 79 |
| Dark Deception (DD_3_chase poster) | 0.0037 | 0.0120 | 0.0363 | 0.113 | 1.61 | 0 % | 31 / 30 / 34 |
| Dark Deception hotel 0:35.3 | 0.0005 | 0.0050 | 0.0862 | 0.489 | 2.70 | 0 % | 32 / 24 / 20 |
| **Ours: title Run wide** | **0.0267** | 0.0648 | 0.170 | 0.275 | 2.87 | **0 %** | **105 / 65 / 43** |
| Ours: title Run doorway | 0.0174 | 0.0453 | 0.196 | 0.272 | 3.65 | 0 % | 78 / 70 / 51 |
| Ours: title Lobby wide | 0.0212 | 0.0899 | 0.207 | 0.595 | 3.61 | 0 % | 93 / 82 / 46 |
| Ours: map Level 0 Standard | 0.0134 | 0.0911 | 0.313 | 0.438 | 5.72 | 0 % | 99 / 85 / 45 |
| Ours: map dead lamp | 0.0141 | 0.0529 | 0.155 | 0.210 | 3.19 | 0 % | 75 / 66 / 32 |
| Ours: map Office | 0.0064 | 0.0657 | 0.198 | 0.616 | 4.40 | 0 % | 63 / 74 / 55 |

Notes:
- Video frames carry their sources' grading and compression (VHS overlays on ETB, 1998 and Kane). Treat them as **look** evidence, not photometry.
- Our frames are the `04_captures.md` set (72° FOV, seed 516574485).
- Our Run room has the **highest black level** (p5 0.027) and **no saturated red**. It is the opposite of both references: lifted, not dark; salmon, not red.

---

## 7. Inputs for the three Run! direction renders

These are research inputs for the direction agents, not decisions. Every number is **SP**. All three share the R1 role (§1.4): a 1-cell-wide corridor, 8–9 cells (24–27 m) long, ending in a door you can shut, in its **tripped** state (and C also in its armed state). Use RN-F / RN-K doors, `Run_Wall` (white, 0.738), `Run_Floor` VCT, handrails at 0.92 m, and the hanging double-faced EXIT signs with a period face.

| | **A · Canon Red** | **B · Emergency Power 1990** | **C · Full Alarm Switch** |
|---|---|---|---|
| Pitch | The wiki and ETB's Level !: the ceiling glows red | What a real 1990 hospital corridor looks like in a power cut | The corridor looks normal until you step in; then the building goes to emergency power |
| Where the red comes from | red troffer lenses (every cell, or every other cell) + EXIT signs | EXIT letters, pull stations, bells, extinguisher cabinets only | the same as B, plus one small red-lensed flasher by the trip device |
| Fill light | red lens glow, emissive, about (0.9, 0.08, 0.05); little bounce | tubes dead; **white twin-head pools** every 2 cells, warm-white about 3000 K, hard edges, 1/10–1/20 of normal (≈ Y 0.01–0.03 on the floor in a pool, ≈ 40 : 1 to the gaps) | **armed:** normal Standard tubes. **Tripped:** tubes clunk off in a wave from the device (`LampFx.Dip`, ≥ 0.3 s attack), pools come on (B) |
| Goal (the door) | the last troffer row + an EXIT over the door | an emergency head aimed at the door; lit wired-glass vision panel (emissive) | same as B; plus the cross-corridor fire doors on hold-opens release **behind** you |
| Relay read (12 m back) | weak: dark body on dark red. Test it; it needs a rim term or white pools | good: dark body crossing white pools | good (as B) |
| Photosafety | the red field must never flash | pools steady; one 1 Hz flasher, small | the trip wave is one pass, ≤ 3 changes/s per lamp; one 1 Hz flasher |
| Era (1990) | weak: red lenses are not period | strong | strong |
| Canon | strong | medium (red signs, hospital, beds, fire door) | medium |
| Fits EGRESS | partly (a red alarm look) | yes (emergency power) | best (the alarm is visible as it happens) |
| What to render | wide from 2 m outside the entrance, the corridor at 1.62 m eye from the trip point, a greyscale copy, the look back at a Relay stand-in 12 m behind, the door from 8 m | same set | same set **twice**: armed and tripped, same camera |
| Cost (rough) | troffer lens variant, lamp colour per module | twin-head emergency unit prop + light (new kit), lamp mode Off per cell | B + a scripted state change (map lamp override + trigger), hold-open doors (map contract) |

**Pass/fail for all three:** the §5.8 checklist, on frames, in greyscale too.

---

## 8. Open questions for Red

1. **Role:** is Run! an alarm corridor that calls the Relay (R1, recommended), a whole zone (R2), or the look of every chase (R3)?
2. **Red light or red signs?** Canon red fill (A), or 1990 white pools with red signs (B/C)?
3. **Silent alarm:** EGRESS says the building-wide alarm makes no sound. May the tripped corridor have one local device sound (a door-alarm horn chirp, the mag release thunk), as the narrative chat allows?
4. **Hold-open fire doors** that shut behind you on alarm: a new map object. Worth a contract request to 关卡设计?
5. **Length:** a 24–27 m straight needs either a corridor carve or a module-size exception (today Standard max is 12 m). Which should we ask the map chat for?
6. **Media:** approve the batch in `media_candidates.md` (real 1990 hardware photos and four film clips)?

---

## 9. Images made for this report

All JPG q85, 1920 px wide or less, in `images/`. Boards have 8 px gutters and no captions (Figma grammar).

| File | Content | Source and time |
|---|---|---|
| `run_research_r01_canon_vs_current.jpg` | top: ETB Level ! (1:34.6, 1:36.6); bottom: our Run room wide and its hanging EXIT sign | ETB 1.0 launch trailer; `room_title-run_wide.jpg`, `room_title-run_sign.jpg` (`04_captures.md`) |
| `run_research_r02_value_only.jpg` | the same four, luminance only | derived |
| `run_research_r03_chase_refs.jpg` | top: ETB Level ! about 1:35.6, 1:37.1, 1:37.5 (VHS 0:31:21, 0:31:36, 0:34:30); bottom: Dark Deception hotel corridor empty (0:35.3), with the monkeys (0:36.3), marble corridor (0:38.5) | ETB 1.0 trailer; `Research/week01/assets/clips/DD_3_chase.mp4` (Enhanced Trailer 0:35–0:39; frames matched to the clip at 0.3, 1.3 and 3.5 s) |
| `run_research_r04_chase_numbers.jpg` | diagram: Relay 12 m behind, five seconds of stamina at tier 1 and 5, lamp rhythm vs 3 Hz | code numbers (§2) |
| `run_research_r05_kane_backofhouse.jpg` | Kane FF back-of-house: tunnel, blur, white corridor, fire exit door, stair door, back to Level 0 | Kane FF 6:10–6:56 |
| `run_research_r06_kane_silhouette.jpg` | Kane FF flight: creature in the lit door gap (8:07.5, 8:09), dark corridor with a lit end (8:26), lunge (8:29) | Kane FF |
| `run_research_r07_chase_light_spectrum.jpg` | full light (A24 0:27), lit tunnel in dark (Kane 6:10), red lamp at the end (1998, 1:16), red field (ETB 1:35.2) | as named |
| `run_ref_etb_levelbang_0094.jpg` | single frame, ETB Level ! 1:34.6 | ETB 1.0 trailer |
| `run_ref_kane_ff_0370_tunnel.jpg`, `_0398_fire_exit.jpg`, `_0489_silhouette.jpg`, `_0506_lit_end.jpg` | single frames | Kane FF 6:10, 6:38, 8:09, 8:26 |
| `run_ref_a24_trailer_0027_run.jpg` | single frame | A24 trailer 0:27 |
| `run_ref_backrooms1998_0076_red.jpg` | single frame | The Backrooms 1998 trailer 1:16 |
| `run_ref_darkdeception_chase.jpg` | the week 1 poster frame of DD_3_chase | Dark Deception Enhanced Trailer (URL not logged) |

`r01`–`r05` were made by the first attempt of this same step (11:32–11:37) and checked again here; `r06`, `r07` and the `run_ref_*` singles were added at 11:57. Every row and its URL is in `SOURCES.md`.

---

## 10. Sources

Full ledger with URLs, access date and licence: `SOURCES.md` (this folder), section "03 Run! research". In short:
- **Canon:** Backrooms Fandom wiki "Level !" [search; page refused 402]; A-Sync Backrooms wiki "level run" (read); Gamer Journalist ETB guide (Aidan Lambourne, 2022-08-22, read); gameplay.tips ETB levels guide (2023-11-13, read).
- **Video (on disk, Red-approved download 2026-10-02):** ETB 1.0 launch trailer; Kane Pixels "The Backrooms (Found Footage)"; A24 "Backrooms" official trailer; The Backrooms 1998 trailer. Week 1 clips: ETB_3_run, DD_3_chase.
- **1990 hardware:** Wikipedia "Exit sign", "Emergency light", "Fire alarm notification appliance", "Crash bar" (read); "Electromagnetic door holder" [search]; NFPA 101 §7.9.2.1 and §7.10 via DMF Lighting and MeyerFire [search]; health-care corridor and smoke-compartment rules via ICC and Allegion [search]; NFPA 72 strobe sync via EC&M [search]; OSHA 29 CFR 1910.144 (read).
- **Design:** The Level Design Book, Lighting (read); GDC Vault 1025179 "Invisible Intuition" [search]; EA "Follow the red" and Wikipedia "Mirror's Edge" [search]; Valve Developer Wiki `prop_car_alarm` [search]; W3C WCAG 2.1 Understanding 2.3.1 (read); PMC colour-vision review [search].
