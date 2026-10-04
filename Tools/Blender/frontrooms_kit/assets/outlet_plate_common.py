"""O2 switches, blanks and jacks: shared plate, toggle and jack builders
(helper module: no NAME, no build). Group O2 of
Documentation/research/outlets/10_spec.md (§1.1-§1.5, §5.0, §5.2).

O2 does NOT import O1's or O3's helper (spec §5.0). Every §1.2 number below is
copied from the spec and asserted against a literal copy of the spec table
(SPEC_1_2), so O1's and O2's plates agree to 0.05 mm by construction.

PLATE FRAME (every O2 asset). Geometry is written in MILLIMETRES as
(x, h, z): x = across the plate (right as seen from the front), h = height
out of the wall (Blender -y), z = up. Mesh.to_object() converts once to
Blender metres (x, -h, z). Origin = the wall-face point at the plate centre;
the plate back lies on the wall plane h = 0 and nothing lies behind it; the
front faces -Y (Unity +Z). kitlib exports Blender (x, y, z) as Unity
(-x, z, -y), so anchors given here in Blender metres reach the sidecar in
Unity space.

What lives here
* §1.2 numbers and assert_shared_numbers().
* 2D outlines (rounded rectangles, circles, mitred polygon offsets).
* Mesh: a vertex/face collector with rings, bridges, fans, lathes, tubes and
  a constrained-Delaunay fill (mathutils.geometry.delaunay_2d_cdt) for faces
  with holes. Faces are wound explicitly (CCW in (x, z) = front-facing).
* thermoset_plate(): LOD0 plate of any size with through-openings and
  countersunk screw holes: the §1.2 edge profile swept round a 12-segment
  corner outline, the crowned field, the hollow back (rim 1.6 on the wall,
  cavity face 3.5), 0.5 mm 3-segment opening rounds and 82-degree
  countersinks with a dark bore.
* toggle_device(): the switch face, its bat slot and the bat handle;
  bat_clearance() slices the bat at the face and field planes.
* jack_6p(): a 6P modular jack insert with its cavity, latch notch and four
  brass contact wires.
* LOD1 / LOD2 hand-built plate and device builders (spec §1.1 table). They
  are built only when kitlib.Kit.make_lods exists (P-1/P-1b); until then each
  FBX holds LOD0 only. tools/o2_review.py (scratchpad) forces them on to
  count and render them without exporting.
* paint_wear(): the fr_wear colour attribute on every part (BYTE_COLOR, face
  corner; R hand grime, G edge wear, B cavity; 1 = untouched), the encoding
  of the interactables helpers.
* finalize(): the per-part normal recipe the interactables G1 probe proved
  survives kitlib.finish() and the FBX round trip (shade smooth -> sharp by
  SMOOTH_ANGLE -> WEIGHTED_NORMAL face area 50, keep sharp).
* finish_meta(): no collider, tags, anchors, LOD fields, the outlet dict for
  R3, and the triangle / proud / wall-plane asserts.
"""

import math

import kitlib
import bmesh
from mathutils import Vector
from mathutils.geometry import delaunay_2d_cdt

MM = 0.001
SMOOTH_ANGLE = 35.0

# ------------------------------------------------------------ slots (§1.5)
TI = "Prop_ThermosetIvory"   # new tint-only slot (P-4b, NEEDS APPROVAL)
NI = "Prop_NylonIvory"       # new tint-only slot (P-4b, NEEDS APPROVAL)
PB = "Prop_PlasticBlack"
BR = "Prop_Brass"
CH = "Prop_Chrome"
CE = "Prop_Ceramic"          # brown (existing)


def _hex(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))


def register_slots():
    """§1.5: preview colours for the two new tint-only slots (the hex values
    as 0-1 floats, as O3 registers Prop_NylonIvory). Roughness = 1 -
    smoothness. Never edits kitlib.SLOTS (register_slot = setdefault)."""
    kitlib.register_slot(TI, _hex("#E6DDC2"), 0.25, 0.0)   # smoothness 0.75
    kitlib.register_slot(NI, _hex("#E2D9BF"), 0.45, 0.0)   # smoothness 0.55


HAS_LODS = hasattr(kitlib.Kit, "make_lods")

# --------------------------------------------------- §1.2 shared numbers (mm)
PLATE_1G = (69.8, 114.3)
PLATE_2G = (115.9, 114.3)
GANG_X_2G = 23.0                      # 2-gang centres at x = +-23.0
CORNER_R = 2.0
CORNER_SEGS = 12                      # §1.1: 12 per plate corner
PROFILE = ((0.00, 0.00), (0.15, 1.20), (0.45, 2.60), (0.95, 3.80),
           (1.70, 4.70), (2.70, 5.25), (4.00, 5.50))
FIELD_INSET = 4.00
FIELD_EDGE_H = 5.50
FIELD_CROWN_H = 5.60
BACK_RIM_W = 1.6
CAVITY_H = 3.5
TOGGLE_OPENING = (10.3, 23.8, 1.0)    # w, h, corner r
PHONE_OPENING = (14.0, 12.0, 1.0)
TOGGLE_SCREW_Z = 30.15
BLANK_SCREW_Z = 41.65                 # blank and phone plates
CSK_D = 7.4                           # countersink at the field
CSK_ANGLE = 82.0
SCREW_HEAD_D = 6.6                    # Kit_OutletScrew (O1); seat level with the field
SCREW_CROWN = 1.0
SCREW_SLOT = (0.8, 0.6)               # w, depth (preview stand-in only; O1 owns the screw)
OPENING_ROUND = 0.5                   # §1.1: opening edges 0.5 mm, 3 segments
OPENING_ROUND_SEGS = 3
SLOT_CHAMFER = 0.15                   # §1.1: slot mouths 0.15 mm, 1 segment
DEVICE_ROUND = 0.6                    # §1.1: device-face edges 0.6 mm, 3 segments
# Toggle (§1.2 Devices)
SWITCH_FACE_BEHIND = 0.5
BAT_BASE = (7.5, 5.0)
BAT_TIP = (6.0, 4.0)
BAT_TIP_R = 2.0
BAT_LEN = 17.0
BAT_PIVOT_BEHIND = 2.0
BAT_ANGLE = 32.0
# 6P modular jack (§1.2 Devices)
JACK_FACE_BEHIND = 0.3
JACK_CAVITY = (9.9, 6.8)
JACK_CAVITY_DEPTH_SPEC = 12.0
LATCH_NOTCH = (3.2, 2.0)
CONTACT_D = 0.45
CONTACT_PITCH = 1.0
CONTACT_ANGLE = 20.0
PLUG_6P_W = 9.85
# Proud limits (§1.1, mm)
PROUD_PLATE = 7.5
PROUD_TOGGLE = 20.0
PROUD_DATA = 25.0
LOD_DISTANCES = (1.5, 4.0, 12.0)

# The §1.2 / §1.1 numbers as printed in the spec (2026-10-03).
# assert_shared_numbers() compares the working constants above against
# them: anyone who edits a number here has to edit it twice, on purpose.
SPEC_1_2 = {
    "PLATE_1G": (69.8, 114.3), "PLATE_2G": (115.9, 114.3), "GANG_X_2G": 23.0,
    "CORNER_R": 2.0, "CORNER_SEGS": 12,
    "PROFILE": ((0, 0), (0.15, 1.20), (0.45, 2.60), (0.95, 3.80), (1.70, 4.70), (2.70, 5.25), (4.00, 5.50)),
    "FIELD_CROWN_H": 5.60, "BACK_RIM_W": 1.6, "CAVITY_H": 3.5,
    "TOGGLE_OPENING": (10.3, 23.8, 1.0), "PHONE_OPENING": (14.0, 12.0, 1.0),
    "TOGGLE_SCREW_Z": 30.15, "BLANK_SCREW_Z": 41.65,
    "CSK_D": 7.4, "CSK_ANGLE": 82.0, "SCREW_HEAD_D": 6.6, "SCREW_CROWN": 1.0,
    "OPENING_ROUND": 0.5, "OPENING_ROUND_SEGS": 3, "SLOT_CHAMFER": 0.15, "DEVICE_ROUND": 0.6,
    "SWITCH_FACE_BEHIND": 0.5, "BAT_BASE": (7.5, 5.0), "BAT_TIP": (6.0, 4.0), "BAT_TIP_R": 2.0,
    "BAT_LEN": 17.0, "BAT_PIVOT_BEHIND": 2.0, "BAT_ANGLE": 32.0,
    "JACK_FACE_BEHIND": 0.3, "JACK_CAVITY": (9.9, 6.8), "JACK_CAVITY_DEPTH_SPEC": 12.0,
    "LATCH_NOTCH": (3.2, 2.0), "CONTACT_D": 0.45, "CONTACT_PITCH": 1.0, "CONTACT_ANGLE": 20.0,
    "PLUG_6P_W": 9.85, "PROUD_PLATE": 7.5, "PROUD_TOGGLE": 20.0, "PROUD_DATA": 25.0,
    "LOD_DISTANCES": (1.5, 4.0, 12.0),
}

# ------------------------------------------- O2 design numbers (ESTIMATE)
# Every number here is O2's reading of an ESTIMATE or of a gap in §1.2;
# build_o2_switches_blanks_and_jacks.md lists them.
R_FLOOR = 0.5            # profile rings inset past the corner radius keep a 0.5 corner (no mitre crease)
CSK_SEGS = 64            # §1.1 >= 64 on the visible countersink rim
BORE_SEGS = 32           # the cone bottom and the dark bore disc: hidden under the Ø6.6 head (2:1 bridge)
BORE_D = 4.0             # #6 clearance bore at the cone bottom (dark disc); keeps the cone in front of the cavity face
FIELD_STEINER = 15.0     # interior field points every 15 mm: the 0.1 mm crown is held to 0.006 mm
EDGE_WEAR_BAND = 1.5     # G = 0.9 on the outer 1.5 mm of every plate (rubbed edge)
BOSS_CLEAR = 0.35        # switch face to plate opening (duplex uses 0.4; 0.35 leaves a 0.15 flat round the bat slot)
BOSS_R = 0.6
BOSS_ROUND = 0.2         # 3 segments; 0.6 (§1.1) does not fit beside the bat slot on a 9.6 wide face
BOSS_SEGS = 4            # per boss corner
BOSS_NI_DEPTH = 0.6      # device colour on the boss side below the round, then dark
GAP_FLOOR_H = 3.0        # dark floor under the boss-to-opening gap (the device strap)
OPENING_SEGS = 6         # per opening corner (r 1.0: sagitta 0.009 mm)
BAT_SLOT = (8.6, 9.0, 0.5)    # bat slot in the switch face: w, h, r (0.55 a side round the 7.5 bat)
BAT_SLOT_SEGS = 4
BAT_SLOT_FLOOR_BELOW_PIVOT = 1.6   # under the bat's base ring (it reaches 1.33 below the pivot at 32 deg)
BAT_BASE_R = 1.5         # base section corner radius
BAT_TIP_R_USED = 1.95    # 2.0 clamped: a 4.0-thick section with r 2.0 has a zero straight
BAT_CAP = 1.4            # length of the domed tip (inside the 17 mm)
BAT_SEGS = 4             # per section corner (r 2: sagitta 0.038 mm = 0.1 px at 0.3 m, FOV 62, 1080p)
JACK_INSERT_CLEAR = 0.3
JACK_INSERT_R = 0.7
JACK_INSERT_ROUND = 0.3
JACK_INSERT_SEGS = 4
JACK_FLOOR_H = 0.05      # the cavity floor stops 0.05 mm in front of the wall plane (12 mm would cross y = 0)
JACK_CAVITY_TOP_Z = 4.4  # cavity z -2.4..+4.4, notch -4.4..-2.4: cavity + notch centred in the insert
CONTACT_SEGS = 6


# ------------------------------------------------------------------ asserts
def _close(a, b, tol=0.05):
    if isinstance(a, (tuple, list)):
        return len(a) == len(b) and all(_close(x, y, tol) for x, y in zip(a, b))
    return abs(float(a) - float(b)) <= tol


def assert_shared_numbers():
    g = globals()
    for key, want in SPEC_1_2.items():
        assert _close(g[key], want, 0.0005), "§1.2 drift: %s = %r, spec %r" % (key, g[key], want)
    assert _close(PROFILE[-1], (FIELD_INSET, FIELD_EDGE_H), 1e-9)
    # §5.2 self-check (c): the 6P cavity takes a 9.85 mm plug with >= 0.02 a side.
    assert (JACK_CAVITY[0] - PLUG_6P_W) / 2 >= 0.02 - 1e-9, "6P cavity too tight"
    # The bat slot keeps >= 0.5 a side round the bat base.
    assert (BAT_SLOT[0] - BAT_BASE[0]) / 2 >= 0.5, "bat slot too narrow"
    # The switch face keeps a flat between its round and the slot chamfer.
    bw = TOGGLE_OPENING[0] - 2 * BOSS_CLEAR
    assert bw / 2 - BOSS_ROUND - (BAT_SLOT[0] / 2 + SLOT_CHAMFER) >= 0.1, "switch face too thin beside the slot"


# ------------------------------------------------------------------ outlines
def lerp(a, b, t):
    return a + (b - a) * t


def rrect(cx, cz, hw, hh, r, segs):
    """Rounded rectangle, CCW in (x, z), starting at the right side. Corner
    radius is clamped below the half sizes so no two points coincide. segs
    = 0 gives a plain 4-point rectangle (r ignored)."""
    if segs <= 0:
        return [(cx + hw, cz - hh), (cx + hw, cz + hh), (cx - hw, cz + hh), (cx - hw, cz - hh)]
    r = max(0.02, min(r, hw - 0.01, hh - 0.01))
    pts = []
    for ox, oz, a0 in ((cx + hw - r, cz + hh - r, 0.0), (cx - hw + r, cz + hh - r, 90.0),
                       (cx - hw + r, cz - hh + r, 180.0), (cx + hw - r, cz - hh + r, 270.0)):
        for k in range(segs + 1):
            a = math.radians(a0 + 90.0 * k / segs)
            pts.append((ox + r * math.cos(a), oz + r * math.sin(a)))
    return pts


def circle(cx, cz, r, n, a0=0.0):
    return [(cx + r * math.cos(a0 + 2 * math.pi * k / n), cz + r * math.sin(a0 + 2 * math.pi * k / n)) for k in range(n)]


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
        k = 1.0 + n1.dot(n2)
        m = (n1 + n2) / k
        out.append((x1 + o * m.x, z1 + o * m.y))
    return out


def inside(poly, x, z):
    c = False
    n = len(poly)
    for i in range(n):
        x1, z1 = poly[i]
        x2, z2 = poly[(i + 1) % n]
        if (z1 > z) != (z2 > z):
            xi = x1 + (z - z1) * (x2 - x1) / (z2 - z1)
            if x < xi:
                c = not c
    return c


def dist_poly(poly, x, z):
    best = 1e9
    n = len(poly)
    for i in range(n):
        ax, az = poly[i]
        bx, bz = poly[(i + 1) % n]
        dx, dz = bx - ax, bz - az
        L = dx * dx + dz * dz
        t = 0.0 if L == 0 else max(0.0, min(1.0, ((x - ax) * dx + (z - az) * dz) / L))
        px, pz = ax + t * dx - x, az + t * dz - z
        best = min(best, math.sqrt(px * px + pz * pz))
    return best


def crown(W, H):
    """Field height above the wall: 5.50 at the field edge (inset 4.0),
    crowning to 5.60 at the plate centre ((1 - u^2)(1 - v^2) cushion)."""
    fx, fz = W / 2 - FIELD_INSET, H / 2 - FIELD_INSET

    def h(x, z):
        u, v = x / fx, z / fz
        return FIELD_EDGE_H + (FIELD_CROWN_H - FIELD_EDGE_H) * max(0.0, 1 - u * u) * max(0.0, 1 - v * v)
    return h


def round_steps(radius, segs):
    """Quarter-round stations (inset, drop) from a face edge into a wall:
    k = 0 is on the face (inset = radius, drop 0), k = segs is on the wall
    (inset 0, drop = radius)."""
    out = []
    for k in range(segs + 1):
        th = math.radians(90.0 * k / segs)
        out.append((radius * (1 - math.sin(th)), radius * (1 - math.cos(th))))
    return out


# --------------------------------------------------------------------- mesh
class Mesh:
    """Vertex/face collector in the plate frame (mm). Faces wound CCW in
    (x, z) face the front (-Y). bridge(A, B) between rings of one CCW
    outline faces: the front when B is inset from A at the same height;
    outward when B is above A (an island wall); into the hole when B is
    below A (a hole wall). In short, walk a lathe or body profile outside-up,
    across the top, then down any bore, and every face points out of the
    solid."""

    def __init__(self):
        self.v = []
        self.f = []

    def add(self, x, h, z):
        self.v.append((float(x), float(h), float(z)))
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

    def bridge_2to1(self, a, b, flip=False):
        """Bridge a ring of 2n points (a) to a ring of n points (b) with the
        same start angle: 3 triangles per b segment, wound as bridge()."""
        n = len(b)
        assert len(a) == 2 * n, "bridge_2to1: %d vs %d" % (len(a), len(b))
        for i in range(n):
            j = (i + 1) % n
            a0, a1, a2 = a[2 * i], a[2 * i + 1], a[(2 * i + 2) % (2 * n)]
            self.face((a0, a1, b[i]), flip)
            self.face((a1, b[j], b[i]), flip)
            self.face((a1, a2, b[j]), flip)

    def chain(self, rings, flip=False):
        for a, b in zip(rings, rings[1:]):
            self.bridge(a, b, flip)

    def fan(self, ring, apex, flip=False):
        n = len(ring)
        for i in range(n):
            self.face((ring[i], ring[(i + 1) % n], apex), flip)

    def cap(self, ring, flip=False):
        self.face(ring, flip)

    def body(self, outline_fn, walk):
        """Rings of outline_fn(inset) at heights h for (inset, h) in walk,
        bridged in order. Returns the rings."""
        rings = [self.ring(outline_fn(o), h) for o, h in walk]
        self.chain(rings)
        return rings

    def lathe(self, cx, cz, walk, segs, a0=0.0):
        """Revolve a (radius, h) walk about the h axis through (cx, cz).
        Radius 0 makes an apex (fan). Returns the rings (apex = [index])."""
        rings = []
        for r, h in walk:
            if r <= 1e-9:
                rings.append([self.add(cx, h, cz)])
            else:
                rings.append(self.ring(circle(cx, cz, r, segs, a0), h))
        for a, b in zip(rings, rings[1:]):
            if len(a) == 1 and len(b) > 1:
                n = len(b)
                for i in range(n):
                    self.face((a[0], b[(i + 1) % n], b[i]))
            elif len(b) == 1 and len(a) > 1:
                self.fan(a, b[0])
            elif len(a) > 1:
                self.bridge(a, b)
        return rings

    def tube(self, pts, radius, segs=CONTACT_SEGS, caps=True):
        """Round wire along a polyline of plate-frame points (mm)."""
        P = [Vector(p) for p in pts]
        rings = []
        for k, p in enumerate(P):
            if k == 0:
                t = (P[1] - P[0]).normalized()
            elif k == len(P) - 1:
                t = (P[-1] - P[-2]).normalized()
            else:
                t = ((P[k] - P[k - 1]).normalized() + (P[k + 1] - P[k]).normalized()).normalized()
            ref = Vector((1, 0, 0)) if abs(t.x) < 0.9 else Vector((0, 0, 1))
            s = t.cross(ref).normalized()
            u = s.cross(t).normalized()
            rings.append([self.add(*(p + (u * math.cos(2 * math.pi * i / segs) + s * math.sin(2 * math.pi * i / segs)) * radius))
                          for i in range(segs)])
        for a, b in zip(rings, rings[1:]):
            self.bridge(a, b)
        if caps:
            self.cap(list(reversed(rings[0])))
            self.cap(rings[-1])
        return rings

    def box(self, x0, x1, h0, h1, z0, z1, skip_back=True):
        """Axis box in the plate frame (the face at h0 against something is
        skipped by default)."""
        bot = self.ring([(x1, z0), (x1, z1), (x0, z1), (x0, z0)], h0)
        top = self.ring([(x1, z0), (x1, z1), (x0, z1), (x0, z0)], h1)
        self.bridge(bot, top)
        self.cap(top)
        if not skip_back:
            self.cap(bot, flip=True)
        return bot, top

    def tris(self):
        return sum(len(f) - 2 for f in self.f)

    def fill(self, outer, holes, h_fn=None, steiner=0.0, clear=None, flip=False):
        """Constrained Delaunay fill of the region inside the ``outer`` loop
        (mesh indices) minus ``holes``. Optional Steiner points on a grid of
        ``steiner`` mm take their height from h_fn. Triangles are wound CCW in
        (x, z) (front-facing), or the reverse with flip=True."""
        loops = [outer] + list(holes)
        coords, ids, edges = [], [], []
        polys = []
        for loop in loops:
            base = len(coords)
            poly = []
            for vi in loop:
                x, h, z = self.v[vi]
                coords.append((x, z))
                ids.append(vi)
                poly.append((x, z))
            polys.append(poly)
            n = len(loop)
            edges.extend((base + k, base + (k + 1) % n) for k in range(n))
        if steiner and h_fn is not None:
            clear = clear if clear is not None else 0.45 * steiner
            xs = [p[0] for p in polys[0]]
            zs = [p[1] for p in polys[0]]
            x0, x1, z0, z1 = min(xs), max(xs), min(zs), max(zs)
            nx, nz = max(1, round((x1 - x0) / steiner)), max(1, round((z1 - z0) / steiner))
            for i in range(1, nx):
                for j in range(1, nz):
                    x, z = x0 + (x1 - x0) * i / nx, z0 + (z1 - z0) * j / nz
                    if not inside(polys[0], x, z) or any(inside(p, x, z) for p in polys[1:]):
                        continue
                    if min(dist_poly(p, x, z) for p in polys) < clear:
                        continue
                    coords.append((x, z))
                    ids.append(self.add(x, h_fn(x, z), z))
        out = delaunay_2d_cdt([Vector(c) for c in coords], edges, [], 0, 1e-7)
        vco, _e, faces, orig_v = out[0], out[1], out[2], out[3]
        assert len(vco) == len(coords), "CDT added %d vertices (loops cross?)" % (len(vco) - len(coords))
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
            a, b, c = (self.v[i] for i in tri[:3])
            area = (b[0] - a[0]) * (c[2] - a[2]) - (b[2] - a[2]) * (c[0] - a[0])
            if area < 0:
                tri.reverse()
            self.face(tri, flip)
            added += 1
        return added

    def to_object(self, kit, name, slot, wear=None, lods="0"):
        bm = bmesh.new()
        bv = [bm.verts.new((x * MM, -h * MM, z * MM)) for x, h, z in self.v]
        for f in self.f:
            bm.faces.new([bv[i] for i in f])
        big = [f for f in bm.faces if len(f.verts) > 4]
        if big:
            bmesh.ops.triangulate(bm, faces=big, quad_method="BEAUTY", ngon_method="BEAUTY")
        loose = [v for v in bm.verts if not v.link_faces]
        if loose:
            bmesh.ops.delete(bm, geom=loose, context="VERTS")
        obj = kit._new_object(name, bm, slot, "metres", "xz")
        paint_wear(obj, wear)
        obj["fr_lods"] = lods
        return obj


# ------------------------------------------------------------------- wear
def to_plate(co):
    """Blender metres -> plate frame mm (x, h, z)."""
    return (co[0] / MM, -co[1] / MM, co[2] / MM)


def paint_wear(obj, fn=None):
    """fr_wear colour attribute (BYTE_COLOR, FACE CORNER), as the
    interactables helpers: R hand grime, G edge wear, B cavity; 1 = clean.
    fn(p, n, slot) gets the plate-frame point (mm) and the face normal in
    plate frame. Dark (Prop_PlasticBlack) faces get B <= 0.35."""
    me = obj.data
    attr = me.color_attributes.get("fr_wear") or me.color_attributes.new("fr_wear", "BYTE_COLOR", "CORNER")
    names = [m.name if m is not None else "" for m in me.materials]
    for poly in me.polygons:
        n = (poly.normal[0], -poly.normal[1], poly.normal[2])
        slot = names[poly.material_index] if poly.material_index < len(names) else ""
        for li in poly.loop_indices:
            p = to_plate(me.vertices[me.loops[li].vertex_index].co)
            c = fn(p, n, slot) if fn is not None else (1.0, 1.0, 1.0)
            if slot == PB:
                c = (c[0], c[1], min(c[2], 0.35))
            attr.data[li].color_srgb = tuple(max(0.0, min(1.0, x)) for x in c) + (1.0,)
    me.color_attributes.active_color = attr
    return obj


def plate_wear(W, H, hand_boxes=(), margin=9.0, hand=0.85, edge=0.9):
    """Plate wear: R = ``hand`` within ``margin`` mm of any (cx, cz, hw, hh)
    device box (§1.1: the rim round the device faces, where fingers go);
    G = ``edge`` on the outer EDGE_WEAR_BAND of the plate."""
    def fn(p, n, slot):
        x, h, z = p
        r = 1.0
        for cx, cz, hw, hh in hand_boxes:
            if abs(x - cx) <= hw + margin and abs(z - cz) <= hh + margin:
                r = hand
                break
        g = edge if (abs(x) > W / 2 - EDGE_WEAR_BAND or abs(z) > H / 2 - EDGE_WEAR_BAND) else 1.0
        return (r, g, 1.0)
    return fn


def uniform_wear(r=1.0, g=1.0, b=1.0):
    return lambda p, n, slot: (r, g, b)


# ------------------------------------------------------------------ openings
class Opening:
    """A through-opening in a plate: kind 'rrect' (w, h, r) or 'circle' (d)."""

    def __init__(self, kind, cx, cz, w=0.0, h=0.0, r=0.0, d=0.0, segs=OPENING_SEGS, round_=True):
        self.kind, self.cx, self.cz = kind, cx, cz
        self.w, self.h, self.r, self.d, self.segs = w, h, r, d, segs
        self.round = round_

    def outline(self, o=0.0):
        if self.kind == "rrect":
            return rrect(self.cx, self.cz, self.w / 2 + o, self.h / 2 + o, self.r + o, self.segs)
        return circle(self.cx, self.cz, self.d / 2 + o, self.segs)

    def box(self):
        if self.kind == "rrect":
            return (self.cx, self.cz, self.w / 2, self.h / 2)
        return (self.cx, self.cz, self.d / 2, self.d / 2)


# --------------------------------------------------------------- LOD0 plate
def thermoset_plate(kit, W, H, screws, openings, wear=None, name="plate", slot=TI):
    """LOD0 thermoset plate (spec §1.1 table, §1.2). Returns a dict with the
    crown function, the screw seats (plate frame mm) and the plate mesh.
    An Opening with round_=False gets a plain vertical wall (hidden under a
    device flange, so the 0.5 round would cost triangles nobody sees)."""
    m = Mesh()
    hf = crown(W, H)
    rings = []
    for d, h in PROFILE:
        pts = rrect(0.0, 0.0, W / 2 - d, H / 2 - d, max(CORNER_R - d, R_FLOOR), CORNER_SEGS)
        rings.append(m.ring(pts, h))
    m.chain(rings)
    # Hollow back: rim 1.6 wide on the wall, skirt wall, cavity face 3.5.
    rim_pts = rrect(0.0, 0.0, W / 2 - BACK_RIM_W, H / 2 - BACK_RIM_W, max(CORNER_R - BACK_RIM_W, R_FLOOR), CORNER_SEGS)
    rim_wall = m.ring(rim_pts, 0.0)
    m.bridge(rings[0], rim_wall, flip=True)           # on the wall: faces +Y
    rim_top = m.ring(rim_pts, CAVITY_H)
    m.bridge(rim_wall, rim_top, flip=True)            # skirt inner wall: faces the hollow
    front_holes, back_holes = [], []
    for op in openings:
        steps = round_steps(OPENING_ROUND, OPENING_ROUND_SEGS) if op.round else [(0.0, 0.0)]
        rr = [m.ring(op.outline(o), lambda x, z, dh=dh: hf(x, z) - dh) for o, dh in steps]
        m.chain(rr)
        bottom = m.ring(op.outline(0.0), CAVITY_H)
        m.bridge(rr[-1], bottom)
        front_holes.append(rr[0])
        back_holes.append(bottom)
    dark = Mesh()
    seats = []
    depth = (CSK_D / 2 - BORE_D / 2) / math.tan(math.radians(CSK_ANGLE / 2))
    for sx, sz in screws:
        hs = hf(sx, sz)
        top = m.ring(circle(sx, sz, CSK_D / 2, CSK_SEGS), hf)
        bot = m.ring(circle(sx, sz, BORE_D / 2, BORE_SEGS), hs - depth)
        m.bridge_2to1(top, bot)
        front_holes.append(top)
        dark.cap(dark.ring(circle(sx, sz, BORE_D / 2, BORE_SEGS), hs - depth))
        seats.append((sx, hs, sz))
        assert hs - depth > CAVITY_H, "countersink breaks through the cavity face"
    m.fill(rings[-1], front_holes, hf, steiner=FIELD_STEINER)
    m.fill(rim_top, back_holes, flip=True)
    plate = m.to_object(kit, name, slot, wear=wear or plate_wear(W, H))
    bores = dark.to_object(kit, name + " screw bores", PB, wear=uniform_wear(1.0, 1.0, 0.2)) if screws else None
    info = {"hf": hf, "seats": seats, "W": W, "H": H, "mesh": m, "plate": plate, "bores": bores,
            "openings": openings, "csk_depth": depth}
    check_plate(info)
    return info


def check_plate(info):
    """§1.2 plate asserts on the built mesh (mm): outline, profile, field and
    crown, back, openings, countersinks."""
    m, W, H = info["mesh"], info["W"], info["H"]
    V = m.v
    wall = [v for v in V if abs(v[1]) < 1e-9]
    assert _close(max(abs(v[0]) for v in wall), W / 2) and _close(max(abs(v[2]) for v in wall), H / 2), "plate outline"
    assert _close(max(v[0] for v in wall) - min(v[0] for v in wall), W) and _close(max(v[2] for v in wall) - min(v[2] for v in wall), H), "plate size"
    for d, h in PROFILE:      # the right side's straight run of each profile ring
        hits = [v for v in V if abs(v[0] - (W / 2 - d)) < 1e-6 and abs(v[1] - h) < 1e-6]
        assert hits, "profile point (%.2f, %.2f) missing" % (d, h)
    hmax = max(v[1] for v in V)
    assert hmax <= FIELD_CROWN_H + 0.005 and hmax >= FIELD_EDGE_H - 1e-6, "plate depth %.3f" % hmax
    assert _close(info["hf"](0.0, 0.0), FIELD_CROWN_H, 1e-6) and _close(info["hf"](W / 2 - FIELD_INSET, 0.0), FIELD_EDGE_H, 1e-6)
    assert any(abs(v[1] - CAVITY_H) < 1e-9 and _close(abs(v[0]), W / 2 - BACK_RIM_W) for v in V), "back rim / cavity"
    for op in info["openings"]:
        pts = op.outline(0.0)
        xs, zs = [p[0] for p in pts], [p[1] for p in pts]
        w, h = (op.w, op.h) if op.kind == "rrect" else (op.d, op.d)
        tol = 0.05 if op.kind == "rrect" else max(0.05, op.d / 2 * (1 - math.cos(math.pi / op.segs)) * 2 + 1e-6)
        assert _close(max(xs) - min(xs), w, tol) and _close(max(zs) - min(zs), h, tol), "opening size"
        assert _close((max(xs) + min(xs)) / 2, op.cx) and _close((max(zs) + min(zs)) / 2, op.cz), "opening centre"
    half = math.degrees(math.atan((CSK_D / 2 - BORE_D / 2) / info["csk_depth"]))
    assert _close(2 * half, CSK_ANGLE, 0.01), "countersink angle"
    for sx, sh, sz in info["seats"]:
        assert _close(sh, info["hf"](sx, sz), 1e-6), "screw seat not level with the field"


# ----------------------------------------------------- LOD0 toggle device
def toggle_device(kit, gx, gz, hf, angle=BAT_ANGLE):
    """Switch face (NI) filling the plate opening with BOSS_CLEAR, 0.5 behind
    the field; a bat slot with a 0.15 chamfer; the bat handle (NI) pivoting
    2.0 behind the field at ``angle`` degrees up. Dark (PB) slot walls, boss
    sides and the gap floor. Returns the parts and the bat mesh."""
    field = hf(gx, gz)
    face_h = field - SWITCH_FACE_BEHIND
    pivot_h = field - BAT_PIVOT_BEHIND
    bw, bh = TOGGLE_OPENING[0] - 2 * BOSS_CLEAR, TOGGLE_OPENING[1] - 2 * BOSS_CLEAR
    sw, sh, sr = BAT_SLOT
    slot_floor = pivot_h - BAT_SLOT_FLOOR_BELOW_PIVOT
    m, d = Mesh(), Mesh()
    rings = [m.ring(rrect(gx, gz, bw / 2 - o, bh / 2 - o, BOSS_R - o, BOSS_SEGS), face_h - dh)
             for o, dh in round_steps(BOSS_ROUND, 3)]
    for a, b in zip(rings, rings[1:]):
        m.bridge(b, a)                               # island: lower ring first
    outline = rrect(gx, gz, bw / 2, bh / 2, BOSS_R, BOSS_SEGS)
    side = m.ring(outline, face_h - BOSS_ROUND - BOSS_NI_DEPTH)
    m.bridge(side, rings[-1])
    st = m.ring(rrect(gx, gz, sw / 2 + SLOT_CHAMFER, sh / 2 + SLOT_CHAMFER, sr + SLOT_CHAMFER, BAT_SLOT_SEGS), face_h)
    sm = m.ring(rrect(gx, gz, sw / 2, sh / 2, sr, BAT_SLOT_SEGS), face_h - SLOT_CHAMFER)
    m.bridge(st, sm)
    m.fill(rings[0], [st])
    boss = m.to_object(kit, "switch face", NI, wear=uniform_wear(0.85, 1.0, 1.0))
    # Dark: lower boss sides, the gap floor, the slot walls and floor.
    a = d.ring(outline, GAP_FLOOR_H)
    b = d.ring(outline, face_h - BOSS_ROUND - BOSS_NI_DEPTH)
    d.bridge(a, b)
    gap_outer = d.ring(rrect(gx, gz, TOGGLE_OPENING[0] / 2 + 0.3, TOGGLE_OPENING[1] / 2 + 0.3,
                             BOSS_R + BOSS_CLEAR + 0.3, BOSS_SEGS), GAP_FLOOR_H)
    d.bridge(gap_outer, a)
    s0 = d.ring(rrect(gx, gz, sw / 2, sh / 2, sr, BAT_SLOT_SEGS), face_h - SLOT_CHAMFER)
    s1 = d.ring(rrect(gx, gz, sw / 2, sh / 2, sr, BAT_SLOT_SEGS), slot_floor)
    d.bridge(s0, s1)
    d.cap(s1)
    dark = d.to_object(kit, "switch dark", PB, wear=uniform_wear(1.0, 1.0, 0.2))
    bat_mesh = bat(gx, gz, pivot_h, angle)
    tip_zone = lambda p, n, s: (0.8 if math.hypot(p[1] - pivot_h, p[2] - gz) > 11.0 else 1.0,
                                0.9 if math.hypot(p[1] - pivot_h, p[2] - gz) > 15.5 else 1.0, 1.0)
    handle = bat_mesh.to_object(kit, "toggle bat", NI, wear=tip_zone)
    return {"boss": boss, "dark": dark, "bat": handle, "bat_mesh": bat_mesh, "face_h": face_h,
            "pivot_h": pivot_h, "field": field, "slot": (gx, gz, sw / 2, sh / 2), "slot_floor": slot_floor}


def bat(gx, gz, pivot_h, angle=BAT_ANGLE, segs=BAT_SEGS):
    """Toggle bat: base 7.5 x 5.0 tapering to 6.0 x 4.0 (r 2.0) over 17 mm
    from a pivot, with a domed tip; ``angle`` degrees up from the wall
    normal (negative = down)."""
    m = Mesh()
    th = math.radians(angle)
    ah, az = math.cos(th), math.sin(th)          # axis (h, z)
    th_, tz_ = -math.sin(th), math.cos(th)       # thickness direction (h, z)
    body = BAT_LEN - BAT_CAP

    def P(s, a, b):
        return (gx + a, pivot_h + s * ah + b * th_, gz + s * az + b * tz_)
    stations = []
    for s in (0.0, 2.5, body):                   # the taper is linear: no station between 2.5 and the tip
        f = s / body
        stations.append((s, lerp(BAT_BASE[0], BAT_TIP[0], f), lerp(BAT_BASE[1], BAT_TIP[1], f), lerp(BAT_BASE_R, BAT_TIP_R_USED, f)))
    for phi in (28.0, 56.0, 80.0):
        k = math.cos(math.radians(phi))
        stations.append((body + BAT_CAP * math.sin(math.radians(phi)), BAT_TIP[0] * k, BAT_TIP[1] * k, BAT_TIP_R_USED * k))
    rings = [[m.add(*P(s, a, b)) for a, b in rrect(0.0, 0.0, w / 2, t / 2, r, segs)] for s, w, t, r in stations]
    m.chain(rings)
    m.fan(rings[-1], m.add(*P(BAT_LEN, 0.0, 0.0)))
    m.cap(list(reversed(rings[0])))              # base (hidden in the slot)
    return m


def bat_clearance(bat_mesh, gx, gz, face_h, field, slot_floor):
    """Smallest clearance (mm) of the bat, as built and mirrored in z (the
    180-degree roll = OFF), to (a) the plate opening between the switch face
    and the field and (b) the bat slot between the slot floor and the face.
    Exact for the mesh: every edge is sliced at the two planes and every
    vertex between them is tested."""
    ow, oh = TOGGLE_OPENING[0] / 2, TOGGLE_OPENING[1] / 2
    sw, sh = BAT_SLOT[0] / 2, BAT_SLOT[1] / 2
    V = bat_mesh.v
    edges = set()
    for f in bat_mesh.f:
        for i in range(len(f)):
            a, b = f[i], f[(i + 1) % len(f)]
            edges.add((min(a, b), max(a, b)))
    pts = {"opening": [], "slot": []}
    for key, lo, hi in (("opening", face_h, field), ("slot", slot_floor, face_h)):
        for x, h, z in V:
            if lo <= h <= hi:
                pts[key].append((x, z))
        for a, b in edges:
            pa, pb = V[a], V[b]
            for plane in (lo, hi):
                if (pa[1] - plane) * (pb[1] - plane) < 0:
                    t = (plane - pa[1]) / (pb[1] - pa[1])
                    pts[key].append((lerp(pa[0], pb[0], t), lerp(pa[2], pb[2], t)))
    best = {}
    for key, (hw, hh) in (("opening", (ow, oh)), ("slot", (sw, sh))):
        c = 1e9
        for sign in (1.0, -1.0):
            for x, z in pts[key]:
                c = min(c, hw - abs(x - gx), hh - abs(sign * (z - gz)))
        best[key] = c
    return best


# --------------------------------------------------------- LOD0 6P jack
def jack_6p(kit, gx, gz, hf, opening):
    """6P modular jack insert (NI) in a plate opening: face 0.3 behind the
    field; cavity 9.9 x 6.8 with the 3.2 x 2.0 latch notch down; 0.15 mouth
    chamfer; dark walls to 0.05 in front of the wall plane; four brass
    contact wires (Ø0.45, 1.0 pitch) angled 20 degrees from the cavity top."""
    face_h = hf(gx, gz) - JACK_FACE_BEHIND
    iw, ih = opening.w - 2 * JACK_INSERT_CLEAR, opening.h - 2 * JACK_INSERT_CLEAR
    cw, ch = JACK_CAVITY
    nw, nh = LATCH_NOTCH
    zt = gz + JACK_CAVITY_TOP_Z
    zb = zt - ch
    cav = [(gx - cw / 2, zb), (gx - nw / 2, zb), (gx - nw / 2, zb - nh), (gx + nw / 2, zb - nh),
           (gx + nw / 2, zb), (gx + cw / 2, zb), (gx + cw / 2, zt), (gx - cw / 2, zt)]
    m, d = Mesh(), Mesh()
    rings = [m.ring(rrect(gx, gz, iw / 2 - o, ih / 2 - o, JACK_INSERT_R - o, JACK_INSERT_SEGS), face_h - dh)
             for o, dh in round_steps(JACK_INSERT_ROUND, 3)]
    for a, b in zip(rings, rings[1:]):
        m.bridge(b, a)
    outline = rrect(gx, gz, iw / 2, ih / 2, JACK_INSERT_R, JACK_INSERT_SEGS)
    side = m.ring(outline, face_h - JACK_INSERT_ROUND - BOSS_NI_DEPTH)
    m.bridge(side, rings[-1])
    mouth_top = m.ring(offset_poly(cav, SLOT_CHAMFER), face_h)
    mouth_bot = m.ring(cav, face_h - SLOT_CHAMFER)
    m.bridge(mouth_top, mouth_bot)
    m.fill(rings[0], [mouth_top])
    insert = m.to_object(kit, "jack insert", NI)
    a = d.ring(outline, GAP_FLOOR_H)
    b = d.ring(outline, face_h - JACK_INSERT_ROUND - BOSS_NI_DEPTH)
    d.bridge(a, b)
    gap_outer = d.ring(rrect(gx, gz, opening.w / 2 + 0.3, opening.h / 2 + 0.3, opening.r + 0.3, JACK_INSERT_SEGS), GAP_FLOOR_H)
    d.bridge(gap_outer, a)
    w0 = d.ring(cav, face_h - SLOT_CHAMFER)
    w1 = d.ring(cav, JACK_FLOOR_H)
    d.bridge(w0, w1)
    d.cap(w1)
    dark = d.to_object(kit, "jack cavity", PB, wear=uniform_wear(1.0, 1.0, 0.2))
    c = Mesh()
    ca = math.radians(CONTACT_ANGLE)
    for i in range(4):
        x = gx + (i - 1.5) * CONTACT_PITCH
        p0 = (x, face_h - 0.9, zt + 0.25)                      # out of the comb in the top wall
        p1 = (x, face_h - 1.35, zt - 0.30)                     # the bend
        p2 = (x, p1[1] - 3.6 * math.cos(ca), p1[2] - 3.6 * math.sin(ca))
        c.tube([p0, p1, p2], CONTACT_D / 2)
    contacts = c.to_object(kit, "jack contacts", BR)
    return {"insert": insert, "dark": dark, "contacts": contacts, "face_h": face_h,
            "cavity": (cw, ch), "cavity_poly": cav, "floor_h": JACK_FLOOR_H}


def check_jack(info):
    """§5.2 (c): a 9.85 mm plug block enters the cavity with >= 0.02 a side;
    the cavity floor stays in front of the wall plane."""
    cav = info["cavity_poly"]
    xs = [p[0] for p in cav]
    width = max(xs) - min(xs)
    assert _close(width, JACK_CAVITY[0], 1e-6), "6P cavity width %.3f" % width
    side = (width - PLUG_6P_W) / 2
    assert side >= 0.02 - 1e-9, "6P plug clearance %.3f" % side
    assert info["floor_h"] > 0.0, "jack floor behind the wall"
    return side


# --------------------------------------------------- LOD1 / LOD2 (hand-built)
LOD1_PROFILE = ((0.00, 0.00), (0.95, 3.80), (4.00, 5.50))   # "2-step edge profile"
LOD1_CORNER_SEGS = 4
LOD1_SCREW_SEGS = 24


def plate_lod1(kit, W, H, screws, openings, slot=TI, lods="1"):
    """LOD1 (1.5-4 m): 2-step edge, 4-segment corners, no back, a plain
    screw stand-in (head disc + slot) so the plate does not pop when the
    separate screws stop drawing at 1.5 m."""
    m = Mesh()
    hf = crown(W, H)
    rings = [m.ring(rrect(0.0, 0.0, W / 2 - d, H / 2 - d, max(CORNER_R - d, R_FLOOR), LOD1_CORNER_SEGS), h)
             for d, h in LOD1_PROFILE]
    m.chain(rings)
    holes = []
    for op in openings:
        lo = Opening(op.kind, op.cx, op.cz, op.w, op.h, op.r, op.d, segs=2 if op.kind == "rrect" else 24)
        top = m.ring(lo.outline(0.0), hf)
        bot = m.ring(lo.outline(0.0), hf(op.cx, op.cz) - 1.2)
        m.bridge(top, bot)
        holes.append(top)
    d = Mesh()
    for sx, sz in screws:
        hs = hf(sx, sz)
        top = m.ring(circle(sx, sz, CSK_D / 2, LOD1_SCREW_SEGS), hf)
        mid = m.ring(circle(sx, sz, SCREW_HEAD_D / 2, LOD1_SCREW_SEGS), hs - 0.35)
        m.bridge(top, mid)
        head = m.ring(circle(sx, sz, SCREW_HEAD_D / 2, LOD1_SCREW_SEGS), hs)
        m.bridge(mid, head)                          # head rim: island wall, lower ring first
        m.fan(head, m.add(sx, hs + SCREW_CROWN * 0.8, sz))
        holes.append(top)
        sl = rrect(sx, sz, SCREW_HEAD_D / 2 - 0.5, 0.4, 0.05, 0)
        d.cap(d.ring(sl, hs + SCREW_CROWN * 0.55))
    m.fill(rings[-1], holes)
    out = [m.to_object(kit, "plate lod1", slot, lods=lods)]
    if screws:
        out.append(d.to_object(kit, "screw slots lod1", PB, lods=lods))
    return out, hf


def oct_outline(hw, hh, c):
    return [(hw, -hh + c), (hw, hh - c), (hw - c, hh), (-hw + c, hh), (-hw, hh - c), (-hw, -hh + c), (-hw + c, -hh), (hw - c, -hh)]


def plate_lod2(kit, W, H, slot=TI, lods="2", bevel=3.0, top_h=FIELD_EDGE_H + 0.05):
    """LOD2 (4-12 m): an 8-vertex rounded outline at full height with one
    bevel ring to the wall."""
    m = Mesh()
    base = m.ring(oct_outline(W / 2, H / 2, 1.2), 0.0)
    top = m.ring(oct_outline(W / 2 - bevel, H / 2 - bevel, 0.6), top_h)
    m.bridge(base, top)
    m.cap(top)
    return [m.to_object(kit, "plate lod2", slot, lods=lods)]


def dark_rect(kit, cx, cz, hw, hh, h, name, lods, slot=PB):
    m = Mesh()
    m.cap(m.ring([(cx + hw, cz - hh), (cx + hw, cz + hh), (cx - hw, cz + hh), (cx - hw, cz - hh)], h))
    return m.to_object(kit, name, slot, lods=lods)


def dark_poly(kit, pts, h, name, lods, slot=PB):
    m = Mesh()
    m.cap(m.ring(pts, h))
    return m.to_object(kit, name, slot, lods=lods)


def toggle_lod1(kit, gx, gz, hf, lods="1"):
    field = hf(gx, gz)
    face_h = field - SWITCH_FACE_BEHIND
    pivot_h = field - BAT_PIVOT_BEHIND
    bw, bh = TOGGLE_OPENING[0] - 2 * BOSS_CLEAR, TOGGLE_OPENING[1] - 2 * BOSS_CLEAR
    m = Mesh()
    top = m.ring(rrect(gx, gz, bw / 2, bh / 2, BOSS_R, 2), face_h)
    slot = m.ring(rrect(gx, gz, BAT_SLOT[0] / 2, BAT_SLOT[1] / 2, BAT_SLOT[2], 1), face_h)
    m.fill(top, [slot])
    boss = m.to_object(kit, "switch face lod1", NI, lods=lods)
    hole = dark_rect(kit, gx, gz, BAT_SLOT[0] / 2, BAT_SLOT[1] / 2, face_h - 0.5, "bat slot lod1", lods)
    b = bat(gx, gz, pivot_h, BAT_ANGLE, segs=2)
    return [boss, hole, b.to_object(kit, "toggle bat lod1", NI, lods=lods)]


def toggle_lod2(kit, gx, gz, hf, lods="2"):
    """Dark opening quad and the bat as a tapered 4-sided box."""
    field = hf(gx, gz)
    hole = dark_rect(kit, gx, gz, TOGGLE_OPENING[0] / 2 - 0.4, TOGGLE_OPENING[1] / 2 - 0.4, field + 0.05, "toggle opening lod2", lods)
    m = Mesh()
    th = math.radians(BAT_ANGLE)
    pivot = field - BAT_PIVOT_BEHIND

    def sec(s, w, t):
        out = []
        for a, b in ((w / 2, -t / 2), (w / 2, t / 2), (-w / 2, t / 2), (-w / 2, -t / 2)):
            out.append(m.add(gx + a, pivot + s * math.cos(th) - b * math.sin(th), gz + s * math.sin(th) + b * math.cos(th)))
        return out
    r0 = sec(2.0, BAT_BASE[0] * 0.95, BAT_BASE[1] * 0.95)
    r1 = sec(BAT_LEN, BAT_TIP[0], BAT_TIP[1])
    m.bridge(r0, r1)
    m.cap(r1)
    return [hole, m.to_object(kit, "toggle bat lod2", NI, lods=lods)]


def jack_lod1(kit, gx, gz, hf, opening, lods="1"):
    """6P jack at LOD1: the insert face as a flat ring and the cavity as a
    0.5 mm-deep black inset (§1.1 table: no contacts)."""
    face_h = hf(gx, gz) - JACK_FACE_BEHIND
    iw, ih = opening.w - 2 * JACK_INSERT_CLEAR, opening.h - 2 * JACK_INSERT_CLEAR
    cw, ch = JACK_CAVITY
    zt = gz + JACK_CAVITY_TOP_Z
    m = Mesh()
    top = m.ring(rrect(gx, gz, iw / 2, ih / 2, JACK_INSERT_R, 1), face_h)
    cav = m.ring(rrect(gx, (zt + zt - ch - LATCH_NOTCH[1]) / 2, cw / 2, (ch + LATCH_NOTCH[1]) / 2, 0.0, 0), face_h)
    m.fill(top, [cav])
    out = [m.to_object(kit, "jack insert lod1", NI, lods=lods)]
    out.append(dark_rect(kit, gx, (zt + zt - ch - LATCH_NOTCH[1]) / 2, cw / 2, (ch + LATCH_NOTCH[1]) / 2, face_h - 0.5, "jack cavity lod1", lods))
    return out


# ------------------------------------------------------------------ finish
def finalize(kit, smooth_angle=SMOOTH_ANGLE):
    """Per-part normal recipe proven by the interactables G1 probe
    (build_g1 §2: 0.00 deg drift after finish() and the FBX round trip):
    shade smooth -> sharp by angle (= the module's SMOOTH_ANGLE) ->
    WEIGHTED_NORMAL (face area, weight 50, keep sharp); finish() applies the
    modifier, joins and re-smooths with the same angle."""
    for obj in kit.parts:
        me = obj.data
        me.shade_smooth()
        me.set_sharp_from_angle(angle=math.radians(smooth_angle))
        mod = obj.modifiers.new("wn", "WEIGHTED_NORMAL")
        mod.mode = "FACE_AREA"
        mod.weight = 50
        mod.keep_sharp = True


def lod_parts(kit, level):
    return [o for o in kit.parts if str(level) in str(o.get("fr_lods", "0"))]


def tris_of(objs):
    return sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in objs)


def bounds_mm(objs):
    lo = [1e9] * 3
    hi = [-1e9] * 3
    for o in objs:
        for v in o.data.vertices:
            p = to_plate(o.matrix_world @ v.co)
            for i in range(3):
                lo[i] = min(lo[i], p[i])
                hi[i] = max(hi[i], p[i])
    return lo, hi


def finish_meta(kit, module, family, W, H, proud_limit, seats, top_z=None, extra_tags=(), extra=None):
    """Sidecar meta (§1.1): no collider, tags, screw and plate_top anchors,
    LOD fields and the outlet dict for R3. Asserts LOD0 triangles within
    +-15 % of the §1.3 budget, the proud limit and nothing behind the wall;
    prints the LOD1/LOD2 counts when those parts were built."""
    budget = module.BUDGET
    kit.no_collider()
    kit.tag("outlet", family, "wall_flush", *extra_tags)
    for i, (sx, sh, sz) in enumerate(seats):
        kit.anchor("screw_%d" % i, (sx * MM, -sh * MM, sz * MM))
    kit.anchor("plate_top", (0.0, 0.0, (H / 2 if top_z is None else top_z) * MM))
    lod0 = lod_parts(kit, 0)
    lo, hi = bounds_mm(lod0)
    proud = hi[1]
    kit.meta["lodDistances"] = list(module.LOD_DISTANCES)
    kit.meta["lodRatios"] = [1.0, round(module.LOD1_RATIO, 3), round(module.LOD2_RATIO, 3)]
    kit.meta["lodBudget"] = list(budget)
    kit.meta["lodHandBuilt"] = True
    kit.meta["lodNote"] = ("LOD1/LOD2 are hand-built part sets (fr_lods); exported only once kitlib "
                           "Kit.make_lods exists (P-1/P-1b). Until then this FBX holds LOD0 only.")
    out = {"plateW": round(W * MM, 5), "plateH": round(H * MM, 5), "proud": round(proud * MM, 5),
           "screws": len(seats), "family": family}
    if extra:
        out.update(extra)
    kit.meta["outlet"] = out
    kit.meta["partFrame"] = "origin = wall-face point at the plate centre; front = Unity +Z (kit -Y); +Y up (10_spec §1.1)"
    n0 = tris_of(lod0)
    print("[o2] %s LOD0: %d tris (budget %d, %+.1f %%), bounds mm x %.2f..%.2f h %.2f..%.2f z %.2f..%.2f, proud %.2f (limit %.1f)" % (
        kit.name, n0, budget[0], 100.0 * (n0 - budget[0]) / budget[0], lo[0], hi[0], lo[1], hi[1], lo[2], hi[2], proud, proud_limit))
    for o in lod0:
        print("[o2]    %-26s %5d tris  %s" % (o.name, tris_of([o]), o.data.materials[0].name))
    for lv in (1, 2):
        parts = lod_parts(kit, lv)
        if parts:
            n = tris_of(parts)
            print("[o2] %s LOD%d: %d tris (budget %d, %+.1f %%)" % (kit.name, lv, n, budget[lv], 100.0 * (n - budget[lv]) / budget[lv]))
            for o in parts:
                print("[o2]    %-26s %5d tris  %s" % (o.name, tris_of([o]), o.data.materials[0].name))
    assert abs(n0 - budget[0]) <= 0.15 * budget[0], "%s: LOD0 %d tris vs budget %d" % (kit.name, n0, budget[0])
    assert proud <= proud_limit + 1e-6, "%s: proud %.2f > %.1f" % (kit.name, proud, proud_limit)
    assert lo[1] >= -1e-6, "%s: geometry behind the wall plane (%.3f mm)" % (kit.name, lo[1])
    assert _close(hi[0] - lo[0], W, 0.05) and _close(hi[2] - lo[2], H, 0.05), \
        "%s: bounds %.2f x %.2f vs %.1f x %.1f" % (kit.name, hi[0] - lo[0], hi[2] - lo[2], W, H)
    kit.meta["o2Check"] = {"tris": [n0] + [tris_of(lod_parts(kit, lv)) if lod_parts(kit, lv) else None for lv in (1, 2)],
                           "boundsMm": [lo, hi]}
    return n0, lo, hi


def begin(kit):
    register_slots()
    assert_shared_numbers()


def end(kit, module, family, W, H, proud_limit, seats, **kw):
    finalize(kit, module.SMOOTH_ANGLE)
    return finish_meta(kit, module, family, W, H, proud_limit, seats, **kw)
