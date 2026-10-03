"""Surface closer main arm (regular arm, a = 0.240 m): a forged flat steel
arm, dark-bronze painted, with a hub clamped on the pinion spindle by a
hex arm bolt, tapering to the elbow boss where the forearm pins on
(10_spec §2.4).

Real-world reference: the main arm of a 1960s-1990s regular-arm surface
closer (LCN 4010 / Norton 1600 class): forged or stamped steel, painted to
match the body, a square-socket hub held by an arm screw, a riveted elbow.
Length a = 0.240 comes from the spec's linkage solve, not a catalogue
template (ESTIMATE, §2.4).

Origin: PART frame: on the spindle axis at the arm's underside (door
(0.062, 2.068, 0.520) when on the body's "spindle" anchor). Part +X runs
along the arm to the elbow; part +Y is up; the arm rotates about part Y.
The visual-chat solver FrontRoomsDoorCloserLinkage sets its yaw each
LateUpdate so that part +X points from the spindle to the solved elbow
(kit.meta["motion"]). Label "Closer arm".

Geometry (part m): hub Ø 0.030 x 0.016 + hex arm bolt (top at Y 0.021,
door 2.089, under the 2.098 head lining); bar 10 mm thick (Y 0.002..0.012
= door 2.070..2.080) and 20 -> 16 mm wide; elbow boss Ø 0.022 with the pin
head underneath. Anchors: spindle (0, 0, 0), elbow (0.240, 0, 0) (the pivot
axis), elbow_top (0.240, 0.012, 0) where Kit_DoorCloser_Forearm's origin
sits.

Budget (§9.1): 1,200 / 400 / 60 tris; slot SteelBrown. No LOD1 (small part,
until P-1). Render-only.
"""

import random
import sys

import interact_door_common as dc

NAME = "Kit_DoorCloser_Arm"
LOD1 = None
LOD1_RATIO = 0.33
LOD2_RATIO = 0.05
LOD_DISTANCES = (1.5, 5.0, 20.0)
SMOOTH_ANGLE = 35.0

PAINT = "Prop_SteelBrown"
A = 0.240
BAR_Y0, BAR_Y1 = 0.002, 0.012


def wear(P, N, edge, obj):
    r = g = b = 1.0
    if edge:
        g -= 0.25
    return r, g, b


def build(kit):
    rng = random.Random(41060)
    # bar: a rounded rectangle swept from inside the hub to inside the elbow boss
    # (0.5 mm thinner than the hub and elbow boss at top and bottom, so no
    # face is coplanar with a boss face)
    n = 9
    path = [(0.010 + (A - 0.004 - 0.010) * i / (n - 1), 0.0, 0.0) for i in range(n)]

    def sec(i):
        w = 0.020 + (0.016 - 0.020) * i / (n - 1)
        return dc.rounded_rect(w, BAR_Y1 - BAR_Y0 - 0.001, 0.0028, 3, cx=0.0, cy=(BAR_Y0 + BAR_Y1) / 2)
    bar = dc.sweep_path(kit, path, sec, PAINT, "arm bar")

    # hub on the spindle, with a hex arm bolt and washer on top
    hub = dc.revolve(kit, [(0.0140, 0.0), (0.0150, 0.0010), (0.0150, 0.0150), (0.0140, 0.0160), (0.0, 0.0160)],
                     (0.0, 0.0, 0.0), (0, 1, 0), 32, PAINT, "hub")
    washer = dc.revolve(kit, [(0.0088, 0.0155), (0.0088, 0.0168), (0.0080, 0.0172), (0.0, 0.0172)],
                        (0.0, 0.0, 0.0), (0, 1, 0), 24, PAINT, "washer", close_start=False)
    hexr = [(x, z) for x, z in dc.hexagon(0.0127, phase=rng.uniform(0, 1.0))]
    bolt = dc.loft_y(kit, [(0.0170, hexr), (0.0205, hexr), (0.0212, [(x * 0.86, z * 0.86) for x, z in hexr])], PAINT, "arm bolt")
    # elbow boss and the pin's rivet head underneath
    boss = dc.revolve(kit, [(0.0100, BAR_Y0), (0.0110, BAR_Y0 + 0.001), (0.0110, BAR_Y1 - 0.001), (0.0100, BAR_Y1), (0.0, BAR_Y1)],
                      (A, 0.0, 0.0), (0, 1, 0), 32, PAINT, "elbow boss")
    rivet = dc.revolve(kit, [(0.0050, 0.0), (0.0050, -0.0008), (0.0038, -0.0016), (0.0020, -0.0021), (0.0, -0.0022)],
                       (A, BAR_Y0 + 0.0005, 0.0), (0, 1, 0), 24, PAINT, "elbow rivet", close_start=False)
    for p in (washer, bolt, rivet):
        dc.lod2_drop(p)

    dc.finalize(kit, SMOOTH_ANGLE, wear)

    lo, hi = dc.bounds_unity(kit.parts)
    assert hi[1] <= 0.0215 and lo[1] >= -0.0025, "arm height band (door 2.066..2.090)"
    assert abs(hi[0] - (A + 0.011)) < 3e-4

    dc.anchor(kit, "spindle", 0.0, 0.0, 0.0)
    dc.anchor(kit, "elbow", A, 0.0, 0.0)
    dc.anchor(kit, "elbow_top", A, BAR_Y1, 0.0)
    kit.meta["motion"] = {
        "type": "rotate", "axis": [0, 1, 0], "pivot": "origin (spindle)",
        "solver": "FrontRoomsDoorCloserLinkage", "aim": "part +X -> solved elbow",
        "a": A, "b": 0.260, "spindleDoor": [0.062, 2.068, 0.520], "shoePivotDoor": [0.125, 2.130, 0.100],
        "elbowRoot": "the circle-circle root farther from the leaf's S-face plane",
        "elbowDoorAt": {"0": [0.222, 0.341], "45": [0.385, 0.109], "95": [0.297, -0.095]},   # G1 check (c); spec §2.4 prints 0.101 at 45 deg
    }
    kit.no_collider()
    kit.tag("interactable", "door", "closer", "office")
    dc.lod_meta(kit, sys.modules[__name__])
