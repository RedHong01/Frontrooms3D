"""Kit_ConduitRun_StationKey: the 1/2 in EMT from the key plate's box to the annunciator (30L S13, §10.2 S13;
NOTE_1022_photos.md "conduit from below"; plan 70 P05).

One static run (ModuleProp has no scale; plan GAP 9 i), the section of outlet_conduit.py: 1/2 in EMT OD 17.9 mm,
galvanised, on clamp-back spacers so its centre is 20.5 mm off the wall (the boxes' knock-out line). It leaves the
key plate's box downward, bends at y 1.00 (7 cm over the crash rail's top), runs 1.0 m along the wall toward the
annunciator and rises 0.25 m into its bottom knock-out. Three one-hole straps. Never from the top or the side.

ORIGIN = THE WALL FACE AT FLOOR LEVEL under the key plate's box; the run goes along local +X (Unity), which the
module's yaw 270 on the riser's west face turns to world +Z (north). Heights baked in.
Budget ~2,000 / 700 / 0. Slots Prop_Aluminium, Prop_Chrome. No collider.
"""

import math

import _station_common as sc

NAME = "Kit_ConduitRun_StationKey"
LODS = (0.94, 0.90)   # hard surface: LODs drop sub-pixel parts only (no collapse: it folded boxes into triangles)
SMOOTH_ANGLE = 50.0

ALU, CHROME = "Prop_Aluminium", "Prop_Chrome"
R, ZC = 0.00895, 0.0205
TOP0, RUN_Y, RUN_X, TOP1 = 1.296, 1.00, 1.00, 1.237
BEND = 0.085


def arc(cx, cy, r, a0, a1, n):
    return [(cx + r * math.cos(math.radians(a0 + (a1 - a0) * k / n)), cy + r * math.sin(math.radians(a0 + (a1 - a0) * k / n))) for k in range(n + 1)]


def strap(kit, x, y, along):
    """A one-hole strap over the tube on a clamp-back spacer; along = 'x' or 'y' (the tube's direction)."""
    w = 0.019
    pts = []
    for k in range(13):
        a = math.radians(180 * k / 12)
        pts.append((math.cos(a) * (R + 0.0012), math.sin(a) * (R + 0.0012)))
    if along == "y":
        path = [(x + px, y, ZC + pz) for px, pz in pts]
        foot = (x - R - 0.016, y, 0.0105)
        sc.box(kit, (0.024, w, 0.0016), foot, ALU, bevel=0.0004, segments=1, name="strap foot")
        sc.box(kit, (0.030, 0.026, ZC - R - 0.0005), (x, y, (ZC - R - 0.0005) / 2), ALU, bevel=0.0006, segments=1, name="clamp back")
        sc.cyl(kit, 0.004, 0.003, (foot[0], y, 0.0125), "z", CHROME, verts=12, bevel=0.0006, name="strap screw")
    else:
        path = [(x, y + px, ZC + pz) for px, pz in pts]
        foot = (x, y - R - 0.016, 0.0105)
        sc.box(kit, (w, 0.024, 0.0016), foot, ALU, bevel=0.0004, segments=1, name="strap foot")
        sc.box(kit, (0.026, 0.030, ZC - R - 0.0005), (x, y, (ZC - R - 0.0005) / 2), ALU, bevel=0.0006, segments=1, name="clamp back")
        sc.cyl(kit, 0.004, 0.003, (x, foot[1], 0.0125), "z", CHROME, verts=12, bevel=0.0006, name="strap screw")
    sc.tube(kit, path, 0.0009, ALU, verts=4, name="strap saddle")


def build(kit):
    sc.register()
    pts = [(0.0, TOP0)]
    pts += [(0.0, RUN_Y + BEND)]
    pts += [(BEND - BEND * math.cos(math.radians(a)), RUN_Y + BEND - BEND * math.sin(math.radians(a))) for a in range(10, 91, 10)]
    pts += [(RUN_X - BEND, RUN_Y)]
    pts += [(RUN_X - BEND + BEND * math.sin(math.radians(a)), RUN_Y + BEND - BEND * math.cos(math.radians(a))) for a in range(10, 91, 10)]
    pts += [(RUN_X, TOP1)]
    sc.tube(kit, [(x, y, ZC) for x, y in pts], R, ALU, verts=18, name="EMT run")
    # Couplings: none on a 1 m run; a compression connector at each end is on the two boxes' kits.
    strap(kit, 0.0, 1.17, "y")
    strap(kit, RUN_X / 2, RUN_Y, "x")
    strap(kit, RUN_X, 1.16, "y")
    kit.no_collider()
    kit.tag("wall_decor", "station")
    kit.meta["run"] = {"from": "Kit_KeySwitchPlate box bottom (y 1.296)", "to": "Kit_FireAnnunciator knock-out (y 1.237)", "length_along_x": RUN_X}
    sc.auto_lod_drops(kit)
    sc.lods(kit, 3.0, 8.0, 40.0, (2000, 700, 0))
    sc.station_meta(kit, "wall face at floor level under the key plate's box; runs along local +X", "")
