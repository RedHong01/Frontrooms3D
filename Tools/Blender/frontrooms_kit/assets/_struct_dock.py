"""SP5's dock (structures G4-a, line visual (game visual), 2026-11-10; package structures_dock, 30_plan.md P19; research/level_space/
63_furniture_run_pool.md 3.6 RN-DOCK and 6.1; 20_research.md C.4.4 and C.5.1; sources LS60, LS105, LS106, LS114, LS119, F19, F20 and the
construction listings W1-W7 of 60_build.md G4-a).

All in UNITY space (x, y, z; metres), converted by _struct_common.ub(); hand-built LOD0 / LOD1 / LOD2 (B.levels). Hero rule as G1-b / G2:
curves at a 0.2 mm chord at LOD0, exposed edges bevelled at LOD0, read at 0.35 m. Period 1988-91, generic: no text, label, brand or logo.
Origins: floor kits stand on y 0 centred on x = z = 0, front +Z; wall kits sit on the wall plane z 0 (+Z out of the wall, +Y up).
ASCII only.

  Kit_OverheadCoilingDoor  (+ _Shut, _Open)  the 12 x 14 ft rolling steel service door of the bay, face-of-wall mounted, hand-chain hoist;
                           origin on the floor at the opening's centre, on the wall's room face; the curtain's bottom bar at 1.20 m
                           (halfUp, the dock's event), 0 (Shut) or tucked under the hood (Open)
  Kit_DockLeveler          the 6 x 8 ft mechanical pit leveler stored flush, its 16 in lip hanging in the keepers; origin on the platform
                           at the dock face line (the deck runs to -Z, the lip hangs on the face at +Z)
  Kit_DockBumper           the laminated rubber bumper, 10 x 18 in, 4-1/2 in projection; origin on the face at its base
  Kit_DockEdgeGuard_*      the removable edge guard: Stanchion, Chain_1400, Chain_1100, Gate
  Kit_Bollard              6-5/8 in pipe bollard, concrete crowned, 1.07 m
  Kit_DockLightPost        the 4 in pipe post at the dock edge the swing-arm light (FrontRoomsLampKit DockArm) clamps to
  Kit_WheelChock (+ _Hung) the moulded rubber chock on its chain
  (the props: platform truck, dumpster, CCTV camera, receiving desk, wrapped loads and the pallet jack follow below)
"""

import math
import random

import bmesh
import bpy
from mathutils import Vector

import kitlib
import _run_b_common as B
import _struct_common as S

# ----------------------------------------------------------------------------- slots (Unity materials: Resources/Surfaces/<slot>.mat)
GALV = "Prop_DoorSlatGalv"        # NEW (G4-a): galvanised slat steel, spangle and roll streaks
PAINT = "Painted_Metal"           # main: field-painted enamel steel (guides, head plates, bottom bar)
CHAIN = "Prop_ChainOxide"         # main
RUBBER = "Prop_Rubber"            # main
YELLOW = "Prop_SafetyYellow"      # NEW: safety-yellow enamel on steel, chipped (kind-6 kicks at the foot)
TREAD = "Prop_TreadPlate"         # NEW: painted lentil tread plate worn to the steel on its lugs (per-texel metal)
STEEL = "Prop_SteelBlack"         # main
DARK = "Plenum_Dark"              # main
GREEN = "Prop_DumpsterGreen"      # NEW: dumpster enamel, chipped, rust in the chips
RED = "Prop_PalletJackRed"        # NEW: pallet-jack enamel, chipped
FILM = "Prop_PalletFilm"          # NEW: the wrapped loads' stretch film with the cartons seen through it (decal atlas)

for _slot, _c, _r, _m in ((GALV, (0.60, 0.61, 0.60), 0.45, 0.85), (YELLOW, (0.80, 0.62, 0.04), 0.45, 0.0), (TREAD, (0.10, 0.10, 0.10), 0.5, 0.3),
                          (GREEN, (0.10, 0.20, 0.13), 0.40, 0.0), (RED, (0.55, 0.07, 0.05), 0.45, 0.0), (FILM, (0.70, 0.62, 0.48), 0.85, 0.0),
                          (CHAIN, (0.20, 0.19, 0.17), 0.35, 0.55), ("Prop_StationPaper", (0.86, 0.85, 0.80), 0.7, 0.0),
                          ("Prop_TapeBeige", (0.62, 0.50, 0.33), 0.5, 0.0)):
    kitlib.register_slot(_slot, _c, _r, _m)

STAMP = "structures_assets G4-a 2026-11-10 (30_plan.md P19, 63 section 6.1)"


# ----------------------------------------------------------------------------- helpers in Unity space
def ub(p):
    return S.ub(p)


def collider_u(kit, centre, size):
    """A box collider given in Unity space (kitlib stores Blender space and converts at export)."""
    kit.collider(list(ub(centre)), [size[0], size[2], size[1]])


def anchor_u(kit, name, p):
    kit.anchor(name, list(ub(p)))


def support_u(kit, name, centre_top, size_xz):
    kit.support(name, list(ub(centre_top)), [size_xz[0], size_xz[1]])


def finish(kit, distances, budget, tags=()):
    B.lod_meta(kit, distances, budget)
    kit.meta["structures"] = STAMP
    if tags:
        kit.tag(*tags)


def _n(a, b, c):
    return (Vector(b) - Vector(a)).cross(Vector(c) - Vector(a))


def qd(m, a, b, c, d, uva, uvb, uvc, uvd, want):
    """A quad into Mesh m wound so its (right-hand) normal faces want (Unity space)."""
    if _n(a, b, c).dot(Vector(want)) < 0:
        m.quad(a, d, c, b, uva, uvd, uvc, uvb)
    else:
        m.quad(a, b, c, d, uva, uvb, uvc, uvd)


def tri(m, a, b, c, uva, uvb, uvc, want):
    if _n(a, b, c).dot(Vector(want)) < 0:
        m.tri(a, c, b, uva, uvc, uvb)
    else:
        m.tri(a, b, c, uva, uvb, uvc)


def recalc(obj):
    return B.flip_check(obj)


def rot_y(p, deg):
    a = math.radians(deg)
    c, s = math.cos(a), math.sin(a)
    return (p[0] * c + p[2] * s, p[1], -p[0] * s + p[2] * c)


def rot_x(p, deg):
    a = math.radians(deg)
    c, s = math.cos(a), math.sin(a)
    return (p[0], p[1] * c - p[2] * s, p[1] * s + p[2] * c)


def add(p, q):
    return (p[0] + q[0], p[1] + q[1], p[2] + q[2])


def extrude_profile(kit, name, prof, x0, x1, slot, closed=True, uv_scale=1.0, caps=True, want_out=None):
    """A (z, y) profile (Unity: z out, y up) extruded along x from x0 to x1, metre UVs (u along x, v round the profile); a closed
    profile is capped and its normals recalculated outward."""
    m = S.Mesh()
    n = len(prof)
    arc = [0.0]
    for i in range(1, n + (1 if closed else 0)):
        p, q = prof[(i - 1) % n], prof[i % n]
        arc.append(arc[-1] + math.hypot(q[0] - p[0], q[1] - p[1]))
    seg = n if closed else n - 1
    for i in range(seg):
        j = (i + 1) % n
        z0, y0 = prof[i]
        z1, y1 = prof[j]
        a, b, c, d = (x0, y0, z0), (x1, y0, z0), (x1, y1, z1), (x0, y1, z1)
        uva, uvb, uvc, uvd = (x0 * uv_scale, arc[i] * uv_scale), (x1 * uv_scale, arc[i] * uv_scale), (x1 * uv_scale, arc[i + 1] * uv_scale), (x0 * uv_scale, arc[i + 1] * uv_scale)
        if want_out is not None:
            qd(m, a, b, c, d, uva, uvb, uvc, uvd, want_out(z0, y0, z1, y1))
        else:
            m.quad(a, b, c, d, uva, uvb, uvc, uvd)
    if closed and caps:
        for x, sgn in ((x0, -1), (x1, 1)):
            idx = [m.vert((x, y, z)) for (z, y) in prof]
            uvs = [(z * uv_scale, y * uv_scale) for (z, y) in prof]
            if sgn > 0:
                m.face(tuple(idx), uvs)
            else:
                m.face(tuple(reversed(idx)), list(reversed(uvs)))
    obj = m.part(kit, name, slot)
    if closed:
        recalc(obj)
    return obj


def angle_section(leg_a, leg_b, t, r_in=0.004, segs=2):
    """An L section (steel angle) in a local (a, b) plane: leg a along +a, leg b along +b, thickness t, a root fillet; closed outline."""
    pts = [(0.0, 0.0), (leg_a, 0.0), (leg_a, t)]
    if segs > 0 and r_in > 0:
        cx, cy = t + r_in, t + r_in
        for k in range(segs + 1):
            ang = -math.pi / 2 - (math.pi / 2) * k / segs
            pts.append((cx + r_in * math.cos(ang), cy + r_in * math.sin(ang)))
    else:
        pts.append((t, t))
    pts += [(t, leg_b), (0.0, leg_b)]
    # de-duplicate the fillet ends
    out = []
    for p in pts:
        if not out or abs(out[-1][0] - p[0]) + abs(out[-1][1] - p[1]) > 1e-6:
            out.append(p)
    return out


def hex_bolt_z(kit, centre, af, h, slot, lv, name="bolt", sign=1.0):
    """A hex head (or nut) on a Unity axis along z (sign +1 faces +z)."""
    c = (centre[0], centre[1], centre[2] + sign * h / 2.0)
    return S.hex_nut(kit, af, h, c, "z", slot, lv, name=name)


def hex_bolt_x(kit, centre, af, h, slot, lv, name="bolt", sign=1.0):
    c = (centre[0] + sign * h / 2.0, centre[1], centre[2])
    return S.hex_nut(kit, af, h, c, "x", slot, lv, name=name)


def hex_bolt_y(kit, centre, af, h, slot, lv, name="bolt", sign=1.0):
    c = (centre[0], centre[1] + sign * h / 2.0, centre[2])
    return S.hex_nut(kit, af, h, c, "y", slot, lv, name=name)


# ----------------------------------------------------------------------------- chain
def _link(m, c, t, side, half, rc, wire, path_n, wire_n):
    """One oval link (a stadium of half-length half, end radius rc, in the plane (t, side)) swept with a wire of radius wire."""
    loop = []
    for k in range(path_n):
        a = 2.0 * math.pi * k / path_n
        ca, sa = math.cos(a), math.sin(a)
        along = (half - rc) * (1.0 if ca >= 0 else -1.0) + rc * ca
        loop.append(tuple(Vector(c) + t * along + side * (rc * sa)))
    S.sweep_section(m, loop + [loop[0]], S.circle(wire, wire_n), closed_section=True)


def chain_along(kit, name, pts, slot, lv, wire=0.003, inner=(0.030, 0.012), q0=(8, 4), q1=(4, 3), up=(0.0, 1.0, 0.0), start_turn=0,
                quality=None, tube_lv=2):
    """Links laid along a Unity polyline pts (each link's centre on the path, pitch = the inner length, every other link turned 90 deg).
    LOD0 links of q0 = (path segments, wire sides) (or quality(centre) -> (path, wire) per link), LOD1 q1; from tube_lv on, a thin tube
    on the path. Returns the number of links."""
    P = [Vector(p) for p in pts]
    L = [0.0]
    for i in range(1, len(P)):
        L.append(L[-1] + (P[i] - P[i - 1]).length)
    if lv >= tube_lv:
        S.tube(kit, name + " tube", [tuple(p) for p in P], wire * 1.7, slot, lv, cap0=True, cap1=True, lo=4 if lv == 1 else 6)
        return 0
    pitch = inner[0]
    half = (inner[0] + 2 * wire) / 2.0
    rc = (inner[1] + 2 * wire) / 2.0
    pn, wn = q0 if lv == 0 else q1
    m = S.Mesh()
    s = pitch * 0.5
    count = 0
    upv = Vector(up)
    while s < L[-1] - pitch * 0.25:
        k = 1
        while k < len(L) - 1 and L[k] < s:
            k += 1
        f = (s - L[k - 1]) / max(L[k] - L[k - 1], 1e-9)
        c = P[k - 1].lerp(P[k], f)
        t = (P[k] - P[k - 1]).normalized()
        a = upv - t * upv.dot(t)
        if a.length < 1e-6:
            a = Vector((1.0, 0.0, 0.0)) - t * t.x
        a.normalize()
        b = t.cross(a).normalized()
        side = a if (count + start_turn) % 2 == 0 else b
        if quality is not None and lv == 0:
            pn, wn = quality(c)
        _link(m, c, t, side, half, rc, wire, pn, wn)
        count += 1
        s += pitch
    m.part(kit, name, slot)
    return count


def catenary_pts(x0, x1, y, z, sag, n):
    """A parabola from (x0, y) to (x1, y) sagging by sag at the middle, at depth z."""
    out = []
    for k in range(n + 1):
        u = k / n
        x = x0 + (x1 - x0) * u
        out.append((x, y - sag * (1.0 - (2.0 * u - 1.0) ** 2), z))
    return out




def rivet(m, c, nrm, r, segs=6, h=None):
    """A low dome (rivet head, pan screw) into Mesh m at Unity point c facing the Unity direction nrm."""
    n = Vector(nrm).normalized()
    a = Vector((0.0, 1.0, 0.0)) if abs(n.y) < 0.9 else Vector((1.0, 0.0, 0.0))
    u = n.cross(a).normalized()
    v = n.cross(u).normalized()
    h = r * 0.45 if h is None else h
    C = Vector(c)
    ring = [C + (u * math.cos(2 * math.pi * k / segs) + v * math.sin(2 * math.pi * k / segs)) * r for k in range(segs)]
    mid = [C + n * (h * 0.7) + (u * math.cos(2 * math.pi * k / segs) + v * math.sin(2 * math.pi * k / segs)) * (r * 0.6) for k in range(segs)]
    top = C + n * h
    for k in range(segs):
        k1 = (k + 1) % segs
        qd(m, tuple(ring[k]), tuple(ring[k1]), tuple(mid[k1]), tuple(mid[k]), (0, 0), (r, 0), (r, r), (0, r), tuple(n + (ring[k] - C).normalized()))
        tri(m, tuple(mid[k]), tuple(mid[k1]), tuple(top), (0, 0), (r, 0), (r / 2, r), tuple(n))


def angle_y(kit, name, x0, z0, la, lb, t, sx, sz, y0, y1, slot, r_in=0.0):
    """A steel angle running along y from y0 to y1: its corner at (x0, z0), leg a (length la) along sx*x, leg b (lb) along sz*z,
    thickness t; metre UVs, capped, normals out."""
    sec = angle_section(la, lb, t, r_in=r_in, segs=2 if r_in > 0 else 0)
    pts = [(x0 + sx * a, z0 + sz * b) for (a, b) in sec]
    m = S.Mesh()
    n = len(pts)
    arc = [0.0]
    for i in range(1, n + 1):
        p, q = pts[i - 1], pts[i % n]
        arc.append(arc[-1] + math.hypot(q[0] - p[0], q[1] - p[1]))
    for i in range(n):
        j = (i + 1) % n
        (xa, za), (xb, zb) = pts[i], pts[j]
        m.quad((xa, y0, za), (xb, y0, zb), (xb, y1, zb), (xa, y1, za), (arc[i], y0), (arc[i + 1], y0), (arc[i + 1], y1), (arc[i], y1))
    m.face(tuple(m.vert((x, y1, z)) for (x, z) in pts), [(x, z) for (x, z) in pts])
    m.face(tuple(reversed([m.vert((x, y0, z)) for (x, z) in pts])), [(x, z) for (x, z) in reversed(pts)])
    return recalc(m.part(kit, name, slot))


def angle_x(kit, name, y0, z0, la, lb, t, sy, sz, x0, x1, slot, r_in=0.0):
    """A steel angle running along x: corner at (y0, z0), leg a along sy*y, leg b along sz*z."""
    sec = angle_section(la, lb, t, r_in=r_in, segs=2 if r_in > 0 else 0)
    pts = [(y0 + sy * a, z0 + sz * b) for (a, b) in sec]
    m = S.Mesh()
    n = len(pts)
    arc = [0.0]
    for i in range(1, n + 1):
        p, q = pts[i - 1], pts[i % n]
        arc.append(arc[-1] + math.hypot(q[0] - p[0], q[1] - p[1]))
    for i in range(n):
        j = (i + 1) % n
        (ya, za), (yb, zb) = pts[i], pts[j]
        m.quad((x0, ya, za), (x1, ya, za), (x1, yb, zb), (x0, yb, zb), (x0, arc[i]), (x1, arc[i]), (x1, arc[i + 1]), (x0, arc[i + 1]))
    m.face(tuple(m.vert((x1, y, z)) for (y, z) in pts), [(y, z) for (y, z) in pts])
    m.face(tuple(reversed([m.vert((x0, y, z)) for (y, z) in pts])), [(y, z) for (y, z) in reversed(pts)])
    return recalc(m.part(kit, name, slot))


def ring_path(c, r, axis_u, axis_v, a0, a1, n):
    C, U, V = Vector(c), Vector(axis_u), Vector(axis_v)
    return [tuple(C + U * (r * math.cos(a0 + (a1 - a0) * k / n)) + V * (r * math.sin(a0 + (a1 - a0) * k / n))) for k in range(n + 1)]


# ============================================================================= Kit_OverheadCoilingDoor
OPEN_W, OPEN_H = 3.658, 4.267          # 12 x 14 ft
CURT_HW = 1.880                         # the curtain runs 2 in into each guide
SLAT = 0.0667                           # 2-5/8 in flat slat (W2)
SLAT_D = 0.0159                         # 5/8 in profile
ZC = 0.070                              # the curtain's centre plane, off the wall's room face
G_IN = 1.826                            # the guides' inner edge (over the jamb by 3 mm)
G_T = 0.0048                            # guide angles 2-1/2 x 2-1/2 x 3/16 in
G_TW = 0.0064                           # wall angle 3 x 3 x 1/4 in
G_X1 = 1.894                            # front angle's return leg (inner face)
HEAD_X = 2.000                          # head plates' inner faces
HP_T = 0.0064
AXLE = (4.620, 0.300)                   # barrel axis (y, z)
BARREL_R = 0.0842                       # 6-5/8 in pipe
HOOD = [(0.000, 4.955), (0.455, 4.862), (0.540, 4.430), (0.140, 4.322)]   # half-hex hood, (z, y) from the wall top round to the lip
WHEEL = (2.118, 4.300, 0.240)           # hand-chain pocket wheel centre (x, y, z), outside the +X head plate
WHEEL_R = 0.140
CHAIN_BOTTOM = 1.10
Y_TOP = 4.43                            # the curtain disappears behind the hood's lip


def _bar_bottom(state):
    return {"halfUp": 1.20, "shut": 0.0, "open": 4.247}[state]


def _coil_r(state):
    # rolled length x slat depth = pi (R^2 - r0^2); the curtain runs 4.62 m from the barrel to the bar when shut
    hang = {"halfUp": Y_TOP - 1.287, "shut": Y_TOP - 0.087, "open": 0.18}[state]
    rolled = max(0.08, 4.62 - hang)
    return math.sqrt((BARREL_R + 0.004) ** 2 + rolled * SLAT_D / math.pi)


def _curtain(kit, lv, state, rnd):
    """The slats from the bottom bar up behind the hood's lip: the room face as real V joints (each slat at its own small tilt and bow,
    three dented low slats in halfUp), the outside face beaded at each curl."""
    if state == "open":
        return
    y0 = _bar_bottom(state) + 0.087            # astragal 20 + bottom bar 51 + clearance
    n = max(1, int(round((Y_TOP - y0) / SLAT)))
    zf, zb = ZC + SLAT_D / 2.0, ZC - SLAT_D / 2.0
    front, back = S.Mesh(), S.Mesh()
    xs_plain = [-CURT_HW + 2 * CURT_HW * i / 4.0 for i in range(5)] if lv == 0 else [-CURT_HW, CURT_HW]
    dents = {0: -0.62, 1: 0.95, 3: -0.15} if (lv == 0 and state == "halfUp") else {}
    for k in range(n):
        ya = y0 + k * SLAT
        tilt = rnd.uniform(-0.0004, 0.0004) if lv == 0 else 0.0
        bow = rnd.uniform(-0.0006, 0.0006) if lv == 0 else 0.0
        if lv == 0:
            prof = [(ya, zf - 0.0042 - tilt), (ya + 0.0055, zf - tilt * 0.8), (ya + 0.0612, zf + tilt * 0.8), (ya + SLAT, zf - 0.0042 + tilt)]
        elif lv == 1:
            prof = [(ya, zf - 0.0035), (ya + 0.0060, zf), (ya + SLAT, zf - 0.0035)]
        else:
            prof = [(ya, zf), (ya + SLAT, zf)]
        dx = dents.get(k)
        xs = [-CURT_HW + 2 * CURT_HW * i / 24.0 for i in range(25)] if dx is not None else xs_plain

        def zz(x, z, y):
            d = 0.0
            if dx is not None:
                d = -0.009 * math.exp(-((x - dx) / 0.22) ** 2) * math.exp(-((y - (ya + SLAT * 0.5)) / 0.05) ** 2)
            return z + d + bow * (1.0 - (x / CURT_HW) ** 2)
        for i in range(len(prof) - 1):
            (ya0, z0), (ya1, z1) = prof[i], prof[i + 1]
            for j in range(len(xs) - 1):
                xa, xb = xs[j], xs[j + 1]
                qd(front, (xa, ya0, zz(xa, z0, ya0)), (xb, ya0, zz(xb, z0, ya0)), (xb, ya1, zz(xb, z1, ya1)), (xa, ya1, zz(xa, z1, ya1)),
                   (xa, ya0), (xb, ya0), (xb, ya1), (xa, ya1), (0, 0, 1))
        if lv < 2:
            bprof = [(ya, zb + 0.0035), (ya + 0.010, zb), (ya + SLAT, zb)] if lv == 0 else [(ya, zb), (ya + SLAT, zb)]
            for i in range(len(bprof) - 1):
                (ya0, z0), (ya1, z1) = bprof[i], bprof[i + 1]
                qd(back, (-CURT_HW, ya0, z0), (CURT_HW, ya0, z0), (CURT_HW, ya1, z1), (-CURT_HW, ya1, z1),
                   (-CURT_HW, ya0), (CURT_HW, ya0), (CURT_HW, ya1), (-CURT_HW, ya1), (0, 0, -1))
    if lv == 2:
        qd(back, (-CURT_HW, y0, zb), (CURT_HW, y0, zb), (CURT_HW, Y_TOP, zb), (-CURT_HW, Y_TOP, zb), (-CURT_HW, y0), (CURT_HW, y0), (CURT_HW, Y_TOP), (-CURT_HW, Y_TOP), (0, 0, -1))
    front.part(kit, "curtain front", GALV, merge=False)
    back.part(kit, "curtain back", GALV, merge=False)
    if lv == 0:
        # malleable endlocks on alternate slats, riding in the guide slot (seen in the slot at close range)
        for k in range(0, n, 2):
            yc = y0 + k * SLAT + SLAT * 0.5
            for sx in (-1.0, 1.0):
                S.box(kit, (0.030, 0.050, 0.026), (sx * (CURT_HW - 0.004), yc, ZC), PAINT, 2, w=0.0, name="endlock")


def _bottom_bar(kit, lv, state):
    yb = _bar_bottom(state)
    t = 0.0032
    y0 = yb + 0.020
    xa, xb = -CURT_HW + 0.02, CURT_HW - 0.02
    # astragal: a black rubber bulb under the bar
    if lv < 2:
        n = 10 if lv == 0 else 6
        bulb = [(ZC + 0.012 * math.cos(math.pi + math.pi * k / n), yb + 0.010 - 0.010 * math.sin(math.pi * k / n)) for k in range(n + 1)]
        bulb += [(ZC + 0.006, y0 + 0.002), (ZC - 0.006, y0 + 0.002)]
        extrude_profile(kit, "astragal", bulb, xa, xb, RUBBER)
    else:
        S.box(kit, (xb - xa, 0.020, 0.024), (0.0, yb + 0.010, ZC), RUBBER, lv, w=0.0, name="astragal")
    # the bar: two 2 x 2 x 1/8 in angles back to back on the bottom slat, their legs out at the bottom
    for sz in (1.0, -1.0):
        z_in = ZC + sz * SLAT_D / 2.0
        if lv < 2:
            angle_x(kit, "bottom bar", y0, z_in, 0.051, 0.051, t, 1.0, sz, xa + 0.01, xb - 0.01, PAINT)
        else:
            S.box(kit, (xb - xa, 0.051, 0.051), (0.0, y0 + 0.0255, z_in + sz * 0.0255), PAINT, lv, w=0.0, name="bottom bar")
    zf = ZC + SLAT_D / 2.0 + t
    if lv == 0:
        m = S.Mesh()
        x = -CURT_HW + 0.15
        while x < CURT_HW - 0.1:
            rivet(m, (x, y0 + 0.032, zf), (0, 0, 1), 0.0075, segs=8)
            x += 0.30
        m.part(kit, "bar bolts", PAINT)
    if lv < 2:
        # two lift handles of 3/8 in rod bent in a U on the room face, on welded tabs
        for hx in (-0.80, 0.80):
            pts = [(hx - 0.07, y0 + 0.030, zf), (hx - 0.07, y0 + 0.030, zf + 0.030), (hx - 0.05, y0 + 0.030, zf + 0.045),
                   (hx + 0.05, y0 + 0.030, zf + 0.045), (hx + 0.07, y0 + 0.030, zf + 0.030), (hx + 0.07, y0 + 0.030, zf)]
            S.tube(kit, "lift handle", pts, 0.0048, PAINT, lv, cap0=False, cap1=False, lo=8)
            for sx in (-0.07, 0.07):
                S.box(kit, (0.022, 0.030, 0.004), (hx + sx, y0 + 0.030, zf + 0.002), PAINT, max(lv, 1), w=0.0008, name="handle tab")
        # the slide bolt at the -X end: its case on the bar, the bolt shot into the guide
        S.box(kit, (0.110, 0.032, 0.014), (-CURT_HW + 0.12, y0 + 0.030, zf + 0.007), PAINT, lv, w=0.0012, name="slide bolt case")
        S.cyl_x(kit, 0.007, 0.15, (-CURT_HW + 0.03, y0 + 0.030, zf + 0.009), PAINT, lv, name="slide bolt")
        if lv == 0:
            S.cyl_z(kit, 0.004, 0.022, (-CURT_HW + 0.10, y0 + 0.030, zf + 0.022), PAINT, lv, name="bolt knob")
            S.cyl_z(kit, 0.007, 0.010, (-CURT_HW + 0.10, y0 + 0.030, zf + 0.036), PAINT, lv, name="bolt knob ball")


def _guides(kit, lv):
    """Each side: two 2-1/2 in guide angles back to back forming the curtain's slot, on a 3 in wall angle anchored to the jamb; bolts."""
    t, tw = G_T, G_TW
    zf = ZC + 0.020                       # front angle's flat leg (room side of the slot)
    zk = ZC - 0.020                       # back angle's flat leg (wall side)
    xb1 = G_X1 + t                        # back angle's return leg starts
    xw1 = xb1 + t                         # wall angle's return leg starts
    for sx in (-1.0, 1.0):
        if lv < 2:
            # front angle: corner at (G_X1 + t, zf + t): flat leg toward the opening (-sx), return leg toward the wall (-z)
            angle_y(kit, "guide front angle", sx * xb1, zf + t, xb1 - G_IN, 0.0635, t, -sx, -1.0, 0.0, Y_TOP, PAINT)
            # back angle: corner at (G_X1 + 2t, zk - t): flat leg toward the opening, return leg toward the room (+z)
            angle_y(kit, "guide back angle", sx * xw1, zk - t, xw1 - G_IN, 0.0635, t, -sx, 1.0, 0.0, Y_TOP, PAINT)
            # wall angle: corner at (G_X1 + 2t, 0): leg along the wall outward (+sx), leg off the wall (+z)
            angle_y(kit, "wall angle", sx * xw1, 0.0, 0.076, 0.076, tw, sx, 1.0, 0.0, Y_TOP, PAINT)
        else:
            S.box(kit, (0.17, Y_TOP, zf + t), (sx * (G_IN + 0.085), Y_TOP / 2.0, (zf + t) / 2.0), PAINT, lv, w=0.0, name="guide")
        if lv == 0:
            y = 0.18
            while y < Y_TOP - 0.1:
                hex_bolt_x(kit, (sx * (xw1 + tw), y, 0.045), 0.019, 0.010, PAINT, 1, name="guide bolt", sign=sx)
                y += 0.45
            y = 0.30
            while y < Y_TOP - 0.1:
                xa = sx * (xw1 + 0.048)
                hex_bolt_z(kit, (xa, y, tw), 0.017, 0.010, PAINT, 1, name="anchor nut")
                S.cyl_z(kit, 0.0055, 0.008, (xa, y, tw + 0.014), PAINT, 2, w=0.0, name="anchor stud", lo=8)
                y += 0.60


def _plate_outline(lv):
    if lv == 0:
        prof = [(0.000, 4.300), (0.500, 4.300)]
        for k in range(1, 5):
            a = -math.pi / 2 + (math.pi / 2) * k / 4
            prof.append((0.500 + 0.080 * math.cos(a), 4.380 + 0.080 * math.sin(a)))
        for k in range(1, 5):
            a = (math.pi / 2) * k / 4
            prof.append((0.500 + 0.080 * math.cos(a), 4.870 + 0.080 * math.sin(a)))
        prof.append((0.000, 4.950))
        return prof
    return [(0.000, 4.300), (0.580, 4.300), (0.580, 4.950), (0.000, 4.950)]


def _head_plates(kit, lv):
    """1/4 in head plates (brackets) at both ends with their wall flanges, the shaft bearings, and the spring wheel on -X."""
    yb, zb = AXLE
    for sx in (-1.0, 1.0):
        x0, x1 = sx * HEAD_X, sx * (HEAD_X + HP_T)
        pts = _plate_outline(lv)
        m = S.Mesh()
        nn = len(pts)
        for i in range(nn):
            j = (i + 1) % nn
            (za, ya), (zb_, yb_) = pts[i], pts[j]
            m.quad((x0, ya, za), (x0, yb_, zb_), (x1, yb_, zb_), (x1, ya, za), (za, ya), (zb_, yb_), (zb_ + HP_T, yb_), (za + HP_T, ya))
        m.face(tuple(m.vert((x0, y, z)) for (z, y) in pts), [(z, y) for (z, y) in pts])
        m.face(tuple(reversed([m.vert((x1, y, z)) for (z, y) in pts])), [(z, y) for (z, y) in reversed(pts)])
        recalc(m.part(kit, "head plate", PAINT))
        if lv < 2:
            S.box(kit, (0.060, 0.65, HP_T), (sx * (HEAD_X + 0.030), 4.625, HP_T / 2.0), PAINT, lv, w=0.001, name="plate flange")
            S.cyl_x(kit, 0.055, 0.012, (sx * (HEAD_X + HP_T + 0.006), yb, zb), PAINT, max(lv, 1), name="bearing flange", lo=24)
            S.cyl_x(kit, 0.022, 0.050, (sx * (HEAD_X + HP_T + 0.030), yb, zb), PAINT, max(lv, 1), name="shaft", lo=16)
        if lv == 0:
            for (dy, dz) in ((0.04, 0.04), (-0.04, 0.04), (0.04, -0.04), (-0.04, -0.04)):
                hex_bolt_x(kit, (sx * (HEAD_X + HP_T + 0.012), yb + dy, zb + dz), 0.011, 0.006, PAINT, lv, name="bearing bolt", sign=sx)
            for y in (4.36, 4.62, 4.88):
                hex_bolt_z(kit, (sx * (HEAD_X + 0.042), y, HP_T), 0.017, 0.009, PAINT, 1, name="plate anchor")
    if lv < 2:
        xw = -(HEAD_X + HP_T + 0.068)
        S.cyl_x(kit, 0.100, 0.018, (xw, yb, zb), PAINT, max(lv, 1), name="tension wheel", lo=32)
        if lv == 0:
            for k in range(4):
                a = 2 * math.pi * k / 4 + 0.4
                hex_bolt_x(kit, (xw - 0.009, yb + 0.068 * math.cos(a), zb + 0.068 * math.sin(a)), 0.013, 0.007, PAINT, lv, name="wheel set screw", sign=-1.0)
            hex_bolt_x(kit, (xw - 0.010, yb, zb), 0.030, 0.016, PAINT, lv, name="wheel nut", sign=-1.0)


def _coil(kit, lv, state):
    yb, zb = AXLE
    if lv < 2:
        S.cyl_x(kit, _coil_r(state), 2 * CURT_HW, (0.0, yb, zb), GALV, 2, w=0.0, name="coil", lo=20)
    S.cyl_x(kit, BARREL_R, 2 * HEAD_X - 0.02, (0.0, yb, zb), GALV, max(lv, 1), w=0.0, name="barrel", lo=12)


def _hood_outer(lv):
    pts = list(HOOD)
    if lv > 0:
        return pts
    out = [pts[0]]
    for i in range(1, len(pts) - 1):
        a, b, c = Vector(pts[i - 1]), Vector(pts[i]), Vector(pts[i + 1])
        u1, u2 = (a - b).normalized(), (c - b).normalized()
        r = 0.010
        p0, p2 = b + u1 * r, b + u2 * r
        for k in range(4):
            f = k / 3.0
            q = (p0 * (1 - f) ** 2 + b * 2 * f * (1 - f) + p2 * f * f)
            out.append((q.x, q.y))
    out.append(pts[-1])
    return out


def _hood(kit, lv):
    """24 ga half-hex hood between the head plates: eased folds, the hemmed lip, the top flange screwed on the wall, a lap seam with
    rivets at mid-span."""
    t = 0.0008
    x0, x1 = -HEAD_X + 0.002, HEAD_X - 0.002
    outer = _hood_outer(lv)
    if lv < 2:
        inner = []
        for i, (z, y) in enumerate(outer):
            j0, j1 = max(0, i - 1), min(len(outer) - 1, i + 1)
            d = Vector((outer[j1][0] - outer[j0][0], outer[j1][1] - outer[j0][1])).normalized()
            nrm = Vector((d.y, -d.x))          # points out of the hood (away from the coil) for this winding
            inner.append((z - nrm.x * t, y - nrm.y * t))
        extrude_profile(kit, "hood", outer + list(reversed(inner)), x0, x1, GALV)
        zl, yl = outer[-1]
        S.cyl_x(kit, 0.0045, x1 - x0, (0.0, yl - 0.002, zl + 0.002), GALV, lv, w=0.0, name="hood hem", lo=10)
        S.box(kit, (x1 - x0, 0.040, 0.0016), (0.0, outer[0][1] - 0.020, 0.0035), GALV, lv, w=0.0, name="hood flange")
        if lv == 0:
            m = S.Mesh()
            x = x0 + 0.15
            while x < x1 - 0.1:
                rivet(m, (x, outer[0][1] - 0.022, 0.0043), (0, 0, 1), 0.0055, segs=6)
                x += 0.30
            # the lap seam: a 30 mm cover strip proud of the sheet across the profile at x 0, a rivet every 90 mm along it
            strip = []
            for i, (z, y) in enumerate(outer):
                j0, j1 = max(0, i - 1), min(len(outer) - 1, i + 1)
                d = Vector((outer[j1][0] - outer[j0][0], outer[j1][1] - outer[j0][1])).normalized()
                nrm = Vector((d.y, -d.x))
                strip.append((z + nrm.x * 0.0012, y + nrm.y * 0.0012, nrm))
            sm = S.Mesh()
            for i in range(len(strip) - 1):
                (za, ya, na), (zb_, yb_, nb) = strip[i], strip[i + 1]
                qd(sm, (-0.015, ya, za), (0.015, ya, za), (0.015, yb_, zb_), (-0.015, yb_, zb_), (0, 0), (0.03, 0), (0.03, 0.1), (0, 0.1), (0.0, na.y, na.x))
                L = math.hypot(zb_ - za, yb_ - ya)
                nr = max(1, int(L / 0.09))
                for r in range(nr):
                    f = (r + 0.5) / nr
                    rivet(m, (0.0, ya + (yb_ - ya) * f, za + (zb_ - za) * f), (0.0, na.y, na.x), 0.0035, segs=6)
            for (z, y, nv) in (strip[0], strip[-1]):
                pass
            sm.part(kit, "hood seam strip", GALV)
            m.part(kit, "hood rivets", GALV)
    else:
        m = S.Mesh()
        for i in range(len(outer) - 1):
            (za, ya), (zb_, yb_) = outer[i], outer[i + 1]
            d = Vector((zb_ - za, yb_ - ya)).normalized()
            qd(m, (x0, ya, za), (x1, ya, za), (x1, yb_, zb_), (x0, yb_, zb_), (x0, ya), (x1, ya), (x1, yb_), (x0, yb_), (0.0, -d.x, d.y))
        m.part(kit, "hood", GALV)


def _chain_quality(c):
    # finer links where a hand reaches (below 2.3 m), coarse ones up at the hoist (seen from 2 m or more below)
    return (6, 4) if c.y < 2.3 else (4, 3)


def _hoist(kit, lv):
    """The hand-chain hoist on the +X head plate: the cast reduction case from the shaft down to the pocket wheel's axle, the pocket wheel,
    the chain guard round its lower half, the hand chain loop, the wall keeper."""
    wx, wy, wz = WHEEL
    yb, zb = AXLE
    xc = HEAD_X + HP_T
    if lv < 2:
        S.box(kit, (0.070, 0.420, 0.180), (xc + 0.035, (yb + wy) / 2.0, (zb + wz) / 2.0), PAINT, lv, w=0.008 if lv == 0 else 0.004, name="gear case")
        if lv == 0:
            for dy in (-0.15, -0.05, 0.05, 0.15):
                S.box(kit, (0.006, 0.010, 0.170), (xc + 0.073, (yb + wy) / 2.0 + dy, (zb + wz) / 2.0), PAINT, lv, w=0.0015, name="case rib")
            for (dy, dz) in ((0.19, 0.075), (0.19, -0.075), (-0.19, 0.075), (-0.19, -0.075)):
                hex_bolt_x(kit, (xc + 0.070, (yb + wy) / 2.0 + dy, (zb + wz) / 2.0 + dz), 0.013, 0.007, PAINT, lv, name="case bolt")
        rim = [(WHEEL_R - 0.010, -0.014), (WHEEL_R + 0.004, -0.014), (WHEEL_R + 0.006, -0.010), (WHEEL_R - 0.003, -0.004),
               (WHEEL_R - 0.003, 0.004), (WHEEL_R + 0.006, 0.010), (WHEEL_R + 0.004, 0.014), (WHEEL_R - 0.010, 0.014), (WHEEL_R - 0.010, -0.014)]
        if lv == 1:
            rim = [(WHEEL_R - 0.010, -0.014), (WHEEL_R + 0.005, -0.012), (WHEEL_R + 0.005, 0.012), (WHEEL_R - 0.010, 0.014), (WHEEL_R - 0.010, -0.014)]
        S.lathe_axis(kit, rim, (wx, wy, wz), "x", CHAIN, lv, name="pocket wheel rim", close_top=False, close_bottom=False, lo=32)
        for k in range(5):
            a = 2 * math.pi * k / 5 + 0.3
            p0 = (wx, wy + 0.026 * math.cos(a), wz + 0.026 * math.sin(a))
            p1 = (wx, wy + (WHEEL_R - 0.008) * math.cos(a), wz + (WHEEL_R - 0.008) * math.sin(a))
            S.tube(kit, "wheel spoke", [p0, p1], 0.0075, CHAIN, lv, cap0=False, cap1=False, lo=8)
        S.cyl_x(kit, 0.030, 0.040, (wx, wy, wz), CHAIN, lv, name="wheel hub")
        S.cyl_x(kit, 0.012, 0.090, (wx - 0.030, wy, wz), CHAIN, lv, name="wheel shaft")
        # the chain guard: a bent strap round the wheel's lower half, open at the strands
        n = 12 if lv == 0 else 6
        rg = WHEEL_R + 0.024
        m = S.Mesh()
        arc = [math.pi * 1.10 + math.pi * 0.80 * k / n for k in range(n + 1)]
        for i in range(n):
            a0, a1 = arc[i], arc[i + 1]
            p0 = (wy + rg * math.sin(a0), wz - rg * math.cos(a0))
            p1 = (wy + rg * math.sin(a1), wz - rg * math.cos(a1))
            out = (0.0, math.sin((a0 + a1) / 2), -math.cos((a0 + a1) / 2))
            for (dzs, nrm) in ((0.0, out), (-0.002, tuple(-v for v in out))):
                r0 = (wy + (rg + dzs) * math.sin(a0), wz - (rg + dzs) * math.cos(a0))
                r1 = (wy + (rg + dzs) * math.sin(a1), wz - (rg + dzs) * math.cos(a1))
                qd(m, (wx - 0.022, r0[0], r0[1]), (wx + 0.022, r0[0], r0[1]), (wx + 0.022, r1[0], r1[1]), (wx - 0.022, r1[0], r1[1]),
                   (0, a0 * rg), (0.044, a0 * rg), (0.044, a1 * rg), (0, a1 * rg), nrm)
        m.part(kit, "chain guard", PAINT)
        S.box(kit, (0.006, 0.090, 0.040), (wx - 0.028, wy - rg - 0.040, wz), PAINT, lv, w=0.001, name="guard stay")
    else:
        S.box(kit, (0.070, 0.42, 0.18), (xc + 0.035, (yb + wy) / 2.0, (zb + wz) / 2.0), PAINT, lv, w=0.0, name="gear case")
        S.cyl_x(kit, WHEEL_R, 0.03, (wx, wy, wz), CHAIN, 1, w=0.0, name="pocket wheel", lo=8)
    # the hand chain: strand A down from the wheel's wall side, the loop, strand B up, over the wheel's top in its pockets
    za, zb2 = wz - WHEEL_R, wz + WHEEL_R
    yl = CHAIN_BOTTOM + WHEEL_R
    path = [(wx, wy, za), (wx, (wy + yl) / 2.0, za - 0.002), (wx, yl, za)]
    path += [(wx, yl - WHEEL_R * math.sin(math.pi * k / 10), wz - WHEEL_R * math.cos(math.pi * k / 10)) for k in range(1, 11)]
    path += [(wx, (wy + yl) / 2.0, zb2 + 0.002), (wx, wy, zb2)]
    path += [(wx, wy + WHEEL_R * math.sin(math.pi * k / 10), wz + WHEEL_R * math.cos(math.pi * k / 10)) for k in range(1, 10)]
    chain_along(kit, "hand chain", path, CHAIN, lv, wire=0.0024, inner=(0.027, 0.010), q0=(6, 4), q1=(4, 3), up=(1.0, 0.0, 0.0),
                quality=_chain_quality, tube_lv=1)
    if lv < 2:
        kx = HEAD_X + 0.10
        S.box(kit, (0.050, 0.140, 0.006), (kx, 1.25, 0.003), PAINT, lv, w=0.001, name="keeper plate")
        S.box(kit, (0.012, 0.090, 0.045), (kx, 1.26, 0.028), PAINT, lv, w=0.002, name="keeper cleat")
        if lv == 0:
            m = S.Mesh()
            for y in (1.195, 1.305):
                rivet(m, (kx, y, 0.006), (0, 0, 1), 0.005, segs=8)
            m.part(kit, "keeper screws", PAINT)


def coiling_door(kit, lv, state="halfUp"):
    rnd = random.Random(14 + lv)
    _curtain(kit, lv, state, rnd)
    _bottom_bar(kit, lv, state)
    _guides(kit, lv)
    _head_plates(kit, lv)
    _coil(kit, lv, state)
    _hood(kit, lv)
    _hoist(kit, lv)


def coiling_door_meta(kit, state):
    yb = _bar_bottom(state)
    if state != "open":
        collider_u(kit, (0.0, (yb + OPEN_H) / 2.0, ZC), (OPEN_W, OPEN_H - yb, 0.06))
    else:
        kit.no_collider()
    anchor_u(kit, "bar_bottom", (0.0, yb, ZC))
    anchor_u(kit, "hoist_wheel", WHEEL)
    kit.meta["state"] = state
    kit.meta["opening"] = [OPEN_W, OPEN_H]
    kit.min_ceiling = 5.0


# ============================================================================= Kit_DockLeveler
TREAD_Y = "Prop_TreadPlateYellow"     # NEW: the tread plate's yellow-painted edges (its own albedo on the tread's N / S)
kitlib.register_slot(TREAD_Y, (0.75, 0.58, 0.05), 0.45, 0.2)
LV_HW = 0.900                         # deck half width (6 ft leveler)
LV_PIT_HW = 0.915
LV_Z0, LV_Z1 = -2.195, -0.035         # deck from the rear hinge to its header at the front
LV_TOP = 0.0035                       # 3.5 mm proud of the platform (no pit hole needed in the platform box)
LIP = (-0.022, 0.012, 0.020)          # lip hinge axis (y, z) and tube radius
LIP_LEN = 0.406                       # 16 in


def leveler(kit, lv):
    hw, z0, z1 = LV_HW, LV_Z0, LV_Z1
    # deck plate (6.4 mm tread plate on its skirts; below y 0 it is inside the platform)
    S.box(kit, (2 * hw, LV_TOP + 0.006, z1 - z0), (0.0, (LV_TOP - 0.006) / 2.0, (z0 + z1) / 2.0), TREAD, lv, w=0.0015, name="deck")
    # yellow stripes along both long edges, 0.4 mm proud
    if lv < 2:
        for sx in (-1.0, 1.0):
            S.box(kit, (0.075, 0.0008, z1 - z0 - 0.010), (sx * (hw - 0.0425), LV_TOP + 0.0002, (z0 + z1) / 2.0), TREAD_Y, 2, w=0.0, name="deck stripe")
    # the pit's curb angles (left, right, rear) flush with the floor, the dark gaps round the deck
    for sx in (-1.0, 1.0):
        S.box(kit, (0.076, 0.0015, (z1 - z0) + 0.10), (sx * (LV_PIT_HW + 0.038), 0.00075, (z0 + z1) / 2.0 - 0.03), STEEL, 2, w=0.0, name="curb angle")
        S.box(kit, (LV_PIT_HW - hw, 0.0008, z1 - z0), (sx * (hw + LV_PIT_HW) / 2.0, 0.0004, (z0 + z1) / 2.0), DARK, 2, w=0.0, name="pit gap")
    S.box(kit, (2 * LV_PIT_HW + 0.152, 0.0015, 0.076), (0.0, 0.00075, z0 - 0.015 - 0.038), STEEL, 2, w=0.0, name="curb angle")
    S.box(kit, (2 * LV_PIT_HW, 0.0008, 0.015), (0.0, 0.0004, z0 - 0.0075), DARK, 2, w=0.0, name="pit gap")
    # rear hinge knuckles in the rear gap
    nk = 10 if lv < 2 else 1
    if lv < 2:
        L = 2 * hw / nk
        for k in range(nk):
            xc = -hw + L * (k + 0.5)
            S.cyl_x(kit, 0.012, L - 0.006, (xc, LV_TOP - 0.012, z0 - 0.004), STEEL, 1, w=0.0, name="rear hinge", lo=10)
    # the pull ring in its pocket at the rear centre, its chain going down into the pit
    if lv < 2:
        S.box(kit, (0.130, 0.0006, 0.085), (0.0, LV_TOP + 0.0003, z0 + 0.120), DARK, 2, w=0.0, name="ring pocket")
        ring = ring_path((0.0, LV_TOP + 0.0045, z0 + 0.120), 0.028, (1, 0, 0), (0, 0, 1), 0.0, 2 * math.pi, 16 if lv == 0 else 8)
        S.tube(kit, "pull ring", ring, 0.0042, STEEL, lv, cap0=False, cap1=False, lo=8)
        chain_along(kit, "pull chain", [(0.0, LV_TOP + 0.004, z0 + 0.092), (0.0, LV_TOP - 0.004, z0 + 0.070), (0.0, -0.04, z0 + 0.066)],
                    STEEL, lv, wire=0.0028, inner=(0.022, 0.010), q0=(6, 4), q1=(4, 3), up=(1, 0, 0), tube_lv=2)
    # the deck's front header plate down to the lip hinge
    S.box(kit, (2 * hw, 0.040, 0.012), (0.0, LV_TOP - 0.020, z1 + 0.006), TREAD, max(lv, 1), w=0.001, name="header")
    _lip(kit, lv)


def _lip(kit, lv):
    hw = LV_HW
    yh, zh, rh = LIP
    # the full-width hinge: knuckles alternating between the header and the lip, a pin through them, its ends peened
    nk = 14 if lv == 0 else 7 if lv == 1 else 1
    L = 2 * hw / nk
    for k in range(nk):
        xc = -hw + L * (k + 0.5)
        S.cyl_x(kit, rh, L - (0.004 if lv < 2 else 0.0), (xc, yh, zh), STEEL, lv, w=0.0012, name="lip knuckle", lo=16)
    if lv < 2:
        for sx in (-1.0, 1.0):
            S.cyl_x(kit, 0.008, 0.012, (sx * (hw + 0.006), yh, zh), STEEL, lv, name="hinge pin end")
    # the lip plate: 5/8 in, hanging in front of the dock face, its leading edge tapered on the back (16 -> 6 mm over 50 mm)
    zf, zb = zh + 0.016, zh               # front (tread side, toward the bay) and back faces
    ytop, ybot = yh - 0.010, yh - LIP_LEN
    prof = [(zb, ytop), (zf, ytop), (zf, ybot + 0.004), (zf - 0.004, ybot), (zb + 0.010 - 0.006, ybot), (zb + 0.010, ybot + 0.050)]
    if lv == 2:
        prof = [(zb, ytop), (zf, ytop), (zf, ybot), (zb + 0.004, ybot)]
    extrude_profile(kit, "lip plate", prof, -hw + 0.004, hw - 0.004, TREAD)
    if lv < 2:
        # yellow edge bands on the tread face and along the toe, 0.4 mm proud
        for sx in (-1.0, 1.0):
            S.box(kit, (0.100, LIP_LEN - 0.030, 0.0008), (sx * (hw - 0.054), (ytop + ybot) / 2.0 + 0.004, zf + 0.0002), TREAD_Y, 2, w=0.0, name="lip band")
        S.box(kit, (2 * hw - 0.21, 0.045, 0.0008), (0.0, ybot + 0.030, zf + 0.0002), TREAD_Y, 2, w=0.0, name="lip toe band")
        # weld beads where the lip's knuckles meet the plate
        S.tube(kit, "lip weld", [(-hw + 0.01, ytop + 0.002, zf + 0.001), (hw - 0.01, ytop + 0.002, zf + 0.001)], 0.0028, TREAD, lv, cap0=True, cap1=True, lo=8)
    # the lip keepers on the dock face: angles holding the toe
    for sx in (-1.0, 1.0):
        xk = sx * 0.70
        if lv < 2:
            angle_x(kit, "lip keeper", ybot - 0.026, 0.0, 0.050, 0.048, 0.0064, 1.0, 1.0, xk - 0.05, xk + 0.05, STEEL)
            S.box(kit, (0.100, 0.008, 0.050), (xk, ybot - 0.030, 0.025), STEEL, lv, w=0.001, name="keeper seat")
            if lv == 0:
                for dx in (-0.032, 0.032):
                    hex_bolt_z(kit, (xk + dx, ybot - 0.002, 0.0064), 0.017, 0.009, STEEL, 1, name="keeper bolt")
        else:
            S.box(kit, (0.10, 0.05, 0.05), (xk, ybot - 0.005, 0.025), STEEL, lv, w=0.0, name="lip keeper")


def leveler_meta(kit):
    kit.no_collider()
    anchor_u(kit, "lip_hinge", (0.0, LIP[0], LIP[1]))
    kit.meta["deckTop"] = LV_TOP
    kit.min_ceiling = 0.0


# ============================================================================= Kit_DockBumper
BUMPER = (0.254, 0.457, 0.114)        # 10 x 18 in, 4-1/2 in projection (LS60: 4-6 in)


def bumper(kit, lv):
    w, h, d = BUMPER
    t = 0.0095                         # 4 x 4 x 3/8 in angles top and bottom
    rnd = random.Random(31)
    n = 23
    pad_t = (h - 2 * t) / n
    if lv == 2:
        S.box(kit, (w, h, d), (0.0, h / 2.0, d / 2.0), RUBBER, lv, w=0.0, name="bumper")
        return
    for k in range(n):
        yc = t + pad_t * (k + 0.5)
        dw = rnd.uniform(-0.004, 0.004)
        dd = rnd.uniform(-0.004, 0.002)
        dx = rnd.uniform(-0.0015, 0.0015)
        rot = rnd.uniform(-1.0, 1.0) if lv == 0 else 0.0
        kit.box((w + dw, d + dd, pad_t - 0.0004), ub((dx, yc, (d + dd) / 2.0)), RUBBER, bevel=0.0025 if lv == 0 else 0.0, segments=1 if lv == 0 else 0,
                rot=(0.0, 0.0, -rot), name="pad")
    for (yy, sy) in ((0.0, 1.0), (h, -1.0)):
        angle_x(kit, "bumper angle", yy, 0.0, 0.102, 0.102, t, sy, 1.0, -w / 2.0 - 0.015, w / 2.0 + 0.015, STEEL)
        if lv == 0:
            # two anchor bolts through the wall leg, the rods' nuts on the horizontal leg
            for sx in (-1.0, 1.0):
                hex_bolt_z(kit, (sx * (w / 2.0 - 0.02), yy + sy * 0.065, t), 0.024, 0.012, STEEL, lv, name="anchor nut")
                hex_bolt_y(kit, (sx * 0.075, yy + (t if sy > 0 else -t), 0.066), 0.024, 0.014, STEEL, lv, name="rod nut", sign=-sy)
                S.cyl_y(kit, 0.0095, 0.02, (sx * 0.075, yy - sy * 0.018, 0.066), STEEL, 1, w=0.0, name="rod end", lo=8)


def bumper_meta(kit):
    w, h, d = BUMPER
    collider_u(kit, (0.0, h / 2.0, d / 2.0), (w, h, d))
    kit.min_ceiling = 0.0


# ============================================================================= Kit_DockEdgeGuard_*
ST_R = 0.0241                         # 1-1/2 in pipe (1.900 in OD)
ST_H = 1.067                          # 42 in
EYE_Y = 1.000


def stanchion(kit, lv):
    # the pipe, its cap, the welded base flange with four anchors, a weld ring, two chain eyes at 1.00 m
    kit.cylinder(ST_R, ST_H - 0.026, ub((0.0, 0.010 + (ST_H - 0.026) / 2.0, 0.0)), YELLOW, verts=B.segs(ST_R, lv, 16), bevel=0.0, segments=0, name="stanchion")
    cap = [(ST_R + 0.0015, 0.0), (ST_R + 0.0015, 0.010), (ST_R * 0.85, 0.016), (ST_R * 0.45, 0.019), (0.0, 0.020)]
    if lv == 2:
        cap = [(ST_R + 0.0015, 0.0), (0.0, 0.018)]
    S.lathe_y(kit, cap, (0.0, ST_H - 0.020, 0.0), YELLOW, lv, name="cap", lo=16)
    S.cyl_y(kit, 0.075, 0.010, (0.0, 0.005, 0.0), YELLOW, lv, w=0.0015, name="base flange", lo=24)
    if lv < 2:
        S.lathe_y(kit, [(ST_R + 0.007, 0.010), (ST_R + 0.0015, 0.017)], (0.0, 0.0, 0.0), YELLOW, lv, name="weld ring", close_top=False, close_bottom=False, lo=16)
        for k in range(4):
            a = 2 * math.pi * (k + 0.5) / 4
            hex_bolt_y(kit, (0.055 * math.cos(a), 0.010, 0.055 * math.sin(a)), 0.017, 0.009, STEEL, 1 if lv == 0 else 2, name="anchor nut")
        for sx in (-1.0, 1.0):
            eye = ring_path((sx * (ST_R + 0.012), EYE_Y, 0.0), 0.009, (1, 0, 0), (0, 1, 0), 0.0, 2 * math.pi, 12 if lv == 0 else 6)
            S.tube(kit, "chain eye", eye, 0.003, YELLOW, lv, cap0=False, cap1=False, lo=6)


def stanchion_meta(kit):
    collider_u(kit, (0.0, ST_H / 2.0, 0.0), (0.05, ST_H, 0.05))
    anchor_u(kit, "eye_left", (-(ST_R + 0.021), EYE_Y, 0.0))
    anchor_u(kit, "eye_right", (ST_R + 0.021, EYE_Y, 0.0))
    kit.min_ceiling = ST_H + 0.05


def _snap_hook(kit, x, y, sgn, lv):
    """A spring snap hook clipped into a stanchion's eye: its eye toward the chain, the hook and gate toward the post."""
    pts = [(x - sgn * 0.045, y, 0.0), (x - sgn * 0.020, y, 0.0), (x - sgn * 0.004, y + 0.006, 0.0), (x + sgn * 0.004, y + 0.014, 0.0),
           (x + sgn * 0.010, y + 0.010, 0.0), (x + sgn * 0.010, y - 0.004, 0.0)]
    S.tube(kit, "snap hook", pts, 0.0035, CHAIN, lv, cap0=True, cap1=True, lo=8)
    S.tube(kit, "hook gate", [(x - sgn * 0.022, y + 0.003, 0.0), (x + sgn * 0.009, y - 0.003, 0.0)], 0.0018, CHAIN, lv, cap0=True, cap1=True, lo=6)
    eye = ring_path((x - sgn * 0.052, y, 0.0), 0.008, (1, 0, 0), (0, 1, 0), 0.0, 2 * math.pi, 10 if lv == 0 else 6)
    S.tube(kit, "hook eye", eye, 0.0028, CHAIN, lv, cap0=False, cap1=False, lo=6)


def chain_span(kit, lv, span):
    """Eye to eye = span (x from -span/2 to +span/2 at y 0, the eyes' height), the chain sagging 0.12 m on its snap hooks."""
    hx = span / 2.0
    xa, xb = -hx + 0.004, hx - 0.004
    if lv < 2:
        _snap_hook(kit, xa, 0.0, -1.0, lv)
        _snap_hook(kit, xb, 0.0, 1.0, lv)
    pts = catenary_pts(xa + 0.062, xb - 0.062, -0.002, 0.0, 0.118, 24)
    chain_along(kit, "chain", pts, CHAIN, lv, wire=0.003, inner=(0.030, 0.012), q0=(6, 4), q1=(4, 3), up=(0, 0, 1), tube_lv=2)


def chain_meta(kit, span):
    kit.no_collider()
    anchor_u(kit, "end_left", (-span / 2.0, 0.0, 0.0))
    anchor_u(kit, "end_right", (span / 2.0, 0.0, 0.0))
    kit.meta["span"] = span
    kit.meta["sag"] = 0.12
    kit.min_ceiling = 0.0


GATE_W = 1.140


def gate(kit, lv):
    """A self-closing swing gate leaf of 1 in pipe between two stanchions 1.20 m apart: origin on the hinge stanchion's axis at the floor;
    the leaf closed along +x; two clamp hinges (the upper with its coil spring), a gravity latch and the keeper on the far stanchion."""
    r = 0.0167                         # 1 in pipe (1.315 in OD)
    x0, x1 = 0.055, 0.055 + GATE_W - 0.07
    yb, yt = 0.12, 1.045
    rb = 0.060
    # the frame: one bent pipe (stile, top rail with rounded corners, stile)
    n = 6 if lv == 0 else 3 if lv == 1 else 1
    pts = [(x0, yb, 0.0)]
    for k in range(n + 1):
        a = math.pi - (math.pi / 2) * k / n
        pts.append((x0 + rb + rb * math.cos(a), yt - rb + rb * math.sin(a), 0.0))
    for k in range(n + 1):
        a = math.pi / 2 - (math.pi / 2) * k / n
        pts.append((x1 - rb + rb * math.cos(a), yt - rb + rb * math.sin(a), 0.0))
    pts.append((x1, yb, 0.0))
    S.tube(kit, "gate frame", pts, r, YELLOW, lv, cap0=False, cap1=False, lo=12)
    for xe in (x0, x1):
        S.lathe_y(kit, [(r + 0.001, 0.0), (r + 0.001, 0.012), (r * 0.6, 0.016), (0.0, 0.017)], (xe, yb, 0.0), RUBBER, lv, name="stile cap", lo=12) if lv < 2 else None
    S.tube(kit, "gate mid rail", [(x0 + r * 0.9, 0.55, 0.0), (x1 - r * 0.9, 0.55, 0.0)], r * 0.85, YELLOW, lv, cap0=False, cap1=False, lo=12)
    if lv == 0:
        for xe in (x0 + r, x1 - r):
            S.lathe_axis(kit, [(r * 0.85 + 0.004, 0.0), (r * 0.85, 0.005)], (xe, 0.55, 0.0), "x" if xe < 0.5 else "-x", YELLOW, lv, name="rail weld", close_top=False, close_bottom=False)
    # hinges: two-piece collars clamped on the hinge stanchion, knuckles on the stile; the upper hinge carries its coil spring
    for yh in (0.30, 0.92):
        if lv < 2:
            S.cyl_y(kit, ST_R + 0.006, 0.050, (0.0, yh, 0.0), YELLOW, lv, w=0.001, name="hinge collar", lo=16)
            S.box(kit, (0.038, 0.050, 0.010), (0.032, yh, 0.0), YELLOW, lv, w=0.001, name="hinge strap")
            S.cyl_y(kit, 0.010, 0.060, (x0 - 0.012, yh, 0.0), YELLOW, lv, w=0.0008, name="hinge knuckle", lo=10)
            if lv == 0:
                hex_bolt_z(kit, (0.0, yh, ST_R + 0.006), 0.013, 0.008, STEEL, 1, name="collar bolt")
        else:
            S.box(kit, (0.07, 0.05, 0.06), (0.02, yh, 0.0), YELLOW, lv, w=0.0, name="hinge")
    if lv == 0:
        helix = [(x0 - 0.012 + 0.016 * math.cos(2 * math.pi * k / 10), 0.93 + 0.006 * k / 10 * 3 - 0.010, 0.016 * math.sin(2 * math.pi * k / 10)) for k in range(41)]
        S.tube(kit, "hinge spring", helix, 0.0022, STEEL, lv, cap0=True, cap1=True, lo=6)
    # gravity latch on the far stile, its keeper strap clamped on the far stanchion at x 1.20
    if lv < 2:
        S.box(kit, (0.110, 0.012, 0.010), (x1 + 0.040, 0.95, 0.0), YELLOW, lv, w=0.001, name="latch bar")
        S.cyl_y(kit, ST_R + 0.006, 0.040, (1.200, 0.95, 0.0), YELLOW, lv, w=0.001, name="keeper collar", lo=16)
        S.box(kit, (0.040, 0.030, 0.030), (1.200 - ST_R - 0.010, 0.955, 0.0), YELLOW, lv, w=0.001, name="keeper hook")


def gate_meta(kit):
    collider_u(kit, (0.60, 0.535, 0.0), (1.20, 1.07, 0.06))
    anchor_u(kit, "hinge_axis", (0.0, 0.0, 0.0))
    anchor_u(kit, "latch_post", (1.20, 0.0, 0.0))
    kit.min_ceiling = 1.10


# ============================================================================= Kit_Bollard
BOL_R = 0.0842                        # 6-5/8 in schedule 40 pipe
BOL_H = 1.067


def bollard(kit, lv):
    """Concrete-filled pipe set in the slab: the pipe (cylindrical metre UVs), a dented flat where a truck hit it, the concrete crowned
    over the rim (painted with it), the caulked joint at the slab."""
    nseg = 48 if lv == 0 else 24 if lv == 1 else 12
    rings = [-0.010, 0.0, 0.03, 0.20, 0.42, 0.50, 0.58, 0.80, 1.00, 1.030, 1.040] if lv == 0 else [-0.01, 0.0, 0.5, 1.04] if lv == 1 else [-0.01, 1.04]
    m = S.Mesh()
    P = []
    for y in rings:
        row = []
        for k in range(nseg):
            a = 2 * math.pi * k / nseg
            r = BOL_R
            if lv == 0:
                # the dent: a shallow flat 0.10 deep-ish on one side at 0.5 m
                da = math.atan2(math.sin(a - 0.9), math.cos(a - 0.9))
                r -= 0.006 * math.exp(-(da / 0.35) ** 2) * math.exp(-((y - 0.50) / 0.07) ** 2)
                if y >= 1.039:
                    r -= 0.0015
            row.append((r * math.cos(a), y, r * math.sin(a)))
        P.append(row)
    circ = 2 * math.pi * BOL_R
    for i in range(len(rings) - 1):
        for k in range(nseg):
            k1 = (k + 1) % nseg
            a, b, c, d = P[i][k], P[i][k1], P[i + 1][k1], P[i + 1][k]
            u0, u1 = circ * k / nseg, circ * (k + 1) / nseg
            out = (math.cos(2 * math.pi * (k + 0.5) / nseg), 0.0, math.sin(2 * math.pi * (k + 0.5) / nseg))
            qd(m, a, b, c, d, (u0, rings[i]), (u1, rings[i]), (u1, rings[i + 1]), (u0, rings[i + 1]), out)
    m.part(kit, "bollard pipe", YELLOW, merge=True)
    # the crown: concrete domed over the rim, painted
    crown = [(BOL_R - 0.0015, 1.040), (BOL_R - 0.004, 1.050), (BOL_R * 0.80, 1.062), (BOL_R * 0.55, 1.070), (BOL_R * 0.25, 1.074), (0.0, 1.075)]
    if lv == 2:
        crown = [(BOL_R, 1.040), (BOL_R * 0.5, 1.07), (0.0, 1.075)]
    S.lathe_y(kit, crown, (0.0, 0.0, 0.0), YELLOW, lv, name="bollard crown", close_top=False, close_bottom=False, lo=24)
    if lv < 2:
        S.lathe_y(kit, [(BOL_R + 0.013, 0.0004), (BOL_R + 0.001, 0.0016)], (0.0, 0.0, 0.0), DARK, lv, name="caulk ring", close_top=False, close_bottom=False, lo=24)


def bollard_meta(kit):
    collider_u(kit, (0.0, BOL_H / 2.0, 0.0), (0.17, BOL_H, 0.17))
    kit.min_ceiling = BOL_H + 0.05


# ============================================================================= Kit_DockLightPost
POST_R = 0.0572                       # 4 in pipe (4.5 in OD)
POST_H = 2.60
ARM_MOUNT_Y = 2.40


def light_post(kit, lv):
    S.cyl_y(kit, POST_R, POST_H - 0.016, (0.0, 0.016 + (POST_H - 0.016) / 2.0, 0.0), YELLOW, lv, w=0.001, name="post", lo=24)
    cap = [(POST_R + 0.002, 0.0), (POST_R + 0.002, 0.012), (POST_R * 0.8, 0.026), (POST_R * 0.4, 0.033), (0.0, 0.035)]
    if lv == 2:
        cap = [(POST_R, 0.0), (0.0, 0.03)]
    S.lathe_y(kit, cap, (0.0, POST_H - 0.012, 0.0), YELLOW, lv, name="post cap", lo=24)
    S.box(kit, (0.250, 0.016, 0.250), (0.0, 0.008, 0.0), YELLOW, lv, w=0.003, name="base plate")
    if lv < 2:
        for k in range(4):
            a = math.pi / 4 + math.pi / 2 * k
            gx, gz = math.cos(a), math.sin(a)
            m = S.Mesh()
            r0, r1 = POST_R, POST_R + 0.075
            tq = 0.005
            n = Vector((-gz, 0.0, gx))
            p = [(r0 * gx, 0.016, r0 * gz), (r1 * gx, 0.016, r1 * gz), (r0 * gx, 0.016 + 0.110, r0 * gz)]
            off = [Vector(q) for q in p]
            A = [tuple(v + n * tq) for v in off]
            Bq = [tuple(v - n * tq) for v in off]
            tri(m, A[0], A[1], A[2], (0, 0), (0.075, 0), (0, 0.11), tuple(n))
            tri(m, Bq[0], Bq[2], Bq[1], (0, 0), (0, 0.11), (0.075, 0), tuple(-n))
            qd(m, A[1], Bq[1], Bq[2], A[2], (0, 0), (0.01, 0), (0.01, 0.13), (0, 0.13), (gx, 0.6, gz))
            qd(m, A[0], A[1], Bq[1], Bq[0], (0, 0), (0.075, 0), (0.075, 0.01), (0, 0.01), (0, -1, 0))
            recalc(m.part(kit, "gusset", YELLOW))
        S.lathe_y(kit, [(POST_R + 0.008, 0.016), (POST_R + 0.001, 0.024)], (0.0, 0.0, 0.0), YELLOW, lv, name="post weld", close_top=False, close_bottom=False, lo=24)
        for (sx, sz) in ((1, 1), (1, -1), (-1, 1), (-1, -1)):
            S.cyl_y(kit, 0.0095, 0.030, (sx * 0.095, 0.031, sz * 0.095), STEEL, 2, w=0.0, name="anchor stud", lo=8)
            hex_bolt_y(kit, (sx * 0.095, 0.016, sz * 0.095), 0.028, 0.016, STEEL, 1 if lv == 0 else 2, name="anchor nut")


def light_post_meta(kit):
    collider_u(kit, (0.0, POST_H / 2.0, 0.0), (0.12, POST_H, 0.12))
    anchor_u(kit, "arm_mount", (0.0, ARM_MOUNT_Y, POST_R))
    kit.min_ceiling = POST_H + 0.05


# ============================================================================= Kit_WheelChock (+ _Hung)
CH = (0.20, 0.20, 0.25)               # width (x), height (y), length (z) of a moulded truck chock


def _chock_profile(lv):
    """(z, y) outline: the tall back at -z, the concave tyre face sweeping down to the toe at +z."""
    w, h, l = CH
    n = 7 if lv == 0 else 3 if lv == 1 else 1
    pts = [(-l / 2.0, 0.0), (l / 2.0, 0.0), (l / 2.0, 0.012)]
    # the tyre face: an arc of radius 0.50 (a truck tyre) from the toe up to the back's top
    R = 0.50
    cz, cy = l / 2.0 + 0.10, 0.012 + R * 0.92
    a0 = math.atan2(0.012 - cy, l / 2.0 - cz)
    a1 = math.atan2(h - 0.012 - cy, -l / 2.0 + 0.030 - cz)
    for k in range(1, n + 1):
        a = a0 + (a1 - a0) * k / n
        pts.append((cz + R * math.cos(a), cy + R * math.sin(a)))
    pts += [(-l / 2.0 + 0.012, h), (-l / 2.0, h - 0.012)]
    return pts


def _xf_extrude(kit, name, prof, x0, x1, slot, xf):
    """A closed (z, y) profile extruded along x, every point passed through xf (a rigid move in Unity space); capped, normals out."""
    m = S.Mesh()
    n = len(prof)
    arc = [0.0]
    for i in range(1, n + 1):
        a, b = prof[i - 1], prof[i % n]
        arc.append(arc[-1] + math.hypot(b[0] - a[0], b[1] - a[1]))
    for i in range(n):
        j = (i + 1) % n
        (za, ya), (zb, yb) = prof[i], prof[j]
        m.quad(xf((x0, ya, za)), xf((x1, ya, za)), xf((x1, yb, zb)), xf((x0, yb, zb)), (x0, arc[i]), (x1, arc[i]), (x1, arc[i + 1]), (x0, arc[i + 1]))
    m.face(tuple(m.vert(xf((x1, y, z))) for (z, y) in prof), [(z, y) for (z, y) in prof])
    m.face(tuple(reversed([m.vert(xf((x0, y, z))) for (z, y) in prof])), [(z, y) for (z, y) in reversed(prof)])
    return recalc(m.part(kit, name, slot))


def _xf_box(m, c, size, xf):
    hx, hy, hz = size[0] / 2.0, size[1] / 2.0, size[2] / 2.0
    corners = [(c[0] + sx * hx, c[1] + sy * hy, c[2] + sz * hz) for sx in (-1, 1) for sy in (-1, 1) for sz in (-1, 1)]
    P = [xf(q) for q in corners]
    faces = ((0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3))
    for f in faces:
        m.quad(P[f[0]], P[f[1]], P[f[2]], P[f[3]], (0, 0), (size[0], 0), (size[0], size[1]), (0, size[1]))


def chock_body(kit, lv, xf=lambda p: p):
    w, h, l = CH
    prof = _chock_profile(lv)
    _xf_extrude(kit, "chock", prof, -w / 2.0, w / 2.0, RUBBER, xf)
    if lv == 0:
        # moulded grip ribs across the tyre face
        m = S.Mesh()
        for (z, y) in prof[3:-2]:
            _xf_box(m, (0.0, y + 0.002, z + 0.002), (w - 0.02, 0.006, 0.006), xf)
        recalc(m.part(kit, "grip ribs", RUBBER))
    if lv < 2:
        # the forged eye on the back, on its boss
        eye = [xf(q) for q in ring_path((0.0, h * 0.62, -l / 2.0 - 0.020), 0.020, (1, 0, 0), (0, 1, 0), 0.0, 2 * math.pi, 12 if lv == 0 else 6)]
        S.tube(kit, "chock eye", eye, 0.0055, STEEL, lv, cap0=False, cap1=False, lo=8)
        m = S.Mesh()
        _xf_box(m, (0.0, h * 0.62 - 0.02, -l / 2.0 - 0.004), (0.030, 0.030, 0.010), xf)
        recalc(m.part(kit, "eye boss", STEEL))


def chock(kit, lv):
    """Kit_WheelChock: lying out on the bay floor, its chain trailing."""
    w, h, l = CH
    chock_body(kit, lv)
    pts = [(0.0, h * 0.62 - 0.035, -l / 2.0 - 0.020), (0.02, 0.012, -l / 2.0 - 0.07), (0.06, 0.006, -l / 2.0 - 0.20), (0.03, 0.006, -l / 2.0 - 0.36), (-0.04, 0.006, -l / 2.0 - 0.48)]
    chain_along(kit, "chock chain", pts, CHAIN, lv, wire=0.003, inner=(0.030, 0.012), q0=(8, 4), q1=(4, 3), up=(0, 1, 0), tube_lv=2)


HUNG_DY = -0.315


def _hung_xf(p):
    # turned up 90 deg about x: the back face (and its eye) up, the bottom face on the wall; hung under the hook
    return add(rot_x(p, 90.0), (0.0, HUNG_DY, 0.0))


def chock_hung(kit, lv):
    """Kit_WheelChock_Hung: on its wall hook (origin on the wall plane at the hook plate's centre, +Z out of the wall), the chain hanging
    to the chock's eye, the chock hanging eye up with its bottom against the wall."""
    w, h, l = CH
    S.box(kit, (0.060, 0.100, 0.008), (0.0, 0.0, 0.004), STEEL, lv, w=0.0012, name="hook plate")
    if lv < 2:
        S.tube(kit, "wall hook", [(0.0, -0.010, 0.008), (0.0, -0.010, 0.045), (0.0, 0.005, 0.060), (0.0, 0.022, 0.052)], 0.0055, STEEL, lv, cap0=False, cap1=True, lo=8)
        if lv == 0:
            m = S.Mesh()
            for y in (-0.035, 0.035):
                rivet(m, (0.0, y, 0.008), (0, 0, 1), 0.006, segs=8)
            m.part(kit, "hook screws", STEEL)
    chock_body(kit, lv, _hung_xf)
    eye = _hung_xf((0.0, h * 0.62, -l / 2.0 - 0.020 - 0.020))
    pts = [(0.0, 0.040, 0.050), (0.0, -0.04, 0.070), (eye[0], eye[1] + 0.012, eye[2])]
    chain_along(kit, "chock chain", pts, CHAIN, lv, wire=0.003, inner=(0.030, 0.012), q0=(8, 4), q1=(4, 3), up=(1, 0, 0), tube_lv=2)


# ============================================================================= wheels (shared by the platform truck and the pallet jack)
def wheel_x(kit, centre, r, width, lv, tyre_slot=RUBBER, hub_slot=STEEL, hub_r=None, segs=(32, 16, 8)):
    """A rubber tyre on a steel hub, its axle along x, lathed (the Run! B hand truck's section); segs per level round the axle."""
    hw = width / 2.0
    tyre = [(r * 0.60, -hw * 0.92), (r * 0.90, -hw), (r * 0.97, -hw * 0.86), (r, -hw * 0.45), (r, hw * 0.45), (r * 0.97, hw * 0.86), (r * 0.90, hw), (r * 0.60, hw * 0.92)]
    if lv == 1:
        tyre = [tyre[0], tyre[2], tyre[3], tyre[4], tyre[5], tyre[7]]
    elif lv == 2:
        tyre = [tyre[0], tyre[3], tyre[4], tyre[7]]
    nv = segs[min(lv, 2)]
    kit.lathe(tyre, ub(centre), tyre_slot, verts=nv, rot=(0, -90, 0), name="tyre", close_top=False, close_bottom=False)
    hr = hub_r if hub_r is not None else r * 0.60
    hub = [(hr, -hw * 0.92), (hr * 0.70, -hw * 0.95), (hr * 0.35, -hw * 1.05), (0.0, -hw * 1.05)]
    hub2 = [(0.0, hw * 1.05), (hr * 0.35, hw * 1.05), (hr * 0.70, hw * 0.95), (hr, hw * 0.92)]
    if lv < 2:
        kit.lathe(list(reversed(hub)), ub(centre), hub_slot, verts=max(8, nv // 2), rot=(0, -90, 0), name="hub", close_top=False, close_bottom=False)
        kit.lathe(hub2, ub(centre), hub_slot, verts=max(8, nv // 2), rot=(0, -90, 0), name="hub", close_top=False, close_bottom=False)
    else:
        S.cyl_x(kit, hr, width * 0.9, centre, hub_slot, lv, w=0.0, name="hub", lo=8)


# ============================================================================= Kit_PlatformTruck (+ _Pads, _Cartons)
PT = (0.762, 1.219)                   # 30 x 48 in deck
PT_TOP = 0.290
CASTER_R = 0.1016                     # 8 in


def _caster(kit, lv, x, z, swivel):
    top = PT_TOP - 0.032 - 0.0048 - 0.018 - 0.0005
    S.box(kit, (0.102, 0.008, 0.114), (x, top - 0.004, z), STEEL, lv, w=0.0012, name="caster plate")
    wz = z + (0.040 if swivel else 0.0)
    if swivel:
        S.cyl_y(kit, 0.055, 0.016, (x, top - 0.016, z), STEEL, max(lv, 1), name="raceway", lo=20)
    yb = top - (0.024 if swivel else 0.008)
    # the yoke: two pressed side plates down to the axle
    for sx in (-1.0, 1.0):
        m = S.Mesh()
        xp = x + sx * 0.034
        prof = [(z - 0.045, yb), (z + 0.045 + (0.040 if swivel else 0.0), yb), (wz + 0.025, CASTER_R + 0.012), (wz - 0.025, CASTER_R - 0.020)]
        _xf_extrude(kit, "yoke", [(zz, yy) for (zz, yy) in prof], xp - 0.0024, xp + 0.0024, STEEL, lambda p: p)
        if lv == 0:
            hex_bolt_x(kit, (x + sx * 0.0364, CASTER_R, wz), 0.017, 0.008, STEEL, 1, name="axle nut", sign=sx)
    wheel_x(kit, (x, CASTER_R, wz), CASTER_R, 0.050, lv)


def platform_truck(kit, lv, load=None):
    w, l = PT
    hw, hl = w / 2.0, l / 2.0
    rnd = random.Random(52)
    # the deck: five oak planks lengthwise with 6 mm gaps
    n = 5
    pw = (w - 0.006 * (n - 1)) / n
    for k in range(n):
        x = -hw + pw * (k + 0.5) + 0.006 * k
        o = kit.box((pw - 0.001, l - 0.002, 0.032), ub((x, PT_TOP - 0.016, 0.0)), "Prop_WoodOak", bevel=0.003 if lv < 2 else 0.0, segments=2 if lv == 0 else 1 if lv == 1 else 0, name="plank")
        o["fr_grain"] = "y"
        o["fr_uv_offset"] = (rnd.uniform(0, 3), rnd.uniform(0, 3))
    yt = PT_TOP - 0.032
    # the frame: 1-1/2 in angles round the deck (horizontal legs under it, vertical legs flush with its top), channels under the casters
    t = 0.0048
    if lv < 2:
        for sx in (-1.0, 1.0):
            m = S.Mesh()
            sec = angle_section(0.038, 0.038, t)
            pts = [(sx * (hw + t) - sx * a_, yt - t + b_) for (a_, b_) in sec]
            nn = len(pts)
            for i in range(nn):
                j = (i + 1) % nn
                (xa, ya), (xb, yb) = pts[i], pts[j]
                m.quad((xa, ya, -hl - t), (xb, yb, -hl - t), (xb, yb, hl + t), (xa, ya, hl + t), (0, -hl), (0.01, -hl), (0.01, hl), (0, hl))
            m.face(tuple(m.vert((x, y, hl + t)) for (x, y) in pts), [(x, y) for (x, y) in pts])
            m.face(tuple(reversed([m.vert((x, y, -hl - t)) for (x, y) in pts])), [(x, y) for (x, y) in reversed(pts)])
            recalc(m.part(kit, "frame angle", STEEL))
        for sz in (-1.0, 1.0):
            angle_x(kit, "end angle", yt - t, sz * (hl + t), 0.038, 0.038, t, 1.0, -sz, -hw, hw, STEEL)
        for z in (-0.47, 0.0, 0.47):
            S.box(kit, (w - 0.02, 0.018, 0.076), (0.0, yt - t - 0.009, z), STEEL, lv, w=0.0015, name="cross channel")
        if lv == 0:
            m = S.Mesh()
            for z in (-0.47, 0.0, 0.47):
                for k in range(n):
                    x = -hw + pw * (k + 0.5) + 0.006 * k
                    rivet(m, (x, PT_TOP, z), (0, 1, 0), 0.0085, segs=8)
            m.part(kit, "carriage bolts", STEEL)
    else:
        S.box(kit, (w, 0.05, l), (0.0, yt - 0.025, 0.0), STEEL, lv, w=0.0, name="frame")
    # casters: swivel at the handle end (+Z), rigid at the far end
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            _caster(kit, lv, sx * 0.290, sz * 0.470, sz > 0)
    # the U handle of 1 in pipe in sockets at the +Z corners
    r = 0.0167
    xh = hw - 0.045
    if lv < 2:
        for sx in (-1.0, 1.0):
            S.cyl_y(kit, r + 0.006, 0.110, (sx * xh, yt + 0.010, hl - 0.035), STEEL, lv, w=0.001, name="handle socket", lo=12)
        nb = 6 if lv == 0 else 3
        pts = [(-xh, yt - 0.03, hl - 0.035)]
        for k in range(nb + 1):
            a = math.pi - (math.pi / 2) * k / nb
            pts.append((-xh + 0.08 + 0.08 * math.cos(a), 0.92 + 0.08 * math.sin(a), hl - 0.035))
        for k in range(nb + 1):
            a = math.pi / 2 - (math.pi / 2) * k / nb
            pts.append((xh - 0.08 + 0.08 * math.cos(a), 0.92 + 0.08 * math.sin(a), hl - 0.035))
        pts.append((xh, yt - 0.03, hl - 0.035))
        S.tube(kit, "handle", pts, r, STEEL, lv, cap0=True, cap1=True, lo=12)
    else:
        S.box(kit, (2 * xh, 0.74, 0.03), (0.0, 0.63, hl - 0.035), STEEL, lv, w=0.0, name="handle")
    if load == "pads":
        for k, (dx, dz, sy) in enumerate(((0.0, -0.05, 0.0), (0.02, 0.03, 0.065), (-0.03, -0.02, 0.128))):
            if lv < 2:
                kit.soft_box((0.66, 0.50, 0.062), ub((dx, PT_TOP + 0.031 + sy, dz - 0.08)), "Prop_FabricNavy", radius=0.025,
                             segments=24 if lv == 0 else 10, rings=10 if lv == 0 else 5, sag=0.004, puff=0.010, rot=(0.0, 0.0, (k - 1) * 4.0), name="moving pad")
            else:
                S.box(kit, (0.66, 0.062, 0.50), (dx, PT_TOP + 0.031 + sy, dz - 0.08), "Prop_FabricNavy", lv, w=0.0, name="moving pad")
    elif load == "cartons":
        for (cx, cz, cy, cw, cd, ch) in ((-0.12, -0.20, 0.0, 0.45, 0.33, 0.30), (0.17, 0.25, 0.0, 0.40, 0.30, 0.25), (-0.10, -0.18, 0.30, 0.40, 0.30, 0.25)):
            yb = PT_TOP + cy
            S.box(kit, (cw, ch, cd), (cx, yb + ch / 2.0, cz), "RunB_Carton", lv, w=0.003, name="carton")
            if lv < 2:
                S.box(kit, (0.050, 0.0012, cd + 0.002), (cx, yb + ch + 0.0006, cz), "Prop_Cardboard", 2, w=0.0, name="tape")
                if lv == 0:
                    for sz in (-1.0, 1.0):
                        S.box(kit, (0.050, 0.060, 0.0012), (cx, yb + ch - 0.030, cz + sz * (cd / 2.0 + 0.0006)), "Prop_Cardboard", 2, w=0.0, name="tape end")


def platform_truck_meta(kit, load=None):
    w, l = PT
    h = 1.0
    collider_u(kit, (0.0, PT_TOP / 2.0 + 0.01, 0.0), (w, PT_TOP + 0.02, l))
    collider_u(kit, (0.0, 0.64, l / 2.0 - 0.035), (w - 0.06, 0.70, 0.05))
    if load is None:
        support_u(kit, "deck", (0.0, PT_TOP, -0.04), (w - 0.02, l - 0.12))
    kit.min_ceiling = 1.05


# ============================================================================= Kit_DumpsterFrontLoad
DU_W = 1.980
DU_PROF = [(-0.840, 0.130), (0.840, 0.130), (0.840, 1.000), (0.360, 1.620), (-0.840, 1.730)]   # (z, y) side profile of the body


def _du_profile(lv):
    if lv == 2:
        return DU_PROF
    # eased corners: a 25 mm round on every edge of the side outline
    out = []
    n = 3 if lv == 0 else 1
    P = DU_PROF
    for i in range(len(P)):
        a, b, c = Vector(P[i - 1]), Vector(P[i]), Vector(P[(i + 1) % len(P)])
        u1, u2 = (a - b).normalized(), (c - b).normalized()
        r = 0.025
        p0, p2 = b + u1 * r, b + u2 * r
        for k in range(n + 1):
            f = k / n
            q = p0 * (1 - f) ** 2 + b * 2 * f * (1 - f) + p2 * f * f
            out.append((q.x, q.y))
    return out


def dumpster(kit, lv):
    hw = DU_W / 2.0
    prof = _du_profile(lv)
    extrude_profile(kit, "body", prof, -hw, hw, GREEN)
    # bottom skids (channels front to back), the drain plug, the top rail round the opening
    for sx in (-1.0, 1.0):
        S.box(kit, (0.100, 0.130, 1.70), (sx * 0.62, 0.065, 0.0), STEEL, lv, w=0.003, name="skid")
    if lv < 2:
        rail = [(-hw - 0.01, 1.742, -0.846), (hw + 0.01, 1.742, -0.846)]
        S.tube(kit, "top rail", rail, 0.020, GREEN, lv, cap0=True, cap1=True, lo=10)
        for sx in (-1.0, 1.0):
            S.tube(kit, "top rail", [(sx * (hw + 0.012), 1.742, -0.846), (sx * (hw + 0.012), 1.632, 0.372)], 0.020, GREEN, lv, cap0=True, cap1=True, lo=10)
        S.tube(kit, "top rail", [(-hw - 0.01, 1.632, 0.372), (hw + 0.01, 1.632, 0.372)], 0.020, GREEN, lv, cap0=True, cap1=True, lo=10)
        S.cyl_x(kit, 0.030, 0.024, (hw + 0.012, 0.22, 0.70), STEEL, lv, name="drain plug")
        # the front's stiffening band and the corner posts
        S.box(kit, (DU_W + 0.012, 0.090, 0.012), (0.0, 0.62, 0.846), GREEN, lv, w=0.003, name="front band")
        for sx in (-1.0, 1.0):
            S.box(kit, (0.012, 0.090, 1.70), (sx * (hw + 0.006), 0.62, 0.0), GREEN, lv, w=0.003, name="side band")
    # the fork sleeves: 8 x 3 in tubes on each side, open at the front, on gussets
    for sx in (-1.0, 1.0):
        xs = sx * (hw + 0.045)
        if lv < 2:
            m = S.Mesh()
            out = [(xs - 0.040, 0.88), (xs + 0.040, 0.88), (xs + 0.040, 1.10), (xs - 0.040, 1.10)]
            inn = [(xs - 0.035, 0.885), (xs + 0.035, 0.885), (xs + 0.035, 1.095), (xs - 0.035, 1.095)]
            z0, z1 = -0.55, 0.86
            for ring, sgn in ((out, 1.0), (inn, -1.0)):
                for i in range(4):
                    j = (i + 1) % 4
                    (xa, ya), (xb, yb) = ring[i], ring[j]
                    nrm = Vector(((ya - yb), (xb - xa), 0.0)).normalized() * sgn
                    cen = Vector((xs, 0.99, 0.0))
                    mid = Vector(((xa + xb) / 2, (ya + yb) / 2, 0.0))
                    want = (mid - cen) * sgn
                    qd(m, (xa, ya, z0), (xb, yb, z0), (xb, yb, z1), (xa, ya, z1), (0, z0), (0.08, z0), (0.08, z1), (0, z1), tuple(want))
            for i in range(4):
                j = (i + 1) % 4
                qd(m, (out[i][0], out[i][1], z1), (out[j][0], out[j][1], z1), (inn[j][0], inn[j][1], z1), (inn[i][0], inn[i][1], z1), (0, 0), (0.08, 0), (0.08, 0.005), (0, 0.005), (0, 0, 1))
                qd(m, (out[i][0], out[i][1], z0), (out[j][0], out[j][1], z0), (inn[j][0], inn[j][1], z0), (inn[i][0], inn[i][1], z0), (0, 0), (0.08, 0), (0.08, 0.005), (0, 0.005), (0, 0, -1))
            m.part(kit, "fork sleeve", GREEN)
            S.box(kit, (0.070, 0.205, 0.005), (xs, 0.99, z1 - 0.10), "Prop_PlasticBlack", 2, w=0.0, name="sleeve shadow")
            for gz in (-0.40, 0.20, 0.70):
                m2 = S.Mesh()
                p0, p1, p2 = (sx * hw, 0.62, gz), (sx * hw, 0.88, gz), (xs + sx * 0.040, 0.88, gz)
                for dz, nz in ((-0.004, -1.0), (0.004, 1.0)):
                    a, b, c = (p0[0], p0[1], gz + dz), (p1[0], p1[1], gz + dz), (p2[0], p2[1], gz + dz)
                    tri(m2, a, b, c, (0, 0), (0, 0.26), (0.08, 0.26), (0, 0, nz))
                qd(m2, (p0[0], p0[1], gz - 0.004), (p2[0], p2[1], gz - 0.004), (p2[0], p2[1], gz + 0.004), (p0[0], p0[1], gz + 0.004), (0, 0), (0.3, 0), (0.3, 0.008), (0, 0.008), (sx, -1.0, 0))
                m2.part(kit, "sleeve gusset", GREEN)
        else:
            S.box(kit, (0.08, 0.22, 1.41), (xs, 0.99, 0.155), GREEN, lv, w=0.0, name="fork sleeve")
    # the lids: two black HDPE halves hinged at the back, ribbed, a turned-down front lip with a hand grip
    for sx in (-1.0, 1.0):
        x0, x1 = (0.004, hw - 0.004) if sx > 0 else (-hw + 0.004, -0.004)
        zb_, yb_ = -0.860, 1.770
        zf_, yf_ = 0.392, 1.655
        if lv < 2:
            prof = [(zb_, yb_ + 0.012), (zf_, yf_ + 0.012), (zf_ + 0.012, yf_ - 0.050), (zf_ - 0.006, yf_ - 0.050), (zf_ - 0.010, yf_ - 0.008), (zb_ + 0.02, yb_ - 0.010)]
            extrude_profile(kit, "lid", prof, x0, x1, "Prop_PlasticBlack")
            for k in range(3):
                xr = x0 + (x1 - x0) * (k + 1) / 4.0
                d = Vector((zf_ - zb_, yf_ - yb_))
                m = S.Mesh()
                for (dx, nx) in ((-0.012, -1.0), (0.012, 1.0)):
                    pass
                S.box(kit, (0.024, 0.016, 1.15), (xr, (yb_ + yf_) / 2.0 + 0.019, (zb_ + zf_) / 2.0 - 0.02), "Prop_PlasticBlack", 1, w=0.004, name="lid rib",
                      rot=(math.degrees(math.atan2(yb_ - yf_, zf_ - zb_)), 0.0, 0.0))
            S.box(kit, (0.30, 0.020, 0.030), ((x0 + x1) / 2.0, yf_ - 0.020, zf_ + 0.022), "Prop_PlasticBlack", lv, w=0.004, name="lid grip")
            for xh in (x0 + 0.12, (x0 + x1) / 2.0, x1 - 0.12):
                S.cyl_x(kit, 0.016, 0.070, (xh, yb_ - 0.004, zb_ - 0.012), STEEL, lv, name="lid hinge")
        else:
            S.box(kit, (x1 - x0, 0.03, 1.26), ((x0 + x1) / 2.0, (yb_ + yf_) / 2.0, (zb_ + zf_) / 2.0), "Prop_PlasticBlack", lv, w=0.0, name="lid")


def dumpster_meta(kit):
    collider_u(kit, (0.0, 0.89, 0.0), (DU_W + 0.17, 1.78, 1.70))
    kit.min_ceiling = 1.85


# ============================================================================= Kit_CCTVCamera
CAM_YAW, CAM_PITCH = 35.0, 25.0


def _cam_frame():
    head = Vector((0.0, 0.088, 0.268))
    yw, pt = math.radians(CAM_YAW), math.radians(CAM_PITCH)
    f = Vector((math.sin(yw) * math.cos(pt), -math.sin(pt), math.cos(yw) * math.cos(pt)))
    r = Vector((math.cos(yw), 0.0, -math.sin(yw)))
    u = r.cross(f).normalized() * -1.0
    if u.y < 0:
        u = -u
    return head, f, u, r


def cctv(kit, lv):
    """Wall plate, cast arm, the pan / tilt head, the extruded housing (front window, rear cap with glands), its sunshield, cables."""
    BEIGE, GLASS, BLACK = "Prop_PlasticBeige", "Prop_Glass", "Prop_PlasticBlack"
    S.box(kit, (0.075, 0.140, 0.006), (0.0, 0.0, 0.003), BEIGE, lv, w=0.0012, name="wall plate")
    if lv == 0:
        m = S.Mesh()
        for (x, y) in ((-0.024, -0.052), (0.024, -0.052), (-0.024, 0.052), (0.024, 0.052)):
            rivet(m, (x, y, 0.006), (0, 0, 1), 0.0045, segs=8)
        m.part(kit, "plate screws", STEEL)
    head, f, u, r = _cam_frame()
    S.tube(kit, "arm", [(0.0, 0.020, 0.006), (0.0, 0.050, 0.120), (0.0, 0.070, 0.230), (0.0, head.y - 0.020, head.z)], 0.016, BEIGE, lv, cap0=True, cap1=True, lo=12)
    S.cyl_x(kit, 0.022, 0.044, (0.0, head.y, head.z), BEIGE, lv, name="tilt knuckle")
    if lv < 2:
        hex_bolt_x(kit, (0.023, head.y, head.z), 0.016, 0.008, STEEL, 1, name="tilt bolt")
    # the housing along f: a rounded rectangle 110 x 100 mm, 380 mm long, sitting 62 mm over the head
    o = head + u * 0.062 - f * 0.12

    def xf(p):
        q = o + f * p[0] + u * p[1] + r * p[2]
        return (q.x, q.y, q.z)
    hwd, hh = 0.055, 0.050
    n = 3 if lv == 0 else 1 if lv == 1 else 0
    prof = []
    for (cz, cy, a0) in ((hwd - 0.010, hh - 0.010, 0.0), (-hwd + 0.010, hh - 0.010, 90.0), (-hwd + 0.010, -hh + 0.010, 180.0), (hwd - 0.010, -hh + 0.010, 270.0)):
        if n == 0:
            prof.append((cz + math.copysign(0.010, cz), cy + math.copysign(0.010, cy)))
            continue
        for k in range(n + 1):
            a = math.radians(a0 + 90.0 * k / n)
            prof.append((cz + 0.010 * math.cos(a), cy + 0.010 * math.sin(a)))
    _xf_extrude(kit, "housing", prof, 0.0, 0.380, BEIGE, xf)
    if lv < 2:
        big = [(zz * 1.06, yy * 1.06) for (zz, yy) in prof]
        _xf_extrude(kit, "front cap", big, 0.380, 0.402, BEIGE, xf)
        _xf_extrude(kit, "rear cap", big, -0.018, 0.0, BEIGE, xf)
        win = [(zz * 0.78, yy * 0.70) for (zz, yy) in prof]
        _xf_extrude(kit, "window", win, 0.402, 0.403, GLASS, xf)
        # the sunshield: a bent sheet on two standoffs, its front past the cap
        sh = [(-0.065, 0.066), (0.065, 0.066), (0.065, 0.046), (0.062, 0.046), (0.062, 0.063), (-0.062, 0.063), (-0.062, 0.046), (-0.065, 0.046)]
        _xf_extrude(kit, "sunshield", [(zz, yy) for (zz, yy) in sh], -0.030, 0.445, BEIGE, xf)
        for x in (0.08, 0.30):
            _xf_extrude(kit, "standoff", [(-0.008, 0.050), (0.008, 0.050), (0.008, 0.064), (-0.008, 0.064)], x - 0.008, x + 0.008, BLACK, xf)
        # cable glands on the rear cap, the coax and the power cord looping down to the wall box
        for gz in (-0.022, 0.022):
            g0 = xf((-0.018, -0.012, gz))
            g1 = xf((-0.040, -0.012, gz))
            S.tube(kit, "gland", [g0, g1], 0.008, BLACK, lv, cap0=True, cap1=True, lo=10)
        box = (0.0, -0.170, 0.026)
        S.box(kit, (0.100, 0.100, 0.052), box, BEIGE, lv, w=0.002, name="junction box")
        for gz, rr in ((-0.022, 0.0035), (0.022, 0.0042)):
            g1 = Vector(xf((-0.040, -0.012, gz)))
            pts = [tuple(g1), tuple(g1 - f * 0.05 + Vector((0.0, -0.06, 0.0))), (gz * 0.5, -0.05, 0.10), (gz * 0.5, -0.10, 0.058), (gz * 0.5, -0.120, 0.052)]
            S.tube(kit, "cable", pts, rr, BLACK, lv, cap0=True, cap1=True, lo=8)
    if lv == 2:
        pass


def cctv_meta(kit):
    kit.no_collider()
    head, f, u, r = _cam_frame()
    o = head + u * 0.062 - f * 0.12
    lens = o + f * 0.40
    anchor_u(kit, "lens", (lens.x, lens.y, lens.z))
    kit.meta["aimYawDeg"] = CAM_YAW
    kit.meta["aimPitchDeg"] = -CAM_PITCH
    kit.min_ceiling = 0.0


# ============================================================================= Kit_ReceivingDesk
RD = (0.762, 0.610)                   # 30 x 24 in
RD_FRONT_Y, RD_BACK_Y, RD_LEDGE = 1.000, 1.100, 1.120
RD_BOX_Y = 0.860


def receiving_desk(kit, lv):
    PUTTY, BLACK = "Prop_SteelPutty", "Prop_PlasticBlack"
    w, d = RD
    hw, hd = w / 2.0, d / 2.0
    zl = -hd + 0.100                       # where the lid meets the flat top ledge
    slope = math.atan2(RD_BACK_Y - RD_FRONT_Y, zl - hd)
    # the desk box: sides, front, back, bottom (one closed side profile extruded across)
    prof = [(hd, RD_BOX_Y), (hd, RD_FRONT_Y - 0.012), (zl, RD_BACK_Y - 0.012), (zl, RD_LEDGE), (-hd, RD_LEDGE), (-hd, RD_BOX_Y)]
    extrude_profile(kit, "desk box", prof, -hw, hw, PUTTY)
    # the lid: a 20 mm panel on the slope with a rolled front edge and the book ledge
    lid = [(hd + 0.006, RD_FRONT_Y - 0.010), (hd + 0.006, RD_FRONT_Y + 0.008), (zl - 0.004, RD_BACK_Y + 0.008), (zl - 0.004, RD_BACK_Y - 0.010)]
    extrude_profile(kit, "lid", lid, -hw - 0.004, hw + 0.004, PUTTY)
    if lv < 2:
        S.cyl_x(kit, 0.007, w + 0.004, (0.0, RD_FRONT_Y - 0.001, hd + 0.006), PUTTY, lv, name="lid edge roll", lo=10)
        S.box(kit, (w - 0.06, 0.022, 0.008), (0.0, RD_FRONT_Y + 0.019, hd - 0.012), PUTTY, lv, w=0.0015, name="book ledge")
        # the piano hinge at the lid's back edge, the pencil trough in the ledge
        S.cyl_x(kit, 0.006, w - 0.02, (0.0, RD_BACK_Y + 0.004, zl - 0.006), PUTTY, lv, name="piano hinge", lo=8)
        S.box(kit, (w - 0.10, 0.004, 0.030), (0.0, RD_LEDGE - 0.0015, -hd + 0.050), BLACK, 2, w=0.0, name="pencil trough")
        # the hasp and staple at the front
        S.box(kit, (0.030, 0.070, 0.004), (0.0, RD_FRONT_Y - 0.030, hd + 0.008), PUTTY, lv, w=0.0008, name="hasp")
        S.tube(kit, "staple", [(-0.008, RD_FRONT_Y - 0.065, hd + 0.003), (-0.008, RD_FRONT_Y - 0.065, hd + 0.016), (0.008, RD_FRONT_Y - 0.065, hd + 0.016), (0.008, RD_FRONT_Y - 0.065, hd + 0.003)],
               0.0025, PUTTY, lv, cap0=False, cap1=False, lo=6)
    # legs of 1-1/4 in square tube, black glides, the lower shelf with its front lip, side stretchers
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            x, z = sx * (hw - 0.024), sz * (hd - 0.024)
            S.box(kit, (0.032, RD_BOX_Y - 0.012, 0.032), (x, 0.012 + (RD_BOX_Y - 0.012) / 2.0, z), PUTTY, lv, w=0.0025, name="leg")
            S.cyl_y(kit, 0.016, 0.012, (x, 0.006, z), BLACK, max(lv, 1), name="glide", lo=12)
    S.box(kit, (w - 0.040, 0.012, d - 0.050), (0.0, 0.206, 0.0), PUTTY, lv, w=0.002, name="lower shelf")
    if lv < 2:
        S.box(kit, (w - 0.040, 0.030, 0.012), (0.0, 0.225, hd - 0.030), PUTTY, lv, w=0.002, name="shelf lip")
        for sx in (-1.0, 1.0):
            S.box(kit, (0.020, 0.020, d - 0.064), (sx * (hw - 0.024), 0.55, 0.0), PUTTY, lv, w=0.002, name="stretcher")


def receiving_desk_meta(kit):
    w, d = RD
    hd = d / 2.0
    collider_u(kit, (0.0, RD_LEDGE / 2.0, 0.0), (w, RD_LEDGE, d))
    support_u(kit, "shelf", (0.0, 0.212, 0.0), (w - 0.06, d - 0.08))
    support_u(kit, "ledge", (0.0, RD_LEDGE, -hd + 0.050), (w - 0.06, 0.08))
    zl = -hd + 0.100
    anchor_u(kit, "lid_centre", (0.0, (RD_FRONT_Y + RD_BACK_Y) / 2.0 + 0.009, (hd + zl) / 2.0))
    kit.meta["lidSlopeDeg"] = round(math.degrees(math.atan2(RD_BACK_Y - RD_FRONT_Y, hd - zl)), 2)
    kit.min_ceiling = 1.2


# ============================================================================= Kit_PalletLoad_Wrapped (+ _High)
PAL = (1.219, 1.016)                  # GMA 48 x 40 in: stringers along x
PAL_TOP = 0.127                       # 5/8 in boards on 3-3/4 in stringers
CART_X = 1.200
CART_Z = 0.900
FILM_TEX = (1024, 512)
FILM_PXM = 239.6                      # atlas px per metre along the perimeter
FILM_ROWS = {"low": (322, 190), "high": (0, 312)}     # (y0, height) of each variant's row in the atlas (px, from the bottom)
FILM_TAIL_X = (1017, 1023)            # a plain-film strip for the torn tail
SHELL_PAD = 0.006                     # the film stands this far off the carton faces
SHELL_R = 0.024                       # corner radius of the wrapped stack
FILM_Y0 = 0.108                       # the film's bottom wraps the pallet's top boards
OVERWRAP = 0.050


def load_layout(variant):
    """Carton boundaries per layer (x and z cuts with a little jitter, the same numbers the film atlas is painted from)."""
    layers = 3 if variant == "low" else 5
    lh = 0.250 if variant == "low" else 0.274
    rnd = random.Random(71 if variant == "low" else 72)
    out = []
    for k in range(layers):
        xs = [-CART_X / 2.0, -0.20 + rnd.uniform(-0.015, 0.015), 0.20 + rnd.uniform(-0.015, 0.015), CART_X / 2.0]
        zs = [-CART_Z / 2.0, -0.15 + rnd.uniform(-0.012, 0.012), 0.15 + rnd.uniform(-0.012, 0.012), CART_Z / 2.0]
        out.append(dict(y0=PAL_TOP + lh * k, y1=PAL_TOP + lh * (k + 1), xs=xs, zs=zs))
    return out


def _faces():
    """The four sides in perimeter order: (name, start corner (x, z), direction (dx, dz), length, outward normal)."""
    hx, hz = CART_X / 2.0 + SHELL_PAD, CART_Z / 2.0 + SHELL_PAD
    return [("front", (-hx, hz), (1.0, 0.0), 2 * hx, (0.0, 1.0)), ("right", (hx, hz), (0.0, -1.0), 2 * hz, (1.0, 0.0)),
            ("back", (hx, -hz), (-1.0, 0.0), 2 * hx, (0.0, -1.0)), ("left", (-hx, -hz), (0.0, 1.0), 2 * hz, (-1.0, 0.0))]


def film_regions():
    """Atlas rects (px) of each variant's four faces: x0, width, y0, height."""
    out = {}
    for variant, (y0, h) in FILM_ROWS.items():
        x = 0.0
        for (name, c, d, L, n) in _faces():
            w = L * FILM_PXM
            out[variant + "_" + name] = (x, w, y0, h)
            x += w
    return out


def film_span(y_top):
    """The film's unfolded height from its bottom (FILM_Y0) over the top edge to the over-wrap's inner edge: v = height / span."""
    return (y_top - 0.012 - FILM_Y0) + 0.016 + OVERWRAP


def _film_uv(variant, face, s, v):
    x0, w, y0, h = film_regions()[variant + "_" + face]
    return ((x0 + 0.5 + s * (w - 1.0)) / FILM_TEX[0], (y0 + 0.5 + v * (h - 1.0)) / FILM_TEX[1])


def _pallet(kit, lv):
    """A GMA 48 x 40 stringer pallet: three notched stringers, seven top boards, five bottom boards."""
    lx, lz = PAL
    rnd = random.Random(9)
    sh = 0.095
    tb = 0.016
    for zc in (-lz / 2.0 + 0.019, 0.0, lz / 2.0 - 0.019):
        if lv < 2:
            prof = [(-lx / 2.0, tb), (-0.445, tb), (-0.445, tb + 0.038), (-0.215, tb + 0.038), (-0.215, tb), (0.215, tb), (0.215, tb + 0.038),
                    (0.445, tb + 0.038), (0.445, tb), (lx / 2.0, tb), (lx / 2.0, tb + sh), (-lx / 2.0, tb + sh)]
            _xf_extrude(kit, "stringer", prof, -0.019, 0.019, "Prop_PinePallet", lambda p, zc=zc: (p[2], p[1], -p[0] + zc))
        else:
            S.box(kit, (lx, sh, 0.038), (0.0, tb + sh / 2.0, zc), "Prop_PinePallet", lv, w=0.0, name="stringer")
    # top boards along z (ends 5-1/2 in, the rest 3-1/2 in), bottom boards
    tops = [(-lx / 2.0 + 0.070, 0.140), (-0.36, 0.089), (-0.18, 0.089), (0.0, 0.140), (0.18, 0.089), (0.36, 0.089), (lx / 2.0 - 0.070, 0.140)]
    for (x, bw) in tops:
        o = kit.box((bw - 0.002, lz, tb), ub((x + rnd.uniform(-0.004, 0.004), PAL_TOP - tb / 2.0, rnd.uniform(-0.003, 0.003))), "Prop_PinePallet",
                    bevel=0.002 if lv == 0 else 0.0, segments=1 if lv == 0 else 0, name="top board")
        o["fr_grain"] = "y"
        o["fr_uv_offset"] = (rnd.uniform(0, 2), rnd.uniform(0, 2))
    if lv < 2:
        for (x, bw) in [(-lx / 2.0 + 0.070, 0.140), (-0.30, 0.089), (0.0, 0.140), (0.30, 0.089), (lx / 2.0 - 0.070, 0.140)]:
            o = kit.box((bw - 0.002, lz, tb), ub((x, tb / 2.0, 0.0)), "Prop_PinePallet", bevel=0.0, segments=0, name="bottom board")
            o["fr_grain"] = "y"
            o["fr_uv_offset"] = (rnd.uniform(0, 2), rnd.uniform(0, 2))


def _shell(kit, lv, variant, layout):
    """The stretch film round the stack: a rounded box whose faces dip a few mm at the carton joints and bulge a little mid-carton,
    the film over-wrapping the top edge by 50 mm; UVs into the variant's row of the film atlas (the cartons are painted through it)."""
    y_top = layout[-1]["y1"]
    yb = FILM_Y0
    rnd = random.Random(5 if variant == "low" else 6)
    m = S.Mesh()
    hx, hz = CART_X / 2.0 + SHELL_PAD, CART_Z / 2.0 + SHELL_PAD
    # rows: bottom, every layer joint and mid-layer, the top edge, the over-wrap's inner edge
    rows = [yb]
    for L in layout:
        if lv == 0:
            rows += [L["y0"] + 0.004, (L["y0"] + L["y1"]) / 2.0]
        elif lv == 1:
            rows += [L["y0"] + 0.004]
    rows += [y_top - 0.012, y_top + 0.004]
    rows = sorted(set(round(r, 5) for r in rows if r >= yb))
    ncorner = 3 if lv == 0 else 1
    for fi, (name, c, d, L, n) in enumerate(_faces()):
        # stations along the face: corner halves, joints, mids
        cuts = [0.0, L]
        layer = layout[0]
        js = layer["xs"] if abs(d[0]) > 0.5 else layer["zs"]
        if lv < 2:
            for j in js[1:-1]:
                s_ = (j - (c[0] if abs(d[0]) > 0.5 else c[1])) * (d[0] if abs(d[0]) > 0.5 else d[1])
                cuts.append(s_)
                if lv == 0:
                    cuts += [s_ - 0.10, s_ + 0.10]
        s_lo = SHELL_R * (1.0 - math.sin(math.pi / 4))       # each face takes half of each corner's arc (to the 45 deg point)
        cuts = [s_lo, L - s_lo] + [x for x in cuts if s_lo < x < L - s_lo]
        for k in range(0, ncorner):
            sk = SHELL_R * (1.0 - math.sin(math.pi / 4 * k / ncorner))
            cuts += [sk, L - sk]
        cuts = sorted(set(round(x, 5) for x in cuts if s_lo - 1e-6 <= x <= L - s_lo + 1e-6))

        def pos(s_, y):
            # the point on this face at station s_, height y (with the corner rounding and the joint dips)
            x = c[0] + d[0] * s_
            z = c[1] + d[1] * s_
            inset = 0.0
            e = min(s_, L - s_)
            if e < SHELL_R:
                inset += SHELL_R - math.sqrt(max(0.0, SHELL_R ** 2 - (SHELL_R - e) ** 2))
            for j in js[1:-1]:
                sj = (j - (c[0] if abs(d[0]) > 0.5 else c[1])) * (d[0] if abs(d[0]) > 0.5 else d[1])
                inset += 0.0025 * math.exp(-((s_ - sj) / 0.03) ** 2)
            for Lr in layout[1:]:
                inset += 0.003 * math.exp(-((y - Lr["y0"]) / 0.025) ** 2)
            inset -= 0.002 * (1.0 if lv == 0 else 0.0) * math.sin(math.pi * min(1.0, max(0.0, (y - yb) / max(y_top - yb, 1e-3)))) * 0.5
            if y > y_top - 0.013:
                # over the top edge: the film turns in over the cartons' tops
                t = min(1.0, (y - (y_top - 0.012)) / 0.016)
                inset += OVERWRAP * t
                yy = y_top - 0.012 + 0.016 * math.sin(t * math.pi / 2) if t < 1.0 else y_top + 0.004
                return (x - n[0] * inset, yy, z - n[1] * inset)
            if y <= yb + 0.001:
                inset += 0.010
            return (x - n[0] * inset, y, z - n[1] * inset)
        span = film_span(y_top)
        for i in range(len(rows) - 1):
            for j in range(len(cuts) - 1):
                s0, s1 = cuts[j], cuts[j + 1]
                y0, y1 = rows[i], rows[i + 1]
                a, b, cc, dd = pos(s0, y0), pos(s1, y0), pos(s1, y1), pos(s0, y1)
                v0 = min(1.0, (y0 - yb) / span)
                v1 = (y1 - yb) / span if y1 <= y_top - 0.012 + 1e-6 else 1.0
                u0, u1 = s0 / L, s1 / L
                qd(m, a, b, cc, dd, _film_uv(variant, name, u0, v0), _film_uv(variant, name, u1, v0), _film_uv(variant, name, u1, v1), _film_uv(variant, name, u0, v1),
                   (n[0], 0.25 if y0 > y_top - 0.013 else 0.0, n[1]))
    m.part(kit, "film", FILM, merge=False)
    # the film rope: the bottom wraps twisted round the pallet's top boards
    if lv < 2:
        nseg = 48 if lv == 0 else 16
        per = []
        hx2, hz2 = hx + 0.004, hz + 0.004
        for k in range(nseg + 1):
            f = k / nseg
            # walk the rounded rectangle
            L = 2 * (2 * hx2 + 2 * hz2)
            s_ = f * L
            if s_ < 2 * hx2:
                x, z = -hx2 + s_, hz2
            elif s_ < 2 * hx2 + 2 * hz2:
                x, z = hx2, hz2 - (s_ - 2 * hx2)
            elif s_ < 4 * hx2 + 2 * hz2:
                x, z = hx2 - (s_ - 2 * hx2 - 2 * hz2), -hz2
            else:
                x, z = -hx2, -hz2 + (s_ - 4 * hx2 - 2 * hz2)
            per.append((x, FILM_Y0 + 0.010 + 0.004 * math.sin(f * 37.0), z))
        u_tail = (FILM_TAIL_X[0] + FILM_TAIL_X[1]) / 2.0 / FILM_TEX[0]
        mm = S.Mesh()
        S.sweep_section(mm, per, [(0.008 * math.cos(2 * math.pi * q / 6), 0.004 * math.sin(2 * math.pi * q / 6)) for q in range(6)], closed_section=True, uv_scale=0.0)
        for i in range(len(mm.uv)):
            mm.uv[i] = (u_tail, 0.2 + 0.6 * ((i * 7) % 11) / 11.0)
        mm.part(kit, "film rope", FILM)
    # the torn tail hanging from the right-back corner
    if lv < 2:
        mt = S.Mesh()
        u0 = (FILM_TAIL_X[0] + 0.5) / FILM_TEX[0]
        u1 = (FILM_TAIL_X[1] - 0.5) / FILM_TEX[0]
        y0 = (layout[0]["y1"] + layout[0]["y0"]) / 2.0 + 0.05
        pts = [(hx + 0.004, y0, -hz + 0.05 + 0.03 * k) for k in range(1)]
        segs = 6 if lv == 0 else 2
        prev = None
        for k in range(segs + 1):
            f = k / segs
            y = y0 - 0.30 * f
            w0 = 0.09 * (1 - 0.7 * f)
            tw = 0.6 * f
            cx, cz = hx + 0.006 + 0.02 * f, -hz + 0.06 + 0.04 * math.sin(f * 2.0)
            a = (cx + 0.004 * math.sin(tw), y, cz - w0 / 2.0 * math.cos(tw))
            b = (cx - 0.004 * math.sin(tw), y, cz + w0 / 2.0 * math.cos(tw))
            if prev is not None:
                qd(mt, prev[0], prev[1], b, a, (u0, 1.0 - (f - 1.0 / segs)), (u1, 1.0 - (f - 1.0 / segs)), (u1, 1.0 - f), (u0, 1.0 - f), (1.0, 0.0, 0.0))
                qd(mt, prev[1], prev[0], a, b, (u1, 1.0 - (f - 1.0 / segs)), (u0, 1.0 - (f - 1.0 / segs)), (u0, 1.0 - f), (u1, 1.0 - f), (-1.0, 0.0, 0.0))
            prev = (a, b)
        mt.part(kit, "film tail", FILM, merge=False)


def _top_cartons(kit, lv, layout):
    """The top layer's tops (unwrapped): each carton's lid with its flap seam and the tape along it."""
    L = layout[-1]
    yt = L["y1"]
    xs, zs = L["xs"], L["zs"]
    for i in range(3):
        for j in range(3):
            x0, x1 = xs[i] + 0.002, xs[i + 1] - 0.002
            z0, z1 = zs[j] + 0.002, zs[j + 1] - 0.002
            if lv < 2:
                m = S.Mesh()
                zm = (z0 + z1) / 2.0
                # two flaps meeting at the seam along x, the seam 2 mm low
                qd(m, (x0, yt, z0), (x1, yt, z0), (x1, yt - 0.002, zm), (x0, yt - 0.002, zm), (x0, z0), (x1, z0), (x1, zm), (x0, zm), (0, 1, 0))
                qd(m, (x0, yt - 0.002, zm), (x1, yt - 0.002, zm), (x1, yt, z1), (x0, yt, z1), (x0, zm), (x1, zm), (x1, z1), (x0, z1), (0, 1, 0))
                m.part(kit, "carton top", "Prop_Cardboard", merge=False)
                S.box(kit, (x1 - x0 + 0.004, 0.0012, 0.048), ((x0 + x1) / 2.0, yt - 0.0004, zm), "Prop_TapeBeige", 2, w=0.0, name="tape")
            else:
                S.box(kit, (x1 - x0, 0.002, z1 - z0), ((x0 + x1) / 2.0, yt - 0.001, (z0 + z1) / 2.0), "Prop_Cardboard", 2, w=0.0, name="carton top")


def pallet_load(kit, lv, variant):
    layout = load_layout(variant)
    _pallet(kit, lv)
    if lv < 2:
        _shell(kit, lv, variant, layout)
    else:
        y_top = layout[-1]["y1"]
        hx, hz = CART_X / 2.0 + SHELL_PAD, CART_Z / 2.0 + SHELL_PAD
        m = S.Mesh()
        for (name, c, d, L, n) in _faces():
            a = (c[0], FILM_Y0, c[1])
            b = (c[0] + d[0] * L, FILM_Y0, c[1] + d[1] * L)
            qd(m, a, b, (b[0], y_top, b[2]), (a[0], y_top, a[2]), _film_uv(variant, name, 0, 0), _film_uv(variant, name, 1, 0), _film_uv(variant, name, 1, 0.95), _film_uv(variant, name, 0, 0.95), (n[0], 0.0, n[1]))
        m.part(kit, "film", FILM, merge=False)
    _top_cartons(kit, lv, layout)


def pallet_load_meta(kit, variant):
    layout = load_layout(variant)
    y_top = layout[-1]["y1"]
    collider_u(kit, (0.0, y_top / 2.0, 0.0), (PAL[0], y_top, PAL[1]))
    support_u(kit, "top", (0.0, y_top, 0.0), (CART_X - 0.10, CART_Z - 0.10))
    kit.meta["filmLayout"] = dict(variant=variant, layers=[[round(L["y0"], 4), round(L["y1"], 4), [round(v, 4) for v in L["xs"]], [round(v, 4) for v in L["zs"]]] for L in layout])
    kit.min_ceiling = y_top + 0.05


# ============================================================================= Kit_PalletJack
PJ_W = 0.686                          # 27 in over the forks
PJ_FORK = (0.160, 0.085, 1.220)       # fork width, lowered height, length (48 in)


def pallet_jack(kit, lv):
    CHROME = "Prop_Chrome"
    fw, fh, fl = PJ_FORK
    tip = -0.950
    root = tip + fl
    for sx in (-1.0, 1.0):
        xc = sx * (PJ_W / 2.0 - fw / 2.0)
        # the fork: an inverted channel, its tip tapered down for entry
        if lv < 2:
            prof = [(root, fh), (tip + 0.140, fh), (tip + 0.010, 0.052), (tip, 0.046), (tip, 0.030)]
            top = [(root, fh), (tip + 0.140, fh), (tip + 0.012, 0.050), (tip, 0.044)]
            # top plate
            m = S.Mesh()
            for i in range(len(top) - 1):
                (za, ya), (zb, yb) = top[i], top[i + 1]
                qd(m, (xc - fw / 2.0, ya, za), (xc + fw / 2.0, ya, za), (xc + fw / 2.0, yb, zb), (xc - fw / 2.0, yb, zb), (xc - fw / 2.0, za), (xc + fw / 2.0, za), (xc + fw / 2.0, zb), (xc - fw / 2.0, zb), (0, 1, 0))
                qd(m, (xc - fw / 2.0 + 0.004, ya - 0.004, za), (xc + fw / 2.0 - 0.004, ya - 0.004, za), (xc + fw / 2.0 - 0.004, yb - 0.004, zb), (xc - fw / 2.0 + 0.004, yb - 0.004, zb), (0, 0), (0.15, 0), (0.15, 1), (0, 1), (0, -1, 0))
            m.part(kit, "fork top", RED)
            for ss in (-1.0, 1.0):
                xs = xc + ss * (fw / 2.0 - 0.002)
                side = [(root, fh), (tip + 0.140, fh), (tip + 0.012, 0.050), (tip, 0.044), (tip, 0.022), (tip + 0.20, 0.012), (root, 0.012)]
                _xf_extrude(kit, "fork side", side, xs - 0.002, xs + 0.002, RED, lambda p: p)
            # tandem load rollers in the fork's tip, and the entry roller under it
            for zr in (tip + 0.24, tip + 0.34):
                S.cyl_x(kit, 0.040, fw - 0.020, (xc, 0.040, zr), STEEL, max(lv, 1), name="load roller", lo=12)
            S.cyl_x(kit, 0.016, fw - 0.020, (xc, 0.020, tip + 0.050), STEEL, max(lv, 1), name="entry roller", lo=8)
        else:
            S.box(kit, (fw, fh - 0.01, fl), (xc, (fh + 0.01) / 2.0, (tip + root) / 2.0), RED, lv, w=0.0, name="fork")
    # the chassis joining the forks, the pump unit with its chrome ram, the steer wheels under it
    zc = root + 0.060
    S.box(kit, (PJ_W, 0.090, 0.130), (0.0, 0.065 + 0.040, zc), RED, lv, w=0.006 if lv < 2 else 0.0, name="chassis")
    zp = root + 0.200
    S.cyl_y(kit, 0.062, 0.230, (0.0, 0.200 + 0.115, zp), RED, lv, w=0.004, name="pump body", lo=24)
    if lv < 2:
        S.cyl_y(kit, 0.075, 0.025, (0.0, 0.200, zp), RED, lv, w=0.003, name="pump base", lo=24)
        S.cyl_y(kit, 0.019, 0.060, (0.0, 0.445 + 0.030, zp), CHROME, lv, w=0.001, name="ram", lo=16)
        S.box(kit, (0.110, 0.020, 0.090), (0.0, 0.520, zp), RED, lv, w=0.003, name="yoke head")
        # the release valve lever on the pump, the return spring
        S.box(kit, (0.012, 0.012, 0.060), (0.055, 0.40, zp + 0.050), STEEL, lv, w=0.001, name="valve lever")
        helix = [(-0.052 + 0.010 * math.cos(2 * math.pi * k / 8), 0.24 + 0.006 * k, zp - 0.050 + 0.010 * math.sin(2 * math.pi * k / 8)) for k in range(25 if lv == 0 else 9)]
        S.tube(kit, "return spring", helix, 0.0025, STEEL, lv, cap0=True, cap1=True, lo=6)
        # the push rods' crossbar under the chassis
        S.box(kit, (PJ_W - 0.20, 0.020, 0.020), (0.0, 0.030, root - 0.040), STEEL, lv, w=0.002, name="push crossbar")
    for sx in (-1.0, 1.0):
        wheel_x(kit, (sx * 0.042, 0.090, zp), 0.090, 0.050, lv, tyre_slot=RUBBER, hub_slot=STEEL, hub_r=0.055, segs=(28, 14, 8))
    if lv < 2:
        S.box(kit, (0.150, 0.030, 0.090), (0.0, 0.195, zp), STEEL, lv, w=0.003, name="steer plate")
    # the handle: two tubes up from the yoke, leaning back, closed in a loop grip with its lever and rubber grip
    r = 0.016
    base = Vector((0.0, 0.525, zp))
    lean = math.radians(12.0)
    up = Vector((0.0, math.cos(lean), math.sin(lean)))
    if lv < 2:
        for sx in (-1.0, 1.0):
            p0 = base + Vector((sx * 0.045, 0.0, 0.0))
            p1 = base + up * 0.55 + Vector((sx * 0.090, 0.0, 0.0))
            S.tube(kit, "handle tube", [tuple(p0), tuple(p1)], r, RED, lv, cap0=True, cap1=False, lo=12)
        n = 8 if lv == 0 else 4
        loop = []
        cen = base + up * 0.62
        for k in range(n * 2 + 1):
            a = 2 * math.pi * k / (n * 2)
            loop.append(tuple(cen + Vector((0.150 * math.cos(a), 0.0, 0.0)) + up * (0.075 * math.sin(a))))
        S.tube(kit, "handle loop", loop, r, RED, lv, cap0=False, cap1=False, lo=12)
        g0 = cen + up * 0.075 + Vector((-0.09, 0.0, 0.0))
        g1 = cen + up * 0.075 + Vector((0.09, 0.0, 0.0))
        S.tube(kit, "grip", [tuple(g0), tuple(g1)], r + 0.006, RUBBER, lv, cap0=True, cap1=True, lo=12)
        lv0 = cen + up * 0.040 + Vector((0.05, 0.0, 0.015))
        S.tube(kit, "control lever", [tuple(lv0), tuple(lv0 + Vector((0.07, -0.01, 0.025)))], 0.006, STEEL, lv, cap0=True, cap1=True, lo=8)
    else:
        S.box(kit, (0.30, 0.70, 0.04), (0.0, 0.525 + 0.35 * math.cos(lean), zp + 0.35 * math.sin(lean)), RED, lv, w=0.0, name="handle")


def pallet_jack_meta(kit):
    fw, fh, fl = PJ_FORK
    tip = -0.950
    collider_u(kit, (0.0, fh / 2.0, tip + fl / 2.0), (PJ_W, fh, fl))
    collider_u(kit, (0.0, 0.60, tip + fl + 0.26), (0.34, 1.20, 0.30))
    kit.min_ceiling = 1.30
