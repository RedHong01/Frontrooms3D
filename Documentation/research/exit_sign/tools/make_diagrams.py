"""Support diagrams for Documentation/research/exit_sign/10_spec.md.

Constructed (not a font) 1990 stencil EXIT face to NFPA 101 / UL 924 (1989)
geometry, the lamp fields, and simulated states. Our own work; no external media.
Run: /usr/bin/python3 make_diagrams.py <out_dir>
"""
import json
import math
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

OUT = sys.argv[1]
PX = 4.0                      # px per mm
H, S = 152.4, 19.05           # letter height, stroke (6 in, 3/4 in)
OPEN_W, OPEN_H = 320.0, 186.0
FRAME = 13.0                  # face frame border: housing 346 x 212
Y0 = (OPEN_H - H) / 2
GAP = 12.0
EW, XW, TW = 54.0, 60.0, 54.0
CH_H, CH_W = 57.15, 24.6      # 2.25 in chevron (NISTIR 4532), w/h 0.43
CH_GAP = 10.0

MONO = "/System/Library/Fonts/Menlo.ttc"


def font(sz):
    return ImageFont.truetype(MONO, sz)


def letter_polys():
    """Polygons in opening mm (origin bottom-left). Returns dict name -> list of polys."""
    legend_w = CH_W + CH_GAP + EW + GAP + XW + GAP + S + GAP + TW + CH_GAP + CH_W
    x = (OPEN_W - legend_w) / 2
    polys = {}
    chl_x = x
    x += CH_W + CH_GAP
    m0, m1 = (H - S) / 2, (H + S) / 2
    e = [(0, 0), (EW, 0), (EW, S), (S, S), (S, m0), (EW - 6, m0), (EW - 6, m1), (S, m1), (S, H - S), (EW, H - S), (EW, H), (0, H)]
    polys["E"] = [[(x + a, Y0 + b) for a, b in e]]
    x += EW + GAP
    hw = S
    for _ in range(6):
        hw = S / math.cos(math.atan((XW - hw) / H))
    p1 = [(0, 0), (hw, 0), (XW, H), (XW - hw, H)]
    p2 = [(XW - hw, 0), (XW, 0), (hw, H), (0, H)]
    polys["X"] = [[(x + a, Y0 + b) for a, b in p] for p in (p1, p2)]
    x += XW + GAP
    polys["I"] = [[(x, Y0), (x + S, Y0), (x + S, Y0 + H), (x, Y0 + H)]]
    x += S + GAP
    tb = [(0, H - S), (TW, H - S), (TW, H), (0, H)]
    ts = [((TW - S) / 2, 0), ((TW + S) / 2, 0), ((TW + S) / 2, H - S), ((TW - S) / 2, H - S)]
    polys["T"] = [[(x + a, Y0 + b) for a, b in p] for p in (tb, ts)]
    x += TW + CH_GAP
    chr_x = x
    cy = Y0 + H / 2 - CH_H / 2
    a = 0.45 * CH_W

    def chevron(x0, left):
        pts = [(0, CH_H / 2), (CH_W - a, CH_H), (CH_W, CH_H), (a, CH_H / 2), (CH_W, 0), (CH_W - a, 0)]
        if not left:
            pts = [(CH_W - px, py) for px, py in pts]
        return [(x0 + px, cy + py) for px, py in pts]

    polys["<"] = [chevron(chl_x, True)]
    polys[">"] = [chevron(chr_x, False)]
    return polys, legend_w


def raster(polys_list, w_px, h_px, ox=0.0, oy=0.0):
    img = Image.new("L", (w_px, h_px), 0)
    d = ImageDraw.Draw(img)
    for poly in polys_list:
        d.polygon([((ox + px) * PX, h_px - (oy + py) * PX) for px, py in poly], fill=255)
    return np.asarray(img).astype(np.float32) / 255


def field(lamps, w_px, h_px):
    """Sum of 2D Gaussians over the opening. lamps: (u, v, sx, sy, amp) in opening fractions."""
    v, u = np.mgrid[0:h_px, 0:w_px].astype(np.float32)
    u = (u + .5) / w_px
    v = 1 - (v + .5) / h_px
    f = np.zeros((h_px, w_px), np.float32)
    for lu, lv, sx, sy, amp in lamps:
        f += amp * np.exp(-((u - lu) ** 2 / (2 * sx * sx) + (v - lv) ** 2 / (2 * sy * sy)))
    return f


LAMP_A = (0.33, 0.70, 0.08, 0.20, 1.0)   # 20 W lamp behind the E-X web (fit to NISTIR 4399 red signs 2 and 3R)
LAMP_B = (0.65, 0.70, 0.08, 0.20, 1.0)   # 20 W lamp behind the I-T web
FILL = (0.5, 0.5, 9.0, 9.0, 0.06)      # cavity inter-reflection: each working lamp adds 0.06 everywhere
BATT = [(0.12, 0.70, 0.08, 0.20, 1.0), (0.88, 0.70, 0.08, 0.20, 1.0), FILL, FILL]   # 2 x 5 W DC lamps near the ends (USACE Type 604; positions ESTIMATE)


def tonemap(x):
    # Simple filmic stand-in (not the game's ACES): just to show relative levels.
    x = np.maximum(x, 0)
    y = (x * (2.51 * x + .03)) / (x * (2.43 * x + .59) + .14)
    y = np.clip(y, 0, 1)
    return np.where(y <= .0031308, 12.92 * y, 1.055 * np.power(y, 1 / 2.4) - .055)


def srgb_to_lin(c):
    c = np.asarray(c, np.float32) / 255
    return np.where(c <= .04045, c / 12.92, ((c + .055) / 1.055) ** 2.4)


def render_state(name, lampset, level, ambient, chevrons_open, w_mm=OPEN_W, h_mm=OPEN_H):
    polys, _ = letter_polys()
    wpx, hpx = int(round((w_mm + 2 * FRAME) * PX)), int(round((h_mm + 2 * FRAME) * PX))
    letters = [p for k in "EXIT" for p in polys[k]]
    chev = [p for k in "<>" for p in polys[k]]
    m_let = raster(letters, wpx, hpx, FRAME, FRAME)
    m_chev = raster(chev, wpx, hpx, FRAME, FRAME)
    holes = np.maximum(m_let, m_chev if chevrons_open else 0)
    # Field over the whole image (opening coordinates).
    f = np.zeros((hpx, wpx), np.float32)
    ox, oy = int(FRAME * PX), int(FRAME * PX)
    wo, ho = int(OPEN_W * PX), int(OPEN_H * PX)
    sub = np.zeros((ho, wo), np.float32)
    for lamp in lampset:
        sub += field([lamp], wo, ho)
    f[oy:oy + ho, ox:ox + wo] = sub
    enamel = srgb_to_lin((232, 228, 214))
    frame_c = srgb_to_lin((214, 210, 196))
    diff_albedo = srgb_to_lin((110, 36, 31))
    img = np.zeros((hpx, wpx, 3), np.float32)
    img[:] = frame_c
    img[oy:oy + ho, ox:ox + wo] = enamel
    # Hole: diffuser seen 6 mm behind the plate: shadowed rim (2 mm) under room light.
    rim = np.asarray(Image.fromarray((holes * 255).astype(np.uint8)).filter(ImageFilter.MinFilter(9))).astype(np.float32) / 255
    shade = 0.45 + 0.55 * rim
    lit_face = img * ambient
    dif = diff_albedo[None, None, :] * ambient * shade[..., None]
    out = lit_face * (1 - holes[..., None]) + dif * holes[..., None]
    if not chevrons_open:
        # Closed knockouts: a scored outline only.
        edge = np.asarray(Image.fromarray((m_chev * 255).astype(np.uint8)).filter(ImageFilter.FIND_EDGES)).astype(np.float32) / 255
        out *= (1 - .35 * edge[..., None])
    red = np.array([1.0, .012, .006], np.float32)
    peak = 2.6
    emis = (f * level)[..., None] * holes[..., None] * red * peak
    bloom = np.stack([np.asarray(Image.fromarray(np.clip(emis[..., c] / 4 * 255, 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(28))).astype(np.float32) / 255 * 4 for c in range(3)], -1)
    out = out + emis + .12 * bloom
    return (tonemap(out) * 255).astype(np.uint8), f, m_let, holes


def letter_means(f, polys):
    res = {}
    wpx, hpx = int(round((OPEN_W + 2 * FRAME) * PX)), int(round((OPEN_H + 2 * FRAME) * PX))
    for k in "EXIT":
        m = raster(polys[k], wpx, hpx, FRAME, FRAME) > .5
        res[k] = float(f[m].mean())
    return res


def main():
    polys, legend_w = letter_polys()
    states = [
        ("LIT  two 20 W lamps (AC)", [LAMP_A, LAMP_B, FILL, FILL], 1.0, .18, False),
        ("HALF  lamp A out", [LAMP_B, FILL], 1.0, .18, False),
        ("LOOSE  lamp B drops (1 frame)", [LAMP_A, FILL, (LAMP_B[0], LAMP_B[1], LAMP_B[2], LAMP_B[3], .08)], 1.0, .18, False),
        ("BATTERY  2 x 5 W DC lamps, mains lost", BATT, .25, .05, False),
        ("DEAD  room lit, no lamps", [], 0.0, .85, False),
        ("LIT + both chevrons open", [LAMP_A, LAMP_B, FILL, FILL], 1.0, .18, True),
    ]
    tiles, report = [], {}
    norm = None
    for label, lamps, level, amb, chev in states:
        im, f, m_let, holes = render_state(label, lamps, level, amb, chev)
        if norm is None:
            means = letter_means(f, polys)
            norm = means["I"]
        means = letter_means(f, polys)
        report[label] = {k: round(v / norm * level, 3) for k, v in means.items()}
        tiles.append((label, Image.fromarray(im)))
    # Sheet of states: 2 x 3.
    tw, th = tiles[0][1].size
    sc = 0.42
    tw2, th2 = int(tw * sc), int(th * sc)
    pad, lab = 24, 40
    sheet = Image.new("RGB", (3 * tw2 + 4 * pad, 2 * (th2 + lab) + 3 * pad), (24, 24, 24))
    d = ImageDraw.Draw(sheet)
    for i, (label, im) in enumerate(tiles):
        cx, cy = pad + (i % 3) * (tw2 + pad), pad + (i // 3) * (th2 + lab + pad)
        d.text((cx, cy), label, font=font(20), fill=(235, 235, 235))
        sheet.paste(im.resize((tw2, th2), Image.LANCZOS), (cx, cy + lab))
    sheet.save(OUT + "/es02_lamp_states.png")

    # Face geometry vs today's texture.
    today = Image.open(sys.argv[2]).convert("RGB")
    im_lit = tiles[0][1]
    W = im_lit.size[0]
    tsc = W / today.size[0]
    today_r = today.resize((W, int(today.size[1] * tsc)), Image.LANCZOS)
    hdr = 70
    canvas = Image.new("RGB", (W + 80, today_r.size[1] + im_lit.size[1] + 3 * hdr + 120), (245, 244, 240))
    d = ImageDraw.Draw(canvas)
    d.text((40, 20), "TODAY  Run_ExitSign_A: grotesque bold, stroke 1:5.0, E 0.76 H, X 0.92 H", font=font(26), fill=(20, 20, 20))
    canvas.paste(today_r, (40, hdr))
    y2 = hdr + today_r.size[1] + 40
    d.text((40, y2), "1990 STENCIL FACE  6 in / 3/4 in (1:8), E,T 54  X 60  I 19  gaps 12 mm", font=font(26), fill=(20, 20, 20))
    canvas.paste(im_lit, (40, y2 + hdr - 20))
    yb = y2 + hdr - 20 + im_lit.size[1] + 20
    d.text((40, yb), "Opening 320 x 186 mm, frame 13 mm (housing 346 x 212). Legend %.0f mm wide. Both rows shown at the same width." % legend_w,
           font=font(22), fill=(60, 60, 60))
    canvas.save(OUT + "/es01_face_today_vs_1990.png")

    # Per-letter bars vs NISTIR 4399 (1990) Table 1, red signs 2 and 3R, normalised to I.
    nist = {"E": (72.0 / 123.3 + 264.0 / 524.5) / 2, "X": (85.7 / 123.3 + 318.7 / 524.5) / 2, "I": 1.0, "T": (59.2 / 123.3 + 304.8 / 524.5) / 2}
    ours = report[states[0][0]]
    half = report[states[1][0]]
    batt = report[states[3][0]]
    bw, bh = 1200, 560
    bars = Image.new("RGB", (bw, bh), (245, 244, 240))
    d = ImageDraw.Draw(bars)
    d.text((30, 18), "Mean letter luminance, relative to I (LIT)", font=font(26), fill=(20, 20, 20))
    series = [("NISTIR 4399 red signs (1990)", nist, (90, 90, 90)), ("ours LIT", ours, (200, 30, 25)), ("ours HALF", half, (230, 140, 120)), ("ours BATTERY", batt, (120, 60, 160))]
    base, top = 470, 90
    for li, k in enumerate("EXIT"):
        gx = 90 + li * 270
        d.text((gx + 70, base + 20), k, font=font(30), fill=(20, 20, 20))
        for si, (_, data, col) in enumerate(series):
            v = data[k]
            x0 = gx + si * 45
            d.rectangle([x0, base - v * (base - top) / 1.2, x0 + 36, base], fill=col)
    for si, (name, _, col) in enumerate(series):
        d.rectangle([760, 70 + si * 34, 784, 92 + si * 34], fill=col)
        d.text((795, 68 + si * 34), name, font=font(20), fill=(20, 20, 20))
    d.line([60, base, bw - 30, base], fill=(20, 20, 20), width=2)
    bars.save(OUT + "/es04_letter_levels.png")
    report["NISTIR4399_red_mean_rel_I"] = {k: round(v, 3) for k, v in nist.items()}
    report["legend_width_mm"] = round(legend_w, 2)
    json.dump(report, open(OUT + "/es_diagrams_report.json", "w"), indent=1)
    print(json.dumps(report, indent=1))


if __name__ == "__main__":
    main()


def mounts(out):
    """Front elevations of the mounts against the game's heights (mm -> px)."""
    k = 0.30
    PW = 560
    Wp, Hp = 5 * PW + 40, int(3000 * k) + 140
    img = Image.new("RGB", (Wp, Hp), (245, 244, 240))
    d = ImageDraw.Draw(img)
    base = Hp - 50

    def Y(mm):
        return base - mm * k

    def rect(x0, x1, y0mm, y1mm, fill, outline=(20, 20, 20)):
        d.rectangle([x0, Y(y1mm), x1, Y(y0mm)], fill=fill, outline=outline, width=2)

    sign_c = (232, 228, 214)
    wood = (150, 120, 90)
    SW, SH = 346, 212
    panels = [
        ("A  Kit_ExitSign over a map door, Low 2.40", 2400, "wall"),
        ("B  Kit_ExitSign on the stream header, 2.90", 2900, "header"),
        ("C  Kit_ExitSign_Hanging, 2.90", 2900, "hang"),
        ("D  Kit_ExitSign_HangingFlush, Low 2.40", 2400, "flush"),
        ("E  Kit_ExitSign_End (side view), 2.90", 2900, "end"),
    ]
    for i, (title, ceil, kind) in enumerate(panels):
        x = 20 + i * PW
        cx = x + PW // 2
        d.text((x + 6, 14), title, font=font(17), fill=(20, 20, 20))
        d.line([x + 6, Y(0), x + PW - 20, Y(0)], fill=(20, 20, 20), width=3)
        d.line([x + 6, Y(ceil), x + PW - 20, Y(ceil)], fill=(20, 20, 20), width=3)
        d.text((x + PW - 80, Y(ceil) - 22), "%.2f m" % (ceil / 1000), font=font(15), fill=(20, 20, 20))
        d.line([x + 6, Y(2030), x + PW - 20, Y(2030)], fill=(200, 120, 0), width=1)
        d.text((x + 8, Y(2030) + 3), "2.03 m headroom (NFPA 101 7.1.5)", font=font(13), fill=(200, 120, 0))
        hs = SW * k / 2
        if kind == "wall":
            rect(cx - 500 * k, cx + 500 * k, 0, 2100, wood)
            rect(cx - 577 * k, cx + 577 * k, 2100, 2177, (60, 50, 40))
            rect(cx - hs, cx + hs, 2180, 2180 + SH, sign_c)
            d.text((x + 8, Y(2392) - 36), "sign 2.180-2.392 (casing top 2.177)", font=font(14), fill=(160, 20, 20))
        elif kind == "header":
            rect(x + 30, x + PW - 40, 0, 2660, wood)
            d.line([cx, Y(0), cx, Y(2660)], fill=(20, 20, 20), width=2)
            rect(cx - hs, cx + hs, 2674, 2674 + SH, sign_c)
            d.text((x + 8, Y(2660) + 6), "sign 2.674-2.886 on header 2.66-2.90", font=font(14), fill=(255, 240, 220))
        elif kind in ("hang", "flush"):
            drop = 400 if kind == "hang" else 25
            top = ceil - drop
            d.rectangle([cx - 64 * k, Y(ceil), cx + 64 * k, Y(ceil - 22)], fill=(200, 196, 180), outline=(20, 20, 20))
            if kind == "hang":
                d.line([cx, Y(ceil - 22), cx, Y(top)], fill=(20, 20, 20), width=4)
            rect(cx - hs, cx + hs, top - SH, top, sign_c)
            d.text((x + 8, Y(top - SH) + 6), "sign %.3f-%.3f, canopy 127 mm" % ((top - SH) / 1000, top / 1000), font=font(14), fill=(160, 20, 20))
        else:
            d.rectangle([x + 40, Y(ceil), x + 52, Y(0)], fill=(200, 196, 180), outline=(20, 20, 20))
            d.rectangle([x + 52, Y(2400 + 64), x + 52 + 6, Y(2400 - 64)], fill=(120, 120, 120))
            rect(x + 58, x + 58 + SW * k, 2400 - SH / 2, 2400 + SH / 2, sign_c)
            d.text((x + 60, Y(2400 - SH / 2) + 6), "sign 2.294-2.506, projects 0.36 m", font=font(14), fill=(160, 20, 20))
    img.save(out + "/es03_mounts.png")


mounts(OUT)
