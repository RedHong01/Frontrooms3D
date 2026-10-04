VOID (2026-10-04 07:5x): these two runs computed fresh references with FARFADE set, and bench 5b then let the far fade reach the reference itself.
Fixed in rt_bench5b.mm (refBlocks forces farFade = 0; cache names carry OWNSCALE). Replaced by ../v5r4_accept_design_own.txt and ../v5r4_accept_design_own05.txt.
