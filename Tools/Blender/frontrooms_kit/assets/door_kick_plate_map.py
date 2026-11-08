"""Kit_DoorKickPlate_Map: HW-DA1's kick plate sized for the D1.6 map leaf (zone doors, the Map stage, 2026-11-06;
research/double_doors/30_build.md Part M): the Office free door (OF-F) and the pool doors (PL-F, PL-K) hang one on each face of
their single DA leaf, as the title pairs carry HW-DA1 (research/door_hardware 10_research.md section 9, 20_build.md).

10 in high x 0.94 m x 0.050 in = 0.254 x 0.94 x 0.00127 m: the map leaf is 0.99 m (Z 0.005-0.995); the plate stands 25 mm in from
each edge, as Kit_DoorLeaf_Steel_DA's own plates (Z 0.030-0.970), clear of the hinge stile's arc and the latch arris (San Diego
08710, Feb 1992: kick plates "0.050-gauge, beveled on 4 sides", "door width less 2 inches for single doors": 0.94 is 1 in a side,
the DA leaf's stile radius taking the other inch). US32D / BHMA 630 satin stainless, grain horizontal, four 45-degree bevelled
edges, square sheared corners, eight #6 oval-head Phillips screws in two rows (1/2 in from the long edges, 1 in from the ends,
0.2963 m pitch: ESTIMATE, as the pair's plate). No maker marks.

Origin: the plate's back centre on the leaf face; front = Unity +Z (the plate module's part frame). The row spawns it at the leaf's
kick_s / kick_p anchors (Y 0.145, Z 0.500), turned to face S / P, its back 0.1 mm off the face. Slots: Prop_KickPlate630 (the pair's
worn map, its left half: the plate's metre UVs crop it to 0.94 m, no stretch), Prop_Stainless630 (screws), Prop_PlasticBlack (the
dark seat rings). VARIANT Kit_DoorKickPlate_Map_B = Prop_KickPlate630B (the other half: face P never repeats face S). LOD0 hero;
LOD1 18 tris; LOD2 10 tris. Distances 2.5 / 10 / 50 m (the pair's plate).
"""

import interact_door_plate_common as pc
import interact_lock_common as lc

NAME = "Kit_DoorKickPlate_Map"
VARIANTS = {"Kit_DoorKickPlate_Map_B": {pc.KICK: pc.KICK_B}}
SMOOTH_ANGLE = 35.0
W, H = 0.94, 0.254
SX0, SY = W / 2 - 0.0254, H / 2 - 0.0127
PITCH = 2 * SX0 / 3
SCREWS = [(-SX0 + i * PITCH, sy) for sy in (-SY, SY) for i in range(4)]
ANGLES = (17.0, 64.0, 31.0, 82.0, 49.0, 6.0, 73.0, 38.0)
SEGS = 16
LOD_DISTANCES = (2.5, 10.0, 50.0)
BUDGET = (2400, 40, 12)


def build(kit):
    assert abs(PITCH - 0.29633) < 1e-4 and len(SCREWS) == 8, (PITCH, len(SCREWS))
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
    kit.tag("map_member")
    kit.meta["mapLeaf"] = {"anchor": "kick_s / kick_p (Y 0.145, Z 0.500)", "zRange": [0.030, 0.970], "yRange": [0.018, 0.272],
                           "rows": "OF-F, PL-F, PL-K (FrontRoomsInteractableKit.Door.cs)"}
    assert n["1"] <= BUDGET[1] and n["2"] <= BUDGET[2], n
    assert abs(n["0"] - BUDGET[0]) <= 0.25 * BUDGET[0], n
