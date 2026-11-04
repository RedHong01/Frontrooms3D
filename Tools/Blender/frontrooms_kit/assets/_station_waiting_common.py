"""Shared helpers for the BS4 hero Waiting kits (Station_Waiting_3x2): tandem beam seating, smoking stand and
its sign, magazine rack (游戏视觉, workflow bs4-hero-kits, builder A; research/station/72_hero_kits.md §2.1-§2.3,
§3, §6).

NOT AN ASSET: no NAME and no build(). tandem_beam.py, tandem_beam_table.py, smoking_stand.py,
sign_no_smoking.py and magazine_rack.py import it.

Authoring is in UNITY space (y up, front +Z into the room) and converted on the way in with
_station_common.B (Unity (X, Y, Z) = Blender (-X, -Z, Y)). The generators here build each part from rings of
Unity points (lathes, sweeps, upholstery lofts), so a part's tessellation is chosen per level of detail:

* hand-built LODs (kitlib P-1b): every part carries obj["fr_lods"]; LOD1 keeps the silhouette with fewer sides
  and no hardware under 5 cm, LOD2 is boxes and 6-sided tubes (72 §2; never a collapse of hard surfaces);
* the G1 per-part recipe (interact_door_common.finalize, outlet_common.finish_part): own modifiers applied,
  n-gons triangulated, doubles removed, smooth, sharp by angle, then a WEIGHTED_NORMAL (face area, weight 50,
  keep sharp) that kitlib.finish applies. The kits use ONE angle for the parts and for kitlib.finish
  (SMOOTH = 50: two-segment bevels and 8-12-sided wire shade round, hex flats and 90-degree creases stay sharp),
  which is what the G1 probe proved survives the join and the FBX round trip.

Copied, not imported (72 §6): task_chair._rrect's idea (fixed point count per corner) as rr_outline(), and
_fillet_path as fillet_path(). New slots (72 §3) are registered with their .mat colour (sRGB -> linear).
"""

import math

import bmesh
import bpy
from mathutils import Matrix, Vector

import kitlib
import _station_common as sc

LINE = "游戏视觉 bs4-hero-kits"
SMOOTH = 50.0

STEEL = "Prop_SteelBlack"
PLASTIC = "Prop_PlasticBlack"
CHROME = "Prop_Chrome"
OAK = "Prop_WoodOak"
PAPER = "Prop_Paper"
CORK = sc.CORK
SLATE = "Prop_VinylSlate"
NAVY = "Prop_VinylNavy"
CHARCOAL = "Prop_VinylCharcoal"
ASH = "Prop_AshSand"
SIGN = "Prop_SignNoSmoking"
COVER = "Prop_MagazineCover"

# Slot -> (.mat _BaseColor sRGB hex, preview roughness). 72 §3.
NEW_SLOTS = {
    SLATE: ("566679", 0.60),
    NAVY: ("26324A", 0.60),
    CHARCOAL: ("3C3F44", 0.60),
    ASH: ("8C877C", 0.92),
    SIGN: ("2B2925", 0.65),
    COVER: ("FFFFFF", 0.40),
}


def srgb_to_linear(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def hex_linear(h):
    return tuple(srgb_to_linear(int(h[i:i + 2], 16) / 255.0) for i in (0, 2, 4))


def register():
    sc.register()
    for slot, (hx, rough) in NEW_SLOTS.items():
        kitlib.register_slot(slot, hex_linear(hx), rough, 0.0)


# ------------------------------------------------------------------ space
B = sc.B
BS = sc.BS


def V(*p):
    return Vector(p if len(p) == 3 else p[0])


def frame_axes(x_axis, y_axis):
    """Orthonormal frame from a local x and an approximate local y (Unity vectors)."""
    x = Vector(x_axis).normalized()
    y = Vector(y_axis)
    y = (y - x * y.dot(x)).normalized()
    z = x.cross(y).normalized()
    return x, y, z


class Frame:
    """Local -> Unity world: origin + axes (each a Unity vector)."""

    def __init__(self, origin=(0, 0, 0), x=(1, 0, 0), y=(0, 1, 0), z=None):
        self.o = Vector(origin)
        self.x = Vector(x).normalized()
        self.y = Vector(y).normalized()
        self.z = Vector(z).normalized() if z is not None else self.x.cross(self.y).normalized()

    def p(self, lx, ly, lz):
        return self.o + self.x * lx + self.y * ly + self.z * lz

    def d(self, lx, ly, lz):
        return self.x * lx + self.y * ly + self.z * lz


def tilt_x(deg):
    """Frame axes rotated about Unity +X by deg (positive tips local +Y toward -Z: a back reclining)."""
    a = math.radians(deg)
    return (1, 0, 0), (0, math.cos(a), -math.sin(a)), (0, math.sin(a), math.cos(a))


# ------------------------------------------------------------------ parts
def _link(kit, name, bm, slot, uv="metres"):
    obj = kit._new_object(name, bm, slot, uv, "xz")
    return obj


def part_from_rings(kit, name, slot, rings, closed=True, cap_start=None, cap_end=None, recalc=True, uv="metres"):
    """A solid from rings of Unity points (all rings the same length). closed: each ring is a loop.
    cap_start / cap_end: None, "fan" (centre vertex) or "ngon". A ring of one point is a pole."""
    bm = bmesh.new()
    vr = []
    for ring in rings:
        vr.append([bm.verts.new(B(*p)) for p in ring])
    for a, b in zip(vr, vr[1:]):
        if len(a) == 1 and len(b) == 1:
            continue
        if len(a) == 1 or len(b) == 1:
            pole, ring = (a[0], b) if len(a) == 1 else (b[0], a)
            n = len(ring)
            m = n if closed else n - 1
            for i in range(m):
                j = (i + 1) % n
                try:
                    bm.faces.new((ring[i], ring[j], pole))
                except ValueError:
                    pass
            continue
        n = len(a)
        m = n if closed else n - 1
        for i in range(m):
            j = (i + 1) % n
            try:
                bm.faces.new((a[i], a[j], b[j], b[i]))
            except ValueError:
                pass
    for ring, kind in ((vr[0], cap_start), (vr[-1], cap_end)):
        if kind is None or len(ring) < 3:
            continue
        if kind == "ngon":
            try:
                bm.faces.new(ring)
            except ValueError:
                pass
        else:
            c = sum((v.co for v in ring), Vector()) / len(ring)
            cv = bm.verts.new(c)
            n = len(ring)
            for i in range(n):
                try:
                    bm.faces.new((ring[i], ring[(i + 1) % n], cv))
                except ValueError:
                    pass
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-7)
    if recalc:
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return _link(kit, name, bm, slot, uv)


def mesh_part(kit, name, slot, verts_u, faces, uvs=None, outward=None, face_dirs=None):
    """A part from explicit Unity vertices and faces. uvs (one (u, v) per face corner, in face order) keeps
    authored UVs (uv="keep", one UV layer by verify(), as _run_b_common.mesh_part). face_dirs: one Unity
    direction per face that its normal must face (thin open shells: a magazine cover, whose curled corner fooled
    the centre test and rendered black); else outward: a Unity point the normals face away from; else recalc."""
    bm = bmesh.new()
    bv = [bm.verts.new(B(*p)) for p in verts_u]
    layer = bm.loops.layers.uv.verify() if uvs is not None else None
    k = 0
    made = []
    for fi, f in enumerate(faces):
        try:
            face = bm.faces.new([bv[i] for i in f])
        except ValueError:
            k += len(f)
            continue
        made.append((face, fi))
        if layer is not None:
            for loop in face.loops:
                loop[layer].uv = uvs[k]
                k += 1
    bm.normal_update()
    if face_dirs is not None:
        for face, fi in made:
            if face.normal.dot(Vector(B(*face_dirs[fi]))) < 0:
                face.normal_flip()
    elif outward is None:
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    else:
        o = Vector(B(*outward))
        for face in bm.faces:
            if face.normal.dot(face.calc_center_median() - o) < 0:
                face.normal_flip()
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(obj)
    obj.data.materials.append(kit._material(slot))
    if uvs is not None:
        return kit.adopt(obj, [slot], uv="keep")
    obj["fr_uv"] = "metres"
    obj["fr_decal_axes"] = "xz"
    kit.parts.append(obj)
    return obj


def box(kit, size, centre, slot, bevel=0.0012, segments=2, name="box"):
    """Axis box in Unity space (kitlib.box: every edge bevelled, miter arcs at the corners)."""
    return kit.box(BS(*size), B(*centre), slot, bevel=bevel, segments=segments, name=name)


# ------------------------------------------------------------------ outlines and profiles
def circle(r, n, phase=0.0):
    return [(r * math.cos(phase + 2 * math.pi * i / n), r * math.sin(phase + 2 * math.pi * i / n)) for i in range(n)]


def rr_profile(w, h, r, steps):
    """Rounded rectangle (u, v) centred on 0, CCW; ``steps`` segments per corner (0 = sharp)."""
    hw, hh = w / 2.0, h / 2.0
    r = min(r, hw * 0.999, hh * 0.999)
    out = []
    for cx, cy, a0 in ((hw - r, hh - r, 0.0), (-hw + r, hh - r, 90.0), (-hw + r, -hh + r, 180.0), (hw - r, -hh + r, 270.0)):
        if steps <= 0 or r <= 0:
            out.append((cx + math.copysign(r, cx), cy + math.copysign(r, cy)))
            continue
        for k in range(steps + 1):
            a = math.radians(a0 + 90.0 * k / steps)
            out.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return out


def rr_outline(hw, hd, radii, steps, fracs):
    """Rounded-rect outline in a local (x, z) plane, CCW seen from +y, with a FIXED point layout so rings of any
    size loft together (task_chair._rrect's rule). radii / steps: per corner in the order (+x +z, -x +z, -x -z,
    +x -z). fracs: per side, in the order (+z edge, -x edge, -z edge, +x edge), the fractions (0..1) of extra
    points along that straight side. Returns [(x, z, nx, nz)] with the outward normal."""
    radii = [min(max(r, 0.0005), hw * 0.999, hd * 0.999) for r in radii]
    corners = [(hw - radii[0], hd - radii[0], 0.0), (-hw + radii[1], hd - radii[1], 90.0),
               (-hw + radii[2], -hd + radii[2], 180.0), (hw - radii[3], -hd + radii[3], 270.0)]

    def sharp(k):
        # steps 0 = a sharp corner: one point on the box corner, its normal on the diagonal
        cx, cz, a0 = corners[k]
        am = math.radians(a0 + 45.0)
        return (cx + math.copysign(radii[k], math.cos(am)), cz + math.copysign(radii[k], math.sin(am)),
                math.cos(am), math.sin(am))
    pts = []
    for k in range(4):
        cx, cz, a0 = corners[k]
        r = radii[k]
        n = steps[k]
        if n <= 0:
            pts.append(sharp(k))
        else:
            for i in range(n + 1):
                a = math.radians(a0 + 90.0 * i / n)
                pts.append((cx + r * math.cos(a), cz + r * math.sin(a), math.cos(a), math.sin(a)))
        # side from this corner's end to the next corner's start
        a_end = math.radians(a0 + 90.0)
        p0 = sharp(k)[:2] if n <= 0 else (cx + r * math.cos(a_end), cz + r * math.sin(a_end))
        nk = (k + 1) % 4
        ncx, ncz, na0 = corners[nk]
        nr = radii[nk]
        a_nxt = math.radians(na0)
        p1 = sharp(nk)[:2] if steps[nk] <= 0 else (ncx + nr * math.cos(a_nxt), ncz + nr * math.sin(a_nxt))
        nrm = (math.cos(a_end), math.sin(a_end))
        for t in fracs[k]:
            pts.append((p0[0] + (p1[0] - p0[0]) * t, p0[1] + (p1[1] - p0[1]) * t, nrm[0], nrm[1]))
    return pts


def fillet_path(a, corner, b, radius, steps=8):
    """2D points from a to b passing round ``corner`` with an arc of ``radius`` (task_chair._fillet_path)."""
    a, c, b = Vector(a), Vector(corner), Vector(b)
    d1 = (a - c).normalized()
    d2 = (b - c).normalized()
    ang = math.acos(max(-1.0, min(1.0, d1.dot(d2))))
    dist = radius / math.tan(ang / 2)
    centre = c + (d1 + d2).normalized() * (radius / math.sin(ang / 2))
    t1, t2 = c + d1 * dist, c + d2 * dist
    a1 = math.atan2(t1.y - centre.y, t1.x - centre.x)
    a2 = math.atan2(t2.y - centre.y, t2.x - centre.x)
    da = a2 - a1
    while da > math.pi:
        da -= 2 * math.pi
    while da < -math.pi:
        da += 2 * math.pi
    pts = [tuple(a)]
    for k in range(steps + 1):
        ak = a1 + da * k / steps
        pts.append((centre.x + radius * math.cos(ak), centre.y + radius * math.sin(ak)))
    pts.append(tuple(b))
    return pts


def bend_steps(deg, step=15.0):
    return max(1, int(math.ceil(abs(deg) / step - 1e-6)))


# ------------------------------------------------------------------ generators
def lathe(kit, name, slot, profile, base=(0, 0, 0), axis=(0, 1, 0), ref=(1, 0, 0), verts=32, angles=None,
          dy=None, cap_start=None, cap_end=None, closed=True):
    """Revolve a (radius, height) profile (bottom to top) about ``axis`` through ``base`` (Unity).
    angles: explicit angle list (radians) instead of ``verts`` evenly spaced. dy(angle, i): an offset along the
    axis per angle and profile index (the bowl rim's cigarette rests). A radius of 0 is a pole."""
    ax = Vector(axis).normalized()
    e1 = Vector(ref)
    e1 = (e1 - ax * e1.dot(ax)).normalized()
    e2 = ax.cross(e1).normalized()
    o = Vector(base)
    angs = angles if angles is not None else [2 * math.pi * i / verts for i in range(verts)]
    rings = []
    for i, (r, h) in enumerate(profile):
        if r < 1e-7:
            rings.append([tuple(o + ax * h)])
            continue
        ring = []
        for a in angs:
            hh = h + (dy(a, i) if dy is not None else 0.0)
            ring.append(tuple(o + ax * hh + (e1 * math.cos(a) + e2 * math.sin(a)) * r))
        rings.append(ring)
    return part_from_rings(kit, name, slot, rings, closed=closed, cap_start=cap_start, cap_end=cap_end)


def sweep(kit, name, slot, path, profile, side=None, closed=False, caps=True, chamfer=0.0, twist_ref=None):
    """Sweep a 2D profile [(s, u)] along a Unity path (mitred joints). ``side``: a fixed Unity vector for the
    profile's s axis (planar bars keep their faces square to the bend plane); else a parallel-transport frame.
    caps: "fan"/"ngon"/True(ngon)/False. chamfer: eases the two ends by a 45-degree ring (bar ends)."""
    pts = [Vector(p) for p in path]
    n = len(pts)
    tans = []
    for k in range(n):
        if closed:
            d = (pts[(k + 1) % n] - pts[k]).normalized() + (pts[k] - pts[k - 1]).normalized()
        elif k == 0:
            d = pts[1] - pts[0]
        elif k == n - 1:
            d = pts[-1] - pts[-2]
        else:
            d = (pts[k + 1] - pts[k]).normalized() + (pts[k] - pts[k - 1]).normalized()
        tans.append(d.normalized())
    frames = []
    if side is not None:
        sv = Vector(side)
        for t in tans:
            s = (sv - t * sv.dot(t)).normalized()
            frames.append((s, t.cross(s).normalized()))
    else:
        ref = Vector(twist_ref) if twist_ref is not None else (Vector((0, 1, 0)) if abs(tans[0].y) < 0.9 else Vector((1, 0, 0)))
        s = (ref - tans[0] * ref.dot(tans[0])).normalized()
        for k, t in enumerate(tans):
            if k > 0:
                s = (s - t * s.dot(t))
                s = s.normalized() if s.length > 1e-9 else frames[-1][0]
            frames.append((s, t.cross(s).normalized()))
    # mitre scale: keep the profile's width at joints
    rings = []
    for k in range(n):
        s, u = frames[k]
        scale = 1.0
        if 0 < k < n - 1 or closed:
            a = (pts[(k + 1) % n] - pts[k]).normalized()
            b = (pts[k] - pts[k - 1]).normalized()
            c = max(0.3, math.cos(0.5 * a.angle(b)) if a.length and b.length else 1.0)
            scale = 1.0 / c
        ring = []
        for (ps, pu) in profile:
            # stretch only along the bend direction (approximation: both axes by the mitre on the bend plane)
            off = s * ps + u * pu
            if scale != 1.0:
                bend_dir = (pts[(k + 1) % n] - pts[k]).normalized() - (pts[k] - pts[k - 1]).normalized()
                if bend_dir.length > 1e-9:
                    bd = bend_dir.normalized()
                    comp = off.dot(bd)
                    off = off + bd * comp * (scale - 1.0)
            ring.append(tuple(pts[k] + off))
        rings.append(ring)
    if chamfer > 0 and not closed:
        def inner(ring_pts, centre, toward):
            return [tuple(centre + (Vector(p) - centre) * 1.0 + toward * 0.0) for p in ring_pts]
        c0 = pts[0]
        c1 = pts[-1]
        # end rings: shrink the profile by the chamfer and push it out along the tangent
        def shrunk(k, sign):
            s, u = frames[k]
            out = []
            for (ps, pu) in profile:
                L = math.hypot(ps, pu)
                f = max(0.0, (L - chamfer) / L) if L > 1e-9 else 0.0
                out.append(tuple(pts[k] + (s * ps + u * pu) * f + tans[k] * (chamfer * sign)))
            return out
        rings = [shrunk(0, -1)] + rings + [shrunk(n - 1, 1)]
    cap = None
    if caps:
        cap = caps if isinstance(caps, str) else "ngon"
    return part_from_rings(kit, name, slot, rings if not closed else rings + [rings[0]], closed=True,
                           cap_start=None if closed else cap, cap_end=None if closed else cap)


def tube(kit, name, slot, path, r, sides, caps=True, side=None, closed=False):
    return sweep(kit, name, slot, path, circle(r, sides, phase=math.pi / sides), side=side, caps=caps, closed=closed)


def bar(kit, name, slot, path, w, t, r=0.0012, steps=2, side=(1, 0, 0), chamfer=0.0008, caps=True):
    """A flat bar (w along ``side``, t across) along a path, its long edges rounded r, its ends eased."""
    return sweep(kit, name, slot, path, rr_profile(w, t, r, steps), side=side, caps=caps, chamfer=chamfer)


def pad(kit, name, slot, frame, hw, hd, radii, steps, fracs, rings, warp=None, cap_top="fan", cap_bottom=None,
        bulge=None):
    """Upholstery or moulded pad lofted through ``rings`` [(inset, height)] (bottom to top) of a rounded-rect
    outline in the frame's local (x, z) plane (height along local +y). bulge(i, x, z, nx, nz) -> extra outward
    offset of ring i at an outline point (welt cords, split seams). warp(x, y, z) -> local point."""
    out = []
    for i, (inset, h) in enumerate(rings):
        rad = [max(r - inset, r * 0.30) for r in radii]
        pts = rr_outline(max(hw - inset, 0.002), max(hd - inset, 0.002), rad, steps, fracs)
        ring = []
        for (x, z, nx, nz) in pts:
            b = bulge(i, x, z, nx, nz) if bulge is not None else 0.0
            lx, ly, lz = x + nx * b, h, z + nz * b
            if warp is not None:
                lx, ly, lz = warp(lx, ly, lz)
            ring.append(tuple(frame.p(lx, ly, lz)))
        out.append(ring)
    return part_from_rings(kit, name, slot, out, closed=True, cap_start=cap_bottom, cap_end=cap_top)


# ------------------------------------------------------------------ hardware
def hex_head(kit, name, slot, base, axis, af, height, ref=(1, 0, 0), chamfer=0.25, washer=None, lv=0):
    """A hex bolt head or nut (A/F ``af``) standing on ``base`` along ``axis``: hex prism, eased at the foot, with
    a chamfered top (the 30-degree cone cut read as a bevel). washer: (radius, thickness) of a round washer face
    under it. The underside is open (it sits on a plate)."""
    rc = af / math.sqrt(3.0)
    prof = []
    h0 = 0.0
    if washer is not None:
        wr, wt = washer
        prof += [(wr, 0.0), (wr, wt)]
        h0 = wt
    c = af * chamfer * 0.35
    prof += [(rc * 0.97, h0), (rc, h0 + c * 0.5), (rc, h0 + height - c), (rc - c * 1.4, h0 + height), (0.0, h0 + height)]
    return lathe(kit, name, slot, prof, base=base, axis=axis, ref=ref, verts=6)


def dome_head(kit, name, slot, base, axis, d, h, sides=8, ref=(1, 0, 0), rings=3):
    """A domed pan / oval / carriage head of diameter d, height h, on ``base`` along ``axis`` (open underside)."""
    r = d / 2.0
    prof = [(r, 0.0), (r * 0.93, h * 0.42), (r * 0.55, h * 0.86), (0.0, h)] if rings >= 3 else [(r, 0.0), (r * 0.72, h * 0.78), (0.0, h)]
    return lathe(kit, name, slot, prof, base=base, axis=axis, ref=ref, verts=sides)


def weld_bead(kit, name, slot, path, r=0.001, sides=5):
    """A fillet weld as a thin bead (its ends die into the parent metal, so no caps)."""
    return tube(kit, name, slot, path, r, sides, caps=False)


# ------------------------------------------------------------------ G1 finish and bookkeeping
def finish_parts(kit, angle=SMOOTH, parts=None):
    """The G1 per-part recipe (interact_door_common.finalize, without the wear paint: the Surface shader ignores
    vertex colour): own modifiers applied, n-gons triangulated, doubles removed, smooth, sharp by ``angle``,
    WEIGHTED_NORMAL (face area, 50, keep sharp) left for kitlib.finish to apply."""
    for obj in list(parts if parts is not None else kit.parts):
        bpy.ops.object.select_all(action="DESELECT")
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        for mod in list(obj.modifiers):
            bpy.ops.object.modifier_apply(modifier=mod.name)
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        big = [f for f in bm.faces if len(f.verts) > 4]
        if big:
            bmesh.ops.triangulate(bm, faces=big, quad_method="BEAUTY", ngon_method="BEAUTY")
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-7)
        bm.to_mesh(obj.data)
        bm.free()
        me = obj.data
        me.shade_smooth()
        me.set_sharp_from_angle(angle=math.radians(angle))
        m = obj.modifiers.new("wn", "WEIGHTED_NORMAL")
        m.mode = "FACE_AREA"
        m.weight = 50
        m.keep_sharp = True


def tag(parts, lods):
    for p in parts:
        p["fr_lods"] = lods
    return parts


class Level:
    """Collects the parts one level-builder call makes and tags them (fr_lods)."""

    def __init__(self, kit, lods):
        self.kit = kit
        self.lods = lods
        self.n0 = len(kit.parts)

    def __enter__(self):
        return self

    def __exit__(self, *exc):
        for p in self.kit.parts[self.n0:]:
            if "fr_lods" not in p.keys():
                p["fr_lods"] = self.lods
        return False


def tris_report(kit, label=""):
    """Evaluated triangle count per level (modifiers included), printed to the log."""
    dg = bpy.context.evaluated_depsgraph_get()
    per = {}
    for p in kit.parts:
        me = p.evaluated_get(dg).to_mesh()
        t = sum(len(f.vertices) - 2 for f in me.polygons)
        p.evaluated_get(dg).to_mesh_clear()
        for c in str(p.get("fr_lods", "0")):
            per.setdefault(c, {})
            key = p.name.split(".")[0]
            per[c][key] = per[c].get(key, 0) + t
    for lv in sorted(per):
        tot = sum(per[lv].values())
        top = ", ".join("%s %d" % kv for kv in sorted(per[lv].items(), key=lambda kv: -kv[1])[:14])
        print("[tris] %s %s LOD%s %d: %s" % (kit.name, label, lv, tot, top))


def station_block(kit, origin, note=""):
    sc.no_pile(kit)
    sc.station_meta(kit, origin, note)
    kit.meta["station"]["line"] = LINE
