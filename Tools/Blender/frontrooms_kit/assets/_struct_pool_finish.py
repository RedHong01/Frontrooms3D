"""The rest of image 1's pool props (structures G2-d, line visual (game visual), 2026-11-10; package structures_pool_finish,
30_plan.md P15's mays; W/structures_assets/g2_review_assets.md sections 3 S-9 and 8; 63_furniture_run_pool section 6.2).

All in UNITY space (x, y, z), converted by _struct_common.ub(); hand-built LOD0 / LOD1 / LOD2 (B.levels), hero rule as G1-b
(curves at a 0.2 mm chord at LOD0, exposed edges bevelled at LOD0). A new helper so every landed kit (G1-b, G2-b, G2-c) builds
byte for byte as before: _struct_pool.py and _struct_common.py are imported, never changed. No text, label, brand or logo:
the counter's window, the monitor and the ring are blank. ASCII only.
  Kit_RingBuoy        a 20 in ring life buoy on its wall bracket (TM 5-662 (1986) 2-13c: rescue gear "strategically located
                      around the pool deck or at lifeguard chairs", fig 2-1 "THROWING-RING BUOY"; 46 CFR 160.050-3 / -4:
                      the 20 in and 24 in sizes, "international orange (Color No. 12197 of Federal Standard 595) or white",
                      a 3/8 in polypropylene grab line four times the outside diameter held by four 2 in beckets at
                      equidistant points): a unicellular ring 508 mm OD / 318 mm ID, 85 mm thick, in an orange shell; the
                      white braided grab line in four bights hanging by gravity; four white webbing beckets; a stainless bracket (a
                      120 x 50 mm wall plate, two screws, two 10 mm rod arms through the ring's eye with turned-up tips).
                      Origin on the wall plane at the plate's centre, +Z out of the wall, +Y up.
  Kit_DispatcherPost  the slide's dispatcher post (63 section 6.2 and its row: intercom handset, ride counter, whistle hook,
                      emergency stop, small CCTV monitor; LS114: the UK code of 1990 lists intercom, telephones, emergency
                      call buttons and CCTV, and recommends ride counters): a wall-standing putty-enamelled steel console 1.20 x
                      0.42 x 1.03 (its top on the start room's tiled shelf's own footprint and height, so the shelf's collider
                      stays the console's), a putty vinyl mat on its top, two doors with a flush lock and pressed louvres,
                      a charcoal vinyl plinth; on it a 9 in B/W CCTV monitor in a black steel case with a sun hood (its screen slot
                      Prop_ScreenCRT, so the builder's _On swap applies), a sloped back panel with a five-drum ride counter
                      (blank window) and its push button and reset knob, a red mushroom emergency stop on a yellow box, a wall
                      intercom with its handset on the hook switch and a coiled cord, a whistle on a lanyard on a cup hook.
                      Origin on the floor at the wall's face, centred, +Z out of the wall (into the room), +Y up.
"""

import math

from mathutils import Vector

import kitlib
import _run_b_common as B
import _struct_common as S

ORANGE = "Prop_RingBuoyOrange"
ROPE = "Prop_PlasticWhite"
SS = "Prop_StainlessSatin"
GALV = "RunB_GalvSteel"
BLACK = "Prop_PlasticBlack"
STEELBLACK = "Prop_SteelBlack"
RUBBER = "Prop_Rubber"
PUTTY = "Prop_PlasticPutty"
RED = "Prop_PlasticRed"
YELLOW = "RunB_BucketYellow"
SCREEN = "Prop_ScreenCRT"
CHROME = "Prop_ChromeSatin"
DARK = "Plenum_Dark"
VINYL = "Prop_VinylCharcoal"

# FED-STD-595 12197 international orange (sRGB about FF4F00), a little weathered; the new Surface Prop_RingBuoyOrange
kitlib.register_slot(ORANGE, (0.86, 0.13, 0.012), 0.42, 0.0)
kitlib.register_slot(CHROME, (0.70, 0.70, 0.70), 0.18, 1.0)
# the game's Prop_VinylCharcoal (Resources/Surfaces, in main): the console's plinth and top mat (Blender looks only; Unity remaps by slot)
kitlib.register_slot(VINYL, (0.2353, 0.2471, 0.2667), 0.62, 0.0)

DIST_SMALL = (2.5, 9.0, 32.0)
DIST_POST = (3.0, 12.0, 40.0)


# ----------------------------------------------------------------------------- small geometry helpers (Unity space)
def _superellipse(a, b, n, e=2.6):
    """A closed rounded-square section (radial a, axial b half sizes), counter-clockwise in (r, z) from the outside (+r)."""
    pts = []
    for k in range(n):
        t = 2.0 * math.pi * k / n
        c, s = math.cos(t), math.sin(t)
        pts.append((a * math.copysign(abs(c) ** (2.0 / e), c), b * math.copysign(abs(s) ** (2.0 / e), s)))
    return pts


def _ring_point(centre, radius, theta, z):
    return (centre[0] + radius * math.cos(theta), centre[1] + radius * math.sin(theta), centre[2] + z)


def _quad_out(m, a, b, c, d, uva, uvb, uvc, uvd, outward):
    """A quad wound so cross(b - a, c - a) points along 'outward' (Unity space; S.Mesh turns it for Blender)."""
    va, vb, vc = Vector(a), Vector(b), Vector(c)
    n = (vb - va).cross(vc - va)
    if n.length < 1e-14:
        n = (Vector(c) - va).cross(Vector(d) - va)
    if n.dot(Vector(outward)) < 0.0:
        m.quad(a, d, c, b, uva, uvd, uvc, uvb)
    else:
        m.quad(a, b, c, d, uva, uvb, uvc, uvd)


def torus(kit, name, centre, rc, section, n_major, slot, t0=0.0, t1=2.0 * math.pi, caps=False):
    """A ring of a closed (r, z) section round 'centre' in the XY plane (axis +Z), from angle t0 to t1. UVs in metres."""
    m = S.Mesh()
    full = abs(t1 - t0 - 2.0 * math.pi) < 1e-6
    ns = len(section)
    arc = [0.0]
    for j in range(1, ns + 1):
        p, q = section[j - 1], section[j % ns]
        arc.append(arc[-1] + math.hypot(q[0] - p[0], q[1] - p[1]))
    steps = n_major if full else max(2, n_major)
    rings = []
    for i in range(steps + 1):
        th = t0 + (t1 - t0) * i / steps
        rings.append([(_ring_point(centre, rc + sr, th, sz), th) for (sr, sz) in section])
    for i in range(steps):
        for j in range(ns):
            j1 = (j + 1) % ns
            a, ta = rings[i][j]
            b, _ = rings[i][j1]
            c, tc = rings[i + 1][j1]
            d, _ = rings[i + 1][j]
            # outward: from the section's own centre (on the major circle) to the face's middle
            mid_th = (ta + tc) * 0.5
            cen = Vector(_ring_point(centre, rc, mid_th, 0.0))
            out = (Vector(a) + Vector(b) + Vector(c) + Vector(d)) * 0.25 - cen
            u0, u1 = rc * (ta - t0), rc * (tc - t0)
            _quad_out(m, a, b, c, d, (u0, arc[j]), (u0, arc[j + 1]), (u1, arc[j + 1]), (u1, arc[j]), out)
    if caps and not full:
        for (ring, th, sgn) in ((rings[0], t0, -1.0), (rings[-1], t1, 1.0)):
            pts = [p for (p, _) in ring]
            tang = Vector((-math.sin(th), math.cos(th), 0.0)) * sgn
            idx = [m.vert(p) for p in pts]
            uvs = [(sr, sz) for (sr, sz) in section]
            va, vb, vc = Vector(pts[0]), Vector(pts[1]), Vector(pts[2])
            n = (vb - va).cross(vc - va)
            if n.dot(tang) < 0.0:
                m.face(tuple(reversed(idx)), list(reversed(uvs)))
            else:
                m.face(tuple(idx), uvs)
    return m.part(kit, name, slot)


def braided_line(kit, name, path, r, slot, lv):
    """A braided 3/8 in polypropylene line along a smooth Unity polyline: a round tube (8 / 6 / 4 sides), open ends (they sit
    inside a becket). A laid rope's twist was tried first: at the 0.35 m hero its strands read as a sawtooth unless sampled
    at 4-5 mm (8-9 k triangles); the braid holds as a smooth cord."""
    m = S.Mesh()
    fr = S.frames_along(path)
    n = 8 if lv == 0 else 6 if lv == 1 else 4
    sec = [(r * math.cos(2 * math.pi * k / n), r * math.sin(2 * math.pi * k / n)) for k in range(n)]
    along = [0.0]
    for i in range(1, len(path)):
        along.append(along[-1] + (Vector(path[i]) - Vector(path[i - 1])).length)
    rings = []
    for i, p in enumerate(path):
        t, nn, bb = fr[i]
        rings.append([tuple(Vector(p) + nn * a + bb * b) for (a, b) in sec])
    circ = 2.0 * math.pi * r
    for i in range(len(path) - 1):
        for j in range(n):
            j1 = (j + 1) % n
            a, b, c, d = rings[i][j], rings[i][j1], rings[i + 1][j1], rings[i + 1][j]
            out = (Vector(a) + Vector(b)) * 0.5 - Vector(path[i])
            _quad_out(m, a, b, c, d, (along[i], circ * j / n), (along[i], circ * (j + 1) / n), (along[i + 1], circ * (j + 1) / n),
                      (along[i + 1], circ * j / n), out)
    return m.part(kit, name, slot)


def resample(points, step):
    """A polyline with a point at most 'step' apart (the corners kept)."""
    out = [points[0]]
    for i in range(1, len(points)):
        a, b = Vector(points[i - 1]), Vector(points[i])
        n = max(1, int(math.ceil((b - a).length / step)))
        for k in range(1, n + 1):
            out.append(tuple(a + (b - a) * (k / float(n))))
    return out


def prism_x(kit, name, profile_yz, x0, x1, slot):
    """A solid prism along x of a closed (y, z) outline (counter-clockwise seen from +x), capped, metre UVs."""
    m = S.Mesh()
    n = len(profile_yz)
    arc = [0.0]
    for j in range(1, n + 1):
        p, q = profile_yz[j - 1], profile_yz[j % n]
        arc.append(arc[-1] + math.hypot(q[0] - p[0], q[1] - p[1]))
    cy = sum(p[0] for p in profile_yz) / n
    cz = sum(p[1] for p in profile_yz) / n
    for j in range(n):
        j1 = (j + 1) % n
        (ya, za), (yb, zb) = profile_yz[j], profile_yz[j1]
        a, b, c, d = (x0, ya, za), (x0, yb, zb), (x1, yb, zb), (x1, ya, za)
        out = (0.0, (ya + yb) * 0.5 - cy, (za + zb) * 0.5 - cz)
        _quad_out(m, a, b, c, d, (x0, arc[j]), (x0, arc[j + 1]), (x1, arc[j + 1]), (x1, arc[j]), out)
    for (x, sgn) in ((x0, -1.0), (x1, 1.0)):
        pts = [(x, y, z) for (y, z) in profile_yz]
        idx = [m.vert(p) for p in pts]
        uvs = [(z, y) for (y, z) in profile_yz]
        va, vb, vc = Vector(pts[0]), Vector(pts[1]), Vector(pts[2])
        nrm = (vb - va).cross(vc - va)
        if nrm.dot(Vector((sgn, 0.0, 0.0))) < 0.0:
            m.face(tuple(reversed(idx)), list(reversed(uvs)))
        else:
            m.face(tuple(idx), uvs)
    return m.part(kit, name, slot)


# ----------------------------------------------------------------------------- the ring buoy
RING_OD = 0.508
RING_ID = 0.318
RING_T = 0.085
RC = (RING_OD + RING_ID) / 4.0          # 0.2065, the section's centre circle
RA = (RING_OD - RING_ID) / 4.0          # 0.0475, half the radial width
RB = RING_T / 2.0                       # 0.0425, half the thickness
ROPE_R = 0.0048                         # 3/8 in
ARM_X = 0.060
ARM_R = 0.005
RING_Z = 0.012 + RB                     # the ring's mid-plane, its back 12 mm off the wall
# the ring hangs with its eye's top edge on the arms' tops (y -0.005 at x +-0.06)
RING_CY = -0.005 - math.sqrt((RC - RA) ** 2 - ARM_X ** 2)
BECKETS = (45.0, 135.0, 225.0, 315.0)


def _ring_section(lv):
    n = 24 if lv == 0 else 12 if lv == 1 else 8
    return _superellipse(RA, RB, n)


def _rope_path(lv):
    """The grab line round the ring's outside in four bights between the beckets, its slack taken by gravity (-y): the top
    bight lies on the ring, the side bights hug its upper half and droop off its lower half, the bottom bight hangs."""
    centre = Vector((0.0, RING_CY, RING_Z))
    ro = RC + RA + ROPE_R + 0.0008
    step = 0.016 if lv == 0 else 0.040 if lv == 1 else 0.10
    pts = []
    total = 0.0
    for b in range(4):
        a0 = math.radians(BECKETS[b])
        a1 = a0 + math.pi / 2.0
        mid = (a0 + a1) * 0.5
        # slack by the bight's place: the bottom one hangs, the top one lies on the ring
        hang = 0.085 * max(0.0, -math.sin(mid)) + 0.035 * (1.0 - abs(math.sin(mid)))
        n = max(4, int(math.ceil(ro * (math.pi / 2.0) / step)))
        for k in range(n + (1 if b == 3 else 0)):
            f = k / float(n)
            th = a0 + (a1 - a0) * f
            p = centre + Vector((ro * math.cos(th), ro * math.sin(th), 0.0))
            w = math.sin(math.pi * f) ** 1.25
            p = p + Vector((0.0, -hang * w, 0.0))
            # never inside the ring's outside: pushed back out on its radius
            d = p - centre
            d.z = 0.0
            if d.length < ro:
                d = d.normalized() * ro
                p = centre + d
                p.z = RING_Z
            # a little off the ring's mid-plane where it hangs free (it falls toward the wall)
            p.z = RING_Z - 0.010 * w * max(0.0, -math.sin(mid))
            pts.append(tuple(p))
    return pts


def ring_buoy(kit, lv):
    """Kit_RingBuoy: origin on the wall plane at the bracket plate's centre, +Z out of the wall, +Y up."""
    centre = (0.0, RING_CY, RING_Z)
    n_major = 64 if lv == 0 else 28 if lv == 1 else 14
    torus(kit, "ring", centre, RC, _ring_section(lv), n_major, ORANGE)
    # the beckets: 2 in white webbing round the ring's section and over the line at the outside (a solid band, 2.5 mm proud)
    if lv <= 1:
        sec = []
        base = _superellipse(RA + 0.0025, RB + 0.0025, 20 if lv == 0 else 10)
        for (sr, sz) in base:
            if sr > RA * 0.55:
                sr = sr + (2.0 * ROPE_R + 0.0015) * min(1.0, (sr - RA * 0.55) / (RA * 0.45))
            sec.append((sr, sz))
        half = 0.025 / (RC + RA)
        for deg in BECKETS:
            t = math.radians(deg)
            torus(kit, "becket", centre, RC, sec, 3 if lv == 0 else 1, ROPE, t - half, t + half, caps=True)
    # the grab line
    braided_line(kit, "grab line", _rope_path(lv), ROPE_R, ROPE, lv)
    # the bracket: a stainless wall plate, two screws, two rod arms through the ring's eye with turned-up tips
    S.box(kit, (0.120, 0.050, 0.004), (0.0, 0.0, 0.002), SS, lv, w=0.0010, name="bracket plate")
    if lv == 0:
        for sx in (-0.042, 0.042):
            S.screw_head(kit, 0.0042, (sx, 0.0, 0.004), "+z", SS, lv, name="bracket screw")
    for sx in (-ARM_X, ARM_X):
        y = -0.010
        bend = 0.010
        pts = [(sx, y, 0.004), (sx, y, 0.112)]
        steps = 6 if lv == 0 else 3 if lv == 1 else 1
        for k in range(1, steps + 1):
            a = (math.pi / 2.0) * k / steps
            pts.append((sx, y + bend - bend * math.cos(a), 0.112 + bend * math.sin(a)))
        pts.append((sx, y + 0.040, 0.112 + bend))
        S.tube(kit, "bracket arm", resample(pts, 0.03), ARM_R, SS, lv, cap0=False, cap1=True, lo=8)


# ----------------------------------------------------------------------------- the dispatcher post
POST_W, POST_D, POST_H = 1.20, 0.42, 1.03
TOP_T = 0.08


def _louvres(kit, x0, x1, y, z, lv):
    if lv >= 2:
        return
    n = 5 if lv == 0 else 3
    for k in range(n):
        xx = x0 + (x1 - x0) * (k + 0.5) / n
        S.box(kit, (0.008, 0.070, 0.0024), (xx, y, z + 0.0006), DARK, lv, w=0.0, name="louvre")


def _monitor(kit, lv):
    """A 9 in B/W CCTV monitor in a black steel case with a sun hood, on the console's top at the left, turned a little in."""
    cx, cz = -0.33, 0.150
    yaw = 8.0
    base = POST_H
    w, h, d = 0.235, 0.215, 0.255
    rot = (0, yaw, 0)
    S.box(kit, (w, h, d), (cx, base + h / 2.0, cz), STEELBLACK, lv, w=0.0018, name="monitor case", rot=rot)
    ca, sa = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))

    def at(dx, dy, dz):
        # a point in the monitor's frame (+z its face) to Unity, turned by yaw about y (Unity's yaw: +z toward +x)
        return (cx + dx * ca + dz * sa, base + dy, cz - dx * sa + dz * ca)

    fz = d / 2.0
    # the bezel and the screen: a dark plastic frame, the tube's face 8 mm domed behind it
    S.box(kit, (w + 0.004, h + 0.004, 0.012), at(0.0, h / 2.0, fz + 0.004), BLACK, lv, w=0.0016, name="monitor bezel", rot=rot)
    sw, sh = 0.172, 0.132
    m = S.Mesh()
    nx, ny = (6, 5) if lv == 0 else (3, 2) if lv == 1 else (1, 1)
    grid = []
    for iy in range(ny + 1):
        row = []
        for ix in range(nx + 1):
            u, v = ix / float(nx), iy / float(ny)
            x, y = (u - 0.5) * sw, (v - 0.5) * sh
            dome = 0.008 * (1.0 - (2 * u - 1) ** 2) * (1.0 - (2 * v - 1) ** 2)
            # u runs right to left in the monitor's frame (+z its face, so a viewer in front has -x on the right): the screen's
            # picture reads the right way round (G2-d render 2026-11-10: with (u, v) the _On swap showed it mirrored)
            row.append((at(x, h / 2.0 + 0.012 + y, fz + 0.007 + dome), (1.0 - u, v)))
        grid.append(row)
    for iy in range(ny):
        for ix in range(nx):
            (a, ua), (b, ub_) = grid[iy][ix], grid[iy][ix + 1]
            (c, uc), (dd, ud) = grid[iy + 1][ix + 1], grid[iy + 1][ix]
            _quad_out(m, a, b, c, dd, ua, ub_, uc, ud, (sa, 0.0, ca))
    m.part(kit, "monitor screen", SCREEN)
    # the hood: top and sides 70 mm forward of the bezel, 3 mm sheet
    hd = 0.060
    S.box(kit, (w + 0.010, 0.003, hd), at(0.0, h + 0.0035, fz + 0.010 + hd / 2.0), STEELBLACK, lv, w=0.0006, name="monitor hood", rot=rot)
    for sx in (-1.0, 1.0):
        S.box(kit, (0.003, h - 0.02, hd), at(sx * (w / 2.0 + 0.0035), h / 2.0 + 0.01, fz + 0.010 + hd / 2.0), STEELBLACK, lv, w=0.0006, name="monitor hood", rot=rot)
    # two knobs under the screen, vents on the top
    if lv <= 1:
        for kx in (0.055, 0.085):
            S.cyl_z(kit, 0.007, 0.012, at(kx, 0.028, fz + 0.014), BLACK, lv, w=0.0006, name="monitor knob")
    if lv == 0:
        for k in range(4):
            S.box(kit, (0.004, 0.0015, 0.120), at(-0.045 + 0.03 * k, h + 0.0004, -0.02), DARK, lv, w=0.0, name="monitor vent", rot=rot)


def _intercom(kit, lv):
    """A wall intercom at the console's right end: a putty box, its handset on the hook switch, the coiled cord."""
    bx, by, bw, bh, bd = 0.47, POST_H + 0.17, 0.110, 0.220, 0.060
    S.box(kit, (bw, bh, bd), (bx, by, bd / 2.0), PUTTY, lv, w=0.0030, name="intercom box")
    # the hook switch: a small black lever in a slot at the box's top front
    S.box(kit, (0.036, 0.010, 0.022), (bx, by + bh / 2.0 - 0.020, bd + 0.009), BLACK, lv, w=0.0010, name="hook switch")
    # the handset in its cradle on the box's face, earpiece up: a handle and two round cups toward the wall
    hz = bd + 0.026
    S.box(kit, (0.040, 0.150, 0.022), (bx, by - 0.005, hz + 0.004), BLACK, lv, w=0.0060, name="handset")
    for cy in (by + 0.072, by - 0.082):
        S.cyl_z(kit, 0.026, 0.026, (bx, cy, hz - 0.004), BLACK, lv, w=0.0040, name="handset cup")
    # the coiled cord from the handset's foot down and back up into the box's foot, a helix along a hanging U
    if lv <= 1:
        start = Vector((bx, by - 0.112, hz - 0.004))
        end = Vector((bx - 0.025, by - bh / 2.0, 0.030))
        spine = []
        n = 24 if lv == 0 else 12
        for k in range(n + 1):
            f = k / float(n)
            p = start.lerp(end, f) + Vector((0.0, -0.040 * math.sin(math.pi * f), 0.006 * math.sin(math.pi * f)))
            spine.append(p)
        coils = 14 if lv == 0 else 8
        helix = []
        steps = coils * (8 if lv == 0 else 5)
        for k in range(steps + 1):
            f = k / float(steps)
            i = min(n - 1, int(f * n))
            g = f * n - i
            p = spine[i].lerp(spine[i + 1], g)
            a = 2.0 * math.pi * coils * f
            helix.append(tuple(p + Vector((0.0075 * math.cos(a), 0.0, 0.0075 * math.sin(a)))))
        S.tube(kit, "handset cord", helix, 0.0018, BLACK, lv, cap0=True, cap1=True, lo=5)


def _slope(f, out, x):
    """A Unity point on the back panel's sloped face: f 0..1 from its foot to its top, 'out' metres off the face."""
    ay, az, by_, bz = POST_H + 0.035, 0.110, POST_H + 0.165, 0.062
    ty, tz = by_ - ay, bz - az
    ln = math.hypot(ty, tz)
    ty, tz = ty / ln, tz / ln
    ny, nz = -tz, ty
    if nz < 0.0:
        ny, nz = -ny, -nz
    return (x, ay + (by_ - ay) * f + ny * out, az + (bz - az) * f + nz * out)


SLOPE_DEG = math.degrees(math.atan2(0.110 - 0.062, 0.130))


def _counter(kit, lv):
    """A five-drum ride counter on the back panel's slope: a black box, a dark (blank) window, a reset knob; its push button
    on the top in front of it."""
    cx = 0.10
    tilt = (-SLOPE_DEG, 0, 0)
    S.box(kit, (0.100, 0.050, 0.055), _slope(0.45, 0.0275, cx), BLACK, lv, w=0.0020, name="counter box", rot=tilt)
    if lv <= 1:
        S.box(kit, (0.056, 0.016, 0.004), _slope(0.50, 0.0560, cx - 0.012), DARK, lv, w=0.0006, name="counter window", rot=tilt)
        S.box(kit, (0.012, 0.012, 0.010), _slope(0.50, 0.0600, cx + 0.034), CHROME, lv, w=0.0010, name="counter reset", rot=tilt)
    S.cyl_y(kit, 0.012, 0.016, (cx + 0.10, POST_H + 0.008, 0.20), BLACK, lv, w=0.0010, name="count button base")
    S.cyl_y(kit, 0.008, 0.010, (cx + 0.10, POST_H + 0.021, 0.20), RED, lv, w=0.0010, name="count button")


def _estop(kit, lv):
    """The emergency stop at the console's front right: a yellow box, a black collar, a red mushroom head."""
    ex, ez = 0.40, 0.30
    S.box(kit, (0.075, 0.060, 0.075), (ex, POST_H + 0.030, ez), YELLOW, lv, w=0.0030, name="e-stop box")
    S.cyl_y(kit, 0.016, 0.012, (ex, POST_H + 0.066, ez), BLACK, lv, w=0.0008, name="e-stop collar")
    n = 6 if lv == 0 else 3 if lv == 1 else 2
    prof = [(0.020, POST_H + 0.072), (0.021, POST_H + 0.080)]
    for k in range(1, n + 1):
        a = (math.pi / 2.0) * k / n
        prof.append((0.021 * math.cos(a), POST_H + 0.080 + 0.010 * math.sin(a)))
    S.lathe_y(kit, prof, (ex, 0.0, ez), RED, lv, name="e-stop head", close_top=False)


def _whistle(kit, lv):
    """A whistle on a lanyard on a chrome cup hook on the console's left side."""
    hx, hy, hz = -POST_W / 2.0, POST_H - 0.075, 0.30
    # the cup hook: a screw shank out of the side and a J
    pts = [(hx, hy, hz), (hx - 0.020, hy, hz)]
    steps = 6 if lv == 0 else 3
    for k in range(1, steps + 1):
        a = math.pi * k / steps
        pts.append((hx - 0.020 - 0.008 * math.sin(a), hy - 0.008 + 0.008 * math.cos(a), hz))
    S.tube(kit, "cup hook", pts, 0.0018, CHROME, lv, cap0=False, cap1=True, lo=6)
    if lv >= 2:
        return
    # the lanyard: a cord loop hanging from the hook to the whistle
    top = Vector((hx - 0.020, hy - 0.002, hz))
    low = Vector((hx - 0.030, hy - 0.150, hz + 0.004))
    loop = []
    n = 20 if lv == 0 else 8
    for k in range(n + 1):
        f = k / float(n)
        a = 2.0 * math.pi * f
        loop.append(tuple(top.lerp(low, (1.0 - math.cos(a)) * 0.5) + Vector((0.0, 0.0, 0.012 * math.sin(a)))))
    S.tube(kit, "lanyard", loop, 0.0016, BLACK, lv, cap0=False, cap1=False, lo=6)
    # the whistle: a drum and a mouthpiece, black plastic with a chrome ring
    wx, wy, wz = low.x - 0.004, low.y - 0.016, low.z
    S.cyl_z(kit, 0.012, 0.022, (wx, wy, wz), BLACK, lv, w=0.0010, name="whistle drum")
    S.box(kit, (0.010, 0.030, 0.012), (wx, wy - 0.024, wz), BLACK, lv, w=0.0015, name="whistle mouthpiece")
    S.cyl_z(kit, 0.0045, 0.006, (wx, wy + 0.012, wz), CHROME, lv, w=0.0004, name="whistle ring")


def dispatcher_post(kit, lv):
    """Kit_DispatcherPost: origin on the floor at the wall's face, centred; +Z into the room; its top at 1.03."""
    hw = POST_W / 2.0
    # the plinth (charcoal vinyl, set back 60 mm) and the cabinet in putty enamel. G2-d render 2026-11-10: the galvanised cabinet
    # with a black plinth and a black rubber top mat, swept through the start room's turn (LW0, SP1 s01), raised the count from
    # 4.5 to 6.0 against the white tile; the period's almond / putty enamel console (Prop_PlasticPutty, as the beige CRTs of the
    # time) sits within a step of the tile's light, the plinth a charcoal vinyl base, the mat the top's own putty
    S.box(kit, (POST_W - 0.04, 0.080, POST_D - 0.10), (0.0, 0.040, (POST_D - 0.10) / 2.0 + 0.02), VINYL, lv, w=0.0012, name="plinth")
    cab_d = POST_D - 0.02
    S.box(kit, (POST_W, POST_H - TOP_T - 0.080, cab_d), (0.0, 0.080 + (POST_H - TOP_T - 0.080) / 2.0, cab_d / 2.0), PUTTY, lv, w=0.0018, name="cabinet")
    # two doors 1.5 mm proud with a 4 mm gap, a flush lock, pressed louvres low on each
    dy0, dy1 = 0.100, POST_H - TOP_T - 0.016
    for sx in (-1.0, 1.0):
        x0, x1 = (0.002, hw - 0.014) if sx > 0 else (-hw + 0.014, -0.002)
        S.box(kit, (x1 - x0, dy1 - dy0, 0.0030), ((x0 + x1) / 2.0, (dy0 + dy1) / 2.0, cab_d + 0.0015), PUTTY, lv, w=0.0010, name="door")
        _louvres(kit, x0 + 0.06, x1 - 0.06, dy0 + 0.070, cab_d + 0.003, lv)
    S.cyl_z(kit, 0.011, 0.006, (0.040, dy1 - 0.070, cab_d + 0.006), CHROME, lv, w=0.0008, name="door lock")
    if lv == 0:
        S.box(kit, (0.0015, 0.010, 0.0012), (0.040, dy1 - 0.070, cab_d + 0.0095), DARK, lv, w=0.0, name="key slot")
    # the top: a 6 mm steel plate on a 74 mm apron, a putty vinyl mat inset 15 mm
    S.box(kit, (POST_W, TOP_T, POST_D), (0.0, POST_H - TOP_T / 2.0, POST_D / 2.0), PUTTY, lv, w=0.0025, name="top")
    S.box(kit, (POST_W - 0.03, 0.003, POST_D - 0.03), (0.0, POST_H + 0.0005, POST_D / 2.0), PUTTY, lv, w=0.0010, name="top mat")
    # the back panel: a sloped steel upstand along the wall (left of the intercom), 20 degrees back
    prof = [(POST_H, 0.0), (POST_H, 0.110), (POST_H + 0.035, 0.110), (POST_H + 0.165, 0.062), (POST_H + 0.165, 0.0)]
    prism_x(kit, "back panel", prof, -0.10, 0.36, PUTTY)
    _monitor(kit, lv)
    _counter(kit, lv)
    _estop(kit, lv)
    _intercom(kit, lv)
    _whistle(kit, lv)
