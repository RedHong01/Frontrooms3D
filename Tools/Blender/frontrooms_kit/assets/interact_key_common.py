"""G3 shared helpers: keys, rings, tags, key hosts and door signage.

Helper module (no NAME, no build): imported by the interact_key_*,
interact_door_sign, interact_door_number_plate and interact_exit_sign
modules. Spec: Documentation/research/interactables/10_spec.md §2.4, §3.2,
§3.3, §4, §9.3, §10.0, §10.3.

Frames
* Every G3 asset is authored in Unity part-local metres (X, Y, Z) and
  converted with U(): Blender (x, y, z) = (-X, -Z, Y). kitlib.export() writes
  Blender (x, y, z) as Unity (-x, z, -y) (kitlib.py:789-806), so a point
  given to U() comes out in the sidecar exactly as written here, and the kit
  "front" (Blender -Y) is Unity +Z.
* The KEY frame (§3.2, §3.3): origin on the turning axis at the shoulder,
  +Z toward the tip (insertion), +Y = cuts up, X = the flat normal.
  The PLUG frame (G2, kit "front" = out of the door) is the key frame turned
  180 deg about Y at full insertion: X_plug = -X_key, Z_plug = -Z_key. The
  §3.2 keyway numbers are in the KEY frame, so G2 mirrors X when it cuts
  the keyway. key_to_plug() is that transform (used by the T8 fit test).

The §3.2 numbers below must stay IDENTICAL to G2's interact_lock_common.py.
assert_blade_spec() checks them against the spec text.

Wear (§1.8 W1/W2): every part carries a POINT colour attribute "fr_wear"
(white = clean; darker R = hand grime, darker G = edge wear, darker B =
cavity). Nothing reads it until P-3 is approved.

LOD (§1.8, §9.3): every G3 asset is under 1.0 m, so no module sets LOD1
(no LODGroup until P-1); modules record lodDistances / lodRatios /
lodBudget in kit.meta and mark LOD2 drops with obj["fr_lod2_drop"].
"""

import math

import bmesh
import bpy
from mathutils import Matrix, Vector

import kitlib

# --------------------------------------------------------------- frames
def U(X, Y, Z):
    """Unity part-local point -> Blender point."""
    return (-X, -Z, Y)


def U_size(SX, SY, SZ):
    """Unity box size (X, Y, Z) -> kit.box size (Blender x, y, z)."""
    return (SX, SZ, SY)


def key_to_plug(p):
    """Key-frame point at full insertion -> plug-frame point (both Unity)."""
    return (-p[0], p[1], -p[2])


# ------------------------------------------------- §3.2 blade and keyway
# Blade cross-section (key frame, metres, counter-clockwise), exactly as
# printed in §3.2. The spine corners get 0.2 mm chamfers (SPINE_CHAMFER).
BLADE_SECTION_SPEC = [
    (-0.0010, -0.0040), (+0.0010, -0.0040),                                         # spine
    (+0.0010, -0.0020), (+0.0006, -0.0020), (+0.0006, -0.0008), (+0.0010, -0.0008),  # right-flat groove
    (+0.0010, +0.0046), (-0.0010, +0.0046),                                         # bitting edge
    (-0.0010, +0.0018), (-0.0006, +0.0018), (-0.0006, +0.0006), (-0.0010, +0.0006),  # left-flat groove
]
SPINE_CHAMFER = 0.0002
KEYWAY_CLEARANCE = 0.00015          # keyway = blade section offset outward 0.15 mm
KEYWAY_SPEC = {                      # §3.2 printed keyway numbers (key frame)
    "walls_x": 0.00115,
    "rib_px": (-0.00185, -0.00095, +0.00075),   # +X wall rib: y0, y1, reaches x
    "rib_nx": (+0.00075, +0.00165, -0.00075),   # -X wall rib
    "y_bottom": -0.00415,
    "y_top_open": +0.0048,
}
BLADE_TOP = 0.0046
BLADE_LEN = 0.025                   # = Unlock.InsertDepth
BLADE_THICK = 0.0020
CUT_Z = (0.0040, 0.0078, 0.0116, 0.0154, 0.0192, 0.0230)
CUT_STEPS = (2, 4, 1, 5, 3, 2)
CUT_STEP = 0.00038
CUT_BASE = 0.0002
CUT_FLAT = 0.0008
CUT_FLANK_DEG = 50.0                # flank angle from the vertical (100 deg included) - ESTIMATE reading of "50 deg flanks"
TIP_BEVEL_Z = 0.0215
TIP_Y = 0.0005
NOSE_R = 0.0005
PIN_Z = CUT_Z                       # G2's pin tips hang at the cut Z values


def blade_section():
    """§3.2 section with the 0.2 mm spine chamfers: 14 points, CCW."""
    c = SPINE_CHAMFER
    pts = [(-0.0010, -0.0040 + c), (-0.0010 + c, -0.0040), (+0.0010 - c, -0.0040), (+0.0010, -0.0040 + c)]
    pts += BLADE_SECTION_SPEC[2:]
    return pts


def cut_depths():
    return [s * CUT_STEP + CUT_BASE for s in CUT_STEPS]


def cut_bottoms():
    return [BLADE_TOP - d for d in cut_depths()]


def _flank_k():
    return 1.0 / math.tan(math.radians(CUT_FLANK_DEG))


def _top_lines():
    """Every straight line the bitting envelope is made of: (a, b) for y = a + b z."""
    k, h = _flank_k(), CUT_FLAT / 2
    lines = [(BLADE_TOP, 0.0)]
    for z, b in zip(CUT_Z, cut_bottoms()):
        lines.append((b, 0.0))                       # flat
        lines.append((b + k * (z - h), -k))          # left flank
        lines.append((b - k * (z + h), k))           # right flank
    return lines


def _env0(z):
    k, h = _flank_k(), CUT_FLAT / 2
    y = BLADE_TOP
    for zc, b in zip(CUT_Z, cut_bottoms()):
        y = min(y, b + k * max(0.0, abs(z - zc) - h))
    return y


def _bevel_line():
    y0 = _env0(TIP_BEVEL_Z)
    slope = (TIP_Y - y0) / (BLADE_LEN - TIP_BEVEL_Z)
    return (y0 - slope * TIP_BEVEL_Z, slope)


def top_at(z):
    """Bitting edge height (key frame Y) at blade station z: cuts + tip bevel."""
    y = _env0(z)
    if z > TIP_BEVEL_Z:
        a, b = _bevel_line()
        y = min(y, a + b * z)
    return y


def top_profile(z0=0.0, z1=BLADE_LEN):
    """Exact breakpoints (z, y) of the bitting edge between z0 and z1."""
    lines = _top_lines() + [_bevel_line()]
    cand = {z0, z1, TIP_BEVEL_Z}
    h = CUT_FLAT / 2
    for zc in CUT_Z:
        cand.update((zc - h, zc + h))
    for i in range(len(lines)):
        for j in range(i + 1, len(lines)):
            (a1, b1), (a2, b2) = lines[i], lines[j]
            if abs(b1 - b2) > 1e-12:
                z = (a2 - a1) / (b1 - b2)
                if z0 <= z <= z1:
                    cand.add(z)
    zs = sorted(cand)
    pts = [(z, top_at(z)) for z in zs]
    out = [pts[0]]
    for i in range(1, len(pts) - 1):
        (za, ya), (zb, yb), (zc, yc) = out[-1], pts[i], pts[i + 1]
        if zc - za < 1e-12:
            continue
        if abs(ya + (yc - ya) * (zb - za) / (zc - za) - yb) > 1e-9:
            out.append(pts[i])
    out.append(pts[-1])
    return out


def keyway_section():
    """The plug keyway in the KEY frame: blade section (no chamfers) offset
    outward by KEYWAY_CLEARANCE. Returns the dict of derived numbers."""
    c = KEYWAY_CLEARANCE
    return {
        "walls_x": 0.0010 + c,
        "rib_px": (-0.0020 + c, -0.0008 - c, 0.0006 + c),
        "rib_nx": (0.0006 + c, 0.0018 - c, -0.0006 - c),
        "y_bottom": -0.0040 - c,
    }


def assert_blade_spec():
    """§3.2 / §3.3 numbers, asserted at build time."""
    sec = BLADE_SECTION_SPEC
    xs = [p[0] for p in sec]
    ys = [p[1] for p in sec]
    assert abs(max(xs) - min(xs) - BLADE_THICK) < 1e-9, "blade must be 2.0 mm thick"
    assert abs(min(ys) + 0.0040) < 1e-9 and abs(max(ys) - BLADE_TOP) < 1e-9
    area = sum(sec[i][0] * sec[(i + 1) % len(sec)][1] - sec[(i + 1) % len(sec)][0] * sec[i][1] for i in range(len(sec)))
    assert area > 0, "§3.2 section must be counter-clockwise"
    kw = keyway_section()
    for key in ("walls_x", "y_bottom"):
        assert abs(kw[key] - KEYWAY_SPEC[key]) < 1e-9, (key, kw[key])
    for key in ("rib_px", "rib_nx"):
        for a, b in zip(kw[key], KEYWAY_SPEC[key]):
            assert abs(a - b) < 1e-9, (key, kw[key])
    d = cut_depths()
    assert [round(x, 7) for x in d] == [0.00096, 0.00172, 0.00058, 0.0021, 0.00134, 0.00096], d
    assert all(abs(CUT_Z[i + 1] - CUT_Z[i] - 0.0038) < 1e-9 for i in range(5)), "0.0038 pitch"
    # cuts never reach the grooves (deepest floor stays above the left groove top)
    assert min(cut_bottoms()) > 0.0018
    assert abs(top_at(BLADE_LEN) - TIP_Y) < 1e-9
    return True


# --------------------------------------------------------- slots, atlases
KEYTAGNO = "Prop_KeyTagNo"          # NEW (P-4): 2048^2, 10 x 10 typed numbers 00-99 on paper
KEYTAGNO_FALLBACK = "Prop_Paper"
SIGN_SLOT = "Prop_SignEngraved"     # NEW (P-4): 2048 x 1024, 2 x 2 engraved texts
EXIT_FACE = "Run_ExitSign"          # existing surface (emissive EXIT)
EXIT_DEAD = "Run_ExitSign_Dead"     # NEW (P-4): non-emissive copy


def register_slots():
    """Preview colours of §2-§4 (the Unity look comes from Resources/Surfaces)."""
    kitlib.register_slot(KEYTAGNO, (0.86, 0.84, 0.77), 0.8, 0.0)          # typed paper
    kitlib.register_slot(SIGN_SLOT, (0.19, 0.14, 0.12), 0.45, 0.0)        # dark brown cap ply, sRGB 48/36/30
    kitlib.register_slot(EXIT_FACE, (0.89, 0.87, 0.84), 0.4, 0.0)         # face ground (letters come from the texture)
    kitlib.register_slot(EXIT_DEAD, (0.62, 0.60, 0.57), 0.5, 0.0)


NUM_COLS = NUM_ROWS = 10
NUMBER_DEFAULT_CELL = 0               # quads ship pointing at "00"
# Inside one 204.8 px cell the two typed digits are centred, ink height
# 0.24 of the cell, ink width about 0.42 (Courier Prime, measured on the
# proposal atlas). Each window crops a centred band of the cell.
DIGIT_INK_H = 0.24


def number_cell_rect(n, crop_w, crop_h, grow_u=1.0, grow_v=1.0):
    """uv_rect for atlas cell n (row-major from the top-left), cropped to a
    centred crop_w x crop_h fraction of the cell. grow_* > 1 extends the
    rect for a quad that is larger than its visible window (it slides
    under a lip), keeping the visible window on the crop."""
    u0, v0, u1, v1 = kitlib.Kit.atlas_cell(n, NUM_COLS, NUM_ROWS)
    cu, cv = (u0 + u1) / 2, (v0 + v1) / 2
    hw, hh = (u1 - u0) * crop_w * grow_u / 2, (v1 - v0) * crop_h * grow_v / 2
    return (cu - hw, cv - hh, cu + hw, cv + hh)


def number_cell_st(n):
    """_BaseMap_ST the facade sets (MaterialPropertyBlock) to show number n on
    a quad that ships pointing at cell 00: a pure offset, no scale."""
    return (1.0, 1.0, (n % NUM_COLS) / NUM_COLS, -(n // NUM_COLS) / NUM_ROWS)


def exit_face_rect(U0, V0, U1, V1, tile=(0.36, 0.18)):
    """uv_rect that shows texture region (U0..U1, V0..V1) of Run_ExitSign_A
    through that material's own transform: _TileSize (0.36, 0.18) and
    _BaseMap_ST (-1, 1, 1, 0) (Run_ExitSign.mat:29-31, 70;
    FrontRoomsSurface.shader:183: uv = metres / tile * ST.xy + ST.zw)."""
    tx, ty = tile
    return (tx * (1 - U0), ty * V0, tx * (1 - U1), ty * V1)


# ------------------------------------------------------------ attributes
WEAR = "fr_wear"


def paint_wear(obj, fn=None):
    """Write the fr_wear POINT colour (white = clean). fn(world_pos) -> RGBA."""
    mesh = obj.data
    attr = mesh.color_attributes.get(WEAR)
    if attr is None:
        attr = mesh.color_attributes.new(WEAR, "BYTE_COLOR", "POINT")
    mw = obj.matrix_basis
    for i, v in enumerate(mesh.vertices):
        attr.data[i].color = (1.0, 1.0, 1.0, 1.0) if fn is None else tuple(fn(mw @ v.co))
    try:
        mesh.color_attributes.active_color = attr
        mesh.color_attributes.render_color_index = list(mesh.color_attributes).index(attr)
    except Exception:
        pass
    return obj


def wear_all(kit, fn=None):
    """Give every part the attribute (parts painted earlier keep theirs)."""
    for obj in kit.parts:
        if obj.data.color_attributes.get(WEAR) is None:
            paint_wear(obj, fn)


def drop2(obj):
    obj["fr_lod2_drop"] = True
    return obj


def lod_meta(kit, dists, budget):
    """§9.3 columns: LOD dist (d01, d12, dcull or None) and Tris (L0, L1, L2 or None)."""
    l0, l1, l2 = budget
    kit.meta["lodDistances"] = list(dists)
    kit.meta["lodBudget"] = [l0, l1, l2]
    kit.meta["lodRatios"] = [round(l1 / l0, 4), (round(l2 / l0, 4) if l2 else None)]


def eval_bounds_unity(kit, parts=None):
    """(min, max) of the evaluated parts in Unity part space."""
    dg = bpy.context.evaluated_depsgraph_get()
    bpy.context.view_layer.update()
    lo = [1e9] * 3
    hi = [-1e9] * 3
    for obj in parts or kit.parts:
        ev = obj.evaluated_get(dg)
        me = ev.to_mesh()
        mw = obj.matrix_world
        for v in me.vertices:
            p = mw @ v.co
            q = (-p.x, p.z, -p.y)
            for i in range(3):
                lo[i] = min(lo[i], q[i])
                hi[i] = max(hi[i], q[i])
        ev.to_mesh_clear()
    return lo, hi


def check_budget(kit, target, tol=0.15):
    """Triangle count of the parts as they will be joined (modifiers applied)."""
    dg = bpy.context.evaluated_depsgraph_get()
    tris = 0
    for obj in kit.parts:
        ev = obj.evaluated_get(dg)
        me = ev.to_mesh()
        tris += sum(len(p.vertices) - 2 for p in me.polygons)
        ev.to_mesh_clear()
    lo, hi = target * (1 - tol), target * (1 + tol)
    assert lo <= tris <= hi, "%s: %d tris, budget %d +/- %d%%" % (kit.name, tris, target, int(tol * 100))
    kit.meta["trianglesCheck"] = tris
    return tris


# ---------------------------------------------------------- mesh builders
def new_obj(kit, bm, slot, name, uv="metres"):
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return kit._new_object(name, bm, slot, uv, "xz")


def _left_normals(poly):
    """Unit left normals at each vertex of a 2D polygon (miter, clamped)."""
    n = len(poly)
    out = []
    for i in range(n):
        p0, p1, p2 = Vector(poly[i - 1]), Vector(poly[i]), Vector(poly[(i + 1) % n])
        e0 = (p1 - p0).normalized()
        e1 = (p2 - p1).normalized()
        n0 = Vector((-e0.y, e0.x))
        n1 = Vector((-e1.y, e1.x))
        m = n0 + n1
        if m.length < 1e-9:
            m = n1
        m.normalize()
        cos_half = max(m.dot(n1), 0.35)
        out.append(m / cos_half)
    return out


def offset_poly(poly, d):
    """Move every vertex d along its left normal (CCW poly: d > 0 = inward)."""
    ns = _left_normals(poly)
    return [(p[0] + n.x * d, p[1] + n.y * d) for p, n in zip(poly, ns)]


def poly_area(poly):
    return 0.5 * sum(poly[i][0] * poly[(i + 1) % len(poly)][1] - poly[(i + 1) % len(poly)][0] * poly[i][1] for i in range(len(poly)))


def ccw(poly):
    return poly if poly_area(poly) > 0 else list(reversed(poly))


def circle(cx, cy, r, n, start=0.0):
    return [(cx + r * math.cos(start + 2 * math.pi * i / n), cy + r * math.sin(start + 2 * math.pi * i / n)) for i in range(n)]


def arc(cx, cy, r, a0, a1, n):
    """n+1 points from angle a0 to a1 (degrees) inclusive."""
    return [(cx + r * math.cos(math.radians(a0 + (a1 - a0) * i / n)), cy + r * math.sin(math.radians(a0 + (a1 - a0) * i / n))) for i in range(n + 1)]


def rounded_rect(w, h, r, seg, cx=0.0, cy=0.0):
    """CCW rounded rectangle centred at (cx, cy)."""
    hw, hh = w / 2, h / 2
    pts = []
    for (x, y, a0) in ((hw - r, -hh + r, -90), (hw - r, hh - r, 0), (-hw + r, hh - r, 90), (-hw + r, -hh + r, 180)):
        pts += arc(cx + x, cy + y, r, a0, a0 + 90, seg)
    return dedupe(pts)


def dedupe(pts, eps=1e-9):
    out = []
    for p in pts:
        if not out or (abs(p[0] - out[-1][0]) > eps or abs(p[1] - out[-1][1]) > eps):
            out.append(p)
    if len(out) > 1 and abs(out[0][0] - out[-1][0]) < eps and abs(out[0][1] - out[-1][1]) < eps:
        out.pop()
    return out


def slab(kit, outline, thickness, slot, to3, round_r=0.0004, round_segs=3, holes=(), pockets=(),
         support=0.00015, name="slab", uv="metres", back=True, back_round=True):
    """A plate cut from a 2D outline with rounded perimeter edges.

    outline: CCW (u, v) points. to3(u, v, w) -> Blender point; w is the
    thickness axis, w = +t/2 is the FRONT face, -t/2 the back.
    holes: through holes, dicts {"poly": CCW points, "chamfer": c, "segs": n}.
    pockets: front-face recesses, dicts {"poly": CCW points (visible opening),
    "depth": d, "chamfer": c, "lip": (lip_thickness, undercut)} - an
    undercut lip retains an insert under the opening edge.
    Support rings (``support`` in from every rounded edge) keep the flat
    faces shading flat under smooth normals.
    back=False leaves the back face open (it lies on something); back_round=False
    keeps the back perimeter edge square (one ring)."""
    outline = ccw(outline)
    t2 = thickness / 2
    bm = bmesh.new()

    def ring(poly, w):
        return [bm.verts.new(to3(u, v, w)) for u, v in poly]

    def bridge(a, b):
        n = len(a)
        for i in range(n):
            j = (i + 1) % n
            bm.faces.new((a[i], a[j], b[j], b[i]))

    # Perimeter rings, front support -> front round -> side -> back round -> back support.
    r = min(round_r, t2 * 0.95)
    angles = [90.0 * i / round_segs for i in range(round_segs + 1)] if r > 0 else [90.0]
    front_rings, back_rings = [], []
    if r > 0:
        front_rings.append(ring(offset_poly(outline, r + support), t2))
    for a in angles:
        th = math.radians(a)
        inset = r * (1 - math.sin(th))
        w = t2 - r + r * math.cos(th)
        front_rings.append(ring(offset_poly(outline, inset), w))
    if back_round:
        for a in reversed(angles):
            th = math.radians(a)
            inset = r * (1 - math.sin(th))
            w = -(t2 - r + r * math.cos(th))
            back_rings.append(ring(offset_poly(outline, inset), w))
        if r > 0:
            back_rings.append(ring(offset_poly(outline, r + support), -t2))
    else:
        back_rings.append(ring(outline, -t2))
    rings = front_rings + back_rings
    for a, b in zip(rings, rings[1:]):
        bridge(a, b)
    front_loops = [front_rings[0]]
    back_loops = [back_rings[-1]]

    for hdef in holes:
        poly = ccw(hdef["poly"])
        c = hdef.get("chamfer", 0.0003)
        hr = [ring(offset_poly(poly, -(c + support)), t2), ring(offset_poly(poly, -c), t2),
              ring(poly, t2 - c), ring(poly, -(t2 - c)), ring(offset_poly(poly, -c), -t2),
              ring(offset_poly(poly, -(c + support)), -t2)]
        for a, b in zip(hr, hr[1:]):
            bridge(b, a)
        front_loops.append(hr[0])
        back_loops.append(hr[-1])

    for pdef in pockets:
        poly = ccw(pdef["poly"])
        c = pdef.get("chamfer", 0.00015)
        depth = pdef["depth"]
        lip_t, under = pdef.get("lip", (0.0, 0.0))
        pr = [ring(offset_poly(poly, -(c + support)), t2), ring(offset_poly(poly, -c), t2), ring(poly, t2 - c)]
        if lip_t > 0 and under > 0:
            pr.append(ring(poly, t2 - lip_t))
            pr.append(ring(offset_poly(poly, -under), t2 - lip_t))
            pr.append(ring(offset_poly(poly, -under), t2 - depth))
        else:
            pr.append(ring(poly, t2 - depth))
        for a, b in zip(pr, pr[1:]):
            bridge(b, a)
        bottom = pr[-1]
        edges = [bm.edges.get((bottom[i], bottom[(i + 1) % len(bottom)])) for i in range(len(bottom))]
        bmesh.ops.triangle_fill(bm, use_beauty=True, use_dissolve=False, edges=edges)
        front_loops.append(pr[0])

    def fill(loops):
        edges = []
        for lp in loops:
            for i in range(len(lp)):
                e = bm.edges.get((lp[i], lp[(i + 1) % len(lp)]))
                edges.append(e)
        bmesh.ops.triangle_fill(bm, use_beauty=True, use_dissolve=False, edges=edges)

    fill(front_loops)
    if back:
        fill(back_loops)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-9)
    return new_obj(kit, bm, slot, name, uv)


def sweep(kit, pts, radii, slot, verts=8, name="wire", cap_start=True, cap_end=True, uv="metres"):
    """Round wire along a 3D polyline (Blender space) with a radius per point
    (radius 0 = a closed point: domed ends). Parallel-transport frames, so
    the rings do not twist."""
    P = [Vector(p) for p in pts]
    T = []
    for k in range(len(P)):
        if k == 0:
            t = P[1] - P[0]
        elif k == len(P) - 1:
            t = P[-1] - P[-2]
        else:
            t = (P[k] - P[k - 1]).normalized() + (P[k + 1] - P[k]).normalized()
        T.append(t.normalized())
    ref = Vector((0, 0, 1)) if abs(T[0].z) < 0.9 else Vector((1, 0, 0))
    side = T[0].cross(ref).normalized()
    bm = bmesh.new()
    rings = []
    for k, p in enumerate(P):
        if k > 0:
            side = side - T[k] * side.dot(T[k])
            side.normalize()
        up = side.cross(T[k]).normalized()
        r = radii[k]
        if r <= 1e-7:
            rings.append([bm.verts.new(p)])
            continue
        rings.append([bm.verts.new(p + (side * math.cos(2 * math.pi * i / verts) + up * math.sin(2 * math.pi * i / verts)) * r)
                      for i in range(verts)])
    for a, b in zip(rings, rings[1:]):
        if len(a) == 1 and len(b) == 1:
            continue
        if len(a) == 1:
            for i in range(verts):
                bm.faces.new((a[0], b[i], b[(i + 1) % verts]))
        elif len(b) == 1:
            for i in range(verts):
                bm.faces.new((a[i], a[(i + 1) % verts], b[0]))
        else:
            for i in range(verts):
                j = (i + 1) % verts
                bm.faces.new((a[i], a[j], b[j], b[i]))
    if cap_start and len(rings[0]) > 1:
        bm.faces.new(list(reversed(rings[0])))
    if cap_end and len(rings[-1]) > 1:
        bm.faces.new(rings[-1])
    return new_obj(kit, bm, slot, name, uv)


def boolean_cut(obj, cutters):
    """Apply EXACT boolean differences now and delete the cutters."""
    for i, c in enumerate(cutters):
        mod = obj.modifiers.new("cut%d" % i, "BOOLEAN")
        mod.operation = "DIFFERENCE"
        mod.solver = "EXACT"
        mod.object = c
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.ops.object.modifier_apply(modifier=mod.name)
    for c in cutters:
        bpy.data.objects.remove(c, do_unlink=True)
    return obj


def prism(points3, axis_vec, length, name="cutter"):
    """Throwaway cutter: a closed polygon (Blender points, roughly planar)
    extruded symmetrically along axis_vec by length. Not a kit part."""
    a = Vector(axis_vec).normalized() * (length / 2)
    bm = bmesh.new()
    f = [bm.verts.new(Vector(p) - a) for p in points3]
    b = [bm.verts.new(Vector(p) + a) for p in points3]
    bm.faces.new(f)
    bm.faces.new(list(reversed(b)))
    n = len(f)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((f[i], f[j], b[j], b[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    obj.hide_render = True
    return obj


def oval_screw(kit, centre, slot, axis="+Z", head_d=0.0055, dome_h=0.0011, rim_h=0.00025, slot_w=0.0006,
               slot_d=0.0007, slot_deg=0.0, segs=16, name="screw"):
    """Slotted oval-head screw standing on a surface. centre = Unity point on
    the surface; axis = the surface normal in Unity part space (+Z, -Z, +X,
    -X, +Y). The slot is real geometry (EXACT boolean), so it reads at 0.3 m."""
    R = head_d / 2
    prof = [(R, 0.0), (R, rim_h), (R * 0.86, rim_h + dome_h * 0.45), (R * 0.58, rim_h + dome_h * 0.82),
            (R * 0.28, rim_h + dome_h * 0.97), (0.0, rim_h + dome_h)]
    obj = kit.lathe(prof, (0, 0, 0), slot, verts=segs, name=name)
    top = rim_h + dome_h
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=Vector((R * 2.6, slot_w, slot_d * 2)), verts=bm.verts)
    bmesh.ops.translate(bm, vec=Vector((0, 0, top)), verts=bm.verts)
    me = bpy.data.meshes.new("slotcut")
    bm.to_mesh(me)
    bm.free()
    cut = bpy.data.objects.new("slotcut", me)
    bpy.context.scene.collection.objects.link(cut)
    cut.hide_render = True
    cut.rotation_euler = (0, 0, math.radians(slot_deg))
    bpy.context.view_layer.update()
    boolean_cut(obj, [cut])
    # Orient: lathe axis (Blender +z) -> the Unity normal.
    to_blender = {"+Z": Vector((0, -1, 0)), "-Z": Vector((0, 1, 0)), "+X": Vector((-1, 0, 0)),
                  "-X": Vector((1, 0, 0)), "+Y": Vector((0, 0, 1)), "-Y": Vector((0, 0, -1))}[axis]
    q = Vector((0, 0, 1)).rotation_difference(to_blender)
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = q
    obj.location = U(*centre)
    return obj


def cup_hook(kit, base, slot, wire_r=0.0015, leg=0.012, bend_r=0.0055, rise=0.004, shoulder_d=0.0065,
             shoulder_t=0.0015, verts=10, name="cup hook", rest_dz=-0.0012, shoulder_verts=20, rivet=False):
    """Screw-in brass cup hook on a surface facing Unity +Z.

    base = Unity point where the shank enters the surface (the shank axis).
    The wire leaves the shoulder along +Z for ``leg``, then bends up through
    a J of centreline radius ``bend_r`` and rises ``rise`` above the bend
    centre to a domed tip. Returns (objects, hook_point) where hook_point is
    the ring's resting point: on top of the wire, rest_dz before the bend
    starts, so a 1.8 mm two-coil ring seated there clears the J (Unity). The shank and thread stay inside the surface (never seen)."""
    bx, by, bz = base
    objs = []
    # Shoulder disc (lathe around Blender z, turned to face Unity +Z).
    if rivet:       # a plain riveted collar (cabinet hook strips)
        prof = [(shoulder_d * 0.5, 0.0), (shoulder_d * 0.5, shoulder_t), (wire_r * 1.05, shoulder_t)]
    else:           # the turned shoulder disc of a screw-in cup hook
        prof = [(shoulder_d * 0.5 - 0.0004, 0.0), (shoulder_d * 0.5, 0.0003), (shoulder_d * 0.5, shoulder_t - 0.0004),
                (shoulder_d * 0.5 - 0.0004, shoulder_t), (wire_r * 1.05, shoulder_t)]
    sh = kit.lathe(prof, (0, 0, 0), slot, verts=shoulder_verts, rot=(90, 0, 0), name=name + " shoulder")
    sh.location = U(bx, by, bz)
    objs.append(sh)
    # Wire centreline in Unity (X const), then to Blender.
    z0 = bz + shoulder_t * 0.5
    zl = bz + shoulder_t + leg
    cy = by + bend_r
    path = [(bx, by, z0), (bx, by, zl - bend_r * 0.35)]
    for i in range(1, 9):
        a = math.radians(-90 + 90 * i / 8)
        path.append((bx, cy + bend_r * math.sin(a), zl + bend_r * math.cos(a)))
    # Straight rise, slightly leaning back toward the wall at the tip.
    tipx = (bx, cy + rise, zl + bend_r - 0.0006)
    path.append(((path[-1][0] + tipx[0]) / 2, (path[-1][1] + tipx[1]) / 2, (path[-1][2] + tipx[2]) / 2))
    path.append(tipx)
    # Domed tip: three shrinking rings along the last direction.
    d = Vector(tipx) - Vector(path[-2])
    d.normalize()
    radii = [wire_r] * len(path)
    for f, rr in ((0.45, 0.88), (0.8, 0.55), (1.0, 0.0)):
        p = Vector(tipx) + d * wire_r * f
        path.append(tuple(p))
        radii.append(wire_r * rr)
    w = sweep(kit, [U(*p) for p in path], radii, slot, verts=verts, name=name + " wire", cap_start=True, cap_end=False)
    objs.append(w)
    hook_point = (bx, by + wire_r, zl + rest_dz)
    return objs, hook_point


def profile_sweep(kit, path, profile, slot, to3, name="moulding", cap_first=False, cap_last=False, uv="metres"):
    """Sweep a section round a closed CCW path (u, v): profile = [(inset, w)]
    pairs (inset > 0 = toward the inside of the path, w = height). One ring
    per profile point, bridged in order: frames, folded edges, housings."""
    path = ccw(path)
    return _bridge_rings(kit, [(offset_poly(path, inset), w) for inset, w in profile], slot, to3, name,
                         cap_first, cap_last, uv)


def rrect_sweep(kit, w, h, r, seg, cx, cy, profile, slot, to3, name="moulding", cap_first=False, cap_last=False,
                uv="metres", r_min=0.0003):
    """profile_sweep for a rounded rectangle whose profile reaches further in
    than the corner radius (folded sheet lips, pan doors). offset_poly moves
    each arc vertex along its own normal, so an inset > r folds the corner
    arc through its centre and the corner faces turn inside out (black
    corner notches). Here every ring is rebuilt as the exact offset curve:
    a rounded rectangle (w - 2 inset) x (h - 2 inset) with radius r - inset,
    never below r_min (the inside bend radius of the fold). Same vertex count
    on every ring, so the bridging is unchanged."""
    polys = []
    for inset, ww in profile:
        rr = max(r - inset, r_min)
        polys.append((rounded_rect(w - 2 * inset, h - 2 * inset, rr, seg, cx, cy), ww))
    n0 = len(polys[0][0])
    assert all(len(p) == n0 for p, _ in polys), "rrect_sweep: ring sizes differ"
    return _bridge_rings(kit, polys, slot, to3, name, cap_first, cap_last, uv)


def _bridge_rings(kit, polys, slot, to3, name, cap_first, cap_last, uv):
    bm = bmesh.new()
    rings = [[bm.verts.new(to3(u, v, w)) for u, v in poly] for poly, w in polys]
    n = len(polys[0][0])
    for a, b in zip(rings, rings[1:]):
        for i in range(n):
            j = (i + 1) % n
            bm.faces.new((a[i], a[j], b[j], b[i]))
    for flag, rg in ((cap_first, rings[0]), (cap_last, rings[-1])):
        if flag:
            edges = [bm.edges.get((rg[i], rg[(i + 1) % n])) for i in range(n)]
            bmesh.ops.triangle_fill(bm, use_beauty=True, use_dissolve=False, edges=edges)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-9)
    return new_obj(kit, bm, slot, name, uv)


def quad_uv(kit, corners_U, uvs, slot, name="decal"):
    """One quad from four Unity points (CCW seen from its front) with explicit
    UVs (u, v) per corner; fr_uv = "keep" so finish() leaves them alone."""
    bm = bmesh.new()
    vs = [bm.verts.new(U(*p)) for p in corners_U]
    f = bm.faces.new(vs)
    layer = bm.loops.layers.uv.verify()
    for loop, uv in zip(f.loops, uvs):
        loop[layer].uv = uv
    obj = kit._new_object(name, bm, slot, "keep", "xz")
    return obj


def disc_uv(kit, centre_U, radius, rect, slot, segs=32, name="disc"):
    """Flat disc facing Unity +Z with decal UVs squeezed into rect."""
    cx, cy, cz = centre_U
    u0, v0, u1, v1 = rect
    bm = bmesh.new()
    vs = []
    uvs = []
    for i in range(segs):
        a = 2 * math.pi * i / segs
        x, y = math.cos(a), math.sin(a)
        vs.append(bm.verts.new(U(cx + radius * x, cy + radius * y, cz)))
        uvs.append((u0 + (u1 - u0) * (0.5 - x * 0.5), v0 + (v1 - v0) * (y * 0.5 + 0.5)))   # u toward Unity -X (viewer's right)
    f = bm.faces.new(vs)
    layer = bm.loops.layers.uv.verify()
    for loop, uv in zip(f.loops, uvs):
        loop[layer].uv = uv
    f.normal_update()
    if f.normal.y > 0:          # must face Blender -Y (= Unity +Z)
        f.normal_flip()
    return kit._new_object(name, bm, slot, "keep", "xz")


# ------------------------------------------------------------ key tags
TAG_T = 0.0042
TAG_HOLE_D = 0.005
TAB_R = 0.0055
TAB_T = 0.0024                       # moulded ring tab, thinner than the body
TAB_ROOT = 0.003                     # the tab roots 3 mm into the body
TAG_EDGE = 0.0006
WINDOW_DEPTH = 0.0008
WINDOW_LIP = (0.0003, 0.0006)        # lip thickness, undercut
INSERT_DEPTH = 0.00075               # insert face below the tag face


def build_tag(kit, body, window, crop, body_slot="Prop_PlasticRed", seg=5, hole_segs=16, window_segs=24, body_segs=40):
    """Plastic key tag with a paper insert (era note R10). Unity part frame:
    origin = ring-hole centre (the swing pivot), hangs along -Y, front +Z,
    mid-plane Z = 0. body = ("rect", W, H, R, y_top) or ("round", D, y_top).
    window = ("rect", w, h, r, centre_y) or ("round", d, centre_y): the
    visible opening. crop = (crop_w, crop_h) of an atlas cell.
    The ring tab is moulded thinner than the body (TAB_T 2.4 mm against 4.2):
    that is what lets a tag turn about 30 deg on a 1.6 x 1.8 mm split-ring wire
    and hang face-on to the room (measured in the G3 hung-pose check)."""
    register_slots()
    to3 = lambda u, v, w: U(u, v, w)
    if body[0] == "rect":
        _, W, H, R, y_top = body
        outline = rounded_rect(W, H, R, seg, 0.0, y_top - H / 2)
    else:
        _, D, y_top = body
        outline = circle(0.0, y_top - D / 2, D / 2, body_segs, start=math.pi / 2)
    if window[0] == "rect":
        _, ww, wh, wr, wy = window
        wpoly = rounded_rect(ww, wh, wr, 3, 0.0, wy)
    else:
        _, wd, wy = window
        wpoly = circle(0.0, wy, wd / 2, window_segs)
    body_obj = slab(kit, outline, TAG_T, body_slot, to3, round_r=TAG_EDGE, round_segs=2,
                    pockets=[{"poly": wpoly, "depth": WINDOW_DEPTH, "chamfer": 0.00012, "lip": WINDOW_LIP}],
                    name="tag body")
    # Ring tab: a 2.4 mm stadium round the hole, rooted 3 mm into the body.
    tab = [(TAB_R, y_top - TAB_ROOT)] + arc(0.0, 0.0, TAB_R, 0.0, 180.0, 14) + [(-TAB_R, y_top - TAB_ROOT)]
    hole = circle(0.0, 0.0, TAG_HOLE_D / 2, hole_segs)
    tab_obj = slab(kit, tab, TAB_T, body_slot, to3, round_r=0.0005, round_segs=2,
                   holes=[{"poly": hole, "chamfer": 0.0003}], name="tag tab")
    # Insert: reaches under the lip (hidden), visible window on the crop.
    under = WINDOW_LIP[1]
    zi = TAG_T / 2 - INSERT_DEPTH
    if window[0] == "rect":
        qw, qh = ww + 2 * under, wh + 2 * under
        rect = number_cell_rect(NUMBER_DEFAULT_CELL, crop[0], crop[1], qw / ww, qh / wh)
        u0, v0, u1, v1 = rect
        corners = [(-qw / 2, wy - qh / 2, zi), (qw / 2, wy - qh / 2, zi), (qw / 2, wy + qh / 2, zi), (-qw / 2, wy + qh / 2, zi)]
        # Unity +X is the viewer's LEFT when facing the tag (front +Z), so u runs toward -X.
        ins = quad_uv(kit, [(-c[0], c[1], c[2]) for c in corners], [(u0, v0), (u1, v0), (u1, v1), (u0, v1)],
                      KEYTAGNO, name="tag insert")
    else:
        qd = wd + 2 * under
        rect = number_cell_rect(NUMBER_DEFAULT_CELL, crop[0], crop[1], qd / wd, qd / wd)
        ins = disc_uv(kit, (0.0, wy, zi), qd / 2, rect, KEYTAGNO, segs=window_segs, name="tag insert")
    def grime(p):
        Y = p.z
        return (1.0, 0.92 if Y < -0.02 else 1.0, 1.0, 1.0)
    paint_wear(body_obj, grime)
    paint_wear(tab_obj)
    paint_wear(ins)
    kit.anchor("hole", U(0, 0, 0))
    kit.anchor("hole_dir", U(0, 0, 0.10))
    kit.anchor("face", U(0, wy, zi))
    kit.anchor("number", U(0, wy, zi))
    kit.anchor("face_dir", U(0, wy, zi + 0.10))
    kit.no_collider()
    kit.tag("interactable", "key", "key_tag")
    kit.meta["numberAtlas"] = {"slot": KEYTAGNO, "fallback": KEYTAGNO_FALLBACK, "cols": NUM_COLS, "rows": NUM_ROWS,
                               "defaultCell": NUMBER_DEFAULT_CELL, "crop": list(crop),
                               "setCell": "_BaseMap_ST = (1, 1, (n % 10) / 10, -(n // 10) / 10)"}
    return body_obj, ins


def orient(obj, origin_U, axis_U, up_U=(0.0, 1.0, 0.0)):
    """Place a part built around Blender +z (lathes, cylinders) so its local
    +z runs along the Unity direction axis_U, its local +y toward up_U, with
    its origin at the Unity point origin_U."""
    z = Vector(U(*axis_U)).normalized()
    y = Vector(U(*up_U))
    y = (y - z * y.dot(z))
    if y.length < 1e-6:
        y = Vector((1, 0, 0)) - z * z.x
    y.normalize()
    x = y.cross(z)
    o = Vector(U(*origin_U))
    obj.matrix_basis = Matrix(((x.x, y.x, z.x, o.x), (x.y, y.y, z.y, o.y), (x.z, y.z, z.z, o.z), (0, 0, 0, 1)))
    return obj
