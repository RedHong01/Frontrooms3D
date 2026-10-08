# Landing audit · 01 · Inventory: which visual work is in the game, and which is not

Date: 2026-10-03, 17:20–17:45 PDT (local clock). Read-only audit. Unity was not opened on Frontrooms3D. Nothing in the real project was changed except this folder.

Red's question (2026-10-03, translated): "Many of the new visual models and updates you made do not show when I play the real game in Editor Play Mode."

**Short answer.** Red is right. Almost every new MODEL from today exists only in private clones. The doors, locks, keys, window frames, glass, outlets, Run! props, level transitions and the Hunter are not in his project. The only visual work that is in the game today: the 10-02 Office kit (54 models), the HUD fixes, the shader-compile fix, the Level 0 lens ×1.5, and the wallpaper paper/print split (since 17:22). On top of that, two hooks are in the game but do nothing yet: the zone reflection stub and the map's new window-glass seam.

---

## 0. Counts

| Class | Meaning | Rows |
|---|---|---:|
| **A** | In the real project, live in Editor Play Mode | **15** (12 with something to see; 3 with no on-screen change by design: A10, A14, A15) |
| **B** | In the real project, but not visible: a step is missing | **3** |
| **C** | Only in a clone, waiting for Red's decision | **9** |
| **D** | Only in a clone, approved or needs no decision | **4** (1 verified and ready: D1. 1 tools-only: D2. 2 still running: D3, D4) |
| **E** | Research or design only, nothing to land | **13** |
| | **Total** | **44** |

By volume, what Red would call "new models" sits in C and D. That is **104 distinct FBX** in clones and **0** of them in the real project:
- 59 interactables (proj_int);
- 13 outlets (proj_outlet);
- 11 Run! props (two Run clones);
- 21 Hunter/giant blockouts (proj).

proj_win also holds 5 copies of the proj_int window models. The real project has 54 kit FBX, and all 54 are the 10-02 Office kit.

---

## 1. How this was checked

- **Hashes, not the task file.** Every clone under `scratchpad/proj*` was walked (Assets, Tools, ProjectSettings, Packages, NativePlugin). Each file changed after the clone was made was compared by md5 with the real project's file. Script: `scratchpad/landing_audit/clonediff.py`. Output: `scratchpad/landing_audit/clonediff_all.json`.
- **Red's own editor log** (`~/Library/Logs/Unity/Editor.log`, read only) shows what his editor imported and what he played:
  - editor start 16:19;
  - Play sessions on seeds 1135111108 and 107685139;
  - at 17:29 the P0 recompile, then a 61 s asset refresh: 207 project textures re-imported (the importer hook changed), plus the Surface shader and the three wallpaper materials;
  - then another Play session, on seed 1018292740, with P0 live.
- **Domain reload is ON when entering Play Mode.** `ProjectSettings/EditorSettings.asset:27-28` has `m_EnterPlayModeOptionsEnabled: 1` and `m_EnterPlayModeOptions: 0`, which means reload domain and scene. The log line "Reloading assemblies for play mode" confirms it. So the static caches in `FrontRoomsKitLibrary` and `FrontRoomsSurfaces` start empty on every Play. **Stale caches are not a cause.**
- **Auto refresh is ON.** Unity prefs have `kAutoRefreshMode = 1`. Files copied into the project are imported the next time the editor has focus.
- **iCloud is not hiding anything.**
  - There are no conflict copies under Assets.
  - The only one in the project is `ProjectSettings/ShaderGraphSettings 3.asset`, which Unity does not load.
  - No file under Assets/Resources, Scripts, Editor or Settings is dataless (evicted).
- **Git:** someone committed everything at 17:27 (`df4cb03`), including the P0 apply and the key glyph. The working tree now has only docs plus three new placard Blender modules.

---

## 2. Class A: in the real project and live in Editor Play Mode

| # | What | Where (real project) | Proof | What Red sees |
|---|---|---|---|---|
| A1 | **Office kit round 2 + piles (10-02)**. 54 kit models with JSON sidecars, 64 `Prop_*` materials, 111 `Prop_*` CC0 textures, and the three Office scripts | `Assets/Resources/Props/Models/Kit_*`, `Resources/Surfaces/Prop_*`, `Scripts/Office/FrontRoomsOfficeKit.cs` (md5 5d45e34c), `FrontRoomsFurniturePile.cs` (f7f95eba), `FrontRoomsKitLibrary.cs` (82ec70e1) | 110 of 110 kit files and 111 of 111 textures are byte-identical to clone `proj`. The scripts match `proj` too. All 54 FBX date from 10-02. Commits 9abf01a, 08031cb | Office rooms in the map (`MapWorld` calls `FrontRoomsOfficeKit.Dress` by reflection, :1689-1710). Furniture piles in large halls. Level Designer module props (`KitLibrary.Spawn`, :1909). Title Office rooms (`RoomStream`:1438) |
| A2 | **Office kit fixes (10-02):** the cut-room dressing fix (`Occupancy.Joined`), the kit cache reset (`ClearCache` + importer hook) and the 1.65 m tall spine panel (**Q9: the task file still lists it as queued, but it is done**) | `FrontRoomsOfficeKit.cs:40, 583, 612, 645-649`; `Editor/Rendering/FrontRoomsKitImporter.cs` (6511c898); `Kit_CubiclePanelTall` (bounds y 0–1.650, 472 / 184 tris) | Same as clone `proj` | Tall panels in about 15 % of cubicle pods. They block the Relay's 1.6 m sight ray |
| A3 | **Era-lock fixes (10-02):** CRT thumbwheels, labels dated 1989/1990, the copier | `Kit_CRTMonitor.fbx` (10-02 18:28), `Kit_Copier.fbx` (18:44), `Prop_Label_A.png` (18:24) | Identical to `proj` | Visible on close inspection |
| A4 | **Office local grade (10-02):** a post volume per Office zone | `Resources/Rendering/FrontRoomsPost_Office.asset` (2d46ab21), `Scripts/Rendering/FrontRoomsPostStack.cs` (5b81f1dd) | The map calls `FrontRoomsPostStack.EnsureZoneVolume(holder, "Office", …)` at `MapWorld:1981`. The asset differs from what RenderSetup would write (hand-tuned; see §7.4) | The Office zones' grade |
| A5 | **Surface look-dev (10-02):** the Office lens is the frosted troffer lens; `Troffer_Lens` and `Office_Louver` regenerated | `Scripts/Rendering/FrontRoomsSurfaces.cs` (2854de18), `Resources/Surfaces/Troffer_Lens.mat`, `Office_Louver.mat` (commit 8f5cb75) | Identical in every clone | Office ceilings |
| A6 | **Relay rig hooks (10-02):** Search, DoorBlow, Step, CeilingHeight, DoorSqueeze, CeilingBrush, Squeeze | `Scripts/FrontRoomsRelayRig.cs` (26691fe5) | Identical in every clone. Wired in `FrontRooms3DGame.cs:302, 330, 1323-1327` | The primitive Relay's motion states |
| A7 | **F1 Play Mode shader compile:** no cyan placeholder | `Editor/Rendering/FrontRoomsPlayModeShaderCompile.cs` (dd7b9a24) | Identical to 4 clones. Imported at editor start (Editor.log line 820). The "missing Metal Toolchain" lines in the log are known probe noise (F1) | No cyan lens or glass. A one-off hitch instead, the first time a variant draws |
| A8 | **F2 crosshair:** a white dot | `Resources/UI/HUD_Crosshair.png` (e6f9cb69) | Same md5 as `scratchpad/hud/HUD_Crosshair_dot.png`. Loaded at `FrontRooms3DGame.cs:1786` | The dot |
| A9 | **F2 key glyph with real alpha** | `Resources/UI/HUD_KeyGlyph.png` (6469c3e7, committed 17:27) | Same md5 as `scratchpad/hud/HUD_KeyGlyph.png`. The importer has `alphaIsTransparency: 1`. Loaded at `:1805`. **Row F2 in the task file is stale: it says this still waits on Red** | No white box, but only while you carry a key |
| A10 | **F5 ambient values** | `Scripts/Rendering/FrontRoomsLook.cs` (2e3ae3bb) | Same md5 as `scratchpad/FrontRoomsLook.cs.new` | **Nothing, by design.** The map chat measured mean luma 73.7 → 73.7 |
| A11 | **Level 0 lens ×1.5** (our pick; the map chat's code) | `MapWorld.cs:3018` `Level0LensEmissionScale = 1.5f`, on a map-owned copy of `Troffer_Lens` (:3029-3033) | Read in the real file (17:16) | Map Level 0 lenses: the far lens is no longer grey |
| A12 | **Wallpaper P0** (paper/print split, emboss 0.60) | `FrontRoomsSurface.shader`, `FrontRoomsRenderSetup.cs`, 3 wallpaper materials, `Wallpaper_Paper_M/N/S`, `Wallpaper_Print_P`, the P0 tests, `Tools/lookdev/gen_surfaces.py` | `scratchpad/p0port/expected_manifest.txt`: **55 of 55 entries match** (sha1, ABSENT or DIR; the task file counts 49 paths). The 6 legacy `Wallpaper_Chevron*` textures are gone from `Resources`. Imported in Red's editor at 17:29. Red played after it | A subtle change on Level 0, Shift and Exit walls, in the title rooms and in the map: the pink/slate cast goes neutral, and the paper shows a linen-weave relief under raking light |
| A13 | **GD2 glass-break shot** ("brace, strike, flinch"; the map chat's code from our plan) | `Scripts/FrontRoomsShots/FrontRoomsGlassShot.cs`, `FrontRoomsShotTimings.GlassBreak`, `FrontRoomsCameraRig.cs` | Wired at `FrontRooms3DGame.cs:615` | **Only the camera part.** Nothing in `Scripts/Rendering` listens to `FrontRoomsGlassShot.Beat`, so there is no CA pulse. The staged glass is GD3 (C4) |
| A14 | **WG0:** the RoomStream cylinder uses a CapsuleCollider | `FrontRoomsRoomStream.cs` (commit 5e61492) | Real file = `scratchpad/outlet_logs_RoomStream.orig.cs` (48893264) | **Nothing, by design** |
| A15 | **Tools only:** the print encode LUT, the 3-view tool, CC0 sources, `gen_props.py`, `FrontRoomsLookdevCapture.cs`, and 60 Blender source modules (42 `interact_*.py`, 15 `outlet_*.py`, 3 `evac_placard*.py`) | `Tools/lookdev/print_encode_lut.json` (sha1 353e29eb), `Tools/three_view/`, `Tools/lookdev/cc0_src/`, `Tools/Blender/frontrooms_kit/assets/` | Present | **Nothing in the game.** The Blender modules are only sources. Their built FBX are in clones (C3, C2, D3, D4) |

---

## 3. Class B: in the real project, but not visible (a step is missing)

| # | What | Where | Why nothing shows | What it takes |
|---|---|---|---|---|
| B1 | **Zone reflections (G6).** The final API is in, and the game calls it | `FrontRoomsLook.cs:34`: `SetZoneReflection(zone, blend)` has an empty body (STUB). `FrontRooms3DGame.cs:1057-1070` calls it every frame | The body does nothing, so walls, floors and glass reflect only the default probe | Promote the glass track's `FrontRoomsZoneReflection.cs` + `Driver`, `FrontRoomsReflectionBlend.shader` and the 4 cube EXRs + manifest. Then make the two-line merge in `FrontRoomsLook` (`research/glass/30_final.md` §4). The clone's Look already carries the F5 values, so the "PROMOTION CHECK" in task row F5 is resolved. Under Red's earlier rule it ships with the glass (C1) |
| B2 | **The map's window/glass seam** (the map chat, 17:16) | `MapWorld.cs:1195-1265`: an unscaled root `Window {a}-{b}`, the events `WindowBuilt`/`WindowReleased`, and a component hook for `FrontRoomsGlassBreakable`. Documented in `Documentation/MAP_GENERATION.md:37` | `FrontRoomsGlassBreakable` does not exist in the real project (only in the GlassLab clones). So the map still draws its own 30 mm cube (`ModuleUnits.GlassThickness = .03`) with a runtime URP/Lit `TransparentGlass` ("Map test / glass", `MapWorld.cs:3062-3065`). **Promoting `Glass_Window.mat` alone would change no window**, because the map never loads it | Ship a visual-side `FrontRoomsGlassBreakable`, or a `WindowBuilt` handler, that draws the frame and the 6 mm `Glass_Window` slab (C1 + C2 + C4). No map edit is needed for the glass. Removing the map's own window trims still needs a contract |
| B3 | **ChatGPT's Metal RT prototype** (not ours; listed because Red asked about RT) | `Assets/Plugins/macOS/libFrontRoomsMetalGlassRT.dylib` (75,744 B), `Scripts/Rendering/FrontRoomsMetalGlassRT*.cs`, its feature in `FrontRooms_URP_Renderer.asset`, `MapWorld.cs:485-488` | `Ensure()` runs only `if (standalone && Application.isPlaying)`. The main game builds the map with `CreateEmbedded`, which sets `standalone = false` (`MapWorld.cs:506`), so the controller never exists in the real game. The G14 review also found 5 stacked defects | G14 production (C5) |

---

## 4. Class C: only in a clone, waiting for Red's decision

The last column is what Red's 18:0x standing order means for each row: merge finished work as assets/code with the recommended defaults, inert until the gameplay wiring is decided. Map-file changes go out as exact contracts.

| # | What | Where (clone) | Status / verification | Red's decision | Landing under the standing order |
|---|---|---|---|---|---|
| C1 | **Glass track G1–G4, G6, G10:** `FrontRooms/Glass` (two-surface reflectance, 8 % at 0° and 36 % at 75°), grime, the `Glass_Window/Edge/Shard/ShardClear` materials, `Prop_Glass` and `Prop_BottleBlue` moved onto the glass shader, zone cubes, the RT input hook | `proj_glass`: 31 changed files (12 cs, 3 shaders, 8 mat, 6 textures/EXR). Text copies in `research/glass/promote_src/`. Promotion list: `research/glass/30_final.md` §4 (23 new files + 2 overwrites + 3 one-line hooks) | DONE and verified (G10 run 3; RT hook bit-identical with the keyword off, 175/175). Both hook anchors still exist in the real files: `RenderSetup.cs:43` `EnsureGlassMaterials();` and `RoomStream.cs:350` `initialized = true;` | 13:3x: "the glass lands together with the window frames (W1), after the DW proposal is confirmed" (D1.5); plus the look sign-off on the `30_final.md` §2 frames | Merge the files plus "Set up glass materials" (`FrontRoomsGlassSetup.EnsureAll`, glass only; **not** RunBatch). Effect without W1: kit props with glass (hutch, display cabinet, vending front, water cooler) and zone reflections (B1). Map windows stay as they are until B2 |
| C2 | **W1 window frames:** W-L0 walnut and W-OF steel (`Kit_WindowFrame_Wood/Steel/Steel_Enamel/Alu`, `Kit_MiniBlind_Raised/Lowered`: 6 FBX), plus the facade `FrontRoomsInteractableKit.Window.cs` | `proj_int` (models, 16:45 builds); `proj_win` (facade, harness, tests) | Tests: 38,538 / 0; 411 windows over 10 seeds. The map suites are identical with and without the kit (`window_landing/02_tests_frames.md`). **The contract diff `window_landing/FrontRoomsMapWorld.window-kit.diff` no longer applies** to the real `MapWorld.cs` of 17:16 (`git apply --check` fails at the hunk at line 215), because the map chat built its own seam (B2) | D1.5: call (6) Lobby glass clear or hammered, (7) raised blinds with one bent slat, (8) the LOD change | Rebase the facade onto `WindowBuilt` + `FrontRoomsGlassBreakable`. Re-send only the trim-removal part as a contract. Merge the 6 FBX + JSON inert |
| C3 | **D1 doors, locks, keys and signage:** the door frame and leaf family, closers, exit device, 20 lock parts, zone keys, 9 key tags, ring, hook, key board and key cabinet, door sign, number plates, exit signs. 53 FBX | `proj_int` (`Resources/Props/Models`, 13:01–17:23). Blender sources already in the real `Tools/Blender/frontrooms_kit/assets/interact_*.py` | G1–G3 built. Render, critic and three-view stages still running (lock parts rebuilt 17:08–17:23) | D1.5: Red's 8 calls on slide 12 of the DW proposal (Figma 2748:6099, rebuilt 2026-10-07). Plus the name: `Kit_KeyHookBoard` or `Kit_KeyRack` | Merge the FBX + JSON inert once the stages finish. In-game use needs the map's door state machine and key hosts (D1.6, a map contract) |
| C4 | **GD3 staged glass breakage:** cracks S0–S2, debris S3–S4, the shot prototype | `proj_gd3`, `proj_gd3_shot` (`Assets/GlassLab/*`). Docs: `research/glass/destruction/build/` | RUNNING (17:2x). Not a deliverable yet: `FrontRoomsGlassBreakable` exists only in the lab | Ships with C1/C2 (the same 13:3x rule). The break rules were decided in GD2 | Target the map's seam (B2) |
| C5 | **G14 ray-traced glass:** a production Metal path (`GlassRT/*.cs`, prepass shader, native sources; the dylib grows from 75,744 to 200,000 B) | `proj_rt`: 45 changed files | RUNNING (P0/P1, 17:24). Its `FrontRoomsGlass.shader` (42,863 B) is newer than `proj_glass`'s (29,095 B): **reconcile the two before promoting** | Ships with the glass (C1) | Promote after its verifier. Replaces B3 |
| C6 | **N1 level transitions:** 5 variations (drift, frame, light lead, neck, renovation) | `proj_trans_drift/frame/lightlead/neck/renovation`. Each one patches `MapWorld.cs` (a map file, so a contract) and adds `Scripts/Rendering/Transitions/*.cs` | Only V5 light lead is rendered and written up (`level_transitions/20_variation_lightlead.md`). The other four are rendering | Red picks one in Figma "FRONTROOMS · LEVEL TRANSITIONS" | After the pick: the visual files + a MapWorld contract. Drift edits `FrontRoomsSurface.shader` on a pre-P0 base (§7.2) |
| C7 | **N2 Run! directions:** red_ward (7 FBX: hospital props + exit signs; 14 textures; `Post_Run_A`), emergency_power (4 FBX, 27 textures, 2 light shaders, `Post_Run_B`), house_lights (20 `Run_C_*` materials, `Post_Run_C`) | `proj_run_red_ward`, `proj_run_emergency_power`, `proj_rooms_house_lights` | Renders RUNNING (17:2x), re-rendered with the glossy waxed VCT Red decided at 14:1x | Red picks a direction | After the pick. All three edit `FrontRoomsSurface.shader` on a pre-P0 base (§7.2) |
| C8 | **Hunter blockouts and giants:** A–D at human scale and 4 giants × 4 poses (21 FBX), 15 `Creature_*` materials | `proj` (`Resources/Creatures`, `Resources/Surfaces/Creature_*`) | DONE as concept blockouts. Figma 2331:852, HR01–HR16 | W1 (task file §4): which body, the neck, the catch distance | Nothing until the pick. The game keeps the primitive `FrontRoomsRelayRig` |
| C9 | **K1 HUD key icon:** 3 directions, and drop-ins in two slot sizes | `Documentation/research/ui_key_icon/design/` (`dropin/slot_40x22`, `slot_49x22`; updated 17:28). Not a clone, but not installed | Design RUNNING. 平面视觉 is rebuilding the variations in Figma | Red's pick after the hand-off. A slot-size change is a map request | With the recommended default: replace `Resources/UI/HUD_KeyGlyph.png` at today's slot size |

---

## 5. Class D: only in a clone, approved or needing no decision

| # | What | Where | Verification | Landing step |
|---|---|---|---|---|
| D1 | **Q13 dressing hitch:** a time-sliced, bit-identical rewrite of the Office and pile dressers, and the new `FrontRoomsDressJob` | `scratchpad/dress_hitch/final/`: `FrontRoomsDressJob.cs` (new), `FrontRoomsOfficeKit.cs`, `FrontRoomsFurniturePile.cs`, `FrontRoomsKitLibrary.cs`, `Editor/FrontRoomsDressPerfTest.cs` (= `proj_rs`). Map contract: `dress_hitch/map_ticket2/FrontRoomsMapWorld.dress_step.patch` | `runs2/all.out`: **1,111 cases, 0 sync and 0 step mismatches** (twice). Map 126/126, designer 138/138, fixture 28/28, Relay nav 60/60. The patch **applies cleanly** to the real `MapWorld.cs` of 17:16. Before: a room took 42–166 ms in one frame. The timing run A3 was still going at 17:31 | No on-screen change, by design; the hitch goes once the map applies the patch. **Merge conflict ahead:** the outlet work (D3) also edits `FrontRoomsOfficeKit.cs` (31 diff lines against 461 here). Land D1 first and rebase D3 |
| D2 | **Q10 look-dev tools:** `FrontRoomsHunterLookdev.cs`, the ceiling-height overload in `FrontRoomsKitLookdev.cs`, and 15 `Creature_*` SurfaceDefs in RenderSetup | `proj` | Compiled and used for the Hunter renders. The gate was "after P0 merges", and P0 merged at 17:22 | Editor only, so nothing in the game. Hand-merge the RenderSetup lines onto the P0 file. **Never copy the file**: the `proj` RenderSetup is a pre-P0 base |
| D3 | **N3 power outlets:** 13 FBX (`Kit_Outlet*`, `Kit_Jack*`, `Kit_FloorBoxTombstone*`, `Kit_Conduit*`, `Kit_CubiclePanel_Powered`), the planner `FrontRoomsWallFixtures*.cs` (3 new `Office/` files), and edits to the Office kit and RoomStream | `proj_outlet` (files up to 17:23). Blender sources in the real Tools | RUNNING. The spec's reference planner passed on 20 seeds × 25 chunks (`outlets/10_spec.md` §4.4). Final tests and critics pending | When verified: merge the visual files, and send the MapWorld call as a contract. In the game today, outlets are only the old 0.32 m placeholder boxes in the title Level 0 rooms (`RoomStream.cs:1420-1425`) |
| D4 | **Q16 evacuation-plan placard** | Spec `research/placard/10_spec.md` (17:1x). Blender modules started in the real Tools (`evac_placard*.py`, 17:29–17:30, untracked). `proj_placard` has no changes yet | RUNNING. Nothing built | When built: merge plus a placement contract for the map |

---

## 6. Class E: research and design only, nothing to land

| # | What | Where |
|---|---|---|
| E1 | R1 interaction and render-quality audit (71 items); the ShotTimings proposal, which the map chat implemented | `research/interaction_audit/`; harness in `proj_audit/Assets/Editor/Audit` (do not promote) |
| E2 | W1.1/W1.2 and D1.1/D1.2 research and spec; the D1.3 DW proposal (Figma 2748:6099, rebuilt 2026-10-07 after 2497:3804 was deleted; 13 slides; `proposal/03_for_red.md`) | `research/interactables/00–06, 10_spec.md, proposal/` |
| E3 | DB1 door break: research and design. The Figma proposal is running | `research/door_break/01, 02, 10` |
| E4 | GD1 glass destruction research and plan | `research/glass/destruction/01–04, 10` |
| E5 | G11 reflections/RT study; the G14 review, probe, research and design; the G14b secondary-reflection research + bench (design stage running) | `research/glass/11_*.md`, `research/glass/rt/01–03, 10, 11a, 11b` |
| E6 | N2 room-visuals inventory, research index, 53 current-state frames, Run! research and directions plan | `research/room_visuals/01–04, 10` |
| E7 | N1 transition research and plan | `research/level_transitions/01–03, 10` |
| E8 | N3 outlet research and spec | `research/outlets/01, 02, 10` |
| E9 | Q16 placard spec | `research/placard/10_spec.md` |
| E10 | K1 key-icon research | `research/ui_key_icon/01_research.md` |
| E11 | WebGL plan rev 5, and the budget probe in `proj_web`. Waiting on Red: D1–D10, D12–D14 | `research/webgl/` |
| E12 | Hunter research (01–04, 10, 11) and the squeezed-giant brief | `research/hunter/` |
| E13 | Rules with no asset: F6 reduce flashing, F7 lamp interface, F8 / Q12 period typography | `VISUAL_CHAT_TASKS.md` §0 |

---

## 7. Why things do not show (cross-cutting)

1. **Most new work never left the clones.** 104 new FBX are clone-only (C2, C3, C7, C8, D3). Clone-only scripts include the glass shader, the zone reflection, the window facade, the transitions, the outlet planner and the dress job. This is the main cause. The working rule ("only compiled, verified files are promoted") plus Red's own gates (W1, D1.5, N1, N2) kept them out.
2. **Code paths the game does not use:**
   - the map builds its own window glass at runtime (B2), so a project glass material is never seen there;
   - the RT prototype only starts for a standalone map (B3);
   - the zone reflection API is a stub (B1).

   The title RoomStream and the map share the surface materials, the troffer lens, the Office kit and the wallpaper, so those reach both.
3. **Not causes today** (checked):
   - stale caches: domain reload runs on every Play;
   - auto refresh: on;
   - iCloud copies: none under Assets;
   - async shader compile (cyan): fixed by A7.
4. **The RenderSetup trap (`RunBatch`).** The P0 port's clone run (`scratchpad/p0port/rs_classify.txt`) shows what RunBatch would do in Red's project:
   - it would change the content of only 3 assets beyond P0: `FrontRoomsPost_Office.asset` (hand-tuned), `Prop_Glass.mat` and `Prop_BottleBlue.mat` (the saved assets don't match their generator, G4);
   - the other 80 materials would only gain serialized print properties (`_PrintTex`, `_UsePrint`, `_PrintAmount`, the ink palette). Checked: 0 of the 80 carry them today. The shader defaults make that harmless.

   So **no promoted code is waiting on a material regeneration.** The disk already matches the code. RunBatch would only destroy tuning. Do not run it. Glass uses its own `EnsureAll`.
5. **Rebase hazards before promoting:**
   - P0 changed `FrontRoomsSurface.shader` (now 28,487 B with `_FR_PRINT`). Every clone shader is a pre-P0 base: drift 29,198 B, red_ward 15,096 B, house_lights 15,590 B, emergency_power 14,790 B. All have 0 `_FR_PRINT`. Their edits must be re-applied as diffs.
   - `FrontRoomsOfficeKit.cs` is edited by both D1 and D3.
   - The glass shader exists twice (C1 29,095 B vs C5 42,863 B).
   - The window-kit contract diff is stale (C2).
6. **Stale rows in `VISUAL_CHAT_TASKS.md`:**
   - F2: the key glyph is done (A9);
   - F5: the "PROMOTION CHECK" is resolved (`proj_glass`'s `FrontRoomsLook` has the F5 values);
   - Q9: the tall panel is in (A2);
   - W1.4: the map chat has since built the window root and the glass seam itself (B2).

---

## 8. Not started, or started after this audit began (no row above)

**Started after this audit began:** these appear in the task file's new "000" section, but nothing was built when I checked at 17:4x. There is no clone yet and no `Resources/Print/`.
- Q1b live print (Hard-edge `Texture2DArray`, merge when verified);
- exit-sign (Red: update the EXIT sign model and textures);
- kit-threeview-figma-docs;
- the VL verification log.

When they produce files, they belong in D (approved).

**Not started:** Q1 (print warp), Q2 (on hold), Q3, Q4, Q5, Q6, Q7, Q8, Q15, G5 (`Kit_InteriorWindow` pane), G7 (room probes; the glass shader is ready in `proj_glass`), WG5–WG7, W2 (torchiere and task-chair period fixes), W3 (delete Codex's `FrontRoomsOfficeFurniture`, which is still unreferenced), H1–H4 (after the Hunter pick).
