"""P17 structures_office_props (structures_assets G3-b, line visual (game visual), 2026-11-10): SP4's Office kits.

Plan: Documentation/research/structures_assets/30_plan.md P17, S4-02 (storefront glazing), S4-04 (the pit's four bays: panels
at 1.35 and 2.16 m, never a scale of the 1.65 m kit), S4-07 (the E5b blade field: freestanding panels with stabiliser feet
at 1.35 / 1.65 / 2.16 m); 70_structures_districts.md section 6; 31 section 5 (E5b).

The panels are the game's own family (Tools/Blender/frontrooms_kit/assets/cubicle_panel.py and panel_post.py, main): fabric
on both faces in a putty frame, a rounded cap rail, end trims with slotted standards, a dark raceway base on glides, the 64 mm
square post with its end cap flush with the caps. These kits build that construction again, part for part, at the period's
own heights: Herman Miller Action Office panels came 32, 39, 47, 53, 62, 67 and 85 in high (AO system literature) and
Steelcase Series 9000's 34.5, 42, 53, 61, 65 and 75 in (Series 9000 guide): 53 in = 1.346 m and 85 in = 2.159 m. The hero
rule (the cap rail at 1.35 and 2.16 m holds at 0.35 m): the cap's arch at 16 steps, the end trims' corners at 6, the glides
at the chord rule; LOD1 drops the slots, standards and glides (the family's own LOD1 rule) and halves the arcs; LOD2 boxes.

Freestanding screens (the blade field): the same panel with both ends finished (a moulded cap end over each cap rail end) on
two T-base stabiliser feet in place of the end glides: 18.4-18.75 in long, 2 in wide, 1.9 in high in present-day listings of
the same long-running form; here 0.470 x 0.051 x 0.048, putty steel, a glide under each toe, a clamp bracket on the panel end.

Storefront (S4-02, the boardroom's glass front): a 1988-91 interior storefront of the 1-3/4 x 4-1/2 in centre-set class
(Kawneer 450 / US Aluminum 450: 44.45 x 114.3 mm, for 1/4 in glass) in clear anodised aluminium: the tube with a glazing
pocket on each glazed side, EPDM wedge gaskets on both faces of the glass (a black bead at each pocket mouth: the product
approval report FL20446 for the 451T lists EPDM gaskets interior and exterior with a snap-in glass stop). Kits: the
intermediate mullion (HERO), the wall jamb (one pocket; shims and a sealant bead at the wall), the 90 degree corner post. The
head and sill channels, the panes and the setting blocks are laid by the builder (FrontRoomsStructureOfficeKits.cs) along
each opening, so a run of any length takes the same members.

Frames: UNITY space as _struct_common (x along the panel / the run, y up, z through it, front +Z), converted with ub().
Slots: Prop_FabricCubicle, Prop_SteelPutty, Prop_PlasticBlack (the family's), Trans_AluminiumSatin, Prop_Rubber (main's).
No new material or map. No text, label, brand or logo. ASCII only.
"""

import math

import bmesh

import _run_b_common as B
import _struct_common as S

FABRIC = "Prop_FabricCubicle"
TRIM = "Prop_SteelPutty"
DARK = "Prop_PlasticBlack"
ALU = "Trans_AluminiumSatin"
RUBBER = "Prop_Rubber"

# The family's numbers (cubicle_panel.py / panel_post.py, main), kept so the new heights join the old kits without a step.
T = 0.064               # panel thickness
TRIM_W = 0.030          # end trim width (x)
TRIM_R = 0.011          # end trim outer corner radius
STRIP_W = 0.016         # slotted standard beside each trim
FABRIC_INSET = 0.004    # fabric face under the trim face
RACE_Y0, RACE_H = 0.020, 0.100
CAP_TOP_H = 0.026       # cap height above the panel body
CAP_LIP = 0.008         # cap sides lap the fabric this far
SLOT_PITCH = 0.050
TRIM_SHORT = 0.0005     # end trims stop this far inside the cap ends
POST_S, POST_R, POST_GLIDE_H, POST_CAP_H = 0.064, 0.008, 0.012, 0.010

AO_53 = 53 * 0.0254     # 1.3462
AO_85 = 85 * 0.0254     # 2.159
W_60 = 1.524            # 60 in panel
W_30 = 0.762            # 30 in panel


# ============================================================================ geometry helpers (Unity space)
def _arc(cx, cy, r, a0, a1, steps):
    return [(cx + r * math.cos(math.radians(a0 + (a1 - a0) * k / steps)),
             cy + r * math.sin(math.radians(a0 + (a1 - a0) * k / steps))) for k in range(steps + 1)]


def _solid(kit, m, name, slot):
    """A closed part from a Unity-space Mesh, its normals made outward (closed solids only)."""
    obj = m.part(kit, name, slot)
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(obj.data)
    bm.free()
    return obj


def _loft(m, rings, caps=(True, True), u_along=None):
    """Rings of Unity points (same count, closed loops) joined in order; caps close the first and last ring.
    UVs: u = metres along the rings' centres (or u_along), v = metres round the ring (mesh-UV metres)."""
    n = len(rings[0])
    cent = [tuple(sum(p[k] for p in r) / n for k in range(3)) for r in rings]
    along = [0.0]
    for i in range(1, len(rings)):
        along.append(along[-1] + math.dist(cent[i], cent[i - 1]))
    if u_along is not None:
        along = u_along
    around = [0.0]
    for j in range(1, n + 1):
        around.append(around[-1] + math.dist(rings[0][(j - 1) % n], rings[0][j % n]))
    for i in range(len(rings) - 1):
        for j in range(n):
            j1 = (j + 1) % n
            m.quad(rings[i][j], rings[i][j1], rings[i + 1][j1], rings[i + 1][j],
                   (along[i], around[j]), (along[i], around[j + 1]), (along[i + 1], around[j + 1]), (along[i + 1], around[j]))
    for flag, ring in ((caps[0], rings[0]), (caps[1], rings[-1])):
        if not flag:
            continue
        idx = [m.vert(p) for p in ring]
        # planar cap UVs from the ring's own two largest axes
        span = [max(p[k] for p in ring) - min(p[k] for p in ring) for k in range(3)]
        ax = sorted(range(3), key=lambda k: -span[k])[:2]
        m.face(tuple(idx), [(p[ax[0]], p[ax[1]]) for p in ring])


def _extrude_y(m, section_xz, y0, y1, caps=(True, True)):
    """A section in (x, z) extruded up from y0 to y1."""
    _loft(m, [[(x, y0, z) for x, z in section_xz], [(x, y1, z) for x, z in section_xz]], caps, u_along=[y0, y1])


def _extrude_x(m, section_yz, x0, x1, caps=(True, True)):
    """A section in (y, z) extruded along x from x0 to x1 (u = world x metres)."""
    _loft(m, [[(x0, y, z) for y, z in section_yz], [(x1, y, z) for y, z in section_yz]], caps, u_along=[x0, x1])


def _rrect(w, h, r, steps, cx=0.0, cy=0.0):
    """A rounded rectangle (counter-clockwise) centred at (cx, cy)."""
    out = []
    hw, hh = w / 2.0, h / 2.0
    for (ox, oy, a0) in ((hw - r, -hh + r, -90), (hw - r, hh - r, 0), (-hw + r, hh - r, 90), (-hw + r, -hh + r, 180)):
        if steps <= 0 or r <= 0:
            out.append((cx + ox + math.copysign(r, ox), cy + oy + math.copysign(r, oy)))
            continue
        for k in range(steps + 1):
            a = math.radians(a0 + 90.0 * k / steps)
            out.append((cx + ox + r * math.cos(a), cy + oy + r * math.sin(a)))
    return out


def _glide(kit, lv, r, h, centre, slot, name="glide"):
    """A levelling glide: a short cylinder, 16 sides with a 0.8 mm round at LOD0, 8 plain at LOD1 (none at LOD2)."""
    if lv >= 2:
        return None
    return kit.cylinder(r, h, S.ub(centre), slot, verts=16 if lv == 0 else 8, bevel=0.0008 if lv == 0 else 0.0, segments=1, name=name)


# ============================================================================ the panel family
def cap_section(lv):
    """The cap rail's section in (y, z), y = 0 the body top: flat sides lapping the fabric, a flattened arch (16 steps at the
    hero level), closed along its underside (inside the panel)."""
    steps = (16, 8, 4)[lv]
    half = T / 2 + 0.0015
    pts = [(half, -CAP_LIP), (half, 0.006)]
    for i in range(1, steps):
        a = math.pi * i / steps
        pts.append((half * math.cos(a), 0.006 + (CAP_TOP_H - 0.006) * math.sin(a)))
    pts += [(-half, 0.006), (-half, -CAP_LIP)]
    return [(y, z) for z, y in pts]


def _trim_section(hw, sx, lv):
    """An end trim's section in (x, z) for the trim at x = sx * hw: outer corners rounded TRIM_R, inner corners chamfered 2 mm;
    its outer face TRIM_SHORT inside hw (never in the cap end's plane)."""
    st = (6, 3, 1)[lv]
    ix, c, ht = hw - TRIM_W, 0.002, T / 2
    ox = hw - TRIM_SHORT
    pts = [(ix + c, -ht)]
    pts += _arc(ox - TRIM_R, -ht + TRIM_R, TRIM_R, -90, 0, st)
    pts += _arc(ox - TRIM_R, ht - TRIM_R, TRIM_R, 0, 90, st)
    pts += [(ix + c, ht), (ix, ht - c), (ix, -ht + c)]
    out = [(sx * x, z) for x, z in pts]
    if sx < 0:
        out.reverse()
    return out


def _cap_end(kit, lv, x_end, sx, body_top):
    """A moulded cap end over an exposed cap rail end (freestanding screens): the cap's section run 3 mm on, then drawn in to
    85 % over 1.5 mm and closed, so the end reads as one moulded piece."""
    sec = cap_section(lv)
    cy = sum(p[0] for p in sec) / len(sec)
    rings = []
    for dx, k in ((0.0, 1.0), (0.003, 1.0), (0.0045, 0.85)):
        x = x_end + sx * dx
        rings.append([(x, body_top + cy + (y - cy) * k, z * k) for y, z in sec])
    if sx < 0:
        rings = [list(reversed(r)) for r in rings]
    m = S.Mesh()
    _loft(m, rings, caps=(False, True))
    return _solid(kit, m, "cap end", TRIM)


def panel(kit, lv, width, height, ends="family"):
    """The family panel (cubicle_panel.build_panel's construction) at any width and height, by level.
    ends: "family" (glides, cap ends meet posts) or "free" (moulded cap ends, no glides: the feet carry it)."""
    hw = width / 2.0
    body_top = height - CAP_TOP_H
    race_top = RACE_Y0 + RACE_H
    face = T / 2 - FABRIC_INSET
    inner_x = hw - TRIM_W

    # Cap rail: one run exactly `width` long; the trims stop TRIM_SHORT inside its ends.
    m = S.Mesh()
    _extrude_x(m, [(body_top + y, z) for y, z in cap_section(lv)], -hw, hw)
    _solid(kit, m, "top cap", TRIM)
    if ends == "free":
        for sx in (-1, 1):
            _cap_end(kit, lv, sx * hw, sx, body_top)

    # End trims: rounded sections from the base rail's foot up into the cap.
    for sx in (-1, 1):
        m = S.Mesh()
        _extrude_y(m, _trim_section(hw, sx, lv), RACE_Y0, body_top + 0.002)
        _solid(kit, m, "end trim", TRIM)

    # Fabric core, both faces, its edges tucked into the trims, the cap's lap and the base rail.
    fab_w = 2 * inner_x + 0.006
    fy0, fy1 = race_top - 0.002, body_top + 0.002
    kit.box((fab_w, 2 * face, fy1 - fy0), S.ub((0.0, (fy0 + fy1) / 2, 0.0)), FABRIC, bevel=0.0, segments=1, name="fabric")

    # Slotted standards (LOD0 only, as the family's LOD1): a putty channel beside each trim, 1 mm proud of the fabric, with a
    # column of 24 mm slots at 50 mm pitch on each face (hang-on shelves and binder bins).
    if lv == 0:
        sy0, sy1 = race_top + 0.001, body_top + 0.002
        slots = S.Mesh()
        n = int((body_top - race_top - 0.12) / SLOT_PITCH) + 1
        for sx in (-1, 1):
            sxc = sx * (inner_x - STRIP_W / 2 + 0.002)
            kit.box((STRIP_W + 0.004, 2 * face + 0.002, sy1 - sy0), S.ub((sxc, (sy0 + sy1) / 2, 0.0)), TRIM, bevel=0.0006, segments=1,
                    name="standard")
            for k in range(n):
                y = race_top + 0.07 + k * SLOT_PITCH
                for sz in (-1, 1):
                    z = sz * (face + 0.0016)
                    a, b = sxc - 0.00225, sxc + 0.00225
                    y0, y1 = y - 0.012, y + 0.012
                    if sz > 0:
                        slots.quad((a, y0, z), (b, y0, z), (b, y1, z), (a, y1, z), (a, y0), (b, y0), (b, y1), (a, y1))
                    else:
                        slots.quad((b, y0, z), (a, y0, z), (a, y1, z), (b, y1, z), (b, y0), (a, y0), (a, y1), (b, y1))
        slots.part(kit, "standard slots", DARK, merge=False)

    # Raceway base: a dark core with a cover plate each side of the middle seam on both faces, 2 mm proud of the fabric.
    core_t = T - 0.012
    kit.box((2 * inner_x + 0.004, core_t, RACE_H), S.ub((0.0, RACE_Y0 + RACE_H / 2, 0.0)), DARK, bevel=0.0, segments=1, name="raceway")
    if lv < 2:
        cover_w = inner_x - 0.0015
        for sx in (-1, 1):
            for sz in (-1, 1):
                kit.box((cover_w, 0.0045, RACE_H - 0.008),
                        S.ub((sx * (cover_w / 2 + 0.0008), RACE_Y0 + RACE_H / 2, sz * (face + 0.002 - 0.00225))), DARK,
                        bevel=0.0012 if lv == 0 else 0.0, segments=1, name="raceway cover")
    else:
        kit.box((2 * inner_x + 0.004, T - 0.004, RACE_H), S.ub((0.0, RACE_Y0 + RACE_H / 2, 0.0)), DARK, bevel=0.0, segments=1, name="raceway")

    if ends == "family" and lv < 2:
        for sx in (-1, 1):
            _glide(kit, lv, 0.013, RACE_Y0, (sx * (hw - TRIM_W / 2 - 0.003), RACE_Y0 / 2, 0.0), DARK)

    if lv == 0:
        kit.collider(S.ub((0.0, height / 2, 0.0)), (width, T, height))
        kit.anchor("top_centre", S.ub((0.0, height, 0.0)))


def post(kit, lv, height):
    """The family's connector post (panel_post.build_post): 64 mm square, r 8 corners, a chamfered end cap flush with the panel
    caps, a levelling glide. No collider (the panels carry it)."""
    st = (3, 1, 0)[lv]
    m = S.Mesh()
    _extrude_y(m, _rrect(POST_S, POST_S, POST_R, st), POST_GLIDE_H, height - POST_CAP_H + 0.002)
    _solid(kit, m, "post", TRIM)
    S.box(kit, (POST_S, POST_CAP_H, POST_S), (0.0, height - POST_CAP_H / 2, 0.0), TRIM, lv, w=0.003, name="end cap")
    if lv < 2:
        _glide(kit, lv, 0.014, POST_GLIDE_H, (0.0, POST_GLIDE_H / 2, 0.0), TRIM)
    if lv == 0:
        kit.no_collider()
        kit.anchor("top_centre", S.ub((0.0, height, 0.0)))


FOOT_L, FOOT_W, FOOT_H = 0.470, 0.051, 0.048


def foot(kit, lv, x):
    """A T-base stabiliser foot across the panel at x: a putty steel channel 0.470 long, 51 wide, 48 high at the panel and
    tapering to 20 mm at the toes, its top edges rounded 6 mm; a glide under each toe; the clamp bracket on the panel end with
    two screws a face."""
    st = (3, 1, 0)[lv]
    zs = (-FOOT_L / 2, -FOOT_L / 2 + 0.020, -0.07, 0.07, FOOT_L / 2 - 0.020, FOOT_L / 2)
    hs = (0.020, 0.026, FOOT_H - 0.006, FOOT_H - 0.006, 0.026, 0.020)
    if lv == 2:
        zs, hs = (zs[0], zs[2], zs[3], zs[5]), (hs[0], hs[2], hs[3], hs[5])
    y0 = 0.006
    rings = []
    for z, h in zip(zs, hs):
        sec = []
        r = min(0.006, h * 0.4)
        hw = FOOT_W / 2
        sec.append((x + hw, y0))
        if st > 0:
            for (cx, cy, cr, a0, a1) in ((x + hw - r, y0 + h - r, r, 0, 90), (x - hw + r, y0 + h - r, r, 90, 180)):
                for k in range(st + 1):
                    a = math.radians(a0 + (a1 - a0) * k / st)
                    sec.append((cx + cr * math.cos(a), cy + cr * math.sin(a)))
        else:
            sec += [(x + hw, y0 + h), (x - hw, y0 + h)]
        sec.append((x - hw, y0))
        rings.append([(px, py, z) for px, py in sec])
    m = S.Mesh()
    _loft(m, rings)
    _solid(kit, m, "stabiliser foot", TRIM)
    if lv < 2:
        for sz in (-1, 1):
            _glide(kit, lv, 0.011, y0, (x, y0 / 2, sz * (FOOT_L / 2 - 0.022)), DARK, name="toe glide")
    # the clamp bracket over the panel's end trim and raceway
    S.box(kit, (TRIM_W + 0.010, 0.085, T + 0.010), (x, y0 + FOOT_H - 0.006 + 0.0425, 0.0), TRIM, lv, w=0.0015, name="foot bracket")
    if lv == 0:
        for sz in (-1, 1):
            # one pan-head screw a face (a flat 10-sided head, 1.8 mm proud of the bracket's face)
            kit.cylinder(0.0042, 0.0018, S.ub((x, 0.092, sz * (T / 2 + 0.005 + 0.0009))), TRIM, verts=10, rot=(90, 0, 0),
                         bevel=0.0004, segments=1, name="bracket screw")


def freestanding(kit, lv, height):
    """A freestanding screen 1.524 wide: the family panel with finished ends on two stabiliser feet (no posts). Collider: the
    panel body (the feet are stepped over)."""
    panel(kit, lv, W_60, height, ends="free")
    for sx in (-1, 1):
        foot(kit, lv, sx * (W_60 / 2 - TRIM_W / 2))


# ============================================================================ storefront (centre-set, 1-3/4 x 4-1/2 in)
SF_W = 0.04445          # sightline (along the run)
SF_D = 0.1143           # depth (through the wall)
SF_POCKET = 0.008       # half the glazing pocket's width (z): 16 mm for 1/4 in glass and its two gaskets
SF_POCKET_D = 0.0127    # pocket depth from the face
GLASS_HALF = 0.003175   # 1/4 in glass
SF_R = 0.0012           # extrusion corner radius
SF_H = 1.81 - 2 * 0.04445  # between the sill and head channels of the boardroom's 1.81 m opening (F1 + 1.07 to + 2.88)


def _tube_section(lv, pockets):
    """The member's section in (x, z): a 44.45 x 114.3 tube with glazing pockets on the sides named in `pockets` ('+x', '-x'),
    small corner radii at LOD0 (the extrusion's own), counter-clockwise."""
    hw, hd = SF_W / 2, SF_D / 2
    p, q = SF_POCKET, SF_POCKET_D
    st = 2 if lv == 0 else 0
    pts = []

    def corner(cx, cy, a0):
        if st == 0:
            pts.append((cx + math.copysign(SF_R, math.cos(math.radians(a0 + 45))), cy + math.copysign(SF_R, math.sin(math.radians(a0 + 45)))))
            return
        for k in range(st + 1):
            a = math.radians(a0 + 90.0 * k / st)
            pts.append((cx + SF_R * math.cos(a), cy + SF_R * math.sin(a)))
    corner(hw - SF_R, -hd + SF_R, -90)
    if "+x" in pockets:
        pts += [(hw, -p - 0.0006), (hw - 0.0006, -p), (hw - q, -p), (hw - q, p), (hw - 0.0006, p), (hw, p + 0.0006)]
    corner(hw - SF_R, hd - SF_R, 0)
    corner(-hw + SF_R, hd - SF_R, 90)
    if "-x" in pockets:
        pts += [(-hw, p + 0.0006), (-hw + 0.0006, p), (-hw + q, p), (-hw + q, -p), (-hw + 0.0006, -p), (-hw, -p - 0.0006)]
    corner(-hw + SF_R, -hd + SF_R, 180)
    return pts


def _gasket_sections(lv, sx):
    """The two wedge gaskets in the pocket on side sx: in the pocket between the glass face and the pocket wall, each with a
    rounded bead 1.5 mm out of the pocket mouth (what one sees: a black line each side of the glass)."""
    hw = SF_W / 2
    out = []
    st = 3 if lv == 0 else 1
    for sz in (-1, 1):
        zi, zo = sz * (GLASS_HALF + 0.0001), sz * (SF_POCKET - 0.0002)
        x_in, x_mouth = sx * (hw - SF_POCKET_D + 0.0015), sx * hw
        sec = [(x_in, zi), (x_in, zo), (x_mouth, zo)]
        # the bead: a half round of 2.4 mm out past the mouth, from the pocket wall to the glass face
        r = abs(zo - zi) / 2
        cz = (zo + zi) / 2
        for k in range(1, st + 1):
            a = math.pi * k / (st + 1)
            sec.append((x_mouth + sx * r * 0.62 * math.sin(a), cz + (zo - cz) * math.cos(a)))
        sec.append((x_mouth, zi))
        area = sum(sec[i][0] * sec[(i + 1) % len(sec)][1] - sec[(i + 1) % len(sec)][0] * sec[i][1] for i in range(len(sec)))
        if area < 0:
            sec.reverse()
        out.append(sec)
    return out


def storefront_member(kit, lv, pockets, height=SF_H, name="mullion"):
    """A vertical storefront member, its foot on the sill's top (origin), its section centred on the glass line (z 0), its
    pockets on the named sides with their gaskets."""
    m = S.Mesh()
    _extrude_y(m, _tube_section(lv, pockets), 0.0, height)
    _solid(kit, m, name, ALU)
    if lv < 2:
        for side in pockets:
            sx = 1 if side == "+x" else -1
            for sec in _gasket_sections(lv, sx):
                g = S.Mesh()
                _extrude_y(g, sec, 0.0, height)
                _solid(kit, g, "gasket", RUBBER)
    else:
        for side in pockets:
            sx = 1 if side == "+x" else -1
            S.box(kit, (0.003, height, SF_POCKET * 2), (sx * (SF_W / 2 - 0.001), height / 2, 0.0), RUBBER, lv, w=0.0, name="gasket")
    if lv == 0:
        kit.no_collider()
        kit.anchor("top", S.ub((0.0, height, 0.0)))


def storefront_jamb(kit, lv, height=SF_H):
    """The jamb at a wall: the tube with one pocket (+x, the glass side), its back (-x) on the wall with shims and a sealant
    bead in both corners where it meets the reveal."""
    storefront_member(kit, lv, ("+x",), height, name="jamb")
    if lv == 0:
        # a concave sealant fillet (r 4.5 mm) in each re-entrant corner between the jamb's face (z = +-hd) and the reveal
        # (the wall plane x = -hw): corner point, along the face, the concave arc round (-hw + r, +-(hd + r)), along the reveal
        hw, hd = SF_W / 2, SF_D / 2
        r, st = 0.0045, 4
        for sz in (-1, 1):
            cx, cz = -hw + r, sz * (hd + r)
            sec = [(-hw, sz * hd), (-hw + r, sz * hd)]
            for k in range(1, st):
                a = math.radians(270.0 - 90.0 * k / st) if sz > 0 else math.radians(90.0 + 90.0 * k / st)
                sec.append((cx + r * math.cos(a), cz + r * math.sin(a)))
            sec.append((-hw, sz * (hd + r)))
            area = sum(sec[i][0] * sec[(i + 1) % len(sec)][1] - sec[(i + 1) % len(sec)][0] * sec[i][1] for i in range(len(sec)))
            if area < 0:
                sec.reverse()
            g = S.Mesh()
            _extrude_y(g, sec, 0.0, height)
            _solid(kit, g, "sealant", RUBBER)


def storefront_corner(kit, lv, height=SF_H):
    """A 90 degree outside corner post: two tubes as one 114.3 square post, a pocket on +x (the run along x, glass at z 0) and on
    +z (the run along z, glass at x 0), the outer corner (-x, -z) eased."""
    hd = SF_D / 2
    p, q = SF_POCKET, SF_POCKET_D
    st = 3 if lv == 0 else 0
    r_out = 0.006 if lv == 0 else 0.0
    pts = [(hd, -hd)]
    pts += [(hd, -p - 0.0006), (hd - 0.0006, -p), (hd - q, -p), (hd - q, p), (hd - 0.0006, p), (hd, p + 0.0006)]
    pts += [(hd, hd)]
    pts += [(p + 0.0006, hd), (p, hd - 0.0006), (p, hd - q), (-p, hd - q), (-p, hd - 0.0006), (-p - 0.0006, hd)]
    pts += [(-hd, hd)]
    if st:
        pts += _arc(-hd + r_out, -hd + r_out, r_out, 180, 270, st)
    else:
        pts.append((-hd, -hd))
    m = S.Mesh()
    _extrude_y(m, pts, 0.0, height)
    _solid(kit, m, "corner post", ALU)
    if lv < 2:
        for sec in _gasket_sections(lv, 1):
            g = S.Mesh()
            _extrude_y(g, [(x + hd - SF_W / 2, z) for x, z in sec], 0.0, height)
            _solid(kit, g, "gasket", RUBBER)
            g = S.Mesh()
            _extrude_y(g, list(reversed([(z, x + hd - SF_W / 2) for x, z in sec])), 0.0, height)
            _solid(kit, g, "gasket", RUBBER)
    if lv == 0:
        kit.no_collider()
        kit.anchor("top", S.ub((0.0, height, 0.0)))


def meta(kit, what):
    kit.meta["structures"] = "structures_assets G3-b 2026-11-10 (30_plan.md P17 structures_office_props: %s)" % what
