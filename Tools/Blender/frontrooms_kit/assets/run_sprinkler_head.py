"""Upright fire-sprinkler head on its welded outlet (US, 1975-1992): a red outlet boss on
the main, a brass frame with two arms, a red 5 mm glass bulb and a toothed deflector.
PRODUCTION kit for Run! B (Kit_SprinklerHead_Upright), hand-built LOD0/1 (no LOD2: it
culls at 15 m). ORIGIN = the outlet's foot on the top of the main (pipe top, z = 0).
Budget 700 / 220 / 0; lodDistances 1.5 / 5 / 15. The outlet boss and the hex take a
1-segment bevel (2 segments put the head at 800 tris). Slots: RunB_SprinklerRed, Prop_Brass.
"""

import math

import _run_b_common as B

NAME = "Kit_SprinklerHead_Upright"
SMOOTH_ANGLE = 40.0
BUDGET = (700, 220, 0)

RED = "RunB_SprinklerRed"
BRASS = "Prop_Brass"


def _deflector(kit, lv, z):
    if lv == 0:
        n = 12
        pts = []
        for k in range(n):
            a0 = 2 * math.pi * k / n
            for da, r in ((0.0, 0.0175), (0.20, 0.0175), (0.24, 0.0120), (0.34, 0.0120)):
                a = a0 + da * 2 * math.pi / n
                pts.append((r * math.cos(a), r * math.sin(a)))
        kit.extrude(pts, 0.0012, (0, 0, z), BRASS, plane="xy", bevel=0.0, segments=0, name="deflector")
    else:
        kit.cylinder(0.0170, 0.0012, (0, 0, z), BRASS, verts=10, bevel=0.0, segments=0, name="deflector")


def _level(kit, lv):
    # Welded outlet (thread-o-let) on the pipe: its foot dips 4 mm into the pipe's curve.
    if lv == 0:
        # Its foot sits in the pipe and its top faces the deck: a sharp welded edge, no bevel (budget 700).
        kit.cylinder(0.016, 0.014, (0, 0, 0.003), RED, verts=20, bevel=0.0, segments=0, name="outlet boss")
    else:
        kit.cylinder(0.016, 0.014, (0, 0, 0.003), RED, verts=10, bevel=0.0, segments=0, name="outlet boss")
    # Head body: wrench flats.
    kit.cylinder(0.0115, 0.008, (0, 0, 0.014), BRASS, verts=6, bevel=0.0008 if lv == 0 else 0.0, segments=1 if lv == 0 else 0, name="body hex")
    # Frame arms converging to the deflector boss.
    for sx in (-1, 1):
        pts = [(sx * 0.0085, 0, 0.017), (sx * 0.0100, 0, 0.032), (sx * 0.0030, 0, 0.049)]
        kit.tube(pts, 0.0022, BRASS, verts=8 if lv == 0 else 6, name="frame arm")
    kit.cylinder(0.0035, 0.006, (0, 0, 0.050), BRASS, verts=12 if lv == 0 else 6, bevel=0.0, segments=0, name="load screw")
    # Bulb: red liquid in glass, between the seat and the load screw.
    bulb = [(0.0, 0.0), (0.0018, 0.001), (0.0026, 0.004), (0.0026, 0.016), (0.0018, 0.020), (0.0, 0.021)]
    kit.lathe(bulb if lv == 0 else [bulb[0], bulb[2], bulb[3], bulb[5]], (0, 0, 0.026), RED, verts=10 if lv == 0 else 6, name="bulb")
    _deflector(kit, lv, 0.0535)


def build(kit):
    B.levels(kit, _level, which=(0, 1))
    kit.no_collider()
    kit.tag("ceiling", "run", "run_b", "sprinkler")
    B.lod_meta(kit, (1.5, 5.0, 15.0), BUDGET)
