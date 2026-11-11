#!/usr/bin/env python3
"""FRONTROOMSSS structures G4-b (line visual (game visual), 2026-11-10; package structures_run_service, 30_plan.md P20): the one new surface
of the Run! services package, for Resources/Surfaces/Textures (FrontRooms/Surface, mesh-UV metres).

  Prop_MoldedStone_{A,N,S}       512, 0.25 m tile: the molded-stone floor service basin of the janitor's closet (63 3.2 RN-JAN, LS103:
                                  "molded stone or terrazzo"): a light grey cast composite with crushed marble and dark stone chips
                                  1-6 mm, a little beige aggregate, satin where the binder is, chips a touch glossier, faint pits and
                                  a soft grey bloom of use (no rings, no lettering)

House style of the zone lamps' generators (numpy + Pillow): _A sRGB albedo, _N OpenGL +Y normal, _S linear mask (R smoothness, G cavity,
B metal 0, A 1). Seeds fixed: the maps rebuild byte for byte. No text, label, brand or logo. ASCII only.
Run: W/venv/bin/python gen_service_maps.py <out dir>
"""
import os
import sys

import numpy as np
from PIL import Image

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(os.path.abspath(__file__))
os.makedirs(OUT, exist_ok=True)
N = 512
TILE = 0.25                      # metres per tile
PX = TILE / N                    # metres per pixel (0.49 mm)


def srgb(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= .0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - .055)


def lin(c):
    c = np.asarray(c, np.float32)
    return np.where(c <= .04045, c / 12.92, ((c + .055) / 1.055) ** 2.4)


def save(name, arr, mode):
    a = np.clip(arr, 0, 1)
    Image.fromarray((a * 255 + .5).astype(np.uint8), mode).save(os.path.join(OUT, name), optimize=True)


def tile_noise(rng, n, octaves):
    """Tileable value noise: sum of upsampled random grids (wrap), octaves = list of (cells, weight)."""
    out = np.zeros((n, n), np.float32)
    for cells, w in octaves:
        g = rng.random((cells, cells)).astype(np.float32)
        x = np.arange(n) * cells / n
        i0 = np.floor(x).astype(int) % cells
        i1 = (i0 + 1) % cells
        f = x - np.floor(x)
        f = f * f * (3 - 2 * f)
        a = g[i0][:, i0] * (1 - f)[None, :] + g[i0][:, i1] * f[None, :]
        b = g[i1][:, i0] * (1 - f)[None, :] + g[i1][:, i1] * f[None, :]
        out += w * (a * (1 - f)[:, None] + b * f[:, None])
    return out / sum(w for _, w in octaves)


def chips(rng, n, count, rmin, rmax):
    """Irregular polygonal chips (crushed aggregate) stamped with wrap: returns (mask 0..1, id map)."""
    mask = np.zeros((n, n), np.float32)
    ids = np.zeros((n, n), np.int32)
    yy, xx = np.mgrid[0:n, 0:n].astype(np.float32)
    for k in range(count):
        cx, cy = rng.random() * n, rng.random() * n
        r = (rmin + (rmax - rmin) * rng.random() ** 2.2) / PX
        sides = rng.integers(4, 8)
        rot = rng.random() * np.pi
        squash = 0.55 + 0.45 * rng.random()
        x0, x1 = int(cx - r - 2), int(cx + r + 3)
        y0, y1 = int(cy - r - 2), int(cy + r + 3)
        for oy in (-n, 0, n):
            for ox in (-n, 0, n):
                ax0, ax1 = max(0, x0 + ox), min(n, x1 + ox)
                ay0, ay1 = max(0, y0 + oy), min(n, y1 + oy)
                if ax0 >= ax1 or ay0 >= ay1:
                    continue
                dx = xx[ay0:ay1, ax0:ax1] - (cx + ox)
                dy = yy[ay0:ay1, ax0:ax1] - (cy + oy)
                c, s = np.cos(rot), np.sin(rot)
                u = (dx * c + dy * s)
                v = (-dx * s + dy * c) / squash
                ang = np.arctan2(v, u)
                rr = np.hypot(u, v)
                # polygon radius in direction ang
                sec = 2 * np.pi / sides
                a = np.mod(ang, sec) - sec / 2
                poly = r * np.cos(sec / 2) / np.maximum(np.cos(a), 1e-3)
                inside = rr < poly
                edge = np.clip((poly - rr) / 1.2, 0, 1)
                sub = mask[ay0:ay1, ax0:ax1]
                upd = inside & (edge > sub)
                sub[upd] = edge[upd]
                ids[ay0:ay1, ax0:ax1][upd] = k + 1
    return mask, ids


def main():
    rng = np.random.default_rng(19890412)
    base = np.array(lin([0.70, 0.70, 0.68]), np.float32)
    tone = tile_noise(rng, N, [(4, 1.0), (16, 0.6), (64, 0.35)])
    alb = np.ones((N, N, 3), np.float32) * base[None, None, :]
    alb *= (0.94 + 0.12 * tone)[..., None]
    # fine sand of the binder
    sand = rng.random((N, N)).astype(np.float32)
    alb *= (0.97 + 0.06 * sand)[..., None]
    smooth = 0.46 + 0.04 * tone
    height = np.zeros((N, N), np.float32)
    # aggregates: white marble, dark stone, grey, a few beige
    kinds = [((0.86, 0.86, 0.84), 260, 0.0010, 0.0045, 0.60), ((0.10, 0.10, 0.11), 210, 0.0008, 0.0035, 0.58), ((0.42, 0.42, 0.41), 180, 0.0008, 0.0040, 0.55),
             ((0.66, 0.58, 0.46), 60, 0.0010, 0.0035, 0.56)]
    for col, count, rmin, rmax, sm in kinds:
        m, ids = chips(rng, N, count, rmin, rmax)
        c = np.array(lin(col), np.float32)
        jitter = 0.85 + 0.3 * rng.random(count + 1).astype(np.float32)
        jm = jitter[ids]
        a = np.clip(m * 1.0, 0, 1)[..., None]
        alb = alb * (1 - a) + (c[None, None, :] * jm[..., None]) * a
        smooth = smooth * (1 - m) + sm * m
        height += 0.25 * m
    # pits (air voids) and a soft bloom of use
    pits = (rng.random((N, N)) > 0.9975).astype(np.float32)
    from PIL import ImageFilter
    pim = Image.fromarray((pits * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(3))
    pits = np.asarray(pim, np.float32) / 255.0
    alb *= (1 - 0.35 * pits)[..., None]
    height -= 0.6 * pits
    smooth -= 0.10 * pits
    bloom = tile_noise(rng, N, [(3, 1.0), (8, 0.5)])
    alb *= (0.97 + 0.04 * bloom)[..., None]
    # normal from height (wrap)
    h = height * 0.0006 / PX
    gx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * 0.5
    gy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * 0.5
    nrm = np.stack([-gx, gy, np.ones_like(gx)], -1)
    nrm /= np.linalg.norm(nrm, axis=-1, keepdims=True)
    cav = np.clip(1.0 - 0.6 * pits - 0.1 * (1 - height.clip(0, 1)), 0, 1)
    mask = np.stack([np.clip(smooth, 0, 1), cav, np.zeros_like(cav), np.ones_like(cav)], -1)
    save("Prop_MoldedStone_A.png", srgb(alb), "RGB")
    save("Prop_MoldedStone_N.png", nrm * .5 + .5, "RGB")
    save("Prop_MoldedStone_S.png", mask, "RGBA")
    print("wrote Prop_MoldedStone A N S", N, "px at", TILE, "m")


main()
