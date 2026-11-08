"""Door wear for the D1.6 map leaves of the zone doors (游戏视觉, zone doors, the Map stage, 2026-11-06; Documentation/research/
double_doors/30_build.md Part M): the leaf-wear masks of the map members' faces (FrontRooms/Surface _FR_LEAFWEAR). numpy + Pillow
only, no downloads, deterministic (fixed seeds). The channel contract of gen_door_wear.py:
  R grime    albedo x (1 - 0.35 R)
  G dull     smoothness x (1 - 0.5 G)
  B burnish  smoothness + 0.20 B
  A chip     albedo -> _FR_LeafWearChip by A, smoothness x (1 - 0.3 A)

Layout: one face of the D1.6 map leaf (0.99 x 2.08 m: Z 0.005-0.995 from the map hinge, Y 0.015-2.095 over the floor), drawn over
u = Z / 1.0 and v = Y / 2.11 (1024 x 2048 RGBA linear, 0.98 x 1.03 mm a texel); the materials' _FR_LeafWearRect is
(0, 0, 1, 1 / 2.11). The kit leaves' face S runs u = +Z and face P u = -Z (kitlib's metre UVs), so the door kit facade samples
these masks with wrapU Mirror (FrontRoomsInteractableKit.DoorParts.MirrorLeafWear): both faces read the latch edge at u 1, as a
double-acting leaf wears alike on both faces (door_hardware 10 section 8). The edges and the top sample the mask's hinge strip
and its top: both stay plain.

The hardware of the D1.6 rows: the lever and rose (free rows) or the knob and escutcheon (locked rows) at Z 0.92, Y 1.00 / 0.968,
the latch at the flat end Z 0.995; the 10 in kick plates (Y 0.018-0.272, Z 0.030-0.970; in the louvre leaves' mesh, the rows'
Kit_DoorKickPlate_Map on the walnut and FRP leaves); the F1 lite (Z 0.273-0.527, Y 1.143-1.905, its frame 19 mm out), the A2 louvre
(Z 0.273-0.527, Y 0.330-0.737, frame 19 mm out) and the A1 porthole (centre Z 0.324, Y 1.397, the ring to r 0.2251). The plates, the
louvre and the porthole cover their rects: those stay clear.

The three masks:
  OfficeMap (OF-F walnut G, OF-K almond; the Office's low traffic, research Part O section O2.6, at the map leaf's lever): the hand
    halo round the lever and on the push zone by the latch stile (Y 0.90-1.45, strongest toward the latch edge, patchy, finger
    smudges), knuckle marks at the latch stile (Y 0.80-0.95), the latch edge darkened (Y 0.90-1.30), a dull chair-arm band toward
    the latch side (Y 0.58-0.79), pale cart nicks just above the kick plate (Y 0.280-0.345), a faint hand-height bloom.
  BackOfHouseMap (BH-F, B-2a's warm grey; the service way hand trucks pass, research section B.5): grime round the lever and the
    push zone (darker, a little glossier), grey-primer chips just above the kick plate and round the louvre's lower corners, hand-
    truck nose marks over the plate's top, knuckle marks at the latch stile, a duller, greyer lower half.
  PoolMap (PL-F white gel coat, PL-K aqua; PD-A's level preset, research Part P section P3.6): gloss-dull wet-palm ovals round the
    lever and the push zone, a softer palm zone higher up, a chalky bloom low on the leaf where chlorinated mop water dries
    (strongest in the 25 mm strips beside the kick plate and just above it, fading by 0.35 m), the calcium tide line at the foot,
    sparse dried splash spots toward the latch edge; no burnish (gel coat does not burnish).

Run (from the project root):
  python Tools/lookdev/gen_door_wear_map.py Assets/Resources/Surfaces/Textures            -> DoorWear_{OfficeMap,BackOfHouseMap,PoolMap}.png
  python Tools/lookdev/gen_door_wear_map.py <dir> preview                                 -> + preview_<name>.png (R G B A side by side)
"""

import os
import sys

import numpy as np
from PIL import Image

W, H = 1024, 2048
SPAN_U, SPAN_V = 1.0, 2.11                         # metres over u and v
XS = (np.arange(W) + 0.5) / W * SPAN_U             # Z metres from the map hinge
YS = (np.arange(H) + 0.5) / H * SPAN_V             # Y metres over the floor
X, Y = np.meshgrid(XS, YS)                         # [H, W]; row 0 = the floor (flipped when written)

LEAF_Z0, LEAF_Z1, LEAF_Y0, LEAF_Y1 = 0.005, 0.995, 0.015, 2.095
LEVER = (0.920, 1.000)                             # the rose / lever spindle (free rows); the knob at Y 0.968 (locked rows)
KICK = (0.030, 0.970, 0.018, 0.272)
LITE = (0.273 - 0.019, 0.527 + 0.019, 1.143 - 0.019, 1.905 + 0.019)
LOUVRE = (0.273 - 0.019, 0.527 + 0.019, 0.3302 - 0.019, 0.7366 + 0.019)
PORT = (0.324, 1.397, 0.2251)


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def value_noise(rng, cells_x, cells_y):
    g = rng.random((cells_y + 2, cells_x + 2))
    gx = (np.arange(W) + 0.5) / W * cells_x
    gy = (np.arange(H) + 0.5) / H * cells_y
    ix, iy = np.floor(gx).astype(int), np.floor(gy).astype(int)
    fx, fy = gx - ix, gy - iy
    fx = fx * fx * (3 - 2 * fx)
    fy = fy * fy * (3 - 2 * fy)
    a = g[iy][:, ix]
    b = g[iy][:, ix + 1]
    c = g[iy + 1][:, ix]
    d = g[iy + 1][:, ix + 1]
    top = a + (b - a) * fx[None, :]
    bot = c + (d - c) * fx[None, :]
    return top + (bot - top) * fy[:, None]


def fbm(rng, base_m, octaves=4, gain=0.5):
    out = np.zeros((H, W))
    amp, tot = 1.0, 0.0
    scale = base_m
    for _ in range(octaves):
        out += amp * value_noise(rng, max(1, int(round(SPAN_U / scale))), max(1, int(round(SPAN_V / scale))))
        tot += amp
        amp *= gain
        scale /= 2.0
    return out / tot


def band(y0, y1, soft):
    return smoothstep(y0 - soft, y0 + soft, Y) * (1.0 - smoothstep(y1 - soft, y1 + soft, Y))


def blobs(rng, n, x_range, y_range, size_range, aspect=(1.0, 1.0), angle_jitter=0.3):
    out = np.zeros((H, W))
    for _ in range(n):
        cx, cy = rng.uniform(*x_range), rng.uniform(*y_range)
        rx = rng.uniform(*size_range)
        ry = rx * rng.uniform(*aspect)
        th = rng.uniform(-angle_jitter, angle_jitter)
        c, s = np.cos(th), np.sin(th)
        r = max(rx, ry) * 3
        x0, x1 = np.searchsorted(XS, cx - r), np.searchsorted(XS, cx + r)
        y0, y1 = np.searchsorted(YS, cy - r), np.searchsorted(YS, cy + r)
        if x1 <= x0 or y1 <= y0:
            continue
        lx = X[y0:y1, x0:x1] - cx
        ly = Y[y0:y1, x0:x1] - cy
        u = (lx * c + ly * s) / rx
        v = (-lx * s + ly * c) / ry
        out[y0:y1, x0:x1] = np.maximum(out[y0:y1, x0:x1], np.exp(-(u * u + v * v) * 2.2))
    return out


def flakes(rng, n, x_range, y_range, size_range, aspect=(1.4, 3.2), cluster=(1, 4), tilt=0.35):
    """gen_door_wear.flakes: angular paint chips, hard-edged (3 x 3 samples a texel), in small clusters."""
    out = np.zeros((H, W))
    ss = 3
    dx, dy = SPAN_U / W, SPAN_V / H
    for _ in range(n):
        cx, cy = rng.uniform(*x_range), rng.uniform(*y_range)
        k = int(rng.integers(cluster[0], cluster[1] + 1))
        for m in range(k):
            ox = cx + (rng.normal(0.0, 0.007) if m else 0.0)
            oy = cy + (rng.normal(0.0, 0.0025) if m else 0.0)
            length = rng.uniform(*size_range) * (1.0 if m == 0 else rng.uniform(0.3, 0.7))
            a = length / 2.0
            b = a / rng.uniform(*aspect)
            th = rng.uniform(-tilt, tilt)
            nv = int(rng.integers(5, 9))
            ang = np.sort(rng.uniform(0.0, 2.0 * np.pi, nv))
            rad = rng.uniform(0.55, 1.0, nv)
            lx, ly = a * rad * np.cos(ang), b * rad * np.sin(ang)
            c, s_ = np.cos(th), np.sin(th)
            px, py = ox + lx * c - ly * s_, oy + lx * s_ + ly * c
            x0, x1 = np.searchsorted(XS, px.min() - 0.002), np.searchsorted(XS, px.max() + 0.002)
            y0, y1 = np.searchsorted(YS, py.min() - 0.002), np.searchsorted(YS, py.max() + 0.002)
            if x1 <= x0 or y1 <= y0:
                continue
            gx = XS[x0:x1][None, :, None, None] + (np.arange(ss)[None, None, None, :] - (ss - 1) / 2.0) * dx / ss
            gy = YS[y0:y1][:, None, None, None] + (np.arange(ss)[None, None, :, None] - (ss - 1) / 2.0) * dy / ss
            gx, gy = np.broadcast_arrays(gx, gy)
            inside = np.zeros(gx.shape, dtype=bool)
            for i in range(nv):
                xa, ya, xb, yb = px[i], py[i], px[(i + 1) % nv], py[(i + 1) % nv]
                cross = (ya > gy) != (yb > gy)
                xint = (xb - xa) * (gy - ya) / ((yb - ya) if abs(yb - ya) > 1e-12 else 1e-12) + xa
                inside ^= cross & (gx < xint)
            out[y0:y1, x0:x1] = np.maximum(out[y0:y1, x0:x1], inside.mean(axis=(2, 3)))
    return out


def streaks(rng, n, x_range, y_range, length_range, width_range, slope=0.06):
    return blobs(rng, n, x_range, y_range, (length_range[0] / 2, length_range[1] / 2),
                 aspect=(width_range[0] / length_range[1], width_range[1] / length_range[0]), angle_jitter=slope)


def rect_in(r, margin=0.0):
    return (X > r[0] - margin) & (X < r[1] + margin) & (Y > r[2] - margin) & (Y < r[3] + margin)


def clear(m, louvre=False, porthole=False, lite=False):
    """The plates, the louvre, the porthole (and, faintly, the lite's frame) cover their rects; outside the leaf stays plain."""
    keep = np.ones((H, W))
    keep[rect_in(KICK, 0.002)] = 0.0
    if louvre:
        keep[rect_in(LOUVRE, 0.002)] = 0.0
    if porthole:
        keep[np.hypot(X - PORT[0], Y - PORT[1]) < PORT[2] + 0.002] = 0.0
    if lite:
        keep[rect_in(LITE, 0.0)] *= 0.35
    outside = (X < LEAF_Z0) | (X > LEAF_Z1) | (Y < LEAF_Y0) | (Y > LEAF_Y1)
    keep[outside] = 0.0
    keep *= smoothstep(0.006, 0.030, X)          # the hinge strip stays plain (the edges and the stile arc sample it)
    return m * keep[..., None]


def hand_zone(rng, inner, outer, latch_bias):
    """The hand halo of a lever on a pushed DA leaf: palms and fingertips round the rose and the lever (inner..outer metres off the
    spindle), and the push zone by the latch stile above it; patchy (noise-broken) with finger smudges."""
    d = np.hypot((X - LEVER[0]) * 1.0, (Y - LEVER[1]) * 1.25)
    ring = smoothstep(0.030, 0.030 + inner, d) * (1.0 - smoothstep(inner, outer, d))
    push = band(1.02, 1.45, 0.06) * smoothstep(0.70, 0.90, X)
    side = 0.55 + latch_bias * smoothstep(0.75, 0.99, X)
    patchy = smoothstep(0.30, 0.78, fbm(rng, 0.05, 4))
    smudge = blobs(rng, 18, (0.74, 0.985), (0.88, 1.42), (0.008, 0.022), aspect=(0.6, 1.4), angle_jitter=1.5)
    smudge *= smoothstep(0.035, 0.05, d)
    return np.clip((ring + 0.65 * push) * side * (0.25 + 1.0 * patchy) + 0.55 * smudge * side, 0.0, 1.0)


def office(rng):
    m = np.zeros((H, W, 4))
    h = hand_zone(rng, 0.07, 0.15, 0.6)
    m[..., 0] += 0.90 * h
    m[..., 2] += 0.90 * h
    k = blobs(rng, 14, (0.915, 0.983), (0.80, 0.95), (0.004, 0.009), aspect=(0.7, 1.3))
    m[..., 0] += 0.55 * k
    m[..., 1] += 0.55 * k
    e = np.exp(-(LEAF_Z1 - X) / 0.012) * band(0.90, 1.30, 0.07) * (0.6 + 0.8 * fbm(rng, 0.05, 3))
    m[..., 0] += 0.65 * e
    m[..., 2] += 0.25 * e
    a = band(0.58, 0.79, 0.03) * smoothstep(0.45, 0.80, X) * (0.45 + 0.9 * fbm(rng, 0.06, 4))
    sc = streaks(rng, 34, (0.48, 0.97), (0.60, 0.77), (0.04, 0.15), (0.0008, 0.0016), slope=0.05)
    m[..., 1] += np.clip(0.42 * a + 0.35 * sc, 0, 1)
    m[..., 3] += flakes(rng, 26, (0.04, 0.96), (0.280, 0.345), (0.0040, 0.0110), aspect=(1.3, 2.6), cluster=(1, 3))
    m[..., 0] += 0.20 * blobs(rng, 9, (0.06, 0.94), (0.28, 0.34), (0.006, 0.014), aspect=(0.3, 0.6))
    m[..., 1] += 0.10 * band(0.85, 1.45, 0.15) * smoothstep(0.30, 0.99, X) * fbm(rng, 0.2, 3)
    return clear(np.clip(m, 0.0, 1.0), lite=True)


def back_of_house(rng):
    m = np.zeros((H, W, 4))
    h = hand_zone(rng, 0.06, 0.17, 0.8)
    m[..., 0] += 0.60 * h
    m[..., 2] += 0.45 * h
    m[..., 3] += flakes(rng, 34, (0.03, 0.97), (0.280, 0.350), (0.0060, 0.0190), aspect=(1.6, 3.2), cluster=(1, 4))
    # knocks round the louvre's lower corners (a hand truck's nose rides higher than the plate)
    m[..., 3] += flakes(rng, 8, (0.22, 0.58), (0.30, 0.34), (0.0050, 0.0140), aspect=(1.5, 3.0), cluster=(1, 3))
    t = streaks(rng, 20, (0.05, 0.95), (0.278, 0.345), (0.05, 0.20), (0.003, 0.009), slope=0.08)
    m[..., 0] += 0.70 * t
    m[..., 1] += 0.30 * t
    k = blobs(rng, 18, (0.91, 0.983), (0.85, 1.15), (0.004, 0.010), aspect=(0.7, 1.3))
    m[..., 0] += 0.40 * k
    m[..., 1] += 0.45 * k
    m[..., 1] += 0.12 * (1.0 - smoothstep(0.4, 1.0, Y)) * fbm(rng, 0.25, 3)
    m[..., 0] += 0.08 * (1.0 - smoothstep(0.3, 0.9, Y)) * fbm(rng, 0.18, 3)
    return clear(np.clip(m, 0.0, 1.0), louvre=True, lite=True)


def pool(rng):
    m = np.zeros((H, W, 4))
    h = hand_zone(rng, 0.07, 0.14, 0.8)
    m[..., 1] += 0.45 * h
    m[..., 0] += 0.08 * h * smoothstep(0.35, 0.75, fbm(rng, 0.03, 3))
    palm = band(1.35, 1.52, 0.04) * smoothstep(0.66, 0.84, X) * (0.35 + 0.8 * fbm(rng, 0.05, 3))
    m[..., 1] += 0.22 * np.clip(palm, 0.0, 1.0)
    fade = 1.0 - smoothstep(0.05, 0.35, Y)
    strips = 1.0 - smoothstep(KICK[0] - 0.004, KICK[0] + 0.004, X) * (1.0 - smoothstep(KICK[1] - 0.004, KICK[1] + 0.004, X))
    above = band(0.275, 0.33, 0.012)
    patchy = smoothstep(0.30, 0.80, fbm(rng, 0.035, 4))
    bloom = np.clip(fade * (0.30 + 0.70 * strips) * patchy + 0.55 * above * patchy, 0.0, 1.0)
    m[..., 1] += 0.30 * bloom
    m[..., 3] += 0.25 * bloom
    edge = 0.036 + 0.006 * (fbm(rng, 0.06, 3) - 0.5)
    tide = smoothstep(0.016, 0.019, Y) * (1.0 - smoothstep(edge - 0.002, edge + 0.002, Y))
    m[..., 3] += 0.70 * tide * (0.75 + 0.25 * fbm(rng, 0.02, 3))
    spots = blobs(rng, 60, (0.06, 0.985), (0.28, 0.55), (0.0005, 0.0020), aspect=(0.7, 1.3), angle_jitter=1.5)
    spots *= 0.55 + 0.45 * smoothstep(0.2, 0.99, X)
    m[..., 3] += 0.40 * spots
    return clear(np.clip(m, 0.0, 1.0), porthole=True)


MASKS = (("DoorWear_OfficeMap", office, 61116), ("DoorWear_BackOfHouseMap", back_of_house, 61117), ("DoorWear_PoolMap", pool, 61118))


def write(path, m):
    img = np.flipud(np.clip(m * 255.0 + 0.5, 0, 255).astype(np.uint8))   # row 0 = the leaf's top in the PNG
    Image.fromarray(img, "RGBA").save(path, optimize=True)


def main(argv):
    out = argv[0] if argv else "Assets/Resources/Surfaces/Textures"
    preview = len(argv) > 1 and argv[1] == "preview"
    os.makedirs(out, exist_ok=True)
    for name, fn, seed in MASKS:
        m = fn(np.random.default_rng(seed))
        write(os.path.join(out, name + ".png"), m)
        print("%s: R %.3f G %.3f B %.3f A %.3f (mean); max %.2f %.2f %.2f %.2f" % ((name,) + tuple(m[..., i].mean() for i in range(4)) + tuple(m[..., i].max() for i in range(4))))
        if preview:
            tiles = [np.flipud((np.clip(m[..., i], 0, 1) * 255).astype(np.uint8)) for i in range(4)]
            Image.fromarray(np.concatenate(tiles, axis=1), "L").resize((W, H // 4)).save(os.path.join(out, "preview_" + name + ".png"))


if __name__ == "__main__":
    main(sys.argv[1:])
