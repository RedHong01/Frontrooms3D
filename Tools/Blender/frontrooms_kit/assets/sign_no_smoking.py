"""Kit_SignNoSmoking: the engraved NO SMOKING plate over the smoking stand (游戏视觉, bs4-hero-kits, builder A;
research/station/72_hero_kits.md §2.2; 01 §6). New in Station_Waiting_3x2: S wall, x 4.60, on the wall 0.34 m
behind the stand, plate centre 1.55 m.

* Plate: engraved two-ply plastic, 10 x 3 x 1/8 in (0.254 x 0.076 x 0.0032 m), corners r 2, front edges eased
  0.8 mm in two steps; its back sits on the wall.
* Fixing: two chrome oval-head screws D7, slotted (two half domes 0.9 mm apart, the slots level), 15 mm in from
  the ends.
* Frame: ORIGIN = THE WALL FACE PLANE AT FLOOR LEVEL, centred (interact_key_board.py's host convention); plate
  centre at 1.55 m; front +Z. No collider.
* Art: the lettering is 平面视觉 · 信息设计's (signs and plates since the 10-15 split; 72 §3). The plate maps the
  decal 0-1 over its face (Prop_SignNoSmoking); until Prop_SignNoSmoking_A.png (1024 x 256) exists it is the blank
  cap colour #2B2925. No type of ours.

Slots: Prop_SignNoSmoking, Prop_Chrome. Budget 200 / 60 / 12; lodDistances [3, 8, 25]. Tags wall_decor, station,
sign.
"""

import math

import bmesh
from mathutils import Vector

import _station_waiting_common as w

NAME = "Kit_SignNoSmoking"
SMOOTH_ANGLE = w.SMOOTH

PW, PH, PT = 0.254, 0.076, 0.0032
CY = 1.55
SCREW_X = PW / 2 - 0.015
SCREW_D, SCREW_H = 0.007, 0.0016


def half_dome(kit, name, centre, side, r, h, gap, segs=6):
    """Half of a slotted oval head: a 180-degree lathe about Unity +z with its flat face on the slot plane, offset
    by gap / 2 toward ``side`` (+1 / -1 along y), so the slot runs along x."""
    prof = [(r, 0.0), (r * 0.72, h * 0.74), (0.0, h)]
    cx, cy, cz = centre
    off = side * gap / 2
    rings = []
    angs = [side * math.pi * k / segs for k in range(segs + 1)]   # 0 .. +-pi: the half on the +y or -y side
    for (rr, hh) in prof:
        if rr < 1e-7:
            rings.append([(cx, cy + off, cz + hh)])
            continue
        rings.append([(cx + rr * math.cos(a), cy + off + rr * math.sin(a), cz + hh) for a in angs])
    bm = bmesh.new()
    vr = [[bm.verts.new(w.B(*p)) for p in ring] for ring in rings]
    for a, b in zip(vr, vr[1:]):
        if len(b) == 1:
            for i in range(len(a) - 1):
                bm.faces.new((a[i], a[i + 1], b[0]))
        else:
            for i in range(len(a) - 1):
                bm.faces.new((a[i], a[i + 1], b[i + 1], b[i]))
    # the flat face on the slot plane: profile at angle 0 then back down at angle pi
    flat = [vr[0][0], vr[1][0], vr[2][0], vr[1][-1], vr[0][-1]]
    try:
        bm.faces.new(flat)
    except ValueError:
        pass
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return kit._new_object(name, bm, w.CHROME, "metres", "xz")


def build_level(kit, lv):
    hw, hh = PW / 2, PH / 2
    fr = w.Frame((0.0, CY, 0.0), (1, 0, 0), (0, 0, 1), (0, 1, 0))   # local x across, local y = out of the wall, local z = up
    if lv == 0:
        plate = w.pad(kit, "plate", w.SIGN, fr, hw, hh, [0.002] * 4, [2, 2, 2, 2], [[], [], [], []],
                      [(0.0, 0.0), (0.0, PT - 0.0008), (0.00035, PT - 0.00023), (0.0008, PT)], cap_top="ngon", cap_bottom=None)
        for sx in (-SCREW_X, SCREW_X):
            # the slots run level (the cut plane is Unity y = const)
            for side in (-1, 1):
                o = half_dome(kit, "screw", (0.0, 0.0, 0.0), side, SCREW_D / 2, SCREW_H, 0.0009)
                o.rotation_mode = "XYZ"
                # lathe built about Unity +z at the origin in a (x, y) plane: move it onto the plate face
                o.location = w.B(sx, CY, PT)
    elif lv == 1:
        plate = w.pad(kit, "plate", w.SIGN, fr, hw, hh, [0.002] * 4, [0, 0, 0, 0], [[], [], [], []],
                      [(0.0, 0.0), (0.0, PT)], cap_top="ngon", cap_bottom=None)
    else:
        plate = w.mesh_part(kit, "plate", w.SIGN, [(-hw, CY - hh, PT), (hw, CY - hh, PT), (hw, CY + hh, PT), (-hw, CY + hh, PT)],
                            [(0, 1, 2, 3)], outward=(0.0, CY, -1.0))
    plate["fr_uv"] = "decal"
    plate["fr_decal_axes"] = "xz"


def build(kit):
    w.register()
    for lv in (0, 1, 2):
        with w.Level(kit, str(lv)):
            build_level(kit, lv)
    w.tris_report(kit, "pre-finish")
    w.finish_parts(kit, SMOOTH_ANGLE)
    kit.no_collider()
    kit.tag("wall_decor", "station", "sign")
    w.sc.lods(kit, 3.0, 8.0, 25.0, (200, 60, 12))
    w.station_block(kit, "wall face plane at floor level, centred", "front +Z; plate centre 1.55 m; art by 平面视觉 · 信息设计")
