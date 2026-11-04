# Device prompts · 20 · Contracts for the screens other lines own

Line: 交互设计 (account B, `local_0fdc206f`), 2026-11-03 20:3x. Design: `10_research.md` §3 (D1-D8). Red, 交互设计 chat,
11-01 18:5x: "如果要是玩家已经使用手柄进行游戏的话，下面的这些iconography应该变成手柄套系，并且在按下按钮的时候应该对应inconography也会有对应的映射，这样玩家在进行菜单设置的时候就能一直有一个恒定用来选择/回退的按钮套系。对于键盘也是一样的".

Landed by 交互设计 (LANDINGS 2026-11-03 20:13, 20:24, 20:31): the input core (D2, D6), `FrontRoomsPromptBar`, the desktop
settings sheet, the touch settings sheet. What is left lives in files of 游戏视觉 and 关卡设计; this file is the contract.
Nothing here is urgent for the playtest: every screen still works and names its controls in words.

## 1. The API (`Assets/Scripts/Input/FrontRoomsPromptBar.cs`)

- `FrontRoomsPromptBar.Create(RectTransform parent, Font mono, Color fore, Color back, float scale = 1f)`: a row of glyph +
  verb chips under `parent`, empty until `Set`. `fore` draws glyphs and words, `back` is the legend colour of a pressed
  glyph: on the dark sheets pass (paper, ink); on a paper card pass (ink, paper). `scale` 1 = the desktop sheet at 1080
  (24 px band, Plex Mono 13); the touch sheet uses 11/13.
- `Root` (pivot: left end of its centre line; anchors (0,0), set your own), `Group` (CanvasGroup for your fades), `Width`,
  `MaxWidth` (extras past it are dropped, never the first two), `RightToLeft` (anchored at its right end, the pair nearest
  the anchor).
- `Set(params Prompt[])` / `Set(IList<Prompt>)`: cheap when nothing changed, so call it every frame. `Tick()` every frame
  while shown: it follows the device in hand (keys, Xbox, PlayStation, Nintendo; a rebind) and fills a chip while its
  button is held (1 px drop, none under Reduce Motion). Your screen still acts on the press itself.
- `Hold(int chip, float amount)`: a ring round chip `chip`'s glyph, 0 to 1 (D4's HOLD USE · BREAK GLASS ring). No screen
  calls it: the HUD keeps the yellow hold bar (§2 G8).
- `Hud = true`: every part of every chip at full strength (edge, legend, marks, d-pad arms, words), for 游戏视觉's HUD USE
  line over the lit world (G8 step 2, with `Keyline` on); wins over `Solid` (.85 edge). A press looks as always, its paper
  fill full already; at rest a glyph's shape stays unfilled, as on every bar, so a filled shape still means a press. A
  `live` false chip still draws at 40 %: its button does nothing here.
- `Prompt(Role role, string verb, bool live = true)`: `verb` is a table key; "{0}  VERB" puts the glyph where {0} is,
  a plain key ("CLOSER") puts it before the word, null draws the glyph alone. `live` false = 40 % (the button does nothing
  here). Roles: Confirm, Back, Reset, Extra, Defaults, Tabs, TabPrev, TabNext, Choose, Change, ChooseAll, Menu, Restart,
  Text, and `Prompt.Bound(Bind key, Bind pad, verb)`: a gameplay action's control as the player bound it, on the keys or
  on a pad (keycap, disc, ✕ ○ □ △, pill, trigger, d-pad, MENU).
- Menu roles by pad: `FrontRoomsInput.ConfirmButton / BackButton / ResetButton / ExtraButton` (a Nintendo pad confirms on
  its A, east). Gameplay keeps its bindings by place.
- Keys a screen passes go through `FrontRoomsLanguage.T` inside the bar: list any new one in
  `Tools/localization/passed_keys.txt` (the "{0}  VERB" rows the bar uses are there).

## 2. 游戏视觉 (Rendering/UI, Rendering/Inspect)

**G8 · the HUD's USE prompt** (`FrontRoomsKeyHud.WithUseControl`, ~l.1436). Today a desktop player on a pad reads the key
("E  ·  UNLOCK"), and a handheld pad reads `PadName(South)` whatever USE is bound to.
1. Words (smallest change): on the desktop with `FrontRoomsInput.PadInUse`, and on a handheld in Gamepad mode, put
   `FrontRoomsInput.ControlName(FrontRoomsInput.Bind.PadUse)` in place of the leading "E" (follows a rebind and the pad's
   own names: A, CROSS, a Nintendo pad's B).
2. Glyph (D1): draw the control as a chip and keep your verb: a bar with `Prompt.Bound(Bind.Use, Bind.PadUse, null)` (glyph
   only) before your "·  UNLOCK" text, or the whole line as `Prompt.Bound(Bind.Use, Bind.PadUse, "{0}  USE")`-style keys
   from your table. HOLD USE · BREAK GLASS: no `Hold` ring on the HUD: the yellow hold bar stays the only progress
   (游戏视觉, 11-04). Over the lit world: `bar.Keyline = true` (the HUD's keyline round every glyph and word) and
   `bar.Hud = true` (every part at full strength, §1).

**Close-up hints** (`FrontRoomsInspectCatalog.HintBack` "E  ·  BACK", `HintCloser` "SCROLL  ·  CLOSER", touch variants;
the handheld pad words come from `FrontRooms3DGame.Mobile.cs` ~l.108, 交互设计's, which hands you
`ControlName(Bind.PadBack) + "  ·  " + T("BACK")`). With a pad or the keys in use, the hint line can be chips:
`Prompt.Bound(Bind.Use, Bind.PadBack, "BACK")` (the keys' E leaves a close-up, a pad's B), `Prompt.Bound(Bind.Use,
Bind.PadZoom, "CLOSER")` only on a pad (the mouse wheel has no glyph: keep "SCROLL  ·  CLOSER" in words on the keys).
Red's 10-30 "Keep it hidden" stands: no BACK in the full view.

## 3. 关卡设计 (`FrontRooms3DGame.cs`)

- **Caught card** (`LayoutDesktopMenu`, `menuLines[0].text = FrontRoomsInput.TryAgainLine()`): keep the Bayon 20 "TRY
  AGAIN" and put a glyph-only chip before it, ink on the paper card: `retry = FrontRoomsPromptBar.Create(overlay.transform
  as RectTransform, monoFont, ink, paper)`; each frame the card shows, `retry.Set(new Prompt(FrontRoomsInput.PadInUse ?
  Role.Confirm : Role.Restart, null)); retry.Tick();` and place `retry.Root` left of the word (its `Width` + 10). The
  retry itself is unchanged (`ReadPadMenu().Confirm`, or the END RUN key). Until then `TryAgainLine()` names the right
  button in words (a Nintendo pad's A since 20:13).
- **Desktop pause card**: not shown on the desktop (`PauseIsSettings`), so nothing to do.
- **Title**: the desktop title has the controls legend (`FrontRoomsInput.ControlsLegend()`, 交互设计's words) and no menu
  prompt; nothing to do for D.
- **Old settings text** (~l.3518, "↑ ↓  CHOOSE    ← →  CHANGE    ESC  CLOSE"): if `displaySettingsText` can still be
  reached, the same bar in its place (Choose, Change, Back "{0}  BACK"); if not, it can go.

## 4. Open for Red (not built)

- D9: the brand words XBOX / PLAYSTATION CONTROLLER (glyph shapes are drawn either way).
- A Nintendo pad's gameplay USE is its B (by place, as Unity reads it); letter defaults (USE on A) would be a binding
  change per family. The touch title says PRESS B TO START on a Nintendo pad for the same reason.
