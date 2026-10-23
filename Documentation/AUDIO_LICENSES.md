# Audio licenses

## What ships (2026-10-22)

- Every sound in the FMOD banks (`Assets/StreamingAssets/FMOD/*.bank`) is either a CC0 recording listed below or a placeholder synthesised for FrontRooms. **No attribution is required** in the game credits or on store pages.
- **The 2026-10-22 licence fix.** Two CC-BY sources had shipped without credit: 187617 (the soaked-shoe squish) and 341512 (the large-room air). They were replaced with CC0 material already in the library:
  - `Foley/plr_step_soaked_any_squelch` is cut from 583287.
  - `Ambience/amb_air_large_loop` is cut from 406508, slowed 0.84x.
  - Builds made before this fix still contain the two CC-BY sounds without credit. The store chat tracks them in `RED_ACTIONS.md` #3.
- **Artlist.** No Artlist audio is in the project. Red's Artlist plan covers video only, so Artlist sounds may be used for trailers and store videos, but never in the game or the public repo.
- **HUD UI sounds** (`~/FrontRoomsSoundWork/hud_sfx`) are synthesised for FrontRooms and are not in the game yet.

## Door creak

- **Asset:** Creaking Door #2
- **Source:** [BigSoundBank](https://bigsoundbank.com/grincement-de-porte-2-s3205.html)
- **Direct file:** [WAV download](https://bigsoundbank.com/UPLOAD/bwf-en/3205.wav)
- **License:** CC0 / public domain. The source page allows editing, redistribution, and commercial game use without attribution.
- **Recorded format:** indoor, realistic, mono, 48 kHz / 24-bit.

The original is stored at `Assets/Resources/Audio/door-creak.wav`. It is read only by the old Unity audio, which is switched off (`m_DisableAudio: 1`). The FMOD assets `Door/door_swing_creak_loop` and `Door/door_stop_soft_03` are cut from it.

## Recorded library (Freesound)

Originals live untouched in `AudioSource/Recorded/<folder>/`; processed FMOD assets are in `AudioSource/FMOD_Library/` (`LIBRARY.json` maps every asset to its source and second). Regenerate with `python3 Tools/audio/recorded_library.py build`.

### Attribution required in the game credits

None. Every recorded sound in the banks comes from a CC0 source (licence fix of 2026-10-22).

### CC-BY originals kept in this repository only

These raw files stay in `AudioSource/Recorded/` for provenance. No asset, bank or build has used them since 2026-10-22. The repository redistributes them, so they keep their credit:

- "wet_soggy_squishy_footsteps.wav" by bewagne, https://freesound.org/people/bewagne/sounds/187617/, licensed under CC-BY 3.0 (https://creativecommons.org/licenses/by/3.0/).
- "roomtone emptymall indoors 04 160327_00.wav" by klankbeeld, https://freesound.org/people/klankbeeld/sounds/341512/, licensed under CC-BY 4.0 (https://creativecommons.org/licenses/by/4.0/).

### All sources

| Freesound ID | Author | Licence | Folder | Used for |
| --- | --- | --- | --- | --- |
| [148466](https://freesound.org/people/conleec/sounds/148466/) | conleec | CC0 | Footsteps | Player carpet-tile steps (walk, stop) |
| [256209](https://freesound.org/people/hannagreen/sounds/256209/) | hannagreen | CC0 | Footsteps | Player carpet body (sneakers) |
| [560477](https://freesound.org/people/jeroberts92/sounds/560477/) | jeroberts92 | CC0 | Footsteps | Relay steps (hard sole on carpet) |
| [575318](https://freesound.org/people/taure/sounds/575318/) | taure | CC0 | Footsteps | Player carpet body (run) |
| [575321](https://freesound.org/people/taure/sounds/575321/) | taure | CC0 | Footsteps | Player carpet body (walk) |
| [187617](https://freesound.org/people/bewagne/sounds/187617/) | bewagne | CC-BY 3.0 | Wet | Not used since 2026-10-22 (original kept for provenance only) |
| [583287](https://freesound.org/people/Profispiesser/sounds/583287/) | Profispiesser | CC0 | Wet | Moisture, peel and soaked squelch layers |
| [768656](https://freesound.org/people/Nox_Sound/sounds/768656/) | Nox_Sound | CC0 | Doors | Handle, locked rattle, blow rattle, stream-door seat |
| [341176](https://freesound.org/people/klangfabrik/sounds/341176/) | klangfabrik | CC0 | Doors | Latch slam, hard stop, Relay blow hit |
| [843829](https://freesound.org/people/thaighaudio/sounds/843829/) | thaighaudio | CC0 | Doors | Stream-door swing and release, unlatch, latches, stops, Relay blow hit |
| [160213](https://freesound.org/people/qubodup/sounds/160213/) | qubodup | CC0 | Doors | Door splitting, break-through |
| [683823](https://freesound.org/people/Kinoton/sounds/683823/) | Kinoton | CC0 | Doors | Title double door: the open (latch and push) and the close (a heavy wooden thud) |
| [347153](https://freesound.org/people/Mediaman57/sounds/347153/) | Mediaman57 | CC0 | Locks | Unlock shot: key in, turn, bolt drawn, key out |
| [468937](https://freesound.org/people/timothyd4y/sounds/468937/) | timothyd4y | CC0 | Locks | Unlock shot: the knob turned and its spring-back |
| [451139](https://freesound.org/people/kyles/sounds/451139/) | kyles | CC0 | Locks | Title door: the deadbolt thrown from the far side |
| [454098](https://freesound.org/people/kyles/sounds/454098/) | kyles | CC0 | Fluorescent | Hum bed, fixture loop |
| [125064](https://freesound.org/people/EverydaySounds/sounds/125064/) | EverydaySounds | CC0 | Fluorescent | Fixture strike / starter buzz |
| [232447](https://freesound.org/people/mmaruska/sounds/232447/) | mmaruska | CC0 | Fluorescent | Fixture strike, tick |
| [406508](https://freesound.org/people/kyles/sounds/406508/) | kyles | CC0 | RoomTone | Air beds (carpeted hallway; the large-space bed slowed to 0.84x) |
| [341512](https://freesound.org/people/klankbeeld/sounds/341512/) | klankbeeld | CC-BY 4.0 | RoomTone | Not used since 2026-10-22 (original kept for provenance only) |
| [376607](https://freesound.org/people/Soundkrampf/sounds/376607/) | Soundkrampf | CC0 | Glass | Window crack |
| [575283](https://freesound.org/people/TRP/sounds/575283/) | TRP | CC0 | Glass | Window crack and shatter |
| [616835](https://freesound.org/people/TRP/sounds/616835/) | TRP | CC0 | Keys | Key pickup |
| [611276](https://freesound.org/people/xkeril/sounds/611276/) | xkeril | CC0 | Cloth | Cloth layer, cloth event |

The BigSoundBank creak above (CC0) also supplies `Door/door_swing_creak_loop` and `Door/door_stop_soft_03`.

CC0 sources need no credit; they are listed for provenance. Nothing here is from the BBC Sound Effects library (non-commercial licence), the A24 film, or any game.
