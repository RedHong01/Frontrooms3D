"""Shared helpers for the stairwell's door kits (stairwell doors, zone doors run, 2026-11-06; Documentation/research/
double_doors/30_build.md Part S; inventory research/door_inventory/10_inventory.md rows D1 and D3).

NOT AN ASSET: no NAME and no build(). The asset modules import it (``import door_stair_common as sc``):
door_leaf_stair (Kit_DoorLeaf_Stair), door_leaf_stair_wide (Kit_DoorLeaf_StairWide), door_leaf_stair_north
(Kit_DoorLeaf_StairNorth), door_frame_stair_exit (Kit_DoorFrame_StairExit), door_frame_stair_south
(Kit_DoorFrame_StairSouth), door_frame_stair_north (Kit_DoorFrame_StairNorth). add_lod2() also gives the landed
Kit_ExitDevice_Crossbar and Kit_DoorCloser_* their LOD2 (P-1) without touching their LOD0 / LOD1.

Frame: the kits' DOOR ROOT (interact_door_common, 10_spec 1.2), Unity metres: origin the opening's hinge-jamb edge on the
leaf's centre plane at the floor; +Z along the opening (0 the hinge jamb, W the latch jamb); +Y up; +X the pull face S (the
side the leaf swings toward, where the hinge knuckles are); -X the push face P (where a stop is). FrontRoomsStairwellDresser
spawns them under roots scaled (-push, 1, 1), as it does the closer kits: the map's hinges have +X on the push face.

Why the leaf keeps the map's 50 mm (FrontRooms ModuleUnits.DoorLeafThickness): every stairwell and A13 anchor is seated on
the cube leaf's faces at +-0.025: the crossbar's back (MapWorld.MountExitCrossbar), the closer body's mount, the plates
(PlateOut 1 mm), the kick plate, the knuckles (face + 8 mm), the F13 lite kit (scaled 25 / 22 to 50 mm), the chain fall's
leaf contact (FrontRoomsExitChainFall LeafX .025). A 2 in heavy-duty hollow-metal leaf; a 1-3/4 in one would leave every one
of them 2.75 mm off its face. The map hinges on the leaf's centre plane (no knuckle axis), so the envelope, the 4 mm gaps and
the swing clearances below are checked against that axis.

Period sources (cited, never re-researched): research/stairwell/01_research.md (1-3/4 in hollow-metal leaf, welded
hollow-metal frame, three ball-bearing hinges, a fire label on the hinge edge of the leaf and of the frame), door_frame/
10_period_frames.md 1.2 (SDI 111A: 2 in face, 5/8 in stop, 1-15/16 in door rabbet, 1-9/16 in second rabbet, 7/16 in
return: the map's EXIT trim 50.8 x 11.1 mm is exactly that frame's face and return; 16 ga, welded, faces mitred and ground),
interactables/10_spec.md (silencers on the latch stop, the leaf's edge seam, the 3 deg lock-edge bevel).
"""

import math

import bmesh
import bpy
from mathutils import Vector

import interact_door_common as dc

U = dc.U

# ---------------------------------------------------------------- the map's stair openings (FrontRoomsModuleUnits, MapWorld,
# FrontRoomsStairwell): a 1.0 x 2.1 m map door; the shell's south doorway is 1.12 wide (LeafRoom 0.06 a side for the EXIT
# leaf's 95 deg swing); every leaf 0.05 thick on its hinge's centre plane.
HEAD = 2.1
T = 0.05
HT = T / 2.0                               # 0.025: the leaf's faces
EXIT_W, SOUTH_W, NORTH_W = 1.0, 1.12, 1.0

# ---------------------------------------------------------------- the leaf (hollow metal, 1990)
GAP_JAMB = 0.004                           # 5/32 in at hinge and latch (the map's cube stops 10 mm short: render-only)
GAP_HEAD = 0.0032                          # 1/8 in
UNDERCUT = 0.008                           # 5/16 in over the floor (no saddle in a stairwell)
LEAF_Y0 = UNDERCUT
LEAF_Y1 = HEAD - GAP_HEAD                  # 2.0968
BEVEL_DEG = 3.0                            # the lock edge's 1/8 in in 2 in bevel: the push face P is the short one
ARRIS_R = 0.0016                           # the formed edge's 1/16 in outside radius
SEAM_W, SEAM_D = 0.0007, 0.0005            # the visible edge seam (a mechanical interlock, ground but not filled)
HINGE_YC = (0.29, 1.07, 1.86)              # the dresser's three 4-1/2 in knuckles (FrontRoomsStairwellDresser.Knuckles)
HINGE_H = 0.114
KNUCKLE_X, KNUCKLE_Z, KNUCKLE_R = HT + 0.008, -0.002, 0.0105
LABEL_YC, LABEL_H, LABEL_W = 1.465, 0.064, 0.032   # the fire label: 2-1/2 x 1-1/4 in, on the hinge edge, painted over
LABEL_PROUD = 0.0005

# the EXIT's F13 lite (Kit_DoorLite_Exit sidecar liteRect: visible z0, z1, y0, y1 in the door root) and its pocket
LITE = (0.679, 0.7806, 1.27, 1.905)
LITE_POCKET = 0.0095                       # FrontRoomsDoubleDoorKit.LitePocket
HOLE_MARGIN = 0.0005                       # the slab's cut 0.5 mm behind the kit's pocket legs: no shared plane

# the north door's lever trim (FrontRoomsStairwellDresser: Kit_Lock_Rose at (face, 0.968, 0.92)): a bored latch's faceplate
LATCH_YC = 0.968
FACEPLATE_W, FACEPLATE_H, FACEPLATE_T = 0.0254, 0.0572, 0.0016

# ---------------------------------------------------------------- the frames (16 ga pressed steel, F4)
SHEET_T = dc.SHEET_T                       # 0.0015
CONVEX = dc.BEND_RI + SHEET_T              # 0.003 outer radius of a convex bend
CONCAVE = dc.BEND_RI                       # 0.0015
FACE = 0.0508                              # 2 in face
EXIT_FACE_X = 0.0911                       # the map's EXIT trim: wall 0.16 / 2 + 7/16 in
WALL_X = 0.08
FOOT_IN = 0.0005                           # every return foot 0.5 mm into its wall face (no slit)
STOP_H = 0.0159                            # 5/8 in
SECOND_RABBET = 0.0397                     # 1-9/16 in, on P
STOP_S_X = -(HT + 0.002)                   # the stop's S face 2 mm off the leaf's P face (-0.027); silencers bridge it
STOP_P_X = -EXIT_FACE_X + SECOND_RABBET    # -0.0514
SILENCER_R, SILENCER_H = 0.005, 0.0022
SILENCER_Y = (0.35, 1.30, 1.90)
# the shell doorways (FrontRoomsStairwell.Doorway): a 0.06 wall on the hinge's centre plane, the box frame 0.07 wide (TrimFace)
# and 0.08 deep (flush with the outer face, 20 mm proud of the stair face); the wall stops at the box's outer edge
SHELL_HALF = 0.03
SHELL_HOLE = 0.07
SHELL_FACE_X = SHELL_HALF + 0.0111         # 7/16 in proud of the stair face, as F4
# the south doorway faces the map's EXIT frame: its X face is 18.9 mm beyond the shell's outer face (Inset 0.11 - 0.0911) and
# its outer edge 9.2 mm inside the shell's opening (LeafRoom 0.06 - FACE)
BRIDGE_X = SHELL_HALF + (0.11 - EXIT_FACE_X)        # 0.0489
BRIDGE_U = 0.06 - FACE                               # 0.0092

STRIKE_W, STRIKE_H, STRIKE_T, STRIKE_LIP = 0.032, 0.124, 0.0016, 0.012    # ANSI curved-lip strike (dc.STRIKE_BORED)

assert abs(STOP_P_X + 0.0514) < 1e-9
assert STOP_S_X < -HT - 1e-4


def tag(obj, levels):
    """kitlib P-1b: the LOD levels this part belongs to ("0", "01", "012", ...)."""
    obj["fr_lods"] = levels
    return obj


def latch_z(W, x):
    """The leaf's bevelled latch edge at x (S +HT is the long side: W - GAP_JAMB)."""
    zs = W - GAP_JAMB
    return zs - (HT - x) * math.tan(math.radians(BEVEL_DEG))


def _face(bm, verts, want_unity):
    f = bm.faces.new(verts)
    f.normal_update()
    w = Vector(U(*want_unity))
    if f.normal.dot(w) < 0:
        f.normal_flip()
    return f


# ---------------------------------------------------------------- leaf
def leaf_plan(W, inset, level):
    """Plan section (x, z) of the leaf, closed, counter-clockwise seen from +Y: P face (-x), hinge edge (z small), S face, the
    bevelled latch edge; arrises filleted (LOD0 2 segments, LOD1 1, LOD2 sharp); LOD0 has the edge seams at x 0."""
    xs = HT - inset
    zh = GAP_JAMB + inset
    r = max(ARRIS_R - inset, 6e-5)
    pts, radii = [(-xs, zh)], [r]
    if level == 0:
        pts += [(-SEAM_W / 2, zh), (0.0, zh + SEAM_D), (SEAM_W / 2, zh)]
        radii += [0, 0, 0]
    pts += [(xs, zh), (xs, latch_z(W, xs) - inset)]
    radii += [r, r]
    if level == 0:
        pts += [(SEAM_W / 2, latch_z(W, SEAM_W / 2) - inset), (0.0, latch_z(W, 0.0) - inset - SEAM_D),
                (-SEAM_W / 2, latch_z(W, -SEAM_W / 2) - inset)]
        radii += [0, 0, 0]
    pts += [(-xs, latch_z(W, -xs) - inset)]
    radii += [r]
    if level == 2:
        return pts
    return dc.fillet_polygon(pts, radii, 2 if level == 0 else 1, closed=True)


def leaf_rings(W, level):
    """(y, section) rings bottom to top: the bottom and top arrises rounded as the vertical ones (LOD0 3 rings an end, LOD1 2,
    LOD2 1)."""
    r = ARRIS_R
    if level == 0:
        steps = [(0.0, r), (r * (1 - math.cos(math.radians(45))), r * (1 - math.sin(math.radians(45)))), (r, 0.0)]
    elif level == 1:
        steps = [(0.0, r), (r, 0.0)]
    else:
        steps = [(0.0, 0.0)]
    rings = []
    for dy, o in steps:
        rings.append((LEAF_Y0 + dy, leaf_plan(W, o, level)))
    for dy, o in reversed(steps):
        rings.append((LEAF_Y1 - dy, leaf_plan(W, o, level)))
    return rings


def leaf_slab(kit, W, level, slot_p, slot_s, name):
    """The slab as one closed loft along Y (P face, S face, edges, top and bottom); returns the object. The faces whose outward
    normal points +X take slot_s, all others slot_p (the dresser maps P / S to each door's stair and room finishes)."""
    rings = leaf_rings(W, level)
    bm = bmesh.new()
    vr = [[bm.verts.new(U(x, y, z)) for x, z in sec] for y, sec in rings]
    n = len(vr[0])
    for a, b in zip(vr, vr[1:]):
        for i in range(n):
            j = (i + 1) % n
            bm.faces.new((a[i], a[j], b[j], b[i]))
    bm.faces.new(vr[0])
    bm.faces.new(list(reversed(vr[-1])))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    obj = dc._part(kit, bm, slot_p, name)
    obj.data.materials.append(kit._material(slot_s))
    # S slot (index 1) for every face that looks +X (Unity) = -x (Blender)
    for p in obj.data.polygons:
        if p.normal.x < -0.9:
            p.material_index = 1
    return obj


def lite_cutter(rect):
    z0, z1, y0, y1 = rect
    m = LITE_POCKET + HOLE_MARGIN
    return dc.cutter((-HT - 0.01, y0 - m, z0 - m), (HT + 0.01, y1 + m, z1 + m))


def hinge_plates(kit, slot, levels):
    """The leaf halves of the three butts, mortised flush in the hinge edge (0.3 mm proud), from the knuckle toward P."""
    out = []
    for yc in HINGE_YC:
        o = dc.obox(kit, slot, lo=(-0.008, yc - HINGE_H / 2, GAP_JAMB - 0.0003), hi=(KNUCKLE_X - KNUCKLE_R + 0.0004, yc + HINGE_H / 2, GAP_JAMB + 0.0025),
                    bevel=0.0003, segs=1, name="hinge leaf (leaf half)")
        out.append(tag(o, levels))
    return out


def fire_label(kit, slot, at_z, normal_sign, x0, x1, y_c, levels, rivets=True):
    """A fire label, painted over: a thin plate on a face at z = at_z whose outward normal is normal_sign * Z (the leaf's hinge
    edge looks -Z, the frame's hinge soffit +Z): 0.4 mm buried, LABEL_PROUD proud, two domed rivets (LOD0)."""
    z_in = at_z - normal_sign * 0.0004
    z_out = at_z + normal_sign * LABEL_PROUD
    plate = dc.obox(kit, slot, lo=(x0, y_c - LABEL_H / 2, min(z_in, z_out)), hi=(x1, y_c + LABEL_H / 2, max(z_in, z_out)),
                    bevel=0.0002, segs=1, name="fire label")
    out = [tag(plate, levels)]
    if rivets:
        prof = [(0.0016, 0.0), (0.0013, 0.00035), (0.0007, 0.0006), (0.0002, 0.0007)]
        for yy in (y_c - LABEL_H / 2 + 0.006, y_c + LABEL_H / 2 - 0.006):
            r = dc.revolve(kit, prof, ((x0 + x1) / 2, yy, z_out - normal_sign * 0.0001), (0, 0, normal_sign), 10, slot, "label rivet")
            out.append(tag(r, "0"))
    return out


# ---------------------------------------------------------------- UVs (kitlib metres, then the leaf's own rules)
def leaf_uv_rules(kit, u_scale=1.0, u_flip=False):
    """After kitlib's metre projection: on the faces that look +-X (the leaf's two faces) u is Z (P) or -Z (S); u_scale squeezes
    the wide leaf onto the standard leaf's mask (the wear mask's u 0 at the hinge edge, 1 at the latch edge), u_flip swaps the
    faces' u for the north door (its stair face is P). Wraps this Kit instance's _uv_metres (kitlib.py is not edited)."""
    base = type(kit)._uv_metres

    def uv_metres(obj):
        base(obj)
        if not obj.get("fr_leaf_uv"):
            return
        me = obj.data
        layer = me.uv_layers.active
        for p in me.polygons:
            if abs(p.normal.x) < 0.9:
                continue
            for li in p.loop_indices:
                u, v = layer.data[li].uv
                s = 1.0 if u >= 0 else -1.0
                u = s * GAP_JAMB + (u - s * GAP_JAMB) * u_scale
                if u_flip:
                    u = -u
                layer.data[li].uv = (u, v)

    kit._uv_metres = uv_metres
    return kit


# ---------------------------------------------------------------- frames
def sheet_section(outer, radii, segs):
    """A closed 16 ga section from its visible outer path (u, x), the visible side on the LEFT of the walk; the inner path is
    the outer offset SHEET_T to the right."""
    o = dc.fillet_polygon(outer, radii, segs, closed=False)
    i = dc.offset_polyline(o, SHEET_T, normals_left=False, closed=False)
    return o + list(reversed(i))


def _signed_area(poly):
    a = 0.0
    for k in range(len(poly)):
        x0, y0 = poly[k]
        x1, y1 = poly[(k + 1) % len(poly)]
        a += x0 * y1 - x1 * y0
    return a / 2.0


def _edge_out(poly, k):
    x0, y0 = poly[k]
    x1, y1 = poly[(k + 1) % len(poly)]
    dx, dy = x1 - x0, y1 - y0
    ln = math.hypot(dx, dy) or 1.0
    n = (dy / ln, -dx / ln)
    return n if _signed_area(poly) > 0 else (-n[0], -n[1])


def opening_sweep(kit, section, W, slot, name, jambs_only=False):
    """The closed (u, x) section swept round the opening: u outward from the soffit plane (the hinge jamb at z = -u, the latch
    jamb at z = W + u, the head at y = HEAD + u), mitred at the head corners, the jambs from the floor. No caps (the feet stand
    on the floor; the mitres close)."""
    keys = ("hb", "hm", "lm", "lb")
    rings = []
    for key in keys:
        pts = []
        for u, x in section:
            if key == "hb":
                pts.append((x, 0.0, -u))
            elif key == "hm":
                pts.append((x, HEAD + u, -u))
            elif key == "lm":
                pts.append((x, HEAD + u, W + u))
            else:
                pts.append((x, 0.0, W + u))
        rings.append(pts)
    bm = bmesh.new()
    vr = [[bm.verts.new(U(*p)) for p in r] for r in rings]
    n = len(section)
    for s in range(3):
        a, b = vr[s], vr[s + 1]
        for k in range(n):
            j = (k + 1) % n
            nu, nx = _edge_out(section, k)
            if s == 0:
                want = (nx, 0.0, -nu)
            elif s == 1:
                want = (nx, nu, 0.0)
            else:
                want = (nx, 0.0, nu)
            _face(bm, (a[k], a[j], b[j], b[k]), want)
    return dc._part(kit, bm, slot, name)


def strip_u(kit, W, u0, u1, x, normal_sign, slot, name, head=True):
    """A flat strip in the plane x (facing normal_sign * X) from u0 to u1 round the opening (jambs, and the head if asked),
    mitred: the block infill beside a shell frame, the outer face of a shell frame."""
    bm = bmesh.new()
    def V(p):
        return bm.verts.new(U(*p))
    pieces = []
    if head:
        pieces.append([(x, 0.0, -u0), (x, 0.0, -u1), (x, HEAD + u1, -u1), (x, HEAD + u0, -u0)])
        pieces.append([(x, HEAD + u0, -u0), (x, HEAD + u1, -u1), (x, HEAD + u1, W + u1), (x, HEAD + u0, W + u0)])
        pieces.append([(x, HEAD + u0, W + u0), (x, HEAD + u1, W + u1), (x, 0.0, W + u1), (x, 0.0, W + u0)])
    else:
        pieces.append([(x, 0.0, -u0), (x, 0.0, -u1), (x, HEAD, -u1), (x, HEAD, -u0)])
        pieces.append([(x, HEAD, W + u0), (x, HEAD, W + u1), (x, 0.0, W + u1), (x, 0.0, W + u0)])
    for q in pieces:
        _face(bm, [V(p) for p in q], (normal_sign, 0.0, 0.0))
    return dc._part(kit, bm, slot, name)


# ---------------------------------------------------------------- LOD2 for the landed hardware kits (P-1)
def add_lod2(kit, build_proxy, note):
    """Append a hand-built <NAME>_LOD2 after build_asset.py's kit.make_lod1(): LOD0 and LOD1 stay exactly as the module makes
    them (the same parts, the same decimation or lod1_keep_all), the proxy is built after the join, given kitlib's metre UVs,
    triangulated, shaded, joined and registered in kit.lods[2], so export() writes LOD0 / LOD1 / LOD2 and the importer's third
    distance (dcull) culls LOD2 where LOD1 was culled before."""
    base = kit.make_lod1

    def make_lod1(ratio):
        base(ratio)
        n0 = len(kit.parts)
        build_proxy(kit)
        new = kit.parts[n0:]
        del kit.parts[n0:]
        for obj in new:
            bpy.ops.object.select_all(action="DESELECT")
            bpy.context.view_layer.objects.active = obj
            obj.select_set(True)
            for mod in list(obj.modifiers):
                bpy.ops.object.modifier_apply(modifier=mod.name)
            bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
            type(kit)._uv_metres(obj)
            bm = bmesh.new()
            bm.from_mesh(obj.data)
            bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 3])
            bm.to_mesh(obj.data)
            bm.free()
        lod2 = kit._join(new)
        lod2.name = lod2.data.name = kit.name + "_LOD2"
        lod2.data.shade_smooth()
        lod2.data.set_sharp_from_angle(angle=math.radians(30.0))
        kit.lods[2] = lod2
        kit.meta["trianglesLod2"] = sum(len(p.vertices) - 2 for p in lod2.data.polygons)
        kit.meta["lodNote"] = note

    kit.make_lod1 = make_lod1
    return kit


def prism_x(kit, x0, x1, section_yz, slot, name):
    """A straight prism along Unity X from a closed (y, z) section (LOD2 proxies)."""
    return dc.loft_x(kit, [(x0, section_yz), (x1, section_yz)], slot, name)


def cyl(kit, a, b, r, sides, slot, name):
    """A capped cylinder between Unity points a and b (LOD2 proxies)."""
    av, bv = Vector(a), Vector(b)
    axis = (bv - av).normalized()
    ref = Vector((0, 1, 0)) if abs(axis.y) < 0.9 else Vector((1, 0, 0))
    s1 = axis.cross(ref).normalized()
    s2 = axis.cross(s1).normalized()
    bm = bmesh.new()
    ra, rb = [], []
    for k in range(sides):
        t = 2 * math.pi * k / sides
        off = s1 * (r * math.cos(t)) + s2 * (r * math.sin(t))
        ra.append(bm.verts.new(U(*(av + off))))
        rb.append(bm.verts.new(U(*(bv + off))))
    for k in range(sides):
        j = (k + 1) % sides
        bm.faces.new((ra[k], ra[j], rb[j], rb[k]))
    bm.faces.new(ra)
    bm.faces.new(list(reversed(rb)))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return dc._part(kit, bm, slot, name)


def tris_of(objs):
    t = 0
    for o in objs:
        for p in o.data.polygons:
            t += len(p.vertices) - 2
    return t


# ---------------------------------------------------------------- slots (Resources/Surfaces/<slot>.mat; preview colours only)
STEEL, STEEL_IN = "Stair_DoorSteel", "Stair_DoorSteelInside"
FRAME, FRAME_IN = "Stair_DoorFrame", "Stair_DoorFrameInside"
HINGE, BLOCK, RUBBER, CHROME = "Stair_Hinge", "Stair_Block", "Prop_Rubber", "Prop_ChromeSatin"


def register_slots(kitlib):
    kitlib.register_slot(STEEL, (0.474, 0.434, 0.342), 0.5, 0.0)
    kitlib.register_slot(STEEL_IN, (0.474, 0.434, 0.342), 0.5, 0.0)
    kitlib.register_slot(FRAME, (0.114, 0.102, 0.080), 0.52, 0.0)
    kitlib.register_slot(FRAME_IN, (0.114, 0.102, 0.080), 0.52, 0.0)
    kitlib.register_slot(HINGE, (0.114, 0.102, 0.080), 0.5, 0.3)
    kitlib.register_slot(BLOCK, (0.70, 0.62, 0.50), 0.8, 0.0)
    kitlib.register_slot(CHROME, (0.75, 0.75, 0.75), 0.35, 1.0)


def _set_slots(obj, kit, slot_mx, slot_px, slot_edge):
    """Material slots on one leaf slab: faces looking -X take slot_mx, +X slot_px, every other face slot_edge."""
    names = []
    for s in (slot_edge, slot_mx, slot_px):
        if s not in names:
            names.append(s)
    obj.data.materials.clear()
    for s in names:
        obj.data.materials.append(kit._material(s))
    for p in obj.data.polygons:
        if p.normal.x < -0.9:          # Blender -x = Unity +X
            p.material_index = names.index(slot_px)
        elif p.normal.x > 0.9:         # Blender +x = Unity -X
            p.material_index = names.index(slot_mx)
        else:
            p.material_index = names.index(slot_edge)


def build_leaf(kit, module, W, lite, north, slot_mx, slot_px, slot_edge, seed):
    """One stair leaf (see the module docstring): LOD0 rounded arrises, edge seams, the lite cut, the hinge leaves, the fire label
    (+ the north door's latch faceplate and bolt); LOD1 1-step arrises, no seams or small parts but the latch; LOD2 sharp."""
    import random
    rng = random.Random(seed)
    if north:
        leaf_uv_rules(kit, u_flip=True)
    else:
        leaf_uv_rules(kit, u_scale=(EXIT_W - 2 * GAP_JAMB) / (W - 2 * GAP_JAMB))
    slabs = {}
    for level in (0, 1, 2):
        o = leaf_slab(kit, W, level, slot_edge, slot_edge, "leaf" if level == 0 else "leaf lod%d" % level)
        _set_slots(o, kit, slot_mx, slot_px, slot_edge)
        o["fr_leaf_uv"] = True
        tag(o, str(level))
        if lite:
            dc.cut(o, [lite_cutter(LITE)])
        slabs[level] = o
    hinge_plates(kit, HINGE, "0")
    fire_label(kit, slot_edge, GAP_JAMB, -1, -0.016, 0.016, LABEL_YC, "0")
    latch = []
    if north:
        # a bored latch's faceplate, mortised in the bevelled lock edge (0.2 mm proud), and its bolt, retracted (the lever's latch)
        n = Vector((-math.tan(math.radians(BEVEL_DEG)), 0.0, 1.0)).normalized()
        xdir = Vector((1.0, 0.0, math.tan(math.radians(BEVEL_DEG)))).normalized()
        from mathutils import Matrix
        R = Matrix((xdir, Vector((0, 1, 0)), n)).transposed()
        face = Vector((0.0, LATCH_YC, latch_z(W, 0.0)))
        plate = dc.obox(kit, CHROME, centre=face + n * (0.0002 - FACEPLATE_T / 2), half=(FACEPLATE_W / 2, FACEPLATE_H / 2, FACEPLATE_T / 2),
                        R=R, bevel=0.0003, segs=1, name="latch faceplate")
        latch.append(tag(plate, "01"))
        bolt = dc.obox(kit, CHROME, centre=face + n * (0.0002 + 0.0015), half=(0.006, 0.0125, 0.0015), R=R, bevel=0.0004, segs=1,
                       name="latch bolt (retracted)")
        latch.append(tag(bolt, "0"))
        for yy in (LATCH_YC - FACEPLATE_H / 2 + 0.0055, LATCH_YC + FACEPLATE_H / 2 - 0.0055):
            p = Vector((0.0, yy, latch_z(W, 0.0))) + n * 0.0002
            s = dc.flat_head(kit, tuple(p), tuple(n), CHROME, rng, d=0.0045, half_segs=5, proud=0.0001, chamfer=0.0003, name="faceplate screw")
            latch.append(tag(s, "0"))

    dc.finalize(kit, getattr(module, "SMOOTH_ANGLE", 35.0))

    # ---- asserts: the envelope; nothing reaches the EXIT frame's push-side stop; the swing keeps off the latch jamb
    for level in (0, 1, 2):
        lo, hi = dc.bounds_unity([slabs[level]])
        assert lo[0] >= -HT - 1e-5 and hi[0] <= HT + 1e-5, "leaf x %s %s" % (lo[0], hi[0])
        assert abs(lo[1] - LEAF_Y0) < 1e-5 and abs(hi[1] - LEAF_Y1) < 1e-5, "leaf y %s %s" % (lo[1], hi[1])
        # the latch edge's S corner is rounded (r 1.6 mm on the 93 deg bevel corner), so its reach is 0.1 mm short of the line
        assert abs(lo[2] - GAP_JAMB) < 1e-5 and -0.0002 < hi[2] - (W - GAP_JAMB) < 1e-5, "leaf z %s %s" % (lo[2], hi[2])
    worst = -1.0
    o = slabs[0]
    mw = o.matrix_world
    pts = [dc.to_unity(mw @ v.co) for v in o.data.vertices]
    for k in range(0, 96):
        th = math.radians(k)
        for sgn in ((1.0, -1.0) if north else (1.0,)):
            c, s_ = math.cos(th), math.sin(th) * sgn
            for X, Y, Z in pts:
                if Z < 0.05:
                    continue
                Xr = X * c + Z * s_
                Zr = -X * s_ + Z * c
                worst = max(worst, Zr)
                if not north:
                    assert not (Xr <= STOP_S_X and Zr > W - STOP_H), "the leaf meets the stop at %d deg" % k
    assert worst < W - 0.0005, "the latch edge reaches z %.4f of the latch soffit %.3f" % (worst, W)
    kit.meta["swingLatchWorstZ"] = round(worst, 5)
    kit.meta["leafEnvelope"] = {"x": [-HT, HT], "y": [LEAF_Y0, LEAF_Y1], "z": [GAP_JAMB, W - GAP_JAMB], "opening": W}
    if lite:
        kit.meta["stairLiteRect"] = list(LITE)
        kit.meta["liteHole"] = [LITE[0] - LITE_POCKET - HOLE_MARGIN, LITE[1] + LITE_POCKET + HOLE_MARGIN,
                                LITE[2] - LITE_POCKET - HOLE_MARGIN, LITE[3] + LITE_POCKET + HOLE_MARGIN]
    kit.meta["leafLevels"] = {"lod0": tris_of([slabs[0]]), "lod1": tris_of([slabs[1]]), "lod2": tris_of([slabs[2]])}
    dc.anchor(kit, "hinge_axis", 0.0, 0.0, 0.0)
    dc.anchor(kit, "face_p_centre", -HT, (LEAF_Y0 + LEAF_Y1) / 2, W / 2)
    dc.anchor(kit, "face_s_centre", HT, (LEAF_Y0 + LEAF_Y1) / 2, W / 2)
    for k, yc in enumerate(HINGE_YC):
        dc.anchor(kit, "knuckle_%d" % (k + 1), KNUCKLE_X, yc, KNUCKLE_Z)
    kit.meta["doorSpace"] = ("the kits' door root: origin the opening's hinge-jamb edge on the leaf's centre plane at the floor, +Z "
                             "along the opening, +X the pull face S; FrontRoomsStairwellDresser spawns it under a root scaled (-push, 1, 1)")
    kit.no_collider()
    kit.tag("door", "door_leaf", "leaf_steel", "stair")
    dc.lod_meta(kit, module)
    kit.meta["lodNote"] = "hand-built LOD0/1/2 (kitlib fr_lods); lodDistances (d01, d12, dcull) m at lodBias 1, FOV 76; -1 = never cull"


# The map's wall ends on the opening's line (MapWorld BuildEdge: Piece to c -+ width / 2, and the head's underside at DoorHeight),
# papered: a soffit on that line ties with the paper in the depth test and the paper won (a strip of wallpaper in the door
# rabbet, first Unity capture 20:1x). The EXIT frame's soffits stand SOFFIT_IN into the opening, in front of it.
SOFFIT_IN = 0.0005


def exit_frame_outer():
    """The EXIT frame's visible path (u, x), P (-x, N's side) to S (+x, the stair): the return, the 2 in face, the second rabbet,
    the 5/8 in stop on P (the leaf swings to S), the deep door rabbet the map's centred leaf leaves, the S face and return; the
    whole section SOFFIT_IN toward the opening (the face's outer edge 0.5 mm inside the trim's envelope)."""
    fx = EXIT_FACE_X
    pts = [(FACE, -(WALL_X - FOOT_IN)), (FACE, -fx), (0.0, -fx), (0.0, STOP_P_X), (-STOP_H, STOP_P_X), (-STOP_H, STOP_S_X),
           (0.0, STOP_S_X), (0.0, fx), (FACE, fx), (FACE, WALL_X - FOOT_IN)]
    return [(u - SOFFIT_IN, x) for u, x in pts]


def exit_frame_section(level):
    outer = exit_frame_outer()
    cv, cc = CONVEX, CONCAVE
    radii = [0, cv, cv, cc, cv, cv, cc, cv, cv, 0] if level < 2 else [0] * 10
    return sheet_section(outer, radii, 3 if level == 0 else 1)


def shell_frame_outer(stair_sign):
    """A shell doorway's visible path (u, x): from the outer face's inner edge, across the soffit to the stair face (7/16 in proud
    of the stair wall face), out to the 2 in face's edge and its return into the block infill. stair_sign: +1 the stair face at
    +x (south: the stair is the pull side), -1 at -x (north: the stair is the push side). Walked with the visible side left."""
    so, sf = -stair_sign * SHELL_HALF, stair_sign * SHELL_FACE_X
    foot = stair_sign * (SHELL_HALF - FOOT_IN)
    pts = [(0.0, so), (0.0, sf), (FACE, sf), (FACE, foot)]
    if stair_sign < 0:
        pts = [(u, x) for u, x in reversed(pts)]
    return pts


def shell_frame_section(stair_sign, level):
    outer = shell_frame_outer(stair_sign)
    cv = CONVEX
    radii = [0, cv, cv, 0] if level < 2 else [0] * 4
    return sheet_section(outer, radii, 3 if level == 0 else 1)


def soffit_strip(kit, W, x0, x1, slot, name, head=True):
    """Flat strips in the soffit plane u = 0 between x0 and x1: the hinge jamb (z 0, facing +Z), the head (y HEAD, facing -Y)
    and the latch jamb (z W, facing -Z)."""
    bm = bmesh.new()
    def V(p):
        return bm.verts.new(U(*p))
    _face(bm, [V((x0, 0.0, 0.0)), V((x1, 0.0, 0.0)), V((x1, HEAD, 0.0)), V((x0, HEAD, 0.0))], (0.0, 0.0, 1.0))
    _face(bm, [V((x0, 0.0, W)), V((x1, 0.0, W)), V((x1, HEAD, W)), V((x0, HEAD, W))], (0.0, 0.0, -1.0))
    if head:
        _face(bm, [V((x0, HEAD, 0.0)), V((x1, HEAD, 0.0)), V((x1, HEAD, W)), V((x0, HEAD, W))], (0.0, -1.0, 0.0))
    return dc._part(kit, bm, slot, name)


def jamb_flanges(kit, W, x, u_in, normal_sign, slot, name):
    """Flat flanges in the plane x on both jambs, reaching u_in into the opening from the soffit line (facing normal_sign * X)."""
    bm = bmesh.new()
    def V(p):
        return bm.verts.new(U(*p))
    _face(bm, [V((x, 0.0, 0.0)), V((x, 0.0, u_in)), V((x, HEAD, u_in)), V((x, HEAD, 0.0))], (normal_sign, 0.0, 0.0))
    _face(bm, [V((x, 0.0, W - u_in)), V((x, 0.0, W)), V((x, HEAD, W)), V((x, HEAD, W - u_in))], (normal_sign, 0.0, 0.0))
    return dc._part(kit, bm, slot, name)


def frame_hinge_halves(kit, levels="0", soffit_in=0.0):
    """The frame halves of the three butts on the hinge jamb's soffit (z soffit_in, 0.3 mm proud), from the knuckle toward P."""
    out = []
    for yc in HINGE_YC:
        o = dc.obox(kit, HINGE, lo=(-0.010, yc - HINGE_H / 2, soffit_in - 0.0025), hi=(KNUCKLE_X - KNUCKLE_R + 0.0004, yc + HINGE_H / 2, soffit_in + 0.0003),
                    bevel=0.0003, segs=1, name="hinge leaf (frame half)")
        out.append(tag(o, levels))
    return out


def assert_frame_clear_of_leaf(objs, W, skip_names=()):
    """No frame vertex inside the shut leaf's box (the map's centred 50 mm leaf with this run's 4 mm gaps)."""
    for o in objs:
        if any(o.name.startswith(s) for s in skip_names):
            continue
        mw = o.matrix_world
        for v in o.data.vertices:
            X, Y, Z = dc.to_unity(mw @ v.co)
            inside = (-HT + 1e-4 < X < HT - 1e-4) and (LEAF_Y0 + 1e-4 < Y < LEAF_Y1 - 1e-4) and \
                     (GAP_JAMB + 1e-4 < Z < latch_z(W, X) - 1e-4)
            assert not inside, "%s vertex %s inside the shut leaf" % (o.name, (round(X, 4), round(Y, 4), round(Z, 4)))


def frame_meta(kit, module, W, extra_tags):
    dc.anchor(kit, "hinge_axis", 0.0, 0.0, 0.0)
    dc.anchor(kit, "head_soffit", 0.0, HEAD, W / 2)
    kit.meta["opening"] = [W, HEAD]
    kit.meta["doorSpace"] = ("the kits' door root: origin the opening's hinge-jamb edge on the leaf's centre plane at the floor, +Z "
                             "along the opening, +X the pull face S; FrontRoomsStairwellDresser spawns it under a root scaled (-push, 1, 1)")
    kit.no_collider()
    kit.tag("door", "door_frame", "frame_steel", "stair", *extra_tags)
    dc.lod_meta(kit, module)
    kit.meta["lodNote"] = "hand-built LOD0/1/2 (kitlib fr_lods); lodDistances (d01, d12, dcull) m at lodBias 1, FOV 76; -1 = never cull"
