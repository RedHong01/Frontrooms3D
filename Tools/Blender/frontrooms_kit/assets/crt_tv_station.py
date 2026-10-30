"""Kit_CRTTV_Station: the station set, a hero pass of Kit_CRTTV under a station-only name (plan 70 P02; 30L S01,
§10.3 S01; plan check C2-4: Kit_CRTTV and every pile Screen stay as they are).

Same cabinet, footprint and ``screen`` anchor as Kit_CRTTV (bounds inside x +-0.31, y 0-0.50, z -0.241..0.2475; the
module's ValidateStation screen rules key on the name containing CRTTV). Changes for the room's hero:
- beige plastics (03_palette.md verdict "CRT TV, desk plastics": putty or beige; DEC 16:0x; plan check C1-9);
- denser bezel, cabinet and glass (more bevel segments, a 28 x 28 curved glass);
- a tuner knob and a volume knob on the chin, the power button, the IR window; a telescoping-antenna rod socket at
  the top rear; the rear vents as before; the floor cord removed (the station's cords are Kit_TVCords, P25);
- no brand, no text (the nameplate and strip are blank silver stock).
No pile block. Front (screen) faces -Y in Blender (+Z in Unity); origin = under the cabinet centre.
Budget 9,000 / 3,000 / 400.
"""

import math

import _station_common as sc  # noqa: F401 (slot registration)

import bmesh
from mathutils import Vector

NAME = "Kit_CRTTV_Station"
LODS = (0.94, 0.90)   # hard surface: LODs drop sub-pixel parts only (no collapse: it folded boxes into triangles)
SMOOTH_ANGLE = 38.0

BLACK = "Prop_PlasticBeige"
DARK = "Prop_Rubber"          # grille backing, vent slots, recesses, IR window, feet
SCREEN = "Prop_ScreenCRT"
LABEL = "Prop_Label"

# Prop_Label atlas (4 x 4, 1024 px): the TV rating plate in cell 12 and a
# blank strip of the same silver plate stock (below its text) for plain
# brushed-aluminium parts.
RATING_PLATE = (10 / 1024, 1 - 964 / 1024, 246 / 1024, 1 - 828 / 1024)
SILVER = (16 / 1024, 1 - 956 / 1024, 240 / 1024, 1 - 938 / 1024)

W, D, H = 0.62, 0.48, 0.50
FOOT = 0.008                     # cabinet bottom above the floor
ZC = FOOT + (H - FOOT) / 2       # cabinet centre height
FRONT = -D / 2                   # bezel face
OPEN_W, OPEN_H = 0.445, 0.336    # tube opening in the bezel
OPEN_TOP = H - 0.048
OPEN_CZ = OPEN_TOP - OPEN_H / 2

# Rear cabinet: front and back rectangles of the taper, back dropped so the
# bottom stays flat.
LOFT_Y0, LOFT_Y1 = -0.070, 0.192
LOFT_FRONT = (0.586, 0.462)
LOFT_BACK = (0.400, 0.300)
LOFT_ZC = ZC - 0.002
LOFT_DROP = -0.080


def smooth_path(points, sub=4):
    """Catmull-Rom through the control points (cords that sag, not kink)."""
    pts = [Vector(p) for p in points]
    ext = [pts[0] * 2 - pts[1]] + pts + [pts[-1] * 2 - pts[-2]]
    out = []
    for i in range(1, len(ext) - 2):
        p0, p1, p2, p3 = ext[i - 1], ext[i], ext[i + 1], ext[i + 2]
        for s in range(sub):
            t = s / sub
            out.append(0.5 * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                              + (-p0 + 3 * p1 - 3 * p2 + p3) * t * t * t))
    out.append(pts[-1])
    return [tuple(v) for v in out]


def drop_faces(obj, test):
    """Delete the faces whose LOCAL normal satisfies ``test``: the hidden
    back / ends of a small part that sits on a bigger one. Only for parts
    without a bevel (the shell is left open where nothing can see in)."""
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bm.normal_update()
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if test(f.normal)], context="FACES")
    bm.to_mesh(obj.data)
    bm.free()
    return obj


def slot_quad(kit, size_u, size_v, loc, slot, facing, uv="metres", uv_rect=None, tilt=0.0, name="quad"):
    """Flat decal or dark slot; ``tilt`` (degrees about X) lays a +z quad
    onto a sloping face."""
    q = kit.quad(size_u, size_v, loc, slot, facing=facing, uv=uv, uv_rect=uv_rect, name=name)
    if tilt:
        q.rotation_mode = "XYZ"
        q.rotation_euler = (math.radians(tilt), 0.0, 0.0)
    return q


def build(kit):
    # ------------------------------------------------------------ cabinet
    # Bezel: one rounded frame with the tube opening set high (deep chin).
    kit.frame((W, H - FOOT), (OPEN_W, OPEN_H), 0.045, (0, FRONT + 0.0225, ZC), BLACK,
              inner_offset=(0, OPEN_CZ - ZC), bevel=0.016, segments=6, name="bezel")
    # Hero: the tube mask's inner lip (a second, recessed frame round the glass).
    kit.frame((OPEN_W + 0.004, OPEN_H + 0.004), (OPEN_W - 0.018, OPEN_H - 0.018), 0.012, (0, FRONT + 0.031, OPEN_CZ), DARK,
              bevel=0.003, segments=3, name="mask lip")
    # Body block behind the bezel (7 mm inset = the bezel/cabinet seam).
    kit.box((W - 0.014, 0.135, H - FOOT - 0.012), (0, -0.1325, ZC), BLACK, bevel=0.010, segments=4,
            name="front cabinet")
    # Tapered rear cabinet (flat bottom, steep top/side draft).
    depth = LOFT_Y1 - LOFT_Y0
    kit.loft_box(LOFT_FRONT, LOFT_BACK, depth, (0, (LOFT_Y0 + LOFT_Y1) / 2, LOFT_ZC), BLACK,
                 back_offset=(0, LOFT_DROP), bevel=0.014, segments=5, name="rear cabinet")
    back_cz = LOFT_ZC + LOFT_DROP
    # Neck housing bump on the back.
    cap_h = 0.20
    kit.box((0.29, 0.042, cap_h), (0, LOFT_Y1 + 0.015, back_cz + 0.005), BLACK, bevel=0.012, segments=4,
            name="neck housing")
    back_y = LOFT_Y1 + 0.036     # rear face of the neck housing
    # Moulded feet (front pair under the body, rear pair under the taper);
    # their tops are inside the cabinet.
    for x, y, h in ((-0.25, -0.17, 0.016), (0.25, -0.17, 0.016), (-0.17, 0.15, 0.026), (0.17, 0.15, 0.026)):
        f = kit.cylinder(0.017, h, (x, y, h / 2), DARK, verts=16, bevel=0.0, name="foot")
        drop_faces(f, lambda n: n.z > 0.9)

    # ------------------------------------------------------------- tube
    kit.box((OPEN_W + 0.01, 0.02, OPEN_H + 0.01), (0, FRONT + 0.050, OPEN_CZ), BLACK, bevel=0.0,
            name="tube mask")
    kit.bulged_panel(OPEN_W + 0.006, OPEN_H + 0.006, 0.020, (0, FRONT + 0.036, OPEN_CZ), SCREEN,
                     segments=28, name="screen glass")

    # --------------------------------------------------- speaker grilles
    # Dark backing quad, 15 bars (front, top and bottom faces only: their
    # ends hide under the rails, their backs on the backing) and two rails.
    side = (W - OPEN_W) / 4 + OPEN_W / 2      # centre of the side border
    gh = OPEN_H - 0.05
    n = 14
    pitch = gh / n
    for sx in (-1, 1):
        slot_quad(kit, 0.050, gh + 0.010, (sx * side, FRONT - 0.0006, OPEN_CZ), DARK, "-y",
                  name="grille backing")
        for i in range(n + 1):
            bar = kit.box((0.050, 0.004, pitch * 0.42), (sx * side, FRONT - 0.0026, OPEN_CZ - gh / 2 + i * pitch),
                          BLACK, bevel=0.0, name="grille bar")
            drop_faces(bar, lambda nn: nn.y > 0.9 or abs(nn.x) > 0.9)
        for xx in (-0.0265, 0.0265):
            rail = kit.box((0.004, 0.0044, gh + 0.010), (sx * side + xx, FRONT - 0.0028, OPEN_CZ), BLACK,
                           bevel=0.0, name="grille rail")
            drop_faces(rail, lambda nn: nn.y > 0.9 or abs(nn.z) > 0.9)

    # ------------------------------------------------------------- chin
    chin_z = FOOT + (OPEN_CZ - OPEN_H / 2 - FOOT) / 2
    fy = FRONT - 0.0005
    # Power button in a dark recess ring, IR window.
    rec = kit.cylinder(0.0145, 0.002, (-0.235, fy + 0.0006, chin_z), DARK, verts=12, rot=(90, 0, 0),
                       bevel=0.0, name="power recess")
    drop_faces(rec, lambda nn: nn.z < 0.9)                   # keep only the front disc
    btn = kit.cylinder(0.0115, 0.010, (-0.235, fy - 0.002, chin_z), BLACK, verts=12, rot=(90, 0, 0),
                       bevel=0.0, name="power button")
    drop_faces(btn, lambda nn: nn.z < -0.9)                  # back cap is in the bezel
    ir = kit.box((0.036, 0.003, 0.016), (-0.160, fy - 0.0005, chin_z + 0.004), DARK, bevel=0.0,
                 name="ir window")
    drop_faces(ir, lambda nn: nn.y > 0.9)
    # Brushed nameplate and the silver control strip with six keys.
    kit.quad(0.078, 0.012, (0, fy - 0.0006, chin_z + 0.020), LABEL, facing="-y", uv_rect=SILVER,
             name="nameplate")
    kit.quad(0.170, 0.024, (0.180, fy - 0.0006, chin_z - 0.002), LABEL, facing="-y", uv_rect=SILVER,
             name="control strip")
    # Hero: a detented tuner knob and a smaller volume knob (skirt, knurled grip, pointer line), on the strip.
    for (kx, kr, kh) in ((0.135, 0.0135, 0.016), (0.205, 0.0105, 0.014)):
        kit.cylinder(kr + 0.0035, 0.003, (kx, fy - 0.0015, chin_z - 0.002), DARK, verts=28, rot=(90, 0, 0), bevel=0.0006, name="knob skirt")
        kit.cylinder(kr, kh, (kx, fy - 0.003 - kh / 2, chin_z - 0.002), BLACK, verts=28, rot=(90, 0, 0), bevel=0.0025, segments=3, name="knob")
        for g in range(12):
            ang = 2 * math.pi * g / 12
            kit.box((0.0016, kh * 0.7, 0.0016), (kx + math.cos(ang) * kr, fy - 0.003 - kh / 2, chin_z - 0.002 + math.sin(ang) * kr),
                    DARK, bevel=0.0, name="knurl")
        kit.box((0.0014, 0.0006, kr * 0.9), (kx, fy - 0.003 - kh - 0.0003, chin_z - 0.002 + kr * 0.45), DARK, bevel=0.0, name="pointer")
    # Hero: telescoping-antenna rod socket at the top rear (boss + bore).
    kit.cylinder(0.011, 0.012, (0.20, LOFT_Y1 - 0.03, LOFT_ZC + LOFT_DROP + LOFT_BACK[1] / 2 + 0.012), BLACK, verts=20, bevel=0.002, segments=2, name="rod socket")
    kit.cylinder(0.0045, 0.002, (0.20, LOFT_Y1 - 0.03, LOFT_ZC + LOFT_DROP + LOFT_BACK[1] / 2 + 0.0185), DARK, verts=16, bevel=0.0, name="rod bore")
    # Moulded design line across the chin.
    slot_quad(kit, W - 0.06, 0.0025, (0, fy - 0.0004, OPEN_CZ - OPEN_H / 2 - 0.016), DARK, "-y",
              name="chin groove")

    # ------------------------------------------------- rear vent slots
    # Slot quads across the sloping top of the rear cabinet.
    fz = LOFT_ZC + LOFT_FRONT[1] / 2
    bz = back_cz + LOFT_BACK[1] / 2
    slope = math.atan2(fz - bz, depth)
    nrm = Vector((0, math.sin(slope), math.cos(slope)))
    for s0 in range(13):
        s = 0.16 + s0 * 0.052
        p = Vector((0, LOFT_Y0 + depth * s, fz + (bz - fz) * s)) + nrm * 0.0008
        w = LOFT_FRONT[0] + (LOFT_BACK[0] - LOFT_FRONT[0]) * s
        ln = w / 2 - 0.075
        for sx in (-1, 1):
            slot_quad(kit, ln, 0.0065, (sx * (0.035 + ln / 2), p.y, p.z), DARK, "+z",
                      tilt=-math.degrees(slope), name="top vent slot")

    # Vent slots on the neck housing and the back panel.
    for i in range(7):
        slot_quad(kit, 0.090, 0.0055, (0.075, back_y + 0.0006, back_cz - 0.07 + i * 0.016), DARK, "+y",
                  name="rear vent slot")
    # AV terminal block: silver plate, F connector, three RCA jacks.
    # (The box keeps only its edges; its face is the silver decal.)
    av = kit.box((0.100, 0.004, 0.060), (-0.075, back_y + 0.0015, back_cz - 0.039), BLACK, bevel=0.0,
                 name="av plate")
    drop_faces(av, lambda nn: abs(nn.y) > 0.9)
    kit.quad(0.100, 0.060, (-0.075, back_y + 0.0035, back_cz - 0.039), LABEL, facing="+y", uv_rect=SILVER,
             name="av plate face")
    jacks = [(-0.105, back_cz - 0.024, 0.0055)] + [(-0.080 + k * 0.022, back_cz - 0.052, 0.0050) for k in range(3)]
    for x, z, r in jacks:
        j = kit.cylinder(r, 0.010, (x, back_y + 0.008, z), BLACK, verts=8, rot=(90, 0, 0), bevel=0.0,
                         name="av jack")
        drop_faces(j, lambda nn: nn.z > 0.9)                 # +Z local = -Y world, inside the plate
    # Rating plate (Prop_Label atlas cell 12, the plate itself).
    kit.quad(0.085, 0.049, (-0.010, back_y + 0.0012, back_cz + 0.060), LABEL, facing="+y",
             uv_rect=RATING_PLATE, name="rating label")

    # Flat top of the front cabinet behind the bezel crown (bezel is all bevel on top).
    kit.support("top", (0, -0.1325, H - 0.006), (0.58, 0.115))
    kit.anchor("screen", (0, FRONT - 0.005, OPEN_CZ))
    # One box (§5.3): front face to the neck housing, floor to the crown.
    kit.collider((0, (FRONT + back_y) / 2, H / 2), (W, back_y - FRONT, H))
    sc.auto_lod_drops(kit)
    kit.tag("electronics", "screen", "station")
    kit.meta["lodDistances"] = [6.0, 14.0, 60.0]
    kit.meta["budget"] = [9000, 3000, 400]
    kit.meta["station"] = {"line": "游戏视觉 station-look-v1", "origin": "floor under the cabinet centre", "note": "hero pass of Kit_CRTTV, beige; no pile block"}
