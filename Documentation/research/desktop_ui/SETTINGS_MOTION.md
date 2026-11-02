# Settings: a chosen row opens in place

平面视觉, 2026-11-01. **Approved for build** (Red, 平面视觉 chat, after revision 2: "Yes, hand it over"); handed to 交互设计 and 设置菜单与画质预设. Nothing in the game has changed yet. Mock and renders: `~/FrontRoomsVisualWork/settings_motion/` (`mock.py`, `graph.py`, `lang.py`, `render_all.sh`). Videos: `video/settings_motion_desktop.mp4` and `video/settings_motion_phone.mp4` (60 fps, silent). Sheet: `images/settings_motion_storyboard.jpg`.

## Red's asks (平面视觉 chat, 2026-11-01)

- 19:4x: "同时我认为在玩家选择特定栏目的时候，这个栏目应该直接展开变成两倍的大小（我需要你思考这部分的motion transition，以及information hierarchy，里面应该显示什么样的信息/detailed information）同时如果是比如说玩家选择了手柄/键盘映射，应该对应的列表向下展开，然后里面的内容物通过motion transition的方式reveal"
- 19:4x: "这样的操作可以保证手机这种小画幅的界面可以每一个栏目给自己争取更多的内容显示空间，并且贯彻向下滑动的逻辑"
- 19:5x, on the 10-29 key map page: "然后目前的这个mapping graph我认为可以再大一些…我不认为在motion transition上，直接让左边的highlight rule到处飞来飞去是一个好的idea…每一个被选中的call out spec如果真的被选中，除了下面的按钮有accent 颜色highlight以外，它自身也会被一个underlayed bar full bleed cover 住，然后上面的type的颜色应该做相应的调整避免视觉上打架。"
- 20:0x: "我认为在 选择语言的这个界面，在右侧的这部分应该可以让玩家就像是之前选择画质的预览窗口这样来看游戏内的语言相关的资产（比如说电视上的graphic，还有文档等）显示的语言的变化。"
- 20:2x, on the mock's controller: "这里应该确保，所有手柄都是同一个graph，这里手柄的样式和旧有的已经不一样了"; on the mouse wheel: "这里不不应该穿透，要让底部的竖线避开中按键".
- 20:3x, on an open row's name: "这里的标题应该适当增大（带有motion transition，然后避免和下方的body copy/ subtitle hierarchy争抢）".
- 20:4x, on the preview: "这里我需要游戏内实际的的"; on the LANGUAGE cells: "这里应该保留之前钥匙的那套逻辑，不要full bleed bar".
- 20:5x, on the map reveal: "这里的transition我不希望是从上往下的那种顶上去的感觉，其次就是我希望它其实是渐隐/渐显的那种".
- Also binding: names beside the flag keychains (18:58, `LANGUAGE_CHARMS.md`); yellow only for focus and choice; motion grows from what is on screen, no dark cards.

## The system: one list, opened downward

1. **A chosen row opens to twice its height, in place.** The row's first line (name, control) does not move; the second line appears under it. Only one row is open; choosing another closes the first as the second opens. Everything below moves down; the list only ever grows downward and scrolls down to keep the open row whole.
2. **A map row unfolds.** KEYBOARD & MOUSE and CONTROLLER (and LANGUAGE) are pickers of many parts, so instead of 2× they unfold to their content's height, under the row, and the list goes on below them. The second-level page is gone.
3. **The explanation moves into the row.** The right column (desktop columns 9–12, the wide phone and iPad sheets) keeps only pictures: the live game on GRAPHICS, the game's own words on LANGUAGE. Tabs with no picture leave it empty. The Duo's outer display already opens explanations under the row; now every screen does.
4. **A choice is a bar under it, not a rule that travels.** An open row keeps a paper underlay (7 %) and its own 4 px yellow bar, which grows and shrinks with that row; no bar ever travels from one row or callout to another. Inside a map, a chosen callout is covered full bleed by a yellow bar, and its type turns ink. LANGUAGE has no bar: it keeps the key (below).
5. **Content fades; it never feeds down or pushes up.** Only a row's height moves; what appears in it fades in, and what leaves fades out.

## What the second line holds (information hierarchy)

Three levels, read left to right and top to bottom: **what it is** (the name, Bayon, which grows as the row opens: desktop 20 → 28 px, phone 17 → 24 pt), **the choice** (the control, unchanged), **what the choice means** (the second line, smaller and lighter: Source Serif at 88 %, Plex Mono at 50–95 %). The grown name is the largest type in the row, so the sentence and the notes never compete with it.

| Row kind | Second line, left | Second line, under the control |
|---|---|---|
| SEGMENTS (QUALITY, SHADOWS …) | what it does, one sentence (Source Serif, 2 lines at most) | under each segment what it means (Plex Mono, the current one at 95 %, the rest at 50 %): QUALITY `30 FPS · 60 FPS · + AO HIGH · FULL RES · YOUR MIX`, FRAME RATE `33 MS · 25 MS · 17 MS · 8 MS`; a short rule under the default; `DEFAULT …` at the right |
| SLIDER (LOOK SPEED, RENDER SCALE) | what it does | the stops' meanings under the track (`0.10° · 0.20° PER PT · 0.30°`) |
| SWITCH (HDR, CAPTIONS) | what it does | what the current state does (`SOUNDS THAT HUNT YOU`) |
| METER (FRAME RATE NOW) | what it measures | a live frame-time line against the target (yellow, 16.7 ms) |
| MAP (KEYBOARD & MOUSE, CONTROLLER) | unfolds: the mapping graph, below | |
| LANGUAGE | unfolds: the ten flag keychains, each name beside its charm, 5 × 2 | the right column previews the game's words |

Notes must fit their segment (Plex Mono 12 under Bayon 18 with 14 px pads on the desktop; measured: LOW 54 px, MEDIUM 76, HIGH 58, ULTRA 67, CUSTOM 77); a note that does not fit is shortened in the strings, in every language, never cut with "…". Desktop QUALITY notes: `70 % · 85 % · 100 % · AS HIGH · YOUR MIX` (ULTRA sets what HIGH sets today, 设置菜单 11-01); BLOOM `BLOCKY · SOFT`.

## Motion (constants in `mock.py`)

| Step | Time | Curve |
|---|---|---|
| Row opens 1× → 2×; its bar leads (it is the row's left edge, so the row grows from it); its name grows 1.4× from its left end, on its own centre line | 0.24 s | ease-out quart |
| Second line: the sentence, then each note under its own segment, left to right, fading in where they stay | from 0.09 s, 0.18 s each, 0.035 s apart | ease-out |
| The row it replaces: second line fades, then 2× → 1× | 0.09 s, then 0.18 s | ease-in-out cubic |
| The list follows down to show the whole open row | 0.32 s | ease-in-out |
| Map row: › turns to ⌄; the row unfolds to the graph's height | 0.26 s + 1 s per 1500 units, at most 0.52 s | ease-out quart |
| Graph reveal, fades only: the keys and the mouse fade in together, then the keys in use fade to paper, then the leaders and the callouts fade in, all in place (Red 20:5x: no top-to-bottom push) | keys 0.10 s for 0.28 s; fills from 0.22 s; leaders from 0.30 s; callouts from 0.36 s, 0.26 s each | ease-out |
| Choosing a callout: its yellow bar fades in under it, full bleed, and its ink type with it; the key and the leader cross-fade to yellow. The callout it leaves fades back in 0.12 s | 0.16 s | ease-out |
| Change a key: the callout's spec reads PRESS A KEY · ESC CANCELS (PRESS A BUTTON · B CANCELS) | at once | |
| Fold: the whole graph fades out, then the row folds | 0.12 s, then 0.22 s | ease-in-out |
| LANGUAGE (the 10-31 key logic, kept): the charms fade in left to right with their names beside them; the one under the pointer swings 4° once and its name brightens, no bar; the preview cross-fades to that language before it is picked; a pick lifts the charm 4, flies it onto the key's bow hole on a 28 arc with a 12° tilt, the key kicks 3° and the charm swings 9° for 1.5 s; the old charm flies home on an 18 arc and settles with 4°; its place keeps a 16 % print while it is on the key | fades 0.035 s apart; preview 0.18 s; lift 0.06 s, flight 0.36 s | ease-out, flight ease-in-out cubic |
| Reduced motion (CAMERA MOTION OFF, iOS Reduce Motion) | heights jump; second lines fade 0.12 s | |

Hover never opens a row (a list must not move under a pointer): on the desktop ↑ ↓ or a click opens; on touch, a tap. Choosing the open row's name again closes it; ESC closes, then goes back.

## The mapping graph

- **Bigger:** the 10-29 page drew a 40 px key pitch in columns 1–8; the unfolded graph draws 58 px (1.45×) across columns 1–12, keyboard, arrows and mouse, about 1,500 px wide. On the phone the controller fills the 710 pt list.
- **One drawing per device, the game's own.** The keyboard and mouse keep the 10-29 page's look (in use paper, free a 25 % keyline, fixed keys in use paper too, locks on their callouts); every controller, Xbox or PlayStation (its own names and ✕ ○ □ △), is the CONTROLLER page's drawing from `FrontRoomsKeyMapView.Pad.cs` (body, wells, triggers, shoulders, sticks, face buttons, the d-pad as keycaps) with its callouts where that page puts them, only scaled.
- **No line crosses a control.** Each leader is one straight segment to its control's middle row or column. On the keyboard the F keys sit on whole units, so the leaders to W, E and R run through the gaps of the F row and the number row; the mouse's middle line stops above the wheel and goes on below it, and the wheel has its own face over the right button.
- **Callouts** keep the 10-29 words and positions (USE, END RUN, MOVE above; SETTINGS, SLOW at the left; SPRINT, START, MOVE arrows below; ZOOM, ZOOM STEP, LOOK at the mouse).
- **Order:** ← → move left to right by the key each callout names (Red: "从左往右"); ↑ ↓ leave the graph for the rows above and below.
- **States:** in use = paper key, free = keyline at 28 %, fixed = keyline at 60 % with a lock on its callout, chosen = yellow key + yellow bar under its callout. The 10-29 legend (IN USE · FREE · CHOSEN) is no longer needed: the bar says chosen.

## LANGUAGE preview

The right column shows the game's real assets in the language under the pointer, before it is picked, as GRAPHICS shows the game at the new settings (Red 20:4x: the real in-game ones). The mock uses in-game frames: the Level 0 evacuation plan at 0.6 m (`research/placard/images/q16_u1_A3_legend_0p6m.jpg`) with its TEXT panel under it in that language (G9: #0E0E0D at 97 %, Plex Mono 13/16 heads, Source Serif 4 20/23 lines, strings from `Resources/Language/<code>.txt`), and the C20 terminal close-up (`research/computer_ui/images/EC01_closeup_desktop.jpg`). **Flags:** (1) the printed placard stays English in every language today, and the translation lives in the TEXT panel; showing the print itself in the language would need per-language print art. (2) The C20 terminal's words (`Resources/Terminal/Content/terminal_*.json`) are not in the string tables at all yet, so the terminal stays English. In the game the preview would be live renders of these assets, not stills.

## Platforms

Same rules on every screen (three-platform parity). Row heights double: desktop 48 → 96 px, phone 44 → 88 pt, tablet its own row height × 2. The phone and the iPad lose the explanation column to the list (it widens from 418 to 710 pt on iPhone 17 Pro); the iPad keeps a picture column for GRAPHICS and LANGUAGE, as the desktop does.

## Owners if Red agrees

- 交互设计: `FrontRoomsDesktopSettingsView.cs`, `FrontRoomsTouchControlsView.cs`, `FrontRoomsKeyMapView.cs` (+ `.Pad.cs`), `FrontRoomsLanguageStrip.cs`: row heights, the second line, the unfold, the graph inside the row, the callout bar.
- 设置菜单与画质预设: `FrontRoomsSettingsMenu.cs`: the note strings per choice, the preview column, the strings for the language preview.
- 叙事设计: every new note and sentence. 平面视觉: type, sizes and this spec; the per-language TV graphic and sheets if Red wants them.
