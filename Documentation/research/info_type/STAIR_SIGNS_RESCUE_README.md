# AREA OF RESCUE ASSISTANCE: the lit stair sign (hand-off to 游戏视觉)

> Repo copy of `~/FrontRoomsVisualWork/info_type/stair_signs/out/rescue/README.md` (2026-10-22 16:4x, 平面视觉 · 信息设计). Relative paths below are relative to that W folder, where the generator `stair_signs.swift` and the art live. Edit the W file, then re-copy.

From 平面视觉 · 信息设计, 2026-10-22. It is sign 7 of the stair set (`../../SPEC.md` v1.3).
- **Why:** Red made the stairwell a safe zone (游戏视觉 chat, 15:5x).
- **Strings:** 叙事设计, `narrative/40_floors_exit_tutorial_strings.md` §5.
- **Rule:** ADAAG 1991 §4.3.11.5: the sign states AREA OF RESCUE ASSISTANCE and shows the International Symbol of Accessibility, and it is lit when exit signs are.
- **Re-bake:** `cd ~/FrontRoomsVisualWork/info_type/stair_signs && swiftc -O stair_signs.swift -o stair_signs && ./stair_signs out 0 rescue`

| | |
|---|---|
| Content | `AREA OF` / `RESCUE` / `ASSISTANCE`, with the ISA on the left |
| Type | TeX Gyre Heros Bold, caps 1.25 in (31.75 mm), tracking +0.04 em, lines 0.45 in apart; widest ink 292.9 mm |
| Symbol | The static 1968 ISA (ADAAG 1991 Fig. 43(a)), drawn from strokes in the generator (no font glyph, so there is no licence question), 118 mm, the height of the text block. `isa_symbol.svg` is in mm |
| Face | 18.5 × 6.5 in (469.9 × 165.1 mm), translucent: white legend and symbol (#F0F2F0) on accessibility blue (#004A8C) |
| Housing | Like the exit-sign boxes: white enamel steel, a 0.5 in frame around the face, overall 19.5 × 7.5 in (495 × 191 mm), 2.25 in (57 mm) deep, flat on the wall |
| Light | **Steady**, on the emergency circuit: it never flickers, dips or dies with the lamps. The legend and symbol glow near-white at 1.0; the blue field glows at 0.4 |
| Never | Red letters, arrows, a call box or posted instructions (叙事). It must not read as an EXIT |
| Mount | Exit landing, stair side, on the latch wall above the stair ID sign, centred on it with a 2 in gap. The box bottom is at 69 in (1.753 m), its top at 76.5 in (1.943 m), centre 1.848 m. That keeps it under ADAAG 4.30.3's 80 in line, above which letters must be 3 in high |

Files, all at 4 px/mm, 1880 × 660 px:
- `rescue_face_albedo.png`: the face as printed, unlit;
- `rescue_face_emission.png`: grey, legend and symbol 1.0, field 0.4; multiply it by the face colour for the glow;
- `isa_symbol.svg`;
- `layout.json`: sizes and positions in mm.

On the blue: the terminal spec keeps VGA blue for a CRT you can READ. This is a different object, a lit wall box, and a deeper blue. It is also the standard accessibility colour, so it reads as "help" without borrowing the EXIT red.

When the stair is in the game, send me a lit frame from just inside the door after it shuts. That is the first view 叙事 wants this sign to own. I'll check it.
