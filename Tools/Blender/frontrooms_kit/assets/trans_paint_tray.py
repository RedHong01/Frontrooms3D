"""Stamped-steel roller tray with a 9-inch roller resting in it (level
transitions, V2 Renovation: the drop being painted).

Real-world reference: the plain steel paint tray of the 1960s-80s (no plastic
liner): 0.36 x 0.27 m, a 50 mm deep well at one end, a ribbed ramp rising to
the rim, a rolled edge and two wire legs under the shallow end. The roller is
a cage-less wire frame with a turned handle and a 9" (0.23 m) nap cover loaded
with the Office's green-grey paint; paint in the well, drips on the ramp.
Unbranded.

Budget ~2 k tris. Slots: Aluminium (steel), PaintWet, RollerNap, PlasticBlack.
"""

import math
import bmesh

import kitlib

NAME = "Kit_TransPaintTray"
LOD1 = 0.5
SMOOTH_ANGLE = 40.0

kitlib.register_slot("Prop_PaintWet", (0.62, 0.63, 0.57), 0.25)
kitlib.register_slot("Prop_RollerNap", (0.80, 0.80, 0.76), 0.9)

STEEL, PAINT, NAP, GRIP = "Prop_Aluminium", "Prop_PaintWet", "Prop_RollerNap", "Prop_PlasticBlack"
L, W, RIM = 0.36, 0.27, 0.065
T = 0.0009


def _floor(x):
    """Inside floor height along the tray (x from the well end, 0..L)."""
    if x < 0.11:
        return 0.006
    return 0.006 + (RIM - 0.010 - 0.006) * min(1.0, (x - 0.11) / (L - 0.13))


def _shell(kit):
    """Floor + side walls + end walls as one thin sheet with a rolled rim."""
    bm = bmesh.new()
    xs = [0.0, 0.008, 0.10, 0.11] + [0.11 + (L - 0.13) * i / 10 for i in range(1, 11)] + [L - 0.008, L]
    xs = sorted(set(round(x, 5) for x in xs))
    hw = W / 2
    # Cross-sections: floor at _floor(x), walls flare 6 mm outward to the rim.
    sections = []
    for x in xs:
        fz = _floor(x) if 0.004 < x < L - 0.004 else RIM - 0.004
        sec = [(x, -hw - 0.006, RIM), (x, -hw + 0.004, fz + 0.004), (x, -hw + 0.012, fz),
               (x, hw - 0.012, fz), (x, hw - 0.004, fz + 0.004), (x, hw + 0.006, RIM)]
        sections.append([bm.verts.new(p) for p in sec])
    for a, b in zip(sections, sections[1:]):
        for i in range(len(a) - 1):
            bm.faces.new((a[i], a[i + 1], b[i + 1], b[i]))
    # End walls (well end at x=0, shallow end at x=L).
    for sec, flip in ((sections[0], True), (sections[-1], False)):
        top0 = bm.verts.new((sec[0].co.x, sec[0].co.y, RIM))
        f = bm.faces.new(list(reversed(sec)) if flip else sec)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    obj = kit._new_object("tray", bm, STEEL, "metres", "xz")
    mod = obj.modifiers.new("solid", "SOLIDIFY")
    mod.thickness = T
    mod.offset = -1
    return obj


def build(kit):
    tray = _shell(kit)
    tray.location = (-L / 2, 0, 0)
    # Rolled rim: four tubes round the top edge.
    hw = W / 2 + 0.006
    r = [(-L / 2 - 0.004, -hw, RIM), (L / 2 + 0.004, -hw, RIM), (L / 2 + 0.004, hw, RIM), (-L / 2 - 0.004, hw, RIM), (-L / 2 - 0.004, -hw, RIM)]
    kit.tube(r, 0.0028, STEEL, verts=8, name="rim")
    # Ramp ribs.
    for i in range(7):
        x = 0.14 + i * 0.028
        z = _floor(x) + 0.001
        ang = math.degrees(math.atan2(RIM - 0.016, L - 0.13))
        kit.box((0.006, W - 0.04, 0.003), (x - L / 2, 0, z), STEEL, bevel=0.0012, segments=1, rot=(0, -ang, 0), name="rib")
    # Wire legs under the shallow end.
    for sy in (-1, 1):
        kit.tube([(L / 2 - 0.03, sy * 0.10, 0.0015), (L / 2 - 0.03, sy * 0.10, _floor(L - 0.03) - 0.002)], 0.0022, STEEL, verts=8, name="leg")
    kit.tube([(L / 2 - 0.03, -0.10, 0.0015), (L / 2 - 0.03, 0.10, 0.0015)], 0.0022, STEEL, verts=8, name="leg foot")
    # Paint in the well: flat surface at 38 mm, meeting the ramp.
    pz = 0.038
    x_end = 0.11 + (pz - 0.006) / (RIM - 0.016) * (L - 0.13)
    outline = [(0.006, 0.007), (x_end - 0.004, 0.007), (x_end, pz), (0.006, pz)]
    kit.extrude([(x - L / 2, z) for x, z in outline], W - 0.03, (0, 0, 0), PAINT, plane="xz", bevel=0.0, name="paint")
    # Drips and a loaded stripe on the ramp.
    for i, (x0, w) in enumerate(((0.17, 0.05), (0.23, 0.035), (0.28, 0.02))):
        kit.box((w, 0.09 - i * 0.02, 0.0012), (x0 - L / 2, -0.02 + 0.03 * i, _floor(x0) + 0.0016), PAINT, bevel=0.0005, segments=1,
                rot=(0, -math.degrees(math.atan2(RIM - 0.016, L - 0.13)), 0), name="paint on ramp")
    # Roller resting on the ramp, handle out over the shallow end and up.
    rx, ry = 0.20 - L / 2, 0.0
    rz = _floor(0.20) + 0.031
    kit.cylinder(0.031, 0.23, (rx, ry, rz), NAP, verts=20, rot=(90, 0, 0), bevel=0.006, segments=2, name="roller cover")
    for sy in (-1, 1):
        kit.cylinder(0.024, 0.006, (rx, ry + sy * 0.118, rz), STEEL, verts=16, rot=(90, 0, 0), bevel=0.0015, name="end cap")
    frame = [(rx, ry + 0.122, rz), (rx, ry + 0.135, rz), (rx + 0.02, ry + 0.150, rz + 0.01), (rx + 0.09, ry + 0.150, rz + 0.04),
             (rx + 0.15, ry + 0.06, rz + 0.07), (rx + 0.21, ry + 0.02, rz + 0.09)]
    kit.tube(frame, 0.0032, STEEL, verts=8, name="frame")
    hx, hz = rx + 0.21, rz + 0.09
    kit.cylinder(0.014, 0.13, (hx + 0.06, ry + 0.02 - 0.006, hz + 0.022), GRIP, verts=14, rot=(0, 70, -8), bevel=0.004, name="handle")
    kit.collider((0, 0, 0.06), (L + 0.08, W + 0.02, 0.12))
    kit.tag("renovation", "work_prop")
