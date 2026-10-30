"""Kit_RegisterBook: the visitor register, open on the transaction ledge (30L S16; 01 §6 headers
DATE  NAME  COMPANY  VISITING  TIME IN  TIME OUT; the open page is the previous business day's, daytime visitors out
by 1745, then blank rows, the last ruled line blank: that line is you; NN §2; plan 70 P15).

A cloth-bound ledger open flat (0.43 x 0.30 over the boards), the pages curving into the gutter, a page block of
~120 leaves each side, head and tail bands, and a ballpoint pen on a bead chain to a weighted base. Reads from the
visitor's side (front +Z). The two pages are paper atlas cells register_left / register_right (平面视觉's art;
blank until then). EXAMINE only (no text entry). ORIGIN = the ledge surface under the book's centre.
Budget 2,500 / 800 / 80. Slots Prop_Vinyl (cloth binding), Prop_StationPaper, Prop_PlasticBlack, Prop_Chrome.
"""

import math

import bmesh

import _station_common as sc

NAME = "Kit_RegisterBook"
LODS = (0.94, 0.90)   # hard surface: LODs drop sub-pixel parts only (no collapse: it folded boxes into triangles)
SMOOTH_ANGLE = 40.0

CW, CD, CT = 0.43, 0.30, 0.004
BLOCK = 0.012
PW, PD = 0.205, 0.285


def page(kit, side, cell):
    """One curved page surface (side -1 left, +1 right as seen from the front), an 8 x 6 grid with decal UVs."""
    bm = bmesh.new()
    nx, nz = 10, 6
    verts = []
    for j in range(nz + 1):
        row = []
        for i in range(nx + 1):
            t = i / nx
            # x from the gutter (t 0) out to the fore-edge (t 1): the page rises out of the gutter, then lies flat.
            x = side * (0.004 + t * PW)
            y = CT + BLOCK * (1 - math.exp(-t * 9.0)) + 0.0015 * math.sin(math.pi * t)
            z = (j / nz - 0.5) * PD
            row.append(bm.verts.new(sc.B(x, y, z)))
        verts.append(row)
    uv_layer = bm.loops.layers.uv.new()
    u0, v0, u1, v1 = sc.paper_rect(cell)
    for j in range(nz):
        for i in range(nx):
            q = (verts[j][i], verts[j][i + 1], verts[j + 1][i + 1], verts[j + 1][i]) if side > 0 else (verts[j][i + 1], verts[j][i], verts[j + 1][i], verts[j + 1][i + 1])
            f = bm.faces.new(q)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    for f in bm.faces:
        f.normal_update()
        if f.normal.z > 0:  # Blender +z is up; normals should point up
            pass
    obj = kit._new_object("page", bm, sc.PAPER, "keep", "xz")
    # Decal UVs by position (the reader at the front: u along x from the left, v along z from the bottom edge).
    me = obj.data
    uvl = me.uv_layers.active or me.uv_layers.new()
    for poly in me.polygons:
        for li in poly.loop_indices:
            co = me.vertices[me.loops[li].vertex_index].co
            ux = -co.x  # Unity x
            uz = -co.y  # Unity z
            t = (abs(ux) - 0.004) / PW
            uu = (1 - t) if side < 0 else t
            # The left page reads right-to-left from the gutter as seen from the front (-X Unity is the reader's right).
            vv = (uz + PD / 2) / PD
            uvl.data[li].uv = (u0 + (u1 - u0) * uu, v0 + (v1 - v0) * vv)
    return obj


def build(kit):
    sc.register()
    # Boards (cloth over board), open flat, a hollow spine under the gutter.
    for side in (-1, 1):
        sc.box(kit, (CW / 2, CT, CD), (side * CW / 4, CT / 2, 0), "Prop_Vinyl", bevel=0.0014, segments=2, name="board")
    sc.cyl(kit, 0.010, CD, (0, 0.004, 0), "z", "Prop_Vinyl", verts=12, bevel=0.0, name="spine")
    # Page blocks (the stacked leaves), lofted into the gutter.
    for side in (-1, 1):
        prof = []
        for k in range(1, 11):
            t = k / 10
            prof.append((side * (0.004 + t * (PW - 0.004)), CT + 0.0003 + (BLOCK - 0.0009) * (1 - math.exp(-t * 9.0))))
        # Close the profile as a strip between the board and the page curve, extruded along z.
        poly = [(side * 0.004, CT)] + prof + [(prof[-1][0], CT)]
        sc.extrude_xy(kit, poly, PD - 0.004, 0.0, "Prop_StationPaper", bevel=0.0, segments=1, name="page block")
    for side in (-1, 1):
        page(kit, side, "register_left" if side < 0 else "register_right")
    # Head and tail bands.
    for z in (-PD / 2, PD / 2):
        sc.cyl(kit, 0.0025, 0.012, (0, CT + 0.004, z), "x", "Prop_Vinyl", verts=8, bevel=0.0, name="headband")
    # Ballpoint on a bead chain to a weighted base at the right.
    bx, bz = -(CW / 2 + 0.045), 0.06
    kit.lathe([(0.022, 0.0), (0.022, 0.006), (0.017, 0.012), (0.006, 0.016), (0.0, 0.017)], sc.B(bx, 0.0, bz), "Prop_PlasticBlack", verts=28, name="pen base", close_bottom=True)
    pen0 = (-(CW / 4 + 0.02), CT + BLOCK + 0.004, 0.02)
    pen1 = (pen0[0] - 0.13, pen0[1], pen0[2] + 0.03)
    sc.tube(kit, [pen0, pen1], 0.0045, "Prop_PlasticBlack", verts=14, name="pen barrel")
    sc.cyl(kit, 0.0022, 0.012, ((pen0[0] + 0.004), pen0[1], pen0[2] - 0.001), "x", "Prop_Chrome", verts=10, bevel=0.0, name="pen tip")
    chain = []
    for k in range(18):
        t = k / 17
        x = pen1[0] + (bx - pen1[0]) * t
        z = pen1[2] + (bz - pen1[2]) * t
        y = pen1[1] + (0.016 - pen1[1]) * t - 0.01 * math.sin(math.pi * t)
        chain.append((x, max(0.0012, y), z))
    for p in chain[1:-1]:
        kit.lathe([(0.0001, -0.0011), (0.0011, 0.0), (0.0001, 0.0011)], sc.B(*p), "Prop_Chrome", verts=6, name="bead")
    kit.no_collider()
    kit.tag("desk_top", "station", "paper")
    kit.meta["paperCells"] = {"left": sc.PAPER_CELLS["register_left"], "right": sc.PAPER_CELLS["register_right"]}
    kit.meta["inspect_note"] = "EXAMINE only (no text entry); strings 01 §6; art 平面视觉's atlas; the last ruled line is blank"
    sc.auto_lod_drops(kit)
    sc.lods(kit, 3.0, 8.0, 30.0, (2500, 800, 80))
    sc.station_meta(kit, "ledge surface under the book's centre", "reads from the visitor side (+Z)")
