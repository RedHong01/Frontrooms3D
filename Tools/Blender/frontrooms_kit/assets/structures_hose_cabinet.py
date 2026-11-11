"""Kit_HoseCabinet: a semi-recessed Class II occupant hose cabinet: 18 ga tub, full-glass door on a continuous hinge, red swing rack with 1-1/2 in linen hose, brass angle valve with a red handwheel, nozzle; no lettering (structures G4-b 2026-11-10, package structures_run_service, 30_plan.md P20; 63 6.1; LS117; NFPA 14). Built in _struct_run."""

import _run_b_common as B
import _struct_run as R

NAME = "Kit_HoseCabinet"
SMOOTH_ANGLE = 35.0
BUDGET = (7000, 2200, 250)


def build(kit):
    B.levels(kit, lambda k, lv: R.hose_cabinet(k, lv))
    R.hose_cabinet_meta(kit)
    R.finish(kit, (6.0, 18.0, 60.0), BUDGET, tags=("structures", "run", "services", "p20"))
