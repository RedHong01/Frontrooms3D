"""Kit_WireCagePartition_Door: the cage door: a 4 ft opening, the leaf on three barrel hinges with a sag brace, barrel bolt, offset hasp over a staple and a laminated padlock (the hero), transom to the deck (structures G4-b 2026-11-10, package structures_run_service, 30_plan.md P20; 62 Kit_WireCagePartition; LS99). Built in _struct_run."""

import _run_b_common as B
import _struct_run as R

NAME = "Kit_WireCagePartition_Door"
SMOOTH_ANGLE = 35.0
BUDGET = (3200, 1100, 120)


def build(kit):
    B.levels(kit, lambda k, lv: R.cage_door(k, lv))
    R.cage_door_meta(kit)
    R.finish(kit, (6.0, 18.0, 60.0), BUDGET, tags=("structures", "run", "cage", "p20"))
