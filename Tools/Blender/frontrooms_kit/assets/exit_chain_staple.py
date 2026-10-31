"""Kit_ChainStaple: the wall anchor of the EXIT door's after-hours chain (游戏视觉, unlock micro cut scene, 2026-10-31;
research/unlock/10_research.md §1 + §5, 20_design.md §2.2). It replaces A13's eye bolt (a 44 mm ring in 9 mm wire,
thinner than the chain it held, with no plate: "a picture hook").

Real-world reference: the period's site-made chain anchor beside a crash-bar exit (research §1: chained panic bars at
Stardust 1981, chained push bars in inspection reports): a 3 x 5 x 1/4 in steel plate, long side vertical, held by
four 3/8 in masonry sleeve anchors (hex nut and washer), a 1/2 in round-bar staple welded to it. Painted over with the
door's putty enamel when the stairs were painted; the staple is bare, worn by the chain.

PART frame: origin = the plate's back centre on the wall face; +Z out of the wall, +Y up, +X along the wall. The
staple is a U in the Y-Z plane (legs one above the other, 25 mm clear between them, standing 38 mm out): the chain's
link threads it with the staple's apex wire (vertical) through the middle of the link (anchor "staple_apex").

LODs (hand-built): LOD0 plate (1 mm bevel), washers, hex nuts with chamfers and the stud ends, the staple (16 sides)
and its two weld beads; LOD1 plate, plain hex nuts, staple 8 sides; LOD2 plate + staple 6 sides. Switch 2 / 6 m,
never culled. Slots: Prop_SteelPutty (the painted plate, submesh 0), Prop_SteelLaminated (nuts, washers, staple).
Render-only.
"""

import math
import sys

import exit_lock_common as L
import interact_door_common as dc
import kitlib

NAME = "Kit_ChainStaple"
SMOOTH_ANGLE = 40.0
LOD_DISTANCES = (2.0, 6.0, None)
LOD1_RATIO = 0.3
LOD2_RATIO = 0.1

PAINT = "Prop_SteelPutty"
STEEL = "Prop_SteelLaminated"
kitlib.register_slot(STEEL, (0.604, 0.612, 0.592), 0.45, 1.0)


def staple_line(step):
    """The staple's centre line: lower leg out, the bow, the upper leg back (PART frame)."""
    z0 = L.PLATE_T - 0.0015                     # the legs start 1.5 mm into the plate (welded)
    zc = L.STAPLE_APEX_Z - L.STAPLE_HALF        # the bow's centre
    h = L.STAPLE_HALF
    pts = []
    n = max(2, int(round((zc - z0) / step)))
    for k in range(n + 1):
        pts.append((0.0, -h, z0 + (zc - z0) * k / n))
    segs = max(6, int(round(math.pi * h / step)))
    for k in range(1, segs):
        a = -math.pi / 2 + math.pi * k / segs
        pts.append((0.0, h * math.sin(a), zc + h * math.cos(a)))
    for k in range(n + 1):
        pts.append((0.0, h, zc - (zc - z0) * k / n))
    return pts


def staple(kit, step, sides, lods):
    r = L.STAPLE_R
    sec = [(r * math.cos(2 * math.pi * i / sides), r * math.sin(2 * math.pi * i / sides)) for i in range(sides)]
    obj = dc.sweep_path(kit, staple_line(step), sec, STEEL, "staple", cap=True, up=(1, 0, 0))
    obj["fr_lods"] = lods
    return obj


def plate(kit, bevel, lods):
    obj = dc.obox(kit, PAINT, lo=(-L.PLATE_W / 2, -L.PLATE_HT / 2, 0.0), hi=(L.PLATE_W / 2, L.PLATE_HT / 2, L.PLATE_T),
                  bevel=bevel, segs=2, name="anchor plate")
    obj["fr_lods"] = lods
    return obj


def fixing(kit, x, y, detail, lods):
    z = L.PLATE_T
    parts = []
    if detail:
        w = dc.revolve(kit, [(L.WASHER_R, 0.0), (L.WASHER_R, L.WASHER_T - 0.0003), (L.WASHER_R - 0.0003, L.WASHER_T), (0.0, L.WASHER_T)],
                       (x, y, z), (0, 0, 1), 20, STEEL, "washer", close_start=False)
        parts.append(w)
        z += L.WASHER_T
    # the hex nut: a 6-sided prism (across flats NUT_AF), its top edge chamfered on LOD0
    rf = L.NUT_AF / math.sqrt(3.0)
    prof = [(rf, 0.0), (rf, L.NUT_H - (0.0008 if detail else 0.0)), (rf - (0.0012 if detail else 0.0), L.NUT_H), (0.0, L.NUT_H)]
    nut = dc.revolve(kit, prof, (x, y, z), (0, 0, 1), 6, STEEL, "hex nut", phase=math.pi / 6, close_start=False)
    parts.append(nut)
    if detail:
        stud = dc.revolve(kit, [(L.STUD_R, 0.0), (L.STUD_R, 0.0025), (L.STUD_R - 0.0008, 0.0032), (0.0, 0.0032)],
                          (x, y, z + L.NUT_H), (0, 0, 1), 12, STEEL, "stud end", close_start=False)
        parts.append(stud)
    for p in parts:
        p["fr_lods"] = lods
    return parts


def weld(kit, y, lods):
    r = L.STAPLE_R
    prof = [(r + 0.0024, 0.0), (r + 0.0020, 0.0012), (r + 0.0010, 0.0024), (r - 0.0002, 0.0030)]
    obj = dc.revolve(kit, prof, (0.0, y, L.PLATE_T), (0, 0, 1), 16, STEEL, "weld bead", close_start=False, close_end=False)
    obj["fr_lods"] = lods
    return obj


def build(kit):
    plate(kit, 0.001, "0")          # painted plate first -> submesh 0
    plate(kit, 0.0, "12")
    for sx in (-1, 1):
        for sy in (-1, 1):
            fixing(kit, sx * L.ANCHOR_X, sy * L.ANCHOR_Y, True, "0")
            fixing(kit, sx * L.ANCHOR_X, sy * L.ANCHOR_Y, False, "1")
    staple(kit, 0.002, 16, "0")
    staple(kit, 0.005, 8, "1")
    staple(kit, 0.010, 6, "2")
    for y in (-L.STAPLE_HALF, L.STAPLE_HALF):
        weld(kit, y, "0")
    dc.finalize(kit, SMOOTH_ANGLE, None)
    lo, hi = dc.bounds_unity(kit.parts)
    assert abs(hi[2] - (L.PLATE_T + L.STAPLE_OUT)) < 3e-4, "staple stands 38 mm out (%s)" % (hi,)
    dc.anchor(kit, "staple_apex", 0.0, 0.0, L.STAPLE_APEX_Z)
    kit.meta["motion"] = {"type": "static", "parent": "EXIT frame dress (A13)",
                          "note": "the EXIT chain's wall anchor, 0.14 m past the latch side of the opening, 1.00 m up"}
    kit.no_collider()
    kit.tag("interactable", "exit", "chain_anchor")
    dc.lod_meta(kit, sys.modules[__name__])
