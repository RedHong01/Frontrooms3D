# 20 — EXAMINE prototype (close-up inspect, world paused)

Status: **PROTOTYPE BUILT AND TESTED in private clones, 2026-10-08 01:50.** Visual chat (游戏视觉). Design: `10_design.md` v1.1.
Red's project was not opened in Unity and not written, except this file, `contracts/map_inspect.diff` and `images/p_*.jpg`.

## 0. Summary

| Check | Result |
|---|---|
| EditMode (`FrontRoomsInspectTests`) | **PASS 93 / 93**: solver at 8 aspects, angular zoom, readable sizes, sidecars, Find, 0 B GC, rig contract (AT-11), 150 synthetic approaches |
| PlayMode (`FrontRoomsInspectPlaytest`, the real game, seed 516574485) | **PASS 89 / 89**: prompt range, entry, 10 s freeze, live-Relay freeze, E / S / Esc / focus / R, return timing, Camera motion 50 / Off, gates, key object kind, OpenHeld, 150 approaches in the real map |
| Pause neutrality (AT-01g, 3 replays) | **PASS**: a 10 s and a 0.25 s close-up give the **same** state at 40 s of game time (player, Relay position / state / StateTime, 1,557 lamps, 115 doors, elapsed); the two controls are identical too |
| Map tests and 20 validator seeds | **Unchanged** vs a clean clone of current main: map signatures (20 seeds) **byte-identical**; camera rig, glass shot, map interaction (153), fixture tick, captions **byte-identical** reports; relay nav and 100-seed map verification identical except a timing / timestamp field |
| WebGL compile (gated) | **0 errors**: Assembly-CSharp with `UNITY_WEBGL` and no `UNITY_EDITOR`; editor + `FRONTROOMS_WEBGL_PREVIEW`; iOS defines too |
| Contract diff | `contracts/map_inspect.diff`, 37 hunks, **dry run OK** on main **bc0aa79** (base sha1s in its header) |
| Captures | 17 JPGs in `images/p_*.jpg` (1080p, 4K crops, transition, enter-read-exit sheet) |

Not merge-ready yet: the visual files need the map chat's rig and timing contract (C16–C19) in main first (design §7.1 merge order). No apply package was made in this stage.

## 1. What was built

### 1.1 Visual-owned (would go to main via `W/apply/inspect.sh`, after C16–C19)

| File | New / changed | What |
|---|---|---|
| `Assets/Scripts/Rendering/Inspect/FrontRoomsInspect.cs` | new, 769 lines | Facade: registry, `Find` (padded OBB, reach and angle with hysteresis, 0 alloc), `CanOpen` (cached solve), `Open`, `OpenHeld`, `Tick` (look ±3°, pan when zoomed, panel orbit, object turn, zoom), `Close`, `Abort`, `Dt`, events, hint queue (2 showings each), transcript gate, hidden runner (object placement through the return, look volumes) |
| `.../Inspect/FrontRoomsInspectable.cs` | new | Per-instance target: anchors from the sidecar, world face and box at query time, `Attach`, gizmo of the solved pose |
| `.../Inspect/FrontRoomsInspectFraming.cs` | new | Pure solver of §3.2 (composition A / B, yaw ≤ 25°, body band tilt, ±20° and B fallbacks, path sphere = near-corner radius at the travel FOV + 1 cm, 0.15 m stand-off) and the angular zoom of §3.4 |
| `.../Inspect/FrontRoomsInspectLook.cs` | new | Runtime volumes: Shot (vignette +0.04 / +0.08, LD −0.001, CA max(0.001, base − 0.04)), Adapt (+1 EV, +12 contrast, τ 0.6 / 1.2 s, dark cells only), DOF (object kind, desktop only, 1 mm f/32 → URP-model target), base snapshot from the stack, prewarm frame |
| `.../Inspect/FrontRoomsInspectDef.cs` | new | The §3.8 `inspect` block and its defaults |
| `.../Inspect/FrontRoomsInspectCatalog.cs` | new | Proposed strings (平面视觉 to replace) and test-bed fallback blocks (clock, vending, copier, phone; `ship: false`) |
| `Assets/Editor/Rendering/Inspect/FrontRoomsInspectTests.cs` | new | EditMode tests (§3 below) |
| `Assets/Editor/Rendering/Inspect/FrontRoomsInspectPlaytest.cs` | new | PlayMode harness and the AT-01g replays |
| `Assets/Editor/Rendering/Inspect/FrontRoomsInspectLookdev.cs` | new | Every kit with a block on a lit L0 wall at the game numbers, 1080p + 4K, p90/p5 contrast probe |
| `Assets/Scripts/Office/FrontRoomsKitLibrary.cs` | +23 / −3 | `Info.inspect`; `Spawn(…, bool inspectable = true)` attaches only in Play Mode, and only `ship` blocks unless `DevTargets` |
| `Assets/Scripts/Office/FrontRoomsDressJob.cs` | +5 / −4 | `Kit(…, inspectable)` (main moved pile dressing into this job at 00:2x) |
| `Assets/Scripts/Office/FrontRoomsFurniturePile.cs` | +1 / −1 | pile pieces pass `inspectable: false` |
| `Tools/Blender/frontrooms_kit/kitlib.py` | +30 | `Kit.inspect(kind, box=…, ship=False, **fields)`; the sidecar writer converts the aim box to Unity part-local |
| `.../assets/evac_placard.py`, `interact_key_tag_rect.py`, `interact_door_sign.py` | +10 / +5 / +5 | the three blocks below |
| `Resources/Props/Models/Kit_KeyTag_Rect{,_Blue,_White}.json`, `Kit_DoorSign.json` (+ `Kit_EvacPlacard.json` with Q16) | regenerated | Blender re-export of the three modules (`W/inspect_work/kitbuild`): every other key is byte-equal to main; only `inspect` is added |

Verified copy of every file: `W/inspect_work/src/`. The visual-owned changes re-applied to main **bc0aa79** give byte-identical files to the tested ones (`W/inspect_work/visual_owned.diff`).

### 1.2 The three prototype assets

| Asset | Kind | Block | Where in the test |
|---|---|---|---|
| **Q16 evacuation placard** | flat, `ship: true` | §3.8 exactly: composition A `[0.01, 0, 0.63, 1]`, d 0.36, reach 1.6, 60°, 6,000 texel/m, smallest cap 2.4 mm (region rect a placeholder until 平面视觉 exports it) | The placard workflow's build from `W/proj_placard` (art v2, 17:24), placed by the game's own path (`EndStreamAt` → `Prepare` → `MapRunStarted` → `Place`, spec §5). Seed 516574485 put it at mount **P2** (side wall, west) |
| **Key + ring + tag** (3D) | object, `ship: false` until D1.5 | on `Kit_KeyTag_Rect`: d 0.20 / zoom 0.15, FOV −10, 3,300 texel/m, 15 mm digits | Test bed: hung on the facade wall at 1.40 m, `Display` = the assembly |
| **Door sign** | flat, `ship: false` | R5 alternative B (C22): reach 1.2, no zoom (17 mm cap), d 0.30 | Test bed: on a wall at 1.524 m, ≥ 0.8 m from the placard |

### 1.3 Clone-only (never main)

- **Map contract applied in the clone** (`W/inspect_work/patch/map_contract.py`): C1–C19, C25, the C13 interim and C15 on `FrontRoomsCameraRig.cs`, `FrontRoomsShotTimings.cs`, `FrontRooms3DGame.cs`, `FrontRooms3DGame.Mobile.cs`. The same edits, as a diff anchored on current main: **`contracts/map_inspect.diff`**.
- The Q16 placard build, its RenderSetup / RoomStream patches and contract Q16-1 (`KeepClearAtStart`), all as the placard track wrote them.
- `Assets/Editor/Audit/FrontRoomsInspectSuites.cs`: runs the project's suites + 20-seed signatures in one session.

### 1.4 Rebuild

```zsh
W=/Users/redwang/FrontRoomsVisualWork
$W/tools/mkclone.sh proj_inspect --feature placard
python3 $W/inspect_work/patch/map_contract.py   $W/proj_inspect     # the map chat's contract, clone only
python3 $W/inspect_work/patch/visual_inspect.py $W/proj_inspect     # placard build + visual files + sidecars + runner
Unity -batchmode -projectPath $W/proj_inspect -buildTarget OSXUniversal -executeMethod FrontRoomsInspectSuites.RunBatch          # EditMode + unchanged
Unity -batchmode -projectPath $W/proj_inspect -buildTarget OSXUniversal -executeMethod FrontRoomsInspectPlaytest.RunBatch        # PlayMode, frames
Unity -batchmode -projectPath $W/proj_inspect -buildTarget OSXUniversal -executeMethod FrontRoomsInspectPlaytest.RunNeutralityBatch
python3 $W/inspect_work/patch/make_contract_diff.py "<this folder>/contracts/map_inspect.diff"   # re-anchor on main + dry run
```
Clones: `W/proj_inspect` (suites, neutrality), `W/proj_inspect_play` (play, look-dev), `W/proj_inspect_base` (clean main baseline). Logs: `W/logs/inspect/`. Raw results: `W/inspect_work/runs/` (p6 = final play, n2 = neutrality, s5 = suites + `base_suites`).

## 2. How it behaves (measured)

| Step | Number |
|---|---|
| Prompt | none at 1.75 m and 2.4 m; `E · READ` at 1.58 m; kept at 1.66 m (hysteresis to 1.7); shown at 58°, none at 70° |
| E → close-up | phase Inspect and `timeScale` 0 on the commit frame; `Time.time` advances one frame (16.7 ms) over the entry, then 0 |
| Travel (placard, 1.5 m) | 1.24 m in **0.43–0.44 s**, sine in-out, peak **4.43 m/s** (design 4.3 at 1.2 m); FOV 76 → 61 |
| Pose | the rendered camera is **0.00 mm** off the solved pose in every opening (the rig's clamp never fired) |
| Hold | ±3° look clamps at 3.00°; zoom dolly to **0.184 m**; pan inside the swept limits |
| Exit | E, S or Esc after 0.20 s (Esc at 0.02 s ignored); control the same frame; `Time.time` +0.0 ms across the exit frame; rotation home **0.13–0.15 s**, position **0.40 s**; `ShotEnded(Inspect)` once |
| Latches | E held after the exit opens nothing (no door: DoorMoved 0); S held 0.5 s after an S exit moves the player **0.0 mm**; S again walks back |
| Return | a fresh E during the 0.40 s return is swallowed (prompt stays) |
| Camera motion | 50 %: same pose, in 0.65 s, home 0.60 s (×1.5). Off: a cut both ways (one frame) |
| Focus / R | focus loss → Paused (from Inspect, timeScale 0) → Esc → the same close-up; R in a close-up ignored; afterwards Playing → Esc → Paused → Esc → Playing |
| Gates | no prompt during a climb, a pull-clear step, Chase, BreakDoor, or under the autopilot |
| Key (object) | centred at **0.200 m**, off-axis 0.1 mm, FOV 66; turns 90° / 180°; back on its hook exactly (local pose restored); OpenHeld fires `HeldReturned` once |
| Events | 26 Inspect shots started = 26 ended; `Inspecting(true)` 25 = `(false)` 25 |
| Expected exceptions only | the FMOD bank (clones have no banks) and Unity's QuickSearch index |

## 3. Tests

### 3.1 Pause correctness

| Test | Result |
|---|---|
| Freeze, 10 s close-up (AT-01f) | **unchanged**: 1,557 lamp levels, 114 doors, placard glow fields, `elapsed`, `Time.time`, Relay; **0** DoorMoved, FixtureChanged, LampDipped (counted from the commit frame) |
| Freeze with a live Relay | Relay placed 12–25 m away, hunting (moved 2.44 m in 1 s); a 3 s close-up: position, state, StateTime, lamps, doors unchanged; after the exit it moved 2.44 m in the next second |
| FixtureTick | frozen at timeScale 0 (`TickFixtures(Time.deltaTime)` gets 0), as the design says |
| No time jump | `Time.time` < 2 frames across the entry and exit frames in all 10 checked entries and their exits |
| No lost or doubled input | exit E swallowed (rig `Consume`, shot ending); E and S latches; mouse look moves the base on the first frame after exit |
| **Pause neutrality** (AT-01g) | 3 replays, captureDeltaTime 1/60, input injected by play-frame index, E at play frame 720 (streaming idle 684 frames), Relay released at 660. Control A vs B (0.25 s): **identical**. Control vs 10 s: **identical** (player (257.590, 25.900), Relay (275.633, 67.500) Wander 6.100 s, 1,557 lamps, 115 doors, elapsed 40.016) |

The short hold is 0.25 s, not 0.2 s: at exactly 12 frames the summed inspect clock is a hair under `CancelAfter` 0.20, so the exit E would be ignored for one frame (harmless in play; noted in §5).

### 3.2 The shot never clips (50 approaches per asset)

| Where | Placard | Door sign | Key (object) | Clock (T1) |
|---|---|---|---|---|
| Real map (body must fit) | 50 stood → 50 poses, **0 clipped** | 50 → 50, **0** | 49 → 49, **0**, object path clear | — |
| Synthetic room (pier, side wall, ceiling) | 44 poses, 6 no stand, **0** | 43, 7, **0** | — | 46, 4, **0** |

Each pose: near-plane sphere (0.113 m) clear at the pose and at 20 points on the eased path; all 4 frame corners on screen; no collider between the camera and any corner. 5 real openings per asset in the map: the camera sat **0.00 mm** from the solved pose. A fully blocked path gives no pose (no prompt).
Two bugs this caught, both fixed: the path sphere used the 61° close-up FOV (0.104 m) instead of the 76° travel FOV (0.123 m); and the key flew straight from its hook, grazing the wall from a 56° stand (now a Bézier that lifts off the wall first, ≤ 0.15 m).

### 3.3 Readable text (1080p and 4K, desktop reference)

Placard composition A at 0.36 m, FOV 61 (2.55 px/mm at 1080p, 5.09 at 4K); zoom 0.184 m (4.99 / 9.98 px/mm).

| Line | Cap | Base 1080p / 4K | Zoom 1080p / 4K | Zoom arcmin desktop / phone |
|---|---|---|---|---|
| EVACUATION PLAN | 15.0 mm | 38.2 / 76.4 px | 74.8 / 149.6 | 116′ / 114′ |
| Glow legend words | 5.2 | 13.2 / 26.5 | 25.9 / 51.9 | 40.3′ / 39.4′ |
| YOU ARE HERE, footer | 4.4 | 11.2 / 22.4 | 21.9 / 43.9 | 34.1′ / 33.4′ |
| EXIT (plan) | 4.2 | 10.7 / 21.4 | 20.9 / 41.9 | 32.6′ / 31.8′ |
| WALLCOVERING 1/25 | 2.4 | 6.1 / 12.2 | **12.0** / 23.9 | **18.6′** / 18.2′ |

Every line reads (≥ 18.6′ desktop, ≥ 18′ phone) at the zoom step. Texel magnification at the zoom: 0.83× (1080p), 1.66× (4K), cap 2.0×. Key tag digits: **62.4 px** (1080p), 124.8 (4K), 1.26× magnification. Door sign: 51.9 / 103.9 px, so no zoom is offered.
Solver numbers (AT-01d): 16:9 A 0.360 m, frame 61.9 % × 74.0 %, x 0.015–0.635; 16:10 A 0.393; 1.54 A 0.408; 1.50 A 0.419; 21:9 A 0.360; phone 2.17 A 0.360; iPad 1.44 **B 0.319**; 4:3 **B 0.345**; all 4 corners on screen in every case. Zoom: phone 0.18 m at FOV **31.2°**, iPad **51.4°**. Clock: 40.1° up, camera 1.720 m.
Look-dev contrast (face p90/p5, lit stage): placard base 9.3 (1080p), zoom 2.1; door sign 1.25 (blank face, below).

### 3.4 Unchanged and compile

- Suites in one session per clone, clean main baseline vs this clone: listed in §0. Main's own MapInteractionTests console (6 warnings, 2 errors, 4 exceptions) is the same in both.
- Roslyn (Unity's csc, outside Unity): all 7 Assets assemblies 0 errors; WebGL player defines 0 errors in Assembly-CSharp (the editor assembly is not built for players); `FRONTROOMS_WEBGL_PREVIEW` 0 errors; iOS 0 errors. `FrontRoomsInspectLook` drops the DOF volume and prewarm under `(UNITY_WEBGL && !UNITY_EDITOR) || FRONTROOMS_WEBGL_PREVIEW || UNITY_IOS || UNITY_ANDROID`; desktop code is unchanged by the gate.
- Budgets (edit mode, under load): `Find` 3.5 µs with 3 targets, `Solve` 68 µs without the world; **0 B** GC over 1,000 Find and 100 Solve calls.

## 4. Frames (`images/`, JPG q85, all from the game's camera, 4× MSAA, full post)

| File | Shows |
|---|---|
| `p_01_before_prompt_2p4m.jpg`, `p_02_prompt_1p5m.jpg`, `p_12_prompt_before_after.jpg` | before / after the prompt |
| `p_03_placard_closeup_1080p.jpg` (hint on), `p_04_…_clean.jpg` | the placard close-up, Red's frame: plan in the left 63 %, WP03 wallpaper on the right |
| `p_05_placard_closeup_4k_crop_legend.jpg` | native 4K crop of the base close-up, legend side |
| `p_06_placard_zoom_1080p.jpg`, `p_07_placard_zoom_4k_crop_legend.jpg` | zoom step 0.184 m |
| `p_13_transition_in.jpg` | in at 0.10 / 0.22 / 0.33 / 0.43 s (HUD fading) |
| `p_14_sequence_enter_read_exit.jpg` | the contact sheet: walk up, enter, read, zoom, pan, held 10 s, exit at 0.05 / 0.15 / 0.30 s |
| `p_08`, `p_09`, `p_16` | door sign close-up (R5-B test bed) |
| `p_10`, `p_11`, `p_15` | key + ring + tag: arrive, turn, zoom, return |
| `p_17_clip_samples.jpg` | one real opening per asset from a random approach |

Not yet in the Figma VERIFICATION LOG (the hand-off stage places them).

## 5. Deviations from the design, and why

1. **C8 restores `timeScale` 1 only if the close-up set 0** (`timeFrozenByInspect`). The design writes 1 on every `SetPhase`; capture harnesses (G10, TransitionHandoff) freeze time on their own.
2. **C11 hint slot (PROPOSED):** in a close-up the prompt slot moves to the bottom band, 64 units above the edge. In the centre slot (−44 px) the hint covered the plan and the key tag (first run). 平面视觉 places it for real.
3. **`CanOpen` is not gated on `Active`,** so the prompt stays during the 0.40 s return as §2.2 says (E is swallowed by `rig.InShot`).
4. **Path sphere uses the travel FOV** (76°), as §3.2 says; the first build used the close-up FOV.
5. **Object kind lifts off its mount** (quadratic Bézier, ≤ 0.15 m along the face normal) before travelling. The design only checks the view axis; from oblique stands the straight path grazed the wall.
6. **`FrontRoomsDressJob` gained the `inspectable` flag**: main moved pile dressing into the dress job (Q13, 00:2x), so `FurniturePile` passes the flag there.
7. **Door sign** carries the R5-B block as a test bed (`ship: false`); v1 still reads it in place (R5 default).
8. **Look-dev** renders on a lit L0 wall stage; the in-game frames are the reference.
9. Shot-volume weight follows the rig's eased position weight; the DOF target uses URP's Bokeh model (f 16.8 mm at f/4 gives 5 px at 3 m with the object at 0.2 m), not the physical 3.7 mm camcorder number, which URP's formula would render almost sharp.

## 6. Findings and open items

1. **Blank faces:** `Prop_SignEngraved` (door sign text) and `Prop_KeyTagNo` (tag digits) do not exist in main, so both test-bed close-ups show blank faces. The mechanics are proven, the readable content is not. Owner: the interactables track / D1.5.
2. **The in-game placard is darker than the look-dev and its legend glows green in the lit state** at the tested mounts (P1 and P2). The adapt lift stayed 0 (lit cell, W ≥ 0.45). This is the placard track's glow-cell question (30_render.md §0 FLAG), not EXAMINE's; the close-up shows the frozen frame honestly.
3. **Exit at exactly 0.20 s:** the summed inspect clock can sit just under `CancelAfter`, so an E on frame 12 is ignored for one frame. Harmless; a 1e-4 epsilon in C6 would remove it (map chat's call).
4. **Rotation-home threshold:** in game it reads 0.133 s at a 0.05° threshold (sine out settles); the rig test reads 0.150 s. Both in tolerance.
5. **Main moved during the work:** Q13 dress hitch (00:2x), MapWorld time-sliced dressing (01:23), `FrontRooms3DGame.cs` working-tree edits, then commit bc0aa79. The contract diff was re-anchored at 01:50 and dry-runs clean; its header carries the base sha1s. Re-anchor again if any differs.
6. **Not done here:** contrast per lamp state (AT-01e / AT-01j) and the phone capture (AT-06); the transcript panel (C11 text, 平面视觉); sound (S1–S5) and touch (T1–T7) contracts; WebGL build run (compile only); the apply package `W/apply/inspect.sh` (after C16–C19 land); Figma VL slides.
7. **Merge order stays as designed:** C16–C19 (rig and timings) first, then `inspect.sh`, then the rest of the map contract.

## 7. Contract for the map chat

`contracts/map_inspect.diff` (316 lines added, 38 removed, 37 hunks, 4 files): C1 phase, C2 events and state, C3 aim, C4 gate, C5 / C5b commit after `relay.Tick`, C6 routing (Esc exits), C7 single rig tick, C8 `SetPhase` (pausedFrom, HUD phase, guarded timeScale), C9 R, C10 focus, C11 HUD (hint alpha and the proposed bottom slot), C12 / C12b / C12c teardown, C13 interim (`MenuState.Playing` until T1), C15 Back and tap, C16–C18 rig (`ShotKind.Inspect`, `ShotEase.Sine`, `fullPose`, `rotBlendOut`, `fovMin`, `EasedWeight`, `RotationWeight`, 16 hits), C19 `FrontRoomsShotTimings.Inspect`, C25 latches. Cubic shots behave exactly as before (rig tests byte-identical).
