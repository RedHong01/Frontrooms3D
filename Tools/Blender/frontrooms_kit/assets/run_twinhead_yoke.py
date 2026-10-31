"""Swivel yoke for one lamp head of the twin-head emergency unit (run_twinhead.py).
PRODUCTION hero kit, Run! B (Kit_EmergencyTwinHead_Yoke), hand-built LOD0/1/2.

A 3 x 18 mm bent steel strap U on a short swivel stem with a black lock collar; a
knurled black wing knob on each arm clamps the head's trunnion. ORIGIN = the stem
foot (it sits on the cabinet's swivel boss, anchor mount_l / mount_r). The rig yaws
this asset about its vertical axis; the head pivots at the "trunnion" anchor, 85 mm
above the foot. Budget 950 / 380 / 60; lodDistances 2.5 / 8 / 30.
Slots: Prop_SteelAlmond, Prop_PlasticBlack.
"""

import math

import _run_b_common as B

NAME = "Kit_EmergencyTwinHead_Yoke"
SMOOTH_ANGLE = 40.0
BUDGET = (950, 380, 60)

STEEL = "Prop_SteelAlmond"
BLACK = "Prop_PlasticBlack"

A = 0.0605          # inner half gap of the U (head housing r 57.5 mm + 3 mm)
T = 0.003           # strap thickness
TRUNNION_Z = 0.085
TOP = 0.104


def _knob(kit, x, sx, lv):
    """Knurled wing knob: a fluted lathe (10 flutes at LOD0) plus the wing."""
    if lv == 2:
        return
    n = 10 if lv == 0 else 8
    r0, r1 = 0.0085, 0.0079
    prof = [(r0, -0.004), (r0, 0.004)]
    obj = kit.lathe([(0.0, -0.004), (r0, -0.004), (r0, 0.004), (0.0, 0.004)], (x, 0, TRUNNION_Z), BLACK, verts=n * (2 if lv == 0 else 1), rot=(0, 90, 0), name="knob")
    if lv == 0:
        # Flutes: pull every other ring vertex in (a knurl you can see at 0.3 m).
        me = obj.data
        for v in me.vertices:
            rr = math.hypot(v.co.x, v.co.y)
            if rr > 1e-5:
                a = math.atan2(v.co.y, v.co.x)
                k = int(round(a / (2 * math.pi) * n * 2))
                if k % 2:
                    s = r1 / rr
                    v.co.x *= s
                    v.co.y *= s
    w = 0.0015 if lv == 0 else 0.0
    kit.box((0.005, 0.005, 0.026), (sx * (A + T + 0.0105), 0, TRUNNION_Z), BLACK, bevel=w, segments=2 if lv == 0 else 0, name="knob wing")


def _level(kit, lv):
    if lv < 2:
        kit.cylinder(0.0115, 0.006, (0, 0, 0.003), BLACK, verts=16 if lv == 0 else 8, bevel=0.001 if lv == 0 else 0.0008, segments=2 if lv == 0 else 1, name="lock collar")
    # The stem shows only as a ring between the collar and the strap: no bevel.
    kit.cylinder(0.0070, 0.016, (0, 0, 0.012), STEEL, verts=B.segs(0.007, lv, 12), bevel=0.0, segments=0, name="stem")
    # The U strap: inner corners bent on a 6 mm radius (outer 9 mm).
    if lv == 2:
        outline = [(-A - T, 0.018), (A + T, 0.018), (A + T, TOP), (A, TOP), (A, 0.021), (-A, 0.021), (-A, TOP), (-A - T, TOP)]
    else:
        ri, ro = 0.006, 0.006 + T
        n = 3 if lv == 0 else 2
        outline = []
        # outer bottom edge, right corner, up the outer right arm
        for k in range(n + 1):
            a = math.radians(270 + 90.0 * k / n)
            outline.append((A - ri + ro * math.cos(a), 0.018 + ro + ro * math.sin(a)))
        outline += [(A + T, TOP), (A, TOP)]
        for k in range(n + 1):
            a = math.radians(360 - 90.0 * k / n)
            outline.append((A - ri + ri * math.cos(a), 0.018 + ro + ri * math.sin(a)))
        for k in range(n + 1):
            a = math.radians(270 - 90.0 * k / n)
            outline.append((-A + ri + ri * math.cos(a), 0.018 + ro + ri * math.sin(a)))
        outline += [(-A, TOP), (-A - T, TOP)]
        for k in range(n + 1):
            a = math.radians(180 + 90.0 * k / n)
            outline.append((-A + ri + ro * math.cos(a), 0.018 + ro + ro * math.sin(a)))
    bw, bs = B.bev(0.0011, lv)
    kit.extrude(outline, 0.018, (0, 0, 0), STEEL, plane="xz", bevel=bw, segments=bs, name="yoke strap")
    for sx in (-1, 1):
        _knob(kit, sx * (A + T + 0.0045), sx, lv)


def build(kit):
    B.levels(kit, _level)
    kit.no_collider()
    kit.anchor("trunnion", (0, 0, TRUNNION_Z))
    kit.tag("run", "run_b")
    B.lod_meta(kit, (2.5, 8.0, 30.0), BUDGET)
