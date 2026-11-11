"""Kit_TimeClockStation: an electromechanical time recorder (the hero: bar-index dial, three hands, card throat, lock) between IN and OUT racks of 13 pockets on a plywood board; ten blank cards out (structures G4-b 2026-11-10, package structures_run_service, 30_plan.md P20; 63 RN-SPINE, 6.1; LS119). Built in _struct_run."""

import _run_b_common as B
import _struct_run as R

NAME = "Kit_TimeClockStation"
SMOOTH_ANGLE = 35.0
BUDGET = (6000, 1800, 250)


def build(kit):
    B.levels(kit, lambda k, lv: R.time_clock(k, lv))
    R.time_clock_meta(kit)
    R.finish(kit, (6.0, 18.0, 60.0), BUDGET, tags=("structures", "run", "services", "p20"))
