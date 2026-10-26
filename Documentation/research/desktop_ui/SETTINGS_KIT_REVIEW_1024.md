# Settings kit review: type, components, grid (平面视觉, 2026-10-24 09:3x)

For 设置菜单与画质预设 (`local_a77b85b3`): its request of 08:27, the review before any build. Reviewed:
- the kit `3692:7842`;
- desktop S1–S3 (`3694:7850`, `3694:8059`, `3694:8280`);
- handheld H1–H6 (`3697:8035` … `3697:8895`);
- the D1 options B / C (`3698:8430`, `3698:8552`).

The section is `3688:7842`. The spec is `research/interaction/settings/IA.md` §1 and §5. Red's picks (总控 chat, 08:5x) don't change anything below.

## Verdict

**On-system, with three fixes before the menu is built.** Faces, sizes, digits, tracking, the grid and the focus bar all follow the system. The fixes are about the surface, BACK and one note left in a screen.

## Fix before the build

1. **The surface: use the settings reading surface, #0E0E0D at 97 %, not the 86 % veil over the frost.**
   - Today's settings panel and the EXAMINE transcript panel (G9) both use 97 %.
   - At 86 %, the 50 % meta labels lose contrast over a bright frosted scene: about 3.0–3.5 : 1, against 4.5 : 1 for small text. This affects:
     - `PRESET`, `DETAIL`, `DISPLAY`, `COST`;
     - the right column's preset table (13 px Plex Mono on desktop, 11 pt on phones).
   - At 97 % the same labels hold about 4.6 : 1 over any scene. PEEK already covers "show me the game".
   - If the frost has to stay visible, raise the meta labels to at least 72 % paper instead. That is the weaker option.
2. **BACK is not a solid yellow chip.**
   - On desktop, draw it like RESET: a keyline button with the word in paper and no fill. When keyboard or controller focus lands on it, the 4 px yellow focus bar marks it, as on any row.
   - On touch, it is a keyline chip (the TEXT chip's off state).
   - Why: a settings page has no single primary action, because changes apply at once. So yellow should mean only one thing here, focus. With a yellow BACK, two yellow things are on screen whenever a row has focus, and the bar stops reading as "you are here".
   - The kit's YELLOW rule then reads "Focus only (the focus bar). No dark backing cards: keylines."
3. **Keep design notes out of player strings.** H3's explanation column carries `PROBE 2026-10-24: RENDER SCALE IS THE ONE DETAIL THAT CLEARLY MOVES THE FRAME`. That is a note for us, not text for the player.
   - The player-facing explanations are UI strings, so run them by 叙事设计, the strings owner, before they ship. That is the same route as the prompt strings (FIX_CARD §B).

## Make consistent

4. **Percent.**
   - Today's in-game strings write `100%` with no space (the settings panel in main, D16). The kit writes `50 %`.
   - Use `50%` in the UI. Documents keep their own style.
5. **The FRAME readout says the same thing on every device.**
   - Desktop shows `9.8 MS · 60 FPS`; the Duo inner (H3) shows `11.4 MS · 60` without the unit.
   - Per the kit's own ONE SYSTEM rule, keep `MS · … FPS` everywhere. Where width is short, drop dashes from the meter, never the unit.
6. **Bind the handheld text to the existing styles.**
   - `Touch/Phone/*` and `Touch/Tablet/*` (`TOUCH_CONTROLS.md` §4): about 31 of 48 runs per handheld frame are unstyled, so a ramp change would miss them.
   - Desktop has no game-UI styles yet (the desktop masters `2911:6716` are unstyled too), so desktop stays as it is.

## Optional

7. **The frame meter in the stamina-dash look** is acceptable: it is labelled (FRAME, ms) and lives outside play.
   - If you want no echo of the HUD's stamina dashes at all, draw it like the slider instead: a 1 px track with a 3 px fill.

## Signed off as drawn

- **Faces and sizes** are from the existing ramps only:
  - desktop Bayon 20 / IBM Plex Mono 13 / Source Serif Pro 24 (Pro, for Unity's opsz 20);
  - phone `Touch/Phone` (Bayon 17, Plex 11, Source Serif 4 17);
  - iPad and Duo inner `Touch/Tablet` (Bayon 19, Plex 12, Source Serif 4 19).
- **Digits** are in Plex Mono at the label size (20 / 17 / 19), never in Bayon. Checked across the whole section: no digit in Bayon.
- **Tracking** is 0 everywhere (Unity legacy Text).
- **Size floor:** nothing under 11 pt.
- **Grid:** 12 columns with 72 px margins on desktop. The list sits on columns 1–7 and the explanation on 9–12.
- **Focus:** the 4 px yellow focus bar at the row's left edge (FIX_CARD A1).
- **Controls:**
  - switches always show their word;
  - a selected segment is a paper fill with ink text, unselected at 75 %;
  - tabs are Bayon labels with a 2 px underline, flanked by Q / E keycaps;
  - the cost bars carry a COST heading.
- **PEEK:** no backing card; text sits on the picture at the margin, like the HUD.
- **The kit's TYPE rule** ("provisional until 平面视觉 signs off") is signed off once fixes 1–3 are in.
