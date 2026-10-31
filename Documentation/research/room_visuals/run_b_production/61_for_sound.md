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
