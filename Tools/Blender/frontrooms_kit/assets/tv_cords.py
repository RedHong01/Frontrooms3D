"""Kit_TVCords: the station set's power cord and RG-59 coax down the north wall (30L §10.2 "power cord and RG-59 coax run
down the wall to a duplex outlet and an F-jack plate", §10.3 S37; plan 70 P25).

ORIGIN = THE WALL FACE AT FLOOR LEVEL at module (9.55, 11.92), yaw 180, so local +X runs toward world -X: the set's
back is at local x ~+0.12..+0.20, the duplex outlet (P26, world x 9.60) at local x -0.05 and the F jack (P27, world
x 9.75) at local x -0.20, both plate centres at y 0.30. The cords leave the back of the set on its bracket, drop to the
wall under the wall plate (2.18), run down the wall loosely, sag over the 6 in crash rail (0.78-0.93, 28 mm proud) and
end in a moulded plug in the outlet's upper receptacle and an F connector on the jack. Budget ~400 / 150 / 0 (the
plugs add their own few hundred). Slots Prop_PlasticBlack, Prop_Chrome. No collider.
"""

import _station_common as sc

NAME = "Kit_TVCords"
LODS = (0.94, 0.90)   # hard surface: LODs drop sub-pixel parts only (no collapse: it folded boxes into triangles)
SMOOTH_ANGLE = 60.0

RAIL_TOP, RAIL_PROUD = 0.93, 0.028


def build(kit):
    sc.register()
    power = [(0.15, 2.42, 0.14), (0.14, 2.33, 0.11), (0.12, 2.19, 0.012), (0.10, 1.80, 0.007), (0.06, 1.35, 0.006),
             (0.00, RAIL_TOP + 0.02, RAIL_PROUD + 0.006), (-0.025, RAIL_TOP - 0.03, RAIL_PROUD + 0.009), (-0.04, 0.70, 0.012),
             (-0.05, 0.42, 0.016), (-0.05, 0.345, 0.024), (-0.05, 0.322, 0.026)]
    sc.tube(kit, sc.smooth_path(power, 5), 0.0034, "Prop_PlasticBlack", verts=10, name="power cord")
    # Moulded two-blade plug in the upper receptacle (outlet face 7 mm proud, face_top at +0.019).
    sc.box(kit, (0.026, 0.034, 0.020), (-0.05, 0.319, 0.007 + 0.010), "Prop_PlasticBlack", bevel=0.004, segments=2, name="plug body")
    sc.box(kit, (0.012, 0.010, 0.010), (-0.05, 0.336, 0.020), "Prop_PlasticBlack", bevel=0.002, segments=1, name="plug strain relief")
    coax = [(0.19, 2.40, 0.12), (0.185, 2.30, 0.08), (0.17, 2.185, 0.011), (0.15, 1.75, 0.006), (0.09, 1.25, 0.006),
            (0.02, RAIL_TOP + 0.015, RAIL_PROUD + 0.005), (-0.02, RAIL_TOP - 0.04, RAIL_PROUD + 0.008), (-0.10, 0.66, 0.011),
            (-0.18, 0.42, 0.018), (-0.20, 0.345, 0.030), (-0.20, 0.316, 0.034)]
    sc.tube(kit, sc.smooth_path(coax, 5), 0.0030, "Prop_PlasticBlack", verts=10, name="RG-59 coax")
    # F connector (crimp sleeve + 7/16 nut) on the jack's barrel (jack front 15.5 mm proud).
    sc.cyl(kit, 0.0062, 0.010, (-0.20, 0.30, 0.0255), "z", "Prop_Chrome", verts=6, bevel=0.0005, name="F nut")
    sc.cyl(kit, 0.0050, 0.012, (-0.20, 0.30, 0.0365), "z", "Prop_Chrome", verts=18, bevel=0.0005, name="F crimp sleeve")
    kit.no_collider()
    kit.tag("wall_decor", "station")
    sc.auto_lod_drops(kit)
    sc.lods(kit, 4.0, 10.0, 40.0, (400, 150, 0))
    sc.station_meta(kit, "wall face at floor level at module (9.55, 11.92), yaw 180", "")
