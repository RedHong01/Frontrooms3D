"""Kit_TempLightString_OneOut: the work lamp string with its middle lamp out (structures G4-b 2026-11-10, package structures_run_service, 30_plan.md P20; 62 Kit_TempLightString). Built in _struct_run."""

import _run_b_common as B
import _struct_run as R

NAME = "Kit_TempLightString_OneOut"
SMOOTH_ANGLE = 35.0
BUDGET = (2400, 800, 120)


def build(kit):
    B.levels(kit, lambda k, lv: R.temp_light_string(k, lv, (True, False, True)))
    R.temp_light_string_meta(kit, (True, False, True))
    R.finish(kit, (5.0, 15.0, 50.0), BUDGET, tags=("structures", "sp7", "p20", "unwired"))
