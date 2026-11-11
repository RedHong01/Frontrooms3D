"""FRONTROOMSSS structures surfaces of group G3-a1 (structures_assets step 2, line visual (game visual), 2026-11-10; lands as
Tools/lookdev/structures/gen_structures_g3a1.py with P16 structures_l0_props): the SP3 computer room of 1988-91.

Run: venv/bin/python gen_structures_g3a1.py <out dir>
House style of gen_structures_g1b.py (numpy + Pillow, OpenGL +Y normals; _A sRGB albedo, _N linear normal, _S linear mask
R smoothness G cavity B metal A 1, _E emission sRGB-encoded linear). Every cell is drawn at twice its size and box-filtered
down (no aliased edges). Smoothness never falls under 0.18 and normals stay low and broad on dark faces (the 11-10 09:52
note on speckle at hero range: no high-frequency normal noise on dark paint).
  Prop_CompAtlas_{A 2048, N 1024, S 1024}  decal atlas of the computer room (kits map a part's face into one cell, uv_rect
      from CELLS below, kept in step with Tools/Blender/frontrooms_kit/assets/_struct_l0_computer.py): the 9-track tape pack's
      side face, the 10 in circular chart, a green-bar sheet (15 x 11 in, tractor margins, line-printer rows), the fanfold
      stack edge, carton print, the halon label, the 25 % perforated air panel (600 mm), three panel-meter dials, the reel
      label, cartridge labels, operator panel plates (CPU, tape, CRAC with its LCD, line printer, dot-matrix, PDU, disk pack,
      cartridge, comms), the EPO and halon plates, drive faceplates, the tape ribbon and 16 colour cells.
  Prop_CompAtlas_Phone_{A 1024, N 512, S 512}.
  Prop_CompIndicator_{A, E} 256  the lit lenses (LED lenses, square push-button lenses, a VFD status window, red digits):
      the ONE slot of a kit that the lamp modes / power state drive; Prop_CompIndicator_Off shares _A with no emission.
  Prop_CompIndicator_Phone_{A, E} 128.
  Prop_PerfSteel_{A, N, S} 512, tiling 0.096 m: perforated painted steel (4.8 mm holes, 8 mm staggered pitch); _Phone 256.
Period basis: 30_plan.md P16, 62_furniture_l0_office.md 2.3 / 3.1 (LS56, LS83-LS86, LS98) and 60_build.md G3-a1 (S1-S8).
No text, brand or logo: printing reads as rows of marks, plates as engraved borders, dials as ticks without numerals.
ASCII only.
"""
import math
import os
import sys

import numpy as np
from PIL import Image

OUT = sys.argv[1] if len(sys.argv) > 1 else "."
os.makedirs(OUT, exist_ok=True)
RNG = np.random.default_rng(1989)
SIZE = 2048

# (x, y, w, h) in pixels of the 2048 atlas, y from the top. Kept in step with _struct_l0_computer.CELLS.
CELLS = {
    "PACK": (0, 0, 512, 512), "CHART": (512, 0, 512, 512), "GREENBAR": (1024, 0, 640, 470),
    "GREENBAR_EDGE": (1664, 0, 384, 96), "CARTON": (1664, 96, 384, 224), "HALON_LABEL": (1664, 320, 384, 192),
    "PERF_PANEL": (0, 512, 512, 512), "METER_V": (512, 512, 192, 192), "METER_A": (704, 512, 192, 192),
    "METER_K": (896, 512, 192, 192), "REEL_LABEL": (512, 704, 256, 128), "CART_LABEL": (768, 704, 256, 64),
    "PANEL_CPU": (1024, 1280, 1024, 440), "PANEL_TAPE": (0, 1280, 1024, 232), "PANEL_PDU": (1088, 832, 448, 192),
    "PANEL_CRAC": (1536, 512, 512, 256), "PANEL_PRINTER": (1536, 768, 256, 128), "PANEL_DOTMATRIX": (1792, 768, 256, 128),
    "PLATE_EPO": (1536, 896, 256, 128), "PLATE_HALON": (1792, 896, 256, 128), "COLOURS": (0, 1024, 512, 128),
    "TAPE": (512, 1024, 512, 64), "DISK_FACE": (1024, 1024, 512, 256), "PANEL_DISKPACK": (1536, 1024, 256, 128),
    "PANEL_CART": (1792, 1024, 256, 128), "PANEL_COMMS": (1536, 1152, 512, 128), "TAPE_BACK": (512, 1088, 512, 64),
}
# Colour cells (index -> name, sRGB, smoothness): 8 x 2 cells of 64 px in COLOURS.
COLOURS = [
    ("belt_blue", (0.11, 0.24, 0.52), 0.55), ("belt_grey", (0.46, 0.47, 0.48), 0.5), ("belt_white", (0.84, 0.84, 0.80), 0.5),
    ("belt_red", (0.60, 0.11, 0.09), 0.55), ("belt_yellow", (0.84, 0.67, 0.14), 0.55), ("belt_black", (0.06, 0.06, 0.06), 0.5),
    ("cable_beige", (0.74, 0.70, 0.60), 0.42), ("cable_grey", (0.50, 0.50, 0.49), 0.42),
    ("cable_black", (0.05, 0.05, 0.05), 0.45), ("cable_blue", (0.15, 0.25, 0.50), 0.45), ("oxide", (0.17, 0.12, 0.09), 0.55),
    ("reel_smoke", (0.50, 0.58, 0.64), 0.78), ("hub_dark", (0.10, 0.10, 0.10), 0.5), ("rubber", (0.035, 0.035, 0.035), 0.25),
    ("ring_red", (0.68, 0.09, 0.07), 0.6), ("paper", (0.91, 0.90, 0.86), 0.24),
]


def hexc(s):
    return np.array([int(s[i:i + 2], 16) / 255 for i in (1, 3, 5)], np.float32)


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def spectral(h, w, beta, seed):
    r = np.random.default_rng(seed)
    F = np.fft.fft2(r.standard_normal((h, w)))
    fy = np.fft.fftfreq(h)[:, None] * h
    fx = np.fft.fftfreq(w)[None, :] * w
    f = np.sqrt(fx * fx + fy * fy)
    f[0, 0] = 1
    amp = 1.0 / np.power(np.maximum(f, 1.0), beta)
    amp[0, 0] = 0
    n = np.real(np.fft.ifft2(F * amp))
    n -= n.min()
    n /= max(1e-8, n.max())
    return n.astype(np.float32)


def down2(a):
    h, w = a.shape[:2]
    return a.reshape(h // 2, 2, w // 2, 2, *a.shape[2:]).mean(axis=(1, 3))


def save(name, arr, mode):
    a = np.clip(arr, 0, 1)
    Image.fromarray((a * 255 + .5).astype(np.uint8), mode).save(os.path.join(OUT, name + ".png"), optimize=True)


class Cell:
    """One cell drawn at twice its atlas size: A sRGB albedo, H height (mm), R smoothness, C cavity; mm = mm per (2x) pixel."""

    def __init__(self, w, h, mm, base=(0.5, 0.5, 0.5), rough=0.4):
        self.w, self.h, self.mm = 2 * w, 2 * h, mm / 2.0
        self.A = np.ones((self.h, self.w, 3), np.float32) * np.array(base, np.float32)
        self.H = np.zeros((self.h, self.w), np.float32)
        self.R = np.full((self.h, self.w), rough, np.float32)
        self.C = np.ones((self.h, self.w), np.float32)
        self.y, self.x = np.mgrid[0:self.h, 0:self.w].astype(np.float32)

    def xy_mm(self, cx=None, cy=None):
        """Coordinates in mm from a centre (default the cell centre), y UP."""
        cx = self.w / 2 if cx is None else cx
        cy = self.h / 2 if cy is None else cy
        return (self.x + .5 - cx) * self.mm, (cy - self.y - .5) * self.mm

    def paint(self, mask, col, rough=None, height=None, cavity=None):
        m = mask[..., None]
        self.A = self.A * (1 - m) + np.array(col, np.float32) * m
        if rough is not None:
            self.R = self.R * (1 - mask) + rough * mask
        if height is not None:
            self.H = self.H * (1 - mask) + height * mask
        if cavity is not None:
            self.C = self.C * (1 - mask) + cavity * mask

    def rect_mask(self, x0, y0, x1, y1, soft=1.0):
        """Mask of a rectangle in 2x pixels (y down), antialiased by `soft` px."""
        mx = smooth(-soft, soft, self.x - x0) * smooth(-soft, soft, x1 - self.x)
        my = smooth(-soft, soft, self.y - y0) * smooth(-soft, soft, y1 - self.y)
        return (mx * my).astype(np.float32)

    def frame_mask(self, x0, y0, x1, y1, t, soft=0.8):
        return np.clip(self.rect_mask(x0, y0, x1, y1, soft) - self.rect_mask(x0 + t, y0 + t, x1 - t, y1 - t, soft), 0, 1)

    def disc_mask(self, cx, cy, r, soft=1.0):
        d = np.hypot(self.x + .5 - cx, self.y + .5 - cy)
        return smooth(r + soft, r - soft, d).astype(np.float32)

    def ring_mask(self, cx, cy, r0, r1, soft=0.8):
        d = np.hypot(self.x + .5 - cx, self.y + .5 - cy)
        return (smooth(r0 - soft, r0 + soft, d) * smooth(r1 + soft, r1 - soft, d)).astype(np.float32)

    def line_mask(self, x0, y0, x1, y1, width, soft=0.8):
        px, py = self.x + .5, self.y + .5
        dx, dy = x1 - x0, y1 - y0
        L2 = max(dx * dx + dy * dy, 1e-6)
        t = np.clip(((px - x0) * dx + (py - y0) * dy) / L2, 0, 1)
        d = np.hypot(px - (x0 + t * dx), py - (y0 + t * dy))
        return smooth(width / 2 + soft, width / 2 - soft, d).astype(np.float32)

    def grain(self, amount, beta=1.6, seed=0, rough_amount=0.0):
        n = spectral(self.h, self.w, beta, seed) - .5
        self.A = np.clip(self.A * (1 + amount * n[..., None]), 0, 1)
        if rough_amount:
            self.R = np.clip(self.R + rough_amount * n, 0.18, 0.95)


# ============================================================================ the cells
def cell_pack(w, h):
    """The 9-track pack's side face (the wound tape's edge) for a reel face of radius 0.1335 m filling the cell: dark oxide
    brown with winding-tension bands, a few popped strands, a fine sheen; the hub zone (r < 48 mm) neutral (geometry)."""
    c = Cell(w, h, 267.0 / w, base=(0.30, 0.30, 0.30), rough=0.45)
    X, Y = c.xy_mm()
    r = np.hypot(X, Y)
    rr = np.linspace(0, 140, 4000)
    band = np.zeros_like(rr)
    rng = np.random.default_rng(7)
    for k in range(9):
        band += rng.uniform(.3, 1) * np.sin(2 * math.pi * rr / rng.uniform(1.2, 9.0) + rng.uniform(0, 6.3))
    band = band / np.abs(band).max()
    fine = np.convolve(rng.standard_normal(rr.size), np.ones(9) / 9, mode="same")
    fine /= np.abs(fine).max()
    b = np.interp(r, rr, band)
    f = np.interp(r, rr, fine)
    oxide = np.array((0.155, 0.112, 0.083), np.float32)
    tone = 1 + 0.10 * b + 0.05 * f
    pack = smooth(47.0, 48.5, r)
    c.A = c.A * (1 - pack[..., None]) + (oxide * tone[..., None]) * pack[..., None]
    c.R = c.R * (1 - pack) + np.clip(0.58 + 0.10 * b, 0.4, 0.72) * pack
    # popped strands: three thin rings standing 0.05 mm proud, a touch lighter
    for rp in (63.4, 81.7, 97.2):
        m = smooth(0.35, 0.0, np.abs(r - rp)) * pack
        c.paint(m, (0.24, 0.18, 0.14), rough=0.62, height=0.05)
    # the pack's outer turns: a slightly lighter, matt edge where the last wraps were wound loose
    edge = smooth(126.0, 131.0, r) * pack
    c.A = c.A * (1 - 0.25 * edge[..., None]) + np.array((0.22, 0.17, 0.13), np.float32) * 0.25 * edge[..., None]
    return c


def cell_chart(w, h):
    """A 10 in (254 mm) circular chart, 24 h: printed rings every 1.8 mm (heavier every 5), 24 curved time lines on the pen's
    arc, a purple temperature trace and a green humidity trace, the centre hole. No numerals."""
    c = Cell(w, h, 254.0 / w, base=(0.925, 0.915, 0.875), rough=0.22)
    X, Y = c.xy_mm()
    r = np.hypot(X, Y)
    th = np.arctan2(Y, X)
    ink = np.array((0.74, 0.30, 0.30), np.float32)
    inside = smooth(126.5, 125.5, r)
    # rings from 30 to 120 mm
    k = (r - 30.0) / 1.8
    kk = np.round(k)
    dr = np.abs(k - kk) * 1.8
    heavy = (np.mod(kk, 5) == 0)
    width = np.where(heavy, 0.30, 0.13)
    ring = smooth(width + .12, width - .02, dr) * (r > 29.5) * (r < 120.5) * np.where(heavy, 0.85, 0.55)
    # time lines on the pen arm's arc (radius 115 mm from a pivot outside the chart)
    Ra = 115.0
    phi = th - np.arcsin(np.clip(r / (2 * Ra), -1, 1))
    t = phi * 24 / (2 * math.pi)
    tt = np.round(t)
    dt = np.abs(t - tt) * (2 * math.pi / 24) * np.maximum(r, 1)
    major = (np.mod(tt, 6) == 0)
    tw = np.where(major, 0.32, 0.14)
    tl = smooth(tw + .12, tw - .02, dt) * (r > 30) * (r < 120.5) * np.where(major, 0.85, 0.5)
    m = np.clip(ring + tl, 0, 1) * inside
    c.A = c.A * (1 - m[..., None]) + ink * m[..., None]
    # traces
    rng = np.random.default_rng(31)
    ang = np.linspace(-math.pi, math.pi, 2000)
    for base, amp, col, seed, start, end in ((72.0, 6.0, (0.42, 0.18, 0.55), 1, -3.0, 2.2), (98.0, 4.0, (0.16, 0.45, 0.22), 2, -3.0, 2.2)):
        g = np.random.default_rng(seed)
        prof = base + amp * np.sin(ang * 1 + g.uniform(0, 6)) * 0.4 + np.convolve(g.standard_normal(ang.size), np.ones(60) / 60, "same") * amp * 2.2
        rt = np.interp(th, ang, prof)
        pen = smooth(0.5, 0.25, np.abs(r - rt)) * ((th > start) & (th < end)) * inside
        c.A = c.A * (1 - pen[..., None]) + np.array(col, np.float32) * pen[..., None]
        c.H += 0.01 * pen
    hole = smooth(5.2, 4.6, r)
    c.paint(hole, (0.05, 0.05, 0.05), rough=0.3, height=-0.4, cavity=0.3)
    outside = 1 - inside
    c.paint(outside, (0.80, 0.78, 0.73), rough=0.3)
    c.grain(0.03, 1.4, 41)
    return c


def cell_greenbar(w, h):
    """A sheet of 15 x 11 in continuous forms: 1/2 in green bars, tractor margins with 5/32 in holes at 1/2 in, the margin
    perforations, rows of line-printer marks at 10 cpi / 6 lpi (no legible text), the fold perforation along top and bottom."""
    W, H = 381.0, 279.4
    c = Cell(w, h, W / w, base=(0.93, 0.93, 0.90), rough=0.24)
    X, Y = c.xy_mm(0, 0)        # from the top-left corner, Y up (negative going down)
    Y = -Y
    bar = (np.floor(Y / 12.7) % 2 == 1) & (X > 12.7) & (X < W - 12.7)
    c.paint(bar.astype(np.float32) * 0.9, (0.80, 0.895, 0.80))
    # print rows: lines at 6 lpi from 25 mm, chars at 10 cpi from 22 mm, random runs (words) and blank lines
    rng = np.random.default_rng(1990)
    marks = np.zeros((c.h, c.w), np.float32)
    lpi = 25.4 / 6
    cpi = 25.4 / 10
    px = 1.0 / c.mm
    for li in range(int((H - 30) / lpi)):
        if rng.random() < 0.22:
            continue
        y0 = 18 + li * lpi
        ncol = int(rng.integers(20, 120))
        col = 0
        while col < ncol:
            run = int(rng.integers(2, 11))
            for _ in range(run):
                if col >= ncol:
                    break
                cx = 22.0 + col * cpi
                # a glyph: a 1.8 x 2.6 mm block of dot-matrix ink at random density (a 5 x 7 dot field, a few dots off)
                ix0, ix1 = int(cx * px), int((cx + 1.8) * px) + 1
                iy0, iy1 = int(y0 * px), int((y0 + 2.6) * px) + 1
                g = rng.uniform(0.35, 0.85, size=(iy1 - iy0, ix1 - ix0)) * (rng.random((iy1 - iy0, ix1 - ix0)) < 0.7)
                marks[iy0:iy1, ix0:ix1] = np.maximum(marks[iy0:iy1, ix0:ix1], g)
                col += 1
            col += int(rng.integers(1, 3))
    c.A = c.A * (1 - 0.8 * marks[..., None]) + np.array((0.16, 0.16, 0.17), np.float32) * 0.8 * marks[..., None]
    c.A = c.A * 0.999
    # tractor holes and perforations
    for xs in (6.35, W - 6.35):
        for k in range(23):
            yc = 6.35 + 12.7 * k
            d = np.hypot(X - xs, Y - yc)
            hole = smooth(2.2, 1.8, d)
            c.paint(hole, (0.12, 0.12, 0.12), rough=0.2, height=-0.15, cavity=0.2)
    for xp in (12.7, W - 12.7):
        perf = (np.abs(X - xp) < 0.25) & (np.mod(Y, 2.5) < 1.6)
        c.paint(perf.astype(np.float32) * 0.6, (0.70, 0.70, 0.68), height=-0.03)
    for yp in (0.6, H - 0.6):
        perf = (np.abs(Y - yp) < 0.5) & (np.mod(X, 2.5) < 1.6)
        c.paint(perf.astype(np.float32) * 0.6, (0.72, 0.72, 0.70), height=-0.03)
    c.grain(0.025, 1.3, 51)
    return c


def cell_greenbar_edge(w, h):
    """The side of a fanfold stack (u along the edge, tiles): sheet edges, faint green where the bars reach the edge, the
    folds every 11 in drawn as slightly darker lines."""
    c = Cell(w, h, 60.0 / h, base=(0.90, 0.90, 0.87), rough=0.3)
    rng = np.random.default_rng(5)
    rows = np.cumsum(rng.uniform(0.6, 1.4, c.h)) % 3
    lines = (rows < 0.35).astype(np.float32)
    c.A *= (1 - 0.10 * lines[:, None, None] * np.ones((1, c.w, 1)))
    tint = (np.sin(c.x / c.w * 2 * math.pi * 6) > 0).astype(np.float32) * 0.5
    c.A = c.A * (1 - 0.06 * tint[..., None]) + np.array((0.80, 0.89, 0.80), np.float32) * 0.06 * tint[..., None]
    c.H += 0.03 * lines[:, None]
    c.grain(0.03, 1.2, 9)
    return c


def cell_carton(w, h):
    """A printed side of a forms carton: kraft board, a printed band, a blank white label panel, the printed corner marks."""
    c = Cell(w, h, 400.0 / w, base=(0.60, 0.46, 0.31), rough=0.3)
    c.grain(0.10, 1.5, 61, 0.05)
    c.paint(c.rect_mask(0, c.h * .12, c.w, c.h * .30), (0.20, 0.27, 0.42), rough=0.33)
    c.paint(c.rect_mask(c.w * .55, c.h * .45, c.w * .92, c.h * .82), (0.88, 0.87, 0.82), rough=0.3)
    for k in range(4):
        c.paint(c.rect_mask(c.w * .59, c.h * (.52 + .07 * k), c.w * (.80 - .05 * (k % 2)), c.h * (.52 + .07 * k) + 3), (0.45, 0.44, 0.42))
    c.paint(c.rect_mask(c.w * .08, c.h * .45, c.w * .40, c.h * .52), (0.20, 0.27, 0.42))
    c.paint(c.frame_mask(c.w * .08, c.h * .58, c.w * .30, c.h * .86, 3), (0.20, 0.27, 0.42))
    # a strip of tape across the top flap edge
    c.paint(c.rect_mask(c.w * .35, 0, c.w * .65, c.h * .06), (0.68, 0.56, 0.38), rough=0.6)
    return c


def cell_halon_label(w, h):
    """A cylinder's label: white, a red band, a hazard diamond, rows of printing as bars (no text)."""
    c = Cell(w, h, 200.0 / w, base=(0.89, 0.88, 0.84), rough=0.4)
    c.paint(c.rect_mask(0, 0, c.w, c.h * .22), (0.66, 0.10, 0.08), rough=0.42)
    cx, cy, s = c.w * .17, c.h * .60, c.h * .24
    d = np.abs(c.x - cx) + np.abs(c.y - cy)
    c.paint(smooth(s + 1, s - 1, d) - smooth(s - 5, s - 7, d), (0.10, 0.10, 0.10))
    c.paint(smooth(s - 7, s - 9, d), (0.85, 0.55, 0.12))
    rng = np.random.default_rng(3)
    for k in range(8):
        y = c.h * (.30 + .08 * k)
        c.paint(c.rect_mask(c.w * .36, y, c.w * (.36 + rng.uniform(.25, .58)), y + 5), (0.30, 0.30, 0.30))
    c.grain(0.03, 1.4, 13)
    return c


def cell_perf_panel(w, h):
    """A 600 mm perforated air panel (25-30 % open): painted grey steel, 6.35 mm holes at 11 mm staggered pitch inside a
    38 mm plain border, the black edge band at the rim (as G1's panels)."""
    c = Cell(w, h, 600.0 / w, base=(0.60, 0.60, 0.585), rough=0.42)
    c.grain(0.04, 1.6, 71, 0.03)
    X, Y = c.xy_mm()
    p = 11.0
    row = np.floor(Y / (p * 0.866))
    xx = X + (row % 2) * p / 2
    gx = xx - np.round(xx / p) * p
    gy = Y - (row + .5) * p * 0.866
    d = np.hypot(gx, gy)
    field = (np.abs(X) < 262) & (np.abs(Y) < 262)
    hole = smooth(3.4, 2.9, d) * field
    c.paint(hole, (0.025, 0.025, 0.025), rough=0.2, height=-0.6, cavity=0.15)
    rim = smooth(3.9, 3.2, d) * (1 - hole) * field
    c.H -= 0.15 * rim
    band = 1 - c.rect_mask(4, 4, c.w - 4, c.h - 4, 1.0)
    c.paint(band, (0.04, 0.04, 0.04), rough=0.4)
    return c


def cell_meter(w, h, red_end=True, needle=0.42):
    """A panel meter's dial (the bezel and glass are geometry): cream face, a 100 degree arc of ticks, a red band at the top
    end, the needle at rest (painted), its pivot. No numerals."""
    c = Cell(w, h, 70.0 / w, base=(0.90, 0.885, 0.835), rough=0.35)
    X, Y = c.xy_mm(c.w / 2, c.h * 0.78)
    r = np.hypot(X, Y)
    a = np.degrees(np.arctan2(X, Y))
    ink = (0.08, 0.08, 0.08)
    arc = smooth(0.35, 0.15, np.abs(r - 44.0)) * (np.abs(a) < 50)
    c.paint(arc, ink)
    for k in range(41):
        ak = -50 + 2.5 * k
        L = 6.0 if k % 5 == 0 else 3.2
        wdt = 0.55 if k % 5 == 0 else 0.3
        da = np.radians(a - ak) * r
        tick = smooth(wdt, wdt * .4, np.abs(da)) * (r > 44.0) * (r < 44.0 + L)
        c.paint(tick, ink)
    if red_end:
        band = (r > 40.5) & (r < 43.5) & (a > 35) & (a < 50)
        c.paint(band.astype(np.float32), (0.70, 0.12, 0.10))
    ang = math.radians(-50 + 100 * needle)
    c.paint(c.line_mask(c.w / 2, c.h * 0.78, c.w / 2 + math.sin(ang) * 50 / c.mm, c.h * 0.78 - math.cos(ang) * 50 / c.mm, 0.9 / c.mm), (0.05, 0.05, 0.05), height=0.2)
    c.paint(c.disc_mask(c.w / 2, c.h * 0.78, 3.0 / c.mm), (0.10, 0.10, 0.10), height=0.4)
    return c


def cell_reel_label(w, h):
    c = Cell(w, h, 90.0 / w, base=(0.90, 0.89, 0.84), rough=0.3)
    c.paint(c.rect_mask(0, 0, c.w * .07, c.h), (0.66, 0.12, 0.10))
    for k in range(5):
        y = c.h * (.18 + .17 * k)
        c.paint(c.rect_mask(c.w * .1, y, c.w * .97, y + 1.6), (0.45, 0.55, 0.75))
    rng = np.random.default_rng(17)
    for k in range(3):
        y = c.h * (.12 + .17 * k)
        x = c.w * .14
        for s in range(int(rng.integers(4, 9))):
            x1 = x + rng.uniform(8, 26)
            c.paint(c.line_mask(x, y + rng.uniform(-2, 2), x1, y + rng.uniform(-3, 3), 2.2), (0.12, 0.13, 0.20))
            x = x1 + rng.uniform(2, 10)
            if x > c.w * .9:
                break
    c.grain(0.03, 1.4, 19)
    return c


def cell_cart_labels(w, h):
    c = Cell(w, h, 102.0 / w, base=(0.88, 0.87, 0.83), rough=0.35)
    cols = [(0.15, 0.30, 0.62), (0.18, 0.48, 0.25), (0.66, 0.13, 0.10), (0.85, 0.68, 0.15)]
    q = c.w // 4
    for i, col in enumerate(cols):
        c.paint(c.rect_mask(i * q, 0, i * q + q, c.h * .28), col)
        for k in range(3):
            y = c.h * (.42 + .17 * k)
            c.paint(c.rect_mask(i * q + q * .12, y, i * q + q * (.85 - .15 * (k % 2)), y + 2.2), (0.35, 0.35, 0.35))
        c.paint(c.frame_mask(i * q + 1, 1, i * q + q - 1, c.h - 1, 1.5), (0.55, 0.55, 0.52))
    return c


# ---------------------------------------------------------------------------- operator panels (layouts shared with the kits)
# Button / lamp sites in plate fractions (u from the left, v from the bottom), kept in step with _struct_l0_computer.PANELS.
PANELS = {
    "PANEL_CPU": {"size": (0.56, 0.24), "buttons": [(0.30 + 0.08 * i, 0.50) for i in range(6)], "bsize": 0.026,
                  "key": (0.12, 0.50), "window": (0.79, 0.34, 0.95, 0.66)},
    "PANEL_TAPE": {"size": (0.66, 0.15), "buttons": [(0.12 + 0.135 * i, 0.50) for i in range(6)], "bsize": 0.030},
    "PANEL_CRAC": {"size": (0.40, 0.20), "buttons": [(0.66 + 0.13 * (i % 3) - 0.0, 0.74 - 0.26 * (i // 3)) for i in range(6)],
                   "bsize": 0.030, "lcd": (0.07, 0.42, 0.55, 0.86), "leds": [(0.12 + 0.11 * i, 0.22) for i in range(4)]},
    "PANEL_PRINTER": {"size": (0.30, 0.15), "buttons": [(0.14 + 0.18 * i, 0.32) for i in range(4)], "bsize": 0.030,
                      "leds": [(0.14, 0.76), (0.28, 0.76)], "window": (0.60, 0.60, 0.86, 0.90)},
    "PANEL_DOTMATRIX": {"size": (0.17, 0.065), "buttons": [(0.42 + 0.16 * i, 0.45) for i in range(3)], "bsize": 0.014,
                        "leds": [(0.10, 0.65), (0.19, 0.65), (0.28, 0.65)]},
    "PANEL_PDU": {"size": (0.56, 0.24), "breakers": [(0.08 + 0.0767 * i, 0.38) for i in range(12)], "bsize": (0.026, 0.07)},
    "PANEL_DISKPACK": {"size": (0.30, 0.15), "buttons": [(0.25, 0.48), (0.50, 0.48), (0.75, 0.48)], "bsize": 0.038},
    "PANEL_CART": {"size": (0.30, 0.15), "buttons": [(0.12 + 0.16 * i, 0.30) for i in range(4)], "bsize": 0.026,
                   "window": (0.10, 0.58, 0.62, 0.86)},
    "PANEL_COMMS": {"size": (0.60, 0.15), "leds": [(0.07 + 0.058 * i, 0.25 + 0.25 * j) for j in range(3) for i in range(16)]},
}


def _panel_base(name, w, h, base, rough=0.45):
    sz = PANELS[name]["size"]
    c = Cell(w, h, sz[0] * 1000.0 / w, base=base, rough=rough)
    c.grain(0.035, 1.8, hash(name) % 1000, 0.02)
    return c, sz


def _site_px(c, uv):
    return uv[0] * c.w, (1 - uv[1]) * c.h


def _frames(c, sites, half_mm, ink, t=1.6):
    for s in sites:
        x, y = _site_px(c, s)
        hp = half_mm / c.mm
        c.paint(c.frame_mask(x - hp, y - hp, x + hp, y + hp, t), ink)


def cell_panel(name, w, h):
    P = PANELS[name]
    dark = name in ("PANEL_CPU", "PANEL_TAPE", "PANEL_PRINTER", "PANEL_CART", "PANEL_COMMS", "PANEL_DISKPACK")
    base = (0.11, 0.11, 0.115) if dark else ((0.70, 0.70, 0.68) if name in ("PANEL_CRAC", "PANEL_PDU") else (0.78, 0.75, 0.66))
    ink = (0.80, 0.80, 0.77) if dark else (0.20, 0.20, 0.21)
    c, sz = _panel_base(name, w, h, base, 0.42 if dark else 0.38)
    if "buttons" in P:
        _frames(c, P["buttons"], P["bsize"] * 1000 / 2 + 3.0, ink)
        for s in P["buttons"]:
            x, y = _site_px(c, s)
            hp = (P["bsize"] * 1000 / 2 + 6.5) / c.mm
            c.paint(c.rect_mask(x - hp * .7, y - hp - 4.5 / c.mm, x + hp * .7, y - hp - 3.0 / c.mm), ink)   # blank legend bar
    if "leds" in P:
        for s in P["leds"]:
            x, y = _site_px(c, s)
            c.paint(c.ring_mask(x, y, 3.2 / c.mm, 4.0 / c.mm), ink)
            if name == "PANEL_COMMS":
                c.paint(c.rect_mask(x - 9 / c.mm, y + 6 / c.mm, x + 9 / c.mm, y + 7.4 / c.mm), (0.55, 0.55, 0.52))
    if "window" in P:
        u0, v0, u1, v1 = P["window"]
        c.paint(c.rect_mask(u0 * c.w, (1 - v1) * c.h, u1 * c.w, (1 - v0) * c.h), (0.03, 0.035, 0.035), rough=0.85, height=-0.3)
        c.paint(c.frame_mask(u0 * c.w - 4, (1 - v1) * c.h - 4, u1 * c.w + 4, (1 - v0) * c.h + 4, 1.6), ink)
    if "key" in P:
        x, y = _site_px(c, P["key"])
        c.paint(c.ring_mask(x, y, 30 / c.mm, 31.6 / c.mm), ink)
        for a in (-60, 0, 60):
            ar = math.radians(a)
            c.paint(c.line_mask(x + math.sin(ar) * 34 / c.mm, y - math.cos(ar) * 34 / c.mm, x + math.sin(ar) * 40 / c.mm, y - math.cos(ar) * 40 / c.mm, 1.8), ink)
    if "lcd" in P:
        u0, v0, u1, v1 = P["lcd"]
        x0, y0, x1, y1 = u0 * c.w, (1 - v1) * c.h, u1 * c.w, (1 - v0) * c.h
        c.paint(c.rect_mask(x0 - 6, y0 - 6, x1 + 6, y1 + 6), (0.16, 0.16, 0.16), rough=0.5, height=-0.4)
        c.paint(c.rect_mask(x0, y0, x1, y1), (0.52, 0.56, 0.45), rough=0.75, height=-0.6)
        # ghost segments of a reflective LCD (unlit, unpowered dim), two rows of bars, no legible digits
        rng = np.random.default_rng(8)
        for row in range(2):
            for k in range(8):
                bx = x0 + (x1 - x0) * (0.06 + 0.115 * k)
                by = y0 + (y1 - y0) * (0.22 + 0.45 * row)
                on = rng.random() < 0.55
                col = (0.18, 0.20, 0.16) if on else (0.47, 0.51, 0.41)
                c.paint(c.rect_mask(bx, by, bx + (x1 - x0) * 0.08, by + (y1 - y0) * 0.07), col)
                c.paint(c.rect_mask(bx, by + (y1 - y0) * 0.17, bx + (x1 - x0) * 0.08, by + (y1 - y0) * 0.24), col if on else (0.47, 0.51, 0.41))
    if "breakers" in P:
        bw, bh = P["bsize"]
        c.paint(c.rect_mask(c.w * .03, c.h * .07, c.w * .97, c.h * .22), (0.90, 0.89, 0.85))      # the blank directory strip
        for k in range(1, 12):
            xk = c.w * (.03 + .94 * k / 12)
            c.paint(c.rect_mask(xk - 1, c.h * .07, xk + 1, c.h * .22), (0.55, 0.55, 0.52))
        for s in P["breakers"]:
            x, y = _site_px(c, s)
            c.paint(c.rect_mask(x - bw * 500 / c.mm, y - bh * 500 / c.mm, x + bw * 500 / c.mm, y + bh * 500 / c.mm), (0.06, 0.06, 0.06), rough=0.4, height=-0.5, cavity=0.4)
        c.paint(c.frame_mask(c.w * .02, c.h * .03, c.w * .98, c.h * .97, 3), (0.35, 0.35, 0.34), height=0.3)
    if name == "PANEL_CPU":
        c.paint(c.rect_mask(0, c.h * .88, c.w, c.h * .93), (0.14, 0.28, 0.55))
        c.paint(c.rect_mask(c.w * .22, c.h * .2, c.w * .222, c.h * .8), ink)
        c.paint(c.rect_mask(c.w * .755, c.h * .2, c.w * .757, c.h * .8), ink)
    if name == "PANEL_TAPE":
        c.paint(c.rect_mask(0, c.h * .9, c.w, c.h * .95), (0.14, 0.28, 0.55))
    if name == "PANEL_CRAC":
        c.paint(c.rect_mask(0, c.h * .93, c.w, c.h * .97), (0.25, 0.45, 0.62))
    return c


def cell_plate(name, w, h):
    """Engraved lamicoid plates without text: EPO red with a white core border and blank band; halon yellow with black."""
    if name == "PLATE_EPO":
        base, core = (0.62, 0.08, 0.07), (0.92, 0.90, 0.86)
    else:
        base, core = (0.86, 0.70, 0.12), (0.08, 0.08, 0.08)
    c = Cell(w, h, 300.0 / w, base=base, rough=0.55)
    c.paint(c.frame_mask(c.w * .04, c.h * .08, c.w * .96, c.h * .92, 4), core, height=-0.15)
    c.paint(c.frame_mask(c.w * .14, c.h * .22, c.w * .86, c.h * .40, 2.5), core, height=-0.15)
    for k in range(4):
        x = c.w * (.09 if k < 2 else .91)
        y = c.h * (.18 if k % 2 == 0 else .82)
        c.paint(c.disc_mask(x, y, 6), (0.55, 0.55, 0.53), rough=0.5, height=0.3)
        c.paint(c.line_mask(x - 4, y, x + 4, y, 1.6), (0.25, 0.25, 0.25))
    c.grain(0.02, 1.5, 23)
    return c


def cell_colours(w, h):
    c = Cell(w, h, 1.0, base=(0.5, 0.5, 0.5), rough=0.5)
    q = c.w // 8
    for i, (_n, col, rough) in enumerate(COLOURS):
        x0, y0 = (i % 8) * q, (i // 8) * q
        c.A[y0:y0 + q, x0:x0 + q] = col
        c.R[y0:y0 + q, x0:x0 + q] = rough
    c.grain(0.03, 1.6, 29)
    return c


def cell_tape(w, h, back=False):
    """1/2 in tape ribbon, u along the length: dark brown oxide with lengthwise streaks; the back a semi-gloss black."""
    c = Cell(w, h, 12.7 / h, base=(0.05, 0.05, 0.05) if back else (0.17, 0.12, 0.09), rough=0.5 if back else 0.55)
    rng = np.random.default_rng(37 if back else 36)
    streak = np.convolve(rng.standard_normal(c.h), np.ones(3) / 3, "same")
    c.A *= (1 + 0.06 * streak[:, None, None])
    c.R += 0.05 * streak[:, None]
    return c


def cell_disk_face(w, h):
    """Two drive faceplates (each 0.56 x 0.14 m) seen through the Disk cabinet's window: textured dark grey, vent slots on
    the left, a handle recess (the handle is geometry), a lamp cluster plate on the right, a thin colour stripe."""
    c = Cell(w, h, 560.0 / w, base=(0.16, 0.165, 0.17), rough=0.4)
    c.grain(0.05, 1.7, 43, 0.03)
    for k in range(2):
        y0 = k * c.h / 2
        c.paint(c.frame_mask(2, y0 + 2, c.w - 2, y0 + c.h / 2 - 2, 3), (0.07, 0.07, 0.07), height=-0.3, cavity=0.5)
        for s in range(9):
            x = c.w * (.05 + .022 * s)
            c.paint(c.rect_mask(x, y0 + c.h * .1, x + c.w * .010, y0 + c.h * .4), (0.02, 0.02, 0.02), rough=0.2, height=-0.8, cavity=0.2)
        c.paint(c.rect_mask(c.w * .30, y0 + c.h * .17, c.w * .62, y0 + c.h * .33), (0.08, 0.08, 0.08), height=-0.6, cavity=0.4)
        c.paint(c.rect_mask(c.w * .70, y0 + c.h * .1, c.w * .95, y0 + c.h * .4), (0.10, 0.10, 0.105), height=0.2)
        c.paint(c.rect_mask(c.w * .30, y0 + c.h * .42, c.w * .95, y0 + c.h * .44), (0.14, 0.28, 0.55))
    return c


# ============================================================================ Prop_CompIndicator
# 4 x 4 cells of 64 px (256 atlas). Lit: 0 green, 1 amber, 2 red, 3 warm white LED lenses; 4 blue LED; 5-8 green / amber /
# red / white square push-button lenses; 9 a VFD status window (green bars); 10 red digits (segments, no number legible);
# 11 a pilot jewel (warm white). Unlit (no emission): 12 green, 13 amber, 14 red, 15 clear. Kept in step with
# _struct_l0_computer.IND.
IND_COLS = {0: (0.25, 1.0, 0.35), 1: (1.0, 0.62, 0.12), 2: (1.0, 0.16, 0.10), 3: (1.0, 0.86, 0.62), 4: (0.25, 0.45, 1.0),
            5: (0.30, 1.0, 0.40), 6: (1.0, 0.70, 0.20), 7: (1.0, 0.22, 0.14), 8: (1.0, 0.94, 0.80), 9: (0.35, 1.0, 0.75),
            10: (1.0, 0.18, 0.10), 11: (1.0, 0.85, 0.60), 12: (0.25, 1.0, 0.35), 13: (1.0, 0.62, 0.12), 14: (1.0, 0.16, 0.10),
            15: (0.9, 0.9, 0.9)}


def indicator(size=256):
    q = size // 4
    S2 = 2 * size
    A = np.zeros((S2, S2, 3), np.float32)
    E = np.zeros((S2, S2, 3), np.float32)
    y, x = np.mgrid[0:S2, 0:S2].astype(np.float32)
    for i in range(16):
        x0, y0 = (i % 4) * 2 * q, (i // 4) * 2 * q
        cx, cy = x0 + q, y0 + q
        col = np.array(IND_COLS[i], np.float32)
        lit = i < 12
        u = (x - cx) / q
        v = (y - cy) / q
        cell = (x >= x0) & (x < x0 + 2 * q) & (y >= y0) & (y < y0 + 2 * q)
        if i in (5, 6, 7, 8):
            d = np.maximum(np.abs(u), np.abs(v))
            body = smooth(0.98, 0.9, d)
            glow = smooth(1.0, 0.0, np.hypot(u, v)) ** 0.7
        elif i == 9:
            body = np.ones_like(u)
            bars = ((np.mod((u + 1) * 6, 1.0) < 0.62) & (np.abs(v - 0.25) < 0.18)) | ((np.mod((u + 1) * 6, 1.0) < 0.62) & (np.abs(v + 0.3) < 0.14) & (u < 0.3))
            glow = bars.astype(np.float32) * 0.9 + 0.04
        elif i == 10:
            body = np.ones_like(u)
            segs = np.zeros_like(u)
            for ox in (-0.45, 0.45):
                uu = (u - ox) / 0.36
                segs = np.maximum(segs, ((np.abs(v - 0.0) < 0.07) & (np.abs(uu) < 0.7)).astype(np.float32))
                segs = np.maximum(segs, ((np.abs(np.abs(v) - 0.66) < 0.07) & (np.abs(uu) < 0.7)).astype(np.float32) * 0.15)
                segs = np.maximum(segs, ((np.abs(np.abs(uu) - 0.82) < 0.12) & (np.abs(v) < 0.62)).astype(np.float32) * 0.15)
            glow = segs
        else:
            d = np.hypot(u, v)
            body = smooth(0.98, 0.88, d)
            glow = smooth(0.95, 0.0, d) ** 0.6
        dark = col * 0.18 + 0.02
        if i == 15:
            dark = np.array((0.55, 0.55, 0.53), np.float32)
        base = np.where(cell[..., None], dark * body[..., None] + np.array((0.03, 0.03, 0.03)) * (1 - body[..., None]), A)
        A = base.astype(np.float32)
        if lit:
            hot = np.clip(glow * body, 0, 1)
            A = np.where(cell[..., None], A + (col * 0.55 - dark) * hot[..., None] * 0.6, A)
            E = np.where(cell[..., None], col * hot[..., None], E)
    return down2(A), down2(E)


# ============================================================================ Prop_PerfSteel
def perf_steel(size=512, tile_mm=96.0):
    """Perforated painted steel, 4.8 mm holes on an 8 mm pitch with alternate rows offset 4 mm (28 % open), 12 x 12 a tile;
    the paint the colour of Prop_SteelBeige; holes dark (the cabinet's inside); a soft dished rim."""
    S2 = 2 * size
    mm = tile_mm / S2
    y, x = np.mgrid[0:S2, 0:S2].astype(np.float32)
    X, Y = (x + .5) * mm, (y + .5) * mm
    row = np.floor(Y / 8.0)
    xx = X + (row % 2) * 4.0
    gx = np.mod(xx, 8.0) - 4.0
    gy = np.mod(Y, 8.0) - 4.0
    d = np.hypot(gx, gy)
    hole = smooth(2.5, 2.3, d)
    rim = smooth(3.1, 2.4, d) * (1 - hole)
    paint = np.array((0.80, 0.765, 0.665), np.float32)
    n = spectral(S2, S2, 1.7, 77) - .5
    A = paint * (1 + 0.035 * n[..., None])
    A = A * (1 - hole[..., None]) + np.array((0.03, 0.03, 0.03), np.float32) * hole[..., None]
    A = A * (1 - 0.10 * rim[..., None])
    H = -0.25 * rim - 0.6 * hole
    R = np.clip(0.42 + 0.03 * n, 0.3, 0.6) * (1 - hole) + 0.2 * hole
    C = 1 - 0.85 * hole - 0.2 * rim
    return down2(A), down2(H), down2(R), down2(C), mm * 2


# ============================================================================ assembly
def normal_from_height(Hmm, mm_per_px, strength=1.0):
    gy_img, gx = np.gradient(Hmm / mm_per_px)
    n = np.stack([-gx * strength, gy_img * strength, np.ones_like(gx)], -1)
    return n / np.linalg.norm(n, axis=-1, keepdims=True)


def build_atlas():
    A = np.zeros((SIZE, SIZE, 3), np.float32) + np.array((0.45, 0.45, 0.45), np.float32)
    Hpx = np.zeros((SIZE, SIZE), np.float32)      # height in atlas pixels (each cell divides by its own mm/px)
    R = np.full((SIZE, SIZE), 0.4, np.float32)
    C = np.ones((SIZE, SIZE), np.float32)
    makers = {
        "PACK": cell_pack, "CHART": cell_chart, "GREENBAR": cell_greenbar, "GREENBAR_EDGE": cell_greenbar_edge,
        "CARTON": cell_carton, "HALON_LABEL": cell_halon_label, "PERF_PANEL": cell_perf_panel,
        "METER_V": lambda w, h: cell_meter(w, h, True, 0.47), "METER_A": lambda w, h: cell_meter(w, h, True, 0.31),
        "METER_K": lambda w, h: cell_meter(w, h, False, 0.38), "REEL_LABEL": cell_reel_label, "CART_LABEL": cell_cart_labels,
        "PLATE_EPO": lambda w, h: cell_plate("PLATE_EPO", w, h), "PLATE_HALON": lambda w, h: cell_plate("PLATE_HALON", w, h),
        "COLOURS": cell_colours, "TAPE": cell_tape, "TAPE_BACK": lambda w, h: cell_tape(w, h, True), "DISK_FACE": cell_disk_face,
    }
    for k in PANELS:
        makers[k] = (lambda name: (lambda w, h: cell_panel(name, w, h)))(k)
    for name, (x, y, w, h) in CELLS.items():
        c = makers[name](w, h)
        A[y:y + h, x:x + w] = down2(c.A)
        Hpx[y:y + h, x:x + w] = down2(c.H) / (2 * c.mm)
        R[y:y + h, x:x + w] = down2(np.clip(c.R, 0.18, 0.95))
        C[y:y + h, x:x + w] = down2(c.C)
        print("[g3a1] cell %-15s %4d x %-4d  %.3f mm/px" % (name, w, h, 2 * c.mm))
    return A, Hpx, R, C


def save_set(prefix, A, Npx, R, C, sizes):
    """A at sizes[0], N and S at sizes[1] (box-filtered halvings)."""
    a = A
    while a.shape[0] > sizes[0]:
        a = down2(a)
    save(prefix + "_A", a, "RGB")
    n = Npx
    r, cc = R, C
    while n.shape[0] > sizes[1]:
        n, r, cc = down2(n), down2(r), down2(cc)
    n = n / np.linalg.norm(n, axis=-1, keepdims=True)
    save(prefix + "_N", n * .5 + .5, "RGB")
    s = np.stack([r, cc, np.zeros_like(r), np.ones_like(r)], -1)
    save(prefix + "_S", s, "RGBA")


def main():
    A, Hpx, R, C = build_atlas()
    gy_img, gx = np.gradient(Hpx)
    N = np.stack([-gx, gy_img, np.ones_like(gx)], -1)
    N /= np.linalg.norm(N, axis=-1, keepdims=True)
    save_set("Prop_CompAtlas", A, N, R, C, (2048, 1024))
    save_set("Prop_CompAtlas_Phone", A, N, R, C, (1024, 512))
    IA, IE = indicator(256)
    save("Prop_CompIndicator_A", IA, "RGB")
    save("Prop_CompIndicator_E", IE, "RGB")
    save("Prop_CompIndicator_Phone_A", down2(IA), "RGB")
    save("Prop_CompIndicator_Phone_E", down2(IE), "RGB")
    pa, ph, pr, pc, mm = perf_steel(512)
    gy_img, gx = np.gradient(ph / mm)
    pn = np.stack([-gx * 0.8, gy_img * 0.8, np.ones_like(gx)], -1)
    pn /= np.linalg.norm(pn, axis=-1, keepdims=True)
    save_set("Prop_PerfSteel", pa, pn, pr, pc, (512, 512))
    save_set("Prop_PerfSteel_Phone", pa, pn, pr, pc, (256, 256))
    print("[g3a1] wrote maps to %s" % OUT)


if __name__ == "__main__":
    main()
