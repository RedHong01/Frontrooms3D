"""Kit_PalletLoad_Wrapped_High: the wrapped load five layers high (1.48 m); stands against a wall (63 G3) (structures G4-a 2026-11-10, package structures_dock, 30_plan.md P19; 63 section 6.1). Built in _struct_dock."""

import _run_b_common as B
import _struct_dock as D

NAME = "Kit_PalletLoad_Wrapped_High"
SMOOTH_ANGLE = 40.0
BUDGET = (3500, 1000, 120)


def build(kit):
    B.levels(kit, lambda k, lv: D.pallet_load(k, lv, 'high'))
    D.pallet_load_meta(kit, 'high')
    D.finish(kit, (5.0, 15.0, 50.0), BUDGET, tags=("structures", "run", "dock", "p19"))
