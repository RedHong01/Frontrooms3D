"""Kit_StationCounter_Run120: one 1.20 m run of the security counter (30L S05, S20, §2.1; 61 §4.4 counter row and §5
柜台正面; 03_palette.md counter CONFIRMED; plan 70 P09, plan check C1-14).

Section inside 61's 0.60 m rect (local z -0.30 staff .. +0.30 visitor; the module turns it yaw 180 so the visitor
face looks south at D):
- visitor transaction ledge, oak with a PVC nosing, top at 1.07 m, 0.30 deep over the fascia (z 0.00..+0.32);
- staff work surface, beige laminate, top at 0.74 m, running back to the fascia under the ledge (z -0.30..+0.27,
  so the phone, the blotter, the DAR, the cup, the ashtray and the charger fit; 30L S05's section);
- grey laminate fascia #8E8E88 (Prop_LaminateGrey) from the toe to the ledge, a black 0.10 m toe recessed 0.05 m;
- knee space under the work surface; steel ledge brackets and a back apron;
- under the ledge (staff side): a dead door-release button and an intercom master (S20), on the 1.20 run only.
ORIGIN = floor, centred on the run. Box collider = the counter's rect. Budget 3,500 / 1,200 / 150.
Slots Prop_LaminateGrey, Prop_WoodOak, Prop_LaminateBeige, Prop_Vinyl (+ Prop_PVCEdge, Prop_SteelBlack, the
intercom's Prop_PlasticBeige): kept to the counter's real finishes.
"""

import math

import _station_common as sc

NAME = "Kit_StationCounter_Run120"
LODS = (0.94, 0.90)   # hard surface: LODs drop sub-pixel parts only (no collapse: it folded boxes into triangles)
SMOOTH_ANGLE = 35.0

GREY, OAK, BEIGE_TOP, TOE = sc.LAMINATE_GREY, "Prop_WoodOak", "Prop_LaminateBeige", "Prop_Vinyl"
EDGE, STEEL = "Prop_PVCEdge", "Prop_SteelBlack"
LEDGE_TOP, LEDGE_T, LEDGE_Z0, LEDGE_Z1 = 1.07, 0.032, 0.00, 0.32
WORK_TOP, WORK_T, WORK_Z0, WORK_Z1 = 0.74, 0.030, -0.30, 0.27
FASCIA_Z0, FASCIA_Z1 = 0.27, 0.30
TOE_H, TOE_SET = 0.10, 0.05


def build_run(kit, width, extras):
    sc.register()
    hw = width / 2
    # Toe base (black rubber/vinyl, recessed).
    sc.box(kit, (width, TOE_H, 0.018), (0, TOE_H / 2, FASCIA_Z1 - TOE_SET - 0.009), TOE, bevel=0.002, segments=1, name="toe base")
    # Fascia panel: grey laminate on a core, two vertical reveals per 1.20 m.
    sc.box(kit, (width, LEDGE_TOP - LEDGE_T - TOE_H, FASCIA_Z1 - FASCIA_Z0), (0, (LEDGE_TOP - LEDGE_T + TOE_H) / 2, (FASCIA_Z0 + FASCIA_Z1) / 2),
           GREY, bevel=0.0015, segments=2, name="fascia")
    sc.box(kit, (width, 0.012, 0.018), (0, TOE_H + 0.006, FASCIA_Z1 - 0.012), GREY, bevel=0.001, segments=1, name="fascia kick rail")
    n = max(1, int(round(width / 0.60)))
    for k in range(1, n):
        x = -hw + k * width / n
        sc.box(kit, (0.004, LEDGE_TOP - LEDGE_T - TOE_H - 0.02, 0.002), (x, (LEDGE_TOP - LEDGE_T + TOE_H) / 2, FASCIA_Z1 + 0.0002), EDGE, bevel=0.0, name="fascia reveal")
    # Transaction ledge: oak, bullnosed visitor edge in PVC.
    prof = [(LEDGE_Z0, LEDGE_TOP - LEDGE_T), (LEDGE_Z1 - 0.012, LEDGE_TOP - LEDGE_T), (LEDGE_Z1 - 0.012, LEDGE_TOP), (LEDGE_Z0, LEDGE_TOP)]
    sc.extrude_zy(kit, prof, width, 0.0, OAK, bevel=0.0015, segments=2, name="ledge top")
    nose = [(LEDGE_Z1 - 0.012, LEDGE_TOP - LEDGE_T)]
    for k in range(9):
        a = math.radians(-90 + 180 * k / 8)
        nose.append((LEDGE_Z1 - 0.012 + 0.012 * math.cos(a), LEDGE_TOP - LEDGE_T / 2 + LEDGE_T / 2 * math.sin(a)))
    nose.append((LEDGE_Z1 - 0.012, LEDGE_TOP))
    sc.extrude_zy(kit, nose, width, 0.0, EDGE, bevel=0.0, segments=1, name="ledge nosing")
    # Steel L brackets under the ledge's staff edge.
    nb = 3 if width > 1.0 else 2
    for k in range(nb):
        x = -hw + 0.10 + k * (width - 0.20) / max(1, nb - 1)
        br = [(LEDGE_Z0 + 0.01, LEDGE_TOP - LEDGE_T), (FASCIA_Z0, LEDGE_TOP - LEDGE_T), (FASCIA_Z0, LEDGE_TOP - LEDGE_T - 0.20),
              (FASCIA_Z0 - 0.006, LEDGE_TOP - LEDGE_T - 0.20), (LEDGE_Z0 + 0.01, LEDGE_TOP - LEDGE_T - 0.006)]
        sc.extrude_zy(kit, br, 0.004, x, STEEL, bevel=0.0006, segments=1, name="ledge bracket")
    # Staff work surface: beige laminate with a PVC edge on the staff side, back apron below.
    sc.box(kit, (width, WORK_T, WORK_Z1 - WORK_Z0 - 0.004), (0, WORK_TOP - WORK_T / 2, (WORK_Z0 + 0.004 + WORK_Z1) / 2), BEIGE_TOP, bevel=0.0012, segments=1, name="work surface")
    sc.box(kit, (width, WORK_T, 0.004), (0, WORK_TOP - WORK_T / 2, WORK_Z0 + 0.002), EDGE, bevel=0.0008, segments=2, name="work edge")
    sc.box(kit, (width - 0.02, 0.06, 0.018), (0, WORK_TOP - WORK_T - 0.03, WORK_Z0 + 0.06), STEEL, bevel=0.002, segments=1, name="back apron")
    sc.box(kit, (width - 0.02, 0.05, 0.012), (0, WORK_TOP - WORK_T - 0.025, FASCIA_Z0 - 0.006), STEEL, bevel=0.0015, segments=1, name="front cleat")
    if extras:
        # Dead door-release: a black box under the ledge's staff edge with a black mushroom button (no red: 30L §2).
        bx, by = -0.30, LEDGE_TOP - LEDGE_T - 0.035
        sc.box(kit, (0.075, 0.05, 0.06), (bx, by, LEDGE_Z0 + 0.04), STEEL, bevel=0.004, segments=2, name="release box")
        sc.cyl(kit, 0.016, 0.006, (bx, by, LEDGE_Z0 + 0.007), "z", STEEL, verts=24, bevel=0.0015, segments=2, name="release collar")
        sc.lathe_y  # noqa
        btn = kit.lathe([(0.0055, 0.0), (0.0055, 0.006), (0.019, 0.010), (0.019, 0.016), (0.012, 0.019), (0.0, 0.020)], (0, 0, 0), "Prop_PlasticBlack",
                        verts=28, name="release mushroom", close_bottom=False)
        btn.rotation_mode = "XYZ"
        btn.rotation_euler = (math.radians(-90), 0, 0)
        btn.location = sc.B(bx, by, LEDGE_Z0 + 0.010)
        # Intercom master on the work surface, tucked under the ledge: putty case, grille slots, five station keys.
        ix, iz = 0.36, 0.12
        sc.box(kit, (0.20, 0.07, 0.13), (ix, WORK_TOP + 0.035, iz), "Prop_PlasticBeige", bevel=0.008, segments=3, name="intercom case")
        for k in range(9):
            sc.box(kit, (0.004, 0.0015, 0.05), (ix - 0.075 + k * 0.009, WORK_TOP + 0.0705, iz + 0.02), "Prop_PlasticBlack", bevel=0.0, name="grille slot")
        for k in range(5):
            sc.box(kit, (0.016, 0.010, 0.014), (ix + 0.015 + k * 0.019, WORK_TOP + 0.072, iz - 0.035), "Prop_PlasticBlack", bevel=0.0015, segments=1, name="station key")
        sc.box(kit, (0.03, 0.012, 0.016), (ix - 0.07, WORK_TOP + 0.074, iz - 0.035), "Prop_PlasticBlack", bevel=0.002, segments=1, name="talk bar")
    sc.auto_lod_drops(kit)
    kit.support("ledge", sc.B(0, LEDGE_TOP, (LEDGE_Z0 + LEDGE_Z1 - 0.012) / 2), (width - 0.02, LEDGE_Z1 - LEDGE_Z0 - 0.02))
    kit.support("work", sc.B(0, WORK_TOP, (WORK_Z0 + LEDGE_Z0) / 2), (width - 0.02, LEDGE_Z0 - WORK_Z0 - 0.01))
    kit.support("work_under_ledge", sc.B(0, WORK_TOP, (LEDGE_Z0 + WORK_Z1) / 2), (width - 0.02, WORK_Z1 - LEDGE_Z0 - 0.01))
    kit.collider(sc.B(0, LEDGE_TOP / 2, 0.0), sc.BS(width, LEDGE_TOP, 0.60))
    kit.tag("station", "counter")
    sc.lods(kit, 4.0, 10.0, 60.0, (3500, 1200, 150))
    sc.station_meta(kit, "floor, centred on the run", "front (+Z) = the visitor face")


def build(kit):
    build_run(kit, 1.20, True)
