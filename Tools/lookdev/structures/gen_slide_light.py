"""The slide start light's lenses (structures G2-b, line visual (game visual), 2026-11-08; Kit_SlideTrafficLight's Prop_SlideLensRed and
Prop_SlideLensGreen; 63_furniture_run_pool.md 6.2: "red and green glass lenses, a lit bulb ... incandescent, not LED dots"). numpy +
Pillow only, no downloads, deterministic (fixed seeds).

Run (from the project root):
  python Tools/lookdev/structures/gen_slide_light.py Assets/Resources/Surfaces/Textures          -> SlideLens_{A,N,S,E}.png
  python Tools/lookdev/structures/gen_slide_light.py <dir> preview                                -> + preview_SlideLens.png

What it is: a period signal roundel (160 mm): pressed coloured glass, its inside moulded with a field of small spherical prisms (the
"stipple" that spreads a bulb's light into a disc) and a few concentric Fresnel rings, lit from behind by one incandescent lamp. Lit,
it shows a hot spot where the filament sits a little above centre, a broad glow over the disc and the stipple's sparkle; dark, a deep
coloured glass with the stipple catching the room's light. Maps on the lens's decal UVs (0..1 across the disc), 256^2:
  SlideLens_A  sRGB albedo, near white (the colour is the material's _BaseColor: the glass), the stipple a touch lighter
  SlideLens_N  linear normal (OpenGL +Y): the stipple's domes and the Fresnel rings
  SlideLens_S  linear mask: R smoothness 0.92 (glass), G cavity 1 (0.8 in the stipple's valleys), B 0, A 1
  SlideLens_E  sRGB emission (relative; the kit's driver sets the colour and level): the hot spot, the glow, the stipple's sparkle,
               fading to 0 at the rim (the retaining ring hides the edge)
No text, label, brand or logo. ASCII only.
"""

import os
import sys

import numpy as np
from PIL import Image

N = 256


def lin_to_srgb(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= .0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - .055)


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def build():
    yy, xx = np.mgrid[0:N, 0:N]
    u = (xx + .5) / N * 2 - 1
    v = 1 - (yy + .5) / N * 2
    r = np.sqrt(u * u + v * v)
    inside = 1.0 - smooth(.96, 1.0, r)
    # the stipple: a hexagonal field of small domes, 22 across the disc
    pitch = 2.0 / 22
    rows = v / (pitch * .866)
    ri = np.round(rows)
    off = (ri % 2) * .5
    ci = np.round(u / pitch - off)
    cx = (ci + off) * pitch
    cy = ri * pitch * .866
    dx, dy = u - cx, v - cy
    d = np.sqrt(dx * dx + dy * dy) / (pitch * .5)
    dome = np.clip(1 - d * d, 0, 1)
    # Fresnel rings (a few, near the rim)
    ring = np.zeros_like(r)
    for rr in (.62, .78, .90):
        ring += np.exp(-((r - rr) / .012) ** 2)
    # normals: the domes' slopes and the rings' ridges
    gx = -dx / (pitch * .5) * dome * .9
    gy = -dy / (pitch * .5) * dome * .9
    gr = np.where(r > 1e-4, 1 / np.maximum(r, 1e-4), 0)
    rs = np.zeros_like(r)
    for rr in (.62, .78, .90):
        rs += -2 * (r - rr) / (.012 ** 2) * np.exp(-((r - rr) / .012) ** 2) * .004
    gx += rs * u * gr
    gy += rs * v * gr
    nx, ny = -gx * .35, -gy * .35
    nz = np.ones_like(nx)
    l = np.sqrt(nx * nx + ny * ny + nz * nz)
    n = np.stack([nx / l, ny / l, nz / l], -1)
    n = n * inside[..., None] + np.array([0, 0, 1.0]) * (1 - inside[..., None])
    # albedo: near white, the domes a touch lighter
    alb = np.clip(.90 + .06 * dome + .03 * ring, 0, 1) * inside + .9 * (1 - inside)
    albedo = np.repeat(lin_to_srgb(alb)[..., None], 3, axis=2)
    # mask
    smo = .92 - .04 * (1 - dome)
    cav = 1 - .2 * (1 - dome) * (d < 1.2)
    s = np.stack([smo, cav, np.zeros_like(smo), np.ones_like(smo)], -1)
    # emission: the filament's hot spot a little above centre, a broad glow, each dome's sparkle, the rim fading
    hx, hy = 0.0, .08
    hd = np.sqrt((u - hx) ** 2 + (v - hy) ** 2)
    hot = np.exp(-(hd / .16) ** 2) * 1.0 + np.exp(-(hd / .45) ** 2) * .55 + .22
    spark = .75 + .25 * dome ** 3 + .12 * ring
    e = np.clip(hot * spark * inside * (1 - .25 * smooth(.7, .98, r)), 0, 1)
    emission = np.repeat(lin_to_srgb(e)[..., None], 3, axis=2)
    return albedo, n, s, emission


def save(path, arr, mode):
    Image.fromarray((np.clip(arr, 0, 1) * 255.0 + 0.5).astype(np.uint8), mode).save(path, optimize=True)


def main(argv):
    out = argv[0] if argv else "Assets/Resources/Surfaces/Textures"
    preview = len(argv) > 1 and argv[1] == "preview"
    os.makedirs(out, exist_ok=True)
    a, n, s, e = build()
    save(os.path.join(out, "SlideLens_A.png"), a, "RGB")
    save(os.path.join(out, "SlideLens_N.png"), n * .5 + .5, "RGB")
    save(os.path.join(out, "SlideLens_S.png"), s, "RGBA")
    save(os.path.join(out, "SlideLens_E.png"), e, "RGB")
    print("SlideLens: emission centre %.3f, rim %.3f; normal tilt p99 %.1f deg" % (e[N // 2 - 10, N // 2, 0], e[N // 2, 4, 0], np.degrees(np.arccos(np.clip(n[..., 2], -1, 1))).max()))
    if preview:
        Image.fromarray((np.concatenate([a[..., 0], (n[..., 0] * .5 + .5), s[..., 0], e[..., 0]], 1) * 255).astype(np.uint8), "L").save(os.path.join(out, "preview_SlideLens.png"))


if __name__ == "__main__":
    main(sys.argv[1:])
