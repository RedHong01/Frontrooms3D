# Bathroom rooms · 60 · Sound asks for 声音设计

Date: 2026-10-31. Line: 浴室房间调研, K session (Red's API key, Cursor, `cursor_K_bathroom_1031`). Stage 3 of the kickoff ("sound asks for 声音设计"). 声音设计 owns the sound: banks, events, mixes and captions (`AUDIO_CONTRACT.md`: game code only posts events; `HunterState` append-only). These are asks, not designs.

Starting points already on this machine (do not redo): `10_research.md` §3.4 (loudness table: water adds no loudness, it takes away silence; slow wade heard at 6 m, ankle at 3 m, plunge 18 m and +8 attention, climb-out 6 m; reverberation numbers), `30_estimate.md` P6 (10–14 PAH), `15_poolrooms.md` §2 and §5 (the canon's dampened sound), `16_pool_palette_fusion.md` §4, `18_structure_fusion.md` §2, `50_modules_and_contracts.md` (modules and zones). 游戏视觉's note (card bathroom-002): the metal-halide ballast hum is 声音设计's; the restrike event's length is 系统设计's.

## 1. Three acoustic zones (the main ask)

| Zone (50 §2) | What the player should hear | Why |
|---|---|---|
| PoolClub (entry, lockers, the hall) | Real tiled acoustics: the hall rings with a long tail (10 §3.4), lockers short and bright; pump hum under everything; fluorescent and metal-halide ballast hum | The building still works with nobody in it (D1 A, 15 §4) |
| PoolLevel (past the far door) | **Wrong acoustics: a tiled room that does not answer.** Short, damped tail; sounds drop off suddenly; voices and the player's own steps feel muted; the water's sound calm, but in rare rooms swelling to cover everything | The canon (15 §2: "sound does not reverberate as it should", speech "muted") and 16 §4; the missing tail is the cue that the place is wrong |
| PoolDark (the dark zone) | Near-silence: wet-niche lamps' faint transformer buzz, the pump far away, drips; the player's wading the loudest thing | Red's fixed dark zone (handoff §6); darkness is never cover (rule 12), so sound must not hide the Relay either |

Ask: zone snapshots (or reverb sends) switched by the map's zone at the far door and at the dark zone's border, crossfaded over one cell.

## 2. Events (game code posts; 声音设计 voices)

| Event | Where it fires (owner) | Notes |
|---|---|---|
| Footsteps on Wet tile, in Ankle water, in Wade (walk, slow, run) | player (交互设计, P2) by `WaterAt` class | loudness per 10 §3.4; ankle run as a sprint step |
| Plunge, climb-out | player (P2) | the plunge is one loud mistake (+8 attention) |
| Beach entry / weir step (C4) | player (P2) | a step down off a weir: a short splash, not a plunge |
| Relay water steps (Ankle, Wade) | Relay (关卡设计, P3) | heard by the player under the stage-2 rules; never a radar (rule 12) |
| Weir sheet and spill (PL_Leisure) | module emitter | a steady falling-water bed; masks nothing within 6 m of the player's own steps |
| Slide tube drip and gurgle (PL_Leisure) | module emitter | Pike's machine-under-water fear (15 §3): from inside the wall |
| Intake and main-drain grates (PC_Hall, PL_FireStair, PL_Gallery) | module emitter | low suction; louder near the dark zone |
| Observation windows (PL_Gallery) | module emitter | the basin behind the glass, muffled |
| Metal-halide restrike (pool rig) | lamp state (游戏视觉 / 系统设计 owns the length) | the overheads go out, a long tick-and-hum back over minutes |
| Pump hum, ballast hum | zone bed | stops in the dark zone |
| Fire stair (PL_FireStair) | module emitter | the stairwell's dry acoustics above the water, the drowned gate's slow knock in the current |

## 3. Captions and rules

- Captions for the beds and every event above, per the captions rule; water steps captioned by class ("wading", "splashing").
- Rule 24: every audition and check by capture and measurement, never through Red's speakers.
- Sources: the CC0 library in use since 10-22; no Artlist audio in Assets (rule 10).
- Stage-2 acceptance re-measured by capture with the water events in (P6).

## 4. Size

P6 stays 10–14 PAH for the club and the base events; the three zone snapshots and the module emitters add about 6–10 PAH [EST]. Send any different number to 系统设计 through 总控.
