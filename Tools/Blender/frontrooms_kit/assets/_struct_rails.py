"""P4 structures_rail_kits: the rail hardware the swept rails of P1 / K1 stand on (structures_assets G1-b, 2026-11-06).

Period basis (20_research.md F8): 1-1/4 in NPS rail tube (42.2 mm OD) 1-1/2 in off the wall (ADA 1991 4.26.2), wall
brackets of cast stainless on a three-screw flange, posts on bolted base flanges or slab-edge side mounts, returns to the
wall at the ends (ADA 4.8.5 / 4.9.4: the extension's end returns or terminates), bent elbows at the run ends; glass in an
aluminium shoe under a cap rail (1988 UBC). Metals by level: pool satin stainless (the base asset), Run! / service / Level
0 painted steel (_Painted, Prop_RailEnamel), Office putty enamel (_Putty); glass hardware satin aluminium, _Stainless in the pool.
Every kit: render only (no collider: the K1 guard keeps its one collider), origin and axes in UNITY space as named in the
journal (W/structures_assets/journal.txt, 2026-11-06 G1-b NAMES).
"""

import math

from mathutils import Vector

import _struct_common as S

R = S.TUBE_R
# _Painted wears Prop_RailEnamel (P3): Painted_Metal's own maps at 0.30 m a tile with an enamel gloss; at Painted_Metal's 1 m tile
# the 0.35 m hero read lumpy plaster on the bracket and the flange (G1 review FIX 4)
SS, PAINT, PUTTY, ALU = "Prop_StainlessSatin", "Prop_RailEnamel", "Prop_SteelPutty", "Trans_AluminiumSatin"
METAL_VARIANTS = lambda name: {name + "_Painted": {SS: PAINT}, name + "_Putty": {SS: PUTTY}}
GLASS_VARIANTS = lambda name: {name + "_Stainless": {ALU: SS}}
SMALL = (2.5, 8.0, 28.0)


def flange(kit, centre, normal_axis, r, t, slot, lv, screws=3, screw_r=0.0042, screw_ring=None, phase=90.0, name="flange"):
    """A round wall / floor flange of radius r, thickness t, its back on the mounting plane at centre, facing +normal.
    normal_axis: '+z' (a wall behind at -z) or '+y' (a floor). A raised boss and a chamfered rim; countersunk screws."""
    ax = normal_axis[1]
    prof = [(r, 0.0), (r, t * 0.55), (r - 0.0015, t * 0.85), (r - 0.004, t), (r * 0.55, t * 1.05), (r * 0.42, t * 1.45), (0.0, t * 1.45)]
    if lv == 1:
        prof = [(r, 0.0), (r, t * 0.7), (r - 0.003, t), (r * 0.42, t * 1.45), (0.0, t * 1.45)]
    elif lv >= 2:
        prof = [(r, 0.0), (r, t), (0.0, t * 1.2)]
    S.lathe_axis(kit, prof, centre, ax, slot, lv, name=name, close_bottom=True)
    if lv >= 2 or screws <= 0:
        return
    ring = screw_ring if screw_ring is not None else r * 0.68
    c = Vector(centre)
    for k in range(screws):
        a = math.radians(phase + 360.0 * k / screws)
        if ax == "z":
            p = c + Vector((ring * math.cos(a), ring * math.sin(a), t * 0.98))
        else:
            p = c + Vector((ring * math.cos(a), t * 0.98, ring * math.sin(a)))
        S.screw_head(kit, screw_r, tuple(p), "+" + ax, slot, lv, name="flange screw", dome=0.22)


def saddle(kit, centre, slot, lv, length=0.040, span_deg=110.0, t=0.003, name="saddle"):
    """A curved cradle under a rail (axis along Unity X through centre): an arc plate round the tube's bottom, thickness t."""
    m = S.Mesh()
    n = max(4, S.arc_steps(R + t, math.radians(span_deg), lv))
    a0, a1 = math.radians(-90.0 - span_deg / 2), math.radians(-90.0 + span_deg / 2)
    c = Vector(centre)
    x0, x1 = c.x - length / 2, c.x + length / 2
    def p(r, a, x):
        return (x, c.y + r * math.sin(a), c.z + r * math.cos(a))
    for k in range(n):
        aa = a0 + (a1 - a0) * k / n
        ab = a0 + (a1 - a0) * (k + 1) / n
        ua, ubb = (R + t) * (aa - a0), (R + t) * (ab - a0)
        # outer face (normal away from the tube), inner face (toward the tube)
        m.quad(p(R + t, aa, x0), p(R + t, aa, x1), p(R + t, ab, x1), p(R + t, ab, x0), (0, ua), (length, ua), (length, ubb), (0, ubb))
        m.quad(p(R + 0.0002, aa, x0), p(R + 0.0002, ab, x0), p(R + 0.0002, ab, x1), p(R + 0.0002, aa, x1), (0, ua), (0, ubb), (length, ubb), (length, ua))
        # the two ends
        m.quad(p(R + 0.0002, aa, x0), p(R + t, aa, x0), p(R + t, ab, x0), p(R + 0.0002, ab, x0), (0, ua), (t, ua), (t, ubb), (0, ubb))
        m.quad(p(R + 0.0002, aa, x1), p(R + 0.0002, ab, x1), p(R + t, ab, x1), p(R + t, aa, x1), (0, ua), (0, ubb), (t, ubb), (t, ua))
    for (aa, sgn) in ((a0, -1), (a1, 1)):
        q = [p(R + 0.0002, aa, x0), p(R + 0.0002, aa, x1), p(R + t, aa, x1), p(R + t, aa, x0)]
        if sgn < 0:
            m.quad(q[0], q[1], q[2], q[3], (0, 0), (length, 0), (length, t), (0, t))
        else:
            m.quad(q[0], q[3], q[2], q[1], (0, 0), (0, t), (length, t), (length, 0))
    m.part(kit, name, slot)
    if lv == 0:
        # two oval-head screws up into the rail through the saddle's bottom
        for sx in (-0.011, 0.011):
            S.screw_head(kit, 0.0028, (c.x + sx, c.y - R - t, c.z), "-y", slot, lv, name="saddle screw", dome=0.3)


# ----------------------------------------------------------------------------- the kits
def bracket_wall(kit, lv):
    """Kit_RailBracket_Wall: a cast stainless swan-neck bracket on a three-screw flange, the rail axis at the origin."""
    flange_y = -0.085
    fz = S.WALL_Z
    flange(kit, (0.0, flange_y, fz), "+z", 0.035, 0.006, SS, lv, screws=3)
    # the arm: out of the boss, a bend up, into the saddle
    ar = 0.0075
    zb = fz + 0.0087
    rb = 0.022
    pts = [(0.0, flange_y, zb), (0.0, flange_y, -rb)]
    n = max(3, S.arc_steps(rb, math.pi / 2, lv))
    for k in range(1, n + 1):
        a = (math.pi / 2) * k / n
        pts.append((0.0, flange_y + rb - rb * math.cos(a), -rb + rb * math.sin(a)))
    pts.append((0.0, -R - 0.0028, 0.0))
    S.tube(kit, "bracket arm", pts, ar, SS, lv, cap0=True, cap1=True, lo=8)
    # a cast fillet collar where the arm leaves the boss
    S.cyl_z(kit, ar * 1.55, 0.006, (0.0, flange_y, zb + 0.003), SS, lv, w=0.0012, name="arm collar")
    saddle(kit, (0.0, 0.0, 0.0), SS, lv, length=0.042)


def bracket_post_top(kit, lv):
    """Kit_RailBracket_PostTop: a post cap with a neck and a saddle; the rail axis 40 mm over the post top (origin)."""
    rail_y = 0.040
    prof = [(R + 0.0010, 0.0), (R + 0.0010, 0.0035), (R - 0.0010, 0.0060), (0.0105, 0.0080), (0.0080, 0.0095), (0.0, 0.0096)]
    if lv >= 1:
        prof = [(R + 0.0010, 0.0), (R + 0.0010, 0.0040), (0.0100, 0.0085), (0.0, 0.0090)]
    S.lathe_y(kit, prof, (0.0, 0.0, 0.0), SS, lv, name="post cap")
    S.cyl_y(kit, 0.0080, rail_y - R - 0.003 - 0.0085, (0.0, 0.0085 + (rail_y - R - 0.003 - 0.0085) / 2, 0.0), SS, lv, w=0.0008, name="saddle neck")
    saddle(kit, (0.0, rail_y, 0.0), SS, lv, length=0.050, span_deg=120.0)


def base_flange(kit, lv):
    """Kit_RailPost_BaseFlange: a 4 in flange 8 mm thick with a socket collar and four anchor studs with hex nuts."""
    t = 0.008
    prof = [(0.051, 0.0), (0.051, t * 0.75), (0.0495, t), (0.026, t), (0.0252, t + 0.003), (0.0252, 0.043), (0.0242, 0.045), (R + 0.0003, 0.045), (R + 0.0003, 0.040)]
    if lv == 1:
        prof = [(0.051, 0.0), (0.051, t), (0.0252, t), (0.0252, 0.045), (R + 0.0003, 0.045)]
    elif lv >= 2:
        prof = [(0.051, 0.0), (0.051, t), (0.0252, t), (0.0252, 0.045), (R, 0.045)]
    S.lathe_y(kit, prof, (0.0, 0.0, 0.0), SS, lv, name="base flange", close_top=False)
    if lv >= 2:
        return
    for k in range(4):
        a = math.radians(45.0 + 90.0 * k)
        x, z = 0.038 * math.cos(a), 0.038 * math.sin(a)
        kit.cylinder(0.0110, 0.0016, S.ub((x, t + 0.0008, z)), SS, verts=S.segs(0.0110, lv, 8), bevel=0.0, segments=0, name="washer")
        S.hex_nut(kit, 0.0170, 0.0080, (x, t + 0.0016 + 0.004, z), "y", SS, lv, name="anchor nut")
        if lv == 0:
            kit.cylinder(0.0048, 0.0045, S.ub((x, t + 0.0096 + 0.00225, z)), SS, verts=12, bevel=0.0006, segments=1, name="anchor stud end")


def side_mount(kit, lv):
    """Kit_RailPost_SideMount: a 200 x 100 x 10 plate bolted to the slab edge, a sleeve round the post on two webs."""
    face = -0.075
    pt = 0.010
    S.box(kit, (0.100, 0.200, pt), (0.0, -0.100, face + pt / 2), SS, lv, w=0.0015, name="side plate")
    if lv < 2:
        for (x, y) in ((-0.032, -0.030), (0.032, -0.030), (-0.032, -0.170), (0.032, -0.170)):
            S.hex_nut(kit, 0.0190, 0.0075, (x, y, face + pt + 0.00375), "z", SS, lv, name="plate bolt")
            if lv == 0:
                kit.cylinder(0.0125, 0.0015, S.ub((x, y, face + pt + 0.00075 - 0.0001)), SS, verts=S.segs(0.0125, lv, 8), rot=(90, 0, 0), bevel=0.0, segments=0, name="washer")
    # the sleeve round the post (the post passes through it), 4 mm wall, chamfered ends
    so = R + 0.0042
    prof = [(R + 0.0003, -0.200), (so - 0.0012, -0.200), (so, -0.1988), (so, -0.0012), (so - 0.0012, 0.0), (R + 0.0003, 0.0)]
    if lv >= 1:
        prof = [(R + 0.0003, -0.200), (so, -0.200), (so, 0.0), (R + 0.0003, 0.0)]
    S.lathe_y(kit, prof, (0.0, 0.0, 0.0), SS, lv, name="post sleeve", close_top=False, close_bottom=False)
    # two webs from the plate to the sleeve
    depth = (face + pt) - (-so + 0.002)
    for y in (-0.165, -0.035):
        S.box(kit, (0.012, 0.040, abs(depth)), (0.0, y, (face + pt + (-so + 0.002)) / 2), SS, lv, w=0.001, name="web")


def return_wall(kit, lv):
    """Kit_RailReturn_Wall: the run's end (from -X, ending at x 0) turned to the wall in a cast short-radius elbow and a
    flange. The other hand is the same kit at scale (-1, 1, 1)."""
    rb = 0.030
    fz = S.WALL_Z
    n = max(3, S.arc_steps(rb, math.pi / 2, lv))
    pts = [(-0.004, 0.0, 0.0), (0.0, 0.0, 0.0)]
    for k in range(1, n + 1):
        a = (math.pi / 2) * k / n
        pts.append((rb * math.sin(a), 0.0, -rb + rb * math.cos(a)))
    pts.append((rb, 0.0, fz + 0.0075))
    S.tube(kit, "return", pts, R, SS, lv, cap0=True, cap1=False, lo=8)
    flange(kit, (rb, 0.0, fz), "+z", 0.035, 0.006, SS, lv, screws=3, phase=30.0)


def elbow_90(kit, lv):
    """Kit_RailElbow_90: a mandrel bend R 63 mm (1.5 D) between a rail along -X and a post going down -Y."""
    rb = 0.063
    n = max(4, S.arc_steps(rb, math.pi / 2, lv))
    pts = []
    for k in range(n + 1):
        a = (math.pi / 2) * k / n
        # from (-rb, 0, 0) heading +X, to (0, -rb, 0) heading -Y, centre (-rb, -rb, 0)
        pts.append((-rb + rb * math.sin(a), -rb + rb * math.cos(a), 0.0))
    S.tube(kit, "bend", pts, R, SS, lv, cap0=False, cap1=False, lo=8)


def end_cap(kit, lv):
    """Kit_RailEnd_Cap: a welded, polished domed end cap on a tube along -X ending at x 0."""
    h = 0.0065
    n = 4 if lv == 0 else 2 if lv == 1 else 1
    prof = [(R, -0.003)]
    for k in range(n + 1):
        a = (math.pi / 2) * k / n
        prof.append((R * math.cos(a) if k < n else 0.0, h * math.sin(a)))
    S.lathe_axis(kit, prof, (0.0, 0.0, 0.0), "x", SS, lv, name="end cap", close_bottom=False)


# Glass shoe and cap rail sections PROPOSED to P1 (journal 2026-11-06 G1-b NAMES): shoe 64 W x 105 H, glass 12 mm centred.
SHOE_W, SHOE_H, SHOE_SLOT, SHOE_FLOOR = 0.064, 0.105, 0.022, 0.025
CAP_W, CAP_H, CAP_SLOT, CAP_SLOT_D = 0.050, 0.038, 0.016, 0.025


def shoe_end_cap(kit, lv):
    """Kit_GlassShoe_EndCap: a 3 mm cast aluminium plate the shoe's U section + 1 mm, two countersunk screws."""
    t = 0.003
    hw, sw = SHOE_W / 2 + 0.001, SHOE_SLOT / 2
    outline = [(-hw, 0.0), (hw, 0.0), (hw, SHOE_H + 0.001), (sw + 0.0045, SHOE_H + 0.001), (sw, SHOE_H - 0.003),
               (sw, SHOE_FLOOR), (-sw, SHOE_FLOOR), (-sw, SHOE_H - 0.003), (-sw - 0.0045, SHOE_H + 0.001), (-hw, SHOE_H + 0.001)]
    # outline in Unity (z, y) on the plane x in [0, t]: kitlib extrude 'yz' takes (Blender y, z) = (-Unity z, Unity y)
    ol = [(-z, y) for (z, y) in outline]
    bw, bs = S.bev(0.0008, lv)
    kit.extrude(ol, t, S.ub((t / 2, 0.0, 0.0)), ALU, plane="yz", bevel=bw, segments=bs, name="shoe end cap")
    if lv == 0:
        for z in (-0.0215, 0.0215):
            S.screw_head(kit, 0.0028, (t, 0.050, z), "+x", ALU, lv, name="cap screw", dome=0.12)


def glass_clamp(kit, lv):
    """Kit_GlassClamp: a cast D clamp on a round post, a 12 mm pane's edge in its jaws (pane plane z 0, edge x +0.045)."""
    h = 0.050
    # back plate curved to the post (an arc of the post + 0.5 mm), front plate screwed to it, the pane between
    m = S.Mesh()
    n = max(4, S.arc_steps(R + 0.0005, math.radians(100), lv))
    r0 = R + 0.0005
    for k in range(n):
        a0 = math.radians(-50 + 100.0 * k / n)
        a1 = math.radians(-50 + 100.0 * (k + 1) / n)
        p = lambda a, y: (r0 * math.cos(a), y, r0 * math.sin(a))
        m.quad(p(a0, -h / 2), p(a1, -h / 2), p(a1, h / 2), p(a0, h / 2), (0, 0), (0.001, 0), (0.001, h), (0, h))
    m.part(kit, "clamp seat", ALU)
    bw = 0.0010 if lv == 0 else 0.0
    for (zc, name) in ((-0.0108, "clamp back jaw"), (0.0108, "clamp front jaw")):
        kit.box((0.050, 0.008, h), S.ub((0.0215 + 0.025, 0.0, zc)), ALU, bevel=bw, segments=2 if lv == 0 else 0, name=name)
    kit.box((0.012, 0.029, h), S.ub((0.0215 + 0.006, 0.0, 0.0)), ALU, bevel=bw, segments=2 if lv == 0 else 0, name="clamp throat")
    if lv < 2:
        for y in (-0.013, 0.013):
            S.screw_head(kit, 0.0030, (0.058, y, 0.0148), "+z", ALU, lv, name="clamp screw", dome=0.15)
    if lv == 0:
        # black gasket strips either side of the pane in the jaws
        for z in (-0.0064, 0.0064):
            kit.box((0.026, 0.0008, h - 0.004), S.ub((0.058, 0.0, z)), "Prop_Rubber", bevel=0.0, segments=0, name="gasket")


def cap_rail_end(kit, lv):
    """Kit_CapRail_EndCap: a cast end closing the cap rail (50 x 38, round top) over the glass, rail along -X."""
    t = 0.004
    r = CAP_W / 2
    n = 12 if lv == 0 else 6 if lv == 1 else 3
    top = [(r * math.cos(math.pi * k / n), CAP_H - r + r * math.sin(math.pi * k / n)) for k in range(n + 1)]
    sw = CAP_SLOT / 2
    outline = [(r, 0.0), (r, CAP_H - r)] + top[1:-1] + [(-r, CAP_H - r), (-r, 0.0), (-sw, 0.0), (-sw, CAP_SLOT_D - 0.010), (sw, CAP_SLOT_D - 0.010), (sw, 0.0)]
    ol = [(-z, y) for (z, y) in outline]
    bw, bs = S.bev(0.0012, lv)
    kit.extrude(ol, t, S.ub((t / 2, 0.0, 0.0)), ALU, plane="yz", bevel=bw, segments=bs, name="cap rail end")
