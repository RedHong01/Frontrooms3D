"""Engraved / screen-printed safety sign plate, 10 x 7 in (254 x 178 mm), 1.6 mm sheet,
6 mm corner radius, four stainless pan-head screws. PRODUCTION kit for Run! B
(Kit_SafetyPlate_10x7) with face variants, hand-built LOD0/1/2.

Faces (Run_Plate*_A, 1024 x 512 = 4.0 x 2.9 px/mm; TeX Gyre Heros Bold, through
平面视觉 before shipping): NOTICE (this asset), ELECTRICAL ROOM (Kit_SafetyPlate_Electrical),
STAFF ONLY (Kit_SafetyPlate_Staff). The plate's own decal UVs carry the face; its 1.6 mm
edge takes the face's border colour. The NOT AN EXIT door stencil is its own asset
(run_stencil_not_exit.py). ORIGIN = the wall point at the plate centre. FRONT = kit -Y.
Budget 300 / 100 / 12; lodDistances 2 / 6 / 25. The plate edge is not bevelled (1.6 mm sheet).
Slots: Run_PlateNotice (variants swap it), Prop_Aluminium (screws).
"""

import _run_b_common as B

NAME = "Kit_SafetyPlate_10x7"
SMOOTH_ANGLE = 30.0
BUDGET = (300, 100, 12)
VARIANTS = {
    "Kit_SafetyPlate_Electrical": {"Run_PlateNotice": "Run_PlateElectrical"},
    "Kit_SafetyPlate_Staff": {"Run_PlateNotice": "Run_PlateStaff"},
}

FACE = "Run_PlateNotice"
SCREW = "Prop_Aluminium"
W, H, T, R = 0.254, 0.178, 0.0016, 0.006


def _level(kit, lv):
    per = {0: 4, 1: 1, 2: 0}[lv]
    outline = B.rrect(W, H, R if per else 0.0, per)
    p = kit.extrude(outline, T, (0, -T / 2 - 0.0002, 0), FACE, plane="xz", bevel=0.0, segments=0, name="plate", uv="decal")
    p["fr_decal_axes"] = "xz"
    if lv == 0:
        for sx in (-1, 1):
            for sz in (-1, 1):
                B.lathe(kit, [(0.0036, 0.0), (0.0035, 0.0007), (0.0025, 0.0014), (0.0, 0.0016)],
                        (sx * (W / 2 - 0.014), -T - 0.0002, sz * (H / 2 - 0.014)), SCREW, lv=1, rot=(90, 0, 0), name="screw", lo=8)


def build(kit):
    B.levels(kit, _level)
    kit.no_collider()
    kit.tag("wall_decor", "run", "run_b", "sign")
    B.lod_meta(kit, (2.0, 6.0, 25.0), BUDGET)
