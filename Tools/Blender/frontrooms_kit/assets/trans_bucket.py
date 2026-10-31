"""Unbranded 5-gallon joint-compound pail, open, with a 6-inch taping knife
across the rim (level transitions, V2 Renovation: work props).

Real-world reference: the HDPE pail every US drywall crew used by 1990
(ready-mixed compound came in 4.5-5 gal pails from the 1960s on): 0.30 m top,
0.26 m base, 0.37 m tall, tapered body, two stiffening rings and a lid flange
under a rolled rim, wire bail on moulded ears with a plastic grip. No label,
no embossed brand. Compound two-thirds up, paddle swirl, smears on the rim;
a stiff 6" taping knife (steel blade, black handle) laid across the top.

Budget ~2.5 k tris LOD0. Slots: PlasticWhite, Compound, Aluminium (steel), PlasticBlack.
"""

import math
import random
import bmesh
import bpy
from mathutils import Vector

import kitlib

NAME = "Kit_TransBucket"
LOD1 = 0.45
SMOOTH_ANGLE = 40.0

kitlib.register_slot("Prop_Compound", (0.86, 0.85, 0.81), 0.85)

PAIL, MUD, STEEL, GRIP = "Prop_PlasticWhite", "Prop_Compound", "Prop_Aluminium", "Prop_PlasticBlack"
H = 0.370
MUD_Z = 0.255


def _r(z):
    """Outer body radius (tapered) at height z."""
    return 0.131 + (0.148 - 0.131) * max(0.0, min(1.0, (z - 0.02) / 0.28))


def build(kit):
    rng = random.Random(1990)
    # Pail shell: outside up, rolled rim, inside down to the compound.
    prof = [
        (0.124, 0.000), (0.129, 0.003), (0.131, 0.012), (0.1305, 0.020),
        (_r(0.10), 0.10), (_r(0.20), 0.20), (_r(0.295), 0.295),
        (0.1535, 0.300), (0.1535, 0.312), (0.149, 0.316), (0.149, 0.322),
        (0.1535, 0.326), (0.1535, 0.338), (0.150, 0.342),
        (0.1575, 0.350), (0.1580, 0.362), (0.1550, 0.368), (0.1515, 0.3705),
        (0.1475, 0.3690), (0.1455, 0.360), (0.1440, 0.330), (0.1415, 0.290), (0.1395, MUD_Z - 0.004),
    ]
    kit.lathe(prof, (0, 0, 0), PAIL, verts=48, close_top=False, close_bottom=True, name="pail")
    # Compound: a swirled surface (paddle marks) with a heap where the knife scooped.
    bm = bmesh.new()
    rings, segs = 9, 48
    grid = []
    centre = bm.verts.new((0, 0, MUD_Z + 0.012))
    for i in range(1, rings + 1):
        rr = 0.141 * i / rings
        ring = []
        for j in range(segs):
            a = 2 * math.pi * j / segs
            swirl = 0.006 * math.sin(3 * a + 28 * rr) * (rr / 0.141)
            heap = 0.018 * math.exp(-(((rr * math.cos(a) - 0.05) ** 2 + (rr * math.sin(a) + 0.03) ** 2) / 0.0025))
            z = MUD_Z + 0.010 * (1 - (rr / 0.141) ** 2) + swirl + heap + 0.0015 * rng.uniform(-1, 1)
            ring.append(bm.verts.new((rr * math.cos(a), rr * math.sin(a), z)))
        grid.append(ring)
    for j in range(segs):
        bm.faces.new((centre, grid[0][j], grid[0][(j + 1) % segs]))
    for i in range(rings - 1):
        for j in range(segs):
            k = (j + 1) % segs
            bm.faces.new((grid[i][j], grid[i + 1][j], grid[i + 1][k], grid[i][k]))
    for f in bm.faces:
        f.normal_update()
        if f.normal.z < 0:
            f.normal_flip()
    kit._new_object("compound", bm, MUD, "metres", "xz")
    # Smears of compound on the rim and down the outside.
    for k in range(5):
        a = rng.uniform(0, 2 * math.pi)
        sz = (rng.uniform(0.02, 0.045), rng.uniform(0.012, 0.02), rng.uniform(0.004, 0.007))
        kit.soft_box(sz, (0.152 * math.cos(a), 0.152 * math.sin(a), rng.uniform(0.35, 0.368)), MUD,
                     radius=0.004, segments=10, rings=6, rot=(0, rng.uniform(-30, 10), math.degrees(a) + 90), name="smear")
    # Bail ears and the wire bail hanging down the front, plastic grip.
    for sx in (-1, 1):
        kit.box((0.012, 0.032, 0.026), (sx * 0.152, 0, 0.334), PAIL, bevel=0.003, segments=1, name="ear")
    pts = []
    for i in range(25):
        phi = math.pi * i / 24
        pts.append((0.159 * math.cos(phi), -0.153 * math.sin(phi), 0.334 - 0.118 * math.sin(phi)))
    kit.tube(pts, 0.0024, STEEL, verts=8, name="bail")
    kit.cylinder(0.0115, 0.11, (0, -0.1545, 0.216), GRIP, verts=14, rot=(0, 90, 0), bevel=0.003, name="grip")
    # 6" taping knife across the top: blade on the far rim, handle on the near rim.
    blade_outline = [(-0.076, 0.0), (0.076, 0.0), (0.03, 0.105), (-0.03, 0.105)]
    tilt = 4.0
    kit.extrude(blade_outline, 0.0009, (0.01, 0.07, 0.3725), STEEL, plane="xy", rot=(tilt, 0, 180 + 8), bevel=0.0, name="blade")
    kit.box((0.026, 0.012, 0.004), (0.003, -0.045, 0.374), STEEL, bevel=0.001, segments=1, rot=(tilt, 0, 8), name="ferrule")
    kit.box((0.028, 0.115, 0.019), (-0.006, -0.11, 0.381), GRIP, bevel=0.006, segments=2, rot=(tilt + 3, 0, 8), name="handle")
    kit.soft_box((0.05, 0.035, 0.008), (0.02, 0.07, 0.377), MUD, radius=0.004, segments=10, rings=6, name="knife mud")

    kit.collider((0, 0, H / 2), (0.32, 0.32, H))
    kit.tag("renovation", "work_prop")


SLOTS_USED = (PAIL, MUD, STEEL, GRIP)
