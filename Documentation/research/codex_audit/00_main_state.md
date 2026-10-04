# Codex audit · 00 · Main state: what Codex put into Red's project, and where each file came from

Date: 2026-10-03, 21:50–22:20 PDT. Read-only on Red's project: Unity was not opened on Frontrooms3D, nothing was committed, and the only files written are the two in this folder. Per-file table: `00_files.tsv` (337 rows, tab-separated).

Base: `7320ed1` (18:25, before Codex). HEAD: `75cfdff` (19:41). Branch `main`.

---

## 0. Short answer

- **335 paths changed** between `7320ed1` and HEAD. 332 come from Codex's four commits. 3 come from another Claude session (`a7b0dbb`, title logo relay), merged cleanly by `fc9e2ca`.
- **Everything in `8ef5b64` ("1") is Codex's.** Codex wrote it between 18:47 and 19:03. The exception is 110 `.meta` files, which Red's open Unity editor generated at about 18:57 when it imported Codex's files. Proof: Codex's own snapshot at 18:40 (`/tmp/frontrooms-before-merge-20261003-184033.status`) is empty, so the tree was clean before it started. Its session log (`~/.codex/sessions/2026/10/03/rollout-2026-10-03T13-09-08-…jsonl`) holds every copy and patch command. GitHub Desktop made the commit at 19:10, after Codex deleted a stale `.git/index.lock`.
- **The map chat wrote none of the `FrontRoomsMapWorld.cs` changes.** Codex's 18:40 backup of MapWorld is byte-identical to `7320ed1`. All 11 hunks are Codex's.
- **By class** (337 rows; 335 paths plus 2 non-git items):

| Class | Rows | Meaning |
|---|---:|---|
| V | 63 | identical to a verified final (glass track 47, window kit G4 models 16) |
| W | 142 | identical to unfinished clone work (G14 30, interactables 106, N1 5, window facade meta 1) |
| E | 11 | a clone file that Codex then edited |
| X | 118 | no clone has it: 106 Unity-generated `.meta` files, 4 fresh folder metas, 2 Codex-made metas, Codex code (MapWorld, KitImporter, KitLibrary), docs, the lock file |
| O / V+O | 3 | other session (`a7b0dbb`); RoomStream mixes a glass hook with `a7b0dbb` |
| N | 0 | at 22:15 no producing workflow has yet written a newer version of a promoted file |

- **What changed in Red's Play Mode:**
  - glass and zone reflections are live;
  - map windows now get the kit frame and the 6 mm `Glass_Window` slab, while the map's own trims are still drawn as well;
  - N1 "light lead" (cool Office lamps +9 %, cool lens) and B0 (corner posts, grime band per side) are on by default, although Red has not picked a transition;
  - G14 ray tracing starts every frame on Mac. With the shader that is in main, it traces nothing on window glass.
  - The 53 door/lock/key/sign kits are in the project but no game code uses them.
- **Red's editor log after Codex's last fix** (Play session from log line 39,670 to the end at 20:01): 0 "Collection was modified" errors, 0 ZBinningJob errors, and no CS0618/CS0414 warnings. Before 19:41 there were 85 and 253.

---

## 1. Commits

| Commit | Time | Author (git) | Who really | Paths | What |
|---|---|---|---|---:|---|
| `7320ed1` | 18:25 | RedHong01 | Red | — | pre-Codex base |
| `a7b0dbb` | 18:47 | Claude | another Claude session (GitHub) | 3 | title logo relay opacity fix |
| `8ef5b64` "1" | 19:10 | RedHong01 | **Codex**; committed by GitHub Desktop | 328 | glass, G14, 116 kit FBX/JSON (58 assets) + 116 metas, window facade, N1, MapWorld edits |
| `fc9e2ca` | 19:11 | RedHong01 | merge | 0 | clean merge of `a7b0dbb`; no conflict hunks |
| `daef6c2` | 19:15 | RedHong01 | Codex | 2 | `VISUAL_CHAT_TASKS.md` rows; header comment in `FrontRoomsInteractableKit.Window.cs` |
| `9e754a2` | 19:33 | RedHong01 | Codex | 6 | `Kit_MiniBlind_Lowered` FBX/JSON + Codex-made metas; RT `DropChunk` fix; task file bullet |
| `75cfdff` | 19:41 | RedHong01 | Codex | 2 | MapWorld `Ensure()` call removed; dead `loggedOn` field removed |

Outside git, Codex also:
- deleted `.git/index.lock` (0 bytes, stale);
- edited `Documentation/research/webgl/10_webgl_plan.md` §7.3 (an ignored folder) at 19:27, then reverted it at 19:34. The heading reads as before.

**Working tree now (22:15) is beyond HEAD.** It holds 41 modified and about 50 untracked files, written by the resuming visual-chat workflows and other chats after 21:5x: docs, images, Blender modules (`interact_window_*`, `interact_key_*`, `outlet_*`, `evac_placard*`). The only change under `Assets/` is `Kit_MiniBlind_Lowered.fbx.meta`. Unity put back the trailing spaces that Codex had stripped; that is whitespace only.

---

## 2. How this was checked

1. `git diff --name-status 7320ed1 HEAD`, `git show --name-status` per commit, and `git status`.
2. **Sha1 index of the clones.** I found 98 clone roots under the scratchpad, including nested ones (`_partial/*`, `win_fix/src`, `critic_win_eng/chk*`, …). Every file with a basename from the diff was hashed: 1,644 files, plus the `.txt` copies in `research/glass/promote_src`. I hashed at 21:55. Workflows began re-creating `proj_rt`, `proj_placard`, `proj_outlet`, `proj_gd3_shot` and `proj_rooms_house_lights` from main at 21:57–22:05. Their pre-Codex states survive as `*_pre1910`, and the GUID checks use those.
3. **Codex's session log** (read only). It gives the exact source of every copy:
   - glass from `promote_src` + `proj_glass`;
   - window files from `proj_win`;
   - `Kit_{Door,Exit,Lock,Key}*` from `proj_int`, "only where main does not already have that asset";
   - N1 from `proj_trans_lightlead`;
   - G14 from `proj_rt`;
   - `Kit_MiniBlind_Lowered` from `proj_int`.

   The six sub-agents Codex spawned only read files.
4. **Red's `~/Library/Logs/Unity/Editor.log`** (read only). The editor restarted about 19:06; the log runs to 20:01.
5. Glass md5s checked against `research/glass/30_final.md` §4: **24 of 24 match**.

---

## 3. Tracks: what is live, what is inert, what is still owed

### 3.1 Glass G1–G4, G6 (47 rows V; source `proj_glass`; verified by `research/glass/20_verification.md` G10 run 3 and `30_final.md`)

- **Copied:** the full promotion list in `30_final.md` §4: 23 new files with metas, 2 overwrites (`Prop_Glass.mat`, `Prop_BottleBlue.mat`) and 3 hooks.
  - The `FrontRoomsLook.cs` hook is the hand-merged `promote_src` file; F5 ambient is unchanged.
  - The RenderSetup hook was applied as the one line in §5.
  - The RoomStream hook is §4.3.
- **One slip, caught by Codex.** Its loop copied every `promote_src` file, including a **pre-P0** `FrontRoomsRenderSetup.cs.txt`. Codex then restored HEAD's file before patching (`git show HEAD:… >`, 01:48:11 UTC). P0 print code is intact.
- **GUIDs:** all glass metas are the clone's GUIDs.
- **Live in Play Mode:**
  - `SetZoneReflection` is no longer a stub. Title rooms use the Level 0 cube; the map switches cubes per zone (landing B1 is now live).
  - `Prop_Glass` and `Prop_BottleBlue` are on `FrontRooms/Glass`.
  - `Glass_Window` draws through the window facade's interim slab.
- **Still owed:** the "after promoting" step 3 in `30_final.md` §4 (recapture the 4 zone cubes). The cubes date from 11:57. They predate the Level 0 lens ×1.5 (landed 17:16) and N1's cool Office lens. Main-project Play Mode acceptance is also pending.
- **Old material values:** the pre-glass `Prop_Glass.mat` (URP/Lit, base colour α 0.16) and `Prop_BottleBlue.mat` can be recovered from `7320ed1`.

### 3.2 G14 ray-traced glass (30 W, 2 E, 3 X; source `proj_rt`, now kept as `proj_rt_pre1910`)

- **State of the source:** the implement stage was interrupted at 17:54. The dylib was built at 17:53:50, 1 minute earlier. There is no verify report.
- **Copied:**
  - `Scripts/Rendering/GlassRT/*` (4 files), `Editor/RT/*` (4), the prepass shader, 8 NativePlugin sources and tools;
  - the dylib (75,744 B → 201,280 B);
  - the new `FrontRoomsMetalGlassRTRendererFeature.cs` and the `FrontRoomsMetalGlassRT.cs` shim, both G14's own files;
  - `PostStack` + `OptIn(camera)` and the renderer asset `prepassShader` field. Both now equal `proj_rt`'s files.
- **Codex edits (E):**
  - `FrontRoomsGlassRTSystem.cs`: `DropChunk` now iterates a snapshot (`9e754a2`), and the unused `loggedOn` field is gone (`75cfdff`). The fix is right. `proj_rt_pre1910` still has the bug.
  - `libFrontRoomsMetalGlassRT.dylib.meta`: trailing spaces only.
- **X:** 3 folder metas (`Editor/RT.meta`, `Scripts/Rendering/GlassRT.meta`, `Shaders/GlassRT.meta`) were made fresh by Red's Unity. Their GUIDs differ from `proj_rt`'s.
- **Live:** yes, on desktop Mac. The log shows `plugin loaded · caps 0x31ff · on: Apple9, High` and `map found: Level 0 map`.
- **But nothing is traced on window glass.** `Glass_Window` (`_RTReceive` 1) is put on prepass bit 30 (`FrontRoomsGlassRTSystem.cs:707`), and bit 30 is drawn with the `FRGlassRTPrepass` pass (`FrontRoomsMetalGlassRTRendererFeature.cs:83`). Main's `FrontRooms/Glass` is the glass track's 29,095 B shader, which has no such pass. The pass exists only in `proj_rt`'s 42,863 B variant, which Codex did not take. The landing audit had warned "reconcile the two before promoting". As a result, coverage is 0 and the glass shader falls back to its normal output. The trace still runs every frame. (Reasoned from code; a capture is needed to confirm.)
- **Also copied without a promotion list:** the test tools `Editor/RT/FrontRoomsGlassRTVerify.cs`, `FrontRoomsGlassRTIsolation.cs` and `NativePlugin/tools/frglassrt_validate.mm`.
- **Owed by glass-rt-track:** finish implement → verify → fix. Merge G14's G-1/G-2/G-3 shader hunks onto the glass track's V shader. Keep main's `DropChunk` fix and main's folder GUIDs.

### 3.3 Interactables kits G1–G3 (106 W + 106 X; source `proj_int`)

- **Copied:** 53 assets = 106 FBX/JSON (doors, closers, frames, leaves, plates, sign, exit device, `Kit_ExitSign`/`_Dead`, 18 lock parts, keys, 9 tags, ring, hook, hook board, cabinet). All match `proj_int` byte for byte. `proj_int` has not changed since 17:23.
- **State of the source:**
  - G1 built (13:12 note), G2 run 2 (17:46 note), G3 second run (17:31 note);
  - the render, critic, fix and three-view stages never ran;
  - the G2/G3 rebuild was interrupted. `g2work/` and `g3/` are active again at 21:58.
- **Metas (X):** `proj_int` holds no `.meta` files. All 106 metas were generated by Red's Unity at 18:57 with **fresh GUIDs**. `proj_3view_int` (17:34) holds different GUIDs. Main's GUIDs are now the canonical ones.
- **Live:** no. No game code names these kits. `FrontRoomsMapWorld.Prewarm()` reads every sidecar, and the Level Designer palette lists them.
- **Name clash:** `Kit_ExitSign*` collides with the exit-sign workflow's own `Kit_ExitSign*`.
- **Owed by interactables-kit (+ exit-sign):** finish G2/G3, the renders and the critics. Merge the verified FBX/JSON over Codex's copies and keep main's metas.

### 3.4 W1 window kit

- **Models (16 V):**
  - `Kit_WindowFrame_Wood/Steel/Steel_Enamel/Alu` and `Kit_MiniBlind_Raised` come from `proj_win`, identical to the `proj_int` build;
  - `Kit_MiniBlind_Lowered.fbx` comes from `proj_int`;
  - verified by `build_g4_…md` §0 (DONE, re-verified 16:4x);
  - the metas keep `proj_win`'s GUIDs.
- **Sidecars (6 E):** Codex changed `"placement": "Floor"` to `"Wall"` in all 6 window/blind JSONs. That is the only difference, and it only affects the Level Designer palette.
- **`Kit_MiniBlind_Lowered` metas (2 X):** Codex wrote them with `uuid.uuid4()` GUIDs (`75e18a03…`, `af825826…`), copying the importer block from the Raised meta.
- **Facade `FrontRoomsInteractableKit.Window.cs` (E):**
  - `8ef5b64` is the `proj_win` file of 17:51. It is the r3 rewrite from the window-landing **fix stage**, edited at 17:41 in `win_fix`.
  - That stage's compile started at 17:52 and was cut off at 17:54. The r3 code was never compiled or tested in a clone; the 38,538/0 tests and 100-seed runs in `02_tests_frames.md` were on r2.
  - Codex's `daef6c2` rewrote 2 header-comment lines.
  - Red's editor compiles it with 0 errors.
- **Map hook (X, Codex):** a direct call `FrontRoomsInteractableKit.DressWindow(this, window, record)` in `RaiseWindowBuilt` (`MapWorld.cs:1296-1300`). It differs from the r3 contract (`win_fix/FrontRoomsMapWorld.window-kit.r3-all.diff`) in three ways:
  - the bool that `DressWindow` returns is ignored, so **the map's own trims are still drawn under the kit frame**. The map's jamb inner face sits at x 0.700 m; the kit lining sits at x 0.6995 m (0.5 mm apart), which risks z-fighting in the reveal;
  - the call is compile-time, not by reflection, so MapWorld now needs a visual-owned class in order to build;
  - the pane keeps its own `FrontRoomsMetalGlassTarget`, and the slab adds a second one.
- **Live:** yes, in every map window. Level 0 rooms get walnut, Office rooms get steel, plus the interim `Glass_Window` slab.
- **Owed by window-landing (r4 already running, `win_r4/`):** compile and test r3/r4 on main. Send the trims-removal and reflection binding to the map chat as a contract that replaces Codex's direct call.

### 3.5 N1 level transitions (5 W, 1 E, 1 X + 1 RenderSetup hunk + 9 MapWorld hunks)

- **Copied:** `FrontRoomsTransitionKit.cs`, `FrontRoomsTransitionLightLead.cs` and `Troffer_Lens_Cool.mat` from `proj_trans_lightlead`. That clone is the V5 **pre-render variant** for Red's pick (`20_variation_lightlead.md`, 17:20). Red has not picked.
- **Codex additions:**
  - a `Borders` flag (E). The dead/dim border-lamp rule runs only with `-lightleadBorders` or `-lightleadSoft`;
  - a `Troffer_Lens_Cool` SurfaceDef in RenderSetup (X);
  - the folder meta, fresh (X).
- **Live by default:** `FrontRoomsTransitionLightLead.On` and `FrontRoomsTransitionKit.B0` are true unless `-transitionsOff` is passed. In Red's game:
  - Office lamps are cool (0.90, 0.96, 1.00) at 6.0 instead of 5.5;
  - Office fixtures use `Troffer_Lens_Cool`;
  - mixed-finish corner posts follow B0.1, and border walls split the grime band (B0.2).
- **A look nobody reviewed.** V5 with borders off is a combination that no V5 render shows.
- **Why Codex added B0.** In Codex's own reply, B0.1 was its fix for the vertical seam at a corner post that Red asked Codex about at 13:09.
- **Determinism:** B0 compares finishes for equality only. With borders off, the lamp rolls equal the base.
- **Owed:** Red's N1 pick (Figma "FRONTROOMS · LEVEL TRANSITIONS"). After the pick: the visual files plus a MapWorld contract. B0 could be offered on its own as the seam fix.

### 3.6 Map edits: `FrontRoomsMapWorld.cs` (map-owned; 11 hunks, all Codex)

| # | Line (HEAD) | Hunk | Track | Basis |
|---|---|---|---|---|
| 1 | 348 | `ThemeMaterials.lampColor` field | N1 V5 | `15_var_lightlead.md` appendix |
| 2 | 485 | removed `FrontRoomsMetalGlassRTController.Ensure()` call (`75cfdff`) | G14 | exactly G14 contract C1 (`rt/10_rt_glass_design.md:377`) |
| 3 | 981–995 | B0.2: per-side height blocks passed to `BuildEdge` | N1 B0 | appendix |
| 4 | 1092–1127 | B0.1: `CornerReach`, `PostPieceAt` | N1 B0 | appendix |
| 5 | 1134–1181 | `BuildEdge` rewrite: per-skin reach and blocks | N1 B0 | appendix |
| 6 | 1296–1305 | `RaiseWindowBuilt`: direct `DressWindow` call; early return moved | W1 | Codex's own; not the r3 contract |
| 7 | 1522 | `light.color = theme.lampColor` | N1 V5 | appendix |
| 8 | 1539–1545 | border lamp rule after the tier roll (behind Codex's `Borders` gate) | N1 V5 | appendix + Codex gate |
| 9 | 1556–1568 | `BorderThemes` helper | N1 V5 | appendix |
| 10 | 2886–2889 | mirror of the rule in the lamp predictor (behind `Borders` gate) | N1 V5 | `15_var_lightlead.md:171` asked for it |
| 11 | 3148–3158 | `BuildMaterials`: V5 colours, intensities, cool lens | N1 V5 | appendix |

MapWorld now refers directly to 3 visual-owned classes: `FrontRoomsInteractableKit`, `FrontRoomsTransitionKit` and `FrontRoomsTransitionLightLead`. No map-chat doc mentions these hunks; `MAP_GENERATION.md` was last changed in `df4cb03` (17:27). All 11 hunks need to become contract patches for the map chat.

### 3.7 Other modified files (hunk by hunk)

| File | Hunks | Owner per hunk |
|---|---|---|
| `Editor/Rendering/FrontRoomsRenderSetup.cs` | L44 `FrontRoomsGlassSetup.EnsureAll()` (glass §5; Codex re-typed the comment) · L317 `Troffer_Lens_Cool` SurfaceDef | glass (Codex applied) · Codex for N1 |
| `Editor/Rendering/FrontRoomsKitImporter.cs` | L74–91: authored `lodDistances` → LOD thresholds | Codex (implements `interactables/10_spec.md` P-1) |
| `Scripts/Office/FrontRoomsKitLibrary.cs` | L59 `float[] lodDistances` on `Info` | Codex (P-1, optional) |
| `Scripts/FrontRoomsRoomStream.cs` | L49 `DoorOpenSeconds` public · L351–352 `SetZoneReflection(Level0, 0)` | `a7b0dbb` other session · glass §4.3 (copied by Codex) |
| `Scripts/Rendering/FrontRoomsLook.cs` | 3 hunks: comment, `SetZoneReflection` body, `Reapply()` | glass track (= `promote_src` file) |
| `Scripts/Rendering/FrontRoomsPostStack.cs` | L74 `FrontRoomsGlassRT.OptIn(camera)` | G14 (= `proj_rt`) |
| `Settings/FrontRooms_URP_Renderer.asset` | `prepassShader` field | G14 (= `proj_rt`) |
| `Scripts/Rendering/FrontRoomsMetalGlassRT.cs` | −395 lines → shim | G14 (= `proj_rt`) |
| `Scripts/Rendering/FrontRoomsMetalGlassRTRendererFeature.cs` | +252/−… new trace pass | G14 (= `proj_rt`) |
| `NativePlugin/FrontRoomsMetalGlassRT.mm`, `build_…sh` | replaced (1,491 and 47 lines changed) | G14 (= `proj_rt`) |
| `Plugins/macOS/libFrontRoomsMetalGlassRT.dylib(.meta)` | binary replaced; meta gains PluginImporter (Editor + OSX ARM64) | G14; Codex whitespace |
| `Resources/Surfaces/Prop_Glass.mat`, `Prop_BottleBlue.mat` | URP/Lit → `FrontRooms/Glass` | glass §4 planned overwrite |
| `Scripts/FrontRooms3DGame.cs`, `Documentation/TITLE_SEQUENCE.md` | logo relay clock | `a7b0dbb` other session |
| `Documentation/VISUAL_CHAT_TASKS.md` | rows W1.4, W1.5, N1, G1, G14, GD3 + "Clone merge audit" bullet | Codex |

**KitImporter defect.** It maps `lodDistances[i]` to `lods[i]`. The sidecars give `[d01, d12, dcull]`, and the FBX files have 2 LODs. So the last (cull) threshold takes d12 instead of dcull:
- door frames and leaves, and window frames, would vanish at 12 m;
- blinds would vanish at 10 m;
- the old default was about 69 m for a 2.17 m frame.

Only `Kit_MiniBlind_Lowered` has been imported with the new code so far (19:27 in Red's log). Every other kit FBX was imported at 18:55–18:57, before the change. Any reimport (Library rebuild, Reimport All, another machine, a build server) would apply it.

### 3.8 Docs and tools

- **`VISUAL_CHAT_TASKS.md` (X, Codex).** Errors:
  - G14 says "the map opts the post-stack camera into `FrontRoomsGlassRT`". It is `FrontRoomsPostStack.ConfigureCamera`.
  - G14 blames a missing Metal toolchain for the lack of acceptance. The dylib is prebuilt, and it loads and runs in Red's editor.
  - W1.4 does not say that the map trims are still drawn, or that r3 was never tested.
  - N1 drops "Red confirms before anything is implemented".
  - The "Clone merge audit" bullet calls Codex-made GUIDs "fresh Unity meta files". It also says no other production paths are missing. It misses `proj_rt`'s glass shader variant and the r3 map contract.
  - G1 and GD3 are accurate. `proj_gd3` has no `FrontRoomsGlassBreakable` class.
- **Tools:**
  - `Tools/lookdev/pack_glass_grime.py` is V (glass);
  - `NativePlugin/tools/frglassrt_validate.mm` is W (G14).

### 3.9 Other session

- `a7b0dbb` (Claude, via GitHub) changed `FrontRooms3DGame.cs`, `RoomStream.cs` L49 and `TITLE_SEQUENCE.md`. It is not Codex and is out of scope.

---

## 4. GUID check (FBX/JSON and other metas)

| Group | Files | Main vs clone |
|---|---:|---|
| Glass metas | 22 | same GUIDs as `proj_glass` |
| G14 file metas (incl. dylib) | 10 | same as `proj_rt` |
| G14 + N1 folder metas | 4 | **fresh** (Red's Unity), differ from `proj_rt` / `proj_trans_lightlead` |
| N1 file metas | 3 | same as `proj_trans_lightlead` |
| Window kit metas (+ facade `.cs.meta`) | 10 + 1 | same as `proj_win` |
| `Kit_MiniBlind_Lowered` metas | 2 | **Codex uuid4** |
| Interactables metas | 106 | **fresh** (Red's Unity 18:57); `proj_int` has none; `proj_3view_int` differs |

Nothing in scenes or prefabs refers to the fresh GUIDs yet: the kits load by name through `Resources`. **Rule for every merging workflow: keep main's `.meta` files and never copy a clone's meta over them.**

---

## 5. Red's Editor.log (19:06–20:01, read only)

| Log lines | Event | Errors |
|---|---|---|
| 1–539 | editor start; 5 window JSONs re-imported (Codex placement edit) | CS0618 (`Ensure()` obsolete) and CS0414 (`loggedOn`) warnings |
| 539–39,365 | 2 Play sessions on `8ef5b64` code | **85** "Collection was modified" in `FrontRoomsGlassRTSystem.DropChunk`, then **253** ZBinningJob `InvalidOperationException` as a knock-on |
| 39,365–39,596 | recompiles for `9e754a2` and `75cfdff`; `Kit_MiniBlind_Lowered` imported | warnings gone after 39,596 |
| 39,670–50,551 | 1 Play session on HEAD; map found twice | **0** of either error |

So Codex's `DropChunk` fix resolved both errors in this session. That session's chunk teardown coverage is unknown.

---

## 6. The pre-Codex landing inventory (`landing_audit/01_inventory.md`): what Codex changed

| Item | Before (17:4x) | After Codex |
|---|---|---|
| B1 zone reflections stub | in main, did nothing | **live** (glass V) |
| B2 map window/glass seam | map drew its own 30 mm pane | facade hangs the kit frame + `Glass_Window` slab through Codex's direct call; map trims kept; `FrontRoomsGlassBreakable` still absent |
| B3 ChatGPT RT prototype | never started in the real game | replaced by G14 (W); `Ensure()` removed; G14 now **starts** from PostStack and runs, with no window-glass coverage |
| C1 glass track | clone only | promoted, V, live |
| C2 W1 window frames | clone only | promoted: models V, sidecars E, facade W/E; live |
| C3 doors/locks/keys/signage | clone only | promoted W (53 assets), inert |
| C4 GD3 | lab only | not promoted (correct) |
| C5 G14 | clone, running | promoted W, live; the inventory's warning to reconcile the two glass shaders was ignored |
| C6 N1 transitions | 5 pre-render variants | **V5 + B0 promoted and on by default**, no pick |
| C7 Run!, C8 Hunter, C9 key icon | — | not touched |
| D1 dress hitch, D2 look-dev tools, D3 outlets, D4 placard | — | not touched |

---

## 7. Verification images

This task builds a provenance map only. It made no renders, so nothing goes to the Figma "FRONTROOMS · VISUAL VERIFICATION LOG". The checks that need images are the window double frame, G14 coverage, and the N1 look against the base. Those belong to the fix stages and the owning workflows.

## 8. Files

- `00_files.tsv`: one row per path. Columns: git status, commits, main mtime, track, class, source clone@mtime, source state or verifying report, meta GUID check, owner, live in Play Mode, note.
- Scratch evidence (not in the project): `<scratchpad>/ca/` holds `match.json`, `cand_sha.txt` and `codex_merge_full.txt` (Codex's merge commands, extracted from its session log).
