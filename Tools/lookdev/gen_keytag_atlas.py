#!/usr/bin/env python3
"""Prop_KeyTagNo_A / _S: the typed key-tag atlas (D1.6 fix 1 cells, 2026-10-22) + the desk fob's STAIR A band (2026-10-31).

游戏视觉, station_labels (W/apply/station_labels.sh). The token cells are W/d16_work/atlas/make_atlases_v2.py's key-tag
section unchanged (its copy: Documentation/research/interactables/d16/data/fix1/make_atlases_v2.py.txt); this script
reproduces them pixel for pixel (--check) and adds one band:

  Atlas: 4096^2, 10 x 10 cells of 409.6 px, cell n = row * 10 + col, row 0 at the TOP (the kits' inserts ship on cell 0;
  the facade offsets them, FrontRoomsInteractableKit DoorParts.SetAtlasCell).
  - cells 0-24 "00".."24", 25-98 "B1".."B74" (B10+ at 0.8x on the same baseline), 99 blank paper: Courier Prime Regular,
    ink #1E1D1A on paper sRGB (220, 216, 204), full-size ink height 0.24 of a cell (DECISIONS #23 / #33).
  - THE DESK FOB (30L S11: Kit_KeyTag_Long_White at 1.5x, the station's floor key; 平面视觉's spec 10-31 01:2x):
    "STAIR A", Courier Prime Regular, ink #171717 (the HUD tag digits' face, DECISIONS #23 O-2), one line, no tracking
    change, centred in the insert window, advance width = 85 % of the window width. Not one of the 100 cells (they are
    all taken): its band is column 9 centred on the row 8 / row 9 boundary, i.e. inside the blank margins of cells 89 and
    99 that no crop reaches (round tag 0.22-0.78 of a cell, rect 0.346-0.654, plate 0.35-0.65, long 0.355-0.645; the
    key cabinet's index card uses rows 0/1 of columns 0-1). The tag shows it with _BaseMap_ST (1, 1, 0.9, -0.85)
    (FrontRoomsKeyIdentity.DeskFobST).
    The long tag's window is 50 x 14 mm (3.571:1) on a crop of the full cell width x 0.29 (3.448:1), so the insert
    stretches the atlas 3.57 % wider than tall. The fob's lettering is drawn pre-compensated (8.192 px/mm across, 8.485
    px/mm up), so on the tag it reads at the font's true proportions: em 10.12 mm, cap 5.87 mm = 0.419 of the window
    height (at 1.5x: em 15.18 mm, cap 8.80 mm of 21 mm).
  _S (mask): R smoothness paper 0.24 / ink 0.18, G 1, A 1 (as before; the fob's ink takes the ink value).

Usage: python3 gen_keytag_atlas.py [--root <Frontrooms3D>] [--out <dir>] [--report <json>] [--proof <png>] [--check]
  --out defaults to <root>/Assets/Resources/Surfaces/Textures. --check writes nothing: it renders and compares with the
  PNGs in --out (exit 1 on a difference), and prints where they differ.
Needs Pillow >= 10.1 (float font sizes) and numpy.
"""
import json
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))


def arg(name, default=None):
    return sys.argv[sys.argv.index(name) + 1] if name in sys.argv else default


ROOT = arg("--root", os.path.dirname(os.path.dirname(HERE)))
OUT = arg("--out", os.path.join(ROOT, "Assets", "Resources", "Surfaces", "Textures"))
REPORT = arg("--report")
PROOF = arg("--proof")
CHECK = "--check" in sys.argv
FONT = os.path.join(ROOT, "Assets", "Fonts", "Period1990", "CourierPrime", "CourierPrime-Regular.ttf")

N = 4096
CELL = N / 10.0
PAPER = (220, 216, 204)
INK = (0x1E, 0x1D, 0x1A)

# The desk fob (see the docstring).
FOB_TEXT = "STAIR A"
FOB_INK = (0x17, 0x17, 0x17)
FOB_COL, FOB_ROW_EDGE = 9, 9            # column 9; centred on the top edge of row 9 (the row 8 / 9 boundary)
FOB_ST = (1.0, 1.0, 0.9, -0.85)         # FrontRoomsKeyIdentity.DeskFobST
LONG_WINDOW_MM = (50.0, 14.0)           # interact_key_tag_long.py WIN
LONG_CROP = (1.0, 0.29)                 # interact_key_tag_long.py CROP
FOB_FIT = 0.85                          # advance width / window width
SS = 8                                  # supersampling of the fob's lettering


def token(n):
    if n < 25:
        return "%02d" % n
    if n <= 98:
        return "B%d" % (n - 24)
    return ""


def tokens():
    """The D1.6 fix 1 token cells, exactly as make_atlases_v2.py draws them: (albedo image, ink mask, rows, font px)."""
    target_h = 0.24 * CELL
    size = 200
    for _ in range(8):
        f = ImageFont.truetype(FONT, size)
        b = f.getbbox("0")
        size = max(10, int(round(size * target_h / (b[3] - b[1]))))
    f = ImageFont.truetype(FONT, size)
    f08 = ImageFont.truetype(FONT, int(round(size * 0.8)))
    img = Image.new("RGB", (N, N), PAPER)
    mask = Image.new("L", (N, N), 0)
    d = ImageDraw.Draw(img)
    dm = ImageDraw.Draw(mask)
    rows = []
    ref = f.getbbox("B1", anchor="ls")
    for n in range(100):
        r, c = divmod(n, 10)
        s = token(n)
        cx, cy = (c + .5) * CELL, (r + .5) * CELL
        if not s:
            rows.append({"cell": n, "text": "", "scale": 0})
            continue
        small = len(s) >= 3
        font = f08 if small else f
        bb = font.getbbox(s, anchor="ls")
        x = cx - (bb[0] + bb[2]) / 2.0
        y = cy - (ref[1] + ref[3]) / 2.0
        d.text((x, y), s, font=font, fill=INK, anchor="ls")
        dm.text((x, y), s, font=font, fill=255, anchor="ls")
        rows.append({"cell": n, "text": s, "scale": 0.8 if small else 1.0})
    return img, mask, rows, size


def fob():
    """The STAIR A band: (patch box in atlas px, coverage 0..1 float array, numbers for the report)."""
    win_w, win_h = LONG_WINDOW_MM
    px_mm_u = CELL * LONG_CROP[0] / win_w            # 8.192 px/mm across
    px_mm_v = CELL * LONG_CROP[1] / win_h            # 8.4846 px/mm up
    probe = ImageFont.truetype(FONT, 1000)
    adv_em = probe.getlength(FOB_TEXT) / 1000.0      # 4.2 em (Courier: 0.6 em a character)
    cap_em = -probe.getbbox("H", anchor="ls")[1] / 1000.0
    em_mm = FOB_FIT * win_w / adv_em                 # 10.119 mm on the tag
    em_v, em_u = em_mm * px_mm_v, em_mm * px_mm_u    # em in atlas px, up and across
    squeeze = em_u / em_v                            # 0.9655: the insert's stretch, undone
    cx = (FOB_COL + .5) * CELL                       # band centre in atlas px (y from the top)
    cy = FOB_ROW_EDGE * CELL
    adv_u = adv_em * em_u                            # 348.16 px = 0.85 of the cell width
    cap_v = cap_em * em_v
    x0, base = cx - adv_u / 2.0, cy + cap_v / 2.0    # advance box centred across (as the tokens), caps centred up
    pad = 8
    X0, Y0 = int(np.floor(x0)) - pad, int(np.floor(base - 1.2 * cap_v)) - pad
    X1, Y1 = int(np.ceil(x0 + adv_u)) + pad, int(np.ceil(base + 0.2 * cap_v)) + pad
    pw, ph = X1 - X0, Y1 - Y0
    sw, sh = int(round(pw * SS / squeeze)), ph * SS
    big = Image.new("L", (sw, sh), 0)
    font = ImageFont.truetype(FONT, em_v * SS)
    ImageDraw.Draw(big).text(((x0 - X0) * SS / squeeze, (base - Y0) * SS), FOB_TEXT, font=font, fill=255, anchor="ls")
    cov = np.asarray(big.resize((pw, ph), Image.BOX)).astype(np.float64) / 255.0
    band = (FOB_COL * CELL, cy - CELL * LONG_CROP[1] / 2, (FOB_COL + 1) * CELL, cy + CELL * LONG_CROP[1] / 2)
    ys, xs = np.nonzero(cov > 0.02)
    ink = (X0 + xs.min(), Y0 + ys.min(), X0 + xs.max() + 1, Y0 + ys.max() + 1)
    numbers = {
        "text": FOB_TEXT, "ink": "#171717", "font": "CourierPrime-Regular.ttf (DECISIONS #23 O-2)",
        "st": list(FOB_ST), "bandPx": [round(v, 2) for v in band],
        "windowMm": list(LONG_WINDOW_MM), "pxPerMmAcross": round(px_mm_u, 4), "pxPerMmUp": round(px_mm_v, 4),
        "fontPxUp": round(em_v, 3), "squeezeAcross": round(squeeze, 5),
        "advanceOfWindow": FOB_FIT, "advancePx": round(adv_u, 2), "emMmOnTag": round(em_mm, 3),
        "capMmOnTag": round(cap_em * em_mm, 3), "capOfWindowHeight": round(cap_em * em_mm / win_h, 4),
        "capMmOnTag1p5": round(1.5 * cap_em * em_mm, 3), "inkBoxPx": [int(v) for v in ink],
        "inkMarginToBandEdgePx": round(min(ink[1] - band[1], band[3] - ink[3]), 1),
        "inkGapToNearestCropPx": round(min(ink[1] - (8 + .78) * CELL, (9 + .22) * CELL - ink[3]), 1),   # the round tag's crop, the tallest
    }
    return (X0, Y0, X1, Y1), cov, numbers


def render():
    img, mask, rows, size = tokens()
    a = np.asarray(img).astype(np.float64)
    m = np.asarray(mask).astype(np.float32) / 255.0      # float32 as make_atlases_v2.py (its _S rounding)
    (X0, Y0, X1, Y1), cov, numbers = fob()
    if m[Y0:Y1, X0:X1].max() > 0 or a[Y0:Y1, X0:X1].min() < PAPER[2]:
        raise SystemExit("the fob band overlaps a token cell's ink")
    patch = a[Y0:Y1, X0:X1]
    for i in range(3):
        patch[..., i] = PAPER[i] + (FOB_INK[i] - PAPER[i]) * cov
    a[Y0:Y1, X0:X1] = patch
    m[Y0:Y1, X0:X1] = cov.astype(np.float32)
    A = Image.fromarray(np.round(a).astype(np.uint8), "RGB")
    S = np.zeros((N, N, 4), np.uint8)
    S[..., 0] = np.round((0.24 + (0.18 - 0.24) * m) * 255).astype(np.uint8)
    S[..., 1] = 255
    S[..., 3] = 255
    report = {"size": N, "font": "CourierPrime-Regular.ttf (DECISIONS #23)", "fontPx": size, "ink": "#1E1D1A", "paper_sRGB": PAPER,
              "cells": "0-24 00..24, 25-98 B1..B74 (B10+ at 0.8x, same baseline), 99 blank", "deskFob": numbers,
              "fobPatchPx": [X0, Y0, X1, Y1], "rows": rows}
    return A, Image.fromarray(S, "RGBA"), report


def main():
    A, S, report = render()
    names = ("Prop_KeyTagNo_A.png", "Prop_KeyTagNo_S.png")
    if CHECK:
        bad = 0
        for name, im in zip(names, (A, S)):
            path = os.path.join(OUT, name)
            have = np.asarray(Image.open(path).convert(im.mode))
            diff = np.argwhere(np.any(have != np.asarray(im), axis=-1))
            if len(diff):
                bad += 1
                (y0, x0), (y1, x1) = diff.min(0), diff.max(0)
                print("DIFFERS %s: %d px in x %d-%d, y %d-%d" % (name, len(diff), x0, x1 + 1, y0, y1 + 1))
            else:
                print("SAME %s" % name)
        sys.exit(1 if bad else 0)
    os.makedirs(OUT, exist_ok=True)
    A.save(os.path.join(OUT, names[0]))
    S.save(os.path.join(OUT, names[1]))
    if REPORT:
        with open(REPORT, "w", newline="\n") as f:
            json.dump(report, f, indent=1)
    if PROOF:
        X0, Y0, X1, Y1 = report["fobPatchPx"]
        p = A.crop((int(9 * CELL), int(8 * CELL), N, N)).resize((818, 1638), Image.LANCZOS)
        p.save(PROOF)
    print(json.dumps(report["deskFob"], indent=1))


if __name__ == "__main__":
    main()
