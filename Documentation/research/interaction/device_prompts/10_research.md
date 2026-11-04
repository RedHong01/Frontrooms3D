# Device prompts · 10 · How AAA games make prompts follow the device in hand, and what FrontRooms takes from it

Line: 交互设计 (account F, `local_cffaf37e`), 2026-11-01 evening. Sources: `SOURCES.md` beside this file (S1–S16).
Brief: `Documentation/Takeover_2026-10-18/briefs/touch.md`, 总控's note of 11-01 19:5x.

Red's ask, 交互设计 chat, 11-01 19:51 (verbatim): "我需要你research一般的3A是怎么做这部分的工作，然后把他们的逻辑和对于这部分需求的处理方案融会贯通进来".
"这部分" is Red's 18:5x ask: with a pad in use, the menu iconography becomes the pad set, and the icon answers when its
button is pressed, so select and back stay one constant pair in every menu. The same holds for a keyboard
(`handoff/interaction.md` §5 item 0).

## 0. Short answer

The platforms and engines all point the same way. There are eight rules, and FrontRooms already meets three of them
(R4, R7, R8: the device package landed 11-01 21:13). The device-glyph package (D) takes on the other five:

1. **Glyphs, not words.** Prompts show the button's own shape, drawn for the pad family in hand: Xbox, PlayStation or
   Nintendo, with a fallback (R3). Today FrontRooms spells the buttons out ("A", "CROSS", "L1").
2. **Switching ignores noise.** The switch is instant toward the device just pressed. Mouse movement alone never takes the
   prompts away from a pad (R2). Today a 2 px mouse twitch flips every prompt to the keys, so a Steam Deck trackpad would
   fail Valve's Deck rule S1.
3. **One prompt bar.** Every menu shows its actions in one place, in one order, as glyph + verb chips, with confirm and back
   always the same pair (R5).
4. **The chip answers the press.** A press depresses its chip. A hold action fills a ring around the glyph (R6).
5. **Nintendo layout.** On a Nintendo pad, confirm is the right button (A) and back the bottom one (B) (R4).

## 1. What the platforms and engines say

**R1 · Prompts follow the device being used, not the devices that are connected.**
- Apple's HIG says that with several controllers connected, the labels and glyphs should match the one the player is
  actively using (S3).
- Google's ChromeOS game guide says to track the "active input" from the input events the game actually receives, and not
  from connection state (S5).
- Unity's PlayerInput switches control scheme when a device sends input on a control that is neither noisy nor synthetic
  (S12).
- Valve's Steam Deck review requires on-screen glyphs to match the inputs in use (S1).

**R2 · Switch at once toward the device pressed; never switch on noise.**
- ChromeOS gives the rule: rank the input types, move up in priority at once, and move down only after a delay and only if
  the lower device is really being used, so the UI does not flip back and forth (S5).
- Valve fails a Deck game that shows non-controller glyphs while the player uses the Deck's controls in their default
  configuration (S1). The Deck's trackpads drive the mouse in many configurations.
- Unity leaves noisy controls (sensors, drift) out of device switching by design (S12).
- Player reports name mouse movement as the usual cause of flicker. Some recent games (reported: Fortnite, Ubisoft titles)
  offer a setting that locks prompts to one family (S14; community sources, treat them as anecdote).

**R3 · Draw the family's own glyphs, and fall back gracefully.**
- Apple: use the connected controller's own labelling. Symbols beat text, because a newcomer does not have to hunt for a
  label (S3). `GCControllerElement.sfSymbolsName` gives each button's symbol, the remapped one when the player has
  remapped it (S4).
- Valve shows Xbox, PlayStation and Nintendo art per device and falls back to Steam's art for an unknown pad (S6). Steam's
  glyph API has three base styles (knockout, light, dark), plus modifiers that drop the ABXY colours for the base colour or
  give them a solid fill (S7).
- Unreal CommonUI (Epic's UI framework from Fortnite) keeps a per-platform table from each key to a brush. On Windows it
  holds both a keyboard set and a gamepad set, so the icons change when a pad is used (S9).

**R4 · Confirm and back are one fixed pair per family.**
- PlayStation 5 made ✕ the confirm button in every region, Japan included, where ○ had confirmed before (S10, reported at
  launch, 2020).
- Nintendo pads put A on the right and B at the bottom. SDL reports the face buttons either by position or by label,
  because the two disagree with Xbox (S11). On a Nintendo pad, confirm is A (right) and back is B (bottom).
- Menus keep their own buttons whatever the gameplay bindings are; only gameplay actions are remapped (FrontRooms already
  works this way: `FrontRoomsKeyMapView.Pad.cs`, BACK's note).

**R5 · One prompt bar per screen.**
- CommonUI's Bound Action Bar lists the actions available on the current screen. Each entry is a glyph and a verb, read
  from the screen's own buttons, and a button can opt out of the bar (S8, S9).
- The bar is the one place a player learns the screen's controls. Its entries come and go with focus, so it never lists an
  action that would do nothing.

**R6 · The prompt answers the press.**
- CommonUI's action widget draws hold progress on the glyph: a material reads the held fraction from 0 to 1 (S8). Its bound
  buttons carry separate press-and-hold settings for keyboard and mouse and for gamepad and touch (S8).
- So a press shows on its chip, and a hold shows its progress on the glyph itself, with no separate meter.

**R7 · Prompts follow remaps.**
- Xbox Accessibility Guideline 107: remapped bindings must update hints, tips, tutorials and controller diagrams. Games
  must offer in-game remapping of all controls, whatever the platform remaps (S13).
- Apple's `sfSymbolsName` already returns the remapped symbol (S4). IGDA's accessibility SIG asks for the same of
  system-level remaps (S15).

**R8 · A lost controller never costs the player anything.**
- Xbox certification XR-115 rates these as Critical failures: returning to the main menu when the controller is powered
  off in play, refusing input from a second controller after the first loses power, and not letting play resume after a
  new controller is synced (S2).
- Valve: the default controller configuration must reach all content, with no setting to change first (S1).

**Legibility.** Valve's Deck floor is 9 px at 1280 × 800, with 12 px recommended (S1). FrontRooms' smallest menu text is
Plex Mono 13 at 1080p, about 10 px at 800 rows, so it passes.

## 2. FrontRooms against the rules (main after 11-01 21:13)

| Rule | Today | Gap |
|---|---|---|
| R1 follow the device used | `FrontRoomsInput.DeviceInUse` follows input, not connection | none |
| R2 no noise | the pad takes over on stick, trigger or button; the keys take over on a key, a click, a scroll, **or any mouse move over 2 px a frame** | a bumped mouse or a Deck trackpad flips every prompt to keys |
| R3 family glyphs | words: `A`, `CROSS`, `LB`, `L1` (`PadName`); families Xbox, PlayStation, Generic | no glyphs; no Nintendo family |
| R4 fixed pair | menus: A/CROSS confirm, B/CIRCLE back, Enter/Esc on keys | a Nintendo pad gets Xbox positions under Xbox names |
| R5 one bar | desktop: a bottom-left hint line in words, plus RESUME and END RUN keyline buttons with no glyph; touch: legend rows | no glyph + verb chips; confirm and back are not one constant pair on screen |
| R6 press answers | none | — |
| R7 follow remaps | prompts read `PathOf(bind)`; the key map shows bound keys | none |
| R8 loss costs nothing | the run pauses when the pad in use goes (21:13); any pad's press takes over | none |

## 3. What the D package builds (design; implementation follows in the same line)

**D1 · Glyph chips.** One drawn set, in the key map's keycap and pad-button style, monochrome paper on the sheet's ink. The
idea is Steam's knockout style with neutral ABXY (S7): colour is never the only cue, and the palette stays the game's.
- Keyboard: a keycap with the key's name in Plex Mono (ENTER, ESC, Q, E, R, SPACE, ← → ↑ ↓).
- Xbox: a disc with A, B, X or Y; pills for LB and RB; trigger shapes for LT and RT; View and Menu as small glyphs.
- PlayStation: a disc with ✕ ○ □ △ drawn as strokes, not type, so every face draws them; pills for L1 and R1; triggers
  for L2 and R2; Create and Options as small glyphs.
- Nintendo: discs with A, B, X or Y at Nintendo's positions; pills for L and R; triggers for ZL and ZR.
- Generic: the Xbox shapes. Valve accepts Xbox glyphs for any pad (S1).

**D2 · Switching without noise.**
- A key, a click, a scroll or a pad button switches at once.
- Mouse movement alone moves the prompts off a pad only when the mouse really travels: about 40 px within 0.25 s (to be
  tuned).
- A stick or trigger switches to the pad only past its deadzone, as today.
- Result: a Deck trackpad, a bumped desk or a resting hand never repaints the prompts. Rules R2 and S1.

**D3 · One bar, one constant pair.**
- Every menu screen (settings sheet, map pages, close-up, title, pause, caught card, and the touch sheet with a pad or
  keyboard in hand) ends in one row of chips, bottom right, in a fixed order: confirm, then back, then that screen's
  extras (Q/E or LB/RB tabs, Backspace or X default, Space or Y preview).
- Confirm and back are always the first two, with the same glyphs, in the same place.
- With touch only, the bar is not drawn: the touch sheet keeps its own controls. Rules R4 and R5.
- As built (2026-11-03, `20_contracts.md`): on the desktop sheet the bar takes the hint line's place, bottom-left, pair
  first, because the bottom-right corner holds RESUME / END RUN; left-anchored, the pair's glyphs keep their place. On
  touch it sits in a 44 pt band at the sheet's foot while a pad or a keyboard is in use. A right-anchored bar puts the pair
  nearest its anchor (extras, confirm, back).

**D4 · The chip answers.**
- On press, the chip fills: its glyph goes solid paper with an ink legend and drops 1 px. The action fires on the same
  press, so nothing waits for the animation. The chip lets go on release, or after 0.12 s.
- A hold action fills a ring around the glyph as it is held, in the HOLD USE · BREAK GLASS manner.
- Reduce Motion keeps the fill and drops the 1 px move. Rule R6.

**D5 · Remaps.** Gameplay chips read the bound control; the menu pair is fixed and never remapped. Rule R7, already met.

**D6 · Nintendo.** A Switch Pro pad or Joy-Cons in hand: confirm is A (east), back B (south); X and Y swap the same way.
Check Unity's Switch Pro layout on Mac before landing: the device package found it reports buttonSouth as "B". Rule R4.

**D7 · Japanese PlayStation.** ✕ confirms everywhere, as on PS5 (S10). There is no ○-confirm option.

**D8 · What stays as it is.** Disconnect handling (R8), prompts following remaps (R7), and the brand words in text (D9 of
the device package, Red's decision: XBOX and PLAYSTATION in words, glyph shapes regardless).

## 4. Considered and not taken now

- **A setting that locks prompts to one family** (reported in some recent games, S14). D2's noise rule removes the reason
  for it. If a player still sees flicker in the combined test, it is a 20-minute addition to CONTROLS.
- **Coloured ABXY.** It clashes with the paper and ink palette and adds nothing, because the glyph already carries the letter.
- **Steam's runtime glyphs (S6, S7).** They need Steamworks in the build, which FrontRooms does not ship. Our own drawn set
  covers the families Valve lists.
