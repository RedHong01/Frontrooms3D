# 20 — EXAMINE prototype (close-up inspect, world paused)

Status: **FIX STAGE DONE, 2026-10-14 17:1x** (re-run after the 10-08 20:00 cut-off). Visual chat (游戏视觉). Design: `10_design.md` v1.1.
Built and re-tested in `W/proj_inspect`, re-synced from main **0a9ee7f + working tree** (10-14 16:2x, after the interactables-kit, COV-4 and GD3 merges).
Red's project was not opened in Unity and not written, except this folder: this file, `contracts/*`, `media_candidates.md` and `images/f_01…f_04`.

v1 (2026-10-08 01:50) numbers are kept in git history; this file states the current build. Where a v1 number changed, the table says so.

## 0. Summary

| Check | Result |
|---|---|
| Machine (macOS 27.0.1) | Unity 6000.3.10f1 batch compile of a clone: 0 errors, 16 s. Blender 4.3.0 headless: OK |
| EditMode (`FrontRoomsInspectTests`) | **PASS 105 / 105** (v1 93; 12 new checks for the fixes) |
| PlayMode (`FrontRoomsInspectPlaytest`, the real game, seed 516574485, **start door opened like a player**) | **PASS 116 / 116** |
| Pause neutrality (AT-01g, 3 replays) | **PASS**: 10 s and 0.25 s close-ups give the same state at 40 s (player (257.590, 25.900), Relay (275.633, 67.500) Wander 6.100, 1,557 lamps, 115 doors, elapsed 40.016) |
| Sound contract, in game with main's FMOD banks (verifier's harness) | **PASS 17 / 17**. During a stage-2 Relay warning close-up: Foley, Mechanism, Music, Subjective buses paused; Relay bus 0.501 (−6 dB); AMB 0.316 (−10 dB); Tension 0.2354 unchanged over 5 s (v1: 0.2354 → 0.2396); 0 new sound requests |
| Map suites vs a clean clone of the same main (`W/proj_inspect_base`) | **byte-identical**: camera rig 33, glass shot 20, map interaction 152, fixture tick 28, captions 9, **door shot 191**, **level designer**, 20-seed map signatures. Relay nav and map verification identical except timing fields |
| Compile (Roslyn, outside Unity) | main + step 1 alone: 0 errors. main + step 1 + `inspect` payload: 0 (OSX, iOS). main + the whole stack (map contract, payloads, sound contract): 0 (OSX, iOS). WebGL player defines: Assembly-CSharp 0 errors. Unity compile of the clone: 0 |
| Map contract | `contracts/map_inspect.diff` (37 hunks) and its two steps **dry-run OK on main 0a9ee7f**; base sha1s in each header |
| Apply packages (v2) | **`inspect_data` READY** (dry run OK on main; `rebase_apply.sh --no-install` PASS). **`inspect` WAITING** for the map contract's step 1 (its dry run says so and writes nothing); tested on a scratch copy of main + step 1: DRY RUN OK, `--apply` MANIFEST OK, then ALREADY APPLIED |

## 1. Verifier issues and what was done

| # | Issue (2026-10-08) | Result | Evidence |
|---|---|---|---|
| V1-1 | Audio did not pause | **FIXED by contract** to the sound chat: `contracts/sound_inspect.diff` (S1, S2, S4, S5; code only, no FMOD project change). Tested in the clone with main's banks | §0; `W/inspect_work/fix/runs/v1_verify_sound/deep_R_entry.tsv` |
| V1-2 | The game asked players to scroll, but scroll did nothing | **FIXED** (visual): `FrontRoomsInspect.ZoomInputWired` (false in v1). Not wired: no zoom step and no CLOSER hint. Measured: 5 s close-up, hints seen `E · BACK` only, step offered False. The map chat's C7 note and touch T3 set it later | play log "zoom not wired" |
| V1-3 / V2 blocker 2 | Dark mount P2 got no adapt lift | **FIXED** (visual): the light is read in the cell the face looks into (0.3 m in front: equals the placard's glow cell at P2), own lamp dominant (W = L + 0.02 ΣL_n), lift = log2(0.45 / W) EV up to **2.25 EV**. P2 (own lamp Dead, 3 lit neighbours): W 0.059 → **+2.24 EV, paper sRGB 124** (v1 ~69). Lit: 0 EV | §3.1; `images/f_01` |
| V2 blocker 1 | The only close-up was the dark state (start door shut) | **FIXED** (harness): the run opens the start door from inside the stream rooms and waits for `StartLampsNorth` 1.000 (0.05 s). Then three lamp states are captured at the real mount: lit, own lamp off, dead area (AT-01e / AT-01j). **Note:** at this seed P2's own lamp is **Dead** by the map's roll (as built: own 0.00, neighbours 0.97 / 0.97 / 0.97 / none), so both verifiers were right: the shut door darkened every mount, and P2 is dark even with the door open | §3.1 |
| V1-4 / V2 merge | The contract no longer applied | **FIXED**: the verifier's three-way rebase was re-checked on main 0a9ee7f (the four bases had not moved since 10-08 19:28), then this stage's edits added. Split into step 1 (rig + timings) and step 3 (game) | `contracts/` headers |
| V1-5 | The visual patch did not anchor; the placard sidecar had no block | **FIXED**: `AttachInspect` sits after the exit-sign block; all 11 sidecars get their block from a fresh Blender build of main's modules (every other key byte-equal to main) | `W/inspect_work/fix/fix_apply.py` |
| V1-6 | No apply package | **DONE as far as possible**: `inspect_data` (17 paths) is READY now; `inspect` (23 paths) waits for step 1 | §2 |
| V1-7 / V2 HIGH | Measured on a superseded placard | **FIXED**: main's merged Q16 (v3 sheet md5 7d589d81, 4096 × 2048, centre 1.56 m, Prop_AluminiumAnodised). texelsPerMetre **7,330** (2048 px / 279 mm; 9,481 across) | sidecar test |
| V1-8 | `E · READ` draws over the placard bottom at 1.5 m | **HANDED to 平面视觉** (G5). Measured 8.7:1 on the frame | `contracts/graphic_inspect.md` |
| V1-9 | Not covered: phone, CRT pause C26, lamp warning bursts, Figma | Still open (§6) | — |
| V2 HIGH | Zoom step off-centre | **FIXED**: the zoom frames the region nearest the base view centre. The placard's legend region is MEASURED on the v3 sheet (299.6, 49.7, 111.0 × 174.3 mm; 平面视觉 to replace from `placard.swift`). Legend at viewport x 0.356–0.644, y 0.158–0.962; its 2.4 mm line **82 px above the hint** (≥ 40) | `images/f_02` |
| V2 HIGH UI | Hints not through 平面视觉; slot inside the 72 px margin | **Margin FIXED** (contract C11 `InspectHintBottom` 64 → 83: glyph bottom **77 px** from the edge, was 53). Prompts now verb + object (PROPOSED: `E · READ PLAN`, `E · EXAMINE KEY`, `E · READ SIGN`). The rest is **HANDED** (G1–G7) | `images/f_03` |
| V2 MEDIUM evidence | HUD captured under the post | **FIXED**: 平面视觉's method (overlay canvases on a post-free camera, linear composite); the UI layer is saved per frame (`ui_*.png`) and measured | `W/inspect_work/fix/tools/hud.py` |
| V2 MEDIUM | Key close-up small and flat | **Fill FIXED**: FOV solved from the real render bounds, floored by the 2.0× texel cap at the 1080 reference: **FOV 44.5, 46 % of the frame** (v1 29 %). **Reflection probe REJECTED with evidence**: one realtime probe at Open (box 0.20 m) changed 75k px of the 4K frame; the changed area got darker (luma mean 101 vs 109) and p99 rose only 179 → 183 (the chrome ring took the olive walls; the brass blade did not change). Removed. The flat brass is material and the frozen room light, not the camera (no added lamp, §3.7) | `images/f_04`; frames 36 / 37 of `runs/p1_play` |
| V2 LOW | Fit ignored the frame depth; look took the frame off screen | **FIXED**: the fit and the look limits test the 8 corners of the frame box (crown 8.95 mm proud). Composition A moves from 0.360 to **0.368 m** (16:9; fill 62.1 % × 74.2 %, close to Red's 61.7 %). Look limits yaw −1.43 to +0.28°, pitch ±1.19°: every corner stays on screen | §3.2 |
| V2 LOW | Return started at peak speed | **FIXED by contract** (C17b): both return legs ramp in over 2 frames (`OutOnset` 1/30 s). First frame 1.7 % of the way (v1 6.5 %); first 50 ms 0.156 m (v1 0.222 m); peak step +4 %; home still at 0.133 / 0.400 s | EditMode "return onset" |
| V2 LOW DECIDE | Off-axis prints keep up to 25° of yaw | **For Red** (§6.2). Unchanged |
| V2 LOW process | No Figma VL slides, AT-01e/j, phone, media | AT-01e/j **DONE** (§3.1). Media listed again (`media_candidates.md` §D). VL slides and phone still open (§6) |

## 2. Merge: packages and order

| Step | Owner | What | Command / state |
|---|---|---|---|
| 1 | map chat | `contracts/map_inspect_1_rig.diff`: C16–C19 (ShotKind.Inspect, ShotEase.Sine with the onset, fullPose, rotBlendOut, fovMin, EasedWeight, RotationWeight, 16 hits, `FrontRoomsShotTimings.Inspect`). Compiles alone; every cubic shot unchanged | `patch -p1 --dry-run < map_inspect_1_rig.diff` OK on 0a9ee7f |
| any time | visual | `W/apply/inspect_data.sh`: kitlib.py `Kit.inspect`, 5 asset modules, 11 sidecars' `inspect` key. Changes no behaviour (JsonUtility ignores the key until step 2) | **READY**: `bash /Users/redwang/FrontRoomsVisualWork/apply/inspect_data.sh` → DRY RUN OK; then `--apply` |
| 2 | visual | `W/apply/inspect.sh`: 20 new (6 runtime + 3 editor scripts, metas, 2 folder metas), 3 replaced Office scripts | **WAITING** until step 1 is in main: `bash /Users/redwang/FrontRoomsVisualWork/apply/inspect.sh` prints `WAITING … map_inspect_1_rig.diff` (exit 3, nothing written). After step 1: the same command → DRY RUN OK → `--apply` |
| 3 | map chat | `contracts/map_inspect_3_game.diff`: C1–C15, C25 (phase, events, aim, gate, routing, rig tick, SetPhase, focus, HUD hint slot, teardown, touch interim) | needs step 2 |
| 4 | sound chat | `contracts/sound_inspect.diff` | needs step 3 (`FrontRooms3DGame.Inspecting`) |

Both packages follow convention v2 (`_bases`, `_keep` (the .meta GUIDs), `_expected`, `_payload/`, `_base/`, `.diff`). The payloads equal the tested clone file for file (40 of 40). If main moves first: `W/tools/rebase_apply.sh inspect_data` (and `inspect` once step 1 is in; before that its compile check cannot pass by design).

## 3. Measured (1920 × 1080 game camera, 4× MSAA, full post; 4K = a second render)

### 3.1 The placard by lamp state (P2, seed 516574485, door open; adapt settled 3 s)

Paper = mean sRGB of blank sheet; contrasts are p90/p5 of linear luminance in the line's box (§3.4 method); `W/inspect_work/fix/tools/boxes.py`.

| State | Lamp levels (own; neighbours) | W | Lift | Paper sRGB | EVACUATION PLAN | YOU ARE HERE | Glow words (WAY ON) |
|---|---|---|---|---|---|---|---|
| Red's frame (placard track, lit) | — | — | — | 200 | 14.5:1 | — | 1.15 (spec A3) |
| **Lit** (own forced Steady) | 0.97; 0.97 0.97 0.97 none | 1.000 | 0 EV | **176** | 22.8:1 | 4.2:1 | **1.14** (faint, as intended) |
| **Own lamp off** (= as built at this seed: Dead) | 0.00; 1.00 0.97 1.00 none | 0.059 | **+2.24 EV**, contrast +12 | **124** | 14.1:1 | 5.2:1 | **1.61** (≥ 1.30, AT-01j) |
| **Dead area** (own + 4 off) | 0.00; 0 0 0 none | 0.000 | +2.24 EV | **115** | 12.2:1 | 4.9:1 | **1.54** |

Calibration: unlifted, the paper reads 0.425 linear lit, 0.037 with the own lamp off and 3 lit neighbours, 0.035 with all off. A neighbour adds about 0.002, so its share of W is 0.02 (v1 0.2). 2.25 EV brings the dark paper to sRGB 115–124 (verifier: ≥ 110). The lit legend stays faint: lit W 1.0 gives no lift.

### 3.2 Framing and readability (base composition A at 0.368 m, FOV 61)

| | Number |
|---|---|
| Base | 2.49 px/mm at 1080p (4.99 at 4K). Title 15 mm 37.4 px; glow words 5.2 mm 13.0 px; 4.2–4.4 mm lines 10.5–11.0 px; 2.4–3.6 mm lines 6.0–9.0 px (they need the zoom) |
| Zoom (when wired) | 0.184 m, 4.99 px/mm: 2.4 mm line 12.0 px (1080p) / 23.9 px (4K), 18.6′; texel magnification 0.68× / 1.36× on 7,330 px/m. Phone: 0.18 m at FOV 30.6° (the angle binds on main's sheet) |
| Solver (8 box corners) | 16:9 A 0.368; 16:10 A 0.407; 1.54 A 0.422; 1.50 A 0.433 (`distMaxA` 0.42 → **0.44**, so A holds down to 1.50 as §3.2 intends); 21:9 and phone A 0.360; iPad 1.44 B 0.328; 4:3 B 0.354 |
| Look | yaw −1.43 to +0.28°, pitch ±1.19° (the frame sits 1 % from the left edge, so the look that way is short) |
| Travel / return | in 0.43 s, 0.00 mm off the solved pose; rotation home 0.133 s, position 0.400 s |
| Door sign (test bed) | B at 0.300 m, 17 mm cap 51.9 px; no zoom |
| Key on its tag (test bed) | 0.200 m, FOV 44.5, 46 % of the frame; tag digits 99 px at 1080p, at the 2.0× texel cap (blank face: `Prop_KeyTagNo` missing) |
| Prompts (overlay, linear) | close-up hint glyph bottom 77 px from the edge; `E · READ PLAN` at 1.5 m 8.7:1; `E · EXAMINE KEY` on lit wallpaper 3.3:1 (p90 2.2:1); `E · READ SIGN` over the sign 2.2:1 (handed to 平面视觉, G6) |

### 3.3 Pause (unchanged from v1, re-run)

Freeze over 10 s: 1,557 lamps, 114 doors, Relay, placard glow, elapsed and `Time.time` unchanged; DoorMoved / FixtureChanged / LampDipped 0. A live hunting Relay: frozen for 3 s, then 2.44 m in the next 1 s. Stage-2 warning (verifier harness): Relay moved 0.0 mm, StateTime / WarnRunSeconds unchanged over 5 s, +1.000 s in the 1 s after. Exits by E, S, Esc after 0.20 s; latches hold; focus loss → Paused → back to the close-up; R ignored; gates (climb, pull-clear, chase, break, autopilot) hold. 30 Inspect shots started = 30 ended.

## 4. What changed in the build (since v1)

| File | Change |
|---|---|
| `Rendering/Inspect/FrontRoomsInspect.cs` | `ZoomInputWired` gate (no step, no CLOSER hint); zoom framed on the nearest region above `HintBand` 0.12; the light read once at Open, 0.3 m in front of the face; object FOV from the presented render bounds (`ObjectFill` 0.5, texel cap at `ReferenceHeightPx` 1080, `fovMin` when below −15); 8-corner depth into the solver input |
| `…/FrontRoomsInspectLook.cs` | `AdaptFor(W)` = log2(0.45 / W) / 2.25; `NeighbourShare` 0.02; `AdaptMaxEv` 2.25 and `AdaptContrast` 12 moved here from the map's timings (look values); lamp-less neighbours count dark; `OwnLight` (transcript glow rule: own lamp < 0.15) |
| `…/FrontRoomsInspectFraming.cs` | `depthFront/Back`; `Fits` and `FitDistance` on 8 corners; `LookLimits` (per axis, then the diagonals) |
| `…/FrontRoomsInspectDef.cs` | `sheet` (mm); `TryRegion` (sheet mm → face metres, viewer axes); `DefaultDistMaxA` 0.44 |
| `…/FrontRoomsInspectable.cs` | depth of the aim box about the face; cached verb + object prompt |
| `…/FrontRoomsInspectCatalog.cs` | `PromptFor(kit, verb)`: READ PLAN, READ SIGN, EXAMINE KEY (every `Kit_KeyTag_*` and `Kit_Key_Zone*`: direction B) |
| `Office/FrontRoomsKitLibrary.cs` | `AttachInspect` after the exit-sign block (main's 19:40 anchor) |
| Blender `kitlib.py` + 5 modules, 11 sidecars | `sheet` field; placard block (legend region, 7,330 px/m, `distMaxA` 0.44); **round and long tags** get the object block too (9 zone tags, direction B), all `ship: false` |
| Editor tests / harness | 12 new EditMode checks; PlayMode opens the start door, captures 3 lamp states, logs sheet-mm boxes per frame, captures the HUD the 平面视觉 way, checks the zoom gate, legend framing, look limits, key fill |
| Map contract | C11 `InspectHintBottom` 83; C7 note for `ZoomInputWired`; C17b `SineOutOnset` + C19 `OutOnset`; adapt amounts removed from C19 |

Rebuild in a clone: `python3 W/inspect_work/fix/fix_apply.py <clone>` (visual), `patch -p1 < contracts/map_inspect.diff` (map, clone only), Blender sidecars from `W/inspect_work/fix/kitbuild2`; runs: `FrontRoomsInspectSuites.RunBatch`, `FrontRoomsInspectPlaytest.RunBatch` / `RunNeutralityBatch`, `FrontRoomsInspectVerify.RunBatch -audioTrace` (needs main's `FMOD/` rsynced into the clone). Packages: `python3 W/inspect_work/fix/build_package.py`. Contracts: `python3 W/inspect_work/fix/make_contract.py <out>`.

## 5. Images (`images/`, JPG q85)

| File | Shows |
|---|---|
| `f_01_lamp_states_P2_vs_red.jpg` | Red's frame vs lit / own lamp off / dead area at P2, with paper and title numbers |
| `f_02_zoom_legend_framed.jpg` | v1 zoom (legend at x 0.76, cut) vs the fix (legend framed above the hint) |
| `f_03_hud_overlay_prompts.jpg` | overlay HUD: close-up hint margin and contrast; two prompts on lit surfaces |
| `f_04_key_fill_v1_vs_fix.jpg` | key on its tag: 29 % → 46 % of the frame |

Placed by the hand-off stage (2026-10-14 17:3x): VL246 close-ups (`h_01`–`h_04`), VL247 transition sheet (`h_05`), VL248 pause chart (`h_06`), VL249 `f_01`, VL250 `f_02`, VL251 `f_03`, VL252 `f_04`, VL253 `v_03`, VL254 `v_04`. The verifier's `v_01` and `v_02` are superseded by VL247 and VL248 (same checks, same verdicts, older build) and are not placed. The `h_*` images are composed from the existing `p2_play` frames and run logs; no new render.

## 6. Open items

### 6.1 Not done here
1. **Phone capture (AT-06)** and the touch contract T1–T7 (touch session). T3 (scroll, RMB, pinch → `Zoom`) is what turns the zoom on.
2. **CRT screen ads (C26)**: owner still unclear; blocking for any build where a CRT can be in the close-up ray.
3. **Lamp warning bursts** (`LampFx.Warn` has no caller in main), so a lamp frozen mid-flicker is untested.
4. **C21 first-key hand-off, re-checked against main's KeyPickup presenter** (`FrontRoomsDoorShot.BeginKeyPickup` / `PlaceKey`): `OpenHeld(key, kit, heldPose, rig)` fits it (the presenter owns the key transform, held at `HeldViewportX/Y` × `HeldDistance` until `HoldUntil`). In Phase.Inspect the game's map update does not run, so `PlaceKey` and the key clock stop by themselves. But during the 0.40 s return the game is Playing again: the presenter's clock would run on toward the pocket while EXAMINE places the key. So C21 needs, in the map-owned DoorShot: hold `keyT` and skip `PlaceKey` while `FrontRoomsInspect.Active` (until `HeldReturned`), and a `E · EXAMINE KEY` prompt during the held window behind the same gate. The exact diff waits for D1.5's dressed key rig (only `FrontRoomsFakeKeyRig` / `KitSeam` seams are in main).
5. Blank faces: `Prop_SignEngraved` (door sign) and `Prop_KeyTagNo` (tag digits) do not exist yet.
6. Figma VL slides (see §5).

### 6.2 Decisions for Red
1. **Zoom before T3.** Default: no zoom in v1 (the 2.4–3.6 mm lines stay at 6–9 px at 1080p). Option: W / stick forward = closer, one line in C7, until T3 lands.
2. **Yaw on off-axis prints.** Now up to 25° (the sheet is keystoned from a 46° approach). Option: `maxYawOff` 10–12° for prints; the swing at the 60° prompt edge then grows to ≤ 50° (about 180°/s over 0.43 s).
3. **The lift.** 2.25 EV (eye adaptation) in a dark cell, 0 when lit; the design's 1.0 EV was an estimate. It keeps the dark legend's glow ratio at 1.54–1.61.
4. **P2 under a dead lamp** (placard track): at this seed the map's roll kills the lamp over the placard. EXAMINE now lifts it; is the dark mount intended?
5. Still open from the design: control back when the camera starts home (now) or when it is home (+0.40 s).

## 7. Contracts

| To | File | State |
|---|---|---|
| map chat | `contracts/map_inspect.md` (why + tests); `contracts/map_inspect.diff` = `map_inspect_1_rig.diff` + `map_inspect_3_game.diff` | dry run OK on 0a9ee7f; bases in headers (re-checked 10-14 17:1x) |
| sound chat | `contracts/sound_inspect.md`; `contracts/sound_inspect.diff` (S1, S2, S4, S5 + AUDIO_CONTRACT lines) | dry run OK on 0a9ee7f; tested with banks |
| 平面视觉 | `contracts/graphic_inspect.md` (G1–G13) | to be filed in GRAPHIC_VISUAL_TASKS.md |
| touch session | `contracts/touch_inspect.md` (T1–T7 and the interim); T3 sets `FrontRoomsInspect.ZoomInputWired` through C7 | written 2026-10-14 (hand-off); not started |
| Red | media approval: `media_candidates.md` §D (SH2 wall photo, Gone Home lift, RE examine) | nothing on disk |

## 8. Evidence and clones

- Runs: `W/inspect_work/fix/runs/` (`s2_suites`, `b1_base_suites`, `p2_play` = final play, `p1_play` = with the reflection probe A/B, `n2_neutrality`, `v1_verify_sound`); logs `W/logs/inspect_fix/`; tables `boxes_p2.tsv`.
- Clones kept: `W/proj_inspect` (the fix build; it also carries the sound contract, main's `FMOD/` and the verifier harness, clone only) and `W/proj_inspect_base` (clean main 0a9ee7f, the baseline).
- Clones that can be removed (about 31.6 GB): `proj_inspect_fix`, `proj_inspect_fix_base` (made 10-08 20:07, never used), `proj_inspect_play`, `proj_inspect_v1`, `proj_inspect_v1_play`, `proj_inspect_v1_warn`, `proj_inspect_v1_base2`, `proj_inspect_v1_now`. Nothing was deleted.
