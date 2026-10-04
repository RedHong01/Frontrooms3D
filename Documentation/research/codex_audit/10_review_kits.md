# Codex audit · 10 · Review: interactables kits + window kit

Date: 2026-10-03, 22:25–23:1x PDT. Track: every `Assets/Resources/Props/Models/Kit_*` file that Codex promoted, `FrontRoomsKitImporter.cs`, `FrontRoomsKitLibrary.cs`, and the window facade `FrontRoomsInteractableKit.Window.cs` with its map hook.

Red's project was only read. Unity was not opened on Frontrooms3D. Nothing was committed. The only file written there is this report. The Unity probe ran in my own minimal clone, `<scratchpad>/proj_cx_kits` (8 kits, the importer, the kit library, a probe script). `<scratchpad>` = `/private/tmp/claude-501/-Users-redwang-Desktop-ArtCenter-Fall26T7-EGAM-401A-01-Individual-Game-Project/5656cffd-bc90-45f6-86a3-09b26549df8d/scratchpad`. Scratch evidence: `<scratchpad>/cxk/`.

Base `7320ed1`, HEAD `75cfdff`. Provenance: `00_main_state.md` §3.3–§3.4 and `00_files.tsv`.

---

## 0. Short answer

- **Nothing in the kits breaks Red's game today.** The 53 door, lock, key and sign kits are inert. The 6 window kits are live, and they work.
- **One latent defect will damage Red's windows on the next import.** Codex's `FrontRoomsKitImporter` change culls every door frame, door leaf and window frame at **12 m**. The spec says never cull. Red's Library still has the old import, so the bug is not visible yet. Any re-import shows it, and the owning workflows are about to merge new window FBX files (F1).
- **The facade in main is r3 plus 2 comment lines from Codex.** It runs correctly: no colliders in the opening, the pane collider is unchanged, the 6 mm slab sits on the pane, the side themes are right, and every section number matches the sidecars.
  - Of the engineering critic's 11 issues, 6 are fixed or superseded in main, 1 is partly fixed, and 4 are still open.
  - Of the art critic's 14 issues, 0 are fixed and 1 is partly fixed. They are now visible in Red's Play Mode (F3).
- **Files match their source.** All 118 kit FBX/JSON files equal `proj_int` as it was when Codex copied it. Since 22:0x the owning workflows have rebuilt 7 of them in `proj_int` (§1).
- **The rest checks out:**
  - The `null` inside six sidecars' `lodDistances` is harmless: Unity reads it as 0 (probe, §5).
  - No GUID appears twice in the project's 1,138 `.meta` files.
  - The case-insensitive `Kit_Keyboard` clash cannot happen: main has no two paths that differ only in case.
  - The `Kit_MiniBlind_Lowered.fbx.meta` change that was uncommitted is whitespace only. Red committed it in `03c43ed` at 22:48.

| # | Severity | Finding | Owner | Hand-off |
|---|---|---|---|---|
| F1 | major | Importer culls door and window frames at 12 m (spec: never) | visual | window-landing (fix diff ready) |
| F2 | minor | Map trims still drawn under every kit frame (hidden overdraw, 0.5 mm coplanar faces) | map (contract) | window-landing (r4 "T") |
| F3 | decision | Windows went live before the doors and before the critics' fixes, which breaks "one grammar" in play | Red | window-landing, interactables-kit |
| F4 | minor | 19 kits use 4 slots that have no material in main | visual | interactables-kit (P-4), exit-sign |
| F5 | minor | `Kit_KeyCabinet` in main is the faulty build (inside-out corner faces) | visual | interactables-kit (G3 §0.3) |
| F6 | decision | The key board ships as `Kit_KeyHookBoard`, but the proposal Red reads calls it `Kit_KeyRack` | Red | interactables-kit |
| F7 | minor | `VISUAL_CHAT_TASKS.md` misstates the window and kit state | docs | visual chat |

---

## 1. The kits: where each group came from and where it stands

59 assets = 118 FBX/JSON files + 118 metas. I compared sha1 against `proj_int` and `proj_3view_int`. Every row matched `proj_int` at copy time (17:23 is the last `proj_int` change before 19:10).

| Group | Assets | In main | State of the source when Codex copied it | State now (22:4x) |
|---|---:|---|---|---|
| G1 doors: frames (3), leaves (6), closer (4), crossbar | 14 | = G1 build 12:57–13:10 | built; render and critic stages never ran | unchanged in `proj_int`. Inert, not verified in engine |
| G2 locks | 18 | = G2 run-2 finals 17:08–17:23 | finished; one render was cut off | G2 run 3 (22:04) confirms main is byte-identical to its finals (`build_g2…md` §9.2). Nothing to merge |
| G3 keys, tags, hosts, signage | 21 | = G3 second run 16:45–17:08 | one model fault not yet found | G3 third pass (22:26 note §0.2): **only `Kit_KeyCabinet.fbx` changed** (F5). Other 41 files are final |
| G4 windows + blinds | 6 | = G4 DONE (16:4x), plus Codex's `"placement": "Wall"` edit in 6 JSONs | verified (`build_g4…md` §0) | being rebuilt now in `proj_int`: new FBX for `_Steel` (300,748 B vs 300,220), `_Steel_Enamel`, `_Alu` (104,156 B vs 161,788); JSONs gain a `wall_decor` tag (22:07–22:27). Merge pending |

**Mid-rebuild when copied:** none of G2. In G3, only the cabinet (its fault was found after the copy). G1 was never render-checked. Main's `Kit_ExitSign`/`_Dead` will be replaced by the exit-sign workflow under the same names and main's GUIDs (`research/exit_sign/10_spec.md` §1, §10.1 item 8). That is a planned overwrite, not a clash.

---

## 2. Checks on the kits

### 2.1 Metas and GUIDs
- 118 metas:
  - 116 were made by Red's Unity at 18:57 with fresh GUIDs. `proj_int` has no metas for these kits, and `proj_3view_int` has different GUIDs for all 59.
  - 2 (`Kit_MiniBlind_Lowered.fbx/.json.meta`) were written by Codex with `uuid4` GUIDs (`75e18a03…`, `af825826…`). The `.fbx.meta` equals `Kit_MiniBlind_Raised.fbx.meta` except for the GUID. Unity accepted them (Red's `Editor.log` lines 39,443 and 39,628).
- **No duplicate GUIDs** in the 1,138 tracked `Assets/**/*.meta` files.
- **Material remaps:** every remap in the 59 new metas points at the `Resources/Surfaces` material of the same name. 0 wrong remaps.
- **Rule for every merge:** keep main's metas. Nothing refers to these GUIDs yet, because kits load by name through `Resources`.
- **The meta change that was uncommitted.** It is in `Kit_MiniBlind_Lowered.fbx.meta` and restores 8 trailing spaces that Codex had stripped. `git diff -w` was empty. Red committed it at 22:48 in `03c43ed`, together with docs and the other workflows' Blender modules. The only `Assets/` path in that commit is this meta, so no game code or model changed. It is harmless.

### 2.2 Import settings (`FrontRoomsKitImporter.cs`, Codex hunk at `:74-91`)
- **Scope.** The new branch runs only when the sidecar has `lodDistances` AND the FBX has a LODGroup with 2 or more levels.
  - Exactly the 59 promoted sidecars carry `lodDistances`. **None of the 54 Office kits do**, so their import is unchanged.
  - Of the 59, 14 FBX have LODs (8 door frames and leaves, 4 window frames, 2 blinds; `_LOD0`/`_LOD1` only, from the FBX node names). The other 45 have no LODGroup, so the branch never runs for them.
- **The defect** is F1.
- **`OnPreprocessModel` is unchanged.** Scale, normals and remaps are the same for all kits.

### 2.3 Sidecar schema vs `FrontRoomsKitLibrary.Info`
- Codex added one field: `float[] lodDistances` (`FrontRoomsKitLibrary.cs:59`). JsonUtility ignores the other new sidecar keys (`glassInterface`, `motion`, `lodRatios`, `lodBudget`, `noCollider`, …).
- Every `Info` field in the 59 sidecars has the right type. The one exception is `null` as the third `lodDistances` entry in `Kit_ExitSign`, `Kit_ExitSign_Dead` and the four `Kit_WindowFrame_*`.
  - **Probe result:** JsonUtility reads `[4.0, 12.0, null]` as `[4, 12, 0]`, and the rest of the object is intact (slots 2, anchors 19, size 1.60 × 1.83 × 0.24).
  - Red's 4 Play sessions ran `Prewarm()`, which parses every sidecar, with 0 JSON errors. So the `null` is harmless. It also means "never cull" under both the r4 importer fix and the spec.
- Anchors: all 59 use `{name, pos[3]}`. Every sidecar's `name` equals its file name.

### 2.4 Kit rules
- `noCollider: true` and `colliders: []` on 59 / 59. No lights.
- **Names.** No existing kit was renamed or overwritten. `Kit_Keyboard.fbx` (sha1 `b1088e8e…`) is unchanged since `9abf01a`. The HEAD tree has no two paths that differ only in case. The open question is the name `Kit_KeyHookBoard` itself (F6).
- **Tris.** Every LOD0 is within ±15 % of `10_spec.md` §9. The largest are `Kit_DoorCloser_Shoe` 570 / 500 (+14 %) and `Kit_Lock_Rose` 2,728 / 2,400 (+14 %). Total for the 59 kits: 137,692 LOD0 and 20,296 LOD1 triangles.
- **LOD groups** follow the spec's interim rule (§1.8): assets of 1.0 m or more have LOD0/LOD1; smaller parts have none.
- **Determinism and seeding.** The pile takes only sidecars with a `pile` block. The map test helpers pick the same kits before and after the promotion: `FloorKit()` → `Kit_BarStool`; `DeskKits()` → `Kit_Credenza` + `Kit_Binders`. I simulated this over the sorted name list, 54 → 113 names. 50 of the new sidecars say `"placement": "Floor"` (locks, keys, tags), but none passes the helpers' size filters.

### 2.5 Materials
19 of the 59 kits use a slot that has no `Resources/Surfaces/<slot>.mat` in main (F4). Every other slot exists.

### 2.6 Not a Codex error, but a cost now
The 59 inert models sit in `Resources`, so they ship in every build, WebGL included (+7.8 MB of FBX source on top of 6.4 MB). `FrontRoomsMapWorld.Prewarm()` (`FrontRooms3DGame.cs:366`) loads all 113 kit models with `Resources.LoadAll` in `Awake`. The landing plan already accepted "merge inert" (`landing_audit/01_inventory.md` C3), so this is a note for the WebGL track, not a finding.

---

## 3. The window kit

### 3.1 What runs in Red's game
- **Facade.** `FrontRoomsInteractableKit.Window.cs` in main = `win_fix/FrontRoomsInteractableKit.Window.cs` (r3, 17:41) + Codex's `daef6c2` header lines 11–12. Diffed: only those 2 lines differ. The r3 file was never compiled or tested in a clone; the 38,538/0 tests were on r2. Red's editor compiles it with 0 errors.
- **Map hook.** Codex's direct call, `MapWorld.cs:1300-1301`: `try { FrontRoomsInteractableKit.DressWindow(this, window, record); } catch (Exception e) { Debug.LogException(e); }`.
  - It runs in `RaiseWindowBuilt`, after the chunk is registered (`:915`), outside `BuildInto`'s guard. So a kit exception can never drop a chunk.
  - The returned bool is ignored, which is why the trims stay (F2).
  - It is a compile-time call, not reflection. Window-landing r4 keeps the direct call as its contract R1 ("a signature change is a compile error, never a silent fallback").
  - The landing plan said no map edit was needed: a `WindowBuilt` handler could have drawn the frame (`landing_audit/01_inventory.md` B2). The MapWorld edit itself is in `00_main_state.md` §3.6, hunk 6.
- **Verified in code:**
  - **Colliders.** `Frame()` spawns with `colliders: false` and kills any `Collider` (`Window.cs:112, 116`). The slab gets no collider (`:175-186`). The importer adds none. So no collider enters the opening.
  - **Pane collider.** It is unchanged. `DressWindow` only sets `paneRenderer.enabled = false` after the slab exists (`:69`), and `windowByCollider` still maps the pane.
  - **6 mm slab.** It is a child of the pane, scaled to 1.391 × 1.642 × 0.006 from the pane's 1.4 × 1.65 × 0.03. Its centre is the pane's (0, 1.175, 0). It uses `Glass_Window`, casts no shadow and receives none, and carries `FrontRoomsMetalGlassTarget`. When the map runs `Kill(pane)` at the shatter, the slab goes with it.
  - **Side themes.** `roomIsB = ZoneOf(a).height == Tall`. Office rooms get `Kit_WindowFrame_Steel`; Level 0 rooms get `Kit_WindowFrame_Wood`. Face A is turned into the room. `_Steel_Enamel` and `_Alu` are not used, so `Door_Enamel` (F4) cannot show.
  - **Stop band and section.** The facade constants (`Window.cs:84-92`) equal the `glassInterface` blocks of all 4 frame sidecars: 0 mismatches over 12 numbers. Checked: lining 0.6995 / 0.3505 / 1.9995; stop line 0.6835 / 0.3665 / 1.9835; stops Z 0.006–0.022; glass edge 0.6955 / 0.354 / 1.996 / ±0.003; slab 1.391 × 1.642 × 0.006.
  - **Scale.** The root is unscaled and the frame spawns at scale 1. The kit origin is at floor level (sidecar `boundsMin.y` 0.2485, `glass_slab` anchor y 1.175).
  - **WebGL.** `WebGLTier` (`:152-160`) forces LOD1 and turns off frame shadows under `#if UNITY_WEBGL && !UNITY_EDITOR` only.
- **Red's `Editor.log`:** 0 exceptions from `DressWindow` or the kit library in the 4 Play sessions (lines 539–50,551).

### 3.2 The critics' issues: what is fixed in main

Sources: the two critic results in the window-landing journal (`wf_59c6e1ca-966`, agents `adf5d448…` engineering and `a4a42aaa…` art). Copies are in `<scratchpad>/cxk/critic_*.json`. The fix stage never ran. r3 (17:41) and Codex's call address some issues; window-landing r4 is running now (`<scratchpad>/win_r4/`, logs 22:13–22:31).

| Critic issue | Status in main | Evidence / owner |
|---|---|---|
| E1 (critical) contract diff stale against the map chat's window model | superseded | main uses Codex's direct call, not the diff; r4 rebases the contract on `75cfdff` |
| E2 (high) facade call not guarded; a throw drops a whole chunk | **fixed** | `MapWorld.cs:1300-1301` try/catch, outside `BuildInto` (read from code). r4's forced-throw test on r4's own hook: 307 / 0, 25/25 chunks built (`win_r4/logs/tf4_base2.log`) |
| E3 (medium) RT target on a disabled pane | partly | the slab carries the target (`Window.cs:186`); the pane keeps the map's target with its renderer off (`MapWorld.cs:1277`). glass-rt-track |
| E4 (medium) reflected signature lacks window, edge and break state | **fixed** | `DressWindow(map, window, record)` |
| E5 (medium) streaming acceptance (autopilot, per-room LOD0 budgets) never run | open | window-landing r4 runs `ap_kit_2554` (started 22:27) |
| E6 (medium) Shift, play-mode rebuild after a break, chunk-border windows not exercised | open in main; r4 covers Shift and border counts | `win_r4/logs/tf4_base2.log` "[WindowTF kit] … revisit shift" lines |
| E7 (low) "58.5 m cull" overstated (far plane 46 m) | **made worse by F1** | with Codex's importer the cull is 12 m, inside the 46 m far plane |
| E8 (low) the RT scan registers LOD0 and LOD1 of every frame | open | glass-rt-track (G14 is now live) |
| E9 (low) generic helper names at class level clash with the door/key partials | **fixed** | nested `WindowParts` (`Window.cs:96`) |
| E10 (low) no stated WebGL tier | **fixed** | `WebGLTier`, gated (`Window.cs:152-160`) |
| E11 (low) docs out of step (S1 approved) | **fixed** in the facade header (`:30-31`) | the LEVEL_MODULE_SPEC row is the map chat's |
| A1 (high) glass does not read (no box projection or blending) | open | `FrontRooms_URP.asset:54-55` both 0. glass track |
| A2 (high) round point-light highlights on the glass | open | `FrontRoomsGlass.shader` = glass-track V. glass track |
| A3 (high) walnut casing and liner fuse into one block | open | main's wood FBX is the 16:45 build. G4 / window-landing |
| A4 (high) steel renders matte black | open | `Prop_SteelBrown.mat` unchanged since `4722918`. visual |
| A5 (medium) authored wear invisible (W2) | open | needs Red's approval |
| A6 (medium) no contact shadow / AO | open | visual |
| A7 (medium) shadow staircase on the steel stops | open | visual (lamp look) |
| A8 (medium) glass grime reads as stains and mould | open | glass track |
| A9 (low) square apron ends | open | G4 |
| A10 (low) black rubber glazing line on the walnut frame | partly | the facade swaps in `Prop_Putty` when it exists (`Window.cs:102-104, 117`); `Prop_Putty.mat` is not in main, so it is still rubber |
| A11 (low) screw channel reads as reeding | open | G4 + map chat |
| A12 (low) walnut grain too weak | open | visual |
| A13 (low) under RT, frames reflect white and LOD-doubled | open | glass-rt-track |
| A14 (low) windows promoted before the G1 doors | **happened** | F3 |

---

## 4. Findings

### F1 · major · The importer culls door and window frames at 12 m; the spec says never
- **Where:** `Assets/Editor/Rendering/FrontRoomsKitImporter.cs:81-86` (Codex, `8ef5b64`; sha1 `6460460a…`).
- **What is wrong:**
  - The loop gives `lods[i]` the value `lodDistances[i]`.
  - The sidecars list `[d01, d12, dcull]`: `[4, 12, -1]` for doors, `[4, 12, null]` for windows, `[3, 10, 30]` for blinds. The FBX files have only 2 levels (`_LOD0`, `_LOD1`).
  - So the last level, which is the cull, takes d12 instead of dcull.
  - `10_spec.md` §9 says `4 / 12 / —` for door frames, leaves and window frames, which means they are never culled.
- **Measured** (my clone, Unity 6000.3.10f1, Codex's importer, `<scratchpad>/cxk/probe_codex.txt`):
  - `Kit_WindowFrame_Wood`: LOD1 at 4.0 m, cull at **12.0 m**. `_Steel`, `Kit_DoorFrame_Wood` and `Kit_DoorLeaf_Veneer`: the same.
  - `Kit_MiniBlind_Raised`/`_Lowered`: cull at 10.0 m (spec: 30 m).
  - Before Codex: window frames cull at 57.6–58.5 m and doors at 66.6–69.6 m, beyond the 46 m far plane (`SightDistance = 2 × 24 − 2`, `FrontRoomsMapWorld.cs:69`; `buildRadius 2` in `FrontRoomsLevel0.asset:44`). See §5.
  - The Editor and builds run at quality level 3 (lodBias 1): `QualitySettings.asset:7`, `FrontRooms3DBuild.cs:146`.
- **Why it is not visible yet:**
  - Codex patched the importer at 19:02. Red's Unity had already imported the kits at 18:55–18:57.
  - Codex did not bump `GetVersion()`, so nothing re-imports.
  - Only `Kit_MiniBlind_Lowered` was imported with the new code (`Editor.log` lines 39,443 and 39,628). No map window uses it.
  - Different machines therefore hold different LOD heights. A fresh clone, a Library rebuild or a build server gets the bug for all 14.
- **What will trigger it:**
  - Merging any of the rebuilt window FBX now in `proj_int` (`_Steel`, `_Steel_Enamel`, `_Alu`, 22:07–22:15) re-imports them in Red's open editor with this code.
  - Then every Office window frame vanishes at 12 m in the Tall halls. The map's flat trims (F2) and the bare slab stay behind.
- **Fix:**
  - window-landing r4 already holds the exact patch: `<scratchpad>/win_r4/FrontRoomsKitImporter.lodcull.diff`, also in `proj_win`.
  - The last level takes the sidecar's last entry; -1, 0 or null means never cull.
  - Measured in my clone: door and window frames are never culled, and the blinds cull at 30 m, as the spec says (§5).
  - `GetVersion() => 2` makes every kit re-import once. That is 113 FBX in Red's editor, about a minute. The 54 Office kits come out identical, because they have no `lodDistances`.
  - **It must land before, or in the same merge as, any `Kit_*` FBX.**
- **Red's call to note:**
  - Reading switch distances from the sidecar at all is part of P-1. That is call (8) on the DW proposal ("approve the LOD change"), still open (`proposal/03_for_red.md:29`).
  - With r4's fix, window frames switch to LOD1 at 4 m (§5) instead of today's 11.7 m in Red's Library.
  - If Red declines (8), the alternative is to restore `7320ed1`'s importer. That is exactly what the window landing verified (38,538 / 0).
- **Owner:** visual (`Editor/Rendering`). **Hand-off:** window-landing (diff ready); interactables-kit for P-1.

### F2 · minor · The map's trims are still drawn under every kit frame
- **Where:** `MapWorld.cs:1211-1220` builds 2 jamb boxes and 1 head box (`TrimFace .07`, `TrimProud .02`, `ModuleUnits.cs:64`) before the window root exists. Codex's call (`:1300`) ignores `DressWindow`'s bool.
- **Effect:**
  - 3 hidden boxes per window: hidden overdraw plus extra shadow casters.
  - Coplanar pairs 0.5 mm apart: the map trim's inner face at |x| 0.700 and the kit lining at 0.6995; the head underside at y 2.000 and the soffit at 1.9995.
  - Fine on desktop Metal (reversed-Z float depth). A z-fight risk at range on targets without reversed Z, such as WebGL.
  - If F1 lands unfixed, these trims are what remains beyond 12 m.
- **Fix (contract, not an edit):**
  - window-landing r4 "T": the map calls `DressWindow` from `BuildEdge` before the trims and builds trims only when it returns false (`<scratchpad>/win_r4/FrontRoomsMapWorld.window-kit.r4-trims.diff`).
  - Until the map chat applies it, today's state is safe: the kit casing covers every trim face (§3.1).
- **Owner:** map (contract). **Hand-off:** window-landing.

### F3 · decision · Windows went live before the doors and before the critics' fixes
- **Evidence:**
  - Every map window in Red's Play Mode now has the walnut ranch casing and stool (Level 0) or the dark-bronze steel frame (Office).
  - Doors in the same halls keep the map's flat box trims, because the G1 doors need the map's door state machine (D1.6).
  - The art critic predicted this "one grammar" break (A14).
  - Its four high issues (A1–A4: glass does not read, walnut fused, steel matte black, round highlights) are unfixed in main (§3.2), so they are what Red now sees.
  - The task file gates W1.7 "In game" on "after Red confirms the DW proposal (D1.3)". D1.3 is WAIT-RED.
  - Red's "merge as you go" order lets waiting items land "with their defaults", but only as compiled, verified files. r3 was never tested in a clone.
- **Options for Red:**
  - (a) Keep the window kit live and record the interim door/window mismatch in the DW proposal.
  - (b) Switch it off until doors land. r4 adds `DisableWindowDressForTools`; a one-line default, visual-owned, with no map change.
- **Recommendation:** (a). It is a clear gain over the bare pane, and the tests pass. But the A1–A4 fixes belong to the next window-landing / G4 pass, before Red judges the look.
- **Owner:** Red. **Hand-off:** window-landing, interactables-kit.

### F4 · minor · 19 kits use 4 slots that have no material in main
- **Missing:**
  - `Door_Enamel`: `Kit_DoorLeaf_Steel`, `_SteelLite`, `Kit_WindowFrame_Steel_Enamel`.
  - `Prop_KeyTagNo`: 9 key tags, 3 number plates, `Kit_KeyHookBoard`, `Kit_KeyCabinet`.
  - `Prop_SignEngraved`: `Kit_DoorSign`.
  - `Run_ExitSign_Dead`: `Kit_ExitSign_Dead`, and the `face.deadSlot` of both exit signs.
- **What happens:** the importer drops the remap (`KitImporter.cs:42-45`), and `ApplyMaterials` keeps the FBX's plain material (`KitLibrary.cs:231-232`). It is not pink, but blank paper, flat grey and dark plates appear where the numbers, engraving and enamel should be.
- **Today:** inert. No window uses `_Steel_Enamel`. The Level Designer palette lists all 59, so a designer could place one.
- **Fix:** P-4 surfaces (`10_spec.md` §8 P-4). The exit-sign workflow owns `Run_ExitSign_Dead` (`exit_sign/10_spec.md` §14, item (a) for codex-audit). Do not add placeholder materials in main: the kits would re-import with them.
- **Owner:** visual. **Hand-off:** interactables-kit (P-4), exit-sign.

### F5 · minor · `Kit_KeyCabinet` in main is the faulty build
- **Evidence:**
  - Main's `Kit_KeyCabinet.fbx` (sha1 `f7e570c7…`, = `proj_3view_int`) has inside-out corner faces at the lip, door pan and card holder, which show as black triangles (`build_g3…md` §0.2 item 1).
  - G3's fix is `proj_int` 22:15 (sha1 `6f35795b…`; 160,748 B vs 161,628). Its JSON is byte-identical.
- **Fix:** copy only that FBX and keep main's meta (GUID `13c85fe1e557741c982832fc03970bc8`), as G3 §0.3 says. It is inert today, so it can wait for the interactables-kit merge.
- **Owner:** visual. **Hand-off:** interactables-kit.

### F6 · decision · Key board name: `Kit_KeyHookBoard` in main, `Kit_KeyRack` in the proposal
- **Evidence:**
  - Main ships `Kit_KeyHookBoard` (sidecar `specName: Kit_KeyBoard`).
  - The summary Red reads says "The new name is `Kit_KeyRack`" (`proposal/03_for_red.md:57`).
  - The task file says "pick one" before promotion (`VISUAL_CHAT_TASKS.md` D1.3). Codex promoted before the pick.
- **Fix:** Red picks. If it is `Kit_KeyRack`:
  - rename the FBX, JSON and both metas together, so the GUIDs stay;
  - update `interact_key_board.py` `NAME`, the spec, the KeySpot `host` values and the proposal.
- No code uses it yet.
- **Owner:** Red. **Hand-off:** interactables-kit.

### F7 · minor · `VISUAL_CHAT_TASKS.md` misstates the window and kit state
- **Errors:**
  - The "Clone merge audit" bullet calls `Kit_MiniBlind_Lowered`'s metas "fresh Unity meta files". Codex wrote them with `uuid4` (§2.1). It also omits that no game code uses the blind (sidecar `decorOnly: "Kit_InteriorWindow (G5); never on a breakable map window"`).
  - W1.4 does not say that r3 was never compiled or tested in a clone, or that the map trims are still drawn (F2).
  - W1.5 says "Ray tracing (G14) targets the visible slab". In main, G14 has no window coverage (`00_main_state.md` §3.2).
  - D1.4 still calls the kits "pre-renders for the proposal, not landing", although 53 are in main (inert).
- **Owner:** docs. **Hand-off:** visual chat (task file).

---

## 5. Probe (my clone, `proj_cx_kits`)
- **Setup:** Unity 6000.3.10f1, `-batchmode -nographics`. 8 kits copied from main (`Kit_WindowFrame_Wood/_Steel`, `Kit_DoorFrame_Wood`, `Kit_DoorLeaf_Veneer`, both blinds, `Kit_ExitSign`, `Kit_Lock_Knob`), with main's `FrontRoomsKitLibrary.cs` and a `FrontRoomsSurfaces.TryGet` stub. `Assets/Editor/CxKitProbe.cs` parses three sidecars, force-re-imports each FBX and prints the LOD heights as distances at lodBias 1 (FOV 76°).
- **Three runs**, one per importer: `7320ed1` (pre), HEAD (Codex), window-landing r4. Outputs: `<scratchpad>/cxk/probe_{pre,codex,r4}.txt`; logs alongside.

**Sidecar parse.** The result is the same in all three runs:
- `Kit_WindowFrame_Wood`: `lodDistances=[4, 12, 0]`, slots 2, anchors 19, size (1.60, 1.83, 0.24).
- `Kit_ExitSign`: `[3, 10, 0]`.
- `Kit_Lock_Knob`: `[1.5, 4, 12]`.
- So JsonUtility reads `null` as 0 and does not throw.

**LOD switch distances** in metres at lodBias 1. "never" means a cull height of 0.

| Kit (LODGroup size) | Spec §9 (d01 / d12 / cull) | Pre-Codex `7320ed1`: LOD1 / cull | **Codex HEAD**: LOD1 / cull | r4 fix: LOD1 / cull |
|---|---|---|---|---|
| `Kit_WindowFrame_Wood` (1.828) | 4 / 12 / — | 11.7 / 58.5 | 4.0 / **12.0** | 4.0 / never |
| `Kit_WindowFrame_Steel` (1.800) | 4 / 12 / — | 11.5 / 57.6 | 4.0 / **12.0** | 4.0 / never |
| `Kit_DoorFrame_Wood` (2.175) | 4 / 12 / — | 13.9 / 69.6 | 4.0 / **12.0** | 4.0 / never |
| `Kit_DoorLeaf_Veneer` (2.080) | 4 / 12 / — | 13.3 / 66.6 | 4.0 / **12.0** | 4.0 / never |
| `Kit_MiniBlind_Raised` (1.550) | 3 / 10 / 30 | 9.9 / 49.6 | 3.0 / **10.0** | 3.0 / 30.0 |
| `Kit_MiniBlind_Lowered` (2.400) | 3 / 10 / 30 | 15.4 / 76.8 | 3.0 / **10.0** | 3.0 / 30.0 |
| `Kit_ExitSign`, `Kit_Lock_Knob` | — | no LODGroup | no LODGroup | no LODGroup |

Today Red's Library holds the pre-Codex column for every kit except `Kit_MiniBlind_Lowered`, which has the Codex column. The far plane is 46 m.

---

## 6. Verification images
None. This review made no renders; the probe is text only. The Figma section `FRONTROOMS · VISUAL VERIFICATION LOG` (2595:6093) gets nothing from this stage. The frame worth capturing goes with whoever lands F1: an Office Tall hall at 4992, looking down a row of windows from 15 m, before and after the importer fix with a forced re-import.

## 7. Evidence files
- `<scratchpad>/cxk/promoted.txt`: the 236 promoted model paths.
- `<scratchpad>/cxk/critic_adf5d44829f1b061c.json` and `critic_a4a42aaac2848ae62.json`: the two window critics, extracted from the journal.
- `<scratchpad>/cxk/importers/{pre,codex,r4}.cs`: sha1 `7dc9f23e…`, `6460460a…`, `a07a68c8…`.
- `<scratchpad>/cxk/probe_*.txt`, `probe_*.log`.
- `<scratchpad>/proj_cx_kits/`: the probe project. Do not reuse it as a full clone.
