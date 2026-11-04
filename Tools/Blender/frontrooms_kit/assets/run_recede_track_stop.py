"""The Run! receding end's track end stop (one at each end of each track). PRODUCTION kit Kit_RunRecede_TrackStop.

An end plate in the track's almond enamel closing the channel, two bolts, and a black rubber bumper inside it facing the trolley.
ORIGIN = the track's end on the soffit plane at its centre line; the bumper faces +Z (into the track).
Budget 300 / 80 / 12; lodDistances 4 / 12 / 30. Slots: Prop_SteelAlmond, Prop_Rubber. No collider.
"""

import _run_b_common as B
import _run_recede_common as R

NAME = "Kit_RunRecede_TrackStop"
SMOOTH_ANGLE = 35.0
BUDGET = (300, 80, 12)
GALV = "Prop_SteelAlmond"   # the track's enamel
RUBBER = "Prop_Rubber"


def _level(kit, lv):
    bw, bs = R.hero(lv, 0.0010)
    R.rbox(kit, -0.054, 0.054, -0.069, 0.0, -0.006, 0.0, GALV, bw, 1, name="end plate")
    if lv < 2:
        R.rbox(kit, -0.012, 0.012, -0.045, -0.015, 0.0, 0.020, GALV, bw, 1, name="bumper block")
        R.rcyl(kit, "z", 0.011, 0.022, (0.0, -0.030, 0.031), RUBBER, 16 if lv == 0 else 8, 0.0020 if lv == 0 else 0.0, 1, name="bumper")
    if lv == 0:
        for x in (-0.033, 0.033):
            R.rcyl(kit, "z", 0.0055, 0.003, (x, -0.034, -0.0075), GALV, 8, name="plate bolt")


def build(kit):
    B.levels(kit, _level)
    kit.no_collider()
    kit.tag("run", "run_recede", "ceiling")
    kit.min_ceiling = 0.0
    B.lod_meta(kit, (4.0, 12.0, 30.0), BUDGET)
