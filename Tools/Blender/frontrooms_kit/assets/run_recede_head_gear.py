"""The Run! receding end, its running gear (71_run_room.md s.5.1; card 2, head first). PRODUCTION kit
Kit_RunRecede_HeadGear, hand-built LOD0/1/2. Rides with Kit_RunRecede_Head (same origin, same frame).

- Two four-wheel trolleys running inside the surface-mounted tracks (Kit_RunRecede_Track_3m, x +-1.25 under the soffit):
  a steel body, two axles, four 36 mm wheels on the track's lips, a 16 mm threaded hanger down through the track's slot
  to the head's corner bearing plate, with a hex nut, a jam nut and a washer (the visible part: the 35 mm between the
  head's notch and the track, and the wheels' rims inside the slot).
- The strip brushes where the corridor's services pass through the header: a galvanized holder down each slot cheek and
  along the header's top, nylon filament tufts (as rubber, matte black) trimmed to the services that sit in the slot when
  the head is seated on a whole cell (the tray, the sprinkler main's grooved coupling; the strip lamps hang between the
  cell lines, so their slot closes), meeting in the middle elsewhere; the top brush closes the last 40 mm to the soffit.
- The goal window light's box (C42, 71 s.5.1: "in a small box behind the lite"): a galvanized hood on a bracket behind
  the wall over the door, louvred toward the hidden length, its lamp holder inside; the rig's C42 light sits in it.
- Two bottom guides: steel fins under the sill running in the two 20 mm floor slots (Kit_RunRecede_FloorSlot_3m).
ORIGIN and frame as Kit_RunRecede_Head. Budget 6,000 / 1,500 / 300; lodDistances 10 / 28 / 0.
Slots: RunB_GalvSteel, Prop_Rubber. No collider.
"""

import math

import _run_b_common as B
import _run_recede_common as R

NAME = "Kit_RunRecede_HeadGear"
SMOOTH_ANGLE = 35.0
BUDGET = (6000, 1500, 300)

GALV = "RunB_GalvSteel"
NYLON = "Prop_Rubber"

PITCH = 0.008                        # tuft pitch along a holder (LOD0)
MAIN = (0.55, 2.62, 0.072)           # the sprinkler main's grooved coupling at a cell line: centre x, y, radius
TRAY = (-0.75, -0.45, 2.700, 2.780)  # the ladder tray's section (x0, x1, y0, y1)


def _trolley(kit, lv, s):
    c = s * R.TRACK_X
    yb = R.NOTCH[2] + 0.010          # the bearing plate's top (Kit_RunRecede_Head)
    lip = R.TRACK_BOTTOM + 0.003     # the track lips' running surface
    wr = 0.018
    wy = lip + wr
    R.rcyl(kit, "y", 0.008, wy - yb + 0.004, (c, (yb + wy) / 2 + 0.002, 0.0), GALV, 12 if lv == 0 else 8, name="hanger rod")
    if lv < 2:
        R.rcyl(kit, "y", 0.0135, 0.013, (c, yb + 0.0095, 0.0), GALV, 6, 0.0008 if lv == 0 else 0.0, 1, name="hanger nut")
    if lv == 0:
        R.rcyl(kit, "y", 0.016, 0.003, (c, yb + 0.0015, 0.0), GALV, 16, name="hanger washer")
        R.rcyl(kit, "y", 0.0135, 0.008, (c, lip - 0.010, 0.0), GALV, 6, 0.0006, 1, name="jam nut")
    bw, bs = R.hero(lv, 0.0012)
    R.rbox(kit, c - 0.012, c + 0.012, wy - 0.010, wy + 0.014, -0.070, 0.070, GALV, bw, bs, name="trolley body")
    if lv < 2:
        for dz in (-0.045, 0.045):
            R.rcyl(kit, "x", 0.0055, 0.074, (c, wy, dz), GALV, 8, name="axle")
            for dx in (-0.031, 0.031):
                R.rcyl(kit, "x", wr, 0.012, (c + dx, wy, dz), GALV, 16 if lv == 0 else 10, 0.0012 if lv == 0 else 0.0, 1, name="wheel")


def _tuft_row(kit, lv, name, roots, direction, lens, lean):
    """LOD0: the filament tufts; LOD1: the brush as one strip a run (it is a dark band from 10 m)."""
    if lv == 0:
        return R.tufts(kit, name, roots, direction, lens, NYLON, width=0.0040, thick=0.0018, lean=lean)
    if not roots:
        return None
    dx, dy, _dz = direction
    reach = sum(lens) / float(len(lens))
    xs = [p[0] for p in roots]
    ys = [p[1] for p in roots]
    if abs(dx) > 0.5:
        x0 = roots[0][0]
        return R.rbox(kit, x0, x0 + dx * reach, min(ys) - 0.003, max(ys) + 0.003, -0.004, 0.004, NYLON, name=name)
    y0 = roots[0][1]
    return R.rbox(kit, min(xs) - 0.003, max(xs) + 0.003, y0, y0 + dy * reach, -0.004, 0.004, NYLON, name=name)


def _brushes(kit, lv):
    """Slot brushes: one holder down each cheek, tufts across the slot to the service or past the middle; the top brush."""
    hold_d = 0.010
    pitch = PITCH if lv == 0 else PITCH * 6.0
    for (name, a, b, bottom) in R.SLOTS:
        mid = (a + b) / 2.0
        for side, root_x, sign in (("l", a, 1.0), ("r", b, -1.0)):
            R.rbox(kit, root_x, root_x + sign * hold_d, bottom, R.TOP, -0.006, 0.006, GALV, 0.0006 if lv == 0 else 0.0, 1, name="brush holder")
            if lv == 2:
                continue
            roots, lens, lean = [], [], []
            y = bottom + 0.004
            i = 0
            start = root_x + sign * hold_d
            while y < R.TOP - 0.002:
                reach = abs(mid - start) + 0.006
                if name == "tray" and TRAY[2] - 0.004 < y < TRAY[3] + 0.004:
                    edge = TRAY[0] if sign > 0 else TRAY[1]
                    reach = max(0.0, abs(edge - start) - 0.004)
                elif name == "main" and abs(y - MAIN[1]) < MAIN[2] + 0.004:
                    dy = y - MAIN[1]
                    hw = math.sqrt(max(0.0, (MAIN[2] + 0.004) ** 2 - dy * dy))
                    edge = MAIN[0] - sign * hw
                    reach = max(0.0, abs(edge - start) - 0.002)
                reach *= 1.0 + 0.04 * R.jitter(i, 7 if sign > 0 else 11)
                roots.append((start, y, 0.0015 * R.jitter(i, 3)))
                lens.append(reach)
                lean.append(0.004 * R.jitter(i, 5))
                y += pitch
                i += 1
            _tuft_row(kit, lv, "brush " + name + " " + side, roots, (sign, 0.0, 0.0), lens, lean)
    for (x0, x1, top_y) in R.header_spans():
        if top_y < R.TOP - 1e-6:
            continue
        R.rbox(kit, x0, x1, R.TOP + 0.002, R.TOP + 0.010, -0.006, 0.006, GALV, 0.0005 if lv == 0 else 0.0, 1, name="top brush holder")
        if lv == 2:
            continue
        roots, lens, lean = [], [], []
        x = x0 + 0.003
        i = 0
        while x < x1 - 0.002:
            roots.append((x, R.TOP + 0.010, 0.0015 * R.jitter(i, 13)))
            lens.append((R.SOFFIT - R.TOP - 0.010) * (1.0 + 0.06 * R.jitter(i, 17)))
            lean.append(0.003 * R.jitter(i, 19))
            x += pitch
            i += 1
        _tuft_row(kit, lv, "top brush", roots, (0.0, 1.0, 0.0), lens, lean)


def _light_box(kit, lv):
    """C42 in its hood: a galvanized box behind the wall over the door, open (louvred) toward the hidden length."""
    cx, cy, cz = R.C42
    w, h, d = 0.26, 0.20, 0.18
    z0, z1 = cz - d * 0.6, cz + d * 0.4
    t = 0.0015
    bw, bs = R.hero(lv, 0.0008)
    if lv == 2:
        R.rbox(kit, cx - w / 2, cx + w / 2, cy - h / 2, cy + h / 2, z0, z1, GALV, name="light box")
    else:
        R.rbox(kit, cx - w / 2, cx + w / 2, cy + h / 2 - t, cy + h / 2, z0, z1, GALV, bw, 1, name="light box top")
        R.rbox(kit, cx - w / 2, cx + w / 2, cy - h / 2, cy - h / 2 + t, z0, z1, GALV, bw, 1, name="light box bottom")
        for s in (-1, 1):
            R.rbox(kit, cx + s * w / 2, cx + s * (w / 2 - t), cy - h / 2, cy + h / 2, z0, z1, GALV, bw, 1, name="light box side")
        R.rbox(kit, cx - w / 2, cx + w / 2, cy - h / 2, cy + h / 2, z0, z0 + t, GALV, bw, 1, name="light box back")
        n = 4 if lv == 0 else 2
        for k in range(n):
            y = cy - h / 2 + h * (k + 0.5) / n
            kit.box(R.S(w - 0.006, 0.0015, 0.036), R.U(cx, y, z1 - 0.010), GALV, bevel=0.0, segments=1, rot=(-32.0, 0.0, 0.0), name="louvre")
        if lv == 0:
            R.rcyl(kit, "z", 0.022, 0.050, (cx, cy + 0.02, z0 + 0.030), GALV, 16, 0.0008, 1, name="lamp holder")
    for s in (-1, 1):
        x = cx + s * (w / 2 - 0.03)
        R.rbox(kit, x - 0.015, x + 0.015, cy + h / 2, cy + h / 2 + 0.005, R.BACK, z0 + 0.06, GALV, bw, 1, name="light box bar")
        R.rbox(kit, x - 0.015, x + 0.015, cy + h / 2 - 0.02, cy + h / 2 + 0.12, R.BACK, R.BACK + 0.005, GALV, bw, 1, name="light box foot")


def _guides(kit, lv):
    bw, bs = R.hero(lv, 0.0010)
    for s in (-1, 1):
        x = s * R.SLOT_X
        R.rbox(kit, x - 0.004, x + 0.004, -0.032, R.BOTTOM, -0.060, 0.060, GALV, bw, 1, name="bottom guide fin")
        if lv < 2:
            R.rbox(kit, x - 0.020, x + 0.020, R.BOTTOM - 0.006, R.BOTTOM, -0.070, 0.070, GALV, bw, 1, name="guide shoe")


def _level(kit, lv):
    for s in (-1, 1):
        _trolley(kit, lv, s)
    _brushes(kit, lv)
    _light_box(kit, lv)
    _guides(kit, lv)


def build(kit):
    B.levels(kit, _level)
    kit.no_collider()
    kit.tag("run", "run_recede", "receding_end")
    B.lod_meta(kit, R.LOD_DISTANCES, BUDGET)
    kit.meta["runRecede"] = "card 2 head first, 2026-11-04 (71_run_room.md s.5.1); inert until rule 5b"
