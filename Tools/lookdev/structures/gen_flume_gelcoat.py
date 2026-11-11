"""The slide's gel coat maps (structures G2-b, line visual (game visual), 2026-11-08; Documentation/research/structures_assets/20_research.md
A.7.2, 60_build.md G2-b). numpy + Pillow only, no downloads, deterministic (fixed seeds).

Run (from the project root):
  python Tools/lookdev/structures/gen_flume_gelcoat.py Assets/Resources/Surfaces/Textures      -> FlumeGelcoat_{N,S}.png, FlumeTrack_{A,N,S}.png
  python Tools/lookdev/structures/gen_flume_gelcoat.py <dir> preview                            -> + preview_FlumeTrack.png

What it is. The flume's shell is moulded FRP like the pool doors (Tools/lookdev/gen_door_gelcoat.py: orange peel, chopped-strand
print-through, pigment mottle; the shell keeps the door's albedo map, DoorGelcoat_A), but waxed to a high gloss (ProSlide's guidance,
PC30: monthly paste wax): its orange peel shows calm in a highlight and the mat's print-through only at grazing light (A.7.2). The door's
own normal and mask (made for a 0.78 door face at 0.35 m) drew a web of strands in a 0.87 highlight at 1.25 m (job g2b_1), so:
  FlumeGelcoat_N  linear normal (OpenGL +Y), 1024^2 over 0.6 m (the door's tile): the peel (2-4 mm) and the flow-out swell (6-12 mm) at
                  the door's spectra, the strands at a fifth of the door's weight; tilt mean about 0.6 deg at _BumpScale 1
  FlumeGelcoat_S  linear mask: R smoothness 0.87 (a waxed gel coat) +- 0.006 with the peel only, G cavity 1, B 0, A 1
Along the trough's bottom the riders' bodies and the water wear a lane (the wax rubbed off and put back): a polished centre, fine
scuffs ALONG the ride, and, where the 2-3 cm film's edges sit on the walls (0.275 m of arc each side), a faint chalky scale line left
by the evaporating film (BR:T9, 20_research.md A.6.1). Its strip (the flume builder lays u along the ride and v across the lane: v 0
at -0.32 m of arc, 0.64 at +0.32; _TileSize (2.0, 0.64)):
  FlumeTrack_A    sRGB albedo, near white like the door's (the material's _BaseColor is the ivory); the scale lines +1.5 %, chalky
  FlumeTrack_N    linear normal: the flow-out swell (calmer in the polished lane), the scuffs as faint grooves along u
  FlumeTrack_S    linear mask: R smoothness 0.87 at the strip's edges (= the shell), 0.92 polished, 0.70 on the scale lines
The strip's long edges match the shell's mean look, so the lane fades into the shell with no seam. No text, label, brand or logo.
ASCII only.
"""

import os
import sys

import numpy as np
from PIL import Image

W, H = 1024, 256                 # u (along, 2.0 m), v (across, 0.64 m)
U_M, V_M = 2.0, 0.64
PX_U = W / (U_M * 1000.0)        # texels per mm along
PX_V = H / (V_M * 1000.0)        # texels per mm across


def band(h, w, lo, hi, seed, sx=1.0 / U_M, sy=1.0 / V_M):
    """Periodic band-limited noise between lo and hi cycles per METRE (sx, sy: 1 / the tile's metres per axis; a larger sx than
    1 / U_M stretches the features along u), zero mean, unit std."""
    r = np.random.default_rng(seed)
    F = np.fft.fft2(r.standard_normal((h, w)))
    fy = np.fft.fftfreq(h)[:, None] * h * sy
    fx = np.fft.fftfreq(w)[None, :] * w * sx
    f = np.sqrt(fx * fx + fy * fy)
    mask = np.exp(-((np.log(np.maximum(f, 1e-3)) - np.log((lo * hi) ** .5)) ** 2) / (2 * (np.log(hi / lo) / 2.5) ** 2))
    mask[0, 0] = 0
    n = np.real(np.fft.ifft2(F * mask))
    return ((n - n.mean()) / max(n.std(), 1e-8)).astype(np.float64)


def normal_from_height(hgt, su, sv):
    """Central differences (u wraps; v clamps at the strip's edges), OpenGL +Y (v up runs against the image rows)."""
    du = (np.roll(hgt, -1, 1) - np.roll(hgt, 1, 1)) * .5
    pad = np.pad(hgt, ((1, 1), (0, 0)), mode="edge")
    drow = (pad[2:, :] - pad[:-2, :]) * .5
    nx = -du * su
    ny = drow * sv
    nz = np.ones_like(hgt)
    l = np.sqrt(nx * nx + ny * ny + nz * nz)
    return np.stack([nx / l, ny / l, nz / l], -1)


def lin_to_srgb(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= .0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - .055)


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def build():
    # across the lane: x in metres, 0 on the trough's bottom line; the image's row 0 is the top (v = 1, x = +0.32)
    x = (1.0 - (np.arange(H) + .5) / H) * V_M - V_M / 2
    x = np.repeat(x[:, None], W, axis=1)
    ax = np.abs(x)
    # the polished lane: the riders' hips and backs, about 0.36 m wide, softest at its edges
    lane = 1.0 - smooth(0.12, 0.20, ax)
    # the scale lines where the film's edges sit (|x| ~ 0.26), wandering a few mm along the ride, 4-7 mm wide, broken in places
    wander = band(1, W, 1.5, 6.0, 71204)[0] * 0.004 + band(1, W, 10.0, 30.0, 71205)[0] * 0.0012
    width = 0.0025 + 0.0012 * (band(1, W, 2.0, 8.0, 71206)[0] * .5 + .5)
    broken = smooth(-0.6, 0.2, band(1, W, 1.0, 4.0, 71207)[0])
    scale = np.zeros((H, W))
    for side in (-1.0, 1.0):
        d = np.abs(x - side * (0.275 + wander[None, :]))
        scale = np.maximum(scale, (1.0 - smooth(width[None, :] * .5, width[None, :] * 1.6, d)) * broken[None, :])
    # the gel coat's flow-out swell (the door's 6-12 mm band: its 2-4 mm peel is under this map's 2 mm texel), calmer in the lane
    peel = band(H, W, 83.0, 166.0, 71210)
    # scuffs along the ride: 0.1-0.6 m long (u stretched 10 x), 6-12 mm apart across; many in the lane's centre, a few anywhere
    sc = band(H, W, 80.0, 160.0, 71211, sx=10.0 / U_M)
    scuff = np.clip(sc - 1.4, 0, None) * (0.35 + 0.65 * lane)
    # height (texel-relative): peel everywhere (the lane at 70 %), the scuffs as fine grooves
    hgt = 0.50 * peel * (1.0 - 0.30 * lane) - 0.60 * scuff
    n = normal_from_height(hgt, 0.050, 0.050 * (PX_U / PX_V))
    # albedo: the door gel coat's near white with a soft mottle; the lane a touch greyer (the water's residue rubbed in, 0.6 %);
    # the scale lines chalky (+1.5 %)
    mottle = band(H, W, 3.0, 15.0, 71212)
    lin = 0.985 * (1.0 + 0.0060 * np.clip(mottle, -2, 2)) * (1.0 - 0.006 * lane) * (1.0 + 0.015 * scale)
    albedo = np.repeat(lin_to_srgb(np.clip(lin, 0, 1))[..., None], 3, axis=2)
    # smoothness: 0.87 off the lane (= the shell), 0.92 polished, the scuffs a little duller, the scale lines matte-chalky
    sm = 0.87 + 0.05 * lane - 0.03 * np.clip(scuff, 0, 2) + 0.012 * np.clip(peel, -2, 2) * (1.0 - lane)
    sm = sm * (1.0 - scale) + 0.70 * scale
    sm = np.clip(sm, 0.62, 0.95)
    s = np.stack([sm, np.ones_like(sm), np.zeros_like(sm), np.ones_like(sm)], -1)
    return albedo, n, s, lane, scale


GN = 1024
G_TILE_M = 0.6


def gband(lo, hi, seed):
    """Periodic band-limited noise over the shell's 0.6 m tile (cycles per tile), zero mean, unit std."""
    r = np.random.default_rng(seed)
    F = np.fft.fft2(r.standard_normal((GN, GN)))
    fy = np.fft.fftfreq(GN)[:, None] * GN
    fx = np.fft.fftfreq(GN)[None, :] * GN
    f = np.sqrt(fx * fx + fy * fy)
    mask = np.exp(-((np.log(np.maximum(f, 1e-3)) - np.log((lo * hi) ** .5)) ** 2) / (2 * (np.log(hi / lo) / 2.5) ** 2))
    mask[0, 0] = 0
    n = np.real(np.fft.ifft2(F * mask))
    return (n - n.mean()) / max(n.std(), 1e-8)


def build_shell():
    """FlumeGelcoat_N / _S: the door's peel and swell spectra (same seeds), the strands faint."""
    peel = gband(150.0, 300.0, 61101)
    swell = gband(50.0, 100.0, 61102)
    strands = gband(12.0, 24.0, 61103)            # stands in for the mat's 25-50 mm print-through, a fifth of the door's weight
    hgt = 0.50 * peel + 0.40 * swell + 0.06 * strands
    dx = (np.roll(hgt, -1, 1) - np.roll(hgt, 1, 1)) * .5
    drow = (np.roll(hgt, -1, 0) - np.roll(hgt, 1, 0)) * .5
    k = 0.020
    nx, ny, nz = -dx * k, drow * k, np.ones_like(hgt)
    l = np.sqrt(nx * nx + ny * ny + nz * nz)
    n = np.stack([nx / l, ny / l, nz / l], -1)
    sm = np.clip(0.87 + 0.006 * np.clip(peel, -2, 2), 0.85, 0.89)
    s = np.stack([sm, np.ones_like(sm), np.zeros_like(sm), np.ones_like(sm)], -1)
    return n, s


def save(path, arr, mode):
    Image.fromarray((np.clip(arr, 0, 1) * 255.0 + 0.5).astype(np.uint8), mode).save(path, optimize=True)


def main(argv):
    out = argv[0] if argv else "Assets/Resources/Surfaces/Textures"
    preview = len(argv) > 1 and argv[1] == "preview"
    os.makedirs(out, exist_ok=True)
    gn, gs = build_shell()
    save(os.path.join(out, "FlumeGelcoat_N.png"), gn * 0.5 + 0.5, "RGB")
    save(os.path.join(out, "FlumeGelcoat_S.png"), gs, "RGBA")
    gtilt = np.degrees(np.arccos(np.clip(gn[..., 2], -1, 1)))
    print("FlumeGelcoat: normal tilt mean %.2f deg, p99 %.2f deg; smoothness %.3f +- %.4f" % (gtilt.mean(), np.percentile(gtilt, 99), gs[..., 0].mean(), gs[..., 0].std()))
    albedo, n, s, lane, scale = build()
    save(os.path.join(out, "FlumeTrack_A.png"), albedo, "RGB")
    save(os.path.join(out, "FlumeTrack_N.png"), n * 0.5 + 0.5, "RGB")
    save(os.path.join(out, "FlumeTrack_S.png"), s, "RGBA")
    tilt = np.degrees(np.arccos(np.clip(n[..., 2], -1, 1)))
    edge = np.r_[0:4, H - 4:H]
    print("FlumeTrack: albedo sRGB mean %.4f; smoothness edge rows %.3f (shell 0.87), lane centre %.3f, scale lines min %.3f; "
          "normal tilt mean %.2f deg, p99 %.2f deg; lane share %.2f, scale share %.3f"
          % (albedo.mean(), s[edge, :, 0].mean(), s[H // 2 - 4:H // 2 + 4, :, 0].mean(), s[..., 0].min(), tilt.mean(),
             np.percentile(tilt, 99), lane.mean(), (scale > .5).mean()))
    if preview:
        sh = np.clip(0.5 + 6.0 * (n[..., 0] * 0.6 + n[..., 1] * 0.8), 0, 1)
        tiles = [albedo[..., 0] * 0 + (albedo[..., 0] - .9) * 8, sh, s[..., 0]]
        Image.fromarray((np.clip(np.concatenate(tiles, axis=0), 0, 1) * 255).astype(np.uint8), "L").save(os.path.join(out, "preview_FlumeTrack.png"))


if __name__ == "__main__":
    main(sys.argv[1:])
