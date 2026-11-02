# Input band · 10 · what others do (first pass)

Line: 周志 · 课程交付. Stage: research, first pass, 2026-11-01. Four searches only (Pro usage gate, RULES rule 19); no page opened beyond search summaries, no media downloaded. Sources in `SOURCES.md`. Deeper lanes to run after the 5-hour reset are at the end.

## Findings
- **Streamers:** the OBS Input Overlay plugin is the common tool: layouts drawn from a template, plus an input-history preset as a browser source. Its mouse-movement display is an arrow or dot, and users find it obscure. Gamepad support is uneven. So the mouse is the weak part of the usual overlay, and it is the main input in our game. (S1)
- **History is prized:** fighting-game players ask for input history, not only live state (S2). TAS people discuss showing inputs frame by frame as an educational device (S3). This supports the tape over a keys-only display.
- **Sync needs a shared clock or a marker:** a video file and a log do not align by themselves. Project CARS Replay Enhancer corrects offsets by hand and estimates about ±0.03 s at best (S4). Paparazzi blinks the screen for 1 s as a marker (S5). Stb-tester records frame-accurate video with its logs (S6). SessionStreamer plays logs against playtest video inside a Unity game (S7, private beta). Our answer: log inside the same harness that writes the frames, so each row belongs to a frame and no sync step exists.
- **Text size at 1080p:** sources disagree: never below 22 px, 18-20 px turns to mush in streaming compression (S8); IGDA suggests 32 px at 1080p and 46 px for timed text (S9). These are full-frame numbers. Ours is judged at slide scale, where Bayon 20 and Plex Mono 13 are the deck's own smallest sizes. 平面视觉 should judge them projected.
- **Safe areas:** 90 % title safe, 93 % action safe (S10, S8). They exist for overscan and platform UI; a band below the footage on a slide has neither, so the deck's 72 px margin is the rule that applies.
- **Timing:** subtitles hold at least 5/6 s (Netflix) or 1 s (BBC), reported by S11. No source covers keycast overlays. Our tape keeps true lengths and the key holds a tap for 0.13 s. That number is ours, not sourced.

## Gaps in this pass
Nothing yet from the analysis-video side (speedrun essays, game-feel talks, GDC), or on game user research tools beyond S7. No Figma Slides video limits checked.

## Lanes to run after the reset (one workflow, Opus, effort max, each writes its own file here)
1. `11_analysis_and_replay.md`: how analysis videos and talks tie input to response; pannenkoek2012, Game Maker's Toolkit, Swink's Game Feel, fighting-game training modes.
2. `12_capture_and_figma.md`: AsyncGPUReadback to an ffmpeg pipe in Unity; Figma Slides video limits and white level; check our `colorspace` tags against the slide's white.
3. `13_legibility.md`: projected-slide legibility with real sources (not vendor blogs); thin keylines through 4:2:0 at slot scale.
