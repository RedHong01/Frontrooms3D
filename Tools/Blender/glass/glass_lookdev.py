"""glass_lookdev.py -- Cycles look-dev frames of the GD3 glass stages S1, S2 and S4 (plan 10 sec 4.1, step 2).

Reads crack_graph_ref.py output (JSON) and renders what the pattern looks like as real, physically plausible
glass in a Level 0 back-office room: a W-L0 window (walnut casing, stool, 16 mm stops) in a 0.16 m wall,
ceiling troffers on a 2.4 m grid whose mirror images land on the pane, and a dim tall hall beyond.

    Blender -b --factory-startup --python glass_lookdev.py -- --jobs jobs.json
    Blender -b --factory-startup --python glass_lookdev.py -- --pattern p.json --stage S2 --view C1 --out f.png

jobs.json: {"res": [1920, 1080], "samples": 256, "gpu": true,
            "jobs": [{"pattern": "p.json", "stages": ["S1", "S2", "S4"], "views": {...}, "out_prefix": "dir/name"}]}
    views per stage default to S1/S2: C1, C2; S4: C1, C2, C4.

Views (window-root metres; x = player's right, y up, z = into the far side; the camera is the game lens:
76 deg vertical FOV, 16:9, near 0.06 m):
    C1  the shot camera: eye (clamp(hit x, +-0.25), 1.62, -0.55), looking at the hit point (Design 2 stand point)
    C2  45 deg, 1.5 m: eye (-1.061, 1.62, -1.061), looking at the pane centre (0, 1.175, 0); a troffer's mirror
        image lands near the top left of the pane
    C3  face-on, 1.2 m: eye (0, 1.62, -1.2), looking at the pane centre (dark hall beyond)
    C4  the far side, 2 m: eye (0, 1.62, +2.0), looking at the floor (0, 0, +0.5) (where the glass lands)
Blender axes: X = root x, Y = root z (far side +Y), Z = root y (up).

Stages (plan sec 2.3):
    S1 (t = 0.35 s)  the intact slab + Crack1 fins (air gaps through the 6 mm, glass-to-air interfaces) + the
                     crushed crater (<= 15 mm) + 6-12 chips just leaving the cracks
    S2 (t = 0.70 s)  every piece at its stage-2 pose (tilt 0.2-0.8 deg, push 0.5-3 mm, falling off from the
                     impact), 0.6 mm bevels, 0.05 mm crack gaps; crushed core gone; chips in the air and on the stool
    S4 (settled)     teeth in the stop, every other piece landed (scripted ballistic landing from the release
                     waves in the JSON; Unity uses rigid bodies, step 6), glitter at the wall base (RCMP91)
Tempered6 (dice mode): S1 = S2 = the intact pane (tempered glass shows no crack stages); S4 = granules and
clumps on both floors, an empty frame.

Glass: Principled BSDF, transmission 1, IOR 1.52, roughness 0, plus volume absorption (7 / 3.5 / 5 per metre
for R / G / B): about 3 % absorbed through 6 mm face-on, green-blue through the edges and fracture faces.
Bevel: the profile's (crack_graph_ref.py, JSON slab.bevel: 0.3 mm desktop, 0 WebGL); a jobs file may override it.
Sides: the breaker stands on the -far_z_sign side (far_z_sign = +1 when GlassBreakRecord.side = +1, struck from
cell a); every camera and the landing use far_z_sign, so the same JSON renders correctly for either side.
"""
import json
import math
import os
import random
import sys
import time

import bmesh
import bpy
from mathutils import Matrix, Vector

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import crack_graph_ref as cg  # noqa: E402

SLAB_CY = cg.SLAB_CY
G = 9.81
GLASS_T = 0.006
EYE = 1.62
VFOV_DEG = 76.0
TROFFER_X = (-2.4, 0.0, 2.4)
TROFFER_Z = (-1.2, -3.6, -6.0)          # root z (player side is negative)
LOOK = {"bevel": None, "inset": 0.00005,      # bevel None = the profile's (JSON slab.bevel, 0.3 mm); 0.05 mm crack gap
        "fracture_rough": (0.04, 0.45)}        # fracture faces: mirror .. hackle


def BL(x, y, z):
    """window root (x right, y up, z far) -> Blender (X, Y = far, Z = up)"""
    return Vector((x, z, y))


# ---------------------------------------------------------------------------------------------------------
# Materials
# ---------------------------------------------------------------------------------------------------------
def _nodes(m):
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    return nt, nt.nodes, nt.links


def mat_glass(name, roughness=0.0, dust=0.0, rough_noise=None):
    """rough_noise=(lo, hi): roughness varies along the fracture faces, from mirror-smooth near the crack origin
    to mist and hackle (fractography: mirror -> mist -> hackle), so a crack's brightness changes along it."""
    m = bpy.data.materials.new(name)
    nt, N, L = _nodes(m)
    out = N.new("ShaderNodeOutputMaterial")
    pb = N.new("ShaderNodeBsdfPrincipled")
    pb.inputs["Base Color"].default_value = (1.0, 1.0, 1.0, 1.0)
    pb.inputs["Roughness"].default_value = roughness
    if rough_noise:
        tcr = N.new("ShaderNodeTexCoord")
        nzr = N.new("ShaderNodeTexNoise")
        nzr.inputs["Scale"].default_value = 35.0
        nzr.inputs["Detail"].default_value = 3.0
        L.new(tcr.outputs["Object"], nzr.inputs["Vector"])
        mrr = N.new("ShaderNodeMapRange")
        mrr.inputs["From Min"].default_value = 0.3
        mrr.inputs["From Max"].default_value = 0.7
        mrr.inputs["To Min"].default_value = rough_noise[0]
        mrr.inputs["To Max"].default_value = rough_noise[1]
        L.new(nzr.outputs["Fac"], mrr.inputs["Value"])
        L.new(mrr.outputs[0], pb.inputs["Roughness"])
    pb.inputs["IOR"].default_value = 1.52
    pb.inputs["Transmission Weight"].default_value = 1.0
    shader = pb.outputs[0]
    if dust > 0.0:
        # a faint dust film in pane space (UV = pane metres), continuous across every stage swap
        tc = N.new("ShaderNodeTexCoord")
        nz = N.new("ShaderNodeTexNoise")
        nz.inputs["Scale"].default_value = 9.0
        nz.inputs["Detail"].default_value = 6.0
        L.new(tc.outputs["UV"], nz.inputs["Vector"])
        ramp = N.new("ShaderNodeMapRange")
        ramp.inputs["From Min"].default_value = 0.35
        ramp.inputs["From Max"].default_value = 0.75
        ramp.inputs["To Min"].default_value = dust * 0.25
        ramp.inputs["To Max"].default_value = dust
        L.new(nz.outputs["Fac"], ramp.inputs["Value"])
        df = N.new("ShaderNodeBsdfDiffuse")
        df.inputs["Color"].default_value = (0.55, 0.52, 0.46, 1.0)
        mix = N.new("ShaderNodeMixShader")
        L.new(ramp.outputs[0], mix.inputs[0])
        L.new(pb.outputs[0], mix.inputs[1])
        L.new(df.outputs[0], mix.inputs[2])
        shader = mix.outputs[0]
    L.new(shader, out.inputs["Surface"])
    vol = N.new("ShaderNodeVolumeAbsorption")
    vol.inputs["Color"].default_value = (0.30, 0.65, 0.50, 1.0)   # sigma = 10 * (1 - c) = 7, 3.5, 5 per metre
    vol.inputs["Density"].default_value = 10.0
    L.new(vol.outputs[0], out.inputs["Volume"])
    return m


def mat_crack_gap():
    """The inside of a crack: an air gap. Normals point INTO the gap, so a ray from the glass meets a back face
    (glass -> air, total internal reflection past 41.8 deg) and leaves through a front face (air -> glass)."""
    m = bpy.data.materials.new("CrackGap")
    nt, N, L = _nodes(m)
    out = N.new("ShaderNodeOutputMaterial")
    gb = N.new("ShaderNodeBsdfGlass")
    gb.inputs["IOR"].default_value = 1.52
    gb.inputs["Roughness"].default_value = 0.0
    L.new(gb.outputs[0], out.inputs["Surface"])
    return m


def mat_crushed():
    """Crushed glass powder: white where the glass is powdered, clear where it is only bruised (no star, no glow)."""
    m = bpy.data.materials.new("Crushed")
    nt, N, L = _nodes(m)
    out = N.new("ShaderNodeOutputMaterial")
    df = N.new("ShaderNodeBsdfDiffuse")
    df.inputs["Color"].default_value = (0.86, 0.87, 0.86, 1.0)
    tl = N.new("ShaderNodeBsdfTranslucent")
    tl.inputs["Color"].default_value = (0.80, 0.84, 0.82, 1.0)
    powder = N.new("ShaderNodeMixShader")
    powder.inputs[0].default_value = 0.35
    L.new(df.outputs[0], powder.inputs[1])
    L.new(tl.outputs[0], powder.inputs[2])
    clear = N.new("ShaderNodeBsdfPrincipled")
    clear.inputs["Roughness"].default_value = 0.25
    clear.inputs["IOR"].default_value = 1.52
    clear.inputs["Transmission Weight"].default_value = 1.0
    tc = N.new("ShaderNodeTexCoord")
    nz = N.new("ShaderNodeTexNoise")
    nz.inputs["Scale"].default_value = 900.0
    nz.inputs["Detail"].default_value = 4.0
    L.new(tc.outputs["Object"], nz.inputs["Vector"])
    mr = N.new("ShaderNodeMapRange")
    mr.inputs["From Min"].default_value = 0.38
    mr.inputs["From Max"].default_value = 0.62
    L.new(nz.outputs["Fac"], mr.inputs["Value"])
    mix = N.new("ShaderNodeMixShader")
    L.new(mr.outputs[0], mix.inputs[0])
    L.new(clear.outputs[0], mix.inputs[1])
    L.new(powder.outputs[0], mix.inputs[2])
    L.new(mix.outputs[0], out.inputs["Surface"])
    return m


def mat_simple(name, color, rough, bump=0.0, stripes=None, grid=None, grain=None, noise=0.0):
    m = bpy.data.materials.new(name)
    nt, N, L = _nodes(m)
    out = N.new("ShaderNodeOutputMaterial")
    pb = N.new("ShaderNodeBsdfPrincipled")
    pb.inputs["Roughness"].default_value = rough
    tc = N.new("ShaderNodeTexCoord")
    col_socket = None
    base = N.new("ShaderNodeRGB")
    base.outputs[0].default_value = color + (1.0,)
    col_socket = base.outputs[0]
    if stripes:      # wallpaper: vertical bands (period, contrast)
        period, contrast = stripes
        wv = N.new("ShaderNodeTexWave")
        wv.wave_type = "BANDS"
        wv.bands_direction = "X"
        wv.inputs["Scale"].default_value = 1.0 / period
        wv.inputs["Distortion"].default_value = 0.0
        L.new(tc.outputs["Object"], wv.inputs["Vector"])
        mx = N.new("ShaderNodeMix")
        mx.data_type = "RGBA"
        mx.inputs[0].default_value = contrast
        dark = N.new("ShaderNodeRGB")
        dark.outputs[0].default_value = tuple(c * 0.80 for c in color) + (1.0,)
        mr = N.new("ShaderNodeMath")
        mr.operation = "MULTIPLY"
        L.new(wv.outputs["Fac"], mr.inputs[0])
        mr.inputs[1].default_value = contrast
        L.new(mr.outputs[0], mx.inputs[0])
        L.new(col_socket, mx.inputs[6])
        L.new(dark.outputs[0], mx.inputs[7])
        col_socket = mx.outputs[2]
    if grid:         # ceiling tiles: brick texture as a 0.6 m grid
        size, line_col = grid
        br = N.new("ShaderNodeTexBrick")
        br.offset = 0.0
        br.inputs["Scale"].default_value = 1.0 / size
        br.inputs["Mortar Size"].default_value = 0.004
        br.inputs["Brick Width"].default_value = 1.0
        br.inputs["Row Height"].default_value = 1.0
        L.new(tc.outputs["Object"], br.inputs["Vector"])
        L.new(col_socket, br.inputs["Color1"])
        L.new(col_socket, br.inputs["Color2"])
        br.inputs["Mortar"].default_value = line_col + (1.0,)
        col_socket = br.outputs["Color"]
    if grain:        # wood: long bands along one axis
        wv = N.new("ShaderNodeTexWave")
        wv.wave_type = "BANDS"
        wv.bands_direction = grain
        wv.inputs["Scale"].default_value = 60.0
        wv.inputs["Distortion"].default_value = 6.0
        wv.inputs["Detail"].default_value = 3.0
        L.new(tc.outputs["Object"], wv.inputs["Vector"])
        mx = N.new("ShaderNodeMix")
        mx.data_type = "RGBA"
        mr = N.new("ShaderNodeMath")
        mr.operation = "MULTIPLY"
        L.new(wv.outputs["Fac"], mr.inputs[0])
        mr.inputs[1].default_value = 0.35
        L.new(mr.outputs[0], mx.inputs[0])
        dark = N.new("ShaderNodeRGB")
        dark.outputs[0].default_value = tuple(c * 0.6 for c in color) + (1.0,)
        L.new(col_socket, mx.inputs[6])
        L.new(dark.outputs[0], mx.inputs[7])
        col_socket = mx.outputs[2]
    if noise > 0.0:
        nz = N.new("ShaderNodeTexNoise")
        nz.inputs["Scale"].default_value = 40.0
        L.new(tc.outputs["Object"], nz.inputs["Vector"])
        mx = N.new("ShaderNodeMix")
        mx.data_type = "RGBA"
        mx.blend_type = "MULTIPLY"
        mx.inputs[0].default_value = noise
        L.new(col_socket, mx.inputs[6])
        L.new(nz.outputs["Color"], mx.inputs[7])
        col_socket = mx.outputs[2]
    L.new(col_socket, pb.inputs["Base Color"])
    if bump > 0.0:
        nz2 = N.new("ShaderNodeTexNoise")
        nz2.inputs["Scale"].default_value = 300.0
        L.new(tc.outputs["Object"], nz2.inputs["Vector"])
        bp = N.new("ShaderNodeBump")
        bp.inputs["Strength"].default_value = bump
        bp.inputs["Distance"].default_value = 0.002
        L.new(nz2.outputs["Fac"], bp.inputs["Height"])
        L.new(bp.outputs[0], pb.inputs["Normal"])
    L.new(pb.outputs[0], out.inputs["Surface"])
    return m


def mat_emit(name, color, strength):
    m = bpy.data.materials.new(name)
    nt, N, L = _nodes(m)
    out = N.new("ShaderNodeOutputMaterial")
    em = N.new("ShaderNodeEmission")
    em.inputs["Color"].default_value = color + (1.0,)
    em.inputs["Strength"].default_value = strength
    L.new(em.outputs[0], out.inputs["Surface"])
    return m


MATS = {}


def materials(lamp_strength):
    MATS["glass"] = mat_glass("Glass", 0.0, dust=0.03)
    MATS["fracture"] = mat_glass("GlassFracture", 0.0, dust=0.0, rough_noise=LOOK["fracture_rough"])
    MATS["glass_floor"] = mat_glass("GlassFloor", 0.0, dust=0.0)
    MATS["gap"] = mat_crack_gap()
    MATS["crushed"] = mat_crushed()
    MATS["wall"] = mat_simple("Wallpaper", (0.62, 0.52, 0.26), 0.85, stripes=(0.06, 0.55), noise=0.15)
    MATS["carpet"] = mat_simple("Carpet", (0.30, 0.25, 0.14), 1.0, bump=0.35, noise=0.35)
    MATS["ceiling"] = mat_simple("CeilingTile", (0.76, 0.73, 0.64), 0.9, grid=(0.6, (0.42, 0.40, 0.35)), noise=0.1)
    MATS["walnut"] = mat_simple("Walnut", (0.15, 0.08, 0.04), 0.42, grain="Z")
    MATS["housing"] = mat_simple("TrofferHousing", (0.80, 0.80, 0.77), 0.35)
    MATS["lens"] = mat_emit("TrofferLens", (1.0, 0.96, 0.88), lamp_strength)
    MATS["lens_dim"] = mat_emit("TrofferLensDim", (1.0, 0.96, 0.88), lamp_strength * 0.05)
    MATS["lens_dead"] = mat_simple("TrofferLensDead", (0.55, 0.55, 0.52), 0.3)


# ---------------------------------------------------------------------------------------------------------
# Geometry helpers
# ---------------------------------------------------------------------------------------------------------
def link(ob, coll=None):
    (coll or bpy.context.scene.collection).objects.link(ob)
    return ob


def box(name, x0, x1, y0, y1, z0, z1, mat, coll=None):
    """Axis-aligned box in ROOT coordinates (x, y up, z far)."""
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    vs = [bm.verts.new(BL(x, y, z)) for x in (x0, x1) for y in (y0, y1) for z in (z0, z1)]
    idx = lambda i, j, k: vs[i * 4 + j * 2 + k]  # noqa: E731
    quads = [
        [idx(0, 0, 0), idx(0, 0, 1), idx(0, 1, 1), idx(0, 1, 0)],
        [idx(1, 0, 0), idx(1, 1, 0), idx(1, 1, 1), idx(1, 0, 1)],
        [idx(0, 0, 0), idx(1, 0, 0), idx(1, 0, 1), idx(0, 0, 1)],
        [idx(0, 1, 0), idx(0, 1, 1), idx(1, 1, 1), idx(1, 1, 0)],
        [idx(0, 0, 0), idx(0, 1, 0), idx(1, 1, 0), idx(1, 0, 0)],
        [idx(0, 0, 1), idx(1, 0, 1), idx(1, 1, 1), idx(0, 1, 1)],
    ]
    for q in quads:
        bm.faces.new(q)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(name, me)
    ob.data.materials.append(mat)
    return link(ob, coll)


def inset_poly(poly, d):
    """Offset a convex CCW polygon inward by d."""
    n = len(poly)
    lines = []
    for i in range(n):
        (x0, y0), (x1, y1) = poly[i], poly[(i + 1) % n]
        ex, ey = x1 - x0, y1 - y0
        L = math.hypot(ex, ey) or 1.0
        nx, ny = -ey / L, ex / L                     # inward normal of a CCW polygon
        lines.append((x0 + nx * d, y0 + ny * d, ex, ey))
    out = []
    for i in range(n):
        ax, ay, adx, ady = lines[i - 1]
        bx, by, bdx, bdy = lines[i]
        den = adx * bdy - ady * bdx
        if abs(den) < 1e-14:
            out.append((bx, by))
            continue
        t = ((bx - ax) * bdy - (by - ay) * bdx) / den
        out.append((ax + adx * t, ay + ady * t))
    return out


def prism_lists(poly, t, bevel, z_off=0.0):
    """A closed 6 mm prism of a convex slab polygon, every edge bevelled. Returns (verts, faces, mat_ids, uvs)
    in SLAB-LOCAL coordinates (x, y, z across); uvs are per-vertex pane metres (x, y)."""
    bm = bmesh.new()
    lo = [bm.verts.new((x, y, -t / 2 + z_off)) for x, y in poly]
    hi = [bm.verts.new((x, y, t / 2 + z_off)) for x, y in poly]
    bm.faces.new(list(reversed(lo)))
    bm.faces.new(hi)
    n = len(poly)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new([lo[i], lo[j], hi[j], hi[i]])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    if bevel > 0.0:
        bmesh.ops.bevel(bm, geom=bm.edges[:], offset=bevel, offset_type="OFFSET", segments=1, profile=0.5,
                        affect="EDGES", clamp_overlap=True)
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    bm.verts.index_update()
    verts = [tuple(v.co) for v in bm.verts]
    faces, mats = [], []
    for f in bm.faces:
        faces.append([v.index for v in f.verts])
        mats.append(0 if abs(f.normal.z) > 0.999 else 1)
    bm.free()
    return verts, faces, mats


class MeshAccum(object):
    """Collect many small meshes, then build one Blender mesh (with a pane-metre UV map)."""

    def __init__(self):
        self.v, self.f, self.m, self.uv = [], [], [], []

    def add(self, verts_world, faces, mats, uvs, mirrored=False):
        """mirrored=True for geometry mapped through BL(): root -> Blender swaps two axes (a mirror), which
        reverses the winding, so faces are reversed back to keep outward normals."""
        base = len(self.v)
        self.v.extend(verts_world)
        self.uv.extend(uvs)
        for fc, mi in zip(faces, mats):
            idx = [base + i for i in fc]
            self.f.append(list(reversed(idx)) if mirrored else idx)
            self.m.append(mi)

    def build(self, name, mat_list, coll=None, flip=False):
        me = bpy.data.meshes.new(name)
        faces = [list(reversed(f)) for f in self.f] if flip else self.f
        me.from_pydata([tuple(v) for v in self.v], [], faces)
        uvl = me.uv_layers.new(name="pane")
        for poly in me.polygons:
            poly.material_index = self.m[poly.index]
            for li in poly.loop_indices:
                vi = me.loops[li].vertex_index
                uvl.data[li].uv = self.uv[vi]
        for mt in mat_list:
            me.materials.append(mt)
        n_before = len(me.polygons)
        me.validate()
        if len(me.polygons) != n_before:
            print("[lookdev] warning: mesh.validate changed", name, n_before, "->", len(me.polygons))
        ob = bpy.data.objects.new(name, me)
        return link(ob, coll)


def slab_point_to_bl(p):
    x, y, z = p
    return BL(x, y + SLAB_CY, z)


# ---------------------------------------------------------------------------------------------------------
# The room, the window, the lamps
# ---------------------------------------------------------------------------------------------------------
def build_room():
    coll = bpy.data.collections.new("Room")
    bpy.context.scene.collection.children.link(coll)
    W = MATS["wall"]
    # window wall (0.16 m, centred on the cell line) with the 1.4 x 1.65 opening at sill 0.35
    box("Wall_L", -5.0, -0.70, 0.0, 5.4, -0.08, 0.08, W, coll)
    box("Wall_R", 0.70, 5.0, 0.0, 5.4, -0.08, 0.08, W, coll)
    box("Wall_B", -0.70, 0.70, 0.0, 0.35, -0.08, 0.08, W, coll)
    box("Wall_T", -0.70, 0.70, 2.0, 5.4, -0.08, 0.08, W, coll)
    # player's room (Level 0, Standard 2.9 m)
    box("Floor_A", -3.6, 3.6, -0.02, 0.0, -7.2, -0.08, MATS["carpet"], coll)
    box("Ceil_A", -3.6, 3.6, 2.9, 2.92, -7.2, -0.08, MATS["ceiling"], coll)
    box("Wall_A_L", -3.62, -3.6, 0.0, 2.9, -7.2, -0.08, W, coll)
    box("Wall_A_R", 3.6, 3.62, 0.0, 2.9, -7.2, -0.08, W, coll)
    box("Wall_A_Back", -3.6, 3.6, 0.0, 2.9, -7.22, -7.2, W, coll)
    # the hall beyond (Tall 5.4 m), dim
    box("Floor_B", -5.0, 5.0, -0.02, 0.0, 0.08, 9.0, MATS["carpet"], coll)
    box("Ceil_B", -5.0, 5.0, 5.4, 5.42, 0.08, 9.0, MATS["ceiling"], coll)
    box("Wall_B_L", -5.02, -5.0, 0.0, 5.4, 0.08, 9.0, W, coll)
    box("Wall_B_R", 5.0, 5.02, 0.0, 5.4, 0.08, 9.0, W, coll)
    box("Wall_B_Back", -5.0, 5.0, 0.0, 5.4, 9.0, 9.02, W, coll)
    # troffers (2 x 4 ft, long axis along the window wall) on a 2.4 m grid
    for x in TROFFER_X:
        for z in TROFFER_Z:
            troffer(coll, x, 2.9, z, MATS["lens"])
    troffer(coll, 2.4, 5.4, 7.2, MATS["lens_dim"])      # a dim lamp far down the hall
    troffer(coll, -2.4, 5.4, 3.6, MATS["lens_dead"])    # a dead one
    build_frame(coll)
    return coll


def troffer(coll, x, ceil_y, z, lens_mat):
    hw, hd = 0.61, 0.305           # 1.22 x 0.61
    box("TrofferRim", x - hw - 0.03, x + hw + 0.03, ceil_y - 0.012, ceil_y - 0.002, z - hd - 0.03, z + hd + 0.03,
        MATS["housing"], coll)
    box("TrofferLens", x - hw, x + hw, ceil_y - 0.016, ceil_y - 0.013, z - hd, z + hd, lens_mat, coll)


def build_frame(coll):
    """W-L0 back-office light, sleeve mode (06w sec 3.1, 4.2): walnut liners, casings, stool, aprons, stops."""
    M = MATS["walnut"]
    for s in (-1.0, 1.0):
        # jamb liners (visible face at |x| 0.6995) and the head liner
        box("Liner", min(s * 0.6995, s * 0.72), max(s * 0.6995, s * 0.72), 0.3505, 2.0195, -0.1005, 0.1005, M, coll)
        for f in (-1.0, 1.0):
            z0, z1 = sorted((f * 0.080, f * 0.105))
            # jamb casings and the apron
            box("Casing", min(s * 0.6995, s * 0.775), max(s * 0.6995, s * 0.775), 0.3505, 2.075, z0, z1, M, coll)
            # stops: 16 x 16 mm, z +-(0.006 -> 0.022), stop line |x| 0.6835
            sz0, sz1 = sorted((f * 0.006, f * 0.022))
            box("Stop", min(s * 0.6835, s * 0.6995), max(s * 0.6835, s * 0.6995), 0.3505, 1.9995, sz0, sz1, M, coll)
    box("HeadLiner", -0.72, 0.72, 1.9995, 2.0195, -0.1005, 0.1005, M, coll)
    for f in (-1.0, 1.0):
        z0, z1 = sorted((f * 0.080, f * 0.105))
        box("HeadCasing", -0.775, 0.775, 1.9995, 2.075, z0, z1, M, coll)
        box("Apron", -0.775, 0.775, 0.250, 0.3255, z0, z1, M, coll)
        sz0, sz1 = sorted((f * 0.006, f * 0.022))
        box("StopHead", -0.6995, 0.6995, 1.9835, 1.9995, sz0, sz1, M, coll)
        box("StopSill", -0.6995, 0.6995, 0.3505, 0.3665, sz0, sz1, M, coll)
    box("Stool", -0.800, 0.800, 0.3255, 0.3505, -0.121, 0.121, M, coll)


# ---------------------------------------------------------------------------------------------------------
# Stage geometry
# ---------------------------------------------------------------------------------------------------------
def build_slab_intact(coll):
    acc = MeshAccum()
    hx, hy = cg.SLAB_W / 2, cg.SLAB_H / 2
    poly = [(-hx, -hy), (hx, -hy), (hx, hy), (-hx, hy)]
    v, f, m = prism_lists(poly, GLASS_T, 0.0)
    acc.add([slab_point_to_bl(p) for p in v], f, m, [(p[0], p[1]) for p in v], mirrored=True)
    return acc.build("Slab", [MATS["glass"], MATS["fracture"]], coll)


def build_fins(res, coll, rng):
    """Crack1 fins: one thin air gap (0.04 mm) per segment, through the thickness. A real radial crack is not
    exactly perpendicular to the faces: its plane twists a few degrees along its length (twist hackle), so
    face-on rays meet it at a grazing angle and it shows as a hairline mirror whose brightness changes along
    the crack (V3). Each segment's plane is tilted 3-9 deg, the sign kept per crack and flipped now and then."""
    acc = MeshAccum()
    half_gap = 0.00002
    t = GLASS_T * 0.996
    for c in res["stage1"]["cracks"]:
        pts = c["points"]
        sign = 1.0 if rng.random() < 0.5 else -1.0
        for i in range(len(pts) - 1):
            (x0, y0), (x1, y1) = pts[i], pts[i + 1]
            ex, ey = x1 - x0, y1 - y0
            L = math.hypot(ex, ey)
            if L < 1e-5:
                continue
            if rng.random() < 0.25:
                sign = -sign
            tilt = sign * math.radians(rng.uniform(3.0, 9.0))
            shear = math.tan(tilt)                  # lateral offset per metre of depth
            nx, ny = -ey / L, ex / L
            ux, uy = ex / L * 0.0001, ey / L * 0.0001
            poly = [(x0 - ux - nx * half_gap, y0 - uy - ny * half_gap), (x1 + ux - nx * half_gap, y1 + uy - ny * half_gap),
                    (x1 + ux + nx * half_gap, y1 + uy + ny * half_gap), (x0 - ux + nx * half_gap, y0 - uy + ny * half_gap)]
            v, f, m = prism_lists(poly, t, 0.0)
            v = [(p[0] + nx * shear * p[2], p[1] + ny * shear * p[2], p[2]) for p in v]
            acc.add([slab_point_to_bl(p) for p in v], f, [0] * len(f), [(p[0], p[1]) for p in v], mirrored=True)
    if not acc.f:
        return None
    return acc.build("Fins", [MATS["gap"]], coll, flip=True)


def _irregular_outline(poly, ix, iy, rng, k_lo=0.72, k_hi=1.04, sub=3):
    out = []
    n = len(poly)
    for i in range(n):
        (x0, y0), (x1, y1) = poly[i], poly[(i + 1) % n]
        for j in range(sub):
            f = j / float(sub)
            x, y = x0 + (x1 - x0) * f, y0 + (y1 - y0) * f
            k = rng.uniform(k_lo, k_hi)
            out.append((ix + (x - ix) * k, iy + (y - iy) * k))
    return out


def build_crater(res, coll, rng):
    """The crushed spot (V5: whitish, <= 15 mm, no star): an irregular shallow pit on the struck face."""
    poly = res["stage1"]["crater"]["poly"]
    if not poly:
        return None
    ix, iy = res["impact"]
    far = res["far_z_sign"]
    face_z = -far * GLASS_T / 2                    # the struck (player) face
    inward = far                                   # into the glass
    acc = MeshAccum()
    outline = _irregular_outline(poly, ix, iy, rng)
    rim = [(x, y, face_z - inward * 0.00003) for x, y in outline]
    mid = []
    for x, y in outline:
        k = rng.uniform(0.3, 0.65)
        mid.append((ix + (x - ix) * k, iy + (y - iy) * k, face_z + inward * rng.uniform(0.0004, 0.0012)))
    apex = (ix, iy, face_z + inward * 0.0016)
    verts = rim + mid + [apex]
    n = len(outline)
    faces = []
    for i in range(n):
        j = (i + 1) % n
        faces.append([i, j, n + j, n + i])
        faces.append([n + i, n + j, 2 * n])
    acc.add([slab_point_to_bl(p) for p in verts], faces, [0] * len(faces), [(p[0], p[1]) for p in verts], mirrored=True)
    return acc.build("Crater", [MATS["crushed"]], coll)


def _piece_at(res, x, y):
    for pc in res["pieces"]:
        if pc["kind"] == "crush":
            continue
        poly = pc["poly"]
        n = len(poly)
        inside = True
        for i in range(n):
            (x0, y0), (x1, y1) = poly[i], poly[(i + 1) % n]
            if (x1 - x0) * (y - y0) - (y1 - y0) * (x - x0) < -1e-12:
                inside = False
                break
        if inside:
            return pc
    return None


def build_crushed_rim(res, coll, rng):
    """S2: the crushed core has fallen out; its edge stays crushed and white on the pieces around the hole."""
    poly = res["stage1"]["crater"]["poly"]
    if not poly:
        return None
    ix, iy = res["impact"]
    far = res["far_z_sign"]
    face_z = -far * (GLASS_T / 2 + 0.00003)
    inner = _irregular_outline(poly, ix, iy, rng, 1.0, 1.0, 3)
    acc = MeshAccum()
    verts = []
    for (x, y) in inner:
        d = math.hypot(x - ix, y - iy) or 1.0
        ux, uy = (x - ix) / d, (y - iy) / d
        w = rng.uniform(0.0008, 0.0035)
        a = (x + ux * 0.0002, y + uy * 0.0002)
        b = (x + ux * w, y + uy * w)
        pc = _piece_at(res, x + ux * 0.001, y + uy * 0.001)
        pose = pc["pose"] if pc is not None else {"pivot": [0, 0], "axis": [1, 0], "deg": 0.0, "push": 0.0}
        verts.append(cg.pose_point((a[0], a[1], face_z), pose, far))
        verts.append(cg.pose_point((b[0], b[1], face_z), pose, far))
    n = len(inner)
    faces = []
    for i in range(n):
        j = (i + 1) % n
        if rng.random() < 0.85:                    # a few gaps: the crush is not a perfect ring
            faces.append([2 * i, 2 * j, 2 * j + 1, 2 * i + 1])
    acc.add([slab_point_to_bl(p) for p in verts], faces, [0] * len(faces), [(p[0], p[1]) for p in verts], mirrored=True)
    return acc.build("CrushedRim", [MATS["crushed"]], coll)


def flake(rng, size, thick):
    """A small irregular glass flake (convex, 10-16 tris), local coordinates."""
    n = rng.randint(4, 6)
    pts = []
    for i in range(n):
        a = (i + rng.uniform(-0.3, 0.3)) * 2 * math.pi / n
        r = size * rng.uniform(0.35, 0.6)
        pts.append((r * math.cos(a), r * math.sin(a)))
    v, f, m = prism_lists(pts, thick, 0.0)
    return v, f


def place(v, M):
    return [M @ Vector(p) for p in v]


def build_chips(points, res, coll, rng, name, drop=0.0, toward_player=(0.01, 0.06), size=(0.003, 0.009)):
    acc = MeshAccum()
    far = res["far_z_sign"]
    for (x, y) in points:
        s = rng.uniform(*size)
        v, f = flake(rng, s, rng.uniform(0.0006, 0.0018))
        loc = slab_point_to_bl((x, y - drop * rng.uniform(0.8, 1.2), -far * rng.uniform(*toward_player)))
        M = Matrix.Translation(loc) @ (Matrix.Rotation(rng.uniform(0, 6.28), 4, "X") @
                                       Matrix.Rotation(rng.uniform(0, 6.28), 4, "Y") @
                                       Matrix.Rotation(rng.uniform(0, 6.28), 4, "Z"))
        acc.add(place(v, M), f, [0] * len(f), [(0.0, 0.0)] * len(v))
    if not acc.f:
        return None
    return acc.build(name, [MATS["glass_floor"]], coll)


def _bevel(res, bevel):
    """The bevel: an explicit argument, else a jobs-file override, else the profile's (JSON slab.bevel)."""
    if bevel is not None:
        return bevel
    if LOOK["bevel"] is not None:
        return LOOK["bevel"]
    return res["slab"].get("bevel", 0.0003)


def build_stage2(res, coll, bevel=None, inset=None):
    bevel = _bevel(res, bevel)
    inset = LOOK["inset"] if inset is None else inset
    acc = MeshAccum()
    far = res["far_z_sign"]
    for pc in res["pieces"]:
        if pc["kind"] == "crush":
            continue                                # the crushed core falls out at 0.70 (plan sec 2.3)
        poly = inset_poly([tuple(p) for p in pc["poly"]], inset)
        v, f, m = prism_lists(poly, GLASS_T, bevel)
        uvs = [(p[0], p[1]) for p in v]
        posed = [cg.pose_point(p, pc["pose"], far) for p in v] if pc["kind"] == "shard" else v
        acc.add([slab_point_to_bl(p) for p in posed], f, m, uvs, mirrored=True)
    return acc.build("Stage2", [MATS["glass"], MATS["fracture"]], coll)


def build_teeth(res, coll, bevel=None, inset=None):
    bevel = _bevel(res, bevel)
    inset = LOOK["inset"] if inset is None else inset
    acc = MeshAccum()
    for pc in res["pieces"]:
        if pc["kind"] != "tooth":
            continue
        poly = inset_poly([tuple(p) for p in pc["poly"]], inset)
        v, f, m = prism_lists(poly, GLASS_T, bevel)
        acc.add([slab_point_to_bl(p) for p in v], f, m, [(p[0], p[1]) for p in v], mirrored=True)
    if not acc.f:
        return None
    return acc.build("Teeth", [MATS["glass"], MATS["fracture"]], coll)


def land(res, rng):
    """Scripted landing for the look-dev (Unity uses rigid bodies in a separate physics scene, step 6).

    Each shard leaves at its release wave with the JSON's speed toward the far side (inner pieces), or tips out of
    the stop (outer pieces, 65 % to the far side); it falls ballistically (t = sqrt(2h/g) class), lands on the
    stool if its path crosses it, else on the carpet, slides a little, and lies flat; pieces that land on others
    stack and tilt. Returns [(piece, root position, yaw, tilt, flip)]."""
    out = []
    ix, iy = res["impact"]
    far = res["far_z_sign"]
    placed = []
    shards = [pc for pc in res["pieces"] if pc["kind"] == "shard"]
    shards.sort(key=lambda pc: pc.get("release", {}).get("ms", 0.0))
    for pc in shards:
        poly = [tuple(p) for p in pc["poly"]]
        cx, cy = cg.centroid(poly)
        a = pc["area"]
        rad = math.sqrt(a / math.pi)
        rel = pc.get("release", {"wave": 2, "speed": 0.0})
        d = math.hypot(cx - ix, cy - iy) or 1.0
        ox, oy = (cx - ix) / d, (cy - iy) / d
        if rel["wave"] == 0:
            # the inner pieces are thrown through by the strike, in a cone around the strike (20-35 deg half angle)
            vz = far * max(0.4, rel["speed"]) * rng.uniform(0.8, 1.2)
            vlat = abs(vz) * rng.uniform(0.35, 0.70)
        elif rel["wave"] == 1 and rng.random() < 0.75:
            vz = far * max(0.2, rel["speed"]) * rng.uniform(0.6, 1.2)
            vlat = abs(vz) * rng.uniform(0.25, 0.60)
        else:
            # outer pieces tip out of the stop and drop, to either side (backward fragmentation, RCMP91)
            sgn = far if rng.random() < 0.5 else -far
            vz = sgn * rng.uniform(0.08, 0.5)
            vlat = rng.uniform(0.0, 0.1)
        vx, vy = ox * vlat, oy * vlat + rng.uniform(-0.1, 0.25)
        x0, y0, z0 = cx, cy + SLAB_CY, far * pc["pose"]["push"]
        # stool crossing (top at 0.3505, |z| <= 0.121, |x| <= 0.80)
        y_top = 0.3505
        tland = None
        on_stool = False
        if y0 > y_top:
            disc = vy * vy + 2 * G * (y0 - y_top)
            ts = (vy + math.sqrt(disc)) / G
            zs, xs = z0 + vz * ts, x0 + vx * ts
            if abs(zs) <= 0.118 and abs(xs) <= 0.79:
                if rad <= 0.055:
                    on_stool, tland = True, ts
                else:
                    # too big for a 0.24 m stool: it hits the nosing and tips off to the side it was moving
                    tland = ts + math.sqrt(2 * y_top / G)
                    vz = math.copysign(max(abs(vz), 0.5) * rng.uniform(0.6, 1.0), vz if vz != 0 else far)
                    z0 = zs - vz * tland + math.copysign(0.121 + 0.4 * rad, vz)
        if not on_stool:
            disc = vy * vy + 2 * G * y0
            tland = (vy + math.sqrt(disc)) / G
        xl, zl = x0 + vx * tland, z0 + vz * tland
        yl = y_top if on_stool else 0.0
        if not on_stool:
            # a short slide in the carpet pile
            zl += vz * rng.uniform(0.01, 0.04)
            xl += vx * rng.uniform(0.01, 0.04)
            # the wall, apron and casing stand between |z| 0.08 and 0.105 below the stool
            clear = 0.105 + 0.5 * rad * rng.uniform(0.5, 1.0)
            if abs(zl) < clear:
                zl = math.copysign(clear, zl if zl != 0 else far)
        zl = max(min(zl, 6.0), -6.0)
        # stacking
        lift, tilt = 0.0, rng.uniform(0.0, 3.0)
        for (px, pz, pr, ptop) in placed:
            if math.hypot(px - xl, pz - zl) < 0.75 * (pr + rad) and abs(ptop - yl) < 0.05:
                lift = min(max(lift, ptop - yl + 0.0005), 2.5 * GLASS_T)
                tilt = rng.uniform(2.0, 8.0)
        y_rest = yl + lift
        placed.append((xl, zl, rad, y_rest + GLASS_T))
        out.append((pc, (xl, y_rest, zl), rng.uniform(0, 2 * math.pi), tilt, rng.random() < 0.5, on_stool))
    return out


def build_floor_glass(res, coll, rng, bevel=None):
    bevel = _bevel(res, bevel)
    acc = MeshAccum()
    landed = land(res, rng)
    stats = {"far": 0, "near": 0, "stool": 0}
    far = res["far_z_sign"]
    for pc, (x, y, z), yaw, tilt, flip, on_stool in landed:
        poly = [tuple(p) for p in pc["poly"]]
        cx, cy = cg.centroid(poly)
        local = [(px - cx, py - cy) for px, py in poly]
        v, f, m = prism_lists(local, GLASS_T, bevel)
        # piece plane (x, y) -> floor plane; z (thickness) -> up
        R = (Matrix.Rotation(yaw, 4, "Z") @ Matrix.Rotation(math.radians(tilt), 4, "X") @
             (Matrix.Rotation(math.pi, 4, "X") if flip else Matrix.Identity(4)))
        M = Matrix.Translation(BL(x, y + GLASS_T / 2, z)) @ R
        verts = [M @ Vector((p[0], p[1], p[2])) for p in v]
        acc.add(verts, f, m, [(p[0] + cx, p[1] + cy) for p in v])
        if on_stool:
            stats["stool"] += 1
        elif z * far > 0:
            stats["far"] += 1
        else:
            stats["near"] += 1
    ob = acc.build("FloorGlass", [MATS["glass"], MATS["fracture"]], coll)
    return ob, stats, landed


def build_glitter(res, coll, rng, n_near=160, n_far=220, n_stool=30):
    """Fine glitter: backward fragments mostly right under the frame (RCMP91: count falls 4-5x per 45 cm),
    the bulk of the crumbs on the far side, some on the stool."""
    acc = MeshAccum()
    far = res["far_z_sign"]

    def add(x, y, z, s):
        v, f = flake(rng, s, rng.uniform(0.0004, 0.0012))
        M = Matrix.Translation(BL(x, y + 0.0006, z)) @ Matrix.Rotation(rng.uniform(0, 6.28), 4, "Z") @ \
            Matrix.Rotation(math.radians(rng.uniform(-15, 15)), 4, "X")
        acc.add(place(v, M), f, [0] * len(f), [(0.0, 0.0)] * len(v))
    for i in range(n_near):
        d = 0.105 + rng.expovariate(1.0 / 0.14)
        add(rng.uniform(-0.75, 0.75), 0.0, -far * d, rng.uniform(0.0015, 0.006))
    for i in range(n_far):
        d = 0.105 + rng.expovariate(1.0 / 0.40)
        add(rng.gauss(0.0, 0.45), 0.0, far * d, rng.uniform(0.002, 0.010))
    for i in range(n_stool):
        add(rng.uniform(-0.70, 0.70), 0.3505, rng.uniform(-0.115, 0.115), rng.uniform(0.0015, 0.005))
    return acc.build("Glitter", [MATS["glass_floor"]], coll)


def build_granules(res, coll, rng):
    """Tempered6 S4: every clump lands and spreads into ~9 mm granules; a few clumps stay half-joined."""
    acc = MeshAccum()
    g = res["granule"]
    far = res["far_z_sign"]
    ix, iy = res["impact"]
    count = 0
    for pc in res["pieces"]:
        poly = [tuple(p) for p in pc["poly"]]
        cx, cy = cg.centroid(poly)
        rel = pc["release"]
        y0 = cy + SLAB_CY
        vz = far * max(0.3, rel["speed"]) if rng.random() < 0.7 else -far * rng.uniform(0.1, 0.4)
        t = math.sqrt(2 * y0 / G)
        xl, zl = cx + rng.uniform(-0.05, 0.05), vz * t
        if abs(zl) < 0.11:
            zl = math.copysign(0.11 + rng.uniform(0.0, 0.08), zl if zl != 0 else far)
        n = int(pc["area"] / (g * g) * 0.30)          # draw 30 % of the granules (the rest is under them)
        spread = 0.06 + 0.10 * rng.random()
        for i in range(n):
            gx = xl + rng.gauss(0.0, spread)
            gz = zl + rng.gauss(0.0, spread * 0.8)
            if abs(gz) < 0.105:
                gz = math.copysign(0.105 + rng.uniform(0, 0.03), gz if gz != 0 else far)
            sx, sy = g * rng.uniform(0.7, 1.25), g * rng.uniform(0.7, 1.25)
            pts = [(-sx / 2, -sy / 2), (sx / 2, -sy / 2), (sx / 2, sy / 2), (-sx / 2, sy / 2)]
            v, f, m = prism_lists(pts, GLASS_T, 0.0)
            M = Matrix.Translation(BL(gx, GLASS_T / 2 + rng.uniform(0, 0.004), gz)) @ \
                Matrix.Rotation(rng.uniform(0, 6.28), 4, "Z") @ Matrix.Rotation(math.radians(rng.uniform(-25, 25)), 4, "X")
            acc.add(place(v, M), f, m, [(0.0, 0.0)] * len(v))
            count += 1
    ob = acc.build("Granules", [MATS["glass_floor"], MATS["glass_floor"]], coll)
    return ob, count


# ---------------------------------------------------------------------------------------------------------
# Cameras and render
# ---------------------------------------------------------------------------------------------------------
def view_pose(view, res):
    hx_root, hy_root = res["impact_root"]
    far = res["far_z_sign"]
    if view == "C1":
        eye = (max(-0.25, min(0.25, hx_root)), EYE, -far * 0.55)
        at = (hx_root, hy_root, 0.0)
    elif view == "C2":
        eye = (-1.061, EYE, -far * 1.061)
        at = (0.0, SLAB_CY, 0.0)
    elif view == "C3":
        eye = (0.0, EYE, -far * 1.2)
        at = (0.0, SLAB_CY, 0.0)
    elif view == "C4":
        eye = (0.0, EYE, far * 2.0)
        at = (0.0, 0.0, far * 0.5)
    else:
        raise ValueError(view)
    return eye, at


def set_camera(view, res):
    sc = bpy.context.scene
    cam = sc.camera
    eye, at = view_pose(view, res)
    e, a = BL(*eye), BL(*at)
    cam.location = e
    d = (a - e).normalized()
    cam.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
    return eye, at


def setup_scene(res_xy, samples, gpu, exposure):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    if gpu:
        prefs = bpy.context.preferences.addons["cycles"].preferences
        try:
            prefs.compute_device_type = "METAL"
            prefs.get_devices()
            for d in prefs.devices:
                d.use = d.type != "CPU"
            sc.cycles.device = "GPU"
        except Exception as e:                      # noqa: BLE001
            print("[lookdev] GPU unavailable, CPU render:", e)
    cy = sc.cycles
    cy.samples = samples
    cy.use_adaptive_sampling = True
    cy.adaptive_threshold = 0.015
    cy.use_denoising = True
    try:
        cy.denoiser = "OPENIMAGEDENOISE"
    except Exception:                               # noqa: BLE001
        pass
    cy.max_bounces = 32
    cy.diffuse_bounces = 4
    cy.glossy_bounces = 16
    cy.transmission_bounces = 32
    cy.transparent_max_bounces = 16
    cy.volume_bounces = 0
    cy.caustics_reflective = True
    cy.caustics_refractive = True
    cy.blur_glossy = 0.5
    cy.sample_clamp_indirect = 20.0
    sc.render.resolution_x, sc.render.resolution_y = res_xy
    sc.render.resolution_percentage = 100
    sc.view_settings.view_transform = "AgX"
    sc.view_settings.look = "AgX - Medium High Contrast"
    sc.view_settings.exposure = exposure
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_depth = "8"
    world = bpy.data.worlds.new("World")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0, 0, 0, 1)
    sc.world = world
    cd = bpy.data.cameras.new("GameCam")
    cd.sensor_fit = "VERTICAL"
    cd.lens_unit = "FOV"
    cd.angle_y = math.radians(VFOV_DEG)
    cd.clip_start = 0.06
    cd.clip_end = 60.0
    cam = bpy.data.objects.new("GameCam", cd)
    sc.collection.objects.link(cam)
    sc.camera = cam


def clear_stage(coll):
    for ob in list(coll.objects):
        me = ob.data
        bpy.data.objects.remove(ob, do_unlink=True)
        if me is not None and me.users == 0:
            bpy.data.meshes.remove(me)


def build_stage(res, stage, coll, seed):
    rng = random.Random(seed * 7919 + {"S1": 1, "S2": 2, "S4": 4}[stage])
    info = {"stage": stage}
    dice = res.get("mode") == "dice"
    if stage in ("S1", "S2") and dice:
        build_slab_intact(coll)
        info["note"] = "tempered: no crack stage, the pane stays intact until the shatter"
        return info
    if stage == "S1":
        build_slab_intact(coll)
        build_fins(res, coll, rng)
        build_crater(res, coll, rng)
        build_chips(res["stage1"]["chips"], res, coll, rng, "ChipsS1", drop=0.0, toward_player=(0.004, 0.03))
        info["fins"] = len(res["stage1"]["cracks"])
        info["chips"] = len(res["stage1"]["chips"])
    elif stage == "S2":
        build_stage2(res, coll)
        build_crushed_rim(res, coll, rng)
        # S1 chips have fallen ~0.6 m by 0.70 s; the S2 chips leave the new cracks
        build_chips(res["stage1"]["chips"], res, coll, rng, "ChipsS1Falling", drop=0.6, toward_player=(0.05, 0.25))
        build_chips(res["stage2"]["chips"], res, coll, rng, "ChipsS2", drop=0.0, toward_player=(0.004, 0.03))
        build_glitter(res, coll, rng, n_near=0, n_far=0, n_stool=12)
        info["pieces"] = sum(1 for p in res["pieces"] if p["kind"] != "crush")
    elif stage == "S4":
        if dice:
            ob, n = build_granules(res, coll, rng)
            info["granules_drawn"] = n
            build_glitter(res, coll, rng, n_near=120, n_far=200, n_stool=25)
        else:
            build_teeth(res, coll)
            ob, st, landed = build_floor_glass(res, coll, rng)
            info["landed"] = st
            build_glitter(res, coll, rng)
    return info


def render_to(path):
    sc = bpy.context.scene
    sc.render.filepath = path
    t0 = time.time()
    bpy.ops.render.render(write_still=True)
    return time.time() - t0


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    import argparse
    ap = argparse.ArgumentParser()
    ap.add_argument("--jobs")
    ap.add_argument("--pattern")
    ap.add_argument("--stage", default="S2")
    ap.add_argument("--view", default="C1")
    ap.add_argument("--out")
    ap.add_argument("--res", type=int, nargs=2, default=[1920, 1080])
    ap.add_argument("--samples", type=int, default=256)
    ap.add_argument("--exposure", type=float, default=0.0)
    ap.add_argument("--lamp", type=float, default=14.0)
    ap.add_argument("--cpu", action="store_true")
    a = ap.parse_args(argv)
    if a.jobs:
        spec = json.load(open(a.jobs))
    else:
        spec = {"jobs": [{"pattern": a.pattern, "stages": [a.stage], "views": {a.stage: [a.view]}, "out": a.out}]}
    res_xy = spec.get("res", a.res)
    samples = spec.get("samples", a.samples)
    exposure = spec.get("exposure", a.exposure)
    lamp = spec.get("lamp", a.lamp)
    LOOK["bevel"] = spec.get("bevel", LOOK["bevel"])          # None = the profile's bevel (JSON slab.bevel)
    LOOK["fracture_rough"] = tuple(spec.get("fracture_rough", LOOK["fracture_rough"]))
    setup_scene(res_xy, samples, not a.cpu and spec.get("gpu", True), exposure)
    if spec.get("border"):
        sc = bpy.context.scene
        sc.render.use_border = True
        sc.render.use_crop_to_border = True
        sc.render.border_min_x, sc.render.border_min_y, sc.render.border_max_x, sc.render.border_max_y = spec["border"]
    materials(lamp)
    build_room()
    stage_coll = bpy.data.collections.new("Stage")
    bpy.context.scene.collection.children.link(stage_coll)
    log = []
    default_views = {"S1": ["C1", "C2"], "S2": ["C1", "C2"], "S4": ["C1", "C2", "C4"]}
    for job in spec["jobs"]:
        res = json.load(open(job["pattern"]))
        for stage in job.get("stages", ["S1", "S2", "S4"]):
            clear_stage(stage_coll)
            info = build_stage(res, stage, stage_coll, res["seed"])
            for view in job.get("views", {}).get(stage, default_views[stage]):
                eye, at = set_camera(view, res)
                out = job.get("out") or "%s_%s_%s.png" % (job["out_prefix"], stage, view)
                dt = render_to(out)
                rec = {"pattern": os.path.basename(job["pattern"]), "stage": stage, "view": view, "out": out,
                       "render_s": round(dt, 1), "load1": round(os.getloadavg()[0], 1), "eye": eye, "at": at,
                       "info": info, "res": res_xy, "samples": samples}
                log.append(rec)
                print("[lookdev]", json.dumps(rec))
    if spec.get("log"):
        with open(spec["log"], "w") as f:
            json.dump(log, f, indent=1)


main()
