"""Builder B helpers for the bs4-hero-kits workflow (game visual line, 2026-11-03): the Desk module's hero kits
Kit_GuestChairSled (+ _Burgundy), Kit_StationCounter_Ledge120 and Kit_Credenza_Laminate, which replace the BS4
stand-ins of Station_Desk_3x3 (research/station/72_hero_kits.md sections 2.4-2.6, 3, 6). Helper module: no NAME,
no build(). It shares no file with builder A (_station_waiting_common.py): the helpers below are copies.

What lives here
* register(): _station_common.register() (Prop_LaminateGrey, Prop_Cork) plus Prop_VinylTeal and Prop_VinylBurgundy.
* Unity-space authoring (x right, y up, z out of the kit's front; kitlib: Unity (X, Y, Z) = Blender (-x, z, -y)),
  through _station_common.B / BS.
* g1(): the G1 hard-surface recipe of outlet_common.finish_part (smooth, sharp by angle, WEIGHTED_NORMAL face area
  50, keep sharp), with the part's bevel baked first so the sharp flags are taken on the bevelled mesh. Pass the
  module's SMOOTH_ANGLE: kitlib.finish() re-marks sharp edges on the join at that angle, so the same angle keeps
  the weighted normals exactly.
* fillet() + tube(): bent tube along a polyline, every corner a circular arc of a given centreline radius sampled
  every 15 degrees, rings carried by rotation-minimising frames (kitlib.tube's fixed reference vector twists the
  rings where a tube turns vertical), open or closed.
* pad(): upholstery pad (rounded-rect rings lofted through a profile, as task_chair._soft_block) whose rings can
  bulge out along the outline normal by a position weight: the welt cord is part of the pad's own surface.
* plate(): a board whose front arrises follow (inset, height) steps (as _newcase.plate, axis-free).
* extrude_rings(): an outline extruded with chamfer rings at both ends (glides, pulls).
* lv(): hand-built LOD membership (kitlib P-1b fr_lods).
* station_block(): the sidecar's station block for this package.
* write_mats(): Prop_VinylTeal / Prop_VinylBurgundy .mat (+ .meta, fresh GUIDs, kept across re-runs) from
  Prop_Vinyl.mat: _BaseMap cleared, the colour in _BaseColor, Prop_Vinyl's _N / _S / macro maps by GUID, a 0.45 m
  tile (VINYL_TILE).
* hero_preview(): the lookdev renders of the sheet (Cycles, AgX): a module-faithful station room (9 x 9 m,
  Station_Wall, the crash rail and base, the VCT floor with its border course, 2 x 4 troffers at the cell centres)
  for R1-R3 and a studio (floor and troffers only) for R4, the turntable frame sets and the silent MP4. Preview
  materials are read from the project's .mat files (colour, base map, smoothness from the mask's R, normal map,
  metallic, tile size), so Blender reads like the game.
"""

import json
import math
import os
import re
import subprocess
import sys
import time
import uuid

import bmesh
import bpy
import kitlib
import _station_common as sc
from mathutils import Matrix, Vector

LINE = "\u6e38\u620f\u89c6\u89c9 bs4-hero-kits"
TEAL = "Prop_VinylTeal"
BURGUNDY = "Prop_VinylBurgundy"
NEW_MATS = {TEAL: "#2C6264", BURGUNDY: "#5C2430"}
VINYL_TILE = 0.45    # m per repeat of Prop_Vinyl's 1024 maps: 2,275 px/m, above the 1,975 px/m of a 1080p frame at
                     # 0.35 m; at Prop_Vinyl's 1 m the grain cells read as 2-4 cm wrinkles. Same tile as builder A's
                     # Prop_VinylSlate / Navy / Charcoal (gen_bs4_waiting_surfaces.py), so the two vinyls are one family.

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", ".."))

B, BS = sc.B, sc.BS


# ------------------------------------------------------------------ colour
def srgb_to_lin(v):
    return v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4


def hex_srgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))


def hex_lin(h):
    return tuple(srgb_to_lin(v) for v in hex_srgb(h))


def register():
    sc.register()
    for slot, hx in NEW_MATS.items():
        kitlib.register_slot(slot, hex_lin(hx), 0.6, 0.0)


# ------------------------------------------------------------------ parts
def lv(obj, levels):
    """Hand-built LOD membership: "0", "1", "2" or a mix such as "01"."""
    obj["fr_lods"] = levels
    return obj


def _apply_mod(obj, name):
    vl = bpy.context.view_layer
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    vl.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=name)


def g1(obj, angle, lods="0"):
    """outlet_common.finish_part's G1 recipe with the part's bevel baked first, then n-gons triangulated and
    doubles removed (interact_door_common.finalize, the order proven at 0.00 deg after finish + FBX)."""
    if obj.modifiers.get("bevel") is not None:
        _apply_mod(obj, "bevel")
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
    obj["fr_lods"] = lods
    m = obj.modifiers.new("wn", "WEIGHTED_NORMAL")
    m.mode = "FACE_AREA"
    m.weight = 50
    m.keep_sharp = True
    return obj


def tri(obj):
    """Triangulate n-gons up front (concave caps), as _newcase.tri."""
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    ngons = [f for f in bm.faces if len(f.verts) > 4]
    if ngons:
        bmesh.ops.triangulate(bm, faces=ngons, quad_method="BEAUTY", ngon_method="BEAUTY")
    bm.to_mesh(obj.data)
    bm.free()
    return obj


def tris(obj):
    return sum(len(p.vertices) - 2 for p in obj.data.polygons)


def bevel_where(obj, pred, width, segments, angle=30.0):
    """Bevel only the edges whose two vertices satisfy pred(local co) (a weight-limited bevel; replaces kitlib's
    angle-limited one on this part)."""
    me = obj.data
    attr = me.attributes.get("bevel_weight_edge") or me.attributes.new("bevel_weight_edge", "FLOAT", "EDGE")
    vs = me.vertices
    for e in me.edges:
        a, b = vs[e.vertices[0]].co, vs[e.vertices[1]].co
        attr.data[e.index].value = 1.0 if (pred(a) and pred(b)) else 0.0
    for m in list(obj.modifiers):
        if m.type == "BEVEL":
            obj.modifiers.remove(m)
    mod = obj.modifiers.new("bevel", "BEVEL")
    mod.width = width
    mod.segments = segments
    mod.limit_method = "WEIGHT"
    mod.miter_outer = "MITER_ARC"
    mod.use_clamp_overlap = True
    return obj


# ------------------------------------------------------------------ tubes
def fillet(points, radius, step=15.0, closed=False):
    """Polyline -> dense polyline: each interior corner (every corner when closed) becomes a circular arc of
    centreline ``radius`` (a number, or a list per point), sampled every ``step`` degrees."""
    P = [Vector(p) for p in points]
    n = len(P)
    out = []
    for i in range(n):
        if not closed and (i == 0 or i == n - 1):
            out.append(P[i].copy())
            continue
        a, c, b = P[(i - 1) % n], P[i], P[(i + 1) % n]
        d1 = (c - a).normalized()
        d2 = (b - c).normalized()
        theta = math.acos(max(-1.0, min(1.0, d1.dot(d2))))
        r = radius[i] if isinstance(radius, (list, tuple)) else radius
        if theta < 1e-4 or r <= 0:
            out.append(c.copy())
            continue
        t = r * math.tan(theta / 2)
        t1 = c - d1 * t
        centre = c + (d2 - d1).normalized() * (r / math.cos(theta / 2))
        v1 = t1 - centre
        axis = v1.cross(d1).normalized()
        if v1.cross(d2).dot(axis) < 0:
            axis = -axis
        k = max(1, int(math.ceil(math.degrees(theta) / step - 1e-6)))
        rot_axis = (t1 - centre).cross((c + d2 * t) - centre).normalized()
        for j in range(k + 1):
            q = Matrix.Rotation(theta * j / k, 3, rot_axis)
            out.append(centre + q @ v1)
    return [tuple(p) for p in out]


def _frames(P, closed):
    n = len(P)
    T = []
    for k in range(n):
        if closed:
            a, b = P[(k - 1) % n], P[(k + 1) % n]
            T.append(((P[k] - a).normalized() + (b - P[k]).normalized()).normalized())
        elif k == 0:
            T.append((P[1] - P[0]).normalized())
        elif k == n - 1:
            T.append((P[-1] - P[-2]).normalized())
        else:
            T.append(((P[k] - P[k - 1]).normalized() + (P[k + 1] - P[k]).normalized()).normalized())
    ref = Vector((0, 0, 1)) if abs(T[0].z) < 0.9 else Vector((1, 0, 0))
    R = [T[0].cross(ref).normalized()]
    for k in range(1, n):
        v1 = P[k] - P[k - 1]
        c1 = v1.dot(v1)
        if c1 < 1e-14:
            R.append(R[-1].copy())
            continue
        rl = R[-1] - (2.0 / c1) * v1.dot(R[-1]) * v1
        tl = T[k - 1] - (2.0 / c1) * v1.dot(T[k - 1]) * v1
        v2 = T[k] - tl
        c2 = v2.dot(v2)
        rk = rl if c2 < 1e-14 else rl - (2.0 / c2) * v2.dot(rl) * v2
        R.append(rk.normalized())
    if closed and n > 2:
        # transport once more back to the start and spread the residual twist along the loop
        v1 = P[0] - P[-1]
        c1 = v1.dot(v1)
        rl = R[-1] - (2.0 / c1) * v1.dot(R[-1]) * v1
        tl = T[-1] - (2.0 / c1) * v1.dot(T[-1]) * v1
        v2 = T[0] - tl
        c2 = v2.dot(v2)
        r_end = rl if c2 < 1e-14 else rl - (2.0 / c2) * v2.dot(rl) * v2
        ang = math.atan2(T[0].dot(r_end.cross(R[0])), r_end.dot(R[0]))
        for k in range(n):
            R[k] = Matrix.Rotation(-ang * k / n, 3, T[k]) @ R[k]
    return T, R


def tube(kit, pts, r, slot, sides=16, closed=False, caps=True, name="tube", phase=0.0):
    """Round tube along a 3D polyline (Blender space) with rotation-minimising frames."""
    P = [Vector(p) for p in pts]
    T, R = _frames(P, closed)
    bm = bmesh.new()
    rings = []
    for k, p in enumerate(P):
        n1 = R[k]
        n2 = T[k].cross(n1).normalized()
        ring = []
        for i in range(sides):
            a = 2 * math.pi * i / sides + phase
            ring.append(bm.verts.new(p + (n1 * math.cos(a) + n2 * math.sin(a)) * r))
        rings.append(ring)
    m = len(rings)
    segs = m if closed else m - 1
    for k in range(segs):
        a, b = rings[k], rings[(k + 1) % m]
        for i in range(sides):
            j = (i + 1) % sides
            bm.faces.new((a[i], a[j], b[j], b[i]))
    if caps and not closed:
        for ring in (rings[0], list(reversed(rings[-1]))):
            c = bm.verts.new(sum((v.co for v in ring), Vector()) / len(ring))
            for i in range(sides):
                bm.faces.new((ring[(i + 1) % sides], ring[i], c))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return kit._new_object(name, bm, slot, "metres", "xz")


def bead(kit, pts, r, slot, sides=6, name="weld"):
    """A closed weld bead (tube loop) through ``pts``."""
    return tube(kit, pts, r, slot, sides=sides, closed=True, caps=False, name=name)


# ------------------------------------------------------------------ outlines
def rrect_pts(hw, hd, r, nc, nx, ny):
    """Rounded rectangle, CCW from the +x+y corner arc, a fixed point count for any size (task_chair._rrect),
    returned as (x, y, outward nx, outward ny). nc = corner steps, or one per corner (+x+y, -x+y, -x-y, +x-y)."""
    r = max(min(r, hw * 0.98, hd * 0.98), 0.0015)
    corners = [(hw - r, hd - r, 0.0), (-hw + r, hd - r, 90.0), (-hw + r, -hd + r, 180.0), (hw - r, -hd + r, 270.0)]
    steps = list(nc) if isinstance(nc, (list, tuple)) else [nc] * 4
    pts = []
    for k, (cx, cy, a0) in enumerate(corners):
        for i in range(steps[k] + 1):
            a = math.radians(a0 + 90.0 * i / steps[k])
            pts.append((cx + r * math.cos(a), cy + r * math.sin(a), math.cos(a), math.sin(a)))
        a_end = math.radians(a0 + 90.0)
        p0 = (cx + r * math.cos(a_end), cy + r * math.sin(a_end))
        nxt = corners[(k + 1) % 4]
        a_n = math.radians(nxt[2])
        p1 = (nxt[0] + r * math.cos(a_n), nxt[1] + r * math.sin(a_n))
        m = nx if k % 2 == 0 else ny
        for i in range(1, m):
            t = i / m
            pts.append((p0[0] + (p1[0] - p0[0]) * t, p0[1] + (p1[1] - p0[1]) * t, math.cos(a_end), math.sin(a_end)))
    return pts


def pad(kit, name, slot, hw, hd, r, profile, nc, nx, ny, loc, rot=(0, 0, 0), warp=None, bulge_w=None,
        cap_bottom=True, cap_top=True):
    """Upholstery pad, local x width, y depth (front -y), z up. ``profile`` = [(inset, z, bulge)], bottom to top;
    a ring with bulge b moves out along the outline normal by b * bulge_w(x, y, nx, ny) (default 1): the welt.
    ``warp(x, y, z)`` reshapes every vertex in local space. Fans close the ends (a cap buried in a shell is
    skipped). Placed by kitlib's _place (loc, rot in degrees, Blender space)."""
    bm = bmesh.new()

    def vert(x, y, z):
        return bm.verts.new(warp(x, y, z) if warp else (x, y, z))

    rings = []
    for inset, z, bul in profile:
        rr = max(r - inset, r * 0.35)
        ring = []
        for x, y, ux, uy in rrect_pts(hw - inset, hd - inset, rr, nc, nx, ny):
            w = (bulge_w(x, y, ux, uy) if bulge_w else 1.0) if bul else 0.0
            ring.append(vert(x + ux * bul * w, y + uy * bul * w, z))
        rings.append(ring)
    n = len(rings[0])
    for a, b in zip(rings, rings[1:]):
        for i in range(n):
            j = (i + 1) % n
            bm.faces.new((a[i], a[j], b[j], b[i]))
    caps = ([(rings[0], profile[0][1], True)] if cap_bottom else []) + ([(rings[-1], profile[-1][1], False)] if cap_top else [])
    for ring, z, bottom in caps:
        c = vert(0.0, 0.0, z)
        for i in range(n):
            bm.faces.new((ring[i], ring[(i + 1) % n], c))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    obj = kit._new_object(name, bm, slot, "metres", "xz")
    kit._place(obj, loc, rot)
    return obj


def plate(kit, w, h, steps, centre, normal, up, slot, name="plate", grain=None):
    """Board of w x h whose back face is centred on ``centre`` (Blender space) and which is built out along the
    unit ``normal`` through (inset, height) or (inset_w, inset_h, height) rings to its front face (as
    _newcase.plate, any facing). ``up`` is the board's h axis."""
    nrm, upv = Vector(normal).normalized(), Vector(up).normalized()
    u = upv.cross(nrm).normalized()
    c = Vector(centre)
    bm = bmesh.new()
    rings = []
    for st in steps:
        iu, iv, d = (st[0], st[0], st[1]) if len(st) == 2 else st
        hw, hh = w / 2 - iu, h / 2 - iv
        rings.append([bm.verts.new(c + u * a + upv * b + nrm * d) for a, b in ((-hw, -hh), (hw, -hh), (hw, hh), (-hw, hh))])
    for r0, r1 in zip(rings, rings[1:]):
        for i in range(4):
            j = (i + 1) % 4
            bm.faces.new((r0[i], r0[j], r1[j], r1[i]))
    bm.faces.new(rings[-1])
    bm.faces.new(list(reversed(rings[0])))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    obj = kit._new_object(name, bm, slot, "metres", "xz")
    if grain:
        obj["fr_grain"] = grain
    return obj


def rounded_steps(t, r, segs=3, back=0.0):
    """Plate steps: an optional eased back arris (``back``), then the front arris rounded with radius r in
    ``segs`` facets."""
    st = [(back, 0.0), (0.0, back)] if back > 0 else [(0.0, 0.0)]
    for k in range(segs + 1):
        a = math.radians(90.0 * k / segs)
        st.append((r - r * math.cos(a), t - r + r * math.sin(a)))
    return st


def extrude_rings(kit, outline, length, centre, axis, slot, chamfer=0.0012, scale_about=(0.0, 0.0), end_scale=None,
                  name="extrude"):
    """Extrude a closed 2D outline [(u, v)] (Blender space: u along the axis' first perpendicular, v along the
    second) by ``length`` along ``axis`` ('x', 'y' or 'z'), centred on ``centre``. The end rings are the outline
    scaled toward ``scale_about`` (end_scale, else from chamfer) and stand ``chamfer`` beyond the full rings:
    a chamfered end in 3 bands."""
    ax = {"x": 0, "y": 1, "z": 2}[axis]
    perp = [i for i in range(3) if i != ax]
    c = Vector(centre)
    size = max(max(p[0] for p in outline) - min(p[0] for p in outline), max(p[1] for p in outline) - min(p[1] for p in outline))
    s = end_scale if end_scale is not None else max(0.5, 1.0 - 2.0 * chamfer / max(size, 1e-4))
    su, sv = scale_about

    def pt(u, v, w):
        q = [0.0, 0.0, 0.0]
        q[perp[0]], q[perp[1]], q[ax] = u, v, w
        return c + Vector(q)

    bm = bmesh.new()
    h = length / 2
    levels = [(-h, s), (-h + chamfer, 1.0), (h - chamfer, 1.0), (h, s)]
    rings = []
    for w, k in levels:
        rings.append([bm.verts.new(pt(su + (u - su) * k, sv + (v - sv) * k, w)) for u, v in outline])
    n = len(outline)
    for a, b in zip(rings, rings[1:]):
        for i in range(n):
            j = (i + 1) % n
            bm.faces.new((a[i], a[j], b[j], b[i]))
    f0 = bm.faces.new(list(reversed(rings[0])))
    f1 = bm.faces.new(rings[-1])
    bmesh.ops.triangulate(bm, faces=[f0, f1], quad_method="BEAUTY", ngon_method="BEAUTY")
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return kit._new_object(name, bm, slot, "metres", "xz")


def lathe_axis(kit, profile, base, axis, slot, verts=16, name="lathe", close_top=True, close_bottom=True):
    """kit.lathe (a (radius, height) profile about Z) turned so its axis runs along the Blender vector ``axis``
    from ``base``."""
    obj = kit.lathe(profile, (0, 0, 0), slot, verts=verts, name=name, close_top=close_top, close_bottom=close_bottom)
    q = Vector((0, 0, 1)).rotation_difference(Vector(axis).normalized())
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = q
    obj.location = base
    return obj


# ------------------------------------------------------------------ sidecar
def station_block(kit, origin, note=""):
    sc.station_meta(kit, origin, note)
    kit.meta["station"]["line"] = LINE
    sc.no_pile(kit)


# ------------------------------------------------------------------ materials (.mat files)
def out_root():
    """--out-root of build_asset.py when given (where the new .mat files are written), else the project root."""
    argv = sys.argv
    if "--out-root" in argv:
        return argv[argv.index("--out-root") + 1]
    return ROOT


def write_mats(root=None):
    """Write Prop_VinylTeal / Prop_VinylBurgundy (.mat + .mat.meta) under <root>/Assets/Resources/Surfaces from the
    project's Prop_Vinyl.mat. A .meta that already exists keeps its GUID (re-runs do not churn GUIDs)."""
    root = root or out_root()
    src = os.path.join(ROOT, "Assets", "Resources", "Surfaces", "Prop_Vinyl.mat")
    src_meta = src + ".meta"
    text = open(src, encoding="utf-8").read()
    meta = open(src_meta, encoding="utf-8").read()
    dst_dir = os.path.join(root, "Assets", "Resources", "Surfaces")
    os.makedirs(dst_dir, exist_ok=True)
    written = []
    for slot, hx in NEW_MATS.items():
        r, g, b = hex_srgb(hx)
        t = text.replace("m_Name: Prop_Vinyl\n", "m_Name: %s\n" % slot)
        t = re.sub(r"(- _BaseMap:\n\s+m_Texture: )\{fileID: 2800000, guid: [0-9a-f]{32}, type: 3\}", r"\1{fileID: 0}", t)
        t = re.sub(r"- _BaseColor: \{r: [^}]*\}", "- _BaseColor: {r: %.4f, g: %.4f, b: %.4f, a: 1}" % (r, g, b), t)
        t = re.sub(r"- _TileSize: \{r: [^}]*\}", "- _TileSize: {r: %.4f, g: %.4f, b: 0, a: 0}" % (VINYL_TILE, VINYL_TILE), t)
        assert "m_Name: %s\n" % slot in t and "{r: %.4f," % r in t and "_TileSize: {r: %.4f," % VINYL_TILE in t, slot
        p = os.path.join(dst_dir, slot + ".mat")
        with open(p, "w", encoding="utf-8", newline="\n") as f:
            f.write(t)
        pm = p + ".meta"
        if os.path.exists(pm):
            guid = re.search(r"^guid: ([0-9a-f]{32})", open(pm, encoding="utf-8").read(), re.M).group(1)
        else:
            guid = uuid.uuid4().hex
        m = re.sub(r"^guid: [0-9a-f]{32}", "guid: " + guid, meta, flags=re.M)
        with open(pm, "w", encoding="utf-8", newline="\n") as f:
            f.write(m)
        written.append((p, guid))
    return written


def _guid_map(roots):
    g = {}
    for r in roots:
        d = os.path.join(r, "Assets", "Resources", "Surfaces", "Textures")
        if not os.path.isdir(d):
            continue
        for fn in os.listdir(d):
            if fn.endswith(".meta"):
                try:
                    t = open(os.path.join(d, fn), encoding="utf-8").read()
                except OSError:
                    continue
                m = re.search(r"^guid: ([0-9a-f]{32})", t, re.M)
                if m:
                    g[m.group(1)] = os.path.join(d, fn[:-5])
    return g


def parse_mat(path):
    t = open(path, encoding="utf-8").read()

    def tex(prop):
        m = re.search(r"- %s:\n\s+m_Texture: \{fileID: \d+(?:, guid: ([0-9a-f]{32}))?" % prop, t)
        return m.group(1) if m else None

    def flt(prop, default):
        m = re.search(r"- %s: (-?[\d.]+)" % prop, t)
        return float(m.group(1)) if m else default

    c = re.search(r"- _BaseColor: \{r: ([-\d.]+), g: ([-\d.]+), b: ([-\d.]+)", t)
    ts = re.search(r"- _TileSize: \{r: ([-\d.]+), g: ([-\d.]+)", t)
    return {"color": tuple(float(v) for v in c.groups()) if c else (1, 1, 1),
            "smooth": flt("_Smoothness", 0.5), "metal": flt("_Metallic", 0.0), "bump": flt("_BumpScale", 1.0),
            "tile": (float(ts.group(1)), float(ts.group(2))) if ts else (1.0, 1.0),
            "base": tex("_BaseMap"), "normal": tex("_BumpMap"), "mask": tex("_MaskMap"),
            "mesh_uv": flt("_MeshUV", 0.0) > 0.5}


def _img(path, colour):
    im = bpy.data.images.get(os.path.basename(path))
    if im is None or im.filepath != path:
        im = bpy.data.images.load(path, check_existing=True)
    im.colorspace_settings.name = "sRGB" if colour else "Non-Color"
    return im


def game_material(slot, roots, gmap, world_uv=False, name=None):
    """Build (or rebuild) the Blender material ``name or slot`` from <root>/Assets/Resources/Surfaces/<slot>.mat."""
    path = None
    for r in roots:
        p = os.path.join(r, "Assets", "Resources", "Surfaces", slot + ".mat")
        if os.path.exists(p):
            path = p
            break
    mat = bpy.data.materials.get(name or slot) or bpy.data.materials.new(name or slot)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    if path is None:
        colour, rough, metal = kitlib.SLOTS.get(slot, ((0.6, 0.6, 0.6), 0.6, 0.0))
        bsdf.inputs["Base Color"].default_value = (*colour, 1)
        bsdf.inputs["Roughness"].default_value = rough
        bsdf.inputs["Metallic"].default_value = metal
        mat["fr_src"] = "kitlib SLOTS (no .mat)"
        return mat
    d = parse_mat(path)
    mat["fr_src"] = path
    col = tuple(srgb_to_lin(v) for v in d["color"])
    if world_uv:
        tc = nt.nodes.new("ShaderNodeTexCoord")
        vec = tc.outputs["Object"]
    else:
        uvn = nt.nodes.new("ShaderNodeUVMap")
        vec = uvn.outputs["UV"]
    mp = nt.nodes.new("ShaderNodeMapping")
    mp.inputs["Scale"].default_value = (1.0 / max(d["tile"][0], 1e-3), 1.0 / max(d["tile"][1], 1e-3), 1.0 / max(d["tile"][0], 1e-3))
    nt.links.new(vec, mp.inputs["Vector"])

    def image_node(guid, colour):
        p = gmap.get(guid) if guid else None
        if not p or not os.path.exists(p):
            return None
        n = nt.nodes.new("ShaderNodeTexImage")
        n.image = _img(p, colour)
        n.interpolation = "Cubic" if colour else "Linear"
        if world_uv:
            n.projection = "BOX"
            n.projection_blend = 0.2
        nt.links.new(mp.outputs["Vector"], n.inputs["Vector"])
        return n

    base = image_node(d["base"], True)
    if base is not None:
        mul = nt.nodes.new("ShaderNodeMix")
        mul.data_type = "RGBA"
        mul.blend_type = "MULTIPLY"
        mul.inputs["Factor"].default_value = 1.0
        nt.links.new(base.outputs["Color"], mul.inputs[6])
        mul.inputs[7].default_value = (*col, 1)
        nt.links.new(mul.outputs[2], bsdf.inputs["Base Color"])
    else:
        bsdf.inputs["Base Color"].default_value = (*col, 1)
    mask = image_node(d["mask"], False)
    if mask is not None:
        sep = nt.nodes.new("ShaderNodeSeparateColor")
        nt.links.new(mask.outputs["Color"], sep.inputs["Color"])
        m1 = nt.nodes.new("ShaderNodeMath")
        m1.operation = "MULTIPLY"
        m1.use_clamp = True
        nt.links.new(sep.outputs["Red"], m1.inputs[0])
        m1.inputs[1].default_value = d["smooth"]
        m2 = nt.nodes.new("ShaderNodeMath")
        m2.operation = "SUBTRACT"
        m2.use_clamp = True
        m2.inputs[0].default_value = 1.0
        nt.links.new(m1.outputs[0], m2.inputs[1])
        nt.links.new(m2.outputs[0], bsdf.inputs["Roughness"])
    else:
        bsdf.inputs["Roughness"].default_value = max(0.02, min(1.0, 1.0 - d["smooth"]))
    nrm = image_node(d["normal"], False)
    if nrm is not None:
        nm = nt.nodes.new("ShaderNodeNormalMap")
        nm.inputs["Strength"].default_value = d["bump"]
        nt.links.new(nrm.outputs["Color"], nm.inputs["Color"])
        nt.links.new(nm.outputs["Normal"], bsdf.inputs["Normal"])
    bsdf.inputs["Metallic"].default_value = d["metal"]
    mat.diffuse_color = (*col, 1)
    return mat


def emission_material(name, strength, colour=(1.0, 0.96, 0.90)):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    em = nt.nodes.new("ShaderNodeEmission")
    em.inputs["Color"].default_value = (*colour, 1)
    em.inputs["Strength"].default_value = strength
    nt.links.new(em.outputs["Emission"], out.inputs["Surface"])
    return mat


def flat_material(name, colour_srgb, rough=0.9):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*[srgb_to_lin(v) for v in colour_srgb], 1)
    bsdf.inputs["Roughness"].default_value = rough
    return mat


# ------------------------------------------------------------------ lookdev scenes
ROOM = {"size": 9.0, "wall": 0.08, "ceiling": 2.9, "rail": (0.78, 0.93, 0.028, 0.010), "base": (0.10, 0.006),
        "border": 0.305, "cells": (1.5, 4.5, 7.5)}
TROFFER = (0.6, 1.2)
LENS = 40.0          # troffer lens emission (W/m2), the same in every render; exposure below


def _mesh_obj(name, verts, faces, mat, coll):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.update()
    ob = bpy.data.objects.new(name, me)
    ob.data.materials.append(mat)
    coll.objects.link(ob)
    return ob


def _box_obj(name, lo, hi, mat, coll, bevel=0.0):
    (x0, y0, z0), (x1, y1, z1) = lo, hi
    v = [(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0), (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)]
    f = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
    ob = _mesh_obj(name, v, f, mat, coll)
    if bevel > 0:
        m = ob.modifiers.new("bevel", "BEVEL")
        m.width = bevel
        m.segments = 3
        m.limit_method = "ANGLE"
    return ob


def _u2b(X, Y, Z):
    """Unity module frame -> Blender world (the same handedness change as kitlib)."""
    return (-X, -Z, Y)


def build_room(mats):
    """The Desk station room in the module frame (9 x 9 m, walls' faces at 0.08 / 8.92, ceiling 2.9)."""
    coll = bpy.data.collections.new("fr_room")
    bpy.context.scene.collection.children.link(coll)
    S, w, H = ROOM["size"], ROOM["wall"], ROOM["ceiling"]
    a, b = w, S - w
    q = lambda pts: [_u2b(*p) for p in pts]
    _mesh_obj("floor", q([(a, 0, a), (b, 0, a), (b, 0, b), (a, 0, b)]), [(0, 3, 2, 1)], mats["floor"], coll)
    bw = ROOM["border"]
    lift = 0.0005
    strips = [((a, a), (b, a + bw)), ((a, b - bw), (b, b)), ((a, a + bw), (a + bw, b - bw)), ((b - bw, a + bw), (b, b - bw))]
    for i, ((x0, z0), (x1, z1)) in enumerate(strips):
        _mesh_obj("border%d" % i, q([(x0, lift, z0), (x1, lift, z0), (x1, lift, z1), (x0, lift, z1)]), [(0, 3, 2, 1)], mats["border"], coll)
    _mesh_obj("ceiling", q([(a, H, a), (b, H, a), (b, H, b), (a, H, b)]), [(0, 1, 2, 3)], mats["ceiling"], coll)
    walls = [((a, a), (b, a)), ((b, a), (b, b)), ((b, b), (a, b)), ((a, b), (a, a))]
    for i, ((x0, z0), (x1, z1)) in enumerate(walls):
        _mesh_obj("wall%d" % i, q([(x0, 0, z0), (x1, 0, z1), (x1, H, z1), (x0, H, z0)]), [(0, 1, 2, 3)], mats["wall"], coll)
    rb, rt, rp, _rbev = ROOM["rail"]
    bh, bp = ROOM["base"]
    # rail and base on the four walls (boxes in the Unity frame, converted)
    for i, (lo, hi) in enumerate([((a, rb, a), (b, rt, a + rp)), ((a, rb, b - rp), (b, rt, b)),
                                  ((a, rb, a + rp), (a + rp, rt, b - rp)), ((b - rp, rb, a + rp), (b, rt, b - rp))]):
        lo_b, hi_b = _u2b(*lo), _u2b(*hi)
        _box_obj("rail%d" % i, [min(lo_b[k], hi_b[k]) for k in range(3)], [max(lo_b[k], hi_b[k]) for k in range(3)], mats["rail"], coll, bevel=0.010)
    for i, (lo, hi) in enumerate([((a, 0, a), (b, bh, a + bp)), ((a, 0, b - bp), (b, bh, b)),
                                  ((a, 0, a + bp), (a + bp, bh, b - bp)), ((b - bp, 0, a + bp), (b, bh, b - bp))]):
        lo_b, hi_b = _u2b(*lo), _u2b(*hi)
        _box_obj("base%d" % i, [min(lo_b[k], hi_b[k]) for k in range(3)], [max(lo_b[k], hi_b[k]) for k in range(3)], mats["base"], coll, bevel=0.002)
    cells = ROOM["cells"]
    tw, tl = TROFFER
    for cx in cells:
        for cz in cells:
            y = H - 0.002
            _mesh_obj("troffer", q([(cx - tw / 2, y, cz - tl / 2), (cx + tw / 2, y, cz - tl / 2), (cx + tw / 2, y, cz + tl / 2), (cx - tw / 2, y, cz + tl / 2)]),
                      [(0, 1, 2, 3)], mats["lens"], coll)
    return coll


def build_studio(mats, half=12.0):
    """Floor and a field of troffers only (no walls): for R4, the turntable frames and the MP4."""
    coll = bpy.data.collections.new("fr_studio")
    bpy.context.scene.collection.children.link(coll)
    q = lambda pts: [_u2b(*p) for p in pts]
    _mesh_obj("studio floor", q([(-half, 0, -half), (half, 0, -half), (half, 0, half), (-half, 0, half)]), [(0, 3, 2, 1)], mats["floor"], coll)
    tw, tl = TROFFER
    k = int(half // 3)
    for i in range(-k, k + 1):
        for j in range(-k, k + 1):
            cx, cz, y = 1.5 + 3.0 * i, 1.5 + 3.0 * j, ROOM["ceiling"] - 0.002
            _mesh_obj("troffer", q([(cx - tw / 2, y, cz - tl / 2), (cx + tw / 2, y, cz - tl / 2), (cx + tw / 2, y, cz + tl / 2), (cx - tw / 2, y, cz + tl / 2)]),
                      [(0, 1, 2, 3)], mats["lens"], coll)
    return coll


def setup_render(samples=64, res=(1920, 1080), exposure=0.0):
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    try:
        prefs = bpy.context.preferences.addons["cycles"].preferences
        prefs.compute_device_type = "METAL"
        prefs.get_devices()
        for d in prefs.devices:
            d.use = True
        scene.cycles.device = "GPU"
    except Exception:
        scene.cycles.device = "CPU"
    scene.cycles.samples = samples
    scene.cycles.use_denoising = True
    scene.cycles.max_bounces = 8
    scene.cycles.diffuse_bounces = 4
    scene.cycles.glossy_bounces = 4
    scene.cycles.sample_clamp_indirect = 8.0
    scene.render.resolution_x, scene.render.resolution_y = res
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = False
    scene.view_settings.view_transform = "AgX"
    scene.view_settings.look = "None"
    scene.view_settings.exposure = exposure
    scene.render.image_settings.file_format = "JPEG"
    scene.render.image_settings.quality = 92
    world = bpy.data.worlds.get("fr world") or bpy.data.worlds.new("fr world")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.05, 0.05, 0.05, 1)
    world.node_tree.nodes["Background"].inputs[1].default_value = 1.0
    scene.world = world
    return scene


def camera(scene, loc, target, vfov, res):
    cam = scene.camera
    if cam is None:
        cd = bpy.data.cameras.new("fr cam")
        cam = bpy.data.objects.new("fr cam", cd)
        scene.collection.objects.link(cam)
        scene.camera = cam
    cam.data.sensor_fit = "VERTICAL"
    cam.data.angle_y = math.radians(vfov)
    cam.data.clip_start = 0.02
    cam.data.clip_end = 200.0
    cam.location = loc
    d = Vector(target) - Vector(loc)
    cam.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
    scene.render.resolution_x, scene.render.resolution_y = res
    return cam


def render(scene, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    return path


class Pose:
    """A kit's module placement: Unity position (x, z) and yaw (degrees, Unity: +Z toward +X)."""

    def __init__(self, x, z, yaw):
        self.x, self.z, self.yaw = x, z, yaw

    def world(self, p):
        """Kit-local Unity point -> Blender world point (in the module room)."""
        lx, ly, lz = p
        t = math.radians(self.yaw)
        X = self.x + lx * math.cos(t) + lz * math.sin(t)
        Z = self.z - lx * math.sin(t) + lz * math.cos(t)
        return Vector(_u2b(X, ly, Z))

    def place(self, obj):
        obj.rotation_mode = "XYZ"
        obj.rotation_euler = (0.0, 0.0, -math.radians(self.yaw))
        obj.location = (-self.x, -self.z, 0.0)


def orbit(target, az, dist, height):
    """Kit-local Unity camera point at azimuth ``az`` (0 = in front, + toward local +x), horizontal ``dist`` from
    the target and at ``height`` above the floor."""
    tx, ty, tz = target
    a = math.radians(az)
    return (tx + dist * math.sin(a), height, tz + dist * math.cos(a))


def _import_fbx(path, roots, gmap):
    """Import a kit FBX (kitlib's export round-trips to the authored Blender frame); its slots take the game
    materials. Returns the new mesh objects (parented to nothing, world matrices kept)."""
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    meshes = []
    for o in new:
        if o.type != "MESH":
            continue
        mw = o.matrix_world.copy()
        o.parent = None
        o.matrix_world = mw
        for i, m in enumerate(o.data.materials):
            if m is None:
                continue
            base_name = m.name.split(".")[0]
            gm = bpy.data.materials.get(base_name)
            if gm is None or "fr_src" not in gm:
                gm = game_material(base_name, roots, gmap)
            o.data.materials[i] = gm
        meshes.append(o)
    for o in new:
        if o.type != "MESH":
            bpy.data.objects.remove(o, do_unlink=True)
    return meshes


def _lod_level(name):
    n = name.upper()
    return 2 if "LOD2" in n else 1 if "LOD1" in n else 0


def _level_at(dist, lod_d, levels):
    """Unity's LOD at ``dist`` for lodDistances (d01, d12, ...), limited to the levels a model has."""
    lv = 0
    for i, d in enumerate(lod_d[:2]):
        if dist >= d - 1e-6:
            lv = i + 1
    while lv > 0 and lv not in levels:
        lv -= 1
    return lv


def _crop_border(scene, cam, objs, pad=0.30, aspect=16.0 / 9.0):
    """Render border (0..1) round the objects' projected bounds, padded and widened to the frame's aspect: the
    crop keeps the 1080p pixel density of the full game-FOV frame (an honest distance view)."""
    from bpy_extras.object_utils import world_to_camera_view
    us, vs = [], []
    for o in objs:
        for c in o.bound_box:
            p = world_to_camera_view(scene, cam, o.matrix_world @ Vector(c))
            us.append(p.x)
            vs.append(p.y)
    if not us:
        return None
    u0, u1, v0, v1 = min(us), max(us), min(vs), max(vs)
    cu, cv = (u0 + u1) / 2, (v0 + v1) / 2
    w, h = (u1 - u0) * (1 + pad), (v1 - v0) * (1 + pad)
    fa = scene.render.resolution_x / scene.render.resolution_y
    # border units: u is a fraction of the width, v of the height; aspect in pixels
    if w * fa / max(h, 1e-6) < aspect:
        w = h * aspect / fa
    else:
        h = w * fa / aspect
    w, h = min(w, 1.0), min(h, 1.0)
    u0, v0 = max(0.0, min(1.0 - w, cu - w / 2)), max(0.0, min(1.0 - h, cv - h / 2))
    return (u0, u0 + w, v0, v0 + h)


def hero_preview(kit, spec, out_dir, parts=("R", "TT", "MP4"), samples=64, tt_samples=40, mp4_frames=72,
                 tmp_dir=None, exposure=0.0, res=(1920, 1080), log=print):
    """The lookdev renders of one kit (see the module docstring). ``spec`` (Unity, kit-local unless noted):
      pose (x, z, yaw)          module placement for the room renders; twins [(x, z, yaw)] more copies (both columns)
      target (x, y, z)          look-at point for R1, R2, R4 and the 1.5 / 4 m turntables
      R1 / R2 dict(az, dist, h, vfov)        room cameras (az 0 = in front, + toward local +x)
      R3 dict(target, dir, dist, vfov)       room hero camera on LOD0
      R4 dict(az, h)            studio cameras at lodDistances d01 and d12, game vFOV 76, cropped (1080p density)
      hero (x, y, z), tt035_az [deg...]      the 0.35 m orbit's pivot and kit yaws
      today                     None | "absent" | (fbx path, json path) of the stand-in (TODAY column)
      variants                  {"Kit_X_Name": {"old slot": "new slot"}} (one column each)
      dressed                   [(fbx path, (x, y, z), yaw)] neighbour kits for the "dressed" column
    Writes JPG q92 into out_dir: R1_<col>.jpg ... R3_<col>.jpg, R4_lod1_<col>.jpg, R4_lod2_<col>.jpg,
    tt035_00..07.jpg, tt150_*, tt400_*, <NAME>_turntable.mp4 (silent: -an), and returns the paths."""
    roots = [out_root(), ROOT]
    gmap = _guid_map(roots)
    scene = setup_render(samples, res, exposure)
    used = set()
    for o in [kit.object] + kit._lod_objects():
        used.update(m.name.split(".")[0] for m in o.data.materials if m is not None)
    for v in spec.get("variants", {}).values():
        used.update(v.values())
    for s in sorted(used):
        game_material(s, roots, gmap)
    for o in [kit.object] + kit._lod_objects():
        for i, m in enumerate(o.data.materials):
            if m is not None:
                o.data.materials[i] = bpy.data.materials[m.name.split(".")[0]]
    mats = {"floor": game_material("Station_Floor", roots, gmap, world_uv=True, name="lk Station_Floor"),
            "border": game_material("Station_FloorBorder", roots, gmap, world_uv=True, name="lk Station_FloorBorder"),
            "wall": game_material("Station_Wall", roots, gmap, world_uv=True, name="lk Station_Wall"),
            "rail": game_material("Station_CrashRail", roots, gmap, world_uv=True, name="lk Station_CrashRail"),
            "base": game_material("Station_Base", roots, gmap, world_uv=True, name="lk Station_Base"),
            "ceiling": flat_material("lk ceiling", (0.80, 0.79, 0.76), 0.95),
            "lens": emission_material("lk lens", LENS)}
    room = build_room(mats)
    studio = build_studio(mats)
    lod0 = kit.object
    lods = {0: lod0, 1: kit.lods.get(1), 2: kit.lods.get(2)}
    root_empty = bpy.data.objects.new("fr kit root", None)
    scene.collection.objects.link(root_empty)
    for o in lods.values():
        if o is not None:
            o.parent = root_empty
    kit_levels = {lv for lv, o in lods.items() if o is not None}
    pose = Pose(*spec["pose"])

    # TODAY: the stand-in
    today = spec.get("today")
    today_objs, today_root, today_lodd = {}, None, [6.0, 12.0, 30.0]
    if today and today != "absent":
        fbx, js = today
        today_root = bpy.data.objects.new("fr today root", None)
        scene.collection.objects.link(today_root)
        for o in _import_fbx(fbx, roots, gmap):
            mw = o.matrix_world.copy()
            o.parent = today_root
            o.matrix_world = mw
            today_objs[_lod_level(o.name)] = o
        try:
            today_lodd = json.load(open(js, encoding="utf-8")).get("lodDistances", today_lodd)
        except (OSError, ValueError):
            pass
    # dressing (neighbour kits on or beside this one), kit-local
    dressed = []
    for fbx, (dx, dy, dz), dyaw in spec.get("dressed", []):
        e = bpy.data.objects.new("fr dressed", None)
        scene.collection.objects.link(e)
        e.parent = root_empty
        e.rotation_mode = "XYZ"
        e.rotation_euler = (0.0, 0.0, -math.radians(dyaw))
        e.location = B(dx, dy, dz)
        objs = _import_fbx(fbx, roots, gmap)
        lvl0 = [o for o in objs if _lod_level(o.name) == 0] or objs
        for o in objs:
            if o in lvl0:
                o.parent = e
                o.matrix_parent_inverse.identity()
                dressed.append(o)
            else:
                bpy.data.objects.remove(o, do_unlink=True)
    # twins: more copies at other module poses (share mesh data, so variants follow)
    twins = []
    for tp in spec.get("twins", []):
        for src_root, objs in ((root_empty, [o for o in lods.values() if o is not None]),
                               (today_root, list(today_objs.values()))):
            if src_root is None:
                continue
            e = bpy.data.objects.new("fr twin", None)
            scene.collection.objects.link(e)
            Pose(*tp).place(e)
            for o in objs:
                t = o.copy()
                scene.collection.objects.link(t)
                t.parent = e
                t.matrix_parent_inverse = o.matrix_parent_inverse.copy()
                t.matrix_basis = o.matrix_basis.copy()
                twins.append((o, t))

    def show(which, level=0, today_level=None, dress=False):
        for lv_, o in lods.items():
            if o is not None:
                o.hide_render = not (which == "kit" and lv_ == level)
        for lv_, o in today_objs.items():
            o.hide_render = not (which == "today" and lv_ == (level if today_level is None else today_level))
        for o in dressed:
            o.hide_render = not (dress and which == "kit")
        for o, t in twins:
            t.hide_render = o.hide_render or not state["twins"]

    state = {"twins": False}

    def stage(name):
        for c, on in ((room, name == "room"), (studio, name == "studio")):
            c.hide_render = not on
            for o in c.objects:
                o.hide_render = not on
        state["twins"] = name == "room"

    def place(p):
        p.place(root_empty)
        if today_root is not None:
            p.place(today_root)

    def set_variant(remap, undo=False):
        for old, new in (remap or {}).items():
            if undo:
                kit.set_slot(new, old)
            else:
                kit.set_slot(old, new)
        for o in [kit.object] + kit._lod_objects():
            for i, m in enumerate(o.data.materials):
                if m is not None and m.name.split(".")[0] in bpy.data.materials and "fr_src" not in m:
                    o.data.materials[i] = bpy.data.materials[m.name.split(".")[0]]

    columns = [("rec", None, False)]
    columns += [(name.split("_")[-1].lower(), remap, False) for name, remap in spec.get("variants", {}).items()]
    if dressed:
        columns.append(("dressed", None, True))
    written = []
    ts = time.time()
    if "R" in parts:
        stage("room")
        place(pose)
        scene.cycles.samples = samples
        tgt = spec["target"]
        cams = {}
        for key in ("R1", "R2"):
            c = spec[key]
            ct = c.get("target", tgt)
            cams[key] = (pose.world(orbit(ct, c["az"], c["dist"], c["h"])), pose.world(ct), c["vfov"])
        c3 = spec["R3"]
        t3 = Vector(c3["target"])
        d3 = Vector(c3["dir"]).normalized()
        cams["R3"] = (pose.world(tuple(t3 + d3 * c3["dist"])), pose.world(tuple(t3)), c3["vfov"])
        cols = ([("today", None, False)] if today else []) + columns
        for col, remap, dress in cols:
            set_variant(remap)
            for key in ("R1", "R2", "R3"):
                if col == "today":
                    show("none" if today == "absent" else "today", 0)
                else:
                    show("kit", 0, dress=dress)
                camera(scene, cams[key][0], cams[key][1], cams[key][2], res)
                written.append(render(scene, os.path.join(out_dir, "%s_%s.jpg" % (key, col))))
                log("[hero] %s (%.0f s)" % (written[-1], time.time() - ts))
            set_variant(remap, undo=True)
        # R4: studio, game vFOV 76, at the LOD switch distances, cropped round the kit at 1080p density
        stage("studio")
        place(Pose(0.0, 0.0, 0.0))
        d01, d12 = kit.meta["lodDistances"][0], kit.meta["lodDistances"][1]
        c4 = spec["R4"]
        r4cols = ([("today", None, False)] if (today and today != "absent" and today_objs) else []) + \
                 [c for c in columns if c[0] != "dressed"]
        for col, remap, _dress in r4cols:
            set_variant(remap)
            for lvl, dist in ((1, d01), (2, d12)):
                cam_p = Pose(0, 0, 0).world(orbit(tgt, c4["az"], dist, c4["h"]))
                cam = camera(scene, cam_p, Pose(0, 0, 0).world(tgt), 76.0, res)
                if col == "today":
                    tl = _level_at(dist, today_lodd, set(today_objs))
                    show("today", tl, today_level=tl)
                    vis = [today_objs[tl]]
                else:
                    show("kit", lvl)
                    vis = [lods[lvl]]
                bpy.context.view_layer.update()
                border = _crop_border(scene, cam, vis)
                if border:
                    scene.render.use_border = True
                    scene.render.use_crop_to_border = True
                    (scene.render.border_min_x, scene.render.border_max_x,
                     scene.render.border_min_y, scene.render.border_max_y) = border
                p = os.path.join(out_dir, "R4_lod%d_%s.jpg" % (lvl, col))
                written.append(render(scene, p))
                scene.render.use_border = False
                with open(p[:-4] + ".txt", "w", encoding="utf-8", newline="\n") as f:
                    f.write("distance %.2f m, vFOV 76, 1920x1080 frame, crop %s, level shown %s\n"
                            % (dist, ",".join("%.4f" % b for b in (border or ())), tl if col == "today" else lvl))
                log("[hero] %s (%.0f s)" % (p, time.time() - ts))
            set_variant(remap, undo=True)
    if "TT" in parts:
        # Turntable frame sets at 1080p: 0.35 m on the named hardware (game vFOV 76, R3's samples), 1.5 m and 4 m
        # on the kit (narrower lenses so the kit fills the frame). Each set shows the LOD the game shows at that
        # distance (lodDistances; a kit whose d01 is 4.0 m is on LOD1 in the 4 m set), written in <tag>.txt.
        stage("studio")
        res_tt = res
        tgt = spec["target"]
        hero = spec["hero"]
        lod_d = kit.meta["lodDistances"]
        sets = [("tt035", hero, 0.35, 76.0, spec.get("tt035_az", [45.0 * k for k in range(8)]), samples),
                ("tt150", tgt, 1.5, 40.0, [45.0 * k for k in range(8)], tt_samples),
                ("tt400", tgt, 4.0, 22.0, [45.0 * k for k in range(8)], tt_samples)]
        for tag, pivot, dist, vfov, angs, smp in sets:
            el = {"tt035": 0.30, "tt150": 0.45, "tt400": 0.30}[tag]
            lvl = _level_at(dist, lod_d, kit_levels)
            show("kit", lvl)
            scene.cycles.samples = smp
            for k, ang in enumerate(angs):
                Pose(0.0, 0.0, ang).place(root_empty)
                piv_rot = Pose(0.0, 0.0, ang).world(pivot)
                cam_p = piv_rot + Vector((0.0, -dist * math.cos(el), dist * math.sin(el)))
                camera(scene, cam_p, piv_rot, vfov, res_tt)
                written.append(render(scene, os.path.join(out_dir, "%s_%02d.jpg" % (tag, k))))
            with open(os.path.join(out_dir, tag + ".txt"), "w", encoding="utf-8", newline="\n") as f:
                f.write("%s: kit yaw %s deg, camera %.2f m from %s (kit-local Unity), elevation %.2f rad, vFOV %.0f, "
                        "%dx%d, %d samples, studio, LOD%d (lodDistances %s)\n"
                        % (tag, ",".join("%.0f" % a for a in angs), dist, tuple(pivot), el, vfov, res_tt[0], res_tt[1],
                           smp, lvl, lod_d))
            log("[hero] %s x%d LOD%d (%.0f s)" % (tag, len(angs), lvl, time.time() - ts))
    if "MP4" in parts:
        stage("studio")
        show("kit", 0)
        scene.cycles.samples = max(24, tt_samples - 8)
        res_mp = (1280, 720)
        tgt = spec["target"]
        tdir = tmp_dir or os.path.join(out_dir, "_mp4")
        os.makedirs(tdir, exist_ok=True)
        el = 0.40
        for k in range(mp4_frames):
            ang = 360.0 * k / mp4_frames
            Pose(0.0, 0.0, ang).place(root_empty)
            piv = Pose(0.0, 0.0, ang).world(tgt)
            cam_p = piv + Vector((0.0, -1.5 * math.cos(el), 1.5 * math.sin(el)))
            camera(scene, cam_p, piv, 40.0, res_mp)
            render(scene, os.path.join(tdir, "f_%03d.jpg" % k))
        mp4 = os.path.join(out_dir, "%s_turntable.mp4" % kit.name)
        cmd = ["ffmpeg", "-y", "-loglevel", "error", "-framerate", "24", "-i", os.path.join(tdir, "f_%03d.jpg"), "-an",
               "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "18", "-movflags", "+faststart", mp4]
        subprocess.run(cmd, check=True)
        written.append(mp4)
        log("[hero] %s (%.0f s)" % (mp4, time.time() - ts))
    for o in [o for o in lods.values() if o is not None]:
        mw = o.matrix_world.copy()
        o.parent = None
        o.matrix_world = mw
    return written
