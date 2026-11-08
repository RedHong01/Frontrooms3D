"""Kit_DoorLite_A1_FRP: the moulded FRP glazing rings of the pool pair's A1 porthole (zone doors, Build B, 2026-11-06;
research/double_doors/20_design.md Part B, sections B3.3 and B8; door_vision 50 section 2.3: the A1 16 in porthole, researched
and kept, built here from its numbers).

The A1 porthole: 16 in clear (201.1 sq in >= 200; Cal. Title 8 3235(i)), centred on each leaf (the slab's x 0) at 55 in (y 1.397;
its clear bottom 47.0 in <= 48), 236 mm clear of the HW-DA1 push plate, 1.1985 m between the pair's two windows. The leaf's
cut-out (FrontRoomsDoubleDoorKit's code slab) is 425.4 mm; the 1/4 in clear tempered pane (6.0 mm, code) is 423.4 mm. The ring is
the moulded FRP glazing ring of the period's FRP doors (Pitt US 4,429,498: a moulded ring held by fibreglass pins under its sleeve;
research Part P): one per face, both faces alike, no screws, in the leaf's white gel coat (slot Door_PoolGelcoat). Its section
(d outward from the clear edge, w from the leaf's centre plane, the +Z face; -Z mirrored): the lip bedded on the glass (0, 3.05)
(the design's 1 mm silicone bed is the ring's own white lip: white on white, and no slit under the lip) -> the inner face to
(0, 14.0) -> (3.0, 20.5) -> the crown (5.8, 24.6), (9.2, 27.6), the top (12.6, 28.6) 6.35 mm (1/4 in) proud of the leaf face,
(15.6, 27.5), (17.6, 25.6), (18.9, 23.8) -> the feathered foot (20.2, 22.75) -> (21.9, 22.15), 0.1 mm into the leaf face
(seated: no slit, no shared plane): the ring meets the face along one clean line at d 21.6 (its 449.6 mm), the ring's outer
radius 225.1 mm. The sleeve down the cut-out is not modelled (hidden: the slab's cut-out wall and the pane close it).

Fix B (2026-11-06, Review B finding 4): Build B's last facet, (18.0, 25.0) -> (19.04, 22.15), stood about 70 deg off the face,
the class Fix A removed from the steel kits: it faced away from the ceiling lamps under the ring and took SSAO in its crease, so
the ring's foot rendered as a dark 3-4 px outline on desktop (42 against the face's 175 at 6 o'clock) and a near-black 32-gon
on phones (LOD1). Now the flank turns concave (55 -> 39 deg) and runs out at 19 deg over the last 2.3 mm (Fix A's 16-22 deg
feathered foot): lit like the face, a little darker at 6 o'clock, a soft band instead of a line. Every level has it (LOD1 at
19 deg, LOD2 at 23 deg). The crown, the lip and the clear aperture are unchanged.

Origin: the lite's centre on the leaf's centre plane (x across, y up, z through; +Z the leaf's +Z face); spawned under the slab at
the porthole's centre. LODs (fr_lods): LOD0 64 segments round x 10 profile segments a face (the lip's silhouette against the room
behind is the door's highest-contrast edge: 64 keep its facets to about a pixel at 0.35 m); LOD1 32 x 6; LOD2 24 x 2 (the crown
and the foot). Switch 2 / 8 m, culled at 40 m. Phones keep LOD0 within 2 m (Fix B: the ring kits skip ReducedTier, so the
32-gon never shows at the hero distance; FrontRoomsDoubleDoorKit.Zones.cs).
"""

import math
import sys

import bmesh

import interact_door_common as dc
import door_pair_common as pc
import kitlib

NAME = "Kit_DoorLite_A1_FRP"
LOD_DISTANCES = (2.0, 8.0, 40.0)
SMOOTH_ANGLE = 45.0                       # the crown turns 37 deg between two LOD0 profile segments: one smooth moulding
SLOT = "Door_PoolGelcoat"
kitlib.register_slot(SLOT, (0.791, 0.791, 0.745), 0.22, 0.0)   # preview only: the gel coat (sRGB 230 230 224)

IN = pc.IN
CLEAR_R = 8.0 * IN                        # 0.2032: 16 in clear
CUT_R = 0.2127                            # the slab's cut-out (FrontRoomsDoubleDoorKit.PortholeCut)
GLASS_R = 0.2117                          # the pane (FrontRoomsDoubleDoorKit.PortholeGlass), 1 mm short of the cut-out
GLASS_HALF_T = 0.003                      # 6.0 mm tempered
LITE_CY = 55.0 * IN                       # 1.397: the porthole's centre above the floor
FOOT_EMBED = 0.0001
# Fix B: the feathered foot. The last facet runs at 19.4 deg off the face from (20.2, 22.75) and ends 0.1 mm under it at d 21.9;
# it crosses the face plane at d 21.6 (the visible foot line).
FEATHER = (0.0202, 0.02275)
FOOT = (0.0219, pc.LEAF_HALF_T - FOOT_EMBED)
SECTIONS = {
    0: [(0.0, 0.00305), (0.0, 0.0140), (0.0030, 0.0205), (0.0058, 0.0246), (0.0092, 0.0276), (0.0126, 0.0286), (0.0156, 0.0275),
        (0.0176, 0.0256), (0.0189, 0.0238), FEATHER, FOOT],
    1: [(0.0, 0.00305), (0.0, 0.0140), (0.0058, 0.0246), (0.0126, 0.0286), (0.0176, 0.0256), FEATHER, FOOT],
    2: [(0.0, 0.00305), (0.0092, 0.0276), FOOT],
}
FOOT_MAX_DEG = 24.0                       # every level's last facet at most this far off the face (Fix A's 16-22 deg class)
ROUND = {0: 64, 1: 32, 2: 24}
OUTER_R = CLEAR_R + SECTIONS[0][-1][0]    # 0.2251: the ring's 450.2 mm with its feathered foot (Build B: 0.2222)


def lathe(kit, section, segs, sign, name):
    """One face's ring: the (d, w) section turned about the lite's axis (Unity z through the origin), at radius CLEAR_R + d and
    z = sign * w; every face turned to the section's visible side (the left of the walk from the lip to the foot)."""
    bm = bmesh.new()
    rings = []
    for d, w in section:
        r = CLEAR_R + d
        rings.append([bm.verts.new(pc.U(r * math.cos(2 * math.pi * i / segs), r * math.sin(2 * math.pi * i / segs), sign * w))
                      for i in range(segs)])
    for k in range(len(section) - 1):
        (d0, w0), (d1, w1) = section[k], section[k + 1]
        dd, dw = d1 - d0, w1 - w0
        ln = math.hypot(dd, dw) or 1.0
        nd, nw = -dw / ln, dd / ln         # the left of the walk: the visible side
        for i in range(segs):
            j = (i + 1) % segs
            am = 2 * math.pi * (i + 0.5) / segs
            want = (nd * math.cos(am), nd * math.sin(am), sign * nw)
            pc._face(bm, (rings[k][i], rings[k][j], rings[k + 1][j], rings[k + 1][i]), want)
    return dc._part(kit, bm, SLOT, name)


def build(kit):
    parts = {0: [], 1: [], 2: []}
    for level in (0, 1, 2):
        for sign, tag in ((1, "p"), (-1, "n")):
            o = lathe(kit, SECTIONS[level], ROUND[level], sign, "ring %s" % tag if level == 0 else "ring %s lod%d" % (tag, level))
            pc.tag(o, str(level))
            parts[level].append(o)
    dc.finalize(kit, SMOOTH_ANGLE)
    for level in (0, 1, 2):
        (d0, w0), (d1, w1) = SECTIONS[level][-2], SECTIONS[level][-1]
        foot_deg = math.degrees(math.atan2(w0 - w1, d1 - d0))
        assert 0.0 < foot_deg <= FOOT_MAX_DEG, "LOD%d foot facet %.1f deg off the face" % (level, foot_deg)
        assert abs(w1 - (pc.LEAF_HALF_T - FOOT_EMBED)) < 1e-9 and w0 > pc.LEAF_HALF_T, "LOD%d foot not seated" % level
        lo, hi = pc.bounds(parts[level])
        assert hi[0] <= OUTER_R + 1e-4 and lo[0] >= -OUTER_R - 1e-4, "ring x %s %s" % (lo[0], hi[0])
        assert hi[2] <= 0.0286 + 1e-5 and lo[2] >= -0.0286 - 1e-5, "ring z %s %s" % (lo[2], hi[2])
        # the clear aperture is never narrowed, and the lip sits on the pane, inside the cut-out
        for o in parts[level]:
            mw = o.matrix_world
            for v in o.data.vertices:
                X, Y, Z = dc.to_unity(mw @ v.co)
                r = math.hypot(X, Y)
                assert r >= CLEAR_R * math.cos(math.pi / ROUND[level]) - 1e-5, "ring inside the clear aperture %s" % ((X, Y, Z),)
                if abs(Z) < pc.LEAF_HALF_T - FOOT_EMBED - 1e-6:
                    assert r <= CUT_R + 1e-6 and abs(Z) >= GLASS_HALF_T, "ring vertex in the leaf's body %s" % ((X, Y, Z),)
    # door vision 50 section 1 (Cal. Title 8 3235(i)) and the pair's plates
    area = math.pi * CLEAR_R * CLEAR_R / (IN * IN)
    assert area >= 200.0 and LITE_CY - CLEAR_R <= 48 * IN + 1e-6, "A1: >= 200 sq in, bottom <= 48 in (%.1f, %.4f)" % (area, LITE_CY - CLEAR_R)
    leaf_half = pc.LEAF_W / 2.0
    plate_x0 = leaf_half - 0.0381 - 0.1016
    assert plate_x0 - OUTER_R >= 0.230, "ring %.4f m from the push plate" % (plate_x0 - OUTER_R)
    assert LITE_CY - OUTER_R >= pc.LEAF_BOTTOM + 0.003 + 0.254 + 0.5, "ring over the kick plate"
    assert CUT_R - GLASS_R >= 0.001 - 1e-9 and CUT_R - CLEAR_R <= 0.019 - 1e-9
    pc.anchor(kit, "lite_centre", 0.0, 0.0, 0.0)
    kit.meta["liteForm"] = "A1"
    kit.meta["liteShape"] = "round"
    kit.meta["liteRect"] = [-CLEAR_R, CLEAR_R, -CLEAR_R, CLEAR_R]
    kit.meta["cutRect"] = [-CUT_R, CUT_R, -CUT_R, CUT_R]
    kit.meta["clearRadius"] = CLEAR_R
    kit.meta["cutRadius"] = CUT_R
    kit.meta["glassRadius"] = GLASS_R
    kit.meta["outerRadius"] = OUTER_R
    kit.meta["liteCentreOnLeaf"] = [0.0, LITE_CY]
    kit.meta["liteAreaSqIn"] = round(area, 1)
    kit.meta["origin"] = "the lite's centre on the leaf's centre plane; x across, y up, z through (+Z the leaf's +Z face); both faces alike, no screws"
    kit.no_collider()
    kit.tag("door", "lite", "vision_lite", "porthole", "zone_pair", "pool")
    dc.lod_meta(kit, sys.modules[__name__])
    kit.meta["lodNote"] = "hand-built LOD0/1/2 (kitlib fr_lods); lodDistances (d01, d12, dcull) m at lodBias 1, FOV 76"
