"""Surface closer forearm (adjustable, b = 0.260 m in plan): a forged elbow
eye, a gooseneck that climbs at once to clear the door top, a steel tube,
a hex lock nut, a threaded adjusting rod and the forged shoe-end fitting
whose vertical stud turns in Kit_DoorCloser_Shoe (10_spec §2.4).

Real-world reference: the adjustable forearm of a 1960s-1990s regular-arm
surface closer (tube + threaded rod + lock nut, so the installer sets the
arm angle), dark-bronze painted. b = 0.260 in plan comes from the spec's
linkage solve (ESTIMATE, §2.4).

Origin: PART frame: at the elbow pivot on top of the main arm (door Y
2.080 when on Kit_DoorCloser_Arm's "elbow_top" anchor). Part +X runs in
plan toward the shoe; part +Y is up; it rotates about part Y. The solver
FrontRoomsDoorCloserLinkage yaws it so that part +X points (in plan) from
the elbow to the shoe pivot. Label "Closer forearm".

Geometry (part m): eye Y 0..0.008; the rod's centre climbs to Y 0.040
within 0.08 m of the elbow, so its underside stands >= 0.032 (door 2.112),
17 mm over the 2.095 leaf top wherever it can cross the leaf; shoe-end stud
top at (0.260, 0.050, 0) = the shoe pivot (door Y 2.130). Anchors: elbow
(0, 0, 0), shoe_end (0.260, 0.050, 0).

Budget (§9.1): 900 / 300 / 50 tris; slots SteelBrown and Chrome (threaded
rod, nut). No LOD1 (until P-1). Render-only.
"""

import math
import random
import sys

import interact_door_common as dc

NAME = "Kit_DoorCloser_Forearm"
LOD1 = None
LOD1_RATIO = 0.33
LOD2_RATIO = 0.06
LOD_DISTANCES = (1.5, 5.0, 20.0)
SMOOTH_ANGLE = 35.0

PAINT = "Prop_SteelBrown"
CHROME = "Prop_Chrome"
B = 0.260
RISE = 0.050
ROD_Y = 0.040


def circle(r, n, cy=0.0):
    return [(r * math.cos(2 * math.pi * i / n), cy + r * math.sin(2 * math.pi * i / n)) for i in range(n)]


def wear(P, N, edge, obj):
    r = g = b = 1.0
    if edge:
        g -= 0.2
    if "thread" in obj.name:
        b -= 0.3
    return r, g, b


def build(kit):
    rng = random.Random(41070)
    # elbow eye (forged), sits on the main arm's elbow boss
    eye = dc.revolve(kit, [(0.0095, 0.0), (0.0105, 0.0008), (0.0105, 0.0072), (0.0095, 0.0080), (0.0, 0.0080)],
                     (0.0, 0.0, 0.0), (0, 1, 0), 24, PAINT, "eye")
    pin = dc.revolve(kit, [(0.0042, 0.0075), (0.0042, 0.0088), (0.0030, 0.0098), (0.0, 0.0102)],
                     (0.0, 0.0, 0.0), (0, 1, 0), 16, PAINT, "eye pin head", close_start=False)
    # gooseneck: from the eye up to the rod line, then the tube
    path = [(0.006, 0.0070, 0.0), (0.018, 0.0085, 0.0), (0.030, 0.0145, 0.0), (0.042, 0.0245, 0.0),
            (0.054, 0.0335, 0.0), (0.066, 0.0385, 0.0), (0.080, ROD_Y, 0.0), (0.152, ROD_Y, 0.0)]

    def sec(i):
        # flattened forging near the eye (16 x 8 mm) rounding out to the Ø 16 tube
        t = min(1.0, i / 4.0)
        if t >= 1.0:
            return circle(0.0080, 16)
        out = []
        for k in range(16):
            a = 2 * math.pi * k / 16
            u, v = 0.0080 * math.cos(a), 0.0080 * math.sin(a)
            out.append((u, v * (0.5 + 0.5 * t)))
        return out
    tube = dc.sweep_path(kit, path, sec, PAINT, "gooseneck tube")
    # hex lock nut, threaded adjusting rod (chrome)
    hexr = dc.hexagon(0.0190, phase=math.pi / 6)          # flats up and down
    def hx(k):
        return [(y * k + ROD_Y, z * k) for y, z in hexr]
    nut = dc.loft_x(kit, [(0.152, hx(0.9)), (0.153, hx(1.0)), (0.159, hx(1.0)), (0.160, hx(0.9))], CHROME, "lock nut")
    rod = dc.revolve(kit, [(0.0058, 0.0), (0.00635, 0.0006), (0.00635, 0.0752), (0.0058, 0.0760)],
                     (0.158, ROD_Y, 0.0), (1, 0, 0), 16, CHROME, "threaded rod")
    # forged shoe-end fitting: a short rounded block to the vertical stud boss
    blk = dc.loft_x(kit, [(0.232, [(y + ROD_Y, z) for y, z in dc.rounded_rect(0.014, 0.016, 0.004, 3)]),
                          (0.252, [(y + ROD_Y + 0.002, z) for y, z in dc.rounded_rect(0.014, 0.016, 0.004, 3)])],
                    PAINT, "shoe-end fitting")
    stud = dc.revolve(kit, [(0.0080, ROD_Y - 0.008), (0.0090, ROD_Y - 0.007), (0.0090, RISE - 0.0008), (0.0082, RISE), (0.0, RISE)],
                      (B, 0.0, 0.0), (0, 1, 0), 24, PAINT, "stud boss")
    for p in (pin, nut):
        dc.lod2_drop(p)

    dc.finalize(kit, SMOOTH_ANGLE, wear)

    lo, hi = dc.bounds_unity(kit.parts)
    assert hi[1] <= RISE + 0.001 and lo[1] >= -1e-6, "forearm height band"
    assert abs(hi[0] - (B + 0.009)) < 3e-4
    # underside of the rod line beyond 0.08 m from the elbow (where it can pass over the leaf top)
    under = min(dc.to_unity(o.matrix_world @ v.co)[1] for o in kit.parts for v in o.data.vertices
                if dc.to_unity(o.matrix_world @ v.co)[0] > 0.08)
    assert under >= 0.030, under

    dc.anchor(kit, "elbow", 0.0, 0.0, 0.0)
    dc.anchor(kit, "shoe_end", B, RISE, 0.0)
    kit.meta["motion"] = {
        "type": "rotate", "axis": [0, 1, 0], "pivot": "origin (elbow)",
        "solver": "FrontRoomsDoorCloserLinkage", "aim": "part +X (plan) -> shoe pivot", "b": B, "rise": RISE,
        "underRodDoorY": round(2.080 + under, 4),
    }
    kit.no_collider()
    kit.tag("interactable", "door", "closer", "office")
    dc.lod_meta(kit, sys.modules[__name__])
