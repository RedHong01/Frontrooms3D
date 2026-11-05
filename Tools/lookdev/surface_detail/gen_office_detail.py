"""Office surface detail maps (research/surface_detail/60_build.md section 2; line 游戏视觉, 2026-11-04).

  python gen_office_detail.py <out textures dir> [names...]     (numpy + Pillow only; no names = all)

FrontRooms/Surface's _FR_DETAIL path reads two maps per material, both linear (the importer's default for these stems):
  _DetailMap  fine relief: R, G = the height's slope along u and v (0.5 + slope * SLOPE_K), B = gloss offset (0.5 + 0.5 x / 0.2),
              A = tone offset (0.5 + 0.5 x / 0.2). Tiled at the material's _DetailKind.y metres.
  _WearMap    what use leaves, per kind (a strip along a wall's foot, a stain atlas, a desk's top). 0 = nothing.
Every map is periodic (FFT noise, strokes drawn three times across the wrap), rows are built bottom-up (row 0 = v 0, the floor for
the wall strip) and flipped on save, because Unity reads a PNG's bottom row as v 0. Real sizes (sources in SOURCES.md D41-D47):
  Office_PaintStipple_D  512 x 512 over 0.30 m: an eggshell latex rolled on with a 3/8 in nap: stipple 1-4 mm, peaks ~45 um, a few
                         popped bubbles; rolled up and down, so a little taller than wide.
  Office_Suede_D         512 x 512 over 0.08 m: high-pressure laminate in a suede finish (~10 GU at 60 deg): a fine stipple
                         0.2-0.7 mm, ~10 um deep.
  Office_WallBase_W      2048 x 256 over 4.8 x 0.6 m, v 0 = the floor: R black rubber (heel swipes, the vacuum's bumper at 0.10-0.16 m,
                         chair casters on the cove), G grey abrasion (scuffed vinyl and paint, lighter and dull). Marks come in
                         groups, where people stop and turn.
  Office_Stains_W        1024 x 1024, four quadrants of one 0.6 m tile each: R body, G tide line. 0 and 1 (v < 0.5): a coffee spill
                         on carpet (dark rim, satellite drops; a tracked smear); 2 and 3 (v > 0.5): water stains on a ceiling panel
                         (rings where each leak dried; one creeping in from an edge).
  Office_DeskWear_W      1024 x 1024 over 1.0 m: R scratches (most along the desk's length, where paper, keyboards and elbows
                         slide), G burnish (where forearms rest), B dust film (where nothing moves), A cup rings.
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


class Canvas:
    """A supersampled greyscale canvas in metres (x along u, y along v up), drawn three times across both wraps."""

    def __init__(self, w_px, h_px, w_m, h_m, ss=3):
        self.ss = ss
        self.W, self.H = w_px * ss, h_px * ss
        self.w_m, self.h_m = w_m, h_m
        self.img = Image.new("F", (self.W, self.H), 0.0)
        self.dr = ImageDraw.Draw(self.img)

    def _px(self, x, y):
        # PIL rows go down; our y goes up: flip here, so the array comes out bottom-up after a flipud
        return (x / self.w_m * self.W, (1.0 - y / self.h_m) * self.H)

    def line(self, pts, width_m, value, wrap_x=True, wrap_y=False):
        wpx = max(1, int(round(width_m / self.w_m * self.W)))
        for ox in ((-self.w_m, 0, self.w_m) if wrap_x else (0,)):
            for oy in ((-self.h_m, 0, self.h_m) if wrap_y else (0,)):
                p = [self._px(x + ox, y + oy) for x, y in pts]
                self.dr.line(p, fill=float(value), width=wpx, joint="curve")

    def ellipse(self, cx, cy, rx, ry, value, wrap_x=True, wrap_y=False):
        for ox in ((-self.w_m, 0, self.w_m) if wrap_x else (0,)):
            for oy in ((-self.h_m, 0, self.h_m) if wrap_y else (0,)):
                x0, y0 = self._px(cx - rx + ox, cy + ry + oy)
                x1, y1 = self._px(cx + rx + ox, cy - ry + oy)
                self.dr.ellipse([x0, y0, x1, y1], fill=float(value))

    def array(self, max_combine=True):
        """Downsampled, bottom-up (row 0 = v 0)."""
        a = np.asarray(self.img, np.float32)
        h, w = a.shape[0] // self.ss, a.shape[1] // self.ss
        a = a.reshape(h, self.ss, w, self.ss).mean(axis=(1, 3))
        return np.flipud(a)


def stamp_max(dst, src):
    np.maximum(dst, src, out=dst)


# ------------------------------------------------------------------------------------------------ _DetailMap
def paint_stipple(name="Office_PaintStipple_D"):
    N, span = 512, 0.30
    texel = span / N
    n1 = band(N, N, 70, 260, 9101, stretch_v=1.35)                   # 1.2-4.3 mm, rolled up and down
    p = np.clip((n1 - 0.47) / 0.30, 0, 1)
    p = p * p * (3 - 2 * p)                                          # rounded stipple peaks
    fine = band(N, N, 250, 520, 9102) - 0.5                          # 0.6-1.2 mm
    r = np.random.default_rng(9103)
    pits = blur((r.random((N, N)) > 0.9986).astype(np.float32), 0.9)
    pits = np.clip(pits / max(1e-6, pits.max()) * 1.6, 0, 1)         # popped bubbles 0.5-1 mm
    h_um = 85.0 * p + 12.0 * fine - 40.0 * pits
    du, dv = slopes(h_um * 1e-6, texel)
    gloss = 0.03 * (p - 0.5) - 0.06 * pits                           # peaks a touch glossier (touched), pits duller
    tone = -0.05 * pits
    save_rgba(name, *detail_code(du, dv, gloss, tone))
    s = np.hypot(du, dv)
    print("  stipple slope rms %.2f deg, p95 %.2f deg" % (np.degrees(np.sqrt((s * s).mean())), np.degrees(np.percentile(s, 95))))


def suede(name="Office_Suede_D"):
    N, span = 512, 0.08
    texel = span / N
    n = band(N, N, 120, 420, 9201) - 0.5                             # 0.19-0.67 mm
    fine = band(N, N, 420, 900, 9202) - 0.5
    h_um = 22.0 * n + 5.0 * fine
    du, dv = slopes(h_um * 1e-6, texel)
    gloss = 0.05 * n                                                 # the tops burnished by use
    tone = 0.0 * n
    save_rgba(name, *detail_code(du, dv, gloss, tone))
    s = np.hypot(du, dv)
    print("  suede slope rms %.2f deg" % np.degrees(np.sqrt((s * s).mean())))


# ------------------------------------------------------------------------------------------------ _WearMap
def wall_base(name="Office_WallBase_W"):
    W, H, wm, hm = 2048, 256, 4.8, 0.6
    r = np.random.default_rng(9301)
    black = Canvas(W, H, wm, hm)
    grey = Canvas(W, H, wm, hm)
    # where people stop and turn: a few busy stretches along the wall
    busy = band(1, 512, 2, 6, 9302)[0]

    def busy_at(x):
        return busy[int((x / wm) % 1.0 * 512) % 512]

    def arc(cx, cy, length, ang, bend, n=9):
        pts = []
        for i in range(n):
            t = i / (n - 1) - 0.5
            a = ang + bend * t
            pts.append((cx + np.cos(a) * length * t, cy + np.sin(a) * length * t + bend * 0.15 * length * (t * t - 0.25)))
        return pts

    placed = 0
    while placed < 30:
        x = r.random() * wm
        if r.random() > 0.25 + 0.75 * busy_at(x):
            continue
        placed += 1
        # heel swipes: on the cove and the wall above it, most at 0.06-0.2 m
        y = float(np.clip(r.normal(0.13, 0.06), 0.02, 0.30))
        length = r.uniform(0.018, 0.075)
        ang = np.radians(r.normal(0, 14))
        bend = r.normal(0, 0.6)
        width = r.uniform(0.0016, 0.0042)
        op = r.uniform(0.35, 0.95)
        pts = arc(x, y, length, ang, bend)
        # tapered: a core and two thinner tails
        black.line(pts, width, op)
        black.line(pts[1:-1], width * 1.5, op * 0.55)
    for _ in range(7):
        # the vacuum's bumper: long thin broken streaks just above the cove
        x = r.random() * wm
        y = r.uniform(0.104, 0.15)
        length = r.uniform(0.15, 0.45)
        segs = int(r.integers(2, 5))
        for s in range(segs):
            x0 = x + length * s / segs
            x1 = x0 + length / segs * r.uniform(0.4, 0.9)
            yy = y + r.normal(0, 0.002)
            black.line([(x0, yy), (x1, yy + r.normal(0, 0.0015))], r.uniform(0.0014, 0.0032), r.uniform(0.12, 0.32))
    for _ in range(14):
        # chair casters and toes on the cove: short dashes
        x = r.random() * wm
        if r.random() > 0.3 + 0.7 * busy_at(x):
            continue
        y = r.uniform(0.02, 0.085)
        length = r.uniform(0.01, 0.035)
        black.line([(x, y), (x + length, y + r.normal(0, 0.002))], r.uniform(0.0015, 0.003), r.uniform(0.3, 0.6))
    for _ in range(16):
        # grey abrasion: soft smudges on the cove and just above, where shoes rub
        x = r.random() * wm
        if r.random() > 0.3 + 0.7 * busy_at(x):
            continue
        y = float(np.clip(r.normal(0.06, 0.035), 0.01, 0.16))
        grey.ellipse(x, y, r.uniform(0.015, 0.06), r.uniform(0.005, 0.02), r.uniform(0.2, 0.5))
    R = np.clip(blur(black.array(), 0.6), 0, 1)
    G = np.clip(blur(grey.array(), 3.0, 1.6), 0, 1)
    z = np.zeros_like(R)
    save_rgba(name, R, G, z, z)
    print("  wall base: black cover %.3f %%, grey cover %.3f %%" % (100 * (R > 0.1).mean(), 100 * (G > 0.1).mean()))


def _polar_blob(N, cx, cy, r0, seed, rough=0.22, lobes=(3, 5, 9)):
    """A blob's signed distance (texels, negative inside) with a wobbly outline."""
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)
    dx, dy = xx - cx, yy - cy
    th = np.arctan2(dy, dx)
    rr = np.hypot(dx, dy)
    g = np.random.default_rng(seed)
    w = np.zeros_like(th)
    for k in lobes:
        w += g.uniform(0.3, 1.0) / k * np.cos(k * th + g.uniform(0, 2 * np.pi))
    return rr - r0 * (1.0 + rough * w)


def _angular(N, cx, cy, seed, ks=(2, 3, 5, 8, 13)):
    """A smooth 0..1 field around (cx, cy) that varies with the angle only (a line's strength along a ring)."""
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)
    th = np.arctan2(yy - cy, xx - cx)
    g = np.random.default_rng(seed)
    w = np.zeros_like(th)
    for k in ks:
        w += g.uniform(0.4, 1.0) / k ** 0.6 * np.cos(k * th + g.uniform(0, 2 * np.pi))
    w -= w.min()
    return w / max(1e-6, w.max())


def _tide(sd, width, strength):
    """A tide line on the dry side of an edge (sd 0): darkest just inside the edge, fading inward."""
    return np.exp(-((sd + 0.8 * width) / width) ** 2) * (sd < 0.6 * width) * strength


def stains(name="Office_Stains_W"):
    Q = 512                        # one quadrant = one 0.6 m tile
    texel = 0.6 / Q
    R = np.zeros((2 * Q, 2 * Q), np.float32)
    G = np.zeros((2 * Q, 2 * Q), np.float32)
    margin = 28

    def put(qx, qy, body, line):
        R[qy * Q:(qy + 1) * Q, qx * Q:(qx + 1) * Q] = np.clip(body, 0, 1)
        G[qy * Q:(qy + 1) * Q, qx * Q:(qx + 1) * Q] = np.clip(line, 0, 1)

    def fade_margin(a):
        yy, xx = np.mgrid[0:Q, 0:Q]
        d = np.minimum.reduce([xx, yy, Q - 1 - xx, Q - 1 - yy]).astype(np.float32)
        return a * np.clip((d - margin) / 12.0, 0, 1)

    # 0: a coffee spill on carpet: the pool, its darker rim (the coffee-ring effect), satellite drops thrown one way
    g = np.random.default_rng(9401)
    sd = _polar_blob(Q, Q * 0.45, Q * 0.48, 0.070 / texel, 9402)
    inside = np.clip(-sd / 3.0, 0, 1)
    rim = _tide(sd, 2.6, 1.0) * (0.45 + 0.55 * _angular(Q, Q * 0.45, Q * 0.48, 9404))
    body = inside * (0.42 + 0.18 * band(Q, Q, 3, 12, 9403))
    line = rim * 0.9
    ang = g.uniform(0, 2 * np.pi)
    for _ in range(11):
        dist = g.uniform(0.085, 0.19) / texel
        a = ang + g.normal(0, 0.35)
        cx, cy = Q * 0.45 + np.cos(a) * dist, Q * 0.48 + np.sin(a) * dist
        rad = g.uniform(0.003, 0.011) / texel
        dd = _polar_blob(Q, cx, cy, rad, int(g.integers(1 << 30)), rough=0.15)
        body = np.maximum(body, np.clip(-dd / 1.5, 0, 1) * 0.45)
        line = np.maximum(line, np.exp(-((dd + 1.2) / 1.2) ** 2) * (dd < 1.0) * 0.7)
    put(0, 0, fade_margin(blur(body, 0.8)), fade_margin(blur(line, 0.6)))

    # 1: a smaller spill and the coffee carried off by a shoe: two faint heel-and-toe smears
    g = np.random.default_rng(9411)
    sd = _polar_blob(Q, Q * 0.38, Q * 0.40, 0.042 / texel, 9412)
    body = np.clip(-sd / 3.0, 0, 1) * 0.5
    line = np.exp(-((sd + 3.0) / 2.4) ** 2) * (sd < 1.5) * 0.85
    yy, xx = np.mgrid[0:Q, 0:Q].astype(np.float32)
    for k, (fx, fy) in enumerate(((0.58, 0.55), (0.70, 0.70))):
        cx, cy = Q * fx, Q * fy
        a = np.radians(35)
        u = ((xx - cx) * np.cos(a) + (yy - cy) * np.sin(a)) / (0.045 / texel)
        v = (-(xx - cx) * np.sin(a) + (yy - cy) * np.cos(a)) / (0.02 / texel)
        toe = np.exp(-(u * u + v * v)) * 0.22 / (1 + k)
        body = np.maximum(body, toe * (0.6 + 0.4 * band(Q, Q, 20, 60, 9413 + k)))
    put(1, 0, fade_margin(blur(body, 0.8)), fade_margin(blur(line, 0.6)))

    # 2: a leak through the slab above, dried in several rings (each one a tide line where the wet edge stopped once): every
    # ring its own centre and wobble, the line stronger in some places than others and broken here and there; the outermost
    # (the oldest and largest wetting) the darkest; the body yellowed, more toward the middle
    g = np.random.default_rng(9421)
    body = np.zeros((Q, Q), np.float32)
    line = np.zeros((Q, Q), np.float32)
    radii = (0.045, 0.085, 0.13, 0.20)
    for i, r0 in enumerate(radii):
        cx, cy = Q * 0.52 + g.normal(0, 9), Q * 0.47 + g.normal(0, 9)
        sd = _polar_blob(Q, cx, cy, r0 / texel, 9430 + i, rough=0.30, lobes=(2, 3, 5, 7, 11))
        along = _angular(Q, cx, cy, 9450 + i)
        broken = np.clip((along - 0.18) / 0.25, 0, 1)
        outer = i == len(radii) - 1
        body = np.maximum(body, np.clip(-sd / 10.0, 0, 1) * (0.62 - 0.11 * i))
        w = (1.6 + 1.2 * g.random()) * (1.5 if outer else 1.0)
        line = np.maximum(line, _tide(sd, w, (1.0 if outer else 0.55 + 0.25 * g.random())) * (0.35 + 0.65 * along) * broken)
    body *= 0.7 + 0.3 * band(Q, Q, 4, 16, 9439)
    put(0, 1, fade_margin(blur(body, 1.2)), fade_margin(blur(line, 0.8)))

    # 3: water that crept in along the grid from one edge (v high here; the shader turns it by 90 degree steps)
    body = np.zeros((Q, Q), np.float32)
    line = np.zeros((Q, Q), np.float32)
    g = np.random.default_rng(9441)
    for i, r0 in enumerate((0.07, 0.13, 0.21)):
        cx, cy = Q * 0.46 + g.normal(0, 14), Q - margin - 2
        sd = _polar_blob(Q, cx, cy, r0 / texel, 9440 + i, rough=0.32, lobes=(2, 3, 5, 7, 11))
        along = _angular(Q, cx, cy, 9460 + i)
        outer = i == 2
        body = np.maximum(body, np.clip(-sd / 10.0, 0, 1) * (0.66 - 0.14 * i))
        w = (1.6 + 1.0 * g.random()) * (1.5 if outer else 1.0)
        line = np.maximum(line, _tide(sd, w, 1.0 if outer else 0.6) * (0.35 + 0.65 * along))
    body *= 0.7 + 0.3 * band(Q, Q, 4, 16, 9449)
    put(1, 1, fade_margin(blur(body, 1.2)), fade_margin(blur(line, 0.8)))
    z = np.zeros_like(R)
    save_rgba(name, R, G, z, z)


def desk_wear(name="Office_DeskWear_W"):
    N, span = 1024, 1.0
    r = np.random.default_rng(9501)
    scr = Canvas(N, N, span, span, ss=3)
    work = band(64, 64, 1, 3, 9502)                                   # where the work happens: more scratches there

    def work_at(x, y):
        return work[int((y % 1.0) * 64) % 64, int((x % 1.0) * 64) % 64]

    n = 0
    while n < 320:
        x, y = r.random() * span, r.random() * span
        if r.random() > 0.35 + 0.65 * work_at(x, y):
            continue
        n += 1
        length = float(np.clip(r.lognormal(np.log(0.045), 0.6), 0.012, 0.26))
        ang = np.radians(r.normal(0, 16)) if r.random() < 0.65 else r.uniform(0, np.pi)
        bend = r.normal(0, 0.08)
        pts = []
        for i in range(7):
            t = i / 6 - 0.5
            a = ang + bend * t
            pts.append((x + np.cos(a) * length * t, y + np.sin(a) * length * t))
        op = r.uniform(0.15, 0.75)
        # broken scratches: drawn in two or three pieces
        cut = sorted(r.choice(range(1, 6), size=int(r.integers(0, 3)), replace=False))
        pieces, last = [], 0
        for c in list(cut) + [6]:
            pieces.append(pts[last:c + 1])
            last = c + 1
        for pc in pieces:
            if len(pc) >= 2:
                scr.line(pc, 0.0007, op, wrap_x=True, wrap_y=True)
    R = np.clip(scr.array(), 0, 1)
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32) / N
    G = np.zeros((N, N), np.float32)
    for _ in range(4):
        cx, cy = r.random(), r.random()
        rx, ry = r.uniform(0.09, 0.2), r.uniform(0.06, 0.13)
        for ox in (-1, 0, 1):
            for oy in (-1, 0, 1):
                G = np.maximum(G, np.exp(-(((xx - cx - ox) / rx) ** 2 + ((yy - cy - oy) / ry) ** 2)) * r.uniform(0.6, 1.0))
    G *= 0.8 + 0.2 * band(N, N, 6, 24, 9503)
    B = np.clip((band(N, N, 2, 7, 9504) - 0.35) / 0.5, 0, 1) ** 1.5
    B = np.clip(B * (1 - 0.9 * G), 0, 1) * 0.85
    rings = Canvas(N, N, span, span, ss=3)
    for k in range(4):
        cx, cy = r.random() * span, r.random() * span
        rad = r.uniform(0.037, 0.043)
        cover = r.uniform(0.5, 1.0)
        a0 = r.uniform(0, 2 * np.pi)
        seg = [(cx + np.cos(a0 + 2 * np.pi * cover * i / 40) * rad, cy + np.sin(a0 + 2 * np.pi * cover * i / 40) * rad) for i in range(41)]
        rings.line(seg, r.uniform(0.002, 0.0038), r.uniform(0.35, 0.8), wrap_x=True, wrap_y=True)
        if k == 0:
            seg2 = [(x + 0.012, y - 0.008) for x, y in seg]
            rings.line(seg2, 0.0025, 0.4, wrap_x=True, wrap_y=True)
    A = np.clip(blur(rings.array(), 0.7), 0, 1)
    save_rgba(name, R, G, B, A)


ALL = dict(paint_stipple=paint_stipple, suede=suede, wall_base=wall_base, stains=stains, desk_wear=desk_wear)

if __name__ == "__main__":
    for nm in (sys.argv[2:] or ALL.keys()):
        ALL[nm]()
