"""Kit_RunRecede_SweepMoving: the receding end's rubber sweeps relaxed (between the tell and the seat). See _run_recede_sweep.py."""

import _run_recede_sweep as W

NAME = "Kit_RunRecede_SweepMoving"
SMOOTH_ANGLE = 60.0


def build(kit):
    W.build_pose(kit, False, (900, 300, 100))
