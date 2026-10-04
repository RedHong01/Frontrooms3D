# 04 — Window landing: promotion list and order (r5)

Status: r5, 2026-10-04 08:50. Nothing here is promoted. It lands only after Red confirms the doors + windows proposal (`05_for_red.md`). Then each owner lands its own items.

This replaces r4 (yesterday 22:45), `01` §6 and `02` §7.

---

## 0. What main already holds (Codex, 2026-10-03 19:10–19:41, without Red's review)

The window landing is **already live** in Red's project. Codex copied our unfinished files into main. The codex audit (`research/codex_audit/00_main_state.md` §3.4, `10_review_kits.md` §3) and our runs agree:

| In main (`1385738`) | Where it came from | State |
|---|---|---|
| `Assets/Scripts/Office/FrontRoomsInteractableKit.Window.cs` (meta guid `fe28a88ad796…`) | our r3 file from the fix stage (17:41), 2 header lines rewritten by Codex | compiles; never tested before Codex took it. **Tested now on main: PASS** (`03` §0) |
| The direct call `FrontRoomsInteractableKit.DressWindow(this, window, record)` in `FrontRoomsMapWorld.RaiseWindowBuilt` (lines 1296–1301) | Codex (not the map chat, not us) | works; the map chat accepts the call site. It logs one error per failing window and ignores the return value, so the map's trims stay under every frame (`03` §2, §4) |
| `Kit_WindowFrame_Wood`, `_Steel`, `_Steel_Enamel`, `_Alu`, `Kit_MiniBlind_Raised`, `_Lowered` FBX + JSON + metas | G4's 16:45 builds; Codex changed `"placement"` to `"Wall"` in the 6 JSONs | the frames are live. G4 pass 3 (22:0x–23:xx) has newer files in `proj_int` (§2 item 6) |
| Glass G1/G3 (`FrontRooms/Glass`, `Glass_Window`, `Glass_Edge`, `Glass_Shard`, grime textures) | glass track | live; the facade's interim slab uses `Glass_Window` |
| `Assets/Editor/Rendering/FrontRoomsKitImporter.cs`: sidecar `lodDistances` → LOD heights | Codex | **bug**: the last LOD takes d12, so every kit frame is culled at 12 m at its next import (§2 item 2) |

**So the order below is not "add the windows". It is "correct what is live, then finish it".**

---

## 1. Rules for every item

- Main is the base. Each item is a diff over main's file. Keep main's `.meta` GUID for every path main already has; never a second GUID for one path.
- Re-read main and `research/codex_audit/20_findings.md` right before landing. At 08:50 today `20_findings.md` did not exist yet; the audit's `10_review_*.md` files list the same items as below. If the audit already landed a fix (it has the importer fix ready, item 2), drop ours.
- Never promote the clone harnesses: `Assets/Editor/Audit/FrontRoomsWindowTF.cs`, `FrontRoomsWindowLandingPlay.cs`, `FrontRoomsWindowAutopilotProbe.cs`, `Assets/Scripts/Audit/WindowProbeBehaviour.cs`. Copies are here as `.txt`.

## 2. The list, in landing order

| # | Item | Owner | Depends on | Files (here) |
|---|---|---|---|---|
| 1 | **Facade r4** over main's copy: header text for the r4 call site; on a failure it also turns the pane renderer back on; a tools switch `DisableWindowDressForTools` (the look before the kit, for tests and as a kill switch). Keep guid `fe28a88ad796546fab9679489bceb49c` | visual chat | — | `FrontRoomsInteractableKit.Window.r4.diff` (80 lines); full file `FrontRoomsInteractableKit.Window.r4.cs.txt` |
| 2 | **Importer LOD fix** (`Assets/Editor/Rendering/FrontRoomsKitImporter.cs`, guid `4add03be…`): the last LOD level takes the sidecar's LAST entry (dcull; −1, 0 or null = never). Plus `GetVersion() => 2`, so every model re-imports once with the right values | visual chat **or** codex-audit, whichever lands first. The audit wrote the same logic at 08:21 today (`research/codex_audit/F1_kit_importer_lod.diff`) without the version line. Land one of the two, then add the version line if it is missing | — | `FrontRoomsKitImporter.lodcull.diff` (32 lines) |
| 3 | **`Prop_Putty` surface** (W-L0 glazing line): one `SurfaceDef` line in `FrontRoomsRenderSetup.cs` + `Resources/Surfaces/Prop_Putty.mat` (+ meta, new path, new guid `ac2f59d6f3ec42dabdfa8f9bbd440b6e`). The facade already swaps it in on the walnut member only | visual chat, after Red's look call (`05` §2.3) | — | `FrontRoomsRenderSetup.putty.diff` (12 lines), `Prop_Putty.mat.txt`, `Prop_Putty.mat.meta.txt` |
| 4 | **Map contract R1 + R2** (`FrontRoomsMapWorld.cs`) | **map chat** | 1 | `FrontRoomsMapWorld.window-kit.r4.diff`; `apply_window_kit_r4.py.txt` |
| 5 | **T (optional): no map trims on framed windows**, plus the `LEVEL_MODULE_SPEC` "Window" row text (`03` §4) | map chat, after Red says yes | 2 and 4 | `FrontRoomsMapWorld.window-kit.r4-trims.diff` (on top of 4), or `…r4-all.diff` (4 + 5 in one) |
| 6 | **G4 pass-3 frames** (`build_g4_…md` §0.4): Steel and Steel_Enamel FBX (screw slots 1.1 mm), Alu FBX (no LOD1, wear stations, 2,066 tris), all 6 JSONs (`wall_decor` tag → `"placement": "Wall"` from the tool, not a hand patch; `lodDistances` `0.0` instead of `null`). Keep main's metas (wood fbx `1cdcbda9…`, steel `c96f7893…`, enamel `195d766c…`, alu `935afd3a…`, blinds `1202dd8d…` / `75e18a03…`) | interactables workflow (G4) | 2 (so the re-import gets the right LODs) | from `proj_int/Assets/Resources/Props/Models/` |
| 7 | **G4 profile pass after T** (art review A3, A9: the wood reveal, ranch slope, open mitre, liner UVs, mitred apron returns, end grain) | G4 | 5 | not built yet (`05` §3) |
| — | `Kit_MiniBlind_Raised` / `_Lowered` are in main but **not placed** | Red (proposal call 7) | — | — |

**Why 2 before 5 and 6.**
- With main's importer, a re-imported frame is culled at d12 = 12 m (24 m on Ultra, lodBias 2). Measured in `proj_win_base` with main's importer: wood and steel LOD1 at 4.0 m, **cull 12.0 m**; door frame the same; blind cull 10 m (`tf_logs/r5/proj_win_base_lod_probe_main_importer.txt`).
- After the fix (`proj_win`, `-lodReimport`): wood, steel, enamel and the wood door frame switch to LOD1 at **4.0 m** and are **never culled**; the blind switches at 3.0 m and culls at **30 m**, as `10_spec` §9 says (`tf_logs/r5/proj_win_lod_probe.txt`).
- Red's Library still holds the 18:55 import, made before Codex's importer change: LOD1 at 11.7 m, cull at 58.5 m. Any re-import (item 6, a Library rebuild, another machine) applies the bug. So item 2 must land before item 6.
- The sight distance is 46 m at the shipped `buildRadius` 2 (`SightDistance = 2 × 24 − 2`, `FrontRoomsMapWorld.cs:69`). The old 58.5 m cull was therefore never visible; the old "frameless glass in long halls" risk in `02` §6 item 2 was overstated. The real cost item was the late LOD switch (11.7 m instead of 4 m).

**Promotion order with the doors (art review A14, codex audit kits F3).** The windows went live before the G1 door frames. In play, the windows are the only openings with real walnut casings and a stool; doors in the same halls keep the map's flat trim boxes. Two options for Red:
- land items 1–6 in the same batch as the G1 door frames; or
- record the interim mismatch as accepted in the doors + windows Figma proposal.

## 3. The glass dependency

- The facade needs only `Resources/Surfaces/Glass_Window` (glass track G1/G3), which is already in main. Without it the interim slab falls back to the map's own pane material.
- The interim slab exists only while GD3's `FrontRoomsGlassBreakable` does not. When GD3 lands, the map hides the pane and the breakable owns the visible glass on `Window.root`; the facade then makes no slab (tested: the facade reads the pane renderer's state after the map's breakable hook).
- **G14 (ray tracing).** Verified in main's `GlassRT/FrontRoomsGlassRTSystem.cs`:
  - it registers only renderers in LOD0 (`InLod0`, line 712, called at 665) and skips disabled ones (line 1011), so the frames will not double;
  - it walks every submesh (line 272) and reads `_BaseMap` for textured materials (lines 491–525), so the walnut and steel will not reflect as white (`_BaseColor` is 1,1,1 on both slots).
  - Still owed by G14 (codex audit glass-look F5): main's `FrontRooms/Glass` has no `FRGlassRTPrepass` pass, so **nothing is traced on window glass yet**. When that lands, G14 should check one RT frame of each member (W-L0, W-OF) before its acceptance, and switch its harnesses from `window.pane`'s renderer to the visible one (`03` §3).

## 4. Cost and the map's §8 acceptance

All in `proj_win` (R1 + R2 + T, importer fix, putty) unless named. Load = `uptime` 1-minute average.

| Item | Before the kit | With the kit | Load | Note |
|---|---|---|---|---|
| Autopilot 2554: avg fps / p99 | 59.0 / 17.0 ms | **59.0 / 17.3 ms** | 6–8 | bar ≥ 55 fps, p99 ≤ 33 ms: PASS |
| Autopilot 20388: avg fps / p99 | 58.6 / 19.0 ms | **58.9 / 19.8 ms** | 8–25 | PASS |
| Chunk build in Play, median / p90 / max | 5.8 / 6.7 / 16.8 ms (2554) | 5.8 / 8.9 / 21.4 ms (2554) | 6–8 | no frame > 50 ms with map work in either kit run |
| Chunk build (edit mode, 7 runs) | 5.51 / 6.70 / 5.59 ms | 5.83 / 6.88 / 5.77 ms | 18–22 | +0.2–0.3 ms per chunk with 7–9 windows |
| `DressWindow` per window | — | 0.017 ms (frame only), 0.034 ms (+ slab) | 18–22 | — |
| Per-room LOD0 tris, worst (10 seeds) | — | Office 85,556 / 120,000; other 47,126 / 60,000 | — | §8 budgets: PASS |
| Static content per chunk with windows (74 of 250 chunks) | — | median +5 frames, +2 renderers, +7 submeshes, +12,970 LOD0 tris; −4 shadow renderers (T removes the trims' shadows) | — | worst chunk: 12 windows, 5,160 → 38,752 tris |
| One view, Office 3 m (`OF_A_front_3m`) | 1,453 submeshes, 51.5 k tris | 1,496 submeshes, 92.0 k tris (1 frame LOD0, 43 LOD1) | — | the frames behind walls in the frustum still draw (no occlusion culling) |

Per window: W-L0 2,322 tris LOD0 / 1,044 LOD1; W-OF 3,862 / 1,438; 1 renderer, 2 submeshes, casts shadows, no light, no collider.

## 5. WebGL

The WebGL build is a separate, reduced tier. Desktop (Editor, Mac, Windows) is never changed for it.

- In a WebGL player only (`#if UNITY_WEBGL && !UNITY_EDITOR`, facade `WindowParts.WebGLTier`), every frame is forced to LOD1 (W-L0 1,044 tris, W-OF 1,438) and casts no shadow. Same materials, same section, same render-only rules.
- Per frame in view that is 1 renderer with 2 submeshes (about 2 draws). The map's trims were part of the merged chunk trim mesh, so they cost no extra draw. In the Office view above, 44 frames are in the frustum, which would be about +88 draws over the WebGL build's ~2,300.
- Not built or measured in a WebGL player here. The WebGL track should count draws on its own harness before release. If it is over budget, the WebGL-only option is a WebGL cull distance for the frames **without T on WebGL** (the map's trims are then the far fallback). That needs a `#if UNITY_WEBGL` in the map's T hunk, so it would be a second contract request.

## 6. Hand-offs from the art review (no code from us)

Each item has an owner and a test. Details and frames: `05_for_red.md` §3.

| # | Item | Owner | Test before it lands |
|---|---|---|---|
| A1 | Intact glass does not read as glass (intact vs broken at 1.5 m: 3.5–5.3 luma; today's cube 10.4–36.6) | visual chat (URP asset, desktop only) + glass track | ≥ 12 luma at 1.5 m; one troffer-shaped reflection at 1.5 m front and 45°; not milkier |
| A2 | Round bloomed lamp highlights on the glass | glass track | no round disc at 0.3 m (`OF_B_close_head`) |
| A3 | Walnut casing and liner read as one block | G4 (after T) | reveal visible at 0.3 m and 1.5 m |
| A4 | Pressed steel reads as matte black plastic | visual chat (`Prop_SteelBrown`, shared with the key door frame) | door readability harness (`interactables/05`), then the 0.3 m screw frame without the inspection light |
| A5 | Wear authored in Blender is invisible (`FrontRooms/Surface` ignores vertex colour) | visual chat, **Red approves W2** | 0.3 m sill, horn and screw frames |
| A6 | Frames sit on the wall like stickers (−2 to −12 % under the stool) | visual chat, desktop only | ≥ 15 % darker in the first 1 cm under the stool at 1.5 m |
| A7 | Shadow staircase on the steel stops | visual chat (lamp look) + map chat (lamp tick) | cause confirmed today (`05` §3.7) |
| A8 | Glass grime reads as aquarium dirt | glass track | 15–20 mm prints, band ≤ 0.3 |
| A9 | Apron ends square-cut; long grain on end faces | G4 | mitred returns at 0.3 m |
| A10 | Black rubber glazing line on walnut | visual chat | **fixed in the clone** (item 3); Red's look call |
| A11 | Decorative groove in the W-OF stop | G4 + map chat (S2, `03` §5) | — |
| A12 | Walnut grain contrast too low beyond 1 m | visual chat (shared with `Kit_DoorFrame_Wood`) | readability values (`Prop_WoodWalnut` luminance 0.036) |
| A13 | RT forward-compatibility | G14 | done in G14's code (§3); one RT frame per member owed |
| A14 | Windows live before the doors | Red | §2 |
