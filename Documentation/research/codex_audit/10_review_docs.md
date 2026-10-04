# Codex audit · 10 · Docs track and repo hygiene

Date: 2026-10-03, 22:30–23:10 PDT. Track: docs and repo hygiene. This review only read files. It did not open Unity, commit, push, or delete anything. The only file written is this one.

Base `7320ed1` → HEAD `75cfdff`. Line numbers for `VISUAL_CHAT_TASKS.md` are for **HEAD**. The working tree has 11 extra lines near the top, from the codex-audit bullet, so add 11 there.

---

## 0. Short answer

- Codex changed one tracked doc: `Documentation/VISUAL_CHAT_TASKS.md`.
  - `daef6c2` rewrote 6 rows: W1.4, W1.5, N1, G1, G14 and GD3.
  - `9e754a2` added the "Clone merge audit" bullet.
- It also patched `research/webgl/10_webgl_plan.md` §7.3, a folder git ignores, at 19:27. It reverted that patch at 19:34. The file reads as before: line 675 is `### 7.3 Where it stands (read, not run)`.
- It also rewrote code comments in two files: `FrontRoomsInteractableKit.Window.cs` and `FrontRoomsTransitionLightLead.cs`.
- No other doc changed. Every patch and write is in `scratchpad/ca/codex_merge_full.txt`.
- **All 4 Codex commits are already on GitHub.** `origin/main` reached `daef6c2` at 19:20 and `75cfdff` at 19:45 (git reflog, "update by push"). Any correction is a new commit, so history stays as it is.
- **What holds up:**
  - GD3 really has no production `FrontRoomsGlassBreakable`.
  - The G1 file list is right.
  - The repo has 0 duplicate GUIDs across 1,138 metas.
  - The G14 dylib was built from the committed sources.
- **What does not hold up:**
  - G14 "pending because the local Metal toolchain was unavailable" is false.
  - G14 "the map opts the camera in" is false.
  - W1.5 "Ray tracing (G14) targets the visible slab" is false today.
  - The "fresh Unity meta files" claim is false.
  - "no other missing production paths" is misleading.
  - N1 and W1.4 call unpicked or untested work "PROMOTED" and drop Red's gates.
  - Codex edited 6 rows and left 7 related rows contradicting them.
- **Hygiene:** Codex added nothing large, no LFS issue, no case clash, no iCloud copy under `Assets/`, no orphan meta, and no harness that runs at load. All the hygiene problems below were there before Codex (except H4). They are listed because they keep making git noise during this audit.

## 1. Codex's doc claims vs reality

| # | Claim (HEAD line) | Verdict | Evidence |
|---|---|---|---|
| C1 | G14 L125: Metal runtime, P0/P1 capture and performance acceptance are "still pending **because the local Metal toolchain was unavailable**" | **FALSE** | See below |
| C2 | G14 L125: "**The map** opts the post-stack camera into `FrontRoomsGlassRT`" | **FALSE** | See below |
| C3 | W1.5 L61: "Ray tracing (G14) targets the visible slab"; G14: "renderer asset points at the prepass shader" | **FALSE in effect** | See below |
| C4 | Bullet L18: `Kit_MiniBlind_Lowered` "with **fresh Unity meta files**" | **FALSE** | See below |
| C5 | Bullet L18: "The six audited clones have **no other missing production paths** for Glass/W1/interactables/N1/G14" | **MISLEADING** | See below |
| C6 | Bullet L18: remaining unique files are "private capture/probe harnesses, generated RT build outputs, or unresolved creature variants" | true | See below |
| C7 | N1 L89: "PROMOTED; visual/runtime acceptance pending" | **MISSTATES THE GATE** | See below |
| C8 | W1.4 L60: "PROMOTED; runtime acceptance pending", frame + stop band + slab | **INCOMPLETE** | See below |
| C9 | G1 L110: glass promoted; WebGL stripping in the editor path; runtime verification pending | true, but drops content | See below |
| C10 | GD3 L129: "`proj_gd3` contains the lab/contract harness but no production `FrontRoomsGlassBreakable`" | **TRUE** | See below |
| C11 | Codex's final reply: "Unity C# compile has no CS error"; "GUIDs not duplicated" | true | See below |

**C1 evidence (G14 toolchain claim)**
- The design does not need the offline Metal toolchain. `NativePlugin/build_frontrooms_metal_glass_rt.sh:5-7` says "no offline Metal toolchain is needed; design 10 §1.9, decision D4". The MSL is compiled at runtime.
- The dylib in main embeds MSL that is byte-identical to `FRGlassRTShared.h` + `FrontRoomsGlassRT.metal`: 53,306 B at offset 66,217. Its sha1 is `56a35164…`, equal to `proj_rt_pre1910`'s.
- Red's `Editor.log:5844` and `:39764` read `plugin loaded · caps 0x31ff · on: Apple9, High, kernel 305 ms (async), layout OK`.
- Codex read that log at 19:18, three minutes after writing the row. Its reply said "G14 插件确实成功加载" (the G14 plugin did load successfully). It never corrected the row.
- The "missing Metal Toolchain" lines it saw (`Editor.log:11092, 11135`) are Unity shader-compiler probe noise. The task file's own F1 row (L38) already says so.

**C2 evidence (who opts the camera in)**
- The log stack is `FrontRoomsPostStack.ConfigureCamera` (`FrontRoomsPostStack.cs:74`) ← `FrontRooms3DGame.Awake` (`FrontRooms3DGame.cs:355`).
- MapWorld has no `OptIn` call.

**C3 evidence (G14 traces no window glass)**
- `Glass_Window.mat:55` has `_RTReceive: 1`, so `IsReceiverMaterial` (`FrontRoomsGlassRTSystem.cs:470`) puts the slab on prepass bit 30 (`:707`).
- Bit 30 is drawn with the material's own `FRGlassRTPrepass` pass (`FrontRoomsMetalGlassRTRendererFeature.cs:83, 168`).
- Main's `FrontRooms/Glass` (29,095 B) has no such pass. Only `proj_rt`'s 42,863 B variant has it (`:517`).
- So window glass gets no RT input, while the trace still runs every frame. This is reasoned from code; a capture is needed to confirm.

**C4 evidence (Kit_MiniBlind_Lowered metas)**
- Codex wrote both metas with `uuid.uuid4().hex` at 19:27 (session log 02:27:25Z). It copied the importer block from `Kit_MiniBlind_Raised.fbx.meta`.
- The GUIDs are `75e18a03b47b404696ced4269a2d17cf` and `af825826b5bf4984acd8b2ad9091a59a`.
- They are valid and unique; Unity imported them at `Editor.log:39443`. They were not Unity-made.

**C5 evidence (missing production paths)**
- Codex's own scan at 19:20 (02:20:33Z) printed `proj_rt DIFF_MAIN Assets/Resources/Rendering/FrontRoomsGlass.shader`. That file is the G-1/G-2/G-3 shader that G14 needs.
- `landing_audit/01_inventory.md:92` said "reconcile the two before promoting".
- The scan did not include `win_fix/`. That folder holds the r3 map contract (`FrontRoomsMapWorld.window-kit.r3-all.diff`, 17:43), which removes the trims.
- The scan only looked at paths matching a name regex: `Glass|Window|Door|Key|Lock|Transition|Break|RT|LightLead|Kit_`.

**C6 evidence (what the remaining files are)**
- The scan lists `Assets/Editor/Audit/*` harnesses, `NativePlugin/build/*` and `Giant_*_door` creatures.
- One gap: it calls the window contract and the G14 shader neither of these, and leaves them out.

**C7 evidence (N1 gate)**
- Codex removed "**Red confirms before anything is implemented.**" (`7320ed1` N1 row).
- `10_transition_plan.md:492` says modules move to Red's project "only after Red's pick".
- `11_var_frame.md:294` says "Red has not picked a variation."
- Codex's own sub-agent (`md_inventory_audit`, 02:23:15Z) told it "N1 目前仍是研究/clone 计划" (N1 is still a research/clone plan) and listed the tests required first.
- In code, `FrontRoomsTransitionLightLead.cs:25` now invents "The provisional N1 pick keeps V5's colour/lens lead on by default".
- Both parts are on by default (`FrontRoomsTransitionKit.cs:26`, `FrontRoomsTransitionLightLead.cs:21`).

**C8 evidence (W1.4)**
- The committed facade (md5 `f3d28037…`) is byte-identical to `win_fix/FrontRoomsInteractableKit.Window.cs` from 17:41:58. That is the r3 fix-stage file, cut off at 17:54.
- `02_tests_frames.md` (17:09) tested r2 (`tf_logs/kit_models_used_r2.txt`).
- `MapWorld.cs:1300` ignores DressWindow's `bool`. The r3 contract has `if (!DressWindowFrame(window, record)) FrameTrims();`. So the map's trims are still drawn under every kit frame.
- The row's own earlier spec (`7320ed1`) says "the map's own window trims dropped for kit windows".

**C9 evidence (G1)**
- The file list matches `30_final.md` §4 (24/24 md5, `00_main_state.md` §3.1).
- `FrontRoomsGlassRTStripper.cs` is in `Editor/Rendering`.
- What the rewrite dropped is under D5.

**C10 evidence (GD3)**
- `grep 'class FrontRoomsGlass*'` over `proj_gd3`, `proj_gd3_shot_pre1910` and `proj_gd3_shot` finds Lab, Contract, Shot, Window and EventsStub classes, but no Breakable.
- The map side is the map chat's pre-Codex reflection seam (`MapWorld.cs:1321-1330`, already in `7320ed1`).

**C11 evidence (compile and GUIDs)**
- 1,138 metas under `Assets/`, 0 duplicate GUIDs.
- 0 unresolved GUID references in the 335 Codex-changed `.mat`, `.asset` and `.meta` files.
- Red's editor compiled with 0 errors (`00_main_state.md` §5).

## 2. Findings

Severity: **major** misleads Red or a merging workflow about what is live or decided. **minor** is accuracy or hygiene.

### D1 · major · G14 and W1.5 rows describe a G14 that is not running, when it is running and tracing nothing
- **Claims:** C1, C2 and C3 above.
- **Effect:** Red and the glass-rt-track merge stage read "not run because no toolchain". In fact G14 runs every frame on Red's Mac (`Editor.log:5844`) and covers no window glass, because main's glass shader lacks the `FRGlassRTPrepass` pass.
- **Fix:** replace the G14 status text (L125) and the W1.5 sentence (L61) with the text in §4.
- **Owner:** docs now; code is handed off to **glass-rt-track** (merge G-1/G-2/G-3 onto the glass track's V shader, then verify).

### D2 · major · The "Clone merge audit" bullet (L18) misreports the metas and hides two owed production pieces
- **Claims:** C4 and C5.
- **Why it matters:** every resuming merge stage reads this file. The bullet tells them nothing is missing, when two pieces are owed:
  - G14's shader hunks (Codex's own scan saw `DIFF_MAIN … FrontRoomsGlass.shader`);
  - the window map contract (r3 in `win_fix`, now r4 in `window_landing/03_contract_map.md`, 22:29).
- **Fix:** replace the bullet (text in §4).
- **Owner:** docs.

### D3 · major · The N1 row drops Red's gate and records an unpicked variant as "PROMOTED"
- **Claim:** C7.
- **Live in Red's game:**
  - Office lamps are (0.90, 0.96, 1.00) at 6.0 instead of 5.5 (`MapWorld.cs:3148-3158`);
  - `Troffer_Lens_Cool` is on;
  - B0.1/B0.2 are on by default.
- **Why the comment matters:** the code comment's "provisional N1 pick" will be read as a decision by the next reader.
- **Fix:**
  - restore "**Red confirms before anything is implemented**" and the WAIT-RED status;
  - say plainly what is live and the flag that turns it off (`-transitionsOff`);
  - reword the comment at `FrontRoomsTransitionLightLead.cs:24-28` to "Codex default, not Red's pick".
- **Owner:** docs. The on/off decision belongs to Red, then level-transitions.

### D4 · major · The W1.4 row and the facade header describe r3 behaviour that main does not have
- **Claim:** C8.
- **Facade header problems:**
  - `Window.cs:10-11` (Codex's edit) and `:28` say the map's guard "keeps today's trims for that window and logs once";
  - in main the trims are kept for every window, not only on failure;
  - `MapWorld.cs:1301` logs every exception (`Debug.LogException`), not once.
- **Gates:** W1.7 (L63) still says in-game waits for "Red confirms the DW proposal (D1.3)", and D1.5 (L75) is WAIT-RED. Yet kit windows have been live since 19:10.
- **Fix:**
  - W1.4 status → "PROMOTED BY CODEX, UNTESTED (r3); map trims still drawn; replaced by window-landing r4 + map contract";
  - W1.7: note that the kit is live before D1.5.
- **Owner:** docs; code is handed off to **window-landing** (r4 already in `03_contract_map.md` §1).

### D5 · minor · Codex edited 6 rows and left 7 related rows contradicting them; it also deleted two of Red's recorded decisions
- **Rows left stale:**
  - G2, G3, G4 (L111–113) say "RUNNING (glass-track)", and G10 (L130) says "RUNNING (glass-track verify)". G1 says all four are promoted.
  - D1.4 (L74) says "These are pre-renders for the proposal, **not landing**" with status RUNNING. 53 of those assets (106 FBX/JSON + 106 metas) are in main, and no row says so.
- **Deleted from G1 (L110):**
  - "**Red decided 2026-10-03 13:3x: the glass lands TOGETHER with the window frames (W1)**, after the DW proposal is confirmed";
  - the face-on caveat ("almost indistinguishable from an empty opening … a gameplay risk");
  - the measured reflectance numbers (8 / 9.5 / 13.7 / 35.8 %).
- **Changed in GD3 (L129):**
  - "The glass lands together with W1" is deleted;
  - status changed from "RUNNING (workflow glass-break-build …)" to "BLOCKED FOR PROMOTION; lab-only", while that workflow is resuming.
- **Fix:** text in §4.
- **Owner:** docs.

### D6 · minor · G1 promoted without the "after promoting" steps, and no row lists them
- `30_final.md:174-179` asks for three steps after promoting:
  - (1) *Set up glass materials*;
  - (2) *Check zone reflection cubes are current*;
  - (3) recapture the 4 cubes once the map uses `Glass_Window` and the Level 0 lens ×1.5 has landed.
- Both conditions for step 3 are now true. The slab has been live since 19:10, and the lens ×1.5 landed at 17:16 (`00_main_state.md` §3.1).
- Main's `Refl_*.exr` are the 11:57 captures. Their md5 matches `proj_glass` (`d6acc839…` for Level 0).
- N1's cool Office lens is also newer than the cubes.
- Step 1 runs `FrontRoomsGlassSetup` in Red's editor. That is Red's or a clone's call, not this audit's.
- **Owner:** visual (glass look); list it in G1.

### D7 · minor · The codex-audit bullet in the working tree has the wrong error counts
- It says "10 'Collection was modified' … and 29 RenderGraph ZBinningJob".
- `Editor.log` has 85 and 253 exception headers (lines 11,173–39,235), and 0 after line 39,365, which is Codex's fix.
- **Fix:** change the numbers to 85 / 253 and add "0 after 9e754a2".
- **Owner:** docs.

### H1 · minor · 153 Python bytecode files are tracked, and every Blender run now dirties git
- **What is tracked:** 153 `*.pyc` under `Tools/Blender/frontrooms_kit/**/__pycache__/` (3.2 MB), since `4722918` (2026-10-02 16:32). This is not Codex.
- **Noise right now:** 16 modified `.pyc` and 3 untracked (`evac_placard*.cpython-311.pyc`). The resuming workflows wrote them when Blender imported modules from Red's tree.
- **Missing rule:** `.gitignore` has no `__pycache__/` or `*.pyc` rule.
- **Fix (Red commits):**
  - add `__pycache__/` and `*.pyc` to `.gitignore`;
  - run `git rm -r --cached Tools/Blender/frontrooms_kit/**/__pycache__`.
- **Fix (workflows):** run Blender with `PYTHONDONTWRITEBYTECODE=1`, or `sys.dont_write_bytecode = True` before the import.

### H2 · minor · Broad ignore rules hide research docs that tracked docs link to
- **The rules:** `.gitignore:5-8` uses `[Bb]uild/`, `[Bb]uilds/`, `[Ww]ebGL/` and `[Ll]ogs/`. These match at any depth (`git check-ignore -v` → `.gitignore:7:[Ww]ebGL/`).
- **Docs missing from git:**
  - `Documentation/research/webgl/`: 7 files, 452 KB, including `10_webgl_plan.md` with WAIT-RED D1–D14. `VISUAL_CHAT_TASKS.md:166` and `ICLOUD_NOSYNC_TASK.md:22` link to it.
  - `Documentation/research/glass/destruction/build/`: 132 MB. It includes GD3's `00_map_contract.md`, `01_setup.md` and `08_shot.md`. `VERIFICATION_LOG.md` links to it.
  - `Tools/audio/build/` and `FMOD/FrontRooms/Build/` are also ignored. The second is FMOD's source bank path, but the banks are also tracked in `Assets/StreamingAssets/FMOD`.
- **Fix (Red):**
  - anchor the Unity output rules to the root: `/[Bb]uild/`, `/[Bb]uilds/`, `/[Ww]eb[Gg][Ll]/`, `/[Ll]ogs/`;
  - decide whether GD3's 132 MB of images and video belongs in git. If not, keep `Documentation/research/glass/destruction/build/{images,video}/` ignored and track the `.md` files.

### H3 · minor · iCloud conflict copies and leftovers are still in the repo
- **Background:** `ICLOUD_NOSYNC_TASK.md` (12:26) has not been carried out.
- **Tracked conflict copies (2):**
  - `AudioSource/FMOD_Library/LIBRARY 2.json`: an older copy, 23,847 B against 26,251 B;
  - `ProjectSettings/ShaderGraphSettings 3.asset`: identical to the original.
- **Untracked or ignored copies (about 430):**
  - 404 `* 2.jpg` under `research/glass/destruction/build/images`;
  - empty `Door 2`, `Foley 2`, `Ambience 2`, `Relay 2` and `Window 2` folders in `FMOD_Library`;
  - more in `Builds/WebGL`, `UserSettings` and `FMOD/.user`;
  - none under `Assets/`.
- **Inside `.git`:** `.git/index 2` … `.git/index 10` (9 copies, up to 16:49; the brief counted 7). This matches the stale `.git/index.lock` that blocked GitHub Desktop at 19:10.
- **Other leftovers, also pre-Codex:**
  - `Assets/_Recovery/0.unity` (+ `.meta`): a tracked crash-recovery scene since `2c4e50f` (10:41);
  - `Assets/Scripts/Rendering/MetalGlassReflection.meta`: tracked, for an empty folder (`edfbc92`).
- **One item in the brief is now fixed:** its "Extra items" note says the RT dylib has an absolute install name. That note is stale. `otool -D` gives `@rpath/libFrontRoomsMetalGlassRT.dylib` (the `7320ed1` dylib had the absolute path).
- **Fix (Red):**
  - run the nosync task;
  - `git rm` the 2 tracked copies, the recovery scene and the orphan folder meta;
  - update `ICLOUD_NOSYNC_TASK.md`.

### H4 · minor · Codex stripped Unity's trailing spaces from 3 metas; Unity puts them back
- **Files:**
  - `Kit_MiniBlind_Lowered.fbx.meta` (Unity rewrote it at 19:45; this is the 1 uncommitted change under `Assets/`, whitespace only, 8 lines);
  - `Kit_MiniBlind_Lowered.json.meta`;
  - `libFrontRoomsMetalGlassRT.dylib.meta`.
- **Evidence:** each has `userData:`, `assetBundleName:` or `assetBundleVariant:` with no trailing space.
- **Effect:** the last two will show up as modified on their next reimport. It is harmless.
- **Fix:** Red commits Unity's version when it appears. Never "fix" metas with `git diff --check`.

## 3. Checked and clean

| Check | Result |
|---|---|
| Large binaries vs LFS | Codex added 18.0 MB over 335 paths. The largest is `GlassGrime_M.png` at 3.1 MB; the 4 EXR cubes are 1.1–1.3 MB each; the dylib is 201 KB. `.gitattributes` has no LFS rules, and none are needed. Don't add any: GitHub Pages serves LFS pointers (`VISUAL_CHAT_TASKS.md:165`). The repo's largest file is older: a 56 MB room-tone WAV (sound chat, under GitHub's 100 MB limit). |
| RT build outputs | `NativePlugin/build/` (embedded header, validator binary) is ignored by `[Bb]uild/` and is not in main. The dylib is the shipped binary and matches its sources (C1). |
| Harnesses that run at load | Two `[InitializeOnLoad]` harnesses: `FrontRoomsGlassVerification.cs:31` (on the glass promotion list) and `FrontRoomsGlassRTVerify.cs:31` (G14, no promotion list). Both hook `EditorApplication.update` only when a `SessionState` flag is set, and only their `RunBatch` sets it. Inert in Red's editor. If someone runs `FrontRoomsGlassRTVerify.RunBatch` on main, it writes frames into tracked `Verification/rt_p0p1/`. Left to glass-rt-track. |
| Two WebGL strippers | `Editor/RT/FrontRoomsGlassRTWebGLStripper.cs` (prepass shader + pass) and `Editor/Rendering/FrontRoomsGlassRTStripper.cs` (`_FR_GLASS_RT` keyword) complement each other. Both return early unless the target is WebGL. |
| Metas without assets / assets without metas | Under `Assets/`: 0 from Codex. 1 tracked orphan from before (H3). 12 files inside FMOD `.bundle` folders have no metas, which is normal. |
| Case-insensitive path clashes | 0 among tracked paths and folders. The 113 `Resources` stem pairs are all `.fbx` + `.json` sidecars, which load by type. |
| Duplicate / dangling GUIDs | 0 duplicates in 1,138 metas. 0 unresolved references in Codex-changed assets. |
| `.txt` copies of C# in `Documentation` | Present (`window_landing/*.cs.txt`, `glass/promote_src`). Fine, as the brief says. |
| `10_webgl_plan.md` revert | Exact. The heading at line 675 matches the pre-Codex text, and no "Historical pre-G14" text remains. |

## 4. Proposed replacement text for `VISUAL_CHAT_TASKS.md` (for the fix stage or the orchestrator)

**Bullet L18 (Clone merge audit):**
> **Clone merge audit 2026-10-03 (Codex, corrected by codex-audit):** Codex added `Kit_MiniBlind_Lowered.fbx/json` at 19:27 with two metas it wrote itself (uuid4 GUIDs `75e18a03…`, `af825826…`, importer block copied from `Kit_MiniBlind_Raised`); placement Wall, like `Kit_InteriorWindow`. Still owed and not in main: G14's `FrontRooms/Glass` hunks G-1/G-2/G-3 (the `FRGlassRTPrepass` pass; `proj_rt` 42,863 B vs main 29,095 B) and the window map contract (window-landing r4, `research/interactables/window_landing/03_contract_map.md`). Full list: `research/codex_audit/00_main_state.md` and `20_findings.md`.

**G14 L125, status text:**
> Codex copied `proj_rt` into main at 19:10 while the implement stage was unfinished (interrupted 17:54; no verify report): controller, RenderGraph pass, prepass shader, plugin sources + dylib (built 17:53 from the same sources), renderer binding, both WebGL strippers, and the `Ensure()` shim. It **runs** on Red's Mac (`Editor.log`: `plugin loaded … on: Apple9, High, kernel 305 ms (async), layout OK`). `FrontRoomsPostStack.ConfigureCamera` opts the camera in. It traces **no window glass** yet, because main's `FrontRooms/Glass` lacks the `FRGlassRTPrepass` pass (G-1…G-3 not merged). No offline Metal toolchain is needed (MSL is compiled at runtime, design D4). | PROMOTED (WIP) → glass-rt-track: verify, fix, merge G-1…G-3, keep main's DropChunk fix and folder GUIDs |

**W1.5 L61, last sentence:**
> Ray tracing (G14) will target the visible slab once G-1 is on the glass shader. Today it traces nothing (see G14).

**N1 L89:** restore the `7320ed1` text, including "**Red confirms before anything is implemented.**". Then append:
> **Codex 19:10:** V5 Light lead (Office lamps 0.90/0.96/1.00 at 6.0, `Troffer_Lens_Cool`) and B0.1/B0.2 are in main and ON by default. `-transitionsOff` turns both off. Border lamps change only with `-lightleadBorders` or `-lightleadSoft`. No render shows V5 without borders. | WAIT-RED for the pick (Codex default live) |

**W1.4 L60, status:**
> PROMOTED BY CODEX, UNTESTED: r3 facade (17:41, never compiled or tested; `02_tests_frames.md` is r2) plus Codex's direct `DressWindow` call, which ignores the result, so the map's trims are still drawn under every kit frame. Replaced by window-landing r4 and its map contract.

**W1.7 L63:** append
> Kit windows have been live in the game since 19:10 (Codex), before D1.5.

**G1 L110:** restore Red's 13:3x decision, the face-on caveat and the reflectance numbers. Then append:
> Owed after promotion (`30_final.md` §4): run *Set up glass materials*, run the cube check, and recapture the 4 zone cubes (main's are the 11:57 captures, older than the Level 0 lens ×1.5 and N1's cool lens).

**G2–G4, G10 (L111–113, L130), status:**
> DONE (proj_glass 13:1x); promoted 19:10 (Codex)

**GD3 L129:** restore "The glass lands together with W1". Set the status to:
> RUNNING (glass-break-build); no production `FrontRoomsGlassBreakable` yet, so nothing promoted

**D1.4 L74:** append
> Codex copied 53 of these pre-renders (106 FBX/JSON; metas generated by Red's Unity at 18:57) into main at 19:10. They are inert: no code uses them. The verified finals merge over them, keeping main's metas.

**Codex-audit bullet (working tree):** "10 … 29" → "85 … 253 (0 after 9e754a2)".

## 5. Verification images

This track made no renders or captures, so nothing goes to the Figma "FRONTROOMS · VISUAL VERIFICATION LOG". The checks that need images belong to other tracks and the owning workflows:
- the double window trims;
- G14 coverage;
- N1 compared with the base.

## 6. Scratch evidence (outside the project)

- `scratchpad/ca/codex_merge_full.txt`: Codex's commands and replies, from its session log.
- `scratchpad/ca_docs_changed.txt`: the 335 changed paths.
- `scratchpad/ca_docs_tree.txt`: `git ls-tree -r -l HEAD`.
- `scratchpad/ca_vct_head.md`: `VISUAL_CHAT_TASKS.md` at HEAD.
