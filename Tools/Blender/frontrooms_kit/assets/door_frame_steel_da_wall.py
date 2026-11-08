"""Kit_DoorFrame_Steel_DA_Wall: the Run! B side doors' frame (ELECTRICAL ROOM / STAFF ONLY / NOT AN EXIT, the RN-K
props of FrontRoomsRunDress.SideDoors), built for the prop on a 0.16 m block wall (zone doors run, Run! polish, 2026-11-06;
Documentation/research/double_doors/30_build.md Part R; research 10_research.md R.4; inventory row C2).

Today the props wear Kit_DoorFrame_Steel_DA (a sleeve for both faces of a 0.16 m wall) squeezed x0.92 along the wall normal
so its far casing hides in the wall: the near casing stands 10.2 mm proud instead of 11.1, the section is 8 % thin, and the
saddle, the pivot caps and the brush seals sit buried in the wall. This kit is the same F4 pressed-steel sleeve cut to the
corridor face only, at the true section: the 50.8 mm face 11.1 mm proud of the wall (SDI cased-open, door_frame 10 F4), its
7/16 in return into the block, the soffit returning from the casing to the wall face, mitred round the opening, welded and
ground (one sweep, no joint line). No far casing, nothing in the wall but the soffit's last 4 mm.

What the prop's gaps show (they were the bare wall 2 mm behind the leaf): black brush strips on the wall face behind the hinge,
latch and head gaps (the DA pair's "black brush strips close the gaps"; the DA leaf's radiused hinge stile turns 12 mm from
the soffit at the wall face), and the undercut's black backer: no saddle (a 1990 service room door over VCT is undercut;
the leaf stands 15 mm over the floor, as the landed leaf kit is built for a 12 mm saddle). The strike is the landed
Kit_Lock_StrikeMortise_DA, spawned by the dress on the latch soffit (its corridor-side lip shows in the 9 mm return).

DOOR ROOT (Unity metres): the prop leaf's own root (Kit_DoorLeaf_SteelRun_DA's origin), so FrontRoomsRunDress spawns frame
and leaf at one point and rotation: origin the opening's hinge-jamb edge on the LEAF's centre plane at the floor; +Z along
the opening (hinge 0, latch 1.0); +Y up; +X the corridor face S. The wall face is at X = +0.020 (the dress hangs the leaf's
S face 2 mm proud of the wall: escutcheon_s 0.022 - 0.002), so the leaf's back half lies in the wall: a prop has no opening.
The return depth from the casing face to the leaf face is therefore 9.1 mm where a real 1-3/4 in DA leaf centred in a
0.16 m wall sits 69 mm back: the honest flaw (30_build.md Part R).

LODs hand-built (fr_lods): LOD0 3-step bends (3 mm convex, 16 ga), LOD1 1-step, LOD2 sharp; the brush strips and the backer
in every level (a far door must stay gap-free). Switch 4 / 12 m, never culled (as Kit_DoorFrame_Steel_DA). Slots
Prop_SteelBrown (the sleeve; the RN-K / RN-F frames' dark bronze), Prop_PlasticBlack (strips, backer). No collider.
"""

import math
import sys

import interact_door_common as dc
import interact_door_da_common as da

NAME = "Kit_DoorFrame_Steel_DA_Wall"
LOD1_RATIO = 0.37
LOD2_RATIO = 0.08
LOD_DISTANCES = (4.0, 12.0, None)
SMOOTH_ANGLE = 35.0

STEEL = "Prop_SteelBrown"
SEAL = "Prop_PlasticBlack"

WALL_FACE = 0.020                          # the wall face in the leaf's root (the dress: leaf S face 2 mm proud)
FACE = da.STEEL_FACE                       # 0.0508
PROUD = da.STEEL_PROUD                     # 0.0111
CASING_X = WALL_FACE + PROUD               # 0.0311
FOOT_IN = 0.0005                           # the return's foot 0.5 mm into the wall face (no slit)
SOFFIT_IN = 0.004                          # the soffit runs 4 mm into the wall (its end never shows)
Z_H, Z_L, Y_T = da.LINING_Z_H, da.LINING_Z_L, da.LINING_Y    # 0.002, 0.998, 2.098 (the DA lining lines)
STRIP_FRONT, STRIP_BACK = WALL_FACE + 0.0005, WALL_FACE - 0.0010
HINGE_GAP_Z = 0.0135                       # the hinge strip reaches past the stile's arc at the wall face (z 0.0123)
LATCH_GAP_Z = 0.9935                       # the latch strip from 1.5 mm behind the leaf's flat end (0.995)
HEAD_GAP_Y = 2.0935                        # the head strip from 1.5 mm under the leaf's top (2.095)
UNDERCUT_Y = 0.0165                        # the backer stands 1.5 mm over the leaf's foot (0.015)


def section(level):
    """Closed 16 ga section (d, X), d = Z_H - Z on the hinge jamb: the soffit's buried end -> the soffit -> the casing face ->
    the return to the wall (the S half of da.steel_sleeve_section at the prop's wall face)."""
    zo = Z_H - FACE
    outer_xz = [(WALL_FACE - SOFFIT_IN, Z_H), (CASING_X, Z_H), (CASING_X, zo), (WALL_FACE - FOOT_IN, zo)]
    convex = dc.BEND_RI + dc.SHEET_T
    radii = [0, convex, convex, 0] if level < 2 else [0] * 4
    outer = dc.fillet_polygon(outer_xz, radii, (3 if level == 0 else 1), closed=False)
    inner = dc.offset_polyline(outer, dc.SHEET_T, normals_left=False, closed=False)
    poly = outer + list(reversed(inner))
    return [(Z_H - z, x) for x, z in poly]


def strips(kit):
    """The black brush strips on the wall face behind the hinge, latch and head gaps, and the undercut's backer."""
    out = []
    for lo, hi, nm in (((STRIP_BACK, 0.0, Z_H - 0.0005), (STRIP_FRONT, Y_T + 0.0005, HINGE_GAP_Z), "seal hinge strip"),
                       ((STRIP_BACK, 0.0, LATCH_GAP_Z), (STRIP_FRONT, Y_T + 0.0005, Z_L + 0.0005), "seal latch strip"),
                       ((STRIP_BACK, HEAD_GAP_Y, Z_H - 0.0005), (STRIP_FRONT, Y_T + 0.0005, Z_L + 0.0005), "seal head strip"),
                       ((STRIP_BACK, 0.0, HINGE_GAP_Z - 0.0005), (STRIP_FRONT, UNDERCUT_Y, LATCH_GAP_Z + 0.0005), "seal undercut backer")):
        o = dc.obox(kit, SEAL, lo=lo, hi=hi, bevel=0.0, segs=1, name=nm)
        o["fr_seal"] = True
        da.tag_lods(o, "012")
        out.append(o)
    return out


def wear(P, N, edge, obj):
    """The sleeve's arrises a touch duller, low down more (carts and mops); the strips matt."""
    X, Y, Z = P
    r = g = b = 1.0
    if obj.get("fr_seal"):
        b -= 0.45
        return r, g, b
    if edge:
        g -= 0.12 + 0.30 * (1 - dc.smoothstep(0.08, 0.45, Y))
    return r, g, b


def build(kit):
    sheets = {}
    envelope = []
    for level in (0, 1, 2):
        sec = section(level)
        o = dc.u_sweep(kit, sec, STEEL, "jamb casing" if level == 0 else "jamb casing lod%d" % level,
                       z_h=Z_H, z_l=Z_L, y_t=Y_T, continuous=True)[0]
        da.tag_lods(o, str(level))
        sheets[level] = o
        envelope.append(o)
    envelope += strips(kit)

    dc.finalize(kit, SMOOTH_ANGLE, wear, protect=envelope)

    # Envelope: in front of the wall by the true F4 section, never deeper than the soffit's buried end; the face band 50.8 mm.
    for level in (0, 1, 2):
        lo, hi = dc.bounds_unity([sheets[level]])
        assert abs(hi[0] - CASING_X) < 1e-4, "casing face x %.5f (want %.5f)" % (hi[0], CASING_X)
        assert lo[0] >= WALL_FACE - SOFFIT_IN - dc.SHEET_T - 1e-4, "soffit end x %.5f" % lo[0]
        assert abs(lo[2] - (Z_H - FACE)) < 1e-4 and abs(hi[2] - (Z_L + FACE)) < 1e-4, "face band z %.5f %.5f" % (lo[2], hi[2])
        assert abs(hi[1] - (Y_T + FACE)) < 1e-4 and lo[1] > -1e-6, "head y %.5f" % hi[1]
    # Nothing inside the shut leaf (the DA leaf: faces +-0.022, the stile's arc about z 0.036, the flat end 0.995, y 0.015-2.095),
    # the strips (on the wall face behind the gaps) apart.
    for o in kit.parts:
        if o.get("fr_seal"):
            continue
        mw = o.matrix_world
        for v in o.data.vertices:
            X, Y, Z = dc.to_unity(mw @ v.co)
            inside = (-da.LEAF_X + 1e-4 < X < da.LEAF_X - 1e-4) and (da.LEAF_Y0 + 1e-4 < Y < da.LEAF_Y1 - 1e-4) and \
                     (da.STILE_APEX_Z + 1e-4 < Z < da.LEAF_Z_LATCH - 1e-4)
            if inside and Z < da.STILE_Z_FACE:
                inside = math.hypot(X, Z - da.PIVOT_Z) < da.R_ARC - 1e-4
            assert not inside, "%s vertex %s inside the shut leaf" % (o.name, (round(X, 4), round(Y, 4), round(Z, 4)))
    # The strips stay behind the leaf's corridor face (x 0.022) and in front of the wall face.
    lo, hi = dc.bounds_unity([o for o in kit.parts if o.get("fr_seal")])
    assert hi[0] < da.LEAF_X - 0.001 and hi[0] > WALL_FACE, "strips x %.5f" % hi[0]

    dc.anchor(kit, "map_hinge", 0.0, 0.0, 0.0)
    dc.anchor(kit, "strike", 0.0, da.STRIKE_MORTISE[2], Z_L)
    dc.anchor(kit, "wall_face", WALL_FACE, 0.0, 0.5)
    dc.anchor(kit, "casing_face", CASING_X, 1.0, 0.5)
    kit.meta["wallFace"] = WALL_FACE
    kit.meta["casing"] = {"face": FACE, "proud": PROUD, "x": CASING_X}
    kit.meta["doorRoot"] = ("the prop leaf's root (Kit_DoorLeaf_SteelRun_DA): origin the opening's hinge-jamb edge on the leaf's "
                            "centre plane at the floor, +Z along the opening, +X the corridor face S; the wall face at X 0.020")
    kit.meta["acting"] = "double"
    kit.no_collider()
    kit.tag("door", "door_frame", "frame_steel", "run", "run_side_door", "double_acting", "one_face")
    kit.meta["frameType"] = "steel"
    da.lod_meta(kit, sys.modules[__name__])
