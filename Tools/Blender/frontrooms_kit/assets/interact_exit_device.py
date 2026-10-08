"""Crossbar exit device on the push face of the free Exit door (EX-F, P2;
era note "push bars on exits"): a Ø 32 mm satin-chrome crossbar between two
cast aluminium end cases, collars where the bar enters the cases, slotted
cover screws (10_spec §2.4 "Kit_ExitDevice_Crossbar").

Real-world reference: the classic US crossbar ("panic bar") exit device,
1950s-1990 (the type before the flat touchpad bars took over): a round
tube on pivoting arms inside two end cases, the latch case at the latch
end driving the latch. Aluminium cases (US28) and a satin-chrome bar
(US26D). A cover seam runs round each case 20 mm off the door. No maker
marks, no "PUSH" decal.

Origin: PART frame: the latch-end case mount, i.e. the centre of the latch
case's back face on the leaf's push face at door (-0.022, 1.000, 0.900).
Front (kit -Y = Unity +Z) = out of the push face. Part +X runs toward the
hinge. Placement (§1.2 face rule for _p): child of "Leaf rig" at door
(-0.022, 1.000, 0.900), localRotation Euler(0, -90, 0), localScale
(-1, 1, 1); part +X then maps to door -Z on either handing.

Geometry (part m): hinge case 0.075 (X) x 0.120 (Y) x 0.095 (Z) centred X
0.800 (door Z 0.100); latch (mechanism) case 0.1025 x 0.180 x 0.095 from part
X -0.065 to +0.0375 (door Z 0.965 .. 0.8625) with a rim-latch head and the
retracted bolt face on its latch side (fix pass 2026-10-08, critic L4; the
swept-clearance assert keeps it 3 mm off the latch stop at every angle 0-95
deg on the A2 axis); bar centre Z 0.070,
outer face 0.086; cases 0.095 proud, which §1.5 allows on the P face only.
The bar's 12 mm push travel needs the bar as its own renderer; this v1 is
one static mesh (kit.meta["motion"] documents the travel; see the G1 build
note's open items).

Budget (§9.1): 3,500 / 1,200 / 200 tris; slots Prop_Aluminium (cases,
submesh 0: the larger area) and Prop_Chrome. LOD1 exported since the 2026-10-08 fix pass (critic H4).
Render-only.
"""

import math
import random
import sys

import interact_door_common as dc

NAME = "Kit_ExitDevice_Crossbar"
LOD1_RATIO = 0.34
LOD1 = LOD1_RATIO  # fix pass 2026-10-08 (critic H4): FrontRoomsKitImporter honours the sidecar distances since 663e858, so LOD1 = LOD0->LOD1 at d01 and a cull at dcull
LOD2_RATIO = 0.051
LOD_DISTANCES = (2.0, 6.0, 25.0)
SMOOTH_ANGLE = 35.0

ALU = "Prop_Aluminium"
CHROME = "Prop_Chrome"
CASE_W, CASE_H, CASE_D = 0.075, 0.120, 0.095
HINGE_CASE_X = 0.800
BAR_R, BAR_Z = 0.016, 0.070
# Fix pass 2026-10-08 (critic L4: "reads as a towel rail"): the latch end is now the device's mechanism case,
# larger than the hinge bracket, as on a real rim crossbar device: 0.1025 wide (part X -0.065 .. +0.0375, door
# Z 0.965 .. 0.8625), 0.180 tall, the same 0.095 proud, with a rim-latch head on its latch side (door Z
# 0.965 -> 0.969) showing the bolt face retracted (the bolt never projects in this static mesh, so it cannot
# cut the stop when the leaf swings; a projecting bolt + rim strike need a moving part, see the 30_final note).
LATCH_X0, LATCH_X1, LATCH_H = -0.065, CASE_W / 2, 0.180
HEAD = (0.004, 0.045, 0.075, 0.064)        # latch head: reach (part -X), proud from .. to, height (Y)
BOLT = (0.012, 0.030, 0.0012)              # bolt face w (Y) x h (proud), stands 1.2 mm off the head face


def case_section(cx, o, case_w=CASE_W):
    w = case_w - 2 * max(o, 0.0)
    d = CASE_D - max(o, 0.0)
    rf = max(0.012 - 0.6 * max(o, 0.0), 0.004)
    g = 0.0006 if o <= 0.0 else 0.0          # cover seam, 20 mm off the door, on the flat sides only
    zs = 0.020
    pts = [(cx - w / 2, 0.0), (cx + w / 2, 0.0),
           (cx + w / 2, zs - 0.0004), (cx + w / 2 - g, zs), (cx + w / 2, zs + 0.0004),
           (cx + w / 2, d), (cx - w / 2, d),
           (cx - w / 2, zs + 0.0004), (cx - w / 2 + g, zs), (cx - w / 2, zs - 0.0004)]
    return dc.fillet_polygon(pts, [0.002, 0.002, 0, 0, 0, rf, rf, 0, 0, 0], [3, 3, 1, 1, 1, 10, 10, 1, 1, 1], closed=True)


def end_case(kit, cx, name, case_w=CASE_W, case_h=CASE_H):
    # bullnose top / bottom: (Y, inset) from the flat sides to the end face
    prof = []
    for k in range(6):      # 6 steps on the R 10 mm bullnose (was 8): keeps the larger latch case inside the §9.1 budget
        t = k / 5
        prof.append((case_h / 2 - 0.010 + 0.010 * math.sin(t * math.pi / 2), 0.010 * (1 - math.cos(t * math.pi / 2))))
    rings = [(-y, case_section(cx, o, case_w)) for y, o in reversed(prof)] + [(y, case_section(cx, o, case_w)) for y, o in prof]
    return dc.loft_y(kit, [(Y, [(x, z) for x, z in sec]) for Y, sec in rings], ALU, name)


def swing_clear(parts, stop_z=0.982, margin=0.003):
    """Every vertex, carried by the leaf about the A2 axis from 0 to 95 deg, stays below door Z stop_z - margin
    while it is inside the push-side stop band (door X <= -0.025). Part -> door: Z = 0.900 - x, X = -0.022 - z."""
    ax, az = dc.AXIS_X, dc.AXIS_Z
    worst = -1.0
    for obj in parts:
        mw = obj.matrix_world
        for v in obj.data.vertices:
            x, y, z = dc.to_unity(mw @ v.co)
            X, Z = -0.022 - z, 0.900 - x
            for k in range(0, 191):
                th = math.radians(k * 0.5)
                c, s_ = math.cos(th), math.sin(th)
                Xr = ax + (X - ax) * c + (Z - az) * s_
                Zr = az - (X - ax) * s_ + (Z - az) * c
                if Xr <= -0.025:
                    worst = max(worst, Zr)
    assert worst < stop_z - margin, "exit device reaches door Z %.4f in the stop band while swinging" % worst
    return worst


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
    dc.lod1_keep_all(kit)   # fix pass 2026-10-08: LOD1 keeps the LOD0 shape (collapse broke it; see dc.lod1_keep_all)
    rng = random.Random(41110)
    # 1. End cases (aluminium first -> submesh 0)
    latch = end_case(kit, (LATCH_X0 + LATCH_X1) / 2, "latch case", case_w=LATCH_X1 - LATCH_X0, case_h=LATCH_H)
    # rim-latch head on the latch side, and the retracted bolt face in it (chrome)
    reach, z0, z1, hh = HEAD
    head = dc.obox(kit, ALU, lo=(LATCH_X0 - reach, -hh / 2, z0), hi=(LATCH_X0 + 0.002, hh / 2, z1), bevel=0.0012, segs=2,
                   name="latch head")
    bw, bh, bp = BOLT
    zc = (z0 + z1) / 2
    bolt = dc.obox(kit, CHROME, lo=(LATCH_X0 - reach - bp, -bw / 2, zc - bh / 2), hi=(LATCH_X0 - reach + 0.001, bw / 2, zc + bh / 2),
                   bevel=0.0006, segs=2, name="rim bolt face")
    dc.lod2_drop(bolt)
    hinge = end_case(kit, HINGE_CASE_X, "hinge case")
    # 2. Crossbar and collars (chrome)
    x0, x1 = CASE_W / 2 - 0.008, HINGE_CASE_X - CASE_W / 2 + 0.008
    bar = dc.revolve(kit, [(BAR_R, 0.0), (BAR_R, x1 - x0)], (x0, 0.0, BAR_Z), (1, 0, 0), 48, CHROME, "crossbar")
    for xa, sgn in ((CASE_W / 2, 1), (HINGE_CASE_X - CASE_W / 2, -1)):
        col = dc.revolve(kit, [(0.0190, -0.001), (0.0200, 0.0005), (0.0200, 0.0062), (0.0185, 0.0080), (BAR_R, 0.0080)],
                         (xa, 0.0, BAR_Z), (sgn, 0, 0), 64, CHROME, "bar collar", close_start=False, close_end=False)
        dc.lod2_drop(col)
    # 3. Two slotted oval cover screws on each case's outer side face
    for xface, side, ys in ((LATCH_X0, -1, (-0.065, 0.065)), (HINGE_CASE_X + CASE_W / 2, 1, (-0.035, 0.035))):
        for y in ys:
            s = dc.oval_head(kit, (xface, y, 0.040), (side, 0, 0), CHROME, rng, d=0.0068, name="case screw")
            dc.lod2_drop(s)

    dc.finalize(kit, SMOOTH_ANGLE, wear)

    lo, hi = dc.bounds_unity(kit.parts)
    assert hi[2] <= CASE_D + 1e-4, "0.095 proud limit on the push face"
    assert abs(lo[1] + LATCH_H / 2) < 2e-4 and abs(hi[1] - LATCH_H / 2) < 2e-4
    door_z = (0.900 - lo[0], 0.900 - hi[0])
    assert door_z[1] > 0.018 + 0.03, "hinge case clear of the hinge-side stop %s" % (door_z,)
    assert door_z[0] <= 0.9705, "latch case, head and bolt face end at door Z 0.9702 %s" % (door_z,)
    kit.meta["swingClearWorstZ"] = round(swing_clear(kit.parts), 5)   # latch stop face at door Z 0.982

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
