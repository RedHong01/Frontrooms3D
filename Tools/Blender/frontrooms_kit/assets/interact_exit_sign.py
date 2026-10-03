"""Wall exit sign (P2): a 1990 stamped-steel single-face EXIT sign over the
Exit doors (spec 10 §2.4, §2.1 EX-F / EX-K).

Real-world reference: the US commercial wall-mount exit sign of the period:
a black painted stamped-steel housing (0.330 x 0.200 x 0.050) with rounded
drawn corners, a stamped face frame with a raised bead and an inner lip that
holds the face, four slotted face screws on the long sides, and a conduit
knockout plug on top. No pilot light, no test button, no brand (era note R10:
no LED indicators). The face is the project's existing Run_ExitSign surface
(red EXIT, emissive in Unity, as the Run level's hanging signs; Stream:1470-
1479), shown through that material's own UV transform (_TileSize 0.36 x 0.18,
_BaseMap_ST (-1, 1, 1, 0)): the face quad's UVs are authored in its metres so
the texture region U 0.165-0.845, V 0.145-0.907 (the word without the Run
chevrons, which point along a corridor) fills the 0.296 x 0.166 opening.
Deviation: with that texture the letters are 0.10 m tall, not 6"; a 6"
legend needs its own face texture (reported).

Origin = THE CENTRE OF THE BACK, on the wall face (the door frame's
exit_sign_p anchor puts it at Y 2.280, so its top is at 2.380); front +Z =
the room. 0.330 x 0.200 x 0.060. No Light component (00): the sign glows by
its emissive face only.

Budget §9.3: 2,000 / 700 / 100 tris, LOD 3 / 10 / - m, no LOD1. Slots:
Prop_SteelBlack (housing, frame, screws), Run_ExitSign (face; existing
surface via register_slot). VARIANT Kit_ExitSign_Dead: Run_ExitSign ->
Run_ExitSign_Dead (NEW non-emissive copy, P-4).
"""

import math

import interact_key_common as kc
from interact_key_common import U

NAME = "Kit_ExitSign"
VARIANTS = {"Kit_ExitSign_Dead": {kc.EXIT_FACE: kc.EXIT_DEAD}}
LOD1_RATIO, LOD2_RATIO = 700 / 2000, 100 / 2000
LOD_DISTANCES = (3.0, 10.0, None)
BUDGET = (2000, 700, 100)

STEEL = "Prop_SteelBlack"
W, H = 0.330, 0.200
BODY_D = 0.050
DEPTH = 0.060           # to the screw heads
FRONT = DEPTH - 0.0015  # the frame's flat
OPEN = (0.296, 0.166, 0.006)
TEX_U = (0.165, 0.845)


def build(kit):
    kc.register_slots()
    to3 = lambda u, v, w: U(u, v, w)
    hpath = kc.rounded_rect(W, H, 0.010, 8)
    r = 0.004
    prof = [(0.0, 0.0), (0.0, BODY_D - r)]
    for i in range(1, 5):
        a = math.radians(90 * i / 4)
        prof.append((r * (1 - math.cos(a)), BODY_D - r + r * math.sin(a)))
    prof.append((r + 0.0004, BODY_D))
    housing = kc.profile_sweep(kit, hpath, prof, STEEL, to3, name="housing", cap_last=True)
    # Stamped face frame round the opening: outward offsets of the opening path.
    ow, oh, orad = OPEN
    opath = kc.rounded_rect(ow, oh, orad, 8)
    border = (W - ow) / 2 - 0.0005
    fprof = [(-border, BODY_D), (-border + 0.0006, BODY_D + 0.0032), (-border + 0.0030, BODY_D + 0.0075),
             (-border + 0.0062, FRONT - 0.0002), (-0.0045, FRONT), (-0.0020, FRONT - 0.0004), (-0.0004, FRONT - 0.0022),
             (0.0, FRONT - 0.0045), (0.0020, FRONT - 0.0050), (0.0020, FRONT - 0.0062)]
    frame = kc.profile_sweep(kit, opath, fprof, STEEL, to3, name="face frame")
    # Conduit knockout plug on the top.
    ko = kit.cylinder(0.011, 0.0016, (0, 0, 0), STEEL, verts=20, bevel=0.0004, segments=1, name="knockout")
    kc.orient(ko, (0.0, H / 2 + 0.0008, 0.024), (0, 1, 0), (0, 0, 1))
    kc.drop2(ko)
    for sx, sy, deg in ((0.10, 1, 20), (-0.10, 1, -35), (-0.10, -1, 75), (0.10, -1, 5)):
        s = kc.oval_screw(kit, (sx, sy * (oh / 2 + 0.0075), FRONT - 0.0001), STEEL, head_d=0.0062, dome_h=0.0012,
                          slot_w=0.0007, slot_d=0.0008, slot_deg=deg, segs=12, name="face screw")
        kc.drop2(s)
    # Face: the existing Run_ExitSign surface, UVs in that material's metres.
    zf = FRONT - 0.0064
    aspect = ow / oh
    u_span = TEX_U[1] - TEX_U[0]
    v_span = u_span * (1024 / 512) / aspect
    vc = 0.5255
    V0, V1 = vc - v_span / 2, vc + v_span / 2
    u0, v0, u1, v1 = kc.exit_face_rect(TEX_U[0], V0, TEX_U[1], V1)
    hw, hh = ow / 2 + 0.002, oh / 2 + 0.002
    gu = (u1 - u0) * 0.002 / ow
    gv = (v1 - v0) * 0.002 / oh
    face = kc.quad_uv(kit, [(hw, -hh, zf), (-hw, -hh, zf), (-hw, hh, zf), (hw, hh, zf)],
                      [(u0 - gu, v0 - gv), (u1 + gu, v0 - gv), (u1 + gu, v1 + gv), (u0 - gu, v1 + gv)],
                      kc.EXIT_FACE, name="exit face")
    kc.wear_all(kit)
    kit.anchor("back", U(0, 0, 0))
    kit.anchor("face", U(0, 0, zf))
    kit.anchor("face_dir", U(0, 0, zf + 0.10))
    kit.no_collider()
    kit.tag("interactable", "exit_sign")
    kc.lod_meta(kit, LOD_DISTANCES, BUDGET)
    kit.meta["face"] = {"slot": kc.EXIT_FACE, "deadSlot": kc.EXIT_DEAD, "textureRegion": [TEX_U[0], round(V0, 4), TEX_U[1], round(V1, 4)],
                        "materialTile": [0.36, 0.18], "materialST": [-1, 1, 1, 0], "letterHeightM": round(0.441 / v_span * oh, 3)}
    lo, hi = kc.eval_bounds_unity(kit)
    assert abs((hi[0] - lo[0]) - W) < 1e-4, ("width", lo, hi)
    assert abs(lo[1] + H / 2) < 1e-4 and abs(hi[1] - (H / 2 + 0.0016)) < 2e-4, ("height", lo, hi)
    assert abs(lo[2]) < 1e-6 and hi[2] <= DEPTH + 1e-4, ("depth", lo, hi)
    kc.check_budget(kit, BUDGET[0])
