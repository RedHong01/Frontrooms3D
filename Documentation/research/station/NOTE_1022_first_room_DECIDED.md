# NOTE 2026-10-22 15:2x · DECIDED: first room = option 1 (read this first; it overrides the open questions in the other NOTE_1022_first_room_* files)

**Red, in the 叙事设计 chat (account A), 2026-10-22 15:24:44 PDT, Red's own words:** "按推荐，第一个房间选方案 1"

The 游戏视觉 session checked the quote in 叙事设计's transcript (23146057, user turn 22:24:44Z). The recommendation Red accepted is option 1 of `NOTE_1022_first_room_tutorial.md` §4. "按推荐" also covers 叙事设计's narrative recommendations from the same message (`NOTE_1022_first_room_narrative.md`).

**What is decided:**
- Floor 1's station is the **first room**: the desk, the set (the one moving screen), the placard and the floor key.
- The **locked EXIT is in view** from it, through a doorway about 3–6 cells away, so "lock before key" still teaches.
- **The first send waits until the player has left the room.** It comes from the far side, and the 150 s fallback stays. This changes C32's "1 s after the take". 系统设计 re-checks Red's words before it changes the data.
- **Floor 2 and later:** the stations stay rooms the player has to find.
- **Fiction:**
  - The room is the building's front desk at the main entrance (front rooms vs back rooms), and the only finished room.
  - The yellow Level 0 paper starts at the threshold.
  - The player is an after-hours visitor: `PLEASE SIGN IN`, and the register's last line is blank.
- **The plate `THIS DOOR TO REMAIN UNLOCKED DURING BUSINESS HOURS` moves** from the stair door to the first room's locked main entrance. That entrance is solid or has a vestibule, never glass to an outside.
- Never A24's showroom. Never LOBBY or STATION on a sign. G1–G10 still hold inside the room, including two ways out.
- Records: `narrative/40` v1.2 §9, `station/20_layout.md` **v1.3**, `01` §4–§6.

**What `61_for_red_first_room.md` must still ask Red (and nothing else about the structure):**
1. **(A) The wall direction** for the first room: its own walls, distinct from Level 0. This was Red's first ask at 15:14.
2. **Sub-choices the build needs:**
   - whether the title room IS the first room, or the first room lies behind the title's next shut door (merge the a/b readings with 关卡设计's `NOTE_1022_first_room_map.md`);
   - the N for the first-send gate (how far, or how long after leaving the room).
   - Each with options as the player feels them, the cost, and a recommendation.
3. Show option 1 as **DECIDED (Red 15:24)** and do not offer the other structures again. Use 60 to record them as history.

The six points in `NOTE_1022_first_room_system.md` still have to be answered for option 1: say how each is kept.

## Update 15:3x (总控 and 关卡设计, after Red's pick)

- **`61_for_red_first_room.md` becomes the PRODUCTION SPEC for option 1, not a decision page** (总控, tutorial tracker). Do not ask Red which structure they want. Put genuinely new questions for Red in a short separate section at the end, in Chinese, with the full background (memory decision-asks-need-detail). The main one is the wall direction Red asked for at 15:14; give it a clear recommendation.
- **Title handoff: 关卡设计's pick is the first MAP room behind the start door, not a restyled title room.**
  - Reasons: the start door, its hold/lock and NO REENTRY stay as they are; the Relay can reach the room later; the title stream code does not change.
  - The room's own walls come from a per-cell skin override on the station's cells (关卡设计's MapWorld side), plus our new Surfaces set, plus the rendering-layer light fix.
  - Use this unless the research finds a strong reason against it. If so, say so plainly in 60, and the 游戏视觉 session settles it with 关卡设计 before 61 is final.
- **Tutorial documents are updated:** `research/tutorial/10_proposal.md` 52967d5c, `20_requests.md` d494b3cd. Our rows:
  - **V1** adds a new module, **Station · Front Desk**: Floor 1's first room, the building's front desk at the main entrance, with its own walls, a seam rule with Level 0 and the main entrance locked (`narrative/40` §9). The two Floor 2+ modules are unchanged.
  - The TV light runs along the doorway axis.
  - At the take, the main subject is the light on the key.
  - **M2 mount code:** the placard hangs in the first room, in the first line of sight after control is handed back.
- **平面视觉's placard constraints** (`NOTE_1022_first_room_placard.md`, 7 points) must be met by the spec: mount wall, the same wall on every seed, and the EXIT side fixed relative to the mount, or else sheet C at run time. The spec gives 平面视觉 the geometry in cells (desk, mount wall, start door, stair door).
- **File ownership (关卡设计 15:3x):** never put hunks into FrontRoomsMapWorld.cs, FrontRooms3DGame*.cs (except the handheld blocks), Scripts/FrontRoomsMap/*, Editor/FrontRoomsMap/* or MAP_GENERATION.md. Map-side changes are contract diffs for 关卡设计.

## Update 15:3x: the first-room signs (平面视觉 · 信息设计; Red approved the size)

- **BUSINESS HOURS plate: FINAL at 18 × 6 in (457.2 × 152.4 mm).** Red, in the 信息设计 chat at 15:33: "457 毫米可以，就按这个尺寸".
  - Mount: centred on the first room's main-entrance leaf (room side), plate centre 1525 mm above the floor. The leaf must be wide and solid enough to take it.
  - If the leaf gets a vision panel, the plate goes on the latch-side wall at the same height.
- **SECURITY:** brushed-aluminium letters, Heros Bold, 4 in cap height, on the desk front.
- Files: `~/FrontRoomsVisualWork/info_type/stair_signs/out/first_room/` (README updated). Use them as delivered. Do not re-letter.
- 信息设计 wants lit blockout frames with both signs mounted, for its check. Put them in the spec's images.

## Update 15:5x: 叙事设计's review (R1, R2) — the spec must follow these

- **R1 · Every station uses the station's own walls,** on Floor 1 and on every later floor. Red's 15:14 ask is about the room type ("当前接待室/警卫室应该是有一个单独的房间墙体设计，而不是直接利用 level 0"). So Floor 2+ stations never fall back to Level 0's paper.
  - Directions B and A (Floor 2+) differ in furniture and story only, never in the wall finish.
  - The wall direction Red picks in 61 applies to every station. On every floor, "the room with these walls has the key" can then be read from the doorway, before the screen.
  - 40_for_red_draft carries this as a one-line sub-question, recommended "yes, the same walls on every floor". 61's wall question must say it covers all stations.
- **R2 · The main entrance IS the start door, seen from inside the station.** 关卡设计 confirmed it at 15:3x: the first room is the first map room behind the start door.
  - The BUSINESS HOURS plate (457.2 × 152.4 mm, final) hangs on the **start door's station face**, plate centre 1525 mm.
  - The start door keeps its title-side hold/lock and NO REENTRY behaviour.
- Red is they/them (COMMON §1). Write "Red's own words".

## DECIDED 15:4x: the station walls = WALLS 2 · POST ROOM (Red, in the 游戏视觉 chat)

**Red's own words, in the 游戏视觉 chat (account A, session `local_5656cffd…`), about 15:4x.** Red showed `images/first_room_s4_walls2_post_room.jpg` and wrote "这个版本可以".

- **Walls 2 · Post Room is the station wall direction.**
  - Walls: cool blue-grey eggshell paint **#9AA3A6**.
  - Crash rail: 6 in, navy-charcoal **#3E4650**, at **0.78–0.93 m**.
  - Floor: VCT **#B2AEA3** with a dark border course.
  - E1: a dark-bronze steel cased frame with stainless corner guards.
  - Same plan, props, lamps and cameras as the blockout (plan W′ rev 4).
- This overrides the sweep's recommendation of walls 1 (Lobby Post). Record that plainly in 60. The sweep noted walls 2 is the cheapest and the closest to Office.
- Measured in the blockout:
  - from E1: mean Y 0.215, set dies 16 % of frame, placard 0.51;
  - from the Level 0 corridor at 4.5 m: opening ΔE 31 (today 19), Y ×0.66.
- **Per R1 it applies to EVERY station**, Floor 1 and Floor 2+. B and A differ in furniture and story only. The one 40-draft sub-question ("the same walls on every floor?") stays as a confirm item, recommended yes.
- **So 61 does NOT ask the wall question any more.** It specifies walls 2 for production:
  - a new Surfaces set (paint, crash rail, VCT field + border, bronze frame, stainless guards) with exact values and texel density;
  - the seam rule with Level 0 at E1 / E2 / the start door, using 关卡设计's per-cell skin override;
  - the station's own reflection and the rendering-layer light fix;
  - placard and sign contrast on #9AA3A6;
  - the tiers.
- Re-frame the walls images' chips from WAIT-RED to DECIDED (walls 2) in the next renders; SHOT_FORMAT §5 does not allow a WAIT-RED chip on a decided slide.

## Update 15:5x: 叙事设计's phrasing for walls 2 (carry into 61)

- "The one finished room" becomes **"the one *kept* room"**: painted, railed and tiled, while the back rooms are only papered.
- "Lobby security desk at the main entrance" becomes **"the building's security post at its entrance"**. The role is the same; the finish is plainer.
- Why this is more period-true: guard posts of the period were function rooms. (P31's "rarely things of beauty" is said of CRT-era security consoles, not rooms: corrected 10-22 18:0x per 叙事.) Paint, a crash rail and VCT are the 1990 clinic and DMV waiting-room vocabulary [memory], so B on Floor 2+ reads truer in these walls.
- Unchanged:
  - the visitor role (`PLEASE SIGN IN`, the blank register line);
  - the plate on the start door's station face;
  - `LEVEL 0 / FRONT ROOMS` for the room;
  - never LOBBY or STATION on a sign.
- Records: `NOTE_1022_first_room_narrative.md` §1 and `01_research.md` §5 (叙事 edited both).

## Update 16:0x: 叙事设计's E15 palette (`research/station/03_palette.md`, sources SOURCES.md §E, 68 rows) — use it for the station props in 61

- **Confirmed:** oak veneer, grey laminate, putty steel files, putty or beige plastics, chrome sled bases.
- **Adjust:**
  - beam seating: slate blue or charcoal vinyl on a chrome or black beam (navy only as a variant);
  - task chair: grey fabric on a black base;
  - `Kit_KeyCabinet`: from steel almond to putty or light grey;
  - annunciator: a beige cabinet with a beige door. Red variants are period-true, but red is our ALARM ZONE colour.
- **Variant for A's guard desk:** putty steel pedestals with an oak-grain top.
- The Post Room walls (#9AA3A6 etc.) are untouched by this. Check the props' contrast against the walls.

## Addendum 2026-10-22 16:5x (游戏视觉, account B): 叙事's review of 61 must be applied before Red sees it

叙事设计 reviewed `61_for_red_first_room.md`. Verdict: OK to go to Red after five wording fixes, R7–R11, in `NOTE_1022_61_review_narrative.md`. The critic and fix stages of `station-first-room` must apply all five exactly as written there:
- **R7:** no "现在不是营业时间". The clocks are live; the business-hours plate stays, unexplained.
- **R8:** the green lamp is the POWER jewel (the panel is awake). No alarm has been "sent".
- **R9:** the room is not "safe". The first send only waits until the player leaves (§13's N).
- **R10:** the fire control room is a separate locked room, not the guard post.
- **R11:** 叙事 accepted the post wall (`20_layout.md` v1.4 §4.5, Plan FD), with three conditions.

## Addendum 2026-10-22 18:0x (游戏视觉): FD-b re-accepted by 叙事

- 叙事 re-accepted FD-b in `20_layout.md` v1.5 §4.5 (938e8359); FD-a is withdrawn. G1, G2, G4, G5, G6 and G7 read as written.
- Its conditions:
  - no nook: the corner block stays solid to the ceiling;
  - the door shuts about 4 m in, and the player turns to the doors and the plate beside them;
  - the room never reads as safe.
- Fiction for the block: the building's riser chase, where the alarm conduit runs.
- The plate goes beside D on the liner wall (B3c; 40 §9 11a6cf2e). 信息设计 was asked to agree (18:0x).
- Union Trust is withdrawn as evidence; the block is our design choice.
- The visitor under live clocks: "a visitor the building treats as after hours, whatever the clock says". The register's open page is the previous business day's: daytime visitors out by 1745, then blank rows, and the last ruled line blank (01 §6 2edc98f8; NOTE_1022_first_room_narrative §2 f83cff72).
- Still waiting on 关卡设计's confirmation of FD-b against its final A26 plan before 61 goes to Red.
- **Business-hours plate, 18:1x (信息设计 confirmed):**
  - It works on the latch-side liner wall beside D: same plate and art, only the mount changes (FIRST_ROOM_SIGNS_README dc3c60af + layout.json).
  - **Size 457 × 152 mm (18 × 6 in):** Red approved it in the 信息设计 chat at 15:33, "457 毫米可以，就按这个尺寸" (verified in transcript 98b50731, a user turn at 22:33Z). The UBC 1 in letters need it.
  - Mount: room side, centre 1525 mm above the floor; door-side edge 50–100 mm from the frame's outer edge; adhesive.
  - **The wall needs ≥ 560 mm of clear flat wall from the frame edge, with nothing tall within 50 mm of the plate's far edge.** Check this against 关卡设计's final plan. If the liner wall is narrower, send 信息设计 its width.
- **Corner block, 18:3x (关卡设计 agreed):** the NE corner block becomes map-built shell, floor to ceiling, built with M4. It is a module column if columns v1 holds that size in the NE cell; otherwise a solid shell piece built with the station, skinned from our station skin (walls 2 · Post Room).
  - The key board and annunciator are 游戏视觉 props on its west face; the map supplies the face's plane and extents.
  - Unchanged: the KeySpot per the limits, that cell's lamp Off, M6's hunt aimed at the take point.
  - The exact footprint comes with 关卡设计's final plan. Update FR05 and the Figma board then.
- **FINAL map plan, 18:4x (关卡设计's judge picked; supersedes the "bounds" wording):** `~/FrontRoomsMapWork/notes/floor1_start_group_plan.md` (§0 decision, §4 design, §9 agreed amendments).
  - CARVE wins (41/50; hybrid 32, search 24), built to FD-b. The station is carved into the floor-1 map data, so the shell, the Relay's nav and every reader agree.
  - Fixed rotation: D in the south wall at column 2; E1 north on the axis; E2 in the west wall at row 1. EXIT k = 4 cells (12 m past E1), which is 24 m from D for 4×4 or 21 m for 4×3. The flat sign is in view through D from the stream room on every seed, by construction.
  - **Footprint 4×4 or 4×3:** 游戏视觉 picks after 关卡设计's Step 0 feasibility batch (6,400 starts). 61 should say so. Recommend 4×4 unless Step 0 shows a problem: it is the FD-b sketch, and 4×3 pushes the TV toward the axis column.
  - **Map cost 36–50 h.** It replaces 30–55 h in 61.
  - **Our side (plan §4.9 asset contract):**
    - station module rules and marker kinds;
    - the placard mount moves to a station anchor and must land together with the liner that covers P1 (§4.8; G1's code stays);
    - the riser block is map-built shell, floor to ceiling (§9);
    - the TV light vs StartLampsNorth, the station reflection, the corner-post rule for the third skin, and `StationShellBuilt` for rendering layers.
  - **Landing:** with the profile's stationModule empty, today's game stays byte-identical until our asset and M3 (the floor key) are green.
  - **Two notes for Red in 61 (plan §7):**
    - D's chunk pinned on floor 1 too (no revisit shifts in those 64 cells; 关卡设计 recommends it);
    - the station would not count as a zone (系统设计 to confirm).
- **Placard waiver GRANTED by 平面视觉, 20:28:** keep the north wall at x 5.5, facing D. The 1.5 m rule came from an early note, when the dot stood for the desk. On the sheet the dot is the reader at the placard (ISO 23601 §5 a) and the desk symbol sits at the counter, so both are drawn true at any distance. Being face-on from D matters more than being close; the east spot is read at ~55° and would steal light from the key-board lamp moment. 61 must say "豁免已获平面视觉同意", not "待确认". 平面视觉 re-renders the room sheets once it has the final room numbers (size and origin, D, E1/E2 wall/centre/width, the EXIT's direction and distance, the counter rect, the mount).
- **20:28:** 叙事 AGREES with N = 2 (key taken; latch entering the second cell out, 3 m past E1). The first dip lands mid-corridor with the EXIT 12 m ahead, so the lesson is "the lamps dip: keep going". At N = 1 it can land on the threshold, where a player may step back into the room as shelter, which breaks its condition 3. 61 §17 may say "叙事同意推荐". 20_layout §4.5 is fixed: chase block map-built to the ceiling, plate on the west liner. 关卡设计 fixed both plan inconsistencies (axis 1.50 m ±0.6 everywhere; the N = 2 gate is 9 m before the EXIT door face). Note: plan §4.7 line ~347 still says the plate is on the liner EAST of D, while its §9 and 61 say WEST; flagged to 关卡设计.

## Update 20:4x (switch #5): 叙事设计's rev-3 review R12–R15 — 61 must carry these before it goes to Red

Source: `research/station/NOTE_1022_61_review_narrative.md` § "Rev-3 review". 叙事 checks the fixed 61 once more before Red sees it.

- **R12 (§2):** the heading says "按推荐的 N = 1，EXIT 在 3–6 格". Change it to **N = 2, EXIT fixed at 4 cells (12 m)**. Timing: the send triggers on entering the 2nd cell out, about 3 m past E1. The first dip comes 1.3–2 s later, mid-corridor, with the EXIT about 9 m ahead.
- **R13 (§17):**
  - Make **N = 2 the recommended option**, and write **"叙事 20:27 同意 N = 2"**. 叙事 HAS agreed, so do not write "待确认".
  - Drop option 2's EXIT-3-cells drawback and its planner-cost line; the EXIT is fixed at 4 cells.
  - Keep N = 1 as the alternative, with its feel: the first dip can land on the threshold, and a new player may step back into the room as shelter.
- **R14 (§17 背景):** "EXIT 在北墙外 3–6 格" → "固定 4 格（12 米）".
- **R15 (§3 vs §18):** §3 says the 1986 building-877 plan is "硬盘上有", but §18 says nothing has been downloaded. Name the approval that covered that file (search SOURCES.md / the media ledgers for its fetch record and Red's OK). If no approval exists, say so plainly and move the file out of the repo-adjacent research folder into W/_partial pending Red's OK. Either way, the page must keep the download rule visibly true.
