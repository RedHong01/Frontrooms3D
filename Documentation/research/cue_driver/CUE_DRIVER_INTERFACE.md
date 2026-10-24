# Wallpaper cue: driver interface and event list (v0.2.5, 平面视觉 · 信息设计, 2026-10-24)

**v0.2.5 (2026-10-24).** Answers to 关卡设计's driver review (`~/FrontRoomsMapWork/integration/s17_way/BUILD_NOTE.md`, after the 10-22 21:48 landing):
- m3 = B: a retracting row settles early when §9.6 forbids its step. The cue never leads toward the Relay, even while it calms down.
- Q8: the chase rows in §3 now state the §9.6 step rules the driver already follows.
- Q7: the first flow click follows the third turn click by `WaveTurnStep` (said in §6).
- Q9: the K00 hold staying on for scheduled cells and the last fade is intended (conservative by a moment).

**v0.2.4 (2026-10-22 16:49).** Names the hook that holds the ambient print while the cue runs: the driver calls `FrontRoomsPrintDriver.SetHold("cue", anyCellOffZero)` and releases it on reset (in 关卡设计's WayCueDriver build, clone `~/FrontRoomsMapWork/proj3`, `FrontRoomsWayCueDriver.cs` :919 and :198; `SetHold` is in main's print driver). This closes the per-cell muting question: the hold is global, as §5 items 12 and 14 say. Nothing else changes.

**v0.2.3 (2026-10-22 15:07).** The motion is decided: Red picked **M4 + V1** (15:05, in the wallpaper line's chat, after the twelve in-game combination previews). The set is A + M4 + V1 as written here, so §1 and §6 stand and the flipbook C00–C16 is final. Only Status and §4 item 5 change.

**v0.2.2 (2026-10-22).** Where the arrows point is now decided. Red's C24 = A and its amendment A20 (systems.json) say: the arrows point at the station TV until the floor key is taken, then at the floor EXIT (C22). 关卡设计调研 wrote the map-side rules in `MAP_ROUTE_SPEC.md` (this folder). This version:
- points to it from §2, §4 and §5;
- drops the interim target ladder (§5 item 9);
- corrects the keyframe note (§5 item 12, VL349);
- marks the motion (M4, V1) as not yet confirmed (Status, §4 item 5).

The per-cell data (`_FR_WayCells`), the events in §3 and the pursuit rules (RELAY §9.6: when cells turn, step or retract) are unchanged.

**v0.2.** 系统设计 confirmed §3. The binding contract is now RELAY_PURSUIT_REDESIGN.md §9.6 "Driver contract", with the invariant in the §14 G7 row. 关卡设计 answered the map side. Their changes are folded in below and listed in §5.

**What it is.** The wallpaper cue A + M4 + V1, played per map cell:
- **A:** every arrow turns to point along the route and is fitted to its field.
- **M4:** the turn ratchets in three 30° clicks.
- **V1:** the rows then step along the route on two speed systems.

**Status (2026-10-22).** Prototype only. It runs in 平面视觉's clone `~/FrontRoomsVisualWork/proj_pgcue2`; nothing is in main. 游戏视觉 lands the shader part. The map side is 关卡设计's and is built after the Alpha.
- **The motion is decided: A + M4 + V1** (Red, 2026-10-22 15:05, in the wallpaper line's chat; earlier that day Red had asked to see the other options again). The flipbook C00–C16 at 2048² is final.
- **The ambient print driver is in the 2026-10-22 build; the cue is not yet.** `FrontRoomsPrintDriver` (HoldAndJump) already holds K00 on warn ≥ 1, Chase, BreakDoor and Reduce Motion (§5 items 12 and 14). 游戏视觉 merges it (`wallpaper_motion/20_level_design_phosphor.md`, Rev 5).
- Left before the cue is in the game: the map side (关卡设计: Wayfinding + WayCueDriver per `MAP_ROUTE_SPEC.md`) and 游戏视觉's CueInk landing. CueInk alone is inert: with no writer for `_FR_WayCells`, every cell is state 0, which is the live print exactly (§5 item 14).

**Sources:**
- Slices: `Tools/print/ink/art_from_graphic/cue_states/cue_flipbook.py`.
- Shader prototype: `proj_pgcue2/Assets/Resources/Rendering/FrontRoomsSurface.shader`, function `CueInk`.
- Timing prototype: `proj_pgcue2/Assets/Editor/CuePreview/FrontRoomsCuePreviewCapture.cs`, function `Schedule`.
- Route targets and field: `MAP_ROUTE_SPEC.md` (关卡设计调研, v1.0.1).

## 1. Data the shader reads (no new sampler)

| Global | Type | Meaning |
|---|---|---|
| `_FR_Cue` | Texture2DArray, RG, 2048² × 17 (desktop) | Encoded like `_FR_Print`. C00 = Q1b's K00, so a cell at state 0 looks exactly like the plain print. Sampled with `sampler_FR_Print`. |
| `_FR_WayCells` | Texture2D RGBA8, 64 × 64, point, read with `LOAD_TEXTURE2D` | One texel per map cell around the player:<br>• R = from-state<br>• G = to-state<br>• B = cross-fade from → to (0..1)<br>• A = route dir (0 +x, 1 +z, 2 −x, 3 −z) + 4 × message (0 none, 1 FLOW) |
| `_FR_WayOrigin` | float4 | xy = world x/z of a map-root corner on a 192 m multiple (never moves), z = cell size (3 m), w = enable. **Toroidal addressing (关卡设计):** texel = (cellX & 63, cellY & 63). 64 × 3 m = 192 m = ModuleUnits.WorldPeriod. The driver clears a chunk's texels when it drops or shifts (`MapWorld.PassageRevision`). |

**States** (per face, the shader picks the direction set):

| State | Meaning |
|---|---|
| 0 | baseline |
| 1, 2, 3 | turn 30 / 60 / 90° (3 = fitted, and also flow click 0) |
| 4–8 | flow clicks 1–5. One loop is 6 clicks = 750 mm = one roll, so click 6 is state 3 again. |

**Direction set.** FaceSign = sign(dot(routeDir, t)) with t = cross(up, n), the +u of PlanarFrame.
- +u faces use slices 1–8; −u faces use slices 9–16.
- Faces across the route (|dot| < 0.5) keep the plain print.

**Cell lookup.** The cell is taken at positionWS + n × 0.5 m, so a wall on a cell edge belongs to the room it faces. Then cell = floor((p.xz − origin.xy) / 3) & 63. (The clone prototype still uses a window around the player; switch to toroidal before landing.)

**Cost per wallpaper fragment:** one LOAD, plus one array sample (two while a cross-fade runs). It only applies when `_FR_PRINT` is on and the cell carries a message.

## 2. Who writes what

| Part | Owner | Notes |
|---|---|---|
| Route field: dir per cell (normal FLOW route, pressure route in a chase), room groups ("your room": BFS over Open edges, depth ≤ 2, ≤ 9 cells), message per cell | 关卡设计 (FrontRoomsWayfinding, after the Alpha); the rules are 关卡设计调研's | Targets and field rules: `MAP_ROUTE_SPEC.md`. C24 = A + A20: the station TV's cell until the floor key is taken, then the EXIT (§1 there). The same field feeds the TV beacon (tutorial M5 `BeaconPath`), so the arrows and the sound never disagree (§5 there). Before C22's EXIT exists, every cell is message 0 (§7 there). |
| Cue driver `FrontRoomsWayCueDriver`: per-cell state machine → `_FR_WayCells`, no allocation | 关卡设计 (its own component, started from `MapRunStarted`). 平面视觉 owns the timing table `FrontRoomsCueTiming`; the driver only reads it. | It advances on game time in its own LateUpdate, after MapWorld and the hunter have ticked that frame. Captures step it with `TickForTools(dt)`, the same pattern as `MapWorld.TickFixturesForTools`, with identical results. It reads no lamp levels, so the full and near-only fixture paths behave the same. It writes a NativeArray<Color32> and calls Apply(false) only when dirty. While any cell is off state 0 it holds the ambient print at K00 with `FrontRoomsPrintDriver.SetHold("cue", anyCellOffZero)`, and it releases the hold on reset. |
| `CueInk` in FrontRoomsSurface, `_FR_Cue` builder (slices are encoded and need FrontRoomsPrintMips like Q1b) | 游戏视觉 | The prototype `CueInk` is the spec |
| Slices (`cue_flipbook.py`) | 平面视觉 | Re-run if the print changes; C00 must stay bit-identical to K00 |

## 3. Events the driver listens to

| Event (source) | Exists? | Driver reaction |
|---|---|---|
| `WarnStageChanged(int)` (FrontRoomsMapHunter) | yes | 1 → **Stage 1**: room-set cells turn, clicks at +0.20 / +0.50 / +0.80 s. Turn only, FLOW route only. Never in the Relay's room or within 2 cells of it. 2 → nothing new. |
| `StateChanged(Chase)` after the 0.7 s hold | pending | **Wave** leaves the room set at 8 m/s. A cell turns when the wave arrives (0.12 s clicks), then, 0.12 s after its third turn click, steps at 4 clicks/s. Steps follow §9.6: a row steps only along a pressure route through a qualifying door (with none, the wave is turn only), and a stepping row settles in one click when its way on comes to lead toward the moving Relay (LD R6). |
| `StateChanged(Search)` (Chase → Search) | yes | **Retract**: outside-in over 2 s. Each cell finishes its 6-click loop, then turns back (0.15 s clicks). A row settles in one click instead when its next step would bring the Relay more than one cell nearer (§9.6; m3 = B). Cells the wave hadn't reached yet stay put. |
| `GaveUp` / `Withdrew` / director `Restore` | pending | Do **not** subscribe (系统设计). Stage 0 comes from `WarnStageChanged(0)`. |
| `Caught` | yes | Freeze: hold the current states, no new clicks. |
| `WarnStageChanged(0)` | yes | Room set turns back, 0.15 s clicks. |
| `LampDipped` / `FixtureChanged` (FrontRoomsMapWorld) | yes | Optional visual beat only: Warn bursts at Stage 1, Dips as the wave passes (as in the preview). The cue's timing never waits on lamps. |
| Settings: Reduce Motion | yes | Turn only: no flow clicks. The wave becomes a one-time turn as it passes. |
| Settings: Reduce Flashing | yes | Unchanged (clicks are 70 ms cross-fades, not flashes) |

**Fixed tempo.** Never tie clicks to the Relay's speed or distance (系统设计).

## 4. Open questions

All five items are settled; their numbers are kept because other docs cite them.

1. **What is an exit?** **Answered 2026-10-22.** Each floor has an EXIT that leads one floor deeper (C22). The arrows point at the station TV until the floor key is taken, then at the EXIT (C24 = A + A20). Rules: `MAP_ROUTE_SPEC.md` §1; see §5 item 9.
2. **Lamp gate.** 关卡设计 proposed that a cell may hint only below lamp level 0.55, with flicker overrides. That fits the old glow-ink hint. For the pattern-native cue, 平面视觉 proposes no darkness gate: the turn is meant to be seen in lit rooms. It is driven by the warn and chase events above, and a dead lamp only makes it harder to see. **Settled:** no darkness gate (§5 item 10).
3. **WebGL tier.** The array is R8G8 at 1024²; maybe turn only (no flow) to save fill rate. This is its own track and must never change desktop. **Settled:** turn only, gated by platform (§5 item 11).
4. **Ambient keyframes.** Once the driver lands, the HoldAndJump keyframes must not use cue vocabulary (straight match, 90/180° turns, flatten, single-row change). **Settled and rebuilt 2026-10-22:** K02 → plate shift, K03 → missing slate plate, K07 → block swap (§5 item 12, VL349).
5. **Motion.** **Decided 2026-10-22 15:05: M4 + V1** (Red, in the wallpaper line's chat; C31 in systems.json). The set is A + M4 + V1 as specified here.

## 5. v0.2 decisions (系统设计 §9.6, 关卡设计)

1. **Chase key.** `StateChanged(Chase)` exists. In v2 it fires at the end of the 0.7 s hold, so key the wave on it and never subscribe to `TargetAcquired`. Today Chase starts on first sight, so a preview from the current build shows the wave without the warning ladder. 平面视觉's preview uses a simulated order for this reason.
2. **BreakDoor inside a chase is still the chase.** `StateChanged(BreakDoor)` changes nothing. The `StateChanged(Chase)` after a break must not restart the wave (idempotent).
3. **Retract on any exit from {Chase, BreakDoor}**, not only Chase → Search. Also Chase → Listen when a cell is re-arrived unbuilt, and test placements.
4. **Retract lands on the stage.** If WarnStage ≥ 1, room-set cells end turned on the FLOW route, not stepping, and only cells outside the set go back to 0. If the pressure dir ≠ the FLOW dir, they turn back first, then make the stage-1 turn.
5. **Precedence: Caught > wave > stage.** A `WarnStageChanged(0)` during a wave is held until the retract. Stage 2↔1 changes nothing. In v2, stage 3 latches.
6. **Room set = `RoomSet()`**, one shared function owned by 关卡设计 (§9.2): the carved or module room the player is in; otherwise a BFS over Open edges, depth ≤ 2, ≤ 9 cells, start rooms excluded. The stage-1 lamp burst uses the same function. When the room set changes at stage ≥ 1, the old set turns back and the new set turns.
7. **Relay exclusion** (2 cells or its room) is tested only when a cell starts to turn. A turned cell holds if the Relay comes near later; turning back as it nears would be a readout. The wave has no exclusion.
8. **Reset** to all-0 on run restart, map rebuild and `ResetWarningMetrics`.
9. **FLOW target: replaced on 2026-10-22 by `MAP_ROUTE_SPEC.md`** (C24 = A + A20).
    - The calm route points at the station TV's cell until `StationKeyTaken`, then at cell N on the EXIT door's edge (§1 there).
    - It is taken from the map alone and never reads the Relay (§2 there, test R3). It never points at glass, broken windows included.
    - Before C22's EXIT exists, every cell is message 0, and the lamps and groan carry stage 1 (§7 there).
    - The old interim target (the nearest unvisited threshold, a zone-border door or window not yet crossed) and its fallback ladder are dropped. They will not be built.
    - **Pressure route** (chase only): unchanged from RELAY §9.6. It points at shuttable doors that lead away from the Relay (LD R6) and may read the Relay. Its fallback is now the calm field, not the old ladder (§6 there).
    - Forged FLOW (tier 4) is in question (C29, decided in 叙事设计's chat). Like MAP_ROUTE_SPEC §7, this doc defines only the true route.
10. **No darkness gate:** settled by 系统设计, 关卡设计 and 平面视觉. Under the v2 burst, lamps stay at 0.68–0.84, so a 0.55 gate would never open. Dead/Off cells just don't show the cue, and the route never depends on it being seen.
11. **WebGL:** turn only (the Reduce Motion path), gated by platform. Desktop is untouched.
12. **Ambient keyframes:** cue vocabulary is reserved (G7: at stage 0 with no chase, every cell is 0 or retracting).
    - The print chat changed K02 → plate shift, K03 → missing slate plate and K07 → block swap (`K02_plate_shift`, `K07_block_swap` in `Tools/print/print_tool.py`). The new K07 is the old K02 "half-drop" byte for byte, because a half-tile roll is a unit swap. (v0.2.1 said "K07 stays", which was wrong.) Rebuilt and checked on 2026-10-22: VL349, `wallpaper_motion/32_pattern_native_hints.md` §9.6. The game shows them once 游戏视觉 rebuilds the print array.
    - While WarnStage ≥ 1 or a chase is on, the HoldAndJump driver blends back to K00 and holds there until every cue cell is back at 0, because the cue slices are authored on K00. The cue's own hold is `SetHold("cue", anyCellOffZero)` (§2); the print driver then waits for a 3–6.5 s quiet slot before it resumes.
13. **Writers (关卡设计):**
    - `FrontRoomsWayfinding` (route field, room set, message) recomputes on a player-cell or PassageRevision change, preallocated.
    - `FrontRoomsWayCueDriver` (state machine) writes a NativeArray<Color32> and calls Apply(false) only when dirty.
    - Both are started from `MapRunStarted`, with no edits to FrontRooms3DGame.cs.
    - The timing table is one static table that 平面视觉 owns and the driver reads.
14. **State 0 = the live print (shader rule, 平面视觉, 2026-10-07).** In `CueInk`, state 0 never samples C00. It returns the print exactly as the static `_PrintTex` and the ambient flipbook give it.
    - A cue starting or ending cross-fades from or to whatever ambient frame is live, so there is no pop and no per-cell hold.
    - Beat-0 lamp cues (stage-0 sags, dying lamps; narrative's addition, LD R1, §9.3 beat 0) can start at any time.
    - The turn and step slices stay authored on K00. A fault frame vanishes on the first click.
    - The global rule stays: ambient holds at K00 during WarnStage ≥ 1 or a chase, and resumes no earlier than the burst lamps' first quiet slot after stage 0.
    - The ambient driver is one global clock, so per-cell ambient exemptions are not possible and are not needed. Closed 2026-10-22: there is no per-cell muting; the hold is the global `SetHold("cue")`.

## 6. `FrontRoomsCueTiming` (the timing table 平面视觉 owns; the driver only reads it)

Paste this as `Assets/Scripts/FrontRoomsMap/FrontRoomsCueTiming.cs` when the driver lands. Keep it ASCII only.

```csharp
// Wallpaper cue timing (owner: graphic-visual chat; read-only for the driver).
// States: 0 = live print, 1..3 = turn 30/60/90 deg (3 = flow click 0), 4..8 = flow clicks 1..5.
public static class FrontRoomsCueTiming
{
    public const int TurnStates = 3;          // M4 ratchet: three 30-degree clicks
    public const int FlowLoop = 6;            // V1: 6 clicks = 750 mm = one roll, then state 3 again
    public const float CrossFade = .07f;      // seconds per click cross-fade (snap look)
    // Stage 1 (room set, FLOW route only): click times after WarnStageChanged(1)
    public static readonly float[] StageTurnAt = { .20f, .50f, .80f };
    // Chase wave (StateChanged(Chase) after the 0.7 s hold)
    public const float WaveSpeed = 8f;        // m/s, outward from the room set
    public const float WaveTurnStep = .12f;   // s between the three turn clicks as the wave arrives, and from the third turn click to the first flow click
    public const float FlowClick = .25f;      // s per flow click (4 clicks/s)
    // Retract (any exit from {Chase, BreakDoor}); turn-back (stage 0, or after a retract)
    public const float RetractWindow = 2f;    // s, outside-in
    public const float TurnBackStep = .15f;   // s between the three turn-back clicks
    // Reduce Motion / WebGL: turn only, no flow clicks (same states, same timing)
}
```
