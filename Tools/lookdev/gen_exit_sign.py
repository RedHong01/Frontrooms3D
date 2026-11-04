"""EXIT sign textures (exit-sign workflow; spec research/exit_sign/10_spec.md §6).

Generated from numbers only (numpy + PIL, no external images; no scipy). The
letters, chevrons, screws and lamps come from
Tools/Blender/frontrooms_kit/assets/exit_sign_geom.py, the same file the
Blender modules cut the plate from, so print and holes cannot drift apart.

Writes to <out>/ (default: Assets/Resources/Surfaces/Textures of this project):
  ExitSign_Face_A/_N/_S       2048 x 1024 over the face 0.346 x 0.212 m (decal)
  ExitSign_Diffuser_A/_S/_E   1024 x 512 over the opening 0.320 x 0.186 m (decal)
  ExitSign_Diffuser_F         linear RGBA: R lamp A, G lamp B, B DC pair, A transmission
  ExitSign_Enamel_A/_N/_S     1024 x 1024, tiling, 0.40 m (housing)
and a review sheet + exit_sign_textures_report.json (field checks vs S1) to --report-dir.

Conventions: u runs to the READER's right, v up (image row 0 = the top).
_A/_E are sRGB; _N is a tangent-space normal map (+Y up, OpenGL); _S/_F are
linear (R smoothness, G cavity for _S). Deterministic (fixed seeds).

  python3 gen_exit_sign.py [--out DIR] [--report-dir DIR]
"""

import argparse
import json
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw


class ndimage:
    """numpy stand-in for scipy.ndimage.gaussian_filter (truncate 4.0; modes
    "reflect" = scipy's default, and "wrap"). The machines here have no scipy
    and nothing may be installed, so the generator carries its own filter."""

    @staticmethod
    def _axis(a, k, axis, mode):
        r = (len(k) - 1) // 2
        n = a.shape[axis]
        pad = [(0, 0)] * a.ndim
        pad[axis] = (r, r)
        b = np.pad(a, pad, mode="wrap" if mode == "wrap" else "symmetric")
        L = b.shape[axis] + len(k) - 1
        nfft = 1 << (L - 1).bit_length()
        shape = [1] * a.ndim
        shape[axis] = -1
        fk = np.fft.rfft(k, nfft).reshape([-1 if i == axis else 1 for i in range(a.ndim)])
        out = np.fft.irfft(np.fft.rfft(b, nfft, axis=axis) * fk, nfft, axis=axis)
        sl = [slice(None)] * a.ndim
        sl[axis] = slice(2 * r, 2 * r + n)
        return out[tuple(sl)]

    @staticmethod
    def gaussian_filter(a, sigma, mode="reflect", truncate=4.0):
        a = np.asarray(a, np.float64)
        r = int(truncate * float(sigma) + 0.5)
        if r < 1:
            return a.astype(np.float32)
        x = np.arange(-r, r + 1, dtype=np.float64)
        k = np.exp(-0.5 * (x / float(sigma)) ** 2)
        k /= k.sum()
        for ax in range(a.ndim):
            a = ndimage._axis(a, k, ax, mode)
        return a.astype(np.float32)

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, os.path.join(ROOT, "Tools", "Blender", "frontrooms_kit", "assets"))
import exit_sign_geom as g  # noqa: E402

FACE_W, FACE_H = 2048, 1024
DIFF_W, DIFF_H = 1024, 512
ENAMEL = 1024
ENAMEL_TILE_MM = 400.0

ENAMEL_SRGB = np.array([230, 226, 212], np.float32)      # baked white enamel, lightly yellowed
STEEL_SRGB = np.array([120, 118, 112], np.float32)       # bare steel in a chip
PRIMER_SRGB = np.array([140, 62, 44], np.float32)        # red-oxide primer at a chip's edge
DUST_SRGB = np.array([170, 162, 146], np.float32)
DIFF_LIN = np.array([0.15, 0.022, 0.018], np.float32)    # red acrylic over the white interior (linear)
SMOOTH_FACE, SMOOTH_DUST, SMOOTH_PRINT, SMOOTH_CHIP = 0.42, 0.25, 0.55, 0.35
SMOOTH_DIFF, SMOOTH_DIFF_DUST, DIFF_CAVITY = 0.55, 0.40, 0.70


# ------------------------------------------------------------------ colour helpers
def to_lin(srgb):
    c = np.asarray(srgb, np.float32) / 255.0
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def to_srgb8(lin):
    c = np.clip(lin, 0, 1)
    s = np.where(c <= 0.0031308, 12.92 * c, 1.055 * np.power(c, 1 / 2.4) - 0.055)
    return np.clip(np.round(s * 255), 0, 255).astype(np.uint8)


def lin8(x):
    return np.clip(np.round(np.asarray(x) * 255), 0, 255).astype(np.uint8)


def save(img, path):
    Image.fromarray(img).save(path, compress_level=6)


# ------------------------------------------------------------------ rasters
class Grid:
    """Pixel grid over a rectangle in READER mm (x right, y up)."""

    def __init__(self, w_px, h_px, x0, y0, w_mm, h_mm):
        self.w, self.h = w_px, h_px
        self.x0, self.y0, self.wm, self.hm = x0, y0, w_mm, h_mm
        self.sx, self.sy = w_px / w_mm, h_px / h_mm          # px per mm
        j, i = np.mgrid[0:h_px, 0:w_px].astype(np.float32)
        self.X = x0 + (i + 0.5) / self.sx                    # mm
        self.Y = y0 + h_mm - (j + 0.5) / self.sy

    def px(self, x, y, ss=1):
        return ((x - self.x0) * self.sx * ss, (self.y0 + self.hm - y) * self.sy * ss)

    def mask(self, polys, ss=4, outline=None):
        """Anti-aliased coverage of polygons (READER mm). outline = line width in mm -> outlines only."""
        img = Image.new("L", (self.w * ss, self.h * ss), 0)
        d = ImageDraw.Draw(img)
        for poly in polys:
            pts = [self.px(x, y, ss) for x, y in poly]
            if outline is None:
                d.polygon(pts, fill=255)
            else:
                wpx = max(1, int(round(outline * self.sx * ss)))
                d.line(pts + [pts[0]], fill=255, width=wpx, joint="curve")
        img = img.resize((self.w, self.h), Image.BOX)
        return np.asarray(img).astype(np.float32) / 255.0


def noise(shape, sigma, rng, octaves=1):
    out = np.zeros(shape, np.float32)
    amp = 1.0
    tot = 0.0
    for o in range(octaves):
        n = rng.standard_normal(shape).astype(np.float32)
        n = ndimage.gaussian_filter(n, sigma * (2 ** o), mode="wrap")
        n /= (n.std() + 1e-8)
        out += amp * n
        tot += amp
        amp *= 0.5
    return out / tot


def orange_peel(shape, cell_px, rng):
    """Enamel orange peel: a band-pass (DoG) field with cells of about cell_px."""
    n = rng.standard_normal(shape).astype(np.float32)
    a = ndimage.gaussian_filter(n, cell_px * 0.30, mode="wrap")
    b = ndimage.gaussian_filter(n, cell_px * 0.85, mode="wrap")
    d = a - b
    return d / (d.std() + 1e-8)


def height_to_normal(h_mm, sx, sy, strength=1.0):
    """Tangent-space normal (+X u right, +Y v up) from a height field in mm."""
    dx = (np.roll(h_mm, -1, 1) - np.roll(h_mm, 1, 1)) * 0.5 * sx      # mm per mm
    dy = (np.roll(h_mm, 1, 0) - np.roll(h_mm, -1, 0)) * 0.5 * sy      # rows go down; v goes up
    nx, ny, nz = -dx * strength, -dy * strength, np.ones_like(h_mm)
    l = np.sqrt(nx * nx + ny * ny + nz * nz)
    n = np.stack([nx / l, ny / l, nz / l], -1)
    return np.clip(np.round((n * 0.5 + 0.5) * 255), 0, 255).astype(np.uint8)


def soft_blob(X, Y, cx, cy, sx, sy, rot=0.0):
    c, s = math.cos(rot), math.sin(rot)
    u = (X - cx) * c + (Y - cy) * s
    v = -(X - cx) * s + (Y - cy) * c
    return np.exp(-0.5 * ((u / sx) ** 2 + (v / sy) ** 2))


def fingerprint(G, cx, cy, warpN, smearN, size=(7.0, 9.0), rot=0.0, pitch=0.48):
    """A partial fingerprint: elliptic whorl ridges at ~0.48 mm pitch in a soft oval
    (warpN / smearN: shared noise fields, so each print costs no new filtering)."""
    c, s = math.cos(rot), math.sin(rot)
    u = (G.X - cx) * c + (G.Y - cy) * s
    v = -(G.X - cx) * s + (G.Y - cy) * c
    r = np.sqrt((u / size[0]) ** 2 + (v / size[1]) ** 2) * size[0] + warpN * 0.25
    ridge = 0.5 + 0.5 * np.sin(2 * math.pi * r / pitch)
    env = np.exp(-0.5 * ((u / (size[0] * 0.85)) ** 2 + (v / (size[1] * 0.85)) ** 2) ** 2)
    smear = np.clip(smearN * 0.5 + 0.6, 0, 1)
    return ridge * env * smear, env * smear


# ------------------------------------------------------------------ the face
def face_maps(rng):
    G = Grid(FACE_W, FACE_H, -g.HOUSING_W / 2, -g.HOUSING_H / 2, g.HOUSING_W, g.HOUSING_H)
    X, Y = G.X, G.Y
    polys = g.letter_polys_open()
    to_r = lambda poly: [g.open_to_reader(p) for p in poly]
    letters = [to_r(p) for k in "EXIT" for p in polys[k]]
    chev = [to_r(p) for k in "<>" for p in polys[k]]
    film = False
    if g.LEGEND is not None:
        # World text: the legend's own holes; no chevron knockouts (research/exit_sign/80).
        letters = [to_r(p) for p in g.legend_holes()]
        chev = []
        film = g.LEGEND.get("mode") == "print" and bool(g.LEGEND.get("film"))
    m_let = legend_lit_mask(G) if film else G.mask(letters)
    # Chevron score: the outline with three 1.5 mm hold-tabs left uncut per chevron.
    score = np.zeros((G.h, G.w), np.float32)
    for poly in chev:
        sc = G.mask([poly], outline=0.3)
        n = len(poly)
        for k in (0, 2, 4):
            ax, ay = poly[k]
            bx, by = poly[(k + 1) % n]
            mx, my = (ax + bx) / 2, (ay + by) / 2
            sc *= 1.0 - (np.hypot(X - mx, Y - my) < 0.75)
        score = np.maximum(score, sc)

    # Distances for the frame / opening layout.
    hw, hh = g.HOUSING_W / 2, g.HOUSING_H / 2
    ow, oh = g.OPEN_W / 2, g.OPEN_H / 2
    edge_in = np.minimum(hw - np.abs(X), hh - np.abs(Y))          # mm in from the outer edge (approx, square)
    in_open = (np.abs(X) <= ow) & (np.abs(Y) <= oh)
    d_open = np.minimum(ow - np.abs(X), oh - np.abs(Y))           # mm in from the opening edge

    # ---------------- albedo (linear)
    base = to_lin(ENAMEL_SRGB)
    tone = 1.0 + 0.012 * noise((G.h, G.w), 30.0, rng, 2) + 0.006 * noise((G.h, G.w), 4.0, rng)
    alb = np.ones((G.h, G.w, 3), np.float32) * base * tone[..., None]
    # Lightly yellowed toward the top (heat rises), and browned over each lamp.
    yellow = np.clip((Y + hh) / (2 * hh), 0, 1) ** 2 * 0.035
    heat = np.zeros_like(X)
    for lamp in (g.LAMP_A, g.LAMP_B):
        la, lb = g.lamp_reader_mm(lamp)
        heat += soft_blob(X, Y, la, lb + 26.0, 42.0, 34.0)
    heat = np.clip(heat, 0, 1)
    warm = np.array([1.0, 0.975, 0.90], np.float32)
    alb *= (1 - (0.06 * heat + yellow))[..., None] * (1 - (1 - warm) * (heat + yellow * 3)[..., None])
    # Letter interiors: enamel (the cut walls were painted after stamping), a touch darker.
    alb *= (1 - 0.08 * m_let)[..., None]
    # Scored knockouts: a dark hairline (-25 %).
    alb *= (1 - 0.25 * score)[..., None]
    # Dust film on the lower lip: the bottom border and the plate's lowest strip.
    dust = np.clip(1 - (Y + hh) / 20.0, 0, 1) ** 1.6
    dust *= 0.7 + 0.3 * np.clip(noise((G.h, G.w), 3.0, rng) * 0.5 + 0.5, 0, 1)
    dust = np.clip(dust + 0.12 * np.clip(noise((G.h, G.w), 1.2, rng) - 1.6, 0, 1), 0, 1)
    # The top fold: the frame's skirt on top faces up, and its UVs are the outermost
    # 0-0.6 mm of the face, so a narrow band here is a dust skin along the whole top.
    dust_top = np.clip(1 - (hh - Y) / 2.2, 0, 1) * (0.55 + 0.25 * np.clip(noise((G.h, G.w), 6.0, rng) * 0.5 + 0.5, 0, 1))
    # The opening's bottom lip (the frame's inner return faces up there): a fine line of dust.
    lip = np.exp(-0.5 * ((Y - (-oh)) / 0.8) ** 2) * (np.abs(X) < ow) * 0.6
    dust = np.clip(np.maximum(dust, np.maximum(dust_top, lip)), 0, 1)
    alb = alb * (1 - 0.30 * dust[..., None]) + to_lin(DUST_SRGB) * (0.30 * dust[..., None])
    # Fingerprints round the screws (relamping) and on the lower border.
    prints_ridge = np.zeros_like(X)
    prints_env = np.zeros_like(X)
    spots = []
    for (sx, sy) in g.screw_positions():
        for k in range(2):
            ang = rng.uniform(0, 2 * math.pi)
            r = rng.uniform(9.0, 18.0)
            spots.append((sx + r * math.cos(ang), np.clip(sy + r * math.sin(ang) * 0.6, -hh + 4, hh - 4), rng.uniform(-0.8, 0.8)))
    spots.append((rng.uniform(-60, -20), -hh + 6.0, 0.3))
    spots.append((rng.uniform(30, 90), -hh + 7.0, -0.2))
    warpN = noise((G.h, G.w), 6.0, rng)
    smearN = noise((G.h, G.w), 10.0, rng)
    for cx, cy, rot in spots:
        rdg, env = fingerprint(G, cx, cy, warpN, smearN, size=(rng.uniform(5.5, 7.5), rng.uniform(7.5, 9.5)), rot=rot)
        prints_ridge = np.maximum(prints_ridge, rdg)
        prints_env = np.maximum(prints_env, env)
    alb *= (1 - 0.035 * prints_ridge)[..., None]
    # Chips: bare steel over a red-oxide primer edge, at sharp corners and one screwdriver slip.
    chip = np.zeros_like(X)
    primer = np.zeros_like(X)
    chips = [(-hw + 2.2, -hh + 2.4, 2.4, 1.6, 0.6), (hw - 24.0, -hh + 1.2, 1.6, 0.9, 0.0), (-ow - 0.3, oh - 9.0, 0.9, 2.0, 0.0)]
    chipN = noise((G.h, G.w), 2.0, rng)
    for cx, cy, ax, ay, rot in chips:
        # irregular edge: the blob is modulated by noise only where it exists (no stray specks)
        blob = soft_blob(X, Y, cx, cy, ax, ay, rot) * (1 + 0.45 * chipN)
        chip = np.maximum(chip, (blob > 0.55).astype(np.float32))
        primer = np.maximum(primer, ((blob > 0.40) & (blob <= 0.55)).astype(np.float32))
    # Screwdriver slip: a 6 mm arc scratch beside the top-right screw.
    sxr, syr = g.screw_positions()[1]
    t = np.arctan2(Y - syr, X - sxr)
    rr = np.hypot(X - sxr, Y - syr)
    slip = ((np.abs(rr - 4.6) < 0.14) & (t > math.radians(200)) | (np.abs(rr - 4.6) < 0.14) & (t < math.radians(-110)) & (t > math.radians(-170))).astype(np.float32)
    slip = ndimage.gaussian_filter(slip, 0.6)
    slip = np.clip(slip * 2.5, 0, 1)
    chip = np.clip(chip, 0, 1)
    primer = np.clip(ndimage.gaussian_filter(primer, 0.5), 0, 1) * (1 - chip)
    alb = alb * (1 - primer[..., None]) + to_lin(PRIMER_SRGB) * primer[..., None]
    steel = np.maximum(chip, slip)
    alb = alb * (1 - steel[..., None]) + to_lin(STEEL_SRGB) * steel[..., None]
    A = to_srgb8(alb)

    # ---------------- height (mm) -> normal
    h = 0.006 * orange_peel((G.h, G.w), 0.7 * G.sx, rng)
    h -= 0.08 * score
    dents = []
    for idx in (2, 1):                         # bottom-left and top-right screws were over-tightened
        sx_, sy_ = g.screw_positions()[idx]
        r = np.hypot(X - sx_, Y - sy_)
        h -= 0.12 * np.exp(-0.5 * ((r - 3.6) / 1.4) ** 2) * (r > 3.1)
        dents.append((sx_, sy_))
    h += 0.04 * (chip + primer * 0.5)          # chip edge: the paint layer's step (negative inside)
    h -= 0.05 * chip
    h -= 0.03 * slip
    N = height_to_normal(h, G.sx, G.sy)

    # ---------------- smoothness / cavity
    rough = SMOOTH_FACE + 0.02 * noise((G.h, G.w), 12.0, rng) + 0.015 * noise((G.h, G.w), 2.0, rng)
    rough = rough * (1 - dust) + SMOOTH_DUST * dust
    rough = rough + (SMOOTH_PRINT - SMOOTH_FACE) * prints_ridge * prints_env
    rough = rough * (1 - chip) + SMOOTH_CHIP * chip
    rough = rough * (1 - slip) + 0.60 * slip
    rough = rough * (1 - 0.25 * score)
    cav = np.ones_like(X)
    # round the screws
    for sx_, sy_ in g.screw_positions():
        r = np.hypot(X - sx_, Y - sy_)
        cav *= 1 - 0.45 * np.exp(-0.5 * ((r - 3.15) / 0.35) ** 2)
    # the bead's two edges and the frame/opening junction
    for d in (g.BEAD_IN0, g.BEAD_IN1):
        cav *= 1 - 0.25 * np.exp(-0.5 * ((edge_in - d) / 0.35) ** 2)
    cav *= 1 - 0.20 * np.exp(-0.5 * ((edge_in - 0.1) / 0.5) ** 2)
    occl = np.where(in_open, np.exp(-np.clip(d_open, 0, None) / 2.2), 0.0)
    cav *= 1 - 0.30 * occl
    # letter edges: a thin dark ring on the plate round every hole (a printed film has no cut edge)
    ring = np.clip(ndimage.gaussian_filter(m_let, 1.2) - m_let, 0, 1)
    if not film:
        cav *= 1 - 0.8 * ring
    cav *= 1 - 0.5 * score
    S = np.stack([lin8(np.clip(rough, 0, 1)), lin8(np.clip(cav, 0, 1)), np.zeros_like(lin8(cav)), np.full_like(lin8(cav), 255)], -1)

    info = {"pxPerMm": [round(G.sx, 3), round(G.sy, 3)], "smoothMeanFace": float(np.mean(rough[~in_open | (m_let < 0.5)])),
            "smoothMeanAll": float(np.mean(rough)), "fingerprints": len(spots), "chips": len(chips), "dents": len(dents)}
    return A, N, S, info, m_let


# ------------------------------------------------------------------ the diffuser
def lamp_field(G, lamps, fill_per_lamp):
    """Sum of Gaussians on the opening (u, v 0-1 from the reader's bottom-left)."""
    u = (G.X + g.OPEN_W / 2) / g.OPEN_W
    v = (G.Y + g.OPEN_H / 2) / g.OPEN_H
    f = np.zeros_like(u)
    for lu, lv, su, sv in lamps:
        f += np.exp(-((u - lu) ** 2 / (2 * su * su) + (v - lv) ** 2 / (2 * sv * sv)))
    return f + fill_per_lamp * len(lamps)


def bug(G, cx, cy, length, rot, rng):
    """A dead insect silhouette (fly / small moth), READER mm. Returns (opacity mask)."""
    c, s = math.cos(rot), math.sin(rot)
    u = (G.X - cx) * c + (G.Y - cy) * s
    v = -(G.X - cx) * s + (G.Y - cy) * c
    L = length
    body = np.exp(-0.5 * ((u / (0.30 * L)) ** 2 + (v / (0.12 * L)) ** 2) ** 1.5)
    head = np.exp(-0.5 * (((u - 0.36 * L) / (0.09 * L)) ** 2 + (v / (0.09 * L)) ** 2) ** 1.5)
    m = np.maximum(body, head)
    # wings: two translucent lobes
    for sgn in (-1, 1):
        w = np.exp(-0.5 * (((u + 0.05 * L) / (0.28 * L)) ** 2 + ((v - sgn * 0.20 * L) / (0.13 * L)) ** 2) ** 1.5)
        m = np.maximum(m, 0.55 * w)
    # six legs: thin strokes
    for k, (ang, ln) in enumerate(((60, 0.30), (95, 0.28), (130, 0.32))):
        for sgn in (-1, 1):
            a = math.radians(ang) * sgn
            x0, y0 = (0.10 - 0.08 * k) * L, 0.0
            dx, dy = math.cos(a), math.sin(a)
            t = np.clip((u - x0) * dx + (v - y0) * dy, 0, ln * L)
            d = np.hypot(u - x0 - t * dx, v - y0 - t * dy)
            m = np.maximum(m, np.clip(1 - d / (0.025 * L + 0.12), 0, 1) * 0.9)
    return np.clip(m, 0, 1)


def diffuser_maps(rng):
    G = Grid(DIFF_W, DIFF_H, -g.OPEN_W / 2, -g.OPEN_H / 2, g.OPEN_W, g.OPEN_H)
    X, Y = G.X, G.Y
    hb = (Y + g.OPEN_H / 2)                      # mm from the opening's bottom
    # Insects along the bottom 25 mm (8 of them), and dust. The legend's baseline is
    # 16.8 mm above the opening's bottom, so insects lower than that hide behind the
    # plate: three are placed where the bottom of a letter hole shows them (the E's
    # foot, the X's right foot, the T's stem: dark shapes against the lit red), five
    # lie lower along the bottom wall and only show at grazing angles.
    polys = g.letter_polys_open()
    def foot(letter, fx, dy):
        xs = [p[0] for p in polys[letter][0]]
        x0, x1 = min(xs), max(xs)
        return (x0 + fx * (x1 - x0) - g.OPEN_W / 2, g.Y0 + dy - g.OPEN_H / 2)
    placed = [(foot("E", 0.62, 6.5), 5.5), (foot("X", 0.90, 4.0), 7.5), (foot("T", 0.50, 9.0), 4.5)]
    lit_mask = None
    if g.LEGEND is not None:
        lit_mask = legend_lit_mask(G)
        placed = legend_feet(G, lit_mask, [5.5, 7.5, 4.5], [0.62, 0.90, 0.50])
    ins = np.zeros_like(X)
    bugs = []
    for (cx, cy), L in placed:
        rot = rng.uniform(0, 2 * math.pi)
        ins = np.maximum(ins, bug(G, cx, cy, L, rot, rng))
        bugs.append([round(cx, 1), round(cy, 1), round(L, 1), "visible"])
    for k in range(5):
        cx = rng.uniform(-g.OPEN_W / 2 + 12, g.OPEN_W / 2 - 12)
        cy = -g.OPEN_H / 2 + rng.uniform(2.5, 13.0)
        L = rng.uniform(4.0, 12.0)
        rot = rng.uniform(0, 2 * math.pi)
        ins = np.maximum(ins, bug(G, cx, cy, L, rot, rng))
        bugs.append([round(cx, 1), round(cy, 1), round(L, 1), "behind plate"])
    dust = np.clip(1 - hb / 26.0, 0, 1) ** 1.4 * (0.75 + 0.25 * np.clip(noise((G.h, G.w), 3.0, rng) * 0.5 + 0.5, 0, 1))
    mottle = 1.0 + 0.04 * noise((G.h, G.w), 1.2, rng) + 0.03 * noise((G.h, G.w), 18.0, rng)
    # Albedo (unlit): red acrylic over white; dust greys it; insects are dark.
    alb = DIFF_LIN[None, None, :] * mottle[..., None]
    dust_lin = np.array([0.24, 0.20, 0.17], np.float32)
    alb = alb * (1 - 0.35 * dust[..., None]) + dust_lin * (0.35 * dust[..., None])
    bug_lin = np.array([0.012, 0.008, 0.006], np.float32)
    printed = g.LEGEND is not None and g.LEGEND.get("mode") == "print" and not g.LEGEND.get("film")
    if printed:
        # Option B (research/exit_sign/80): the lens is screen-printed: an opaque white ink (the face enamel's colour)
        # everywhere but the characters, which stay the red acrylic. The insects lie inside the box, behind the lens:
        # they show only through the characters.
        ink = lit_mask
        ins = ins * ink
        alb = alb * (1 - 0.92 * ins[..., None]) + bug_lin * (0.92 * ins[..., None])
        pr = to_lin(ENAMEL_SRGB) * (1.0 + 0.015 * noise((G.h, G.w), 2.0, rng))[..., None]
        pr = pr * (1 - 0.30 * dust[..., None]) + dust_lin * (0.30 * dust[..., None])
        alb = alb * ink[..., None] + pr * (1 - ink[..., None])
    else:
        alb = alb * (1 - 0.92 * ins[..., None]) + bug_lin * (0.92 * ins[..., None])
    A = to_srgb8(alb)
    # Frosted red acrylic (the spec's "fine frosted mottle"): smoothness 0.55, 0.40 under dust.
    # Build round 1 used the spec's 0.72; a DEAD sign seen from below then showed the
    # troffer's highlight across whole letters (white letters), so the sheet is frosted.
    smooth = SMOOTH_DIFF * (1 - dust) + SMOOTH_DIFF_DUST * dust
    smooth = smooth * (1 - 0.6 * ins) + 0.35 * 0.6 * ins
    edge = np.minimum(g.OPEN_W / 2 - np.abs(X), g.OPEN_H / 2 - np.abs(Y))
    # Cavity = occlusion of the recessed sheet: 6.9 mm behind a plate that covers about
    # 2/3 of the opening, inside a closed white box. It dims the room's reflection and
    # ambient on the sheet (not the lamp field), darker toward the opening's rim.
    cav = DIFF_CAVITY - 0.20 * np.exp(-np.clip(edge, 0, None) / 3.0)
    if printed:
        # The printed lens sits behind a full window, not behind a plate: less occluded; satin ink, frosted letters.
        smooth = smooth * ink + (0.45 * (1 - dust) + SMOOTH_DIFF_DUST * dust) * (1 - ink)
        cav = 0.90 - 0.25 * np.exp(-np.clip(edge, 0, None) / 4.0)
    S = np.stack([lin8(smooth), lin8(cav), np.zeros_like(lin8(cav)), np.full_like(lin8(cav), 255)], -1)
    # Lamp fields (linear), each lamp with its own share of the cavity fill.
    fa = lamp_field(G, [g.LAMP_A], g.LAMP_FILL)
    fb = lamp_field(G, [g.LAMP_B], g.LAMP_FILL)
    fdc = lamp_field(G, list(g.DC_LAMPS), g.LAMP_FILL)
    norm = float((fa + fb).max())
    fa, fb, fdc = fa / norm, fb / norm, fdc / norm
    trans = np.clip((1 - 0.85 * ins) * (1 - 0.12 * dust) * (1 + 0.02 * (mottle - 1)), 0, 1)
    if printed:
        trans = trans * (ink + 0.01 * (1 - ink))         # the white ink is opaque (UL 924: translucent letters, opaque ground)
    dither = (rng.random((G.h, G.w)).astype(np.float32) - 0.5) / 255.0
    F = np.stack([lin8(fa + dither), lin8(fb + dither), lin8(fdc + dither), lin8(trans)], -1)
    lit = np.clip((fa + fb) * trans, 0, 1)
    E = to_srgb8(np.repeat(lit[..., None], 3, -1))
    info = {"bugs": bugs, "fieldNorm": round(norm, 4), "dcPeak": round(float(fdc.max()), 4)}
    return A, S, E, F, info, (fa, fb, fdc, trans, G)


def field_checks(fields):
    """§6.3 / T4 checks on the LIT field through the letter holes."""
    fa, fb, fdc, trans, G = fields
    if g.LEGEND is not None:
        return legend_field_checks(fields)
    polys = g.letter_polys_open()
    res = {}
    states = {"LIT": fa + fb, "HALF_A_out": fb, "BATTERY": fdc * g.DC_LEVEL}
    lit_I = None
    for name, f in states.items():
        means, spreads = {}, {}
        allv, allf = [], []
        for k in "EXIT":
            m = G.mask([[g.open_to_reader(p) for p in poly] for poly in polys[k]]) > 0.5
            vals = (f * trans)[m]
            means[k] = float(vals.mean())
            spreads[k] = float(vals.std() / vals.mean())
            allv.append(vals)
            allf.append(f[m])
        if lit_I is None:
            lit_I = means["I"]
        allv = np.concatenate(allv)
        allf = np.concatenate(allf)
        # UL 924's max/min is a property of the lamps and the diffuser: measured on the
        # field without the insects (a dead fly is not part of the sign). The figure
        # with the insects is reported too.
        res[name] = {"relI": {k: round(v / lit_I, 3) for k, v in means.items()},
                     "spread": {k: round(v, 3) for k, v in spreads.items()},
                     "maxOverMin": round(float(allf.max() / max(allf.min(), 1e-6)), 2),
                     "maxOverMinWithInsects": round(float(allv.max() / max(allv.min(), 1e-6)), 2)}
    res["targetRelI"] = g.NIST_REL
    return res


# ------------------------------------------------------------------ world text: a legend in another language
def legend_lit_mask(G):
    """What glows: the stencil's holes, or (print) the printed characters with their counters."""
    L = g.LEGEND
    if L.get("mode") == "print":
        img = Image.new("L", (G.w * 4, G.h * 4), 0)
        d = ImageDraw.Draw(img)
        loops = sorted(L["ink"], key=lambda q: -abs(_area(q)))
        for lp in loops:
            pts = [G.px(*g.open_to_reader(p), ss=4) for p in lp]
            d.polygon(pts, fill=255 if _area(lp) > 0 else 0)
        img = img.resize((G.w, G.h), Image.BOX)
        return np.asarray(img).astype(np.float32) / 255.0
    return G.mask([[g.open_to_reader(p) for p in h] for h in g.legend_holes()])


def _area(poly):
    n = len(poly)
    return 0.5 * sum(poly[i][0] * poly[(i + 1) % n][1] - poly[(i + 1) % n][0] * poly[i][1] for i in range(n))


def legend_stroke_mm():
    """The legend's one constructed stroke (the narrower of its vertical stem and horizontal stroke), mm."""
    st = g.LEGEND.get("strokes") or {}
    vals = [v for v in (st.get("verticalStemMm"), st.get("horizontalStrokeMm")) if v]
    return min(vals) if vals else g.S


def legend_feet(G, lit, lengths, fracs):
    """Three insects that show through the legend: in the left, middle and right thirds of the ink box, the lowest
    point inside the lit shape with at least 9 mm of it to each side, the insect's body 5 mm above that edge.
    A legend whose stroke is under 19 mm (the Latin and Cyrillic die face: H/8, 11-16 mm) asks half its stroke less
    0.5 mm to each side instead (9.0 at the English 19.05 stroke, so 出口 is placed as before); a third with no such
    point takes its lowest lit pixel, so every legend places three and the generator's random stream never shifts."""
    x0, y0, x1, y1 = g.LEGEND["inkBox"]
    m = lit > 0.5
    out = []
    reach_mm = min(9.0, legend_stroke_mm() / 2.0 - 0.5)
    for k, (L, fx) in enumerate(zip(lengths, fracs)):
        a = x0 + (x1 - x0) * k / 3.0
        b = x0 + (x1 - x0) * (k + 1) / 3.0
        best = None
        lowest = None
        cols = np.nonzero((G.X[0] >= a - g.OPEN_W / 2) & (G.X[0] <= b - g.OPEN_W / 2))[0]
        reach = int(round(reach_mm * G.sx))
        for j in cols[::2]:
            col = m[:, j]
            rows = np.nonzero(col)[0]
            if len(rows) == 0:
                continue
            i = rows.max()                     # lowest lit pixel (rows go down)
            if lowest is None or G.Y[i, j] < lowest[1] - 1e-6:
                lowest = (float(G.X[0, j]), float(G.Y[i, j]))
            lo, hi = max(0, j - reach), min(G.w, j + reach + 1)
            if not m[i - 1, lo:hi].all():
                continue
            yy = G.Y[i, j]
            if best is None or yy < best[1] - 1e-6 or (abs(yy - best[1]) < 1e-6 and abs(G.X[0, j] - (a + fx * (b - a) - g.OPEN_W / 2)) < abs(best[0] - (a + fx * (b - a) - g.OPEN_W / 2))):
                best = (float(G.X[0, j]), float(yy))
        if best is None:
            best = lowest
        if best is None:
            continue
        out.append(((best[0], best[1] + 5.0), L))
    return out


def legend_field_checks(fields):
    """Per character: the LIT, HALF A and BATTERY levels through the legend, and the letter shares
    (FrontRoomsExitSign LetterShareA/B/DC: area means of each lamp channel over what glows, relative to LIT)."""
    fa, fb, fdc, trans, G = fields
    L = g.LEGEND
    lit = legend_lit_mask(G) > 0.5
    x0, y0, x1, y1 = L["inkBox"]
    word = L["word"]
    n = len(word)
    # Each character's slot: equal parts of the ink box (出口), or, when the legend lists every character's x extent
    # (the Latin and Cyrillic die face), from the ink box's edge to the midpoint of each gap between characters.
    # A word that repeats a letter keys its characters by position ("1 A" ... "7 G"), so no letter is lost.
    chars = L.get("chars") or []
    if len(chars) == n and all("xMm" in c and "widthMm" in c for c in chars):
        edges = [x0] + [(chars[k]["xMm"] + chars[k]["widthMm"] + chars[k + 1]["xMm"]) / 2.0 for k in range(n - 1)] + [x1]
    else:
        edges = [x0 + (x1 - x0) * k / n for k in range(n + 1)]
    keys = list(word) if len(set(word)) == n else ["%d %s" % (k + 1, ch) for k, ch in enumerate(word)]
    res = {}
    lit_ref = None
    states = {"LIT": fa + fb, "HALF_A_out": fb, "BATTERY": fdc * g.DC_LEVEL}
    for name, f in states.items():
        means, spreads = {}, {}
        allf = []
        for k, ch in enumerate(keys):
            a = edges[k] - g.OPEN_W / 2
            b = edges[k + 1] - g.OPEN_W / 2
            m = lit & (G.X >= a) & (G.X < b)
            vals = (f * trans)[m]
            means[ch] = float(vals.mean())
            spreads[ch] = float(vals.std() / vals.mean())
            allf.append(f[m])
        if lit_ref is None:
            lit_ref = max(means.values())
        allf = np.concatenate(allf)
        res[name] = {"relBrightest": {k: round(v / lit_ref, 3) for k, v in means.items()},
                     "spread": {k: round(v, 3) for k, v in spreads.items()},
                     "maxOverMin": round(float(allf.max() / max(allf.min(), 1e-6)), 2)}
    ma, mb, md = float(fa[lit].mean()), float(fb[lit].mean()), float(fdc[lit].mean())
    res["letterShares"] = {"A": round(ma / (ma + mb), 4), "B": round(mb / (ma + mb), 4), "DC": round(md / (ma + mb), 4)}
    res["englishShares"] = {"A": 0.4587, "B": 0.5413, "DC": 0.6621}
    return res


# ------------------------------------------------------------------ the housing enamel
def enamel_maps(rng):
    n = ENAMEL
    s = n / ENAMEL_TILE_MM                          # px per mm
    base = to_lin(ENAMEL_SRGB)
    tone = 1.0 + 0.012 * noise((n, n), 40.0, rng, 2) + 0.005 * noise((n, n), 2.0, rng)
    alb = np.ones((n, n, 3), np.float32) * base * tone[..., None]
    A = to_srgb8(alb)
    h = 0.006 * orange_peel((n, n), 0.75 * s, rng)
    N = height_to_normal(h, s, s)
    rough = 0.50 + 0.03 * noise((n, n), 20.0, rng) + 0.015 * noise((n, n), 1.5, rng)
    # Build round 3: capped at 0.60 (acceptance T1: face and housing <= 0.6; before, 0.06 % of
    # texels, the fingerprints, reached 0.655).
    S = np.stack([lin8(np.clip(rough, 0, 0.60)), np.full((n, n), 255, np.uint8), np.zeros((n, n), np.uint8), np.full((n, n), 255, np.uint8)], -1)
    return A, N, S, {"smoothMean": float(rough.mean())}


# ------------------------------------------------------------------ review sheet
def review_sheet(path, face_A, face_S, diff_A, diff_E, diff_F, en_A, m_let):
    def fit(a, w):
        im = Image.fromarray(a if a.ndim == 3 else a)
        r = w / im.size[0]
        return im.convert("RGB").resize((w, int(im.size[1] * r)), Image.LANCZOS)
    W = 1800
    tiles = [fit(face_A, W), fit(np.repeat(face_S[..., :1], 3, -1), W // 2), fit(np.repeat(face_S[..., 1:2], 3, -1), W // 2),
             fit(diff_A, W // 2), fit(diff_E, W // 2), fit(diff_F[..., :3], W // 2), fit(np.repeat(diff_F[..., 3:4], 3, -1), W // 2)]
    H = tiles[0].size[1] + tiles[1].size[1] + tiles[3].size[1] + tiles[5].size[1] + 40
    sheet = Image.new("RGB", (W, H), (30, 30, 30))
    y = 0
    sheet.paste(tiles[0], (0, y)); y += tiles[0].size[1] + 10
    sheet.paste(tiles[1], (0, y)); sheet.paste(tiles[2], (W // 2, y)); y += tiles[1].size[1] + 10
    sheet.paste(tiles[3], (0, y)); sheet.paste(tiles[4], (W // 2, y)); y += tiles[3].size[1] + 10
    sheet.paste(tiles[5], (0, y)); sheet.paste(tiles[6], (W // 2, y))
    sheet.save(path, quality=88)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=os.path.join(ROOT, "Assets", "Resources", "Surfaces", "Textures"))
    ap.add_argument("--report-dir", default=None)
    a = ap.parse_args()
    os.makedirs(a.out, exist_ok=True)
    rng = np.random.default_rng(19900401)
    fA, fN, fS, finfo, m_let = face_maps(rng)
    dA, dS, dE, dF, dinfo, fields = diffuser_maps(np.random.default_rng(19901002))
    eA, eN, eS, einfo = enamel_maps(np.random.default_rng(19880515))
    out = {"ExitSign_Face_A": fA, "ExitSign_Face_N": fN, "ExitSign_Face_S": fS,
           "ExitSign_Diffuser_A": dA, "ExitSign_Diffuser_S": dS, "ExitSign_Diffuser_E": dE, "ExitSign_Diffuser_F": dF,
           "ExitSign_Enamel_A": eA, "ExitSign_Enamel_N": eN, "ExitSign_Enamel_S": eS}
    for stem, img in out.items():
        save(img, os.path.join(a.out, stem + ".png"))
    report = {"face": finfo, "diffuser": dinfo, "enamel": einfo, "field": field_checks(fields)}
    if a.report_dir:
        os.makedirs(a.report_dir, exist_ok=True)
        review_sheet(os.path.join(a.report_dir, "exit_sign_textures_sheet.jpg"), fA, fS, dA, dE, dF, eA, m_let)
        with open(os.path.join(a.report_dir, "exit_sign_textures_report.json"), "w") as f:
            json.dump(report, f, indent=1)
    print(json.dumps(report["field"], indent=1))
    print(json.dumps({k: v for k, v in report.items() if k != "field"}, indent=1))


if __name__ == "__main__":
    main()
