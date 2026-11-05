"""Room families' surface detail maps (research/surface_detail/60_build.md section 3; line 游戏视觉, 2026-11-05).

  python gen_rooms_detail.py <out textures dir> [names...]     (numpy + Pillow only; no names = all)

FrontRooms/Surface's _FR_DETAIL path (kinds 9 painted block and 10 hard floor) reads these, all linear (the importer's default
for these stems), in the encoding of gen_office_detail.py:
  _DetailMap  R, G = the height's slope along u and v (0.5 + slope * SLOPE_K), B = gloss offset (0.5 + 0.5 x / 0.2),
              A = tone offset (0.5 + 0.5 x / 0.2). Tiled at the material's _DetailKind.y metres.
  _WearMap    what use leaves (here: the floor marks atlas). 0 = nothing.
Every map is periodic (FFT noise; strokes and arcs drawn across the wrap), rows are built bottom-up (row 0 = v 0) and flipped on
save, because Unity reads a PNG's bottom row as v 0. Real sizes (sources in SOURCES.md D48-D53):
  Rooms_BlockPores_D  512 x 512 over 0.40 m: the face of a lightweight concrete block (ASTM C90, open texture) under block filler and
                      two coats of semi-gloss: the aggregate's texture softened to a few tenths of a millimetre, pinholes 0.6-3 mm
                      where the filler bridged a void (darker, duller), a few bug holes.
  Rooms_FloorSwirl_D  1024 x 1024 over 1.20 m: a floor finish buffed with a 17 in (0.43 m) pad on a low-speed rotary machine swung
                      side to side: arcs of fine scratches round each place the pad stood (the sheen lower where they cross; their
                      density averages in the mips into the haze seen from far off), and the uneven buffing in soft patches.
  Rooms_FloorMarks_W  1024 x 1024, four quadrants of one 0.6 m field each (a margin keeps every mark off the field's edge; the
                      shader turns and mirrors them): R black rubber (heel strikes and drags, a cart's wheels turning, a mop
                      bucket's casters), G grey scuffs (the finish scraped: lighter and dull).
"""
import os
import sys

import numpy as np
from PIL import Image, ImageDraw

SLOPE_K = 4.0       # _DetailMap slope code: 0.5 + slope * SLOPE_K (FrontRoomsSurface.shader FR_DETAIL_K)
OUT = sys.argv[1] if len(sys.argv) > 1 else "."
os.makedirs(OUT, exist_ok=True)


# ------------------------------------------------------------------------------------------------ periodic helpers
def band(h, w, lo, hi, seed, stretch_v=1.0):
    """Periodic band-limited noise between lo and hi cycles per tile (0..1); stretch_v > 1 makes features taller than wide."""
    r = np.random.default_rng(seed)
    F = np.fft.fft2(r.standard_normal((h, w)))
    fy = np.fft.fftfreq(h)[:, None] * h * stretch_v
    fx = np.fft.fftfreq(w)[None, :] * w
    f = np.sqrt(fx * fx + fy * fy)
    m = np.exp(-((np.log(np.maximum(f, 1e-3)) - np.log((lo * hi) ** .5)) ** 2) / (2 * (np.log(hi / lo) / 2.5) ** 2))
    m[0, 0] = 0
    n = np.real(np.fft.ifft2(F * m))
    n -= n.min()
    n /= max(1e-8, n.max())
    return n.astype(np.float32)


def blur(img, sx, sy=None):
    """Periodic gaussian blur (sigma in texels; sy for rows)."""
    sy = sx if sy is None else sy
    h, w = img.shape
    fy = np.fft.fftfreq(h)[:, None]
    fx = np.fft.fftfreq(w)[None, :]
    g = np.exp(-2 * np.pi ** 2 * ((sx * fx) ** 2 + (sy * fy) ** 2))
    return np.real(np.fft.ifft2(np.fft.fft2(img) * g)).astype(np.float32)


def slopes(hm, texel):
    """Height (metres, rows = v up) -> (dh/du, dh/dv), periodic central differences."""
    du = (np.roll(hm, -1, 1) - np.roll(hm, 1, 1)) / (2 * texel)
    dv = (np.roll(hm, -1, 0) - np.roll(hm, 1, 0)) / (2 * texel)
    return du, dv


def save_rgba(name, r, g, b, a):
    arr = np.stack([r, g, b, a], -1)
    img = (np.clip(arr, 0, 1) * 255 + .5).astype(np.uint8)
    Image.fromarray(np.flipud(img), "RGBA").save(os.path.join(OUT, name + ".png"), optimize=True)
    print("wrote", name, img.shape[1], "x", img.shape[0], flush=True)


def detail_code(du, dv, gloss, tone):
    """The _DetailMap channels: slopes at SLOPE_K, gloss and tone offsets at 0.5 + 0.5 x / 0.2."""
    return (0.5 + du * SLOPE_K, 0.5 + dv * SLOPE_K, 0.5 + 0.5 * gloss / 0.2, 0.5 + 0.5 * tone / 0.2)


def wrapped_stamp(dst, src, cy, cx):
    """Adds src (odd size) into dst centred at (cy, cx) texels, wrapping round both edges."""
    h, w = dst.shape
    sh, sw = src.shape
    ys = (np.arange(sh) + int(round(cy)) - sh // 2) % h
    xs = (np.arange(sw) + int(round(cx)) - sw // 2) % w
    dst[np.ix_(ys, xs)] += src


# ------------------------------------------------------------------------------------------------ _DetailMap
def block_pores(name="Rooms_BlockPores_D"):
    N, span = 512, 0.40
    texel = span / N
    # the aggregate's open texture under filler and paint: 2-8 mm features ~0.12 mm, 0.8-2 mm ~0.04 mm
    agg = band(N, N, 50, 200, 9501) - 0.5
    fine = band(N, N, 200, 500, 9502) - 0.5
    h = 240e-6 * agg + 70e-6 * fine
    # pinholes where the filler bridged a void, and a few bug holes: round dimples, dark and dull inside
    r = np.random.default_rng(9503)
    pits = np.zeros((N, N), np.float32)
    core = np.zeros((N, N), np.float32)
    for _ in range(95):
        # most pinholes small, a third medium, one in ten a bug hole
        pick = r.random()
        rad_m = r.uniform(0.0003, 0.0008) if pick < 0.55 else (r.uniform(0.0008, 0.0016) if pick < 0.90 else r.uniform(0.0016, 0.0028))
        rad = rad_m / texel
        k = int(np.ceil(rad * 2.5)) * 2 + 1
        yy, xx = np.mgrid[0:k, 0:k].astype(np.float32) - k // 2
        dd = np.hypot(xx, yy) / max(rad, 0.4)
        depth = rad_m * r.uniform(0.5, 1.1)
        cy, cx = r.uniform(0, N), r.uniform(0, N)
        wrapped_stamp(pits, (np.clip(1.0 - dd * dd, 0, 1) * depth).astype(np.float32), cy, cx)
        wrapped_stamp(core, (np.clip(1.0 - dd, 0, 1) ** 0.8).astype(np.float32), cy, cx)
    core = np.clip(core, 0, 1)
    h = h - pits
    du, dv = slopes(h, texel)
    gloss = 0.04 * (agg + 0.0) - 0.16 * core           # the paint's peaks a touch glossier, the voids dull
    tone = -0.035 * np.clip(-agg, 0, 1) - 0.38 * core  # the voids hold shadow and grime
    save_rgba(name, *detail_code(du, dv, gloss, tone))
    s = np.hypot(du, dv)
    print("  block face slope rms %.2f deg, p95 %.2f deg; pores %.2f %% of the face" % (
        np.degrees(np.sqrt((s * s).mean())), np.degrees(np.percentile(s, 95)), 100.0 * (core > 0.3).mean()))


def floor_swirl(name="Rooms_FloorSwirl_D"):
    N, span, ss = 1024, 1.20, 3
    texel = span / N
    W = N * ss
    canvas = Image.new("F", (W, W), 0.0)
    dr = ImageDraw.Draw(canvas)
    r = np.random.default_rng(9601)
    pad = 0.215                                       # a 17 in pad's radius
    # the operator swings the machine side to side while walking it back: pad centres along a few wavy passes across the tile
    centres = []
    for p in range(7):
        y0 = (p + r.uniform(0.3, 0.7)) / 7.0 * span       # the passes spread evenly up the tile (no dense band to repeat)
        for i in range(14):
            x = (i / 14.0) * span + r.normal(0, 0.03)
            y = y0 + 0.16 * np.sin(i * 0.9 + p) + r.normal(0, 0.03)
            centres.append((x % span, y % span))
    for cx, cy in centres:
        for _ in range(46):
            rad = pad * np.sqrt(r.uniform(0.02, 1.0))
            a0 = r.uniform(0, 360)
            sweep = r.uniform(25, 160)
            val = float(r.uniform(0.12, 0.8))
            for ox in (-span, 0.0, span):
                for oy in (-span, 0.0, span):
                    x = (cx + ox) / span * W
                    y = (1.0 - (cy + oy) / span) * W
                    rr = rad / span * W
                    if x + rr < 0 or x - rr > W or y + rr < 0 or y - rr > W:
                        continue
                    dr.arc([x - rr, y - rr, x + rr, y + rr], a0, a0 + sweep, fill=val, width=1)
    a = np.asarray(canvas, np.float32)
    dens = np.flipud(a.reshape(N, ss, N, ss).mean(axis=(1, 3)))
    dens = np.clip(dens / max(1e-6, np.percentile(dens, 99.5)), 0, 1)
    patches = band(N, N, 1.2, 5, 9602) - 0.5          # buffed harder in places: 0.25-1 m patches
    gloss = -0.17 * dens + 0.07 * patches
    tone = 0.04 * dens                                 # scratched finish scatters a little more: a touch lighter
    # the scratches as a faint radial roughness: no slope worth coding at 1.2 mm texels (they are 10-50 um wide)
    z = np.zeros((N, N), np.float32)
    save_rgba(name, *detail_code(z, z, gloss, tone))
    print("  swirl density mean %.3f, gloss offset p5 %.3f p95 %.3f" % (dens.mean(), np.percentile(gloss, 5), np.percentile(gloss, 95)))


# ------------------------------------------------------------------------------------------------ _WearMap
class Canvas:
    """A supersampled greyscale canvas in metres (x along u, y along v up); no wrap (the quadrants keep a margin)."""

    def __init__(self, w_px, h_px, w_m, h_m, ss=3):
        self.ss = ss
        self.W, self.H = w_px * ss, h_px * ss
        self.w_m, self.h_m = w_m, h_m
        self.img = Image.new("F", (self.W, self.H), 0.0)
        self.dr = ImageDraw.Draw(self.img)

    def _px(self, x, y):
        return (x / self.w_m * self.W, (1.0 - y / self.h_m) * self.H)

    def line(self, pts, width_m, value):
        wpx = max(1, int(round(width_m / self.w_m * self.W)))
        self.dr.line([self._px(x, y) for x, y in pts], fill=float(value), width=wpx, joint="curve")

    def ellipse(self, cx, cy, rx, ry, value):
        x0, y0 = self._px(cx - rx, cy + ry)
        x1, y1 = self._px(cx + rx, cy - ry)
        self.dr.ellipse([x0, y0, x1, y1], fill=float(value))

    def array(self):
        a = np.asarray(self.img, np.float32)
        h, w = a.shape[0] // self.ss, a.shape[1] // self.ss
        return np.flipud(a.reshape(h, self.ss, w, self.ss).mean(axis=(1, 3)))


def _arc(cx, cy, length, ang, bend, n=10):
    pts = []
    for i in range(n):
        t = i / (n - 1) - 0.5
        a = ang + bend * t
        pts.append((cx + np.cos(a) * length * t, cy + np.sin(a) * length * t + bend * 0.15 * length * (t * t - 0.25)))
    return pts


def floor_marks(name="Rooms_FloorMarks_W"):
    Q, F = 512, 0.6                  # one quadrant = one 0.6 m field
    R = np.zeros((2 * Q, 2 * Q), np.float32)
    G = np.zeros((2 * Q, 2 * Q), np.float32)
    lo, hi = 0.07, F - 0.07          # every mark stays 7 cm off the field's edge

    def heel(c, r, x, y, scale=1.0):
        # a heel strike or a short drag: a dark comma, tapered, 2-7 cm, a lighter smear trailing it
        length = r.uniform(0.02, 0.07) * scale
        ang = r.uniform(0, 2 * np.pi)
        bend = r.normal(0, 0.8)
        width = r.uniform(0.002, 0.005)
        op = r.uniform(0.45, 1.0)
        pts = _arc(x, y, length, ang, bend)
        c.line(pts, width, op)
        c.line(pts[2:-2], width * 1.6, op * 0.6)

    def field(seed, kind):
        r = np.random.default_rng(seed)
        black = Canvas(Q, Q, F, F)
        grey = Canvas(Q, Q, F, F)
        if kind == 0:        # a place people stop and turn: a cluster of heel marks, two scuffs
            for _ in range(9):
                heel(black, r, r.uniform(0.18, 0.42), r.uniform(0.18, 0.42))
            for _ in range(2):
                pts = _arc(r.uniform(0.2, 0.4), r.uniform(0.2, 0.4), r.uniform(0.06, 0.14), r.uniform(0, 6.3), r.normal(0, 0.4))
                grey.line(pts, r.uniform(0.006, 0.014), r.uniform(0.35, 0.7))
        elif kind == 1:      # a sole dragged a long way (a skid), and a few strikes round it
            x, y = r.uniform(0.18, 0.25), r.uniform(0.2, 0.4)
            pts = _arc(x + 0.12, y, 0.24, r.uniform(-0.4, 0.4), r.normal(0, 0.25), n=16)
            black.line(pts, 0.004, 0.85)
            black.line(pts[3:-3], 0.007, 0.35)
            for _ in range(4):
                heel(black, r, r.uniform(0.15, 0.45), r.uniform(0.15, 0.45), 0.8)
        elif kind == 2:      # a cart turning: two wheels' arcs, thin and dark, a hand's width apart
            cx, cy = r.uniform(0.25, 0.35), r.uniform(0.25, 0.35)
            rad = r.uniform(0.18, 0.24)
            a0 = r.uniform(0, 2 * np.pi)
            for k, off in enumerate((0.0, 0.055)):
                pts = [(cx + (rad + off) * np.cos(a0 + t), cy + (rad + off) * np.sin(a0 + t)) for t in np.linspace(0, 0.9, 18)]
                pts = [(min(max(px_, lo), hi), min(max(py_, lo), hi)) for px_, py_ in pts]
                black.line(pts, 0.003, 0.7 - 0.2 * k)
            for _ in range(2):
                heel(black, r, r.uniform(0.2, 0.4), r.uniform(0.2, 0.4))
        else:                # the finish scraped: grey scuffs (a chair leg, a dropped box), a couple of small marks
            for _ in range(5):
                pts = _arc(r.uniform(0.18, 0.42), r.uniform(0.18, 0.42), r.uniform(0.04, 0.16), r.uniform(0, 6.3), r.normal(0, 0.5))
                grey.line(pts, r.uniform(0.004, 0.012), r.uniform(0.4, 0.85))
            for _ in range(3):
                heel(black, r, r.uniform(0.2, 0.4), r.uniform(0.2, 0.4), 0.7)
        b = blur(black.array(), 0.7)
        g = blur(grey.array(), 1.4)
        yy, xx = np.mgrid[0:Q, 0:Q]
        d = np.minimum.reduce([xx, yy, Q - 1 - xx, Q - 1 - yy]).astype(np.float32)
        fade = np.clip((d - 0.05 / F * Q) / 10.0, 0, 1)
        return np.clip(b, 0, 1) * fade, np.clip(g, 0, 1) * fade

    for k, (qx, qy) in enumerate(((0, 0), (1, 0), (0, 1), (1, 1))):
        b, g = field(9701 + k, k)
        R[qy * Q:(qy + 1) * Q, qx * Q:(qx + 1) * Q] = b
        G[qy * Q:(qy + 1) * Q, qx * Q:(qx + 1) * Q] = g
    z = np.zeros_like(R)
    save_rgba(name, R, G, z, z)
    print("  marks: black cover %.2f %%, grey cover %.2f %%" % (100 * (R > 0.2).mean(), 100 * (G > 0.2).mean()))


MAPS = {"Rooms_BlockPores_D": block_pores, "Rooms_FloorSwirl_D": floor_swirl, "Rooms_FloorMarks_W": floor_marks}

if __name__ == "__main__":
    names = sys.argv[2:] or list(MAPS)
    for n in names:
        MAPS[n]()
