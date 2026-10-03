"""Mortise cylinder housing with an interchangeable core (IC), the static
part of the LOCKED door's keyway (10_spec §3.1; era note R10: "interchange-
able-core (IC) cylinders with the figure-8 core face, generic and
unbranded"). Best-style small-format IC: a satin-chrome collar ring and
housing face, a brass figure-8 core flush with the face, a 0.25 mm dark
groove round the figure-8, the plug bore (Ø 0.0118) in the lower lobe and
the control-lug notch at the top of the upper lobe. No brand, no stamping.

Real-world reference: 1-1/4" mortise cylinder with a 1-3/4" collar ring and
an SFIC core (Best since the 1920s, 02 §13). Size Ø 0.044 x 0.0075.
Core-face dimensions are ESTIMATES (UNVERIFIED against a real SFIC, §13):
lobes Ø 0.0127 at 0.0095 centres.

Origin (PART frame, 10_spec §1.2): THE KEYHOLE = the core-face centre on the
plug axis. Front = Unity +Z (kit -Y), outward. It goes on the escutcheon's
`keyhole` anchor (0.0095 off the leaf face: door X +-0.0315, Y 1.000,
Z 0.920). The collar's back (Z -0.0075) then sits 0.3 mm above the
escutcheon's recess floor. The part is symmetric in X, so the §1.2 mirror on
face _p does not change it.

Anchors: keyhole (0, 0, 0); keyhole_in (0, 0, -0.10) (insertion axis);
keyhole_up (0, 0.10, 0); collar_back (0, 0, -0.0075).
Motion: static.
Budget (§9.2): LOD0 2,600 (asserted +-15 %), LOD1 1,000, LOD2 150; LOD
distances 1.5 / 4 / 12 m; no LOD1 export (part < 1 m, §1.8).
Slots: Prop_Chrome (housing, first), Prop_PlasticBlack (groove, bore and
notch shadows), Prop_Brass (core face). VARIANT _Brass (housing to brass).
Detail: 96-segment collar with a 1 mm rounded, domed front.
"""

import math

import interact_lock_common as lc

NAME = "Kit_Lock_CylinderShell"
LOD1 = None
LOD1_RATIO = 0.385
LOD2_RATIO = 0.058
LOD_DISTANCES = (1.5, 4.0, 12.0)
BUDGET = 2600
VARIANTS = {"Kit_Lock_CylinderShell_Brass": {lc.CHROME: lc.BRASS}}

COLLAR_R, COLLAR_BACK, COLLAR_FRONT = 0.022, -0.0075, -0.0015
FACE_R = 0.0175
LOBE_R, LOBE_D = 0.0127 / 2, 0.0095
GROOVE = 0.00025
GROOVE_FLOOR = -0.0010
BORE_R = 0.0118 / 2


def _housing(kit):
    m = lc.Mesh()
    rr = 0.0010
    c45 = math.radians(45)
    prof = [(0.0, COLLAR_BACK), (COLLAR_R, COLLAR_BACK), (COLLAR_R, -0.0035),
            (COLLAR_R - rr + rr * math.cos(c45), -0.0035 + rr * math.sin(c45)),
            (COLLAR_R - rr, -0.0025),                     # 1 mm round onto the domed front
            (0.0195, -0.0019),                            # the collar's domed front
            (FACE_R + 0.0001, COLLAR_FRONT),
            (FACE_R, -0.0004),
            (FACE_R - 0.0004, 0.0),                       # 0.4 mm chamfer: a crisp face edge
            (0.0, 0.0)]
    m.lathe(prof, 96)
    return m.to_object(kit, "cylinder housing", lc.CHROME)


def _fig8_cut(R, z0, z1):
    m = lc.Mesh()
    m.prism(lc.figure8(R, LOBE_D, 64), z0, z1)
    return m


def _bore_cut(z0, z1, segs=64):
    m = lc.Mesh()
    m.lathe([(0.0, z0), (BORE_R, z0), (BORE_R, z1), (0.0, z1)], segs)
    return m


def _wear_housing(p, n, slot):
    x, y, z = p
    rad = math.hypot(x, y)
    g = 1.0
    if rad > 0.0195 and abs(n[2]) < 0.95:
        g = 0.78                                   # collar's rounded rim
    elif FACE_R - 0.0006 < rad < FACE_R + 0.0002 and z > -0.0005:
        g = 0.85                                   # housing face edge
    r = 0.9 if rad < FACE_R else 1.0
    return (r, g, 1.0)


def _wear_core(p, n, slot):
    x, y, z = p
    g = 0.85 if math.hypot(x, y) < BORE_R + 0.0012 and z > -0.0002 else 1.0   # keys rub the plug lobe
    return (0.92, g, 1.0)


def build(kit):
    lc.assert_keyway()
    housing = _housing(kit)
    lc.boolean(kit, housing, _fig8_cut(LOBE_R + GROOVE, GROOVE_FLOOR, 0.001), slot=lc.DARK)
    lc.boolean(kit, housing, _bore_cut(COLLAR_BACK - 0.001, 0.001, 48), slot=lc.DARK)
    lc.finish_part(housing, _wear_housing, delete_below=COLLAR_BACK + 0.0001)   # back face sits on the escutcheon

    m = lc.Mesh()
    m.prism(lc.figure8(LOBE_R, LOBE_D, 64), GROOVE_FLOOR + 0.00005, 0.0)
    core = m.to_object(kit, "IC core face", lc.BRASS)
    lc.boolean(kit, core, _bore_cut(GROOVE_FLOOR - 0.001, 0.001, 64), slot=lc.DARK)
    lug = lc.Mesh()
    top = LOBE_D + LOBE_R
    lug.prism([(-0.0008, top - 0.0008), (0.0008, top - 0.0008), (0.0008, top + 0.001), (-0.0008, top + 0.001)],
              -0.0007, 0.001)
    lc.boolean(kit, core, lug, slot=lc.DARK)
    lc.finish_part(core, _wear_core, delete_below=GROOVE_FLOOR + 0.0001)

    kit.anchor("keyhole", lc.U(0.0, 0.0, 0.0))
    kit.anchor("keyhole_in", lc.U(0.0, 0.0, -0.10))
    kit.anchor("keyhole_up", lc.U(0.0, 0.10, 0.0))
    kit.anchor("collar_back", lc.U(0.0, 0.0, COLLAR_BACK))
    lc.common_meta(kit, {"type": "static"}, LOD_DISTANCES, LOD1_RATIO, LOD2_RATIO, (BUDGET, 1000, 150))
    kit.tag("cylinder", "lock_static", "ic_core")
    # Key numbers (10_spec §3.1).
    assert abs(LOBE_D + LOBE_R + GROOVE - 0.0161) < 1e-4 and LOBE_D + LOBE_R + GROOVE < FACE_R - 0.0008
    assert BORE_R > 0.0115 / 2 + 0.0001                  # plug Ø 0.0115 turns freely
    lc.check(kit, BUDGET, (-COLLAR_R, -COLLAR_R, COLLAR_BACK), (COLLAR_R, COLLAR_R, 0.0), tol=0.0001)
