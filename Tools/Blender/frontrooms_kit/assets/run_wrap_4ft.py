"""4 ft two-lamp wraparound fluorescent, surface-mounted (the 1960s-90s back-of-house
fixture). PRODUCTION hero kit for Run! B (Kit_WrapFixture_4ft), rebuilt 2026-10-22: the
r8 version was a 200-triangle blockout.

A white baked-enamel steel channel (back pan) on the deck, a 2.5 mm prismatic acrylic
lens that wraps the bottom and both sides on a 22 mm radius (the prisms are in the
normal map), stamped steel end caps that follow the lens section with an embossed
panel, two cap screws per end and a lens-retaining thumb screw under each cap.

1.22 x 0.25 x 0.08 m. ORIGIN = top centre (the mounting plane on the soffit): the body
hangs below it to z = -0.08 (Unity y = -0.08). Long axis along X (Blender and Unity).
The lens has authored UVs into Run_WrapLens (1024 x 512): u = along the fixture
(0 at x = -0.61), v = the outer arc length from the +Y top edge (0) to the -Y top edge
(1), total 0.32838 m (gen_run_b_prod.py draws the two lamp bands at v 0.3624 / 0.6376).
Anchor "light" = the lens bottom centre. Budget 2,400 / 960 / 160; lodDistances 4 / 12 / never.
Slots: Painted_Metal, Run_WrapLens, Prop_SteelAlmond (screws).
"""

import math

import _run_b_common as B

NAME = "Kit_WrapFixture_4ft"
SMOOTH_ANGLE = 32.0
BUDGET = (2400, 960, 160)

PAN = "Painted_Metal"
LENS = "Run_WrapLens"
SCREW = "Prop_SteelAlmond"

L = 1.22
P_TOP = (0.1190, -0.0130)
P_SIDE = (0.1040, -0.0580)
C_ARC = (0.0820, -0.0580)
R_ARC = 0.022
Z_BOT = -0.0800
LENS_T = 0.0025
ARC_TOTAL = 0.32838


def outer_profile(lv, offset=0.0):
    """Right top -> bottom -> left top, offset outward by ``offset``."""
    n = {0: 6, 1: 3, 2: 1}[lv]
    right = [(P_TOP[0] + offset, P_TOP[1]), (P_SIDE[0] + offset, P_SIDE[1])]
    for k in range(1, n + 1):
        a = math.radians(-90.0 * k / n)
        right.append((C_ARC[0] + (R_ARC + offset) * math.cos(a), C_ARC[1] + (R_ARC + offset) * math.sin(a)))
    left = [(-y, z) for (y, z) in reversed(right)]
    return right + left


def arc_params(pts):
    s = [0.0]
    for i in range(1, len(pts)):
        s.append(s[-1] + math.hypot(pts[i][0] - pts[i - 1][0], pts[i][1] - pts[i - 1][1]))
    return s


def _lens(kit, lv):
    outer = outer_profile(lv)
    inner = outer_profile(lv, -LENS_T)
    s = arc_params(outer)
    total = s[-1]
    nx = {0: 8, 1: 2, 2: 1}[lv]
    x0 = -L / 2 + 0.0015
    x1 = L / 2 - 0.0015
    xs = [x0 + (x1 - x0) * i / nx for i in range(nx + 1)]
    m = len(outer)
    verts, faces, uvs = [], [], []
    # outer grid
    for x in xs:
        for (y, z) in outer:
            verts.append((x, y, z))
    base_in = len(verts)
    for x in xs:
        for (y, z) in inner:
            verts.append((x, y, z))

    def uv(i, j):
        return ((xs[i] + L / 2) / L, s[j] / total)

    for i in range(nx):
        for j in range(m - 1):
            a, b = i * m + j, i * m + j + 1
            c, d = (i + 1) * m + j + 1, (i + 1) * m + j
            faces.append((a, b, c, d))            # outward (checked by recalc below)
            uvs += [uv(i, j), uv(i, j + 1), uv(i + 1, j + 1), uv(i + 1, j)]
            a2, b2, c2, d2 = base_in + a, base_in + b, base_in + c, base_in + d
            faces.append((a2, d2, c2, b2))        # inner face
            uvs += [uv(i, j), uv(i + 1, j), uv(i + 1, j + 1), uv(i, j + 1)]
    # the two top edges (tucked under the pan) and the two end faces
    for i in range(nx):
        for j in (0, m - 1):
            a, b = i * m + j, (i + 1) * m + j
            faces.append((a, b, base_in + b, base_in + a))
            uvs += [uv(i, j)] * 4
    for i in (0, nx):
        for j in range(m - 1):
            a, b = i * m + j, i * m + j + 1
            faces.append((a, b, base_in + b, base_in + a))
            uvs += [uv(i, j)] * 4
    obj = B.mesh_part(kit, "wrap lens", verts, faces, uvs, LENS)
    B.flip_check(obj)
    return obj


def _endcap_outline(lv):
    # The lens section offset 2 mm outward, closed along the mounting plane.
    pts = outer_profile(lv, 0.002)
    yr = pts[0][0] + 0.004
    return [(yr, 0.0)] + [(y, z - 0.0) for (y, z) in pts] + [(-yr, 0.0)]


def _level(kit, lv):
    bw, bs = B.bev(0.0015, lv)
    kit.box((L, 0.250, 0.0130), (0, 0, -0.0065), PAN, bevel=bw, segments=bs, name="back pan")
    _lens(kit, lv)
    for sx in (-1, 1):
        x = sx * (L / 2 + 0.00075)
        ol = _endcap_outline(lv)
        kit.extrude(ol, 0.0015, (x, 0, 0), PAN, plane="yz", bevel=0.0006 if lv == 0 else 0.0, segments=2 if lv == 0 else 0, name="end cap")
        if lv == 0:
            # Embossed panel on the cap's outer face (a stamped bead), and the cap screws.
            inset = [(y * 0.78, z * 0.78 - 0.004) for (y, z) in outer_profile(lv, 0.0)]
            inset = [(inset[0][0], -0.010)] + inset + [(inset[-1][0], -0.010)]
            kit.extrude(inset, 0.0008, (sx * (L / 2 + 0.0015 + 0.0004), 0, 0), PAN, plane="yz", bevel=0.0003, segments=2, name="end cap bead")
            for sy in (-1, 1):
                B.lathe(kit, [(0.0032, 0.0), (0.0031, 0.0007), (0.0022, 0.0014), (0.0, 0.0016)], (sx * (L / 2 + 0.0015), sy * 0.085, -0.020), SCREW, lv=lv, rot=(0, sx * 90, 0), name="cap screw", lo=12)
        if lv < 2:
            # Lens-retaining thumb screw through the cap, low on the section.
            B.cyl(kit, 0.0045, 0.006, (sx * (L / 2 + 0.0045), 0.0, -0.062), SCREW, lv=lv, rot=(0, 90, 0), w=0.0006, name="thumb screw", lo=12)


def build(kit):
    B.levels(kit, _level)
    kit.no_collider()
    kit.anchor("light", (0, 0, Z_BOT - 0.002))
    kit.tag("ceiling", "run", "run_b")
    kit.min_ceiling = 0.0
    B.lod_meta(kit, (4.0, 12.0, 0.0), BUDGET)
