"""Folding-chair dolly loaded with eight folded grey steel chairs (#5F615E) standing on
edge, the back-of-house stack left in the corridor (US, 1975-1992). PRODUCTION obstacle
kit for Run! B (Kit_ChairDolly8), hand-built LOD0/1/2.

Dolly: a black steel channel frame 0.92 x 0.52 m on four 3 in rubber casters, a tube
end rack at +X. Chairs: 0.44 wide, 0.80 tall folded, 45 mm thick each, side by side
along X (the chair planes are YZ); tube legs, a pressed seat folded flat between them,
a curved back panel, rubber feet. ORIGIN = the floor under the dolly's centre.
FRONT = kit -Y (Unity +Z), the chairs' seat side. Box collider over the load.
Budget 12,000 / 4,800 / 900; lodDistances 4 / 20 / never (F6, stage C).
Slots: RunB_ChairGrey, Prop_SteelBlack, Prop_Rubber.
"""

import _run_b_common as B

NAME = "Kit_ChairDolly8"
SMOOTH_ANGLE = 40.0
BUDGET = (12000, 4800, 900)

GREY = "RunB_ChairGrey"
BLACK = "Prop_SteelBlack"
RUBBER = "Prop_Rubber"

DL, DW = 0.92, 0.52
DECK = 0.115            # top of the deck rails
N = 8
PITCH = 0.052
CH_W, CH_H = 0.44, 0.80


def _caster(kit, x, y, lv):
    r = 0.038
    B.cyl(kit, r, 0.028, (x, y, r), RUBBER, lv=lv, rot=(0, 90, 0), w=0.004, name="caster wheel", lo=24)
    if lv < 2:
        B.box(kit, (0.040, 0.050, 0.006), (x, y, DECK - 0.026), BLACK, w=0.0015, lv=lv, name="caster plate")
        for sx in (-1, 1):
            B.box(kit, (0.004, 0.030, 0.050), (x + sx * 0.018, y, r + 0.016), BLACK, w=0.0012, lv=lv, name="caster fork")


def _chair(kit, x, lv, k):
    yl = CH_W / 2 - 0.02
    z0 = DECK
    tr = 0.0110
    if lv == 2:
        kit.box((0.040, CH_W, CH_H), (x, 0, z0 + CH_H / 2), GREY, bevel=0.0, segments=0, name="chair")
        return
    # Back legs (rear plane) and front legs, crossing slightly when folded.
    for sy in (-1, 1):
        B.tube(kit, [(x + 0.012, sy * yl, z0), (x + 0.012, sy * yl, z0 + CH_H - 0.03), (x + 0.010, sy * (yl - 0.02), z0 + CH_H)], tr, GREY, lv=lv, name="back leg", lo=12)
        B.tube(kit, [(x - 0.010, sy * (yl - 0.012), z0), (x - 0.006, sy * (yl - 0.016), z0 + 0.70)], tr, GREY, lv=lv, name="front leg", lo=12)
        if lv == 0:
            for lx, ly in ((x + 0.012, sy * yl), (x - 0.010, sy * (yl - 0.012))):
                B.cyl(kit, tr + 0.0015, 0.018, (lx, ly, z0 + 0.009), RUBBER, lv=1, w=0.001, name="foot", lo=12)
    # Cross braces.
    B.tube(kit, [(x + 0.012, -yl, z0 + 0.10), (x + 0.012, yl, z0 + 0.10)], 0.0075, GREY, lv=lv, name="rear brace", lo=8)
    B.tube(kit, [(x - 0.008, -(yl - 0.014), z0 + 0.14), (x - 0.008, yl - 0.014, z0 + 0.14)], 0.0075, GREY, lv=lv, name="front brace", lo=8)
    # Back panel (curved in plan in life; a flat pressed panel with rolled edges here).
    B.box(kit, (0.014, CH_W - 0.03, 0.20), (x + 0.012, 0, z0 + CH_H - 0.13), GREY, w=0.004, lv=lv, name="back panel")
    # Seat folded flat between the legs.
    B.box(kit, (0.016, CH_W - 0.06, 0.38), (x + 0.001, 0, z0 + 0.42), GREY, w=0.005, lv=lv, rot=(0, 2.5 * ((k % 3) - 1), 0), name="seat")


def _level(kit, lv):
    bw, bs = B.bev(0.002, lv)
    # Deck frame: two long channels and three cross channels.
    for sy in (-1, 1):
        kit.box((DL, 0.040, 0.030), (0, sy * (DW / 2 - 0.02), DECK - 0.015), BLACK, bevel=bw, segments=bs, name="deck rail")
    for sx in (-1, 0, 1):
        if lv == 2 and sx == 0:
            continue
        kit.box((0.040, DW - 0.08, 0.026), (sx * (DL / 2 - 0.04), 0, DECK - 0.013), BLACK, bevel=bw, segments=bs, name="deck cross")
    # End rack at +X: a bent tube U.
    xr = DL / 2 - 0.02
    if lv == 2:
        kit.box((0.025, DW - 0.04, 0.025), (xr, 0, DECK + 0.70), BLACK, bevel=0.0, segments=0, name="rack")
    else:
        B.tube(kit, [(xr, -(DW / 2 - 0.03), DECK), (xr, -(DW / 2 - 0.03), DECK + 0.68), (xr, -(DW / 2 - 0.07), DECK + 0.72),
                     (xr, DW / 2 - 0.07, DECK + 0.72), (xr, DW / 2 - 0.03, DECK + 0.68), (xr, DW / 2 - 0.03, DECK)], 0.0127, BLACK, lv=lv, name="rack", lo=16)
    for sx in (-1, 1):
        for sy in (-1, 1):
            _caster(kit, sx * (DL / 2 - 0.07), sy * (DW / 2 - 0.06), lv)
    x0 = -((N - 1) * PITCH) / 2 - 0.04
    for k in range(N):
        _chair(kit, x0 + k * PITCH, lv, k)


def build(kit):
    B.levels(kit, _level)
    kit.collider((0, 0, (DECK + CH_H) / 2), (DL, DW, DECK + CH_H))
    kit.tag("run", "run_b", "obstacle")
    B.lod_meta(kit, (4.0, 20.0, 0.0), BUDGET)  # F6 (stage C, 2026-10-31): pops <= 2 %; was (4.0, 12.0, 0.0)
