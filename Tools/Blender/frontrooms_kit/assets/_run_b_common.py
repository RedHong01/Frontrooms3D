"""Shared helpers for the Run! B (EMERGENCY POWER) production kit (run_*.py).

Production plan: Documentation/research/room_visuals/run_b_production/10_plan.md §1.1-§1.2.

Every B module builds its three levels BY HAND (kitlib P-1b, fr_lods "0" / "1" / "2"):
the same parts at three qualities, so a LOD keeps its silhouette and slots and no
collapse decimator folds a bevel or a decal. ``levels(kit, fn)`` calls ``fn(kit, lv)``
for lv = 0, 1, 2 and tags the parts each pass made.

Hero rules (VL338, checked at 0.3 m with vFOV 76: one screen pixel = 0.43 mm):
* curves: chord error <= 0.2 mm at LOD0 (``segs(r, 0)``), e.g. the 115 mm lens rim -> 48;
* edges: every box edge bevelled >= 0.8 mm with 2 segments at LOD0 (``bev(w, 0)``);
* decals: >= 2.5 px/mm (texture sizes in gen_run_b_prod.py);
* no colliders except the three floor obstacles (box colliders).
Slots are registered here at import time (kitlib.SLOTS is not edited). Their Unity
materials are Resources/Surfaces/<slot>.mat (FrontRoomsKitImporter remaps by name).
"""

import math

import bmesh
import bpy
from mathutils import Vector

import kitlib

CHORD = 0.0002      # 0.2 mm

# B slots (preview colours only; the look is the Unity material).
B_SLOTS = {
    "Run_SealedBeamLens": ((0.62, 0.62, 0.60), 0.1),
    "Run_NeonPilot": ((0.45, 0.04, 0.03), 0.2),
    "Run_UnitLegend": ((0.85, 0.83, 0.78), 0.6),
    "Run_WrapLens": ((0.88, 0.88, 0.86), 0.3),
    "Run_PlateNotice": ((0.85, 0.84, 0.80), 0.5),
    "Run_PlateElectrical": ((0.85, 0.84, 0.80), 0.5),
    "Run_PlateStaff": ((0.85, 0.84, 0.80), 0.5),
    "Run_PlatePull": ((0.60, 0.07, 0.05), 0.5),
    "Run_PlateExtinguisher": ((0.88, 0.87, 0.84), 0.5),
    "Run_StencilNotExit": ((0.02, 0.02, 0.02), 0.8),
    "RunB_SprinklerRed": ((0.36, 0.02, 0.012), 0.52),   # #A3201A
    "RunB_FireRed": ((0.43, 0.012, 0.009), 0.5),        # #B01E18
    "RunB_FireHandle": ((0.55, 0.02, 0.016), 0.45),     # #C42A20
    "RunB_BumperVinyl": ((0.042, 0.042, 0.040), 0.6),   # #3A3A38
    "RunB_CartPlastic": ((0.41, 0.40, 0.35), 0.55),     # #ABA9A0
    "RunB_BucketYellow": ((0.69, 0.42, 0.007), 0.45),   # #D9AE14
    "RunB_Carton": ((0.67, 0.65, 0.58), 0.85),          # #D6D3C8
    "RunB_GalvSteel": ((0.62, 0.63, 0.63), 0.45),
    "RunB_ChairGrey": ((0.11, 0.12, 0.11), 0.5),        # #5F615E
    "RunB_CabinetWhite": ((0.78, 0.76, 0.70), 0.45),    # #E4E2DA
    "Painted_Metal": ((0.85, 0.83, 0.78), 0.5),
}
for _slot, (_c, _r) in B_SLOTS.items():
    kitlib.register_slot(_slot, _c, _r)


def segs(r, lv=0, lo=8, step=4):
    """Segments for a round part of radius r: LOD0 meets the 0.2 mm chord error,
    LOD1 halves it, LOD2 quarters it (never under ``lo`` at LOD0, 6 below)."""
    if r <= CHORD:
        n = lo
    else:
        n = math.pi / math.acos(max(-1.0, 1.0 - CHORD / r))
    n = int(math.ceil(n / step) * step)
    n = max(lo, n)
    if lv == 1:
        n = max(6, int(math.ceil(n / 2.0 / 2) * 2))
    elif lv >= 2:
        n = max(6, int(math.ceil(n / 4.0 / 2) * 2))
    return n


def bev(w, lv=0):
    """(width, segments): LOD0 >= 0.8 mm x 2, LOD1 the same width x 1, LOD2 none."""
    if lv == 0:
        return max(w, 0.0008), 2
    if lv == 1:
        return max(w, 0.0008), 1
    return 0.0, 0


def levels(kit, fn, which=(0, 1, 2)):
    """Build each level with fn(kit, lv) and tag its parts fr_lods = lv.
    FR_RUNB_TRIS=1 prints every part's evaluated triangle count (budget work)."""
    import os
    report = os.environ.get("FR_RUNB_TRIS") == "1"
    for lv in which:
        n0 = len(kit.parts)
        fn(kit, lv)
        for p in kit.parts[n0:]:
            p["fr_lods"] = str(lv)
        if report:
            dg = bpy.context.evaluated_depsgraph_get()
            per = {}
            for p in kit.parts[n0:]:
                me = p.evaluated_get(dg).to_mesh()
                t = sum(len(f.vertices) - 2 for f in me.polygons)
                p.evaluated_get(dg).to_mesh_clear()
                key = p.name.split(".")[0]
                per[key] = per.get(key, 0) + t
            total = sum(per.values())
            print("[tris] %s LOD%d %d: %s" % (kit.name, lv, total, ", ".join("%s %d" % kv for kv in sorted(per.items(), key=lambda kv: -kv[1]))))


def lod_meta(kit, distances, budget):
    """Sidecar lodDistances (d01, d12, dcull; 0 = never cull) and the plan's budget."""
    kit.meta["lodDistances"] = [float(d) for d in distances]
    kit.meta["lodBudget"] = list(budget)
    kit.meta["lodHandBuilt"] = True
    kit.meta["runB"] = "production 2026-10-22 (run_b_production/10_plan.md)"


def box(kit, size, loc, slot, w=0.0012, lv=0, rot=(0, 0, 0), name="box", uv="metres", decal_axes="xz"):
    bw, bs = bev(w, lv)
    return kit.box(size, loc, slot, bevel=bw, segments=bs, rot=rot, name=name, uv=uv, decal_axes=decal_axes)


def cyl(kit, r, depth, loc, slot, lv=0, rot=(0, 0, 0), w=0.0008, name="cyl", lo=8, radius_top=None, uv="metres"):
    bw, bs = bev(w, lv)
    return kit.cylinder(r, depth, loc, slot, verts=segs(r, lv, lo), rot=rot, bevel=bw, segments=bs, name=name, radius_top=radius_top, uv=uv)


def lathe(kit, profile, loc, slot, lv=0, rot=(0, 0, 0), name="lathe", uv="metres", close_top=True, close_bottom=True, lo=8, rings=None):
    """kitlib lathe at the hero segment count of the largest radius. ``rings`` (a list
    of profile indices) keeps only those rings at LOD1/LOD2 when given per level."""
    r = max(p[0] for p in profile)
    prof = profile if rings is None else [profile[i] for i in rings]
    return kit.lathe(prof, loc, slot, verts=segs(r, lv, lo), rot=rot, name=name, uv=uv, close_top=close_top, close_bottom=close_bottom)


def tube(kit, pts, r, slot, lv=0, name="tube", caps=True, lo=8):
    return kit.tube(pts, r, slot, verts=segs(r, lv, lo), name=name, caps=caps)


def rrect(w, h, r, per_corner):
    """Rounded rectangle outline (u, v) centred on 0, counter-clockwise."""
    out = []
    hw, hh = w / 2.0, h / 2.0
    r = min(r, hw, hh)
    corners = ((hw - r, hh - r, 0.0), (-hw + r, hh - r, 90.0), (-hw + r, -hh + r, 180.0), (hw - r, -hh + r, 270.0))
    for cx, cy, a0 in corners:
        if per_corner <= 0 or r <= 0:
            out.append((cx + math.copysign(r, cx) if r else cx, cy + math.copysign(r, cy) if r else cy))
            continue
        for k in range(per_corner + 1):
            a = math.radians(a0 + 90.0 * k / per_corner)
            out.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return out


def corner_steps(r, lv):
    """Points per 90-degree corner for the chord rule at this level."""
    return max(1, segs(r, lv, lo=4, step=4) // 4)


def mesh_part(kit, name, verts, faces, uvs, slot, smooth=True):
    """A part built straight from vertex / face / per-loop UV lists (UVs kept: uv="keep").
    uvs: one (u, v) per face corner, in face order."""
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bv = [bm.verts.new(v) for v in verts]
    # verify(), as kitlib._uv_metres and _newcase do: the same default layer name (Blender 4.3: "Float2"), so the
    # join keeps ONE UV channel. A named "UVMap" layer became a second channel and Unity read the lens at UV 0.
    layer = bm.loops.layers.uv.verify()
    k = 0
    for f in faces:
        try:
            face = bm.faces.new([bv[i] for i in f])
        except ValueError:
            k += len(f)
            continue
        for loop in face.loops:
            loop[layer].uv = uvs[k]
            k += 1
    bm.normal_update()
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(obj)
    obj.data.materials.append(kit._material(slot))
    return kit.adopt(obj, [slot], uv="keep")


def sweep(kit, name, profile, length, slot, lv=0, axis="x", closed=True, uv_scale=1.0, cap=True, loc=(0, 0, 0)):
    """Extrude a 2D profile (list of (a, b)) along ``axis`` over [-length/2, length/2] with
    metre UVs that run u along the length and v along the profile's arc length (the
    wrap lens, the bumper rail). axis "x": profile in (y, z)."""
    n = len(profile)
    x0, x1 = -length / 2.0, length / 2.0
    verts = []
    for x in (x0, x1):
        for (a, b) in profile:
            if axis == "x":
                verts.append((loc[0] + x, loc[1] + a, loc[2] + b))
            elif axis == "y":
                verts.append((loc[0] + a, loc[1] + x, loc[2] + b))
            else:
                verts.append((loc[0] + a, loc[1] + b, loc[2] + x))
    arc = [0.0]
    for i in range(1, n + (1 if closed else 0)):
        p, q = profile[(i - 1) % n], profile[i % n]
        arc.append(arc[-1] + math.hypot(q[0] - p[0], q[1] - p[1]))
    faces, uvs = [], []
    m = n if closed else n - 1
    for i in range(m):
        j = (i + 1) % n
        a, b, c, d = i, j, n + j, n + i
        faces.append((a, d, c, b))
        ui, uj = arc[i] * uv_scale, arc[i + 1] * uv_scale
        uvs += [(x0 * uv_scale, ui), (x1 * uv_scale, ui), (x1 * uv_scale, uj), (x0 * uv_scale, uj)]
    if cap and closed:
        faces.append(tuple(range(n)))
        uvs += [(profile[i][0], profile[i][1]) for i in range(n)]
        faces.append(tuple(range(2 * n - 1, n - 1, -1)))
        uvs += [(profile[i][0], profile[i][1]) for i in range(n - 1, -1, -1)]
    obj = mesh_part(kit, name, verts, faces, uvs, slot)
    # Normals out: recalc on the closed solid.
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(obj.data)
    bm.free()
    return obj


def flip_check(obj, outward=None):
    """Recalculate normals of a closed part."""
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(obj.data)
    bm.free()
    return obj


def tris_of(obj):
    return sum(len(p.vertices) - 2 for p in obj.data.polygons)
