"""Surface closer arm shoe: the small bracket screwed to the S-face head of
the steel frame, whose ear carries the forearm's vertical pivot stud
(10_spec §2.4).

Real-world reference: the regular-arm shoe of a 1960s-1990s surface closer
(a cast or pressed bracket, two #12 screws into the frame head), painted
dark bronze to match the arm. Unbranded.

Origin: PART frame: the shoe pivot itself (door (0.125, 2.130, 0.100) = the
frame's "closer_shoe" anchor): the top of the forearm's stud, on its
vertical axis. Front (kit -Y = Unity +Z) = out of the frame face (door +X).
Placed as a child of the frame at closer_shoe with localRotation
Euler(0, 90, 0), label "Closer shoe". Its back plate lands on the steel
face band at door X 0.105 (part Z -0.020).

Geometry (part m): back plate 0.030 (X) x 0.040 (Y) x 0.004, part Z
-0.020..-0.016; a 6 mm ear (Y 0..0.006) from the plate out round the pivot
(R 0.011); a slotted pan-head pivot screw on the ear; two countersunk
slotted screws in the plate. The forearm stud's top (Y 0) meets the ear's
underside.

Budget (§9.1): 500 / 200 / 40 tris; since the 2026-10-08 fix pass about
1,000 LOD0: the ear's round end has 48 segments per circle and the pivot
screw 32 (critic L1, §1.8's ">= 48 on anything <= 0.07 m across" wins over the
500 budget for this 0.3 m close-up part). LOD1 exported (critic H4). Slots
SteelBrown and Chrome. Render-only. kit.meta["motion"] = static.
"""

import math
import random
import sys

import interact_door_common as dc

NAME = "Kit_DoorCloser_Shoe"
LOD1_RATIO = 0.4
LOD1 = LOD1_RATIO  # fix pass 2026-10-08 (critic H4): FrontRoomsKitImporter honours the sidecar distances since 663e858, so LOD1 = LOD0->LOD1 at d01 and a cull at dcull
LOD2_RATIO = 0.08
LOD_DISTANCES = (1.5, 5.0, 20.0)
SMOOTH_ANGLE = 35.0

PAINT = "Prop_SteelBrown"
CHROME = "Prop_Chrome"
FACE_Z = -0.020          # frame face band, part Z
PLATE_T = 0.004
EAR_R = 0.011
EAR_Y = (0.0, 0.006)
EAR_PTS = 35


def wear(P, N, edge, obj):
    r = g = b = 1.0
    if edge:
        g -= 0.2
    if obj.get("fr_screw"):
        b -= 0.35
    return r, g, b


def build(kit):
    rng = random.Random(41080)
    # back plate with 1 mm eased edges
    plate = dc.obox(kit, PAINT, lo=(-0.015, -0.020, FACE_Z), hi=(0.015, 0.020, FACE_Z + PLATE_T),
                    bevel=0.0010, segs=2, name="shoe plate")
    # the ear: an extruded tab (plan outline in X, Z) round the pivot, Y 0..0.006
    zb = FACE_Z + PLATE_T - 0.001                     # root, buried 1 mm in the plate
    a0 = math.degrees(math.atan2(-math.sqrt(EAR_R ** 2 - 0.009 ** 2), 0.009))
    outline = [(-0.009, zb), (0.009, zb)]
    # Fix pass 2026-10-08 (critic L1): the round end had 15 points over its 250 deg arc (about 20 per full
    # circle) and faceted in close-ups; EAR_PTS = 35 gives 48 per full circle (§1.8).
    for k in range(EAR_PTS):
        a = math.radians(a0 + (180 - 2 * a0) * k / (EAR_PTS - 1))  # round end about the pivot (0, 0)
        outline.append((EAR_R * math.cos(a), EAR_R * math.sin(a)))
    ear = dc.loft_y(kit, [(EAR_Y[0], outline), (EAR_Y[1], outline)], PAINT, "shoe ear")
    kit._bevel(ear, 0.0008, 2, angle=40)
    # pivot screw: slotted pan head on the ear, and two plate screws
    piv = dc.oval_head(kit, (0.0, EAR_Y[1], 0.0), (0, 1, 0), CHROME, rng, d=0.0080, dome_k=0.30, half_segs=16,
                       name="pivot screw")                    # 32 round the dome (was 16; L1)
    for yy in (-0.0125, 0.0140):
        dc.flat_head(kit, (0.0, yy, FACE_Z + PLATE_T), (0, 0, 1), CHROME, rng, d=0.0068, host=plate, notch=0.0010,
                     name="shoe screw")
    dc.lod2_drop(piv)

    dc.finalize(kit, SMOOTH_ANGLE, wear)

    lo, hi = dc.bounds_unity(kit.parts)
    assert abs(lo[2] - FACE_Z) < 1e-6, "back plate must sit on the frame face (door X 0.105)"
    assert hi[1] <= 0.0205 and lo[1] >= -0.0205 and abs(lo[0] + 0.015) < 1e-4
    assert hi[2] <= EAR_R + 1e-4

    dc.anchor(kit, "pivot", 0.0, 0.0, 0.0)
    dc.anchor(kit, "pivot_dir", 0.0, -0.10, 0.0)
    dc.anchor(kit, "mount", 0.0, 0.0, FACE_Z)
    kit.meta["motion"] = {"type": "static", "parent": "Door frame (kit)", "placeAt": "frame anchor closer_shoe",
                          "localRotation": [0, 90, 0], "face": "s", "pivotAxis": [0, 1, 0]}
    kit.meta["doorRoot"] = {"pivot": [0.125, 2.130, 0.100], "plateBack": 0.105}
    kit.no_collider()
    kit.tag("interactable", "door", "closer", "office")
    dc.lod_meta(kit, sys.modules[__name__])
