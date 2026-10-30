"""Kit_JacketDraped: the guard's uniform jacket left on his chair (30L S23, §10.3 S23: draped, sloped shoulders, a sleeve
over the arm, uniform tone #59647A, a blank shoulder patch; NN §2 the jacket; plan 70 P14).

Cloth-baked over Kit_TaskChair's back (the same chair mesh as Kit_TaskChair_Charcoal): the body panel is a cloth sheet
pinned along the backrest's top (the shoulders' line) and dropped under gravity onto the chair's collision mesh, so it
folds over the backrest, front shorter than back; one sleeve is a second cloth strip pinned at the shoulder that falls
over the right armrest. Both are given a 5 mm thickness after the bake. A blank embroidered patch sits on the sleeve's
shoulder (no lettering). Falls back to a procedural drape if the chair FBX is missing.
ORIGIN = the chair's origin (the floor under it): the module places it at the chair's pivot and yaw. Top 0.953 + cloth.
Budget 8,000 / 2,500 / 300. Slots Prop_FabricUniform (#59647A), Prop_FabricNavy (patch). No collider.
"""

import math
import os

import bmesh
import bpy
from mathutils import Vector

import _station_common as sc

NAME = "Kit_JacketDraped"
LODS = (2500 / 8000, 300 / 8000)
SMOOTH_ANGLE = 80.0

HERE = os.path.dirname(os.path.abspath(__file__))
CHAIR_FBX = os.path.join(HERE, "..", "..", "..", "..", "Assets", "Resources", "Props", "Models", "Kit_TaskChair.fbx")
BACK_TOP_Y, BACK_Z = 0.953, -0.328


def grid(name, w, l, nx, nl, origin_u, along="z"):
    """A cloth sheet in Unity space: w along x, l from origin_u along -z (backwards) when along == 'z'."""
    bm = bmesh.new()
    rows = []
    for j in range(nl + 1):
        row = []
        for i in range(nx + 1):
            x = origin_u[0] + (i / nx - 0.5) * w
            z = origin_u[2] + (j / nl - 0.5) * l
            row.append(bm.verts.new(sc.B(x, origin_u[1], z)))
        rows.append(row)
    for j in range(nl):
        for i in range(nx):
            bm.faces.new((rows[j][i], rows[j][i + 1], rows[j + 1][i + 1], rows[j + 1][i]))
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def pin(ob, test):
    vg = ob.vertex_groups.new(name="pin")
    idx = [v.index for v in ob.data.vertices if test(Vector((-v.co.x, v.co.z, -v.co.y)))]
    vg.add(idx, 1.0, "REPLACE")
    return vg


def cloth(ob, pin_group, mass=0.35, bend=6.0):
    m = ob.modifiers.new("cloth", "CLOTH")
    s = m.settings
    s.quality = 10
    s.mass = mass
    s.tension_stiffness = 20.0
    s.compression_stiffness = 20.0
    s.shear_stiffness = 10.0
    s.bending_stiffness = bend
    s.air_damping = 2.0
    s.vertex_group_mass = pin_group.name
    c = m.collision_settings
    c.distance_min = 0.004
    c.collision_quality = 4
    return m


def simulate(objs, frames):
    scene = bpy.context.scene
    scene.frame_start = 1
    scene.frame_end = frames
    for ob in objs:
        for m in ob.modifiers:
            if m.type == "CLOTH":
                m.point_cache.frame_start = 1
                m.point_cache.frame_end = frames
    for f in range(1, frames + 1):
        scene.frame_set(f)
    for ob in objs:
        bpy.context.view_layer.objects.active = ob
        bpy.ops.object.select_all(action="DESELECT")
        ob.select_set(True)
        bpy.ops.object.modifier_apply(modifier="cloth")


def thicken(ob, t=0.005):
    m = ob.modifiers.new("solid", "SOLIDIFY")
    m.thickness = t
    m.offset = 1.0
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.ops.object.modifier_apply(modifier="solid")


def build(kit):
    sc.register()
    chair = None
    if os.path.exists(CHAIR_FBX):
        before = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=CHAIR_FBX)
        new = [o for o in bpy.data.objects if o not in before]
        for o in new:
            if o.type == "MESH" and chair is None and (o.name.endswith("LOD0") or "LOD" not in o.name):
                chair = o
        for o in new:
            if o is not chair:
                bpy.data.objects.remove(o, do_unlink=True)
    body = grid("jacket body", 0.50, 1.04, 22, 52, (0.0, BACK_TOP_Y + 0.012, BACK_Z - 0.12))
    sleeve = grid("jacket sleeve", 0.15, 0.62, 7, 30, (0.205, BACK_TOP_Y + 0.004, BACK_Z + 0.20))
    if chair is not None:
        bpy.context.view_layer.objects.active = chair
        chair.modifiers.new("collision", "COLLISION")
        chair.collision.cloth_friction = 40.0
        chair.collision.thickness_outer = 0.006
        # Pin the shoulder line on the backrest top (the jacket hangs from it), and the sleeve's shoulder end.
        bp = pin(body, lambda p: abs(p.z - BACK_Z) < 0.012)
        sp = pin(sleeve, lambda p: p.z < BACK_Z + 0.20 - 0.31 + 0.025)
        cloth(body, bp)
        cloth(sleeve, sp, mass=0.2, bend=3.0)
        simulate([body, sleeve], 70)
        bpy.data.objects.remove(chair, do_unlink=True)
        kit.meta["drape"] = "cloth-baked over Kit_TaskChair (Blender cloth, 70 frames, pinned at the shoulder line)"
    else:
        # Procedural fallback: fold the sheet over the backrest top.
        for ob in (body, sleeve):
            for v in ob.data.vertices:
                p = Vector((-v.co.x, v.co.z, -v.co.y))
                d = p.z - BACK_Z
                if d > 0:
                    p.y -= d * 1.6
                    p.z = BACK_Z + d * 0.25
                else:
                    p.y -= -d * 1.0
                    p.z = BACK_Z - 0.035 - (-d) * 0.06
                v.co = Vector(sc.B(p.x, max(0.42, p.y), p.z))
        kit.meta["drape"] = "procedural fallback (no chair FBX)"
    for ob in (body, sleeve):
        thicken(ob)
        ob.data.materials.append(kit._material(sc.UNIFORM))
        kit.adopt(ob, [sc.UNIFORM], uv="metres")
    # The blank shoulder patch on the sleeve's upper face (embroidered, no lettering).
    me = sleeve.data
    best = max(me.vertices, key=lambda v: v.co.z - abs(-v.co.y - (BACK_Z + 0.02)) * 2.0)
    loc = best.co.copy()
    patch = kit.cylinder(0.035, 0.002, (loc.x, loc.y, loc.z + 0.0035), "Prop_FabricNavy", verts=24, bevel=0.0, name="shoulder patch")
    patch.scale = (1.0, 1.25, 1.0)
    kit.no_collider()
    kit.tag("station", "soft")
    sc.auto_lod_drops(kit)
    sc.lods(kit, 4.0, 10.0, 50.0, (8000, 2500, 300))
    sc.station_meta(kit, "the chair's origin (floor under it); placed at the chair's pivot and yaw", "")
