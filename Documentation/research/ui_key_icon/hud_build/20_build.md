# 20 · Build: FrontRoomsKeyHud (B key, L3·D, B·4, E·1 look-eye, P4, U·3)

游戏视觉 build workflow, build stage. The first run was cut by a network outage on 2026-10-21 22:3x; this stage resumed it on **2026-10-22 06:52–07:10** and finished the code. No images were made here, so no VL row was written (VL297–VL304 stay reserved for the capture stage).

**Inputs read:**

| Input | Version |
|---|---|
| `hud_build/DECISIONS.md` | 2026-10-21 21:46, entries #1–#20. Read at the start and again at 07:04, before returning: unchanged. |
| `hud_build/10_design.md` | 10-14 17:26 (the design; this build follows it except where DECISIONS #13–#20 changed it) |
| `research/computer_ui/30_crosshair_grammar.md` | 10-15 13:54, §2–§8 |
| `~/FrontRoomsVisualWork/keyicon_motion/lab_template_v2.html` | 10-15 13:53, byte-identical to the snapshot `W/hudkey/lab/lab_template_v2_snap1015_1353.html` the port cites |
| Main | HEAD `dfc0086` + working tree, read 06:52–07:04. **Main moved during this stage:** `FrontRooms3DGame.cs` b789cd15 and `.Mobile.cs` 1f6124fa at 06:51 (关卡设计's EXAMINE step 2 in the game), `FrontRoomsMapWorld.cs` 7ef0ca45 at 06:54 (the outlets' wall-face wiring). |

## 1. Where the cut run had got to (inventory)

The 10-21 run had already written almost all of the code; the outage hit after its last compile (c09, 22:39) and before it compared that run's curves or wrote this file.

| Item | State found | This stage |
|---|---|---|
| Registry of crosshair states (#16) | done: `XState` with build / react / leave, latch and leave clock; Lock, Broken, Eye entries | kept |
| E·1 look-eye (#17) | done: `FrontRoomsKeyHudMotion` port + `EyeState` + shader eye | kept; gated again (§4) |
| #13 (d) default + `PanelShotExempt` (#14, #15) | done | kept |
| #18 first key | done on our side: `PickupFlight(..., shotClock, takeOffClock)` | kept; the map's K hold is not in main (O-27) |
| #19 priority, 0.50 s deny | done | kept |
| #20 bind to main's `AimKind`, `StubIdentity`, no MapWorld hunk | done (contract v2.1) | **re-based to v2.2** on today's Game.cs |
| Curve gate incl. E·1 | c07/c08 0 failures; c09 not compared | re-run (c11), 0 failures |
| Shader compile gate | done in c09 (5 targets) | re-run in c11, PASS |
| 10-14 play captures | `p01/p02_desk`: only 1920×1080, lit door: `deny`, `interrupt` and the start of `unlock_b_b` (1,033 crops) before the cut | left for the capture stage |

## 2. Files built

Payload (visual-owned paths only; masters in `W/hudkey/src`, installed in the clone `W/proj_hudkey`; every path is ABSENT in main except `HUD_KeyGlyph.png`, base d450ab42, its `.meta` e32c418b kept):

| Path | sha1 | Lines | What |
|---|---|---|---|
| `Assets/Scripts/Rendering/UI/FrontRoomsKeyHud.cs` | deb0261c | 1366 | the component: seam, registry (Lock, Broken, Eye), panel, P4, U·3 |
| `Assets/Scripts/Rendering/UI/FrontRoomsKeyHudMotion.cs` | 6017ed73 | 479 | pure lab ports, incl. E·1 |
| `Assets/Scripts/Rendering/UI/FrontRoomsKeyHudParts.cs` | 90836aa0 | 73 | part paths, atlas cells (from `W/hudkey/parts`) |
| `Assets/Scripts/Rendering/UI/FrontRoomsHudVectorGraphic.cs` | 0d781fdf | 48 | the one-quad UI graphic |
| `Assets/Scripts/Rendering/UI/FrontRoomsUITextLayout.cs` | 6cc09ae7 | 94 | baselines + tracking for legacy Text |
| `Assets/Resources/UI/KeyHud/FrontRoomsHudVector.shader` | 2d861a9d | 293 | `FrontRooms/UI/HudVector`: dot, padlock, B·4, eye |
| `Assets/Resources/UI/KeyHud/*.png` | | | 58 parts (B ring+tag 9 × 4 densities, key × 4, numbers 3 × 3 × 2) |
| `Assets/Resources/UI/Fonts/CourierPrime-Bold.ttf` + `OFL.txt` | 0e31ca90 | | the panel number |
| `Assets/Resources/UI/HUD_KeyGlyph.png` (replaced, GUID kept) | 3f84e7a5 | | B rect red 14 @2x for the legacy slot |
| + 69 hand-written `.meta` (GUIDs from the path, none used in main) | | | |

136 files in all, all `.cs`/`.shader` ASCII.

Contract (map-owned files, clone only; for 关卡设计), `W/hudkey/contract/`:
- `apply_contract.py` **v2.2** (v2.1 kept as `apply_contract_1021.py`, its bases in `base_1021/`);
- `FrontRooms3DGame.HK1.diff` (Game.cs only, 15 hunks), `KP1_handoff.diff` (Game.cs 3 hunks + DoorShot 2), `FrontRooms3DGame.contract.diff` (both);
- bases: Game.cs **b789cd15**, MapWorld **7ef0ca45** (checked, never touched), DoorShot b1b230e8.

Harness (clone only, never packaged), `W/hudkey/harness/Assets/`: `Editor/HudKey/FrontRoomsKeyHudCurves.cs` (curves + 42 registry checks + shader gate), `FrontRoomsKeyHudShaderGate.cs`, `FrontRoomsKeyHudCapture.cs` (play captures), `FrontRoomsKeyHudReimport.cs`, `HudKeyHarness/FrontRoomsKeyHudHarnessBoot.cs`.

Tools: `W/hudkey/pipeline/resync_clone.sh` (rsync of main with `--delete` and the iCloud excludes as `mkclone.sh`, local patches, tools, PlayerPrefs isolation, payload, harness, contract), `W/hudkey/lab/extract_lab.py` + `lab_curves.js`, `W/hudkey/analysis/compare_curves.py`.

## 3. What changed since the 10-14 build

1. **State registry (#16).** Each crosshair state is one `XState` entry (rank, kind, build / react / leave, latch, shared leave clock). Adding a state is one class plus one line in `Build`.
2. **Read state E·1 "Opens from the dot" (#17, grammar H3).** Ported verbatim from the lab's p16 LOOK block (lab lines 974–1081: `EYE`/`DOTEYE` 974–975, `lerpE` 976, `slowBlink` 978, `LKT` 979, `lidD` 986, `eyeDraw` 998–1010, `LOOK[0]` 1012–1017, `lookPose` 1048–1062, `frameLook` 1063–1081).
   - Build: dot → almond 40 × 15, pupil r 8 → 5 in 0.30 s (eio). One slow blink starts at 0.42 s and lasts 0.63 s (shut 220 ms, hold 70 ms, open 340 ms). At rest from 1.05 s. Prompt alpha = x / 0.30.
   - React only on `FrontRoomsInspect.Opened` (also callable as `InspectOpened()`): 80 ms focus (lids −9.5 / +8, pupil r 4), eye and line fade over 50–150 ms, then hidden until `Closed`.
   - Return: `Closed` with the aim still on the target → rest pose fades in over 0.15 s, no blink; otherwise the dot. A 2 s safety net returns it if no `Closed` ever comes.
   - Leave: upper lid shuts in 120 ms, then back into the dot by 300 ms. Cut to 0.12 s when another state or a map prompt waits. A re-aim mid-leave resumes, with no rebuild and no blink.
   - Re-blink only after a fresh build (the aim left) **and** ≥ 1.0 s since the last blink started.
   - Reduce Flashing: no blink, at rest in 0.30 s. Reduce Motion: a 0.15 s alpha cross-fade.
   - `SetAim` gains `Read` / `Examine` and the words as shown (`SetAim(AimKind, id, haveKey, prompt)`). The eye's line is the map's own string ("E · READ PLAN", "READ PLAN" on touch), so the state and its line are one predicate.
3. **#13 (d) is the default:** `PanelMode = D_ShotBeats`; `UnlockStart = B_AfterShot` for the crosshair padlock; (a)/(b)/(c) stay parameters. `PanelShotExempt` is true from `Unlocking()` until the shot's fade has risen and fallen; U·3 starts on `DoorBeat.KeyInsert`. A shot that ends before `BoltRetract` eases the panel key back in 0.15 s and brings the padlock back **closed** (O-23).
4. **#18:** a late take-off overload carries the 3D tag's held swing on from the hold's start. Later keys still take off at `HoldUntil`.
5. **#19:** the order is Hidden (HUD alpha) > a latched reaction (deny 0.50 s, broken swing 0.55 s, unlock / fall, the eye's react) > the map's aimed kind (Locked > Broken > Use > Hold > Read, picked by the map's ray) > Idle. Use and Hold have no crosshair state, but they count as "waiting" for the 0.12 s leave cut.
6. **#20:** the component binds to main's `FrontRoomsMapWorld.AimKind` (all 11 values mapped). `StubIdentity` (a zone hash) stands in until D1.6. No MapWorld hunk is left.
7. **Contract v2.2 (today):** re-based on main's EXAMINE step 2.
   - Three anchors moved: SetPhase's dot and key-panel lines (`hudPhase`), and UpdateHud's prompt line, now an if/else for the close-up hint. The hint is never gated by `OwnsPrompt`.
   - One hunk is new: an EXAMINE target sets the crosshair's kind (`InspectKind.Object` → `Examine`, `Flat` / `Panel` → `Read`), because grammar H1 says the aim registry supplies them.
   - In `Phase.Inspect` the game passes aim None. The eye stays latched in its close-up state, and the shot's `HudFade` hides the crosshair.

## 4. Curve gate (lab JS vs Unity)

- Lab: `extract_lab.py` on the current lab → `lab_curves_1022.js`, run with macOS `jsc` (JavaScriptCore) → `W/hudkey/lab/curves_lab_1022.tsv`. It is byte-identical to the 10-21 `curves_lab.tsv`.
- Unity: `FrontRoomsKeyHudCurves.RunBatch` (c11, 07:00:58–07:01:11, load 310, **UNDER LOAD**) → `W/hudkey/analysis/curves_unity_c11.tsv`. It is byte-identical to c08's.
- `compare_curves.py` → `W/hudkey/analysis/curves_compare_c11.json`: **24,147 samples, 182,913 values compared, 0 missing, 0 failures** (|Δ| ≤ 1e-4, or 2e-6 relative on canvas px).
  - The Unity y flips are applied to the E·1 lid points and the look timelines.
  - 1,551 `btimeline` rows and 1 `lookpress` row are skipped: the lab draws at opacity 0 there, and Unity does not draw.
- E·1 alone: 9,505 rows, 92,179 values, max |Δ| 6.97e-05 (`lookpress` up at 3.89 s).

| Kind | Rows | Max abs. difference | Gate |
|---|---|---|---|
| in / famout / deny / open / fall / geo | 1503 / 963 / 501 / 561 / 1653 / 3 | 1.32e-05 | PASS |
| btimeline (in-context (b)) | 2201 | 4.02e-05 | PASS |
| b4in / b4react / b4out / b4hold | 801 / 551 / 301 / 1 | 3.48e-06 | PASS |
| p4_desk / p4_phone | 1801 / 1801 | 1.78e-04 px (relative rule) | PASS |
| u3 | 2001 | 3.86e-06 | PASS |
| **E·1:** eyein / eyereact / eyeback / eyeout / eyerest / eyegeo / eyelid / blink | 1081 / 151 / 151 / 301 / 1 / 3 / 14 / 801 | 6.61e-06 | PASS |
| **E·1 theatre timelines:** lookaway / lookpress | 2851 / 4151 | 6.97e-05 | PASS |

**Registry checks (c11):** 42 PASS, 0 FAIL, listed in `W/hudkey/analysis/registry_checks_c11.txt`. They cover:
- every E·1 envelope: ≤ 0.25 s, ≤ 0.30 s, the blink ≤ 0.45 s and ≤ 0.63 s, ≤ 1.05 s, the leave ≤ 0.30 s and its 0.12 s cut;
- the re-blink rule, and strobe at 10 Hz and 1 Hz (≤ 1 blink per second; 0 under Reduce Flashing);
- Reduce Flashing and Reduce Motion;
- InspectOpened and InspectClosed: the react only on Opened, gone by 0.15 s, back at rest within 0.15 s with no blink;
- #19 latches and switches;
- (d) exemption and cancel;
- #18;
- #20 mapping and stub;
- zero GC per frame in Idle, Locked, Broken and Read;
- `FrontRooms/UI/HudVector` compiling for Metal macOS, Metal iOS, D3D11, GLES3/WebGL2 and Vulkan (8/8 each).

## 5. Compile

| Check | Result |
|---|---|
| Unity batch, forced reimport of `Assets/Scripts` (c10, `-nographics -quit -buildTarget OSXUniversal`, 06:59:54–07:00:34, load 392, **UNDER LOAD**) | exit 0, **0 `error CS`**, Assembly-CSharp + Editor rebuilt |
| Unity curve harness (c11) | exit 0, 0 `error CS` |
| Roslyn, clone tree `--all` (roslyn22) | COMPILE OK, 7 assemblies, 0 errors |
| Roslyn, main + payload overrides `--all` / `--ios` (roslyn23 / 24) | COMPILE OK / COMPILE OK (iOS defines) |
| Roslyn, main + payload + HK1 `--all` / `--ios` (roslyn25 / 26) | COMPILE OK / COMPILE OK |
| Roslyn, main + payload + HK1 + KP-1 `--all` / `--ios` (roslyn27 / 28) | COMPILE OK / COMPILE OK |
| Roslyn, main alone `--all` / `--ios` (roslyn29 / 30) | COMPILE OK / COMPILE OK |

Logs: `W/hudkey/logs/c10_reimport.log`, `c11_curves.log`, `roslyn22…30*.txt`, `s06_rsync_dryrun.txt`, `s07_resync.log`. The pre-resync backup is in `W/hudkey/backup_1022_pre_resync/`, and the 10-21 wired copies are in `W/_partial/hudkey_contract_1021_wired/`.

## 6. Left for the capture stage

1. **An E·1 script in `FrontRoomsKeyHudCapture`** (none exists yet) through the real game. Steps:
   - aim at an EXAMINE target (the evacuation plan, with `FrontRoomsInspect.DevTargets` if the seed has none);
   - capture the build and the blink;
   - E → `Opened` → `Phase.Inspect` (timeScale 0, which is also the real-clock smoke for unscaled time) → E back → `Closed` → rest with no blink;
   - leave, and switch to a door (the 0.12 s cut);
   - Reduce Flashing and Reduce Motion variants;
   - the first key's Examine eye through `EditorAimOverride` (main has no K hold).
2. **The 10-14 scripts on all three stands** (lit L0, dark cell, Office):
   - deny;
   - interrupt;
   - unlock in the defaults (crosshair (b), panel (d)), (a) with no shot, (c);
   - cancel before the commit in (d);
   - B·4 through the map's tool path;
   - P4 with KP-1 and `ShowPlaceholderKey`;
   - the idle dot.
   The cut run reached only 1920×1080 lit deny, interrupt and the start of `unlock_b_b`.
3. **Sizes:** 1920×1080, 1920×1200, and 2622×1206 forced handheld.
4. **§9.4 image gates:** reference renders, the keyline ring mean (calibrates `KeylineAlpha` .95 and `TextOutlineAlpha` .78, still the uncalibrated 10-14 starting values, O-6), baselines 591/614 (239/259 pt) ±0.5 px, and whole pixels at rest.
5. **Measure the return from a close-up** against the rig's HUD fade (O-25).
6. **VL rows VL297–VL304** to `W/vl_pending/hud_key_b.md`, plus the per-frame `hudlog.tsv` wiring diff.
7. **A fresh-clone READY check:** a Play harness must see the game's READY line within 60 s. c10 forced the reimport, so this clone is ready.

## 7. Open issues

| # | Issue | Owner / default |
|---|---|---|
| O-25 (new) | **The HUD returns late after a close-up.** The rig keeps `HudFade` at 1 while an ending shot still blends out (`FrontRoomsCameraRig.cs:406` counts every live shot). So after E-back the whole HUD, the eye included, comes back only after the Inspect shot's 0.40 s position blend plus the 0.15 s fade, not "within 0.15 s of control returning" (grammar §2). The eye's own 0.15 s return runs at `Closed`, under the hidden group. | 关卡设计 (rig) / 系统设计; the capture stage measures it |
| O-26 (new) | **The eye follows a raw Relay check.** Main's EXAMINE gate `RelayBlocksInspect()` (Game.cs ~1189) uses `relay.PlayerSeesRelay` and the Relay's state, not `ThreatPerceivable`, which does not exist in main yet (#16). The eye follows the prompt's predicate exactly, so the two always agree, but that predicate is the map's to fix. | 系统设计 → 关卡设计 |
| O-27 (new) | **The first-key K hold (#18) is not in main.** No `OpenHeld` call exists in the game or DoorShot. HK1 cannot set `Examine` + "E · EXAMINE KEY" during the hold, and KP-1 still hands off at `HoldUntil`. When the hold lands, the map sets `aimKind = Examine` for it and calls `PickupFlight(..., clock, takeOffClock)` at the hold's end. That API is ready and checked. | 关卡设计 |
| O-28 (new) | **HK1's Read / Examine mapping is a proposal.** It maps `InspectKind.Object` to Examine and Flat / Panel to Read. The eye looks the same either way; the touch verb READ / EXAMINE (grammar H4) depends on it. | 关卡设计 / touch |
| O-29 (new) | **Main moved under this stage** (Game.cs, Mobile.cs and MapWorld at 06:51–06:54). Contract v2.2 is on b789cd15 / 7ef0ca45 / b1b230e8. Re-check them before packaging; `apply_contract.py` refuses on a mismatch. | build / package stage |
| O-1, O-2 | B glyph keyline (none); digit face (Regular, from the masters) | 平面视觉 |
| O-6 | Keyline and Outline alpha in linear light (uncalibrated) | capture stage |
| O-16 | `held_pose/measure.json` absent; P4 start factors 3/3 (1.117 at 402 pt) are parameters, **pending** | 平面视觉 / measure |
| O-17 | HDR output with a custom UI shader | one player build |
| O-20 | #13's optional echo turn is undefined, so it is not implemented (`UnlockEcho` false) | 平面视觉 |
| O-23 | A cancel before the commit brings the padlock back closed (visual chat default; the lab has no cancel) | 平面视觉 to confirm |
| #7, #11 | Strings are not confirmed; one constant each in `FrontRoomsKeyHud.Strings` | Red |
| #20 | `ZoneKeyIdentity` is a stub (`StubIdentity`) until D1.6 | 关卡设计 |
| T-1 / H4 | Touch verbs UNLOCK, BROKEN, READ and EXAMINE, keyed on the kind | touch chat |
| O-13 | Before wiring, `HUD_KeyGlyph.png` shows B in the old 40×22 slot (keys are off in shipped L0) | package stage |

## 8. Packages (2026-10-22 evening, capture stage re-run #2 after account switch #5)

> Interim entry written at 21:5x; §9 holds the capture results once the chain finishes.

- **`hud_style`** (DECISIONS #31): landed 15:52 (LANDINGS, MERGED.md). FrontRoomsKeyHud reads it: KeylineColor, KeylineAlpha 0.95, KeylineDistance and OutlineTapAlpha. Registry check #31 passes.
- **`hud_key_b`** (`W/apply/hud_key_b.sh`, apply convention v2, built by `W/hudkey/pipeline/build_package_hud_key_b.py` from the masters in `W/hudkey/src`):
  - **Writes 161 NEW paths:** 5 C# scripts in `Assets/Scripts/Rendering/UI/`, the `FrontRooms/UI/HudVector` shader, 74 part PNGs, the `KeyHud` folder `.meta`, and 80 file `.meta`s with 81 new GUIDs, none of them used in main.
  - **KEPT (never written):** hud_style's three paths. The package is stacked on hud_style and refuses with "land hud_style first" if they are missing or have changed.
  - **Not touched:** `HUD_KeyGlyph.png` (#27).
  - **Bases:** main HEAD 1521cd81 + working tree. Game.cs 938b0ee9, Mobile.cs 545b57d4, MapWorld ca24a280, DoorShot b2014396; nothing under Assets has changed since the 21:20 clone sync.
  - **Checks:**
    - dry run on main: **DRY RUN OK**;
    - `rebase_apply.sh hud_key_b --no-install --ios`: **PASS**, nothing to rebase;
    - Roslyn `--all`, main + payload: **COMPILE OK on macOS and iOS** (`W/hudkey/logs/roslyn60_*`);
    - Roslyn `--all`, main + payload + the wiring contract: COMPILE OK on macOS and iOS, for HK1 + KP-1 and for HK1 alone (`roslyn61_*`);
    - scratch tests in `W/tools/_test/hud_key_b_1022/t_apply_script_tests.log`: apply gives MANIFEST OK, then ALREADY APPLIED (dry run and `--apply`). The script REFUSES a half-applied target, a GUID clash, a missing hud_style, a tampered payload and a changed HudStyle.cs.
  - **On its own it is inert:** nothing calls `FrontRoomsKeyHud.Attach` until 关卡设计 applies the wiring contract. That contract is `contract/40_contract.md` with three diffs: HK1, KP-1 and both combined. It is v2.5 on Game 938b0ee9 / DoorShot b2014396, and `patch -p1` applies it cleanly to main's files.
- **Curve gate c20** (21:42, UNDER LOAD):
  - 25,832 samples and 196,393 values, 0 missing, **0 failures**;
  - byte-identical to c19;
  - the lab is unchanged against the 10-15 snapshot.
- **Registry checks c20:** **51/51 PASS**. c19's single FAIL ("a canvas-scale change regenerates the prompt line meshes") was a harness bug: it read a destroyed object after `Dispose`. The harness was fixed and the product is unchanged. The checks include the shader compiling 8/8 on each of Metal macOS, Metal iOS, D3D11, GLES3/WebGL2 and Vulkan.

## 9. Capture and image gates (2026-10-24 11:4x–12:3x, x9 run, account A, unattended)

The 10-22 21:4x chain stopped with its agent: its desk run p19 crawled after 21:53 (frames in bursts about 18 minutes apart until 23:25, the night's network-outage window), and nothing after it ran. This stage re-synced the clone to today's main, re-based the contract, ran the captures once, and gated them. The payload did not change (`W/hudkey/src` = `W/apply/hud_key_b_payload` = the clone's files). So c20's curve gate (25,832 samples, 0 failures), its 51/51 registry checks and the shader gate (8/8 on Metal macOS, Metal iOS, D3D11, GLES3/WebGL2 and Vulkan) stay valid and were not re-run.

### 9.1 Bases and the package on today's main

- **Main:** 4049c70b + working tree. Game.cs **2ebd70b9** (11:26, 交互设计: the handheld startup curtain, the desktop settings view, and the phone HUD canvas now ConstantPixelSize at `FrontRoomsHandheld.PointScale` with the corner give-way). Mobile cfa8cd5d, MapWorld ca24a280, DoorShot b2014396, CameraRig b8ba623e, HudStyle 5cef54df.
- **`hud_key_b.sh`:** DRY RUN OK (11:4x and again at 12:34); `rebase_apply.sh hud_key_b --no-install --ios`: PASS, nothing to rebase. Every one of its 161 paths is new, so 交互设计's 10-24 work does not touch it.
- **Roslyn `--all`:**
  - main + payload: COMPILE OK on macOS and iOS (`W/hudkey/logs/roslyn70_*`);
  - main + payload + contract v2.6 HK1 + KP-1: COMPILE OK on both (`roslyn71_*`);
  - main + payload + HK1 alone: COMPILE OK on both (`roslyn73_*`);
  - the re-synced clone tree with the harness: COMPILE OK (`roslyn72_*`).
- **Contract v2.6** (`W/hudkey/contract`, copies in `hud_build/contract/`): every v2.5 hunk applies unchanged to Game 2ebd70b9 (19 Game.cs + 2 DoorShot); only the line numbers moved. `patch -p1` reproduces the tested files byte for byte. v2.5 is kept as `*_1022h_v25*` with `base_1022h_v25/`. 40_contract.md is updated.
- **Phone scale:** the new phone canvas gives scaleFactor = PointScale = 3 on the iPhone 16 Pro, the same as the old 874×402 reference at 2622×1206. FrontRoomsKeyHud reads `canvas.scaleFactor` everywhere and needs no change.

### 9.2 Captures (clone `W/proj_hudkey`, re-synced 11:48 with `pipeline/resync_clone3.sh`; backup first in `W/hudkey/backup_1024_pre_resync4`)

| Run | What | When / load | Result |
|---|---|---|---|
| c21 | reimport `Assets/Scripts` (nographics) | 11:49:05–11:49:20, load 24 | exit 0, 0 `error CS` |
| p22 | desk: 1920×1080 full (plan, lit, dark, Office) + 1920×1200 lite → `Verification/hudkey/cap1024a_desk` | 11:49–12:09, **UNDER LOAD** (1-min load 89–157 at 11:50–11:52) | READY in 2 s, 20 PASS / 2 FAIL. Both FAILs are the 1200 plan stand: the long run had streamed the plan's chunk out, so the stand faced a Zone 05 wall (harness order, not the HUD) |
| p23 | phone: 2622×1206 forced handheld, lite → `cap1024a_phone` | 12:09–12:18, load 7–14 | 7 PASS / 0 FAIL |
| p24 | real-clock smoke → `cap1024a_smoke` | 12:18:50–12:19:27, load 7 | 6 PASS / 0 FAIL |
| p25 | 1920×1200, `eye_gate` + `eye_closeup` alone → `cap1024a_desk1200eye` | 12:19–12:20, load 10–20 | 3 PASS / 0 FAIL |

Chains: `W/hudkey/logs/run_c21p24.zsh`, `run_p25.zsh`, under `caffeinate -i -s`. All clone runs used the test bundle id; no player was built.

### 9.3 Gates

Tools: `W/hudkey/capture4/tools` (the capture3 tools plus `gates_summary2.py`, `o25.py`, `figs3.py`). Results: `W/hudkey/capture4/results`.

| Gate | Numbers | Verdict |
|---|---|---|
| **Wiring** (hudlog vs `curves_lab_f60.tsv` at the same clock) | **294 / 302 pass** (desk 207/212, 1200 eye 30/31, phone 57/59). The failures: <br> - 5 × O-25 (the rig, below); <br> - 2 × the p22 1200 stand (re-run in p25: PASS); <br> - 1 × B·4 on the phone: at rel 48 (0.800 s) th 11.603, the in-curve's last value, and the 12° hold one frame later. 29.77 − 28.97 s falls below 0.8 in float32. This is the lab's own 0.4° IN→HOLD step (O-8), shown one frame late. | PASS except O-25 (map) and the B·4 phone frame (cosmetic; follow-up: an epsilon on that boundary) |
| E·1 envelopes (grammar §2–§5) | Build on the prompt frame; blink at 0.420 s; at rest by 1.05 s; the gate swallows E with no react; cut leave 0.133 s; no re-blink at 0.947 s, re-blink after 1 s (starts 0.42 / 2.92 s); leave 0.300 s; react on InspectOpened; gone 0.150 s; back at rest 0.150 s after Closed with no blink; Reduce Flashing / Reduce Motion; timeScale 0 throughout | PASS (all three sizes) |
| Locked / unlock / cancel / broken / P4 wiring | L3·D, D·3, famOut, F·1 and U·3 match the lab to 0 beyond float32 slack. (b): t_ret 1.450 s, F·1 at +0.10. (d): U·3 on KeyInsert 0.533 s, panel exempt 87 frames. O-23: cancel → padlock closed, key eased back 0.150 s. B·4 exact on desk. P4 takes off on the KeyHandoff frame, factor 3 (3.162 at 1200) | PASS |
| **Real-clock smoke** | The eye builds and blinks at timeScale 0; the close-up and react run on unscaled time; back at rest; the padlock advances 16 distinct u over 73 frames in 1.2 s wall at timeScale 0 | PASS |
| **Layout:** baselines ±0.5 px, whole pixels at rest | 1080: 591.000 / 614.000. 1200: 653.999 / 677.9995 (+0.240). Phone: 717.000 / 777.000. Fractional parts ≤ 0.001 px; measured paper baseline within 0.04 px. 53/53 rows | PASS |
| **§9.4 inside shapes ≤ 3/255** | 0.00 on all 352 opaque frames (sRGB and linear references), all sizes | PASS |
| **§9.4 outside (nothing where the lab draws nothing)** | 0 on every frame except one per size, B·4's build frame rel 12 (20 / 27 / 147): the prompt fading in outside the reference's text mask (gate-tool limit) and a sub-pixel stroke cap | PASS at rest |
| **§9.4 edge band ≤ 12/255** | sRGB reference: at rest, max 112, 48 % of edge px over 12. Linear-light reference: max 112, share over 12 median 1.8 % / p90 4.0 % / max 8.0 % at rest. The outliers sit on curved corners: the shader's analytic SDF coverage against the reference's area coverage. By eye, Unity = reference. 60 frames where the HUD group itself is faded (crossA < 1) were dropped, because the reference has no group alpha. | **FAIL as defined** (FINDING: the sRGB reference cannot match Unity's linear UI blend) |
| **§9.4 keyline ring mean ≤ 2/255** (calibrates `KeylineAlpha`, DECISIONS #22/#31) | Pooled over cap1022e + cap1024a, desk + phone: minimax alpha **0.950**, worst stand 2.25/255 (dark 1.62, lit 2.04, Office 2.25, plan 1.67); per-stand zero-mean alphas 0.933–0.945; this run alone 0.945 (worst 1.97). | FAIL by 0.25 at the best single value. **KeylineAlpha stays 0.95** (already in main; no hud_style change) |
| **Panel gate** (B glyph at rest vs the SVG masters) | Whole pixels; ring 1.60 / 1.51 / 1.58 (linear 1.87 / 1.84 / 1.86): PASS. Inside 13.7 / 39.6 / 11.4 and edge 58 / 60 / 54 (linear 3.9 / 7.0 / 3.3 % over 12): FAIL. The token strokes and the 1 px ring are all edge, plus the blend space | FINDING |
| **O-25** (return from a close-up) | InspectClosed rel 149, control back that frame, HUD visible again at rel 181 = **+0.533 s** (grammar 0.15 s). The eye is at rest at +0.150 s under the hidden group | FAIL, 关卡设计's rig |

### 9.4 Images, MP4s, VL rows

- Rows VL297–VL304: `W/vl_pending/hud_key_b.md`, the reserved numbers only.
- Images in `hud_build/images/`:
  - `VL297_e1_eye_plan_1080.jpg`, `VL298_locked_deny_stands_1080.jpg`, `VL298b_identity_matrix_1080.jpg`;
  - `VL299_unlock_d_b_1080.jpg`, `VL299b_unlock_variants_1080.jpg`, `VL300_broken_b4_1080.jpg`;
  - `VL301_pickup_p4_1080.jpg`, `VL301b_panel_rest_tokens.jpg`, `VL302_sizes.jpg`;
  - `VL303_image_gates.jpg`, `VL304_wiring_smoke_o25.jpg`.
- MP4s (ffmpeg already installed): `VL297_e1_eye_gate_1080.mp4`, `VL297_e1_closeup_1080.mp4`, `VL298_lock_deny_lit_1080.mp4`, `VL299_unlock_crosshair_1080.mp4`, `VL299_unlock_panel_u3_1080.mp4`, `VL300_broken_b4_1080.mp4`, `VL301_pickup_p4_1080.mp4`.
- The `30_*` files from 10-22 show the old clone. Keep them as history; do not place them.

### 9.5 Open issues after this stage

- **O-25:** the rig's late HUD return (+0.533 s). 关卡设计 / 系统设计.
- **O-26:** the EXAMINE gate still uses the raw Relay check (`RelayBlocksInspect`, Game.cs 1782).
- **O-27:** no first-key K hold in main.
- **O-28:** the Read / Examine split is a proposal.
- **O-29:** main moves. The contract is v2.6 on 2ebd70b9.
- **New, O-30:** B·4's IN→HOLD boundary is one frame late when the float32 clock rounds below 0.8 s. Seen on the phone run, invisible (0.4°). Follow-up for 游戏视觉; it is not blocking.
- **New, O-31:** §9.4 edge band and panel inside/edge fail as written, because the reference is sRGB and Unity blends UI in linear light; the linear reference leaves 1.8 % (median) of edge pixels on curved corners. The gate's definition is for 平面视觉 / 游戏视觉 to restate (a linear reference with a corner allowance), or to accept as is.
- **O-16:** P4 factor 3 is still pending D1.6's key model (#24).
- **The phone prompt glyph (交互设计):** see 40_contract.md. After HK1, `promptText` is empty for the kinds the key HUD owns, so the X2 mini glyph hides for Locked, LockedHaveKey, Broken, Read and Examine. Keep it for Open, Shut, TakeKey and Glass. The safe default is to leave it hidden for the key HUD's kinds, because the USE button carries the verb. Anchoring it to the key HUD's line 1 needs a small getter, a 游戏视觉 follow-up on request.
- **#27:** `HUD_KeyGlyph.png` is untouched.
- **#34:** the sound-cue follow-up comes right after the landing.
