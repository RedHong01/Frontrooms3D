# Tools/Blender/glass: GD3 glass fracture pattern (reference + look-dev)

Plan: `Documentation/research/glass/destruction/10_glass_destruction_plan.md` §2.6, §4.1, §5.2 step 2.
Report: `Documentation/research/glass/destruction/build/02_pattern.md`.

| File | What it is | Run with |
|---|---|---|
| `crack_graph_ref.py` | The crack-graph REFERENCE generator: one window pane → radial tracks, forks, ring chords, crush core, pieces, teeth (clipped to the tooth band), stage-1 cracks, stage-2 poses, release waves. Plain Python, no bpy. Deterministic by seed. The test oracle for the C# port `FrontRoomsGlassFracturePattern` (step 3) | `python3 crack_graph_ref.py gen --seed 4242 --impact-root 0 1.62` |
| `test_crack_graph_ref.py` | The validation suite: area ±0.05 %, no overlaps, no crossing cracks, convex pieces, ≥ 1 cm², teeth in the band, T-ends not '+', budgets, determinism, golden bytes, negative tests, contract inputs | `/usr/bin/python3 test_crack_graph_ref.py --n 120 --report r.json` |
| `golden/` | 20 golden cases + `index.json` (sha256 of each file's bytes). The C# port must match every vertex within 0.1 mm | `python3 crack_graph_ref.py golden --out-dir golden` |
| `glass_lookdev.py` | Cycles look-dev of S1 / S2 / S4 from generator JSON: a W-L0 window in a Level 0 room, troffers in the reflection, a dim hall beyond, the game lens (76° vertical) | `Blender -b --factory-startup --python glass_lookdev.py -- --jobs jobs.json` |
| `glass_review_sheets.py` | The review sheets (`pattern_*.jpg`, ≤ 1920 px wide) | `/usr/bin/python3 glass_review_sheets.py --json-root J --render-root R --out O --test-root T --verdicts v.json` |

**Inputs follow the map's `GlassBreakRecord`** (in main, `FrontRoomsMapWorld.cs`):
- `impact`: window-root metres (x across, y up);
- `impactUV`: over the exposed glass 1.367 × 1.617 (u from x −0.6835, v from y 0.3665);
- `rotation`: degrees;
- `side`: +1 = struck from cell a (glass flies toward +Z); −1 = from cell b.

The pattern never depends on `side`; it only sets `far_z_sign` for the poses.

**Profiles** (`FrontRoomsGlassTypeProfile` data): `Annealed6` (desktop High, the build), `Annealed6_Cinematic`, `Annealed6_WebGL`, and the parked `Wired6` and `Tempered6`.

**Never** write these scripts' output into `Assets/`. They are tools; the game builds its pieces at E-press in C#.
