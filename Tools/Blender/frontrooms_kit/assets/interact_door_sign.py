"""Door sign: a two-ply engraved plastic plate on the locked doors (spec 10
§2.4; 05 §6.2): "EMPLOYEES ONLY" by default, the same plate on both faces.

Real-world reference: the 10" x 3" engraved two-ply sign of a 1990 back-room
door: a dark-brown cap ply over an ivory core, letters routed through the
cap so the core shows, the face edges bevelled 1 mm (the bevel cuts into the
core, so the plate is outlined in a fine core-colour line), bonded (fix 1).
No Braille (pre-ADAAG), no date, no logo.

Plate 0.254 x 0.076 x 0.003, square corners, 1 mm x 45 deg face bevel; four
#4 slotted oval-head screws 10 mm in from each corner. The text is a decal
quad on Prop_SignEngraved (NEW slot, P-4): a 2048 x 1024 atlas of 2 x 2
cells (1024 x 512 px = 0.254 x 0.127 m, 4031 px/m): 0 EMPLOYEES ONLY, 1
STAFF ONLY, 2 STORAGE, 3 spare. The quad ships on cell 0; the facade picks
cell n with _BaseMap_ST = (1, 1, (n % 2) / 2, -(n // 2) / 2). Fallback: no
text, the cap ply reads as a dark plate (Prop_PlasticBlack).

Origin = THE CENTRE OF THE BACK FACE, on the leaf face (part frame §1.2);
front +Z = outward. The facade mounts it at the leaf's sign_s / sign_p
anchors (Y 1.524, Z 0.500) with localScale (S_sign, 1, 1) so the text is
never mirrored.

Budget §9.3: 600 / 200 / 12 tris, LOD 2 / 6 / 25 m; LOD1 exported since the 2026-10-08 fix pass (critic H4). Slots (dominant
first): Prop_PlasticBlack (cap ply sides and back), Prop_SignEngraved (face),
Prop_PlasticWhite (the bevel's exposed ivory core), Prop_Chrome (screws).
"""

import interact_key_common as kc
from interact_key_common import U

NAME = "Kit_DoorSign"
LOD1_RATIO, LOD2_RATIO = 200 / 600, 12 / 600
LOD1 = LOD1_RATIO  # fix pass 2026-10-08 (critic H4): FrontRoomsKitImporter honours the sidecar distances since 663e858, so LOD1 = LOD0->LOD1 at d01 and a cull at dcull
LOD_DISTANCES = (2.0, 6.0, 25.0)
BUDGET = (20, 20, 12)   # D1.6 fix 1: bonded (no screws, SPEC v1.3 §4): the plate is 20 tris; the screws were 644 of the 664

CAP, CORE, CHROME = "Prop_PlasticBlack", "Prop_PlasticWhite", "Prop_Chrome"
W, H, T = 0.254, 0.076, 0.003
BEV = 0.001
SCREW_IN = 0.010
CELL_W_M, CELL_H_M = 0.254, 0.127      # one atlas cell at 1:1


def build(kit):
    kc.register_slots()
    hw, hh = W / 2, H / 2
    path = [(-hw, -hh), (hw, -hh), (hw, hh), (-hw, hh)]
    to3 = lambda u, v, w: U(u, v, w)
    body = kc.profile_sweep(kit, path, [(0.0, 0.0), (0.0, T - BEV)], CAP, to3, name="cap ply", cap_first=True)
    core = kc.profile_sweep(kit, path, [(0.0, T - BEV), (BEV, T)], CORE, to3, name="bevel core")
    # Face quad = the plate face inside the bevel; UVs on atlas cell 0.
    fw, fh = W - 2 * BEV, H - 2 * BEV
    du = fw / CELL_W_M / 2
    dv = fh / CELL_H_M / 2
    cu, cv = 0.25, 0.75                     # centre of cell 0 (top-left) in a 2 x 2 atlas
    u0, u1 = cu - du / 2 * 1.0, cu + du / 2 * 1.0
    v0, v1 = cv - dv / 2, cv + dv / 2
    face = kc.quad_uv(kit, [(fw / 2, -fh / 2, T), (-fw / 2, -fh / 2, T), (-fw / 2, fh / 2, T), (fw / 2, fh / 2, T)],
                      [(u0, v0), (u1, v0), (u1, v1), (u0, v1)], kc.SIGN_SLOT, name="engraved face")
    # D1.6 fix 1: bonded, no screws: the info line's two-ply plate rules (W/info_type/stair_signs/SPEC.md v1.3 §4:
    # "door plates are bonded (no screws through a labelled fire door)"); the atlas follows its colourways (pale plates
    # say no: EMPLOYEES ONLY / STAFF ONLY black on white; STORAGE white on black), see the facade's sign colourway swap.
    kc.wear_all(kit)
    kit.anchor("back", U(0, 0, 0))
    kit.anchor("face", U(0, 0, T))
    kit.anchor("face_dir", U(0, 0, T + 0.10))
    kit.no_collider()
    kit.tag("interactable", "door_sign")
    # EXAMINE: v1 reads the sign in place (research/inspect/10_design.md §1.3, R5). This is R5's alternative B
    # (C22: sign only, reach 1.2 m, no zoom: its 17 mm cap already reads), kept as a test-bed block (ship=False).
    kit.inspect("flat", verb="read", face="face", faceDir="face_dir", size=(W, H), margin=0.08, dist=(0.30, 0),
                minDist=0.18, maxDist=1.0, reach=1.2, approachAngle=60, box=(U(0, 0, T / 2), kc.U_size(W, H, T)),
                aimPad=0.02, regions=[{"id": "sign", "rect": [0, 0, 0, 0], "capMm": 17}], sound="plastic", ship=False)
    kc.lod_meta(kit, LOD_DISTANCES, BUDGET)
    kit.meta["signAtlas"] = {"slot": kc.SIGN_SLOT, "cols": 2, "rows": 2, "cells": ["EMPLOYEES ONLY", "STAFF ONLY", "STORAGE", "spare"],
                             "defaultCell": 0, "setCell": "_BaseMap_ST = (1, 1, (n % 2) / 2, -(n // 2) / 2)",
                             "capHeightMm": 17, "font": "TeX Gyre Heros Bold"}
    lo, hi = kc.eval_bounds_unity(kit)
    assert abs((hi[0] - lo[0]) - W) < 1e-5 and abs((hi[1] - lo[1]) - H) < 1e-5, ("plate", lo, hi)
    assert abs(lo[2]) < 1e-6 and hi[2] <= 0.0045, ("thickness", lo, hi)
    kc.check_budget(kit, BUDGET[0])


# D1.6 fix 1 (2026-10-22): LOD0/1/2 by limited dissolve (interact_lod_dissolve.py; Red's call 8). The sidecar's
# lodRatios are the targets; kitlib.py and build_asset.py are unchanged.
import interact_lod_dissolve as _ld  # noqa: E402

_build_lod0 = build


def build(kit):  # noqa: F811
    _build_lod0(kit)
    _ld.use(kit, LOD1_RATIO, LOD2_RATIO if "LOD2_RATIO" in globals() and LOD2_RATIO else 0.05)
