# Pattern-native cue states for WP03 hard_edge, from 平面视觉

These are reference renders of the final states in `Documentation/research/wallpaper_motion/32_pattern_native_hints.md` §9, drawn from the real geometry in `Tools/print/patterns/hard_edge.py`. They are design references for the prototype and the shader; nothing here is a game texture.

| File | State |
|---|---|
| `wall_baseline.png` | A 3 m wall face as printed, half-drop (z 0.3–2.4 m, 0.5 px/mm) |
| `wall_flow.png` | FLOW: unit 1 slips up one half-drop (562.5 mm); the guide row's lower grey bands turn 90° and point along the route |
| `wall_here.png` | HERE: 1.0 m door centred (u 1.0–2.0 m). Fields left of it point right, fields right of it point left; no paper inside the opening |
| `wall_stop.png` | STOP: the guide row's grey bands flatten to 64 × 206.7 mm bars centred on the row |
| `wall_pressure.png` | Optional, pending Red: no slip; the unit-2 row (≈1.29 m) and unit 1's knee row (≈0.73 m) both turn |
| `detail_*.png` | One roll around the guide row at 2 px/mm (baseline, FLOW, STOP) |
| `states.py` | The renderer: `/usr/bin/python3 states.py <outdir>`. It imports `hard_edge.py` |

**Geometry.**
- **Guide row:** the unit-2 lower chevron of block 2, assuming the print starts at the floor in 1125 mm blocks. The apex is at z 1393.5 mm and the band centre at about 1287.7 mm. Treat "≈1.29 m" as a result of that assumption, not a fixed number.
- **Turn:** the turned band measures 211.6 mm across a 206.7 mm field, so each hairline trims about 2.5 mm. It spans z 1184–1391 mm and overlaps the upper chevron by about 47 mm.
- **Overlap:** the turned band sits on top, like a re-pasted piece. This is option (a).
- **Cream band:** the lower chevron's cream band is absorbed, as in Red's 2502:4565.
- **Direction:** it comes from the route in world space. On facing walls the same route reads `>` on one and `<` on the other in print coordinates.
