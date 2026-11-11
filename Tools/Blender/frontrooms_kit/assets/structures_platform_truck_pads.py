"""Kit_PlatformTruck_Pads: the platform truck with three folded quilted moving pads (structures G4-a 2026-11-10, package structures_dock, 30_plan.md P19; 63 section 6.1). Built in _struct_dock."""

import _run_b_common as B
import _struct_dock as D

NAME = "Kit_PlatformTruck_Pads"
SMOOTH_ANGLE = 40.0
BUDGET = (5000, 1700, 240)


def build(kit):
    B.levels(kit, lambda k, lv: D.platform_truck(k, lv, 'pads'))
    D.platform_truck_meta(kit, 'pads')
    D.finish(kit, (5.0, 15.0, 50.0), BUDGET, tags=("structures", "run", "dock", "p19"))
