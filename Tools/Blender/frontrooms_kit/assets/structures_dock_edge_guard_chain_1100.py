"""Kit_DockEdgeGuard_Chain_1100: a steel chain on snap hooks between two stanchion eyes 1.10 m apart, sagging 0.12 m; origin midway between the eyes at their height (structures G4-a 2026-11-10, package structures_dock, 30_plan.md P19; 63 section 6.1). Built in _struct_dock."""

import _run_b_common as B
import _struct_dock as D

NAME = "Kit_DockEdgeGuard_Chain_1100"
SMOOTH_ANGLE = 40.0
BUDGET = (2600, 700, 120)


def build(kit):
    B.levels(kit, lambda k, lv: D.chain_span(k, lv, 1.10))
    D.chain_meta(kit, 1.10)
    D.finish(kit, (4.0, 12.0, 40.0), BUDGET, tags=("structures", "run", "dock", "p19"))
