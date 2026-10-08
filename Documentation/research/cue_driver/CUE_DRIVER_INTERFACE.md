# Wallpaper cue: driver interface and event list (v0.2.1, 平面视觉, 2026-10-07)

**v0.2.** 系统设计 confirmed §3. The binding contract is now RELAY_PURSUIT_REDESIGN.md §9.6 "Driver contract", with the invariant in the §14 G7 row. 关卡设计 answered the map side. Their changes are folded in below and listed in §5.

**What it is.** The wallpaper cue A + M4 + V1, played per map cell:
- **A:** every arrow turns to point along the route and is fitted to its field.
- **M4:** the turn ratchets in three 30° clicks.
- **V1:** the rows then step along the route on two speed systems.

**Status.** Prototype only. It runs in 平面视觉's clone `~/FrontRoomsVisualWork/proj_pgcue`; nothing is in main. 游戏视觉 lands the shader part, and the map side is 关卡设计's. All of it waits until Red, 系统设计, 关卡设计, 游戏视觉 and 平面视觉 agree.

**Sources:**
- Slices: `Tools/print/ink/art_from_graphic/cue_states/cue_flipbook.py`.
- Shader prototype: `proj_pgcue/Assets/Resources/Rendering/FrontRoomsSurface.shader`, function `CueInk`.
- Timing prototype: `proj_pgcue/Assets/Editor/CuePreview/FrontRoomsCuePreviewCapture.cs`, function `Schedule`.

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
| Route field: dir per cell (normal FLOW route, pressure route in a chase), room groups ("your room": BFS over Open edges, depth ≤ 2, ≤ 9 cells), message per cell | 关卡设计 (FrontRoomsWayfinding, not built yet) | "Exit" is still undefined (Red / narrative) |
| Cue driver `FrontRoomsWayCueDriver`: per-cell state machine → `_FR_WayCells`, no allocation | 关卡设计 (its own component, started from `MapRunStarted`). 平面视觉 owns the timing table `FrontRoomsCueTiming`; the driver only reads it. | It advances on game time in its own LateUpdate, after MapWorld and the hunter have ticked that frame. Captures step it with `TickForTools(dt)`, the same pattern as `MapWorld.TickFixturesForTools`, with identical results. It reads no lamp levels, so the full and near-only fixture paths behave the same. It writes a NativeArray<Color32> and calls Apply(false) only when dirty. |
| `CueInk` in FrontRoomsSurface, `_FR_Cue` builder (slices are encoded and need FrontRoomsPrintMips like Q1b) | 游戏视觉 | The prototype `CueInk` is the spec |
| Slices (`cue_flipbook.py`) | 平面视觉 | Re-run if the print changes; C00 must stay bit-identical to K00 |

## 3. Events the driver listens to

| Event (source) | Exists? | Driver reaction |
|---|---|---|
| `WarnStageChanged(int)` (FrontRoomsMapHunter) | yes | 1 → **Stage 1**: room-set cells turn, clicks at +0.20 / +0.50 / +0.80 s. Turn only, FLOW route only. Never in the Relay's room or within 2 cells of it. 2 → nothing new. |
| `StateChanged(Chase)` after the 0.7 s hold | pending | **Wave** leaves the room set at 8 m/s. A cell turns when the wave arrives (0.12 s clicks), then steps at 4 clicks/s. It may switch to the pressure route (LD R6). |
| `StateChanged(Search)` (Chase → Search) | yes | **Retract**: outside-in over 2 s. Each cell finishes its 6-click loop, then turns back (0.15 s clicks). Cells the wave hadn't reached yet stay put. |
| `GaveUp` / `Withdrew` / director `Restore` | pending | Do **not** subscribe (系统设计). Stage 0 comes from `WarnStageChanged(0)`. |
| `Caught` | yes | Freeze: hold the current states, no new clicks. |
| `WarnStageChanged(0)` | yes | Room set turns back, 0.15 s clicks. |
| `LampDipped` / `FixtureChanged` (FrontRoomsMapWorld) | yes | Optional visual beat only: Warn bursts at Stage 1, Dips as the wave passes (as in the preview). The cue's timing never waits on lamps. |
| Settings: Reduce Motion | yes | Turn only: no flow clicks. The wave becomes a one-time turn as it passes. |
| Settings: Reduce Flashing | yes | Unchanged (clicks are 70 ms cross-fades, not flashes) |

**Fixed tempo.** Never tie clicks to the Relay's speed or distance (系统设计).

## 4. Open questions

1. **What is an exit?** Red / narrative decide. Until then the route field has no target.
2. **Lamp gate.** 关卡设计 proposed that a cell may hint only below lamp level 0.55, with flicker overrides. That fits the old glow-ink hint. For the pattern-native cue, 平面视觉 proposes no darkness gate: the turn is meant to be seen in lit rooms. It is driven by the warn and chase events above, and a dead lamp only makes it harder to see. Decide together.
3. **WebGL tier.** The array is R8G8 at 1024²; maybe turn only (no flow) to save fill rate. This is its own track and must never change desktop.
4. **Ambient keyframes.** Once the driver lands, the HoldAndJump keyframes must not use cue vocabulary (straight match, 90/180° turns, flatten, single-row change). The swaps proposed to the print chat: K02 → plate shift, K07 → block swap.

## 5. v0.2 decisions (系统设计 §9.6, 关卡设计)

1. **Chase key.** `StateChanged(Chase)` exists. In v2 it fires at the end of the 0.7 s hold, so key the wave on it and never subscribe to `TargetAcquired`. Today Chase starts on first sight, so a preview from the current build shows the wave without the warning ladder. 平面视觉's preview uses a simulated order for this reason.
2. **BreakDoor inside a chase is still the chase.** `StateChanged(BreakDoor)` changes nothing. The `StateChanged(Chase)` after a break must not restart the wave (idempotent).
3. **Retract on any exit from {Chase, BreakDoor}**, not only Chase → Search. Also Chase → Listen when a cell is re-arrived unbuilt, and test placements.
4. **Retract lands on the stage.** If WarnStage ≥ 1, room-set cells end turned on the FLOW route, not stepping, and only cells outside the set go back to 0. If the pressure dir ≠ the FLOW dir, they turn back first, then make the stage-1 turn.
5. **Precedence: Caught > wave > stage.** A `WarnStageChanged(0)` during a wave is held until the retract. Stage 2↔1 changes nothing. In v2, stage 3 latches.
6. **Room set = `RoomSet()`**, one shared function owned by 关卡设计 (§9.2): the carved or module room the player is in; otherwise a BFS over Open edges, depth ≤ 2, ≤ 9 cells, start rooms excluded. The stage-1 lamp burst uses the same function. When the room set changes at stage ≥ 1, the old set turns back and the new set turns.
7. **Relay exclusion** (2 cells or its room) is tested only when a cell starts to turn. A turned cell holds if the Relay comes near later; turning back as it nears would be a readout. The wave has no exclusion.
8. **Reset** to all-0 on run restart, map rebuild and `ResetWarningMetrics`.
9. **FLOW target (interim):** the nearest unvisited threshold, a zone-border door or window not yet crossed, taken from the map alone and never from the Relay. With no target the cell stays at 0. **Pressure route** (chase only): the same targets, with cells the Relay reaches no later than the player costing extra; shuttable doors and loops preferred. Red decides what an exit is.
10. **No darkness gate:** settled by 系统设计, 关卡设计 and 平面视觉. Under the v2 burst, lamps stay at 0.68–0.84, so a 0.55 gate would never open. Dead/Off cells just don't show the cue, and the route never depends on it being seen.
11. **WebGL:** turn only (the Reduce Motion path), gated by platform. Desktop is untouched.
12. **Ambient keyframes:** cue vocabulary is reserved (G7: at stage 0 with no chase, every cell is 0 or retracting).
    - The print chat changes K02 → plate shift and K03 → missing slate plate. K07 stays: "half-drop" was always a block swap.
    - While WarnStage ≥ 1 or a chase is on, the HoldAndJump driver blends back to K00 and holds there until every cue cell is back at 0, because the cue slices are authored on K00.
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
    - The ambient driver is one global clock, so per-cell ambient exemptions are not possible and are not needed.

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
    public const float WaveTurnStep = .12f;   // s between the three turn clicks as the wave arrives
    public const float FlowClick = .25f;      // s per flow click (4 clicks/s)
    // Retract (any exit from {Chase, BreakDoor}); turn-back (stage 0, or after a retract)
    public const float RetractWindow = 2f;    // s, outside-in
    public const float TurnBackStep = .15f;   // s between the three turn-back clicks
    // Reduce Motion / WebGL: turn only, no flow clicks (same states, same timing)
}
```
