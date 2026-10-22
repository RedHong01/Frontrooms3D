# In-game screens: one visual system (平面视觉, 2026-10-07)

Red (2026-10-07) gave the TV/computer-ad slice to 平面视觉: "分析消化并且revise，把它接入到游戏中一整套视觉系统里". This file is the analysis and the revision plan.
- The v0 slice and its engineering notes stay in `SCREEN_ADS.md`.
- Work items live in `GRAPHIC_VISUAL_TASKS.md` §3.
- The research is in `../Research/week02/furniture-ads/`: `tv_notes.md`, `FINDINGS.md` and `SOURCES.md`. It is reference only and never ships.

> **2026-10-22: the station is now WTCZ-TV 61** (city of licence FAIRVIEW). WXRM was a real call sign (WXRM-LP, NJ), so 叙事设计 renamed it under rule 11 (no real stations; `research/screens/30_screen_strings.md` v2.1, check record in `AD_TYPE_SYSTEM.md` §7.1). Red was told, not asked, and can still veto in the 广告 chat. Older mentions of WXRM in this file mean the same fictional station. The game's Ad_02 still shows WXRM until Red approves the swap.

## 1. What exists (v0, Codex chat, 2026-10-04)

**Video.** `Assets/StreamingAssets/FrontRooms_Ad_01.mp4`: 640×434, 30 fps, 12 s loop, four cards: LEVITZ / FRONTROOMS FURNITURE $799 / CALL NOW 1 800 FRONTROOMS / YOU WILL LOVE IT. It was made inline with ffmpeg drawtext; there is no generator in `Tools/`.

**Playback.** `Assets/Scripts/Media/FrontRoomsScreenVideo.cs`:
- A VideoPlayer bank per source, writing to a runtime RenderTexture.
- A URP/Unlit quad of 0.28×0.19 m, placed 4 mm in front of the `Kit_CRTMonitor` screen anchor.
- Decoding is gated by visibility within 18 m. E toggles power within 2.4 m.

**Where it plays.** Every office desk monitor (`FrontRoomsOfficeKit.cs:662-675`). `Kit_CRTTV` shows nothing, and neither do monitors that land in piles.

**Figma.** The Week 2 deck pages 25–28 ("SCREEN ADS · 1990 BROADCAST FRAMES", frames 2669:6094 / 6122 / 6150 / 6178) document the four cuts.

## 2. What has to change before anything ships

| # | Problem | Why it matters |
|---|---|---|
| P1 | Card 1 is **LEVITZ**, a real 1990 retailer, taken from the research clips | The research is "study and reference only, must not ship" (`SOURCES.md`). A real trademark on screen is the clearest case of that. |
| P2 | The type is **macOS Arial Black / Arial Narrow / Courier** baked into the video. "FRONTROOMS FURNITURE" runs off the frame. Secondary lines are dark-on-dark red, blue or green. | It is outside the game's type system (faces go through 平面视觉; ship from `Assets/Fonts/Period1990` per `FONTS_PERIOD_1990.md`). There is no title-safe area, and the secondary lines can't be read on a 28 cm face from 2 m away. |
| P3 | **There is no CRT.** It is a flat Unlit quad at the same brightness in a lit office or a dead room: no glass, phosphor, scanlines, bloom, curvature, or power-off state. | It reads as a phone screen pasted on a 1990 monitor. It also ignores the lamp and power systems that everything else in the room obeys. |
| P4 | Every screen plays the same cut on the same clock | Same-channel sync is period-correct (a broadcast is one signal). What breaks the illusion is that every set is on, on the same channel, all the time. |
| P5 | `Shader.Find` creates the material at runtime (`FrontRoomsScreenVideo.cs:210`) | Codex audit F6 (MAJOR): the material is missing in a player build. |
| P6 | Screen anchor and size are hard-coded to `Kit_CRTMonitor` | `Kit_CRTTV` (anchor (0, .284, .245)) cannot carry a screen. |

## 3. The system: three layers on every screen

The research found two type systems stacked in every 1990 local spot: the agency's produced creative, then the station's character-generator tag. See `tv_notes.md`, ethanallen_1991_cnn: "two type systems stacked (agency vs. station CG)". The game's screens use that structure, plus a physical layer.

1. **Programme, the agency layer.** The spot's own display type, built from the research findings:
   - heavy condensed oblique sans for price supers, with a superscript $ and a hard drop shadow (levitz_1990-12, waterbedcity_1988);
   - a script or swash logotype bug in a corner (evans_1990, levitz).
   - The faces are Period1990 stand-ins for the period face, mapped in `FONTS_PERIOD_1990.md`.
   - The brand is fictional; the narrative chat writes all strings.
2. **Station, the character-generator layer.** One fictional local UHF station is the only "channel" in the game.
   - **Revised 2026-10-15 (平面视觉 · 广告) against 1990 airchecks, the Chyron VP-2 manuals and SMPTE documents.** Research: `~/FrontRoomsVisualWork/reference/ad_reference/station_1990/NOTES.md`, 156 sources. The spec is `AD_TYPE_SYSTEM.md` §3.
   - **The CG** is a VP-2-class unit: Helvetica Medium and Bold (TeX Gyre Heros in the generator), set in whole scan lines (18–48) with a 2-line black edge and a 4-line drop shadow, unsmoothed. This replaces VT323, which is a terminal face.
   - **Identity.** The channel number is the brand ("61"); the call letters come second. The legal ID is the call letters immediately followed by the city of licence (47 CFR 73.1201). No "UHF" on screen.
   - **Not in 1990:**
     - a permanent corner logo bug: networks only adopted them in autumn 1993;
     - a corner time and temperature: in 1990 the time showed only over the off-air bars.
     - Both are dropped.
   - **Non-programme states:**
     - the station ID;
     - stand-by: the ID slide with "PLEASE STAND BY / TECHNICAL DIFFICULTIES";
     - the sign-off;
     - off air: SMPTE EG 1-1990 bars with the time and date keyed in;
     - snow.
   - **Dealer tags** fill the last 3–7 s of a spot: store, address, town, phone, hours.
   - **Safe areas.** Everything is laid out on the 4:3 raster inside SMPTE RP 27.3-1989's title-safe (80 %) and action-safe (90 %) areas, which have rounded corners. The CRT face then crops it the way a real tube's overscan does (4–5 % per edge).
3. **Tube, the physical layer.** Shader work owned by 游戏视觉, specified here:
   - aperture-grille phosphor and scanlines that fade with distance, so they never alias;
   - phosphor bloom and a slight barrel curvature with corner vignette;
   - the curved glass with reflections from the glass system;
   - a power-off state of dark glass. The faint burn-in ghost of the station bug was dropped on 2026-10-15: no bug existed in 1990, so there is nothing to burn in;
   - a power-on/off collapse to a horizontal line and a dot.
   - **Power and the warning language stay apart** (amended 2026-10-14; 系统设计, `research/computer_ui/20_design.md` §6.3). The ink's stage-0 power sags and the chase-wave dips fire the same `FixtureChanged` / `LampDipped` events as a real warning. If the screens followed them, ambient would speak in cue vocabulary, which breaks G7: ambient never uses the cue's signs. So on every screen, TVs and terminals alike:
     - the picture sags, shrinks or rolls only on `WarnBurst`, and only if Red approves §4;
     - no screen reads `LampLevel` or the Warn overrides;
     - an ambient set may follow only its own cell's generated lamp temperament;
     - every change is slow, with an attack of at least 1.5 s.

## 4. How the screens join the warning language

These are the same events and timing as the wallpaper cue (A + M4 + V1, `GRAPHIC_VISUAL_TASKS.md` §1). Wallpaper, lamps and screens tell one story.

| Game event | Wallpaper | Screens in the same cells |
|---|---|---|
| Normal | plain print | programme, or off (most sets are off) |
| WarnStage 1 (Warn lamp bursts) | arrows turn, turn only | interference: hum bars, chroma loss, a roll; the programme continues |
| StateChanged(Chase), wave at 8 m/s | stepping wave | as the wave reaches a set, it cuts to the station's **emergency slate**. The strings belong to the narrative chat; EGRESS language is the candidate. |
| Chase → Search, 2–3 s retract | arrows turn back | the slate drops to snow, then the programme comes back with a roll |
| Reduce Motion / Reduce Flashing | turn only | no roll, no flicker; hard cuts only |

Proposal only. 系统设计 and 关卡设计 own the events (driver interface pending), and Red decides whether screens join the warning language at all.

## 5. Content and pipeline

- **Generator:** `Tools/screens/` (to build). A JSON spec of cards, holds, cuts and CG tags renders 4:3 frames with Period1990 fonts, mirroring the print and ink tools. It writes:
  - the live video for near screens, H.264 at 640×480;
  - a flipbook Texture2DArray (16 frames at 256×192) for every other screen, sharing one channel clock so all sets stay in sync.
- **Budget, desktop:** live decode for the two nearest powered screens in view within 6 m; every other screen uses the flipbook.
- **Budget, WebGL:** flipbook only. This is its own tier with no VideoPlayer, so the unverified Chrome codec and range-request questions in `SCREEN_ADS.md` never come up.
- **Variation:** about 1 set in 3 is on; the rest are off (dark glass). A few show snow or the slate. Sets on the station channel are in sync.

## 6. Owners

| Part | Owner |
|---|---|
| Programme and station design, type, safe areas, generator, state visuals, tube look spec | 平面视觉 · 广告 (since the 2026-10-15 split; §4 is co-owned with 平面视觉 · 信息设计) |
| Station fiction, brand, every on-screen string, EGRESS slate wording | narrative chat |
| Tube shader and material, the F6 fix (serialized material, no `Shader.Find`), per-kit screen anchors (`Kit_CRTTV`) | 游戏视觉 |
| Event hooks (warn, chase, search, lamp events), budget gating | 系统设计 / 关卡设计 |
| Go or no-go on screens joining the warning language | Red |

## 7. Next steps (平面视觉 · 广告)

**Done:**
1. Style frames v1 (2026-10-08, Figma `2800:6096`).
2. The generator and Ad_02, in the game since 2026-10-14.
3. **Station v3 (2026-10-15).**
   - Figma `3226:7267`: Ad / styles, six 1× masters, slides S09–S12.
   - The dev generator: `~/FrontRoomsVisualWork/screens_dev/`.
   - VL287 `3230:8391`.

**Next:**
1. Red approves v3. Then:
   - the generator moves into `Tools/screens`;
   - Ad_03 is cut (Ad_02's cards plus the v3 station card, the numeral fix and the dealer tag);
   - 游戏视觉 swaps the StreamingAssets file.
2. Strings from the narrative chat:
   - a city of licence;
   - the off-air time and date;
   - the sign-off notice;
   - the dealer tag's town;
   - whether WXRM is renamed (WXRM-LP is a real NJ low-power FM station).
3. The tube request to 游戏视觉: the tube layer and the Kit_CRTTV anchor. F6 is fixed. It waits for Red's go.
4. Interference frames and the tube look over all states, once §4 is decided.
