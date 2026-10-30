"""Kit_StationCounter_End: a finished end panel of the security counter (30L S05 Kit_StationCounter_End; 20L G7: the
staff strip behind the counter stays open at both ends; plan 70 P11).

A 25 mm grey-laminate gable following the counter's section (to the ledge at the visitor side, to the work surface at
the staff side), PVC-edged, with an oak cap where the ledge ends and a black toe. ORIGIN = floor, centred on the panel
(it straddles the run's end by 12.5 mm). Front +Z = the visitor side. Budget 1,200 / 400 / 60. Box collider.
"""

import _station_common as sc
import station_counter as base

NAME = "Kit_StationCounter_End"
LODS = (0.94, 0.90)   # hard surface: LODs drop sub-pixel parts only (no collapse: it folded boxes into triangles)
SMOOTH_ANGLE = 35.0

T = 0.025


def build(kit):
    sc.register()
    # Two boxes (a concave extrude triangulated badly): the visitor half to the ledge, the staff half to the work surface.
    sc.box(kit, (T, base.LEDGE_TOP - base.LEDGE_T, 0.30), (0, (base.LEDGE_TOP - base.LEDGE_T) / 2, 0.15), base.GREY, bevel=0.0015, segments=2, name="end gable front")
    sc.box(kit, (T, base.WORK_TOP, 0.30), (0, base.WORK_TOP / 2, -0.15), base.GREY, bevel=0.0015, segments=2, name="end gable back")
    # PVC edge band on the staff edges.
    sc.box(kit, (T + 0.001, 0.003, 0.30), (0, base.WORK_TOP + 0.0015, -0.15), base.EDGE, bevel=0.0008, segments=1, name="work edge band")
    sc.box(kit, (T + 0.001, base.LEDGE_TOP - base.LEDGE_T - base.WORK_TOP, 0.003), (0, (base.LEDGE_TOP - base.LEDGE_T + base.WORK_TOP) / 2, 0.0015), base.EDGE,
           bevel=0.0008, segments=1, name="riser edge band")
    sc.box(kit, (T + 0.001, base.WORK_TOP - 0.10, 0.003), (0, (base.WORK_TOP + 0.10) / 2, -0.3015), base.EDGE, bevel=0.0008, segments=1, name="back edge band")
    # Oak end cap of the ledge.
    cap = [(0.0, base.LEDGE_TOP - base.LEDGE_T), (0.32, base.LEDGE_TOP - base.LEDGE_T), (0.32, base.LEDGE_TOP), (0.0, base.LEDGE_TOP)]
    sc.extrude_zy(kit, cap, T + 0.002, 0.0, "Prop_WoodOak", bevel=0.0015, segments=2, name="ledge end cap")
    sc.box(kit, (T + 0.002, base.TOE_H, 0.006), (0, base.TOE_H / 2, 0.303), "Prop_Vinyl", bevel=0.001, segments=1, name="toe end")
    kit.collider(sc.B(0, base.LEDGE_TOP / 2, 0.0), sc.BS(T, base.LEDGE_TOP, 0.60))
    kit.tag("station", "counter")
    sc.auto_lod_drops(kit)
    sc.lods(kit, 4.0, 10.0, 60.0, (1200, 400, 60))
    sc.station_meta(kit, "floor, centred on the 25 mm panel", "")
