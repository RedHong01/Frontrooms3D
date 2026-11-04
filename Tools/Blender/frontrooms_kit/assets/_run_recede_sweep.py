"""The Run! receding end's rubber edge sweeps, in two poses (71_run_room.md s.5.1: "rubber edge sweeps with two poses,
pressed when seated, relaxed when moving"). Two kits, Kit_RunRecede_SweepSeated and Kit_RunRecede_SweepMoving, swapped
on the beat (the seat, the tell) the way the door break swaps its stages: the importer takes no blend shapes, and one
change per beat hides in the jolt (research/door_break/50_rethink.md s.3.9).

Side sweeps: a 3 mm neoprene blade on a bead, rooted on each edge channel's web 60 mm behind the front face, 0.11 m
long. Seated it bows against the wall (its tip on the wall face, trailing toward the entrance); moving it swings 40 deg
back, its tip 15 mm or more off the wall, the way a period automatic door bottom retracts. Gaps where the head passes
fixed things: the bumper rail (0.81-0.99 m), the hold-open magnet (1.89-2.01 m) and, on the left, the EMT and its
handy boxes (from 2.54 m). Bottom sweeps on both sides of the door: seated on the floor, moving lifted 10 mm.
ORIGIN and frame as Kit_RunRecede_Head. Budget 900 / 300 / 100 each; lodDistances 10 / 28 / 0. Slot: Prop_Rubber.
"""

import math

import _run_b_common as B
import _run_recede_common as R

RUBBER = "Prop_Rubber"
ROOT_Z = -0.060
BLADE = 0.110
T = 0.0030
SIDE_SPANS = {1: ((R.BOTTOM, 0.81), (0.99, 1.89), (2.01, R.NOTCH[2])),
              -1: ((R.BOTTOM, 0.81), (0.99, 1.89), (2.01, 2.54))}


def _curve(seated, n):
    """The blade's centre line in (out, back) metres from the root: 'out' toward the wall (or the floor), 'back' toward
    the entrance (-z). Seated: bowed onto the wall 85 mm out; moving: straight, swung 40 deg back."""
    pts = []
    for k in range(n + 1):
        u = k / float(n)
        if seated:
            # a quarter-ellipse bow: out to 0.0845, trailing 0.045 toward the entrance at the tip
            a = u * math.pi / 2.0
            pts.append((0.0845 * math.sin(a), 0.045 * (1.0 - math.cos(a)) + 0.006 * u))
        else:
            ang = math.radians(40.0)
            pts.append((BLADE * u * math.cos(ang), BLADE * u * math.sin(ang)))
    return pts


def _outline(centre):
    """Offset a centre line by +-T/2 into a closed polygon."""
    left, right = [], []
    for i, (a, b) in enumerate(centre):
        if i == 0:
            da, db = centre[1][0] - a, centre[1][1] - b
        elif i == len(centre) - 1:
            da, db = a - centre[i - 1][0], b - centre[i - 1][1]
        else:
            da, db = centre[i + 1][0] - centre[i - 1][0], centre[i + 1][1] - centre[i - 1][1]
        L = math.hypot(da, db) or 1.0
        na, nb = -db / L, da / L
        left.append((a + na * T / 2, b + nb * T / 2))
        right.append((a - na * T / 2, b - nb * T / 2))
    return left + list(reversed(right))


def _level(kit, lv, seated):
    n = 6 if lv == 0 else 3 if lv == 1 else 1
    centre = _curve(seated, n)
    poly = _outline(centre)
    for s in (-1, 1):
        x_root = s * R.HALF
        for (y0, y1) in SIDE_SPANS[s]:
            # extrude along Unity Y (Blender Z): profile (a, b) = (Blender x, Blender y) = (-Unity x, -Unity z)
            prof = [(-(x_root + s * o), -(ROOT_Z - bk)) for (o, bk) in poly]
            if s < 0:
                prof = list(reversed(prof))
            B.sweep(kit, "side sweep", prof, y1 - y0, RUBBER, lv=lv, axis="z", loc=(0.0, 0.0, (y0 + y1) / 2.0))
            if lv == 0:
                # the bead the blade is moulded on, clipped to the edge web
                R.rbox(kit, x_root, x_root + s * 0.006, y0, y1, ROOT_Z - 0.004, ROOT_Z + 0.004, RUBBER, 0.0015, 1, name="sweep bead")
    for s in (-1, 1):
        x0, x1 = sorted((s * R.DOOR_HALF, s * R.HALF))
        # extrude along Unity X (Blender X, mirrored): profile (a, b) = (Blender y, Blender z) = (-Unity z, Unity y)
        # seated: down onto the floor (the tip 1.4 mm over it); moving: lifted, the tip 10 mm over it
        down = (0.0186 / 0.0845) if seated else 0.010 / (BLADE * math.cos(math.radians(40.0)))
        bottom = [(o * down, bk) for (o, bk) in centre]
        bpoly = _outline(bottom)
        prof = [(-(ROOT_Z - bk), R.BOTTOM - o) for (o, bk) in bpoly]
        B.sweep(kit, "bottom sweep", prof, x1 - x0, RUBBER, lv=lv, axis="x", loc=(-(x0 + x1) / 2.0, 0.0, 0.0))
        if lv == 0:
            R.rbox(kit, x0, x1, R.BOTTOM - 0.004, R.BOTTOM, ROOT_Z - 0.004, ROOT_Z + 0.004, RUBBER, 0.0015, 1, name="sweep bead")


def build_pose(kit, seated, budget):
    B.levels(kit, lambda k, lv: _level(k, lv, seated))
    kit.no_collider()
    kit.tag("run", "run_recede", "receding_end")
    B.lod_meta(kit, R.LOD_DISTANCES, budget)
    kit.meta["runRecede"] = "card 2 head first, 2026-11-04 (71_run_room.md s.5.1); pose " + ("seated" if seated else "moving")
