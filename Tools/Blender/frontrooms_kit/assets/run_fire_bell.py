"""Vibrating fire-alarm bell, 6 in gong, wall-mounted (US, 1975-1992).
PRODUCTION kit for Run! B (Kit_FireBell), hand-built LOD0/1/2. It stays silent (EGRESS).

A 6 in (152 mm) red enamel gong (#A8231C, slot RunB_SprinklerRed #A3201A) on a round red
mounting plate over the vibrating mechanism, a chrome centre nut, the black striker
showing under the rim. ORIGIN = the wall point at the gong's centre line (plate back on
y = 0). FRONT = kit -Y (Unity +Z). Budget 1,800 / 700 / 120; lodDistances 2 / 6 / 25.
Slots: RunB_SprinklerRed (gong), RunB_FireRed (plate, mechanism), Prop_PlasticBlack, Prop_Chrome.
"""

import _run_b_common as B

NAME = "Kit_FireBell"
SMOOTH_ANGLE = 45.0
BUDGET = (1800, 700, 120)

GONG = "RunB_SprinklerRed"
RED = "RunB_FireRed"
BLACK = "Prop_PlasticBlack"
CHROME = "Prop_Chrome"

# Gong section (r, h), h out from the wall: outer surface from the front centre to the
# rim, then the inner surface back to the centre (5 mm wall, a rolled rim).
GONG_P = [(0.0, 0.074), (0.028, 0.071), (0.050, 0.063), (0.064, 0.053), (0.0725, 0.043), (0.0760, 0.034),
          (0.0762, 0.030), (0.0740, 0.029), (0.0705, 0.037), (0.0625, 0.047), (0.0490, 0.057), (0.0275, 0.065), (0.0, 0.068)]
GONG_RINGS = {1: [0, 2, 4, 5, 6, 7, 9, 11, 12], 2: [0, 3, 6, 9, 12]}
PLATE_P = [(0.0, 0.0), (0.056, 0.0), (0.058, 0.002), (0.058, 0.006), (0.055, 0.009), (0.040, 0.011), (0.0, 0.012)]
PLATE_RINGS = {1: [0, 1, 3, 4, 6], 2: [0, 1, 4, 6]}


def _level(kit, lv):
    B.lathe(kit, PLATE_P, (0, 0, 0), RED, lv=lv, rot=(90, 0, 0), name="mounting plate", lo=32, rings=[0, 1, 3, 4, 6] if lv == 0 else PLATE_RINGS[lv])
    if lv < 2:
        # The mechanism housing between the plate and the gong (seen past the rim).
        B.box(kit, (0.050, 0.020, 0.042), (0, -0.021, 0.0), RED, w=0.003, lv=lv, name="mechanism")
        # The post is hidden behind the gong: no bevel.
        kit.cylinder(0.008, 0.016, (0, -0.036, 0.0), RED, verts=8 if lv == 0 else 6, rot=(90, 0, 0), bevel=0.0, segments=0, name="gong post")
    # LOD0: the chord rule gives 44 segments for the 76 mm rim; the two inner-surface rings 8 and 10 are hidden
    # inside the bowl from every reachable view (it hangs at 2.3 m), so LOD0 keeps 11 of the 13 rings.
    B.lathe(kit, GONG_P, (0, 0, 0), GONG, lv=lv, rot=(90, 0, 0), name="gong", lo=44, rings=[0, 1, 2, 3, 4, 5, 6, 7, 9, 11, 12] if lv == 0 else GONG_RINGS[lv])
    if lv < 2:
        kit.cylinder(0.0085, 0.006, (0, -0.0765, 0.0), CHROME, verts=6, rot=(90, 0, 0), bevel=0.0008 if lv == 0 else 0.0, segments=1 if lv == 0 else 0, name="centre nut")
        # Striker: a black hammer head on its arm, just inside the rim at the bottom.
        kit.cylinder(0.0055, 0.012, (0, -0.040, -0.0655), BLACK, verts=12 if lv == 0 else 8, rot=(90, 0, 0), bevel=0.0008 if lv == 0 else 0.0, segments=1 if lv == 0 else 0, name="striker")
        B.box(kit, (0.006, 0.008, 0.046), (0, -0.032, -0.040), BLACK, w=0.001, lv=lv, name="striker arm")


def build(kit):
    B.levels(kit, _level)
    kit.no_collider()
    kit.anchor("centre", (0, -0.074, 0))
    kit.tag("wall_unit", "run", "run_b", "fire_alarm")
    B.lod_meta(kit, (2.0, 6.0, 25.0), BUDGET)
