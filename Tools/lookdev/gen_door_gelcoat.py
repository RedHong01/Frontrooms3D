"""Door gel coat: the moulded FRP surface of the pool pair (游戏视觉, zone doors Build B, 2026-11-06; Documentation/research/
double_doors/20_design.md Part B, sections B3.1 and B8). numpy + Pillow only, no downloads, deterministic (fixed seeds).

Run (from the project root):
  python Tools/lookdev/gen_door_gelcoat.py Assets/Resources/Surfaces/Textures            -> DoorGelcoat_{A,N,S}.png
  python Tools/lookdev/gen_door_gelcoat.py <dir> preview                                  -> + preview_DoorGelcoat.png

What it is: the white gel coat of a moulded FRP door (Pitt US 4,429,498; Special-Lite's FRP faces; research Part P): a 0.5 mm
pigmented polyester skin cast against a polished mould, over chopped-strand glass mat in polyester. Three things hold it at 0.35 m
(Red, 2026-11-04, on the pool tile: "太平整…看起来特别的假"), none of them a stamp:
  orange peel   a soft waviness 2-4 mm across (the gel coat's flow-out against the mould and its cure), the highlights' grain;
  print-through the chopped strands 25-50 mm long telegraphing through the skin as a faint relief (the glass and the resin
                shrink differently on cure), seen only where a lamp or the water glints on the face at a grazing angle;
  pigment       a +-1.5 % mottle in the white (the pigment paste dispersed by hand in a 1980s shop).
Maps (FrontRooms/Surface, mesh-UV mode, _TileSize 0.6 m: 1024 texels over 0.6 m, 0.59 mm a texel), all periodic (seamless):
  _A  sRGB albedo: near white (linear 0.985 x mottle); the material's _BaseColor carries the gel coat's colour (the leaf's white
      sRGB 230 230 224; the frame's course aqua 102 194 196), so one set serves the leaves, the rings, the frame and the map's PL
      leaf.
  _N  linear normal (OpenGL +Y): the orange peel and the print-through.
  _S  linear mask: R smoothness 0.78 +- 0.04 (the peel's crests a little glossier, the strands' print a little duller), G cavity 1,
      B 0, A 1.
"""

import os
import sys

import numpy as np
from PIL import Image, ImageDraw

N = 1024
TILE_M = 0.6
PX_PER_MM = N / (TILE_M * 1000.0)        # 1.707


def band(h, w, lo, hi, seed):
    """Periodic band-limited noise between lo and hi cycles per tile, normalised to zero mean, unit std."""
    r = np.random.default_rng(seed)
    F = np.fft.fft2(r.standard_normal((h, w)))
    fy = np.fft.fftfreq(h)[:, None] * h
    fx = np.fft.fftfreq(w)[None, :] * w
    f = np.sqrt(fx * fx + fy * fy)
    mask = np.exp(-((np.log(np.maximum(f, 1e-3)) - np.log((lo * hi) ** .5)) ** 2) / (2 * (np.log(hi / lo) / 2.5) ** 2))
    mask[0, 0] = 0
    n = np.real(np.fft.ifft2(F * mask))
    return ((n - n.mean()) / max(n.std(), 1e-8)).astype(np.float64)


def spectral(h, w, beta, seed, fmin=1.0):
    """Periodic 1/f^beta noise, zero mean, unit std."""
    r = np.random.default_rng(seed)
    F = np.fft.fft2(r.standard_normal((h, w)))
    fy = np.fft.fftfreq(h)[:, None] * h
    fx = np.fft.fftfreq(w)[None, :] * w
    f = np.sqrt(fx * fx + fy * fy)
    f[0, 0] = 1.0
    amp = 1.0 / np.power(np.maximum(f, fmin), beta)
    amp[0, 0] = 0.0
    n = np.real(np.fft.ifft2(F * amp))
    return ((n - n.mean()) / max(n.std(), 1e-8)).astype(np.float64)


def blur(img, sigma):
    """Periodic gaussian blur via FFT (sigma in texels)."""
    h, w = img.shape
    fy = np.fft.fftfreq(h)[:, None]
    fx = np.fft.fftfreq(w)[None, :]
    g = np.exp(-2 * (np.pi * sigma) ** 2 * (fx * fx + fy * fy))
    return np.real(np.fft.ifft2(np.fft.fft2(img) * g))


def strands(seed, count):
    """Chopped-strand mat: count straight bundles 25-50 mm long, 0.6-1.2 mm wide, any direction, drawn wrapped (periodic), as a
    coverage field (3 x supersampled, then down), so the print-through is a field of short straight fibres, never a pattern."""
    ss = 3
    img = Image.new("L", (N * ss, N * ss), 0)
    dr = ImageDraw.Draw(img)
    r = np.random.default_rng(seed)
    for _ in range(count):
        x, y = r.random() * N * ss, r.random() * N * ss
        a = r.random() * np.pi
        length = r.uniform(25.0, 50.0) * PX_PER_MM * ss
        width = max(1, int(round(r.uniform(0.6, 1.2) * PX_PER_MM * ss)))
        dx, dy = np.cos(a) * length / 2, np.sin(a) * length / 2
        level = int(r.uniform(150, 255))
        for ox in (-N * ss, 0, N * ss):
            for oy in (-N * ss, 0, N * ss):
                dr.line([(x - dx + ox, y - dy + oy), (x + dx + ox, y + dy + oy)], fill=level, width=width)
    a = np.asarray(img).astype(np.float64) / 255.0
    return a.reshape(N, ss, N, ss).mean(axis=(1, 3))


def normal_from_height(hgt, strength):
    """Central differences on the periodic height (texel units), OpenGL +Y (v up runs against the image rows)."""
    dx = (np.roll(hgt, -1, 1) - np.roll(hgt, 1, 1)) * .5
    drow = (np.roll(hgt, -1, 0) - np.roll(hgt, 1, 0)) * .5
    nx = -dx * strength
    ny = drow * strength
    nz = np.ones_like(hgt)
    l = np.sqrt(nx * nx + ny * ny + nz * nz)
    return np.stack([nx / l, ny / l, nz / l], -1)


def lin_to_srgb(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= .0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - .055)


def build():
    # orange peel: 2-4 mm across (150-300 cycles over 0.6 m), a softer 6-12 mm swell under it (the flow-out)
    peel = band(N, N, 150.0, 300.0, 61101)
    swell = band(N, N, 50.0, 100.0, 61102)
    # print-through: the strands blurred by the 0.5 mm skin (sigma ~1.0 mm), their relief a few tenths of the peel's
    st = strands(61103, 2600)
    st = blur(st, 1.0 * PX_PER_MM)
    st = (st - st.mean()) / max(st.std(), 1e-8)
    # height in texel-relative units; the strengths are set so the normal's tilt reads as a light orange peel (std ~2 deg)
    height = 0.50 * peel + 0.40 * swell + 0.30 * st
    n = normal_from_height(height, 0.052)
    # pigment mottle (+-1.5 %, large and soft) and a faint fine speckle of the filler
    mottle = spectral(N, N, 2.2, 61104)
    speck = band(N, N, 300.0, 500.0, 61105)
    lin = 0.985 * (1.0 + 0.0075 * np.clip(mottle, -2, 2) + 0.002 * np.clip(speck, -2.5, 2.5))
    albedo = np.repeat(lin_to_srgb(np.clip(lin, 0, 1))[..., None], 3, axis=2)
    smooth = 0.78 + 0.018 * np.clip(peel, -2, 2) - 0.012 * np.clip(st, -1, 3) + 0.006 * np.clip(mottle, -2, 2)
    smooth = np.clip(smooth, 0.70, 0.86)
    s = np.stack([smooth, np.ones_like(smooth), np.zeros_like(smooth), np.ones_like(smooth)], -1)
    return albedo, n, s


def save(path, arr, mode):
    Image.fromarray((np.clip(arr, 0, 1) * 255.0 + 0.5).astype(np.uint8), mode).save(path, optimize=True)


def main(argv):
    out = argv[0] if argv else "Assets/Resources/Surfaces/Textures"
    preview = len(argv) > 1 and argv[1] == "preview"
    os.makedirs(out, exist_ok=True)
    albedo, n, s = build()
    save(os.path.join(out, "DoorGelcoat_A.png"), albedo, "RGB")
    save(os.path.join(out, "DoorGelcoat_N.png"), n * 0.5 + 0.5, "RGB")
    save(os.path.join(out, "DoorGelcoat_S.png"), s, "RGBA")
    tilt = np.degrees(np.arccos(np.clip(n[..., 2], -1, 1)))
    print("DoorGelcoat: albedo sRGB mean %.4f (min %.4f max %.4f); normal tilt mean %.2f deg, p99 %.2f deg; smoothness %.3f +- %.3f (min %.3f max %.3f)"
          % (albedo.mean(), albedo.min(), albedo.max(), tilt.mean(), np.percentile(tilt, 99), s[..., 0].mean(), s[..., 0].std(), s[..., 0].min(), s[..., 0].max()))
    if preview:
        sh = np.clip(0.5 + 6.0 * (n[..., 0] * 0.6 + n[..., 1] * 0.8), 0, 1)
        tiles = [albedo[..., 0], sh, s[..., 0]]
        Image.fromarray((np.concatenate(tiles, axis=1) * 255).astype(np.uint8), "L").save(os.path.join(out, "preview_DoorGelcoat.png"))


if __name__ == "__main__":
    main(sys.argv[1:])
