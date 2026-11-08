# Double doors · 10 · Period research for the zone pairs

- **Line:** 游戏视觉 (ledger key `visual`), account G. Workflow visual-zone-doors-refine (wf_afdae4db-b34, parent session local_1fc9f09c). Research stages of 2026-11-05: R-1 the Office pair (21:1x-22:0x), R-3..R-5 station, back-of-house and Run! (21:1x-21:5x), R-2 the pool (21:2x-22:1x). Merged here by stage Design A, 2026-11-06 10:xx PST: Part O (R-1) and Part Z (R-3..R-5) as their stages wrote them, own section numbers renumbered. **Part P (R-2, the pool) is appended by Design B.** The design is `20_design.md`; the sources are `SOURCES.md`.
- **Red's asks** (this line's chat, plain user turns, verbatim): 2026-11-05 19:17:06 "同时我需要游戏视觉显根据当年做调研，去翻新每一个区域的双开门模型，规格和结构上的要求和之前一样"; 19:17:37 "把这个工作当作第一要务"; 19:35:21 "目前level 0的门不需要改变，我是需要你改变办公室，车站，后场，run！，泳池等的门"; 19:39:25 "我需要你看一下之前在项目里做的所有门相关的调研，现在还剩下那些门没有细化，给我清点一下，避免重复性工作，然后做细化和模型升级的调研和模型重做等工作。不要推翻之前已经做了的工作".
- **Decisions in force** (journal PARENT NOTEs, `W/zone_doors/journal.txt`): 21:15:15 "按默认做" (D-3 station face on D; D-4 the Office leaf stays walnut G, its casing face, hardware finish and lite change; D-5 the pool: three options + TODAY). 21:25:37 "这里改成选项 2：整扇门都换成区域的门。* 玩家在 Level 0 房间里走向下一扇门时，会先看到办公室的门，算是一点预告。" (D-1 = the whole door). 21:26:27 "选项 2：新增一类后场门（漆面钢门、踢板、百叶）。…" (D-2 = a new back-of-house map member).
- **Status:** RESEARCH ONLY. Main read only; no Unity; no clone changed. Web pages were read as text. Two city spec PDFs were read as text from the fetch tool's cache, as the door_hardware and door_frame passes did; nothing was downloaded on purpose, nothing saved in the project or in W. One PDF that the fetch tool cached by itself was never opened (journal 21:4x). No image in this file: the drawings are `20_design.md`'s sheets, and the leads for real period photographs are §O7 and §ZE (no download asked).
- **Inventory base:** `door_inventory/10_inventory.md` (001666d5): §2 DO NOT REDO is reused as it is; §3 STILL TO REFINE is the work list. Nothing here re-researches, re-plans or alters a §2 item.
- **Marks:** K = a fact read in our code or docs (sha1 = the first 8 hex of main's file); R = read in these passes; S = seen only in a search summary; [E] = estimate or inference. Period weight: IN (1985-1993), NEAR, LATER (a current source, used only for how materials behave or for practice), lineage. Source ids: O1-O9 (Office), Z1-Z5 (station, back-of-house, Run!), N1-N32 (pool, Part P), K (reused project sources): all in `SOURCES.md`.
- **How to read:** §0 the binding spec; §1 what is reused; **Part O** the Office pair; **Part Z** station D and the night post (§S), back-of-house (§B), Run! (§R), sound, photosafety and tiers (§ZD), open points (§ZE); **Part P** the pool (Design B).

## 0. The binding spec (Red 19:17: "规格和结构上的要求和之前一样")

| Item | Value | Source |
|---|---|---|
| Type | a c. 1990 commercial **double-acting** pair; no latch, so never a rated fire door, an exit with panic hardware or a lockable entry | door_hardware/10 §4, §10.5 (Cal. Title 8 §3235, the 1979 UBC) |
| Opening | 2.4 m × 2.66 m in the stream (RoomHeight 2.9 − the 0.24 m header) | FrontRoomsDoubleDoorKit b7ab5af0 |
| Leaves | 1-3/4 in flush: 1.1955 × 2.639 × 0.0445 m; 3 mm from the linings, the head and each other; 6 mm over a 12 mm saddle (152 mm wide); black brush strips close the gaps | kit header, :31-49 |
| Hinges | three DA spring hinges per leaf, Bommer's layout (one within 7 in of the top, a pair within 3 in of the bottom): centres y 0.167, 0.405, 2.418 m; a barrel on each face, r 11 mm, axis x ±1.1985, z ±0.03425; the leaf turns on the barrel of the face it swings toward (`PlaceLeaf`) | kit :11-17, `HingeCentres` |
| Frame today | walnut linings x 1.200-1.219 as deep as the leaf; streamline casing from x 1.212 (the 12 mm reveal the barrels need) to 1.250, 14 mm proud at the opening edge; head casing to 2.710 | kit :43-49, `CasingProfile` |
| HW-DA1 | push plates 4 × 16 × 0.050 in, centre 45 in above the floor, 1-1/2 in from the meeting edge; 10 in kick plates; BHMA 630; on every face of each leaf | door_hardware/10 §9; 20_build |
| Lite rule | a DA leaf's lite ≥ 200 sq in, its bottom ≤ 48 in; set A by zone: lobby F4b 10 × 48, office F1 10 × 30, run F5 15 × 15, exit F13 4 × 25; alternates A1 16 in porthole, A2 F1 + louvre | door_vision/50 §0-§1 (Red 10-30 "A 按区域四种") |
| Door space | x across (0 the opening's centre), y up, z through the wall (0 the leaves' centre plane; +Z the stream's swing face, the station's side) | kit header |
| Render-only | everything but the leaves and the callers' collider-only boxes | kit header |
| Must keep working | RoomStream's swing both ways and `SetHingeSide`; the start / terminal door; StationDress's static entrance (`Faces.Plus`); StairwellDresser; TitleVignettes; the vision lites (LIVE / DARK, PEEK); door break; door culling; sound and pivot names ("double door left/right hinge"); the body-door interaction; GlassRT; the phone tier; WebGL keeps Today's title | inventory §2; `20_design.md` §A1.2 |

## 1. Reused, not redone

| Doc (sha1) | What it gives the zone pairs | Where used |
|---|---|---|
| `door_hardware/10_research.md` (039ad92f), `20_build.md` (d63889ab) | the DA type and its code limits, HW-DA1 and its built plates (Kit_DoorPushPlate 2,238 / 18 / 10, Kit_DoorKickPlate 2,978 / 18 / 10, 12.6 MB of 630 maps), 626 / 630 wear, §10 findings | §O1, §O2.4, §O2.6, §Z1 |
| `door_frame/10_period_frames.md` (67fafac0), `20_for_red.md`, `30_build.md` | SDI 111 cased-open DA profile; F4 = steel 50.8 × 11.1 mm (Red "F4 当年尺寸"); welded, mitred, ground; dark bronze as the period's aluminium finish | §O2.2, §Z1, §S.2 |
| `door_vision/50_lite_forms.md` (b68d27cc), `51` (abe42a65) | set A and its numbers (F1, F4b, F5, F12 = A2, F7 = A1), the frames per leaf material, the Relay band method | §O2.5, §B.1, §R.3 |
| `door_wood/40_for_red.md` (4637ddf6), `40_pores.md` (08a3dfe9) | walnut G, the period's office faces, the T6 brightness rule | §O2.3, §B.4 |
| `interactables/d16/10_design.md` (998f2e93), `FrontRoomsInteractableKit.Door.cs` (0dbdc107); `interactables/05_locked_door_type.md` (3fb99f6f) | the Office map rows (Steel_DA frame, satin chrome trim), the storeroom family (almond enamel, dark-bronze frame, stainless kick plates) | §O1, §O2.4, §B.1 |
| `station/70_station_look_v1_plan.md`, `NOTE_1022_first_room_DECIDED.md`, `61_for_red_first_room.md`; `FrontRoomsStationTrim` (d618387a) | the station's door language (Red "这个版本可以"): pressed-steel cased frames, stainless corner guards; D's keep list | §S.1-§S.7 |
| `room_visuals/03_run_research.md` (798c1520), `run_b_production/60_production_66.md` (dd0e36e2), `level_space/71_run_room.md` (e52ce6d8) | Run! B's materials and door set; the rated hold-open pairs are not DA pairs | §R.1-§R.5 |
| `bathroom_rooms/*` | the pool zone (Part P) | Part P |

---

# Part O · The Office pair (R-1)

## O0. Answer first

1. **What the period door was.** In a c. 1985-1993 US office, a double-acting pair was a **non-rated interior pair inside a suite** (reception to the open office, mailroom, pantry). The suite's corridor door was usually a single-acting, often 20-minute door. The DA pair had a **hollow-metal "cased open" frame**: no stop, a flat soffit, a 2 in face, 16 ga, welded and mitred, painted on site. Its leaves were **1-3/4 in solid-core flush wood doors** (plain-sliced red oak by default; walnut or cherry on executive floors) in clear satin lacquer. Each face had **a push plate and a kick plate**, and the leaves hung on **DA spring hinges or a floor closer**. Each leaf had a **10 x 30 in wired lite** in a bead of the door's own wood. All of this is on disk already (§O1); this pass adds only the finish evidence (§O2.4) and the office wear numbers (§O2.6).
2. **The Office door, within the spec** (one look for the whole door, both faces):
   - **frame:** a dark-bronze 16 ga pressed-steel cased frame at the period F4 size, 50.8 mm face, 11.1 mm proud, round the jambs, the head and the soffit, with a 12 mm hinge-clearance step. It replaces the walnut streamline casing and linings.
   - **saddle:** the fluted saddle in the frame's finish (the Steel_DA rule).
   - **leaf:** walnut G, unchanged.
   - **lite:** the Office F1 10 x 30 in, centred on the tall leaf, 45-75 in above the floor.
   - **hinges:** satin chrome (652 / US26D look), the Office map rows' finish.
   - **plates:** HW-DA1 kept in 630 satin stainless, because period practice kept plates in stainless even where the trim was satin chrome. The landed OF-K row pairs the two finishes the same way.
   - **wear:** an Office wear layer on the leaf (hand halo, chair-arm band, cart nicks).
3. **Spec kept:** the 2.4 x 2.66 m opening, 1.1955 x 2.639 x 0.0445 m leaves, three Bommer DA spring hinges each, 3 mm gaps, brushes, the 12 mm saddle, HW-DA1 sizes and places, pivots and their names, colliders, LIVE / DARK, render-only.
4. **Cost per Office door** (estimated; the kit's sidecar gives the real numbers): frame kit about **1,100 / 310 / 180 tris** at LOD0 / LOD1 / LOD2, replacing about 226 tris of code mesh with no LOD. **-1 draw** (the frame and saddle share one material). **0 MB of new textures**: Prop_SteelBrown and Prop_ChromeSatin already ship. The wear mask is optional, **+0.7 to 2.7 MB**.
5. **Findings for Design A:**
   - (a) TitleVignettes' `DoorCasingHalf` 1.27 counts the 19 mm lining on top of the 12 mm reveal, but the casing ends at 1.250. Every family room's end-wall base or band therefore stops **20 mm short** of the casing. The Office frame (outer edge 1.2628) still leaves 7.2 mm, so the base must follow the look's frame edge.
   - (b) Door 1 of Tour (the Office entry pair) already sits at the 3.0-flash limit: re-run flash_check after the build.
   - (c) No sound contract is needed: the leaf stays wood, and a DA leaf never strikes its frame.

## O1. Reused, not redone

| Doc (sha1) | What it gives the Office door | Used in |
|---|---|---|
| `door_frame/10_period_frames.md` (67fafac0), SOURCES F1-F5, F19 | SDI 111 "Cased Open" DA profile: 2 in face, 7/16 in return at 5-3/4 in jamb depth, no stop, flat soffit, 16 ga interior; Feb 1991 SD 08110: welded, 16 ga minimum, "trim faces mitered, welded, and finished smooth"; dark bronze is a period ALUMINIUM finish (Feb 1991 SD 08410: Kawneer Tri-Fab II 450, US Aluminum Series 450); "a dark-bronze paint on a steel frame is our own choice, not a sourced default [E]"; F4 = steel 50.8 x 11.1 mm (Red: "F4 当年尺寸") | §O2.2, §O4 |
| `door_hardware/10_research.md` (039ad92f) §2.1, §4, §8, §9, §10 | Finishes 626 / 630 and how each wears; a DA pair gets push and kick plates on every face and no pull, lock or closer; HW-DA1 (4 x 16 x 0.050 in at 45 in, 1-1/2 in from the meeting edge; 10 in kick plates; 630); §10.5: a DA pair is never a rated or egress door; §8: worn 630 plates and "the walnut around the plate darkens" (the leaf's job) | §O2.1, §O2.4, §O2.6 |
| `door_hardware/20_build.md` (d63889ab) | The built plates: Kit_DoorPushPlate 2,238 / 18 / 10, Kit_DoorKickPlate(_B) 2,978 / 18 / 10; Prop_PushPlate630 / KickPlate630(B) maps (palm oval 70 x 220 mm, finger haze, heel streaks 50-180 mm, scuffs, creased corner, a low tide line), about 12.6 MB | §O2.6, §O5 |
| `door_vision/50_lite_forms.md` (b68d27cc) §0, §1, §3, §5-§7; `51` (abe42a65) | F1 = DA-L1 10 x 30 in (300 sq in), Office; DA lite ≥ 200 sq in, bottom ≤ 48 in (Cal. Title 8 §3235); wood leaves take a bead of their own species (SD 08150, Feb 1992); the Relay band method; set A decided by Red: "A 按区域四种" (lobby F4b, office F1, run F5, exit F13; the lobby lite centred) | §O2.5 |
| `door_wood/40_for_red.md` (4637ddf6) §2, §4; `40_pores.md` (08a3dfe9) | Period office face: plain-sliced red oak, A grade, book-matched, running; walnut and cherry for executive floors; option 1 cost 9.3 MiB, T6 1.21 stops; the FINAL lacquer pores (Red's "drops" fix) | §O2.3 |
| `interactables/d16/10_design.md` (998f2e93) §3.2; `FrontRoomsInteractableKit.Door.cs` (0dbdc107) :124-149, :398-408 | The Office's map language: OF-F / OF-K on `Kit_DoorFrame_Steel_DA` in Prop_SteelBrown, chrome trim swapped to Prop_ChromeSatin ("satin chrome (US26D, the 1990 commercial lockset finish)"), steel leaves' plates in Prop_StainlessSatin ("satin stainless (US32D, the kit source)") | §O2.2, §O2.4 |

Nothing in §2 of the inventory is reopened: HW-DA1's geometry, the F4 sizes, walnut G, set A and the kit's spec are used as they stand.

## O2. The c. 1985-1993 office DA pair, seen from the office side

### O2.1 Where it stood

- A DA pair is pushed from both sides and has no latch, so it cannot be a rated door, an exit with panic hardware, or a lockable suite entry (door_hardware §4, §10.5; Title 8 §3235 via door_vision P1). In a 1990 office it is **a non-rated pair inside the suite**: between reception and the work area, into the mailroom or copy room, into the pantry. The suite's corridor door was usually a single-acting, often 20-minute door (door_vision 50 §5).
- **Our title Office room is a bullpen** (O-C pendants, a four-pod cubicle farm round a 1.6 m chase lane, dark CRTs; TitleVignettes 5133251e :79-83). So a reception-to-bullpen pair is the right period reading of the title door. The leaf (8 ft 8 in) is oversize for any real pair, but it is the binding spec (door_hardware §0, §10.4).

### O2.2 The frame and its face

- **Period.** Hollow metal, 16 ga, welded, mitred, ground smooth, then painted on site to the architect's colour (F1, F2). On a DA frame the profile is **cased open**: the 2 in face and its return wrap the opening, and a flat soffit carries no stop (F1, F19). A DA frame has **no silencers and no strike**. A non-rated DA pair has **no fire label**.
- **Colour.** Dark bronze is documented as the period's **aluminium** finish (F3, Feb 1991). A painted hollow-metal frame in dark bronze, matching a building's dark-bronze storefront and windows, is plausible but **not proven by a period text** [E]. Two searches for a 1985-93 source on painted or anodised dark-bronze interior office frames found only current catalogues (§O7).
- **It is the game's landed Office language either way.**
  - The map's Office doors stand in `Kit_DoorFrame_Steel_DA` in Prop_SteelBrown (#3B2E25, Y 0.030).
  - The Office windows' sash uses the same surface (FrontRoomsInteractableKit.Window.cs :264, FrontRoomsWindowSash.cs :472).
  - So do the station's cased frames (FrontRoomsStationTrim :302) and Run!'s compartment frame.
  - One dark-bronze frame across the building is the consistent choice, and it is the one Red has already seen on every Office map door.
- **Size.** F4, Red's "F4 当年尺寸": 2 in x 7/16 in = **50.8 mm face, 11.1 mm proud**.
- **Against the room.**
  - Office_Drywall (181, 174, 157) Y 0.43, the Office carpet tile (83, 91, 98) Y 0.10, the Office base Cove_Base (0.23, 0.19, 0.14): a dark-bronze frame sits with the brown rubber base, and it is a sharp dark outline on the greige wall.
  - The walnut frame stock it replaces (Door_FrameU (120, 85, 60), Y 0.11) is about 1.9 stops lighter.
  - From the Level 0 room before the Office, the same frame is the dark outline on the yellow wallpaper that the map's Office doors already show from Level 0. That is Red's preview.

### O2.3 The leaf face

- **Period default:** plain-sliced red oak, A grade, book-matched, running assembly, factory clear satin lacquer, same-species hardwood edges (door_wood §2: UMich MasterSpec 08 14 16, NIU 08 1416). Current guide specs still default to the same veneer on both faces (UMich MasterSpec, VA 08 14 00 [S]). Rotary birch was the budget face; walnut and cherry marked executive floors.
- **Built:** **walnut G stays** (D-4, decided 21:15; Red's 10-30 pick, option 4 at +15 %). Walnut is a period office face, the executive grade. Since D-1 is the whole door, the walnut needs no per-face split.
- **B column (red oak):** skipped by the 21:15 note; recorded in §O5.3 only.

### O2.4 The hardware finish within HW-DA1 (US26D satin chrome or US32D satin stainless)

| Evidence | What it says | Tag |
|---|---|---|
| City of San Diego 08710 Finish Hardware, pages [FEBRUARY 1992] (pp. 7-8) | Locksets and latchsets "[satin stainless steel finish 630 (US 32D)] unless otherwise indicated"; "Push plates shall be 4-inch by 16-inch by 0.050 thick"; kickplates "brushed brass or brushed stainless steel, 0.050-gauge, beveled on 4 sides" (16 in high, 10 in where a louver must clear); only the overhead door holders are "chrome plated bronze with satin finish, US 26D". Hinges "[630 (brush finished) stainless steel] [613 …]" is on a page revised [DECEMBER 1994] (p. 5), a year past the lock | R (period) |
| Ives 8400 / 8402 protection plates (Allegion), current page | Satin chrome US26D plates are **brass** with the chrome plated on; satin stainless US32D plates are solid stainless | R (current) |
| door_hardware §2.1 | 626 wears through at grip points to yellow brass through a grey-white nickel ring; 630 cannot wear through and burnishes | K |
| Bommer 3029 DA spring hinge (retailer listing) | Sold in 600 prime, 603, 633, 640 and **652** (BHMA 652 = satin chrome plated on a steel base, the US26D look on steel) | S |
| Door.cs :124-132 (landed OF-K) | The Office's locked map door already pairs satin chrome trim (Prop_ChromeSatin) with satin stainless plates (Prop_StainlessSatin) | K |

**Reading.** In a 1990 office the **trim** (knobs, levers, roses, holders) was very often 626 satin chrome, and the **plates** were stainless 630 or brass even then: in the period spec, and as a practice that lasts to today, because a plated plate wears through where hands and carts hit it [E for "very often"]. 626 and 630 are both satin silver and read as one family at play distance. A DA pair has no trim. Its only trim-like parts are the **hinges**, and DA spring hinges were made in 652 satin chrome.

**Proposal.** D-4's "hardware finish change" is the **hinges, now satin chrome** (Prop_ChromeSatin, the OF rows' surface: tint 0.80, smoothness 0.50, metallic 1, no texture). The **plates stay HW-DA1 in 630** with their landed maps. This is the exact OF-K pairing, and it is period-true. The difference from the walnut door's "Door / brushed steel" (0.64, 0.63, 0.60) is small: slightly brighter and cooler barrels.

**Alternative, if the parent reads D-4 as "all chrome":** 626 plates (plated brass), whose palm oval and the kick plate's worst scuffs wear through to brass. That is a true wear tell, but less usual on plates in the period spec. Cost in §O5.2.

### O2.5 The F1 10 x 30 in lite on the 2.639 m leaf

| Item | Value (title leaf 1.1955 x 2.639 m, bottom 0.018, top 2.657) | Check |
|---|---|---|
| Visible glass | 0.254 x 0.762 m = 10 x 30 in, **300 sq in** | ≥ 200 sq in (Title 8 §3235) ✓ |
| Height | **Y 1.143-1.905** (45-75 in), the set-A bottom shared with F4b and with the push plate's centre | bottom ≤ 48 in ✓ |
| Across the leaf | **centred**: Z 0.4708-0.7248 from the hinge edge (centre 0.5978; slab x ±0.127), the axis of the Level 0 F4b lite (set A keeps the lobby lite centred, door_vision 51) | the literal DA-L1 axis Z 0.273-0.527 is needed only on a leaf with a break code; the title pair has none (door_vision 50 §3) |
| Bead | walnut, the leaf's own material, as built (19 mm face; outer Z 0.4518-0.7438, Y 1.124-1.924); 6.8 mm wired glass in a 9.5 mm pocket | "Light frames shall match the specie and finish of the door" (SD 08150, Feb 1992, via door_vision L2) ✓ |
| Rails | top rail 0.752 m (0.733 m to the bead); bottom rail 1.125 m | a 10 x 30 kit placed at the standard 45-75 in on a tall leaf leaves a tall top rail; this is the lite of the map's Office doors, so the title and the map read alike |
| Clearances | push plate Z 1.0558-1.1574, Y 0.940-1.346: 0.31 m clear of the bead; hinges stay at the hinge edge | ✓ |
| Play (door_vision 50 method: player 1.5 m in front, Relay 1 m behind, eye 1.62 m) | Relay band **0.83-2.10 m**, head and torso, crown just inside; 0.42 m wide. F4b shows 0.83-2.86 m | same LIVE / DARK logic; the title doors have no PEEK |
| Preview | from the Level 0 room before the Office, the short eye-level lite reads against Level 0's tall lobby lite, together with the dark frame | Red 21:25:37 |

The kit change is small: the lite's rect becomes the look's (`LiteTopY` 1.905 for the Office), and the leaf mesh, bead sweeps, glass box and `FrontRoomsDoorLite.AttachStream` rect follow it (DoubleDoorKit.cs `LiteVisible`, `LeafMeshLite`, `BuildLite`).

### O2.6 Office wear (both faces wear alike on a DA door, door_hardware §8)

| Where | Mark (Y above the floor) | Basis | Map |
|---|---|---|---|
| Push plate | palm-burnished oval 70 x 220 mm, grey finger haze, screw grime | door_hardware §8 | **already in Prop_PushPlate630**: reuse |
| Walnut round the push plate | the **hand halo**: darker, slightly glossier lacquer 60-120 mm beyond the plate, strongest on the meeting-edge side, Y 0.90-1.40 | door_hardware §8 ("the leaf texture's job"); sizes [E] | Office wear mask |
| Meeting stile and below the plate | knuckle and forearm marks Y 0.80-0.95, where hands are full of files or a mug; the hardwood edge darkened and rounded Y 0.90-1.30 | [E] | Office wear mask |
| Chair arms | a dulled lacquer band Y **0.58-0.79** toward the meeting edge, faint dark transfer: task-chair seats 16-20.5 in (ANSI/HFS 100-1988) plus armrests 7-10.5 in above the seat | [S] | Office wear mask |
| Chair base and casters | short black arcs on the kick plate Y 0.03-0.15 | [E] | **in the existing heel-streak band**: reuse |
| Carts (mail, file, book) | nicks and chipped lacquer **just above the kick plate**, Y 0.275-0.35: a 10 in plate sits below a cart's bumper and shelf corners | 12-16 in plates were specified for cart doors (Archtoolbox, CDF via door_hardware S12, S13; SD 08710 Feb 1992 writes 16 in by default) [R]; heights [E] | Office wear mask |
| Carpet on both sides | grey vacuum-bumper marks at the plate's bottom edge Y 0.01-0.06; the low tide line on the 630 map reads as carpet-shampoo splash | [E] | reuse |
| Frame (painted steel) | chips to grey primer and bright steel on the jamb faces' inner edge Y 0.15-0.35 (carts); shoe scuffs Y 0-0.10 beside the base | [E] | Prop_SteelBrown's own wear; a frame-only mask is optional |

The wear layer is a variant of the leaf material (walnut G's A / N / S plus one mask), used only by the Office door's leaves. Door_Veneer and every Level 0 door stay byte for byte.

## O3. Our kit today, the facts the Office door must keep (FrontRoomsDoubleDoorKit b7ab5af0)

- **Opening and leaves:** Width 2.4, HeadBottom 2.66 (RoomHeight 2.9 - 0.24 header), Gap 0.003, leaves 1.1955 x 2.639 x 0.0445, LeafBottom 0.018 over the 12 mm saddle (SaddleHalf 0.076, flat ±0.052). Hinge centres Y 0.167, 0.405, 2.418 (HingeCentres). Barrel r 0.011, axis x ±1.1985, z ±0.03425 (HingeAxisOffset). So a barrel spans x 1.1875-1.2095 and |z| 0.02325-0.04525.
- **Today's frame (one walnut mesh for both faces):**
  - linings x 1.200-1.219, |z| ≤ 0.02225 (as deep as the leaf: "a DA spring hinge needs the jamb flush with the door faces");
  - head lining 2.660-2.679;
  - streamline casing from x 1.212 (the 12 mm `CasingReveal` the barrels need: 2.5 mm clear) to **1.250**, 14 mm proud at the opening edge, falling to 4 mm;
  - head casing to 2.710;
  - the casing's base sits at |z| 0.020, 2 mm behind RoomStream's paper reveal planes at |z| 0.022 (RoomStream 6893ef1a :1340-1360).
- **The stream end wall** is 0.04 m thick, so a period frame cannot hide the barrels in its throat, as it would on a 4-7/8 in wall with a 5-3/4 in jamb (door_frame §1.2). The 12 mm step below is that barrel clearance [K, E].
- **Base gap (finding, from code, not yet seen in a render):**
  - `FrontRoomsTitleVignettes.DoorCasingHalf = 1.27f` ("1.2 m + lining 0.019 + reveal 0.012 + face 0.038") is 20 mm wider than the casing, because the reveal is measured from the opening edge, not from the lining's back.
  - So the Office cove base, Run!'s end coves, the Waiting room's VCT border and the pool band all end at x ±1.27. Each leaves a **20 x 100 mm strip of bare wall** beside each casing foot (about 5 px wide at 3 m at 1080p).
  - The Office frame below ends at 1.2628, which leaves 7.2 mm. Each family's trims should read their door's outer edge from the kit (TitleVignettes is ours: Scripts/Rendering/Title).
- **Other callers stay as they are:**
  - RoomStream's swing both ways, SetHingeSide, PlaceLeaf, the terminal door;
  - the pivot names `double door left/right hinge`;
  - the collider-only boxes, LIVE / DARK, ta_door_culling.
  - The start door's stream face is never an Office door: it always opens from a Level 0 room (TitleRooms; TITLE_SEQUENCE §3.5).

## O4. The proposed Office door, within the spec

| Part | Today (walnut, Level 0) | Office door (whole door, both faces) | Stays |
|---|---|---|---|
| **Frame** | walnut linings + streamline casing (code mesh, Door_FrameU) | **a Blender kit through frontrooms_kit (LOD0 / 1 / 2, sidecar).** Dark-bronze 16 ga pressed-steel cased frame, the F4 section of `interact_door_da_common.steel_sleeve_section` (1.5 mm sheet, 3 mm convex bends, 3 steps at LOD0, 1 at LOD1, sharp at LOD2). Path per jamb: soffit at x 1.200 across the 44.5 mm leaf depth -> a **12 mm step** at the leaf-face plane |z| 0.02225 out to x 1.212 (barrel clearance) -> **face band 50.8 mm** from x 1.212 to **1.2628** at **11.1 mm** above the paper plane (|z| 0.0331) -> 7/16 in return to the paper (|z| 0.022). Head band Y 2.672-2.7228, mitred, welded and ground smooth (a faint grind line in the normal map). No stop, silencers, strike or label. Prop_SteelBrown | opening, linings' inner face at x 1.200 (the hinge flanges sit on it), render-only, no collider |
| **Saddle** | 12 mm oak (Prop_WoodOak) | the Steel_DA saddle section (12 mm, 152 mm, 12 flutes of 0.8 mm, a 12 mm screw land) in the frame's finish, the M1 rule of `Kit_DoorFrame_Steel_DA`; countersunk screws about every 0.35 m [E] | height 12 mm, leaves 6 mm over it |
| **Brushes** | black, code | unchanged | yes |
| **Hinges** | 3 per leaf, "Door / brushed steel" flat (0.64, 0.63, 0.60) | the same meshes in **satin chrome** (Prop_ChromeSatin, or an equal runtime flat at tint 0.80, smoothness 0.50, metallic 1) | count, layout (Bommer), pivots, motion |
| **Leaf** | walnut G (Door_Veneer), F4b lite | **walnut G** with the **Office wear variant** (§O2.6); **F1 10 x 30 centred**, walnut bead, wired glass (§O2.5) | slab size, bevels, both faces alike, LIVE / DARK |
| **Plates** | HW-DA1 630, A half left leaf / B half right | **unchanged** (630; maps already carry the period wear) | kits, sizes, places, LODs, the phone's LOD1 rule |
| **Colliders, sound, names** | as built | unchanged | leaves + collider-only boxes; `double door left/right hinge`; StreamOpen / StreamClose / StreamLock |
| **WebGL** | title = Today | Today (no Office door on WebGL) | rule 5 |

Where it shows: the Office room's entry door, also seen from the Level 0 room before it (Red's preview, Tour door 1), and its exit door (Tour door 2). In Descent, a door between the Office and the next family room takes the next room's look (the parent's 21:28 rule), so there the Office's exit door is Run!'s.

## O5. Cost

### O5.1 The Office door (one pair; the Office room has two). Estimates [E] until the kit's sidecar is written

| Part | LOD0 / LOD1 / LOD2 tris | New texture MB (desktop, with mips) | Draws |
|---|---|---|---|
| Frame kit: sleeve about 540 / 280 / 150 (a 68-edge closed section x 3 runs + 2 floor caps) + fluted saddle about 240 / 24 / 24 + about 7 flat heads x 44 = 308 at LOD0 | **about 1,100 / 310 / 180**, replacing about 226 tris of code frame (linings 36, casing 180, saddle 10) that has no LOD | **0** (Prop_SteelBrown 1024² A/N/S ships; it is resident whenever a map Office door is) | frame + saddle **2 -> 1** |
| Leaf + F1 | about today's (the F1 hole and its four bead sweeps have F4b's counts) | 0 | 0 |
| Office wear on the leaf (material variant: walnut G maps + 1 mask) | 0 | **+0.7** (512 x 1024 single channel) to **+2.7** (1024 x 2048) | 0 (a material swap) |
| Hinges in satin chrome | 0 | 0 (no texture) | 0 |
| HW-DA1 plates | 0 | 0 | 0 |
| **Per door** | **about +870 / +84 / -46** against today | **0 required; +0.7-2.7 optional** | **-1** |
| Per Office room (2 doors) | about +1.7 k / +170 / -90 | the mask is shared | -2 |

Phone tier: the frame kit takes its LOD rule; the plates keep LOD1 at most (ReducedTier); the leaf and lite are the code meshes as today. WebGL: Today. Photosafety: door 1 (the Office entry) is at the 3.0 limit today (HV.25). The new lite is smaller (300 against 480 sq in) and the frame is static, so I expect the count not to rise [E]. Run `Tools/lookdev/flash_check.py` on the Tour film after the build.

### O5.2 The all-chrome alternative (§O2.4; the parent's call, sheet only)

626 plate materials: the same 630 A / N / S maps re-tinted to the chrome tint, plus a brass wear-through mask for the push plate (512 x 2048) and kick plate (4096 x 512) sheets: **about +1.4 to 2.7 MB** [E], 0 tris, 0 draws.

### O5.3 The B column (red oak), recorded only: skipped by Red's "按默认做" (parent note 21:15)

Plain-sliced red oak (door_wood option 1), book-matched, running, 5 leaves of 24-26 cm: +9.3 MiB of leaf maps (door_wood §4); T6 1.21 stops (matters only on map doors). If it is ever revived:
- The option-1 maps on disk (`W/door_wood/tex/DW1_Leaf_*`, 10-30) predate the pore fix. They must be regenerated with `FINAL_LACQUER` (40_pores), or Red's "drops" return.
- A map OF-F in oak would also need its break palette (door_break families). The title pair has no break.

## O6. Other lines

- **声音设计 (paused):** none. The leaf stays walnut wood, so StreamOpen / StreamClose (683823, "a large wooden door", AUDIO_CONTRACT.md :49) still fit. A DA leaf has no stop, and its brushes keep it off the frame, so the steel frame is never struck. Precedent: the landed OF-F (a walnut leaf in the dark-bronze Steel_DA frame) already sounds as wood, because DoorType follows the leaf's break family, not the frame (FrontRoomsSoundDirector :1164). The Office needs no row in `W/zone_doors/contracts/sound.md`.
- **关卡设计:** none. No map file calls the kit, and the title doors are ours. The map's OF rows are untouched here (inventory model item 5 comes later).
- **交互设计:** none. Pivots, colliders and names are unchanged.
- **平面视觉 / 信息设计 / 叙事设计:** none. No plate or sign is added. A 1990 suite might carry a room sign on the wall beside a door; it is not proposed.

## O7. Not proven, and leads

1. **Painted dark bronze on interior hollow-metal office frames, 1985-93:** no period text found. The period evidence is the aluminium finish (F3, Feb 1991). The searches returned current catalogues only: CRL 487 OfficeFront frames in dark bronze anodised, UFGS 08120 (09/99), Timely, Frameworks, Alpha Aluminum. The choice rests on the landed Office language (§O2.2) [E].
2. **"Plates stayed 630 on 626 doors" is a practice, not a rule.** The period spec here writes 630 for locks too. Current catalogues sell 626 plates (Ives). Hence the alternative in §O5.2.
3. **Wear heights:** the chair numbers are from secondary sheets [S]; cart and hand marks are [E]. Red's eye at the 0.35 m hero row is the check.
4. **No period photograph of an office DA pair was viewed.** Text tools only, nothing downloaded. Leads for a media pass, not read: *Interiors* and *Progressive Architecture* issues of 1985-1991 on usmodernist.org (one 1984 *Interiors* issue is level_space LS88), and door_hardware §11's corridor photos.
5. **The 20 mm base gap** (§O3) is from code. Check it on the first TODAY frame of the Office room.

---

# Part Z · Station D and the night post, back-of-house, Run! (R-3..R-5)

## Z0. The answer for the station, back-of-house and Run! (twelve lines)

1. **One frame for every non-Level-0 title pair: R-1's zone frame kit** (§O4). It is a 16 ga pressed-steel "cased open" sleeve in `Prop_SteelBrown` (#3B2E25, the E1 / E2, L0-K, RN-K and compartment-frame finish) at the F4 section:
   - a steel soffit at x 1.200, then the 12 mm hinge step to x 1.212;
   - a 50.8 mm face to x 1.2628, 11.1 mm proud;
   - the Steel_DA fluted saddle in the frame's finish.

   The night post, LOW and Run! pairs take it unchanged. **Station D alone** keeps today's outer edge 1.250 (a 38 mm face band), because D-3 binds its rail and base stop at 50 mm and its plate at 75 mm; D also keeps its walnut soffit and oak saddle (item 2). The zone reads from its leaf, lite and trim.
2. **Station D (D-3):** only its station face changes: the steel casing, stainless casing guards, the lining's station-side end in bronze, the station-side barrels in 630. Leaves stay walnut on both faces and the F4b lite stays dead in its walnut bead. That is the period's normal door (wood leaves in a hollow-metal frame), and D's stream face stays Level 0.
3. **D has two builders.** At the start the player sees the stream's terminal pair (RoomStream.BuildDoor, its +Z face) when it shuts 4 m in. StationDress's static entrance appears only after ClearStartArea. Both need the station face, or the first view of D stays walnut.
4. **E1's 50 × 50 × 3 mm guard cannot wrap D's jambs.** Both jambs carry DA spring hinge barrels at y 0.066-0.269, 0.304-0.507 and 2.317-2.520 m, reaching x 1.2095 m in the 12 mm reveal. A 1.5 mm stainless angle over the frame's inner arris (38 mm face wing on D, 50 mm on the night-post pairs; 11 mm side wing; 0.10-1.32 m) clears them by 1 mm in every swing. It is the closest form; say so on the sheet.
5. **Night post / Waiting pairs (whole door):** D's station look on both faces: R-1's frame kit on both faces, guards on the night-post face only (as E1), walnut leaves, F4b. No plate (the BUSINESS HOURS plate is D's alone).
6. **Back-of-house (title LOW pairs and the new map member):** Red's three words are the spec: painted steel, kick plate, louvre. Leaf = painted hollow metal; lite + louvre = A2 (F1 10 × 30 over a 10 × 16 in sight-proof louvre, 20 ga); kick plate 10 in, which is exactly the Feb 1992 spec's rule for a louvred door (16 in otherwise; Z1). The unused steel leaf kit is **`Kit_DoorLeaf_Steel_PaintedMetal_DA`** (5,536 / 1,724 / 212 tris, F1 already cut, slot `Painted_Metal`): the base of the map leaf.
7. **Back-of-house paint keeps the lock read:** the locked row stays almond `Door_Enamel` (every key door), so the free row's paint must sit at least 1 stop below it (T6's principle) and above the walnut: a warm service grey near sRGB (140, 135, 124), Y about 0.24 [E]. Never the stair putty #B7B09E, never red or yellow.
8. **Run! (title RUN! pairs):** the Run! B steel pair at title scale: almond `Prop_SteelAlmond` hollow-metal leaves (the SteelRun finish of the room's own ELECTRICAL ROOM door), F5 15 × 15 wired lites centred like F4b, pressed-steel lite kits, HW-DA1 (10 in kick plates, as SteelRun_DA carries), the bronze frame. It is a ward / service traffic pair, never the rated hold-open pair of 03 §4.4.
9. **HW-DA1 stays on every zone pair** (Red: "规格和结构上的要求和之前一样"). Armour plates (S10, S12) and the Ward laminate are recorded, not proposed.
10. **Run! polish (later stage):** side doors get a one-face Steel_DA frame built for the prop on a 0.16 m wall (no ×0.92 squeeze). The compartment pairs get closers (a held-open fire door shuts by its closer: US 3,204,154, Z2) and armature plates on the pull face at 1.95 m (US 4,852,919, Schlage 1989, Z3).
11. **Sound:** station and Waiting leaves stay wood, so no change. Run!, LOW and map back-of-house leaves are steel: a contract for Red (§ZD).
12. **Cost:** no new texture for station or Run!. Back-of-house needs one new paint material on existing maps and one new louvre part. Against today's 26 k tris a pair (LOD0, mostly HW-DA1), the zone pairs add about +1 k (night post), +1.4 k (Run!), +3.2 k (LOW) and +0.15 k (D). Every new part gets LOD0 / 1 / 2.

## Z1. Facts that hold for all three zones (K)

| Fact | Value | Source |
|---|---|---|
| Opening, leaves, gaps | 2.4 × 2.66 m; leaves 1.1955 × 2.639 × 0.0445 m; 3 mm gaps, brush strips; 12 mm oak saddle, leaf 6 mm over it | DoubleDoorKit.cs header, :31-49 |
| Lining (jamb stock) | x 1.200-1.219 m from the centre line, as deep as the leaf (a DA spring hinge needs the jamb flush with the door faces); one mesh for both faces (`FrameMesh(both)`) | :43, :339-351 |
| Casing envelope | inner edge x 1.212 (12 mm reveal for the barrels), outer edge **1.250**, 14 mm proud, streamline 16-point profile; head casing top 2.710 m | `CasingProfile`, :45-49, :352-362 |
| Hinge barrels | axis x 1.1985, z ±0.03425, r 0.011; centres from `HingeCentres()`: barrels at y 0.0655-0.2685, 0.3035-0.5065, 2.3165-2.5195 | :265-271, `HingeJambMesh`, `HingeBodyMesh` |
| Swing | a leaf turns on the barrel of the face it swings toward (PlaceLeaf); both barrels then move toward the opening (-x on the +x jamb), never outward over the casing | :175-186 (checked by hand: §S.6) |
| HW-DA1 | push plates 4 × 16 × 0.050 in at y 0.940-1.346, z from the meeting edge 38 mm; kick plates 10 in (y 0.021-0.275); 630; both faces | door_hardware/10 §9 |
| Today's pair cost | leaf with F4b + bead 224 tris each; lining + casing 216; brush 108; hinge jamb halves 2,376; barrels 1,188 per leaf; plates 4 × 2,238 + 4 × 2,978 (LOD0) ≈ 26.4 k tris at LOD0; no LODGroup on the procedural parts | W/door_inventory/sweep_code.md row 1 |
| TitleVignettes' casing constants | `DoorCasingHalf` 1.27 and `DoorCasingTop` 2.73 count the 19 mm lining once too often: the real casing ends at 1.250 / 2.710. The Waiting border strips and the Run! end lower-tone and cove strips stop 20 mm short of the casing today | TitleVignettes.cs:120-123, :345-353, :409-415 (same finding as R-1, journal 21:3x) |
| Lite rule for DA leaves | ≥ 200 sq in per leaf, bottom ≤ 48 in: F4b 480, F1 300, F5 225 all pass | door_vision/50 §1 (P1, Title 8 §3235) |
| A DA pair is never rated or an exit | no fire assembly, no panic hardware | door_hardware/10 §4, §10.5 (S4) |

**Under D-1 = the whole door,** a family pair needs no per-face split: the frame, the leaf, the lite form and the lite kit are the family's on both faces. My earlier through-lite finding (a lite is one hole through the one slab) now matters only for **station D**, whose stream face stays Level 0: D keeps the walnut leaves and the F4b lite, and its only per-face parts are the casing, the guards, the lining end and the +Z barrels.

**The shared frame (reconciled with R-1).** R-1 already designed the zone frame kit (§O4, §O5.1):
- a Blender kit through frontrooms_kit, LOD0 / 1 / 2, on `interact_door_da_common.steel_sleeve_section` (1.5 mm sheet, 3 mm convex bends);
- per jamb: the soffit at x 1.200 across the leaf depth, a 12 mm step at the leaf-face plane out to x 1.212 (barrel clearance), a 50.8 mm face to **1.2628** at 11.1 mm proud, a 7/16 in return to the paper;
- the head mitred, welded and ground smooth (Feb 1991 "welded unit type", "trim faces mitered, welded, and finished smooth", door_frame F2);
- the Steel_DA fluted saddle in the frame's finish;
- about 1,100 / 310 / 180 tris, replacing about 226 tris of code mesh, 0 new textures.

Part Z uses that kit for the night post, LOW and Run! pairs (whole doors). TitleVignettes' end trims must then follow the look's frame edge (R-1 finding (a)).

The one exception is **station D**: D-3 binds the rail and base stop at 50 mm and the plate at 75 mm, so D's station face takes the same section with a **38 mm face band (x 1.212-1.250)**, about 150 tris a face at LOD0 [E]. At 1.2628 the rail ends would run 12.8 mm under the casing, and the plate's gap would shrink from 25 to 12 mm.

Hinges for the station, back-of-house and Run! pairs: **630**, the city spec's hinge default ("Hinges shall be [630 (brush finished) stainless steel] [613 …]", Z1 §2.4.E). That clause sits on a page revised December 1994 (the page dates mapped this pass), so it is NEAR, not IN. It matches HW-DA1 and the station's guards. The Office takes satin chrome (R-1).

---

## S. Station (车站): D's station face, and the night post / Waiting pairs

### S.1 What is on disk (cite, never redo)

- **The station language, Red's pick.** Walls 2 · Post Room, Red "这个版本可以" (10-22 15:44, DEC 15:4x): eggshell #9AA3A6, a 6 in crash rail #3E4650 at 0.78-0.93 m, VCT #B2AEA3 with a 0.305 m dark border #3A3A38, 4 in black rubber base #1A1918, and at E1 "a dark-bronze steel cased frame with stainless corner guards" (NOTE_1022_first_room_DECIDED.md:65-75; 61 §5).
- **As built** (StationTrim d618387a :11-19, :299-327): pressed steel on both faces of E1 / E2 (face 0.05, proud 0.012, flat boxes), 12 mm throat liners, `Prop_SteelBrown`; guards 50 × 50 × 3 mm stainless angles round each jamb arris on the station face, y 0.10-1.32 m, `Station_StainlessGuard` (#A8A9A4, metallic 1, smoothness 0.68; 70 §4); VCT through the throat; a rubber reducer at the Level 0 face; a 0.30 m gypsum band. Render-only, one mesh per material and mask. The station has **no leaf of its own**: E1 and E2 are cased openings, and D is the start door (R2: "The main entrance IS the start door, seen from inside the station", DEC:60-62).
- **D today** (StationDress 759a22a9 :236-279; RoomStream 6893ef1a :1313-1414): the walnut DA pair, `Door_FrameU` casing and linings, `Door_Veneer` leaves, oak saddle, F4b lite in a walnut bead, HW-DA1, hinges in the runtime flat "Door / brushed steel".
- **The 10-22 "不改" for D** (61:209 "标题代码的双开门，不改") had two reasons, both gone. The leaves were "标题代码的东西，不能改" (61:133); since 10-29 the pair is our kit (Rendering). And a plate "挂在门扇正中" would sit on the leaves' seam, so the plate went on the liner beside D (DEC 18:0x-18:1x). Red's 19:35 names 车站, and 21:15 "按默认做" settles D-3.

### S.2 What a c. 1990 post room / staff entrance pair looked like

| Point | Evidence | Weight |
|---|---|---|
| A DA pair in a 1990 office is a **non-rated interior pair with a lite**, used "suite to corridor, pantry, mailroom" | door_hardware/10 §4 from Cal. Title 8 §3235 (S4, the 1979 UBC) | IN, R |
| **Wood leaves in hollow-metal frames** are the commercial default ("the most common configuration"); hollow metal goes to high-traffic corridors, stairs, docks, mechanical rooms, healthcare | Z4 (current article, a long-standing practice); 05_locked_door_type §4 (same distributor) | LATER, R |
| A DA frame is the **cased open** profile: 2 in face, 1/2 in return (7/16 at 5-3/4 in jamb depth), no stop, flat soffit | door_frame F1, F11, F12, F19 | LATER (2 in back-dated to 1990 as [E], door_frame/10 §1.5) |
| Steel casings could be **separate trim members** ("Adjusta-Trim 2" face, 1/2" return legs") and knock-down frames had separate mitred casings by 1981-83 | door_frame F6 (1996), F21 (US 4,395,855) | NEAR / IN |
| **Light frames match the door's species** on non-rated wood doors ("Light frames shall match the specie and finish of the door") | door_vision L2 (San Diego 08150, Feb 1992) | IN, R |
| The period's **entrance finish was dark bronze**: anodised storefront entrances, Feb 1991 | door_frame F3 (08410) | IN, R; dark-bronze paint on steel is our choice [E] (door_frame/10 §1.2) |
| Plates in a 1992 city spec: push 4 × 16 × 0.050; kick plates brushed brass or **brushed stainless**, 0.050, bevelled, 16 in high (10 in at a louvre); house finish 630 | Z1 (08710, Feb 1992 pages) | IN, R |
| A postal workroom opening today is a **DA traffic door** (a polyethylene leaf with barred polycarbonate windows, to USPS's RE-5) | Z5 (current product page) | LATER, R: shows the door type, no period weight |
| A dated transit or postal staff pair (1985-93) | not found (searches: SOURCES.md, Z rows) | gap, recorded |

**Reading.** The 1990 staff pair of a post room or a security post is a flush wood (or laminate) DA pair with a lite, in a painted hollow-metal frame, with satin-stainless push and kick plates. That is D with its station face. The leaf need not change: a veneer door has the same face both sides [E], and the station's only leaf is D's walnut.

### S.3 How D's sealed F4b lite reads (K)

- **Start-area phase:** D is the stream's terminal pair. Its F4b is real wired glass, LIVE while the door is open. When CloseTerminalDoor shuts it for good, it turns DARK: "an opaque dark wired pane that keeps its reflection" (FrontRoomsDoorLite 17f9bcb8 header; Glass_WiredDark), "for good on a dead door (the station entrance, the shut terminal door)". From the station it reads as a dark vestibule behind wired glass, holding the TV's blue light, the troffers and the room in its reflection.
- **After ClearStartArea:** the static entrance carries a 1 mm DARK skin, 0.5 mm proud of the +Z face, in the same outline (kit `BuildLite`, Plus; door_vision/50 §6 item 1: "its dead skin takes the same outline").
- **Fiction:** the entrance must be "solid or has a vestibule, never glass to an outside" (DEC:16). The title's airlock is that vestibule, so the lite is allowed. The 10-29 plan's "solid leaves, never glass" (70 §6.3) predates door vision; set A (Red 10-30 'A 按区域四种', L.1205) superseded it. The plate rule had already covered a lite: "If the leaf gets a vision panel, the plate goes on the latch-side wall at the same height" (DEC:48-49), which is today's liner mount.
- **Keep:** F4b and its walnut bead, LIVE / DARK, no peek on D.

### S.4 Keep list for D (do not move)

1. **BUSINESS HOURS plate**: 457.2 × 152.4 × 1.6 mm, adhesive, at `PlateAnchor`: door-side edge 75 mm west of the opening (x 6.225), centre 1525 mm, on the liner (z 0.024) and, after the clear, on the data wall (z 0.08); ≥ 560 mm clear wall; nothing tall within 50 mm (70 :351-356; SS `PlateDoorGap` 0.075, `PlateCentreHeight` 1.525). Pre-existing note for 信息设计, not ours to change: SIGNS asks the plate's door-side edge to be 50-100 mm from the frame's outer edge (DEC 18:1x). That was written when D had no frame (C1-6). Since 10-29 20:14 the casing ends 25 mm short of the plate (kit comment :45-47).
2. **Rail and base stop 50 mm out** (`FrontRoomsStationTrim.DoorStop` .05, "the stream leaves open to 90 deg") = the casing's outer edge 1.250. The new casing keeps that edge exactly.
3. **No header panel / transom over D** (C1-5): the bare liner header 2.73-2.90 stays. The steel head casing tops out at 2.710 like today's.
4. **Render-only**: no collider on any new part; the stream's doorway colliders (returns, header, leaves, the 10-29 collider-only pulls and kick plates) unchanged.
5. **D's stream face = Level 0 byte for byte** (scope): the -Z casing, the shared lining's soffit, the leaves, the saddle, the F4b hole and the -Z barrels.
6. **Masks**: the static entrance's renderers keep `SetMask(r, false)` (StationDress :275-279).
7. **WebGL**: lites off on WebGL as today (kit :66-71). Whether D's station face ships on WebGL is the WebGL track's call; desktop does not change for it.

### S.5 D has two builders (K, finding)

| Phase | What stands at D | Builder | Seen? |
|---|---|---|---|
| Start area (the title hands over; the door shuts 4 m in; "the player turns to the doors and the plate", DEC:125) | the stream's **terminal pair**, Faces.Both, centre plane in the facade, its +Z face toward the station, beside the liner | `FrontRoomsRoomStream.BuildDoor`, made terminal by `EndStreamAt` (:572-616) | **yes, first and longest** |
| After `ClearStartArea` (happens only while no chunk round the strip is built, PLAN §4.7) | the **static entrance**, Faces.Plus, on the data wall's inner face z 0.08 | `FrontRoomsStationDress.DressEntrance` (:244-279) | yes, when the player comes back |

D-3 must dress both, with the same parts in the same place relative to the opening, or the swap at ClearStartArea changes the door. On the terminal pair, apply the +Z station face when the stream ends at a map that holds a station (`map.HasStation`). That happens at `EndStreamAt`, while the door is shut and its +Z face cannot be seen. The parent's 21:28 note names only StationDress; this row adds the RoomStream half.

### S.6 The guards at D (geometry, K)

- E1's guard (50 mm face wing + 50 mm throat wing, 3 mm) wraps the arris of an opening with no leaf. At D **both jambs are hinge jambs**. The +Z barrels sit in the 12 mm reveal (x 1.1875-1.2095) at y 0.066-0.269 and 0.304-0.507 (inside the guards' 0.10-1.32 band) and at 2.317-2.520. A throat wing cannot exist there.
- **Swing check.** Swinging toward the station, the leaf turns on the +Z barrel and its hinge-edge corner moves toward the opening (to x 1.142 at 90°). Swinging toward Level 0, it turns on the -Z barrel and carries the +Z barrels with it, also toward the opening (x 1.150 at 45°, 1.130 at 90°). Nothing passes outward of x 1.2095.
- **Closest form:** a 1.5 mm (16 ga) stainless guard wrapping the steel frame's inner arris, y 0.10-1.32 m, in `Station_StainlessGuard`, on the station face only.
  - Face wing: 38 mm on D (x 1.212-1.250); 50 mm on the night-post pairs (E1's wing, on R-1's 50.8 mm face).
  - Side wing: 11 mm down the frame's inner rise at x 1.2105-1.212 (the rise from the 12 mm step at |z| 0.02225 to the face at 0.0331).
  - Clearance to the barrels is 1.0 mm. On D the rail stop and the plate stay where they are (the wing ends at 1.250; the plate starts at 1.275 and at y 1.449).
- **Honest flaw:** it is narrower than E1's guard. Period support for guards on a door frame is weak [E]: a hollow-metal frame is itself the protection, and E1's guards protect outside corners where carts turn.

### S.7 Proposed faces (within the spec) and cost

**S-1 · D's station face (REC; D-3), on both builders:**

| Part | Today | S-1 |
|---|---|---|
| Casing, +Z face | walnut streamline, `Door_FrameU` | R-1's F4 steel section (§Z1) with a 38 mm face band (x 1.212-1.250), 11.1 mm proud, in `Prop_SteelBrown`: today's envelope |
| Lining | walnut both faces, one box | walnut stays (its soffit is seen from the stream side). A 0.5 mm bronze cap covers its +Z end (the 12 mm strip beside the leaf), so the station face reads as one steel frame |
| Guards | none | §S.6, station face only |
| Leaves | walnut G both faces, F4b | unchanged |
| Lite | F4b, walnut bead, DARK for good | unchanged (§S.3) |
| HW-DA1 | 630, both faces | unchanged; it carries the station's stainless with the guards |
| +Z barrels | runtime flat "Door / brushed steel" | `Prop_Stainless630` (a real Surface; the -Z barrels stay as they are) |
| Saddle, brushes | oak, black | unchanged (the VCT field and border stop at D's jambs, 70 §5) |
| Wear | — | the kept room: clean, as PICK ("macro tone, dirt … 0", 70 §4) |

**Cost of S-1 (per builder; one stands at a time):** the +Z band of R-1's section ≈ 150-250 tris, minus the walnut +Z half (≈ 108); guards ≈ 100; lining end cap ≈ 12; LOD1 / LOD2 ≈ 60-120 / 16-40 [E]. **0 new textures, 0 new materials** (`Prop_SteelBrown`, `Station_StainlessGuard`, `Prop_Stainless630` are all loaded already). +2 draws (bronze casing, guards). No collider, no light, no sound change (the leaf stays wood).

**S-2 · Night post / Waiting pairs, the whole door (REC; D-1 = option 2):** D's station look on both faces of both pairs (the room's entry and exit doors; between two family rooms the forward-walk rule applies):
- R-1's zone frame kit on both faces (50.8 mm face, steel soffit, the fluted steel saddle);
- guards on the night-post face only, as E1 (its guards are on the station face only);
- walnut G leaves with F4b in the walnut bead, HW-DA1, hinges in 630 (`Prop_Stainless630`);
- no plate.

The preview from the Level 0 room before it: the same walnut pair, now framed in a crisp dark-bronze steel band about 2 stops darker than the walnut casing (albedo Y .030 vs .115, R-1's measures), with stainless at the night-post side.

Cost per pair:
- frame kit ≈ 1,100 / 310 / 180 tris (R-1's estimate), replacing ≈ 226 tris of code mesh;
- guards ≈ 100 / 24 / 8 [E];
- 0 new textures; draws: frame and saddle 2 → 1, guards +1, so net 0;
- no sound change.

Two small dress notes for the Waiting vignette (ours, TitleVignettes):
- let the end trims follow the look's frame edge (§Z1; R-1 finding (a)): 1.2628 / 2.7228 for the zone frame kit (1.250 / 2.710 for the walnut pair), not today's `DoorCasingHalf` 1.27 / `DoorCasingTop` 2.73;
- optionally, return the 6 in crash rail along the end walls, ending square at the casing (61 §5: "屋里每面墙和墙垛都有，门洞处收头"). Today the vignette rails the long walls only.

**Sheet:** TODAY | S-1 (D, both phases) | S-2 (Waiting, both faces), with VISUAL_STANDARD §2.2 rows and a hero row of the guard at the low barrels. No ask.

---

## B. Back-of-house (后场): the title LOW pairs and the new map member

### B.1 What is on disk

- **05_locked_door_type (10-02, 3fb99f6f):** the 1990 split is "occupied rooms (veneer) and service rooms (painted steel, keyed storeroom lock)". Hollow metal goes "in stairwells, back entries, corridors, and anywhere security is a concern" (cdf, read). The storeroom door has:
  - an almond-white enamel leaf `Door_Enamel` (sRGB ≈ (205, 197, 176), albedo Y ≥ 0.50, smoothness 0.45-0.55);
  - a dark-bronze 2 in steel frame;
  - a 10 in stainless kick plate on both faces;
  - an engraved EMPLOYEES ONLY plate and a storeroom knob;
  - wear: "scuffs to grey primer just above the kick plate", hand grime at 0.85-1.25 m.
- **door_vision/50 (b68d27cc):** back of house = "steel or laminate DA doors with armour plates; kitchen traffic doors"; squares on service doors, portholes on food service, **louvres on janitor / mechanical rooms** (§5). **A2 = DA-L1 + louvre** (§0 :24, §2.6, §6 item 6 :182, §7 F12):
  - visible 10 W × 16 H in (254 × 406 mm) on the lite's axis, Y 0.330-0.737 (13-29 in);
  - louvre frame Y 0.311-0.756, 39 mm over a D1.6 kick plate (36 mm over the title's);
  - **sight-proof inverted-V blades**: light and sound pass, no view;
  - 20 ga galvanised steel, factory primed (L2, 08150 Feb 1992); SDI 111-C louvre details (P13);
  - Steel_DA only (it hits the Ward armour); +250-350 tris [E];
  - not built (no louvre kit in PM, sweep_code §3 #7). Builder to add in `interact_door_lite_common.py`, keeping its asserts (≥ 50 / 45 / 40 mm) (50 §8).
- **The title LOW room** (TitleVignettes :101-102, :576-628; Descent only, not in Tour):
  - "a Low stock room (Red 10-31 "低矮区按推荐")";
  - Level 0 paper walls and the 2' × 2' print ceiling, a 2.4 m soffit stepping up 1.2 m before each door;
  - eight 4 ft wraps; bookcases, ply and rolling cabinets, a pallet and crates, a hand truck with cartons;
  - "no base (the game's Low rooms are Level 0 theme)".
- **The map today** (inventory B7): no back-of-house member; the Low side takes the Standard side's row (Door.cs:151-163); back-of-house modules open by arches (Low_Storage_2x3.asset `backOfHouse: 1`).
- **Red's spec** (21:26:27, via the 21:28 note): "新增一类后场门（漆面钢门、踢板、百叶）……玩家在矮区会看到更像设备间的门".

### B.2 Period read (one language for both sizes)

| Element | 1990 practice | Source |
|---|---|---|
| Leaf | flush hollow-metal, 1-3/4 in, 16-18 ga, prime-painted and painted on site | door_frame F2 (Feb 1991); 05 §6.2 |
| Frame | hollow metal, 2 in face, dark trim paint (ours: dark bronze, the storeroom family) | 05 §6.2; F1 |
| Kick plate | brushed stainless, 0.050, bevelled 4 sides; **16 in high, 10 in "where necessary to clear a louver"**; door width less 1-1/2 in for pairs, less 2 in for single doors | **Z1 (S1 08710, Feb 1992 page, re-read this pass: the height rule was not recorded before)** |
| Louvre | sight-proof, metal 20 ga galvanised, factory primed; on janitor and mechanical doors | 50 §2.6, L2 |
| Lite | a DA leaf must have one ≥ 200 sq in (A2's F1 = 300) | P1 |
| Closer | none: no surface closer on a DA door | door_hardware/10 §4 (S10) |

A real stock room usually had a single keyed storeroom door (05). The DA pair is the closest the structure allows for a room that hand trucks go in and out of. Say so on the sheet.

### B.3 The unused steel leaf kit (name it)

**`Kit_DoorLeaf_Steel_PaintedMetal_DA`** (PM; 5,536 / 1,724 / 212 tris; slots `Painted_Metal`, `Prop_Aluminium` (kick plates in the mesh, swapped to stainless by `LeafPlateTo`), `Prop_Chrome`, `Glass_Wired`; F1 10 × 30 already cut at liteRect Z 0.273-0.527, Y 1.143-1.905; tags hollow_metal, double_acting). No row and no script use it (grep of Scripts / Editor: no hit). A second spare is `Kit_DoorLeaf_SteelLite_DA` (5,618 / 1,740 / 208, `Door_Enamel` + `Prop_Glass`). Both are 0.98 × 2.08 m map leaves. They are the base of the map leaf and the section reference for the title leaf, not parts for the 1.1955 × 2.639 m pair.

### B.4 Paint, and the lock read

- **The locked row keeps the key-door look:** almond `Door_Enamel`, as L0-K / OF-K / RN-K.
- **The free row** needs a paint that keeps T6's principle (door_wood/40 §3: a locked steel door reads ≥ 1 stop brighter than a free door in a dim Level 0 cell). It must also stay brighter than the walnut free doors and never be a dark rectangle (HR03 rule 03, 05 §1 item 2). So: a **warm service grey, sRGB ≈ (140, 135, 124), albedo Y ≈ 0.24** [E].
  - Y ≈ 1.2 stops under the almond target (Y ≈ 0.56) and ≈ 1.1 stops over walnut (Y .115, R-1).
  - A new `.mat` on `Painted_Metal`'s maps: 0 new textures. Measure it with T6's method at build: albedo ratios are not render ratios.
- **Not:** `Prop_SteelPutty` / `Prop_SteelAlmond` (both render at 0.34 in 05 §5.3, too close to the almond key door); the stairwell's putty #B7B09E (the EXIT's read, A13 contrast); red or yellow (fire gear and hazards only, 03 §4.5).
- **The title LOW pair is free**, so it takes the free paint.

### B.5 Proposed doors (within the spec) and cost

**B-1 · Map member, single DA leaf (REC; D-2 = option 2), rows in our Door.cs `Rows`, the member appended after Exit (and Pool):**

| Row | Frame | Leaf | Trim | Signs | Lite / louvre |
|---|---|---|---|---|---|
| **BH-F** (free) | `Kit_DoorFrame_Steel_DA` (`Prop_SteelBrown`) | new **`Kit_DoorLeaf_SteelLouvre_DA`** = PaintedMetal_DA + the A2 louvre, in the free grey | rose + lever, bored DA latch and strike, chrome (the OF-F set; Red A19 "保留把手") | — | F1 + louvre (F12: louvre Z 0.273-0.527, Y 0.330-0.737) |
| **BH-K** (locked) | as BH-F | the same kit, in `Door_Enamel` | L0-K's mortise set, deadbolt, knob (chrome) | EMPLOYEES ONLY (sign cell 0, top rail) + number plates, as L0-K | as BH-F |

- **Fit checks** (50 §1, §7):
  - the louvre frame (Z ≤ 0.546, Y 0.311-0.756) is 54 mm from the break window (Z ≥ 0.600) and wholly below the `_FR_DAMAGE` mask (Y 0.80-1.40), so the steel break cassettes work unchanged;
  - it is 39 mm over the leaf's kick plate (Y ≤ 0.272);
  - the knob / escutcheon (Y 0.968-1.0, Z 0.92) and the number plate (Y 1.0, Z 0.79) are clear of it.
- **Break:** the Steel family (`Kit_DoorLeafBreak_Steel_DA`), plus a new 'veneer' palette entry for the grey (paint over grey primer [E]). BH-K's almond is L0-K's.
- **Cost:** leaf ≈ 6,200 / 1,900 / 230 tris [E] (5,536 / 1,724 / 212 + the louvre ≈ 600-900 at LOD0 with its frame and 8 screws, ≈ 150 / 12 below); frame as L0-K. One new material (the grey), 0 new textures. The louvre's blades go in the leaf's paint slot, so the 4-slot kit rule holds.
- **Sound:** DoorType Steel (exists). The louvre leaks sound: footsteps louder (50 §2.6). Contract, §ZD.
- **Light through the louvre** (sight-proof): its blades lit from the far side, the level **read, never typed**, as `FrontRoomsRunDoorGap` reads its light (60_production_66 D.3). Our build decides this; no map change.
- **Others:** the scene-3 museum stand's `% 4` gains the member (交互设计, the parent's 21:28 note). 关卡设计 owns the rule (§B.6).

**B-2 · Title LOW pairs, the whole door (REC; one language with B-1):**
- leaves: painted hollow-metal in the free grey;
- **A2 centred on each leaf like F4b**: F1 Z 0.471-0.725, Y 1.143-1.905; louvre Z 0.471-0.725, Y 0.330-0.737. The title pair has no break code, so centring is allowed (50 §3);
- pressed-steel lite kit and louvre frame in the leaf paint, 6 screws on one face (50 §4);
- `Glass_Wired` with the stream's LIVE / DARK;
- HW-DA1 unchanged (its 10 in kick plate is Z1's louvre rule);
- R-1's zone frame kit both faces (§Z1), hinges in 630;
- wear: grey-primer scuffs above the kick plates, hand-truck nose marks at 0.15-0.35 m, grime at the push plates (05 §6.2; door_hardware/10 §8).

Cost per pair:
- leaves ≈ 2 × 1,100-1,400 tris (today 2 × 224); LOD1 / LOD2 ≈ 2 × 300 / 2 × 40 [E];
- frame kit ≈ 1,100 / 310 / 180 (R-1), replacing ≈ 226;
- so ≈ +2.6-3.2 k at LOD0;
- one new material (the grey, shared with B-1), 0 new textures;
- draws: the louvres in their own submesh +2, frame and saddle −1;
- sound: a steel DA pair (contract, §ZD).

### B.6 The map rule (关卡设计's; the period basis for our suggestion)

Period service doors serve storage and utility, closets, janitor, mechanical and electrical rooms (05 §4, SDI 108; 50 §5). Suggestion for the Map stage's contract: a map door is back-of-house when its Low side's cell belongs to a module flagged `backOfHouse` (Low_Storage_2x3 has it) or is a Low zone bordering a Standard zone. 关卡设计 decides. Until their rule lands, no map door uses the member, and the player sees no change (21:28 note).

---

## R. Run! (run！): the title RUN! pairs, then the side doors and the compartment pairs

### R.1 What is on disk

- **Run! B** (60_production_66 dd0e36e2; 03_run_research 798c1520; 71_run_room e52ce6d8) is "a service corridor of fire compartments", already on the alarm (71:23, :263). Its materials and parts:
  - block walls in two tones, VCT and lane, bumper rails, cove, exposed services;
  - almond steel `Prop_SteelAlmond` on the doors and fire gear housings; dark-bronze `Prop_SteelBrown` frames;
  - red only on fire gear, yellow only on hazards (03 §4.5).
- **Its doors:**
  - **RN-F goal door:** `Kit_DoorFrame_Steel_DA` + `Kit_DoorLeaf_Ward_DA` (wood-grain laminate, armour plates to Y 0.831, F5, lever, never locked). At 5 m it reads "a walnut door with a stainless kick plate and a square wired window" (D.2).
  - **RN-K side doors:** Steel_DA frame squeezed ×0.92 + `Kit_DoorLeaf_SteelRun_DA` in almond, dead F5, locked trim, plates (B.2; RunDress b94ade20 :558-622).
  - **FD1 / FD2 compartment frames:** `Kit_RunCompartmentFrame` + two SteelRun_DA leaves swung 115° on `Kit_RunMagnetHolder` at 1.95 m. They have no closer (H.3; RunDress :624-661).
- **03 §4.4:** "cross-corridor double doors on hold-opens, with wired-glass vision panels, are the most '1990 hospital' object we could add"; held open magnetically and self-closing (current NFPA 101; 1990 UNVERIFIED). These are **rated** pairs, so not a DA pair (door_hardware/10 §10.5).
- **door_vision/50 §5:** service and ward doors are steel or laminate DA doors with armour plates and square lites. The F5 square is "the hospital / service door" and "the 'lit square' at the end of a dark corridor" (§2.2, quoting 03 L29).
- **The title RUN! room** (TitleVignettes :283-374, in Tour): a 2.84 m corridor (faces x ±1.42) with 0.16 m block walls, 4 cells, the ELECTRICAL ROOM side door (almond), the janitor cart, strips with the red tube. Its far pair is the rig's goal (goalPoint at z 12, `goalHalf` = the kit's half width). The services ride at 2.76-2.80 over the head casing. The end walls are the stream's reveals: 0.15 m strips each side of the casing.

### R.2 Period read

The title RUN! pair is a DA pair, so it can only be the corridor's **service / ward traffic pair** (no latch, push plates both faces, kick or armour plates, a square lite). It can never be the rated hold-open pair (no DA in a fire assembly, S4). The rated pairs are FD1 / FD2, and they need closers.

### R.3 Proposed title RUN! door (within the spec; D-1 = option 2) and cost

**C-1 · the Run! B steel pair (REC):**
- leaves: almond `Prop_SteelAlmond` painted hollow metal, the SteelRun finish of the room's own side door;
- **F5 15 × 15 in wired lites centred on each leaf like F4b**: Z 0.4075-0.7885, Y 1.194-1.575; kit frame Z 0.3885-0.8075, Y 1.175-1.594. That is 0.25 m from the push plate's edge (Z 1.0558). The pair's squares stand 1.2 m apart, centre to centre;
- pressed-steel lite kits in the leaf paint, 8 screws on one face (50 §4 [E]);
- `Glass_Wired`, LIVE / DARK as every stream door;
- HW-DA1 unchanged: its 10 in kick plates are what SteelRun_DA carries;
- R-1's zone frame kit both faces (§Z1; `Prop_SteelBrown`, as `Kit_RunCompartmentFrame` and the RN-K frames), hinges in 630;
- wear: cart scuffs on the lower leaf, grime at the push plates.

What it gives:
- the preview from the Level 0 room before it: a light almond pair with two low squares, unlike any walnut door;
- inside the room: the far pair is the goal, and "the lit square at the end of the corridor" returns in the title;
- through a square: a torso without its head (50 §6 item 3).

**Cost per pair:** leaves ≈ 2 × 500 tris (today 2 × 224), LOD1 / LOD2 ≈ 2 × 120 / 2 × 20 [E]; frame kit ≈ 1,100 / 310 / 180 (R-1), replacing ≈ 226; so ≈ +1.4 k at LOD0. **0 new textures, 0 new materials** (`Prop_SteelAlmond` and `Prop_SteelBrown` are loaded by the Run! dress). Draws: frame and saddle 2 → 1. Sound: a steel DA pair (contract, §ZD). Photosafety: an almond leaf swinging in the armed white light changes the Tour's flash count. Doors 1 and 8 already sit at 3.0, so re-run `Tools/lookdev/flash_check.py` on the Tour. Phone tier: RN-F's leaf already reads dark on phones (Y .043 vs .25, HV.26), so check this pair's phone row.

Recorded, not proposed (outside HW-DA1 or weaker):
- **the Ward look** (RN-F at title scale: `Prop_WoodLaminate`, armour plates, F5). It matches the game's goal door, but from Level 0 it reads almost like today's walnut, and its armour plate changes HW-DA1;
- **armour plates** on the almond pair (S10 "where carts hit", S12 36 / 42 in): a spec change.

Fix while building: TitleVignettes' end lower-tone and cove strips follow the look's frame edge (1.2628 with the zone frame kit; §1, R-1 finding (a)). The services at 2.76-2.80 still clear the kit's head band (top 2.7228).

### R.4 Side doors (RN-K props): a frame built for the wall (later Run! polish)

- **Today:** `Kit_DoorFrame_Steel_DA` (2,030 / 724 / 276, ±0.0911 m deep = a 0.16 m wall + two 11.1 mm casings) is squeezed ×0.92 along the wall normal so its far casing hides inside the wall. The near casing ends up 10.2 mm proud instead of 11.1, and the section is 8 % thin. The leaf sits 2 mm proud of the wall face.
- **Proposed:** a one-face variant, `Kit_DoorFrame_Steel_DA_Wall`: the corridor casing at the true F4 section (50.8 × 11.1 mm), its jamb and head returning into the wall to the leaf's face plane, and no far casing.
  - Cut from `interact_door_frame_steel_da.py` with a faces switch, hand-built LOD0 / 1 / 2, the same slots (`Prop_SteelBrown`, `Prop_PlasticBlack`, `Prop_Chrome`).
  - ≈ 1,100 / 400 / 150 tris [E], about 900 fewer per door.
  - `RunDress.SideDoors` drops the squeeze. No break: these doors never open.
- **Honest flaw:** the prop leaf sits only about 9 mm behind the casing face. A real leaf centred in a 0.16 m wall sits about 69 mm behind it, so at grazing angles the side door reads shallow [E]. A true recess needs a 1.0 × 2.1 m pocket in the wall: ours in the title room's block slabs, a 关卡设计 contract in map Run! rooms. Only after Red walks SP7.

### R.5 Compartment pairs FD1 / FD2: closers and armatures (later Run! polish)

- **Why:** a held-open fire door closes by its closer when the holder lets go. US 3,204,154 (filed 1962, granted 1965, Z2): on breaking the magnet's circuit "the door is closed generally by a door closer". The 1992 spec: "Make labeled doors self-closing", with closers that have "rectangular, removable non-ferrous covers", "spray painted to match door hardware" (Z1 §2.6). The common holder in 1989 was a wall electromagnet with **an armature plate on the door's pull side** (US 4,852,919, Schlage, Z3). Our holders have no armature (`run_magnet_holder.py`), and the leaves touch the bare magnet face.
- **Proposed:**
  - one regular-arm closer per leaf, as on the stair door: `Kit_DoorCloser_Body` / `_Arm` / `_Forearm` / `_Shoe` on the pull (goal) face, posed at 115° by `FrontRoomsDoorCloserLinkage`;
  - an armature plate on each leaf's pull face at 1.95 m: a new tiny kit, a Ø 64 mm steel disc on a swivel stud, ≈ 300 / 80 / 12 tris [E];
  - the closer covers sprayed to match the hardware: aluminium-painted, a `Prop_Aluminium` swap [E].
- **Needs:**
  - anchors: `closer_shoe_l/_r` on `Kit_RunCompartmentFrame`, and a `closer_mount_s` on SteelRun_DA. No DA kit has closer anchors (a DA door never takes a surface closer); the single-acting kits do (`Kit_DoorLeaf_Steel*`, `_Veneer*`, `_Ward` carry `closer_mount`; `Kit_DoorFrame_Steel`, `_Steel_Alu` carry `closer_shoe`), but they have no F5 and no LOD2, so adding the anchors to SteelRun_DA and the compartment frame is the cheaper path;
  - **LOD2 for the four closer kits** (none is exported until "P-1"). This is shared with the stairwell item (inventory §4.3 item 6).
- **Cost:** 4 closers × 6,210 LOD0 / 4,041 LOD1 = 24,840 / 16,164 tris, plus LOD2 ≈ 4 × 250 [E], plus 4 armatures. 0 new textures. Never in today's game (rule 5b); scene 4 row S5-04 already lists closers.
- **Honest flaw:** SteelRun_DA is a DA leaf kit (bull-nosed edge, stileArcRadius 0.031) standing in for a single-acting rated pair. Invisible beyond about 2 m [E].

---

## ZD. Sound, photosafety, tiers (for the parent's contracts)

- **Sound (声音设计 paused; the contract goes to Red):**
  - Station D and the Waiting pairs keep walnut leaves, so StreamOpen / StreamClose (a large wooden door, 683823) stay right.
  - The title LOW and RUN! pairs become **steel** DA pairs: a hollow-metal leaf on DA spring hinges, no latch; it swings past centre and settles, with a thin metal tone [E]. The LOW pair's louvre also passes sound (50 §2.6).
  - The map back-of-house rows use DoorType Steel (exists, AUDIO_CONTRACT :145-146).
  - Pivot names stay "double door left / right hinge".
  - Contract text: `W/zone_doors/contracts/sound.md`, section R-3..R-5.
- **Photosafety:** re-run `flash_check.py` on the Tour after the zone leaves land (the almond Run! leaves swing brighter than walnut; doors 1 and 8 already sit at the 3.0 limit, HV.25), and once on Descent for the grey LOW pair and the night post.
- **Phone tier:** HW-DA1 plates start at LOD1 (kit :22). New parts carry LOD1 / LOD2. Check RN-F's dark-phone finding on the almond pair.
- **WebGL:** the title keeps Today on WebGL (TitleRooms.cs:28) and map doors stay the map's cubes (`DressOnWebGL = false`), so neither B-1 nor the title pairs show there. D on WebGL: the WebGL track decides.

## ZE. Open points and honest flaws (no ask unless the parent makes one)

1. D-3's guards are a narrow casing guard, not E1's angle (§S.6).
2. D-3 needs RoomStream's terminal pair as well as StationDress (§S.5).
3. The back-of-house free paint is new. It is a look choice inside Red's "漆面钢门". The REC grey (§B.4) must pass T6's 1-stop check in engine. If the parent wants an ask, a 3-swatch row (grey REC / putty / pale grey) fits on the B sheet.
4. Pre-existing for 信息设计: the plate sits 25 mm from D's casing edge (SIGNS asked 50-100 mm; written when D had no frame). Unchanged by this work.
5. `DoorCasingHalf` / `DoorCasingTop` mismatch: with the build, TitleVignettes' end trims follow each look's frame edge (§1, §S.7, §R.3).
6. door_hardware/10 calls the 630 hinge clause "the 1992 spec's default". Its page is a December 1994 revision (§Z1), so for hinges the claim is NEAR, not IN. The brushed-stainless kick plates, the 630 locksets and the closer clause are on Feb 1992 pages.
7. Not found: a dated (1985-93) photograph or spec of a transit or postal staff pair. Lead for a later pass: a 1985-93 Sweet's page on service and mailroom doors (no download asked).

---

# Part P · The pool and dark pool doors (R-2)

- **Stage:** Research R-2 (2026-11-05 21:2x-22:1x PST), `W/zone_doors/research_pool.md` (sha1 8b9ebe3d, 317 lines). Merged here by stage Design B on 2026-11-06 with the script `W/zone_doors/tools/merge_pool.py`: the text is R-2's, its own section numbers renumbered P0-P8 (references to other documents unchanged); its §9 SOURCES rows are N1-N32 in `SOURCES.md`.
- **Binding decisions it follows:** Red 21:15:15 "按默认做" (D-5 = research, three options + TODAY, the REC built, Red may still pick from the sheet); Red 21:25:37 "这里改成选项 2：整扇门都换成区域的门。…" (D-1 = the whole door: a title door that touches the Pool or Dark pool room is the pool door on both faces); 21:26:27 (D-2 = a new back-of-house map member in painted hollow metal, which PD-C would resemble).
- **Status:** research only. Main read only, no Unity, no clone change, no download (web pages read as text; one PDF the fetch tool cached by itself was not opened, journal 21:4x). The design is `20_design.md` Part B; the drawings are its sheets. **Since 2026-11-06 19:3x:** PD-A is built and packaged (`30_build.md` Part B, `W/apply/zone_double_doors_pool`), held off in the game by `PoolDoorsLive` (photosafety); the portholes' warm glow of Design B note 1 shows only after the door opens (a shut door's lites stay DARK: `30_build.md` §B14.2).
- **Marks:** **[E]** estimate or inference, not measured or sourced. **LATER** = a source after 1993, used only for how materials behave or for current practice, never as proof of a 1990 look. **[N7]** = a row of `SOURCES.md` (Part P). **[bath 10]**, **[dv 50]** etc. = our docs (§P1).
- **Design B notes on this research** (the text below is kept as R-2 wrote it; these six points are corrected or settled in `20_design.md` Part B):
  1. **The Dark pool read** (§P0 item 10, §P5.1 "Dark pool", §P5.4): the niches stand 5.7 m from either door, beyond their 4.5 m range (TitleVignettes `Pool()`), so only the water light reaches the door, and the end wall's field tile (albedo .93/.95/.96) takes the same light as the leaf. So the white FRP leaf is a hair darker than its wall, not "the palest thing in the room". It reads by its aqua frame (about 1 stop under the tile), its black gaps and plates, and above all by its two portholes, which glow warm once the next room (WAKE, a Level 0 room) is LIVE behind them. Today's walnut pair reads as a dark mass there (Part B §B5).
  2. **The open leaf** (§P0 item 8, §P2): the stream opens its leaves to 88° (`FrontRoomsRoomStream.ApplyDoorPose`), so the meeting edge reaches z 1.229 m: **0.33 m past the 0.9 m sill**, the leaf's bottom 48 mm and its kick plate 51 mm over the water. C11 holds (Part B §B6).
  3. **Draws** (§P5.1, §P5.4): the porthole ring is a kit with its own LODs, one draw per leaf, so PD-A is **+2 draws a pair**, not +0. Tris and memory per option: Part B §B9.
  4. **The frame envelope:** all three options keep Part A's zone frame envelope (outer edge 1.2628 m, head top 2.7228 m), so the pool band's ends and the trim stops follow Part A §A3.6 and nothing new is needed in TitleVignettes.
  5. **Plates:** the title level keeps the landed 630 plate maps (they already carry a low tide line). Tea stain on the plates belongs to a club preset for the map's PoolClub rows (the Map stage).
  6. **Tempered glass needs no shader work:** `FrontRoomsGlass` without its `_FR_GLASS_WIRE` keyword is a clear pane. Two new materials and a per-lite glazing plus a round sight test in `FrontRoomsDoorLite` (Part B §B3.5).

## P0. Short answer

1. **Wood was the wrong door for a pool.** Doors and frames in wet pool areas were aluminium, stainless, fibreglass (FRP) or galvanised steel under epoxy. A Navy design handbook of 1998 (it superseded a 1993 edition, not seen) asks for tempered glass and brass, bronze or stainless frames and hardware inside the pool hall. It allows steel only galvanised and epoxy-coated [N10]. Today the title Pool rooms show Level 0's walnut veneer pair on a dry tiled sill.
2. **FRP doors are the period's purpose-built answer to wet, corrosive rooms.** Pitt's moulded fibreglass door was filed in 1975 [N1], and Chem-Pruf was founded in 1974 by the same W. V. Pitt [N1, N4]. Warminster patented a one-piece moulded FRP door with a foam core in 1985-87 [N2]. Special-Lite put FRP skins on flush doors in 1981-83 [N5, N6]. Faces were through-coloured gel coat, smooth gloss in white, grey or camel tan or a custom chip, or "pebble grain". Frames were one-piece moulded FRP with no jointed corners [N3]. Honest limit: I found no period source naming an FRP door at a US pool. Pool use is documented from 2011 on [N15, N16, N22].
3. **Aluminium flush doors** are period-correct: Cline from 1961, Special-Lite from 1971 [N7, N8]. Aluminium works above water as door and window framing [N15]. It pits where it meets stainless or steel fasteners, and its corrosion product is a white powder that swells out of screw holes [N18, N21].
4. **Painted steel is the documented 1980s pool door, and the documented failure.** Northwestern's natatorium opened in 1987; a later repair replaced its existing steel doors and frames with stainless steel and added aluminium doors [N19]. Painted metal pool doors rust and peel to raw metal [N20], and paint on carbon steel does not last there [N15].
5. **Stainless:** type 304 and 316 do well in pools for doors, windows, ladders and rails that get washed [N12, N13]. The danger is stressed parts in high places nobody washes: the Uster ceiling collapse of 9 May 1985 (12 dead) came from cracked 304 hangers [N12, N14], and by 1990 304 and 316 had been reported cracking at room temperature in pool air [N11]. So HW-DA1's satin stainless plates are right for the pool door; specify type 316. Brown "tea stain" rust spots are the honest wear [N15, N28].
6. **Glass:** the pool door takes 1/4 in clear tempered glass [N10]. Wired glass, set A's choice, rusts along its cut wire edges where water sits [dv 10 §5, wear row]. The A1 16 in porthole (already researched [dv 50 §2.3]) fits the DA rule and ties the door to the pool level's own round windows and oculi [bath 18 M4, M5].
7. **Where a DA pair stands in a club:** between the lockers and the deck, or on the club's service corridor (towel carts). A DA leaf cannot latch, so it is never the pool's barrier, egress, fire or chlorine-room door [dh 10 §4; bath 10 row 25]. Modern UK pools and leisure centres use GRP double-action doors, singly or in pairs, with vision panels [N22, N23].
8. **The sill rule is real practice:** a dry, wide, drained deck at locker and shower room doors and a threshold at the shower room [N10, N25, N26]. Our 0.9 m dry tiled sill and "no leaf in water" (C11 / C12) stay. The open leaf reaches 0.3 m past the sill but passes 48 mm over the water.
9. **Three options + TODAY (§P5):** PD-A moulded FRP pair with the A1 porthole (**REC**), PD-B clear-anodised aluminium flush pair with a narrow tempered lite, PD-C epoxy-coated galvanised hollow metal in pool aqua with the porthole.
10. **Why PD-A:**
    - it is the lightest leaf, so it suits DA spring hinges better than walnut does [dh 10 §10.4, E];
    - it is seamless and white, like the pool level's "eerily pristine" tile [bath 15 §2];
    - its round window speaks the level's language;
    - it shows the pool from the Level 0 room before;
    - it is unlike every other zone's door. PD-C, by contrast, is Red's new back-of-house language (D-2).
    Flaws: the evidence for an FRP door at a pool is post-period; a white door in a white wall is low in contrast; and for the map it needs the most new kits (§P5.5, §P6).

## P1. Reused, not redone

| From | What this file takes |
|---|---|
| inventory `door_inventory/10_inventory.md` (001666d5) | A7 / B5 rows, §2 DO NOT REDO, §3.5, §3.7, §4 |
| kit `FrontRoomsDoubleDoorKit.cs` (b7ab5af0) header | The spec: 2.4 × 2.66 m opening; leaves 1.1955 × 2.639 × 0.0445 m; 3 DA spring hinges each (Bommer layout); 3 mm gaps; brush strips; leaf 6 mm over a 12 mm saddle; streamline casing (reveal 12 / face 38 / proud 14 mm, outer edge 1.250 m from the axis); HW-DA1; door space; render-only parts |
| `door_hardware/10` §4, §8, §10 (dh 10) | DA code limits (1979 UBC via Cal. Title 8 §3235, [dh S4]): lite ≥ 200 sq in, bottom ≤ 48 in, ≤ 100 occupants, never fire or panic. DA spring hinges for light leaves. A 47 × 104 in walnut leaf is past DA spring-hinge territory (§10.4). 630 wear (§8) |
| `door_hardware/20_build` §4-§5 | Plate kits 2,238/18/10 and 2,978/18/10. The pair's leaves with HW-DA1: 316 tris / 13 draws beyond the plates' LOD0 range, ≈ 21,000 tris / 29 draws with all 8 plates at LOD0 (frame parts not counted). Plate maps 12.6 MB |
| `door_vision/10` §1.3, §5 (dv 10) | CPSC 16 CFR 1201 (1977) and the wired-glass fire-door exemption. The 1992 San Diego spec: interior door lites tempered (Type D). Wired-glass wear: rust at the wire ends along the bottom bead where cleaning water sits (GGI) |
| `door_vision/50` §2.3, A1 (dv 50) | Porthole 16 in clear = 201 sq in, centre 55 in (Y 1.397), clear Y 1.194-1.600, cut-out Ø 425, ring outer Ø 444. A steel-leaf form; on wood a metal ring kit. +500-900 tris at LOD0 (24-32 segments). Needs a round sight test (§8 there) |
| `door_wood/40_for_red` T6 | On map doors a locked door must read ≥ 1 stop brighter than a free one; pale birch failed it |
| `bathroom_rooms/10` §1.2, §1.6, §2.2, §3.5 (bath 10) | The club fiction (tenant club on a B level, mid-1980s fit-out). A42 gives wet-side doors a vision panel; §1.6 also says wired glass fits a wet area, which §P3.5 here revises to tempered. Doors never stand in water. Pump room door, locked steel chlorine room (TM 5-662 8-5..8-7). Louvred lockers. Sealed and galvanised parts against chloramines |
| `bathroom_rooms/15`, `18` | Canon: white pristine tile, "senseless" joins, repetition. The level = the club's building going wrong (M1-M7): M4 gallery portholes, M5 1980s round windows / oculi / glass block, M6 the club programme repeated |
| `bathroom_rooms/50` C11, C12 | No door leaf on Ankle / Wade edges; validators: no leaf in water |
| `bathroom_rooms/70`, `71` | Tile T1. Field porcelain white .93/.95/.96. Grout grey-green .66/.70/.67. Course aqua .40/.76/.77. Pristine in the level, worn in the club (`_TileWear`). T9: white calcium line where splashed water dries. Red's tile pick DishDeep / DishTone 1.5 (landed 11-05 12:49) |
| `TitleVignettes.cs` (5133251e) `Pool()` | The rooms: 11.24 m between side faces, 12 m long, 2.9 m high; basin floor -0.15, ankle water -0.03; **dry sills 0.9 m deep in deck tile, top at 0, full width, at both doors**; tile everywhere; course band at floor + 1.20-1.50 (y 1.05-1.35), running "round the doors' casings" from DoorCasingHalf 1.27. Lamps: the slot's L03 at (.86, .96, 1), one in three dead. Dark: every overhead dead, four wet niches (.62, .92, 1) under the water; absorbScale 12; Refl_PoolLevel / Refl_PoolDead |
| R-1's journal finding (21:3x) | DoorCasingHalf 1.27 counts the lining twice, so the course band stops 20 mm short of the kit casing's outer edge (1.250) |

## P2. The room the door stands in (what the pool door must answer)

- **Both faces are now pool faces.** Its neighbour is a Level 0 room (lit yellow, carpet) or the next family room. From the Level 0 room the player sees it ahead at 12 m and up to the door: the preview (D-1). Its swing face: RoomStream swings both ways.
- **Surroundings:** porcelain-white glazed tile with grey-green joints, the aqua course at 1.05-1.35 m meeting the casing, abrasive white deck tile on the sill, dark teal ankle water from 0.9 m on, cool L03 light with dead lamps. In the Dark pool the only light is cyan from under the water (four niches and the water light), so the door is lit from below and from the side.
- **Floor at the door:** sill top 0 = Level 0 floor; water surface -0.03 starts 0.9 m into the room. The leaf bottom is 18 mm above the floor (6 mm over the 12 mm saddle). A leaf swung 90° reaches 1.1955 m into the room, 0.30 m past the sill, 48 mm above the water. It never touches water, so C11 holds.
- **What must keep working:** the DA structure and PlaceLeaf, pivots "double door left/right hinge", body doors, door culling, LIVE / DARK lites and PEEK, phone tier, WebGL keeps Today. The title pairs have no break, but the leaf design must also serve the map's future Pool member, which breaks (§P6).

## P3. Period research

### P3.1 Where a double-acting pair stands in a 1985-1993 pool or club

| Place in a club or natatorium | Door there c. 1990 | A DA pair? |
|---|---|---|
| Lockers / showers ↔ deck | A door or a screened opening [E]; codes speak of "locker or shower room doors" with a wide deck at them [N25]; the shower room has a threshold [N10]; the locker room must not be seen from the deck [N24, LATER] | **Yes, the best fit.** Wet traffic, hands full, pushed by the body. ≤ 100 occupants per door is plausible: locker rooms 50 sq ft per person [N30, LATER]. A vision panel is required (dh 10 §4). Privacy stays because the lite is small (no full glass) |
| Club corridor / towel desk ↔ service (laundry, towel carts) | Traffic door with armour or kick plates | **Yes** (the service DA door of dv 50 :160) |
| Pool hall ↔ lobby / viewing corridor | Aluminium entrance pair with tempered glass; weatherstripped, frames sealed, because the hall runs humid at negative pressure [N10] | No when it is an exit serving > 100 or needs panic hardware (dh 10 §4; deck 15 sq ft per person [N30]) |
| Pool hall egress, fire stair | Single-acting, rated, closer, exit device | **No** (the stairwell A door language; PL_FireStair's south door belongs there, inventory B5) |
| Pump room, chlorine room | Locked steel, signed; chlorine in a fire-resistant room with two exits [bath 10 rows 24-25] | **No** |
| Maintenance | A 6 ft doorway at grade, a roll-up door for equipment [N10] | No |

Modern counterpart, LATER: UK makers sell GRP **double-action** doors, single or in pairs, with a vision panel in each leaf, for swimming pools, gyms and other wet areas [N22, N23]. **So the title Pool pair reads as the club's lockers ↔ deck pair, repeated in the level (M6, the club programme repeated).**

### P3.2 Leaf materials

| Material | Period evidence (≤ 1993) | Pool evidence | How it fits our DA spec | Verdict |
|---|---|---|---|---|
| **FRP / fibreglass, moulded** | Pitt "atmospheric resistant door": a one-piece moulded FRP U-collar (stiles and rails) and two FRP plates, foam inside, polyester gel coat, the cut edges spray-coated "seamless", an optional lite on bedding held by fibreglass glazing pins. Filed 16 Oct 1975 (US 4,068,431), continued to US 4,429,498 (1984). Named uses: corrosive chemicals, high humidity, meat plants, hospitals, labs [N1]. Chem-Pruf, founded 1974 by W. V. Pitt [N4]. Warminster US 4,676,041 (filed 1985, granted 1987): one-piece RTM door, closed-cell PU core, pigmented polyester skin, a moulded narrow window and hinge recesses, hinge metal stainless, bronze or aluminium as the atmosphere demands [N2]. A 1996-98 master spec still describes the same product: 1-3/4 in, gel coat through-coloured, smooth gloss, white / gray / camel tan or a custom chip, one-piece moulded stile and rail, PU foam (≥ 4 lb/ft³) or phenolic honeycomb core, **frames solid FRP "with no jointed corners"**, stainless screws, glazing on fibreglass pins and silicone, 10-year corrosion warranty, "minimum twenty (20) years documented experience" [N3]. Special-Lite: FRP door skins from the early 1980s, the SL-17 "pebble grain" (0.120 in FRP on aluminium stiles and rails, PU core) designed in 1983 for schools [N5, N6]. FIB-R-DOR trademark filed 1989 [N9] | Fibreglass doors "appropriate for most pool area applications" (2011) [N15]; doors and frames in wet areas incl. locker rooms "aluminum, stainless steel, or fiberglass" (2025) [N16]; GRP double-action pool doors [N22]. All LATER | 1-3/4 in ✓. With aluminium, the lightest leaf: about 120 lb for our 47 × 104 in leaf [E], against 150-170 lb for the walnut leaf (dh 10 §10.4, E). A listing rates three 6 in DA spring hinges at 107 lb (finish unclear) [N29, LATER], so FRP is the closest to what the hinges were made for. No grain, no seams | **PD-A** |
| **Aluminium flush** | Cline: aluminium flush doors since 1961, Series 100BE its first product, later an FRP-skinned 200BE [N8]. Special-Lite began 1971 as an aluminium door and frame maker; SL-16 aluminium flush door [N7] | Aluminium "perform[s] quite well above water as door and window framing" (2011) [N15]; Northwestern's renovation put in aluminium doors [N19]. LATER | 1-3/4 in ✓; light (about 95 lb [E]). Real aluminium entrances usually hang on pivots and floor closers, so DA spring hinges are "the closest look the DA structure allows" | **PD-B** |
| **Aluminium stile-and-rail, large glass** | The period entrance door (dv 50 :112: "a 1990 Class-A lobby would use clear tempered glass in aluminium stiles") | Natatorium lobby and viewing doors | Full glass breaks the locker-room privacy logic [N24]. It cannot be the map's breakable leaf (dv 50: the break window removes half and full glass) | Rejected (noted on the sheet only if wanted) |
| **Galvanised hollow metal under epoxy** | The Navy rule (1998 edition; the 1993 one not seen): steel only galvanised and coated with high-performance epoxy [N10]. Galvannealed A40 / A60 coatings; a university spec asks A60 for toilets, lockers, showers and other humid rooms [N32, LATER]. Northwestern 1987: steel doors and frames [N19, E: the originals] | Rust, peeling paint, raw metal (an aquatic centre, 2024) [N20]; paint on carbon steel "typically a short-lived protection"; "a thin zinc coating is not adequate" [N15] | 1-3/4 in ✓; heavy (≈ 150-190 lb, 18-16 ga [E]). The game already has this leaf: dormant `Kit_DoorLeaf_Steel_PaintedMetal_DA` 5,536/1,724/212. **The same family as Red's new back-of-house door (D-2)** | **PD-C** |
| **Stainless-clad** | Kitchen and hospital DA doors | Northwestern used stainless for the frames [N19] | Reads as a kitchen door (dv 50: the porthole and stainless are food-service signs). Costly | Rejected for the leaf; stainless kept for hardware, saddle and an optional frame |
| **Plastic laminate on a wood core** | Common club and school interior door; 1980s laminate trims in the club's postmodern fit-out [bath 50 PL_PoMo] | Jointed and wood-core doors in humid air delaminate and crack [N2]; GRP is said to be "far more hygienic" than "timber, laminate or steel doors which are still sometimes used" [N22, LATER] | ✓ dims, but the wood core swells at the bottom edge | Rejected (a club accent at most) |
| **Solid plastic (HDPE)** | HDPE toilet partitions from 1978 (Santana), Capitol 1983, Comtec 1989 [N27] | Partitions in wet rooms | Partition panels are about 1 in, not a 1-3/4 in swing leaf; no period HDPE swing door found | Rejected for the pair (the club's shower and toilet partitions may be HDPE) |
| **Wood veneer (TODAY)** | The office door | Fails: delamination, swelling, mildew in humid air [N2, N22, E] | Too heavy for DA spring hinges (dh 10 §10.4) | TODAY only |

### P3.3 Frames and casing

- **FRP:** solid moulded FRP frames, one piece, **no mitre joints**, the head corners moulded round, any gel-coat colour [N3].
- **Aluminium:** extruded and anodised, square sections, mitred corners with a hairline joint and visible fasteners. "Above water" is fine [N15].
- **Stainless:** the Navy rule lists stainless frames [N10]; Northwestern's repair replaced its steel doors and frames with stainless steel [N19].
- **Galvanised pressed steel:** epoxy-painted, grouted solid. The feet rust first, where mop water sits [E, N20].
- **Ours:** the F4 steel face (50.8 × 11.1 mm) is built (door_frame 30). The kit casing is 38 mm wide, its outer edge at 1.250 m; the course band ends at 1.27 m (20 mm short, §P1). A pool casing should end where the band starts, or the band should start at the casing. That is a fix to make in TitleVignettes (ours).
- **Sealing:** doors that separate the hall from the rest of the building were weatherstripped and their frames sealed against moisture [N10]. The kit's black brush strips are that weatherstrip. Keep them.

### P3.4 Hardware

- **Stainless grade.**
  - What happened: on 9 May 1985 the suspended ceiling of the Uster indoor pool fell, killing 12 [N14]. Molybdenum-free type 304 hangers had cracked by chloride stress corrosion in the warm air above the water [N12, N14].
  - What was learned: by 1990 Oldfield and Todd had reported type 304 and 316 parts cracking at room temperature in indoor pool air [N11].
  - The rule since: the danger is stressed parts in places nobody washes. 304 and 316 are fine for **doors, windows, ladders, rails and benches** that are washed; overhead load-bearing parts need 6 % Mo grades [N12, N13].
  - So: HW-DA1's satin stainless plates (BHMA 630) and stainless hinges suit the pool. A pool hardware schedule would write **630, type 316** [E].
- **Bronze and brass** resist pool air well [N10, N15]. Copper alloys turn green in chlorine air [N15]; chrome plating pits [E].
- **DA spring hinges.** Our structure needs three per leaf. Stainless DA spring hinges are sold today, and a listing rates three 6 in hinges at 107 lb (finish unclear) [N29, LATER]. One reseller says Bommer made none in stainless [N29], and period stainless DA spring hinges are **unverified**. So: satin stainless barrels (type 316) on the pool door, with bronze as the honest alternative.
  - The springs inside are carbon spring steel [E]. On a pool door their grease weep (dh 10 §8) turns rust-brown at the barrel ends [E].
- **Plates (HW-DA1, kept):**
  - push plates 4 × 16 in at 45 in;
  - 10 in kick plates;
  - no latch, pull, bolt or closer.
  The pool wear goes on top of the 630 wear of dh 10 §8:
  - a whiter, chalky tide line in the bottom 20-40 mm (calcium left where splashed or mop water dries; bath 70 T9) [E];
  - brown tea-stain specks at screw heads and the kick plate's bottom edge [N15, N28].
- **Saddle:** the 12 mm saddle is the floor's border piece where Level 0 carpet meets pool tile ("a finish stops on something built", bath 10 §1.6). A stainless or aluminium saddle is what the period's metal thresholds were [E]. The oak saddle goes with the walnut.
- **Never on this door:** a latch or lock (a DA leaf cannot latch), panic hardware, a closer arm, an electrified anything, a lever. The pool barrier and the hall's egress doors are other doors (§P3.1).

### P3.5 Lite and glass

- **Rule:** one lite per leaf, ≥ 200 sq in, bottom ≤ 48 in (dh 10 §4, dv 50).
- **Glass:** **1/4 in clear tempered**:
  - the Navy handbook asks tempered glass in the pool hall [N10];
  - the 1992 San Diego spec already used tempered for interior door lites (dv 10 §1.3);
  - wired glass rusts at its wire ends where water sits (dv 10 §5, GGI).
  Wired glass stays set A's choice everywhere else; the pool door is the one place where tempered is the period answer.
- **Form, the A1 porthole:** 16 in clear, 201 sq in, centre 55 in, clear bottom 47 in [dv 50 §2.3]. It is already researched; build it, do not research it again. dv 50 reads it as the kitchen DA door; in our pool family round windows are the level's own vocabulary: the gallery's portholes into lit water (M4) and the club's 1980s oculi (M5) [bath 18].
  - On FRP the ring is moulded with the leaf, glazed on fibreglass pins and silicone [N1, N3].
  - On steel it is dv 50's pressed-steel ring; on aluminium a metal ring kit.
  - On the 1.1955 m title leaf: centred like F4b (Z 0.598) or at A1's offset (Design B). The push plate stays clear of it by about 0.23 m [E].
- **Alternatives:** F1 10 × 30 in (300 sq in) or F4b 10 × 48 in, rectangular in a metal or FRP stop. These are the rectangular lites of FRP and aluminium doors [N2].
- **Silhouettes per zone:** the porthole is the one form no other zone uses (L0 F4b tall, Office F1, Run! F5 square, back-of-house A2 louvre, EXIT F13). The preview reads as "pool" from the Level 0 room.
- **Map leaf breaks:** tempered glass dices; it does not crack and hold as wired glass does (dv 10 §9.3). The map's Pool member needs its own lite stage in the break, closer to the windows' shatter look [E].

### P3.6 How they aged (the wear to model)

| Material | Wear, where | Evidence |
|---|---|---|
| FRP gel coat | Gloss loss and chalky dull ovals on push and hand zones. A faint yellowing of white gel coat low on the leaf. Glass-mat print-through visible at grazing light. Grime in a pebble grain. Fibres showing through the gel coat ("fibre bloom", FRP's weathering mode outdoors) only after long years indoors. No rust | [E]; gel coat moulded through-colour, does not peel [N31, LATER] |
| FRP edges | A faint trimmed seam line round the leaf edge (Pitt's two peripheral seams, spray-coated) | [N1] |
| Aluminium | White powdery pitting bloom where water and chlorine sit: the bottom rail, round hardware cut-outs and screws. Corrosion product swells out of holes. Milky patches on the anodising. Galvanic halos round stainless or steel fasteners | [N18, N21]; [E] for the milky anodising |
| Galvanised + epoxy steel | Rust blisters lifting the paint at the frame feet and the leaf's bottom 50-150 mm. Brown weeping from screws and hinges. Peeled paint to raw metal at kick zones. White zinc rust where scraped | [N20, N15]; [E] for the heights |
| Stainless 304 / 316 | Brown tea-stain spots (cosmetic) on plates, hinges and screws | [N15, N28, N13] |
| Bronze / brass, chrome plate | Green at edges and screws; chrome pits | [N15]; [E] |
| Wired glass | A rust bloom along the bottom bead line | dv 10 §5 (GGI) |
| All, at the floor | A white calcium tide line where splashed or mop water dries (pool water is hard and chlorinated) | bath 70 T9, [E] |
| Wood veneer (TODAY) | Swelling and delamination at the bottom rail, black mildew at the lower edge | [N2] (delamination of jointed doors in humid air); [E] |

**Gradient (bath 15 §0 item 7, bath 70 `_TileWear`):** the club is worn and the level is pristine. So the title Pool and Dark pool doors (level rooms) take the material's faintest wear: hand-dull ovals, a thin tide line. A "club" wear preset goes on the map's PoolClub rows. The Dark pool door keeps the level's wear; its look changes by its light (§P5).

### P3.7 The sill and the floor at the door

- Real pools keep a wide, dry, drained deck at locker and shower room doors: at least 12 ft wide at those doors for large basins in today's Wisconsin code [N25, LATER].
- Decks slope ¼-½ in per foot away from the pool to deck drains [N26, LATER; bath 10 row 2, TM 5-662].
- The Navy handbook puts a drain on every ≤ 150 sq ft of the locker and shower passage and a threshold between the shower room and what lies next to it [N10].
- So our rule (C11 / C12: no door leaf in water; dry tiled sills in front of every door) is the real one, shortened for the 12 m title room. **Keep the 0.9 m sill.** A slot drain across the sill's wet edge would be an honest detail for the room (TitleVignettes, ours), not part of the door [E].

### P3.8 Do not show on a 1990 pool door

Electrified or sensor hardware, ADA lever trim on a DA leaf, LED or illuminated signage, a magnetic hold-open, a pool-alarm contact, an HDPE swing leaf, a stainless DA leaf with a kitchen "bumper" rail, modern composite doors with printed wood grain [§P3.2, E].

## P4. What the spec fixes, and what the pool door may change

| Fixed (Red 19:17 "规格和结构上的要求和之前一样") | Free for the pool door (D-1 whole door) |
|---|---|
| Opening 2.4 × 2.66 m; leaves 1.1955 × 2.639 × 0.0445 m; three DA spring hinges per leaf in the Bommer layout; 3 mm gaps; brush strips; 6 mm over a 12 mm saddle; PlaceLeaf; HW-DA1 plate kits, sizes and heights; no latch, bolt or pull; door space; render-only; pivots and their names; colliders | Leaf face material and edges; lite form and glass (≥ 200 sq in, bottom ≤ 48 in); casing profile and material (keep the 50 mm envelope and the reveal that clears the barrels); saddle material (same section); hinge barrel finish; plate finish variant (630 in type 316, pool wear); wear maps |

- **Map Pool member.** The same leaf must work as a single 1.0 × 2.1 m DA leaf: d16 rows PL-F / PL-K, LOD0/1/2, a break family and its palettes, and the museum stand's member count (with D-2's back-of-house member: two new members).
- **T6 on map rows.** A white free leaf cannot have a locked leaf 1 stop brighter, as birch failed. So PL-F takes a mid tone (grey gel coat, or the course aqua) and PL-K the white, or T6 is re-measured in a pool cell [E]. Not in play while `doorsNeedKeys` is 0.
- **Photosafety.** A pale glossy leaf swinging in front of the yellow Level 0 room changes the title's flash count. Doors 1 and 8 sit at 3.0, the limit. Re-run `Tools/lookdev/flash_check.py` on the Tour (inventory §3.7).
- **Sound.** Today the title pair plays StreamOpen / StreamClose, a large wooden door (683823). Under D-1 the pool door's sound follows its material: a contract for Red (`W/zone_doors/contracts/sound.md`, pool section).
- **WebGL** keeps the Today title. **Phone tier:** textures at the 1024 cap; the porthole ring at LOD1 from the start (as the plates).

## P5. Options for the title Pool and Dark pool pairs

All three keep the spec of §P4 and the dry 0.9 m sill. Labels **PD-** (pool door), so they do not collide with the pool lamps P1-A / P2-A. Costs are per pair, added to today's pair (its leaves with HW-DA1: 316 tris / 13 draws beyond the plates' LOD0 range, ≈ 21,000 tris / 29 draws with all 8 plates at LOD0; frame parts not counted there; plate maps 12.6 MB, kept). Tris are **[E]** until Build measures them. Textures: BC7 with mips, desktop; phones capped at 1024.

### P5.0 TODAY: the walnut pair on a dry sill

Walnut G veneer leaves, walnut streamline casing, oak saddle, F4b 10 × 48 in wired lites in walnut beads, HW-DA1 in 630, hinge barrels a flat "brushed steel". On the deck-tile sill, with the course band stopping 20 mm short of the casing.
- **Feel:** an office door carried into a pool. Warm, dark, grained wood in a cold white tiled room; it reads "Level 0 goes on" rather than "you are entering the pool". Honest for the Poolrooms' wrongness, wrong for the period (wood in a wet room).

### P5.1 PD-A (REC): moulded FRP pair with the porthole

- **Leaf:** one-piece moulded FRP look (Pitt / Chem-Pruf type). Smooth white gel coat, about .90/.90/.88, a hair warmer and glossier than the porcelain field tile, so it reads as a different, man-made surface [E]. Light orange peel, faint glass-mat print-through at grazing light, edges moulded round (r ≈ 3 mm) with the faint trimmed seam.
- **Lite:** the A1 16 in porthole, moulded ring, fibreglass glazing pins and a silicone bead, **1/4 in clear tempered**.
- **Frame:** a one-piece moulded FRP casing with round head corners and no mitres [N3], the reveal kept for the barrels, outer edge meeting the course band. Default colour the course aqua .40/.76/.77 (a custom gel-coat chip, which the period offered [N3]). Sheet variants: all white; period grey.
- **Hardware:** HW-DA1 in 630 type 316, pool wear (§P3.4); DA spring hinge barrels in 316 satin stainless with a faint rust weep at the barrel ends; stainless saddle.
- **Wear (level):** hand-dull ovals at the push plates, a thin white tide line on the bottom 25 mm, no rust. A club preset for the map's PoolClub rows: chalked push zones, tide line to 40 mm, yellowed kick zone, grime in the joints.
- **Dark pool:** the same door. The white face is the palest thing in the room, lit cyan from below by the niches and the water light, the porthole a dark disc until the next room is revealed. The goal stays readable in the dark.
- **Feel (lit Pool):** from the Level 0 room, a pale door with a round window in an aqua frame, the band of the pool wall running into it: you know a pool is next before the door opens. Up close the door is too clean: no grain, no screws, no seams, like the tile. Through the porthole, the next tiled room, like the gallery's windows into the water.
- **Cost per pair [E]:**
  - tris: leaves 2 × (≈ 900 / 300 / 60), casing ≈ 1,200 / 450 / 120, so **≈ +3,000 / 1,050 / 240** (LOD0 / 1 / 2);
  - textures: gel coat tileable 1024 A/N/S **4.0 MiB** + pair wear atlas 1024 × 2048 RGBA **2.7 MiB** + plate pool overlay 512² **0.3 MiB** = **≈ 7 MiB**;
  - draws: **+0** (FRP replaces walnut on the leaves and casing, stainless the oak saddle, tempered glass the wired pane).
- **Map Pool member:**
  - new leaf kit (FRP, porthole) ≈ 3,600 / 1,400 / 200 (Veneer_DA's 2,944 / 1,228 / 188 plus the porthole);
  - new frame kit (moulded FRP) ≈ 2,000 / 720 / 270;
  - **a new FRP break family**: gel-coat shell cracks and splits, PU foam core, collar; palettes gel coat / foam / collar; the tempered porthole dices.
  The most new kit work of the three.
- **Flaws:**
  1. No period source shows an FRP DA pair at a US pool; FRP doors were sold for chemical and food plants, labs and schools. It is a plausible premium choice for a developer's 1980s club, not a documented norm.
  2. A white leaf in a white wall is low contrast. It reads by the aqua frame, the black brush gaps, the plates and the porthole; check at 12 m and on the phone tier.
  3. Smooth gel coat risks reading as CG plastic. It needs the print-through, orange peel, edge seam and wear maps to hold at 0.35 m (Red 11-04: tile "太平整…看起来特别的假").

### P5.2 PD-B: clear-anodised aluminium flush pair

- **Leaf:** clear anodised aluminium face sheets on an extruded stile and rail (Cline / Special-Lite type). Satin, faint roll direction; a visible stile cap and hairline seam on each edge.
- **Lite:** a narrow F1 10 × 30 in lite (300 sq in, bottom 45 in) in snap-in aluminium glazing stops, mitred, 1/4 in clear tempered.
- **Frame:** an extruded aluminium casing, square, mitred head corners with a hairline joint, visible stainless fastener heads.
- **Hardware:** HW-DA1 630 type 316; 316 hinges; aluminium saddle.
- **Wear (level):** a faint milky bloom low on the leaf. Club preset: white powdery pitting along the bottom rail and round every screw, the corrosion product swelling out of the holes [N18].
- **Feel:** a 1980s club or mall entrance carried inside. A cool metal door that mirrors the water's cyan light and the dead lamps in long soft streaks. Modern and hard, less strange than PD-A. In the Dark pool it goes nearly black except for the niche reflections, so the goal reads less well.
- **Cost per pair [E]:**
  - tris: leaves 2 × (≈ 500 / 180 / 40), casing with fasteners ≈ 1,300 / 400 / 100, so **≈ +2,300 / 760 / 180**;
  - textures: anodised tileable 1024 A/N/S **4.0 MiB** + wear atlas A/N 1024 × 2048 **5.3 MiB** + plate overlay **0.3 MiB** = **≈ 9.6 MiB**;
  - draws: **+1** (fastener heads).
- **Map Pool member:** new aluminium leaf kit ≈ 3,200 / 1,300 / 200; **frame: reuse the dormant `Kit_DoorFrame_Steel_Alu_DA` (2,030 / 724 / 276)**; break: the Steel family with an aluminium palette (dents, no crack).
- **Flaws:** real aluminium entrances hang on pivots, not DA spring hinges (the closest look the DA structure allows). The rectangular lite loses the round-window tie. Galvanic pitting is the strongest wear, so it suits the club more than the pristine level.

### P5.3 PD-C: epoxy-coated galvanised hollow metal in pool aqua

- **Leaf:** 16 ga galvannealed hollow metal, high-build epoxy enamel in the course aqua .40/.76/.77 (or pool white), slight orange peel, the hemmed edge seam.
- **Lite:** the A1 porthole in dv 50's pressed-steel ring, 1/4 in clear tempered.
- **Frame:** the F4 pressed-steel frame face (50.8 × 11.1 mm), the same epoxy.
- **Hardware:** HW-DA1 630; bronze or stainless hinges; stainless saddle.
- **Wear (level):** a thin tide line; paint dull at the push zones. Club preset: rust blisters at the frame feet and the leaf's bottom 50-150 mm, brown weeping from hinge screws, raw metal at the kick plate's ends.
- **Feel:** a municipal or school pool. A heavy painted door the colour of the pool band, a ship's porthole in it. The most "the water has been winning for years" door of the three, and the most institutional.
- **Cost per pair [E]:**
  - tris: leaves 2 × (≈ 900 / 300 / 60), F4 casing ≈ 2,000 / 720 / 270, so **≈ +3,800 / 1,320 / 390**;
  - textures: epoxy tileable 1024 A/N/S **4.0 MiB** + rust/blister atlas A/N/S 1024 × 2048 **8.0 MiB** + plate overlay **0.3 MiB** = **≈ 12.3 MiB**;
  - draws: **+0**.
- **Map Pool member:** **the cheapest.** Reuse `Kit_DoorLeaf_Steel_PaintedMetal_DA` (5,536 / 1,724 / 212, F1), or a porthole variant of it (≈ 5,800 / 1,800 / 220); `Kit_DoorFrame_Steel_DA` (2,030 / 724 / 276); the Steel break family with a pool palette.
- **Flaws:**
  1. **The same door family as Red's new back-of-house door (D-2: painted steel, kick plate, louvre).** Only the colour and the lite would tell them apart.
  2. Rust contradicts the pristine level; it belongs to the club.
  3. Steel in a pool is exactly what the period guidance tried to avoid unless galvanised and epoxy-coated.

### P5.4 Summary table

| | TODAY | PD-A FRP + porthole (REC) | PD-B anodised aluminium | PD-C epoxy galvanised steel |
|---|---|---|---|---|
| Period evidence | office door | patents 1975-87, makers 1974 / 1981-83; pool use only LATER | flush doors 1961 / 1971; framing OK above water | the 1993 Navy rule; a 1987 natatorium's steel doors |
| Reads from the Level 0 room | more Level 0 | pool (round window, aqua frame) | entrance / metal | pool colour, institutional |
| Lit Pool / Dark pool read | dark shape / dark shape | pale door / palest thing, cyan-lit | cyan streaks / near black | aqua / dark teal |
| Distinct from other families | no (Level 0) | yes | yes (clear anodised, not the Office / station dark bronze) | **no: back-of-house (D-2)** |
| Tris per pair LOD0 / 1 / 2 [E] | 0 | +3,000 / 1,050 / 240 | +2,300 / 760 / 180 | +3,800 / 1,320 / 390 |
| Textures | 0 | ≈ 7 MiB | ≈ 9.6 MiB | ≈ 12.3 MiB |
| Draws per pair (leaves, plates beyond / at LOD0) | 13 / 29 | +0 | +1 | +0 |
| Map member work | none | new leaf, frame, break family | new leaf; frame and break reused | reuse leaf, frame, break |

### P5.5 Recommendation: PD-A

PD-A is the period's purpose-made door for wet, corrosive rooms [N1-N6] and today's standard pool door [N15, N16, N22]. Reasons:
- it is the lightest leaf on our DA spring hinges [E];
- its seamless white face matches the level's pristine tile, and its porthole matches the level's round windows;
- it lets the Level 0 room preview the pool;
- it is the palest, most readable goal in the Dark pool;
- it overlaps no other family.

Its flaws (§P5.1) are a post-period pool source, a low-contrast white-on-white face, and the most new map kits. Build PD-A as the default; Red may pick PD-B or PD-C from the sheet (D-5).

## P6. Notes for Design B, Build and the Map stage

1. **Sheet (VISUAL_STANDARD §2.2):**
   - columns TODAY | PD-A REC | PD-B | PD-C;
   - rows: from the Level 0 room at 1.62 m (the preview, 12 m), phone tier, 3/4 at 3 m inside the Pool room, hero 0.35 m (porthole ring, plate, barrel, gel coat), the Dark pool, the open leaf over the water, COST;
   - PD-A sub-variants (frame white / grey / aqua; pebble grain) only if Design B wants them on the sheet.
2. **Kit zone look:** leaf face material and edge radius; lite form (round, with the A1 numbers); glass (tempered); casing profile per zone; saddle material; hinge Surface; plate variant material; wear atlas; LOD1 / LOD2 on every zone part. One zone look for the whole door (21:28).
3. **TitleVignettes (ours):** the course band ends at the new casing edge, not at 1.27 m. The sill stays 0.9 m.
4. **GlassRT and lites:** the tempered porthole needs a clear-glass material in the lite system (LIVE / DARK, PEEK), and a round sight test (dv 50 §8). Check what `FrontRoomsDoorLite` expects of the pane.
5. **Map stage:** the Pool member after Exit (and D-2's back-of-house member): rows PL-F / PL-K, a palette in the break family, T6 in a pool cell, the museum stand count → 交互设计. The map's module edges (PL_FireStair's south fire door) stay 关卡设计's; that door belongs to the stairwell A language, not this pair.
6. **Sound (contract for Red; 声音设计 paused):** `W/zone_doors/contracts/sound.md`, pool section.
7. **Photosafety:** flash_check on the Tour after the build.

## P7. Media for Red (nothing downloaded)

Pages with photos that would help a look round, if Red allows a download batch. Sizes were not checked (no HEAD request made).
- iDigHardware 2019, the corroded aluminium pool door [N18];
- BD+C 2010, photo 6, corrosion on stainless door hardware [N17];
- Special-Lite SL-17 pebble grain product images [N6];
- Dortek leisure GRP double-action doors brochure [N22].

## P8. Limits

- **No period photograph of a 1985-93 US natatorium door was found or opened.** The FRP evidence is patents and maker histories; the pool evidence is post-1993.
- The 1985 *Planning Facilities for Athletics, Physical Education and Recreation* (ERIC ED252951, 278 pp.) has a swimming-pool chapter, but its PDF is over the fetch limit and was not read.
- The UBC 1988 / 1991 text on occupant loads and safety glazing was not found online; the IBC values used are marked LATER.
