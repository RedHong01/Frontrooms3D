"""Powered systems-furniture panel: Kit_CubiclePanel with one duplex
receptacle set sideways into the dark base rail on each face (the pod's
spine panel, where the desks plugged in).

Real-world reference: Steelcase Series 9000 / Haworth-era powered panels.
Pre-1991 ("original", before the Enhanced base) hard-wired panels carry the
wiring in a raceway at the base with "at least one electrical outlet or
plug receptacle ... along each wiring way" (Steelcase US 4,203,639, 1978/80;
01_period_research §6.2, S30). One duplex per side, centred about 0.05-0.07
m above the floor: here 0.07 m, the rail's own centre.

Construction: ``cubicle_panel.build_panel(kit, 1.524, 1.52)`` unchanged
(1,524 x 1,520 x 64 mm: fabric, putty cap and trims, 100 mm dark base rail
on two glides; its collider, anchor and LOD1 shim). Then, on each face, a
receptacle unit on the base rail face (|y| = 30 mm):
* a black bezel 78 x 44 mm (R 3), 1.0 mm proud of the rail, whose inner
  wall steps 0.5 mm DOWN to the ivory insert (72 x 38, R 2), so the
  receptacle reads as set into the rail;
* a sideways duplex (§1.2 5-15R faces, ground toward +u) standing 1.0 mm
  out of the insert: 1.5 mm proud of the rail in all (spec: <= 2 mm), and
  2.0 mm inside the panel's own bounds (the cap is 33.5 mm half-thick).
The front unit sits at x = +0.30 m, the back unit at x = -0.30 m, so the
panel reads the same after a 180 deg turn.

DETAIL: the receptacle uses the helper's "mid" face (32 segments per circle,
1-segment 0.4 mm edge, 3 mm slots, no brass contacts: ≤ 4 slots), seen from
>= 1 m on a floor-level rail. Chord error at 0.3 m: 0.078 mm (0.18 px).

LOD: the panel's own ``LOD1 = 0.45`` through _workstation_lod.sharp_lod1;
every receptacle part is flagged for deletion at LOD1 (the shim's fr_lod1_drop
face attribute, plus kit.lod1_drop), and the rail is untouched, so LOD1 is
exactly Kit_CubiclePanel's LOD1. The collider is kept (it is furniture).
ORIGIN / FRONT as Kit_CubiclePanel (floor centre; front -Y = Unity +Z).
Budget (10_spec §1.3): panel + 900 tris at LOD0, panel LOD1 at LOD1.
Slots (≤ 4): Prop_SteelPutty (submesh 0, as the panel), Prop_PlasticBlack,
Prop_FabricCubicle, Prop_NylonIvory. Era: no text, logos or dates.
"""

import sys

from cubicle_panel import H, T, build_panel
from _workstation_lod import lod1_drop as ws_lod1_drop

import outlet_box_common as B

NAME = "Kit_CubiclePanel_Powered"
LOD1 = 0.45

PANEL_TRIS, PANEL_TRIS_LOD1 = 456, 184     # Kit_CubiclePanel sidecar (built 2026-10-02/03)
BUDGET = (PANEL_TRIS + 900, PANEL_TRIS_LOD1, 0)
SMOOTH_ANGLE = 35.0

PB, NI = B.PB, B.NI
MM = B.MM
RAIL_FACE = (T - 0.004) / 2 / MM          # 30.0 mm: the base rail face |y|
RAIL_Z = 70.0                              # rail centre = receptacle centre (mm)
UNIT_X = 300.0                             # receptacle centre along the panel (ESTIMATE)
BEZEL = (78.0, 44.0, 3.0)                  # ESTIMATE
INSERT = (72.0, 38.0, 2.0)                 # ESTIMATE
BEZEL_PROUD, INSERT_PROUD = 1.0, 0.5


def receptacle(kit, fr, name):
    """One receptacle unit; fr origin on the rail face, n out of the panel."""
    bw, bh, br = BEZEL
    iw, ih, ir = INSERT
    mb = B.MB()
    outer0 = mb.ring(fr, B.rrect(bw, bh, br, 2), -0.3)
    outer1 = mb.ring(fr, B.rrect(bw, bh, br, 2), BEZEL_PROUD)
    inner1 = mb.ring(fr, B.rrect(iw, ih, ir, 2), BEZEL_PROUD)
    inner0 = mb.ring(fr, B.rrect(iw, ih, ir, 2), INSERT_PROUD)
    mb.band(outer0, outer1, PB)
    mb.band(outer1, inner1, PB)
    mb.band(inner1, inner0, PB)
    mb.cap(inner0, NI, front=True)
    B.duplex(mb, fr, INSERT_PROUD, "mid", sideways=True, contact_slot=PB)
    obj = mb.to_part(kit, name, "0")
    return obj


def build(kit):
    build_panel(kit, 1.524, H)
    panel_parts = list(kit.parts)
    for obj in panel_parts:                    # fr_wear on the panel's own parts (white)
        B.paint_wear(obj, None)
    units = [receptacle(kit, B.front_frame(UNIT_X * MM, -RAIL_FACE * MM, RAIL_Z * MM), "receptacle_front"),
             receptacle(kit, B.back_frame(-UNIT_X * MM, RAIL_FACE * MM, RAIL_Z * MM), "receptacle_back")]

    def wear(p, n, slot):
        return (1.0, 0.85, 1.0) if slot == PB and abs(n[2]) < 0.9 else (1.0, 1.0, 1.0)   # bezel edges

    B.finalize(kit, SMOOTH_ANGLE, wear, parts=units, weighted=False)
    for obj in units:
        ws_lod1_drop(obj)
        kit.lod1_drop(obj)

    kit.anchor("receptacle_front", (UNIT_X * MM, -(RAIL_FACE + INSERT_PROUD + B.FACE_PROUD) * MM, RAIL_Z * MM))
    kit.anchor("receptacle_back", (-UNIT_X * MM, (RAIL_FACE + INSERT_PROUD + B.FACE_PROUD) * MM, RAIL_Z * MM))
    kit.tag("outlet", "outlet_surface", "powered_panel")
    kit.meta["powered"] = {"receptacles": 2, "height": RAIL_Z * MM, "x": [UNIT_X * MM, -UNIT_X * MM],
                           "proudOfRail": (INSERT_PROUD + B.FACE_PROUD) * MM}

    # Self-checks: proud <= 2 mm off the rail and inside the panel bounds;
    # triangles within +-15 % of panel + 900.
    lo, hi = B.bounds_mm(units)
    proud = max(-lo[1], hi[1]) - RAIL_FACE
    assert proud <= 2.0 + 1e-6, "receptacle %.2f mm proud of the rail" % proud
    assert max(-lo[1], hi[1]) < T / 2 / MM + 1.5 - 1e-6, "receptacle leaves the panel bounds"
    n_units = sum(B.tris(o) for o in units)
    n_panel = sum(B.tris(o) for o in panel_parts)
    print("[o3] %s receptacles %d tris (2 units), proud %.2f mm of the rail; z %.1f..%.1f" % (
        kit.name, n_units, proud, lo[2], hi[2]))
    for o in units:
        print("[o3]    %-26s %5d tris  %s" % (o.name, B.tris(o), ",".join(m.name for m in o.data.materials)))
    assert abs(n_units - 900) <= 0.15 * 900, "receptacles %d tris vs +900" % n_units
