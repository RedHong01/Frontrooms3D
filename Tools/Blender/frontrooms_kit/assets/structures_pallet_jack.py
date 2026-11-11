"""Kit_PalletJack: a manual hydraulic pallet jack, 27 x 48 in forks lowered to 85 mm, pump with chrome ram, 180 mm steer wheels, tandem load rollers, loop handle with its lever; parked, handle up; origin under the footprint's centre, forks toward -Z (structures G4-a 2026-11-10, package structures_dock, 30_plan.md P19; 63 section 6.1). Built in _struct_dock."""

import _run_b_common as B
import _struct_dock as D

NAME = "Kit_PalletJack"
SMOOTH_ANGLE = 40.0
BUDGET = (4500, 1500, 250)


def build(kit):
    B.levels(kit, D.pallet_jack)
    D.pallet_jack_meta(kit)
    D.finish(kit, (5.0, 15.0, 50.0), BUDGET, tags=("structures", "run", "dock", "p19"))
