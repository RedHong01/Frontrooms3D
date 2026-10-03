"""Crossbar exit device on the push face of the free Exit door (EX-F, P2;
era note "push bars on exits"): a Ø 32 mm satin-chrome crossbar between two
cast aluminium end cases, collars where the bar enters the cases, slotted
cover screws (10_spec §2.4 "Kit_ExitDevice_Crossbar").

Real-world reference: the classic US crossbar ("panic bar") exit device,
1950s-1990 (the type before the flat touchpad bars took over): a round
tube on pivoting arms inside two end cases, the latch case at the latch
end driving the latch. Aluminium cases (US28) and a satin-chrome bar
(US26D). No maker marks, no "PUSH" decal.

Origin: PART frame: the latch-end case mount, i.e. the centre of the latch
case's back face on the leaf's push face at door (-0.022, 1.000, 0.900).
Front (kit -Y = Unity +Z) = out of the push face. Part +X runs toward the
hinge. Placement (§1.2 face rule for _p): child of "Leaf rig" at door
(-0.022, 1.000, 0.900), localRotation Euler(0, -90, 0), localScale
(-1, 1, 1); part +X then maps to door -Z on either handing.

Geometry (part m): cases 0.075 (X) x 0.120 (Y) x 0.095 (Z) centred X 0
(latch) and X 0.800 (hinge) (door Z 0.900 / 0.100); bar centre Z 0.070,
outer face 0.086; cases 0.095 proud, which §1.5 allows on the P face only.
The bar's 12 mm push travel needs the bar as its own renderer; this v1 is
one static mesh (kit.meta["motion"] documents the travel; see the G1 build
note's open items).

Budget (§9.1): 3,500 / 1,200 / 200 tris; slots Prop_Aluminium (cases,
submesh 0: the larger area) and Prop_Chrome. No LOD1 (until P-1).
Render-only.
"""

import math
import random
import sys

import interact_door_common as dc

NAME = "Kit_ExitDevice_Crossbar"
LOD1 = None
LOD1_RATIO = 0.34
LOD2_RATIO = 0.06
LOD_DISTANCES = (2.0, 6.0, 25.0)
SMOOTH_ANGLE = 35.0

ALU = "Prop_Aluminium"
CHROME = "Prop_Chrome"
CASE_W, CASE_H, CASE_D = 0.075, 0.120, 0.095
HINGE_CASE_X = 0.800
BAR_R, BAR_Z = 0.016, 0.070


def case_section(cx, o):
    w = CASE_W - 2 * max(o, 0.0)
    d = CASE_D - max(o, 0.0)
    rf = max(0.012 - 0.6 * max(o, 0.0), 0.004)
    pts = [(cx - w / 2, 0.0), (cx + w / 2, 0.0), (cx + w / 2, d), (cx - w / 2, d)]
    return dc.fillet_polygon(pts, [0.002, 0.002, rf, rf], [3, 3, 10, 10], closed=True)


def end_case(kit, cx, name):
    # bullnose top / bottom: (Y, inset) from the flat sides to the end face
    prof = []
    for k in range(8):
        t = k / 7
        prof.append((CASE_H / 2 - 0.010 + 0.010 * math.sin(t * math.pi / 2), 0.010 * (1 - math.cos(t * math.pi / 2))))
    rings = [(-y, case_section(cx, o)) for y, o in reversed(prof)] + [(y, case_section(cx, o)) for y, o in prof]
    return dc.loft_y(kit, [(Y, [(x, z) for x, z in sec]) for Y, sec in rings], ALU, name)


def wear(P, N, edge, obj):
    r = g = b = 1.0
    if "bar" in obj.name:
        X = P[0]
        r -= 0.35 * (1 - min(1.0, abs(X - 0.45) / 0.30))     # hands push the middle of the bar
    if edge:
        g -= 0.2
    if obj.get("fr_screw"):
        b -= 0.35
    return r, g, b


def build(kit):
    rng = random.Random(41110)
    # 1. End cases (aluminium first -> submesh 0)
    latch = end_case(kit, 0.0, "latch case")
    hinge = end_case(kit, HINGE_CASE_X, "hinge case")
    # 2. Crossbar and collars (chrome)
    x0, x1 = CASE_W / 2 - 0.008, HINGE_CASE_X - CASE_W / 2 + 0.008
    bar = dc.revolve(kit, [(BAR_R, 0.0), (BAR_R, x1 - x0)], (x0, 0.0, BAR_Z), (1, 0, 0), 48, CHROME, "crossbar")
    for xa, sgn in ((CASE_W / 2, 1), (HINGE_CASE_X - CASE_W / 2, -1)):
        col = dc.revolve(kit, [(0.0190, -0.001), (0.0200, 0.0005), (0.0200, 0.0062), (0.0185, 0.0080), (BAR_R, 0.0080)],
                         (xa, 0.0, BAR_Z), (sgn, 0, 0), 64, CHROME, "bar collar", close_start=False, close_end=False)
        dc.lod2_drop(col)
    # 3. Two slotted oval cover screws on each case's outer side face
    for cx, side in ((0.0, -1), (HINGE_CASE_X, 1)):
        host = latch if side < 0 else hinge
        for y in (-0.035, 0.035):
            s = dc.oval_head(kit, (cx + side * CASE_W / 2, y, 0.040), (side, 0, 0), CHROME, rng, d=0.0068, name="case screw")
            dc.lod2_drop(s)

    dc.finalize(kit, SMOOTH_ANGLE, wear)

    lo, hi = dc.bounds_unity(kit.parts)
    assert hi[2] <= CASE_D + 1e-4, "0.095 proud limit on the push face"
    assert abs(lo[1] + CASE_H / 2) < 2e-4 and abs(hi[1] - CASE_H / 2) < 2e-4
    door_z = (0.900 - lo[0], 0.900 - hi[0])
    assert door_z[1] > 0.018 + 0.03 and door_z[0] < 0.982 - 0.03, "cases clear of the push-side stops %s" % (door_z,)

    dc.anchor(kit, "latch_case", 0.0, 0.0, 0.0)
    dc.anchor(kit, "hinge_case", HINGE_CASE_X, 0.0, 0.0)
    dc.anchor(kit, "bar_centre", HINGE_CASE_X / 2, 0.0, BAR_Z)
    dc.anchor(kit, "push_point", HINGE_CASE_X / 2, 0.0, BAR_Z + BAR_R)
    kit.meta["motion"] = {"type": "static", "parent": "Leaf rig", "placeAtDoor": [-0.022, 1.000, 0.900],
                          "localRotation": [0, -90, 0], "localScale": [-1, 1, 1], "face": "p",
                          "barTravel": {"axis": [0, 0, -1], "travel": 0.012,
                                        "note": "needs the bar as a separate renderer (not in v1)"}}
    kit.no_collider()
    kit.tag("interactable", "door", "exit_device", "exit")
    dc.lod_meta(kit, sys.modules[__name__])
