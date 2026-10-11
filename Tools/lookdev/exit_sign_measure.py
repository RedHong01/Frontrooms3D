"""EXIT sign acceptance numbers from raw HDR captures (exit-sign workflow, build stage).

Reads <dir>/raw_*.rgbf32 + .json written by FrontRoomsExitSignLookdev (Editor,
clone-only): linear radiance before post, camera and sign matrices. Projects
the letter polygons of exit_sign_geom.py onto the image and reports
  T2  LIT letter mean / lit wall luminance        (pass 1.5-2.5)
  T3  DEAD (L_face - L_letters) / L_face          (pass >= 0.5, UL 924)
  T4  per-letter means vs "I", spread, max/min    (E/X/T within +-0.08 of 0.54/0.65/0.53;
                                                   spread 0.4-0.75; max/min <= 40)
  python3 exit_sign_measure.py <dir> [--out report.json]
"""
import json
import os
import sys

import numpy as np
from PIL import Image, ImageDraw


class ndimage:
    """numpy stand-ins for scipy.ndimage.binary_erosion / binary_dilation with
    scipy's default cross (4-neighbour) element and a False border."""

    @staticmethod
    def _shifts(m):
        p = np.pad(m, 1, constant_values=False)
        return p[:-2, 1:-1], p[2:, 1:-1], p[1:-1, :-2], p[1:-1, 2:]

    @staticmethod
    def binary_erosion(m, iterations=1):
        m = np.asarray(m, bool)
        for _ in range(iterations):
            a, b, c, d = ndimage._shifts(m)
            m = m & a & b & c & d
        return m

    @staticmethod
    def binary_dilation(m, iterations=1):
        m = np.asarray(m, bool)
        for _ in range(iterations):
            a, b, c, d = ndimage._shifts(m)
            m = m | a | b | c | d
        return m

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "Blender", "frontrooms_kit", "assets"))
import exit_sign_geom as g  # noqa: E402

Z_PLATE_WALL = (62.5 - g.LIP_DEPTH) / 1000.0     # Kit_ExitSign plate front, sign space
YC_WALL = g.HOUSING_H / 2 / 1000.0


def load(dirn, name):
    meta = json.load(open(os.path.join(dirn, name + ".json")))
    w, h = meta["w"], meta["h"]
    img = np.fromfile(os.path.join(dirn, name + ".rgbf32"), np.float32).reshape(h, w, 3)
    M = lambda k: np.array(meta[k], np.float64).reshape(4, 4)
    return img, M("proj"), M("view"), M("sign"), w, h


def project(pts_local, P, V, S, w, h):
    out = []
    for x, y, z in pts_local:
        c = P @ V @ S @ np.array([x, y, z, 1.0])
        nx, ny = c[0] / c[3], c[1] / c[3]
        out.append(((nx + 1) / 2 * w, (ny + 1) / 2 * h))       # y from the bottom
    return out


def mask(polys_px, w, h, erode=2):
    im = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(im)
    for poly in polys_px:
        d.polygon([(x, h - y) for x, y in poly], fill=255)
    m = np.asarray(im)[::-1] > 127                              # back to row 0 = bottom
    if erode:
        m = ndimage.binary_erosion(m, iterations=erode)
    return m


def lum(img):
    return img[..., 0] * 0.2126 + img[..., 1] * 0.7152 + img[..., 2] * 0.0722


def letter_local(poly_open):
    return [(-(a) / 1000.0, YC_WALL + b / 1000.0, Z_PLATE_WALL) for a, b in (g.open_to_reader(p) for p in poly_open)]


def letters_masks(P, V, S, w, h, erode=2):
    polys = g.letter_polys_open()
    return {k: mask([project(letter_local(p), P, V, S, w, h) for p in polys[k]], w, h, erode) for k in "EXIT"}


def t4(dirn):
    img, P, V, S, w, h = load(dirn, "raw_t4_lit_dark")
    L = lum(img)
    ms = letters_masks(P, V, S, w, h, 3)
    means = {k: float(L[m].mean()) for k, m in ms.items()}
    spread = {k: float(L[m].std() / L[m].mean()) for k, m in ms.items()}
    allv = np.concatenate([L[m] for m in ms.values()])
    rel = {k: round(v / means["I"], 3) for k, v in means.items()}
    ok = all(abs(rel[k] - g.NIST_REL[k]) <= 0.08 for k in "EXT")
    return {"relI": rel, "target": g.NIST_REL, "spread": {k: round(v, 3) for k, v in spread.items()},
            "maxOverMin_p99.5/p0.5": round(float(np.percentile(allv, 99.5) / max(np.percentile(allv, 0.5), 1e-6)), 2),
            "pixels": {k: int(m.sum()) for k, m in ms.items()}, "passRel": ok,
            "passSpread": all(0.4 <= v <= 0.75 for v in spread.values())}


def t2(dirn):
    img, P, V, S, w, h = load(dirn, "raw_t2_lit_3m")
    L = lum(img)
    ms = letters_masks(P, V, S, w, h, 1)
    letters = np.concatenate([L[m] for m in ms.values()])
    # Wall patches either side of the sign, same height band, on the wall plane (sign space z = 0).
    patches = []
    for x0, x1 in ((.40, .70), (-.70, -.40)):
        quad = [(x0, .04, 0.0), (x1, .04, 0.0), (x1, .17, 0.0), (x0, .17, 0.0)]
        patches.append(mask([project(quad, P, V, S, w, h)], w, h, 2))
    wall = np.concatenate([L[m] for m in patches])
    ratio = float(letters.mean() / wall.mean())
    return {"letterMean": round(float(letters.mean()), 4), "wallMean": round(float(wall.mean()), 4), "ratio": round(ratio, 3),
            "pass": 1.5 <= ratio <= 2.5, "letterPixels": int(letters.size), "wallPixels": int(wall.size)}


def t3(dirn):
    img, P, V, S, w, h = load(dirn, "raw_t3_dead")
    L = lum(img)
    ms = letters_masks(P, V, S, w, h, 2)
    letters = np.concatenate([L[m] for m in ms.values()])
    # Plate inside the opening, away from the letters, the chevron scores and the lip shadow.
    inner = [(-(g.OPEN_W / 2 - 8) / 1000.0, YC_WALL + (-(g.OPEN_H / 2 - 8)) / 1000.0, Z_PLATE_WALL),
             ((g.OPEN_W / 2 - 8) / 1000.0, YC_WALL + (-(g.OPEN_H / 2 - 8)) / 1000.0, Z_PLATE_WALL),
             ((g.OPEN_W / 2 - 8) / 1000.0, YC_WALL + (g.OPEN_H / 2 - 8) / 1000.0, Z_PLATE_WALL),
             (-(g.OPEN_W / 2 - 8) / 1000.0, YC_WALL + (g.OPEN_H / 2 - 8) / 1000.0, Z_PLATE_WALL)]
    face = mask([project(inner, P, V, S, w, h)], w, h, 0)
    polys = g.letter_polys_open()
    near = mask([project(letter_local(p), P, V, S, w, h) for k in "EXIT<>" for p in polys[k]], w, h, 0)
    near = ndimage.binary_dilation(near, iterations=6)
    face &= ~near
    Lf, Ll = float(L[face].mean()), float(letters.mean())
    c = (Lf - Ll) / Lf
    return {"faceMean": round(Lf, 4), "letterMean": round(Ll, 4), "contrast": round(c, 3), "pass": c >= 0.5}


def main():
    d = sys.argv[1]
    out = {}
    for name, fn in (("T4_hot_spots", t4), ("T2_lit_level", t2), ("T3_unlit_contrast", t3)):
        try:
            out[name] = fn(d)
        except FileNotFoundError as e:
            out[name] = {"missing": str(e)}
    s = json.dumps(out, indent=1)
    print(s)
    if "--out" in sys.argv:
        open(sys.argv[sys.argv.index("--out") + 1], "w").write(s)


if __name__ == "__main__":
    main()
