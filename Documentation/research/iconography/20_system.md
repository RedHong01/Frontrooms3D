# The FrontRooms icon system (draft for Red's picks, 2026-10-30)

One programme, two families. HUD marks are paper with a keyline, born from the crosshair dot. World marks are printed by a 1990 building in ink, AIGA/DOT style. Construction is shared; ink, colour and motion differ by family. Figma: IC06 `3773:8577`, IC07 `3773:8581`.

## 1. The unit

`s` = the stem of the face the icon sits with, at the size it is shown. `u = s / 2` is the drawing grid.

| Context | Paired face | s | At size |
|---|---|---|---|
| HUD, desktop 1080p | Bayon 20 px | 0.174 × H (0.122 em) | 2.43 px |
| HUD, iPad / Duo inner | Bayon 19 pt | | 2.31 pt |
| HUD, phone | Bayon 17 pt | | 2.06 pt |
| Plates, stencils | Heros Cn Bold | 0.171 × H | per plate |
| Placard labels | Heros Bold 5.0 mm caps | 0.206 × H | 1.03 mm |
| CRT, meta | IBM Plex Mono | 0.120 × H | per size |

Nothing in an icon is thinner than 1 s. Print drafting lines (walls, treads, frames) are at least 0.6 mm (ISO 23601 §6 d, the generator's own rule).

## 2. Fields and placement

- HUD field 24 u (12 s), padding 2 u, live area 20 u. Wide glyphs grow in 4 u steps (gait = 32 × 24 u).
- World field 28 u (14 s), live 24 u: the label's cap is then 0.42 of the field (DOT's range is 1/4–2/5 of the symbol).
- Icons centre on the band between baseline and cap line, never on the baseline. The icon comes first; the label starts 2 s after the field (about one word space).

## 3. Lines

- Angles 0 / 45 / 90°, and arcs drawn from one centre. Diagonals keep the perpendicular stroke.
- Gap and counter ≥ 1 s (stroke = gap for concentric arcs).
- Ends from the face: HUD square ends and sharp (mitred) corners; print flat cuts square to the stroke; CRT on the pixel grid with 45° chamfers.
- One level of abstraction: solid silhouettes and 1 s lines, front or profile, no perspective, texture or glow.
- One arrowhead: 90° (45° flanks) wherever the game draws its own arrow (HUD, placard route, wall arrow). The EXIT chevron keeps its UL 924 / NFPA 101 form.

## 4. Registry (closed: a new meaning needs a new shape or a word)

| Mark | Family | Meaning |
|---|---|---|
| dot (Ø16, #FFFFFF) | HUD | aim: what E does here, now |
| padlock (L3·D; print master on the placard) | both | a key opens this |
| broken door (B·4) | HUD | it won't shut |
| eye (E·1) | HUD | read / look at this |
| key B on its tag (print: key tag) | both | the floor key |
| footprints + 0 / 1 / 3 arcs (new) | HUD | how far your steps carry |
| stamina slots | HUD | breath left |
| you are here, route | world | your position, the way out |
| steps, sound arcs | world | heard (steps; the playing TV) |

Retired or reassigned: the key map's padlock ("binding fixed") → the word FIXED; yellow beyond focus → paper; five arrow forms → one head; translucent empties → outline slots.

## 5. Ink and colour

- HUD: paper `#F4F1E8` with a 1 px `#141414` keyline at 85 % (Unity 0.95 linear). No dark cards, no translucent ink. The dot is the one `#FFFFFF`. Yellow `#F4DF3B` = focus only.
- World: black `#171717` on paper (canary `#FCF7CE` for forms); white on safety red only for EXIT and fire; accessibility blue only for the rescue-area sign. Game meanings never borrow ISO / ANSI safety colours.
- Colour may double a state, never carry it alone.

## 6. States and motion

- A state is a shape: a count, a form, open or shut. Dashed = not yet / ghost; solid = now. No slash: a slashed sound mark reads as mute.
- HUD motion comes out of an element already on screen (the dot) and goes back into it; hard on/off, never faster than 3 Hz; steady states stay still.
- One motion, one meaning: the padlock's no-key tug and shake (D·3: ±3.5 px at 8 Hz for 360 ms) means "no" everywhere.

## 7. Masters and testing

- One master per medium, never scaled across: HUD per platform at its label size; print; plate texture; CRT raster.
- Every new icon passes an open-answer test with players: ≥ 67 % correct (ISO 3864 practice), aiming for 85 % with < 5 % critical confusion (ANSI Z535.3). Critical pair to test: RUN read as EXIT. Below the bar, the icon ships with its word.

## 8. The run state (IC07, option A)

- Glyph 32 × 24 u. Prints = the placard's `steps()` prints at k = 1.25 u: soles at (5, 11.375) and (13.25, 6.875), radii 3 × 4.875, heels offset (0.75, 7.75) radii 2.375 × 2.625, all rotated −8°. Arcs from (13, 12), radii 7 / 11 / 15 u, ±38°, stroke 2 u (= s), gap 2 u.
- States: SLOW (Shift) = prints only; WALK = 1 arc; RUN (Ctrl) = 3 arcs (heard 0 / 9 / 26 m; 12.6 / 36.4 m in a chase). RUN refused while winded = stays WALK and shakes with the dashes (D·3).
- Shown while Shift or Ctrl is held and for 2 s after any gait change; plain walking shows nothing. Setting SHOW NOISE · ALWAYS shows WALK too.
- Motion: prints cut on and off; arcs step out one at a time 60 ms apart when louder, step back in when quieter.
- Place: right edge 12 px left of the first stamina dash, centred on the dash line (desktop 1080p: 38.4 × 28.8 px). Same rules on desktop, iPad and phone.
- Stamina: dashes in paper; spent dashes are empty slots (1 px paper outline), all with the keyline.
- Cost: one SDF glyph in KeyHud (4 ellipses + up to 3 arcs); one hook on PlayerGait; one settings row. 交互设计 builds; 平面视觉 supplies the masters; words via 叙事设计.
