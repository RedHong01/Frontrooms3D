"""Kit_TVWallBracket: a period steel TV wall shelf for the station set (30L S02,
§10.2; 61 §4.4 TV row; plan 70 P01).

Real reference: the 1980s-90s institutional TV wall mount (hospital rooms,
waiting rooms, classrooms): a wall plate lag-bolted to the wall, two side
cheeks with a pivot, a flat steel platform with a front lip tilted down, two
diagonal struts and a steel safety strap over the set. Black powder coat.

ORIGIN = THE WALL FACE AT FLOOR LEVEL (host convention), front +Z into the
room, heights baked in: the module places it at y 0.
- Wall plate 0.40 x 0.20 at y 2.20-2.40.
- Hinge (anchor ``set_mount``) at y 2.36, 0.05 m off the wall, pitched 20 deg
  front-down (meta ``setMountPitchDeg``): the set's back-bottom edge sits on it.
- Platform 0.56 x 0.45 x 0.012 from the hinge along the tilted plane, front lip.
- Lowest point of the bracket ~2.195 m (plan check C1-8: bracket bottom >= 2.15).
Budget 3,000 / 1,000 / 120 (30L S02). Slots Prop_SteelBlack, Prop_Chrome.
No collider (it hangs over 2.15 m); footprint = bounds.
"""

import math

import _station_common as sc

NAME = "Kit_TVWallBracket"
LODS = (0.94, 0.90)   # hard surface: LODs drop sub-pixel parts only (no collapse: it folded boxes into triangles)
SMOOTH_ANGLE = 40.0

STEEL, CHROME = "Prop_SteelBlack", "Prop_Chrome"
HINGE_Y, HINGE_Z, PITCH = 2.36, 0.05, 20.0
PW, PD, PT = 0.56, 0.45, 0.012
PLATE_W, PLATE_Y0, PLATE_Y1, PLATE_T = 0.40, 2.20, 2.40, 0.006


def tilt(u, v):
    """A point u along the platform (from the hinge toward the room) and v normal to it -> Unity (y, z)."""
    a = math.radians(PITCH)
    return HINGE_Y - u * math.sin(a) + v * math.cos(a), HINGE_Z + u * math.cos(a) + v * math.sin(a)


def build(kit):
    sc.register()
    # Wall plate with four lag bolts.
    sc.box(kit, (PLATE_W, PLATE_Y1 - PLATE_Y0, PLATE_T), (0, (PLATE_Y0 + PLATE_Y1) / 2, PLATE_T / 2), STEEL, bevel=0.0025, segments=2, name="wall plate")
    for x in (-0.16, 0.16):
        for y in (PLATE_Y0 + 0.03, PLATE_Y1 - 0.03):
            sc.cyl(kit, 0.0095, 0.006, (x, y, PLATE_T + 0.003), "z", CHROME, verts=6, bevel=0.0008, name="lag bolt hex")
            sc.cyl(kit, 0.012, 0.0015, (x, y, PLATE_T + 0.00075), "z", CHROME, verts=16, bevel=0.0, name="lag washer")
    # Side cheeks: wall plate to the hinge, x = +-0.245 (outside the 0.56 platform's 0.28? inside it by the pivot bolt).
    for sx in (-1, 1):
        x = sx * 0.272
        outline = [(PLATE_T, PLATE_Y0 + 0.02), (PLATE_T, PLATE_Y1 - 0.005), (HINGE_Z + 0.018, HINGE_Y + 0.012),
                   (HINGE_Z + 0.022, HINGE_Y - 0.010), (HINGE_Z - 0.01, HINGE_Y - 0.06), (PLATE_T, PLATE_Y0 + 0.02)]
        sc.extrude_zy(kit, outline[:-1], 0.005, x, STEEL, bevel=0.0012, segments=2, name="side cheek")
        # Pivot bolt through cheek and platform flange.
        sc.cyl(kit, 0.0075, 0.016, (x + sx * 0.004, HINGE_Y, HINGE_Z), "x", CHROME, verts=6, bevel=0.0006, name="pivot bolt")
        sc.cyl(kit, 0.010, 0.002, (x + sx * 0.0125, HINGE_Y, HINGE_Z), "x", CHROME, verts=18, bevel=0.0, name="pivot washer")
    # Platform: a tilted plate whose TOP passes through the hinge line, plus side flanges and a front lip.
    a = math.radians(PITCH)
    corners = []
    for (u, v) in ((0.0, 0.0), (PD, 0.0), (PD, -PT), (0.0, -PT)):
        y, z = tilt(u, v)
        corners.append((z, y))
    sc.extrude_zy(kit, corners, PW, 0.0, STEEL, bevel=0.002, segments=2, name="platform")
    for sx in (-1, 1):
        fl = []
        for (u, v) in ((0.0, 0.0), (PD, 0.0), (PD, -0.045), (0.06, -0.045), (0.0, -0.02)):
            y, z = tilt(u, v)
            fl.append((z, y))
        sc.extrude_zy(kit, fl, 0.004, sx * (PW / 2 - 0.002), STEEL, bevel=0.0008, segments=1, name="platform flange")
    lip = []
    for (u, v) in ((PD - 0.004, 0.0), (PD, 0.0), (PD, 0.025), (PD - 0.004, 0.025)):
        y, z = tilt(u, v)
        lip.append((z, y))
    sc.extrude_zy(kit, lip, PW - 0.02, 0.0, STEEL, bevel=0.0012, segments=2, name="front lip")
    # Rubber-free steel: two diagonal struts from the wall plate's bottom to the platform underside.
    for sx in (-1, 1):
        y0, z0 = PLATE_Y0 + 0.012, PLATE_T + 0.008
        y1, z1 = tilt(0.33, -PT - 0.002)
        sc.tube(kit, [(sx * 0.20, y0, z0), (sx * 0.20, y1, z1)], 0.0085, STEEL, verts=14, name="strut")
        for (yy, zz) in ((y0, z0), (y1, z1)):
            sc.cyl(kit, 0.012, 0.010, (sx * 0.20, yy, zz), "x", STEEL, verts=14, bevel=0.0006, name="strut eye")
    # Safety strap over the set (at 0.37 m along the platform, where the set's top is its full 0.50 height).
    u_s = 0.37
    pts = []
    for (x, v) in ((-0.276, 0.0), (-0.300, 0.02), (-0.314, 0.46), (-0.300, 0.505), (-0.25, 0.512), (0.25, 0.512),
                   (0.300, 0.505), (0.314, 0.46), (0.300, 0.02), (0.276, 0.0)):
        y, z = tilt(u_s, v)
        pts.append((x, y, z))
    sm = sc.smooth_path(pts, 3)
    sc.tube(kit, sm, 0.0042, STEEL, verts=6, name="safety strap")
    for sx in (-1, 1):
        y, z = tilt(u_s, -0.004)
        sc.cyl(kit, 0.007, 0.012, (sx * 0.276, y, z), "x", CHROME, verts=6, bevel=0.0005, name="strap bolt")

    kit.anchor("set_mount", sc.B(0.0, HINGE_Y, HINGE_Z))
    kit.meta["setMountPitchDeg"] = PITCH
    kit.no_collider()
    kit.tag("wall_decor", "station")
    sc.auto_lod_drops(kit)
    sc.lods(kit, 3.0, 8.0, 40.0, (3000, 1000, 120))
    sc.station_meta(kit, "wall face at floor level, heights baked in", "set_mount = the set's back-bottom edge, pitch 20 deg front-down")
