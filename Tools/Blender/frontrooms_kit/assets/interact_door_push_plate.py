"""Push plate, HW-DA1 (research/door_hardware 10_research.md section 9, 20_build.md): one per face of each leaf of
the start double door (FrontRoomsDoubleDoorKit), c. 1990 US commercial double-acting pair.

4 in x 16 in x 0.050 in (0.1016 x 0.4064 x 0.00127 m) US32D / BHMA 630 satin stainless, grain vertical, square
sheared corners, four 45-degree bevelled edges (1.0 x 1.0 mm, 0.27 mm land), six #6 oval-head Phillips screws
(corners 3/8 in from the long edges and 1/2 in from the ends, plus mid-height: the Rockwood No. 70 / Hager 30S
pattern). The San Diego 08710 spec (Feb 1992): "Push plates shall be 4-inch by 16-inch by 0.050 thick".
No maker marks (era lock: generic, unbranded).

Origin: the plate's back centre on the leaf face; front = Unity +Z. Mounted by FrontRoomsDoubleDoorKit with its
centre 45 in (1.143 m) above the floor and its long edge 1-1/2 in (38.1 mm) from the meeting edge.
Slots: Prop_PushPlate630 (the plate: unique 630 wear map, _TileSize = 0.1016 x 0.4064), Prop_Stainless630 (screw
heads), Prop_PlasticBlack (countersink lips, the Phillips recesses).
LOD0 hero (holds at 0.3 m); LOD1 18 tris (screws in the maps); LOD2 10 tris. Distances 1.5 / 6 / 40 m.
"""

import interact_door_plate_common as pc
import interact_lock_common as lc

NAME = "Kit_DoorPushPlate"
SMOOTH_ANGLE = 35.0
W, H = 0.1016, 0.4064
SX, SY = W / 2 - 0.009525, H / 2 - 0.0127
SCREWS = [(-SX, -SY), (SX, -SY), (-SX, 0.0), (SX, 0.0), (-SX, SY), (SX, SY)]
ANGLES = (17.0, 62.0, 38.0, 81.0, 5.0, 49.0)
SEGS = 16
LOD_DISTANCES = (1.5, 6.0, 40.0)
BUDGET = (2250, 40, 12)


def build(kit):
    plate = pc.plate_lod0(kit, W, H, pc.PLATE)
    for sx, sy in SCREWS:
        pc.seat(kit, plate, sx, sy, SEGS)
    lc.finish_part(plate, None, delete_below=0.00001)
    pc.planar_uv(plate, W, H)
    pc.lods(plate, "0")
    for (sx, sy), ang in zip(SCREWS, ANGLES):
        s = pc.phillips_screw(kit, sx, sy, pc.T, ang, SEGS, name="push plate screw")
        lc.finish_part(s, pc.head_wear(pc.T), delete_below=pc.T + 0.00001)
        pc.lods(s, "0")
    l1 = pc.plate_lod1(kit, W, H, pc.PLATE)
    lc.finish_part(l1, None, delete_below=0.00001)
    pc.planar_uv(l1, W, H)
    pc.lods(l1, "1")
    l2 = pc.plate_lod2(kit, W, H, pc.PLATE)
    lc.finish_part(l2, None, delete_below=0.00001)
    pc.planar_uv(l2, W, H)
    pc.lods(l2, "2")
    n = pc.count(kit, NAME)
    pc.meta(kit, "push_plate", W, H, SCREWS, LOD_DISTANCES, BUDGET, (round(n["1"] / n["0"], 4), round(n["2"] / n["0"], 4)))
    assert n["1"] <= BUDGET[1] and n["2"] <= BUDGET[2], n
    assert abs(n["0"] - BUDGET[0]) <= 0.15 * BUDGET[0], n
