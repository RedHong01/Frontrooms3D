#!/usr/bin/env python3
"""EXIT sign legend in another script (game visual line, world text EXIT stage, 2026-11-03). No bpy.

Research and options: Documentation/research/exit_sign/80_world_text_options.md. Words: Language/<code>.txt
(叙事设计), faces and sizes: research/localization/WORLD_TYPE.md (平面视觉): 简体中文 R1 sign = 黑体 Noto Sans SC 700.

The English legend is code geometry (exit_sign_geom.letter_polys_open): EXIT is CONSTRUCTED, one stroke of 3/4 in
(S = 19.05 mm) with square ends, sharp convex corners and the punch's R 0.5 fillet in every reflex corner. A die-cut
legend in another script is constructed the same way (WORLD_TYPE.md §3 "Stencils", 平面视觉 2026-11-03 22:31): drawn
on the CJK face's skeleton with one stroke width equal to the English stroke.

Stencil (--mode stencil), CONSTRUCTED (the default): W/venv has no fontTools, so each character of the WORLD_TYPE face
(Noto Sans SC VF at the wght asked) is RENDERED by FreeType (PIL) at --pxmm pixels per mm and READ as axis strokes:
the long vertical and horizontal ink runs, grouped into strokes; each stroke's centre line and its two ends are
measured to the sub-pixel from the coverage (medians over the stroke). That is the skeleton. A stroke end that stops
in another stroke (a corner or a T) is JOINED: it runs to the far side of that stroke's constructed width, so a corner
is a full square as in EXIT; any other end is FREE and keeps the face's own end, cut square. Every stroke is then a
rectangle S wide on its centre line, the union is taken exactly (on the rectangles' own coordinates, no raster), each
counter is held by ties (below), and every reflex corner gets HOLE_FILLET. A character whose ink the axis strokes do
not cover (a diagonal or curved stroke: 安, 全) is refused: its skeleton needs drawing by hand (option A is not built).
Ends of one kind are drawn to one length (平面视觉 11-04, WORLD_TYPE §3 "Constructed ends", "口's two feet equal"):
the free ends of one character on one axis and one side whose face ends lie within --kind-tol x the em of each other
take their mean, at the em built (construction.endKinds lists each kind). Height means ink (11-04): --ink-floor refuses
a legend whose ink stands lower than a life-safety floor (EXIT's 152.4 mm letters: 出口 at em 162.6, ink 152.4).
Air at the sides (11-04 07:0x, WORLD_TYPE §3): --side-air X refuses a legend whose side margins stand under X times its
top / bottom ones (1.25 at the floor height; construction.sideAir records them); --width auto takes the widest 0.001
step of 长体 that keeps it (the skeleton narrowed across, the constructed strokes, ties and ends unchanged), never below
85 % (then a shorter word signed by 叙事): 出口 at em 162.6 -> 0.949 (95.0 % leaves the sides 1.244 x).
Ties: bars of plate left across the stroke from a counter (an island of the plate: 口) to the open plate, where the
stroke is thinnest (a constructed legend has one width, so top and bottom win, WORLD_TYPE: "口: top and bottom");
one tie for a small island, two opposite ties for one of at least --tie-big em^2; width --tie-frac x the stroke (at
least --tie-min mm).
Stencil TRACED (--traced; the 21:32 build of option C, kept for the record and the comparison): the outlines are
traced (marching squares on the coverage at 0.5, Douglas-Peucker at --tol mm, a corner clean-up), the ties painted in
the raster and re-traced, width --tie-frac x the face's vertical stem; reflex corners over 25 degrees get the fillet.
Print (--mode print): the plate has one window (the opening, 2 mm in) and the ink (traced: printing follows the face)
is printed on the lens: the loops (counters included, as clockwise loops) go to the texture generator; no ties.

Output JSON (OPENING mm: origin = the opening's bottom-left, x to the reader's right, y up; CCW holes):
  holes      the plate's through-holes (stencil: the tied ink; print: the window)
  ink        the printed / lit shape as loops (outer CCW, counters CW) -- what glows
  ties, strokes (vertical stem / horizontal stroke), ink box, margins, counts, the face file's sha1;
  construction (constructed only): the skeleton (each stroke's centre line, ends and how each end was set, the face's
  own width there), the coverage check per character, the stroke and fillet used.
  python3 exit_legend.py --word 出口 --em 162.6 --width auto --side-air 1.25 --mode stencil --ink-floor 152.4 --out legend.json --preview p.png
  python3 exit_legend.py --set-shares <exit_sign_textures_report.json> --out legend.json   (after gen_exit_sign.py:
      writes the lamp channels' letter shares the generator measured through the legend into the legend file, which the
      kit modules copy into each kit's sidecar for FrontRoomsExitSign)
  python3 exit_legend.py --set-lod1 <the same word at a coarser --tol> --out legend.json   (a long legend, 2026-11-04:
      the Latin and Cyrillic words of 平面视觉's die face (exit_legend_dieface.py + exit_dieface.swift beside this file)
      cut 170-286 hole vertices against EXIT's 56; the kit's LOD1 plate takes the coarse build's tied holes without the
      punch fillets, LOD0 keeps the hero holes. The coarse file: python3 exit_legend_dieface.py de --tol 1.0 --out DIR
      (never into this folder: exit_legend_dieface.py writes exit_legend_<code>.json and would replace the legend that
      carries letterShares and lod1), then --set-lod1 DIR/exit_legend_de.json)
  A Latin or Cyrillic legend in this folder (exit_legend_<code>.json) = exit_legend_dieface.py's file + letterShares
  (--set-shares) + lod1 (--set-lod1); nothing else is changed in it.
  A CJK legend built here (出口: zh-Hans on Noto Sans SC, ja on Noto Sans JP; 2026-11-04) takes its LOD1 the same way from
  the same word built with --fillet 0 (all else as the hero's: --em, the --width found, --font): its tied holes without
  the punch fillets (axis strokes, no arc to flatten). E.g. ja: python3 exit_legend.py --word 出口 --font <NotoSansJP-Bold.otf>
  --em 164.2 --width 0.945 --side-air 1.25 --ink-floor 152.4 --fillet 0 --out DIR/ja_f0.json, then --set-lod1 DIR/ja_f0.json.
"""

import argparse
import hashlib
import json
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
OPEN_W, OPEN_H = 320.0, 186.0          # exit_sign_geom.OPEN_W / OPEN_H
OPEN_R = 3.0
HOLE_FILLET = 0.5                      # exit_sign_geom.HOLE_FILLET
FONT_SC = os.path.expanduser("~/Library/Fonts/NotoSansSC-VariableFont_wght.ttf")


# ------------------------------------------------------------------ geometry helpers
def area(poly):
    n = len(poly)
    return 0.5 * sum(poly[i][0] * poly[(i + 1) % n][1] - poly[(i + 1) % n][0] * poly[i][1] for i in range(n))


def centroid(poly):
    a = area(poly)
    n = len(poly)
    cx = cy = 0.0
    for i in range(n):
        x0, y0 = poly[i]
        x1, y1 = poly[(i + 1) % n]
        c = x0 * y1 - x1 * y0
        cx += (x0 + x1) * c
        cy += (y0 + y1) * c
    return (cx / (6 * a), cy / (6 * a))


def inside(pt, poly):
    x, y = pt
    c = False
    n = len(poly)
    for i in range(n):
        x0, y0 = poly[i]
        x1, y1 = poly[(i + 1) % n]
        if (y0 > y) != (y1 > y):
            xi = x0 + (y - y0) * (x1 - x0) / (y1 - y0)
            if xi > x:
                c = not c
    return c


def dp(points, tol):
    """Douglas-Peucker on an open polyline (list of (x, y))."""
    pts = np.asarray(points, np.float64)
    keep = np.zeros(len(pts), bool)
    keep[0] = keep[-1] = True
    stack = [(0, len(pts) - 1)]
    while stack:
        a, b = stack.pop()
        if b <= a + 1:
            continue
        p, q = pts[a], pts[b]
        d = q - p
        L = math.hypot(d[0], d[1])
        seg = pts[a + 1:b]
        if L < 1e-12:
            dist = np.hypot(seg[:, 0] - p[0], seg[:, 1] - p[1])
        else:
            dist = np.abs(d[0] * (seg[:, 1] - p[1]) - d[1] * (seg[:, 0] - p[0])) / L
        k = int(np.argmax(dist))
        if dist[k] > tol:
            m = a + 1 + k
            keep[m] = True
            stack.append((a, m))
            stack.append((m, b))
    return [tuple(x) for x in pts[keep]]


def dp_closed(loop, tol):
    pts = loop
    n = len(pts)
    if n < 8:
        return list(pts)
    p0 = np.asarray(pts[0])
    d = [math.hypot(p[0] - p0[0], p[1] - p0[1]) for p in pts]
    k = int(np.argmax(d))
    a = dp(pts[:k + 1], tol)
    b = dp(pts[k:] + [pts[0]], tol)
    out = a[:-1] + b[:-1]
    return out


def clean(poly, min_len):
    """Merge vertices closer than min_len (keeps the sharper corner) and drop collinear points."""
    pts = list(poly)
    changed = True
    while changed and len(pts) > 3:
        changed = False
        n = len(pts)
        for i in range(n):
            a, b = pts[i], pts[(i + 1) % n]
            if math.hypot(b[0] - a[0], b[1] - a[1]) < min_len:
                # keep the one with the bigger turn
                def turn(j):
                    p0, p1, p2 = pts[j - 1], pts[j], pts[(j + 1) % n]
                    e0 = (p1[0] - p0[0], p1[1] - p0[1])
                    e1 = (p2[0] - p1[0], p2[1] - p1[1])
                    l0, l1 = math.hypot(*e0) or 1e-12, math.hypot(*e1) or 1e-12
                    return abs(math.atan2(e0[0] * e1[1] - e0[1] * e1[0], e0[0] * e1[0] + e0[1] * e1[1]))
                drop = (i + 1) % n if turn(i) >= turn((i + 1) % n) else i
                pts.pop(drop)
                changed = True
                break
    # collinear clean-up (turn below 0.5 degree)
    out = []
    n = len(pts)
    for i in range(n):
        p0, p1, p2 = pts[i - 1], pts[i], pts[(i + 1) % n]
        e0 = (p1[0] - p0[0], p1[1] - p0[1])
        e1 = (p2[0] - p1[0], p2[1] - p1[1])
        ang = abs(math.atan2(e0[0] * e1[1] - e0[1] * e1[0], e0[0] * e1[0] + e0[1] * e1[1]))
        if ang > math.radians(0.5):
            out.append(p1)
    return out


def axis_snap(poly, tol=0.15, min_len=3.0):
    """Edges within tol mm of vertical or horizontal over at least min_len mm are made exactly so (the traced
    corners of a stroke sit a few hundredths of a mm off the font's straight edge)."""
    pts = [list(p) for p in poly]
    n = len(pts)
    for axis in (0, 1):                      # 0: vertical edges (snap x), 1: horizontal edges (snap y)
        for i in range(n):
            a, b = pts[i], pts[(i + 1) % n]
            dx, dy = abs(b[0] - a[0]), abs(b[1] - a[1])
            if axis == 0 and dx < tol and dy >= min_len:
                m = (a[0] + b[0]) / 2
                a[0] = b[0] = m
            elif axis == 1 and dy < tol and dx >= min_len:
                m = (a[1] + b[1]) / 2
                a[1] = b[1] = m
    return [tuple(p) for p in pts]


def fillet_reflex(poly, r, segs=2, min_turn_deg=25.0):
    """exit_sign_geom.fillet_reflex for traced outlines: only corners that turn more than min_turn_deg get the
    punch's fillet (a traced curve's small turns stay as they are)."""
    out = []
    n = len(poly)
    for i in range(n):
        p0, p1, p2 = poly[i - 1], poly[i], poly[(i + 1) % n]
        e0 = (p1[0] - p0[0], p1[1] - p0[1])
        e1 = (p2[0] - p1[0], p2[1] - p1[1])
        cross = e0[0] * e1[1] - e0[1] * e1[0]
        l0, l1 = math.hypot(*e0), math.hypot(*e1)
        turn = abs(math.degrees(math.atan2(cross, e0[0] * e1[0] + e0[1] * e1[1])))
        if cross >= 0 or r <= 0 or turn < min_turn_deg or l0 < 1e-9 or l1 < 1e-9:
            out.append(p1)
            continue
        u0 = (e0[0] / l0, e0[1] / l0)
        u1 = (e1[0] / l1, e1[1] / l1)
        ang = math.acos(max(-1.0, min(1.0, -(u0[0] * u1[0] + u0[1] * u1[1]))))
        t = r / math.tan(ang / 2)
        t = min(t, 0.45 * l0, 0.45 * l1)
        a = (p1[0] - u0[0] * t, p1[1] - u0[1] * t)
        b = (p1[0] + u1[0] * t, p1[1] + u1[1] * t)
        for k in range(segs + 1):
            s = k / segs
            out.append(((1 - s) ** 2 * a[0] + 2 * (1 - s) * s * p1[0] + s * s * b[0],
                        (1 - s) ** 2 * a[1] + 2 * (1 - s) * s * p1[1] + s * s * b[1]))
    return out


def rounded_rect(w, h, r, seg, cx=0.0, cy=0.0):
    hw, hh = w / 2, h / 2
    pts = []
    for (x, y, a0) in ((hw - r, -hh + r, -90), (hw - r, hh - r, 0), (-hw + r, hh - r, 90), (-hw + r, -hh + r, 180)):
        for i in range(seg + 1):
            a = math.radians(a0 + 90 * i / seg)
            pts.append((cx + x + r * math.cos(a), cy + y + r * math.sin(a)))
    out = []
    for p in pts:
        if not out or abs(p[0] - out[-1][0]) > 1e-9 or abs(p[1] - out[-1][1]) > 1e-9:
            out.append(p)
    if abs(out[0][0] - out[-1][0]) < 1e-9 and abs(out[0][1] - out[-1][1]) < 1e-9:
        out.pop()
    return out


# ------------------------------------------------------------------ marching squares
def trace(f, iso=0.5):
    """Closed iso-contours of f (rows down) in pixel coordinates (x right, y down), each oriented with the
    high side (ink) on its LEFT when y points up (i.e. CCW around ink, CW around a counter, after y flip).
    f must be below iso on its whole border."""
    H, W = f.shape
    a = f[:-1, :-1] >= iso
    b = f[:-1, 1:] >= iso
    c = f[1:, 1:] >= iso
    d = f[1:, :-1] >= iso
    case = (a.astype(np.uint8) << 3) | (b.astype(np.uint8) << 2) | (c.astype(np.uint8) << 1) | d.astype(np.uint8)
    ii, jj = np.nonzero((case != 0) & (case != 15))
    cs = case[ii, jj]
    segs = {}         # edge id -> list of (other edge id)
    pos = {}          # edge id -> (x, y)

    def hid(i, j):    # horizontal edge between (i, j) and (i, j+1)
        return i * W + j

    def vid(i, j):    # vertical edge between (i, j) and (i+1, j)
        return H * W + i * W + j

    def ept(e):
        if e in pos:
            return
        if e < H * W:
            i, j = divmod(e, W)
            v0, v1 = f[i, j], f[i, j + 1]
            t = (iso - v0) / (v1 - v0)
            pos[e] = (float(j + t), float(i))
        else:
            i, j = divmod(e - H * W, W)
            v0, v1 = f[i, j], f[i + 1, j]
            t = (iso - v0) / (v1 - v0)
            pos[e] = (float(j), float(i + t))

    # Directed segments, ink on the left in the y-down image frame is ink on the right... so we build undirected
    # links and orient each loop afterwards by sampling.
    links = {}

    def link(e0, e1):
        links.setdefault(e0, []).append(e1)
        links.setdefault(e1, []).append(e0)

    for k in range(len(ii)):
        i, j, cc = int(ii[k]), int(jj[k]), int(cs[k])
        T, R, B, L = hid(i, j), vid(i, j + 1), hid(i + 1, j), vid(i, j)
        if cc in (1, 14):
            pr = [(L, B)]
        elif cc in (2, 13):
            pr = [(B, R)]
        elif cc in (3, 12):
            pr = [(L, R)]
        elif cc in (4, 11):
            pr = [(T, R)]
        elif cc in (6, 9):
            pr = [(T, B)]
        elif cc in (7, 8):
            pr = [(L, T)]
        elif cc == 5:      # b and d high
            centre = (f[i, j] + f[i, j + 1] + f[i + 1, j + 1] + f[i + 1, j]) / 4
            pr = [(T, R), (L, B)] if centre < iso else [(T, L), (B, R)]
        elif cc == 10:     # a and c high
            centre = (f[i, j] + f[i, j + 1] + f[i + 1, j + 1] + f[i + 1, j]) / 4
            pr = [(L, T), (B, R)] if centre < iso else [(T, R), (L, B)]
        else:
            continue
        for e0, e1 in pr:
            ept(e0)
            ept(e1)
            link(e0, e1)
    loops = []
    seen = set()
    for start in list(links.keys()):
        if start in seen:
            continue
        loop = [start]
        seen.add(start)
        prev, cur = None, start
        while True:
            nb = links[cur]
            nxt = nb[0] if nb[0] != prev else (nb[1] if len(nb) > 1 else nb[0])
            if nxt == start:
                break
            if nxt in seen:
                break
            loop.append(nxt)
            seen.add(nxt)
            prev, cur = cur, nxt
        if len(loop) >= 3:
            loops.append([pos[e] for e in loop])
    return loops


def orient_loops(loops_px, f, pxmm, x0_mm, y_top_px):
    """px loops -> mm loops (y up), each oriented with the ink on its left: CCW around ink, CW around a counter."""
    H, W = f.shape
    out = []
    for lp in loops_px:
        mm = [(x0_mm + x / pxmm, (y_top_px - y) / pxmm) for x, y in lp]
        a = area(mm)
        # which side is ink? sample a point just left of the first long-enough edge (in mm, y up)
        n = len(mm)
        side = None
        for k in range(n):
            p, q = mm[k], mm[(k + 1) % n]
            dx, dy = q[0] - p[0], q[1] - p[1]
            L = math.hypot(dx, dy)
            if L < 0.02:
                continue
            mx, my = (p[0] + q[0]) / 2, (p[1] + q[1]) / 2
            lx, ly = mx - dy / L * 0.06, my + dx / L * 0.06        # 0.06 mm to the left
            px = (lx - x0_mm) * pxmm
            py = y_top_px - ly * pxmm
            jx, iy = int(round(px)), int(round(py))
            if 0 <= iy < H and 0 <= jx < W:
                side = f[iy, jx] >= 0.5
                break
        if side is None:
            continue
        if not side:
            mm = list(reversed(mm))
        out.append(mm)
    return out


# ------------------------------------------------------------------ the legend
def render_char(font, ch, pxmm, pad_px):
    """Coverage of one character: array (rows down), the pen origin (px), the advance (px)."""
    adv = font.getlength(ch)
    l, t, r, b = font.getbbox(ch, anchor="ls")
    w = int(math.ceil(max(r, adv) - min(l, 0))) + 2 * pad_px
    h = int(math.ceil(b - t)) + 2 * pad_px
    ox = pad_px - min(l, 0)
    oy = pad_px - t
    img = Image.new("L", (w, h), 0)
    ImageDraw.Draw(img).text((ox, oy), ch, font=font, fill=255, anchor="ls")
    return img, ox, oy, adv


def stroke_widths(f, pxmm, em_px):
    """Vertical stem (horizontal ink runs) and horizontal stroke (vertical ink runs), mm: the mode of runs between
    0.06 and 0.2 em (the 黑体 stems are about 0.12 em)."""
    m = f >= 0.5

    def runs(mat):
        out = []
        for row in mat:
            d = np.diff(np.concatenate([[0], row.astype(np.int8), [0]]))
            s = np.nonzero(d == 1)[0]
            e = np.nonzero(d == -1)[0]
            out.append(e - s)
        return np.concatenate(out) if out else np.array([])

    lo, hi = 0.06 * em_px, 0.2 * em_px
    res = []
    for mat in (m, m.T):
        r = runs(mat[::3])
        r = r[(r >= lo) & (r <= hi)]
        if len(r) == 0:
            res.append(None)
            continue
        hist, edges = np.histogram(r, bins=np.arange(lo, hi + 2, 2))
        k = int(np.argmax(hist))
        res.append(float((edges[k] + edges[k + 1]) / 2 / pxmm))
    return res


def ray_run(f, pxmm, x0_mm, y_top_px, start, d, counters, self_idx, step=0.05, max_mm=60.0):
    """From start (mm) along d: (entry, exit) distances through ink to the open plate, or None."""
    H, W = f.shape
    state = 0
    t = 0.0
    entry = None
    while t < max_mm:
        x, y = start[0] + d[0] * t, start[1] + d[1] * t
        jx = int(round((x - x0_mm) * pxmm))
        iy = int(round(y_top_px - y * pxmm))
        if not (0 <= iy < H and 0 <= jx < W):
            return None
        ink = f[iy, jx] >= 0.5
        if state == 0 and ink:
            state, entry = 1, t
        elif state == 1 and not ink:
            # out of the ink: is it the open plate (not another counter)?
            for k, c in enumerate(counters):
                if k != self_idx and inside((x, y), c):
                    return None
            if inside((x, y), counters[self_idx]):
                return None
            return entry, t
        t += step
    return None


def build(word, em_mm, width, mode, font_path, wght, pxmm, tol, tie_frac, tie_min, tie_big_em2, note=""):
    em_px = int(round(em_mm * pxmm))
    font = ImageFont.truetype(font_path, em_px)
    try:
        axes = font.get_variation_axes()
        font.set_variation_by_axes([wght])
    except Exception as e:      # a static face
        axes = str(e)
    pad = int(3 * pxmm)
    chars = []
    x_mm = 0.0
    loops_all = []
    counters_all = []
    char_info = []
    f_store = []
    for ch in word:
        img, ox, oy, adv = render_char(font, ch, pxmm, pad)
        if width != 1.0:
            nw = max(1, int(round(img.size[0] * width)))
            img = img.resize((nw, img.size[1]), Image.BOX)
            ox = ox * width
        f = np.asarray(img, np.float32) / 255.0
        f_store.append((ch, f, ox, oy, x_mm))
        x_mm += adv * width / pxmm
    total_adv_mm = x_mm
    # stroke widths from the first character with a stem
    sw = [stroke_widths(f, pxmm, em_px) for ch, f, ox, oy, xm in f_store]
    vs = [s[0] for s in sw if s[0]]
    hs = [s[1] for s in sw if s[1]]
    stem_v = float(np.median(vs)) if vs else None
    stem_h = float(np.median(hs)) if hs else None
    tie_w = max(tie_min, tie_frac * (stem_v or 0.0))
    ties = []
    ink_loops = []
    hole_loops = []
    islands = 0
    for ch, f, ox, oy, xm in f_store:
        x0 = xm - ox / pxmm                          # mm of the raster's left edge (pen at xm)
        ytop = oy                                    # px row of the baseline -> y_mm = (oy - row) / pxmm (baseline 0)
        loops = orient_loops(trace(f), f, pxmm, x0, ytop)
        outer = [lp for lp in loops if area(lp) > 0]
        counters = [lp for lp in loops if area(lp) < 0]
        ink_loops += [axis_snap(clean(dp_closed(lp, tol), 0.12)) for lp in loops]
        islands += len(counters)
        info = {"char": ch, "counters": [round(float(-area(c)), 2) for c in counters]}
        if mode == "stencil" and counters:
            img = Image.fromarray(np.clip(np.round(f * 255), 0, 255).astype(np.uint8))
            dr = ImageDraw.Draw(img)
            for k, c in enumerate(counters):
                cen = centroid(list(reversed(c)))
                big = -area(c) >= tie_big_em2 * em_mm * em_mm
                runs = {}
                for name, d in (("up", (0, 1)), ("down", (0, -1)), ("left", (-1, 0)), ("right", (1, 0))):
                    r = ray_run(f, pxmm, x0, ytop, cen, d, counters, k)
                    if r is not None:
                        runs[name] = (r, d)
                if not runs:
                    raise RuntimeError("no tie path for a counter of " + ch)
                if big and (("up" in runs and "down" in runs) or ("left" in runs and "right" in runs)):
                    pairs = []
                    for p in (("up", "down"), ("left", "right")):
                        if p[0] in runs and p[1] in runs:
                            L = max(runs[p[0]][0][1] - runs[p[0]][0][0], runs[p[1]][0][1] - runs[p[1]][0][0])
                            pairs.append((L * (1.0 if p[0] == "up" else 1.03), p))
                    pairs.sort()
                    chosen = list(pairs[0][1])
                else:
                    chosen = [min(runs, key=lambda n: runs[n][0][1] - runs[n][0][0])]
                for name in chosen:
                    (t0, t1), d = runs[name]
                    a0, a1 = t0 - 1.5, t1 + 1.5
                    px_, py_ = -d[1], d[0]
                    hw = tie_w / 2
                    quad = [(cen[0] + d[0] * a0 + px_ * hw, cen[1] + d[1] * a0 + py_ * hw),
                            (cen[0] + d[0] * a1 + px_ * hw, cen[1] + d[1] * a1 + py_ * hw),
                            (cen[0] + d[0] * a1 - px_ * hw, cen[1] + d[1] * a1 - py_ * hw),
                            (cen[0] + d[0] * a0 - px_ * hw, cen[1] + d[1] * a0 - py_ * hw)]
                    dr.polygon([((x - x0) * pxmm, ytop - y * pxmm) for x, y in quad], fill=0)
                    ties.append({"char": ch, "dir": name, "centre_mm": [round(cen[0] + d[0] * (t0 + t1) / 2, 2), round(cen[1] + d[1] * (t0 + t1) / 2, 2)],
                                 "width_mm": round(tie_w, 2), "across_mm": round(t1 - t0, 2)})
            f2 = np.asarray(img, np.float32) / 255.0
            loops2 = orient_loops(trace(f2), f2, pxmm, x0, ytop)
            left = [lp for lp in loops2 if area(lp) < 0]
            if left:
                raise RuntimeError("%s: %d counters left after ties" % (ch, len(left)))
            holes = loops2
        else:
            holes = outer if mode == "stencil" else []
        for lp in holes:
            s = axis_snap(clean(dp_closed(lp, tol), 0.12))
            hole_loops.append(fillet_reflex(s, HOLE_FILLET))
        info["outer"] = len(outer)
        char_info.append(info)
    # place: the ink box centred in the opening
    if mode == "stencil":
        allp = [p for lp in hole_loops for p in lp]
    else:
        allp = [p for lp in ink_loops for p in lp]
    xs = [p[0] for p in allp]
    ys = [p[1] for p in allp]
    bx0, bx1, by0, by1 = min(xs), max(xs), min(ys), max(ys)
    dx = OPEN_W / 2 - (bx0 + bx1) / 2
    dy = OPEN_H / 2 - (by0 + by1) / 2
    mv = lambda lp: [(round(x + dx, 4), round(y + dy, 4)) for x, y in lp]
    hole_loops = [mv(lp) for lp in hole_loops]
    ink_loops = [mv(lp) for lp in ink_loops]
    for t in ties:
        t["centre_mm"] = [round(t["centre_mm"][0] + dx, 2), round(t["centre_mm"][1] + dy, 2)]
    if mode == "print":
        hole_loops = [rounded_rect(OPEN_W - 4.0, OPEN_H - 4.0, OPEN_R, 3, OPEN_W / 2, OPEN_H / 2)]
    ink_box = [round(bx0 + dx, 2), round(by0 + dy, 2), round(bx1 + dx, 2), round(by1 + dy, 2)]
    with open(font_path, "rb") as fh:
        font_sha = hashlib.sha1(fh.read()).hexdigest()
    verts = sum(len(lp) for lp in hole_loops)
    return {
        "word": word, "mode": mode, "emMm": round(em_px / pxmm, 3), "emAsked": em_mm, "width": width,
        "face": os.path.basename(font_path), "faceSha1": font_sha, "wght": wght, "axes": str(axes),
        "pxPerMm": pxmm, "tolMm": tol, "advanceMm": round(total_adv_mm, 2),
        "inkBox": ink_box, "inkW": round(bx1 - bx0, 2), "inkH": round(by1 - by0, 2),
        "margins": {"left": ink_box[0], "right": round(OPEN_W - ink_box[2], 2), "bottom": ink_box[1], "top": round(OPEN_H - ink_box[3], 2)},
        "strokes": {"verticalStemMm": round(stem_v, 2) if stem_v else None, "horizontalStrokeMm": round(stem_h, 2) if stem_h else None},
        "islands": islands, "ties": ties, "tieWidthMm": round(tie_w, 2) if mode == "stencil" else 0,
        "chars": char_info, "holeVerts": verts, "holes": hole_loops, "ink": ink_loops, "note": note,
    }


# ------------------------------------------------------------------ the constructed legend (die face, as EXIT)
S_EN = 19.05                           # exit_sign_geom.S: the English stroke, 3/4 in


def _runs(v):
    d = np.diff(np.concatenate([[0], v.astype(np.int8), [0]]))
    return np.nonzero(d == 1)[0], np.nonzero(d == -1)[0]


def _edge_lo(fv, s):
    """Sub-pixel position (pixel edges at integers) of the rising edge whose first pixel at or over 0.5 is s."""
    a = float(fv[s - 1]) if s >= 1 else 0.0
    return s + 1.0 - float(fv[s]) - a


def _edge_hi(fv, e):
    """Sub-pixel position of the falling edge whose last pixel at or over 0.5 is e - 1."""
    b = float(fv[e]) if e < len(fv) else 0.0
    return (e - 1) + float(fv[e - 1]) + b


def _stroke_groups(m, min_px):
    """Long runs along one axis (rows of m are the lines across which a stroke is wide; each line's runs at least
    min_px long), grouped over neighbouring lines that overlap: one group = one stroke."""
    lines = []
    for k in range(m.shape[0]):
        s, e = _runs(m[k])
        lines.append([(int(a), int(b)) for a, b in zip(s, e) if b - a >= min_px])
    parent = {}

    def find(x):
        while parent[x] != x:
            parent[x] = parent[parent[x]]
            x = parent[x]
        return x

    for k, rr in enumerate(lines):
        for r in rr:
            parent[(k, r)] = (k, r)
    for k in range(1, len(lines)):
        for a, b in lines[k]:
            for c, d in lines[k - 1]:
                if a < d and c < b:
                    ra, rb = find((k, (a, b))), find((k - 1, (c, d)))
                    if ra != rb:
                        parent[ra] = rb
    groups = {}
    for key in parent:
        groups.setdefault(find(key), []).append(key)
    out = []
    for g in groups.values():
        by = {}
        for k, r in g:
            by.setdefault(k, []).append(r)
        out.append(by)
    return out


def axis_strokes(f, pxmm, x0_mm, ybase_px, min_run_mm=30.0):
    """The face's strokes as axis rectangles, measured to the sub-pixel: [{axis V|H, centre, width, lo, hi}] in mm
    (x from the character's pen, y up from the baseline). A V stroke: centre = x of its centre line, lo/hi = its
    bottom / top end; an H stroke: centre = y, lo/hi = its left / right end."""
    m = f >= 0.5
    min_px = int(round(min_run_mm * pxmm))
    out = []
    for axis in ("V", "H"):
        A = m.T if axis == "V" else m              # rows of A: lines across the stroke's width
        F = f.T if axis == "V" else f
        for by in _stroke_groups(A, min_px):
            ks = sorted(by)
            k0, k1 = ks[0], ks[-1]
            wpx = k1 - k0 + 1
            # lateral edges: in each position along the stroke where the ink across it is only this stroke (no bar
            # crossing there), the run's two sub-pixel edges; medians
            along = sorted(set(p for k in ks for a, b in by[k] for p in range(a, b)))
            kmid = (k0 + k1) // 2
            lo_e, hi_e = [], []
            for p in along[::2]:
                if axis == "V":                                  # across the stroke at position p along it
                    line, fl = m[p, :], f[p, :]
                else:
                    line, fl = m[:, p], f[:, p]
                s, e = _runs(line)
                hit = [(a, b) for a, b in zip(s, e) if a <= kmid < b]
                if not hit:
                    continue
                a, b = hit[0]
                if b - a > 1.25 * wpx:
                    continue                                     # a crossing stroke widens the run here
                lo_e.append(_edge_lo(fl, a))
                hi_e.append(_edge_hi(fl, b))
            if not lo_e:
                continue                                         # no clean section: not an axis stroke (the coverage check refuses)
            c_lo, c_hi = float(np.median(lo_e)), float(np.median(hi_e))
            # ends: along the central half of the width, the first and last ink of the stroke's run; medians
            q0, q1 = int(round(c_lo + 0.25 * (c_hi - c_lo))), int(round(c_hi - 0.25 * (c_hi - c_lo)))
            e_lo, e_hi = [], []
            for k in range(max(q0, k0), min(q1, k1) + 1):
                if k not in by:
                    continue
                a = min(r[0] for r in by[k])
                b = max(r[1] for r in by[k])
                Fk = F[k]
                e_lo.append(_edge_lo(Fk, a))
                e_hi.append(_edge_hi(Fk, b))
            if not e_lo:
                continue
            a_px, b_px = float(np.median(e_lo)), float(np.median(e_hi))
            if axis == "V":
                cx = x0_mm + (c_lo + c_hi) / 2 / pxmm
                out.append({"axis": "V", "centre": cx, "width": (c_hi - c_lo) / pxmm,
                            "lo": (ybase_px - b_px) / pxmm, "hi": (ybase_px - a_px) / pxmm})
            else:
                cy = (ybase_px - (c_lo + c_hi) / 2) / pxmm
                out.append({"axis": "H", "centre": cy, "width": (c_hi - c_lo) / pxmm,
                            "lo": x0_mm + a_px / pxmm, "hi": x0_mm + b_px / pxmm})
    out.sort(key=lambda s: (s["axis"], s["centre"]))
    return out


def stroke_rect(s, width=None, lo=None, hi=None):
    """(x0, y0, x1, y1) of a stroke: its centre line +- width / 2, from lo to hi."""
    w = s["width"] if width is None else width
    a = s["lo"] if lo is None else lo
    b = s["hi"] if hi is None else hi
    if s["axis"] == "V":
        return (s["centre"] - w / 2, a, s["centre"] + w / 2, b)
    return (a, s["centre"] - w / 2, b, s["centre"] + w / 2)


def raster_rects(rects, shape, pxmm, x0_mm, ybase_px):
    """Pixels whose centre lies in any rect (the frame of the character raster)."""
    H, W = shape
    out = np.zeros(shape, bool)
    xc = (np.arange(W) + 0.5) / pxmm + x0_mm
    yc = (ybase_px - (np.arange(H) + 0.5)) / pxmm
    for x0, y0, x1, y1 in rects:
        cols = (xc >= x0) & (xc < x1)
        rows = (yc >= y0) & (yc < y1)
        out[np.ix_(rows, cols)] = True
    return out


def rect_union_loops(plus, minus=()):
    """Exact outline of (union of plus) minus (union of minus), all axis rectangles (x0, y0, x1, y1): the cells of
    the rectangles' own coordinate grid, their boundary walked with the ink on the left (outer loops CCW, counters CW;
    at a pinch the walk takes the leftmost turn), collinear points dropped."""
    xs = sorted(set(round(v, 6) for r in list(plus) + list(minus) for v in (r[0], r[2])))
    ys = sorted(set(round(v, 6) for r in list(plus) + list(minus) for v in (r[1], r[3])))
    nx, ny = len(xs) - 1, len(ys) - 1
    xc = [(xs[i] + xs[i + 1]) / 2 for i in range(nx)]
    yc = [(ys[j] + ys[j + 1]) / 2 for j in range(ny)]
    cov = np.zeros((nx, ny), bool)
    for rs, val in ((plus, True), (minus, False)):
        for x0, y0, x1, y1 in rs:
            for i in range(nx):
                if x0 < xc[i] < x1:
                    for j in range(ny):
                        if y0 < yc[j] < y1:
                            cov[i, j] = val
    c = lambda i, j: 0 <= i < nx and 0 <= j < ny and bool(cov[i, j])
    edges = {}
    for i in range(nx):
        for j in range(ny):
            if not cov[i, j]:
                continue
            if not c(i, j - 1):
                edges.setdefault((i, j), []).append((i + 1, j))
            if not c(i + 1, j):
                edges.setdefault((i + 1, j), []).append((i + 1, j + 1))
            if not c(i, j + 1):
                edges.setdefault((i + 1, j + 1), []).append((i, j + 1))
            if not c(i - 1, j):
                edges.setdefault((i, j + 1), []).append((i, j))
    loops = []
    while edges:
        start = next(iter(edges))
        loop = [start]
        cur = start
        prev_dir = None
        while True:
            outs = edges[cur]
            if len(outs) == 1 or prev_dir is None:
                nxt = outs[0]
            else:
                def turn(n):
                    d = (n[0] - cur[0], n[1] - cur[1])
                    return prev_dir[0] * d[1] - prev_dir[1] * d[0]   # > 0 = a left turn
                nxt = max(outs, key=turn)
            outs.remove(nxt)
            if not outs:
                del edges[cur]
            prev_dir = (nxt[0] - cur[0], nxt[1] - cur[1])
            cur = nxt
            if cur == start and (start not in edges):
                break
            if cur == start:
                break
            loop.append(cur)
        pts = [(xs[i], ys[j]) for i, j in loop]
        out = []
        n = len(pts)
        for k in range(n):
            p0, p1, p2 = pts[k - 1], pts[k], pts[(k + 1) % n]
            if (p1[0] - p0[0]) * (p2[1] - p1[1]) - (p1[1] - p0[1]) * (p2[0] - p1[0]) != 0:
                out.append(p1)
        loops.append(out)
    return loops


class SideAirError(RuntimeError):
    """the legend crowds its opening's sides (WORLD_TYPE §3 'Air at the sides', 11-04)"""


def build_constructed(word, em_mm, width, font_path, wght, pxmm, stroke, fillet, tie_frac, tie_min, tie_big_em2,
                      note="", join_tol=0.6, min_run_mm=30.0, max_miss=0.004, kind_tol=0.01, ink_floor=None, side_air=None):
    em_px = int(round(em_mm * pxmm))
    kinds = []
    font = ImageFont.truetype(font_path, em_px)
    try:
        axes = font.get_variation_axes()
        font.set_variation_by_axes([wght])
    except Exception as e:      # a static face
        axes = str(e)
    pad = int(3 * pxmm)
    hw = stroke / 2.0
    x_mm = 0.0
    plus = []
    skeleton = []
    coverage = {}
    noto_v, noto_h = [], []
    per_char_rects = []
    for ch in word:
        img, ox, oy, adv = render_char(font, ch, pxmm, pad)
        f = np.asarray(img, np.float32) / 255.0
        x0 = -ox / pxmm                                  # mm of the raster's left edge, pen at 0
        st = axis_strokes(f, pxmm, x0, oy, min_run_mm)
        # the coverage check: the face's ink = the union of its axis strokes at the face's own widths and ends
        m = f >= 0.5
        mr = raster_rects([stroke_rect(s) for s in st], f.shape, pxmm, x0, oy)
        miss = int(np.count_nonzero(m & ~mr))
        extra = int(np.count_nonzero(mr & ~m))
        ink = int(np.count_nonzero(m))
        coverage[ch] = {"inkPx": ink, "notCoveredPx": miss, "overPx": extra, "notCoveredFrac": round(miss / ink, 5),
                        "overFrac": round(extra / ink, 5), "strokes": len(st)}
        if miss > max_miss * ink or extra > max_miss * ink:
            raise RuntimeError("%s: the face's ink is not axis strokes (%.2f %% not covered, %.2f %% over): its skeleton "
                               "needs drawing by hand; refused" % (ch, 100.0 * miss / ink, 100.0 * extra / ink))
        V = [s for s in st if s["axis"] == "V"]
        Hs = [s for s in st if s["axis"] == "H"]
        noto_v += [s["width"] for s in V]
        noto_h += [s["width"] for s in Hs]

        def ends(s):
            """(lo, hi, how_lo, how_hi) of the constructed stroke on the skeleton (character mm, before 长体)."""
            res = []
            others = Hs if s["axis"] == "V" else V
            for which in ("lo", "hi"):
                e = s[which]
                hit = None
                for o in others:
                    o_lo, o_hi = o["centre"] - o["width"] / 2, o["centre"] + o["width"] / 2
                    if o["lo"] - join_tol <= s["centre"] <= o["hi"] + join_tol and o_lo - join_tol <= e <= o_hi + join_tol:
                        hit = o
                        break
                if hit is None:
                    res.append((e, "free"))
                else:
                    far = hit["centre"] - hw if which == "lo" else hit["centre"] + hw
                    res.append((far, "joined %s %.2f" % (hit["axis"], hit["centre"])))
            return res

        rects = []
        # ends of one kind are drawn to one length (平面视觉 11-04, WORLD_TYPE §3 "Constructed ends": "口's two feet
        # equal"): the FREE ends of one character on one axis and one side whose face ends lie within kind_tol (a
        # fraction of the em) of each other are one kind, and each is drawn to the kind's mean, at this em
        st_ends = [list(ends(s)) for s in st]
        kind_mm = kind_tol * em_px / pxmm
        for axis in ("V", "H"):
            for side in (0, 1):
                vals = sorted((st_ends[i][side][0], i) for i, s in enumerate(st)
                              if s["axis"] == axis and st_ends[i][side][1] == "free")
                groups, cur = [], []
                for e, i in vals:
                    if cur and e - cur[0][0] > kind_mm:
                        groups.append(cur)
                        cur = []
                    cur.append((e, i))
                if cur:
                    groups.append(cur)
                for g in groups:
                    if len(g) < 2:
                        continue
                    mean = sum(e for e, i in g) / len(g)
                    for e, i in g:
                        st_ends[i][side] = (mean, "free")
                    wf = (lambda v: v) if axis == "V" else (lambda v, x0w=x_mm: x0w + v * width)   # the word frame
                    kinds.append({"char": ch, "axis": axis, "side": ("lo", "hi")[side],
                                  "faceEnds": [wf(e) for e, i in g], "drawnTo": wf(mean),
                                  "spreadMm": round(g[-1][0] - g[0][0], 3), "strokes": len(g)})
        for s, ((lo, how_lo), (hi, how_hi)) in zip(st, st_ends):
            if s["axis"] == "V":
                cx = x_mm + s["centre"] * width
                r = (cx - hw, lo, cx + hw, hi)
                sk = {"char": ch, "axis": "V", "x": cx, "from": lo, "to": hi}
            else:
                cy = s["centre"]
                # 长体 scales the skeleton across, never the stroke: a joined end sits S/2 past its V's centre line
                lo_w = x_mm + (lo + hw) * width - hw if how_lo != "free" else x_mm + lo * width
                hi_w = x_mm + (hi - hw) * width + hw if how_hi != "free" else x_mm + hi * width
                r = (lo_w, cy - hw, hi_w, cy + hw)
                sk = {"char": ch, "axis": "H", "y": cy, "from": lo_w, "to": hi_w}
            if s["axis"] == "V":
                face_ends = [round(s["lo"], 3), round(s["hi"], 3)]
            else:
                face_ends = [round(x_mm + s["lo"] * width, 3), round(x_mm + s["hi"] * width, 3)]
            sk.update({"ends": [how_lo, how_hi], "faceWidthMm": round(s["width"], 3), "faceEnds": face_ends})
            skeleton.append(sk)
            rects.append(r)
        per_char_rects.append((ch, rects))
        plus += rects
        x_mm += adv * width / pxmm
    total_adv_mm = x_mm
    # the untied ink (outer loops CCW, counters CW)
    ink_loops = rect_union_loops(plus)
    counters = [lp for lp in ink_loops if area(lp) < 0]
    tie_w = max(tie_min, tie_frac * stroke)
    ties = []
    minus = []
    char_info = []
    if counters:
        # rays through the constructed ink: rasterise it in a word frame
        xs = [p[0] for lp in ink_loops for p in lp]
        ys = [p[1] for lp in ink_loops for p in lp]
        fx0, fy1 = min(xs) - 5.0, max(ys) + 5.0
        Wd = int(math.ceil((max(xs) + 5.0 - fx0) * pxmm))
        Hd = int(math.ceil((fy1 - min(ys) + 5.0) * pxmm))
        fw = raster_rects(plus, (Hd, Wd), pxmm, fx0, fy1 * pxmm).astype(np.float32)
        for k, cnt in enumerate(counters):
            cen = centroid(list(reversed(cnt)))
            ch = word[min(len(word) - 1, int(cen[0] // (em_mm * width)))]
            big = -area(cnt) >= tie_big_em2 * em_mm * em_mm
            runs = {}
            for name, d in (("up", (0, 1)), ("down", (0, -1)), ("left", (-1, 0)), ("right", (1, 0))):
                r = ray_run(fw, pxmm, fx0, fy1 * pxmm, cen, d, counters, k)
                if r is not None:
                    runs[name] = (r, d)
            if not runs:
                # A counter too big for the fixed 60 mm search (ja 出口 at em 164.1: 41.1 mm from 口's centre to its stroke
                # + 19.05 across = 60.2): search again as far as the counter's half size + 3 strokes. Only a counter that
                # found no path at all is searched again, so every legend that built before builds the same.
                cx = [p[0] for p in cnt]
                cy = [p[1] for p in cnt]
                reach = 0.5 * max(max(cx) - min(cx), max(cy) - min(cy)) + 3.0 * stroke
                for name, d in (("up", (0, 1)), ("down", (0, -1)), ("left", (-1, 0)), ("right", (1, 0))):
                    r = ray_run(fw, pxmm, fx0, fy1 * pxmm, cen, d, counters, k, max_mm=reach)
                    if r is not None:
                        runs[name] = (r, d)
            if not runs:
                raise RuntimeError("no tie path for a counter of " + ch)
            if big and (("up" in runs and "down" in runs) or ("left" in runs and "right" in runs)):
                pairs = []
                for p in (("up", "down"), ("left", "right")):
                    if p[0] in runs and p[1] in runs:
                        L = max(runs[p[0]][0][1] - runs[p[0]][0][0], runs[p[1]][0][1] - runs[p[1]][0][0])
                        pairs.append((L * (1.0 if p[0] == "up" else 1.03), p))
                pairs.sort()
                chosen = list(pairs[0][1])
            else:
                chosen = [min(runs, key=lambda n: runs[n][0][1] - runs[n][0][0])]
            for name in chosen:
                (t0, t1), d = runs[name]
                a0, a1 = t0 - 1.5, t1 + 1.5
                if d[0] == 0:
                    ya, yb = cen[1] + d[1] * a0, cen[1] + d[1] * a1
                    minus.append((cen[0] - tie_w / 2, min(ya, yb), cen[0] + tie_w / 2, max(ya, yb)))
                else:
                    xa, xb = cen[0] + d[0] * a0, cen[0] + d[0] * a1
                    minus.append((min(xa, xb), cen[1] - tie_w / 2, max(xa, xb), cen[1] + tie_w / 2))
                ties.append({"char": ch, "dir": name, "centre_mm": [round(cen[0] + d[0] * (t0 + t1) / 2, 2), round(cen[1] + d[1] * (t0 + t1) / 2, 2)],
                             "width_mm": round(tie_w, 2), "across_mm": round(t1 - t0, 2)})
    holes0 = rect_union_loops(plus, minus)
    left = [lp for lp in holes0 if area(lp) < 0]
    if left:
        raise RuntimeError("%d counters left after the ties" % len(left))
    hole_loops = [fillet_reflex(lp, fillet, segs=2, min_turn_deg=0.0) for lp in holes0]
    for ch, rects in per_char_rects:
        cl = rect_union_loops(rects)
        char_info.append({"char": ch, "counters": [round(float(-area(c)), 2) for c in cl if area(c) < 0],
                          "outer": len([c for c in cl if area(c) > 0])})
    # place: the ink box centred in the opening
    allp = [p for lp in hole_loops for p in lp]
    xs = [p[0] for p in allp]
    ys = [p[1] for p in allp]
    bx0, bx1, by0, by1 = min(xs), max(xs), min(ys), max(ys)
    dx = OPEN_W / 2 - (bx0 + bx1) / 2
    dy = OPEN_H / 2 - (by0 + by1) / 2
    mv = lambda lp: [(round(x + dx, 4), round(y + dy, 4)) for x, y in lp]
    hole_loops = [mv(lp) for lp in hole_loops]
    ink_loops = [mv(lp) for lp in ink_loops]
    for t in ties:
        t["centre_mm"] = [round(t["centre_mm"][0] + dx, 2), round(t["centre_mm"][1] + dy, 2)]
    for sk in skeleton:
        if sk["axis"] == "V":
            sk["x"] = round(sk["x"] + dx, 3)
            sk["from"], sk["to"] = round(sk["from"] + dy, 3), round(sk["to"] + dy, 3)
            sk["faceEnds"] = [round(v + dy, 3) for v in sk["faceEnds"]]
        else:
            sk["y"] = round(sk["y"] + dy, 3)
            sk["from"], sk["to"] = round(sk["from"] + dx, 3), round(sk["to"] + dx, 3)
            sk["faceEnds"] = [round(v + dx, 3) for v in sk["faceEnds"]]
    ink_box = [round(bx0 + dx, 2), round(by0 + dy, 2), round(bx1 + dx, 2), round(by1 + dy, 2)]
    # height means ink (平面视觉 11-04, WORLD_TYPE §3): a life-safety floor in letter height is met by the CJK ink, not
    # its em; a legend whose ink stands lower than the floor is refused (0.05 mm = the raster's reading)
    if ink_floor is not None and (by1 - by0) < ink_floor - 0.05:
        raise RuntimeError("ink %.2f mm stands below the floor %.2f mm (WORLD_TYPE §3 'Height means ink'): about em %.1f "
                           "would meet it; refused" % (by1 - by0, ink_floor, em_mm * ink_floor / (by1 - by0)))
    # air at the sides (平面视觉 11-04 07:0x, WORLD_TYPE §3): at the floor height the legend's side margins stay at least
    # side_air x its top and bottom ones; the skeleton is narrowed (长体, the constructed strokes unchanged) to get there,
    # never below 85 % (then a shorter word signed by 叙事). Measured on the placed holes, unrounded.
    side_rec = None
    if side_air is not None:
        m_l, m_r = bx0 + dx, OPEN_W - (bx1 + dx)
        m_b, m_t = by0 + dy, OPEN_H - (by1 + dy)
        side_mm, tb_mm = min(m_l, m_r), max(m_b, m_t)
        side_rec = {"ruleX": side_air, "sideMm": round(side_mm, 3), "topBottomMm": round(tb_mm, 3),
                    "ratio": round(side_mm / tb_mm, 4), "widthFloor": 0.85}
        if side_mm < side_air * tb_mm - 1e-6:
            # a hint only: here the ink width is linear in the 长体 (the V centres scale, the stroke does not)
            need_w = width * (OPEN_W - 2.0 * side_air * tb_mm - stroke) / max(1e-9, (bx1 - bx0) - stroke)
            raise SideAirError("sides %.3f mm = %.4f x the top / bottom %.3f mm: under the %.2f x of WORLD_TYPE §3 'Air at the "
                               "sides' at 长体 %.1f %%; about %.2f %% would meet it (never below 85 %%); refused"
                               % (side_mm, side_mm / tb_mm, tb_mm, side_air, 100.0 * width, 100.0 * need_w))
    with open(font_path, "rb") as fh:
        font_sha = hashlib.sha1(fh.read()).hexdigest()
    verts = sum(len(lp) for lp in hole_loops)
    for k in kinds:
        k["faceEnds"] = [round(v + (dy if k["axis"] == "V" else dx), 3) for v in k["faceEnds"]]
        k["drawnTo"] = round(k["drawnTo"] + (dy if k["axis"] == "V" else dx), 3)
    construction = {
        "method": "constructed on the face's skeleton (WORLD_TYPE §3 Stencils): one stroke, square ends, the punch's fillet",
        "strokeMm": stroke, "filletMm": fillet, "joinTolMm": join_tol, "minRunMm": min_run_mm,
        "inkFloorMm": ink_floor, "kindTolEm": kind_tol, "endKinds": kinds,
        "faceStrokesMm": {"vertical": [round(v, 3) for v in noto_v], "horizontal": [round(v, 3) for v in noto_h],
                          "verticalMedian": round(float(np.median(noto_v)), 3) if noto_v else None,
                          "horizontalMedian": round(float(np.median(noto_h)), 3) if noto_h else None},
        "coverage": coverage, "skeleton": skeleton}
    if side_rec is not None:
        construction["sideAir"] = side_rec
    return {
        "word": word, "mode": "stencil", "build": "constructed", "emMm": round(em_px / pxmm, 3), "emAsked": em_mm, "width": width,
        "face": os.path.basename(font_path), "faceSha1": font_sha, "wght": wght, "axes": str(axes),
        "pxPerMm": pxmm, "tolMm": 0.0, "advanceMm": round(total_adv_mm, 2),
        "inkBox": ink_box, "inkW": round(bx1 - bx0, 2), "inkH": round(by1 - by0, 2),
        "margins": {"left": ink_box[0], "right": round(OPEN_W - ink_box[2], 2), "bottom": ink_box[1], "top": round(OPEN_H - ink_box[3], 2)},
        "strokes": {"verticalStemMm": round(stroke, 2), "horizontalStrokeMm": round(stroke, 2)},
        "islands": len(counters), "ties": ties, "tieWidthMm": round(tie_w, 2),
        "chars": char_info, "holeVerts": verts,
        "construction": construction,
        "holes": hole_loops, "ink": ink_loops, "note": note,
    }


def preview(leg, path, scale=4.0, english=None):
    """The opening at scale px/mm: white plate, red holes (stencil) or the printed lens, ties visible."""
    W, H = int(OPEN_W * scale), int(OPEN_H * scale)
    img = Image.new("RGB", (W, H), (226, 222, 210))
    d = ImageDraw.Draw(img)
    P = lambda lp: [(x * scale, (OPEN_H - y) * scale) for x, y in lp]
    if leg["mode"] == "stencil":
        for lp in leg["holes"]:
            d.polygon(P(lp), fill=(200, 30, 26))
    else:
        d.polygon(P(leg["holes"][0]), fill=(232, 228, 216), outline=(120, 120, 120))
        m = Image.new("L", (W, H), 0)
        dm = ImageDraw.Draw(m)
        for lp in leg["ink"]:
            dm.polygon(P(lp), fill=255 if area(lp) > 0 else 0)
        # counters: draw outer first then holes (sorted by area desc)
        m = Image.new("L", (W, H), 0)
        dm = ImageDraw.Draw(m)
        for lp in sorted(leg["ink"], key=lambda q: -abs(area(q))):
            dm.polygon(P(lp), fill=255 if area(lp) > 0 else 0)
        img.paste((200, 30, 26), mask=m)
    if "construction" in leg:
        # the skeleton the strokes were drawn on: centre lines, a dot at a free end, a bar at a joined end
        for sk in leg["construction"]["skeleton"]:
            if sk["axis"] == "V":
                a, b = (sk["x"], sk["from"]), (sk["x"], sk["to"])
            else:
                a, b = (sk["from"], sk["y"]), (sk["to"], sk["y"])
            d.line(P([a, b]), fill=(250, 236, 200), width=max(1, int(scale * 0.35)))
            for pt, how in ((a, sk["ends"][0]), (b, sk["ends"][1])):
                (px, py), = P([pt])
                rr = scale * 1.2
                if how == "free":
                    d.ellipse((px - rr, py - rr, px + rr, py + rr), fill=(250, 236, 200))
                else:
                    d.rectangle((px - rr, py - rr, px + rr, py + rr), outline=(250, 236, 200))
    img.save(path)


def _outline_gap(loops_a, loops_b, step=0.05):
    """Largest distance from a point on one set of loops to the nearest edge of the other, both ways (mm)."""
    def segs(loops):
        A = np.concatenate([np.array(l, float) for l in loops])
        B = np.concatenate([np.roll(np.array(l, float), -1, 0) for l in loops])
        return A, B

    def dense(loops):
        pts = []
        for l in loops:
            l = np.array(l, float)
            for i in range(len(l)):
                p, q = l[i], l[(i + 1) % len(l)]
                n = max(1, int(np.linalg.norm(q - p) / step))
                for k in range(n):
                    pts.append(p + (q - p) * k / n)
        return np.array(pts)

    def far(P, A, B):
        AB = B - A
        L2 = np.maximum((AB ** 2).sum(1), 1e-12)
        t = np.clip(((P[:, None, :] - A[None]) * AB[None]).sum(2) / L2[None], 0, 1)
        Q = A[None] + t[..., None] * AB[None]
        return float(np.sqrt(((P[:, None, :] - Q) ** 2).sum(2)).min(1).max())

    a_seg, b_seg = segs(loops_a), segs(loops_b)
    return max(far(dense(loops_a), *b_seg), far(dense(loops_b), *a_seg))


def set_lod1(coarse_path, out):
    """The LOD1 outline of a long legend (EXIT in the Latin and Cyrillic languages, game visual 2026-11-04): the hero
    plate keeps its 0.25 mm arcs and the punch's fillets in LOD0; the kit's LOD1 cuts its plate from these loops."""
    with open(coarse_path, encoding="utf-8") as fh:
        c = json.load(fh)
    with open(out, encoding="utf-8") as fh:
        leg = json.load(fh)
    for key in ("word", "ties", "inkBox"):
        if c.get(key) != leg.get(key):
            sys.exit("--set-lod1: %s differs from the hero legend's" % key)
    # A CJK legend constructed by this file (出口, 2026-11-04 jako run): its strokes are axis rectangles, so there is no
    # arc to flatten; the coarse file is the same word built here with --fillet 0 and its LOD1 loops are that build's
    # "holes" (the tied holes without the punch fillets; its "ink" is the untied outline with the counter). Face, em,
    # 长体 and stroke must be the hero's. A die-face legend (Latin, Cyrillic, ko) keeps the branch below as it was.
    cjk = c.get("build") == "constructed" and "emMm" in c and (c.get("construction") or {}).get("filletMm") == 0
    if cjk:
        for key in ("faceSha1", "emMm", "width", "strokes"):
            if c.get(key) != leg.get(key):
                sys.exit("--set-lod1: %s differs from the hero legend's (a constructed coarse file is the same build at --fillet 0)" % key)
        loops = c["holes"]
    else:
        loops = c["ink"]
    if len(loops) != len(leg["holes"]):
        sys.exit("--set-lod1: %d loops against the hero's %d holes" % (len(loops), len(leg["holes"])))
    if cjk:
        tol = c.get("tolMm", 0.0)
        leg["lod1"] = {"tolMm": tol, "loops": "holes: the tied holes of the same word built with --fillet 0 (no punch fillets; "
                                              "a constructed legend has axis strokes only, no arc to flatten)",
                       "generator": "exit_legend.build_constructed(word, em=%s, width=%s, fillet=0) with %s %s" % (
                           c["emMm"], c["width"], c.get("face", ""), c.get("faceSha1", "")[:8]),
                       "holeVerts": sum(len(h) for h in loops),
                       "devFromHeroMm": round(_outline_gap(loops, leg["holes"]), 3),
                       "use": "the kit's LOD1 plate only (exit_sign_common.build_plate_lod1); LOD0 and the maps keep the hero holes",
                       "holes": loops}
    else:
        tol = c["construction"]["tolMm"]
        # the generator as the coarse file names its face file: 平面视觉's W set (legend.py + dieface.swift, which the 11-04
        # 08:53 LOD1 build used) or the set shipped in Tools/lookdev (exit_legend_dieface.py + exit_dieface.swift / _ko.swift)
        face_file = (c.get("face") or "").rsplit(": ", 1)[-1].strip()
        builder = "legend.build" if face_file == "dieface.swift" else "exit_legend_dieface.build"
        leg["lod1"] = {"tolMm": tol, "loops": "ink: the tied holes of the same word before the R 0.5 punch fillets",
                       "generator": "%s(code, word, tol=%s) with %s %s" % (builder, tol, face_file, c.get("faceSha1", "")[:8]),
                       "holeVerts": sum(len(h) for h in c["ink"]),
                       "devFromHeroMm": round(_outline_gap(c["ink"], leg["holes"]), 3),
                       "use": "the kit's LOD1 plate only (exit_sign_common.build_plate_lod1); LOD0 and the maps keep the hero holes",
                       "holes": c["ink"]}
    with open(out, "w", encoding="utf-8", newline="\n") as fh:
        json.dump(leg, fh, ensure_ascii=False, indent=1)
        fh.write("\n")
    print("lod1 tol %s: %d hole vertices (hero %d), %.3f mm from the hero outline -> %s" % (
        tol, leg["lod1"]["holeVerts"], sum(len(h) for h in leg["holes"]), leg["lod1"]["devFromHeroMm"], out))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--word", default=None)
    ap.add_argument("--em", type=float, default=None, help="CJK em, mm")
    ap.add_argument("--width", default="1.0",
                    help="长体: 1.0 or down to 0.85; 'auto' (constructed, with --side-air): the widest 0.001 step from 1.0 down "
                         "to 0.85 that keeps the side-air rule (WORLD_TYPE §3 'Air at the sides', 11-04)")
    ap.add_argument("--mode", choices=["stencil", "print"], default="stencil")
    ap.add_argument("--font", default=FONT_SC)
    ap.add_argument("--wght", type=float, default=700)
    ap.add_argument("--pxmm", type=float, default=30.0)
    ap.add_argument("--tol", type=float, default=0.04)
    ap.add_argument("--tie-frac", type=float, default=0.30)
    ap.add_argument("--tie-min", type=float, default=3.0)
    ap.add_argument("--tie-big", type=float, default=0.06, help="a counter of at least this many em^2 gets two ties")
    ap.add_argument("--traced", action="store_true", help="stencil: trace the face's outlines (the 21:32 build) instead of constructing")
    ap.add_argument("--stroke", type=float, default=S_EN, help="constructed stroke, mm (exit_sign_geom.S, the English 3/4 in)")
    ap.add_argument("--fillet", type=float, default=HOLE_FILLET, help="constructed: the punch's fillet in every reflex corner, mm")
    ap.add_argument("--kind-tol", type=float, default=0.01,
                    help="constructed: free ends of one character, one axis and one side within this fraction of the em are "
                         "one kind, drawn to their mean (WORLD_TYPE §3 'Constructed ends', 11-04); 0 = each its own")
    ap.add_argument("--ink-floor", type=float, default=None,
                    help="constructed: refuse a legend whose ink stands lower than this many mm (a life-safety floor in "
                         "letter height is met by the ink, not the em: WORLD_TYPE §3 'Height means ink', 11-04)")
    ap.add_argument("--side-air", type=float, default=None,
                    help="constructed: refuse a legend whose side margins stand under this many times its top / bottom ones "
                         "(WORLD_TYPE §3 'Air at the sides', 11-04: 1.25 at the floor height); recorded as construction.sideAir")
    ap.add_argument("--set-shares", default=None, metavar="REPORT",
                    help="write field.letterShares of gen_exit_sign.py's report into the legend file --out, then stop")
    ap.add_argument("--set-lod1", default=None, metavar="COARSE",
                    help="write the LOD1 outline into the legend file --out, then stop: COARSE is the same word built by its own "
                         "generator at a coarser arc flattening (a Latin or Cyrillic legend: 平面视觉's exit_legend_dieface.py <code> "
                         "--tol 1.0 --out DIR, never into this folder); "
                         "its 'ink' loops (the tied holes before the punch fillets) become the kit's LOD1 plate (exit_sign_common."
                         "build_plate_lod1); a CJK legend built here: the same build at --fillet 0, whose 'holes' are taken "
                         "(face, em, 长体 and stroke must match too). Refused unless word, ties, ink box and hole count match the hero legend")
    ap.add_argument("--note", default="")
    ap.add_argument("--out", required=True)
    ap.add_argument("--preview", default=None)
    a = ap.parse_args()
    if a.set_lod1:
        set_lod1(a.set_lod1, a.out)
        return
    if a.set_shares:
        with open(a.set_shares, encoding="utf-8") as fh:
            sh = json.load(fh)["field"]["letterShares"]
        with open(a.out, encoding="utf-8") as fh:
            leg = json.load(fh)
        leg["letterShares"] = sh
        with open(a.out, "w", encoding="utf-8", newline="\n") as fh:
            json.dump(leg, fh, ensure_ascii=False, indent=1)
            fh.write("\n")
        print("letterShares", json.dumps(sh), "->", a.out)
        return
    if not a.word or a.em is None:
        sys.exit("--word and --em are needed")
    constructed = a.mode == "stencil" and not a.traced
    search = None
    if a.width == "auto":
        if not constructed or a.side_air is None:
            sys.exit("--width auto needs the constructed stencil and --side-air")

        def fits(k):
            try:
                build_constructed(a.word, a.em, k / 1000.0, a.font, a.wght, a.pxmm, a.stroke, a.fillet, a.tie_frac, a.tie_min,
                                  a.tie_big, a.note, kind_tol=a.kind_tol, ink_floor=a.ink_floor, side_air=a.side_air)
                return True
            except SideAirError as e:
                print("  side-air at %.1f %%: %s" % (k / 10.0, e), file=sys.stderr)
                return False
        tried = []
        if fits(1000):
            k_ok = 1000
            tried.append([1.0, True])
        else:
            tried.append([1.0, False])
            if not fits(850):
                sys.exit("even 长体 85 %% crowds the sides (WORLD_TYPE §3 'Air at the sides'): a shorter word signed by 叙事 "
                         "(§3 Width); refused")
            tried.append([0.85, True])
            k_ok, k_bad = 850, 1000
            while k_bad - k_ok > 1:
                mid = (k_ok + k_bad) // 2
                ok = fits(mid)
                tried.append([mid / 1000.0, ok])
                if ok:
                    k_ok = mid
                else:
                    k_bad = mid
        width = k_ok / 1000.0
        search = {"asked": "auto", "step": 0.001, "tried": tried, "widest": width}
        print("  长体 auto: the widest 0.001 step that keeps sides >= %.2f x top/bottom = %.3f" % (a.side_air, width), file=sys.stderr)
    else:
        width = float(a.width)
    if not (0.85 - 1e-9 <= width <= 1.0):
        sys.exit("width must be 0.85-1.0 (WORLD_TYPE: 长体 down to 85 %)")
    if constructed:
        try:
            leg = build_constructed(a.word, a.em, width, a.font, a.wght, a.pxmm, a.stroke, a.fillet, a.tie_frac, a.tie_min,
                                    a.tie_big, a.note, kind_tol=a.kind_tol, ink_floor=a.ink_floor, side_air=a.side_air)
        except SideAirError as e:
            sys.exit(str(e))
        if search is not None:
            leg["construction"]["sideAir"]["widthSearch"] = search
    else:
        if a.side_air is not None:
            sys.exit("--side-air applies to the constructed stencil")
        leg = build(a.word, a.em, width, a.mode, a.font, a.wght, a.pxmm, a.tol, a.tie_frac, a.tie_min, a.tie_big, a.note)
    os.makedirs(os.path.dirname(os.path.abspath(a.out)), exist_ok=True)
    with open(a.out, "w", encoding="utf-8", newline="\n") as fh:
        json.dump(leg, fh, ensure_ascii=False, indent=1)
        fh.write("\n")
    if a.preview:
        preview(leg, a.preview)
    s = {k: v for k, v in leg.items() if k not in ("holes", "ink", "construction")}
    print(json.dumps(s, ensure_ascii=False, indent=1))
    if "construction" in leg:
        c = leg["construction"]
        print(json.dumps({k: v for k, v in c.items() if k != "skeleton"}, ensure_ascii=False))
        for sk in c["skeleton"]:
            print(json.dumps(sk, ensure_ascii=False))


if __name__ == "__main__":
    main()
