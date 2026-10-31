"""Vinyl wall-protection bumper rail, 150 mm high, charcoal (#3A3A38), on an aluminium
retainer and two standoffs; one 1.2 m tile. PRODUCTION kit for Run! B
(Kit_BumperRail_1200), hand-built LOD0/1/2.

ORIGIN = the wall point at the rail's vertical centre, tile centre (the tile spans
x -0.6 .. 0.6, Unity the same with x flipped). FRONT = kit -Y (Unity +Z).
Budget 400 / 140 / 24; lodDistances 3 / 9 / never. Slots: RunB_BumperVinyl, Prop_Aluminium.
"""

import math

import _run_b_common as B

NAME = "Kit_BumperRail_1200"
SMOOTH_ANGLE = 35.0
BUDGET = (400, 140, 24)

VINYL = "RunB_BumperVinyl"
ALU = "Prop_Aluminium"

L = 1.2
H = 0.150
STAND = 0.018          # the vinyl's back sits this far off the wall
DEEP = 0.030           # vinyl depth


def _profile(lv):
    """(y, z): back flat at y = -STAND, the face bowed, 12 mm front corners."""
    n = {0: 5, 1: 2}.get(lv, 0)
    r = 0.012
    yb, yf = -STAND, -STAND - DEEP
    pts = [(yb, -H / 2)]
    for k in range(n + 1):
        a = math.radians(270 - 90.0 * k / n)
        pts.append((yf + r + r * math.cos(a), -H / 2 + r + r * math.sin(a)))
    pts.append((yf - 0.002, 0.0))
    for k in range(n + 1):
        a = math.radians(180 - 90.0 * k / n)
        pts.append((yf + r + r * math.cos(a), H / 2 - r + r * math.sin(a)))
    pts.append((yb, H / 2))
    return pts


def _level(kit, lv):
    if lv == 2:
        kit.box((L, DEEP, H), (0, -STAND - DEEP / 2, 0), VINYL, bevel=0.0, segments=0, name="vinyl")
        kit.box((L, STAND, H * 0.6), (0, -STAND / 2, 0), ALU, bevel=0.0, segments=0, name="retainer")
        return
    B.sweep(kit, "vinyl", _profile(lv), L, VINYL, lv=lv, axis="x")
    kit.box((L, 0.004, H - 0.024), (0, -STAND + 0.002, 0), ALU, bevel=0.0, segments=0, name="retainer")
    for sx in (-1, 1):
        if lv == 0:
            B.cyl(kit, 0.010, STAND - 0.004, (sx * 0.45, -(STAND - 0.004) / 2, 0), ALU, lv=1, rot=(90, 0, 0), w=0.0012, name="standoff", lo=16)
        else:
            kit.cylinder(0.010, STAND - 0.004, (sx * 0.45, -(STAND - 0.004) / 2, 0), ALU, verts=8, rot=(90, 0, 0), bevel=0.0, segments=0, name="standoff")


def build(kit):
    B.levels(kit, _level)
    kit.no_collider()
    kit.tag("wall_decor", "run", "run_b")
    B.lod_meta(kit, (3.0, 9.0, 0.0), BUDGET)
