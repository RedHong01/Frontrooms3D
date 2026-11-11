"""Kit_MopSink: a 24 x 24 x 10 in molded-stone floor service basin with stainless curb caps, dome strainer, the wall service faucet with vacuum breaker, pail hook and wall brace, and a rubber hose into the basin (structures G4-b 2026-11-10, package structures_run_service, 30_plan.md P20; 63 3.2 RN-JAN; LS103). Built in _struct_run."""

import _run_b_common as B
import _struct_run as R

NAME = "Kit_MopSink"
SMOOTH_ANGLE = 35.0
BUDGET = (4500, 1600, 200)


def build(kit):
    B.levels(kit, lambda k, lv: R.mop_sink(k, lv))
    R.mop_sink_meta(kit)
    R.finish(kit, (5.0, 15.0, 50.0), BUDGET, tags=("structures", "run", "closet", "p20"))
