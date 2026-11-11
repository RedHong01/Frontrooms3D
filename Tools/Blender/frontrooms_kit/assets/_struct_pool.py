"""The pool's fittings in image 1 (structures G2-c, line visual (game visual), 2026-11-10; package structures_pool_fittings, 30_plan.md
P11; W/structures_assets/g2_review_assets.md section 6; 20_research.md A.5.1; BR:S40 2-5, S41, S46; PC20-PC23).

All in UNITY space (x, y, z), converted by _struct_common.ub(); hand-built LOD0 / LOD1 / LOD2 (B.levels), hero rule as G1-b (curves at
a 0.2 mm chord at LOD0, exposed edges bevelled at LOD0). Image 1's own rule: the ways out are steps, so no ladder; the pool level is
pristine (A.8), so the metals are clean. No text, label, brand or logo. ASCII only.
  Kit_MainDrainGrate  the main drain at a basin's deepest point (period: a flat slotted grate, pre-2007, "marked plainly in an
                      appropriate contrasting color", S40 2-5): a 12 in square cast bronze frame and a grate of 12 mm bars at 22 mm
                      over the dark sump, four countersunk screws; origin at the floor's surface, the grate 2 mm proud; the
                      contrasting frame of dark tile round it is the caller's (the Lane tile in the floor).
  Kit_ReturnInlet     a return inlet 12 in under the water line: a chrome-plated bronze "eyeball" fitting, its flange on the wall,
                      the ball in its socket, the dark bore; origin on the wall plane, +Z out of the wall (the lamps' wall frame).
  Kit_FloorDrain      a deck drain (the deck falls 1/4-1/2 in per ft to it): a 5 in chrome strainer with radial slots over a dark
                      bowl, in its flange; origin at the floor's surface.
"""

import math

import kitlib
import _run_b_common as B
import _struct_common as S

BRONZE = "Prop_BrassSatin"
CHROME = "Prop_ChromeSatin"
DARK = "Plenum_Dark"

# slots this module needs that kitlib.SLOTS does not list (their Unity materials are main's Resources/Surfaces/<slot>.mat)
kitlib.register_slot(CHROME, (0.70, 0.70, 0.70), 0.18, 1.0)
kitlib.register_slot(BRONZE, (0.55, 0.42, 0.24), 0.40, 1.0)

DIST_SMALL = (2.0, 7.0, 24.0)


# ----------------------------------------------------------------------------- the main drain
FRAME_OUT = 0.330
FRAME_IN = 0.305
GRATE_T = 0.006


def main_drain(kit, lv):
    """Kit_MainDrainGrate: origin at the floor's surface (the basin's floor), the grate's top 2 mm over it."""
    top = 0.002
    # the frame: four bevelled bars round the grate, 8 mm deep (its top flush with the grate)
    fw = (FRAME_OUT - FRAME_IN) / 2.0
    for sx, sz, w, d in ((0.0, (FRAME_OUT - fw) / 2.0, FRAME_OUT, fw), (0.0, -(FRAME_OUT - fw) / 2.0, FRAME_OUT, fw),
                         ((FRAME_OUT - fw) / 2.0, 0.0, fw, FRAME_IN), (-(FRAME_OUT - fw) / 2.0, 0.0, fw, FRAME_IN)):
        S.box(kit, (w, 0.008, d), (sx, top - 0.004, sz), BRONZE, lv, w=0.0015, name="frame")
    # the sump under the grate, dark
    S.box(kit, (FRAME_IN, 0.004, FRAME_IN), (0.0, top - 0.045, 0.0), DARK, lv, w=0.0, name="sump")
    if lv >= 2:
        S.box(kit, (FRAME_IN - 0.004, GRATE_T, FRAME_IN - 0.004), (0.0, top - GRATE_T / 2.0, 0.0), BRONZE, lv, w=0.0, name="grate plate")
        return
    # the bars across x (12 mm at 22 mm; LOD1 half as many, wider), their ends on two edge bars
    n = 13 if lv == 0 else 7
    bw = 0.012 if lv == 0 else 0.022
    span = FRAME_IN - 0.024
    for k in range(n):
        z = -span / 2.0 + span * k / (n - 1.0)
        S.box(kit, (FRAME_IN - 0.004, GRATE_T, bw), (0.0, top - GRATE_T / 2.0, z), BRONZE, lv, w=0.0012, name="grate bar")
    for sx in (-1.0, 1.0):
        S.box(kit, (0.012, GRATE_T, FRAME_IN - 0.004), (sx * (FRAME_IN / 2.0 - 0.008), top - GRATE_T / 2.0, 0.0), BRONZE, lv, w=0.0012, name="grate edge")
    if lv == 0:
        for sx, sz in ((-1, -1), (1, -1), (-1, 1), (1, 1)):
            S.screw_head(kit, 0.0045, (sx * (FRAME_OUT / 2.0 - fw / 2.0), top, sz * (FRAME_OUT / 2.0 - fw / 2.0)), "+y", CHROME, lv, name="frame screw", dome=0.18)


# ----------------------------------------------------------------------------- the return inlet
def return_inlet(kit, lv):
    """Kit_ReturnInlet: origin on the wall plane at the fitting's centre, +Z out of the wall."""
    # the flange: 90 mm, 6 mm proud, its edge eased (a lathe about +z)
    prof = [(0.045, 0.0), (0.045, 0.004), (0.043, 0.006), (0.032, 0.0065), (0.030, 0.008)]
    if lv >= 1:
        prof = [(0.045, 0.0), (0.045, 0.006), (0.030, 0.008)]
    S.lathe_axis(kit, prof, (0.0, 0.0, 0.0), "z", CHROME, lv, name="inlet flange", close_top=False, close_bottom=False)
    # the socket ring round the ball
    ring = [(0.030, 0.008), (0.029, 0.013), (0.0245, 0.0145), (0.0225, 0.013)]
    if lv >= 1:
        ring = [(0.030, 0.008), (0.026, 0.0145), (0.0225, 0.013)]
    S.lathe_axis(kit, ring, (0.0, 0.0, 0.0), "z", CHROME, lv, name="socket ring", close_top=False, close_bottom=False)
    # the eyeball: a sphere r 22 mm whose centre sits 6 mm out, turned a little down (the jets aim the flow round the pool)
    n = 10 if lv == 0 else 6 if lv == 1 else 4
    r = 0.022
    ball = []
    for k in range(n + 1):
        a = -math.pi / 2.0 + math.pi * k / n
        ball.append((max(r * math.cos(a), 0.0) if k < n else 0.0, r * math.sin(a)))
    S.lathe_axis(kit, [(p[0], p[1] + 0.006) for p in ball if p[1] + 0.006 >= 0.0], (0.0, 0.0, 0.0), "z", CHROME, lv, name="eyeball", close_top=True, close_bottom=False)
    # the bore: a dark disc 1 mm under the ball's crest, 16 mm across
    if lv <= 1:
        S.cyl_z(kit, 0.008, 0.002, (0.0, -0.002, 0.006 + r - 0.0015), DARK, lv, w=0.0, name="bore")


# ----------------------------------------------------------------------------- the deck drain
def floor_drain(kit, lv):
    """Kit_FloorDrain: origin at the floor's surface, the strainer's top 1.5 mm proud."""
    top = 0.0015
    prof = [(0.070, 0.0), (0.070, top - 0.0005), (0.068, top), (0.058, top)]
    S.lathe_y(kit, prof, (0.0, 0.0, 0.0), CHROME, lv, name="drain flange", close_top=False, close_bottom=False)
    S.cyl_y(kit, 0.058, 0.004, (0.0, -0.02, 0.0), DARK, lv, w=0.0, name="drain bowl")
    if lv >= 2:
        S.cyl_y(kit, 0.058, 0.003, (0.0, top - 0.0015, 0.0), CHROME, lv, w=0.0, name="strainer plate")
        return
    # the strainer: a hub, a rim and radial bars with the slots between them
    S.cyl_y(kit, 0.012, 0.003, (0.0, top - 0.0015, 0.0), CHROME, lv, w=0.0006, name="strainer hub")
    rim = [(0.058, top - 0.003), (0.058, top), (0.050, top), (0.050, top - 0.003)]
    S.lathe_y(kit, rim, (0.0, 0.0, 0.0), CHROME, lv, name="strainer rim", close_top=False, close_bottom=False)
    bars = 16 if lv == 0 else 8
    for k in range(bars):
        a = 360.0 * k / bars
        rad = math.radians(a)
        cx, cz = 0.031 * math.cos(rad), 0.031 * math.sin(rad)
        S.box(kit, (0.040, 0.003, 0.004), (cx, top - 0.0015, cz), CHROME, lv, w=0.0006, name="strainer bar", rot=(0, -a, 0))
