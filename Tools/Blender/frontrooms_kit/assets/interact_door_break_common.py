"""Shared code for the pre-fractured DOOR BREAK leaves (door-break prototype DB2, 2026-10-30; production P1, same day;
research/door_break/50_rethink.md sections 3.7 and 4.3 item 1, 52_production.md). Red approved the look 2026-10-30
("1a, 2 yes, 3 yes, 4 baked, 5 yes"); P1 adds LOD2, three split motifs per family, the Ward leaf's armor plates and the
dust palettes.

NOT AN ASSET: no NAME and no build(). interact_door_leaf_veneer_da_break.py imports it. It reads the D1.6
constants and the leaf section from interact_door_da_common and never changes them, so the intact D1.6
leaves rebuild byte-identically.

Frames: door root D (Unity metres; origin = the map hinge on the wall centre plane at the floor; +Y up;
+Z along the opening, latch end at Z 0.995; +X = face S). Every piece is authored for a BURST toward +X
(the push face, where the Relay stands, is -X). A -X burst mirrors the break root at run time.

Geometry is built in pure Python (class Geo, Unity coordinates), then turned into Blender objects only at
export (to_blender), so door_break_bake.py can rebuild the debris without a scene.

What it makes (per stage, 50_rethink 3.3 and 3.7):
  body      the intact slab with the cassette window removed (Z 0.60..0.995, Y 0.50..1.70), lofted from the
            D1.6 section (da.leaf_section) so it matches the intact leaf outside the window;
  cassettes the window at stage M, D1, D2, TORN: prisms across the 44 mm (veneer caps, Door_CoreWood /
            Door_CoreStile cut walls), the split motif, the latch strip, the petal, splinters, the bore and
            the lock chassis; one object per stage and LOD;
  leak      thin emissive cards inside the split (stages M, D1, D2);
  debris    the torn latch strip with its faceplate, splinters and crumb clusters (FINAL), each its own object
            with its origin at its centroid (the baked throw keys move these origins).
"""

import math
import random

import interact_door_common as dc
import interact_door_da_common as da

LEAF_X = da.LEAF_X                        # 0.022
LEAF_Y0, LEAF_Y1 = da.LEAF_Y0, da.LEAF_Y1
LATCH_Z = da.LEAF_Z_LATCH                 # 0.995
WIN_Z0, WIN_Z1 = 0.600, LATCH_Z           # the cassette window (50_rethink 3.7)
WIN_Y0, WIN_Y1 = 0.500, 1.700
SKIN_T = 0.0022                           # veneer 0.6 + glue + crossband 1.6 mm (the petal's thickness)
VENEER_T = 0.0006
STILE_Z = 0.960                           # hardwood edge stile from here to the latch end
BORE_Y, BORE_Z, BORE_R = 1.000, 0.920, 0.027    # the 2-1/8" cross bore at the 75 mm backset
CHASSIS_R = 0.0225                        # the bored lock chassis (zinc), 4.5 mm inside the bore
LOCK_BLOCK = (0.85, 1.15)                 # the hardwood lock block around the bore (Y)

VENEER, CORE, STILE, CHIP, BRASS, CHROME, LEAK = (
    "Door_Veneer", "Door_CoreWood", "Door_CoreStile", "Prop_Chipboard", "Prop_Brass", "Prop_ChromeSatin", "Door_LeakCard")
SLOT_COLOURS = {
    VENEER: (0.337, 0.153, 0.044), CORE: (0.66, 0.55, 0.40), STILE: (0.72, 0.58, 0.40), CHIP: (0.61, 0.52, 0.40),
    BRASS: (0.70, 0.52, 0.25), CHROME: (0.70, 0.70, 0.70), LEAK: (1.0, 0.9, 0.7),
}


# ===================================================================== geometry container
class Geo:
    """Vertices in Unity door-root metres; faces = (indices, slot, uvmode). uvmode: "box" (the kit's metre box
    projection, so veneer faces continue the body's texture), "core" (U = depth across the leaf, V = along the
    grain), or ("proj", e, d) (U = p.e, V = p.d)."""

    def __init__(self):
        self.v = []
        self.f = []

    def vert(self, p):
        self.v.append((float(p[0]), float(p[1]), float(p[2])))
        return len(self.v) - 1

    def face(self, idx, slot, uv="box", hint=None):
        idx = list(idx)
        if hint is not None:
            n = newell([self.v[i] for i in idx])
            if dot(n, hint) < 0:
                idx.reverse()
        if len(idx) > 4:
            # convex n-gons only reach here (cylinder caps): fan them, so tangents export
            for k in range(1, len(idx) - 1):
                self.f.append(((idx[0], idx[k], idx[k + 1]), slot, uv))
            return
        self.f.append((tuple(idx), slot, uv))

    def extend(self, other):
        base = len(self.v)
        self.v.extend(other.v)
        for idx, slot, uv in other.f:
            self.f.append((tuple(i + base for i in idx), slot, uv))
        return self

    def map(self, fn):
        self.v = [tuple(fn(p)) for p in self.v]
        return self

    def tris(self):
        return sum(len(idx) - 2 for idx, _s, _u in self.f)

    def centroid(self):
        n = max(1, len(self.v))
        return tuple(sum(p[i] for p in self.v) / n for i in range(3))

    def bounds(self):
        return ([min(p[i] for p in self.v) for i in range(3)], [max(p[i] for p in self.v) for i in range(3)])


def dot(a, b):
    return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]


def sub(a, b):
    return (a[0] - b[0], a[1] - b[1], a[2] - b[2])


def add(a, b):
    return (a[0] + b[0], a[1] + b[1], a[2] + b[2])


def mul(a, s):
    return (a[0] * s, a[1] * s, a[2] * s)


def cross(a, b):
    return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])


def norm(a):
    l = math.sqrt(dot(a, a)) or 1.0
    return (a[0] / l, a[1] / l, a[2] / l)


def newell(pts):
    nx = ny = nz = 0.0
    for i in range(len(pts)):
        a, b = pts[i], pts[(i + 1) % len(pts)]
        nx += (a[1] - b[1]) * (a[2] + b[2])
        ny += (a[2] - b[2]) * (a[0] + b[0])
        nz += (a[0] - b[0]) * (a[1] + b[1])
    return (nx, ny, nz)


def area2(poly):
    """Signed area of a (z, y) polygon (CCW > 0)."""
    s = 0.0
    for i in range(len(poly)):
        a, b = poly[i], poly[(i + 1) % len(poly)]
        s += a[0] * b[1] - b[0] * a[1]
    return s * 0.5


def clean(poly, eps=2e-5):
    out = []
    for p in poly:
        if not out or abs(p[0] - out[-1][0]) > eps or abs(p[1] - out[-1][1]) > eps:
            out.append(p)
    if len(out) > 2 and abs(out[0][0] - out[-1][0]) <= eps and abs(out[0][1] - out[-1][1]) <= eps:
        out.pop()
    return out


def tessellate(poly):
    """Triangles of a simple (z, y) polygon (ear clipping; mathutils when inside Blender)."""
    try:
        from mathutils import Vector
        from mathutils.geometry import tessellate_polygon
        return [tuple(t) for t in tessellate_polygon([[Vector((p[0], p[1], 0.0)) for p in poly]])]
    except ImportError:
        return _earclip(poly)


def _earclip(poly):
    idx = list(range(len(poly)))
    if area2(poly) < 0:
        idx.reverse()
    out = []
    guard = 0
    while len(idx) > 3 and guard < 10000:
        guard += 1
        for k in range(len(idx)):
            i0, i1, i2 = idx[k - 1], idx[k], idx[(k + 1) % len(idx)]
            a, b, c = poly[i0], poly[i1], poly[i2]
            if (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0]) <= 0:
                continue
            ok = True
            for j in idx:
                if j in (i0, i1, i2):
                    continue
                p = poly[j]
                d1 = (b[0] - a[0]) * (p[1] - a[1]) - (b[1] - a[1]) * (p[0] - a[0])
                d2 = (c[0] - b[0]) * (p[1] - b[1]) - (c[1] - b[1]) * (p[0] - b[0])
                d3 = (a[0] - c[0]) * (p[1] - c[1]) - (a[1] - c[1]) * (p[0] - c[0])
                if d1 >= 0 and d2 >= 0 and d3 >= 0:
                    ok = False
                    break
            if ok:
                out.append((i0, i1, i2))
                idx.pop(k)
                break
    if len(idx) == 3:
        out.append(tuple(idx))
    return out


# ===================================================================== primitives
def prism(outline, x0, x1, cap_lo=VENEER, cap_hi=VENEER, wall=None, xoff=None):
    """A solid across X from x0 to x1 over a simple (z, y) outline. wall(z_mid, y_mid, dz, dy) -> slot.
    xoff(y, z) -> extra X shift per vertex (the strip's bend). Caps take the "box" mapping (veneer continuity),
    walls take "core"."""
    g = Geo()
    poly = clean(outline)
    if area2(poly) < 0:
        poly = list(reversed(poly))
    sh = xoff or (lambda y, z: 0.0)
    lo = [g.vert((x0 + sh(y, z), y, z)) for z, y in poly]
    hi = [g.vert((x1 + sh(y, z), y, z)) for z, y in poly]
    for t in tessellate(poly):
        g.face([lo[i] for i in t], cap_lo, "box", hint=(-1, 0, 0))
        g.face([hi[i] for i in t], cap_hi, "box", hint=(1, 0, 0))
    n = len(poly)
    for i in range(n):
        j = (i + 1) % n
        (za, ya), (zb, yb) = poly[i], poly[j]
        dz, dy = zb - za, yb - ya
        slot = wall((za + zb) * .5, (ya + yb) * .5, dz, dy) if wall else CORE
        # CCW outline: the outward normal of edge (dz, dy) is (dy, -dz) in (z, y)
        g.face([lo[i], lo[j], hi[j], hi[i]], slot, "core", hint=(0, -dz, dy))
    return g


def box(lo, hi, slot, uv="box"):
    g = Geo()
    c = [(lo[0] if i & 1 == 0 else hi[0], lo[1] if i & 2 == 0 else hi[1], lo[2] if i & 4 == 0 else hi[2]) for i in range(8)]
    vi = [g.vert(p) for p in c]
    for axis in range(3):
        for s in (0, 1):
            corners = [k for k in range(8) if ((k >> axis) & 1) == s]
            a, b, c2, d = corners
            hint = [0, 0, 0]
            hint[axis] = 1 if s else -1
            g.face([vi[a], vi[b], vi[d], vi[c2]], slot, uv, hint=tuple(hint))
    return g


def cylinder_x(cy, cz, r, x0, x1, segs, slot, cap_slot=None):
    g = Geo()
    lo, hi = [], []
    for k in range(segs):
        a = 2 * math.pi * k / segs
        y, z = cy + r * math.sin(a), cz + r * math.cos(a)
        lo.append(g.vert((x0, y, z)))
        hi.append(g.vert((x1, y, z)))
    for k in range(segs):
        j = (k + 1) % segs
        a = 2 * math.pi * (k + .5) / segs
        g.face([lo[k], lo[j], hi[j], hi[k]], slot, "core", hint=(0, math.sin(a), math.cos(a)))
    g.face(lo, cap_slot or slot, "box", hint=(-1, 0, 0))
    g.face(hi, cap_slot or slot, "box", hint=(1, 0, 0))
    return g


def splinter(root, along, out, length, width, rng, slot=STILE, segs=3, curl=0.35):
    """A tapered triangular prism of wood fibre: from ``root`` along ``along`` (unit), curling toward ``out``
    (unit, the burst side), ``length`` long, ``width`` wide at the root and 12 % of that at the tip."""
    g = Geo()
    along, out = norm(along), norm(out)
    side = norm(cross(along, out))
    rings = []
    p = root
    d = along
    step = length / segs
    for s in range(segs + 1):
        t = s / segs
        w = width * (1.0 - 0.88 * t)
        h = w * 0.55
        a = g.vert(add(p, mul(side, -w * .5)))
        b = g.vert(add(p, mul(side, w * .5)))
        c = g.vert(add(p, mul(out, h)))
        rings.append((a, b, c, p))
        if s < segs:
            d = norm(add(d, mul(out, curl * (0.6 + 0.8 * rng.random()) / segs)))
            p = add(p, mul(d, step))
    uv = ("proj", side, along)
    for s in range(segs):
        r0, r1 = rings[s], rings[s + 1]
        mid = mul(add(r0[3], r1[3]), .5)
        for i, j in ((0, 1), (1, 2), (2, 0)):
            q = [r0[i], r0[j], r1[j], r1[i]]
            c = mul(add(add(g.v[r0[i]], g.v[r0[j]]), add(g.v[r1[i]], g.v[r1[j]])), .25)
            g.face(q, slot, uv, hint=sub(c, mid))
    g.face([rings[0][0], rings[0][1], rings[0][2]], slot, uv, hint=mul(along, -1))
    g.face([rings[-1][0], rings[-1][1], rings[-1][2]], slot, uv, hint=d)
    return g


def crumb(centre, size, rng, slot=CHIP):
    """A granular particleboard crumb: a jittered octahedron (8 tris)."""
    g = Geo()
    ax = [(1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1)]
    vi = [g.vert(add(centre, mul(a, size * (0.55 + 0.6 * rng.random())))) for a in ax]
    for sx in (0, 1):
        for sy in (2, 3):
            for sz in (4, 5):
                c = (1 if sx == 0 else -1, 1 if sy == 2 else -1, 1 if sz == 4 else -1)
                g.face([vi[sx], vi[sy], vi[sz]], slot, "box", hint=c)
    return g


# ===================================================================== the split motif (one seeded polyline)
class Motif:
    """The split line Z(y) along the grain at the lock (50_rethink 3.7 item 2): Z 0.905..0.935, a run-out of
    +-6 mm over 400 mm plus a 1.5 mm fibre wander, sampled every 10 mm. The same motif cuts the mesh at every
    stage and (production) drives the _FR_DAMAGE onset mask."""

    def __init__(self, seed):
        rng = random.Random(seed)
        self.seed = seed
        ph = rng.uniform(0, 2 * math.pi)
        self.ys = [WIN_Y0 + 0.01 * k for k in range(int(round((WIN_Y1 - WIN_Y0) / 0.01)) + 1)]
        wander = 0.0
        self.zs = []
        for y in self.ys:
            wander = 0.8 * wander + rng.uniform(-0.0012, 0.0012)
            z = 0.919 + 0.006 * math.sin(2 * math.pi * (y - 1.0) / 0.8 + ph) + wander
            self.zs.append(min(0.935, max(0.905, z)))

    def z(self, y):
        k = (y - WIN_Y0) / 0.01
        i = max(0, min(len(self.ys) - 2, int(math.floor(k))))
        t = min(1.0, max(0.0, k - i))
        return self.zs[i] * (1 - t) + self.zs[i + 1] * t

    def samples(self, y0, y1, step=0.01):
        n = max(2, int(round((y1 - y0) / step)) + 1)
        return [y0 + (y1 - y0) * k / (n - 1) for k in range(n)]


def lens(y, y0, y1, ypk, gmax, gmin=0.0, power=0.7):
    """A crack's opening at height y: 0 at the ends y0, y1, gmax at ypk (the bore)."""
    if y <= y0 or y >= y1:
        return 0.0
    u = (y - y0) / (ypk - y0) if y < ypk else (y1 - y) / (y1 - ypk)
    return gmin + (gmax - gmin) * (math.sin(.5 * math.pi * min(1.0, u)) ** power)


def bore_half(y, r=BORE_R):
    dy = y - BORE_Y
    return math.sqrt(max(0.0, r * r - dy * dy)) if abs(dy) < r else None


def wall_slot(z, y, dz, dy):
    """Cut faces: the hardwood stile (Z >= 0.96) and the lock block around the bore split long (Door_CoreStile);
    the particleboard core crumbles (Door_CoreWood). The latch end itself is the edge band (Door_Veneer)."""
    if z > LATCH_Z - 0.0005 and abs(dz) < 1e-6:
        return VENEER
    if z >= STILE_Z - 0.002 or (LOCK_BLOCK[0] <= y <= LOCK_BLOCK[1] and z >= 0.86):
        return STILE
    return CORE


# ===================================================================== the body (outside the window)
def _insert_z(section, zc):
    """Add the points where the section's edges cross Z = zc (the window's hinge-side wall)."""
    out = []
    n = len(section)
    for i in range(n):
        a, b = section[i], section[(i + 1) % n]
        out.append(a)
        if (a[1] - zc) * (b[1] - zc) < 0:
            t = (zc - a[1]) / (b[1] - a[1])
            out.append((a[0] + (b[0] - a[0]) * t, zc))
    return out


def _loft(rings, slot, caps=(True, True), skip_window_wall=False):
    """Rings [(Y, [(X, Z), ...])], equal counts, capped at the ends asked for (caps tessellated here: the D1.6 section
    is concave at its seams); "box" UVs (the kit's own projection). P1: the interface faces with the cassette window
    are deleted (50_rethink 3.7 item 1): no cap on a window line (Y 0.50 / 1.70), and with skip_window_wall no side
    face on the window's hinge-side wall (both ends at Z 0.60)."""
    g = Geo()
    vr = [[g.vert((x, y, z)) for x, z in pts] for y, pts in rings]
    n = len(vr[0])
    pts0 = rings[0][1]
    cx = sum(p[0] for p in pts0) / n
    cz = sum(p[1] for p in pts0) / n
    for a, b, (ya, pa) in zip(vr, vr[1:], rings):
        for i in range(n):
            j = (i + 1) % n
            if skip_window_wall and abs(pa[i][1] - WIN_Z0) < 1e-7 and abs(pa[j][1] - WIN_Z0) < 1e-7:
                continue
            mx = (pa[i][0] + pa[j][0]) * .5
            mz = (pa[i][1] + pa[j][1]) * .5
            g.face([a[i], a[j], b[j], b[i]], slot, "box", hint=(mx - cx, 0.0, mz - cz))
    for k, (ring, (y, pts), hy) in enumerate(((vr[0], rings[0], -1), (vr[-1], rings[-1], 1))):
        if not caps[k]:
            continue
        poly = [(p[1], p[0]) for p in pts]          # (z, x) for the tessellator
        for t in tessellate(poly):
            g.face([ring[i] for i in t], slot, "box", hint=(0, hy, 0))
    return g


def body(level, seams):
    """The intact slab minus the window: A (Y0..0.50, full section), B (0.50..1.70, hinge side of Z 0.60),
    C (1.70..Y1, full section). The sections are da.leaf_section's, with the Z 0.60 points inserted, so the
    faces outside the window keep the intact leaf's shape and metre UVs. P1: the interface faces are deleted (they sit
    against the cassette's own closed walls; 50_rethink 3.7 item 1)."""
    def sec(o):
        return _insert_z(da.leaf_section(o, level, seams=seams), WIN_Z0)

    a_rings = [(y, sec_pts) for y, sec_pts in da.chamfer_rings(LEAF_Y0, 1, lambda o: sec(o), level)] + [(WIN_Y0, sec(0.0))]
    c_rings = [(WIN_Y1, sec(0.0))] + list(reversed(da.chamfer_rings(LEAF_Y1, -1, lambda o: sec(o), level)))
    full = sec(0.0)
    clip = [p for p in full if p[1] <= WIN_Z0 + 1e-7]
    g = Geo()
    # A keeps its bottom cap (the leaf's foot), C its top; B (the hinge side beside the window) has none, and its
    # Z 0.60 wall is the cassette's (the interfaces are inside the closed leaf: deleted, P1)
    g.extend(_loft(a_rings, VENEER, caps=(True, False)))
    g.extend(_loft([(WIN_Y0, clip), (WIN_Y1, clip)], VENEER, caps=(False, False), skip_window_wall=True))
    g.extend(_loft(c_rings, VENEER, caps=(False, True)))
    return g


# ===================================================================== stage cassettes
STAGES = ("M", "D1", "D2", "TORN")
# The split's extent, opening at the bore and the strip's push toward the burst face, per stage (50_rethink 3.3).
SPLIT = {
    "M": dict(y0=0.850, y1=1.150, gmax=0.0012, gmin=0.0006, push=0.0, twist=0.0),
    "D1": dict(y0=0.780, y1=1.240, gmax=0.0050, gmin=0.0012, push=0.0040, twist=0.0),
    "D2": dict(y0=0.620, y1=1.420, gmax=0.0180, gmin=0.0020, push=0.0220, twist=0.0),
}
NOTCH = (0.720, 1.380)                      # TORN: the latch block that tears out (90 x 660 mm)
PETAL = dict(y0=1.060, y1=1.290, w=0.110, deg=40.0)   # D2 / TORN: veneer + crossband peeled from the slot's hinge lip


class Stage:
    """Everything a stage needs from the motif: z_h(y) (the hinge-side lip), z_s(y) (the strip's lip), push(y)."""

    def __init__(self, motif, name, lod):
        self.m, self.name, self.lod = motif, name, lod
        s = SPLIT.get(name, SPLIT["D2"])
        self.y0, self.y1, self.gmax, self.gmin, self.pushmax = s["y0"], s["y1"], s["gmax"], s["gmin"], s["push"]

    def gap(self, y):
        return lens(y, self.y0, self.y1, BORE_Y, self.gmax, self.gmin)

    def push(self, y):
        if self.pushmax <= 0 or y <= self.y0 or y >= self.y1:
            return 0.0
        u = (y - self.y0) / (self.y1 - self.y0)
        return self.pushmax * (math.sin(math.pi * u) ** 0.8)

    def z_h(self, y):
        z = self.m.z(y) - self.gap(y) * 0.5
        b = bore_half(y)
        return min(z, BORE_Z - b) if b is not None and BORE_Z - b < z else z

    def z_s(self, y):
        z = self.m.z(y) + self.gap(y) * 0.5
        b = bore_half(y)
        return max(z, BORE_Z + b) if b is not None and BORE_Z + b > z else z

    def ys(self, y0, y1):
        step = 0.01 if self.lod == 0 else 0.02 if self.lod == 1 else 0.10
        ys = self.m.samples(y0, y1, step)
        # the bore's bite: sample its arc finely
        n = 16 if self.lod == 0 else 8 if self.lod == 1 else 2
        extra = [BORE_Y + BORE_R * math.sin(-.5 * math.pi + math.pi * k / n) for k in range(n + 1)]
        ys = sorted(set([round(y, 5) for y in ys + [e for e in extra if y0 < e < y1]]))
        return ys


def _hinge_outline(st, y0, y1, notch=None):
    """The hinge-side piece: the window minus the strip region [y0, y1] (or the torn notch, with ragged ends)."""
    pts = [(WIN_Z0, WIN_Y0), (LATCH_Z, WIN_Y0)]
    if notch is None:
        pts.append((LATCH_Z, y0))
        for y in st.ys(y0, y1):
            pts.append((st.z_h(y), y))
        pts.append((LATCH_Z, y1))
    else:
        pts += notch
    pts += [(LATCH_Z, WIN_Y1), (WIN_Z0, WIN_Y1)]
    return pts


def _strip_outline(st, y0, y1):
    ys = st.ys(y0, y1)
    return [(st.z_s(y), y) for y in ys] + [(LATCH_Z, y1), (LATCH_Z, y0)]


def _tear(rng, z_from, y_at, sign, steps=4, amp=0.014):
    """A ragged cross-grain tear from the split (z_from) to the latch end at about y_at: hardwood breaks in steps
    along the grain (sign = +1 going up into the leaf, -1 going down)."""
    pts = []
    zs = [z_from + (LATCH_Z - z_from) * k / steps for k in range(steps + 1)]
    y = y_at
    for k, z in enumerate(zs):
        dy = sign * amp * (0.3 + 0.7 * rng.random()) * (1 if k % 2 == 0 else -0.6)
        pts.append((z, y))
        if 0 < k < steps:
            y2 = y + dy
            pts.append((z, y2))
            y = y2
    return pts


def notch_outline(st, rng):
    """TORN: the notch edge = the split's hinge lip between NOTCH[0] and NOTCH[1] (with the bore's half bite), and
    ragged stepped tears to the latch end at both ends."""
    a, b = NOTCH
    bot = _tear(rng, st.z_h(a), a, -1)          # from the split down-and-out to the latch end... reversed below
    top = _tear(rng, st.z_h(b), b, +1)
    pts = list(reversed(bot))                   # latch end -> split at the bottom
    for y in st.ys(a, b):
        pts.append((st.z_h(y), y))
    pts += top                                  # split -> latch end at the top
    # keep the outline monotone along the latch end
    pts[0] = (LATCH_Z, pts[0][1])
    pts[-1] = (LATCH_Z, pts[-1][1])
    return pts


def cassette(motif, name, lod, seed=0):
    """One stage of the window, canonical burst toward +X. Returns (geo, info) with anchors."""
    rng = random.Random(1000 * (seed + 1) + STAGES.index(name) * 17 + lod)
    st = Stage(motif, name, lod)
    g = Geo()
    info = {}
    x0, x1 = -LEAF_X, LEAF_X
    torn = name == "TORN"
    petal = name in ("D2", "TORN")
    if torn:
        st = Stage(motif, "D2", lod)
        nout = notch_outline(st, random.Random(7000 + seed))
        hinge = _hinge_outline(st, None, None, notch=nout)
    else:
        hinge = _hinge_outline(st, st.y0, st.y1)
    # The hinge-side piece: core (to the skin) + the burst-face skin (minus the petal footprint).
    if petal:
        py0, py1, pw = PETAL["y0"], PETAL["y1"], PETAL["w"]
        lip = [(st.z_h(y), y) for y in st.ys(py0, py1)]
        foot = [(z - pw + 0.012 * math.sin(7.0 * y + 1.3), y) for z, y in lip]
        petal_poly = list(reversed(lip)) + foot     # the lip top -> bottom, then the foot line bottom -> top
        g.extend(prism(hinge, x0, x1 - SKIN_T, cap_lo=VENEER, cap_hi=CHIP, wall=wall_slot))
        skin = _skin_minus(hinge, lip, foot, st)
        g.extend(prism(skin, x1 - SKIN_T, x1, cap_lo=CHIP, cap_hi=VENEER, wall=lambda z, y, dz, dy: STILE if z > 0.86 else CORE))
        # the petal: the same skin, peeled about its foot line (the hinge-side edge) toward +X
        pg = prism(petal_poly, x1 - SKIN_T, x1, cap_lo=STILE, cap_hi=VENEER, wall=lambda *a: STILE)
        fz = sum(z for z, _y in foot) / len(foot)
        ang = math.radians(PETAL["deg"] if not torn else PETAL["deg"] + 12.0)

        def peel(p, fz=fz, ang=ang):
            x, y, z = p
            dz, dx = z - fz, x - (x1 - SKIN_T)
            if dz <= 0:
                return p
            # rotate in the X-Z plane about the foot line (X = x1 - SKIN_T, Z = fz), lifting toward +X
            return ((x1 - SKIN_T) + dx * math.cos(ang) + dz * math.sin(ang), y, fz + dz * math.cos(ang) - dx * math.sin(ang))
        pg.map(peel)
        g.extend(pg)
        info["petal_hinge"] = (x1, (py0 + py1) * .5, fz)
    else:
        g.extend(prism(hinge, x0, x1, wall=wall_slot))
    # The latch strip(s).
    if not torn:
        if name == "D1":
            # the stile splits and the strip stands proud in 4 segments (a stepped bend)
            cuts = [st.y0, 0.900, 1.020, 1.130, st.y1]
            proud = [0.0015, 0.0035, 0.0040, 0.0020]
            for k in range(4):
                d = proud[k]
                g.extend(prism(_strip_outline(st, cuts[k], cuts[k + 1]), x0 + d, x1 + d, wall=wall_slot))
        else:
            g.extend(prism(_strip_outline(st, st.y0, st.y1), x0, x1, wall=wall_slot, xoff=lambda y, z: st.push(y)))
        # the faceplate on the latch end (it rides on the strip)
        fy = 1.000
        fx = st.push(fy) if name == "D2" else (0.0035 if name == "D1" else 0.0)
        g.extend(box((-0.0143 + fx, fy - 0.0286, LATCH_Z - 0.0012), (0.0143 + fx, fy + 0.0286, LATCH_Z + 0.0003), BRASS))
        info["strip_dx_latch"] = fx
    else:
        # the split beyond the notch stays as open cracks, the stile there knocked 5 mm out
        for ya, yb in ((st.y0, NOTCH[0] - 0.012), (NOTCH[1] + 0.012, st.y1)):
            if yb - ya > 0.02:
                g.extend(prism(_strip_outline(st, ya, yb), x0 + 0.005, x1 + 0.005, wall=wall_slot))
        info["strip_dx_latch"] = 0.0
        info["notch"] = (LATCH_Z, NOTCH[0], NOTCH[1])
    # The lock chassis in the bore (zinc case of the bored lock), from stage M on.
    g.extend(cylinder_x(BORE_Y, BORE_Z, CHASSIS_R, x0 + 0.003, x1 - 0.003, 24 if lod == 0 else 12, CHROME))
    # Lips and splinters on the burst face.
    if name == "D1":
        lip_g = prism([(st.z_h(y) - 0.026, y) for y in (1.06, 1.18)] + [(st.z_h(y), y) for y in (1.18, 1.06)],
                      x1, x1 + VENEER_T, cap_lo=STILE, cap_hi=VENEER, wall=lambda *a: STILE)
        g.extend(lip_g.map(lambda p: (p[0] + 0.002 * max(0.0, (p[2] - (st.z_h(p[1]) - 0.026)) / 0.026), p[1], p[2])))
        for k, y in enumerate((0.93, 1.07, 1.16)):
            if lod and k == 1:
                continue
            g.extend(splinter((x1 - 0.001, y, st.z_s(y) + 0.002), (0, 1 if k % 2 else -1, 0.08), (1, 0, 0.2), 0.035 + 0.02 * k, 0.004, rng))
    if name in ("D2", "TORN"):
        n = 26 if lod == 0 else 10
        for k in range(n):
            y = st.y0 + 0.06 + (st.y1 - st.y0 - 0.12) * (k + rng.random() * .6) / n
            if torn and NOTCH[0] - 0.01 < y < NOTCH[1] + 0.01:
                continue
            onstrip = k % 2 == 0
            z = (st.z_s(y) + 0.001) if onstrip else (st.z_h(y) - 0.001)
            if not onstrip and PETAL["y0"] < y < PETAL["y1"]:
                continue
            xr = x1 - 0.0008 + (st.push(y) if onstrip else 0.0)
            ln = 0.030 + 0.150 * rng.random() ** 1.6
            if lod and ln < 0.07:
                continue
            g.extend(splinter((xr, y, z), (0, 1 if rng.random() < .5 else -1, (0.15 if onstrip else -0.15)), (1, 0, 0.1 if onstrip else -0.1),
                              ln, 0.002 + 0.006 * rng.random(), rng))
        # the branch crack down the edge band: a 3 mm wedge out of the latch end below the bore
        if not torn:
            br = [(LATCH_Z - 0.0006, 0.80), (LATCH_Z - 0.0006, 0.97), (LATCH_Z + 0.0008, 0.97), (LATCH_Z + 0.0008, 0.80)]
            g.extend(prism(br, -0.0025 + st.push(0.88), 0.0025 + st.push(0.88), cap_lo=STILE, cap_hi=STILE, wall=lambda *a: STILE))
    if torn:
        # splinter teeth at the notch's ragged ends, pointing into the notch, bent to the burst face
        for end, sgn in ((NOTCH[0], 1), (NOTCH[1], -1)):
            for k in range(5 if lod == 0 else 2):
                z = 0.945 + 0.045 * (k + .5) / 5
                g.extend(splinter((x1 - 0.004 - 0.008 * rng.random(), end + sgn * 0.004, z), (0.25, sgn, 0.0), (1, 0, 0),
                                  0.04 + 0.05 * rng.random(), 0.004 + 0.004 * rng.random(), rng))
        # crumbled core on the notch's hinge lip: granular crumbs stuck on the cut wall
        for k in range(18 if lod == 0 else 6):
            y = NOTCH[0] + 0.03 + (NOTCH[1] - NOTCH[0] - 0.06) * rng.random()
            g.extend(crumb((rng.uniform(-0.016, 0.016), y, st.z_h(y) + 0.002), 0.003 + 0.003 * rng.random(), rng))
    # anchors (door root, canonical +X burst)
    info["bore"] = (0.0, BORE_Y, BORE_Z)
    info["split_bot"] = (0.0, st.y0, motif.z(st.y0))
    info["split_top"] = (0.0, st.y1, motif.z(st.y1))
    info["contact_push"] = (-LEAF_X, 1.30, 0.745)
    info["gap_at_bore"] = st.gap(BORE_Y) if not torn else 0.09
    return g, info


def cassette_l2(motif, name):
    """LOD2 of a stage (beyond 12 m, a 1 m leaf is under 50 px): M and D1 are the plain window block; D2 a coarse dark
    slot (hinge piece + the pushed strip); TORN the notch as a stepped rectangle. Burst toward +X."""
    x0, x1 = -LEAF_X, LEAF_X
    if name in ("M", "D1"):
        rect = [(WIN_Z0, WIN_Y0), (LATCH_Z, WIN_Y0), (LATCH_Z, WIN_Y1), (WIN_Z0, WIN_Y1)]
        return prism(rect, x0, x1, wall=wall_slot)
    st = Stage(motif, "D2", 2)
    g = Geo()
    if name == "D2":
        ys = (st.y0, BORE_Y, st.y1)
        hinge = [(WIN_Z0, WIN_Y0), (LATCH_Z, WIN_Y0), (LATCH_Z, st.y0)] + [(st.z_h(y), y) for y in ys] + [(LATCH_Z, st.y1), (LATCH_Z, WIN_Y1), (WIN_Z0, WIN_Y1)]
        strip = [(st.z_s(y), y) for y in ys] + [(LATCH_Z, st.y1), (LATCH_Z, st.y0)]
        g.extend(prism(hinge, x0, x1, wall=wall_slot))
        g.extend(prism(strip, x0, x1, wall=wall_slot, xoff=lambda y, z: st.push(y)))
        return g
    a, b = NOTCH
    notch = [(LATCH_Z, a), (st.z_h(a), a), (st.z_h(BORE_Y), BORE_Y), (st.z_h(b), b), (LATCH_Z, b)]
    g.extend(prism(_hinge_outline(st, None, None, notch=notch), x0, x1, wall=wall_slot))
    return g


# ===================================================================== the Ward leaf's armor plates (P1)
ARMOR_T = 0.00127                     # .050" satin stainless (interact_door_leaf_ward_da.KT)
ARMOR_Y = (0.018, 0.831)
ARMOR_Z = (0.030, 0.970)
ARMOR = "Prop_Aluminium"              # the kit slot; the run time gives it the intact leaf's plate surface


def clip_box(poly, z0, z1, y0, y1):
    """Sutherland-Hodgman: a (z, y) polygon clipped to the box [z0, z1] x [y0, y1]."""
    def clip(pts, inside, cut):
        out = []
        n = len(pts)
        for i in range(n):
            a, b = pts[i], pts[(i + 1) % n]
            ia, ib = inside(a), inside(b)
            if ia:
                out.append(a)
            if ia != ib:
                out.append(cut(a, b))
        return out

    def zcut(zc):
        return lambda a, b: (zc, a[1] + (b[1] - a[1]) * (zc - a[0]) / (b[0] - a[0]))

    def ycut(yc):
        return lambda a, b: (a[0] + (b[0] - a[0]) * (yc - a[1]) / (b[1] - a[1]), yc)
    pts = list(poly)
    for inside, cut in ((lambda p: p[0] >= z0, zcut(z0)), (lambda p: p[0] <= z1, zcut(z1)),
                        (lambda p: p[1] >= y0, ycut(y0)), (lambda p: p[1] <= y1, ycut(y1))):
        if len(pts) < 3:
            return []
        pts = clip(pts, inside, cut)
    pts = clean(pts)
    return pts if len(pts) >= 3 and abs(area2(pts)) > 1e-8 else []


def _plates(outline, off=None):
    """Both faces' plate over one (z, y) outline; off(y, z) = the wood's X shift under it (the strip's push)."""
    g = Geo()
    if not outline:
        return g
    sh = off or (lambda y, z: 0.0)
    g.extend(prism(outline, LEAF_X, LEAF_X + ARMOR_T, cap_lo=ARMOR, cap_hi=ARMOR, wall=lambda *a: ARMOR, xoff=sh))
    g.extend(prism(outline, -LEAF_X - ARMOR_T, -LEAF_X, cap_lo=ARMOR, cap_hi=ARMOR, wall=lambda *a: ARMOR, xoff=sh))
    return g


def armor_body(lod):
    """The armor plates outside the window (an L on each face), every LOD."""
    z0, z1 = ARMOR_Z
    y0, y1 = ARMOR_Y
    outline = [(z0, y0), (z1, y0), (z1, WIN_Y0), (WIN_Z0, WIN_Y0), (WIN_Z0, y1), (z0, y1)]
    return _plates(outline)


def armor_window(motif, name, lod):
    """The armor plates inside the window at a stage: cut along the same split as the cassette's wood, the strip's
    share riding out with the strip (a .050" plate creases with the wood under it). LOD2: one flat plate."""
    box = (WIN_Z0, ARMOR_Z[1], WIN_Y0, ARMOR_Y[1])
    if lod >= 2 or name == "M":
        return _plates(clip_box([(WIN_Z0, WIN_Y0), (LATCH_Z, WIN_Y0), (LATCH_Z, WIN_Y1), (WIN_Z0, WIN_Y1)], *box))
    g = Geo()
    if name == "TORN":
        st = Stage(motif, "D2", lod)
        nout = notch_outline(st, random.Random(7000 + motif.seed))
        g.extend(_plates(clip_box(_hinge_outline(st, None, None, notch=nout), *box)))
        ya, yb = st.y0, NOTCH[0] - 0.012
        if yb - ya > 0.02:
            g.extend(_plates(clip_box(_strip_outline(st, ya, yb), *box), lambda y, z: 0.005))
        return g
    st = Stage(motif, name, lod)
    g.extend(_plates(clip_box(_hinge_outline(st, st.y0, st.y1), *box)))
    if name == "D1":
        cuts = [st.y0, 0.900, 1.020, 1.130, st.y1]
        proud = [0.0015, 0.0035, 0.0040, 0.0020]
        for k in range(4):
            d = proud[k]
            g.extend(_plates(clip_box(_strip_outline(st, cuts[k], cuts[k + 1]), *box), lambda y, z, d=d: d))
    else:
        g.extend(_plates(clip_box(_strip_outline(st, st.y0, st.y1), *box), lambda y, z: st.push(y)))
    return g


def _skin_minus(hinge, lip, foot, st):
    """The burst-face skin outline: the hinge piece's outline with the petal's footprint (between the lip and the
    foot line) cut out of its notch edge. The lip runs along the notch edge, so the cut is a bay in that edge."""
    y0, y1 = lip[0][1], lip[-1][1]
    out = []
    inserted = False
    for z, y in hinge:
        if y0 - 1e-6 <= y <= y1 + 1e-6 and abs(z - st.z_h(y)) < 1e-6 and z < LATCH_Z - 1e-4:
            if not inserted:
                # the hinge outline runs bottom -> top along the lip; walk the foot line instead
                out += [(st.z_h(y0), y0)] + foot + [(st.z_h(y1), y1)]
                inserted = True
            continue
        out.append((z, y))
    return out


def leak_cards(motif, name):
    """Thin emissive cards on the centre plane inside the split (+1 mm past each lip) and a ring in the bore."""
    if name not in SPLIT:
        return None
    st = Stage(motif, name, 0)
    g = Geo()
    ys = [y for y in st.ys(st.y0, st.y1)]
    lo = [(st.z_h(y) - 0.001, y) for y in ys]
    hi = [(st.z_s(y) + 0.001, y) for y in ys]
    poly = lo + list(reversed(hi))
    sh = (lambda y, z: st.push(y) * 0.5) if name == "D2" else None
    g.extend(prism(poly, -0.0004, 0.0004, cap_lo=LEAK, cap_hi=LEAK, wall=lambda *a: LEAK, xoff=sh))
    return g


# ===================================================================== debris (FINAL)
def debris(motif, seed, lod=0):
    """The pieces the FINAL frees, at their D2 / TORN rest in the leaf (canonical +X burst): the latch strip with
    its faceplate (90 x 660 x 44 mm), splinters and crumb clusters. Returns [(name, geo)]."""
    # P1: the splinters' and crumbs' shapes are the family's (one rng for every motif), so a baked throw fits all three
    # motifs; only their roots follow the motif's split (and the strip's lip is the motif's)
    rng = random.Random(9101)
    st = Stage(motif, "D2", lod)
    out = []
    # the strip: its D2 lip (z_s) between the notch's ragged ends (the same stair heights as the TORN notch)
    trng = random.Random(7000 + seed)
    a, b = NOTCH
    bot = _tear(trng, st.z_s(a), a, -1)
    top = _tear(trng, st.z_s(b), b, +1)
    lip = [(st.z_s(y), y) for y in st.ys(a, b)]
    strip_poly = list(reversed(bot)) + lip + top
    strip_poly[0] = (LATCH_Z, strip_poly[0][1])
    strip_poly[-1] = (LATCH_Z, strip_poly[-1][1])
    sg = prism(strip_poly, -LEAF_X, LEAF_X, wall=wall_slot, xoff=lambda y, z: st.push(y))
    fx = st.push(1.0)
    sg.extend(box((-0.0143 + fx, 0.9714, LATCH_Z - 0.0012), (0.0143 + fx, 1.0286, LATCH_Z + 0.0003), BRASS))
    out.append(("deb_strip", sg))
    for k in range(14):
        y = NOTCH[0] + (NOTCH[1] - NOTCH[0]) * (k + .5) / 14
        z = st.z_h(y) + 0.003
        x = rng.uniform(-0.016, 0.018)
        ln = 0.04 + 0.11 * rng.random() ** 1.3
        out.append(("deb_spl_%02d" % k, splinter((x, y, z), (0.1, 1 if k % 2 else -1, 0.05), (1, 0, 0), ln, 0.003 + 0.004 * rng.random(), rng)))
    for k in range(16):
        y = NOTCH[0] + 0.03 + (NOTCH[1] - NOTCH[0] - 0.06) * rng.random()
        c = (rng.uniform(-0.015, 0.015), y, st.z_h(y) + 0.008)
        cg = Geo()
        for j in range(3 + int(rng.random() * 3)):
            cg.extend(crumb(add(c, (rng.uniform(-.006, .006), rng.uniform(-.008, .008), rng.uniform(-.004, .004))), 0.0035 + 0.004 * rng.random(), rng))
        out.append(("deb_crm_%02d" % k, cg))
    return out


# ===================================================================== Blender export
def to_blender(geo, name, smooth_angle=35.0, origin=None):
    """A Blender mesh object from a Geo (Unity coordinates -> Blender via dc.U), with one material per slot,
    UVs per face mode, outward normals and sharp edges by angle. origin (Unity) moves the object origin there."""
    import bmesh
    import bpy
    from mathutils import Vector
    ox = origin or (0.0, 0.0, 0.0)
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    vs = [bm.verts.new(dc.U(p[0] - ox[0], p[1] - ox[1], p[2] - ox[2])) for p in geo.v]
    slots = []
    modes = []
    for idx, slot, uv in geo.f:
        if len(set(idx)) < 3:
            continue
        try:
            f = bm.faces.new([vs[i] for i in reversed(idx)])   # U() is a reflection: reverse to keep outward
        except ValueError:
            continue
        if slot not in slots:
            slots.append(slot)
        f.material_index = slots.index(slot)
        modes.append((f, uv))
    bm.normal_update()
    layer = bm.loops.layers.uv.verify()
    for f, uv in modes:
        nb = f.normal
        n = (-nb.x, nb.z, -nb.y)
        for loop in f.loops:
            b = loop.vert.co
            p = (-b.x + ox[0], b.z + ox[1], -b.y + ox[2])
            loop[layer].uv = uv_of(p, n, uv)
    bm.to_mesh(me)
    bm.free()
    for s in slots:
        me.materials.append(material(s))
    obj = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(obj)
    obj.location = Vector(dc.U(*ox))
    me.shade_smooth()
    me.set_sharp_from_angle(angle=math.radians(smooth_angle))
    # P1 (the prototype's E->M shading shift, 51 sec. 4.1): the leaf's face planes keep FLAT normals, as the intact kit's
    # do (measured on Kit_DoorLeaf_Veneer_DA LOD0/1: 0.04 deg at the arris rings). Smoothing by angle alone bent the
    # face's top and bottom rings 11.25 deg into the 22.5 deg arris band, which put a hot specular spot on the body's top
    # 0.4 m from the first swap on. Every other face keeps the angle-smoothed normal (a zero custom normal).
    cn = []
    for poly in me.polygons:
        n = poly.normal
        flat = abs(n.x) > 0.99                   # Blender X = -Unity X: the leaf's two faces
        for _li in poly.loop_indices:
            cn.append((n.x, n.y, n.z) if flat else (0.0, 0.0, 0.0))
    me.normals_split_custom_set(cn)
    return obj


def uv_of(p, n, mode):
    X, Y, Z = p
    nX, nY, nZ = n
    if mode == "box":
        if abs(nY) >= abs(nX) and abs(nY) >= abs(nZ):
            s = 1.0 if nY >= 0 else -1.0
            return (-X * s, -Z)
        if abs(nX) >= abs(nZ):
            s = 1.0 if nX <= 0 else -1.0
            return (Z * s, Y)
        s = 1.0 if nZ <= 0 else -1.0
        return (X * s, Y)
    if mode == "core":
        if abs(nX) >= 0.8:
            return (Z, Y)
        return (X + LEAF_X, Y if abs(nY) <= 0.7 else Z)
    _tag, e, d = mode
    return (dot(p, e), dot(p, d))


def material(slot):
    import bpy
    m = bpy.data.materials.get(slot)
    if m is None:
        m = bpy.data.materials.new(slot)
        c = SLOT_COLOURS.get(slot, (0.6, 0.6, 0.6))
        m.diffuse_color = (c[0], c[1], c[2], 1.0)
    return m


def export_fbx(objects, path):
    import bpy
    bpy.ops.object.select_all(action="DESELECT")
    for o in objects:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"MESH"}, apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", bake_space_transform=True,
        mesh_smooth_type="OFF", use_mesh_modifiers=True, use_tspace=True, add_leaf_bones=False, bake_anim=False,
        path_mode="STRIP")


def reset_scene():
    import bpy
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0


def boundary_check(body_geo, cas_geo, tol=0.00005):
    """G2: every body vertex on the window's interface planes must have a cassette vertex within tol, or lie on a
    cassette edge (the shared boundary). Returns the worst distance in metres over the face lines Y 0.50 / 1.70 at
    X = +-0.022 (the visible seam)."""
    worst = 0.0
    pts = [p for p in body_geo.v if (abs(p[1] - WIN_Y0) < 1e-6 or abs(p[1] - WIN_Y1) < 1e-6) and abs(abs(p[0]) - LEAF_X) < 1e-6
           and WIN_Z0 - 1e-6 <= p[2] <= 0.98]
    cas = [p for p in cas_geo.v if (abs(p[1] - WIN_Y0) < 1e-6 or abs(p[1] - WIN_Y1) < 1e-6)]
    for p in pts:
        # the cassette's cap edge runs straight along Z at the same X and Y: distance to that line
        best = min((abs(p[0] - q[0]) + abs(p[1] - q[1]) for q in cas), default=1.0)
        worst = max(worst, best)
    return worst
