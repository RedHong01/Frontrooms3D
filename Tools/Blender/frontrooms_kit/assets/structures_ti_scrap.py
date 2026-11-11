"""Kit_TIScrap: fit-out floor scatter A: stud and track offcuts, broken gypsum board, a corner bead, a screw carton (built, not wired) (structures G4-b 2026-11-10, package structures_run_service, 30_plan.md P20; 62 Kit_TIScrap; LS99). Built in _struct_run."""

import _run_b_common as B
import _struct_run as R

NAME = "Kit_TIScrap"
SMOOTH_ANGLE = 35.0
BUDGET = (1400, 500, 80)


def build(kit):
    B.levels(kit, lambda k, lv: R.ti_scrap(k, lv, "A"))
    R.ti_scrap_meta(kit)
    R.finish(kit, (4.0, 12.0, 40.0), BUDGET, tags=("structures", "sp7", "p20", "unwired"))
