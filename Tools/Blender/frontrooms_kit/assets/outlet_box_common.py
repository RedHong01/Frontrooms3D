"""O3 helper: floor boxes, surface boxes, EMT conduit and the powered panel
base (research/outlets/10_spec.md §1.1-§1.3, §2.10, §5.0, §5.3; period
research 01 §6). NOT AN ASSET: no NAME, no build(). Imported by the O3
modules (outlet_floorbox*.py, outlet_handy_box.py, outlet_conduit*.py,
outlet_panel_powered.py). It does not import the O1/O2 helpers: the 5-15R
face below is O3's own copy of the §1.2 numbers, asserted at import time.

FRAMES. Geometry is written in MILLIMETRES in a local Frame (origin, u, v,
n): u = right as seen from the front, v = up, n = out toward the viewer
(u x v = n, right-handed). A wall face on the kit front is
Frame(o, +X, +Z, -Y): kit -Y is Unity +Z (kitlib export()). Rings are listed
counter-clockwise as seen from +n, so band(back_ring, front_ring) faces out
of a solid and band(upper_ring, lower_ring) faces into a hole.

WHAT LIVES HERE
* §1.2 numbers (5-15R face, duplex openings, countersink) and their asserts.
* Outline generators (mm, CCW): round-with-flats (fixed vertex count, so
  grown/inset rings always band), rounded rectangles, chamfered slot
  rectangles, the U ground hole, circles, hexagons.
* MB: a small bmesh builder (rings, bands, caps, hole fills with
  triangle_fill, outward orientation, multi-slot parts).
* face_515(): one receptacle face at hero / mid / lod1 detail; face_lod2().
* tomb_body() + build_tombstone(): the cast floor fitting (both SFH sizes).
* flange(), sweep() (EMT tube, connector neck), slotted screws (oval head
  for countersinks, pan head for sheet metal; EXACT boolean slot).
* finalize(): the per-part recipe the interactables G1 probe proved survives
  kitlib.finish() and FBX (apply modifiers -> weld -> triangulate n-gons ->
  shade smooth -> sharp by angle -> paint fr_wear -> WEIGHTED_NORMAL).
* paint_wear(): fr_wear colour attribute (BYTE_COLOR, face corner):
  R = hand grime, G = edge wear, B = cavity, 1 = untouched; PlasticBlack
  faces get B <= 0.35.
* box_meta(): sidecar fields, tags and the triangle / envelope asserts.
* LOD: hand-built LOD parts carry obj["fr_lods"] ("0", "1", "2" or a mix);
  LOD1/LOD2 parts are built ONLY when kitlib.Kit.make_lods exists (spec
  §1.1, P-1/P-1b). Until then every FBX holds LOD0 only.

SHARED O3 NUMBERS (ESTIMATES that became numbers; see the build note)
* E_AXIS = 11.5 mm: the EMT axis stands this far off the wall face (the
  tube's back is 2.55 mm off the wall; the strap is 22 mm proud; the
  connector socket just clears the wall). Conduit, strap and the handy
  box's offset connector all use it, so a tile placed at conduit_base lines
  up with the socket exactly.
* Screws on O3 kits are BAKED into the mesh (LOD0 parts), not drawn by R3
  from screw_k anchors: the back face of Kit_FloorBoxTombstone2 and the
  plain GameObject spawns of the Office pods could not show R3 screws.

SEGMENT COUNTS. Hero curved outlines (device faces, openings, ground holes,
countersinks) use PC_HERO = 64 per full circle (spec §1.1). Screw heads
and tubes use fewer where the budget forces it; each module says so and the
build note gives the chord error at 0.3 m (FOV 76, 1080p = 2.30 px/mm).
"""

import math

import bmesh
import bpy
from mathutils import Vector

import kitlib

MM = 0.001

AL = "Prop_Aluminium"      # cast / galvanized / die-cast metal (existing slot)
NI = "Prop_NylonIvory"     # device nylon (NEW tint-only slot, P-4b)
PB = "Prop_PlasticBlack"   # slots, cavities
BR = "Prop_Brass"          # contacts


def _hex(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))


# P-4b: #E2D9BF, smoothness 0.55 -> roughness 0.45 (preview colour only).
kitlib.register_slot(NI, _hex("#E2D9BF"), 0.45, 0.0)

# ----------------------------------------------------------- §1.2 numbers (mm)
FACE_D = 32.5            # 5-15R face: round with flats
FACE_FLAT = 13.9         # flats at +-13.9 of the face centre
FACE_PROUD = 1.0         # face stands 1.0 proud of the field
BACK_PLANE = 1.5         # device back plane behind the field (shows in the gap)
OPEN_D = 33.3            # plate opening: round with flats
OPEN_FLAT = 14.3
DUPLEX_HALF = 19.45      # opening / face centres at +-19.45 (1-17/32 in apart)
NEUTRAL = (2.0, 8.5, -6.35, 3.0)   # (w, h, u, v) in the face frame, ground down
HOT = (2.0, 7.0, 6.35, 3.0)
GROUND_D = 5.0           # U hole: round bottom, flat top
GROUND_V = -8.9          # centre of the round
SLOT_DEPTH = 7.0         # from the face top
CONTACT_T = 0.4          # brass leaf thickness
CONTACT_DEPTH = 3.0      # leaves start this far below the face top
MOUTH_CHAMFER = 0.15     # slot mouth chamfer (1 segment)
FACE_EDGE_R = 0.6        # device-face edge round (3 segments)
OPEN_EDGE_R = 0.5        # opening edge round (3 segments)
SCREW_SEAT_D = 6.6       # #6-32 oval head
SCREW_CROWN = 1.0
SCREW_SLOT_W, SCREW_SLOT_DEPTH = 0.8, 0.6
CSK_D = 7.4              # countersink diameter at the field, 82 deg
CSK_HALF_ANGLE = 41.0

# ESTIMATES that became numbers in this helper (reported in the build note).
GROUND_SIDE = 2.0        # U hole: straight sides 2.0 above the round's centre
GROUND_CORNER_R = 0.5    # U hole: top corner radius
SLOT_CORNER = 0.25       # blade slot corner chamfer (moulded slots are not sharp)
CONTACT_H = 2.5          # leaf height along the slot depth
PC_HERO = 64             # segments per full circle at hero detail (spec §1.1)
E_AXIS = 11.5            # EMT axis off the wall face
EMT_OD = 17.9            # 1/2 in EMT outside diameter (0.706 in)


def _close(a, b, tol=0.05):
    return abs(a - b) <= tol


# Asserts every helper runs (spec §1.2).
assert _close((OPEN_D - FACE_D) / 2, 0.4), "face-to-opening clearance must be 0.4"
assert _close(OPEN_FLAT - FACE_FLAT, 0.4), "flat clearance must be 0.4"
assert _close(HOT[2] - NEUTRAL[2], 12.7), "blade pitch 12.7"
assert _close(NEUTRAL[3] - GROUND_V, 11.9), "ground 11.9 below the blade line"
assert GROUND_V + GROUND_SIDE < NEUTRAL[3] - NEUTRAL[1] / 2, "ground hole below the blade slots"
assert NEUTRAL[1] > HOT[1], "neutral slot is the longer one"
assert _close(2 * DUPLEX_HALF, 38.9), "duplex pitch 1-17/32 in"
assert FACE_PROUD <= 1.5
assert _close(NEUTRAL[0], 2.0) and _close(NEUTRAL[1], 8.5) and _close(HOT[1], 7.0) and _close(GROUND_D, 5.0)


# ------------------------------------------------------------------ frames
class Frame:
    """Local frame in Blender metres; local coordinates in millimetres."""

    def __init__(self, o, u, v, n):
        self.o, self.u, self.v, self.n = Vector(o), Vector(u).normalized(), Vector(v).normalized(), Vector(n).normalized()

    def p(self, a, b, c=0.0):
        return self.o + (self.u * a + self.v * b + self.n * c) * MM

    def at(self, a, b, c=0.0):
        return Frame(self.p(a, b, c), self.u, self.v, self.n)

    def rot(self, deg):
        """Turn u and v by ``deg`` counter-clockwise about n."""
        r = math.radians(deg)
        u = self.u * math.cos(r) + self.v * math.sin(r)
        v = -self.u * math.sin(r) + self.v * math.cos(r)
        return Frame(self.o, u, v, self.n)


def front_frame(x, y, z):
    """Kit front (-Y) face frame at Blender metres (x, y, z)."""
    return Frame((x, y, z), (1, 0, 0), (0, 0, 1), (0, -1, 0))


def back_frame(x, y, z):
    """Kit back (+Y) face frame."""
    return Frame((x, y, z), (-1, 0, 0), (0, 0, 1), (0, 1, 0))


def up_frame(x, y, z):
    """Facing +Z (flanges, connector hubs)."""
    return Frame((x, y, z), (1, 0, 0), (0, 1, 0), (0, 0, 1))


def to_unity(p):
    return (-p[0], p[2], -p[1])


# ----------------------------------------------------------------- outlines
def _rot2(pts, deg, cu=0.0, cv=0.0):
    r = math.radians(deg)
    c, s = math.cos(r), math.sin(r)
    return [(cu + x * c - y * s, cv + x * s + y * c) for x, y in pts]


def flats_n(R, F, pc):
    """Arc segment count of a round-with-flats outline (fixed per feature)."""
    th = math.asin(min(1.0, F / R))
    return max(2, int(math.ceil(2 * th / (2 * math.pi / pc) - 1e-6)))


def round_flats(R, F, n):
    """Circle of radius R clipped by flats at v = +-F; CCW from the right arc.
    ``n`` segments per arc (use flats_n of the NOMINAL size so grown and
    inset rings keep the same vertex count)."""
    th = math.asin(min(1.0, F / R))
    pts = []
    for k in range(n + 1):
        a = -th + 2 * th * k / n
        pts.append((R * math.cos(a), R * math.sin(a)))
    for k in range(n + 1):
        a = math.pi - th + 2 * th * k / n
        pts.append((R * math.cos(a), R * math.sin(a)))
    return pts


def rrect(w, h, r, per_corner=8, cu=0.0, cv=0.0):
    """Rounded rectangle, CCW, corners from bottom-right."""
    r = max(min(r, w / 2 - 1e-3, h / 2 - 1e-3), 0.05)
    pts = []
    for x, y, a0 in ((w / 2 - r, -h / 2 + r, -90), (w / 2 - r, h / 2 - r, 0),
                     (-w / 2 + r, h / 2 - r, 90), (-w / 2 + r, -h / 2 + r, 180)):
        for k in range(per_corner + 1):
            a = math.radians(a0 + 90.0 * k / per_corner)
            pts.append((cu + x + r * math.cos(a), cv + y + r * math.sin(a)))
    return pts


def slot_rect(w, h, cu, cv, ch=0.0):
    """Blade slot, CCW; ch > 0 chamfers the corners (8 vertices)."""
    hw, hh = w / 2, h / 2
    if ch <= 0:
        return [(cu + hw, cv - hh), (cu + hw, cv + hh), (cu - hw, cv + hh), (cu - hw, cv - hh)]
    c = min(ch, hw * 0.45, hh * 0.45)
    return [(cu + hw, cv - hh + c), (cu + hw, cv + hh - c), (cu + hw - c, cv + hh), (cu - hw + c, cv + hh),
            (cu - hw, cv + hh - c), (cu - hw, cv - hh + c), (cu - hw + c, cv - hh), (cu + hw - c, cv - hh)]


def ground_u(e=0.0, semi=32, corner=2, cu=0.0, cv=GROUND_V):
    """U ground hole grown by e: round bottom (R 2.5) centred on cv, straight
    sides up GROUND_SIDE, flat top with GROUND_CORNER_R corners. CCW."""
    R = GROUND_D / 2 + e
    top = cv + GROUND_SIDE + e
    r = max(GROUND_CORNER_R + e, 0.05)
    pts = [(cu + R, cv)]
    for k in range(corner + 1):
        a = math.radians(90.0 * k / corner)
        pts.append((cu + R - r + r * math.cos(a), top - r + r * math.sin(a)))
    for k in range(corner + 1):
        a = math.radians(90.0 + 90.0 * k / corner)
        pts.append((cu - R + r + r * math.cos(a), top - r + r * math.sin(a)))
    for k in range(semi):
        a = math.pi + math.pi * k / semi
        pts.append((cu + R * math.cos(a), cv + R * math.sin(a)))
    return pts


def circle(r, segs, cu=0.0, cv=0.0, phase=0.0):
    return [(cu + r * math.cos(phase + 2 * math.pi * k / segs), cv + r * math.sin(phase + 2 * math.pi * k / segs))
            for k in range(segs)]


def hexagon(af, cu=0.0, cv=0.0):
    """Hexagon with across-flats ``af``; vertices on +-u, flats facing +-v."""
    return circle(af / 2 / math.cos(math.pi / 6), 6, cu, cv, phase=0.0)


def opening_fn(cu, cv, rot=0.0, pc=PC_HERO):
    """Duplex opening outline grown by e, centred (cu, cv), rotated rot deg."""
    n = flats_n(OPEN_D / 2, OPEN_FLAT, pc)
    return lambda e: _rot2(round_flats(OPEN_D / 2 + e, OPEN_FLAT + e, n), rot, cu, cv)


# ------------------------------------------------------------- mesh builder
class MB:
    """bmesh builder for one kit part with any number of slots."""

    def __init__(self):
        self.bm = bmesh.new()
        self.slots = []

    def mi(self, slot):
        if slot not in self.slots:
            self.slots.append(slot)
        return self.slots.index(slot)

    def vert(self, fr, a, b, c=0.0):
        return self.bm.verts.new(fr.p(a, b, c))

    def ring(self, fr, pts, c):
        return [self.bm.verts.new(fr.p(a, b, c)) for a, b in pts]

    def face(self, verts, slot):
        f = self.bm.faces.new(verts)
        f.material_index = self.mi(slot)
        return f

    def band(self, ra, rb, slot, closed=True):
        n = len(ra)
        assert n == len(rb), "ring sizes differ (%d vs %d)" % (n, len(rb))
        out = []
        for i in range(n if closed else n - 1):
            j = (i + 1) % n
            out.append(self.face((ra[i], ra[j], rb[j], rb[i]), slot))
        return out

    def cap(self, ring, slot, front=True):
        return self.face(ring if front else list(reversed(ring)), slot)

    def fan(self, ring, apex, slot):
        n = len(ring)
        return [self.face((ring[i], ring[(i + 1) % n], apex), slot) for i in range(n)]

    def fill(self, outer, holes, slot, normal):
        """Triangulated region between ``outer`` and the hole rings."""
        edges = []
        for r in [outer] + list(holes):
            for i in range(len(r)):
                a, b = r[i], r[(i + 1) % len(r)]
                e = self.bm.edges.get((a, b)) or self.bm.edges.new((a, b))
                edges.append(e)
        res = bmesh.ops.triangle_fill(self.bm, use_beauty=True, use_dissolve=False, edges=edges)
        faces = [g for g in res["geom"] if isinstance(g, bmesh.types.BMFace)]
        normal = Vector(normal)
        for f in faces:
            f.material_index = self.mi(slot)
            f.normal_update()
            if f.normal.dot(normal) < 0:
                f.normal_flip()
        return faces

    def orient(self, faces, interior):
        interior = Vector(interior)
        for f in faces:
            f.normal_update()
            if f.normal.dot(f.calc_center_median() - interior) < 0:
                f.normal_flip()

    def box(self, fr, u0, u1, v0, v1, c0, c1, slot, faces="all"):
        """Axis box in a frame (mm). faces: 'all' or a string of sides to
        keep, out of 'udlrfb' (up, down, left, right, front = c1, back = c0)."""
        P = {}
        for iu, a in enumerate((u0, u1)):
            for iv, b in enumerate((v0, v1)):
                for ic, c in enumerate((c0, c1)):
                    P[iu, iv, ic] = self.bm.verts.new(fr.p(a, b, c))
        quads = {"l": [(0, 0, 0), (0, 1, 0), (0, 1, 1), (0, 0, 1)], "r": [(1, 0, 0), (1, 0, 1), (1, 1, 1), (1, 1, 0)],
                 "d": [(0, 0, 0), (0, 0, 1), (1, 0, 1), (1, 0, 0)], "u": [(0, 1, 0), (1, 1, 0), (1, 1, 1), (0, 1, 1)],
                 "b": [(0, 0, 0), (1, 0, 0), (1, 1, 0), (0, 1, 0)], "f": [(0, 0, 1), (0, 1, 1), (1, 1, 1), (1, 0, 1)]}
        keep = "udlrfb" if faces == "all" else faces
        out = [self.face([P[k] for k in quads[s]], slot) for s in keep]
        centre = fr.p((u0 + u1) / 2, (v0 + v1) / 2, (c0 + c1) / 2)
        self.orient(out, centre)
        loose = [v for v in P.values() if not v.link_faces]
        if loose:
            bmesh.ops.delete(self.bm, geom=loose, context="VERTS")
        return out

    def to_part(self, kit, name, lods="0", uv="metres"):
        if not self.slots:
            raise ValueError("empty part %s" % name)
        obj = kit._new_object(name, self.bm, self.slots[0], uv, "xz")
        for s in self.slots[1:]:
            obj.data.materials.append(kit._material(s))
        obj["fr_lods"] = lods
        self.bm = None
        return obj


def profile_stack(mb, fr, outline, prof, slot):
    """Rings of outline(d) at heights c for prof = [(d, c), ...] (back to
    front, d = inset), banded. Returns the rings."""
    rings = [mb.ring(fr, outline(d), c) for d, c in prof]
    for a, b in zip(rings, rings[1:]):
        mb.band(a, b, slot)
    return rings


def hole_stack(mb, fr, outline, prof, slot):
    """Rings of outline(e) (e = growth) for prof = [(e, c), ...] from the
    mouth downward, banded so the walls face into the hole."""
    rings = [mb.ring(fr, outline(e), c) for e, c in prof]
    for a, b in zip(rings, rings[1:]):
        mb.band(a, b, slot)
    return rings


def round_profile(r, segs, top, start_d=0.0):
    """Quarter round from a vertical wall (d = start_d, c = top - r) to a flat
    top (d = start_d + r, c = top). Returns [(d, c), ...] without the wall
    start point."""
    out = []
    for k in range(1, segs + 1):
        a = math.radians(90.0 * k / segs)
        out.append((start_d + r * (1 - math.cos(a)), top - r + r * math.sin(a)))
    return out


def mouth_profile(r, segs, top, bottom):
    """Rounded hole mouth: from the field (grown by r, at top) down a
    quarter round to the wall, then to ``bottom``. [(e, c), ...]"""
    out = []
    for k in range(segs, -1, -1):
        a = math.radians(90.0 * k / segs)
        out.append((r * (1 - math.cos(a)), top - r + r * math.sin(a)))
    out.append((0.0, bottom))
    return out


# ---------------------------------------------------------- receptacle face
FACE_LEVELS = {
    # pc: segments per circle; rr/rs: edge round radius/segments; ch: slot
    # mouth chamfer; depth: slot depth; semi/corner: U-hole segments;
    # sc: blade slot corner chamfer; contacts: brass leaves.
    "hero": dict(pc=PC_HERO, rr=FACE_EDGE_R, rs=3, ch=MOUTH_CHAMFER, depth=SLOT_DEPTH, semi=32, corner=2,
                 sc=SLOT_CORNER, contacts=True),
    # Hero at 48 per circle (ground U 24 per half circle): for kits whose
    # budget cannot hold four 64-segment faces (Kit_FloorBoxTombstone2).
    "hero48": dict(pc=48, rr=FACE_EDGE_R, rs=3, ch=MOUTH_CHAMFER, depth=SLOT_DEPTH, semi=24, corner=2,
                   sc=SLOT_CORNER, contacts=True),
    # Furniture-base duplex (panel rail, seen from >= 1 m): 32 per circle,
    # a 1-segment edge, 3 mm slots, no contacts.
    "mid": dict(pc=32, rr=0.4, rs=1, ch=0.0, depth=3.0, semi=10, corner=1, sc=0.0, contacts=False),
    # LOD1 (spec §1.1): 24-segment faces, slots as 0.5 mm black insets.
    "lod1": dict(pc=24, rr=0.0, rs=0, ch=0.0, depth=0.5, semi=6, corner=1, sc=0.0, contacts=False),
}


def face_515(mb, fr, level="hero", contact_slot=BR):
    """One NEMA 5-15R face (ground down in the frame). fr: origin = face
    centre ON THE FIELD (c = 0), n out. The face stands FACE_PROUD out of the
    field; its side wall starts at the device back plane (c = -BACK_PLANE)."""
    L = FACE_LEVELS[level]
    pc, rr, rs = L["pc"], L["rr"], L["rs"]
    n = flats_n(FACE_D / 2, FACE_FLAT, pc)
    outline = lambda d: round_flats(FACE_D / 2 - d, FACE_FLAT - d, n)
    prof = [(0.0, -BACK_PLANE)]
    if rs:
        prof.append((0.0, FACE_PROUD - rr))
        prof += round_profile(rr, rs, FACE_PROUD)
    else:
        prof.append((0.0, FACE_PROUD))
    rings = profile_stack(mb, fr, outline, prof, NI)
    top_c = FACE_PROUD
    floor_c = FACE_PROUD - L["depth"]
    holes = []
    # Blade slots (black walls, chamfered NI mouth; the duplicate ring at the
    # chamfer foot is welded by finalize()).
    for w, h, u0, v0 in (NEUTRAL, HOT):
        fn = lambda e, w=w, h=h, u0=u0, v0=v0: slot_rect(w + 2 * e, h + 2 * e, u0, v0, L["sc"] + (e * 0.4 if L["sc"] else 0))
        if L["ch"]:
            rs_ = hole_stack(mb, fr, fn, [(L["ch"], top_c), (0.0, top_c - L["ch"])], NI)
            walls = hole_stack(mb, fr, fn, [(0.0, top_c - L["ch"]), (0.0, floor_c)], PB)
            holes.append(rs_[0])
        else:
            walls = hole_stack(mb, fr, fn, [(0.0, top_c), (0.0, floor_c)], PB)
            holes.append(walls[0])
        mb.cap(walls[-1], PB, front=True)
    # Ground hole.
    gfn = lambda e: ground_u(e, L["semi"], L["corner"])
    if L["ch"]:
        rs_ = hole_stack(mb, fr, gfn, [(L["ch"], top_c), (0.0, top_c - L["ch"])], NI)
        walls = hole_stack(mb, fr, gfn, [(0.0, top_c - L["ch"]), (0.0, floor_c)], PB)
        holes.append(rs_[0])
    else:
        walls = hole_stack(mb, fr, gfn, [(0.0, top_c), (0.0, floor_c)], PB)
        holes.append(walls[0])
    mb.cap(walls[-1], PB, front=True)
    mb.fill(rings[-1], holes, NI, fr.n)
    # Brass contact pairs in the blade slots: two leaves against the long
    # walls, 0.4 thick, CONTACT_DEPTH below the face top. Only the faces a
    # viewer can reach are built (top edge and the inner face).
    if L["contacts"]:
        c1 = top_c - CONTACT_DEPTH
        c0 = c1 - CONTACT_H
        for w, h, u0, v0 in (NEUTRAL, HOT):
            for s in (-1, 1):
                ua = u0 + s * (w / 2 - 0.02)
                ub = u0 + s * (w / 2 - 0.02 - CONTACT_T)
                lo, hi = min(ua, ub), max(ua, ub)
                side = "l" if s > 0 else "r"
                mb.box(fr, lo, hi, v0 - h / 2 + 0.35, v0 + h / 2 - 0.35, c0, c1, contact_slot, faces="f" + side)
    return rings


def face_lod2(mb, fr, slot=PB, lift=0.3):
    """LOD2 face: a flat dark hexagon 0.3 mm proud of the field (spec §1.1)."""
    pts = [(FACE_D / 2 * math.cos(math.radians(60 * k)), FACE_D / 2 * 0.88 * math.sin(math.radians(60 * k))) for k in range(6)]
    ring = mb.ring(fr, pts, lift)
    return mb.cap(ring, slot, front=True)


def _face_frames(plate_fr, field_c, sideways):
    out = []
    for s in (1, -1):
        if sideways:
            out.append(plate_fr.at(s * DUPLEX_HALF, 0.0, field_c).rot(90.0))
        else:
            out.append(plate_fr.at(0.0, s * DUPLEX_HALF, field_c))
    return out


def duplex(mb, plate_fr, field_c, level="hero", sideways=False, contact_slot=BR):
    """Two faces on a plate frame (u right, v up), field at c = field_c.
    Sideways turns the device 90 deg CCW (ground holes point to +u)."""
    for fr in _face_frames(plate_fr, field_c, sideways):
        face_515(mb, fr, level, contact_slot)


def duplex_lod2(mb, plate_fr, field_c, sideways=False):
    for fr in _face_frames(plate_fr, field_c, sideways):
        face_lod2(mb, fr)


def face_centres(plate_fr, field_c, sideways):
    """Blender points of the two face tops (anchors for P3 plug props)."""
    return [fr.p(0.0, 0.0, FACE_PROUD) for fr in _face_frames(plate_fr, field_c, sideways)]


def duplex_openings(sideways=False, pc=PC_HERO):
    if sideways:
        return [opening_fn(s * DUPLEX_HALF, 0.0, 90.0, pc) for s in (1, -1)]
    return [opening_fn(0.0, s * DUPLEX_HALF, 0.0, pc) for s in (1, -1)]


def duplex_span(sideways):
    """Half extents (u, v) of the two openings together."""
    a, b = OPEN_D / 2, OPEN_FLAT
    return (DUPLEX_HALF + b, a) if sideways else (a, DUPLEX_HALF + b)


def backing(mb, fr, w, h, r, c, slot=NI, per_corner=4):
    """Device colour behind the openings (the back plane seen in the gap)."""
    ring = mb.ring(fr, rrect(w, h, r, per_corner), c)
    return mb.cap(ring, slot, front=True)


def countersink(mb, fr, cu, cv, field, slot=AL, segs=PC_HERO, depth=1.0):
    """82 deg countersink of CSK_D at the field, ``depth`` deep, floor capped.
    Returns the top ring (a hole for the surrounding fill)."""
    top = mb.ring(fr, circle(CSK_D / 2, segs, cu, cv), field)
    bot = mb.ring(fr, circle(CSK_D / 2 - depth * math.tan(math.radians(CSK_HALF_ANGLE)), segs, cu, cv), field - depth)
    mb.band(top, bot, slot)
    mb.cap(bot, slot, front=True)
    return top


# ----------------------------------------------------------------- housings
def tomb_outline(hw, H, zb, rt, d, arc_segs):
    """Front-view tombstone outline inset by d: an open path (x, z) from the
    bottom-left up over the two rounded top corners (radius rt) to the
    bottom-right. zb is the (hidden) foot inside the flange."""
    r = max(rt - d, 0.05)
    pts = [(-(hw - d), zb)]
    for k in range(arc_segs + 1):
        a = math.radians(180.0 - 90.0 * k / arc_segs)
        pts.append((-(hw - rt) + r * math.cos(a), (H - rt) + r * math.sin(a)))
    for k in range(arc_segs + 1):
        a = math.radians(90.0 - 90.0 * k / arc_segs)
        pts.append(((hw - rt) + r * math.cos(a), (H - rt) + r * math.sin(a)))
    pts.append(((hw - d), zb))
    return pts


def tomb_body(mb, hw, hd, H, zb, rt, re, arc_segs, edge_segs, slot=AL):
    """Cast floor-fitting housing (mm): the front view is a tombstone (flat
    sides, rounded top corners rt), extruded front to back (|y| <= hd) with
    every front and back edge rounded by re (edge_segs). Bottom open (it
    stands in the flange). Returns (back_ring, front_ring): the open paths
    bounding the flat back and front faces (close them for fills)."""
    stations = []
    if edge_segs:
        for k in range(edge_segs + 1):
            phi = math.radians(90.0 * k / edge_segs)
            stations.append((re - re * math.sin(phi), (hd - re) + re * math.cos(phi)))
        for k in range(edge_segs, -1, -1):
            phi = math.radians(90.0 * k / edge_segs)
            stations.append((re - re * math.sin(phi), -(hd - re) - re * math.cos(phi)))
    else:
        stations = [(0.0, hd), (0.0, -hd)]
    rings = []
    for d, y in stations:
        rings.append([mb.bm.verts.new((x * MM, y * MM, z * MM)) for x, z in tomb_outline(hw, H, zb, rt, d, arc_segs)])
    faces = []
    for a, b in zip(rings, rings[1:]):
        faces += mb.band(a, b, slot, closed=False)
    mb.orient(faces, (0.0, 0.0, (zb + H) / 2 * MM))
    return rings[0], rings[-1]


def flange(mb, fr, w, h, r, t, edge_r, edge_segs, per_corner=6, slot=AL):
    """Base flange on the carpet: fr origin = floor contact centre, n = +Z.
    Bottom open (it lies on the floor)."""
    outline = lambda d: rrect(w - 2 * d, h - 2 * d, r - d, per_corner)
    prof = [(0.0, 0.0)]
    if edge_segs:
        prof.append((0.0, t - edge_r))
        prof += round_profile(edge_r, edge_segs, t)
    else:
        prof.append((0.0, t))
    rings = profile_stack(mb, fr, outline, prof, slot)
    mb.fill(rings[-1], [], slot, fr.n)
    return rings


# -------------------------------------------------------------------- sweep
def sweep(mb, path, radii, segs, slot, cap_start=False, cap_end=False, ref=(0, 0, 1), phase=0.0):
    """Round sections along a polyline (Blender metres) with a radius per
    point (metres). Rings are CCW about the local tangent, so bands face out.
    Returns the rings."""
    pts = [Vector(p) for p in path]
    rings = []
    prev_side = None
    for k, p in enumerate(pts):
        if k == 0:
            t = (pts[1] - pts[0]).normalized()
        elif k == len(pts) - 1:
            t = (pts[-1] - pts[-2]).normalized()
        else:
            t = ((pts[k] - pts[k - 1]).normalized() + (pts[k + 1] - pts[k]).normalized()).normalized()
        if prev_side is None:
            r0 = Vector(ref)
            if abs(t.dot(r0)) > 0.9:
                r0 = Vector((1, 0, 0))
            side = t.cross(r0).normalized()
        else:
            side = (prev_side - t * prev_side.dot(t)).normalized()
        up = t.cross(side).normalized()
        side = up.cross(t).normalized()
        prev_side = side
        ring = []
        for i in range(segs):
            a = phase + 2 * math.pi * i / segs
            ring.append(mb.bm.verts.new(p + (side * math.cos(a) + up * math.sin(a)) * radii[k]))
        rings.append(ring)
    for a, b in zip(rings, rings[1:]):
        mb.band(a, b, slot)
    if cap_start:
        mb.face(list(reversed(rings[0])), slot)
    if cap_end:
        mb.face(rings[-1], slot)
    return rings


# ------------------------------------------------------------------- screws
def boolean_cut(obj, cutter_mb_fn, name="cutter"):
    """EXACT difference with a temporary cutter mesh built by
    cutter_mb_fn(mb) (Blender metres). Cut faces keep the part's slot 0."""
    cmb = MB()
    cutter_mb_fn(cmb)
    me = bpy.data.meshes.new(name)
    cmb.bm.to_mesh(me)
    cmb.bm.free()
    cutter = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(cutter)
    mod = obj.modifiers.new("cut", "BOOLEAN")
    mod.operation = "DIFFERENCE"
    mod.solver = "EXACT"
    mod.object = cutter
    try:
        mod.material_mode = "INDEX"
    except Exception:
        pass
    bpy.ops.object.select_all(action="DESELECT")
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.data.objects.remove(cutter, do_unlink=True)
    bpy.data.meshes.remove(me)
    return obj


def _slot_cutter(fr, deg, width, top, depth, reach, slot):
    f2 = fr.rot(deg)

    def cutter(cmb):
        cmb.box(f2, -reach, reach, -width / 2, width / 2, top - depth, top + 1.0, slot)
    return cutter


def screw_oval(kit, fr, slot_deg=0.0, segs=48, slot=AL, name="screw", lods="0"):
    """#6-32 oval head for a countersink: seat (Ø6.6) level with the field
    (c = 0), crown 1.0 proud, slot 0.8 x 0.6 across the head at slot_deg.
    fr origin = seat centre."""
    Rs = ((SCREW_SEAT_D / 2) ** 2 + SCREW_CROWN ** 2) / (2 * SCREW_CROWN)
    cz = SCREW_CROWN - Rs
    mb = MB()
    seat_r = SCREW_SEAT_D / 2
    rings = [mb.ring(fr, circle(seat_r - 0.55, segs), -0.6),
             mb.ring(fr, circle(seat_r, segs), -0.02)]
    for r in (2.45, 1.35):
        rings.append(mb.ring(fr, circle(r, segs), cz + math.sqrt(Rs * Rs - r * r)))
    for a, b in zip(rings, rings[1:]):
        mb.band(a, b, slot)
    apex = mb.vert(fr, 0.0, 0.0, SCREW_CROWN)
    mb.fan(rings[-1], apex, slot)
    mb.cap(rings[0], slot, front=False)
    obj = mb.to_part(kit, name, lods)
    boolean_cut(obj, _slot_cutter(fr, slot_deg, SCREW_SLOT_W, SCREW_CROWN, SCREW_SLOT_DEPTH, SCREW_SEAT_D, slot))
    _delete_faces_below(obj, fr, -0.3)
    return obj


def screw_pan(kit, fr, slot_deg=0.0, segs=32, slot=AL, head_d=6.6, head_h=1.9, slot_w=0.9, slot_depth=0.8,
              name="pan screw", lods="0"):
    """Slotted pan/binding head sitting ON a flat surface (sheet covers,
    straps, set screws): fr origin = head centre on the surface, n out."""
    r = head_d / 2
    mb = MB()
    prof = [(r, -0.25), (r, head_h * 0.42), (r - 0.35, head_h * 0.72), (r - 0.95, head_h * 0.93)]
    rings = [mb.ring(fr, circle(rr, segs), c) for rr, c in prof]
    for a, b in zip(rings, rings[1:]):
        mb.band(a, b, slot)
    top = mb.ring(fr, circle(r - 1.6, segs), head_h)
    mb.band(rings[-1], top, slot)
    mb.cap(top, slot, front=True)
    mb.cap(rings[0], slot, front=False)
    obj = mb.to_part(kit, name, lods)
    boolean_cut(obj, _slot_cutter(fr, slot_deg, slot_w, head_h, slot_depth, head_d, slot))
    _delete_faces_below(obj, fr, -0.15)
    return obj


def _delete_faces_below(obj, fr, c_mm):
    """Drop faces whose centre lies below c (hidden inside the host)."""
    me = obj.data
    bm = bmesh.new()
    bm.from_mesh(me)
    o, n = fr.o, fr.n
    doomed = [f for f in bm.faces if (f.calc_center_median() - o).dot(n) < c_mm * MM]
    if doomed:
        bmesh.ops.delete(bm, geom=doomed, context="FACES")
    bm.to_mesh(me)
    bm.free()


# -------------------------------------------------------------- wear / final
def paint_wear(obj, fn=None):
    """fr_wear colour attribute (BYTE_COLOR, CORNER). fn(P_unity_mm,
    N_unity, slot) -> (R grime, G edge wear, B cavity); 1 = untouched."""
    me = obj.data
    attr = me.color_attributes.get("fr_wear") or me.color_attributes.new("fr_wear", "BYTE_COLOR", "CORNER")
    names = [m.name.split(".")[0] if m is not None else "" for m in me.materials]
    mw = obj.matrix_world
    rot = mw.to_3x3()
    for poly in me.polygons:
        n = to_unity((rot @ poly.normal).normalized())
        slot = names[poly.material_index] if poly.material_index < len(names) else ""
        for li in poly.loop_indices:
            pw = mw @ me.vertices[me.loops[li].vertex_index].co
            p = tuple(x / MM for x in to_unity(pw))
            c = fn(p, n, slot) if fn is not None else (1.0, 1.0, 1.0)
            if slot == PB:
                c = (c[0], c[1], min(c[2], 0.35))
            attr.data[li].color_srgb = (max(0.0, min(1.0, c[0])), max(0.0, min(1.0, c[1])), max(0.0, min(1.0, c[2])), 1.0)
    me.color_attributes.active_color = attr
    return obj


def finalize(kit, smooth_angle, wear_fn=None, parts=None, weighted=True):
    """Per-part recipe (interactables G1 probe): apply modifiers ->
    weld -> triangulate n-gons -> shade smooth -> sharp by angle -> paint
    fr_wear -> WEIGHTED_NORMAL (applied by kit.finish())."""
    for obj in list(parts if parts is not None else kit.parts):
        bpy.ops.object.select_all(action="DESELECT")
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        for mod in list(obj.modifiers):
            bpy.ops.object.modifier_apply(modifier=mod.name)
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-7)
        big = [f for f in bm.faces if len(f.verts) > 4]
        if big:
            bmesh.ops.triangulate(bm, faces=big, quad_method="BEAUTY", ngon_method="BEAUTY")
        bm.to_mesh(obj.data)
        bm.free()
        me = obj.data
        me.shade_smooth()
        me.set_sharp_from_angle(angle=math.radians(smooth_angle))
        paint_wear(obj, wear_fn)
        if weighted:
            m = obj.modifiers.new("wn", "WEIGHTED_NORMAL")
            m.mode = "FACE_AREA"
            m.weight = 50
            m.keep_sharp = True


def tris(obj):
    return sum(len(p.vertices) - 2 for p in obj.data.polygons)


# ---------------------------------------------------------------------- LOD
def lods_enabled():
    """Hand-built LOD1/LOD2 parts only when kitlib can export them (P-1)."""
    return hasattr(kitlib.Kit, "make_lods")


def lod_parts(kit, level):
    return [o for o in kit.parts if str(level) in str(o.get("fr_lods", "0"))]


def lod_tris(kit):
    """Triangles per LOD membership (after finalize)."""
    return {k: sum(tris(o) for o in lod_parts(kit, k)) for k in ("0", "1", "2")}


def bounds_mm(objs):
    lo = [1e9] * 3
    hi = [-1e9] * 3
    for o in objs:
        mw = o.matrix_world
        for v in o.data.vertices:
            p = mw @ v.co
            for i in range(3):
                lo[i] = min(lo[i], p[i] / MM)
                hi[i] = max(hi[i], p[i] / MM)
    return lo, hi


def lod_meta(kit, module):
    d = list(getattr(module, "LOD_DISTANCES", (None, None, None)))
    kit.meta["lodDistances"] = [(-1.0 if x is None else float(x)) for x in d]
    kit.meta["lodRatios"] = [float(getattr(module, "LOD1_RATIO", 0) or 0), float(getattr(module, "LOD2_RATIO", 0) or 0)]
    kit.meta["lodBudget"] = list(getattr(module, "BUDGET", ()))
    kit.meta["lodHandBuilt"] = True


def box_meta(kit, module, family, plate_w, plate_h, wall=True, extra_tags=(), outlet_extra=None):
    """Sidecar meta (§1.1): no collider, tags, LOD fields and the outlet
    dict for R3. Asserts LOD0 triangles within +-15 % of BUDGET (and LOD1 /
    LOD2 when those parts were built) and that nothing of a wall kit lies
    behind the wall plane. Returns (LOD0 tris, lo_mm, hi_mm) in Blender mm."""
    kit.no_collider()
    kit.tag("outlet", family, *(("wall_flush",) if wall else ()), *extra_tags)
    lod_meta(kit, module)
    lod0 = lod_parts(kit, 0)
    lo, hi = bounds_mm(lod0)
    out = {"plateW": round(plate_w * MM, 5), "plateH": round(plate_h * MM, 5),
           "proud": round((-lo[1] if wall else hi[2]) * MM, 5), "screws": 0, "screwsBaked": True,
           "family": family, "plateSlot": AL, "deviceSlot": NI}
    if outlet_extra:
        out.update(outlet_extra)
    kit.meta["outlet"] = out
    kit.meta["partFrame"] = ("origin = wall-face point (Blender y = 0 = wall plane); front = Unity +Z (kit -Y); +Y up"
                             if wall else "origin = floor contact centre (z = 0); front = Unity +Z (kit -Y); +Y up")
    budget = getattr(module, "BUDGET")
    counts = lod_tris(kit)
    print("[o3] %s LOD0 %d tris (budget %d, %+.1f %%); bounds mm x %.2f..%.2f y %.2f..%.2f z %.2f..%.2f" % (
        kit.name, counts["0"], budget[0], 100.0 * (counts["0"] - budget[0]) / budget[0],
        lo[0], hi[0], lo[1], hi[1], lo[2], hi[2]))
    for o in lod0:
        print("[o3]    %-26s %5d tris  %s" % (o.name, tris(o), ",".join(m.name for m in o.data.materials)))
    assert abs(counts["0"] - budget[0]) <= 0.15 * budget[0], "%s: LOD0 %d tris vs budget %d" % (kit.name, counts["0"], budget[0])
    for lv in (1, 2):
        if lods_enabled() and len(budget) > lv and budget[lv] and lod_parts(kit, lv):
            n = counts[str(lv)]
            print("[o3] %s LOD%d %d tris (budget %d, %+.1f %%)" % (kit.name, lv, n, budget[lv], 100.0 * (n - budget[lv]) / budget[lv]))
            assert abs(n - budget[lv]) <= 0.15 * budget[lv], "%s: LOD%d %d tris vs budget %d" % (kit.name, lv, n, budget[lv])
    if wall:
        assert lo[1] <= 1e-6 and hi[1] <= 1e-3, "%s: geometry behind the wall plane (%.3f mm)" % (kit.name, hi[1])
    for o in kit.parts:
        assert o.data.color_attributes.get("fr_wear") is not None, "%s: part %s has no fr_wear" % (kit.name, o.name)
    return counts["0"], lo, hi


# ------------------------------------------------------- floor fitting builder
def build_tombstone(kit, module, W, D, H, inset, rt, re, zc, sides, flange_r=8.0, flange_t=2.0,
                    level="hero", pc_open=PC_HERO, csk_segs=PC_HERO, screw_segs=48, arc_segs=10, edge_segs=3):
    """SFH-type above-floor fitting (mm): flange W x D x flange_t on the
    carpet, a cast tombstone housing inset by ``inset`` up to H, one sideways
    duplex per entry of ``sides`` ("front" = kit -Y = Unity +Z, "back").
    Origin = floor contact centre. Returns the anchors written."""
    hw, hd = W / 2 - inset, D / 2 - inset
    zb = 1.0                                  # housing foot, hidden in the flange
    span_u, span_v = duplex_span(True)

    def wear(p, n, slot):
        x, y, z = p                           # Unity mm: x right-left, y up, z front
        r, g = 1.0, 1.0
        if abs(n[2]) > 0.9 and abs(x) < span_u + 9 and abs(y - zc) < span_v + 9 and abs(z) > hd - 0.5:
            r = 0.85                          # hands round the receptacle faces
        if slot == AL and 0.25 < n[1] < 0.97 and y > flange_t + 0.5:
            g = 0.8                           # scuffed shoulders and top edges
        if slot == AL and y <= flange_t + 0.01 and 0.2 < n[1] < 0.98:
            g = 0.7                           # flange edge (shoes, vacuum)
        return (r, g, 1.0)

    # LOD0 ------------------------------------------------------------------
    mb = MB()                                  # dominant slot first (AL)
    flange(mb, up_frame(0, 0, 0), W, D, flange_r, flange_t, 0.8, 2, per_corner=6)
    mb.to_part(kit, "flange", "0")

    mb = MB()
    back_ring, front_ring = tomb_body(mb, hw, hd, H, zb, rt, re, arc_segs=arc_segs, edge_segs=edge_segs)
    frames = {"front": front_frame(0, -hd * MM, zc * MM), "back": back_frame(0, hd * MM, zc * MM)}
    rings = {"front": front_ring, "back": back_ring}
    wall = 3.0                                # cast wall behind the openings
    for side in ("front", "back"):
        fr = frames[side]
        if side in sides:
            holes = [hole_stack(mb, fr, fn, mouth_profile(OPEN_EDGE_R, 3, 0.0, -wall), AL)[0]
                     for fn in duplex_openings(sideways=True, pc=pc_open)]
            holes.append(countersink(mb, fr, 0.0, 0.0, 0.0, segs=csk_segs))
            mb.fill(rings[side], holes, AL, fr.n)
        else:
            f = mb.face(rings[side], AL)
            mb.orient([f], (0.0, 0.0, (zb + H) / 2 * MM))
    mb.to_part(kit, "housing", "0")

    anchors = {}
    slot_angles = {"front": 37.0, "back": 112.0}
    for side in sides:
        fr = frames[side]
        mb = MB()
        duplex(mb, fr, 0.0, level, sideways=True)
        backing(mb, fr, 2 * span_u + 3.0, 2 * span_v + 3.0, 3.0, -BACK_PLANE)
        mb.to_part(kit, "device_" + side, "0")
        screw_oval(kit, fr, slot_angles[side], segs=screw_segs, name="screw_" + side)
        a, b = face_centres(fr, 0.0, True)
        sfx = "" if side == "front" else "_back"
        anchors["cord_in" + sfx] = tuple(fr.p(0.0, 0.0, FACE_PROUD))
        anchors["face_a" + sfx] = tuple(a)
        anchors["face_b" + sfx] = tuple(b)

    # LOD1 / LOD2 (hand-built; only when kitlib can export them) -------------
    if lods_enabled():
        mb = MB()
        flange(mb, up_frame(0, 0, 0), W, D, flange_r, flange_t, 0.0, 0, per_corner=3)
        back_ring, front_ring = tomb_body(mb, hw, hd, H, zb, rt, re, arc_segs=4, edge_segs=1)
        rings = {"front": front_ring, "back": back_ring}
        for side in ("front", "back"):
            fr = frames[side]
            if side in sides:
                holes = [hole_stack(mb, fr, fn, [(0.0, 0.0), (0.0, -2.0)], AL)[0]
                         for fn in duplex_openings(sideways=True, pc=24)]
                mb.fill(rings[side], holes, AL, fr.n)
            else:
                f = mb.face(rings[side], AL)
                mb.orient([f], (0.0, 0.0, (zb + H) / 2 * MM))
        for side in sides:
            fr = frames[side]
            duplex(mb, fr, 0.0, "lod1", sideways=True)
            backing(mb, fr, 2 * span_u + 3.0, 2 * span_v + 3.0, 3.0, -BACK_PLANE, per_corner=1)
            mb.cap(mb.ring(fr, circle(3.3, 12), 0.4), AL)          # plain screw head
        mb.to_part(kit, "lod1", "1")
        mb = MB()
        flange(mb, up_frame(0, 0, 0), W, D, flange_r, flange_t, 0.0, 0, per_corner=1)
        back_ring, front_ring = tomb_body(mb, hw, hd, H, zb, rt, 0.0, arc_segs=1, edge_segs=0)
        for ring in (back_ring, front_ring):
            f = mb.face(ring, AL)
            mb.orient([f], (0.0, 0.0, (zb + H) / 2 * MM))
        for side in sides:
            duplex_lod2(mb, frames[side], 0.0, sideways=True)
        mb.to_part(kit, "lod2", "2")

    finalize(kit, getattr(module, "SMOOTH_ANGLE", 35.0), wear)
    for name, pos in anchors.items():
        kit.anchor(name, pos)
    return anchors


def assert_tombstone(kit, W, D, H, flange_t, lo, hi, tol=1.0):
    """Self-check (a)/(b): envelope within tol mm, origin at the floor
    contact, flange thickness."""
    span = (hi[0] - lo[0], hi[1] - lo[1], hi[2] - lo[2])
    assert abs(span[0] - W) <= tol and abs(span[1] - D) <= tol and abs(span[2] - H) <= tol, \
        "%s envelope %.2f x %.2f x %.2f vs %s x %s x %s" % (kit.name, span[0], span[2], span[1], W, H, D)
    assert abs(lo[2]) < 1e-3, "%s: lowest point %.4f mm, origin must be the floor contact" % (kit.name, lo[2])
    assert abs(lo[0] + hi[0]) < 0.05 and abs(lo[1] + hi[1]) < 0.05, "%s: not centred on the origin" % kit.name
    fl = [o for o in kit.parts if o.name.startswith("flange") and "0" in str(o.get("fr_lods", "0"))][0]
    flo, fhi = bounds_mm([fl])
    assert abs(fhi[2] - flange_t) < 1e-3 and abs(flo[2]) < 1e-3, "%s: flange %.3f mm" % (kit.name, fhi[2] - flo[2])
    print("[o3] %s envelope W %.2f x H %.2f x D %.2f mm (spec %s x %s x %s); flange %.2f mm; z min %.4f" % (
        kit.name, span[0], span[2], span[1], W, H, D, fhi[2] - flo[2], lo[2]))
