"""Skinned production creatures: armature, weights, pose keys, bakes and the FBX (production_b plan §2-§4).

Generic helpers used by creatures/relay_night_shift.py. Conventions as the kit (creature_lib):
metres, Z up, the creature faces -Y (Unity +Z), feet on z = 0, nav axis at x = y = 0.

Pose keys are stored as ARMATURE-SPACE deltas, so the imported bone rolls never matter:
  delta[b] = A_pose[b] * A_bind[b]^T      (A = an orthonormal frame built from the bone's direction
                                           and a reference vector computed the same way in every pose)
  off[b]   = delta[parent]^T (head_pose[b] - head_pose[parent])     (offset in the parent's bind frame)
so forward kinematics at runtime is  rot[b] = delta[b] * bindRot[b],  pos[b] = pos[parent] + delta[parent] * off[b].
Unity converts the Blender vectors with the axis map it measures from the bind heads (JSON "bind").
"""

import json
import math

import bmesh
import bpy
import numpy as np
from mathutils import Matrix, Quaternion, Vector


def V(x, y, z):
    return Vector((x, y, z))


# ------------------------------------------------------------------ frames
def aux_frame(direction, ref):
    """Orthonormal frame (columns x, y, z): y along `direction`, x toward `ref` (orthogonalised)."""
    y = direction.normalized()
    x = ref - y * ref.dot(y)
    if x.length < 1e-6:
        for alt in (V(1, 0, 0), V(0, 0, 1), V(0, 1, 0)):
            x = alt - y * alt.dot(y)
            if x.length > 1e-3:
                break
    x.normalize()
    z = x.cross(y).normalized()
    return Matrix((x, y, z)).transposed()


def bend_normal(a, m, c, pole):
    """Normal of the plane of a two-bone chain a-m-c (thigh/shin, upper arm/forearm); falls back on the
    pole hint when the chain is straight, so it never flips between poses."""
    ac = c - a
    b = m - (a + ac.normalized() * (m - a).dot(ac.normalized()))
    if b.length < 0.02:
        b = pole - ac.normalized() * pole.dot(ac.normalized())
    n = ac.cross(b)
    if n.length < 1e-6:
        n = V(1, 0, 0)
    return n.normalized()


def rot_axis(axis, angle):
    return Matrix.Rotation(angle, 3, axis.normalized())


def signed_angle(a, b, axis):
    """Angle from a to b about axis (both projected onto the plane normal to axis)."""
    ax = axis.normalized()
    pa = (a - ax * a.dot(ax))
    pb = (b - ax * b.dot(ax))
    if pa.length < 1e-6 or pb.length < 1e-6:
        return 0.0
    pa.normalize()
    pb.normalize()
    return math.atan2(ax.dot(pa.cross(pb)), max(-1.0, min(1.0, pa.dot(pb))))


# ------------------------------------------------------------------ armature
def build_armature(name, bones, mirror_suffix=("_L", "_R")):
    """bones: [dict(name, head, tail, parent, zref, deform)] with only the centre and _L bones;
    the _R side is made by Blender's Symmetrize (+X -> -X), so both sides have identical lengths."""
    arm = bpy.data.armatures.new(name)
    obj = bpy.data.objects.new(name, arm)
    bpy.context.scene.collection.objects.link(obj)
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode="EDIT")
    made = {}
    for b in bones:
        e = arm.edit_bones.new(b["name"])
        e.head = b["head"]
        e.tail = b["tail"]
        e.align_roll(b["zref"])
        e.use_connect = False
        e.use_deform = b.get("deform", True)
        if b.get("parent"):
            e.parent = made[b["parent"]]
        made[b["name"]] = e
    for e in arm.edit_bones:
        sel = e.name.endswith(mirror_suffix[0])
        e.select = e.select_head = e.select_tail = sel
    bpy.ops.armature.symmetrize(direction="POSITIVE_X")
    bpy.ops.object.mode_set(mode="OBJECT")
    return obj


def bone_order(arm_obj):
    """Bones parents-first (stable: the edit order)."""
    out, seen = [], set()
    bones = list(arm_obj.data.bones)

    def visit(b):
        if b.name in seen:
            return
        if b.parent is not None:
            visit(b.parent)
        seen.add(b.name)
        out.append(b)
    for b in bones:
        visit(b)
    return out


# ------------------------------------------------------------------ weights
def seg_dist(P, a, b):
    """Distances from points P (N x 3) to segment a-b, and the segment parameter t."""
    ab = b - a
    L2 = max(float(ab.dot(ab)), 1e-12)
    t = np.clip(((P - a) @ ab) / L2, 0.0, 1.0)
    q = a + t[:, None] * ab
    return np.linalg.norm(P - q, axis=1), t


def adjacency(mesh):
    """Vertex adjacency as (i, j) edge arrays."""
    e = np.zeros(len(mesh.edges) * 2, dtype=np.int64)
    mesh.edges.foreach_get("vertices", e)
    e = e.reshape(-1, 2)
    return e


def smooth_weights(W, edges, iterations=4, lam=0.5, keep=None):
    """Laplacian smoothing of an N x K weight matrix over the mesh edges (rows renormalised).
    `keep` (N bool) pins vertices (rigid parts)."""
    n = W.shape[0]
    if len(edges) == 0:
        return W
    deg = np.bincount(edges.ravel(), minlength=n).astype(np.float64)
    deg[deg == 0] = 1.0
    for _ in range(iterations):
        acc = np.zeros_like(W)
        np.add.at(acc, edges[:, 0], W[edges[:, 1]])
        np.add.at(acc, edges[:, 1], W[edges[:, 0]])
        acc /= deg[:, None]
        Wn = (1 - lam) * W + lam * acc
        if keep is not None:
            Wn[keep] = W[keep]
        s = Wn.sum(axis=1, keepdims=True)
        s[s == 0] = 1.0
        W = Wn / s
    return W


def limit_weights(W, max_influences, prune=0.02):
    """Keep the strongest `max_influences` per row (an int or an N int array), prune, renormalise."""
    W = W.copy()
    n, k = W.shape
    order = np.argsort(-W, axis=1)
    mi = np.full(n, max_influences) if np.isscalar(max_influences) else np.asarray(max_influences)
    rank = np.empty_like(order)
    rows = np.arange(n)[:, None]
    rank[rows, order] = np.arange(k)[None, :]
    W[rank >= mi[:, None]] = 0.0
    W[W < prune] = 0.0
    s = W.sum(axis=1, keepdims=True)
    bad = s[:, 0] == 0
    if bad.any():
        W[bad, order[bad, 0]] = 1.0
        s = W.sum(axis=1, keepdims=True)
    return W / s


def write_groups(obj, names, W, vidx=None):
    """Write weights (N x K for vertices vidx, default all) into vertex groups named `names`."""
    groups = {}
    for nme in names:
        groups[nme] = obj.vertex_groups.get(nme) or obj.vertex_groups.new(name=nme)
    vidx = np.arange(W.shape[0]) if vidx is None else vidx
    for k, nme in enumerate(names):
        col = W[:, k]
        nz = np.nonzero(col > 0)[0]
        g = groups[nme]
        # group by weight value is slow; add one by one but in Python ints
        for i in nz:
            g.add([int(vidx[i])], float(col[i]), "REPLACE")


def read_groups(obj, names):
    """N x K weights for the named vertex groups."""
    idx = {g.name: g.index for g in obj.vertex_groups}
    W = np.zeros((len(obj.data.vertices), len(names)))
    col = {idx[n]: k for k, n in enumerate(names) if n in idx}
    for v in obj.data.vertices:
        for ge in v.groups:
            k = col.get(ge.group)
            if k is not None:
                W[v.index, k] = ge.weight
    return W


def set_all_weights(obj, names, W):
    """Replace every vertex group on obj with W (N x K, rows over all vertices)."""
    for g in list(obj.vertex_groups):
        obj.vertex_groups.remove(g)
    write_groups(obj, names, W)


# ------------------------------------------------------------------ posing
def apply_pose(arm_obj, pose_mats):
    """pose_mats: {bone name: 4x4 armature-space matrix}; set parents first, updating as we go."""
    order = bone_order(arm_obj)
    bpy.context.view_layer.objects.active = arm_obj
    for b in order:
        pb = arm_obj.pose.bones[b.name]
        if b.name in pose_mats:
            pb.matrix = pose_mats[b.name]
        else:
            pb.matrix_basis = Matrix.Identity(4)
        bpy.context.view_layer.update()


def rest_pose(arm_obj):
    for pb in arm_obj.pose.bones:
        pb.matrix_basis = Matrix.Identity(4)
    bpy.context.view_layer.update()


def evaluated_positions(obj):
    """World positions of an object's evaluated (deformed) mesh, N x 3."""
    dg = bpy.context.evaluated_depsgraph_get()
    ev = obj.evaluated_get(dg)
    me = ev.to_mesh()
    co = np.zeros(len(me.vertices) * 3)
    me.vertices.foreach_get("co", co)
    co = co.reshape(-1, 3)
    mw = np.array(ev.matrix_world)
    co = co @ mw[:3, :3].T + mw[:3, 3]
    ev.to_mesh_clear()
    return co


def mesh_positions(obj):
    co = np.zeros(len(obj.data.vertices) * 3)
    obj.data.vertices.foreach_get("co", co)
    co = co.reshape(-1, 3)
    mw = np.array(obj.matrix_world)
    return co @ mw[:3, :3].T + mw[:3, 3]


# ------------------------------------------------------------------ JSON
def qlist(m3):
    q = m3.to_quaternion()
    return [round(q.x, 6), round(q.y, 6), round(q.z, 6), round(q.w, 6)]


def vlist(v, nd=5):
    return [round(float(v[0]), nd), round(float(v[1]), nd), round(float(v[2]), nd)]


def write_json(path, data):
    with open(path, "w") as fh:
        json.dump(data, fh, indent=1)


# ------------------------------------------------------------------ images
def new_image(name, w, h, float_buffer=False, colorspace="sRGB", alpha=True):
    img = bpy.data.images.get(name)
    if img is not None:
        bpy.data.images.remove(img)
    img = bpy.data.images.new(name, w, h, alpha=alpha, float_buffer=float_buffer)
    img.colorspace_settings.name = colorspace
    return img


def image_array(img):
    a = np.zeros(img.size[0] * img.size[1] * 4, dtype=np.float32)
    img.pixels.foreach_get(a)
    return a.reshape(img.size[1], img.size[0], 4)


def save_png(arr, path, colorspace="sRGB", name="out"):
    """arr: H x W x 3/4 float 0..1 in the FILE's encoding (sRGB values for albedo); rows bottom-up (Blender)."""
    h, w = arr.shape[:2]
    if arr.shape[2] == 3:
        arr = np.concatenate([arr, np.ones((h, w, 1), dtype=arr.dtype)], axis=2)
    img = new_image(name, w, h, float_buffer=False, colorspace=colorspace)
    img.pixels.foreach_set(np.clip(arr, 0, 1).astype(np.float32).ravel())
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()
    bpy.data.images.remove(img)


def srgb_encode(lin):
    lin = np.clip(lin, 0, 1)
    return np.where(lin <= 0.0031308, lin * 12.92, 1.055 * np.power(lin, 1 / 2.4) - 0.055)


def srgb_decode(s):
    s = np.clip(s, 0, 1)
    return np.where(s <= 0.04045, s / 12.92, np.power((s + 0.055) / 1.055, 2.4))


def hex_lin(h):
    h = h.lstrip("#")
    s = np.array([int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4)])
    return srgb_decode(s)


def dilate(arr, mask, steps=8):
    """Push colours outward from mask (H x W bool) by `steps` px (bake margin for composites)."""
    out = arr.copy()
    m = mask.copy()
    for _ in range(steps):
        acc = np.zeros_like(out)
        cnt = np.zeros(m.shape, dtype=np.float32)
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            sm = np.roll(m, (dy, dx), axis=(0, 1))
            sa = np.roll(out, (dy, dx), axis=(0, 1))
            acc += sa * sm[..., None]
            cnt += sm
        grow = (~m) & (cnt > 0)
        out[grow] = acc[grow] / cnt[grow][:, None]
        m = m | grow
    return out


def upsample(arr, factor):
    return np.repeat(np.repeat(arr, factor, axis=0), factor, axis=1)


def box_blur(a, r):
    """Separable box blur, radius r px (wraps; fine inside islands with dilation)."""
    if r <= 0:
        return a
    out = a.copy()
    for axis in (0, 1):
        acc = np.zeros_like(out)
        for d in range(-r, r + 1):
            acc += np.roll(out, d, axis=axis)
        out = acc / (2 * r + 1)
    return out


def normal_from_height(hgt, strength):
    """Tangent-space normal (OpenGL, +Y up the image) from a height field (rows bottom-up)."""
    gy, gx = np.gradient(hgt)
    n = np.stack([-gx * strength, -gy * strength, np.ones_like(hgt)], axis=-1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    return n


def blend_normals(base, detail):
    """Reoriented normal mapping (base, detail: unit tangent normals)."""
    t = base + np.array([0, 0, 1.0])
    u = detail * np.array([-1.0, -1.0, 1.0])
    r = t * np.sum(t * u, axis=-1, keepdims=True) / np.maximum(t[..., 2:3], 1e-4) - u
    r /= np.linalg.norm(r, axis=-1, keepdims=True)
    return r


# ------------------------------------------------------------------ export
def export_skinned_fbx(path, arm_obj, mesh_objs):
    bpy.ops.object.select_all(action="DESELECT")
    arm_obj.select_set(True)
    for o in mesh_objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=False,
        mesh_smooth_type="OFF",
        use_mesh_modifiers=True,
        use_triangles=True,
        use_tspace=True,
        add_leaf_bones=False,
        primary_bone_axis="Y",
        secondary_bone_axis="X",
        use_armature_deform_only=False,
        armature_nodetype="NULL",
        bake_anim=False,
        path_mode="STRIP",
    )
