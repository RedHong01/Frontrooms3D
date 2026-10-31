"""Lamp head of the twin-head emergency unit (run_twinhead.py): a 4.5 in (PAR-36
class) 6 V sealed-beam lamp in a spun almond housing with a chrome retaining bezel.
PRODUCTION hero kit, Run! B (Kit_EmergencyTwinHead_Lamp), hand-built LOD0/1/2.

ORIGIN = the trunnion axis (the head's pivot in the yoke), at the housing centre. The
lens faces -Y in Blender (+Z in Unity): the rig aims the head by rotating this asset
and puts the spot light at the "lens" anchor. The lens is a domed disc with decal
UVs across its face (Run_SealedBeamLens_*: the fluted-lens normal map, the lit
filament / reflector emission). 48 segments at LOD0 (0.2 mm chord on the 115 mm rim).
Budget 2,500 / 1,000 / 150; lodDistances 2.5 / 8 / never.
Slots: Prop_SteelAlmond, Prop_Chrome, Run_SealedBeamLens.
"""

import _run_b_common as B

NAME = "Kit_EmergencyTwinHead_Lamp"
SMOOTH_ANGLE = 50.0
BUDGET = (2500, 1000, 150)

STEEL = "Prop_SteelAlmond"
CHROME = "Prop_Chrome"
LENS = "Run_SealedBeamLens"

R = 0.0575

HOUSING = [(0.0, -0.052), (0.020, -0.0505), (0.034, -0.0460), (0.045, -0.0380), (0.052, -0.0270),
           (0.0560, -0.0130), (R, -0.0020), (R, 0.0200), (0.0570, 0.0215), (R, 0.0230),
           (R, 0.0340), (0.0585, 0.0360), (0.0585, 0.0390)]
HOUSING_RINGS = {1: [0, 2, 4, 5, 6, 7, 9, 10, 12], 2: [0, 6, 12]}
BEZEL = [(0.0500, 0.0385), (0.0588, 0.0383), (0.0601, 0.0410), (0.0590, 0.0458), (0.0545, 0.0476), (0.0503, 0.0468)]
BEZEL_RINGS = {1: [0, 1, 2, 3, 4, 5], 2: [1, 2, 4]}
LENS_P = [(0.0512, 0.0418), (0.0500, 0.0442), (0.0450, 0.0458), (0.0350, 0.0473), (0.0220, 0.0483), (0.0, 0.0487)]
LENS_RINGS = {1: [0, 2, 3, 5], 2: [0, 5]}


def _level(kit, lv):
    hr = None if lv == 0 else HOUSING_RINGS[lv]
    br = None if lv == 0 else BEZEL_RINGS[lv]
    lr = None if lv == 0 else LENS_RINGS[lv]
    B.lathe(kit, HOUSING, (0, 0, 0), STEEL, lv=lv, rot=(90, 0, 0), name="housing", lo=48, rings=hr)
    B.lathe(kit, BEZEL, (0, 0, 0), CHROME, lv=lv, rot=(90, 0, 0), name="bezel", close_top=False, close_bottom=False, lo=48, rings=br)
    lens = B.lathe(kit, LENS_P, (0, 0, 0), LENS, lv=lv, rot=(90, 0, 0), name="lens", uv="decal", close_top=False, lo=48, rings=lr)
    lens["fr_decal_axes"] = "xy"   # local axes: the lathe disc lies in its own XY plane before the rotation is applied
    if lv < 2:
        for sx in (-1, 1):
            # The pins sit inside the yoke's knobs: only their ends show, so no bevel.
            kit.cylinder(0.0055, 0.010, (sx * (R + 0.003), 0, 0), STEEL, verts=12 if lv == 0 else 8, rot=(0, 90, 0), bevel=0.0, segments=0, name="trunnion pin")
        # Cord gland at the back, low; the cord loops down to the cabinet.
        kit.cylinder(0.0060, 0.012, (0, 0.050, -0.018), STEEL, verts=B.segs(0.006, lv, 12), rot=(90, 0, 0), bevel=0.0, segments=0, name="cord gland")


def build(kit):
    B.levels(kit, _level)
    kit.no_collider()
    kit.anchor("lens", (0, -0.049, 0))
    kit.tag("run", "run_b")
    B.lod_meta(kit, (2.5, 8.0, 0.0), BUDGET)
