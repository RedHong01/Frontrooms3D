"""Q16 evacuation-plan placard: the snap frame's clear lens
(Kit_EvacPlacardLens). Spec: Documentation/research/placard/10_spec.md §1.3,
§2.4, §2.7, §3.2.

A 1.0 mm non-glare plastic sheet lying on the printed sheet, 432 x 279 mm,
w 4.55-5.55 mm; its edges sit under the clip rails. Modelled as the top face
at w 5.55 and four edge quads down to w 4.60, with NO bottom face (it would
be coplanar with the opaque sheet and z-fight). Same origin and frame as
Kit_EvacPlacard (spawned at the same transform). One slot, Prop_LensNonGlare
(URP Lit transparent, black base, alpha 0.04, smoothness 0.55, no shadow
caster). Its own asset so the lens has its own renderer and sort order, its
shadows can stay off, and WebGL can skip it. 10 tris; culled at 6 m.
"""

import evac_placard_common as pc
import interact_key_common as kc
from interact_key_common import U

NAME = "Kit_EvacPlacardLens"
SMOOTH_ANGLE = pc.SMOOTH_ANGLE
LOD1 = None


def build(kit):
    pc.register_slots()
    lens, b = pc.lens_box(kit)
    kc.wear_all(kit)
    pc.finalize(kit)
    kit.no_collider()
    kit.tag("placard", "sign", "wall_flush", "lens")
    M = pc.MM
    kit.anchor("back", U(0.0, 0.0, 0.0))
    kit.anchor("lens_top", U(0.0, 0.0, pc.LENS_TOP * M))
    kit.meta["placardLens"] = {"size": [pc.SHEET[0] * M, pc.SHEET[1] * M, pc.LENS_T * M], "top": pc.LENS_TOP * M,
                               "edgeLow": pc.LENS_EDGE_LOW * M, "slot": pc.LENS, "frameKit": "Kit_EvacPlacard"}
    kc.lod_meta(kit, (pc.LENS_CULL, pc.LENS_CULL, pc.LENS_CULL), (pc.LENS_BUDGET, pc.LENS_BUDGET, pc.LENS_BUDGET))
    kit.meta["lodNote"] = "One level; culled at 6 m (lodBias 1) once P-1 lands. Draw it only with the frame's LOD0/LOD1."
    lo, hi = pc.bounds_unity_mm([lens])
    assert abs((hi[0] - lo[0]) - pc.SHEET[0]) < 0.1 and abs((hi[1] - lo[1]) - pc.SHEET[1]) < 0.1, ("lens size", lo, hi)
    assert abs(lo[2] - pc.LENS_EDGE_LOW) < 0.01 and abs(hi[2] - pc.LENS_TOP) < 0.01, ("lens z", lo, hi)
    assert b.worst > 0.99, ("lens faces", b.worst)
    n = pc.tris_of([lens])
    assert n <= 12, ("lens tris", n)
    kit.meta["trianglesCheck"] = n
    print("[placard] %s %d tris, bounds mm %s..%s" % (kit.name, n, [round(x, 3) for x in lo], [round(x, 3) for x in hi]))
