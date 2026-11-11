"""Kit_Duct_3m: a 3 m run of 24 x 12 in galvanised supply duct, two 1.5 m sections with S-slip and drive joints, cross-broken panels, Pittsburgh corner seams (structures G4-b 2026-11-10, package structures_run_service, 30_plan.md P20; 63 6.1 Kit_Duct_3m; SMACNA low-pressure practice). Built in _struct_run."""

import _run_b_common as B
import _struct_run as R

NAME = "Kit_Duct_3m"
SMOOTH_ANGLE = 30.0
BUDGET = (900, 300, 40)


def build(kit):
    B.levels(kit, lambda k, lv: R.duct(k, lv, 3.0, 2))
    R.duct_meta(kit, 3.0)
    R.finish(kit, (6.0, 18.0, 60.0), BUDGET, tags=("structures", "run", "services", "p20", "ceiling"))
