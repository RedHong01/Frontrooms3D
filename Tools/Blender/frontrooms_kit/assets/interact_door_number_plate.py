"""Door number plate: the zone's key number on the locked door, in the key
tag's colour (spec 10 §2.4, §4.1; 03 §2.5; 05 §6.2).

Real-world reference: a small coloured room/number plate screwed to a
storeroom door beside the lock. Here it repeats the key tag's construction
on purpose - a coloured plastic plate with a recessed window holding a typed
paper insert - so the player matches the door to the tag by colour (2-5 m)
and by the typed number (<= 2.7 m). Plate 0.100 x 0.050 x 0.003, R 0.004
corners, 0.5 mm edge chamfer; window 0.070 x 0.034 (R 2 mm), 0.6 mm deep;
two #4 slotted oval-head screws. Digits 27 mm tall (Courier Prime).
Deviation: §2.4 calls it "two-ply engraved"; engraved digits would need a
second number atlas, so the plate carries the same Prop_KeyTagNo insert as
the tags (reported).

Origin = THE CENTRE OF THE BACK FACE, on the leaf face (part frame §1.2);
front +Z = outward. Mounted at the leaf's tagplate_s / tagplate_p anchors
(Y 1.000, Z 0.790) with localScale (S_sign, 1, 1). The insert UVs point at
atlas cell "00"; the facade shows number n with _BaseMap_ST = (1, 1,
(n%10)/10, -(n//10)/10).

Budget §9.3: 500 / 180 / 12 tris, LOD 1.5 / 5 / 15 m; LOD1 exported since the 2026-10-08 fix pass (critic H4). Slots:
Prop_PlasticRed (VARIANTS _Blue, _White), Prop_KeyTagNo (NEW; fallback
Prop_Paper), Prop_Chrome (screws).
"""

import interact_key_common as kc
from interact_key_common import U

NAME = "Kit_DoorNumberPlate"
VARIANTS = {"Kit_DoorNumberPlate_Blue": {"Prop_PlasticRed": "Prop_PlasticBlue"},
            "Kit_DoorNumberPlate_White": {"Prop_PlasticRed": "Prop_PlasticWhite"}}
LOD1_RATIO, LOD2_RATIO = 180 / 500, 12 / 500
LOD1 = LOD1_RATIO  # fix pass 2026-10-08 (critic H4): FrontRoomsKitImporter honours the sidecar distances since 663e858, so LOD1 = LOD0->LOD1 at d01 and a cull at dcull
LOD_DISTANCES = (1.5, 5.0, 15.0)
BUDGET = (500, 180, 12)

RED, CHROME = "Prop_PlasticRed", "Prop_Chrome"
W, H, T, R = 0.100, 0.050, 0.003, 0.004
WIN = (0.070, 0.034, 0.002)
DEPTH = 0.0006
CROP = (0.618, 0.30)


def build(kit):
    kc.register_slots()
    outline = kc.rounded_rect(W, H, R, 3)
    to3 = lambda u, v, w: U(u, v, w + T / 2)
    wpoly = kc.rounded_rect(WIN[0], WIN[1], WIN[2], 2)
    plate = kc.slab(kit, outline, T, RED, to3, round_r=0.0005, round_segs=1, back=False, back_round=False,
                    pockets=[{"poly": wpoly, "depth": DEPTH, "chamfer": 0.0001}], name="plate")
    zi = T - DEPTH + 0.00005
    ww, wh = WIN[0], WIN[1]
    u0, v0, u1, v1 = kc.number_cell_rect(kc.NUMBER_DEFAULT_CELL, CROP[0], CROP[1])
    kc.quad_uv(kit, [(ww / 2, -wh / 2, zi), (-ww / 2, -wh / 2, zi), (-ww / 2, wh / 2, zi), (ww / 2, wh / 2, zi)],
               [(u0, v0), (u1, v0), (u1, v1), (u0, v1)], kc.KEYTAGNO, name="number insert")
    for sx, deg in ((1, 30), (-1, -40)):
        s = kc.oval_screw(kit, (sx * 0.0425, 0.0, T), CHROME, head_d=0.005, dome_h=0.001, slot_w=0.0006, slot_d=0.0006,
                          slot_deg=deg, segs=10, name="plate screw")
        kc.drop2(s)
    kc.wear_all(kit)
    kit.anchor("back", U(0, 0, 0))
    kit.anchor("face", U(0, 0, zi))
    kit.anchor("number", U(0, 0, zi))
    kit.anchor("face_dir", U(0, 0, zi + 0.10))
    kit.no_collider()
    kit.tag("interactable", "door_sign", "number_plate")
    kc.lod_meta(kit, LOD_DISTANCES, BUDGET)
    kit.meta["numberAtlas"] = {"slot": kc.KEYTAGNO, "fallback": kc.KEYTAGNO_FALLBACK, "cols": 10, "rows": 10,
                               "defaultCell": 0, "crop": list(CROP),
                               "setCell": "_BaseMap_ST = (1, 1, (n % 10) / 10, -(n // 10) / 10)"}
    lo, hi = kc.eval_bounds_unity(kit)
    assert abs((hi[0] - lo[0]) - W) < 1e-5 and abs((hi[1] - lo[1]) - H) < 1e-5, ("plate", lo, hi)
    assert abs(lo[2]) < 1e-6 and hi[2] <= 0.0045, ("thickness", lo, hi)
    kc.check_budget(kit, BUDGET[0])
