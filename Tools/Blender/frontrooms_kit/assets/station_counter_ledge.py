"""Kit_StationCounter_Ledge120: the sign-in ledge on the W wall of Station_Desk_3x3 (D' rev 4 "the ledge in the
W wall's middle"; BS4 placed none), game visual line, workflow bs4-hero-kits (research/station/72_hero_kits.md
section 2.5; 01 section 1 transaction ledge ~42 in, guides 42-48 in (P33, P34), depth 10-14 in; 03_palette
reception counter CONFIRMED, oak with grey laminate (E02, E10, E19); colour-through laminate of the 1980s (P36);
30_look S05). The Run120 visitor-ledge section (station_counter.py, read only: LEDGE_TOP 1.07, LEDGE_T 0.032, the
12 mm PVC bullnose, TOE_H 0.10, TOE_SET 0.05) on its own wall-hung base:

* oak top 32 mm (y 1.038-1.07), z 0.001 (1 mm scribe to the wall) to 0.318, grain along the run;
* dark PVC bullnose to z 0.330 (the Run120 half-ellipse, 15 deg facets), 2 mm PVC bands on the oak's ends;
* a 12 x 25 mm oak cleat under the top at the wall carries it: nothing proud on top, so the register lies flat;
* grey colour-through laminate base z 0.035-0.27 (clears the 28 mm crash rail): 19 mm end panels from the wall to
  the fascia, scribed round the station trim that runs on behind the base (a 32 x 160 mm notch for the crash rail,
  y 0.775-0.935, and a 10 x 105 mm notch for the base, y 0-0.105), 12 mm in under the top's ends;
* 19 mm fascia panels with a 3 mm shadow gap under the top and one centre reveal 4 x 2 mm (dark backing strips so
  both lines read under flat top light, as _newcase.shadow);
* black vinyl toe kick 0.10 m tall, recessed 0.05 m, with a cove foot; a back panel and a bottom board close the box
  (seen only in turntables).

Unity space; ORIGIN = THE WALL FACE PLANE AT FLOOR LEVEL, centred on the run (host convention of
interact_key_board.py); front +Z into the room. 1.20 x 1.07 x 0.33 m. Slots Prop_WoodOak (fr_grain x, grain on the
scan's U via _newcase.grain_on_u), Prop_PVCEdge, Prop_LaminateGrey, Prop_Vinyl: 4 draws. Hand-built LODs: LOD1 no
bevels, no reveal or shadow strips, no end bands; LOD2 four boxes. Budget 3,000 / 1,200 / 150 (cap 3,500);
lodDistances [4, 10, 60].
"""

import math

import _newcase as nc
import _station_common as sc
import _station_desk_common as dc

NAME = "Kit_StationCounter_Ledge120"
SMOOTH_ANGLE = 50.0           # 3-segment laminate bevels and 1-segment band chamfers shade round; 90 deg corners stay sharp

OAK, EDGE, GREY, VINYL = "Prop_WoodOak", "Prop_PVCEdge", sc.LAMINATE_GREY, "Prop_Vinyl"

# Run120's visitor-ledge constants (station_counter.py)
LEDGE_TOP, LEDGE_T = 1.07, 0.032
NOSE = 0.012                  # bullnose depth (half-ellipse 12 x 32 mm)
TOE_H, TOE_SET = 0.10, 0.05

W = 1.20
HW = W / 2
Z_TOP0, Z_TOP1 = 0.001, 0.330 - NOSE          # oak: wall scribe to the nosing
Y_TOP0 = LEDGE_TOP - LEDGE_T                  # 1.038
END_T, END_IN = 0.019, 0.012                  # end panels, set in under the top's ends
X_END_O = HW - END_IN                         # 0.588 outer face
X_END_I = X_END_O - END_T                     # 0.569 inner face
Z_FACE = 0.27                                 # fascia / end panel front plane
FASCIA_T = 0.019
Z_BACK = 0.035                                # base back (clears the 28 mm rail)
GAP = 0.003                                   # shadow gap under the top
REV_W, REV_D = 0.004, 0.002                   # centre reveal


def g(obj, lods):
    return dc.g1(obj, SMOOTH_ANGLE, lods)


def end_outline():
    """End panel side profile (z, y): from the wall to the fascia plane, floor to the top's underside, notched
    round the crash rail and the base trim at the wall edge."""
    return [(0.010, 0.0), (Z_FACE, 0.0), (Z_FACE, Y_TOP0), (0.0, Y_TOP0), (0.0, 0.935), (0.032, 0.935),
            (0.032, 0.775), (0.0, 0.775), (0.0, 0.105), (0.010, 0.105)]


def nose_outline(n):
    pts = []
    for k in range(n + 1):
        a = math.radians(-90.0 + 180.0 * k / n)
        pts.append((Z_TOP1 + NOSE * math.cos(a), Y_TOP0 + LEDGE_T / 2 + LEDGE_T / 2 * math.sin(a)))
    return pts


def toe_outline():
    """Vinyl cove base on a 12 mm kick board: the face at z 0.22 with a 6 mm cove foot and a tapered top lip."""
    zf = Z_FACE - TOE_SET
    pts = [(zf - 0.012, 0.0), (zf + 0.007, 0.0), (zf + 0.007, 0.0012)]
    for k in range(1, 5):
        a = math.radians(90.0 * k / 4)
        pts.append((zf + 0.007 - 0.007 * math.sin(a), 0.0012 + 0.0068 * (1 - math.cos(a))))
    pts += [(zf, TOE_H - 0.004), (zf - 0.0015, TOE_H), (zf - 0.012, TOE_H)]
    return pts


def build(kit):
    sc.register()
    dc.register()
    nc.grain_on_u(kit)

    # ---------------- LOD0
    top = sc.box(kit, (2 * (HW - 0.002), LEDGE_T, Z_TOP1 - Z_TOP0), (0.0, Y_TOP0 + LEDGE_T / 2, (Z_TOP0 + Z_TOP1) / 2),
                 OAK, bevel=0.0, name="oak top")
    top["fr_grain"] = "x"
    # ease only the top's back arris at the wall (its other edges meet the nosing and the end bands)
    hb = (Z_TOP1 - Z_TOP0) / 2 - 1e-5
    dc.bevel_where(top, lambda co: co.z > LEDGE_T / 2 - 1e-5 and co.y > hb, 0.001, 2)
    g(top, "0")
    nose = sc.extrude_zy(kit, nose_outline(18), W, 0.0, EDGE, bevel=0.001, segments=2, name="pvc bullnose")   # 1 mm cut ends
    g(nose, "0")
    for s in (-1, 1):
        g(sc.box(kit, (0.002, LEDGE_T, Z_TOP1 - Z_TOP0), (s * (HW - 0.001), Y_TOP0 + LEDGE_T / 2, (Z_TOP0 + Z_TOP1) / 2),
                 EDGE, bevel=0.0008, segments=2, name="pvc end band"), "0")   # 2 mm band: arrises eased 0.8 mm
    cleat = sc.box(kit, (2 * X_END_I, 0.025, 0.012), (0.0, Y_TOP0 - 0.0125, Z_TOP0 + 0.006), OAK, bevel=0.0008,
                   segments=1, name="oak cleat")
    cleat["fr_grain"] = "x"
    g(cleat, "0")             # G1: its 45 deg arris chamfers would otherwise smooth-shade the whole 25 mm face
    for s in (-1, 1):
        ep = sc.extrude_zy(kit, end_outline(), END_T, s * (X_END_I + END_T / 2), GREY, bevel=0.0015, segments=3,
                           name="end panel")
        g(dc.tri(ep), "0")
    fy0, fy1 = TOE_H, Y_TOP0 - GAP
    for s in (-1, 1):
        x0, x1 = REV_W / 2, X_END_I
        g(sc.box(kit, (x1 - x0, fy1 - fy0, FASCIA_T), (s * (x0 + x1) / 2, (fy0 + fy1) / 2, Z_FACE - FASCIA_T / 2), GREY,
                 bevel=0.0015, segments=3, name="fascia panel"), "0")
    dc.lv(sc.box(kit, (REV_W + 0.004, fy1 - fy0, 0.002), (0.0, (fy0 + fy1) / 2, Z_FACE - REV_D - 0.001), VINYL,
                 bevel=0.0, name="reveal backing"), "0")
    dc.lv(sc.box(kit, (2 * X_END_I, GAP + 0.002, 0.002), (0.0, fy1 + (GAP + 0.002) / 2 - 0.001, Z_FACE - FASCIA_T - 0.001),
                 VINYL, bevel=0.0, name="shadow gap backing"), "0")
    g(sc.extrude_zy(kit, toe_outline(), 2 * X_END_I, 0.0, VINYL, bevel=0.0, name="vinyl toe"), "0")   # G1: flat face, round cove
    dc.lv(sc.box(kit, (2 * X_END_I, Y_TOP0, 0.006), (0.0, Y_TOP0 / 2, Z_BACK + 0.003), GREY, bevel=0.0,
                 name="back panel"), "01")
    dc.lv(sc.box(kit, (2 * X_END_I, 0.012, Z_FACE - FASCIA_T - Z_BACK - 0.006),
                 (0.0, TOE_H - 0.006, (Z_BACK + 0.006 + Z_FACE - FASCIA_T) / 2), GREY, bevel=0.0, name="bottom board"),
          "01")

    # ---------------- LOD1: the same planes, no bevels, no reveal, strips or bands
    t1 = sc.box(kit, (W, LEDGE_T, Z_TOP1 - Z_TOP0), (0.0, Y_TOP0 + LEDGE_T / 2, (Z_TOP0 + Z_TOP1) / 2), OAK, bevel=0.0,
                name="oak top lod1")
    t1["fr_grain"] = "x"
    dc.lv(t1, "1")
    dc.lv(dc.tri(sc.extrude_zy(kit, nose_outline(6), W, 0.0, EDGE, bevel=0.0, name="pvc bullnose lod1")), "1")   # no n-gon caps (FBX tangents)
    for s in (-1, 1):
        dc.lv(dc.tri(sc.extrude_zy(kit, end_outline(), END_T, s * (X_END_I + END_T / 2), GREY, bevel=0.0,
                                   name="end panel lod1")), "1")
    dc.lv(sc.box(kit, (2 * X_END_I, fy1 - fy0, FASCIA_T), (0.0, (fy0 + fy1) / 2, Z_FACE - FASCIA_T / 2), GREY, bevel=0.0,
                 name="fascia lod1"), "1")
    dc.lv(sc.box(kit, (2 * X_END_I, TOE_H, 0.012), (0.0, TOE_H / 2, Z_FACE - TOE_SET - 0.006), VINYL, bevel=0.0,
                 name="toe lod1"), "1")

    # ---------------- LOD2: four boxes
    t2 = sc.box(kit, (W, LEDGE_T, 0.33 - Z_TOP0), (0.0, Y_TOP0 + LEDGE_T / 2, (Z_TOP0 + 0.33) / 2), OAK, bevel=0.0,
                name="top lod2")
    t2["fr_grain"] = "x"
    dc.lv(t2, "2")
    dc.lv(sc.box(kit, (2 * X_END_O, Y_TOP0 - TOE_H, Z_FACE - Z_BACK), (0.0, (Y_TOP0 + TOE_H) / 2, (Z_FACE + Z_BACK) / 2),
                 GREY, bevel=0.0, name="body lod2"), "2")
    dc.lv(sc.box(kit, (2 * X_END_O, TOE_H, Z_FACE - TOE_SET - Z_BACK), (0.0, TOE_H / 2, (Z_FACE - TOE_SET + Z_BACK) / 2),
                 VINYL, bevel=0.0, name="toe lod2"), "2")

    # ---------------- metadata (72 section 2.5)
    kit.collider(sc.B(0.0, 0.535, 0.165), sc.BS(1.20, 1.07, 0.33))
    kit.support("ledge", sc.B(0.0, LEDGE_TOP, 0.175), (1.16, 0.28))
    kit.anchor("register", sc.B(-0.22, LEDGE_TOP, 0.165))
    kit.anchor("tent_card", sc.B(0.36, LEDGE_TOP, 0.20))
    kit.tag("station", "counter")
    sc.lods(kit, 4.0, 10.0, 60.0, (3000, 1200, 150))
    dc.station_block(kit, "wall face plane at floor level, centred on the run",
                     "front (+Z) = the visitor face; Station_Desk_3x3 W wall middle (x 0.08, z 4.50, yaw 90) with "
                     "Kit_RegisterBook and Kit_TentCard on the anchors")
