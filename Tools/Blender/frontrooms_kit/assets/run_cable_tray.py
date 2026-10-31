"""Galvanized steel ladder cable tray, 300 mm wide x 80 mm deep, one 3 m tile with rungs
every 230 mm and a bundle of six cables laid in it. PRODUCTION kit for Run! B
(Kit_CableTray_3m), hand-built LOD0/1/2.

ORIGIN = the tray's bottom centre at the tile start; the tile runs along +X (Blender),
Unity -X: the dress yaws it 90 degrees. Budget 2,600 / 900 / 120; lodDistances 3 / 9 / 30.
Slots: RunB_GalvSteel, Prop_PlasticBlack, Prop_PlasticGrey.
"""

import _run_b_common as B

NAME = "Kit_CableTray_3m"
SMOOTH_ANGLE = 40.0
BUDGET = (2600, 900, 120)

GALV = "RunB_GalvSteel"
BLACK = "Prop_PlasticBlack"
GREY = "Prop_PlasticGrey"

LENGTH, WIDTH, DEPTH = 3.0, 0.30, 0.080
PITCH = 0.23
CABLES = [(-0.070, 0.0090, BLACK), (-0.048, 0.0110, GREY), (-0.024, 0.0085, BLACK), (0.002, 0.0120, BLACK),
          (0.028, 0.0095, GREY), (0.052, 0.0080, BLACK)]


def _rail_outline(sy):
    w = WIDTH / 2
    o = [(w, 0.0), (w, DEPTH), (w - 0.020, DEPTH), (w - 0.020, DEPTH - 0.0025), (w - 0.0025, DEPTH - 0.0025),
         (w - 0.0025, 0.0025), (w - 0.020, 0.0025), (w - 0.020, 0.0)]
    o = [(sy * y, z) for (y, z) in o]
    return o if sy > 0 else list(reversed(o))


def _level(kit, lv):
    bw, bs = B.bev(0.0008, lv)
    for sy in (-1, 1):
        if lv == 2:
            kit.box((LENGTH, 0.004, DEPTH), (LENGTH / 2, sy * (WIDTH / 2 - 0.002), DEPTH / 2), GALV, bevel=0.0, segments=0, name="side rail")
        else:
            kit.extrude(_rail_outline(sy), LENGTH, (LENGTH / 2, 0, 0), GALV, plane="yz", bevel=bw, segments=bs, name="side rail")
    count = int(LENGTH / PITCH)
    for k in range(count):
        if lv == 2 and k % 2:
            continue
        x = PITCH / 2 + PITCH * k
        if lv == 2:
            kit.box((0.024, WIDTH - 0.008, 0.012), (x, 0, 0.006), GALV, bevel=0.0, segments=0, name="rung")
        else:
            kit.box((0.024, WIDTH - 0.005, 0.012), (x, 0, 0.0085), GALV, bevel=bw, segments=bs, name="rung")
    if lv == 2:
        kit.box((LENGTH, 0.13, 0.016), (LENGTH / 2, -0.009, 0.0225), BLACK, bevel=0.0, segments=0, name="cable bundle")
        return
    for y, r, slot in CABLES:
        z = 0.0145 + r
        kit.tube([(0.0, y, z), (LENGTH, y, z)], r, slot, verts=8 if lv == 0 else 5, name="cable", caps=False)


def build(kit):
    B.levels(kit, _level)
    kit.no_collider()
    kit.anchor("end", (LENGTH, 0, 0))
    kit.tag("ceiling", "run", "run_b")
    B.lod_meta(kit, (3.0, 9.0, 30.0), BUDGET)
