"""Door wood: the door leaf and frame textures (游戏视觉, 2026-10-30). numpy + Pillow only, no downloads, deterministic.

Red's pick (2026-10-30, 游戏视觉 chat): option 4, plain-sliced WALNUT (book match, running, satin lacquer), with
"我希望稍微亮百分之15即可": the FINAL albedo is option 4's x 1.15 in LINEAR light (leaf and frame alike, so they stay
matched); a uniform linear scale keeps chromaticity, i.e. hue and saturation. Normals and masks are option 4's, byte for
byte. Research and the four options: Documentation/research/door_wood (40_for_red.md, media_candidates.md).

Run (from the project root):
  python Tools/lookdev/gen_door_wood.py Assets/Resources/Surfaces/Textures Tools/lookdev/cc0_src/polyhaven
      no names = FINAL: DoorVeneer_{A,N,S}.png   the leaf face (Door_Veneer: title / station / stream / D1.6 / stair doors)
                        DoorFrame_{A,N,S}.png    frame stock, grain along V (Door_Frame: Kit_DoorFrame_Wood_DA)
                        DoorFrameU_{A,N,S}.png   the same stock turned, grain along U (Door_FrameU: the title kit's
                                                 casing, FrontRoomsDoubleDoorKit, whose U runs along the casing)
                        preview_final.jpg (flat albedo; only when <out_dir> is outside a Resources folder)
  python Tools/lookdev/gen_door_wood.py <out_dir> <cc0_polyhaven_dir> 1 2 3 4
      the 2026-10-30 comparison set exactly as shown to Red (DW<N>_*, no lift): research only, never into Resources.
Red, 2026-10-30, on the landed leaf: "目前这个门的小纹路可以，但是大的纹路看起来不太对". The ring model's arches read as closed
eyes. A CC0 scan face landed first; Red then asked for options and picked G, this model with a one-way cut (FINAL_FACE):
the fine grain stays, the arches open. Six faces: research/door_wood/images/door_veneer_options.jpg, W/door_wood/v4.
Importer: Resources/Surfaces/Textures/* is set by FrontRoomsSurfaceTextureImporter (FrontRoomsRenderSetup.cs); the
materials are FrontRoomsRenderSetup SurfaceDefs Door_Veneer (tile 1.25 x 2.75), Door_Frame (0.5 x 2.0), Door_FrameU
(2.0 x 0.5). Replaces gen_surfaces.py veneer() (the 2.38 cm sine stripes), which no longer writes DoorVeneer.

What it models (a 1990 commercial flush door, 1-3/4 in solid core, factory finished):
  * the face is ONE texture over the whole leaf (tile 1.25 x 2.75 m, 2048 x 4096 albedo = 1638 x 1489 px/m), so no
    tile repeat shows on a leaf: the title stream leaf (1.1955 x 2.639 m, UV from its min corner) and the D1.6 leaf
    (0.99 x 2.08 m) both sit inside one tile. U = 0 / 1.25 is always a veneer-leaf joint, so a wrap reads as a joint.
  * veneer leaves 0.15-0.32 m wide cut from one flitch: book match (every other leaf mirrored) or slip match
    (leaves repeated in sequence), running assembly (widths vary, a part leaf at one edge), small per-leaf tone and the
    book-match "barber pole" (tight / loose face) shift, a hairline at each joint.
  * growth rings with variable widths, grouped intensities (wet / dry years), sharp latewood -> earlywood boundary,
    plain slicing through a tilted, tapered log -> cathedral arches in the leaf middle, straight grain at its edges
    (r = sqrt((x - pith(y))^2 + depth(y)^2) + eccentricity noise); rift = straight comb grain; rotary = a peeled sheet
    whose depth wanders -> broad wild figure, one piece, no joints.
  * open pores (oak: in the earlywood), fibre streaks, and the CC0 Poly Haven scan already in the project
    (Tools/lookdev/cc0_src/polyhaven, approved by Red 2026-10-02) as high-pass micro detail only.
  * lacquer (TR-2 catalysed lacquer, satin): the surface is FLAT. Normal = pore dimples + a faint telegraph of the
    grain only; smoothness 0.66-0.72 on the wood, lower in the pores (one URP specular lobe standing in for the coat).
  * a matching solid-wood frame stock texture, seamless (0.5 x 2.0 m, grain along V) + a copy with the grain along U
    for the title kit's frame meshes (their U runs along the casing).

Sizes: leaf A 2048x4096 sRGB, leaf N 1024x2048 (OpenGL +Y), leaf S 1024x2048 (R smoothness, G cavity); frame
{A,N,S} 1024x2048 (V) / 2048x1024 (U).
"""
import os, sys
import numpy as np
from PIL import Image

OUT = sys.argv[1]
CC0 = sys.argv[2]
ARGS = sys.argv[3:] or ["final"]
ONLY = [a if a == "final" else int(a) for a in ARGS]
os.makedirs(OUT, exist_ok=True)

TILE_U, TILE_V = 1.25, 2.75          # metres per face tile
FW, FH = 2048, 4096                   # face albedo
FRAME_U, FRAME_V = 0.5, 2.0           # metres per frame tile
RW, RH = 1024, 2048                   # frame textures


# ------------------------------------------------------------------ helpers
def to_lin(c):
    c = np.asarray(c, np.float32)
    return np.where(c <= .04045, c / 12.92, ((c + .055) / 1.055) ** 2.4)


def to_srgb(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= .0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - .055)


def hexc(s):
    return np.array([int(s[i:i + 2], 16) / 255 for i in (1, 3, 5)], np.float32)


def gnoise(h, w, sx, sy, seed):
    """Periodic gaussian-correlated noise, correlation sx px across (columns), sy px along (rows); zero mean, unit std."""
    r = np.random.default_rng(seed)
    F = np.fft.fft2(r.standard_normal((h, w)).astype(np.float32))
    fy = np.fft.fftfreq(h)[:, None]
    fx = np.fft.fftfreq(w)[None, :]
    g = np.exp(-2 * (np.pi ** 2) * ((fx * sx) ** 2 + (fy * sy) ** 2))
    n = np.real(np.fft.ifft2(F * g)).astype(np.float32)
    n -= n.mean()
    return n / max(1e-8, n.std())


def gnoise1(n, s, seed):
    r = np.random.default_rng(seed)
    F = np.fft.fft(r.standard_normal(n))
    f = np.fft.fftfreq(n)
    x = np.real(np.fft.ifft(F * np.exp(-2 * (np.pi * f * s) ** 2)))
    x -= x.mean()
    return (x / max(1e-8, x.std())).astype(np.float32)


def blur(img, sigma):
    h, w = img.shape[:2]
    fy = np.fft.fftfreq(h)[:, None]
    fx = np.fft.fftfreq(w)[None, :]
    g = np.exp(-2 * (np.pi * sigma) ** 2 * (fx * fx + fy * fy))
    if img.ndim == 2:
        return np.real(np.fft.ifft2(np.fft.fft2(img) * g)).astype(np.float32)
    return np.stack([blur(img[..., c], sigma) for c in range(img.shape[2])], -1)


def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def ring_table(n, mean_w, cv, group_amp, seed, total=None):
    """Growth-ring boundaries R[0..n] (metres) and per-ring latewood intensity. Widths vary ring to ring (cv) and in
    groups of years (group_amp, ~4-ring correlation); intensity follows the groups. total: force sum(widths)."""
    r = np.random.default_rng(seed)
    g = gnoise1(n, 3.0, seed + 1)
    w = mean_w * np.exp(cv * r.standard_normal(n) * .7 + group_amp * g)
    if total is not None:
        w *= total / w.sum()
    R = np.concatenate([[0.0], np.cumsum(w)]).astype(np.float64)
    inten = np.clip(.62 + .30 * gnoise1(n, 2.5, seed + 2) + .12 * r.standard_normal(n), .12, 1.0).astype(np.float32)
    return R, inten


def phase_of(r, R):
    return np.interp(r, R, np.arange(len(R), dtype=np.float64)).astype(np.float32)


def micro_from_scan(name, out_w, out_h, px_per_m_u, px_per_m_v, scan_m=1.0, rotate=True, sigma=5.0):
    """High-pass luminance of a CC0 scan (grain turned to run along V), scaled to the target px/m, tiled. ~unit std."""
    im = Image.open(os.path.join(CC0, name, f"{name}_diff_2k.jpg")).convert("L")
    a = np.asarray(im).astype(np.float32) / 255
    if rotate:
        a = np.rot90(a).copy()
    hp = a - blur(a, sigma)
    hp /= max(1e-6, hp.std())
    sw, sh = max(8, int(round(scan_m * px_per_m_u))), max(8, int(round(scan_m * px_per_m_v)))
    lo, hi = np.percentile(hp, .5), np.percentile(hp, 99.5)
    t = np.asarray(Image.fromarray(np.clip(hp, lo, hi).astype(np.float32), "F").resize((sw, sh), Image.LANCZOS)).astype(np.float32)
    t -= t.mean(); t /= max(1e-6, t.std())
    ys = np.arange(out_h) % sh
    xs = np.arange(out_w) % sw
    return t[ys][:, xs]


def normal_from_height(h, strength):
    dx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * .5
    dr = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * .5
    nx, ny, nz = -dx * strength, dr * strength, np.ones_like(h)
    l = np.sqrt(nx * nx + ny * ny + nz * nz)
    return np.stack([nx / l, ny / l, nz / l], -1)


def half(a):
    """2x2 box downsample (normals renormalised by the caller)."""
    return (a[0::2, 0::2] + a[1::2, 0::2] + a[0::2, 1::2] + a[1::2, 1::2]) * .25


def save_rgb(path, rgb):
    Image.fromarray((np.clip(rgb, 0, 1) * 255 + .5).astype(np.uint8), "RGB").save(path, optimize=False, compress_level=4)


def save_normal(path, n):
    n = n / np.linalg.norm(n, axis=-1, keepdims=True)
    save_rgb(path, n * .5 + .5)


def save_mask(path, smooth, cavity):
    save_rgb(path, np.stack([smooth, cavity, np.zeros_like(smooth)], -1))


# ------------------------------------------------------------------ option definitions
# Colours are sRGB of the FINISHED wood (clear satin lacquer, a few years of ambering), measured by eye against the
# manufacturer swatches listed in media_candidates.md B (not copied). light = earlywood ground, dark = full latewood.
OPTIONS = {
    1: dict(key="plain_red_oak", label="Plain-sliced red oak, book match, running", cut="plain", match="book",
            leaves=[.255, .245, .26, .25, .24], light="#C99E72", dark="#8C5C38", ring=.0042, cv=.35, group=.35,
            depth0=.07, depth_slope=.013, flitch=.30, pores=.85, pore_ew=.30, contrast=1.0, leaf_tone=.035,
            barber=.035, smooth=.68, pore_smooth=.50, scan="red_oak_veneer", micro=.05, seed=101),
    2: dict(key="rotary_birch", label="Rotary natural birch, one piece", cut="rotary", match="none",
            leaves=[1.25], light="#E2CDA9", dark="#C4A074", ring=.0032, cv=.30, group=.30,
            pores=.20, pore_ew=1.0, contrast=.40, leaf_tone=0.0, barber=0.0, smooth=.70, pore_smooth=.64,
            scan="walnut_veneer", micro=.012, seed=202),
    3: dict(key="rift_red_oak", label="Rift-sliced red oak, slip match", cut="rift", match="slip",
            leaves=[.16, .155, .16, .15, .165, .155, .15, .155], light="#C29468", dark="#8E5E3A", ring=.0036, cv=.35,
            group=.45, pores=.75, pore_ew=.30, contrast=1.05, leaf_tone=.025, barber=0.0, smooth=.68, pore_smooth=.52,
            scan="red_oak_veneer", micro=.05, seed=303),
    4: dict(key="plain_walnut", label="Plain-sliced walnut, book match, running", cut="plain", match="book",
            leaves=[.305, .315, .32, .31], light="#7E5A40", dark="#4A3122", ring=.0050, cv=.40, group=.40,
            depth0=.08, depth_slope=.012, flitch=.36, pores=.45, pore_ew=.45, contrast=.9, leaf_tone=.045,
            barber=.03, smooth=.72, pore_smooth=.60, scan="walnut_veneer", micro=.05, seed=404),
}


# Red 2026-10-30: option 4 + "稍微亮百分之15": albedo x 1.15 in linear light (FINAL_LIFT), nothing else changes.
FINAL_OPTION = 4
FINAL_LIFT = 1.15
# Red 2026-10-30: the leaf's closed "eyes" were wrong, and of six faces rendered in game Red picked G: this model with open
# arches. These override option 4 for the FINAL leaf only, so the frame stock and the research options stay as they were.
FINAL_FACE = dict(depth0=.075, depth_slope=.034, arch_p=1.75, wobble=.0016, band_fade=(.010, .028), line_break=.12, contrast=.85)


def ring_colour(p, inten, o):
    """Latewood darkness 0..1 along one ring (p 0 = ring start / earlywood, 1 = end of latewood)."""
    late = smoothstep(.30, .97, p) ** 1.3
    early = (1 - smoothstep(0.0, .22, p)) * .30
    d = (late * (.55 + .45 * inten) + early * inten) * o["contrast"]
    return np.clip(d, 0, 1)


# ------------------------------------------------------------------ face
def face(n, o, lift=1.0):
    s = o["seed"]
    H, W = FH, FW
    du, dv = TILE_U / W, TILE_V / H
    y = (H - 1 - np.arange(H)) * dv + dv * .5           # metres up the leaf (row 0 = top of the texture = v max)
    x = np.arange(W) * du + du * .5
    edges = np.concatenate([[0.0], np.cumsum(o["leaves"])])
    edges[-1] = TILE_U
    leaf = np.clip(np.searchsorted(edges, x, side="right") - 1, 0, len(o["leaves"]) - 1)
    wl = np.array(o["leaves"])[leaf]
    xl = x - edges[leaf]                                  # 0..w inside the leaf
    xf = xl - wl * .5                                     # flitch coordinate, centred
    if o["match"] == "book":
        xf = np.where(leaf % 2 == 1, -xf, xf)
    rng = np.random.default_rng(s)
    R, inten = ring_table(400, o["ring"], o["cv"], o["group"], s + 10)

    # shared flitch-space noise (same for every leaf of the flitch: index by the flitch column, mirrored with xf)
    fcols = 768
    fspan = max(o.get("flitch", .3), .3)
    fidx = np.clip(((xf + fspan * .5) / fspan * fcols).astype(np.int32), 0, fcols - 1)
    ecc = gnoise(H, fcols, 60, 500, s + 20)[:, fidx] * .0018 + gnoise(H, fcols, 14, 120, s + 21)[:, fidx] * .0004

    if o["cut"] == "plain":
        pith = .018 * gnoise1(H, 420, s + 30) + .006 * (y - 1.3)          # sweep + lean of the log
        if "arch_p" in o:
            # The knife only ever moves one way through the log, so depth is MONOTONIC up the leaf: arches stay open and
            # stack one way. (A depth that wanders back cuts the same ring twice: the closed "eyes" Red rejected 10-30.)
            rate = o["depth_slope"] * np.clip(1 + .75 * gnoise1(H, 300, s + 31), .22, 2.2)
            depth = o["depth0"] + np.cumsum(rate[::-1])[::-1] * dv
            depth = depth[::-1] if y[0] < y[-1] else depth
            depth = o["depth0"] + (depth.max() - depth)
            q = o["arch_p"]                                                # < 2: the arches come to a point, not a dome
            ax = np.abs(xf[None, :] - pith[:, None])
            ad = (depth[:, None] + leaf[None, :] * .0007)
            wob = gnoise(H, fcols, 26, 80, s + 22)[:, fidx] * o["wobble"]  # the rings are not drawn with a compass
            rr = (ax ** q + ad ** q) ** (1 / q) + ecc + wob
        else:
            depth = o["depth0"] + o["depth_slope"] * y + .005 * gnoise1(H, 520, s + 31) + .0015 * gnoise1(H, 140, s + 32)
            rr = np.sqrt((xf[None, :] - pith[:, None]) ** 2 + (depth[:, None] + leaf[None, :] * .0007) ** 2) + ecc
    elif o["cut"] == "rift":
        lean = .0025 * (y - 1.3) + .0012 * gnoise1(H, 420, s + 30)
        rr = .12 + xf[None, :] * .97 + lean[:, None] + ecc * .6
    else:  # rotary: a peeled sheet; the depth below the surface wanders, broad figure elongated along the grain
        wav = gnoise(H, W, 220, 1100, s + 40) * .008 + gnoise(H, W, 45, 300, s + 41) * .0025 + gnoise(H, W, 12, 90, s + 42) * .0005
        rr = .16 + wav + .0035 * np.sin(2 * np.pi * x / TILE_U)[None, :]
    ph = phase_of(rr.astype(np.float64), R)
    k = np.clip(ph.astype(np.int32), 0, len(inten) - 1)
    p = ph - np.floor(ph)
    dark = ring_colour(p, inten[k], o)
    if "band_fade" in o:
        # Where the knife runs along a ring its one dark ramp stretches across centimetres and reads as a painted shadow:
        # the ring's contrast falls away with its apparent width on the face. Lines break up a little along the grain.
        gy, gx = np.gradient(rr.astype(np.float32), dv, du)
        wide = o["ring"] / np.maximum(np.hypot(gx, gy), 1e-4)
        keep = 1 - smoothstep(o["band_fade"][0], o["band_fade"][1], blur(wide.astype(np.float32), 2.0))
        dark = np.float32(dark.mean()) + (dark - dark.mean()) * keep
        brk = gnoise(H, W, 1.6, 34, s + 53) * .6 + gnoise(H, W, 5.0, 110, s + 54) * .4
        dark = np.clip(dark * (1 + o["line_break"] * np.clip(brk, -1.6, 1.6)), 0, 1)

    # fibre streaks along the grain, broad tone, per-leaf tone + barber pole
    fib = gnoise(H, W, .8, 18, s + 50) * .5 + gnoise(H, W, 2.5, 60, s + 51) * .5
    broad = gnoise(H, W, 120, 500, s + 52)
    lt = to_lin(hexc(o["light"]))
    dk = to_lin(hexc(o["dark"]))
    col = lt[None, None, :] * (1 - dark[..., None]) + dk[None, None, :] * dark[..., None]
    tone = 1 + .025 * fib + .035 * broad
    nl = len(o["leaves"])
    ltone = 1 + o["leaf_tone"] * rng.standard_normal(nl).clip(-1.6, 1.6)
    if o["match"] == "book":
        ltone *= np.where(np.arange(nl) % 2 == 1, 1 - o["barber"], 1 + o["barber"] * .3)
    tone = tone * ltone[leaf][None, :]
    hue = 1 + .02 * rng.standard_normal((nl, 3)).clip(-1.5, 1.5)   # slight hue difference leaf to leaf
    col = col * tone[..., None] * hue[leaf][None, :, :]
    # micro detail from the scan (high-pass only), as luminance
    if o["micro"] > 0:
        mic = micro_from_scan(o["scan"], W, H, W / TILE_U, H / TILE_V)
        col = col * (1 + o["micro"] * np.clip(mic, -3, 3))[..., None]
    # pores: short streaks, dense in the earlywood (ring-porous oak), sparse elsewhere
    pn = gnoise(H, W, .55, 3.5, s + 60)
    ew = 1 - smoothstep(o["pore_ew"] * .7, o["pore_ew"], p)
    thr = np.where(ew > .5, 1.35, 2.3)
    pores = np.clip((pn - thr) / .6, 0, 1) * o["pores"]
    pores = np.maximum(pores, 0)
    col = col * (1 - .42 * pores)[..., None]
    # joints: a hairline (glue line, the leaves' edges a shade darker)
    jd = np.min(np.abs(x[:, None] - edges[None, :]), axis=1)
    if nl > 1:
        col = col * (1 - .10 * np.exp(-(jd / (du * .7)) ** 2))[None, :, None]
    # foot of the leaf: shoe scuffs, faint
    scuff = np.clip(gnoise(H, W, 40, 12, s + 70) - .6, 0, None) * (1 - smoothstep(.05, .22, y))[:, None]
    col = col * (1 - .10 * np.clip(scuff, 0, 1))[..., None]
    col = blur(col, .45)
    if lift != 1.0:
        col = col * lift                                  # FINAL: linear scale (chromaticity kept), not in the options
    alb = to_srgb(col)

    # lacquer: flat; pores dimple, the grain telegraphs faintly
    hgt = -blur(pores, .6) * 1.0 + .10 * blur(dark, 2.0)
    nrm = normal_from_height(hgt, 1.6)
    nrm = half(nrm)
    sm = o["smooth"] - (o["smooth"] - o["pore_smooth"]) * blur(pores, .6) - .02 * np.clip(fib, -2, 2) * .5 - .10 * np.clip(scuff, 0, 1)
    cav = 1 - .30 * blur(pores, .6)
    return alb, nrm, half(sm), half(cav), dict(edges=edges)


# ------------------------------------------------------------------ frame stock (seamless, grain along V)
def frame(n, o, lift=1.0):
    s = o["seed"] + 500
    H, W = RH, RW
    du, dv = FRAME_U / W, FRAME_V / H
    x = np.arange(W) * du
    nr = int(round(FRAME_U / (o["ring"] * 1.1)))
    R, inten = ring_table(nr, o["ring"] * 1.1, o["cv"], o["group"], s, total=FRAME_U)
    wob = gnoise(H, W, 90, 700, s + 1) * .004 + gnoise(H, W, 12, 140, s + 2) * .0006
    rr = (x[None, :] + wob) % FRAME_U
    ph = phase_of(rr.astype(np.float64), R)
    k = np.clip(ph.astype(np.int32), 0, len(inten) - 1)
    p = ph - np.floor(ph)
    o2 = dict(o); o2["contrast"] = o["contrast"] * .9
    dark = ring_colour(p, inten[k], o2)
    fib = gnoise(H, W, .8, 18, s + 3) * .5 + gnoise(H, W, 2.5, 60, s + 4) * .5
    broad = gnoise(H, W, 60, 400, s + 5)
    lt = to_lin(hexc(o["light"])) * .94                  # solid stock takes the finish a shade deeper
    dk = to_lin(hexc(o["dark"])) * .94
    col = lt[None, None, :] * (1 - dark[..., None]) + dk[None, None, :] * dark[..., None]
    col = col * (1 + .025 * fib + .04 * broad)[..., None]
    if o["micro"] > 0:
        mic = micro_from_scan(o["scan"], W, H, W / FRAME_U, H / FRAME_V)
        col = col * (1 + o["micro"] * np.clip(mic, -3, 3))[..., None]
    pn = gnoise(H, W, .55, 3.5, s + 6)
    ew = 1 - smoothstep(o["pore_ew"] * .7, o["pore_ew"], p)
    pores = np.clip((pn - np.where(ew > .5, 1.35, 2.3)) / .6, 0, 1) * o["pores"]
    col = col * (1 - .42 * pores)[..., None]
    col = blur(col, .45)
    if lift != 1.0:
        col = col * lift                                  # FINAL: linear scale (chromaticity kept), not in the options
    alb = to_srgb(col)
    hgt = -blur(pores, .6) + .10 * blur(dark, 2.0)
    nrm = normal_from_height(hgt, 1.6)
    sm = o["smooth"] - .02 - (o["smooth"] - o["pore_smooth"]) * blur(pores, .6)
    cav = 1 - .30 * blur(pores, .6)
    return alb, nrm, sm, cav


def rot_u(a):
    """Grain along V -> along U (the title kit's frame meshes): turn 90 deg. Normal x/y swap with the turn."""
    return np.rot90(a, k=-1).copy()


def rot_normal_u(n):
    r = rot_u(n)
    # after a clockwise turn of the image, old +V (up) points to +U (right) and old +U to -V
    nx, ny = r[..., 0].copy(), r[..., 1].copy()
    r[..., 0], r[..., 1] = ny, -nx
    return r


def preview(n, alb, falb, edges):
    """Flat albedo: the title pair (1.1955 x 2.639 m each, as UV'd), a D1.6 leaf (0.99 x 2.08), the frame, 300 px/m."""
    if "/Resources/" in os.path.abspath(OUT) + "/":
        return                                            # never a stray JPEG in Resources (it would import and ship)
    ppm = 300
    def crop(u0, u1, v0, v1):
        H, W = alb.shape[:2]
        c0, c1 = int(u0 / TILE_U * W), int(u1 / TILE_U * W)
        r0, r1 = int((1 - v1 / TILE_V) * H), int((1 - v0 / TILE_V) * H)
        im = Image.fromarray((alb[r0:r1, c0:c1] * 255).astype(np.uint8))
        return im.resize((max(1, int((u1 - u0) * ppm)), max(1, int((v1 - v0) * ppm))), Image.LANCZOS)
    a = crop(0, 1.1955, 0, 2.639)
    b = crop(0, .99, .015, 2.095)
    f = Image.fromarray((falb * 255).astype(np.uint8)).resize((int(FRAME_U * ppm), int(FRAME_V * ppm)), Image.LANCZOS)
    W = a.width * 2 + 10 + b.width + 10 + f.width + 20
    sheet = Image.new("RGB", (W, a.height + 20), (240, 238, 232))
    sheet.paste(a, (10, 10)); sheet.paste(a, (10 + a.width + 3, 10))
    sheet.paste(b, (10 + 2 * a.width + 13, 10 + a.height - b.height))
    sheet.paste(f, (W - f.width - 10, 10))
    sheet.save(os.path.join(OUT, f"preview_{n}.jpg"), quality=88)


def lum(a):
    """Mean linear (Rec. 709) luminance of an sRGB float image, as the 8-bit file will hold it."""
    q = np.round(np.clip(a, 0, 1) * 255) / 255
    l = to_lin(q)
    return float((l[..., 0] * .2126 + l[..., 1] * .7152 + l[..., 2] * .0722).mean())


for n in ONLY:
    final = n == "final"
    o = OPTIONS[FINAL_OPTION if final else n]
    lift = FINAL_LIFT if final else 1.0
    leaf_stem, fv_stem, fu_stem = ("DoorVeneer", "DoorFrame", "DoorFrameU") if final else (f"DW{n}_Leaf", f"DW{n}_FrameV", f"DW{n}_FrameU")
    alb, nrm, sm, cav, info = face(n, dict(o, **FINAL_FACE) if final else o, lift)
    save_rgb(os.path.join(OUT, f"{leaf_stem}_A.png"), alb)
    save_normal(os.path.join(OUT, f"{leaf_stem}_N.png"), nrm)
    save_mask(os.path.join(OUT, f"{leaf_stem}_S.png"), np.clip(sm, 0, 1), np.clip(cav, 0, 1))
    falb, fn, fsm, fcav = frame(n, o, lift)
    save_rgb(os.path.join(OUT, f"{fv_stem}_A.png"), falb)
    save_normal(os.path.join(OUT, f"{fv_stem}_N.png"), fn)
    save_mask(os.path.join(OUT, f"{fv_stem}_S.png"), np.clip(fsm, 0, 1), np.clip(fcav, 0, 1))
    save_rgb(os.path.join(OUT, f"{fu_stem}_A.png"), rot_u(falb))
    save_normal(os.path.join(OUT, f"{fu_stem}_N.png"), rot_normal_u(fn))
    save_mask(os.path.join(OUT, f"{fu_stem}_S.png"), np.clip(rot_u(fsm), 0, 1), np.clip(rot_u(fcav), 0, 1))
    preview(n, alb, falb, info["edges"])
    m = alb.reshape(-1, 3).mean(0) * 255
    nxy = np.sqrt(nrm[..., 0] ** 2 + nrm[..., 1] ** 2).mean()
    print(f"{'FINAL (option %d x %.2f linear)' % (FINAL_OPTION, lift) if final else 'option %s' % n} {o['key']}: "
          f"albedo mean sRGB ({m[0]:.0f},{m[1]:.0f},{m[2]:.0f}), mean linear Y leaf {lum(alb):.4f} frame {lum(falb):.4f}, "
          f"normal mean |xy| {nxy:.3f}, smoothness mean {np.mean(sm):.2f}, leaves {len(o['leaves'])}")
