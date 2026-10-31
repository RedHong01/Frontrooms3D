"""Shared code for the door-break families built FROM THE INTACT KIT (production P3 steel leaves and P4 frame strike-zone
inserts, 2026-10-30; research/door_break/50_rethink.md 3.5-3.7, 52_production.md).

NOT AN ASSET: no NAME and no build(). The P1 veneer family lofts its own body from the D1.6 section; P3 and P4 instead run
the intact D1.6 generator in this Blender (kitlib, byte-identical recipe), take its finished LOD meshes and cut them with
planes (bmesh bisect, no booleans):
  body     the intact mesh minus a window (every face whose centre is inside it): the same vertices, UVs, fr_wear colours,
           kick plates, pivot plates and screws as the intact kit, normals rebuilt with its recipe, so the swap changes only
           the window;
  window   the faces inside, refined by a plane grid (only the slots that bend) and then deformed per stage by analytic
           fields (bow, dish, crush, peel for the steel leaf; split, dent, flare, fin bend for the frames). The window's
           boundary vertices sit where the fields are zero, so body + window close exactly.
Pieces are bmesh copies (Piece) with their slot names; Geo parts (interact_door_break_common: leak cards, debris, teeth)
export beside them. Frames: Unity door root (+X face S = the canonical burst face, +Y up, +Z along the opening);
Blender = dc.U(Unity).
"""

import math

import bmesh
import bpy

import interact_door_break_common as bc
import kitlib


def to_unity(co):
    return (-co[0], co[2], -co[1])


def to_bl(p):
    return (-p[0], -p[2], p[1])


def s3(t):
    """Smoothstep on [0, 1] (zero slope at both ends)."""
    t = max(0.0, min(1.0, t))
    return t * t * (3.0 - 2.0 * t)


def bump(y, a, b, pk):
    """0 at a and b (zero slope), 1 at pk."""
    if y <= a or y >= b:
        return 0.0
    if y <= pk:
        return s3((y - a) / max(1e-9, pk - a))
    return s3((b - y) / max(1e-9, b - pk))


class Piece:
    """A bmesh and its slot names (material index -> slot)."""

    def __init__(self, bm, slots):
        self.bm = bm
        self.slots = list(slots)
        # every cut piece rebuilds its normals with the kit's recipe: a plane cut interpolates the kit's ENCODED custom
        # normals at the new vertices, which darkened the body below the window by 8 levels at the swap (smoke3 2026-10-30)
        self.renormal = True

    def copy(self):
        p = Piece(self.bm.copy(), self.slots)
        p.renormal = self.renormal
        return p

    def tris(self):
        return sum(len(f.verts) - 2 for f in self.bm.faces)

    def slot(self, name):
        if name not in self.slots:
            self.slots.append(name)
        return self.slots.index(name)

    def faces_of(self, names):
        idx = {i for i, s in enumerate(self.slots) if s in names}
        return [f for f in self.bm.faces if f.material_index in idx]


def build_intact(module):
    """Run an intact kit generator (kitlib, as build_asset.py does) and return {level: Piece} of its finished LOD meshes."""
    kit = kitlib.Kit(module.NAME)
    module.build(kit)
    kit.finish(getattr(module, "SMOOTH_ANGLE", 35.0))
    kit.make_lods(getattr(module, "LODS", None))
    objs = {0: kit.object}
    for lv, o in kit.lods.items():
        if o is not None:
            objs[lv] = o
    out = {}
    for lv, o in objs.items():
        bm = bmesh.new()
        bm.from_mesh(o.data)
        out[lv] = Piece(bm, [m.name if m is not None else "Prop_Default" for m in o.data.materials])
    return out, kit


def _plane(axis, value):
    if axis == "X":
        return (-value, 0.0, 0.0), (1.0, 0.0, 0.0)
    if axis == "Y":
        return (0.0, 0.0, value), (0.0, 0.0, 1.0)
    return (0.0, -value, 0.0), (0.0, 1.0, 0.0)


def cut(piece, axis, value, faces=None):
    """Bisect by the Unity plane axis = value (all geometry, or only these faces)."""
    bm = piece.bm
    if faces is None:
        geom = list(bm.verts) + list(bm.edges) + list(bm.faces)
    else:
        fs = list(faces)
        if not fs:
            return
        es = {e for f in fs for e in f.edges}
        vs = {v for f in fs for v in f.verts}
        geom = list(vs) + list(es) + fs
    co, no = _plane(axis, value)
    bmesh.ops.bisect_plane(bm, geom=geom, dist=1e-7, plane_co=co, plane_no=no, clear_inner=False, clear_outer=False)


def centre(f):
    return to_unity(f.calc_center_median())


def split(piece, inside):
    """(outside, inside) copies by each face's centre (Unity) and the face: inside(c, f) -> bool."""
    bm = piece.bm
    bm.faces.ensure_lookup_table()
    flags = [bool(inside(centre(f), f)) for f in bm.faces]
    out = []
    for keep in (False, True):
        pc = piece.copy()
        pc.bm.faces.ensure_lookup_table()
        doomed = [pc.bm.faces[i] for i, fl in enumerate(flags) if fl != keep]
        if doomed:
            bmesh.ops.delete(pc.bm, geom=doomed, context="FACES")
        out.append(pc)
    return out[0], out[1]


def delete_faces(piece, pred):
    doomed = [f for f in piece.bm.faces if pred(centre(f), f)]
    if doomed:
        bmesh.ops.delete(piece.bm, geom=doomed, context="FACES")
    return len(doomed)


def grid(piece, axis, values, faces_fn):
    """Bisect by several planes, each over the faces faces_fn() returns now (the cut faces are new faces)."""
    for v in values:
        cut(piece, axis, v, faces_fn())


def deform(piece, fn):
    """fn(P_unity, vert) -> P_unity for every vertex (fields read the ORIGINAL position)."""
    for v in piece.bm.verts:
        v.co = to_bl(fn(to_unity(v.co), v))
    piece.renormal = True


def weld_rip(piece, edges):
    """Disconnect a chain of edges (a crack), so the faces either side move apart; the chain's ends stay shared."""
    if edges:
        bmesh.ops.split_edges(piece.bm, edges=list(edges))


# ===================================================================== export
def to_object(piece, name, smooth_angle=35.0):
    """A Blender object from a Piece, its normals rebuilt with the kit's own recipe (interact_door_common.finalize: smooth,
    sharp by angle, a face-area weighted normal keeping sharp edges), so an uncut face shades exactly as the intact kit's."""
    me = bpy.data.meshes.new(name)
    big = [f for f in piece.bm.faces if len(f.verts) > 4]
    if big:
        # the plane cuts leave n-gons; the FBX exporter writes no tangents for them (the kit recipe triangulates too)
        bmesh.ops.triangulate(piece.bm, faces=big, quad_method="BEAUTY", ngon_method="BEAUTY")
    piece.bm.to_mesh(me)
    for s in piece.slots:
        me.materials.append(bc.material(s))
    obj = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(obj)
    if piece.renormal:
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        if me.has_custom_normals:
            bpy.ops.mesh.customdata_custom_splitnormals_clear()
        me.shade_smooth()
        me.set_sharp_from_angle(angle=math.radians(smooth_angle))
        m = obj.modifiers.new("wn", "WEIGHTED_NORMAL")
        m.mode = "FACE_AREA"
        m.weight = 50
        m.keep_sharp = True
        bpy.ops.object.modifier_apply(modifier=m.name)
    return obj


def export(models, name, pieces, geos=None, meta=None, tags=()):
    """One FBX + sidecar: pieces {part name: Piece}, geos {part name: (Geo, origin)} (interact_door_break_common)."""
    import json
    import os
    bc.reset_scene()
    objs = []
    tris = {}
    for n, p in pieces.items():
        if p is None or len(p.bm.faces) == 0:
            continue
        objs.append(to_object(p, n))
        tris[n] = p.tris()
    for n, (g, o) in (geos or {}).items():
        if g is None or not g.f:
            continue
        objs.append(bc.to_blender(g, n, origin=o))
        tris[n] = g.tris()
    bc.export_fbx(objs, os.path.join(models, name + ".fbx"))
    slots = set()
    for o in objs:
        for m in o.data.materials:
            if m is not None:
                slots.add(m.name)
    side = {
        "name": name,
        "slots": sorted(slots),
        "anchors": [],
        "tags": ["door", "door_break"] + list(tags),
        "noCollider": True,
        "colliders": [],
        "frontAxis": "+Z",
        "lodDistances": [4.0, 12.0, -1.0],
        "triangles": 0, "trianglesLod1": 0,
        "partTris": [{"name": k, "tris": v} for k, v in sorted(tris.items())],
    }
    side.update(meta or {})
    with open(os.path.join(models, name + ".json"), "w", newline="\n") as f:
        json.dump(side, f, indent=1)
    return tris
