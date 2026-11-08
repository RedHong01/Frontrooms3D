"""FRONTROOMSSS structures lamps (P7 structures_lamp_common, structures_assets G1-b, 2026-11-06): the two zone-lamps
fixtures scene 4 names and the game had no kit for, built the way the landed zone lamps were (zone_lamps/kit/zl_build.py,
whose helpers are reused here unchanged: mesh accumulator, profile sweeps, atlas, trim rows of Troffer_Trim, FBX export).

Run (headless, Blender 4.3):
  Blender --background --factory-startup --python zl_struct.py -- <outdir> [FLUSH GLOBE]
Writes <outdir>/ZL_<ID>.fbx and <outdir>/ZL_<ID>_layout.json (zl_struct_tex.py paints the lens maps from the layout).

Contract per kit (as main's FrontRoomsLampKit families): Lens = the ONE renderer the lamp logic drives (_EmissionColor);
Housing_L0 / L1 / L2 on Troffer_Trim, shadows off, LOD 4 / 12 / 46 m; Phone = one mesh on the lens material (frame in
emission-free swatches). Frame: mm, Blender X short, Y long, Z up, Z 0 the mounting plane (the ceiling, or the wall for a
wall-mounted globe), the room toward -Z; Unity sees (x, y, z) = (-x, z, -y). No text, label, brand or logo. ASCII only.

Kits:
  FLUSH  ZL F5: a 2 x 2 ft recessed, enclosed and gasketed lay-in (wet location): a white enamel door frame with four cam
         latches over a prismatic acrylic lens on a black gasket, two U-lamps faint through it (zone_lamps/10_pool.md 3.5:
         "flat white squares ... a bright, flat, edge-less tile"; scene 4's SP6 dry landings, soffits and the tower).
  GLOBE  ZL F7: the vapour-tight "jelly jar": a cast body and threaded collar, a glass globe on a gasket over a 100 W A19
         lamp, a four-bar cast guard (10_pool.md 3.7: globe about 162 mm for 75-100 W; PL23). Ceiling or wall (the
         builder turns the mounting face): SP6 C9's wet block, SP5's janitor closet, P's fire stair.
"""
import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
_src = open(os.path.join(HERE, "zl_build.py")).read()
exec(compile(_src[:_src.index("\nBUILDERS = dict(")], "zl_build_helpers", "exec"))

REPORT = {}

# =========================================================================== FLUSH
FLUSH = dict(open=561.0, face=31.0, proud=10.0, lens_up=6.0, flange_out=4.0, latch=(30.0, 12.0, 3.5))


def flush_atlas():
    at = Atlas(512, 512)
    at.put("face", 0, 0, 452, 452, kind="flushface")
    add_swatches(at, ["enamel", "black", "zinc"], 512 - SWATCH)
    return at


def flush_lens(at, phone=False):
    acc = Acc()
    s = FLUSH
    h = s["open"] / 2 + 1.0
    z = -s["proud"] + s["lens_up"]
    n = 4 if not phone else 1
    for i in range(n):
        for j in range(n):
            x0, x1 = -h + 2 * h * i / n, -h + 2 * h * (i + 1) / n
            y0, y1 = -h + 2 * h * j / n, -h + 2 * h * (j + 1) / n
            quad(acc, [(x0, y0, z), (x1, y0, z), (x1, y1, z), (x0, y1, z)],
                 lambda p: at.uv("face", (p[0] + h) / (2 * h), (p[1] + h) / (2 * h)), (0, 0, -1))
    if phone:
        # the frame face as a flat ring in the enamel swatch, the gasket line black
        H = s["open"] / 2 + s["face"]
        zf = -s["proud"]
        o = s["open"] / 2
        for (x0, y0, x1, y1) in ((-H, -H, H, -o), (-H, o, H, H), (-H, -o, -o, o), (o, -o, H, o)):
            quad(acc, [(x0, y0, zf), (x1, y0, zf), (x1, y1, zf), (x0, y1, zf)], lambda p: at.uv("sw_enamel", .5, .5), (0, 0, -1))
    return acc


def flush_housing(lod):
    acc = Acc()
    s = FLUSH
    o = s["open"] / 2
    P, U, F = s["proud"], s["lens_up"], s["face"]
    zf = -P
    zl = -P + U
    pts = [(-0.5, zl), (0.0, zl - 0.2), (0.0, zf), (F - 2.0, zf), (F, zf + 2.5), (F + s["flange_out"] - 1.0, -0.6), (F + s["flange_out"], 0.0)]
    prof = fillet(pts, [0, 0, 1.4, 1.8, 0, 0, 0], SEGS[lod]) if lod < 2 else plain([(0.0, zl), (0.0, zf), (F, zf), (F + s["flange_out"], 0.0)])
    sweep_rect(acc, o, o, prof, lambda side, along, t, tt: trim_uv("enamel", along + 300, t, tt, side))
    if lod < 2:
        # the black gasket the lens bears on, 1.5 mm proud of the lens at its edge
        sweep_rect(acc, o - 2.5, o - 2.5, plain([(0.0, zl - 0.6), (2.5, zl - 0.6)]), lambda side, along, t, tt: trim_uv("black", along, t + 20, 100, side))
    if lod == 0:
        # four quarter-turn cam latches on the frame face, two on each long side, and a mitre seam at each corner
        lw, ll, lh = s["latch"]
        for (x, y, rot) in ((0.0, -(o + F / 2), 0), (0.0, o + F / 2, 0), (-(o + F / 2), 0.0, 1), (o + F / 2, 0.0, 1)):
            sx, sy = (ll, lw) if rot == 0 else (lw, ll)
            box(acc, (x, y, zf - lh / 2), (sx, sy, lh), lambda p, n: trim_uv("zinc", p[0] + p[1] + 400, 50, 100), skip=("+z",))
            disk(acc, x, y, zf - lh - 0.05, 4.2, 12, lambda p: trim_uv("zinc", p[0] + 700, 40, 100), down=True, dome=1.4)
            quad(acc, [(x - 3.2, y - 0.5, zf - lh - 1.5), (x + 3.2, y - 0.5, zf - lh - 1.5), (x + 3.2, y + 0.5, zf - lh - 1.5), (x - 3.2, y + 0.5, zf - lh - 1.5)],
                 lambda p: trim_uv("black", p[0] + 100, 50, 100), (0, 0, -1))
        for (cx, cy) in ((1, 1), (-1, 1), (-1, -1), (1, -1)):
            a = (cx * (o + 1.0), cy * (o + 1.0), zf - 0.12)
            b = (cx * (o + F - 1.0), cy * (o + F - 1.0), zf - 0.12)
            w = 0.35
            quad(acc, [(a[0] - w, a[1] + w, a[2]), (a[0] + w, a[1] - w, a[2]), (b[0] + w, b[1] - w, b[2]), (b[0] - w, b[1] + w, b[2])],
                 lambda p: trim_uv("black", p[0] + 50, 50, 100), (0, 0, -1))
    return acc


def build_flush():
    at = flush_atlas()
    objs = [(flush_lens(at), "Lens", ["ZL_FLUSH_Lens"])]
    for lod in (0, 1, 2):
        objs.append((flush_housing(lod), "Housing_L%d" % lod, ["LampOpt_Trim"]))
    objs.append((flush_lens(at, phone=True), "Phone", ["ZL_FLUSH_Lens"]))
    emit("ZL_FLUSH", objs, dict(id="FLUSH", spec=FLUSH, atlas=at.json()))


# =========================================================================== GLOBE
GLOBE = dict(body_r=62.0, body_h=58.0, collar_r=52.0, collar_h=14.0, globe_r=48.0, globe_len=158.0, guard_r=56.0, guard_bar=3.2,
             guard_bars=4, lamp_r=30.0, lamp_len=108.0)


def globe_atlas():
    at = Atlas(512, 512)
    at.put("jar", 0, 0, 440, 320, kind="jar")
    at.put("cap", 0, 326, 140, 140, kind="jarcap")
    add_swatches(at, ["zinc", "black", "enamel"], 512 - SWATCH)
    return at


def globe_profile(phone=False):
    """The jar's outline (r, depth below the collar) from the collar down to the bottom pole: a straight-sided glass jar
    with a rounded shoulder and a domed bottom (the 1938 form, PL23)."""
    s = GLOBE
    R, L = s["globe_r"], s["globe_len"]
    out = [(R - 6.0, 0.0), (R - 3.0, 4.0), (R, 14.0)]
    side_end = L - R * 0.62
    nside = 3 if not phone else 1
    for k in range(1, nside + 1):
        out.append((R + 1.5 * math.sin(math.pi * k / (nside + 1)), 14.0 + (side_end - 14.0) * k / nside))
    nb = 8 if not phone else 4
    for k in range(1, nb + 1):
        a = (math.pi / 2) * k / nb
        out.append((max(R * math.cos(a), 0.01), side_end + R * 0.62 * math.sin(a)))
    return out


def globe_lens(at, phone=False):
    acc = Acc()
    s = GLOBE
    z0 = -(s["body_h"] + s["collar_h"])
    prof = globe_profile(phone)
    L = prof[-1][1]
    n = 40 if not phone else 16
    lathe(acc, prof, n, lambda a, v: at.uv("jar", a, v), z0=z0, zsign=-1.0, ref=(0.0, L * 0.5))
    if phone:
        # the body and collar as plain rings in the zinc swatch
        lathe(acc, [(s["collar_r"], 0.0), (s["body_r"], -s["body_h"] * 0.25), (s["body_r"], -s["body_h"]), (s["collar_r"], -s["body_h"] - s["collar_h"])],
              12, lambda a, v: at.uv("sw_zinc", .5, .5), z0=0.0, zsign=1.0, ref=(0.0, -s["body_h"] * 0.5))
    return acc


def globe_housing(lod):
    acc = Acc()
    s = GLOBE
    n = {0: 48, 1: 24, 2: 12}[lod]
    H, CR, CH, BR = s["body_h"], s["collar_r"], s["collar_h"], s["body_r"]
    # the cast body: a flange on the box, a domed shoulder with four cast ribs, then the threaded collar
    body = [(BR + 6.0, 0.0), (BR + 6.0, -4.0), (BR, -7.0), (BR, -H * 0.55), (BR - 6.0, -H * 0.85), (CR + 4.0, -H)]
    if lod < 2:
        body = [(p, False) for p in body]
        prof = fillet([p for p, _ in body], [0, 1.5, 3.0, 6.0, 8.0, 0], SEGS[lod])
    else:
        prof = plain([(BR + 6.0, 0.0), (BR, -6.0), (BR, -H * 0.6), (CR + 4.0, -H)])
    sweep_round(acc, 0.0, prof, n, lambda a, t, T: trim_uv("zinc", a * 2 * math.pi * BR, t, T))
    collar = [(CR + 4.0, -H), (CR + 1.5, -H - 1.0)]
    nthreads = 4 if lod == 0 else 2 if lod == 1 else 0
    for k in range(nthreads):
        zc = -H - 2.5 - k * (CH - 4.0) / max(1, nthreads)
        collar += [(CR + 1.5, zc), (CR + 0.6, zc - 1.2)]
    collar += [(CR, -H - CH), (s["globe_r"] - 3.0, -H - CH)]
    sweep_round(acc, 0.0, plain(collar), n, lambda a, t, T: trim_uv("zinc", a * 2 * math.pi * CR + 200, t, T))
    if lod < 2:
        # the black gasket ring under the collar where the glass seats
        sweep_round(acc, 0.0, plain([(s["globe_r"] - 3.0, -H - CH - 0.2), (s["globe_r"] - 6.5, -H - CH - 0.2)]), n,
                    lambda a, t, T: trim_uv("black", a * 300, t + 30, 100))
        for k in range(4):
            a = math.pi / 4 + k * math.pi / 2
            x, y = (BR - 1.0) * math.cos(a), (BR - 1.0) * math.sin(a)
            box(acc, (x, y, -H * 0.42), (7.0, 7.0, H * 0.7), lambda p, nn: trim_uv("zinc", p[0] + p[1] + 600, 40, 100), skip=())
    # the guard: four cast bars from the collar ring round the globe to a bottom boss, a hoop at mid height
    z0 = -(H + CH)
    prof_g = globe_profile()
    L = prof_g[-1][1]
    nb = s["guard_bars"]
    bar_n = 6 if lod == 0 else 4
    gap = s["guard_r"] - s["globe_r"]
    if lod < 2:
        sweep_round(acc, 0.0, plain([(CR + 3.0, z0 + 1.0), (s["guard_r"] + 3.0, z0 - 2.0), (s["guard_r"] + 3.0, z0 - 7.0), (CR + 3.0, z0 - 7.0)]), n,
                    lambda a, t, T: trim_uv("zinc", a * 400, t, T))
    for k in range(nb):
        a = 2 * math.pi * (k + 0.5) / nb
        pts = []
        for (r, d) in prof_g[2:]:
            rr = r + gap
            pts.append((rr * math.cos(a), rr * math.sin(a), z0 - d - 2.0))
        pts.append((0.0, 0.0, z0 - L - gap - 1.0))
        if lod == 2:
            pts = [pts[0], pts[len(pts) // 2], pts[-1]]
        tube_path(acc, pts, s["guard_bar"], bar_n, lambda a2, l, L2: trim_uv("zinc", l + 500, a2 * 20 + 40, 100))
    if lod < 2:
        # the mid hoop and the bottom boss
        zm = z0 - L * 0.55
        rm = s["globe_r"] + 1.5 + gap
        hoop = [(rm * math.cos(2 * math.pi * k / n), rm * math.sin(2 * math.pi * k / n), zm) for k in range(n + 1)]
        tube_path(acc, hoop, s["guard_bar"] * 0.9, bar_n, lambda a2, l, L2: trim_uv("zinc", l + 800, a2 * 20 + 40, 100))
        disk(acc, 0.0, 0.0, z0 - L - gap - 1.0, 7.0, 12, lambda p: trim_uv("zinc", p[0] + 900, 40, 100), down=True, dome=3.0)
    return acc


def build_globe():
    at = globe_atlas()
    objs = [(globe_lens(at), "Lens", ["ZL_GLOBE_Lens"])]
    for lod in (0, 1, 2):
        objs.append((globe_housing(lod), "Housing_L%d" % lod, ["LampOpt_Trim"]))
    objs.append((globe_lens(at, phone=True), "Phone", ["ZL_GLOBE_Lens"]))
    emit("ZL_GLOBE", objs, dict(id="GLOBE", spec=GLOBE, atlas=at.json()))


SB = dict(FLUSH=build_flush, GLOBE=build_globe)
for kid, fn in SB.items():
    if ZL_ONLY and kid not in ZL_ONLY:
        continue
    fn()
path = os.path.join(OUT, "zl_build_report.json")
old = json.load(open(path)) if os.path.exists(path) else {}
old.update(REPORT)
json.dump(old, open(path, "w"), indent=1)
print("ZL STRUCT DONE", ", ".join(sorted(REPORT)))
