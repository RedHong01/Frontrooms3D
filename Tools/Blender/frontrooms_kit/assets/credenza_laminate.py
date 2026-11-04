"""Kit_Credenza_Laminate: the beige laminate credenza under the key cabinet on the N wall of Station_Desk_3x3
(D' had a credenza under the set; dropped when BS4 hung the set on the bracket), game visual line, workflow
bs4-hero-kits (research/station/72_hero_kits.md section 2.6; 30_look S04 and section 2.2 beige laminate, chrome
pulls; E04, Contract Nov 1989: desk, credenza, file and pedestal lines in six standard laminate colours). New mesh
under the planned name, not a slot swap of Kit_Credenza (cherry ogee top, routed fronts, brass bails). It is the
companion of Kit_OfficeDesk (office_desk.py: beige laminate top at the same height, the rounded laminate self-edge
of Red's target office image, EDGE = LAM), so the two read as one suite.

60 x 20 x 29.2 in (1.524 x 0.508 x 0.741 m):
* top 1-1/8 in (29 mm) beige HPL, rounded self-edge 2.5 mm in 2 segments (as the desk), overhanging the sides 12 mm
  and the front 15 mm, flush at the back;
* case: 19 mm laminate-faced sides, self-edged (1.5 mm eased) on every arris, a case bottom, a tempered hardboard
  back recessed 10 mm;
* a pedestal at each end, 0.40 m wide: a 6 in box drawer over a file drawer; two hinged doors between them on
  concealed hinges (none visible);
* fronts full overlay with 3 mm reveals, 19 mm, front arris eased 1.5 mm in 2 facets; one dark quad behind the
  shut lines (_newcase.shadow, Prop_Vinyl) so the reveals read under flat top light;
* chrome wire D-pulls: 8 mm wire (12 sides) on 4 in (102 mm) centres, 32 mm projection, 8 mm bend radius,
  12 mm round rosettes; horizontal on the drawers, vertical on the doors at the meeting stiles;
* chrome lock cores (19 mm bezel, the plug 0.5 mm down inside it, a dark keyway) on the two box drawers;
* black vinyl toe kick 3 in (76 mm) high, 2 in (51 mm) recess, returning on the sides; a rear plinth board.

Unity space; ORIGIN = floor, centred; front +Z. Slots Prop_LaminateBeige, Prop_Chrome, Prop_Vinyl (toe kick,
reveal shadow, keyways), Prop_Hardboard (back): 4 draws. Hand-built LODs: LOD1 plain plates, 8-sided pulls with
one facet per bend, no rosettes or locks; LOD2 three boxes. Budget 3,500 / 1,300 / 200 (cap 5,300);
lodDistances [5, 12, 60].
"""

import _station_common as sc
import _station_desk_common as dc

NAME = "Kit_Credenza_Laminate"
SMOOTH_ANGLE = 50.0           # 2-segment self-edges and the 12-sided wire shade round; 90 deg corners stay sharp

LAM, CHROME, VINYL, BACK = "Prop_LaminateBeige", "Prop_Chrome", "Prop_Vinyl", "Prop_Hardboard"
B = sc.B

W, D, H = 1.524, 0.508, 0.741
TOP_T = 0.029
Y_TOP0 = H - TOP_T                    # 0.712
X_SIDE_O = W / 2 - 0.012              # 0.750
SIDE_T = 0.019
X_SIDE_I = X_SIDE_O - SIDE_T          # 0.731
Z_FACE = D / 2 - 0.015                # 0.239 front faces
FRONT_T = 0.019
Z_CARC = Z_FACE - FRONT_T             # 0.220 carcass front plane
Z_BACK = -D / 2                       # -0.254
TOE_H, TOE_IN = 0.076, 0.051
REV = 0.003
X_FRONT = X_SIDE_O - 0.0015           # fronts stop 1.5 mm in from the side faces
Y_F0, Y_F1 = TOE_H + 0.0015, Y_TOP0 - REV
PED_W = 0.40
X_PED_IN = X_FRONT - (PED_W - 0.0015)  # 0.350 inner edge of a pedestal front
BOX_H = 0.152
Y_BOX0 = Y_F1 - BOX_H

WIRE_R, PULL_C, PULL_P, PULL_RB = 0.004, 0.102, 0.032, 0.008


def bl(p):
    return B(p[0], p[1], p[2])


def g(obj, lods):
    return dc.g1(obj, SMOOTH_ANGLE, lods)


def front(kit, x0, x1, y0, y1, steps, lods, name):
    """A front plate between x0..x1 / y0..y1, its back on the carcass plane, built out to Z_FACE."""
    w, h = abs(x1 - x0), abs(y1 - y0)
    c = bl(((x0 + x1) / 2, (y0 + y1) / 2, Z_CARC))
    o = dc.plate(kit, w, h, steps, c, bl((0.0, 0.0, 1.0)), bl((0.0, 1.0, 0.0)), LAM, name=name)
    return g(o, lods)


def d_pull(kit, cx, cy, horizontal, lod):
    half = PULL_C / 2

    def p(a, z):
        return (cx + a, cy, z) if horizontal else (cx, cy + a, z)

    poly = [p(-half, Z_FACE - 0.003), p(-half, Z_FACE + PULL_P - WIRE_R), p(half, Z_FACE + PULL_P - WIRE_R),
            p(half, Z_FACE - 0.003)]
    if lod == 0:
        pts = dc.fillet(poly, PULL_RB, step=30.0)
        dc.lv(dc.tube(kit, [bl(q) for q in pts], WIRE_R, CHROME, sides=12, caps=False, name="d-pull wire"), "0")
        prof = [(0.0060, 0.0), (0.0060, 0.0012), (0.0052, 0.0026), (0.0040, 0.0030)]
        for a in (-half, half):
            ro = dc.lathe_axis(kit, prof, bl(p(a, Z_FACE)), bl((0.0, 0.0, 1.0)), CHROME, verts=12, name="pull rosette",
                               close_bottom=False, close_top=True)
            g(ro, "0")
    else:
        pts = dc.fillet(poly, PULL_RB, step=90.0)
        dc.lv(dc.tube(kit, [bl(q) for q in pts], WIRE_R, CHROME, sides=8, caps=False, name="d-pull wire lod1"), "1")


def lock_core(kit, x, y):
    prof = [(0.0095, 0.0), (0.0095, 0.0012), (0.0088, 0.0024), (0.0068, 0.0026), (0.0066, 0.0021), (0.0, 0.0021)]
    o = dc.lathe_axis(kit, prof, bl((x, y, Z_FACE)), bl((0.0, 0.0, 1.0)), CHROME, verts=16, name="lock core",
                      close_bottom=False)
    g(o, "0")
    dc.lv(sc.box(kit, (0.0016, 0.0070, 0.0006), (x, y, Z_FACE + 0.0021), VINYL, bevel=0.0, name="keyway"), "0")


def build(kit):
    sc.register()
    eased = dc.rounded_steps(FRONT_T, 0.0015, segs=2)

    # ---------------- LOD0
    g(sc.box(kit, (W, TOP_T, D), (0.0, Y_TOP0 + TOP_T / 2, 0.0), LAM, bevel=0.0025, segments=2, name="top"), "0")
    side_h = Y_TOP0 - TOE_H
    side_d = Z_CARC - Z_BACK
    for s in (-1, 1):
        g(sc.box(kit, (SIDE_T, side_h, side_d), (s * (X_SIDE_I + SIDE_T / 2), TOE_H + side_h / 2, (Z_CARC + Z_BACK) / 2),
                 LAM, bevel=0.0015, segments=2, name="case side"), "0")
    dc.lv(sc.box(kit, (2 * X_SIDE_I, 0.019, Z_CARC - (Z_BACK + 0.010)), (0.0, TOE_H + 0.0095, (Z_CARC + Z_BACK + 0.010) / 2),
                 LAM, bevel=0.0, name="case bottom"), "0")
    dc.lv(sc.box(kit, (2 * X_SIDE_I, Y_TOP0 - TOE_H - 0.019, 0.004),
                 (0.0, (Y_TOP0 + TOE_H + 0.019) / 2, Z_BACK + 0.012), BACK, bevel=0.0, name="hardboard back"), "01")
    # plinth: toe kick and returns (black vinyl), rear board
    zt = Z_FACE - TOE_IN
    xr = X_SIDE_O - TOE_IN
    g(sc.box(kit, (2 * xr, TOE_H, 0.018), (0.0, TOE_H / 2, zt - 0.009), VINYL, bevel=0.001, segments=1, name="toe kick"), "0")
    for s in (-1, 1):
        g(sc.box(kit, (0.018, TOE_H, zt - 0.018 - (Z_BACK + 0.010)), (s * (xr - 0.009), TOE_H / 2, (zt - 0.018 + Z_BACK + 0.010) / 2),
                 VINYL, bevel=0.001, segments=1, name="toe return"), "0")
    dc.lv(sc.box(kit, (2 * (xr - 0.018), TOE_H, 0.018), (0.0, TOE_H / 2, Z_BACK + 0.019), VINYL, bevel=0.0,
                 name="rear plinth"), "0")
    # reveal shadow behind the shut lines
    dc.lv(sc.quad(kit, 2 * X_FRONT, Y_TOP0 - TOE_H, (0.0, (Y_TOP0 + TOE_H) / 2, Z_CARC - 0.0005), VINYL, facing="+z",
                  uv="metres", name="reveal shadow"), "01")
    # fronts
    for s in (-1, 1):
        xo, xi = s * X_FRONT, s * X_PED_IN
        front(kit, xi, xo, Y_BOX0, Y_F1, eased, "0", "box drawer")
        front(kit, xi, xo, Y_F0, Y_BOX0 - REV, eased, "0", "file drawer")
        front(kit, s * (REV / 2), s * (X_PED_IN - REV), Y_F0, Y_F1, eased, "0", "door")
        pcx = s * (X_FRONT + X_PED_IN) / 2
        d_pull(kit, pcx, (Y_BOX0 + Y_F1) / 2, True, 0)
        d_pull(kit, pcx, Y_BOX0 - REV - 0.085, True, 0)
        d_pull(kit, s * (REV / 2 + 0.042), Y_F1 - 0.105, False, 0)
        lock_core(kit, pcx + s * 0.125, (Y_BOX0 + Y_F1) / 2)

    # ---------------- LOD1: plain plates, 8-sided pulls, no rosettes or locks
    chamfer = [(0.0, 0.0), (0.0, FRONT_T - 0.0015), (0.0015, FRONT_T)]
    dc.lv(sc.box(kit, (W, TOP_T, D), (0.0, Y_TOP0 + TOP_T / 2, 0.0), LAM, bevel=0.0, name="top lod1"), "1")
    for s in (-1, 1):
        dc.lv(sc.box(kit, (SIDE_T, side_h, side_d), (s * (X_SIDE_I + SIDE_T / 2), TOE_H + side_h / 2, (Z_CARC + Z_BACK) / 2),
                     LAM, bevel=0.0, name="case side lod1"), "1")
    dc.lv(sc.box(kit, (2 * xr, TOE_H, Z_FACE - TOE_IN - Z_BACK - 0.010), (0.0, TOE_H / 2, (zt + Z_BACK + 0.010) / 2), VINYL,
                 bevel=0.0, name="plinth lod1"), "1")
    for s in (-1, 1):
        xo, xi = s * X_FRONT, s * X_PED_IN
        front(kit, xi, xo, Y_BOX0, Y_F1, chamfer, "1", "box drawer lod1")
        front(kit, xi, xo, Y_F0, Y_BOX0 - REV, chamfer, "1", "file drawer lod1")
        front(kit, s * (REV / 2), s * (X_PED_IN - REV), Y_F0, Y_F1, chamfer, "1", "door lod1")
        pcx = s * (X_FRONT + X_PED_IN) / 2
        d_pull(kit, pcx, (Y_BOX0 + Y_F1) / 2, True, 1)
        d_pull(kit, pcx, Y_BOX0 - REV - 0.085, True, 1)
        d_pull(kit, s * (REV / 2 + 0.042), Y_F1 - 0.105, False, 1)

    # ---------------- LOD2: three boxes
    dc.lv(sc.box(kit, (W, TOP_T, D), (0.0, Y_TOP0 + TOP_T / 2, 0.0), LAM, bevel=0.0, name="top lod2"), "2")
    dc.lv(sc.box(kit, (2 * X_SIDE_O, Y_TOP0 - TOE_H, Z_FACE - Z_BACK), (0.0, (Y_TOP0 + TOE_H) / 2, (Z_FACE + Z_BACK) / 2),
                 LAM, bevel=0.0, name="case lod2"), "2")
    dc.lv(sc.box(kit, (2 * xr, TOE_H, zt - Z_BACK - 0.010), (0.0, TOE_H / 2, (zt + Z_BACK + 0.010) / 2), VINYL,
                 bevel=0.0, name="plinth lod2"), "2")

    # ---------------- metadata (72 section 2.6)
    kit.collider(B(0.0, H / 2, 0.0), sc.BS(W, H, D))
    kit.support("top", B(0.0, H, 0.0), (1.48, 0.46))
    kit.anchor("top", B(0.0, H, 0.0))
    kit.tag("station", "case_goods", "office")
    sc.lods(kit, 5.0, 12.0, 60.0, (3500, 1300, 200))
    dc.station_block(kit, "floor, centred", "front (+Z) = the drawer faces; Station_Desk_3x3 N wall under the key "
                     "cabinet (x 6.36, z 8.651, yaw 180); back 15 mm off the wall, top under the crash rail")
