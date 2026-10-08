"""In-game cue flipbook (Red 2026-10-07: "先把其中一套应用在游戏里"). 平面视觉.

The chosen set A + M4 + V1 as encoded print slices for a per-cell flipbook player:
  A   every arrow turns 90 degrees to point along the route and is FITTED to its field
      (field 0.654, motif 0.255, from motion_demo.FIT), centred in its field
  M4  the turn is a ratchet: three 30-degree clicks
  V1  stepping, every other field row on its own speed system. The upper chevrons (system A)
      tick 125 mm per click; the lower chevrons (system B) hop one unit (375 mm) on clicks 2
      and 5. Rails, diamonds and pendants never move; field arrows pass behind the rails.
      One loop = 6 clicks = one roll (750 mm) for every row, so click 6 is the turned state.

Slices (C00 shared; "+u" = arrows point to +x of the image = +u of FrontRoomsSurface's
PlanarFrame, t = cross(up, n)):
  C00            baseline = Q1b's hard-edge K00, bit-identical (same code path as build-print)
  C01..C03  +u   turn 30 / 60 / 90 (C03 = flow click 0)
  C04..C08  +u   flow clicks 1..5
  C09..C11  -u   turn 30 / 60 / 90
  C12..C16  -u   flow clicks 1..5

Encoding is print_tool build-print's: raw (density, cream) per ink at 4x, box-down, then the
visual chat's print_encode LUT (Tools/lookdev/print_encode_lut.json). Linear RGBA, R/G encoded,
B 0, A 1, no mips.

Usage: /usr/bin/python3 cue_flipbook.py [out_dir] [--size 2048] [--preview-only]
Default out: Tools/print/ink/art_from_graphic/cue_states/flipbook
"""
import hashlib
import json
import math
import os
import shutil
import subprocess
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
PRINT = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
sys.path.insert(0, os.path.join(PRINT, "patterns"))
sys.path.insert(0, PRINT)
sys.path.insert(0, HERE)
import hard_edge as he          # noqa: E402
import print_tool as pt         # noqa: E402
import motion_demo as M         # noqa: E402

TW, TH, UNIT, S55 = he.TW, he.TH, he.UNIT, he.S
FIT = M.FIT                                     # {'field': 0.654, 'motif': 0.255}
TURNS = (1 / 3, 2 / 3, 1.0)                     # M4: three 30-degree clicks
CLICKS = 6                                      # V1 loop: one roll per row system
ROW_A = [125.0 * c for c in range(CLICKS)]      # uppers tick 125 mm per click
ROW_B = [0.0, 0.0, 375.0, 375.0, 375.0, 750.0]  # lowers hop a unit on clicks 2 and 5


# ------------------------------------------------------------------ geometry (tile mm, y down)
def band(x0, x1, xc, y_apex, t, up):
    sg = 1 if up else -1
    top = [(x0, y_apex + sg * S55 * (xc - x0)), (xc, y_apex), (x1, y_apex + sg * S55 * (x1 - xc))]
    return top + [(x, y + t) for x, y in top][::-1]


class Group:
    """Bands that turn as one piece about their common centre."""
    def __init__(self, kind, row, bands):
        self.kind, self.row, self.bands = kind, row, bands          # bands: [(ink, poly)]
        xs = [x for _, p in bands for x, _ in p]
        ys = [y for _, p in bands for _, y in p]
        self.cx, self.cy = (min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2
        self.base = 1 if kind == "field" else -1                    # up chevrons turn clockwise to point +x

    def shapes(self, sign, tu, hoff):
        a = math.radians(90.0 * tu) * sign * self.base
        s = 1.0 + (FIT[self.kind] - 1.0) * tu
        ca, sa = math.cos(a), math.sin(a)
        out = []
        for ink, p in self.bands:
            q = []
            for x, y in p:
                rx, ry = x - self.cx, y - self.cy
                q.append((self.cx + s * (rx * ca - ry * sa) + sign * hoff, self.cy + s * (rx * sa + ry * ca)))
            out.append((ink, q))
        return out


def build():
    fields, motifs, statics = [], [], []
    depth = S55 * he.BAND_HW
    T = sum(t for _, t in he.MOTIF_BANDS)
    for u in (0, 1):
        dx, dy = u * UNIT, u * he.UNIT2_DROP
        fx0, fx1, fxc = he.FIELD_X0 + dx, he.FIELD_X1 + dx, he.FIELD_XC + dx
        for row, (apex, bands) in (("A", (he.UPPER_APEX, he.UPPER_BANDS)), ("B", (he.LOWER_APEX, he.LOWER_BANDS))):
            y, bl = apex + dy, []
            for ink, t in bands:
                bl.append((ink, band(fx0, fx1, fxc, y, t, True)))
                y += t
            fields.append(Group("field", row, bl))
        bx0, bx1, bxc = he.BAND_X0 + dx, he.BAND_X1 + dx, he.BAND_XC + dx
        for k in range(4):
            tip = he.MOTIF_EDGE_Y + k * he.PERIOD + depth
            y, bl = tip, []
            for ink, t in he.MOTIF_BANDS:
                bl.append((ink, band(bx0, bx1, bxc, y, t, False)))
                y += t
            motifs.append(Group("motif", None, bl))
            hw, hh = he.BAND_HW, S55 * he.BAND_HW
            cyd = tip - depth
            statics.append(("cream", [(bxc - hw, cyd), (bxc, cyd - hh), (bxc + hw, cyd), (bxc, cyd + hh)]))
            hw2 = he.DROP_HW
            hh2 = S55 * hw2
            cyp = tip + T + he.DROP_GAP + S55 * hw2
            statics.append(("deep", [(bxc - hw2, cyp), (bxc, cyp - hh2), (bxc + hw2, cyp), (bxc, cyp + hh2)]))
    stripes = [(ink, [(x0 + u * UNIT, 0.0), (x1 + u * UNIT, 0.0), (x1 + u * UNIT, TH), (x0 + u * UNIT, TH)])
               for u in (0, 1) for x0, x1, ink in he.STRIPES]
    # The rail cover: ground over everything between two field windows (the field arrows pass behind it).
    cover = [("ground", [(he.FIELD_X1 - UNIT + k * UNIT, 0.0), (he.FIELD_X0 + k * UNIT, 0.0),
                         (he.FIELD_X0 + k * UNIT, TH), (he.FIELD_X1 - UNIT + k * UNIT, TH)]) for k in (0, 1, 2)]
    return fields, motifs, statics, stripes, cover


FIELDS, MOTIFS, STATICS, STRIPES, COVER = build()


def wrap(pieces):
    """Copies shifted by whole rolls so every piece that touches the tile is drawn (seamless wrap)."""
    out = []
    for ink, poly in pieces:
        xs = [x for x, _ in poly]
        ys = [y for _, y in poly]
        for ox in (-2 * TW, -TW, 0.0, TW, 2 * TW):
            if max(xs) + ox <= 0 or min(xs) + ox >= TW:
                continue
            for oy in (-TH, 0.0, TH):
                if max(ys) + oy <= 0 or min(ys) + oy >= TH:
                    continue
                out.append((ink, [(x + ox, y + oy) for x, y in poly]))
    return out


def state_pieces(sign, tu, click):
    """Draw order: field arrows, rail cover, motif arrows, stripes, diamonds, pendants."""
    p = []
    for g in FIELDS:
        hoff = (ROW_A if g.row == "A" else ROW_B)[click] if click else 0.0
        p += g.shapes(sign, tu, hoff)
    p += COVER
    for g in MOTIFS:
        p += g.shapes(sign, tu, 0.0)
    p += STRIPES
    p += STATICS
    return wrap(p)


def states():
    """[(name, sign, turn, click)] in slice order; None = Q1b's K00 path."""
    out = [("C00_baseline", None, 0.0, 0)]
    for sign, tag in ((1, "pu"), (-1, "mu")):
        for i, tu in enumerate(TURNS):
            out.append((f"turn{(i + 1) * 30}_{tag}", sign, tu, 0))
        for c in range(1, CLICKS):
            out.append((f"flow{c}_{tag}", sign, 1.0, c))
    return [(f"C{i:02d}_{n.split('_', 1)[-1] if i else 'baseline'}", s, t, c) for i, (n, s, t, c) in enumerate(out)]


# ------------------------------------------------------------------ raster + encode
SS = he.SUPER


def labels(name, sign, tu, click, w, h):
    if sign is None:
        return he.rasterise(he.tile_pieces(he.build_regions()), w, h)   # exactly build-print's K00
    return he.rasterise(state_pieces(sign, tu, click), w, h)


def encode_slice(args):
    i, (name, sign, tu, click), size, out = args
    lab = labels(name, sign, tu, click, size * SS, size * SS)
    enc = he.encode_table().astype(np.float32)
    rd = he.box_down(enc[lab, 0], SS)
    rc = he.box_down(enc[lab, 1], SS)
    del lab
    lut = pt.load_lut()
    ed, ec = pt.print_encode(rd, rc, lut)
    rgba = np.stack([ed, ec, np.zeros_like(ed), np.ones_like(ed)], -1)
    Image.fromarray((np.clip(rgba, 0, 1) * 255 + .5).astype(np.uint8), "RGBA").save(os.path.join(out, "encoded", f"C{i:02d}.png"))
    # seam: the wrap columns/rows against their interior neighbours (raw density)
    sx = float(np.abs(rd[:, 0] - rd[:, -1]).mean() / max(np.abs(np.diff(rd, axis=1)).mean(), 1e-6))
    sy = float(np.abs(rd[0] - rd[-1]).mean() / max(np.abs(np.diff(rd, axis=0)).mean(), 1e-6))
    return {"file": f"encoded/C{i:02d}.png", "name": name, "raw_mean": [round(float(rd.mean()), 4), round(float(rc.mean()), 4)],
            "encoded_mean": [round(float(ed.mean()), 4), round(float(ec.mean()), 4)], "seam_xy": [round(sx, 2), round(sy, 2)]}


# ------------------------------------------------------------------ previews (reference palette)
PAL = np.array([he.hexc(he.PALETTE[n]) for n in he.LABELS], np.float32)


def preview_tile(st, ppm=0.4):
    name, sign, tu, click = st
    w, h = int(TW * ppm), int(TH * ppm)
    lab = labels(name, sign, tu, click, w * SS, h * SS)
    return he.box_down(PAL[lab], SS)


def contact_sheet(sts, out):
    tiles = [np.tile(preview_tile(s), (1, 2, 1)) for s in sts]
    th, tw = tiles[0].shape[:2]
    cols = 6
    rows = (len(tiles) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * (tw + 12) + 12, rows * (th + 40) + 12), (24, 24, 24))
    d = ImageDraw.Draw(sheet)
    font = ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial Bold.ttf", 15)
    for i, (t, s) in enumerate(zip(tiles, sts)):
        r, c = divmod(i, cols)
        x, y = 12 + c * (tw + 12), 12 + r * (th + 40)
        sheet.paste(Image.fromarray((np.clip(t, 0, 1) * 255 + .5).astype(np.uint8)), (x, y + 28))
        d.text((x, y + 4), s[0], fill=(244, 223, 59), font=font)
    sheet.save(out)


def timing_video(sts, out, fps=30):
    """One wall in the intended rhythm: baseline 1 s, three 30-degree clicks 0.3 s apart, hold, three
    flow loops at 4 clicks/s, retract (clicks back). Snap cross-fades of 2 frames. Both directions."""
    seq = []                                                           # (slice index, seconds)
    seq += [(0, 1.0), (1, .3), (2, .3), (3, .9)]
    for _ in range(3):
        seq += [(3, .25)] + [(3 + c, .25) for c in range(1, CLICKS)]
    seq += [(3, .5), (2, .3), (1, .3), (0, 1.0)]
    cache = {}

    def strip(i):
        if i not in cache:
            a = np.tile(preview_tile(sts[i]), (1, 3, 1))
            b = np.tile(preview_tile(sts[i + 8 if i else 0]), (1, 3, 1))
            gap = np.full((a.shape[0], 16, 3), .1, np.float32)
            cache[i] = np.concatenate([a, gap, b], 1)
        return cache[i]
    tmp = out + "_frames"
    shutil.rmtree(tmp, ignore_errors=True)
    os.makedirs(tmp)
    n, prev = 0, None
    for idx, dur in seq:
        cur = strip(idx)
        for f in range(int(round(dur * fps))):
            img = cur if (prev is None or f >= 2) else prev * (1 - (f + 1) / 3) + cur * ((f + 1) / 3)
            Image.fromarray((np.clip(img, 0, 1) * 255 + .5).astype(np.uint8)).save(os.path.join(tmp, f"f{n:04d}.png"))
            n += 1
        prev = cur
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", str(fps), "-i", os.path.join(tmp, "f%04d.png"),
                    "-vf", "pad=ceil(iw/2)*2:ceil(ih/2)*2:color=black", "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "18",
                    "-movflags", "+faststart", out], check=True)
    shutil.rmtree(tmp)


def main(argv):
    out = next((a for a in argv[1:] if not a.startswith("--")), os.path.join(HERE, "flipbook"))
    size = int(argv[argv.index("--size") + 1]) if "--size" in argv else 2048
    sts = states()
    os.makedirs(os.path.join(out, "encoded"), exist_ok=True)
    contact_sheet(sts, os.path.join(out, "flipbook_states.png"))
    timing_video(sts, os.path.join(out, "flipbook_timing.mp4"))
    if "--preview-only" in argv:
        print("previews in", out)
        return
    import multiprocessing as mp
    with mp.get_context("fork").Pool(6) as pool:
        report = pool.map(encode_slice, [(i, s, size, out) for i, s in enumerate(sts)])
    sha = hashlib.sha1(open(pt.LUT_PATH, "rb").read()).hexdigest()
    k00 = os.path.join(PRINT, "patterns", "out", "hard_edge", "encoded", "K00.png")
    same = None
    if os.path.exists(k00) and size == 2048:
        same = bool(np.array_equal(np.asarray(Image.open(k00)), np.asarray(Image.open(os.path.join(out, "encoded", "C00.png")))))
    layout = {"baseline": 0,
              "plus": {"turn": [1, 2, 3], "flow": [3, 4, 5, 6, 7, 8]},
              "minus": {"turn": [9, 10, 11], "flow": [11, 12, 13, 14, 15, 16]}}
    meta = {"name": "FR_Cue_HardEdge", "slices": len(sts), "sliceWidth": size, "sliceHeight": size,
            "tileMetres": list(pt.TILE_METRES), "encoded": True, "lut_sha1": sha, "mips": "none (builder makes them)",
            "encoding": "R/G = print_encode(raw density, raw cream); B = 0; A = 1; linear",
            "set": "A + M4 + V1", "fit": {k: round(v, 4) for k, v in FIT.items()},
            "turnDegrees": [30, 60, 90], "flow": {"clicks": CLICKS, "rowA_mm": ROW_A, "rowB_mm": ROW_B},
            "plusU": "arrows point to +x of the image = +u of PlanarFrame (t = cross(up, n))",
            "layout": layout, "c00_equals_q1b_k00": same, "slicesReport": report}
    json.dump(meta, open(os.path.join(out, "flipbook.json"), "w"), indent=2)
    print(json.dumps({k: v for k, v in meta.items() if k != "slicesReport"}, indent=1))
    for r in report:
        print(r["file"], r["name"], "seam", r["seam_xy"], "enc mean", r["encoded_mean"])


if __name__ == "__main__":
    main(sys.argv)
