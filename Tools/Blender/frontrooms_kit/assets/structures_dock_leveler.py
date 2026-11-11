"""Kit_DockLeveler: the 6 x 8 ft mechanical pit leveler stored flush, 6.4 mm tread deck 3.5 mm proud of the platform, its 16 in lip hanging in the keepers on the dock face; origin on the platform at the dock face line, the deck toward -Z (structures G4-a 2026-11-10, package structures_dock, 30_plan.md P19; 63 section 6.1). Built in _struct_dock."""

import _run_b_common as B
import _struct_dock as D

NAME = "Kit_DockLeveler"
SMOOTH_ANGLE = 40.0
BUDGET = (6000, 1800, 200)


def build(kit):
    B.levels(kit, D.leveler)
    D.leveler_meta(kit)
    D.finish(kit, (6.0, 18.0, 60.0), BUDGET, tags=("structures", "run", "dock", "p19"))
