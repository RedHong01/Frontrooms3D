"""Kit_DuctHanger: 1 in x 18 ga galvanised strap hangers each side of the 24 x 12 in duct, turned under, screwed, pinned to the soffit 0.15 m over the duct (structures G4-b 2026-11-10, package structures_run_service, 30_plan.md P20; SMACNA hanger practice). Built in _struct_run."""

import _run_b_common as B
import _struct_run as R

NAME = "Kit_DuctHanger"
SMOOTH_ANGLE = 35.0
BUDGET = (300, 80, 24)


def build(kit):
    B.levels(kit, lambda k, lv: R.duct_hanger(k, lv))
    R.duct_hanger_meta(kit)
    R.finish(kit, (4.0, 12.0, 40.0), BUDGET, tags=("structures", "run", "services", "p20", "ceiling"))
