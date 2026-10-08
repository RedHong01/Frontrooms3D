"""Rectangular plastic key tag with a paper number insert (spec 10 §4.1;
era note R10: "plastic ring key tags with a paper insert").

Real-world reference: the moulded plastic key identification tag sold in
hardware stores since the 1960s (02 §8.2): a coloured body with a ring tab,
a recessed label window and a typed paper insert held under a retaining lip.
Body 0.057 x 0.029 x 0.0042 (Unity X x Y x Z, the hung orientation of §9.3),
R 0.005 corners, 0.6 mm rounded edges; a moulded ring tab, 2.4 mm thick (R
0.0055 round the Ø 0.005 hole), so the tag can turn ~30 deg on the ring; window 0.040 x 0.019 (R 1.5 mm corners), 0.8 mm
deep, 0.3 mm lip with a 0.6 mm undercut; the insert is one quad under it.

Origin = THE RING HOLE (its swing pivot), hangs along -Y, front +Z, mid-plane
Z = 0. The body hangs from a top-centre tab, so the typed number reads
upright when the tag hangs (the §9.3 bounds are landscape: X 0.057 > Y).
Anchors: hole (0, 0, 0) (+ hole_dir), face = number = insert centre (+ face_dir).

Budget §9.3: 1,200 / 400 / 60 tris, LOD distances 1.0 / 3 / 15 m; LOD1 exported since the 2026-10-08 fix pass (critic H4).
Slots: Prop_PlasticRed (body; VARIANTS _Blue = Prop_PlasticBlue, _White =
Prop_PlasticWhite) and Prop_KeyTagNo (insert, NEW slot for P-4; fallback
Prop_Paper, blank). The insert UVs point at atlas cell "00", cropped to the
window; the facade shows number n with _BaseMap_ST = (1, 1, (n%10)/10,
-(n//10)/10) on a MaterialPropertyBlock.
"""

import interact_key_common as kc

NAME = "Kit_KeyTag_Rect"
VARIANTS = {"Kit_KeyTag_Rect_Blue": {"Prop_PlasticRed": "Prop_PlasticBlue"},
            "Kit_KeyTag_Rect_White": {"Prop_PlasticRed": "Prop_PlasticWhite"}}
LOD1_RATIO, LOD2_RATIO = 400 / 1200, 60 / 1200
LOD1 = LOD1_RATIO  # fix pass 2026-10-08 (critic H4): FrontRoomsKitImporter honours the sidecar distances since 663e858, so LOD1 = LOD0->LOD1 at d01 and a cull at dcull
LOD_DISTANCES = (1.0, 3.0, 15.0)
BUDGET = (1200, 400, 60)
SMOOTH_ANGLE = 40.0

W, H, R = 0.057, 0.029, 0.005
Y_TOP = -0.0045
WIN = (0.040, 0.019, 0.0015)
CROP = (0.648, 0.308)       # 15 mm digits in the 19 mm window


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
