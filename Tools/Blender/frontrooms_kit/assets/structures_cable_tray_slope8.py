"""Kit_CableTray_Slope8: the ladder tray's hinged vertical splice plates for the 1:8 change (structures G4-b 2026-11-10, package structures_run_service, 30_plan.md P20; LS102). Built in _struct_run."""

import _run_b_common as B
import _struct_run as R

NAME = "Kit_CableTray_Slope8"
SMOOTH_ANGLE = 35.0
BUDGET = (300, 100, 24)


def build(kit):
    B.levels(kit, lambda k, lv: R.tray_slope(k, lv))
    R.tray_slope_meta(kit)
    R.finish(kit, (4.0, 12.0, 40.0), BUDGET, tags=("structures", "run", "services", "p20", "ceiling"))
