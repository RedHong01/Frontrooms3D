"""Surface door closer body, regular-arm pull-side mount (Office, Run and
Exit members, S face): a dark-bronze painted cast body with a full cover,
cast bullnose end caps, the cover seam, two slotted regulating-valve screws
on the hinge end, the pinion spindle (top) and its cap (bottom)
(10_spec §2.4).

Real-world reference: the 1958-1990s US commercial surface closer of the
LCN 4010 / Norton 1600 class (body about 290 x 65 x 50 mm; ESTIMATE, not
checked against a template, §2.4), mounted on the pull-side top rail with
a regular two-link arm to a shoe on the frame head. Allowed only because
the door is single-acting (02 §5.3). Unbranded: no maker plate, no size
dial.

Origin: PART frame (§1.2): the centre of the body's back face, on the
leaf's S face; front (kit -Y = Unity +Z) = outward from the door. Placed on
the leaf rig at the leaf anchor closer_mount_s (0.022, 2.0275, 0.545) with
localRotation Euler(0, 90, 0), label "Closer body". Part +X then runs
toward the hinge (door -Z), so the hinge end is part +X.

Geometry (part m): body X -0.145..0.145, Y -0.0325..0.0325, Z 0..0.050
(door X 0.022..0.072, Y 1.995..2.060, Z 0.400..0.690). Spindle Ø 12.7 mm at
part (0.025, 0.0325..0.0475, 0.040) = door (0.062, 2.060..2.075, 0.520);
anchor "spindle" (0.025, 0.0405, 0.040) = door (0.062, 2.068, 0.520), where
Kit_DoorCloser_Arm's origin goes. 50 mm proud only above Y 1.995, which
§1.5 allows (closer parts above 2.06 m may stand 0.20 m; the body sits
inside the 0.065 lockset envelope's height band only above 1.99 m).

Budget (§9.1): 3,000 / 1,000 / 150 tris; slots SteelBrown (painted
aluminium; submesh 0) and Chrome. LOD1 exported since the 2026-10-08 fix pass (critic H4). Render-only. kit.meta["motion"] = static.
"""

import random
import sys

import interact_door_common as dc

NAME = "Kit_DoorCloser_Body"
LOD1_RATIO = 0.33
LOD1 = LOD1_RATIO  # fix pass 2026-10-08 (critic H4): FrontRoomsKitImporter honours the sidecar distances since 663e858, so LOD1 = LOD0->LOD1 at d01 and a cull at dcull
LOD2_RATIO = 0.05
LOD_DISTANCES = (1.5, 5.0, 20.0)
SMOOTH_ANGLE = 35.0

PAINT = "Prop_SteelBrown"
CHROME = "Prop_Chrome"

HALF_L, HALF_H, DEPTH = 0.145, 0.0325, 0.050
BODY_END = 0.133
SPINDLE = (0.025, 0.040)          # part (X, Z)
MOUNT = (0.022, 2.0275, 0.545)    # leaf anchor closer_mount_s


def section(o=0.0, groove=True):
    """(Y, Z) section, shrunk by o on the top/bottom/front (back stays on
    the door). Cover seam V-grooves at Z 0.010 on the top and bottom."""
    y = HALF_H - o
    zf = DEPTH - o
    g = 0.0005 if groove else 0.0
    pts = [(-y, 0.0), (-y, 0.0097), (-y + g, 0.010), (-y, 0.0103), (-y, zf), (y, zf),
           (y, 0.0103), (y - g, 0.010), (y, 0.0097), (y, 0.0)]
    rf = 0.006 * (1.0 - 0.5 * max(o, 0.0) / 0.016)
    rb = 0.003
    radii = [rb, 0, 0, 0, rf, rf, 0, 0, 0, rb]
    segs = [4, 1, 1, 1, 12, 12, 1, 1, 1, 4]
    return dc.fillet_polygon(pts, radii, segs, closed=True)


def wear(P, N, edge, obj):
    r = g = b = 1.0
    if edge:
        g -= 0.18
    if obj.get("fr_screw"):
        b -= 0.35
    if "seam" in obj.name:
        b -= 0.3
    return r, g, b


def build(kit):
    rng = random.Random(41050)
    # 1. Body + cast end caps: one loft along part X (paint first -> submesh 0).
    # cast bullnose end caps: a 0.6 mm lip, then a quarter-ellipse to the end face
    import math
    cap = [(0.1340 + 0.011 * math.sin(t * math.pi / 2), -0.0006 + 0.0166 * (1 - math.cos(t * math.pi / 2)))
           for t in [k / 9 for k in range(9, -1, -1)]]
    cap.append((0.1335, -0.0006))
    rings = [(-x, section(o, groove=False)) for x, o in cap]
    rings += [(-BODY_END, section()), (BODY_END, section())]
    rings += [(x, section(o, groove=False)) for x, o in reversed(cap)]
    body = dc.loft_x(kit, rings, PAINT, "body")

    # 2. Cast spindle boss on top, the pinion spindle, the bottom spindle cap.
    sx, sz = SPINDLE
    boss = dc.revolve(kit, [(0.0080, -0.002), (0.0080, 0.0007), (0.0072, 0.0013), (0.0, 0.0013)],
                      (sx, HALF_H, sz), (0, 1, 0), 48, PAINT, "spindle boss", close_start=False)
    spindle = dc.revolve(kit, [(0.00635, 0.0), (0.00635, 0.0142), (0.0058, 0.0150), (0.0, 0.0150)],
                         (sx, HALF_H, sz), (0, 1, 0), 48, CHROME, "spindle", close_start=False)
    capb = dc.revolve(kit, [(0.0078, 0.002), (0.0078, -0.0012), (0.0066, -0.0022), (0.0040, -0.0027), (0.0, -0.0028)],
                      (sx, -HALF_H, sz), (0, 1, 0), 48, PAINT, "spindle cap", close_start=False)
    dc.lod2_drop(boss)
    dc.lod2_drop(capb)
    # spring-power adjusting plug on the latch-end cap (socket plug, painted)
    plug = dc.revolve(kit, [(0.0062, -0.001), (0.0062, 0.0012), (0.0054, 0.0020), (0.0030, 0.0020), (0.0030, 0.0008),
                            (0.0, 0.0008)], (-HALF_L, 0.0, 0.017), (-1, 0, 0), 32, PAINT, "adjusting plug", close_start=False)
    dc.lod2_drop(plug)

    # 3. Two slotted regulating-valve screws on the hinge-end cap face.
    for yy in (-0.0075, 0.0075):
        s = dc.flat_head(kit, (HALF_L, yy, 0.016), (1, 0, 0), CHROME, rng, d=0.0055, proud=0.0003,
                         chamfer=0.0005, host=body, notch=0.0009, name="valve screw")
        dc.lod2_drop(s)

    dc.finalize(kit, SMOOTH_ANGLE, wear)

    # ---- checks (part frame, Unity m) ----------------------------------
    lo, hi = dc.bounds_unity([body])
    assert abs(lo[0] + HALF_L) < 2e-4 and abs(hi[0] - HALF_L) < 2e-4
    assert abs(hi[1] - (HALF_H + 0.0006)) < 3e-4 and abs(lo[2]) < 1e-6 and hi[2] <= DEPTH + 0.0007
    door_spindle = (MOUNT[0] + sz, MOUNT[1] + 0.0405, MOUNT[2] - sx)
    assert all(abs(a - b) < 1e-6 for a, b in zip(door_spindle, (0.062, 2.068, 0.520))), door_spindle
    blo, bhi = dc.bounds_unity(kit.parts)
    assert MOUNT[1] + blo[1] >= 1.990 and MOUNT[1] + bhi[1] <= 2.0760, "body/spindle height band"

    # ---- metadata -------------------------------------------------------
    dc.anchor(kit, "spindle", sx, 0.0405, sz)
    dc.anchor(kit, "spindle_dir", sx, 0.1405, sz)
    dc.anchor(kit, "mount", 0.0, 0.0, 0.0)
    kit.meta["motion"] = {"type": "static", "parent": "Leaf rig", "placeAt": "leaf anchor closer_mount_s",
                          "localRotation": [0, 90, 0], "face": "s"}
    kit.meta["doorRoot"] = {"spindle": [0.062, 2.068, 0.520], "bodyMin": [0.022, 1.995, 0.400], "bodyMax": [0.072, 2.060, 0.690]}
    kit.no_collider()
    kit.tag("interactable", "door", "closer", "office")
    dc.lod_meta(kit, sys.modules[__name__])
