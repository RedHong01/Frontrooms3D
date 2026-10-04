"""Q16 evacuation-plan placard: shared numbers and builders (helper module:
no NAME, no build). Imported by evac_placard.py (Kit_EvacPlacard: the snap
frame and the printed sheet) and evac_placard_lens.py (Kit_EvacPlacardLens:
the clear lens, its own renderer). Spec:
Documentation/research/placard/10_spec.md §2 (numbers), §2.5 (slots, UVs),
§2.6 (sidecar), §2.7 (LODs), §2.8 (asserts), §2.9 (wear).

The object
* A 17 x 11 in (432 x 279 mm) evacuation plan in a 1 in clear-anodised
  aluminium snap frame (US, from 1977: MDI US 4,145,828), tamper-proof
  bullnose clip rails (MDI US 4,519,152, 1983), four rails mitred 45 deg, a
  thin non-glare plastic lens on the sheet, screwed flat to the wall through
  the base under the closed rails (no visible screws, no standoffs).

PART FRAME (both assets). Unity part-local metres converted with
interact_key_common.U(): origin = the wall-face point at the frame centre,
+Z out of the wall (the kit front, Blender -Y), +Y up; seen from the front
the viewer's right is -X. Nothing lies behind z = 0.

SECTION FRAME (one rail, spec §2.3). Millimetres (u, w): u = inset from the
frame's outside edge toward the centre, w = height above the wall. A
profile is listed so that the rail's material lies on its RIGHT (clockwise
round the section with u to the right and w up): the LEFT normal of every
segment is the outward surface normal. Every face this module makes is
oriented against that analytic outward direction (no recalc guesswork), so
open sweeps and closed solids come out the same way.

Deviation from §2.3 (reported in 20_build.md): with the crown at (5.00,
13.50) level and the nose arc R 0.80 at (24.60, 6.35), "tangent at both
ends" fixes the face arc at R 34.22 (centre (5.00, -20.72)); the §2.3
"R about 29.6" does not touch the nose. It meets the nose 35.9 deg below
the horizontal (§2.3: "about 40").

LODs (interactables §1.8 / outlets §1.1 convention). LOD1 and LOD2 are
hand-built part sets marked obj["fr_lods"] ("0", "1", "2", "012"), built
only when kitlib.Kit.make_lods exists (P-1/P-1b). Until then each FBX holds
LOD0 only. The scratchpad review tool forces them on to count and render
them without exporting (FORCE_LODS below).
"""

import math

import bmesh
import bpy
from mathutils import Vector

import kitlib
import interact_key_common as kc
from interact_key_common import U

MM = 0.001
SMOOTH_ANGLE = 35.0
FORCE_LODS = False            # the review tool sets this; never True in a build

# ------------------------------------------------------------ slots (§2.5)
ALU = "Prop_Aluminium"        # existing: clear-anodised satin aluminium
PRINT = "Prop_EvacPlan"       # NEW: the printed sheet (FrontRooms/Surface; glow mask = emission map)
LENS = "Prop_LensNonGlare"    # NEW: 1.0 mm non-glare lens (URP Lit transparent, black base, alpha 0.04)


def register_slots():
    """§2.5 preview colours (Blender only; the Unity look comes from
    Resources/Surfaces). register_slot is setdefault: kitlib.SLOTS is never edited."""
    kitlib.register_slot(PRINT, (0.957, 0.945, 0.910), 0.85, 0.0)
    kitlib.register_slot(LENS, (0.90, 0.92, 0.92), 0.45, 0.0)


def lods_on():
    return FORCE_LODS or hasattr(kitlib.Kit, "make_lods")


# --------------------------------------------------------- §2.2 numbers (mm)
SHEET = (432.0, 279.0)                 # the artwork, 6 px/mm: 2592 x 1674
SHEET_TO_OUTSIDE = 17.4                # frame outside edge -> sheet edge
FRAME = (SHEET[0] + 2 * SHEET_TO_OUTSIDE, SHEET[1] + 2 * SHEET_TO_OUTSIDE)   # 466.8 x 313.8
A_HALF, B_HALF = FRAME[0] / 2, FRAME[1] / 2                                  # 233.4, 156.9
FACE_W = 25.4                          # rail face width = sight edge inset
SIGHT = (FRAME[0] - 2 * FACE_W, FRAME[1] - 2 * FACE_W)                       # 416.0 x 263.0
DEPTH = 13.5                           # wall to crown
SHEET_W = 4.55                         # printed sheet's face above the wall
LENS_T = 1.0
LENS_TOP = SHEET_W + LENS_T            # 5.55: the lip nose rests on it
LENS_EDGE_LOW = 4.60                   # lens edge quads stop 0.05 above the sheet (never coplanar)
# Seams
HINGE_LO, HINGE_HI, HINGE_DEPTH = 2.80, 3.20, 0.35
MITRE_CHAMFER = 0.25                   # each rail end, 0.25 x 45 deg
MITRE_GAP = 0.05                       # rails 0.05 apart across the mitre line
# Section (§2.3)
BULL_R, BULL_C = 5.0, (5.0, 8.5)
CROWN = (5.0, DEPTH)
NOSE_R, NOSE_C = 0.8, (24.6, 6.35)
UNDER_END = (18.0, 5.75)
BULL_SEGS, FACE_SEGS, NOSE_SEGS = 16, 20, 8
BASE_INNER = 1.6                       # the base strip's hidden top runs in to here under the rail
# Wall screws (§1.5, §2.6): hidden under the closed rails, anchors only.
SCREW_X, SCREW_Y, SCREW_W = 150.0, 145.9, 1.2
# Artwork (§2.5, §3.3): the packed texture adds one edge row top and bottom.
TEX_ROWS = 1676
UV_V0, UV_V1 = 1.0 / TEX_ROWS, (TEX_ROWS - 1.0) / TEX_ROWS
# Glow-mask bounding box, measured on placard_glow_mask.png (R > 8): sheet
# x 300.0-407.0 mm, y 56.5-229.0 mm from the top-left.
LEGEND_BOX = (300.0, 407.0, 56.5, 229.0)
# LODs (§2.7): switch distances at lodBias 1, FOV 76; triangle budgets.
LOD_DISTANCES = (2.0, 6.0, 30.0)
BUDGET = (1600, 120, 28)
LENS_CULL = 6.0
LENS_BUDGET = 10


def face_arc():
    """The convex face arc tangent to the level crown and to the nose arc:
    (radius, centre, tangent angle on the nose in degrees)."""
    # centre (cx, 13.5 - R); |centre - nose centre| = R - r  (internal tangency)
    dx = NOSE_C[0] - CROWN[0]
    k = NOSE_C[1] - CROWN[1]          # nose centre height relative to the crown (negative)
    # dx^2 + (k + R)^2 = (R - r)^2  ->  R = (r^2 - dx^2 - k^2) / (2 (k + r))
    r = NOSE_R
    R = (r * r - dx * dx - k * k) / (2 * (k + r))
    c = (CROWN[0], CROWN[1] - R)
    ang = math.degrees(math.atan2(NOSE_C[1] - c[1], NOSE_C[0] - c[0]))
    return R, c, ang


def _arc(c, r, a0, a1, n):
    return [(c[0] + r * math.cos(math.radians(a0 + (a1 - a0) * i / n)),
             c[1] + r * math.sin(math.radians(a0 + (a1 - a0) * i / n))) for i in range(n + 1)]


def _arc_n(c, r, a0, a1, n):
    """Arc points and their TRUE outward normals (radial, convex arcs)."""
    out = []
    for i in range(n + 1):
        a = math.radians(a0 + (a1 - a0) * i / n)
        out.append(((c[0] + r * math.cos(a), c[1] + r * math.sin(a)), (math.cos(a), math.sin(a))))
    return out


def profile_n(level):
    """The rail section at an LOD as (points, normals). normals[i] is the TRUE
    outward surface normal of the §2.3 curve at that point (unit (nu, nw)), or
    None at a real corner (the hinge V, the hidden underside), where the faces
    keep their own flat normals. Every LOD shades from the same analytic curve,
    so the bullnose/face highlight is the same at LOD0, LOD1 and LOD2 (round 2:
    the round-1 LOD1 shaded its 5-segment face differently and popped at 2 m).

    LOD0 (48 points): hinge-V upper half, outer side, bullnose (16), face (20),
    lip nose (3 + 5 = 8; 0 and -90 deg exact), hidden underside; the base strip
    carries the V's lower half.
    LOD1 (14 points): outer side 2 (from the wall: no hinge seam, 0.4 mm is
    0.2 px at 2 m), bullnose 4, face 5, nose 2, underside 1.
    LOD2 (4 points): (0, 0), (0, 9.0), (6.0, 13.5), (25.4, 5.55), each with
    the normal of the true curve it stands for (the last one the face's at the lip)."""
    R, c, ang = face_arc()
    if level == 0:
        pn = [((HINGE_DEPTH, (HINGE_LO + HINGE_HI) / 2), None), ((0.0, HINGE_HI), None), ((0.0, BULL_C[1]), (-1.0, 0.0))]
        pn += _arc_n(BULL_C, BULL_R, 180.0, 90.0, BULL_SEGS)[1:]
        pn += _arc_n(c, R, 90.0, ang, FACE_SEGS)[1:]
        pn += _arc_n(NOSE_C, NOSE_R, ang, 0.0, 3)[1:]
        pn += _arc_n(NOSE_C, NOSE_R, 0.0, -90.0, NOSE_SEGS - 3)[1:]
        pn.append((UNDER_END, None))
    elif level == 1:
        pn = [((0.0, 0.0), None), ((0.0, BULL_C[1]), (-1.0, 0.0))]
        pn += _arc_n(BULL_C, BULL_R, 180.0, 90.0, 4)[1:]
        pn += _arc_n(c, R, 90.0, ang, 5)[1:]
        pn += [((NOSE_C[0] + NOSE_R, NOSE_C[1]), (1.0, 0.0)), ((NOSE_C[0], NOSE_C[1] - NOSE_R), (0.0, -1.0)), (UNDER_END, None)]
    else:
        a9 = math.asin((9.0 - BULL_C[1]) / BULL_R)                        # the bullnose point at w = 9.0
        crown = (6.0 - c[0]) / R                                           # the face arc at u = 6.0
        pn = [((0.0, 0.0), None), ((0.0, 9.0), (-math.cos(a9), math.sin(a9))),
              ((6.0, DEPTH), (crown, math.sqrt(1.0 - crown * crown))),
              ((FACE_W, LENS_TOP), (math.cos(math.radians(ang)), math.sin(math.radians(ang))))]
    return [p for p, _ in pn], [n for _, n in pn]


def profile_lod0():
    return profile_n(0)[0]


def profile_lod1():
    return profile_n(1)[0]


def profile_lod2():
    return profile_n(2)[0]


BASE_PROFILE = [(0.0, 0.0), (0.0, HINGE_LO), (HINGE_DEPTH, (HINGE_LO + HINGE_HI) / 2),
                (BASE_INNER, (HINGE_LO + HINGE_HI) / 2), (BASE_INNER, 0.0)]   # closed loop


# ----------------------------------------------------------- 2D helpers
def left_normal(a, b):
    d = Vector((b[0] - a[0], b[1] - a[1]))
    d.normalize()
    return Vector((-d.y, d.x))


def vertex_normals(poly, closed):
    """Outward (left) unit normals at each vertex: the mean of the adjacent
    segment normals (no mitre scaling)."""
    n = len(poly)
    out = []
    for i in range(n):
        segs = []
        if i > 0 or closed:
            segs.append(left_normal(poly[i - 1], poly[i]))
        if i < n - 1 or closed:
            segs.append(left_normal(poly[i], poly[(i + 1) % n]))
        m = sum(segs, Vector((0.0, 0.0)))
        m.normalize()
        out.append(m)
    return out


# -------------------------------------------------------- 3D mapping
# Sides of the rectangle in Unity plan (X, Y): outward normal n, along axis t,
# perpendicular half extent E, along half length L.
SIDES = {
    "top": ((0.0, 1.0), (1.0, 0.0), B_HALF, A_HALF),
    "bottom": ((0.0, -1.0), (1.0, 0.0), B_HALF, A_HALF),
    "right": ((1.0, 0.0), (0.0, 1.0), A_HALF, B_HALF),      # Unity +X = the viewer's LEFT
    "left": ((-1.0, 0.0), (0.0, 1.0), A_HALF, B_HALF),
}


def side_point(side, u, w, s):
    """Unity part-space point (mm) of section point (u, w) at along-rail s."""
    n, t, E, _ = SIDES[side]
    return (n[0] * (E - u) + t[0] * s, n[1] * (E - u) + t[1] * s, w)


def side_normal(side, nuw):
    """Unity direction of a section normal (nu, nw) on a side."""
    n, _, _, _ = SIDES[side]
    return (-nuw[0] * n[0], -nuw[0] * n[1], nuw[1])


def bl(p_mm):
    """Unity mm -> Blender metres."""
    return Vector(U(p_mm[0] * MM, p_mm[1] * MM, p_mm[2] * MM))


def bl_dir(d):
    v = Vector(U(d[0], d[1], d[2]))
    v.normalize()
    return v


# Per-object loop-normal tables from Builder.object(), consumed by finalize():
# {object name: [None | {vertex index: Blender-space unit normal}]}, indexed by
# the face attribute "fr_nid".
NORMALS = {}
NID = "fr_nid"


class Builder:
    """Faces with an explicit outward direction each (Unity), flipped to match.
    A face may carry per-vertex TRUE surface normals (Unity directions); its
    other vertices, and faces without them, keep the flat face normal."""

    def __init__(self):
        self.bm = bmesh.new()
        self.nid = self.bm.faces.layers.int.new(NID)
        self.table = [None]                     # 0 = flat
        self.flips = 0
        self.worst = 1.0

    def vert(self, p_mm):
        return self.bm.verts.new(bl(p_mm))

    def face(self, verts, outward_unity, normals=None):
        f = self.bm.faces.new(verts)
        f.normal_update()
        want = bl_dir(outward_unity)
        d = f.normal.dot(want)
        if d < 0:
            f.normal_flip()
            self.flips += 1
            d = -d
        self.worst = min(self.worst, d)
        if normals is not None and any(n is not None for n in normals):
            self.table.append({v: bl_dir(n) for v, n in zip(verts, normals) if n is not None})
            f[self.nid] = len(self.table) - 1
        return f

    def object(self, kit, name, slot, lods, uv="metres"):
        bm = self.bm
        tri = [f for f in bm.faces if len(f.verts) > 4]
        if tri:
            bmesh.ops.triangulate(bm, faces=tri, quad_method="BEAUTY", ngon_method="BEAUTY")
        bm.verts.index_update()
        table = [None if e is None else {v.index: n for v, n in e.items()} for e in self.table]
        obj = kit._new_object(name, bm, slot, uv, "xz")      # no recalc: faces are oriented already
        obj["fr_lods"] = lods
        NORMALS[obj.name] = table
        return obj


# ------------------------------------------------------------- LOD0 rail
def mitre_s(side, u):
    """Along-rail position of the +s mitre plane at inset u (the rail stops
    MITRE_GAP / 2 short of the 45 deg corner line)."""
    _, _, _, L = SIDES[side]
    return L - u - (MITRE_GAP / 2) * math.sqrt(2.0)


def rail_lod0(kit, side, extra_s=(), wear=None):
    """One LOD0 clip rail: the §2.3 section extruded along its side, both ends
    cut at 45 deg (mitre) and chamfered 0.25 x 45 deg on every visible edge
    (the section's hinge apex and hidden underside end stay square), closed
    by the hidden inner face and two end caps: a watertight solid.
    extra_s: extra section rings (mm along the rail) for wear paint. The
    profile bands carry the curve's true normals (profile_n); the chamfers and
    end caps stay flat. The ring where a chamfer meets the profile band is
    doubled (coincident vertices, no crack): near the lip the chamfer meets the
    band at under SMOOTH_ANGLE, and kitlib.finish() re-marks sharp edges by
    angle, so a shared ring merged the band's true normals into the chamfer's
    fan (round 2a: a light band along every rail just above the lip)."""
    prof, true_n = profile_n(0)
    n = len(prof)
    closed_normals = vertex_normals(prof, closed=True)          # incl. the hidden closing edge
    inset = []
    for i, (p, nv) in enumerate(zip(prof, closed_normals)):
        c = 0.0 if i in (0, n - 1) else MITRE_CHAMFER
        inset.append((p[0] - nv.x * c, p[1] - nv.y * c))
    seg_n = [left_normal(prof[i], prof[(i + 1) % n]) for i in range(n)]   # n segments incl. closing
    b = Builder()
    # Rings from the -s end to the +s end.
    rings = []
    sets = []
    sets.append(("end", -1))
    sets.append(("cham", -1))
    for s in sorted(extra_s):
        sets.append(("mid", s))
    sets.append(("cham", +1))
    sets.append(("end", +1))
    for kind, arg in sets:
        pts = []
        for i in range(n):
            if kind == "end":
                q = inset[i]
                pts.append(side_point(side, q[0], q[1], arg * mitre_s(side, q[0])))
            elif kind == "cham":
                p = prof[i]
                pts.append(side_point(side, p[0], p[1], arg * (mitre_s(side, p[0]) - MITRE_CHAMFER * math.sqrt(2.0))))
            else:
                p = prof[i]
                pts.append(side_point(side, p[0], p[1], arg))
        ring = [b.vert(q) for q in pts]
        chamfer_side = [b.vert(q) for q in pts] if kind == "cham" else ring
        rings.append((kind, arg, ring, chamfer_side))
    _, t, _, _ = SIDES[side]
    for k in range(len(rings) - 1):
        kind_a, arg_a, ra, ra_c = rings[k]
        kind_b, arg_b, rb, rb_c = rings[k + 1]
        end_dir = None
        if kind_a == "end":
            end_dir = (-t[0], -t[1], 0.0)
            rb = rb_c
        elif kind_b == "end":
            end_dir = (t[0], t[1], 0.0)
            ra = ra_c
        for i in range(n):
            j = (i + 1) % n
            nn = side_normal(side, seg_n[i])
            normals = None
            if end_dir is not None:
                nn = (nn[0] + end_dir[0], nn[1] + end_dir[1], nn[2] + end_dir[2])
            elif j != 0:                                   # a profile band (not the hidden closing face)
                ni = None if true_n[i] is None else side_normal(side, true_n[i])
                nj = None if true_n[j] is None else side_normal(side, true_n[j])
                normals = (ni, nj, nj, ni)
            b.face((ra[i], ra[j], rb[j], rb[i]), nn, normals)
    # End caps in the mitre planes.
    nside, _, _, _ = SIDES[side]
    for kind, arg, ring, _ in (rings[0], rings[-1]):
        out = (arg * t[0] - nside[0], arg * t[1] - nside[1], 0.0)
        b.face(list(ring), out)
    obj = b.object(kit, "rail %s" % side, ALU, "0")
    obj["fr_flips"] = b.flips
    return obj, b


def base_strip(kit, profile, closed, name, lods, true_n=None):
    """A section swept once round the frame rectangle with 45 deg corners
    (inset u at corner (sx, sy) = (sx (A - u), sy (B - u))). true_n: the
    curve's normal per profile point (None = flat there), as profile_n.

    Each side is its own strip (its corner vertices are not shared with the
    next side's), so the 45 deg mitre stays a crisp line in every LOD: on the
    sloped face the two sides meet at only about 30 deg, under SMOOTH_ANGLE, and
    kitlib.finish() re-marks sharp edges by angle, so shared corner vertices
    would merge both sides' normals into one fan (round 1's LOD1 "pillow"
    corners). Coincident vertices, so no crack."""
    n = len(profile)
    corners = [(1, 1), (-1, 1), (-1, -1), (1, -1)]     # CCW seen from the front... in Unity plan
    b = Builder()
    sides_between = ["top", "left", "bottom", "right"]   # corner k -> k+1
    segs = n if closed else n - 1
    for k in range(4):
        m = (k + 1) % 4
        side = sides_between[k]
        ck, cm = corners[k], corners[m]
        ring_k = [b.vert((ck[0] * (A_HALF - u), ck[1] * (B_HALF - u), w)) for u, w in profile]
        ring_m = [b.vert((cm[0] * (A_HALF - u), cm[1] * (B_HALF - u), w)) for u, w in profile]
        for i in range(segs):
            j = (i + 1) % n
            out = side_normal(side, left_normal(profile[i], profile[j]))
            normals = None
            if true_n is not None:
                ni = None if true_n[i] is None else side_normal(side, true_n[i])
                nj = None if true_n[j] is None else side_normal(side, true_n[j])
                normals = (ni, ni, nj, nj)
            b.face((ring_k[i], ring_m[i], ring_m[j], ring_k[j]), out, normals)
    return b.object(kit, name, ALU, lods), b


def sheet_quad(kit, lods="012"):
    """§2.5: one quad at w = 4.55, UV 0..1 across, v0/v1 skip the padding rows.
    Corners CCW seen from the front (the viewer's right is -X)."""
    hx, hy, z = SHEET[0] / 2 * MM, SHEET[1] / 2 * MM, SHEET_W * MM
    obj = kc.quad_uv(kit, [(hx, -hy, z), (-hx, -hy, z), (-hx, hy, z), (hx, hy, z)],
                     [(0.0, UV_V0), (1.0, UV_V0), (1.0, UV_V1), (0.0, UV_V1)], PRINT, name="printed sheet")
    obj["fr_lods"] = lods
    return obj


def lens_box(kit):
    """Kit_EvacPlacardLens: the top face at w 5.55 and four edge quads down to
    w 4.60; no bottom face (it would be coplanar with the opaque sheet)."""
    hx, hy = SHEET[0] / 2, SHEET[1] / 2
    b = Builder()
    top = [b.vert((x, y, LENS_TOP)) for x, y in ((hx, -hy), (-hx, -hy), (-hx, hy), (hx, hy))]
    low = [b.vert((x, y, LENS_EDGE_LOW)) for x, y in ((hx, -hy), (-hx, -hy), (-hx, hy), (hx, hy))]
    b.face(top, (0.0, 0.0, 1.0))
    outs = [(0.0, -1.0, 0.0), (-1.0, 0.0, 0.0), (0.0, 1.0, 0.0), (1.0, 0.0, 0.0)]
    for k in range(4):
        m = (k + 1) % 4
        b.face((top[k], low[k], low[m], top[m]), outs[k])
    obj = b.object(kit, "lens", LENS, "0")
    return obj, b


# ---------------------------------------------------------------- normals
def finalize(kit, parts=None):
    """Normals that survive kitlib.finish() and the FBX round trip (the
    interactables G1 order: shade smooth -> sharp by SMOOTH_ANGLE -> custom
    normals; finish() joins and re-marks sharp edges with the same angle, so
    the custom-normal fans are unchanged). Builder parts get their TRUE curve
    normals (round 2); any other part (the sheet quad) the G1 WEIGHTED_NORMAL."""
    for obj in parts or kit.parts:
        me = obj.data
        me.shade_smooth()
        me.set_sharp_from_angle(angle=math.radians(SMOOTH_ANGLE))
        table = NORMALS.pop(obj.name, None)
        attr = me.attributes.get(NID)
        if table is not None and attr is not None:
            loops = [None] * len(me.loops)
            for poly in me.polygons:
                entry = table[attr.data[poly.index].value]
                for li in poly.loop_indices:
                    nrm = entry.get(me.loops[li].vertex_index) if entry else None
                    loops[li] = tuple(nrm) if nrm is not None else tuple(poly.normal)
            me.normals_split_custom_set(loops)
            me.attributes.remove(me.attributes[NID])
            continue
        if attr is not None:
            me.attributes.remove(attr)
        mod = obj.modifiers.new("wn", "WEIGHTED_NORMAL")
        mod.mode = "FACE_AREA"
        mod.weight = 50
        mod.keep_sharp = True


# ------------------------------------------------------------------- query
def lod_parts(kit, level):
    return [o for o in kit.parts if str(level) in str(o.get("fr_lods", "0"))]


def tris_of(objs):
    dg = bpy.context.evaluated_depsgraph_get()
    t = 0
    for o in objs:
        ev = o.evaluated_get(dg)
        me = ev.to_mesh()
        t += sum(len(p.vertices) - 2 for p in me.polygons)
        ev.to_mesh_clear()
    return t


def to_unity_mm(co):
    return (-co[0] / MM, co[2] / MM, -co[1] / MM)


def bounds_unity_mm(objs):
    lo, hi = kc.eval_bounds_unity(None, objs)
    return [x / MM for x in lo], [x / MM for x in hi]
