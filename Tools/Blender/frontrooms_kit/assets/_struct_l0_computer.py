"""P16 structures_l0_props, part 1: the SP3 computer room of 1988-91 (structures_assets G3-a1, line visual (game visual),
2026-11-10). Geometry for every kit of the room; the thin structures_*.py modules name them.

Plan: Documentation/research/structures_assets/30_plan.md P16 / S3-07; specs 62_furniture_l0_office.md 2.3 and 3.1 (LS56,
LS83-LS86, LS98); real sizes 60_build.md G3-a1 S1-S8: IBM 3420-class tape unit 29 7/8 w x 66 3/8 h x 29 5/8 d in (S1),
10.5 in reels of 1/2 in tape (S2), Dataproducts B-600 acoustic cabinet 87 w x 75.5 d x 113 h cm, warm tone tan and brown,
paper in at the bottom front, out at the rear stacker (S3), VT320-class terminal 31.4 w x 25.3 h x 31.5 d cm (S4), Liebert
Deluxe System/3-class downflow unit, insulated powder-coated panels on a welded frame (S5), Kidde-class Halon 1301
container (S6), 10 in circular chart recorder (S7).

Frames and rules as _struct_common: every kit is specified in UNITY space (front +Z, origin the floor centre unless the kit
says otherwise) and converted with ub(); levels built by hand (B.levels: LOD0 hero, LOD1 about a third, LOD2 boxes). New
slots: Prop_CompAtlas (decal atlas, uv_rect from CELLS: kept in step with Tools/lookdev/structures/gen_structures_g3a1.py),
Prop_CompIndicator (the lit lenses: the ONE slot a kit's lamps use, so the lamp modes or the power state drive it or swap it
for Prop_CompIndicator_Off), Prop_PerfSteel (tiling perforated steel, metres UVs, 0.096 m tile). Screens dark. No text, label,
brand or logo. ASCII only.
"""

import math
import random

import bmesh
import bpy
from mathutils import Vector

import kitlib
import _run_b_common as B
import _struct_common as S
import _struct_shared as H

ATLAS, IND, PERF = "Prop_CompAtlas", "Prop_CompIndicator", "Prop_PerfSteel"
for _slot, _c, _r in ((ATLAS, (0.55, 0.55, 0.55), 0.5), (IND, (0.3, 0.6, 0.3), 0.3), (PERF, (0.62, 0.58, 0.48), 0.55),
                      ("Prop_CompIndicator_Off", (0.15, 0.2, 0.15), 0.3),
                      ("Stair_Slab", (0.45, 0.45, 0.43), 0.8), ("Prop_SteelBrown", (0.16, 0.11, 0.08), 0.55)):
    kitlib.register_slot(_slot, _c, _r)

STEEL = "Prop_SteelBeige"        # painted steel of the cabinets (main)
PUTTY = "Prop_PlasticPutty"      # textured putty ABS of doors and bezels (main)
CHAR = "Prop_PlasticGrey"        # charcoal trim (main)
BLACK = "Prop_PlasticBlack"
GLASS = "Prop_Glass"
CHROME = "Prop_Chrome"
ALU = "Prop_Aluminium"
BROWN = "Prop_SteelBrown"
PBEIGE = "Prop_PlasticBeige"
SPUTTY = "Prop_SteelPutty"
SBLACK = "Prop_SteelBlack"
RED = "RunB_FireRed"
BRASS = "Prop_Brass"
GALV = "RunB_GalvSteel"
CARD = "Prop_Cardboard"
SCREEN = "Prop_ScreenCRT"
RUBBER = "Prop_Rubber"

SIZE = 2048.0
CELLS = {
    "PACK": (0, 0, 512, 512), "CHART": (512, 0, 512, 512), "GREENBAR": (1024, 0, 640, 470),
    "GREENBAR_EDGE": (1664, 0, 384, 96), "CARTON": (1664, 96, 384, 224), "HALON_LABEL": (1664, 320, 384, 192),
    "PERF_PANEL": (0, 512, 512, 512), "METER_V": (512, 512, 192, 192), "METER_A": (704, 512, 192, 192),
    "METER_K": (896, 512, 192, 192), "REEL_LABEL": (512, 704, 256, 128), "CART_LABEL": (768, 704, 256, 64),
    "PANEL_CPU": (1024, 1280, 1024, 440), "PANEL_TAPE": (0, 1280, 1024, 232), "PANEL_PDU": (1088, 832, 448, 192),
    "PANEL_CRAC": (1536, 512, 512, 256), "PANEL_PRINTER": (1536, 768, 256, 128), "PANEL_DOTMATRIX": (1792, 768, 256, 128),
    "PLATE_EPO": (1536, 896, 256, 128), "PLATE_HALON": (1792, 896, 256, 128), "COLOURS": (0, 1024, 512, 128),
    "TAPE": (512, 1024, 512, 64), "DISK_FACE": (1024, 1024, 512, 256), "PANEL_DISKPACK": (1536, 1024, 256, 128),
    "PANEL_CART": (1792, 1024, 256, 128), "PANEL_COMMS": (1536, 1152, 512, 128), "TAPE_BACK": (512, 1088, 512, 64),
}
COL = {n: i for i, n in enumerate(("belt_blue", "belt_grey", "belt_white", "belt_red", "belt_yellow", "belt_black",
                                   "cable_beige", "cable_grey", "cable_black", "cable_blue", "oxide", "reel_smoke",
                                   "hub_dark", "rubber", "ring_red", "paper"))}
# Prop_CompIndicator cells: lit 0 green, 1 amber, 2 red, 3 white LED; 4 blue; 5-8 green / amber / red / white square buttons;
# 9 VFD window; 10 red digits; 11 pilot jewel; unlit 12 green, 13 amber, 14 red, 15 clear.
LED_G, LED_A, LED_R, LED_W, LED_B = 0, 1, 2, 3, 4
BTN_G, BTN_A, BTN_R, BTN_W = 5, 6, 7, 8
VFD, DIGITS, JEWEL = 9, 10, 11
OFF_G, OFF_A, OFF_R, OFF_C = 12, 13, 14, 15
# Button / lamp sites in plate fractions (u from the left as seen from the front, v from the bottom) = the generator's PANELS.
PANELS = {
    "PANEL_CPU": {"size": (0.56, 0.24), "buttons": [(0.30 + 0.08 * i, 0.50) for i in range(6)], "bsize": 0.026,
                  "key": (0.12, 0.50), "window": (0.79, 0.34, 0.95, 0.66)},
    "PANEL_TAPE": {"size": (0.66, 0.15), "buttons": [(0.12 + 0.135 * i, 0.50) for i in range(6)], "bsize": 0.030},
    "PANEL_CRAC": {"size": (0.40, 0.20), "buttons": [(0.66 + 0.13 * (i % 3), 0.74 - 0.26 * (i // 3)) for i in range(6)],
                   "bsize": 0.030, "lcd": (0.07, 0.42, 0.55, 0.86), "leds": [(0.12 + 0.11 * i, 0.22) for i in range(4)]},
    "PANEL_PRINTER": {"size": (0.30, 0.15), "buttons": [(0.14 + 0.18 * i, 0.32) for i in range(4)], "bsize": 0.030,
                      "leds": [(0.14, 0.76), (0.28, 0.76)], "window": (0.60, 0.60, 0.86, 0.90)},
    "PANEL_DOTMATRIX": {"size": (0.17, 0.065), "buttons": [(0.42 + 0.16 * i, 0.45) for i in range(3)], "bsize": 0.014,
                        "leds": [(0.10, 0.65), (0.19, 0.65), (0.28, 0.65)]},
    "PANEL_PDU": {"size": (0.56, 0.24), "breakers": [(0.08 + 0.0767 * i, 0.38) for i in range(12)], "bsize": (0.026, 0.07)},
    "PANEL_DISKPACK": {"size": (0.30, 0.15), "buttons": [(0.25, 0.48), (0.50, 0.48), (0.75, 0.48)], "bsize": 0.038},
    "PANEL_CART": {"size": (0.30, 0.15), "buttons": [(0.12 + 0.16 * i, 0.30) for i in range(4)], "bsize": 0.026,
                   "window": (0.10, 0.58, 0.62, 0.86)},
    "PANEL_COMMS": {"size": (0.60, 0.15), "leds": [(0.07 + 0.058 * i, 0.25 + 0.25 * j) for j in range(3) for i in range(16)]},
}


# ============================================================================ atlas rects
def rect(name, inset=1.5):
    x, y, w, h = CELLS[name]
    return ((x + inset) / SIZE, 1 - (y + h - inset) / SIZE, (x + w - inset) / SIZE, 1 - (y + inset) / SIZE)


def sub(name, fu0, fv0, fu1, fv1):
    """A sub-rect of a cell in its own fractions (u from the left, v from the bottom)."""
    u0, v0, u1, v1 = rect(name, 0.0)
    return (u0 + (u1 - u0) * fu0, v0 + (v1 - v0) * fv0, u0 + (u1 - u0) * fu1, v0 + (v1 - v0) * fv1)


def colour(name):
    i = COL[name]
    x, y = (i % 8) * 64, 1024 + (i // 8) * 64
    return ((x + 18) / SIZE, 1 - (y + 46) / SIZE, (x + 46) / SIZE, 1 - (y + 18) / SIZE)


def ind(i, full=False):
    c, r = i % 4, i // 4
    k = 0.004 if full else 0.03
    return (c / 4 + k, 1 - (r + 1) / 4 + k, (c + 1) / 4 - k, 1 - r / 4 - k)


AXES = {"+z": "xz", "+y": "xy", "+x": "yz"}


def dbox(kit, size, centre, rc, lv, slot=ATLAS, w=0.0005, facing="+z", name="plate", segs=None):
    """A box (Unity size and centre) whose faces carry an atlas rect, planar on the facing's axes (kitlib decal UVs)."""
    bw, bs = S.bev(w, lv)
    if segs is not None and lv == 0:
        bs = segs
    o = kit.box((size[0], size[2], size[1]), S.ub(centre), slot, bevel=bw if bw > 0 else 0.0, segments=bs, uv="decal",
                decal_axes=AXES[facing], name=name)
    o["fr_uv_rect"] = list(rc)
    return o


def cbox(kit, size, centre, cname, lv, w=0.0006, name="part"):
    """A box in one flat colour of the atlas."""
    return dbox(kit, size, centre, colour(cname), lv, w=w, name=name)


def flat(kit, size, centre, slot, name="flat"):
    """An unbevelled box for hidden or backing parts (interiors, well floors, dial backs)."""
    return kit.box((size[0], size[2], size[1]), S.ub(centre), slot, bevel=0.0, segments=0, name=name)


def box(kit, size, centre, slot, lv, w=0.0012, name="box", segs=None):
    if segs is not None and lv == 0:
        bw, _ = S.bev(w, lv)
        return kit.box((size[0], size[2], size[1]), S.ub(centre), slot, bevel=bw, segments=segs, name=name)
    return S.box(kit, size, centre, slot, lv, w=w, name=name)


# ============================================================================ Unity-space meshes with atlas UVs
RIGHT_UP = {"+z": ((-1, 0, 0), (0, 1, 0)), "-z": ((1, 0, 0), (0, 1, 0)), "+x": ((0, 0, 1), (0, 1, 0)),
            "-x": ((0, 0, -1), (0, 1, 0)), "+y": ((-1, 0, 0), (0, 0, -1)), "-y": ((-1, 0, 0), (0, 0, 1))}
NORMAL = {"+z": (0, 0, 1), "-z": (0, 0, -1), "+x": (1, 0, 0), "-x": (-1, 0, 0), "+y": (0, 1, 0), "-y": (0, -1, 0)}


def _oriented(m, pts, uvs, out):
    """Add a polygon (Unity points, UVs) so its normal points along `out` (Mesh.face keeps right-handed CCW = outward)."""
    a, b, c = Vector(pts[0]), Vector(pts[1]), Vector(pts[2])
    n = (b - a).cross(c - a)
    if n.dot(Vector(out)) < 0:
        pts, uvs = list(reversed(pts)), list(reversed(uvs))
    idx = [m.vert(tuple(p)) for p in pts]
    m.face(tuple(idx), list(uvs))


def face_quad(m, centre, facing, w, h, rc):
    """A quad of w x h facing a Unity axis, mapped to the atlas rect as seen from outside (u right, v up)."""
    r, u = Vector(RIGHT_UP[facing][0]), Vector(RIGHT_UP[facing][1])
    c = Vector(centre)
    bl, br = c - r * w / 2 - u * h / 2, c + r * w / 2 - u * h / 2
    tr, tl = c + r * w / 2 + u * h / 2, c - r * w / 2 + u * h / 2
    u0, v0, u1, v1 = rc
    _oriented(m, [bl, br, tr, tl], [(u0, v0), (u1, v0), (u1, v1), (u0, v1)], NORMAL[facing])


def quad_part(kit, centre, facing, w, h, rc, slot=ATLAS, name="decal"):
    m = S.Mesh()
    face_quad(m, centre, facing, w, h, rc)
    return m.part(kit, name, slot)


def disc_part(kit, centre, r, facing, rc, segs, slot=ATLAS, r_in=0.0, r_map=None, name="disc"):
    """A flat disc (or ring from r_in) facing a Unity axis; UVs map the square of side 2 * r_map (default r) onto rc."""
    rm = r_map or r
    rv, uv_ = Vector(RIGHT_UP[facing][0]), Vector(RIGHT_UP[facing][1])
    c = Vector(centre)
    u0, v0, u1, v1 = rc
    m = S.Mesh()

    def P(rad, a):
        x, y = rad * math.cos(a), rad * math.sin(a)
        return c + rv * x + uv_ * y, (u0 + (u1 - u0) * (0.5 + x / (2 * rm)), v0 + (v1 - v0) * (0.5 + y / (2 * rm)))

    for k in range(segs):
        a0, a1 = 2 * math.pi * k / segs, 2 * math.pi * (k + 1) / segs
        p0, t0 = P(r, a0)
        p1, t1 = P(r, a1)
        if r_in > 0:
            q0, s0 = P(r_in, a0)
            q1, s1 = P(r_in, a1)
            _oriented(m, [q0, p0, p1, q1], [s0, t0, t1, s1], NORMAL[facing])
        else:
            cc, tc = P(0.0, 0.0)
            _oriented(m, [cc, p0, p1], [tc, t0, t1], NORMAL[facing])
    return m.part(kit, name, slot)


def dome(kit, centre, r, h, facing, idx, lv, name="lens", segs=None):
    """An LED dome (radius r, height h) standing on a face: rings of a spherical cap, UVs into the indicator cell's disc."""
    rv, uv_ = Vector(RIGHT_UP[facing][0]), Vector(RIGHT_UP[facing][1])
    nv = Vector(NORMAL[facing])
    c = Vector(centre)
    u0, v0, u1, v1 = ind(idx, full=True)
    rings = 2 if (lv == 0 and not segs) else 1
    segs = segs or (10 if lv == 0 else 6)
    m = S.Mesh()
    pts = []
    for j in range(rings + 1):
        t = j / rings
        rr = r * math.cos(t * math.pi / 2 * 0.92)
        hh = h * math.sin(t * math.pi / 2 * 0.92)
        pts.append((rr, hh))
    pts.append((0.0, h))

    def P(rr, hh, a):
        x, y = rr * math.cos(a), rr * math.sin(a)
        return c + rv * x + uv_ * y + nv * hh, (u0 + (u1 - u0) * (0.5 + 0.48 * x / r), v0 + (v1 - v0) * (0.5 + 0.48 * y / r))

    for k in range(segs):
        a0, a1 = 2 * math.pi * k / segs, 2 * math.pi * (k + 1) / segs
        for j in range(len(pts) - 1):
            (r0, h0), (r1, h1) = pts[j], pts[j + 1]
            p00, t00 = P(r0, h0, a0)
            p01, t01 = P(r0, h0, a1)
            p11, t11 = P(r1, h1, a1)
            p10, t10 = P(r1, h1, a0)
            if r1 < 1e-6:
                _oriented(m, [p00, p01, p11], [t00, t01, t11], (p00 - c).normalized() + nv * 0.5)
            else:
                _oriented(m, [p00, p01, p11, p10], [t00, t01, t11, t10], ((p00 + p11) / 2 - c).normalized() + nv * 0.2)
    return m.part(kit, name, IND)


def led(kit, centre, idx, lv, facing="+z", r=0.0032, bezel=True, segs=None):
    """A 5 mm panel LED (dome lens, one indicator cell) in a black bezel ring; fields of lamps pass bezel=False (the plate
    prints the ring) and segs=6."""
    nv = Vector(NORMAL[facing])
    rot = {"+z": (90, 0, 0), "-z": (90, 0, 0), "+x": (0, 90, 0), "-x": (0, 90, 0), "+y": (0, 0, 0), "-y": (0, 0, 0)}[facing]
    if bezel and lv < 2:
        kit.cylinder(r * 1.6, 0.002, S.ub(tuple(Vector(centre) + nv * 0.001)), BLACK, verts=10 if lv == 0 else 6,
                     rot=rot, bevel=0.0004 if lv == 0 else 0.0, segments=1, name="led bezel")
    if lv == 2:
        return quad_part(kit, tuple(Vector(centre) + nv * 0.0015), facing, r * 2, r * 2, ind(idx), IND, name="led")
    return dome(kit, tuple(Vector(centre) + nv * 0.0018), r, r * 0.9, facing, idx, lv, segs=segs)


def button(kit, centre, size, idx, lv, facing="+z", depth=0.012, bezel_slot=BLACK):
    """A lit square push-button: the bezel collar and the lens cap (one indicator cell), standing on a face."""
    nv = Vector(NORMAL[facing])
    c = Vector(centre)
    s = size
    if facing not in ("+z",):
        raise ValueError("button faces +z only")
    if lv < 2:
        box(kit, (s + 0.008, s + 0.008, 0.004), tuple(c + nv * 0.002), bezel_slot, lv, w=0.0008, name="button bezel", segs=1)
    cap = dbox(kit, (s, s, depth * 0.5), tuple(c + nv * (0.004 + depth * 0.25)), ind(idx, full=True), lv, slot=IND, w=0.0009,
               name="button lens", segs=1)
    return cap


def key_switch(kit, centre, lv):
    """A key-operated rotary switch: a chrome bezel ring, the black barrel, the key's flat bow turned to RUN."""
    c = Vector(centre)
    S.cyl_z(kit, 0.019, 0.006, tuple(c + Vector((0, 0, 0.003))), CHROME, lv, w=0.0008, name="key bezel")
    if lv < 2:
        S.cyl_z(kit, 0.012, 0.010, tuple(c + Vector((0, 0, 0.009))), BLACK, lv, w=0.0006, name="key barrel")
        box(kit, (0.006, 0.018, 0.010), tuple(c + Vector((0, 0, 0.018))), CHROME, lv, w=0.0006, name="key blade")
        box(kit, (0.026, 0.022, 0.004), tuple(c + Vector((0, 0.018, 0.024))), BLACK, lv, w=0.0012, name="key bow")


def panel_sites(name, size_override=None):
    P = PANELS[name]
    return P, (size_override or P["size"])


def site(name, uv, centre, size=None):
    """Unity point of a panel site: the plate faces +Z at `centre`; u runs to the viewer's right (Unity -X)."""
    P, sz = panel_sites(name, size)
    return (centre[0] - (uv[0] - 0.5) * sz[0], centre[1] + (uv[1] - 0.5) * sz[1], centre[2])


def plate(kit, name, centre, lv, t=0.0016, size=None):
    """A printed operator plate (atlas cell `name`) facing +Z, its front face at centre z + t / 2."""
    P, sz = panel_sites(name, size)
    return dbox(kit, (sz[0], sz[1], t), centre, rect(name), lv, w=0.0004, name=name.lower())


def uvoff(obj, seed):
    rnd = random.Random(seed)
    obj["fr_uv_offset"] = (rnd.uniform(0.0, 5.0), rnd.uniform(0.0, 5.0))
    return obj


# ============================================================================ Kit_SystemCabinet_* (1989 midrange cabinets)
CAB_W, CAB_D = 0.76, 0.86
PLINTH, SIDE_T, TOP_T, DOOR_T, GAP = 0.075, 0.018, 0.022, 0.032, 0.003


def cab_shell(kit, lv, W, D, H, seed=0, side_vent=True, back_vent=True):
    """Welded-frame cabinet shell: a charcoal plinth recessed 20 mm, two painted steel side panels with a pressed inner
    field and a perforated vent at the top rear, the top cover with its rear vent, the rear door with a perforated field."""
    box(kit, (W - 0.04, PLINTH, D - 0.035), (0.0, PLINTH / 2, -0.0025), CHAR, lv, w=0.002, name="plinth")
    hs = H - PLINTH - TOP_T
    yc = PLINTH + hs / 2
    for sx in (-1, 1):
        uvoff(box(kit, (SIDE_T, hs, D), (sx * (W / 2 - SIDE_T / 2), yc, 0.0), STEEL, lv, w=0.003, name="side panel"), seed + sx)
        if lv == 0:
            uvoff(box(kit, (0.002, hs - 0.14, D - 0.14), (sx * (W / 2 + 0.001), yc, 0.0), STEEL, lv, w=0.0015, name="side field"), seed + 3 * sx)
        if side_vent and lv < 2:
            box(kit, (0.0015, 0.20, 0.26), (sx * (W / 2 + (0.0028 if lv == 0 else 0.0008)), H - TOP_T - 0.17, -D / 2 + 0.19),
                PERF, lv, w=0.0004, name="side vent", segs=1)
    uvoff(box(kit, (W, TOP_T, D), (0.0, H - TOP_T / 2, 0.0), STEEL, lv, w=0.004, name="top cover"), seed + 7)
    if lv < 2:
        box(kit, (W - 0.12, 0.0015, 0.22), (0.0, H + 0.0007, -D / 2 + 0.16), PERF, lv, w=0.0004, name="top vent", segs=1)
    box(kit, (W - 2 * SIDE_T, hs, 0.02), (0.0, yc, -D / 2 + 0.01), STEEL, lv, w=0.002, name="rear door")
    if back_vent:
        box(kit, (W - 2 * SIDE_T - 0.12, hs * 0.42, 0.0015), (0.0, PLINTH + hs * 0.34, -D / 2 - 0.0007), PERF, lv, w=0.0004,
            name="rear vent", segs=1)
        if lv < 2:
            box(kit, (0.02, 0.16, 0.012), ((W / 2 - SIDE_T - 0.05), PLINTH + hs * 0.62, -D / 2 - 0.006), CHAR, lv, w=0.002, name="rear pull")


def cab_door(kit, lv, W, D, y0, y1, seed=0, grille=True, stripe=True, pull=True):
    """The front door between the side panels, flush with their front edges: textured putty ABS over steel, a recessed
    charcoal vertical stripe at the left quarter (the late-80s two-tone), an intake grille band low, a flush pull."""
    zf = D / 2
    wd = W - 2 * SIDE_T - 2 * GAP
    h = y1 - y0
    yc = (y0 + y1) / 2
    xl = wd / 2                      # the door's left edge as seen from the front is Unity +x
    if stripe:
        sw, sx0 = 0.026, 0.11        # stripe width and its distance from the left edge
        left_w = sx0
        right_w = wd - sx0 - sw
        uvoff(box(kit, (left_w, h, DOOR_T), (xl - left_w / 2, yc, zf - DOOR_T / 2), PUTTY, lv, w=0.003, name="door left"), seed)
        uvoff(box(kit, (right_w, h, DOOR_T), (xl - sx0 - sw - right_w / 2, yc, zf - DOOR_T / 2), PUTTY, lv, w=0.003, name="door"), seed + 1)
        box(kit, (sw + 0.002, h - 0.002, DOOR_T - 0.008), (xl - sx0 - sw / 2, yc, zf - DOOR_T / 2 - 0.004), CHAR, lv, w=0.0015, name="door stripe")
    else:
        uvoff(box(kit, (wd, h, DOOR_T), (0.0, yc, zf - DOOR_T / 2), PUTTY, lv, w=0.003, name="door"), seed + 1)
    if grille and lv < 2:
        gw = wd - (0.19 if stripe else 0.08)
        gx = (xl - 0.15 - gw / 2) if stripe else 0.0
        box(kit, (gw, 0.11, 0.0015), (gx, y0 + 0.11, zf + 0.0007), PERF, lv, w=0.0004, name="intake grille", segs=1)
        if lv == 0:
            box(kit, (gw + 0.012, 0.122, 0.0012), (gx, y0 + 0.11, zf + 0.0002), CHAR, lv, w=0.0004, name="grille frame", segs=1)
    if pull and lv < 2:
        px = -(wd / 2 - 0.035)
        box(kit, (0.024, 0.17, 0.0015), (px, yc + h * 0.08, zf + 0.0007), BLACK, lv, w=0.0005, name="pull recess", segs=1)
        box(kit, (0.006, 0.15, 0.009), (px - 0.009, yc + h * 0.08, zf + 0.0045), CHROME, lv, w=0.0012, name="pull lip")


def cab_fascia(kit, lv, W, D, y0, y1):
    wd = W - 2 * SIDE_T - 2 * GAP
    box(kit, (wd, y1 - y0, DOOR_T + 0.002), (0.0, (y0 + y1) / 2, D / 2 - DOOR_T / 2 + 0.001), CHAR, lv, w=0.003, name="fascia")
    return D / 2 + 0.002


def cabinet_cpu(kit, lv):
    """Kit_SystemCabinet_CPU, 1.83 m: the processor cabinet with its operator panel (key switch, six lit buttons, the VFD
    status window) in the charcoal fascia over the door."""
    W, D, H = CAB_W, CAB_D, 1.83
    cab_shell(kit, lv, W, D, H, seed=11)
    fy0, fy1 = H - TOP_T - 0.30, H - TOP_T - 0.002
    zf = cab_fascia(kit, lv, W, D, fy0, fy1)
    cab_door(kit, lv, W, D, PLINTH + GAP, fy0 - GAP, seed=12)
    pc = (0.0, (fy0 + fy1) / 2, zf + 0.0008)
    plate(kit, "PANEL_CPU", pc, lv)
    P = PANELS["PANEL_CPU"]
    zp = zf + 0.0016
    lit = [BTN_G, BTN_A, OFF_G, OFF_A, OFF_R, BTN_W]
    for k, s in enumerate(P["buttons"]):
        x, y, _ = site("PANEL_CPU", s, pc)
        if lv == 2:
            if lit[k] < 12:
                quad_part(kit, (x, y, zp + 0.003), "+z", P["bsize"], P["bsize"], ind(lit[k]), IND, name="button lens")
            continue
        button(kit, (x, y, zp), P["bsize"], lit[k], lv)
    x, y, _ = site("PANEL_CPU", P["key"], pc)
    key_switch(kit, (x, y, zp), lv) if lv < 2 else None
    u0, v0, u1, v1 = P["window"]
    wx, wy, _ = site("PANEL_CPU", ((u0 + u1) / 2, (v0 + v1) / 2), pc)
    quad_part(kit, (wx, wy, zp + 0.0006), "+z", (u1 - u0) * 0.56 * 0.92, (v1 - v0) * 0.24 * 0.84, ind(VFD), IND, name="vfd window")


def _drive_unit(kit, lv, centre, w, h, k):
    """A disk drive's faceplate behind the cabinet window: the printed face (one of the two DISK_FACE cells), its handle,
    the lamp buttons (READY lit green, FAULT dark, WRITE PROTECT dark) and an activity lamp."""
    x, y, z = centre
    cell = sub("DISK_FACE", 0.0, 0.5 if k % 2 == 0 else 0.0, 1.0, 1.0 if k % 2 == 0 else 0.5)
    dbox(kit, (w, h, 0.03), (x, y, z - 0.015), cell, lv, w=0.0012, name="drive face")
    if lv < 2:
        S.tube(kit, "drive handle", [(x + w * 0.16, y, z + 0.002), (x + w * 0.16, y, z + 0.018),
                                     (x - w * 0.12, y, z + 0.018), (x - w * 0.12, y, z + 0.002)],
               0.0045, CHROME, lv, cap0=True, cap1=True, lo=8)
        for j, idx in enumerate((BTN_G, OFF_R, OFF_A)):
            bx = x - w * (0.235 + 0.075 * j)
            dbox(kit, (0.018, 0.018, 0.006), (bx, y + h * 0.14, z + 0.003), ind(idx, full=True), lv, slot=IND, w=0.0008,
                 name="drive lamp", segs=1)
        led(kit, (x - w * 0.235, y - h * 0.18, z), LED_A if k != 1 else OFF_A, lv)


def cabinet_disk(kit, lv):
    """Kit_SystemCabinet_Disk, 1.83 m: three disk drives behind a smoked window in a charcoal-framed door."""
    W, D, H = CAB_W, CAB_D, 1.83
    cab_shell(kit, lv, W, D, H, seed=21)
    fy0, fy1 = H - TOP_T - 0.10, H - TOP_T - 0.002
    zf = cab_fascia(kit, lv, W, D, fy0, fy1)
    for j, idx in enumerate((LED_G, LED_A, OFF_R)):
        led(kit, (0.25 - 0.03 * j, (fy0 + fy1) / 2, zf), idx, lv)
    if lv < 2:
        box(kit, (0.16, 0.035, 0.0015), (-0.18, (fy0 + fy1) / 2, zf + 0.0007), BLACK, lv, w=0.0004, name="badge plate", segs=1)
    y0, y1 = PLINTH + GAP, fy0 - GAP
    wd = W - 2 * SIDE_T - 2 * GAP
    ow, oh = 0.60, 0.98
    wy = y1 - 0.06 - oh / 2
    zf = D / 2
    # the door: a putty frame round the window opening (kitlib frame, facing Unity +Z), a lower panel with the grille
    bw, bs = S.bev(0.003, lv)
    door_h = y1 - (wy - oh / 2 - 0.05)
    door_yc = y1 - door_h / 2
    kit.frame((wd, door_h), (ow, oh), DOOR_T, S.ub((0.0, door_yc, zf - DOOR_T / 2)), PUTTY,
              inner_offset=(0.0, wy - door_yc), bevel=bw, segments=bs, name="door frame")
    uvoff(box(kit, (wd, (wy - oh / 2 - 0.05) - y0, DOOR_T), (0.0, (y0 + wy - oh / 2 - 0.05) / 2, zf - DOOR_T / 2), PUTTY, lv, w=0.003,
              name="door lower"), 22)
    if lv < 2:
        box(kit, (wd - 0.10, 0.11, 0.0015), (0.0, y0 + 0.11, zf + 0.0007), PERF, lv, w=0.0004, name="intake grille", segs=1)
        kit.frame((ow + 0.024, oh + 0.024), (ow, oh), 0.006, S.ub((0.0, wy, zf + 0.002)), CHAR, bevel=S.bev(0.0015, lv)[0],
                  segments=S.bev(0.0015, lv)[1], name="window trim")
    box(kit, (ow + 0.01, oh + 0.01, 0.004), (0.0, wy, zf - 0.012), GLASS, lv, w=0.0008, name="window glass")
    flat(kit, (wd - 0.01, oh + 0.02, 0.004), (0.0, wy, zf - DOOR_T - 0.12), BLACK, name="interior")
    for k in range(3):
        dy = wy + oh / 2 - 0.20 - k * 0.30
        _drive_unit(kit, lv, (0.0, dy, zf - DOOR_T - 0.012), 0.56, 0.14, k)
        if lv < 2:
            box(kit, (0.58, 0.012, 0.10), (0.0, dy - 0.085, zf - DOOR_T - 0.06), CHAR, lv, w=0.001, name="drive rail")


def cabinet_comms(kit, lv):
    """Kit_SystemCabinet_Comms, 1.52 m: the communications controller: a fascia of line lamps (48 sites, most lit) over a
    door with a perforated field."""
    W, D, H = CAB_W, CAB_D, 1.52
    cab_shell(kit, lv, W, D, H, seed=31)
    fy0, fy1 = H - TOP_T - 0.18, H - TOP_T - 0.002
    zf = cab_fascia(kit, lv, W, D, fy0, fy1)
    pc = (0.0, (fy0 + fy1) / 2, zf + 0.0008)
    plate(kit, "PANEL_COMMS", pc, lv)
    rnd = random.Random(1989)
    P = PANELS["PANEL_COMMS"]
    zp = zf + 0.0016
    if lv == 2:
        quad_part(kit, (pc[0], pc[1], zp + 0.002), "+z", 0.56, 0.11, ind(VFD), IND, name="lamp field")
    else:
        for s in P["leds"]:
            r = rnd.random()
            idx = LED_G if r < 0.58 else (LED_A if r < 0.72 else OFF_G)
            x, y, _ = site("PANEL_COMMS", s, pc)
            led(kit, (x, y, zp), idx, lv, r=0.0026, bezel=False, segs=6)
    y0, y1 = PLINTH + GAP, fy0 - GAP
    cab_door(kit, lv, W, D, y0, y1, seed=32, grille=False, stripe=False)
    wd = W - 2 * SIDE_T - 2 * GAP
    zf = D / 2
    box(kit, (wd - 0.12, (y1 - y0) - 0.22, 0.0015), (0.0, (y0 + y1) / 2 - 0.02, zf + 0.0007), PERF, lv, w=0.0004, name="door field", segs=1)
    if lv < 2:
        kit.frame((wd - 0.10, (y1 - y0) - 0.20), (wd - 0.12, (y1 - y0) - 0.22), 0.004, S.ub((0.0, (y0 + y1) / 2 - 0.02, zf + 0.0015)),
                  CHAR, bevel=S.bev(0.001, lv)[0], segments=S.bev(0.001, lv)[1], name="field trim")


def _meter(kit, lv, centre, cell):
    """A 96 mm square panel meter: black bezel frame, its dial (atlas) 6 mm back, the zero screw."""
    x, y, z = centre
    bw, bs = S.bev(0.002, lv)
    kit.frame((0.096, 0.096), (0.082, 0.070), 0.018, S.ub((x, y, z + 0.009)), BLACK, inner_offset=(0.0, 0.006), bevel=bw,
              segments=bs, name="meter bezel")
    quad_part(kit, (x, y + 0.006, z + 0.006), "+z", 0.082, 0.070, sub(cell, 0.03, 0.12, 0.97, 0.94), name="meter dial")
    if lv < 2:
        flat(kit, (0.080, 0.068, 0.002), (x, y + 0.006, z + 0.0035), BLACK, name="meter back")
    if lv == 0:
        kit.cylinder(0.004, 0.003, S.ub((x, y - 0.036, z + 0.0195)), BLACK, verts=8, rot=(90, 0, 0), bevel=0.0, name="zero screw")


def cabinet_pdu(kit, lv):
    """Kit_SystemCabinet_PDU, 1.52 m: power distribution: three panel meters (volts, amps, kW), the pilot jewel and a key
    switch in the fascia; the breaker field (12 toggles) under a hinged clear cover on the door; an intake grille."""
    W, D, H = CAB_W, CAB_D, 1.52
    cab_shell(kit, lv, W, D, H, seed=41)
    fy0, fy1 = H - TOP_T - 0.20, H - TOP_T - 0.002
    zf = cab_fascia(kit, lv, W, D, fy0, fy1)
    fyc = (fy0 + fy1) / 2
    for k, cell in enumerate(("METER_V", "METER_A", "METER_K")):
        _meter(kit, lv, (0.20 - 0.115 * k, fyc, zf), cell)
    led(kit, (-0.17, fyc + 0.03, zf), JEWEL, lv, r=0.0085)
    if lv < 2:
        key_switch(kit, (-0.24, fyc - 0.02, zf), lv)
    y0, y1 = PLINTH + GAP, fy0 - GAP
    cab_door(kit, lv, W, D, y0, y1, seed=42, stripe=False)
    zd = D / 2
    pc = (0.0, y1 - 0.20, zd + 0.0008)
    plate(kit, "PANEL_PDU", pc, lv)
    P = PANELS["PANEL_PDU"]
    for s in P["breakers"]:
        x, y, _ = site("PANEL_PDU", s, pc)
        if lv < 2:
            flat(kit, (0.016, 0.050, 0.010), (x, y, zd + 0.006), BLACK, name="breaker body")
            box(kit, (0.008, 0.016, 0.016), (x, y + 0.010, zd + 0.016), CHAR if lv == 0 else BLACK, lv, w=0.0012, name="breaker toggle", segs=1)
    # the hinged clear cover over the breakers, on two hinge knuckles at its top and a pull tab
    cw, ch = 0.54, 0.205
    box(kit, (cw, ch, 0.003), (0.0, pc[1] - 0.005, zd + 0.034), GLASS, lv, w=0.0008, name="breaker cover")
    if lv < 2:
        for sx in (-1, 1):
            S.cyl_x(kit, 0.0055, 0.05, (sx * 0.20, pc[1] - 0.005 + ch / 2 + 0.004, zd + 0.030), CHROME, lv, w=0.0006, name="hinge")
            box(kit, (0.012, 0.012, 0.030), (sx * (cw / 2 + 0.008), pc[1] - 0.005, zd + 0.016), CHAR, lv, w=0.001, name="cover stop")
        box(kit, (0.05, 0.012, 0.008), (0.0, pc[1] - 0.005 - ch / 2 - 0.004, zd + 0.034), CHAR, lv, w=0.0015, name="cover tab")


def cabinet_diskpack(kit, lv):
    """Kit_SystemCabinet_DiskPack, 0.95 m: a second-hand top-loading removable-pack drive (62 3.1): a rounded steel body,
    a hinged lid with a smoked window over the pack (eleven 14 in platters on the spindle), the control panel with three big
    lit buttons, an intake grille, the charcoal plinth."""
    W, D, H = 0.76, 0.76, 0.95
    box(kit, (W - 0.04, PLINTH, D - 0.04), (0.0, PLINTH / 2, 0.0), CHAR, lv, w=0.002, name="plinth")
    hb = H - PLINTH - 0.05
    yt = PLINTH + hb
    yw = yt - 0.19
    uvoff(box(kit, (W, yw - PLINTH, D), (0.0, (PLINTH + yw) / 2, 0.0), STEEL, lv, w=0.012 if lv == 0 else 0.006, name="body"), 51)
    for sx in (-1, 1):
        uvoff(box(kit, (0.03, yt - yw, D), (sx * (W / 2 - 0.015), (yw + yt) / 2, 0.0), STEEL, lv, w=0.008 if lv == 0 else 0.004, name="body side"), 52 + sx)
    for sz in (-1, 1):
        uvoff(box(kit, (W - 0.06, yt - yw, 0.03), (0.0, (yw + yt) / 2, sz * (D / 2 - 0.015)), STEEL, lv, w=0.008 if lv == 0 else 0.004, name="body end"), 55 + sz)
    lw, ld = 0.56, 0.56
    # the top deck in charcoal round the well, the lid frame (putty) and its window over it
    bwd, bsd = S.bev(0.004, lv)
    kit.frame((W - 0.02, D - 0.02), (lw, ld), 0.02, S.ub((0.0, yt + 0.01, 0.0)), CHAR, inner_offset=(0.0, -0.02), bevel=bwd, segments=bsd,
              rot=(90, 0, 0), name="top deck")
    bw, bs = S.bev(0.004, lv)
    o = kit.frame((lw + 0.06, ld + 0.06), (lw, ld), 0.03, S.ub((0.0, yt + 0.035, -0.02)), PUTTY, bevel=bw, segments=bs,
                  rot=(90, 0, 0), name="lid frame")
    box(kit, (lw + 0.004, 0.004, ld + 0.004), (0.0, yt + 0.044, -0.02), GLASS, lv, w=0.0008, name="lid window")
    if lv < 2:
        S.cyl_x(kit, 0.008, 0.50, (0.0, yt + 0.03, -0.02 - ld / 2 - 0.035), CHROME, lv, w=0.0008, name="lid hinge")
        box(kit, (0.12, 0.014, 0.02), (0.0, yt + 0.045, -0.02 + ld / 2 + 0.04), CHAR, lv, w=0.002, name="lid handle")
    # the pack inside the well: platters (oxide brown) on a chrome spindle, seen through the window
    pr = 0.178
    nplat = 6 if lv == 0 else (3 if lv == 1 else 0)
    for k in range(nplat):
        yk = yt - 0.03 - k * (0.13 / max(nplat - 1, 1))
        kit.cylinder(pr, 0.002, S.ub((0.0, yk, -0.02)), ALU, verts=40 if lv == 0 else 20, bevel=0.0, name="platter")
    kit.cylinder(0.06, 0.14, S.ub((0.0, yt - 0.10, -0.02)), BLACK, verts=16 if lv < 2 else 8, bevel=0.0, name="pack core")
    if lv == 2:
        kit.cylinder(pr, 0.004, S.ub((0.0, yt - 0.03, -0.02)), ALU, verts=16, bevel=0.0, name="platter")
    if lv < 2:
        disc_part(kit, (0.0, yt - 0.028, -0.02), pr - 0.002, "+y", colour("oxide"), 48 if lv == 0 else 24, name="top platter")
        S.cyl_y(kit, 0.05, 0.03, (0.0, yt - 0.012, -0.02), CHROME, lv, w=0.002, name="pack hub")
        S.cyl_y(kit, 0.018, 0.04, (0.0, yt - 0.004, -0.02), BLACK, lv, w=0.002, name="pack knob")
    flat(kit, (lw + 0.02, 0.004, ld + 0.02), (0.0, yt - 0.17, -0.02), BLACK, name="well floor")
    # front: the panel near the top, three big lit buttons, the grille low
    zf = D / 2
    pc = (0.0, yt - 0.11, zf + 0.0008)
    plate(kit, "PANEL_DISKPACK", pc, lv)
    P = PANELS["PANEL_DISKPACK"]
    for s, idx in zip(P["buttons"], (BTN_G, BTN_A, OFF_R)):
        x, y, _ = site("PANEL_DISKPACK", s, pc)
        if lv == 2:
            if idx < 12:
                quad_part(kit, (x, y, zf + 0.004), "+z", P["bsize"], P["bsize"], ind(idx), IND, name="button lens")
            continue
        button(kit, (x, y, zf + 0.0016), P["bsize"], idx, lv)
    if lv < 2:
        box(kit, (W - 0.16, 0.20, 0.0015), (0.0, PLINTH + 0.16, zf + 0.0007), PERF, lv, w=0.0004, name="intake grille", segs=1)


# ============================================================================ sidecar helpers
# Unity body boxes (centre, size) for the kits that keep a collider (62 2.3: one box per row is merged by the builder).
BODIES = {
    "Kit_SystemCabinet_CPU": ((0.0, 0.915, 0.0), (0.76, 1.83, 0.86)),
    "Kit_SystemCabinet_Disk": ((0.0, 0.915, 0.0), (0.76, 1.83, 0.86)),
    "Kit_SystemCabinet_Comms": ((0.0, 0.76, 0.0), (0.76, 1.52, 0.86)),
    "Kit_SystemCabinet_PDU": ((0.0, 0.76, 0.0), (0.76, 1.52, 0.86)),
    "Kit_SystemCabinet_DiskPack": ((0.0, 0.475, 0.0), (0.76, 0.95, 0.76)),
}


def collider_unity(kit, centre, size):
    """kitlib's collider takes Blender asset space: convert a Unity box."""
    kit.collider(S.ub(centre), (size[0], size[2], size[1]))


def cabinet_collider(kit, name):
    c, s = BODIES[name]
    collider_unity(kit, c, s)


def meta(kit, service=None):
    kit.meta["structures"] = "structures_assets G3-a1 2026-11-10 (30_plan.md P16 structures_l0_props, the SP3 computer room)"
    if service is not None:
        kit.meta["service"] = service


# ============================================================================ the 9-track tape unit (HERO) and its reels
TD_W, TD_D, TD_H = 0.76, 0.75, 1.69              # S1: IBM 3420-class, 29 7/8 w x 29 5/8 d x 66 3/8 h in
REEL_R, FLANGE_T, FLANGE_GAP = 0.13335, 0.0016, 0.0135   # 10.5 in reel, 1/2 in tape between flanges
HUB_R = 0.070                                    # the reel's winding hub
REEL_Y, REEL_X = 1.215, 0.17                     # reel centres on the deck (file reel at +x = the viewer's left)
DECK_Z = TD_D / 2 - 0.075                        # the tape deck's front face
REEL_Z0 = DECK_Z + 0.004                         # the back flange's back face (the reel kits' origin plane)
PACK_FILE, PACK_MACHINE = 0.116, 0.091           # pack radii now (file reel partly read)
SPOKES = (90.0, 210.0, 330.0)
SPOKE_HALF = 16.0


def _flange(m, z0, t, lv, window=True, cell="reel_smoke"):
    """One reel flange (faces +Z and -Z at z0 + t and z0): an inner ring, three spokes and the rim around three windows.
    Polar polygons in the reel's own frame (centre on the axis)."""
    n = {0: 48, 1: 24, 2: 12}[lv]
    r_hole, r_in, r_win_out, r_rim = 0.0381, 0.074, 0.1255, REEL_R
    cr = colour(cell)

    def pt(r, a, z):
        return (r * math.cos(math.radians(a)), r * math.sin(math.radians(a)), z)

    def uvs(pts):
        return [(cr[0] + (cr[2] - cr[0]) * (0.5 + q[0] / (2 * REEL_R)), cr[1] + (cr[3] - cr[1]) * (0.5 + q[1] / (2 * REEL_R) + 0.1 * q[2]))
                for q in pts]

    def ring(r0, r1, a0, a1, steps):
        for k in range(steps):
            b0, b1 = a0 + (a1 - a0) * k / steps, a0 + (a1 - a0) * (k + 1) / steps
            for z, out in ((z0 + t, (0, 0, 1)), (z0, (0, 0, -1))):
                q = [pt(r0, b0, z), pt(r1, b0, z), pt(r1, b1, z), pt(r0, b1, z)]
                _oriented(m, q, uvs(q), out)

    def wall(r, a0, a1, steps, outward):
        for k in range(steps):
            b0, b1 = a0 + (a1 - a0) * k / steps, a0 + (a1 - a0) * (k + 1) / steps
            mid = math.radians((b0 + b1) / 2)
            out = (math.cos(mid) * outward, math.sin(mid) * outward, 0)
            q = [pt(r, b0, z0), pt(r, b1, z0), pt(r, b1, z0 + t), pt(r, b0, z0 + t)]
            _oriented(m, q, uvs(q), out)

    if not window or lv == 2:
        ring(r_hole, r_rim, 0, 360, n)
        wall(r_rim, 0, 360, n, 1)
        return
    ring(r_hole, r_in, 0, 360, n)
    ring(r_win_out, r_rim, 0, 360, n)
    wall(r_rim, 0, 360, n, 1)
    wall(r_hole, 0, 360, max(8, n // 2), -1)
    per = max(2, n // 12)
    for sa in SPOKES:
        ring(r_in, r_win_out, sa - SPOKE_HALF, sa + SPOKE_HALF, per)
    # window walls: the inner arc, the outer arc and the two radial edges of each window
    for i, sa in enumerate(SPOKES):
        a0, a1 = sa + SPOKE_HALF, SPOKES[(i + 1) % 3] - SPOKE_HALF + (360 if i == 2 else 0)
        steps = max(3, int(n * (a1 - a0) / 360))
        wall(r_in, a0, a1, steps, 1)
        wall(r_win_out, a0, a1, steps, -1)
        for a, sgn in ((a0, 1), (a1, -1)):
            ar = math.radians(a)
            tang = (-math.sin(ar) * sgn, math.cos(ar) * sgn, 0)
            q = [pt(r_in, a, z0), pt(r_win_out, a, z0), pt(r_win_out, a, z0 + t), pt(r_in, a, z0 + t)]
            _oriented(m, q, uvs(q), tang)


def reel(kit, lv, pack_r, label=True, ring_red=True, cell="reel_smoke"):
    """A 10.5 in reel of 1/2 in tape (S2) in its own frame: the back flange's back face at z 0, front +Z, the axis at the
    origin. Two windowed flanges in smoky tinted plastic, the wound pack between them (its side face from the atlas's PACK
    cell: the wound tape's edge; its rim the tape's back coat), the winding hub, the drive hub's latch knob through the
    centre (it turns with the reel), the file reel's label on a spoke and its write-enable ring at the back."""
    m = S.Mesh()
    z_back0 = 0.0
    z_front0 = FLANGE_T + FLANGE_GAP
    _flange(m, z_back0, FLANGE_T, lv, cell=cell)
    _flange(m, z_front0, FLANGE_T, lv, cell=cell)
    m.part(kit, "flanges", ATLAS, merge=False)
    n = {0: 48, 1: 24, 2: 12}[lv]
    zp0, zp1 = FLANGE_T + 0.0004, z_front0 - 0.0004
    # the pack: its front and back faces (PACK cell, the reel face's radius maps the cell's square), its rim
    disc_part(kit, (0, 0, zp1), pack_r, "+z", rect("PACK", 2.0), n, r_in=HUB_R - 0.002, r_map=REEL_R, name="pack face")
    if lv < 2:
        disc_part(kit, (0, 0, zp0), pack_r, "-z", rect("PACK", 2.0), n // 2, r_in=HUB_R - 0.002, r_map=REEL_R, name="pack back")
    mr = S.Mesh()
    tb = rect("TAPE_BACK", 3.0)
    for k in range(n):
        a0, a1 = 2 * math.pi * k / n, 2 * math.pi * (k + 1) / n
        p = lambda a, z: (pack_r * math.cos(a), pack_r * math.sin(a), z)
        u0 = tb[0] + (tb[2] - tb[0]) * (k / n)
        u1 = tb[0] + (tb[2] - tb[0]) * ((k + 1) / n)
        _oriented(mr, [p(a0, zp0), p(a1, zp0), p(a1, zp1), p(a0, zp1)], [(u0, tb[1]), (u1, tb[1]), (u1, tb[3]), (u0, tb[3])],
                  (math.cos((a0 + a1) / 2), math.sin((a0 + a1) / 2), 0))
    mr.part(kit, "pack rim", ATLAS)
    # the winding hub between the flanges and the drive hub's knob in front
    kit.cylinder(HUB_R - 0.002, FLANGE_GAP - 0.0008, S.ub((0, 0, (zp0 + zp1) / 2)), ATLAS, verts=max(12, n // 2), rot=(90, 0, 0),
                 bevel=0.0, name="winding hub")["fr_uv"] = "decal"
    kit.parts[-1]["fr_uv_rect"] = list(colour("hub_dark"))
    zf = z_front0 + FLANGE_T
    kit.cylinder(0.0365, 0.010, S.ub((0, 0, zf + 0.005)), ALU, verts=24 if lv == 0 else 12, rot=(90, 0, 0),
                 bevel=0.001 if lv < 2 else 0.0, segments=1, name="hub face")
    if lv < 2:
        kit.cylinder(0.019, 0.024, S.ub((0, 0, zf + 0.022)), BLACK, verts=16 if lv == 0 else 8, rot=(90, 0, 0),
                     bevel=0.0012 if lv == 0 else 0.0, segments=1, name="hub knob")
        box(kit, (0.052, 0.011, 0.008), (0, 0, zf + 0.037), CHROME, lv, w=0.0015, name="hub lever")
    if label and lv < 2:
        a = math.radians(SPOKES[0])
        rl = 0.100
        dbox(kit, (0.030, 0.042, 0.0004), (rl * math.cos(a), rl * math.sin(a), zf + 0.0003), rect("REEL_LABEL", 2.0), lv, w=0.0,
             name="reel label")
    if ring_red and lv < 2:
        disc_part(kit, (0, 0, -0.0012), 0.047, "-z", colour("ring_red"), 24, r_in=0.040, name="write ring")
        kit.cylinder(0.047, 0.0024, S.ub((0, 0, -0.0006)), ATLAS, verts=24, rot=(90, 0, 0), bevel=0.0, name="write ring body")
        kit.parts[-1]["fr_uv"] = "decal"
        kit.parts[-1]["fr_uv_rect"] = list(colour("ring_red"))


def _tangent_point(c, r, p, side):
    """The point where a line from p touches the circle (c, r); side +1 / -1 picks which of the two."""
    cx, cy = c
    dx, dy = p[0] - cx, p[1] - cy
    d = math.hypot(dx, dy)
    a = math.atan2(dy, dx)
    b = math.acos(min(1.0, r / d))
    t = a + side * b
    return (cx + r * math.cos(t), cy + r * math.sin(t))


def _ribbon(m, pts, z0, z1, lv):
    """A tape ribbon through deck points (x, y): width along Z from z0 to z1, oxide on the outer face, back coat inside."""
    ox, bk = rect("TAPE", 3.0), rect("TAPE_BACK", 3.0)
    run = 0.0
    for i in range(len(pts) - 1):
        (x0, y0), (x1, y1) = pts[i], pts[i + 1]
        L = math.hypot(x1 - x0, y1 - y0)
        if L < 1e-6:
            continue
        nx, ny = (y1 - y0) / L, -(x1 - x0) / L
        u0 = ox[0] + (ox[2] - ox[0]) * ((run * 4) % 1.0)
        u1 = min(ox[2], u0 + (ox[2] - ox[0]) * (L * 4))
        quad = [(x0, y0, z0), (x1, y1, z0), (x1, y1, z1), (x0, y0, z1)]
        _oriented(m, quad, [(u0, ox[1]), (u1, ox[1]), (u1, ox[3]), (u0, ox[3])], (nx, ny, 0))
        if lv < 2:
            _oriented(m, list(reversed(quad)), [(u0, bk[3]), (u0, bk[1]), (u1, bk[1]), (u1, bk[3])], (-nx, -ny, 0))
        run += L


def _u_bend(cx, cy, r, steps, up=True):
    """Points of a U loop's bottom (a semicircle under (cx, cy)) from the right leg to the left leg."""
    return [(cx + r * math.cos(math.pi * k / steps + math.pi), cy + r * math.sin(math.pi * k / steps + math.pi)) for k in range(steps + 1)]


def tape_drive(kit, lv, with_reels=False):
    """Kit_TapeDrive_Reel: a vacuum-column 9-track unit of the 3420 class (S1). The operator strip of lit push-buttons on
    top; a glass door over the charcoal tape deck: two reel hubs (the reels are their own kits at the sidecar anchors so the
    builder can turn them), the head assembly with its cover, two chrome guide rollers, the rubber capstan, two tapered
    vacuum columns with the tape's loops; the lower door with its grille and pull. with_reels bakes both reels in (the
    static kit, one renderer)."""
    W, D, H = TD_W, TD_D, TD_H
    cab_shell(kit, lv, W, D, H, seed=61)
    zf = D / 2
    wd = W - 2 * SIDE_T - 2 * GAP
    fy0, fy1 = H - TOP_T - 0.17, H - TOP_T - 0.002
    zfa = cab_fascia(kit, lv, W, D, fy0, fy1)
    pc = (0.0, (fy0 + fy1) / 2, zfa + 0.0008)
    plate(kit, "PANEL_TAPE", pc, lv)
    P = PANELS["PANEL_TAPE"]
    lit = [BTN_W, BTN_G, OFF_A, BTN_A, OFF_R, OFF_G]
    for k, s in enumerate(P["buttons"]):
        x, y, _ = site("PANEL_TAPE", s, pc)
        if lv == 2:
            if lit[k] < 12:
                quad_part(kit, (x, y, zfa + 0.005), "+z", P["bsize"], P["bsize"], ind(lit[k]), IND, name="button lens")
            continue
        button(kit, (x, y, zfa + 0.0016), P["bsize"], lit[k], lv)
    # the glass door: a slim charcoal frame, the pane, the chrome pull on the viewer's right
    gy0, gy1 = 0.70, fy0 - GAP
    gh = gy1 - gy0
    gyc = (gy0 + gy1) / 2
    bw, bs = S.bev(0.003, lv)
    kit.frame((wd, gh), (wd - 0.07, gh - 0.07), 0.030, S.ub((0.0, gyc, zf - 0.015)), CHAR, bevel=bw, segments=bs, name="door frame")
    box(kit, (wd - 0.06, gh - 0.06, 0.005), (0.0, gyc, zf - 0.010), GLASS, lv, w=0.0008, name="door glass")
    if lv < 2:
        box(kit, (0.012, 0.16, 0.022), (-(wd / 2 - 0.016), gyc, zf + 0.011), CHROME, lv, w=0.002, name="door pull")
    # the lower door
    cab_door(kit, lv, W, D, PLINTH + GAP, gy0 - GAP, seed=62, stripe=False)
    # behind the glass: the deck, its surround, the reel hubs
    dy0, dy1 = gy0 + 0.035, gy1 - 0.035
    flat(kit, (wd - 0.07, dy1 - dy0, 0.012), (0.0, (dy0 + dy1) / 2, DECK_Z - 0.006), "Painted_Metal", name="deck")
    if lv < 2:
        for sx in (-1, 1):
            flat(kit, (0.004, dy1 - dy0, 0.075), (sx * (wd / 2 - 0.037), (dy0 + dy1) / 2, DECK_Z + 0.0375), CHAR, name="deck cheek")
        flat(kit, (wd - 0.07, 0.004, 0.075), (0.0, dy1 - 0.002, DECK_Z + 0.0375), CHAR, name="deck head")
        flat(kit, (wd - 0.07, 0.004, 0.075), (0.0, dy0 + 0.002, DECK_Z + 0.0375), CHAR, name="deck sill")
    for sx in (-1, 1):
        kit.cylinder(0.05, 0.004, S.ub((sx * REEL_X, REEL_Y, DECK_Z + 0.002)), ALU, verts=24 if lv == 0 else 12, rot=(90, 0, 0),
                     bevel=0.0, name="hub flange")
    # the vacuum columns: channel walls (satin aluminium), the dark back, the glass is the door's
    col_w, cy0, cy1 = 0.080, 0.775, 1.060
    zt0, zt1 = DECK_Z + 0.004 + FLANGE_T + 0.0004, DECK_Z + 0.004 + FLANGE_T + FLANGE_GAP - 0.0004
    for sx in (-1, 1):
        cx = sx * REEL_X
        flat(kit, (col_w + 0.012, cy1 - cy0, 0.004), (cx, (cy0 + cy1) / 2, DECK_Z + 0.002), "Prop_PlasticWhite", name="column back")
        for wx in (-1, 1):
            box(kit, (0.006, cy1 - cy0, 0.020), (cx + wx * (col_w / 2 + 0.003), (cy0 + cy1) / 2, DECK_Z + 0.010), ALU, lv,
                w=0.0008, name="column wall", segs=1)
        box(kit, (col_w + 0.012, 0.006, 0.020), (cx, cy0 - 0.003, DECK_Z + 0.010), ALU, lv, w=0.0008, name="column floor", segs=1)
        if lv < 2:
            kit.cylinder(0.0045, 0.020, S.ub((cx, cy0 + 0.04, DECK_Z + 0.010)), BLACK, verts=8, rot=(90, 0, 0), bevel=0.0, name="vacuum port")
    # the head assembly between the columns' tops: the head cover, the head, guides, the capstan, the cleaner
    hy = 1.088
    box(kit, (0.11, 0.050, 0.040), (0.0, hy + 0.035, DECK_Z + 0.020), ALU, lv, w=0.003, name="head cover")
    if lv < 2:
        box(kit, (0.034, 0.016, 0.022), (0.0, hy - 0.004, DECK_Z + 0.011), BLACK, lv, w=0.001, name="head", segs=1)
        box(kit, (0.12, 0.012, 0.030), (0.0, hy - 0.024, DECK_Z + 0.015), CHAR, lv, w=0.0015, name="head plate")
    guides = [(0.118, hy), (-0.118, hy)]
    for gx, gy in guides:
        kit.cylinder(0.012, 0.020, S.ub((gx, gy, DECK_Z + 0.010)), CHROME, verts=20 if lv == 0 else 10, rot=(90, 0, 0),
                     bevel=0.0008 if lv < 2 else 0.0, segments=1, name="guide roller")
        if lv < 2:
            kit.cylinder(0.016, 0.002, S.ub((gx, gy, DECK_Z + 0.021)), CHROME, verts=20 if lv == 0 else 10, rot=(90, 0, 0),
                         bevel=0.0, name="guide flange")
    kit.cylinder(0.024, 0.018, S.ub((-0.062, hy + 0.012, DECK_Z + 0.010)), RUBBER, verts=24 if lv == 0 else 12, rot=(90, 0, 0),
                 bevel=0.0012 if lv < 2 else 0.0, segments=1, name="capstan")
    if lv < 2:
        kit.cylinder(0.008, 0.024, S.ub((-0.062, hy + 0.012, DECK_Z + 0.012)), CHROME, verts=12 if lv == 0 else 8, rot=(90, 0, 0),
                     bevel=0.0, name="capstan hub")
        box(kit, (0.018, 0.020, 0.026), (0.060, hy - 0.002, DECK_Z + 0.013), BLACK, lv, w=0.0012, name="cleaner", segs=1)
        box(kit, (0.016, 0.014, 0.022), (0.040, hy + 0.062, DECK_Z + 0.011), BLACK, lv, w=0.001, name="tape sensor", segs=1)
    # the tape: file pack -> column outer leg -> loop -> inner leg -> guide -> head -> guide -> machine column -> machine pack
    zt = (zt0, zt1)
    leg_o, leg_i = REEL_X + col_w / 2 - 0.004, REEL_X - col_w / 2 + 0.004
    rb = (leg_o - leg_i) / 2
    steps = 8 if lv == 0 else (4 if lv == 1 else 2)
    m = S.Mesh()
    yt = hy - 0.012
    for sx, pack, loop_y in ((1, PACK_FILE, 0.905), (-1, PACK_MACHINE, 0.945)):
        xo, xi = sx * leg_o, sx * leg_i
        top = (xo, cy1 - 0.006)
        tp = _tangent_point((sx * REEL_X, REEL_Y), pack, top, sx)
        bend = _u_bend(sx * REEL_X, loop_y, rb, steps)      # left to right through the bottom
        if sx > 0:
            bend = list(reversed(bend))                      # outer (right) leg to inner (left) leg
        path = [tp, top] + bend + [(xi, yt), (sx * 0.118, yt)]
        _ribbon(m, path, zt[0], zt[1], lv)
    _ribbon(m, [(0.118, yt), (-0.118, yt)], zt[0], zt[1], lv)
    m.part(kit, "tape", ATLAS)
    kit.anchor("reelFile", S.ub((REEL_X, REEL_Y, REEL_Z0)))
    kit.anchor("reelMachine", S.ub((-REEL_X, REEL_Y, REEL_Z0)))
    if with_reels:
        n0 = len(kit.parts)
        reel(kit, lv, PACK_FILE)
        _shift(kit.parts[n0:], (REEL_X, REEL_Y, REEL_Z0))
        n0 = len(kit.parts)
        reel(kit, lv, PACK_MACHINE, label=False, ring_red=False, cell="belt_grey")
        _shift(kit.parts[n0:], (-REEL_X, REEL_Y, REEL_Z0), spin=25.0)


def _shift(parts, offset, spin=0.0):
    """Move parts built in a sub-kit frame to a Unity offset (optionally spun about Unity Z by `spin` degrees)."""
    from mathutils import Matrix
    off = Vector(S.ub(offset))
    rot = Matrix.Rotation(math.radians(-spin), 4, "Y")
    for p in parts:
        # matrix_basis, not matrix_world: a part made this frame has a stale world matrix until the depsgraph updates
        p.matrix_basis = Matrix.Translation(off) @ rot @ p.matrix_basis


# ============================================================================ Kit_TapeDrive_Cartridge (3480 class)
def tape_cartridge(kit, lv):
    """Kit_TapeDrive_Cartridge: the cartridge unit of the same cabinet (LS86: 3480 cartridges, 4 x 5 x 1 in, 1984): two
    drives side by side, each with a six-cartridge loader behind a smoked window (the cartridges' label ends showing), the
    feed slot and its lamps; the operator strip with a lit display; a mid band; the lower door."""
    W, D, H = TD_W, TD_D, TD_H
    cab_shell(kit, lv, W, D, H, seed=71)
    zf = D / 2
    wd = W - 2 * SIDE_T - 2 * GAP
    fy0, fy1 = H - TOP_T - 0.17, H - TOP_T - 0.002
    zfa = cab_fascia(kit, lv, W, D, fy0, fy1)
    pc = (-0.15, (fy0 + fy1) / 2, zfa + 0.0008)
    plate(kit, "PANEL_CART", pc, lv)
    P = PANELS["PANEL_CART"]
    u0, v0, u1, v1 = P["window"]
    wx, wy, _ = site("PANEL_CART", ((u0 + u1) / 2, (v0 + v1) / 2), pc)
    quad_part(kit, (wx, wy, zfa + 0.0024), "+z", (u1 - u0) * 0.30 * 0.9, (v1 - v0) * 0.15 * 0.8, ind(VFD), IND, name="display")
    for k, s in enumerate(P["buttons"]):
        x, y, _ = site("PANEL_CART", s, pc)
        idx = (BTN_G, OFF_A, BTN_W, OFF_R)[k]
        if lv == 2:
            continue
        button(kit, (x, y, zfa + 0.0016), P["bsize"], idx, lv)
    if lv < 2:
        box(kit, (0.20, 0.04, 0.0015), (0.20, (fy0 + fy1) / 2, zfa + 0.0007), BLACK, lv, w=0.0004, name="badge plate", segs=1)
    # the loader bays
    by0, by1 = 0.96, fy0 - GAP
    wy = by1 - 0.20
    ow, oh = 0.20, 0.24
    y_lo, y_hi = wy - oh / 2, wy + oh / 2
    pieces = [((wd, by1 - y_hi), (0.0, (y_hi + by1) / 2)), ((wd, y_lo - by0), (0.0, (by0 + y_lo) / 2)),
              ((wd / 2 - 0.175 - ow / 2, oh), (wd / 2 - (wd / 2 - 0.175 - ow / 2) / 2, wy)),
              ((wd / 2 - 0.175 - ow / 2, oh), (-(wd / 2 - (wd / 2 - 0.175 - ow / 2) / 2), wy)),
              ((2 * (0.175 - ow / 2), oh), (0.0, wy))]
    for k, ((pw_, ph_), (px_, py_)) in enumerate(pieces):
        uvoff(box(kit, (pw_, ph_, DOOR_T), (px_, py_, zf - DOOR_T / 2), PUTTY, lv, w=0.003, name="bay panel"), 72 + k)
    for k, bx in enumerate((0.175, -0.175)):
        bw, bs = S.bev(0.002, lv)
        kit.frame((0.26, 0.30), (0.20, 0.24), 0.012, S.ub((bx, wy, zf + 0.006)), CHAR, bevel=bw, segments=bs, name="loader frame")
        box(kit, (0.205, 0.245, 0.004), (bx, wy, zf + 0.002), GLASS, lv, w=0.0006, name="loader window")
        flat(kit, (0.20, 0.24, 0.004), (bx, wy, zf - 0.17), BLACK, name="loader back")
        ncart = 6 if lv < 2 else 1
        for c in range(ncart):
            cy = wy - 0.09 + c * 0.0262 if lv < 2 else wy - 0.02
            ch = 0.0245 if lv < 2 else 0.16
            box(kit, (0.102, ch, 0.127), (bx, cy, zf - 0.075), CHAR, lv, w=0.001, name="cartridge", segs=1)
            if lv < 2:
                q = (c + k) % 4
                quad_part(kit, (bx, cy, zf - 0.075 + 0.0636), "+z", 0.096, 0.019, sub("CART_LABEL", q / 4.0, 0.0, (q + 1) / 4.0, 1.0),
                          name="cartridge label")
        sy = wy - 0.20
        flat(kit, (0.13, 0.034, 0.004), (bx, sy, zf + 0.0005), BLACK, name="feed slot")
        if lv < 2:
            box(kit, (0.15, 0.05, 0.004), (bx, sy, zf + 0.0002), CHAR, lv, w=0.001, name="slot bezel", segs=1)
        led(kit, (bx - 0.09, sy, zf), LED_G if k == 0 else OFF_G, lv)
        led(kit, (bx - 0.105, sy, zf), LED_A if k == 1 else OFF_A, lv)
    band_y = by0 - 0.03
    box(kit, (wd, 0.06, DOOR_T + 0.004), (0.0, band_y, zf - DOOR_T / 2 + 0.002), CHAR, lv, w=0.002, name="mid band")
    cab_door(kit, lv, W, D, PLINTH + GAP, band_y - 0.03 - GAP, seed=73, stripe=False)


# ============================================================================ Kit_LinePrinter (B-600 class acoustic cabinet)
LP_W, LP_D, LP_H = 0.87, 0.755, 1.13          # S3


def _paper_stack(kit, lv, centre, w, d, h, name="paper stack"):
    """A fanfold stack: green-bar top sheet, sheet edges on the sides (atlas), the folds front and back."""
    x, y, z = centre
    dbox(kit, (w, h, d), (x, y, z), rect("GREENBAR_EDGE"), lv, w=0.0008, facing="+z", name=name)
    quad_part(kit, (x, y + h / 2 + 0.0005, z), "+y", w, d, rect("GREENBAR"), name=name + " top")


def line_printer(kit, lv):
    """Kit_LinePrinter: a band line printer in its acoustic cabinet (S3, Dataproducts B-600 class: 87 w x 75.5 d x 113 h cm,
    warm tone tan and brown). The lid over the print station with its window (the band, the ribbon cartridge and the paper
    seen inside), the control panel with lit keys and the two-digit display, the bottom paper-entry door, the rear doors with
    the stacker window over the green-bar output, the brown plinth and lid frame."""
    W, D, H = LP_W, LP_D, LP_H
    zf = D / 2
    box(kit, (W - 0.03, 0.07, D - 0.03), (0.0, 0.035, 0.0), BROWN, lv, w=0.003, name="plinth")
    y0, y1 = 0.07, 0.86
    for sx in (-1, 1):
        uvoff(box(kit, (0.02, y1 - y0, D), (sx * (W / 2 - 0.01), (y0 + y1) / 2, 0.0), STEEL, lv, w=0.004, name="side"), 80 + sx)
        if lv < 2:
            box(kit, (0.0015, 0.10, 0.40), (sx * (W / 2 + 0.0008), y1 - 0.12, 0.0), PERF, lv, w=0.0004, name="side louvre", segs=1)
    # front: the paper-entry door below, the fascia with the control panel above it
    wd = W - 0.04 - 0.006
    dy1 = 0.70
    uvoff(box(kit, (wd, dy1 - y0 - 0.006, 0.03), (0.0, (y0 + dy1) / 2, zf - 0.015), PUTTY, lv, w=0.003, name="paper door"), 82)
    if lv < 2:
        box(kit, (wd - 0.10, 0.09, 0.0015), (0.0, y0 + 0.09, zf + 0.0007), PERF, lv, w=0.0004, name="door grille", segs=1)
        box(kit, (0.20, 0.022, 0.0015), (0.0, dy1 - 0.06, zf + 0.0007), BLACK, lv, w=0.0004, name="door pull recess", segs=1)
        box(kit, (0.18, 0.008, 0.010), (0.0, dy1 - 0.068, zf + 0.005), BROWN, lv, w=0.0015, name="door pull")
    fasy0, fasy1 = dy1, y1
    box(kit, (W - 0.04, fasy1 - fasy0, 0.034), (0.0, (fasy0 + fasy1) / 2, zf - 0.017 + 0.002), BROWN, lv, w=0.003, name="fascia")
    pc = (-0.22, (fasy0 + fasy1) / 2, zf + 0.0028)
    plate(kit, "PANEL_PRINTER", pc, lv)
    P = PANELS["PANEL_PRINTER"]
    for k, s in enumerate(P["buttons"]):
        x, y, _ = site("PANEL_PRINTER", s, pc)
        if lv < 2:
            dbox(kit, (P["bsize"], P["bsize"] * 0.8, 0.006), (x, y, zf + 0.0066), ind((OFF_C, BTN_G, OFF_C, OFF_C)[k], full=True), lv,
                 slot=IND, w=0.0008, name="key", segs=1)
    for k, s in enumerate(P["leds"]):
        x, y, _ = site("PANEL_PRINTER", s, pc)
        led(kit, (x, y, zf + 0.0036), (LED_G, LED_A)[k], lv)
    u0, v0, u1, v1 = P["window"]
    wx, wy, _ = site("PANEL_PRINTER", ((u0 + u1) / 2, (v0 + v1) / 2), pc)
    quad_part(kit, (wx, wy, zf + 0.0042), "+z", (u1 - u0) * 0.30 * 0.9, (v1 - v0) * 0.15 * 0.8, ind(DIGITS), IND, name="digits")
    # the lid: a brown frame, the tan lid, its window over the print station; inside: the paper, the ribbon cartridge
    ly0 = y1
    lh = H - ly0
    ld = D * 0.62
    lz = zf - ld / 2
    win_w, win_h = 0.58, 0.13
    wyc = ly0 + lh * 0.55
    bw, bs = S.bev(0.004, lv)
    kit.frame((W, lh), (win_w, win_h), 0.03, S.ub((0.0, ly0 + lh / 2, zf - 0.015)), PUTTY, inner_offset=(0.0, wyc - (ly0 + lh / 2)),
              bevel=bw, segments=bs, name="lid front")
    uvoff(box(kit, (W, 0.03, ld - 0.03), (0.0, H - 0.015, lz - 0.015), PUTTY, lv, w=0.008 if lv == 0 else 0.004, name="lid top"), 84)
    for sx in (-1, 1):
        box(kit, (0.02, lh - 0.03, ld - 0.03), (sx * (W / 2 - 0.01), ly0 + (lh - 0.03) / 2, lz - 0.015), PUTTY, lv, w=0.004, name="lid side")
    box(kit, (W + 0.004, 0.025, ld + 0.004), (0.0, ly0 + 0.0125, lz), BROWN, lv, w=0.004, name="lid band")
    box(kit, (W - 0.02, lh * 0.9, D - ld), (0.0, ly0 + lh * 0.45, -zf + (D - ld) / 2), STEEL, lv, w=0.008, name="rear top")
    if lv < 2:
        flat(kit, (W * 0.5, 0.004, 0.03), (0.0, ly0 + lh * 0.9 + 0.001, -zf + (D - ld) - 0.05), BLACK, name="paper exit")
    box(kit, (win_w + 0.006, win_h + 0.006, 0.004), (0.0, wyc, zf - 0.008), GLASS, lv, w=0.0006, name="lid window")
    flat(kit, (W - 0.04, lh - 0.04, 0.004), (0.0, ly0 + lh / 2, zf - 0.12), BLACK, name="station back")
    if lv < 2:
        kit.frame((win_w + 0.03, win_h + 0.03), (win_w, win_h), 0.006, S.ub((0.0, wyc, zf + 0.003)), BROWN,
                  bevel=S.bev(0.0015, lv)[0], segments=S.bev(0.0015, lv)[1], name="window frame")
        quad_part(kit, (0.0, wyc - 0.012, zf - 0.07), "+z", 0.42, 0.10, rect("GREENBAR"), name="paper at the band")
        box(kit, (0.46, 0.035, 0.05), (0.0, wyc + 0.048, zf - 0.06), BLACK, lv, w=0.002, name="ribbon cartridge")
        box(kit, (0.52, 0.012, 0.02), (0.0, wyc - 0.058, zf - 0.05), "Prop_Aluminium", lv, w=0.001, name="band guard")
    # rear: two doors, the stacker window, the output stack inside
    dw, dh = W / 2 - 0.03, y1 - y0 - 0.01
    dyc = (y0 + y1) / 2
    box(kit, (dw, dh, 0.025), (-(W / 4 - 0.005), dyc, -zf + 0.0125), STEEL, lv, w=0.003, name="rear door")
    bwr, bsr = S.bev(0.003, lv)
    kit.frame((dw, dh), (0.30, 0.34), 0.025, S.ub((W / 4 - 0.005, dyc, -zf + 0.0125)), STEEL, inner_offset=(-(0.20 - (W / 4 - 0.005)), 0.06),
              bevel=bwr, segments=bsr, name="rear door window")
    box(kit, (0.304, 0.344, 0.004), (0.20, dyc + 0.06, -zf + 0.01), GLASS, lv, w=0.0006, name="stacker window")
    if lv < 2:
        flat(kit, (W - 0.06, 0.004, D - 0.10), (0.0, 0.20, 0.0), BLACK, name="stacker shelf")
        _paper_stack(kit, lv, (0.12, 0.27, -zf + 0.18), 0.38, 0.28, 0.13, name="output stack")
    if lv < 2:
        box(kit, (0.02, 0.18, 0.014), (-0.35, (y0 + y1) / 2, -zf - 0.007), BROWN, lv, w=0.002, name="rear pull")


# ============================================================================ Kit_CRACUnit (downflow, Liebert Deluxe System/3 class)
CR_W, CR_D, CR_H = 1.83, 0.86, 1.90           # 62 / LS84; S5 construction


def crac_unit(kit, lv):
    """Kit_CRACUnit: a downflow computer-room air unit (S5): four insulated powder-coated front panels on the welded frame
    with quarter-turn latches and pulls, the control panel (LCD, keys, status lamps) on the left panel, the return-air
    filter grille across the top, the charcoal base; it discharges into the raised floor below."""
    W, D, H = CR_W, CR_D, CR_H
    zf = D / 2
    kick = 0.10
    box(kit, (W - 0.02, kick, D - 0.02), (0.0, kick / 2, 0.0), CHAR, lv, w=0.003, name="base")
    hb = H - kick - 0.06
    for sx in (-1, 1):
        uvoff(box(kit, (0.025, hb, D), (sx * (W / 2 - 0.0125), kick + hb / 2, 0.0), STEEL, lv, w=0.004, name="end panel"), 90 + sx)
    box(kit, (W - 0.05, hb, 0.025), (0.0, kick + hb / 2, -zf + 0.0125), STEEL, lv, w=0.003, name="back panel")
    flat(kit, (W - 0.05, hb, 0.004), (0.0, kick + hb / 2, zf - 0.03), BLACK, name="panel backing")
    # the top: a frame round the filter grille
    ty = H - 0.03
    box(kit, (W, 0.06, D), (0.0, ty, 0.0), STEEL, lv, w=0.006, name="top frame") if lv == 2 else None
    if lv < 2:
        for sz in (-1, 1):
            box(kit, (W, 0.06, 0.06), (0.0, ty, sz * (zf - 0.03)), STEEL, lv, w=0.004, name="top rail")
        for sx in (-1, 1):
            box(kit, (0.06, 0.06, D - 0.12), (sx * (W / 2 - 0.03), ty, 0.0), STEEL, lv, w=0.004, name="top rail")
        box(kit, (W - 0.12, 0.003, D - 0.12), (0.0, H - 0.008, 0.0), PERF, lv, w=0.0005, name="return grille", segs=1)
        for k in range(1, 4):
            box(kit, (0.012, 0.012, D - 0.12), ((W - 0.12) / 2 - k * (W - 0.12) / 4, H - 0.012, 0.0), CHAR, lv, w=0.001, name="grille bar", segs=1)
        flat(kit, (W - 0.12, 0.004, D - 0.12), (0.0, H - 0.05, 0.0), "Prop_PlasticWhite", name="filter media")
    # four front panels with shadow gaps, latches and pulls
    pw = (W - 0.05 - 3 * 0.005) / 4
    py0, py1 = kick + 0.004, H - 0.06 - 0.004
    for k in range(4):
        px = (W - 0.05) / 2 - pw / 2 - k * (pw + 0.005)
        uvoff(box(kit, (pw, py1 - py0, 0.025), (px, (py0 + py1) / 2, zf - 0.0125), STEEL, lv, w=0.004, name="front panel"), 93 + k)
        if lv < 2:
            for ly in (py0 + 0.20, py1 - 0.10):
                kit.cylinder(0.011, 0.004, S.ub((px - pw / 2 + 0.04, ly, zf + 0.002)), CHROME, verts=12 if lv == 0 else 8, rot=(90, 0, 0),
                             bevel=0.0008 if lv == 0 else 0.0, segments=1, name="latch")
                box(kit, (0.003, 0.014, 0.002), (px - pw / 2 + 0.04, ly, zf + 0.0045), BLACK, lv, w=0.0, name="latch slot") if lv == 0 else None
            box(kit, (0.12, 0.020, 0.0015), (px, py0 + 0.50, zf + 0.0007), BLACK, lv, w=0.0004, name="pull recess", segs=1)
            box(kit, (0.10, 0.006, 0.010), (px, py0 + 0.495, zf + 0.005), CHAR, lv, w=0.0012, name="pull lip")
    # the control panel on the left panel (the viewer's left = +x)
    px0 = (W - 0.05) / 2 - pw / 2
    pc = (px0, 1.52, zf + 0.0008)
    plate(kit, "PANEL_CRAC", pc, lv)
    P = PANELS["PANEL_CRAC"]
    for s in P["buttons"]:
        x, y, _ = site("PANEL_CRAC", s, pc)
        if lv < 2:
            box(kit, (P["bsize"], P["bsize"] * 0.8, 0.004), (x, y, zf + 0.0036), "Prop_PlasticWhite", lv, w=0.0012, name="membrane key", segs=1)
    for k, s in enumerate(P["leds"]):
        x, y, _ = site("PANEL_CRAC", s, pc)
        led(kit, (x, y, zf + 0.0016), (LED_G, LED_G, OFF_A, OFF_R)[k], lv)


# ============================================================================ Kit_DataTerminal (VT320 class)
def data_terminal(kit, lv):
    """Kit_DataTerminal: a character terminal of the VT320 class (S4: 31.4 w x 25.3 h x 31.5 d cm) on its tilt-swivel foot,
    the 14 in tube dark (screens dark), the power lamp unlit; origin the desk point under the foot, front +Z. The keyboard
    is the game's Kit_Keyboard at the sidecar anchor 'keyboard'."""
    foot_h, neck_h = 0.016, 0.020
    box(kit, (0.20, foot_h, 0.20), (0.0, foot_h / 2, -0.02), PBEIGE, lv, w=0.008 if lv == 0 else 0.004, name="foot")
    box(kit, (0.11, neck_h + 0.01, 0.09), (0.0, foot_h + neck_h / 2, -0.03), PBEIGE, lv, w=0.004, name="swivel neck")
    hy0 = foot_h + neck_h
    hh = 0.253 - hy0
    hyc = hy0 + hh / 2
    bw, bs = S.bev(0.006, lv)
    zb = 0.1575
    opening = (0.226, 0.168)
    oy = 0.012
    kit.frame((0.314, hh), opening, 0.030, S.ub((0.0, hyc, zb - 0.015)), PBEIGE, inner_offset=(0.0, oy), bevel=bw, segments=bs, name="bezel")
    box(kit, (0.306, hh - 0.008, 0.07), (0.0, hyc, zb - 0.065), PBEIGE, lv, w=0.004, name="front casing")
    kit.loft_box((0.296, hh - 0.012), (0.17, 0.13), 0.20, S.ub((0.0, hyc + 0.004, zb - 0.20)), PBEIGE, back_offset=(0.0, 0.012),
                 bevel=bw, segments=bs, name="tube housing")
    flat(kit, (opening[0] + 0.004, opening[1] + 0.004, 0.012), (0.0, hyc + oy, zb - 0.024), BLACK, name="tube surround")
    kit.bulged_panel(opening[0] - 0.002, opening[1] - 0.002, 0.009 if lv < 2 else 0.0, S.ub((0.0, hyc + oy, zb - 0.020)), SCREEN,
                     segments=12 if lv == 0 else (6 if lv == 1 else 1), name="screen glass")
    if lv < 2:
        for k in range(6):
            flat(kit, (0.11, 0.004, 0.003), (0.0, hyc - 0.03 + k * 0.014, zb - 0.30 + 0.002), BLACK, name="rear vent")
        led(kit, (-0.13, hy0 + 0.014, zb), OFF_G, lv, r=0.0022)
        box(kit, (0.022, 0.012, 0.004), (0.12, hy0 + 0.014, zb + 0.001), CHAR, lv, w=0.0008, name="brightness wheel", segs=1)
        S.tube(kit, "power cord", [(0.04, hyc - 0.04, zb - 0.28), (0.05, hyc - 0.07, zb - 0.31), (0.06, 0.03, zb - 0.33),
                                   (0.07, 0.004, zb - 0.36), (0.09, 0.004, zb - 0.42)], 0.003, BLACK, lv, lo=6)
    kit.anchor("keyboard", S.ub((0.0, 0.0, 0.30)))


# ============================================================================ Kit_DotMatrixPrinter (wide carriage, tractor feed)
def _dm_printer(kit, lv, base_y):
    """The printer itself (62 3.1 Desk 0.60 x 0.38 x 0.18; S8 wide carriage): a moulded case that rises toward the back, the
    smoked acoustic cover over the platen, the platen knob on the right, the tractor unit over the rear, the control panel
    at the front right; the forms come up behind into the tractors, round the platen and out over the cover, folding back."""
    W, D = 0.60, 0.38
    bw, bs = S.bev(0.010, lv)
    # the case: a loft from a low front (0.085) to a tall back (0.150), 0.36 deep, on a 10 mm plinth
    box(kit, (W - 0.02, 0.012, D - 0.03), (0.0, base_y + 0.006, 0.0), CHAR, lv, w=0.002, name="case plinth")
    kit.loft_box((W, 0.085), (W, 0.150), D - 0.02, S.ub((0.0, base_y + 0.012 + 0.0425, 0.0)), PBEIGE, back_offset=(0.0, 0.0325),
                 bevel=bw, segments=bs, name="case")
    # the smoked cover over the platen, sloping up toward the tractors
    o = box(kit, (W - 0.13, 0.008, 0.14), (0.0, base_y + 0.012 + 0.135, -0.055), CHAR, lv, w=0.003, name="acoustic cover")
    o.rotation_euler[0] = math.radians(-10)
    if lv < 2:
        kit.cylinder(0.024, 0.032, S.ub((-(W / 2 + 0.014), base_y + 0.115, -0.07)), BLACK, verts=16 if lv == 0 else 8, rot=(0, 90, 0),
                     bevel=0.0015 if lv == 0 else 0.0, segments=1, name="platen knob")
        for sx in (-1, 1):
            box(kit, (0.045, 0.034, 0.09), (sx * 0.205, base_y + 0.185, -0.165), BLACK, lv, w=0.002, name="tractor")
            box(kit, (0.030, 0.008, 0.075), (sx * 0.205, base_y + 0.206, -0.165), "Prop_PlasticGrey", lv, w=0.0015, name="tractor lid", segs=1)
        kit.cylinder(0.006, W - 0.10, S.ub((0.0, base_y + 0.180, -0.165)), CHROME, verts=8, rot=(0, 90, 0), bevel=0.0, name="tractor bar")
        flat(kit, (W - 0.16, 0.004, 0.02), (0.0, base_y + 0.163, -0.115), BLACK, name="paper slot")
    pc = (-0.195, base_y + 0.058, D / 2 + 0.0012)
    plate(kit, "PANEL_DOTMATRIX", pc, lv)
    P = PANELS["PANEL_DOTMATRIX"]
    if lv < 2:
        for s_ in P["buttons"]:
            x, y, _ = site("PANEL_DOTMATRIX", s_, pc)
            box(kit, (P["bsize"], P["bsize"] * 0.7, 0.004), (x, y, D / 2 + 0.004), PBEIGE, lv, w=0.001, name="key", segs=1)
        for k, s_ in enumerate(P["leds"]):
            x, y, _ = site("PANEL_DOTMATRIX", s_, pc)
            led(kit, (x, y, D / 2 + 0.002), (LED_G, LED_G, OFF_A)[k], lv, r=0.0022, bezel=False)
    # the forms: from below the back up into the tractors, out of the slot over the cover, back over the tractors and down
    if lv < 2:
        m = S.Mesh()
        g = rect("GREENBAR")
        pts = [(base_y - 0.05, -0.215), (base_y + 0.17, -0.205), (base_y + 0.205, -0.185),
               (base_y + 0.168, -0.115), (base_y + 0.215, -0.10), (base_y + 0.255, -0.13), (base_y + 0.25, -0.20),
               (base_y + 0.20, -0.25), (base_y + 0.10, -0.27)]
        run = 0.0
        for i in range(len(pts) - 1):
            (y0, z0), (y1, z1) = pts[i], pts[i + 1]
            L = math.hypot(y1 - y0, z1 - z0)
            v0 = g[1] + (g[3] - g[1]) * ((run / 0.2794) % 1.0)
            v1 = min(g[3], v0 + (g[3] - g[1]) * L / 0.2794)
            ny, nz = (z1 - z0), -(y1 - y0)
            quad = [(0.19, y0, z0), (-0.19, y0, z0), (-0.19, y1, z1), (0.19, y1, z1)]
            _oriented(m, quad, [(g[0], v0), (g[2], v0), (g[2], v1), (g[0], v1)], (0, ny, nz))
            _oriented(m, list(reversed(quad)), [(g[0], v1), (g[2], v1), (g[2], v0), (g[0], v0)], (0, -ny, -nz))
            run += L
        m.part(kit, "paper", ATLAS, merge=False)


def dot_matrix(kit, lv, on_stand=True):
    """Kit_DotMatrixPrinter_OnStand / _Desk: the wide-carriage printer with its tractor feed (62 3.1; S8), on a black steel
    printer stand with the paper slot in its top, the carton of continuous forms on the wire shelf below and the paper
    rising through the slot (OnStand); or alone on a desk with a short fanfold stack behind (Desk)."""
    if not on_stand:
        _dm_printer(kit, lv, 0.0)
        if lv < 2:
            _paper_stack(kit, lv, (0.0, 0.03, -0.38), 0.38, 0.28, 0.06, name="paper stack")
        return
    SW, SD, SH = 0.70, 0.50, 0.68
    tube = 0.025
    for sx in (-1, 1):
        for sz in (-1, 1):
            box(kit, (tube, SH - 0.04, tube), (sx * (SW / 2 - tube / 2), 0.04 + (SH - 0.04) / 2, sz * (SD / 2 - tube / 2)), SBLACK, lv,
                w=0.002, name="leg")
            if lv < 2:
                kit.cylinder(0.02, 0.02, S.ub((sx * (SW / 2 - 0.03), 0.02, sz * (SD / 2 - 0.03))), BLACK, verts=12 if lv == 0 else 6,
                             rot=(0, 90, 0), bevel=0.0, name="caster")
    # the top in two plates either side of the paper slot near the back
    box(kit, (SW, 0.02, SD - 0.07), (0.0, SH - 0.01, 0.035), SBLACK, lv, w=0.002, name="top plate")
    box(kit, (SW, 0.02, 0.025), (0.0, SH - 0.01, -SD / 2 + 0.0125), SBLACK, lv, w=0.002, name="top back strip")
    # the wire shelf with the carton
    sy = 0.12
    if lv < 2:
        for k in range(7):
            z = -SD / 2 + 0.03 + k * (SD - 0.06) / 6
            kit.cylinder(0.003, SW - 0.04, S.ub((0.0, sy, z)), SBLACK, verts=6, rot=(0, 90, 0), bevel=0.0, name="shelf wire")
    box(kit, (SW - 0.04, 0.012, 0.02), (0.0, sy, SD / 2 - 0.03), SBLACK, lv, w=0.001, name="shelf rail")
    box(kit, (SW - 0.04, 0.012, 0.02), (0.0, sy, -SD / 2 + 0.03), SBLACK, lv, w=0.001, name="shelf rail")
    fanfold_carton(kit, lv, base=(0.0, sy + 0.006, -0.02), open_top=True, printout=False)
    if lv < 2:
        m = S.Mesh()
        g = rect("GREENBAR")
        quad = [(0.19, sy + 0.26, -0.03), (-0.19, sy + 0.26, -0.03), (-0.19, SH - 0.05, -0.215), (0.19, SH - 0.05, -0.215)]
        _oriented(m, quad, [(g[0], g[1]), (g[2], g[1]), (g[2], g[3]), (g[0], g[3])], (0, 0.5, 1))
        _oriented(m, list(reversed(quad)), [(g[0], g[3]), (g[2], g[3]), (g[2], g[1]), (g[0], g[1])], (0, -0.5, -1))
        m.part(kit, "paper feed", ATLAS, merge=False)
    _dm_printer(kit, lv, SH)


# ============================================================================ Kit_EPOStation (+ _Halon)
def _mushroom(kit, lv, centre, r, slot_cap):
    """A mushroom push-button standing on a +Z face: the black collar, the domed cap."""
    x, y, z = centre
    kit.cylinder(r * 0.75, 0.010, S.ub((x, y, z + 0.005)), BLACK, verts=16 if lv == 0 else 8, rot=(90, 0, 0),
                 bevel=0.001 if lv == 0 else 0.0, segments=1, name="button collar")
    prof = [(r, 0.0), (r, 0.006), (r * 0.92, 0.011), (r * 0.62, 0.0155), (0.0, 0.017)] if lv < 2 else [(r, 0.0), (r * 0.6, 0.014), (0.0, 0.015)]
    kit.lathe(prof, S.ub((x, y, z + 0.010)), slot_cap, verts=24 if lv == 0 else 10, rot=(90, 0, 0), name="button cap")


def _clear_cover(kit, lv, centre, w, h, d):
    """A hinged clear cover over a button: a thin-walled polycarbonate hood (front, top, sides; open at the bottom and back)
    on a chrome hinge pin along its top."""
    x, y, z = centre
    t = 0.002
    box(kit, (w, h, t), (x, y, z + d - t / 2), GLASS, lv, w=0.0006, name="cover front")
    box(kit, (w, t, d), (x, y + h / 2 - t / 2, z + d / 2), GLASS, lv, w=0.0006, name="cover top")
    for sx in (-1, 1):
        box(kit, (t, h, d), (x + sx * (w / 2 - t / 2), y, z + d / 2), GLASS, lv, w=0.0006, name="cover side")
    if lv < 2:
        kit.cylinder(0.003, w + 0.01, S.ub((x, y + h / 2 + 0.002, z + 0.006)), CHROME, verts=8, rot=(0, 90, 0), bevel=0.0, name="cover hinge")


def epo_station(kit, lv, halon=False):
    """Kit_EPOStation / _Halon (62 3.1; LS83-LS85): the emergency power-off station by an exit: a red mushroom button on its
    red box under a hinged clear cover, on a blank engraved red plate, its pilot jewel; the halon version adds a yellow plate
    with the abort push-button box and the manual-release pull box. Origin the wall point under the plates' bottom centre
    (mounted at 1.2 m by the builder), front +Z; no collider."""
    pw = 0.22 if halon else 0.30
    px = 0.09 if halon else 0.0
    dbox(kit, (pw, 0.15, 0.003), (px, 0.075, 0.0015), rect("PLATE_EPO"), lv, w=0.0006, name="epo plate")
    bx, by = px, 0.07
    box(kit, (0.105, 0.105, 0.045), (bx, by, 0.003 + 0.0225), RED, lv, w=0.003, name="epo box")
    _mushroom(kit, lv, (bx, by, 0.048), 0.021, RED)
    _clear_cover(kit, lv, (bx, by, 0.048), 0.10, 0.10, 0.036)
    led(kit, (bx - 0.08 if not halon else bx - 0.075, 0.125, 0.003), JEWEL, lv, r=0.006)
    if not halon:
        return
    hx = -0.115
    dbox(kit, (0.16, 0.15, 0.003), (hx, 0.075, 0.0015), rect("PLATE_HALON"), lv, w=0.0006, name="halon plate")
    # abort station: a yellow box with a black push-button (held to abort)
    ax = hx + 0.04
    dbox(kit, (0.06, 0.09, 0.04), (ax, 0.07, 0.023), colour("belt_yellow"), lv, w=0.002, name="abort box")
    _mushroom(kit, lv, (ax, 0.07, 0.043), 0.015, BLACK)
    # manual release: a red pull box with its white handle
    rx = hx - 0.042
    box(kit, (0.065, 0.10, 0.04), (rx, 0.07, 0.023), RED, lv, w=0.002, name="release box")
    if lv < 2:
        flat(kit, (0.045, 0.03, 0.003), (rx, 0.085, 0.0445), BLACK, name="handle recess")
        box(kit, (0.035, 0.014, 0.012), (rx, 0.083, 0.050), "Prop_PlasticWhite", lv, w=0.002, name="pull handle", segs=1)


# ============================================================================ Kit_TapeRack (+ _Gaps), Kit_TapeCart
BELTS = ("belt_blue", "belt_grey", "belt_white", "belt_blue", "belt_red", "belt_yellow", "belt_black", "belt_grey", "belt_white")


def _hanging_reels(m, x0, x1, y_bar, z, rnd, lv, gaps=0.0, pitch=0.034):
    """A row of 10.5 in reels hanging edge-on from a bar in their seal belts (seen from the aisle: the belts' rims), each the
    front half of a cylinder (axis along X) and its two half caps; a colour per belt from the atlas."""
    n = int((x1 - x0) / pitch)
    segs = 6 if lv == 0 else 3
    r = 0.137
    t = 0.028
    yc = y_bar - 0.02 - r
    count = 0
    for k in range(n):
        if rnd.random() < gaps:
            continue
        cx = x1 - pitch / 2 - k * pitch
        c = colour(BELTS[rnd.randrange(len(BELTS))])
        cu, cv = (c[0] + c[2]) / 2, (c[1] + c[3]) / 2
        uv = [(cu, cv)] * 4
        ring = [(yc + r * math.sin(a), z + r * math.cos(a)) for a in [(-math.pi / 2) + math.pi * j / segs for j in range(segs + 1)]]
        for j in range(segs):
            (ya, za), (yb, zb) = ring[j], ring[j + 1]
            mid = ((ya + yb) / 2 - yc, (za + zb) / 2 - z)
            _oriented(m, [(cx - t / 2, ya, za), (cx + t / 2, ya, za), (cx + t / 2, yb, zb), (cx - t / 2, yb, zb)],
                      [(c[0], c[1]), (c[2], c[1]), (c[2], c[3]), (c[0], c[3])], (0, mid[0], mid[1]))
        for sx in (-1, 1):
            pts = [(cx + sx * t / 2, yc, z)] + [(cx + sx * t / 2, ya, za) for (ya, za) in ring]
            for j in range(segs):
                _oriented(m, [pts[0], pts[j + 1], pts[j + 2]], [(cu, cv)] * 3, (sx, 0, 0))
        if lv == 0:
            # the belt's hook over the bar
            _oriented(m, [(cx - 0.004, y_bar - 0.02, z - 0.012), (cx + 0.004, y_bar - 0.02, z - 0.012), (cx + 0.004, y_bar + 0.012, z - 0.012),
                          (cx - 0.004, y_bar + 0.012, z - 0.012)], [(cu, cv)] * 4, (0, 0, 1))
        count += 1
    return count


def tape_rack(kit, lv, gaps=False):
    """Kit_TapeRack (+ _Gaps): a wall rack of hanging reels (62 3.1, LS56 the tape library): two steel end panels, six bars,
    the reels in their coloured seal belts; _Gaps = the abandoned state with reels pulled (a quarter gone). 1.07 x 0.38 x 1.98,
    back to the wall (z -0.19), front +Z."""
    W, D, H = 1.07, 0.38, 1.98
    for sx in (-1, 1):
        uvoff(box(kit, (0.02, H, D), (sx * (W / 2 - 0.01), H / 2, 0.0), SPUTTY, lv, w=0.003, name="end panel"), 120 + sx)
        if lv < 2:
            box(kit, (0.0015, H - 0.20, D - 0.12), (sx * (W / 2 + 0.0008), H / 2, 0.0), SPUTTY, lv, w=0.0004, name="end field", segs=1)
    box(kit, (W, 0.02, D), (0.0, H - 0.01, 0.0), SPUTTY, lv, w=0.003, name="top")
    box(kit, (W - 0.04, 0.06, D - 0.02), (0.0, 0.03, 0.0), SPUTTY, lv, w=0.003, name="kick")
    flat(kit, (W - 0.04, H - 0.08, 0.004), (0.0, H / 2 + 0.03, -D / 2 + 0.01), SPUTTY, name="back")
    rnd = random.Random(1987 + (1 if gaps else 0))
    m = S.Mesh()
    bars = [H - 0.06 - k * 0.315 for k in range(6)]
    for yb in bars:
        kit.cylinder(0.011, W - 0.04, S.ub((0.0, yb, 0.0)), CHROME if lv == 0 else SPUTTY, verts=10 if lv == 0 else 6, rot=(0, 90, 0),
                     bevel=0.0, name="hanger bar")
        if lv < 2:
            _hanging_reels(m, -(W / 2 - 0.03), W / 2 - 0.03, yb, 0.0, rnd, lv, gaps=0.25 if gaps else 0.04)
        else:
            c = colour("belt_blue")
            _oriented(m, [(W / 2 - 0.03, yb - 0.30, 0.14), (-(W / 2 - 0.03), yb - 0.30, 0.14), (-(W / 2 - 0.03), yb - 0.02, 0.14),
                          (W / 2 - 0.03, yb - 0.02, 0.14)], [(c[0], c[1]), (c[2], c[1]), (c[2], c[3]), (c[0], c[3])], (0, 0, 1))
    m.part(kit, "reels", ATLAS, merge=False)


def tape_cart(kit, lv):
    """Kit_TapeCart: the mobile tape cart (62 3.1: 0.91 x 0.51 x 1.20): a steel frame on casters with three bars of hanging
    reels, a push handle."""
    W, D, H = 0.91, 0.51, 1.20
    for sx in (-1, 1):
        for sz in (-1, 1):
            box(kit, (0.025, H - 0.08, 0.025), (sx * (W / 2 - 0.0125), 0.08 + (H - 0.08) / 2, sz * (D / 2 - 0.0125)), SPUTTY, lv, w=0.002, name="post")
            kit.cylinder(0.035, 0.025, S.ub((sx * (W / 2 - 0.04), 0.035, sz * (D / 2 - 0.05))), BLACK, verts=12 if lv == 0 else 6,
                         rot=(0, 90, 0), bevel=0.0, name="caster")
    box(kit, (W, 0.03, D), (0.0, 0.095, 0.0), SPUTTY, lv, w=0.003, name="base tray")
    box(kit, (W, 0.025, D), (0.0, H - 0.0125, 0.0), SPUTTY, lv, w=0.003, name="top frame")
    if lv < 2:
        S.tube(kit, "push handle", [(W / 2 - 0.0125, H - 0.05, -D / 2), (W / 2 + 0.06, H - 0.05, -D / 2 + 0.04),
                                    (W / 2 + 0.06, H - 0.05, D / 2 - 0.04), (W / 2 - 0.0125, H - 0.05, D / 2)], 0.012, SPUTTY, lv, lo=8)
    rnd = random.Random(1991)
    m = S.Mesh()
    for k, yb in enumerate((H - 0.05, H - 0.38, H - 0.71)):
        kit.cylinder(0.010, W - 0.04, S.ub((0.0, yb, 0.0)), CHROME if lv == 0 else SPUTTY, verts=10 if lv == 0 else 6, rot=(0, 90, 0),
                     bevel=0.0, name="hanger bar")
        if lv < 2:
            _hanging_reels(m, -(W / 2 - 0.03), W / 2 - 0.03, yb, 0.0, rnd, lv, gaps=0.15)
    if lv < 2:
        m.part(kit, "reels", ATLAS, merge=False)


# ============================================================================ Kit_FanfoldBox (+ _Stack)
FB_W, FB_D, FB_H = 0.40, 0.30, 0.28           # LS98: a carton of 15 x 11 in continuous forms


def fanfold_carton(kit, lv, base=(0.0, 0.0, 0.0), open_top=True, printout=True):
    """An open carton of continuous forms (62 3.1): four 4 mm board walls (one printed side), the flaps folded out and down,
    the paper stack inside (sheet edges, the green-bar top sheet), a printout folded on top. Origin the floor centre."""
    bx, by, bz = base
    t = 0.004
    hp = FB_H
    walls = [((FB_W, hp, t), (bx, by + hp / 2, bz + FB_D / 2 - t / 2), "front"), ((FB_W, hp, t), (bx, by + hp / 2, bz - FB_D / 2 + t / 2), "back"),
             ((t, hp, FB_D - 2 * t), (bx + FB_W / 2 - t / 2, by + hp / 2, bz), "side"), ((t, hp, FB_D - 2 * t), (bx - FB_W / 2 + t / 2, by + hp / 2, bz), "side")]
    for size, c, kind in walls:
        if kind == "front":
            dbox(kit, size, c, rect("CARTON"), lv, w=0.0008, name="carton front")
        else:
            uvoff(box(kit, size, c, CARD, lv, w=0.0008, name="carton wall", segs=1), int(c[0] * 100 + c[2] * 10))
    flat(kit, (FB_W - 2 * t, t, FB_D - 2 * t), (bx, by + t / 2, bz), CARD, name="carton floor")
    if open_top and lv < 2:
        # the flaps: long ones folded out and down at 110 degrees, short ones out
        for sz in (-1, 1):
            o = box(kit, (FB_W, 0.15, t), (bx, by + hp - 0.07, bz + sz * (FB_D / 2 + 0.05)), CARD, lv, w=0.0008, name="long flap", segs=1)
            o.rotation_euler[0] = math.radians(-sz * 22)
        for sx in (-1, 1):
            o = box(kit, (t, 0.10, FB_D - 0.02), (bx + sx * (FB_W / 2 + 0.035), by + hp - 0.045, bz), CARD, lv, w=0.0008, name="short flap", segs=1)
            o.rotation_euler[1] = math.radians(sx * 28)
    ph = hp - 0.05
    _paper_stack(kit, lv, (bx, by + t + ph / 2, bz), FB_W - 2 * t - 0.006, FB_D - 2 * t - 0.006, ph, name="forms")
    if printout and lv < 2:
        # a printout of a few folds lying on top, slightly askew
        dbox(kit, (0.38, 0.012, 0.28), (bx + 0.01, by + t + ph + 0.007, bz - 0.01), rect("GREENBAR_EDGE"), lv, w=0.0006, name="printout")
        quad_part(kit, (bx + 0.01, by + t + ph + 0.0135, bz - 0.01), "+y", 0.38, 0.28, rect("GREENBAR"), name="printout top")


def fanfold_stack(kit, lv):
    """Kit_FanfoldBox_Stack: a printout stack set down on the floor or a desk (0.38 x 0.28 x 0.05), the top fold lifting."""
    _paper_stack(kit, lv, (0.0, 0.025, 0.0), 0.381, 0.279, 0.05, name="stack")
    if lv < 2:
        m = S.Mesh()
        g = rect("GREENBAR")
        y0 = 0.0505
        quad = [(0.19, y0, -0.14), (-0.19, y0, -0.14), (-0.19, y0 + 0.03, 0.12), (0.19, y0 + 0.03, 0.12)]
        _oriented(m, quad, [(g[0], g[1]), (g[2], g[1]), (g[2], g[3]), (g[0], g[3])], (0, 1, -0.1))
        _oriented(m, list(reversed(quad)), [(g[0], g[3]), (g[2], g[3]), (g[2], g[1]), (g[0], g[1])], (0, -1, 0.1))
        m.part(kit, "lifted fold", ATLAS, merge=False)


# ============================================================================ Kit_HalonCylinder (+ _Pair)
def _cylinder(kit, lv, x, z):
    """One Halon 1301 container (S6, Kidde class 1990-92): red, 0.30 m, 0.95 m to the shoulder, the brass valve with its
    gauge, the black actuator, the label wrapped on the front, two galvanised straps on a strut channel."""
    r = 0.15
    prof = [(0.0, 0.02), (r * 0.82, 0.025), (r * 0.97, 0.05), (r, 0.10), (r, 0.82), (r * 0.94, 0.89), (r * 0.70, 0.94), (r * 0.36, 0.975), (0.05, 0.99), (0.0, 0.99)]
    if lv == 2:
        prof = [(0.0, 0.02), (r, 0.06), (r, 0.86), (0.05, 0.99), (0.0, 0.99)]
    kit.lathe(prof, S.ub((x, 0.0, z)), RED, verts=32 if lv == 0 else (16 if lv == 1 else 8), name="cylinder")
    kit.cylinder(r * 0.86, 0.025, S.ub((x, 0.0125, z)), BLACK if lv < 2 else RED, verts=24 if lv == 0 else 8, bevel=0.0, name="foot ring")
    # the label: a curved band on the front
    if lv < 2:
        m = S.Mesh()
        lr = r + 0.0008
        c = rect("HALON_LABEL")
        steps = 10 if lv == 0 else 5
        a0, a1 = math.radians(-50), math.radians(50)
        for k in range(steps):
            b0, b1 = a0 + (a1 - a0) * k / steps, a0 + (a1 - a0) * (k + 1) / steps
            p = lambda b, y: (x - lr * math.sin(b), y, z + lr * math.cos(b))
            u0 = c[0] + (c[2] - c[0]) * (k / steps)
            u1 = c[0] + (c[2] - c[0]) * ((k + 1) / steps)
            _oriented(m, [p(b0, 0.42), p(b1, 0.42), p(b1, 0.64), p(b0, 0.64)], [(u0, c[1]), (u1, c[1]), (u1, c[3]), (u0, c[3])],
                      (-math.sin((b0 + b1) / 2), 0, math.cos((b0 + b1) / 2)))
        m.part(kit, "label", ATLAS, merge=False)
    # valve, gauge, actuator
    kit.cylinder(0.035, 0.07, S.ub((x, 1.025, z)), BRASS, verts=16 if lv == 0 else 8, bevel=0.002 if lv == 0 else 0.0, segments=1, name="valve body")
    if lv < 2:
        box(kit, (0.05, 0.04, 0.06), (x, 1.06, z), BRASS, lv, w=0.003, name="valve block")
        kit.cylinder(0.026, 0.012, S.ub((x, 1.065, z + 0.045)), CHROME, verts=16 if lv == 0 else 8, rot=(90, 0, 0), bevel=0.0, name="gauge")
        disc_part(kit, (x, 1.065, z + 0.0515), 0.022, "+z", sub("METER_K", 0.1, 0.1, 0.9, 0.9), 16, name="gauge dial")
        kit.cylinder(0.035, 0.09, S.ub((x - 0.05, 1.08, z)), BLACK, verts=16 if lv == 0 else 8, bevel=0.002 if lv == 0 else 0.0, segments=1, name="actuator")
    # the discharge hose and pipe up the wall to the kit top
    S.tube(kit, "discharge", [(x, 1.08, z), (x, 1.14, z - 0.02), (x, 1.20, z - 0.10), (x, 1.24, z - 0.14)], 0.016, BLACK, lv, lo=8)
    S.tube(kit, "pipe", [(x, 1.24, z - 0.14), (x, 1.55, z - 0.14)], 0.017, GALV, lv, lo=8)
    if lv < 2:
        kit.cylinder(0.025, 0.04, S.ub((x, 1.25, z - 0.14)), GALV, verts=12 if lv == 0 else 6, bevel=0.0, name="union")
    # straps and channel
    for y in (0.35, 0.72):
        m = S.Mesh()
        steps = 14 if lv == 0 else 6
        rr = r + 0.003
        for k in range(steps):
            b0 = math.radians(-100 + 200 * k / steps)
            b1 = math.radians(-100 + 200 * (k + 1) / steps)
            p = lambda b, yy: (x - rr * math.sin(b), yy, z + rr * math.cos(b))
            _oriented(m, [p(b0, y - 0.02), p(b1, y - 0.02), p(b1, y + 0.02), p(b0, y + 0.02)], [(0.0, 0.0), (0.05, 0.0), (0.05, 0.04), (0.0, 0.04)],
                      (-math.sin((b0 + b1) / 2), 0, math.cos((b0 + b1) / 2)))
        m.part(kit, "strap", GALV, merge=False)
    box(kit, (0.041, 0.80, 0.041), (x, 0.55, -0.18), GALV, lv, w=0.002, name="strut channel")


def halon_cylinder(kit, lv, pair=False):
    """Kit_HalonCylinder (+ _Pair): 0.40 x 0.40 x 1.55 (62 3.1); origin the floor centre, the wall at z -0.20, front +Z."""
    if not pair:
        _cylinder(kit, lv, 0.0, 0.0)
        return
    _cylinder(kit, lv, 0.18, 0.0)
    _cylinder(kit, lv, -0.18, 0.0)
    S.tube(kit, "manifold", [(0.18, 1.45, -0.14), (-0.18, 1.45, -0.14)], 0.017, GALV, lv, lo=8)


# ============================================================================ Kit_ChartRecorder
def chart_recorder(kit, lv):
    """Kit_ChartRecorder: a circular-chart temperature and humidity recorder (S7; LS56's dewpoint recorder): a 0.36 x 0.40 x
    0.15 steel case, the hinged glass door, the 10 in chart on its hub, two pen arms (purple, green). Origin the wall point
    under the case's bottom centre (at 1.5 m by the builder), front +Z; no collider."""
    W, H, D = 0.36, 0.40, 0.15
    dc = D - 0.02
    flat(kit, (W - 0.01, H - 0.01, 0.004), (0.0, H / 2, 0.002), STEEL, name="case back")
    for sx in (-1, 1):
        uvoff(box(kit, (0.012, H, dc), (sx * (W / 2 - 0.006), H / 2, dc / 2), STEEL, lv, w=0.004 if lv == 0 else 0.002, name="case side"), 131 + sx)
    for sy in (0, 1):
        uvoff(box(kit, (W - 0.024, 0.012, dc), (0.0, 0.006 + sy * (H - 0.012), dc / 2), STEEL, lv, w=0.004 if lv == 0 else 0.002, name="case end"), 133 + sy)
    bw, bs = S.bev(0.003, lv)
    kit.frame((W, H), (W - 0.05, H - 0.05), 0.022, S.ub((0.0, H / 2, D - 0.011)), STEEL, bevel=bw, segments=bs, name="door frame")
    box(kit, (W - 0.045, H - 0.045, 0.003), (0.0, H / 2, D - 0.006), GLASS, lv, w=0.0006, name="door glass")
    flat(kit, (W - 0.03, H - 0.03, 0.004), (0.0, H / 2, D - 0.075), BLACK, name="chart plate")
    cy = H / 2 + 0.01
    disc_part(kit, (0.0, cy, D - 0.07), 0.127, "+z", rect("CHART", 2.0), 48 if lv == 0 else (24 if lv == 1 else 12), name="chart")
    if lv < 2:
        kit.cylinder(0.012, 0.012, S.ub((0.0, cy, D - 0.064)), CHROME, verts=12 if lv == 0 else 6, rot=(90, 0, 0), bevel=0.0, name="chart hub")
        for k, (ang, col) in enumerate(((-30.0, "belt_blue"), (-38.0, "cable_beige"))):
            px, py = -0.13, cy + 0.10
            L = 0.15 - k * 0.02
            a = math.radians(ang + 180)
            ex, ey = px + L * math.cos(a) * -1, py + L * math.sin(a)
            S.tube(kit, "pen arm", [(px, py, D - 0.058 + k * 0.004), (ex, ey, D - 0.058 + k * 0.004)], 0.0015, CHROME, lv, lo=6)
            cbox(kit, (0.006, 0.006, 0.008), (ex, ey, D - 0.062 + k * 0.004), ("belt_blue", "belt_red")[k], lv, w=0.0, name="pen")
        kit.cylinder(0.008, 0.01, S.ub((-0.13, cy + 0.10, D - 0.06)), BLACK, verts=10, rot=(90, 0, 0), bevel=0.0, name="pen pivot")
        box(kit, (0.012, 0.04, 0.012), (-(W / 2 - 0.004), H / 2, D - 0.01), CHROME, lv, w=0.002, name="latch", segs=1)
        led(kit, (0.14, 0.03, D), LED_G, lv, r=0.003)


# ============================================================================ the access floor: Kit_FloorTileLifted, the open and perforated cells
def lifter(kit, lv, centre, yaw=0.0):
    """A twin suction-cup panel lifter (62 3.1): two black rubber cups with lever locks on a satin aluminium handle, lying on
    the panel face (centre on the face, the handle along X)."""
    x, y, z = centre
    for sx in (-1, 1):
        kit.cylinder(0.055, 0.012, S.ub((x + sx * 0.10, y + 0.006, z)), RUBBER, verts=20 if lv == 0 else 8, radius_top=0.045,
                     bevel=0.0, name="cup")
        if lv < 2:
            box(kit, (0.05, 0.012, 0.022), (x + sx * 0.10, y + 0.018, z + 0.01), BLACK, lv, w=0.002, name="lever", segs=1)
    S.tube(kit, "lifter handle", [(x + 0.10, y + 0.012, z), (x + 0.08, y + 0.07, z), (x - 0.08, y + 0.07, z), (x - 0.10, y + 0.012, z)],
           0.010, "Prop_Aluminium", lv, lo=8)


def floor_tile_lifted(kit, lv, flat_on_floor=False):
    """Kit_FloorTileLifted (Leaning / _Flat): one of G1's 600 mm panels (laminate top, edge band, steel pan) lifted out, leaning
    back 14 degrees against whatever stands behind (origin the floor point under its bottom edge's centre, the laminate facing
    +Z), or lying on the floor next to its hole; its suction lifter still on it. No collider."""
    n0 = len(kit.parts)
    H._panel(kit, 0.0, 0.0, lv, (1, 1), underside=True)
    for p in kit.parts[n0:]:
        if p.name.startswith("panel top"):
            # one drawn panel of Prop_AccessFloor (a 0.6 m cell of its 1.2 m tile, _TileSize 1.2: the rect in metres), mapped
            # in the panel's own frame so the drawn PVC edge stays on the panel's edge however the panel is turned
            p["fr_uv"] = "decal"
            p["fr_decal_axes"] = "xy"
            p["fr_uv_rect"] = [0.6, 0.6, 1.2, 1.2]
            if "fr_uv_offset" in p.keys():
                del p["fr_uv_offset"]
    lifter(kit, lv, (0.0, 0.0, 0.0))
    from mathutils import Matrix
    parts = kit.parts[n0:]
    if flat_on_floor:
        T = Matrix.Translation(Vector(S.ub((0.0, 0.0332, 0.0)))) @ Matrix.Rotation(math.radians(9), 4, "Z")
    else:
        # stand it up: the top face to +Z (Unity), leaning back 14 degrees, its lower edge on the floor
        T = Matrix.Translation(Vector(S.ub((0.0, 0.008, 0.0)))) @ Matrix.Rotation(math.radians(76), 4, "X") @ Matrix.Translation(Vector(S.ub((0.0, 0.0, -0.30))))
    for p in parts:
        p.matrix_basis = T @ p.matrix_basis


def _cell_panels(kit, lv, skip=(), perf=()):
    """G1's Kit_AccessFloor_Cell_3m panels (H.access_cell's layout and P8 cells) with panels left out or perforated."""
    import random as _r
    rnd = _r.Random(1990)
    proud = {(1, 3), (3, 0)}
    cells = [(0, 0), (1, 0), (0, 1), (1, 1)]
    chosen = {}
    s = H.PANEL - H.GAP
    for i in range(5):
        for j in range(5):
            cx, cz = -1.2 + 0.6 * i, -1.2 + 0.6 * j
            near = (chosen.get((i - 1, j)), chosen.get((i, j - 1)))
            pick = [c for c in cells if c not in near]
            chosen[(i, j)] = pick[rnd.randrange(len(pick))]
            if (i, j) in skip:
                continue
            lift = 0.0006 if (i, j) in proud else 0.0
            if (i, j) in perf:
                dbox(kit, (s, 0.0016, s), (cx, -0.0008 + lift, cz), rect("PERF_PANEL"), lv, w=0.0006 if lv == 0 else 0.0, facing="+y", name="perf panel")
                continue
            o = S.box(kit, (s, 0.0016, s), (cx, -0.0008 + lift, cz), H.FLOOR_TOP, lv, w=0.0006 if lv == 0 else 0.0, name="panel top")
            H._panel_uv(o, cx, cz, chosen[(i, j)])


def access_cell_open(kit, lv):
    """Kit_AccessFloor_CellOpen_3m: G1's 3 x 3 m cell with one panel out (panel (1, 3): centre x -0.6, z +0.6): the hole's four
    edge bands, the pedestals at its corners, the sealed slab 0.30 below, two cable bundles and a flexible-conduit power whip
    to its receptacle box. Place Kit_FloorTileLifted beside it (62 2.3's event)."""
    hole = (1, 3)
    _cell_panels(kit, lv, skip=(hole,))
    hx, hz = -1.2 + 0.6 * hole[0], -1.2 + 0.6 * hole[1]
    # the joint shadow under the faces, cut round the hole (four strips)
    flat(kit, (3.0, 0.004, hz - 0.3 + 1.5), (0.0, -0.0036, (-1.5 + hz - 0.3) / 2), BLACK, name="joint shadow")
    flat(kit, (3.0, 0.004, 1.5 - hz - 0.3), (0.0, -0.0036, (hz + 0.3 + 1.5) / 2), BLACK, name="joint shadow")
    flat(kit, (hx - 0.3 + 1.5, 0.004, 0.6), ((-1.5 + hx - 0.3) / 2, -0.0036, hz), BLACK, name="joint shadow")
    flat(kit, (1.5 - hx - 0.3, 0.004, 0.6), ((hx + 0.3 + 1.5) / 2, -0.0036, hz), BLACK, name="joint shadow")
    # the neighbours' edge bands seen in the hole
    for (sx, sz, w, d) in ((1, 0, 0.004, 0.6), (-1, 0, 0.004, 0.6), (0, 1, 0.6, 0.004), (0, -1, 0.6, 0.004)):
        flat(kit, (w, H.PANEL_T, d), (hx + sx * 0.3015, -H.PANEL_T / 2, hz + sz * 0.3015), "Prop_PlasticBlack", name="edge band")
    # slab, pedestals
    flat(kit, (1.4, 0.01, 1.4), (hx, -0.305, hz), "Stair_Slab", name="slab")
    if lv < 2:
        for sx in (-1, 1):
            for sz in (-1, 1):
                n0 = len(kit.parts)
                H.pedestal(kit, min(2, lv + 1))
                for p in kit.parts[n0:]:
                    p.location = p.location + Vector(S.ub((hx + sx * 0.3, 0.0, hz + sz * 0.3)))
        # cable bundles along X under the hole and a power whip
        for k, (cz, col, r) in enumerate(((hz - 0.12, "cable_grey", 0.022), (hz + 0.05, "cable_beige", 0.016), (hz + 0.16, "cable_black", 0.011))):
            m = S.Mesh()
            path = [(hx + 0.9, -0.285 + r, cz), (hx + 0.3, -0.282 + r, cz + 0.01 * k), (hx, -0.278 + r, cz - 0.01), (hx - 0.3, -0.283 + r, cz + 0.02), (hx - 0.9, -0.285 + r, cz)]
            S.sweep_section(m, path, S.circle(r, 8 if lv == 0 else 5), cap0=False, cap1=False)
            c = colour(col)
            m.uv = [((c[0] + c[2]) / 2, (c[1] + c[3]) / 2)] * len(m.uv)
            m.part(kit, "cable bundle", ATLAS)
        S.tube(kit, "power whip", [(hx + 0.5, -0.29, hz - 0.25), (hx + 0.15, -0.27, hz - 0.22), (hx - 0.05, -0.26, hz - 0.15), (hx - 0.12, -0.27, hz - 0.05)],
               0.011, "Prop_FlexConduit", lv, lo=8)
        box(kit, (0.10, 0.06, 0.10), (hx - 0.16, -0.27, hz - 0.02), "RunB_GalvSteel", lv, w=0.002, name="receptacle box")


def access_cell_perf(kit, lv):
    """Kit_AccessFloor_CellPerf_3m: G1's cell with its j = 0 row (z -1.2) as five 25 % perforated air panels (the cold aisle
    in front of the cabinets, fed by the downflow unit through the floor)."""
    perf = tuple((i, 0) for i in range(5))
    _cell_panels(kit, lv, perf=perf)
    flat(kit, (3.0, 0.004, 3.0), (0.0, -0.0036, 0.0), BLACK, name="joint shadow")
