"""Door wear: the leaf-wear masks of the zone pairs (游戏视觉, zone doors Build A, 2026-11-06; Documentation/research/double_doors/
20_design.md Part A, sections A4 and A6). numpy + Pillow only, no downloads, deterministic (fixed seeds).

Run (from the project root):
  python Tools/lookdev/gen_door_wear.py Assets/Resources/Surfaces/Textures      -> DoorWear_{Office,BackOfHouse,Run}.png
  python Tools/lookdev/gen_door_wear.py <dir> preview                            -> + preview_<name>.png (each channel as grey)

What a mask is: one leaf's face, the same on both faces (a double-acting leaf is pushed from both sides and wears alike,
door_hardware 10 section 8), drawn over the title pair's slab (1.1955 x 2.639 m, its UVs metres from the slab's min corner).
1024 x 2048 RGBA, linear: u 0 at the HINGE edge, 1 at the MEETING edge (the right leaf's material mirrors u through
_FR_LeafWearRect), v 0 at the leaf's bottom (y 0.018), 1 at its top. 1.17 x 1.29 mm a texel. FrontRooms/Surface's
_FR_LEAFWEAR reads it in mesh-UV mode:
  R grime    albedo x (1 - 0.35 R)
  G dull     smoothness x (1 - 0.5 G)
  B burnish  smoothness + 0.20 B
  A chip     albedo -> _FR_LeafWearChip by A, smoothness x (1 - 0.3 A)
The HW-DA1 push and kick plate rects stay clear: the plates' own maps carry their wear (palm oval, heel streaks, tide line).
Everything is soft-edged and noise-broken, never a stamp.
Fix A (2026-11-06, Review A): the chips were soft round blobs and, with the chip colour stored as if linear in an sRGB Color
slot, rendered near-black red-brown: on the grey and almond leaves they read as drops of blood. Chips are now angular paint
flakes (irregular 5-8 sided polygons, wider than tall, hard-edged, in small clusters round each knock: flakes(), 3 x 3
supersampled), the steel leaves' chip colour is the grey shop primer, and every chip colour is stored sRGB-encoded in the
materials (as A8 #11 did for the base colour). The Office's wear was invisible at 0.35 m in the game's light: its hand halo,
knuckle and edge grime are about 60 % stronger (still an office door's low traffic) and its pale nicks now render pale.

The three masks (20_design.md A4):
  Office (O-1, walnut G, the Office's low traffic): the hand halo round the push plate (darker, glossier lacquer 60-120 mm
    beyond it, strongest on the meeting-edge side), knuckle marks at the meeting stile (y 0.80-0.95), the meeting edge
    darkened (y 0.90-1.30), a dull chair-arm band toward the meeting edge (y 0.58-0.79), pale cart nicks just above the kick
    plate (y 0.275-0.35).
  BackOfHouse (B-2, painted hollow metal, the service way hand trucks pass): grey-primer chips just above the kick plate
    (y 0.275-0.35), hand-truck nose marks (y 0.15-0.35, above the plate's top), grime round the push plate (y 0.95-1.35,
    darker, a little glossier), knuckle marks at the meeting stile.
  Run (C-1, almond hollow metal, janitor carts): cart scuffs (y 0.25-0.45: primer chips, dark rubber), grime at the push
    plate, a few dents on the lower leaf (shading only).
Build B (2026-11-06; 20_design.md Part B, section B3.6) adds the pool's level preset (one mask for the title Pool and Dark pool
pairs, PD-A's white gel coat; the level is pristine, the club worn, so the title takes the faintest wear):
  Pool (PD-A, moulded FRP, wet palms and dried pool water): gloss-dull hand ovals round the push plate (G .45, y 0.90-1.40, 60-120
    mm beyond it, strongest at the meeting edge) and a softer palm zone (y 1.35-1.50); a faint grey core in them (R .08); a chalky
    bloom low on the leaf where chlorinated mop water and splashes dry (G .30, A .25, y 0.018-0.35 fading upward, strongest in the
    19 mm strips beside the kick plate and just above it); the calcium tide line (A .70, y 0.018-0.040, on the strips and the
    bottom edge); sparse dried splash spots 1-4 mm across (A .40, y 0.28-0.55); no burnish (gel coat does not burnish). Its chip
    colour is the calcium's chalk white (sRGB 236 234 226): A lifts the leaf a little paler and duller, never darker. The club
    preset (yellowed lower leaf, tea-stained plates) is the map's PoolClub rows (the Map stage), not drawn here.
Run with --only <name> to write one mask (the others are left as they are on disk).
"""

import os
import sys

import numpy as np
from PIL import Image

W, H = 1024, 2048
LEAF_W, LEAF_H, LEAF_Y0 = 1.1955, 2.639, 0.018
# HW-DA1 (FrontRoomsDoubleDoorKit): push plate 4 x 16 in, centre 45 in, 1-1/2 in from the meeting edge; kick plate 10 in,
# the leaf's width less 1-1/2 in, centred, its bottom 3 mm over the leaf's
PUSH = (LEAF_W - 0.0381 - 0.1016, LEAF_W - 0.0381, 1.143 - 0.2032, 1.143 + 0.2032)
KICK = (0.0381 / 2, LEAF_W - 0.0381 / 2, LEAF_Y0 + 0.003, LEAF_Y0 + 0.003 + 0.254)

# x metres from the hinge edge, y metres above the floor, per texel centre
XS = (np.arange(W) + 0.5) / W * LEAF_W
YS = LEAF_Y0 + (np.arange(H) + 0.5) / H * LEAF_H
X, Y = np.meshgrid(XS, YS)          # [H, W], row 0 = the leaf's bottom (flipped when written)


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def value_noise(rng, cells_x, cells_y):
    """One octave of value noise over the mask (bilinear between a random grid), in [0, 1]."""
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
    """Fractal noise with features about base_m metres across, in [0, 1]."""
    out = np.zeros((H, W))
    amp, tot = 1.0, 0.0
    scale = base_m
    for _ in range(octaves):
        cx = max(1, int(round(LEAF_W / scale)))
        cy = max(1, int(round(LEAF_H / scale)))
        out += amp * value_noise(rng, cx, cy)
        tot += amp
        amp *= gain
        scale /= 2.0
    return out / tot


def rect_distance(r):
    """Distance (m) from the rect (x0, x1, y0, y1); 0 inside."""
    dx = np.maximum(np.maximum(r[0] - X, X - r[1]), 0.0)
    dy = np.maximum(np.maximum(r[2] - Y, Y - r[3]), 0.0)
    return np.hypot(dx, dy)


def band(y0, y1, soft):
    return smoothstep(y0 - soft, y0 + soft, Y) * (1.0 - smoothstep(y1 - soft, y1 + soft, Y))


def blobs(rng, n, x_range, y_range, size_range, aspect=(1.0, 1.0), angle_jitter=0.3):
    """Sum of n soft elliptical blobs (max-combined), sizes in metres."""
    out = np.zeros((H, W))
    for _ in range(n):
        cx = rng.uniform(*x_range)
        cy = rng.uniform(*y_range)
        rx = rng.uniform(*size_range)
        ry = rx * rng.uniform(*aspect)
        th = rng.uniform(-angle_jitter, angle_jitter)
        c, s = np.cos(th), np.sin(th)
        # local window only (speed)
        r = max(rx, ry) * 3
        x0, x1 = np.searchsorted(XS, cx - r), np.searchsorted(XS, cx + r)
        y0, y1 = np.searchsorted(YS, cy - r), np.searchsorted(YS, cy + r)
        if x1 <= x0 or y1 <= y0:
            continue
        lx = X[y0:y1, x0:x1] - cx
        ly = Y[y0:y1, x0:x1] - cy
        u = (lx * c + ly * s) / rx
        v = (-lx * s + ly * c) / ry
        d = u * u + v * v
        blob = np.exp(-d * 2.2)
        out[y0:y1, x0:x1] = np.maximum(out[y0:y1, x0:x1], blob)
    return out


def flakes(rng, n, x_range, y_range, size_range, aspect=(1.4, 3.2), cluster=(1, 4), tilt=0.35):
    """Angular paint chips (Fix A): n knocks, each a cluster of 1-4 flakes; a flake is an irregular star-shaped polygon of 5-8
    corners, size_range metres along its long axis, aspect wider than tall (a cart or a hand truck strikes sideways), turned
    within +-tilt rad; hard-edged (coverage from 3 x 3 samples a texel), max-combined. Never a round blob."""
    out = np.zeros((H, W))
    ss = 3
    for _ in range(n):
        cx = rng.uniform(*x_range)
        cy = rng.uniform(*y_range)
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
            dx, dy = LEAF_W / W, LEAF_H / H
            gx = XS[x0:x1][None, :, None, None] + (np.arange(ss)[None, None, None, :] - (ss - 1) / 2.0) * dx / ss
            gy = YS[y0:y1][:, None, None, None] + (np.arange(ss)[None, None, :, None] - (ss - 1) / 2.0) * dy / ss
            gx, gy = np.broadcast_arrays(gx, gy)
            inside = np.zeros(gx.shape, dtype=bool)
            for i in range(nv):
                xa, ya, xb, yb = px[i], py[i], px[(i + 1) % nv], py[(i + 1) % nv]
                cross = (ya > gy) != (yb > gy)
                xint = (xb - xa) * (gy - ya) / ((yb - ya) if abs(yb - ya) > 1e-12 else 1e-12) + xa
                inside ^= cross & (gx < xint)
            cov = inside.mean(axis=(2, 3))
            out[y0:y1, x0:x1] = np.maximum(out[y0:y1, x0:x1], cov)
    return out


def streaks(rng, n, x_range, y_range, length_range, width_range, slope=0.06):
    """Horizontal swipes (rubber, shoe and cart marks): long thin soft ellipses with a little slope."""
    return blobs(rng, n, x_range, y_range, (length_range[0] / 2, length_range[1] / 2),
                 aspect=(width_range[0] / length_range[1], width_range[1] / length_range[0]), angle_jitter=slope)


def clear_plates(m, margin=0.002):
    keep = np.ones((H, W))
    for r in (PUSH, KICK):
        inside = (X > r[0] - margin) & (X < r[1] + margin) & (Y > r[2] - margin) & (Y < r[3] + margin)
        keep[inside] = 0.0
    return m * keep[..., None]


def halo(rng, inner, outer, meeting_bias):
    """The hand halo round the push plate: palms and fingertips that miss the plate, inner..outer metres beyond it, on the
    meeting-edge side and above and below it, almost none on its hinge side; patchy (noise-broken) with finger smudges."""
    d = rect_distance(PUSH)
    ring = smoothstep(0.0, inner, d) * (1.0 - smoothstep(inner, outer, d))
    ring = np.where(d <= 0.0, 0.0, ring)
    side = smoothstep(PUSH[0] - 0.10, PUSH[0] + 0.03, X) * (0.55 + meeting_bias * smoothstep(PUSH[0], LEAF_W, X))
    patchy = smoothstep(0.30, 0.78, fbm(rng, 0.05, 4))
    smudge = blobs(rng, 16, (PUSH[0] - 0.02, LEAF_W - 0.004), (PUSH[2] - 0.10, PUSH[3] + 0.12), (0.008, 0.022), aspect=(0.6, 1.4), angle_jitter=1.5)
    smudge = smudge * smoothstep(0.003, 0.02, d)
    return np.clip(ring * side * (0.25 + 1.0 * patchy) + 0.55 * smudge * side, 0.0, 1.0)


def office(rng):
    m = np.zeros((H, W, 4))
    h = halo(rng, 0.06, 0.13, 0.6) * band(0.90, 1.40, 0.06)
    m[..., 0] += 0.90 * h                    # Fix A: 0.55 -> 0.90 (it did not read at 0.35 m in the game's light)
    m[..., 2] += 0.90 * h
    # knuckle marks: small dull dark spots near the meeting stile
    k = blobs(rng, 14, (LEAF_W - 0.075, LEAF_W - 0.012), (0.80, 0.95), (0.004, 0.009), aspect=(0.7, 1.3))
    m[..., 0] += 0.55 * k
    m[..., 1] += 0.55 * k
    # the meeting edge darkened (hands on the edge)
    e = np.exp(-(LEAF_W - X) / 0.012) * band(0.90, 1.30, 0.07) * (0.6 + 0.8 * fbm(rng, 0.05, 3))
    m[..., 0] += 0.65 * e
    m[..., 2] += 0.25 * e
    # a dull chair-arm band toward the meeting edge: the arm of a pushed office chair rubs the lacquer flat
    a = band(0.58, 0.79, 0.03) * smoothstep(0.50, 0.85, X) * (0.45 + 0.9 * fbm(rng, 0.06, 4))
    sc = streaks(rng, 40, (0.55, LEAF_W - 0.02), (0.60, 0.77), (0.04, 0.16), (0.0008, 0.0016), slope=0.05)
    m[..., 1] += np.clip(0.42 * a + 0.35 * sc, 0, 1)
    # pale cart nicks just above the kick plate (the lacquer and the dark stain broken to pale wood): small angular flakes
    m[..., 3] += flakes(rng, 30, (0.03, LEAF_W - 0.03), (0.280, 0.345), (0.0040, 0.0110), aspect=(1.3, 2.6), cluster=(1, 3))
    m[..., 0] += 0.20 * blobs(rng, 10, (0.05, LEAF_W - 0.05), (0.28, 0.34), (0.006, 0.014), aspect=(0.3, 0.6))
    # a faint general hand-height bloom (the lacquer duller where people push without the plate)
    m[..., 1] += 0.10 * band(0.85, 1.45, 0.15) * smoothstep(0.35, LEAF_W, X) * fbm(rng, 0.2, 3)
    return clear_plates(np.clip(m, 0.0, 1.0))


def back_of_house(rng):
    m = np.zeros((H, W, 4))
    # grime round the push plate (darker, a little glossier)
    h = halo(rng, 0.05, 0.15, 0.8) * band(0.95, 1.35, 0.07)
    m[..., 0] += 0.60 * h
    m[..., 2] += 0.45 * h
    # grey-primer chips just above the kick plate: angular flakes in clusters, where hand trucks knock the leaf
    m[..., 3] += flakes(rng, 40, (0.02, LEAF_W - 0.02), (0.280, 0.350), (0.0060, 0.0190), aspect=(1.6, 3.2), cluster=(1, 4))
    # hand-truck nose marks: dark rubber swipes over the plate's top and a little above
    t = streaks(rng, 22, (0.05, LEAF_W - 0.05), (0.278, 0.345), (0.05, 0.22), (0.003, 0.009), slope=0.08)
    m[..., 0] += 0.70 * t
    m[..., 1] += 0.30 * t
    # knuckle marks at the meeting stile
    k = blobs(rng, 18, (LEAF_W - 0.08, LEAF_W - 0.012), (0.85, 1.15), (0.004, 0.010), aspect=(0.7, 1.3))
    m[..., 0] += 0.40 * k
    m[..., 1] += 0.45 * k
    # a service door's general handling: a duller, slightly greyer lower half
    m[..., 1] += 0.12 * (1.0 - smoothstep(0.4, 1.0, Y)) * fbm(rng, 0.25, 3)
    m[..., 0] += 0.08 * (1.0 - smoothstep(0.3, 0.9, Y)) * fbm(rng, 0.18, 3)
    return clear_plates(np.clip(m, 0.0, 1.0))


def run(rng):
    m = np.zeros((H, W, 4))
    h = halo(rng, 0.05, 0.13, 0.7) * band(0.95, 1.35, 0.07)
    m[..., 0] += 0.55 * h
    m[..., 2] += 0.45 * h
    # janitor-cart scuffs: grey-primer chips (angular flakes, the cart's corner height) and dark rubber, 0.25-0.45 m
    m[..., 3] += flakes(rng, 44, (0.03, LEAF_W - 0.03), (0.280, 0.450), (0.0060, 0.0170), aspect=(1.6, 3.2), cluster=(1, 4))
    rub = streaks(rng, 26, (0.05, LEAF_W - 0.05), (0.280, 0.440), (0.04, 0.20), (0.003, 0.010), slope=0.06)
    m[..., 0] += 0.65 * rub
    m[..., 1] += 0.25 * rub
    # a few dents on the lower leaf (shading only: a darker, duller crescent under each)
    d = blobs(rng, 5, (0.15, LEAF_W - 0.15), (0.36, 0.80), (0.015, 0.03), aspect=(0.6, 1.0))
    m[..., 0] += 0.18 * d
    m[..., 1] += 0.30 * d
    k = blobs(rng, 12, (LEAF_W - 0.07, LEAF_W - 0.012), (0.85, 1.10), (0.004, 0.010), aspect=(0.7, 1.3))
    m[..., 0] += 0.35 * k
    m[..., 1] += 0.40 * k
    return clear_plates(np.clip(m, 0.0, 1.0))


def pool(rng):
    """PD-A, the title Pool and Dark pool pairs (20_design.md B3.6): the level preset, the faintest wear of the zone doors."""
    m = np.zeros((H, W, 4))
    # wet palms dull the gel coat first: hand ovals round the push plate, strongest on the meeting-edge side, and a softer palm zone
    h = halo(rng, 0.06, 0.12, 0.8) * band(0.90, 1.40, 0.06)
    m[..., 1] += 0.45 * h
    m[..., 0] += 0.08 * h * smoothstep(0.35, 0.75, fbm(rng, 0.03, 3))
    palm = band(1.35, 1.50, 0.04) * smoothstep(PUSH[0] - 0.08, PUSH[0] + 0.02, X) * (0.35 + 0.8 * fbm(rng, 0.05, 3))
    m[..., 1] += 0.22 * np.clip(palm, 0.0, 1.0)
    # chalky bloom low on the leaf: chlorinated mop water and splashes dry there; strongest in the 19 mm strips beside the kick
    # plate (the plate covers the middle) and in a band just above it, fading upward by 0.35 m, noise-broken
    fade = 1.0 - smoothstep(0.05, 0.35, Y)
    strips = 1.0 - smoothstep(KICK[0] - 0.004, KICK[0] + 0.004, X) * (1.0 - smoothstep(KICK[1] - 0.004, KICK[1] + 0.004, X))
    above = band(0.277, 0.33, 0.012)
    patchy = smoothstep(0.30, 0.80, fbm(rng, 0.035, 4))
    bloom = np.clip(fade * (0.30 + 0.70 * strips) * patchy + 0.55 * above * patchy, 0.0, 1.0)
    m[..., 1] += 0.30 * bloom
    m[..., 3] += 0.25 * bloom
    # the calcium tide line along the bottom (y 0.018-0.040), its upper edge wavy where the water stood longer
    edge = 0.034 + 0.006 * (fbm(rng, 0.06, 3) - 0.5)
    tide = smoothstep(0.016, 0.019, Y) * (1.0 - smoothstep(edge - 0.002, edge + 0.002, Y))
    m[..., 3] += 0.70 * tide * (0.75 + 0.25 * fbm(rng, 0.02, 3))
    # sparse dried splash spots, 1-4 mm, mostly toward the meeting edge (the swing throws the drops there)
    spots = blobs(rng, 70, (0.04, LEAF_W - 0.01), (0.28, 0.55), (0.0005, 0.0020), aspect=(0.7, 1.3), angle_jitter=1.5)
    spots *= 0.55 + 0.45 * smoothstep(0.2, LEAF_W, X)
    m[..., 3] += 0.40 * spots
    return clear_plates(np.clip(m, 0.0, 1.0))


MASKS = (("DoorWear_Office", office, 51106), ("DoorWear_BackOfHouse", back_of_house, 51107), ("DoorWear_Run", run, 51108),
         ("DoorWear_Pool", pool, 51109))


def write(path, m):
    img = np.flipud(np.clip(m * 255.0 + 0.5, 0, 255).astype(np.uint8))   # row 0 = the leaf's top in the PNG
    Image.fromarray(img, "RGBA").save(path, optimize=True)


def main(argv):
    only = None
    if "--only" in argv:
        k = argv.index("--only")
        only = argv[k + 1]
        argv = argv[:k] + argv[k + 2:]
    out = argv[0] if argv else "Assets/Resources/Surfaces/Textures"
    preview = len(argv) > 1 and argv[1] == "preview"
    os.makedirs(out, exist_ok=True)
    for name, fn, seed in MASKS:
        if only is not None and name != only:
            continue
        m = fn(np.random.default_rng(seed))
        write(os.path.join(out, name + ".png"), m)
        print("%s: R %.3f G %.3f B %.3f A %.3f (mean); max %.2f %.2f %.2f %.2f" % ((name,) + tuple(m[..., i].mean() for i in range(4)) + tuple(m[..., i].max() for i in range(4))))
        if preview:
            tiles = [np.flipud((np.clip(m[..., i], 0, 1) * 255).astype(np.uint8)) for i in range(4)]
            Image.fromarray(np.concatenate(tiles, axis=1), "L").resize((W * 4 // 4, H // 4)).save(os.path.join(out, "preview_" + name + ".png"))


if __name__ == "__main__":
    main(sys.argv[1:])
