"""Janitor cart, structural-foam plastic (#ABA9A0, r8 D12: light platinum grey), with a
dark vinyl trash bag on its hoop, a yellow mop bucket and wringer (#D9AE14), a mop and
two spray bottles (US, 1975-1992; no brand). PRODUCTION obstacle kit for Run! B
(Kit_JanitorCart), hand-built LOD0/1/2.

1.16 x 0.56 x 0.97 m. The shelf body stands at +X, the bag hangs off its outer end, the
bucket rides the low deck at -X. ORIGIN = the floor under the cart's centre.
FRONT = kit -Y (Unity +Z). Box collider. Budget 9,000 / 3,600 / 700; lodDistances 4 / 12 / never.
Slots: RunB_CartPlastic (cart, mop head, bottles), Prop_Vinyl (bag), RunB_BucketYellow,
Prop_PlasticBlack (wheels, mop handle, triggers, wringer lever).
"""

import _run_b_common as B

NAME = "Kit_JanitorCart"
SMOOTH_ANGLE = 40.0
BUDGET = (9000, 3600, 700)

CART = "RunB_CartPlastic"
BAG = "Prop_Vinyl"
YELLOW = "RunB_BucketYellow"
BLACK = "Prop_PlasticBlack"

L, W = 1.16, 0.56
SHELF_X0, SHELF_X1 = 0.04, 0.44        # the shelf body
DECK_Z = 0.20


def _wheel(kit, x, y, r, width, lv, name):
    B.cyl(kit, r, width, (x, y, r), BLACK, lv=lv, rot=(90, 0, 0), w=0.004, name=name, lo=24)
    if lv == 0:
        B.cyl(kit, r * 0.45, width + 0.006, (x, y, r), CART, lv=1, rot=(90, 0, 0), w=0.002, name=name + " hub", lo=12)


def _level(kit, lv):
    bw, bs = B.bev(0.012, lv)
    # Base: the moulded platform with the low bucket deck at -X.
    kit.box((L - 0.10, W - 0.04, 0.070), (-0.03, 0, DECK_Z - 0.035), CART, bevel=bw, segments=bs, name="base")
    if lv < 2:
        kit.box((0.52, W - 0.08, 0.020), (-0.27, 0, DECK_Z + 0.010), CART, bevel=B.bev(0.006, lv)[0], segments=B.bev(0.006, lv)[1], name="deck lip")
    # Shelf body: two side panels, three shelves, a top tray with a rim.
    sx0, sx1 = SHELF_X0, SHELF_X1
    cx = (sx0 + sx1) / 2
    for sy in (-1, 1):
        kit.box((sx1 - sx0, 0.026, 0.76), (cx, sy * (W / 2 - 0.013), DECK_Z + 0.38), CART, bevel=B.bev(0.008, lv)[0], segments=B.bev(0.008, lv)[1], name="side panel")
    for z in (DECK_Z + 0.02, DECK_Z + 0.36):
        kit.box((sx1 - sx0 - 0.01, W - 0.05, 0.024), (cx, 0, z), CART, bevel=B.bev(0.006, lv)[0], segments=B.bev(0.006, lv)[1], name="shelf")
    kit.box((sx1 - sx0 + 0.04, W, 0.030), (cx, 0, DECK_Z + 0.76), CART, bevel=B.bev(0.010, lv)[0], segments=B.bev(0.010, lv)[1], name="top tray")
    if lv < 2:
        bw2, bs2 = B.bev(0.004, lv)
        kit.frame((sx1 - sx0 + 0.04, W), (sx1 - sx0 + 0.01, W - 0.03), 0.040, (cx, 0, DECK_Z + 0.795), CART, bevel=bw2, segments=bs2, rot=(90, 0, 0), name="tray rim")
    # Push handle across the shelf body's inner end.
    if lv < 2:
        B.tube(kit, [(sx0 - 0.02, -(W / 2 - 0.04), DECK_Z + 0.70), (sx0 - 0.06, -(W / 2 - 0.06), DECK_Z + 0.74),
                     (sx0 - 0.06, W / 2 - 0.06, DECK_Z + 0.74), (sx0 - 0.02, W / 2 - 0.04, DECK_Z + 0.70)], 0.014, CART, lv=lv, name="push handle", lo=16)
    # Bag hoop and the vinyl bag on the outer end (+X).
    hx = sx1 + 0.02
    if lv < 2:
        B.tube(kit, [(hx, -(W / 2 - 0.05), DECK_Z + 0.70), (hx + 0.30, -(W / 2 - 0.05), DECK_Z + 0.70),
                     (hx + 0.30, W / 2 - 0.05, DECK_Z + 0.70), (hx, W / 2 - 0.05, DECK_Z + 0.70)], 0.010, CART, lv=lv, name="bag hoop", lo=12)
    if lv == 2:
        kit.box((0.30, W - 0.10, 0.62), (hx + 0.15, 0, DECK_Z + 0.39), BAG, bevel=0.0, segments=0, name="bag")
    else:
        kit.soft_box((0.30, W - 0.10, 0.66), (hx + 0.15, 0, DECK_Z + 0.37), BAG, radius=0.05,
                     segments=24 if lv == 0 else 10, rings=14 if lv == 0 else 6, sag=0.0, puff=0.012 if lv == 0 else 0.0, name="bag")
        if lv == 0:
            # The bag's cuff folded over the hoop.
            kit.frame((0.33, W - 0.07), (0.29, W - 0.11), 0.05, (hx + 0.15, 0, DECK_Z + 0.68), BAG, bevel=0.006, segments=2, rot=(90, 0, 0), name="bag cuff")
    # Wheels: two rear 8 in wheels under the shelf body, two 3 in casters at the bucket end.
    for sy in (-1, 1):
        _wheel(kit, sx1 - 0.08, sy * (W / 2 - 0.02), 0.100, 0.040, lv, "rear wheel")
        _wheel(kit, -L / 2 + 0.10, sy * (W / 2 - 0.08), 0.038, 0.026, lv, "caster")
    # The mop bucket and its wringer on the deck.
    bx = -0.30
    bz = DECK_Z + 0.02
    if lv == 2:
        kit.box((0.40, 0.32, 0.34), (bx, 0, bz + 0.17), YELLOW, bevel=0.0, segments=0, name="bucket")
    else:
        bw3, bs3 = B.bev(0.012, lv)
        kit.box((0.40, 0.32, 0.30), (bx, 0, bz + 0.15), YELLOW, bevel=bw3, segments=bs3, name="bucket")
        kit.frame((0.42, 0.34), (0.37, 0.29), 0.03, (bx, 0, bz + 0.31), YELLOW, bevel=B.bev(0.005, lv)[0], segments=B.bev(0.005, lv)[1], rot=(90, 0, 0), name="bucket rim")
        kit.quad(0.36, 0.28, (bx, 0, bz + 0.27), BLACK, facing="+z", uv="metres", name="dirty water")
        # Wringer: a press box on the rim and its lever handle.
        B.box(kit, (0.22, 0.24, 0.16), (bx + 0.06, 0, bz + 0.40), YELLOW, w=0.010, lv=lv, name="wringer")
        B.tube(kit, [(bx + 0.14, 0, bz + 0.46), (bx + 0.20, 0, bz + 0.70), (bx + 0.22, 0, bz + 0.80)], 0.013, BLACK, lv=lv, name="wringer lever", lo=12)
    # Mop: handle leaning on the shelf body, head in the bucket.
    if lv < 2:
        B.tube(kit, [(bx - 0.08, 0.04, bz + 0.18), (sx0 - 0.04, 0.16, 1.40)], 0.012, BLACK, lv=lv, name="mop handle", lo=12)
        kit.soft_box((0.14, 0.12, 0.22), (bx - 0.08, 0.04, bz + 0.20), CART, radius=0.04, segments=16 if lv == 0 else 8, rings=10 if lv == 0 else 5, puff=0.008, name="mop head")
    # Two spray bottles on the top tray.
    if lv < 2:
        for k, by in enumerate((-0.10, 0.08)):
            prof = [(0.0, 0.0), (0.038, 0.0), (0.040, 0.004), (0.040, 0.150), (0.030, 0.180), (0.014, 0.195), (0.013, 0.215), (0.0, 0.215)]
            B.lathe(kit, prof, (cx + 0.06 * (1 - 2 * k), by, DECK_Z + 0.775), CART, lv=lv, name="spray bottle", lo=24,
                    rings=None if lv == 0 else [0, 1, 3, 5, 7])
            B.box(kit, (0.050, 0.026, 0.040), (cx + 0.06 * (1 - 2 * k) - 0.01, by, DECK_Z + 1.010), BLACK, w=0.004, lv=lv, name="trigger head")


def build(kit):
    B.levels(kit, _level)
    kit.collider((0.0, 0, 0.485), (L, W, 0.97))
    kit.tag("run", "run_b", "obstacle")
    B.lod_meta(kit, (4.0, 12.0, 0.0), BUDGET)
