"""Kick plate, HW-DA1 (research/door_hardware 10_research.md section 9, 20_build.md): one per face of each leaf of
the start double door (FrontRoomsDoubleDoorKit), c. 1990 US commercial double-acting pair.

10 in high x (leaf width - 1-1/2 in) x 0.050 in = 0.254 x 1.1574 x 0.00127 m for the kit's 1.1955 m leaf, US32D /
BHMA 630 satin stainless, grain horizontal, four 45-degree bevelled edges, square sheared corners, ten #6 oval-head
Phillips screws in two rows (1/2 in from the long edges, 1 in from the ends, 0.2766 m pitch: ESTIMATE, catalogues
do not fix the pitch). San Diego 08710 (Feb 1992): kick plates "0.050-gauge, beveled on 4 sides", "door width less
1-1/2 inches for pairs of doors". No maker marks.

Origin: the plate's back centre on the leaf face; front = Unity +Z. Mounted centred on the leaf, its bottom 3 mm
over the leaf's bottom (21 mm above the floor), 19.05 mm in from each edge (clear of the DA hinge barrels).
Slots: Prop_KickPlate630 (A: the left half of the shared wear map, _TileSize = 2 x 1.1574 x 0.254),
Prop_Stainless630, Prop_PlasticBlack. VARIANT Kit_DoorKickPlate_B = Prop_KickPlate630B (the right half: a second
worn plate, so the two leaves of a pair never repeat). LOD0 hero; LOD1 18 tris; LOD2 10 tris. Distances 2.5 / 10 / 50 m.
"""

import interact_door_plate_common as pc
import interact_lock_common as lc

NAME = "Kit_DoorKickPlate"
VARIANTS = {"Kit_DoorKickPlate_B": {pc.KICK: pc.KICK_B}}
SMOOTH_ANGLE = 35.0
LEAF = (2.4 - 3 * 0.003) * 0.5        # FrontRoomsDoubleDoorKit.LeafWidth
W, H = LEAF - 0.0381, 0.254
SX0, SY = W / 2 - 0.0254, H / 2 - 0.0127
PITCH = 2 * SX0 / 4
SCREWS = [(-SX0 + i * PITCH, sy) for sy in (-SY, SY) for i in range(5)]
ANGLES = (23.0, 71.0, 8.0, 55.0, 34.0, 88.0, 12.0, 46.0, 67.0, 29.0)
SEGS = 16
LOD_DISTANCES = (2.5, 10.0, 50.0)
BUDGET = (3000, 40, 12)


def build(kit):
    assert abs(W - 1.1574) < 1e-4 and abs(PITCH - 0.27665) < 1e-4, (W, PITCH)
    plate = pc.plate_lod0(kit, W, H, pc.KICK)
    for sx, sy in SCREWS:
        pc.seat(kit, plate, sx, sy, SEGS)
    lc.finish_part(plate, None, delete_below=0.00001)
    pc.planar_uv(plate, W, H)
    pc.lods(plate, "0")
    for (sx, sy), ang in zip(SCREWS, ANGLES):
        s = pc.phillips_screw(kit, sx, sy, pc.T, ang, SEGS, name="kick plate screw")
        lc.finish_part(s, pc.head_wear(pc.T), delete_below=pc.T + 0.00001)
        pc.lods(s, "0")
    l1 = pc.plate_lod1(kit, W, H, pc.KICK)
    lc.finish_part(l1, None, delete_below=0.00001)
    pc.planar_uv(l1, W, H)
    pc.lods(l1, "1")
    l2 = pc.plate_lod2(kit, W, H, pc.KICK)
    lc.finish_part(l2, None, delete_below=0.00001)
    pc.planar_uv(l2, W, H)
    pc.lods(l2, "2")
    n = pc.count(kit, NAME)
    pc.meta(kit, "kick_plate", W, H, SCREWS, LOD_DISTANCES, BUDGET, (round(n["1"] / n["0"], 4), round(n["2"] / n["0"], 4)))
    assert n["1"] <= BUDGET[1] and n["2"] <= BUDGET[2], n
    assert abs(n["0"] - BUDGET[0]) <= 0.15 * BUDGET[0], n
