# Sound coverage audit

Refreshed 2026-10-22 after the C10 sound pass landed. The first version was 2026-10-02.

This file records what each game moment plays now. All sound goes through FMOD: Unity's built-in audio is off (`m_DisableAudio: 1`), so nothing else can be heard. Every recording in the banks is CC0 or was made for FrontRooms (`AUDIO_LICENSES.md`). How the Relay is heard is binding, and is set out in `AUDIO_CONTRACT.md` under "How the Relay is heard".

## Status key

| Status | Meaning |
|---|---|
| **Recorded** | A real recording. Some are reshaped: pitched or layered. |
| **Synth** | An FMOD placeholder, to be replaced. |
| **Partial** | Part of the moment has sound and part is missing. |
| **Silent** | Nothing plays. |
| **Planned** | The work is decided and scheduled, but not built in main. |

## Player

| Moment | Trigger | Sound now | Status |
|---|---|---|---|
| Walk, run and stop on carpet or office tile | distance travelled (`FrontRoomsPlayerFootsteps`) | carpet or tile body, plus damp layers set by zone and shoe wetness | Recorded. The moist, peel and soaked-squelch layers come from CC0 583287 (the squelch replaced the CC-BY 187617 on 10-22). |
| Touch slow walk | `PlayerGait` Slow | the same steps, −15 dB | Recorded |
| Running cloth | run strides | jacket swish | Recorded |
| Metal threshold strip | `Surface.Metal` | in the banks, but the map has no metal surface yet | Unused |
| Climb through a broken window | `PlayerClimbed` | cloth swish only: no hands on the sill, no glass underfoot | Partial |
| Out of stamina | stamina statics | breath | Synth |
| Heartbeat | `Arousal`: winded gives the slow beat, a chase the fast one, fading over 3 s after it (C10) | heartbeat loops. It never follows the Relay's distance. | Synth |
| Caught | `Caught` | every bus cut, then tinnitus. No grab or impact. | Partial |

## Keys and doors

| Moment | Trigger | Sound now | Status |
|---|---|---|---|
| Pick up a key | `KeyTaken` | key ring | Recorded |
| Try a locked door | `DoorLocked` | handle rattling on the bolt. Doors lock only where a level profile sets `doorsNeedKeys` (default off). | Recorded |
| Unlock a door with a key | `DoorUnlocked`, `FrontRoomsDoorShot.Beat` | the open skips the handle beat. There is no key in the lock and no bolt turn yet. | **Silent**. CC0 lock and key takes (347153, 451139, 468937) were approved on 10-22 and are waiting on Red's Freesound login. |
| Open or close a map door | hinge rotation (`FrontRoomsDoorSound`) | handle, unlatch, stops and latch strike are recorded. The closer hiss, creak loop and air whump are synth or cliché layers. | Recorded + Synth. Cleanup planned on top of A12 (double-acting doors): no closer or panic-bar layers; handle on `DoorHandleTurned`, close on `DoorLatched`. |
| Title-stream double doors | hinge rotation (Stream mode) | release and seat per leaf, plus a stand-in lock. The swing is muted (the old 843829 swing sounded like "electrical current"). | **Partial**. The replacement swing, Freesound 683823 (CC0), was approved on 10-22 and is waiting on the download. |
| Try a broken door | `DoorBrokenTried` (in main) | nothing. RELAY §10 allows a small hanging-leaf Foley here. | Silent (optional) |
| Relay squeezes through a door frame | rig `Squeeze` | nothing | Silent |

## Windows

| Moment | Trigger | Sound now | Status |
|---|---|---|---|
| Hold E on a window | `GlassHold` + `GlassCracked` | stress tone, and a crack per stage | Synth (stress) + Recorded (cracks) |
| Let go early | `GlassHoldReleased` | the stress tone stops | ok |
| Window breaks | `GlassBroken` | shatter | Recorded |

## The Relay (C10, 2026-10-22: no cue says "near" through walls)

| Moment | Trigger | Sound now | Status |
|---|---|---|---|
| Its steps | rig `Step` | Heard only at warning stage 2 or in sight, from its real position. The level comes from the path (`PathGain`, about +10 dB over the room tone at the stage-2 entry behind a shut door); shut openings and distance muffle it. Captions `[MUFFLED HEAVY STEPS]` / `[HEAVY STEPS]`. | Recorded (reshaped) |
| Its low hum | stage 2 or in sight (C30) | `Relay/Hum` at the steps' gain and muffle, faint, about room-tone level at the entry. Caption `[LOW HUM]`. | Synth, until v2's coil hum |
| Hunt / Search / Chase / Lost | `StateChanged` | nothing: the 2D stingers and the release clicks were removed (C10) | removed on purpose |
| Hits and breaks a door | `DoorBlow`, `DoorBroken` | slams pitched down, wood splitting, the break. Blow strength comes from `BlowIndex`/`BlowCount`. | Recorded (reshaped) |
| The giant brushing the ceiling | rig `CeilingBrush` (the prototype rig never fires it) | nothing | Silent |

## The world

| Moment | Trigger | Sound now | Status |
|---|---|---|---|
| Room hum | always on; Tension follows the warning stage, 0.05 / 0.2 / 0.4, 1.0 in a chase (C30) | tube hum, with a detuned layer whose beating rises with Tension | Recorded |
| Warning burst (W1) | `FrontRooms3DGame.WarnBurst` / `WarnBursting` | ballast brown-out on the room hum: −5 dB and −70 cents while the dips play. Caption `[LIGHTS DIP]`, or `[HUM DROPS]` when the burst has no lamps. | Recorded (hum) |
| Air per zone | Zone global | hallway air; a large-space bed takes over in Tall zones. Both from CC0 406508; the large bed replaced the CC-BY 341512 on 10-22. | Recorded |
| Nearest lamps | lamp discovery | fixture hum, 4 voices started 0.53 s apart | Recorded |
| Lamp flicker | light intensity | starter strike and ballast ticks | Recorded |
| A tube failing | none | the pop is in the banks, but nothing triggers it | Unused |

## Game flow

| Moment | Trigger | Sound now | Status |
|---|---|---|---|
| Pause | `Paused(bool)` | SFX, AMB, Music and Subjective pause and resume where they were | done |
| EXAMINE close-up | `Inspecting(bool)` | Foley, Mechanism, Music and Subjective pause; Relay −6 dB, AMB −10 dB; Tension held. A focus loss inside a close-up resumes into the close-up mix. | done (10-22) |
| Title screen | none | room hum only. `Music/Title` is empty, because no motif has been chosen. | Silent (music). The tutorial's title TV (T6, S1) is planned. |
| Station TV beacon (tutorial) | `BeaconPath` (planned) | none yet | Planned: S1–S5 |
| Stairwell EXIT door and stair (C22) | `ExitDoorSealed`, `NorthDoorMoved` / `NorthDoorLatched` (land with C22) | none yet | Planned: real steel-door Foley (Red 10-22), plus one optional distant slam |
| Menus, display settings, restart | none | nothing | Silent. HUD key/lock sounds wait on Red's notes. |

## Open with Red

1. **Freesound login.** It unblocks the title-door swing and the key-unlock beats.
2. **HUD key/lock sounds v2.** Waiting on Red's notes on the first pass.
3. **Title music / motif.** M1, M2 or M3. Relay v2 keeps the lock-on chime as the game's only tonal chime, so a motif would live in the title loop only.
