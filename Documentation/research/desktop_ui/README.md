# Desktop UI: every Mac and Windows screen as a Figma master (平面视觉, 2026-10-08)

Red, 2026-10-08: "当前mac/windows 的UI呢？我也需要你在figma里把所有 frame的 mockup也都给我做出来".

This is the desktop counterpart of TOUCH FLOWS (2889:6157): every screen the desktop game draws today (main `bc0aa79`), rebuilt in Figma at 1920 × 1080 as editable layers.

## Where it is (Figma `0tCbAiVUlrPId3RWd9LRif`, page 2099:76)

**FRONTROOMS · DESKTOP UI · SCREENS · MAC + WINDOWS · 1920×1080 (平面视觉)**, section `2911:6716`, at x 103377, y 2000.
- Row 0 has the kit and three full-screen menu components:
  - `DK · Desktop kit masters` (`2911:6720`): the components `HUD·D / Room block`, `Threat block`, `Crosshair`, `Prompt`, `Hold bar`, `Stamina`, `Key row`, `Hint card` and `Captions`, plus `Brand / FrontRooms logo · desktop title` (the game's own `FrontRoomsLogo_White.svg` at 0.75);
  - `MENU·D / Pause screen` (`2913:6592`);
  - `MENU·D / Caught screen` (`2913:6598`);
  - `MENU·D / Settings screen` (`2913:6603`).
- Rows 1–5 hold the 18 screen masters, `SCR / Desktop / D01 … D17` (components). Each one is the captured scene with kit instances on top, carrying that screen's exact strings.

**FRONTROOMS · DESKTOP FLOWS · EVERY SCREEN · MAC + WINDOWS**, section `2920:6693`, at x 94697, y 2000, next to TOUCH FLOWS.
- It lays out the same screens at 50 % in five lanes: START, USE, KEY + ASSISTS, MENUS, END.
- The key is written on each arrow, and each screen has its way back underneath.
- A short block lists what was found on desktop.

## The screens

| # | Screen | In | Out |
|---|---|---|---|
| D01 | Title (corridor stream + logo; no "press" text on desktop) | launch | Space / Enter |
| D02 | Lobby · first 9 s (hint card: "Shift to sprint. About 5 seconds, and it hears every step.") | from the title | through the first door |
| D03 | Calm (room meta + room name + dot) | — | Esc pauses |
| D04 | Sprint (5 × 24 px stamina segments, lit #F4DF3B) | hold Shift | let go |
| D05 | Winded (all segments dim; no text on desktop) | ≈ 5 s of sprint | rest 1 s, refill 1 s per s |
| D06 | Door in reach `E  ·  OPEN DOOR` | the dot on a door ≤ 2.4 m | look away |
| D07 | Door open `E  ·  SHUT DOOR` | E | E again |
| D08 | Locked `LOCKED  ·  NEEDS THIS ZONE'S KEY` | only with `doorsNeedKeys` on (off today) | — |
| D09 | Glass in reach `HOLD E  ·  BREAK GLASS` | the dot on a pane ≤ 1.2 m | look away |
| D10 | Hold E (120 × 4 bar, fill 120 × progress) | hold E | let go before 1 s. In play, the glass shot fades this HUD within 0.15 s |
| D11 | Glass broken (hint-card flash, 3 s) | the pane gives | walk into the frame |
| D12 | Key held (key row, bottom-left (72, 118)) | walk over a key (0.9 m) | leave the zone |
| D13 | Captions (assist; up to 3 lines) | Settings ▸ CAPTIONS ON | OFF |
| D14 | Relay readout (assist; the distance only, see the findings) | Settings ▸ RELAY READOUT ON, Relay out | OFF |
| D15 | Pause (#EDEBE0 wash + Bayon text) | Esc, or the window loses focus | Esc ▸ play · R ▸ restart at once · O ▸ settings |
| D16 / D16b | Settings (dark card; ↑ ↓ choose, ← → change) | O | Esc or O ▸ pause |
| D17 | Caught (stats line; R  TRY AGAIN) | the Relay catches you (hard cut) | R ▸ a new run, straight into play (D02) |

## How it was made

### 1. Capture

The capture ran in clone `~/FrontRoomsVisualWork/proj_deskui` (made with `mkclone.sh`, main `bc0aa79`).

- **Harness:** `Assets/Editor/DesktopUI/FrontRoomsDesktopUICapture.cs` and `.States.cs`, plus `DeskUIBlendOver.shader`.
- **Driving the game:** each state is set through the game's own fields and calls, with `UpdateHud()` run right before the render. These include `stamina`, `winded`, `holdProgress`, `Flash`, `map.Use`, `map.Hold`, `ToggleDisplaySettings`, `End`, `keysHeld` and `FrontRoomsCaptions.Post`.
- **Settings stay off Red's machine:** `RunBatchIsolated` first gives the clone its own product name, so the assist toggles never touch Red's PlayerPrefs.
- **Outputs per screen:**
  - `_scene.png`: the camera alone, which becomes the Figma background;
  - `_ui.png`: every overlay canvas, moved onto a post-free camera, plus the UI Toolkit logo panel (pointed at a texture) over transparent black;
  - `_rects.txt`: every visible Text and Image, with box, string, font, size and colour.
- **Composite:** `~/FrontRoomsVisualWork/deskui/desktop_ui_composite.py` composites scene + UI in linear light, as the screen does.
- **Batch-import fix:** the clone's batch SVG import leaves the logo's S1/S2 gradient atlas uninitialised (0xCD), so the harness rebuilds it from the SVG stops. The real project's import is correct.

To re-capture after a UI change:
- run `mkclone.sh` (or rsync main into `proj_deskui`);
- then run `Unity -batchmode -projectPath ~/FrontRoomsVisualWork/proj_deskui -buildTarget OSXUniversal -executeMethod FrontRoomsDesktopUICapture.RunBatchIsolated -logFile <f>` (no `-quit`);
- output goes to `proj_deskui/Verification/desktop_ui/`.

### 2. Figma build

The numbers come from the game's code (inventory of `FrontRooms3DGame.cs`) and are checked against the rects dumps.
- **Faces:**
  - Bayon and IBM Plex Mono, as in the game.
  - The game's Source Serif 4 is a variable font drawn at its default optical size 20. Figma's Source Serif 4 has no optical-size axis (only `wght`) and draws the display cut, which is 9–10 % narrow at 50 px. These masters therefore use **Source Serif Pro**, the static text cut, which is within 1–2 % of the game's widths.
- **Whole-pixel advances:** Unity rounds glyph advances to whole pixels. Tracking reproduces it:
  - Plex Mono 13: +0.2 px;
  - the room name: +0.5 px;
  - the hint: +0.1 px;
  - the all-caps glass flash: +0.33 px;
  - the Bayon 13 pause controls: −0.08 px.
- **Opacity:** the game blends UI in linear light and Figma in sRGB. Each translucent layer carries the Figma value that looks like the game over these scenes, with the game value in the layer name:

  | Element | Game | Figma |
  |---|---|---|
  | Hint card | 90 % | 77 % |
  | Captions card | 75 % | 58 % |
  | White tracks and dim stamina | 25 % | 37 % |
  | Settings card | 97 % | 85 % |
  | Wash | 98 % | 99 % |

- **Synthetic bold** (`<b>`, which Unity fakes): a stroke plus tracking, 1.2 + 0.9 px at 88 px and 0.6 + 0 px at 30 px.
- **Menu lines:** the game spaces mixed-size rich-text lines differently from a plain line-height model, so each menu line sits at the game's measured cap top.
- **The prompt's "·":** Bayon has no U+00B7, and the game draws it from the system font. IBM Plex Mono's square dot stands in, with 3 px less space each side, which keeps the game's width exactly (876–1044).

## Verification

Every master was rendered from Figma at 1920 × 1080 and compared with the game composite. Results:
- **Text lines:** tops match within 0–1 px; widths within ±3 px. The exception is the room name at −9 px, which is the optical-size difference above.
- **Prompt:** identical extent, 876–1044.
- **Whole frames:** mean |Δ| 1.4–2.7 / 255, p99 ≤ 10 / 255, and under 0.9 % of pixels differ by more than 16.

Verification slides:
- **VL216** (`2922:6863`): game vs Figma for the room block, prompt + hold bar, hint card, key row, captions, logo, pause, settings and caught.
- **VL217** (`2922:6873`): the findings.

Tools: `~/FrontRoomsVisualWork/deskui/compare.py`, `bands.py`. Renders are in `deskui/verify/final/`.

## Found on desktop (not fixed; Red's call, code in `FrontRooms3DGame.cs`)

1. `ESC  RESUME` (pause) and `R  TRY AGAIN` (caught) are #F4DF3B on the #EDEBE0 98 % wash. The measured contrast is **1.11 : 1**, so they are unreadable.
2. The relay readout's state line (`RELAY  /  CHASE`, Bayon 20 bold) never shows. Bayon's line is 36 px tall and its box is 34 px with vertical Truncate, so only `RELAY  216 M` appears.
3. Bayon has no "·", so the prompt dot comes from the system fallback font.
4. Bayon draws 0 as O: `LEVEL O / THE MAZE KEY`.
5. There is no restart question on desktop (touch asks). R in pause restarts at once.
6. Menus take no mouse, because there is no EventSystem.
7. LOCKED cannot be reached today: `doorsNeedKeys` is off in `FrontRoomsLevel0.asset`.

## Images

- `images/desktop_ui_hud_game_vs_figma.jpg`, `images/desktop_ui_menus_game_vs_figma.jpg` (VL216)
- `images/desktop_ui_findings.jpg` (VL217)
