# Level transitions · production · 10 · Plan

Date: 2026-10-22 13:2x PDT. Stage "Plan" of the workflow `level-transitions-prod` (wf_81041a8c-86f), run by the 游戏视觉 session (local_a9a1dce2, account B).
Nothing is built yet. Unity did not run. No clone was made in this stage. Nothing was downloaded. The only file written is this one.

**Decided by Red, 2026-10-21, "全部按推荐" in the 游戏视觉 chat** (top of `../40_for_red.md`):
- a Frame on every zone border: a casing, portal, steel frame or base. The Office is the newer fit-out and owns every frame and base;
- Renovation on about 3 in 10 crossings;
- the colour stays today's V5 lead (in main since 10-03, with B0.1 corner ownership and B0.2 side-specific grime height).

The LT16 defaults hold with it (they were "defaults that hold unless you say otherwise"): only Neck would narrow crossings; free posts get a collider; the base is on Office walls only; rougher borders at higher tiers; fix the corner flicker in the map first; the torn paper scraps use today's Level 0 print.

**Not decided:** Close (the map rule that closes Office | Level 0 borders), the light rule (soft or dark border cells), the ~60 MB media download. Nothing here depends on them, and nothing here pre-empts them (§12).

---

## 0. Short answer

- **Build two visual kits on top of today's game.** Frame goes on every Office | Level 0 border. Renovation goes on 30 % of border crossings, drawn per crossing from the run seed. V5 stays as it runs. Both kits merge into main's own `FrontRoomsTransitionKit.cs` (GUID `b215858f…`) as partial-class files. They are never copied over it.
- **The map's part is small and data-driven.** About +115 / −10 lines in `FrontRoomsMapWorld.cs`, against +255 / −11 in the 10-08 draft. The map:
  - decides which crossing is renovated (`BorderRenovated`, a seeded hash with the share read from the level profile);
  - hands the kit a read-only view and its own computed openings;
  - keeps collision byte-identical, except for one collider box per free post (a decided default).
- **A12 makes the frame simpler.** A double-acting door has no stop and no push side. The frame becomes symmetric, and nothing reads `door.swing`. The proxy is built to D1.6's `Kit_DoorFrame_Steel_DA` envelope, and it gives way to that kit once D1.6 lands. Renovation clutter stays out of the leaf's sweep on both faces.
- **Three changes from the 10-08 pick.**
  1. The renovation kit no longer rebuilds every wall piece through its own corner rule. That rule caused much of the ×2.2 triangles. Posts belong to the map's coming full post-seam fix (codex C3).
  2. A dressed open edge keeps its portal in an unfinished state (primer and galvanised beads), so that "a Frame on every border" holds there too.
  3. The paper scraps use today's Level 0 print.
- **Budgets.** On desktop, Frame alone adds ≤ +45 k triangles within 46 m. Frame plus renovation adds ≤ +110 k (the pick measured +169 k), renderers ≤ +8 % and 94 MB of textures. The WebGL and handheld tier stays at ≤ 34 MB and about +1 % of draws.
- **It lands after the map's own queue.** That queue is M2 → A12 → glass → sound → C22, then the walkable stair. The transition hunks go with or after codex C3. They are written now against today's main plus A12 r2, and get a 3-way merge onto main after C22.

---

## 1. Bases and what moved since the drafts

### 1.1 Today's main (read 13:1x–13:2x)

HEAD **`2c450e117c84da7c3ac1cb29c9c130a95701e748`** (12:55). `git status` shows 24 uncommitted paths, all docs, input or placard files. No map or visual runtime file is dirty. The last `LANDINGS.md` lines (13:06) move no file this plan touches.

| File | sha1 (shasum) | Owner | Note |
|---|---|---|---|
| `Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs` | **7ef0ca45** | map | outlets C-1 landed 06:54; = A12 r2's base |
| `Assets/Scripts/FrontRoomsMap/FrontRoomsMap.cs` | 66d7c959 | map | **unchanged since 22bb75f**, so `Resolve` and the layout are the pick's |
| `Assets/Scripts/FrontRoomsMap/FrontRoomsModuleUnits.cs` | f374d320 | map | unchanged since 22bb75f |
| `Assets/Scripts/FrontRoomsMap/FrontRoomsLevelProfile.cs` | d9e17bd0 | map | |
| `Assets/Levels/FrontRoomsLevel0.asset` | 07514c33 | map | |
| `Assets/Scripts/FrontRoomsMap/FrontRoomsMapHunter.cs` | fb2fe3dd | map | not touched by this plan |
| `Assets/Scripts/FrontRooms3DGame.cs` | 5acc5147 | map | not touched by this plan |
| `Assets/Editor/FrontRoomsMap/FrontRoomsMapInteractionTests.cs` | d9b51f23 | map | |
| `Documentation/MAP_GENERATION.md` | 47cc5b54 | map | |
| `Documentation/LEVEL_MODULE_SPEC.md` | c2951c06 | map | |
| `Assets/Scripts/Rendering/Transitions/FrontRoomsTransitionKit.cs` | 6ef32ddd (md5 7a8c8af1…) | visual | GUID `b215858f1c0014f87ae44abb9c680391`; V5's B0.1 `CornerReach` + `PostPiece`, called by MapWorld |
| `Assets/Scripts/Rendering/Transitions/FrontRoomsTransitionLightLead.cs` | 8f41b31e | visual | V5 on; `Borders` opt-in (`-lightleadBorders`) |
| `Assets/Scripts/Office/FrontRoomsWallFixtures.cs` | b5d05f9e | visual | outlets; `WallFace.baseTop` (§4 T6) |
| `Assets/Scripts/Office/FrontRoomsKitLibrary.cs` | 1f7dac20 | visual | props spawn |
| `Assets/Scripts/Office/FrontRoomsInteractableKit.Window.cs` | 07dc067e | visual | `DressWindow` frames every map window |
| `Assets/Editor/Rendering/FrontRoomsRenderSetup.cs` | 256cf3a4 | visual | SurfaceDefs |
| `Assets/Scripts/Rendering/FrontRoomsPrintDriver.cs` | f1df9051 | visual | the print sandwich (`FR_Print_HardEdge`) |

In-flight map work (read only):
- **A12 r2:** `~/FrontRoomsInteractionWork/a12/package/A12_double_acting.r2.diff`, sha1 50ec80b2. Its bases are MapWorld 7ef0ca45, Hunter fb2fe3dd, KitSeam b33b1d8d, Game 88c93b09, InteractionTests d9b51f23 and MAP_GENERATION 47cc5b54.
- **C22:** the clone `~/FrontRoomsMapWork/proj3`. Its MapWorld read **3880d866** at 13:2x and is still moving (handoff: 17d6eaed after the review fixes, then the C22 additions pass `wf_6334e490-eca`).
- **Harness:** `FrontRoomsTransitionAudit.cs` md5 `76a624e3217630e637bdb381af7ca5b1` (in `W/proj_trans_pick`, installed by the shared tools). `shots.json` md5 `6406e199…`.

### 1.2 What the drafts were written against

- `code/frame/` was written against the Oct 3 clone base: MapWorld md5 `e5204f9c…`, git `2c4e50f`.
- `code/renovation/` was written against the same base.
- `code/pick/` was written against main **22bb75f** (`mapworld_pick.diff`, +255 / −11).

**Since 22bb75f, `FrontRoomsMapWorld.cs` changed in 44 hunks.** The hunks that matter here:
- `BuildEdge` gained `MapChunk faceData, List<FrontRoomsWallFixtures.WallFace> faces` and `AddWallFaces(...)` calls (outlets C-1). The pick appended four parameters to the same signature, so every one of its BuildEdge hunks conflicts.
- Windows now get a root, glass stages (`FrontRoomsGlassBreakable`) and `RaiseWindowBuilt` → `FrontRoomsInteractableKit.DressWindow`. **Every map window is framed by the window kit now.** A border window takes the member of its non-Tall side, which is always the Office side (`WindowMemberFor`), so it gets `Kit_WindowFrame_Steel`: the Office owns it, as decided.
- Doors: C1 key material, C2 `EdgeNoGenerate`, broken doors (`brokenDoors`, `UseResult.Broken`), `Door.rig` (D1.6 seam, null today).
- `BuildInto` now ends with `FrontRoomsWallFixtures.Install(...)` and then `Furnish(...)`.
- W1 lamps, `lampModes` and the warn lamps changed. None of it is in code this plan touches.

**Unchanged since 22bb75f:**
- the wall, opening, floor and ceiling code;
- `Piece`;
- the B0 `CornerReach` / `PostPieceAt`;
- `ArchOpening`, `FixedSwing`'s hash;
- the light-lead hooks;
- `FrontRoomsMap.cs`, `ModuleUnits`, the `Transitions/` folder and the shaders.

So the 10-08 layout and the plan files should still match, though this must be proved again (G2). The NOW frames will differ in look (outlets, print, glass), so a new NOW baseline is rendered (§8 S2).

---

## 2. Scope

- **A border** in this plan is a theme border: an edge whose two cells have different `ZoneOf().theme` (Office | Level 0). Both cells must be outside the start area, and after C22 outside the exit area and the walkable stair's X1/X2.
- **Level 0 | Level 0 height borders stay as they are.** Their doors get D1.6's Level 0 frames (L0-F) when D1.6 lands.
- **The Office is always Standard (2.9 m), so the height pair fixes the edge kind** (`Resolve`, unchanged).

Per 24 m chunk, mean over 1,620 chunks (`../03_cut_inventory.md`, re-confirmed on 22bb75f in `16` §4.2):

| Height pair (Level 0 side \| Office) | Edge kinds | Per chunk |
|---|---|---|
| Standard 2.9 \| 2.9 | open 0.89, arch 0.37, wall (part of 2.28) | |
| Low 2.4 \| 2.9 | door 1.37, wall | |
| Tall 5.4 \| 2.9 | window 0.33, wall | |
| **Crossings** (open + arch + door + window) | | **2.96** |
| Solid border walls | | 2.28 |

---

## 3. Kits and finishes to build

### 3.1 Frame profiles: zone pair × height × edge (undressed crossing)

All pieces are render-only swept profiles: C# `Sweep` with real fillets and mitred joints, LOD0 that holds at 0.3 m. Numbers are metres unless marked, as in `../11_var_frame.md` §2.2.

| Height pair | Edge | Built piece (the Office owns it) | Office face | Level 0 face | Floor | Ceiling |
|---|---|---|---|---|---|---|
| Std \| Std | **Open** | **Portal**: two pilasters 0.12 along × 0.20 deep, 0.02 proud of each face, full height, against the wall that continues at each end. Header 0.20 deep from ceiling − 0.35 (2.55) to the ceiling (2.90). 6 mm beads on every arris. A 22 × 22 × 1.5 mm white wall angle on both header faces at the ceiling. Consecutive border open edges on one line form an arcade: a 0.24 shared pilaster and one continuous header. Where border open edges meet with no wall, a free post (0.20, or 0.24 in an arcade), beaded, **with a collider** (decided default) | Office_Wall lining; the base wraps the pilasters and posts | the paper stops on the pilaster | satin aluminium binder bar 35 × 5 mm on the line, between the pilasters | both grids die into the header faces |
| Std \| Std | **Arch** | **Cased arch**: the wall pieces beside the opening stop 0.012 short (`BeadInset`, **render only**), and the head piece starts 0.012 higher. An Office_Wall lining fills the reveal: 0.16 deep, 0.012 returns on both faces, 6 mm quarter-round beads (8 segments) on both arrises, mitred. Opening as the map computed it: top min(2.2, ceiling − 0.2); width 1.1–1.8 (or a module's per-edge `archWidth`, M4) | lining in Office paint; the base wraps into the reveal and stops at the Level 0 face plane | the paper stops at the bead | bar on the line, clear width | unchanged |
| Std \| Std | **Wall** | none on the line | base on every Office face; **end cap** at a free border wall end (inset 0.012, Office lining round the 0.16 end face with beads, the base across it); **corner guard** on each exposed Office outside corner of a border wall (0.05 × 0.05 wings, 2 mm, 3 mm nose, 1.22 m above the base, `Prop_PlasticBeige`) | unchanged (no base on Level 0) | — | — |
| Low \| Std | **Door** | **Steel frame, double-acting** (§5): sleeve casing on both faces 0.0795–0.105 off the wall centre, 0.075 wide; jambs 0–2.175, head 2.098–2.175, 3 / 1.5 mm radii; **no stop**. Saddle 0.152 × 0.012, flat top ±0.052, 1:2 bevels. `Prop_SteelBrown` (dark bronze). The map's `Cove_Base` trims (±0.10) stay hidden inside the casing (±0.105) | the base stops 5 mm short of the casing (0.575 from the door centre); 0.725 m of drywall over the head | 0.225 m of paper over the head (2.175 → 2.40) | the saddle | unchanged (the step is the wall over the head) |
| Low \| Std | Wall | none | base | unchanged | — | — |
| Tall \| Std | **Window** | **Main's `DressWindow`** frame (`Kit_WindowFrame_Steel`, the Office member). **No transition casing**, because one would double it (`../11_var_frame.md` §11.5) | the base runs under the sill (the frame does not reach the floor) | paper to the frame; 3.4 m of wall over the head | — | — |
| Tall \| Std | Wall | none | base | unchanged | — | — |

- **The base:** Cove_Base 0.10 × 6 mm (3.2 mm web, 2.6 mm cove toe) on **every Office wall face** in the built chunks, not only on borders. That is 1.7 km per 25 chunks, at 16 tris per run.
- **The pilaster cap is not built.** On this map a flat seam always lands on a portal pilaster (`../11_var_frame.md` §8.7, 0 caps needed).
- **Heights.** The kit reads both ceilings from the map, never constants. If a profile ever makes Office Low or Tall, the header stays at ceiling − 0.35 and the arch top at min(2.2, ceiling − 0.2) with no code change.

**Materials:**
- `Office_Wall`, `Cove_Base`, `Prop_SteelBrown`, `Prop_PlasticWhite` and `Prop_PlasticBeige` are all in main.
- **`Trans_AluminiumSatin`** is new: 2.1 KB, `Prop_Aluminium`'s maps, base (0.80, 0.80, 0.78), smoothness 0.80, mesh UV. It is a RenderSetup SurfaceDef row.
- The **door saddle** uses D1.6's saddle material, so the proxy and `Kit_DoorFrame_Steel_DA` match (D1.6 §3: "bronze saddle, merge M1 rule"). Carpet bars at arches and portals stay satin aluminium. A carpet bar is not a door saddle.

**Draws:**
- Office_Wall pieces (linings, portals, end caps, unfinished-portal skins) are appended into their own 6 m block's wall mesh. They add 0 draws and keep the block's grime band.
- Every other material is one renderer per chunk × material × shadow flag, built by the kit **without a MaterialPropertyBlock**.
- The pick used `AddRenderer`, which sets a `_CeilingHeight` property block. That is not reused for kit renderers.

### 3.2 On a dressed crossing (renovation, §3.3): still a frame, in its unfinished state

| Edge | Dressed (renovation owns the joint; the frame is not finished yet) |
|---|---|
| Open | **Unfinished portal (CHANGE vs the 10-08 pick, which left a dressed open edge with no piece):** the same pilasters and header, faced in `Wall_Primer`, with galvanised corner beads (30 mm flanges, 3.5 mm proud, `Prop_Aluminium`) and compound spots. **No bar:** the Level 0 carpet is cut back and the Office tiles run to the line. **Why:** "a Frame on every zone border" holds on every border, and the story stays the same (the fit-out frames the opening first, then strips the paper). Fallback if Red dislikes it in VL367: the pick's bare line, one switch |
| Arch | Bare drywall reveal (as the pick): 3 mm `Wall_Primer` liners, galvanised beads on the Level 0 arrises, a 60–100 mm compound halo on the Level 0 face. This is the casing before it is lined |
| Door | The steel frame as undressed. On the Level 0 face, a 0.08–0.20 m compound halo round the casing outline, the paper cut ragged round it (3–45 mm over the compound, 2–4 tongues, white lip), 1–3 knife smears per side |
| Window | The `DressWindow` frame. On the Tall Level 0 face, the same halo round the frame |
| Office face (all) | Finished: the base, linings and guards as undressed |

### 3.3 The renovation finish set

**The band**
- It covers the Level 0 wall faces in the same run as a dressed crossing, within 3 paper drops (2.25 m) of the line. It also covers the Level 0 face of a border wall within 2.25 m of a dressed opening.
- Drops are world-anchored on the paper's 0.75 m grid. Chunk lines lie at multiples of 24 m (32 drops), so a band that a chunk line clips always ends on a drop seam (§3.4).

| n (drops from the line) | Stage | Material |
|---|---|---|
| 0 | Fresh Office paint, with a 0.05 m brushed cut-in at the ceiling, corners and jambs | `Office_Wall_Fresh`, `Office_Wall_CutIn` |
| 1 | Primer over taped drywall: tape every 1.2 m, screw spots, compound flashing | `Wall_Primer` |
| 2 | Stripped board: face paper, backing remnants, paste streaks, gouges | `Wall_Stripped` |
| ≥ 3 | Paper intact | `L0_Wallpaper` (unchanged) |

- **Pattern per crossing** (`CrossingHash`, salt 811): 60 % full, 25 % stripped only (n = 0..1), 15 % one stripped drop. Every face round one opening shows the same state.
- **Each drop** is its own 0.08 m skin box in its stage material. There are no overlay quads.
- **Torn edges.** On every paper | stripped line there is a real mesh:
  - a ragged 5–27 mm contour, with 2–3 tongues of 40–150 mm;
  - a 2.5–8 mm white lip (`Paper_Backing`);
  - the top 0.10 m curls 15–25 mm off the wall.
  
  Also: remnants at the floor and under the ceiling, 1–3 islands of print per stripped drop, and hanging strips (`Paper_Peel`). Desktop samples the edge every 4.5 mm (hero spec). The reduced tier samples it every 12 mm (§6.2).
- **Floor**, in the Level 0 cell of a dressed crossing:
  - the loop pile is cut 0.6 m before the line, with a 5 mm frayed edge;
  - `Slab_Adhesive` sits 7 mm below the pile, with trowel ridges and a blue chalk line (`Chalk_Blue`) 0.14–0.28 m from the line;
  - the Office tiles run to the line;
  - at most one loose tile lies on the strip (`Office_CarpetTile_Loose`), with 2–4 stacked against a wall. Doors get extra rules in §5.
  
  The render slab is built in pieces. Collision keeps the full slab.
- **Ceiling**, in the same cell:
  - one 0.6 × 1.2 tile is out, over a 0.25 m `Plenum_Dark` box with T-bar webs, a hint of duct and a sagging cable;
  - one tile is pushed up askew by 30–70 mm;
  - two new 2 × 2 tiles (`Office_Ceiling_New`, 6 % brighter) sit in the 0.6 m strip nearest the line.
  
  **The hole is never in the cell's troffer slot or the tile next to it.** The lamp and fixture are untouched, so M2's limiter and the light rule see the same lamps.
- **Props** (render-only):
  - `Kit_StepStool` (in main), plus the new `Kit_TransBucket` (LOD0/LOD1 4,386 / 1,972 tris), `Kit_TransPaintTray` (1,860 / 1,018), `Kit_TransDropCloth` (1,480 / 740) and `Kit_TransTileBox` (876 / 438);
  - paper scraps (`Paper_Scrap`) and beige crepe masking tape (`Prop_TapeBeige`);
  - scraping gear stands at stripped drops, paint gear at primer drops;
  - each prop stands ≥ 1.0 m clear of every opening, against Level 0 band faces;
  - **never inside a room that Furnish dresses** (`RoomIntact && Uniform && !RoomInStartArea`) or inside a module room. Inside those rooms the band still runs; only the floor props stay out;
  - each prop gives `FrontRoomsWallFixtures` a keep-out rectangle (both files are visual-owned, so no map change). The kit places props in `Finish`, which runs before `WallFixtures.Install` (§4 T4);
  - about 2.1–2.2 per chunk at 100 %, so about 0.65 per chunk at 30 %.

**Assets**

| Kind | Count | Size |
|---|---|---|
| New materials (`FrontRooms/Surface`) | 19: `Office_Wall_Fresh`, `Office_Wall_CutIn`, `Wall_Primer`, `Wall_Stripped`, `Wall_Compound`, `Paper_Backing`, `Paper_Scrap`, `Paper_Peel`, `Slab_Adhesive`, `Office_Ceiling_New`, `L0_CeilingTile_Loose`, `Office_CarpetTile_Loose`, `Plenum_Dark`, `Chalk_Blue`, `Prop_Compound`, `Prop_PaintWet`, `Prop_RollerNap`, `Prop_Canvas`, `Prop_TapeBeige` | — |
| Textures | 10 sets, 30 PNGs (5 × 2048², 3 × 1024², 2 × 512²), from `Tools/lookdev/gen_transition_surfaces.py` and the CC0 sources already on disk (ambientCG Leaking001, Smear007, SurfaceImperfections001/007/013/015, Tape005; Poly Haven rough_linen). **No download** | 70.4 MB of source; **94.0 MB GPU on desktop** (BC7 with mips); WebGL and handheld 34.0 MB (1024² cap) |
| Props | 4 new FBX + json, Blender modules `trans_*.py` | 671 KB of FBX |

**Fixes the drafts need before anything ships:**
1. **`Paper_Scrap`** points at `Wallpaper_Chevron`. It must copy today's `L0_Wallpaper` (the hard-edge print `FR_Print_HardEdge` and the print-sandwich setup). `Paper_Peel` must be rechecked the same way. This is a decided default (`16` §6.2).
2. **Stripped, primed and painted drops carry no print and no EGRESS ink.** Remnants, islands and torn edges keep the live paper. Note for the wallpaper and narrative lines in §13.
3. Keep the renovation run's fixes:
   - the knife-smear clamp plus the non-finite guard in every mesh writer;
   - bead flanges at 3.5 mm (the float32 sawtooth at 5.8 km);
   - the prop slide rule.
4. **Drop the renovation kit's own `WallPiece` corner rule.** The pick routed every wall piece outside the start area through it (`16` §3.2). The kit now takes over only the band skins of dressed faces. Every other piece keeps the map's path (§3.5).
5. **No MaterialPropertyBlock** on any new renderer (WebGL SRP batching).

### 3.4 The 3-in-10 selection rule (deterministic per crossing, seeded)

**The map decides, through one pure function** (§4 T3):

```
bool BorderRenovated(GridCoord a, GridCoord b):
  1. theme(a) != theme(b)                                   // Office | Level 0
  2. kind = the edge kind the map builds (StartAreaEdge today; EffectiveEdge after C22)
     and kind in { Open, Arch, Door, Window }               // a crossing; walls are not
  3. not excluded: start area (a or b); after C22 the exit area (X; X1/X2 after the
     walkable stair), the EXIT door edge, and every cell within 2 cells (Chebyshev) of the
     EXIT's N cell (C22's exit-lamp hold zone, where C23 mounts the LIT sign);
     station module rects (M4) once they exist
  4. u = Unit(MapHash.Hash(runSeed, ek.x, ek.y, 977)) < profile.renovationShare (0.30)
     ek = EdgeKey(a, b) (the west/south cell, ×2 + 1 for an east edge)
```

- **No revision, no tier and no build order are read.** Both chunks, every rebuild and every revisit shift agree while the edge is still a crossing, the same way as `FixedSwing`.
- The **pattern** comes from a separate draw (salt 811), so changing the share never re-rolls which pattern a dressed crossing shows.
- **Expected:** 2.96 × 0.30 ≈ **0.89 dressed crossings per chunk**. With about 4,800 crossings over 20 seeds × 81 chunks, σ ≈ 0.0066, so **gate G5 takes 0.30 ± 0.02**. The pick's small sample at salt 0 gave 11 of 43 (26 %).
- **Why per crossing, not per border run:** a run of edges along one line can cross chunk lines. Deciding per run means reading the neighbour chunk's edges and revision, which is codex audit MAP-2. Per crossing stays one hash. Adjacent crossings on one line can differ, and the pick showed that reads fine: a stripped corridor beside a framed portal (`16` shot 2).
- **Exclusions keep these places clean:**
  - the EXIT approach (C22, C23 and the stairwell's "N must be Standard / no window in the sightline" rules);
  - the station (M4).
- **Tier lift (P2, not in the first diff).** The LT16 default says rougher at higher tiers. Proposal: `share(t) = min(0.30 + 0.04·(t − 1), 0.42)`, with t = `MapChunk.tier` of the chunk that owns the edge (C04: one tier per 150 new cells). Two problems:
  - a tier read for a chunk-line edge is a neighbour-chunk read;
  - a revisit shift changes it ("a revisit shift takes the tier of its time").
  
  So it goes in only after the map's C3 post table settles neighbour reads, with a cross-chunk agreement test. Data: a `renovationShare` column in `FrontRoomsTier`.

**Neighbour reads:**
- **Renovation may read the neighbour chunk's edges** (seed-pure). After a revisit shift, a stale read gives at worst an orphaned stage drop or slab strip: no overlap and no z-fight. That still reads as a building under work.
- **Frame corner pieces at chunk-line posts** (end caps, guards, free posts) follow the map's post rule (§3.5). A stale read there is an overlap, which is MAP-2.

### 3.5 Corner posts belong to the map

- **Today:** main's B0.1 (`CornerReach`, the per-skin reach in `FrontRoomsTransitionKit`) is live, and it stays live.
- **The full fix is the map's.** Red approved the full post-seam fix for after the Alpha (map chat, 10-22: "C3 不落、Alpha 后做完整修复"). Its form is a world table keyed by post, with quarter boxes per finish. That is the same idea as Frame's B0.1 quarters (`../11_var_frame.md` §2.1).
- **LT16's default** is "fix the corner flicker in the map first".
- **So the kits never build a post.** They read the post rule. Two pieces need it:
  - the **end cap**: at a free border wall end, both skins stop at −`BeadInset` and the kit wraps the end in Office lining;
  - the **corner guard**: it needs the Office outside arris.
- **Interface asked from C3:**
  - per lattice point: four arms (the effective edge kinds), the finish per quarter (material + height class), the owner, a `special` flag (start or exit area), and a `FreeEnd` flag;
  - one query, `PostAt(GridCoord)`, which the view (T2) forwards.
- **If C3 is not in when this lands:** the diff keeps `CornerReach` exactly as main has it, and adds only the end-cap inset to `Piece`'s render extents. Collision is unchanged either way.

---

## 4. The map hunks (`CONTRACT_map.diff`, map-owned; clone only)

- **Target:** today's main, MapWorld **7ef0ca45**, with A12 r2 applied in the clone (§5). Line numbers below are on 7ef0ca45.
- **Rules followed:** each hunk is marked `// TRANSITION`. The kit is reached through **one field** (`transition`), not new `BuildEdge` parameters, so outlets' `faceData`/`faces`, A12's door lines and C22's `EffectiveEdge` lines keep their text.
- **Delivered as:** `production/CONTRACT_map.diff`, with `production/CONTRACT_map_README.md` giving each hunk's reason and the decision it serves.

| # | File (base) | Where | Change | Why (decision) | Lines |
|---|---|---|---|---|---|
| T1 | MapWorld (7ef0ca45) | `MeshBuilder` (:381) | `Append(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t)` (the pick's `Raw` and `Append` as one) | Office_Wall kit pieces and band skins join their block's mesh: 0 extra draws, the block's `_CeilingHeight` (grime band). Frame + Renovation | +8 |
| T2 | MapWorld | fields beside `level0, office` (:519); new `TransitionView()` | One read-only view per cache for the visual kit: `theme(cell)`, `ceiling(cell)`, `edge(a, b)` (the kind the map builds: `StartAreaEdge` now, `EffectiveEdge` after C22), `excluded(cell)`, `renovated(a, b)` (= T3), `dressedRoom(chunk, i, j)`, `seed`, `officeWall`, `level0` materials; later `PostAt` (C3) | The kits stay pure; the map stays the only reader of its cache and its rules. Every decision | +25 |
| T3 | MapWorld | new `public bool BorderRenovated(GridCoord a, GridCoord b)` | §3.4 | **The map decides which crossing is renovated**, seeded and testable in map tests. Renovation 3 in 10 | +14 |
| T4 | MapWorld | `BuildInto` (:1150) | (a) `transition = view != null ? FrontRoomsTransitionKit.BeginChunk(view, coord, chunk.root.transform, origin) : null` before the cell loop. (b) The two `Solid` slabs (:1196–1197): when `transition.TakesCell(cell, …)` (a Level 0 cell of a renovated crossing), add both full slabs to `collision` only; else today's `Solid`. (c) After the two `BuildEdge` calls: `transition?.Corner(cell)` (the cell's south-west lattice point, so each point once). (d) After the cell loop, before the block renderers (:1244): `transition?.Finish(Get)`. The kit merges its pieces into the builders, makes its own loose renderers and props under `Transitions` (no MPB, colliders off), and returns `colliders` (free posts), which go to `collision.Box`. Then `transition = null` | Floor and ceiling of a crossing cell (Renovation); corner pieces (Frame); free-post colliders (LT16 default) | +22 / −2 |
| T5 | MapWorld | `BuildEdge` (:1371) | (a) :1376 `if (kind == EdgeKind.Open) { transition?.Open(a, b, start, along, height, sideA, sideB); return; }`. (b) `Piece` (:1393): **collision boxes exactly as today** (same extents, same order: hash-tested). For pieces the kit touches, `solid()` splits into `collision.Box` plus a render box with render-only insets (`BeadInset` beside an undressed border arch; the end-cap inset at a free border end), or the render goes to `transition.Skin(...)` (a dressed band face becomes per-drop skins). Every other piece keeps today's code path. (c) Arch (:1446–1448): the bead inset as a render-only inset. (d) After the wall piece (:1422) and after the openings (:1450): `transition?.Edge(kind, a, b, start, along, height, c, width, openingTop, sill, sideA, sideB)`, with **the map's own computed opening**, so a module's per-edge `archWidth` (M4) and C22's EXIT door need no second copy of the rule | Portals, casings, steel frames, base, bars, halos, reveals, band | +35 / −6 |
| T6 | MapWorld | `AddWallFaces` (:1605) | `baseTop = transition != null && zone.theme == ZoneTheme.Office ? ModuleUnits.CoveHeight : 0f` | Outlets C-1 keep `BaseClear` 0.03 above the new rubber base on Office faces | +1 / −1 |
| T7 | `FrontRoomsModuleUnits.cs` (f374d320) | constants | `BeadInset .012`, `PortalPilaster .12`, `PortalDepth .20`, `PortalHeaderDrop = BulkheadDepth`, `SharedPilaster .24`, `FreePost .20`, `BarWidth .035`, `BarHeight .005` | One source for the keep-clears in LEVEL_MODULE_SPEC (`../11_var_frame.md` §5) | +4 |
| T8 | `FrontRoomsLevelProfile.cs` (d9e17bd0) + `Assets/Levels/FrontRoomsLevel0.asset` (07514c33) | header "Dressing" | `public bool transitions = true;` `[Range(0f, 1f)] public float renovationShare = .30f;` and the two asset values | Data-driven share; the Level Designer's live tuning (`ApplyLive`) re-reads it | +4 / asset +2 |
| T9 | `Editor/FrontRoomsMap/FrontRoomsMapInteractionTests.cs` (d9b51f23) | new section "Transitions" | (1) The collision hash with kits on = kits off + exactly the free-post boxes. (2) `BorderRenovated`: the same set from both chunks, after `RebuildChunk`, and after a revisit shift where the edge stays a crossing. (3) Share 0.30 ± 0.02 over 20 seeds × 81 chunks. (4) Never in excluded cells. (5) `transitions = false` builds the same renderers and meshes as today | Determinism, layout and collision freeze | +90 |
| T10 | `MAP_GENERATION.md` (47cc5b54), `LEVEL_MODULE_SPEC.md` (c2951c06) | docs | A "Transitions" bullet. Keep-clears: props against Office walls clear the 6 mm base; at border open edges, props clear the pilasters (0.12) and the header (0.35 drop); render-only renovation props never stand in a dressed room | Contract text | docs |

- **Total:** about +115 / −10 lines in MapWorld (the 10-08 pick: +255 / −11), +4 ModuleUnits, +4 profile, +90 tests.
- **Not in the diff:**
  - `FrontRoomsMap.cs` / `Resolve` (Close is not decided);
  - any lamp code (the light rule is open);
  - `FrontRooms3DGame*.cs`, the Hunter, `Assets/Levels/Modules`.

**What the map hands the visual side, and what comes back:**
- **Per chunk:** coord, root, origin, `Get(block, material)`, `BlockOf(i, j, ceiling)`.
- **Per edge:** kind; a, b; start, along, across; height (the max of the two sides); both ceilings; both themes; both wall materials; the side blocks; `mayExtendStart/End`; the opening lo / hi / top / sill exactly as `BuildEdge` built it; the `renovated` and `excluded` flags.
- **Per cell:** cell, local centre, ceiling, block, floor and ceiling materials.
- **Per lattice point:** arms, 4 themes, 4 ceilings, 4 wall materials, `special`, and the C3 post entry when it exists.
- **From the kit back to the map:** render boxes and meshes per (block, material); and **collider boxes for free posts only**. The kit makes its own loose renderers and props.

---

## 5. What changes with A12 double-acting doors

**A12 in the map** (r2, 50ec80b2):
- `Door.swing` turns away from whoever moves the leaf (`SwingAwayFrom`);
- `doorSides` keeps the side by edge for rebuilds;
- there is a new `DoorMotion.Through`.

It changes no wall, opening, trim or `Piece` code. Its `BuildEdge` hunk is the one door-creation line (`swing = doorSides…`, :1479). Our T5 (d) sits above it at :1450. **A plain 3-way merge, with no semantic conflict.**

What it changes for the frame:
1. **No stop.** The draft's 16 mm push-side stop (`Sleeve(withStop)`) is removed. The casing is the same on both faces, and nothing reads `door.swing`. The draft's to-do "rebase the door hook on single-acting doors / the stop follows the swing side" (`../11_var_frame.md` §11.3, `../20_variation_frame.md`) is **withdrawn**.
2. **The proxy is D1.6's DA envelope** (`interactables/d16/10_design.md` §2.2):
   - casing S 0.0795 → 0.105 and P −0.105 → −0.0795;
   - along the door: hinge −0.075 → 0.002, latch 0.998 → 1.075;
   - jambs 0–2.175, head 2.098–2.175;
   - saddle ±0.076, flat top ±0.052, 0 → 0.012.
   
   No butts, no closer. The seal fins and pivot caps are D1.6's, not the proxy's.
3. **D1.6 replaces the proxy door by door.** D1.6 is BLOCKED today (`interactables/d16/30_status.md`: no clone, no rig, no `_DA` kits in main). When `DressDoor` lands it sets `door.rig`, and the kit then stops building the frame on that door; it keeps the base stop and the renovation halo. A visual-owned static (`FrontRoomsInteractableKit.DoorFramesLive`) decides this, so nothing depends on build order.
   - **Border doors must take an Office member** (OF-F / OF-K, `Kit_DoorFrame_Steel_DA`), because the Office owns every frame. This is a D1.6 member rule. It is visual-owned and is noted in §13.
4. **The leaf sweeps both faces.** The sweep is r 0.98 m about the hinge, 0–95°, and includes a break's throw and its crooked rest.
   - **No renovation prop, tile stack, hanging strip or scrap** stands within the sweep disc + 0.05 m on either face.
   - Inside the disc, nothing is higher than 0.012 m (the leaf clears the floor by 0.015 m): only the slab strip, the fray and the saddle. The one loose tile lies outside the disc.
   - Tile stacks keep out of the map's 1.2 m door keep-clear strip on both faces.
5. **Halo and paper cut** are on the Level 0 face only, sized from the DA casing outline (0.075 beyond each jamb, head 2.175) plus 0.08–0.20 m.
6. **The base** stops 5 mm short of the casing's outer edge.
7. **Close (not decided)** would turn more borders into doors. They would get the same frame for free; nothing here prepares for it or blocks it.
8. **The clone is main + A12 r2**, applied with `git apply` inside the clone. A12 lands before our hunks (the map's order), so frames are fitted and filmed on double-acting doors (VL370). `W/proj_a12` is only read.

---

## 6. Budgets

### 6.1 Reference, measured 10-08 over main 22bb75f

Measured within 46 m of the eye by `FrontRoomsTransitionAssets.Stats`:

| | NOW | Frame only | Pick (Frame + renovation at 0.30) |
|---|---|---|---|
| Triangles | 136–146 k | +35–41 k (+21–24 %; Oct 3 base) | 305–314 k (×2.2) |
| Renderers (≈ draws before culling) | 1,711–1,744 | +62–80 | 1,893–1,919 (+9–11 %) |
| Kit renderers in the frustum | 0 | 23–28 | 68–91 |
| Materials | 42 | 43 | 62–63 |
| Textures added | 0 | 0 MB | 94 MB desktop (WebGL 34 MB) |
| Mesh per 25-chunk build | — | +4.3–4.6 MB | not measured |
| Lights | 84 / 82 / 83 / 88 lit | unchanged | unchanged |

### 6.2 Targets for production

These are measured on today's main + A12, in the same clone, against the new NOW baseline: shots 1–4 and 8–10, within 46 m, with the same Stats method.

**Desktop is the reference, at the highest spec: no downgrade.** Hero meshes keep the 4.5 mm tear sampling. The texture sets keep their authored 2048².

| Item | Frame only | Frame + renovation at 0.30 | Hard ceiling (fail) |
|---|---|---|---|
| Δ triangles | ≤ +45 k | **≤ +110 k** (pick +169 k) | +135 k |
| Δ renderers within 46 m | ≤ +70 | ≤ +8 % | +10 % |
| Kit renderers in the frustum | ≤ 30 | ≤ 60 | 75 |
| Δ materials | +1 (`Trans_AluminiumSatin`) | +20 (satin + 19 renovation) | +21 |
| Δ texture memory | 0 MB | 94.0 MB, loaded on the first renovated crossing in range, not at map start | 95 MB |
| Δ mesh per 25-chunk build | ≤ +5 MB | ≤ +12 MB | +15 MB |
| Lights, lamp modes | 0 | 0 | any change |
| Chunk build CPU (median over the 25 chunks, editor, Stopwatch round `BuildInto`) | ≤ +2 ms | ≤ +5 ms | +8 ms |

- **Where the desktop cut comes from:** removing the per-face re-cut of every undressed wall (the pick's `WallPiece`, §3.3 fix 4). That part of the pick's +169 k sat inside the block meshes, not in the 55–69 k kit triangles. No hero LOD is lowered.
- **True draws need a Play capture,** because batch mode reads 0 draw calls (`../11_var_frame.md` §4.2). The proxy is renderers in the frustum, as in every earlier report.

**The reduced tier, WebGL and handheld.** One static `FrontRoomsTransitionKit.Reduced`:
- `true` under `#if UNITY_WEBGL && !UNITY_EDITOR`, or when `FrontRoomsHandheld.Active`;
- the editor tool argument `-transitionReduced` only for measuring.

Editor, Mac and Windows never take it. G8 proves byte-identical frames with it compiled in.

| Item | Rule | Target |
|---|---|---|
| Textures | WebGL import tab and iOS/Android tab max 1024² on the 10 renovation sets (desktop tab untouched) | ≤ 34 MB |
| Tears and cut lines | 12 mm sampling; no hanging strips, islands, scraps or tape; a straight carpet cut (no fray mesh) | — |
| Props | none on the reduced tier (props were 101–129 k of the tris at 100 %) | — |
| Base | appended into the block's existing `Cove_Base` trim builder (one renderer fewer per chunk; it then casts shadows like the trims) | — |
| Bars and saddles | one satin renderer per chunk | — |
| Δ triangles within 46 m | | ≤ +50 k |
| Δ renderers in the frustum | | ≤ +40 near a renovated crossing, ≤ +25 near a framed one (about +1.3 % / +0.8 % of WebGL's ~3,200 draws) |
| Δ mesh per 25 chunks | | ≤ +8 MB |
| WebGL frame time | **not measured in this workflow** (no WebGL build here); ESTIMATE from the draw proxy; open item for WebGL 卡顿性能分析 | ≤ +2 % |

---

## 7. What we ship (visual-owned) and how it merges

| Path | What |
|---|---|
| `Assets/Scripts/Rendering/Transitions/FrontRoomsTransitionKit.cs` | **Merge, never copy.** `static class` → `static partial class`. Keep GUID `b215858f…`, `B0`, `HasArg`, `PostPiece`, `CornerReach` and the V5 behaviour byte for byte. Add `Reduced`, the modes (`-transitionsOff` stays the inert switch; new `-transitionFrameOnly`, `-renoShare F` tool overrides) |
| `…/Transitions/FrontRoomsTransitionKit.Frame.cs` (new) | the frame kit (from `code/pick/FrontRoomsTransitionFrameKit.cs.txt`): stopless DA sleeve proxy, unfinished-portal variant, no window casing |
| `…/Transitions/FrontRoomsTransitionKit.Renovation.cs` (new) | the renovation kit (from `code/pick/FrontRoomsTransitionRenovationKit.cs.txt`), without its own corner rule; skins only on dressed band faces; exclusions; A12 sweep rules |
| `…/Transitions/FrontRoomsTransitionKit.Mesh.cs` (new) | `Sweep`, `EarClip`, profiles, `TransitionMesh`, the finite guards |
| `…/Transitions/FrontRoomsTransitionKit.Chunk.cs` (new) | `BeginChunk`, `TakesCell`, `Open`, `Edge`, `Skin`, `Corner`, `Finish` (the T4/T5 entry points) |
| `Assets/Scripts/Office/FrontRoomsWallFixtures.cs` | read the kit's prop keep-outs (a static list filled before `Install`) |
| `Assets/Editor/Rendering/FrontRoomsTransitionAssets.cs` | materials, band maps, Stats (tools) |
| `Assets/Editor/Rendering/FrontRoomsRenderSetup.cs` | SurfaceDef rows: `Trans_AluminiumSatin` + 19 renovation materials; `Paper_Scrap`/`Paper_Peel` from `L0_Wallpaper` |
| `Assets/Resources/Surfaces/*.mat` (20) and `Surfaces/Textures/*` (30, with WebGL/iOS/Android 1024 overrides in the `.meta`) | |
| `Assets/Resources/Props/Models/Kit_Trans{Bucket,PaintTray,DropCloth,TileBox}.fbx/.json` | |
| `Tools/Blender/frontrooms_kit/assets/trans_{bucket,paint_tray,drop_cloth,tile_box}.py` | |
| `Tools/lookdev/gen_transition_surfaces.py` | |

- **New `.meta` GUIDs:** checked unused in main.
- **Clone editor scripts:** ASCII only, with no C# tuples. The pick's `TransitionSink` used a tuple key; production uses a small struct.
- **When:** the visual files are inert without the map hunks (nothing calls them), so they can land first through `W/apply/level_transitions.sh` once verified (Red 10-21, "验证通过就直接合进主工程"). The materials and textures are then only on disk. Nothing is loaded at run time until T4 calls the kit.

---

## 8. Stages after this plan

Every Unity call:
- `-batchmode -buildTarget OSXUniversal`;
- edit-mode harnesses add `-quit`; renders never use `-nographics`;
- one Unity per clone; at most 2 on the visual line (`pgrep -fl "Unity.*-projectPath /Users/redwang/FrontRoomsVisualWork" | wc -l` < 2 first);
- timings at a 1-minute load above 32 are marked UNDER LOAD (it was 39–58 at 13:19).

1. **S1 Clone.**
   - Read `W/TOOLS.md`, then run `W/tools/mkclone.sh proj_lt`.
   - Apply A12 r2 in the clone (`git apply` of `A12_double_acting.r2.diff`; the main bases match 7ef0ca45 / d9b51f23 / 47cc5b54).
   - Compile (`-nographics -quit`): 0 `error CS`.
   - Run `roslyn_check.py --all --ios`.
   - Write `MKCLONE.txt` and the bases into `production/20_build.md`.
2. **S2 NOW baseline, before any kit file.**
   - `ShotsBatch` with `shots.json` + `code/pick/pick_shots*.json` (shots 1–4, 8–10) `-plan`.
   - Compare the `-plan` JSONs with `../logs/shot<k>_plan.json`: identical expected, since `FrontRoomsMap.cs` is unchanged.
   - Run `CountBatch` (1,620 chunks), Stats and the collision hash.
3. **S3 Visual side:** the partial-class merge, materials (`FrontRoomsTransitionAssets.Prepare`), textures, the prop reimport, the RenderSetup rows.
4. **S4 Map hunks** in the clone (§4). Check that `-transitionsOff` gives frames byte-identical to S2 (G1).
5. **S5 Renders:**
   - modes off / frame / pick / pick + reduced;
   - the fixed and extra shots;
   - close frames (`EvidenceBatch -perType 3 -noCount`);
   - DA door frames (shut, 45° both ways, broken rest, from both faces).
6. **S6 Stats and counts** for every gate.
7. **S7 Verify:** independent lenses (spec, engineering, look) and fixes.
8. **S8 Package:**
   - `W/apply/level_transitions.sh` (visual paths, dry run on main);
   - `production/CONTRACT_map.diff` + `production/CONTRACT_map_README.md`;
   - VL rows in `W/vl_pending/level_transitions.md` (VL365–VL370 only);
   - Shot-format images (`shotframe.py`);
   - notes for other lines in `production/`.

---

## 9. Gates (all must pass before any landing)

| Gate | Pass when |
|---|---|
| G0 base | `proj_lt` = main at its sync + A12 r2; Roslyn `--all --ios` COMPILE OK; harness md5 `76a624e3…` |
| G1 inert | `-transitionsOff` (and `transitions = false` in the profile): the four fixed shots byte-identical to S2; the same renderer/mesh list |
| G2 layout | every `-plan` file identical across off / frame / pick / reduced, and to S2 |
| G3 collision | the hash (FNV-1a over all chunk colliders) for kits on = kits off + exactly N free-post boxes, N = the kit's free-post count; map suites by 关卡设计 at landing: interaction, nav 60/60, door shot, seeds 100/100, Level Designer, play 2554/20388, touch |
| G4 lamps | lit/shadowed per shot as S2; no `SetLampMode` or lamp mode changed; V5 colour unchanged; `-lightleadBorders` still opt-in |
| G5 determinism | the T9 tests; share **0.30 ± 0.02** over 20 seeds × 81 chunks; 0 renovated crossings in excluded cells; the same set from both chunks, after a rebuild and after a shift |
| G6 look | at every close frame (post, wall end, seam, door both faces, window, arch, portal, unfinished portal): no floating, clipping, z-fight or near-plane contact; the casing hides the map trims; the paper stops on a built piece at every border |
| G7 A12 fit | a sweep check of every kit piece against the leaf at 0–95° in 5° steps on both faces, plus the break rest: 0 intersections above 0.012 m; the base 5 mm off the casing |
| G8 tiers | desktop frames byte-identical with the `Reduced` code compiled in and off; the reduced-tier frames render with 0 errors; the WebGL/iOS defines compile (Roslyn) |
| G9 budgets | §6.2 targets met; any hard ceiling = FAIL |
| G10 outlets | outlets probe (`research/outlets/data/30_FrontRoomsOutletContractProbe.cs.txt` 1ba48cea) PASS with `baseTop` 0.10; 0 fixtures inside a kit piece or a prop keep-out |
| G11 print | stripped/primed/painted drops show no print or ink; remnants, islands and scraps show today's hard-edge print (sampled pixels against `L0_Wallpaper` at the same world point) |
| G12 era | no labels, brands or printed dates; beige crepe tape only (no blue tape) |

---

## 10. Verification frames (VL365–VL370 only, `W/vl_pending/level_transitions.md`, Shot format)

| VL | Title | Question | Images |
|---|---|---|---|
| VL365 | Today's game, kits off | Is the hooked build exactly today's game (frames, plans, collision)? | NOW shots 1–4, 8–10; the G1/G2/G3 table |
| VL366 | A frame on every border | Does every Office \| Level 0 change now sit on a built Office piece? | shots 1, 2, 4, 8, 10 NOW \| FRAME; close sheet (portal, cased arch, end cap, guard, seam, base) |
| VL367 | Renovation on 3 in 10 | Does a renovated crossing read as work in progress, and is it rare enough? | shots 2, 9, 10 NOW \| PICK; the Level 0 face of a renovated door; the unfinished portal (and the pick's bare line beside it); a 13 × 13-cell plan with renovated crossings marked; the share table |
| VL368 | Your frame and the arch | What changes where Red looks (shot 4) and at the arch (shot 1)? | shot 4 and shot 1 NOW \| PICK, with the % of changed pixels |
| VL369 | What it costs | Desktop vs the reduced tier: triangles, draws, memory, build time | Stats table plate; shot 2 desktop \| reduced |
| VL370 | Double-acting doors in the frame | Does the frame fit the A12 door from both faces, open both ways and broken? | shut, 45° to S, 45° to P, broken rest, from the Office and Level 0 sides; G7 table |

---

## 11. Conflicts with in-flight map work, and the landing order

| Work (owner, state) | Where it meets this plan | Resolution |
|---|---|---|
| **codex C3 full post-seam fix** (关卡设计, after the Alpha; map handoff §4 item 6, next to "level transitions map hunks") | Both want the 0.16 m post. The 10-08 renovation kit rebuilt every wall piece through its own corner rule | **The map owns posts** (§3.5). The kits drop their corner rule and read C3's table. **Land C3 first or together** (LT16: "fix the corner flicker in the map first"). Interface asked: `PostAt` (§3.5) |
| **C22 EXIT + stairwell** (关卡设计, built in proj3, lands after M2 → A12 → glass → sound) | `EffectiveEdge` replaces the edge reads in `BuildInto` / `PostPieceAt`; `InExitArea`; `SealExit`; FrontRoomsStairwell; exit-lamp hold; C23 sign mount. Textual overlap with T4/T5 in `BuildInto` and `BuildEdge` | The view's `edge` = `EffectiveEdge`, `excluded` adds `InExitArea` and the EXIT door edge; nothing renovated within 2 cells of the EXIT. The transition hunks are re-cut by 3-way onto main after C22 (two lines change) |
| **Walkable stair** (after C22; `research/stairwell/60_walkable_stair_for_map.md`) | X1/X2 reserved on both floors | `excluded` covers X1/X2 (one line when it lands) |
| **A12 double-acting doors** (交互设计's r2, lands second after the Alpha) | `BuildEdge` door branch (:1479) next to T5 (d) | No semantic conflict (§5); the stop is gone; plain 3-way |
| **D1.6 DA frames** (游戏视觉, BLOCKED) | Border door frames | The proxy is built to D1.6's envelope; it gives way to `DressDoor`; the border doors' member rule = Office (§5.3); one saddle material |
| **M2 ambient flicker limiter** (关卡设计, first after the Alpha) | Lamps | None: no lamp, mode or fixture changes here (G4) |
| **Glass gd3c contract** (游戏视觉 → map; window hold/progress) | the window branch of `BuildEdge` | None: the kit only reads the window's opening; window lines untouched |
| **Outlets C-1** (landed) | `AddWallFaces.baseTop`; props vs fixtures | T6 + prop keep-outs (visual-internal); G10 |
| **Station M4** (map + 游戏视觉 + 叙事, after C22) | per-edge `archWidth`; station rects | The kit takes `BuildEdge`'s computed opening (T5 d); station rects excluded from renovation (frames still apply) |
| **Wallpaper / EGRESS cues** (动画和动态图形模型技术 + 叙事设计; rev 4 "the paper is the plan") | stripped/painted drops carry no print | Note in §13; cue placement must avoid or accept the band (≤ 2.25 m round about 0.89 crossings per chunk) |
| **The Alpha** | — | Nothing here lands before Red confirms the Alpha is handed in; the map order above is 关卡设计's |

**Proposed landing order:**
1. Visual files (inert) whenever verified.
2. Then, in 关卡设计's queue after C22 and its walkable-stair step: codex C3, then `CONTRACT_map.diff` (3-way onto that main), one suite run for both.

---

## 12. What stays open (not decided; nothing here pre-empts it)

- **Close** (map rule: Office | Level 0 borders become a door or a wall). Not touched: `Resolve` is unchanged. If Red picks it later:
  - the new doors get this frame for free;
  - fewer open and arched crossings means fewer portals;
  - the 3-in-10 draw needs no change.
- **The light rule** (soft or dark border cells, LT08). V5 runs as today. `Borders` stays opt-in. The renovation's dark plenum hole is not a dark cell, and no lamp changes.
- **The media download** (~60 MB, 22 rows). Not approved. Nothing is downloaded; every texture comes from CC0 sources on disk.
- **Tier lift** (LT16 default "rougher at higher tiers"). The design is in §3.4. It is held for P2 behind C3 and a cross-chunk test.
- **The unfinished portal on renovated open edges** (§3.2). This is my reading of "a Frame on every border". Red sees it in VL367; one switch reverts to the pick's bare line.
- **WebGL frame time.** Only the draw proxy is measured here. A WebGL build measurement is for WebGL 卡顿性能分析.
- **The base colour** (`Cove_Base` 0.23 / 0.19 / 0.14, a strong dark line). The pick showed it and Red kept the pick. Left as is.

---

## 13. Notes for other lines (written here; no messages are sent)

- **关卡设计:**
  - §4 is the contract, delivered as `CONTRACT_map.diff` + README after verification.
  - Asks: C3's `PostAt` interface (§3.5); `EffectiveEdge` / `InExitArea` names kept public (C22); free-post colliders accepted as a collision change (LT16 default, G3); T9 tests in your suite.
  - The transitions land after C22 + the walkable stair, with or after C3.
- **游戏视觉 (D1.6):**
  - border doors take an Office member (OF-F / OF-K, `Kit_DoorFrame_Steel_DA`);
  - one saddle material shared with the transition proxy;
  - publish `FrontRoomsInteractableKit.DoorFramesLive` when `DressDoor` lands.
- **动画和动态图形模型技术 (wallpaper, print driver) and 叙事设计:**
  - within ≤ 2.25 m of about 0.89 crossings per chunk, Level 0 drops become stripped board, primer or fresh paint, with no print, no motion and no EGRESS ink;
  - remnants, islands, torn edges and scraps keep the live print;
  - pattern cues (WAY ON, ALARM ROUTE) placed by cell should skip renovated band faces (`BorderRenovated` + the drop distance) or accept losing them there.
- **交互设计:** nothing to do. Doors behave as A12 says; the kit only fits around the leaf (§5.4).
- **平面视觉:** nothing printed. No type or signage here.
- **声音设计 (optional, not requested):** an aluminium footstep on bars and saddles; a quiet work-site room tone near renovated crossings.
- **WebGL 卡顿性能分析:** the reduced tier (§6.2) is ready for a WebGL frame-time capture once it lands.
