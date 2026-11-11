"""Kit_CombPlate: DREAM RUN's landing plate 2.80 wide: galvanised plate, nosing, safety-yellow band, eight comb segments, 112 teeth at 25 mm (built, not wired) (structures G4-b 2026-11-10, package structures_run_service, 30_plan.md P20; RunEndDreamRun). Built in _struct_run."""

import _run_b_common as B
import _struct_run as R

NAME = "Kit_CombPlate"
SMOOTH_ANGLE = 35.0
BUDGET = (3000, 1000, 60)


def build(kit):
    B.levels(kit, lambda k, lv: R.comb_plate(k, lv))
    R.comb_plate_meta(kit)
    R.finish(kit, (5.0, 15.0, 50.0), BUDGET, tags=("structures", "sp7", "p20", "unwired"))
