"""The vision lite of every double-acting door leaf and of the C22 EXIT door (door vision, 2026-10-30;
Documentation/research/door_vision/30_design.md section 1, 10_period.md section 4, 50_lite_forms.md section 7; Red's set A,
2026-10-30 "A 按区域四种", 51_lite_forms_for_red.md; log 60_landing.md).

NOT AN ASSET: no NAME and no build(). The DA leaf modules call add_lite() through their existing ``extra(kit, level, cuts)``
hook (walnut bead on Kit_DoorLeaf_Veneer_DA, the leaf's own wood on _Veneer_Oak_DA; the pressed steel lite kit with oval-head
screws on face S on Kit_DoorLeaf_Steel_DA, _SteelRun_DA and _Ward_DA), then lite_meta() for the sidecar. The lobby's F4b
10 x 48 lives in FrontRoomsDoubleDoorKit (the title / station pair is not a kit); interact_door_lite_exit builds F13.

LITE FORMS (set A): F1 10 x 30 office, F5 15 x 15 run (6 screws: +568 / +584 tris), F13 4 x 25 exit. Every form is a Form,
its visible glass rect.

Real-world reference: Cal. Title 8 section 3235(i) (a double-acting door carries a vision panel of at least
200 sq in per leaf, bottom at most 48 in above the floor; wired glass named) and the period San Diego CWP
08110 / 08150 / 08800 door and glazing specs: 1/4 in polished wired glass (diamond mesh), a 45 deg bevelled
pressed-steel lite kit on hollow-metal leaves, a mitred solid-wood glazing bead on wood leaves. No maker marks.

Frame: DOOR ROOT D (closed pose), Unity metres: Y up from the floor, Z along the opening from the pivot axis
(latch end Z 0.995), X through the leaf (face S = +X).

DA-L1 geometry (30_design 1.2): visible glass Z 0.273..0.527, Y 1.143..1.905 (254 x 762 mm); glass blank = visible +
9.5 mm a side (the leaf cut-out), the glass 1 mm short of it on every edge, 6.8 mm thick (X +-0.0034); frame
outer edge 19 mm outside the visible edge (Z 0.254..0.546, Y 1.124..1.924). Kept outside the door-break lock
zone: the cassette window starts at Z 0.600 and the _FR_DAMAGE mask at Z 0.595 (54 / 49 mm of intact face).

LODs (switching at 4 m / 12 m as the leaves): the cut-out in EVERY level (the picture always matches the
player-side sight rule, which sees through the lite at any range); LOD0 the full frame section (+ the steel
kit's screws), LOD1 a 6-7 point section, LOD2 a flat ring per face; the glass box in every level.
Slots: the frame goes into the leaf's own submesh 0 slot (no extra opaque draw); the glass is Glass_Wired.
"""

import interact_door_common as dc
import interact_door_da_common as da

GLASS_SLOT = "Glass_Wired"
LITE_W, LITE_H = 0.254, 0.762            # DA-L1 visible glass, Z x Y (10 x 30 in)
LITE_Z, LITE_Y = 0.400, 1.524            # centre (bottom 45 in = 1.143, top 75 in = 1.905)
POCKET = 0.0095                          # leaf cut-out = visible + 9.5 mm a side (the glass blank)
GLASS_T = 0.0068                         # 1/4 in polished wired glass (6.8 mm)
GLASS_GAP = 0.001                        # glass 1 mm short of the cut-out on every edge (>= 0.5 mm, 30_design 1.5)
FRAME_FACE = 0.019                       # 19 mm sight face of the bead / kit
BREAK_WINDOW_Z = 0.600                   # door-break cassette window (52_production)
DAMAGE_MASK_Z = 0.995 - 0.40             # _FR_DAMAGE 0.40 x 0.60 m at damage_latch
DAMAGE_MASK_Y = (1.10 - 0.30, 1.10 + 0.30)
SIGN_Y, SIGN_Z, SIGN_H = 2.010, 0.400, 0.076   # the locked-door sign, moved to the top rail (30_design 1.2)
PEEK_REACH = 1.0                         # the peek is offered within 1.0 m of the glass (交互设计 2026-10-30, NOTE_1030)

Z0, Z1 = LITE_Z - LITE_W / 2, LITE_Z + LITE_W / 2
Y0, Y1 = LITE_Y - LITE_H / 2, LITE_Y + LITE_H / 2
LIP = GLASS_T / 2 + 0.0001                # the bead / kit lip on the glass face (X +-0.0035)
IN = 0.0254


class Form:
    """One lite form (50_lite_forms.md section 7): its visible glass rect (z0, z1, y0, y1); break_safe False skips the
    door-break window asserts and the top-rail sign's (the C22 door has neither)."""

    def __init__(self, key, label, rect, break_safe=True):
        self.key, self.label, self.rect, self.break_safe = key, label, rect, break_safe

    @property
    def area_sq_in(self):
        z0, z1, y0, y1 = self.rect
        return (z1 - z0) * (y1 - y0) / (IN * IN)


# Set A on the D1.6 leaf (Z from the pivot, faces Z 0.014..0.995). F13 hangs on the map's EXIT leaf (hinge frame: Z along the
# opening): 50_lite_forms puts it at Z 0.349-0.451, where the two stair plates at Z 0.500 cover it (6 x 6 in on the push face,
# 11 x 5 in on the pull face, Figma 3545:7368), so it moves to the lock side, the norm on a latched door (50 section 3): the
# frame stands 20 mm clear of the 11 in plate and the glass 8.2 in from the latch edge. No break cassette on this door.
FORMS = {
    "F1": Form("F1", "DA-L1 10 x 30", (0.2730, 0.5270, 1.1430, 1.9050)),
    "F5": Form("F5", "Square 15 x 15", (0.1495, 0.5305, 1.1938, 1.5748)),
    "F13": Form("F13", "Rated 4 x 25", (0.6790, 0.7806, 1.2700, 1.9050), break_safe=False),
}
DEFAULT = FORMS["F1"]

# (d, X): d outward from the visible edge, X on face +1 (mirrored for face -1). Closed polygons.
WOOD_SECTIONS = {
    # LOD0: 45 deg-ish sight chamfer to a 6 mm proud flat top, 0.6 mm arrises, the leg in the cut-out against the glass
    0: [(0.0, LIP), (0.0, 0.0095), (0.0006, 0.0103), (0.0146, 0.0274), (0.0152, 0.0280), (0.0184, 0.0280),
        (0.0190, 0.0274), (0.0190, 0.0220), (POCKET, 0.0220), (POCKET, LIP)],
    1: [(0.0, LIP), (0.0, 0.0100), (0.0150, 0.0280), (0.0190, 0.0280), (0.0190, 0.0220), (POCKET, 0.0220), (POCKET, LIP)],
}
STEEL_SECTIONS = {
    # interact_door_leaf_steel_lite.kit_section, inner lip widened to the 6.8 mm glass (X +-0.0031 -> +-0.0035)
    0: [(0.0, LIP), (0.0, 0.0215), (0.0030, 0.0245), (0.0170, 0.0245), (0.0190, 0.0225), (0.0190, 0.0220),
        (POCKET, 0.0220), (POCKET, LIP)],
    1: [(0.0, LIP), (0.0, 0.0215), (0.0030, 0.0245), (0.0190, 0.0245), (0.0190, 0.0220), (POCKET, 0.0220), (POCKET, LIP)],
}
RING_X = {"wood": 0.0280, "steel": 0.0245}   # LOD2 flat ring height (the frame's top)


def _side(section, sign):
    return [(d, sign * x) for d, x in section]


def mitred_side(kit, section, which, slot, name, grain=None, rect=None):
    """One straight frame member with 45 deg mitres at both ends (its own part, so wood grain runs along it).
    which: "zlo" / "zhi" (the stiles, along Y) or "ylo" / "yhi" (the rails, along Z)."""
    import bmesh
    z0, z1, y0, y1 = rect if rect is not None else (Z0, Z1, Y0, Y1)
    bm = bmesh.new()
    a, b = [], []
    for d, x in section:
        if which == "zlo":
            pa, pb = (x, y0 - d, z0 - d), (x, y1 + d, z0 - d)
        elif which == "zhi":
            pa, pb = (x, y0 - d, z1 + d), (x, y1 + d, z1 + d)
        elif which == "ylo":
            pa, pb = (x, y0 - d, z0 - d), (x, y0 - d, z1 + d)
        else:
            pa, pb = (x, y1 + d, z0 - d), (x, y1 + d, z1 + d)
        a.append(bm.verts.new(dc.U(*pa)))
        b.append(bm.verts.new(dc.U(*pb)))
    n = len(section)
    # no end caps: the mitre faces of two members coincide and are never seen (the budget, 30_design 1.4)
    cx = sum(x for d, x in section) / n
    cd = sum(d for d, x in section) / n
    for k in range(n):
        j = (k + 1) % n
        f = bm.faces.new((a[k], a[j], b[j], b[k]))
        f.normal_update()
        # outward from the section's centroid (in Unity (d, X) terms): flip when the face points at it
        mid_d = (section[k][0] + section[j][0]) / 2 - cd
        mid_x = (section[k][1] + section[j][1]) / 2 - cx
        # the face normal in Unity X is -Blender x; in d it is the outward direction of this member
        nx = -f.normal.x
        if which == "zlo":
            nd = f.normal.y        # outward d = -Z = +Blender y
        elif which == "zhi":
            nd = -f.normal.y       # outward d = +Z = -Blender y
        elif which == "ylo":
            nd = -f.normal.z       # outward d = -Y = -Blender z
        else:
            nd = f.normal.z
        if nx * mid_x + nd * mid_d < 0:
            f.normal_flip()
    return dc._part(kit, bm, slot, name, grain)


def _face(bm, vs, sign):
    f = bm.faces.new(vs)
    f.normal_update()
    # facing +X * sign (outward): Blender's -X is Unity +X
    if (f.normal.x < 0) != (sign > 0):
        f.normal_flip()
    return f


def ring(kit, sign, x, slot, name, rect=None):
    """LOD2: a flat mitred ring on one face (4 trapezoids, 8 triangles), d 0..19 mm at X = sign * x."""
    import bmesh
    z0, z1, y0, y1 = rect if rect is not None else (Z0, Z1, Y0, Y1)
    bm = bmesh.new()
    X = sign * x
    inner = [(y0, z0), (y0, z1), (y1, z1), (y1, z0)]
    f = FRAME_FACE
    outer = [(y0 - f, z0 - f), (y0 - f, z1 + f), (y1 + f, z1 + f), (y1 + f, z0 - f)]
    vi = [bm.verts.new(dc.U(X, y, z)) for y, z in inner]
    vo = [bm.verts.new(dc.U(X, y, z)) for y, z in outer]
    for k in range(4):
        j = (k + 1) % 4
        _face(bm, (vi[k], vi[j], vo[j], vo[k]), sign)
    return dc._part(kit, bm, slot, name)


def cutter(rect=None):
    """The leaf cut-out (the glass blank), through the slab."""
    z0, z1, y0, y1 = rect if rect is not None else (Z0, Z1, Y0, Y1)
    return dc.cutter((-0.03, y0 - POCKET, z0 - POCKET), (0.03, y1 + POCKET, z1 + POCKET))


# ------------------------------------------------------------------------------------------------------ builders
def _slot(frame_slot, sign):
    """A frame slot, or {+1: face S, -1: face P} (the EXIT kit wears the leaf's two finishes)."""
    return frame_slot[sign] if isinstance(frame_slot, dict) else frame_slot


def _rect_frame(kit, level, rect, frame_slot, kind, prefix):
    lv = str(level)
    sections = WOOD_SECTIONS if kind == "wood" else STEEL_SECTIONS
    for sign, tag in ((1, "s"), (-1, "p")):
        sec = _side(sections[level], sign)
        if kind == "wood":
            for which, grain in (("zlo", "z"), ("zhi", "z"), ("ylo", "y"), ("yhi", "y")):
                p = mitred_side(kit, sec, which, _slot(frame_slot, sign), "%s bead %s %s" % (prefix, tag, which), grain=grain, rect=rect)
                da.tag_lods(p, lv)
        else:
            import interact_door_leaf_steel_lite as sl
            z0, z1, y0, y1 = rect
            p = sl.rect_sweep_yz(kit, sec, z0, z1, y0, y1, _slot(frame_slot, sign), "%s kit %s" % (prefix, tag))
            da.tag_lods(p, lv)


def _rect_screws(kit, rect, rng, screw_slot, prefix):
    """Six slotted oval-head screws on face S of a steel kit, 12 mm outside the visible edge: one a rail, two a stile (at
    0.15 m from the ends; on a lite under 0.45 m tall at a quarter of its height). F5 takes six too (Red's set A, not 8)."""
    z0, z1, y0, y1 = rect
    h = y1 - y0
    z = (z0 + z1) / 2
    ys = [y0 + 0.15, y1 - 0.15] if h > 0.45 else [y0 + h / 4, y1 - h / 4]
    pts = [(y0 - 0.012, z), (y1 + 0.012, z)] + [(y, z0 - 0.012) for y in ys] + [(y, z1 + 0.012) for y in ys]
    for y, zz in pts:
        s = dc.oval_head(kit, (0.0245, y, zz), (1, 0, 0), screw_slot, rng, d=0.0055, half_segs=5, dome_k=0.16, name="%s screw" % prefix)
        da.tag_lods(s, "0")
    return len(pts)


def _rect_lite(kit, level, cuts, rect, frame_slot, kind, rng, screw_slot):
    cuts.append(cutter(rect))
    z0, z1, y0, y1 = rect
    g = POCKET - GLASS_GAP
    if level == 0:
        gl = dc.obox(kit, GLASS_SLOT, lo=(-GLASS_T / 2, y0 - g, z0 - g), hi=(GLASS_T / 2, y1 + g, z1 + g), name="lite glass")
        da.tag_lods(gl, "01")
    elif level == 2:
        # LOD2: one quad per face (the edges are under the ring)
        import bmesh
        for sign in (1, -1):
            bm = bmesh.new()
            X = sign * GLASS_T / 2
            vs = [bm.verts.new(dc.U(X, y, z)) for y, z in ((y0 - g, z0 - g), (y0 - g, z1 + g), (y1 + g, z1 + g), (y1 + g, z0 - g))]
            _face(bm, vs, sign)
            q = dc._part(kit, bm, GLASS_SLOT, "lite glass lod2 %s" % ("s" if sign > 0 else "p"))
            da.tag_lods(q, "2")
    if level < 2:
        _rect_frame(kit, level, rect, frame_slot, kind, "lite")
        if kind == "steel" and level == 0 and rng is not None:
            kit.meta["liteScrews"] = kit.meta.get("liteScrews", 0) + _rect_screws(kit, rect, rng, screw_slot, "lite")
    else:
        for sign, tag in ((1, "s"), (-1, "p")):
            p = ring(kit, sign, RING_X[kind], _slot(frame_slot, sign), "lite ring lod2 " + tag, rect=rect)
            da.tag_lods(p, "2")


def add_lite(kit, level, cuts, frame_slot, kind, rng=None, screw_slot=None, form=None):
    """The form's lite for one LOD level through a DA leaf's ``extra`` hook. kind "wood" (mitred bead in the leaf's wood,
    grain along each member) or "steel" (the pressed lite kit, painted with the leaf; slotted oval-head screws on face S at
    LOD0). Every level gets the cut-out."""
    form = form or DEFAULT
    _rect_lite(kit, level, cuts, form.rect, frame_slot, kind, rng, screw_slot)


def lite_meta(kit, kind, form=None):
    """Sidecar: anchors lite_centre / lite_dir, liteRect, liteGlass, liteForm and the EXAMINE block of kind peek (30_design
    4.2 as amended 2026-10-30: reach 1.0 m on the glass, C36 no threat gate, A43 runs live)."""
    form = form or DEFAULT
    z0, z1, y0, y1 = form.rect
    zc, yc, w, h = (z0 + z1) / 2, (y0 + y1) / 2, z1 - z0, y1 - y0
    dc.anchor(kit, "lite_centre", 0.0, yc, zc)
    dc.anchor(kit, "lite_dir", 0.10, yc, zc)
    kit.meta["liteRect"] = [z0, z1, y0, y1]
    kit.meta["liteGlass"] = [w + 2 * POCKET, h + 2 * POCKET, GLASS_T]
    kit.meta["liteFrame"] = kind
    kit.meta["liteForm"] = form.key
    kit.meta["liteAreaSqIn"] = round(form.area_sq_in, 1)
    kit.meta["liteNote"] = ("door vision lite (research/door_vision/30_design 1, 60_landing): 1/4 in wired glass, diamond mesh, Glass_Wired; "
                            "the map's leaf collider stays solid (the Relay's sight stops at it, RelaySeesThroughLites false); the player's "
                            "side of sight passes inside liteRect inset 10 mm (FrontRoomsDoorLite.PassesPlayerSight); never a window (A42)")
    inset = 0.010
    kit.meta["inspect"] = {
        "kind": "peek", "verb": "look", "face": "lite_centre", "faceDir": "lite_dir",
        "size": [w, h], "reach": PEEK_REACH, "approachAngle": 35, "aimPad": 0.001,
        "box": {"c": [0.0, yc, zc], "s": [2 * da.LEAF_X + 2 * inset, h - 2 * inset, w - 2 * inset]},
        "sound": "glass", "ship": True, "pause": "live", "noThreatGate": True,
    }
    kit.tag("lite", "vision_lite")


def assert_lite(kit, kind, form=None):
    """30_design 1.5: the frame clear of the door-break window and the damage mask, the frame closing on the cut-out, the
    glass inside its pocket, the sign clear of the frame and the leaf top (the last three not on the EXIT kit's leaf)."""
    form = form or DEFAULT
    z0, z1, y0, y1 = form.rect
    frames = [o for o in kit.parts if any(k in o.name for k in ("lite bead", "lite kit", "lite ring"))]
    lo, hi = dc.bounds_unity(frames)
    assert hi[2] <= z1 + FRAME_FACE + 1e-4, "frame outer Z %.4f" % hi[2]
    if form.break_safe:
        assert BREAK_WINDOW_Z - hi[2] >= 0.050 - 1e-4, "frame %.4f m from the break window" % (BREAK_WINDOW_Z - hi[2])
        assert DAMAGE_MASK_Z - hi[2] >= 0.045 - 1e-4, "frame %.4f m from the damage mask" % (DAMAGE_MASK_Z - hi[2])
    assert lo[1] >= y0 - FRAME_FACE - 1e-4 and hi[1] <= y1 + FRAME_FACE + 1e-4, "frame Y %s %s" % (lo[1], hi[1])
    glass = [o for o in kit.parts if o.name == "lite glass" or o.name.startswith("lite glass.")]
    clear = 1.0
    for gobj in glass:
        glo, ghi = dc.bounds_unity([gobj])
        clear = min(clear, glo[2] - (z0 - POCKET), (z1 + POCKET) - ghi[2], glo[1] - (y0 - POCKET), (y1 + POCKET) - ghi[1])
        assert abs((ghi[0] - glo[0]) - GLASS_T) < 1e-5, "glass thickness %.5f" % (ghi[0] - glo[0])
    assert clear >= 0.0005 - 1e-6, "glass pocket clearance %.4f" % clear
    # the frame's leg in the cut-out reaches the cut edge (closure <= 0.1 mm)
    secs = WOOD_SECTIONS if kind == "wood" else STEEL_SECTIONS
    for lv, sec in secs.items():
        dmax = max(d for d, x in sec)
        assert abs(dmax - FRAME_FACE) < 1e-6 and any(abs(d - POCKET) < 1e-4 for d, x in sec), "section %d closure" % lv
    out = {"form": form.key, "frameZ": [round(lo[2], 4), round(hi[2], 4)], "frameY": [round(lo[1], 4), round(hi[1], 4)],
           "frameX": [round(lo[0], 4), round(hi[0], 4)], "glassClearance": round(clear, 5)}
    if form.break_safe:
        sign_lo, sign_hi = SIGN_Y - SIGN_H / 2, SIGN_Y + SIGN_H / 2
        assert sign_lo - hi[1] >= 0.040 - 1e-6, "sign %.4f above the frame" % (sign_lo - hi[1])
        assert da.LEAF_Y1 - sign_hi >= 0.040 - 1e-3, "sign %.4f under the leaf top" % (da.LEAF_Y1 - sign_hi)
        out.update(toBreakWindow=round(BREAK_WINDOW_Z - hi[2], 4), toDamageMask=round(DAMAGE_MASK_Z - hi[2], 4),
                   signAboveFrame=round(sign_lo - hi[1], 4), signUnderTop=round(da.LEAF_Y1 - sign_hi, 4))
    return out
