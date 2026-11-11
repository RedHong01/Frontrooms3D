"""Kit_WireCagePartition_Panel1520: a 5 ft (1.52 m post pitch) welded-wire partition panel: 12 ga channel frame, 10 ga woven wire built as two faces (FR-1), mid stiffener, clamp plates (structures G4-b 2026-11-10, package structures_run_service, 30_plan.md P20; 62 Kit_WireCagePartition; LS99). Built in _struct_run."""

import _run_b_common as B
import _struct_run as R

NAME = "Kit_WireCagePartition_Panel1520"
SMOOTH_ANGLE = 35.0
BUDGET = (700, 300, 40)


def build(kit):
    B.levels(kit, lambda k, lv: R.cage_panel(k, lv, 1.52 - R.POST - 0.004))
    R.cage_panel_meta(kit, 1.52 - R.POST - 0.004)
    R.finish(kit, (5.0, 15.0, 50.0), BUDGET, tags=("structures", "run", "cage", "p20"))
