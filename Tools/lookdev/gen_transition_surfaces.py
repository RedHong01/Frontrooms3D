"""FrontRooms level transitions, V2 Renovation: surface sets for the work in progress.

Run: /usr/bin/python3 gen_transition_surfaces.py <outdir> <cc0 dir> <existing textures dir> [names...]

Same conventions as gen_surfaces.py (gen_common.py): every map is seamless,
images are in texture orientation (row 0 = top = v 1), outputs are
<name>_A.png (sRGB albedo), <name>_N.png (OpenGL +Y normal), <name>_S.png
(R smoothness, G cavity, B wet 0, A 1). World tile sizes (set in the Unity
material, FrontRoomsTransitionAssets.cs) divide 192 m:

  Wall_Primer      2.4 x 2.4 m  primer over taped drywall: sheet joints every 1.2 m,
                                screw spots every 0.30 m on studs every 0.40 m
  Wall_Stripped    2.4 x 2.4 m  drywall after the paper came off: backing remnants,
                                torn face paper, amber paste, scraper gouges
  Wall_FreshPaint  1.2 x 1.2 m  Office_Drywall + 4 % value, roller laps every 0.24 m
  Wall_CutIn       1.2 x 1.2 m  the brushed 0.05 m band at ceilings and corners
  Wall_Compound    1.2 x 1.2 m  bare joint compound (door halos, bucket)
  Slab_Adhesive    2.4 x 2.4 m  slab under pulled glue-down carpet: notched-trowel ridges
  Paper_Backing    0.6 x 0.6 m  the white base layer / back of the Level 0 paper
  Prop_Canvas      1.0 x 1.0 m  drop cloth: rough linen (CC0) with paint spatter
  Prop_TapeBeige   0.3 x 0.3 m  crepe masking tape (CC0 Tape005)
  Prop_RollerNap   0.3 x 0.3 m  roller cover nap, paint-loaded

CC0 inputs (ambientCG, Poly Haven; on disk in Tools/lookdev/cc0_src): Smear007 (knife and
trowel strokes), Leaking001/006 (runs), SurfaceImperfections001/007/013/015, Tape005,
rough_linen, CardboardSet001 (box, not used here).
"""
import os, sys
import numpy as np
from PIL import Image

OUT, CC0, TEX = sys.argv[1], sys.argv[2], sys.argv[3]
NAMES = sys.argv[4:]
sys.argv = [sys.argv[0], OUT, ""]
exec(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "gen_common.py")).read())
os.makedirs(OUT, exist_ok=True)


def save_rgb(path, rgb):
    Image.fromarray((np.clip(rgb, 0, 1) * 255 + .5).astype(np.uint8), "RGB").save(path, compress_level=6)


def save_rgba(path, rgba):
    Image.fromarray((np.clip(rgba, 0, 1) * 255 + .5).astype(np.uint8), "RGBA").save(path, compress_level=6)


def save_normal(path, n):
    save_rgb(path, n * .5 + .5)


def resize(arr, w, h):
    """Float resize (PIL mode F for 2-D fields, 8-bit RGB otherwise)."""
    if arr.ndim == 2:
        return np.asarray(Image.fromarray(arr.astype(np.float32), "F").resize((w, h), Image.BICUBIC)).astype(np.float32)
    return np.asarray(Image.fromarray((np.clip(arr, 0, 1) * 255).astype(np.uint8)).resize((w, h), Image.LANCZOS)).astype(np.float32) / 255


def mask(smooth, cavity):
    z = np.zeros_like(smooth)
    return np.stack([np.clip(smooth, 0, 1), np.clip(cavity, 0, 1), z, np.ones_like(smooth)], -1)


def write(name, col, n, s):
    save_rgb(f"{OUT}/{name}_A.png", col)
    save_normal(f"{OUT}/{name}_N.png", n)
    save_rgba(f"{OUT}/{name}_S.png", s)
    print("wrote", name, col.shape, flush=True)


def load_gray(path, N):
    im = Image.open(path).convert("L").resize((N, N), Image.LANCZOS)
    return np.asarray(im).astype(np.float32) / 255


def load_rgb(path, N):
    im = Image.open(path).convert("RGB").resize((N, N), Image.LANCZOS)
    return np.asarray(im).astype(np.float32) / 255


def cc0(name, kind="Color"):
    d = os.path.join(CC0, name)
    for f in os.listdir(d):
        if f.endswith(kind + ".jpg"):
            return os.path.join(d, f)
    raise FileNotFoundError(name + " " + kind)


def wrapd(a, N):
    """Periodic distance along one axis (pixels)."""
    a = np.abs(a) % N
    return np.minimum(a, N - a)


def stamp_ellipse(field, cx, cy, rx, ry, ang, value_fn, N, mode="max"):
    """Stamp a soft ellipse (periodic) into field. value_fn(d) gives the value for normalised radius d."""
    R = int(max(rx, ry) * 1.6) + 3
    ys = np.arange(int(cy) - R, int(cy) + R + 1)
    xs = np.arange(int(cx) - R, int(cx) + R + 1)
    Y, X = np.meshgrid(ys, xs, indexing="ij")
    dx, dy = X - cx, Y - cy
    c, s = np.cos(ang), np.sin(ang)
    u = (dx * c + dy * s) / rx
    v = (-dx * s + dy * c) / ry
    d = np.sqrt(u * u + v * v)
    val = value_fn(d, u, v)
    yi, xi = Y % N, X % N
    if mode == "max":
        np.maximum.at(field, (yi, xi), val)
    else:
        np.add.at(field, (yi, xi), val)


def fbm_edge(N, seed, lo, hi):
    return band(N, N, lo, hi, seed) * 2 - 1


def rows_y(N, T):
    """World height (m above the tile's v=0) of every image row."""
    return (T * (1.0 - (np.arange(N) + .5) / N)).astype(np.float32)


# ------------------------------------------------------------------ drywall joints (shared)
def joints(N, T, seed, sheet=1.2, stud=0.4, screw=0.30, width=0.15):
    """Masks for taped joints and spotted screws on drywall hung vertically.
    Returns (compound, tape, tape_edge, screw_spot, screw_ring, screw_dimple)."""
    ppm = N / T
    r = np.random.default_rng(seed)
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)
    wob = fbm_edge(N, seed + 1, 3, 14)                      # knife-edge wander
    comp = np.zeros((N, N), np.float32)
    tape = np.zeros((N, N), np.float32)
    tedge = np.zeros((N, N), np.float32)
    for jx in np.arange(0, T, sheet):
        d = wrapd(xx - jx * ppm, N) / ppm                    # metres from the joint
        w = width * (1 + .18 * wob)                          # feathered width varies
        comp = np.maximum(comp, np.clip(1 - (d / w) ** 2.2, 0, 1))
        tape = np.maximum(tape, (d < .026).astype(np.float32))
        tedge = np.maximum(tedge, np.exp(-((d - .026) / .0014) ** 2))
    spot = np.zeros((N, N), np.float32)
    ring = np.zeros((N, N), np.float32)
    dimple = np.zeros((N, N), np.float32)
    for sx in np.arange(0, T, stud):
        if any(abs(sx - jx) < 1e-6 for jx in np.arange(0, T, sheet)):
            continue                                          # sheet-edge screws are under the joint
        for sy in np.arange(screw * .5, T, screw):
            cx = (sx + r.normal(0, .006)) * ppm
            cy = (T - (sy + r.normal(0, .012))) * ppm
            rx, ry = r.uniform(.020, .028) * ppm, r.uniform(.013, .018) * ppm
            ang = r.uniform(-.6, .6) + (np.pi / 2 if r.random() < .5 else 0)
            stamp_ellipse(spot, cx, cy, rx, ry, ang, lambda d, u, v: np.clip(1.15 - d, 0, 1) ** .7, N)
            stamp_ellipse(ring, cx, cy, rx, ry, ang, lambda d, u, v: np.exp(-((d - 1.0) / .12) ** 2), N)
            stamp_ellipse(dimple, cx, cy, .0045 * ppm, .0045 * ppm, 0, lambda d, u, v: np.clip(1 - d, 0, 1) ** 2, N)
    return comp, tape, tedge, np.clip(spot, 0, 1), np.clip(ring, 0, 1), np.clip(dimple, 0, 1)


# ------------------------------------------------------------------ primer
def primer():
    N, T = 2048, 2.4
    comp, tape, tedge, spot, ring, dimple = joints(N, T, 401)
    stip = band(N, N, 260, 720, 402)                         # roller stipple 3-9 mm
    holiday = resize(spectral(N, N // 4, 1.3, 403), N, N)    # thin vertical streaks of one coat
    mott = spectral(N, N, 1.8, 404)
    sand = resize(band(N * 3, N, 400, 1400, 405), N, N)       # sanding scratches on compound, mostly vertical
    paper = hexc("#D9D3C4")                                   # primer over face paper (absorbs, a touch grey)
    over = hexc("#E9E5DA")                                    # primer over compound (whiter, flatter)
    # Iteration 3: screw spots are two thin coats under one coat of primer: a faint, slightly
    # whiter flashing with a soft knife ridge, never a white dot.
    compound = np.clip(np.maximum(comp, spot * .22), 0, 1)
    col = paper[None, None, :] * (1 - compound[..., None]) + over[None, None, :] * compound[..., None]
    col *= (0.985 + .02 * stip[..., None]) * (0.99 + .025 * (holiday[..., None] - .5)) * (0.985 + .03 * mott[..., None])
    col -= .008 * ring[..., None] * (1 - comp[..., None])     # the knife ridge round each screw spot
    col -= .015 * tedge[..., None]
    height = .25 * stip + .9 * blur(comp, 3) * .4 + .35 * tape + .5 * tedge + .10 * ring - .25 * dimple + .08 * sand * comp
    n = normal_from_height(blur(height, .6), 1.4)
    smooth = .17 - .05 * np.clip(np.maximum(comp, spot * .6), 0, 1) + .02 * stip   # flashing: compound reads flatter
    cav = .92 + .08 * stip - .2 * dimple - .1 * tedge
    write("Wall_Primer", col, n, mask(smooth, cav))


# ------------------------------------------------------------------ stripped (iteration 2)
def strokes_field(N, T, seed, worked, n_strokes=150):
    """Scraper passes: capsules 0.08-0.14 m wide (a 4-5 in. blade), 0.2-0.9 m long, laid in
    runs inside the worked area, mostly vertical, some diagonal. Returns (removed 0..1,
    paste streak 0..1, dig 0..1). Drawn on a padded canvas, folded so the tile wraps."""
    ppm = N / T
    r = np.random.default_rng(seed)
    P = int(.55 * ppm) + 40
    M = N + 2 * P
    removed = np.zeros((M, M), np.float32)
    streak = np.zeros((M, M), np.float32)
    dig = np.zeros((M, M), np.float32)
    ang_field = band(N, N, 1, 3, seed + 1)        # slowly varying pass direction
    cand = np.argwhere(worked > .5)
    for k in range(n_strokes):
        if len(cand) == 0:
            break
        cy, cx = cand[r.integers(len(cand))].astype(np.float32)
        a = np.pi / 2 + (ang_field[int(cy), int(cx)] - .5) * 1.4 + r.normal(0, .06)
        L = r.uniform(.2, .9) * ppm
        w = r.uniform(.08, .14) * ppm
        ca, sa = np.cos(a), np.sin(a)
        ex = int(abs(ca) * L / 2 + abs(sa) * w * .75) + 6
        ey = int(abs(sa) * L / 2 + abs(ca) * w * .75) + 6
        y0, x0 = int(cy) - ey + P, int(cx) - ex + P
        Y, X = np.mgrid[y0:y0 + 2 * ey + 1, x0:x0 + 2 * ex + 1].astype(np.float32)
        dx, dy = X - (cx + P), Y - (cy + P)
        s = dx * ca + dy * sa
        t = -dx * sa + dy * ca
        q = (s + L / 2) / L
        rag = .08 * np.sin(q * r.uniform(7, 16) + r.uniform(0, 6)) + .05 * np.sin(q * r.uniform(30, 60) + r.uniform(0, 6))
        side = 1 - np.clip((np.abs(t) - w / 2 * (1 + rag)) / 2.0, 0, 1)
        lk = np.cumsum(r.uniform(3, 14, 60)) - w
        lift = (1 - q) * L / (4 + 22 * np.interp(t, lk, r.random(60)))   # ragged where the blade lifted
        m = (np.clip(side * np.clip(q * L / 2.5, 0, 1) * np.clip(lift, 0, 1), 0, 1) * ((q > 0) & (q < 1))).astype(np.float32)
        sl = (slice(y0, y0 + 2 * ey + 1), slice(x0, x0 + 2 * ex + 1))
        np.maximum(removed[sl], m, out=removed[sl])
        # Paste dragged along the pass: irregular streaks parallel to it (random knots across the blade).
        knots = np.cumsum(r.uniform(1.5, 7, 80)) - w
        vals = .25 + .75 * r.random(80) ** 2
        st = np.interp(t, knots, vals).astype(np.float32)
        np.maximum(streak[sl], m * st * (.35 + .65 * q), out=streak[sl])
        # A blade corner dug through the face paper where it was set down: a torn sliver.
        if r.random() < .12:
            off = r.uniform(-.3, .3) * w
            dl, dw = r.uniform(.03, .10) * ppm, r.uniform(.012, .035) * ppm
            dq = (s + L / 2 - dl * .6) / dl
            jag = 1 + .35 * np.sin(dq * 9 + k) + .2 * np.sin(dq * 23 + 2 * k)
            dg = np.clip(1.6 - np.sqrt(dq ** 2 + ((t - off) / (dw * .5 * jag)) ** 2) * 1.6, 0, 1)
            np.maximum(dig[sl], dg.astype(np.float32), out=dig[sl])
    def fold(a):
        out = a[P:P + N, P:P + N].copy()
        for oy in (-1, 0, 1):
            for ox in (-1, 0, 1):
                if oy == 0 and ox == 0:
                    continue
                yy0 = max(0, P + oy * N); yy1 = min(M, P + oy * N + N)
                xx0 = max(0, P + ox * N); xx1 = min(M, P + ox * N + N)
                if yy1 <= yy0 or xx1 <= xx0:
                    continue
                sub = a[yy0:yy1, xx0:xx1]
                ty0 = yy0 - (P + oy * N); tx0 = xx0 - (P + ox * N)
                tgt = out[ty0:ty0 + sub.shape[0], tx0:tx0 + sub.shape[1]]
                np.maximum(tgt, sub, out=tgt)
        return out
    return np.clip(fold(removed), 0, 1), np.clip(fold(streak), 0, 1), np.clip(fold(dig), 0, 1)


def stripped():
    N, T = int(os.environ.get("TEXN", 2048)), 2.4
    ppm = N / T
    r = np.random.default_rng(501)
    comp, tape_m, tedge, spot, ring, dimple = joints(N, T, 401)  # same sheets and screws as the primer
    fib = band(N, N, N // 4, N * 2 // 3, 502)
    mott = spectral(N, N, 1.7, 503)
    # 1. Drywall face paper (ivory-grey); the mudded joints and screw spots faintly whiter.
    face = hexc("#CFC9BA")[None, None, :] * (0.965 + .05 * mott[..., None]) * (0.985 + .03 * fib[..., None])
    jc = np.clip(np.maximum(comp * .5, spot * .45), 0, 1)
    face = face * (1 - jc[..., None]) + hexc("#DCD7CB")[None, None, :] * jc[..., None]
    # 2. The worked area (about 75 %) and the scraper passes in it.
    wf = band(N, N, 1.5, 5, 504)
    worked = (wf > np.quantile(wf, .16)).astype(np.float32)
    removed, streak, dig = strokes_field(N, T, 505, worked)
    tear_n = band(N, N, N // 12, N // 2, 506) - .5           # torn fibre edge, 2-15 mm
    keep = np.clip((.5 - removed + .45 * tear_n) / .06 + .5, 0, 1)    # backing still on the wall
    backing = np.clip((blur(keep, .5) - .5) / .3 + .5, 0, 1)
    # The torn rim, on the remnant's side: 1.5-3 mm of the backing's white core.
    rim = np.clip(1 - np.abs(blur(backing, 2.2 * N / 2048) - .62) / .38, 0, 1) ** 2 * np.clip(backing * 1.5, 0, 1)
    # 3. Backing remnants: grey-brown, darker where still damp from the soak.
    soak = band(N, N, 4, 14, 508)
    damp = np.clip((soak - .55) / .25, 0, 1)
    bfib = band(N, N, N // 10, N // 3, 509)
    bcol = hexc("#B9AD97")[None, None, :] * (0.93 + .10 * bfib[..., None]) * (0.95 + .07 * mott[..., None])
    bcol = bcol * (1 - .07 * damp[..., None])
    col = face * (1 - backing[..., None]) + bcol * backing[..., None]
    col = col * (1 - .5 * rim[..., None]) + hexc("#E6E1D4") * .5 * rim[..., None]   # white core at the torn rim
    height = .2 * fib + .5 * blur(backing, .8) + .25 * rim
    # 4. Paste on the scraped face paper: amber and glossy along the passes, a few runs.
    paste = np.clip(streak * .9, 0, 1) * (1 - backing)
    runs = np.clip(load_gray(cc0("Leaking001", "Opacity"), N) * 1.1, 0, 1) * np.clip((band(N, N, 2, 7, 511) - .5) / .25, 0, 1) * (1 - backing)
    paste = np.clip(paste * .8 + .3 * runs, 0, 1)
    col = col * (1 - .17 * paste[..., None]) + hexc("#B08C55") * .17 * paste[..., None]
    height += .05 * paste
    # 5. Blade digs: the face paper torn to the brown under-ply, fuzzy white rim.
    digm = np.clip((dig - .2) / .25, 0, 1) * (1 - backing)
    digm = np.clip(blur(digm, .6 * N / 2048) * 1.2, 0, 1)
    digrim = np.clip(1 - np.abs(blur(digm, 1.4 * N / 2048) - .5) / .5, 0, 1) ** 2
    col = col * (1 - .9 * digm[..., None]) + hexc("#A88B66")[None, None, :] * (0.9 + .18 * fib[..., None]) * .9 * digm[..., None]
    col = col * (1 - .3 * digrim[..., None]) + hexc("#E2DCCD") * .3 * digrim[..., None]
    height -= .5 * digm
    # 6. Sharp gouges from the blade corner along the passes (gypsum shows white).
    gouge = np.zeros((N, N), np.float32)
    for _ in range(22):
        cx, cy = r.random() * N, r.random() * N
        if removed[int(cy) % N, int(cx) % N] < .5:
            continue
        L = r.uniform(.02, .06) * ppm
        W = max(1.2, r.uniform(.0015, .003) * ppm)
        ang = np.pi / 2 + r.normal(0, .35)
        stamp_ellipse(gouge, cx, cy, L, W, ang, lambda d, u, v: np.clip(1.25 - d, 0, 1) ** .5, N)
    gouge = np.where(gouge > .4, gouge, 0)
    col = col * (1 - .6 * gouge[..., None]) + hexc("#E7E3DA") * .6 * gouge[..., None]
    height -= .9 * gouge
    grime = load_gray(cc0("SurfaceImperfections013", "Color"), N)
    col *= (1 - .05 * grime[..., None])
    col = np.clip(col, 0, 1)
    n = normal_from_height(blur(height, .7 * N / 2048), 1.6 * 2048 / N)
    smooth = .10 + .30 * paste + .03 * jc - .05 * digm - .03 * backing
    cav = .93 + .07 * fib - .3 * gouge - .15 * digm - .06 * rim
    write("Wall_Stripped", col, n, mask(smooth, cav))
    print("backing fraction", float(backing.mean()), "paste", float(paste.mean()), "dig", float(digm.mean()))


# ------------------------------------------------------------------ fresh office paint
def fresh():
    N, T = 2048, 1.2
    a = np.asarray(Image.open(os.path.join(TEX, "Office_Drywall_A.png")).convert("RGB").resize((N, N), Image.BICUBIC)).astype(np.float32) / 255
    nm = np.asarray(Image.open(os.path.join(TEX, "Office_Drywall_N.png")).convert("RGB").resize((N, N), Image.BICUBIC)).astype(np.float32) / 255 * 2 - 1
    s = np.asarray(Image.open(os.path.join(TEX, "Office_Drywall_S.png")).convert("RGBA").resize((N, N), Image.BICUBIC)).astype(np.float32) / 255
    ppm = N / T
    xx = np.arange(N, dtype=np.float32)[None, :].repeat(N, 0)
    lap = .24                                                 # 9-inch roller, five laps per 1.2 m
    wob = fbm_edge(N, 601, 4, 20) * .004
    u = (xx / ppm + wob) % lap
    tram = np.exp(-((u - .003) / .0016) ** 2) + np.exp(-((u - lap + .003) / .0016) ** 2)
    tram *= np.clip((band(N, N, 3, 16, 602) - .35) / .3, 0, 1)   # tram lines come and go
    lap_id = np.floor((xx / ppm + wob) / lap).astype(int) % 5
    lap_tone = np.array([0, .006, -.004, .008, -.006], np.float32)[lap_id]
    stip = band(N, N * 2, 220, 600, 603); stip = resize(stip, N, N)   # roller stipple, slightly vertical
    col = a * 1.04 * (1 + lap_tone[..., None]) * (0.992 + .016 * stip[..., None])
    col = np.clip(col, 0, 1)
    # Normal: drywall knockdown under a fresh film, plus laps and stipple.
    h = .5 * tram + .22 * stip
    n2 = normal_from_height(blur(h, .6), 1.2)
    nn = nm * np.array([1, 1, 1], np.float32) + (n2 - np.array([0, 0, 1], np.float32)) * 1.0
    nn /= np.linalg.norm(nn, axis=-1, keepdims=True)
    smooth = np.clip(s[..., 0] * 1.0 + .07 + .02 * stip, 0, 1)
    cav = s[..., 1]
    write("Wall_FreshPaint", col, nn, mask(smooth, cav))


def cutin():
    N, T = 1024, 1.2
    a = np.asarray(Image.open(os.path.join(TEX, "Office_Drywall_A.png")).convert("RGB")).astype(np.float32) / 255
    nm = np.asarray(Image.open(os.path.join(TEX, "Office_Drywall_N.png")).convert("RGB")).astype(np.float32) / 255 * 2 - 1
    brush = band(N // 8, N, 6, 300, 611); brush = resize(brush, N, N)   # fine strokes along the band
    col = np.clip(a * 1.05 * (0.995 + .012 * brush[..., None]), 0, 1)
    flat = np.array([0, 0, 1], np.float32)
    n2 = normal_from_height(blur(brush, .5), .9)
    nn = (nm * .45 + flat * .55) + (n2 - flat)                # the brush fills the knockdown more
    nn /= np.linalg.norm(nn, axis=-1, keepdims=True)
    smooth = np.full((N, N), .40, np.float32) + .03 * brush
    write("Wall_CutIn", col, nn, mask(smooth, np.full((N, N), .97, np.float32)))


def compound():
    N, T = 1024, 1.2
    sm = load_gray(cc0("Smear007", "Opacity"), N)
    sh = load_gray(cc0("Smear007", "Color"), N)
    mott = spectral(N, N, 1.7, 621)
    wet = np.clip((band(N, N, 2, 9, 622) - .5) / .3, 0, 1)    # slower-drying, greyer areas
    col = hexc("#E5E2DA")[None, None, :] * (0.975 + .04 * mott[..., None]) * (1 - .06 * wet[..., None])
    col = col * (1 - .05 * sm[..., None]) + hexc("#F2F0EA") * .05 * sm[..., None]
    pits = load_gray(cc0("SurfaceImperfections007", "Color"), N)
    col *= (1 - .08 * pits[..., None])
    height = .8 * blur(sh, 1.0) + .3 * band(N, N, 200, 600, 623) - .6 * pits
    n = normal_from_height(blur(height, .6), 1.5)
    smooth = .14 + .05 * sm - .03 * wet
    write("Wall_Compound", np.clip(col, 0, 1), n, mask(smooth, .95 - .2 * pits))


# ------------------------------------------------------------------ slab
def slab():
    N, T = 2048, 2.4
    ppm = N / T
    r = np.random.default_rng(701)
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)
    # Concrete: grey, sand speckle, power-trowel burnish arcs, curing mottle.
    speck = (r.random((N, N)) > .985).astype(np.float32)
    speck = blur(speck, .6) * 4
    sand = band(N, N, 600, 1400, 702)
    mott = spectral(N, N, 1.6, 703)
    burn = band(N, N, 6, 30, 704)
    col = hexc("#958F84")[None, None, :] * (0.92 + .12 * mott[..., None]) * (0.97 + .05 * sand[..., None])
    col *= (1 - .12 * np.clip(speck, 0, 1)[..., None])
    col *= (0.97 + .05 * burn[..., None])
    stains = load_gray(cc0("SurfaceImperfections015", "Color"), N)
    col *= (0.86 + .16 * stains[..., None])
    height = .3 * sand + .25 * speck
    smooth = .18 + .12 * burn
    # Notched-trowel adhesive: arcs of parallel ridges (5 mm pitch from a 3 mm notch).
    glue = np.zeros((N, N), np.float32)
    ridge = np.zeros((N, N), np.float32)
    pitch = .0055 * ppm
    for k in range(130):
        cx, cy = r.random() * N, r.random() * N
        R = r.uniform(.28, .55) * ppm
        w = r.uniform(.18, .24) * ppm                          # trowel width
        a0 = r.uniform(0, 2 * np.pi); span = r.uniform(.5, 1.3)
        ox, oy = cx - R * np.cos(a0), cy - R * np.sin(a0)       # arc centre
        box = int(R + w) + 4
        ys = np.arange(int(oy) - box, int(oy) + box + 1); xs = np.arange(int(ox) - box, int(ox) + box + 1)
        Y, X = np.meshgrid(ys, xs, indexing="ij")
        d = np.hypot(X - ox, Y - oy)
        ang = (np.arctan2(Y - oy, X - ox) - a0 + np.pi) % (2 * np.pi) - np.pi
        inside = (np.abs(d - R) < w / 2) & (np.abs(ang) < span / 2)
        fade = np.clip((1 - np.abs(ang) / (span / 2)) * 3, 0, 1) * np.clip((w / 2 - np.abs(d - R)) / (.02 * ppm), 0, 1)
        prof = .5 + .5 * np.cos(2 * np.pi * (d - R) / pitch)
        yi, xi = Y % N, X % N
        m = inside.astype(np.float32) * fade
        cur = glue[yi, xi]
        upd = m > cur * .6
        glue[yi[upd], xi[upd]] = np.maximum(m[upd], cur[upd])
        ridge[yi[upd], xi[upd]] = prof[upd]
    tear = np.clip((band(N, N, 2, 9, 705) - .22) / .30, 0, 1) * (0.75 + .25 * band(N, N, 40, 160, 709))  # scraped areas
    glue *= tear
    fib = np.clip((band(N, N, 40, 160, 706) - .78) / .08, 0, 1) * glue * .8  # carpet backing fibres stuck in it
    # Multipurpose latex carpet adhesive dries amber-tan; the ridges catch light, the valleys stay dark.
    # Iteration 3: dried and dusty, so less saturated than wet glue.
    gcol = hexc("#76634A")[None, None, :] * (0.74 + .46 * ridge[..., None]) * (0.9 + .2 * band(N, N, 30, 120, 707)[..., None])
    col = col * (1 - .78 * glue[..., None]) + gcol * .78 * glue[..., None]
    fcol = hexc("#A39578")[None, None, :] * (0.85 + .3 * band(N, N, 300, 900, 708)[..., None])
    col = col * (1 - fib[..., None]) + fcol * fib[..., None]
    height += glue * (.8 * ridge + .3) + .4 * fib
    smooth = smooth * (1 - glue) + (.30 + .18 * ridge) * glue - .2 * fib
    col = np.clip(col, 0, 1)
    n = normal_from_height(blur(height, .55), 2.0)
    cav = .88 + .12 * ridge * glue - .1 * speck
    write("Slab_Adhesive", col, n, mask(smooth, cav))


# ------------------------------------------------------------------ paper back, canvas, tape, nap
def backing():
    N = 1024
    fib = band(N, N, 300, 900, 801); clouds = spectral(N, N, 1.5, 802)
    paste = np.clip((band(N, N, 3, 14, 803) - .55) / .3, 0, 1)
    col = hexc("#E3DDCD")[None, None, :] * (0.96 + .06 * clouds[..., None]) * (0.98 + .03 * fib[..., None])
    col = col * (1 - .25 * paste[..., None]) + hexc("#CDB98C") * .25 * paste[..., None]
    n = normal_from_height(blur(.6 * fib + .2 * clouds, .6), 1.2)
    write("Paper_Backing", np.clip(col, 0, 1), n, mask(.12 + .15 * paste, .95 + .05 * fib))


def canvas():
    N = 2048
    base = os.path.join(os.path.dirname(os.path.abspath(__file__)), "cc0_src", "polyhaven", "rough_linen")
    d = load_rgb(os.path.join(base, "rough_linen_diff_2k.jpg"), N)
    nm = load_rgb(os.path.join(base, "rough_linen_nor_gl_2k.jpg"), N) * 2 - 1
    lum = d @ np.array([.2126, .7152, .0722], np.float32)
    col = hexc("#C4B699")[None, None, :] * (0.82 + .18 * (lum / max(1e-3, lum.mean())))[..., None]
    col = np.clip(col, 0, 1)
    r = np.random.default_rng(811)
    sp = np.zeros((N, N), np.float32); sp2 = np.zeros((N, N), np.float32)
    for k in range(420):
        cx, cy = r.random() * N, r.random() * N
        rad = r.uniform(1.2, 9) if r.random() < .9 else r.uniform(10, 26)
        tgt = sp if r.random() < .55 else sp2
        stamp_ellipse(tgt, cx, cy, rad * r.uniform(.8, 1.6), rad, r.uniform(0, np.pi), lambda dd, u, v: np.clip((1.05 - dd) * 6, 0, 1), N)
    sp = np.clip(sp, 0, 1); sp2 = np.clip(sp2, 0, 1) * (1 - sp)
    col = col * (1 - sp[..., None]) + hexc("#E6E1D3") * sp[..., None]            # primer drips
    col = col * (1 - sp2[..., None]) + hexc("#A9AFA3") * sp2[..., None]          # office paint drips
    grime = load_gray(cc0("SurfaceImperfections001", "Color"), N)
    col *= (1 - .12 * grime[..., None])
    nn = nm.copy(); nn[..., 2] = np.abs(nn[..., 2]); nn /= np.linalg.norm(nn, axis=-1, keepdims=True)
    flat = np.array([0, 0, 1], np.float32)
    nn = nn * (1 - .7 * np.maximum(sp, sp2)[..., None]) + flat * .7 * np.maximum(sp, sp2)[..., None]
    nn /= np.linalg.norm(nn, axis=-1, keepdims=True)
    write("Prop_Canvas", np.clip(col, 0, 1), nn, mask(.08 + .3 * np.maximum(sp, sp2), .85 + .15 * lum))


def tape():
    N = 512
    c = np.asarray(Image.open(cc0("Tape005", "Color")).convert("RGB")).astype(np.float32) / 255
    op = np.asarray(Image.open(cc0("Tape005", "Opacity")).convert("L")).astype(np.float32) / 255
    H, W = op.shape
    # A clean patch inside the first long strip, mirrored to tile.
    ys, xs = np.nonzero(op > .99)
    x0 = int(np.percentile(xs[xs < W * .25], 50)) - 40; y0 = int(H * .25)
    patch = c[y0:y0 + 160, x0:x0 + 80]
    tile = np.concatenate([patch, patch[::-1]], 0); tile = np.concatenate([tile, tile[:, ::-1]], 1)
    tile = np.asarray(Image.fromarray((tile * 255).astype(np.uint8)).resize((N, N), Image.LANCZOS)).astype(np.float32) / 255
    lum = tile @ np.array([.2126, .7152, .0722], np.float32)
    col = hexc("#CDB991")[None, None, :] * (lum / max(1e-3, lum.mean()))[..., None]   # beige crepe masking tape
    crepe = resize(band(N // 6, N, 40, 200, 821), N, N)
    n = normal_from_height(blur(crepe, .5), 1.4)
    write("Prop_TapeBeige", np.clip(col, 0, 1), n, mask(.28 + .05 * crepe, np.full_like(crepe, .95)))


def nap():
    N = 512
    fib = band(N, N, 120, 400, 831); clump = band(N, N, 20, 60, 832)
    paint = np.clip((clump - .35) / .3, 0, 1)
    col = hexc("#DAD6CB")[None, None, :] * (0.85 + .2 * fib[..., None])
    col = col * (1 - .7 * paint[..., None]) + hexc("#A9AFA3") * .7 * paint[..., None]
    n = normal_from_height(blur(.7 * fib + .5 * clump, .6), 2.2)
    write("Prop_RollerNap", np.clip(col, 0, 1), n, mask(.1 + .35 * paint, .7 + .3 * fib))


ALL = dict(primer=primer, stripped=stripped, fresh=fresh, cutin=cutin, compound=compound, slab=slab,
           backing=backing, canvas=canvas, tape=tape, nap=nap)

if __name__ == "__main__":
    for name in (NAMES or ALL.keys()):
        ALL[name]()
