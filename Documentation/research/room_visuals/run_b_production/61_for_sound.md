# 61 · Run! B rooms: what the sound line can hook (ask from 游戏视觉)

From 游戏视觉 (account A, session local_5656cffd), 2026-10-31, Run! B land stage. Written, not sent: 声音设计 picks it up from here. The visual side never touches audio files or FMOD (AUDIO_CONTRACT). Background: `10_run_directions.md` §8 (CR-S1 to CR-S4), C07 in `system_design/systems.json` L7 (the trip sounds only the devices' own local sounds, no building bell).

## State

- Package `W/apply/run_b.sh` (195 paths), not in main yet. It lands **inert**: everything below exists only while the map's `RunRoomsOn` is true (the level profile's `runRooms`, default false, or the editor tools' `-autopilotRunRooms 1|2`).
- Nothing here is needed for the package to land. These are the hooks for when you add the Run! room sounds.

## CR-S3 · the trip events (C07: the devices' own sounds)

Every built Run! room has one `FrontRoomsRunRig`. Find them through the binder:

| Hook | Signature | When |
|---|---|---|
| `FrontRoomsRunRigBinder.RigBuilt` (static) | `Action<FrontRoomsRunRoom, FrontRoomsRunRig>` | a room's rig is built (both chunks standing; again after each rebuild) |
| `FrontRoomsRunRigBinder.RigDropped` (static) | same | just before the rig is destroyed (chunk dropped, floor changed) |
| `rig.StateChanged` | `Action<RunRigState, Vector3>` (Armed, Tripping, Tripped, Resetting; the room's centre) | every state change |
| `rig.MainsDropped` | `Action<Vector3>` (room centre) | t 0 of the trip: the contactor drops out, every wraparound goes dark in 0.06 s |
| `rig.TransferRelay` | `Action<int, Vector3>` (head index, head position) | once per emergency head as its transfer relay pulls in (8 heads, staggered) |
| `rig.ResetStarted` | `Action<Vector3>` (room centre) | an animated re-strike begins (the zone reset, viewer within 30 m) |

- A room rebuilt while its alarm still stands comes back tripped with **no** events (`TripImmediate`), so nothing replays.
- The map's own `RoomTriggered(ModuleTrigger.Alarm, room, tag)` / `RoomReset(room)` / `RunRoomWarned(room, stage)` on `FrontRoomsMapWorld` are there too (关卡设计's). The rig's events are the device-level ones, with positions.
- The ALARM ZONE tell's LED (`FrontRoomsAlarmTellLed`) blinks silently. It makes no sound.

## CR-S4 · the rig's lights and the fixture hum

`FrontRoomsSoundDirector` takes hum voices from lights named `Light` or `fluorescent light*`. The rig's lights are named:

- `head light` / `head light (goal spot)`: the emergency heads, lit only after the trip;
- `run b wrap light <k>`: the 9 fluorescent wraparounds, lit while armed;
- `run b goal window light (C42)`: the unshadowed light behind the goal wall.

So **none hums today**. If you want the armed wraparounds to hum (and stop dead at `MainsDropped`), say so: we either rename them `fluorescent light …`, or you add a rule for `run b wrap light`. Your call.

## CR-S1 / CR-S2 · floor and reverb

- Floor: B's corridor is glossy VCT over the whole room (z 0 to 27). `SoundIds.Surface.VCT` already exists. A Run! cell is `map.RunRooms != null && map.RunRooms.IsRunCell(cell)`.
- Reverb: a hard, bright corridor, 1 × 9 cells (3 m × 27 m), painted block walls, a 2.9 m painted soffit. Yours to tune.

## What we need back

Only a line in your handoff on which of the above you take, and whether the wraparounds should hum (CR-S4). Nothing blocks the visual package.

## Update 2026-10-31 12:3x · what `run_b_fix.sh` changes for you (READY, not applied yet)

The core landed 11:38, and your CR-S3 hooks are in main (`FrontRoomsSoundDirector`, 11:45). The fix package keeps every hook with the same shape: `RigBuilt`, `RigDropped`, `StateChanged`, `MainsDropped`, `TransferRelay` and `ResetStarted`. Its dry run refuses if one of them changes. Two timings move:

- **The re-strike (F3, the old 4 flashes/s strobe).** `ResetStarted` still fires at t 0. After it, each wraparound flicks **once**, to 0.55 for 0.08 s. The flicks ripple from the entrance, 0.025 s a cell, so the corridor reads as one short bump. Then the corridor is dark. From 0.45 s the tubes come up one cell at a time, 0.15 s a cell from the entrance to the goal. The reset ends at **2.4 s** (it was 2.63 s). With REDUCE FLASHING on, there is no flick: 0.5 s rises only, as before. If a re-strike cue (ballast tick, tube ping) follows the light, key it to these times.
- **Handhelds (step 9).** Phones and tablets keep 5 of the 8 head lights and 5 of the 9 wrap lights. `TransferRelay` still fires **8 times** (once per head, same positions), and the light names are unchanged. So a per-head relay clack sounds the same on every platform.

Nothing here blocks the package. Nothing makes a sound in our clones (rule 24).

## Update 2026-11-04 · the Run! rethink: the receding end and the Run door (READY, not applied yet)

From 游戏视觉 (workflow `visual-run-rethink`, account A, parent session local_be0f3747), for `research/level_space/71_run_room.md` §9: your cards 1-3 and our cards 2 and 4. Red's answer to 71 §8, 2026-11-03 23:18: "全部按推荐" (71 §0.1). Packages `W/apply/run_recede_head.sh`, `run_t2_body.sh` and `run_door_tell.sh`; the visual parent lands them in that order. All of it is inert until Red has walked SP7 in scene 4 (rule 5b). We play nothing (rule 24). Full contract: `W/apply/run_rethink_contract/README.md`, section "Final"; build log `60_production_66.md` H, T, D, P.

**The receding end has one event:** `FrontRoomsRunRecede.Cue`, `Action<RunRecedeCue, int, Vector3>` (the cue, an index, the world point). `RunRecedeCue` is append-only. T1's ten come with every step on every tier. T2's six come with the blocks (desktop, iPad) and the strips (phones); WebGL runs T1 alone.

| Cue | When | Index | Point |
|---|---|---|---|
| `MagnetRelease` | the tell: the first hold-open magnet lets go; the second at the tell's second clack, or at the travel's start | 0, 1 | that magnet |
| `Unlatch` | the tell's jolt | the step | the end |
| `Grit` | the tell (grit from the soffit seam) and the seat | 0 tell, 1 seat | the seam, the end |
| `TrolleyRoll` | the travel starts | the step | the end at soffit height |
| `SweepDrag` | the travel starts | the step | the end |
| `Whump` | the travel starts | the step | the end |
| `Seat` / `FinalSeat` | the seat; `FinalSeat` on the last step, or where the keep-away glide ends | the step | the end |
| `StripStrike` | the seat: once for each strip the step uncovered | the cell | the strip |
| `CellUncovered` | the tell: each cell the step uncovers (never in a restore) | the cell | the end |
| `MortarCrack` | T2: the tell of every step (the joints let go) | 0 | the wall |
| `SubframeSlide` | T2: the travel starts, the door's steel rolls back alone | 0 | the sill |
| `BlockLift` | T2: each course lifts out of its beds, top course first | the course (0 at the floor) | its first piece |
| `StreamClatter` | T2: the two streams of blocks start along the walls | 0 left, 1 right | each stream |
| `CourseLand` | T2: each course lands, bottom first, the top course last into the seat | the course | the course |
| `MortarSift` | T2: grit sifts from that course's joints (drawn on desktop only; the cue on every tier) | the course | the course |

- A one-cell T2 step raises 43 T2 cues besides T1's, in a fixed order from the clips' contact lists: the crack, the lifts from the top course down (the first during the tell), the slide as the travel starts, the streams, the landings from the bottom course up, the top course's last.
- A room rebuilt after its recede began comes back seated, quietly: no cue replays (as `TripImmediate` above).
- **Finding it:** today nothing in the game builds a receding room. When the binder does (with 关卡设计's card 4 record, after SP7), the hook that hands you each room's recede comes with those binder lines, as `RigBuilt` hands you the rig.
- **The Run door** (our card 4) adds no event: it is a normal wooden door (rule 10), and its sounds are the doors'. The trip about 2.5 m inside is 关卡设计's device point (their card 5); its sound is yours (71 §9, 声音设计 3).
- **Unchanged:** every hook above (`RigBuilt`, `RigDropped`, `StateChanged`, `MainsDropped`, `TransferRelay`, `ResetStarted`) and the re-strike's timing, 0.15 s a cell from the entrance, now over the room's own length (7 to 14 cells).
- **Waiting with our card 3 (WAIT-RED):** a click per cell as the red trip front passes, and B's pulse.
