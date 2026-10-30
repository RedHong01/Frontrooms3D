"""Kit_TaskChair_Charcoal: the guard's chair (61 §4.4 chair row "他看的是电视，不是门"; 03_palette.md task chair ADJUST:
grey fabric on a black base; DEC 16:0x; plan 70 P13).

task_chair.py's Kit_TaskChair with Prop_FabricChair -> Prop_FabricCharcoal, and NO pile block / pile tags (plan check
C2-4: FrontRoomsFurniturePile.Library takes every IsPilePiece kit, so a variant with a pile block would enter every
seeded Level 0 pile). Same mesh, bounds, anchors and collider. ORIGIN = the floor under the chair.
"""

import _station_common as sc
import task_chair as base

NAME = "Kit_TaskChair_Charcoal"
LOD1 = base.LOD1
SMOOTH_ANGLE = getattr(base, "SMOOTH_ANGLE", 35.0)


def build(kit):
    base.build(kit)
    mat = kit._material("Prop_FabricCharcoal")
    for obj in kit.parts:
        mats = obj.data.materials
        for i, m in enumerate(mats):
            if m is not None and m.name == "Prop_FabricChair":
                mats[i] = mat
    sc.no_pile(kit)
    kit.meta["tags"] = [t for t in kit.meta["tags"] if t != "office"] + ["station"]
    sc.station_meta(kit, "floor under the chair", "Kit_TaskChair geometry, charcoal fabric; no pile block")
