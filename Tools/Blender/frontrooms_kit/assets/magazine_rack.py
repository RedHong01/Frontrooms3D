"""Kit_MagazineRack: a welded chrome wire literature rack on the waiting room wall, 2 columns x 3 tiers, four
magazines in it (游戏视觉, bs4-hero-kits, builder A; research/station/72_hero_kits.md §2.3). New in
Station_Waiting_3x2 (N wall, x 2.10, yaw 180; absent in BS4).

Period: 30_look S29 (a wall rack, blank-title covers, Prop_Chrome); research/level_space/62 LS98 (a wall literature
rack 0.61 x 0.10 x 0.91 under this name; this is its station-height cut, 0.66 m tall over the crash rail; a floor
stand would be Kit_MagazineRack_Floor); 02_refs §3.8 (a rack belongs to the waiting variant); typology [memory]:
welded chrome-plated steel-wire contract racks.

Unity metres; ORIGIN = THE WALL FACE PLANE AT FLOOR LEVEL, centred; front +Z; heights baked in.
* 24 x 26 x 4.25 in (0.61 x 0.66 x 0.108): wire 0.98-1.64 m over the crash rail (0.78-0.93), pockets 8.5 in apart
  (0.216), floor 2.75 in deep, front lip 3.5 in (0.089);
* frame: 1/4 in rod (6.35): a rounded rectangle (top and bottom rails, side rods) and a centre rod in the back
  plane, a J per tier and per side / centre (floor forward, r 8 bend, lip up) bent at 30-degree steps;
* lips: 3/16 in (4.76) wire, two per tier across the full width (+0.045 / +0.085);
* back grid: 9 ga (3.76) verticals at 25 mm and two horizontals; pocket floors: two 9 ga wires per tier, 35 and 70
  mm off the grid (the back 35 mm stays open, so a lower magazine's top passes behind the tray above);
* fixings: two 20 x 30 x 2 mm tabs on the top rail with chrome pan-head screws (D8; the screw heads cover the
  keyholes); no bumpers (72 second pass);
* magazines, 4 in 6 pockets (top-left, middle-right, bottom-left, bottom-right as seen from the room): 8.5 x 11 in,
  4-6 mm thick, standing on the trays and leaning back (about 18 degrees: bottom against the lip wires, top on the
  grid); the bottom-right one turned 3 degrees in its plane, the top-left one's top fore-edge corner curled 3 mm; cover faces
  on Prop_MagazineCover (one 2 x 2 atlas cell each, the whole cell over the cover), page edges on Prop_Paper.

Slots: Prop_Chrome, Prop_MagazineCover, Prop_Paper (3 draws). LOD1: wires 4-6 sides, every second grid wire, the
magazines as single quads; LOD2: the frame as 6 thin boxes and 4 magazine quads. Budget 3,000 / 1,000 / 150 (cap
3,500); lodDistances [3, 8, 30]; no collider (wall decor above the rail); anchors pocket_1..6 (each pocket floor's
centre, row-major from the top-left as seen from the room).
"""

import math

from mathutils import Matrix, Vector

import _station_waiting_common as w

NAME = "Kit_MagazineRack"
SMOOTH_ANGLE = w.SMOOTH

CHROME, COVER, PAPER = w.CHROME, w.COVER, w.PAPER
Y0, Y1 = 0.98, 1.64
R_ROD, R_LIP, R_WIRE = 0.003175, 0.00238, 0.00188
X_SIDE = 0.305 - R_ROD                    # 0.3018
Z_ROD = 0.002 + R_ROD                     # rods' back 2 mm off the wall (the tabs)
Z_GRID = Z_ROD + R_ROD + R_WIRE           # grid in front of the frame rods
Z_LIP_J = 0.100                           # the J's lip rod
Z_LIP_W = Z_LIP_J + R_ROD + R_LIP         # lip wires welded on its front: front of the rack 0.108
TIER_Y = [Y0 + R_ROD + k * 0.216 for k in range(3)]     # J floor rod centres, bottom to top
LIP_H = 0.089
TRAY_Z = (Z_GRID + 0.035, Z_GRID + 0.070)
MAG_W, MAG_H = 0.216, 0.279
POCKET_X = (0.151, -0.151)                # room-left (+x), room-right (-x)


def j_path(x, yf, lv):
    """A tier's J: from the grid forward along the floor, an r 8 bend up, the lip to yf + LIP_H."""
    rb = 0.008
    pts2 = w.fillet_path((Z_GRID, yf), (Z_LIP_J, yf), (Z_LIP_J, yf + LIP_H), rb, steps=3 if lv == 0 else 1)
    return [(x, y, z) for (z, y) in pts2]


def frame_path(lv):
    r = 0.012
    steps = 3 if lv == 0 else 1
    ya, yb = Y0 + R_ROD, Y1 - R_ROD
    pts = []
    for (cx, cy, a0) in ((X_SIDE - r, yb - r, 0.0), (-X_SIDE + r, yb - r, 90.0), (-X_SIDE + r, ya + r, 180.0),
                         (X_SIDE - r, ya + r, 270.0)):
        for k in range(steps + 1):
            a = math.radians(a0 + 90.0 * k / steps)
            pts.append((cx + r * math.cos(a), cy + r * math.sin(a), Z_ROD))
    return pts


def magazine(kit, idx, cell, pocket_x, yf, thick, turn=0.0, curl=0.0, lv=0):
    """One magazine standing on its tray and leaning back onto the grid. Local frame: origin at the bottom-front
    edge centre, a = across (room-left = +x), b = up the cover, n = out of the cover."""
    y0 = yf + R_ROD + 2 * R_WIRE + 0.0002          # on the tray wires
    z0 = Z_LIP_W - R_LIP - 0.0003                  # bottom-front edge against the lip wires
    # lean: the top-back edge on the grid's front
    target = z0 - (Z_GRID + R_WIRE)
    th = math.asin(max(0.0, min(0.9, (target - thick * 0.95) / MAG_H)))
    for _ in range(6):
        th = math.asin(max(0.0, min(0.9, (target - thick * math.cos(th)) / MAG_H)))
    b = Vector((0.0, math.cos(th), -math.sin(th)))
    n = Vector((0.0, math.sin(th), math.cos(th)))
    a = Vector((1.0, 0.0, 0.0))
    if turn:
        rot = Matrix.Rotation(math.radians(turn), 3, n)
        a, b = rot @ a, rot @ b
    o = Vector((pocket_x, y0, z0))
    u0, v0, u1, v1 = cell

    def P(sa, sb, sn):
        return tuple(o + a * sa + b * sb + n * sn)
    hw = MAG_W / 2
    if lv >= 1:
        verts = [P(hw, 0, 0), P(-hw, 0, 0), P(-hw, MAG_H, 0), P(hw, MAG_H, 0)]
        uvs = [(u0, v0), (u1, v0), (u1, v1), (u0, v1)]
        w.mesh_part(kit, "magazine %d" % idx, COVER, verts, [(0, 1, 2, 3)], uvs=uvs, face_dirs=[tuple(n)])
        return
    # cover: front face as a 4 x 6 grid (the curl lifts the top fore-edge corner), the back face, the spine (+x side)
    NA, NB = 4, 6
    verts, faces, uvs = [], [], []

    def lift(sa, sb):
        if curl <= 0:
            return 0.0
        da = (-sa - (hw - 0.04)) / 0.04            # the fore-edge (room-right) corner; the spine cannot curl
        db = (sb - (MAG_H - 0.05)) / 0.05
        if da <= 0 or db <= 0:
            return 0.0
        return curl * min(1.0, da) ** 2 * min(1.0, db) ** 2
    grid = []
    for j in range(NB + 1):
        row = []
        for i in range(NA + 1):
            sa = hw - MAG_W * i / NA          # i = 0 at room-left (+x) = the spine edge
            sb = MAG_H * j / NB
            verts.append(P(sa, sb, thick + lift(sa, sb)))
            row.append(len(verts) - 1)
        grid.append(row)
    fuv = []
    for j in range(NB):
        for i in range(NA):
            q = (grid[j][i], grid[j][i + 1], grid[j + 1][i + 1], grid[j + 1][i])
            faces.append(q)
            for (ii, jj) in ((i, j), (i + 1, j), (i + 1, j + 1), (i, j + 1)):
                # u runs room-left -> room-right as seen from the front (the atlas reads left to right)
                fuv.append((u0 + (u1 - u0) * ii / NA, v0 + (v1 - v0) * jj / NB))
    # back face (same cell, mirrored)
    k = len(verts)
    verts += [P(hw, 0, 0), P(-hw, 0, 0), P(-hw, MAG_H, 0), P(hw, MAG_H, 0)]
    faces.append((k, k + 3, k + 2, k + 1))
    fuv += [(u0, v0), (u0, v1), (u1, v1), (u1, v0)]
    # spine (room-left edge): a thin strip of the cell's left edge
    k = len(verts)
    verts += [P(hw, 0, 0), P(hw, 0, thick), P(hw, MAG_H, thick), P(hw, MAG_H, 0)]
    faces.append((k, k + 1, k + 2, k + 3))
    du = (u1 - u0) * 0.02
    fuv += [(u0, v0), (u0 + du, v0), (u0 + du, v1), (u0, v1)]
    dirs = [tuple(n)] * (NA * NB) + [tuple(-n), tuple(a)]
    w.mesh_part(kit, "magazine %d cover" % idx, COVER, verts, faces, uvs=fuv, face_dirs=dirs)
    # page edges: top, bottom, fore-edge (room-right)
    pv = [P(-hw, 0, 0), P(-hw, 0, thick), P(-hw, MAG_H, thick + lift(-hw, MAG_H)), P(-hw, MAG_H, 0),
          P(hw, MAG_H, 0), P(hw, MAG_H, thick + lift(hw, MAG_H)), P(hw, 0, thick), P(hw, 0, 0)]
    pf = [(0, 1, 2, 3), (3, 2, 5, 4), (7, 6, 1, 0)]
    w.mesh_part(kit, "magazine %d pages" % idx, PAPER, pv, pf, face_dirs=[tuple(-a), tuple(b), tuple(-b)])


def build_level(kit, lv):
    if lv == 2:
        ya, yb = Y0 + R_ROD, Y1 - R_ROD
        t = 0.006
        for (c, s) in (((0, yb, Z_ROD), (0.61, t, t)), ((0, ya, Z_ROD), (0.61, t, t)), ((X_SIDE, (ya + yb) / 2, Z_ROD), (t, yb - ya, t)),
                       ((-X_SIDE, (ya + yb) / 2, Z_ROD), (t, yb - ya, t)), ((0, (ya + yb) / 2, Z_ROD), (t, yb - ya, t))):
            w.box(kit, s, c, CHROME, bevel=0.0, segments=1, name="frame lod2")
        w.box(kit, (0.61, 0.6, 0.004), (0, (ya + yb) / 2, Z_GRID), CHROME, bevel=0.0, segments=1, name="grid lod2")
    else:
        rod_sides = 12 if lv == 0 else 6
        lip_sides = 10 if lv == 0 else 4
        wire_sides = 8 if lv == 0 else 4
        w.tube(kit, "frame", CHROME, frame_path(lv), R_ROD, rod_sides, caps=False, closed=True)
        w.tube(kit, "centre rod", CHROME, [(0.0, Y0 + R_ROD, Z_ROD), (0.0, Y1 - R_ROD, Z_ROD)], R_ROD, rod_sides, caps=False)
        # back grid
        xs = [0.0125 + 0.025 * k for k in range(12)]
        xs = sorted([-x for x in xs] + xs)
        if lv == 1:
            xs = xs[::2]
        for x in xs:
            w.tube(kit, "grid wire", CHROME, [(x, Y0 + R_ROD, Z_GRID), (x, Y1 - R_ROD, Z_GRID)], R_WIRE, wire_sides, caps=False)
        for yy in (TIER_Y[1] + 0.06, TIER_Y[2] + 0.06):
            w.tube(kit, "grid rail", CHROME, [(-X_SIDE, yy, Z_GRID), (X_SIDE, yy, Z_GRID)], R_WIRE, wire_sides, caps=False)
        # tiers: a J per side and centre, two tray wires, two lip wires
        for yf in TIER_Y:
            for x in (X_SIDE, 0.0, -X_SIDE):
                w.tube(kit, "j rod", CHROME, j_path(x, yf, lv), R_ROD, rod_sides, caps="fan" if lv == 0 else "ngon")
            for tz in TRAY_Z:
                w.tube(kit, "tray wire", CHROME, [(-X_SIDE, yf + R_ROD + R_WIRE, tz), (X_SIDE, yf + R_ROD + R_WIRE, tz)],
                       R_WIRE, wire_sides, caps=False)
            for dy in (0.045, LIP_H - 0.004):
                w.tube(kit, "lip wire", CHROME, [(-0.3045, yf + dy, Z_LIP_W), (0.3045, yf + dy, Z_LIP_W)], R_LIP, lip_sides,
                       caps="fan" if lv == 0 else "ngon")
        if lv == 0:
            for tx in (-0.20, 0.20):
                w.box(kit, (0.020, 0.030, 0.002), (tx, Y1 - R_ROD + 0.007, 0.001), CHROME, bevel=0.0006, segments=1, name="tab")
                w.dome_head(kit, "tab screw", CHROME, (tx, Y1 - R_ROD + 0.013, 0.002), (0, 0, 1), 0.008, 0.0028, sides=10)
    # magazines (cells: 0 top-left, 1 top-right, 2 bottom-left, 3 bottom-right of the atlas)
    cells = [w.kitlib.Kit.atlas_cell(i, 2, 2) for i in range(4)]
    if lv == 2:
        for idx, (px, tier, cell) in enumerate(((POCKET_X[0], 2, 0), (POCKET_X[1], 1, 1), (POCKET_X[0], 0, 2), (POCKET_X[1], 0, 3))):
            magazine(kit, idx, cells[cell], px, TIER_Y[tier], 0.005, lv=2)
        return
    magazine(kit, 0, cells[0], POCKET_X[0], TIER_Y[2], 0.0048, curl=0.003 if lv == 0 else 0.0, lv=lv)
    magazine(kit, 1, cells[1], POCKET_X[1], TIER_Y[1], 0.0058, lv=lv)
    magazine(kit, 2, cells[2], POCKET_X[0], TIER_Y[0], 0.0042, lv=lv)
    magazine(kit, 3, cells[3], POCKET_X[1] + 0.006, TIER_Y[0], 0.0052, turn=3.0, lv=lv)


def build(kit):
    w.register()
    for lv in (0, 1, 2):
        with w.Level(kit, str(lv)):
            build_level(kit, lv)
    w.tris_report(kit, "pre-finish")
    w.finish_parts(kit, SMOOTH_ANGLE)
    kit.no_collider()
    k = 1
    for tier in (2, 1, 0):
        for px in POCKET_X:
            kit.anchor("pocket_%d" % k, w.B(px, TIER_Y[tier] + R_ROD + 2 * R_WIRE, (Z_GRID + Z_LIP_J) / 2))
            k += 1
    kit.tag("wall_decor", "station", "paper")
    w.sc.lods(kit, 3.0, 8.0, 30.0, (3000, 1000, 150))
    w.station_block(kit, "wall face plane at floor level, centred", "front +Z; wire 0.98-1.64 m, top magazines to about 1.69 m")
