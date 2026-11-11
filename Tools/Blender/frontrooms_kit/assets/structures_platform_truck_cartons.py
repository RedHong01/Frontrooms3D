"""Kit_PlatformTruck_Cartons: the platform truck with three taped cartons (structures G4-a 2026-11-10, package structures_dock, 30_plan.md P19; 63 section 6.1). Built in _struct_dock."""

import _run_b_common as B
import _struct_dock as D

NAME = "Kit_PlatformTruck_Cartons"
SMOOTH_ANGLE = 40.0
BUDGET = (4600, 1600, 240)


def build(kit):
    B.levels(kit, lambda k, lv: D.platform_truck(k, lv, 'cartons'))
    D.platform_truck_meta(kit, 'cartons')
    D.finish(kit, (5.0, 15.0, 50.0), BUDGET, tags=("structures", "run", "dock", "p19"))
