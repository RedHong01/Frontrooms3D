"""Kit_ReceivingDesk: a steel standing slant-top receiving desk 30 x 24 in, lift lid on a piano hinge, book ledge, pencil trough, hasp, lower shelf, black glides; its back to the wall (structures G4-a 2026-11-10, package structures_dock, 30_plan.md P19; 63 section 6.1). Built in _struct_dock."""

import _run_b_common as B
import _struct_dock as D

NAME = "Kit_ReceivingDesk"
SMOOTH_ANGLE = 40.0
BUDGET = (2000, 700, 100)


def build(kit):
    B.levels(kit, D.receiving_desk)
    D.receiving_desk_meta(kit)
    D.finish(kit, (4.0, 12.0, 40.0), BUDGET, tags=("structures", "run", "dock", "p19"))
