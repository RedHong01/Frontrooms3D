"""Kit_WireCagePartition_Post: a 2 in square tube partition post on a bolted floor shoe with its deck clip (SP5 deck 2.88) (structures G4-b 2026-11-10, package structures_run_service, 30_plan.md P20; 62 Kit_WireCagePartition; LS99). Built in _struct_run."""

import _run_b_common as B
import _struct_run as R

NAME = "Kit_WireCagePartition_Post"
SMOOTH_ANGLE = 35.0
BUDGET = (450, 160, 24)


def build(kit):
    B.levels(kit, lambda k, lv: R.cage_post(k, lv))
    R.cage_post_meta(kit)
    R.finish(kit, (5.0, 15.0, 50.0), BUDGET, tags=("structures", "run", "cage", "p20"))
