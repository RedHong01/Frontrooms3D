"""Kit_FloorMachine: a 17 in rotary floor machine stored upright: brush, bumper, deck, ribbed motor, transport wheels, chrome handle with grips and switch box, black cord coiled on its hooks (structures G4-b 2026-11-10, package structures_run_service, 30_plan.md P20; 63 3.2 RN-JAN, Kit_FloorMachine; LS103). Built in _struct_run."""

import _run_b_common as B
import _struct_run as R

NAME = "Kit_FloorMachine"
SMOOTH_ANGLE = 35.0
BUDGET = (5000, 1700, 200)


def build(kit):
    B.levels(kit, lambda k, lv: R.floor_machine(k, lv))
    R.floor_machine_meta(kit)
    R.finish(kit, (5.0, 15.0, 50.0), BUDGET, tags=("structures", "run", "closet", "p20"))
