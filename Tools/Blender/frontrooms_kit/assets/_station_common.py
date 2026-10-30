"""Shared helpers for the A26 station (Floor 1 first room, "Station_FrontDesk_4x4") kits.

游戏视觉, station-look-v1 (research/station/70_station_look_v1_plan.md, 71_station_look_v1_build.md).

Authoring is in UNITY space (x right as seen from the room, y up, z toward the
room / the kit's front), converted to the kit's Blender convention on the way
in (kitlib: Unity (X, Y, Z) = Blender (-x, z, -y)).

Wall kits use the host convention of interact_key_board.py: ORIGIN = THE WALL
FACE PLANE AT FLOOR LEVEL, front +Z into the room, heights baked in, so the
module places them at y 0 and p.y + boundsMax.y is the real top.

The station paper atlas (Prop_StationPaper) is 平面视觉's art; until it exists
the slot is a blank paper placeholder (no invented type). Its cells (4 x 4,
row-major from the top-left) are fixed here so the art can drop in unchanged.
"""

import math

import kitlib
from mathutils import Vector

# --------------------------------------------------------------- slots
STEEL_BEIGE = "Prop_SteelBeige"          # annunciator cabinet (PAL: beige cabinet, beige door)
JEWEL_GREEN = "Prop_JewelGreen"          # POWER pilot (emissive, steady)
ANN_LABEL = "Prop_AnnunciatorLabel"      # engraved label strips + frosted lenses #C9C3B2
PAPER = "Prop_StationPaper"              # 平面视觉's atlas (blank placeholder)
KEY_PLATE = "Prop_StationKeyPlate"       # STAIR KEY engraved plate (blank placeholder)
CORK = "Prop_Cork"
UNIFORM = "Prop_FabricUniform"           # jacket #59647A
LAMINATE_GREY = "Prop_LaminateGrey"      # counter fascia #8E8E88


def register():
    kitlib.register_slot(STEEL_BEIGE, (0.66, 0.62, 0.53), 0.55, 0.0)
    kitlib.register_slot(JEWEL_GREEN, (0.06, 0.55, 0.12), 0.15, 0.0)
    kitlib.register_slot(ANN_LABEL, (0.58, 0.54, 0.45), 0.6, 0.0)
    kitlib.register_slot(PAPER, (0.86, 0.85, 0.80), 0.85, 0.0)
    kitlib.register_slot(KEY_PLATE, (0.16, 0.15, 0.14), 0.5, 0.0)
    kitlib.register_slot(CORK, (0.33, 0.20, 0.10), 0.9, 0.0)
    kitlib.register_slot(UNIFORM, (0.10, 0.13, 0.19), 0.9, 0.0)
    kitlib.register_slot(LAMINATE_GREY, (0.27, 0.27, 0.25), 0.5, 0.0)
    kitlib.register_slot("Prop_ThermosetIvory", (0.80, 0.76, 0.64), 0.4, 0.0)


# Paper atlas cells (4 x 4).
PAPER_CELLS = {
    "register_left": 0, "register_right": 1, "tent_card": 2, "dar": 3,
    "sign_out": 4, "key_control": 5, "phone_list": 6, "notice_a": 7,
    "notice_b": 8, "notice_c": 9, "notice_d": 10, "post_orders_spine": 11,
    "sheet_blank": 12,
}


def paper_rect(name, crop=(1.0, 1.0)):
    c = PAPER_CELLS[name]
    u0, v0, u1, v1 = kitlib.Kit.atlas_cell(c, 4, 4)
    cw, ch = (u1 - u0) * crop[0], (v1 - v0) * crop[1]
    return (u0, v1 - ch, u0 + cw, v1)


# --------------------------------------------------------------- space
def B(x, y, z):
    """Unity point -> Blender point."""
    return (-x, -z, y)


def BS(sx, sy, sz):
    """Unity size -> Blender size."""
    return (sx, sz, sy)


def box(kit, size, centre, slot, bevel=0.002, segments=2, name="box"):
    return kit.box(BS(*size), B(*centre), slot, bevel=bevel, segments=segments, name=name)


def cyl(kit, r, length, centre, axis, slot, verts=24, bevel=0.0008, segments=2, name="cyl", radius_top=None):
    """Cylinder along a Unity axis ('x', 'y' or 'z')."""
    rot = {"y": (0, 0, 0), "z": (90, 0, 0), "x": (0, 90, 0)}[axis]
    return kit.cylinder(r, length, B(*centre), slot, verts=verts, rot=rot, bevel=bevel, segments=segments,
                        name=name, radius_top=radius_top)


def tube(kit, pts, r, slot, verts=12, name="tube", caps=True):
    return kit.tube([B(*p) for p in pts], r, slot, verts=verts, name=name, caps=caps)


FACING = {"+z": "-y", "-z": "+y", "+y": "+z", "-x": "+x", "+x": "-x"}


def quad(kit, w, h, centre, slot, facing="+z", uv="decal", uv_rect=None, name="quad"):
    return kit.quad(w, h, B(*centre), slot, facing=FACING[facing], uv=uv, uv_rect=uv_rect, name=name)


def extrude_xy(kit, outline, depth, z_centre, slot, bevel=0.0005, segments=1, name="extrude"):
    """Extrude a Unity x/y outline (a closed polygon) along Unity z, centred at z_centre."""
    pts = [(-x, y) for x, y in outline]
    if signed_area(pts) < 0:
        pts = list(reversed(pts))
    return kit.extrude(pts, depth, (0.0, -z_centre, 0.0), slot, plane="xz", bevel=bevel, segments=segments, name=name)


def extrude_zy(kit, outline, depth, x_centre, slot, bevel=0.0005, segments=1, name="extrude"):
    """Extrude a Unity z/y outline (side profile) along Unity x, centred at x_centre."""
    # Blender plane "yz": (u, v) = (by, bz) = (-z, y), extruded along bx = -x.
    pts = [(-z, y) for z, y in outline]
    if signed_area(pts) < 0:
        pts = list(reversed(pts))
    return kit.extrude(pts, depth, (-x_centre, 0.0, 0.0), slot, plane="yz", bevel=bevel, segments=segments, name=name)


def lathe_y(kit, profile, base, slot, verts=32, name="lathe", close_top=True, close_bottom=True):
    """Revolve a (radius, height) profile about a vertical Unity axis through base (x, y, z)."""
    return kit.lathe(profile, B(*base), slot, verts=verts, name=name, close_top=close_top, close_bottom=close_bottom)


def signed_area(pts):
    a = 0.0
    for i in range(len(pts)):
        x0, y0 = pts[i]
        x1, y1 = pts[(i + 1) % len(pts)]
        a += x0 * y1 - x1 * y0
    return a * 0.5


def rounded_rect(w, h, r, segs=4, cx=0.0, cy=0.0):
    out = []
    for (sx, sy, a0) in ((1, 1, 0), (-1, 1, 90), (-1, -1, 180), (1, -1, 270)):
        ccx, ccy = cx + sx * (w / 2 - r), cy + sy * (h / 2 - r)
        for k in range(segs + 1):
            a = math.radians(a0 + 90 * k / segs)
            out.append((ccx + r * math.cos(a), ccy + r * math.sin(a)))
    return out


def smooth_path(points, sub=4):
    """Catmull-Rom through control points (cords that sag, not kink)."""
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


def coil(start, end, radius, turns, pitch_sag=0.0, samples_per_turn=10):
    """A coiled phone cord between two Unity points (a helix about the chord, sagging by pitch_sag)."""
    a, b = Vector(start), Vector(end)
    axis = b - a
    n = max(2, int(turns * samples_per_turn))
    d = axis.normalized()
    ref = Vector((0, 1, 0)) if abs(d.y) < 0.9 else Vector((1, 0, 0))
    s1 = d.cross(ref).normalized()
    s2 = d.cross(s1).normalized()
    pts = []
    for i in range(n + 1):
        t = i / n
        c = a + axis * t + Vector((0, -pitch_sag * 4 * t * (1 - t), 0))
        ang = 2 * math.pi * turns * t
        pts.append(tuple(c + (s1 * math.cos(ang) + s2 * math.sin(ang)) * radius))
    return pts


def lods(kit, d01, d12, dcull, budget):
    kit.meta["lodDistances"] = [d01, d12, dcull]
    kit.meta["budget"] = list(budget)


def no_pile(kit):
    """Station kits never join a furniture pile (plan check C2-4)."""
    kit.meta.pop("pile", None)
    kit.meta["tags"] = [t for t in kit.meta.get("tags", []) if t not in ("pile", "pile_piece")]


def station_meta(kit, origin, note=""):
    kit.meta["station"] = {"line": "游戏视觉 station-look-v1", "origin": origin, "note": note}


def auto_lod_drops(kit, d1=0.05, d2=0.20, keep=()):
    """Sub-pixel parts leave the LODs instead of blocking the collapse (kitlib protects every part under 12 mm, so
    screws, washers and thin plates would otherwise keep LOD1 and LOD2 at LOD0's count): a part whose largest
    dimension is under d1 is dropped from LOD1 and LOD2, under d2 from LOD2. Parts named in ``keep`` stay."""
    for obj in kit.parts:
        if any(k in obj.name for k in keep):
            continue
        m = max(obj.dimensions)
        if m < d1:
            obj["fr_lod1_drop"] = True
            obj["fr_lod2_drop"] = True
        elif m < d2:
            obj["fr_lod2_drop"] = True
