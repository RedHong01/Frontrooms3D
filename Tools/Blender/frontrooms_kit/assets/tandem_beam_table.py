"""Kit_TandemBeam2Table: two seats and a beam table on one tandem beam (游戏视觉, bs4-hero-kits, builder A;
research/station/72_hero_kits.md §2.1). Replaces the two Kit_TaskChair_Navy stand-ins P30-P31 of
Station_Waiting_3x2 (row B along the S wall).

The period systems put tables in place of seats ("tables in place of seats", E11 1/1992; "plastic-laminate beam
tables and an ashtray", E12 12/1986), so row B is one kit: tandem_beam.py's frame (beam, T-legs at +-0.325,
brackets, arms, pads, the same wear rules) with the table at x -0.65 instead of a seat, seats at x 0 / +0.65,
arms at -0.325 / +0.325 / +0.975 (none at the table's outer end).

Beam table: oak-grain laminate top 0.50 x 0.457 x 0.029, top at 0.47 m (grain along x), on a seat bracket with
a 113 mm riser (51 square tube) and a 6 mm sub-plate; black PP T-mould round all four edges (3 mm bead); a flush
round ashtray at the front centre: chrome spun steel, D127 x 32, a 3 mm rolled rim resting on the laminate with
three U-notch rests at 120 degrees, grey ash in the bowl (Prop_AshSand; no butts here, to keep the slots down).

Slots: Prop_SteelBlack, Prop_PlasticBlack, Prop_VinylSlate, Prop_WoodOak, Prop_Chrome, Prop_AshSand (6 draws at
LOD0, over kitlib's 4: the oak top, the chrome bowl and its ash are the period beam table, 72 §2.1); LOD1 drops the
ash (5); LOD2 is steel, vinyl and oak (3; its arm caps take the steel slot). Budget 8,000 / 2,700 / 500 (cap
9,500); lodDistances [6, 15, 60]. Origin on the floor under the x 0 seat; front +Z. Variants _Navy, _Charcoal.
"""

import math

import bmesh
from mathutils import Vector

import _station_waiting_common as w
import tandem_beam as tb

NAME = "Kit_TandemBeam2Table"
SMOOTH_ANGLE = w.SMOOTH
VARIANTS = {"Kit_TandemBeam2Table_Navy": {w.SLATE: w.NAVY}, "Kit_TandemBeam2Table_Charcoal": {w.SLATE: w.CHARCOAL}}

TABLE_X = -0.65
TOP_Y1, TOP_T = 0.470, 0.029
TOP_Y0 = TOP_Y1 - TOP_T
TOP_W, TOP_D, TOP_R = 0.50, 0.457, 0.020
MOULD = 0.003
TRAY_Z = 0.08
TRAY_R = 0.0635
HOLE_R = 0.0605


def slab_with_hole(kit, name, slot, cx, cz, hw, hd, r, steps, fracs, y0, y1, hole, hole_r, hole_n, bottom_slot=None):
    """A horizontal slab with a round through-hole (top and bottom faces by scanfill). bottom_slot: the underside
    as its own part in that slot (the laminate's dark phenolic backer sheet, not a second oak face). Every face
    is oriented by rule (top up, underside down, the edge band away from the slab, the hole wall into the hole),
    so an open slab cannot come out inside-out."""
    outer = [(cx + x, cz + z) for (x, z, _nx, _nz) in w.rr_outline(hw, hd, [r] * 4, steps, fracs)]
    inner = [(hole[0] + hole_r * math.cos(2 * math.pi * i / hole_n), hole[1] + hole_r * math.sin(2 * math.pi * i / hole_n))
             for i in range(hole_n)]
    centre_b = Vector(w.B(cx, 0.0, cz))
    hole_b = Vector(w.B(hole[0], 0.0, hole[1]))

    def fill(bm, ring_o, ring_i, up):
        edges = []
        for ring in (ring_o, ring_i):
            n = len(ring)
            for i in range(n):
                edges.append(bm.edges.get((ring[i], ring[(i + 1) % n])) or bm.edges.new((ring[i], ring[(i + 1) % n])))
        res = bmesh.ops.triangle_fill(bm, use_beauty=True, use_dissolve=False, edges=edges)
        for f in [g for g in res["geom"] if isinstance(g, bmesh.types.BMFace)]:
            f.normal_update()
            if (f.normal.z > 0) != up:
                f.normal_flip()

    def wall(bm, a, b, away_from, outward):
        n = len(a)
        for i in range(n):
            j = (i + 1) % n
            f = bm.faces.new((a[i], a[j], b[j], b[i]))
            f.normal_update()
            d = f.calc_center_median() - away_from
            d.z = 0.0
            if (f.normal.dot(d) > 0) != outward:
                f.normal_flip()
    bm = bmesh.new()

    def loop(pts, y):
        return [bm.verts.new(w.B(px, y, pz)) for (px, pz) in pts]
    ot, ob = loop(outer, y1), loop(outer, y0)
    it, ib = loop(inner, y1), loop(inner, y0)
    wall(bm, ot, ob, centre_b, True)
    wall(bm, it, ib, hole_b, False)
    fill(bm, ot, it, True)
    if bottom_slot is None:
        fill(bm, ob, ib, False)
    obj = kit._new_object(name, bm, slot, "metres", "xz")
    if bottom_slot is not None:
        bm2 = bmesh.new()
        ob2 = [bm2.verts.new(w.B(px, y0, pz)) for (px, pz) in outer]
        ib2 = [bm2.verts.new(w.B(px, y0, pz)) for (px, pz) in inner]
        fill(bm2, ob2, ib2, False)
        kit._new_object(name + " backer", bm2, bottom_slot, "metres", "xz")
    return obj


def t_mould(kit, cx, cz, hw, hd, r, steps, prof, ym):
    """The PP T-mould: a bead profile [(s outward, u up)] swept round the core's edge (offset along the
    outline's own normals, so the corners stay true arcs)."""
    pts = w.rr_outline(hw, hd, [r] * 4, steps, [[], [], [], []])
    rings = []
    for (x, z, nx, nz) in pts:
        rings.append([(cx + x + nx * s, ym + u, cz + z + nz * s) for (s, u) in prof])
    return w.part_from_rings(kit, "t-mould", w.PLASTIC, rings + [rings[0]], closed=True)


def table(kit, x, lv):
    tb.seat_bracket(kit, x, lv, back=False, riser=TOP_Y0)
    hw, hd = TOP_W / 2, TOP_D / 2
    if lv == 2:
        w.box(kit, (TOP_W, TOP_T, TOP_D), (x, TOP_Y1 - TOP_T / 2, 0.0), w.OAK, bevel=0.0, segments=1, name="table top lod2")
        w.box(kit, (0.051, TOP_Y0 - tb.BEAM_TOP, 0.051), (x, (TOP_Y0 + tb.BEAM_TOP) / 2, tb.BEAM_Z), w.STEEL, bevel=0.0,
              segments=1, name="table riser lod2")
        return
    ym = TOP_Y1 - TOP_T / 2
    if lv == 0:
        core = slab_with_hole(kit, "table top", w.OAK, x, 0.0, hw - MOULD, hd - MOULD, TOP_R - MOULD, [4, 4, 4, 4],
                              [[0.5], [0.5], [0.5], [0.5]], TOP_Y0, TOP_Y1, (x, TRAY_Z), HOLE_R, 28, bottom_slot=w.PLASTIC)
        prof = [(-0.002, -0.0145), (0.0, -0.0145), (0.0012, -0.0136), (0.0024, -0.0098), (0.0030, 0.0), (0.0024, 0.0098),
                (0.0012, 0.0136), (0.0, 0.0145), (-0.002, 0.0145)]
        t_mould(kit, x, 0.0, hw - MOULD, hd - MOULD, TOP_R - MOULD, [4, 4, 4, 4], prof, ym)
    else:
        core = slab_with_hole(kit, "table top", w.OAK, x, 0.0, hw - MOULD, hd - MOULD, TOP_R - MOULD, [2, 2, 2, 2],
                              [[], [], [], []], TOP_Y0, TOP_Y1, (x, TRAY_Z), HOLE_R, 16, bottom_slot=w.PLASTIC)
        prof = [(-0.002, -0.0145), (0.0030, -0.012), (0.0030, 0.012), (-0.002, 0.0145)]
        t_mould(kit, x, 0.0, hw - MOULD, hd - MOULD, TOP_R - MOULD, [2, 2, 2, 2], prof, ym)
    core["fr_grain"] = "x"
    # flush ashtray: a rolled rim on the laminate, the bowl inside the hole, three U-notch rests at 120 degrees
    base = (x, 0.0, TRAY_Z)
    prof = [(0.0636, 0.4700), (0.0638, 0.4717), (0.0624, 0.4731), (0.0607, 0.4719), (0.0598, 0.4683), (0.0560, 0.4500),
            (0.0470, 0.4402), (0.0, 0.4392)]
    if lv == 0:
        notch_c = [math.radians(90.0 + 120.0 * k) for k in range(3)]
        half, fall, depth = 0.0062 / 0.062, 0.0035 / 0.062, 0.0046
        angs = [2 * math.pi * i / 28 for i in range(28)]
        extra = []
        for c in notch_c:
            for d in (-half - fall, -half, -half * 0.45, half * 0.45, half, half + fall):
                extra.append((c + d) % (2 * math.pi))

        def near_notch(a):
            return min(abs((a - c + math.pi) % (2 * math.pi) - math.pi) for c in notch_c)
        angs = [a for a in angs if near_notch(a) > half + fall + 0.03] + extra
        angs.sort()

        def dy(a, i):
            d = near_notch(a)
            if d >= half + fall:
                g = 0.0
            elif d <= half:
                g = 1.0
            else:
                g = 0.5 * (1 + math.cos(math.pi * (d - half) / fall))
            k = {0: 0.0, 1: 0.75, 2: 1.0, 3: 1.0, 4: 0.55, 5: 0.0}.get(i, 0.0)
            return -depth * g * k
        w.lathe(kit, "ashtray", w.CHROME, prof, base=base, angles=angs, dy=dy, cap_end=None)
        # the ash: a soft bed with a low heap, a little uneven
        aprof = [(0.0497, 0.4428), (0.034, 0.4441), (0.016, 0.4452), (0.0, 0.4457)]

        def ash_dy(a, i):
            return 0.0006 * math.sin(3 * a + 0.7) * (1 if i > 0 else 0) + 0.0004 * math.sin(7 * a) * (1 if i in (1, 2) else 0)
        w.lathe(kit, "ash", w.ASH, aprof, base=(x, 0.0, TRAY_Z), verts=20, dy=ash_dy)
        # the bowl's outside (seen only from under the table, through the hole)
        w.lathe(kit, "ashtray outer", w.CHROME, [(0.0601, 0.4700), (0.0562, 0.4495), (0.0472, 0.4396), (0.0, 0.4386)],
                base=base, verts=20)
    else:
        w.lathe(kit, "ashtray", w.CHROME, [prof[0], prof[2], prof[4], prof[6], prof[7]], base=base, verts=16)


def build(kit):
    tb.build_tandem(kit, seats=(0.0, 0.65), arms=(-0.325, 0.325, 0.975), sags=(0.007, 0.006), table_x=TABLE_X,
                    table_fn=table, arm_lod2_slot=w.STEEL, budget=(8000, 2700, 500),
                    note="front +Z; beam seating, beam table at x -0.65, seats at x 0 / +0.65")
    kit.support("table", w.B(TABLE_X, TOP_Y1, 0.0), (0.46, 0.42))
    kit.anchor("ashtray", w.B(TABLE_X, TOP_Y1, TRAY_Z))
