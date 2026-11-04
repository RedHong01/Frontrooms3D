# Menu sound · 10 · Research: how games voice their menus

Line: 声音设计 (sound), 2026-11-03. Asked by Red in this line's chat, 2026-11-03: "现有的声音设计也需要做一个新的调研线路，就是在和设置菜单时交互是否有触发音" and "如果有，一整套触发音一般3A/我这种题材的游戏都是怎么做的，调研之后给我的游戏菜单交互触发音优化". Sources in `SOURCES.md` (S1–S12). The design for FrontRooms is `20_design.md`.

## 1. Does FrontRooms make any sound in its menus today? No.

Checked in main's code on 2026-11-03 (`Assets/Scripts`):

| Surface | Interactions | Sound today |
|---|---|---|
| Title | Space / TAP TO START, the title TV | the TV's broadcast only (`Screen/TitleTV`); the start makes no UI sound |
| Pause = the desktop settings page (Red 10-24) | open with Esc, RESUME, END RUN, Space peek | none. On pause the director pauses every bus but UI (`PausedBuses`: SFX, AMB, Music, Subjective), so the world cuts to dead silence |
| Settings rows (`FrontRoomsSettingsMenu`, 4 tabs) | focus move (keys, pad, mouse hover), tab change, Switch, Segments, Slider, Action (RESET), Page (key map, pad map), Language | none |
| Key map / pad map pages, rebinding (`FrontRoomsBindCapture`) | open page, listen for a key, capture, conflict | none |
| Language strip (charms on the key, Red 10-31) | hover swings a charm, a pick flies it onto the key | none |
| Touch sheet (iPhone, iPad) | taps, drags on sliders | none |
| In-game HUD B | key flight, land, padlock tug, unlock drop, broken door | yes: six real-Foley cues on the UI bus (`UI/Hud*`, code .8) |

There is also **no audio section in settings**: no master, music, effects, ambience or UI volume, and no mono option. The FMOD project already has the three VCAs a volume page would drive (`VCA Music`, `VCA SFX` incl. UI, `VCA Ambience`).

## 2. What large games do (the common grammar)

1. **A small, fixed vocabulary, one sound per meaning.** Focus (move), commit (select/confirm), back (cancel/close), refused (error, unavailable), value change (slider step, toggle), page/tab change, open/close of the menu. Platform holders write this into their accessibility rules: sounds for different key events must be distinct (XAG 105 via the Game Accessibility Guidelines, S3, S4), and Xbox itself lets players mute "focus blips" and "page whooshes" (S5).
2. **A hierarchy of weight.** Focus is the lightest (a tick: the cursor moved); commit is brighter or heavier; back is duller or falls; refused is distinct in timbre, not just louder (S1, S2; Civilization VII's GDC talk is built on "hierarchies and design principles", S6; Formosa's GDC 2025 talk on UI sound "with intention": clarity and function first, then character, S7).
3. **Short, and never longer than the motion.** A sound that outlasts its transition makes the menu feel slow (S2). Typical lengths: focus 20–80 ms, commit 80–250 ms, open/close up to the animation's length.
4. **Repeats are limited and varied.** Held keys, a scrolled list and a dragged slider fire many events: rate-limit them (one tick per step, at most every 40–60 ms) and vary pitch slightly so they never machine-gun (S1, S2).
5. **The world ducks under the menu.** A pause or settings snapshot lowers and low-passes the game rather than cutting it, so the place is still felt and the UI reads on top. In this project the EXAMINE close-up already does this (room tone 10 dB down, after SOMA's Safe Mode, AUDIO_CONTRACT); the pause does not.
6. **Players control it.** Separate volumes for music, voice, effects, ambience, and often a UI slider (Grounded's six sliders are XAG 105's example, S3); a mono option; on Xbox a system switch to mute navigation sounds (S5). The Last of Us Part II goes further: menu narration and an audio glossary of every cue (S8).
7. **On phones the sound rides with haptics.** Apple's guidance: match a haptic's intensity and sharpness to its animation, synchronise sound with it, and keep feedback for moments that matter (S9, S10).

## 3. What horror games do (the genre's own rule: the menu belongs to the world)

- **Diegetic or period hardware, not abstract bleeps.** Alien: Isolation built its whole interface from 1979 technology: UI recorded to VHS and played back on tube TVs, sound made with "quarter inch tape, razor blades and all things analogue" (S11, S12). Dead Space put the inventory and the store inside Isaac's RIG (GDC 2013, S13). The menu sounds like an object of that world, so opening it does not break the dread.
- **Restraint.** Silent Hill 2 (2024) lets the player switch every UI element off and lets sound do the guiding (S14). Horror menus are quiet: the tension is the world's, the menu only answers.
- **No musical stingers in the menus.** Tonal blips teach the ear a meaning; horror keeps tonal cues for threat. *[Our observation of RE2 (2019), RE Village, Alien: Isolation, Amnesia: menus answer with clicks, paper and machine noise, not tones; not from a published source.]*
- **Pause rarely cuts to digital silence.** Either the room tone stays under the menu (SOMA's Safe Mode, already our model for EXAMINE) or the menu has its own bed (RE's save-room music) (S15).

## 4. What follows for FrontRooms (constraints from our own rules)

- **The only tonal chime in the game is the Relay's lock-on** (RELAY 9.4). No menu sound may be a chime, a beep or a musical tone, or the ear learns the wrong thing.
- **Era lock ≤ 1993, an American office of c.1990, no brands.** The menu's paper look (keyline buttons, paper words, the canary yellow focus) asks for the sounds of that office: paper, a manila folder, a rocker switch, a rotary selector's detent, a desk keyboard, the keychain the language charms hang on.
- **Three-platform parity.** The same events on desktop, iPhone and iPad. Touch has no focus, so it hears commits, steps and pages only; keys and pad hear focus too.
- **Rule 24.** Every check of these sounds by capture and measurement, never through Red's speakers.
- **Game code only posts events** (AUDIO_CONTRACT): the menus raise cues, the sound line voices them, as HUD B does with `FrontRoomsKeyHud.SoundCue`.
