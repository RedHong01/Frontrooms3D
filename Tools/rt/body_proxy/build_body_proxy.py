# G14 follow-up, D2 (b) (Red 2026-10-22 12:36, "G14 的 D1、D6、D2 全部按推荐"): the player's body for the traced reflections.
#
# Builds the reflection-only body proxy in Blender and writes it as C# data, so the proxy compiles out on every platform
# that is not macOS (no asset ships to WebGL, iOS or Windows):
#   Assets/Scripts/Rendering/GlassRT/FrontRoomsGlassRTBodyProxyMesh.cs   (positions in mm, normals x127, 4 submeshes)
# plus, in the output folder, body_proxy.json (stats, landmarks), body_proxy.blend and preview renders.
#
# Art (game visuals): a 1990 office worker, 1.75 m tall with the eye at 1.62 m (FrontRooms.Map.ModuleUnits.PlayerHeight /
# PlayerEye, the CharacterController the camera rides on; nothing of the player rig is changed): a single-breasted suit
# jacket with the period's padded square shoulders and a low button stance, the shirt and tie in the V, straight trousers
# with a break over plain shoes, a short haircut, arms relaxed at the sides; no face detail. Low poly (about 3.5k
# triangles), smooth normals with hard edges only where the cloth folds sharply. Proportions follow the standing plumb line:
# ear, shoulder, hip and ankle on one vertical 8.5 cm behind the eye (the eye is on the capsule's axis).
#
# Coordinates: everything is authored in Unity's frame (x = the body's right, y up, z forward, metres, feet on y = 0, eye at
# (0, 1.62, 0)) and converted to Blender's (x, y, z)_b = (-x, -z, y) for the mesh; the export converts back and reverses the
# winding (Blender's front faces are counter-clockwise, Unity's clockwise).
#
# Run (no downloads; Blender 4.3):
#   Blender -b --factory-startup --python Tools/rt/body_proxy/build_body_proxy.py -- <project root> <out dir> [--previews]
import json
import math
import os
import sys

import bmesh
import bpy
from mathutils import Vector

SUIT, DARK, SKIN, SHIRT = 0, 1, 2, 3
# preview colours = the runtime materials in FrontRoomsGlassRTBodyProxy.cs (fix round 1, art review #3: skin and shirt darkened
# so the head reads as part of the dark silhouette; the generated mesh does not depend on them)
MATERIALS = [("suit", (0x2B, 0x2E, 0x36)), ("dark", (0x17, 0x15, 0x16)), ("skin", (0x3A, 0x2F, 0x29)), ("shirt", (0x4A, 0x4C, 0x50))]
EYE_Y = 1.62
STATURE = 1.75
PLUMB_Z = -0.085          # ear / shoulder / hip / ankle line, behind the eye (the capsule axis is x = z = 0)

parts = []                # (name, verts [Vector, Unity frame], faces [(i...)], face materials, axis points for orientation)


def superellipse_ring(center, u, v, a, b, n, seg, phase=0.0):
    pts = []
    for k in range(seg):
        th = 2.0 * math.pi * k / seg + phase
        c, s = math.cos(th), math.sin(th)
        x = math.copysign(abs(c) ** (2.0 / n), c) * a
        y = math.copysign(abs(s) ** (2.0 / n), s) * b
        pts.append(center + u * x + v * y)
    return pts


def loft(name, rings, mat, cap_start=None, cap_end=None, axis=None):
    """rings: lists of Vectors with the same count. cap_*: a Vector apex for a fan cap (or None = open)."""
    verts, faces = [], []
    seg = len(rings[0])
    for r in rings:
        verts.extend(r)
    for i in range(len(rings) - 1):
        for k in range(seg):
            a = i * seg + k
            b = i * seg + (k + 1) % seg
            c = (i + 1) * seg + (k + 1) % seg
            d = (i + 1) * seg + k
            faces.append((a, b, c, d))
    if cap_start is not None:
        ci = len(verts)
        verts.append(cap_start)
        for k in range(seg):
            faces.append((ci, (k + 1) % seg, k))
    if cap_end is not None:
        ci = len(verts)
        verts.append(cap_end)
        base = (len(rings) - 1) * seg
        for k in range(seg):
            faces.append((ci, base + k, base + (k + 1) % seg))
    centers = [sum(r, Vector()) / len(r) for r in rings]
    parts.append({"name": name, "verts": verts, "faces": faces, "mats": [mat] * len(faces), "axis": axis or centers})
    return parts[-1]


def horizontal_rings(spec, seg, x0=0.0, n_default=2.0):
    """spec rows: (y, half width a (x), half depth b (z), centre z, [n], [centre x])."""
    rings = []
    for row in spec:
        y, a, b, cz = row[0], row[1], row[2], row[3]
        n = row[4] if len(row) > 4 else n_default
        cx = row[5] if len(row) > 5 else x0
        rings.append(superellipse_ring(Vector((cx, y, cz)), Vector((1, 0, 0)), Vector((0, 0, 1)), a, b, n, seg, math.pi / 2.0))
    return rings


def limb_rings(points, radii_a, radii_b, seg, n=2.0, forward=Vector((0, 0, 1))):
    """Rings along a polyline; u roughly across the body (x), v roughly forward (z)."""
    rings = []
    for i, p in enumerate(points):
        if i == 0:
            t = points[1] - points[0]
        elif i == len(points) - 1:
            t = points[-1] - points[-2]
        else:
            t = points[i + 1] - points[i - 1]
        t.normalize()
        v = forward - t * forward.dot(t)
        v.normalize()
        u = t.cross(v)
        u.normalize()
        if u.x < 0:
            u = -u
        rings.append(superellipse_ring(p, u, v, radii_a[i], radii_b[i], n, seg, math.pi / 2.0))
    return rings


def lerp(a, b, t):
    return a + (b - a) * t


# ------------------------------------------------------------------ head (skin), no face detail; hair = a shell (below)
HEAD = [  # y, a (half width), b (half depth), centre z, n
    (1.500, .030, .036, -.024, 2.2),
    (1.518, .050, .060, -.040, 2.2),
    (1.542, .061, .079, -.054, 2.3),
    (1.572, .069, .091, -.066, 2.3),
    (1.605, .074, .097, -.075, 2.4),
    (1.640, .077, .100, -.079, 2.4),
    (1.672, .075, .098, -.082, 2.3),
    (1.700, .068, .090, -.084, 2.2),
    (1.722, .056, .075, -.086, 2.1),
    (1.738, .038, .052, -.087, 2.0),
    (1.747, .018, .025, -.087, 2.0),
]
head = loft("head", horizontal_rings(HEAD, 24), SKIN, cap_end=Vector((0, STATURE, -.087)))


def profile_at(table, y):
    """Interpolated ring parameters (a, b, cz, n) of a horizontal-ring table at height y."""
    if y <= table[0][0]:
        r = table[0]
        return r[1], r[2], r[3], r[4]
    for r0, r1 in zip(table, table[1:]):
        if r0[0] <= y <= r1[0]:
            t = (y - r0[0]) / (r1[0] - r0[0])
            return lerp(r0[1], r1[1], t), lerp(r0[2], r1[2], t), lerp(r0[3], r1[3], t), lerp(r0[4], r1[4], t)
    r = table[-1]
    return r[1], r[2], r[3], r[4]


def surface_point(table, y, phi, out=0.0):
    """The point of a superellipse-ring surface at height y and azimuth phi (0 = front, +pi/2 = the body's right),
    pushed `out` metres along the horizontal normal."""
    a, b, cz, n = profile_at(table, y)
    s_, c_ = math.sin(phi), math.cos(phi)
    x = math.copysign(abs(s_) ** (2.0 / n), s_) * a
    z = math.copysign(abs(c_) ** (2.0 / n), c_) * b
    nx = math.copysign(abs(x / a) ** (n - 1), x) / a if a > 0 else 0.0
    nz = math.copysign(abs(z / b) ** (n - 1), z) / b if b > 0 else 0.0
    ln = math.hypot(nx, nz) or 1.0
    return Vector((x + out * nx / ln, y, cz + z + out * nz / ln))


def hairline(phi):
    """Height of the hairline around the head: 1.700 at the forehead, temples 1.672, sideburns 1.618 above the ears, nape 1.585."""
    d = abs(math.atan2(math.sin(phi), math.cos(phi)))       # 0 front .. pi back
    pts = [(0.0, 1.700), (.55, 1.692), (.95, 1.660), (1.35, 1.618), (1.75, 1.625), (2.30, 1.600), (math.pi, 1.585)]
    for (d0, y0), (d1, y1) in zip(pts, pts[1:]):
        if d0 <= d <= d1:
            return lerp(y0, y1, (d - d0) / (d1 - d0))
    return pts[-1][1]


# a short 1990 haircut: a 6-9 mm shell over the cranium from the hairline to the crown (fuller on top)
HAIR_SEG, HAIR_ROWS = 32, 9
hair_rings = []
for r in range(HAIR_ROWS):
    t = r / (HAIR_ROWS - 1.0)
    ring = []
    for k in range(HAIR_SEG):
        phi = 2.0 * math.pi * k / HAIR_SEG
        y0 = hairline(phi)
        y = lerp(y0, 1.744, t * t * (3 - 2 * t) * .35 + t * .65)
        ring.append(surface_point(HEAD, y, phi, out=.006 + .004 * t))
    hair_rings.append(ring)
loft("hair", hair_rings, DARK, cap_end=Vector((0, STATURE + .009, -.087)))

# ------------------------------------------------------------------ neck (skin) and the shirt collar band
NECK = [(1.420, .058, .058, -.088), (1.460, .056, .056, -.080), (1.500, .054, .054, -.068), (1.535, .050, .052, -.060)]
loft("neck", horizontal_rings(NECK, 14), SKIN)
COLLAR = [(1.440, .071, .070, -.086, 2.2), (1.470, .066, .066, -.080, 2.2), (1.488, .062, .062, -.075, 2.2)]
collar = loft("collar", horizontal_rings(COLLAR, 16), SHIRT)

# ------------------------------------------------------------------ jacket: boxy 1990 single-breasted, padded shoulders
JACKET = [  # y, a, b, centre z, n
    (0.760, .196, .136, -.090, 2.6),
    (0.800, .193, .134, -.089, 2.6),
    (0.880, .186, .128, -.087, 2.6),
    (0.960, .179, .122, -.085, 2.6),
    (1.040, .174, .118, -.083, 2.6),
    (1.110, .178, .122, -.081, 2.7),
    (1.160, .183, .126, -.080, 2.7),
    (1.205, .189, .130, -.079, 2.8),
    (1.250, .195, .134, -.078, 2.8),
    (1.295, .200, .135, -.078, 2.9),
    (1.335, .205, .133, -.079, 3.0),
    (1.370, .210, .126, -.081, 3.2),
    (1.398, .214, .115, -.083, 3.4),
    (1.420, .214, .102, -.085, 3.6),
    (1.438, .205, .090, -.086, 3.4),
    (1.452, .172, .080, -.087, 3.0),
    (1.462, .125, .074, -.087, 2.6),
    (1.470, .082, .070, -.086, 2.3),
]
jacket = loft("jacket", horizontal_rings(JACKET, 40), SUIT)


def v_half(y):            # the V of the lapels: closes at the top button (1.18), 7.5 cm each side at the collar
    return max(0.0, (y - 1.18) / (1.462 - 1.18)) * .075


def tie_half(y):          # the knot at 1.43 (2.2 cm wide each side), widening to 3.6 cm at the V's bottom
    return lerp(.036, .022, max(0.0, min(1.0, (y - 1.18) / (1.43 - 1.18))))


def front_point(x, y, out):
    """The jacket's front surface at (x, y), pushed `out` metres forward (along +z: the panels face the reflection)."""
    a, b, cz, n = profile_at(JACKET, y)
    u = min(.999, abs(x) / a)
    return Vector((x, y, cz + b * (1.0 - u ** n) ** (1.0 / n) + out))


def panel(name, mat, y0, y1, half, out, rows=16, cols=8):
    """A panel on the jacket front between heights y0..y1, |x| <= half(y): a grid that ends in a point where half = 0."""
    rings = []
    for r in range(rows + 1):
        y = lerp(y0, y1, r / float(rows))
        h = half(y)
        rings.append([front_point(lerp(-h, h, c / float(cols)), y, out) for c in range(cols + 1)])
    verts, faces = [], []
    for row in rings:
        verts.extend(row)
    w = cols + 1
    for r in range(rows):
        for c in range(cols):
            faces.append((r * w + c, r * w + c + 1, (r + 1) * w + c + 1, (r + 1) * w + c))
    parts.append({"name": name, "verts": verts, "faces": faces, "mats": [mat] * len(faces),
                  "axis": [Vector((0, lerp(y0, y1, k / 4.0), -.08)) for k in range(5)]})


panel("shirt", SHIRT, 1.180, 1.468, v_half, .0035)
panel("tie", DARK, 1.180, 1.432, lambda y: min(tie_half(y), v_half(y) * .92), .0065, rows=14, cols=4)

# ------------------------------------------------------------------ sleeves (suit) and hands (skin)
for side in (-1.0, 1.0):
    sp = [Vector((side * .194, 1.405, -.086)), Vector((side * .208, 1.330, -.084)), Vector((side * .219, 1.230, -.080)),
          Vector((side * .225, 1.110, -.074)), Vector((side * .228, 1.030, -.062)), Vector((side * .230, .945, -.046)),
          Vector((side * .232, .878, -.036))]
    ra = [.070, .066, .060, .054, .050, .047, .046]
    rb = [.072, .066, .059, .053, .050, .047, .046]
    loft("sleeve", limb_rings(sp, ra, rb, 14), SUIT)
    hp = [Vector((side * .232, .905, -.034)), Vector((side * .233, .860, -.031)), Vector((side * .234, .815, -.028)),
          Vector((side * .235, .775, -.024)), Vector((side * .235, .735, -.020)), Vector((side * .234, .705, -.018))]
    # across the hand (x, thickness) and along the palm (z, width): the palm faces the thigh
    loft("hand", limb_rings(hp, [.024, .022, .020, .019, .016, .011], [.030, .040, .044, .043, .036, .022], 12, n=2.4),
         SKIN, cap_end=Vector((side * .233, .690, -.018)))

# ------------------------------------------------------------------ trousers (suit), straight 1990 cut with a break
for side in (-1.0, 1.0):
    LEG = [  # y, a, b, centre z, n, centre x
        (0.990, .084, .100, -.086, 2.2, side * .088),   # inside the jacket: never through its hips
        (0.900, .084, .094, -.084, 2.2, side * .093),
        (0.800, .082, .088, -.080, 2.2, side * .099),
        (0.680, .073, .078, -.074, 2.1, side * .101),
        (0.560, .065, .068, -.069, 2.0, side * .103),
        (0.480, .062, .064, -.068, 2.0, side * .104),
        (0.380, .059, .060, -.073, 2.0, side * .106),
        (0.260, .058, .059, -.080, 2.0, side * .108),
        (0.150, .058, .061, -.084, 2.0, side * .109),
        (0.085, .060, .066, -.083, 2.1, side * .110),
        (0.045, .062, .070, -.080, 2.1, side * .110),
    ]
    loft("leg", horizontal_rings(LEG, 16), SUIT)

# ------------------------------------------------------------------ shoes (dark): plain lace-ups, toes turned out 6 deg
for side in (-1.0, 1.0):
    SHOE = [  # z (along the foot), a (half width x), b (half height y), centre y
        (-.150, .030, .030, .034),
        (-.132, .037, .043, .045),
        (-.090, .041, .052, .053),
        (-.030, .044, .050, .051),
        (0.030, .049, .040, .040),
        (0.080, .050, .031, .031),
        (0.115, .044, .023, .024),
        (0.138, .028, .016, .018),
    ]
    rings = []
    for z, a, b, cy in SHOE:
        rings.append(superellipse_ring(Vector((0, cy, z)), Vector((1, 0, 0)), Vector((0, 1, 0)), a, b, 2.6, 14, -math.pi / 2.0))
    ang = math.radians(6.0) * side
    ca, sa = math.cos(ang), math.sin(ang)
    off = Vector((side * .110, 0, PLUMB_Z + .005))

    def place(p):
        x, z = p.x * ca + p.z * sa, -p.x * sa + p.z * ca
        return Vector((x, p.y, z)) + off

    rings = [[place(p) for p in r] for r in rings]
    loft("shoe", rings, DARK, cap_start=place(Vector((0, .034, -.153))), cap_end=place(Vector((0, .018, .141))))


# ------------------------------------------------------------------ Blender mesh
def to_blender(p):
    return Vector((-p.x, -p.z, p.y))


def build_object():
    me = bpy.data.meshes.new("FRBodyProxy")
    bm = bmesh.new()
    for part in parts:
        base = [bm.verts.new(to_blender(p)) for p in part["verts"]]
        bm.verts.ensure_lookup_table()
        # orientation: the first quad's normal must point away from the part's axis (right-handed in Blender's frame)
        flip = False
        f0 = part["faces"][len(part["faces"]) // 2]
        a, b, c = (to_blender(part["verts"][f0[i]]) for i in range(3))
        nrm = (b - a).cross(c - a)
        fc = sum((to_blender(part["verts"][i]) for i in f0), Vector()) / len(f0)
        ax = min((to_blender(q) for q in part["axis"]), key=lambda q: (q - fc).length)
        if nrm.dot(fc - ax) < 0:
            flip = True
        for f, m in zip(part["faces"], part["mats"]):
            vs = [base[i] for i in (reversed(f) if flip else f)]
            try:
                face = bm.faces.new(vs)
            except ValueError:
                continue
            face.material_index = m
            face.smooth = True
    bm.normal_update()
    bm.to_mesh(me)
    bm.free()
    for name, rgb in MATERIALS:
        mat = bpy.data.materials.new("FRBodyProxy_" + name)
        lin = [((c / 255.0 + .055) / 1.055) ** 2.4 for c in rgb]
        mat.diffuse_color = (lin[0], lin[1], lin[2], 1.0)
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = (lin[0], lin[1], lin[2], 1.0)
            bsdf.inputs["Roughness"].default_value = .8
        me.materials.append(mat)
    me.set_sharp_from_angle(angle=math.radians(55.0))
    ob = bpy.data.objects.new("FRBodyProxy", me)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def export(ob, root, out):
    me = ob.data
    me.calc_loop_triangles()
    cn = me.corner_normals
    keymap, verts, norms = {}, [], []
    subs = [[] for _ in MATERIALS]
    for tri in me.loop_triangles:
        idx = []
        for li in tri.loops:
            vi = me.loops[li].vertex_index
            pb = me.vertices[vi].co
            nb = cn[li].vector
            pu = (-pb.x, pb.z, -pb.y)
            nu = (-nb.x, nb.z, -nb.y)
            key = (round(pu[0] * 1000), round(pu[1] * 1000), round(pu[2] * 1000),
                   round(nu[0] * 127), round(nu[1] * 127), round(nu[2] * 127))
            if key not in keymap:
                keymap[key] = len(verts)
                verts.append(key[:3])
                norms.append(key[3:])
            idx.append(keymap[key])
        subs[tri.material_index].append((idx[0], idx[2], idx[1]))   # Blender CCW -> Unity CW
    lo = [min(v[i] for v in verts) for i in range(3)]
    hi = [max(v[i] for v in verts) for i in range(3)]
    tris = sum(len(s) for s in subs)
    stats = {"vertices": len(verts), "triangles": tris, "submesh_triangles": {MATERIALS[i][0]: len(s) for i, s in enumerate(subs)},
             "bounds_mm": {"min": lo, "max": hi}, "eye_mm": [0, int(EYE_Y * 1000), 0], "stature_m": STATURE, "plumb_z_m": PLUMB_Z,
             "max_radius_from_axis_m": round(max(math.hypot(v[0], v[2]) for v in verts) / 1000.0, 4),
             "front_of_face_at_eye_height_mm": max(v[2] for v in verts if abs(v[1] - 1620) <= 25 and abs(v[0]) < 25)}
    cs = os.path.join(root, "Assets", "Scripts", "Rendering", "GlassRT", "FrontRoomsGlassRTBodyProxyMesh.cs")

    def rows(vals, per=24):
        out_lines = []
        for i in range(0, len(vals), per):
            out_lines.append("        " + ", ".join(str(x) for x in vals[i:i + per]) + ",")
        return "\n".join(out_lines)

    flat_p = [c for v in verts for c in v]
    flat_n = [c for n in norms for c in n]
    lines = []
    lines.append("// GENERATED by Tools/rt/body_proxy/build_body_proxy.py (Blender " + bpy.app.version_string + "): do not edit by hand.")
    lines.append("// D2 (b) (g14-followup, Red 2026-10-22): the reflection-only player body for the ray-traced glass (FrontRoomsGlassRTBodyProxy.cs).")
    lines.append("// A 1990 office worker, 1.75 m, eye at (0, 1.62, 0), feet on y = 0, facing +Z; " + str(len(verts)) + " vertices, " + str(tris) + " triangles,")
    lines.append("// submeshes suit / dark (shoes, hair, tie) / skin / shirt. Positions in mm, normals x127. macOS only: nothing ships elsewhere.")
    lines.append("#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX")
    lines.append("using UnityEngine;")
    lines.append("using UnityEngine.Rendering;")
    lines.append("")
    lines.append("static class FrontRoomsGlassRTBodyProxyMesh")
    lines.append("{")
    lines.append("    public const int Vertices = " + str(len(verts)) + ", Triangles = " + str(tris) + ";")
    lines.append("")
    lines.append("    public static Mesh Build()")
    lines.append("    {")
    lines.append("        var n = P.Length / 3;")
    lines.append("        var pos = new Vector3[n];")
    lines.append("        var nrm = new Vector3[n];")
    lines.append("        for (var i = 0; i < n; i++)")
    lines.append("        {")
    lines.append("            pos[i] = new Vector3(P[3 * i] * .001f, P[3 * i + 1] * .001f, P[3 * i + 2] * .001f);")
    lines.append("            nrm[i] = new Vector3(N[3 * i], N[3 * i + 1], N[3 * i + 2]).normalized;")
    lines.append("        }")
    lines.append("        var m = new Mesh { name = \"FrontRooms glass RT body proxy\", indexFormat = IndexFormat.UInt16 };")
    lines.append("        m.vertices = pos;")
    lines.append("        m.normals = nrm;")
    lines.append("        m.subMeshCount = 4;")
    lines.append("        m.SetTriangles(Suit, 0);")
    lines.append("        m.SetTriangles(Dark, 1);")
    lines.append("        m.SetTriangles(Skin, 2);")
    lines.append("        m.SetTriangles(Shirt, 3);")
    lines.append("        m.RecalculateBounds();")
    lines.append("        return m;")
    lines.append("    }")
    lines.append("")
    lines.append("    static readonly short[] P =")
    lines.append("    {")
    lines.append(rows(flat_p))
    lines.append("    };")
    lines.append("")
    lines.append("    static readonly sbyte[] N =")
    lines.append("    {")
    lines.append(rows(flat_n))
    lines.append("    };")
    for name, s in zip(["Suit", "Dark", "Skin", "Shirt"], subs):
        lines.append("")
        lines.append("    static readonly int[] " + name + " =")
        lines.append("    {")
        lines.append(rows([i for t in s for i in t]))
        lines.append("    };")
    lines.append("}")
    lines.append("#endif")
    with open(cs, "w", encoding="ascii", newline="\n") as fh:
        fh.write("\n".join(lines) + "\n")
    os.makedirs(out, exist_ok=True)
    with open(os.path.join(out, "body_proxy.json"), "w", encoding="ascii", newline="\n") as fh:
        json.dump(stats, fh, indent=1)
    print("BODYPROXY", json.dumps(stats))
    return stats


def previews(ob, out):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.show_cavity = False
    scene.render.resolution_x, scene.render.resolution_y = 900, 1200
    scene.render.film_transparent = False
    if scene.world is None:
        scene.world = bpy.data.worlds.new("World")
    scene.world.color = (.80, .79, .76)
    scene.display.shading.background_type = "WORLD"
    scene.display_settings.display_device = "sRGB"
    scene.view_settings.view_transform = "Standard"
    cam_data = bpy.data.cameras.new("cam")
    cam = bpy.data.objects.new("cam", cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam
    shots = {  # name: (location in Unity frame, look-at, ortho)
        "front": (Vector((0, 0.90, 4.0)), Vector((0, 0.90, -0.08)), True),
        "side": (Vector((4.0, 0.90, -0.08)), Vector((0, 0.90, -0.08)), True),
        "back": (Vector((0, 0.90, -4.0)), Vector((0, 0.90, -0.08)), True),
        "three_quarter": (Vector((1.9, 1.35, 2.7)), Vector((0, 0.88, -0.08)), False),   # head to shoes in frame
        "mirror_eye": (Vector((0, 1.62, 3.0)), Vector((0, 1.30, 0.0)), False),
    }
    files = []
    for name, (loc, at, ortho) in shots.items():
        cam_data.type = "ORTHO" if ortho else "PERSP"
        cam_data.ortho_scale = 2.0
        cam_data.lens = 50
        lb, ab = to_blender(loc), to_blender(at)
        cam.location = lb
        cam.rotation_euler = (ab - lb).to_track_quat("-Z", "Y").to_euler()
        path = os.path.join(out, "body_proxy_" + name + ".png")
        scene.render.filepath = path
        bpy.ops.render.render(write_still=True)
        files.append(path)
    print("PREVIEWS", files)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    if len(argv) < 2:
        print("usage: Blender -b --factory-startup --python build_body_proxy.py -- <project root> <out dir> [--previews]")
        sys.exit(2)
    root, out = argv[0], argv[1]
    for ob in list(bpy.data.objects):
        bpy.data.objects.remove(ob, do_unlink=True)
    ob = build_object()
    export(ob, root, out)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out, "body_proxy.blend"))
    if "--previews" in argv:
        previews(ob, out)


main()
