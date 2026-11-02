"""Alarm_ExitAlu: the tiling maps of the C16 alarm door's exit-alarm device (游戏视觉, 2026-11-01).

Clear anodised aluminium, US28 / BHMA 628 satin, the period finish of exit devices (research/door_hardware/v7b_alarm_door/
10_research.md section 3): a fine satin grain run along U (the kits' metre UVs put U along the bar and across the housing
front), a faint tone in the grain, and a smoothness field that is a little duller across the grain's deeper streaks. No
wear map: the device is a few years old, and the touch pad's palm polish is not baked into a tiling map.

numpy + Pillow, deterministic (fixed seed), tileable on both axes (every filter wraps). Run from the project root:
  python Tools/lookdev/gen_alarm_exit_alu.py [out_dir]      (default Assets/Resources/Surfaces/Textures)
      Alarm_ExitAlu_A.png  sRGB, albedo multiplier (the material's _BaseColor sets the metal's colour)
      Alarm_ExitAlu_N.png  linear, tangent-space normal (OpenGL +Y), grain slopes
      Alarm_ExitAlu_S.png  linear, mask: R smoothness multiplier, G cavity (1 = none), B and A 1
1024 x 1024 px over the material's _TileSize 0.15 m (0.15 mm per px).
"""

import os
import sys

import numpy as np
from PIL import Image

N = 1024
SEED = 6281990


def wrap_blur(a, rx, ry):
    """Box blur with wrap-around, rx px along U (axis 1), ry px along V (axis 0)."""
    out = a.astype(np.float64)
    if rx > 0:
        c = np.cumsum(np.concatenate([out[:, -rx:], out, out[:, :rx]], axis=1), axis=1)
        c = np.concatenate([np.zeros((out.shape[0], 1)), c], axis=1)
        out = (c[:, 2 * rx + 1:] - c[:, :-2 * rx - 1]) / (2 * rx + 1)
    if ry > 0:
        c = np.cumsum(np.concatenate([out[-ry:, :], out, out[:ry, :]], axis=0), axis=0)
        c = np.concatenate([np.zeros((1, out.shape[1])), c], axis=0)
        out = (c[2 * ry + 1:, :] - c[:-2 * ry - 1, :]) / (2 * ry + 1)
    return out


def norm01(a):
    a = a - a.mean()
    return a / (np.abs(a).max() + 1e-9)


def main(argv):
    out = argv[0] if argv else os.path.join("Assets", "Resources", "Surfaces", "Textures")
    os.makedirs(out, exist_ok=True)
    rng = np.random.default_rng(SEED)
    # Satin grain: long streaks along U (two scales), sub-pixel across V; a few deeper scratches along the grain.
    g1 = norm01(wrap_blur(rng.standard_normal((N, N)), 90, 0))
    g2 = norm01(wrap_blur(rng.standard_normal((N, N)), 24, 0))
    lines = np.zeros((N, N))
    for _ in range(140):
        y = rng.integers(0, N)
        x0 = rng.integers(0, N)
        ln = int(rng.integers(60, 420))
        xs = (x0 + np.arange(ln)) % N
        lines[y, xs] += rng.uniform(0.4, 1.0) * np.sin(np.linspace(0.0, np.pi, ln))
    lines = wrap_blur(lines, 1, 0)
    grain = 0.62 * g1 + 0.30 * g2 - 0.55 * lines
    tone = norm01(wrap_blur(rng.standard_normal((N, N)), 160, 40))
    # Albedo multiplier: within +-2.5 % (grain) and +-1.5 % (a soft anodising tone).
    alb = 0.965 + 0.022 * grain + 0.015 * tone
    a8 = np.clip(alb * 255.0, 0, 255).astype(np.uint8)
    Image.fromarray(np.dstack([a8, a8, a8]), "RGB").save(os.path.join(out, "Alarm_ExitAlu_A.png"), optimize=True)
    # Normal: slopes across the grain (V) carry the satin look; along U nearly flat.
    h = grain
    dv = (np.roll(h, -1, axis=0) - np.roll(h, 1, axis=0)) * 0.5
    du = (np.roll(h, -1, axis=1) - np.roll(h, 1, axis=1)) * 0.5
    sv = 0.060 * dv / (dv.std() + 1e-9)
    su = 0.006 * du / (du.std() + 1e-9)
    nx, ny, nz = -su, sv, np.ones_like(h)
    ln = np.sqrt(nx * nx + ny * ny + nz * nz)
    rgb = np.dstack([nx / ln, ny / ln, nz / ln]) * 0.5 + 0.5
    Image.fromarray(np.clip(rgb * 255.0 + 0.5, 0, 255).astype(np.uint8), "RGB").save(os.path.join(out, "Alarm_ExitAlu_N.png"), optimize=True)
    # Mask: R smoothness multiplier 0.86 .. 1.0 (duller in the deeper streaks), G cavity 1.
    smooth = np.clip(0.94 + 0.05 * g2 - 0.06 * np.clip(lines, 0, 1) + 0.02 * tone, 0.86, 1.0)
    r8 = (smooth * 255.0 + 0.5).astype(np.uint8)
    full = np.full((N, N), 255, np.uint8)
    Image.fromarray(np.dstack([r8, full, full, full]), "RGBA").save(os.path.join(out, "Alarm_ExitAlu_S.png"), optimize=True)
    print("wrote Alarm_ExitAlu_{A,N,S}.png", N, "px in", out)


if __name__ == "__main__":
    main(sys.argv[1:])
