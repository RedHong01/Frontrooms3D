"""The Run! receding end's bottom guide slot, one 3 m section (71_run_room.md s.5.1: "a bottom guide in a 20 mm floor
slot"). PRODUCTION kit Kit_RunRecede_FloorSlot_3m, hand-built LOD0/1/2.

A galvanized channel set flush in the VCT: two 6 mm lips either side of a 20 mm slot, a black interior, countersunk
screws every 500 mm. Two runs (x +-0.95, in the floor field outside the lane) from the line the end stands on to the
line it seats on; the head's guide fins (Kit_RunRecede_HeadGear) run in them.
ORIGIN = the section's start on the floor at the slot's centre line; runs +Z 3.0 m. Budget 300 / 40 / 12; lodDistances
4 / 12 / 30. Slots: RunB_GalvSteel, Prop_PlasticBlack. No collider.
"""

import _run_b_common as B
import _run_recede_common as R

NAME = "Kit_RunRecede_FloorSlot_3m"
SMOOTH_ANGLE = 35.0
BUDGET = (300, 40, 12)
GALV = "RunB_GalvSteel"
DARK = "Prop_PlasticBlack"
L = 3.0


def _level(kit, lv):
    R.rbox(kit, -0.0100, 0.0100, 0.0010, 0.0014, 0.0, L, DARK, name="slot dark")
    if lv == 2:
        return
    for s in (-1, 1):
        R.rbox(kit, s * 0.010, s * 0.016, 0.0004, 0.0020, 0.0, L, GALV, 0.0005 if lv == 0 else 0.0, 1, name="slot lip")
        if lv == 0:
            z = 0.25
            while z < L:
                R.rcyl(kit, "y", 0.0022, 0.0006, (s * 0.013, 0.0021, z), GALV, 6, name="lip screw")
                z += 0.50


def build(kit):
    B.levels(kit, _level)
    kit.no_collider()
    kit.tag("run", "run_recede")
    B.lod_meta(kit, (4.0, 12.0, 30.0), BUDGET)
