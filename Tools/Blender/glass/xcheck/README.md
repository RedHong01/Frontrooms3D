# Cross-check fixture for the C# port (GD3 step 3)

`make_xcheck.py` runs `crack_graph_ref.py` on 1,015 breaks and writes `cases.txt` (inputs) and `py_sha.txt` (sha256 of
each break's JSON plus the `validate()` verdict). `FrontRoomsGlassFractureTests` T9 runs the C# generator on the same
inputs and requires every sha256 and verdict to match. The goldens in `../golden/` hold 20 full outputs; this fixture
holds 1,015 hashes.

- Cases 0–214: the exact T1 sweep inputs of `test_crack_graph_ref.py`.
- Cases 215–1014: wider random breaks (impacts beyond the 0.2 m clamp, `band` and `stop`, explicit rotations).
- 4 cases (347, 629, 695, 884) are INVALID in the reference too, on the teeth-triangle budget only
  (2,020–2,120 vs 2,000). See `Documentation/research/glass/destruction/build/03_generator.md` §6.

Regenerate only together with the reference: `python3 make_xcheck.py` (about 40 min under heavy load).
