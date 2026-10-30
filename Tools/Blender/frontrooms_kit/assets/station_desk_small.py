"""Shared builders for the station's small desk and wall kits (each kit has its own module below that calls one).

Tent card (30L S22), desk blotter (S39), coffee cup (S24), glass ashtray (S25 A), radio charger (S19), notice board
(S40), heavy hook (S26 as reworded in §10.3), post-orders binder (S17). All 1990, no brand, no invented type: every
printed surface is a paper atlas cell (平面视觉's art; blank until it exists).
"""

import math

import _station_common as sc


def tent_card(kit):
    sc.register()
    w, h, a = 0.20, 0.09, math.radians(18)
    # Two leaves of card stock meeting at the top fold (front leaf faces +Z = the visitor).
    for side in (1, -1):
        zc = side * h / 2 * math.sin(a)
        pts = [(-w / 2, 0.0), (w / 2, 0.0), (w / 2, h * math.cos(a)), (-w / 2, h * math.cos(a))]
        o = kit.quad(w, h, sc.B(0, h * math.cos(a) / 2, zc), sc.PAPER, facing="-y" if side > 0 else "+y",
                     uv="decal", uv_rect=sc.paper_rect("tent_card"), name="tent leaf")
        o.rotation_mode = "XYZ"
        o.rotation_euler = (math.radians(-18 * side), 0, 0)
        # Card thickness: a thin back leaf in the same stock.
        b = kit.quad(w, h, sc.B(0, h * math.cos(a) / 2, zc - side * 0.0004), sc.PAPER, facing="+y" if side > 0 else "-y",
                     uv="decal", uv_rect=sc.paper_rect("sheet_blank"), name="tent back")
        b.rotation_mode = "XYZ"
        b.rotation_euler = (math.radians(-18 * side), 0, 0)
    sc.cyl(kit, 0.0008, w, (0, h * math.cos(a), 0), "x", sc.PAPER, verts=6, bevel=0.0, name="fold")
    sc.auto_lod_drops(kit)
    kit.no_collider()
    kit.tag("desk_top", "station", "paper")
    kit.meta["paperCells"] = {"front": sc.PAPER_CELLS["tent_card"]}
    kit.meta["text"] = "PLEASE SIGN IN (平面视觉's type; blank until the atlas exists)"


def blotter(kit):
    sc.register()
    w, d = 0.56, 0.40
    sc.box(kit, (w, 0.004, d), (0, 0.002, 0), "Prop_Paper", bevel=0.0008, segments=1, name="blotter pad")
    for sx in (-1, 1):
        sc.box(kit, (0.06, 0.006, d + 0.004), (sx * (w / 2 - 0.03), 0.003, 0), "Prop_Vinyl", bevel=0.0018, segments=2, name="vinyl end")
    # Desk glass (6 mm, polished edges) over a typed phone list (no printed date: 01 §5).
    sc.quad(kit, 0.14, 0.20, (0.16, 0.0042, 0.06), sc.PAPER, "+y", uv_rect=sc.paper_rect("phone_list"), name="phone list")
    sc.box(kit, (0.30, 0.006, 0.36), (0.12, 0.0075, 0.0), "Prop_Glass", bevel=0.0015, segments=2, name="desk glass")
    sc.auto_lod_drops(kit)
    kit.no_collider()
    kit.tag("desk_top", "station", "paper")
    kit.meta["paperCells"] = {"phone_list": sc.PAPER_CELLS["phone_list"]}


def flat_ring(kit, centre, r0, r1, slot, name, n=40):
    """A flat annulus facing up (Unity +Y), e.g. a dried ring stain."""
    import bmesh
    bm = bmesh.new()
    inner, outer = [], []
    for k in range(n):
        a = 2 * math.pi * k / n
        inner.append(bm.verts.new(sc.B(centre[0] + r0 * math.cos(a), centre[1], centre[2] + r0 * math.sin(a))))
        outer.append(bm.verts.new(sc.B(centre[0] + r1 * math.cos(a), centre[1], centre[2] + r1 * math.sin(a))))
    for k in range(n):
        j = (k + 1) % n
        f = bm.faces.new((inner[k], outer[k], outer[j], inner[j]))
        f.normal_update()
        if f.normal.z < 0:
            f.normal_flip()
    return kit._new_object(name, bm, slot, "metres", "xz")


def coffee_cup(kit):
    sc.register()
    # 8 oz expanded-foam cup, rolled rim, cold coffee about 2/3 up, a dried ring stain on the blotter beside it.
    prof = [(0.0, 0.0), (0.0255, 0.0), (0.0262, 0.003), (0.0365, 0.090), (0.0395, 0.0935), (0.0398, 0.096), (0.0375, 0.0965), (0.0352, 0.093), (0.0248, 0.004), (0.0, 0.004)]
    kit.lathe(prof, sc.B(0, 0, 0), "Prop_PlasticWhite", verts=40, name="foam cup", close_top=False, close_bottom=False)
    sc.cyl(kit, 0.0312, 0.001, (0, 0.0595, 0), "y", "Prop_Ceramic", verts=40, bevel=0.0, name="cold coffee")
    flat_ring(kit, (0.055, 0.0003, 0.03), 0.0262, 0.0284, "Prop_Ceramic", "ring stain")
    sc.auto_lod_drops(kit)
    kit.no_collider()
    kit.tag("desk_top", "station")


def ashtray(kit):
    sc.register()
    # Pressed-glass round desk ashtray, Ø 0.12, three cigarette rests.
    prof = [(0.0, 0.0), (0.050, 0.0), (0.060, 0.006), (0.060, 0.028), (0.056, 0.030), (0.046, 0.030), (0.040, 0.012), (0.0, 0.010)]
    kit.lathe(prof, sc.B(0, 0, 0), "Prop_Glass", verts=48, name="ashtray", close_top=False, close_bottom=True)
    sc.auto_lod_drops(kit)
    kit.no_collider()
    kit.tag("desk_top", "station")


def radio_charger(kit):
    sc.register()
    # Black drop-in desk charger for a portable radio: one well (EMPTY), a dark charge lamp, a spare battery beside it.
    sc.box(kit, (0.085, 0.045, 0.105), (0, 0.0225, 0), "Prop_PlasticBlack", bevel=0.008, segments=3, name="charger base")
    sc.box(kit, (0.064, 0.04, 0.042), (0, 0.030, -0.012), "Prop_PlasticGrey", bevel=0.004, segments=2, name="well (empty)")
    for x in (-0.012, 0.012):
        sc.box(kit, (0.006, 0.004, 0.006), (x, 0.012, -0.012), "Prop_Chrome", bevel=0.0008, segments=1, name="contact")
    sc.cyl(kit, 0.0035, 0.002, (0.0, 0.034, 0.045), "z", "Prop_PlasticGrey", verts=16, bevel=0.0005, name="charge lamp (dark)")
    sc.box(kit, (0.055, 0.025, 0.020), (0.075, 0.0125, 0.02), "Prop_PlasticBlack", bevel=0.003, segments=2, name="spare battery")
    for x in (0.063, 0.087):
        sc.box(kit, (0.006, 0.002, 0.008), (x, 0.026, 0.02), "Prop_Chrome", bevel=0.0004, segments=1, name="battery contact")
    # Wall-wart lead (short, toward the back).
    sc.tube(kit, sc.smooth_path([(0, 0.01, -0.052), (0.01, 0.003, -0.08), (0.05, 0.003, -0.10)], 4), 0.0018, "Prop_PlasticBlack", verts=6, name="lead")
    sc.auto_lod_drops(kit)
    kit.no_collider()
    kit.tag("desk_top", "station")


def notice_board(kit):
    sc.register()
    w, h, y0 = 0.60, 0.45, 1.20
    yc = y0 + h / 2
    sc.box(kit, (w - 0.02, h - 0.02, 0.008), (0, yc, 0.004), sc.CORK, bevel=0.001, segments=1, name="cork")
    kit.frame((w, h), (w - 0.024, h - 0.024), 0.014, sc.B(0, yc, 0.007), "Prop_Aluminium", bevel=0.0018, segments=2, name="frame")
    notices = (("notice_a", -0.15, 0.06, 0.216 * 0.7, 0.279 * 0.7, 2.5), ("notice_b", 0.12, 0.08, 0.14, 0.10, -2.0),
               ("notice_c", 0.13, -0.10, 0.10, 0.14, 1.5), ("notice_d", -0.17, -0.12, 0.12, 0.08, -1.0))
    for cell, x, y, nw, nh, tilt in notices:
        q = sc.quad(kit, nw, nh, (x, yc + y, 0.0082), sc.PAPER, "+z", uv_rect=sc.paper_rect(cell), name=cell)
        q.rotation_mode = "XYZ"
        q.rotation_euler = (0, math.radians(tilt), 0)
        sc.cyl(kit, 0.004, 0.006, (x, yc + y + nh / 2 - 0.008, 0.011), "z", "Prop_Chrome", verts=12, bevel=0.001, name="push pin")
    sc.auto_lod_drops(kit)
    kit.no_collider()
    kit.tag("wall_decor", "station", "paper")
    kit.meta["paperCells"] = {c: sc.PAPER_CELLS[c] for c in ("notice_a", "notice_b", "notice_c", "notice_d")}


def hook_heavy(kit):
    sc.register()
    # A heavy cast single coat hook (rose + arm), EMPTY of the watchclock; the strap's leather buckle loop left on it.
    y = 1.60
    sc.cyl(kit, 0.022, 0.006, (0, y, 0.003), "z", "Prop_SteelBlack", verts=28, bevel=0.0015, segments=2, name="hook rose")
    for k in range(2):
        sc.cyl(kit, 0.0035, 0.002, (0, y + (0.013 if k == 0 else -0.013), 0.0065), "z", "Prop_Chrome", verts=12, bevel=0.0005, name="rose screw")
    arm = [(0, y, 0.006), (0, y + 0.002, 0.030), (0, y - 0.012, 0.055), (0, y - 0.032, 0.062), (0, y - 0.046, 0.058), (0, y - 0.050, 0.050), (0, y - 0.044, 0.044)]
    sc.tube(kit, sc.smooth_path(arm, 6), 0.0055, "Prop_SteelBlack", verts=14, name="hook arm")
    sc.lathe_y  # noqa
    kit.lathe([(0.0, -0.006), (0.0068, -0.003), (0.0068, 0.002), (0.0, 0.006)], sc.B(0, y - 0.044, 0.044), "Prop_SteelBlack", verts=16, name="hook knob")
    # The buckle loop: the strap's leather end looped over the arm, its buckle hanging below (no cut strap).
    front = [(0, y - 0.040, 0.049), (0, y - 0.028, 0.055), (0, y - 0.031, 0.066), (0, y - 0.046, 0.068), (0, y - 0.078, 0.062), (0, y - 0.108, 0.057)]
    back = [(0, y - 0.040, 0.049), (0, y - 0.072, 0.052), (0, y - 0.108, 0.054)]
    for path in (front, back):
        t = sc.tube(kit, sc.smooth_path(path, 5), 0.0026, "Prop_Vinyl", verts=6, name="strap leather")
        t.scale = (3.8, 1.0, 1.0)
    sc.box(kit, (0.020, 0.024, 0.004), (0.0, y - 0.118, 0.056), "Prop_Chrome", bevel=0.0012, segments=1, name="buckle frame")
    sc.box(kit, (0.014, 0.018, 0.0045), (0.0, y - 0.118, 0.056), "Prop_SteelBlack", bevel=0.0, segments=1, name="buckle opening")
    sc.cyl(kit, 0.0011, 0.020, (0.0, y - 0.118, 0.058), "y", "Prop_Chrome", verts=6, bevel=0.0, name="buckle tongue")
    sc.auto_lod_drops(kit)
    kit.no_collider()
    kit.tag("wall_decor", "station")
    kit.meta["note"] = "30L S26 as reworded (§10.3): an empty heavy hook with the strap's buckle loop left on it; never a cut strap"


def binder(kit):
    sc.register()
    # One 1.5 in three-ring binder lying flat: vinyl covers, the spine label holder with the POST ORDERS card.
    w, d, t = 0.29, 0.30, 0.048
    for y in (0.002, t - 0.002):
        sc.box(kit, (w, 0.003, d), (0, y, 0), "Prop_Vinyl", bevel=0.0012, segments=2, name="cover")
    sc.box(kit, (0.004, t, d), (w / 2 - 0.002, t / 2, 0), "Prop_Vinyl", bevel=0.0014, segments=2, name="spine")
    sc.box(kit, (w - 0.02, t - 0.012, d - 0.01), (-0.005, t / 2, 0), "Prop_Paper", bevel=0.0006, segments=1, name="page block")
    sc.quad(kit, 0.20, 0.034, (w / 2 + 0.0002, t / 2, 0), sc.PAPER, "+x", uv_rect=sc.paper_rect("post_orders_spine"), name="spine card")
    sc.auto_lod_drops(kit)
    kit.no_collider()
    kit.tag("desk_top", "station", "paper")
    kit.meta["paperCells"] = {"spine": sc.PAPER_CELLS["post_orders_spine"]}
    kit.meta["text"] = "POST ORDERS (01 §6; no company name; 平面视觉's type)"
