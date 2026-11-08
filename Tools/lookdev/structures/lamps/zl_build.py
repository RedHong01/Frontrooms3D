"""FRONTROOMSSS zone lamps: new fixture kits for the zone-lamps options (zone-lamps workflow, design + build stage,
2026-10-31). Clone only; nothing here lands in main.

Run (headless, Blender 4.3):
  Blender --background --factory-startup --python zl_build.py -- <outdir> [IDS ...]
Writes <outdir>/ZL_<ID>.fbx, <outdir>/ZL_<ID>_layout.json and <outdir>/zl_build_report.json.

Reuses the lamp study's generator (lamp_options/kit/build_lamps.py, copied to this folder): its mesh accumulator,
profile sweeps, atlas packer, trim sheet rows (LampOpt_Trim, shared by every housing) and FBX export. Same frame:
mm, Blender X = short axis, Y = long axis, Z up; Z = 0 is the mounting plane (ceiling plane for ceiling kits, positive
Z into the ceiling; the wall plane for wall kits, the room toward -Y). Unity sees (x, y, z) = (-x, z, -y).

Contract per kit (as lamp_options/kit/README.md):
  Lens        the ONE renderer the lamp logic drives (_EmissionColor); every glowing part is on it. Submesh 0 is the
              lens material (FrontRooms/Surface + emission map); where a kit lights a ceiling it has a submesh 1 on the
              kit's additive glow material (the lit ceiling patch of an indirect fixture), so it dims and dies with
              the lamp through the same property block.
  Housing_L0/1/2  unlit exterior on LampOpt_Trim, shadows off; LOD switch 4 m / 12 m, culled 46 m.
  Phone       one mesh on the lens material (frame in emission-free swatches) [+ the glow submesh where the kit has one].
No text, label, brand or logo anywhere. ASCII only.

Kits:
  PEND   lensed-indirect pendant, 8 ft, 2 x F40 (Office O-C)                         [10_office F4: O02, O03, O09]
  MHDL   recessed metal-halide downlight, 10 in specular cone, coated lamp (Tall T-A) [10_other 4.4 A: Z03, Z04]
  HIDUP  wall / column HID indirect uplight box (Pool P1-A, Tall T-B)                  [10_pool F2, 10_other Z07]
  MHLB   enclosed metal-halide low-bay, 22 in spun reflector, gasketed lens (Pool P1-B)[10_pool F1]
  VT8    sealed vapour-tight 8 ft 2 x F96T12, prismatic diffuser (Pool P1-C)           [10_pool F4: PL08, PL09]
  NICHE  wet-niche underwater lamp face ring, PAR56 300 W (Pool P2-A / B)              [10_pool F6: PL02, PL07, PL08]
  PORT   dry-niche viewing port with its lamp behind (Pool P2-C)                       [10_pool F6: PL09]
  STRIP  bare 4 ft 2 x F40 strip with a wire guard (Run R-B)                           [10_run F2 + F4]
  INDR   4 ft 2 x F40 strip with a white enamel industrial reflector, stem hung (Run R-C) [10_run F3]
  RPLATE remote twin-head mount (junction box + bracket) for the existing heads (Run tripped T-B) [10_run E2, R04]
"""
import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
_src = open(os.path.join(HERE, "build_lamps.py")).read()
exec(compile(_src[:_src.index("\ndef main():")], "build_lamps_helpers", "exec"))

ZL_ONLY = set(ARGV[1:])
# ZL_SPLIT_GLOW=1: the pendant's ceiling patch as its own object (the main landing); unset: as the option renders
SPLIT_GLOW = os.environ.get("ZL_SPLIT_GLOW") == "1"
REPORT = {}


def kit_uv_swatch(at, finish):
    return lambda *a: at.uv("sw_" + finish, 0.5, 0.5)


def tri_mapper(finish):
    """trim row for a sweep: along -> U, profile t -> V"""
    return lambda side, along, t, tt: trim_uv(finish, along, t, tt, side)


def round_mapper(finish, r):
    return lambda a, t, T: trim_uv(finish, a * 2 * math.pi * r, t, T)


def cyl_mapper(finish, r):
    return lambda a, s, l: trim_uv(finish, s, a * 2 * math.pi * r, 2 * math.pi * r)


def ring_disk(acc, cx, cy, z, r0, r1, n, finish, down=True):
    """flat annulus between r0 and r1 at z, facing down (or up)"""
    want = (0, 0, -1 if down else 1)
    for k in range(n):
        a0, a1 = 2 * math.pi * k / n, 2 * math.pi * (k + 1) / n
        pts = [(cx + r0 * math.cos(a0), cy + r0 * math.sin(a0), z), (cx + r0 * math.cos(a1), cy + r0 * math.sin(a1), z),
               (cx + r1 * math.cos(a1), cy + r1 * math.sin(a1), z), (cx + r1 * math.cos(a0), cy + r1 * math.sin(a0), z)]
        quad(acc, pts, lambda p: trim_uv(finish, math.atan2(p[1] - cy, p[0] - cx) * r1 + 400, math.hypot(p[0] - cx, p[1] - cy), r1 + 1), want)


def section_extrude(acc, prof, y0, y1, uvf, closed=False, mat=0, smooth_all=False):
    """Extrude a 2D (x, z) section along Y from y0 to y1. Normal of segment i: right-hand rule with the
    section traversed counter-clockwise seen from +Y gives outward faces; we use 'want' = (dz, 0, -dx) for a
    profile traversed so that the visible side is to its right (as sweep_line). uvf(t, along, ttot)."""
    pts = [p for p, _ in prof]
    sm = [s for _, s in prof]
    n = len(pts)
    t = [0.0]
    for i in range(1, n):
        t.append(t[-1] + math.hypot(pts[i][0] - pts[i - 1][0], pts[i][1] - pts[i - 1][1]))
    if closed:
        tt = t[-1] + math.hypot(pts[0][0] - pts[-1][0], pts[0][1] - pts[-1][1])
    else:
        tt = t[-1]
    rows = [(acc.v((x, y0, z)), acc.v((x, y1, z))) for (x, z) in pts]
    segs = range(n if closed else n - 1)
    for i in segs:
        j = (i + 1) % n
        dx = pts[j][0] - pts[i][0]
        dz = pts[j][1] - pts[i][1]
        if abs(dx) + abs(dz) < 1e-9:
            continue
        want = (dz, 0, -dx)
        tj = t[j] if j > 0 else tt
        uv = [uvf(t[i], y0, tt), uvf(t[i], y1, tt), uvf(tj, y1, tt), uvf(tj, y0, tt)]
        acc.f([rows[i][0], rows[i][1], rows[j][1], rows[j][0]], uv, smooth_all or sm[j], want, mat)
    return rows


def cap_polygon(acc, pts2, y, outward_y, uvf, mat=0):
    """flat cap polygon (x, z) at y, facing +/-Y; fan from centroid (convex-ish sections only)."""
    cx = sum(p[0] for p in pts2) / len(pts2)
    cz = sum(p[1] for p in pts2) / len(pts2)
    c = acc.v((cx, y, cz))
    ids = [acc.v((x, y, z)) for (x, z) in pts2]
    for k in range(len(ids)):
        k1 = (k + 1) % len(ids)
        tri = [c, ids[k], ids[k1]]
        acc.f(tri, [uvf(acc.V[i]) for i in tri], False, (0, outward_y, 0), mat)


def egg_profile(r_max, h, segs=10, base_r=None):
    """Lamp envelope (ED / BT) as an (r, z_from_tip) list from the tip (r ~0) to the neck: an ellipsoid bulb over
    the lower 80 % of the height, then a short neck tapering to the base."""
    base_r = base_r or r_max * 0.35
    zc, az = 0.42 * h, 0.42 * h
    out = []
    nb = max(3, int(segs * 0.75))
    for k in range(nb + 1):
        t = k / nb
        th = math.pi * t * 0.86     # 0 .. ~155 deg round the ellipse from the tip
        z = zc - az * math.cos(th)
        r = r_max * math.sin(th)
        out.append((max(r, 0.01), z))
    zl, rl = out[-1][1], out[-1][0]
    nn = max(2, segs - nb)
    for k in range(1, nn + 1):
        t = k / nn
        out.append((rl + (base_r - rl) * t ** 0.7, zl + (h - zl) * t))
    return out


def lathe(acc, prof_rz, n, uvf, cx=0.0, cy=0.0, z0=0.0, zsign=1.0, ref=None, outward=True, mat=0):
    """Revolve an (r, z) profile about the vertical axis at (cx, cy); actual z = z0 + zsign * z. uvf(a, v).
    Normals point away from ref (r, z in profile space) when given, else follow (dz, -dr) (flipped if not outward)."""
    rings = []
    for (r, z) in prof_rz:
        zz = z0 + zsign * z
        rings.append([acc.v((cx + r * math.cos(2 * math.pi * k / n), cy + r * math.sin(2 * math.pi * k / n), zz)) for k in range(n)])
    m = len(prof_rz)
    for i in range(m - 1):
        dr = prof_rz[i + 1][0] - prof_rz[i][0]
        dz = prof_rz[i + 1][1] - prof_rz[i][1]
        nr, nz = dz, -dr
        rm = (prof_rz[i + 1][0] + prof_rz[i][0]) / 2
        zm = (prof_rz[i + 1][1] + prof_rz[i][1]) / 2
        if ref is not None:
            if (rm - ref[0]) * nr + (zm - ref[1]) * nz < 0:
                nr, nz = -nr, -nz
        elif not outward:
            nr, nz = -nr, -nz
        for k in range(n):
            k1 = (k + 1) % n
            am = 2 * math.pi * (k + 0.5) / n
            ids = [rings[i][k], rings[i][k1], rings[i + 1][k1], rings[i + 1][k]]
            nn = (math.cos(am) * nr, math.sin(am) * nr, nz * zsign)
            v0, v1 = i / (m - 1), (i + 1) / (m - 1)
            uv = [uvf(k / n, v0), uvf((k + 1) / n, v0), uvf((k + 1) / n, v1), uvf(k / n, v1)]
            acc.f(ids, uv, True, nn, mat)
    return rings


def screw_head(acc, x, y, z, r, normal_down=True, finish="zinc", n=8):
    """a pan / slotted head as a low dome facing -z"""
    disk(acc, x, y, z, r, n, lambda p: trim_uv(finish, p[0] + 500, (p[1] % 40) + 30, 100), down=normal_down, dome=r * 0.45, rings=1)


def emit(name, objs_accs, layout):
    """objs_accs: [(acc, object name, [material names])]"""
    reset()
    coll = bpy.context.scene.collection
    objs, rep = [], {}
    for acc, oname, mats in objs_accs:
        ob, up = to_object(acc, oname, mats, coll)
        objs.append(ob)
        rep[oname] = tri_count(ob)
    export(objs, os.path.join(OUT, name + ".fbx"))
    json.dump(layout, open(os.path.join(OUT, name + "_layout.json"), "w"), indent=1)
    REPORT[name] = rep
    print("BUILT", name, " ".join("%s=%d" % kv for kv in rep.items()))


SEGS = {0: 6, 1: 2, 2: 0}


# =========================================================================== PEND
PEND = dict(length=2438.0, top=-500.0, height=100.0, half_w=127.0, lens_h=40.0, cable_y=900.0, cord_y=700.0,
            glow=(3400.0, 2200.0, -2.0), tubes=[-38.0, 38.0])


def pend_section(lod):
    """half profile (x >= 0) of the extruded aluminium body, from the bottom centre round to the lens seat"""
    p = PEND
    zb = p["top"] - p["height"]
    hw = p["half_w"]
    z_lens0 = p["top"] - p["lens_h"] - 5.0
    pts = [(0.0, zb), (hw - 30.0, zb), (hw, zb + 32.0), (hw, z_lens0)]
    return fillet(pts, [0, 26.0, 10.0, 0], SEGS[lod]) if lod < 2 else plain(pts)


def pend_atlas():
    at = Atlas(1024, 512)
    at.put("sidelens", 0, 0, 1024, 120, kind="sidelens", length=PEND["length"])
    at.put("sidelens_b", 0, 126, 1024, 120, kind="sidelens", length=PEND["length"])
    at.put("toplens", 0, 252, 1024, 200, kind="toplens", length=PEND["length"], tubes=PEND["tubes"], width=2 * PEND["half_w"] - 14)
    add_swatches(at, ["enamel", "alu", "black"], 1024 - SWATCH)
    return at


def pend_lens(at, phone=False, glow=True):
    acc = Acc()
    p = PEND
    L2 = p["length"] / 2
    hw = p["half_w"]
    z0 = p["top"] - p["lens_h"] - 5.0
    z1 = p["top"] - 5.0
    for sx in (1, -1):
        # side lens: 40 mm strip leaning 8 mm outward, external prisms in the normal map
        a, b = (sx * hw, -L2 + 6), (sx * hw, L2 - 6)
        x0, x1 = sx * hw, sx * (hw + 8.0)
        reg = "sidelens" if sx > 0 else "sidelens_b"
        rows = [(acc.v((x0, -L2 + 6, z0)), acc.v((x0, L2 - 6, z0))), (acc.v((x1, -L2 + 6, z1)), acc.v((x1, L2 - 6, z1)))]
        uv = [at.uv(reg, 0, 0), at.uv(reg, 1, 0), at.uv(reg, 1, 1), at.uv(reg, 0, 1)]
        acc.f([rows[0][0], rows[0][1], rows[1][1], rows[1][0]], uv, False, (sx, 0, -0.2))
    # top lens: flat clear prismatic acrylic, seen only from above (and in reflections)
    tw = hw - 7.0
    quad(acc, [(-tw, -L2 + 6, z1 + 1), (tw, -L2 + 6, z1 + 1), (tw, L2 - 6, z1 + 1), (-tw, L2 - 6, z1 + 1)],
         lambda q: at.uv("toplens", (q[1] + L2) / p["length"], (q[0] + tw) / (2 * tw)), (0, 0, 1))
    if phone:
        # body as one shell in the enamel swatch
        prof = pend_section(2)
        full = [(-x, z) for (x, z), _ in reversed(prof)] + [(x, z) for (x, z), _ in prof]
        section_extrude(acc, plain(full), -L2, L2, lambda t, y, tt: at.uv("sw_enamel", .5, .5))
    # the lit ceiling: one quad on the glow material, 2 mm under the ceiling plane
    if glow:
        pend_glow_quad(acc, 1)
    return acc


def pend_glow_quad(acc, mat):
    p = PEND
    gx, gy, gz = p["glow"][1] / 2, p["glow"][0] / 2, p["glow"][2]
    ids = [acc.v((-gx, -gy, gz)), acc.v((gx, -gy, gz)), acc.v((gx, gy, gz)), acc.v((-gx, gy, gz))]
    acc.f(ids, [(0, 0), (1, 0), (1, 1), (0, 1)], False, (0, 0, -1), mat)
    return acc


def pend_housing(lod):
    acc = Acc()
    p = PEND
    L2 = p["length"] / 2
    hw = p["half_w"]
    prof = pend_section(lod)
    full = [(-x, z) for (x, z), _ in reversed(prof)] + [(x, z) for (x, z), _ in prof]
    sm = [s for _, s in reversed(prof)] + [s for _, s in prof]
    # traverse so the visible side (outside, down) is to the right: from +x side top down round to -x side top
    seq = list(zip(full, sm))
    section_extrude(acc, [(q, s) for q, s in seq], -L2, L2, lambda t, y, tt: trim_uv("enamel", y + L2, t, tt))
    # top rails: aluminium lips holding the top lens (6 x 5)
    for sx in (1, -1):
        x0 = sx * (hw + 8.0)
        box(acc, (sx * (hw + 1.0), 0, p["top"] - 2.5), (16.0, p["length"] - 8, 5.0), box_trim("alu"), skip=())
    # end caps: die-cast, the whole section + lens height, 5 mm thick
    zb = p["top"] - p["height"]
    cap = [(-hw - 9, p["top"])] + [(x, z) for (x, z) in full] + [(hw + 9, p["top"])]
    cap = [(x * 1.02, z - (1.5 if z < zb + 1 else 0.0)) for (x, z) in cap]
    ccx = sum(q[0] for q in cap) / len(cap)
    ccz = sum(q[1] for q in cap) / len(cap)
    for sy in (1, -1):
        y = sy * (L2 + 5.0)
        cap_polygon(acc, cap, y, sy, lambda v: trim_uv("alu", v[0] + 600, v[2] - zb, p["height"] + 10))
        for i in range(len(cap)):
            a2, b2 = cap[i], cap[(i + 1) % len(cap)]
            q = [(a2[0], y - sy * 5.0, a2[1]), (b2[0], y - sy * 5.0, b2[1]), (b2[0], y, b2[1]), (a2[0], y, a2[1])]
            nx, nz = (b2[1] - a2[1]), -(b2[0] - a2[0])
            mx, mz = (a2[0] + b2[0]) / 2 - ccx, (a2[1] + b2[1]) / 2 - ccz
            if nx * mx + nz * mz < 0:
                nx, nz = -nx, -nz
            quad(acc, q, lambda v: trim_uv("alu", v[0] + 700, abs(v[1]) % 10, 10), (nx, 0, nz))
    if lod == 2:
        return acc
    n_c = 6 if lod == 0 else 4
    for sy in (1, -1):
        y = sy * p["cable_y"]
        # aircraft cable 1.6 mm (drawn 3 mm), gripper, saddle, canopy
        cylinder(acc, (0, y, p["top"] + 6), (0, y, -14.0), 1.5, n_c, cyl_mapper("zinc", 1.5))
        if lod == 0:
            cylinder(acc, (0, y, -14.0), (0, y, -48.0), 4.5, 8, cyl_mapper("zinc", 4.5), cap0=True)
            box(acc, (0, y, p["top"] + 3), (30.0, 18.0, 6.0), box_trim("alu"), skip=("-z",))
        lathe(acc, [(45.0, 0.0), (44.0, -4.0), (40.0, -11.0), (6.0, -14.0)], 16 if lod == 0 else 8,
              lambda a, v: trim_uv("enamel", a * 280, v * 40, 40), cx=0, cy=y, z0=0.0, zsign=1.0, ref=(0.0, 5.0))
    # power cord with its canopy (white, 8 mm)
    yc = p["cord_y"] * 0.0 + p["cord_y"]
    cylinder(acc, (30.0, yc, p["top"] + 2), (30.0, yc, -16.0), 4.0, n_c, cyl_mapper("holder", 4.0))
    lathe(acc, [(60.0, 0.0), (59.0, -5.0), (54.0, -16.0), (8.0, -18.0)], 16 if lod == 0 else 8,
          lambda a, v: trim_uv("enamel", a * 377, v * 40, 40), cx=30.0, cy=yc, z0=0.0, zsign=1.0, ref=(0.0, 5.0))
    if lod == 0:
        # strain relief bushing on the body top
        cylinder(acc, (30.0, yc, p["top"] - 1), (30.0, yc, p["top"] + 14), 9.0, 10, cyl_mapper("black", 9.0), cap1=True)
        # body joint lines every 4 ft? (8 ft single extrusion: none). Lens retainer clips every 600 mm
        for k in range(-1, 2):
            for sx in (1, -1):
                box(acc, (sx * (hw + 6.0), k * 600.0, p["top"] - 3.0), (6.0, 20.0, 8.0), box_trim("alu"), skip=())
    return acc


def build_pend():
    at = pend_atlas()
    if SPLIT_GLOW:
        # main landing (2026-11-03): the ceiling patch is its own object ("Glow", one material), so a renderer with the
        # additive material first (GlassRT does not trace transparents) carries it; the lens and the phone mesh carry no glow
        objs = [(pend_lens(at, glow=False), "Lens", ["ZL_PEND_Lens"]), (pend_glow_quad(Acc(), 0), "Glow", ["ZL_PEND_Glow"])]
        for lod in (0, 1, 2):
            objs.append((pend_housing(lod), "Housing_L%d" % lod, ["LampOpt_Trim"]))
        objs.append((pend_lens(at, phone=True, glow=False), "Phone", ["ZL_PEND_Lens"]))
        emit("ZL_PEND", objs, dict(id="PEND", spec=PEND, atlas=at.json(), split_glow=True))
        return
    lens = pend_lens(at)
    ph = pend_lens(at, phone=True)
    objs = [(lens, "Lens", ["ZL_PEND_Lens", "ZL_PEND_Glow"])]
    for lod in (0, 1, 2):
        objs.append((pend_housing(lod), "Housing_L%d" % lod, ["LampOpt_Trim"]))
    objs.append((ph, "Phone", ["ZL_PEND_Lens", "ZL_PEND_Glow"]))
    emit("ZL_PEND", objs, dict(id="PEND", spec=PEND, atlas=at.json()))


# =========================================================================== MHDL
MHDL = dict(aperture=254.0, flange_od=330.0, hole=280.0, cone_top_r=72.0, depth=235.0, lamp_r=45.0, lamp_h=175.0, lamp_tip=78.0)


def mhdl_atlas():
    at = Atlas(1024, 1024)
    at.put("cone", 0, 0, 1024, 420, kind="cone_spec")
    at.put("egg", 0, 426, 512, 300, kind="egg")
    at.put("plate", 518, 426, 200, 200, kind="plate")
    at.put("quartz", 724, 426, 120, 60, kind="quartz")
    at.put("ring", 518, 632, 300, 40, kind="dark")
    add_swatches(at, ["enamel", "rim_alu", "black"], 1024 - SWATCH)
    return at


def mhdl_cone_prof(n=6):
    s = MHDL
    ra, rt, D = s["aperture"] / 2, s["cone_top_r"], s["depth"]
    out = []
    for k in range(n + 1):
        u = k / n
        r = ra - (ra - rt) * (u ** 1.35)
        out.append((r, 6.0 + (D - 6.0) * u))
    return out


def mhdl_lens(at, phone=False):
    acc = Acc()
    s = MHDL
    n = 24 if not phone else 12
    prof = mhdl_cone_prof(5 if not phone else 3)
    # cone interior, seen from below: inward normals
    lathe(acc, prof, n, lambda a, v: at.uv("cone", a, v), ref=(s["aperture"], s["depth"] / 2))
    # socket plate at the top
    disk(acc, 0, 0, s["depth"], s["cone_top_r"], n, lambda p: at.uv("plate", (p[0] / s["cone_top_r"] + 1) / 2, (p[1] / s["cone_top_r"] + 1) / 2), down=True)
    # coated ED-28 lamp, base up: tip at lamp_tip above the aperture, neck into the socket
    eg = egg_profile(s["lamp_r"], s["depth"] - s["lamp_tip"], 9 if not phone else 5, base_r=14.0)
    lathe(acc, eg, 16 if not phone else 8, lambda a, v: at.uv("egg", a, v), z0=s["lamp_tip"], zsign=1.0, ref=(0.0, (s["depth"] - s["lamp_tip"]) / 2))
    if not phone:
        # standby quartz capsule beside the socket (dark unless the HID is out: not driven in this study)
        cylinder(acc, (42.0, 0, s["depth"] - 8), (42.0, 0, s["depth"] - 58), 5.0, 8, lambda a, sx, l: at.uv("quartz", a, sx / l), cap1=True)
    else:
        ring_r0, ring_r1 = s["aperture"] / 2, s["flange_od"] / 2
        for k in range(n):
            a0, a1 = 2 * math.pi * k / n, 2 * math.pi * (k + 1) / n
            pts = [(ring_r0 * math.cos(a0), ring_r0 * math.sin(a0), -3), (ring_r0 * math.cos(a1), ring_r0 * math.sin(a1), -3),
                   (ring_r1 * math.cos(a1), ring_r1 * math.sin(a1), -3), (ring_r1 * math.cos(a0), ring_r1 * math.sin(a0), -3)]
            quad(acc, pts, lambda p: at.uv("sw_enamel", .5, .5), (0, 0, -1))
    return acc


def mhdl_housing(lod):
    acc = Acc()
    s = MHDL
    ra, rf = s["aperture"] / 2, s["flange_od"] / 2
    n = {0: 48, 1: 24, 2: 12}[lod]
    w = rf - ra
    if lod == 2:
        prof = plain([(0.0, 6.0), (0.0, -3.0), (w, -3.0)])
    else:
        prof = fillet([(0.0, 8.0), (0.0, -3.0), (w - 3.0, -3.0), (w, -1.0), (w, 2.0)], [0, 1.2, 2.5, 0, 0], SEGS[lod])
    sweep_round(acc, ra, prof, n, lambda a, t, T: trim_uv("enamel", a * 2 * math.pi * rf, t, T))
    if lod < 2:
        # lens retaining ring (glass guard frame) just inside the aperture
        sweep_round(acc, ra - 6.0, plain([(0.0, 14.0), (0.0, 8.0), (6.0, 8.0)]), n, lambda a, t, T: trim_uv("rim_alu", a * 800, t, T))
    if lod == 0:
        # three retaining screws in the ring, a tiny gap shadow line between flange and ring
        for k in range(3):
            a = 2 * math.pi * k / 3 + 0.3
            screw_head(acc, (ra - 3.0) * math.cos(a), (ra - 3.0) * math.sin(a), 13.0, 3.0, finish="zinc")
    return acc


def build_mhdl():
    at = mhdl_atlas()
    objs = [(mhdl_lens(at), "Lens", ["ZL_MHDL_Lens"])]
    for lod in (0, 1, 2):
        objs.append((mhdl_housing(lod), "Housing_L%d" % lod, ["LampOpt_Trim"]))
    objs.append((mhdl_lens(at, phone=True), "Phone", ["ZL_MHDL_Lens"]))
    # the round ceiling hole panel (1.2 x 1.8 m, 2 x 3 tiles) like LampOpt_CeilingCuts
    cut = Acc()
    R, OX, OY, nn = s_hole = (MHDL["hole"] / 2, 600.0, 900.0, 40)
    sq = []
    for k in range(nn):
        a = 2 * math.pi * k / nn
        c, si = math.cos(a), math.sin(a)
        t = min(OX / max(abs(c), 1e-6), OY / max(abs(si), 1e-6))
        sq.append((t * c, t * si))
    uvf = lambda p: (p[0] / 1000.0, p[1] / 1000.0)
    for k in range(nn):
        k1 = (k + 1) % nn
        a0, a1 = 2 * math.pi * k / nn, 2 * math.pi * k1 / nn
        pts = [(R * math.cos(a0), R * math.sin(a0), 0), (R * math.cos(a1), R * math.sin(a1), 0), (sq[k1][0], sq[k1][1], 0), (sq[k][0], sq[k][1], 0)]
        quad(cut, pts, uvf, (0, 0, -1))
    sweep_round(cut, R, plain([(0.0, 16.0), (0.0, 0.0)]), nn, lambda a, t, T: (a, t / 1000.0))
    objs.append((cut, "CeilingCut_MH", ["Ceiling"]))
    emit("ZL_MHDL", objs, dict(id="MHDL", spec=MHDL, atlas=at.json()))


# =========================================================================== HIDUP
# wall kit: origin on the wall at the box's mounting centre; room toward -Y; Z up.
HIDUP = dict(w=460.0, proj=300.0, top=190.0, bottom=-150.0, glass_inset=16.0, plate=(380.0, 10.0, 440.0),
             glow_pool=(4200.0, 3200.0, 1994.0, 1150.0), glow_tall=(3000.0, 2600.0, 1794.0, 950.0))


def hidup_side_section(lod):
    """(y, z) outline of the pod side (y negative = into the room)"""
    s = HIDUP
    P, T, B = s["proj"], s["top"], s["bottom"]
    pts = [(-P, T), (-P, T - 42.0), (-0.55 * P, -40.0), (-20.0, B)]
    return fillet(pts, [0, 30.0, 60.0, 0], SEGS[lod]) if lod < 2 else plain(pts)


def hidup_atlas():
    at = Atlas(512, 512)
    at.put("glass", 0, 0, 512, 360, kind="glass_top")
    at.put("edge", 0, 366, 512, 40, kind="glass_edge")
    add_swatches(at, ["alu", "black", "enamel"], 512 - SWATCH)
    return at


def hidup_lens(at, glow, phone=False):
    acc = Acc()
    s = HIDUP
    hx = s["w"] / 2 - s["glass_inset"]
    y0, y1 = -20.0 - s["glass_inset"], -s["proj"] + s["glass_inset"]
    zt = s["top"] - 3.0
    # top glass (faces up)
    quad(acc, [(-hx, y0, zt), (hx, y0, zt), (hx, y1, zt), (-hx, y1, zt)],
         lambda q: at.uv("glass", (q[0] + hx) / (2 * hx), (q[1] - y1) / (y0 - y1)), (0, 0, 1))
    if phone:
        prof = hidup_side_section(2)
        pts = [p for p, _ in prof]
        # underside as one strip loop
        for i in range(len(pts) - 1):
            (ya, za), (yb, zb) = pts[i], pts[i + 1]
            quad(acc, [(-s["w"] / 2, ya, za), (s["w"] / 2, ya, za), (s["w"] / 2, yb, zb), (-s["w"] / 2, yb, zb)],
                 lambda q: at.uv("sw_alu", .5, .5), (0, -(zb - za), (yb - ya)))
    # the lit ceiling patch: glow quad (gx along the wall, gy out from it), centred gyc out, at gz above the mount
    gx, gy, gz, gyc = glow
    ids = [acc.v((-gx / 2, -gyc + gy / 2, gz)), acc.v((gx / 2, -gyc + gy / 2, gz)), acc.v((gx / 2, -gyc - gy / 2, gz)), acc.v((-gx / 2, -gyc - gy / 2, gz))]
    acc.f(ids, [(0, 1), (1, 1), (1, 0), (0, 0)], False, (0, 0, -1), 1)
    return acc


def hidup_housing(lod):
    acc = Acc()
    s = HIDUP
    hw = s["w"] / 2
    prof = hidup_side_section(lod)
    pts = [p for p, _ in prof]
    sm = [q for _, q in prof]
    # pod shell: sweep the side outline across X (visible side = outside/below)
    rows = []
    for (y, z) in pts:
        rows.append((acc.v((-hw, y, z)), acc.v((hw, y, z))))
    tlen = 0.0
    for i in range(len(pts) - 1):
        (ya, za), (yb, zb) = pts[i], pts[i + 1]
        seg = math.hypot(yb - ya, zb - za)
        want = (0, -(zb - za), (yb - ya))   # outward for this traversal (top inner -> out -> down -> wall)
        want = (0, (zb - za) * -1.0, (yb - ya) * 1.0)
        uv = [trim_uv("black", 0, tlen, 900), trim_uv("black", s["w"], tlen, 900), trim_uv("black", s["w"], tlen + seg, 900), trim_uv("black", 0, tlen + seg, 900)]
        acc.f([rows[i][0], rows[i][1], rows[i + 1][1], rows[i + 1][0]], uv, sm[i + 1], _pod_normal(ya, za, yb, zb))
        tlen += seg
    # side plates (x = +/- hw): the outline closed by the wall edge up to the top
    outline = pts + [(-20.0, s["top"])]
    ocy = sum(q[0] for q in outline) / len(outline)
    ocz = sum(q[1] for q in outline) / len(outline)
    for sx in (1, -1):
        c = acc.v((sx * hw, ocy, ocz))
        ids = [acc.v((sx * hw, y, z)) for (y, z) in outline]
        for k in range(len(ids)):
            tri = [c, ids[k], ids[(k + 1) % len(ids)]]
            acc.f(tri, [trim_uv("black", acc.V[q][1] + 300, acc.V[q][2] + 200, 600) for q in tri], False, (sx, 0, 0))
    # inner liner just under the glass: the reflector's dark back seen past the rim at grazing (black)
    # glass rim: 16 mm aluminium frame round the top glass
    gi = s["glass_inset"]
    y0, y1 = -20.0, -s["proj"]
    zt = s["top"]
    for (xa, ya, xb, yb) in ((-hw, y1, hw, y1 + gi), (-hw, y0 - gi, hw, y0), (-hw, y1 + gi, -hw + gi, y0 - gi), (hw - gi, y1 + gi, hw, y0 - gi)):
        box(acc, ((xa + xb) / 2, (ya + yb) / 2, zt + 2.0), (xb - xa, yb - ya, 4.0), box_trim("alu"), skip=("-z",))
    # mounting plate on the wall + ballast box with fins below the pod
    pw, pd, ph = s["plate"]
    box(acc, (0, -pd / 2, 20.0), (pw, pd, ph), box_trim("alu"), skip=("+y",))
    if lod < 2:
        box(acc, (0, -10.0 - 55.0, -205.0), (300.0, 110.0, 120.0), box_trim("alu"), skip=("+y",))
    if lod == 0:
        for k in range(9):
            x = -140.0 + 35.0 * k
            box(acc, (x, -10.0 - 55.0, -270.0), (3.0, 104.0, 14.0), box_trim("alu"), skip=("+y", "+z"))
        # ribs on the sloped underside
        for k in range(5):
            x = -hw + 40.0 + k * (s["w"] - 80.0) / 4
            for i in range(len(pts) - 1):
                (ya, za), (yb, zb) = pts[i], pts[i + 1]
                if za > s["top"] - 50 and zb > s["top"] - 50:
                    continue
                nn = _pod_normal(ya, za, yb, zb)
                off = 5.0
                qa = (x - 2.5, ya + nn[1] * off, za + nn[2] * off)
                qb = (x + 2.5, yb + nn[1] * off, zb + nn[2] * off)
                quad(acc, [(x - 2.5, ya + nn[1] * off, za + nn[2] * off), (x + 2.5, ya + nn[1] * off, za + nn[2] * off),
                           (x + 2.5, yb + nn[1] * off, zb + nn[2] * off), (x - 2.5, yb + nn[1] * off, zb + nn[2] * off)],
                     lambda q: trim_uv("black", q[1] + 800, q[2] + 400, 900), nn)
                for sx in (1, -1):
                    quad(acc, [(x + sx * 2.5, ya, za), (x + sx * 2.5, yb, zb), (x + sx * 2.5, yb + nn[1] * off, zb + nn[2] * off), (x + sx * 2.5, ya + nn[1] * off, za + nn[2] * off)],
                         lambda q: trim_uv("black", q[1] + 800, q[2] + 400, 900), (sx, 0, 0))
        # frame screws on the rim (8)
        for k in range(4):
            x = -hw + gi / 2 + k * (s["w"] - gi) / 3
            for yy in (y1 + gi / 2, y0 - gi / 2):
                disk(acc, x, yy, zt + 4.2, 3.0, 6, lambda p: trim_uv("zinc", p[0] + 500, 50, 100), down=False, dome=-1.0)
        # four wall bolts on the plate
        for (x, z) in ((-pw / 2 + 25, 20 + ph / 2 - 25), (pw / 2 - 25, 20 + ph / 2 - 25), (-pw / 2 + 25, 20 - ph / 2 + 25), (pw / 2 - 25, 20 - ph / 2 + 25)):
            cylinder(acc, (x, -pd, z), (x, -pd - 6.0, z), 7.0, 6, cyl_mapper("zinc", 7.0), cap1=True)
    if lod < 2:
        # EMT stub from the ballast box down 300 mm (feed from below) with a connector
        cylinder(acc, (110.0, -38.0, -265.0), (110.0, -38.0, -620.0), 11.0, 10 if lod == 0 else 6, cyl_mapper("zinc", 11.0), cap1=True)
        if lod == 0:
            cylinder(acc, (110.0, -38.0, -262.0), (110.0, -38.0, -290.0), 14.0, 6, cyl_mapper("zinc", 14.0))
    return acc


def _pod_normal(ya, za, yb, zb):
    # outward normal of the pod outline in (y, z): the outline runs top-inner -> top-outer -> down -> wall bottom;
    # the room is at -Y and below, so outward = rotate the tangent by -90 deg in (y, z)
    ty, tz = yb - ya, zb - za
    L = math.hypot(ty, tz) or 1.0
    return (0.0, tz / L, -ty / L)


def build_hidup():
    at = hidup_atlas()
    objs = [(hidup_lens(at, HIDUP["glow_pool"]), "Lens", ["ZL_HIDUP_Lens", "ZL_HIDUP_Glow"]),
            (hidup_lens(at, HIDUP["glow_tall"]), "Lens_Tall", ["ZL_HIDUP_Lens", "ZL_HIDUP_Glow"])]
    for lod in (0, 1, 2):
        objs.append((hidup_housing(lod), "Housing_L%d" % lod, ["LampOpt_Trim"]))
    objs.append((hidup_lens(at, HIDUP["glow_pool"], phone=True), "Phone", ["ZL_HIDUP_Lens", "ZL_HIDUP_Glow"]))
    objs.append((hidup_lens(at, HIDUP["glow_tall"], phone=True), "Phone_Tall", ["ZL_HIDUP_Lens", "ZL_HIDUP_Glow"]))
    emit("ZL_HIDUP", objs, dict(id="HIDUP", spec=HIDUP, atlas=at.json()))


# =========================================================================== MHLB
MHLB = dict(lens_z=-600.0, mouth_r=280.0, neck_r=85.0, neck_z=-380.0, ballast=(150.0, -210.0, -360.0), stem_r=13.5,
            lamp_r=58.0, lamp_tip=-585.0, lamp_top=-395.0)


def mhlb_atlas():
    at = Atlas(1024, 1024)
    at.put("refl", 0, 0, 1024, 520, kind="refl_inner")
    at.put("egg", 0, 526, 512, 300, kind="egg")
    at.put("plate", 518, 526, 200, 200, kind="plate")
    add_swatches(at, ["alu", "black", "rim_alu"], 1024 - SWATCH)
    return at


def mhlb_bell(n_rows=8):
    s = MHLB
    out = []
    for k in range(n_rows + 1):
        u = k / n_rows                     # 0 at the neck, 1 at the mouth
        r = s["neck_r"] + (s["mouth_r"] - s["neck_r"]) * (u ** 0.62)
        z = s["neck_z"] + (s["lens_z"] - s["neck_z"]) * u
        out.append((r, z))
    return out


def mhlb_lens(at, phone=False):
    acc = Acc()
    s = MHLB
    n = 32 if not phone else 16
    bell = mhlb_bell(8 if not phone else 4)
    # reflector interior (faces inward/down), v 0 at the mouth -> 1 at the neck
    rings = []
    m = len(bell)
    for (r, z) in bell:
        rings.append([acc.v((r * math.cos(2 * math.pi * k / n), r * math.sin(2 * math.pi * k / n), z)) for k in range(n)])
    for i in range(m - 1):
        for k in range(n):
            k1 = (k + 1) % n
            am = 2 * math.pi * (k + 0.5) / n
            dr = bell[i + 1][0] - bell[i][0]
            dz = bell[i + 1][1] - bell[i][1]
            nn = (-math.cos(am) * dz, -math.sin(am) * dz, dr) if True else None
            # inward: the outward normal of a surface flaring down is (rad * (-dz), +dr) up-out; inward is the negative
            nn = (math.cos(am) * dz, math.sin(am) * dz, -dr)
            ids = [rings[i][k], rings[i][k1], rings[i + 1][k1], rings[i + 1][k]]
            v0, v1 = 1 - i / (m - 1), 1 - (i + 1) / (m - 1)
            acc.f(ids, [at.uv("refl", k / n, v0), at.uv("refl", (k + 1) / n, v0), at.uv("refl", (k + 1) / n, v1), at.uv("refl", k / n, v1)], True, nn)
    # socket plate at the neck
    disk(acc, 0, 0, s["neck_z"], s["neck_r"], n, lambda p: at.uv("plate", (p[0] / s["neck_r"] + 1) / 2, (p[1] / s["neck_r"] + 1) / 2), down=True)
    # coated ED-37 lamp, base up
    eg = egg_profile(s["lamp_r"], s["lamp_top"] - s["lamp_tip"], 9 if not phone else 5, base_r=20.0)
    lathe(acc, eg, 16 if not phone else 8, lambda a, v: at.uv("egg", a, v), z0=s["lamp_tip"], zsign=1.0, ref=(0.0, (s["lamp_top"] - s["lamp_tip"]) / 2))
    if phone:
        # outer bell as one shell on the alu swatch
        rings2 = []
        for (r, z) in bell:
            rings2.append([acc.v(((r + 3) * math.cos(2 * math.pi * k / n), (r + 3) * math.sin(2 * math.pi * k / n), z)) for k in range(n)])
        for i in range(m - 1):
            for k in range(n):
                k1 = (k + 1) % n
                am = 2 * math.pi * (k + 0.5) / n
                dr = bell[i + 1][0] - bell[i][0]
                dz = bell[i + 1][1] - bell[i][1]
                nn = (-math.cos(am) * dz, -math.sin(am) * dz, dr)
                acc.f([rings2[i][k], rings2[i][k1], rings2[i + 1][k1], rings2[i + 1][k]], [at.uv("sw_alu", .5, .5)] * 4, True, nn)
        cylinder(acc, (0, 0, s["ballast"][1]), (0, 0, s["ballast"][2]), s["ballast"][0], 12, lambda a, sx, l: at.uv("sw_alu", .5, .5), cap0=True)
        cylinder(acc, (0, 0, 0), (0, 0, s["ballast"][1]), s["stem_r"], 6, lambda a, sx, l: at.uv("sw_black", .5, .5))
    return acc


def mhlb_housing(lod):
    acc = Acc()
    s = MHLB
    n = {0: 40, 1: 20, 2: 10}[lod]
    bell = mhlb_bell({0: 10, 1: 5, 2: 2}[lod])
    # bell exterior: spun aluminium, 1.2 mm off the interior, outward/up normals
    m = len(bell)
    rings = []
    for (r, z) in bell:
        rings.append([acc.v(((r + 1.2) * math.cos(2 * math.pi * k / n), (r + 1.2) * math.sin(2 * math.pi * k / n), z)) for k in range(n)])
    for i in range(m - 1):
        for k in range(n):
            k1 = (k + 1) % n
            am = 2 * math.pi * (k + 0.5) / n
            dr = bell[i + 1][0] - bell[i][0]
            dz = bell[i + 1][1] - bell[i][1]
            nn = (-math.cos(am) * dz, -math.sin(am) * dz, dr)
            acc.f([rings[i][k], rings[i][k1], rings[i + 1][k1], rings[i + 1][k]],
                  [trim_uv("alu", k / n * 1700, 100 + 400 * i / (m - 1), 600), trim_uv("alu", (k + 1) / n * 1700, 100 + 400 * i / (m - 1), 600),
                   trim_uv("alu", (k + 1) / n * 1700, 100 + 400 * (i + 1) / (m - 1), 600), trim_uv("alu", k / n * 1700, 100 + 400 * (i + 1) / (m - 1), 600)], True, nn)
    mr = s["mouth_r"]
    lz = s["lens_z"]
    # rolled lip + gasket ring + door ring at the mouth
    if lod < 2:
        sweep_round(acc, mr + 1.2, fillet([(0.0, lz + 0.0), (6.0, lz - 2.0), (8.0, lz + 6.0)], [0, 3.0, 0], SEGS[lod]), n,
                    lambda a, t, T: trim_uv("alu", a * 1760, t, T))
    sweep_round(acc, mr - 4.0, plain([(0.0, lz - 2.0), (0.0, lz - 12.0), (14.0, lz - 12.0), (14.0, lz - 2.0)]), n,
                lambda a, t, T: trim_uv("black", a * 1760, t, T))
    if lod == 0:
        # door ring (aluminium) under the gasket and six spring clips
        sweep_round(acc, mr - 8.0, fillet([(0.0, lz - 12.0), (0.0, lz - 18.0), (22.0, lz - 18.0), (22.0, lz - 12.0)], [0, 1.5, 1.5, 0], 2), n,
                    lambda a, t, T: trim_uv("rim_alu", a * 1760, t, T))
        for k in range(6):
            a = 2 * math.pi * k / 6 + 0.26
            cx, cy = (mr + 6.0) * math.cos(a), (mr + 6.0) * math.sin(a)
            box(acc, (cx, cy, lz + 20.0), (9.0, 9.0, 50.0), box_trim("zinc"), skip=())
    # neck, ballast housing (finned), dome cap, swivel, stem, canopy hook box
    cylinder(acc, (0, 0, s["neck_z"] + 2), (0, 0, s["ballast"][2]), s["neck_r"] + 4.0, n // 2, cyl_mapper("alu", 89.0))
    br, bz0, bz1 = s["ballast"]
    cylinder(acc, (0, 0, bz1), (0, 0, bz0), br, n // 2 if lod < 2 else 10, cyl_mapper("alu", br), cap0=True)
    if lod == 0:
        for k in range(16):
            a = 2 * math.pi * k / 16
            cx, cy = (br + 9.0) * math.cos(a), (br + 9.0) * math.sin(a)
            # radial fin 18 x 3 x 130
            ux, uy = math.cos(a), math.sin(a)
            vx, vy = -uy, ux
            pts0 = [(cx - ux * 9 - vx * 1.5, cy - uy * 9 - vy * 1.5), (cx + ux * 9 - vx * 1.5, cy + uy * 9 - vy * 1.5),
                    (cx + ux * 9 + vx * 1.5, cy + uy * 9 + vy * 1.5), (cx - ux * 9 + vx * 1.5, cy - uy * 9 + vy * 1.5)]
            for i in range(4):
                a0, a1 = pts0[i], pts0[(i + 1) % 4]
                nrm = (a1[1] - a0[1], -(a1[0] - a0[0]), 0)
                quad(acc, [(a0[0], a0[1], bz1 + 10), (a1[0], a1[1], bz1 + 10), (a1[0], a1[1], bz0 - 10), (a0[0], a0[1], bz0 - 10)],
                     lambda q: trim_uv("alu", q[2] + 900, (q[0] + q[1]) % 100, 100), (-nrm[0], -nrm[1], 0) if (nrm[0] * ux + nrm[1] * uy) < 0 and False else nrm)
            quad(acc, [pts0[0] + (bz1 + 10,), pts0[1] + (bz1 + 10,), pts0[2] + (bz1 + 10,), pts0[3] + (bz1 + 10,)],
                 lambda q: trim_uv("alu", q[0] + 900, 50, 100), (0, 0, -1))
    lathe(acc, [(br, 0.0), (br - 10.0, -18.0), (60.0, -38.0), (s["stem_r"] + 6.0, -46.0)], n // 2,
          lambda a, v: trim_uv("alu", a * 900, v * 60, 60), z0=bz0, zsign=-1.0, ref=(0.0, -10.0))
    if lod < 2:
        lathe(acc, [(0.01, 0.0), (16.0, 4.0), (22.0, 22.0), (16.0, 40.0), (0.01, 44.0)], 12,
              lambda a, v: trim_uv("zinc", a * 140, v * 44, 44), z0=bz0 + 40.0, zsign=1.0, ref=(0.0, 22.0))
    cylinder(acc, (0, 0, bz0 + 80.0), (0, 0, -36.0), s["stem_r"], 10 if lod == 0 else 6, cyl_mapper("zinc", s["stem_r"]))
    box(acc, (0, 0, -18.0), (110.0, 110.0, 36.0), box_trim("alu"), skip=("+z",))
    return acc


def build_mhlb():
    at = mhlb_atlas()
    objs = [(mhlb_lens(at), "Lens", ["ZL_MHLB_Lens"])]
    for lod in (0, 1, 2):
        objs.append((mhlb_housing(lod), "Housing_L%d" % lod, ["LampOpt_Trim"]))
    objs.append((mhlb_lens(at, phone=True), "Phone", ["ZL_MHLB_Lens"]))
    emit("ZL_MHLB", objs, dict(id="MHLB", spec=MHLB, atlas=at.json()))


# =========================================================================== VT8
VT8 = dict(length=2440.0, top=-690.0, w=150.0, body_h=58.0, lens_h=52.0, latch_pitch=305.0, stem_y=800.0, tubes=[-36.0, 36.0])


def vt8_lens_section():
    s = VT8
    z0 = s["top"] - s["body_h"]
    hw = s["w"] / 2 + 6.0
    # U-ish prismatic diffuser: from the -x seam down round to the +x seam (visible side outward/down = right)
    pts = []
    for k in range(13):
        a = math.pi * k / 12
        x = -hw * math.cos(a)
        z = z0 - s["lens_h"] * math.sin(a) ** 0.75
        pts.append((x, z))
    return pts


def vt8_atlas():
    at = Atlas(1024, 512)
    at.put("diff", 0, 0, 1024, 300, kind="diffuser", length=VT8["length"])
    add_swatches(at, ["holder", "black", "zinc"], 1024 - SWATCH)
    return at


def vt8_lens(at, phone=False):
    acc = Acc()
    s = VT8
    L2 = s["length"] / 2 - 18.0
    pts = vt8_lens_section() if not phone else vt8_lens_section()[::2]
    # traverse +x seam -> bottom -> -x seam so the outside is to the right
    seq = pts[::-1]
    tot = sum(math.hypot(seq[i + 1][0] - seq[i][0], seq[i + 1][1] - seq[i][1]) for i in range(len(seq) - 1))
    acc_t = [0.0]
    for i in range(len(seq) - 1):
        acc_t.append(acc_t[-1] + math.hypot(seq[i + 1][0] - seq[i][0], seq[i + 1][1] - seq[i][1]))
    rows = [(acc.v((x, -L2, z)), acc.v((x, L2, z))) for (x, z) in seq]
    for i in range(len(seq) - 1):
        dx = seq[i + 1][0] - seq[i][0]
        dz = seq[i + 1][1] - seq[i][1]
        want = (dz, 0, -dx)
        want = (-dz, 0, dx)
        u0, u1 = acc_t[i] / tot, acc_t[i + 1] / tot
        acc.f([rows[i][0], rows[i][1], rows[i + 1][1], rows[i + 1][0]],
              [at.uv("diff", 0, u0), at.uv("diff", 1, u0), at.uv("diff", 1, u1), at.uv("diff", 0, u1)], True, _vt8_out(seq[i], seq[i + 1]))
    # lens end closures (the body's end walls cover them; dark)
    if phone:
        box(acc, (0, 0, s["top"] - s["body_h"] / 2), (s["w"], s["length"], s["body_h"]), lambda p, n: at.uv("sw_holder", .5, .5), skip=("+z",))
    return acc


def _vt8_out(a, b):
    # outward (away from the section centre line x=0, z=centre) for the diffuser
    mx, mz = (a[0] + b[0]) / 2, (a[1] + b[1]) / 2
    cz = VT8["top"] - VT8["body_h"]
    dx, dz = b[0] - a[0], b[1] - a[1]
    n1 = (dz, 0, -dx)
    if n1[0] * mx + n1[2] * (mz - cz) < 0:
        n1 = (-dz, 0, dx)
    return n1


def vt8_housing(lod):
    acc = Acc()
    s = VT8
    L2 = s["length"] / 2
    hw = s["w"] / 2
    zt, zb = s["top"], s["top"] - s["body_h"]
    # body: fibreglass, rounded top corners, sides flare 6 mm to the seam
    pts = [(-(hw + 6.0), zb), (-hw, zt - 12.0), (-hw + 12.0, zt), (hw - 12.0, zt), (hw, zt - 12.0), (hw + 6.0, zb)]
    prof = fillet(pts, [0, 6.0, 8.0, 8.0, 6.0, 0], SEGS[lod]) if lod < 2 else plain(pts)
    rows = []
    P = [p for p, _ in prof]
    for (x, z) in P:
        rows.append((acc.v((x, -L2, z)), acc.v((x, L2, z))))
    for i in range(len(P) - 1):
        a, b = P[i], P[i + 1]
        nn = (-(b[1] - a[1]), 0, (b[0] - a[0]))
        if nn[0] * (a[0] + b[0]) + nn[2] * ((a[1] + b[1]) / 2 - (zt + zb) / 2) < 0:
            nn = (-nn[0], 0, -nn[2])
        acc.f([rows[i][0], rows[i][1], rows[i + 1][1], rows[i + 1][0]],
              [trim_uv("holder", -L2, i * 20, 200), trim_uv("holder", L2, i * 20, 200), trim_uv("holder", L2, i * 20 + 20, 200), trim_uv("holder", -L2, i * 20 + 20, 200)],
              prof[i + 1][1], nn)
    # end walls: body section + lens section closed (moulded end cap)
    lens = vt8_lens_section()
    for sy in (1, -1):
        y = sy * L2
        outline = [(x, z) for (x, z) in P] + [(x, z) for (x, z) in lens[::-1]][1:-1]
        cap_polygon(acc, outline, y, sy, lambda v: trim_uv("holder", v[0] + 300, v[2] - zb + 60, 140))
        # the end walls drop round the lens ends 18 mm deep
        lp = [(x * 1.04, z - 2.0) for (x, z) in lens]
        for i in range(len(lp) - 1):
            a, b = lp[i], lp[i + 1]
            q = [(a[0], y, a[1]), (b[0], y, b[1]), (b[0], y - sy * 18.0, b[1]), (a[0], y - sy * 18.0, a[1])]
            quad(acc, q, lambda v: trim_uv("holder", v[0] + 320, 90, 140), _vt8_out(a, b))
    # gasket line at the seam (black, 3 mm) both sides
    for sx in (1, -1):
        quad(acc, [(sx * (hw + 6.5), -L2, zb + 1.5), (sx * (hw + 6.5), L2, zb + 1.5), (sx * (hw + 6.5), L2, zb - 1.5), (sx * (hw + 6.5), -L2, zb - 1.5)],
             lambda v: trim_uv("black", v[1] + L2, 50, 100), (sx, 0, 0))
    if lod < 2:
        # cam latches (stainless): every 305 mm on both sides, over the seam
        nl = int((s["length"] - 200) // s["latch_pitch"]) + 1
        for k in range(nl):
            y = -L2 + 100.0 + k * s["latch_pitch"]
            for sx in (1, -1):
                box(acc, (sx * (hw + 9.0), y, zb + 2.0), (6.0, 22.0, 34.0), box_trim("rim_alu"), skip=())
                if lod == 0:
                    box(acc, (sx * (hw + 12.5), y, zb - 10.0), (3.0, 14.0, 10.0), box_trim("rim_alu"), skip=())
        # conduit hub at the -Y end (gland)
        cylinder(acc, (0, -L2, zt - 28.0), (0, -L2 - 34.0, zt - 28.0), 18.0, 12 if lod == 0 else 6, cyl_mapper("zinc", 18.0), cap1=True)
    # stems + canopies + hangers
    for sy in (1, -1):
        y = sy * s["stem_y"]
        cylinder(acc, (0, y, zt), (0, y, -12.0), 10.5, 10 if lod == 0 else 6, cyl_mapper("zinc", 10.5))
        if lod < 2:
            lathe(acc, [(50.0, 0.0), (49.0, -4.0), (44.0, -12.0), (12.0, -14.0)], 16 if lod == 0 else 8,
                  lambda a, v: trim_uv("enamel", a * 314, v * 30, 30), cx=0, cy=y, z0=0.0, zsign=1.0, ref=(0.0, 5.0))
            box(acc, (0, y, zt + 9.0), (60.0, 40.0, 18.0), box_trim("zinc"), skip=("-z",))
    return acc


def build_vt8():
    at = vt8_atlas()
    objs = [(vt8_lens(at), "Lens", ["ZL_VT8_Lens"])]
    for lod in (0, 1, 2):
        objs.append((vt8_housing(lod), "Housing_L%d" % lod, ["LampOpt_Trim"]))
    objs.append((vt8_lens(at, phone=True), "Phone", ["ZL_VT8_Lens"]))
    emit("ZL_VT8", objs, dict(id="VT8", spec=VT8, atlas=at.json()))


# =========================================================================== NICHE + PORT (wall kits facing -Z = into the pool)
NICHE = dict(ring_r=136.5, ring_in=108.0, ring_h=9.0, lens_r=104.0)
PORT = dict(half=160.0, frame=28.0, proud=24.0, glass_back=30.0, bolts=12)


def niche_atlas():
    at = Atlas(512, 512)
    at.put("face", 0, 0, 400, 400, kind="parface")
    at.put("port", 0, 406, 100, 100, kind="port_dummy")
    add_swatches(at, ["rim_alu", "black", "zinc"], 512 - SWATCH)
    return at


def port_atlas():
    at = Atlas(512, 512)
    at.put("port", 0, 0, 440, 440, kind="port")
    add_swatches(at, ["zinc", "black", "rim_alu"], 512 - SWATCH)
    return at


def niche_lens(at, phone=False):
    acc = Acc()
    s = NICHE
    n = 32 if not phone else 16
    disk(acc, 0, 0, 3.0, s["lens_r"], n, lambda p: at.uv("face", (p[0] / s["lens_r"] + 1) / 2, (p[1] / s["lens_r"] + 1) / 2), down=True, dome=4.0, rings=3 if not phone else 1)
    if phone:
        ring_disk_uv = lambda p: at.uv("sw_rim_alu", .5, .5)
        for k in range(n):
            a0, a1 = 2 * math.pi * k / n, 2 * math.pi * (k + 1) / n
            r0, r1 = s["ring_in"], s["ring_r"]
            quad(acc, [(r0 * math.cos(a0), r0 * math.sin(a0), -s["ring_h"]), (r0 * math.cos(a1), r0 * math.sin(a1), -s["ring_h"]),
                       (r1 * math.cos(a1), r1 * math.sin(a1), -1.0), (r1 * math.cos(a0), r1 * math.sin(a0), -1.0)], ring_disk_uv, (0, 0, -1))
    return acc


def niche_housing(lod):
    acc = Acc()
    s = NICHE
    n = {0: 48, 1: 24, 2: 12}[lod]
    w = s["ring_r"] - s["ring_in"]
    h = s["ring_h"]
    if lod == 2:
        prof = plain([(0.0, -h), (w, 0.0)])
    else:
        prof = fillet([(-2.0, 2.0), (0.0, -h + 1.0), (w * 0.35, -h), (w, -2.0), (w + 1.0, 1.0)], [0, 2.0, 6.0, 3.0, 0], SEGS[lod])
    sweep_round(acc, s["ring_in"], prof, n, lambda a, t, T: trim_uv("rim_alu", a * 2 * math.pi * s["ring_r"], t, T))
    # black elastomer seal ring between the lens and the ring
    sweep_round(acc, s["lens_r"], plain([(0.0, 3.0), (s["ring_in"] - s["lens_r"] - 2.0, -2.0)]), n,
                lambda a, t, T: trim_uv("black", a * 700, t, T))
    if lod == 0:
        # the pilot screw at the top (slotted), and the stamped rim bead
        a = math.pi / 2
        cx, cy = (s["ring_in"] + w * 0.45) * math.cos(a), (s["ring_in"] + w * 0.45) * math.sin(a)
        disk(acc, cx, cy, -h - 0.2, 5.0, 12, lambda p: trim_uv("rim_alu", p[0] + 900, 40, 100), down=True, dome=1.5)
        quad(acc, [(cx - 4.0, cy - 0.6, -h - 1.8), (cx + 4.0, cy - 0.6, -h - 1.8), (cx + 4.0, cy + 0.6, -h - 1.8), (cx - 4.0, cy + 0.6, -h - 1.8)],
             lambda p: trim_uv("black", p[0] + 100, 50, 100), (0, 0, -1))
    return acc


def port_lens(at, phone=False):
    acc = Acc()
    s = PORT
    g = s["half"] - s["frame"]
    zb = s["glass_back"]
    quad(acc, [(-g, -g, zb), (g, -g, zb), (g, g, zb), (-g, g, zb)], lambda p: at.uv("port", (p[0] + g) / (2 * g), (p[1] + g) / (2 * g)), (0, 0, -1))
    if phone:
        H = s["half"]
        for (x0, y0, x1, y1) in ((-H, -H, H, -g), (-H, g, H, H), (-H, -g, -g, g), (g, -g, H, g)):
            quad(acc, [(x0, y0, -s["proud"]), (x1, y0, -s["proud"]), (x1, y1, -s["proud"]), (x0, y1, -s["proud"])], lambda p: at.uv("sw_zinc", .5, .5), (0, 0, -1))
    return acc


def port_housing(lod):
    acc = Acc()
    s = PORT
    H, F, P, zb = s["half"], s["frame"], s["proud"], s["glass_back"]
    g = H - F
    # cast frame: a mitred ring, face at -P, outer chamfer, inner reveal back to the glass
    pts = [(0.0, zb), (0.0, -P + 3.0), (3.0, -P), (F - 6.0, -P), (F, -P + 6.0), (F, 0.0)]
    face = fillet(pts, [0, 2.0, 0, 3.0, 0, 0], SEGS[lod]) if lod < 2 else plain(pts)
    sweep_rect(acc, g, g, face, lambda side, along, t, tt: trim_uv("zinc", along + 900, t, tt, side))
    if lod < 2:
        nb = s["bolts"]
        for k in range(nb):
            per = 8 * g
            u = (k + 0.5) / nb * per
            side = int(u // (2 * g))
            t = u - side * 2 * g - g
            r = g + F / 2
            x, y = [(r, t), (-t, r), (-r, -t), (t, -r)][side]
            cylinder(acc, (x, y, -P), (x, y, -P - 5.0), 7.0, 6 if lod == 0 else 4, cyl_mapper("zinc", 7.0), cap1=True)
    return acc


def build_niche():
    at = niche_atlas()
    objs = [(niche_lens(at), "Lens", ["ZL_NICHE_Lens"])]
    for lod in (0, 1, 2):
        objs.append((niche_housing(lod), "Housing_L%d" % lod, ["LampOpt_Trim"]))
    objs.append((niche_lens(at, phone=True), "Phone", ["ZL_NICHE_Lens"]))
    emit("ZL_NICHE", objs, dict(id="NICHE", spec=NICHE, atlas=at.json()))
    at2 = port_atlas()
    objs = [(port_lens(at2), "Lens", ["ZL_PORT_Lens"])]
    for lod in (0, 1, 2):
        objs.append((port_housing(lod), "Housing_L%d" % lod, ["LampOpt_Trim"]))
    objs.append((port_lens(at2, phone=True), "Phone", ["ZL_PORT_Lens"]))
    emit("ZL_PORT", objs, dict(id="PORT", spec=PORT, atlas=at2.json()))


# =========================================================================== STRIP and INDR (Run!, soffit kits; long axis Y)
STRIP = dict(length=1270.0, ch_w=110.0, ch_d=50.0, tube_x=38.0, tube_z=-84.0, tube_len=1180.0, holder_y=600.0, guard_pitch=50.0)
INDR = dict(length=1270.0, drop=60.0, ch_w=110.0, ch_d=50.0, tube_x=38.0, tube_z=-142.0, tube_len=1180.0, holder_y=600.0,
            refl_top=-64.0, refl_flange=88.0, refl_hem=(168.0, -188.0))


def strip_atlas():
    at = Atlas(1024, 256)
    at.put("tube0", 0, 0, 1024, 60, kind="tube", length=STRIP["tube_len"], seed=1)
    at.put("tube1", 0, 66, 1024, 60, kind="tube", length=STRIP["tube_len"], seed=2)
    add_swatches(at, ["enamel", "holder", "zinc"], 1024 - SWATCH)
    return at


def indr_atlas():
    at = Atlas(1024, 512)
    at.put("tube0", 0, 0, 1024, 60, kind="tube", length=INDR["tube_len"], seed=3)
    at.put("tube1", 0, 66, 1024, 60, kind="tube", length=INDR["tube_len"], seed=4)
    at.put("refl", 0, 132, 1024, 300, kind="refl_ind", length=INDR["length"])
    add_swatches(at, ["enamel", "holder", "zinc", "black"], 1024 - SWATCH)
    return at


def tubes(acc, at, xs, z, L, n=16, segs=1):
    for i, x in enumerate(xs):
        reg = "tube%d" % i
        y0, y1 = -L / 2, L / 2
        for k in range(segs):
            ya, yb = y0 + (y1 - y0) * k / segs, y0 + (y1 - y0) * (k + 1) / segs
            cylinder(acc, (x, ya, z), (x, yb, z), 19.0, n, lambda a, s, l, ya=ya, yb=yb: at.uv(reg, ((ya - y0) + s) / L, a))
    return acc


def channel(acc, w, d, L, z_top, lod, finish="enamel"):
    hw = w / 2
    pts = [(-hw, z_top), (-hw, z_top - d), (hw, z_top - d), (hw, z_top)]
    prof = fillet(pts, [0, 5.0, 5.0, 0], SEGS[lod]) if lod < 2 else plain(pts)
    P = [p for p, _ in prof]
    rows = [(acc.v((x, -L / 2, z)), acc.v((x, L / 2, z))) for (x, z) in P]
    for i in range(len(P) - 1):
        a, b = P[i], P[i + 1]
        nn = (-(b[1] - a[1]), 0, (b[0] - a[0]))
        if nn[0] * (a[0] + b[0]) + nn[2] * ((a[1] + b[1]) / 2 - (z_top - d / 2)) < 0:
            nn = (-nn[0], 0, -nn[2])
        acc.f([rows[i][0], rows[i][1], rows[i + 1][1], rows[i + 1][0]],
              [trim_uv(finish, -L / 2, i * 15, 100), trim_uv(finish, L / 2, i * 15, 100), trim_uv(finish, L / 2, i * 15 + 15, 100), trim_uv(finish, -L / 2, i * 15 + 15, 100)],
              prof[i + 1][1], nn)
    for sy in (1, -1):
        cap_polygon(acc, P, sy * L / 2, sy, lambda v: trim_uv(finish, v[0] + 200, v[2] - z_top + 60, 80))
    return P


def holders(acc, xs, y_end, z_top, z_tube, lod):
    if lod == 2:
        return
    for sy in (1, -1):
        for x in xs:
            # white thermoplastic lampholder hanging from the channel, the pin slot toward the tube
            box(acc, (x, sy * (y_end + 14.0), (z_top + z_tube - 22.0) / 2), (30.0, 28.0, (z_top - z_tube) + 22.0), box_trim("holder"), skip=("+z",))
            if lod == 0:
                quad(acc, [(x - 2.0, sy * y_end - sy * 0.1, z_tube - 10), (x + 2.0, sy * y_end - sy * 0.1, z_tube - 10),
                           (x + 2.0, sy * y_end - sy * 0.1, z_tube + 10), (x - 2.0, sy * y_end - sy * 0.1, z_tube + 10)],
                     lambda q: trim_uv("black", q[0] + 300, 60, 100), (0, -sy, 0))


def tube_bases(acc, xs, z, L, lod):
    if lod == 2:
        return
    for x in xs:
        for sy in (1, -1):
            y0, y1 = sy * (L / 2), sy * (L / 2 + 22.0)
            cylinder(acc, (x, min(y0, y1), z), (x, max(y0, y1), z), 19.3, 16 if lod == 0 else 8, cyl_mapper("alu", 19.3))


def strip_lens(at, phone=False):
    acc = Acc()
    s = STRIP
    tubes(acc, at, [-s["tube_x"], s["tube_x"]], s["tube_z"], s["tube_len"], 16 if not phone else 8)
    if phone:
        box(acc, (0, 0, -s["ch_d"] / 2), (s["ch_w"], s["length"], s["ch_d"]), lambda p, n: at.uv("sw_enamel", .5, .5), skip=("+z",))
    return acc


def strip_housing(lod):
    acc = Acc()
    s = STRIP
    channel(acc, s["ch_w"], s["ch_d"], s["length"], 0.0, lod)
    xs = [-s["tube_x"], s["tube_x"]]
    holders(acc, xs, s["tube_len"] / 2 + 22.0, -s["ch_d"], s["tube_z"], lod)
    tube_bases(acc, xs, s["tube_z"], s["tube_len"], lod)
    if lod == 2:
        return acc
    # wire guard: two side rails + U-wires round the tubes every 50 mm (every 100 mm at LOD1), clips to the channel
    rail_x, rail_z = s["ch_w"] / 2 + 8.0, -s["ch_d"] - 4.0
    nwire = 6 if lod == 0 else 4
    for sx in (1, -1):
        cylinder(acc, (sx * rail_x, -s["tube_len"] / 2 + 10, rail_z), (sx * rail_x, s["tube_len"] / 2 - 10, rail_z), 2.5, nwire, cyl_mapper("zinc", 2.5))
    pitch = s["guard_pitch"] * (1 if lod == 0 else 2)
    nw = int((s["tube_len"] - 60) // pitch) + 1
    zlow = s["tube_z"] - 19.0 - 12.0
    for k in range(nw):
        y = -s["tube_len"] / 2 + 30.0 + k * pitch
        path = [(-rail_x, y, rail_z), (-rail_x, y, zlow + 10.0), (-rail_x + 8.0, y, zlow + 1.0), (-rail_x + 20.0, y, zlow),
                (rail_x - 20.0, y, zlow), (rail_x - 8.0, y, zlow + 1.0), (rail_x, y, zlow + 10.0), (rail_x, y, rail_z)]
        tube_path(acc, path, 1.5, nwire, lambda a, sx, tot: trim_uv("zinc", sx + 300, a * 9.4, 9.4))
    if lod == 0:
        for y in (-s["tube_len"] / 2 + 60, -s["tube_len"] / 6, s["tube_len"] / 6, s["tube_len"] / 2 - 60):
            for sx in (1, -1):
                box(acc, (sx * (s["ch_w"] / 2 + 4.0), y, -s["ch_d"] + 4.0), (10.0, 14.0, 18.0), box_trim("zinc"), skip=("+z",))
    return acc


def indr_lens(at, phone=False):
    acc = Acc()
    s = INDR
    tubes(acc, at, [-s["tube_x"], s["tube_x"]], s["tube_z"], s["tube_len"], 16 if not phone else 8)
    # reflector interior (faces down/inward): from the flange root to the hem, both sides; v 0 at the root -> 1 at the hem
    hx, hz = s["refl_hem"]
    L2 = s["length"] / 2
    for sx in (1, -1):
        pts = [(sx * (s["ch_w"] / 2 + 2.0), s["refl_top"] - 2.0)]
        for k in range(1, 9):
            u = k / 8
            x = s["ch_w"] / 2 + 2.0 + (hx - s["ch_w"] / 2 - 2.0) * (u ** 0.85)
            z = s["refl_top"] - 2.0 + (hz - s["refl_top"] + 2.0) * (u ** 1.6)
            pts.append((sx * x, z))
        for i in range(len(pts) - 1):
            a, b = pts[i], pts[i + 1]
            nn = (-(b[1] - a[1]), 0, (b[0] - a[0]))
            if nn[0] * sx > 0:
                nn = (-nn[0], 0, -nn[2])
            reg = "refl"
            u0, u1 = i / (len(pts) - 1), (i + 1) / (len(pts) - 1)
            ua = 0.5 + 0.5 * sx * u0
            ub = 0.5 + 0.5 * sx * u1
            quad_ids = [acc.v((a[0], -L2, a[1])), acc.v((a[0], L2, a[1])), acc.v((b[0], L2, b[1])), acc.v((b[0], -L2, b[1]))]
            acc.f(quad_ids, [at.uv(reg, 0, ua), at.uv(reg, 1, ua), at.uv(reg, 1, ub), at.uv(reg, 0, ub)], True, nn)
    if phone:
        box(acc, (0, 0, -s["drop"] - s["ch_d"] / 2), (s["ch_w"], s["length"], s["ch_d"]), lambda p, n: at.uv("sw_enamel", .5, .5), skip=())
    # soffit stripes above the uplight slots (the lit ceiling), glow submesh
    for sx in (1, -1):
        x0, x1 = sx * 50.0, sx * 230.0
        ids = [acc.v((min(x0, x1), -L2 - 150, -2.0)), acc.v((max(x0, x1), -L2 - 150, -2.0)), acc.v((max(x0, x1), L2 + 150, -2.0)), acc.v((min(x0, x1), L2 + 150, -2.0))]
        acc.f(ids, [(0, 0), (1, 0), (1, 1), (0, 1)] if sx > 0 else [(1, 0), (0, 0), (0, 1), (1, 1)], False, (0, 0, -1), 1)
    return acc


def indr_housing(lod):
    acc = Acc()
    s = INDR
    L = s["length"]
    channel(acc, s["ch_w"], s["ch_d"], L, -s["drop"], lod)
    xs = [-s["tube_x"], s["tube_x"]]
    holders(acc, xs, s["tube_len"] / 2 + 22.0, -s["drop"] - s["ch_d"], s["tube_z"], lod)
    tube_bases(acc, xs, s["tube_z"], s["tube_len"], lod)
    hx, hz = s["refl_hem"]
    L2 = L / 2
    # reflector exterior (white porcelain enamel): flat flange with slots, then the curve, then the rolled hem
    for sx in (1, -1):
        pts = [(sx * (s["ch_w"] / 2), s["refl_top"]), (sx * s["refl_flange"], s["refl_top"])]
        for k in range(1, 9 if lod < 2 else 3):
            u = k / (8 if lod < 2 else 2)
            x = s["ch_w"] / 2 + 2.0 + (hx - s["ch_w"] / 2 - 2.0) * (u ** 0.85)
            z = s["refl_top"] + (hz - s["refl_top"]) * (u ** 1.6)
            if abs(x) < s["refl_flange"]:
                continue
            pts.append((sx * (x + 1.5), z + 1.0))
        for i in range(len(pts) - 1):
            a, b = pts[i], pts[i + 1]
            nn = (-(b[1] - a[1]), 0, (b[0] - a[0]))
            if nn[0] * sx < 0 and abs(b[1] - a[1]) > 1e-3 or (abs(b[1] - a[1]) <= 1e-3 and nn[2] < 0):
                nn = (-nn[0], 0, -nn[2])
            quad(acc, [(a[0], -L2, a[1]), (a[0], L2, a[1]), (b[0], L2, b[1]), (b[0], -L2, b[1])],
                 lambda q: trim_uv("enamel", q[1] + L2, abs(q[0]) + 100, 400), nn)
        if lod < 2:
            # rolled hem (r 6) along the bottom edge
            cylinder(acc, (sx * (hx + 3.0), -L2, hz - 1.0), (sx * (hx + 3.0), L2, hz - 1.0), 6.0, 8 if lod == 0 else 4, cyl_mapper("enamel", 6.0))
        if lod == 0:
            # uplight slots in the flange: dark recesses (the light through them is the soffit stripe on the lens)
            for k in range(11):
                y = -L2 + 66.0 + k * 110.0
                quad(acc, [(sx * (s["ch_w"] / 2 + 8.0), y - 22.0, s["refl_top"] + 0.6), (sx * (s["refl_flange"] - 6.0), y - 22.0, s["refl_top"] + 0.6),
                           (sx * (s["refl_flange"] - 6.0), y + 22.0, s["refl_top"] + 0.6), (sx * (s["ch_w"] / 2 + 8.0), y + 22.0, s["refl_top"] + 0.6)],
                     lambda q: trim_uv("black", q[1] + 600, 50, 100), (0, 0, 1))
    # end plates of the reflector (stamped), following the outline
    for sy in (1, -1):
        outline = [(-s["refl_flange"], s["refl_top"]), (s["refl_flange"], s["refl_top"]), (hx, hz), (-hx, hz)]
        cap_polygon(acc, outline, sy * (L2 + 1.0), sy, lambda v: trim_uv("enamel", v[0] + 500, v[2] - hz + 20, 160))
    # two hangers: threaded rod + nuts from the soffit to the channel
    for y in (-L2 + 200.0, L2 - 200.0):
        cylinder(acc, (0, y, 0.0), (0, y, -s["drop"]), 5.0, 8 if lod == 0 else 4, cyl_mapper("zinc", 5.0))
        if lod == 0:
            for zz in (-4.0, -s["drop"] + 6.0):
                cylinder(acc, (0, y, zz + 3.0), (0, y, zz - 3.0), 9.0, 6, cyl_mapper("zinc", 9.0), cap0=True, cap1=True)
            box(acc, (0, y, -6.0), (60.0, 30.0, 6.0), box_trim("zinc"), skip=("+z",))
    return acc


# STRIP3 (2026-11-04, card 灯具适配 1, clone-only mock-up for Red's still): R-B's strip as a three-lamp strip, the middle tube
# red-coated on the battery circuit (research/level_space/71_run_room.md s.6.2). Three T12s need 50 mm centres, so the channel
# widens 110 -> 140 mm and the guard with it; everything else is STRIP's. "Lens" = the two white tubes (the strip's lens
# material and atlas); "LensRed" = the red tube alone (its own material, the same atlas), so the two are driven apart.
STRIP3 = dict(STRIP, ch_w=140.0, tubes=[-50.0, 0.0, 50.0], red=1)


def strip3_lens(at, red=False):
    acc = Acc()
    s = STRIP3
    xs = [x for i, x in enumerate(s["tubes"]) if (i == s["red"]) == red]
    tubes(acc, at, xs, s["tube_z"], s["tube_len"], 16)
    return acc


def strip3_housing(lod):
    acc = Acc()
    s = STRIP3
    channel(acc, s["ch_w"], s["ch_d"], s["length"], 0.0, lod)
    xs = s["tubes"]
    holders(acc, xs, s["tube_len"] / 2 + 22.0, -s["ch_d"], s["tube_z"], lod)
    tube_bases(acc, xs, s["tube_z"], s["tube_len"], lod)
    if lod == 2:
        return acc
    rail_x, rail_z = s["ch_w"] / 2 + 8.0, -s["ch_d"] - 4.0
    nwire = 6 if lod == 0 else 4
    for sx in (1, -1):
        cylinder(acc, (sx * rail_x, -s["tube_len"] / 2 + 10, rail_z), (sx * rail_x, s["tube_len"] / 2 - 10, rail_z), 2.5, nwire, cyl_mapper("zinc", 2.5))
    pitch = s["guard_pitch"] * (1 if lod == 0 else 2)
    nw = int((s["tube_len"] - 60) // pitch) + 1
    zlow = s["tube_z"] - 19.0 - 12.0
    for k in range(nw):
        y = -s["tube_len"] / 2 + 30.0 + k * pitch
        path = [(-rail_x, y, rail_z), (-rail_x, y, zlow + 10.0), (-rail_x + 8.0, y, zlow + 1.0), (-rail_x + 20.0, y, zlow),
                (rail_x - 20.0, y, zlow), (rail_x - 8.0, y, zlow + 1.0), (rail_x, y, zlow + 10.0), (rail_x, y, rail_z)]
        tube_path(acc, path, 1.5, nwire, lambda a, sx, tot: trim_uv("zinc", sx + 300, a * 9.4, 9.4))
    if lod == 0:
        for y in (-s["tube_len"] / 2 + 60, -s["tube_len"] / 6, s["tube_len"] / 6, s["tube_len"] / 2 - 60):
            for sx in (1, -1):
                box(acc, (sx * (s["ch_w"] / 2 + 4.0), y, -s["ch_d"] + 4.0), (10.0, 14.0, 18.0), box_trim("zinc"), skip=("+z",))
    return acc


def build_strip3():
    at = strip_atlas()
    objs = [(strip3_lens(at), "Lens", ["ZL_STRIP_Lens"]), (strip3_lens(at, red=True), "LensRed", ["ZL_STRIP3_Red"])]
    for lod in (0, 1, 2):
        objs.append((strip3_housing(lod), "Housing_L%d" % lod, ["LampOpt_Trim"]))
    emit("ZL_STRIP3", objs, dict(id="STRIP3", spec=STRIP3, atlas=at.json()))


def strip3_phone(at, red=False):
    """phone tier: the tubes at 8 sides; the white set carries the channel as a box in the enamel swatch, the red tube is alone"""
    acc = Acc()
    s = STRIP3
    xs = [x for i, x in enumerate(s["tubes"]) if (i == s["red"]) == red]
    tubes(acc, at, xs, s["tube_z"], s["tube_len"], 8)
    if not red:
        box(acc, (0, 0, -s["ch_d"] / 2), (s["ch_w"], s["length"], s["ch_d"]), lambda p, n: at.uv("sw_enamel", .5, .5), skip=("+z",))
    return acc


def build_strip_red():
    """main landing (2026-11-04, Red: "加，用更亮档"): ZL_STRIP becomes the three-lamp strip with the red tube, same file name, so the
    kit keeps its path and GUIDs; "LensRed" / "PhoneRed" are the red tube alone, driven apart from the white "Lens" / "Phone"."""
    at = strip_atlas()
    objs = [(strip3_lens(at), "Lens", ["ZL_STRIP_Lens"]), (strip3_lens(at, red=True), "LensRed", ["ZL_STRIP_Red"])]
    for lod in (0, 1, 2):
        objs.append((strip3_housing(lod), "Housing_L%d" % lod, ["LampOpt_Trim"]))
    objs.append((strip3_phone(at), "Phone", ["ZL_STRIP_Lens"]))
    objs.append((strip3_phone(at, red=True), "PhoneRed", ["ZL_STRIP_Red"]))
    emit("ZL_STRIP", objs, dict(id="STRIP", spec=STRIP3, atlas=at.json(), red_tube=True))


def build_strip():
    at = strip_atlas()
    objs = [(strip_lens(at), "Lens", ["ZL_STRIP_Lens"])]
    for lod in (0, 1, 2):
        objs.append((strip_housing(lod), "Housing_L%d" % lod, ["LampOpt_Trim"]))
    objs.append((strip_lens(at, phone=True), "Phone", ["ZL_STRIP_Lens"]))
    emit("ZL_STRIP", objs, dict(id="STRIP", spec=STRIP, atlas=at.json()))
    at = indr_atlas()
    objs = [(indr_lens(at), "Lens", ["ZL_INDR_Lens", "ZL_INDR_Glow"])]
    for lod in (0, 1, 2):
        objs.append((indr_housing(lod), "Housing_L%d" % lod, ["LampOpt_Trim"]))
    objs.append((indr_lens(at, phone=True), "Phone", ["ZL_INDR_Lens", "ZL_INDR_Glow"]))
    emit("ZL_INDR", objs, dict(id="INDR", spec=INDR, atlas=at.json()))


# =========================================================================== RPLATE
# Built in the twin-head cabinet's own frame converted to Blender: Unity (ux, uy, uz) = (-bx, bz, -by): wall at by = 0,
# room toward -Y, the cabinet's mounts at Unity (+-0.112, 0.201, 0.059) -> Blender (-+112, -59, 201) mm.
def rplate_housing(lod):
    acc = Acc()
    # 4 in octagon box on the wall under the bracket, 54 deep, centre at z 95 mm
    n = 8
    zc = 95.0
    for k in range(n):
        a0, a1 = 2 * math.pi * (k + 0.5) / n, 2 * math.pi * (k + 1.5) / n
        r = 54.0
        p0 = (r * math.cos(a0), zc + r * math.sin(a0))
        p1 = (r * math.cos(a1), zc + r * math.sin(a1))
        nrm = (math.cos((a0 + a1) / 2), 0, math.sin((a0 + a1) / 2))
        quad(acc, [(p0[0], 0.0, p0[1]), (p1[0], 0.0, p1[1]), (p1[0], -54.0, p1[1]), (p0[0], -54.0, p0[1])],
             lambda q: trim_uv("zinc", q[0] + 600, q[1] + 60, 120), nrm)
    # cover plate (almond = enamel row), 1.5 mm, round 110
    ids = []
    m = 24 if lod == 0 else 10
    c = acc.v((0, -56.0, zc))
    ring = [acc.v((57.0 * math.cos(2 * math.pi * k / m), -56.0, zc + 57.0 * math.sin(2 * math.pi * k / m))) for k in range(m)]
    for k in range(m):
        tri = [c, ring[k], ring[(k + 1) % m]]
        acc.f(tri, [trim_uv("enamel", acc.V[q][0] + 300, acc.V[q][2] - zc + 60, 120) for q in tri], False, (0, -1, 0))
    if lod == 0:
        for (x, z) in ((-38.0, zc + 38.0), (38.0, zc - 38.0)):
            cylinder(acc, (x, -56.0, z), (x, -59.0, z), 4.0, 8, cyl_mapper("zinc", 4.0), cap1=True)
    # bracket: a formed steel channel across under the two mounts, from the wall out to the mounts
    box(acc, (0, -40.0, 188.0), (290.0, 80.0, 26.0), box_trim("enamel"), skip=())
    if lod < 2:
        for sx in (1, -1):
            box(acc, (sx * 112.0, -59.0, 197.5), (44.0, 44.0, 7.0), box_trim("zinc"), skip=())
        # stem from the octagon box up to the bracket
        cylinder(acc, (0, -27.0, zc + 54.0), (0, -27.0, 176.0), 11.0, 8, cyl_mapper("zinc", 11.0))
    return acc


def build_rplate():
    objs = []
    for lod in (0, 1, 2):
        objs.append((rplate_housing(lod), "Housing_L%d" % lod, ["LampOpt_Trim"]))
    emit("ZL_RPLATE", objs, dict(id="RPLATE"))


BUILDERS = dict(PEND=build_pend, MHDL=build_mhdl, HIDUP=build_hidup, MHLB=build_mhlb, VT8=build_vt8, NICHE=build_niche,
                STRIP=build_strip, RPLATE=build_rplate, STRIP3=build_strip3, STRIPRED=build_strip_red)

for kid, fn in BUILDERS.items():
    if ZL_ONLY and kid not in ZL_ONLY:
        continue
    fn()
path = os.path.join(OUT, "zl_build_report.json")
old = json.load(open(path)) if os.path.exists(path) else {}
old.update(REPORT)
json.dump(old, open(path, "w"), indent=1)
print("ZL DONE", ", ".join(sorted(REPORT)))
