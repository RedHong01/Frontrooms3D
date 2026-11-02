# Input band · 20 · design and recording rule

Line: 周志 · 课程交付. Stage: first design, 2026-11-01. Options only: nothing is in the deck, type is PROVISIONAL until 平面视觉 · 印刷与演示 signs it (RULES rule 9).
Red: "我希望能够visualize玩家的input，就像是很多主播在自己的直播下方会放置一些用来visualize当前即时鼠标input的内容…辅助…玩家的输入/游戏的实况之间的联系…应该和平面视觉协同工作，符合目前平面视觉打造的typographic system和grid system".
Tool: `~/FrontRoomsVisualWork/tools/input_band/input_band.py`. Sheet: `sheets/input_band_options.jpg`. Samples: `~/FrontRoomsVisualWork/input_band/samples/sample_{A,B,C}_slot1176.mp4`, `sample_A_slot576.mp4` (scripted sample log on a Run-room film, tagged SAMPLE: not a record of play, not for the deck).

## Split, not burned (Red, 2026-11-01)
Red: "这些录屏可能会被scale和fit…一个分体式设计，上面的部分是一个正儿八经的视频，下面是多出来一个bar用来显示input visualization，这样我在设计ppt的时候可以更有效的利用空间，根据当前有没有input，空间够不够多来决定是否需要这个部分".
One recording gives three files: the clean 16:9 video (never changes), the bar alone (`--part bar`), and the two joined (`--part joined`). A page without room uses the clean video.

Why a single burned file fails (`sheets/legibility_matrix.jpg`, measured through H.264 CRF 20): a band burned at the 1176 slot and scaled down has legends of 14.9 px at 876, 9.8 at 576, 9.2 at 540, 7.2 at 426, 4.7 at 276, and row names under 7 px from 576 down. A bar rendered for its own width keeps a 20 px legend at every size.

Sync is the open risk: forum reports say each video in Present mode can need its own click (S13), and no source confirms two videos stay aligned. Not tested here. Until a test passes, a page that uses the bar places the joined file; the joined file is one command for any slot, so the design freedom stays.

## How much can be read, and from where
Community rule of thumb, not a standard: text of 1/50 of screen height is the minimum at 8 screen heights, 1/25 for projection (S12). On a 1080 slide that is 21.6 and 43 px.
| Layer | Size at k 1 | k 1.4 | Reads |
|---|---|---|---|
| Which key is held (a filled box, by position) | 35 px box | 49 px | across a room |
| Key legend, game words (Bayon, cap about 0.71 em) | 20 px, cap 14 | 28 px, cap 20 | near screen or laptop at k 1; mid room at k 1.4 |
| Row names, tag (Plex Mono) | 13 px | 18 px | close reading only |
So at k 1 the bar is for shared screens and the front of a room; the filled box still carries the meaning further back. k 1.4 is for pages meant to be read projected. No option reaches 1/25. 平面视觉 should judge both on a real projector.

Tiers (logical width = slot / k): 700+ tape; 420-699 keys, look, mouse, last word; 326-419 keys, look, mouse; 276-325 keys, look; under 276 no bar. Do not scale a bar in Figma by more than about 15 %.

## What it is
A bar under the footage. It never covers the footage (`SHOT_FORMAT`: evidence is never covered). Red's reference is also "下方".

Three parts, left to right:
1. **Keys:** CTRL, SHIFT, W, E, A, S, D. The defaults of `FrontRoomsInput` (SPRINT, SLOW, MOVE, USE).
2. **Look:** a pad and a mouse. In a first-person game the pointer is locked, so its position means nothing: the dot shows mouse speed and direction with a short trail, from the crosshair's centre dot. The mouse shows LMB, RMB and the wheel (close-up zoom).
3. **Tape** (slots 876 and wider): the last 6 s, NOW at the right. Rows MOVE, GAIT, USE as solid bars of true length. Game responses stand above them as words, each on a rule that drops through the rows: E, then DOOR OPENS, on one vertical line. This is the link Red asked for, and no streamer overlay has it. At smaller slots the last word alone replaces the tape.

## Type and grid (proposal for 平面视觉)
- Sizes are slide pixels, multiplied at render by source width / slot width. Placed at its slot, the band's type is true deck size, however large the source. This answers Codex's HUD rule (an 18 px label in a 1920 source is about 5 px in a 540 slot).
- Faces and roles as derived systems keep them: Bayon 20 for key legends and game words (P1 Label); IBM Plex Mono 13 for row names and the source tag (P1 Meta). Nothing smaller. The deck's rule "small mono only in the running header" gets one exception here, for 平面视觉 to accept or replace.
- Keys 35 on a 40 pitch, from the settings key map (`FrontRoomsKeyMapView`, Figma 3688:7842 lane KEY MAP), so the deck and the game draw one keyboard.
- Grid at the 1176 slot (8 columns of the 12-col / 72 margin / 24 gutter deck): keys and look on columns 1-2 (x 0-326), tape from x 350, right edge on the slot edge. Heights: 96 band (13 + 75 + 8), footage 662 at 1176 wide, total 758, which fits under a title at y 232 (ends at 990). At the 576 slot: 84 band, 324 footage, total 408.
- Keylines 1 slide px, solid colours only, no translucency, hard on and off (平面视觉's 10-07 ruling for UI). Rectilinear, as the print forms: pressed keys read like filled boxes on the answer sheet.
- Source tag, the Info status grammar: LIVE ink fill (a person played), SCRIPT outline (a scripted player drove the real controller), SAMPLE outline (test only).

## Accent colour (Red, 2026-11-01)
Red: "目前的accent颜色为什么这么浅呢？和我们的品牌色其实不符". On screen the accent is the brand yellow **#F4DF3B**, the game's own (stamina dashes, key HUD, touch controls, deck chips). #FCF7CE is the canary print paper (the brand yellow at 25 %, Red 10-15, print only) and is not used in the band. Real-input options re-rendered in #F4DF3B: `W/input_band/real/doors/input_bar_options_real.jpg` (pale versions kept as `*_canary`). Scheme on the real clip: 1 white bar (pressed = ink, game answers = yellow), 2 ink bar (pressed = white), 3 ink bar with yellow keys (input and answers both yellow).

## Options on the sheet (first pass, sample data, superseded colours)
| | Pressed | Paper | For | Against |
|---|---|---|---|---|
| A REC | solid ink | white, one with the slide | reads from the back of a room; survives H.264 at a small slot | heaviest |
| B | yellow | white | exactly the key map's focus colour | #FCF7CE on white is faint when projected |
| C | solid ink | canary #FCF7CE | reads as a paper strip, ties to the forms | a visible box on the slide, more colour than the deck uses |
Rejected before the sheet: corner overlay (covers evidence, fights the game's own HUD), side rail (takes width from footage in a 16:9 slide), a separate transparent video (no sync in Slides, alpha not verified).

## Recording rule (in `Documentation/Course/video_delivery_policy.json`, key `inputBand`)
- Every new gameplay recording writes an input log with its frames. Old recordings have no log and get no band: input is never invented.
- Capture point: `FrontRoomsInput.ReadFrame()` once per frame, in a clone-only harness. It is the one boundary for keyboard, mouse, pad, touch and the Editor's `InjectSnapshot` (how BodyDoorBot steers the real controller), so no game code changes and the three platforms share a log format. Game words come from the same harness (for example the result of `UseDoor`). Frames go straight to the encoder, no PNG sequence (policy `storage`).
- OS-level key logging is not used: it needs Input Monitoring (a security setting, Red's) and records everything typed on the machine.
- Touch clips: the game's own stick is already on screen, so the band shows gait and USE only (not built yet).
- Output: H.264 yuv420p BT.709, silent, CRF 20, fit, no upscale. The MP4 is taller than 16:9: slots change to the totals above.

## Not done, not verified
Clone harness not written. No real log yet. Figma playback of a band clip not checked; white seam between band and slide not measured in Figma. Research is one first pass (`10_practice.md`).
