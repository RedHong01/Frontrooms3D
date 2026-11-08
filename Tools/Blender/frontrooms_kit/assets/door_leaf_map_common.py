"""Zone doors, the Map stage (2026-11-06; research/double_doors/30_build.md Part M): the shared numbers and builders of the D1.6
map leaves for the members the zone doors run adds (Red 2026-11-05 21:26:27, D-2 option 2: a new back-of-house door, painted steel with a kick plate
and a louvre, verbatim in handoff/visual.md section 5 0x; the pool family, D-5 at its recommended PD-A). NOT AN ASSET: no NAME and no build(); the leaf modules call it.

The map leaf keeps the D1.6 DA envelope (interact_door_da_common: X +-0.022, Y 0.015..2.095, the hinge stile turned about the
visual pivot, the flat latch end at Z 0.995) so every lock, sign, plate and kick anchor of the D1.6 rows stays seated, the steel
break cassettes fit, and the map's collider is unchanged. Door root D: X through (+X face S), Y up, Z along the opening.

  Back-of-house (BH-F / BH-K; 10_research section B.5): the painted hollow-metal leaf of Kit_DoorLeaf_Steel_DA (16 ga faces, the
  satin-stainless kick plates both faces, the channel lines, the pressed F1 lite painted with the leaf) plus the A2 louvre's cut-out
  on the F1 axis at 13-29 in (Z 0.400, Y 0.5334: the landed Kit_DoorLouvre_F12, spawned into it by the row at a through-scale of
  LEAF_X / 0.02225 so its flange seats 0.1 mm into the 44 mm map face). Free: the bored latch's faceplate at the lever (Y 1.000,
  satin chrome) and the rose anchors; locked: Steel_DA's mortise armor front and the key-door anchors.
  Pool (PL-F / PL-K; 20_design section B10): a moulded FRP leaf (the U-collar and the two skins as one gel-coat shell: r 3 mm
  arrises, a parting seam on the latch end), the A1 16 in porthole on the hinge side (centre Z 0.324, Y 1.397: the ring's foot ends
  at Z 0.5491, under the break window's 0.5495 clearance) with a 1/4 in clear tempered pane (Glass_Tempered) in the leaf; the moulded
  ring is the landed Kit_DoorLite_A1_FRP, spawned by the row at the same through-scale. Free: a 630 bored-latch faceplate; locked: a
  630 mortise armor front. No in-mesh kick plates (the rows hang Kit_DoorKickPlate_Map, HW-DA1's 10 in 630 plate).

Mesh UVs (kitlib metres): face S u = +Z, face P u = -Z, v = Y on every leaf here (probed on the landed leaves 2026-11-06), so the
leaf-wear masks of the map members are imported with wrapU Mirror: one material serves both faces with the latch edge at u 1.
"""

import math
import random

import bmesh
import kitlib
from mathutils import Vector

import interact_door_common as dc
import interact_door_da_common as da
import interact_door_lite_common as lite
import interact_door_leaf_steel_da as steel
import interact_door_leaf_veneer_da as veneer

# ------------------------------------------------------------------ the louvre (A2: door_vision 50; the landed Kit_DoorLouvre_F12)
LOUVRE_Z = lite.LITE_Z                     # 0.400: on the F1 axis (DA-L1), the hinge side of the break window
LOUVRE_Y = 0.5334                          # Kit_DoorLouvre_F12 louvreCentreOnLeaf: 10 x 16 in visible at 13-29 in
LOUVRE_HALF = (0.127, 0.2032)              # visible half (z, y)
LOUVRE_CUT = (0.1365, 0.2127)              # the F12 kit's cutRect half (visible + 9.5 mm)
LOUVRE_FRAME = 0.019                       # the pressed frame's sight face
# ------------------------------------------------------------------ the porthole (A1: door_vision 50 section 2.3; Kit_DoorLite_A1_FRP)
PORT_Z, PORT_Y = 0.324, 1.397              # centre: 55 in up (clear bottom 47 in <= 48), hinge side
PORT_CLEAR, PORT_CUT, PORT_GLASS, PORT_OUTER = 0.2032, 0.2127, 0.2117, 0.2251
GLASS_HALF_T = 0.003                       # 1/4 in tempered, 6.0 mm (the pair's pane)
PORT_SEGS = {0: 64, 1: 32, 2: 24}          # the cut-out and the pane (the ring kit: 64 / 32 / 24)
# ------------------------------------------------------------------ the pair kits' through-scale on the map leaf
PAIR_LEAF_HALF_T = 0.02225
KIT_Z_SCALE = da.LEAF_X / PAIR_LEAF_HALF_T  # 0.98876: the ring's foot and the louvre's flange back 0.1 mm into the 44 mm face
# ------------------------------------------------------------------ the FRP shell
R_FRP = 0.003                              # the moulded arrises (PD-A's leaf: r 3 mm)
SEAM_W, SEAM_D = 0.0006, 0.0004            # the parting seam on the latch end (the mould's split line at mid-thickness)
FRP_RINGS = (0.080, 0.300, 0.800, 0.950, 1.150, 1.300, 1.700, 2.000)   # extra rings: vertex-colour wear resolution

CHROME = "Prop_Chrome"
STAINLESS = "Prop_Stainless630"
ALU = "Prop_Aluminium"
GELCOAT = "Door_PoolGelcoat_Map"
GELCOAT_AQUA = "Door_PoolGelcoat_MapAqua"
BOH = "Door_BackOfHouse_Map"
ENAMEL = "Door_Enamel"
TEMPERED = "Glass_Tempered"
# preview colours only (Unity maps the slots to Resources/Surfaces by name)
kitlib.register_slot(BOH, (0.262, 0.250, 0.214), 0.5, 0.0)            # B-2a warm grey, sRGB about (140, 135, 124)
kitlib.register_slot(ENAMEL, (0.610, 0.558, 0.434), 0.5, 0.0)
kitlib.register_slot(GELCOAT, (0.791, 0.791, 0.745), 0.22, 0.0)       # the gel coat, sRGB (230, 230, 224)
kitlib.register_slot(GELCOAT_AQUA, (0.054, 0.342, 0.366), 0.22, 0.0)  # the course aqua one step deeper (F1, sRGB 66 158 163)
kitlib.register_slot(STAINLESS, (0.64, 0.63, 0.60), 0.35, 0.9)
kitlib.register_slot(TEMPERED, (0.80, 0.85, 0.83), 0.05, 0.0)
kitlib.register_slot(lite.GLASS_SLOT, (0.80, 0.85, 0.83), 0.05, 0.0)


# ================================================================== shared parts
def latch_prep(kit, level, cuts, hw_slot, locked, rng):
    """The latch end's hardware: locked = Steel_DA's mortise armor front (8 in, two screws, the latch and deadbolt openings);
    free = Veneer_DA's bored-latch faceplate at the lever's height (Y 1.000, the DA latch opening, two screws). LOD1 a skin."""
    if locked:
        aw, ah, at, ay = steel.ARMOR
        if level == 0:
            armor, mort = da.edge_plate(kit, hw_slot, ay, aw, ah, at, "armor front", bevel=0.0004)
            cuts.append(mort)
            dc.cut(armor, [da.edge_opening(da.LATCH_Y_MORTISE, *steel.BOLT_OPENING), da.edge_opening(da.DEADBOLT_Y, *steel.BOLT_OPENING)])
            for dy in (-0.088, 0.088):
                sc = dc.flat_head(kit, Vector((0.0, ay + dy, da.LEAF_Z_LATCH)), (0, 0, 1), hw_slot, rng, d=0.0076, host=armor,
                                  notch=0.0012, name="armor screw")
                da.tag_lods(sc, "0")
            da.tag_lods(armor, "0")
        elif level == 1:
            armor = dc.obox(kit, hw_slot, lo=(-aw / 2, ay - ah / 2, da.LEAF_Z_LATCH - 0.0002), hi=(aw / 2, ay + ah / 2, da.LEAF_Z_LATCH + 0.0002),
                            name="armor front lod1")
            da.tag_lods(armor, "1")
        return
    fw, fh, ft = veneer.FACEPLATE
    if level == 0:
        plate, mort = da.edge_plate(kit, hw_slot, da.LATCH_Y_BORED, fw, fh, ft, "latch faceplate", bevel=0.0002)
        cuts.append(mort)
        dc.cut(plate, [da.edge_opening(da.LATCH_Y_BORED, *veneer.LATCH_OPENING)])
        for dy in (-0.0215, 0.0215):
            c = Vector((0.0, da.LATCH_Y_BORED + dy, da.LEAF_Z_LATCH))
            sc = dc.flat_head(kit, c, (0, 0, 1), hw_slot, rng, d=0.0076, host=plate, notch=0.0009, name="faceplate screw")
            da.tag_lods(sc, "0")
        da.tag_lods(plate, "0")
    elif level == 1:
        plate = dc.obox(kit, hw_slot, lo=(-fw / 2, da.LATCH_Y_BORED - fh / 2, da.LEAF_Z_LATCH - 0.0002),
                        hi=(fw / 2, da.LATCH_Y_BORED + fh / 2, da.LEAF_Z_LATCH + 0.0002), name="latch faceplate lod1")
        da.tag_lods(plate, "1")


def kick_plates(kit, level, rng):
    """Steel_DA's .050 satin-stainless kick plates on both faces (Y 0.018-0.272, Z 0.030-0.970, 1.27 mm proud), every level."""
    for side in (1, -1):
        z0, z1 = steel.KICK_Z
        x0, x1 = side * da.LEAF_X, side * (da.LEAF_X + steel.KICK_T)
        kp = dc.obox(kit, ALU, lo=(min(x0, x1), steel.KICK_Y[0], z0), hi=(max(x0, x1), steel.KICK_Y[1], z1),
                     bevel=(0.0009 if level < 2 else 0.0), segs=1, name="kick plate %s" % ("s" if side > 0 else "p"))
        da.tag_lods(kp, str(level))
        if level == 0:
            for z in (z0 + 0.015, (z0 + z1) / 2, z1 - 0.015):
                for y in (steel.KICK_Y[0] + 0.015, steel.KICK_Y[1] - 0.015):
                    sc = dc.oval_head(kit, (x1, y, z), (side, 0, 0), ALU, rng, d=0.0068, name="kick screw")
                    da.tag_lods(sc, "0")


def louvre_cutter():
    """The A2 louvre's cut-out (the F12 kit's cutRect) through the slab, in every level: the kit's frame legs and blades close it."""
    hz, hy = LOUVRE_CUT
    return dc.cutter((-da.LEAF_X - 0.002, LOUVRE_Y - hy, LOUVRE_Z - hz), (da.LEAF_X + 0.002, LOUVRE_Y + hy, LOUVRE_Z + hz))


def disc_cutter(radius, segs, half_x):
    """A segs-gon prism along X through the porthole's centre (a boolean cutter, not part of the asset)."""
    bm = bmesh.new()
    a = [bm.verts.new(dc.U(-half_x, PORT_Y + radius * math.sin(2 * math.pi * i / segs), PORT_Z + radius * math.cos(2 * math.pi * i / segs)))
         for i in range(segs)]
    b = [bm.verts.new(dc.U(half_x, PORT_Y + radius * math.sin(2 * math.pi * i / segs), PORT_Z + radius * math.cos(2 * math.pi * i / segs)))
         for i in range(segs)]
    for i in range(segs):
        j = (i + 1) % segs
        bm.faces.new((a[i], a[j], b[j], b[i]))
    bm.faces.new(a)
    bm.faces.new(list(reversed(b)))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    import bpy
    me = bpy.data.meshes.new("cutter")
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new("cutter", me)
    dc._cutter_collection().objects.link(ob)
    ob.hide_render = True
    dc._CUTTERS.append(ob)
    return ob


def porthole_glass(kit, level):
    """The 1/4 in clear tempered pane (Glass_Tempered), 1 mm short of the cut-out: a disc of the level's segments, its two faces
    and its edge (the edge shows only through the cut-out's 1 mm)."""
    segs = PORT_SEGS[level]
    r, t = PORT_GLASS, GLASS_HALF_T
    g = dc.revolve(kit, [(0.0, -t), (r, -t), (r, t), (0.0, t)], (0.0, PORT_Y, PORT_Z), (1, 0, 0), segs, TEMPERED,
                   "lite glass" if level == 0 else "lite glass lod%d" % level)
    da.tag_lods(g, str(level))
    return g


def porthole_meta(kit):
    z0, z1, y0, y1 = PORT_Z - PORT_CLEAR, PORT_Z + PORT_CLEAR, PORT_Y - PORT_CLEAR, PORT_Y + PORT_CLEAR
    dc.anchor(kit, "lite_centre", 0.0, PORT_Y, PORT_Z)
    dc.anchor(kit, "lite_dir", 0.10, PORT_Y, PORT_Z)
    kit.meta["liteRect"] = [z0, z1, y0, y1]
    kit.meta["liteShape"] = "round"
    kit.meta["liteGlass"] = [2 * PORT_GLASS, 2 * PORT_GLASS, 2 * GLASS_HALF_T]
    kit.meta["liteFrame"] = "frp ring (Kit_DoorLite_A1_FRP, spawned by the row)"
    kit.meta["liteForm"] = "A1"
    kit.meta["liteAreaSqIn"] = round(math.pi * PORT_CLEAR * PORT_CLEAR / (0.0254 * 0.0254), 1)
    kit.meta["liteNote"] = ("the A1 16 in porthole (door_vision 50 section 2.3), 1/4 in clear tempered glass (Glass_Tempered: FrontRoomsDoorLite "
                            "Glazing Tempered, Shape Round from the row); the player's sight passes the circle inscribed in liteRect inset 10 mm; "
                            "the map's leaf collider stays solid")
    inset = 0.010
    w = 2 * PORT_CLEAR
    kit.meta["inspect"] = {
        "kind": "peek", "verb": "look", "face": "lite_centre", "faceDir": "lite_dir",
        "size": [w, w], "reach": lite.PEEK_REACH, "approachAngle": 35, "aimPad": 0.001,
        "box": {"c": [0.0, PORT_Y, PORT_Z], "s": [2 * da.LEAF_X + 2 * inset, w - 2 * inset, w - 2 * inset]},
        "sound": "glass", "ship": True, "pause": "live", "noThreatGate": True,
    }
    kit.meta["portholeCentre"] = [PORT_Z, PORT_Y]
    kit.meta["portholeCutRadius"] = PORT_CUT
    kit.meta["ringKit"] = "Kit_DoorLite_A1_FRP"
    kit.meta["ringOuterReachZ"] = round(PORT_Z + PORT_OUTER, 4)
    kit.tag("lite", "vision_lite")


def louvre_meta(kit):
    kit.meta["louvreCentre"] = [LOUVRE_Z, LOUVRE_Y]
    kit.meta["louvreRect"] = [LOUVRE_Z - LOUVRE_HALF[0], LOUVRE_Z + LOUVRE_HALF[0], LOUVRE_Y - LOUVRE_HALF[1], LOUVRE_Y + LOUVRE_HALF[1]]
    kit.meta["louvreCut"] = [LOUVRE_Z - LOUVRE_CUT[0], LOUVRE_Z + LOUVRE_CUT[0], LOUVRE_Y - LOUVRE_CUT[1], LOUVRE_Y + LOUVRE_CUT[1]]
    kit.meta["louvreKit"] = "Kit_DoorLouvre_F12"
    kit.meta["kitZScale"] = round(KIT_Z_SCALE, 5)
    kit.tag("louvre", "back_of_house")


# ================================================================== the back-of-house leaf
def build_louvre_leaf(kit, face_slot, locked, seed):
    """The painted hollow-metal DA leaf with the F1 lite and the A2 louvre's cut-out, every LOD level."""
    rng = random.Random(seed)
    lrng = random.Random(seed + 5)
    slabs = []
    for level in (0, 1, 2):
        lv = str(level)
        s = da.slab(kit, face_slot, level, seams=(0.0,), extra_rings=steel.channel_rings, grain=None)
        da.tag_lods(s, lv)
        slabs.append(s)
        cuts = []
        kick_plates(kit, level, rng)
        if level == 0:
            cuts += da.pivot_plate_cutters()
            da.pivot_plates(kit, CHROME, 0, rng)
        latch_prep(kit, level, cuts, CHROME, locked, rng)
        lite.add_lite(kit, level, cuts, face_slot, "steel", lrng, CHROME)
        cuts.append(louvre_cutter())
        dc.cut(s, cuts)
    dc.finalize(kit, 35.0, steel.wear, protect=slabs)
    da.assert_leaf(kit, slabs[0], kit.name)
    # the louvre's frame (19 mm sight face round the visible 10 x 16) clear of the break window, the damage mask, the kick plates
    fz1 = LOUVRE_Z + LOUVRE_HALF[0] + LOUVRE_FRAME
    fy0, fy1 = LOUVRE_Y - LOUVRE_HALF[1] - LOUVRE_FRAME, LOUVRE_Y + LOUVRE_HALF[1] + LOUVRE_FRAME
    assert fz1 <= 0.5495 + 1e-6, "louvre frame reaches Z %.4f (break window clearance 0.5495)" % fz1
    assert fy1 <= lite.DAMAGE_MASK_Y[0] - 0.040, "louvre frame top %.4f under the damage mask %.3f" % (fy1, lite.DAMAGE_MASK_Y[0])
    assert fy0 - steel.KICK_Y[1] >= 0.035, "louvre frame %.4f m over the kick plate" % (fy0 - steel.KICK_Y[1])
    assert LOUVRE_Y + LOUVRE_CUT[1] < lite.Y0 - lite.POCKET - 0.30, "louvre cut-out clear of the lite's"
    return slabs


def louvre_leaf_meta(kit, module, locked):
    lite.lite_meta(kit, "steel")
    kit.meta["liteAsserts"] = lite.assert_lite(kit, "steel")
    louvre_meta(kit)
    da.leaf_anchors(kit, locked=locked, sign_yz=(lite.SIGN_Y, lite.SIGN_Z))
    kit.no_collider()
    kit.tag("interactable", "door", "door_leaf", "back_of_house", "door_type_hollow_metal", "double_acting", "map_member")
    if locked:
        kit.tag("locked")
    kit.meta["doorType"] = "hollow_metal"
    da.lod_meta(kit, module)


# ================================================================== the pool leaf
def frp_section(inset, level):
    return da.leaf_section(inset, level, r_l=R_FRP, seams=(0.0,), seam_w=SEAM_W, seam_d=SEAM_D)


def frp_slab(kit, slot, level, name="slab"):
    """The moulded FRP shell for one level: loft_y over the r 3 mm top and bottom arris rings (4 / 3 / 2 rings) and the wear rings."""
    A = R_FRP
    phis = (0.0, 30.0, 60.0, 90.0) if level == 0 else ((0.0, 45.0, 90.0) if level == 1 else (0.0, 90.0))

    def arris(y_edge, sign):
        out = []
        for phi in phis:
            o = A - A * math.sin(math.radians(phi))
            h = A - A * math.cos(math.radians(phi))
            out.append((y_edge + sign * h, frp_section(o, level)))
        return out
    rings = arris(da.LEAF_Y0, 1)
    if level < 2:
        rings += [(y, frp_section(0.0, level)) for y in FRP_RINGS]
    rings += list(reversed(arris(da.LEAF_Y1, -1)))
    return dc.loft_y(kit, rings, slot, name, grain=None)


def build_frp_leaf(kit, face_slot, locked, seed):
    """The moulded FRP DA leaf with the A1 porthole's cut-out and its tempered pane, every LOD level."""
    rng = random.Random(seed)
    slabs = []
    for level in (0, 1, 2):
        s = frp_slab(kit, face_slot, level)
        da.tag_lods(s, str(level))
        slabs.append(s)
        cuts = [disc_cutter(PORT_CUT, PORT_SEGS[level], da.LEAF_X + 0.004)]
        porthole_glass(kit, level)
        if level == 0:
            cuts += da.pivot_plate_cutters()
            da.pivot_plates(kit, STAINLESS, 0, rng)
        latch_prep(kit, level, cuts, STAINLESS, locked, rng)
        dc.cut(s, cuts)
    dc.finalize(kit, 35.0, da.wear_leaf, protect=slabs)
    da.assert_leaf(kit, slabs[0], kit.name)
    assert PORT_Z + PORT_OUTER <= 0.5495 + 1e-6, "the ring reaches Z %.4f (break window clearance 0.5495)" % (PORT_Z + PORT_OUTER)
    assert PORT_Z - PORT_OUTER >= da.STILE_Z_FACE + 0.08, "the ring clear of the hinge stile"
    assert PORT_Y - PORT_CLEAR <= 48 * 0.0254 + 1e-6 and math.pi * PORT_CLEAR ** 2 / 0.0254 ** 2 >= 200.0, "A1: >= 200 sq in, bottom <= 48 in"
    assert PORT_Y + PORT_OUTER <= lite.SIGN_Y - lite.SIGN_H / 2 - 0.30, "the ring clear of the top-rail sign"
    assert PORT_Y - PORT_OUTER >= steel.KICK_Y[1] + 0.80, "the ring clear of the kick plate"
    # the pane sits in the cut-out, 1 mm short of it, inside the faces
    lo, hi = dc.bounds_unity([o for o in kit.parts if o.name == "lite glass"])
    assert abs(hi[0] - GLASS_HALF_T) < 1e-5 and abs(lo[0] + GLASS_HALF_T) < 1e-5, "pane thickness %s %s" % (lo, hi)
    assert hi[2] <= PORT_Z + PORT_GLASS + 1e-5 and lo[2] >= PORT_Z - PORT_GLASS - 1e-5, "pane radius"
    return slabs


def frp_leaf_meta(kit, module, locked):
    porthole_meta(kit)
    da.leaf_anchors(kit, locked=locked, sign_yz=(lite.SIGN_Y, lite.SIGN_Z))
    kit.no_collider()
    kit.tag("interactable", "door", "door_leaf", "pool", "door_type_frp", "double_acting", "map_member")
    if locked:
        kit.tag("locked")
    kit.meta["doorType"] = "frp"
    kit.meta["frpArris"] = R_FRP
    da.lod_meta(kit, module)
