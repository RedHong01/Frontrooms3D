"""Kit_MopRack_OneOut: the mop rack with one wet mop out (its holder empty) (structures G4-b 2026-11-10, package structures_run_service, 30_plan.md P20; 63 3.2 RN-JAN; LS103). Built in _struct_run."""

import _run_b_common as B
import _struct_run as R

NAME = "Kit_MopRack_OneOut"
SMOOTH_ANGLE = 35.0
BUDGET = (3000, 1100, 150)


def build(kit):
    B.levels(kit, lambda k, lv: R.mop_rack(k, lv, one_out=True))
    R.mop_rack_meta(kit)
    R.finish(kit, (5.0, 15.0, 50.0), BUDGET, tags=("structures", "run", "closet", "p20"))
