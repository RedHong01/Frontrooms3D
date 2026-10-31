"""Two-wheel hand truck (dolly) with three white corrugated cartons (#D6D3C8) taped
shut, parked leaning 16 degrees back against a wall (US, 1975-1992; no print).
PRODUCTION obstacle kit for Run! B (Kit_HandTruck_Cartons), hand-built LOD0/1/2.

Black tube frame 1.20 m, a 0.36 x 0.20 m nose plate, two 10 in rubber wheels on the
axle. The whole asset is modelled in the leaning pose: the axle is the pivot, the top
leans toward +Y (Blender; the wall side, Unity -Z). ORIGIN = the floor under the axle
centre. FRONT = kit -Y (Unity +Z). Box collider. Budget 4,500 / 1,800 / 350;
lodDistances 4 / 12 / never. Slots: Prop_SteelBlack, Prop_Rubber, RunB_Carton, Prop_Cardboard (tape).
"""

import math

from mathutils import Vector

import _run_b_common as B

NAME = "Kit_HandTruck_Cartons"
SMOOTH_ANGLE = 40.0
BUDGET = (4500, 1800, 350)

STEEL = "Prop_SteelBlack"
RUBBER = "Prop_Rubber"
CARTON = "RunB_Carton"
TAPE = "Prop_Cardboard"

LEAN = 16.0
WR = 0.127                  # wheel radius (10 in)
AXLE_Y = 0.06               # axle behind the frame plane


def lean(p):
    """Rotate a point of the upright model about the axle (x axis at y AXLE_Y, z WR)."""
    a = math.radians(-LEAN)
    y, z = p[1] - AXLE_Y, p[2] - WR
    return (p[0], AXLE_Y + y * math.cos(a) - z * math.sin(a), WR + y * math.sin(a) + z * math.cos(a))


def lbox(kit, size, p, slot, lv, w, name):
    bw, bs = B.bev(w, lv)
    return kit.box(size, lean(p), slot, bevel=bw, segments=bs, rot=(-LEAN, 0, 0), name=name)


def _level(kit, lv):
    fx = 0.17              # frame tube half spacing
    tr = 0.0127
    if lv == 2:
        lbox(kit, (0.38, 0.03, 1.12), (0, 0.0, 0.62), STEEL, lv, 0.0, "frame")
    else:
        # Uprights with the top loop handle.
        pts = [(-fx, 0.0, 0.04), (-fx, 0.0, 1.05), (-fx + 0.05, 0.0, 1.20), (fx - 0.05, 0.0, 1.20), (fx, 0.0, 1.05), (fx, 0.0, 0.04)]
        B.tube(kit, [lean(p) for p in pts], tr, STEEL, lv=lv, name="frame", lo=16)
        for z in (0.36, 0.70, 0.98):
            B.tube(kit, [lean((-fx, 0.0, z)), lean((fx, 0.0, z))], 0.009, STEEL, lv=lv, name="cross bar", lo=12)
        # Axle brackets down to the axle.
        for sx in (-1, 1):
            B.tube(kit, [lean((sx * fx, 0.0, 0.30)), lean((sx * fx, AXLE_Y, WR))], 0.010, STEEL, lv=lv, name="axle strut", lo=12)
    # Nose plate on the floor line.
    lbox(kit, (0.36, 0.20, 0.006), (0, -0.10, 0.006 + 0.0), STEEL, lv, 0.0015, "nose plate")
    # Axle and wheels (the wheels do not lean: they stand on the floor).
    ax = 0.235
    kit.cylinder(0.009, 2 * ax, (0, AXLE_Y, WR), STEEL, verts=B.segs(0.009, lv, 12), rot=(0, 90, 0), bevel=0.0, segments=0, name="axle")
    for sx in (-1, 1):
        tyre = [(0.060, -0.030), (0.105, -0.032), (0.120, -0.028), (0.1265, -0.016), (WR, 0.0), (0.1265, 0.016), (0.120, 0.028), (0.105, 0.032), (0.060, 0.030)]
        rings = None if lv == 0 else ([0, 2, 3, 4, 5, 6, 8] if lv == 1 else [0, 3, 5, 8])
        B.lathe(kit, tyre, (sx * ax, AXLE_Y, WR), RUBBER, lv=lv, rot=(0, 90, 0), name="tyre", lo=48, rings=rings)
        if lv < 2:
            hub = [(0.0, -0.034), (0.030, -0.034), (0.058, -0.028), (0.060, 0.0), (0.058, 0.028), (0.030, 0.034), (0.0, 0.034)]
            B.lathe(kit, hub, (sx * ax, AXLE_Y, WR), STEEL, lv=lv, rot=(0, 90, 0), name="hub", lo=24, rings=None if lv == 0 else [0, 1, 3, 5, 6])
    # Cartons stacked on the nose plate, against the frame.
    stack = [((0.40, 0.30, 0.30), 0.0), ((0.40, 0.30, 0.30), 0.30), ((0.34, 0.26, 0.24), 0.60)]
    for k, ((cw, cd, ch), z0) in enumerate(stack):
        cy = -cd / 2 - 0.004
        lbox(kit, (cw, cd, ch), (0.004 * (k - 1), cy, 0.012 + z0 + ch / 2), CARTON, lv, 0.003, "carton")
        if lv < 2:
            # Tape across the top flaps and down both ends.
            lbox(kit, (0.05, cd + 0.002, 0.0012), (0.004 * (k - 1), cy, 0.012 + z0 + ch + 0.0006), TAPE, 2, 0.0, "tape")
            if lv == 0:
                for sy in (-1, 1):
                    lbox(kit, (0.05, 0.0012, 0.06), (0.004 * (k - 1), cy + sy * (cd / 2 + 0.0006), 0.012 + z0 + ch - 0.03), TAPE, 2, 0.0, "tape end")


def build(kit):
    B.levels(kit, _level)
    kit.collider((0, 0.10, 0.60), (0.52, 0.48, 1.20))
    kit.tag("run", "run_b", "obstacle")
    B.lod_meta(kit, (4.0, 12.0, 0.0), BUDGET)
