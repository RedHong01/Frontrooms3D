"""Long ("valet") plastic key tag with a paper number insert (spec 10 §4.1;
era note R10). Body 0.076 x 0.022 x 0.0042 (Unity X x Y x Z as §9.3), R
0.005 corners, 0.6 mm rounded edges, a 2.4 mm moulded ring tab (R
0.0055 round the Ø 0.005 hole) at the top centre; window 0.050 x 0.014 (R 1.5 mm),
0.8 mm deep under a 0.3 mm lip with a 0.6 mm undercut; one insert quad.

Origin = THE RING HOLE, hangs along -Y, front +Z, mid-plane Z = 0. Anchors:
hole (+ hole_dir), face = number (+ face_dir).

Budget §9.3: 1,200 / 400 / 60 tris, LOD 1.0 / 3 / 15 m; LOD1 exported since the 2026-10-08 fix pass (critic H4). Slots
Prop_PlasticRed (VARIANTS _Blue, _White) and Prop_KeyTagNo (NEW; fallback
Prop_Paper). The 3.57:1 window is wider than a square atlas cell allows at
a useful digit size: the crop is the full cell width x 0.29 (3.45:1), so
the typed digits are stretched 3.5 % horizontally (12 mm digits).
"""

import interact_key_common as kc

NAME = "Kit_KeyTag_Long"
VARIANTS = {"Kit_KeyTag_Long_Blue": {"Prop_PlasticRed": "Prop_PlasticBlue"},
            "Kit_KeyTag_Long_White": {"Prop_PlasticRed": "Prop_PlasticWhite"}}
LOD1_RATIO, LOD2_RATIO = 400 / 1200, 60 / 1200
LOD1 = LOD1_RATIO  # fix pass 2026-10-08 (critic H4): FrontRoomsKitImporter honours the sidecar distances since 663e858, so LOD1 = LOD0->LOD1 at d01 and a cull at dcull
LOD_DISTANCES = (1.0, 3.0, 15.0)
BUDGET = (1200, 400, 60)
SMOOTH_ANGLE = 40.0

W, H, R = 0.076, 0.022, 0.005
Y_TOP = -0.0045
WIN = (0.050, 0.014, 0.0015)
CROP = (1.0, 0.29)


def build(kit):
    wy = Y_TOP - H / 2
    kc.build_tag(kit, ("rect", W, H, R, Y_TOP), ("rect", WIN[0], WIN[1], WIN[2], wy), CROP)
    kc.lod_meta(kit, LOD_DISTANCES, BUDGET)
    lo, hi = kc.eval_bounds_unity(kit)
    assert abs((hi[0] - lo[0]) - W) < 1e-4, ("width", lo, hi)
    assert abs(lo[1] - (Y_TOP - H)) < 1e-4 and abs(hi[1] - kc.TAB_R) < 1e-4, ("height", lo, hi)
    assert abs((hi[2] - lo[2]) - kc.TAG_T) < 1e-5, ("thickness", lo, hi)
    assert abs(hi[1] - kc.TAB_R) < 1e-4, ("tab top", lo, hi)
    kc.check_budget(kit, BUDGET[0])
