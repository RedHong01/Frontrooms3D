"""O1 receptacles: shared plate, device, LOD and check helpers for the outlet
kit (helper module: no NAME, no build).

Group O1 of Documentation/research/outlets/10_spec.md (sections 1.1-1.5,
5.0, 5.1). O1 does not import the O2 or O3 helpers (spec 5.0). Every
section 1.2 number below is copied from the spec and asserted against a
literal copy of the spec table (SPEC_1_2), so the plates of every group
agree to 0.05 mm by construction.

FRAME (every O1 asset)
* Geometry is written in MILLIMETRES as (x, h, z): x across the plate
  (right as seen from the front), h out of the wall (Blender -y), z up.
  Mesh.to_object() converts once to Blender metres (x, -h, z).
* Origin = the wall-face point at the plate centre. The plate back lies on
  the wall plane h = 0 and nothing lies behind it (Kit_OutletBare's box is
  the one exception, <= 35 mm). The front faces -Y (Unity +Z). kitlib
  exports Blender (x, y, z) as Unity (-x, z, -y).

What lives here
* Section 1.2 numbers, SPEC_1_2 and assert_shared_numbers().
* Outlines: rounded rectangles that stay in step from ring to ring (extra
  columns allowed on the straight edges), the duplex "round with flats",
  slot outlines (5-15R blades, the 5-20R T, the ground U), mitred offsets.
* Mesh: a vertex/face collector with rings, bridges, fans and a
  constrained-Delaunay fill (mathutils.geometry.delaunay_2d_cdt) for fields
  with holes, slits and Steiner points. Rings are CCW in (x, z); faces are
  wound so CCW in (x, z) faces the front (+h).
* thermoset_plate(): the section 1.2 edge profile swept round a 12-segment
  corner outline, the crowned field, 0.5 mm 3-segment opening rounds, the
  82-degree countersink with its seat level with the field, the hollow back
  (1.6 rim on the wall, cavity face 3.5). Optional crack (a stepped V split
  from the screw hole to the edge) and corner chip (bisected facets).
* stainless_plate(): 0.76 sheet, flat field 4.70, quarter-round formed edge
  r 1.5 with a straight leg to the wall, corner radius 1.0.
* duplex_device(): two 5-15R (or 5-20R) faces, 1.0 proud of the field with
  0.6 mm 3-segment edges, the 0.4 clearance gap down to the device plane
  1.5 behind the field, slots with 0.15 mouth chamfers going 7 mm deep
  (clamped at the wall: see SLOT_FLOOR_MIN), brass contact leaves 3 mm in.
* LOD1 / LOD2 hand-built part sets (spec 1.1 table). Modules build them
  only when kitlib.Kit.make_lods exists (P-1 / P-1b); until then each FBX
  holds LOD0 only.
* paint_wear(): the fr_wear colour attribute on every part (BYTE_COLOR,
  face corner; R hand grime, G edge wear, B cavity; 1 = untouched).
* to_object(): the G1 weighted-normal recipe per part (smooth, sharp by
  SMOOTH_ANGLE, WEIGHTED_NORMAL modifier that finish() applies; proven by
  interactables/build_g1 section 2, no kitlib change).
* meta(), anchors and the plate / device / proud / triangle asserts.
"""

import math

import bmesh
import kitlib
from mathutils import Vector
from mathutils.geometry import delaunay_2d_cdt

MM = 0.001
SMOOTH_ANGLE = 35.0

# ------------------------------------------------------------ slots (1.5)
TI = "Prop_ThermosetIvory"   # new tint-only slot (P-4b, NEEDS APPROVAL)
NI = "Prop_NylonIvory"       # new tint-only slot (P-4b, NEEDS APPROVAL)
OR = "Prop_PlasticOrange"    # new tint-only slot (P-4b, NEEDS APPROVAL)
PB = "Prop_PlasticBlack"
BR = "Prop_Brass"
AL = "Prop_Aluminium"
CE = "Prop_Ceramic"          # brown (existing)


def _hex(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))


def register_slots():
    """Spec 1.5 preview colours (sRGB / 255, as O2 does). Roughness =
    1 - smoothness of the spec table's Unity values."""
    kitlib.register_slot(TI, _hex("#E6DDC2"), 0.25, 0.0)
    kitlib.register_slot(NI, _hex("#E2D9BF"), 0.45, 0.0)
    kitlib.register_slot(OR, _hex("#D8642A"), 0.45, 0.0)


def has_lods():
    """P-1/P-1b gate, read at build time (spec 1.1)."""
    return hasattr(kitlib.Kit, "make_lods")


# ------------------------------------------------- 1.2 shared numbers (mm)
PLATE_W, PLATE_H = 69.8, 114.3
CORNER_R = 2.0
CORNER_SEGS = 12
PROFILE = ((0.00, 0.00), (0.15, 1.20), (0.45, 2.60), (0.95, 3.80),
           (1.70, 4.70), (2.70, 5.25), (4.00, 5.50))
FIELD_CROWN = 5.60
BACK_RIM = 1.6
CAVITY_H = 3.5
SS_SHEET = 0.76
SS_FIELD = 4.70
SS_EDGE_R = 1.5
SS_CORNER_R = 1.0
JUMBO_W, JUMBO_H, JUMBO_D = 88.9, 133.4, 6.5
OPEN_D = 33.3
OPEN_FLAT = 14.3
OPEN_Z = 19.45
OPEN_ROUND = 0.5
OPEN_ROUND_SEGS = 3
CSK_D = 7.4
CSK_ANGLE = 82.0
SEAT_D = 6.6
FACE_D = 32.5
FACE_FLAT = 13.9
FACE_PROUD = 1.0
FACE_ROUND = 0.6
FACE_ROUND_SEGS = 3
DEVICE_BACK = 1.5            # the device plane behind the field
NEUTRAL = (2.0, 8.5, -6.35, 3.0)   # w, h, x, z in the face frame
HOT = (2.0, 7.0, 6.35, 3.0)
GROUND_D = 5.0
GROUND_Z = -8.9
SLOT_DEPTH = 7.0
SLOT_CHAMFER = 0.15
CONTACT_T = 0.4
CONTACT_DEPTH = 3.0
ARM_20 = (6.4, 2.0)          # 5-20R arm: length (to -x from the neutral centre), height
SCREW_D = 6.6
SCREW_CROWN = 1.0
SCREW_SLOT = (0.8, 0.6)      # width, depth
SCREW_SEGS = 64
PROUD_LIMIT = 7.5
LOD_DISTANCES = (1.5, 4.0, 12.0)

# The spec 1.2 table as printed (2026-10-03). assert_shared_numbers() checks
# the working constants against it: a number edited here must be edited
# twice, on purpose.
SPEC_1_2 = {
    "PLATE_W": 69.8, "PLATE_H": 114.3, "CORNER_R": 2.0, "CORNER_SEGS": 12,
    "PROFILE": ((0, 0), (0.15, 1.20), (0.45, 2.60), (0.95, 3.80), (1.70, 4.70), (2.70, 5.25), (4.00, 5.50)),
    "FIELD_CROWN": 5.60, "BACK_RIM": 1.6, "CAVITY_H": 3.5,
    "SS_SHEET": 0.76, "SS_FIELD": 4.70, "SS_EDGE_R": 1.5, "SS_CORNER_R": 1.0,
    "JUMBO_W": 88.9, "JUMBO_H": 133.4, "JUMBO_D": 6.5,
    "OPEN_D": 33.3, "OPEN_FLAT": 14.3, "OPEN_Z": 19.45, "OPEN_ROUND": 0.5, "OPEN_ROUND_SEGS": 3,
    "CSK_D": 7.4, "CSK_ANGLE": 82.0, "SEAT_D": 6.6,
    "FACE_D": 32.5, "FACE_FLAT": 13.9, "FACE_PROUD": 1.0, "FACE_ROUND": 0.6, "FACE_ROUND_SEGS": 3,
    "DEVICE_BACK": 1.5,
    "NEUTRAL": (2.0, 8.5, -6.35, 3.0), "HOT": (2.0, 7.0, 6.35, 3.0), "GROUND_D": 5.0, "GROUND_Z": -8.9,
    "SLOT_DEPTH": 7.0, "SLOT_CHAMFER": 0.15, "CONTACT_T": 0.4, "CONTACT_DEPTH": 3.0, "ARM_20": (6.4, 2.0),
    "SCREW_D": 6.6, "SCREW_CROWN": 1.0, "SCREW_SLOT": (0.8, 0.6), "SCREW_SEGS": 64,
    "PROUD_LIMIT": 7.5, "LOD_DISTANCES": (1.5, 4.0, 12.0),
}

# --------------------------------------------- O1 design numbers (ESTIMATE)
# Each is O1's reading of an ESTIMATE or of a gap in 1.2; the build report
# (build_o1_receptacles_wall_plates_with_duplex_devices.md) lists them.
ARC_DEG = 360.0 / 64   # opening / face arcs at 64 segments per circle (spec 1.1):
                       # 22 per arc, 46 verts per outline (round 5 had 7.0 deg = 51/circle)
CSK_SEGS = 40          # countersink rings (sagitta 0.011 mm; hidden under the 64-seg head)
GROUND_SEGS = 16       # ground U semicircle (sagitta 0.012 mm = 0.04 px at 0.3 m)
R_FLOOR = 0.5          # corner radius kept by profile rings inset past CORNER_R
# Corner segments per profile ring (outline first). The outline (r 2.0) keeps
# the spec's 12; the inner rings, whose radius shrinks to 0.5, keep about the
# same chord (<= 0.26 mm): 12/12/10/8/4/4/4 for r 2.0/1.85/1.55/1.05/0.5/0.5/0.5.
# Rings of different counts are joined by Mesh.zip (structural points in step).
CORNER_SEGS_RINGS = (12, 12, 10, 8, 4, 4, 4)
SS_CORNER_SEGS_RINGS = (12, 12, 12, 6, 4)   # stainless: r 1.0/1.0/0.8/0.3/0.3
SS_R_FLOOR = 0.3
SEAT_DROP = 0.05       # seat ring 0.05 below the field: the 0.4 mm countersink lip reads
BORE_D = 3.6           # #6 clearance bore at the cone bottom (dark cap)
SLOT_FLOOR_MIN = 0.25  # slot floors stop 0.25 in front of the wall plane (7.0 would cross it)
CONTACT_INSET = 0.5    # leaves stop 0.5 short of each slot end
CONTACT_H = 1.2        # leaf depth (h) from 3.0 to 4.2 below the face top
STEINER = 9.0          # crown points every 9 mm on the thermoset field
BACK_SEGS = 4          # hidden back rings: 4 segments per corner
BOX_SHEET = (50.8, 76.2)   # dark box plane behind the device (2 x 3 in device box)
SS_OPEN_ROUND = 0.35   # stainless opening edge (punched sheet)
SS_OPEN_SEGS = 2
SS_EDGE_SEGS = 3


# ------------------------------------------------------------------ asserts
def _close(a, b, tol=0.05):
    if isinstance(a, (tuple, list)):
        return len(a) == len(b) and all(_close(x, y, tol) for x, y in zip(a, b))
    return abs(float(a) - float(b)) <= tol


def assert_shared_numbers():
    g = globals()
    for key, want in SPEC_1_2.items():
        assert _close(g[key], want, 0.0005), "1.2 drift: %s = %r, spec %r" % (key, g[key], want)
    # The 1.2 relations the spec states in words.
    assert _close(OPEN_D / 2 - FACE_D / 2, 0.4, 0.05) and _close(OPEN_FLAT - FACE_FLAT, 0.4, 0.05), "face clearance"
    assert _close(2 * OPEN_FLAT, 28.6, 0.05), "opening height 28.6"
    assert _close(2 * OPEN_Z, 38.9, 0.05), "opening pitch 1-17/32 in"
    assert _close(HOT[2] - NEUTRAL[2], 12.7, 0.05), "blade pitch 12.7"
    assert _close(HOT[3] - GROUND_Z, 11.9, 0.05), "ground 11.9 below the blade line"
    assert GROUND_Z + GROUND_D / 2 < NEUTRAL[3] - NEUTRAL[1] / 2, "ground hole below the blades"


# ------------------------------------------------------------------ outlines
def rrect(hw, hh, r, segs, cols=None, params=False):
    """CCW rounded rectangle centred at 0. The cyclic list begins on the right
    edge (just above the bottom-right arc). cols = {"R": [z..], "L": [z..],
    "T": [x..], "B": [x..]} adds points on the straight edges; give the same
    values to every ring and the rings stay in step. params=True also
    returns (params, span): every column point and arc end gets an integer
    parameter, arc interiors fractions, so rings with different ``segs``
    can be zipped (Mesh.zip)."""
    cols = cols or {}
    r = max(0.02, min(r, hw - 0.01, hh - 0.01))
    pts, prm = [], []
    p = [0.0]

    def col(pt):
        p[0] += 1.0
        pts.append(pt)
        prm.append(p[0])

    def arc(cx, cz, a0):
        for k in range(segs + 1):
            a = math.radians(a0 + 90.0 * k / segs)
            pts.append((cx + r * math.cos(a), cz + r * math.sin(a)))
            prm.append(p[0] + 1.0 + k / segs)
        p[0] += 2.0

    for z in sorted(cols.get("R", ())):
        col((hw, z))
    arc(hw - r, hh - r, 0.0)
    for x in sorted(cols.get("T", ()), reverse=True):
        col((x, hh))
    arc(-hw + r, hh - r, 90.0)
    for z in sorted(cols.get("L", ()), reverse=True):
        col((-hw, z))
    arc(-hw + r, -hh + r, 180.0)
    for x in sorted(cols.get("B", ())):
        col((x, -hh))
    arc(hw - r, -hh + r, 270.0)
    if params:
        return pts, prm, p[0]
    return pts


def arc_k(R, flat):
    """Segments per arc for a round-with-flats outline at ARC_DEG."""
    tj = math.asin(min(1.0, flat / R))
    k = int(math.ceil(math.degrees(2 * tj) / ARC_DEG))
    return max(4, k + (k % 2))     # even: a vertex sits on the widest point


def rwf(cx, cz, R, flat, k, off=0.0):
    """Round-with-flats outline (CCW): circle R + off cut by flats at
    cz +- (flat + off); k segments per arc, 2 (k + 1) points. Point i stays in
    step for every offset, so rings bridge."""
    Ro, fo = R + off, flat + off
    tj = math.asin(min(1.0, fo / Ro))
    pts = []
    for i in range(k + 1):
        a = -tj + 2 * tj * i / k
        pts.append((cx + Ro * math.cos(a), cz + Ro * math.sin(a)))
    for i in range(k + 1):
        a = math.pi - tj + 2 * tj * i / k
        pts.append((cx + Ro * math.cos(a), cz + Ro * math.sin(a)))
    return pts


def circle(cx, cz, r, n, a0=0.0):
    return [(cx + r * math.cos(a0 + 2 * math.pi * i / n), cz + r * math.sin(a0 + 2 * math.pi * i / n)) for i in range(n)]


def rect(cx, cz, w, h):
    return [(cx + w / 2, cz - h / 2), (cx + w / 2, cz + h / 2), (cx - w / 2, cz + h / 2), (cx - w / 2, cz - h / 2)]


def t_slot(cx, cz, w, h, arm_len, arm_h):
    """5-20R neutral: the vertical slot plus a horizontal arm from its centre
    line to -x (CCW)."""
    x0, x1 = cx - w / 2, cx + w / 2
    z0, z1 = cz - h / 2, cz + h / 2
    xa = cx - arm_len
    return [(x1, z0), (x1, z1), (x0, z1), (x0, cz + arm_h / 2), (xa, cz + arm_h / 2),
            (xa, cz - arm_h / 2), (x0, cz - arm_h / 2), (x0, z0)]


def ground_u(cx, cz, d, segs):
    """Ground hole: a semicircle below (centre = pin centre), straight sides
    and a flat top d/2 above the centre (CCW)."""
    r = d / 2
    pts = []
    for i in range(segs + 1):
        a = math.pi + math.pi * i / segs
        pts.append((cx + r * math.cos(a), cz + r * math.sin(a)))
    pts.append((cx + r, cz + r))
    pts.append((cx - r, cz + r))
    return pts


def offset_poly(pts, o):
    """Mitred offset of a simple CCW polygon (o > 0 grows it)."""
    n = len(pts)
    out = []
    for i in range(n):
        x0, z0 = pts[i - 1]
        x1, z1 = pts[i]
        x2, z2 = pts[(i + 1) % n]
        d1 = Vector((x1 - x0, z1 - z0)).normalized()
        d2 = Vector((x2 - x1, z2 - z1)).normalized()
        n1 = Vector((d1.y, -d1.x))
        n2 = Vector((d2.y, -d2.x))
        m = (n1 + n2) / (1.0 + n1.dot(n2))
        out.append((x1 + o * m.x, z1 + o * m.y))
    return out


def inside(poly, x, z):
    c = False
    n = len(poly)
    for i in range(n):
        x1, z1 = poly[i]
        x2, z2 = poly[(i + 1) % n]
        if (z1 > z) != (z2 > z):
            if x < x1 + (z - z1) * (x2 - x1) / (z2 - z1):
                c = not c
    return c


def seg_dist(px, pz, ax, az, bx, bz):
    dx, dz = bx - ax, bz - az
    L = dx * dx + dz * dz
    t = 0.0 if L == 0 else max(0.0, min(1.0, ((px - ax) * dx + (pz - az) * dz) / L))
    qx, qz = ax + t * dx - px, az + t * dz - pz
    return math.sqrt(qx * qx + qz * qz), t


def dist_poly(poly, x, z, closed=True):
    best = 1e9
    n = len(poly)
    for i in range(n if closed else n - 1):
        ax, az = poly[i]
        bx, bz = poly[(i + 1) % n]
        best = min(best, seg_dist(x, z, ax, az, bx, bz)[0])
    return best


def smoothstep(e0, e1, x):
    t = max(0.0, min(1.0, (x - e0) / (e1 - e0)))
    return t * t * (3 - 2 * t)


# ------------------------------------------------------------------- mesh
class Mesh:
    """Vertex/face collector in the plate frame (mm). Rings are CCW in (x, z).
    bridge(a, b): a quad strip a[i], a[i+1], b[i+1], b[i]. It faces the front
    or outward when a is the outer/lower ring of an island or the outer/upper
    ring of a hole (see the module notes)."""

    def __init__(self):
        self.v = []
        self.f = []

    def add(self, x, h, z):
        self.v.append([float(x), float(h), float(z)])
        return len(self.v) - 1

    def ring(self, pts, h):
        if callable(h):
            return [self.add(x, h(x, z), z) for x, z in pts]
        return [self.add(x, h, z) for x, z in pts]

    def face(self, idx, flip=False):
        idx = tuple(idx)
        self.f.append(tuple(reversed(idx)) if flip else idx)

    def bridge(self, a, b, flip=False, closed=True):
        assert len(a) == len(b), "bridge: %d vs %d" % (len(a), len(b))
        n = len(a)
        for i in range(n if closed else n - 1):
            j = (i + 1) % n
            self.face((a[i], a[j], b[j], b[i]), flip)

    def zip(self, a, pa, b, pb, span):
        """Bridge two closed rings with different vertex counts whose
        structural points share parameters (rrect(params=True)). Wound as
        bridge(a, b); n_a + n_b triangles."""
        na, nb = len(a), len(b)
        assert abs(pa[0] - pb[0]) < 1e-9, "zip: rings start out of step"
        i = j = 0
        while i < na or j < nb:
            ta = (pa[i + 1] if i + 1 < na else pa[0] + span) if i < na else 1e18
            tb = (pb[j + 1] if j + 1 < nb else pb[0] + span) if j < nb else 1e18
            if ta <= tb + 1e-9:
                self.face((a[i % na], a[(i + 1) % na], b[j % nb]))
                i += 1
            else:
                self.face((a[i % na], b[(j + 1) % nb], b[j % nb]))
                j += 1

    def fan(self, ring, apex, flip=False):
        n = len(ring)
        for i in range(n):
            self.face((ring[i], ring[(i + 1) % n], apex), flip)

    def facing(self, idx, n_hint):
        """Add a polygon wound so its normal points along n_hint (x, h, z)."""
        p = [Vector(self.v[i]) for i in idx]
        n = Vector((0.0, 0.0, 0.0))
        for i in range(len(p)):
            a, b = p[i], p[(i + 1) % len(p)]
            n += Vector(((a.y - b.y) * (a.z + b.z), (a.z - b.z) * (a.x + b.x), (a.x - b.x) * (a.y + b.y)))
        # n is the Newell normal in (x, h, z); the (x, h, z) frame is
        # left-handed against Blender's, so CCW in (x, z) has n.y < 0 here.
        want = Vector(n_hint)
        self.face(idx, flip=(n.dot(want) > 0))

    def fill(self, outer, holes=(), lines=(), steiner=(), flip=False):
        """Constrained Delaunay fill of the region inside the ``outer`` loop
        (vertex indices) minus the ``holes`` loops. ``lines`` are open
        polylines (vertex indices) kept as edges (slits, crease lines);
        ``steiner`` are interior vertex indices. Triangles are wound CCW in
        (x, z) (front-facing), or the reverse with flip=True."""
        loops = [list(outer)] + [list(h) for h in holes]
        coords, ids, edges = [], [], []
        seen = {}

        def vid(vi):
            if vi in seen:
                return seen[vi]
            x, h, z = self.v[vi]
            coords.append((x, z))
            ids.append(vi)
            seen[vi] = len(coords) - 1
            return seen[vi]

        polys = []
        for loop in loops:
            polys.append([(self.v[vi][0], self.v[vi][2]) for vi in loop])
            n = len(loop)
            for k in range(n):
                edges.append((vid(loop[k]), vid(loop[(k + 1) % n])))
        for line in lines:
            for a, b in zip(line, line[1:]):
                edges.append((vid(a), vid(b)))
        for vi in steiner:
            vid(vi)
        out = delaunay_2d_cdt([Vector(c) for c in coords], edges, [], 0, 1e-7, True)
        vco, faces, orig_v = out[0], out[2], out[3]
        assert len(vco) == len(coords), "CDT added %d vertices (loops cross or touch?)" % (len(vco) - len(coords))
        omap = []
        for ov in orig_v:
            assert ov, "CDT produced a new vertex"
            omap.append(ids[ov[0]])
        added = 0
        for f in faces:
            cx = sum(vco[i][0] for i in f) / len(f)
            cz = sum(vco[i][1] for i in f) / len(f)
            if not inside(polys[0], cx, cz) or any(inside(p, cx, cz) for p in polys[1:]):
                continue
            tri = [omap[i] for i in f]
            a, b, c = (self.v[i] for i in tri)
            area = (b[0] - a[0]) * (c[2] - a[2]) - (b[2] - a[2]) * (c[0] - a[0])
            if abs(area) < 1e-12:
                continue
            if area < 0:
                tri.reverse()
            self.face(tri, flip)
            added += 1
        return added

    def tris(self):
        return sum(len(f) - 2 for f in self.f)

    def merge(self, other):
        base = len(self.v)
        self.v.extend([list(v) for v in other.v])
        self.f.extend([tuple(i + base for i in f) for f in other.f])
        return self

    def xform(self, axis="+h", offset=(0.0, 0.0, 0.0)):
        """Proper rotation of a local part whose axis is +h onto the plate
        axis "+h", "+x" or "-x", then a translation (winding is preserved)."""
        ox, oh, oz = offset
        for v in self.v:
            x, h, z = v
            if axis == "+x":
                x, h = h, -x
            elif axis == "-x":
                x, h = -h, x
            v[0], v[1], v[2] = x + ox, h + oh, z + oz
        return self

    def hmax(self):
        return max(v[1] for v in self.v) if self.v else 0.0

    def hmin(self):
        return min(v[1] for v in self.v) if self.v else 0.0

    def to_object(self, kit, name, slot, wear=None, lods="0", sharp_all=False, bm_hook=None):
        bm = bmesh.new()
        bv = [bm.verts.new((x * MM, -h * MM, z * MM)) for x, h, z in self.v]
        for f in self.f:
            try:
                bm.faces.new([bv[i] for i in f])
            except ValueError:
                pass  # duplicate face (shared fan), skip
        big = [f for f in bm.faces if len(f.verts) > 4]
        if big:
            bmesh.ops.triangulate(bm, faces=big, quad_method="BEAUTY", ngon_method="BEAUTY")
        loose = [v for v in bm.verts if not v.link_faces]
        if loose:
            bmesh.ops.delete(bm, geom=loose, context="VERTS")
        if bm_hook is not None:
            bm_hook(bm)
        return finish_part(kit, kit._new_object(name, bm, slot, "metres", "xz"), wear, lods, sharp_all)


def finish_part(kit, obj, wear=None, lods="0", sharp_all=False):
    """The G1 per-part recipe: smooth, sharp by SMOOTH_ANGLE, fr_wear, then a
    WEIGHTED_NORMAL modifier that kit.finish() applies (survives the join and
    the FBX round trip exactly, interactables/build_g1 section 2)."""
    me = obj.data
    me.shade_smooth()
    me.set_sharp_from_angle(angle=math.radians(0.0 if sharp_all else SMOOTH_ANGLE))
    paint_wear(obj, wear)
    obj["fr_lods"] = lods
    m = obj.modifiers.new("wn", "WEIGHTED_NORMAL")
    m.mode = "FACE_AREA"
    m.weight = 50
    m.keep_sharp = True
    return obj


def part_tris(obj):
    return sum(len(p.vertices) - 2 for p in obj.data.polygons)


# ------------------------------------------------------------------- wear
def to_plate(co):
    """Blender metres -> plate frame mm (x, h, z)."""
    return (co[0] / MM, -co[1] / MM, co[2] / MM)


def paint_wear(obj, fn=None):
    """fr_wear colour attribute (BYTE_COLOR, face corner), the interactables
    encoding: R hand grime, G edge wear, B cavity; 1 = clean. W2/P-3 decides
    whether Unity reads it. fn(p, n) gets the plate-frame point (mm) and the
    face normal in the plate frame."""
    me = obj.data
    attr = me.color_attributes.get("fr_wear") or me.color_attributes.new("fr_wear", "BYTE_COLOR", "CORNER")
    for poly in me.polygons:
        n = (poly.normal[0], -poly.normal[1], poly.normal[2])
        for li in poly.loop_indices:
            p = to_plate(me.vertices[me.loops[li].vertex_index].co)
            c = fn(p, n) if fn is not None else (1.0, 1.0, 1.0)
            attr.data[li].color_srgb = (max(0.0, min(1.0, c[0])), max(0.0, min(1.0, c[1])), max(0.0, min(1.0, c[2])), 1.0)


def wear_cavity(p, n):
    return (1.0, 1.0, 0.35)


def make_plate_wear(openings_rwf, hw, hh, edge_band=2.7):
    """R 0.85 on the field within 4 mm of an opening (where hands and plugs
    go), G 0.9 on the outer edge crest (inset < edge_band), else white."""
    def fn(p, n):
        x, h, z = p
        r = 1.0
        for poly in openings_rwf:
            if dist_poly(poly, x, z) < 4.0 and n[1] > 0.2:
                r = 0.85
                break
        inset = min(hw - abs(x), hh - abs(z))
        g = 0.9 if (inset < edge_band and n[1] > 0.15 and h > 3.0) else 1.0
        return (r, g, 1.0)
    return fn


def wear_device(p, n):
    return (0.85 if n[1] > 0.5 else 1.0, 1.0, 1.0)


# ------------------------------------------------------- plate (thermoset)
def crown_fn(hw, hh, inset, edge_h, crown_h):
    fx, fz = hw - inset, hh - inset

    def h(x, z):
        u, v = x / fx, z / fz
        return edge_h + (crown_h - edge_h) * max(0.0, 1 - u * u) * max(0.0, 1 - v * v)
    return h


class Crack:
    """A radial split from the countersink to the right edge (spec 1.3):
    0.25 wide V groove, the lower (B) side stepped down STEP at the lip and
    blended over FALL mm; the step tapers in over TAPER mm from the screw
    (the screw still clamps the centre). The groove floor never reaches the
    wall plane (self-check d)."""
    WIDTH = 0.25
    STEP = 0.20
    DEPTH = 0.55
    FALL = 6.0
    TAPER = 5.0

    def __init__(self, path, z_exit):
        self.path = path          # field polyline (x, z) from the countersink ring to the field edge
        self.z_exit = z_exit      # z of the straight run through the edge profile

    def offset_lines(self):
        """A (upper), C (centre), B (lower) polylines, mitred, 0.125 apart."""
        pts = self.path
        n = len(pts)
        A, C, B = [], [], []
        for i in range(n):
            if i == 0:
                d = Vector((pts[1][0] - pts[0][0], pts[1][1] - pts[0][1])).normalized()
                m = Vector((-d.y, d.x))
            elif i == n - 1:
                d = Vector((pts[-1][0] - pts[-2][0], pts[-1][1] - pts[-2][1])).normalized()
                m = Vector((-d.y, d.x))
            else:
                d1 = Vector((pts[i][0] - pts[i - 1][0], pts[i][1] - pts[i - 1][1])).normalized()
                d2 = Vector((pts[i + 1][0] - pts[i][0], pts[i + 1][1] - pts[i][1])).normalized()
                n1, n2 = Vector((-d1.y, d1.x)), Vector((-d2.y, d2.x))
                m = (n1 + n2) / (1.0 + n1.dot(n2))
            w = self.WIDTH / 2
            x, z = pts[i]
            A.append((x + m.x * w, z + m.y * w))
            C.append((x, z))
            B.append((x - m.x * w, z - m.y * w))
        return A, C, B

    def signed(self, x, z):
        """(signed distance, arc length t) to the crack centre line, which runs
        on through the profile at z_exit. s > 0 = A side (above)."""
        line = list(self.path) + [(self.path[-1][0] + 10.0, self.z_exit)]
        best, bs, bt, acc = 1e9, 0.0, 0.0, 0.0
        for (ax, az), (bx, bz) in zip(line, line[1:]):
            d, t = seg_dist(x, z, ax, az, bx, bz)
            L = math.hypot(bx - ax, bz - az)
            if d < best:
                cross = (bx - ax) * (z - az) - (bz - az) * (x - ax)
                best, bs, bt = d, (d if cross >= 0 else -d), acc + t * L
            acc += L
        return bs, bt

    def step_at(self, x, z, h, hscale=3.8):
        s, t = self.signed(x, z)
        if s > -self.WIDTH / 2 + 1e-4:
            return 0.0
        fall = 1.0 - smoothstep(self.WIDTH / 2, self.FALL, -s)
        return -self.STEP * fall * smoothstep(0.0, self.TAPER, t) * min(1.0, h / hscale)


class Chip:
    """A chipped corner (spec 1.3: 5 x 4 mm, 3-4 conchoidal facets): the
    surface is clamped down to F = max(facet planes), kept above a floor
    that protects the hollow back; the plate is bisected along each plane
    first, so the break line is a crisp mesh edge. Planes are (a, b, c) for
    h = a*u + b*w + c, with (u, w) measured from the corner along the
    edges."""

    def __init__(self, corner, planes, floor_in=0.3, floor_out=3.75, floor_d=(0.9, 1.5), reach=9.0):
        self.corner = corner      # (sx, sz): -1/+1 signs of the corner
        self.reach = reach
        self.clamped = 0
        self.planes = planes
        self.floor_in = floor_in
        self.floor_out = floor_out
        self.floor_d = floor_d
        # The bisect zone: the plate rings get extra columns ZONE mm from the
        # corner on both edges, and only faces wholly inside the zone are cut,
        # so the facet cuts never run along the whole edge band (a cut through
        # a long smooth-shaded band shows as a shading line, round 5).
        self.zone = reach + 1.5

    def cols(self, hw, hh, cols=None):
        """Ring columns that close the bisect zone (stay in step on every ring)."""
        cols = {k: list(v) for k, v in (cols or {}).items()}
        sx, sz = self.corner
        cols.setdefault("B" if sz < 0 else "T", []).append(sx * (hw - self.zone))
        cols.setdefault("L" if sx < 0 else "R", []).append(sz * (hh - self.zone))
        return cols

    def steiner(self, hw, hh):
        """Field points that keep the field triangles at the corner small."""
        sx, sz = self.corner
        return [(sx * (hw - self.zone + 1.0), sz * (hh - self.zone + 1.0))]

    def local(self, x, z, hw, hh):
        sx, sz = self.corner
        return hw - sx * x, hh - sz * z     # u from the side edge, w from the top/bottom edge

    def F(self, u, w):
        return max(a * u + b * w + c for a, b, c in self.planes)

    def floor(self, u, w):
        d = min(u, w)
        return self.floor_in + (self.floor_out - self.floor_in) * smoothstep(self.floor_d[0], self.floor_d[1], d)


def thermoset_plate(W=PLATE_W, H=PLATE_H, R=CORNER_R, profile=PROFILE, crown=FIELD_CROWN,
                    open_z=(OPEN_Z, -OPEN_Z), csk=((0.0, 0.0),), crack=None, steiner=STEINER, chip=None):
    """LOD0 thermoset plate as a Mesh, plus a dict of rings the device needs.
    profile = ((inset, h), ...) from the outline to the field edge."""
    hw, hh = W / 2, H / 2
    fin, fh = profile[-1]
    hfn = crown_fn(hw, hh, fin, fh, crown)
    m = Mesh()
    cols = None
    if crack is not None:
        ze = crack.z_exit
        w2 = Crack.WIDTH / 2
        cols = {"R": [ze + w2, ze, ze - w2, ze - Crack.FALL]}
    if chip is not None:
        cols = chip.cols(hw, hh, cols)
    # Edge profile rings (12 segments per corner on the outline, fewer on the
    # small inner radii: CORNER_SEGS_RINGS; inner rings keep R_FLOOR).
    assert len(profile) == len(CORNER_SEGS_RINGS) and CORNER_SEGS_RINGS[0] == CORNER_SEGS
    rings, prms = [], []
    for (d, h), segs in zip(profile, CORNER_SEGS_RINGS):
        pts, prm, span = rrect(hw - d, hh - d, max(R - d, R_FLOOR), segs, cols, params=True)
        rings.append(m.ring(pts, h))
        prms.append(prm)
    for k in range(len(rings) - 1):
        m.zip(rings[k], prms[k], rings[k + 1], prms[k + 1], span)
    field_ring = rings[-1]
    # Openings: the 0.5 mm 3-segment round from the field to the vertical
    # tangent (the device gap starts below it).
    k = arc_k(OPEN_D / 2, OPEN_FLAT)
    open_rings = []
    open_tops = []
    for oz in open_z:
        rr = []
        for j in range(OPEN_ROUND_SEGS + 1):
            th = math.radians(90.0 * j / OPEN_ROUND_SEGS)
            off = OPEN_ROUND * (1 - math.sin(th))
            dh = OPEN_ROUND * (1 - math.cos(th))
            pts = rwf(0.0, oz, OPEN_D / 2, OPEN_FLAT, k, off)
            rr.append(m.ring(pts, lambda x, z, dh=dh: hfn(x, z) - dh))
        for a, b in zip(rr, rr[1:]):
            m.bridge(a, b)
        open_rings.append(rr)
        open_tops.append(rr[0])
    # Countersinks: field ring 7.4 -> seat 6.6 (SEAT_DROP below the field)
    # -> 82 deg cone to the bore; a dark cap closes the bore (separate part).
    csk_info = []
    lines = []
    for cx, cz in csk:
        angs = [2 * math.pi * i / CSK_SEGS for i in range(CSK_SEGS)]
        if crack is not None and abs(cx) < 1e-6 and abs(cz) < 1e-6:
            # Insert the crack's A, C, B mouth points into every countersink ring.
            A, C, B = crack.offset_lines()
            mouth = [math.atan2(p[1], p[0]) for p in (B[0], C[0], A[0])]
            angs = sorted([a for a in angs if min(abs(_wrap(a - mm)) for mm in mouth) > math.radians(3.0)] + [_wrap0(mm) for mm in mouth])
        r0, r1 = CSK_D / 2, SEAT_D / 2
        seat_h = hfn(cx, cz) - SEAT_DROP
        cone_drop = (r1 - BORE_D / 2) / math.tan(math.radians(CSK_ANGLE / 2))
        c0 = m.ring([(cx + r0 * math.cos(a), cz + r0 * math.sin(a)) for a in angs], hfn)
        c1 = m.ring([(cx + r1 * math.cos(a), cz + r1 * math.sin(a)) for a in angs], seat_h)
        c2 = m.ring([(cx + BORE_D / 2 * math.cos(a), cz + BORE_D / 2 * math.sin(a)) for a in angs], seat_h - cone_drop)
        m.bridge(c0, c1)
        m.bridge(c1, c2)
        csk_info.append({"centre": (cx, cz), "seat_h": seat_h, "bore_h": seat_h - cone_drop, "angs": angs,
                         "field_ring": c0})
    # Crack polylines in the field (A, C, B), ending on the field-edge ring's
    # extra columns.
    crack_ids = None
    if crack is not None:
        A, C, B = crack.offset_lines()
        c0 = csk_info[0]["field_ring"]

        def find(ring, x, z):
            return min(ring, key=lambda vi: (m.v[vi][0] - x) ** 2 + (m.v[vi][2] - z) ** 2)

        ids = []
        for line, zcol in ((A, crack.z_exit + Crack.WIDTH / 2), (C, crack.z_exit), (B, crack.z_exit - Crack.WIDTH / 2)):
            start = find(c0, line[0][0], line[0][1])
            mid = [m.add(x, hfn(x, z), z) for x, z in line[1:-1]]
            end = find(field_ring, hw - fin, zcol)
            ids.append([start] + mid + [end])
        lines = ids
        crack_ids = ids
    # Steiner points carry the 0.1 crown (and the crack's B-side fall).
    st = []
    holes_xz = [[(m.v[vi][0], m.v[vi][2]) for vi in ring] for ring in open_tops] + \
               [[(m.v[vi][0], m.v[vi][2]) for vi in ci["field_ring"]] for ci in csk_info]
    outer_xz = [(m.v[vi][0], m.v[vi][2]) for vi in field_ring]
    cand = []
    if steiner:
        nx = max(1, int(round((2 * (hw - fin)) / steiner)))
        nz = max(1, int(round((2 * (hh - fin)) / steiner)))
        for i in range(1, nx):
            for j in range(1, nz):
                cand.append((-(hw - fin) + 2 * (hw - fin) * i / nx, -(hh - fin) + 2 * (hh - fin) * j / nz))
    if crack is not None:
        # rows parallel to the crack: B side at 1.6 and 3.6 mm, A side at 2.0
        for off in (-1.6, -3.6, 2.0):
            for i in range(1, 14):
                t = i / 14.0
                x = 3.7 + (hw - fin - 3.7) * t
                z = _poly_z(crack.path, x)
                cand.append((x, z + off))
    if chip is not None:
        cand += chip.steiner(hw, hh)
    for x, z in cand:
        if not inside(outer_xz, x, z) or any(inside(p, x, z) for p in holes_xz):
            continue
        if min(dist_poly(p, x, z) for p in [outer_xz] + holes_xz) < 1.4:
            continue
        if crack is not None:
            s, _ = crack.signed(x, z)
            if abs(s) < 1.0:
                continue
        if any((x - a) ** 2 + (z - b) ** 2 < 1.0 for a, b in [(m.v[q][0], m.v[q][2]) for q in st]):
            continue
        st.append(m.add(x, hfn(x, z), z))
    m.fill(field_ring, holes=open_tops + [ci["field_ring"] for ci in csk_info], lines=lines, steiner=st)
    # Hollow back (hidden; 4 segments per corner): rim on the wall, inner rim
    # wall, cavity face (continuous: the device pierces it, back-face culled).
    outline = rings[0]
    rim_in = m.ring(rrect(hw - BACK_RIM, hh - BACK_RIM, max(R - BACK_RIM, R_FLOOR), BACK_SEGS), 0.0)
    m.fill(outline, holes=[rim_in], flip=True)
    rim_top = m.ring([(m.v[vi][0], m.v[vi][2]) for vi in rim_in], CAVITY_H)
    m.bridge(rim_top, rim_in)
    m.fill(rim_top, flip=True)
    info = {"rings": rings, "open_rings": open_rings, "csk": csk_info, "hfn": hfn, "hw": hw, "hh": hh,
            "field_edge_h": fh, "crown": crown, "crack_ids": crack_ids, "k": k, "open_z": open_z,
            "outline": [(m.v[vi][0], m.v[vi][2]) for vi in outline], "profile": profile}
    if crack is not None:
        apply_crack(m, info, crack)
    return m, info


def _wrap(a):
    return (a + math.pi) % (2 * math.pi) - math.pi


def _wrap0(a):
    return a % (2 * math.pi)


def _poly_z(path, x):
    for (ax, az), (bx, bz) in zip(path, path[1:]):
        if ax <= x <= bx:
            return az + (bz - az) * (x - ax) / max(bx - ax, 1e-9)
    return path[-1][1] if x > path[-1][0] else path[0][1]


def apply_crack(m, info, crack):
    """Deform the plate front: the C line sinks DEPTH into the material
    (along the inward surface normal in the edge profile, straight down in
    the field), the B side steps down. Back faces (h = 0 rim, cavity) are
    left alone: everything is at h >= 0."""
    hw = info["hw"]
    profile = info["profile"]
    A, Cl, B = info["crack_ids"]
    centre_ids = set(Cl)
    # Edge-profile C column: the rings' vertex at (x = hw - d, z = z_exit).
    prof_c = []
    for k_ring, ring in enumerate(info["rings"]):
        d, h = profile[k_ring]
        vi = min(ring, key=lambda q: (m.v[q][0] - (hw - d)) ** 2 + (m.v[q][2] - crack.z_exit) ** 2)
        prof_c.append((k_ring, vi))
    back = set()
    # back vertices: everything at h == 0 except the outline ring, plus the cavity ring
    front_ids = set()
    for ring in info["rings"]:
        front_ids.update(ring)
    for rr in info["open_rings"]:
        for ring in rr:
            front_ids.update(ring)
    for ci in info["csk"]:
        front_ids.update(ci["field_ring"])
    # field interior vertices (Steiner, crack polylines) are those not in any
    # ring and with h > CAVITY_H + 0.5
    for i, v in enumerate(m.v):
        if i not in front_ids and v[1] > CAVITY_H + 1.0:
            front_ids.add(i)
    # 1. B-side step on every front vertex.
    for i in front_ids:
        x, h, z = m.v[i]
        if i in centre_ids:
            continue
        m.v[i][1] = h + crack.step_at(x, z, h)
    # 2. The V floor: C in the field drops DEPTH below the A lip; in the
    #    edge profile it moves DEPTH along the inward normal (none at h = 0).
    for vi in Cl[:-1]:
        x, h, z = m.v[vi]
        m.v[vi][1] = h - crack.DEPTH - crack.STEP * 0.5 * smoothstep(0.0, crack.TAPER, crack.signed(x, z)[1])
    n_p = len(profile)
    for k_ring, vi in prof_c:
        d, h = profile[k_ring]
        if h <= 1e-6:
            continue
        dp = profile[min(k_ring + 1, n_p - 1)]
        dm = profile[max(k_ring - 1, 0)]
        ti, th = dp[0] - dm[0], dp[1] - dm[1]
        L = math.hypot(ti, th)
        # outward normal in (inset, h) is (-th, ti)/L: move against it
        ni, nh = -th / L, ti / L
        depth = crack.DEPTH * min(1.0, h / 3.8)
        m.v[vi][0] = hw - (d - ni * depth)      # inset grows by -ni * depth
        m.v[vi][1] = h - nh * depth - crack.STEP * 0.5 * min(1.0, h / 3.8)


def chip_bmesh(bm, info, chip):
    """Bisect the plate's front faces along each chip plane and each facet
    ridge near the corner, then clamp the front surface to max(F, floor).
    Works on the plate part's bmesh (Blender metres) before the object is
    made, so sharp edges and normals are computed on the final shape."""
    hw, hh = info["hw"], info["hh"]
    sx, sz = chip.corner
    reach = chip.reach

    def uvw(co):
        x, h, z = to_plate(co)
        u, w = chip.local(x, z, hw, hh)
        return u, w, h

    def region():
        bm.normal_update()
        faces = []
        for f in bm.faces:
            if f.normal.y >= -0.02:          # front-ish faces only (plate +h = Blender -y)
                continue
            if all(max(uvw(v.co)[:2]) <= chip.zone + 1e-4 for v in f.verts):
                faces.append(f)
        edges = list({e for f in faces for e in f.edges})
        verts = list({v for f in faces for v in f.verts})
        return faces + edges + verts

    for a, b, c in list(chip.planes) + [(0.0, 0.0, chip.floor_out)]:
        nrm = Vector((a * sx, -1.0, b * sz)).normalized()
        p = Vector((sx * (hw - 2.0) * MM, -(2 * a + 2 * b + c) * MM, sz * (hh - 2.0) * MM))
        bmesh.ops.bisect_plane(bm, geom=region(), dist=1e-10, plane_co=p, plane_no=nrm)
    P = chip.planes
    for i in range(len(P)):
        for j in range(i + 1, len(P)):
            da, db, dc = P[i][0] - P[j][0], P[i][1] - P[j][1], P[i][2] - P[j][2]
            if abs(da) + abs(db) < 1e-9:
                continue
            if abs(da + db) > 1e-9:
                u0 = w0 = -dc / (da + db)
            elif abs(da) > 1e-9:
                u0, w0 = -dc / da, 0.0
            else:
                u0, w0 = 0.0, -dc / db
            nrm = Vector((da * sx, 0.0, db * sz)).normalized()
            p = Vector((sx * (hw - u0) * MM, 0.0, sz * (hh - w0) * MM))
            bmesh.ops.bisect_plane(bm, geom=region(), dist=1e-10, plane_co=p, plane_no=nrm)
    big = [f for f in bm.faces if len(f.verts) > 4]
    if big:
        bmesh.ops.triangulate(bm, faces=big, quad_method="BEAUTY", ngon_method="BEAUTY")
    bm.normal_update()
    clamped = 0
    ext = [0.0, 0.0, 0.0]
    for v in bm.verts:
        u, w, h = uvw(v.co)
        if u > reach or w > reach or h <= 1e-6:
            continue
        if not any(f.normal.y < -0.02 for f in v.link_faces):
            continue
        target = max(chip.F(u, w), chip.floor(u, w))
        if target < h - 1e-4:
            v.co.y = -target * MM
            clamped += 1
            if h - target > 0.25:
                ext[0], ext[1], ext[2] = max(ext[0], u), max(ext[1], w), max(ext[2], h - target)
    chip.clamped = clamped
    chip.extent = tuple(round(e, 2) for e in ext)
    return clamped


# ------------------------------------------------------- plate (stainless)
def stainless_plate(W=PLATE_W, H=PLATE_H, open_z=(OPEN_Z, -OPEN_Z), csk=((0.0, 0.0),)):
    """0.030 in stainless: flat field SS_FIELD, a quarter-round formed edge of
    radius SS_EDGE_R and a straight leg to the wall; the sheet's back (inner
    leg, inner bend, underside) hidden at 4 segments per corner."""
    hw, hh = W / 2, H / 2
    m = Mesh()
    rc = SS_FIELD - SS_EDGE_R       # height of the bend centre
    prof = [(0.0, 0.0)]
    for j in range(SS_EDGE_SEGS + 1):
        ph = math.radians(90.0 - 90.0 * j / SS_EDGE_SEGS)   # 90 (side) -> 0 (top)
        prof.append((SS_EDGE_R - SS_EDGE_R * math.sin(ph), rc + SS_EDGE_R * math.cos(ph)))
    assert len(prof) == len(SS_CORNER_SEGS_RINGS) and SS_CORNER_SEGS_RINGS[0] == CORNER_SEGS
    rings, prms = [], []
    for (d, h), segs in zip(prof, SS_CORNER_SEGS_RINGS):
        pts, prm, span = rrect(hw - d, hh - d, max(SS_CORNER_R - d, SS_R_FLOOR), segs, params=True)
        rings.append(m.ring(pts, h))
        prms.append(prm)
    for k in range(len(rings) - 1):
        m.zip(rings[k], prms[k], rings[k + 1], prms[k + 1], span)
    field_ring = rings[-1]
    hfn = lambda x, z: SS_FIELD   # noqa: E731
    k = arc_k(OPEN_D / 2, OPEN_FLAT)
    open_rings, open_tops = [], []
    for oz in open_z:
        rr = []
        for j in range(SS_OPEN_SEGS + 1):
            th = math.radians(90.0 * j / SS_OPEN_SEGS)
            off = SS_OPEN_ROUND * (1 - math.sin(th))
            dh = SS_OPEN_ROUND * (1 - math.cos(th))
            rr.append(m.ring(rwf(0.0, oz, OPEN_D / 2, OPEN_FLAT, k, off), SS_FIELD - dh))
        for a, b in zip(rr, rr[1:]):
            m.bridge(a, b)
        open_rings.append(rr)
        open_tops.append(rr[0])
    csk_info = []
    for cx, cz in csk:
        angs = [2 * math.pi * i / CSK_SEGS for i in range(CSK_SEGS)]
        r0, r1 = CSK_D / 2, SEAT_D / 2
        seat_h = SS_FIELD - SEAT_DROP
        cone_drop = (r1 - BORE_D / 2) / math.tan(math.radians(CSK_ANGLE / 2))
        c0 = m.ring([(cx + r0 * math.cos(a), cz + r0 * math.sin(a)) for a in angs], SS_FIELD)
        c1 = m.ring([(cx + r1 * math.cos(a), cz + r1 * math.sin(a)) for a in angs], seat_h)
        c2 = m.ring([(cx + BORE_D / 2 * math.cos(a), cz + BORE_D / 2 * math.sin(a)) for a in angs], seat_h - cone_drop)
        m.bridge(c0, c1)
        m.bridge(c1, c2)
        csk_info.append({"centre": (cx, cz), "seat_h": seat_h, "bore_h": seat_h - cone_drop, "angs": angs, "field_ring": c0})
    m.fill(field_ring, holes=open_tops + [ci["field_ring"] for ci in csk_info])
    # Sheet back: bottom edge (0 -> 0.76 at h 0), inner leg, inner bend, underside.
    outline = rings[0]
    t = SS_SHEET
    in0 = m.ring(rrect(hw - t, hh - t, max(SS_CORNER_R - t, SS_R_FLOOR), BACK_SEGS), 0.0)
    m.fill(outline, holes=[in0], flip=True)
    in1 = m.ring([(m.v[vi][0], m.v[vi][2]) for vi in in0], rc)
    m.bridge(in1, in0)
    ri = SS_EDGE_R - t
    in2 = m.ring(rrect(hw - SS_EDGE_R, hh - SS_EDGE_R, SS_R_FLOOR, BACK_SEGS), SS_FIELD - t)
    m.bridge(in2, in1)
    m.fill(in2, flip=True)
    info = {"rings": rings, "open_rings": open_rings, "csk": csk_info, "hfn": hfn, "hw": hw, "hh": hh,
            "field_edge_h": SS_FIELD, "crown": SS_FIELD, "k": k, "open_z": open_z,
            "outline": [(m.v[vi][0], m.v[vi][2]) for vi in outline], "profile": prof, "inner_r": ri}
    return m, info


def bore_caps(info):
    """Dark (PB) caps closing each countersink bore."""
    m = Mesh()
    for ci in info["csk"]:
        cx, cz = ci["centre"]
        ring = m.ring([(cx + BORE_D / 2 * math.cos(a), cz + BORE_D / 2 * math.sin(a)) for a in ci["angs"]], ci["bore_h"])
        m.fill(ring)
    return m


# ------------------------------------------------------------------ device
def receptacle_face(body, slots, cont, oz, face_top, skirt_h, k, t20=False, floor_h=None, floors=False,
                    ground_segs=GROUND_SEGS):
    """One 5-15R / 5-20R face centred at (0, oz): the skirt from skirt_h up,
    the 0.6 mm 3-segment round, the face top with 0.15 mm mouth chamfers,
    dark slot walls down to floor_h (optional dark floors) and the brass
    contact leaves 3 mm in. Returns the skirt's bottom ring (body ids)."""
    off0 = -(OPEN_D / 2 - FACE_D / 2)     # the face outline = opening - 0.4
    sk_b = body.ring(rwf(0.0, oz, OPEN_D / 2, OPEN_FLAT, k, off0), skirt_h)
    rr = []
    for j in range(FACE_ROUND_SEGS + 1):
        th = math.radians(90.0 - 90.0 * j / FACE_ROUND_SEGS)   # 90 (side) -> 0 (top)
        off = off0 - FACE_ROUND * (1 - math.sin(th))
        hh_ = face_top - FACE_ROUND * (1 - math.cos(th))
        rr.append(body.ring(rwf(0.0, oz, OPEN_D / 2, OPEN_FLAT, k, off), hh_))
    body.bridge(sk_b, rr[0])               # skirt
    for a, b in zip(rr, rr[1:]):
        body.bridge(a, b)                  # 0.6 round
    nw, nh, nx, nz = NEUTRAL
    hw_, hh_s, hx, hz = HOT
    neutral = t_slot(nx, oz + nz, nw, nh, ARM_20[0], ARM_20[1]) if t20 else rect(nx, oz + nz, nw, nh)
    hot = rect(hx, oz + hz, hw_, hh_s)
    ground = ground_u(0.0, oz + GROUND_Z, GROUND_D, ground_segs)
    if floor_h is None:
        floor_h = max(face_top - SLOT_DEPTH, SLOT_FLOOR_MIN)
    mouths = []
    for poly in (neutral, hot, ground):
        mouth = body.ring(offset_poly(poly, SLOT_CHAMFER), face_top)
        cham = body.ring(poly, face_top - SLOT_CHAMFER)
        body.bridge(mouth, cham)           # mouth chamfer (device colour)
        mouths.append(mouth)
        s_top = slots.ring(poly, face_top - SLOT_CHAMFER)
        s_bot = slots.ring(poly, floor_h)
        slots.bridge(s_top, s_bot)         # slot walls (dark)
        if floors:
            slots.fill(s_bot)
    body.fill(rr[-1], holes=mouths)        # face top
    for (w, h, x, z) in (NEUTRAL, HOT):
        zc = oz + z
        z0, z1 = zc - h / 2 + CONTACT_INSET, zc + h / 2 - CONTACT_INSET
        top = face_top - CONTACT_DEPTH
        bot = top - CONTACT_H
        for side in (-1, 1):
            xo = x + side * w / 2                 # slot wall
            xi = x + side * (w / 2 - CONTACT_T)   # leaf inner face
            q = [cont.add(xo, top, z0), cont.add(xi, top, z0), cont.add(xi, top, z1), cont.add(xo, top, z1)]
            cont.facing(q, (0, 1, 0))
            qi = [cont.add(xi, top, z0), cont.add(xi, bot, z0), cont.add(xi, bot, z1), cont.add(xi, top, z1)]
            cont.facing(qi, (-side, 0, 0))
            for zz, sgn in ((z0, -1), (z1, 1)):
                qe = [cont.add(xo, top, zz), cont.add(xi, top, zz), cont.add(xi, bot, zz), cont.add(xo, bot, zz)]
                cont.facing(qe, (0, 0, sgn))
    return sk_b


def duplex_device(info, face_top, gap_h, t20=False, open_bottom_h=None):
    """Two receptacle faces in the plate's openings.
    Returns (body Mesh [device colour], slots Mesh [PB], contacts Mesh [BR],
    numbers). The body runs: the 0.4 gap band from the opening's lowest ring
    down to the device plane gap_h -> the face skirt -> the 0.6 round -> the
    face top with the slot mouths (0.15 chamfer). Slot walls go down to the
    dark box sheet (the slot floors and the void behind the device)."""
    k = info["k"]
    body, slots, cont = Mesh(), Mesh(), Mesh()
    sheet_h = max(face_top - SLOT_DEPTH, SLOT_FLOOR_MIN)
    face_rings_xz = []
    hfn = info["hfn"]
    off0 = -(OPEN_D / 2 - FACE_D / 2)
    for oz in info["open_z"]:
        ob = body.ring(rwf(0.0, oz, OPEN_D / 2, OPEN_FLAT, k, 0.0),
                       (lambda x, z: hfn(x, z) - OPEN_ROUND) if open_bottom_h is None else open_bottom_h)
        sk_b = receptacle_face(body, slots, cont, oz, face_top, gap_h, k, t20, sheet_h)
        body.bridge(ob, sk_b)              # gap floor (slants down to the device plane)
        face_rings_xz.append(rwf(0.0, oz, OPEN_D / 2, OPEN_FLAT, k, off0))
    bw, bh = BOX_SHEET
    sh = slots.ring(rect(0.0, 0.0, bw, bh), sheet_h)
    slots.fill(sh)
    nums = {"sheet_h": sheet_h, "face_top": face_top, "gap_h": gap_h, "face_rings_xz": face_rings_xz}
    return body, slots, cont, nums


# ------------------------------------------------------------- screw heads
def screw_head(R=SCREW_D / 2, crown=SCREW_CROWN, slot_w=SCREW_SLOT[0], slot_d=SCREW_SLOT[1], rim_segs=SCREW_SEGS,
               line_segs=10, rings=((0.894, 32), (0.606, 20), (0.333, 10)), side=0.0, slot_angle=0.0):
    """Slotted oval head (side = 0) or binding head (side > 0: a straight band
    below the dome) in a local frame: axis +h, seat (or band bottom) at
    h = 0 / -side, slot along x rotated by slot_angle (deg). A flat-bottomed
    slot, as a saw cuts it: it runs out through the dome where the dome is
    lower than the floor. Steiner rings are (radius / R, count).
    Returns (Mesh, dome function)."""
    RS = (R * R + crown * crown) / (2 * crown)

    def dome(x, z):
        return max(0.0, math.sqrt(max(RS * RS - x * x - z * z, 0.0)) - (RS - crown))
    floor_h = crown - slot_d
    r_e = math.sqrt(RS * RS - (floor_h + RS - crown) ** 2)
    hz = slot_w / 2
    x_e = math.sqrt(r_e * r_e - hz * hz)
    a_e = math.asin(hz / r_e)
    m = Mesh()
    rim = m.ring(circle(0.0, 0.0, R, rim_segs), 0.0)
    xs = [-x_e + 2 * x_e * i / line_segs for i in range(line_segs + 1)]
    c_bl, c_br = m.add(-x_e, floor_h, -hz), m.add(x_e, floor_h, -hz)
    c_tr, c_tl = m.add(x_e, floor_h, hz), m.add(-x_e, floor_h, hz)
    bot_d = [c_bl] + [m.add(x, dome(x, -hz), -hz) for x in xs[1:-1]] + [c_br]
    top_d = [c_tr] + [m.add(x, dome(x, hz), hz) for x in reversed(xs[1:-1])] + [c_tl]
    bot_f = [c_bl] + [m.add(x, floor_h, -hz) for x in xs[1:-1]] + [c_br]
    top_f = [c_tr] + [m.add(x, floor_h, hz) for x in reversed(xs[1:-1])] + [c_tl]
    arc_r = [m.add(r_e * math.cos(a), floor_h, r_e * math.sin(a)) for a in (-a_e / 3, a_e / 3)]
    arc_l = [m.add(r_e * math.cos(a), floor_h, r_e * math.sin(a)) for a in (math.pi - a_e / 3, math.pi + a_e / 3)]
    st = []
    for rr_, n in rings:
        rr_ *= R
        for i in range(n):
            a = 2 * math.pi * (i + 0.5 * (n % 2)) / n
            x, z = rr_ * math.cos(a), rr_ * math.sin(a)
            if abs(z) < hz + 0.18 and rr_ < r_e + 0.25:
                continue
            st.append(m.add(x, dome(x, z), z))
    m.fill(rim, holes=[bot_d + arc_r + top_d + arc_l], steiner=st)
    m.fill(bot_f + arc_r + top_f + arc_l)
    for dl, fl, nz in ((bot_d, bot_f, 1.0), (top_d, top_f, -1.0)):
        for i in range(len(dl) - 1):
            q = [dl[i], dl[i + 1], fl[i + 1], fl[i]]
            q = [v for j, v in enumerate(q) if v not in q[:j]]
            m.facing(q, (0.0, 0.0, nz))
    if side > 0:
        low = m.ring(circle(0.0, 0.0, R, rim_segs), -side)
        m.bridge(low, rim)
    if slot_angle:
        a = math.radians(slot_angle)
        c, s_ = math.cos(a), math.sin(a)
        for v in m.v:
            x, h, z = v
            v[0], v[2] = x * c - z * s_, x * s_ + z * c
    return m, dome


# --------------------------------------------------------------- LOD1/LOD2
LOD1_CORNER_SEGS = 4
LOD1_ARC_K = 11        # 24-point faces and openings (spec 1.1 table)
LOD1_CSK_SEGS = 12


def lod1_plate(W, H, R, profile2, field_h, crown, open_z=(OPEN_Z, -OPEN_Z), slot_body=NI, crack=None, chip=None,
               steel=False):
    """LOD1 (1.5-4 m): 2-step edge, 4-segment corners, no back, a plain dark
    screw hole; device faces at 24 points with a 2-step edge and 0.5 mm deep
    black slot insets; no contacts, no screws. Returns plate, dark, body Meshes."""
    hw, hh = W / 2, H / 2
    fin = profile2[-1][0]
    hfn = crown_fn(hw, hh, fin, field_h, crown)
    p, dark, body = Mesh(), Mesh(), Mesh()
    cols = None
    if crack is not None:
        cols = {"R": [crack.z_exit, crack.z_exit - Crack.FALL]}
    if chip is not None:
        cols = chip.cols(hw, hh, cols)
    rings = [p.ring(rrect(hw - d, hh - d, max(R - d, R_FLOOR if not steel else SS_R_FLOOR), LOD1_CORNER_SEGS, cols), h)
             for d, h in profile2]
    for a, b in zip(rings, rings[1:]):
        p.bridge(a, b)
    k = LOD1_ARC_K
    tops = []
    lows = []
    for oz in open_z:
        r0 = p.ring(rwf(0.0, oz, OPEN_D / 2, OPEN_FLAT, k, 0.35), hfn)
        r1 = p.ring(rwf(0.0, oz, OPEN_D / 2, OPEN_FLAT, k, 0.1), lambda x, z: hfn(x, z) - 0.25)
        r2 = p.ring(rwf(0.0, oz, OPEN_D / 2, OPEN_FLAT, k, 0.0), lambda x, z: hfn(x, z) - 0.6)
        p.bridge(r0, r1)
        p.bridge(r1, r2)
        tops.append(r0)
        lows.append(r2)
    sh = p.ring(circle(0.0, 0.0, CSK_D / 2, LOD1_CSK_SEGS), hfn)
    sb = p.ring(circle(0.0, 0.0, CSK_D / 2 - 0.5, LOD1_CSK_SEGS), field_h - 0.6)
    p.bridge(sh, sb)
    dk = dark.ring(circle(0.0, 0.0, CSK_D / 2 - 0.5, LOD1_CSK_SEGS), field_h - 0.6)
    dark.fill(dk)
    lines, st = [], []
    if crack is not None:
        # the crack as a stepped crease (the 0.25 groove is 0.1 px at 1.5 m)
        pts = [(x, z) for x, z in crack.path]
        start = min(sh, key=lambda q: (p.v[q][0] - pts[0][0]) ** 2 + (p.v[q][2] - pts[0][1]) ** 2)
        mid = [p.add(x, hfn(x, z), z) for x, z in pts[1:-1]]
        end = min(rings[-1], key=lambda q: (p.v[q][0] - (hw - fin)) ** 2 + (p.v[q][2] - crack.z_exit) ** 2)
        lines = [[start] + mid + [end]]
        for i in range(1, 6):
            x = 4.0 + (hw - fin - 4.0) * i / 6.0
            st.append(p.add(x, hfn(x, _poly_z(crack.path, x) - 3.0), _poly_z(crack.path, x) - 3.0))
    p.fill(rings[-1], holes=tops + [sh], lines=lines, steiner=st)
    if crack is not None:
        line_ids = set(lines[0])
        for i, v in enumerate(p.v):
            x, h, z = v
            if i in line_ids:
                continue
            p.v[i][1] = h + crack.step_at(x, z, h)
    return p, dark, body, {"rings": rings, "lows": lows, "hfn": hfn, "k": k, "open_z": open_z, "hw": hw, "hh": hh}


def lod1_device(info1, face_top, gap_h, t20=False):
    k = info1["k"]
    body, dark = Mesh(), Mesh()
    for idx, oz in enumerate(info1["open_z"]):
        hfn = info1["hfn"]
        ob = body.ring(rwf(0.0, oz, OPEN_D / 2, OPEN_FLAT, k, 0.0), lambda x, z: hfn(x, z) - 0.6)
        sk = body.ring(rwf(0.0, oz, OPEN_D / 2, OPEN_FLAT, k, -0.4), gap_h)
        body.bridge(ob, sk)
        r1 = body.ring(rwf(0.0, oz, OPEN_D / 2, OPEN_FLAT, k, -0.4), face_top - 0.45)
        r2 = body.ring(rwf(0.0, oz, OPEN_D / 2, OPEN_FLAT, k, -0.55), face_top - 0.12)
        r3 = body.ring(rwf(0.0, oz, OPEN_D / 2, OPEN_FLAT, k, -0.9), face_top)
        body.bridge(sk, r1)
        body.bridge(r1, r2)
        body.bridge(r2, r3)
        nw, nh, nx, nz = NEUTRAL
        neutral = t_slot(nx, oz + nz, nw, nh, ARM_20[0], ARM_20[1]) if t20 else rect(nx, oz + nz, nw, nh)
        hot = rect(HOT[2], oz + HOT[3], HOT[0], HOT[1])
        ground = ground_u(0.0, oz + GROUND_Z, GROUND_D, 6)
        holes = []
        for poly in (neutral, hot, ground):
            top = body.ring(poly, face_top)
            holes.append(top)
            dt = dark.ring(poly, face_top)
            db = dark.ring(poly, face_top - 0.5)
            dark.bridge(dt, db)
            dark.fill(db)
        body.fill(r3, holes=holes)
    return body, dark


def lod2_plate(W, H, field_h, crown, face_top, chamfer=2.0, open_z=(OPEN_Z, -OPEN_Z)):
    """LOD2 (4-12 m): an 8-point rounded outline at full height (fan to a
    crowned centre), one bevel ring to the wall, the faces as two flat dark
    6-gons 0.3 mm proud (fans)."""
    hw, hh = W / 2, H / 2
    p, dark = Mesh(), Mesh()

    def octo(hw_, hh_, c):
        return [(hw_, -hh_ + c), (hw_, hh_ - c), (hw_ - c, hh_), (-hw_ + c, hh_), (-hw_, hh_ - c),
                (-hw_, -hh_ + c), (-hw_ + c, -hh_), (hw_ - c, -hh_)]
    base = p.ring(octo(hw, hh, chamfer + 1.0), 0.0)
    top = p.ring(octo(hw - 2.2, hh - 2.2, chamfer), field_h)
    p.bridge(base, top)
    centre = p.add(0.0, crown, 0.0)
    p.fan(top, centre)
    for oz in open_z:
        hexa = dark.ring([(15.6 * math.cos(math.radians(a)), oz + 13.0 * math.sin(math.radians(a)))
                          for a in (0, 60, 120, 180, 240, 300)], max(face_top - 0.7, crown + 0.3))
        c = dark.add(0.0, max(face_top - 0.7, crown + 0.3), oz)
        dark.fan(hexa, c)
    return p, dark


# ------------------------------------------------------------------- meta
def common_meta(kit, module, W, H, proud, extra=None):
    d = list(getattr(module, "LOD_DISTANCES", LOD_DISTANCES))
    kit.meta["lodDistances"] = [(-1.0 if x is None else float(x)) for x in d]
    kit.meta["lodRatios"] = [1.0, float(getattr(module, "LOD1_RATIO", 0.0)), float(getattr(module, "LOD2_RATIO", 0.0))]
    kit.meta["lodBudget"] = list(getattr(module, "BUDGET", ()))
    kit.meta["lodNote"] = ("lodDistances (d01, d12, dcull) m at lodBias 1, FOV 76. LOD1/LOD2 are hand-built part "
                           "sets (fr_lods) exported only once P-1/P-1b land; until then the FBX holds LOD0.")
    out = {"plateW": round(W * MM, 5), "plateH": round(H * MM, 5), "proud": round(proud * MM, 5)}
    if extra:
        out.update(extra)
    kit.meta["outlet"] = out


def tri_check(name, tris, budget, tol=0.15):
    lo, hi = budget * (1 - tol), budget * (1 + tol)
    assert lo <= tris <= hi, "%s: %d tris outside %d +-15%% (%d..%d)" % (name, tris, budget, lo, hi)


def lod_check(name, counts, budget):
    """LOD0 always; LOD1/LOD2 when their part sets were built (P-1/P-1b)."""
    for level, tris in enumerate(counts):
        if level < len(budget):
            tri_check("%s LOD%d" % (name, level), tris, budget[level])


# kitlib exports material VARIANTS with the base module's meta, so the
# "outlet" block names the BASE slots. A variant's real materials are its
# sidecar "slots" list: _Brown maps both ivory slots to Prop_Ceramic, which
# the FBX exporter merges into ONE submesh (3 submeshes, checked round 5);
# _IG maps Prop_NylonIvory to Prop_PlasticOrange. R3 maps by material name
# (Prop_Thermoset* = plate palette, Prop_NylonIvory / Prop_PlasticOrange =
# device palette), never by submesh index.
SLOT_NOTE = ("base slots; a VARIANT's real materials are in 'slots' (_Brown: plate and device share one "
             "Prop_Ceramic submesh; _IG: device Prop_PlasticOrange). Map by material name, not by index.")


def check_plate(m, info, W, H, depth, label):
    xs = [v[0] for v in m.v]
    zs = [v[2] for v in m.v]
    assert _close(max(xs), W / 2) and _close(-min(xs), W / 2), "%s outline width %.3f" % (label, max(xs) - min(xs))
    assert _close(max(zs), H / 2) and _close(-min(zs), H / 2), "%s outline height %.3f" % (label, max(zs) - min(zs))
    assert _close(m.hmax(), depth), "%s depth %.3f (want %.2f)" % (label, m.hmax(), depth)
    assert m.hmin() >= -1e-6, "%s: geometry behind the wall plane (%.3f)" % (label, m.hmin())


def check_openings(info):
    """Opening centres and sizes on the vertical-tangent ring."""
    m_open = []
    for idx, oz in enumerate(info["open_z"]):
        pts = rwf(0.0, oz, OPEN_D / 2, OPEN_FLAT, info["k"], 0.0)
        xs = [p[0] for p in pts]
        zs = [p[1] for p in pts]
        assert _close(max(xs) - min(xs), OPEN_D), "opening width %.3f" % (max(xs) - min(xs))
        assert _close(max(zs) - min(zs), 2 * OPEN_FLAT), "opening height %.3f" % (max(zs) - min(zs))
        assert _close((max(zs) + min(zs)) / 2, oz), "opening centre"
        m_open.append(pts)
    assert _close(abs(info["open_z"][0] - info["open_z"][1]), 2 * OPEN_Z), "opening pitch"
    return m_open


def check_device(nums, field_max, label):
    assert _close(nums["face_top"] - field_max, FACE_PROUD), "%s face proud %.3f" % (label, nums["face_top"] - field_max)
    for pts in nums["face_rings_xz"]:
        xs = [p[0] for p in pts]
        zs = [p[1] for p in pts]
        assert _close((OPEN_D - (max(xs) - min(xs))) / 2, 0.4), "%s face clearance (round)" % label
        assert _close((2 * OPEN_FLAT - (max(zs) - min(zs))) / 2, 0.4), "%s face clearance (flats)" % label
    assert nums["face_top"] <= PROUD_LIMIT + 1e-6, "%s proud %.2f > %.1f" % (label, nums["face_top"], PROUD_LIMIT)
    assert nums["sheet_h"] >= 0.0, "slot floor behind the wall"


def anchors(kit, info, face_top, field_top_h=None):
    for i, ci in enumerate(info["csk"]):
        cx, cz = ci["centre"]
        kit.anchor("screw_%d" % i, (cx * MM, -ci["seat_h"] * MM, cz * MM))
    zs = sorted(info["open_z"], reverse=True)
    kit.anchor("face_top", (0.0, -face_top * MM, zs[0] * MM))
    kit.anchor("face_bottom", (0.0, -face_top * MM, zs[-1] * MM))
    kit.anchor("plate_top", (0.0, 0.0, info["hh"] * MM))


def total_tris(kit, lods="0"):
    return sum(part_tris(o) for o in kit.parts if lods in str(o.get("fr_lods", "0")))


# --------------------------------------------------------------- assembly
CHIP_PLANES = ((1.45, 0.35, -3.9), (0.30, 1.75, -3.3), (0.85, 0.95, -2.3))   # O1 design (scratchpad chip_design.py)


def profile_h(profile, d):
    if d <= 0:
        return 0.0
    for (d0, h0), (d1, h1) in zip(profile, profile[1:]):
        if d <= d1:
            return h0 + (h1 - h0) * (d - d0) / (d1 - d0)
    return profile[-1][1]


def jumbo_profile():
    s = JUMBO_D / FIELD_CROWN
    return tuple((d * s, h * s) for d, h in PROFILE)


def plate_spec(kind):
    """(W, H, R, profile, crown, field_edge_h) for a plate kind."""
    if kind == "thermoset":
        return PLATE_W, PLATE_H, CORNER_R, PROFILE, FIELD_CROWN, PROFILE[-1][1]
    if kind == "jumbo":
        p = jumbo_profile()
        return JUMBO_W, JUMBO_H, CORNER_R, p, JUMBO_D, p[-1][1]
    if kind == "steel":
        return PLATE_W, PLATE_H, SS_CORNER_R, None, SS_FIELD, SS_FIELD
    raise ValueError(kind)


def build_duplex(kit, module, kind="thermoset", t20=False, crack=None, chip=None):
    """One duplex receptacle plate kit: plate (dominant slot first), device,
    slots + box sheet, bore caps, contacts; LOD1/LOD2 part sets when P-1
    lands; anchors, tags, meta and the asserts."""
    register_slots()
    assert_shared_numbers()
    W, H, R, prof, crown, edge_h = plate_spec(kind)
    label = module.NAME
    if kind == "steel":
        m, info = stainless_plate()
        plate_slot = AL
        open_bottom_h = SS_FIELD - SS_OPEN_ROUND
    else:
        m, info = thermoset_plate(W, H, R, prof, crown, crack=crack, chip=chip)
        plate_slot = TI
        open_bottom_h = None
    check_plate(m, info, W, H, crown, label)
    opens = check_openings(info)
    field_max = crown
    face_top = field_max + FACE_PROUD
    gap_h = edge_h - DEVICE_BACK
    base_wear = make_plate_wear(opens, info["hw"], info["hh"])
    wear = base_wear
    if crack is not None or chip is not None:
        hw, hh = info["hw"], info["hh"]

        def wear(p, n):
            r, g, b = base_wear(p, n)
            x, h, z = p
            if crack is not None:
                s, t = crack.signed(x, z)
                if abs(s) < Crack.WIDTH * 0.6 and h < info["hfn"](x, z) - 0.08:
                    b = 0.4                       # dirt in the split
            if chip is not None:
                u, w = chip.local(x, z, hw, hh)
                if u < chip.reach and w < chip.reach:
                    d = min(hw - abs(x), hh - abs(z))
                    if h < profile_h(prof, d) - 0.05 and abs(h - max(chip.F(u, w), chip.floor(u, w))) < 0.05:
                        g = 0.6                   # fresh fracture: lighter, rougher
            return (r, g, b)
    hook = None
    if chip is not None:
        def hook(bm):
            chip_bmesh(bm, info, chip)
            assert chip.clamped > 10, "chip clamped only %d vertices" % chip.clamped
    plate = m.to_object(kit, "plate", plate_slot, wear=wear, lods="0", bm_hook=hook)
    body, slots, cont, nums = duplex_device(info, face_top, gap_h, t20=t20, open_bottom_h=open_bottom_h)
    check_device(nums, field_max, label)
    body.to_object(kit, "device", NI, wear=wear_device, lods="0")
    slots.to_object(kit, "slots", PB, wear=wear_cavity, lods="0")
    bore_caps(info).to_object(kit, "bore cap", PB, wear=wear_cavity, lods="0")
    cont.to_object(kit, "contacts", BR, lods="0")
    # every part in front of the wall, under the proud limit
    for o in kit.parts:
        ys = [v.co.y for v in o.data.vertices]
        assert max(ys) <= 1e-9, "%s: %s behind the wall plane" % (label, o.name)
        assert -min(ys) / MM <= PROUD_LIMIT + 1e-6, "%s: %s %.2f proud" % (label, o.name, -min(ys) / MM)
    # screw seat level with the field (self-check c)
    for ci in info["csk"]:
        assert _close(ci["seat_h"], info["hfn"](*ci["centre"]), 0.051), "seat not level with the field"
        assert ci["seat_h"] + SCREW_CROWN <= PROUD_LIMIT + 1e-6, "screw crown above the proud limit"
    lod_counts = [total_tris(kit, "0")]
    if has_lods():
        lod_counts += build_lod_parts(kit, kind, info, face_top, gap_h, t20, crack, chip)
    kit.no_collider()
    kit.tag("outlet", "outlet_receptacle", "wall_flush")
    anchors(kit, info, face_top)
    common_meta(kit, module, W, H, face_top, {
        "kind": kind, "fieldH": round(crown * MM, 6), "seatH": round(info["csk"][0]["seat_h"] * MM, 6),
        "screws": len(info["csk"]), "faces": "5-20R" if t20 else "5-15R", "groundDown": True,
        "basePlateSlot": plate_slot, "baseDeviceSlot": NI, "slotNote": SLOT_NOTE})
    lod_check(label, lod_counts, module.BUDGET)
    kit.meta["trianglesByLod"] = lod_counts
    return info


def build_lod_parts(kit, kind, info, face_top, gap_h, t20, crack, chip):
    """Hand-built LOD1/LOD2 parts (spec 1.1 table), tagged fr_lods."""
    W, H, R, prof, crown, edge_h = plate_spec(kind)
    steel = kind == "steel"
    if steel:
        prof2 = ((0.0, 0.0), (0.2, SS_FIELD - 0.75), (SS_EDGE_R, SS_FIELD))
    else:
        prof2 = ((0.0, 0.0), (prof[3][0], prof[3][1]), (prof[-1][0], prof[-1][1]))
    p1, d1, _b, i1 = lod1_plate(W, H, R, prof2, edge_h, crown, crack=crack, chip=chip, steel=steel)
    hook1 = None
    if chip is not None:
        info1 = {"hw": W / 2, "hh": H / 2}
        chip1 = Chip(chip.corner, chip.planes, chip.floor_in, chip.floor_out, chip.floor_d, chip.reach)

        def hook1(bm):
            chip_bmesh(bm, info1, chip1)
    b1, k1 = lod1_device(i1, face_top, gap_h, t20=t20)
    plate_slot = AL if steel else TI
    p1.to_object(kit, "plate LOD1", plate_slot, lods="1", bm_hook=hook1)
    b1.to_object(kit, "device LOD1", NI, wear=wear_device, lods="1")
    dk = Mesh()
    dk.v, dk.f = d1.v + k1.v, d1.f + [tuple(i + len(d1.v) for i in f) for f in k1.f]
    dk.to_object(kit, "dark LOD1", PB, wear=wear_cavity, lods="1")
    p2, d2 = lod2_plate(W, H, edge_h, crown, face_top)
    p2.to_object(kit, "plate LOD2", plate_slot, lods="2")
    d2.to_object(kit, "faces LOD2", PB, lods="2")
    return [total_tris(kit, "1"), total_tris(kit, "2")]
