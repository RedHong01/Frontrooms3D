"""Kit_Clipboard: the Daily Activity Report on the work surface (30L S15; 01 §6 DAR; plan 70 P17).

A 9 x 12.5 in tempered hardboard clipboard with a nickel spring clip (lever, clamp bar, two rivets) and four letter
sheets; the top sheet carries the DAR (paper atlas cell ``dar``, 平面视觉's art, 叙事's strings; blank until then),
the sheets below the blank cell. EXAMINE-readable (30L S15; plan check C1-10). DeskTop placement: lies flat, the clip
toward local -Z (away from a reader on the staff side at yaw 0). ORIGIN = the desk surface under the board's centre.
Budget 1,800 / 600 / 60. Slots Prop_Hardboard, Prop_Chrome, Prop_StationPaper. No collider.

``build_board(kit, wall)`` is shared with clipboard_wall.py (Kit_Clipboard_Wall, the sign-out sheet).
"""

import math

import _station_common as sc

NAME = "Kit_Clipboard"
LODS = (0.94, 0.0)    # small desk/wall kit: LOD1 drops sub-pixel parts only (no collapse), no LOD2
SMOOTH_ANGLE = 35.0

BW, BH, BT = 0.229, 0.318, 0.003
SW, SH = 0.216, 0.279


def build_board(kit, wall, cell):
    """Build the board in a local frame: x across, 'up' along the board (toward the clip), 'n' out of the board face.
    wall: the board hangs on a nail (up = +Y, n = +Z, origin on the wall face at floor level); else it lies on a desk
    (up = -Z, n = +Y, origin on the desk)."""
    sc.register()
    if wall:
        y0 = 1.12

        def P(x, u, n):
            return (x, y0 + u, 0.004 + n)
        face = "+z"
    else:
        def P(x, u, n):
            return (x, n, BH / 2 - u)
        face = "+y"

    def box_l(size_x, size_u, size_n, x, u, n, slot, bevel=0.001, name="part"):
        if wall:
            sc.box(kit, (size_x, size_u, size_n), P(x, u, n), slot, bevel=bevel, segments=2, name=name)
        else:
            sc.box(kit, (size_x, size_n, size_u), P(x, u, n), slot, bevel=bevel, segments=2, name=name)

    # Hardboard with rounded corners.
    outline = sc.rounded_rect(BW, BH, 0.012, 5, 0, 0)
    if wall:
        sc.extrude_xy(kit, [(x, y0 + BH / 2 + y) for x, y in outline], BT, 0.004 + BT / 2, "Prop_Hardboard", bevel=0.0006, segments=1, name="hardboard")
    else:
        # Lying flat: the same outline in x/z, extruded along y.
        import bmesh
        from mathutils import Vector
        bm = bmesh.new()
        top = [bm.verts.new(sc.B(x, BT, -y)) for x, y in outline]
        bot = [bm.verts.new(sc.B(x, 0.0, -y)) for x, y in outline]
        bm.faces.new(top)
        bm.faces.new(list(reversed(bot)))
        n = len(outline)
        for i in range(n):
            j = (i + 1) % n
            bm.faces.new((top[i], top[j], bot[j], bot[i]))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        kit._new_object("hardboard", bm, "Prop_Hardboard", "metres", "xz")
    # Sheets: three under-sheets fanned 2-3 mm, the top sheet with the form.
    for k, (dx, du) in enumerate(((0.002, -0.004), (-0.0015, -0.0025), (0.001, -0.0012))):
        nrm = BT + 0.0003 * (k + 1)
        if wall:
            sc.quad(kit, SW, SH, P(dx, BH / 2 - 0.012 + du - SH / 2 + 0.006, nrm), sc.PAPER, face, uv_rect=sc.paper_rect("sheet_blank"), name="under sheet")
        else:
            sc.quad(kit, SW, SH, P(dx, BH / 2 - 0.012 + du - SH / 2 + 0.006, nrm), sc.PAPER, face, uv_rect=sc.paper_rect("sheet_blank"), name="under sheet")
    sc.quad(kit, SW, SH, P(0.0, BH / 2 - 0.012 - SH / 2 + 0.006, BT + 0.0013), sc.PAPER, face, uv_rect=sc.paper_rect(cell), name="top sheet")
    # Spring clip: base plate with two rivets, rolled clamp bar, wire lever with a hanging hole.
    u_c = BH / 2 - 0.022
    box_l(0.11, 0.030, 0.0012, 0.0, u_c, BT + 0.0006, "Prop_Chrome", bevel=0.0004, name="clip base")
    for rx in (-0.035, 0.035):
        r = sc.cyl(kit, 0.003, 0.002, P(rx, u_c + 0.006, BT + 0.0022), "z" if wall else "y", "Prop_Chrome", verts=12, bevel=0.0005, name="rivet")
    box_l(0.105, 0.010, 0.006, 0.0, u_c - 0.012, BT + 0.004, "Prop_Chrome", bevel=0.0025, name="clamp bar")
    lever = []
    for k in range(9):
        a = math.radians(180 * k / 8)
        lever.append(P(0.03 * math.cos(a), u_c + 0.010 + 0.022 * math.sin(a), BT + 0.007 + 0.003 * math.sin(a)))
    sc.tube(kit, lever, 0.0018, "Prop_Chrome", verts=8, name="clip lever")
    if wall:
        # The nail it hangs on, through the lever's loop.
        sc.cyl(kit, 0.0016, 0.012, (0.0, 1.12 + u_c + 0.028, 0.008), "z", "Prop_Chrome", verts=8, bevel=0.0, name="nail shank")
        sc.cyl(kit, 0.0035, 0.0012, (0.0, 1.12 + u_c + 0.028, 0.0145), "z", "Prop_Chrome", verts=12, bevel=0.0003, name="nail head")
    sc.auto_lod_drops(kit)
    kit.no_collider()
    kit.meta["paperCells"] = {"top_sheet": sc.PAPER_CELLS[cell], "under_sheets": sc.PAPER_CELLS["sheet_blank"]}
    kit.meta["inspect_note"] = "EXAMINE-readable (30L S15); strings 01 §6; art 平面视觉's atlas"


def build(kit):
    build_board(kit, False, "dar")
    kit.tag("desk_top", "station", "paper")
    sc.lods(kit, 3.0, 8.0, 30.0, (1800, 600, 60))
    sc.station_meta(kit, "desk surface under the board's centre", "the Daily Activity Report (01 §6)")
