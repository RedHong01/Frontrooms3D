"""Kit_GuestChairSled (+ _Burgundy): the contract sled-base guest chair with arms that replaces the two
Kit_TaskChair_Navy stand-ins on the W wall of Station_Desk_3x3 (P23, P24), game visual line, workflow
bs4-hero-kits (research/station/72_hero_kits.md section 2.4; palette 03_palette A row "sled-base guest chairs
CONFIRMED", E02 / E10 / E18 / E19 / E01; colour rule 30_look section 2: teal as the one small accent).

Period: a 1986-89 contract guest chair. Each side is ONE 1 in (25.4 mm) OD 16 ga chrome tube bent into a closed
loop: floor runner, 51 mm centreline-radius bends, a vertical front post, the arm, a rear post raked 8 deg forward,
back into the runner. Two 3/4 in (19 mm) chrome cross tubes join the loops under the seat (front posts, rear
posts) with 2 mm weld beads; two chrome flat-bar seat rails (25 x 4.8 mm) run front to back on the cross tubes and
carry the black polypropylene seat pan (4 hex bolt heads under the seat). The plan's four 40 mm tabs cannot reach
the pan here: the cross tubes sit at the posts, 0.50 m apart against a 0.43 m pan, so the rails take their place.
Two 19 mm chrome back uprights rise from the rear cross tube (welded, beads) and follow the back's 10 deg recline
behind its black PP shell, 2 domed chrome bolt heads each, black PP end plugs. Seat pad 0.483 x 0.445 x 0.076,
crowned 8 mm, waterfall front, 5 mm welt cord on the side and back top seam (the pad's own surface), 8 mm sag,
raked 3 deg; back pad 0.47 x 0.305 x 0.057, crowned 6 mm, welt on the front seam, in a black PP shell with a
5 mm lip. Four black nylon snap-on sled glides 44 mm long wrap the runners 6 mm off the floor.

Unity space (x right, y up, z = the sitter's facing; kitlib converts). ORIGIN = floor, footprint centre; front +Z;
x +-0.292, z -0.30..+0.27, top 0.838 (envelope of 72 section 1.2, +-0.02).
Slots Prop_Chrome, Prop_VinylTeal (new, #2C6264), Prop_PlasticBlack: 3 draws. Hand-built LODs (fr_lods):
LOD0 tubes 16 sides, bends every 15 deg; LOD1 8 sides, 30 deg, pads without welts, no sub-5 cm hardware;
LOD2 6-sided tubes with one facet per bend, pads as slabs. Budget 5,000 / 1,700 / 350 (cap 5,300);
lodDistances [5, 12, 50].
"""

import math

import bmesh
from mathutils import Vector

import _station_desk_common as dc

NAME = "Kit_GuestChairSled"
SMOOTH_ANGLE = 62.0          # pads and 6-sided LOD2 tubes shade round; bevelled boxes keep 90 deg corners sharp
VARIANTS = {"Kit_GuestChairSled_Burgundy": {dc.TEAL: dc.BURGUNDY}}

CHROME = "Prop_Chrome"
VINYL = dc.TEAL
BLACK = "Prop_PlasticBlack"

B = dc.B

# --------------------------------------------------------------- frame numbers (metres, Unity space)
R_T = 0.0127                 # 1 in side-loop tube
R_C = 0.009525               # 3/4 in cross tubes and back uprights
R_BEND = 0.051               # 2 in centreline bend radius
XL = 0.292 - R_T             # loop centreline x
Y_RUN = 0.006 + R_T          # runner centreline (glides lift it 6 mm)
Y_ARM = 0.648 - R_T          # arm centreline (arm top 0.648)
Z_FRONT = 0.27 - R_T         # vertical front post
Z_RR = -0.2952               # rear corner of the control polygon: rearmost tube face at -0.300 (solved)
RAKE = math.radians(8.0)     # rear post leans forward going up
Z_ARM_R = Z_RR + (Y_ARM - Y_RUN) * math.tan(RAKE)

SEAT_RAKE = 3.0              # deg, rear lower
SLOPE = math.tan(math.radians(SEAT_RAKE))
Y_XF, Z_XF = 0.3583, Z_FRONT                 # front cross tube centre
Y_XR = 0.3317                                # rear cross tube centre (on the raked rear posts)
Z_XR = Z_RR + (Y_XR - Y_RUN) * math.tan(RAKE)
RAIL_T, RAIL_W, RAIL_X = 0.0048, 0.025, 0.19
RAIL_TOP_F = Y_XF + R_C + RAIL_T             # rail top over the front cross tube


def rail_top(z):
    return RAIL_TOP_F - (Z_XF - z) * SLOPE


# seat pad / pan
SEAT_HW, SEAT_HD, SEAT_R = 0.2415, 0.2225, 0.045
SEAT_ZC = 0.255 - SEAT_HD                    # pad centre z (its waterfall front at 0.255)
PAN_HW, PAN_HD, PAN_R, PAN_T = 0.2335, 0.2145, 0.040, 0.012
PAN_O = (0.0, rail_top(SEAT_ZC), SEAT_ZC)    # pan bottom centre (Unity)
SEAT_UP = Vector((0.0, math.cos(math.radians(SEAT_RAKE)), -math.sin(math.radians(SEAT_RAKE))))
PAD_O = Vector(PAN_O) + SEAT_UP * 0.008      # pad base sits 4 mm down inside the pan rim

# back
BACK_REC = 10.0
BACK_HW, BACK_HD, BACK_R = 0.235, 0.1525, 0.050
SHELL_LIP = 0.005
BU = Vector((0.0, math.cos(math.radians(BACK_REC)), -math.sin(math.radians(BACK_REC))))   # back's up
BN = Vector((0.0, math.sin(math.radians(BACK_REC)), math.cos(math.radians(BACK_REC))))    # back's front normal
PB = Vector((0.0, 0.6783, -0.2329))          # pad base centre: shell top 0.835, shell back top at z -0.270
UPR_X = 0.15
UPR_OFF = R_C + 0.0015                       # upright centreline behind the shell's back face (z -0.012)


def L(t):
    """Upright centreline behind the shell, t along the back's up axis from its centre."""
    return PB + BN * (-0.012 - UPR_OFF) + BU * t


def bl(p):
    """Unity point or direction -> Blender tuple."""
    return B(p[0], p[1], p[2])


# --------------------------------------------------------------- helpers
def loop_points(x, step):
    poly = [(x, Y_RUN, Z_RR), (x, Y_RUN, Z_FRONT), (x, Y_ARM, Z_FRONT), (x, Y_ARM, Z_ARM_R)]
    return dc.fillet(poly, R_BEND, step=step, closed=True)


def side_loops(kit, sides, step, lods, name):
    for s in (-1, 1):
        pts = loop_points(s * XL, step)
        dc.lv(dc.tube(kit, [bl(p) for p in pts], R_T, CHROME, sides=sides, closed=True, caps=False, name=name), lods)


def straight_tube(kit, a, b, r, sides, lods, name, caps=False):
    return dc.lv(dc.tube(kit, [bl(a), bl(b)], r, CHROME, sides=sides, caps=caps, name=name), lods)


def upright_points(x, step):
    p0 = (x, Y_XR, Z_XR)
    p2 = L(-0.10)
    p3 = L(0.135)
    return dc.fillet([p0, (x, p2.y, p2.z), (x, p3.y, p3.z)], R_BEND, step=step)


def saddle_bead(kit, x_post, yc, zc, inner_sign, n=10, sides=3, r=0.0011, name="weld bead"):
    """Weld bead along the intersection of a cross tube (axis x, centre y/z, radius R_C) with a post (vertical
    axis at x_post, radius R_T); inner_sign = -1 for the +x post (the bead on its inner face)."""
    pts = []
    for k in range(n):
        th = 2 * math.pi * k / n
        dz = R_C * math.sin(th)
        w = math.sqrt(max(R_T * R_T - dz * dz, 0.0))
        p = Vector((x_post + inner_sign * w, yc + R_C * math.cos(th), zc + dz))
        n1 = Vector((0.0, math.cos(th), math.sin(th)))
        n2 = Vector((inner_sign * w / R_T, 0.0, dz / R_T))
        bis = (n1 + n2).normalized()
        pts.append(bl(p + bis * 0.0007))
    return dc.bead(kit, pts, r, CHROME, sides=sides, name=name)


def tee_bead(kit, xu, yc, zc, n=8, sides=3, r=0.0010, name="weld bead"):
    """Bead where an upright (nearly vertical, radius R_C) stands on the rear cross tube (axis x, radius R_C)."""
    pts = []
    for k in range(n):
        ph = 2 * math.pi * k / n
        dz = R_C * math.sin(ph)
        h = math.sqrt(max(R_C * R_C - dz * dz, 0.0))
        p = Vector((xu + R_C * math.cos(ph), yc + h, zc + dz))
        n1 = Vector((math.cos(ph), 0.0, math.sin(ph)))
        n2 = Vector((0.0, h / R_C, dz / R_C))
        bis = (n1 + n2).normalized()
        pts.append(bl(p + bis * 0.0006))
    return dc.bead(kit, pts, r, CHROME, sides=sides, name=name)


def glide(kit, x, z, lods="0"):
    """Snap-on nylon sled glide: a C-section shoe round the runner's bottom, 44 mm long, chamfered ends."""
    rt = R_T
    out = [(0.0157, 0.016), (0.0157, 0.0045), (0.0147, 0.0012), (0.0117, 0.0)]
    out += [(-u, v) for (u, v) in reversed(out)]
    arc = []
    a0, a1 = math.radians(192.0), math.radians(348.0)
    for k in range(5):
        a = a0 + (a1 - a0) * k / 4
        arc.append((rt * math.cos(a), Y_RUN + rt * math.sin(a)))
    outline = out + arc          # right lip, down, sole, up, left lip, inner arc back (u = Blender x, v = height)
    # extrude_rings works in Blender space: axis 'y' (= Unity z), u along Blender x, v along Blender z
    o = dc.extrude_rings(kit, outline, 0.044, (-x, -z, 0.0), "y", BLACK, chamfer=0.0015, scale_about=(0.0, 0.006),
                         name="sled glide")
    return dc.g1(o, SMOOTH_ANGLE, lods)


def hex_head(kit, p, axis_u, lods="0"):
    prof = [(0.0058, 0.0), (0.0058, 0.0030), (0.0046, 0.0040), (0.0, 0.0040)]
    o = dc.lathe_axis(kit, prof, bl(p), bl(axis_u), CHROME, verts=6, name="hex head", close_bottom=False)
    return dc.g1(o, SMOOTH_ANGLE, lods)


def dome_head(kit, p, axis_u, lods="0"):
    prof = [(0.0060, 0.0), (0.0055, 0.0020), (0.0031, 0.0034), (0.0, 0.0037)]
    o = dc.lathe_axis(kit, prof, bl(p), bl(axis_u), CHROME, verts=12, name="dome bolt head", close_bottom=False)
    return dc.lv(o, lods)


def end_plug(kit, p, axis_u, lods="0"):
    prof = [(0.0080, -0.003), (0.0103, 0.0004), (0.0098, 0.0030), (0.0, 0.0042)]
    o = dc.lathe_axis(kit, prof, bl(p), bl(axis_u), BLACK, verts=12, name="end plug", close_bottom=False)
    return dc.g1(o, SMOOTH_ANGLE, lods)


def orient(obj, ref):
    """Turn every face of an open shell away from ``ref`` (local space)."""
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    r = Vector(ref)
    for f in bm.faces:
        f.normal_update()
        if f.normal.dot(f.calc_center_median() - r) < 0:
            f.normal_flip()
    bm.to_mesh(obj.data)
    bm.free()
    return obj


def smoothstep(e0, e1, x):
    t = max(0.0, min(1.0, (x - e0) / (e1 - e0)))
    return t * t * (3 - 2 * t)


# --------------------------------------------------------------- upholstery
WELT = 0.0030


def seat_profile(lod):
    if lod == 0:
        return [(0.007, 0.000, 0.0), (0.000, 0.012, 0.0), (0.000, 0.0590, 0.0),
                (-0.0003, 0.0622, WELT * 0.55), (0.0000, 0.0650, WELT), (0.0010, 0.0680, WELT * 0.55),
                (0.0040, 0.0712, 0.0), (0.0120, 0.0772, 0.0), (0.0300, 0.0822, 0.0), (0.0800, 0.0848, 0.0)]
    if lod == 1:
        return [(0.007, 0.000, 0.0), (0.000, 0.012, 0.0), (0.000, 0.064, 0.0), (0.010, 0.077, 0.0),
                (0.045, 0.0835, 0.0)]
    return [(0.004, 0.000, 0.0), (0.000, 0.060, 0.0), (0.030, 0.082, 0.0)]


def back_profile(lod):
    if lod == 0:
        return [(0.006, 0.000, 0.0), (0.000, 0.008, 0.0), (0.000, 0.0365, 0.0),
                (-0.0003, 0.0395, WELT * 0.55), (0.0000, 0.0423, WELT), (0.0010, 0.0451, WELT * 0.55),
                (0.0040, 0.0481, 0.0), (0.0120, 0.0531, 0.0), (0.0350, 0.0563, 0.0)]
    return [(0.006, 0.000, 0.0), (0.000, 0.008, 0.0), (0.000, 0.042, 0.0), (0.010, 0.052, 0.0),
            (0.040, 0.0565, 0.0)]


def seat_warp(x, y, z):
    f = max(0.0, min(1.0, z / 0.085))
    x *= 1.0 - 0.02 * max(0.0, y) / SEAT_HD                                   # a touch narrower at the back
    dish = 0.008 * math.exp(-(x * x) / 0.018 - ((y - 0.03) ** 2) / 0.022)      # 8 mm sag where people sit
    s = max(0.0, (-y - (SEAT_HD - 0.075)) / 0.075)
    roll = 0.024 * s * s                                                       # waterfall front
    return (x, y, z - (dish + roll) * f)


def seat_welt_w(x, y, nx, ny):
    return smoothstep(-SEAT_HD + 0.015, -SEAT_HD + 0.065, y)                  # no welt across the front


def back_warp(x, y, z):
    f = max(0.0, min(1.0, z / 0.057))
    return (x, y, z + 0.003 * (1.0 - (x / BACK_HW) ** 2) * f)


def seat(kit, lod):
    nc, nx = {0: (5, 3), 1: (3, 1), 2: (2, 1)}[lod]
    lods = str(lod)
    rot = (-SEAT_RAKE, 0.0, 0.0)
    if lod < 2:
        # pan (black PP tray)
        prof = ([(0.0060, 0.000, 0.0), (0.0015, 0.0015, 0.0), (0.0000, 0.0060, 0.0), (0.0000, PAN_T, 0.0)] if lod == 0
                else [(0.004, 0.000, 0.0), (0.000, PAN_T, 0.0)])
        pan = dc.pad(kit, "seat pan", BLACK, PAN_HW, PAN_HD, PAN_R, prof, 4 if lod == 0 else 2, 2, 2,
                     bl(PAN_O), rot=rot)
        dc.lv(pan, lods)
    pad = dc.pad(kit, "seat pad", VINYL, SEAT_HW, SEAT_HD, SEAT_R, seat_profile(lod), nc, nx, nx,
                 bl(PAD_O), rot=rot, warp=seat_warp, bulge_w=seat_welt_w if lod == 0 else None, cap_bottom=(lod == 2))
    orient(pad, (0.0, 0.0, 0.03))
    dc.lv(pad, lods)


def back(kit, lod):
    nc, nx = {0: (5, 3), 1: (3, 1), 2: (2, 1)}[lod]
    lods = str(lod)
    rot = (90.0 - BACK_REC, 0.0, 0.0)
    shw, shd, shr = BACK_HW + SHELL_LIP, BACK_HD + SHELL_LIP, BACK_R + SHELL_LIP
    if lod == 2:
        # one slab: the shell's back to the pad's crowned front
        prof = [(0.0, -0.012, 0.0), (0.0, 0.040, 0.0), (0.025, 0.056, 0.0)]
        slab = dc.pad(kit, "back slab", VINYL, shw, shd, shr, prof, nc, nx, nx, bl(PB), rot=rot)
        orient(slab, (0.0, 0.0, 0.02))
        dc.lv(slab, lods)
        return
    pad = dc.pad(kit, "back pad", VINYL, BACK_HW, BACK_HD, BACK_R, back_profile(lod), nc, nx, nx, bl(PB), rot=rot,
                 warp=back_warp, bulge_w=None, cap_bottom=False)
    orient(pad, (0.0, 0.0, 0.025))
    dc.lv(pad, lods)
    rim_in = shw - (BACK_HW - 0.0005)
    if lod == 0:
        prof = [(0.060, -0.0120, 0.0), (0.012, -0.0115, 0.0), (0.004, -0.0095, 0.0), (0.0, -0.0040, 0.0),
                (0.0, 0.0090, 0.0), (0.0015, 0.0115, 0.0), (rim_in, 0.0120, 0.0)]
    else:
        prof = [(0.030, -0.012, 0.0), (0.004, -0.010, 0.0), (0.0, 0.008, 0.0), (rim_in, 0.012, 0.0)]
    shell = dc.pad(kit, "back shell", BLACK, shw, shd, shr, prof, nc, nx, nx, bl(PB), rot=rot, cap_bottom=True,
                   cap_top=False)
    orient(shell, (0.0, 0.0, 0.05))
    dc.lv(shell, lods)


# --------------------------------------------------------------- build
def build(kit):
    dc.register()
    dc.write_mats()

    # ---------------- LOD0
    side_loops(kit, 16, 15.0, "0", "side loop")
    straight_tube(kit, (-XL, Y_XF, Z_XF), (XL, Y_XF, Z_XF), R_C, 16, "0", "front cross tube")
    straight_tube(kit, (-XL, Y_XR, Z_XR), (XL, Y_XR, Z_XR), R_C, 16, "0", "rear cross tube")
    for s in (-1, 1):
        dc.lv(saddle_bead(kit, s * XL, Y_XF, Z_XF, -s), "0")
        dc.lv(saddle_bead(kit, s * XL, Y_XR, Z_XR, -s), "0")
    # seat rails (chrome flat bar) on the cross tubes, at the seat's rake
    z0, z1 = Z_XF + 0.006, Z_XR - 0.006
    zc = (z0 + z1) / 2
    length = (z0 - z1) / math.cos(math.radians(SEAT_RAKE))
    for s in (-1, 1):
        o = kit.box((RAIL_W, length, RAIL_T), bl((s * RAIL_X, rail_top(zc) - RAIL_T / 2, zc)), CHROME, bevel=0.0009,
                    segments=1, rot=(-SEAT_RAKE, 0, 0), name="seat rail")
        dc.g1(o, SMOOTH_ANGLE, "0")
        for zb in (0.175, -0.125):
            hex_head(kit, (s * RAIL_X, rail_top(zb) - RAIL_T, zb), (0.0, -1.0, 0.0), "0")
    # back uprights
    for s in (-1, 1):
        pts = upright_points(s * UPR_X, 15.0)
        dc.lv(dc.tube(kit, [bl(p) for p in pts], R_C, CHROME, sides=16, caps=False, name="back upright"), "0")
        dc.lv(tee_bead(kit, s * UPR_X, Y_XR, Z_XR), "0")
        axis_top = (Vector(pts[-1]) - Vector(pts[-2])).normalized()
        end_plug(kit, pts[-1], tuple(axis_top), "0")
        for t in (-0.035, 0.090):
            p = L(t)
            dome_head(kit, (s * UPR_X, p.y - BN.y * R_C, p.z - BN.z * R_C), tuple(-BN), "0")
    # glides
    for s in (-1, 1):
        for zg in (0.171, -0.2016):
            glide(kit, s * XL, zg, "0")
    seat(kit, 0)
    back(kit, 0)

    # ---------------- LOD1: 8 sides, 30 deg bends, no welts, no hardware under 5 cm
    side_loops(kit, 8, 30.0, "1", "side loop lod1")
    straight_tube(kit, (-XL, Y_XF, Z_XF), (XL, Y_XF, Z_XF), R_C, 8, "1", "front cross tube lod1")
    straight_tube(kit, (-XL, Y_XR, Z_XR), (XL, Y_XR, Z_XR), R_C, 8, "1", "rear cross tube lod1")
    for s in (-1, 1):
        o = kit.box((RAIL_W, length, RAIL_T), bl((s * RAIL_X, rail_top(zc) - RAIL_T / 2, zc)), CHROME, bevel=0.0,
                    rot=(-SEAT_RAKE, 0, 0), name="seat rail lod1")
        dc.lv(o, "1")
        pts = upright_points(s * UPR_X, 30.0)
        dc.lv(dc.tube(kit, [bl(p) for p in pts], R_C, CHROME, sides=8, caps=True, name="back upright lod1"), "1")
    seat(kit, 1)
    back(kit, 1)

    # ---------------- LOD2: 6-sided loops with one facet per bend, seat and back slabs (uprights < 1 px)
    side_loops(kit, 6, 90.0, "2", "side loop lod2")
    seat(kit, 2)
    back(kit, 2)

    # ---------------- metadata (72 section 2.4)
    kit.collider(B(0.0, 0.42, -0.015), dc.BS(0.584, 0.84, 0.57))
    kit.support("seat", B(0.0, 0.457, 0.02), (0.44, 0.40))
    kit.anchor("sit", B(0.0, 0.457, 0.02))
    kit.anchor("back_top", B(0.0, 0.835, -0.27))
    kit.tag("station", "seat", "office")
    dc.sc.lods(kit, 5.0, 12.0, 50.0, (5000, 1700, 350))
    dc.station_block(kit, "floor, footprint centre", "front (+Z) = the sitter's facing; Station_Desk_3x3 P23, P24 "
                     "(W wall, yaw 90); colour S-A teal (REC), S-B burgundy variant")
