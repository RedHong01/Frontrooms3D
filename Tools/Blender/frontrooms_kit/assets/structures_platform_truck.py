"""Kit_PlatformTruck: the US four-wheel platform truck, 30 x 48 in oak deck on a steel angle frame, 8 in casters (swivel at the handle end), 1 in pipe U handle; empty (structures G4-a 2026-11-10, package structures_dock, 30_plan.md P19; 63 section 6.1). Built in _struct_dock."""

import _run_b_common as B
import _struct_dock as D

NAME = "Kit_PlatformTruck"
SMOOTH_ANGLE = 40.0
BUDGET = (4000, 1400, 200)


def build(kit):
    B.levels(kit, lambda k, lv: D.platform_truck(k, lv))
    D.platform_truck_meta(kit)
    D.finish(kit, (5.0, 15.0, 50.0), BUDGET, tags=("structures", "run", "dock", "p19"))
