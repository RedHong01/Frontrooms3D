"""FRONTROOMSSS ceiling lamp options L01-L10 (lamp_options workflow, build stage, 2026-10-31).

Run (headless, Blender 4.3):
  Blender --background --python build_lamps.py -- <outdir> [L01 L02 ...]
Writes, per option, <outdir>/LampOpt_<ID>.fbx and <outdir>/LampOpt_<ID>_layout.json, plus
<outdir>/LampOpt_CeilingCuts.fbx and <outdir>/build_report.json (triangles per mesh, counted from
the triangulated mesh that is exported).

Spec: lamp_options/docs/20_options.md (dimensions per option). All sizes below are mm.
Blender frame: X = lamp short axis, Y = lamp long axis, Z up. Z = 0 is the ceiling plane
(the printed tile face); positive Z goes INTO the ceiling (the "depth" column of the spec).
FBX export bakes -Z forward / Y up, so Unity sees (x, y, z) = (-x, z, -y): Unity +Y is up into
the ceiling, the long axis runs along Unity Z (FrontRooms kitlib convention).

Meshes per option (object names are the contract with the Unity setup script):
  Lens         the one renderer that takes the lamp's _EmissionColor (lens, lit return / walls,
               louver blades, reflector, real tubes, can baffle). No LOD.
  Housing_L0/1/2  unlit exterior (T-bar ring, door frame, reveal, rim, hinges, latches,
               lampholders); switch at 4 m and 12 m.
  Phone        one mesh on the lens material for the phone tier (frame in swatches with no
               emission).
Variants: L02 Housing_b_L*, L03 Soil, L05 Lens_Sag + Open_Lens + Open_Housing_L*.
No text, brand or logo anywhere. ASCII only.
"""
import bpy
import bmesh
import json
import math
import os
import sys
from mathutils import Vector

ARGV = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.abspath(ARGV[0]) if ARGV else os.path.join(HERE, "out")
ONLY = set(ARGV[1:])
os.makedirs(OUT, exist_ok=True)

# --------------------------------------------------------------------------- trim sheet
# LampOpt_Trim: 1024 x 1024, 1 px per mm, U repeats every 1024 mm (wrap Repeat). Rows (px from
# the bottom = UV v) hold one finish each; geometry maps its profile across a row.
TRIM_PX = 1024.0
STRIPS = {
    "tbar": (0, 120), "enamel": (120, 280), "alu": (280, 400), "black": (400, 520),
    "zinc": (520, 620), "holder": (620, 720), "rim_alu": (720, 850), "tile": (850, 1024),
}
PAD = 8.0

SIDE_SHIFT = [0.0, 311.0, 523.0, 787.0]   # decorrelate the four sides of a ring along U


def trim_uv(finish, along, t, ttot, side=0):
    lo, hi = STRIPS[finish]
    span = hi - lo - 2 * PAD
    scale = min(1.0, span / max(ttot, 1e-6))
    v = lo + PAD + (span - ttot * scale) * 0.5 + t * scale
    u = along + SIDE_SHIFT[side % 4]
    return (u / TRIM_PX, v / TRIM_PX)


def trim_mapper(finish):
    return lambda side, along, t, ttot: trim_uv(finish, along, t, ttot, side)


def box_trim(finish):
    """Box projection into a finish row for small parts: U from the longest axis, V compressed."""
    def f(p, n):
        ax = max(range(3), key=lambda i: abs(n[i]))
        a, b = [(1, 2), (0, 2), (0, 1)][ax]
        lo, hi = STRIPS[finish]
        span = hi - lo - 2 * PAD
        v = lo + PAD + span * 0.5 + max(-span * 0.5, min(span * 0.5, p[b] * 0.5))
        return ((p[a] + 97.0 * ax) / TRIM_PX, v / TRIM_PX)
    return f


# --------------------------------------------------------------------------- mesh accumulator
class Acc:
    def __init__(self):
        self.V, self.F, self.UV, self.SM, self.MAT = [], [], [], [], []

    def v(self, p):
        self.V.append((float(p[0]), float(p[1]), float(p[2])))
        return len(self.V) - 1

    def f(self, idx, uvs, smooth=False, want=None, mat=0):
        if want is not None:
            n = newell([self.V[i] for i in idx])
            if n[0] * want[0] + n[1] * want[1] + n[2] * want[2] < 0:
                idx = list(idx)[::-1]
                uvs = list(uvs)[::-1]
        self.F.append(list(idx))
        self.UV.append([tuple(u) for u in uvs])
        self.SM.append(bool(smooth))
        self.MAT.append(mat)

    def tris(self):
        return sum(len(f) - 2 for f in self.F)

    def add(self, other):
        o = len(self.V)
        self.V += other.V
        self.F += [[i + o for i in f] for f in other.F]
        self.UV += other.UV
        self.SM += other.SM
        self.MAT += other.MAT
        return self

    def transformed(self, fn):
        a = Acc()
        a.V = [fn(p) for p in self.V]
        a.F = [list(f) for f in self.F]
        a.UV = [list(u) for u in self.UV]
        a.SM = list(self.SM)
        a.MAT = list(self.MAT)
        return a


def newell(pts):
    nx = ny = nz = 0.0
    for i in range(len(pts)):
        x0, y0, z0 = pts[i]
        x1, y1, z1 = pts[(i + 1) % len(pts)]
        nx += (y0 - y1) * (z0 + z1)
        ny += (z0 - z1) * (x0 + x1)
        nz += (x0 - x1) * (y0 + y1)
    return (nx, ny, nz)


# --------------------------------------------------------------------------- profiles
def fillet(pts, radii, segs):
    """Round the interior corners of a 2D polyline. Returns [((d, z), smooth_into_this_point)]."""
    out = [(tuple(pts[0]), False)]
    for i in range(1, len(pts) - 1):
        r = radii[i] if i < len(radii) else 0.0
        P, A, B = Vector(pts[i]), Vector(pts[i - 1]), Vector(pts[i + 1])
        if r <= 0 or segs <= 0:
            out.append((tuple(pts[i]), False))
            continue
        u1 = (A - P).normalized()
        u2 = (B - P).normalized()
        ang = math.acos(max(-1.0, min(1.0, u1.dot(u2))))
        if ang < 1e-3 or ang > math.pi - 1e-3:
            out.append((tuple(pts[i]), False))
            continue
        td = r / math.tan(ang / 2)
        T1, T2 = P + u1 * td, P + u2 * td
        C = P + (u1 + u2).normalized() * (r / math.sin(ang / 2))
        a1 = math.atan2(T1.y - C.y, T1.x - C.x)
        a2 = math.atan2(T2.y - C.y, T2.x - C.x)
        da = (a2 - a1 + math.pi) % (2 * math.pi) - math.pi
        out.append(((T1.x, T1.y), False))
        for k in range(1, segs + 1):
            a = a1 + da * k / segs
            out.append(((C.x + r * math.cos(a), C.y + r * math.sin(a)), True))
    out.append((tuple(pts[-1]), False))
    return out


def plain(pts):
    return [(tuple(p), False) for p in pts]


OUTW = [(1, 0), (0, 1), (-1, 0), (0, -1)]
TANG = [(0, 1), (-1, 0), (0, -1), (1, 0)]


def rect_corners(hx, hy, d, s):
    X, Y = hx + d, hy + d
    C = [(X, -Y), (X, Y), (-X, Y), (-X, -Y)]
    return C[s], C[(s + 1) % 4]


def sweep_rect(acc, hx, hy, prof, uvf, sides=(0, 1, 2, 3), mat=0):
    """Sweep a (d, z) profile around the rectangle |x| = hx + d, |y| = hy + d (mitred corners).
    The visible side of segment p0 -> p1 is n = (dz, -dd) (d outward, z up); order profiles so.
    uvf(side, along_mm, t_mm, t_total) -> (u, v)."""
    pts = [p for p, _ in prof]
    sm = [s for _, s in prof]
    t = [0.0]
    for i in range(1, len(pts)):
        t.append(t[-1] + math.hypot(pts[i][0] - pts[i - 1][0], pts[i][1] - pts[i - 1][1]))
    tt = t[-1]
    for s in sides:
        o, tg = OUTW[s], TANG[s]
        rows = []
        for (d, z) in pts:
            a, b = rect_corners(hx, hy, d, s)
            rows.append((acc.v((a[0], a[1], z)), acc.v((b[0], b[1], z)), a, b))
        for i in range(len(pts) - 1):
            dd = pts[i + 1][0] - pts[i][0]
            dz = pts[i + 1][1] - pts[i][1]
            if abs(dd) + abs(dz) < 1e-9:
                continue
            want = (o[0] * dz, o[1] * dz, -dd)
            ia0, ib0, a0, b0 = rows[i]
            ia1, ib1, a1, b1 = rows[i + 1]
            al = lambda p: p[0] * tg[0] + p[1] * tg[1]
            uv = [uvf(s, al(a0), t[i], tt), uvf(s, al(b0), t[i], tt), uvf(s, al(b1), t[i + 1], tt), uvf(s, al(a1), t[i + 1], tt)]
            acc.f([ia0, ib0, ib1, ia1], uv, sm[i + 1], want, mat)


def sweep_round(acc, r0, prof, n, uvf, mat=0, cx=0.0, cy=0.0):
    """Sweep a (d, z) profile around a circle of radius r0 + d. uvf(frac_around, t, t_total)."""
    pts = [p for p, _ in prof]
    sm = [s for _, s in prof]
    t = [0.0]
    for i in range(1, len(pts)):
        t.append(t[-1] + math.hypot(pts[i][0] - pts[i - 1][0], pts[i][1] - pts[i - 1][1]))
    tt = t[-1]
    rings = []
    for (d, z) in pts:
        r = r0 + d
        rings.append([acc.v((cx + r * math.cos(2 * math.pi * k / n), cy + r * math.sin(2 * math.pi * k / n), z)) for k in range(n)])
    for i in range(len(pts) - 1):
        dd = pts[i + 1][0] - pts[i][0]
        dz = pts[i + 1][1] - pts[i][1]
        for k in range(n):
            k1 = (k + 1) % n
            am = 2 * math.pi * (k + 0.5) / n
            want = (math.cos(am) * dz, math.sin(am) * dz, -dd)
            f0, f1 = k / n, (k + 1) / n
            uv = [uvf(f0, t[i], tt), uvf(f1, t[i], tt), uvf(f1, t[i + 1], tt), uvf(f0, t[i + 1], tt)]
            acc.f([rings[i][k], rings[i][k1], rings[i + 1][k1], rings[i + 1][k]], uv, sm[i + 1] or True, want, mat)
    return rings


def disk(acc, cx, cy, z, r, n, uvf, down=True, mat=0, dome=0.0, rings=1):
    """Fan disk facing down (or up); optional dome (mm toward -z at the centre)."""
    want = (0, 0, -1 if down else 1)
    centre = acc.v((cx, cy, z - dome))
    prev = None
    ring_r = [r * (1 - j / rings) for j in range(rings)]
    ring_ids = []
    for j, rr in enumerate(ring_r):
        zz = z - dome * (1 - (rr / r) ** 2)
        ring_ids.append([acc.v((cx + rr * math.cos(2 * math.pi * k / n), cy + rr * math.sin(2 * math.pi * k / n), zz)) for k in range(n)])
    for j in range(len(ring_ids) - 1):
        for k in range(n):
            k1 = (k + 1) % n
            ids = [ring_ids[j][k], ring_ids[j][k1], ring_ids[j + 1][k1], ring_ids[j + 1][k]]
            acc.f(ids, [uvf(acc.V[i]) for i in ids], True, want, mat)
    last = ring_ids[-1]
    for k in range(n):
        k1 = (k + 1) % n
        ids = [last[k], last[k1], centre]
        acc.f(ids, [uvf(acc.V[i]) for i in ids], True, want, mat)


def quad(acc, pts, uvf, want, smooth=False, mat=0):
    ids = [acc.v(p) for p in pts]
    acc.f(ids, [uvf(p) for p in pts], smooth, want, mat)


def box(acc, c, s, uvf, skip=("+z",), mat=0):
    """Axis box centre c, size s; faces listed in skip are not built (hidden faces)."""
    hx, hy, hz = s[0] / 2, s[1] / 2, s[2] / 2
    x, y, z = c
    faces = {
        "-z": ([(x - hx, y - hy, z - hz), (x + hx, y - hy, z - hz), (x + hx, y + hy, z - hz), (x - hx, y + hy, z - hz)], (0, 0, -1)),
        "+z": ([(x - hx, y - hy, z + hz), (x - hx, y + hy, z + hz), (x + hx, y + hy, z + hz), (x + hx, y - hy, z + hz)], (0, 0, 1)),
        "+x": ([(x + hx, y - hy, z - hz), (x + hx, y - hy, z + hz), (x + hx, y + hy, z + hz), (x + hx, y + hy, z - hz)], (1, 0, 0)),
        "-x": ([(x - hx, y - hy, z - hz), (x - hx, y + hy, z - hz), (x - hx, y + hy, z + hz), (x - hx, y - hy, z + hz)], (-1, 0, 0)),
        "+y": ([(x - hx, y + hy, z - hz), (x + hx, y + hy, z - hz), (x + hx, y + hy, z + hz), (x - hx, y + hy, z + hz)], (0, 1, 0)),
        "-y": ([(x - hx, y - hy, z - hz), (x - hx, y - hy, z + hz), (x + hx, y - hy, z + hz), (x + hx, y - hy, z - hz)], (0, -1, 0)),
    }
    for k, (pts, n) in faces.items():
        if k in skip:
            continue
        quad(acc, pts, lambda p, n=n: uvf(p, n), n, False, mat)


def cylinder(acc, p0, p1, r, n, uvf, mat=0, cap0=False, cap1=False, skip_up=False):
    """Cylinder p0 -> p1. uvf(frac_around, along_mm, length_mm). skip_up drops faces facing +z."""
    p0, p1 = Vector(p0), Vector(p1)
    ax = p1 - p0
    L = ax.length
    ax.normalize()
    e1 = ax.cross(Vector((0, 0, 1)))
    if e1.length < 1e-6:
        e1 = ax.cross(Vector((1, 0, 0)))
    e1.normalize()
    e2 = ax.cross(e1)
    dirs = [e1 * math.cos(2 * math.pi * k / n) + e2 * math.sin(2 * math.pi * k / n) for k in range(n)]
    r0 = [acc.v(p0 + d * r) for d in dirs]
    r1 = [acc.v(p1 + d * r) for d in dirs]
    for k in range(n):
        k1 = (k + 1) % n
        dm = (dirs[k] + dirs[k1]).normalized()
        if skip_up and dm.z > 0.75:
            continue
        uv = [uvf(k / n, 0, L), uvf((k + 1) / n, 0, L), uvf((k + 1) / n, L, L), uvf(k / n, L, L)]
        acc.f([r0[k], r0[k1], r1[k1], r1[k]], uv, True, tuple(dm), mat)
    for cap, ring, sgn in ((cap0, r0, -1), (cap1, r1, 1)):
        if cap:
            ids = list(ring)
            acc.f(ids, [uvf(0.5, 0, L)] * len(ids), False, tuple(ax * sgn), mat)


def tube_path(acc, path, r, n, uvf, mat=0):
    """Tube along a 3D polyline (parallel-transport frames). uvf(frac_around, along_mm, total)."""
    P = [Vector(p) for p in path]
    T = []
    for i in range(len(P)):
        a = P[min(i + 1, len(P) - 1)] - P[max(i - 1, 0)]
        T.append(a.normalized())
    up = Vector((0, 0, 1))
    N0 = T[0].cross(up)
    if N0.length < 1e-6:
        N0 = T[0].cross(Vector((1, 0, 0)))
    N0.normalize()
    frames = []
    Ncur = N0
    for i in range(len(P)):
        if i > 0:
            Ncur = (Ncur - T[i] * Ncur.dot(T[i])).normalized()
        frames.append((Ncur, T[i].cross(Ncur)))
    s = [0.0]
    for i in range(1, len(P)):
        s.append(s[-1] + (P[i] - P[i - 1]).length)
    rings = []
    for i in range(len(P)):
        Nn, B = frames[i]
        rings.append([acc.v(P[i] + (Nn * math.cos(2 * math.pi * k / n) + B * math.sin(2 * math.pi * k / n)) * r) for k in range(n)])
    for i in range(len(P) - 1):
        for k in range(n):
            k1 = (k + 1) % n
            mid = (Vector(acc.V[rings[i][k]]) + Vector(acc.V[rings[i][k1]]) + Vector(acc.V[rings[i + 1][k]]) + Vector(acc.V[rings[i + 1][k1]])) / 4
            cen = (P[i] + P[i + 1]) / 2
            want = tuple(mid - cen)
            uv = [uvf(k / n, s[i], s[-1]), uvf((k + 1) / n, s[i], s[-1]), uvf((k + 1) / n, s[i + 1], s[-1]), uvf(k / n, s[i + 1], s[-1])]
            acc.f([rings[i][k], rings[i][k1], rings[i + 1][k1], rings[i + 1][k]], uv, True, want, mat)


def sweep_line(acc, a, b, prof, uvf, mat=0):
    """Sweep a (w, z) profile along the 2D segment a -> b (w = offset to the right of a->b).
    Visible side n = (dz, -dw) with w right. uvf(along_frac, t, t_total)."""
    a, b = Vector((a[0], a[1])), Vector((b[0], b[1]))
    tg = (b - a).normalized()
    rt = Vector((tg.y, -tg.x))
    pts = [p for p, _ in prof]
    sm = [s for _, s in prof]
    t = [0.0]
    for i in range(1, len(pts)):
        t.append(t[-1] + math.hypot(pts[i][0] - pts[i - 1][0], pts[i][1] - pts[i - 1][1]))
    tt = t[-1]
    rows = []
    for (w, z) in pts:
        pa, pb = a + rt * w, b + rt * w
        rows.append((acc.v((pa.x, pa.y, z)), acc.v((pb.x, pb.y, z))))
    for i in range(len(pts) - 1):
        dw = pts[i + 1][0] - pts[i][0]
        dz = pts[i + 1][1] - pts[i][1]
        want = (rt.x * dz, rt.y * dz, -dw)
        ia0, ib0 = rows[i]
        ia1, ib1 = rows[i + 1]
        uv = [uvf(0, t[i], tt), uvf(1, t[i], tt), uvf(1, t[i + 1], tt), uvf(0, t[i + 1], tt)]
        acc.f([ia0, ib0, ib1, ia1], uv, sm[i + 1], want, mat)


# --------------------------------------------------------------------------- atlas packing
class Atlas:
    """Lens-material atlas. Regions in px, origin bottom-left (UV space)."""

    def __init__(self, tw, th, gutter=6):
        self.tw, self.th, self.g = tw, th, gutter
        self.regions = {}

    def put(self, name, x, y, w, h, **meta):
        r = dict(x=x, y=y, w=w, h=h)
        r.update(meta)
        self.regions[name] = r
        return r

    def pack_columns(self, x0, x1, items):
        """items: [(name, w, h, meta)] stacked in columns of equal width, tallest first."""
        cols = []
        for name, w, h, meta in sorted(items, key=lambda it: -it[2]):
            placed = False
            for c in cols:
                if c["w"] == w and c["y"] + h + self.g <= self.th:
                    self.put(name, c["x"], c["y"], w, h, **meta)
                    c["y"] += h + self.g
                    placed = True
                    break
            if not placed:
                x = x0 if not cols else cols[-1]["x"] + cols[-1]["w"] + self.g
                assert x + w <= x1, "atlas overflow at %s" % name
                assert h <= self.th, "region too tall %s" % name
                self.put(name, x, 0, w, h, **meta)
                cols.append(dict(x=x, w=w, y=h + self.g))
        return (cols[-1]["x"] + cols[-1]["w"]) if cols else x0

    def uv(self, name, a, b):
        """a, b in 0..1 within the region (a along region width, b along height)."""
        r = self.regions[name]
        return ((r["x"] + 0.5 + a * (r["w"] - 1)) / self.tw, (r["y"] + 0.5 + b * (r["h"] - 1)) / self.th)

    def json(self):
        return dict(size=[self.tw, self.th], gutter=self.g, regions=self.regions)


SWATCH = 40


def add_swatches(atlas, names, x):
    y = atlas.th - SWATCH
    for n in names:
        atlas.put("sw_" + n, x, y, SWATCH, SWATCH, kind="swatch", finish=n)
        y -= SWATCH + atlas.g
    return x + SWATCH


def sw(atlas, finish):
    return lambda *a: atlas.uv("sw_" + finish, 0.5, 0.5)


# --------------------------------------------------------------------------- option specs
# Lensed family: module (mx, my) at T-bar centrelines. Rings inward from the T-bar inner edge.
T_BAR = 24.0
SKIRT = 45.0     # outer skirt height: covers the no-hole hang (max 41 mm, L02)

LENSED = {
    # door: finish, width; ret: kind 'slope' (height, inset, lip) or 'regress' / 'vertical'
    "L01": dict(mod=(600, 1200), door=("enamel", 13.5), reveal=0, ret=("slope", 20.0, 10.0, 2.0), tubes=("straight", [-195, -65, 65, 195], 70), hinge=(40, 8, 30, 12)),
    "L02": dict(mod=(600, 1200), door=("alu", 10.0), reveal=8, ret=("regress", 38.0, 5.0, 4.0), tubes=("straight", [-195, -65, 65, 195], 90), hinge=(40, 8, 30, 12)),
    "L03": dict(mod=(600, 1200), door=("enamel", 12.0), reveal=0, ret=("vertical", 20.0, 0.0, 0.0), tubes=("straight", [-180, -60, 60, 180], 85), hinge=(40, 8, 30, 12), channel=(8.0, 13.0, 6.0)),
    "L04": dict(mod=(600, 1200), door=("enamel", 13.5), reveal=0, ret=("slope", 20.0, 10.0, 2.0), tubes=("straight", [-195, 195], 70), empty=[-65, 65], hinge=(40, 8, 30, 12)),
    "L05": dict(mod=(600, 1200), door=("enamel", 13.5), reveal=0, ret=("slope", 20.0, 10.0, 2.0), tubes=("straight", [-195, -65, 65, 195], 70), hinge=(40, 8, 30, 12)),
    "L08": dict(mod=(600, 600), door=("enamel", 13.5), reveal=0, ret=("slope", 20.0, 10.0, 2.0), tubes=("u", [-228, -76, 76, 228], 70), hinge=(40, 8, 30, 12)),
    "L09": dict(mod=(300, 1200), door=("enamel", 13.5), reveal=0, ret=("slope", 20.0, 10.0, 2.0), tubes=("straight", [-55, 55], 70), hinge=(30, 6, 24, 10)),
}
LOUVER = {
    "L06": dict(mod=(600, 1200), longs=2, crosses=5, depth=76.0, blade_top=18.0, tubes=("straight", [-181.33, 0.0, 181.33], 95)),
    "L07": dict(mod=(600, 600), longs=2, crosses=2, depth=76.0, blade_top=18.0, tubes=("u", [-228, -76, 76, 228], 95)),
}
CAN = {"L10": dict(cut=178.0, trim=13.0, opening=152.0, baffle_top_d=140.0, baffle_h=120.0, cup_top=130.0, face_d=120.0)}

LOD_SEGS = {0: 4, 1: 2, 2: 0}


# --------------------------------------------------------------------------- shared housing parts
def tbar_ring(acc, hxt, hyt, lod, inner_up=3.0):
    """T-bar ring: inner edge at (hxt, hyt), 24 wide face 1 mm below the print, outer skirt."""
    segs = LOD_SEGS[lod]
    if lod == 2:
        prof = plain([(0, -1.0), (T_BAR, -1.0), (T_BAR, SKIRT)])
    else:
        pts = ([(0, inner_up)] if inner_up > -0.3 else []) + [(0, -1.0), (T_BAR, -1.0), (T_BAR, SKIRT)]
        rad = ([0] if inner_up > -0.3 else []) + [0.9, 0.9, 0]
        if inner_up <= -0.3:
            pts = [(0.0, -0.1)] + pts
            rad = [0] + rad
        prof = fillet(pts, rad, segs)
    sweep_rect(acc, hxt, hyt, prof, trim_mapper("tbar"))


def door_ring(acc, hx, hy, width, finish, lod, wall_top=10.0, wall_finish=None, z0=1.0):
    """Formed door frame: inner edge (hx, hy) rolls up to z0 (joins the lens return), 'width' face
    at z = 0, outer fold up to wall_top."""
    segs = LOD_SEGS[lod]
    if lod == 2:
        sweep_rect(acc, hx, hy, plain([(0, 0.0), (width, 0.0)]), trim_mapper(finish))
        return
    face = fillet([(0, z0), (0, 0.0), (width, 0.0), (width, 2.2)], [0, 1.2, 1.8, 0], segs)
    sweep_rect(acc, hx, hy, face, trim_mapper(finish))
    sweep_rect(acc, hx, hy, plain([(width, 2.2), (width, wall_top)]), trim_mapper(wall_finish or finish))


def mitre_seams(acc, hx, hy, width):
    for sx, sy in ((1, 1), (-1, 1), (-1, -1), (1, -1)):
        a = (sx * hx, sy * hy)
        b = (sx * (hx + width), sy * (hy + width))
        w = 0.35
        n = Vector((b[0] - a[0], b[1] - a[1])).normalized()
        p = Vector((-n.y, n.x)) * w
        pts = [(a[0] - p.x, a[1] - p.y, -0.12), (b[0] - p.x, b[1] - p.y, -0.12), (b[0] + p.x, b[1] + p.y, -0.12), (a[0] + p.x, a[1] + p.y, -0.12)]
        quad(acc, pts, lambda q: trim_uv("black", q[0] + q[1], 50, 100), (0, 0, -1))


def hinge(acc, x_edge, sgn, y, size, lod):
    """Hinge on the door's outer edge x_edge (sgn = side): leaf on the face + knuckle in the crease."""
    L, W = size
    xc = x_edge - sgn * (W / 2 + 0.6)
    box(acc, (xc, y, -0.4), (W, L, 0.8), box_trim("zinc"))
    if lod == 0:
        cylinder(acc, (x_edge, y - L / 2, 1.0), (x_edge, y + L / 2, 1.0), 2.0, 8,
                 lambda a, s, l: trim_uv("zinc", s + 200, a * 12.6, 12.6), skip_up=True)
        for dy in (-L * 0.28, L * 0.28):
            rivet(acc, xc, y + dy, -0.8, 1.6, "zinc")


def latch(acc, x_edge, sgn, y, size, lod):
    L, W = size
    xc = x_edge - sgn * (W / 2 + 1.2)
    if lod == 0:
        # rounded plate: octagon prism 0.9 mm proud of the door face
        r = [(W / 2, L / 2 - 3), (W / 2 - 3, L / 2), (-W / 2 + 3, L / 2), (-W / 2, L / 2 - 3),
             (-W / 2, -L / 2 + 3), (-W / 2 + 3, -L / 2), (W / 2 - 3, -L / 2), (W / 2, -L / 2 + 3)]
        bot = [acc.v((xc + px, y + py, -0.9)) for px, py in r]
        top = [acc.v((xc + px, y + py, 0.2)) for px, py in r]
        uvb = box_trim("enamel")
        acc.f(bot, [uvb(acc.V[i], (0, 0, -1)) for i in bot], False, (0, 0, -1))
        for k in range(8):
            k1 = (k + 1) % 8
            mid = ((r[k][0] + r[k1][0]) / 2, (r[k][1] + r[k1][1]) / 2, 0)
            ids = [bot[k], bot[k1], top[k1], top[k]]
            acc.f(ids, [uvb(acc.V[i], mid) for i in ids], True, mid)
        # the latch slot (dark) and the thumb tab
        quad(acc, [(xc - 1.2, y - 9, -0.95), (xc + 1.2, y - 9, -0.95), (xc + 1.2, y + 9, -0.95), (xc - 1.2, y + 9, -0.95)],
             lambda q: trim_uv("black", q[1], 40, 80), (0, 0, -1))
        box(acc, (xc - sgn * 1.5, y + L / 2 - 4, -1.6), (5, 4, 1.4), box_trim("zinc"))
    else:
        box(acc, (xc, y, -0.45), (W, L, 0.9), box_trim("enamel"))
        quad(acc, [(xc - 1.2, y - 9, -0.95), (xc + 1.2, y - 9, -0.95), (xc + 1.2, y + 9, -0.95), (xc - 1.2, y + 9, -0.95)],
             lambda q: trim_uv("black", q[1], 40, 80), (0, 0, -1))


def rivet(acc, x, y, z, r, finish):
    n = 6
    ring = [acc.v((x + r * math.cos(2 * math.pi * k / n), y + r * math.sin(2 * math.pi * k / n), z + 0.3)) for k in range(n)]
    tip = acc.v((x, y, z - 0.5))
    uvf = box_trim(finish)
    for k in range(n):
        ids = [ring[k], ring[(k + 1) % n], tip]
        acc.f(ids, [uvf(acc.V[i], (0, 0, -1)) for i in ids], True, (0, 0, -1))


def filler_ring(acc, hx_in, hy_in, hx_out, hy_out, z):
    """Black closure above the T-bar / door crease so nothing shows through."""
    prof = plain([(0, z), (hx_out - hx_in, z)])
    sweep_rect(acc, hx_in, hy_in, prof, trim_mapper("black"))


def lampholder(acc, x, y_end, sgn, z, lod, u_bend=False):
    """T12 lampholder on an end wall (y_end, facing -sgn*y): body + pin face."""
    if lod == 2:
        return
    w, d, h = 26.0, 18.0, 34.0
    yc = y_end - sgn * d / 2
    box(acc, (x, yc, z + 4), (w, d, h), box_trim("holder"), skip=("+z", "+y" if sgn > 0 else "-y"))
    if lod == 0:
        # the slot the pins slide into: a dark strip on the face toward the lamp
        yf = y_end - sgn * (d + 0.05)
        quad(acc, [(x - 2, yf, z - 9), (x + 2, yf, z - 9), (x + 2, yf, z + 4), (x - 2, yf, z + 4)],
             lambda q: trim_uv("black", q[0] + 300, 60, 100), (0, -sgn, 0))


# --------------------------------------------------------------------------- lensed family
def lensed_rings(spec):
    mx, my = spec["mod"]
    hxt, hyt = mx / 2 - T_BAR / 2, my / 2 - T_BAR / 2         # T-bar inner edge
    finish, dw = spec["door"]
    rv = spec["reveal"]
    if "channel" in spec:
        fl, ch, eg = spec["channel"]
        hx_do, hy_do = hxt - fl - ch, hyt - fl - eg             # door outer edge
    else:
        hx_do, hy_do = hxt - rv - (0.5 if rv == 0 else 0.0), hyt - rv - (0.5 if rv == 0 else 0.0)
    hx_di, hy_di = hx_do - dw, hy_do - dw                       # door inner edge
    kind, h, inset, lip = spec["ret"]
    if kind == "slope":
        hx_l, hy_l, zl = hx_di - inset - lip, hy_di - inset - lip, h + 0.5
    elif kind == "regress":
        dr = h * math.tan(math.radians(inset))
        hx_l, hy_l, zl = hx_di - dr - lip, hy_di - dr - lip, h + 0.5
    else:
        hx_l, hy_l, zl = hx_di, hy_di, h + 0.5
    return dict(hxt=hxt, hyt=hyt, hx_do=hx_do, hy_do=hy_do, hx_di=hx_di, hy_di=hy_di, hx_l=hx_l, hy_l=hy_l, zl=zl, dw=dw)


def ret_profile(spec, R):
    """Lens-side profile from the lens edge out to the door's inner roll (d from the door inner edge)."""
    kind, h, inset, lip = spec["ret"]
    if kind == "slope":
        return [(-(inset + lip), h + 0.5), (-(inset + lip), h), (-inset, h), (0.0, 1.0)]
    if kind == "regress":
        dr = h * math.tan(math.radians(inset))
        return [(-(dr + lip), h + 0.5), (-(dr + lip), h), (-dr, h), (0.0, 0.8)]
    return [(0.0, h + 0.5), (0.0, 1.0)]


def lensed_atlas(oid, spec, R):
    lw, ll = 2 * R["hx_l"], 2 * R["hy_l"]
    sq = abs(spec["mod"][0] - spec["mod"][1]) < 1
    tw, th = (512, 2048) if spec["mod"][0] < 400 else ((1024, 1024) if sq else (1024, 2048))
    at = Atlas(tw, th)
    prof = ret_profile(spec, R)
    tlen = sum(math.hypot(prof[i + 1][0] - prof[i][0], prof[i + 1][1] - prof[i][1]) for i in range(len(prof) - 1))
    rw = int(max(28, min(96, round(tlen * 2.0))))
    long_len = 2 * (R["hy_di"] + 0)
    short_len = 2 * (R["hx_di"] + 0)
    mx = max(long_len, short_len)
    items = []
    for s in range(4):
        L = long_len if s in (0, 2) else short_len
        items.append(("ret%d" % s, rw, int(round((th - 2) * L / mx)), dict(kind="ret", side=s, length=L, t_total=tlen, rot=True)))
    # right part: return strips + swatch column; lens takes the rest
    need_cols = 2 if sq is False else 4
    if not sq and items[1][2] * 2 + at.g <= th:
        need_cols = 3
    right_w = need_cols * (rw + at.g) + SWATCH + at.g
    lens_w = tw - right_w - at.g
    at.put("lens", 0, 0, lens_w, th, kind="lens", W=lw, L=ll)
    x = at.pack_columns(lens_w + at.g, tw - SWATCH - at.g, items)
    add_swatches(at, ["tbar", "door", "black", "enamel"], tw - SWATCH)
    at.meta = dict(px_per_mm_lens=[lens_w / lw, th / ll], ret_profile_mm=tlen)
    return at


def lensed_lens(oid, spec, R, at, sag=0.0):
    acc = Acc()
    prof = ret_profile(spec, R)
    pts = [(p, i > 0 and False) for i, p in enumerate(prof)]
    def uvf(side, along, t, tt):
        L = at.regions["ret%d" % side]["length"]
        return at.uv("ret%d" % side, t / tt, (along + L / 2) / L)
    sweep_rect(acc, R["hx_di"], R["hy_di"], pts, uvf)
    # lens plate (bottom face), optionally drooped (L05 sag)
    hx, hy, z = R["hx_l"], R["hy_l"], R["zl"]
    nx, ny = (8, 16) if sag > 0 else (1, 1)
    grid = {}
    for i in range(nx + 1):
        for j in range(ny + 1):
            x = -hx + 2 * hx * i / nx
            y = -hy + 2 * hy * j / ny
            dz = sag * (1 - (x / hx) ** 2) * (1 - (y / hy) ** 2)
            grid[i, j] = acc.v((x, y, z - dz))
    for i in range(nx):
        for j in range(ny):
            ids = [grid[i, j], grid[i + 1, j], grid[i + 1, j + 1], grid[i, j + 1]]
            uvs = [at.uv("lens", (acc.V[k][0] + hx) / (2 * hx), (acc.V[k][1] + hy) / (2 * hy)) for k in ids]
            acc.f(ids, uvs, sag > 0, (0, 0, -1))
    return acc


def lensed_housing(oid, spec, R, lod, door_finish=None):
    acc = Acc()
    rv = spec["reveal"]
    df = door_finish or spec["door"][0]
    if "channel" in spec:
        fl, ch, eg = spec["channel"]
        tbar_ring(acc, R["hxt"], R["hyt"], lod, inner_up=0.5)
        # outer flange 8 wide (white steel) and the dark channel / end gaps
        hxf, hyf = R["hxt"] - fl, R["hyt"] - fl
        if lod == 2:
            sweep_rect(acc, hxf, hyf, plain([(0, 0.0), (fl, 0.0)]), trim_mapper("enamel"))
        else:
            sweep_rect(acc, hxf, hyf, fillet([(0, 2.0), (0, 0.0), (fl, 0.0)], [0, 0.8, 0], LOD_SEGS[lod]), trim_mapper("enamel"))
            sweep_rect(acc, hxf, hyf, plain([(0, 20.0), (0, 2.0)]), trim_mapper("black"))
        # channel tops: light trap V on the long sides, flat at the end gaps
        hxd, hyd = R["hx_do"], R["hy_do"]
        for sx in (1, -1):
            xm = sx * (hxf + hxd) / 2
            apex = 14.0 if lod < 2 else 20.0
            for xa, xb in ((sx * hxf, xm), (xm, sx * hxd)):
                za = 20.0 if abs(abs(xa) - (hxf if abs(xa) > abs(xm) else hxd)) < 1e-6 else apex
                zb = 20.0 if abs(abs(xb) - (hxf if abs(xb) > abs(xm) else hxd)) < 1e-6 else apex
                pts = [(xa, -hyd, za), (xb, -hyd, zb), (xb, hyd, zb), (xa, hyd, za)]
                quad(acc, pts, lambda q: trim_uv("black", q[1], abs(q[0]) - hxd, hxf - hxd), (0, 0, -1))
        for sy in (1, -1):
            pts = [(-hxf, sy * hyd, 20.0), (hxf, sy * hyd, 20.0), (hxf, sy * hyf, 20.0), (-hxf, sy * hyf, 20.0)]
            quad(acc, pts, lambda q: trim_uv("black", q[0], abs(q[1]) - hyd, hyf - hyd), (0, 0, -1))
        door_ring(acc, R["hx_di"], R["hy_di"], R["dw"], df, lod, wall_top=20.0, wall_finish="black")
    elif rv > 0:
        tbar_ring(acc, R["hxt"], R["hyt"], lod, inner_up=-1.0)
        # black reveal: T-bar side wall, top, door side wall
        sweep_rect(acc, R["hxt"], R["hyt"], plain([(0, rv), (0, -0.2)]), trim_mapper("black"))
        sweep_rect(acc, R["hx_do"], R["hy_do"], plain([(0, rv), (rv, rv)]), trim_mapper("black"))
        door_ring(acc, R["hx_di"], R["hy_di"], R["dw"], df, lod, wall_top=rv, wall_finish="black", z0=0.8)
    else:
        tbar_ring(acc, R["hxt"], R["hyt"], lod)
        door_ring(acc, R["hx_di"], R["hy_di"], R["dw"], df, lod)
        if lod < 2:
            filler_ring(acc, R["hx_do"], R["hy_do"], R["hxt"] + T_BAR, R["hyt"] + T_BAR, 10.0)
    if lod == 0:
        mitre_seams(acc, R["hx_di"], R["hy_di"], R["dw"])
    if lod < 2:
        hl, hw, ll, lw = spec["hinge"]
        sq = abs(spec["mod"][0] - spec["mod"][1]) < 1
        ys = [-R["hy_di"] * (0.55 if sq else 0.72), R["hy_di"] * (0.55 if sq else 0.72)]
        for y in ys:
            hinge(acc, R["hx_do"], 1, y, (hl, hw), lod)
            latch(acc, -R["hx_do"], -1, y, (ll, lw), lod)
    return acc


def lensed_phone(oid, spec, R, at):
    """One mesh on the lens material: T-bar + door in swatches (no emission), return, lens, skirt."""
    acc = Acc()
    s_t, s_d, s_b = sw(at, "tbar"), sw(at, "door"), sw(at, "black")
    sweep_rect(acc, R["hxt"], R["hyt"], plain([(0, -1.0), (T_BAR, -1.0), (T_BAR, SKIRT)]), s_t)
    if "channel" in spec:
        fl, ch, eg = spec["channel"]
        hxf, hyf = R["hxt"] - fl, R["hyt"] - fl
        sweep_rect(acc, hxf, hyf, plain([(0, 20.0), (0, 0.0), (fl, 0.0)]), lambda *a: at.uv("sw_door", .5, .5) if a[2] > 19 else at.uv("sw_black", .5, .5))
        sweep_rect(acc, R["hx_do"], R["hy_do"], plain([(0, 20.0), (hxf - R["hx_do"], 20.0)]), s_b)
        sweep_rect(acc, R["hx_di"], R["hy_di"], plain([(0, 0.0), (R["dw"], 0.0), (R["dw"], 20.0)]), lambda *a: at.uv("sw_door", .5, .5) if a[2] < R["dw"] + 0.1 else at.uv("sw_black", .5, .5))
    elif spec["reveal"] > 0:
        rv = spec["reveal"]
        sweep_rect(acc, R["hx_do"], R["hy_do"], plain([(0, rv), (rv, rv), (rv, -0.9)]), s_b)
        sweep_rect(acc, R["hx_di"], R["hy_di"], plain([(0, 0.0), (R["dw"], 0.0), (R["dw"], rv)]), lambda *a: at.uv("sw_door", .5, .5) if a[2] < R["dw"] + 0.1 else at.uv("sw_black", .5, .5))
    else:
        sweep_rect(acc, R["hx_di"], R["hy_di"], plain([(0, 0.0), (R["dw"], 0.0), (R["dw"] + 0.5, -1.0)]), s_d)
    prof = ret_profile(spec, R)
    keep = [prof[0], prof[-1]] if len(prof) > 2 else prof
    def uvf(side, along, t, tt):
        L = at.regions["ret%d" % side]["length"]
        return at.uv("ret%d" % side, t / tt, (along + L / 2) / L)
    sweep_rect(acc, R["hx_di"], R["hy_di"], plain(keep), uvf)
    hx, hy, z = R["hx_l"], R["hy_l"], R["zl"]
    quad(acc, [(-hx, -hy, z), (hx, -hy, z), (hx, hy, z), (-hx, hy, z)],
         lambda p: at.uv("lens", (p[0] + hx) / (2 * hx), (p[1] + hy) / (2 * hy)), (0, 0, -1))
    return acc


# --------------------------------------------------------------------------- L05 door-open variant
def open_atlas():
    at = Atlas(1024, 1024)
    at.put("lens_hang", 0, 0, 280, 600, kind="lens_hang", W=524.0, L=1124.0)
    at.put("cav_long", 286, 0, 120, 600, kind="cav_long", rot=True)
    at.put("cav_end", 412, 0, 260, 60, kind="cav_end")
    at.put("top", 412, 66, 240, 534, kind="cav_top")
    at.put("ballast", 658, 66, 64, 534, kind="ballast", rot=True)
    at.put("tube", 728, 0, 48, 600, kind="tube", rot=True)
    at.put("ret_hang", 782, 0, 40, 600, kind="ret_hang", rot=True)
    at.put("glass_back", 828, 0, 150, 300, kind="lens_back")
    add_swatches(at, ["tbar", "door", "black"], 1024 - SWATCH)
    return at


def l05_open(spec, R, at):
    """Door hanging open on its hinges (+X long side), cavity, ballast cover, four real tubes."""
    lens = Acc()
    # cavity: sloped long walls, vertical end walls, top, ballast cover
    hxo, hyo = R["hxt"] - 1.0, R["hyt"] - 1.0
    hxt_top, ztop = 232.0, 120.0
    for sx in (1, -1):
        pts = [(sx * hxo, -hyo, 6.0), (sx * hxo, hyo, 6.0), (sx * hxt_top, hyo, ztop), (sx * hxt_top, -hyo, ztop)]
        quad(lens, pts, lambda q, sx=sx: at.uv("cav_long", (q[2] - 6.0) / (ztop - 6.0), (q[1] + hyo) / (2 * hyo)), (-sx * 0.9, 0, -0.45))
    for sy in (1, -1):
        pts = [(-hxo, sy * hyo, 6.0), (hxo, sy * hyo, 6.0), (hxt_top, sy * hyo, ztop), (-hxt_top, sy * hyo, ztop)]
        quad(lens, pts, lambda q: at.uv("cav_end", (q[0] + hxo) / (2 * hxo), (q[2] - 6.0) / (ztop - 6.0)), (0, -sy, 0))
    for xa, xb in ((-hxt_top, -45.0), (45.0, hxt_top)):
        quad(lens, [(xa, -hyo, ztop), (xb, -hyo, ztop), (xb, hyo, ztop), (xa, hyo, ztop)],
             lambda q: at.uv("top", (q[0] + hxt_top) / (2 * hxt_top), (q[1] + hyo) / (2 * hyo)), (0, 0, -1))
    # ballast cover: a channel hanging 10 mm below the top between the inner tubes
    for pts, n in (([(-45, -hyo, 110), (45, -hyo, 110), (45, hyo, 110), (-45, hyo, 110)], (0, 0, -1)),
                   ([(-45, -hyo, 110), (-45, hyo, 110), (-45, hyo, ztop), (-45, -hyo, ztop)], (-1, 0, 0)),
                   ([(45, -hyo, 110), (45, -hyo, ztop), (45, hyo, ztop), (45, hyo, 110)], (1, 0, 0))):
        quad(lens, pts, lambda q: at.uv("ballast", (q[0] + 45) / 90 * 0.8 + (0.2 if q[2] > 111 else 0.0), (q[1] + hyo) / (2 * hyo)), n)
    zt = spec["tubes"][2]
    for x in spec["tubes"][1]:
        cylinder(lens, (x, -hyo + 22, zt), (x, hyo - 22, zt), 19.0, 12,
                 lambda a, s, L: at.uv("tube", a, s / L))
    # the door: frame (housing) + return + lens (lens mesh, both faces), rotated about the hinge
    door_lens = Acc()
    prof = ret_profile(spec, R)
    sweep_rect(door_lens, R["hx_di"], R["hy_di"], plain(prof),
               lambda side, along, t, tt: at.uv("ret_hang", t / tt, (along + 600) / 1200))
    hx, hy, z = R["hx_l"], R["hy_l"], R["zl"]
    quad(door_lens, [(-hx, -hy, z), (hx, -hy, z), (hx, hy, z), (-hx, hy, z)],
         lambda p: at.uv("lens_hang", (p[0] + hx) / (2 * hx), (p[1] + hy) / (2 * hy)), (0, 0, -1))
    quad(door_lens, [(-hx, -hy, z + 3.2), (hx, -hy, z + 3.2), (hx, hy, z + 3.2), (-hx, hy, z + 3.2)],
         lambda p: at.uv("glass_back", (p[0] + hx) / (2 * hx), (p[1] + hy) / (2 * hy)), (0, 0, 1))
    hx_axis, hz_axis, ang = R["hx_do"], 1.0, math.radians(-84.0)
    def rot(p):
        x, y, z = p[0] - hx_axis, p[1], p[2] - hz_axis
        c, s = math.cos(ang), math.sin(ang)
        return (x * c + z * s + hx_axis, y, -x * s + z * c + hz_axis)
    lens.add(door_lens.transformed(rot))
    housings = {}
    for lod in (0, 1, 2):
        h = Acc()
        tbar_ring(h, R["hxt"], R["hyt"], lod)
        # the housing's own flange inside the T-bar (seen now that the door is down)
        sweep_rect(h, hxo, hyo, plain([(0, 6.0), (0, 0.0), (1.0, 0.0)]), trim_mapper("enamel"))
        d = Acc()
        door_ring(d, R["hx_di"], R["hy_di"], R["dw"], "enamel", lod)
        if lod < 2:
            hl, hw, ll, lw = spec["hinge"]
            for y in (-R["hy_di"] * 0.72, R["hy_di"] * 0.72):
                hinge(d, R["hx_do"], 1, y, (hl, hw), lod)
                latch(d, -R["hx_do"], -1, y, (ll, lw), lod)
        h.add(d.transformed(rot))
        if lod < 2:
            for x in spec["tubes"][1]:
                for sy in (1, -1):
                    lampholder(h, x, sy * hyo, sy, zt, lod)
        housings[lod] = h
    return lens, housings


# --------------------------------------------------------------------------- louver family
def louver_atlas(oid, spec):
    mx, my = spec["mod"]
    sq = abs(mx - my) < 1
    tw, th = (1024, 1024) if sq else (1024, 2048)
    at = Atlas(tw, th)
    fx, fy = mx / 2 - T_BAR / 2 - 16, my / 2 - T_BAR / 2 - 16      # louver field half sizes
    refl_w = 360 if not sq else 420
    at.put("refl", 0, 0, refl_w, th, kind="refl", W=2 * fx, L=2 * fy)
    x = refl_w + at.g
    at.put("tube", x, 0, 48, th, kind="tube", rot=True)
    x += 48 + at.g
    at.put("cav_long", x, 0, 40, th, kind="cav_long", rot=True)
    x += 40 + at.g
    bh = (th - 3 * at.g) // 4
    for i, n in enumerate(["blade_long", "blade_cross", "perim_long", "perim_short"]):
        at.put(n, x, i * (bh + at.g), 256, bh, kind="blade", which=n)
    x += 256 + at.g
    at.put("cav_end", x, 0, tw - x - SWATCH - at.g, 220, kind="cav_end")
    at.put("lip", x, 226, 64, 64, kind="lip")
    at.put("flatblade", x + 70, 226, 128, 128, kind="flatblade")
    add_swatches(at, ["tbar", "black", "rim_alu", "holder"], tw - SWATCH)
    at.meta = dict(field_mm=[2 * fx, 2 * fy])
    return at, fx, fy


def blade_profile(depth, top, segs):
    """Hollow parabolic blade: lip 3 mm at z = 0, faces flare to 'top' mm wide at 'depth'.
    Ordered left face top -> lip -> right face top (visible side outward/down)."""
    ws = []
    for k in range(segs, -1, -1):
        z = depth * k / segs
        ws.append((-(1.5 + (top / 2 - 1.5) * (z / depth) ** 2), z))
    right = [(-w, z) for (w, z) in reversed(ws)]
    pts = ws + right
    prof = []
    for i, p in enumerate(pts):
        lip = i == len(ws)          # the segment ending here crosses the lip: flat
        prof.append((p, not lip and i > 0))
    return prof


def louver_lens(oid, spec, at, fx, fy, segs=5):
    acc = Acc()
    D, top = spec["depth"], spec["blade_top"]
    prof = blade_profile(D, top, segs)
    tt = sum(math.hypot(prof[i + 1][0][0] - prof[i][0][0], prof[i + 1][0][1] - prof[i][0][1]) for i in range(len(prof) - 1))
    nl, nc = spec["longs"], spec["crosses"]
    xs = [-fx + 2 * fx * (k + 1) / (nl + 1) for k in range(nl)]
    ys = [-fy + 2 * fy * (k + 1) / (nc + 1) for k in range(nc)]
    for x in xs:
        sweep_line(acc, (x, -fy), (x, fy), prof, lambda a, t, T: at.uv("blade_long", a, t / T))
    for y in ys:
        sweep_line(acc, (-fx, y), (fx, y), [((p[0], p[1] + 0.25), s) for p, s in prof], lambda a, t, T: at.uv("blade_cross", a, t / T))
    # perimeter half-blades: from the rim edge (d = 0, z = 0) leaning in to the top
    per = []
    for k in range(segs, -1, -1):
        z = D * k / segs
        per.append(((-(top / 2 - 1.5) * (z / D) ** 2, z), k < segs))
    per = [(p, i > 0) for i, (p, _) in enumerate(per)]
    def puv(side, along, t, T):
        L = 2 * (fy if side in (0, 2) else fx)
        return at.uv("perim_long" if side in (0, 2) else "perim_short", (along + L / 2) / L, t / T)
    sweep_rect(acc, fx, fy, per, puv)
    # cavity: long walls 76 -> 100, reflector troughs, end walls
    zr0, zr1 = 100.0, 120.0
    sweep_rect(acc, fx, fy, plain([(-(top / 2 - 1.5), zr0), (-(top / 2 - 1.5), D)]),
               lambda side, along, t, T: at.uv("cav_long", t / T, (along + fy) / (2 * fy)) if side in (0, 2) else at.uv("cav_long", t / T, 0.5),
               sides=(0, 1, 2, 3))
    edge = fx - (top / 2 - 1.5)
    cols = []
    if spec["tubes"][0] == "straight":
        cw = 2 * edge / (nl + 1)
        cols = [(-edge + cw * (k + 0.5), cw / 2) for k in range(nl + 1)]
    else:
        cols = [(-edge / 2, edge / 2), (edge / 2, edge / 2)]
    for xc, hw in cols:
        pr = [(xc - hw, zr0), (xc - hw * 0.62, zr0 + 13), (xc - hw * 0.25, zr1), (xc + hw * 0.25, zr1), (xc + hw * 0.62, zr0 + 13), (xc + hw, zr0)]
        for i in range(len(pr) - 1):
            (xa, za), (xb, zb) = pr[i], pr[i + 1]
            pts = [(xa, -fy, za), (xb, -fy, zb), (xb, fy, zb), (xa, fy, za)]
            quad(acc, pts, lambda q: at.uv("refl", (q[0] + fx) / (2 * fx), (q[1] + fy) / (2 * fy)), (0, 0, -1), smooth=True)
        for sy in (1, -1):
            ids = [acc.v((xa, sy * fy, D)) for xa in (xc - hw, xc + hw)] + [acc.v((p[0], sy * fy, p[1])) for p in reversed(pr)]
            acc.f(ids, [at.uv("cav_end", (acc.V[i][0] + fx) / (2 * fx), (acc.V[i][2] - D) / (zr1 - D)) for i in ids], False, (0, -sy, 0))
    # real tubes
    kind, xs_t, zt = spec["tubes"]
    if kind == "straight":
        for x in xs_t:
            cylinder(acc, (x, -fy + 20, zt), (x, fy - 20, zt), 19.0, 12, lambda a, s, L: at.uv("tube", a, s / L))
    else:
        for xa, xb in ((xs_t[0], xs_t[1]), (xs_t[2], xs_t[3])):
            u_tube(acc, xa, xb, -fy + 20, fy - 22, zt, lambda a, s, L: at.uv("tube", a, s / L))
    return acc, cols


def u_tube(acc, xa, xb, y_socket, y_far, z, uvf, n=10, bend_segs=5):
    """U-bent T12: two legs from the socket end, a semicircular bend at the far end."""
    r_b = abs(xb - xa) / 2
    xc = (xa + xb) / 2
    yb = y_far - r_b
    path = [(xa, y_socket, z), (xa, yb, z)]
    for k in range(1, bend_segs):
        a = math.pi - math.pi * k / bend_segs
        path.append((xc + r_b * math.cos(a), yb + r_b * math.sin(a), z))
    path += [(xb, yb, z), (xb, y_socket, z)]
    tube_path(acc, path, 19.0, n, uvf)


def louver_housing(oid, spec, fx, fy, lod):
    acc = Acc()
    mx, my = spec["mod"]
    hxt, hyt = mx / 2 - T_BAR / 2, my / 2 - T_BAR / 2
    tbar_ring(acc, hxt, hyt, lod, inner_up=-1.0)
    # black reveal 8 wide, 10 deep
    sweep_rect(acc, hxt, hyt, plain([(0, 10.0), (0, -0.2)]), trim_mapper("black"))
    sweep_rect(acc, hxt - 8, hyt - 8, plain([(0, 10.0), (8, 10.0)]), trim_mapper("black"))
    # louver rim 8 wide, semi-specular aluminium, rolled outer edge
    if lod == 2:
        sweep_rect(acc, fx, fy, plain([(0, 0.0), (8, 0.0)]), trim_mapper("rim_alu"))
    else:
        sweep_rect(acc, fx, fy, fillet([(0, 0.0), (8, 0.0), (8, 10.0)], [0, 1.0, 0], LOD_SEGS[lod]),
                   lambda side, along, t, tt: trim_uv("rim_alu" if t < 8.5 else "black", along, t, tt, side))
    if lod < 2:
        kind, xs_t, zt = spec["tubes"]
        if kind == "straight":
            for x in xs_t:
                for sy in (1, -1):
                    lampholder(acc, x, sy * fy, sy, zt, lod)
        else:
            for x in xs_t:
                lampholder(acc, x, -fy, -1, zt, lod)
            if lod == 0:
                for xa, xb in ((xs_t[0], xs_t[1]), (xs_t[2], xs_t[3])):
                    box(acc, ((xa + xb) / 2, fy - 6, zt + 12), (40, 8, 14), box_trim("zinc"), skip=("+z", "+y"))
    return acc


def louver_phone(oid, spec, at, fx, fy):
    acc = Acc()
    mx, my = spec["mod"]
    hxt, hyt = mx / 2 - T_BAR / 2, my / 2 - T_BAR / 2
    sweep_rect(acc, hxt, hyt, plain([(0, 10.0), (0, -1.0), (T_BAR, -1.0), (T_BAR, SKIRT)]),
               lambda s, a, t, tt: at.uv("sw_black", .5, .5) if t < 10.5 else at.uv("sw_tbar", .5, .5))
    sweep_rect(acc, fx, fy, plain([(0, 0.0), (8, 0.0)]), sw(at, "rim_alu"))
    D = spec["depth"]
    sweep_rect(acc, fx, fy, plain([(0, D), (0, 0.0)]), lambda s, a, t, tt: at.uv("flatblade", 0.5, 1 - t / tt))
    nl, nc = spec["longs"], spec["crosses"]
    for k in range(nl):
        x = -fx + 2 * fx * (k + 1) / (nl + 1)
        for sx in (1, -1):
            quad(acc, [(x, -fy, 0), (x, fy, 0), (x, fy, D), (x, -fy, D)], lambda p: at.uv("flatblade", 0.5, p[2] / D), (sx, 0, 0))
    for k in range(nc):
        y = -fy + 2 * fy * (k + 1) / (nc + 1)
        for sy in (1, -1):
            quad(acc, [(-fx, y, 0), (fx, y, 0), (fx, y, D), (-fx, y, D)], lambda p: at.uv("flatblade", 0.5, p[2] / D), (0, sy, 0))
    quad(acc, [(-fx, -fy, D), (fx, -fy, D), (fx, fy, D), (-fx, fy, D)],
         lambda p: at.uv("refl", (p[0] + fx) / (2 * fx), (p[1] + fy) / (2 * fy)), (0, 0, -1))
    return acc


# --------------------------------------------------------------------------- can family
def can_atlas():
    at = Atlas(1024, 1024)
    at.put("baffle", 0, 0, 1024, 360, kind="baffle")
    at.put("cup", 0, 366, 1024, 90, kind="cup")
    at.put("face", 0, 462, 420, 420, kind="face")
    at.put("gap", 426, 462, 256, 64, kind="gap")
    add_swatches(at, ["trim", "black"], 1024 - SWATCH)
    return at


def can_lens(spec, at, n=24):
    acc = Acc()
    r0 = spec["opening"] / 2
    rt = spec["baffle_top_d"] / 2
    H = spec["baffle_h"]
    # baffle: ribbed cone (ribs in the maps at 6 mm pitch), seen from below
    sweep_round(acc, r0, plain([(rt - r0, H), (0.0, 0.0)]), n, lambda a, t, T: at.uv("baffle", a, 1 - t / T))
    # reflector cup band and the dark gap round the lamp face
    zc = spec["cup_top"]
    rf = spec["face_d"] / 2
    sweep_round(acc, rt, plain([(rf + 3 - rt, zc), (0.0, H)]), n, lambda a, t, T: at.uv("cup", a, 1 - t / T))
    sweep_round(acc, rf, plain([(0.0, zc - 0.5), (3.0, zc)]), n, lambda a, t, T: at.uv("gap", a, t / T))
    # 75 W reflector lamp face, domed 5 mm toward the room
    disk(acc, 0, 0, zc - 0.5, rf, n, lambda p: at.uv("face", (p[0] + rf) / (2 * rf), (p[1] + rf) / (2 * rf)), dome=5.0, rings=2)
    return acc


def can_housing(spec, lod):
    acc = Acc()
    r0 = spec["opening"] / 2
    w = spec["trim"]
    n = {0: 32, 1: 16, 2: 8}[lod]
    if lod == 2:
        prof = plain([(0.0, -3.0), (w, -3.0)])
    elif lod == 1:
        prof = fillet([(0.0, 0.0), (0.0, -3.0), (w, -3.0), (w, 0.0)], [0, 0.0, 1.5, 0], 1)
        prof = [prof[1]] + [(p, s) for p, s in prof[2:]]
    else:
        prof = fillet([(0.0, 0.5), (0.0, -3.0), (w, -3.0), (w, 0.0), (w, 6.0)], [0, 0.9, 2.0, 0, 0], 2)
    sweep_round(acc, r0, prof, n, lambda a, t, T: trim_uv("enamel", a * 2 * math.pi * (r0 + w / 2), t, T))
    return acc


def can_phone(spec, at):
    acc = Acc()
    r0 = spec["opening"] / 2
    sweep_round(acc, r0, plain([(0.0, -3.0), (spec["trim"], -3.0)]), 10, sw(at, "trim"))
    rt = spec["baffle_top_d"] / 2
    sweep_round(acc, r0, plain([(rt - r0, spec["baffle_h"]), (0.0, -3.0)]), 10, lambda a, t, T: at.uv("baffle", a, 1 - t / T))
    rf = spec["face_d"] / 2
    ids = [acc.v((rt * math.cos(2 * math.pi * k / 10), rt * math.sin(2 * math.pi * k / 10), spec["baffle_h"])) for k in range(10)]
    acc.f(ids, [at.uv("face", (acc.V[i][0] / rt) * 0.5 * (rt / rf) * 0.98 + 0.5, (acc.V[i][1] / rt) * 0.5 * (rt / rf) * 0.98 + 0.5) for i in ids], False, (0, 0, -1))
    return acc


# --------------------------------------------------------------------------- L03 soil, ceiling cuts
def soil_ring(R):
    acc = Acc()
    hx_i, hy_i = R["hxt"] + T_BAR, R["hyt"] + T_BAR
    hx_o, hy_o = hx_i + 150.0, hy_i + 60.0
    def uvf(p):
        return ((p[0] + hx_o) / (2 * hx_o), (p[1] + hy_o) / (2 * hy_o))
    inner = [(hx_i, -hy_i), (hx_i, hy_i), (-hx_i, hy_i), (-hx_i, -hy_i)]
    outer = [(hx_o, -hy_o), (hx_o, hy_o), (-hx_o, hy_o), (-hx_o, -hy_o)]
    for k in range(4):
        k1 = (k + 1) % 4
        pts = [(inner[k][0], inner[k][1], -1.0), (inner[k1][0], inner[k1][1], -1.0), (outer[k1][0], outer[k1][1], -1.0), (outer[k][0], outer[k][1], -1.0)]
        quad(acc, pts, uvf, (0, 0, -1))
    return acc, [2 * hx_o, 2 * hy_o]


def ceiling_cut(kind):
    """A tile-plane panel with the opening cut, 16 mm tile edge. Slot 'Ceiling' (world projected)."""
    acc = Acc()
    uvf = lambda p: (p[0] / 1000.0, p[1] / 1000.0)
    if kind == "Can":
        R, OX, OY, n = 89.0, 600.0, 900.0, 32
        # every cut panel is 1.2 x 1.8 m (2 x 3 tiles of 0.6 m) so panels tile on the grid
        sq = []
        for k in range(n):
            a = 2 * math.pi * k / n
            c, s = math.cos(a), math.sin(a)
            t = min(OX / max(abs(c), 1e-6), OY / max(abs(s), 1e-6))
            sq.append((t * c, t * s))
        for k in range(n):
            k1 = (k + 1) % n
            a0, a1 = 2 * math.pi * k / n, 2 * math.pi * k1 / n
            pts = [(R * math.cos(a0), R * math.sin(a0), 0), (R * math.cos(a1), R * math.sin(a1), 0), (sq[k1][0], sq[k1][1], 0), (sq[k][0], sq[k][1], 0)]
            quad(acc, pts, uvf, (0, 0, -1))
        sweep_round(acc, R, plain([(0.0, 16.0), (0.0, 0.0)]), n, lambda a, t, T: (a, t / 1000.0))
        return acc
    hx, hy = {"2x4": (300, 600), "2x2": (300, 300), "1x4": (150, 600)}[kind]
    ox, oy = 600, 900
    inner = [(hx, -hy), (hx, hy), (-hx, hy), (-hx, -hy)]
    outer = [(ox, -oy), (ox, oy), (-ox, oy), (-ox, -oy)]
    for k in range(4):
        k1 = (k + 1) % 4
        pts = [(inner[k][0], inner[k][1], 0), (inner[k1][0], inner[k1][1], 0), (outer[k1][0], outer[k1][1], 0), (outer[k][0], outer[k][1], 0)]
        quad(acc, pts, uvf, (0, 0, -1))
    sweep_rect(acc, hx, hy, plain([(0.0, 16.0), (0.0, 0.0)]), lambda s, a, t, T: (a / 1000.0, t / 1000.0))
    return acc


# --------------------------------------------------------------------------- Blender objects + export
def to_object(acc, name, mat_names, coll):
    me = bpy.data.meshes.new(name)
    me.from_pydata([(x * 0.001, y * 0.001, z * 0.001) for x, y, z in acc.V], [], acc.F)
    uv = me.uv_layers.new(name="UVMap")
    li = 0
    for pi, poly in enumerate(me.polygons):
        for k, loop in enumerate(poly.loop_indices):
            uv.data[loop].uv = acc.UV[pi][k]
    me.polygons.foreach_set("use_smooth", acc.SM)
    for m in mat_names:
        mat = bpy.data.materials.get(m) or bpy.data.materials.new(m)
        me.materials.append(mat)
    me.polygons.foreach_set("material_index", acc.MAT)
    me.validate(clean_customdata=False)
    me.update()
    me.set_sharp_from_angle(angle=math.radians(50.0))
    ob = bpy.data.objects.new(name, me)
    coll.objects.link(ob)
    # check: nothing visible may face up into the ceiling (tubes and knuckles excepted)
    up = 0
    for poly in me.polygons:
        if poly.normal.z > 0.2:
            up += 1
    return ob, up


def tri_count(ob):
    return sum(len(p.vertices) - 2 for p in ob.data.polygons)


def export(objs, path):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"MESH"}, apply_unit_scale=True,
                             apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
                             bake_space_transform=True, mesh_smooth_type="OFF", use_mesh_modifiers=True,
                             use_tspace=True, add_leaf_bones=False, bake_anim=False, path_mode="STRIP")


def reset():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for m in list(bpy.data.meshes):
        bpy.data.meshes.remove(m)


def main():
    report = {}
    coll = bpy.context.scene.collection
    want = lambda oid: not ONLY or oid in ONLY
    for oid, spec in LENSED.items():
        if not want(oid):
            continue
        reset()
        R = lensed_rings(spec)
        at = lensed_atlas(oid, spec, R)
        lensmat, trim = "LampOpt_%s_Lens" % oid, "LampOpt_Trim"
        objs, rep = [], {}
        ob, up = to_object(lensed_lens(oid, spec, R, at), "Lens", [lensmat], coll)
        objs.append(ob); rep["Lens"] = (tri_count(ob), up)
        for lod in (0, 1, 2):
            ob, up = to_object(lensed_housing(oid, spec, R, lod), "Housing_L%d" % lod, [trim], coll)
            objs.append(ob); rep[ob.name] = (tri_count(ob), up)
        ob, up = to_object(lensed_phone(oid, spec, R, at), "Phone", [lensmat], coll)
        objs.append(ob); rep["Phone"] = (tri_count(ob), up)
        if oid == "L02":
            for lod in (0, 1, 2):
                ob, up = to_object(lensed_housing(oid, spec, R, lod, door_finish="black"), "Housing_b_L%d" % lod, [trim], coll)
                objs.append(ob); rep[ob.name] = (tri_count(ob), up)
        extra = {}
        if oid == "L03":
            acc, size = soil_ring(R)
            ob, up = to_object(acc, "Soil", ["LampOpt_L03_Soil"], coll)
            objs.append(ob); rep["Soil"] = (tri_count(ob), up)
            extra["soil_mm"] = size
        if oid == "L05":
            ob, up = to_object(lensed_lens(oid, spec, R, at, sag=4.0), "Lens_Sag", [lensmat], coll)
            objs.append(ob); rep["Lens_Sag"] = (tri_count(ob), up)
            oat = open_atlas()
            lens, hs = l05_open(spec, R, oat)
            ob, up = to_object(lens, "Open_Lens", ["LampOpt_L05_Open"], coll)
            objs.append(ob); rep["Open_Lens"] = (tri_count(ob), up)
            for lod, h in hs.items():
                ob, up = to_object(h, "Open_Housing_L%d" % lod, [trim], coll)
                objs.append(ob); rep[ob.name] = (tri_count(ob), up)
            extra["open_atlas"] = oat.json()
        export(objs, os.path.join(OUT, "LampOpt_%s.fbx" % oid))
        lay = dict(id=oid, family="lensed", spec=spec, rings=R, atlas=at.json(), atlas_meta=at.meta, **extra)
        json.dump(lay, open(os.path.join(OUT, "LampOpt_%s_layout.json" % oid), "w"), indent=1)
        report[oid] = rep
    for oid, spec in LOUVER.items():
        if not want(oid):
            continue
        reset()
        at, fx, fy = louver_atlas(oid, spec)
        lensmat, trim = "LampOpt_%s_Lens" % oid, "LampOpt_Trim"
        objs, rep = [], {}
        lacc, cols = louver_lens(oid, spec, at, fx, fy)
        ob, up = to_object(lacc, "Lens", [lensmat], coll)
        objs.append(ob); rep["Lens"] = (tri_count(ob), up)
        for lod in (0, 1, 2):
            ob, up = to_object(louver_housing(oid, spec, fx, fy, lod), "Housing_L%d" % lod, [trim], coll)
            objs.append(ob); rep[ob.name] = (tri_count(ob), up)
        ob, up = to_object(louver_phone(oid, spec, at, fx, fy), "Phone", [lensmat], coll)
        objs.append(ob); rep["Phone"] = (tri_count(ob), up)
        export(objs, os.path.join(OUT, "LampOpt_%s.fbx" % oid))
        lay = dict(id=oid, family="louver", spec=spec, field=[fx, fy], columns=cols, atlas=at.json(), atlas_meta=at.meta)
        json.dump(lay, open(os.path.join(OUT, "LampOpt_%s_layout.json" % oid), "w"), indent=1)
        report[oid] = rep
    for oid, spec in CAN.items():
        if not want(oid):
            continue
        reset()
        at = can_atlas()
        lensmat, trim = "LampOpt_%s_Lens" % oid, "LampOpt_Trim"
        objs, rep = [], {}
        ob, up = to_object(can_lens(spec, at), "Lens", [lensmat], coll)
        objs.append(ob); rep["Lens"] = (tri_count(ob), up)
        for lod in (0, 1, 2):
            ob, up = to_object(can_housing(spec, lod), "Housing_L%d" % lod, [trim], coll)
            objs.append(ob); rep[ob.name] = (tri_count(ob), up)
        ob, up = to_object(can_phone(spec, at), "Phone", [lensmat], coll)
        objs.append(ob); rep["Phone"] = (tri_count(ob), up)
        export(objs, os.path.join(OUT, "LampOpt_%s.fbx" % oid))
        lay = dict(id=oid, family="can", spec=spec, atlas=at.json())
        json.dump(lay, open(os.path.join(OUT, "LampOpt_%s_layout.json" % oid), "w"), indent=1)
        report[oid] = rep
    if want("CUTS"):
        reset()
        objs, rep = [], {}
        for kind in ("2x4", "2x2", "1x4", "Can"):
            ob, up = to_object(ceiling_cut(kind), "CeilingCut_" + kind, ["Ceiling"], coll)
            objs.append(ob); rep[ob.name] = (tri_count(ob), up)
        export(objs, os.path.join(OUT, "LampOpt_CeilingCuts.fbx"))
        report["CUTS"] = rep
    path = os.path.join(OUT, "build_report.json")
    old = json.load(open(path)) if os.path.exists(path) else {}
    old.update(report)
    json.dump(old, open(path, "w"), indent=1)
    for oid, rep in report.items():
        print("BUILT", oid, " ".join("%s=%d%s" % (k, v[0], ("(UP %d)" % v[1]) if v[1] else "") for k, v in rep.items()))


main()
