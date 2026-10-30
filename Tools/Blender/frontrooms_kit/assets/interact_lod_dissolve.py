"""LOD1 / LOD2 by limited dissolve (D1.6 fix 1, 2026-10-22; Red's call 8: LOD0/1/2 on every model).

NOT AN ASSET: no NAME. The lock, key, tag, key-host, door-sign and number-plate modules append

    import interact_lod_dissolve as ld
    _build_lod0 = build
    def build(kit):
        _build_lod0(kit)
        ld.use(kit, LOD1_RATIO, LOD2_RATIO)

so build_asset.py (unchanged) calls this module's make_lod1 on the kit instead of kitlib's collapse (kitlib.py
unchanged). Why: kitlib's collapse decimation protects every part thinner than 12 mm (fr_lod_keep), and these
parts are all thinner than 12 mm, so their LOD1 came out equal to LOD0 (escutcheon 3,248 / 3,248) and no LOD2 was
made; the 2026-10-08 fix pass then kept LOD1 = LOD0 on purpose (dc.lod1_keep_all) because an unprotected collapse
moved shapes 5-34 px.

Limited dissolve keeps the shape: it only merges faces whose dihedral angle is below a limit and removes
vertices on nearly straight edge chains, never across a sharp edge (the kit's 35 deg smoothing split), a material
border, a UV seam or a mark seam. So a 96-segment lathe becomes a 24- or 12-segment one, flats stay flat, and the
printed inserts (one quad each, their own UVs) stay. The limit is searched per level for the module's own
lodRatios target (sidecar "lodRatios"), capped (LOD1 at 20 deg; LOD2 at 60 deg and free to cross sharp splits and UV seams, not material borders) so a part that cannot reach its
target keeps its shape instead. LOD1 drops the parts flagged fr_lod1_drop; LOD2 also fr_lod2_drop (screws, pins).
Normals are re-smoothed with the kit's angle. The sidecar records what was built ("lodBuild").
"""

import math

import bmesh
import bpy

LOD1_MAX_DEG = 20.0
LOD2_MAX_DEG = 60.0


# Main's kitlib.py (2026-10-21 21:37) has no Kit.inspect, yet interact_door_sign.py, the three key tags and
# evac_placard.py call it (the EXAMINE block writer lives only in the inspect clones' kitlib, research/inspect
# 10_design.md §3.8), so those modules cannot be rebuilt from main. Until kitlib gets it back, this supplies the same
# block when missing, writing the aim box straight in Unity part-local units as the sidecars in main hold it
# ("box": {"c", "s"}); kitlib.py itself is not edited.
import kitlib  # noqa: E402

if not hasattr(kitlib.Kit, "inspect"):
    _INSPECT_KINDS = ("flat", "panel", "object")
    _INSPECT_FIELDS = ("verb", "face", "faceDir", "up", "size", "margin", "screen", "dist", "distMaxA", "minDist", "maxDist",
                       "maxAngle", "maxYawOff", "fovDelta", "reach", "approachAngle", "orbit", "aimPad", "texelsPerMetre",
                       "regions", "dof", "transcript", "sound")

    def _inspect(self, kind, box=None, ship=False, **kw):
        if kind not in _INSPECT_KINDS:
            raise ValueError("inspect kind %r" % kind)
        block = {"kind": kind}
        for k, v in kw.items():
            if k not in _INSPECT_FIELDS:
                raise ValueError("inspect field %r" % k)
            block[k] = list(v) if isinstance(v, tuple) else v
        if box is not None:
            c, sz = box
            block["box"] = {"c": [round(-c[0], 6), round(c[2], 6), round(-c[1], 6)], "s": [round(sz[0], 6), round(sz[2], 6), round(sz[1], 6)]}
        block["ship"] = bool(ship)
        self.meta["inspect"] = block

    kitlib.Kit.inspect = _inspect


def _tris(obj):
    return sum(len(p.vertices) - 2 for p in obj.data.polygons)


def _copy(obj):
    o = obj.copy()
    o.data = obj.data.copy()
    bpy.context.scene.collection.objects.link(o)
    return o


def _drop_groups(obj, names):
    idx = [obj.vertex_groups[n].index for n in names if n in obj.vertex_groups]
    if not idx:
        return
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    deform = bm.verts.layers.deform.verify()
    doomed = [v for v in bm.verts if any(i in v[deform] for i in idx)]
    if doomed:
        bmesh.ops.delete(bm, geom=doomed, context="VERTS")
    bm.to_mesh(obj.data)
    bm.free()


LOD1_DELIMIT = {"MATERIAL", "SEAM", "SHARP", "UV"}
LOD2_DELIMIT = {"MATERIAL", "SEAM"}       # 4-12 m and beyond: sharp splits and UV seams may go, material borders stay


def _dissolved(src, drop, deg, smooth_angle, delimit=LOD1_DELIMIT):
    o = _copy(src)
    _drop_groups(o, drop)
    if deg > 0.0:
        bm = bmesh.new()
        bm.from_mesh(o.data)
        bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(deg), use_dissolve_boundaries=False,
                                 verts=list(bm.verts), edges=list(bm.edges), delimit=set(delimit))
        bmesh.ops.triangulate(bm, faces=list(bm.faces), quad_method="BEAUTY", ngon_method="BEAUTY")
        bm.to_mesh(o.data)
        bm.free()
    o.data.shade_smooth()
    o.data.set_sharp_from_angle(angle=math.radians(smooth_angle))
    return o


def _level(src, drop, target, max_deg, smooth_angle, delimit=LOD1_DELIMIT):
    """The smallest dissolve limit (0.5 deg steps) that reaches the target tris, or max_deg."""
    lo_deg = 0.5
    best = None
    deg = lo_deg
    while deg <= max_deg + 1e-6:
        o = _dissolved(src, drop, deg, smooth_angle, delimit)
        n = _tris(o)
        if best is not None:
            bpy.data.objects.remove(best[0], do_unlink=True)
        best = (o, deg, n)
        if n <= target:
            break
        deg += 0.5 if deg < 5.0 else (1.0 if deg < 20.0 else 2.5)
    return best


def use(kit, r1, r2, max1=LOD1_MAX_DEG, max2=LOD2_MAX_DEG):
    """Replace this kit instance's make_lod1 (called by build_asset.py after finish()) with the dissolve LODs."""
    r1 = r1 if r1 else 0.4
    r2 = r2 if r2 else 0.05

    def make_lod1(_ratio=None):
        src = kit.object
        smooth = getattr(kit, "smooth_angle", 35.0)
        n0 = kit.meta.get("triangles") or _tris(src)
        o1, d1, n1 = _level(src, ("fr_lod1_drop",), n0 * r1, max1, smooth)
        o2, d2, n2 = _level(src, ("fr_lod1_drop", "fr_lod2_drop"), n0 * r2, max2, smooth, LOD2_DELIMIT)
        if n2 >= n1:   # a part too simple to reduce twice: LOD2 = LOD1 without its dropped parts
            bpy.data.objects.remove(o2, do_unlink=True)
            o2, d2, n2 = _level(src, ("fr_lod1_drop", "fr_lod2_drop"), 0, max2, smooth, LOD2_DELIMIT)
        kit.lod1 = None
        kit.lods = {1: o1, 2: o2}
        kit.object.name = kit.name + "_LOD0"
        for lv, obj in sorted(kit.lods.items()):
            obj.name = obj.data.name = "%s_LOD%d" % (kit.name, lv)
            kit.meta["trianglesLod%d" % lv] = _tris(obj)
        kit.meta["lodBuild"] = {"method": "limited dissolve (interact_lod_dissolve.py, D1.6 fix 1)",
                                "targets": [round(n0 * r1), round(n0 * r2)], "angleDeg": [d1, d2], "tris": [n0, n1, n2]}
        print("[lod] %s: LOD0 %d, LOD1 %d (dissolve %.1f deg, target %d), LOD2 %d (%.1f deg, target %d)"
              % (kit.name, n0, n1, d1, n0 * r1, n2, d2, n0 * r2))

    kit.make_lod1 = make_lod1
    kit.make_lods = lambda ratios=None: make_lod1()
    return kit
