"""Pressed-steel vision lite kits for the title pair's steel leaves (zone doors, Build A, 2026-11-06; research/double_doors/
20_design.md Part A, sections A4.4, A4.5, A5). NOT AN ASSET: door_lite_f1_steel (Kit_DoorLite_F1_Steel, back-of-house B-2)
and door_lite_f5_steel (Kit_DoorLite_F5_Steel, Run! C-1) import it.

Door vision set A (Red 2026-10-30 "A 按区域四种"; door_vision 50 / 60): F1 10 x 30 in and F5 15 x 15 in, centred on the
leaf. The kit is interact_door_lite_common's pressed steel section (a 45 deg sight chamfer to a flat top, the leg down the
cut-out against the 1/4 in wired glass; Fix A, 2026-11-06: its outer edge a low feathered slope into the leaf face, see
door_pair_common.steel_section) on the title pair's 44.5 mm leaf (faces at 0.02225: the flange back 0.1 mm under
the face, the top 2.55 mm proud), both faces, six slotted oval-head screws on the -Z face (7.5 mm out, on the flat) (the face the title's forward walk
meets; F5 keeps six, Red's set A). The glass is not in the kit: FrontRoomsDoubleDoorKit's code glass box (Glass_Wired,
FrontRoomsDoorLite LIVE / DARK) stands in the slab's cut-out, as on the walnut pair.

Origin: the lite's centre on the leaf's centre plane (x across, y up, z through; +Z the leaf's +Z face); spawned under the
slab. Slot: Painted_Metal (FrontRoomsDoubleDoorKit swaps it to the leaf's own paint at spawn; the screw heads are painted
over, as field-painted kits were). LODs (fr_lods): LOD0 the full section and the screws; LOD1 a 7-point section; LOD2 a
flat ring per face. Switch 2 / 8 m, culled at 40 m.
"""

import door_pair_common as pc
import interact_door_common as dc
import kitlib

SLOT = "Painted_Metal"
kitlib.register_slot(SLOT, (0.644, 0.610, 0.509), 0.5, 0.0)


def local_rect(rect):
    x0, x1, y0, y1 = rect
    xc, yc = (x0 + x1) / 2.0, (y0 + y1) / 2.0
    return (x0 - xc, x1 - xc, y0 - yc, y1 - yc), (xc, yc)


def build_lite(kit, key, rect, rng):
    r, (xc, yc) = local_rect(rect)
    for level in (0, 1):
        for sign, tag in ((1, "p"), (-1, "n")):
            o = pc.rect_sweep(kit, pc.steel_section(level), r, sign, SLOT, "lite kit %s" % tag if level == 0 else "lite kit %s lod1" % tag)
            pc.tag(o, str(level))
    for sign, tag in ((1, "p"), (-1, "n")):
        o = pc.flat_ring(kit, r, sign, pc.KIT_TOP, pc.KIT_FACE, SLOT, "lite ring lod2 %s" % tag, w_outer=pc.KIT_BACK)
        pc.tag(o, "2")
    screws = pc.rect_screws(kit, r, -1, SLOT, rng, count_per_stile=2, name="lite screw")
    dc.finalize(kit, 35.0)
    lo, hi = pc.bounds(pc.level_parts(kit, 0))
    assert hi[0] <= r[1] + pc.KIT_FACE + 1e-4 and lo[0] >= r[0] - pc.KIT_FACE - 1e-4, "kit x %s %s" % (lo[0], hi[0])
    assert hi[2] <= pc.KIT_TOP + 0.0016 and lo[2] >= -(pc.KIT_TOP + 0.0016), "kit z %s %s (screw domes on -Z)" % (lo[2], hi[2])
    # in the pair's leaf: clear of the push plate (centre 45 in, 4 x 16 in, 1-1/2 in from the meeting edge) and the kick plate
    leaf_half = pc.LEAF_W / 2.0
    plate_x0 = leaf_half - 0.0381 - 0.1016
    assert rect[1] + pc.KIT_FACE <= plate_x0 - 0.050, "kit %.4f m from the push plate" % (plate_x0 - rect[1] - pc.KIT_FACE)
    assert rect[2] - pc.KIT_FACE >= pc.LEAF_BOTTOM + 0.003 + 0.254 + 0.040, "kit over the kick plate"
    assert pc.area_sq_in(rect) >= 200.0 - 1e-6 and rect[2] <= 48 * pc.IN + 1e-6, "Cal. Title 8 3235(i): >= 200 sq in, bottom <= 48 in"
    pc.anchor(kit, "lite_centre", 0.0, 0.0, 0.0)
    kit.meta["liteForm"] = key
    kit.meta["liteRect"] = [r[0], r[1], r[2], r[3]]
    kit.meta["cutRect"] = [r[0] - pc.POCKET, r[1] + pc.POCKET, r[2] - pc.POCKET, r[3] + pc.POCKET]
    kit.meta["liteCentreOnLeaf"] = [xc, yc]
    kit.meta["liteAreaSqIn"] = round(pc.area_sq_in(rect), 1)
    kit.meta["liteScrews"] = len(screws)
    kit.meta["origin"] = "the lite's centre on the leaf's centre plane; x across, y up, z through (+Z the leaf's +Z face); screws on -Z"
    kit.no_collider()
    kit.tag("door", "lite", "vision_lite", "zone_pair")
