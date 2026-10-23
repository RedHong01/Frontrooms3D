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

**A7. The CAUGHT screen** (叙事 v1.1 §6; Red, 2026-10-22 12:15, in the 叙事设计 chat: "全部按推荐（Q1–Q7、照片下载、LOBBY 改名）"). Code: desktop :2374–:2381, touch :2183–:2186.
- **Title `CAUGHT`:** unchanged (Bayon 88 bold on desktop, Bayon 48 on touch).
- **Subtitle `OCCUPANT ACCOUNTED FOR`:**
  - **Desktop:** directly under `CAUGHT` in the same `overlayText`, with no blank line between them: `<size=28>OCCUPANT ACCOUNTED FOR</size>`. Same Bayon and the same ink, about one third of the title's size. The blank line (`\n\n`) stays between the subtitle and the stats.
  - **Touch** (corrected 12:5x after 交互设计's check): the HUD copies in `FrontRooms3DGame.cs` (:2183–:2191) are switched off on handhelds. The visible card is the touch layer's `FrontRoomsTouchControlsView` (Figma TS 8 · CAUGHT; master 874 × 402 pt, y down), owned by 交互设计.
    - Subtitle `OCCUPANT ACCOUNTED FOR`: Bayon 17, ink, centred, a 600 × 22 box centred at **y 97**, under CAUGHT (Bayon 48, box 40–88).
    - **Push everything below down 10 pt:** the four numbers to y 135, their labels to y 164, the rule to y 196, the sentence to y 235, and the TRY AGAIN chip by the same 10.
    - That leaves about 10 pt from CAUGHT's caps to the subtitle's caps and about 21 pt from the subtitle to the numbers, so the subtitle reads as part of the title (proximity), not as a fifth row. With y 99 and no push the two gaps were equal (about 12 / 11.5 pt).
    - Motion as 交互设计 proposed: it fades in with the title (+80 ms) and drops 12 pt like it.
    - The phone card has no Plex Mono stats: its numbers are Source Serif 26, unchanged. `TIER REACHED` is replaced by 叙事's string (proposed `KEYS TAKEN`), same style.
- **Stats line, desktop: out of Bayon.**
  - Today it is `<size=24>` inside the Bayon `overlayText`, so every digit misprints: `1 KEYS` reads "I KEYS", `0 DOORS` reads "O DOORS", and a `LEVEL B1` would read "LEVEL BI".
  - Fix: put it in its own `Text` ("Menu stats", where the `\n\n` put it): **IBM Plex Mono 20 caps**, ink, centred, the `  /  ` separators as today.
  - The touch stats already use Plex Mono 17: unchanged.
- **Strings** come from 叙事 v1.1 §6 through one `FloorLabel()`: `LEVEL B2  /  123 S  /  5 ZONES  /  1 KEY  /  0 DOORS BROKEN`, with singular forms for 1.
  - **No TIER:** C04 c (Red, 2026-10-21, 系统设计 chat) moves it to the off-by-default readout. Confirmed by 系统设计 and 叙事, 12:4x.

**A8. The HUD room line.**
- `LEVEL 0 / FRONT ROOMS` (was `THE LOBBY`), `LEVEL B1 / THE MAZE`, … (叙事 v1.1 §6).
- `roomText` is Source Serif 4 (50 desktop, 26 touch), which draws 0 and 1 correctly: **no face change**. Caps as today. The touch card's sentence-case form is `Level 0 / Front Rooms` (交互设计).
- Code: :2487, Mobile :155, and `ZoneName()` :1651–:1658 for the floor token.

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

- **The HUD key panel** (KeyHud B): `STAIR KEY`; the meta line reads `OPENS THE STAIR DOOR`; the tag shows the floor token (`00`, `B1`, `B2` …; C28, decided by Red 12:15 in 叙事's chat).
  - Token type is in the "B · the floor token" block below (= hud_build DECISIONS #33): B10 and deeper at 0.8×, not scaled to the insert's width.
- **The map's one-line prompt until KeyHud B lands** (MapWorld :2537 today reads `LOCKED  ·  NEEDS THIS ZONE'S KEY`):
  - `LOCKED  ·  NEEDS STAIR KEY` once floor keys replace zone keys (tutorial M3);
  - a broken door shows `BROKEN`, then `WON'T SHUT` for 0.55 s on a shut try.
- **叙事 signed this table as written** (2026-10-22 09:4x); its §4 "no string" row was a mistake and is corrected. 叙事's scope notes:
  - `READ SCREEN` is only on READ terminals (C19), never on ambient desk monitors (AT-T19);
  - `READ SIGN` covers the stair plates;
  - the B10+ tag rule is fine (since replaced by the 0.8× rule below, DECISIONS #33).

**B · the floor token on the key tag (KeyHud B; 叙事 v1.1 §4, C28 B levels).**
- The tag shows `00`, `B1`, `B2` … `B9` in the tag's Courier Prime numerals: two characters fit as designed.
- From `B10`: set the three characters at **0.8×** the size, on the same baseline, centred in the same tag. The tag does not grow.
- The A panel's interim label stays `STAIR KEY` (A4).

## C. E·1 stays, and the playtest checks it

- Red kept E·1, the eye that opens from the dot.
- One playtest item tests the Skyrim risk that 信息设计 raised: **"When the crosshair turned into an eye, what did it mean?"** (open answer; code "look / read" vs "being seen").
- Questionnaire content belongs to 系统设计 (FR-PT04) and its layout to 印刷与演示.
- **Coding and switch rule.** The item is in `research/computer_ui/30_crosshair_grammar.md` §8 (系统设计, data 2026-10-22n A23). Answers are coded LOOK/READ, BEING SEEN, OTHER or DIDN'T NOTICE.
  - **Switch to E·3's lowered gaze when 2 or more testers in one round, or ≥ 25 % of a round, answer BEING SEEN.** 系统设计 proposed it; 平面视觉 accepts it as the rule.
  - The switch is parameters only (`LOOK` E·3 in the lab, DECISIONS #17).

## D. Verification

- When each part lands, 平面视觉 checks it against this card in a desktop capture:
  - A1 contrast;
  - A3 hover and click;
  - A4 the "·" and STAIR KEY;
  - the prompts in B.
- The slide goes in the VL log in the Shot format (`SHOT_FORMAT.md`).

## E. The §D check (VL413, 2026-10-22 17:4x) and three fixes

关卡设计 landed A1–A3, A5–A7, B (desktop) and tutorial M1/T5 in Game 67e956c9. Checked on its clone captures (1920 × 1080, `~/FrontRoomsMapWork/logs/s16/capture/`, 49/49 harness checks). Figma VL413 `3607:7488`; images `research/desktop_ui/images/vl413_*.png`.

**Pass, as the card writes it:**
- **A1:** every menu line is ink at 16 : 1 on the wash. The marker is 4 × 14 px, its right edge 10 px left of the caps, centred on them.
- **A2:** the first R turns the line into `R AGAIN TO RESTART`, with the marker on it; it times out after 2 s.
- **A3:** hover moves the marker (also on the second pause after a restart), click acts, and hover selects a settings row. The settings panel is unchanged.
- **A7 stats:** Plex Mono 20, and every digit reads (`LEVEL 0`, `1 ZONE`, `0 KEYS`).
- **B:** `BROKEN` on aim, then `WON'T SHUT` on a shut try (the one-line form until KeyHud B).
- **T5:** the title legend is Plex Mono 13, paper with the keyline, 5.7 : 1 on the floor.
  - **`TitleLegendY = -168` confirmed.** The legend sits 116 px under the wordmark's foot, about one wordmark cap height (106 px), on the floor, clear of the far wall's skirting and the door. Desktop has no title prompt, so the legend takes that slot.

**Not covered** (capture limits, not failures):
- A real mouse through the input module: batch mode drove the event system. One windowed hand check is still owed.
- A5 (the readout's state line) and A6 (the flash line): not in the captures. Send one frame each when convenient.
- **A4's "·": accepted** on 游戏视觉's VL388 (`images/vl388_bayon_dot_prompts.jpg`, desktop 1080 / 1440, phone HUD, touch layer). Bayon now falls back to IBM Plex Mono (`Bayon-Regular.ttf.meta` c68a3a3b). The dot sits at 0.39–0.43 of the caps instead of the middle: about 1–1.5 px low at 20 px. It reads clearly as a separator in every prompt, so it stays as is.
  - Centring it would need a glyph added to Bayon (a modified OFL font must be renamed) or a string split, and A4 rules out splits. The text after the dot moves 5 px right (Plex's wider advance); no prompt overflows its box.

**E1. The CAUGHT subtitle (corrects A7, desktop).**
- **The problem:** inside `overlayText` the subtitle line takes Bayon's tall line box, so it sits 68 px under CAUGHT's caps. The stats sit 67 px under it, and TRY AGAIN 60 px under them. With equal gaps the subtitle reads as a fifth row, not part of the title (the proximity A7 asked for on touch).
- **The fix:** in `:3225`, swap `<size=28>OCCUPANT ACCOUNTED FOR</size>` for a slot of the same height (`MenuSlot(28)`), so nothing else moves. Draw the subtitle as its own `Text` ("Menu subtitle", Bayon 28, ink, centred), with its caps top **20 px under CAUGHT's caps foot** (0.3 × the title's 64 px caps, as on touch); measure the caps with `InkBand`.
- Today that is 48 px higher than now. The stats and TRY AGAIN stay put. Gaps after: 20 / 115 / 60.

**E2. No synthetic bold on PAUSED and CAUGHT.**
- `<b>` on Bayon (`:2542`, `:3225`) makes Unity smear the glyphs, which notches the stroke ends at 88 px.
- Drop the `<b>` and keep Bayon Regular 88, as the desktop masters D15 / D17 (`2913:6763`, `2913:6806`) draw it. Bayon is already a heavy display face.
- The readout's threat-state bold (`:2934`) is a separate element and stays.

**E3. Both cards sit low.**
- `overlayText`'s desktop offset `(0, -170)` (`:3007`) is left over from the title layout, which hung the text under the wordmark. Since then the title text is empty, and the pause and caught cards inherit the drop. Their ink blocks run 566–885 and 573–878, so the block centres sit 185 px under the screen centre, while the settings panel is centred.
- **The fix:** `(0, +30)`. The lines, hits, legend and stats are placed from that origin, so they follow. The block centres end 15 px above the screen centre (the optical centre).
- Title and touch are unaffected.

**E4. The scene through the wash: keep.** The wash at 98 % lets the paused room show at 1.01 : 1, a frost that never competes with the type (ink 16 : 1). It is also in the masters.

After E1–E3 land: one desktop capture of pause and caught for my check, then I update the masters D15 / D17.

