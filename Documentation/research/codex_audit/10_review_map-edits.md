# Codex audit · 10 · Review: map-file and gameplay-path edits

Date: 2026-10-03, 22:30–22:50 PDT. Track: map-file and gameplay-path edits. **Read only.** I did not open Unity, run anything in Unity, or edit any project file except this report.

Base `7320ed1` (pre-Codex). HEAD `75cfdff`. At 22:45 the working tree has no change under `Assets/Scripts`, `Assets/Levels` or `Assets/Editor/FrontRoomsMap`. `FrontRoomsMapWorld.cs` md5 is `a152e6bb…` (= HEAD).

Scratch evidence (outside the project): `SCR/mapaudit/` holds `mw_base.cs`, `mw_head.cs`, `clone_ll.diff`, `main_codex.diff`, the B0 model `b0_model.py`, and the three patches below. SCR = `/private/tmp/claude-501/-Users-redwang-Desktop-ArtCenter-Fall26T7-EGAM-401A-01-Individual-Game-Project/5656cffd-bc90-45f6-86a3-09b26549df8d/scratchpad`.

---

## 0. Short answer

- **Who wrote what.**
  - All 11 hunks in `FrontRoomsMapWorld.cs` are Codex's. The map chat confirms this in `NOTE_map_chat_attribution.md`.
  - `FrontRooms3DGame.cs` has only the other Claude session's logo fix (`a7b0dbb`). `git diff a7b0dbb HEAD` on that file is empty.
  - `FrontRoomsRoomStream.cs` has 2 lines: one from `a7b0dbb`, and the glass G6 hook (glass track, verified).
  - Nothing changed under `Assets/Levels`, `Editor/FrontRoomsMap`, or any other `FrontRoomsMap/` file.
- **The code matches its source.** Codex copied the V5 clone (`proj_trans_lightlead`) faithfully. The only changes in code lines are:
  - the new `Borders` gate;
  - the `LampModeOf` mirror;
  - the `DressWindow` call;
  - the removal of the old `Ensure()` call.

  Codex rewrote the comments and dropped the `// TRANSITION lightlead` markers.
- **Nothing in this track throws or breaks the build.**
  - Red's editor log has no map, transition or window exception.
  - Determinism holds for the same seed and the same play path. The transition code never uses `UnityEngine.Random` or time.
  - Nav, interaction and Relay sight are unchanged (§3.6).
- **What changed in Red's game:**
  - Office lamps went from warm (1.00, 0.96, 0.88) at 5.5 to cool (0.90, 0.96, 1.00) at 6.0.
  - Office lenses now use `Troffer_Lens_Cool`.
  - The B0 corner and grime-band fix is on.
  - Level 0 lamps are unchanged.
  - No lamp goes dead or dim from the border rule, because that rule is off by default.
- **Four real problems:**
  1. **Decision (Red).** The V5 colour lead is on by default, but Red has not picked an N1 variation.
     - "Colour only" is a combination that no render shows.
     - B0, on the other hand, fixes the corner seam Red reported to Codex at 13:09. I recommend keeping B0.
  2. **Major (map).** B0.1 breaks the generator's rule that "neighbours agree no matter which is built first":
     - After a revisit shift, a chunk-line junction can show an 8 × 16 cm notch.
     - Each build also generates the data of up to 6 neighbour chunks early, at the current tier.
  3. **Minor (map).** With `-lightleadBorders` or `-lightleadSoft`, `LampModeOf` generates chunks. That breaks its own contract and the map test that checks it.
  4. **Minor (map, owned by window-landing).** The `DressWindow` call is not the window contract: the map's trims stay under the kit frame.
- **Also:** main's `FrontRoomsTransitionKit` is the V5 version. Any other pick must rework Codex's map hunks, and one B0 rule must be chosen. Codex's N1 notes are also wrong (one docs finding).

---

## 1. Files in scope and who wrote them

| File | Change 7320ed1 → HEAD | Author | Evidence |
|---|---|---|---|
| `Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs` | +138 / −31, 11 hunks | **Codex** (8ef5b64, 75cfdff) | `00_main_state.md` §0: Codex's 18:40 backup = `7320ed1`; map chat note |
| `Scripts/Rendering/Transitions/FrontRoomsTransitionKit.cs` (+meta) | new, 119 lines | Codex copy of `proj_trans_lightlead` | md5 `7a8c8af1…` = clone; GUID `b215858f…` = clone |
| `Scripts/Rendering/Transitions/FrontRoomsTransitionLightLead.cs` (+meta) | new, 82 lines | clone + Codex `Borders` gate | `diff` vs clone: only lines 24–29 added |
| `Scripts/Rendering/Transitions.meta` | new folder meta | Red's Unity (fresh GUID) | `00_files.tsv` |
| `Resources/Surfaces/Troffer_Lens_Cool.mat` | new | clone | md5 `1eecb065…` = clone |
| `Editor/Rendering/FrontRoomsRenderSetup.cs` L317 | `Troffer_Lens_Cool` SurfaceDef | Codex | matches the .mat (emission 1.989/2.121/2.210, `TrofferLens` texture) |
| `Scripts/FrontRoomsRoomStream.cs` | L49 `public const DoorOpenSeconds`; L351–352 G6 hook | `a7b0dbb` (other session); glass track | `git show --stat a7b0dbb`; glass `30_final.md` §4.3 |
| `Scripts/FrontRooms3DGame.cs` | logo relay clock only (+39 / −13) | `a7b0dbb` (other session) | `git diff a7b0dbb HEAD` is empty |
| `Assets/Levels/*`, other `FrontRoomsMap/*`, `Editor/FrontRoomsMap/*` | none | — | `git diff --name-status` |

---

## 2. Hunk by hunk: `FrontRoomsMapWorld.cs` (HEAD line numbers)

| # | Lines | What it does | Source | Matches the design? | Verdict |
|---|---|---|---|---|---|
| 1 | 347–349 | `ThemeMaterials.lampColor`, default warm (1, .96, .88) | V5 H1 | yes | inert alone |
| 2 | 484 (was 485–488) | removes `FrontRoomsMetalGlassRTController.Ensure()` from `Awake` | G14 | exactly G14 contract C1 (`glass/rt/10_rt_glass_design.md:377`) | correct; a map edit without the map chat's sign-off |
| 3 | 981–995 | B0.2: each face of a height-border wall goes into its own side's height block | V5 H6 | the plan's B0.2 (`10_transition_plan.md` §2.3) | works. Zones only, no cross-chunk read |
| 4 | 1092–1127 | B0.1: `CornerReach` and `PostPieceAt` describe the 4 pieces at a post | V5 H8 | V5's per-skin rule. This differs from the brief (V5 report §7) and from Frame's quarter-post B0 | **MAP-2, MAP-5** |
| 5 | 1135–1181 | `BuildEdge`: per-skin reach and blocks | V5 H7 | same code lines as the clone | inert with `-transitionsOff` (old box sizes and centres) |
| 6 | 1296–1308 | `RaiseWindowBuilt`: direct `DressWindow` call; the early return moved below it | Codex's own | **not** the window contract (r3/r4) | **MAP-4** |
| 7 | 1522 | `light.color = theme.lampColor` | V5 H3 | yes | live |
| 8 | 1539–1545 | border lamp rule after the tier roll, behind `On && Borders` | V5 H4 + Codex gate | yes. No extra `Rand` call, so the other rolls stay put | off by default |
| 9 | 1556–1568 | `BorderThemes` | V5 H5 | yes | **MAP-3** (it calls `Cache.Edge`) |
| 10 | 2886–2889 | the rule mirrored in `LampModeOf` | asked for by V5 §4 | yes, but it breaks the predictor's purity | **MAP-3** |
| 11 | 3148–3158 | `BuildMaterials`: V5 colours, intensities, cool Office lens | V5 H2 | yes | live, **MAP-1** |

**How I checked the source match.** I diffed `proj_trans` → `proj_trans_lightlead` against `7320ed1` → HEAD, then compared the added and removed code lines with comments stripped (`SCR/mapaudit/c.txt`, `m.txt`).
- Every V5 code line is in main.
- Main has only 5 extra code lines: the `Borders` gate (×2), the `LampModeOf` mirror, `DressWindow` and its try/catch, and the `Ensure()` removal.

---

## 3. Checks

### 3.1 Design match (level transitions)

- **The V5 clone was a pre-render variant for Red's pick.** Evidence:
  - `20_variation_lightlead.md`: "Red decides: dead, soft or colour only".
  - `VISUAL_CHAT_TASKS.md:88` at `7320ed1`: "**Red confirms before anything is implemented.** … WAIT-RED for the pick".
  - No pick has been recorded since. The Frame and Neck reports (22:23–22:26) also say Red has not picked.
- **Red did ask Codex to merge.** Codex's session log shows two requests:
  - 18:36 PDT: "merge these into the main project", meaning the clone-verified items in `VISUAL_CHAT_TASKS.md:92`;
  - 19:18 PDT: "merge all the unmerged content".

  So the promotion was requested. The N1 pick was not made.
- **B0 has its own basis.** At 13:09 Red sent Codex a screenshot and asked for the cause. At 13:29 Codex diagnosed the corner-post z-fight: cut D, matching `level_transitions/images/cut_post_2.jpg`. The plan calls B0 "bugs, not looks", included in every variation (§2.3).
- **The `Borders` gate is Codex's.** Main runs "colour + lens only". That is option (c) in V5 §9, but it was never rendered: `v_lightlead_vs_before.jpg` has only BEFORE | LIGHT LEAD | SOFT.
- **All variation frames use the frozen clone base,** not main's look. For example, the clone's Level 0 lens is a flat 2.6 lens; main's is `Troffer_Lens` ×1.5 (`BuildMaterials`, L3118–3131).
- **The title already uses cool Office light.** `RoomStream.ProfileLightColor` gives Office rooms (0.93, 0.96, 1.00) (`FrontRoomsRoomStream.cs:1358`). So V5 makes map Office lamps match the title's Office rooms. Before Codex, map Office lamps were warm. This is a point for Red's pick.

### 3.2 Determinism

- **Transition files:** no `Random`, `Time`, `DateTime`, `GetHashCode`, `Dictionary` or `HashSet` (grep).
- **Flags** are read once per domain load from `Environment.GetCommandLineArgs()`.
- **`PostPieceAt` finish keys** use `Material.GetInstanceID()` (L1122–1123) only for equality inside one session. The result is the same in every session.
- **The border rule draws no extra random numbers** (L1536–1545). The roll is taken first, as before.
- **Same seed and same play path → same map.** This holds.
- **But B0.1 breaks the generator's neighbour rule.** The rule is in `FrontRoomsMap.cs:246–251`: "Everything that two chunks share (zones, heights, border edges) is a pure function of the seed … so neighbours agree no matter which is built first. A chunk's revision only reshuffles its own interior walls." A post on a chunk line now depends on the neighbour's interior walls. See **MAP-2**.

### 3.3 Streaming cost per chunk

- **B0.1 per non-open edge:** 2 `CornerReach` calls → 8 `PostPieceAt` calls. Each one does 1 `Cache.Edge`, 2 `ZoneOf`, 1 `StartAreaEdge`, 2 `HeightClass` and 2 Unity `==` material compares (L1106–1126, L1149–1156).
  - That is at most 128 edges × 8 = 1,024 calls per chunk. Before Codex there were none.
  - `PieceReach` allocates a `new long[2]` per present piece until it finds a mixed finish (`FrontRoomsTransitionKit.cs:83`). That is a few hundred short-lived arrays per chunk build: a small GC cost, more relevant on WebGL.
- **`Cache.Edge` generates missing chunks.** It calls `Get` → `Generator.Generate(coord, revision, Tier)` (`FrontRoomsMap.cs:751–755, 796–804`). So building chunk C now also generates the E, N, S, W, SE and NW neighbours' data (MAP-2).
- **Measured in the clone** (`15_var_lightlead.md` §3.5, static 25-chunk builds, no shifts):
  - +1.0 to +1.4 % triangles within 46 m;
  - renderers −1 to +14;
  - in-frustum renderers −5 to +1;
  - build 190–356 ms vs 189–317 ms per 25 chunks ("within noise").
- **The light lead itself** costs 0 triangles and 0 draws, and adds one 2.1 KB material.
- **The border rule** (off by default) adds 4 `Cache.Edge` calls per Auto lamp.

### 3.4 Command-line flags

| Flag | Effect | Who |
|---|---|---|
| (none) | B0 on; V5 colour + lens on; border rule off | Codex default |
| `-transitionsOff` | B0 off and V5 off. Builds the old boxes exactly (checked by reading `BuildEdge`: same sizes, centres, early-outs) | clone |
| `-b0Off` | B0 only off | clone |
| `-lightleadOff` | V5 off, B0 stays on | clone |
| `-lightleadBorders` | V5 as rendered (dead/dim border cells) | Codex |
| `-lightleadSoft` | implies Borders; Level 0 border cells dim instead of dead | clone + Codex |

Notes:
- **In Red's editor these only work through Unity's launch arguments.** There is no switch inside the editor.
- **On WebGL, `HasArg` returns false,** so B0 and V5 are always on there.
- **`-lightleadOff -lightleadBorders` does nothing:** `BorderMode` and both call sites check `On` first.

### 3.5 Window hunk vs the window contract

- **Main** calls `FrontRoomsInteractableKit.DressWindow(this, window, record)` inside `RaiseWindowBuilt` (L1300), after `BuildInto` has already emitted the jamb and head trims (L1211–1220).
  - The `bool` it returns ("true when a frame was hung, so the map leaves its own trims out", `FrontRoomsInteractableKit.Window.cs:50–52`) is ignored.
  - So the map's trims (jambs at x 0.700–0.770, proud to z ±0.100) stay inside the kit frame (lining x 0.6995, casing inner z 0.1005). The two surfaces sit 0.5 mm apart.
- **The window-landing r4 contract** (`SCR/win_r4/FrontRoomsMapWorld.window-kit.r4-all.diff.plain`, 22:15) replaces this. It:
  - defers the trims (`FrameTrims()`);
  - calls `DressWindowFrame` in `BuildEdge` and draws the trims only when no frame was hung;
  - logs a failure once.
- **The map binds the other visual classes by reflection.** `GlassBreakableType()` is at L1321–1327 and `ResolveDressers()` at L1775–1792. Codex's call is a compile-time binding, as are the two transition classes (MAP-5).

### 3.6 Nav, interaction and Relay sight

- **Nav and the Relay's paths** use `PassageBetween` → `Cache.Edge` kinds (L1424). They are unchanged: no edge kind changed.
- **Collision.** `Solid()` adds every render box to the chunk's collision mesh (L943–947), so B0 moves collision boxes too.
  - In one consistent build, the post square is covered exactly as before. I checked this with `b0_model.py` over all 3,840 post configurations: 0 gaps.
  - After a revisit shift, notches appear (MAP-2), but none goes through a wall: 0 see-through cases in the model. The Relay's `Visible()` raycast (`FrontRoomsMapHunter.cs:1038–1052`, all layers) is unchanged.
- **Windows.**
  - The kit frame spawns with `colliders: false` (`Window.cs:112`). Its FBX colliders are destroyed anyway (L116). The interim slab gets no collider (L175–186).
  - The pane cube keeps its collider and its `windowByCollider` entry (L1281). Only its renderer is hidden (L69).
  - So glass-hold interaction and Relay sight through windows are unchanged.
- **Lamp modes** are unchanged by default (`Borders` off). So the Relay `LampFx.Warn`, the phosphor ink gate and the lamp tests behave as before. The map chat confirms that the lamp tests pass on its rebased copy.
- **`GlassBreakOf` is pure** (L2537–2547). Calling it before the `WindowBuilt == null` return is safe.

### 3.7 Other checks: fine

- The Ensure() removal (hunk 2) is G14 contract C1, word for word. G14 now starts from `FrontRoomsPostStack.ConfigureCamera`.
- **Troffer lenses.** Before Codex, `FrontRoomsSurfaces.OfficeLouver` already returned `Troffer_Lens` (`FrontRoomsSurfaces.cs:45–48`). So the cool lens replaces the same prismatic lens, at the same luminance (2.100). No louver look is lost.
- **Level 0 lamps:** colour (1, .96, .88) and 5.0 are unchanged; V5's Level 0 values equal the old ones.
- **Red's `Editor.log`** (19:06–20:01): no exception from the map, transitions, `DressWindow` or `InteractableKit`. The only exceptions are the 253 ZBinningJob errors before 19:41 (G14 track).

### 3.8 Not verified (reasoned only; no finding)

- **Start area.** `PostPieceAt` mirrors `StartAreaEdge` but not `mayExtendStart/End`, which caps a piece's reach at 0.
  - At the door-line corners of the start area, an owner piece whose reach is capped can leave the outside 8 × 8 cm corner of the post empty.
  - This needs the cell west of the start area's top row to be Office, the cell north-west to be Level 0, the edge between those two cells to stand, and the edge north of the post to be open.
  - Whether the stream room's end wall hides it needs a capture. Owner: map chat, with MAP-2.

---

## 4. Findings

### MAP-1 · decision · V5 colour lead is on by default before Red's pick

- **What it does now:**
  - Office lamps: (0.90, 0.96, 1.00) at 6.0, was (1.00, 0.96, 0.88) at 5.5. In Tall rooms, 9.6 instead of 8.8.
  - Office lens: `Troffer_Lens_Cool`, emission (1.989, 2.121, 2.210), was (2.2, 2.1, 1.8).
- **Where:** `FrontRoomsTransitionLightLead.cs:21` (`On` unless `-transitionsOff` / `-lightleadOff`) and `MapWorld.cs:3148–3158`.
- **Why Red decides:**
  - Red's own rule was "Red confirms before anything is implemented" (`7320ed1:Documentation/VISUAL_CHAT_TASKS.md:88`).
  - The live combination (colour only, no border rule) was never rendered.
  - The renders used the frozen clone base, not main.
- **B0 is separate.** It answers the corner seam Red raised at 13:09, and it is in every variation. Keep it on, subject to MAP-2.
- **Options:**
  - (a) Keep V5 colour on. It matches the title's Office rooms, which are already (0.93, 0.96, 1.00).
  - (b) Make V5 opt-in until the pick. This is a visual-owned one-file patch with no map edit; it gives back exactly the pre-Codex lamps and lens:

```diff
--- a/Assets/Scripts/Rendering/Transitions/FrontRoomsTransitionLightLead.cs
+++ b/Assets/Scripts/Rendering/Transitions/FrontRoomsTransitionLightLead.cs
@@ -17,15 +17,15 @@
 /// </summary>
 public static class FrontRoomsTransitionLightLead
 {
-    /// <summary>The variation is on. Off with -transitionsOff or -lightleadOff (inert check).</summary>
-    public static bool On = !FrontRoomsTransitionKit.HasArg("-transitionsOff") && !FrontRoomsTransitionKit.HasArg("-lightleadOff");
-    /// <summary>lightlead_soft: Level 0 border cells dim instead of dead.</summary>
-    public static bool Soft = FrontRoomsTransitionKit.HasArg("-lightleadSoft");
     /// <summary>
-    /// The provisional N1 pick keeps V5's colour/lens lead on by default. The
-    /// stronger dead/dim border rule is opt-in for clone comparison or a
-    /// deliberate lightlead run.
+    /// The variation is on. Red has not picked an N1 variation yet, so it is opt-in: -lightleadOn
+    /// (or -lightleadBorders / -lightleadSoft) turns it on; -transitionsOff and -lightleadOff win.
     /// </summary>
+    public static bool On = (FrontRoomsTransitionKit.HasArg("-lightleadOn") || FrontRoomsTransitionKit.HasArg("-lightleadBorders") || FrontRoomsTransitionKit.HasArg("-lightleadSoft"))
+        && !FrontRoomsTransitionKit.HasArg("-transitionsOff") && !FrontRoomsTransitionKit.HasArg("-lightleadOff");
+    /// <summary>lightlead_soft: Level 0 border cells dim instead of dead.</summary>
+    public static bool Soft = FrontRoomsTransitionKit.HasArg("-lightleadSoft");
+    /// <summary>The dead/dim border rule (V5 as rendered); opt-in, for comparison.</summary>
     public static bool Borders = Soft || FrontRoomsTransitionKit.HasArg("-lightleadBorders");
```

- **Handoff:** level-transitions should re-render the pick over main, as `13_var_neck.md` §12 also asks.

### MAP-2 · major · B0.1 makes chunk-line wall ends depend on the neighbour's interior

- **Cause.** `PostPieceAt` (L1106–1126) calls `Cache.Edge` for all 4 cells round a post (L1114). At a post on a chunk line, two of those cells are in the next chunk.
  - So building C generates (`FrontRoomsMap.cs:751–755`) the E, N, S, W, SE and NW chunks' data at the current `Tier`.
  - C's wall ends are then cut for those chunks' interior walls (revision-dependent).
  - Before Codex, a chunk's walls came only from its own data and the analytic zones. That is the generator's stated rule (`FrontRoomsMap.cs:246–251`).
- **Consequence 1: notches after a revisit shift.** `Stream` shifts a chunk on re-entry after 30 s (`shiftAfterSeconds: 30` in `FrontRoomsLevel0.asset:46`; `Cache.Shift` L847). It rebuilds only that chunk. The still-built neighbour keeps ends cut for the old interior.
  - **Model result** (`SCR/mapaudit/b0_model.py`, a port of `CornerReach` + `PostPieceAt`, 4 finishes, all 3,840 configurations): 0 gaps and 0 mixed overlaps in one build.
  - When the neighbour's interior piece flips in a shift:
    - vertical chunk line: 19.7 % of cases leave a visible notch at a junction;
    - horizontal chunk line: 13.1 %;
    - in 26–28 % the mixed overlap (the z-fight B0 removes) comes back;
    - in 0 cases does the gap go through a wall.
  - **Example:** cells SW/SE/NW = Level 0 Standard and NE = Level 0 Low. The old pieces were N, W and E; the new pieces are N and W. Then N stays at −1 and W at 0, and the east half of the post (8 × 16 cm, full wall height) is empty.
  - The uniform enumeration overweights mixed posts. Only 22 % of real posts are mixed (V5 §3.4: 12.2 of 55 per chunk). So expect roughly one notch per few shifts. A two-visit capture is needed.
- **Consequence 2: earlier tiers.** Neighbour data generated one ring early keeps the tier of that moment. In Red's 19:5x run the tier rose at 26.9, 56.2, 97.5 and 118.9 s (`Editor.log` lines 50300–50468). So chunks in the ring beyond `buildRadius 2` can carry an older tier than the "tier of its time" rule intends.
- **Frame has the same dependency.** Frame's B0 also reads all four arms with `Cache.Edge` (`proj_trans_frame` `TransitionCornerAt`). There, though, collision stays as today, so its mismatch would be render-only.
- **Minimal fix (map contract).** Posts on a chunk line keep today's reach. This is deterministic and reads only the chunk's own cells. It gives up B0.1 on about 15 of 64 posts per chunk, where the old stripe stays:

```diff
--- a/Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs
+++ b/Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs
@@ -1095,6 +1095,11 @@
 
     void CornerReach(GridCoord k, int role, out int reachA, out int reachB)
     {
+        // A post on a chunk line keeps today's reach (+1): its four pieces come from two chunks, and
+        // a revisit shift of one would leave the other's ends cut for the old interior. Reading only
+        // this chunk's cells also keeps a build from generating its neighbours (and their tier) early.
+        var o = MapGrid.ChunkOrigin(MapGrid.ChunkOf(k));
+        if (k.x == o.x || k.y == o.y) { reachA = reachB = 1; return; }
         GridCoord sw = new GridCoord(k.x - 1, k.y - 1), se = new GridCoord(k.x, k.y - 1), nw = new GridCoord(k.x - 1, k.y), ne = k;
         postPieces[FrontRoomsTransitionKit.South] = PostPieceAt(sw, se, false);
         postPieces[FrontRoomsTransitionKit.North] = PostPieceAt(nw, ne, false);
```

- **Full fix (map chat's call).** Fill the chunk-line post square with quarters. Each quarter is built by the chunk that owns the cell it faces, and filled when one of its two arms stands. Each chunk can read its arms from its own data plus the border edges, which do not change between revisions (`chunk.west/south`). Combine this with the per-chunk post table that V5 §3.5 already asks for.

### MAP-3 · minor · With `-lightleadBorders` or `-lightleadSoft`, `LampModeOf` generates chunks

- **Contract:** `LampModeOf` is documented as "Pure … it never generates a chunk" (L2860–2866), and tested (`FrontRoomsMapInteractionTests.cs:1025–1030`: "LampModeOf a far cell generates no chunk").
- **What breaks it:** the mirror (L2887–2888) calls `BorderThemes` → `Cache.Edge` → `Cache.Get`, which generates the chunk, and its neighbours, at the current tier. So that test fails under those flags.
- **Default:** off, so this is not live.
- **Fix (map contract):** a non-generating lookup for the predictor. For a built lamp, its chunk and its neighbours were already generated by `BuildFixture`'s own lookup, so the predictor still matches built lamps.

```diff
--- a/Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs
+++ b/Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs
@@ -1556,17 +1556,28 @@
     readonly ZoneTheme[] borderThemes = new ZoneTheme[4];
     readonly EdgeKind[] borderKinds = new EdgeKind[4];
 
-    ZoneTheme[] BorderThemes(GridCoord cell, EdgeKind[] kinds)
+    // generate: false for the pure predictor (LampModeOf), which must never generate a chunk;
+    // an edge whose chunk is not generated then counts as Wall (no crossing), so the roll stands.
+    ZoneTheme[] BorderThemes(GridCoord cell, EdgeKind[] kinds, bool generate = true)
     {
         for (var k = 0; k < 4; k++)
         {
             var n = k == 0 ? new GridCoord(cell.x + 1, cell.y) : k == 1 ? new GridCoord(cell.x - 1, cell.y) : k == 2 ? new GridCoord(cell.x, cell.y + 1) : new GridCoord(cell.x, cell.y - 1);
             borderThemes[k] = Cache.ZoneOf(n).theme;
-            kinds[k] = InStartArea(n) || InStartArea(cell) ? EdgeKind.Wall : Cache.Edge(cell, n);
+            kinds[k] = InStartArea(n) || InStartArea(cell) ? EdgeKind.Wall : generate ? Cache.Edge(cell, n) : EdgeIfGenerated(cell, n);
         }
         return borderThemes;
     }
 
+    EdgeKind EdgeIfGenerated(GridCoord a, GridCoord b)
+    {
+        if (b.x < a.x || b.y < a.y) { var t = a; a = b; b = t; }
+        if (!Cache.TryGetGenerated(MapGrid.ChunkOf(a), out var chunk)) return EdgeKind.Wall;
+        var o = chunk.Origin;
+        var index = MapGrid.LocalIndex(a.x - o.x, a.y - o.y);
+        return b.x > a.x ? chunk.east[index] : chunk.north[index];
+    }
+
     /// <summary>
     /// Map lamps near the start area, held dark by the game while the stream
     /// rooms can be seen: a lamp with no shadow lights straight through the
@@ -2885,7 +2896,7 @@
         // Modes 0..4 (steady, stutter, failing, dead, dim) are ModuleLamp.Steady..Dim.
         var rolled = (tierRules ?? new FrontRoomsTierRules()).At(tier).LampMode(Rand(ref rng));
         if (FrontRoomsTransitionLightLead.On && FrontRoomsTransitionLightLead.Borders)
-            rolled = FrontRoomsTransitionLightLead.BorderMode(rolled, Cache.ZoneOf(cell).theme, BorderThemes(cell, borderKinds), borderKinds);
+            rolled = FrontRoomsTransitionLightLead.BorderMode(rolled, Cache.ZoneOf(cell).theme, BorderThemes(cell, borderKinds, false), borderKinds);
         return (ModuleLamp)(rolled + 1);
     }
```

### MAP-4 · minor · `RaiseWindowBuilt` calls `DressWindow` directly, not through the window contract

See §3.5.
- **Visible effect:** map trims sit 0.5 mm inside the kit frame on every map window. This is not seen as a fault in Red's log.
- **Other differences:**
  - every failure is logged, where the facade says "logs once";
  - the call is a compile-time binding;
  - the pane keeps `FrontRoomsMetalGlassTarget` (L1277) next to the slab's.
- **Gameplay:** unchanged (§3.6).
- **Fix:** the map chat applies window-landing's r4 contract in place of hunk 6. That means restoring the early return order is no longer needed, because r4 moves the call into `BuildEdge`. Handoff: window-landing.

### MAP-5 · minor · Main's transition kit is V5's; the other variants and B0 rules collide with it

- **Main's kit:** `FrontRoomsTransitionKit` (path, GUID `b215858f…`) is V5's 119-line kit. MapWorld uses its API directly: `B0`, `PostPiece`, `South..East`, `CornerReach(PostPiece[], …)` at L983, L1094–1126, L1149–1156.
- **Each other clone defines a different class with the same name:**
  - Frame: 1,016 lines, `B0On`, `TransitionCorner`, render-only quarter posts, collision as today;
  - Neck: 875 lines;
  - Renovation: 1,633 lines;
  - Drift: 130 lines.
- **What this means:**
  - Any pick other than V5 must rework hunks 3–5.
  - The map chat must choose one B0.1 rule. V5's per-skin rule moves collision boxes; Frame's does not.
  - The map's own convention binds visual classes by reflection (L1321–1327, L1775–1792). These hunks break it, and so does MAP-4.
- **Fix:**
  - Keep main's GUID and file, and merge into it; never copy a clone's kit over it. The Frame and Neck reports agree (`11_var_frame.md:298`, `13_var_neck.md` §12).
  - Choose the B0 rule with MAP-2 in mind.
  - Express the final hooks as a map contract.
- **Handoff:** level-transitions.

### MAP-6 · minor · Codex's N1 notes misstate the state

- **`VISUAL_CHAT_TASKS.md` N1 row:**
  - it says "PROMOTED; visual/runtime acceptance pending";
  - it drops "Red confirms before anything is implemented";
  - it does not say that Red has not picked, or that colour-only was never rendered.
- **Code comments:**
  - `FrontRoomsTransitionLightLead.cs:24–28` calls it "The provisional N1 pick";
  - the `// TRANSITION lightlead` hook markers, which plan §2.4 makes the contract's anchors, became `N1/V5` and `N1/B0` comments.
- **Fix:** restore the rule and the WAIT-RED status in the N1 row, and correct the comment. The map contract can quote this report's hunk table instead of the markers.

---

## 5. Verification images

This is a read-only code review. It made no renders or captures, so nothing goes to the Figma "FRONTROOMS · VISUAL VERIFICATION LOG". `codex_audit/runtime/` did not exist at 22:45.

Captures still needed, for the owning workflows:
1. MAP-2: a two-visit route over a mixed chunk-line junction (shift at 30 s), before and after.
2. MAP-1: the four fixed shots on **main** with V5 colour only vs `-lightleadOff` vs `-lightleadBorders`.
3. §3.8: the start-area door-line corners on an Office-west seed.

## 6. Handoffs

| Item | To |
|---|---|
| MAP-1 pick (V5 colour on or opt-in) | Red; then level-transitions re-renders over main |
| MAP-2, MAP-3 patches; one B0 rule | map chat (关卡设计) as contracts; level-transitions for the B0 choice |
| MAP-4 | window-landing (r4 contract, already written) → map chat |
| MAP-5 | level-transitions merge stages (Frame, Neck, Renovation, Drift) |
| MAP-6 | docs (visual chat) |
