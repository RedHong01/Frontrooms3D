"""FRONTROOMSSS structures surfaces of group G3-a2 (structures_assets step 2, line visual (game visual), 2026-11-10; lands as
Tools/lookdev/structures/gen_structures_g3a2.py with P16 structures_l0_props): the SP3 training room and file mezzanine of
1988-91.

Run: venv/bin/python gen_structures_g3a2.py <out dir>
House style of gen_structures_g3a1.py (numpy + Pillow, OpenGL +Y normals; _A sRGB albedo, _N linear normal, _S linear mask
R smoothness G cavity B metal A 1, _E emission sRGB-encoded linear). Every cell is drawn at twice its size and box-filtered
down. Smoothness never under 0.18; normals broad and low (the 11-10 09:52 note: no high-frequency normal noise).
  Prop_PaperAtlas_{A 2048, N 1024, S 1024}  decal atlas of paper and print (kits map a face into one cell, uv_rect from
      CELLS below, kept in step with Tools/Blender/frontrooms_kit/assets/_struct_l0.py): two 800 mm strips of letter end-tab
      folder fronts at 2.56 px/mm (colour-coded labels in bands, terminal-digit order in A, alphabetic in B) and their top
      edges, the folder side, out-guides, the records carton's end and side print, the marker board written / clean, the
      chalkboard, a flip-chart page, a handout, a binder spine, a name tent, a lateral-file label card, hanging-folder tabs,
      a VHS deck face, a power strip face, the fiche envelope and 16 colour cells.
  Prop_PaperAtlas_Phone_{A 1024, N 512, S 512}.
  Prop_ProjectionLit_{A, E} 1024  the overhead's square of light on the screen (keystoned, a transparency's chart), the lit
      stage with that transparency on the Fresnel glass, and a microfiche page on the reader's screen; Prop_ProjectionLit_Off
      shares _A with no emission (the screen fabric, the dark stage, the dark fiche screen).
  Prop_ProjectionLit_Phone_{A, E} 512.
  Prop_LaminateMatte_{A, N, S} 512, tiling 0.30 m: a solid-colour matte plastic laminate (fine suede stipple, faint wipe
      marks), neutral: Prop_LaminateFawn and Prop_LaminateDove tint it by _BaseColor; _Phone 256.
Period basis: 30_plan.md P16, 62_furniture_l0_office.md 2.4 / 2.9 / 3.1 (LS57, LS59, LS91, LS93, LS94, LS97-LS99) and
60_build.md G3-a2 (R1-R6). No text, brand or logo: type reads as dashes, labels as colour blocks, marker writing as
diagrams and wavy strokes without letters, the box maker's stamp as rings without words. ASCII only.
"""
import math
import os
import sys

import numpy as np
from PIL import Image

OUT = sys.argv[1] if len(sys.argv) > 1 else "."
os.makedirs(OUT, exist_ok=True)
SIZE = 2048

# (x, y, w, h) in pixels of the 2048 atlas, y from the top. Kept in step with _struct_l0.CELLS.
CELLS = {
    "FILES_A": (0, 0, 2048, 620), "FILES_B": (0, 620, 2048, 620),
    "FILES_TOP_A": (0, 1240, 1024, 48), "FILES_TOP_B": (1024, 1240, 1024, 48),
    "BOARD_WRITTEN": (0, 1288, 1024, 512), "FLIP_WRITTEN": (1024, 1288, 384, 480),
    "CARTON_END": (1408, 1288, 320, 272), "CARTON_SIDE": (1728, 1288, 320, 220),
    "VCR_FACE": (1728, 1508, 320, 72), "COLOURS": (1408, 1580, 512, 128), "STRIP_FACE": (1920, 1580, 128, 40),
    "READER_PLATE": (1920, 1620, 128, 64),
    "FICHE_ENV": (1408, 1708, 136, 92), "HANG_TABS": (1544, 1708, 256, 56), "LABEL_CARD": (1544, 1764, 128, 36),
    "TENT_CARD": (1672, 1764, 128, 36),
    "BOARD_CHALK": (0, 1800, 512, 248), "BOARD_CLEAN": (512, 1800, 256, 128), "BOARD_TRAY": (512, 1928, 256, 32),
    "HANDOUT": (768, 1800, 192, 248), "BINDER_SPINE": (960, 1800, 64, 248), "FILE_SIDE": (1024, 1800, 160, 248),
    "OUTGUIDE_RED": (1184, 1800, 96, 248), "OUTGUIDE_BLUE": (1280, 1800, 96, 248), "TRANSPARENCY": (1376, 1800, 248, 248),
    "MANILA": (1624, 1800, 128, 248), "KRAFT": (1752, 1800, 128, 248), "PRESS_RED": (1880, 1800, 84, 248),
    "PRESS_GREEN": (1964, 1800, 84, 248),
}
# Colour cells (index -> name, sRGB, smoothness): 8 x 2 cells of 64 px in COLOURS.
COLOURS = [
    ("manila", (0.86, 0.77, 0.56), 0.28), ("kraft", (0.62, 0.47, 0.30), 0.26), ("hang_green", (0.30, 0.45, 0.26), 0.32),
    ("press_red", (0.58, 0.16, 0.12), 0.30), ("press_blue", (0.17, 0.27, 0.48), 0.30), ("vinyl_navy", (0.10, 0.13, 0.24), 0.45),
    ("vinyl_teal", (0.12, 0.33, 0.35), 0.45), ("paper", (0.90, 0.89, 0.85), 0.24),
    ("marker_blue", (0.10, 0.20, 0.55), 0.45), ("marker_black", (0.05, 0.05, 0.06), 0.45), ("marker_red", (0.66, 0.10, 0.09), 0.45),
    ("felt_grey", (0.30, 0.30, 0.30), 0.20), ("chalk", (0.86, 0.86, 0.82), 0.20), ("vcr_black", (0.06, 0.06, 0.065), 0.50),
    ("vcr_silver", (0.55, 0.55, 0.55), 0.55), ("pencil_yellow", (0.86, 0.66, 0.12), 0.50),
]
# Label colours of the colour-coded end-tab system (generic palette; 10 numeric + year bands).
LABEL = [(0.74, 0.12, 0.10), (0.87, 0.43, 0.10), (0.93, 0.79, 0.17), (0.52, 0.73, 0.29), (0.09, 0.45, 0.26),
         (0.36, 0.63, 0.86), (0.12, 0.22, 0.56), (0.44, 0.23, 0.56), (0.89, 0.51, 0.63), (0.46, 0.29, 0.15)]
YEAR = {88: (0.55, 0.32, 0.62), 89: (0.95, 0.83, 0.20), 90: (0.30, 0.66, 0.42)}


# ============================================================================ folder runs (kept in step with _struct_l0)
def lcg(seed):
    s = [int(seed) % 2147483648]

    def rnd():
        s[0] = (s[0] * 1103515245 + 12345) % 2147483648
        return s[0] / 2147483648.0
    return rnd


def folder_layout(seed, length_mm=800.0):
    """A deterministic run of letter end-tab folders along a strip: (x0, x1, kind) in mm from the strip's left edge as
    seen from the aisle; kind 0 manila, 1 kraft, 2 red pressboard, 3 green pressboard. Kept in step with
    Tools/Blender/frontrooms_kit/assets/_struct_l0.folder_layout()."""
    rnd = lcg(seed)
    x, out = 0.0, []
    while x < length_mm - 0.5:
        r = rnd()
        if r < 0.10:
            t = 2.5 + 2.0 * rnd()
        elif r < 0.82:
            t = 6.0 + 9.0 * rnd()
        else:
            t = 15.0 + 13.0 * rnd()
        q = rnd()
        k = 2 if q < 0.05 else 3 if q < 0.08 else (1 if (seed % 2 == 0 and q < 0.30) else 0)
        x1 = min(length_mm, x + t)
        out.append((x, x1, k))
        x = x1
    return out


SEED_A, SEED_B = 4021, 7716


# ============================================================================ helpers (gen_structures_g3a1 house style)
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
    """One cell drawn at twice its atlas size: A sRGB albedo, H height (mm), R smoothness, C cavity, E emission; mm = mm per
    (2x) pixel."""

    def __init__(self, w, h, mm, base=(0.5, 0.5, 0.5), rough=0.4):
        self.w, self.h, self.mm = 2 * w, 2 * h, mm / 2.0
        self.A = np.ones((self.h, self.w, 3), np.float32) * np.array(base, np.float32)
        self.H = np.zeros((self.h, self.w), np.float32)
        self.R = np.full((self.h, self.w), rough, np.float32)
        self.C = np.ones((self.h, self.w), np.float32)
        self.E = np.zeros((self.h, self.w, 3), np.float32)
        self.y, self.x = np.mgrid[0:self.h, 0:self.w].astype(np.float32)

    def paint(self, mask, col, rough=None, height=None, cavity=None, alpha=1.0):
        m = mask[..., None] * alpha
        self.A = self.A * (1 - m) + np.array(col, np.float32) * m
        if rough is not None:
            self.R = self.R * (1 - mask * alpha) + rough * mask * alpha
        if height is not None:
            self.H = self.H * (1 - mask * alpha) + height * mask * alpha
        if cavity is not None:
            self.C = self.C * (1 - mask * alpha) + cavity * mask * alpha

    def rect_mask(self, x0, y0, x1, y1, soft=1.0):
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

    def poly_mask(self, pts, width, soft=0.8):
        m = np.zeros((self.h, self.w), np.float32)
        for (a, b) in zip(pts, pts[1:]):
            m = np.maximum(m, self.line_mask(a[0], a[1], b[0], b[1], width, soft))
        return m

    def grain(self, amount, beta=1.6, seed=0, rough_amount=0.0):
        n = spectral(self.h, self.w, beta, seed) - .5
        self.A = np.clip(self.A * (1 + amount * n[..., None]), 0, 1)
        if rough_amount:
            self.R = np.clip(self.R + rough_amount * n, 0.18, 0.95)


def stroke_path(rng, x0, y0, length, amp, step, slope=0.0):
    """A wavy 'handwriting' stroke without letters: a polyline drifting right with a damped random wiggle."""
    pts, x, y, v = [], x0, y0, 0.0
    while x < x0 + length:
        v = 0.6 * v + rng.uniform(-1, 1) * amp
        pts.append((x, y + v))
        x += step * rng.uniform(0.6, 1.3)
        y += slope * step
    return pts


# ============================================================================ folder fronts (the hero) and their top edges
FOLDER_H = 241.0     # letter end-tab folder, 9 1/2 in (R6)
STRIP_MM = 800.0


def kind_colour(kind, rnd):
    base = {0: (0.86, 0.77, 0.56), 1: (0.62, 0.47, 0.30), 2: (0.58, 0.16, 0.12), 3: (0.20, 0.42, 0.26)}[kind]
    v = 1.0 + (rnd.uniform(-0.05, 0.04) if kind < 2 else rnd.uniform(-0.03, 0.03))
    return tuple(min(1.0, b * v) for b in base)


SECTION_MM = 200.0      # a shelf section: one primary pair (terminal digits) or one first letter, then a new section
PRIMARY = [(2, 7), (5, 1), (0, 9), (8, 3)]
FIRST = [16, 4, 21, 9]


def label_bands(scheme, i, x0, rng):
    """The label colours of folder i (left edge x0 mm) on a strip: a list of (y0_mm, y1_mm, colour, white_stripe). The strip
    holds four sections of 200 mm, so a shelf mapped from any start shows two or three different primaries."""
    out = []
    sec = min(3, int(x0 / SECTION_MM))
    j = i - FIRST_INDEX[scheme][sec]
    year = YEAR[(88, 89, 90)[int(rng.integers(0, 3)) if rng.random() < 0.5 else 1]]
    out.append((18.0, 40.0, year, True))
    if scheme == "A":
        # terminal-digit order: the primary pair is constant in the section, the secondary pair counts up folder by folder
        t1, t2 = PRIMARY[sec]
        m = 31 + 17 * sec + j
        out += [(62.0, 85.0, LABEL[t1], False), (87.0, 110.0, LABEL[t2], False),
                (131.0, 154.0, LABEL[(m // 10) % 10], False), (156.0, 179.0, LABEL[m % 10], False)]
    else:
        # alphabetic: the first letter constant in the section, the second steps every few folders; N-Z carry a white stripe
        a1 = FIRST[sec]
        a2 = 2 + (j * 11) // 18
        out += [(66.0, 106.0, LABEL[a1 % 10], a1 >= 13), (110.0, 150.0, LABEL[a2 % 10], a2 >= 13)]
    return out


def _first_index(seed):
    lay = folder_layout(seed, STRIP_MM)
    out = []
    for s in range(4):
        out.append(next(i for i, (x0, _x1, _k) in enumerate(lay) if x0 >= s * SECTION_MM - 1e-6) if s else 0)
    return out


FIRST_INDEX = {"A": _first_index(SEED_A), "B": _first_index(SEED_B)}


def cell_files(w, h, seed, scheme):
    c = Cell(w, h, STRIP_MM / w, base=(0.10, 0.085, 0.07), rough=0.2)
    rng = np.random.default_rng(seed)
    lay = folder_layout(seed, STRIP_MM)
    n = len(lay)
    ymm = (c.h - c.y - 0.5) * c.mm            # mm from the strip's bottom
    c.H[:] = -1.6
    c.C[:] = 0.4
    for i, (x0, x1, kind) in enumerate(lay):
        t = x1 - x0
        p0, p1 = int(x0 / c.mm), min(c.w, int(math.ceil(x1 / c.mm)))
        if p1 <= p0:
            continue
        sl = slice(p0, p1)
        xm = (c.x[:, sl] + 0.5) * c.mm - x0       # mm from this folder's left edge
        yy = ymm[:, sl]
        A, H, R, C = c.A[:, sl], c.H[:, sl], c.R[:, sl], c.C[:, sl]
        col = np.array(kind_colour(kind, rng), np.float32)
        top = FOLDER_H - rng.uniform(0.0, 1.2)
        inside = smooth(-0.3, 0.3, top - yy) * smooth(-0.2, 0.2, yy - 0.4)
        seam = 0.45 if t > 3 else 0.25
        tabw = min(t - seam, max(4.2, 0.72 * t))
        tab = smooth(seam - 0.2, seam + 0.2, xm) * smooth(-0.25, 0.25, seam + tabw - xm) * inside
        inner = smooth(-0.25, 0.25, xm - (seam + tabw)) * smooth(-0.2, 0.2, t - xm) * inside
        # the tab face: the folder board, a bright edge line on its left, a soft shade toward the right, finger grime
        shade = 1.0 - 0.10 * np.clip((xm - seam) / max(tabw, 1e-3), 0, 1)
        edge = smooth(seam + 0.9, seam + 0.2, xm) * tab
        grime = 1.0 - 0.07 * smooth(95.0, 120.0, yy) * smooth(185.0, 150.0, yy)
        tc = col[None, None, :] * (shade * grime)[..., None] + 0.08 * edge[..., None]
        A[:] = A * (1 - tab[..., None]) + tc * tab[..., None]
        H[:] = H * (1 - tab) + 0.45 * tab
        R[:] = R * (1 - tab) + (0.27 if kind < 2 else 0.31) * tab
        C[:] = C * (1 - tab) + 1.0 * tab
        # the inside: paper edges (a stack of sheets) below the papers' top, the folder's own front panel edge on the right
        if t > 6.0:
            ptop = top - rng.uniform(14.0, 26.0)
            paper = inner * smooth(-0.3, 0.3, ptop - yy)
            lines = 0.92 + 0.08 * np.sin((xm * 3.1 + rng.uniform(0, 6)) * 2.0)
            pc = np.array((0.80, 0.79, 0.75), np.float32) * 0.78
            pcol = pc[None, None, :] * lines[..., None]
            # multi-part forms: a few coloured copies (pink, canary, green, blue) among the white sheets
            for _k in range(int(rng.integers(0, 3)) if t - 1.2 > seam + tabw + 0.9 else 0):
                cx = rng.uniform(seam + tabw + 0.8, t - 1.2)
                sheet = smooth(0.55, 0.15, np.abs(xm - cx))
                tint = np.array(((0.86, 0.66, 0.68), (0.90, 0.84, 0.52), (0.66, 0.80, 0.62), (0.64, 0.74, 0.86))[int(rng.integers(0, 4))], np.float32) * 0.80
                pcol = pcol * (1 - sheet[..., None]) + tint[None, None, :] * sheet[..., None]
            A[:] = A * (1 - paper[..., None]) + pcol * paper[..., None]
            H[:] = H * (1 - paper) + (-0.95) * paper
            R[:] = R * (1 - paper) + 0.22 * paper
            C[:] = C * (1 - paper) + 0.72 * paper
            above = inner * smooth(-0.3, 0.3, yy - ptop)
            A[:] = A * (1 - above[..., None]) + (col * 0.38)[None, None, :] * above[..., None]
            H[:] = H * (1 - above) + (-1.3) * above
            C[:] = C * (1 - above) + 0.5 * above
            panel = smooth(t - 1.4, t - 0.7, xm) * smooth(-0.2, 0.2, t - xm) * inside
            A[:] = A * (1 - panel[..., None]) + (col * 0.80)[None, None, :] * panel[..., None]
            H[:] = H * (1 - panel) + 0.1 * panel
        elif t > seam + tabw + 0.2:
            A[:] = A * (1 - inner[..., None]) + (col * 0.55)[None, None, :] * inner[..., None]
            H[:] = H * (1 - inner) + (-0.6) * inner
        # the labels on the tab (wrap-around vinyl, laminated: higher smoothness, a hair proud)
        if rng.random() > 0.05:
            for (y0, y1, lc, stripe) in label_bands(scheme, i, x0, rng):
                jit = rng.normal(0.0, 0.9)
                y0j, y1j = y0 + jit, y1 + jit
                lw = min(t - seam, tabw + 1.6)
                lm = (smooth(seam - 0.15, seam + 0.25, xm) * smooth(-0.25, 0.25, seam + lw - xm)
                      * smooth(-0.3, 0.3, yy - y0j) * smooth(-0.3, 0.3, y1j - yy)) * inside
                lcv = np.array(lc, np.float32)
                hi = smooth(y1j - 1.2, y1j - 0.3, yy)
                lcol = lcv[None, None, :] * (1.0 - 0.06 * np.clip((xm - seam) / max(lw, 1e-3), 0, 1))[..., None] + 0.10 * hi[..., None]
                if stripe:
                    sm = smooth(-0.3, 0.3, yy - (y0j + 0.36 * (y1j - y0j))) * smooth(-0.3, 0.3, (y0j + 0.62 * (y1j - y0j)) - yy)
                    lcol = lcol * (1 - sm[..., None]) + np.array((0.93, 0.93, 0.90), np.float32) * sm[..., None]
                A[:] = A * (1 - lm[..., None]) + lcol * lm[..., None]
                H[:] = H * (1 - lm) + 0.62 * lm
                R[:] = R * (1 - lm) + 0.60 * lm
            # the name label: white, typed along the tab (a vertical dash of type, no letters)
            y0n = 192.0 + rng.normal(0, 1.0)
            y1n = min(top - 2.0, 236.0 + rng.normal(0, 1.0))
            nm = (smooth(seam - 0.15, seam + 0.25, xm) * smooth(-0.25, 0.25, seam + min(t - seam, tabw + 1.2) - xm)
                  * smooth(-0.3, 0.3, yy - y0n) * smooth(-0.3, 0.3, y1n - yy)) * inside
            A[:] = A * (1 - nm[..., None]) + np.array((0.90, 0.90, 0.87), np.float32) * nm[..., None]
            H[:] = H * (1 - nm) + 0.55 * nm
            R[:] = R * (1 - nm) + 0.45 * nm
            if tabw > 3.0:
                L = rng.uniform(14.0, 34.0)
                cx = seam + 0.5 * min(t - seam, tabw + 1.2)
                ty = smooth(-0.25, 0.25, yy - (y1n - 4.0 - L)) * smooth(-0.25, 0.25, (y1n - 4.0) - yy)
                dash = smooth(0.75, 0.35, np.abs(xm - cx)) * ty * nm
                dash *= 0.55 + 0.45 * (np.sin(yy * 2.4) > -0.3)
                A[:] = A * (1 - 0.8 * dash[..., None]) + np.array((0.22, 0.22, 0.24), np.float32) * 0.8 * dash[..., None]
        # the shelf's contact shadow at the folder's foot
        foot = smooth(3.0, 0.4, yy)
        A[:] = A * (1 - 0.35 * foot[..., None])
    # broad normals: soften the height field (no high-frequency normal noise)
    k = 3
    Hs = c.H.copy()
    for _ in range(2):
        Hs = (np.roll(Hs, 1, 1) + Hs + np.roll(Hs, -1, 1)) / 3.0
        Hs = (np.roll(Hs, 1, 0) + Hs + np.roll(Hs, -1, 0)) / 3.0
    c.H = Hs * 0.6
    c.grain(0.05, 1.7, seed + 11, 0.02)
    return c


def cell_files_top(w, h, seed):
    """The folders' top edges seen from above (u along the run, same scale and folders as the front strip; v = depth)."""
    c = Cell(w, h, STRIP_MM / w, base=(0.10, 0.085, 0.07), rough=0.2)
    rng = np.random.default_rng(seed + 5)
    for (x0, x1, kind) in folder_layout(seed, STRIP_MM):
        t = x1 - x0
        p0, p1 = int(x0 / c.mm), min(c.w, int(math.ceil(x1 / c.mm)))
        if p1 <= p0:
            continue
        sl = slice(p0, p1)
        xm = (c.x[:, sl] + 0.5) * c.mm - x0
        col = np.array(kind_colour(kind, rng), np.float32)
        body = smooth(0.25, 0.6, xm) * smooth(-0.2, 0.2, t - 0.15 - xm)
        A = c.A[:, sl]
        A[:] = A * (1 - body[..., None]) + col[None, None, :] * body[..., None]
        if t > 4.0:
            pap = smooth(1.0, 1.5, xm) * smooth(-0.2, 0.2, t - 1.2 - xm)
            v = 0.86 + 0.06 * np.sin(xm * 5.0 + rng.uniform(0, 6))
            A[:] = A * (1 - pap[..., None]) + (np.array((0.80, 0.79, 0.75), np.float32) * 0.82 * v[..., None]) * pap[..., None]
        c.R[:, sl] = 0.25
        c.C[:, sl] = 0.9 * body + 0.4 * (1 - body)
    c.grain(0.05, 1.6, seed + 3)
    return c


def cell_board_texture(w, h, mm, base, rough, seed):
    c = Cell(w, h, mm, base=base, rough=rough)
    c.grain(0.035, 2.0, seed, 0.03)
    return c


def cell_file_side(w, h):
    """The outer face of a run of folders (a manila panel 311 mm deep x 241 mm tall), the end tab's line near the front."""
    c = Cell(w, h, 311.0 / w, base=(0.84, 0.75, 0.55), rough=0.27)
    c.grain(0.06, 1.6, 41, 0.02)
    X = (c.x + 0.5) * c.mm
    tabline = smooth(10.0, 11.0, X) * smooth(12.6, 11.6, X)
    c.A = c.A * (1 - 0.18 * tabline[..., None])
    c.H -= 0.3 * tabline
    shade = smooth(0.0, 120.0, X)
    c.A = c.A * (0.92 + 0.08 * (1 - shade))[..., None]
    return c


def cell_outguide(w, h, col):
    """An out-guide (vinyl, with a clear pocket for the charge-out card), seen on its side face."""
    c = Cell(w, h, 311.0 / w, base=col, rough=0.48)
    c.grain(0.04, 1.8, 77)
    p = c.frame_mask(c.w * 0.18, c.h * 0.22, c.w * 0.86, c.h * 0.80, 2.2)
    c.paint(p, tuple(min(1.0, x * 1.35 + 0.08) for x in col), rough=0.62, height=0.2)
    card = c.rect_mask(c.w * 0.22, c.h * 0.26, c.w * 0.82, c.h * 0.76)
    c.paint(card, (0.86, 0.84, 0.78), rough=0.4, alpha=0.65)
    return c


# ============================================================================ cartons (NARA cubic-foot records box, LS94)
INK = (0.16, 0.24, 0.20)


def cell_carton_end(w, h):
    """The box end (324 x 274 mm): kraft board, a printed form block (ruled fields, no words) beside the handhold zone, which is
    left plain (the handhold is cut in the geometry), a printed rule along the top."""
    c = Cell(w, h, 324.0 / w, base=(0.64, 0.49, 0.32), rough=0.27)
    c.grain(0.10, 1.5, 61, 0.05)
    # flute shadow lines (the corrugation telegraphing through the liner), horizontal at 8 mm
    Y = (c.y + 0.5) * c.mm
    c.A = c.A * (1 - 0.025 * (0.5 + 0.5 * np.sin(Y / 8.0 * 2 * math.pi)))[..., None]
    x0, x1 = c.w * 0.10, c.w * 0.90
    y0, y1 = c.h * 0.44, c.h * 0.90
    c.paint(c.frame_mask(x0, y0, x1, y1, 3.0), INK, rough=0.3)
    for k in range(1, 4):
        yk = y0 + (y1 - y0) * k / 4.0
        c.paint(c.rect_mask(x0, yk - 1.2, x1, yk + 1.2), INK, rough=0.3)
    c.paint(c.rect_mask(x0 + (x1 - x0) * 0.62, y0, x0 + (x1 - x0) * 0.62 + 2.4, y0 + (y1 - y0) * 0.5), INK)
    c.paint(c.rect_mask(c.w * 0.06, c.h * 0.05, c.w * 0.94, c.h * 0.05 + 3.0), INK)
    return c


def cell_carton_side(w, h):
    """The long side (401 x 274 mm): kraft, a small ruled number block and the box maker's round stamp (rings, no words)."""
    c = Cell(w, h, 401.0 / w, base=(0.63, 0.48, 0.31), rough=0.27)
    c.grain(0.10, 1.5, 63, 0.05)
    Y = (c.y + 0.5) * c.mm
    c.A = c.A * (1 - 0.025 * (0.5 + 0.5 * np.sin(Y / 8.0 * 2 * math.pi)))[..., None]
    c.paint(c.frame_mask(c.w * 0.58, c.h * 0.16, c.w * 0.92, c.h * 0.44, 2.6), INK)
    c.paint(c.rect_mask(c.w * 0.58, c.h * 0.30 - 1.0, c.w * 0.92, c.h * 0.30 + 1.0), INK)
    cx, cy, r = c.w * 0.17, c.h * 0.76, c.h * 0.14
    c.paint(c.ring_mask(cx, cy, r - 2.4, r), INK, alpha=0.85)
    c.paint(c.ring_mask(cx, cy, r * 0.62 - 1.6, r * 0.62), INK, alpha=0.85)
    for k in range(10):
        a = 2 * math.pi * k / 10
        c.paint(c.line_mask(cx + math.cos(a) * r * 0.70, cy + math.sin(a) * r * 0.70, cx + math.cos(a) * r * 0.90,
                            cy + math.sin(a) * r * 0.90, 2.0), INK, alpha=0.7)
    return c


# ============================================================================ boards, pages, small print
def marker_diagram(c, rng, x0, y0, x1, y1, pens, scale):
    """Marker drawings without letters: a box chart with arrows, bullets with wavy strokes, a bar chart."""
    W, Hh = x1 - x0, y1 - y0
    pw = 4.2 * scale
    # title: a thick wavy stroke, underlined
    pts = stroke_path(rng, x0 + 0.05 * W, y0 + 0.08 * Hh, 0.34 * W, 3.0 * scale, 9 * scale)
    c.paint(c.poly_mask(pts, pw * 1.25), pens[0], rough=0.45)
    c.paint(c.line_mask(x0 + 0.05 * W, y0 + 0.13 * Hh, x0 + 0.40 * W, y0 + 0.135 * Hh, pw), pens[0], rough=0.45)
    # three boxes and arrows (left half)
    bx = [(0.06, 0.24), (0.24, 0.42), (0.06, 0.42)]
    for k, (a, b) in enumerate(((0.06, 0.20), (0.27, 0.41), (0.16, 0.30))):
        yb = (0.24, 0.24, 0.50)[k]
        bxa, bxb = x0 + a * W, x0 + b * W
        bya, byb = y0 + yb * Hh, y0 + (yb + 0.13) * Hh
        c.paint(c.frame_mask(bxa, bya, bxb, byb, pw * 0.9, 1.0), pens[k % len(pens)], rough=0.45)
        pts = stroke_path(rng, bxa + 0.12 * (bxb - bxa), (bya + byb) / 2, 0.7 * (bxb - bxa), 2.0 * scale, 7 * scale)
        c.paint(c.poly_mask(pts, pw * 0.8), pens[k % len(pens)], rough=0.45)
    c.paint(c.line_mask(x0 + 0.20 * W, y0 + 0.305 * Hh, x0 + 0.27 * W, y0 + 0.305 * Hh, pw), pens[0])
    c.paint(c.line_mask(x0 + 0.13 * W, y0 + 0.37 * Hh, x0 + 0.20 * W, y0 + 0.50 * Hh, pw), pens[0])
    c.paint(c.line_mask(x0 + 0.34 * W, y0 + 0.37 * Hh, x0 + 0.27 * W, y0 + 0.50 * Hh, pw), pens[0])
    for (ax, ay, dx, dy) in ((0.27, 0.305, -1, 0), (0.20, 0.50, -0.5, -1), (0.27, 0.50, 0.5, -1)):
        hx, hy = x0 + ax * W, y0 + ay * Hh
        L = 14 * scale
        for s in (-1, 1):
            c.paint(c.line_mask(hx, hy, hx + (dx * -0.8 + s * dy * 0.5) * L, hy + (dy * -0.8 - s * dx * 0.5) * L, pw), pens[0])
    # bullets with wavy strokes (right half)
    for k in range(5):
        by = y0 + (0.24 + 0.11 * k) * Hh
        c.paint(c.disc_mask(x0 + 0.53 * W, by, pw * 1.1), pens[1 % len(pens)])
        L = rng.uniform(0.18, 0.36) * W
        pts = stroke_path(rng, x0 + 0.56 * W, by, L, 2.4 * scale, 8 * scale)
        c.paint(c.poly_mask(pts, pw * 0.85), pens[1 % len(pens)], rough=0.45)
    # a small bar chart lower right, a circled item
    ax0, ay0 = x0 + 0.56 * W, y0 + 0.92 * Hh
    c.paint(c.line_mask(ax0, ay0, ax0 + 0.36 * W, ay0, pw), pens[0])
    c.paint(c.line_mask(ax0, ay0, ax0, ay0 - 0.28 * Hh, pw), pens[0])
    for k, v in enumerate((0.45, 0.70, 0.55, 0.90, 0.80)):
        bx0 = ax0 + (0.03 + 0.065 * k) * W
        c.paint(c.frame_mask(bx0, ay0 - v * 0.25 * Hh, bx0 + 0.04 * W, ay0, pw * 0.8, 1.0), pens[(k + 2) % len(pens)])
    c.paint(c.ring_mask(x0 + 0.53 * W, y0 + 0.35 * Hh, 0.05 * Hh, 0.05 * Hh + pw * 0.8), pens[2 % len(pens)])


def ghosting(c, seed, amount, col):
    """Erased marker or chalk: soft cloudy smears, mostly horizontal (the eraser's strokes)."""
    n = spectral(c.h, c.w, 2.6, seed)
    sm = np.clip((n - 0.55) * 3.0, 0, 1)
    rowwave = 0.5 + 0.5 * np.sin((c.y / c.h) * 2 * math.pi * 7 + spectral(c.h, 1, 1.0, seed + 1)[:, :1] * 6)
    m = sm * (0.6 + 0.4 * rowwave) * amount
    c.paint(np.clip(m, 0, 1).astype(np.float32), col)


def cell_board_written(w, h):
    c = Cell(w, h, 2440.0 / w, base=(0.89, 0.89, 0.87), rough=0.78)
    ghosting(c, 101, 0.10, (0.70, 0.72, 0.78))
    rng = np.random.default_rng(1990)
    pens = [(0.10, 0.20, 0.55), (0.05, 0.05, 0.06), (0.66, 0.10, 0.09), (0.10, 0.42, 0.20)]
    marker_diagram(c, rng, c.w * 0.03, c.h * 0.04, c.w * 0.97, c.h * 0.96, pens, c.w / 2048.0 * 1.6)
    c.grain(0.015, 2.0, 103, 0.02)
    return c


def cell_board_clean(w, h):
    c = Cell(w, h, 2440.0 / w, base=(0.89, 0.89, 0.87), rough=0.78)
    ghosting(c, 107, 0.08, (0.72, 0.74, 0.80))
    c.grain(0.012, 2.0, 109, 0.02)
    return c


def cell_board_chalk(w, h):
    c = Cell(w, h, 2440.0 / w, base=(0.165, 0.235, 0.205), rough=0.20)
    ghosting(c, 113, 0.30, (0.42, 0.48, 0.45))
    Y = c.y / c.h
    dust = smooth(0.80, 1.0, Y) * (0.25 + 0.5 * spectral(c.h, c.w, 1.5, 115))
    c.paint(np.clip(dust, 0, 1).astype(np.float32), (0.55, 0.58, 0.55), alpha=0.6)
    rng = np.random.default_rng(1988)
    for k in range(4):
        by = c.h * (0.18 + 0.15 * k)
        pts = stroke_path(rng, c.w * rng.uniform(0.05, 0.4), by, c.w * rng.uniform(0.2, 0.4), 3.0, 8.0)
        c.paint(c.poly_mask(pts, 3.0), (0.33, 0.40, 0.37), alpha=0.6)
    c.grain(0.05, 1.8, 117, 0.02)
    return c


def cell_board_tray(w, h):
    """The tray's floor seen from above: satin aluminium with marker and chalk dust along its length."""
    c = Cell(w, h, 2440.0 / w, base=(0.62, 0.62, 0.60), rough=0.55)
    n = spectral(c.h, c.w, 1.4, 119)
    c.paint(np.clip((n - 0.5) * 2.5, 0, 1).astype(np.float32), (0.35, 0.36, 0.40), alpha=0.5)
    return c


def cell_flip_written(w, h):
    c = Cell(w, h, 686.0 / w, base=(0.91, 0.90, 0.86), rough=0.24)
    c.grain(0.03, 1.8, 121, 0.02)
    rng = np.random.default_rng(1987)
    pens = [(0.05, 0.05, 0.06), (0.10, 0.20, 0.55), (0.66, 0.10, 0.09)]
    s = c.w / 384.0 * 1.4
    pts = stroke_path(rng, c.w * 0.10, c.h * 0.09, c.w * 0.6, 3.0 * s, 8 * s)
    c.paint(c.poly_mask(pts, 6.0 * s), pens[2])
    c.paint(c.line_mask(c.w * 0.10, c.h * 0.14, c.w * 0.74, c.h * 0.145, 5.0 * s), pens[2])
    for k in range(5):
        by = c.h * (0.24 + 0.09 * k)
        c.paint(c.disc_mask(c.w * 0.12, by, 4.5 * s), pens[k % 2])
        pts = stroke_path(rng, c.w * 0.18, by, c.w * rng.uniform(0.35, 0.65), 2.6 * s, 7 * s)
        c.paint(c.poly_mask(pts, 4.4 * s), pens[k % 2])
    cx, cy, r = c.w * 0.62, c.h * 0.80, c.w * 0.18
    c.paint(c.ring_mask(cx, cy, r - 4.0 * s, r), pens[1])
    for a in (0.3, 2.2, 4.1):
        c.paint(c.line_mask(cx, cy, cx + math.cos(a) * r, cy + math.sin(a) * r, 4.0 * s), pens[1])
    # the pad's perforation line and binding holes at the top edge
    c.paint(c.rect_mask(0, c.h * 0.012, c.w, c.h * 0.012 + 1.2), (0.70, 0.69, 0.66))
    return c


def cell_handout(w, h):
    c = Cell(w, h, 216.0 / w, base=(0.91, 0.90, 0.86), rough=0.24)
    c.grain(0.03, 1.8, 131, 0.02)
    rng = np.random.default_rng(131)
    ink = (0.25, 0.25, 0.27)
    c.paint(c.rect_mask(c.w * 0.12, c.h * 0.08, c.w * 0.62, c.h * 0.08 + 7), ink)
    y = c.h * 0.16
    while y < c.h * 0.92:
        if rng.random() < 0.13:
            y += 10
            continue
        x = c.w * 0.12
        xe = c.w * (0.88 - (0.3 if rng.random() < 0.15 else 0.0))
        while x < xe:
            L = rng.uniform(6, 22)
            c.paint(c.rect_mask(x, y, min(x + L, xe), y + 2.6), ink, alpha=0.75)
            x += L + rng.uniform(2.5, 4.0)
        y += 7.2
    c.paint(c.line_mask(c.w * 0.05, c.h * 0.045, c.w * 0.13, c.h * 0.03, 2.2), (0.55, 0.55, 0.57), rough=0.6)
    return c


def cell_binder_spine(w, h):
    c = Cell(w, h, 38.0 / w, base=(0.10, 0.13, 0.24), rough=0.45)
    c.grain(0.05, 1.8, 141)
    c.paint(c.rect_mask(c.w * 0.14, c.h * 0.10, c.w * 0.86, c.h * 0.62), (0.88, 0.87, 0.83), rough=0.62)
    c.paint(c.rect_mask(c.w * 0.14, c.h * 0.16, c.w * 0.86, c.h * 0.22), (0.12, 0.26, 0.50), rough=0.62)
    c.paint(c.frame_mask(c.w * 0.12, c.h * 0.085, c.w * 0.88, c.h * 0.635, 1.5), (0.30, 0.33, 0.42), rough=0.7)
    for yy in (0.72, 0.88):
        c.paint(c.disc_mask(c.w * 0.5, c.h * yy, c.w * 0.12), (0.55, 0.55, 0.56), rough=0.6)
    return c


def cell_tent_card(w, h):
    c = Cell(w, h, 200.0 / w, base=(0.91, 0.90, 0.86), rough=0.3)
    c.paint(c.frame_mask(c.w * 0.04, c.h * 0.10, c.w * 0.96, c.h * 0.90, 3.0), (0.12, 0.24, 0.52))
    rng = np.random.default_rng(151)
    pts = stroke_path(rng, c.w * 0.22, c.h * 0.52, c.w * 0.5, 4.0, 9.0)
    c.paint(c.poly_mask(pts, 5.0), (0.08, 0.08, 0.10))
    return c


def cell_label_card(w, h):
    c = Cell(w, h, 76.0 / w, base=(0.90, 0.89, 0.85), rough=0.35)
    c.paint(c.frame_mask(c.w * 0.05, c.h * 0.12, c.w * 0.95, c.h * 0.88, 2.0), (0.20, 0.20, 0.22))
    c.paint(c.rect_mask(c.w * 0.18, c.h * 0.42, c.w * 0.66, c.h * 0.42 + 5.0), (0.25, 0.25, 0.27), alpha=0.7)
    return c


def cell_hang_tabs(w, h):
    c = Cell(w, h, 220.0 / w, base=(0.30, 0.45, 0.26), rough=0.32)
    for k in range(4):
        x0 = c.w * (0.02 + 0.245 * k)
        tab = c.rect_mask(x0, c.h * 0.10, x0 + c.w * 0.22, c.h * 0.90)
        c.paint(tab, (0.78, 0.82, 0.84), rough=0.75, height=0.3)
        c.paint(c.rect_mask(x0 + 6, c.h * 0.24, x0 + c.w * 0.22 - 6, c.h * 0.78), (0.90, 0.89, 0.85), rough=0.5)
        c.paint(c.rect_mask(x0 + 6, c.h * 0.10, x0 + c.w * 0.22 - 6, c.h * 0.20), LABEL[(k * 3) % 10], rough=0.6)
        c.paint(c.rect_mask(x0 + 14, c.h * 0.46, x0 + c.w * 0.16, c.h * 0.46 + 4), (0.25, 0.25, 0.27), alpha=0.6)
    return c


def cell_fiche_env(w, h):
    """A microfiche jacket in its paper envelope, 152 x 105 mm: white with a coloured header strip and a ruled title line."""
    c = Cell(w, h, 152.0 / w, base=(0.90, 0.89, 0.85), rough=0.3)
    c.grain(0.03, 1.8, 161)
    c.paint(c.rect_mask(0, 0, c.w, c.h * 0.16), LABEL[5], rough=0.4)
    c.paint(c.rect_mask(c.w * 0.06, c.h * 0.26, c.w * 0.70, c.h * 0.26 + 3), (0.30, 0.30, 0.32), alpha=0.7)
    c.paint(c.rect_mask(c.w * 0.06, c.h * 0.40, c.w * 0.94, c.h * 0.92), (0.16, 0.17, 0.20), rough=0.55, alpha=0.85)
    return c


def cell_vcr(w, h):
    """A 1988 front-loading VHS deck's face (430 x 97 mm): the cassette door, a dark display, silver keys, no words."""
    c = Cell(w, h, 430.0 / w, base=(0.07, 0.07, 0.075), rough=0.5)
    c.grain(0.04, 2.0, 171, 0.02)
    c.paint(c.rect_mask(0, c.h * 0.70, c.w, c.h), (0.42, 0.42, 0.43), rough=0.55)
    door = c.rect_mask(c.w * 0.06, c.h * 0.16, c.w * 0.46, c.h * 0.58)
    c.paint(door, (0.10, 0.10, 0.105), rough=0.45, height=0.3)
    c.paint(c.frame_mask(c.w * 0.06, c.h * 0.16, c.w * 0.46, c.h * 0.58, 1.6), (0.03, 0.03, 0.03), height=-0.2)
    c.paint(c.rect_mask(c.w * 0.54, c.h * 0.18, c.w * 0.74, c.h * 0.42), (0.04, 0.07, 0.06), rough=0.85)
    for k in range(6):
        bx = c.w * (0.55 + 0.068 * k)
        c.paint(c.rect_mask(bx, c.h * 0.50, bx + c.w * 0.05, c.h * 0.62), (0.58, 0.58, 0.58), rough=0.55, height=0.4)
    for k in range(3):
        bx = c.w * (0.80 + 0.055 * k)
        c.paint(c.disc_mask(bx, c.h * 0.30, c.h * 0.07), (0.50, 0.50, 0.50), rough=0.55, height=0.4)
    c.paint(c.rect_mask(c.w * 0.92, c.h * 0.48, c.w * 0.97, c.h * 0.62), (0.58, 0.58, 0.58), height=0.4)
    return c


def cell_strip(w, h):
    """A three-outlet power strip's face (180 x 56 mm): black, three beige duplex faces, a dark rocker."""
    c = Cell(w, h, 180.0 / w, base=(0.06, 0.06, 0.06), rough=0.45)
    for k in range(3):
        cx = c.w * (0.18 + 0.24 * k)
        c.paint(c.rect_mask(cx - c.w * 0.09, c.h * 0.18, cx + c.w * 0.09, c.h * 0.82), (0.66, 0.62, 0.52), rough=0.5, height=0.3)
        for s in (-1, 1):
            c.paint(c.rect_mask(cx + s * c.w * 0.03 - 1.5, c.h * 0.32, cx + s * c.w * 0.03 + 1.5, c.h * 0.52), (0.08, 0.07, 0.06), height=-0.4)
        c.paint(c.disc_mask(cx, c.h * 0.68, 2.6), (0.08, 0.07, 0.06), height=-0.4)
    c.paint(c.rect_mask(c.w * 0.88, c.h * 0.25, c.w * 0.96, c.h * 0.75), (0.35, 0.06, 0.05), rough=0.5, height=0.5)
    return c


def cell_reader_plate(w, h):
    """The fiche reader's front plate (120 x 60 mm): a three-way rocker (low / high / off) and a knob ring, ticks without words."""
    c = Cell(w, h, 120.0 / w, base=(0.70, 0.66, 0.57), rough=0.42)
    c.paint(c.frame_mask(c.w * 0.04, c.h * 0.10, c.w * 0.96, c.h * 0.90, 1.6), (0.40, 0.38, 0.33))
    c.paint(c.rect_mask(c.w * 0.12, c.h * 0.28, c.w * 0.36, c.h * 0.72), (0.08, 0.08, 0.08), height=0.4)
    for k in range(3):
        c.paint(c.rect_mask(c.w * 0.40, c.h * (0.30 + 0.17 * k), c.w * 0.46, c.h * (0.30 + 0.17 * k) + 2), (0.30, 0.28, 0.25))
    c.paint(c.ring_mask(c.w * 0.74, c.h * 0.5, c.h * 0.20, c.h * 0.24), (0.30, 0.28, 0.25))
    return c


def cell_transparency(w, h):
    """The acetate on the stage, seen from above (254 mm square): a framed chart in black and two colours on clear film."""
    c = Cell(w, h, 254.0 / w, base=(0.0, 0.0, 0.0), rough=0.8)
    c.A[:] = 0.0
    rng = np.random.default_rng(1986)
    ink = [(0.05, 0.05, 0.06), (0.12, 0.25, 0.62), (0.68, 0.12, 0.10)]
    m = np.zeros((c.h, c.w), np.float32)
    col = np.zeros((c.h, c.w, 3), np.float32)

    def put(mask, k):
        nonlocal m, col
        col = col * (1 - mask[..., None]) + np.array(ink[k], np.float32) * mask[..., None]
        m = np.maximum(m, mask)
    put(c.frame_mask(c.w * 0.08, c.h * 0.08, c.w * 0.92, c.h * 0.92, 3.0), 0)
    put(c.rect_mask(c.w * 0.14, c.h * 0.14, c.w * 0.62, c.h * 0.20), 0)
    ax0, ay0 = c.w * 0.18, c.h * 0.82
    put(c.line_mask(ax0, ay0, c.w * 0.84, ay0, 3.0), 0)
    put(c.line_mask(ax0, ay0, ax0, c.h * 0.30, 3.0), 0)
    for k, v in enumerate((0.30, 0.42, 0.38, 0.52, 0.47)):
        bx0 = ax0 + c.w * (0.04 + 0.12 * k)
        put(c.rect_mask(bx0, ay0 - v * c.h, bx0 + c.w * 0.07, ay0 - 1), 1 + (k % 2))
    pts = [(ax0 + c.w * (0.075 + 0.12 * k), ay0 - c.h * (0.36 + 0.05 * math.sin(k * 1.7))) for k in range(5)]
    put(c.poly_mask(pts, 3.0), 2)
    c.A = col
    c.C = 1.0 - 0.0 * m
    c.E = m[..., None] * np.ones((1, 1, 3), np.float32)     # coverage, used by the stage cell
    return c


def cell_plain(w, h, mm, col, rough, seed, fibre=0.06):
    c = Cell(w, h, mm, base=col, rough=rough)
    c.grain(fibre, 1.6, seed, 0.02)
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


# ============================================================================ Prop_ProjectionLit (A + E, 1024)
PCELLS = {"SCREEN": (0, 0, 768, 768), "STAGE": (768, 0, 256, 256), "FICHE": (768, 256, 256, 224)}
PSIZE = 1024


def _quad_sample(src, sx, sy):
    """Bilinear sample of src (h, w, k) at float pixel coords."""
    h, w = src.shape[:2]
    x0 = np.clip(np.floor(sx).astype(int), 0, w - 1)
    y0 = np.clip(np.floor(sy).astype(int), 0, h - 1)
    x1, y1 = np.clip(x0 + 1, 0, w - 1), np.clip(y0 + 1, 0, h - 1)
    fx, fy = (sx - np.floor(sx))[..., None], (sy - np.floor(sy))[..., None]
    a = src[y0, x0] * (1 - fx) + src[y0, x1] * fx
    b = src[y1, x0] * (1 - fx) + src[y1, x1] * fx
    return a * (1 - fy) + b * fy


def _transmit(ink):
    """What an ink on clear film lets through: a coloured ink passes its own hue (scaled to its brightest channel), black
    passes almost nothing."""
    m = np.max(ink, axis=-1, keepdims=True)
    coloured = smooth(0.18, 0.30, m)
    return (ink / np.maximum(m, 1e-3) * 0.85) * coloured + 0.035 * (1 - coloured)


def projection_lit():
    A = np.zeros((2 * PSIZE, 2 * PSIZE, 3), np.float32)
    E = np.zeros((2 * PSIZE, 2 * PSIZE, 3), np.float32)
    tr = cell_transparency(256, 256)             # 512 x 512 at 2x: col (A) and coverage (E)
    tcol, tcov = tr.A, tr.E[..., 0]
    warm = np.array((1.0, 0.93, 0.80), np.float32)
    # SCREEN: 1.78 m matte white; the overhead's image keystoned (wider at the top), a hot centre, a coloured rim
    x, y, w, h = [2 * v for v in PCELLS["SCREEN"]]
    c = Cell(w // 2, h // 2, 1780.0 / (w // 2), base=(0.86, 0.86, 0.84), rough=0.20)
    c.grain(0.02, 2.2, 201, 0.0)
    u = (c.x + 0.5) / c.w
    v = (c.y + 0.5) / c.h
    top_half, bot_half = 0.43, 0.38               # half widths at the image's top and bottom (fractions of the screen)
    ty0, ty1 = 0.10, 0.84                          # the image's top and bottom
    t = (v - ty0) / (ty1 - ty0)
    half = top_half + (bot_half - top_half) * t
    s = (u - 0.5) / np.maximum(half, 1e-3) * 0.5 + 0.5   # 0..1 across the keystone
    inside = smooth(-0.004, 0.004, s) * smooth(-0.004, 0.004, 1 - s) * smooth(-0.004, 0.004, t) * smooth(-0.004, 0.004, 1 - t)
    # the stage aperture (285 mm) carries the 254 mm transparency: map its square into the inner 89 %
    k = 254.0 / 285.0
    su, sv = (s - 0.5) / k + 0.5, (t - 0.5) / k + 0.5
    on_film = smooth(-0.003, 0.003, su) * smooth(-0.003, 0.003, 1 - su) * smooth(-0.003, 0.003, sv) * smooth(-0.003, 0.003, 1 - sv)
    sx, sy = np.clip(su, 0, 1) * (tcol.shape[1] - 1), np.clip(sv, 0, 1) * (tcol.shape[0] - 1)
    ink = _quad_sample(tcol, sx, sy)
    cov = _quad_sample(tcov[..., None], sx, sy)[..., 0] * on_film
    hot = 1.0 - 0.22 * np.clip(((s - 0.5) ** 2 + (t - 0.5) ** 2) * 2.4, 0, 1)
    rim = smooth(0.0, 0.03, s) * smooth(0.0, 0.03, 1 - s) * smooth(0.0, 0.03, t) * smooth(0.0, 0.03, 1 - t)
    fringe = (1 - rim)[..., None] * np.array((-0.10, -0.06, 0.10), np.float32)
    film_edge = 1.0 - 0.06 * (on_film * (1 - smooth(0.0, 0.01, np.minimum(np.minimum(su, 1 - su), np.minimum(sv, 1 - sv)))))
    lit = warm[None, None, :] * (hot * film_edge)[..., None] + fringe
    lit = lit * (1 - cov[..., None]) + _transmit(ink) * lit * cov[..., None]
    E[y:y + h, x:x + w] = np.clip(lit, 0, 2) * inside[..., None]
    A[y:y + h, x:x + w] = c.A
    # STAGE: the Fresnel stage from above (285 mm), dark when off; the transparency lies on it
    x, y, w, h = [2 * v for v in PCELLS["STAGE"]]
    c = Cell(w // 2, h // 2, 285.0 / (w // 2), base=(0.11, 0.115, 0.12), rough=0.85)
    X, Y = c.xy_mm() if hasattr(c, "xy_mm") else (None, None)
    X = (c.x + 0.5 - c.w / 2) * c.mm
    Y = (c.h / 2 - c.y - 0.5) * c.mm
    r = np.hypot(X, Y)
    rings = 0.5 + 0.5 * np.cos(r / 0.9 * 2 * math.pi)
    c.A = c.A * (1 + 0.06 * rings)[..., None]
    su = (X / 254.0) + 0.5
    sv = 0.5 - (Y / 254.0)
    on_film = smooth(-0.003, 0.003, su) * smooth(-0.003, 0.003, 1 - su) * smooth(-0.003, 0.003, sv) * smooth(-0.003, 0.003, 1 - sv)
    sx, sy = np.clip(su, 0, 1) * (tcol.shape[1] - 1), np.clip(sv, 0, 1) * (tcol.shape[0] - 1)
    ink = _quad_sample(tcol, sx, sy)
    cov = _quad_sample(tcov[..., None], sx, sy)[..., 0] * on_film
    c.A = c.A * (1 - cov[..., None] * 0.6) + ink * cov[..., None] * 0.6 + 0.03 * on_film[..., None]
    glow = warm[None, None, :] * (1.0 - 0.25 * np.clip(r / 160.0, 0, 1) ** 2)[..., None] * smooth(146.0, 140.0, np.maximum(np.abs(X), np.abs(Y)))[..., None]
    glow = glow * (1 - cov[..., None]) + _transmit(ink) * glow * cov[..., None]
    E[y:y + h, x:x + w] = np.clip(glow, 0, 2)
    A[y:y + h, x:x + w] = c.A
    # FICHE: the reader's blue-tinted rear screen (292 x 248 mm): dark when off, a filmed page when lit
    x, y, w, h = [2 * v for v in PCELLS["FICHE"]]
    c = Cell(w // 2, h // 2, 292.0 / (w // 2), base=(0.10, 0.13, 0.16), rough=0.86)
    rng = np.random.default_rng(211)
    page = np.ones((c.h, c.w, 3), np.float32) * np.array((0.80, 0.88, 0.95), np.float32)
    X = (c.x + 0.5) * c.mm
    Y = (c.y + 0.5) * c.mm
    pm = c.rect_mask(c.w * 0.14, c.h * 0.06, c.w * 0.86, c.h * 0.96, 1.5)
    yy = 0.12
    marks = np.zeros((c.h, c.w), np.float32)
    while yy < 0.92:
        if rng.random() < 0.12:
            yy += 0.025
            continue
        xx = 0.18
        xe = 0.82 - (0.25 if rng.random() < 0.2 else 0.0)
        while xx < xe:
            L = rng.uniform(0.02, 0.07)
            marks = np.maximum(marks, c.rect_mask(c.w * xx, c.h * yy, c.w * min(xx + L, xe), c.h * yy + 2.2, 0.8))
            xx += L + rng.uniform(0.008, 0.014)
        yy += 0.027
    lit = page * pm[..., None] * (1 - 0.82 * marks[..., None]) + np.array((0.25, 0.30, 0.36), np.float32) * (1 - pm[..., None])
    hot = 1.0 - 0.25 * np.clip((((X / (c.w * c.mm)) - 0.5) ** 2 + ((Y / (c.h * c.mm)) - 0.5) ** 2) * 2.5, 0, 1)
    E[y:y + h, x:x + w] = np.clip(lit * hot[..., None], 0, 2)
    A[y:y + h, x:x + w] = c.A
    return down2(A), down2(E)


# ============================================================================ Prop_LaminateMatte (512, tile 0.30 m)
def laminate_matte(size=512, tile_mm=300.0):
    mm = tile_mm / size
    rng = np.random.default_rng(1985)
    fine = spectral(size, size, 0.9, 301)
    broad = spectral(size, size, 2.4, 303)
    A = np.ones((size, size, 3), np.float32) * 0.90
    A *= (1.0 + 0.018 * (broad - 0.5))[..., None]
    specks = (spectral(size, size, 0.2, 305) > 0.985).astype(np.float32)
    A *= (1.0 - 0.05 * specks)[..., None]
    H = 0.010 * (fine - 0.5) + 0.020 * (broad - 0.5)
    R = 0.40 + 0.05 * (spectral(size, size, 1.8, 307) - 0.5)
    yy, xx = np.mgrid[0:size, 0:size].astype(np.float32)
    for k in range(5):
        cx, cy, rr = rng.uniform(0, size), rng.uniform(0, size), rng.uniform(size * 0.2, size * 0.6)
        d = np.hypot(((xx - cx + size / 2) % size) - size / 2, ((yy - cy + size / 2) % size) - size / 2)
        R += 0.05 * smooth(6.0, 0.0, np.abs(d - rr)) * (spectral(size, size, 1.2, 309 + k) > 0.45)
    C = np.ones((size, size), np.float32)
    gy, gx = np.gradient(H / mm)
    N = np.stack([-gx, gy, np.ones_like(gx)], -1)
    N /= np.linalg.norm(N, axis=-1, keepdims=True)
    return A, N, np.clip(R, 0.18, 0.95), C


# ============================================================================ assembly
def build_atlas():
    names = sorted(CELLS)
    for i, a in enumerate(names):
        xa, ya, wa, ha = CELLS[a]
        assert xa + wa <= SIZE and ya + ha <= SIZE, a
        for b in names[i + 1:]:
            xb, yb, wb, hb = CELLS[b]
            assert xa + wa <= xb or xb + wb <= xa or ya + ha <= yb or yb + hb <= ya, (a, b)
    A = np.zeros((SIZE, SIZE, 3), np.float32) + np.array((0.45, 0.45, 0.45), np.float32)
    Hpx = np.zeros((SIZE, SIZE), np.float32)
    R = np.full((SIZE, SIZE), 0.4, np.float32)
    C = np.ones((SIZE, SIZE), np.float32)
    makers = {
        "FILES_A": lambda w, h: cell_files(w, h, SEED_A, "A"), "FILES_B": lambda w, h: cell_files(w, h, SEED_B, "B"),
        "FILES_TOP_A": lambda w, h: cell_files_top(w, h, SEED_A), "FILES_TOP_B": lambda w, h: cell_files_top(w, h, SEED_B),
        "BOARD_WRITTEN": cell_board_written, "BOARD_CLEAN": cell_board_clean, "BOARD_CHALK": cell_board_chalk,
        "BOARD_TRAY": cell_board_tray, "FLIP_WRITTEN": cell_flip_written, "CARTON_END": cell_carton_end,
        "CARTON_SIDE": cell_carton_side, "VCR_FACE": cell_vcr, "COLOURS": cell_colours, "STRIP_FACE": cell_strip,
        "READER_PLATE": cell_reader_plate, "FICHE_ENV": cell_fiche_env, "HANG_TABS": cell_hang_tabs,
        "LABEL_CARD": cell_label_card, "TENT_CARD": cell_tent_card, "HANDOUT": cell_handout, "BINDER_SPINE": cell_binder_spine,
        "FILE_SIDE": cell_file_side, "OUTGUIDE_RED": lambda w, h: cell_outguide(w, h, (0.60, 0.13, 0.10)),
        "OUTGUIDE_BLUE": lambda w, h: cell_outguide(w, h, (0.13, 0.25, 0.55)),
        "TRANSPARENCY": lambda w, h: _transparency_view(w, h),
        "MANILA": lambda w, h: cell_plain(w, h, 311.0 / w, (0.86, 0.77, 0.56), 0.28, 401),
        "KRAFT": lambda w, h: cell_plain(w, h, 311.0 / w, (0.62, 0.47, 0.30), 0.26, 403),
        "PRESS_RED": lambda w, h: cell_plain(w, h, 311.0 / w, (0.58, 0.16, 0.12), 0.30, 405, 0.08),
        "PRESS_GREEN": lambda w, h: cell_plain(w, h, 311.0 / w, (0.30, 0.45, 0.26), 0.32, 407, 0.08),
    }
    for name, (x, y, w, h) in CELLS.items():
        c = makers[name](w, h)
        A[y:y + h, x:x + w] = down2(c.A)
        Hpx[y:y + h, x:x + w] = down2(c.H) / (2 * c.mm)
        R[y:y + h, x:x + w] = down2(np.clip(c.R, 0.18, 0.95))
        C[y:y + h, x:x + w] = down2(c.C)
        print("[g3a2] cell %-14s %4d x %-4d  %.3f mm/px" % (name, w, h, 2 * c.mm))
    return A, Hpx, R, C


def _transparency_view(w, h):
    """The acetate as a loose sheet (on the cart, in a box lid): the chart's ink on a pale grey-clear base."""
    t = cell_transparency(w, h)
    cov = t.E[..., 0]
    c = Cell(w, h, 254.0 / w, base=(0.70, 0.72, 0.73), rough=0.82)
    c.A = c.A * (1 - cov[..., None]) + t.A * cov[..., None]
    return c


def save_set(prefix, A, Npx, R, C, sizes):
    a = A
    while a.shape[0] > sizes[0]:
        a = down2(a)
    save(prefix + "_A", a, "RGB")
    n, r, cc = Npx, R, C
    while n.shape[0] > sizes[1]:
        n, r, cc = down2(n), down2(r), down2(cc)
    n = n / np.linalg.norm(n, axis=-1, keepdims=True)
    save(prefix + "_N", n * .5 + .5, "RGB")
    s = np.stack([r, cc, np.zeros_like(r), np.ones_like(r)], -1)
    save(prefix + "_S", s, "RGBA")


def main():
    A, Hpx, R, C = build_atlas()
    gy_img, gx = np.gradient(Hpx)
    N = np.stack([-gx * 0.8, gy_img * 0.8, np.ones_like(gx)], -1)
    N /= np.linalg.norm(N, axis=-1, keepdims=True)
    save_set("Prop_PaperAtlas", A, N, R, C, (2048, 1024))
    save_set("Prop_PaperAtlas_Phone", A, N, R, C, (1024, 512))
    PA, PE = projection_lit()
    save("Prop_ProjectionLit_A", PA, "RGB")
    save("Prop_ProjectionLit_E", PE, "RGB")
    save("Prop_ProjectionLit_Phone_A", down2(PA), "RGB")
    save("Prop_ProjectionLit_Phone_E", down2(PE), "RGB")
    la, ln, lr, lc = laminate_matte(512)
    save_set("Prop_LaminateMatte", la, ln, lr, lc, (512, 512))
    save_set("Prop_LaminateMatte_Phone", la, ln, lr, lc, (256, 256))
    print("[g3a2] wrote maps to %s" % OUT)


if __name__ == "__main__":
    main()
