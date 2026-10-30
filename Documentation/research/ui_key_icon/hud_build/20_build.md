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

> **Superseded (fix round 2, 2026-10-29).** This section is the 10-22 build stage's list and is kept as a record. The package as it stands is in §10.5 and §11.5: no font file (the tag digits are Courier Prime **Regular** atlases, #23), `HUD_KeyGlyph.png` untouched (#27), 161 new paths plus 2 replaced files (`FrontRoomsInspectPlaytest.cs`, `FrontRoomsHudStyle.cs` v3) = 163 paths. The CourierPrime-Bold row, the replaced HUD_KeyGlyph row and "136 files" below no longer hold.

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
| **§9.4 edge band ≤ 12/255** (corrected 2026-10-29, spec verify: the "at rest" set below was wrong; the restated gate is in §9.4) | sRGB reference: max 112, 48 % of edge px over 12. ~~Linear reference: max 112, share over 12 median 1.8 % / p90 4.0 % / max 8.0 % at rest; the outliers sit on curved corners.~~ **Correction:** `gates_summary2.py` counted every `hold` frame as at rest, so its "at rest" set held D·3 tug / shake, swing and recoil frames and frames under a faded HUD. **The 112 is `lit/interrupt:72`, a D·3 shake frame** (clock − tDeny = 0.18 s, the padlock 2.22 px off centre: the 1× digit atlas resampled at a sub-pixel offset). It is not a still frame. On **truly still** frames against the linear reference (`fix1029/tools/gates_still.py`), see §9.4. | sRGB definition: FAIL (a gate-definition issue, O-31). **Restated gate: PASS** (§9.4) |
| **§9.4 keyline ring mean ≤ 2/255** (calibrates `KeylineAlpha`, DECISIONS #22/#31) | Pooled over cap1022e + cap1024a, desk + phone: minimax alpha **0.950**, worst stand 2.25/255 (dark 1.62, lit 2.04, Office 2.25, plan 1.67); per-stand zero-mean alphas 0.933–0.945; this run alone 0.945 (worst 1.97). | FAIL by 0.25 at the best single value. **KeylineAlpha stays 0.95** (already in main; no hud_style change) |
| **Panel gate** (B glyph at rest vs the SVG masters; corrected 2026-10-29) | Whole pixels. Ring 1.60 / 1.51 / 1.58 (linear 1.87 / 1.84 / 1.86): PASS. Against the sRGB reference: inside 13.7 / 39.6 / 11.4 and edge 58 / 60 / 54: FAIL. **Against the linear reference:** inside max 0.45 / 0.54 / 0.47 (≤ 3) and edge max 3.9 / 7.0 / 3.3 /255 (≤ 12). These are maximum differences in /255, as `panelgate.py:133` writes `lin_edge_max`; this row first misread them as "% over 12". Outside 0. | **PASS** against the linear reference (the restated gate, §9.4) |
| **O-25** (return from a close-up) | InspectClosed rel 149, control back that frame, HUD visible again at rel 181 = **+0.533 s** (grammar 0.15 s). The eye is at rest at +0.150 s under the hidden group | FAIL, 关卡设计's rig |

### 9.4 Images, MP4s, VL rows, and the restated image gate (O-31)

**The restated gate (O-31, recorded 2026-10-29 by fix round 1, as RESUME NOTES #7 and #8 ask).**

Why the gate is restated: the §9.4 edge band and the panel gate compared Unity's crops with an **sRGB-blended** reference, but Unity blends UI in **linear light**. So every anti-aliased pixel differs by construction, whatever the HUD does. The gate is restated as follows. Inside ≤ 3/255, outside ≤ 1/255 and the keyline ring ≤ 2/255 stay as they were.

- **Reference:** the lab geometry blended in linear light (the `lin_*` fields of `gates.py` / `panelgate.py`).
- **Frames:** truly still frames only. That means:
  - crossA ≥ 0.999 (no HUD-group fade) and an opaque frame;
  - phase hold / rest / idle;
  - for the padlock: x, y, tx and rot = 0, op = 1, and no D·3 window (clock − tDeny ≥ 0.5 s);
  - for the door: th = 12°, dy = 0, and no won't-shut window;
  - for the eye: phase rest.
  `W/hudkey/fix1029/tools/gates_still.py` selects them from the hudlog.
- **Crosshair edge band:** at most **5 %** of the edge pixels may differ by more than 12/255. This allows for curved anti-aliasing: the shader's analytic SDF coverage against the reference's area coverage.
- **Panel:** the linear reference's inside ≤ 3/255 and edge ≤ 12/255, with no allowance.

**Numbers on the 10-24 captures (cap1024a).** Share of edge pixels over 12/255, against the linear reference:

| Pass | Truly still frames | Median | p90 | Max | Largest single difference |
|---|---|---|---|---|---|
| 1920×1080 | 96 | 1.44 % | 3.23 % | 3.58 % | 36.7/255 |
| 1920×1200 | 26 | 0.00 % | 0.00 % | 4.63 % | 63.4/255 (`lit/deny:30`, the 1.054 px/unit scale) |
| 2622×1206 | 10 | 2.16 % | 3.70 % | 3.73 % | 63.0/255 |
| 1920×1200 eye | 8 | 2.00 % | 2.00 % | 2.00 % | 25.9/255 |

- **Pooled desk** (1080 + 1200, 122 frames): median 1.36 %, p90 3.23 %, max 4.63 %. **No still frame is over 5 %.**
- The spec verifier's own subset (66 frames at 1080, measured independently): median 1.36 %, p90 2.5 %, max 3.6 %, largest 33/255 on the round padlock's curved rim. That is the same picture.
- Inside 0.00 and outside 0.00 on every still frame.
- **Verdict: the crosshair PASSES the restated gate.**
- **The panel PASSES:** linear inside 0.45 / 0.54 / 0.47, edge max 3.9 / 7.0 / 3.3 /255 (§9.3, corrected row).
- **Keyline ring:** 2.25/255 against the 2/255 limit at KeylineAlpha 0.95. This is recorded, not changed: DECISIONS #31's landed constant stays (RESUME NOTE #7). It is the one image gate still over its limit.
- **Not part of the gate:** the moving frames (D·3, U·3, swings, recoil). Their differences come from sub-pixel resampling of the moving atlas and are judged in the wiring gate (§9.3) against the lab's curves.

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
- **New, O-31:** §9.4 edge band and panel inside/edge fail as written, because the reference is sRGB and Unity blends UI in linear light. **Closed 2026-10-29:** the gate is restated in §9.4 (linear reference, truly still frames, ≤ 5 % of edge pixels over 12/255 on the crosshair), and both the crosshair and the panel pass. 平面视觉 may tighten the allowance; nothing in the HUD changes for it.
- **O-16:** P4 factor 3 is still pending D1.6's key model (#24).
- **The phone prompt glyph (交互设计):** see 40_contract.md. After HK1, `promptText` is empty for the kinds the key HUD owns, so the X2 mini glyph hides for Locked, LockedHaveKey, Broken, Read and Examine. Keep it for Open, Shut, TakeKey and Glass. The safe default is to leave it hidden for the key HUD's kinds, because the USE button carries the verb. Anchoring it to the key HUD's line 1 needs a small getter, a 游戏视觉 follow-up on request.
- **#27:** `HUD_KeyGlyph.png` is untouched.
- **#34:** the sound-cue follow-up comes right after the landing.

## 10. Fix round 1 (2026-10-29 10:04–11:3x, x11 run, account B, session local_a9a1dce2, unattended)

**Inputs:**
- The three verify reports: spec NOT READY, engineering NOT READY, legibility READY.
- DECISIONS.md: read at the start (1aef55a2, #1–#34) and again at 11:0x (9a4c7a3a). It had gained three entries during this stage:
  - **#35:** the P4 target is the panel slot, desktop bottom-left (90, 914), phone top-left. That is what the build already does.
  - **#36:** main is on Unity 6000.6.3f1, and the audit's fix list. Both done here.
  - **#37, DECIDED (Red 10:55):** **KP-1b in this package now.** Done here (§10.3).
- **Main** is ac63418d (Red 10-28) plus its working tree:
  - **Unity 6000.6.3f1** (it was 6000.3.10f1 for every earlier run);
  - Game.cs 00d06f84 at the start, then **a6b6b6c6** from 10:51 (关卡设计: no TIER, the prompt's keyline, O-25 in the rig), then **90154a8e** from 11:21 (the captions' keyline);
  - CameraRig **bfbdb69b**, Mobile 845e32b7, DoorShot b2014396, DoorShotTests abc598da, MapWorld a8a83b0b, InspectPlaytest 46717d34, HudStyle 5cef54df.

**Load:** 1-min load 17–190 over the stage (another session's Unity was compiling `proj_ref66`). Every timing taken over 32 is **UNDER LOAD**. All HUD captures run on the injected 1/60 s clock, so their numbers do not depend on load.

### 10.1 The verify findings and what was done

| # | Finding (verifier) | Verdict | Done |
|---|---|---|---|
| S1 | The compile on today's main can't be confirmed: no clone had a `Library/`, so `roslyn_check.py` had no `.rsp` (spec) | Valid then; now resolved | Roslyn against **main's own 6000.6.3f1 `.rsp`**, first via a symlinked reference (the engineering verifier's method), at the end with the updated shared `W/tools/roslyn_check.py` (6.6's own Roslyn, reference `W/proj_ref66`). **COMPILE OK on macOS and iOS** for main + payload, + HK1, and + HK1 + KP-1 (§10.5). The clone was also re-synced to main and imported on 6000.6.3f1 (3 min), and every Unity run of this stage is on 6.6. |
| S2 | The restated O-31 gate is not recorded; the §9.3 numbers are wrong (spec, legibility) | **Valid** (docs) | §9.3's edge-band and panel rows corrected; the restated gate and its numbers are in §9.4; VL301 (chip PASS) and VL303 corrected in `W/vl_pending/hud_key_b.md`. The 112/255 was a D·3 shake frame, and the panel's "linear %" were /255 values. |
| S3 | The contract docs are still on Game 2ebd70b9; M-1 … M-3 not clearly named (spec) | **Valid** | Contract **v2.8** on Game 90154a8e (§10.3). The new `contract/README.md` names M-1 … M-8, and every hunk carries `// HK M-n`. |
| S4 | U·3's "it worked" click plays on a cancelled unlock (spec, not blocking: a gap in the spec) | **Fixed** | §10.2 (2). |
| E1 | KP-1 breaks 关卡设计's tests: no Pocket beat, key gone at 0.75 s (engineering) | **Valid, fixed** | KP-1 v2.7 hands off only with a listener; the 3D key is hidden, not destroyed; Pocket stays at 0.85 s and the end at 0.95 s; new test M-7. `FrontRoomsDoorShotTests` **247/0**. |
| E2a | HK1 breaks the autopilot's key-panel checks (`keyPanel.activeSelf`) (engineering) | **Valid, fixed** | M-6 reads the key HUD's `PanelShown` / `PanelArrival` (new payload getters). The door scenario gives **34/34 ok**. |
| E2b | HK1 breaks `FrontRoomsInspectPlaytest` (`promptText.text == target.Prompt`) (engineering) | **Valid, fixed** | The package replaces that visual-owned file: its prompt checks read the game's `prompt` string. **132/0** with HK1 attached. |
| E3 | E events are lost around a state switch (`FrontRoomsKeyHud.cs:566-571`) (engineering) | **Valid, fixed** | §10.2 (1). |
| E–R1 | Landing does not show P4 in a build: shipped pickups are instant (engineering; 总控's audit §4 Q1) | Red decided KP-1b (#37) | §10.3, M-8 + `PickupInstant`. |
| L1 | The prompt keyline (Unity `Outline`, 4 diagonal copies) is lighter than Figma beside straight edges on very light grounds: almond leaf line 2 paper/ring 3.2–3.3 against Figma 5.0–5.6 (legibility) | Valid finding, **not changed here** | **O-32**, below. Today's backgrounds give 6.4–11:1, and the almond-leaf door is no longer in the game. `OutlineTapAlpha` is the shared, landed HudStyle constant (#31), which main's flash line, title legend and (since 10:51) prompt keyline also use. An 8-copy outline would change all of them, so it is 平面视觉's call. |
| L2 | During E·1's build the dot loses its keyline for about 80 ms (rel 0–4), faithful to the lab (legibility) | Not a build defect | **O-33** for 平面视觉: keep the dot's keyline until lidOp reaches 1, if wanted. |
| L3 | O-31 not recorded (legibility) | **Valid** | As S2. |

### 10.2 Payload changes (visual-owned; masters `W/hudkey/src`)

1. **E events around a switch (E3).** The 10-24 build applied `NoKey` / `Unlocking` / `BrokenTry` / `InspectOpened` only when their state was ALREADY the one showing. So they were lost:
   - on the first frame of an aim;
   - while the previous state was still leaving (the 0.12 s cut);
   - on another zone's door.

   An event's state now takes the dot **on that frame** and builds with the reaction on top (rule 3), because a reaction (#19 rank 2) outranks another state's leave. Also:
   - A leave in progress resumes first (`Resume`).
   - The aim used is this frame's, or the last frame's when the game already passes None (`Phase.Inspect` on the Opened frame).
   - #19 is kept: a reaction already latched on another state keeps the dot for an in-place event (tug, won't-shut). A takeover (unlock, close-up) always takes the dot, except from an unlock in progress or an eye in its close-up.
   - `InspectClosed` is unchanged.
2. **The cancelled unlock (S4).** The shot's own cancel raises KeyWithdraw + Return with no BoltRetract (`FrontRoomsDoorShot.Finish`). On that beat the panel key now eases back over 0.15 s (`GestureCancelEase`), so U·3's click never plays. Before, it eased back only at the HUD's return, 15 frames later, after the click. The padlock still comes back closed at the HUD's return (O-23, #26).
3. **Panel getters** for the map's autopilot (M-6): `PanelShown`, `PanelArrival`.
4. **KP-1b (#37):** `PickupInstant(Identity, verticalFov)`.
   - The glyph appears at the hand position at the held size (`PickupFactor` 3 × the 0.35 m scale; 1.117 on the phone).
   - The flight's lab clock starts at t = 0, so the lab's 0.5 s hold is drawn, with `HeldSwing(t)` from the glyph's arrival.
   - Then comes exactly the KP-1 flight.
   - Reduce Motion: a fade-in at the slot.
   - The "key no longer held here" guard counts from the hold's start (`tFlightStart`), as for a KP-1 flight. This came from the doors autopilot at 11:14, where the next zone's key came 0.28 s after an instant pickup.
5. **`Assets/Editor/Rendering/Inspect/FrontRoomsInspectPlaytest.cs`** (E2b, the one replaced file): `ShownPrompt` = the game's `prompt` string.

FrontRoomsKeyHud.cs is now sha1 **8f1f89f0**. Motion, Parts, VectorGraphic, TextLayout, the shader and the PNGs are unchanged. ASCII, LF. *(Corrected in round 2: the "no allocation per frame" checks of this round used `GC.GetAllocatedBytesForCurrentThread()`, which reads 0 on Unity's Mono 6.13 even for a `new byte[4096]`, so they could not fail. §11.3 replaces them with a measure that can, and it shows +0 bytes.)*

### 10.3 Contract v2.8 (关卡设计's; `contract/`, `contract/README.md`)

| Name | What changed |
|---|---|
| M-5 (KP-1) | The hand-off happens only with a `KeyHandoff` listener. The 3D key is **hidden** at `HoldUntil`. Pocket stays at 0.85 s; `Presenting`, Release and the destroy stay at 0.95 s. New `HandedOff(zone)`. The game subscribes only with the key HUD attached. The key HUD shows the key from the hand-off. |
| M-6 (HK1) | The autopilot's key-panel checks read the key HUD's panel. "Too early" = while the 3D key is still in view. On an instant contact the panel is "up" as its P4 starts. |
| M-7 (KP-1) | New map test `21 hand-off (KP-1)`. |
| **M-8 (HK1, KP-1b, #37)** | In `OnKeyTaken`, when nothing presents the key: `keyHud.PickupInstant(HudIdentity(zone), BaseFov)`. |

- **Bases:** Game **90154a8e** (main's working tree at 11:21: 10:51's a6b6b6c6 plus 关卡设计's caption keyline; the hunks are the same on both), DoorShot b2014396, DoorShotTests abc598da. MapWorld a8a83b0b is checked and not touched.
- **Hunks:** HK1 18; KP-1 on top of it 11; the combined diff 27 (Game 20, DoorShot 5, Tests 2).
- `patch -p1` is clean on copies of main's files and reproduces the generator's output byte for byte, both combined and HK1 alone.
- v2.7 and v2.6 are kept as `*_1029_v27*` / `*_1024_v26*` with their bases.

### 10.4 Verification on Unity 6000.6.3f1

The clone `W/proj_hudkey` was re-synced at 10:31 to main ac63418d + working tree (`pipeline/resync_clone4.sh`, log `logs/s11_resync.log`):
- The clone had no Library: the first import took 3 min.
- The shared W Audit tools do not compile on 6.6 (`GetInstanceID`), so they were moved aside to `W/_partial/hudkey_clone_audit_tools_1029/`. The resync script now does that itself.
- The harness's single `GetInstanceID` now uses `FrontRoomsObjectId`.
- At 11:0x the clone took main's 9 files changed since then (incl. Game a6b6b6c6 and the O-25 rig) and contract v2.8.

One Unity job at a time, never more than 2 on the line. Logs `W/hudkey/logs/f1*.log`, `f2*.log`, `f3*.log`.

| Run | What | Result |
|---|---|---|
| f1c / f2c / **f3c** | Curves + registry + shader gate (`FrontRoomsKeyHudCurves.RunBatch`) | **63/63 registry checks.** That is the 51 earlier ones plus 12 new: F1–F7 (E around a switch), the (d) cancel beat, the panel getters, KP-1b ×3. Curves are **byte-identical to c20** (25,832 samples, 0 failures against the lab). Shader 8/8 on Metal macOS, Metal iOS, D3D11, GLES3/WebGL2 and Vulkan. f2c had one FAIL from a harness bug: two Sims share the static EditorClock. It was fixed in the harness; the product was unchanged by it. |
| f1d | `FrontRoomsDoorShotTests.RunBatch` with contract v2.7 (DoorShot / Tests hunks unchanged in v2.8) | **247 passed, 0 failed**, incl. 1226 (pocket 0.850 s, gone 0.950 s), 1307 (Release, destroy 0.950 s) and the new M-7 (one KeyHandoff at 0.750 s, depth 0.350 m, the key hidden after it, HandedOff until 0.933 s) |
| f1p | Capture 1920×1080: `eye_gate`, `eye_closeup`, `unlock` (lit, dark, Office), `unlock_cancel_early`, `unlock_cancel`, `pickup` → `cap1029_fix1` | `wiring3.py` (the cancel check now keyed to the KeyWithdraw beat) **83/84**; the one failure is O-25 on 10:31's rig. **Cancel:** KeyWithdraw at rel 84, U·3 eased back by rel 93, rule max 1.000 after it (`VL299c_cancel_no_click_1080.jpg`). **P4 via KP-1 v2.7:** take-off on the KeyHandoff frame (rel 47) at 3×, Pocket at rel 53, path within 0.03 px of 10-24. |
| f1e | `FrontRoomsInspectPlaytest.RunBatch` with HK1 + KP-1 (v2.7) | **132/0** (`ok prompt at 1.5 m: "E · READ PLAN"`, `ok key: prompt "E · EXAMINE KEY"`) |
| f1a, f1b ×3 | Autopilot `-autopilotDoors` (v2.7), plus same-seed control runs | Door checks **33 ok + 1 note, 0 fail** of 34 rows each time with the contract, incl. M-6 (round 2 correction: the note is "a raw ray meets the player's own controller from 44° down (the aim skips it)"). The overall verdicts were FAIL. The FMOD BankLoadException is the known clone issue. Seed 2106545503 was also caught by the Relay after the scenario, and two **no-contract** control runs on the same seed went: one ended by time, one was caught at 28.0 s. So the Relay's chase is nondeterministic and not HUD B (the payload has no Relay reference). |
| f2p / f2q | KP-1b capture: desk 1920×1080 (`eye_closeup`, `pickup`, `pickup_instant`), phone 2622×1206 (`pickup`, `pickup_instant`) → `cap1029_kp1b_desk` / `_phone` | Wiring desk **18/18**, phone 2/2: these rows cover `pickup` and `eye_closeup` only, none is a `pickup_instant` row (round 2 correction). **KP-1b's evidence** is the hudlog of `pickup_instant` (the numbers below), `VL301c` and the 3 KP-1b registry checks. **O-25 now PASSES** on main's 10:51 rig: the HUD with the eye at rest is back **0.150 s** after E-back (rel 149 → 158; 10-24: 0.533 s). **KP-1b:** E takes the key (instant), the glyph appears next frame at the hand position (desk (1140, 396) 3×; phone 1.117×), holds 30 frames, flies, and lands in the slot (desk (90, 166) = bottom-left (90, 914); phone top-left) at +0.72 s, rule and label after. `images/VL301c_kp1b_instant_pickup.jpg` is for 平面视觉's review. |
| f3a | Autopilot doors with v2.8 + the guard fix, seed 861860538 (f2a had FAILed the presented check: the instant key's hold outlived a zone change) | **33 door checks ok + 1 note (the same self-hit note), 0 fail**, ended by time; the overall FAIL is FMOD only |

**Images** (Shot format, `hud_build/images/`):
- `VL299c_cancel_no_click_1080.jpg`: 10-24 against the fix, the cancelled unlock;
- `VL301c_kp1b_instant_pickup.jpg`: KP-1b, desk and phone.

The VL rows (VL297, 299, 301, 303, 304) were revised in `W/vl_pending/hud_key_b.md`. Only the reserved VL297–VL304 are used; they were still unused at 10:4x (next free VL442).

### 10.5 Package (`W/apply/hud_key_b.sh`, convention v2)

- It writes **161 NEW paths** (unchanged list) and **1 REPLACED** file: `FrontRoomsInspectPlaytest.cs`, base 46717d34, exact copy in `hud_key_b_base/`, its .meta kept.
- It keeps hud_style's 3 files and does not touch `HUD_KeyGlyph.png` (#27).
- It was rebuilt by `pipeline/build_package_hud_key_b.py` (now allows that one replaced path). The 10-22 package is in `W/_partial/hud_key_b_pkg_20261029_*`.
- **Dry run on main** (ac63418d + working tree, 11:2x): **DRY RUN OK** (162 bases, 81 new GUIDs unused).
- A scratch apply (`W/tools/_test/hud_key_b_1029/t2`) gives MANIFEST OK, then ALREADY APPLIED.
- **Roslyn `--all`, Unity 6000.6.3f1's own compiler, reference `W/proj_ref66`:** COMPILE OK on **macOS and iOS** for:
  - main + payload;
  - + HK1;
  - + HK1 + KP-1 (v2.8).

  Logs: `W/tools/_rebase/hud_key_b_fix1029_u66/` (Game a6b6b6c6) and `hud_key_b_fix1029_u66b/` (HK1 and HK1 + KP-1 on Game 90154a8e, 11:2x). The same configurations also pass with the 6000.3.10f1 compiler: `hud_key_b_fix1029c/`.
- Apply (游戏视觉): `bash /Users/redwang/FrontRoomsVisualWork/apply/hud_key_b.sh --apply`. On its own it is still inert; HK1 (+ KP-1) is 关卡设计's to land (`contract/`).

### 10.6 Open after this round

- **O-25:** fixed by 关卡设计 in main's **working tree** (CameraRig bfbdb69b, Game a6b6b6c6, 10:51, not committed yet). It measured 0.150 s in the clone.
- **O-26 … O-28, O-30, O-16:** unchanged (§9.5).
- **New, O-32 (平面视觉 + 游戏视觉):** the prompt keyline is Unity's 4-diagonal `Outline`. On a very light ground it reads lighter than Figma beside straight edges. Options:
  - an 8-copy outline or a shader keyline (it changes every HudStyle text: flash, title legend and main's prompt);
  - accept it.
- **New, O-33 (平面视觉):** E·1's dot keyline dips for about 80 ms in the build (lab-faithful). Hold it until lidOp = 1 if wanted.
- **New, O-34 (平面视觉, from 总控's audit of the legibility verify):** the panel's line 2 `OPENS THE STAIR DOOR` (Muted #BDBAB0) is 1.6–2.6:1 against lit L0. It is a type/colour call; the build implements the spec.
- **KP-1b's look** is for 平面视觉 to review (`VL301c`, #37). P4's start factor stays 3× / 1.117× until D1.6's model is measured (#24, O-16).
- **When D1.6's real key lands,** its presented pickup goes through KP-1 (M-5); M-8 stays for any key without a model.

## 11. Fix round 2 (2026-10-29 13:30–14:0x, account B, session local_a9a1dce2, unattended)

**Inputs:**
- The three verify reports on round 1's package: spec NOT READY, engineering NOT READY, legibility READY.
- DECISIONS.md: 9a4c7a3a (#1–#37) at the start; this round added **#38** (G6, below), 8513cc42; re-read before returning.
- **Main** (ac63418d + working tree) moved three times around this round. Game.cs was **d958edfd** (11:39, 关卡设计's s25 HUD kit: prompt keyline 2 px, zone keylines, the IT readout strings), then **83b148c2** at 13:41 (the zone meta row in paper), then **69687b6a** at 13:51 (s27: phone captions 220 pt). Mobile went 845e32b7 → **6869ef97** (13:44, 交互设计: HudKeepOut's caption rect gated on Captions). The other bases are unchanged: DoorShot b2014396, DoorShotTests abc598da, MapWorld a8a83b0b, InspectPlaytest 46717d34, HudStyle 5cef54df.
- **Load:** 1-min load 13–110 during the Unity runs; the start of r3c / r3q / r4p (44–52) is **UNDER LOAD**. Every HUD capture runs on the injected 1/60 s clock, so no number below depends on load.

### 11.1 The verify findings and what was done

| # | Finding (verifier) | Verdict | Done |
|---|---|---|---|
| S1 / E1 | Contract v2.8 fails `patch --dry-run` on today's Game.cs: M-2's UpdateHud hunk has `"RELAY  "` in its context, now `"IT  "` (spec, engineering) | **Valid** | **v2.9** on Game **69687b6a** (§11.5; first made on 83b148c2, re-generated at 14:00 when s27 landed). Every generator anchor still applied; only the diff context moved. New `W/hudkey/contract/regen_contract.sh` re-makes the diffs from main's files and runs every dry run in one go. |
| S2 / E2 | HUD B's prompt line keeps a 1 px keyline while main's `promptText` is at 2 px since G6 (spec, engineering) | **Valid on desktop; the phone half is not adopted, with evidence** | DECISIONS **#38**. `FrontRoomsHudStyle` v3 appends `PromptKeylineDistance = 2` and `PromptKeyline(handheld)`. KeyHud's line 1 (a, dot, b) reads it, line 2 stays 1. Contract **M-9** makes main's `promptText` read it too. **Desktop: 2 px, = main.** **Phone: 1 pt (#23), not 2 pt:** the verifiers asked for "2 pt on phone, as main's promptText gets". Captured at 2 pt (`cap1029_r2_phone`), that is 6 px on the 3× canvas: Unity's four diagonal Outline copies come apart from Bayon 17's stems and read as a ghost of the line, and the ring's gap doubles (5.0 → 10.6 %). G6 was measured on a desktop frame and says "2 px", and #23's 1 pt is already 3 px. Main's phone `promptText` has the same Outline, font size and canvas, so it shows the same ghost today (inferred, not captured); M-9 brings it back to 1 pt. `images/VL303b_g6_prompt_keyline.jpg`. |
| S3a | §2 still lists CourierPrime-Bold + OFL, a replaced HUD_KeyGlyph and "136 files" (spec) | **Valid** | §2 is marked superseded by §10.5 / §11.5. |
| S3b | "Wiring 18/18 desk, 2/2 phone" covers no `pickup_instant` row (spec) | **Valid** | Reworded in §10.4 and in VL301 / VL304: KP-1b's evidence is the `pickup_instant` hudlog, VL301c and the 3 KP-1b registry checks. No wiring row was added. |
| S3c | The autopilot's "34/34 ok" is 33 ok + 1 note (spec) | **Valid** (checked in `f3a_report.json`: 33 `ok`, 1 `note`) | Corrected in §10.4, `contract/README.md`, `40_contract.md` and VL304. |
| S3d | VL297's image and MP4s are from 10-24 (6000.3.10f1, the pre-fix payload, the O-25 rig) (spec) | **Valid** | Re-shot on 6000.6.3f1 with the final payload and main's rig (`cap1029_r2_eye`: all five E·1 scripts). New `VL297_e1_eye_plan_1080.jpg` and both MP4s; O-25 is 0.150 s in all three close-up scripts. The 10-24 files are in `W/_partial/hudkey_vl297_1024_images/`. |
| S4 | The package replaces `FrontRoomsInspectPlaytest.cs` (spec, truncated; needs the 游戏视觉 session's OK) | For the session | It now replaces two visual-owned files: InspectPlaytest (base 46717d34) and **FrontRoomsHudStyle.cs (base 5cef54df, hud_style v3, append-only)**. Both bases are checked by sha1 and their exact copies are in `hud_key_b_base/`. The session's OK covers both. |
| E3 | The registry's "no per-frame GC" checks cannot fail: `GC.GetAllocatedBytesForCurrentThread()` reads 0 on Unity's Mono 6.13 (engineering) | **Valid** (harness only) | The 4 checks are replaced by the verifier's method (§11.3): a control that must show, then 10 scenarios at **+0 bytes, 0 gen-0 collections** over 1200 frames each. |
| E4 | The replaced InspectPlaytest no longer checks what is drawn: `ShownPrompt` is the game's `prompt` field (engineering, truncated) | **Valid** (test coverage) | `DrawnPrompt`: the key HUD's line 1 as drawn (new editor-only `Line1Drawn`) when it owns the line, else `promptText`. Two new checks at rest (placard at 1.5 m, the key at 0.9 m). The range / hysteresis checks keep `ShownPrompt`: the key HUD fades its line out over its leave while `promptText` snaps, so "as drawn" there would test the fade, not the rule. **134/0** with HK1. |
| L1 | The prompt hops 1 px between owners: main's baseline 590, the key HUD's 591 (= Figma) (legibility) | 关卡设计's | Written in `contract/40_contract.md` (open items). |
| L2 | Locked / Unlock never show in today's game (`doorsNeedKeys: 0`; `exitLocked` false until A13) (legibility) | Recorded | In `40_contract.md`. Captured on the steel EXIT leaf after A13 (O-36). |

### 11.2 Payload changes (visual-owned; masters `W/hudkey/src`)

1. **`Assets/Scripts/Rendering/UI/FrontRoomsHudStyle.cs` → v3, sha1 2ab130b8** (REPLACED, base 5cef54df). Two members are appended, and nothing that exists changes:
   - `PromptKeylineDistance = 2` (desktop px, G6);
   - `PromptKeyline(bool handheld)`: 2 on desktop, `KeylineDistance` (1 pt) on the phone.
2. **`FrontRoomsKeyHud.cs` → 25be30ee** (1564 lines):
   - `AddOutline(Text, distance)`. Line 1's three runs take `PromptKeyline(handheld)`; line 2 takes `KeylineDistance`.
   - Editor-only getters for the playtests: `Line1Drawn`, `Line1KeylineDistance`, `Line2KeylineDistance`.
3. **`FrontRoomsInspectPlaytest.cs` → af130833** (REPLACED, base 46717d34): `DrawnPrompt`, `PromptOwner`, and the 2 checks.

Motion, Parts, VectorGraphic, TextLayout, the shader and the 74 PNGs are unchanged. All `.cs` files are ASCII and LF.

### 11.3 Harness (clone only, `W/hudkey/harness`)

- **G6:** 2 checks. Desktop: line 1's three runs at 2, line 2 at 1. Phone (a new `Sim(handheld)`): line 1 at 1, line 2 at 1. Both read `FrontRoomsHudStyle.PromptKeyline`.
- **Per-frame GC:** the 4 `GetAllocatedBytesForCurrentThread` checks are gone. The new measure reads the heap in use (`GC.GetTotalMemory(false)`, in 4 KB block steps) and the gen-0 collections over 1200 frames, after 60 warm frames and a full collect.
  - **Control:** 100 B per frame must show, and it does: **+167,936 bytes**.
  - **10 scenarios, each +0 bytes and 0 collections:** Idle; Locked hold; Locked build/leave; D·3 every 0.6 s; Broken + won't-shut; Read rest; Read build/blink/leave; panel at rest; KP-1b P4 every 2.5 s; the unlock under (b)+(d) with U·3 on the beats and F·1.
  - The shot's `Beat` delegate is fetched before the measured frames, so no reflection runs inside them.

### 11.4 Verification (Unity 6000.6.3f1, clone `W/proj_hudkey`)

The clone took main's changed files at 13:40, Game 83b148c2 at 13:48, the payload, the harness and contract v2.9 (`logs/s12_resync.log`). s27 (13:51, phone captions) and Mobile 6869ef97 came after the runs; they touch no HUD B path, and Roslyn covers them. One Unity job at a time; the line never had more than 1.

| Run | What | Result |
|---|---|---|
| r2c, **r3c** | Curves + registry + shader gate | **72/72 registry checks** (the 63 of round 1, minus the 4 old GC checks, plus 2 G6, plus 11 heap checks). Curves are **byte-identical** to round 1's (25,832 samples; 0 failures against the lab). |
| r2p | Desk 1920×1080 lite: `deny` (lit L0 door), `eye_closeup` (evac plan) → `cap1029_r2_desk` | Wiring **24/24**; layout PASS (line 1 at 591.000; the measured paper baseline is within 0.014 px). |
| r2q | Phone 2622×1206 at **2 pt** → `cap1029_r2_phone` | The ghost (§11.1 S2/E2). Kept only as the rejected evidence. |
| **r3q** | Phone at **1 pt** (final) → `cap1029_r3_phone` | Wiring **24/24**; layout PASS (717.000, measured within 0.012 px). |
| r2e | `FrontRoomsInspectPlaytest.RunBatch` with HK1 + KP-1 (v2.9) | **134 passed, 0 failed**: `ok prompt at 1.5 m as drawn: "E · READ PLAN" (drawn by the key HUD)`, `ok key: prompt as drawn "E · EXAMINE KEY" (drawn by the key HUD)`. |
| r4p | Desk full pass, all E·1 scripts (`eye_gate`, `eye_closeup`, `eye_rf`, `eye_rm`, `eye_firstkey`) → `cap1029_r2_eye` | Wiring **65/65**, incl. **O-25 at 0.150 s** in each close-up script. It gives VL297. |

r2p and r2e ran before the phone value moved to 1 pt; on desktop the payload is identical. The map suites (DoorShotTests 247/0, autopilot doors 33 ok + 1 note) are round 1's: M-5 … M-8 did not change.

**G6 on the key HUD's line 1** (`fix1029/tools/keyline_g6.py`). Luminance comes from the composite. Core = fully covered paper; ring = keyline ink at alpha ≥ 0.25. "Lightest ring" = the ring's lightest 10 %, where a thin ring lets the background through. Gap = the share of the zone within the keyline distance of the glyph that has no ink.

| Frame | Keyline | Gap | Core : ring | Core : lightest ring | Ring : background |
|---|---|---|---|---|---|
| Desk, lit L0 door, LOCKED | 1 px (before) | 8.0 % | 8.81 | 6.16 | 1.78 |
| | **2 px (final)** | 8.0 % | **10.97** | **7.13** | **2.19** |
| Desk, evac plan, E · READ PLAN | 1 px (before) | 10.6 % | 5.51 | 3.43 | 2.06 |
| | **2 px (final)** | 11.8 % | **7.63** | **4.19** | **2.79** |
| Phone, lit L0 door, LOCKED | 1 pt (before) | 5.0 % | 10.42 | 7.61 | 2.70 |
| | 2 pt (rejected) | 10.6 % | 11.28 | 8.56 | 2.91 |
| | **1 pt (final)** | 5.0 % | 10.43 | 7.63 | 2.70 |
| Phone, evac plan, READ PLAN | 1 pt (before) | 5.2 % | 10.47 | 7.31 | 2.66 |
| | 2 pt (rejected) | 10.9 % | 11.42 | 8.15 | 2.91 |
| | **1 pt (final)** | 5.2 % | 10.47 | 7.36 | 2.67 |

At 2 pt the phone's mean ratios rise a little, because there is more ink, but the ring opens up, and the picture shows why it was rejected. These numbers are not 平面视觉's G6 measure (their "core vs ring" was 2.65 → 3.25 on a lit-wallpaper "DOOR"), so they compare the before and after only. The 1 px "before" desk frames are 10-24's (6000.3.10f1); round 1's legibility verify showed that HUD layers match 6.6 to within 0–1/255.

**Images** (Shot format, `hud_build/images/`):
- `VL303b_g6_prompt_keyline.jpg`: the table above as crops (desk ×4, phone ×2);
- `VL297_e1_eye_plan_1080.jpg`, `VL297_e1_closeup_1080.mp4`, `VL297_e1_eye_gate_1080.mp4`: re-made from `cap1029_r2_eye`.

The VL rows were revised in `W/vl_pending/hud_key_b.md` (VL297, VL301, VL303, VL304). They are still VL297–VL304: re-checked at 13:5x, still reserved and unused, next free VL442.

### 11.5 Contract v2.9 and the package

**Contract v2.9** (`contract/`, generator `W/hudkey/contract/apply_contract.py`):
- **Bases:** Game **69687b6a**, DoorShot b2014396, DoorShotTests abc598da. MapWorld a8a83b0b is checked and not touched.
- **Hunks:** HK1 19 (round 1's 18 + M-9); KP-1 11 on top of HK1; the combined diff 28 (Game 21, DoorShot 5, Tests 2).
- **v2.8 → v2.9:** only `"RELAY  "` → `"IT  "` in M-2's context, plus M-9.
- **Dry runs** (`regen_contract.sh 1029g v2.9`, 14:00, `W/hudkey/logs/r2_contract_dryrun.txt`) on fresh copies of main: HK1 alone, combined, and KP-1 on HK1. All clean: no fuzz, no offset, 0 rejects. The applied files equal the generator's output byte for byte.
- **Roslyn `--all`** (6000.6.3f1's compiler, `--ref W/proj_ref66`, `W/tools/_rebase/hud_key_b_fix1029_r2/`), on main's files of 14:00 (Game 69687b6a, Mobile 6869ef97): **COMPILE OK on macOS and iOS** for main + payload, + HK1, and + HK1 + KP-1.
- v2.8 is kept as `*_1029_v28*` with `base_1029_v28/`.

**Package** (`W/apply/hud_key_b.sh`, convention v2; rebuilt by `pipeline/build_package_hud_key_b.py --force`, the previous one in `W/_partial/hud_key_b_pkg_*`):
- **161 NEW paths** (unchanged);
- **2 REPLACED:** `FrontRoomsInspectPlaytest.cs` (base 46717d34) and `FrontRoomsHudStyle.cs` (base 5cef54df);
- **2 KEPT:** `Rendering/UI.meta`, `FrontRoomsHudStyle.cs.meta`;
- `HUD_KeyGlyph.png` (d450ab42) is not touched (#27).
- **DRY RUN OK** on main: 163 bases, 81 new GUIDs unused.
- A scratch apply (`W/tools/_test/hud_key_b_1029r2`) gives **MANIFEST OK**, then **ALREADY APPLIED**.

Apply (游戏视觉): `bash /Users/redwang/FrontRoomsVisualWork/apply/hud_key_b.sh --apply`. On its own the package is inert, except that FrontRoomsHudStyle gains its two members. HK1 (+ KP-1) is 关卡设计's to land (`contract/`).

### 11.6 Open after this round

- **O-35 (平面视觉; answer in the track docs, no Red call):**
  - (a) **Line 2** (Plex Mono 13 / 11) stays at 1 px. Should it follow G6? The default is no.
  - (b) **The phone prompt** is at 1 pt (#23), not 2 pt (#38's reading of G6, with the ghost evidence in VL303b). If you want 2 pt anyway, it is one constant.
  - (c) At 2 px, **the Plex "·" run** shows its four Outline copies as a small dark cluster round a pale dot (VL303b, desk READ PLAN). Keep it, or put the dot run at 1 px. Main's Bayon "·" was accepted pale at 2 px.
- **O-36 (关卡设计):**
  - main's prompt baseline is 1 px above the key HUD's;
  - the Locked capture on the steel EXIT leaf, after A13.
  Both are in `40_contract.md`.
- **If Game.cs moves again before HK1 lands:** put the new sha1 in the generator's BASES and run `W/hudkey/contract/regen_contract.sh <tag> v2.9`. s27 (13:51) needed only that.
- O-25 (fixed in main's working-tree rig), O-26 … O-28, O-30, O-32 … O-34 and O-16 are unchanged.

## 12. Package stage (2026-10-29 14:20–14:3x, account B, session local_a9a1dce2, unattended)

- **Verify round on fix round 2's package:** spec READY, engineering READY, legibility READY. DECISIONS.md 8513cc42 (#1–#38) read at the start and again before returning: unchanged.
- **Bases re-read at 14:2x, all unchanged:** Game 69687b6a, Mobile 6869ef97, MapWorld a8a83b0b, DoorShot b2014396, DoorShotTests abc598da, CameraRig bfbdb69b, HudStyle 5cef54df, InspectPlaytest 46717d34. No Unity job ran in this stage (line count 0).
- **Package:** `W/apply/hud_key_b.sh` (unchanged since 13:49). Dry run on main 14:22: **DRY RUN OK** (163 bases, 81 new GUIDs unused; `W/hudkey/logs/pkg_final_dryrun.txt`). A fresh scratch apply (`W/tools/_test/hud_key_b_1029pkg`, APPLY_REAL + APPLY_BACKUP): DRY RUN OK, then MANIFEST OK, then ALREADY APPLIED. HUD_KeyGlyph.png and its .meta are untouched, and the two backups equal `hud_key_b_base/`.
- **Contract v2.9:** combined `patch -p1` on fresh copies of main's files at 14:2x is clean (0 rejects). The clone's Game.cs = main + the diff, except s27's phone captions. The clone's DoorShot and DoorShotTests = main + the diff, byte for byte (`W/tools/_test/hud_key_b_contract_1029pkg`).
- **Docs written:**
  - `contract_map_HK1.md`: bases, M-1 … M-9, the (d) line, KP-1 / KP-1b / the #18 first-key steps, H1 (no MapWorld hunk), and the full diff;
  - `21_contract_touch.md`: T-1 UNLOCK, T-2 EXAMINE / READ by kind, T-3 the mini USE glyph, T-4 the rest;
  - `40_for_graphic.md`: 12 lines for 平面视觉 · 信息设计.
  - VL rows: VL301 and VL303 images re-ordered largest first (`W/vl_pending/hud_key_b.md`).
- **Follow-ups from the engineering verifier's mid-switch harness** (`W/_partial/hudkey_verify3e_harness_1029/checks.txt`; not blocking, judged so by that lens):
  - **O-37:** leaving a padlock 0.2 s into its build. famOut from the partial pose, then the dot after 0.133 s. One frame's line-alpha step is 0.24 and the body-width step is 10.0 px, against the full leave's 0.087 / 9.4.
  - **O-38:** an aim dithered across a locked door's edge at 10 Hz gives about 20 dot on/off changes per second (3 Hz gives 5.75). This is input-driven, not HUD flashing; the dot is far under WCAG 2.3.1's area. Fix later with a short minimum dwell on the aim kind (游戏视觉, with 平面视觉's OK on the motion).

## 13. Live on main (2026-10-29, after HK1 + KP-1)

游戏视觉's live check, capture stage (account B, workflow stage, 14:5x–15:4x, unattended). It checks that main runs exactly the decided version in the real game: nothing new, no harness scene. Red's complaint that decided designs had not really landed is why the check exists.

**Bases.**
- Main c5e03368 plus the working tree at 14:5x:
  - Game.cs 0bd2190e: HK1 M-1…M-4, M-6, M-8, KP-1, KP-1b and O-36, landed 14:49;
  - Mobile 31a8dfdd and TouchControls 03dd76d8: T-1 / T-2;
  - DoorShot ef941ca9, DoorShotTests 8503b314;
  - FrontRoomsKeyHud 25be30ee, HudStyle 2ab130b8, InspectPlaytest af130833;
  - CameraRig bfbdb69b, MapWorld a8a83b0b.
- **Main moved during the check. These changes were not re-run:**
  - Game 3b125479 and MapWorld 527ddb39 at 15:08: A26, the floor-1 start group. The diff touches no HK line, `keyHud` call or `promptText` line.
  - FrontRoomsKeyHud 14d506ac at 15:19: #34, the sound cues. It adds the event and its `Cue` calls and changes no drawn value.
- **Clone:** `W/proj_hudlive`, a fresh mkclone on Unity 6000.6.3f1. No game code was changed. The only addition is the harness `W/hudkey/live1029/harness`, the capture harness run with **`-hkLive`**:
  - no identity override: main's own `StubIdentity`, so each zone shows its own shape and colour;
  - no mode overrides: main's shipped `UnlockStart` / `PanelMode` are logged and checked;
  - no placeholder key: shipped play's instant pickup;
  - promptText and the touch USE disc / mini USE glyph are logged on every frame;
  - a new `o36` script.
  Roslyn: COMPILE OK before the runs.
- **The real game:**
  - FrontRooms3D.unity through the game's title handoff (run seed 516574485), stands found in the generated map.
  - The aim is set through the player's yaw / pitch. E goes through the game's `UseDoor` (doors, keys) or the game's input snapshot (the plan's close-up; on the phone that is the same `UseDown` the USE disc raises).
  - **Locked doors** come from the map's own keys-on tool path (`DoorsNeedKeysForTools`). Shipped L0 has `doorsNeedKeys` 0 and A13's EXIT lock has not landed, so shipped play has no locked door yet.
  - **The broken door** comes from `map.BreakDoor`, the call the Relay makes; the Relay was held dormant for the stands. The map builds no broken door by itself.
  - The clock is the injected 1/60 s.
- **Unity jobs**, one at a time on the clone, the line never above 2 (`W/hudkey/live1029/logs/h01…h07`):
  - import 14:58–15:00;
  - desk 1920×1080 15:00–15:09, 2342 frames, 0 harness failures;
  - phone 2622×1206 (iPhone 16 Pro forced) 15:09–15:29, 2342 frames, 0 harness failures;
  - suites 15:29–15:33.
  - The 1-min load was 47–168 (**UNDER LOAD**). The captures run on the injected clock, so no number below depends on load.
- No player was built or launched.

### 13.1 Checks against the decided set

| # | Decided (source) | Desk 1920×1080 | Phone 2622×1206 | Verdict |
|---|---|---|---|---|
| L1 | The key HUD drawn by the game (M-1); shipped modes (b) crosshair / (d) panel (#13) | attached, owns the dot; `B_AfterShot` / `D_ShotBeats` | same | PASS |
| L2 | B glyph on its tag, the padlock 3113:6999; identities from the zone (#20), token 00 on floor 1 | long white / long blue / rect red (by zone), token 00; the wiring tool matches each frame's shape to the lab's curve | round blue / rect white / rect red, 00 | PASS (B1…B10+ not reachable live, §13.4) |
| L3 | E·1 on the plan (READ): opens from the dot, one slow blink, rest; the close-up and back (#17, grammar §2–§5) | build vs the lab max 2.9e-6; blink at 0.420 s; at rest by 1.05 s; react on InspectOpened; gone 0.150 s; back at rest 0.150 s, no blink; **O-25 0.150 s** | the same numbers. USE READ → BACK → READ; a tap on the plan opens the close-up in the touch suite | PASS |
| L4 | E·1 grammar: transient gate, 0.12 s cut, re-blink ≥ 1 s, photosafety (eye_gate) | 0.133 s cut; no re-blink at 0.947 s; re-blink 0.420 s; blinks [0.42, 2.92] | same | PASS |
| L5 | KP-1b (#37): the glyph at the hand (0.65, 0.30) at the held size, ~0.5 s hold, flight INTO the slot (#35) | on the E frame at 3.000×; pivot (1140, 396), glyph centroid (0.644, 0.311); hold 0.517 s (31 frames, the tag swinging); lands (90, 166) = **(90, 914)** at +0.717 s; rule and label after | 1.117×, centroid (0.652, 0.314); hold 0.517 s; lands top-left (93.3, 330 pt from the bottom), 0.85×, +0.717 s | PASS |
| L6 | KP-1b = the 10-29 verified clone run | frame by frame from E: max \|Δ\| 0.011 px | 0.0034 px | PASS |
| L7 | L3·D, the iris from the dot, on a locked door | build vs the lab 0 beyond the float32 slack, lit / dark / Office | same | PASS |
| L8 | D·3 tug then shake on NoKey | starts on the E frame (rel 42); tug / shake vs the lab 0 beyond the slack; lines stay 1 | same | PASS |
| L9 | U·3 push-fold-click in the panel with the unlock shot; C27 = (d): the panel stays, the key never enters the lock; F·1 drop and fade | (b) t_ret 1.450 s; U·3 on KeyInsert 0.533 s; panel exempt 87 frames, crosshair hidden; U·3 vs the lab max 1.3e-5; F·1 vs the lab 0 beyond the slack; lit / dark / Office | same | PASS |
| L10 | B·4: the broken door hangs off its hinge | build, react (WON'T SHUT 0.55 s, the 2nd E swallowed by the map), leave: 0 beyond the slack. **Hold: rel 48 (0.800 s) shows th 11.603°, 12° one frame later** | the same frame | **FAIL** (O-30, one frame, 0.4°) |
| L11 | O-36: one prompt baseline at 591 (1080), no hop | §13.2 | n/a (desktop decision) | PASS |
| L12 | Layout: line 1 / line 2 baselines, whole pixels at rest | 591.000 / 614.000, measured within 0.039 px, max frac 0.0001 | 717.000 / 777.000, measured within 0.099 px | PASS |
| L13 | Keyline #141414 at KeylineAlpha 0.95, 1 px; prompt line 1 2 px desk (G6, #38), 1 pt phone; NO card | measured on lit / dark / Office / plan (`keyline.py`): padlock and eye 1 px, ring α p99 0.93–0.95 (1.00 where the tag's opaque ink digits fall in the band); line 1 2 px (key HUD and main's promptText); line 2 1 px; panel 1 px, α p99 0.950; ink 21–26 sRGB; **0 UI px more than 3 px from a glyph** | 1 pt = 3 px on every part, ink 20–23; 0 card px (the phone panel crop's lower rows hold the touch stick, excluded) | PASS |
| L14 | Touch T-1: USE says UNLOCK on a locked door you hold the key for | n/a | UNLOCK on 261/261 LockedHaveKey frames (lit / dark / Office); LOCKED on 405/405 Locked frames | PASS |
| L15 | Touch T-2: EXAMINE on an object close-up, READ on a readable; BACK in the close-up | n/a | READ on 345/345 Read frames; BACK on 71/71 close-up frames; TAKE on the key; OPEN on a plain door; BROKEN | PASS for READ; **EXAMINE not shown: no shipped object target (§13.4)** |
| L16 | Touch T-3: the small USE glyph hides while the key HUD owns the line | n/a | shown on 0 of 1737 owning frames; shown for OPEN (61/70; the 9 others are the key HUD's own 0.12 s leave) and TAKE | PASS |
| L17 | Phone frames match the touch spec 2528:5403 (`2532:3826`) | n/a | HUD B's parts sit where the spec puts them. **Two differences that are not HUD B:** the master still shows TIER, and pause sits at x 846 pt (as the touch suite expects, `pause (846, 368)`) against about 781 pt in the master | FINDING (touch spec lag, not HUD B) |

### 13.2 O-36 measured (desktop 1080)

The pixel rows were measured from the UI layer over black (`cb_`). For TAKE KEY they come from the composite, because the pickup script saves no layers; the key's pale box stands behind it, so a column rule is used (`live1029/tools/o36.py`). Rows are screen rows from the top. A baseline "at 591" means the ink of the flat-bottomed capitals ends in row 590.

| Line | Owner | Frames | Last ink row (coverage) | Next row | Baseline |
|---|---|---|---|---|---|
| E · OPEN DOOR | main's promptText (y −45, its centre logged at 585.0004) | 61 × 3 grounds | 590 (0.95) | 591: 0.03 | **591.031** |
| E · TAKE KEY | main's promptText | 3 | 590 (1.00) | 591: 0 | **591.000** |
| LOCKED | key HUD line 1 (b1y logged 590.9999) | 27 × 3 + deny 73 | 590 (1.18) | 591: 0.04 | **591.036** |
| E · UNLOCK | key HUD line 1 | 46 × 3 + 25 | 590 (1.07) | 591: 0.04 | **591.039** |
| E · READ PLAN | key HUD line 1 | 129 + 139 | 590 (1.08) | 591: 0.01 | **591.012** |
| BROKEN | key HUD line 1 | 84 | 590 | 591: 0.02 | **591.020** |

- **No hop.** Every frame of both owners ends its capitals in row 590. The largest spread between owners is 0.039 px, the anti-aliasing of different letters.
- **Owner changes on one aim** (`o36`, lit / dark / Office): OPEN DOOR → LOCKED → UNLOCK → OPEN DOOR.
  - No frame draws both lines.
  - Going to the key HUD, the line is blank for 1 frame, then builds in place.
  - Going back to promptText, it is blank for 4 frames (rel 125–128) while the padlock finishes its 0.12 s leave. That is the grammar's wait for the leave, not a hop.
- Image: `W/hudkey/live1029/images/HBL06_o36_baselines_1080.jpg`.

### 13.3 The wiring gate and the suites

**Wiring:** the hudlog of the live run against `W/hudkey/capture/curves_lab_f60.tsv` at the same clock. The tool is `fix1029/tools/wiring3.py`; its live copy `live1029/tools/wiring_live.py` only reads the padlock shape from the frames, because there is no rect-red override.

| Run | Checks | Failed |
|---|---|---|
| Desk | 95 | 1 |
| Phone | 95 | 1 |
| **Total** | **190** | **2, both O-30** |

- **The two failures** are `lit/broken`, "B.4 hold from 0.8 s": th 11.6030 at rel 48. The lab's IN→HOLD boundary falls one frame late when the float32 clock rounds below 0.8 s (O-30; seen on the phone on 10-24; now on desk too).
- **Separately** (`live1029/tools/live_checks.py`): KP-1b desk 5/5; phone KP-1b 5/5 + touch 32/32.

**Suites,** read-only, run in the clone with main's current touch files (T-1 / T-2):

| Suite | Owner | Result |
|---|---|---|
| `FrontRoomsAdaptiveLayoutTests.RunBatch` (layout) | 交互设计 | **896 passed, 0 failed** |
| `FrontRoomsTouchPlaytest -touchProfile iphone -touchFocus examine` (touch EXAMINE) | 交互设计 | **51 entries: 45 PASS, 0 FAIL, 6 NOTE**. This is the "51/0" of 14:38. Target Kit_EvacPlacard#1 (Flat → READ): a tap opens the close-up, USE BACK at (768, 65), zoom, TEXT panel |
| `FrontRoomsInspectPlaytest.RunBatch` | 游戏视觉 | **134 passed, 0 failed**, incl. `prompt at 1.5 m as drawn: "E · READ PLAN" (drawn by the key HUD)` and `key: prompt as drawn "E · EXAMINE KEY" (drawn by the key HUD)` (its test-bed key) |
| `FrontRoomsDoorShotTests.RunBatch` | 关卡设计 | **247 passed, 0 failed** |

### 13.4 Not covered, and why

- **EXAMINE on an object, phone or desk, in shipped play:** the map registers one EXAMINE target, `Kit_EvacPlacard#1` (Flat → READ). `Kit_KeyTag_*` and `Kit_DeskPhone` are `ship: false`. On desk the key's EXAMINE prompt drawn by the key HUD is covered only by InspectPlaytest's spawned test bed. The phone USE "EXAMINE" verb was not seen in any run.
- **Floor tokens B1…B9 / B10+ at 0.8×:** the live run stays on floor 1 (token 00). Deeper floors were not driven.
- **A broken door the map makes by itself:** there is none. B·4 ran on the map's `BreakDoor`.
- **A locked door in shipped play:** there is none until A13. L3·D, D·3 and U·3 ran under the map's keys-on path.
- **Main after 15:08:** A26 and #34 were not re-run (§13 Bases).
- **#39 (平面视觉, 15:0x):** the meta line in paper and the ≥ 200 ms dwell. It is a follow-up package after #34, so it is not in main: the meta is still Muted #BDBAB0. This is recorded, not failed against the landed package.

### 13.5 Images (Shot format) and VL rows

- `W/hudkey/live1029/images/`:
  - `HBL01_e1_desk_plan.jpg` — PASS
  - `HBL02_e1_phone_closeup.jpg` — PASS
  - `HBL03_kp1b_p4_live.jpg` — PASS
  - `HBL04_locked_unlock_live.jpg` — PASS
  - `HBL05_broken_b4_live.jpg` — FAIL, O-30
  - `HBL06_o36_baselines_1080.jpg` — PASS
  - `HBL07_keyline_no_card_live.jpg` — PASS
  - `HBL08_touch_use_phone.jpg` — PASS
  - `HBL09_phone_vs_figma_spec.jpg` — FINDING
- Rows: `W/vl_pending/hud_b_live.md`. No VL number was taken. The phone frames were compared with Figma `2532:3826` first (HBL09).
- Results: `W/hudkey/live1029/results/`:
  - `wiring_desk|phone.json/.txt`;
  - `live_desk|phone.json`;
  - `o36_desk.json`;
  - `layout_desk|phone.json`;
  - `keyline_desk.jsonl`, `keyline_phone2.jsonl`.
- Frames: `W/proj_hudlive/Verification/hudkey/live_desk|live_phone`.

### 13.6 Findings for owners

- **O-30 (游戏视觉):** B·4's IN→HOLD frame at 0.800 s, now seen on desk as well. One frame, 0.4°.
- **Spec lag (交互设计 / 平面视觉):** Figma `2532:3826` still shows TIER, and pause at about 781 pt. Live and the touch suite have pause at 846 pt. Update the master, or confirm 846.

### 13.7 Independent review (2026-10-29 16:0x, wf_1a7b25d9-a17 review stage): DEFECTS, corrections to 13.1

The reviewer reopened all 9 images, the hudlogs, the wiring, live and keyline results, and logs h02–h07. **The decided set is live as decided**, with one build defect and one decided package still open:
- **B·4 (O-30), cosmetic:**
  - On the 0.800 s frame the leaf shows 11.603° instead of the 12° hold, on desk and phone. The float32 clock reads 0.79999995 s there, so the HUD still evaluates the IN spring; the next frame is 12°.
  - Fix: an epsilon on the IN→HOLD boundary in a small follow-up (hud-b-followup-2), after #39 lands. Both touch FrontRoomsKeyHud.cs.
- **#39 is not in main yet.** It is running as wf_80a2e11f-8aa (the meta ink goes to paper; the ≥ 200 ms dot dwell). L4's "photosafety" row tests only the blink spacing, not this dwell.

Corrections to the rows above (the check claimed more than its data shows):
- **L2:** identity is proven for the **shape only**. The desk Office padlock and panel are rect **blue**, not rect red. The wiring tool never compared a zone's colour with StubIdentity's hash. The colour check moves to hud-b-followup-2. StubIdentity stays a stand-in until D1.6.
- **HBL04, the "F·1: THE PADLOCK DROPS, FADES" panel:**
  - It is rel 137, F·1's first frame, so it shows neither a drop nor a fade. Rel 150 shows both (y 17.5 px, rot 5.0, op 0.714).
  - F·1 itself passes numerically: 240 values, 0 beyond the slack. The image is re-cut in hud-b-followup-2.
- **L13p:** "0 card px" is wrong. Up to 3 px at alpha 0.045 lie more than 3 px from a glyph: the anti-alias tail of the 1 pt (3 px) Outline. There is still no card.
- **L17 / HBL09:** the phone spec 2528:5403 has **no** key panel, padlock, eye, UNLOCK or READ in any TS master. The comparison proves only the shared layer: the label, and the prompt under the dot. HUD B's phone placement must be compared with the HUD key B Figma frames instead (an open item for hud-b-followup-2).

Still not covered live:
- an EXAMINE object in shipped play (the map ships only the plan);
- tokens B1…B9 / B10+ (floor 1 only);
- the #18 first-key EXAMINE K hold (needs D1.6's presented key);
- Reduce Motion and Reduce Flashing live;
- an unlock cancelled before BoltRetract;
- locked and broken doors made by shipped play (none exist until M3 / A13);
- current main (Game 3b125479, KeyHud 14d506ac), which was diffed (cue calls and A26 lines only) but not re-run.

## Follow-up #39 (2026-10-29)

游戏视觉's HUD B follow-up build stage (account B, workflow stage, 15:2x–16:3x, unattended). It builds exactly DECISIONS #39 (平面视觉's review of 40_for_graphic): (1) the key panel's line 2 in paper on the 1 px keyline, and (2) a minimum dwell of ≥ 200 ms on the crosshair dot (photosafety: nothing on the HUD flashes faster than 3 Hz). DECISIONS.md e8fc5f95 (#1–#40) was read at the start and again before returning: unchanged.

**Bases** (main refs/heads/main c5e03368 + working tree, re-read 16:31): FrontRoomsHudStyle **2ab130b8**, FrontRoomsKeyHudMotion **2bf60d6a**, FrontRoomsKeyHud **14d506ac** (HUD B + #34 as landed 15:19), Game 3b125479, SoundCue tests bd0b9386.

**Package:** `W/apply/hud_b_followup.sh` (convention v2, in the hud_key_b.sh / hud_soundcue.sh format; masters `W/hudkey/followup39/src`, builder `W/hudkey/followup39/build_package.sh`).
- **3 REPLACED**, exact base copies in `hud_b_followup_base/`, `.meta` files kept (GUIDs 0d07a9d6, 19241531, b5f3b284):
  - `FrontRoomsHudStyle.cs` → f1f7e64e: ONE appended member, `TextPaper` = #F4F1E8. Nothing existing changes (the script checks append-only).
  - `FrontRoomsKeyHudMotion.cs` → 7555d735: ONE appended constant, `MinDwellSeconds = .2f`, its comment citing #39 and the 3 Hz rule.
  - `FrontRoomsKeyHud.cs` → cf2cb7fc: the meta Text in `FrontRoomsHudStyle.TextPaper` + `AddOutline(meta, FrontRoomsHudStyle.KeylineDistance)`; the unused `Muted` colour removed; the dwell (below). The #34 enum and the field-like `public static event System.Action<FrontRoomsHudSoundCue> SoundCue;` are unchanged (#40; the script greps them).
- **DRY RUN OK on main** (16:31, `W/hudkey/followup39/logs/pkg_dryrun_final.txt`). Scratch apply on main's sources (`W/tools/_test/hud_b_followup_1029/`, APPLY_REAL + APPLY_BACKUP): DRY RUN OK, MANIFEST OK, then ALREADY APPLIED; the backups equal the base copies. **Not applied** (the session lands it). ASCII, LF.

### F39.1 What the payload does

**(1) Line 2 of the key panel** (OPENS THE STAIR DOOR, Plex Mono 13 desktop / 11 phone): ink `TextPaper` #F4F1E8 (the panel label's paper) instead of the grey meta #BDBAB0, on the shared keyline (`KeylineDistance` 1 unit, `KeylineColor` at `OutlineTapAlpha` per tap = `Keyline`), exactly as the crosshair's line 2. Phone and desktop. It stays quieter only by its face and size (T5).

**(2) The dwell.** The HUD tracks one bit, the dot *open* (a state building, built or reacting, or a leave running back on a re-aim) or *closed* (a leave running forward, or idle), and when it last changed (`tDot`).
- Inside `MinDwellSeconds` after an open, an aim that leaves is treated as still aimed: the state keeps building / stays built, then leaves with the decided curve (cut to 0.12 s when something waits).
- Inside `MinDwellSeconds` after a close, an aim back on the target is treated as gone: the leave carries on (nothing waits for it), and the dot reopens only if the aim is still there when the dwell ends. A new build (any state) waits the same way, and never happens if the aim has gone.
- An aim that lands when the dot has rested ≥ 0.2 s builds on that frame; the 1e-4 s slack keeps an exact 0.2 s on its frame under a float32 clock.
- Player events are never held: NoKey, Unlocking (U·3 / the shot), BrokenTry, InspectOpened / InspectClosed still go through `Take()` on their frame; the dwell then counts from that open (`ApplyEvents` marks it).
- The #34 cues follow the motion: `BrokenBuild` is raised in `Enter`, so it fires when the build really starts; a build the dwell drops raises nothing. Reduce Motion (the cross-fades) and Reduce Flashing (no blink) run under the same dwell.
- Editor-only: `DebugLine` gains `open`, `tDot`, `dwellHold`.

### F39.2 Clone and runs

`W/proj_hudkey`, re-synced 15:42 to main's current files (`W/hudkey/followup39/resync.sh`: main + local patches + tools + the follow-up payload + **its own harness copy** `W/hudkey/followup39/harness`; HK1 / KP-1 / O-36 are in main, so no contract). The 24 clone paths main no longer has (old tools, the old harness) were moved to `W/_trash_20261029/proj_hudkey_followup39_1029_154205/`, not deleted. Unity 6000.6.3f1, one job at a time on this clone, the line never above 2 (`W/tools/unity_jobs.sh` before each launch). Logs `W/hudkey/followup39/logs/f39*`, outputs `W/proj_hudkey/Verification/hudkey/f39/`.

| Run | What | When / load | Result |
|---|---|---|---|
| f39t | `FrontRoomsKeyHudSoundCueTests.RunBatch` (#34, EditMode) | 15:44 | **16 / 0** |
| f39c | `FrontRoomsKeyHudCurves.RunBatch`: curves, registry, shader gate | 15:44 | **89 / 89**; curves byte-identical |
| f39d | desk 1920×1080 full + 1920×1200 lite, + `jitter_eye`, `jitter_lock`, `meta` (4 stands) | 15:44–16:13, 1-min load 36–197 (**UNDER LOAD** 16:05) | §F39.5 |
| f39e | 1920×1200, `eye_gate` + `eye_closeup` alone (as p25) | 16:13–16:15 | 31 / 31 |
| f39p | phone 2622×1206, `-hkMetaAll` (meta at all four stands) | 16:15–16:27 | §F39.5 |
| f39g / f39h | desk 1080 + 1200 lite / phone: `broken` + `jitter_lock` only, after two harness set-up fixes | 16:27–16:29 | §F39.5 |

Every capture runs on the injected 1/60 s clock, so no number depends on load.

### F39.3 Proof (a): line 2's contrast, before / after

Measure (`W/hudkey/followup39/tools/meta39.py`): the harness's new `meta` script holds the key panel at rest at each stand with the camera on the stand's door or plan and captures the panel crop four times on consecutive frames: after (as built), after with only line 2 drawn, **before** (the live Text set to main's #BDBAB0 with its keyline off, i.e. main as landed), before with only line 2. Composited in linear light over the scene; WCAG Y.
- **core** = line 2's fully covered pixels (a ≥ 0.98, UI luminance ≥ 0.3);
- **local background** = the zone within the keyline distance (1 px desktop, 3 px phone) outside the glyph. That is 20_build's legibility measure (core : ring, §11.4), with the ring taken by geometry so it exists without a keyline too: the keyline ink after, the bare wall and the text's own anti-aliasing before;
- also the lightest 10 % of that zone, and core against the scene alone (no UI) under the glyph grown by 4 units (mean / p95).

Target: core : local ≥ 4.3.

| Stand | Device | Core : local, before → after | Lightest 10 % | Core : wall (mean) | Core : wall (p95) | Verdict |
|---|---|---|---|---|---|---|
| Lit Level 0 | desktop | 2.80 → **6.66** | 1.76 → 3.77 | 3.84 → 6.36 | 3.16 → 5.31 | PASS |
| Dark cell | desktop | 3.00 → **6.94** | 1.81 → 3.72 | 4.26 → 7.08 | 3.26 → 5.36 | PASS |
| Office | desktop | 3.26 → **7.19** | 1.87 → 3.85 | 4.94 → 8.15 | 4.03 → 6.64 | PASS |
| Evacuation plan | desktop | 2.85 → **6.72** | 1.79 → 3.74 | 3.94 → 6.54 | 3.68 → 6.12 | PASS |
| Lit Level 0 | phone | 4.18 → **9.54** | 2.46 → 5.35 | 5.26 → 8.99 | 3.27 → 5.66 | PASS |
| Dark cell | phone | 4.94 → **10.18** | 2.63 → 5.59 | 6.65 → 11.14 | 5.25 → 8.70 | PASS |
| Office | phone | 3.63 → **8.82** | 2.28 → 5.09 | 4.40 → 7.29 | 3.22 → 5.27 | PASS |
| Evacuation plan | phone | 4.74 → **10.02** | 2.61 → 5.49 | 6.26 → 10.53 | 5.59 → 9.37 | PASS |
| *G6's lit L0 frame under the panel* | *desktop* | *2.77 → 6.59* | *1.77 → 3.69* | *3.70 → 6.16* | *2.48 → 4.14* | *PASS* |
| *G6's brightest 178×25 window (y 160, x 548, Y 0.58; a ceiling-lamp area, not a stand)* | *desktop* | *1.15 → 3.04* | *1.19 → 2.06* | *1.17 → 1.43* | *1.20 → 1.39* | *below 4.3 (O-40, information)* |

- Core luminance: before 0.488 (#BDBAB0), after 0.847 desktop / 0.859 phone.
- The italic rows composite the same captured layers over G6's lit frame (`prompt_open_door_lit_scene.png`): under the panel's own rect, and over that frame's brightest window, the worst case. The 1.6–2.6:1 of O-34 sits between these two "before" rows.
- **Line 2 meets 4.3:1 at every stand on desktop and phone.** The label STAIR KEY has no keyline (unchanged, O-39).

### F39.4 Proof (b) and (d): the dwell in the real game path

**Registry (Sim, a real FrontRoomsKeyHud on the harness clock), 17 new checks, all PASS** (`registry_checks_f39c.txt`):
- a 10 Hz jitter (3 frames on / 3 off, 3 s) on Locked, Broken and Read, the same under Reduce Motion, Read under Reduce Flashing, Locked on the phone: 60 aim changes, 12 dot changes, smallest gap 0.250 s, 2.0 cycles a second; Broken raises one BrokenBuild per real build (1; 6 under Reduce Motion);
- after the jitter and 0.6 s at rest, a normal aim builds on its landing frame with the lab's curve (u 0.5 at 0.25 s, 1 at 0.5 s; B·4 and E·1 equal to `HM.DoorIn` / `HM.EyeIn` at the same times; the blink at 0.42 s), and leaves on the frame the aim leaves (0.333 / 0.300 / 0.300 s back to the dot);
- a 0.05 s aim keeps the padlock building until 0.2 s, then famOut from u0 0.3667;
- a padlock asked for 0.15 s after a close builds at 0.2 s; a broken door aimed 0.13–0.18 s after a close never builds and raises no cue; BrokenBuild at 1.2 s, not at the 1.0 s request;
- E with no key and E on a broken door 0.05 s after a close: D·3 / LockTug and the swing / BrokenKnock on the press frame.

**Captures** (`tools/dwell39.py`): the harness's new `jitter_lock` / `jitter_eye` scripts aim through the game itself (yaw / pitch → UpdateAim → SetAim), on and off the target 3 frames each for 3 s (timeScale 0), then 0.6 s off, a normal aim (1.2 s door, 1.5 s plan), and the leave. PASS = every interval between two dot changes ≥ 0.200 s (one frame of tolerance), ≤ 2.5 cycles a second, and the normal aim's build and leave equal to the lab (curves_lab_f60.tsv, the wiring tool's slack).

| Run | Aim changes in the jitter | Dot changes (clock s, rel) | Smallest gap | Cycles / s | Normal aim | Verdict |
|---|---|---|---|---|---|---|
| desk, locked door (f39g) | 59 | open 11.6833 r1, close 11.9167 r15, open 12.1667 r30, close 12.4167 r45, open 12.6667 r60, close 12.9167 r75, open 13.1667 r90, close 13.4167 r105, open 13.6667 r120, close 13.9167 r135, open 14.1667 r150, close 14.4167 r165; open 15.2667 r216, close 16.4667 r288 | 0.233 s | 2.01 | builds r216 = landing frame; L3·D vs lab 310 values, 0; famOut vs lab 0 | **PASS** |
| desk, evacuation plan (f39d) | 59 | open 36.0833 r1, close 36.3167 r15, … every 0.250 s … close 38.8167 r165; open 39.6667 r216, close 41.1667 r306 | 0.233 s | 2.01 | builds r216; E·1 (with its blink) vs lab 576 values, 3.9e-8; leave vs lab 0 | **PASS** |
| phone, locked door (f39h) | 59 | open 11.6500 r1, close 11.8833 r15, … close 14.3833 r165; open 15.2333 r216, close 16.4333 r288 | 0.233 s | 2.01 | builds r216; vs lab 0 / 0 | **PASS** |
| phone, evacuation plan (f39p) | 59 | open 18.3000 r1, close 18.5333 r15, … close 21.0333 r165; open 21.8833 r216, close 23.3833 r306 | 0.233 s | 2.01 | builds r216; vs lab 1.6e-6 / 0 | **PASS** |

Without the dwell the same input changes the dot about 20 times a second (O-38). Full change lists: `results/dwell_*.json`.

**(d) a normal aim after rest builds on its landing frame** (`results/landing_lines.txt`, hudlog excerpts):
```
cap_desk_r2 1920x1080/jitter_lock rel 215 clock 15.25    aim=None   cs=Idle state=Idle phase=- u=0 dot=1 line=0 open=False tDot=14.41667
cap_desk_r2 1920x1080/jitter_lock rel 216 clock 15.26667 aim=Locked cs=Lock state=Locked phase=in u=0 dot=0 line=0 open=True tDot=15.26667
cap_desk_r2 1920x1080/jitter_lock rel 217 clock 15.28333 aim=Locked cs=Lock state=Locked phase=in u=0.03333 dot=0 line=0.05555 open=True
cap_desk    1920x1080/jitter_eye  rel 215 clock 39.65    aim=None   cs=Idle state=Idle phase=- dot=1 open=False tDot=38.81667
cap_desk    1920x1080/jitter_eye  rel 216 clock 39.66667 aim=Read   cs=Read state=Read phase=in dot=0 eyeW=8 eyeLid=0 open=True tDot=39.66667
cap_phone_r2 2622x1206/jitter_lock rel 216 clock 15.23333 aim=Locked cs=Lock state=Locked phase=in u=0 dot=0 open=True tDot=15.23333
cap_phone    2622x1206/jitter_eye  rel 216 clock 21.88334 aim=Read   cs=Read state=Read phase=in dot=0 eyeW=8 eyeLid=0 open=True tDot=21.88334
```
The dot had rested 0.85 s; the state builds on the frame the game first reports the aim.

### F39.5 Proof (c): the wiring gate, the registry, the cues

**Registry** `FrontRoomsKeyHudCurves`: **89 / 89** = the 72 landed checks (incl. the shader gate, 8/8 on each target) + the 17 dwell checks. **Three of the 72 are restated for #39** (their old form tests a build or a resume less than 0.2 s after a close, which #39 now holds):
- "switch Read → Locked": the eye's leave is still cut to ≤ 0.12 s (0.133 s), and the padlock now builds **0.2 s** after the switch (was ≤ 0.12 s);
- "re-aim mid-leave" 0.1 s after the close: the leave carries on to 1.7 s, then resumes from where it is (no rebuild, no re-blink), at rest at **1.9 s** (was ≤ 1.72 s);
- "Locked → Broken": famOut is still cut (0.133 s), and B·4 builds **0.2 s** after the switch (was ≤ 0.12 s).

The other 69 are unchanged and pass. **Curves byte-identical** to every landed run (`curves_unity.tsv`, 25,832 rows, sha1 4597ed9c = fix1029r2 / r3, soundcue_sc1 / scv), including the real-KeyHud E·1 theatre timelines. **SoundCue tests 16 / 16** (C2c, C2d and the story's BrokenBuild at 8.000 s unchanged).

**Wiring** (`fix1029/tools/wiring3.py` vs `W/hudkey/capture/curves_lab_f60.tsv`, compared check by check with the landed run's JSONs, `tools/wcompare39.py`):

| Run | Landed (cap1024a) | Follow-up | Failures now |
|---|---|---|---|
| Desk (1080 full + 1200 lite) | 207 / 212 | **208 / 212** | `lit/interrupt` re-aim mid-fold (#39, below); `1920x1200/lit/broken` B·4 hold (O-30); 2 × the 1200 plan stand streamed out (known, p22 harness order) |
| 1200 eye scripts | 30 / 31 | **31 / 31** | none |
| Phone | 57 / 59 | **59 / 59** | none |
| **Total** | **294 / 302** | **298 / 302** | |

- **Passing now that failed then:** the 5 O-25 rows (main's rig fix, not this package).
- **Same known failures:** the two 1200 plan rows. O-30 (B·4's IN→HOLD one frame late on the float32 clock; th 11.603 at rel 48, 12° one frame later) moved from the phone to the 1200 desk run with the run's clock. §13.3 sees it on the live desk run too. The dwell does not touch B·4's build timing.
- **One check fails by design: `1920x1080/lit/interrupt`, "leave mid-build then re-aim mid-fold … no rebuild".** The script leaves at rel 15 (0.25 s after the build) and re-aims at rel 21, **0.1 s after that close**. #39 forbids reopening before 0.2 s, so the fold now runs on to the dot and the padlock rebuilds at rel 27. The **restated check** (`tools/interrupt39.py`) PASSES: famOut carries on at the lab's rate (8 steps of −1/19.2), the dot is back at rel 24, it reopens at rel 27 (0.200 s after the close) from u 0, and the rebuild vs the lab's L3·D is 310 values, max 0.
- **Harness set-up fixes** (clone only, `followup39/harness`). The first full runs showed 4 failures on `1920x1200/lit/broken` and 4 on `2622x1206/lit/broken`. They came from the set-up, not the motion:
  - The broken door's stand search ends on a 1-frame look at the leaf. The dwell now holds that B·4 for 0.2 s, and its 0.3 s leave then ended one frame after the script's 30-frame rest, so the scripted aim resumed a leave instead of building fresh.
  - The set-up now rests 42 frames. `jitter_lock` also re-places the player at its stand (the pickup scripts had walked away, so its first run saw no locked door).
  - The targeted re-runs give: `broken` 1080 7/7, 1200 6/7 (O-30 only), phone 7/7; `jitter_lock` desk and phone PASS. These rows replace the set-up-broken rows in the table above (`results/wiring_*_r2.*`).
- **Every scripted aim ≥ 200 ms plays the lab unchanged:** all deny / unlock / cancel / matrix / broken / eye / pickup rows pass. Of the 86 curve rows compared with the landed run, 82 are within the gate's 2e-4 beyond the float32 slack and no worse than landed. The other 4 are the set-up-broken B·4 rows, and their re-runs come to ≤ 3.4e-6.

### F39.6 Proof (e): compile

`python3 W/tools/roslyn_check.py --tree W/tools/_test/hud_b_followup_1029/tree --out … --all` on a scratch tree = main's sources (every .cs / .asmdef / .asmref, ProjectVersion, the three kept .metas) with the package applied by its own script:
- **COMPILE OK** on macOS and **COMPILE OK (iOS defines)** with `--ios`: Assembly-CSharp 124 files, Assembly-CSharp-Editor 69, FMOD 5 assemblies, 0 errors;
- no new warning in the HUD files;
- logs `W/tools/_test/hud_b_followup_1029/roslyn_{mac,ios}.log`.

The clone tree with the harness also compiles (Roslyn and Unity, 0 `error CS`).

### F39.7 Images (Shot format, `W/hudkey/followup39/images/`; VL rows in `W/vl_pending/hud_b_followup.md`, numbers at placement)

- `hudb39_meta_lit_before_after.jpg`: the panel on lit Level 0, desktop ×3 and phone, before / after (PASS);
- `hudb39_meta_stands.jpg`: the four stands × desktop / phone, before / after, with each core : local ratio (PASS);
- `hudb39_dwell_jitter.jpg`: aim on / dot open per frame through the 10 Hz jitter and the normal aim, desk / phone, door / plan (PASS).

### F39.8 Open after this stage

- **New, O-39 (平面视觉):** the panel label STAIR KEY (Bayon 20 / 17, paper) has no keyline. Line 2 now has one, so the label is the only HUD text without it. Not changed here, as #39 asks.
- **New, O-40 (平面视觉, information):**
  - At 1 px, line 2's lightest 10 % of the keyline zone is 3.7–3.9:1 on desktop (5.1–5.6:1 on the phone's 3 px).
  - Over G6's brightest lit window (a lamp area, Y 0.58) line 2 is 3.04:1 core : local. Every real stand is ≥ 6.66:1.
  - A 2 px line-2 keyline was O-35 (a)'s alternative, declined in #39.
- **New, O-41 (verify lens):** 3 registry checks and 1 wiring check are restated for #39, as listed in §F39.5, and the broken / jitter_lock set-up rests are longer. The old checks encode the lab grammar's re-aim within 0.1–0.12 s, which #39 overrides for gaps under 0.2 s. The harness copy is `W/hudkey/followup39/harness`; the shared `W/hudkey/harness` is unchanged, because the live run used it.
- **Not run:** main's EXAMINE playtest (`FrontRoomsInspectPlaytest`, 134 checks) and the map suites with the dwell. Its "prompt as drawn" checks read 23 frames after the aim, so a dwell wait (≤ 12 frames, only right after a close) leaves the eye's line partly faded at worst. Run it at landing.
- O-30 (B·4 one frame, float32) is unchanged. It is not this package's.

## 14. Follow-up 2 (2026-10-29)

游戏视觉's hud-b-followup-2 build + capture stage (account B, workflow stage, 17:0x–18:2x, unattended). It closes what §13.7 left (O-30, the L2 colour check, the HBL04 panel, L17, Reduce Motion / Reduce Flashing / a cancelled unlock / the jitter live, and the §13 re-run on current main), builds O-39 = DECISIONS #41, and places the VL rows. DECISIONS.md 4eb2dce3 (#1–#41) was read at the start and again before returning: unchanged.

**Bases** (main refs/heads/main c5e03368 + working tree, read 17:07, re-read 18:21): FrontRoomsKeyHud **cf2cb7fc** (#39 as landed 16:5x), Motion 7555d735, HudStyle f1f7e64e, Game 2e93c426, Mobile 31a8dfdd, MapWorld 6bbc3341, DoorShot ef941ca9, CameraRig bfbdb69b, SoundCue tests bd0b9386. No dataless file in the five synced folders.

**Package:** `W/apply/hud_b_followup2.sh` (convention v2, the hud_b_followup.sh format; master `W/hudkey/followup2/src`, builder `W/hudkey/followup2/build_package.sh`). **Not applied.**
- **1 REPLACED:** `FrontRoomsKeyHud.cs` cf2cb7fc → **029c2d80**, exact base copy in `hud_b_followup2_base/`, `.meta` kept (GUID b5f3b284). Exactly two changes, 6 diff lines; the script checks that count, the #34 enum, the field-like `SoundCue` event (#40), #39's lines, and that main holds #39, `KeylineDistance` 1 and `DoorInSeconds` 0.8:
  - after the label's position: `AddOutline(labelKey, FrontRoomsHudStyle.KeylineDistance);` (O-39, #41);
  - in `BrokenState.Built`: `if (x >= 1f)` → `if (now - tB + 1e-3f >= HM.DoorInSeconds)` (O-30).
- **DRY RUN OK on main** (17:1x and 18:21, `W/hudkey/followup2/logs/pkg_dryrun_final.txt`). Scratch apply on main's sources (`W/tools/_test/hud_b_followup2_1029/`, APPLY_REAL + APPLY_BACKUP): DRY RUN OK, MANIFEST OK, then ALREADY APPLIED; the backup equals the base copy. ASCII, LF.
- **Roslyn** on that scratch tree (main + package), `--all` and `--all --ios`: **COMPILE OK** and **COMPILE OK (iOS defines)**: Assembly-CSharp 125 files, Assembly-CSharp-Editor 69, 5 FMOD assemblies, 0 errors, no warning in FrontRoomsKeyHud.cs (`roslyn_{mac,ios}.log`). Both clones (+ the harness) also compile in Roslyn and in Unity, 0 `error CS`.

**Clones and runs.** Unity 6000.6.3f1; every capture on the injected 1/60 s clock, so no number depends on load; `W/tools/unity_jobs.sh` before each launch, the line never above 2; logs `W/hudkey/followup2/logs/`.
- `W/proj_hudkey` and `W/proj_hudlive` were both re-synced to main's current files (`W/hudkey/followup2/resync.sh`: main + local patches + tools + the payload + the harness copy). Clone paths main no longer has were moved to `W/_trash_20261029/<clone>_followup2_*`, not deleted. The two clones hold identical game and editor code (checked file by file); only the harness is not main's.
- **Harness** `W/hudkey/followup2/harness` = the shared harness (#39's restated checks, `jitter_*`, `meta`) + §13's live mode (`-hkLive`, `o36`, the touch / promptText columns) + new scripts:
  - `label`: O-39 after / before;
  - `deny_rm`, `broken_rm`, `pickup_rm`;
  - under `-hkLive`, `eye_rf` / `eye_rm` through the **shipped settings**: FrontRoomsSettings.ReduceFlashing, and Reduce Motion = Camera motion 0 % on desktop or the OS switch on phone. These are set without PlayerPrefs, and KeyHud's Editor overrides stay at −1;
  - per-frame identity columns (`akz`, `lockId`, `panelTex`, `panelId`, `kz`) and the panel's layers through `pickup_instant`.

  The shared `W/hudkey/harness` is unchanged.

| Run | Clone | What | When | Result |
|---|---|---|---|---|
| k1 | hudkey | `FrontRoomsKeyHudSoundCueTests.RunBatch` | 17:08 | **16 / 0** |
| k2 | hudkey | `FrontRoomsKeyHudCurves.RunBatch` (curves, registry, shader gate) | 17:08 | **89 / 89**; curves byte-identical |
| k3 / k4 | hudkey | `broken`: desk 1080 full + 1200 lite, phone | 17:09–17:10 | the 0.800 s frame HOLD on all three |
| v02 | hudlive | **live desk** 1920×1080 full, 19 scripts, `-hkLive` | 17:20–17:33 | 21 harness PASS, 0 FAIL |
| k9 | hudkey | **live phone** 2622×1206 full (iPhone 16 Pro forced), the same 19 scripts | 17:39–18:14 | 21 PASS, 0 FAIL |
| k5–k8 | hudkey | the four suites of §13.3 | 17:28–17:31 | §14.5 |
| k3n / k4n, k3o / k4o | hudkey | O-30 same-clock control: `broken` with the payload, then with main's KeyHud, then the payload back (029c2d80, checked) | 18:17–18:21 | §14.1 |

- The 19 scripts: `eye_gate`, `eye_closeup`, `eye_rf`, `eye_rm`, `deny`, `deny_rm`, `o36`, `unlock`, `unlock_cancel_early`, `unlock_cancel`, `broken`, `broken_rm`, `pickup_instant`, `pickup_rm`, `idle`, `jitter_lock`, `jitter_eye`, `meta`, `label`. They ran on the real map (seed 516574485) through the title handoff, at lit L0, the dark cell, the Office and the evacuation plan.
- **One interrupted run:**
  - The first live desk attempt (17:12) was killed (exit 137, SIGKILL) at 17:16:45 from outside this stage, inside its `unlock` capture; its outputs are in `W/_partial/f2_live_desk_killed_1716`.
  - The re-run passed the same point and finished.
  - The live phone ran in `W/proj_hudkey` (same code, same harness) to use a free slot while the desk ran.
- No player was built or launched.

### 14.1 O-30: B·4's IN→HOLD boundary

- **Cause:** `Built` switched to the hold on `x = (now − tB) / 0.8 ≥ 1`. Under a float32 clock, the frame 48 frames (0.800 s) after the build can read `now − tB` = 0.79999924 s. Then x < 1, and that frame drew the IN spring's last value, 11.603°, one frame before the 12° hold (§13.3).
- **Fix:** `now − tB + 1e-3f >= HM.DoorInSeconds`. The slack is 1 ms, not the dwell's 1e-4 s; `W/hudkey/followup2/tools/o30_sweep.py` checks it in exact float32 (`results/o30_sweep.txt`):

| Clock | Frame | Old rule HOLD | New rule HOLD |
|---|---|---|---|
| harness clock (frames × 1/60 in float32), every start frame in 0–3 h | +48 (0.800 s) | 44.3 % of starts | **100 %** |
| same | +47 (0.783 s) | 0 % | 0 % |
| real-game float32 `Time.unscaledTime`, 2 M random starts in 0–8192 s | the 0.800 s frame | 52.0 % | **100 %** |
| same | the frame before at 120 Hz / 240 Hz | 0 % | 0 % |

  Why 1 ms:
  - A 1e-4 s slack fails once one float32 step exceeds it, at clocks past about 17 min.
  - 1 ms holds to 8192 s (2.3 h).
  - It stays 7 ms clear of the previous frame at 120 Hz, and 3 ms at 240 Hz.

- **In the runs** (`tools/live2_checks.py`; the exact float32 clock is rebuilt from the frame count; `results/live2_checks.*`):

| Run | Build clock | float32 elapsed at +48 | Old rule would show | +47 | +48 | +49 |
|---|---|---|---|---|---|---|
| clone desk 1080 (k3) | 8.53333 | 0.80000019 | HOLD | IN 12.208 | **HOLD 12** | HOLD 12 |
| clone desk 1200 (k3) | 19.63334 | **0.79999924** | **IN (11.603)** | IN 12.208 | **HOLD 12** | HOLD 12 |
| clone phone (k4) | 8.50000 | 0.80000019 | HOLD | IN 12.208 | **HOLD 12** | HOLD 12 |
| live desk (v02) | 72.33334 | 0.80000305 | HOLD | IN 12.208 | **HOLD 12** | HOLD 12 |
| live phone (k9) | 72.30000 | 0.80000305 | HOLD | IN 12.208 | **HOLD 12** | HOLD 12 |

- **Everything else unchanged:**
  - **Same-clock control** (`tools/b4compare.py`, `results/o30_pair_desk.txt`): the same harness and the same runs, with the payload and then with main's KeyHud.
    - Of the 284 desk `broken` rows (1080 + 1200), every column except the frame number is identical, except **one row**: 1200 +48, `th` 11.60305 (IN) → 12 (HOLD).
    - All 142 phone rows are identical (its clock holds under both rules).
  - Registry 89 / 89; SoundCue 16 / 16 (C2c, C2d and BrokenBuild at 8.000 s unchanged).
  - **Curves byte-identical:** `curves_unity.tsv` sha1 4597ed9c, 25,832 rows, **no row changed**. The B·4 rows sample `HM.DoorIn` directly, and the file holds no real-KeyHud B·4 timeline.
  - Wiring gate: **no O-30 failure** ("B.4 hold from 0.8 s": th 12.0000, dy 1.5000 on desk and phone).
  - Across runs at different absolute clocks (live 72 s vs §13's 18 s), B·4 values differ by ≤ 4.5e-4°. That is float32 clock jitter inside the gate's ulp slack, not a change.

### 14.2 L2: zone colour identity (shape AND colour)

`tools/l2_colour.py`, `results/l2_colour.*`. For each live stand it compares four things:
- the zone the game passed: `game.aimKeyZone` for the padlock, the held key's zone for the panel;
- StubIdentity's hash, recomputed here independently (`hudlog.stub`: C# int wrap, xor, × 2654435761u; Shape = h / 100 % 3, Tint = h / 300 % 3);
- the identity the HUD drew: `lockId`, `panelId`, and the ring + tag texture;
- the zone colour counted in the drawn pixels: opaque UI pixels within 6/255 of a master colour. On dark / Office the padlock is read from the composite inside its box, where opaque UI keeps its exact colour; the panel is read from the `label` script's UI layers.

| Device | Stand | Zone | StubIdentity | Padlock drawn | Panel drawn | Verdict |
|---|---|---|---|---|---|---|
| desk | lit L0 | (34, 0) | Long White | Long, white (240 px) | KeyB_long_white, white (301 px) | MATCH |
| desk | dark cell | (34, 1) | Long Blue | Long, blue (236 px) | KeyB_long_blue, blue (219 px) | MATCH |
| desk | Office | (32, −1) | Rect Blue | Rect, blue (248 px) | KeyB_rect_blue, blue (226 px) | MATCH |
| phone | lit L0 | (10, −2) | Round Blue | Round, blue (904 px) | KeyB_round_blue, blue (1974 px) | MATCH |
| phone | dark cell | (10, −2) | Round Blue | Round, blue (904 px) | KeyB_round_blue, blue (1974 px) | MATCH |
| phone | Office | (12, −1) | Rect Red | Rect, red (1352 px) | KeyB_rect_red, red (3050 px) | MATCH |

**L2 is closed: every live padlock and panel wears its zone's StubIdentity, shape and colour.** §13.1's "rect red" for the desk Office was a wrong label in the check, not in the build: the Office zone (32, −1) hashes to Rect Blue, and that is what the build draws (§13.7). On the phone the dark-cell stand fell in the lit stand's zone. StubIdentity stays a stand-in until D1.6.

### 14.3 HBL04 re-cut

`HBL04b_locked_unlock_live.jpg` comes from the new live run (`tools/figs_live2.py`).
- Its F·1 tile is now 18 frames into the fall, where the drop and the fade both read. That is rel 154 on desk and phone: y 28.08, rot 8.03°, op 0.524, the dot back at 0.267.
- §13.7's tile was rel 137, F·1's first frame.
- F·1 itself passes the wiring gate (0 beyond the slack).
- The rest of the sheet keeps HBL04's layout, taken from the re-run. `HBL05b_broken_b4_live.jpg` is the B·4 sheet re-shot with O-30 fixed.

### 14.4 L17: phone placement against the HUD key B phone mockup

**The decided placement** is lock_motion/README.md §2c together with the motion lab's phone frame (`W/keyicon_motion/lab_template_v2.html` DEV.phone), the in-context mockup Red reviewed on 10-14. In Figma, KV11 3113:7000 and KV12 3113:7147 (section 3113:6872) are desktop boards at 3× / 2× with no phone frame. They fix the padlock geometry and the two prompt baselines relative to the dot, and the lab's phone frame scales those by 0.75.

**Live** (`tools/l17.py`, the live phone run, 874 × 402 pt at 3 px / pt, `results/l17.*`):

| Part | Live (pt) | Decided (pt) | Offset (pt) |
|---|---|---|---|
| crosshair point = padlock insert pivot | (437.0, 201.0) | (437, 201) | 0.0, 0.0 |
| padlock zone-fill centre | (437.0, 201.0) | (437, 201) | 0.0, 0.0 |
| eye centre at rest | (437.0, 200.67) | (437, 201) | 0.0, −0.33 |
| prompt baselines, line 1 / line 2 | 239.0 / 259.0 | 239 / 259 | 0.0 / 0.0 |
| prompt line 1 ink centre x | 436.83 | 437 | −0.17 |
| key panel rule, top-left | (78.0, 72.0) | (78, 72) | 0.0, 0.0 |
| key panel rule, size | 3.33 × 40.67 | 3.4 × 40.8 | −0.07, −0.13 |
| key glyph slot (logged) / scale | (93.33, 72.0) / 0.85 | (93.3, 72) / 0.85 | +0.03, 0.0 / 0 |
| key glyph ink top (drawn) | 72.0 | 72 | 0.0 |
| label STAIR KEY left x, baseline | (166.67, 91.0) | (166.4, 90.7) | +0.27, +0.30 |
| meta line left x, baseline | (166.67, 107.33) | (166.4, 106.85) | +0.27, +0.48 |
| USE disc centre (the touch suite's layout) | (740, 262) | (740, 262) | 0.0, 0.0 |

- Every part is within 0.5 pt, the whole-pixel grain at 3 px / pt. **L17 passes: no placement defect.**
- The USE verbs sit on that disc. Live they read LOCKED / UNLOCK / READ / BACK / OPEN / TAKE / BROKEN, and T-1 / T-2 pass (`results/live_phone.*`).
- §13's spec lag (TIER, pause at 846 pt vs about 781 pt in 2532:3826) belongs to the touch spec, not to HUD B, and is unchanged.

### 14.5 Live on current main (+ this package): the §13 re-run and what §13.7 left

**Wiring gate** (`live1029/tools/wiring_live.py` vs `curves_lab_f60.tsv`):

| Run | Checks | Failed |
|---|---|---|
| Desk | 150 | 0 |
| Phone | 150 | 0 |
| **Total** | **300** | **0** (§13: 190 / 2, both O-30) |

- The gate now includes `eye_rf` / `eye_rm` through the shipped settings and both cancels.
- **Separately:**
  - KP-1b desk 4 / 4 and phone 4 / 4: held 0.517 s at the hand, lands (90, 166) / (93.3, 330) at +0.717 s;
  - phone touch T-1 / T-2 / T-3 76 / 77: the one row is a finding below, not HUD B;
  - O-36 (`o36.py`): every key-HUD line and every promptText line still ends its capitals in row 590, with baselines 591.000–591.039 (no hop).

**Reduce Motion / Reduce Flashing, live through the shipped settings** (`tools/live2_checks.py` + the wiring rows):

| Check | Desk | Phone |
|---|---|---|
| padlock (`deny_rm`): cross-fade with the dot in 9 frames = 0.150 s at the rest pose (no iris); E moves nothing (no tug, no shake); leave 0.150 s | PASS | PASS |
| B·4 (`broken_rm`): cross-fade 0.150 s at the hold (th 12); E swings nothing; leave 0.150 s | PASS | PASS |
| P4 (`pickup_rm`): no flight; the panel fades in at its slot in 0.150 s, glyph fixed at the slot ((90, 166) / (93.3, 330)), no swing | PASS | PASS |
| E·1 Reduce Motion (`eye_rm`): cross-fade 0.15 s, at rest, no blink | PASS | PASS |
| E·1 Reduce Flashing (`eye_rf`): no blink, at rest after 0.30 s | PASS | PASS |

- **Finding (not HUD B; 交互设计 / EXAMINE's).**
  - Under Reduce Motion, the close-up shows a hint in main's promptText: `PINCH · CLOSER` on the phone, `SCROLL · CLOSER` on desk. The normal close-up shows none.
  - On the phone, the mini USE glyph then stands in front of that hint while USE says BACK, for 70 frames (`eye_rm` rel 79–149).
  - The key HUD draws nothing then (`gone`) but still reports `OwnsPrompt`, so the T-3 row ("glyph hidden while the key HUD owns the line") flags it.
  - The decision belongs to the hint's owner: whether the glyph belongs there, and whether that hint should show only under Reduce Motion.

**An unlock cancelled before BoltRetract (#26 / O-23), live:**
- The wiring gate's cancel rows pass, **desk 20 / 20 and phone 20 / 20**:
  - the padlock comes back CLOSED (shK 1, shY 0, no swing, no fall);
  - the early cancel has no KeyInsert, so no U·3;
  - the U·3 cancel eases back in 0.15 s from the cancel's own KeyWithdraw + Return beat, with no click.
- Desk U·3 cancel at rel 84: (dx, keySy) = (6.0, 0.83) → rel 87 (5.1, 0.85) → rel 90 (0.9, 0.97) → at rest at rel 93.

**The 10 Hz jitter (#39 dwell), live** (`tools/dwell_live.py` = dwell39.py with the padlock's lab curve taken from the shape the frames show, as wiring_live does):

| Run | Aim changes | Dot changes | Smallest gap | Cycles / s | Normal aim | Verdict |
|---|---|---|---|---|---|---|
| desk, locked door | 59 | 14 | 0.233 s | 2.01 | builds on its landing frame (rel 216); L3·D 310 values vs the lab 0, famOut 0 | PASS |
| desk, evacuation plan | 59 | 14 | 0.233 s | 2.01 | rel 216; E·1 576 values 7.3e-8, leave 0 | PASS |
| phone, locked door | 59 | 14 | 0.233 s | 2.01 | rel 216; 0 / 0 | PASS |
| phone, evacuation plan | 59 | 14 | 0.233 s | 2.01 | rel 216; 7.3e-8 / 0 | PASS |

**Suites** (run in `W/proj_hudkey`, which holds the same game code as the live clone: main's current files + this package):

| Suite | Owner | Result |
|---|---|---|
| `FrontRoomsInspectPlaytest.RunBatch` | 游戏视觉 | **134 passed, 0 failed**, incl. `prompt at 1.5 m as drawn: "E · READ PLAN" (drawn by the key HUD)` and the key's `"E · EXAMINE KEY"` |
| `FrontRoomsDoorShotTests.RunBatch` | 关卡设计 | **252 passed, 0 failed** (§13 had 247; main added tests since) |
| `FrontRoomsAdaptiveLayoutTests.RunBatch` (layout) | 交互设计 | **896 passed, 0 failed** |
| `FrontRoomsTouchPlaytest -touchProfile iphone -touchFocus examine` (touch EXAMINE) | 交互设计 | **51 entries: 45 PASS, 0 FAIL, 6 NOTE**; target Kit_EvacPlacard#1 (Flat → READ); layout use (740, 140), pause (846, 368) |

**§13.7's "current main not re-run" is closed.** This run is main as of 17:07 (Game 2e93c426, KeyHud cf2cb7fc with #39, MapWorld 6bbc3341) plus this package's two lines. The two lines are proven not to change anything else: §14.1's same-clock control, and §14.6's before layers, which are main's label.

### 14.6 O-39 = DECISIONS #41: STAIR KEY on the shared keyline

**Method:** measured like #39's meta39 table (`tools/label41.py`, `results/label41.*`).
- The harness's `label` script holds the panel at rest at each stand and captures four layers: after (as built), after label-only, **before** (the label's Outline disabled = main after #39), and before label-only.
- Each is composited in linear light; WCAG Y.
- core = the label's fully covered pixels; local = the 1-unit zone outside the glyph (1 px desktop, 3 px phone).

**Bar 3 : 1** (Bayon 20 / 17 is display size; #41). The line 2 rows are F39.3's (bar 4.3); the label rows are new.

| Text | Stand | Device | Core : local, before → after | Lightest 10 % | Core : wall (mean) | Core : wall (p95) | Verdict |
|---|---|---|---|---|---|---|---|
| line 2 (#39) | Lit Level 0 | desktop | 2.80 → 6.66 | 1.76 → 3.77 | 3.84 → 6.36 | 3.16 → 5.31 | PASS |
| line 2 (#39) | Dark cell | desktop | 3.00 → 6.94 | 1.81 → 3.72 | 4.26 → 7.08 | 3.26 → 5.36 | PASS |
| line 2 (#39) | Office | desktop | 3.26 → 7.19 | 1.87 → 3.85 | 4.94 → 8.15 | 4.03 → 6.64 | PASS |
| line 2 (#39) | Evacuation plan | desktop | 2.85 → 6.72 | 1.79 → 3.74 | 3.94 → 6.54 | 3.68 → 6.12 | PASS |
| line 2 (#39) | Lit Level 0 | phone | 4.18 → 9.54 | 2.46 → 5.35 | 5.26 → 8.99 | 3.27 → 5.66 | PASS |
| line 2 (#39) | Dark cell | phone | 4.94 → 10.18 | 2.63 → 5.59 | 6.65 → 11.14 | 5.25 → 8.70 | PASS |
| line 2 (#39) | Office | phone | 3.63 → 8.82 | 2.28 → 5.09 | 4.40 → 7.29 | 3.22 → 5.27 | PASS |
| line 2 (#39) | Evacuation plan | phone | 4.74 → 10.02 | 2.61 → 5.49 | 6.26 → 10.53 | 5.59 → 9.37 | PASS |
| line 2 (#39) | *G6's lit L0 frame under the panel* | *desktop* | *2.77 → 6.59* | *1.77 → 3.69* | *3.70 → 6.16* | *2.48 → 4.14* | *PASS* |
| line 2 (#39) | *G6's brightest window (Y 0.58)* | *desktop* | *1.15 → 3.04* | *1.19 → 2.06* | *1.17 → 1.43* | *1.20 → 1.39* | *O-40, accepted (#41)* |
| **label STAIR KEY** | Lit Level 0 | desktop | 3.97 → **5.15** | 2.23 → 2.49 | 6.45 → 6.11 | 5.40 → 5.11 | PASS |
| **label STAIR KEY** | Dark cell | desktop | 3.98 → **5.16** | 2.25 → 2.48 | 6.48 → 6.21 | 5.43 → 5.17 | PASS |
| **label STAIR KEY** | Office | desktop | 4.77 → **5.38** | 2.43 → 2.50 | 9.16 → 8.75 | 8.03 → 7.68 | PASS |
| **label STAIR KEY** | Evacuation plan | desktop | 4.16 → **5.21** | 2.29 → 2.50 | 6.96 → 6.57 | 6.60 → 6.22 | PASS |
| **label STAIR KEY** | Lit Level 0 | phone | 7.41 → **7.82** | 2.92 → 2.90 | 13.00 → 12.58 | 10.92 → 10.64 | PASS |
| **label STAIR KEY** | Dark cell | phone | 7.66 → **7.86** | 2.95 → 2.90 | 13.87 → 13.39 | 12.21 → 11.88 | PASS |
| **label STAIR KEY** | Office | phone | 6.67 → **7.66** | 2.79 → 2.89 | 10.75 → 10.27 | 8.93 → 8.49 | PASS |
| **label STAIR KEY** | Evacuation plan | phone | 6.91 → **7.72** | 2.86 → 2.89 | 11.47 → 11.06 | 10.86 → 10.48 | PASS |
| **label STAIR KEY** | *G6's lit L0 frame under the panel* | *desktop* | *3.39 → 4.94* | *2.15 → 2.47* | *4.91 → 4.65* | *4.00 → 3.78* | *PASS* |
| **label STAIR KEY** | *G6's brightest window (Y 0.58)* | *desktop* | *1.38 → **3.52*** | *1.26 → 2.22* | *1.46 → 1.38* | *1.41 → 1.34* | *PASS (bar 3)* |

- The label's core luminance is 0.83 desktop / 0.85 phone, before and after.
- "Before" over G6's window is 1.38 : 1, #41's "about 1.4 : 1". After, it is 3.52 : 1.
- **The label reaches 3 : 1 everywhere, including the worst window.**
- Core : wall changes by up to 5 % between before and after. That is the lit scene moving between the consecutive frames the four layers come from, not the label.

**The P4 slide-in** (`tools/slide41.py`, `results/slide41.*`): every frame of the instant pickup's label slide, read on the panel's UI layers.
- **The keyline fades with the word, desk and phone.**
  - The outline copies are the label's own Text under `labelGroup`, so the keyline moves with the word.
  - Its coverage stays a share of the word's: 0.51 → 0.84 desktop and 0.54 → 0.86 phone as labelA goes 0.13 → 1.
  - It never leads the word: no keyline before the word, no ring left behind.
- **No dark ghost on the live scenes:** every frame's word core is lighter than the wall it sits on.
- **Over a bright background there is a brief dark ghost:**
  - Over a flat Y 0.58 (G6's brightest window), the first 3–4 frames, at labelA 0.13–0.62, the word core reads darker than the background: core / bg 0.86 / 0.79 / 0.87 / 0.97 desktop, 0.88 / 0.84 / 0.91 phone.
  - Over Y 0.45 it is 2 frames (0.91–0.98); at Y ≤ 0.30, none.
  - Cause: Unity's CanvasGroup fades each vertex. The four outline copies under the translucent word darken its core, and Bayon's thick stems lie over all four copies.
  - #39's line 2 (thin Plex Mono strokes, landed) shows no ghost at any background in the same frames. The image `hudb2_o39_label_slide.jpg` shows both rows.
  - **This is built exactly as decided** (`AddOutline(labelKey, KeylineDistance)`, nothing else changed) and reported as **O-42 for 平面视觉**. #41's "land it when green" makes the call theirs: accept the 2–4 frames over lamp-bright walls, or decide a different fade (for example the keyline's alpha following labelA², or group opacity). This stage invents no motion.

### 14.7 Images (Shot format, `W/hudkey/followup2/images/`) and VL rows

The slides were placed in Figma (section 2595:6093, page 2099:76) under the next free numbers. Before placing, this stage re-read `VERIFICATION_LOG.md` (next free VL442; VL297–VL304 stay reserved for hud_key_b) and the section's own cells (highest placed VL441).
- The section grew 137,720 → **140,200** (113 rows: VL000–VL451). The bounds below were checked first: nothing within 400 px.
- Every slot holds one IMAGE fill (FIT) at its image's aspect ratio, and each slide got one screenshot.
- The cover header was re-counted on the canvas: `Oct 2–29, 2026 · 42 tasks · 360 checks · 926 images · index`.

| VL | Node | Images | Verdict |
|---|---|---|---|
| VL442 | 3734:8457 | `W/hudkey/followup39/images/hudb39_meta_stands.jpg`, `hudb39_meta_lit_before_after.jpg` (#39 line 2) | PASS |
| VL443 | 3734:8467 | `W/hudkey/followup39/images/hudb39_dwell_jitter.jpg` (#39 dwell) | PASS |
| VL444 | 3737:8457 | `hudb2_o39_label_stands.jpg`, `hudb2_o39_label_slide.jpg` (O-39) | FLAG (O-42) |
| VL445 | 3737:8467 | `hudb2_o30_b4_hold.jpg`, `HBL05b_broken_b4_live.jpg` (O-30) | PASS |
| VL446 | 3737:8477 | `hudb2_l2_zone_identity.jpg` (L2) | PASS |
| VL447 | 3737:8486 | `HBL04b_locked_unlock_live.jpg` (HBL04 re-cut) | PASS |
| VL448 | 3737:8495 | `hudb2_l17_phone_placement.jpg` (L17) | PASS |
| VL449 | 3737:8504 | `hudb2_comfort_live.jpg` (Reduce Motion / Flashing) | PASS |
| VL450 | 3737:8513 | `hudb2_cancel_live.jpg` (#26 / O-23) | PASS |
| VL451 | 3737:8522 | `hudb2_dwell_live.jpg` (#39 dwell, live) | PASS |

- **Rows:** `W/vl_pending/hud_b_followup.md` (VL442–443) and `W/vl_pending/hud_b_followup2.md` (VL444–451), in the §3 format with their node ids.
- **Not done by this stage, which may not write main beyond this section:**
  - append the ten rows to `VERIFICATION_LOG.md` §3, bump "Next free" to **VL452**, and record the section height;
  - copy the 13 images to `research/ui_key_icon/hud_build/images/`;
  - re-lay the cover legend: it has no room for a new HUD entry; the header was updated.
- HBL04 / HBL05 in `hud_b_live.md` are superseded by HBL04b (VL447) / HBL05b (VL445). HBL01–03 and 06–09 are still unplaced (not in this package's list).

### 14.8 Open after this stage

- **O-42 (平面视觉): the label's keyline in the P4 slide-in** (§14.6).
  - Over backgrounds of Y ≥ 0.45, the first 2–4 frames (≤ 67 ms) read as a faint dark word.
  - There is none on the live scenes, and none on #39's line 2.
  - #41 says land when green, so the package waits on this call. O-30 is independent of it: the same script carries both lines, and they are separable on request.
- **Finding for 交互设计 / EXAMINE:** under Reduce Motion the close-up shows `PINCH · CLOSER` / `SCROLL · CLOSER`. On the phone the mini USE glyph stands in front of it while USE says BACK and the key HUD still reports `OwnsPrompt` (§14.5).
- **VERIFICATION_LOG.md** rows, the next-free bump (VL452), the image copies into `hud_build/images/`, and the cover legend re-layout (§14.7): the session's.
- **Not covered live, unchanged from §13.7:**
  - an EXAMINE object in shipped play;
  - tokens B1…B9 / B10+;
  - the #18 first-key EXAMINE K hold (D1.6);
  - locked and broken doors that shipped play makes by itself (M3 / A13).
- D1.6 will re-base on FrontRoomsKeyHud.cs. This package's two lines sit in `Build` (after the label's position) and in `BrokenState.Built`.
