"""The Run! T2 re-stack's pieces (71_run_room.md s.5.3, decision 5; card 2, the T2 body). PRODUCTION kits, pre-built
(the door break's rule D2: never runtime cutting; research/door_break/50_rethink.md s.3.7):

  Kit_RunRestack_Blocks   the end wall's 69 concrete masonry units (desktop and iPad), each its own object at three
                          hand-built levels: <name>_L0 (hero: two face shells with 3 mm chamfered arrises, the webs, one or
                          two hollow cores, mortar remnants on the beds and the head joints), _L1 (the shells and webs as
                          plain boxes: the cores still read from 10-28 m), _L2 (one box). lodDistances 10 / 28 / 0.
  Kit_RunRestack_Strips   phones (71 s.5.3: "half-course strips (about 28 pieces), no grit"): each bay's course as one
                          strip a side, the two courses over the lintel as half strips with the centre block alone; one
                          level (a 3 mm chamfered box), the joints from the texture.

Every object's origin is its piece's rest centre in the end's frame (Unity: x across, y up, z along the corridor, the front
face toward -z; _run_restack_common), so the run time hangs one bone per piece at that point. The painted faces carry
the end's own bond in metre UVs computed at the piece's REST position (front u = -x + 0.1, back u = x + 0.3, v = y), the
same as Kit_RunRecede_Wall's skins: the cassette swaps in at the tell without a seam (door break D2/D3). The raw faces
(beds, ends, cores) take RunB_BlockCore in a box projection with a per-piece offset; mortar remnants RunB_BlockMortar.
A real 1990 painted CMU wall: a 6 in block (the end is 0.16 m, the corridor's own wall thickness), painted both faces.

Run (headless, no sound):
  /Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup \
      --python Tools/Blender/frontrooms_kit/assets/run_restack_blocks.py -- [--out-root <unity project>]
Writes Assets/Resources/Props/Models/Kit_RunRestack_{Blocks,Strips}.fbx + .json (LF).
"""

import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))

import bmesh  # noqa: E402
import bpy  # noqa: E402

import _run_restack_common as RS  # noqa: E402

FS = 0.032          # face shell thickness (1 1/4 in)
WEB = 0.026         # web thickness
CHAMFER = 0.003     # the painted arris
WEB_BEVEL = 0.0015
LOD_DISTANCES = (10.0, 28.0, 0.0)
PREVIEW = {RS.SLOT_UPPER: (0.70, 0.69, 0.62), RS.SLOT_LOWER: (0.27, 0.30, 0.26), RS.SLOT_CORE: (0.42, 0.41, 0.38), RS.SLOT_MORTAR: (0.55, 0.53, 0.48)}


def U(x, y, z):
    """Unity end space -> Blender (Unity imports Blender (x, y, z) as (-x, z, -y): kitlib.export)."""
    return (-x, -z, y)


def jit(i, seed=0):
    """A stable value in [-1, 1] (no RNG state: a rebuild is byte-stable)."""
    h = (i * 2654435761 + seed * 40503 + 977) & 0xFFFFFFFF
    h ^= h >> 15
    h = (h * 2246822519) & 0xFFFFFFFF
    h ^= h >> 13
    return (h & 0xFFFF) / 32767.5 - 1.0


def material(slot):
    m = bpy.data.materials.get(slot)
    if m is None:
        m = bpy.data.materials.new(slot)
        c = PREVIEW.get(slot, (0.6, 0.6, 0.6))
        m.diffuse_color = (c[0], c[1], c[2], 1.0)
    return m


def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0


class Part(object):
    """Collects boxes (in the piece's local Unity space) into one bmesh with per-face slots and UVs."""

    def __init__(self, piece, centre):
        self.bm = bmesh.new()
        self.uv = self.bm.loops.layers.uv.verify()
        self.piece = piece
        self.cx, self.cy = centre
        self.slots = []
        self.paint = RS.SLOT_LOWER if piece["lower"] else RS.SLOT_UPPER
        self.ox = 0.37 * (jit(piece["index"], 3) + 1.0)
        self.oy = 0.37 * (jit(piece["index"], 4) + 1.0)

    def slot_index(self, slot):
        if slot not in self.slots:
            self.slots.append(slot)
        return self.slots.index(slot)

    def box(self, x0, x1, y0, y1, z0, z1, bevel=0.0, kind="shell", lump=False):
        """An axis box (local Unity metres), bevelled (1 segment), its faces classified: a shell's outer face and the
        chamfers leaning to it are paint; every other face raw block; lumps are mortar."""
        bm2 = bmesh.new()
        vs = [bm2.verts.new(U(x, y, z)) for (x, y, z) in
              ((x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0), (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1))]
        for q in ((0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (3, 7, 6, 2), (0, 4, 7, 3), (1, 2, 6, 5)):
            bm2.faces.new([vs[i] for i in q])
        bmesh.ops.recalc_face_normals(bm2, faces=bm2.faces)
        if bevel > 0.0:
            bmesh.ops.bevel(bm2, geom=list(bm2.edges) + list(bm2.verts), offset=bevel, offset_type="OFFSET", segments=1,
                            profile=0.5, affect="EDGES", clamp_overlap=True)
        bm2.normal_update()
        self._append(bm2, kind, lump)
        bm2.free()

    def fillet(self, xc, zc, sx, sz, r, y0, y1, segs=3):
        """A core's rounded corner (a 1990 CMU's cores are cast round-cornered): the concave fillet between a web face
        (x = xc) and a shell's inner face (z = zc), radius r, run the piece's height. Only the curved faces and the two bed
        caps are built (the flat sides lie on the web and the shell)."""
        bm2 = bmesh.new()
        cxr, czr = xc + sx * r, zc + sz * r
        prof = [(xc, zc)]
        for i in range(segs + 1):
            a = (math.pi / 2.0) * i / segs
            # from (xc + sx r, zc) round to (xc, zc + sz r) about (cxr, czr)
            prof.append((cxr - sx * r * math.sin(a), czr - sz * r * math.cos(a)))
        lo = [bm2.verts.new(U(x, y0, z)) for (x, z) in prof]
        hi = [bm2.verts.new(U(x, y1, z)) for (x, z) in prof]
        n = len(prof)
        for i in range(1, n - 1):
            bm2.faces.new([lo[i], lo[i + 1], hi[i + 1], hi[i]])
        bm2.faces.new(lo)
        bm2.faces.new(list(reversed(hi)))
        bmesh.ops.recalc_face_normals(bm2, faces=bm2.faces)
        bm2.normal_update()
        self._append(bm2, "web", False)
        bm2.free()

    def lump(self, x0, x1, y0, y1, z0, z1, axis, sign, seed):
        """A torn mortar remnant inside the given box: an irregular six-sided mound on the bed (or head) face, its base a
        jittered hexagon over the box's footprint, its top a smaller jittered hexagon at 60-100 % of the box's height (the
        outer side is +axis when sign > 0). 20 tris."""
        a = {"x": 0, "y": 1, "z": 2}[axis]
        lo = [x0, y0, z0]
        hi = [x1, y1, z1]
        u, v = [i for i in range(3) if i != a]
        cu, cv = (lo[u] + hi[u]) / 2.0, (lo[v] + hi[v]) / 2.0
        ru, rv = (hi[u] - lo[u]) / 2.0, (hi[v] - lo[v]) / 2.0
        base = lo[a] if sign > 0 else hi[a]
        top = hi[a] if sign > 0 else lo[a]
        bm2 = bmesh.new()
        ring_b, ring_t = [], []
        ang0 = jit(seed, 13) * 0.5
        for k in range(6):
            ang = ang0 + k * math.pi / 3.0
            rr = 0.72 + 0.28 * (jit(seed * 6 + k, 14) + 1.0) / 2.0
            pb = [0.0, 0.0, 0.0]
            pb[u] = cu + math.cos(ang) * ru * rr
            pb[v] = cv + math.sin(ang) * rv * rr
            pb[a] = base
            pt = list(pb)
            shrink = 0.55 + 0.25 * (jit(seed * 6 + k, 15) + 1.0) / 2.0
            pt[u] = cu + (pb[u] - cu) * shrink
            pt[v] = cv + (pb[v] - cv) * shrink
            pt[a] = base + (top - base) * (0.6 + 0.4 * (jit(seed * 6 + k, 16) + 1.0) / 2.0)
            ring_b.append(bm2.verts.new(U(*pb)))
            ring_t.append(bm2.verts.new(U(*pt)))
        for k in range(6):
            k1 = (k + 1) % 6
            bm2.faces.new([ring_b[k], ring_b[k1], ring_t[k1], ring_t[k]])
        bm2.faces.new(ring_t)
        bm2.faces.new(list(reversed(ring_b)))
        bmesh.ops.recalc_face_normals(bm2, faces=bm2.faces)
        bm2.normal_update()
        self._append(bm2, "lump", True)
        bm2.free()

    def _append(self, bm2, kind, lump):
        vmap = {}
        for v in bm2.verts:
            vmap[v] = self.bm.verts.new(v.co)
        d = self.piece["depth"]
        for f in bm2.faces:
            nf = self.bm.faces.new([vmap[v] for v in f.verts])
            nb = f.normal
            n = (-nb.x, nb.z, -nb.y)          # Blender -> Unity direction
            centre = f.calc_center_median()
            cz = -centre.y                     # Unity z of the face centre
            if lump:
                slot = RS.SLOT_MORTAR
            elif kind == "shell" and n[2] < -0.38 and cz < -d + FS * 0.5:
                slot = self.paint
            elif kind == "shell" and n[2] > 0.38 and cz > d - FS * 0.5:
                slot = self.paint
            elif kind == "solid" and abs(n[2]) > 0.38:
                slot = self.paint
            else:
                slot = RS.SLOT_CORE
            nf.material_index = self.slot_index(slot)
            for loop in nf.loops:
                b = loop.vert.co
                X, Y, Z = -b.x + self.cx, b.z + self.cy, -b.y      # end space at rest
                if slot == self.paint:
                    loop[self.uv].uv = RS.face_uv(X, Y, n[2] < 0.0)
                elif abs(n[0]) >= abs(n[1]) and abs(n[0]) >= abs(n[2]):
                    loop[self.uv].uv = (Z + self.ox, Y + self.oy)
                elif abs(n[1]) >= abs(n[2]):
                    loop[self.uv].uv = (X + self.ox, Z + self.oy)
                else:
                    loop[self.uv].uv = (X + self.ox, Y + self.oy)

    def finish(self, name):
        self.bm.normal_update()
        me = bpy.data.meshes.new(name)
        self.bm.to_mesh(me)
        self.bm.free()
        for s in self.slots:
            me.materials.append(material(s))
        obj = bpy.data.objects.new(name, me)
        bpy.context.scene.collection.objects.link(obj)
        obj.location = U(self.cx, self.cy, 0.0)
        me.shade_smooth()
        me.set_sharp_from_angle(angle=math.radians(35.0))
        return obj


def cores(L):
    """The webs' x spans (local, centred) and the number of cores for a piece of length L."""
    n = 2 if L >= 0.30 else (1 if L >= 0.12 else 0)
    if n == 0:
        return [], 0
    webs = []
    # end webs at both ends, n - 1 inner webs evenly between
    inner = (L - (n + 1) * WEB) / n
    x = -L / 2.0
    for k in range(n + 1):
        webs.append((x, x + WEB))
        x += WEB + inner
    return webs, n


def build_piece(p, lv):
    L = p["x1"] - p["x0"]
    H = p["y1"] - p["y0"]
    d = p["depth"]
    cx, cy = (p["x0"] + p["x1"]) / 2.0, (p["y0"] + p["y1"]) / 2.0
    part = Part(p, (cx, cy))
    hx, hy = L / 2.0, H / 2.0
    if lv == 2:
        part.box(-hx, hx, -hy, hy, -d, d, 0.0, "solid")
        return part.finish("%s_L2" % p["name"])
    webs, n = cores(L)
    bev = CHAMFER if lv == 0 else 0.0
    if n == 0:
        part.box(-hx, hx, -hy, hy, -d, d, bev, "solid")
    else:
        part.box(-hx, hx, -hy, hy, -d, -d + FS, bev, "shell")
        part.box(-hx, hx, -hy, hy, d - FS, d, bev, "shell")
        e = 0.002 if lv == 0 else 0.0
        for (a, b) in webs:
            part.box(a, b, -hy, hy, -d + FS - e, d - FS + e, WEB_BEVEL if lv == 0 else 0.0, "web")
        if lv == 0:
            r = 0.020
            for k in range(len(webs) - 1):
                xa, xb = webs[k][1], webs[k + 1][0]
                if xb - xa < 2.5 * r:
                    continue
                for (xc, sx) in ((xa, 1), (xb, -1)):
                    for (zc, sz) in ((-d + FS, 1), (d - FS, -1)):
                        part.fillet(xc, zc, sx, sz, r, -hy, hy)
    if lv == 0:
        i = p["index"]
        # mortar remnants: two on each shell's two beds, one on each head joint (the 9.5 mm joint's mortar, torn)
        for si, (z0, z1) in enumerate(((-d + 0.004, -d + FS + 0.006), (d - FS - 0.006, d - 0.004))):
            for bed, sign in ((hy, 1), (-hy, -1)):
                for m in range(2):
                    seed = i * 37 + si * 11 + (0 if sign > 0 else 5) + m * 3
                    span = 0.03 + 0.05 * (jit(seed, 1) + 1.0) / 2.0
                    span = min(span, L * 0.4)
                    c = (-hx + span / 2 + 0.008) + (L - span - 0.016) * ((jit(seed, 2) + 1.0) / 2.0 if m == 0 else (jit(seed, 2) + 1.0) / 4.0 + 0.5)
                    c = max(-hx + span / 2 + 0.006, min(hx - span / 2 - 0.006, c))
                    h = 0.0015 + 0.003 * (jit(seed, 5) + 1.0) / 2.0
                    if sign > 0:
                        part.lump(c - span / 2, c + span / 2, bed - 0.0005, bed + h, z0, z1, "y", 1, seed)
                    else:
                        part.lump(c - span / 2, c + span / 2, bed - h, bed + 0.0005, z0, z1, "y", -1, seed)
        for end, sign in ((hx, 1), (-hx, -1)):
            seed = i * 53 + (7 if sign > 0 else 13)
            h = 0.0012 + 0.0025 * (jit(seed, 6) + 1.0) / 2.0
            y0 = -hy + H * 0.15 * (jit(seed, 7) + 1.0)
            y1 = min(hy - 0.01, y0 + H * (0.35 + 0.2 * (jit(seed, 8) + 1.0) / 2.0))
            z0 = -d + 0.006
            z1 = z0 + FS + 0.004 if jit(seed, 9) < 0 else d - 0.006
            if sign > 0:
                part.lump(end - 0.0005, end + h, y0, y1, z0, z1, "x", 1, seed)
            else:
                part.lump(end - h, end + 0.0005, y0, y1, z0, z1, "x", -1, seed)
    return part.finish("%s_L%d" % (p["name"], lv))


def build_strip(s):
    L = s["x1"] - s["x0"]
    H = s["y1"] - s["y0"]
    d = s["depth"]
    cx, cy = (s["x0"] + s["x1"]) / 2.0, (s["y0"] + s["y1"]) / 2.0
    part = Part(s, (cx, cy))
    part.box(-L / 2, L / 2, -H / 2, H / 2, -d, d, CHAMFER, "solid")
    return part.finish(s["name"])


def tris(obj):
    return sum(len(f.vertices) - 2 for f in obj.data.polygons)


def export(objs, fbx, sidecar):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.export_scene.fbx(
        filepath=fbx, use_selection=True, object_types={"MESH"}, apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z", axis_up="Y", bake_space_transform=True, mesh_smooth_type="OFF", use_mesh_modifiers=True,
        use_tspace=True, add_leaf_bones=False, bake_anim=False, path_mode="STRIP")
    with open(sidecar, "w", newline="\n") as f:
        json.dump(sidecar_data, f, indent=1)
        f.write("\n")


sidecar_data = {}


def main(argv):
    global sidecar_data
    out_root = os.path.abspath(os.path.join(HERE, "..", "..", "..", ".."))
    i = 0
    while i < len(argv):
        if argv[i] == "--out-root":
            out_root = argv[i + 1]; i += 2; continue
        i += 1
    out = os.path.join(out_root, "Assets", "Resources", "Props", "Models")
    os.makedirs(out, exist_ok=True)

    # ---- the blocks
    reset_scene()
    ps = RS.pieces()
    objs, rows, t = [], [], [0, 0, 0]
    for p in ps:
        per = []
        for lv in (0, 1, 2):
            o = build_piece(p, lv)
            objs.append(o)
            per.append(tris(o))
            t[lv] += per[-1]
        rows.append({"name": p["name"], "index": p["index"], "course": p["course"], "side": p["side"], "k": p["k"], "kn": p["kn"],
                     "x0": p["x0"], "x1": p["x1"], "y0": p["y0"], "y1": p["y1"], "lower": p["lower"], "depth": p["depth"], "tris": per})
    sidecar_data = {
        "name": RS.BLOCKS, "slots": list(RS.SLOTS), "boundsMin": [-RS.SKIN_IN, RS.SILL_TOP, -0.0865], "boundsMax": [RS.SKIN_IN, RS.GIRDER, 0.0865],
        "triangles": t[0], "trianglesLod1": t[1], "trianglesLod2": t[2], "lodDistances": list(LOD_DISTANCES), "lodHandBuilt": True,
        "supports": [], "colliders": [], "noCollider": True, "anchors": [], "frontAxis": "+Z", "tags": ["run", "run_recede", "restack"],
        "placement": "Floor", "service": 0.0, "minCeiling": 2.9,
        "bond": {"uFront": RS.U_FRONT, "uBack": RS.U_BACK, "tile": RS.TILE, "course": RS.COURSE},
        "levels": "each piece <name>_L0 / _L1 / _L2, origin at its rest centre",
        "pieces": rows,
        "runRecede": "card 2 T2 body, 2026-11-04 (71_run_room.md s.5.3): 69 CMU pieces; inert until rule 5b",
    }
    export(objs, os.path.join(out, RS.BLOCKS + ".fbx"), os.path.join(out, RS.BLOCKS + ".json"))
    print("[restack] %s: %d pieces, tris LOD0 %d / LOD1 %d / LOD2 %d" % (RS.BLOCKS, len(ps), t[0], t[1], t[2]))
    for lv in (0, 1, 2):
        mx = max(rows, key=lambda r: r["tris"][lv])
        print("[restack]   L%d max %d (%s), min %d" % (lv, mx["tris"][lv], mx["name"], min(r["tris"][lv] for r in rows)))

    # ---- the strips (phones)
    reset_scene()
    ss = RS.strips()
    objs, rows, total = [], [], 0
    for s in ss:
        o = build_strip(s)
        objs.append(o)
        n = tris(o)
        total += n
        rows.append({"name": s["name"], "index": s["index"], "course": s["course"], "side": s["side"], "k": s["k"], "kn": s["kn"],
                     "x0": s["x0"], "x1": s["x1"], "y0": s["y0"], "y1": s["y1"], "lower": s["lower"], "depth": s["depth"],
                     "members": s["members"], "tris": [n, n, n]})
    sidecar_data = {
        "name": RS.STRIPS, "slots": [RS.SLOT_UPPER, RS.SLOT_LOWER, RS.SLOT_CORE], "boundsMin": [-RS.SKIN_IN, RS.SILL_TOP, -0.0835],
        "boundsMax": [RS.SKIN_IN, RS.GIRDER, 0.0835], "triangles": total, "lodDistances": [0.0], "supports": [], "colliders": [],
        "noCollider": True, "anchors": [], "frontAxis": "+Z", "tags": ["run", "run_recede", "restack", "phone"], "placement": "Floor",
        "service": 0.0, "minCeiling": 2.9, "bond": {"uFront": RS.U_FRONT, "uBack": RS.U_BACK, "tile": RS.TILE, "course": RS.COURSE},
        "levels": "one level, each strip its own object, origin at its rest centre", "pieces": rows,
        "runRecede": "card 2 T2 body, 2026-11-04 (71_run_room.md s.5.3): phones' 28 half-course strips; inert until rule 5b",
    }
    export(objs, os.path.join(out, RS.STRIPS + ".fbx"), os.path.join(out, RS.STRIPS + ".json"))
    print("[restack] %s: %d strips, %d tris" % (RS.STRIPS, len(ss), total))


if __name__ == "__main__":
    main(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
