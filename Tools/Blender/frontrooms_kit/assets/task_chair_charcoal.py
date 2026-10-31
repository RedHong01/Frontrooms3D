"""Kit_TaskChair_Charcoal: the guard's chair (61 §4.4 chair row "他看的是电视，不是门"; 03_palette.md task chair ADJUST:
grey fabric on a black base; DEC 16:0x; plan 70 P13).

task_chair_armed.py's chair with Prop_FabricChair -> Prop_FabricCharcoal, and NO pile block / pile tags (plan check
C2-4: FrontRoomsFurniturePile.Library takes every IsPilePiece kit, so a variant with a pile block would enter every
seeded Level 0 pile). Same mesh, bounds, anchors and collider. ORIGIN = the floor under the chair.

W2 (2026-10-30): the chair this kit was cut from moved to task_chair_armed.py when Kit_TaskChair became the armless
office chair (task_chair.py). The guard keeps it: Kit_JacketDraped is cloth-baked over this shape.
"""

import _station_common as sc
import task_chair_armed as base

NAME = "Kit_TaskChair_Charcoal"
LODS = base.LODS
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
