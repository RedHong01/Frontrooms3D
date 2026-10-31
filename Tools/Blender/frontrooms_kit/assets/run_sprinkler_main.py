"""Fire-sprinkler main, 4 in (OD 0.10 m), painted red (#A3201A, smoothness .48), one 3 m
tile with a grooved coupling at its start. PRODUCTION kit for Run! B
(Kit_SprinklerMain_3m), hand-built LOD0/1/2. Tiles run end to end (the next tile's
coupling joins this tile's end); the ends are open (they die in the end walls).
ORIGIN = the pipe axis at the tile's start; the tile runs along +X (Blender), so Unity
-X: the dress yaws it 90 degrees to run along +Z. Budget 500 / 200 / 40;
lodDistances 3 / 9 / never. The coupling's two bolt pads take a 1-segment bevel (the
2-segment hero bevel would put the tile at 608 tris). Slots: RunB_SprinklerRed.
"""

import _run_b_common as B

NAME = "Kit_SprinklerMain_3m"
SMOOTH_ANGLE = 40.0
BUDGET = (500, 200, 40)

RED = "RunB_SprinklerRed"
R = 0.050
LENGTH = 3.0
COUPLING = [(0.0500, -0.0220), (0.0560, -0.0220), (0.0592, -0.0150), (0.0592, 0.0150), (0.0560, 0.0220), (0.0500, 0.0220)]


def _level(kit, lv):
    n = {0: 36, 1: 18, 2: 8}[lv]
    kit.tube([(0, 0, 0), (LENGTH, 0, 0)], R, RED, verts=n, name="pipe", caps=False)
    prof = [COUPLING[i] for i in (0, 2, 3, 5)] if lv == 0 else ([COUPLING[0], COUPLING[2], COUPLING[3], COUPLING[5]] if lv == 1 else [COUPLING[0], COUPLING[5]])
    if lv == 2:
        prof = [(0.0560, -0.020), (0.0560, 0.020)]
    kit.lathe(prof, (0.0, 0, 0), RED, verts={0: 40, 1: 20, 2: 8}[lv], rot=(0, 90, 0), name="coupling", close_top=False, close_bottom=False)
    if lv < 2:
        for sz in (-1, 1):
            kit.box((0.026, 0.022, 0.016), (0.0, 0, sz * 0.064), RED, bevel=0.0015 if lv == 0 else 0.0, segments=1 if lv == 0 else 0, name="bolt pad")


def build(kit):
    B.levels(kit, _level)
    kit.no_collider()
    kit.anchor("end", (LENGTH, 0, 0))
    kit.tag("ceiling", "run", "run_b", "sprinkler")
    B.lod_meta(kit, (3.0, 9.0, 0.0), BUDGET)
