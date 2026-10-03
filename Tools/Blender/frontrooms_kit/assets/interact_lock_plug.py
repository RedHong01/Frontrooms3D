"""The IC cylinder's PLUG: the part that turns with the key (10_spec §3.1,
§3.2; Red iii "a real keyable lock as separate animatable parts"). A brass
plug Ø 0.0115 with a 0.3 mm face chamfer (the dark shear line against the
core), the §3.2 keyway broached through the face and open 0.027 deep with
dark walls so the blade is seen going in, six pin chambers drilled down into
the keyway's top, and six domed brass pin tips hanging into the slot at the
cut positions, each 0.2 mm above its cut floor with the key fully home.

Real-world reference: a pin-tumbler plug, US pins-up (02 §4.2). The keyway is
a generic IC-style warded section, not any maker's (ESTIMATE, §13).

Origin (PART frame, 10_spec §1.2): THE KEYHOLE = the plug-face centre on its
axis, the same point as Kit_Lock_CylinderShell's origin and the key's
`shoulder` at full insertion. Front = Unity +Z (kit -Y), outward; the key
goes in along -Z with its cuts up (+Y).

KEYWAY FRAME. The key's local frame is the plug's turned 180 deg about Y
(the key arrives with LookRotation(-out, up)), so this keyway is §3.2's
key-local keyway mirrored in X (interact_lock_common.KEYWAY_PLUG). The ward
rib that matches the blade's right-flat groove therefore sits on the plug's
-X wall.

PLACEMENT (deviation from §1.2's face rule, see the build note): the plug is
chiral. It must never be mirrored in world space, or the same key no longer
fits. Place it like a text part: face _s Euler(0, 90, 0), face _p Euler(0,
-90, 0), both with localScale (S_sign, 1, 1).

Motion: rotate about part -Z, 0 -> 90 deg (positive turns the cuts (+Y)
toward part +X = toward the hinge on face _s; negate on face _p), returning
with the key; a key only leaves at 0 deg.
Anchors: keyhole (0, 0, 0); keyhole_in (0, 0, -0.10); keyhole_up
(0, 0.10, 0); pin_1..pin_6 (the six tip points, Y = cut floor + 0.0002).
Budget (§9.2): LOD0 1,800 (asserted +-15 %), LOD1 700, LOD2 80; LOD
distances 1.0 / 3 / 8 m; no LOD1 export (part < 1 m). Pins are
fr_lod2_drop. Slots: Prop_Brass (first), Prop_PlasticBlack (keyway and
chamber walls). 64-segment plug; pins 12-segment (Ø 2.9 mm = 9 px at 0.3 m).
"""

import math

import interact_lock_common as lc

NAME = "Kit_Lock_Plug"
LOD1 = None
LOD1_RATIO = 0.39
LOD2_RATIO = 0.044
LOD_DISTANCES = (1.0, 3.0, 8.0)
BUDGET = 1800

PLUG_R = 0.0115 / 2
FACE_CH = 0.0003
LENGTH = 0.030
PIN_SEGS = 12


def _body(kit):
    m = lc.Mesh()
    m.lathe([(0.0, -LENGTH), (PLUG_R - 0.0003, -LENGTH), (PLUG_R, -LENGTH + 0.0003),
             (PLUG_R, -FACE_CH), (PLUG_R - FACE_CH, 0.0), (0.0, 0.0)], 64)
    return m.to_object(kit, "plug", lc.BRASS)


def _keyway_cut():
    m = lc.Mesh()
    m.prism(lc.KEYWAY_PLUG, -lc.KEYWAY_DEPTH, 0.001)
    return m


def _mouth_cut():
    """A 0.2 mm lead-in chamfer round the keyway mouth (the key's glint edge)."""
    m = lc.Mesh()
    wide = lc.offset_polygon(lc.KEYWAY_PLUG, 0.0002)
    m.loft([[(x, y, -0.0003) for x, y in lc.KEYWAY_PLUG],
            [(x, y, 0.0) for x, y in wide],
            [(x, y, 0.001) for x, y in wide]])
    return m


def _y_axis(m, start, z):
    """Turn a lathe built about Z into one about +Y at (0, *, z)."""
    m.transform(lambda p: (p[0], p[2], -p[1] + z), start)


def _chambers():
    m = lc.Mesh()
    rc = lc.CHAMBER_D / 2
    for zc in lc.CUT_Z:
        start = len(m.v)
        tip = 0.0022
        m.lathe([(0.0, tip), (rc, tip + rc * math.tan(math.radians(31))), (rc, 0.0075), (0.0, 0.0075)], PIN_SEGS)
        _y_axis(m, start, -zc)
    return m


def _pins(kit):
    m = lc.Mesh()
    r = lc.PIN_D / 2
    for zc, ty in zip(lc.CUT_Z, lc.PIN_TIP_Y):
        start = len(m.v)
        prof = [(0.0, ty)]
        for a in (35.0, 65.0):
            t = math.radians(a)
            prof.append((r * math.sin(t), ty + r - r * math.cos(t)))
        prof += [(r, ty + r), (r, 0.0053), (0.0, 0.0053)]
        m.lathe(prof, PIN_SEGS)
        _y_axis(m, start, -zc)
    return m.to_object(kit, "pin tips", lc.BRASS)


def _wear(p, n, slot):
    x, y, z = p
    g = 1.0
    if z > -0.0004 and slot == lc.BRASS:
        g = 0.80                                   # the key-rubbed plug face
        if abs(x) < 0.0016 and -0.0045 < y < 0.0051:
            g = 0.65                               # the keyway's mouth
    return (0.95, g, 1.0)


def build(kit):
    lc.assert_keyway()
    plug = _body(kit)
    lc.boolean(kit, plug, _keyway_cut(), slot=lc.DARK)
    lc.boolean(kit, plug, _mouth_cut(), slot=lc.DARK)
    lc.boolean(kit, plug, _chambers(), slot=lc.DARK)
    lc.finish_part(plug, _wear)
    pins = _pins(kit)
    lc.finish_part(pins, lambda p, n, s: (1.0, 0.85 if p[1] < min(lc.PIN_TIP_Y) + 0.0006 else 1.0, 1.0), lod2=True)

    kit.anchor("keyhole", lc.U(0.0, 0.0, 0.0))
    kit.anchor("keyhole_in", lc.U(0.0, 0.0, -0.10))
    kit.anchor("keyhole_up", lc.U(0.0, 0.10, 0.0))
    for i, (zc, ty) in enumerate(zip(lc.CUT_Z, lc.PIN_TIP_Y)):
        kit.anchor("pin_%d" % (i + 1), lc.U(0.0, ty, -zc))
    lc.common_meta(kit, {"type": "rotate", "axis": [0, 0, -1], "min": 0, "max": 90, "rest": 0,
                         "turnResistDeg": 10,
                         "note": "+angle turns the cuts (+Y) toward part +X (hinge on face _s); negate on _p. "
                                 "Never mirror this part: place with scale (S_sign,1,1) on both faces."},
                   LOD_DISTANCES, LOD1_RATIO, LOD2_RATIO, (BUDGET, 700, 80))
    kit.meta["keyway"] = {"plugFrame": [list(p) for p in lc.KEYWAY_PLUG], "depth": lc.KEYWAY_DEPTH,
                          "cutZ": list(lc.CUT_Z), "pinTipY": list(lc.PIN_TIP_Y)}
    kit.tag("plug", "lock_moving")
    # Key numbers (10_spec §3.1-3.2).
    assert abs(PLUG_R * 2 - 0.0115) < 1e-9 and abs(lc.KEYWAY_DEPTH - 0.027) < 1e-9
    assert max(math.hypot(x, y) for x, y in lc.KEYWAY_PLUG) < PLUG_R - FACE_CH - 0.0002   # slot inside the face
    assert all(abs(t - (f + 0.0002)) < 1e-9 for t, f in zip(lc.PIN_TIP_Y, lc.CUT_FLOOR))
    lc.check(kit, BUDGET, (-PLUG_R, -PLUG_R, -LENGTH), (PLUG_R, PLUG_R, 0.0), tol=0.0001)
