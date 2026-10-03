"""Hollow-metal locked leaf with a narrow vision lite (RN-K option, P2;
Red's call: a see-through hole, 05 §6.4): Kit_DoorLeaf_Steel plus a 4" x
25" lite on the latch side, 6 mm glass, and a 45 deg bevelled steel lite
kit on both faces (10_spec §2.3 "Kit_DoorLeaf_SteelLite").

Real-world reference: the US hollow-metal storeroom/utility door with a
narrow vision lite, 1960s-1990: the lite cut-out factory-prepped, a
pressed-steel "low-profile" lite kit with 45 deg bevelled glazing stops,
painted to match the door, screwed from the secure (pull) side; 1/4"
glass. The wired-glass look needs Glass_Wired from the glass track
(P-5); until then the slot is the kit's Prop_Glass.

Origin: DOOR ROOT D, closed pose (§1.2), as Kit_DoorLeaf_Steel.

Geometry (Unity m): the steel leaf; visible lite 0.102 (Z) x 0.635 (Y)
centred Y 1.575, Z 0.755 (clear of the sign at 1.524 / Z 0.500 and the
escutcheon); slab cut-out 9.5 mm larger each side; glass X +-0.003; lite
kit 19 mm face, 2.5 mm proud, 45 deg bevels at the sight and outer edges;
six slotted oval-head screws on the S kit.

GAMEPLAY FLAG (02 §6.1): the hole shows the next room, but the Relay's
sight ray stops at the 0.05 x 2.08 x 0.98 leaf collider, so the player can
see it while it cannot see the player. Red / the map chat decide.

Budget (§9.1): 5,200 / 1,900 / 200 tris; slots Door_Enamel (submesh 0),
Prop_Aluminium, Prop_Chrome, Prop_Glass. LOD1 0.40. Render-only.
"""

import random
import sys

import interact_door_common as dc
import interact_door_leaf_steel as base

NAME = "Kit_DoorLeaf_SteelLite"
LOD1 = 0.40
LOD1_RATIO = 0.40
LOD2_RATIO = 0.04
LOD_DISTANCES = (4.0, 12.0, None)
SMOOTH_ANGLE = 35.0

GLASS = "Prop_Glass"
LITE_W, LITE_H, LITE_Y, LITE_Z = 0.102, 0.635, 1.575, 0.755
POCKET = 0.0095


def kit_section(side):
    """(d, X): d outward from the visible edge; X on face ``side``."""
    pts = [(0.0, 0.0031), (0.0, 0.0215), (0.0030, 0.0245), (0.0170, 0.0245), (0.0190, 0.0225), (0.0190, 0.0220),
           (POCKET, 0.0220), (POCKET, 0.0031)]
    return [(d, side * x) for d, x in pts]


def rect_sweep_yz(kit, section, z0, z1, y0, y1, slot, name):
    """Sweep a closed (d, X) section round the rectangle Z z0..z1, Y y0..y1
    (in the leaf's face plane), d growing outward; mitred corners."""
    import bmesh
    bm = bmesh.new()
    rings = []
    for d, x in section:
        rings.append([bm.verts.new(dc.U(x, y, z)) for z, y in ((z0 - d, y0 - d), (z1 + d, y0 - d), (z1 + d, y1 + d), (z0 - d, y1 + d))])
    n = len(section)
    for k in range(n):
        a, b = rings[k], rings[(k + 1) % n]
        for i in range(4):
            j = (i + 1) % 4
            bm.faces.new((a[i], a[j], b[j], b[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return dc._part(kit, bm, slot, name)


def extra(kit, slab, cuts, rng):
    z0, z1 = LITE_Z - LITE_W / 2, LITE_Z + LITE_W / 2
    y0, y1 = LITE_Y - LITE_H / 2, LITE_Y + LITE_H / 2
    cuts.append(dc.cutter((-0.03, y0 - POCKET, z0 - POCKET), (0.03, y1 + POCKET, z1 + POCKET)))
    glass = dc.obox(kit, GLASS, lo=(-0.003, y0 - POCKET + 0.001, z0 - POCKET + 0.001),
                    hi=(0.003, y1 + POCKET - 0.001, z1 + POCKET - 0.001), name="lite glass")
    for side, tag in ((1, "s"), (-1, "p")):
        rect_sweep_yz(kit, kit_section(side), z0, z1, y0, y1, base.ENAMEL, "lite kit " + tag)
    ym = (y0 + y1) / 2
    for y, z in ((y0 - 0.012, LITE_Z), (y1 + 0.012, LITE_Z), (y0 + 0.12, z0 - 0.012), (y1 - 0.12, z0 - 0.012),
                 (y0 + 0.12, z1 + 0.012), (y1 - 0.12, z1 + 0.012)):
        s = dc.oval_head(kit, (0.0245, y, z), (1, 0, 0), base.CHROME, rng, d=0.0055, half_segs=6, dome_k=0.16, name="lite screw")
        kit.lod1_drop(s)
        dc.lod2_drop(s)
    return []


def build(kit):
    rng = random.Random(41100)
    base.build_steel(kit, rng, extra=lambda k, s, c: extra(k, s, c, rng))
    blo, bhi = dc.bounds_unity(kit.parts)
    assert bhi[0] <= dc.AXIS_X + dc.KNUCKLE_R + 1e-4 and blo[0] >= -(dc.LEAF_X + base.KICK_T + 0.0035), "proud limit"
    trim = 0.0285
    assert LITE_Z - LITE_W / 2 - trim > 0.500 + 0.127 + 0.02, "lite kit clear of the sign plate (Z 0.373..0.627)"
    assert LITE_Y - LITE_H / 2 - trim > 0.968 + 0.1016 + 0.05, "lite kit clear of the escutcheon"
    assert LITE_Z + LITE_W / 2 + trim < dc.LEAF_Z_LATCH_P - 0.10, "lite kit clear of the latch stile"
    base.anchors(kit)
    dc.anchor(kit, "lite_centre", 0.0, LITE_Y, LITE_Z)
    kit.meta["glassSlab"] = [LITE_W + 2 * POCKET - 0.002, LITE_H + 2 * POCKET - 0.002, 0.006]
    kit.meta["gameplayNote"] = "see-through lite; the Relay's sight stops at the leaf collider (02 6.1)"
    kit.tag("lite", "run")
    dc.lod_meta(kit, sys.modules[__name__])
