> **STATUS 2026-10-07 19:4x: item 1 (facade r6) MERGED into main.** Red approved it, the session 视觉材料未同步到主场景 applied it, and MANIFEST OK. `FrontRoomsInteractableKit.Window.cs` went e1472c6d → 07dc067e, GUID fe28a88a kept; backup in `W/backup/2026-10-07_window_landing`.
> - Not merged: Prop_Putty (waits for Red to see its look) and MapWorld R1/R2 (map contract, base 9abacc6f unchanged).
> - A re-run of the apply script reports "already applied"; do not re-record bases.

# 04 — Window landing: promotion list and order (r6)

Status: r6, 2026-10-07 17:40. Nothing here is promoted by this workflow. It lands only after Red confirms the doors + windows proposal (Figma 2748:6099, rebuilt 2026-10-07 after 2497:3804 was deleted; `05_for_red.md`). Then each owner lands its own items.

This replaces r5 (2026-10-04 08:50, with the visual chat's 09:40 note), `01` §6 and `02` §7.

---

## 0. What main already holds

Main HEAD is `279c144` (2026-10-04 21:30); the window files below have not changed since r5 except where marked.

| In main | Where it came from | State |
|---|---|---|
| `Assets/Scripts/Office/FrontRoomsInteractableKit.Window.cs` (meta guid `fe28a88ad796546fab9679489bceb49c`, sha256 `cafa48bd…`) | our r3 file from the 2026-10-03 fix stage (17:41), 2 header lines rewritten by Codex (`daef6c2`) | live; works. Tested again today on main: PASS (`03` §0). Its header still describes r3 (codex audit DOC-D4) |
| The direct call `FrontRoomsInteractableKit.DressWindow(this, window, record)` in `FrontRoomsMapWorld.RaiseWindowBuilt` (now lines 1342–1343) | Codex (`8ef5b64`), not the map chat, not us | works; the map chat accepts the call site. It logs one error per failing window and ignores the return value, so the map's trims stay under every frame (`03` §2, §4) |
| `Kit_WindowFrame_Wood`, `_Steel`, `_Steel_Enamel`, `_Alu`, `Kit_MiniBlind_Raised`, `_Lowered` FBX + JSON + metas | G4's 2026-10-03 16:45 builds; Codex set `"placement": "Wall"` in the 6 JSONs | the frames are live. **Main's G4 modules are newer** (`interact_window_*.py`, 2026-10-04 08:09–08:11, in `b44e1c5`): the FBX in main were not rebuilt from them (§2 item 6) |
| Glass G1/G3 (`FrontRooms/Glass`, `Glass_Window`, `Glass_Edge`, `Glass_Shard`, grime textures) | glass track | live; the facade's interim slab uses `Glass_Window` |
| **G14's `FRGlassRTPrepass` pass in `FrontRooms/Glass`** | glass-rt-track (`9eddc35`, `bf2e883`, `70644f0`, 2026-10-04) | **new since r5.** `Glass_Window` (`_RTReceive` 1) is now traced in Play on Mac. So the kit's slab turns G14 on for every intact window (`03` §1, §6.2) |
| `Assets/Editor/Rendering/FrontRoomsKitImporter.cs` LOD mapping | Codex's change, fixed by the visual chat in `663e858` + `1114d6b` (2026-10-04 09:40) | **fixed in main** (codex audit F1, 2/2 votes, resolved): the last LOD takes the sidecar's cull entry, `GetVersion() => 2`. Same logic as our retired `FrontRoomsKitImporter.lodcull.diff` |

**So the order below is not "add the windows". It is "correct what is live, then finish it".**

---

## 1. Rules for every item

- Main is the base. Each item is a diff over main's file. Keep main's `.meta` GUID for every path main already has; never a second GUID for one path.
- Re-read main and `research/codex_audit/` right before landing. Today `20_findings.md` does not exist; the audit's confirmed findings that name this workflow are F1 (importer: fixed in main), DOC-D2 and DOC-D4 (docs, below). F2 / MAP-4 (the trims under the frame) and F3 (windows before doors) were kept at 1 / 2 votes; they are T and Red's call A14 here.
- Never promote the clone harnesses: `Assets/Editor/Audit/FrontRoomsWindowTF.cs`, `FrontRoomsWindowLandingPlay.cs`, `FrontRoomsWindowAutopilotProbe.cs`, `Assets/Scripts/Audit/WindowProbeBehaviour.cs`. Copies are here as `.txt`.
- iCloud conflict copies (`* 2.*`) are never copied or deleted by a landing.

## 2. The list, in landing order

| # | Item | Owner | Depends on | Files (here) |
|---|---|---|---|---|
| 1 | **Facade r6** over main's copy: a header that says what main does and what the contract does (DOC-D4); on a failure it also turns the pane renderer back on; a tools switch `DisableWindowDressForTools` (the look before the kit, for tests and as a kill switch). Keep guid `fe28a88ad796546fab9679489bceb49c` | visual chat | — | `FrontRoomsInteractableKit.Window.r6.diff` (diff over main's sha `cafa48bd…`); full file `FrontRoomsInteractableKit.Window.r6.cs.txt` |
| 2 | ~~Importer LOD fix~~ | — | — | **done in main** (`663e858`). Measured today in `proj_win` (main's importer): wood, steel and enamel frames LOD1 at 4.0 m, never culled; blind 3.0 m / 30 m (`tf_logs/r6/lod_probe.txt`) |
| 3 | **`Prop_Putty` surface** (W-L0 glazing line): one `SurfaceDef` line in `FrontRoomsRenderSetup.cs` (guid `aaa9a62f…`) + `Resources/Surfaces/Prop_Putty.mat` (+ meta, a new path, new guid `ac2f59d6f3ec42dabdfa8f9bbd440b6e`, checked unique in main today). The facade already swaps it in on the walnut member only; main's `interact_window_frame_wood.py` docstring already expects it | visual chat, after Red's look call (`05` §2.3) | — | `FrontRoomsRenderSetup.putty.r6.diff` (12 lines), `Prop_Putty.mat.txt`, `Prop_Putty.mat.meta.txt` |
| 4 | **Map contract R1 + R2** (`FrontRoomsMapWorld.cs`, base sha `a56fc9e1…`) | **map chat** | 1 | `FrontRoomsMapWorld.window-kit.r6.diff`; `apply_window_kit_r4.py.txt` |
| 5 | **T (optional): no map trims on framed windows**, plus the `LEVEL_MODULE_SPEC` "Window" row text (`03` §4) | map chat, after Red says yes | 4 (and the importer fix, now in main) | `FrontRoomsMapWorld.window-kit.r6-trims.diff` (on top of 4), or `…r6-all.diff` (4 + 5 in one) |
| 6 | **G4 rebuild of the window FBX** from main's own modules (2026-10-04 08:09–08:11, `b44e1c5`: the steel/alu LOD1 strategy of pass 4, screw slots, the putty note). The 2026-10-03 `proj_int` builds are gone (the 2026-10-05 wipe). Keep main's metas: wood fbx `1cdcbda9…`, steel `c96f7893…`, enamel `195d766c…`, alu `935afd3a…`, blinds `1202dd8d…` / `75e18a03…`. Re-run `FrontRoomsWindowTF.RunAll` on the rebuild (about 4 min on a quiet machine) | interactables workflow (G4) | — | built into a clone, never straight into main |
| 7 | **G4 profile pass after T** (art review A3, A9, A11: the wood reveal, ranch slope, open mitre, liner UVs, mitred apron returns, end grain; drop the W-OF screw groove if the map chat grants S2) | G4 | 5 (and S2 for A11) | not built yet (`05` §3) |
| — | `Kit_MiniBlind_Raised` / `_Lowered` are in main but **not placed** | Red (proposal call 7) | — | — |

**Promotion order with the doors (art review A14, codex audit F3).** The windows went live before the G1 door frames. In play, the windows are the only openings with real walnut casings and a stool; doors in the same halls keep the map's flat trim boxes (`images/r6_L0_hall_tall_5.0m_pair.jpg`). Two options for Red (`05` §2.4):
- keep the windows live, record the interim mismatch as accepted in the doors + windows proposal, and land items 1–7 in the same batch as the G1 door frames (recommended); or
- switch the window dress off until the doors land. With item 1 that is one line (`DisableWindowDressForTools = true` as a default); it brings back the trims and the 30 mm milky cube, and with it G14 has no window glass to trace.

## 3. The glass dependency

- The facade needs only `Resources/Surfaces/Glass_Window` (glass track G1/G3), which is in main. Without it the interim slab falls back to the map's own pane material.
- The interim slab exists only while GD3's `FrontRoomsGlassBreakable` does not (still absent in main today). When GD3 lands, the map hides the pane and the breakable owns the visible glass on `Window.root`; the facade then makes no slab (tested: the facade reads the pane renderer's state after the map's breakable hook).
- **G14 (ray tracing).** Verified today in main's `GlassRT/FrontRoomsGlassRTSystem.cs`:
  - it registers only renderers in LOD0 (`InLod0`, line 712, called at 665) and skips disabled ones (line 1011), so the frames do not double (critic E8, A13);
  - it uploads every triangle submesh (`Upload`, lines 268–275) and builds one material block for all slots (`MaterialBlock(mats, …)`, line 679), reading `_BaseMap` for textured materials (lines 491–526), so the walnut and steel will not reflect as white (`_BaseColor` is 1, 1, 1 on both slots);
  - the prepass is merged, so window glass **can** be traced now. **Still owed by G14:** (a) one RT frame of each member (W-L0, W-OF) against RT off, before its acceptance; our Play frames today show no measurable change from 2026-10-04 (intact vs broken 2.9 / 2.8 luma with T, 3.2 / 2.8 without; then 2.9 / 2.7; `05` §3 A1, VL107), although G14 does trace window glass now (codex audit VL116: 3.5 % of a 1.5 m view changes by ≥ 8/255): the RT reflection is too faint to make the pane read; (b) switch its harnesses from `window.pane`'s renderer to the visible one (`03` §3).

## 4. Cost and the map's §8 acceptance

All in `proj_win` (R1 + R2 + T, importer fix, putty) unless named. Load = `uptime` 1-minute average.

| Item | Before the kit | With the kit | Load | Note |
|---|---|---|---|---|
| Autopilot 2554: avg fps / p99 | 59.0 / 17.0 ms | **59.0 / 17.3 ms** | 6–8 | 2026-10-04, main `1385738`, before G14's prepass. **Re-run owed** (`03` §6.2) |
| G14 main thread with a window in view (codex audit VL117, main `279c144`) | 0.14 ms p50 (no window glass in view) | p50 2.76 ms, p99 8.03 ms per traced frame | 500–730 | the kit's `Glass_Window` slab is what G14 traces; bar 0.3 ms p99. G14's budget item, but it lands with the windows |
| Autopilot 20388: avg fps / p99 | 58.6 / 19.0 ms | **58.9 / 19.8 ms** | 8–25 | same |
| Chunk build in Play, median / p90 / max | 5.8 / 6.7 / 16.8 ms (2554) | 5.8 / 8.9 / 21.4 ms (2554) | 6–8 | no frame > 50 ms with map work in either kit run |
| Chunk build (edit mode, 7 runs) | 5.51 / 6.70 / 5.59 ms | 5.83 / 6.88 / 5.77 ms | 18–22 | +0.2–0.3 ms per chunk with 7–9 windows |
| `DressWindow` per window | — | 0.017 ms (frame only), 0.034 ms (+ slab) | 18–22 | — |
| Per-room LOD0 tris, worst (10 seeds) | — | Office 85,556 / 120,000; other 47,126 / 60,000 | — | §8 budgets: PASS. **Same today** |
| Static content per chunk with windows (74 of 250 chunks) | — | median +5 frames, +2 renderers, +7 submeshes, +12,970 LOD0 tris; −4 shadow renderers (T removes the trims' shadows) | — | worst chunk: 12 windows, 5,160 → 38,752 tris |
| One view, Office 3 m (`OF_A_front_3m`) | 1,453 submeshes, 51.5 k tris | 1,496 submeshes, 92.0 k tris (1 frame LOD0, 43 LOD1) | — | the frames behind walls in the frustum still draw (no occlusion culling) |

Today's timings were taken at load 280–860 and are not used. Per window: W-L0 2,322 tris LOD0 / 1,044 LOD1; W-OF 3,862 / 1,438; 1 renderer, 2 submeshes, casts shadows, no light, no collider.

## 5. WebGL

The WebGL build is a separate, reduced tier. Desktop (Editor, Mac, Windows) is never changed for it.

- In a WebGL player only (`#if UNITY_WEBGL && !UNITY_EDITOR`, facade `WindowParts.WebGLTier`, already in main), every frame is forced to LOD1 (W-L0 1,044 tris, W-OF 1,438) and casts no shadow. Same materials, same section, same render-only rules.
- Per frame in view that is 1 renderer with 2 submeshes (about 2 draws). The map's trims were part of the merged chunk trim mesh, so they cost no extra draw. In the Office view above, 44 frames are in the frustum, which would be about +88 draws over the WebGL build's ~2,300.
- G14 is desktop macOS only (both WebGL strippers are in main), so the RT cost in §4 does not apply to WebGL.
- Not built or measured in a WebGL player here. The WebGL track should count draws on its own harness before release. If it is over budget, the WebGL-only option is a WebGL cull distance for the frames **without T on WebGL** (the map's trims are then the far fallback). That needs a `#if UNITY_WEBGL` in the map's T hunk, so it would be a second contract request.

## 6. Hand-offs from the art review (no code from us)

Each item has an owner and a test. Details and frames: `05_for_red.md` §3.

| # | Item | Owner | Test before it lands |
|---|---|---|---|
| A1 | Intact glass does not read as glass (edit mode 1.5 m: 3.2–5.4 luma with the kit against 10.4–36.6 for the old milky cube; Play 1 m: 2.9 / 2.8 luma) | visual chat (URP asset: probe box projection + blending, desktop only) + glass track + G14 | ≥ 12 luma at 1.5 m; one troffer-shaped reflection at 1.5 m front and 45°; not milkier |
| A2 | Round bloomed lamp highlights on the glass | glass track | no round disc at 0.3 m (`OF_B_close_head`) |
| A3 | Walnut casing and liner read as one block | G4 (after T) | reveal visible at 0.3 m and 1.5 m |
| A4 | Pressed steel reads as matte black plastic | visual chat (`Prop_SteelBrown`, shared with the key door frame) | door readability harness (`interactables/05`), then the 0.3 m screw frame without the inspection light |
| A5 | Wear authored in Blender is invisible (`FrontRooms/Surface` ignores vertex colour) | visual chat, **Red approves W2** | 0.3 m sill, horn and screw frames |
| A6 | Frames sit on the wall like stickers (−2 to −12 % under the stool) | visual chat, desktop only | ≥ 15 % darker in the first 1 cm under the stool at 1.5 m |
| A7 | Shadow staircase on the steel stops | visual chat (lamp look) + map chat (lamp tick) | cause confirmed: shadow-map resolution/filtering (`05` §3 A7, VL108) |
| A8 | Glass grime reads as aquarium dirt | glass track | 15–20 mm prints, band ≤ 0.3 |
| A9 | Apron ends square-cut; long grain on end faces | G4 | mitred returns at 0.3 m |
| A10 | Black rubber glazing line on walnut | visual chat | **fixed in the clone** (item 3); Red's look call (VL109) |
| A11 | Decorative groove in the W-OF stop | G4 + map chat (S2, `03` §5) | — |
| A12 | Walnut grain contrast too low beyond 1 m | visual chat (shared with `Kit_DoorFrame_Wood`) | readability values (`Prop_WoodWalnut` luminance 0.036) |
| A13 | RT forward-compatibility | G14 | LOD0-only and all-slot albedo are in G14's code (§3); one RT frame per member owed |
| A14 | Windows live before the doors | Red | §2 |

## 7. Docs owed by other owners (codex audit DOC-D4 / F7; outside this workflow's write scope)

`Documentation/VISUAL_CHAT_TASKS.md` still says "PROMOTED; runtime acceptance pending" for W1.4 (line 78 today). Exact replacement text for the visual chat:
- **W1.4 description:** "Promoted to main by Codex 2026-10-03 (the r3 facade plus a direct `DressWindow` call in `RaiseWindowBuilt`). Re-tested on main 2026-10-07 (window landing r6): 411 windows in 10 seeds, gameplay identical to kit off, Play 26 / 26, the map suites pass. The map's trims are still drawn under every frame. Contract r6 (R1 + R2, optional T): `research/interactables/window_landing/03_contract_map.md`; promotion list: `04_promotion.md`; Red's view: `05_for_red.md`."
- **W1.4 status:** "LIVE (Codex); r6 tested PASS; contract waits for Red's DW confirmation".
- **W1.5, last sentence:** "Ray tracing (G14) traces the visible slab by its material since `9eddc35`; its look is not accepted yet (codex audit F5b)."
- **W1.7, append:** "The kit frames have been live in Play since 2026-10-03 19:10 (Codex), before D1.5."

`LEVEL_MODULE_SPEC.md` "Window" row: the map chat's, with T (`03` §4).

## 8. Landing commands (only after Red's OK; each owner runs its own lines)

Dry-run today (17:50) on copies of main's files at HEAD `a5262fb`: every diff below passes `git apply --check`, and the results are byte-identical to the tested clone files.

```zsh
cd "<Frontrooms3D>"; D=Documentation/research/interactables/window_landing
# bases (sha256): facade cafa48bd…, RenderSetup cef579cc…, MapWorld a56fc9e1… — stop if any differs and re-run 03 §6
shasum -a 256 Assets/Scripts/Office/FrontRoomsInteractableKit.Window.cs Assets/Editor/Rendering/FrontRoomsRenderSetup.cs Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs
# 1  facade r6 (visual chat)
git apply --check $D/FrontRoomsInteractableKit.Window.r6.diff && git apply $D/FrontRoomsInteractableKit.Window.r6.diff
# 3  putty (visual chat, after Red's look call): one SurfaceDef line + the new material and its meta
git apply --check $D/FrontRoomsRenderSetup.putty.r6.diff && git apply $D/FrontRoomsRenderSetup.putty.r6.diff
cp $D/Prop_Putty.mat.txt Assets/Resources/Surfaces/Prop_Putty.mat; cp $D/Prop_Putty.mat.meta.txt Assets/Resources/Surfaces/Prop_Putty.mat.meta
# 4  map R1 + R2 (map chat), then 5 T only if Red says yes
git apply --check $D/FrontRoomsMapWorld.window-kit.r6.diff && git apply $D/FrontRoomsMapWorld.window-kit.r6.diff
git apply --check $D/FrontRoomsMapWorld.window-kit.r6-trims.diff && git apply $D/FrontRoomsMapWorld.window-kit.r6-trims.diff
#    or, if the file has drifted: python3 $D/apply_window_kit_r4.py.txt Assets/Scripts/FrontRoomsMap/FrontRoomsMapWorld.cs [--trims] --out <file>, then diff and re-test
```

Manifest: 3 modified files (`FrontRoomsInteractableKit.Window.cs`, `FrontRoomsRenderSetup.cs`, `FrontRoomsMapWorld.cs`), 2 new files (`Prop_Putty.mat`, `.meta`), no `.meta` GUID changed, no file deleted, no clone harness.
