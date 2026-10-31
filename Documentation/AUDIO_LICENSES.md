# Audio licenses

## What ships (2026-10-22)

- Every sound in the FMOD banks (`Assets/StreamingAssets/FMOD/*.bank`) is either a CC0 recording listed below or a placeholder synthesised for FrontRooms. **No attribution is required** in the game credits or on store pages.
- **The 2026-10-22 licence fix.** Two CC-BY sources had shipped without credit: 187617 (the soaked-shoe squish) and 341512 (the large-room air). They were replaced with CC0 material already in the library:
  - `Foley/plr_step_soaked_any_squelch` is cut from 583287.
  - `Ambience/amb_air_large_loop` is cut from 406508, slowed 0.84x.
  - Builds made before this fix still contain the two CC-BY sounds without credit. The store chat tracks them in `RED_ACTIONS.md` #3.
- **Artlist.** No Artlist audio is in the project. Red's Artlist plan covers video only, so Artlist sounds may be used for trailers and store videos, but never in the game or the public repo.
- **HUD UI sounds** (`AudioSource/HUD/`, in the banks from code 2026-10-22.8 as `UI/Hud*`) are cut and processed by `Tools/audio/hud/build_hud_sfx.py` from CC0 takes already in the library (611276 xkeril, 616835 TRP, 768656 Nox_Sound, 843829 thaighaudio). No new source.
- **Screen sounds** (`AudioSource/Screen/`, from code 2026-10-22.9) are made for FrontRooms by `Tools/audio/screen/`; no third-party recording. The WTCZ station loop is composed in `wtcz_loop.py` and rendered offline through macOS's built-in GS synth (Apple's `gs_instruments.dls`), as 平面视觉 · 广告's promo audio is (`Tools/screens/promo_audio.py`); the set's power-on, power-off and whine are synthesised (`tv_set.py`). Open question before a public or store build: confirm that Apple's licence for its system instrument set covers rendered audio in a game.

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
| [341176](https://freesound.org/people/klangfabrik/sounds/341176/) | klangfabrik | CC0 | Doors | Stairwell EXIT door: crash bar and steel close; Relay blow hit (-4 st) |
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
| [637479](https://freesound.org/people/kyles/sounds/637479/) | kyles | CC0 | Doors | EXIT crash bar and close |
| [557348](https://freesound.org/people/launchsite/sounds/557348/) | launchsite | CC0 | Doors | EXIT crash bar and close |
| [637477](https://freesound.org/people/kyles/sounds/637477/) | kyles | CC0 | Doors | EXIT crash bar and close, the leaf behind the chain |
| [453301](https://freesound.org/people/kyles/sounds/453301/) | kyles | CC0 | Chains | EXIT chain: tug and drop |
| [167913](https://freesound.org/people/ani_music/sounds/167913/) | ani_music | CC0 | Chains | EXIT chain: drop on carpet |
| [330635](https://freesound.org/people/leonelmail/sounds/330635/) | leonelmail | CC0 | Chains | EXIT chain: pulled through |
| [733605](https://freesound.org/people/C_Sander/sounds/733605/) | C_Sander | CC0 | Chains | EXIT chain: rattle, pitched down |
| [536414](https://freesound.org/people/Rudmer_Rotteveel/sounds/536414/) | Rudmer_Rotteveel | CC0 | Locks | EXIT padlock |
| [632489](https://freesound.org/people/ani_music/sounds/632489/) | ani_music | CC0 | Locks | EXIT padlock |
| [844287](https://freesound.org/people/randbsoundbites/sounds/844287/) | randbsoundbites | CC0 | Body | Breath, heavy |
| [422342](https://freesound.org/people/BehanSean/sounds/422342/) | BehanSean | CC0 | Body | Breath, calm |
| [571204](https://freesound.org/people/SamuelGremaud/sounds/571204/) | SamuelGremaud | CC0 | Body | Heartbeat |
| [407303](https://freesound.org/people/kyles/sounds/407303/) | kyles | CC0 | Doors | Relay steps: the landing layer |
| [450791](https://freesound.org/people/kyles/sounds/450791/) | kyles | CC0 | Doors | Relay steps: the low tail |
| [570004](https://freesound.org/people/Sheyvan/sounds/570004/) | Sheyvan | CC0 | Doors | Relay steps: the building's answer |
| [155753](https://freesound.org/people/klangfabrik/sounds/155753/) | klangfabrik | CC0 | RoomTone | Stairwell air |
| [502180](https://freesound.org/people/thaighaudio/sounds/502180/) | thaighaudio | CC0 | RoomTone | Air through the far door's gap |
| [770282](https://freesound.org/people/Sadiquecat/sounds/770282/) | Sadiquecat | CC0 | RoomTone | The far door's whistle |
| [593939](https://freesound.org/people/mincedbeats/sounds/593939/) | mincedbeats | CC0 | Locks | The far door's handle |
| [571276](https://freesound.org/people/gecop/sounds/571276/) | gecop | CC0 | Doors | The far door's pull |
| [372901](https://freesound.org/people/FairhavenCollection/sounds/372901/) | FairhavenCollection | CC0 | Doors | A door far below |
| [106128](https://freesound.org/people/j1987/sounds/106128/) | j1987 | CC0 | Doors | A door far below |
| [148462](https://freesound.org/people/conleec/sounds/148462/) | conleec | CC0 | Footsteps | Player on steel stairs: walk, run, stop |
| [698697](https://freesound.org/people/IENBA/sounds/698697/) | IENBA | CC0 | Footsteps | Player on steel stairs: walk |
| [537740](https://freesound.org/people/steeltowngaming/sounds/537740/) | steeltowngaming | CC0 | Locks | Bolt, variations |
| [218324](https://freesound.org/people/SpliceSound/sounds/218324/) | SpliceSound | CC0 | Locks | Knob turn, variations |
| [342873](https://freesound.org/people/Pfannkuchn/sounds/342873/) | Pfannkuchn | CC0 | Doors | Latch strike, variations |
| [627768](https://freesound.org/people/ahriik/sounds/627768/) | ahriik | CC0 | Doors | Latch strike, variations |
| [757195](https://freesound.org/people/HenKonen/sounds/757195/) | HenKonen | CC0 | Doors | Unlatch, variations |
| [402466](https://freesound.org/people/LampEight/sounds/402466/) | LampEight | CC0 | Doors | Title door seat, variation |
| [484157](https://freesound.org/people/TemperMode/sounds/484157/) | TemperMode | CC0 | Fluorescent | Fixture ticks, variations |
| [637546](https://freesound.org/people/kyles/sounds/637546/) | kyles | CC0 | Fluorescent | Fixture strikes, variations |
| [203306](https://freesound.org/people/mzui/sounds/203306/) | mzui | CC0 | RoomTone | Office air |
| [708021](https://freesound.org/people/Soup_UnderScore/sounds/708021/) | Soup_UnderScore | CC0 | RoomTone | Office air, raised to 60 Hz mains |
| [804975](https://freesound.org/people/Alpaca_Audio/sounds/804975/) | Alpaca_Audio | CC0 | Footsteps | Player carpet run, variations |
| [234855](https://freesound.org/people/mlsulli/sounds/234855/) | mlsulli | CC0 | Footsteps | Player carpet walk, variations |
| [406261](https://freesound.org/people/Anthousai/sounds/406261/) | Anthousai | CC0 | Glass | Pane hit, variation |
| [648134](https://freesound.org/people/5plus5equalspatata/sounds/648134/) | 5plus5equalspatata | CC0 | Glass | Pane gives way, variation |
| [552161](https://freesound.org/people/quantumriver/sounds/552161/) | quantumriver | CC0 | Keys | Key pickup, variations |
| [450357](https://freesound.org/people/kyles/sounds/450357/) | kyles | CC0 | Keys | Key pickup, variations |
| [555307](https://freesound.org/people/Nox_Sound/sounds/555307/) | Nox_Sound | CC0 | Cloth | Cloth, variations |
| [429080](https://freesound.org/people/leonelmail/sounds/429080/) | leonelmail | CC0 | Cloth | Cloth, variations |
| [637870](https://freesound.org/people/kyles/sounds/637870/) | kyles | CC0 | Screen | CRT whine and power |
| [784207](https://freesound.org/people/OverlookHotelRecords/sounds/784207/) | OverlookHotelRecords | CC0 | Screen | CRT power |
| [427110](https://freesound.org/people/buzzatsea/sounds/427110/) | buzzatsea | CC0 | Doors | The EXIT door's closer |
| [727367](https://freesound.org/people/mrmonkeychunkey/sounds/727367/) | mrmonkeychunkey | CC0 | Footsteps | Player on the station's hard floor: walk, run, stop |

The BigSoundBank creak above (CC0) also supplies `Door/door_swing_creak_loop` and `Door/door_stop_soft_03`.

CC0 sources need no credit; they are listed for provenance. Nothing here is from the BBC Sound Effects library (non-commercial licence), the A24 film, or any game.
