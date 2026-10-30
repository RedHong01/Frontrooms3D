"""Kit_FireAnnunciator: a remote fire-alarm annunciator on the riser's west face (30L S12, §4.3, §10.2 S12;
03_palette.md: a beige cabinet with a beige door; plan 70 P04).

Real reference: a 1980s surface-mount remote annunciator (lamp-per-zone, steel cabinet, hinged steel door with a
key lock): here as 30L §10.2 rebuilt it: a beige enamel cabinet, a narrow beige door frame, NO glazing, nine round
frosted lenses Ø 14 mm (8 zones + TROUBLE) beside narrow engraved label strips #C9C3B2 (blank: 平面视觉's words),
and the POWER pilot: an incandescent green glass jewel (emissive only, steady, no realtime light). It stays lit
after the take and never dips in W1. Matte throughout. Bottom knock-out with a set-screw connector where the EMT
from the key plate enters (from below, NOTE_1022_photos.md). No brand.

ORIGIN = THE WALL FACE AT FLOOR LEVEL, front +Z, heights baked in (body y 1.25-1.75, centre 1.50).
0.38 x 0.50 x 0.10. Budget 6,000 / 2,000 / 200. Slots Prop_SteelBeige, Prop_AnnunciatorLabel, Prop_JewelGreen,
Prop_Chrome. No collider (module prop noCollider); footprint = the body.
"""

import math

import _station_common as sc

NAME = "Kit_FireAnnunciator"
LODS = (0.94, 0.90)   # hard surface: LODs drop sub-pixel parts only (no collapse: it folded boxes into triangles)
SMOOTH_ANGLE = 35.0

BEIGE, LABEL, JEWEL, CHROME = sc.STEEL_BEIGE, sc.ANN_LABEL, sc.JEWEL_GREEN, "Prop_Chrome"
W, H, D = 0.38, 0.50, 0.10
Y0 = 1.25
YC = Y0 + H / 2
BODY_D = 0.085
DOOR_T = 0.012
FRAME = 0.014


def build(kit):
    sc.register()
    # Back box (deep-drawn steel, rounded edges).
    sc.box(kit, (W, H, BODY_D), (0, YC, BODY_D / 2), BEIGE, bevel=0.005, segments=3, name="back box")
    # Door: a flat panel inside a narrow raised frame (both beige), no glazing.
    zd = BODY_D + DOOR_T / 2
    sc.extrude_xy(kit, sc.rounded_rect(W - 0.006, H - 0.006, 0.006, 4, 0, YC), DOOR_T, zd, BEIGE, bevel=0.0018, segments=3, name="door")
    outer = sc.rounded_rect(W - 0.010, H - 0.010, 0.005, 3, 0, YC)
    zf = BODY_D + DOOR_T
    kit.frame((W - 0.010, H - 0.010), (W - 0.010 - 2 * FRAME, H - 0.010 - 2 * FRAME), 0.003, sc.B(0, YC, zf + 0.0015), BEIGE,
              bevel=0.0009, segments=2, name="door frame")
    zt = zf + 0.003
    # Piano hinge down the left (as seen from the room: Unity -x), chrome knuckles.
    for k in range(10):
        y = Y0 + 0.02 + k * (H - 0.04) / 10 + 0.0225
        sc.cyl(kit, 0.0045, (H - 0.04) / 10 - 0.002, (-W / 2 + 0.002, y, BODY_D + 0.003), "y", CHROME, verts=12, bevel=0.0004, name="hinge knuckle")
    # Key cylinder lock on the right with its bezel and keyway.
    sc.cyl(kit, 0.011, 0.004, (W / 2 - 0.035, YC + 0.06, zt + 0.002), "z", CHROME, verts=28, bevel=0.0012, segments=2, name="lock bezel")
    sc.cyl(kit, 0.0075, 0.003, (W / 2 - 0.035, YC + 0.06, zt + 0.005), "z", CHROME, verts=24, bevel=0.0008, name="lock plug")
    # Nine frosted lenses (8 zones + TROUBLE) with label strips beside each: a column on the door's left half.
    pitch = 0.034
    top = Y0 + H - 0.072
    for i in range(9):
        y = top - i * pitch
        x = -W / 2 + 0.070
        sc.cyl(kit, 0.0105, 0.003, (x, y, zt + 0.0015), "z", BEIGE, verts=18, bevel=0.0006, segments=1, name="lens bezel")
        sc.lathe_y  # noqa
        dome = kit.lathe([(0.0070, 0.0), (0.0068, 0.0015), (0.0055, 0.0032), (0.0032, 0.0042), (0.0, 0.0046)],
                         (0, 0, 0), LABEL, verts=16, name="frosted lens", close_bottom=False)
        dome.rotation_mode = "XYZ"
        dome.rotation_euler = (math.radians(90), 0, 0)
        dome.location = sc.B(x, y, zt + 0.003)
        sc.box(kit, (0.150, 0.016, 0.0012), (x + 0.0275 + 0.075, y, zt + 0.0006), LABEL, bevel=0.0003, segments=1, name="label strip")
    # Lamp-test / silence / reset (period: key-switch functions behind the door: shown as three recessed screws).
    for k in range(3):
        sc.cyl(kit, 0.004, 0.0012, (W / 2 - 0.10 + k * 0.03, Y0 + 0.07, zt + 0.0006), "z", CHROME, verts=12, bevel=0.0, name="cover screw")
    # POWER: chrome bezel ring and a faceted green glass jewel, lower right, with its label strip.
    jx, jy = W / 2 - 0.060, Y0 + 0.115
    sc.cyl(kit, 0.0105, 0.004, (jx, jy, zt + 0.002), "z", CHROME, verts=32, bevel=0.0012, segments=2, name="jewel bezel")
    jewel = kit.lathe([(0.0062, 0.0), (0.0062, 0.0012), (0.0052, 0.0034), (0.0030, 0.0050), (0.0, 0.0056)], (0, 0, 0), JEWEL,
                      verts=10, name="power jewel", close_bottom=False)
    jewel.rotation_mode = "XYZ"
    jewel.rotation_euler = (math.radians(90), 0, 0)
    jewel.location = sc.B(jx, jy, zt + 0.004)
    sc.box(kit, (0.060, 0.016, 0.0012), (jx - 0.050, jy, zt + 0.0006), LABEL, bevel=0.0003, segments=1, name="power label strip")
    # Bottom knock-out with the EMT set-screw connector (from the key plate's conduit, below).
    sc.cyl(kit, 0.0125, 0.006, (0, Y0 - 0.003, 0.0205), "y", CHROME, verts=6, bevel=0.0006, name="connector nut")
    sc.cyl(kit, 0.0105, 0.014, (0, Y0 - 0.013, 0.0205), "y", CHROME, verts=16, bevel=0.0006, name="connector body")
    sc.cyl(kit, 0.003, 0.006, (0.012, Y0 - 0.013, 0.0205), "x", CHROME, verts=8, bevel=0.0, name="set screw")
    # Mounting: four screw heads on the back box's flanges are hidden; the door seam reads.
    kit.anchor("power", sc.B(jx, jy, zt + 0.0096))
    kit.anchor("knockout", sc.B(0, Y0 - 0.02, 0.0205))
    kit.collider(sc.B(0, YC, D / 2), sc.BS(W, H, D))
    kit.tag("wall_decor", "station")
    kit.meta["power"] = {"slot": JEWEL, "lit": "steady, emissive only, no realtime light; stays lit after the take; never dips in W1"}
    sc.auto_lod_drops(kit, keep=('jewel',))
    sc.lods(kit, 3.0, 8.0, 40.0, (6000, 2000, 200))
    sc.station_meta(kit, "wall face at floor level, heights baked in", "lenses never lit; labels blank (平面视觉's words)")
