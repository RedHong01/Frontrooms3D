"""Stainless-steel corner (jamb) guard, 38 x 38 mm legs, 1.2 mm sheet, 1.2 m tall, for
an outside wall corner or a door jamb. PRODUCTION kit for Run! B (Kit_CornerGuard_1200),
hand-built LOD0/1/2.

ORIGIN = the guarded corner at the floor. The wall is the quadrant x > 0, y > 0: leg A
lies on the face y = 0 (normal -Y = Unity +Z, the room side), leg B on the face x = 0
(normal -X, the opening side). Budget 120 / 40 / 12; lodDistances 2 / 6 / 25.
Slots: Prop_Aluminium (brushed stainless).
"""

import math

import _run_b_common as B

NAME = "Kit_CornerGuard_1200"
SMOOTH_ANGLE = 30.0
BUDGET = (120, 40, 12)

STEEL = "Prop_Aluminium"
LEG, T, HGT = 0.038, 0.0012, 1.2


def _outline(lv):
    pts = [(LEG, 0.0), (0.0, 0.0), (0.0, LEG), (-T, LEG)]
    n = {0: 3, 1: 1, 2: 0}[lv]
    if n:
        for k in range(n + 1):
            a = math.radians(180 + 90.0 * k / n)
            pts.append((T * math.cos(a), T * math.sin(a)))
    else:
        pts.append((-T, -T))
    pts.append((LEG, -T))
    return pts


def _level(kit, lv):
    if lv == 2:
        kit.box((LEG, LEG, HGT), (LEG / 2 - T, LEG / 2 - T, HGT / 2), STEEL, bevel=0.0, segments=0, name="guard")
        return
    kit.extrude(_outline(lv), HGT, (0, 0, HGT / 2), STEEL, plane="xy", bevel=0.0, segments=0, name="guard")


def build(kit):
    B.levels(kit, _level)
    kit.no_collider()
    kit.tag("wall_decor", "run", "run_b")
    B.lod_meta(kit, (2.0, 6.0, 25.0), BUDGET)
