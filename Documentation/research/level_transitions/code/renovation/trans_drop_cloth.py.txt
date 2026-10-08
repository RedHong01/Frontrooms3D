"""A canvas drop cloth folded into a soft bundle against the wall, one corner
pulled out onto the floor (level transitions, V2 Renovation).

Real-world reference: cotton duck painter's drop cloth, 9 x 12 ft, folded to
about 0.46 x 0.34 m, ~0.11 m high; the folded edges show as soft layers on one
side; primer and paint drips (in the Prop_Canvas texture). Unbranded.

Budget ~2 k tris. Slot: Prop_Canvas.
"""

import math
import random
import bmesh

import kitlib

NAME = "Kit_TransDropCloth"
LOD1 = 0.5
SMOOTH_ANGLE = 70.0

kitlib.register_slot("Prop_Canvas", (0.70, 0.65, 0.55), 0.95)
CANVAS = "Prop_Canvas"


def build(kit):
    rng = random.Random(1989)
    # The bundle: stacked folds, each a soft slab slightly offset.
    z = 0.0
    for i, h in enumerate((0.032, 0.028, 0.026, 0.024)):
        dx, dy = rng.uniform(-0.008, 0.008), rng.uniform(-0.01, 0.01)
        kit.soft_box((0.46 + rng.uniform(-0.01, 0.01), 0.34 + rng.uniform(-0.01, 0.01), h + 0.006), (dx, dy, z + h / 2), CANVAS,
                     radius=0.014, segments=18, rings=8, sag=0.004 if i == 3 else 0.0, puff=0.004, rot=(0, 0, rng.uniform(-1.5, 1.5)), name="fold")
        z += h
    # A corner pulled out over the front edge and onto the floor: one smooth sheet.
    bm = bmesh.new()
    nu, nv = 16, 6
    grid = []
    top = z
    for j in range(nv + 1):
        v = j / nv
        row = []
        for i in range(nu + 1):
            u = i / nu
            # Arc length s: 0.03 over the top edge, down the front, then out across the floor.
            s_ = u * 0.40
            if s_ < 0.03:
                y, zz = -0.165 - s_ * 0.6, top + 0.002 - s_ * 0.3
            elif s_ < 0.03 + top:
                k = (s_ - 0.03) / top
                y, zz = -0.183 - 0.025 * math.sin(k * math.pi), top - (s_ - 0.03) + 0.002
            else:
                y, zz = -0.183 - (s_ - 0.03 - top), 0.0025
            x = -0.21 + v * (0.24 - 0.10 * u) + 0.06 * u
            ripple = 0.0025 * math.sin(i * 1.1 + j * 0.7) if zz < 0.01 else 0.0
            row.append(bm.verts.new((x, y, zz + ripple)))
        grid.append(row)
    for j in range(nv):
        for i in range(nu):
            bm.faces.new((grid[j][i], grid[j][i + 1], grid[j + 1][i + 1], grid[j + 1][i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    # Outside of the cloth faces away from the bundle: the floor part looks up.
    last = [f for f in bm.faces if f.calc_center_median().z < 0.01]
    if last and sum(f.normal.z for f in last) < 0:
        for f in bm.faces:
            f.normal_flip()
    flap = kit._new_object("flap", bm, CANVAS, "metres", "xz")
    mod = flap.modifiers.new("solid", "SOLIDIFY")
    mod.thickness = 0.003
    mod.offset = 1
    kit.collider((0, 0, z / 2), (0.47, 0.35, z))
    kit.tag("renovation", "work_prop")
