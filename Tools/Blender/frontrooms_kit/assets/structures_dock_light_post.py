"""Kit_DockLightPost: the 4 in pipe post at the dock edge the swing-arm dock light (FrontRoomsLampKit DockArm, ZL_DOCKARM) clamps to at its arm_mount anchor (2.40 m), on a gusseted base plate with four anchors (structures G4-a 2026-11-10, package structures_dock, 30_plan.md P19; 63 section 6.1). Built in _struct_dock."""

import _run_b_common as B
import _struct_dock as D

NAME = "Kit_DockLightPost"
SMOOTH_ANGLE = 40.0
BUDGET = (1200, 300, 60)


def build(kit):
    B.levels(kit, D.light_post)
    D.light_post_meta(kit)
    D.finish(kit, (5.0, 15.0, 50.0), BUDGET, tags=("structures", "run", "dock", "p19"))
