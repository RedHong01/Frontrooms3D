"""Kit_WireCagePartition_Panel760: a 2-1/2 ft (0.76 m post pitch) welded-wire partition panel (as Panel1520) (structures G4-b 2026-11-10, package structures_run_service, 30_plan.md P20; 62 Kit_WireCagePartition; LS99). Built in _struct_run."""

import _run_b_common as B
import _struct_run as R

NAME = "Kit_WireCagePartition_Panel760"
SMOOTH_ANGLE = 35.0
BUDGET = (600, 260, 40)


def build(kit):
    B.levels(kit, lambda k, lv: R.cage_panel(k, lv, 0.76 - R.POST - 0.004))
    R.cage_panel_meta(kit, 0.76 - R.POST - 0.004)
    R.finish(kit, (5.0, 15.0, 50.0), BUDGET, tags=("structures", "run", "cage", "p20"))
