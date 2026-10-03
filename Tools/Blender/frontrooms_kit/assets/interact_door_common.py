"""Shared helpers for the G1 door-construction modules of the interactables
kit (research/interactables/10_spec.md §1, §2.1, §2.3-2.4, §9.1, §10.1).

NOT AN ASSET: no NAME and no build(). The asset modules import it
(``import interact_door_common as dc``); build_asset.py puts assets/ on
sys.path, so the import works headless.

Frames (spec §1.2)
* DOOR ROOT D, Unity metres. Origin = the opening's hinge-jamb edge, on the
  wall centre line, at floor level. +Z runs along the opening from the hinge
  jamb (0) to the latch jamb (1.0); +Y is up; +X is the swing / pull face S;
  -X is the push face P, where the stops are. Blender (x, y, z) =
  U(X, Y, Z) = (-X, -Z, Y). Every number below is a Unity metre.
* PART frame (closer parts): origin at the part's pivot or mount point;
  front = Unity +Z (kit -Y) = the outward normal of the face it mounts on.

What lives here
* U(), the section constants of §1.4 (lining, stop, casing envelope, saddle,
  leaf envelope, hinge axis A2, hinge heights), asserted at import time.
* RANCH_CASING (§2.3) and its hard rule (w >= 0.0215 for u in
  [0.0015, 0.0735]) and steel_jamb_section() (§2.3 pressed steel, 16 ga,
  1.5 mm inside bends).
* Builders that take Unity coordinates: oriented boxes and cutters, a loft
  along Y, the mitred U-sweep round the opening, a revolve about any Unity
  axis, slotted screw heads, EXACT-boolean cuts.
* hinge_half(): one 4-1/2" five-knuckle butt on the A2 axis, frame half
  (knuckles 1/3/5 + button tips + plate + screws) or leaf half (2/4).
* finalize(): the per-part recipe that the G1 weighted-normal probe (task 0)
  proved survives kitlib.finish() and the FBX round trip with 0.00 deg
  deviation: apply the part's own modifiers -> triangulate n-gons -> shade
  smooth -> sharp by SMOOTH_ANGLE -> paint fr_wear -> WEIGHTED_NORMAL
  modifier (finish() applies it). A WEIGHTED_NORMAL on a part that is not
  yet shaded that way comes out scrambled after the join.
* lod_meta(): LOD1_RATIO / LOD2_RATIO / LOD_DISTANCES into the sidecar.

Segment counts (deviation from §1.8's flat ">= 48 segments", reported in
the G1 build note): round parts are sized by silhouette chord error at the
hero distance (0.3 m, about 1,600 px/m) instead: a knuckle (R 7.4 mm) at 32
segments is off a true circle by 0.035 mm = 0.06 px; a 9.5 mm screw head at
16 segments by 0.09 mm = 0.15 px. 48 everywhere would put every frame 30-45 %
over its §9.1 budget for no visible change. The constants below are the
only place to change if the visual chat wants 48 anyway.
"""

import math
import random

import bmesh
import bpy
from mathutils import Matrix, Vector

# ----------------------------------------------------------------- frames
def U(X, Y, Z):
    """Unity door-root (or part) metres -> Blender."""
    return (-X, -Z, Y)


def to_unity(p):
    return (-p[0], p[2], -p[1])


# ------------------------------------------------------------ §1.4 section
AXIS_X, AXIS_Z = 0.0295, 0.0098          # A2 hinge axis (knuckle centre)
LEAF_X = 0.022                            # visual leaf faces at +-0.022 (44 mm)
LEAF_Y0, LEAF_Y1 = 0.015, 2.095
LEAF_Z_HINGE = 0.005
LEAF_Z_LATCH_S, LEAF_Z_LATCH_P = 0.995, 0.9927   # 3 deg lock-edge bevel
LEAF_Z_LATCH_MID = 0.9939
LATCH_SLOPE = (LEAF_Z_LATCH_S - LEAF_Z_LATCH_P) / (2 * LEAF_X)   # dZ/dX of the bevel
ARRIS = 0.0015                            # 1.5 mm x 45 deg arris chamfers
LINING_Z_H, LINING_Z_L, LINING_Y = 0.002, 0.998, 2.098
LINING_BACK_H, LINING_BACK_L, LINING_BACK_Y = -0.0005, 1.0005, 2.1005
WALL_X = 0.0795                           # casing back / lining ends (wall face 0.08, 0.5 mm buried)
STOP_X0, STOP_X1 = -0.0605, -0.025        # push-side stop, 35.5 mm
STOP_Z_H, STOP_Z_L = 0.018, 0.982         # stop faces into the opening (16 mm proud)
HEAD_STOP_Y0 = 2.082
CASING_X1 = 0.105
CASING_U = 0.077                          # casing reach: Z -0.075 / 1.075, Y 2.175
CASING_TOP = LINING_Y + CASING_U          # 2.175
SADDLE_HALF, SADDLE_FLAT_HALF, SADDLE_H = 0.076, 0.052, 0.012
HINGES = ((0.254, 0.368), (1.0565, 1.1705), (1.859, 1.973))
HINGE_H = 0.114
KNUCKLE_PITCH = HINGE_H / 5               # 0.0228
KNUCKLE_GAP = 0.0004
KNUCKLE_R = 0.0074                        # spec 0.0075; 0.1 mm off the leaf face (see hinge_half)
PLATE_X0, PLATE_X1 = -0.016, 0.022        # hinge leaves: 8 mm frame / 6 mm door backset (02 §5.1)
PLATE_T = 0.0034                          # standard weight .134"
STRIKE_BORED = (0.032, 0.124, 1.000)      # ANSI curved-lip strike prep (w, h, centre Y)
STRIKE_MORTISE = (0.032, 0.200, 0.968)
STRIKE_DEPTH = 0.0016

# Segment counts (see the module docstring).
KNUCKLE_SEGS = 32
TIP_SEGS = 16
SCREW_SEGS = 16

# Map trims the frame must enclose (MapWorld:999-1009; 00 "Map trim").
TRIM_FACE, TRIM_X, TRIM_TOP = 0.07, 0.10, 2.17

assert abs(AXIS_X - (LEAF_X + 0.0075)) < 1e-9, "A2 axis must be tangent-off the S face by the knuckle radius"
assert abs((LEAF_Z_LATCH_S + LEAF_Z_LATCH_P) / 2 - LEAF_Z_LATCH_MID) < 2e-4
assert abs(math.degrees(math.atan(LATCH_SLOPE)) - 3.0) < 0.05
assert abs(HINGES[0][1] - HINGES[0][0] - HINGE_H) < 1e-9 and abs(HINGES[1][1] - HINGES[1][0] - HINGE_H) < 1e-9


# --------------------------------------------------------------- casing
def _arc(cx, cy, r, a0, a1, segs):
    """Points on an arc (degrees, inclusive of both ends)."""
    return [(cx + r * math.cos(math.radians(a0 + (a1 - a0) * i / segs)),
             cy + r * math.sin(math.radians(a0 + (a1 - a0) * i / segs))) for i in range(segs + 1)]


def ranch_casing_profile():
    """§2.3 ranch casing with a back band, (u, w): u outward from the lining
    face, w height above the wall face (X 0.0795). Closed (the back w = 0)."""
    p = [(0.0, 0.0), (0.0, 0.0195)]
    p += _arc(0.003, 0.0195, 0.003, 180, 90, 3)[1:]          # R 3 round-over -> (0.003, 0.0225)
    p += [(0.058, 0.0245),                                    # ranch slope
          (0.0583, 0.0232), (0.0598, 0.0232),                 # 1.5 mm quirk
          (0.0600, 0.0247), (0.0605, 0.0255),                 # onto the back band
          (0.0735, 0.0255)]
    p += _arc(0.0735, 0.022, 0.0035, 90, 0, 3)[1:]           # R 3.5 -> (0.077, 0.022)
    p += [(0.077, 0.0)]
    return p


RANCH_CASING = ranch_casing_profile()


def casing_w_at(u, profile=RANCH_CASING):
    """Top-surface height of the casing profile at u (max over the polyline)."""
    best = 0.0
    for (u0, w0), (u1, w1) in zip(profile, profile[1:]):
        if min(u0, u1) - 1e-12 <= u <= max(u0, u1) + 1e-12:
            if abs(u1 - u0) < 1e-12:
                best = max(best, w0, w1)
            else:
                best = max(best, w0 + (w1 - w0) * (u - u0) / (u1 - u0))
    return best


def assert_casing_rule(profile=RANCH_CASING):
    worst = min(casing_w_at(0.0015 + 0.072 * i / 400, profile) for i in range(401))
    assert worst >= 0.0215 - 1e-9, "casing w %.4f < 0.0215 over the trim" % worst
    assert abs(WALL_X + max(w for _, w in profile) - CASING_X1) < 1e-9
    assert abs(max(u for u, _ in profile) - CASING_U) < 1e-9
    return worst


assert_casing_rule()


# ----------------------------------------------------------- 2D helpers
def fillet_polygon(points, radii, segs, closed=True):
    """Replace each corner i of a polyline by a circular arc of radius
    radii[i] (0 = keep the corner) with ``segs`` segments."""
    n = len(points)
    out = []
    for i in range(n):
        p = Vector(points[i])
        r = radii[i]
        if r <= 0 or (not closed and i in (0, n - 1)):
            out.append(tuple(p))
            continue
        a = Vector(points[i - 1]) if i > 0 else Vector(points[-1])
        b = Vector(points[(i + 1) % n])
        d1 = (p - a).normalized()
        d2 = (b - p).normalized()
        cosang = max(-1.0, min(1.0, -d1.dot(d2)))
        theta = math.acos(cosang)                      # interior angle
        t = r / math.tan(theta / 2)
        t1 = p - d1 * t
        t2 = p + d2 * t
        bis = ((-d1) + d2).normalized()
        c = p + bis * (r / math.sin(theta / 2))
        v1, v2 = t1 - c, t2 - c
        a1 = math.atan2(v1.y, v1.x)
        a2 = math.atan2(v2.y, v2.x)
        da = a2 - a1
        while da > math.pi:
            da -= 2 * math.pi
        while da < -math.pi:
            da += 2 * math.pi
        for k in range(segs + 1):
            ang = a1 + da * k / segs
            out.append((c.x + r * math.cos(ang), c.y + r * math.sin(ang)))
    return out


def offset_polyline(points, dist, normals_left=True, closed=False):
    """Miter offset of a polyline by ``dist`` to its left (normals_left)."""
    pts = [Vector(p) for p in points]
    n = len(pts)
    res = []
    for i in range(n):
        if closed or 0 < i < n - 1:
            d1 = (pts[i] - pts[i - 1]).normalized()
            d2 = (pts[(i + 1) % n] - pts[i]).normalized()
        elif i == 0:
            d1 = d2 = (pts[1] - pts[0]).normalized()
        else:
            d1 = d2 = (pts[-1] - pts[-2]).normalized()
        n1 = Vector((-d1.y, d1.x))
        n2 = Vector((-d2.y, d2.x))
        m = (n1 + n2)
        if m.length < 1e-9:
            m = n1
        m.normalize()
        s = dist / max(m.dot(n1), 0.2)
        if not normals_left:
            s = -s
        res.append(tuple(pts[i] + m * s))
    return res


# ------------------------------------------------- steel jamb (§2.3)
SHEET_T = 0.0015          # 16 ga
BEND_RI = 0.0015          # inside bend radius
STEEL_Z_REF = STOP_Z_H    # d = 0 at the stop face


def steel_jamb_section():
    """Closed sheet section of the pressed-steel jamb, (d, X): d outward from
    the stop face (Z 0.018 on the hinge jamb), X across the wall. Outer
    (visible) path: P return -> P face band -> P soffit -> formed stop ->
    rabbet soffit -> S face band -> S return; inner path offset 1.5 mm.
    Outer bend radii: 3.0 mm convex, 1.5 mm concave (1.5 mm inside bends)."""
    zr = STEEL_Z_REF
    outer_xz = [(-WALL_X, -0.075), (-CASING_X1, -0.075), (-CASING_X1, LINING_Z_H), (STOP_X0, LINING_Z_H),
                (STOP_X0, STOP_Z_H), (STOP_X1, STOP_Z_H), (STOP_X1, LINING_Z_H), (CASING_X1, LINING_Z_H),
                (CASING_X1, -0.075), (WALL_X, -0.075)]
    convex = BEND_RI + SHEET_T
    radii = [0, convex, convex, BEND_RI, convex, convex, BEND_RI, convex, convex, 0]
    # work in (X, Z) then convert to (d, X)
    outer = fillet_polygon(outer_xz, radii, 3, closed=False)
    # In (X, Z) the visible side (rooms and opening) is to the LEFT of the
    # path direction, so the sheet's inner surface is offset to the right.
    inner = offset_polyline(outer, SHEET_T, normals_left=False, closed=False)
    poly = outer + list(reversed(inner))
    return [(zr - z, x) for x, z in poly]


# ------------------------------------------------------------ cutters
_CUTTERS = []


def _cutter_collection():
    coll = bpy.data.collections.get("fr_cutters")
    if coll is None:
        coll = bpy.data.collections.new("fr_cutters")
        bpy.context.scene.collection.children.link(coll)
        coll.hide_render = True
    return coll


def _ob_box_bm(centre, half, R):
    bm = bmesh.new()
    c = Vector(centre)
    vs = []
    for sx in (-1, 1):
        for sy in (-1, 1):
            for sz in (-1, 1):
                p = c + R @ Vector((sx * half[0], sy * half[1], sz * half[2]))
                vs.append(bm.verts.new(U(*p)))
    idx = lambda sx, sy, sz: ((sx > 0) * 4 + (sy > 0) * 2 + (sz > 0))
    quads = [(-1, None, None), (1, None, None), (None, -1, None), (None, 1, None), (None, None, -1), (None, None, 1)]
    for ax in range(3):
        for s in (-1, 1):
            corners = []
            for a, b in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
                k = [0, 0, 0]
                k[ax] = s
                o = [i for i in range(3) if i != ax]
                k[o[0]], k[o[1]] = a, b
                corners.append(vs[idx(*k)])
            bm.faces.new(corners)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm


def cutter(lo, hi, R=None, centre=None, half=None):
    """A box cutter in Unity coordinates (lo/hi corners, or centre/half with
    an optional local->Unity rotation R). Not part of the asset."""
    if centre is None:
        centre = [(lo[i] + hi[i]) / 2 for i in range(3)]
        half = [(hi[i] - lo[i]) / 2 for i in range(3)]
    R = R or Matrix.Identity(3)
    bm = _ob_box_bm(centre, half, R)
    me = bpy.data.meshes.new("cutter")
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new("cutter", me)
    _cutter_collection().objects.link(ob)
    ob.hide_render = True
    _CUTTERS.append(ob)
    return ob


def cut(obj, cutters):
    """Boolean DIFFERENCE (EXACT) of each cutter, applied by finalize()."""
    for c in cutters:
        m = obj.modifiers.new("cut", "BOOLEAN")
        m.operation = "DIFFERENCE"
        m.solver = "EXACT"
        m.object = c
    return obj


def clear_cutters():
    for ob in _CUTTERS:
        try:
            bpy.data.objects.remove(ob, do_unlink=True)
        except ReferenceError:
            pass
    _CUTTERS.clear()


# ------------------------------------------------------------- builders
def _part(kit, bm, slot, name, grain=None, uv="metres"):
    obj = kit._new_object(name, bm, slot, uv, "xz")
    if grain:
        obj["fr_grain"] = grain
    return obj


def obox(kit, slot, lo=None, hi=None, centre=None, half=None, R=None, bevel=0.0, segs=2, name="box", grain=None, bevel_angle=40.0):
    """Oriented box in Unity coordinates, optional angle-limited bevel."""
    if centre is None:
        centre = [(lo[i] + hi[i]) / 2 for i in range(3)]
        half = [(hi[i] - lo[i]) / 2 for i in range(3)]
    bm = _ob_box_bm(centre, half, R or Matrix.Identity(3))
    obj = _part(kit, bm, slot, name, grain)
    if bevel > 0:
        kit._bevel(obj, min(bevel, min(half) * 0.9), segs, angle=bevel_angle)
    return obj


def loft_y(kit, rings, slot, name, grain=None, cap=True):
    """Vertical loft: rings = [(Y, [(X, Z), ...]), ...], equal counts."""
    bm = bmesh.new()
    vr = [[bm.verts.new(U(x, y, z)) for x, z in pts] for y, pts in rings]
    n = len(vr[0])
    for a, b in zip(vr, vr[1:]):
        for i in range(n):
            j = (i + 1) % n
            bm.faces.new((a[i], a[j], b[j], b[i]))
    if cap:
        bm.faces.new(vr[0])
        bm.faces.new(list(reversed(vr[-1])))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return _part(kit, bm, slot, name, grain)


def loft_z(kit, rings, slot, name, grain=None, cap=True):
    """Loft along the opening: rings = [(Z, [(X, Y), ...]), ...]."""
    bm = bmesh.new()
    vr = [[bm.verts.new(U(x, y, z)) for x, y in pts] for z, pts in rings]
    n = len(vr[0])
    for a, b in zip(vr, vr[1:]):
        for i in range(n):
            j = (i + 1) % n
            bm.faces.new((a[i], a[j], b[j], b[i]))
    if cap:
        bm.faces.new(vr[0])
        bm.faces.new(list(reversed(vr[-1])))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return _part(kit, bm, slot, name, grain)


def saddle_section(flutes=False):
    """§1.4 / §2.3 saddle in (X, Y): 0.152 wide, flat top +-0.052 at 0.012,
    1:2 bevels, 1 mm eased arrises. flutes=True: 12 longitudinal flutes,
    0.8 mm deep at 6 mm pitch on the flat, a 12 mm screw land at X 0."""
    h, f, w = SADDLE_H, SADDLE_FLAT_HALF, SADDLE_HALF
    top = []
    if flutes:
        centres = [c * sgn for c in (0.009, 0.015, 0.021, 0.027, 0.033, 0.039) for sgn in (-1, 1)]
        for c in sorted(centres):
            top += [(c - 0.0015, h), (c - 0.0010, h - 0.0008), (c + 0.0010, h - 0.0008), (c + 0.0015, h)]
    pts = [(-w, 0.0), (-w, 0.0006), (-f, h)] + top + [(f, h), (w, 0.0006), (w, 0.0)]
    radii = [0.0, 0.0008, 0.0010] + [0.0] * len(top) + [0.0010, 0.0008, 0.0]
    return fillet_polygon(pts, radii, 2, closed=True)


def saddle_top_at(x):
    """Top height of the plain saddle at X (for scribing stops onto it)."""
    ax = abs(x)
    if ax <= SADDLE_FLAT_HALF:
        return SADDLE_H
    if ax >= SADDLE_HALF:
        return 0.0
    return max(0.0, SADDLE_H - (ax - SADDLE_FLAT_HALF) / 2.0)


def u_sweep(kit, profile, slot, name, z_h, z_l, y_t, y_bot=0.0, legs=("hinge", "head", "latch"),
            grains=("z", "y", "z"), continuous=False, caps=True, bot_fn=None):
    """Mitred sweep of a closed (d, X) profile round the opening: d grows
    outward from the inner line (Z = z_h - d on the hinge jamb, Y = y_t + d
    on the head, Z = z_l + d on the latch jamb). Jambs stop at y_bot (or
    bot_fn(X, d)). continuous=True gives one object (no mitre caps)."""
    def ybot(d, x):
        return bot_fn(x, d) if bot_fn else y_bot
    ring = {
        "hb": [(x, ybot(d, x), z_h - d) for d, x in profile],
        "hm": [(x, y_t + d, z_h - d) for d, x in profile],
        "lm": [(x, y_t + d, z_l + d) for d, x in profile],
        "lb": [(x, ybot(d, x), z_l + d) for d, x in profile],
    }
    def build(keys, nm, grain):
        bm = bmesh.new()
        vr = [[bm.verts.new(U(*p)) for p in ring[k]] for k in keys]
        n = len(profile)
        for a, b in zip(vr, vr[1:]):
            for i in range(n):
                j = (i + 1) % n
                bm.faces.new((a[i], a[j], b[j], b[i]))
        if caps:
            bm.faces.new(vr[0])
            bm.faces.new(list(reversed(vr[-1])))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        return _part(kit, bm, slot, nm, grain)
    if continuous:
        return [build(["hb", "hm", "lm", "lb"], name, None)]
    out = []
    spec = {"hinge": (["hb", "hm"], grains[0]), "head": (["hm", "lm"], grains[1]), "latch": (["lm", "lb"], grains[2])}
    for leg in legs:
        keys, g = spec[leg]
        out.append(build(keys, "%s %s" % (name, leg), g))
    return out


def _frame_for_axis(axis):
    a = Vector(axis).normalized()
    ref = Vector((0, 1, 0)) if abs(a.y) < 0.9 else Vector((1, 0, 0))
    e1 = ref.cross(a).normalized()
    e2 = a.cross(e1).normalized()
    return a, e1, e2


def revolve(kit, profile, base, axis, segs, slot, name, phase=0.0, close_start=True, close_end=True, grain=None):
    """Revolve (r, h) about the Unity axis through ``base``; h along axis.
    r = 0 at an end makes a pole."""
    a, e1, e2 = _frame_for_axis(axis)
    b = Vector(base)
    bm = bmesh.new()
    rings = []
    for r, h in profile:
        if r < 1e-7:
            rings.append([bm.verts.new(U(*(b + a * h)))])
            continue
        ring = []
        for i in range(segs):
            t = 2 * math.pi * i / segs + phase
            p = b + a * h + (e1 * math.cos(t) + e2 * math.sin(t)) * r
            ring.append(bm.verts.new(U(*p)))
        rings.append(ring)
    for A, B in zip(rings, rings[1:]):
        if len(A) == 1 and len(B) == 1:
            continue
        if len(A) == 1:
            for i in range(segs):
                bm.faces.new((A[0], B[i], B[(i + 1) % segs]))
        elif len(B) == 1:
            for i in range(segs):
                bm.faces.new((A[i], A[(i + 1) % segs], B[0]))
        else:
            for i in range(segs):
                j = (i + 1) % segs
                bm.faces.new((A[i], A[j], B[j], B[i]))
    if close_start and len(rings[0]) > 1:
        bm.faces.new(rings[0])
    if close_end and len(rings[-1]) > 1:
        bm.faces.new(list(reversed(rings[-1])))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return _part(kit, bm, slot, name, grain)


def screw_head(kit, centre, normal, slot, rng, kind="flat", d=0.0095, segs=None, proud=0.00015,
               slot_w=None, host=None, notch=0.0010, name="screw"):
    """A slotted screw head facing ``normal`` (Unity), seated on the surface
    at ``centre``. kind "flat": a countersunk head flush with the surface
    (0.15 mm proud, a 0.4 mm rim chamfer reads as the countersink ring);
    the slot goes through the head and ``notch`` deep into ``host`` (the
    plate it sits in), so it reads as a real 1 mm slot. kind "oval": a
    raised oval head (dome ~0.18 d), slot cut into the dome. The slot angle
    is random, as installers leave them."""
    segs = segs or SCREW_SEGS
    r = d / 2
    slot_w = slot_w or max(0.0008, d * 0.13)
    if kind == "flat":
        prof = [(r, 0.0), (r - 0.0004, proud), (0.0, proud)]
        top = proud
        lo_z = -notch
    else:
        dome = d * 0.18
        prof = [(r, 0.0), (r, 0.0001), (r * 0.82, 0.0001 + dome * 0.55), (r * 0.45, 0.0001 + dome * 0.92), (0.0, 0.0001 + dome)]
        top = 0.0001 + dome
        lo_z = top - max(0.0006, dome * 0.6)
    n = Vector(normal).normalized()
    obj = revolve(kit, prof, centre, n, segs, slot, name, phase=rng.uniform(0, math.pi))
    a, e1, e2 = _frame_for_axis(n)
    ang = rng.uniform(0, math.pi)
    t = e1 * math.cos(ang) + e2 * math.sin(ang)
    s = n.cross(t)
    R = Matrix((t, s, n)).transposed()   # local x = t, y = s, z = n
    hi_z = top + 0.0006
    c = Vector(centre) + n * ((lo_z + hi_z) / 2)
    cut(obj, [cutter(None, None, R=R, centre=c, half=(r * 1.25, slot_w / 2, (hi_z - lo_z) / 2))])
    if host is not None and kind == "flat":
        # the slot continues into the plate under the head: floor at -notch
        c2 = Vector(centre) + n * ((-notch + 0.0003) / 2)
        cut(host, [cutter(None, None, R=R, centre=c2, half=(r * 0.96, slot_w / 2, (notch + 0.0003) / 2))])
    obj["fr_screw"] = True
    return obj


def flat_head(kit, centre, normal, slot, rng, d=0.0095, half_segs=None, proud=0.00015, chamfer=0.0004,
              slot_w=None, host=None, notch=0.0010, name="screw"):
    """Slotted countersunk (flat) head, built directly (no boolean on the
    head): two D-shaped halves, each a rim chamfer band (z 0 -> proud) and a
    flat top, with the slot wall between them. 16 rim points by default,
    44 tris. The slot continues ``notch`` deep into ``host`` (a box cut on
    the plate under the head), so it reads as a real 1 mm slot."""
    m = half_segs or SCREW_SEGS // 2
    r = d / 2
    w = slot_w or max(0.0008, d * 0.13)
    n = Vector(normal).normalized()
    a_, e1, e2 = _frame_for_axis(n)
    ang = rng.uniform(0, math.pi)
    t = e1 * math.cos(ang) + e2 * math.sin(ang)
    s_ = n.cross(t)
    c0 = Vector(centre)
    bm = bmesh.new()
    for sg in (1, -1):
        a0 = math.asin((w / 2) / r)
        b0 = math.asin((w / 2) / (r - chamfer))
        rim, inn = [], []
        for k in range(m):
            th = a0 + (math.pi - 2 * a0) * k / (m - 1)
            ph = b0 + (math.pi - 2 * b0) * k / (m - 1)
            rim.append(bm.verts.new(U(*(c0 + t * (r * math.cos(th)) + s_ * (sg * r * math.sin(th))))))
            inn.append(bm.verts.new(U(*(c0 + n * proud + t * ((r - chamfer) * math.cos(ph)) + s_ * (sg * (r - chamfer) * math.sin(ph))))))
        faces = []
        for k in range(m - 1):
            faces.append(bm.faces.new((rim[k], rim[k + 1], inn[k + 1], inn[k])))
        faces.append(bm.faces.new(inn))
        faces.append(bm.faces.new((rim[0], inn[0], inn[-1], rim[-1])))
    bm.normal_update()
    # orient: away from the head's centre line, and up for the top
    nb = Vector(U(*n)) - Vector(U(0, 0, 0))
    cb = Vector(U(*c0))
    for f in bm.faces:
        fc = f.calc_center_median()
        radial = fc - cb
        want = radial - nb * radial.dot(nb)
        if len(f.verts) > 4:
            want = nb
        elif want.length < 1e-9:
            want = nb
        if f.normal.dot(want) < 0:
            f.normal_flip()
    obj = _part(kit, bm, slot, name)
    if host is not None:
        R = Matrix((t, s_, n)).transposed()
        c2 = c0 + n * ((-notch + 0.0003) / 2)
        cut(host, [cutter(None, None, R=R, centre=c2, half=(r * 0.97, w / 2, (notch + 0.0003) / 2))])
    obj["fr_screw"] = True
    return obj


# --------------------------------------------------------------- hinges
def knuckle_range(y0, i):
    return (y0 + i * KNUCKLE_PITCH + KNUCKLE_GAP / 2, y0 + (i + 1) * KNUCKLE_PITCH - KNUCKLE_GAP / 2)


def hinge_half(kit, which, y0, slot, rng, screws=True, tips=True):
    """One butt hinge half on the A2 axis (AXIS_X, *, AXIS_Z), 0.114 tall.

    frame: knuckles 1/3/5, a domed pin button tip above 5 and below 1, the
      frame leaf mortised flush into the hinge lining (front face Z 0.002,
      X PLATE_X0 -> 0.030, running under the barrel), a curl web under each
      of its knuckles, four slotted countersunk #12 screws.
    leaf: knuckles 2/4, the door leaf mortised flush into the leaf's hinge
      edge (front face Z 0.005, X PLATE_X0 -> 0.022), a curl web beside each
      of its knuckles, four screws.
    Knuckle R 0.0074 (spec 0.0075): the leaf's S face is tangent to the A2
    circle of radius 0.0075, so the frame knuckles sweep past it at 0.1 mm
    instead of touching it. Returns the list of parts."""
    parts = []
    ks = (0, 2, 4) if which == "frame" else (1, 3)
    for i in ks:
        ya, yb = knuckle_range(y0, i)
        k = revolve(kit, [(KNUCKLE_R, 0.0), (KNUCKLE_R, yb - ya)], (AXIS_X, ya, AXIS_Z), (0, 1, 0),
                    KNUCKLE_SEGS, slot, "knuckle %d" % (i + 1), phase=math.pi / KNUCKLE_SEGS)
        parts.append(k)
    yc = y0 + HINGE_H / 2
    if which == "frame":
        plate = obox(kit, slot, lo=(PLATE_X0, y0, LINING_Z_H - PLATE_T), hi=(0.030, y0 + HINGE_H, LINING_Z_H),
                     bevel=0.0003, segs=1, name="hinge leaf frame")
        parts.append(plate)
        for i in ks:
            ya, yb = knuckle_range(y0, i)
            parts.append(obox(kit, slot, lo=(0.0250, ya, 0.0015), hi=(0.0340, yb, 0.0050), name="curl web"))
        if tips:
            prof = [(0.00475, 0.0), (0.00475, 0.0008), (0.0034, 0.0029), (0.0, 0.0035)]
            parts.append(revolve(kit, prof, (AXIS_X, y0 + HINGE_H - KNUCKLE_GAP / 2, AXIS_Z), (0, 1, 0), TIP_SEGS, slot,
                                 "pin tip top", close_start=False))
            parts.append(revolve(kit, prof, (AXIS_X, y0 + KNUCKLE_GAP / 2, AXIS_Z), (0, -1, 0), TIP_SEGS, slot,
                                 "pin tip bottom", close_start=False))
        face_z, nrm, xs = LINING_Z_H, (0, 0, 1), (-0.0030, 0.0090)
    else:
        plate = obox(kit, slot, lo=(PLATE_X0, y0, LEAF_Z_HINGE), hi=(PLATE_X1, y0 + HINGE_H, LEAF_Z_HINGE + PLATE_T),
                     bevel=0.0003, segs=1, name="hinge leaf door")
        parts.append(plate)
        for i in ks:
            ya, yb = knuckle_range(y0, i)
            parts.append(obox(kit, slot, lo=(PLATE_X1, ya, LEAF_Z_HINGE), hi=(0.0262, yb, AXIS_Z + 0.001), name="curl web"))
        face_z, nrm, xs = LEAF_Z_HINGE, (0, 0, -1), (0.0090, -0.0030)
    if screws:
        for k, dy in enumerate((-0.042, -0.014, 0.014, 0.042)):
            x = xs[k % 2]
            s = flat_head(kit, (x, yc + dy, face_z), nrm, slot, rng, d=0.0095, host=plate, name="hinge screw")
            parts.append(s)
    return parts


# ------------------------------------------------------------- finalize
def _edge_flags(me, angle_deg=20.0):
    """Per vertex: True where adjacent face normals differ by > angle."""
    lim = math.cos(math.radians(angle_deg))
    vf = [[] for _ in me.vertices]
    for p in me.polygons:
        for v in p.vertices:
            vf[v].append(p.normal)
    flags = []
    for ns in vf:
        e = False
        for i in range(len(ns)):
            for j in range(i + 1, len(ns)):
                if ns[i].dot(ns[j]) < lim:
                    e = True
                    break
            if e:
                break
        flags.append(e)
    return flags


def paint_wear(obj, fn):
    """fr_wear colour attribute (W2, NEEDS APPROVAL to be read): white =
    no wear; R darkens for hand grime, G for edge wear, B for cavity.
    fn(P_unity, N_unity, is_edge, obj) -> (r, g, b)."""
    me = obj.data
    mw = obj.matrix_world
    rot = mw.to_3x3()
    flags = _edge_flags(me)
    attr = me.color_attributes.get("fr_wear") or me.color_attributes.new("fr_wear", "BYTE_COLOR", "POINT")
    for i, v in enumerate(me.vertices):
        p = mw @ v.co
        nn = (rot @ v.normal).normalized()
        r, g, b = fn(to_unity(p), to_unity(nn), flags[i], obj) if fn else (1.0, 1.0, 1.0)
        attr.data[i].color_srgb = (max(0.0, min(1.0, r)), max(0.0, min(1.0, g)), max(0.0, min(1.0, b)), 1.0)


def smoothstep(e0, e1, x):
    t = max(0.0, min(1.0, (x - e0) / (e1 - e0)))
    return t * t * (3 - 2 * t)


def finalize(kit, smooth_angle, wear_fn=None, protect=()):
    """Per-part recipe proven by the G1 probe (see module docstring).
    protect: parts whose vertices join kitlib's fr_lod_keep group, so
    make_lod1() never collapses them (the gap-closing envelope)."""
    for obj in list(kit.parts):
        bpy.ops.object.select_all(action="DESELECT")
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        for mod in list(obj.modifiers):
            bpy.ops.object.modifier_apply(modifier=mod.name)
    clear_cutters()
    for obj in kit.parts:
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        big = [f for f in bm.faces if len(f.verts) > 4]
        if big:
            bmesh.ops.triangulate(bm, faces=big, quad_method="BEAUTY", ngon_method="BEAUTY")
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-7)
        bm.to_mesh(obj.data)
        bm.free()
        me = obj.data
        me.shade_smooth()
        me.set_sharp_from_angle(angle=math.radians(smooth_angle))
        paint_wear(obj, wear_fn)
        if obj in protect:
            vg = obj.vertex_groups.get("fr_lod_keep") or obj.vertex_groups.new(name="fr_lod_keep")
            vg.add([v.index for v in me.vertices], 1.0, "REPLACE")
        m = obj.modifiers.new("wn", "WEIGHTED_NORMAL")
        m.mode = "FACE_AREA"
        m.weight = 50
        m.keep_sharp = True


def lod_meta(kit, module):
    d = list(getattr(module, "LOD_DISTANCES", (None, None, None)))
    kit.meta["lodDistances"] = [(-1.0 if x is None else float(x)) for x in d]
    kit.meta["lodRatios"] = [1.0, float(getattr(module, "LOD1_RATIO", 0)), float(getattr(module, "LOD2_RATIO", 0))]
    kit.meta["lodNote"] = "lodDistances (d01, d12, dcull) m at lodBias 1, FOV 76; -1 = never cull. LOD2 is not exported until P-1."


def lod2_drop(obj):
    obj["fr_lod2_drop"] = True
    return obj


def anchor(kit, name, X, Y, Z):
    kit.anchor(name, U(X, Y, Z))


def tri_count(objs):
    n = 0
    for o in objs:
        n += sum(len(p.vertices) - 2 for p in o.data.polygons)
    return n


# ---------------------------------------------------------------- checks
def bounds_unity(objs):
    """(min, max) over the parts' vertices in Unity coordinates (parts are
    built in Unity-mapped space with identity transforms, so co is world)."""
    lo = [1e9] * 3
    hi = [-1e9] * 3
    for o in objs:
        mw = o.matrix_world
        for v in o.data.vertices:
            p = to_unity(mw @ v.co)
            for i in range(3):
                lo[i] = min(lo[i], p[i])
                hi[i] = max(hi[i], p[i])
    return lo, hi


def assert_box(objs, lo, hi, tol=2e-4, what="part"):
    blo, bhi = bounds_unity(objs)
    for i in range(3):
        assert blo[i] >= lo[i] - tol and bhi[i] <= hi[i] + tol, \
            "%s bounds %s..%s outside %s..%s" % (what, [round(x, 4) for x in blo], [round(x, 4) for x in bhi], lo, hi)
    return blo, bhi


def assert_outside_leaf(objs, what="frame"):
    """No static frame vertex inside the closed visual leaf's box (the
    hinge knuckles are outside: X > 0.022)."""
    for o in objs:
        mw = o.matrix_world
        for v in o.data.vertices:
            X, Y, Z = to_unity(mw @ v.co)
            inside = (-LEAF_X + 1e-4 < X < LEAF_X - 1e-4) and (LEAF_Y0 + 1e-4 < Y < LEAF_Y1 - 1e-4) and \
                     (LEAF_Z_HINGE + 1e-4 < Z < LEAF_Z_LATCH_P - 1e-4)
            assert not inside, "%s vertex %s inside the closed leaf" % (what, (round(X, 4), round(Y, 4), round(Z, 4)))
