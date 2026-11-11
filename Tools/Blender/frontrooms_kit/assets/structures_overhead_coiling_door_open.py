"""Kit_OverheadCoilingDoor_Open: the 12 x 14 ft rolling steel service door of SP5's bay, open: the curtain coiled, the bar tucked under the hood (structures G4-a 2026-11-10, package structures_dock, 30_plan.md
P19). Face-of-wall mounted, hand-chain hoist; built in _struct_dock.coiling_door. Origin on the floor at the opening's centre on the
wall's room face, front (+Z) into the room."""

import _run_b_common as B
import _struct_dock as D

NAME = "Kit_OverheadCoilingDoor_Open"
SMOOTH_ANGLE = 40.0
BUDGET = (16000, 4500, 600)


def build(kit):
    B.levels(kit, lambda k, lv: D.coiling_door(k, lv, "open"))
    D.coiling_door_meta(kit, "open")
    D.finish(kit, (14.0, 40.0, 0.0), BUDGET, tags=("structures", "run", "dock", "p19", "wall_unit"))
