"""FrontRooms surface library v2: Level 0, Level 4 (Office), Level ! (Run), shared.

Run: python gen_surfaces.py <outdir> <cc0 chevron png> [names...]   (numpy + Pillow only)
  no names = every texture a material uses (ALL); test-only targets (EXTRA) run when named.
Repeats (world metres) divide 256 so the stream's floating-origin rebase never
shifts a pattern: wallpaper 256/373, carpet 1, ceiling/office/VCT 256/210, macro 8.
"""
import os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont

sys.argv = [sys.argv[0], sys.argv[1], sys.argv[2]] + sys.argv[3:]
exec(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "gen_common.py")).read())

P4 = 256 / 210  # 4 ft run (1.21905 m)


def mask(smooth, cavity, wet=None):
    z = np.zeros_like(smooth)
    return np.stack([smooth, cavity, z if wet is None else wet, np.ones_like(smooth)], -1)


def write(name, col, n=None, s=None, e=None):
    save_rgb(f"{OUT}/{name}_A.png", col)
    if n is not None: save_normal(f"{OUT}/{name}_N.png", n)
    if s is not None: save_rgba(f"{OUT}/{name}_S.png", s)
    if e is not None: save_rgb(f"{OUT}/{name}_E.png", e)


def wrap_lines(dr, N, M, pts, width, fill=255):
    for ox in (-N, 0, N):
        for oy in (-M, 0, M):
            dr.line([(a + ox, b + oy) for a, b in pts], fill=fill, width=width)


# ======================================================== Level 0 / Lobby / Shift / Exit
# The wallpaper is built from two independent layers
# (Documentation/research/wallpaper_motion/10_synthesis.md §2-3):
#   PRINT  the motion layer: R = continuous ink density, G = cream lift, linear,
#          no paper and no light in it. Colour comes from the material palette
#          (_InkGround/_InkMid/_InkDeep/_InkCream), never from the frame.
#   PAPER  the static layer: fibre, mottling, tobacco blots, foxing and the roll
#          seam as a per-texel affine modulation of the print colour, plus the
#          paper-only normal and mask (no ink terms, with a linen emboss).
# FrontRooms/Surface (_FR_PRINT) recombines them in linear light:
#   albedo = print * alpha + tobacco * beta + gamma
# wallpaper_fields() computes every term once. The legacy wallpaper() composes
# them exactly as before (bit-identical Wallpaper_Chevron_*); it is kept for
# the P0 T1 parity gate. wallpaper_print() / wallpaper_paper() write the split.
# KEEP_HUE RULE: the legacy keep_hue chroma has the chevron's own shape (pink
# stripes, slate bands). It goes into neither layer (in the paper it would
# leave a chevron-shaped colour ghost once the print moves; print B is reserved
# for the phosphor ink and A is kept free), so the split reproduces
# wallpaper(keep_hue=0) and drops the source hue on purpose.
LOBBY_PALETTE = dict(ground="#D2C27C", mid="#AC9A52", deep="#766A34", cream="#E3D594", keep_hue=.16)
COLD_PALETTE = dict(ground="#AFC0B6", mid="#87998F", deep="#56655E", cream="#C5D3C9", keep_hue=.10)
TOBACCO = "#7A5B2A"
_FIELDS = None


def wallpaper_fields():
    """Every term of the wallpaper at 2048 x 3072 (one 0.75 x 1.125 m roll repeat), float32, sRGB-space values."""
    global _FIELDS
    if _FIELDS is not None:
        return _FIELDS
    src = np.asarray(Image.open(REF).convert("RGB")).astype(np.float32) / 255
    src = src[:-1, :-1]  # the CC0 file repeats its first row/column at the far edge
    W, H = 2048, 3072
    pad = np.concatenate([src, src[:, :8]], 1)
    pad = np.concatenate([pad, pad[:8]], 0)
    big = resize(pad, int(W * pad.shape[1] / src.shape[1]), int(H * pad.shape[0] / src.shape[0]))[:H, :W]
    lum = big @ np.array([.2126, .7152, .0722], np.float32)
    f = dict(W=W, H=H, big=big, lum=lum)
    # print: continuous density (0 = ground coat, 1 = deepest ink) and cream lift
    f["ink"] = np.clip((0.92 - lum) / 0.35, 0, 1)
    f["cr"] = np.clip((lum - .90) / .06, 0, 1)
    f["chroma"] = big - lum[..., None]          # source hue residual (pink vs grey inks), x keep_hue
    # paper
    f["fib"] = band(H, W, 180, 900, 11)
    f["mot"] = spectral(H, W, 1.6, 12)
    f["blot"] = band(H, W, 3, 14, 13)
    f["tob"] = np.clip((f["blot"] - .64) / .25, 0, 1) * .08
    fox = (np.random.default_rng(5).random((H, W)) > .99993).astype(np.float32)
    f["fox"] = np.clip(blur(fox, 1.8) * 30, 0, 1)
    x = np.arange(W)[None, :]
    dd = np.minimum(x, W - x).astype(np.float32)
    f["dd"] = dd
    f["seam_shadow"] = np.exp(-(dd / 1.6) ** 2) * .20
    f["seam_lift"] = np.exp(-((dd - 3.5) / 1.6) ** 2) * .04
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    weave = np.sin(xx * 2 * np.pi / (W / 640)) * np.sin(yy * 2 * np.pi / (H / 960))
    f["weave"] = weave * (0.6 + 0.4 * band(H, W, 40, 200, 14))
    _FIELDS = f
    return f


def wallpaper_legacy_col(ground, mid, deep, cream, keep_hue, f=None):
    """Today's albedo (sRGB values, unclipped), composed exactly as the pre-split wallpaper()."""
    f = f or wallpaper_fields()
    big, lum, ink = f["big"], f["lum"], f["ink"]
    g, m, d = hexc(ground), hexc(mid), hexc(deep)
    t = ink[..., None]
    duo = np.where(t < .5, g + (m - g) * (t / .5), m + (d - m) * ((t - .5) / .5))
    col = duo + (big - lum[..., None]) * keep_hue
    cr = f["cr"][..., None]
    col = col + (hexc(cream) - col) * cr * .55
    mot, fib = f["mot"], f["fib"]
    col *= (0.97 + 0.06 * mot[..., None]) * (0.988 + 0.024 * fib[..., None])
    tob = f["tob"][..., None]
    col = col * (1 - tob) + hexc(TOBACCO) * tob
    col *= (1 - .09 * f["fox"][..., None])
    col = col * (1 - f["seam_shadow"][..., None]) + f["seam_lift"][..., None]
    return col


def wallpaper(name="Wallpaper_Chevron", ground="#D2C27C", mid="#AC9A52", deep="#766A34", cream="#E3D594", keep_hue=.16):
    """Legacy one-layer wallpaper (ink baked into albedo, normal and mask). Kept for the P0 T1 parity gate."""
    f = wallpaper_fields()
    col = wallpaper_legacy_col(ground, mid, deep, cream, keep_hue, f)
    ink, fib, mot, dd = f["ink"], f["fib"], f["mot"], f["dd"]
    height = .18 * f["weave"] + .9 * blur(ink, 1.2) + .40 * fib + .6 * np.exp(-((dd - 2) / 2.2) ** 2)
    n = normal_from_height(height, 1.0)
    smooth = .20 + .07 * blur(ink, 1.0) - .04 * mot + .03 * (1 - fib)
    cav = 1 - .35 * f["seam_shadow"] / .20 - .10 * ink
    write(name, col, n, mask(smooth, cav))


def wallpaper_cold():
    # Exit / cold threshold: the same paper under a different print run.
    wallpaper("Wallpaper_Chevron_Cold", **COLD_PALETTE)


# ---------------------------------------------------------------- the split (P0)
CREAM_MIX = .55            # shader: print = lerp(InkRamp(R), _InkCream, G * 0.55)
# Paper texel decode, mirrored in FrontRoomsSurface.shader (FR_PAPER_SCALE / FR_PAPER_BIAS):
#   (alpha, beta, gamma) = texel.rgb * PAPER_SCALE + PAPER_BIAS
PAPER_SCALE = np.array([.70, .14, .02], np.float32)
PAPER_BIAS = np.array([.46, -.001, -.004], np.float32)
# One print serves every Level 0 palette; the fit weights Lobby (Lobby + Shift) over Exit.
PRINT_FIT = (("lobby", LOBBY_PALETTE, 2.0), ("cold", COLD_PALETTE, 1.0))
# Optional frame 0 from the print tools (Tools/print/patterns/out/<chosen>/K00.png, RGBA:
# R raw density, G raw cream, B phosphor, A unused). Set it to replace the CC0-derived frame.
# Both sources go through the same print_encode(), so nothing else changes (the paper does
# not depend on the print). Since Q1b the game's print does NOT come from here: the print
# tools deliver ENCODED 2048 x 2048 slices and Unity's Build Print Array writes _PrintTex and
# the _FR_Print array from them (Assets/Editor/Rendering/FrontRoomsPrintArray.cs).
PRINT_FILE = os.environ.get("FR_PRINT_K00")
# PRINT ENCODING (binding, shared with the print tools): a print texel's R and G are the
# parameters of the SHADER's ramp, which runs in LINEAR light:
#   colour = lerp(InkRamp_linear(R), _InkCream, G * 0.55)
# Art made with today's meaning ("raw": the duotone and the cream lerp in sRGB values, as
# wallpaper() and print_tool.py's preview shade them) must be converted with print_encode()
# before it becomes a texel: _PrintTex here, every _FR_Print slice in the print tools. The
# table is exported by the `print_encode_lut` target (print_encode_lut.json next to this
# file). It keeps 0 / 0.5 / 1 density (ground / mid / deep) and zero cream fixed and bends
# the values in between by up to ~0.03 density and ~0.1 cream.
# PRINT MIPS (binding too): never plain box mips. Cream is box-filtered, density is averaged
# with the weight (1 - 0.55 * cream):  d' = box((1 - .55c) d) / box(1 - .55c),  c' = box(c).
# Unity applies it on import to every *_P texture (FrontRoomsPrintMips in
# Assets/Editor/Rendering/FrontRoomsRenderSetup.cs); _FR_Print slices must be built with
# FrontRoomsPrintMips.ApplySlice (or this rule), or the static frame 0 and the live slices
# filter differently. See print_mips() below.


def _ramp(t, g, m, d):
    t = np.asarray(t, np.float32)[..., None]
    return np.where(t < .5, g + (m - g) * (t * 2), m + (d - m) * (t * 2 - 1))


def palette_srgb(pal):
    return [hexc(pal[k]) for k in ("ground", "mid", "deep", "cream")]


def palette_lin(pal):
    """What the shader receives: Unity linearises material Color properties in Linear colour space."""
    return [srgb_to_lin(c).astype(np.float32) for c in palette_srgb(pal)]


def raw_print_from_lum(lum):
    """Today's (raw) density and cream lift from the source luminance (as wallpaper() and print_tool.py)."""
    return np.clip((0.92 - lum) / 0.35, 0, 1), np.clip((lum - .90) / .06, 0, 1)


def print_srgb_raw(d, c, pal):
    """Raw print colour (sRGB values): the duotone and the cream lerp in sRGB values, as today."""
    g, m, dp, cr = palette_srgb(pal)
    duo = _ramp(d, g, m, dp)
    return duo + (cr - duo) * np.asarray(c, np.float32)[..., None] * CREAM_MIX


def print_srgb_from_lum(lum, pal):
    """Today's print colour without keep_hue (sRGB values). It depends on the source luminance only."""
    return print_srgb_raw(*raw_print_from_lum(lum), pal)


def print_lin(d, c, pal):
    """The shader's print colour in linear light: lerp(InkRamp(d), _InkCream, c * 0.55)."""
    g, m, dp, cr = palette_lin(pal)
    r = _ramp(d, g, m, dp)
    return r + (cr - r) * (np.asarray(c, np.float32)[..., None] * CREAM_MIX)


_PRINT_ENC = None


def _fit_1d(x, resid, iters=40):
    """Gauss-Newton on one clipped 0..1 unknown per row; resid(x) -> (n, k) residuals."""
    for _ in range(iters):
        r = resid(x)
        e = np.where(x < .5, 1e-3, -1e-3).astype(np.float32)
        J = (resid(x + e) - r) / e[:, None]
        x = np.clip(x - (J * r).sum(-1) / ((J * J).sum(-1) + 1e-6), 0, 1).astype(np.float32)
    return x


def print_encode_lut(nd=257, nc=65):
    """(raw density grid, raw cream grid, density table, cream table): the print encoding.

    Fitted by Gauss-Newton, jointly for the PRINT_FIT palettes with error in 8-bit sRGB units,
    so the shader's linear-light colour print_lin(d, c) lands on the raw colour
    print_srgb_raw(raw d, raw c), in two well-posed 1-D steps (a joint 2-D fit is ill-posed:
    the cream and the ground are nearly the same hue, so it trades cream for lighter density
    and G would lose its meaning):
      density d = D(raw d), fitted with no cream; it does not depend on cream;
      cream   c = C(raw d, raw c), fitted with d fixed; 0 wherever raw cream is 0.
    Both are monotone. The grids include 0, 0.5 and 1 exactly (the ramp's stops and kink)."""
    global _PRINT_ENC
    if _PRINT_ENC is not None:
        return _PRINT_ENC
    gd = np.linspace(0, 1, nd).astype(np.float32)
    gc = np.linspace(0, 1, nc).astype(np.float32)
    zero = np.zeros_like(gd)
    t0 = [(print_srgb_raw(gd, zero, p) * 255, np.float32(np.sqrt(w)), p) for _, p, w in PRINT_FIT]
    d = _fit_1d(gd.copy(), lambda x: np.concatenate([w * (lin_to_srgb(print_lin(x, zero, p)) * 255 - t) for t, w, p in t0], -1))
    D0, C0 = (a.ravel() for a in np.meshgrid(gd, gc, indexing="ij"))
    dd = np.repeat(d, nc)
    tc = [(print_srgb_raw(D0, C0, p) * 255, np.float32(np.sqrt(w)), p) for _, p, w in PRINT_FIT]
    c = _fit_1d(C0.copy(), lambda x: np.concatenate([w * (lin_to_srgb(print_lin(dd, x, p)) * 255 - t) for t, w, p in tc], -1))
    c = np.where(C0 > 0, c, 0).astype(np.float32)
    _PRINT_ENC = (gd, gc, np.repeat(d[:, None], nc, 1), c.reshape(nd, nc))
    return _PRINT_ENC


def print_encode(d_raw, c_raw):
    """Raw (density, cream) -> shader texel (density, cream): bilinear in the print_encode_lut table."""
    gd, gc, td, tc = print_encode_lut()
    d_raw = np.clip(np.asarray(d_raw, np.float64), 0, 1); c_raw = np.clip(np.asarray(c_raw, np.float64), 0, 1)
    x = d_raw * (len(gd) - 1); y = c_raw * (len(gc) - 1)
    i = np.minimum(np.floor(x).astype(int), len(gd) - 2); j = np.minimum(np.floor(y).astype(int), len(gc) - 2)
    u = x - i; v = y - j
    out = []
    for t in (td, tc):
        out.append(((t[i, j] * (1 - u) + t[i + 1, j] * u) * (1 - v) + (t[i, j + 1] * (1 - u) + t[i + 1, j + 1] * u) * v).astype(np.float32))
    return out[0], out[1]


def print_encode_lut_json(path=None):
    """Write the encoding table for the print tools (they apply it to every _FR_Print slice)."""
    import json
    gd, gc, td, tc = print_encode_lut()
    path = path or os.path.join(os.path.dirname(os.path.abspath(__file__)), "print_encode_lut.json")
    doc = {"what": "FrontRooms wallpaper print encoding (gen_surfaces.py print_encode). Input: RAW density and cream "
                   "(today's meaning: the duotone and the cream lerp in sRGB values). Output: the texel R (density) and "
                   "G (cream) for _PrintTex and every _FR_Print slice; the shader ramps in linear light.",
           "lookup": "bilinear: x = raw_density * (len(density_raw) - 1), y = raw_cream * (len(cream_raw) - 1); "
                     "table[i][j] is indexed [density_raw][cream_raw]. B (phosphor) and A pass through unchanged.",
           "order": "encode at the frame's own resolution, then resample (as the GPU filters encoded texels)",
           "mips": "not plain box: c' = box(c), d' = box((1 - 0.55 c) d) / box(1 - 0.55 c) on the ENCODED values "
                   "(FrontRoomsPrintMips in Assets/Editor/Rendering/FrontRoomsRenderSetup.cs; gen_surfaces.print_mips)",
           "palettes_fitted": {k: {n: p[n] for n in ("ground", "mid", "deep", "cream")} for k, p, _ in PRINT_FIT},
           "palette_weights": {k: w for k, _, w in PRINT_FIT},
           "density_raw": [round(float(v), 6) for v in gd], "cream_raw": [round(float(v), 6) for v in gc],
           "density": [[round(float(v), 5) for v in row] for row in td],
           "cream": [[round(float(v), 5) for v in row] for row in tc]}
    with open(path, "w") as fh:
        json.dump(doc, fh, separators=(",", ":"))
    print("wrote", path, flush=True)


def _periodic_resize(a, w, h):
    """Resize a seamless tile without breaking its wrap: resample a 3 x 3 tiling, keep the centre
    (the same as print_tool.py resample_periodic)."""
    if a.shape[1] == w and a.shape[0] == h:
        return a.astype(np.float32)
    big = np.tile(a.astype(np.float32), (3, 3))
    r = np.asarray(Image.fromarray(big, "F").resize((w * 3, h * 3), Image.BICUBIC), np.float32)
    return np.clip(r[h:2 * h, w:2 * w], 0, 1)


def print_maps(f=None):
    """Frame 0 at 2048 x 3072: (density, cream, phosphor) in 0..1, linear, ENCODED (print_encode).
    The CC0 chevron and a FR_PRINT_K00 file take the same path: raw values -> print_encode."""
    f = f or wallpaper_fields()
    if PRINT_FILE:
        k = np.asarray(Image.open(PRINT_FILE).convert("RGBA")).astype(np.float32) / 255
        d, c = print_encode(k[..., 0], k[..., 1])                   # at the file's own resolution
        return tuple(_periodic_resize(x, f["W"], f["H"]) for x in (d, c, k[..., 2]))
    d, c = print_encode(*raw_print_from_lum(f["lum"]))
    return d, c, np.zeros_like(d)


def print_mips(d, c, levels=None):
    """Reference implementation of the print mip rule (Unity: FrontRoomsPrintMips): list of (d, c)
    per level from mip 0, 2 x 2 box steps (even sizes). Cream-weighted density keeps the composed
    colour of every mip texel equal to the box average of the mip-0 colours, for any palette,
    wherever the footprint stays on one side of the ramp's mid stop."""
    down = lambda a: (a[0::2, 0::2] + a[1::2, 0::2] + a[0::2, 1::2] + a[1::2, 1::2]) / 4
    wgt = 1 - CREAM_MIX * c
    A, B, C = wgt.astype(np.float64), (wgt * d).astype(np.float64), c.astype(np.float64)
    out = [(d, c)]
    while (levels is None or len(out) < levels) and A.shape[0] % 2 == 0 and A.shape[1] % 2 == 0:
        A, B, C = down(A), down(B), down(C)
        out.append(((B / A).astype(np.float32), C.astype(np.float32)))
    return out


def paper_terms(f=None):
    """Today's paper terms as one sRGB-space affine map of the print colour P (sRGB values):
         col = P * a + TOBACCO * b + lift
    (exactly the legacy composition with keep_hue = 0: mottle x fibre, tobacco blot lerp,
    foxing, seam shadow and seam lift). Pattern-free: nothing here depends on the print."""
    f = f or wallpaper_fields()
    M = (0.97 + 0.06 * f["mot"]) * (0.988 + 0.024 * f["fib"])
    FS = (1 - .09 * f["fox"]) * (1 - f["seam_shadow"])
    a = M * (1 - f["tob"]) * FS
    b = f["tob"] * FS
    lift = np.broadcast_to(f["seam_lift"], a.shape)
    return a.astype(np.float32), b.astype(np.float32), lift.astype(np.float32)


def _paper_print_samples():
    """Print colours the paper must work under: every PRINT_FIT palette, density 0..1, cream 0..1."""
    dd = np.concatenate([np.linspace(0, 1, 41), np.zeros(8)]).astype(np.float32)
    cc = np.concatenate([np.zeros(41), np.linspace(.125, 1, 8)]).astype(np.float32)
    P = np.concatenate([print_lin(dd, cc, p) for _, p, _ in PRINT_FIT], 0)
    w = np.concatenate([np.full(len(dd), wt, np.float32) for _, _, wt in PRINT_FIT])
    return P.astype(np.float64), w.astype(np.float64)


_PAPER_LUT = None


def paper_lut(na=257, nb=19, nl=15):
    """3-D table (a, b, lift) -> (alpha, beta, gamma) such that, in linear light,
         print * alpha + tobacco * beta + gamma  ~=  lin(srgb(print) * a + TOBACCO * b + lift)
    for every print colour of every PRINT_FIT palette (weighted least squares in 8-bit sRGB units)."""
    global _PAPER_LUT
    if _PAPER_LUT is not None:
        return _PAPER_LUT
    a, b, lift = paper_terms()
    ga = np.linspace(float(a.min()) - 1e-3, float(a.max()) + 1e-3, na)
    gb = np.linspace(0, float(b.max()) + 1e-4, nb)
    gl = np.linspace(0, float(lift.max()) + 1e-4, nl)
    Pl, pw = _paper_print_samples()                                  # (n, 3) linear prints
    Ps = lin_to_srgb(Pl)
    T = hexc(TOBACCO).astype(np.float64); Tl = srgb_to_lin(T)
    A, B, Lf = np.meshgrid(ga, gb, gl, indexing="ij")
    cells = np.stack([A.ravel(), B.ravel(), Lf.ravel()], -1)        # (C, 3)
    X = np.stack([Pl.ravel(), np.tile(Tl, len(Pl)), np.ones(Pl.size)], -1)    # (n*3, 3)
    tgt = np.clip(Ps.ravel()[None] * cells[:, :1] + np.tile(T, len(Pl))[None] * cells[:, 1:2] + cells[:, 2:3], 0, 1)
    ylin = srgb_to_lin(tgt)                                          # (C, n*3)
    slope = 1.055 / 2.4 * np.maximum(ylin, 1e-4) ** (1 / 2.4 - 1) * 255   # d sRGB8 / d linear
    W = (slope ** 2) * np.repeat(pw, 3)[None]
    AtA = np.einsum("cn,ni,nj->cij", W, X, X) + np.eye(3)[None] * 1e-9
    Aty = np.einsum("cn,ni,cn->ci", W, X, ylin)
    theta = np.linalg.solve(AtA, Aty[..., None])[..., 0]
    _PAPER_LUT = (ga, gb, gl, theta.reshape(na, nb, nl, 3).astype(np.float32))
    return _PAPER_LUT


def _grid_index(g, x):
    """Cell index and weight of x on the uniform grid g (x inside the grid)."""
    t = (np.asarray(x, np.float64) - g[0]) / (g[1] - g[0])
    i = np.clip(np.floor(t).astype(int), 0, len(g) - 2)
    return i, t - i


def paper_affine(f=None):
    """Per-texel (alpha, beta, gamma), float32 (H, W, 3): trilinear in the paper_lut table
    (numpy only; the (a, b, lift) grids are uniform)."""
    ga, gb, gl, th = paper_lut()
    a, b, lift = paper_terms(f)
    th = th.astype(np.float64)
    out = np.empty(a.shape + (3,), np.float32)
    for r0 in range(0, a.shape[0], 512):
        sl = slice(r0, r0 + 512)
        (i, u), (j, v), (k, w) = _grid_index(ga, a[sl]), _grid_index(gb, b[sl]), _grid_index(gl, lift[sl])
        u, v, w = u[..., None], v[..., None], w[..., None]
        acc = 0
        for di, wu in ((0, 1 - u), (1, u)):
            for dj, wv in ((0, 1 - v), (1, v)):
                for dk, ww in ((0, 1 - w), (1, w)):
                    acc = acc + th[i + di, j + dj, k + dk] * (wu * wv * ww)
        out[sl] = acc
    return out


def paper_encode(theta):
    """(alpha, beta, gamma) -> texel 0..1 (the shader decodes texel * PAPER_SCALE + PAPER_BIAS)."""
    t = (theta - PAPER_BIAS) / PAPER_SCALE
    lo, hi = float(t.min()), float(t.max())
    if lo < -.5 / 255 or hi > 1 + .5 / 255:
        print(f"WARNING paper encoding clips: texel range {lo:.4f}..{hi:.4f}; widen PAPER_SCALE/PAPER_BIAS", flush=True)
    return np.clip(t, 0, 1)


def paper_decode(texel):
    return texel * PAPER_SCALE + PAPER_BIAS


def _wrap_rows(a, n):
    """Periodic linear resample of axis 0 to n rows (keeps the tile seamless)."""
    m = a.shape[0]
    y = np.arange(n, dtype=np.float32) * m / n
    i0 = np.floor(y).astype(int); t = (y - i0)[:, None]
    return (a[i0 % m] * (1 - t) + a[(i0 + 1) % m] * t).astype(np.float32)


TILE_M = (0.75, 1.125)     # one wallpaper repeat (x, y) in metres: RenderSetup Roll x 1.5 Roll


def band_metric(H, W, lo, hi, seed):
    """Periodic band-limited noise, ISOTROPIC in metres (gen_common.band counts cycles per tile
    axis, so on the 0.75 x 1.125 m tile it is 1.5x stretched in y). lo..hi in cycles per metre,
    log-gaussian like band() and hard-limited to [lo, hi]; the FFT bins are integer cycles per
    tile, so the tile stays seamless."""
    r = np.random.default_rng(seed)
    F = np.fft.fft2(r.standard_normal((H, W)))
    fy = np.fft.fftfreq(H)[:, None] * H / TILE_M[1]
    fx = np.fft.fftfreq(W)[None, :] * W / TILE_M[0]
    f = np.sqrt(fx * fx + fy * fy)
    m = np.exp(-((np.log(np.maximum(f, 1e-3)) - np.log((lo * hi) ** .5)) ** 2) / (2 * (np.log(hi / lo) / 2.5) ** 2))
    m *= (f >= lo) & (f <= hi)
    n = np.real(np.fft.ifft2(F * m))
    n -= n.min(); n /= max(1e-8, n.max())
    return n.astype(np.float32)


def linen_emboss(H, W):
    """Period-plausible embossed linen (vinyl-coated wallcoverings of the 1970s-90s), 0..1,
    every feature inside the 2-6 mm band of the research spec (§2):
    plain weave: warp threads 8 px = 2.93 mm apart, weft 9.6 px = 3.52 mm; each thread has its
    own slubs and goes over/under at alternate crossings. `coarse` = an isotropic 2-6 mm pebble
    (167-500 cycles per metre) plus a sparse ~6 mm stipple (dots ~2.7 mm across). Integer
    cycles per tile, so the tile stays seamless. Returns (linen, coarse)."""
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    nx, ny = W // 8, 320                                  # threads per tile (both even)
    u = xx * nx / W; v = yy * ny / H
    sx = _wrap_rows(band(64, nx, 3, 12, 142), H)          # (H, nx): slubs along each warp thread
    sy = _wrap_rows(band(64, ny, 3, 12, 143), W).T        # (ny, W): slubs along each weft thread
    ix = np.rint(u).astype(int) % nx; iy = np.rint(v).astype(int) % ny
    wx = sx[np.arange(H)[:, None], ix]
    wy = sy[iy, np.arange(W)[None, :]]
    warp = (.5 + .5 * np.cos(2 * np.pi * u)) * (.75 + .5 * wx)
    weft = (.5 + .5 * np.cos(2 * np.pi * v)) * (.75 + .5 * wy)
    s = np.cos(np.pi * u) * np.cos(np.pi * v)            # sign (-1)^(i+j) at crossing (i, j)
    over = .5 + .5 * np.sign(s) * np.sqrt(np.abs(s))
    h = blur(np.maximum(warp * (.55 + .45 * over), weft * (.55 + .45 * (1 - over))), .6)
    r = np.random.default_rng(141)
    stip = blur((r.random((H, W)) > .9965).astype(np.float32), 3.2)
    stip = stip / max(1e-6, float(stip.max()))
    pebble = band_metric(H, W, 1 / .006, 1 / .002, 144)  # 6-2 mm, isotropic
    coarse = .6 * pebble + .4 * stip
    coarse = (coarse - coarse.min()) / max(1e-6, float(coarse.max() - coarse.min()))
    return h.astype(np.float32), coarse.astype(np.float32)


# Height weights (art call, Red). RMS normal tilt at mip 0/1/2/3 (strength 1):
#   legacy with ink 6.1/3.0/1.8/1.3; ink-free, no emboss 5.7/2.2/0.7/0.3;
#   this default (linen .25, coarse .6): 6.4/3.5/2.1/0.6.
# A 2-6 mm emboss cannot keep today's 1.3 deg at mip 3 (~2 m head-on at 1080p): coarse 1.7
# would, at 7.9/5.5/3.7/1.3 (rougher than today up close). Today's mip-3 relief is the
# 37.5 mm chevron itself.
EMBOSS_LINEN, EMBOSS_COARSE = .25, .60


def paper_emboss_height(linen, coarse):
    return EMBOSS_LINEN * linen + EMBOSS_COARSE * coarse


def wallpaper_paper(name="Wallpaper_Paper"):
    """The static paper: _M affine modulation (linear RGB: alpha, beta, gamma), _N paper-only
    normal (no ink relief; weave, fibre, seam and the linen emboss), _S paper-only mask."""
    f = wallpaper_fields()
    theta = paper_affine(f)
    save_rgb(f"{OUT}/{name}_M.png", paper_encode(theta))
    fib, mot, dd = f["fib"], f["mot"], f["dd"]
    linen, coarse = linen_emboss(f["H"], f["W"])
    emb = EMBOSS_LINEN * linen + EMBOSS_COARSE * coarse
    emb = (emb - emb.min()) / max(1e-6, float(emb.max() - emb.min()))
    ink_mean_blur, ink_mean = float(blur(f["ink"], 1.0).mean()), float(f["ink"].mean())
    height = .18 * f["weave"] + .40 * fib + .6 * np.exp(-((dd - 2) / 2.2) ** 2) + paper_emboss_height(linen, coarse)
    n = normal_from_height(height, 1.0)
    smooth = .20 + .07 * ink_mean_blur - .04 * mot + .03 * (1 - fib) + .03 * (emb - float(emb.mean()))
    cav = 1 - .35 * f["seam_shadow"] / .20 - .10 * ink_mean - .06 * ((1 - emb) - float((1 - emb).mean()))
    save_normal(f"{OUT}/{name}_N.png", n)
    save_rgba(f"{OUT}/{name}_S.png", mask(smooth, np.broadcast_to(cav, smooth.shape)))


def wallpaper_print(name="Wallpaper_Print_CC0_P"):
    """The CC0 chevron frame 0 that P0 shipped as _PrintTex (2048 x 3072): linear RGB, R density,
    G cream, B phosphor (reserved, 0). Since Q1b (2026-10-03) the production _PrintTex
    (Resources/Surfaces/Textures/Wallpaper_Print_P.png) is the Hard edge K00 at 2048 x 2048,
    written by Unity's FrontRooms > Rendering > Build Print Array from the print tools' encoded
    slices; this generator must never write Wallpaper_Print_P again. This target only rebuilds
    the CC0 frame for the P0 T1 test and the before/after captures:
    <outdir> = Assets/Editor/Rendering/PrintP0/Ref."""
    d, c, ph = print_maps()
    save_rgb(f"{OUT}/{name}.png", np.stack([d, c, ph], -1))


def wallpaper_t1_reference():
    """T1a references: the legacy albedo with keep_hue = 0 (same generator, same N/S as today).
    Test-only; not used by any shipping material."""
    f = wallpaper_fields()
    for name, pal in (("Wallpaper_Chevron_NoHue", LOBBY_PALETTE), ("Wallpaper_Chevron_Cold_NoHue", COLD_PALETTE)):
        save_rgb(f"{OUT}/{name}_A.png", wallpaper_legacy_col(**{**pal, "keep_hue": 0.0}, f=f))


def carpet():
    N = 2048
    r = np.random.default_rng(29)
    # Scatter loop "nubs" on a jittered lattice; each nub is a soft bump with its
    # own size, height and yarn tone. Rows drift so nothing reads as a weave.
    p = 7.0
    nubs = np.zeros((N, N), np.float32)
    tones = np.zeros((N, N), np.float32)
    k = int(N / p)
    yy, xx = np.mgrid[0:7, 0:7].astype(np.float32) - 3
    for j in range(k):
        drift = r.normal(0, 1.2)
        for i in range(k):
            cx = (i + .5 + r.normal(0, .22)) * p + drift
            cy = (j + .5 + r.normal(0, .22)) * p
            rad = r.uniform(1.8, 3.2)
            hgt = r.uniform(.55, 1.0)
            ix, iy = int(cx), int(cy)
            bump = np.exp(-((xx - (cx - ix)) ** 2 + (yy - (cy - iy)) ** 2) / (rad * rad)) * hgt
            ys = (np.arange(iy - 3, iy + 4) % N)[:, None]; xs = (np.arange(ix - 3, ix + 4) % N)[None, :]
            nubs[ys, xs] = np.maximum(nubs[ys, xs], bump)
            tones[ys, xs] = np.where(bump > .25, r.random(), tones[ys, xs])
    fibre = band(N, N, 500, 1000, 24)
    tuft = band(N, N, 60, 220, 23)
    height = .62 * nubs + .25 * tuft + .18 * fibre
    tone = spectral(N, N, 1.4, 25)
    base = hexc("#9A8558")
    yarn = np.where(tones[..., None] > .88, hexc("#7C6A47"), np.where(tones[..., None] < .08, hexc("#B09C6E"), base))
    col = yarn * (0.93 + 0.09 * tone[..., None]) * (0.72 + 0.36 * height[..., None])
    n = normal_from_height(blur(height, .45), 2.2)
    write("Carpet_LoopPile", col, n, mask(.05 + .05 * (1 - height), .6 + .4 * height))


def ceiling(name="Ceiling_Fissured", layout="2x4", fissures=2400, depth=.16, face_hex="#D9D2BF", seed=31):
    N = 2048
    ppm = N / P4
    grid = 0.0238 * ppm
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)
    if layout == "2x4":
        dx = np.minimum.reduce([xx, np.abs(xx - N / 2), N - xx]); dy = np.minimum(yy, N - yy)
        tile_id = (xx >= N / 2).astype(np.float32)
    else:
        dx = np.minimum.reduce([xx, np.abs(xx - N / 2), N - xx]); dy = np.minimum.reduce([yy, np.abs(yy - N / 2), N - yy])
        tile_id = ((xx >= N / 2).astype(int) + 2 * (yy >= N / 2).astype(int)).astype(np.float32)
    dgrid = np.minimum(dx, dy)
    tbar = (dgrid < grid / 2).astype(np.float32)
    bevel = np.clip((dgrid - grid / 2) / (0.010 * ppm), 0, 1)
    img = Image.new("L", (N, N), 0)
    dr = ImageDraw.Draw(img)
    r = np.random.default_rng(seed)
    for _ in range(fissures):
        x, y = r.random() * N, r.random() * N
        a = r.random() * np.pi * 2
        pts = [(x, y)]
        for _ in range(int(r.integers(2, 8))):
            a += r.normal(0, .8); x += np.cos(a) * 4; y += np.sin(a) * 4
            pts.append((x, y))
        wrap_lines(dr, N, N, pts, int(r.integers(1, 3)))
    fis = blur(np.asarray(img).astype(np.float32) / 255, .7)
    pin = (r.random((N, N)) > .9955).astype(np.float32)
    pin = np.clip(blur(pin, .8) * 5, 0, 1)
    grain = band(N, N, 300, 900, seed + 1)
    tone = 1 - .02 * tile_id
    face = hexc(face_hex)[None, None, :] * tone[..., None]
    face = face * (0.95 + .05 * grain[..., None]) * (1 - depth * fis[..., None]) * (1 - .28 * pin[..., None])
    face *= (0.96 + .06 * spectral(N, N, 1.8, seed + 2)[..., None])
    col = face * (0.82 + .18 * bevel[..., None])
    col = col * (1 - tbar[..., None]) + hexc("#E3DFD3") * tbar[..., None]
    height = np.where(tbar > 0, 1.6, (.5 * grain - .8 * fis - .6 * pin) * .25 + (bevel - 1) * .9)
    n = normal_from_height(blur(height, .8), 3.0)
    smooth = np.where(tbar > 0, .42, .07 + .03 * grain)
    cav = np.where(tbar > 0, 1.0, .55 + .45 * bevel) * (1 - .25 * fis)
    write(name, col, n, mask(smooth, cav))


def lens():
    W, H = 1024, 2048
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    p = 5.0
    prism = np.abs(((xx % p) / p) - .5) + np.abs(((yy % p) / p) - .5)
    u = xx / W
    tubes = sum(np.exp(-((u - c) / .085) ** 2) for c in (.27, .5, .73))
    ends = np.clip(np.minimum(yy, H - yy) / (H * .06), 0, 1) ** .6
    emit = blur((.55 + .45 * tubes / tubes.max()) * ends * (.88 + .12 * (1 - prism)), .7)
    alb = hexc("#EDEBE3")[None, None, :] * (.94 + .06 * (1 - prism)[..., None])
    write("TrofferLens", alb, normal_from_height(prism, 1.6), None, np.repeat(emit[..., None], 3, -1))


def carpet_wet_free_macro():
    pass


# ======================================================== Level 4 / Office
def office_carpet():
    N = 2048  # 2 x 2 tiles of 24" (256/420 m), quarter-turned
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)
    half = N // 2
    quarter = ((xx >= half).astype(int) + (yy >= half).astype(int)) % 2
    # Cut-pile striation that changes direction tile to tile (quarter-turn install).
    sx = band(N, N * 8, 200, 900, 71); sx = resize(sx, N, N)
    sy = sx.T
    stri = np.where(quarter == 0, sx, sy)
    fibre = band(N, N, 400, 1000, 72)
    base = hexc("#5B636B")
    col = base[None, None, :] * (0.90 + .12 * stri[..., None]) * (0.92 + .10 * fibre[..., None])
    r = np.random.default_rng(73)
    flecks = r.random((N, N))
    for thr, c in ((.9975, "#3E8079"), (.9985, "#8C6A7D"), (.996, "#2E3237"), (.997, "#9AA2A8")):
        m = blur((flecks > thr).astype(np.float32), .6)
        flecks = r.random((N, N))
        col = col * (1 - np.clip(m * 2.5, 0, 1)[..., None]) + hexc(c) * np.clip(m * 2.5, 0, 1)[..., None]
    x = np.minimum.reduce([xx, np.abs(xx - half), N - xx]); y = np.minimum.reduce([yy, np.abs(yy - half), N - yy])
    seam = np.exp(-(np.minimum(x, y) / 1.2) ** 2)
    col *= (1 - .25 * seam[..., None])
    sheen = np.where(quarter == 0, 1.0, .965)
    col *= sheen[..., None]
    height = .5 * stri + .5 * fibre - .6 * seam
    write("Office_CarpetTile", col, normal_from_height(blur(height, .5), 1.8), mask(.06 + .03 * stri, .7 + .3 * fibre))


def drywall():
    N = 1024  # 256/210 m
    knock = blur(np.clip((band(N, N, 18, 70, 81) - .55) / .2, 0, 1), 1.5)  # knockdown splatter
    peel = band(N, N, 120, 400, 82)
    col = hexc("#BDB6A4")[None, None, :] * (0.97 + .03 * knock[..., None]) * (0.985 + .02 * peel[..., None])
    col *= (0.96 + .06 * spectral(N, N, 1.6, 83)[..., None])
    height = .8 * knock + .3 * peel
    write("Office_Drywall", col, normal_from_height(blur(height, .8), 1.6), mask(.30 + .06 * knock, .9 + .1 * knock))


def office_ceiling():
    ceiling("Office_Ceiling2x2", layout="2x2", fissures=1500, depth=.10, face_hex="#DCD8CC", seed=91)


def louver():
    N = 1024  # 2'x2' parabolic louver: 4 x 4 deep cells
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)
    c = N / 4
    lx = (xx % c) / c; ly = (yy % c) / c
    blade = np.minimum.reduce([lx, 1 - lx, ly, 1 - ly])
    cellglow = np.clip(blade / .5, 0, 1) ** 1.6
    emit = blur(cellglow * .95 + .05, 1.0)
    alb = hexc("#BFC2C4")[None, None, :] * (0.6 + .4 * (1 - cellglow[..., None]))
    height = -blade * 2
    write("Office_Louver", alb, normal_from_height(height, 4.0), mask(.82 - .3 * cellglow, 1 - .5 * cellglow), np.repeat(emit[..., None], 3, -1))


def fabric():
    N = 1024
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)
    weave = .5 * (np.sin(xx * 2 * np.pi / 6) ** 2) + .5 * (np.sin(yy * 2 * np.pi / 6 + np.pi * (np.floor(xx / 6) % 2)) ** 2)
    slub = band(N, N, 30, 160, 101)
    col = hexc("#6A7480")[None, None, :] * (0.88 + .10 * weave[..., None] * .5 + .12 * slub[..., None])
    write("Office_CubicleFabric", col, normal_from_height(blur(.6 * weave + .4 * slub, .5), 1.4), mask(.08 + 0 * slub, .8 + .2 * slub))


# ======================================================== Level ! / Run (hospital corridor)
def vct():
    N = 2048  # 4 x 4 tiles of 12" (256/840 m)
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)
    t = N / 4
    tid = (np.floor(xx / t) + 4 * np.floor(yy / t)).astype(int)
    r = np.random.default_rng(111)
    tone = r.normal(0, .025, 16)[tid]
    # VCT chip pattern: directional flecks.
    chips = band(N * 4, N, 120, 500, 112); chips = resize(chips, N, N)
    chips2 = band(N, N, 300, 900, 113)
    base = hexc("#DAD7CC")
    col = base[None, None, :] * (1 + tone[..., None])
    darkf = np.clip((chips - .62) / .2, 0, 1)
    col = col * (1 - .22 * darkf[..., None]) + hexc("#8F8A7E") * .10 * darkf[..., None]
    col *= (0.97 + .04 * chips2[..., None])
    gx = np.minimum(xx % t, t - xx % t); gy = np.minimum(yy % t, t - yy % t)
    grout = np.exp(-(np.minimum(gx, gy) / 1.1) ** 2)
    col *= (1 - .30 * grout[..., None])
    # Heel scuffs (black marks) and a wax wear lane.
    img = Image.new("L", (N, N), 0); dr = ImageDraw.Draw(img)
    for _ in range(90):
        x, y = r.random() * N, r.random() * N; a = r.random() * np.pi; l = r.random() * 50 + 10
        wrap_lines(dr, N, N, [(x, y), (x + np.cos(a) * l, y + np.sin(a) * l)], int(r.integers(1, 4)))
    sc = blur(np.asarray(img).astype(np.float32) / 255, 1.2)
    col *= (1 - .35 * sc[..., None])
    wear = spectral(N, N, 1.9, 114)
    smooth = .62 - .22 * wear - .25 * sc - .3 * grout
    height = -.8 * grout + .1 * chips2
    write("Run_VCT", col, normal_from_height(blur(height, .6), 1.5), mask(smooth, 1 - .4 * grout))


def hospital_wall():
    N = 1024
    peel = band(N, N, 120, 400, 121)
    img = Image.new("L", (N, N), 0); dr = ImageDraw.Draw(img)
    r = np.random.default_rng(122)
    for _ in range(70):
        x, y = r.random() * N, r.random() * N; a = r.random() * .5 - .25; l = r.random() * 70 + 15
        wrap_lines(dr, N, N, [(x, y), (x + np.cos(a) * l, y + np.sin(a) * l)], 1)
    sc = blur(np.asarray(img).astype(np.float32) / 255, .7)
    col = hexc("#E3E1D9")[None, None, :] * (0.97 + .04 * spectral(N, N, 1.6, 123)[..., None]) * (1 - .18 * sc[..., None])
    write("Run_HospitalWall", col, normal_from_height(.6 * peel - .3 * sc, 1.0), mask(.55 + .06 * peel - .2 * sc, np.ones_like(peel)))


def exit_sign():
    W, H = 1024, 512
    img = Image.new("RGB", (W, H), (226, 224, 216))
    dr = ImageDraw.Draw(img)
    font = ImageFont.truetype("/System/Library/Fonts/Supplemental/Arial Bold.ttf", 300)
    text = "EXIT"
    bb = dr.textbbox((0, 0), text, font=font)
    tw, th = bb[2] - bb[0], bb[3] - bb[1]
    ox, oy = (W - tw) // 2 - bb[0], (H - th) // 2 - bb[1] - 10
    dr.text((ox, oy), text, font=font, fill=(200, 26, 22))
    for sgn, x in ((-1, 70), (1, W - 70)):  # chevrons both directions
        dr.polygon([(x, H // 2), (x - sgn * 40, H // 2 - 46), (x - sgn * 40, H // 2 + 46)], fill=(200, 26, 22))
    a = np.asarray(img).astype(np.float32) / 255
    red = (a[..., 0] > .6) & (a[..., 1] < .3)
    emit = np.zeros_like(a)
    emit[red] = (1.0, .10, .06)
    emit = np.maximum(blur(emit, 2.0) * .6, emit)
    emit += np.array([.10, .02, .015]) * 0.5  # diffused panel glow
    write("Run_ExitSign", a, None, None, np.clip(emit, 0, 1))


# ======================================================== shared
def veneer():
    W, H = 1024, 2048
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    warp = (spectral(H, W, 2.8, 41) - .5) * 26 + (band(H, W, 4, 12, 42) - .5) * 5
    rings = np.sin((xx + warp) * 2 * np.pi / 22.0 + 2.2 * np.sin(yy / H * 2 * np.pi * 2 + xx / W * 6))
    fine = band(H, W, 200, 700, 43)
    pores = blur((band(H, W, 400, 1000, 44) > .80).astype(np.float32), .6)
    g = .5 + .5 * rings
    light, dark = hexc("#BE8A4E"), hexc("#8A5A2E")
    col = dark + (light - dark) * (0.55 * g + .45 * fine)[..., None]
    col *= (1 - .16 * pores[..., None]) * (0.92 + .10 * spectral(H, W, 1.7, 45)[..., None])
    v = 1 - yy / H
    kick = np.clip((0.12 - v) / .12, 0, 1) * (0.5 + .5 * band(H, W, 30, 120, 46))
    col *= (1 - .25 * kick[..., None])
    height = .4 * g + .4 * fine - .8 * pores
    write("DoorVeneer", col, normal_from_height(blur(height, .5), 1.2), mask(.48 - .18 * kick - .10 * pores, 1 - .3 * pores))


def painted_metal():
    N = 1024
    peel = band(N, N, 60, 260, 51)
    grime = spectral(N, N, 1.5, 52)
    img = Image.new("L", (N, N), 0); dr = ImageDraw.Draw(img)
    r = np.random.default_rng(53)
    for _ in range(140):
        x, y = r.random() * N, r.random() * N; a = r.random() * np.pi; l = r.random() * 40 + 8
        wrap_lines(dr, N, N, [(x, y), (x + np.cos(a) * l, y + np.sin(a) * l)], 1)
    scr = blur(np.asarray(img).astype(np.float32) / 255, .5)
    col = hexc("#DAD4C4")[None, None, :] * (0.94 + .06 * grime[..., None]) * (1 - .10 * scr[..., None])
    write("PaintedMetal", col, normal_from_height(peel * .6 - scr * .4, 1.0), mask(.52 + .08 * peel - .25 * scr - .08 * grime, np.ones_like(peel)))


def macro():
    N = 1024
    R = spectral(N, N, 1.7, 61)
    G = band(N, N, 12, 60, 62)
    B = blur(np.clip((band(N, N, 3, 9, 63) - .66) / .16, 0, 1), 4)
    A = np.clip((resize(spectral(N, N * 8, 1.4, 64), N, N) - .55) / .35, 0, 1)
    save_rgba(f"{OUT}/MacroWear_M.png", np.stack([R, G, B, A], -1))


# The default run: every texture a material uses (written to <outdir>, i.e. Resources).
ALL = dict(wallpaper_paper=wallpaper_paper,
           carpet=carpet, ceiling=ceiling, lens=lens,
           office_carpet=office_carpet, drywall=drywall, office_ceiling=office_ceiling, louver=louver, fabric=fabric,
           vct=vct, hospital_wall=hospital_wall, exit_sign=exit_sign, veneer=veneer, metal=painted_metal, macro=macro)
# Run only when named. The legacy one-layer wallpapers and the T1 keep_hue = 0 references feed
# the P0 parity test (FrontRoomsPrintP0Test); no material uses them, so write them OUTSIDE
# Resources: <outdir> = Assets/Editor/Rendering/PrintP0/Ref. So does wallpaper_print (the CC0
# frame 0, Wallpaper_Print_CC0_P; the production print comes from Build Print Array since Q1b).
# print_encode_lut writes print_encode_lut.json next to this file (for the print tools).
EXTRA = dict(wallpaper=wallpaper, wallpaper_cold=wallpaper_cold, wallpaper_t1_reference=wallpaper_t1_reference,
             wallpaper_print=wallpaper_print, print_encode_lut=print_encode_lut_json)

if __name__ == "__main__":
    for name in (sys.argv[3:] or ALL.keys()):
        {**ALL, **EXTRA}[name]()
        print("done", name, flush=True)
