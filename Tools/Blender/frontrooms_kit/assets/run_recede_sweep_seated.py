"""Kit_RunRecede_SweepSeated: the receding end's rubber sweeps pressed (seated on a cell line). See _run_recede_sweep.py."""

import _run_recede_sweep as W

NAME = "Kit_RunRecede_SweepSeated"
SMOOTH_ANGLE = 60.0


def build(kit):
    W.build_pose(kit, True, (900, 300, 100))
