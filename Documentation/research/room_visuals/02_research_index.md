# 02 — Index of all visual research (docs, Figma sections, tools, media)

Date: 2026-10-03 (about 11:40). Status: DONE (index step of the room-visuals workflow, N2).
Read-only. This file links the work; it does not repeat it.

**How to read it**
- One line per item: **what it decided**, then where it is and its status.
- Paths are relative to `Frontrooms3D/Documentation/` unless they start with `Assets/`, `Tools/`, `Verification/` or `Research/` (the course folder `../Research/`).
- Figma: file `0tCbAiVUlrPId3RWd9LRif`, page **2099:76** ("Indivicual Game Project"). Node ids and positions were read with `get_metadata` at 11:0x today.
- Owner codes: **VIS** = visual chat (游戏视觉); **MAP** = map / level chat (关卡设计); **GFX** = graphic chat (平面视觉); **SND** = sound chat (声音); **SYS** = systems chat (系统设计); **NAR** = narrative chat; **WP** = wallpaper-print chat; **WEB** = WebGL perf session; **RED** = Red's own deck.
- Status: **DECIDED** (Red or the owning chat decided; may be built or not), **BUILT** (in the real project), **CLONE** (built in a private clone, not promoted), **PROPOSAL** (waits on Red), **RESEARCH** (facts, no decision), **RUNNING**, **STALE** (superseded; read only for history).

---

## A. Figma sections on page 2099:76

Listed by position. Do not duplicate them; link them.

| Section (node) | Position (abs) | Owner | Frames | What it decided / shows | Status |
|---|---|---|---|---|---|
| Week 1 · PRESENTATION · Razor & Outline (**2127:24**) | −383, −1046 | RED | 16 | The pitch. Visual items: **"Different rooms, different rules" (2235:735)**: Level 0 "the room that moves", Level 4 "the office that offers", **Level ! "A red corridor. The hunter behind. Nothing to read." Tell: the lights turn red. Counter: speed, and the window you learned to break.** S07 "IP — Escape the Backrooms" (2127:68) with the Level ! "run for your life" clip (1:34–1:39) and the lighting target. "THE EXIT 8" (2235:159): one looping corridor, lighting target. "Dark Deception" (2235:137): primary reference. "Three ways out" (2235:626): key, door, glass. "Level design / Environmental experience" (2235:757): atmosphere is information | DECIDED (pitch). Note: `BACKROOMS_VISUAL_SPEC.md` cites the Exit 8 frame as 2127:56; that node is gone, use 2235:159 |
| ↳ FRONTROOMS · IP RESEARCH (**2312:852**, nested in 2127:24) | −383, 4212 | GFX | IR01–IR06 | IR01 "One bad photo" (the 2002 Oshkosh photo), IR02 "One room, retold" (2002 → 2026 → FrontRooms), **IR03 "Set in 1990"** (on-screen dates), IR04 "Yellow by accident" (white balance, not yellow paper), IR05 "Copy, paste, pile" (film piles), **IR06 "The games"** (ETB, Backrooms 1998, POOLS, and a "FrontRooms · Run zone" still) | DECIDED (era 1990; yellow = grade) |
| Week 1 DEVELOPER DIARY #1 (**2169:2062**) | −383, −2584 | RED | D01–D02 | Next steps; AI log | history |
| FRONTROOMS · DESIGN PLAN (**2245:8**) | 7897, −1046 | MAP + RED | DP01–DP08 | DP01: from a train of rooms (Lobby, Lobby, Shift, Office, Run) to a field of rooms. DP03 "No doors, until it matters": pillar halls, off-centre arches, pockets; doors only low ↔ standard, windows into tall. DP04: chunks. **DP08 "It gets harder"**: tiers raise the Relay and, on the slide, **Tall share 10 → 30 %** and **rooms with failing lights 25 → 65 %** | DECIDED; built as the map. DP08's tall-share and failing-room rows are **not** what the code does (tiers change per-lamp odds only, `FrontRoomsLevelProfile.cs:164-171`) |
| FRONTROOMS · UI MOCKUP · HUD + screens (**2256:8**) | 7897, 2103 | UI (VIS/MAP) | UI01–UI09, S17 | HUD kit, title, calm/chase play, prompts, pause, display settings, caught | DECIDED (UI) |
| Week 2 · DEVELOPER DIARY #2 (**2305:927**) | 7897, −2584 | RED | 1–6 | weekly log | history |
| FRONTROOMS · SOUND RESEARCH (**2339:858**) | 7897, 6409 | SND | SR01–SR08 | damp carpet, steps, Foley, door in nine moments | not visual |
| FRONTROOMS · HUNTER · EARLY VISUAL PROPOSALS (**2331:852**) | 7897, 9475 | VIS | HR01–HR16 | HR01 the Relay today; HR02 "the box it lives in" (room limits: 2.05 m, doors, 2.4 m ceilings); **HR03 "What the Backrooms teach"** (IP rules, used by later research); HR04–HR08 directions A Floor Sample, B Night Shift, C Duplicate, D Delivery; HR09–HR15 the same as squeezed giants; HR16 what Red decides | PROPOSAL (W1 waits on Red) |
| FRONTROOMS · RELAY PURSUIT · AUDIT + REDESIGN (**2441:3804**) | 7897, 15097 | SYS + MAP + NAR | RP01–RP10 | why you always run; the new loop; **RP05 three warnings** (your room's lamps flicker → muffled steps → lock-on cue); **RP07 rooms that call it** (trigger rooms with a 1990 device); **RP08 the alarm that sends it** (EGRESS fire-alarm fiction) | PROPOSAL (staged warning and EGRESS decided by Red) |
| FRONTROOMS · PROP KIT · THREE-VIEW + ERA (**2324:852**) | 16577, 1914 | GFX (VIS models) | K00–K45 | one kit, one scale; K01 when each piece is from; three-views of every kit asset (desk … stud doorway) | BUILT (kit), sheets current to 2026-10-02 |
| FRONTROOMS · 1990 FURNITURE MEDIA · TYPE + GRID (**2397:852**) | 16577, 17505 | GFX | T01–T19 | the 1990 type and grid kit for in-world print; T19 applied to FrontRooms (price tags, catalogue page, TV end card) | DECIDED; fonts in `Assets/Fonts/Period1990` |
| Week 3 · DEVELOPER DIARY #3 (**2440:3804**) | 23257, −2584 | RED | 1–6 | weekly log | history |
| FRONTROOMS · PROP KIT · REAL vs MODEL (**2349:852**) | 25257, 2000 | GFX | R01–R47 | each model beside its 1990 photo (Sears 1993, Commons, A24); period fixes (W2: torchiere, task chair) | DECIDED (fixes W2 wait on Red) |
| FRONTROOMS · WALLPAPER PRINT · PRECISE VECTOR (**2407:852**) | 25257, 17634 | WP | WP01–WP05 | WP01 smear to print, WP02 faithful, **WP03 "Hard edge" (2407:884) — Red's pick**, WP04 stepped grid, WP05 hybrid | DECIDED; not in game yet (Q1b after R2) |
| *planned* FRONTROOMS · LEVEL TRANSITIONS | 33937, 2000 | VIS | — | N1: transitions between levels | RUNNING (not on the page yet) |
| *planned* FRONTROOMS · ROOM VISUALS | 42617, 2000 | VIS | — | N2: this workflow (inventory, research index, Run! research and three directions) | RUNNING |

Notes:
- The weekly Google Slides deck mirrors the Figma decks; it holds no extra room research (memory: `weekly-dev-log`).
- Old section "S19 无限房间预览" (node 2233:8) no longer exists on the page.

---

## B. Research folders (`Documentation/research/`)

### B1. `office_and_film/` — Office level, film furniture, era lock (VIS, 2026-10-02)

| File | What it decided | Status |
|---|---|---|
| `01_film_production.md` | The A24 film used real duplicate furniture from liquidators, nothing newer than the early 1990s; piles are landmarks; lit only by 4000 K practical troffers (Venice 2, WB 4300 K, 14–18 mm lenses) | RESEARCH, COMPLETE |
| `02_film_shots.md` | 34 film shots clustered into 8 tableau archetypes, each with a game recipe for `FurniturePile.Build` and `OfficeKit.Dress` | RESEARCH, COMPLETE |
| `03_ip_canon.md` | Canon offices are empty, with blacked-out windows, coolers and vending; big rooms dark, small rooms lit; wiki text is CC BY-SA, so concepts only | RESEARCH, COMPLETE |
| `04_model_sourcing.md` | Candidates for 31 furniture types with licences; outcome: build in Blender, download one CC0 chair | RESEARCH, COMPLETE |
| `05_texture_sourcing.md` | CC0 scans per surface; download-vs-generate matrix; a shopping list (Red approved 37 items, 35 fetched) | DECIDED, BUILT (`Tools/lookdev/cc0_src`, git-ignored) |
| `06_distortion_tech.md` | Why Codex's "memory bleed" failed; the pile generator: analytic, quantised rest states, exact duplicates, no physics | DECIDED, BUILT (`Assets/Scripts/Office/FrontRoomsFurniturePile.cs`) |
| `10_synthesis.md` | The decision document: one Blender kit; Office = FrontRooms' own 1990s office; **flat opal lens**; 4000 K lamps + an Office grade; Office fog #3B3F35 at 0.018; pile chances by zone; FOV 72°; measured target values | DECIDED; **partly BUILT** (kit, piles, grade yes; opal lens, Office fog, Office lamp odds, FOV no) |
| `20_ip_period.md` | Evidence log for the era (on-screen dates, Kane's 1988–91 timeline) | RESEARCH (draft); §1–3 kept as evidence for 22 |
| `21_dating_domestic-furniture.md`, `21_dating_office-electronics.md`, `21_dating_office-furniture.md` | Per-asset dating drafts | STALE (superseded by 22) |
| `22_era_lock.md` | **Era lock: now 1990, newest design 1993, second-hand to ~1955, printed dates ≤ 1990**; all 54 kit assets dated; four fixes applied | DECIDED, BUILT |

### B2. `wallpaper_motion/` — the moving wallpaper print (VIS + WP + NAR)

| File | What it decided | Status |
|---|---|---|
| `10_synthesis.md` | The "sandwich" works: a moving albedo print under a static paper layer; highlights cannot see the print. Blocker: today's chevron is baked into the paper's normal, smoothness and cavity | RESEARCH DONE; implementation R2 RUNNING (clone), `_FR_PRINT` not in main |
| `20_level_design_phosphor.md` | "Afterglow Blazes": level design for the phosphorescent ink (rev 3): placement by zone and tier, triggers, the lamp-override layer v1 (agreed with MAP) | PROPOSAL (rev 3) |
| `30_narrative_phosphor.md` | **EGRESS** (Red's pick): the glow ink is the building's own exit plan; visible paper = WP03; staged Relay warning; ink content spec A.11 | DECIDED (direction); nothing built |
| `SOURCES.md` | Source ledger for all five research directions (no media downloaded) | ledger |
| `agent_reports.json`, `20_level_design_agent_reports.json` | Raw agent reports and verifier verdicts | raw data |

### B3. `interaction_audit/` — interaction, glass and render-quality audit (VIS, 2026-10-02)

| File | What it decided | Status |
|---|---|---|
| `10_audit_report.md` | **The audit.** 17 findings (F1–F17), shot designs, glass spec, rendering tiers (Web / High / Cinematic), ranked render changes. Desktop Mac is the quality bar; WebGL a reduced tier | DECIDED (contracts sent); remediation partly RUNNING (G, R, Q rows) |
| `01_interaction_inventory.md` | Every interaction is "press E, the world changes in one frame"; no camera language | RESEARCH, COMPLETE |
| `02_glass_and_breakables.md` | The glass has nothing to reflect; the pane is a veil on a 30 mm box; breaking is a delete | RESEARCH → G1–G12 |
| `03_rendering_quality.md` | The URP frame is flat for five content reasons (flat ambient, no reflections, erased lamp shadows, fill leak, primitives), not because of URP | RESEARCH → audit §5 |
| `04_aaa_references.md` | Camera grammar "B, the object is the actor", with C's grammar for two hero moments; no hands (Red later: head-dip, no hands) | DECIDED (B + head-dip) |
| `05_in_engine_evidence.md` | 81 frames from the game camera (seed 4242), material dumps, pipeline state; how to frame seeds | RESEARCH; the harness pattern every later capture follows |
| `FrontRoomsShotTimings.proposal.cs.txt` | Shot timing constants (unlock, glass, head-dip) for MAP | PROPOSAL (delivered) |
| `harness/FrontRoomsInteractionAudit.cs.txt` | Capture harness (copy) | tool |
| `images/` (58 files) | Evidence frames 00–80 + `runtime_dump.txt`, `frames.txt`, `audit_log.txt` | evidence (PNG; W7 asks to convert) |

### B4. `glass/` — glass material, reflections, ray tracing, destruction (VIS)

| File | What it decided | Status |
|---|---|---|
| `10_implementation.md` | `FrontRooms/Glass` shader, grime maps, `Glass_Window/Edge/Shard`, prop glass, **four zone reflection cubes + `SetZoneReflection`** proven in a play-mode test; Unity's reflection slider is gamma (0.3 = 7 % linear) | CLONE (G1–G4, G6), not promoted |
| `11_reflections_and_raytracing.md` | G11: Unity reports no ray tracing on Metal; URP has none; recommended zone cubes + room probes + a planar reflection on the held pane | DECIDED; RT point superseded by G14 |
| `20_verification.md` | G10: the veil is gone, but clear glass now reads as an empty opening; the zone cube adds only +1–4/255 on glass | RESEARCH (verification) |
| `destruction/01_conference_destruction.md` | Shipped games pre-fracture and swap, dressed with debris, decals, camera and sound | RESEARCH |
| `destruction/02_glass_in_games_and_real_glass.md` | Six families of game glass; a 1990 window = 6 mm annealed float (or wired glass in rated walls); why our crack reads fake (ranked) | RESEARCH |
| `destruction/03_micro_cutscene_camera.md` | Glass shot "Brace, strike, flinch" (1.65 s, no hands) | PROPOSAL (GD2) |
| `destruction/04_unity_implementation.md` | Three geometry stages on the 0.35 / 0.70 / 1.0 s beats; crack graph, not Voronoi; shards in a separate physics scene | PROPOSAL |
| `destruction/10_glass_destruction_plan.md` | The plan GD1 → GD3 + G14 | PROPOSAL (GD3 QUEUED) |
| `destruction/04_proto/`, `destruction/images/` | Python crack prototypes and their stats; 4 pattern images | prototype |
| `rt/01_code_review.md` | ChatGPT's Metal RT prototype: Metal RT is real on the M3 Max, but the prototype never draws (5 stacked blockers) and hitches 64–70 ms/s in the test scenes | DECIDED (fix list); G14 RUNNING |
| `rt/03_research.md` | Production RT glass: about 0.5 ms at 1080p on the M3 Max; glass G-buffer → one mirror ray per pixel → `_FR_GlassRTReflection` before tonemapping | PROPOSAL |
| `rt/03_bench/`, `rt/harness/`, `rt/images/` | Benchmarks, harness logs, diffs, one review image | evidence |
| `g10_harness/`, `g11_bench/`, `logs/`, `images/` (39) | G10 capture harness and sheets, G11 reflection bench (5 frames), logs, before/after frames incl. dead-lamp windows | evidence |

### B5. `hunter/` — the Relay's look (VIS, 2026-10-02)

| File | What it decided | Status |
|---|---|---|
| `01_ip_entities.md` | The IP's entity grammar (one countable error, misremembered ordinary thing, sound first, framed by doorways, deliberate) | RESEARCH |
| `02_creature_craft.md` | How to design a chaser that reads in dim fluorescent first person | RESEARCH |
| `03_gameplay_tech.md` | Hard numbers: top ≤ 2.0 / 2.05 m, face at 1.6 m, fits a 1.0 × 2.1 door; today's rig is broken | DECIDED (constraints) |
| `04_period_wardrobe.md` | Period materials; away from Slender Man; pale bodies fail in Level 0 and read only in Office and **Run** | RESEARCH |
| `10_hunter_directions.md` | Four directions A–D with blockout specs and a rubric | PROPOSAL (W1) |
| `11_squeezed_giant.md` | Red's direction: a 3.1–3.3 m giant crammed into small rooms | DECIDED (direction); model waits on W1 |

### B6. `interactables/` — doors, locks, keys, windows (VIS)

| File | What it decided | Status |
|---|---|---|
| `00_map_constraints.md` | MAP's binding rules: every collider counts, so hardware, frames and glass bits are render-only | DECIDED (binding) |
| `01_inventory.md` | Every placeholder interactable in code; **no exit, goal or end object exists** | RESEARCH |
| `02_period_hardware.md` | 1985–93 US hardware facts and sizes | RESEARCH |
| `03_readability_placement_shots.md` | Key hosts and anchors, pixel budgets, **per-zone key identity** (number + shape + colour) | DECIDED |
| `04_door_re8_gap.md` | Why shut doors show slits; two gap-free door fixes prototyped | DECIDED (Red: option A single-acting) |
| `05_locked_door_type.md` | Free door = veneer; key door = almond enamel hollow-metal "EMPLOYEES ONLY" storeroom door; reads at 6/12/20 m | DECIDED |
| `06_period_windows.md` | One window family, four members: W-L0 wood, W-OF bronze steel, **W-RN white enamel steel with wired glass (Run)**, **W-EX clear aluminium (Exit)** | DECIDED (spec) |
| `10_spec.md` | Kit build contract: 8 door members incl. **RN-F ward door, RN-K "STAFF ONLY" utility door** and **EX-F / EX-K exit doors with `Kit_ExitSign`**; locks, keys, tags, windows | DECIDED (spec v1); build R3 RUNNING |
| `harness/`, `images/` (72) | door-gap and locked-door readability captures | evidence |

### B7. Other folders

| Folder / file | What it decided | Owner | Status |
|---|---|---|---|
| `level_transitions/` (empty `harness/`, `images/`) | N1: transitions between levels | VIS | RUNNING |
| `outlets/02_code_paths.md`, `outlets/data/` | **Map walls have no baseboard or cove**; ~128 wall faces per chunk, 87 % in corridors; an outlet planner must run outside the dress step | VIS | RUNNING (N3) |
| `relay_pursuit/20_level_design.md` | MAP feasibility: the maze already has loops; the lamp-override layer (Omen / Dip / Sag) costs ~1 day | MAP | PROPOSAL |
| `relay_pursuit/30_narrative.md` | EGRESS alarm fiction: the Relay is what a fire-alarm panel sends; "emergency power" warning stage; alarm doors with push bars | NAR | PROPOSAL |
| `relay_pursuit/sim/` | Python map port and diagram scripts behind RP01–RP10 | SYS | tool |
| `webgl/00_measured_baseline_2026-10-02.md` | WebGL runs ~13 fps, CPU and draw-call bound | WEB | RESEARCH |
| `webgl/01_platform_and_build.md` | Build size: 97 % textures; NPOT wallpaper maps load uncompressed | VIS | RESEARCH |
| `webgl/02_visibility_portals.md`, `02_pvs_sim.cs.txt` | Cell-and-portal visibility; only ~13 cells are really visible | VIS | PROPOSAL (WebGL only) |
| `webgl/03_measured_budgets.md` | Per-content frame costs; a maze frame = 2,400–5,000 batches; 86–91 lights | VIS | RESEARCH |
| `webgl/04_techniques.md` | No-pop techniques; choose lights ourselves; WebGL-only scope | VIS | RESEARCH |
| `webgl/10_webgl_plan.md` | The WebGL tier plan (rev 2) | VIS | PROPOSAL (WebGL only) |
| `room_visuals/01_inventory.md` | Every room type's look today, with numbers and problems | VIS | DONE (this workflow) |
| `room_visuals/02_research_index.md` | This index | VIS | DONE |
| `room_visuals/04_captures.md`, `harness/`, `images/` (60) | 53 current frames of every room type + 3 contact sheets | VIS | DONE |

---

## C. Visual documents at the top of `Documentation/`

| File | Date | What it decided | Status |
|---|---|---|---|
| `VISUAL_RESEARCH_LOOKDEV.md` | 10-01 | The URP upgrade: research per level (Level 0, **Level 4**, **Level ! "Run For Your Life"** = white hospital corridor lit red by exit signs), the film look, the surface library, the light model | DECIDED, BUILT; numbers partly STALE (tiles, Run odds, shadows; see `01_inventory.md` §8) |
| `OFFICE_LEVEL_FURNITURE_RESEARCH.md` | 10-02 | Red-approved Office decisions (one kit, CC0 downloads, columns rule v1, analytic piles, Office look); open: Level 0 opal lens?, FOV 72?, piles in Office halls?, revisit rule | DECIDED; 4 questions open |
| `VISUAL_CHAT_TASKS.md` | 10-03 | The visual chat's queue (F, N, G, WG, R, Q, W, H rows) | live |
| `LEVEL_MODULE_SPEC.md` (MAP) | 10-02 | The unit contract: grid, heights, openings, **§4 troffer and lamp numbers**, **§5 tile sizes and the 192 m period**, dressing API, budgets | DECIDED (binding); §11 partly stale |
| `MAP_GENERATION.md` (MAP) | 10-03 | How the map and the title handoff work | current |
| `LEVEL_DESIGNER.md` (MAP) | 10-03 | Room modules authored as data, lamp per cell, fill, columns, markers | current |
| `RELAY_PURSUIT_REDESIGN.md` (SYS) | 10-03 | Relay away/called/arrive loop, trigger rooms (`ModuleTrigger Alarm`, props from VIS), staged warning, EGRESS | PROPOSAL |
| `FONTS_PERIOD_1990.md` (GFX) | 10-03 | Game-safe stand-ins for 1988–93 faces (TeX Gyre Heros, Archivo, Jost …) | DECIDED, BUILT (`Assets/Fonts/Period1990`) |
| `BRAND_ASSET.md` | 10-01 | The SVG wordmark and its S afterimages | BUILT |
| `TITLE_SEQUENCE.md` | 10-01 | The looping title corridor | STALE in parts (pool 3 → 5, rebase 256 → 192, handoff changed) |
| `RELAY_MODEL_RIG_RESEARCH.md` | 10-01 | Procedural rig contract (24–36 bones if skinned) | partly STALE (look moved to `research/hunter/`) |
| `BACKROOMS_VISUAL_SPEC.md` | 10-01 | Room roles incl. **Run = red paper, dark carpet, red pulse**; lighting target refs | STALE |
| `LIGHTING_SPEC.md` | 10-01 | Point lights, fog 0.024, **Red Run #D8493D** | STALE |
| `BACKROOMS_WALLPAPER_RESEARCH.md` | 10-01 | 256 px runtime paper, desaturated base, light does the yellow | STALE (sources still useful) |
| `WALLPAPER_UV.md` | 10-01 | Per-slab paper UVs | STALE |
| `BACKROOMS_DOOR_RESEARCH.md` | 10-01 | Primitive door kit with gasket and closer | STALE (replaced by `research/interactables/`) |
| `LEVELS_AND_ENTITIES.md` | 10-01 | Stream route incl. **Run = utility pipes, junction boxes, red service cue; entering Run wakes the Relay** | STALE |
| `FRONTROOMS_MAZE_LEVEL_DESIGN.md` | 10-02 | An older 9 × 7 maze with a cool-contrast exit cell | STALE |
| `UNITY_EDITOR_WORKFLOW.md` | 10-02 | Edit the scene's "EDITOR_PREVIEW / FrontRooms3D" greybox | STALE (the preview is now generated) |
| `UI_SYSTEM.md`, `UI_SHARPNESS.md`, `DISPLAY_SETTINGS.md` | 09-30–10-01 | UI type and sharpness; HDR/SDR toggle | UI (not room visuals) |
| `WEBGL_BUILD.md`, `CRASH_FIX_LEVEL0.md` | 10-01 | Build profile; level0 crash fix | build (not room visuals) |
| `RACE_SLICE.md` | 09-30 | Data-only route Start → Shift → Office → Run → Exit | not visual (history of the room order) |

Not visual (listed so the index is complete): `AUDIO_CONTRACT.md`, `AUDIO_LICENSES.md`, `DOOR_FOLEY_SEGMENTS.md`, `FOLEY_SYSTEM.md`, `SOUND_COVERAGE_AUDIT.md`, `SOUND_FOLEY_MOTIF_RESEARCH.md`, `../Docs/FOLEY_SYSTEM.md` (all SND).

Course folder `../../Documentation/` (outside `Frontrooms3D`):
- `BACKROOMS_VISUAL_DIRECTION.md` (09-30): surface grammar (80 % familiar / 20 % anomalous) and room tokens incl. **"Red Loop / Threat": crimson paper bleed, dark carpet, low red pulse**. STALE (history of Run).
- `LEVELS_AND_ENTITIES.md` (copy), `BACKROOMS_RACE_GENERATION_RESEARCH.md`, `DEVELOPMENT_WORKFLOW.md`: not room visuals.

---

## D. Tools that make the visuals

| Tool | What it does | Doc |
|---|---|---|
| `Tools/lookdev/gen_surfaces.py` (+ `gen_common.py`) | Generates every room texture (paper, carpets, ceilings, drywall, VCT, hospital wall, exit sign, lens, veneer, painted metal, macro wear) | `VISUAL_RESEARCH_LOOKDEV.md` §2 |
| `Tools/lookdev/gen_props.py`, `gen_office_furniture_textures.py`, `import_cc0_textures.py`, `fetch_cc0.py`, `preview.py`, `sheet.py`, `ref/Backrooms_Chevron_CC0.png` (+ LICENSE), `cc0_src/` (SOURCES.txt) | Prop labels and atlases, CC0 packing, previews | `OFFICE_LEVEL_FURNITURE_RESEARCH.md` |
| `Tools/Blender/frontrooms_kit/` (`kitlib.py`, `build_asset.py`, 52 asset modules in `assets/`, `creatures/` 8 Relay blockouts + `ref_human.py`, `ingest_cc0.py`) | Builds every kit FBX + sidecar | `research/office_and_film/10_synthesis.md` §5 |
| `Tools/Blender/generate_office_furniture_assets.py` | Codex's older Office furniture (feeds the unused `Resources/Models/Office`) | STALE (W3) |
| `Tools/print/` (`print_tool.py`, `ink_tool.py`, `patterns/`, `frames/`, `out/`, `unity_staging/`, README) | The wallpaper print and glow-ink pipeline | `Tools/print/README.md` (WP) |
| `Tools/three_view/` (`FrontRoomsThreeView.cs`, README, manifest) | Three-view sheets for the PROP KIT section | its README |
| `Assets/Editor/Rendering/FrontRoomsRenderSetup.cs` | URP asset, post profiles, every surface material | `01_inventory.md` §1 |
| `Assets/Editor/Rendering/FrontRoomsKitLookdev.cs`, `FrontRoomsLookdevCapture.cs` | Look-dev hall and title-profile captures (`Verification/kit_lookdev`, `Verification/lookdev`) | — |
| `Assets/Editor/Rendering/FrontRoomsKitImporter.cs`, `FrontRoomsPlayModeShaderCompile.cs` | Kit import rules; synchronous shader compile in Play Mode (fix F1) | `VISUAL_CHAT_TASKS.md` §0 |
| Capture harnesses (copies as `.txt`) | interaction audit, door gap, locked-door readability, G10, G11, room visuals | in each folder's `harness/` |

---

## E. Media and frames on disk

| Where | What | Ledger |
|---|---|---|
| `Research/week01/assets/` and `assets/clips/` | Week 1 deck clips: Dark Deception (DD_1–4), **Escape the Backrooms (ETB_L0, ETB_1 noclip, ETB_2 smilers, ETB_3_run)**, The Exit 8 (EX8_1–3), Lethal Company (LC_*), CURTAIN.png | **no SOURCES.md**: source URLs and timestamps for these clips are not logged (S07 says 1:34–1:39 for the Level ! clip); add a ledger before reuse |
| `Research/week01/source-captures/` | wiki captures as JSON | — |
| `Research/week02/ip-research/` | 16 stills, 18 clip files (2002 photo, Kane Pixels, A24 trailer, ETB, Backrooms 1998, POOLS; our Lobby and Run frames) | `SOURCES.md` (approved 2026-10-02) |
| `Research/week02/kit-references/` | Sears 1993 pages and crops, Commons photos, two A24 frames | `SOURCES.md` |
| `Research/week02/furniture-ads/` | 99 pieces of 1987–93 furniture media (magazines, TV, TV shopping) | `SOURCES.md`, `FINDINGS.md`, `tv_notes.md`, `page_notes.md` |
| `Research/week02/assets/` | sound previews, in-game door captures | `sound-previews/SOURCES.md` |
| `Verification/lookdev/` | 10 title-profile frames + sheet (2026-10-01) | — |
| `Verification/kit_lookdev/` | Office kit and piles, rounds 1–2 (2026-10-02) | — |
| `Verification/map-*.png`, `main-autopilot/` | map test scene and autopilot frames (autopilot has no post; do not judge the look) | — |
| `research/*/images/` | audit (58), glass (39 + 4 + 1 + 5 bench), interactables (72), room visuals (60) | each folder's report |

---

## F. Already in hand for the Run! plan (cross-index)

| Item | Where | Use |
|---|---|---|
| Red's design intent: a red corridor, the hunter behind, nothing to read; the lights turn red; counter = speed + the window | Figma 2235:735 | the brief |
| The canon "Level ! Run For Your Life": long white hospital corridor, dim red from hanging exit signs, chairs and beds in the way | `VISUAL_RESEARCH_LOOKDEV.md` §1; ETB clip `Research/week01/assets/clips/ETB_3_run.*` (ledger missing) | IP reference |
| Four earlier Run looks (red paper; utility pipes; red loop; hospital) | `01_inventory.md` §3.4 table | history, what not to repeat |
| Today's Run room: surfaces, lights, props, measured values | `01_inventory.md` §3.4; `04_captures.md`; `images/room_title-run_*.jpg` | baseline frames |
| Run door pair RN-F (ward) and RN-K ("STAFF ONLY") | `research/interactables/10_spec.md` D5 | doors for any Run direction |
| Run window W-RN: white enamel steel, wired glass, two-stage break option | `research/interactables/06_period_windows.md` §3.3 | windows |
| Exit door EX-F / EX-K with a lit or dead `Kit_ExitSign` (reuses `Run_ExitSign`) | `research/interactables/10_spec.md` | signage language shared with Run |
| EGRESS fire-alarm fiction: panel, emergency power, alarm doors with push bars ("EMERGENCY EXIT ONLY — ALARM WILL SOUND"), trigger rooms with a readable 1990 device | `research/relay_pursuit/30_narrative.md`; `RELAY_PURSUIT_REDESIGN.md` §4, §9, §10; Figma RP07–RP08 | a game role for a red-lit alarm room |
| Staged warning stage 1 (your room's lamps flicker) and the lamp-override layer | `RELAY_PURSUIT_REDESIGN.md` §7; `research/relay_pursuit/20_level_design.md` §2 | how Run lighting could be driven |
| Era lock 1990 (hospital/institutional props must fit) | `research/office_and_film/22_era_lock.md` | constraint |
| Period type for signs | `FONTS_PERIOD_1990.md`; Figma 2397:852 | the EXIT face |
| Hunter contrast: pale bodies read only in Office and Run | `research/hunter/04_period_wardrobe.md` §9–10 | Run palette vs the Relay |
| No hard-floor footstep in FMOD (`Surface { Carpet, CarpetTile, Metal }`) | `Assets/Scripts/Audio/FrontRoomsSoundIds.cs:87` | a VCT Run needs SND |
| Glass and reflections on VCT: zone cube API has no Run entry (`ReflectionZone { Level0, Office, Tall, DeadLamp }`) | `Assets/Scripts/Rendering/FrontRoomsLook.cs:26`; `glass/10_implementation.md` §8 | a Run zone needs a cube |
