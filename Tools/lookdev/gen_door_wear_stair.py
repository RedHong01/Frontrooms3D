"""Door wear for the stairwell's hollow-metal leaves (游戏视觉, stairwell doors, 2026-11-06; Documentation/research/double_doors/
30_build.md Part S): DoorWear_Stair, the leaf-wear mask of Stair_DoorSteel / Stair_DoorSteelInside (FrontRooms/Surface
_FR_LEAFWEAR). numpy + Pillow only, no downloads, deterministic (fixed seed). Same channel contract as gen_door_wear.py:
  R grime    albedo x (1 - 0.35 R)
  G dull     smoothness x (1 - 0.5 G)
  B burnish  smoothness + 0.20 B
  A chip     albedo -> _FR_LeafWearChip (the grey shop primer) by A, smoothness x (1 - 0.3 A)

Layout: one face of the standard stair leaf (Kit_DoorLeaf_Stair: 0.992 x 2.0888 m, its face u metres from the hinge edge at
z 0.004), 1024 x 2048 RGBA linear: u 0 at the HINGE edge, 1 at the LATCH edge; v 0 at the leaf's bottom (y 0.008), 1 at its top
(y 2.0968). 0.97 x 1.02 mm a texel. The wide sealed leaf squeezes its u onto it, the north leaf flips its faces' u, so every
stair face reads the latch edge at u 1 (door_stair_common.leaf_uv_rules); the materials' _FR_LeafWearRect map metres to it.

What a 1990 stair door's paint shows (research/stairwell/01_research.md: putty enamel over a shop primer; door_hardware 10
section 8: wear follows the hands): the latch stile darkened and dulled where hands steady the door (0.85-1.55 m; the push face's
crossbar sits at 1.0 m, the stair face is caught by the hand as the leaf swings in), the very edge a little burnished; shoe and
rubber scuffs low on the leaf (0.02-0.40 m, more toward the latch); a grey mop line along the foot; primer chips: small angular
flakes along the latch arris and at the bottom corners, a few knocks on the lower latch half. Soft-edged and noise-broken,
never a stamp; nothing at the hinge stile but faint dirt.

Run (from the project root):
  python Tools/lookdev/gen_door_wear_stair.py Assets/Resources/Surfaces/Textures          -> DoorWear_Stair.png
  python Tools/lookdev/gen_door_wear_stair.py <dir> preview                               -> + preview_DoorWear_Stair_<c>.png
"""

import math
import os
import sys

import numpy as np
from PIL import Image

W, H = 1024, 2048
LEAF_W = 0.992
LEAF_Y0, LEAF_Y1 = 0.008, 2.0968
LEAF_H = LEAF_Y1 - LEAF_Y0
SEED = 61106

XS = (np.arange(W) + 0.5) / W * LEAF_W                 # metres from the hinge edge
YS = LEAF_Y0 + (np.arange(H) + 0.5) / H * LEAF_H       # metres over the floor
X, Y = np.meshgrid(XS, YS)                             # [H, W]; row 0 = the bottom (flipped when written)


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def value_noise(rng, cells_x, cells_y):
    g = rng.random((cells_y + 2, cells_x + 2))
    fx = XS / LEAF_W * cells_x
    fy = (YS - LEAF_Y0) / LEAF_H * cells_y
    ix, iy = np.floor(fx).astype(int), np.floor(fy).astype(int)
    tx, ty = fx - ix, fy - iy
    tx, ty = tx * tx * (3 - 2 * tx), ty * ty * (3 - 2 * ty)
    a = g[iy][:, ix]
    b = g[iy][:, ix + 1]
    c = g[iy + 1][:, ix]
    d = g[iy + 1][:, ix + 1]
    tx2 = tx[None, :]
    ty2 = ty[:, None]
    return (a * (1 - tx2) + b * tx2) * (1 - ty2) + (c * (1 - tx2) + d * tx2) * ty2


def fbm(rng, base_m, octaves=4, gain=0.5):
    out = np.zeros((H, W))
    amp, tot = 1.0, 0.0
    size = base_m
    for _ in range(octaves):
        cx = max(1, int(round(LEAF_W / size)))
        cy = max(1, int(round(LEAF_H / size)))
        out += amp * value_noise(rng, cx, cy)
        tot += amp
        amp *= gain
        size /= 2.0
    return out / tot


def ellipse_field(cx, cy, rx, ry, ang):
    c, s = math.cos(ang), math.sin(ang)
    dx, dy = X - cx, Y - cy
    u = (dx * c + dy * s) / rx
    v = (-dx * s + dy * c) / ry
    return np.clip(1.0 - (u * u + v * v), 0.0, 1.0)


def streaks(rng, n, x_range, y_range, length_range, width_range, slope=0.06):
    """Horizontal swipes (shoe and rubber marks): long thin soft ellipses with a little slope, max-combined."""
    out = np.zeros((H, W))
    for _ in range(n):
        cx = rng.uniform(*x_range)
        cy = rng.uniform(*y_range)
        L = rng.uniform(*length_range)
        w = rng.uniform(*width_range)
        f = ellipse_field(cx, cy, L / 2, w / 2, rng.uniform(-slope, slope)) ** 0.7
        out = np.maximum(out, f * rng.uniform(0.5, 1.0))
    return out


def flakes(rng, n, x_range, y_range, size_range, cluster=(1, 4), aspect=(1.3, 2.8)):
    """Angular primer chips: n knocks, each a cluster of 1-4 irregular 5-8 sided flakes, wider than tall, hard-edged
    (3 x 3 supersampled at the texel)."""
    out = np.zeros((H, W))
    px = LEAF_W / W
    for _ in range(n):
        kx = rng.uniform(*x_range)
        ky = rng.uniform(*y_range)
        for _k in range(rng.randint(cluster[0], cluster[1])):
            r = rng.uniform(*size_range)
            asp = rng.uniform(*aspect)
            cx = kx + rng.normal(0, r * 0.9)
            cy = ky + rng.normal(0, r * 0.5)
            m = rng.randint(5, 8)
            ang0 = rng.uniform(0, 2 * math.pi)
            tilt = rng.uniform(-0.35, 0.35)
            pts = []
            for j in range(m):
                a = ang0 + 2 * math.pi * j / m + rng.uniform(-0.25, 0.25)
                rr = r * rng.uniform(0.55, 1.0)
                lx, ly = rr * math.cos(a) * asp, rr * math.sin(a)
                pts.append((cx + lx * math.cos(tilt) - ly * math.sin(tilt), cy + lx * math.sin(tilt) + ly * math.cos(tilt)))
            xs = [p[0] for p in pts]
            ys = [p[1] for p in pts]
            i0 = max(0, int((min(xs)) / px) - 1)
            i1 = min(W, int((max(xs)) / px) + 2)
            j0 = max(0, int((min(ys) - LEAF_Y0) / (LEAF_H / H)) - 1)
            j1 = min(H, int((max(ys) - LEAF_Y0) / (LEAF_H / H)) + 2)
            if i1 <= i0 or j1 <= j0:
                continue
            acc = np.zeros((j1 - j0, i1 - i0))
            for sx in (-1 / 3, 0, 1 / 3):
                for sy in (-1 / 3, 0, 1 / 3):
                    gx = XS[i0:i1][None, :] + sx * px
                    gy = YS[j0:j1][:, None] + sy * (LEAF_H / H)
                    inside = np.ones_like(acc, dtype=bool)
                    for j in range(m):
                        x0, y0 = pts[j]
                        x1, y1 = pts[(j + 1) % m]
                        cross = (x1 - x0) * (gy - y0) - (y1 - y0) * (gx - x0)
                        inside &= cross >= 0
                    acc += inside
            out[j0:j1, i0:i1] = np.maximum(out[j0:j1, i0:i1], acc / 9.0)
    return out


def stair(rng):
    R = np.zeros((H, W))
    G = np.zeros((H, W))
    B = np.zeros((H, W))
    A = np.zeros((H, W))
    u = X / LEAF_W
    # faint mottled dirt everywhere, a little more low
    dirt = fbm(rng, 0.35, 4)
    R += 0.05 * smoothstep(0.45, 0.85, dirt) + 0.04 * (1.0 - smoothstep(0.0, 1.2, Y))
    G += 0.06 * smoothstep(0.4, 0.9, fbm(rng, 0.25, 3))
    # the hand zone on the latch stile: darker, duller, a halo broken by noise; the very edge burnished
    hand = smoothstep(0.62, 0.86, u) * smoothstep(0.80, 0.98, Y) * (1.0 - smoothstep(1.35, 1.62, Y))
    brk = smoothstep(0.30, 0.75, fbm(rng, 0.06, 4))
    palm = np.zeros((H, W))
    for _ in range(9):
        palm = np.maximum(palm, ellipse_field(rng.uniform(0.80, 0.97), rng.uniform(0.95, 1.45), rng.uniform(0.05, 0.08),
                                              rng.uniform(0.07, 0.11), rng.uniform(-0.5, 0.5)) ** 0.6 * rng.uniform(0.6, 1.0))
    R += 0.30 * hand * (0.30 + 0.70 * brk) + 0.18 * palm * brk
    G += 0.35 * hand * (0.35 + 0.65 * brk) + 0.20 * palm * (0.5 + 0.5 * brk)
    edge = smoothstep(0.955, 0.995, u)
    B += 0.45 * edge * smoothstep(0.92, 1.05, Y) * (1.0 - smoothstep(1.30, 1.45, Y)) * (0.6 + 0.4 * brk)
    R += 0.20 * edge * smoothstep(0.30, 0.6, Y) * (1.0 - smoothstep(1.7, 1.9, Y))
    # shoe and rubber scuffs low on the leaf, more toward the latch; a duller band
    sc = streaks(rng, 34, (0.10, 0.95), (0.03, 0.40), (0.06, 0.22), (0.004, 0.012))
    sc *= 0.45 + 0.55 * smoothstep(0.2, 0.9, u)
    R += 0.55 * sc
    G += 0.22 * smoothstep(0.02, 0.10, Y) * (1.0 - smoothstep(0.26, 0.52, Y + 0.08 * (fbm(rng, 0.12, 3) - 0.5))) * (0.4 + 0.6 * fbm(rng, 0.08, 3))
    # the mop line along the foot
    mop = (1.0 - smoothstep(0.012, 0.045, Y - LEAF_Y0 + 0.004 * (fbm(rng, 0.05, 3) - 0.5) * 6))
    R += 0.28 * mop
    G += 0.30 * mop
    # primer chips: the latch arris, the bottom corners, a few knocks on the lower latch half
    A = np.maximum(A, flakes(rng, 26, (LEAF_W - 0.010, LEAF_W - 0.001), (0.30, 1.90), (0.0012, 0.0030)))
    A = np.maximum(A, flakes(rng, 10, (LEAF_W - 0.06, LEAF_W - 0.002), (0.015, 0.30), (0.0015, 0.0045)))
    A = np.maximum(A, flakes(rng, 6, (0.003, 0.05), (0.015, 0.22), (0.0012, 0.0035)))
    A = np.maximum(A, flakes(rng, 7, (0.55, LEAF_W - 0.03), (0.28, 0.75), (0.0015, 0.0040), cluster=(1, 3)))
    A = np.maximum(A, flakes(rng, 4, (0.70, LEAF_W - 0.03), (0.95, 1.35), (0.0010, 0.0025), cluster=(1, 2)))
    R = np.clip(R, 0.0, 1.0)
    G = np.clip(G, 0.0, 1.0)
    B = np.clip(B, 0.0, 1.0)
    A = np.clip(A, 0.0, 1.0)
    return np.stack([R, G, B, A], axis=-1)


def write(path, m):
    img = (np.clip(m, 0.0, 1.0) * 255.0 + 0.5).astype(np.uint8)[::-1]      # row 0 of the file = the top of the leaf
    Image.fromarray(img, "RGBA").save(path, optimize=True)


def main(argv):
    out = argv[0] if argv else "Assets/Resources/Surfaces/Textures"
    rng = np.random.default_rng(SEED)

    class R:                                    # the helpers take Python's random API; numpy's generator behind it
        def random(self, shape):
            return rng.random(shape)

        def uniform(self, a, b):
            return float(rng.uniform(a, b))

        def normal(self, m, s):
            return float(rng.normal(m, s))

        def randint(self, a, b):
            return int(rng.integers(a, b + 1))
    m = stair(R())
    os.makedirs(out, exist_ok=True)
    path = os.path.join(out, "DoorWear_Stair.png")
    write(path, m)
    print("wrote", path, "means R %.4f G %.4f B %.4f A %.4f" % tuple(m.reshape(-1, 4).mean(0)))
    if len(argv) > 1 and argv[1] == "preview":
        for i, c in enumerate("RGBA"):
            Image.fromarray((m[::-1, :, i] * 255).astype(np.uint8), "L").save(os.path.join(out, "preview_DoorWear_Stair_%s.png" % c))


if __name__ == "__main__":
    main(sys.argv[1:])
