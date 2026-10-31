"""Brass halogen torchiere floor lamp, c. 1985-93 (W2, 2026-10-21: period fix).

Red, 2026-10-21: "the torchiere to brass with a glass/acrylic shade".
Spec: Documentation/research/office_and_film/w2_props/10_spec.md sec. 3;
fix rounds 1, 2 and 3 (2026-10-22): 20_build.md sec. 12, 14 and 16.

Body after the Sears 1993 Home Catalog p.319 item N (a terraced bowl with a
gold rim on a polished brass cup, a plain brass pole and a terraced brass
base, 66 1/2 in high); kept a 300 W halogen uplight (era lock row 76), so the
bowl is frosted (satin-etched) glass and an R7s tube on a brass holder sits
inside it. The lamp is OFF (nothing emissive): every placement is unplugged.

Measured on the full-res scan (0.98 mm/px, +-3 %):
* 1.689 m to the glass lip (Sears' 66 1/2 in); bowl dia. 0.39, 0.11 deep,
  foot dia. 0.12; a 4.5 mm brass band proud of the glass by 2 mm, with 4.5 mm
  of the frosted glass lip showing above it (fix 2: the glass edge reads as
  glass, not as the rim of a spun metal dish); 4 mm glass wall.
* Terraces (fix 2): 6 sloped bands (45 deg) stepping out by 5 ledges of
  8 mm radial, so the notches show in the outline at standing distance
  (>= 3 px at 2.4 m, vFOV 60), as on the scan (five or more notches).
* Fake light through the glass (fix 2, no shader change): the outside of
  the bowl carries custom split normals tilted up (BOWL_NORMAL_ELEV above
  the outward horizontal), so its underside takes the ceiling light the way
  the light coming through frosted glass would, instead of facing the floor
  and going dark. Each ledge's normal sits BOWL_LEDGE_DIP lower, so every
  terrace reads as a faint ring (never a dark one). A dead room stays dark:
  nothing is emissive. Line-up in the game's Level 0 room (20_build.md
  sec. 14.1): at 45 deg the underside is L* 65 at 0.3 m (wall 22), 52 at
  0.8 m (wall 36) and 60 at standing eye (wall 42); 35 deg sat at the wall's
  level from standing eye, 55 deg began to glow. (Copying the inside face's normal, as round 2's
  verifier proposed, lit the underside like an upward white surface, L* 83
  against walls at 42, a lamp that looks switched on; blends between that
  and the geometric normal jump from L* 13 to 80 between 45 and 55 %.)
* Rim (fix 3): the brass band, the glass lip and the top band (band 6) have
  RIM_SIDES = 96 sides, the inner rim INNER_RIM_SIDES = 64; the lower bands
  stay at 48 and the lower inside at 32. Rings of different side counts are
  joined by a stitch row (each face of the coarse ring meets two of the fine
  one), so there are no T-junctions and the custom glass normals stay
  continuous: the ledge under band 6 stitches 48 -> 96, the inside's sloped
  wall 32 -> 64. Verify round 3 saw a corner every 7.5 deg on the rim at
  0.3 m; at 96 sides the sagitta is 0.10 mm (0.25 px at 0.3 m).
* No dark crescent under the lip (fix 3): the inside's short vertical wall
  under the lip top carries custom normals equal to the sloped inner wall's
  (INNER_RIM_ELEV above the inward horizontal), so from above the inside
  reads as one piece of glass, not as a dish set into a ring.
* LOD2 is hand-built (fix 3, kitlib P-1b, as on the chairs): a decimated 9 %
  copy turned the bowl blotchy and the base octagonal, a visible pop at the
  20 m switch. The proxy keeps the silhouette whole: a smooth bowl cone with
  the glass lip, the brass band, the cup, the pole and the base's rim and
  slope, LOD2_SIDES around, with the same tilted glass normals. LOD1 is still
  LOD0 collapse-decimated to LODS[0].
* Socket cup: polished brass hemisphere dia. 0.10 x 0.05 with a rolled lip
  and a neck collar into the pole; the bowl foot rests on the lip. A knurled
  brass dimmer knob on the cup's right side (+X, the viewer's right from the
  front), where Sears N has its switch, standing out on a short shank with a
  11 mm knurled cylinder, a chamfered edge and a domed face (fix 2: it shows
  in profile from the front instead of as a disc).
* Pole: plain polished brass tube dia. 0.024, no couplings.
* Base: dia. 0.245 x ~0.056: a 20 mm vertical rim with a rounded top edge and
  a flat ring, then 5 concentric rounded beads (each a short riser rolling
  over a round crest into a near-flat tread, with a crisp groove at its foot)
  up to a dia. 0.045 boss; black felt disc underneath.
* Cord from the back of the base to a two-pin plug, inside the bowl footprint
  (the lamp packs into a pile).

Front faces -Y. Origin = floor under the base centre.
Film: Still B, the tallest thin spike on the pile (Upright / EdgeLean), now
brass with a white bowl.

Slots (3):
* Prop_BrassPolished (base, pole, cup, rim band, knob, holder, tube end
  holders): a torchiere-only polished, lacquered brass. The shared Prop_Brass
  (sRGB 0.69/0.54/0.29, smoothness 0.6, 38 kits) reads dark bronze on a
  vertical pole in the game's rooms; changing it is Red's call.
* Prop_FrostedGlass (frosted bowl both faces, halogen tube): opaque satin
  white without a normal map. Prop_Glass is clear.
* Prop_PlasticBlack (felt, cord, plug).
Both new slots ship as .mat files with new GUIDs; their FrontRoomsRenderSetup
SurfaceDefs are a separate diff (w2_props/20_build.md sec. 2).

Budget (spec sec. 3.8; LOD0 raised from 4,500 to 5,300 by the game-visual
line on 2026-10-22 under the highest-spec rule, for the fix-3 rim): LOD0
<= 5,300 (rim 96 sides, inner rim 64, lower bowl 48, base 40, cup 24, pole 20,
knob 24 points), LOD1 <= 1,600, LOD2 <= 400 (LODS lowered to 0.30 / 0.075);
lodDistances [8, 20, 54]. One collider box
(bowl footprint x full height) for module placements; in furniture piles the
lamp stays walk-through, as before W2 (tag "pile_walkthrough", read by
FrontRoomsFurniturePile).
The rim band shares one part with the cup, and the bowl's outside, bottom
ring and inside are one part, so kitlib's thin-part protection (< 12 mm) does
not keep their rings in LOD1/2. Open surfaces are oriented explicitly
(recalc_face_normals is unreliable on open lathes). The glass normals are set
after kitlib's LODs are made, on every level.
"""

import math

import bmesh
from mathutils import Vector

try:
    import kitlib
    # Blender-side preview colours only; Unity renders Resources/Surfaces/<slot>.mat.
    kitlib.register_slot("Prop_BrassPolished", (0.86, 0.55, 0.20), 0.15, 1.0)
    kitlib.register_slot("Prop_FrostedGlass", (0.78, 0.75, 0.68), 0.32, 0.0)
except ImportError:      # imported outside build_asset.py
    pass

NAME = "Kit_Torchiere"
LODS = (0.30, 0.075)     # LOD1 <= 1,600, LOD2 <= 400 from a 5.3k LOD0 (fix 3; fix 2: 0.35 / 0.088 from 4.5k)
# Creases sharper than 50 deg stay sharp (rim band corners, the groove at the
# foot of each base bead (72 deg), the collar, the bowl's step corners); the
# bead crests (35-37 deg), the rounded rim and boss edges (<= 49 deg), the
# knob chamfer (45 deg) and the cup (21 deg steps) shade round.
SMOOTH_ANGLE = 50.0

BRASS = "Prop_BrassPolished"
GLASS = "Prop_FrostedGlass"
BLACK = "Prop_PlasticBlack"

TOP = 1.689            # glass lip top (Sears 66 1/2 in)
BOWL_R = 0.193         # glass outer radius at the lip
BOWL_FOOT_R = 0.060
BOWL_Z0 = 1.579        # bowl foot (rests on the cup lip)
WALL = 0.004
BAND_Z0 = 1.6800       # brass band, proud of the glass by 2 mm ...
BAND_Z1 = 1.6845       # ... with 4.5 mm of glass lip above it
BANDS = 6              # sloped bands; BANDS - 1 ledges between them
LEDGE = 0.008          # ledge radial run (verifier: 6-10 mm; 3.1 px at 2.4 m, vFOV 60)
LEDGE_RISE = LEDGE * math.tan(math.radians(12))   # sloped 12 deg up and out
OUTER_SIDES = 48       # lower bowl outside (bands 1-5, ledges 1-4), bottom ring
RIM_SIDES = 96         # fix 3: band 6, the glass lip and the brass band (0.10 mm sagitta at r 0.195)
INNER_SIDES = 32       # the inside's floor and lower wall
INNER_RIM_SIDES = 64   # fix 3: the inside's upper wall and the inner rim under the lip
LOD2_SIDES = 16        # fix 3: the hand-built LOD2 (bowl, band, base); cup 10, pole 6
BOWL_NORMAL_ELEV = 45.0   # outside of the bowl: normal tilted this far above the outward horizontal (deg)
BOWL_LEDGE_DIP = 5.0      # ... and this much lower on each ledge (a faint ring per terrace)
POLE_R = 0.012
CUP_R = 0.050
CUP_SIDES = 24
BASE_R = 0.1225
BASE_SIDES = 40        # 0.38 mm sagitta at r 0.1225: <= 1 px at 0.3 m (2.3 px/mm)
KNOB_Z = 1.555


# --------------------------------------------------------------------- helpers
def _ring_lathe(bm, profile, verts):
    """Revolve (r, z) points into ``bm`` (one open surface; a point on the
    axis is welded later by remove_doubles). ``verts`` is one side count, or
    one per profile point (fix 3): neighbouring rings either match (a row of
    quads) or differ by a factor of 2 (a stitch row: each edge of the coarse
    ring meets two edges of the fine one through a quad and a triangle, so
    no vertex sits on another face's edge)."""
    counts = list(verts) if isinstance(verts, (list, tuple)) else [verts] * len(profile)
    assert len(counts) == len(profile)
    rings = []
    for (r, z), n in zip(profile, counts):
        rings.append([bm.verts.new((r * math.cos(2 * math.pi * i / n), r * math.sin(2 * math.pi * i / n), z))
                      for i in range(n)])
    faces = []
    for a, b in zip(rings, rings[1:]):
        na, nb = len(a), len(b)
        if na == nb:
            for i in range(na):
                j = (i + 1) % na
                faces.append(bm.faces.new((a[i], a[j], b[j], b[i])))
        elif nb == 2 * na:
            for i in range(na):
                j = (i + 1) % na
                faces.append(bm.faces.new((a[i], a[j], b[(2 * i + 2) % nb], b[2 * i + 1])))
                faces.append(bm.faces.new((a[i], b[2 * i + 1], b[2 * i])))
        elif na == 2 * nb:
            for i in range(nb):
                j = (i + 1) % nb
                faces.append(bm.faces.new((a[2 * i], a[2 * i + 1], b[i])))
                faces.append(bm.faces.new((a[2 * i + 1], a[(2 * i + 2) % na], b[j], b[i])))
        else:
            raise ValueError("stitch needs equal or doubled side counts: %d -> %d" % (na, nb))
    return faces, rings


def _orient(bm, faces, direction):
    """Point the faces of one open surface along ``direction(centre)``."""
    score = 0.0
    for f in faces:
        f.normal_update()
        score += f.normal.dot(direction(f.calc_center_median())) * f.calc_area()
    if score < 0:
        bmesh.ops.reverse_faces(bm, faces=faces)


def _surfaces(kit, name, slot, surfaces):
    """One part made of several revolved open surfaces
    [(profile, verts, direction), ...], each oriented on its own; ``verts``
    is a side count or one per profile point (see _ring_lathe)."""
    bm = bmesh.new()
    for profile, verts, direction in surfaces:
        faces, _ = _ring_lathe(bm, profile, verts)
        _orient(bm, faces, direction)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-7)
    obj = kit._new_object(name, bm, slot, "metres", "xz")
    kit._place(obj, (0, 0, 0), (0, 0, 0))
    return obj


def _disc(kit, name, slot, r, z, verts, down=True):
    """A single n-gon cap facing down (or up)."""
    bm = bmesh.new()
    ring = [bm.verts.new((r * math.cos(2 * math.pi * i / verts), r * math.sin(2 * math.pi * i / verts), z))
            for i in range(verts)]
    face = bm.faces.new(list(reversed(ring)) if down else ring)
    face.normal_update()
    if (face.normal.z < 0) != down:
        face.normal_flip()
    obj = kit._new_object(name, bm, slot, "metres", "xz")
    kit._place(obj, (0, 0, 0), (0, 0, 0))
    return obj


def _knob(kit, name, slot, back_centre, ribs=12, r_out=0.0105, r_in=0.0101, side=0.011, chamfer=0.0015,
          dome=0.0009):
    """Knurled knob along +X from ``back_centre`` (its back face): ``ribs``
    shallow ribs (2 x ribs points, 0.4 mm deep so the knurl reads light), a
    ``side`` long cylindrical side, a 45 deg chamfer on the front edge and a
    low domed face (fix 2: on the side of the cup, seen in profile)."""
    cx, cy, cz = back_centre
    n = 2 * ribs

    def ring(radius, x):
        pts = []
        for i in range(n):
            a = 2 * math.pi * i / n
            rr = radius(i)
            pts.append((cx + x, cy + rr * math.cos(a), cz + rr * math.sin(a)))
        return pts

    def knurl(i):
        return r_out if i % 2 == 0 else r_in

    bm = bmesh.new()
    rings = [
        [bm.verts.new(p) for p in ring(knurl, 0.0)],                                       # back edge
        [bm.verts.new(p) for p in ring(knurl, side)],                                      # front edge of the knurl
        [bm.verts.new(p) for p in ring(lambda i: r_in - chamfer, side + chamfer * 0.85)],  # chamfer
        [bm.verts.new(p) for p in ring(lambda i: 0.55 * (r_in - chamfer), side + chamfer + dome * 0.45)],  # dome shoulder
    ]
    tip = bm.verts.new((cx + side + chamfer + dome, cy, cz))                                # dome centre
    faces = []
    for a, b in zip(rings, rings[1:]):
        for i in range(n):
            j = (i + 1) % n
            faces.append(bm.faces.new((a[i], a[j], b[j], b[i])))
    for i in range(n):
        faces.append(bm.faces.new((rings[-1][i], rings[-1][(i + 1) % n], tip)))
    faces.append(bm.faces.new(list(reversed(rings[0]))))                                   # back cap
    # Outward from the knob's axis, and forward (+X) on the face.
    axis = Vector((1.0, 0.0, 0.0))
    c0 = Vector((cx, cy, cz))
    score = 0.0
    for f in faces:
        f.normal_update()
        v = f.calc_center_median() - c0
        along = v.dot(axis)
        radial = v - axis * along
        want = radial.normalized() if radial.length > 0.004 else (axis if along > 0.002 else -axis)
        score += f.normal.dot(want) * f.calc_area()
    if score < 0:
        bmesh.ops.reverse_faces(bm, faces=faces)
    obj = kit._new_object(name, bm, slot, "metres", "xz")
    kit._place(obj, (0, 0, 0), (0, 0, 0))
    return obj


def _out_down(c):
    """Outside of a bowl / cup: outward and down."""
    return Vector((c.x, c.y, -0.35 * math.hypot(c.x, c.y)))


def _out_up(c):
    """Outside of the terraced base: outward and up."""
    return Vector((c.x, c.y, 0.30 * math.hypot(c.x, c.y)))


def _up_in(c):
    """Inside of the bowl: toward the axis and up."""
    return Vector((-c.x, -c.y, 1.0))


def _radial(c):
    return Vector((c.x, c.y, 0.0))


def _both(*flags):
    def mark(obj):
        for f in flags:
            obj[f] = True
        return obj
    return mark


LOD12_DROP = _both("fr_lod1_drop", "fr_lod2_drop")
LOD2_DROP = _both("fr_lod2_drop")


def bowl_outside():
    """Foot -> 6 bands and 5 ledges -> straight up behind the brass band ->
    the glass lip's top, bottom to top (r, z):
    [foot, A1, N1, A2, N2, ..., A6, outer lip top, inner lip top], where A_k
    is the top of band k (the inner corner of a step) and N_k the ledge's
    nose (the foot of band k + 1)."""
    run = (BOWL_R - BOWL_FOOT_R - (BANDS - 1) * LEDGE) / BANDS
    hb = (BAND_Z0 - BOWL_Z0 - (BANDS - 1) * LEDGE_RISE) / BANDS
    pts = [(BOWL_FOOT_R, BOWL_Z0)]
    r, z = BOWL_FOOT_R, BOWL_Z0
    for k in range(BANDS):
        r, z = r + run, z + hb
        pts.append((r, z))                                 # A_k: band top
        if k < BANDS - 1:
            r, z = r + LEDGE, z + LEDGE_RISE
            pts.append((r, z))                             # N_k: ledge nose
    pts.append((BOWL_R, TOP))                              # the glass goes on up behind the band
    pts.append((BOWL_R - WALL - 0.001, TOP))               # lip top (5 mm), over the inside's top edge
    return pts


def bowl_inside():
    """The plunger's dished inside (r, z): the floor, then a straight wall
    4 mm inside the line through every step's inner corner (the A_k are
    collinear), then straight up to the lip, tucked under the lip top."""
    floor_z = BOWL_Z0 + WALL
    out = bowl_outside()
    a1, a6 = out[1], out[2 * BANDS - 1]
    return [(0.0, floor_z), (BOWL_FOOT_R - WALL, floor_z), (a1[0] - WALL, a1[1]), (a6[0] - WALL, a6[1]),
            (BOWL_R - WALL, TOP - 0.0008)]


def _inside_radius():
    prof = bowl_inside()[1:]

    def at(z):
        if z <= prof[0][1]:
            return prof[0][0]
        for (ra, za), (rb, zb) in zip(prof, prof[1:]):
            if za <= z <= zb and zb > za:
                return ra + (rb - ra) * (z - za) / (zb - za)
        return prof[-1][0]
    return at


def inner_rim_elev():
    """Elevation (deg above the inward horizontal) of the sloped inner wall's
    normal, from the line through (A1 - wall) and (A6 - wall)."""
    ins = bowl_inside()
    (ra, za), (rb, zb) = ins[2], ins[3]
    return math.degrees(math.atan2(rb - ra, zb - za))


def bowl_outside_sides():
    """Side count per bowl_outside() point (fix 3): 48 up to the top of
    band 5, 96 from the nose of ledge 5 (band 6, the glass behind the band,
    the lip top), so ledge 5 is the stitch row."""
    n = len(bowl_outside())
    return [OUTER_SIDES] * (n - 4) + [RIM_SIDES] * 4


def bowl_inside_sides():
    """Side count per bowl_inside() point (fix 3): 32 for the floor and the
    lower wall, 64 from (A6 - wall) up, so the long sloped wall is the
    stitch row and the inner rim is 64-sided."""
    return [INNER_SIDES, INNER_SIDES, INNER_SIDES, INNER_RIM_SIDES, INNER_RIM_SIDES]


def glass_normals(obj, elev=None, dip=None, rim_elev=None):
    """Custom split normals on the outside of the bowl: every face of the
    glass slot that lies outside the inside face (not the lip top, not the
    halogen tube) gets, at each corner, the normal tilted ``elev`` degrees
    above the outward horizontal at that corner's azimuth (``elev - dip`` on
    the downward-facing ledges). Fix 3: the inside's short vertical wall
    under the lip top gets the sloped inner wall's normal (``rim_elev`` above
    the inward horizontal), so it no longer shades as a dark crescent. Other
    corners keep their smooth normals."""
    elev = BOWL_NORMAL_ELEV if elev is None else elev
    dip = BOWL_LEDGE_DIP if dip is None else dip
    rim_elev = inner_rim_elev() if rim_elev is None else rim_elev
    rim_z0 = bowl_inside()[3][1]
    mesh = obj.data
    glass = {i for i, m in enumerate(mesh.materials) if m is not None and m.name == GLASS}
    if not glass:
        return 0
    inside_r = _inside_radius()
    floor_z = BOWL_Z0 + WALL
    custom = [(0.0, 0.0, 0.0)] * len(mesh.loops)
    count = 0
    for poly in mesh.polygons:
        if poly.material_index not in glass:
            continue
        c = poly.center
        if poly.normal.z > 0.9 and c.z > TOP - 0.0005:
            continue                                    # lip top: its own (up) normal
        rc = math.hypot(c.x, c.y)
        if c.z >= floor_z - 0.0002 and rc < inside_r(c.z) + 0.0015:
            if c.z > rim_z0 + 0.0002 and abs(poly.normal.z) < 0.3 and rc > inside_r(c.z) - 0.002:
                # inside's vertical wall under the lip (fix 3): the sloped wall's normal
                e = math.radians(rim_elev)
                count += 1
                for li in poly.loop_indices:
                    co = mesh.vertices[mesh.loops[li].vertex_index].co
                    rv = math.hypot(co.x, co.y)
                    cx, cy = (co.x / rv, co.y / rv) if rv > 1e-6 else (0.0, 0.0)
                    custom[li] = (-math.cos(e) * cx, -math.cos(e) * cy, math.sin(e))
            continue                                    # inside face, halogen tube
        ledge = poly.normal.z < -0.9 and c.z > BOWL_Z0 + 0.0005
        e = math.radians(elev - (dip if ledge else 0.0))
        count += 1
        for li in poly.loop_indices:
            co = mesh.vertices[mesh.loops[li].vertex_index].co
            rv = math.hypot(co.x, co.y)
            cx, cy = (co.x / rv, co.y / rv) if rv > 1e-6 else (0.0, 0.0)
            custom[li] = (math.cos(e) * cx, math.cos(e) * cy, math.sin(e))
    mesh.normals_split_custom_set(custom)
    return count


# ----------------------------------------------------------------------- build
def build(kit):
    # ---------------------------------------------------------------- base
    base_top = base_profile()[-1][1]
    _surfaces(kit, "base", BRASS, [(base_profile(), BASE_SIDES, _out_up)])
    # Felt under the base (seen only when the lamp leans out of a pile).
    LOD12_DROP(_disc(kit, "felt", BLACK, BASE_R - 0.0004, 0.0006, BASE_SIDES, down=True))

    # ---------------------------------------------------------------- pole
    pole_top = BOWL_Z0 - CUP_R - 0.016
    _surfaces(kit, "pole", BRASS, [([(POLE_R, base_top - 0.004), (POLE_R, pole_top + 0.004)], 20, _radial)])

    # ------------------------------------------- socket cup, collar, rim band
    # Neck collar, hemisphere (4 steps of ~21 deg), rolled lip. One part with
    # the bowl's brass rim band (see the module docstring).
    zc = BOWL_Z0 - 0.0015                # sphere centre, just under the lip plane
    cup = [(POLE_R + 0.0004, pole_top), (0.0165, pole_top + 0.002), (0.0165, pole_top + 0.011)]
    for a in (-64.0, -42.0, -21.0, 0.0):
        t = math.radians(a)
        cup.append((CUP_R * math.cos(t), zc + CUP_R * math.sin(t)))
    cup += [(CUP_R + 0.0022, zc + 0.0011), (CUP_R + 0.0008, BOWL_Z0 + 0.0012)]   # rolled lip, seated in the foot
    band = [(BOWL_R - 0.0003, BAND_Z0 - 0.0004), (BOWL_R + 0.0020, BAND_Z0 + 0.0006),
            (BOWL_R + 0.0020, BAND_Z1 - 0.0006), (BOWL_R - 0.0003, BAND_Z1 + 0.0002)]
    _surfaces(kit, "cup and rim band", BRASS, [(cup, CUP_SIDES, _out_down), (band, RIM_SIDES, _radial)])

    # ---------------------------------------------------------- dimmer knob
    # On the cup's right side (+X), on a short shank: knurled (12 shallow ribs),
    # chamfered and domed, so it stands out in profile from the front.
    kz = KNOB_Z
    cup_r_at = math.sqrt(max(CUP_R ** 2 - (zc - kz) ** 2, 0.0))
    LOD12_DROP(kit.cylinder(0.0042, 0.006, (cup_r_at + 0.001, 0, kz), BRASS, verts=8, rot=(0, 90, 0), bevel=0.0,
                            name="knob shank"))
    LOD12_DROP(_knob(kit, "dimmer knob", BRASS, (cup_r_at + 0.004, 0.0, kz)))

    # ---------------------------------------------------------------- bowl
    # Pressed frosted glass: the mould's terraced outside and lip top
    # (48 sides below, 96 from ledge 5 up: band 6, the lip), the bottom ring
    # seated on the cup, and the plunger's smooth dished inside (32 sides
    # below, 64 at the inner rim), clear of the outside by >= 2.8 mm
    # everywhere (4 mm at the foot and the lip).
    floor_z = BOWL_Z0 + WALL
    _surfaces(kit, "bowl", GLASS, [
        (bowl_outside(), bowl_outside_sides(), _out_down),
        ([(0.044, BOWL_Z0), (BOWL_FOOT_R, BOWL_Z0)], OUTER_SIDES, lambda c: Vector((0, 0, -1))),
        (bowl_inside(), bowl_inside_sides(), _up_in),
    ])

    # Lamp holder on the bowl floor: a brass strap and two end holders with the
    # R7s tube between them (seen from above / when the lamp leans).
    # The strap bridges 6 mm over the floor on a centre stud; its ends and the
    # holders rest on the dished inside, clear of the outside wall.
    hz = floor_z + 0.006
    LOD2_DROP(kit.cylinder(0.006, 0.0065, (0, 0, floor_z + 0.003), BRASS, verts=8, bevel=0.0, name="holder stud"))
    LOD2_DROP(kit.box((0.128, 0.016, 0.003), (0, 0, hz + 0.0015), BRASS, bevel=0.0, name="holder strap"))
    for s in (-1, 1):
        LOD2_DROP(kit.box((0.010, 0.018, 0.022), (s * 0.0615, 0, hz + 0.011), BRASS, bevel=0.0,
                          name="tube end holder"))
    LOD2_DROP(kit.cylinder(0.005, 0.118, (0, 0, hz + 0.014), GLASS, verts=8, rot=(0, 90, 0), bevel=0.0,
                           name="halogen tube"))

    # ---------------------------------------------------------------- cord
    LOD2_DROP(kit.tube([(0, BASE_R - 0.004, 0.009), (0.004, BASE_R + 0.014, 0.0032), (0.060, 0.162, 0.0032),
                        (0.128, 0.136, 0.0032), (0.160, 0.084, 0.0032)], 0.0032, BLACK, verts=6, name="power cord"))
    LOD12_DROP(kit.box((0.026, 0.034, 0.016), (0.162, 0.060, 0.008), BLACK, bevel=0.002, segments=1, rot=(0, 0, 8),
                       name="plug body"))

    # ------------------------------------------------------------ metadata
    kit.anchor("light", (0, 0, 1.62))
    kit.anchor("dimmer", (cup_r_at + 0.004 + 0.0135, 0, kz))
    # One box, bowl footprint x full height: the player no longer walks
    # through the lamp in module placements (L0_WaitingRoom_4x3). In furniture
    # piles FrontRoomsFurniturePile spawns it without colliders, as before W2
    # ("pile_walkthrough"): a 0.39 m box round a 24 mm pole, tilted in EdgeLean,
    # would be an invisible wall.
    kit.collider((0, 0, TOP / 2), (2 * (BOWL_R + 0.002), 2 * (BOWL_R + 0.002), TOP))
    kit.meta["lodDistances"] = [8.0, 20.0, 54.0]
    kit.tag("pile", "lamp", "domestic", "pile_walkthrough")
    kit.pile("Tall", mass=0, palette="domestic70s", topper=True, states=["Upright", "EdgeLean"])

    # ------------------------------------------------- LOD2 proxy (fix 3)
    build_lod2(kit, base_top, pole_top, zc)

    # The glass normals go on after kitlib has joined and decimated every level.
    # With a hand-built LOD2 kitlib no longer decimates, so LOD1 is made here.
    make_lods = kit.make_lods

    def make_lods_then_glass(ratios=None):
        if 1 not in kit.lods:
            kit.lods[1] = kit._decimated(LODS[0], 1)
        make_lods(ratios)
        for obj in [kit.object] + [o for _lv, o in sorted(kit.lods.items())]:
            n = glass_normals(obj)
            print("[torchiere] glass normals on %s: %d faces (inner rim elev %.1f)" % (obj.name, n, inner_rim_elev()))
    kit.make_lods = make_lods_then_glass


def _lod2(obj):
    """A part of the hand-built LOD2 only (kitlib P-1b)."""
    obj["fr_lods"] = "2"
    return obj


def build_lod2(kit, base_top, pole_top, zc):
    """Whole low-poly parts, the LOD0 outline at LOD2_SIDES (about 370 tris):
    the bowl as one smooth cone (the 8 mm notches are under half a pixel at
    20 m) with the lip top and a fan inside, the band and the cup in one part,
    the pole, the base's rim, flat ring, bead slope and boss."""
    n = LOD2_SIDES
    floor_z = BOWL_Z0 + WALL
    _lod2(_surfaces(kit, "bowl lod2", GLASS, [
        ([(BOWL_FOOT_R, BOWL_Z0), (BOWL_R, BAND_Z0), (BOWL_R, TOP), (BOWL_R - WALL - 0.001, TOP)], n, _out_down),
        ([(0.0, floor_z), (BOWL_R - WALL, TOP - 0.0008)], n, _up_in),
    ]))
    cup = [(POLE_R + 0.0004, pole_top), (0.0165, pole_top + 0.011)]
    for a in (-55.0, -25.0, 0.0):
        t = math.radians(a)
        cup.append((CUP_R * math.cos(t), zc + CUP_R * math.sin(t)))
    cup.append((CUP_R + 0.0008, BOWL_Z0 + 0.0012))
    band = [(BOWL_R + 0.0020, BAND_Z0), (BOWL_R + 0.0020, BAND_Z1)]
    _lod2(_surfaces(kit, "cup and rim band lod2", BRASS, [(cup, 10, _out_down), (band, n, _radial)]))
    _lod2(_surfaces(kit, "pole lod2", BRASS, [([(POLE_R, base_top - 0.004), (POLE_R, pole_top + 0.004)], 6, _radial)]))
    prof = base_profile()
    _lod2(_surfaces(kit, "base lod2", BRASS, [([prof[0], (BASE_R, 0.022), (BASE_R - 0.014, 0.022), prof[-3], prof[-1]],
                                               n, _out_up)]))


def base_profile():
    """Outside of the base, rim foot -> boss -> pole (r, z)."""
    z_rim = 0.022
    boss_r, boss_top = 0.0225, 0.056
    r0 = BASE_R - 0.014                       # inner edge of the flat rim ring
    beads = 5
    pitch = (r0 - boss_r) / beads             # 17.2 mm per bead
    lift = 0.0056                             # net rise per bead
    prof = [(BASE_R, 0.0012),                 # foot (the felt disc closes the underside)
            (BASE_R, z_rim - 0.0025),         # 20 mm vertical rim
            (BASE_R - 0.0007, z_rim - 0.0007),
            (BASE_R - 0.0025, z_rim),         # rounded top edge
            (r0, z_rim)]                      # flat rim ring; groove at the first bead's foot
    r, z = r0, z_rim
    for k in range(beads):
        # riser (70 deg) rolling over a round crest (35 deg) into a near-flat
        # tread that dips 0.4 mm into the next groove (a crisp > 40 deg crease).
        prof.append((r - 0.0013, z + 0.0036))
        prof.append((r - 0.0047, z + 0.0060))
        r, z = r - pitch, z + lift
        prof.append((r, z))
    # boss shoulder, rounded, into the pole
    prof += [(boss_r - 0.0008, boss_top - 0.0025), (boss_r - 0.0030, boss_top), (POLE_R + 0.0004, boss_top)]
    return prof
