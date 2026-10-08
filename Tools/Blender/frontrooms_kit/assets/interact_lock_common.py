"""G2 lock hardware: shared data and geometry (helper module, no NAME, no build).

Spec: Documentation/research/interactables/10_spec.md §3 (lock parts, the
keyway profile and the bitting), §1.2 (part frame and face placement), §1.8
(detail, wear, LOD) and §9.2 (budgets). Imported by every interact_lock_*.py.

PART FRAME (every G2 asset). Geometry is written in UNITY metres and converted
once with U(): origin = the part's pivot or mount point; FRONT = Unity +Z =
kit -Y = the outward normal of the face the part mounts on (for bolts: the
throw direction); +Y up. kitlib exports Blender (x, y, z) as Unity
(-x, z, -y) (kitlib.py export()), so Blender = U(X, Y, Z) = (-X, -Z, Y).

What lives here
* §3.2 data: the blade section (key-local), the keyway (offset 0.15 mm), the
  6-cut bitting and the pin-tip heights. KEY-LOCAL vs PLUG frame: the key
  enters with LookRotation(-out, up), i.e. turned 180 deg about Y from the
  plug's part frame, so plug (X, Y, Z) = key (-X, Y, -Z). The plug's keyway
  is therefore the key-local keyway MIRRORED in X (KEYWAY_PLUG).
* Mesh: a small vertex/face collector in Unity coordinates, with lathe,
  modulated (knurl) rings, prisms, lofts and pole fans; to_object() maps to
  Blender and creates a kit part.
* boolean(): EXACT boolean through a temporary cutter; slot=... transfers the
  cutter's material to the cut faces (dark cavities = Prop_PlasticBlack).
* Slotted screw builders (oval head and flat countersunk head).
* paint_wear(): the fr_wear colour attribute (spec §1.8 W2), on every part
  (BYTE_COLOR, face-corner domain). Encoding (1 = untouched):
  R = hand grime, G = edge wear, B = cavity.
* Meta helpers: motion, LOD distances and ratios, triangle and bounds checks.
"""

import math

import bmesh
import bpy
from mathutils import Vector

CHROME = "Prop_Chrome"
BRASS = "Prop_Brass"
DARK = "Prop_PlasticBlack"
ALU = "Prop_Aluminium"
TAGS = ("interactable", "lock_part")


# ------------------------------------------------------------------ frames
def U(X, Y, Z):
    """Unity part frame -> Blender (the inverse of kitlib's export mapping)."""
    return (-X, -Z, Y)


def to_unity(co):
    return (-co[0], co[2], -co[1])


# ------------------------------------------------------------ §3.2 profile
# Blade cross-section, KEY-LOCAL (X across the flats, Y up = cuts up), CCW.
# Spine corners carry the 0.2 mm chamfers.
BLADE = [
    (-0.0008, -0.0040), (0.0008, -0.0040), (0.0010, -0.0038),
    (0.0010, -0.0020), (0.0006, -0.0020), (0.0006, -0.0008), (0.0010, -0.0008),
    (0.0010, 0.0046), (-0.0010, 0.0046),
    (-0.0010, 0.0018), (-0.0006, 0.0018), (-0.0006, 0.0006), (-0.0010, 0.0006),
    (-0.0010, -0.0038),
]
KEYWAY_OFFSET = 0.00015
KEYWAY_TOP = 0.0048          # the slot top: open to the pin chambers
BLADE_TOP = 0.0046
BLADE_LEN = 0.025            # shoulder to tip = Unlock.InsertDepth
KEYWAY_DEPTH = 0.027         # open cavity behind the plug face

CUT_Z = (0.0040, 0.0078, 0.0116, 0.0154, 0.0192, 0.0230)
CUT_STEPS = (2, 4, 1, 5, 3, 2)
CUT_UNIT, CUT_BASE = 0.00038, 0.0002
CUT_DEPTH = tuple(s * CUT_UNIT + CUT_BASE for s in CUT_STEPS)
CUT_FLOOR = tuple(BLADE_TOP - d for d in CUT_DEPTH)
CUT_FLAT = 0.0008
# "50 deg flanks": read as each flank 50 deg off the vertical (100 deg
# included, the common US cut angle). ESTIMATE reading of the spec wording.
CUT_FLANK_DEG = 50.0
TIP_START, TIP_Y, NOSE_R = 0.0234, 0.0005, 0.0005   # fix pass 2026-10-08 (L6): = interact_key_common.TIP_BEVEL_Z

PIN_D = 0.0029
PIN_LIFT = 0.0002            # pin tip above its cut floor at full insertion
PIN_TIP_Y = tuple(f + PIN_LIFT for f in CUT_FLOOR)
CHAMBER_D = 0.0030           # pin chamber bore (0.05 mm radial clearance)


def offset_polygon(poly, d):
    """Mitred outward offset of a CCW polygon (right angles and 45s only)."""
    n = len(poly)
    out = []
    for i in range(n):
        p0, p1, p2 = Vector(poly[i - 1]), Vector(poly[i]), Vector(poly[(i + 1) % n])
        e1, e2 = (p1 - p0).normalized(), (p2 - p1).normalized()
        n1, n2 = Vector((e1.y, -e1.x)), Vector((e2.y, -e2.x))
        m = (n1 + n2)
        m = m / (1.0 + n1.dot(n2))
        q = p1 + m * d
        out.append((q.x, q.y))
    return out


def _keyway_key():
    k = offset_polygon(BLADE, KEYWAY_OFFSET)
    # The bitting edge is open to the pin chambers: the slot runs to 0.0048.
    return [(x, KEYWAY_TOP if y > 0.0045 else y) for x, y in k]


KEYWAY_KEY = _keyway_key()
KEYWAY_PLUG = [(-x, y) for x, y in reversed(KEYWAY_KEY)]


def assert_keyway():
    """The §3.2 numbers, checked on the computed offset polygon."""
    xs = [p[0] for p in KEYWAY_KEY]
    ys = [p[1] for p in KEYWAY_KEY]
    tol = 1e-7
    assert abs(max(xs) - 0.00115) < tol and abs(min(xs) + 0.00115) < tol, "keyway walls"
    assert abs(min(ys) + 0.00415) < tol and abs(max(ys) - 0.0048) < tol, "keyway Y range"
    rib_r = [p for p in KEYWAY_KEY if abs(p[0] - 0.00075) < tol]
    rib_l = [p for p in KEYWAY_KEY if abs(p[0] + 0.00075) < tol]
    assert sorted(round(p[1], 6) for p in rib_r) == [-0.00185, -0.00095], rib_r
    assert sorted(round(p[1], 6) for p in rib_l) == [0.00075, 0.00165], rib_l
    assert len(CUT_Z) == 6 and abs(CUT_Z[1] - CUT_Z[0] - 0.0038) < 1e-9
    want = [0.00096, 0.00172, 0.00058, 0.00210, 0.00134, 0.00096]
    assert all(abs(a - b) < 1e-9 for a, b in zip(CUT_DEPTH, want)), CUT_DEPTH


def bitting_y(z):
    """Top edge of the blade at key-local z (0 = shoulder), before the tip."""
    k = 1.0 / math.tan(math.radians(CUT_FLANK_DEG))
    y = BLADE_TOP
    for zc, fl in zip(CUT_Z, CUT_FLOOR):
        d = abs(z - zc) - CUT_FLAT / 2
        y = min(y, fl + max(0.0, d) * k)
    return y


def bitting_breaks():
    """z of every corner of the bitting edge between the shoulder and the tip."""
    k = 1.0 / math.tan(math.radians(CUT_FLANK_DEG))
    zs = {0.0, TIP_START}
    for zc, fl in zip(CUT_Z, CUT_FLOOR):
        w = CUT_FLAT / 2
        run = (BLADE_TOP - fl) / k
        zs.update((zc - w, zc + w, zc - w - run, zc + w + run))
    for i in range(len(CUT_Z) - 1):
        zs.add((CUT_FLOOR[i + 1] - CUT_FLOOR[i]) / (2 * k) + (CUT_Z[i] + CUT_Z[i + 1]) / 2)
    zs = sorted(z for z in zs if 0.0 <= z <= TIP_START)
    pts = [(z, bitting_y(z)) for z in zs]
    # Drop collinear points.
    out = [pts[0]]
    for i in range(1, len(pts) - 1):
        (z0, y0), (z1, y1), (z2, y2) = out[-1], pts[i], pts[i + 1]
        if abs((z1 - z0) * (y2 - y0) - (z2 - z0) * (y1 - y0)) > 1e-13:
            out.append(pts[i])
    out.append(pts[-1])
    return out


def blade_side_silhouette():
    """The blade's side outline in (z, y), key-local, CCW (z right, y up),
    reaching 1 mm behind the shoulder so booleans never meet coplanar faces."""
    pts = [(-0.001, -0.0045), (BLADE_LEN - NOSE_R, -0.0045), (BLADE_LEN - NOSE_R, -0.0040)]
    for i in range(1, 7):
        a = math.radians(-90 + 90 * i / 6)
        pts.append((BLADE_LEN - NOSE_R + NOSE_R * math.cos(a), -0.0040 + NOSE_R + NOSE_R * math.sin(a)))
    pts.append((BLADE_LEN, TIP_Y))
    for z, y in reversed(bitting_breaks()):
        pts.append((z, y))
    pts.append((-0.001, BLADE_TOP))
    return pts


# ------------------------------------------------------------------- Mesh
class Mesh:
    """Vertices and faces in UNITY part coordinates. Faces are wound so that
    their right-hand-rule normal points out of the material when the numbers
    are read in a right-handed frame; to_bmesh() maps with U() (a reflection)
    and reverses the winding, so Blender normals point out."""

    def __init__(self):
        self.v = []
        self.f = []

    def add(self, p):
        self.v.append((float(p[0]), float(p[1]), float(p[2])))
        return len(self.v) - 1

    def face(self, idx):
        idx = list(idx)
        if len(set(idx)) >= 3:
            self.f.append(idx)

    def transform(self, fn, start=0):
        for i in range(start, len(self.v)):
            self.v[i] = tuple(float(c) for c in fn(self.v[i]))

    # -- rings -----------------------------------------------------------
    def ring(self, r, z, segs, centre=(0.0, 0.0), a0=0.0, rfun=None):
        if r is not None and r < 1e-9 and rfun is None:
            return [self.add((centre[0], centre[1], z))]
        out = []
        for i in range(segs):
            a = a0 + 2 * math.pi * i / segs
            rr = rfun(i, a) if rfun is not None else r
            out.append(self.add((centre[0] + rr * math.cos(a), centre[1] + rr * math.sin(a), z)))
        return out

    def bridge(self, A, B):
        """Connect two rings (A below/inside, B next along the profile).
        Counts may be 1 (pole), equal, or an integer multiple of each other."""
        na, nb = len(A), len(B)
        if na == 1 and nb == 1:
            return
        if na == 1:
            for i in range(nb):
                self.face((A[0], B[(i + 1) % nb], B[i]))
            return
        if nb == 1:
            for i in range(na):
                self.face((A[i], A[(i + 1) % na], B[0]))
            return
        if na == nb:
            for i in range(na):
                j = (i + 1) % na
                self.face((A[i], A[j], B[j], B[i]))
            return
        if nb % na == 0:
            k = nb // na
            for i in range(na):
                poly = [A[i], A[(i + 1) % na]]
                poly += [B[(k * (i + 1) - t) % nb] for t in range(k + 1)]
                self.face(poly)
            return
        if na % nb == 0:
            k = na // nb
            for i in range(nb):
                poly = [A[(k * i + t) % na] for t in range(k + 1)]
                poly += [B[(i + 1) % nb], B[i]]
                self.face(poly)
            return
        raise ValueError("ring counts %d / %d" % (na, nb))

    def lathe(self, profile, segs, centre=(0.0, 0.0), a0=0.0, closed=False):
        """Revolve (r, z) about the part Z axis through ``centre``. The profile
        runs counter-clockwise round the material in the (r, z) half plane
        (r = 0 points become poles). Entries may be (r, z, rfun, segs) for a
        modulated ring (knurl). closed=True bridges the last ring back to the
        first (annular profiles: rings, bosses)."""
        rings = []
        for p in profile:
            if len(p) == 2:
                rings.append(self.ring(p[0], p[1], segs, centre, a0))
            else:
                r, z, rfun, n = p
                rings.append(self.ring(r, z, n, centre, a0, rfun))
        for A, B in zip(rings, rings[1:]):
            self.bridge(A, B)
        if closed:
            self.bridge(rings[-1], rings[0])
        return rings

    def prism(self, poly, z0, z1):
        """Extrude a CCW (X, Y) polygon from z0 to z1 (z1 > z0)."""
        lo = [self.add((x, y, z0)) for x, y in poly]
        hi = [self.add((x, y, z1)) for x, y in poly]
        self.face(list(reversed(lo)))
        self.face(hi)
        n = len(poly)
        for i in range(n):
            j = (i + 1) % n
            self.face((lo[i], lo[j], hi[j], hi[i]))
        return lo, hi

    def loft(self, sections, cap0=True, cap1=True):
        """Quads between equal-count sections (lists of 3D points)."""
        rings = [[self.add(p) for p in s] for s in sections]
        for A, B in zip(rings, rings[1:]):
            self.bridge(A, B)
        if cap0 and len(rings[0]) > 2:
            self.face(list(reversed(rings[0])))
        if cap1 and len(rings[-1]) > 2:
            self.face(rings[-1])
        return rings

    # -- output ------------------------------------------------------------
    def to_bmesh(self, recalc=True):
        bm = bmesh.new()
        vs = [bm.verts.new(U(*p)) for p in self.v]
        for f in self.f:
            try:
                bm.faces.new([vs[i] for i in f])
            except ValueError:
                pass
        bmesh.ops.reverse_faces(bm, faces=bm.faces)
        loose = [v for v in bm.verts if not v.link_faces]
        if loose:
            bmesh.ops.delete(bm, geom=loose, context="VERTS")
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=2e-8)
        if recalc:
            bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        return bm

    def to_object(self, kit, name, slot, recalc=True):
        return kit._new_object(name, self.to_bmesh(recalc), slot, "metres", "xz")


# ----------------------------------------------------------------- outlines
def rounded_rect(cx, cy, a, b, r, k=6):
    """CCW outline of a rectangle (half sizes a, b) with corner radius r."""
    pts = []
    r = min(r, a, b)
    for cxs, cys, a0 in ((1, -1, -90), (1, 1, 0), (-1, 1, 90), (-1, -1, 180)):
        ccx, ccy = cx + cxs * (a - r), cy + cys * (b - r)
        for i in range(k + 1):
            t = math.radians(a0 + 90.0 * i / k)
            pts.append((ccx + r * math.cos(t), ccy + r * math.sin(t)))
    return pts


def figure8(R, d, n_full=64):
    """CCW outline of two overlapping circles of radius R at (0, 0) and
    (0, d): the IC (interchangeable-core) face. Cusps at the waist."""
    xi = math.sqrt(R * R - (d / 2) ** 2)
    au0 = math.atan2(-d / 2, xi)
    span = 2 * math.pi - 2 * abs(au0)
    n = max(8, int(round(n_full * span / (2 * math.pi))))
    pts = []
    for i in range(n):                 # upper lobe, from the right waist over the top
        a = au0 + span * i / n
        pts.append((R * math.cos(a), d + R * math.sin(a)))
    bl0 = math.atan2(d / 2, -xi)
    for i in range(n):                 # lower lobe, from the left waist under the bottom
        a = bl0 + span * i / n
        pts.append((R * math.cos(a), R * math.sin(a)))
    return pts


def plate_sweep(m, cx, cy, a, b, rc, k, profile):
    """A rounded-rectangle solid along part Z: ``profile`` is a list of
    (inset d, z) from the back face to the front face; each ring is the
    outline (half sizes a, b, corner radius rc) inset by d. Back and front
    are capped (n-gons, triangulated by tidy())."""
    rings = []
    for d, z in profile:
        rr = max(rc - d, 0.00005)
        rings.append([m.add((x, y, z)) for x, y in rounded_rect(cx, cy, a - d, b - d, rr, k)])
    for A, B in zip(rings, rings[1:]):
        m.bridge(A, B)
    m.face(list(reversed(rings[0])))
    m.face(rings[-1])
    return rings


def round_profile(r, k, reverse=False):
    """(inset, rise) pairs of a quarter round of radius r in k steps."""
    out = []
    for i in range(k + 1):
        t = math.radians(90.0 * i / k)
        out.append((r * (1 - math.sin(t)), r * (1 - math.cos(t))))
    return list(reversed(out)) if reverse else out


def extrude_rounded_y(m, outline_xz, y0, y1, r, k=3):
    """Extrude a CCW (X, Z) outline along part Y from y0 to y1 with both
    end edges rounded (radius r, k steps): latches, plungers, strike lips."""
    secs = []
    for d, rise in round_profile(r, k):
        secs.append((d, y0 + rise))
    for d, rise in round_profile(r, k, reverse=True):
        secs.append((d, y1 - rise))
    sections = []
    for d, y in secs:
        o = offset_polygon(outline_xz, -d) if d > 1e-9 else outline_xz
        sections.append([(x, y, z) for x, z in o])
    return m.loft(sections)


def arc_pts(cx, cy, r, a0, a1, n, include_start=True):
    out = []
    for i in range(0 if include_start else 1, n + 1):
        t = math.radians(a0 + (a1 - a0) * i / n)
        out.append((cx + r * math.cos(t), cy + r * math.sin(t)))
    return out


def latch_outline(t, throw, back, land=0.0005, tip_r=0.0005, bevel_deg=30.0, sagitta=0.0006, arc_n=10):
    """Top view (X, Z) of a spring latchbolt, CCW: flat side at +X (the swing
    side S), bevel facing -X (the push side P, where the stops are), §3.1.
    The bevel chord runs from the land at the tip to the -X face at
    ``bevel_deg`` off the bolt axis (Z), bulged ``sagitta`` (a rounded
    bevel). Returns (outline, z where the bevel meets the -X face)."""
    hx = t / 2
    x0 = hx - tip_r - land                      # land end = bevel start
    zb = throw - (x0 + hx) / math.tan(math.radians(bevel_deg))
    pts = [(-hx, back), (hx, back), (hx, throw - tip_r)]
    pts += arc_pts(hx - tip_r, throw - tip_r, tip_r, 0, 90, 3, include_start=False)
    p0, p1 = Vector((x0, throw)), Vector((-hx, zb))
    mid = (p0 + p1) / 2
    nrm = Vector((-(p1 - p0).y, (p1 - p0).x)).normalized()
    if nrm.dot(Vector((-1.0, 1.0))) < 0:
        nrm = -nrm                              # bulge outward (toward -X / +Z)
    half = (p1 - p0).length / 2
    R = (half * half + sagitta * sagitta) / (2 * sagitta)
    ctr = mid - nrm * (R - sagitta)
    a0 = math.atan2(p0.y - ctr.y, p0.x - ctr.x)
    a1 = math.atan2(p1.y - ctr.y, p1.x - ctr.x)
    delta = (a1 - a0 + math.pi) % (2 * math.pi) - math.pi     # the short arc
    for i in range(arc_n + 1):
        a = a0 + delta * i / arc_n
        pts.append((ctr.x + R * math.cos(a), ctr.y + R * math.sin(a)))
    return pts, zb


def plunger_outline(x0, x1, back, protrude, n=12):
    """Top view (X, Z) of the auxiliary deadlatch plunger: a bar with a
    half-round nose standing ``protrude`` out of the front at Z 0."""
    r = (x1 - x0) / 2
    pts = [(x0, back), (x1, back), (x1, protrude - r)]
    pts += arc_pts((x0 + x1) / 2, protrude - r, r, 0, 180, n, include_start=False)
    return pts


# ----------------------------------------------------------------- booleans
def boolean(kit, obj, cutter, op="DIFFERENCE", slot=None):
    """EXACT boolean of a kit part with a Mesh (Unity coords). slot transfers
    that material onto the faces the cutter makes."""
    bm = cutter.to_bmesh(True)
    me = bpy.data.meshes.new("g2_cutter")
    bm.to_mesh(me)
    bm.free()
    cut = bpy.data.objects.new("g2_cutter", me)
    bpy.context.scene.collection.objects.link(cut)
    if slot is not None:
        me.materials.append(kit._material(slot))
    mod = obj.modifiers.new("g2_bool", "BOOLEAN")
    mod.operation = op
    mod.solver = "EXACT"
    mod.object = cut
    mod.material_mode = "TRANSFER" if slot is not None else "INDEX"
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.data.objects.remove(cut, do_unlink=True)
    bpy.data.meshes.remove(me)
    return obj


def tidy(obj, delete_below=None, axis_z=None):
    """Triangulate n-gons (> 4 sides; concave-safe), optionally delete faces
    whose every vertex lies below Unity part Z ``delete_below`` (hidden
    undersides; dark slot faces are always kept), and drop loose geometry."""
    me = obj.data
    bm = bmesh.new()
    bm.from_mesh(me)
    if delete_below is not None:
        dark = [i for i, m in enumerate(me.materials) if m is not None and m.name == DARK]
        doomed = [f for f in bm.faces if f.material_index not in dark
                  and all(to_unity(v.co)[2] < delete_below for v in f.verts)]
        if doomed:
            bmesh.ops.delete(bm, geom=doomed, context="FACES")
    # EXACT booleans and the lever's union leave zero-area triangles and
    # sub-micron slivers along coincident rims: collapse them (1 um; the
    # smallest real feature in G2 is the 0.05 mm pin-chamber clearance).
    bmesh.ops.dissolve_degenerate(bm, dist=DEGENERATE_DIST, edges=bm.edges[:])
    big = [f for f in bm.faces if len(f.verts) > 4]
    if big:
        bmesh.ops.triangulate(bm, faces=big, quad_method="BEAUTY", ngon_method="BEAUTY")
    loose = [v for v in bm.verts if not v.link_faces]
    if loose:
        bmesh.ops.delete(bm, geom=loose, context="VERTS")
    bm.to_mesh(me)
    bm.free()
    me.update()
    return obj


DEGENERATE_DIST = 1e-6


def tris(obj):
    return sum(len(p.vertices) - 2 for p in obj.data.polygons)


# ------------------------------------------------------------------- wear
def paint_wear(obj, fn=None):
    """fr_wear colour attribute (BYTE_COLOR, FACE-CORNER domain). fn(P, N,
    slot) gets the Unity part-space position, the face normal and the face's
    slot name and returns (R hand grime, G edge wear, B cavity), 1 =
    untouched. Faces in Prop_PlasticBlack (cavities) get B <= 0.35.
    Corner, not point (G1/G3/G4 use point): boolean rims share vertices
    between a dark cavity wall and a long face triangle, and a per-point
    value would smear the cavity mask across the whole face. Both domains
    export as FBX vertex colours."""
    me = obj.data
    attr = me.color_attributes.get("fr_wear")
    if attr is None:
        attr = me.color_attributes.new("fr_wear", "BYTE_COLOR", "CORNER")
    names = [m.name if m is not None else "" for m in me.materials]
    for poly in me.polygons:
        n = to_unity(poly.normal)
        slot = names[poly.material_index] if poly.material_index < len(names) else ""
        for li in poly.loop_indices:
            p = to_unity(me.vertices[me.loops[li].vertex_index].co)
            c = fn(p, n, slot) if fn is not None else (1.0, 1.0, 1.0)
            if slot == DARK:
                c = (c[0], c[1], min(c[2], 0.35))
            attr.data[li].color_srgb = (max(0.0, min(1.0, c[0])), max(0.0, min(1.0, c[1])),
                                        max(0.0, min(1.0, c[2])), 1.0)
    me.color_attributes.active_color = attr
    return obj


def lod2_drop(obj):
    """Spec §1.8 / §10.0: obj["fr_lod2_drop"] = True. The custom property is
    lost in finish()'s join, so the part is ALSO put in an "fr_lod2_drop"
    vertex group (all its vertices), which survives the join the same way
    kitlib's own fr_lod1_drop bookkeeping does. FBX does not export
    unskinned vertex groups, so this changes nothing in Unity today."""
    obj["fr_lod2_drop"] = True
    vg = obj.vertex_groups.get("fr_lod2_drop") or obj.vertex_groups.new(name="fr_lod2_drop")
    vg.add([v.index for v in obj.data.vertices], 1.0, "REPLACE")
    return obj


def finish_part(obj, wear=None, lod2=False, delete_below=None):
    tidy(obj, delete_below)
    paint_wear(obj, wear)
    if lod2:
        lod2_drop(obj)
    return obj


# ---------------------------------------------------------- slotted screws
def _slot_cutter(cx, cy, ztop, depth, width, length, ang):
    m = Mesh()
    ca, sa = math.cos(math.radians(ang)), math.sin(math.radians(ang))
    hw, hl = width / 2, length / 2
    poly = [(-hl, -hw), (hl, -hw), (hl, hw), (-hl, hw)]
    poly = [(cx + x * ca - y * sa, cy + x * sa + y * ca) for x, y in poly]
    m.prism(poly, ztop - depth, ztop + 0.002)
    return m


def oval_screw(kit, cx, cy, zs, D=0.0070, dome=0.0012, slot_w=0.0008, slot_d=0.0008,
               ang=0.0, segs=48, slot=CHROME, name="oval screw", dome_rings=(0.42, 0.78), rim=0.00012):
    """Oval-head (raised countersunk) slotted wood/machine screw seen from the
    front: the rim sits at the surface z = zs (Unity part Z), the dome rises
    ``dome``; the countersunk underside stays hidden and is deleted."""
    r = D / 2
    R = (r * r + dome * dome) / (2 * dome)            # spherical cap radius
    zc = zs + rim + dome - R
    prof = [(0.0, zs - 0.0006), (r - 0.0004, zs - 0.0006), (r, zs)]
    if rim > 0:
        prof.append((r, zs + rim))
    for t in dome_rings:
        a = math.asin(r / R) * (1 - t)
        prof.append((R * math.sin(a), zc + R * math.cos(a)))
    prof.append((0.0, zs + rim + dome))
    m = Mesh()
    m.lathe(prof, segs, (cx, cy))
    obj = m.to_object(kit, name, slot)
    boolean(kit, obj, _slot_cutter(cx, cy, zs + rim + dome, slot_d, slot_w, D + 0.002, ang), slot=DARK)
    return obj


def flat_screw(kit, cx, cy, zs, D=0.0095, proud=0.0001, slot_w=0.0009, slot_d=0.0007,
               ang=0.0, segs=48, slot=CHROME, name="flat screw"):
    """Flat countersunk slotted screw: the face sits ``proud`` above the
    surface z = zs (negative = just sunk) with a 0.15 mm edge chamfer; the
    82-degree-ish cone below sits in a countersink(); its hidden bottom is
    deleted by finish_part(delete_below=zs - 0.0003)."""
    r = D / 2
    zf = zs + proud
    prof = [(0.0, zs - 0.0008), (r - 0.0008, zs - 0.0008), (r, zf - 0.00025),
            (r - 0.00015, zf), (0.0, zf)]
    m = Mesh()
    m.lathe(prof, segs, (cx, cy))
    obj = m.to_object(kit, name, slot)
    boolean(kit, obj, _slot_cutter(cx, cy, zf, slot_d, slot_w, D + 0.002, ang), slot=DARK)
    return obj


def countersink(cx, cy, zs, r_top, cone=0.0012, r_bottom=0.0, segs=48):
    """Cutter: a blind countersink seat for an oval or flat head: a 0.3 mm
    vertical lip at r_top (a crisp rim: no smoothing fan across the face it
    is cut into), then a cone ``cone`` deep (hidden by the head)."""
    m = Mesh()
    prof = [(0.0, zs - cone)] if r_bottom <= 0 else [(0.0, zs - cone), (r_bottom, zs - cone)]
    prof += [(r_top, zs - 0.0003), (r_top, zs + 0.001), (0.0, zs + 0.001)]
    m.lathe(prof, segs, (cx, cy))
    return m


# ------------------------------------------------------------------ strike
STRIKE_W = 0.032
STRIKE_T = 0.0016
STRIKE_PROUD = 0.0002          # face 0.2 mm proud of the lining: never coplanar with it
LIP_X0, LIP_X1 = -0.016, -0.040
LIP_CURL = 0.0008              # the lip end rises to Z +0.001 (<= 1 mm, §3.1)


def build_strike(kit, half_len, openings, lip_y, screw_y, screw_segs=48, corner_k=4, box_k=3,
                 lip_xs=(-0.0157, -0.021, -0.026, -0.030, -0.033, -0.035, -0.0368, -0.0382, -0.0393, -0.040)):
    """A strike plate in the part frame of 10_spec §3.1: plate centre on the
    latch lining, front (+Z) facing the leaf edge, the curved lip toward part
    -X (= door +X, the swing side the latch arrives from). ``openings`` =
    [(y, half_x, half_y, box_depth)], dark dust boxes behind each.
    Returns (plate, lip, boxes, screws)."""
    zf, zb = STRIKE_PROUD, STRIKE_PROUD - STRIKE_T
    m = Mesh()
    plate_sweep(m, 0.0, 0.0, STRIKE_W / 2, half_len, 0.002, corner_k,
                [(0.0, zb), (0.0, zf - 0.0003), (0.0003, zf)])
    plate = m.to_object(kit, "strike plate", CHROME)
    for y, hx, hy, depth in openings:
        c = Mesh()
        c.prism(rounded_rect(0.0, y, hx, hy, 0.001, box_k), zb - 0.002, zf + 0.002)
        boolean(kit, plate, c)
    for sy in screw_y:      # countersink seats: the slotted heads sit in them, flush
        boolean(kit, plate, countersink(0.0, sy, zf, 0.0085 / 2 + 0.00015, cone=0.0010, segs=screw_segs), slot=DARK)
    # Lip: a tongue off the -X edge round the latch opening, curling up
    # toward the leaf by LIP_CURL over its outer 14 mm (one piece with the
    # plate in reality; overlaps the plate edge by 0.3 mm here).
    y0, y1 = lip_y
    rc = 0.006
    xs = sorted(set(list(lip_xs) + [LIP_X1 + rc]), reverse=True)
    sections = []
    for x in xs:
        dy = 0.0
        if x < LIP_X1 + rc:
            dy = rc - math.sqrt(max(0.0, rc * rc - (x - (LIP_X1 + rc)) ** 2))
        u = max(0.0, (-0.026 - x) / (-0.026 - LIP_X1))
        zt = zf + LIP_CURL * u * u
        a, b = y0 + dy, y1 - dy
        e = 0.0002
        sections.append([(x, a, zt - STRIKE_T), (x, b, zt - STRIKE_T), (x, b, zt - e), (x, b - e, zt),
                         (x, a + e, zt), (x, a, zt - e)])
    lm = Mesh()
    lm.loft(sections)
    lip = lm.to_object(kit, "strike lip", CHROME)
    bm_ = Mesh()
    for y, hx, hy, depth in openings:
        top = [bm_.add((x, yy, zb)) for x, yy in rounded_rect(0.0, y, hx, hy, 0.001, box_k)]
        bot = [bm_.add((x, yy, zb - depth)) for x, yy in rounded_rect(0.0, y, hx, hy, 0.001, box_k)]
        n = len(top)
        for i in range(n):
            j = (i + 1) % n
            bm_.face((top[i], top[j], bot[j], bot[i]))     # walls face inward
        bm_.face(bot)                                       # floor faces the opening (+Z)
    boxes = bm_.to_object(kit, "dust boxes", DARK, recalc=False)
    screws = []
    for sy, ang in zip(screw_y, (8.0, -23.0, 41.0, -5.0)):
        s = flat_screw(kit, 0.0, sy, zf, D=0.0085, proud=-0.00005, slot_w=0.0009, slot_d=0.0006, ang=ang,
                       segs=screw_segs, name="strike screw")
        screws.append(s)
    return plate, lip, boxes, screws


def strike_wear(openings):
    def fn(p, n, slot):
        x, y, z = p
        g = 1.0
        if x < LIP_X0 + 0.0005:
            g = 0.62                                 # the lip: the latch rides it every close
        for oy, hx, hy, d in openings:
            if abs(y - oy) < hy + 0.0015 and abs(x) < hx + 0.0015:
                g = min(g, 0.72)                     # bolts rub the opening edges
        return (1.0, g, 1.0)
    return fn


# ------------------------------------------------------------------- meta
def common_meta(kit, motion, lod_distances, lod1_ratio, lod2_ratio, budget):
    kit.no_collider()
    kit.tag(*TAGS)
    kit.meta["motion"] = motion
    kit.meta["lodDistances"] = list(lod_distances)
    kit.meta["lodRatios"] = [lod1_ratio, lod2_ratio]
    kit.meta["lodBudget"] = list(budget)
    kit.meta["partFrame"] = "origin = pivot/mount; front = Unity +Z (kit -Y); +Y up (10_spec §1.2)"


def unity_bounds(kit):
    lo = [1e9] * 3
    hi = [-1e9] * 3
    for obj in kit.parts:
        mw = obj.matrix_world
        for v in obj.data.vertices:
            p = to_unity(mw @ v.co)
            for i in range(3):
                lo[i] = min(lo[i], p[i])
                hi[i] = max(hi[i], p[i])
    return lo, hi


def check(kit, budget, lo_want=None, hi_want=None, tol=0.0002):
    """Assert the LOD0 triangle count within +-15 % of the §9.2 budget and the
    Unity-space bounds against the spec envelope. Prints the numbers."""
    n = sum(tris(o) for o in kit.parts)
    lo, hi = unity_bounds(kit)
    print("[g2] %s: %d tris (budget %d, %+.1f %%), bounds %s .. %s" % (
        kit.name, n, budget, 100.0 * (n - budget) / budget,
        ["%.4f" % x for x in lo], ["%.4f" % x for x in hi]))
    for o in kit.parts:
        print("[g2]    %-28s %5d tris  slots %s" % (o.name, tris(o), [m.name for m in o.data.materials]))
    assert abs(n - budget) <= 0.15 * budget, "%s: %d tris vs budget %d" % (kit.name, n, budget)
    for want, got, label in ((lo_want, lo, "min"), (hi_want, hi, "max")):
        if want is None:
            continue
        for i in range(3):
            if want[i] is not None:
                assert abs(got[i] - want[i]) <= tol, "%s bounds %s[%d]: %.5f vs %.5f" % (kit.name, label, i, got[i], want[i])
    return n, lo, hi
