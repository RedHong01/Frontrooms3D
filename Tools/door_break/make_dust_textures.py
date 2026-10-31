"""Dust textures for the door break (P1/P2 production stage 2026-10-30; research/door_break/54_dust_grit.md 4.1, 4.10).
numpy + PIL, deterministic (seeded). Nothing here is a photo or a download: procedural only.

  python Tools/door_break/make_dust_textures.py [--out <project>/Assets/Resources/Surfaces/Textures]

Door_DustWisp_D.png  1024 x 512, LINEAR, 8 frames of 256 x 256 (4 x 2): the haze WISP atlas (FrontRooms/DoorHaze).
    R = density: domain-warped fBm with filaments (ridged), under an ANISOTROPIC window that is never radially
        symmetric (an off-centre, skewed, lobed envelope; each frame its own), zero at the frame border;
    G = erosion order: the threshold at which a texel goes as the wisp ages (low density and the filament gaps go
        first), so the wisp breaks into filaments instead of fading as a disc.
    B = 0. The importer clamps it (no bleeding between frames at the far mips: a 4 px gutter per frame).
Tools/door_break/palettes.json  8 LINEAR colours per material (54 4.4), sorted dark to light, sampled from our own albedo
    maps (core from the strip's core columns only), plus fixed gypsum, settled-dust and glue entries. The Blender
    generator copies it into the family sidecar ("palettes"), so the run time reads no texture.
Door_Grain_D.png     128 x 16, LINEAR, 8 grain silhouettes of 16 x 16 (FrontRooms/DoorGrit): R = coverage of an
    irregular polygonal grain (4-7 corners, flakes 2.5 : 1), G = a pseudo-height (for the tumbling pseudo-normal),
    so a grain seen at 0.3 m is a speck with an edge, never a square.
"""
import argparse
import math
import os

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
TEX = os.path.join(ROOT, "Assets", "Resources", "Surfaces", "Textures")


def value_noise(rng, n, cells):
    g = rng.random((cells + 1, cells + 1))
    im = Image.fromarray((g * 65535).astype(np.int32), mode="I").resize((n, n), Image.BICUBIC)
    return np.clip(np.asarray(im, dtype=np.float64) / 65535.0, 0, 1)


def fbm(rng, n, base, octaves, gain=0.55):
    out = np.zeros((n, n))
    amp, tot = 1.0, 0.0
    c = base
    for _ in range(octaves):
        out += amp * value_noise(rng, n, c)
        tot += amp
        amp *= gain
        c *= 2
    return out / tot


def warp(field, du, dv):
    n = field.shape[0]
    yy, xx = np.mgrid[0:n, 0:n].astype(np.float64)
    x = np.clip(xx + du, 0, n - 1)
    y = np.clip(yy + dv, 0, n - 1)
    x0 = np.floor(x).astype(int)
    y0 = np.floor(y).astype(int)
    x1 = np.minimum(x0 + 1, n - 1)
    y1 = np.minimum(y0 + 1, n - 1)
    fx, fy = x - x0, y - y0
    return (field[y0, x0] * (1 - fx) * (1 - fy) + field[y0, x1] * fx * (1 - fy) + field[y1, x0] * (1 - fx) * fy + field[y1, x1] * fx * fy)


def wisp_frame(rng, k, n=256, gutter=4):
    """One wisp: R density, G erosion order."""
    base = fbm(rng, n, 3, 5)
    w1 = fbm(rng, n, 2, 3) - 0.5
    w2 = fbm(rng, n, 2, 3) - 0.5
    warped = warp(base, w1 * 70.0, w2 * 70.0)
    ridge = 1.0 - np.abs(2.0 * fbm(rng, n, 4, 4) - 1.0)            # ridged: filaments
    ridge = warp(ridge, w2 * 50.0, -w1 * 50.0) ** 3
    dens = np.clip(0.55 * warped + 0.65 * ridge - 0.25, 0, 1)
    # the envelope: an off-centre, skewed, lobed blob (never radially symmetric); elongated along V (the motion axis
    # the shader turns onto the wisp's screen velocity)
    yy, xx = np.mgrid[0:n, 0:n].astype(np.float64)
    u = (xx + 0.5) / n * 2 - 1
    v = (yy + 0.5) / n * 2 - 1
    cx, cy = rng.uniform(-0.18, 0.18), rng.uniform(-0.22, 0.12)
    sk = rng.uniform(-0.5, 0.5)
    uu = (u - cx) + sk * (v - cy)
    vv = (v - cy)
    ang = np.arctan2(vv, uu)
    lobes = 1.0 + 0.28 * np.sin(3 * ang + rng.uniform(0, 6.28)) + 0.18 * np.sin(5 * ang + rng.uniform(0, 6.28))
    rad = np.sqrt((uu / rng.uniform(0.45, 0.65)) ** 2 + (vv / rng.uniform(0.75, 0.92)) ** 2) / lobes
    # a tail: dust trails behind its motion (lower V)
    tail = np.clip(1.0 - np.abs(uu) / 0.35, 0, 1) * np.clip(-(vv + 0.2) / 0.7, 0, 1) * 0.35
    env = np.clip(1.0 - rad, 0, 1) ** 1.6 + tail * (1 - np.clip(rad - 1, 0, 1))
    dens = np.clip(dens * env * 1.8, 0, 1)
    # zero gutter (clamp, mips)
    edge = np.minimum(np.minimum(xx, n - 1 - xx), np.minimum(yy, n - 1 - yy))
    dens *= np.clip((edge - gutter) / 6.0, 0, 1)
    # erosion order: thin density and the gaps between filaments go first; the core goes last
    eros = np.clip(0.15 + 0.7 * (dens / max(1e-6, dens.max())) ** 0.7 + 0.25 * (ridge - 0.5), 0, 1)
    eros[dens <= 1e-4] = 0.0
    return dens, eros


def grain_frame(rng, n=16):
    yy, xx = np.mgrid[0:n, 0:n].astype(np.float64)
    ss = 4
    cov = np.zeros((n, n))
    hgt = np.zeros((n, n))
    corners = rng.integers(4, 8)
    flake = rng.random() < 0.4
    ax, ay = (0.92, 0.38) if flake else (rng.uniform(0.6, 0.9), rng.uniform(0.5, 0.85))
    rot = rng.uniform(0, math.pi)
    angs = np.sort(rng.uniform(0, 2 * math.pi, corners))
    rads = rng.uniform(0.65, 1.0, corners)
    poly = [(math.cos(a) * r * ax, math.sin(a) * r * ay) for a, r in zip(angs, rads)]
    poly = [(x * math.cos(rot) - y * math.sin(rot), x * math.sin(rot) + y * math.cos(rot)) for x, y in poly]

    def inside(px, py):
        c = False
        for i in range(len(poly)):
            x1, y1 = poly[i]
            x2, y2 = poly[(i + 1) % len(poly)]
            if (y1 > py) != (y2 > py) and px < (x2 - x1) * (py - y1) / (y2 - y1 + 1e-12) + x1:
                c = not c
        return c
    for j in range(n):
        for i in range(n):
            hit = 0
            for sj in range(ss):
                for si in range(ss):
                    px = ((i + (si + 0.5) / ss) / n) * 2 - 1
                    py = ((j + (sj + 0.5) / ss) / n) * 2 - 1
                    hit += inside(px * 1.15, py * 1.15)
            cov[j, i] = hit / (ss * ss)
    cx = (xx + 0.5) / n * 2 - 1
    cy = (yy + 0.5) / n * 2 - 1
    hgt = np.clip(1.0 - np.sqrt(cx * cx + cy * cy), 0, 1) * cov
    return cov, hgt


def srgb_to_lin(c):
    c = np.asarray(c, dtype=np.float64) / 255.0
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def palette_from(path, rng, cols=None, tint=(1.0, 1.0, 1.0)):
    a = np.asarray(Image.open(path).convert("RGB"), dtype=np.float64)
    if cols is not None:
        a = a[:, cols[0]:cols[1]]
    px = a.reshape(-1, 3)
    pick = px[rng.integers(0, len(px), 8192)]
    lin = srgb_to_lin(pick) * np.asarray(tint)
    lum = lin @ np.array([0.2126, 0.7152, 0.0722])
    order = np.argsort(lum)
    bins = np.array_split(lin[order], 8)
    return [[round(float(c), 5) for c in b.mean(0)] for b in bins]


def ramp(a, b):
    la, lb = srgb_to_lin(a), srgb_to_lin(b)
    return [[round(float(c), 5) for c in (la + (lb - la) * k / 7.0)] for k in range(8)]


def palettes(tex, rng):
    out = {
        "core": palette_from(os.path.join(tex, "Door_CoreWood_A.png"), rng, cols=(116, 908)),
        "stile": palette_from(os.path.join(tex, "Door_CoreStile_A.png"), rng),
        "veneer": palette_from(os.path.join(tex, "DoorVeneer_A.png"), rng),
        "laminate": palette_from(os.path.join(tex, "Prop_WoodLaminate_A.png"), rng),
        "frame": palette_from(os.path.join(tex, "DoorFrame_A.png"), rng),
        "steel": palette_from(os.path.join(tex, "PaintedMetal_A.png"), rng, tint=(1.129, 1.116, 1.079))[:7] + [[round(float(c), 5) for c in srgb_to_lin([142, 118, 96])]],
        "gypsum": ramp([205, 203, 196], [225, 222, 212]),
        "settled": ramp([118, 110, 99], [150, 140, 126]),
        "glue": [[round(float(c), 5) for c in srgb_to_lin([120, 78, 30])]] * 8,
        # P3 (2026-10-30), appended so the entries above keep their draws: kraft honeycomb paper, the steel frame's sleeve
        "kraft": ramp([118, 84, 50], [170, 128, 82]),
        "steelframe": palette_from(os.path.join(tex, "Prop_SteelBrown_A.png"), rng),
    }
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=TEX)
    a = ap.parse_args()
    os.makedirs(a.out, exist_ok=True)
    rng = np.random.default_rng(54202610)
    atlas = np.zeros((512, 1024, 3), dtype=np.uint8)
    areas = []
    for k in range(8):
        d, e = wisp_frame(rng, k)
        r, c = divmod(k, 4)
        # rows bottom-up in Unity UV: frame k sits at column c, row r from the bottom
        y0 = 512 - (r + 1) * 256
        atlas[y0:y0 + 256, c * 256:(c + 1) * 256, 0] = np.round(d[::-1] * 255).astype(np.uint8)
        atlas[y0:y0 + 256, c * 256:(c + 1) * 256, 1] = np.round(e[::-1] * 255).astype(np.uint8)
        areas.append(float((d > 0.05).mean()))
    Image.fromarray(atlas, "RGB").save(os.path.join(a.out, "Door_DustWisp_D.png"))
    grains = np.zeros((16, 128, 3), dtype=np.uint8)
    for k in range(8):
        cov, h = grain_frame(rng)
        grains[:, k * 16:(k + 1) * 16, 0] = np.round(cov[::-1] * 255).astype(np.uint8)
        grains[:, k * 16:(k + 1) * 16, 1] = np.round(h[::-1] * 255).astype(np.uint8)
    Image.fromarray(grains, "RGB").save(os.path.join(a.out, "Door_Grain_D.png"))
    print("[dust] Door_DustWisp_D 1024x512, 8 frames, covered fraction per frame %s" % [round(x, 2) for x in areas])
    print("[dust] Door_Grain_D 128x16, 8 grains")
    pal = palettes(a.out, np.random.default_rng(4401))
    import json
    with open(os.path.join(HERE, "palettes.json"), "w", newline="\n") as f:
        json.dump(pal, f, indent=1)
    print("[dust] palettes.json: " + ", ".join("%s mid %s" % (k, v[4]) for k, v in pal.items()))


if __name__ == "__main__":
    main()
