# 40 — Door vision lites: the prototype (built, one test round, one video)

- **Line:** 游戏视觉, session local_5656cffd (account A), door-vision-panel workflow, stage "Prototype".
- **Date:** 2026-10-30, 13:09 to about 14:00.
- **Status:** READY for Red's look. Everything was built in the clone `W/proj_doorvision` (W = `/Users/redwang/FrontRoomsVisualWork`). Main was only read; nothing is packaged for main yet (the door kits are being changed by the door-break production, section 6).
- **Design:** `30_design.md` (the DA-L1 lite, LIVE / DARK, the PEEK, the one-way rule). Red's asks of 2026-10-30 are quoted there.
- **Video:** `video/door_vision_prototype_20261030.mp4` (23.8 s, 1920 x 1080, 30 fps, H.264 CRF 23, yuv420p, faststart, silent, 9.1 MB), its contact sheet `video/door_vision_prototype_20261030_contact.jpg`, and the Shot-format check grid `video/door_vision_prototype_shot.jpg` (for the VERIFICATION LOG batch).
- **Contract for the other lines:** `W/apply/door_vision_contract/` (`README.md`, `CONTRACT_map.diff`, `CONTRACT_interaction_peek.diff`).

---

## 1. What Red will see in the video

1. The title stream's double doors now have a narrow wired-glass window in each leaf. The window is a dark mirror until the room behind connects, then it turns clear as the door swings.
2. A map door (walnut, double-acting) has the same 10 x 30 in window with a walnut bead. Walk up, aim at the glass: the eye and "E · LOOK THROUGH" appear.
3. E glides the camera to the glass, and you look through a diamond wire mesh into the next room while the world keeps running. Look around, lean to the edges, then E goes back.
4. Behind the glass the Relay walks into view. It does not react (its sight test reads false through the leaf). After 0.8 s the camera pulls back, and the door prompt switches to OPEN DOOR because the Relay is in sight.
5. When the room behind is "not ready" (a stand-in switch here), the window fades to dark reflective glass in 0.4 s and the peek prompt is gone. Last come the steel leaf with its pressed steel lite kit, and the station entrance's dead window: dark glass with no hole.

---

## 2. What was built (clone only)

| # | File (clone) | What |
|---|---|---|
| 1 | `Tools/Blender/frontrooms_kit/assets/interact_door_lite_common.py` (new) | DA-L1 lite. 254 x 762 mm visible glass at Z 0.273–0.527, Y 1.143–1.905. A cut-out of the glass blank (+9.5 mm per side) in every LOD. 6.8 mm glass with a 1 mm gap. The mitred walnut bead has 4 separate members so the grain runs along each, and goes into the leaf's own wood slot. The steel lite kit uses the SteelLite section with the lip widened to the 6.8 mm glass; it is painted with the leaf and has 6 oval-head screws on face S. LOD1 uses a 6-7 point section, LOD2 a flat ring plus one glass quad per face. Sidecar keys: `lite_centre`, `lite_dir`, `liteRect`, `liteGlass`, an `inspect` block of kind `peek` (reach 0.9, two-sided 35°, aim box = the glass inset 10 mm). Section 1.5 asserts |
| 2 | `interact_door_leaf_veneer_da.py`, `interact_door_leaf_steel_da.py`, `interact_door_da_common.py` | The lite through the existing `extra` hook. `leaf_anchors(..., sign_yz)` moves the locked-door sign to (Y 2.010, Z 0.400) on the lited steel leaf; every other kit keeps (1.524, 0.500) |
| 3 | `Assets/Resources/Props/Models/Kit_DoorLeaf_Veneer_DA`, `Kit_DoorLeaf_Steel_DA`, `Kit_DoorLeaf_Steel_PaintedMetal_DA` (.fbx/.json) | Regenerated (Blender 4, `build_asset.py`) |
| 4 | `Assets/Resources/Rendering/FrontRoomsGlass.shader` | `_FR_GLASS_WIRE` (local fragment keyword): an analytic, box-filtered wire mesh (pitch 12.5 mm, wire 0.46 mm, 45° diamond, `_WireSoftPx`). `_LiteDark` (DARK: opaque dark pane, reflection kept). `_PeekReflect` (the peek cuts the reflection to 15 %). Both CBUFFERs updated (SRP Batcher) |
| 5 | `Assets/Resources/Surfaces/Glass_Wired.mat`, `Glass_WiredDark.mat` (new) | The two shared lite materials (alpha 0.14 + wire, F0 0.085, RT receive on) |
| 6 | `Assets/Scripts/Rendering/Doors/FrontRoomsDoorLite.cs` (new) | Registry, LIVE / DARK with the 0.40 s sine fade, pooled fade materials (only a lite in view fades; one out of view snaps), the peek weight, the readiness hooks (`MapDoorReady`, `ForceNotReady`, the stream's `Ready`, `SetDead`), `PassesPlayerSight(collider, point)`, the holed shadow box (32 tris) |
| 7 | `Office/FrontRoomsInteractableKit.Door.cs`, `Office/FrontRoomsKitLibrary.cs` | `DressDoor` attaches the lite and the peek's `Available`. The shadow proxy of a lited leaf is the holed box. The sidecar `liteRect` is read through `Info` |
| 8 | `Rendering/FrontRoomsDoubleDoorKit.cs` | Title pair (`Faces.Both`): the slab with the glass blank cut through it (`BevelBoxHoled`), a mitred walnut bead on both faces in the leaf's mesh (no extra opaque draw), and a 6.8 mm wired pane that starts DARK. Station entrance (`Faces.Plus`): the bead on +Z and a 1 mm DARK skin 0.5 mm proud, a dead door with no hole and no peek. `Lites = false` under `UNITY_WEBGL && !UNITY_EDITOR` |
| 9 | `Rendering/Inspect/FrontRoomsInspect*.cs` | `InspectKind.Peek` (append-only). Glass-relative pose at 0.100 m on the eye's side, height held to 1.35–1.75 m; look ±35° / ±30°; head lean ±5 cm past 70 % of a clamp; near plane 0.02 m within 0.25 m of the glass, restored after; the lite's peek weight; two-sided approach. Look volume: colour filter (0.95, 0.985, 0.955), −0.32 EV, vignette +0.10, adaptation capped at +0.5 EV. Prompt "E  ·  LOOK THROUGH" (PROPOSED). Lab switch `PeekPausesWorld` |
| 10 | clone-only glue, written out as contracts | `FrontRooms3DGame.cs` (the live peek: `BeginPeek` / `TickPeek` / `EndPeek`, gate, exits, the 0.8 s glimpse, HUD hint), `FrontRoomsCameraRig.cs` (`ClampIgnore`), `FrontRoomsMapHunter(.V2).cs` (`VisibleToPlayer`, the peek eye), `FrontRoomsRoomStream.cs` (the stream lites' readiness, the dead terminal door). See `W/apply/door_vision_contract/README.md` |
| 11 | `Assets/Editor/DoorLite/FrontRoomsDoorLiteFilm.cs` (clone-only, ASCII, no tuples, no GetInstanceID) | Play-mode harness. It films the title crawl, then starts the game's own run (seed 516574485 via `Random.InitState(4242)`) and picks a lited L0-F door with 7.3 m clear on both sides and lit lamps. It drives the game by injected input (never the autopilot), places and walks the Relay with the tools API (`DebugPlace`, held), and composites the HUD as the EXAMINE playtest does. It renders every frame through the game camera, with RT glass off. Output: frames + `frames.tsv` + `stats.json` |

Not built (as scoped): Oak_DA, Ward_DA (same code path, production); `Kit_DoorLeaf_SteelLite_DA` (untouched, retired at production); the cracked-and-held lite (door-break production); WebGL lites (Q-C); touch (contract I-L4); any sound; a device run.

---

## 3. The one test round (numbers)

The film harness ran twice. Run 1 found two problems: the fade pool grew to 124 materials, and two G4 measures mixed the far room's parallax and the post grain into the wire reading. One fix went in: lites out of view now snap instead of fading, the 12-step fade lands exactly on 0.40 s, and the G4 measures isolate the wire. Run 2 is the record below. The suites ran once, after the fix.

| Gate | Result | Numbers |
|---|---|---|
| G1 Generator | **PASS** | Veneer_DA tris LOD0 2736→2944 (**+208** ≤ 400), LOD1 1068→1228 (**+160** ≤ 160), LOD2 148→188 (**+40** ≤ 40). Steel_DA 4968→5536 (**+568** ≤ 650), 1580→1724 (**+144** ≤ 160), 172→212 (**+40** ≤ 40). Frame Z 0.254–0.546: **54 mm** to the break window, **49 mm** to the damage mask. Glass pocket clearance **1.0 mm**; closure 0 (the bead leg ends on the cut edge). Sign **48 mm** above the frame, **47 mm** under the leaf top |
| G2 Compile | **PASS** | Roslyn macOS, all 7 Assets assemblies: 0 errors. Roslyn iOS defines: 0 errors. Assembly-CSharp with WebGL player defines: 0 errors. No warning in any changed file. Unity compile in both runs: 0 `error CS`. Harness: ASCII, no tuples, no GetInstanceID |
| G3 Draws (structure) | **PASS** | Lited leaf: 3 LOD renderers, each **+1 Glass_Wired slot**. Opaque slots unchanged (LOD0 Veneer + Brass, LOD1 Veneer + Brass, LOD2 Veneer): **+0 opaque**. Shadow proxy replaced by the holed box (32 tris): **+0 shadow casters**; the glass has no caster pass. Counted from the live renderers; GPU draw counters are not readable in batch |
| G4 Wire stability | **PASS** | The wire's own contribution, frame to frame, in a lateral dolly: **0.49 %** at 3 m, **0.89 %** at 6 m (≤ 2 %). For reference, the whole glass varies 1.6 % / 1.9 % with the far room's parallax |
| G4 Wire resolves | **FLAG** (measure, not look) | Local contrast median with the wire on vs off: **4.1 %** in the peek, **9.5 %** at 1 m (target ≥ 10 %). This measure is contaminated: 43–55 % of the glass pixels differ by more than 2 % between two renders of one pose that changed only the wire, which is the post stack's per-render grain. The frames show the mesh clearly (see the PEEK tile, about 70 px pitch). The next stage measures with grain off |
| G5 Peek | **PASS** | Camera **0.100** m from the glass at full weight (min = max = 0.100, 118 samples). Near plane 0.02 m inside the 0.25 m band and 0.06 m outside: **0** wrong samples. Nearest near-plane corner **40 mm** in front of the bead's top at the look extremes. Travel in **0.400 s**, return 0.433 s (0.40 s + the 1-frame end detection). Phase stays **Playing**, timeScale 1.000. Hint "E  ·  BACK". The clamp's ignore is cleared after the return |
| G6 One-way | **PASS** | Relay 2.4 m behind the lite, lit, player at 0.80 m: the Relay sees the player **0/30**; the player sees the Relay **30/30** (30/30 samples had both Relay points inside the glass); the ray passed a lite 30/30. During the peek the Relay sees the player **0/23**. Glimpse **0.800 s** from the first RelayOnScreen frame to the return (ended by "glimpse") |
| G7 States | **PASS** | LIVE→DARK **0.400 s**, DARK→LIVE **0.400 s**; **0** peek prompt frames while not LIVE (the prompt falls back to "E · OPEN DOOR"). Mean glass luminance (linear) DARK 0.042 (p95 0.094, the reflection) vs LIVE 0.186. Title stream: 10 stream lites; not-ready and DARK 8, **not-ready but LIVE 0** (the seal never shows); in the crawl, 164 samples DARK ahead and 95 LIVE. Station skin: 2 dead DARK skins, no EXAMINE target, skin 0.5 mm proud of the leaf face against a wall 2.25 mm behind it (no shared plane) |
| G8 Cost | **PASS** (CPU) / not measured (RT GPU) | `FrontRoomsDoorLite.TickAll` **0.017 ms/frame** with 137 lites (≤ 0.02). `PassesPlayerSight` 0.6 µs a call. Fade pool: 3 materials at the first settle (run 1: 124), 20 by the end of the run (the fades of lites in view; the design's 4 is not a hard cap, see section 5). RT on vs off GPU delta: not measured (batch runs with `-frGlassRT off`) |
| G9 Isolation | **PASS** | Of 378 kit FBX/JSON files, only the 3 intended pairs changed (Veneer_DA, Steel_DA, Steel_PaintedMetal_DA). `Kit_DoorLeaf_SteelLite_DA` is untouched. WebGL-define compile OK with the title-pair lite gated off |
| G10 Rules | **PASS** | LF only (every changed file and diff checked). Silencer present (`CloneOnly/FrontRoomsCloneOnlySilence.cs`). No sound made. At most 2 visual-line Unity jobs (`tools/unity_jobs.sh` gate in `door_vision/scripts/run_unity.sh`). Main read only; no git writes; nothing deleted (the killed run 2 attempt was moved to `W/_partial/door_vision_film2_killed_20261030`) |
| EXAMINE suites | **PASS** | `FrontRoomsInspectTests` **130 passed, 0 failed**. `FrontRoomsInspectPlaytest` **134 passed, 0 failed** |
| Title stream | **PASS** | `FrontRoomsStreamVerification` **5/5** (handoff, prepared rooms, walking run, standing caught, sprint escape) |
| Station / exit run | **PASS** | `FrontRoomsMainScenePlaytest -autopilotExit -autopilotStation 4 -autopilotSeed 2554`: ended by exits, 4 floors, 44/44 checks ok, 0 errors (section 3.1) |

### 3.1 Station / exit autopilot

**PASS: "ended by exits".** Seed 2554, station 4: 4 floors through the EXIT, not caught, 0 errors, **44 checks ok, 0 not ok** (the exit, station, door and key checks). p95 frame 16.8 ms (Editor). The run went through the map with the lited leaves, the player-side sight rule (M-L2 glue) and the stream lites live. The autopilot gates close-ups off, so it never peeks; the peek's own checks are in G5 to G7.

### 3.2 Peek check per door type

| Door | What renders through the lite | Peek |
|---|---|---|
| Title stream door | DARK (dark wired mirror) while the room behind is not connected; the real room once it is (fade 0.40 s). The rear seal never shows (0 not-ready LIVE lites) | none (no EXAMINE target; stream doors open by proximity) |
| Station entrance | a dead door: walnut bead + DARK skin, no hole, so no void and no wall behind it can show | none |
| Map door, room built | the real room, live (lamps, the Relay), through the diamond mesh | opens at ≤ 0.9 m, still; 0.10 m from the glass |
| Map door, room not ready (stand-in switch) | DARK in 0.40 s: an opaque dark pane that keeps its reflection. No void, no fake room | prompt gone; E opens the door |

---

## 4. Files and folders

- Clone: `W/proj_doorvision` (MKCLONE.txt: main 3ea3d7bb, synced 2026-10-30 13:09; 6.6 Library cp -Rc from `proj_streamdoor`).
- Harness output: `W/door_vision/out/film/` (run 2: frames, `frames.tsv`, `stats.json`, `film_log.txt`); run 1 in `W/door_vision/out/film_run1/`.
- Suites: `W/door_vision/out/suites/` (`inspect-tests.json`, `inspect-playtest.json`, `stream-verification-latest.json`, `autopilot-2554-report.json`).
- Logs: `W/door_vision/logs/` (`gen.log`, `unity_*.log`, kit hashes before/after). Roslyn: `W/door_vision/roslyn/{mac,mac_all,ios,webgl}`.
- Scripts: `W/door_vision/scripts/run_unity.sh`, `suites.sh`, `make_diffs.sh`; video `W/door_vision/video/build.py`.
- 游戏视觉's own changes as one diff (for reading, not a landing package): `W/door_vision/apply_stage/visual_payload.diff`.

---

## 5. Findings for the next stage

1. **Fade pool.** Only lites in view fade, but after a jump up to about 20 in view can fade at once. Production should cap the pool at 4 and snap the rest. Nobody sees a snap that happens off-screen, and on-screen 4 concurrent fades are plenty.
2. **G4 measure.** Re-measure the mesh's local contrast with the post grain off (or the same grain seed for both renders). The look in the frames is not in doubt.
3. **Readiness.** The stand-in `LiteDoorReady` (both cells' chunks built) does not see furnishing or the first lamp pass. CONTRACT M-L4 asks the map for `CellReadyForLite`.
4. **Focus loss. CLOSED 2026-10-30** (交互设计): the resume returns into the peek through Game's A43 path, as the design wanted; no code was needed.
5. **Steel screws.** The harness's near side was face P, so the steel shots show the kit without its face-S screws. Production stills should take face S.
6. **The Relay at close range.** The Relay's rig reads well through the glass at 2-3 m. The sight rule held in every sample.

---

## 6. The landing package (NOT made now: the door kits are moving under door-break production)

**Bases.** Main's working tree of 2026-10-30 13:09 (HEAD 3ea3d7bb + the door-wood walnut and RelayV2 working-tree changes). Short md5 of each base:

| File | Base md5 | Owner | Note |
|---|---|---|---|
| `Tools/Blender/frontrooms_kit/assets/interact_door_lite_common.py` | (new) | 游戏视觉 | |
| `Tools/Blender/frontrooms_kit/assets/interact_door_da_common.py` | `a86b9817` | 游戏视觉 | `leaf_anchors(sign_yz)` |
| `Tools/Blender/frontrooms_kit/assets/interact_door_leaf_veneer_da.py` | `a2b130b3` | 游戏视觉 | the break generator builds `body` from this leaf: regenerate the break kit after |
| `Tools/Blender/frontrooms_kit/assets/interact_door_leaf_steel_da.py` | `277059fb` | 游戏视觉 | P3 steel cassettes rebase on it |
| `Assets/Resources/Props/Models/Kit_DoorLeaf_Veneer_DA.fbx/.json` | `9b0db5f2` (fbx) | 游戏视觉 | regenerate at landing, never copy the prototype's |
| `Assets/Resources/Props/Models/Kit_DoorLeaf_Steel_DA.fbx/.json`, `Kit_DoorLeaf_Steel_PaintedMetal_DA.fbx/.json` | `0b91bf50`, `675051bf` (fbx) | 游戏视觉 | the same |
| `Assets/Resources/Rendering/FrontRoomsGlass.shader` | `f3b9fea4` | 游戏视觉 (glass track) | both CBUFFERs; re-check any G14 / GD3c change since |
| `Assets/Resources/Surfaces/Glass_Wired.mat`, `Glass_WiredDark.mat` (+ .meta) | (new) | 游戏视觉 | |
| `Assets/Scripts/Rendering/Doors/FrontRoomsDoorLite.cs` (+ .meta) | (new) | 游戏视觉 | add the pool cap (section 5.1) |
| `Assets/Scripts/Office/FrontRoomsInteractableKit.Door.cs` | `c6aabf60` | 游戏视觉 | **the same base as door-break's restore call**: land one after the other, 3-way |
| `Assets/Scripts/Office/FrontRoomsKitLibrary.cs` | `36b85a09` | 游戏视觉 | `Info.liteRect` |
| `Assets/Scripts/Rendering/FrontRoomsDoubleDoorKit.cs` | `887c5982` | 游戏视觉 | the double-door-hardware workflow may move it |
| `Assets/Scripts/Rendering/Inspect/FrontRoomsInspect.cs`, `FrontRoomsInspectDef.cs`, `FrontRoomsInspectLook.cs`, `FrontRoomsInspectable.cs`, `FrontRoomsInspectCatalog.cs` | `a35d2185`, `1f24eb03`, `ed71f170`, `a2a4fbc4`, `3ba349fe` | 游戏视觉 | the catalog's strings wait for 叙事设计 (N-L1) |

**Order.**
1. The door-break production lands P1 / P2 first: they share `Door.cs` and the veneer leaf generator.
2. This package rebases on that (`rebase_apply.sh`) and regenerates Veneer_DA / Steel_DA / PaintedMetal_DA.
3. Door-break regenerates its `body` from the lited slab (section 6 of `30_design.md`).
4. The contract diffs land through their owners: 关卡设计 (`CONTRACT_map.diff`), 交互设计 (`CONTRACT_interaction_peek.diff`).

Without the interaction glue, the package is inert: the peek's EXAMINE target opens a paused close-up only if `PeekPausesWorld` is set. With `PeekPausesWorld` false, `BeginInspect` needs I-L2. So the visual package and I-L2 land together, or the visual package lands with `PeekPausesWorld = true` until I-L2 lands.

**Gates at landing.** The same as this round: Roslyn mac + iOS; `FrontRoomsInspectTests` + `FrontRoomsInspectPlaytest`; `FrontRoomsStreamVerification`; the station / exit autopilot; the door-break smoke on the lited leaf; a re-film of the peek.

**Red's open questions** (from `30_design.md` 9.1; the prototype filmed the recommended answers):
- **Q-A:** live world while peeking.
- **Q-B:** 0.8 s glimpse.
- **Q-C:** WebGL lites.
