"""EXIT sign family: shared Blender construction (helper module, no NAME).

Spec: Documentation/research/exit_sign/10_spec.md §4 (model), §5 (letters),
§6 (textures), §7 (materials). Numbers: exit_sign_geom.py (no bpy; the
texture generator Tools/lookdev/gen_exit_sign.py reads the same file).

A 1990 US internally lit EXIT sign: a drawn steel pan, a pressed face frame
whose skirt overlaps the pan (the 0.6 x 0.4 seam), a 0.9 mm stencil plate
with the letters cut THROUGH it (stamped reflex corners R 0.5, 0.2 front
chamfer), a white 6 mm spacer, and a red acrylic diffuser 6 mm behind the
plate. Two 20 W lamps inside light the diffuser; their hot-spot field lives
in the diffuser textures (_E, _F), not in geometry.

Frames
* Assets are authored in Unity part metres and converted with
  interact_key_common.U(): Blender (x, y, z) = (-X, -Z, Y); kitlib.export()
  writes them back unchanged, so anchors below are exact Unity numbers.
* A FACE is placed by (origin, normal n, right r) in Unity space; face-local
  (a, b, c) mm = (to the reader's right, up, toward the reader) maps to
  origin + r*a + up*b + n*c. c = 0 is the frame's front flat.
* Face UVs are written here ("keep"), in metres over the face, so the
  materials are position-independent and a double face reads correctly from
  both sides: ExitSign_Face (0.346 x 0.212, frame + plate), ExitSign_Diffuser
  (0.320 x 0.186, the opening). The back diffuser of a double face is
  U-mirrored, so lamp A is the same physical lamp from both sides.

Slots (4, the kit rule): ExitSign_Housing (pan, canopy, spacer; tiling metre
UVs), ExitSign_Face, ExitSign_Diffuser (VARIANT -> ExitSign_Diffuser_Dead),
Prop_Chrome (screws, stems, collars, nuts).

Never edit kitlib.py: everything here goes through Kit's public helpers.
"""

import math

import bmesh
import bpy
from mathutils import Vector

import kitlib
import interact_key_common as kc
import exit_sign_geom as g

HOUSING = "ExitSign_Housing"
FACE = "ExitSign_Face"
DIFFUSER = "ExitSign_Diffuser"
DIFFUSER_DEAD = "ExitSign_Diffuser_Dead"
CHROME = "Prop_Chrome"
MM = 0.001

FACE_TILE_M = (g.HOUSING_W * MM, g.HOUSING_H * MM)
DIFF_TILE_M = (g.OPEN_W * MM, g.OPEN_H * MM)
LOD_DISTANCES = (5.0, 0.0, 0.0)      # 2-LOD FBX: LOD0 -> LOD1 at 5 m, LOD1 never culls (spec §4.3; never null)


def register_slots():
    """Preview colours only; the Unity look comes from Resources/Surfaces."""
    kitlib.register_slot(HOUSING, (0.78, 0.76, 0.70), 0.5, 0.0)
    kitlib.register_slot(FACE, (0.79, 0.77, 0.69), 0.55, 0.0)
    kitlib.register_slot(DIFFUSER, (0.60, 0.04, 0.03), 0.3, 0.0)
    kitlib.register_slot(DIFFUSER_DEAD, (0.15, 0.022, 0.018), 0.3, 0.0)


# ------------------------------------------------------------------ helpers
def V(p):
    return Vector(p)


class Face:
    """One sign face: origin (Unity m), outward normal n, reader's right r."""

    def __init__(self, origin, n, r, mirror_lamps=False, screw_axis="+Z"):
        self.o = V(origin)
        self.n = V(n).normalized()
        self.r = V(r).normalized()
        self.up = Vector((0.0, 1.0, 0.0))
        self.mirror = mirror_lamps
        self.screw_axis = screw_axis

    def unity(self, a, b, c):
        return self.o + self.r * (a * MM) + self.up * (b * MM) + self.n * (c * MM)

    def blender(self, a, b, c):
        p = self.unity(a, b, c)
        return kc.U(p.x, p.y, p.z)

    def want(self):
        """The face normal in Blender space (for orientation checks)."""
        return V(kc.U(self.n.x, self.n.y, self.n.z)).normalized()

    def face_uv(self, a, b):
        return ((a + g.HOUSING_W / 2) * MM, (b + g.HOUSING_H / 2) * MM)

    def diff_uv(self, a, b):
        u = (g.OPEN_W / 2 - a) if self.mirror else (a + g.OPEN_W / 2)
        return (u * MM, (b + g.OPEN_H / 2) * MM)


class MB:
    """bmesh builder that remembers a UV per vertex (for "keep" parts)."""

    def __init__(self):
        self.bm = bmesh.new()
        self.uv = {}

    def v(self, p, uv=None):
        vert = self.bm.verts.new(p)
        if uv is not None:
            self.uv[vert] = uv
        return vert

    def ring(self, pts, uvs=None):
        return [self.v(p, None if uvs is None else uvs[i]) for i, p in enumerate(pts)]

    def bridge(self, a, b):
        n = len(a)
        out = []
        for i in range(n):
            j = (i + 1) % n
            out.append(self.bm.faces.new((a[i], a[j], b[j], b[i])))
        return out

    def fill(self, loops):
        edges = []
        for lp in loops:
            for i in range(len(lp)):
                e = self.bm.edges.get((lp[i], lp[(i + 1) % len(lp)]))
                if e is None:
                    e = self.bm.edges.new((lp[i], lp[(i + 1) % len(lp)]))
                edges.append(e)
        res = bmesh.ops.triangle_fill(self.bm, use_beauty=True, use_dissolve=False, edges=edges)
        return [f for f in res["geom"] if isinstance(f, bmesh.types.BMFace)]

    @staticmethod
    def orient(faces, want, ref=None):
        """Flip all faces if the reference face (default: the first) points away from want."""
        if not faces:
            return
        f = ref or faces[0]
        f.normal_update()
        if f.normal.dot(want) < 0:
            for x in faces:
                x.normal_flip()

    @staticmethod
    def orient_each(faces, want):
        for f in faces:
            f.normal_update()
            if f.normal.dot(want) < 0:
                f.normal_flip()

    def finish(self, kit, slot, name, keep_uv):
        bm = self.bm
        if keep_uv:
            layer = bm.loops.layers.uv.verify()
            for f in bm.faces:
                for loop in f.loops:
                    loop[layer].uv = self.uv.get(loop.vert, (0.0, 0.0))
        for f in bm.faces:
            f.smooth = True
        return kit._new_object(name, bm, slot, "keep" if keep_uv else "metres", "xz")


def rr(w, h, r, seg, cx=0.0, cy=0.0):
    return g.rounded_rect(w, h, r, seg, cx, cy)


def wear(obj):
    kc.paint_wear(obj)
    return obj


# ------------------------------------------------------------------ the face assembly
def frame_profile():
    """(inset from the outer edge, c) for the pressed face frame, skirt -> lip."""
    P = [(g.SEAM_RETURN, -g.FRAME_SKIRT), (0.0, -g.FRAME_SKIRT + 0.3), (0.0, -g.ROLL_R)]
    for th in (30.0, 60.0):
        t = math.radians(th)
        P.append((g.ROLL_R - g.ROLL_R * math.cos(t), -g.ROLL_R + g.ROLL_R * math.sin(t)))
    P.append((g.ROLL_R, 0.0))
    P.append((g.BEAD_IN0, 0.0))
    for ph in (30.0, 60.0, 90.0, 120.0, 150.0):
        t = math.radians(ph)
        P.append((g.BEAD_IN0 + (g.BEAD_IN1 - g.BEAD_IN0) * (1 - math.cos(t)) / 2, g.BEAD_H * math.sin(t)))
    P.append((g.BEAD_IN1, 0.0))
    P.append((g.BORDER - g.LIP_R, 0.0))
    t = math.radians(45.0)
    P.append((g.BORDER - g.LIP_R + g.LIP_R * math.sin(t), -g.LIP_R + g.LIP_R * math.cos(t)))
    P.append((g.BORDER, -g.LIP_R))
    P.append((g.BORDER, -g.LIP_DEPTH))
    return P


def frame_uv_insets(prof):
    """UV inset per profile ring. The front (roll top, flat, bead, lip top) keeps
    its geometric inset, so the texture's bead and border lines sit on the
    geometry. The skirt + outer roll and the inner lip return are UNFOLDED by
    arc length (outward past the face edge, inward into the opening): rings at
    one inset but different depths no longer share a UV, so their tangent frame
    is defined and nothing stretches (build round 2; Unity probe: 152 Face
    vertices had a tangent within 60 degrees of the normal)."""
    out = [d for d, c in prof]
    i0 = prof.index((g.ROLL_R, 0.0))
    i1 = prof.index((g.BORDER - g.LIP_R, 0.0))
    acc = 0.0
    for k in range(i0 - 1, -1, -1):
        acc += math.hypot(prof[k][0] - prof[k + 1][0], prof[k][1] - prof[k + 1][1])
        out[k] = g.ROLL_R - acc
    acc = 0.0
    for k in range(i1 + 1, len(prof)):
        acc += math.hypot(prof[k][0] - prof[k - 1][0], prof[k][1] - prof[k - 1][1])
        out[k] = g.BORDER - g.LIP_R + acc
    return out


def build_frame(kit, face, seg=8, name="face frame"):
    mb = MB()
    rings = []
    ref_band = None
    prof = frame_profile()
    uvd = frame_uv_insets(prof)
    for k, (d, c) in enumerate(prof):
        r = max(g.HOUSING_R - d, g.OPEN_R)
        poly = rr(g.HOUSING_W - 2 * d, g.HOUSING_H - 2 * d, r, seg)
        e = uvd[k]
        puv = rr(g.HOUSING_W - 2 * e, g.HOUSING_H - 2 * e, max(g.HOUSING_R - e, g.OPEN_R), seg)
        assert len(puv) == len(poly)
        rings.append(mb.ring([face.blender(a, b, c) for a, b in poly], [face.face_uv(a, b) for a, b in puv]))
    faces = []
    for k in range(len(rings) - 1):
        band = mb.bridge(rings[k], rings[k + 1])
        faces += band
        d0, d1 = prof[k][0], prof[k + 1][0]
        if ref_band is None and abs(prof[k][1]) < 1e-9 and abs(prof[k + 1][1]) < 1e-9 and d1 > d0:
            ref_band = band
    MB.orient(faces, face.want(), ref_band[0])
    return wear(mb.finish(kit, FACE, name, True))


def build_plate(kit, face, chevrons="closed", name="stencil plate"):
    """Stencil plate: front face with the letter holes, 0.2 chamfer, cut walls
    0.9 deep. No back face and no outer edge (both hidden: the frame covers
    the edge, and every ray through a hole ends on the diffuser or spacer)."""
    mb = MB()
    cp = -g.LIP_DEPTH
    cb = cp - g.SHEET
    ch = g.HOLE_CHAMFER
    sup = 0.15
    outline = rr(g.OPEN_W + 2 * g.PLATE_MARGIN, g.OPEN_H + 2 * g.PLATE_MARGIN, g.PLATE_R + g.PLATE_MARGIN, 3)
    out_loop = mb.ring([face.blender(a, b, cp) for a, b in outline], [face.face_uv(a, b) for a, b in outline])
    loops = [out_loop]
    wall_faces = []
    for hole in g.hole_polys(chevrons):
        poly = [g.open_to_reader(p) for p in hole]
        rings_2d = [(kc.offset_poly(poly, -(ch + sup)), cp), (kc.offset_poly(poly, -ch), cp), (poly, cp - ch), (poly, cb)]
        # UVs: the chamfer and the cut wall are unfolded INTO the hole by arc length
        # (the texture's letter interior is the painted cut wall), so the wall rings
        # do not share one UV and the wall has a real tangent frame (build round 2).
        e_ch = -ch + ch * math.sqrt(2.0)
        uv_off = [-(ch + sup), -ch, e_ch, e_ch + (g.SHEET - ch)]
        rings = [mb.ring([face.blender(a, b, c) for a, b in pts], [face.face_uv(a, b) for a, b in kc.offset_poly(poly, uo)])
                 for (pts, c), uo in zip(rings_2d, uv_off)]
        for k in range(len(rings) - 1):
            wall_faces.append(mb.bridge(rings[k], rings[k + 1]))
        loops.append(rings[0])
    front = mb.fill(loops)
    MB.orient_each(front, face.want())
    # Bands: support ring (flat, faces out), chamfer, wall (faces into the hole).
    for bands in wall_faces:
        MB.orient(bands, face.want(), bands[0])       # band 0 lies on the front plane
    # The wall bands of a hole share the support band's winding, so fix them per
    # band against the hole centre instead: a wall faces the hole's axis.
    obj = mb.finish(kit, FACE, name, True)
    _fix_hole_walls(obj, face, chevrons)
    obj = wear(obj)
    lod1 = legend_lod1_holes()
    if lod1 is not None:
        # A legend with a LOD1 outline (the Latin and Cyrillic EXIT, 2026-11-04): this hero plate is LOD0 only, and
        # the LOD1 gets a plate of its own (build_plate_lod1), joined after the decimation (legend_lod1). The hero
        # plate is protected from the collapse (thin part), so a long word's plate (AUSGANG: 2030 tris a face) left
        # the decimator nothing but the housing and frame to cut: LOD1 kept 14 housing triangles of 924.
        tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
        want = plate_tris(g.hole_polys(chevrons), 4)
        assert tris == want, "%s: hero plate %d tris, plate_tris() says %d" % (kit.name, tris, want)
        kit.lod1_drop(obj)
        kit.legend_hero_plate_tris = getattr(kit, "legend_hero_plate_tris", 0) + tris
        kit.legend_lod1_plates = getattr(kit, "legend_lod1_plates", []) + [build_plate_lod1(kit, face, lod1, name + " LOD1")]
    return obj


# ------------------------------------------------------------------ the LOD1 of a long legend
def legend_lod1_holes():
    """The legend's LOD1 outline (OPENING mm, CCW loops), or None (English, 出口)."""
    if g.LEGEND is None or not g.LEGEND.get("lod1"):
        return None
    return [g.ccw([(float(x), float(y)) for x, y in h]) for h in g.LEGEND["lod1"]["holes"]]


def english_hole_polys():
    """EXIT's own through-holes (E X I T, closed chevrons), as exit_sign_geom.hole_polys gives them with no legend."""
    p = g.letter_polys_open()
    return [g.fillet_reflex(g.ccw(poly), g.HOLE_FILLET) for k in ("E", "X", "I", "T") for poly in p[k]]


def plate_tris(holes, rings):
    """Triangles of a stencil plate cut with these holes: the front fill of the outline with h hole loops (V + 2h - 2
    for V loop vertices; triangle_fill adds none) and (rings - 1) wall bands of 2 triangles per hole vertex
    (build_plate: 4 rings; build_plate_lod1: 2)."""
    outline = rr(g.OPEN_W + 2 * g.PLATE_MARGIN, g.OPEN_H + 2 * g.PLATE_MARGIN, g.PLATE_R + g.PLATE_MARGIN, 3)
    n = sum(len(h) for h in holes)
    return len(outline) + n + 2 * len(holes) - 2 + 2 * (rings - 1) * n


def build_plate_lod1(kit, face, holes, name):
    """The LOD1 stencil plate of a long legend: the word's outline from its own generator at a coarser arc
    flattening (the legend file's "lod1"), the tied holes without the R 0.5 punch fillets, and one cut wall band
    from the front edge to the back (no chamfer): at the LOD1 switch (5 m, 3.5 m on the phone tier) a millimetre is
    half a pixel. Kept out of LOD0 (removed from the kit's parts); legend_lod1 joins it into the LOD1."""
    mb = MB()
    cp = -g.LIP_DEPTH
    cb = cp - g.SHEET
    outline = rr(g.OPEN_W + 2 * g.PLATE_MARGIN, g.OPEN_H + 2 * g.PLATE_MARGIN, g.PLATE_R + g.PLATE_MARGIN, 3)
    loops = [mb.ring([face.blender(a, b, cp) for a, b in outline], [face.face_uv(a, b) for a, b in outline])]
    for hole in holes:
        poly = [g.open_to_reader(p) for p in hole]
        front = mb.ring([face.blender(a, b, cp) for a, b in poly], [face.face_uv(a, b) for a, b in poly])
        # The wall's UVs run into the hole by its depth, as the hero plate's do (the painted cut wall).
        back = mb.ring([face.blender(a, b, cb) for a, b in poly], [face.face_uv(a, b) for a, b in kc.offset_poly(poly, g.SHEET)])
        mb.bridge(front, back)
        loops.append(front)
    MB.orient_each(mb.fill(loops), face.want())
    obj = mb.finish(kit, FACE, name, True)
    _fix_hole_walls(obj, face, holes=holes)
    kit.parts.remove(obj)
    tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
    want = plate_tris(holes, 2)
    assert tris == want, "%s: LOD1 plate %d tris, plate_tris() says %d" % (kit.name, tris, want)
    return wear(obj)


def legend_lod1(kit, faces):
    """A legend with LOD1 plates: the kit's LOD1 is the English kit's LOD1 without its plate (the housing, frame and
    diffuser decimated to what the English sign keeps: the same triangle target, its plate's share taken off) plus
    the LOD1 plates. build_asset calls kit.make_lod1(module.LOD1) after build(); this wraps it for this kit only
    (kitlib is not edited, English and 出口 never come here)."""
    plates = getattr(kit, "legend_lod1_plates", None)
    if not plates:
        return
    base = kit.make_lod1
    hero = kit.legend_hero_plate_tris
    en_plate = plate_tris(english_hole_polys(), 4) * faces

    def make_lod1(ratio):
        if ratio is None:
            return base(ratio)
        lod0 = kit.meta["triangles"]
        rest = (lod0 - hero + en_plate) * ratio - en_plate
        base(rest / float(lod0))
        lod1 = kit.lod1
        rest_tris = sum(len(p.vertices) - 2 for p in lod1.data.polygons)
        plate1 = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in plates)
        kit._join([lod1] + list(plates))
        lod1.data.shade_smooth()
        lod1.data.set_sharp_from_angle(angle=math.radians(getattr(kit, "smooth_angle", 35.0)))
        tris = sum(len(p.vertices) - 2 for p in lod1.data.polygons)
        kit.meta["trianglesLod1"] = tris
        budget = (kit.meta.get("lodBudget") or [0, 0])[1]
        kit.meta["budgetNoteLod1"] = "legend %s LOD1: %d tris (rest %d as English's %d less its plate %d; LOD1 plates %d) against %d (%+.0f%%)" % (
            g.LEGEND["word"], tris, rest_tris, int(round(rest + en_plate)), en_plate, plate1, budget, 100.0 * (tris - budget) / max(budget, 1))
        print("[exit_sign] " + kit.meta["budgetNoteLod1"])
    kit.make_lod1 = make_lod1


def _fix_hole_walls(obj, face, chevrons="closed", holes=None):
    """Cut walls must face into their hole (toward the hole's own centre line)."""
    me = obj.data
    bm = bmesh.new()
    bm.from_mesh(me)
    want = face.want()
    bm.faces.ensure_lookup_table()
    # Group wall faces (normal roughly in the plate plane) and orient each toward
    # the nearest hole's centroid: a wall of a hole of a CCW polygon faces inward.
    holes = [[g.open_to_reader(p) for p in h] for h in (g.hole_polys(chevrons) if holes is None else holes)]
    cents = []
    for h in holes:
        cx = sum(p[0] for p in h) / len(h)
        cy = sum(p[1] for p in h) / len(h)
        cents.append((h, cx, cy))
    inv_r = face.r
    for f in bm.faces:
        f.normal_update()
        if abs(f.normal.dot(want)) > 0.5:
            continue
        c = f.calc_center_median()
        # Unity-space centre -> reader (a, b)
        P = Vector((-c.x, c.z, -c.y))
        rel = P - face.o
        a = rel.dot(inv_r) / MM
        b = rel.y / MM
        # inside which hole polygon (or nearest)?
        best, bd = None, 1e18
        for h, cx, cy in cents:
            if _point_near_poly(a, b, h):
                best = (h, cx, cy)
                break
            dd = (a - cx) ** 2 + (b - cy) ** 2
            if dd < bd:
                bd, best = dd, (h, cx, cy)
        h = best[0]
        # inward direction: from the face centre toward the closest interior point,
        # approximated by the polygon edge normal of the nearest edge (CCW -> left normal).
        e = _nearest_edge(a, b, h)
        (x0, y0), (x1, y1) = e
        ex, ey = x1 - x0, y1 - y0
        L = math.hypot(ex, ey) or 1.0
        inward_r = (-ey / L, ex / L)       # left normal of a CCW edge points inside the hole
        inward_U = face.r * inward_r[0] + face.up * inward_r[1]
        inward_B = Vector(kc.U(inward_U.x, inward_U.y, inward_U.z))
        if f.normal.dot(inward_B) < 0:
            f.normal_flip()
    bm.to_mesh(me)
    bm.free()


def _point_near_poly(a, b, poly):
    # crossing-number test with a 1.5 mm tolerance band (walls sit on the outline)
    inside = False
    n = len(poly)
    for i in range(n):
        x0, y0 = poly[i]
        x1, y1 = poly[(i + 1) % n]
        if (y0 > b) != (y1 > b):
            xi = x0 + (b - y0) * (x1 - x0) / (y1 - y0)
            if xi > a:
                inside = not inside
    if inside:
        return True
    e = _nearest_edge(a, b, poly)
    return _seg_dist(a, b, e) < 1.5


def _seg_dist(a, b, e):
    (x0, y0), (x1, y1) = e
    ex, ey = x1 - x0, y1 - y0
    L2 = ex * ex + ey * ey or 1e-12
    t = max(0.0, min(1.0, ((a - x0) * ex + (b - y0) * ey) / L2))
    return math.hypot(a - (x0 + t * ex), b - (y0 + t * ey))


def _nearest_edge(a, b, poly):
    n = len(poly)
    best, bd = None, 1e18
    for i in range(n):
        e = (poly[i], poly[(i + 1) % n])
        d = _seg_dist(a, b, e)
        if d < bd:
            bd, best = d, e
    return best


def build_spacer(kit, face, name="spacer"):
    """White spacer: the inner wall round the opening, plate back to diffuser."""
    mb = MB()
    c0 = -g.LIP_DEPTH - g.SHEET
    c1 = c0 - g.DIFFUSER_GAP
    poly = rr(g.OPEN_W, g.OPEN_H, g.OPEN_R, 3)
    r0 = mb.ring([face.blender(a, b, c0) for a, b in poly])
    r1 = mb.ring([face.blender(a, b, c1) for a, b in poly])
    faces = mb.bridge(r0, r1)
    # walls face the opening's centre: test the face on the +a (reader's right) side
    for f in faces:
        f.normal_update()
    centre = V(face.blender(0, 0, (c0 + c1) / 2))
    for f in faces:
        if f.normal.dot(centre - f.calc_center_median()) < 0:
            f.normal_flip()
    obj = mb.finish(kit, HOUSING, name, False)
    kit.lod1_drop(obj)
    kc.drop2(obj)
    return wear(obj)


def build_diffuser(kit, face, slot=DIFFUSER, name="diffuser"):
    mb = MB()
    c = -g.LIP_DEPTH - g.SHEET - g.DIFFUSER_GAP
    w, h = g.OPEN_W / 2 + g.PLATE_MARGIN, g.OPEN_H / 2 + g.PLATE_MARGIN
    pts = [(-w, -h), (w, -h), (w, h), (-w, h)]
    vs = [mb.v(face.blender(a, b, c), face.diff_uv(a, b)) for a, b in pts]
    f = mb.bm.faces.new(vs)
    MB.orient_each([f], face.want())
    return wear(mb.finish(kit, slot, name, True))


def build_diffuser_back(kit, face, name="diffuser back"):
    """The diffuser's inner side (white interior, HOUSING slot), facing INTO the box.
    Build round 3: the interior had no surface facing outward from inside (the plate
    has no back, the diffuser was one outward quad), so a shadow-map ray that slipped
    through the 0.4 mm frame seam crossed the whole sign and left light dashes in its
    shadow (Run room ceiling, under the red light). Hidden from every outside view."""
    mb = MB()
    c = -g.LIP_DEPTH - g.SHEET - g.DIFFUSER_GAP - 0.05
    w, h = g.OPEN_W / 2 + g.PLATE_MARGIN, g.OPEN_H / 2 + g.PLATE_MARGIN
    pts = [(-w, -h), (w, -h), (w, h), (-w, h)]
    f = mb.bm.faces.new([mb.v(face.blender(a, b, c)) for a, b in pts])
    MB.orient_each([f], -face.want())
    obj = mb.finish(kit, HOUSING, name, False)
    return _lod_keep(obj)


def _lod_keep(obj):
    """Protect a hidden shadow part from kitlib's LOD1 collapse: kitlib marks thin
    and decal parts with the vertex group "fr_lod_keep", and the join merges groups
    by name, so the same group on this part keeps it whole in the LOD1 (kitlib.py
    itself is not touched)."""
    vg = obj.vertex_groups.get("fr_lod_keep") or obj.vertex_groups.new(name="fr_lod_keep")
    vg.add([v.index for v in obj.data.vertices], 1.0, "REPLACE")
    return obj


CHASSIS_INSET = 8.0          # mm in from the pan's outside (clears the R10 drawn corners)
CHASSIS_GAP = 2.0            # mm behind the diffuser's back (no depth fight at distance)


def chassis_front_c():
    """Face-local c (mm, 0 = the frame's front flat) of the chassis front."""
    return -g.LIP_DEPTH - g.SHEET - g.DIFFUSER_GAP - CHASSIS_GAP


def build_chassis(kit, centre_U, size_U, name="lamp chassis"):
    """The lamp chassis: a closed box inside the housing (the sockets and wiring tray
    of a real sign), on HOUSING, 12 triangles. Build round 3: it makes the sign a
    solid shadow caster, so the seams, the stencil holes and the one-sided interior
    no longer let shadow-map rays through. Hidden behind the diffusers in every view.
    centre_U / size_U: Unity metres (part space)."""
    obj = kit.box(kc.U_size(*size_U), kc.U(*centre_U), HOUSING, bevel=0.0, segments=1, name=name)
    return _lod_keep(obj)


def build_screws(kit, face, segs=12):
    out = []
    for (a, b), deg in zip(g.screw_positions(), g.SCREW_SLOT_DEG):
        p = face.unity(a, b, 0.0)
        s = kc.oval_screw(kit, (p.x, p.y, p.z), CHROME, axis=face.screw_axis, head_d=g.SCREW_HEAD_D * MM,
                          dome_h=g.SCREW_DOME * MM, slot_w=0.0007, slot_d=0.0008, slot_deg=deg, segs=segs, name="face screw")
        kit.lod1_drop(s)
        kc.drop2(s)
        out.append(wear(s))
    return out


def build_print_film(kit, face, name="print film"):
    """A printed legend (world text option B, research/exit_sign/80): no stencil plate; the lens is screen-printed with an
    opaque white ink everywhere but the characters. The ink is a film 0.4 mm proud of the diffuser's front (so the lit
    characters and the print sit on one sheet, no parallax), the characters cut out of it and their counters left in it
    as islands. It takes the face's material and decal UVs (the face's white enamel), so the diffuser's own red tint
    (ExitSign_Diffuser _BaseColor) never colours the print."""
    mb = MB()
    c = -g.LIP_DEPTH - g.SHEET - g.DIFFUSER_GAP + 0.4
    outline = rr(g.OPEN_W + 2 * g.PLATE_MARGIN, g.OPEN_H + 2 * g.PLATE_MARGIN, g.PLATE_R + g.PLATE_MARGIN, 3)
    loops = [mb.ring([face.blender(a, b, c) for a, b in outline], [face.face_uv(a, b) for a, b in outline])]
    for lp in g.LEGEND["ink"]:
        poly = [g.open_to_reader((float(x), float(y))) for x, y in lp]
        loops.append(mb.ring([face.blender(a, b, c) for a, b in poly], [face.face_uv(a, b) for a, b in poly]))
    front = mb.fill(loops)
    MB.orient_each(front, face.want())
    return wear(mb.finish(kit, FACE, name, True))


def build_face(kit, face, chevrons="closed"):
    if g.LEGEND is not None and g.LEGEND.get("mode") == "print":
        parts = [build_frame(kit, face), build_print_film(kit, face), build_spacer(kit, face), build_diffuser(kit, face),
                 build_diffuser_back(kit, face)]
        parts += build_screws(kit, face)
        return parts
    parts = [build_frame(kit, face), build_plate(kit, face, chevrons), build_spacer(kit, face), build_diffuser(kit, face),
             build_diffuser_back(kit, face)]
    parts += build_screws(kit, face)
    return parts


# ------------------------------------------------------------------ housings
def pan_rings_profile(z_front_seam):
    """Pan side profile (inset, Z mm) from the back edge to the seam flange."""
    zp = z_front_seam
    # first ring = a flat support ring 0.3 mm inside the roll, so the back cap shades flat
    return [(2.3, 0.0), (2.0, 0.0), (0.586, 0.586), (0.0, 2.0), (0.0, zp - 0.5), (0.15, zp - 0.15), (0.5, zp), (1.6, zp), (1.6, zp - 1.2)]


def build_pan_wall(kit, yc, zf, seg=8, name="housing"):
    """Single-face pan on the wall: back at Z 0 (with the two keyholes), side
    walls with drawn R10 corners and an R2 back edge, a front flange that is
    the bottom of the frame seam. yc = housing centre Y (mm), zf = the frame's
    front flat Z (mm)."""
    mb = MB()
    zp = zf - g.FRAME_SKIRT - g.SEAM_GAP
    prof = pan_rings_profile(zp)
    rings = []
    for d, z in prof:
        poly = kc.offset_poly(rr(g.HOUSING_W, g.HOUSING_H, g.HOUSING_R, seg), d)
        rings.append(mb.ring([kc.U(-a * MM, (yc + b) * MM, z * MM) for a, b in poly]))
    sides = []
    for k in range(len(rings) - 1):
        sides += mb.bridge(rings[k], rings[k + 1])
    # outward test: a face on the reader's-right side wall (Unity -X) must face -X.
    ref = max(sides, key=lambda f: f.calc_center_median().x)      # Blender +x = Unity -X
    MB.orient(sides, Vector((1, 0, 0)), ref)
    # Back cap with the keyholes (blind 1 mm pockets: the dark box interior).
    loops = [rings[0]]
    pockets = []
    for sx in (+1, -1):
        kh = keyhole_poly(sx * g.KEYHOLE_PITCH / 2, 140.0)
        top = mb.ring([kc.U(-a * MM, b * MM, 0.0) for a, b in kh])
        bot = mb.ring([kc.U(-a * MM, b * MM, 1.0 * MM) for a, b in kh])
        walls = mb.bridge(top, bot)
        pockets.append((walls, kh))
        loops.append(top)
        cap = mb.fill([bot])
        MB.orient_each(cap, Vector(kc.U(0, 0, -1)))
    back = mb.fill(loops)
    MB.orient_each(back, Vector(kc.U(0, 0, -1)))
    for walls, kh in pockets:
        cx = sum(p[0] for p in kh) / len(kh)
        cy = sum(p[1] for p in kh) / len(kh)
        c = Vector(kc.U(-cx * MM, cy * MM, 0.5 * MM))
        for f in walls:
            f.normal_update()
            if f.normal.dot(c - f.calc_center_median()) < 0:
                f.normal_flip()
    return wear(mb.finish(kit, HOUSING, name, False))


def keyhole_poly(cx, y_top):
    """Keyhole in READER mm: Ø8.5 entry, 4.5 wide slot rising to the screw seat at y_top."""
    R, w = 4.25, 2.25
    yc = y_top - 8.0
    pts = []
    # slot top (semicircle round the seat)
    for i in range(5):
        a = math.radians(0 + 180 * i / 4)
        pts.append((cx + w * math.cos(a), y_top + w * math.sin(a)))
    # down the left side of the slot to the circle
    sa = math.degrees(math.asin(w / R))
    for i in range(9):
        a = math.radians(90 + sa + (360 - 2 * sa) * i / 8)
        pts.append((cx + R * math.cos(a), yc + R * math.sin(a)))
    return g.ccw(pts)


def build_ring_double(kit, yc, zf, seg=8, name="housing", to_unity=None):
    """Double-face housing: the side ring between the two face frames.
    to_unity(a, b, z) -> Unity mm (default: the ring round +-Z, X = -a,
    Y = yc + b, Z = z). a/b = the reader's right/up of the +n face."""
    if to_unity is None:
        to_unity = lambda a, b, z: (-a, yc + b, z)
    mb = MB()
    zp = zf - g.FRAME_SKIRT - g.SEAM_GAP
    # back flange -> back seam -> side wall -> front seam -> front flange
    prof = [(1.6, -(zp - 1.2)), (1.6, -zp), (0.5, -zp), (0.15, -(zp - 0.15)), (0.0, -(zp - 0.5)),
            (0.0, zp - 0.5), (0.15, zp - 0.15), (0.5, zp), (1.6, zp), (1.6, zp - 1.2)]
    base = rr(g.HOUSING_W, g.HOUSING_H, g.HOUSING_R, seg)
    rings = []
    for d, z in prof:
        poly = kc.offset_poly(base, d)
        rings.append(mb.ring([kc.U(*[c * MM for c in to_unity(a, b, z)]) for a, b in poly]))
    bands = [mb.bridge(rings[k], rings[k + 1]) for k in range(len(rings) - 1)]
    faces = [f for band in bands for f in band]
    # Reference: the middle (side wall) band's first face must face away from the axis.
    k = 4
    a0, b0 = base[0]
    a1, b1 = base[1]
    am, bm_ = (a0 + a1) / 2, (b0 + b1) / 2
    p_out = Vector(kc.U(*[c * MM for c in to_unity(am, bm_, 0.0)]))
    p_axis = Vector(kc.U(*[c * MM for c in to_unity(0.0, 0.0, 0.0)]))
    MB.orient(faces, (p_out - p_axis).normalized(), bands[k][0])
    return wear(mb.finish(kit, HOUSING, name, False))


def build_knockout(kit, centre_U):
    """1/2 in concentric knockout, stamped 0.4 mm proud on the top (centre_U in Unity m)."""
    ko = kit.cylinder(g.KO_D / 2 * MM, 0.0008, (0, 0, 0), HOUSING, verts=20, bevel=0.0003, segments=1, name="knockout")
    kc.orient(ko, centre_U, (0, 1, 0), (0, 0, 1))
    kit.lod1_drop(ko)
    kc.drop2(ko)
    return wear(ko)


def build_canopy(kit, length, width, height, radius, y_ceiling=0.0, seg=4, name="canopy", to_unity=None, out_U=(0, -1, 0)):
    """Rectangular canopy pan, open on its mounting side. to_unity(a, b, d) ->
    Unity mm, d >= 0 = distance from the mounting surface (default: on the
    ceiling, X = -a, Y = y_ceiling - d, Z = b). out_U = the cap's outward normal."""
    if to_unity is None:
        to_unity = lambda a, b, d: (-a, y_ceiling - d, b)
    mb = MB()
    prof = [(0.0, 0.0), (0.0, height - 2.0), (0.586, height - 0.586), (2.0, height), (2.3, height)]   # + flat support ring
    base = rr(length, width, radius, seg)
    rings = []
    for d, dd in prof:
        poly = kc.offset_poly(base, d)
        rings.append(mb.ring([kc.U(*[c * MM for c in to_unity(a, b, dd)]) for a, b in poly]))
    bands = [mb.bridge(rings[k], rings[k + 1]) for k in range(len(rings) - 1)]
    faces = [f for band in bands for f in band]
    a0, b0 = base[0]
    a1, b1 = base[1]
    p_out = Vector(kc.U(*[c * MM for c in to_unity((a0 + a1) / 2, (b0 + b1) / 2, height / 2)]))
    p_axis = Vector(kc.U(*[c * MM for c in to_unity(0.0, 0.0, height / 2)]))
    MB.orient(faces, (p_out - p_axis).normalized(), bands[0][0])
    cap = mb.fill([rings[-1]])
    MB.orient_each(cap, Vector(kc.U(*out_U)))
    return wear(mb.finish(kit, HOUSING, name, False))


def chrome_cyl(kit, r, length, centre_U, axis_U, verts=12, bevel=0.0004, name="stem"):
    c = kit.cylinder(r, length, (0, 0, 0), CHROME, verts=verts, bevel=bevel, segments=1, name=name)
    kc.orient(c, centre_U, axis_U, (0, 0, 1) if abs(axis_U[1]) > 0.9 else (0, 1, 0))
    return wear(c)


def build_stems(kit, y_top_housing, y_canopy_bottom, xs=(g.STEM_X, -g.STEM_X), collars=True):
    """Ø12 chrome stems from the canopy to the housing top, a collar under the
    canopy and a hex lock nut + washer on the housing top."""
    parts = []
    for x in xs:
        X = x * MM
        L = (y_canopy_bottom - y_top_housing) * MM
        if L > 0.002:
            parts.append(chrome_cyl(kit, g.STEM_D / 2 * MM, L + 0.004, (X, (y_top_housing + y_canopy_bottom) / 2 * MM - 0.0, 0.0),
                                    (0, 1, 0), verts=14, name="stem"))
        if collars:
            collar = chrome_cyl(kit, 0.0085, 0.006, (X, y_canopy_bottom * MM - 0.003, 0.0), (0, 1, 0), verts=16, name="collar")
            kit.lod1_drop(collar)
            parts.append(collar)
        nut = chrome_cyl(kit, 0.0087, 0.0055, (X, y_top_housing * MM + 0.0042, 0.0), (0, 1, 0), verts=6, bevel=0.0006, name="lock nut")
        washer = chrome_cyl(kit, 0.0105, 0.0014, (X, y_top_housing * MM + 0.0007, 0.0), (0, 1, 0), verts=16, bevel=0.0003, name="washer")
        kit.lod1_drop(washer)
        parts += [nut, washer]
    return parts


def canopy_screws(kit, y_bottom, xs=(g.KEYHOLE_PITCH / 2, -g.KEYHOLE_PITCH / 2)):
    out = []
    for x, deg in zip(xs, (40.0, -15.0)):
        s = kc.oval_screw(kit, (x * MM, y_bottom * MM, 0.0), CHROME, axis="-Y", head_d=0.0062, dome_h=0.0012,
                          slot_w=0.0007, slot_d=0.0008, slot_deg=deg, segs=12, name="canopy screw")
        kit.lod1_drop(s)
        kc.drop2(s)
        out.append(wear(s))
    return out


# ------------------------------------------------------------------ meta
def exit_meta(kit, faces, chevrons="closed"):
    kit.meta["exitSign"] = {
        "faces": faces,
        "openingM": [round(g.OPEN_W * MM, 4), round(g.OPEN_H * MM, 4)],
        "letterHeightM": round(g.H * MM, 4),
        "strokeM": round(g.S * MM, 5),
        "faceSlot": FACE,
        "diffuserSlot": DIFFUSER,
        "deadSlot": DIFFUSER_DEAD,
        "chevrons": chevrons,
        "lampField": {"lampA": list(g.LAMP_A), "lampB": list(g.LAMP_B), "fill": g.LAMP_FILL,
                      "dc": [list(x) for x in g.DC_LAMPS], "dcLevel": g.DC_LEVEL,
                      "channels": "ExitSign_Diffuser_F: R lamp A, G lamp B, B DC pair, A transmission"},
    }
    if g.LEGEND is not None:
        # World text (95 §6 Q2): the legend in another language. Its letter height and stroke come from the legend file
        # (exit_legend.py: a die-cut legend is constructed at the English stroke, WORLD_TYPE §3); the closed chevron
        # knockouts are gone (no room beside a CJK line or a longer word).
        # A Latin or Cyrillic legend is cut in EXIT's own die face, extended (平面视觉's exit_dieface.swift + exit_legend_dieface.py,
        # WORLD_TYPE §3 "EXIT in the Latin and Cyrillic languages"): no variable font and no em, so its face is named
        # as the file names it and its cap height (letterHeightMm) stands where a CJK legend gives its em.
        L = g.LEGEND
        es = kit.meta["exitSign"]
        es["legend"] = L["word"]
        es["legendMode"] = L["mode"]
        es["legendFace"] = ("%s wght %d" % (L["face"], int(L["wght"]))) if "wght" in L else L["face"]
        if "emMm" in L:
            es["legendEmM"] = round(L["emMm"] * MM, 5)
        else:
            es["legendCapM"] = round(L["letterHeightMm"] * MM, 5)
        es["legendWidth"] = L["width"]
        es["letterHeightM"] = round(L["inkH"] * MM, 4)
        sv = L["strokes"]["verticalStemMm"] or 0.0
        sh = L["strokes"]["horizontalStrokeMm"] or 0.0
        es["strokeM"] = round(min(sv, sh) * MM, 5)
        es["chevrons"] = "none"
        if "letterShares" in L:
            es["letterShares"] = L["letterShares"]
        if L.get("lod1"):
            es["legendLod1"] = {"tolMm": L["lod1"]["tolMm"], "holeVerts": sum(len(h) for h in L["lod1"]["holes"]),
                                "plate": "tied holes without the punch fillets, one wall band"}
            legend_lod1(kit, faces)
    kit.meta["lodDistances"] = list(LOD_DISTANCES)


def tri_count(kit):
    dg = bpy.context.evaluated_depsgraph_get()
    tris = 0
    for obj in kit.parts:
        ev = obj.evaluated_get(dg)
        me = ev.to_mesh()
        tris += sum(len(p.vertices) - 2 for p in me.polygons)
        ev.to_mesh_clear()
    return tris


def part_report(kit):
    dg = bpy.context.evaluated_depsgraph_get()
    rows = {}
    for obj in kit.parts:
        ev = obj.evaluated_get(dg)
        me = ev.to_mesh()
        n = sum(len(p.vertices) - 2 for p in me.polygons)
        ev.to_mesh_clear()
        key = obj.name.split(".")[0]
        rows[key] = rows.get(key, 0) + n
    print("[exit_sign] parts: " + ", ".join("%s %d" % kv for kv in sorted(rows.items(), key=lambda kv: -kv[1])))
    return rows


def check_budget(kit, target, tol=0.15):
    part_report(kit)
    tris = tri_count(kit)
    lo, hi = target * (1 - tol), target * (1 + tol)
    if g.LEGEND is not None:
        # A legend in another language: the budget is reported, not enforced (research/exit_sign/80 COST row).
        kit.meta["budgetNote"] = "legend %s: %d tris against %d +/- %d%% (%+.0f%%)" % (g.LEGEND["word"], tris, target, int(tol * 100), 100.0 * (tris - target) / target)
        print("[exit_sign] " + kit.meta["budgetNote"])
        kit.meta["trianglesCheck"] = tris
        return tris
    assert lo <= tris <= hi, "%s: %d tris, budget %d +/- %d%%" % (kit.name, tris, target, int(tol * 100))
    kit.meta["trianglesCheck"] = tris
    return tris
