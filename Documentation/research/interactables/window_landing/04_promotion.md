# 04 — Window landing: promotion list and order (r4)

Status: r4, 2026-10-03 22:45. Nothing here is promoted yet. It lands only after Red confirms the doors + windows proposal (`05_for_red.md`). Then each owner lands its own items.

This replaces `01` §6 and `02` §7.

---

## 0. What main already holds (Codex, 19:10–19:41, without Red's review)

The window landing is **already live** in Red's project. Codex copied our unfinished files into main. The codex audit (`research/codex_audit/00_main_state.md` §3.4) and our runs agree on the details:

| In main (`75cfdff`) | Where it came from | State |
|---|---|---|
| `Assets/Scripts/Office/FrontRoomsInteractableKit.Window.cs` (+ meta, guid `fe28a88a…`) | our r3 file from the fix stage (17:41), 2 header lines rewritten by Codex | compiles; it was never tested before Codex took it. **Tested now, on main: PASS** (`03` §0) |
| The direct call `FrontRoomsInteractableKit.DressWindow(this, window, record)` in `FrontRoomsMapWorld.RaiseWindowBuilt` | Codex (not the map chat, not us) | works; the map chat accepts the call site. It logs one error per failing window and ignores the return value (`03` §2) |
| `Kit_WindowFrame_Wood`, `_Steel`, `_Steel_Enamel`, `_Alu`, `Kit_MiniBlind_Raised`, `_Lowered` FBX + JSON + metas | G4's 16:45 builds (byte-identical FBX); Codex changed `"placement"` to `"Wall"` in the 6 JSONs | the frames are live. G4 is still iterating (`interact_window_*.py` changed again at 22:04–22:06) |
| Glass G1/G3 (`FrontRooms/Glass`, `Glass_Window`, `Glass_Edge`, `Glass_Shard`, grime textures) | glass track | live; the facade's interim slab uses `Glass_Window` |
| `FrontRoomsKitImporter.cs`: authored `lodDistances` → LOD heights | Codex | **bug** (§2 item 2): the cull takes d12, so kit frames would vanish past 12 m at the next reimport |

**So the order below is not "add the windows". It is "correct what is live, then finish it".**

---

## 1. Rules for every item

- Main is the base. Each item is a diff over main's file. Keep main's `.meta` GUID for every path main already has; never a second GUID for one path.
- Re-read main and `research/codex_audit/20_findings.md` right before landing. If the audit already landed the same fix (it found the importer bug too), drop ours.
- Never promote the clone harnesses: `Assets/Editor/Audit/FrontRoomsWindowTF.cs`, `FrontRoomsWindowLandingPlay.cs`, `FrontRoomsWindowAutopilotProbe.cs`, `Assets/Scripts/Audit/WindowProbeBehaviour.cs`. Copies are here as `.txt`.

## 2. The list, in landing order

| # | Item | Owner | Depends on | Files (here) |
|---|---|---|---|---|
| 1 | **Facade r4** over main's copy: header text for the r4 call site; on a failure it also turns the pane renderer back on; a tools switch `DisableWindowDressForTools` (the look before the kit, for tests and as a kill switch). Keep guid `fe28a88a…` | visual chat | — | `FrontRoomsInteractableKit.Window.r4.diff` (80 lines), full file `FrontRoomsInteractableKit.Window.r4.cs.txt` |
| 2 | **Importer LOD fix** (`Assets/Editor/Rendering/FrontRoomsKitImporter.cs`): the last LOD level takes the sidecar's LAST entry (dcull; −1, 0 or null = never). `GetVersion() => 2` so every kit FBX re-imports once with the right values | visual chat (or codex-audit, whichever lands first) | — | `FrontRoomsKitImporter.lodcull.diff` (32 lines) |
| 3 | **Map contract R1 + R2** (`FrontRoomsMapWorld.cs`) | **map chat** | 1 | `FrontRoomsMapWorld.window-kit.r4.diff`; `apply_window_kit_r4.py.txt` re-applies by content |
| 4 | **T (optional): no map trims on framed windows** | map chat, after Red says yes | 2 and 3 | `FrontRoomsMapWorld.window-kit.r4-trims.diff` (on top of 3), or `…r4-all.diff` (3 + 4 in one) |
| 5 | **G4's final frames** (FBX + JSON) when the interactables workflow finishes: keep main's metas; keep `"placement": "Wall"`; if T is in, G4 may drop the three trim-hiding rules (reveal, ranch slope, open mitre; `05` §3) | G4 / visual chat | 4 for the relaxed profile | from `proj_int` |
| 6 | `Prop_Putty` surface (W-L0 glazing) | visual chat | — | none yet. The facade already swaps the glazing slot to `Resources/Surfaces/Prop_Putty` when it exists; nothing to change in code |
| — | `Kit_MiniBlind_Raised` / `_Lowered` are in main but **not placed** | Red (proposal call 7) | — | — |

**Why 2 before 4.** With T, a frame is the only thing round the opening. With main's importer, a re-imported frame is culled at d12 = 12 m (24 m on Ultra, lodBias 2), well inside the 46 m sight distance (`SightDistance = 2 × 24 − 2`). The window would then show a bare cut in the wall. Without T the map's trims stay under the frame, so a culled frame only pops to the old trims.

**Measured on the importer (clone, after the fix):** see the table below.

<!-- LOD-RESULTS -->

## 3. The glass dependency

- The facade needs only `Resources/Surfaces/Glass_Window` (glass track G1/G3), which is already in main. Without it the interim slab falls back to the map's own pane material.
- The interim slab exists only while GD3's `FrontRoomsGlassBreakable` does not. When GD3 lands, the map hides the pane and the breakable owns the visible glass on `Window.root`; the facade then makes no slab (tested: the facade checks the pane renderer's state after the map's breakable hook).
- G14 (ray tracing): the slab is traced as glass by its material, not by the hint. Two G14 items found by the codex audit stay with G14: main's `FrontRooms/Glass` has no `FRGlassRTPrepass` pass, so nothing is traced on window glass yet; and its harnesses read `window.pane`'s renderer, which the kit hides (`03` §3).
- G14 already does two things our art review asked for: it registers LOD0 only (`InLod0`, `GlassRT/FrontRoomsGlassRTSystem.cs:712`) and all submeshes, and it reads `_BaseMap` textures for FrontRooms/Surface materials (`:488-512`). So the frames will not reflect as white or doubled. One RT frame per member is still owed before G14's acceptance.

## 4. Cost and the map's §8 acceptance

<!-- PERF-RESULTS -->

## 5. WebGL

The WebGL build is a separate, reduced tier. Desktop (Editor, Mac, Windows) is never changed for it.

- In a WebGL player only (`#if UNITY_WEBGL && !UNITY_EDITOR`, facade `WindowParts.WebGLTier`), every frame is forced to LOD1 (W-L0 1,044 tris, W-OF 1,438) and casts no shadow. Same materials, same section, same render-only rules.
- Per window that is 1 renderer with 2 submeshes. The interim slab replaces the pane cube one for one, with no shadow.
- Not built or measured in a WebGL player here. The WebGL track should count draws on its own harness before release.
