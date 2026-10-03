"""Round plastic key tag with a paper number insert (spec 10 §4.1; era note
R10). The round sibling of Kit_KeyTag_Rect: a Ø 0.038 x 0.0042 coloured
body with a ring tab (R 0.0055 round the Ø 0.005 hole, 1.5 mm fillets), a
Ø 0.026 window 0.8 mm deep with a 0.3 mm lip over a 0.6 mm undercut, and a
round paper insert under it. Same lineage as the metal-rim paper tag (02
§8.2) but in moulded plastic, so it shares the kit's colour identities.

Origin = THE RING HOLE, hangs along -Y, front +Z, mid-plane Z = 0. Anchors:
hole (+ hole_dir), face = number (+ face_dir).

Budget §9.3: 1,200 / 400 / 60 tris, LOD 1.0 / 3 / 15 m, no LOD1. Slots
Prop_PlasticRed (VARIANTS _Blue, _White) and Prop_KeyTagNo (NEW; fallback
Prop_Paper). The insert shows atlas cell "00"; the facade offsets it.
"""

import interact_key_common as kc

NAME = "Kit_KeyTag_Round"
VARIANTS = {"Kit_KeyTag_Round_Blue": {"Prop_PlasticRed": "Prop_PlasticBlue"},
            "Kit_KeyTag_Round_White": {"Prop_PlasticRed": "Prop_PlasticWhite"}}
LOD1_RATIO, LOD2_RATIO = 400 / 1200, 60 / 1200
LOD_DISTANCES = (1.0, 3.0, 15.0)
BUDGET = (1200, 400, 60)
SMOOTH_ANGLE = 40.0

D = 0.038
Y_TOP = -0.0045
WIN_D = 0.026
CROP = (0.56, 0.56)         # 11 mm digits inside the Ø 26 mm window


def build(kit):
    wy = Y_TOP - D / 2
    kc.build_tag(kit, ("round", D, Y_TOP), ("round", WIN_D, wy), CROP)
    kc.lod_meta(kit, LOD_DISTANCES, BUDGET)
    lo, hi = kc.eval_bounds_unity(kit)
    assert abs((hi[0] - lo[0]) - D) < 2e-4, ("diameter", lo, hi)
    assert abs(lo[1] - (Y_TOP - D)) < 2e-4 and abs(hi[1] - kc.TAB_R) < 1e-4, ("height", lo, hi)
    assert abs((hi[2] - lo[2]) - kc.TAG_T) < 1e-5, ("thickness", lo, hi)
    kc.check_budget(kit, BUDGET[0])
