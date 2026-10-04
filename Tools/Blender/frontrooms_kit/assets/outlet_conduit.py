"""1/2 in EMT conduit tile: one metre of plain thin-wall steel tube running
up the wall from a surface handy box to the ceiling.

Real-world reference: 1/2 in trade size EMT (electrical metallic tubing),
OD 0.706 in = 17.9 mm, galvanized, sold in 10 ft sticks
(01_period_research §6.4). Unchanged since the 1950s: fits 1990 and older
retrofits alike. No strap on the tile (Kit_ConduitStrap is separate, every
1.5 m), no coupling, no markings (era lock: no printed text).

Construction: an open tube (no end caps; the ends always sit inside the
connector socket, the next tile or the ceiling), 48 segments round, 6 rings
along the metre (the bands carry fr_wear and keep the triangles short for
vertex fog). Radial normals only, so a tile scaled in Unity Y (R3 scales the
last tile of a run to meet the ceiling) keeps its round shading: tested at
0.4.

ORIGIN = THE BOTTOM OF THE TILE ON THE WALL PLANE: (0, 0, 0) is the wall
point behind the tube axis; the axis runs at y = -11.5 mm (E_AXIS, shared
with the handy box connector and the strap), from z = 0 to z = 1.000 m.
Unity: +Y up along the tube, front +Z. Place it at Kit_OutletHandyBox's
``conduit_base`` anchor with the box's own rotation.

Budget (10_spec §1.3): 600 / 220 / 24 tris, LOD_DISTANCES 3 / 8 / 24 m.
Slot: Prop_Aluminium (galvanized). Render-only (no collider, no shadows).
Chord error of 48 segments at 0.3 m: 8.95 mm x (1 - cos 3.75 deg) = 0.019 mm
(0.04 px at 1080p, FOV 76).
"""

import outlet_box_common as B

NAME = "Kit_ConduitEMT"
LOD1 = None
LOD1_RATIO = 220 / 600.0
LOD2_RATIO = 24 / 600.0
LOD_DISTANCES = (3.0, 8.0, 24.0)
BUDGET = (600, 220, 24)
SMOOTH_ANGLE = 35.0

LENGTH = 1000.0
SEGS, BANDS = 48, 6


def _tube(kit, segs, bands, lods, name):
    mb = B.MB()
    r = B.EMT_OD / 2 * B.MM
    path = [(0.0, -B.E_AXIS * B.MM, LENGTH * B.MM * k / bands) for k in range(bands + 1)]
    B.sweep(mb, path, [r] * len(path), segs, B.AL, ref=(0, -1, 0))
    return mb.to_part(kit, name, lods)


def build(kit):
    _tube(kit, SEGS, BANDS, "0", "tube")
    if B.lods_enabled():
        _tube(kit, 22, 5, "1", "lod1")
        _tube(kit, 12, 1, "2", "lod2")
    B.finalize(kit, SMOOTH_ANGLE, None)
    kit.anchor("bottom", (0.0, 0.0, 0.0))
    kit.anchor("top", (0.0, 0.0, LENGTH * B.MM))
    import sys
    module = sys.modules[__name__]
    _, lo, hi = B.box_meta(kit, module, "outlet_surface", B.EMT_OD, LENGTH, wall=True, extra_tags=("conduit",),
                           outlet_extra={"tileLength": LENGTH * B.MM, "axisOffset": B.E_AXIS * B.MM,
                                         "od": B.EMT_OD * B.MM, "scaleAxis": "Y", "caps": False})
    assert abs((hi[0] - lo[0]) - B.EMT_OD) < 0.05 and abs(hi[2] - LENGTH) < 1e-3 and abs(lo[2]) < 1e-3
    assert abs((lo[1] + hi[1]) / 2 + B.E_AXIS) < 0.02, "tube axis off E_AXIS"
    print("[o3] %s OD %.3f mm, axis y %.3f mm, z %.1f..%.1f" % (kit.name, hi[0] - lo[0], (lo[1] + hi[1]) / 2, lo[2], hi[2]))
