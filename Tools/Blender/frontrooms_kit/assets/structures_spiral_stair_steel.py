"""Kit_SpiralStair_Steel: B4-S, the one-cell steel spiral stair of Level 0 and the Office (P5 structures_stair_kits,
structures_assets G1-b, 2026-11-06; 30_plan.md S3-08, 20_research.md F6).

Period basis: the 1980s-90s steel spiral kit (a centre column of steel pipe, pie treads welded to hub sleeves stacked
on the column, open risers, balusters welded at the tread ends, a helical handrail): F6 "a steel kit in period practice:
a centre column, pie treads in checker plate or pans, balusters and a helical rail" (LS48). In an office suite the pans
carry the floor's carpet (the room's language, VISUAL_STANDARD 1.3: never industrial checker plate in a carpeted office);
the steel is the scene's own Painted_Metal (Level0.cs:40), _Putty (Office enamel) as a variant.

Geometry = the scene's B4-S exactly (Level0.cs:341, Level0Beyond.cs:221): treads from the column to r 1.25, 15 risers of
0.18 m, 14 treads of 23 degrees, the last riser at 322 degrees, the top floor at +2.70. UNITY frame: origin the column's
centre on the bottom floor; the first riser on local +X; the treads rise from +X toward +Z (counter-clockwise seen from
above); the builder places it with localRotation Euler(0, -startDeg, 0). Render only: the builder keeps its helical
walking ramp and its unseen collision post (no collider here). The column (OD 168 mm, 6-5/8 in) replaces the scene's
Ring post r 0.12 in NEW. Handrail 42.2 mm at 0.91 m over the nosing line (34-38 in), three 19 mm balusters a tread
(the 6 in sphere of the late-1980s codes), a heavier newel at the foot.
Budget LOD0 / LOD1 / LOD2 about 14 k / 4.5 k / 1.2 k; lodDistances 6 / 18 / 70 m.
"""

import math

import _run_b_common as B
import _struct_common as S

NAME = "Kit_SpiralStair_Steel"
SMOOTH_ANGLE = 40.0
BUDGET = (14000, 4500, 1200)
VARIANTS = {"Kit_SpiralStair_Steel_Putty": {"Painted_Metal": "Prop_SteelPutty"}}

STEEL, CARPET = "Painted_Metal", "L0_Carpet"
R_OUT = 1.25
STEP_DEG = 23.0
TREADS = 14
RISER = 0.18
COL_R = 0.0841
SLEEVE_R = 0.0905
RAIL_R_PATH = 1.212
RAIL_H = 0.91
BAL_R = 0.0095
PAN_T = 0.003
PAN_RIM = 0.012
INSERT_T = 0.010


def pol(r, deg, y):
    a = math.radians(deg)
    return (r * math.cos(a), y, r * math.sin(a))


def nosing_y(deg):
    return RISER * (deg / STEP_DEG + 1.0)


def annular_sector(m, r0, r1, a0, a1, y0, y1, n, top=True, bottom=True, inner=True, outer=True, ends=True):
    """A solid annular sector (Unity frame, angles in degrees from +X toward +Z) between heights y0 < y1, n facets."""
    def P(r, a, y):
        return pol(r, a, y)
    for k in range(n):
        aa = a0 + (a1 - a0) * k / n
        ab = a0 + (a1 - a0) * (k + 1) / n
        sa, sb = math.radians(aa), math.radians(ab)
        if top:
            m.quad(P(r0, aa, y1), P(r0, ab, y1), P(r1, ab, y1), P(r1, aa, y1),
                   (r0 * math.cos(sa), r0 * math.sin(sa)), (r0 * math.cos(sb), r0 * math.sin(sb)), (r1 * math.cos(sb), r1 * math.sin(sb)), (r1 * math.cos(sa), r1 * math.sin(sa)))
        if bottom:
            m.quad(P(r0, aa, y0), P(r1, aa, y0), P(r1, ab, y0), P(r0, ab, y0),
                   (r0 * math.cos(sa), r0 * math.sin(sa)), (r1 * math.cos(sa), r1 * math.sin(sa)), (r1 * math.cos(sb), r1 * math.sin(sb)), (r0 * math.cos(sb), r0 * math.sin(sb)))
        if outer:
            ua, ubb = r1 * sa, r1 * sb
            m.quad(P(r1, aa, y0), P(r1, aa, y1), P(r1, ab, y1), P(r1, ab, y0), (ua, y0), (ua, y1), (ubb, y1), (ubb, y0))
        if inner:
            ua, ubb = r0 * sa, r0 * sb
            m.quad(P(r0, aa, y0), P(r0, ab, y0), P(r0, ab, y1), P(r0, aa, y1), (ua, y0), (ubb, y0), (ubb, y1), (ua, y1))
    if ends:
        m.quad(P(r0, a0, y0), P(r0, a0, y1), P(r1, a0, y1), P(r1, a0, y0), (r0, y0), (r0, y1), (r1, y1), (r1, y0))
        m.quad(P(r0, a1, y0), P(r1, a1, y0), P(r1, a1, y1), P(r0, a1, y1), (r0, y0), (r1, y0), (r1, y1), (r0, y1))


def _level(kit, lv):
    n_arc = {0: 10, 1: 4, 2: 2}[lv]        # facets on a tread's 23 degrees (chord 1.25 x (1 - cos 1.15 deg) = 0.25 mm at LOD0)
    top_y = RISER * (TREADS + 1)
    # ---- column: floor plate, the pole, its cap
    plate_prof = [(0.150, 0.0), (0.150, 0.008), (0.146, 0.010), (0.0, 0.010)] if lv < 2 else [(0.150, 0.0), (0.150, 0.010), (0.0, 0.010)]
    S.lathe_y(kit, plate_prof, (0.0, 0.0, 0.0), STEEL, lv, name="floor plate")
    col_top = top_y + RAIL_H + 0.04
    S.lathe_y(kit, [(COL_R, 0.0), (COL_R, col_top)], (0.0, 0.0, 0.0), STEEL, lv, name="column", close_top=False, close_bottom=False)
    cap = [(COL_R + 0.003, col_top), (COL_R + 0.003, col_top + 0.006), (COL_R * 0.8, col_top + 0.016), (COL_R * 0.35, col_top + 0.022), (0.0, col_top + 0.023)]
    if lv >= 1:
        cap = [(COL_R + 0.003, col_top), (COL_R + 0.003, col_top + 0.008), (0.0, col_top + 0.020)]
    S.lathe_y(kit, cap, (0.0, 0.0, 0.0), STEEL, lv, name="column cap")
    if lv < 2:
        for k in range(4):
            a = math.radians(45 + 90 * k)
            S.hex_nut(kit, 0.024, 0.010, (0.115 * math.cos(a), 0.015, 0.115 * math.sin(a)), "y", STEEL, lv, name="anchor nut")
    # ---- treads
    for i in range(TREADS):
        a0, a1 = STEP_DEG * i, STEP_DEG * (i + 1)
        y = RISER * (i + 1)
        # hub sleeve under the tread (the stacked spacer that sets the rise), with a chamfer line at its top
        hub = [(SLEEVE_R, y - RISER + 0.001), (SLEEVE_R, y - 0.002), (SLEEVE_R - 0.002, y)] if lv == 0 else [(SLEEVE_R, y - RISER + 0.001), (SLEEVE_R, y)]
        S.lathe_y(kit, hub, (0.0, 0.0, 0.0), STEEL, lv, name="hub sleeve", close_top=False, close_bottom=False)
        # the pan: bottom plate, a rim round the carpet, the carpet insert
        m = S.Mesh()
        annular_sector(m, SLEEVE_R, R_OUT, a0, a1, y - PAN_RIM - PAN_T, y - PAN_RIM, n_arc, top=False)
        m.part(kit, "tread pan", STEEL)
        if lv < 2:
            rim = S.Mesh()
            annular_sector(rim, R_OUT - 0.003, R_OUT, a0, a1, y - PAN_RIM, y + 0.002, n_arc, inner=False, bottom=False)
            # the leading edge's nosing: a formed lip 3 mm, 38 mm down from the top, along the leading radius
            annular_sector(rim, SLEEVE_R, R_OUT, a0, a0 + 0.14 * (1.25 / 1.25), y - 0.038, y + 0.002, 1, inner=False, outer=False)
            annular_sector(rim, SLEEVE_R, R_OUT, a1 - 0.14, a1, y - PAN_RIM, y + 0.002, 1, inner=False, outer=False, bottom=False)
            rim.part(kit, "tread rim", STEEL)
        ins = S.Mesh()
        annular_sector(ins, SLEEVE_R + 0.002, R_OUT - 0.003, a0 + 0.14, a1 - 0.14, y - PAN_RIM, y, n_arc, bottom=False, inner=False)
        ins.part(kit, "carpet insert", CARPET)
        # the stiffener under the tread: a 50 x 6 flat bar on edge under the leading radius
        if lv < 2:
            st = S.Mesh()
            aa = a0 + 1.2
            r_in, r_end = SLEEVE_R, R_OUT - 0.08
            y_top = y - PAN_RIM - PAN_T
            hw = math.degrees(0.003 / ((r_in + r_end) / 2))
            annular_sector(st, r_in, r_end, aa - hw, aa + hw, y_top - 0.050, y_top, 1, top=False)
            st.part(kit, "tread stiffener", STEEL)
        # balusters on the tread's outer end
        for f in (1.0 / 6.0, 0.5, 5.0 / 6.0):
            ang = a0 + STEP_DEG * f
            yb = y + 0.002
            yt = nosing_y(ang) + RAIL_H - S.TUBE_R + 0.004
            if lv == 2 and f != 0.5:
                continue
            c = pol(RAIL_R_PATH, ang, (yb + yt) / 2)
            # welded ends: no bevel (kitlib straight, so the hero rule's 0.8 mm minimum does not apply to a weld)
            kit.cylinder(BAL_R, yt - yb, S.ub(c), STEEL, verts=S.segs(BAL_R, lv, 6), bevel=0.0, segments=0, name="baluster")
    # ---- the newel at the foot (a 1-1/2 in post from the floor to the rail at the first tread's leading corner)
    ny = nosing_y(-1.0) + RAIL_H
    S.cyl_y(kit, 0.0241, ny - 0.0, pol(RAIL_R_PATH, -1.0, ny / 2), STEEL, lv, w=0.001 if lv == 0 else 0.0, name="newel", lo=12)
    if lv < 2:
        S.lathe_y(kit, [(0.040, 0.0), (0.040, 0.006), (0.030, 0.010), (0.0, 0.010)], pol(RAIL_R_PATH, -1.0, 0.0), STEEL, lv, name="newel foot")
    # ---- the helical handrail, an end cap at each end
    step = {0: 3.0, 1: 7.5, 2: 15.0}[lv]
    a_end = STEP_DEG * TREADS + 6.0
    a_start = -1.0
    n = int(math.ceil((a_end - a_start) / step))
    path = []
    for k in range(n + 1):
        ang = a_start + (a_end - a_start) * k / n
        path.append(pol(RAIL_R_PATH, ang, nosing_y(min(ang, STEP_DEG * TREADS)) + RAIL_H))
    sides = {0: 20, 1: 10, 2: 6}[lv]
    rail = S.Mesh()
    S.sweep_section(rail, path, S.circle(S.TUBE_R, sides), cap0=True, cap1=True)
    rail.part(kit, "handrail", STEEL)
    if lv == 0:
        # a short top return post where the rail meets the landing guard (the last baluster, heavier)
        ang = STEP_DEG * TREADS + 5.0
        yt = nosing_y(STEP_DEG * TREADS) + RAIL_H
        S.cyl_y(kit, BAL_R * 1.25, yt - top_y, pol(RAIL_R_PATH, ang, (top_y + yt) / 2), STEEL, lv, w=0.0008, name="top baluster", lo=8)


def build(kit):
    B.levels(kit, _level)
    kit.no_collider()
    kit.min_ceiling = 0.0
    S.finish(kit, (6.0, 18.0, 70.0), BUDGET, tags=("structures", "stair", "p5", "b4s"))
    kit.anchor("column", S.ub((0.0, 0.0, 0.0)))
    kit.anchor("top_floor", S.ub((0.0, RISER * (TREADS + 1), 0.0)))
    kit.meta["spiral"] = {"treads": TREADS, "riser": RISER, "stepDeg": STEP_DEG, "rOut": R_OUT, "columnOD": 2 * COL_R,
                          "railHeight": RAIL_H, "firstRiserAxis": "+X", "rises": "counter-clockwise from +X toward +Z",
                          "place": "localRotation = Euler(0, -startDeg, 0)"}
