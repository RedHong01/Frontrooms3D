# Fix card: desktop menus (VL217) and the HUD prompt strings (平面视觉, 2026-10-22)

**Red, 2026-10-22, in the 平面视觉 chat:** "全部按推荐". That answers the one message of 09:1x:
1. the prompt strings, incl. C11;
2. keep E·1 and add a playtest item;
3. the VL217 menu fixes;
4. 平面视觉's own HUD key B calls (DECISIONS #22–#28) and the X2 type answers (`research/placard/50_tutorial_placard.md` §11) stand.

**Owners:**
- `FrontRooms3DGame.cs`: 关卡设计.
- the Bayon importer: 游戏视觉.
- phone views: 交互设计.
- KeyHud B: 游戏视觉.

Everything lands **after the Alpha**. Line numbers are main at 2026-10-22 09:3x (Game 88c93b09).

## A. Desktop menus

**A1. The call-to-action lines on the light wash.**
- **The problem:** `ESC  RESUME` (pause, `PauseText()` :1984) and `R  TRY AGAIN` (caught, :2381) are `<color=#F4DF3B>` on the menu wash (.93, .92, .88, .98) = #EDEBE0 at 98 %. That is 1.11 : 1, unreadable.
- **The fix:**
  - Drop the colour tag. The line is ink, in the same face and size (20).
  - The yellow becomes a **marker**: a 4 px wide #F4DF3B bar, as tall as the line's caps (0.70 × 20 = 14 px). Its right edge sits 10 px left of the text's left edge, centred on the caps. It is the same mark as the HUD key panel's yellow rule.
  - Ink on the wash is about 15 : 1.
- **How:** take the CTA out of `overlayText` into its own `Text` ("Menu CTA": same font, size and ink, centred, where the `\n\n` put it). Add the bar as an `Image` sibling at x = −preferredWidth / 2 − 14 (centred text).
- The settings panel's yellow selected row (:1948) sits on its dark panel (#0E0E0D at 97 %) and reads well: **unchanged**.

**A2. A restart question on desktop.**
- In the pause card, the first R changes that line to `R AGAIN TO RESTART` for 2.0 s (unscaled time). A second R inside the window restarts. Otherwise the line goes back to `R  RESTART`.
- The caught screen's `R  TRY AGAIN` needs no question: it is the only action there.
- This matches the touch layer, which already asks.

**A3. The mouse in menus.** The cursor is already free outside play (:2353), but there is no EventSystem.
- Add an `EventSystem` (the project's input module) and a `GraphicRaycaster` on the menu canvas.
- Each action becomes its own line:
  - pause: `ESC  RESUME`, `R  RESTART`, `O  SETTINGS`, stacked, Bayon 20 ink;
  - caught: `R  TRY AGAIN`.
  - The T5 controls legend (Plex Mono 13 caps, `50_tutorial_placard.md` §11) stays above them.
- **Hover** moves A1's yellow bar to that line (RESUME has it by default). **Click** does what the key does (RESTART still asks, A2).
- **Settings panel:** hover selects the row (`settingsIndex`), click = Enter.
- Each line gets a transparent hit `Image` (raycast target) the size of the line + 8 px. The keys keep working.

**A4. Bayon's missing glyphs.**
- **"·":** set Bayon's importer fallback to IBM Plex Mono Regular (`Assets/Resources/Fonts/Bayon-Regular.ttf`). This fixes every `E  ·  …` prompt at once. It is an asset change, so **游戏视觉 lands it**. Legacy `Text` can't mix faces inline, so never split a string for the dot.
- **Digits:** Bayon draws 1 and 0 as I and O, so no digits in Bayon. The A panel's `KeyLabel` (:2574, now `LEVEL 0 KEY` → "LEVEL O KEY") reads **`STAIR KEY`** until KeyHud B, which puts the floor token on the tag in Courier Prime.

**A5. The assist readout's state line.** `threatStateText` (:2105) is a 34 px box with vertical truncate, but Bayon 20's line is 36 px, so `RELAY  /  CHASE` never shows. Make the box 40 px tall (or VerticalWrapMode.Overflow). It is off by default (`FrontRoomsSettings.RelayReadout`).

**A6. The `Flash()` card.** `contextText` (:2117) loses its dark card on desktop: paper text with the 1 px #141414 keyline at 85 % (Unity `Outline`), in the same slot. The CalmHint itself goes with tutorial M1. The phone card is 交互设计's (X2).

## B. HUD prompt strings (decided)

All strings go in one constant (`FrontRoomsKeyHud.Strings`, DECISIONS #7/#11/#29; the map's prompt table). The two spaces round "·" follow today's grammar (`E  ·  OPEN DOOR`). Never RUN, HIDE or "relay" (RELAY §9.4).

| State | Desktop (crosshair prompt) | Phone USE verb |
|---|---|---|
| A key lying in the world | `E  ·  TAKE KEY` (already in main) | TAKE |
| The first key, during the EXAMINE hold | `E  ·  EXAMINE KEY` | EXAMINE |
| A locked door, no key | `LOCKED` / line 2 `NEEDS STAIR KEY` | LOCKED |
| A locked door, key in hand | `E  ·  UNLOCK` / line 2 `STAIR KEY` | UNLOCK |
| A broken door, aimed (C11) | `BROKEN` | BROKEN |
| A broken door, shut tried (C11) | line 2 `WON'T SHUT` during the 0.55 s reaction | BROKEN |
| The evacuation plan | `E  ·  READ PLAN` | READ |
| A READ terminal (C19) | `E  ·  READ SCREEN`; **never on ambient desk monitors** (AT-T19) | READ |
| A sign, including the stair plates | `E  ·  READ SIGN` | READ |

- **The HUD key panel** (KeyHud B): `STAIR KEY`; the tag shows the floor token (`00`, `B1`, `B2` …; 叙事's scheme, pending Red in 叙事's chat); the meta line reads `OPENS THE STAIR DOOR`.
  - Two characters up to B9. B10 and deeper scale uniformly to the tag insert's width, never stretched.
- **The map's one-line prompt until KeyHud B lands** (MapWorld :2537 today reads `LOCKED  ·  NEEDS THIS ZONE'S KEY`):
  - `LOCKED  ·  NEEDS STAIR KEY` once floor keys replace zone keys (tutorial M3);
  - a broken door shows `BROKEN`, then `WON'T SHUT` for 0.55 s on a shut try.
- **叙事 signed this table as written** (2026-10-22 09:4x); its §4 "no string" row was a mistake and is corrected. 叙事's scope notes:
  - `READ SCREEN` is only on READ terminals (C19), never on ambient desk monitors (AT-T19);
  - `READ SIGN` covers the stair plates;
  - the B10+ tag rule is fine.

## C. E·1 stays, and the playtest checks it

- Red kept E·1, the eye that opens from the dot.
- One playtest item tests the Skyrim risk that 信息设计 raised: **"When the crosshair turned into an eye, what did it mean?"** (open answer; code "look / read" vs "being seen").
- Questionnaire content belongs to 系统设计 (FR-PT04) and its layout to 印刷与演示.
- If testers read it as "being seen", the switch is E·3's lowered gaze: parameters only (`LOOK` E·3 in the lab, DECISIONS #17).

## D. Verification

- When each part lands, 平面视觉 checks it against this card in a desktop capture:
  - A1 contrast;
  - A3 hover and click;
  - A4 the "·" and STAIR KEY;
  - the prompts in B.
- The slide goes in the VL log in the Shot format (`SHOT_FORMAT.md`).
