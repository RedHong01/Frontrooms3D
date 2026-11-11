"""Kit_PalletLoad_Wrapped: a GMA 48 x 40 in stringer pallet with three layers of cartons, hand stretch-wrapped (the film with the cartons painted through it), the rope at the deck, a torn tail; 0.88 m (structures G4-a 2026-11-10, package structures_dock, 30_plan.md P19; 63 section 6.1). Built in _struct_dock."""

import _run_b_common as B
import _struct_dock as D

NAME = "Kit_PalletLoad_Wrapped"
SMOOTH_ANGLE = 40.0
BUDGET = (3000, 900, 120)


def build(kit):
    B.levels(kit, lambda k, lv: D.pallet_load(k, lv, 'low'))
    D.pallet_load_meta(kit, 'low')
    D.finish(kit, (5.0, 15.0, 50.0), BUDGET, tags=("structures", "run", "dock", "p19"))
