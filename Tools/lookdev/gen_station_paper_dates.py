#!/usr/bin/python3
"""Station paper live dates (BS7, 游戏视觉 2026-10-31): the glyph data FrontRoomsStationPaper draws the night-shift date with.

平面视觉's atlas (Tools/print/ink/art_from_graphic/station_paper/, build_atlas.py) leaves three date slots empty
(date_slots.json): the DAR header (Courier Prime), the register's first DATE cell and the sign-out sheet's first DATE cell
(Rock Salt, blue ballpoint, stroke 1). The date follows the live clock (research/station/01_research.md §6), so it cannot be
baked. This script bakes what the game needs to draw it the way build_atlas.py draws text:
- per slot, the glyphs 0-9 and '-' as 8-bit coverage at build_atlas's drawing scale (PX px per mm, the page unstretched),
  drawn with the same font size rule, fill, stroke and baseline fraction as Page.text / hand();
- advances and kerning (PIL getlength), and the pen rounding that reproduces PIL's whole-string layout best;
- the patch: the rectangle of the cell (in atlas texels, 1024 per cell) every possible date touches, Lanczos support
  included, and the atlas's own texels there (the background the date is drawn over).
At run time the game composes the date's coverage at PX, resamples it to the cell's texels with PIL's LANCZOS weights
(build_atlas.py stretches each page into its 1024 x 1024 cell), and writes bg - A * (255 - ink) into a small patch texture
laid over the paper at exactly those texels.

Usage: gen_station_paper_dates.py <project root> <out json> [<atlas png>]
Reads the atlas (default <root>/Assets/Resources/Surfaces/Textures/Prop_StationPaper_A.png) and
<root>/Tools/print/ink/art_from_graphic/station_paper/date_slots.json. LF only, ASCII JSON.
"""
import base64
import datetime
import hashlib
import json
import math
import os
import sys

from PIL import Image, ImageDraw, ImageFont

PX = 12                      # build_atlas.py: px per mm while drawing
CELL, GRID = 1024, 4         # build_atlas.py: cell size, cells per row
K, BLUE = (26, 26, 26), (31, 42, 92)
CHARS = "0123456789-"
PAD = 40

# build_atlas.py's pages (mm) for the dated cells, and the cell order (_station_common.PAPER_CELLS).
PAGES = {"register_left": (205, 285), "dar": (216, 279), "sign_out": (216, 279)}
CELLS = {"register_left": 0, "dar": 3, "sign_out": 4}


def faces(root):
    f = root + "/Assets/Fonts/Period1990/"
    return {"Courier Prime": (f + "CourierPrime/CourierPrime-Regular.ttf", K, 0),
            "Rock Salt, blue": (os.path.expanduser("~/Library/Fonts/RockSalt.ttf"), BLUE, 1)}


def font(path, cap):
    """build_atlas.font: the size whose 'H' is cap mm tall at PX."""
    probe = ImageFont.truetype(path, 1000)
    b = probe.getbbox("H")
    return ImageFont.truetype(path, round(cap * PX * 1000 / (b[3] - b[1])))


def render(fnt, s, x, y, stroke):
    """Coverage of s drawn as Page.text draws it (anchor ls at x, y in px), on a canvas big enough; returns (img, x0, y0)."""
    w = int(fnt.getlength(s)) + 4 * PAD
    h = int(fnt.size * 2) + 2 * PAD
    im = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(im)
    ix, iy = math.floor(x), math.floor(y)
    d.text((PAD + (x - ix), PAD + fnt.size + (y - iy)), s, font=fnt, fill=255, anchor="ls", stroke_width=stroke, stroke_fill=255)
    return im, ix - PAD, iy - PAD - fnt.size


def nights():
    """Every DAR date the live-clock rule can give: the in-game date (1990) minus 1 or 2 days."""
    d = datetime.date(1989, 12, 30)
    while d <= datetime.date(1990, 12, 30):
        yield d
        d += datetime.timedelta(days=1)


def stamp(d):
    return "%02d-%02d-%02d" % (d.month, d.day, d.year % 100)


def compose(glyphs, kern, s, x0, base, mode):
    """The game's composition (FrontRoomsStationPaper.Compose): {(x, y): coverage 0..255}, max over glyphs."""
    out = {}
    pen = x0
    prev = None
    for c in s:
        if prev is not None:
            pen += kern.get(prev + c, 0.0)
        g = glyphs[c]
        gx = (math.floor(pen + .5) if mode == "round" else math.floor(pen)) + g["ox"]
        gy = base + g["oy"]
        a = g["_cov"]
        for j in range(g["h"]):
            for i in range(g["w"]):
                v = a[j * g["w"] + i]
                if v:
                    k = (gx + i, gy + j)
                    if out.get(k, 0) < v:
                        out[k] = v
        pen += g["adv"]
        prev = c
    return out


def main(root, out, atlas_path=None):
    atlas_path = atlas_path or root + "/Assets/Resources/Surfaces/Textures/Prop_StationPaper_A.png"
    slots_path = root + "/Tools/print/ink/art_from_graphic/station_paper/date_slots.json"
    atlas = Image.open(atlas_path).convert("RGB")
    assert atlas.size == (CELL * GRID, CELL * GRID), atlas.size
    slots = json.load(open(slots_path, encoding="utf-8"))
    fx = faces(root)
    result = {"version": 1, "pxPerMm": PX, "cellPx": CELL, "grid": GRID,
              "atlas": "Prop_StationPaper_A.png md5 " + hashlib.md5(open(atlas_path, "rb").read()).hexdigest(),
              "slotsSource": "date_slots.json md5 " + hashlib.md5(open(slots_path, "rb").read()).hexdigest(),
              "slots": []}
    dates = [stamp(d) for d in nights()]
    for sid in ("dar_date", "register_date", "sign_out_date"):
        sd = slots[sid]
        cell = sd["cell"]
        pw, ph = PAGES[cell]
        path, ink, stroke = fx[sd["face"]]
        fnt = font(path, sd["cap_mm"])
        x0 = sd["x_mm"] * PX
        by = sd["baseline_mm"] * PX
        base = int(by)
        glyphs = {}
        for c in CHARS:
            im, ox0, oy0 = render(fnt, c, x0, by, stroke)
            bb = im.getbbox()
            crop = im.crop(bb)
            glyphs[c] = {"c": c, "w": crop.width, "h": crop.height,
                         "ox": ox0 + bb[0] - math.floor(x0), "oy": oy0 + bb[1] - base,
                         "adv": round(fnt.getlength(c), 4), "_cov": list(crop.tobytes())}
        kern = {}
        for a in CHARS:
            for b in CHARS:
                k = fnt.getlength(a + b) - fnt.getlength(a) - fnt.getlength(b)
                if abs(k) > 1e-3:
                    kern[a + b] = round(k, 4)
        # Pen rounding: the mode whose composition is closest to PIL drawing the whole string (a few dates).
        best = None
        for mode in ("round", "floor"):
            err = 0
            for s in ("10-31-90", "12-30-89", "01-01-90", "07-14-90", "08-28-90"):
                im, ox0, oy0 = render(fnt, s, x0, by, stroke)
                ref = im.load()
                got = compose(glyphs, kern, s, math.floor(x0) + (x0 - math.floor(x0)), base, mode)
                for yy in range(im.height):
                    for xx in range(im.width):
                        e = abs(ref[xx, yy] - got.get((ox0 + xx, oy0 + yy), 0))
                        if e > err:
                            err = e
            if best is None or err < best[1]:
                best = (mode, err)
        mode, err = best
        # The patch: every date's ink, plus Lanczos support (3 output texels) and one texel, in cell texels.
        sx, sy = CELL / round(pw * PX), CELL / round(ph * PX)
        X0 = Y0 = 10 ** 9
        X1 = Y1 = -10 ** 9
        widest = 0.0
        for s in dates:
            pen = x0
            prev = None
            for c in s:
                if prev is not None:
                    pen += kern.get(prev + c, 0.0)
                g = glyphs[c]
                gx = (math.floor(pen + .5) if mode == "round" else math.floor(pen)) + g["ox"]
                X0, X1 = min(X0, gx), max(X1, gx + g["w"])
                Y0, Y1 = min(Y0, base + g["oy"]), max(Y1, base + g["oy"] + g["h"])
                pen += g["adv"]
                prev = c
            widest = max(widest, (pen - x0) / PX)
        tx0 = max(0, math.floor(X0 * sx) - 4)
        tx1 = min(CELL, math.ceil(X1 * sx) + 4)
        ty0 = max(0, math.floor(Y0 * sy) - 4)
        ty1 = min(CELL, math.ceil(Y1 * sy) + 4)
        col, row = CELLS[cell] % GRID, CELLS[cell] // GRID
        bg = atlas.crop((col * CELL + tx0, row * CELL + ty0, col * CELL + tx1, row * CELL + ty1))
        px = list(bg.getdata())
        white = sum(1 for p in px if min(p) >= 250)
        slot = {"id": sid, "cell": CELLS[cell], "cellName": cell, "pageW": pw, "pageH": ph,
                "pageWpx": round(pw * PX), "pageHpx": round(ph * PX),
                "x": sd["x_mm"], "baseline": sd["baseline_mm"], "cap": sd["cap_mm"], "slotW": sd["w_mm"],
                "face": sd["face"], "fontSize": fnt.size, "stroke": stroke, "ink": list(ink),
                "penX": x0, "baseInt": base, "penRound": mode,
                "patch": [tx0, ty0, tx1 - tx0, ty1 - ty0],
                "bg": base64.b64encode(bg.tobytes()).decode("ascii"),
                "glyphs": [], "kern": [{"p": p, "k": k} for p, k in sorted(kern.items())]}
        for c in CHARS:
            g = glyphs[c]
            slot["glyphs"].append({"c": c, "w": g["w"], "h": g["h"], "ox": g["ox"], "oy": g["oy"], "adv": g["adv"],
                                   "a": base64.b64encode(bytes(g["_cov"])).decode("ascii")})
        result["slots"].append(slot)
        print("%-14s cell %-13s font %3d px  pen %-5s (max err vs PIL %3d)  widest %.1f mm of %.0f  patch %s  bg white %d/%d"
              % (sid, cell, fnt.size, mode, err, widest, sd["w_mm"], slot["patch"], white, len(px)))
    with open(out, "w", encoding="ascii", newline="\n") as f:
        json.dump(result, f, indent=1, ensure_ascii=True)
        f.write("\n")
    print("wrote", out, os.path.getsize(out), "bytes")


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3] if len(sys.argv) > 3 else None)
