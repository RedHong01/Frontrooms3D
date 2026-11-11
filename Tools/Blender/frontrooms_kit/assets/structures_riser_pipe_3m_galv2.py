"""Kit_RiserPipe_3m_Galv2: 2 in galvanised threaded pipe 3 m long with its coupling (structures G4-b 2026-11-10, package structures_run_service, 30_plan.md P20; S5-05). Built in _struct_run."""

import _run_b_common as B
import _struct_run as R

NAME = "Kit_RiserPipe_3m_Galv2"
SMOOTH_ANGLE = 40.0
BUDGET = (700, 260, 40)


def build(kit):
    B.levels(kit, lambda k, lv: R.riser_galv(k, lv, 3.0))
    R.riser_meta(kit, 3.0, R.GALV2_R)
    R.finish(kit, (6.0, 18.0, 60.0), BUDGET, tags=("structures", "run", "shaft", "p20"))
