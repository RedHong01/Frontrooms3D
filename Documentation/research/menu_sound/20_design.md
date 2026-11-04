# Menu sound · 20 · Design for FrontRooms

Line: 声音设计, 2026-11-03. Research and sources: `10_research.md`, `SOURCES.md`. Status: decided by Red 2026-11-03 ("全部按推荐": M1 B, M2 B, M3 B), being built. The code hook (§4) is the settings line's and 交互设计's to raise; the sound line voices it.

## 1. The idea in one line

The menus sound like the office the game is set in, c.1990: paper, a manila folder, a rocker switch, a rotary selector's detent, a desk keyboard, the keychain the language charms hang on. Quiet, short, never a tone (the Relay's lock-on stays the only chime in the game), and the world stays faintly under the pause instead of cutting to digital silence.

## 2. The vocabulary (one sound per meaning, lightest to heaviest)

All 2D on the UI bus (not paused by the pause), levels relative to `UI/HudKeyLand` (0 dB). Length is the sound's, never longer than the motion it answers.

| Cue | When | Sound (the object) | Level | Length | Repeats |
|---|---|---|---|---|---|
| Focus | focus moves to another row or button (keys, pad, mouse hover). Not on touch | a rotary selector's detent, the lightest tick | -14 | 20-40 ms | at most every 45 ms; ±0.5 st |
| Step | a Segments row changes value | the same detent, heavier, with the switch's spring | -8 | 40-70 ms | one per step |
| Slider tick | a Slider passes a step (dragged, keys, pad) | the detent, pitched by the value (-2 st at 0, +2 st at 1) | -12 | 20-40 ms | at most every 40 ms; a fast drag is a few ticks, not a buzz |
| Switch on / off | a Switch row toggles | a rocker switch: on is the bright snap, off the lower one | -6 | 60-100 ms | one |
| Commit | an Action (RESET GRAPHICS), RESUME, a page's entry, a language pick's confirm | a push button seated, with its click | -4 | 80-150 ms | one |
| Back | leave a page, close a sub-view | the commit's button released: duller, 2-3 st lower | -7 | 60-120 ms | one |
| Refused | a row that cannot change now, a bind conflict, END RUN's first press if it asks again | a locked desk drawer's short bump: dull, wooden, no rattle (never the door's `Locked` rattle, which means a locked door) | -6 | 80-120 ms | at most every 300 ms |
| Tab | Graphics, Comfort, Controls, Assist | a manila folder's tab flicked over | -9 | 120-200 ms | one per tab |
| Page open / close | key map, pad map open / close | a sheet slid out of / back into a folder | -8 | 200-300 ms | one |
| Menu open / close | the pause page opens (Esc) / RESUME | the world ducks (§3) and a sheet slides out / back | -8 | as the page's motion (about 0.25 s) | one |
| Bind listening / captured | the key map waits for a key / takes it | silence while it waits; on capture a desk keyboard's keystroke (the key's travel and bottoming) | -6 | 60-100 ms | one |
| Charm hover / pick | the language strip: a charm swings / flies onto the key | the keyring's small tinkle / the keys landing (the same keys as HUD B's `HudKeyLand`, so the strip and the HUD speak one language) | -14 / -4 | 100-300 ms | hover at most every 150 ms |

Not voiced: the frame-rate meter, the Space peek (the world under it is the sound), text input, the title's start (the title door and TV already carry it).

Rules: no tones, chimes or musical stingers; no reverb on menu sounds (they are close to the hand); mono sources, centred; each cue 2-3 takes, picked without repeat; nothing on a timer, only on an action.

## 3. The pause snapshot (replaces dead silence)

Today the pause pauses SFX, Ambience, Music and Subjective: a cut to digital silence. Proposed, as EXAMINE already does (SOMA's Safe Mode): the room tone and the HumBed stay, 14 dB down and low-passed at 1.2 kHz over 0.25 s, so the building is still there behind the menu; everything that moves (Foley, mechanisms, the Relay, the heartbeat, stingers) stays paused as today. On RESUME it all comes back over 0.25 s. From the title (settings opened over the title TV), the TV ducks 10 dB the same way.

## 4. The hook (code contract, same shape as HUD B)

- New enum `FrontRoomsMenuSoundCue { Focus, Step, SliderTick, SwitchOn, SwitchOff, Commit, Back, Refused, Tab, PageOpen, PageClose, MenuOpen, MenuClose, BindCaptured, CharmHover, CharmPick }` (append only).
- One static event raised by the menus: `public static event Action<FrontRoomsMenuSoundCue, float> SoundCue;` (the float is the slider's value 0-1, else 0), on the menus' own clock at the start of each motion, like `FrontRoomsKeyHud.SoundCue`.
- Raised by: `FrontRoomsDesktopSettingsView` and `FrontRoomsSettingsMenu` (settings line), the touch settings sheet and the bind capture (交互设计), `FrontRoomsLanguageStrip` (settings line). The rate limits of §2 live in the sound line's director, so the menus raise every event and never count.
- The director plays `UI/Menu*` events (2D, UI bus) and applies §3 on `FrontRooms3DGame.Paused`. Nothing in the menus' behaviour changes; with the sound line's side not landed, the events are raised to nobody.

## 5. Player control (XAG 105, Red to decide in §6)

An AUDIO group in settings (the settings line's rows; the VCAs exist): MASTER, EFFECTS (`VCA SFX`), AMBIENCE (`VCA Ambience`), MUSIC (`VCA Music`), MENU SOUNDS (on / off: the UI bus's menu events only, never HUD B or captions), MONO (a stereo-to-mono downmix). RELAY 9.5's "Warning cue volume" can join it later. Same rows on desktop, iPhone and iPad.

## 6. Decisions for Red

**Decided 2026-11-03, Red in 声音设计: "全部按推荐" = M1 B (download office-hardware CC0 takes), M2 B (the building stays under the pause), M3 B (the AUDIO group).**

**M1 · Where the sounds come from.**
- A · Only what the library has now (CC0, already in the repo): a CRT set's power relay (637870), a latch tick (757195), the keyring (616835), cloth (611276). Works at once. The menu then sounds like small metal switches and clicks: clean, but no paper, no folder, no keyboard, so tabs and pages use cloth swishes. Cost: about half a day.
- B · **(recommended)** Download about 8 to 10 CC0 recordings of real office hardware from Freesound (a rocker switch, a rotary selector, an old desk keyboard, a push button, a manila folder, paper sheets, a desk drawer), outside the repo, credited in AUDIO_LICENSES. The menu then sounds like the paper menu it looks like. Nothing is played on speakers (rule 24); Red can audition them on the listening page. Cost: about one day.

**M2 · The pause.** A · keep today's dead silence. B · **(recommended)** the building stays faintly under the pause (§3), as EXAMINE already does. C · a quiet pause bed of its own.

**M3 · Player volume controls.** A · none (today). B · **(recommended)** the AUDIO group of §5, built by the settings line with the sound line's buses.

## 7. Order of work once Red answers

1. Sound line: `UI/Menu*` events and takes (from M1's sources), the director's listener with the rate limits, the pause snapshot (M2). Lands inert (no menu raises yet). Gate: Roslyn mac + iOS, dry run; renders by FMOD NRT, never speakers.
2. Settings line and 交互设计: raise `SoundCue` at the points of §2 (desktop page, touch sheet, bind capture, language strip); the AUDIO group (M3).
3. Combined test (rule 25): a capture walk through every row with keys, pad and touch; each cue once per action, rate limits hold on a held key and a fast slider drag, nothing plays under the frame meter, the pause keeps the room tone 14 dB down.
