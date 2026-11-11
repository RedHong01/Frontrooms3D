"""Kit_DoorWedge: a hardwood door wedge 150 x 50 x 40 mm (structures G4-b 2026-11-10, package structures_run_service, 30_plan.md P20; 63 Kit_DoorWedge; LS119). Built in _struct_run."""

import _run_b_common as B
import _struct_run as R

NAME = "Kit_DoorWedge"
SMOOTH_ANGLE = 35.0
BUDGET = (120, 40, 12)


def build(kit):
    B.levels(kit, lambda k, lv: R.door_wedge(k, lv))
    R.door_wedge_meta(kit)
    R.finish(kit, (3.0, 8.0, 20.0), BUDGET, tags=("structures", "run", "closet", "p20"))
