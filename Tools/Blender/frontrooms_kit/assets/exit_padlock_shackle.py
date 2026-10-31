"""Kit_Padlock_Shackle: the J shackle of the EXIT chain's 2 in laminated padlock (游戏视觉, unlock micro cut scene,
2026-10-31; research/unlock/10_research.md §2 + §6, 20_design.md §2.3).

How it works (US 2,379,437, 1945; US 4,098,100, 1978; 10_research §6): the long HEEL leg stays in the body, a spring
under it; the short TOE leg drops into its bore and is held there by the latch (or ball) in its notch. Turning the key
draws the latch out of the notches: the spring drives the shackle up until the heel's shoulder hits its stop (10-12 mm),
the toe clears the body by a few millimetres, and the shackle can swivel round the heel leg.

PART frame: origin = the HEEL leg's axis on the body's top face plane (Kit_Padlock_Body's (-0.017, 0, 0)); +X toward
the toe leg (at +0.034), +Y up, +Z out of the lock's face. FrontRoomsExitPadlock moves it +Y by the pop and turns it
about +Y for the swivel. Shut, the heel runs 30 mm and the toe 9 mm into the body (hidden); the notches (a waist on
each leg) show only once the shackle is out.

Geometry: one sweep of a round 9.6 mm section along heel leg -> bow (centre-line radius 17 mm, 25 mm clear inside) ->
toe leg, with the toe's notch 4-7 mm above its end and the heel's at 20 mm (both a 1.2 mm waist), chamfered ends.
LOD0 16 sides, LOD1 8, LOD2 6. Slot Prop_ChromeSatin (hardened, polished by use). Render-only.
"""

import math
import sys

import exit_lock_common as L
import interact_door_common as dc

NAME = "Kit_Padlock_Shackle"
SMOOTH_ANGLE = 60.0
LOD_DISTANCES = (1.5, 5.0, None)
LOD1_RATIO = 0.25
LOD2_RATIO = 0.08

CHROME = "Prop_ChromeSatin"

import kitlib  # noqa: E402

kitlib.register_slot(CHROME, (0.72, 0.72, 0.70), 0.3, 1.0)


def centre_line(step):
    """(points, radius per point) heel end -> toe end, in the PART frame."""
    w = L.SHACKLE_WIRE_R
    pts, rad = [], []

    def leg(x, y0, y1, notch, end_at_start):
        n = max(2, int(round(abs(y1 - y0) / step)))
        for k in range(n + 1):
            y = y0 + (y1 - y0) * k / n
            r = w
            if notch is not None and notch[0] <= y <= notch[1]:
                r = w - 0.0012
            pts.append((x, y, 0.0))
            rad.append(r)

    # heel leg, from its end inside the body up to the bow's start
    pts.append((0.0, -L.HEEL_DEPTH, 0.0)); rad.append(w - 0.0015)              # chamfered end
    leg(0.0, -L.HEEL_DEPTH + 0.0015, L.SHACKLE_BOW_C, (-0.0215, -0.0185), True)
    # the bow, centre (0.017, bow_c), from the heel side over the top to the toe side
    segs = max(6, int(round(math.pi * L.SHACKLE_HALF / step)))
    for k in range(1, segs):
        a = math.pi - math.pi * k / segs
        pts.append((L.SHACKLE_HALF + L.SHACKLE_HALF * math.cos(a), L.SHACKLE_BOW_C + L.SHACKLE_HALF * math.sin(a), 0.0))
        rad.append(w)
    # toe leg down to its end, the notch on it
    leg(2 * L.SHACKLE_HALF, L.SHACKLE_BOW_C, -L.TOE_DEPTH + 0.0015, (-L.TOE_DEPTH + 0.004, -L.TOE_DEPTH + 0.007), False)
    pts.append((2 * L.SHACKLE_HALF, -L.TOE_DEPTH, 0.0)); rad.append(w - 0.0015)
    return pts, rad


def sweep(kit, step, sides, lods):
    pts, rad = centre_line(step)

    def section(k):
        r = rad[k]
        return [(r * math.cos(2 * math.pi * i / sides), r * math.sin(2 * math.pi * i / sides)) for i in range(sides)]

    # up = +Z: the shackle lies in the X-Y plane, so the section's frame turns smoothly round the bow
    obj = dc.sweep_path(kit, pts, section, CHROME, "shackle", cap=True, up=(0, 0, 1))
    obj["fr_lods"] = lods
    return obj


def build(kit):
    sweep(kit, 0.0025, 16, "0")
    sweep(kit, 0.005, 8, "1")
    sweep(kit, 0.010, 6, "2")
    dc.finalize(kit, SMOOTH_ANGLE, None)
    lo, hi = dc.bounds_unity(kit.parts)
    apex_in = L.SHACKLE_BOW_C + L.SHACKLE_HALF - L.SHACKLE_WIRE_R
    assert abs(apex_in - L.SHACKLE_CLEAR) < 1e-6, "25 mm clear when shut"
    assert abs(hi[1] - (L.SHACKLE_BOW_C + L.SHACKLE_HALF + L.SHACKLE_WIRE_R)) < 3e-4, "bow top %s" % (hi,)
    dc.anchor(kit, "heel_axis", 0.0, 0.0, 0.0)
    dc.anchor(kit, "toe_axis", 2 * L.SHACKLE_HALF, 0.0, 0.0)
    dc.anchor(kit, "bow_centre", L.SHACKLE_HALF, L.SHACKLE_BOW_C, 0.0)
    kit.meta["motion"] = {"type": "rig", "owner": "FrontRoomsExitPadlock", "pop": L.POP_TRAVEL, "swivelAxis": "+Y at heel_axis",
                          "note": "the EXIT chain's padlock shackle: pops 11 mm at the release, swivels about the heel leg"}
    kit.no_collider()
    kit.tag("interactable", "exit", "padlock")
    dc.lod_meta(kit, sys.modules[__name__])
