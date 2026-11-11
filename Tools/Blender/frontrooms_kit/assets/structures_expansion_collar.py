"""Kit_ExpansionCollar: FAR SHORE's steel expansion-joint collar round the 2.84 x 2.90 section: 200 / 100 mm bands, fire-stop wool in the 25 mm joint, bolts, galvanised threshold plate (built, not wired) (structures G4-b 2026-11-10, package structures_run_service, 30_plan.md P20; 72 3.2; RunEndFarShore). Built in _struct_run."""

import _run_b_common as B
import _struct_run as R

NAME = "Kit_ExpansionCollar"
SMOOTH_ANGLE = 35.0
BUDGET = (3500, 1000, 150)


def build(kit):
    B.levels(kit, lambda k, lv: R.expansion_collar(k, lv))
    R.expansion_collar_meta(kit)
    R.finish(kit, (6.0, 18.0, 60.0), BUDGET, tags=("structures", "sp7", "p20", "unwired"))
